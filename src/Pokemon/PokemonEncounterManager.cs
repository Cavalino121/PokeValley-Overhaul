using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Locations;

namespace StardewValley.Pokemon;

public static class PokemonEncounterManager
{
	public static int GetCanonicalLegendaryEncounterLevel(string speciesName)
	{
		string name = (speciesName ?? "").Trim().ToLowerInvariant();
		return name switch
		{
			"phione" => 1,
			"cosmog" => 5,
			"kubfu" => 10,
			"meltan" => 15,
			"poipole" or "type: null" or "type_null" or "typenull" => 40,
			"cosmoem" => 43,
			"urshifu" => 50,
			"silvally" => 50,
			"naganadel" => 50,
			"melmetal" => 50,
			"articuno" or "zapdos" or "moltres" => 50,
			"raikou" or "entei" or "suicune" => 50,
			"regirock" or "regice" or "registeel" => 50,
			"uxie" or "mesprit" or "azelf" => 50,
			"cobalion" or "terrakion" or "virizion" or "keldeo" => 50,
			"tornadus" or "thundurus" or "landorus" => 50,
			"latios" or "latias" => 60,
			"heatran" or "cresselia" or "regigigas" => 60,
			"tapu koko" or "tapu lele" or "tapu bulu" or "tapu fini" => 60,
			"nihilego" or "buzzwole" or "pheromosa" or "xurkitree" or "celesteela" or "kartana" or "guzzlord" or "stakataka" or "blacephalon" => 60,
			"regieleki" or "regidrago" or "glastrier" or "spectrier" => 60,
			"enamorus" => 60,
			"wo-chien" or "chien-pao" or "ting-lu" or "chi-yu" => 60,
			"okidogi" or "munkidori" or "fezandipiti" or "ogerpon" or "pecharunt" => 60,
			"roaring moon" or "iron valiant" or "walking wake" or "iron leaves" or "gouging fire" or "raging bolt" or "iron boulder" or "iron crown" => 65,
			_ => 70
		};
	}

	private static float _randomLegendarySpawnTimer = 0f;

	public static void CheckRandomWildLegendarySpawn(float dt)
	{
		_randomLegendarySpawnTimer += dt;
		if (_randomLegendarySpawnTimer < 120f) return;
		_randomLegendarySpawnTimer = 0f;

		if (Game1.random.NextDouble() > 0.02) return;

		var allSummons = new List<LegendarySummonDef>();
		foreach (var kv in LegendarySummonsByRelic)
		{
			allSummons.AddRange(kv.Value);
		}
		if (allSummons.Count == 0) return;

		var candidate = allSummons[Game1.random.Next(allSummons.Count)];
		var spec = PokemonSpecies.Get(candidate.SpeciesName);
		if (spec == null) return;

		var targetLoc = Game1.getLocationFromName(candidate.Location);
		if (targetLoc == null) return;

		string locKey = targetLoc.NameOrUniqueName ?? targetLoc.Name;
		if (_spawnsByLocation.TryGetValue(locKey, out var st) && st != null)
		{
			if (st.Spawns.Any(s => string.Equals(s.Instance?.Species?.Name, spec.Name, StringComparison.OrdinalIgnoreCase)))
				return;
		}

		Point targetTile = new Point(candidate.TargetX, candidate.TargetY);
		int level = GetCanonicalLegendaryEncounterLevel(spec.Name);
		var spawned = SpawnWildAt(targetLoc, targetTile, spec.InternalName, level: level, isShiny: RollWildShiny());
		if (spawned != null)
		{
			Color chatCol = PokemonCutsceneManager.DetermineElementalColor(spec);
			Game1.chatBox?.addMessage(PokemonCutsceneManager.GetCustomWildSpawnMessage(spec.Name), chatCol);
		}
	}


	public const float WildRate = 0.04f; // 4% step encounter rate

	private static readonly Dictionary<long, int> _lastEncounterTickByFarmer = new();
	private static readonly Dictionary<long, Vector2> _lastEncounterTileByFarmer = new();

	public class OverworldWildSpawn
	{
		public PokemonInstance Instance;
		public Vector2 Position;
		public Vector2 Velocity;
		public float MoveTimer;
		public bool IsMoving;
		public int FacingDir = 2;
		public float WalkAnimTimer;
		public float DespawnTimer = 300f;
		public float ContactCooldown = 0f;
		public bool MarkedForRemoval = false;
		public long CurrentBattlerFarmerId = 0L;

		public void Remove()
		{
			MarkedForRemoval = true;
		}
	}

	private class LocationSpawnState
	{
		public readonly List<OverworldWildSpawn> Spawns = new();
		public float RefreshTimer = 0.5f;
		public int LastUpdateTick = -1;
	}

	private static readonly Dictionary<string, LocationSpawnState> _spawnsByLocation = new(StringComparer.OrdinalIgnoreCase);

	public static bool CheckStepEncounter(GameLocation location, Farmer farmer)
	{
		if (farmer == null || !farmer.IsLocalPlayer || Game1.activeClickableMenu != null || !PlayerParty.GetForFarmer(farmer).HasChosenStarter)
		{
			return false;
		}

		long fid = farmer.UniqueMultiplayerID;
		Vector2 currentTile = farmer.Tile;
		_lastEncounterTileByFarmer.TryGetValue(fid, out Vector2 lastTile);
		_lastEncounterTickByFarmer.TryGetValue(fid, out int lastTick);

		if (currentTile == lastTile || (Game1.ticks - lastTick < 180))
		{
			return false;
		}

		_lastEncounterTileByFarmer[fid] = currentTile;

		if (PokemonItemUse.RepelSteps > 0)
		{
			PokemonItemUse.RepelSteps--;
			if (PokemonItemUse.RepelSteps == 0)
			{
				Game1.addHUDMessage(new HUDMessage("O efeito do Repel passou!", 3));
			}
			return false;
		}

		if (Game1.random.NextDouble() >= WildRate)
		{
			return false;
		}

		_lastEncounterTickByFarmer[fid] = Game1.ticks;
		TriggerWildBattle(location);
		return true;
	}

	
	public class LegendarySummonDef
	{
		public string SpeciesName { get; set; }
		public string Location { get; set; }
		public int TargetX { get; set; }
		public int TargetY { get; set; }
		public string RelicId { get; set; }
		public string RelicName { get; set; }
		public bool RequiresStorm { get; set; }
		public bool RequiresRain { get; set; }
		public bool RequiresSun { get; set; }
		public bool RequiresNight { get; set; }
		public bool RequiresDay { get; set; }
		public Season? RequiredSeason { get; set; }
		public bool IsFishing { get; set; }
	}

	private static readonly Dictionary<string, List<LegendarySummonDef>> LegendarySummonsByRelic = new(StringComparer.OrdinalIgnoreCase);

	private static void RegisterSummon(string relicId, string cleanRelicId, LegendarySummonDef def)
	{
		if (!LegendarySummonsByRelic.TryGetValue(relicId, out var list1))
		{
			list1 = new List<LegendarySummonDef>();
			LegendarySummonsByRelic[relicId] = list1;
		}
		list1.Add(def);

		if (!LegendarySummonsByRelic.TryGetValue(cleanRelicId, out var list2))
		{
			list2 = new List<LegendarySummonDef>();
			LegendarySummonsByRelic[cleanRelicId] = list2;
		}
		list2.Add(def);
	}

	static PokemonEncounterManager()
	{
		RegisterSummon("Poke_FreezingFeather", "FreezingFeather", new LegendarySummonDef { SpeciesName = "Articuno", Location = "UndergroundMine60", TargetX = 6, TargetY = 12, RelicId = "Poke_FreezingFeather", RelicName = "Pena Glacial", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Winter, IsFishing = false });
		RegisterSummon("Poke_ElectricFeather", "ElectricFeather", new LegendarySummonDef { SpeciesName = "Zapdos", Location = "Mountain", TargetX = 26, TargetY = 11, RelicId = "Poke_ElectricFeather", RelicName = "Pena Elétrica", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_FlameFeather", "FlameFeather", new LegendarySummonDef { SpeciesName = "Moltres", Location = "Caldera", TargetX = 24, TargetY = 28, RelicId = "Poke_FlameFeather", RelicName = "Pena de Chamas", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_BerserkGene", "BerserkGene", new LegendarySummonDef { SpeciesName = "Mewtwo", Location = "SkullCave", TargetX = 4, TargetY = 5, RelicId = "Poke_BerserkGene", RelicName = "Gene Berserk", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_OldSeaMap", "OldSeaMap", new LegendarySummonDef { SpeciesName = "Mew", Location = "IslandSouthEast", TargetX = 22, TargetY = 16, RelicId = "Poke_OldSeaMap", RelicName = "Mapa do Mar Antigo", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_RaikouBell", "RaikouBell", new LegendarySummonDef { SpeciesName = "Raikou", Location = "Woods", TargetX = 25, TargetY = 15, RelicId = "Poke_RaikouBell", RelicName = "Sino do Trovão", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_EnteiBell", "EnteiBell", new LegendarySummonDef { SpeciesName = "Entei", Location = "Mountain", TargetX = 44, TargetY = 30, RelicId = "Poke_EnteiBell", RelicName = "Sino do Magma", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_SuicuneBell", "SuicuneBell", new LegendarySummonDef { SpeciesName = "Suicune", Location = "Beach", TargetX = 25, TargetY = 34, RelicId = "Poke_SuicuneBell", RelicName = "Sino das Ondas", RequiresStorm = false, RequiresRain = true, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall, IsFishing = false });
		RegisterSummon("Poke_TidalBell", "TidalBell", new LegendarySummonDef { SpeciesName = "Lugia", Location = "Beach", TargetX = 28, TargetY = 36, RelicId = "Poke_TidalBell", RelicName = "Sino das Marés", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = true });
		RegisterSummon("Poke_TidalBell", "TidalBell", new LegendarySummonDef { SpeciesName = "Lugia", Location = "Submarine", TargetX = 13, TargetY = 5, RelicId = "Poke_TidalBell", RelicName = "Sino das Marés", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = true });
		RegisterSummon("Poke_RainbowWing", "RainbowWing", new LegendarySummonDef { SpeciesName = "Ho-Oh", Location = "Summit", TargetX = 10, TargetY = 22, RelicId = "Poke_RainbowWing", RelicName = "Asa do Arco-Íris", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_GSBall", "GSBall", new LegendarySummonDef { SpeciesName = "Celebi", Location = "Woods", TargetX = 12, TargetY = 14, RelicId = "Poke_GSBall", RelicName = "GS Ball", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_RelicRock", "RelicRock", new LegendarySummonDef { SpeciesName = "Regirock", Location = "UndergroundMine20", TargetX = 6, TargetY = 12, RelicId = "Poke_RelicRock", RelicName = "Núcleo de Pedra Ancestral", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_RelicIce", "RelicIce", new LegendarySummonDef { SpeciesName = "Regice", Location = "UndergroundMine60", TargetX = 6, TargetY = 12, RelicId = "Poke_RelicIce", RelicName = "Núcleo de Gelo Eterno", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Winter, IsFishing = false });
		RegisterSummon("Poke_RelicSteel", "RelicSteel", new LegendarySummonDef { SpeciesName = "Registeel", Location = "UndergroundMine100", TargetX = 6, TargetY = 12, RelicId = "Poke_RelicSteel", RelicName = "Núcleo de Aço Cósmico", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_EonFlute_Latias", "EonFlute_Latias", new LegendarySummonDef { SpeciesName = "Latias", Location = "IslandWest", TargetX = 76, TargetY = 61, RelicId = "Poke_EonFlute_Latias", RelicName = "Flauta Eon Escarlate", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_EonFlute_Latios", "EonFlute_Latios", new LegendarySummonDef { SpeciesName = "Latios", Location = "Beach", TargetX = 38, TargetY = 23, RelicId = "Poke_EonFlute_Latios", RelicName = "Flauta Eon Anil", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_BlueOrb", "BlueOrb", new LegendarySummonDef { SpeciesName = "Kyogre", Location = "Beach", TargetX = 12, TargetY = 38, RelicId = "Poke_BlueOrb", RelicName = "Esfera Azul Primordial", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = true });
		RegisterSummon("Poke_RedOrb", "RedOrb", new LegendarySummonDef { SpeciesName = "Groudon", Location = "Caldera", TargetX = 23, TargetY = 28, RelicId = "Poke_RedOrb", RelicName = "Esfera Vermelha Primordial", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_JadeOrb", "JadeOrb", new LegendarySummonDef { SpeciesName = "Rayquaza", Location = "Summit", TargetX = 10, TargetY = 22, RelicId = "Poke_JadeOrb", RelicName = "Esfera de Jade", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_CometShard", "CometShard", new LegendarySummonDef { SpeciesName = "Jirachi", Location = "Mountain", TargetX = 44, TargetY = 30, RelicId = "Poke_CometShard", RelicName = "Fragmento de Cometa", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_MeteoritePiece", "MeteoritePiece", new LegendarySummonDef { SpeciesName = "Deoxys", Location = "Railroad", TargetX = 28, TargetY = 43, RelicId = "Poke_MeteoritePiece", RelicName = "Pedaço de Meteorito", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_MindGem", "MindGem", new LegendarySummonDef { SpeciesName = "Uxie", Location = "Mountain", TargetX = 44, TargetY = 30, RelicId = "Poke_MindGem", RelicName = "Gema da Sabedoria", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_EmotionGem", "EmotionGem", new LegendarySummonDef { SpeciesName = "Mesprit", Location = "Woods", TargetX = 12, TargetY = 14, RelicId = "Poke_EmotionGem", RelicName = "Gema da Emoção", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_ValorGem", "ValorGem", new LegendarySummonDef { SpeciesName = "Azelf", Location = "Forest", TargetX = 34, TargetY = 60, RelicId = "Poke_ValorGem", RelicName = "Gema da Bravura", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_AdamantOrb", "AdamantOrb", new LegendarySummonDef { SpeciesName = "Dialga", Location = "Summit", TargetX = 10, TargetY = 22, RelicId = "Poke_AdamantOrb", RelicName = "Adamant Orb", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_LustrousOrb", "LustrousOrb", new LegendarySummonDef { SpeciesName = "Palkia", Location = "Summit", TargetX = 11, TargetY = 24, RelicId = "Poke_LustrousOrb", RelicName = "Lustrous Orb", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_MagmaStone", "MagmaStone", new LegendarySummonDef { SpeciesName = "Heatran", Location = "Caldera", TargetX = 25, TargetY = 28, RelicId = "Poke_MagmaStone", RelicName = "Pedra de Magma", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_ColossalKey", "ColossalKey", new LegendarySummonDef { SpeciesName = "Regigigas", Location = "UndergroundMine20", TargetX = 6, TargetY = 12, RelicId = "Poke_ColossalKey", RelicName = "Chave Colossal", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_GriseousCore", "GriseousCore", new LegendarySummonDef { SpeciesName = "Giratina", Location = "Sewer", TargetX = 16, TargetY = 17, RelicId = "Poke_GriseousCore", RelicName = "Griseous Core", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_LunarWing", "LunarWing", new LegendarySummonDef { SpeciesName = "Cresselia", Location = "Beach", TargetX = 25, TargetY = 34, RelicId = "Poke_LunarWing", RelicName = "Pena Lunar", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_SeaDriftTear", "SeaDriftTear", new LegendarySummonDef { SpeciesName = "Phione", Location = "Beach", TargetX = 12, TargetY = 38, RelicId = "Poke_SeaDriftTear", RelicName = "Lágrima das Correntes", RequiresStorm = false, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = true });
		RegisterSummon("Poke_SeaCrown", "SeaCrown", new LegendarySummonDef { SpeciesName = "Manaphy", Location = "Beach", TargetX = 13, TargetY = 38, RelicId = "Poke_SeaCrown", RelicName = "Coroa do Oceano", RequiresStorm = false, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = true });
		RegisterSummon("Poke_MemberCard", "MemberCard", new LegendarySummonDef { SpeciesName = "Darkrai", Location = "Railroad", TargetX = 33, TargetY = 54, RelicId = "Poke_MemberCard", RelicName = "Cartão de Membro Sombrio", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall, IsFishing = false });
		RegisterSummon("Poke_GracideaFlower", "GracideaFlower", new LegendarySummonDef { SpeciesName = "Shaymin", Location = "Forest", TargetX = 64, TargetY = 40, RelicId = "Poke_GracideaFlower", RelicName = "Flor Gracídea", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_AzureFlute", "AzureFlute", new LegendarySummonDef { SpeciesName = "Arceus", Location = "Summit", TargetX = 10, TargetY = 22, RelicId = "Poke_AzureFlute", RelicName = "Flauta Azure", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_LibertyPass", "LibertyPass", new LegendarySummonDef { SpeciesName = "Victini", Location = "IslandSouthEast", TargetX = 22, TargetY = 16, RelicId = "Poke_LibertyPass", RelicName = "Passe da Liberdade", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_IronWillRelic", "IronWillRelic", new LegendarySummonDef { SpeciesName = "Cobalion", Location = "UndergroundMine100", TargetX = 6, TargetY = 12, RelicId = "Poke_IronWillRelic", RelicName = "Relíquia da Lâmina de Ferro", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_StoneWillRelic", "StoneWillRelic", new LegendarySummonDef { SpeciesName = "Terrakion", Location = "Mountain", TargetX = 44, TargetY = 7, RelicId = "Poke_StoneWillRelic", RelicName = "Relíquia da Lâmina de Rocha", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_GrassWillRelic", "GrassWillRelic", new LegendarySummonDef { SpeciesName = "Virizion", Location = "Woods", TargetX = 25, TargetY = 15, RelicId = "Poke_GrassWillRelic", RelicName = "Relíquia da Lâmina Esmeralda", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_CycloneMirror", "CycloneMirror", new LegendarySummonDef { SpeciesName = "Tornadus", Location = "Mountain", TargetX = 26, TargetY = 11, RelicId = "Poke_CycloneMirror", RelicName = "Espelho dos Ciclones", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Fall, IsFishing = false });
		RegisterSummon("Poke_BoltMirror", "BoltMirror", new LegendarySummonDef { SpeciesName = "Thundurus", Location = "Railroad", TargetX = 28, TargetY = 43, RelicId = "Poke_BoltMirror", RelicName = "Espelho dos Trovões", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_LightStone", "LightStone", new LegendarySummonDef { SpeciesName = "Reshiram", Location = "Caldera", TargetX = 24, TargetY = 28, RelicId = "Poke_LightStone", RelicName = "Light Stone", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_DarkStone", "DarkStone", new LegendarySummonDef { SpeciesName = "Zekrom", Location = "Desert", TargetX = 19, TargetY = 12, RelicId = "Poke_DarkStone", RelicName = "Dark Stone", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_FertilityMirror", "FertilityMirror", new LegendarySummonDef { SpeciesName = "Landorus", Location = "Forest", TargetX = 64, TargetY = 40, RelicId = "Poke_FertilityMirror", RelicName = "Espelho da Fertilidade", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_DNA_Splicers", "DNA_Splicers", new LegendarySummonDef { SpeciesName = "Kyurem", Location = "UndergroundMine60", TargetX = 6, TargetY = 12, RelicId = "Poke_DNA_Splicers", RelicName = "Fundidores de DNA", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Winter, IsFishing = false });
		RegisterSummon("Poke_SecretSwordRelic", "SecretSwordRelic", new LegendarySummonDef { SpeciesName = "Keldeo", Location = "Forest", TargetX = 34, TargetY = 60, RelicId = "Poke_SecretSwordRelic", RelicName = "Relíquia da Espada Secreta", RequiresStorm = false, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_RelicSongSheet", "RelicSongSheet", new LegendarySummonDef { SpeciesName = "Meloetta", Location = "Town", TargetX = 51, TargetY = 59, RelicId = "Poke_RelicSongSheet", RelicName = "Partitura Ancestral", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_DriveBurn", "DriveBurn", new LegendarySummonDef { SpeciesName = "Genesect", Location = "UndergroundMine100", TargetX = 6, TargetY = 12, RelicId = "Poke_DriveBurn", RelicName = "Burn Drive Tecnológico", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_LifeTreeRoot", "LifeTreeRoot", new LegendarySummonDef { SpeciesName = "Xerneas", Location = "Woods", TargetX = 25, TargetY = 15, RelicId = "Poke_LifeTreeRoot", RelicName = "Raiz da Árvore da Vida", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_DestructionCocoon", "DestructionCocoon", new LegendarySummonDef { SpeciesName = "Yveltal", Location = "SkullCave", TargetX = 7, TargetY = 6, RelicId = "Poke_DestructionCocoon", RelicName = "Fragmento de Casulo Noturno", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall, IsFishing = false });
		RegisterSummon("Poke_ZygardeCube", "ZygardeCube", new LegendarySummonDef { SpeciesName = "Zygarde", Location = "UndergroundMine20", TargetX = 6, TargetY = 12, RelicId = "Poke_ZygardeCube", RelicName = "Cubo de Zygarde", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_PinkDiamondRelic", "PinkDiamondRelic", new LegendarySummonDef { SpeciesName = "Diancie", Location = "UndergroundMine20", TargetX = 6, TargetY = 12, RelicId = "Poke_PinkDiamondRelic", RelicName = "Diamante Cor-de-Rosa", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_PrisonBottle", "PrisonBottle", new LegendarySummonDef { SpeciesName = "Hoopa", Location = "Desert", TargetX = 34, TargetY = 28, RelicId = "Poke_PrisonBottle", RelicName = "Garrafa da Prisão", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_SteamCore", "SteamCore", new LegendarySummonDef { SpeciesName = "Volcanion", Location = "Caldera", TargetX = 23, TargetY = 28, RelicId = "Poke_SteamCore", RelicName = "Núcleo de Caldeira Térmica", RequiresStorm = false, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_SyntheticHelmet", "SyntheticHelmet", new LegendarySummonDef { SpeciesName = "Type: Null", Location = "Mine", TargetX = 18, TargetY = 8, RelicId = "Poke_SyntheticHelmet", RelicName = "Máscara de Controle de Fera", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_MemoryDrive_Normal", "MemoryDrive_Normal", new LegendarySummonDef { SpeciesName = "Silvally", Location = "Mine", TargetX = 18, TargetY = 8, RelicId = "Poke_MemoryDrive_Normal", RelicName = "Disco de Memória RKS", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_KokoMask", "KokoMask", new LegendarySummonDef { SpeciesName = "Tapu Koko", Location = "Mountain", TargetX = 26, TargetY = 11, RelicId = "Poke_KokoMask", RelicName = "Máscara Trovejante de Tapu", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_LeleMask", "LeleMask", new LegendarySummonDef { SpeciesName = "Tapu Lele", Location = "Woods", TargetX = 25, TargetY = 15, RelicId = "Poke_LeleMask", RelicName = "Máscara Floral de Tapu", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_BuluMask", "BuluMask", new LegendarySummonDef { SpeciesName = "Tapu Bulu", Location = "Forest", TargetX = 64, TargetY = 40, RelicId = "Poke_BuluMask", RelicName = "Máscara Telúrica de Tapu", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Fall, IsFishing = false });
		RegisterSummon("Poke_FiniMask", "FiniMask", new LegendarySummonDef { SpeciesName = "Tapu Fini", Location = "Beach", TargetX = 25, TargetY = 34, RelicId = "Poke_FiniMask", RelicName = "Máscara das Névoas de Tapu", RequiresStorm = false, RequiresRain = true, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_NebulaCore", "NebulaCore", new LegendarySummonDef { SpeciesName = "Cosmog", Location = "Summit", TargetX = 10, TargetY = 22, RelicId = "Poke_NebulaCore", RelicName = "Núcleo de Poeira Estelar", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_CosmicCocoon", "CosmicCocoon", new LegendarySummonDef { SpeciesName = "Cosmoem", Location = "Summit", TargetX = 11, TargetY = 24, RelicId = "Poke_CosmicCocoon", RelicName = "Crisálida das Estrelas", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_SunFlute", "SunFlute", new LegendarySummonDef { SpeciesName = "Solgaleo", Location = "Desert", TargetX = 34, TargetY = 28, RelicId = "Poke_SunFlute", RelicName = "Flauta do Sol", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_MoonFlute", "MoonFlute", new LegendarySummonDef { SpeciesName = "Lunala", Location = "Beach", TargetX = 25, TargetY = 34, RelicId = "Poke_MoonFlute", RelicName = "Flauta da Lua", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_BeastBall_Nihilego", "BeastBall_Nihilego", new LegendarySummonDef { SpeciesName = "Nihilego", Location = "UndergroundMine60", TargetX = 6, TargetY = 12, RelicId = "Poke_BeastBall_Nihilego", RelicName = "Ultra Esfera (Nihilego)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_BeastBall_Buzzwole", "BeastBall_Buzzwole", new LegendarySummonDef { SpeciesName = "Buzzwole", Location = "Forest", TargetX = 34, TargetY = 60, RelicId = "Poke_BeastBall_Buzzwole", RelicName = "Ultra Esfera (Buzzwole)", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_BeastBall_Pheromosa", "BeastBall_Pheromosa", new LegendarySummonDef { SpeciesName = "Pheromosa", Location = "Desert", TargetX = 19, TargetY = 12, RelicId = "Poke_BeastBall_Pheromosa", RelicName = "Ultra Esfera (Pheromosa)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_BeastBall_Xurkitree", "BeastBall_Xurkitree", new LegendarySummonDef { SpeciesName = "Xurkitree", Location = "Railroad", TargetX = 28, TargetY = 43, RelicId = "Poke_BeastBall_Xurkitree", RelicName = "Ultra Esfera (Xurkitree)", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_BeastBall_Celesteela", "BeastBall_Celesteela", new LegendarySummonDef { SpeciesName = "Celesteela", Location = "Summit", TargetX = 10, TargetY = 22, RelicId = "Poke_BeastBall_Celesteela", RelicName = "Ultra Esfera (Celesteela)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_BeastBall_Kartana", "BeastBall_Kartana", new LegendarySummonDef { SpeciesName = "Kartana", Location = "Woods", TargetX = 25, TargetY = 15, RelicId = "Poke_BeastBall_Kartana", RelicName = "Ultra Esfera (Kartana)", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Fall, IsFishing = false });
		RegisterSummon("Poke_BeastBall_Guzzlord", "BeastBall_Guzzlord", new LegendarySummonDef { SpeciesName = "Guzzlord", Location = "Sewer", TargetX = 17, TargetY = 27, RelicId = "Poke_BeastBall_Guzzlord", RelicName = "Ultra Esfera (Guzzlord)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_UltranecroziumZ", "UltranecroziumZ", new LegendarySummonDef { SpeciesName = "Necrozma", Location = "SkullCave", TargetX = 4, TargetY = 5, RelicId = "Poke_UltranecroziumZ", RelicName = "Ultranecrozium Z", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_SoulHeartRelic", "SoulHeartRelic", new LegendarySummonDef { SpeciesName = "Magearna", Location = "Town", TargetX = 51, TargetY = 59, RelicId = "Poke_SoulHeartRelic", RelicName = "Alma Coração Mecânica", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_MarshadiumZ", "MarshadiumZ", new LegendarySummonDef { SpeciesName = "Marshadow", Location = "BugLand", TargetX = 29, TargetY = 46, RelicId = "Poke_MarshadiumZ", RelicName = "Marshadium Z", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_BeastBall_Poipole", "BeastBall_Poipole", new LegendarySummonDef { SpeciesName = "Poipole", Location = "BugLand", TargetX = 29, TargetY = 46, RelicId = "Poke_BeastBall_Poipole", RelicName = "Ultra Esfera (Poipole)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_DragonPoisonBarb", "DragonPoisonBarb", new LegendarySummonDef { SpeciesName = "Naganadel", Location = "BugLand", TargetX = 29, TargetY = 46, RelicId = "Poke_DragonPoisonBarb", RelicName = "Ferrão Venenoso de Dragão", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_BeastBall_Stakataka", "BeastBall_Stakataka", new LegendarySummonDef { SpeciesName = "Stakataka", Location = "Mountain", TargetX = 44, TargetY = 7, RelicId = "Poke_BeastBall_Stakataka", RelicName = "Ultra Esfera (Stakataka)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_BeastBall_Blacephalon", "BeastBall_Blacephalon", new LegendarySummonDef { SpeciesName = "Blacephalon", Location = "Railroad", TargetX = 33, TargetY = 54, RelicId = "Poke_BeastBall_Blacephalon", RelicName = "Ultra Esfera (Blacephalon)", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_ThunderClawRelic", "ThunderClawRelic", new LegendarySummonDef { SpeciesName = "Zeraora", Location = "Mountain", TargetX = 26, TargetY = 11, RelicId = "Poke_ThunderClawRelic", RelicName = "Garra da Fagulha Plasma", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_MysteryBox_Steel", "MysteryBox_Steel", new LegendarySummonDef { SpeciesName = "Meltan", Location = "Mine", TargetX = 18, TargetY = 8, RelicId = "Poke_MysteryBox_Steel", RelicName = "Caixa Misteriosa de Aço Líquido", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_GigantamaxNut", "GigantamaxNut", new LegendarySummonDef { SpeciesName = "Melmetal", Location = "UndergroundMine100", TargetX = 6, TargetY = 12, RelicId = "Poke_GigantamaxNut", RelicName = "Porca Hexagonal Titânica", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_RustedSword", "RustedSword", new LegendarySummonDef { SpeciesName = "Zacian", Location = "Forest", TargetX = 34, TargetY = 60, RelicId = "Poke_RustedSword", RelicName = "Espada Enferrujada", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_RustedShield", "RustedShield", new LegendarySummonDef { SpeciesName = "Zamazenta", Location = "Mountain", TargetX = 44, TargetY = 30, RelicId = "Poke_RustedShield", RelicName = "Escudo Enferrujado", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_WishingStar", "WishingStar", new LegendarySummonDef { SpeciesName = "Eternatus", Location = "Summit", TargetX = 10, TargetY = 22, RelicId = "Poke_WishingStar", RelicName = "Estrela dos Desejos Corrompida", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_MartialScroll", "MartialScroll", new LegendarySummonDef { SpeciesName = "Kubfu", Location = "Desert", TargetX = 19, TargetY = 12, RelicId = "Poke_MartialScroll", RelicName = "Pergaminho das Artes Marciais", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_ScrollOfDarkness", "ScrollOfDarkness", new LegendarySummonDef { SpeciesName = "Urshifu", Location = "Caldera", TargetX = 25, TargetY = 28, RelicId = "Poke_ScrollOfDarkness", RelicName = "Pergaminho das Trevas", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_JungleVineRelic", "JungleVineRelic", new LegendarySummonDef { SpeciesName = "Zarude", Location = "Woods", TargetX = 25, TargetY = 15, RelicId = "Poke_JungleVineRelic", RelicName = "Cipó da Selva Primordial", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_ElekiPattern", "ElekiPattern", new LegendarySummonDef { SpeciesName = "Regieleki", Location = "Railroad", TargetX = 28, TargetY = 43, RelicId = "Poke_ElekiPattern", RelicName = "Padrão de Raios em Braille", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_DragoPattern", "DragoPattern", new LegendarySummonDef { SpeciesName = "Regidrago", Location = "Caldera", TargetX = 23, TargetY = 28, RelicId = "Poke_DragoPattern", RelicName = "Mandíbula de Dragão em Braille", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_IcerootCarrot", "IcerootCarrot", new LegendarySummonDef { SpeciesName = "Glastrier", Location = "Summit", TargetX = 11, TargetY = 24, RelicId = "Poke_IcerootCarrot", RelicName = "Cenoura Gélida da Tundra", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Winter, IsFishing = false });
		RegisterSummon("Poke_ShaderootCarrot", "ShaderootCarrot", new LegendarySummonDef { SpeciesName = "Spectrier", Location = "Woods", TargetX = 12, TargetY = 14, RelicId = "Poke_ShaderootCarrot", RelicName = "Cenoura Espectral das Sombras", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall, IsFishing = false });
		RegisterSummon("Poke_ReinsOfUnity", "ReinsOfUnity", new LegendarySummonDef { SpeciesName = "Calyrex", Location = "Forest", TargetX = 64, TargetY = 40, RelicId = "Poke_ReinsOfUnity", RelicName = "Rédeas da Unidade Sagrada", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_RevealGlass_Love", "RevealGlass_Love", new LegendarySummonDef { SpeciesName = "Enamorus", Location = "Woods", TargetX = 25, TargetY = 15, RelicId = "Poke_RevealGlass_Love", RelicName = "Espelho do Amor da Primavera", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_RuinousLeaves", "RuinousLeaves", new LegendarySummonDef { SpeciesName = "Wo-Chien", Location = "Forest", TargetX = 34, TargetY = 60, RelicId = "Poke_RuinousLeaves", RelicName = "Tábua de Madeira da Ruína", RequiresStorm = false, RequiresRain = true, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall, IsFishing = false });
		RegisterSummon("Poke_RuinousSword", "RuinousSword", new LegendarySummonDef { SpeciesName = "Chien-Pao", Location = "Summit", TargetX = 10, TargetY = 22, RelicId = "Poke_RuinousSword", RelicName = "Espada Fraturada de Gelo", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Winter, IsFishing = false });
		RegisterSummon("Poke_RuinousVessel", "RuinousVessel", new LegendarySummonDef { SpeciesName = "Ting-Lu", Location = "Mountain", TargetX = 44, TargetY = 7, RelicId = "Poke_RuinousVessel", RelicName = "Vaso de Pedra da Ruína", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_RuinousBeads", "RuinousBeads", new LegendarySummonDef { SpeciesName = "Chi-Yu", Location = "Caldera", TargetX = 24, TargetY = 28, RelicId = "Poke_RuinousBeads", RelicName = "Contas de Fogo da Ruína", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_Booster_RoaringMoon", "Booster_RoaringMoon", new LegendarySummonDef { SpeciesName = "Roaring Moon", Location = "SkullCave", TargetX = 7, TargetY = 6, RelicId = "Poke_Booster_RoaringMoon", RelicName = "Cápsula Ancestral (Roaring Moon)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_Booster_IronValiant", "Booster_IronValiant", new LegendarySummonDef { SpeciesName = "Iron Valiant", Location = "UndergroundMine100", TargetX = 6, TargetY = 12, RelicId = "Poke_Booster_IronValiant", RelicName = "Cápsula Futurista (Iron Valiant)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_KoraidonFeather", "KoraidonFeather", new LegendarySummonDef { SpeciesName = "Koraidon", Location = "Caldera", TargetX = 23, TargetY = 28, RelicId = "Poke_KoraidonFeather", RelicName = "Pena do Passado Selvagem", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_MiraidonCore", "MiraidonCore", new LegendarySummonDef { SpeciesName = "Miraidon", Location = "Railroad", TargetX = 33, TargetY = 54, RelicId = "Poke_MiraidonCore", RelicName = "Núcleo de Propulsão Futura", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_Booster_WalkingWake", "Booster_WalkingWake", new LegendarySummonDef { SpeciesName = "Walking Wake", Location = "Beach", TargetX = 28, TargetY = 14, RelicId = "Poke_Booster_WalkingWake", RelicName = "Cápsula Ancestral (Walking Wake)", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_Booster_IronLeaves", "Booster_IronLeaves", new LegendarySummonDef { SpeciesName = "Iron Leaves", Location = "Woods", TargetX = 25, TargetY = 15, RelicId = "Poke_Booster_IronLeaves", RelicName = "Cápsula Futurista (Iron Leaves)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_ToxicChain_Oki", "ToxicChain_Oki", new LegendarySummonDef { SpeciesName = "Okidogi", Location = "Mountain", TargetX = 16, TargetY = 9, RelicId = "Poke_ToxicChain_Oki", RelicName = "Corrente Tóxica de Força", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_ToxicChain_Munk", "ToxicChain_Munk", new LegendarySummonDef { SpeciesName = "Munkidori", Location = "Woods", TargetX = 12, TargetY = 14, RelicId = "Poke_ToxicChain_Munk", RelicName = "Corrente Tóxica da Astúcia", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_ToxicChain_Fez", "ToxicChain_Fez", new LegendarySummonDef { SpeciesName = "Fezandipiti", Location = "Forest", TargetX = 34, TargetY = 60, RelicId = "Poke_ToxicChain_Fez", RelicName = "Corrente Tóxica da Beleza", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_TealMask", "TealMask", new LegendarySummonDef { SpeciesName = "Ogerpon", Location = "Woods", TargetX = 25, TargetY = 15, RelicId = "Poke_TealMask", RelicName = "Máscara Turquesa de Hera", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring, IsFishing = false });
		RegisterSummon("Poke_Booster_GougingFire", "Booster_GougingFire", new LegendarySummonDef { SpeciesName = "Gouging Fire", Location = "Caldera", TargetX = 25, TargetY = 28, RelicId = "Poke_Booster_GougingFire", RelicName = "Cápsula Ancestral (Gouging Fire)", RequiresStorm = false, RequiresRain = false, RequiresSun = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer, IsFishing = false });
		RegisterSummon("Poke_Booster_RagingBolt", "Booster_RagingBolt", new LegendarySummonDef { SpeciesName = "Raging Bolt", Location = "Mountain", TargetX = 26, TargetY = 11, RelicId = "Poke_Booster_RagingBolt", RelicName = "Cápsula Ancestral (Raging Bolt)", RequiresStorm = true, RequiresRain = true, RequiresSun = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_Booster_IronBoulder", "Booster_IronBoulder", new LegendarySummonDef { SpeciesName = "Iron Boulder", Location = "Mountain", TargetX = 44, TargetY = 7, RelicId = "Poke_Booster_IronBoulder", RelicName = "Cápsula Futurista (Iron Boulder)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_Booster_IronCrown", "Booster_IronCrown", new LegendarySummonDef { SpeciesName = "Iron Crown", Location = "UndergroundMine100", TargetX = 6, TargetY = 12, RelicId = "Poke_Booster_IronCrown", RelicName = "Cápsula Futurista (Iron Crown)", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_IndigoDisk", "IndigoDisk", new LegendarySummonDef { SpeciesName = "Terapagos", Location = "Caldera", TargetX = 24, TargetY = 28, RelicId = "Poke_IndigoDisk", RelicName = "Disco Índigo Cristalino", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null, IsFishing = false });
		RegisterSummon("Poke_MythicalPechaMochi", "MythicalPechaMochi", new LegendarySummonDef { SpeciesName = "Pecharunt", Location = "Town", TargetX = 51, TargetY = 59, RelicId = "Poke_MythicalPechaMochi", RelicName = "Mochi Venenoso de Pêssego", RequiresStorm = false, RequiresRain = false, RequiresSun = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall, IsFishing = false });
	}

	public static bool IsSummonRelic(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId)) return false;
		if (itemId.StartsWith("(O)")) itemId = itemId.Substring(3);
		return LegendarySummonsByRelic.ContainsKey(itemId);
	}

	public static bool IsFishingSummonRelic(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId)) return false;
		if (itemId.StartsWith("(O)")) itemId = itemId.Substring(3);
		if (!LegendarySummonsByRelic.TryGetValue(itemId, out var list)) return false;
		return list.Any(d => d.IsFishing);
	}

	public static bool MatchesHabitatLocation(GameLocation loc, string targetLoc)
	{
		if (loc == null || string.IsNullOrEmpty(targetLoc)) return false;
		string locName = loc.NameOrUniqueName ?? "";
		string baseName = loc.Name ?? "";

		if (locName.Equals(targetLoc, StringComparison.OrdinalIgnoreCase) ||
		    baseName.Equals(targetLoc, StringComparison.OrdinalIgnoreCase))
			return true;

		if (targetLoc.Equals("Mine", StringComparison.OrdinalIgnoreCase) ||
		    targetLoc.StartsWith("UndergroundMine", StringComparison.OrdinalIgnoreCase))
		{
			if (baseName.Equals("Mine", StringComparison.OrdinalIgnoreCase)) return true;
			if (loc is MineShaft ms && ms.mineLevel < 121) return true;
		}

		if (targetLoc.Equals("SkullCave", StringComparison.OrdinalIgnoreCase))
		{
			if (baseName.Equals("SkullCave", StringComparison.OrdinalIgnoreCase)) return true;
			if (loc is MineShaft ms && ms.mineLevel >= 121) return true;
		}

		if (targetLoc.Equals("Caldera", StringComparison.OrdinalIgnoreCase))
		{
			if (baseName.Equals("Caldera", StringComparison.OrdinalIgnoreCase) || loc is VolcanoDungeon) return true;
		}

		if (targetLoc.Equals("IslandSouthEast", StringComparison.OrdinalIgnoreCase) || targetLoc.Equals("Island_SE", StringComparison.OrdinalIgnoreCase))
		{
			if (baseName.Equals("IslandSouthEast", StringComparison.OrdinalIgnoreCase) || baseName.Equals("Island_SE", StringComparison.OrdinalIgnoreCase))
				return true;
		}

		if (targetLoc.Equals("IslandWest", StringComparison.OrdinalIgnoreCase) || targetLoc.Equals("Island_W", StringComparison.OrdinalIgnoreCase))
		{
			if (baseName.Equals("IslandWest", StringComparison.OrdinalIgnoreCase) || baseName.Equals("Island_W", StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	private static string GetSeasonPtName(Season s) => s switch
	{
		Season.Spring => "Primavera",
		Season.Summer => "Verão",
		Season.Fall => "Outono",
		Season.Winter => "Inverno",
		_ => s.ToString()
	};

	/// <summary>
	/// Called from Object.placementAction when the player places a Legendary Summon Relic on the ground.
	/// Returns true if the Legendary was summoned (which consumes 1 stack of the item without placing the object on the map),
	/// or false if environmental conditions were not met (preserving the item in the player's hand).
	/// </summary>
	public static bool TryPlaceSummonRelic(StardewValley.Object relicObj, GameLocation loc, int tileX, int tileY, Farmer who)
	{
		if (relicObj == null || loc == null) return false;
		string itemId = relicObj.ItemId ?? "";
		if (itemId.StartsWith("(O)")) itemId = itemId.Substring(3);

		if (!LegendarySummonsByRelic.TryGetValue(itemId, out var summons) || summons.Count == 0)
			return false;

		if (PokemonItemUse.CanUseFormOrEvoItemOnParty(itemId))
		{
			PokemonItemUse.UseFormOrEvoItem(itemId);
			return false;
		}

		var firstDef = summons[0];
		if (firstDef.IsFishing)
		{
			Game1.playSound("cancel");
			Game1.addHUDMessage(new HUDMessage($"🎣 {firstDef.RelicName} deve ser usado PESCANDO na água em {firstDef.Location} com o item na mochila!", 3));
			return false;
		}

		List<string> missingReasons = new();
		LegendarySummonDef matchedDef = null;

		bool isRaining = Game1.isRaining || loc.IsRainingHere();
		bool isStorm = Game1.isLightning || loc.IsLightningHere();
		bool isNight = Game1.timeOfDay >= 1800;
		bool isDay = Game1.timeOfDay >= 600 && Game1.timeOfDay < 1800;
		Season curSeason = Game1.season;

		foreach (var def in summons)
		{
			missingReasons.Clear();

			if (!MatchesHabitatLocation(loc, def.Location))
				missingReasons.Add($"Local: {def.Location}");

			if (def.RequiresStorm && !isStorm)
				missingReasons.Add("Clima: Tempestade");
			else if (def.RequiresRain && !isRaining && !isStorm)
				missingReasons.Add("Clima: Chuva");
			else if (def.RequiresSun && (isRaining || isStorm))
				missingReasons.Add("Clima: Ensolarado");

			if (def.RequiresNight && !isNight)
				missingReasons.Add("Horário: Noite (18:00+)");
			else if (def.RequiresDay && !isDay)
				missingReasons.Add("Horário: Dia (06:00-18:00)");

			if (def.RequiredSeason.HasValue && curSeason != def.RequiredSeason.Value)
				missingReasons.Add($"Estação: {GetSeasonPtName(def.RequiredSeason.Value)}");

			if (missingReasons.Count == 0)
			{
				matchedDef = def;
				break;
			}
		}

		if (matchedDef == null)
		{
			Game1.playSound("cancel");
			Game1.addHUDMessage(new HUDMessage($"⚠️ {firstDef.RelicName} não reagiu! Para invocar {firstDef.SpeciesName}, falta: {string.Join(", ", missingReasons)}.", 3));
			return false;
		}

		// Ensure no object remains on the placement tile
		loc.objects.Remove(new Vector2(tileX, tileY));

		var spec = PokemonSpecies.Get(matchedDef.SpeciesName);
		if (spec != null)
		{
			// Play full cinematic cutscene: letterboxing, screen darkening, elemental storm flash, rumbling, roar & cry
			PokemonCutsceneManager.PlayLegendaryAwakening(loc, new Point(tileX, tileY), spec, matchedDef.RelicName, () =>
			{
				int spawnLv = GetCanonicalLegendaryEncounterLevel(spec.Name);
				var spawn = SpawnWildAt(loc, new Point(tileX, tileY), spec.InternalName, level: spawnLv, isShiny: RollWildShiny());
				Color chatColor = PokemonCutsceneManager.DetermineElementalColor(spec);
				Game1.chatBox?.addMessage(PokemonCutsceneManager.GetCustomSummonMessage(who.Name, spec.Name), chatColor);
				if (spawn != null)
				{
					spawn.ContactCooldown = 1.2f;
				}
				Game1.addHUDMessage(new HUDMessage($"✨ {matchedDef.RelicName} desapareceu em luz! O lendário {spec.Name} emergiu!", 2));
			});
		}

		return true;
	}

	public static int ClearAllActiveSpawns(GameLocation onlyLocation = null)
	{
		int removed = 0;
		if (onlyLocation != null)
		{
			string k = onlyLocation.NameOrUniqueName ?? "";
			if (_spawnsByLocation.TryGetValue(k, out var st) && st != null)
			{
				removed += st.Spawns.Count;
				st.Spawns.Clear();
			}
			return removed;
		}

		foreach (var kv in _spawnsByLocation)
		{
			if (kv.Value?.Spawns != null)
			{
				removed += kv.Value.Spawns.Count;
				kv.Value.Spawns.Clear();
			}
		}
		_spawnsByLocation.Clear();
		return removed;
	}

	public static bool IsValidWalkableSpawnTile(GameLocation loc, int tx, int ty, Point? reachableFrom = null)
	{
		if (loc == null) return false;
		Vector2 tileVec = new Vector2(tx, ty);
		if (!loc.isTileOnMap(tileVec)) return false;
		if (!loc.hasTileAt(tx, ty, "Back")) return false;
		if (loc.hasTileAt(tx, ty, "Buildings")) return false;
		if (loc.isWaterTile(tx, ty) || loc.isOpenWater(tx, ty)) return false;
		if (loc.doesTileHaveProperty(tx, ty, "Water", "Back") != null || loc.doesTileHaveProperty(tx, ty, "WaterSource", "Back") != null) return false;
		if (!loc.isTilePassable(new xTile.Dimensions.Location(tx, ty), Game1.viewport)) return false;
		if (loc.Objects.ContainsKey(tileVec)) return false;

		if (loc is MineShaft ms)
		{
			if (!ms.isTileOnClearAndSolidGround(tx, ty)) return false;
			if (ms.getTileIndexAt(tx, ty, "Back") == 77) return false;
		}

		if (loc is MineShaft && loc.hasTileAt(tx, ty, "Front"))
			return false;

		Vector2 worldPos = new Vector2(tx * 64f + 32f, ty * 64f + 32f);
		Rectangle box = new Rectangle((int)worldPos.X - 14, (int)worldPos.Y - 14, 28, 28);
		if (loc.isCollidingPosition(box, Game1.viewport, isFarmer: false, damagesFarmer: 0, glider: false, character: null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true, skipCollisionEffects: true))
			return false;

		if (reachableFrom.HasValue)
		{
			Point start = reachableFrom.Value;
			if (start.X == tx && start.Y == ty) return true;
			Queue<Point> q = new Queue<Point>();
			HashSet<Point> visited = new HashSet<Point>();
			q.Enqueue(start);
			visited.Add(start);
			int steps = 0;
			bool found = false;
			int[] dxs = { 1, -1, 0, 0 };
			int[] dys = { 0, 0, 1, -1 };

			while (q.Count > 0 && steps < 350)
			{
				Point cur = q.Dequeue();
				steps++;
				if (cur.X == tx && cur.Y == ty)
				{
					found = true;
					break;
				}
				for (int d = 0; d < 4; d++)
				{
					Point nxt = new Point(cur.X + dxs[d], cur.Y + dys[d]);
					if (visited.Contains(nxt)) continue;
					if (!loc.isTileOnMap(new Vector2(nxt.X, nxt.Y))) continue;
					if (!loc.hasTileAt(nxt.X, nxt.Y, "Back") || loc.hasTileAt(nxt.X, nxt.Y, "Buildings")) continue;
					if (loc.isWaterTile(nxt.X, nxt.Y) || !loc.isTilePassable(new xTile.Dimensions.Location(nxt.X, nxt.Y), Game1.viewport)) continue;
					if (loc is MineShaft ms2 && (!ms2.isTileOnClearAndSolidGround(nxt.X, nxt.Y) || ms2.getTileIndexAt(nxt.X, nxt.Y, "Back") == 77)) continue;
					visited.Add(nxt);
					q.Enqueue(nxt);
				}
			}
			if (!found) return false;
		}

		return true;
	}

	public static void UpdateOverworldSpawns(GameTime gameTime)
	{
		var loc = Game1.currentLocation;
		var player = Game1.player;
		if (loc == null || player == null || !PlayerParty.GetForFarmer(player).HasChosenStarter)
			return;

		float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
		PokemonMusicManager.UpdateOverworldMusic(dt);
		CheckRandomWildLegendarySpawn(dt);
		string locKey = loc.NameOrUniqueName;

		bool hasExistingSpawns = _spawnsByLocation.TryGetValue(locKey, out var existingState) && existingState.Spawns.Count > 0;
		bool validMap = hasExistingSpawns || loc.IsOutdoors || loc is MineShaft || loc is Sewer || loc is VolcanoDungeon || loc is Caldera || loc is Summit || loc is BugLand || loc is IslandLocation ||
		                locKey.Contains("Cave", StringComparison.OrdinalIgnoreCase) ||
		                locKey.Contains("Mine", StringComparison.OrdinalIgnoreCase) ||
		                locKey.Contains("Summit", StringComparison.OrdinalIgnoreCase) ||
		                locKey.Contains("Caldera", StringComparison.OrdinalIgnoreCase) ||
		                locKey.Contains("BugLand", StringComparison.OrdinalIgnoreCase) ||
		                locKey.Contains("Submarine", StringComparison.OrdinalIgnoreCase) ||
		                locKey.Contains("BathHouse", StringComparison.OrdinalIgnoreCase) ||
		                locKey.Contains("Archaeology", StringComparison.OrdinalIgnoreCase) ||
		                locKey.Contains("Witch", StringComparison.OrdinalIgnoreCase);
		if (!validMap)
		{
			_spawnsByLocation.Remove(locKey);
			return;
		}

		if (!_spawnsByLocation.TryGetValue(locKey, out var state))
		{
			state = new LocationSpawnState();
			_spawnsByLocation[locKey] = state;
		}


		long fid = player.UniqueMultiplayerID;
		_lastEncounterTickByFarmer.TryGetValue(fid, out int lastTick);

		// Avoid double-stepping AI movement if two split-screen players are in the same location on the same tick
		bool stepPhysics = (state.LastUpdateTick != Game1.ticks);
		state.LastUpdateTick = Game1.ticks;

		if (stepPhysics)
		{
			state.RefreshTimer -= dt;
			if (state.RefreshTimer <= 0f)
			{
				state.RefreshTimer = 8.0f;
				if (state.Spawns.Count < 3)
				{
					TrySpawnOverworldPokemon(loc, player, state.Spawns);
				}
			}
		}

		for (int i = state.Spawns.Count - 1; i >= 0; i--)
		{
			var s = state.Spawns[i];
			if (stepPhysics)
			{
				s.WalkAnimTimer += dt * 6f;
				s.MoveTimer -= dt;

				if (s.MoveTimer <= 0f)
				{
					if (Game1.random.NextDouble() < 0.55)
					{
						s.IsMoving = true;
						int dir = Game1.random.Next(4);
						s.FacingDir = dir;
						s.Velocity = dir switch
						{
							0 => new Vector2(0, -58f),
							1 => new Vector2(58f, 0),
							2 => new Vector2(0, 58f),
							_ => new Vector2(-58f, 0)
						};
						s.MoveTimer = 0.8f + (float)Game1.random.NextDouble() * 1.2f;
					}
					else
					{
						s.IsMoving = false;
						s.Velocity = Vector2.Zero;
						s.MoveTimer = 1.0f + (float)Game1.random.NextDouble() * 1.5f;
					}
				}

				if (s.IsMoving)
				{
					Vector2 nextPos = s.Position + s.Velocity * dt;
					Point tilePt = new Point((int)(nextPos.X / 64f), (int)(nextPos.Y / 64f));
					if (IsValidWalkableSpawnTile(loc, tilePt.X, tilePt.Y))
					{
						s.Position = nextPos;
					}
					else
					{
						s.IsMoving = false;
						s.Velocity = Vector2.Zero;
					}
				}
			}

			if (s.ContactCooldown > 0f)
			{
				s.ContactCooldown = Math.Max(0f, s.ContactCooldown - dt);
			}

			// Despawn check when no farmers are within 15 tiles (960px)
			bool anyFarmerNearby = false;
			foreach (var f in loc.farmers)
			{
				if (Vector2.Distance(f.Position, s.Position) <= 960f)
				{
					anyFarmerNearby = true;
					break;
				}
			}
			if (!anyFarmerNearby)
			{
				s.DespawnTimer -= dt;
				if (s.DespawnTimer <= 0f)
				{
					s.MarkedForRemoval = true;
				}
			}

			if (s.MarkedForRemoval)
			{
				state.Spawns.RemoveAt(i);
				continue;
			}

			// Trigger wild encounter on player contact
			if (s.CurrentBattlerFarmerId == 0L && Game1.activeClickableMenu == null && s.ContactCooldown <= 0f && (Game1.ticks - lastTick >= 150) && PokemonItemUse.RepelSteps <= 0)
			{
				Rectangle pBox = player.GetBoundingBox();
				float dist = Vector2.Distance(new Vector2(pBox.Center.X, pBox.Center.Y), s.Position);
				if (dist <= 48f)
				{
					var wildInst = s.Instance;
					s.ContactCooldown = 4.0f; // Keep spawn in world, with cooldown if fleeing
					s.CurrentBattlerFarmerId = fid;
					_lastEncounterTickByFarmer[fid] = Game1.ticks;
					Game1.playSound("dwoop");
					Game1.activeClickableMenu = new PokemonBattleMenu(wildInst, isWild: true, sourceSpawn: s);
					return;
				}
			}
		}
	}

	/// <summary>Wild encounter Shiny percentage (default 2%, configurable in Debug Menu).</summary>
	public static int WildShinyChancePercent { get; set; } = 2;

	public static double GetLuckMultiplier()
	{
		double dailyLuck = Game1.player != null ? Game1.player.DailyLuck : 0.0;
		int luckLevel = Game1.player != null ? Game1.player.LuckLevel : 0;
		// Stardew Valley dailyLuck: -0.1 (bad spirits) to +0.1 (very happy spirits)
		// Total net luck factoring LuckLevel buffs:
		double netLuck = dailyLuck + (luckLevel * 0.05);
		// Exponential scaling based on spirits:
		return Math.Max(0.2, Math.Pow(2.0, netLuck * 8.0));
	}

	public static bool RollWildShiny()
	{
		if (Game1.random == null) return false;
		double luckMult = GetLuckMultiplier();
		double effectivePercent = WildShinyChancePercent * luckMult;
		if (effectivePercent <= 0) return Game1.random.Next(512) == 0;
		return (Game1.random.NextDouble() * 100.0) <= effectivePercent;
	}

	private static void TrySpawnOverworldPokemon(GameLocation loc, Farmer player, List<OverworldWildSpawn> targetList)
	{
		Point pTile = player.TilePoint;
		bool isCompactMap = !loc.IsOutdoors || loc is MineShaft || loc is VolcanoDungeon || loc is Sewer ||
		                    (loc.NameOrUniqueName != null && (loc.NameOrUniqueName.Contains("Cave") || loc.NameOrUniqueName.Contains("Mine") || loc.NameOrUniqueName.Contains("Caldera")));
		int maxRangeX = isCompactMap ? 6 : 10;
		int maxRangeY = isCompactMap ? 5 : 8;
		int minDist = isCompactMap ? 2 : 4;

		for (int attempt = 0; attempt < 24; attempt++)
		{
			int dx = Game1.random.Next(-maxRangeX, maxRangeX + 1);
			int dy = Game1.random.Next(-maxRangeY, maxRangeY + 1);
			if (Math.Abs(dx) < minDist && Math.Abs(dy) < minDist) continue;

			int tx = pTile.X + dx;
			int ty = pTile.Y + dy;
			if (!IsValidWalkableSpawnTile(loc, tx, ty, reachableFrom: pTile))
				continue;

			Vector2 worldPos = new Vector2(tx * 64f + 32f, ty * 64f + 32f);

			bool isNight = Game1.timeOfDay >= 1800;
			bool isRain = Game1.isRaining;
			string speciesName = GetWildSpeciesForLocation(loc, isNight, isRain);
			var species = PokemonSpecies.Get(speciesName);
			if (species == null) continue;

			int level = GetScaledWildLevel();
			var inst = new PokemonInstance(species, level, isShiny: RollWildShiny());
			targetList.Add(new OverworldWildSpawn
			{
				Instance = inst,
				Position = worldPos,
				MoveTimer = 0.5f,
				FacingDir = Game1.random.Next(4)
			});
			break;
		}
	}

	public static void DrawOverworldSpawns(SpriteBatch b)
	{
		if (Game1.currentLocation == null) return;
		if (!_spawnsByLocation.TryGetValue(Game1.currentLocation.NameOrUniqueName, out var state) || state.Spawns.Count == 0)
			return;

		Color drawTint = Color.White;
		if (Game1.isRaining || Game1.currentLocation.IsRainingHere())
		{
			drawTint = new Color(236, 242, 255);
		}
		else if (Game1.currentLocation.IsOutdoors && Game1.timeOfDay >= 1900)
		{
			drawTint = new Color(235, 238, 250);
		}

		foreach (var s in state.Spawns)
		{
			if (s.Instance == null) continue;
			Vector2 screenPos = s.Position - new Vector2(Game1.viewport.X, Game1.viewport.Y);
			if (screenPos.X < -100 || screenPos.X > Game1.viewport.Width + 100 || screenPos.Y < -100 || screenPos.Y > Game1.viewport.Height + 100)
				continue;

			float depth = Math.Clamp((s.Position.Y + 12f) / 10000f, 0.001f, 0.999f);
			PokemonUI.SoftShadow(b, (int)screenPos.X, (int)screenPos.Y + 2, 32, 10, 0.32f, Math.Max(0.0001f, depth - 0.0002f));

			Texture2D icon = s.Instance.GetIconSprite(Game1.graphics.GraphicsDevice) ?? s.Instance.GetFrontSprite(Game1.graphics.GraphicsDevice);
			if (icon != null)
			{
				Rectangle srcRect = (icon == s.Instance.GetIconSprite(Game1.graphics.GraphicsDevice))
					? s.Instance.GetIconSourceRect()
					: s.Instance.GetFrontSourceRect();
				float bounceY = s.IsMoving ? -(float)Math.Abs(Math.Sin(s.WalkAnimTimer * Math.PI * 0.5)) * 4f : 0f;
				// Standard icons face left by default: when moving right (FacingDir == 1), flip horizontally to look right!
				SpriteEffects flip = (s.FacingDir == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				float scale = PokemonInstance.GetOverworldScale(s.Instance, isIcon: (srcRect.Width <= 40));
				Vector2 origin = new Vector2(srcRect.Width / 2f, srcRect.Height - 4f);

				b.Draw(icon, screenPos + new Vector2(0, bounceY), srcRect, drawTint, 0f, origin, scale, flip, depth);

				if (s.Instance.IsShiny)
				{
					PokemonUI.Text(b, Game1.tinyFont, "★", new Rectangle((int)screenPos.X - 10, (int)screenPos.Y - 46, 20, 18), Color.Gold, center: true, maxScale: 0.85f);
				}
			}
		}
	}

	private static int GetScaledWildLevel()
	{
		int partyMax = 5;
		var party = PlayerParty.GetForFarmer(Game1.player);
		if (party?.Team != null && party.Team.Count > 0)
		{
			foreach (var p in party.Team)
			{
				if (p != null && p.Level > partyMax) partyMax = p.Level;
			}
		}
		int minLv = Math.Clamp(partyMax - 5, 3, 60);
		int maxLv = Math.Clamp(partyMax + 2, 7, 70);
		return Game1.random.Next(minLv, maxLv + 1);
	}

	public static void TriggerWildBattle(GameLocation location)
	{
		bool isNight = Game1.timeOfDay >= 1800;
		bool isRain = Game1.isRaining;
		string speciesName = GetWildSpeciesForLocation(location, isNight, isRain);

		int level = GetScaledWildLevel();
		var species = PokemonSpecies.Get(speciesName);
		if (species != null)
		{
			var wildInstance = new PokemonInstance(species, level, isShiny: RollWildShiny());
			Game1.playSound("dwoop");
			Game1.activeClickableMenu = new PokemonBattleMenu(wildInstance, isWild: true);
		}
	}

	public static OverworldWildSpawn SpawnWildAt(GameLocation loc, Point tile, string speciesName, int level = -1, bool? isShiny = null)
	{
		if (loc == null) loc = Game1.currentLocation;
		if (loc == null) return null;

		var species = PokemonSpecies.Get(speciesName);
		if (species == null) return null;

		string locKey = loc.NameOrUniqueName ?? loc.Name;
		if (!_spawnsByLocation.TryGetValue(locKey, out var state))
		{
			state = new LocationSpawnState();
			_spawnsByLocation[locKey] = state;
		}


		if (level <= 0) level = GetScaledWildLevel();
		bool shiny = isShiny ?? RollWildShiny();

		var inst = new PokemonInstance(species, level, isShiny: shiny);
		Vector2 worldPos = new Vector2(tile.X * 64f + 32f, tile.Y * 64f + 32f);

		var spawn = new OverworldWildSpawn
		{
			Instance = inst,
			Position = worldPos,
			MoveTimer = 0.5f,
			FacingDir = Game1.random.Next(4),
			DespawnTimer = 600f
		};
		state.Spawns.Add(spawn);
		return spawn;
	}

	public static string GetWildSpeciesForLocation(GameLocation location, bool isNight, bool isRain)
	{
		string locName = location?.Name ?? "";

		// Note: Legendary Pokémon are summoned actively via held relic in hand at their canonical habitats.

		// Exponential daily luck roll for rare / pseudo-legendary encounters
		if (Game1.random.NextDouble() < (0.035 * GetLuckMultiplier()))
		{
			return Choose(new[] {
				"dratini", "larvitar", "bagon", "beldum", "gible", "riolu", "deino", "goomy",
				"jangmo_o", "dreepy", "frigibax", "eevee", "rotom", "spiritomb", "charcadet", "tinkatink", "zorua"
			});
		}

		// 1. Mines, Caves, Skull Cavern, Volcano (Gen 1-9 Cavern & Volcanic Pool)
		if (location is MineShaft || locName.Contains("Cave") || locName.Contains("Mine") || locName.Contains("Volcano") || locName.Contains("Caldera"))
		{
			if (locName.Contains("Volcano") || locName.Contains("Caldera"))
			{
				return Choose(new[] {
					"torkoal", "magcargo", "slugma", "charmander", "charmeleon", "magmar", "numel", "camerupt", "ponyta", "houndour",
					"larvesta", "volcarona", "salandit", "turtonator", "rolycoly", "carkol", "coalossal", "charcadet", "ceruledge", "armarouge", "fuecoco"
				});
			}
			if (location is MineShaft ms)
			{
				if (ms.mineLevel >= 80)
				{
					return Choose(new[] {
						"magmar", "torkoal", "magcargo", "lairon", "aggron", "golem", "rhydon", "sableye", "mawile", "banette", "dusclops", "beldum", "metang", "larvitar", "bagon",
						"gible", "gabite", "riolu", "lucario", "excadrill", "axew", "fraxure", "deino", "zweilous", "pawniard", "bisharp", "honedge", "doublade", "jangmo_o", "dreepy", "drakloak", "tinkatuff", "glimmora"
					});
				}
				if (ms.mineLevel >= 40)
				{
					return Choose(new[] {
						"snorunt", "glalie", "spheal", "sealeo", "swinub", "piloswine", "sneasel", "jynx", "golbat", "graveler", "magneton", "haunter",
						"snover", "abomasnow", "vanillite", "cubchoo", "cryogonal", "bergmite", "snom", "frosmoth", "cetoddle", "frigibax", "arctibax"
					});
				}
			}
			return Choose(new[] {
				"geodude", "graveler", "onix", "zubat", "golbat", "machop", "aron", "sableye", "mawile", "diglett", "paras", "clefairy", "nosepass", "whismur", "makuhita", "duskull", "gastly",
				"roggenrola", "boldore", "drilbur", "timburr", "joltik", "ferroseed", "klang", "carbink", "noibat", "rockruff", "rolycoly", "impidimp", "nacli", "tinkatink", "glimmet"
			});
		}

		// 2. Sewers / Bug Land (Gen 1-9 Poison & Dark Pool)
		if (location is Sewer || locName.Contains("Sewer") || locName.Contains("BugLand"))
		{
			return Choose(new[] {
				"grimer", "muk", "koffing", "weezing", "gulpin", "swalot", "zubat", "golbat", "spinarak", "ariados", "venonat", "venomoth", "rattata", "raticate",
				"skorupi", "croagunk", "trubbish", "garbodor", "venipede", "whirlipede", "scolipede", "mareanie", "salandit", "toxel", "toxtricity", "shroodle", "grafaiai", "varoom"
			});
		}

		// 3. Desert / Ginger Island (Gen 1-9 Arid & Tropical Pool)
		if (location is Desert || locName.Contains("Desert") || locName.Contains("Island"))
		{
			if (locName.Contains("Island"))
			{
				if (isRain) return Choose(new[] {
					"tropius", "pelipper", "ludicolo", "exeggutor", "kingler", "corsola", "lapras",
					"goodra", "goomy", "popplio", "mareanie", "dewpider", "araquanid", "cramorant", "quaxly", "finizen", "palafin"
				});
				return Choose(new[] {
					"tropius", "exeggcute", "exeggutor", "treecko", "torchic", "mudkip", "heracross", "slakoth", "vigoroth", "wingull", "krabby", "marowak",
					"rowlet", "litten", "popplio", "pikipek", "toucannon", "fomantis", "lurantis", "comfey", "oranguru", "passimian", "sandygast", "mimikyu", "jangmo_o", "flamigo"
				});
			}
			if (isNight) return Choose(new[] {
				"cacnea", "cacturne", "trapinch", "vibrava", "sandslash", "houndour", "murkrow", "baltoy", "claydol", "lunatone",
				"sandile", "krokorok", "scraggy", "sigilyph", "yamask", "pawniard", "mandibuzz", "silicobra", "sandaconda", "lokix", "mabosstiff"
			});
			return Choose(new[] {
				"sandshrew", "sandslash", "trapinch", "vibrava", "cacnea", "baltoy", "cubone", "marowak", "doduo", "phanpy", "numel", "solrock",
				"hippopotas", "sandile", "darumaka", "dwebble", "maractus", "helioptile", "mudbray", "silicobra", "orthworm", "capsakid", "scovillain", "rellor"
			});
		}

		// 4. Beach / Ocean shore (Gen 1-9 Coastal Pool)
		if (location is Beach || locName.Contains("Beach"))
		{
			if (isRain) return Choose(new[] {
				"lotad", "lombre", "squirtle", "wartortle", "poliwhirl", "pelipper", "psyduck", "golduck", "marill", "azumarill", "spheal",
				"piplup", "oshawott", "froakie", "popplio", "sobble", "quaxly", "mareanie", "arrokuda", "wattrel", "kilowattrel", "finizen"
			});
			if (isNight) return Choose(new[] {
				"krabby", "kingler", "corphish", "staryu", "shellder", "slowpoke", "murkrow", "shuppet", "psyduck",
				"frillish", "jellicent", "inkay", "malamar", "mareanie", "sandygast", "pyukumuku", "pincurchin", "clodsire"
			});
			return Choose(new[] {
				"krabby", "corphish", "wingull", "pelipper", "psyduck", "slowpoke", "squirtle", "marill", "shellder", "taillow", "pidgey",
				"buizel", "shellos", "oshawott", "tirtouga", "binacle", "clauncher", "skrelp", "wimpod", "chewtle", "cramorant", "wiglett", "tatsugiri"
			});
		}

		// 5. Woods / Secret Woods / Forest (Gen 1-9 Woodland & Fairy Pool)
		if (location is Forest || locName.Contains("Woods") || locName.Contains("Forest"))
		{
			if (isRain) return Choose(new[] {
				"lotad", "lombre", "mudkip", "surskit", "masquerain", "roselia", "oddish", "gloom", "shroomish", "breloom", "poliwag", "wooper",
				"tympole", "palpitoad", "goomy", "sliggoo", "dewpider", "chewtle", "tadbulb", "bellibolt", "wooper", "clodsire"
			});
			if (isNight) return Choose(new[] {
				"poochyena", "mightyena", "hoothoot", "noctowl", "spinarak", "gastly", "haunter", "duskull", "shuppet", "murkrow", "umbreon", "absol",
				"mismagius", "honchkrow", "zorua", "zoroark", "litwick", "lampent", "phantump", "trevenant", "pumpkaboo", "mimikyu", "impidimp", "morgrem", "hatenna", "greavard"
			});
			return Choose(new[] {
				"caterpie", "metapod", "butterfree", "weedle", "beedrill", "wurmple", "beautifly", "oddish", "bellsprout", "shroomish", "seedot", "nuzleaf", "treecko", "bulbasaur", "chikorita", "pikachu", "ralts", "scyther", "pinsir", "heracross",
				"turtwig", "budew", "roserade", "combee", "cherubi", "snivy", "sewaddle", "leavanny", "cottonee", "whimsicott", "petilil", "lilligant", "chespin", "flabebe", "sylveon", "rowlet", "grubbin", "cutiefly", "grookey", "gossifleur", "applin", "sprigatito", "smoliv", "tinkatink"
			});
		}

		// 6. Mountain / Quarry / Railroad (Gen 1-9 Highland & Dragon/Steel Pool)
		if (location is Mountain || locName.Contains("Mountain") || locName.Contains("Railroad") || locName.Contains("Backwoods"))
		{
			if (isRain) return Choose(new[] {
				"electrike", "manectric", "magnemite", "voltorb", "aron", "larvitar", "gligar", "wooper", "quagsire",
				"shinx", "luxio", "magnezone", "joltik", "galvantula", "tynamo", "helioptile", "vikavolt", "toxel", "pawmi", "pawmo", "tadbulb"
			});
			if (isNight) return Choose(new[] {
				"sneasel", "houndour", "absol", "lunatone", "clefairy", "zubat", "golbat", "noctowl", "gligar", "duskull",
				"gliscor", "weavile", "zorua", "deino", "honedge", "noibat", "noivern", "lycanroc", "corviknight", "dreepy", "charcadet", "kingambit"
			});
			return Choose(new[] {
				"machop", "mankey", "spearow", "fearow", "taillow", "swablu", "geodude", "phanpy", "teddiursa", "skarmory", "numel", "spoink", "charmander", "cyndaquil", "torchic",
				"chimchar", "starly", "staravia", "cranidos", "shieldon", "gible", "riolu", "tepig", "roggenrola", "excadrill", "axew", "rufflet", "fennekin", "fletchling", "talonflame", "hawlucha", "rockruff", "mudbray", "scorbunny", "rookidee", "cufant", "duraludon", "fuecoco", "charcadet", "klawf", "frigibax"
			});
		}

		// 7. Town / BusStop / Farm default (Gen 1-9 Meadow & Village Pool)
		if (isRain)
		{
			return Choose(new[] {
				"lotad", "poliwag", "poliwhirl", "wooper", "mudkip", "squirtle", "totodile", "plusle", "minun", "magnemite", "roselia",
				"piplup", "buizel", "oshawott", "tympole", "ducklett", "froakie", "goomy", "popplio", "sobble", "chewtle", "quaxly", "tadbulb"
			});
		}
		if (isNight)
		{
			return Choose(new[] {
				"poochyena", "rattata", "meowth", "hoothoot", "zubat", "gastly", "drowzee", "snubbull", "ralts", "abra", "illumise", "volbeat",
				"shinx", "drifloon", "stunky", "purrloin", "munna", "zorua", "gothita", "espurr", "spritzee", "swirlix", "litten", "nickit", "impidimp", "hatenna", "maschiff", "shroodle"
			});
		}

		// Day & Sunny
		return Choose(new[] {
			"zigzagoon", "sentret", "pidgey", "rattata", "pikachu", "eevee", "oddish", "hoppip", "sunkern", "mareep", "flaaffy", "skitty", "ralts", "abra", "growlithe", "vulpix", "bulbasaur",
			"starly", "bidoof", "shinx", "pachirisu", "buneary", "munchlax", "lillipup", "pidove", "blitzle", "audino", "minccino", "emolga", "bunnelby", "fletchling", "litleo", "skiddo", "pancham", "dedenne", "pikipek", "rockruff", "stufful", "wooloo", "yamper", "lechonk", "pawmi", "tandemaus", "fidough", "sprigatito"
		});
	}

	public static string GetFishingSpecies(GameLocation location, bool isLava, bool isSewer, bool isOcean, bool isRiver, bool isLake, bool isNight, bool isRain)
	{
		// 1. Lava Fishing (Mines 100+, Caldera, Volcano)
		if (isLava)
		{
			return Choose(new[] { "torkoal", "magcargo", "slugma", "magmar", "charmander", "charmeleon", "houndoom", "camerupt", "salazzle", "turtonator", "coalossal", "centiskorch", "ceruledge" });
		}

		// 2. Sewers (Esgoto / Mutated Bug Lair)
		if (isSewer)
		{
			return Choose(new[] { "grimer", "muk", "koffing", "weezing", "qwilfish", "tentacool", "tentacruel", "barboach", "whiscash", "skrelp", "dragalge", "mareanie", "toxapex", "clodsire" });
		}

		// 3. Ocean / Beach
		if (isOcean)
		{
			if (isRain) return Choose(new[] { "gyarados", "lapras", "lanturn", "chinchou", "tentacruel", "starmie", "kingdra", "wailmer", "mantine", "gorebyss", "huntail", "relicanth", "jellicent", "dragalge", "clawitzer", "araquanid", "dhelmise", "basculegion", "palafin", "dondozo" });
			if (isNight) return Choose(new[] { "staryu", "starmie", "chinchou", "lanturn", "shellder", "cloyster", "carvanha", "sharpedo", "horsea", "seadra", "frillish", "jellicent", "skrelp", "mareanie", "pyukumuku", "veluza" });
			return Choose(new[] { "magikarp", "tentacool", "horsea", "krabby", "shellder", "staryu", "corsola", "remoraid", "octillery", "wailmer", "spheal", "clamperl", "luvdisc", "finneon", "lumineon", "tirtouga", "clauncher", "wimpod", "arrokuda", "finizen", "wiglett", "tatsugiri" });
		}

		// 4. Rivers (Town / Forest)
		if (isRiver)
		{
			if (isRain) return Choose(new[] { "politoed", "poliwhirl", "poliwrath", "lombre", "ludicolo", "quagsire", "mudkip", "marshtomp", "azumarill", "feebas", "milotic", "floatzel", "seismitoad", "greninja", "primarina", "inteleon", "quaquaval" });
			if (isNight) return Choose(new[] { "poliwhirl", "golduck", "wooper", "quagsire", "corphish", "crawdaunt", "barboach", "whiscash", "murkrow", "basculin", "greninja", "drednaw", "clodsire" });
			return Choose(new[] { "magikarp", "goldeen", "seaking", "poliwag", "psyduck", "krabby", "corphish", "marill", "lotad", "surskit", "squirtle", "totodile", "mudkip", "piplup", "buizel", "oshawott", "basculin", "froakie", "chewtle", "quaxly" });
		}

		// 5. Lakes (Mountain Lake, Farm Lake, Secret Woods Pond)
		if (isRain) return Choose(new[] { "dratini", "dragonair", "milotic", "feebas", "gyarados", "lapras", "slowbro", "slowking", "quagsire", "goodra", "wishiwashi", "basculegion", "dondozo", "tatsugiri" });
		if (isNight) return Choose(new[] { "dratini", "dragonair", "poliwhirl", "golduck", "gyarados", "carvanha", "whiscash", "basculin", "dewpider", "drednaw", "tatsugiri" });
		return Choose(new[] { "magikarp", "goldeen", "seaking", "psyduck", "slowpoke", "poliwag", "marill", "barboach", "feebas", "dratini", "buizel", "tympole", "basculin", "wishiwashi", "chewtle", "arrokuda", "tatsugiri" });
	}

	private static string Choose(string[] list)
	{
		if (list == null || list.Length == 0) return "magikarp";
		return list[Game1.random.Next(list.Length)].ToLowerInvariant();
	}
}
