using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Locations;

namespace StardewValley.Pokemon;

public static class PokemonRelics
{
	// Elemental affinity mappings for Geodes
	public static readonly Dictionary<string, List<PokemonType>> GeodeTypeAffinities = new(StringComparer.OrdinalIgnoreCase)
	{
		// Regular Geode: Ground, Rock, Normal, Grass, Steel, Bug
		["(O)535"] = new List<PokemonType> { PokemonType.Ground, PokemonType.Rock, PokemonType.Normal, PokemonType.Grass, PokemonType.Steel, PokemonType.Bug },
		// Frozen Geode: Ice, Water, Flying
		["(O)536"] = new List<PokemonType> { PokemonType.Ice, PokemonType.Water, PokemonType.Flying },
		// Magma Geode: Fire, Fighting, Poison, Electric
		["(O)537"] = new List<PokemonType> { PokemonType.Fire, PokemonType.Fighting, PokemonType.Poison, PokemonType.Electric },
		// Omni Geode: Dragon, Fairy, Psychic, Ghost, Dark + Wildcard of all types
		["(O)749"] = new List<PokemonType> { PokemonType.Dragon, PokemonType.Fairy, PokemonType.Psychic, PokemonType.Ghost, PokemonType.Dark }
	};

	public static Item TryGetGeodeRelic(Item geode, Random random)
	{
		if (geode == null || random == null) return null;
		string qId = geode.QualifiedItemId;
		bool isOmni = qId == "(O)749" || qId.Contains("MysteryBox") || qId == "(O)GoldenCoconut";

		List<PokemonType> affinities;
		if (!GeodeTypeAffinities.TryGetValue(qId, out affinities))
		{
			if (isOmni)
			{
				affinities = new List<PokemonType>((PokemonType[])Enum.GetValues(typeof(PokemonType)));
			}
			else
			{
				return null;
			}
		}

		// Pity tracking
		int pity = 0;
		if (Game1.player != null && Game1.player.modData != null)
		{
			if (Game1.player.modData.TryGetValue("poke_relicPity", out string pityStr) && int.TryParse(pityStr, out int pVal))
			{
				pity = pVal;
			}
			pity++;
			Game1.player.modData["poke_relicPity"] = pity.ToString();
		}

		double roll = random.NextDouble();
		bool guaranteedRelic = pity >= 40;

		// Odds:
		// Mega Stone: 4% (Omni: 6%)
		// Z-Crystal: 4% (Omni: 6%)
		// Tera Shards: 1% (Omni: 2%) - drops 3 shards
		// Signature Z-Crystal: 0% (Omni: 0.5%)
		// Held item: 11% (Omni: 10.5%)
		double megaThreshold = isOmni ? 0.06 : 0.04;
		double zThreshold = megaThreshold + (isOmni ? 0.06 : 0.04);
		double teraThreshold = zThreshold + (isOmni ? 0.02 : 0.01);
		double sigZThreshold = isOmni ? (teraThreshold + 0.005) : teraThreshold;
		double heldThreshold = sigZThreshold + (isOmni ? 0.105 : 0.11);

		if (guaranteedRelic)
		{
			roll = random.NextDouble() * zThreshold; // Force either Mega Stone or Z-Crystal
		}

		if (roll < megaThreshold)
		{
			// Mega Stone from affinity
			string stoneId = GetAffinityMegaStone(affinities, random);
			if (!string.IsNullOrEmpty(stoneId))
			{
				ResetPity();
				return ItemRegistry.Create("(O)" + stoneId);
			}
		}
		else if (roll < zThreshold)
		{
			// Z-Crystal from affinity
			string zId = GetAffinityZCrystal(affinities, random);
			if (!string.IsNullOrEmpty(zId))
			{
				ResetPity();
				return ItemRegistry.Create("(O)" + zId);
			}
		}
		else if (roll < teraThreshold)
		{
			// Tera Shards (stack of 3)
			string shardId = GetAffinityTeraShard(affinities, random);
			if (!string.IsNullOrEmpty(shardId))
			{
				ResetPity();
				return ItemRegistry.Create("(O)" + shardId, 3);
			}
		}
		else if (roll < sigZThreshold && isOmni)
		{
			// Signature Z-Crystal
			string sigZId = GetRandomSignatureZCrystal(random);
			if (!string.IsNullOrEmpty(sigZId))
			{
				ResetPity();
				return ItemRegistry.Create("(O)" + sigZId);
			}
		}
		else if (roll < heldThreshold)
		{
			string heldId = PokemonHeldItemManager.GetRandomDigDrop(random);
			if (!string.IsNullOrEmpty(heldId))
			{
				return ItemRegistry.Create("(O)" + heldId);
			}
		}

		return null;
	}

	private static void ResetPity()
	{
		if (Game1.player != null && Game1.player.modData != null)
		{
			Game1.player.modData["poke_relicPity"] = "0";
		}
	}

	public static string GetAffinityMegaStone(List<PokemonType> affinities, Random random)
	{
		var candidates = new List<string>();
		foreach (var kv in PokemonItemManager.MegaStoneDefinitions)
		{
			string stoneId = kv.Key;
			string targetMon = kv.Value.DisplayName.Replace("Pedra Mega ", "").Replace("ite X", " X").Replace("ite Y", " Y").Replace("ite", "").Replace("nite", "").Trim();
			var spec = PokemonSpecies.Get(targetMon);
			if (spec != null)
			{
				if (affinities.Contains(spec.PrimaryType) || (spec.SecondaryType.HasValue && affinities.Contains(spec.SecondaryType.Value)))
				{
					// Anti-repetition: if already in inventory, add with 25% weight
					bool hasAlready = Game1.player?.Items != null && (Game1.player.Items.ContainsId(stoneId) || Game1.player.Items.ContainsId("(O)" + stoneId));
					if (!hasAlready || random.NextDouble() < 0.25)
					{
						candidates.Add(stoneId);
					}
				}
			}
			else
			{
				candidates.Add(stoneId);
			}
		}

		if (candidates.Count > 0)
		{
			return candidates[random.Next(candidates.Count)];
		}
		return "Poke_MegaBracelet";
	}

	public static string GetAffinityZCrystal(List<PokemonType> affinities, Random random)
	{
		var candidates = new List<string>();
		foreach (var kv in PokemonItemManager.ZCrystalDefinitions)
		{
			if (affinities.Contains(kv.Value.Type))
			{
				candidates.Add(kv.Key);
			}
		}
		if (candidates.Count > 0)
		{
			return candidates[random.Next(candidates.Count)];
		}
		return "Poke_NormaliumZ";
	}

	public static string GetAffinityTeraShard(List<PokemonType> affinities, Random random)
	{
		if (affinities.Count == 0) return "Poke_TeraShard_Normal";
		PokemonType chosen = affinities[random.Next(affinities.Count)];
		return $"Poke_TeraShard_{chosen}";
	}

	public static string GetRandomSignatureZCrystal(Random random)
	{
		var list = new List<string>(PokemonItemManager.SignatureZCrystalDefinitions.Keys);
		if (list.Count > 0)
		{
			return list[random.Next(list.Count)];
		}
		return null;
	}

	public static string GetArtifactSpotRelic(GameLocation location, Random random)
	{
		if (location == null || random == null) return null;
		string loc = location.NameOrUniqueName ?? "";

		var affinities = new List<PokemonType>();
		if (location is MineShaft || loc.Contains("Mine") || loc.Contains("Quarry") || loc.Contains("Mountain"))
		{
			affinities.AddRange(new[] { PokemonType.Rock, PokemonType.Ground, PokemonType.Steel });
		}
		else if (loc.Contains("Beach") || loc.Contains("IslandSouth") || loc.Contains("IslandEast"))
		{
			affinities.AddRange(new[] { PokemonType.Water, PokemonType.Ice });
		}
		else if (loc.Contains("Woods") || loc.Contains("Forest"))
		{
			affinities.AddRange(new[] { PokemonType.Grass, PokemonType.Bug, PokemonType.Fairy });
		}
		else if (loc.Contains("Desert") || loc.Contains("Volcano") || loc.Contains("Caldera"))
		{
			affinities.AddRange(new[] { PokemonType.Fire, PokemonType.Dragon });
		}
		else
		{
			affinities.AddRange(new[] { PokemonType.Normal, PokemonType.Flying });
		}

		double roll = random.NextDouble();
		if (roll < 0.10)
		{
			return GetAffinityMegaStone(affinities, random);
		}
		else if (roll < 0.20)
		{
			return GetAffinityZCrystal(affinities, random);
		}
		else if (roll < 0.25)
		{
			return GetAffinityTeraShard(affinities, random);
		}

		return PokemonHeldItemManager.GetRandomDigDrop(random);
	}
}
