using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StardewValley.Pokemon;

/// <summary>Which part of the document should be flushed.</summary>
public enum PokemonSaveSection
{
	All,
	Party,
	Pokedex,
	Storage
}

/// <summary>
/// One versioned save document for every piece of PokeValley state
/// (party, Pokédex and PC boxes).
///
/// Two carriers are written on every flush:
/// 1. The player's <c>modData</c>, which the vanilla serializer stores inside the
///    save file itself (<c>.sv</c>) and syncs through the vanilla netcode. This is
///    the authoritative copy: a farmhand carries their own party, Pokédex and boxes
///    in their own farmer record, so the host's save contains every player and a
///    copy of the save folder is enough to keep the Pokémon.
/// 2. A JSON mirror inside the save folder (or %AppData% when the folder is not
///    writable), kept for inspection, backups and legacy migration.
///
/// The mirror is never required to play: if the embedded copy exists it wins.
/// </summary>
public static class PokemonSaveStore
{
	public const int SchemaVersion = 3;
	public const string FolderName = "pokevalley";
	public const string FileNameHost = "pokemon_save.json";

	/// <summary>Player modData key that holds the party section.</summary>
	public const string ModDataPartyKey = "PokeValley.Pokemon.Party";
	/// <summary>Player modData key that holds the Pokédex section.</summary>
	public const string ModDataPokedexKey = "PokeValley.Pokemon.Pokedex";
	/// <summary>Player modData key that holds the PC boxes section.</summary>
	public const string ModDataStorageKey = "PokeValley.Pokemon.Storage";
	/// <summary>Player modData key with the UTC ticks of the last flush.</summary>
	public const string ModDataStampKey = "PokeValley.Pokemon.SavedAt";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		// Null entries inside the box slot lists are meaningful (they are empty slots),
		// so no ignore condition here: the list must keep its positional alignment.
		WriteIndented = true
	};

	private static readonly JsonSerializerOptions CompactOptions = new()
	{
		WriteIndented = false
	};

	private static PokemonSaveFile _data;
	private static string _key;
	private static string _path;
	private static bool _loaded;
	private static string _lastError;
	private static bool _missingPathLogged;
	private static bool _readFailed;
	private static bool _reportedEmbeddedRead;
	private static int _lastEmbeddedProbeMs;

	/// <summary>Last persistence problem, for the debug menu. Null when everything went fine.</summary>
	public static string LastError => _lastError;

	/// <summary>Path of the mirror file currently in use, or null when no save is loaded.</summary>
	public static string CurrentFilePath => _path;

	/// <summary>True when the last load came from the copy embedded in the save file.</summary>
	public static bool LoadedFromSaveFile { get; private set; }

	/// <summary>Current save document. Never null once a save is loaded.</summary>
	public static PokemonSaveFile Data
	{
		get
		{
			EnsureLoaded();
			return _data;
		}
	}

	/// <summary>Drops cached state and reads the current save again. Called after a save is loaded.</summary>
	public static void Reload()
	{
		_loaded = false;
		_key = null;
		_path = null;
		_data = null;
		EnsureLoaded();
	}

	/// <summary>Starts from a blank document. Called when a new game is created.</summary>
	public static void ResetForNewGame()
	{
		_data = new PokemonSaveFile();
		_loaded = false;
		_key = null;
		_path = null;
		_lastError = null;
		LoadedFromSaveFile = false;
		ClearPlayerModData();
	}

	/// <summary>
	/// Copies the in-memory document into the player's modData (which is serialised
	/// into the .sv) and refreshes the JSON mirror. Safe to call often.
	/// </summary>
	public static void Save(PokemonSaveSection section = PokemonSaveSection.All)
	{
		EnsureLoaded();
		_data.SavedAtUtc = DateTime.UtcNow.Ticks;
		WritePlayerModData(section);
		WriteMirror();
	}

	private static void EnsureLoaded()
	{
		string key = CurrentSaveKey();
		if (_loaded && string.Equals(_key, key, StringComparison.Ordinal))
		{
			// A farmhand can finish loading the save before the host sends their
			// farmer record. When the embedded document finally arrives, re-read it
			// once so the player does not keep a stale mirror. Throttled because Data
			// is read every frame by the HUD.
			if (!LoadedFromSaveFile
				&& _lastEmbeddedProbeMs > 0
				&& Environment.TickCount - _lastEmbeddedProbeMs > 2000
				&& HasEmbeddedData())
			{
				_lastEmbeddedProbeMs = Environment.TickCount;
				_loaded = false;
			}
			else
			{
				return;
			}
		}

		_key = key;
		_loaded = true;
		_path = null;
		_data = null;
		LoadedFromSaveFile = false;

		if (key == null)
		{
			// No save selected yet: keep an empty document instead of reading a global file
			// that belongs to a different playthrough.
			_data = new PokemonSaveFile();
			return;
		}

		_path = ResolvePath(key);

		var file = new PokemonSaveFile();
		bool haveData = false;

		if (_path != null)
		{
			_readFailed = false;
			PokemonSaveFile mirror = ReadFile(_path);
			if (mirror != null)
			{
				file = mirror;
				haveData = true;
			}
			else if (_readFailed)
			{
				// The mirror exists but could not be read (locked by another process,
				// permissions, antivirus). Keep it on disk untouched and work in memory.
				_path = null;
			}
		}

		// The embedded copy is authoritative, so it is applied on top of the mirror.
		// Exception: a crash between two saves leaves the mirror ahead of the .sv;
		// in that case the newer mirror is recovered instead of rolling progress back.
		long embeddedStamp = ReadEmbeddedStamp();
		bool recoveredFromMirror = haveData && embeddedStamp > 0 && file.SavedAtUtc > embeddedStamp
			&& Game1.player != null && Game1.player.IsMainPlayer;

		if (!recoveredFromMirror && ReadPlayerModData(file))
		{
			haveData = true;
			LoadedFromSaveFile = true;
		}

		if (!haveData)
		{
			PokemonSaveFile legacy = TryImportLegacyFiles();
			if (legacy != null)
			{
				_data = legacy;
				Log("migrated legacy pokemon_*.json files into the unified save");
				Save();
				LoadedFromSaveFile = true;
				return;
			}
		}

		_data = file;
		if (!LoadedFromSaveFile)
		{
			_lastEmbeddedProbeMs = Environment.TickCount;
		}

		if (recoveredFromMirror)
		{
			Log("recovered a newer pokemon mirror after the save file lagged behind");
			Save();
			LoadedFromSaveFile = true;
		}
	}

	/// <summary>UTC ticks stamped on the embedded copy, or 0 when absent/unreadable.</summary>
	private static long ReadEmbeddedStamp()
	{
		try
		{
			var modData = Game1.player?.modData;
			if (modData == null || !modData.ContainsKey(ModDataStampKey)) return 0;
			return long.TryParse(modData[ModDataStampKey], out long ticks) ? ticks : 0;
		}
		catch
		{
			return 0;
		}
	}

	/// <summary>Cheap check used to notice a farmer record that arrived after the load.</summary>
	private static bool HasEmbeddedData()
	{
		try
		{
			var modData = Game1.player?.modData;
			if (modData == null) return false;
			return (modData.ContainsKey(ModDataPartyKey) && !string.IsNullOrWhiteSpace(modData[ModDataPartyKey]))
				|| (modData.ContainsKey(ModDataPokedexKey) && !string.IsNullOrWhiteSpace(modData[ModDataPokedexKey]))
				|| (modData.ContainsKey(ModDataStorageKey) && !string.IsNullOrWhiteSpace(modData[ModDataStorageKey]));
		}
		catch
		{
			return false;
		}
	}

	private static string CurrentSaveKey()
	{
		try
		{
			if (Game1.uniqueIDForThisGame == 0UL) return null;
			string name = Game1.GetSaveGameName(set_value: false);
			if (string.IsNullOrWhiteSpace(name)) name = "Save";
			return SaveGame.FilterFileName(name) + "_" + Game1.uniqueIDForThisGame;
		}
		catch
		{
			return null;
		}
	}

	private static string ResolvePath(string key)
	{
		string fileName = FileNameForPlayer();

		try
		{
			string savesRoot = Program.GetSavesFolder();
			if (!string.IsNullOrEmpty(savesRoot))
			{
				string saveFolder = Path.Combine(savesRoot, key);
				if (Directory.Exists(saveFolder))
				{
					string localDir = Path.Combine(saveFolder, FolderName);
					if (TryPrepareDirectory(localDir))
					{
						return Path.Combine(localDir, fileName);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log($"could not use the save folder: {ex.Message}");
		}

		try
		{
			string fallback = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"StardewValley", "PokeValley", key);
			if (TryPrepareDirectory(fallback))
			{
				return Path.Combine(fallback, fileName);
			}
		}
		catch (Exception ex)
		{
			Log($"could not use the AppData folder: {ex.Message}");
		}

		return null;
	}

	private static string FileNameForPlayer()
	{
		try
		{
			if (Game1.player == null || Game1.player.IsMainPlayer)
			{
				return FileNameHost;
			}
			return $"pokemon_save_{Game1.player.UniqueMultiplayerID}.json";
		}
		catch
		{
			return FileNameHost;
		}
	}

	private static bool TryPrepareDirectory(string dir)
	{
		try
		{
			Directory.CreateDirectory(dir);
			return true;
		}
		catch (Exception ex)
		{
			Log($"cannot create {dir}: {ex.Message}");
			return false;
		}
	}

	// ---- Embedded copy (Farmer.modData -> .sv) ----------------------------

	private static void WritePlayerModData(PokemonSaveSection section)
	{
		try
		{
			var player = Game1.player;
			if (player?.modData == null) return;

			if (section is PokemonSaveSection.All or PokemonSaveSection.Party)
			{
				var party = new PokemonPartySectionDto
				{
					Party = _data.Party ?? new List<PokemonSaveDto>(),
					ActiveIndex = _data.ActiveIndex
				};
				player.modData[ModDataPartyKey] = JsonSerializer.Serialize(party, CompactOptions);
			}

			if (section is PokemonSaveSection.All or PokemonSaveSection.Pokedex)
			{
				player.modData[ModDataPokedexKey] = JsonSerializer.Serialize(
					_data.Pokedex ?? new PokedexSaveData(), CompactOptions);
			}

			if (section is PokemonSaveSection.All or PokemonSaveSection.Storage)
			{
				player.modData[ModDataStorageKey] = JsonSerializer.Serialize(
					_data.Storage ?? new PokemonStorageSaveDto(), CompactOptions);
			}

			player.modData[ModDataStampKey] = _data.SavedAtUtc.ToString();
		}
		catch (Exception ex)
		{
			Log($"could not embed the Pokémon document in the save file: {ex.Message}");
		}
	}

	/// <summary>Overlays any embedded section on <paramref name="file"/>. Returns true when at least one section was read.</summary>
	private static bool ReadPlayerModData(PokemonSaveFile file)
	{
		try
		{
			var player = Game1.player;
			if (player?.modData == null) return false;

			bool any = false;
			any |= ReadPartySection(player.modData, file);
			any |= ReadPokedexSection(player.modData, file);
			any |= ReadStorageSection(player.modData, file);
			return any;
		}
		catch (Exception ex)
		{
			if (!_reportedEmbeddedRead)
			{
				_reportedEmbeddedRead = true;
				Log($"could not read the Pokémon document from the save file: {ex.Message}");
			}
			return false;
		}
	}

	private static bool ReadPartySection(StardewValley.Mods.ModDataDictionary modData, PokemonSaveFile file)
	{
		if (!modData.ContainsKey(ModDataPartyKey)) return false;
		string json = modData[ModDataPartyKey];
		if (string.IsNullOrWhiteSpace(json)) return false;
		try
		{
			var section = JsonSerializer.Deserialize<PokemonPartySectionDto>(json, CompactOptions);
			if (section == null) return false;
			file.Party = section.Party ?? new List<PokemonSaveDto>();
			file.ActiveIndex = section.ActiveIndex;
			return true;
		}
		catch (Exception ex)
		{
			Log($"embedded party section ignored: {ex.Message}");
			return false;
		}
	}

	private static bool ReadPokedexSection(StardewValley.Mods.ModDataDictionary modData, PokemonSaveFile file)
	{
		if (!modData.ContainsKey(ModDataPokedexKey)) return false;
		string json = modData[ModDataPokedexKey];
		if (string.IsNullOrWhiteSpace(json)) return false;
		try
		{
			var section = JsonSerializer.Deserialize<PokedexSaveData>(json, CompactOptions);
			if (section == null) return false;
			file.Pokedex = section;
			return true;
		}
		catch (Exception ex)
		{
			Log($"embedded pokedex section ignored: {ex.Message}");
			return false;
		}
	}

	private static bool ReadStorageSection(StardewValley.Mods.ModDataDictionary modData, PokemonSaveFile file)
	{
		if (!modData.ContainsKey(ModDataStorageKey)) return false;
		string json = modData[ModDataStorageKey];
		if (string.IsNullOrWhiteSpace(json)) return false;
		try
		{
			var section = JsonSerializer.Deserialize<PokemonStorageSaveDto>(json, CompactOptions);
			if (section == null) return false;
			file.Storage = section;
			return true;
		}
		catch (Exception ex)
		{
			Log($"embedded storage section ignored: {ex.Message}");
			return false;
		}
	}

	private static void ClearPlayerModData()
	{
		try
		{
			var player = Game1.player;
			if (player?.modData == null) return;
			player.modData.Remove(ModDataPartyKey);
			player.modData.Remove(ModDataPokedexKey);
			player.modData.Remove(ModDataStorageKey);
			player.modData.Remove(ModDataStampKey);
		}
		catch (Exception ex)
		{
			Log($"could not clear the embedded Pokémon document: {ex.Message}");
		}
	}

	// ---- Mirror file ------------------------------------------------------

	private static void WriteMirror()
	{
		if (_path == null)
		{
			if (!_missingPathLogged && _key != null)
			{
				_missingPathLogged = true;
				Log("no writable mirror location found; progress lives in the save file only this session");
			}
			return;
		}

		try
		{
			string json = JsonSerializer.Serialize(_data, JsonOptions);
			string temp = _path + ".tmp";
			File.WriteAllText(temp, json, new UTF8Encoding(false));
			WriteReplacing(temp, _path);
		}
		catch (Exception ex)
		{
			Log($"failed to write {Path.GetFileName(_path)}: {ex.Message}");
		}
	}

	private static PokemonSaveFile ReadFile(string path)
	{
		try
		{
			if (!File.Exists(path)) return null;
			string json = File.ReadAllText(path);
			if (string.IsNullOrWhiteSpace(json)) return null;

			PokemonSaveFile file = JsonSerializer.Deserialize<PokemonSaveFile>(json, JsonOptions);
			if (file == null) return null;

			file.Party ??= new List<PokemonSaveDto>();
			file.Pokedex ??= new PokedexSaveData();
			file.Storage ??= new PokemonStorageSaveDto();

			if (file.Version > SchemaVersion)
			{
				Log($"save file version {file.Version} is newer than this build supports ({SchemaVersion})");
			}
			return file;
		}
		catch (JsonException ex)
		{
			// Only a broken document is quarantined; IO errors (locked file, permissions)
			// must never move a perfectly good save out of the way.
			Log($"could not parse {Path.GetFileName(path)}: {ex.Message}");
			Quarantine(path);
			return null;
		}
		catch (Exception ex)
		{
			_readFailed = true;
			Log($"could not read {Path.GetFileName(path)}: {ex.Message}");
			return null;
		}
	}

	private static void WriteReplacing(string temp, string target)
	{
		try
		{
			if (File.Exists(target))
			{
				File.Replace(temp, target, target + ".bak", ignoreMetadataErrors: true);
				return;
			}
			File.Move(temp, target);
		}
		catch (Exception)
		{
			// Some filesystems (and network drives) refuse Replace/Move.
			File.Copy(temp, target, overwrite: true);
			try { File.Delete(temp); } catch { }
		}
	}

	private static void Quarantine(string path)
	{
		try
		{
			string moved = path + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
			File.Move(path, moved);
			Log($"moved unreadable file to {Path.GetFileName(moved)}");
		}
		catch (Exception ex)
		{
			Log($"could not quarantine {Path.GetFileName(path)}: {ex.Message}");
		}
	}

	// ---- Legacy migration -------------------------------------------------

	private static PokemonSaveFile TryImportLegacyFiles()
	{
		var folders = new List<string>();
		try
		{
			string savesRoot = Program.GetSavesFolder();
			if (!string.IsNullOrEmpty(savesRoot) && _key != null)
			{
				folders.Add(Path.Combine(savesRoot, _key));
			}
		}
		catch { }

		try
		{
			folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"));
		}
		catch { }

		var file = new PokemonSaveFile();
		var imported = new List<string>();

		foreach (string folder in folders)
		{
			ImportParty(Path.Combine(folder, "pokemon_party_data.json"), file, imported);
			ImportPokedex(Path.Combine(folder, "pokemon_pokedex_data.json"), file, imported);
			ImportStorage(Path.Combine(folder, "pokemon_storage_data.json"), file, imported);
		}

		if (imported.Count == 0) return null;

		foreach (string path in imported)
		{
			try { File.Move(path, path + ".migrated"); } catch { }
		}
		return file;
	}

	private static void ImportParty(string path, PokemonSaveFile file, List<string> imported)
	{
		if (!File.Exists(path) || file.Party.Count > 0) return;
		try
		{
			var party = JsonSerializer.Deserialize<List<PokemonSaveDto>>(File.ReadAllText(path), JsonOptions);
			if (party != null && party.Count > 0)
			{
				file.Party = party;
				imported.Add(path);
			}
		}
		catch (Exception ex)
		{
			Log($"legacy party file ignored: {ex.Message}");
		}
	}

	private static void ImportPokedex(string path, PokemonSaveFile file, List<string> imported)
	{
		if (!File.Exists(path)) return;
		if ((file.Pokedex?.Seen?.Count ?? 0) > 0 || (file.Pokedex?.Caught?.Count ?? 0) > 0) return;
		try
		{
			var dex = JsonSerializer.Deserialize<PokedexSaveData>(File.ReadAllText(path), JsonOptions);
			if (dex != null && ((dex.Seen?.Count ?? 0) > 0 || (dex.Caught?.Count ?? 0) > 0))
			{
				file.Pokedex = dex;
				imported.Add(path);
			}
		}
		catch (Exception ex)
		{
			Log($"legacy pokedex file ignored: {ex.Message}");
		}
	}

	private static void ImportStorage(string path, PokemonSaveFile file, List<string> imported)
	{
		if (!File.Exists(path) || file.Storage.Boxes.Count > 0) return;
		try
		{
			var storage = JsonSerializer.Deserialize<PokemonStorageSaveDto>(File.ReadAllText(path), JsonOptions);
			if (storage != null && storage.Boxes != null && storage.Boxes.Count > 0)
			{
				file.Storage = storage;
				imported.Add(path);
			}
		}
		catch (Exception ex)
		{
			Log($"legacy storage file ignored: {ex.Message}");
		}
	}

	// ---- Diagnostics ------------------------------------------------------

	private static void Log(string message)
	{
		_lastError = message;
		try { Console.WriteLine("[PokeValley] " + message); } catch { }
		try
		{
			string dir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"StardewValley", "PokeValley");
			Directory.CreateDirectory(dir);
			File.AppendAllText(
				Path.Combine(dir, "pokevalley.log"),
				$"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
		}
		catch { }
	}
}

/// <summary>Root document persisted by <see cref="PokemonSaveStore"/>.</summary>
public class PokemonSaveFile
{
	public int Version { get; set; } = PokemonSaveStore.SchemaVersion;
	/// <summary>UTC ticks of the last flush; used to decide between the .sv copy and a newer mirror.</summary>
	public long SavedAtUtc { get; set; }
	public List<PokemonSaveDto> Party { get; set; } = new();
	public int ActiveIndex { get; set; }
	public PokedexSaveData Pokedex { get; set; } = new();
	public PokemonStorageSaveDto Storage { get; set; } = new();
}

/// <summary>Party slice stored under <see cref="PokemonSaveStore.ModDataPartyKey"/>.</summary>
public class PokemonPartySectionDto
{
	public List<PokemonSaveDto> Party { get; set; } = new();
	public int ActiveIndex { get; set; }
}

/// <summary>
/// Single place that converts a live <see cref="PokemonInstance"/> to and from its
/// serialised form. Party, PC boxes and the trade terminal all go through here so
/// they can never drift apart again.
/// </summary>
public static class PokemonDtoMapper
{
	public static PokemonSaveDto ToDto(PokemonInstance p)
	{
		if (p == null || p.Species == null) return null;

		string saveSpecies = p.Species.InternalName ?? p.Species.Name;
		if (p.IsMegaEvolved && p.PreMegaSpecies != null)
		{
			saveSpecies = p.PreMegaSpecies.InternalName ?? p.PreMegaSpecies.Name;
		}
		else if (p.Species.IsMega && !string.IsNullOrEmpty(p.Species.BaseSpeciesName))
		{
			saveSpecies = p.Species.BaseSpeciesName;
		}

		var dto = new PokemonSaveDto
		{
			Species = saveSpecies,
			Nickname = p.Nickname,
			Level = p.Level,
			CurrentHP = p.CurrentHP,
			CurrentExp = p.CurrentExp,
			Nature = p.Nature.ToString(),
			IV_HP = p.IV_HP,
			IV_Attack = p.IV_Attack,
			IV_Defense = p.IV_Defense,
			IV_SpAttack = p.IV_SpAttack,
			IV_SpDefense = p.IV_SpDefense,
			IV_Speed = p.IV_Speed,
			EV_HP = p.EV_HP,
			EV_Attack = p.EV_Attack,
			EV_Defense = p.EV_Defense,
			EV_SpAttack = p.EV_SpAttack,
			EV_SpDefense = p.EV_SpDefense,
			EV_Speed = p.EV_Speed,
			IsEgg = p.IsEgg,
			EggSteps = p.EggSteps,
			IsShiny = p.IsShiny,
			Gender = p.Gender ?? "♂",
			Status = p.Status.ToString(),
			UsedMegaStoneId = p.UsedMegaStoneId,
			HeldItem = p.HeldItem,
			Ability = p.Ability,
			Friendship = p.Friendship,
			TeraType = p.TeraType.ToString()
		};

		if (p.Moves != null)
		{
			foreach (var move in p.Moves)
			{
				if (move == null || string.IsNullOrEmpty(move.Name)) continue;
				dto.Moves.Add(move.Name);
				dto.MovePP.Add(move.CurrentPP);
			}
		}
		return dto;
	}

	public static PokemonInstance FromDto(PokemonSaveDto dto)
	{
		if (dto == null || string.IsNullOrWhiteSpace(dto.Species)) return null;

		var species = PokemonSpecies.Get(dto.Species);
		if (species == null) return null;

		if (species.IsMega && !string.IsNullOrEmpty(species.BaseSpeciesName))
		{
			var baseSpecies = PokemonSpecies.Get(species.BaseSpeciesName);
			if (baseSpecies != null) species = baseSpecies;
		}

		string[] moves = null;
		if (dto.Moves != null)
		{
			var clean = new List<string>();
			foreach (string name in dto.Moves)
			{
				if (!string.IsNullOrWhiteSpace(name)) clean.Add(name);
			}
			if (clean.Count > 0) moves = clean.ToArray();
		}

		var inst = new PokemonInstance(species, Math.Max(1, dto.Level), moves);
		inst.Nickname = string.IsNullOrWhiteSpace(dto.Nickname) ? species.Name : dto.Nickname;

		if (!string.IsNullOrEmpty(dto.Nature) && Enum.TryParse<PokemonNature>(dto.Nature, out var nature))
		{
			inst.Nature = nature;
		}

		inst.IV_HP = dto.IV_HP;
		inst.IV_Attack = dto.IV_Attack;
		inst.IV_Defense = dto.IV_Defense;
		inst.IV_SpAttack = dto.IV_SpAttack;
		inst.IV_SpDefense = dto.IV_SpDefense;
		inst.IV_Speed = dto.IV_Speed;
		inst.EV_HP = dto.EV_HP;
		inst.EV_Attack = dto.EV_Attack;
		inst.EV_Defense = dto.EV_Defense;
		inst.EV_SpAttack = dto.EV_SpAttack;
		inst.EV_SpDefense = dto.EV_SpDefense;
		inst.EV_Speed = dto.EV_Speed;
		inst.IsEgg = dto.IsEgg;
		inst.EggSteps = dto.EggSteps;
		inst.IsShiny = dto.IsShiny;
		inst.UsedMegaStoneId = dto.UsedMegaStoneId;
		inst.HeldItem = dto.HeldItem;

		if (!string.IsNullOrEmpty(dto.Ability)) inst.Ability = dto.Ability;
		if (!string.IsNullOrEmpty(dto.Gender)) inst.Gender = dto.Gender;
		if (!string.IsNullOrEmpty(dto.TeraType) && Enum.TryParse<PokemonType>(dto.TeraType, out var teraType))
		{
			inst.TeraType = teraType;
		}
		if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<PokemonStatusCondition>(dto.Status, out var status))
		{
			inst.Status = status;
		}

		// Battles never resume from a save, so battle-only state is always cleared.
		inst.IsMegaEvolved = false;
		inst.IsTerastallized = false;
		inst.DynamaxTurns = 0;
		inst.IsUsingGigantamax = false;
		inst.PreMegaSpecies = null;
		inst.ConfusionTurns = 0;
		inst.StatusTurns = 0;
		inst.Flinched = false;
		inst.SaltCureTurns = 0;

		// Recalculate before applying HP so the clamp uses the real maximum.
		inst.RecalculateStats();
		inst.CurrentHP = Math.Clamp(dto.CurrentHP, 0, inst.MaxHP);
		inst.CurrentExp = Math.Max(0, dto.CurrentExp);
		inst.Friendship = Math.Clamp(dto.Friendship <= 0 ? 70 : dto.Friendship, 0, 255);

		if (dto.MovePP != null)
		{
			for (int i = 0; i < inst.Moves.Count && i < dto.MovePP.Count; i++)
			{
				inst.Moves[i].CurrentPP = Math.Clamp(dto.MovePP[i], 0, inst.Moves[i].MaxPP);
			}
		}
		return inst;
	}
}
