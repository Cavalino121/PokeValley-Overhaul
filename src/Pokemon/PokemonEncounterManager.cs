using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Locations;

namespace StardewValley.Pokemon;

public static class PokemonEncounterManager
{
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
		public bool RequiresNight { get; set; }
		public bool RequiresDay { get; set; }
		public Season? RequiredSeason { get; set; }
	}

	private static readonly Dictionary<string, List<LegendarySummonDef>> LegendarySummonsByRelic = new(StringComparer.OrdinalIgnoreCase);
	private static int _lastLegendarySummonTick = -1000;

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
		RegisterSummon("Poke_FreezingFeather", "FreezingFeather", new LegendarySummonDef { SpeciesName = "Articuno", Location = "Mine", TargetX = 18, TargetY = 10, RelicId = "Poke_FreezingFeather", RelicName = "Pena Glacial", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Winter });
		RegisterSummon("Poke_ElectricFeather", "ElectricFeather", new LegendarySummonDef { SpeciesName = "Zapdos", Location = "Mountain", TargetX = 32, TargetY = 8, RelicId = "Poke_ElectricFeather", RelicName = "Pena Elétrica", RequiresStorm = true, RequiresRain = true, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_FlameFeather", "FlameFeather", new LegendarySummonDef { SpeciesName = "Moltres", Location = "Caldera", TargetX = 25, TargetY = 31, RelicId = "Poke_FlameFeather", RelicName = "Pena de Chamas", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_BerserkGene", "BerserkGene", new LegendarySummonDef { SpeciesName = "Mewtwo", Location = "SkullCave", TargetX = 3, TargetY = 4, RelicId = "Poke_BerserkGene", RelicName = "Gene Berserk", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_OldSeaMap", "OldSeaMap", new LegendarySummonDef { SpeciesName = "Mew", Location = "Island_SE", TargetX = 22, TargetY = 18, RelicId = "Poke_OldSeaMap", RelicName = "Mapa do Mar Antigo", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_RaikouBell", "RaikouBell", new LegendarySummonDef { SpeciesName = "Raikou", Location = "Woods", TargetX = 28, TargetY = 15, RelicId = "Poke_RaikouBell", RelicName = "Sino do Trovão", RequiresStorm = true, RequiresRain = true, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_EnteiBell", "EnteiBell", new LegendarySummonDef { SpeciesName = "Entei", Location = "Mountain", TargetX = 48, TargetY = 12, RelicId = "Poke_EnteiBell", RelicName = "Sino do Magma", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_SuicuneBell", "SuicuneBell", new LegendarySummonDef { SpeciesName = "Suicune", Location = "Beach", TargetX = 28, TargetY = 35, RelicId = "Poke_SuicuneBell", RelicName = "Sino das Ondas", RequiresStorm = false, RequiresRain = true, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall });
		RegisterSummon("Poke_RainbowWing", "RainbowWing", new LegendarySummonDef { SpeciesName = "Ho-Oh", Location = "Summit", TargetX = 11, TargetY = 13, RelicId = "Poke_RainbowWing", RelicName = "Asa do Arco-Íris", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_GSBall", "GSBall", new LegendarySummonDef { SpeciesName = "Celebi", Location = "Woods", TargetX = 15, TargetY = 15, RelicId = "Poke_GSBall", RelicName = "GS Ball", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_RelicRock", "RelicRock", new LegendarySummonDef { SpeciesName = "Regirock", Location = "Mine", TargetX = 21, TargetY = 12, RelicId = "Poke_RelicRock", RelicName = "Núcleo de Pedra Ancestral", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_RelicIce", "RelicIce", new LegendarySummonDef { SpeciesName = "Regice", Location = "Mine", TargetX = 18, TargetY = 10, RelicId = "Poke_RelicIce", RelicName = "Núcleo de Gelo Eterno", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Winter });
		RegisterSummon("Poke_RelicSteel", "RelicSteel", new LegendarySummonDef { SpeciesName = "Registeel", Location = "Mine", TargetX = 21, TargetY = 12, RelicId = "Poke_RelicSteel", RelicName = "Núcleo de Aço Cósmico", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_EonFlute_Latias", "EonFlute_Latias", new LegendarySummonDef { SpeciesName = "Latias", Location = "Island_W", TargetX = 30, TargetY = 40, RelicId = "Poke_EonFlute_Latias", RelicName = "Flauta Eon Escarlate", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_EonFlute_Latios", "EonFlute_Latios", new LegendarySummonDef { SpeciesName = "Latios", Location = "Beach", TargetX = 40, TargetY = 20, RelicId = "Poke_EonFlute_Latios", RelicName = "Flauta Eon Anil", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_RedOrb", "RedOrb", new LegendarySummonDef { SpeciesName = "Groudon", Location = "Caldera", TargetX = 21, TargetY = 38, RelicId = "Poke_RedOrb", RelicName = "Esfera Vermelha Primordial", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_JadeOrb", "JadeOrb", new LegendarySummonDef { SpeciesName = "Rayquaza", Location = "Summit", TargetX = 11, TargetY = 13, RelicId = "Poke_JadeOrb", RelicName = "Esfera de Jade", RequiresStorm = true, RequiresRain = true, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_CometShard", "CometShard", new LegendarySummonDef { SpeciesName = "Jirachi", Location = "Mountain", TargetX = 48, TargetY = 12, RelicId = "Poke_CometShard", RelicName = "Fragmento de Cometa", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_MeteoritePiece", "MeteoritePiece", new LegendarySummonDef { SpeciesName = "Deoxys", Location = "Railroad", TargetX = 18, TargetY = 42, RelicId = "Poke_MeteoritePiece", RelicName = "Pedaço de Meteorito", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_MindGem", "MindGem", new LegendarySummonDef { SpeciesName = "Uxie", Location = "Mountain", TargetX = 48, TargetY = 12, RelicId = "Poke_MindGem", RelicName = "Gema da Sabedoria", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_EmotionGem", "EmotionGem", new LegendarySummonDef { SpeciesName = "Mesprit", Location = "Woods", TargetX = 15, TargetY = 15, RelicId = "Poke_EmotionGem", RelicName = "Gema da Emoção", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_ValorGem", "ValorGem", new LegendarySummonDef { SpeciesName = "Azelf", Location = "Forest", TargetX = 45, TargetY = 50, RelicId = "Poke_ValorGem", RelicName = "Gema da Bravura", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_AdamantOrb", "AdamantOrb", new LegendarySummonDef { SpeciesName = "Dialga", Location = "Summit", TargetX = 11, TargetY = 13, RelicId = "Poke_AdamantOrb", RelicName = "Adamant Orb", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_LustrousOrb", "LustrousOrb", new LegendarySummonDef { SpeciesName = "Palkia", Location = "Summit", TargetX = 10, TargetY = 15, RelicId = "Poke_LustrousOrb", RelicName = "Lustrous Orb", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_MagmaStone", "MagmaStone", new LegendarySummonDef { SpeciesName = "Heatran", Location = "Caldera", TargetX = 25, TargetY = 31, RelicId = "Poke_MagmaStone", RelicName = "Pedra de Magma", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_ColossalKey", "ColossalKey", new LegendarySummonDef { SpeciesName = "Regigigas", Location = "Mine", TargetX = 18, TargetY = 10, RelicId = "Poke_ColossalKey", RelicName = "Chave Colossal", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_GriseousCore", "GriseousCore", new LegendarySummonDef { SpeciesName = "Giratina", Location = "Sewer", TargetX = 16, TargetY = 12, RelicId = "Poke_GriseousCore", RelicName = "Griseous Core", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_LunarWing", "LunarWing", new LegendarySummonDef { SpeciesName = "Cresselia", Location = "Beach", TargetX = 28, TargetY = 35, RelicId = "Poke_LunarWing", RelicName = "Pena Lunar", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_MemberCard", "MemberCard", new LegendarySummonDef { SpeciesName = "Darkrai", Location = "Railroad", TargetX = 25, TargetY = 45, RelicId = "Poke_MemberCard", RelicName = "Cartão de Membro Sombrio", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall });
		RegisterSummon("Poke_GracideaFlower", "GracideaFlower", new LegendarySummonDef { SpeciesName = "Shaymin", Location = "Forest", TargetX = 60, TargetY = 40, RelicId = "Poke_GracideaFlower", RelicName = "Flor Gracídea", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_AzureFlute", "AzureFlute", new LegendarySummonDef { SpeciesName = "Arceus", Location = "Summit", TargetX = 11, TargetY = 13, RelicId = "Poke_AzureFlute", RelicName = "Flauta Azure", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_LibertyPass", "LibertyPass", new LegendarySummonDef { SpeciesName = "Victini", Location = "Island_SE", TargetX = 22, TargetY = 18, RelicId = "Poke_LibertyPass", RelicName = "Passe da Liberdade", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_IronWillRelic", "IronWillRelic", new LegendarySummonDef { SpeciesName = "Cobalion", Location = "Mine", TargetX = 21, TargetY = 12, RelicId = "Poke_IronWillRelic", RelicName = "Relíquia da Lâmina de Ferro", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_StoneWillRelic", "StoneWillRelic", new LegendarySummonDef { SpeciesName = "Terrakion", Location = "Mountain", TargetX = 103, TargetY = 14, RelicId = "Poke_StoneWillRelic", RelicName = "Relíquia da Lâmina de Rocha", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_GrassWillRelic", "GrassWillRelic", new LegendarySummonDef { SpeciesName = "Virizion", Location = "Woods", TargetX = 28, TargetY = 15, RelicId = "Poke_GrassWillRelic", RelicName = "Relíquia da Lâmina Esmeralda", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_CycloneMirror", "CycloneMirror", new LegendarySummonDef { SpeciesName = "Tornadus", Location = "Mountain", TargetX = 32, TargetY = 8, RelicId = "Poke_CycloneMirror", RelicName = "Espelho dos Ciclones", RequiresStorm = true, RequiresRain = true, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Fall });
		RegisterSummon("Poke_BoltMirror", "BoltMirror", new LegendarySummonDef { SpeciesName = "Thundurus", Location = "Railroad", TargetX = 18, TargetY = 42, RelicId = "Poke_BoltMirror", RelicName = "Espelho dos Trovões", RequiresStorm = true, RequiresRain = true, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_LightStone", "LightStone", new LegendarySummonDef { SpeciesName = "Reshiram", Location = "Caldera", TargetX = 25, TargetY = 31, RelicId = "Poke_LightStone", RelicName = "Light Stone", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_DarkStone", "DarkStone", new LegendarySummonDef { SpeciesName = "Zekrom", Location = "Desert", TargetX = 18, TargetY = 32, RelicId = "Poke_DarkStone", RelicName = "Dark Stone", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_FertilityMirror", "FertilityMirror", new LegendarySummonDef { SpeciesName = "Landorus", Location = "Forest", TargetX = 60, TargetY = 40, RelicId = "Poke_FertilityMirror", RelicName = "Espelho da Fertilidade", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_DNA_Splicers", "DNA_Splicers", new LegendarySummonDef { SpeciesName = "Kyurem", Location = "Mine", TargetX = 18, TargetY = 10, RelicId = "Poke_DNA_Splicers", RelicName = "Fundidores de DNA", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Winter });
		RegisterSummon("Poke_SecretSwordRelic", "SecretSwordRelic", new LegendarySummonDef { SpeciesName = "Keldeo", Location = "Forest", TargetX = 45, TargetY = 50, RelicId = "Poke_SecretSwordRelic", RelicName = "Relíquia da Espada Secreta", RequiresStorm = false, RequiresRain = true, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_RelicSongSheet", "RelicSongSheet", new LegendarySummonDef { SpeciesName = "Meloetta", Location = "Town", TargetX = 55, TargetY = 16, RelicId = "Poke_RelicSongSheet", RelicName = "Partitura Ancestral", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_DriveBurn", "DriveBurn", new LegendarySummonDef { SpeciesName = "Genesect", Location = "Mine", TargetX = 21, TargetY = 12, RelicId = "Poke_DriveBurn", RelicName = "Burn Drive Tecnológico", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_LifeTreeRoot", "LifeTreeRoot", new LegendarySummonDef { SpeciesName = "Xerneas", Location = "Woods", TargetX = 28, TargetY = 15, RelicId = "Poke_LifeTreeRoot", RelicName = "Raiz da Árvore da Vida", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_DestructionCocoon", "DestructionCocoon", new LegendarySummonDef { SpeciesName = "Yveltal", Location = "SkullCave", TargetX = 3, TargetY = 4, RelicId = "Poke_DestructionCocoon", RelicName = "Fragmento de Casulo Noturno", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall });
		RegisterSummon("Poke_ZygardeCube", "ZygardeCube", new LegendarySummonDef { SpeciesName = "Zygarde", Location = "Mine", TargetX = 21, TargetY = 12, RelicId = "Poke_ZygardeCube", RelicName = "Cubo de Zygarde", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_PinkDiamondRelic", "PinkDiamondRelic", new LegendarySummonDef { SpeciesName = "Diancie", Location = "Mine", TargetX = 18, TargetY = 10, RelicId = "Poke_PinkDiamondRelic", RelicName = "Diamante Cor-de-Rosa", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_PrisonBottle", "PrisonBottle", new LegendarySummonDef { SpeciesName = "Hoopa", Location = "Desert", TargetX = 33, TargetY = 43, RelicId = "Poke_PrisonBottle", RelicName = "Garrafa da Prisão", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_SteamCore", "SteamCore", new LegendarySummonDef { SpeciesName = "Volcanion", Location = "Caldera", TargetX = 21, TargetY = 38, RelicId = "Poke_SteamCore", RelicName = "Núcleo de Caldeira Térmica", RequiresStorm = false, RequiresRain = true, RequiresNight = false, RequiresDay = false, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_SyntheticHelmet", "SyntheticHelmet", new LegendarySummonDef { SpeciesName = "Type: Null", Location = "Mine", TargetX = 21, TargetY = 12, RelicId = "Poke_SyntheticHelmet", RelicName = "Máscara de Controle de Fera", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_MemoryDrive_Normal", "MemoryDrive_Normal", new LegendarySummonDef { SpeciesName = "Silvally", Location = "Mine", TargetX = 21, TargetY = 12, RelicId = "Poke_MemoryDrive_Normal", RelicName = "Disco de Memória RKS", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_KokoMask", "KokoMask", new LegendarySummonDef { SpeciesName = "Tapu Koko", Location = "Mountain", TargetX = 32, TargetY = 8, RelicId = "Poke_KokoMask", RelicName = "Máscara Trovejante de Tapu", RequiresStorm = true, RequiresRain = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_LeleMask", "LeleMask", new LegendarySummonDef { SpeciesName = "Tapu Lele", Location = "Woods", TargetX = 28, TargetY = 15, RelicId = "Poke_LeleMask", RelicName = "Máscara Floral de Tapu", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_BuluMask", "BuluMask", new LegendarySummonDef { SpeciesName = "Tapu Bulu", Location = "Forest", TargetX = 60, TargetY = 40, RelicId = "Poke_BuluMask", RelicName = "Máscara Telúrica de Tapu", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Fall });
		RegisterSummon("Poke_FiniMask", "FiniMask", new LegendarySummonDef { SpeciesName = "Tapu Fini", Location = "Beach", TargetX = 28, TargetY = 35, RelicId = "Poke_FiniMask", RelicName = "Máscara das Névoas de Tapu", RequiresStorm = false, RequiresRain = true, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_NebulaCore", "NebulaCore", new LegendarySummonDef { SpeciesName = "Cosmog", Location = "Summit", TargetX = 11, TargetY = 13, RelicId = "Poke_NebulaCore", RelicName = "Núcleo de Poeira Estelar", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_CosmicCocoon", "CosmicCocoon", new LegendarySummonDef { SpeciesName = "Cosmoem", Location = "Summit", TargetX = 10, TargetY = 15, RelicId = "Poke_CosmicCocoon", RelicName = "Crisálida das Estrelas", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_SunFlute", "SunFlute", new LegendarySummonDef { SpeciesName = "Solgaleo", Location = "Desert", TargetX = 33, TargetY = 43, RelicId = "Poke_SunFlute", RelicName = "Flauta do Sol", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_MoonFlute", "MoonFlute", new LegendarySummonDef { SpeciesName = "Lunala", Location = "Beach", TargetX = 28, TargetY = 35, RelicId = "Poke_MoonFlute", RelicName = "Flauta da Lua", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BeastBall_Relic", "BeastBall_Relic", new LegendarySummonDef { SpeciesName = "Nihilego", Location = "Mine", TargetX = 18, TargetY = 10, RelicId = "Poke_BeastBall_Relic", RelicName = "Ultra Esfera Transdimensional", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BeastBall_Relic", "BeastBall_Relic", new LegendarySummonDef { SpeciesName = "Buzzwole", Location = "Forest", TargetX = 45, TargetY = 50, RelicId = "Poke_BeastBall_Relic", RelicName = "Ultra Esfera Transdimensional", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_BeastBall_Relic", "BeastBall_Relic", new LegendarySummonDef { SpeciesName = "Pheromosa", Location = "Desert", TargetX = 18, TargetY = 32, RelicId = "Poke_BeastBall_Relic", RelicName = "Ultra Esfera Transdimensional", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BeastBall_Relic", "BeastBall_Relic", new LegendarySummonDef { SpeciesName = "Xurkitree", Location = "Railroad", TargetX = 18, TargetY = 42, RelicId = "Poke_BeastBall_Relic", RelicName = "Ultra Esfera Transdimensional", RequiresStorm = true, RequiresRain = true, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BeastBall_Relic", "BeastBall_Relic", new LegendarySummonDef { SpeciesName = "Celesteela", Location = "Summit", TargetX = 11, TargetY = 13, RelicId = "Poke_BeastBall_Relic", RelicName = "Ultra Esfera Transdimensional", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BeastBall_Relic", "BeastBall_Relic", new LegendarySummonDef { SpeciesName = "Kartana", Location = "Woods", TargetX = 28, TargetY = 15, RelicId = "Poke_BeastBall_Relic", RelicName = "Ultra Esfera Transdimensional", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Fall });
		RegisterSummon("Poke_BeastBall_Relic", "BeastBall_Relic", new LegendarySummonDef { SpeciesName = "Guzzlord", Location = "Sewer", TargetX = 30, TargetY = 20, RelicId = "Poke_BeastBall_Relic", RelicName = "Ultra Esfera Transdimensional", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_UltranecroziumZ", "UltranecroziumZ", new LegendarySummonDef { SpeciesName = "Necrozma", Location = "SkullCave", TargetX = 3, TargetY = 4, RelicId = "Poke_UltranecroziumZ", RelicName = "Ultralnecrozium Z", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_SoulHeartRelic", "SoulHeartRelic", new LegendarySummonDef { SpeciesName = "Magearna", Location = "Town", TargetX = 55, TargetY = 16, RelicId = "Poke_SoulHeartRelic", RelicName = "Alma Coração Mecânica", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_MarshadiumZ", "MarshadiumZ", new LegendarySummonDef { SpeciesName = "Marshadow", Location = "BugLand", TargetX = 31, TargetY = 20, RelicId = "Poke_MarshadiumZ", RelicName = "Marshadium Z", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BeastBall_Relic", "BeastBall_Relic", new LegendarySummonDef { SpeciesName = "Poipole", Location = "BugLand", TargetX = 31, TargetY = 20, RelicId = "Poke_BeastBall_Relic", RelicName = "Ultra Esfera Transdimensional", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_DragonPoisonBarb", "DragonPoisonBarb", new LegendarySummonDef { SpeciesName = "Naganadel", Location = "BugLand", TargetX = 31, TargetY = 20, RelicId = "Poke_DragonPoisonBarb", RelicName = "Ferrão Venenoso de Dragão", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BeastBall_Relic", "BeastBall_Relic", new LegendarySummonDef { SpeciesName = "Stakataka", Location = "Mountain", TargetX = 103, TargetY = 14, RelicId = "Poke_BeastBall_Relic", RelicName = "Ultra Esfera Transdimensional", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BeastBall_Relic", "BeastBall_Relic", new LegendarySummonDef { SpeciesName = "Blacephalon", Location = "Railroad", TargetX = 25, TargetY = 45, RelicId = "Poke_BeastBall_Relic", RelicName = "Ultra Esfera Transdimensional", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_ThunderClawRelic", "ThunderClawRelic", new LegendarySummonDef { SpeciesName = "Zeraora", Location = "Mountain", TargetX = 32, TargetY = 8, RelicId = "Poke_ThunderClawRelic", RelicName = "Garra da Fagulha Plasma", RequiresStorm = true, RequiresRain = true, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_MysteryBox_Steel", "MysteryBox_Steel", new LegendarySummonDef { SpeciesName = "Meltan", Location = "Mine", TargetX = 18, TargetY = 10, RelicId = "Poke_MysteryBox_Steel", RelicName = "Caixa Misteriosa de Aço Líquido", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_GigantamaxNut", "GigantamaxNut", new LegendarySummonDef { SpeciesName = "Melmetal", Location = "Mine", TargetX = 21, TargetY = 12, RelicId = "Poke_GigantamaxNut", RelicName = "Porca Hexagonal Titânica", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_RustedSword", "RustedSword", new LegendarySummonDef { SpeciesName = "Zacian", Location = "Forest", TargetX = 45, TargetY = 50, RelicId = "Poke_RustedSword", RelicName = "Espada Enferrujada", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_RustedShield", "RustedShield", new LegendarySummonDef { SpeciesName = "Zamazenta", Location = "Mountain", TargetX = 48, TargetY = 12, RelicId = "Poke_RustedShield", RelicName = "Escudo Enferrujado", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_WishingStar", "WishingStar", new LegendarySummonDef { SpeciesName = "Eternatus", Location = "Summit", TargetX = 11, TargetY = 13, RelicId = "Poke_WishingStar", RelicName = "Estrela dos Desejos Corrompida", RequiresStorm = true, RequiresRain = true, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_MartialScroll", "MartialScroll", new LegendarySummonDef { SpeciesName = "Kubfu", Location = "Desert", TargetX = 18, TargetY = 32, RelicId = "Poke_MartialScroll", RelicName = "Pergaminho das Artes Marciais", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_ScrollOfDarkness", "ScrollOfDarkness", new LegendarySummonDef { SpeciesName = "Urshifu", Location = "Caldera", TargetX = 25, TargetY = 31, RelicId = "Poke_ScrollOfDarkness", RelicName = "Pergaminho das Trevas", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_JungleVineRelic", "JungleVineRelic", new LegendarySummonDef { SpeciesName = "Zarude", Location = "Woods", TargetX = 28, TargetY = 15, RelicId = "Poke_JungleVineRelic", RelicName = "Cipó da Selva Primordial", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_ElekiPattern", "ElekiPattern", new LegendarySummonDef { SpeciesName = "Regieleki", Location = "Railroad", TargetX = 18, TargetY = 42, RelicId = "Poke_ElekiPattern", RelicName = "Padrão de Raios em Braille", RequiresStorm = true, RequiresRain = true, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_DragoPattern", "DragoPattern", new LegendarySummonDef { SpeciesName = "Regidrago", Location = "Caldera", TargetX = 21, TargetY = 38, RelicId = "Poke_DragoPattern", RelicName = "Mandíbula de Dragão em Braille", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_IcerootCarrot", "IcerootCarrot", new LegendarySummonDef { SpeciesName = "Glastrier", Location = "Summit", TargetX = 11, TargetY = 13, RelicId = "Poke_IcerootCarrot", RelicName = "Cenoura Gélida da Tundra", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Winter });
		RegisterSummon("Poke_ShaderootCarrot", "ShaderootCarrot", new LegendarySummonDef { SpeciesName = "Spectrier", Location = "Woods", TargetX = 15, TargetY = 15, RelicId = "Poke_ShaderootCarrot", RelicName = "Cenoura Espectral das Sombras", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall });
		RegisterSummon("Poke_ReinsOfUnity", "ReinsOfUnity", new LegendarySummonDef { SpeciesName = "Calyrex", Location = "Forest", TargetX = 60, TargetY = 40, RelicId = "Poke_ReinsOfUnity", RelicName = "Rédeas da Unidade Sagrada", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_RevealGlass_Love", "RevealGlass_Love", new LegendarySummonDef { SpeciesName = "Enamorus", Location = "Woods", TargetX = 28, TargetY = 15, RelicId = "Poke_RevealGlass_Love", RelicName = "Espelho do Amor da Primavera", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_RuinousLeaves", "RuinousLeaves", new LegendarySummonDef { SpeciesName = "Wo-Chien", Location = "Forest", TargetX = 45, TargetY = 50, RelicId = "Poke_RuinousLeaves", RelicName = "Tábua de Madeira da Ruína", RequiresStorm = false, RequiresRain = true, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall });
		RegisterSummon("Poke_RuinousSword", "RuinousSword", new LegendarySummonDef { SpeciesName = "Chien-Pao", Location = "Summit", TargetX = 11, TargetY = 13, RelicId = "Poke_RuinousSword", RelicName = "Espada Fraturada de Gelo", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Winter });
		RegisterSummon("Poke_RuinousVessel", "RuinousVessel", new LegendarySummonDef { SpeciesName = "Ting-Lu", Location = "Mountain", TargetX = 103, TargetY = 14, RelicId = "Poke_RuinousVessel", RelicName = "Vaso de Pedra da Ruína", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_RuinousBeads", "RuinousBeads", new LegendarySummonDef { SpeciesName = "Chi-Yu", Location = "Caldera", TargetX = 25, TargetY = 31, RelicId = "Poke_RuinousBeads", RelicName = "Contas de Fogo da Ruína", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_BoosterEnergy_Past", "BoosterEnergy_Past", new LegendarySummonDef { SpeciesName = "Roaring Moon", Location = "SkullCave", TargetX = 3, TargetY = 4, RelicId = "Poke_BoosterEnergy_Past", RelicName = "Cápsula de Energia Ancestral", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BoosterEnergy_Future", "BoosterEnergy_Future", new LegendarySummonDef { SpeciesName = "Iron Valiant", Location = "Mine", TargetX = 21, TargetY = 12, RelicId = "Poke_BoosterEnergy_Future", RelicName = "Cápsula de Energia Futurista", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_KoraidonFeather", "KoraidonFeather", new LegendarySummonDef { SpeciesName = "Koraidon", Location = "Caldera", TargetX = 25, TargetY = 31, RelicId = "Poke_KoraidonFeather", RelicName = "Pena do Passado Selvagem", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_MiraidonCore", "MiraidonCore", new LegendarySummonDef { SpeciesName = "Miraidon", Location = "Railroad", TargetX = 25, TargetY = 45, RelicId = "Poke_MiraidonCore", RelicName = "Núcleo de Propulsão Futura", RequiresStorm = true, RequiresRain = true, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BoosterEnergy_Past", "BoosterEnergy_Past", new LegendarySummonDef { SpeciesName = "Walking Wake", Location = "Beach", TargetX = 28, TargetY = 35, RelicId = "Poke_BoosterEnergy_Past", RelicName = "Cápsula de Energia Ancestral", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_BoosterEnergy_Future", "BoosterEnergy_Future", new LegendarySummonDef { SpeciesName = "Iron Leaves", Location = "Woods", TargetX = 28, TargetY = 15, RelicId = "Poke_BoosterEnergy_Future", RelicName = "Cápsula de Energia Futurista", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_ToxicChain_Oki", "ToxicChain_Oki", new LegendarySummonDef { SpeciesName = "Okidogi", Location = "Mountain", TargetX = 48, TargetY = 12, RelicId = "Poke_ToxicChain_Oki", RelicName = "Corrente Tóxica de Força", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_ToxicChain_Munk", "ToxicChain_Munk", new LegendarySummonDef { SpeciesName = "Munkidori", Location = "Woods", TargetX = 15, TargetY = 15, RelicId = "Poke_ToxicChain_Munk", RelicName = "Corrente Tóxica da Astúcia", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_ToxicChain_Fez", "ToxicChain_Fez", new LegendarySummonDef { SpeciesName = "Fezandipiti", Location = "Forest", TargetX = 45, TargetY = 50, RelicId = "Poke_ToxicChain_Fez", RelicName = "Corrente Tóxica da Beleza", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_TealMask", "TealMask", new LegendarySummonDef { SpeciesName = "Ogerpon", Location = "Woods", TargetX = 28, TargetY = 15, RelicId = "Poke_TealMask", RelicName = "Máscara Turquesa de Hera", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Spring });
		RegisterSummon("Poke_BoosterEnergy_Past", "BoosterEnergy_Past", new LegendarySummonDef { SpeciesName = "Gouging Fire", Location = "Caldera", TargetX = 21, TargetY = 38, RelicId = "Poke_BoosterEnergy_Past", RelicName = "Cápsula de Energia Ancestral", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = true, RequiredSeason = Season.Summer });
		RegisterSummon("Poke_BoosterEnergy_Past", "BoosterEnergy_Past", new LegendarySummonDef { SpeciesName = "Raging Bolt", Location = "Mountain", TargetX = 32, TargetY = 8, RelicId = "Poke_BoosterEnergy_Past", RelicName = "Cápsula de Energia Ancestral", RequiresStorm = true, RequiresRain = true, RequiresNight = false, RequiresDay = true, RequiredSeason = null });
		RegisterSummon("Poke_BoosterEnergy_Future", "BoosterEnergy_Future", new LegendarySummonDef { SpeciesName = "Iron Boulder", Location = "Mountain", TargetX = 103, TargetY = 14, RelicId = "Poke_BoosterEnergy_Future", RelicName = "Cápsula de Energia Futurista", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_BoosterEnergy_Future", "BoosterEnergy_Future", new LegendarySummonDef { SpeciesName = "Iron Crown", Location = "Mine", TargetX = 21, TargetY = 12, RelicId = "Poke_BoosterEnergy_Future", RelicName = "Cápsula de Energia Futurista", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_IndigoDisk", "IndigoDisk", new LegendarySummonDef { SpeciesName = "Terapagos", Location = "Caldera", TargetX = 25, TargetY = 31, RelicId = "Poke_IndigoDisk", RelicName = "Disco Índigo Cristalino", RequiresStorm = false, RequiresRain = false, RequiresNight = false, RequiresDay = false, RequiredSeason = null });
		RegisterSummon("Poke_MythicalPechaMochi", "MythicalPechaMochi", new LegendarySummonDef { SpeciesName = "Pecharunt", Location = "Town", TargetX = 55, TargetY = 16, RelicId = "Poke_MythicalPechaMochi", RelicName = "Mochi Venenoso de Pêssego", RequiresStorm = false, RequiresRain = false, RequiresNight = true, RequiresDay = false, RequiredSeason = Season.Fall });
	}

	private static void CheckActiveItemLegendarySummon(GameLocation loc, Farmer player, LocationSpawnState state)
	{
		if (player == null || loc == null || state == null) return;
		Item held = player.ActiveObject ?? player.CurrentItem;
		if (held == null) return;

		string heldId = held.ItemId;
		if (!LegendarySummonsByRelic.TryGetValue(heldId, out var summons))
			return;

		string locName = loc.NameOrUniqueName ?? "";

		foreach (var def in summons)
		{
			if (!locName.Equals(def.Location, StringComparison.OrdinalIgnoreCase))
				continue;

			// Check distance to the sacred habitat altar / coordinates (within 6 tiles)
			float dist = Vector2.Distance(player.Tile, new Vector2(def.TargetX, def.TargetY));
			if (dist > 6.0f)
				continue;

			// Environmental conditions check
			if (def.RequiresStorm && !Game1.isLightning) continue;
			if (def.RequiresRain && !Game1.isRaining) continue;
			if (def.RequiresNight && Game1.timeOfDay < 1800) continue;
			if (def.RequiresDay && (Game1.timeOfDay >= 1800 || Game1.timeOfDay < 600)) continue;
			if (def.RequiredSeason.HasValue && Game1.season != def.RequiredSeason.Value) continue;

			// Prevent duplicate spawn on map
			if (state.Spawns.Any(s => s.Instance?.Species?.Name.Equals(def.SpeciesName, StringComparison.OrdinalIgnoreCase) == true))
				continue;

			// Debounce cooldown (at least 5 seconds between summons)
			if (Game1.ticks - _lastLegendarySummonTick < 300)
				continue;

			_lastLegendarySummonTick = Game1.ticks;

			// Consume 1 relic item from player's hand/inventory
			if (player.ActiveObject != null)
			{
				player.ActiveObject.ConsumeStack(1);
				if (player.ActiveObject.Stack <= 0) player.ActiveObject = null;
			}
			else
			{
				player.removeFirstOfThisItemFromInventory(heldId);
				player.removeFirstOfThisItemFromInventory("(O)" + heldId);
			}

			// Dramatic summon effects
			Game1.playSound("thunder");
			Game1.playSound("roar");
			Rumble.rumble(0.9f, 600f);
			Game1.screenGlowOnce(Color.Gold, hold: false);

			// Spawn right in front of the player
			var spec = PokemonSpecies.Get(def.SpeciesName);
			if (spec != null)
			{
				Point spawnTile = new Point((int)player.Tile.X, (int)player.Tile.Y);
				switch (player.FacingDirection)
				{
					case 0: spawnTile.Y -= 2; break;
					case 1: spawnTile.X += 2; break;
					case 2: spawnTile.Y += 2; break;
					case 3: spawnTile.X -= 2; break;
				}
				SpawnWildAt(loc, spawnTile, spec.InternalName, level: 70, isShiny: RollWildShiny());
				Game1.addHUDMessage(new HUDMessage($"✨ A relíquia ressoou com o habitat! O lendário {spec.Name} emergiu!", 2));
			}
			break;
		}
	}

	public static void UpdateOverworldSpawns(GameTime gameTime)
	{
		var loc = Game1.currentLocation;
		var player = Game1.player;
		if (loc == null || player == null || !PlayerParty.GetForFarmer(player).HasChosenStarter)
			return;

		float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
		PokemonMusicManager.UpdateOverworldMusic(dt);
		string locKey = loc.NameOrUniqueName;

		bool validMap = loc.IsOutdoors || loc is MineShaft || loc is Sewer || loc is VolcanoDungeon || locKey.Contains("Cave") || locKey.Contains("Mine");
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
					Rectangle box = new Rectangle((int)nextPos.X - 14, (int)nextPos.Y - 14, 28, 28);
					Point tilePt = new Point((int)(nextPos.X / 64f), (int)(nextPos.Y / 64f));
					if (loc.isTileOnMap(new Vector2(tilePt.X, tilePt.Y)) &&
					    loc.isTilePassable(new xTile.Dimensions.Location(tilePt.X, tilePt.Y), Game1.viewport) &&
					    !loc.Objects.ContainsKey(new Vector2(tilePt.X, tilePt.Y)) &&
					    !loc.isCollidingPosition(box, Game1.viewport, isFarmer: false, damagesFarmer: 0, glider: false, character: null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true, skipCollisionEffects: true))
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
		for (int attempt = 0; attempt < 15; attempt++)
		{
			int dx = Game1.random.Next(-10, 11);
			int dy = Game1.random.Next(-8, 9);
			if (Math.Abs(dx) < 4 && Math.Abs(dy) < 4) continue;

			int tx = pTile.X + dx;
			int ty = pTile.Y + dy;
			Vector2 tileVec = new Vector2(tx, ty);
			if (!loc.isTileOnMap(tileVec)) continue;
			if (!loc.isTilePassable(new xTile.Dimensions.Location(tx, ty), Game1.viewport)) continue;
			if (loc.Objects.ContainsKey(tileVec)) continue;
			if (loc.isWaterTile(tx, ty)) continue;

			Vector2 worldPos = new Vector2(tx * 64f + 32f, ty * 64f + 32f);
			Rectangle box = new Rectangle((int)worldPos.X - 16, (int)worldPos.Y - 16, 32, 32);
			if (loc.isCollidingPosition(box, Game1.viewport, isFarmer: false, damagesFarmer: 0, glider: false, character: null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true, skipCollisionEffects: true))
				continue;

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
