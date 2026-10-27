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
	public List<int> ClaimedMilestones { get; set; } = new();
}

public class PokemonPokedex
{
	private static readonly Dictionary<long, PokemonPokedex> _instancesByFarmer = new();
	private static readonly object _syncLock = new();
	private static readonly Dictionary<string, PokedexEntryInfo> _entries = new(StringComparer.OrdinalIgnoreCase);
	private static bool _catalogInitialized = false;

	public static PokemonPokedex Instance => GetForFarmer(Game1.player);

	public static PokemonPokedex GetForFarmer(Farmer farmer)
	{
		long id = farmer?.UniqueMultiplayerID ?? 0L;
		return GetForFarmer(id);
	}

	public static PokemonPokedex GetForFarmer(long farmerId)
	{
		lock (_syncLock)
		{
			if (!_instancesByFarmer.TryGetValue(farmerId, out var dex))
			{
				dex = new PokemonPokedex(farmerId);
				_instancesByFarmer[farmerId] = dex;
			}
			return dex;
		}
	}

	public static void ClearAll()
	{
		lock (_syncLock)
		{
			_instancesByFarmer.Clear();
		}
	}

	public static void SaveAll()
	{
		List<PokemonPokedex> list;
		lock (_syncLock)
		{
			list = new List<PokemonPokedex>(_instancesByFarmer.Values);
		}
		foreach (var d in list)
		{
			d.Save();
		}
	}

	public long FarmerId { get; }

	private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _caught = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _hintLevels = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _dailyPurchasedKeys = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<int> _claimedMilestones = new();
	public IReadOnlyCollection<int> ClaimedMilestones => _claimedMilestones;
	private string _lastPurchaseDayKey = "";

	public IReadOnlyCollection<string> SeenSpecies => _seen;
	public IReadOnlyCollection<string> CaughtSpecies => _caught;
	public int CaughtCount => _caught.Count;

	private PokemonPokedex(long farmerId)
	{
		FarmerId = farmerId;
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

	public float GetCompletionPercentage()
	{
		const int totalNational = 1025;
		return Math.Clamp((float)_caught.Count / totalNational * 100f, 0f, 100f);
	}

	public bool HasClaimedMilestone(int percent) => _claimedMilestones.Contains(percent);

	public bool CanClaimMilestone(int percent)
	{
		return GetCompletionPercentage() >= (float)percent && !HasClaimedMilestone(percent);
	}

	public bool ClaimMilestone(int percent, bool forceDebug = false)
	{
		if (!forceDebug && !CanClaimMilestone(percent)) return false;
		if (_claimedMilestones.Contains(percent) && !forceDebug) return false;

		_claimedMilestones.Add(percent);
		Save();

		var farmer = Game1.getFarmer(FarmerId) ?? Game1.player;
		DeliverMilestoneRewards(farmer, percent);
		return true;
	}

	public void ResetClaimedMilestones()
	{
		_claimedMilestones.Clear();
		Save();
	}

	public void FillAllForDebug()
	{
		var all = PokemonSpecies.AllSpecies();
		if (all != null)
		{
			foreach (var sp in all)
			{
				if (sp != null && !sp.IsMega)
				{
					RecordCaught(sp.Name);
				}
			}
		}
		Save();
	}

	private void DeliverMilestoneRewards(Farmer farmer, int percent)
	{
		if (farmer == null) farmer = Game1.player;

		void GiveItem(string id, int qty)
		{
			Item it = ItemRegistry.Create("(O)" + id, qty, 0, allowNull: true) ?? new StardewValley.Object(id, qty);
			if (it != null) farmer.addItemByMenuIfNecessary(it);
		}

		switch (percent)
		{
			case 10:
				GiveItem("Poke_RunningShoes", 1);
				GiveItem("UltraBall", 10);
				GiveItem("PokemonRareCandy", 5);
				Game1.addHUDMessage(new HUDMessage("🏆 Marco 10% da Pokédex: Tênis de Corrida (+1 Vel) e suprimentos recebidos!", 2));
				break;
			case 25:
				GiveItem("Poke_CompetitiveCase", 1);
				GiveItem("PokemonExpShare", 1);
				Game1.addHUDMessage(new HUDMessage("🏆 Marco 25% da Pokédex: Estojo de Itens de Combate e Exp. Share recebidos!", 2));
				break;
			case 50:
				GiveItem("Poke_PortableHealMachine", 1);
				GiveItem("Poke_AbilityPatch", 2);
				GiveItem("Poke_FriendshipSweet", 5);
				Game1.addHUDMessage(new HUDMessage("🏆 Marco 50% da Pokédex: Centro Pokémon Portátil, Doces da Amizade e Ability Patches recebidos!", 2));
				break;
			case 75:
				GiveItem("Poke_BoxLink", 1);
				GiveItem("MasterBall", 1);
				GiveItem("PokemonRareCandy", 15);
				Game1.addHUDMessage(new HUDMessage("🏆 Marco 75% da Pokédex: Box Link Portátil (PC) e 1x Master Ball recebidos!", 2));
				break;
			case 90:
				GiveItem("Poke_MegaStoneSelector", 1);
				GiveItem("Poke_MegaBracelet", 1);
				GiveItem("Poke_ShinyCharm", 1);
				Game1.addHUDMessage(new HUDMessage("🏆 Marco 90% da Pokédex: Estojo de Mega Pedras e Mega Bracelete recebidos!", 2));
				break;
			case 100:
				GiveItem("Poke_MasterBallBundle", 1);
				GiveItem("PokemonRareCandy", 50);
				GiveItem("Poke_AbilityPatch", 10);
				Game1.addHUDMessage(new HUDMessage("👑 Marco 100% da Pokédex: Maleta Silph Co. (5x Master Ball) e 50x Rare Candy!", 2));
				break;
		}

		Game1.playSound("reward");
	}

	public void ResetPokedex()
	{
		_seen.Clear();
		_caught.Clear();
		_hintLevels.Clear();
		_dailyPurchasedKeys.Clear();
		_claimedMilestones.Clear();
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
		var party = PlayerParty.GetForFarmer(FarmerId);
		if (party?.Team == null) return;
		foreach (var p in party.Team)
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
			PokemonSaveFile save = PokemonSaveStore.GetDataForFarmer(FarmerId);
			save.Pokedex = new PokedexSaveData
			{
				Seen = new List<string>(_seen),
				Caught = new List<string>(_caught),
				HintLevels = new Dictionary<string, int>(_hintLevels),
				LastPurchaseDayKey = _lastPurchaseDayKey,
				DailyPurchasedKeys = new List<string>(_dailyPurchasedKeys),
				ClaimedMilestones = new List<int>(_claimedMilestones)
			};
			PokemonSaveStore.SaveForFarmer(FarmerId, PokemonSaveSection.Pokedex);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PokemonPokedex] Erro ao salvar Pokédex do jogador {FarmerId}: {ex.Message}");
		}
	}

	public void Load()
	{
		try
		{
			PokemonSaveFile save = PokemonSaveStore.GetDataForFarmer(FarmerId);
			PokedexSaveData data = save?.Pokedex;
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
			if (data.ClaimedMilestones != null)
				foreach (var m in data.ClaimedMilestones) _claimedMilestones.Add(m);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PokemonPokedex] Erro ao carregar Pokédex do jogador {FarmerId}: {ex.Message}");
		}
	}

	/// <summary>Drops in-memory progress and re-reads the Pokédex from the loaded save.</summary>
	public void Reload()
	{
		_seen.Clear();
		_caught.Clear();
		_hintLevels.Clear();
		_dailyPurchasedKeys.Clear();
		_lastPurchaseDayKey = "";
		ClearAll();
		PokemonSaveStore.Reload();
		Load();
		SyncWithParty();
	}

	/// <summary>Clears every piece of progress for a brand new playthrough.</summary>
	public void ResetForNewGame()
	{
		_seen.Clear();
		_caught.Clear();
		_hintLevels.Clear();
		_dailyPurchasedKeys.Clear();
		_lastPurchaseDayKey = "";
		ClearAll();
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

	private static void InitializeDexData()
	{
		if (_catalogInitialized) return;
		_catalogInitialized = true;
		// Populate all 1,025 species with authentic height, weight, category, description, habitat, season, weather, time, and tier
		PokemonPokedexCatalog.Populate(_entries);
	}
}