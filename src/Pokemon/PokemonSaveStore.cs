using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StardewValley.Pokemon;

/// <summary>
/// One versioned save document for every piece of PokeValley state
/// (party, Pokédex and PC boxes), written atomically.
///
/// Replaces the three loose JSON files that used to sit outside the save:
/// they were never written atomically, were shared between all save slots and,
/// in the Pokédex case, lived in %AppData% as a single global file.
///
/// Storage rules:
/// * The file lives inside the save folder whenever that folder already exists
///   (so copying the save folder carries the Pokémon with it).
/// * Farmhands (and read-only installs) fall back to
///   %AppData%/StardewValley/PokeValley/&lt;save key&gt;/, never to a shared global file.
/// * Writes go to a .tmp file and replace the target, keeping a .bak of the
///   previous good version.
/// </summary>
public static class PokemonSaveStore
{
	public const int SchemaVersion = 2;
	public const string FolderName = "pokevalley";
	public const string FileNameHost = "pokemon_save.json";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		// Null entries inside the box slot lists are meaningful (they are empty slots),
		// so no ignore condition here: the list must keep its positional alignment.
		WriteIndented = true
	};

	private static PokemonSaveFile _data;
	private static string _key;
	private static string _path;
	private static bool _loaded;
	private static string _lastError;
	private static bool _missingPathLogged;
	private static bool _readFailed;

	/// <summary>Last persistence problem, for the debug menu. Null when everything went fine.</summary>
	public static string LastError => _lastError;

	/// <summary>Path of the file currently in use, or null when no save is loaded.</summary>
	public static string CurrentFilePath => _path;

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
	}

	/// <summary>Serialises the current document to disk. Safe to call often.</summary>
	public static void Save()
	{
		EnsureLoaded();
		if (_path == null)
		{
			if (!_missingPathLogged && _key != null)
			{
				_missingPathLogged = true;
				Log("no writable location found; progress will only live in memory this session");
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

	private static void EnsureLoaded()
	{
		string key = CurrentSaveKey();
		if (_loaded && string.Equals(_key, key, StringComparison.Ordinal))
		{
			return;
		}

		_key = key;
		_loaded = true;
		_path = null;
		_data = null;

		if (key == null)
		{
			// No save selected yet: keep an empty document instead of reading a global file
			// that belongs to a different playthrough.
			_data = new PokemonSaveFile();
			return;
		}

		_path = ResolvePath(key);
		if (_path != null)
		{
			_readFailed = false;
			_data = ReadFile(_path);
			if (_data == null && _readFailed)
			{
				// The document exists but could not be read (locked by another process,
				// permissions, antivirus). Keep it on disk untouched and work in memory.
				_path = null;
				_data = new PokemonSaveFile();
				return;
			}
		}

		if (_data == null)
		{
			_data = TryImportLegacyFiles();
			if (_data != null)
			{
				Log("migrated legacy pokemon_*.json files into the unified save");
				Save();
				return;
			}
			_data = new PokemonSaveFile();
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
	public List<PokemonSaveDto> Party { get; set; } = new();
	public int ActiveIndex { get; set; }
	public PokedexSaveData Pokedex { get; set; } = new();
	public PokemonStorageSaveDto Storage { get; set; } = new();
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
