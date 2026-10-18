using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace StardewValley.Pokemon;

public class PokedexEntryInfo
{
	public int Number { get; set; }
	public string Name { get; set; }
	public string Category { get; set; }
	public string Description { get; set; }
	public string Habitat { get; set; }
	public string Seasons { get; set; } = "Todas as Estações";
	public string Weather { get; set; }
	public string TimeOfDay { get; set; }
	public string Tier { get; set; } = "Normal"; // Normal, Starter, Legendary, Mythical
	public PokemonType PrimaryType { get; set; }
	public PokemonType? SecondaryType { get; set; }
	public float HeightMeters { get; set; }
	public float WeightKg { get; set; }
}

public class PokedexSaveData
{
	public List<string> Seen { get; set; } = new();
	public List<string> Caught { get; set; } = new();
	public Dictionary<string, int> HintLevels { get; set; } = new();
	public string LastPurchaseDayKey { get; set; } = "";
	public List<string> DailyPurchasedKeys { get; set; } = new();
}

public class PokemonPokedex
{
	private static PokemonPokedex _instance;
	public static PokemonPokedex Instance => _instance ??= new PokemonPokedex();

	private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _caught = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _hintLevels = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, PokedexEntryInfo> _entries = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _dailyPurchasedKeys = new(StringComparer.OrdinalIgnoreCase);
	private string _lastPurchaseDayKey = "";

	public IReadOnlyCollection<string> SeenSpecies => _seen;
	public IReadOnlyCollection<string> CaughtSpecies => _caught;

	private static string GetSaveFilePath()
	{
		try
		{
			string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");
			Directory.CreateDirectory(appData);
			return Path.Combine(appData, "pokemon_pokedex_data.json");
		}
		catch
		{
			return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokemon_pokedex_data.json");
		}
	}

	private PokemonPokedex()
	{
		InitializeDexData();
		Load();
		SyncWithParty();
	}

	public void RecordSeen(string speciesName)
	{
		if (string.IsNullOrWhiteSpace(speciesName)) return;
		string clean = NormalizeSpeciesName(speciesName);
		if (_seen.Add(clean))
		{
			Save();
		}
	}

	public void RecordCaught(string speciesName)
	{
		if (string.IsNullOrWhiteSpace(speciesName)) return;
		string clean = NormalizeSpeciesName(speciesName);
		bool changed = _seen.Add(clean);
		if (_caught.Add(clean)) changed = true;
		if (_hintLevels.TryGetValue(clean, out int lvl) && lvl < 3)
		{
			_hintLevels[clean] = 3;
			changed = true;
		}
		else if (!_hintLevels.ContainsKey(clean))
		{
			_hintLevels[clean] = 3;
			changed = true;
		}
		if (changed) Save();
	}

	public void ResetPokedex()
	{
		_seen.Clear();
		_caught.Clear();
		_hintLevels.Clear();
		_dailyPurchasedKeys.Clear();
		Save();
	}

	private static string NormalizeSpeciesName(string speciesName)
	{
		string clean = speciesName.Trim();
		var sp = PokemonSpecies.Get(clean);
		if (sp != null && !string.IsNullOrWhiteSpace(sp.Name))
		{
			if (sp.IsMega && !string.IsNullOrWhiteSpace(sp.BaseSpeciesName))
			{
				var baseSp = PokemonSpecies.Get(sp.BaseSpeciesName);
				if (baseSp != null) return baseSp.Name;
			}
			return sp.Name;
		}
		return clean;
	}

	public bool IsSeen(string speciesName)
	{
		if (string.IsNullOrWhiteSpace(speciesName)) return false;
		return _seen.Contains(NormalizeSpeciesName(speciesName)) || _seen.Contains(speciesName.Trim());
	}

	public bool IsCaught(string speciesName)
	{
		if (string.IsNullOrWhiteSpace(speciesName)) return false;
		return _caught.Contains(NormalizeSpeciesName(speciesName)) || _caught.Contains(speciesName.Trim());
	}

	/// <summary>
	/// Returns the Dwarf hint level unlocked for a species:
	/// 0 = None, 1 = Dica Normal (Habitat + Season), 2 = Dica Avançada (+ Weather), 3 = Dica Total (+ TimeOfDay) or Caught.
	/// </summary>
	public int GetHintLevel(string speciesName)
	{
		if (string.IsNullOrWhiteSpace(speciesName)) return 0;
		if (IsCaught(speciesName)) return 3;
		string clean = NormalizeSpeciesName(speciesName);
		if (_hintLevels.TryGetValue(clean, out int level)) return level;
		if (_hintLevels.TryGetValue(speciesName.Trim(), out int levelRaw)) return levelRaw;
		return 0;
	}

	public void SetHintLevel(string speciesName, int level)
	{
		if (string.IsNullOrWhiteSpace(speciesName)) return;
		string clean = NormalizeSpeciesName(speciesName);
		_seen.Add(clean);
		int current = GetHintLevel(clean);
		_hintLevels[clean] = Math.Max(current, Math.Clamp(level, 1, 3));
		Save();
	}

	private string GetCurrentDayKey()
	{
		try
		{
			return $"{Game1.year}_{Game1.currentSeason}_{Game1.dayOfMonth}_{Game1.stats?.DaysPlayed ?? 0}";
		}
		catch
		{
			return "default_day";
		}
	}

	private void EnsureDailyReset()
	{
		string today = GetCurrentDayKey();
		if (_lastPurchaseDayKey != today)
		{
			_lastPurchaseDayKey = today;
			_dailyPurchasedKeys.Clear();
		}
	}

	public bool HasBoughtHintToday(string tier, int hintLevel)
	{
		EnsureDailyReset();
		return _dailyPurchasedKeys.Contains($"{tier}_{hintLevel}");
	}

	public void MarkHintBoughtToday(string tier, int hintLevel)
	{
		EnsureDailyReset();
		_dailyPurchasedKeys.Add($"{tier}_{hintLevel}");
		Save();
	}

	public void ResetDailyHintPurchasesForDebug()
	{
		_dailyPurchasedKeys.Clear();
		Save();
	}

	public void SyncWithParty()
	{
		if (PlayerParty.Instance?.Team == null) return;
		foreach (var p in PlayerParty.Instance.Team)
		{
			if (p?.Species != null && !p.IsEgg)
			{
				RecordCaught(p.Species.Name);
			}
		}
	}

	public void Save()
	{
		try
		{
			var data = new PokedexSaveData
			{
				Seen = new List<string>(_seen),
				Caught = new List<string>(_caught),
				HintLevels = new Dictionary<string, int>(_hintLevels),
				LastPurchaseDayKey = _lastPurchaseDayKey,
				DailyPurchasedKeys = new List<string>(_dailyPurchasedKeys)
			};
			string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(GetSaveFilePath(), json);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PokemonPokedex] Erro ao salvar Pokédex: {ex.Message}");
		}
	}

	public void Load()
	{
		try
		{
			string path = GetSaveFilePath();
			if (!File.Exists(path)) return;
			string json = File.ReadAllText(path);
			var data = JsonSerializer.Deserialize<PokedexSaveData>(json);
			if (data == null) return;

			if (data.Seen != null)
				foreach (var s in data.Seen) _seen.Add(s);
			if (data.Caught != null)
				foreach (var c in data.Caught) { _caught.Add(c); _seen.Add(c); }
			if (data.HintLevels != null)
				foreach (var kv in data.HintLevels) _hintLevels[kv.Key] = kv.Value;

			_lastPurchaseDayKey = data.LastPurchaseDayKey ?? "";
			if (data.DailyPurchasedKeys != null)
				foreach (var k in data.DailyPurchasedKeys) _dailyPurchasedKeys.Add(k);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PokemonPokedex] Erro ao carregar Pokédex: {ex.Message}");
		}
	}

	public PokedexEntryInfo GetEntry(string speciesName)
	{
		string clean = NormalizeSpeciesName(speciesName);
		if (_entries.TryGetValue(clean, out var entry))
			return entry;
		if (_entries.TryGetValue(speciesName, out var rawEntry))
			return rawEntry;

		var sp = PokemonSpecies.Get(clean);
		if (sp == null) return null;

		return new PokedexEntryInfo
		{
			Number = sp.PokedexNumber,
			Name = sp.Name,
			Category = "Espécie",
			Description = $"{sp.Name} é um Pokémon encontrado pelas terras e vales da região de Pelicanos.",
			Habitat = "Fazenda, Floresta Cinzenta & Montanha",
			Seasons = "Todas as Estações",
			Weather = "Qualquer Clima (Sol ou Chuva)",
			TimeOfDay = "Dia Inteiro (06:00h às 02:00h)",
			Tier = "Normal",
			PrimaryType = sp.PrimaryType,
			SecondaryType = sp.SecondaryType,
			HeightMeters = 1.0f,
			WeightKg = 20.0f
		};
	}

	private void InitializeDexData()
	{
		// Populate all 1,025 species with authentic height, weight, category, description, habitat, season, weather, time, and tier
		PokemonPokedexCatalog.Populate(_entries);
	}
}
