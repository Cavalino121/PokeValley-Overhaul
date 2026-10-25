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

	private sealed class FarmerSaveEntry
	{
		public PokemonSaveFile Data = new();
		public string Path;
		public string Key;
		public bool Loaded;
		public bool LoadedFromSaveFile;
		public int LastEmbeddedProbeMs;
	}

	private static readonly Dictionary<long, FarmerSaveEntry> _farmerEntries = new();
	private static readonly object _syncLock = new();

	private static string _lastError;
	private static bool _missingPathLogged;
	private static bool _readFailed;
	private static bool _reportedEmbeddedRead;

	/// <summary>Last persistence problem, for the debug menu. Null when everything went fine.</summary>
	public static string LastError => _lastError;

	/// <summary>Path of the mirror file currently in use, or null when no save is loaded.</summary>
	public static string CurrentFilePath => _farmerEntries.TryGetValue(Game1.player?.UniqueMultiplayerID ?? 0L, out var e) ? e.Path : null;

	/// <summary>True when the last load came from the copy embedded in the save file.</summary>
	public static bool LoadedFromSaveFile => _farmerEntries.TryGetValue(Game1.player?.UniqueMultiplayerID ?? 0L, out var e) && e.LoadedFromSaveFile;

	/// <summary>Current save document. Never null once a save is loaded.</summary>
	public static PokemonSaveFile Data => GetDataForFarmer(Game1.player?.UniqueMultiplayerID ?? 0L);

	public static PokemonSaveFile GetDataForFarmer(long farmerId)
	{
		lock (_syncLock)
		{
			EnsureLoadedForFarmer(farmerId);
			if (_farmerEntries.TryGetValue(farmerId, out var entry))
			{
				return entry.Data;
			}
			return new PokemonSaveFile();
		}
	}

	/// <summary>Drops cached state and reads the current save again. Called after a save is loaded.</summary>
	public static void Reload()
	{
		lock (_syncLock)
		{
			_farmerEntries.Clear();
			EnsureLoadedForFarmer(Game1.player?.UniqueMultiplayerID ?? 0L);
		}
	}

	/// <summary>Starts from a blank document. Called when a new game is created.</summary>
	public static void ResetForNewGame()
	{
		lock (_syncLock)
		{
			_farmerEntries.Clear();
			_lastError = null;
			ClearPlayerModData(Game1.player);
		}
	}

	/// <summary>
	/// Copies the in-memory document into the player's modData (which is serialised
	/// into the .sv) and refreshes the JSON mirror. Safe to call often.
	/// </summary>
	public static void Save(PokemonSaveSection section = PokemonSaveSection.All)
	{
		SaveForFarmer(Game1.player?.UniqueMultiplayerID ?? 0L, section);
	}

	public static void SaveForFarmer(long farmerId, PokemonSaveSection section = PokemonSaveSection.All)
	{
		lock (_syncLock)
		{
			EnsureLoadedForFarmer(farmerId);
			if (!_farmerEntries.TryGetValue(farmerId, out var entry)) return;

			entry.Data.SavedAtUtc = DateTime.UtcNow.Ticks;
			Farmer farmer = Game1.getFarmerMaybeOffline(farmerId) ?? (Game1.player?.UniqueMultiplayerID == farmerId ? Game1.player : null);
			WriteFarmerModData(farmer, entry.Data, section);
			WriteMirrorForEntry(entry);
		}
	}

	private static void EnsureLoadedForFarmer(long farmerId)
	{
		string key = CurrentSaveKey();
		if (!_farmerEntries.TryGetValue(farmerId, out var entry))
		{
			entry = new FarmerSaveEntry();
			_farmerEntries[farmerId] = entry;
		}

		if (entry.Loaded && string.Equals(entry.Key, key, StringComparison.Ordinal))
		{
			if (!entry.LoadedFromSaveFile
				&& entry.LastEmbeddedProbeMs > 0
				&& Environment.TickCount - entry.LastEmbeddedProbeMs > 2000
				&& HasEmbeddedDataForFarmer(farmerId))
			{
				entry.LastEmbeddedProbeMs = Environment.TickCount;
				entry.Loaded = false;
			}
			else
			{
				return;
			}
		}

		entry.Key = key;
		entry.Loaded = true;
		entry.Path = null;
		entry.Data = new PokemonSaveFile();
		entry.LoadedFromSaveFile = false;

		if (key == null)
		{
			return;
		}

		Farmer farmer = Game1.getFarmerMaybeOffline(farmerId) ?? (Game1.player?.UniqueMultiplayerID == farmerId ? Game1.player : null);
		bool isHost = IsHostFarmer(farmerId, farmer);
		string fileName = isHost ? FileNameHost : $"pokemon_save_{(farmerId != 0L ? farmerId.ToString() : "farmhand_" + (Game1.game1?.instanceIndex ?? 1))}.json";
		entry.Path = ResolvePathForFile(key, fileName);

		var file = new PokemonSaveFile();
		bool haveData = false;

		if (entry.Path != null && (isHost || farmerId != 0L))
		{
			_readFailed = false;
			PokemonSaveFile mirror = ReadFile(entry.Path);
			if (mirror != null)
			{
				file = mirror;
				haveData = true;
			}
			else if (_readFailed)
			{
				entry.Path = null;
			}
		}

		long embeddedStamp = ReadEmbeddedStampForFarmer(farmer);

		// Single Source of Truth & Anti-Exploit Rollback Protection:
		// Stardew Valley saves all player progress (inventory, items, money, world state) into the .sv save file
		// when the player sleeps at night. If the player quits without saving (e.g. after summoning and catching a Legendary),
		// the day rolls back to 6:00 AM.
		// To prevent desync exploits where the player keeps the caught Legendary while recovering the used Master Ball
		// and Summon Relic, farmer.modData MUST be authoritative whenever present.
		// The disk mirror (pokemon_save.json) is strictly a secondary fallback for initial import or offline tools.
		if (ReadFarmerModData(farmer, file))
		{
			haveData = true;
			entry.LoadedFromSaveFile = true;
		}

		if (!haveData && isHost)
		{
			PokemonSaveFile legacy = TryImportLegacyFiles(key);
			if (legacy != null)
			{
				entry.Data = legacy;
				Log("migrated legacy pokemon_*.json files into the unified save");
				SaveForFarmer(farmerId);
				entry.LoadedFromSaveFile = true;
				return;
			}
		}

		entry.Data = file;
		if (!entry.LoadedFromSaveFile)
		{
			entry.LastEmbeddedProbeMs = Environment.TickCount;
		}
		else
		{
			// Align disk mirror immediately with the authoritative morning save, scrubbing any uncommitted
			// progress from an aborted/unsaved day.
			WriteMirrorForEntry(entry);
		}
	}

	private static bool IsHostFarmer(long farmerId, Farmer farmer)
	{
		if (Game1.game1 != null && !Game1.game1.IsMainInstance && farmerId == (Game1.player?.UniqueMultiplayerID ?? 0L))
		{
			return false;
		}
		if (Game1.MasterPlayer != null && Game1.MasterPlayer.UniqueMultiplayerID != 0L)
		{
			return farmerId != 0L && farmerId == Game1.MasterPlayer.UniqueMultiplayerID;
		}
		if (Game1.serverHost?.Value != null && Game1.serverHost.Value.UniqueMultiplayerID != 0L)
		{
			return farmerId != 0L && farmerId == Game1.serverHost.Value.UniqueMultiplayerID;
		}
		if (Game1.game1 != null && !Game1.game1.IsMainInstance)
		{
			return false;
		}
		return Game1.IsMasterGame && (farmer == null || farmer == Game1.player);
	}

	/// <summary>UTC ticks stamped on the embedded copy, or 0 when absent/unreadable.</summary>
	private static long ReadEmbeddedStampForFarmer(Farmer farmer)
	{
		try
		{
			var modData = farmer?.modData;
			if (modData == null || !modData.ContainsKey(ModDataStampKey)) return 0;
			return long.TryParse(modData[ModDataStampKey], out long ticks) ? ticks : 0;
		}
		catch
		{
			return 0;
		}
	}

	/// <summary>Cheap check used to notice a farmer record that arrived after the load.</summary>
	private static bool HasEmbeddedDataForFarmer(long farmerId)
	{
		try
		{
			Farmer farmer = Game1.getFarmerMaybeOffline(farmerId) ?? (Game1.player?.UniqueMultiplayerID == farmerId ? Game1.player : null);
			var modData = farmer?.modData;
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

	private static string ResolvePathForFile(string key, string fileName)
	{
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

	private static void WriteFarmerModData(Farmer farmer, PokemonSaveFile data, PokemonSaveSection section)
	{
		try
		{
			if (farmer?.modData == null) return;

			if (section is PokemonSaveSection.All or PokemonSaveSection.Party)
			{
				var party = new PokemonPartySectionDto
				{
					Party = data.Party ?? new List<PokemonSaveDto>(),
					ActiveIndex = data.ActiveIndex
				};
				farmer.modData[ModDataPartyKey] = JsonSerializer.Serialize(party, CompactOptions);
			}

			if (section is PokemonSaveSection.All or PokemonSaveSection.Pokedex)
			{
				farmer.modData[ModDataPokedexKey] = JsonSerializer.Serialize(
					data.Pokedex ?? new PokedexSaveData(), CompactOptions);
			}

			if (section is PokemonSaveSection.All or PokemonSaveSection.Storage)
			{
				farmer.modData[ModDataStorageKey] = JsonSerializer.Serialize(
					data.Storage ?? new PokemonStorageSaveDto(), CompactOptions);
			}

			farmer.modData[ModDataStampKey] = data.SavedAtUtc.ToString();
		}
		catch (Exception ex)
		{
			Log($"could not embed the Pokémon document for farmer {farmer?.UniqueMultiplayerID}: {ex.Message}");
		}
	}

	private static bool ReadFarmerModData(Farmer farmer, PokemonSaveFile file)
	{
		try
		{
			if (farmer?.modData == null) return false;

			bool any = false;
			any |= ReadPartySection(farmer.modData, file);
			any |= ReadPokedexSection(farmer.modData, file);
			any |= ReadStorageSection(farmer.modData, file);
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

	private static void ClearPlayerModData(Farmer farmer)
	{
		try
		{
			if (farmer?.modData == null) return;
			farmer.modData.Remove(ModDataPartyKey);
			farmer.modData.Remove(ModDataPokedexKey);
			farmer.modData.Remove(ModDataStorageKey);
			farmer.modData.Remove(ModDataStampKey);
		}
		catch (Exception ex)
		{
			Log($"could not clear the embedded Pokémon document: {ex.Message}");
		}
	}

	// ---- Mirror file ------------------------------------------------------

	private static void WriteMirrorForEntry(FarmerSaveEntry entry)
	{
		if (entry?.Path == null)
		{
			if (!_missingPathLogged && entry?.Key != null)
			{
				_missingPathLogged = true;
				Log("no writable mirror location found; progress lives in the save file only this session");
			}
			return;
		}

		try
		{
			string json = JsonSerializer.Serialize(entry.Data, JsonOptions);
			string temp = entry.Path + ".tmp";
			File.WriteAllText(temp, json, new UTF8Encoding(false));
			WriteReplacing(temp, entry.Path);
		}
		catch (Exception ex)
		{
			Log($"failed to write {Path.GetFileName(entry.Path)}: {ex.Message}");
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

	private static PokemonSaveFile TryImportLegacyFiles(string key = null)
	{
		var folders = new List<string>();
		try
		{
			string savesRoot = Program.GetSavesFolder();
			if (!string.IsNullOrEmpty(savesRoot) && key != null)
			{
				folders.Add(Path.Combine(savesRoot, key));
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
