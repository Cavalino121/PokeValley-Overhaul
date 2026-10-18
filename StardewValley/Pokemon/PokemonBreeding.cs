using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StardewValley.Pokemon;

public static class PokemonBreeding
{
	public static PokemonInstance DaycareParent1 { get; set; }
	public static PokemonInstance DaycareParent2 { get; set; }
	public static PokemonInstance ReadyEgg { get; set; }

	// Unbreedable legendaries / mythical
	private static readonly HashSet<string> UndiscoveredGroup = new(StringComparer.OrdinalIgnoreCase)
	{
		"Mewtwo", "Mew", "Articuno", "Zapdos", "Moltres", "Raikou", "Entei", "Suicune",
		"Lugia", "Ho-Oh", "Celebi", "Regirock", "Regice", "Registeel", "Latias", "Latios",
		"Kyogre", "Groudon", "Rayquaza", "Jirachi", "Deoxys", "Uxie", "Mesprit", "Azelf",
		"Dialga", "Palkia", "Heatran", "Regigigas", "Giratina", "Cresselia", "Darkrai",
		"Shaymin", "Arceus"
	};

	// Base / Baby species mapping (evolution backwards mapping)
	private static readonly Dictionary<string, string> BaseSpeciesMap = new(StringComparer.OrdinalIgnoreCase)
	{
		{ "Charizard", "Charmander" }, { "Charmeleon", "Charmander" }, { "Charmander", "Charmander" },
		{ "Blastoise", "Squirtle" }, { "Wartortle", "Squirtle" }, { "Squirtle", "Squirtle" },
		{ "Venusaur", "Bulbasaur" }, { "Ivysaur", "Bulbasaur" }, { "Bulbasaur", "Bulbasaur" },
		{ "Raichu", "Pikachu" }, { "Pikachu", "Pikachu" }, { "Pichu", "Pichu" },
		{ "Gyarados", "Magikarp" }, { "Magikarp", "Magikarp" },
		{ "Blaziken", "Torchic" }, { "Combusken", "Torchic" }, { "Torchic", "Torchic" },
		{ "Sceptile", "Treecko" }, { "Grovyle", "Treecko" }, { "Treecko", "Treecko" },
		{ "Swampert", "Mudkip" }, { "Marshtomp", "Mudkip" }, { "Mudkip", "Mudkip" },
		{ "Torterra", "Turtwig" }, { "Grotle", "Turtwig" }, { "Turtwig", "Turtwig" },
		{ "Infernape", "Chimchar" }, { "Monferno", "Chimchar" }, { "Chimchar", "Chimchar" },
		{ "Empoleon", "Piplup" }, { "Prinplup", "Piplup" }, { "Piplup", "Piplup" },
		{ "Lucario", "Riolu" }, { "Riolu", "Riolu" },
		{ "Gengar", "Gastly" }, { "Haunter", "Gastly" }, { "Gastly", "Gastly" },
		{ "Alakazam", "Abra" }, { "Kadabra", "Abra" }, { "Abra", "Abra" },
		{ "Machamp", "Machop" }, { "Machoke", "Machop" }, { "Machop", "Machop" },
		{ "Golem", "Geodude" }, { "Graveler", "Geodude" }, { "Geodude", "Geodude" },
		{ "Dragonite", "Dratini" }, { "Dragonair", "Dratini" }, { "Dratini", "Dratini" },
		{ "Tyranitar", "Larvitar" }, { "Pupitar", "Larvitar" }, { "Larvitar", "Larvitar" },
		{ "Salamence", "Bagon" }, { "Shelgon", "Bagon" }, { "Bagon", "Bagon" },
		{ "Metagross", "Beldum" }, { "Metang", "Beldum" }, { "Beldum", "Beldum" },
		{ "Garchomp", "Gible" }, { "Gabite", "Gible" }, { "Gible", "Gible" },
		{ "Umbreon", "Eevee" }, { "Espeon", "Eevee" }, { "Vaporeon", "Eevee" }, { "Jolteon", "Eevee" }, { "Flareon", "Eevee" }, { "Eevee", "Eevee" }
	};

	public static bool CanBreed(PokemonInstance a, PokemonInstance b)
	{
		if (a == null || b == null) return false;
		if (a.IsEgg || b.IsEgg) return false;

		string nameA = a.Species.Name;
		string nameB = b.Species.Name;

		if (UndiscoveredGroup.Contains(nameA) || UndiscoveredGroup.Contains(nameB))
			return false;

		// Ditto can breed with any non-legendary
		if (string.Equals(nameA, "Ditto", StringComparison.OrdinalIgnoreCase) || string.Equals(nameB, "Ditto", StringComparison.OrdinalIgnoreCase))
			return true;

		// Same base family can breed
		string baseA = GetBaseSpecies(nameA);
		string baseB = GetBaseSpecies(nameB);
		if (string.Equals(baseA, baseB, StringComparison.OrdinalIgnoreCase))
			return true;

		// Shared primary or secondary typing creates compatible egg groups
		if (a.Species.PrimaryType == b.Species.PrimaryType ||
			(b.Species.SecondaryType.HasValue && a.Species.PrimaryType == b.Species.SecondaryType.Value) ||
			(a.Species.SecondaryType.HasValue && a.Species.SecondaryType.Value == b.Species.PrimaryType))
		{
			return true;
		}

		return false;
	}

	public static string GetBaseSpecies(string name)
	{
		if (BaseSpeciesMap.TryGetValue(name, out var b))
			return b;
		return name;
	}

	public static PokemonInstance ProduceEgg(PokemonInstance parent1, PokemonInstance parent2)
	{
		if (!CanBreed(parent1, parent2)) return null;

		// Determine child base species
		string childSpeciesName;
		if (string.Equals(parent1.Species.Name, "Ditto", StringComparison.OrdinalIgnoreCase))
		{
			childSpeciesName = GetBaseSpecies(parent2.Species.Name);
		}
		else if (string.Equals(parent2.Species.Name, "Ditto", StringComparison.OrdinalIgnoreCase))
		{
			childSpeciesName = GetBaseSpecies(parent1.Species.Name);
		}
		else
		{
			// Mother determines species (or parent1)
			childSpeciesName = GetBaseSpecies(parent1.Species.Name);
		}

		var childSpecies = PokemonSpecies.Get(childSpeciesName);
		var egg = new PokemonInstance(childSpecies, 1)
		{
			IsEgg = true,
			EggSteps = 150, // 150 player steps in Stardew Valley
			Nickname = "Ovo Pokémon"
		};

		// 1. Inherit IVs (Destiny Knot passes 5 IVs, Power items pass specific IV, standard passes 3 IVs)
		bool hasDestinyKnot = (parent1.HeldItem != null && (parent1.HeldItem.Equals("DestinyKnot", StringComparison.OrdinalIgnoreCase) || parent1.HeldItem.Equals("Poke_DestinyKnot", StringComparison.OrdinalIgnoreCase))) ||
		                      (parent2.HeldItem != null && (parent2.HeldItem.Equals("DestinyKnot", StringComparison.OrdinalIgnoreCase) || parent2.HeldItem.Equals("Poke_DestinyKnot", StringComparison.OrdinalIgnoreCase)));

		int ivCountToInherit = hasDestinyKnot ? 5 : 3;

		// Power items check (Power Weight, Bracer, Belt, Lens, Band, Anklet)
		var powerItemStats = new Dictionary<PokemonStat, PokemonInstance>();
		void CheckPowerItem(PokemonInstance p)
		{
			if (string.IsNullOrEmpty(p?.HeldItem)) return;
			string h = p.HeldItem.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase) ? p.HeldItem.Substring(5) : p.HeldItem;
			switch (h.ToLowerInvariant())
			{
				case "powerweight": powerItemStats[PokemonStat.HP] = p; break;
				case "powerbracer": powerItemStats[PokemonStat.Attack] = p; break;
				case "powerbelt": powerItemStats[PokemonStat.Defense] = p; break;
				case "powerlens": powerItemStats[PokemonStat.SpAttack] = p; break;
				case "powerband": powerItemStats[PokemonStat.SpDefense] = p; break;
				case "poweranklet": powerItemStats[PokemonStat.Speed] = p; break;
			}
		}
		CheckPowerItem(parent1);
		CheckPowerItem(parent2);

		var inheritedStats = new HashSet<PokemonStat>();
		foreach (var kv in powerItemStats)
		{
			if (inheritedStats.Count < ivCountToInherit)
			{
				SetStatIV(egg, kv.Key, GetStatIV(kv.Value, kv.Key));
				inheritedStats.Add(kv.Key);
			}
		}

		var remainingStats = new List<PokemonStat>
		{
			PokemonStat.HP, PokemonStat.Attack, PokemonStat.Defense,
			PokemonStat.SpAttack, PokemonStat.SpDefense, PokemonStat.Speed
		};
		remainingStats.RemoveAll(s => inheritedStats.Contains(s));

		// Shuffle remaining stats
		for (int i = 0; i < remainingStats.Count; i++)
		{
			int r = Game1.random.Next(remainingStats.Count);
			(remainingStats[i], remainingStats[r]) = (remainingStats[r], remainingStats[i]);
		}

		int needed = ivCountToInherit - inheritedStats.Count;
		for (int i = 0; i < needed && i < remainingStats.Count; i++)
		{
			PokemonStat statToInherit = remainingStats[i];
			PokemonInstance chosenParent = Game1.random.Next(2) == 0 ? parent1 : parent2;
			SetStatIV(egg, statToInherit, GetStatIV(chosenParent, statToInherit));
		}

		// 2. Nature inheritance: Everstone guarantees 100% nature inheritance
		bool p1Everstone = parent1.HeldItem != null && (parent1.HeldItem.Equals("Everstone", StringComparison.OrdinalIgnoreCase) || parent1.HeldItem.Equals("Poke_Everstone", StringComparison.OrdinalIgnoreCase));
		bool p2Everstone = parent2.HeldItem != null && (parent2.HeldItem.Equals("Everstone", StringComparison.OrdinalIgnoreCase) || parent2.HeldItem.Equals("Poke_Everstone", StringComparison.OrdinalIgnoreCase));

		if (p1Everstone && p2Everstone)
		{
			egg.Nature = Game1.random.Next(2) == 0 ? parent1.Nature : parent2.Nature;
		}
		else if (p1Everstone)
		{
			egg.Nature = parent1.Nature;
		}
		else if (p2Everstone)
		{
			egg.Nature = parent2.Nature;
		}
		else
		{
			int natRoll = Game1.random.Next(3);
			if (natRoll == 0) egg.Nature = parent1.Nature;
			else if (natRoll == 1) egg.Nature = parent2.Nature;
			else egg.Nature = (PokemonNature)Game1.random.Next(25);
		}

		// 3. Inherit moves from parents if applicable
		foreach (var move in parent1.Moves)
		{
			if (egg.Moves.Count < 4 && !egg.Moves.Exists(m => m.Name == move.Name))
			{
				egg.Moves.Add(move.Clone());
			}
		}
		foreach (var move in parent2.Moves)
		{
			if (egg.Moves.Count < 4 && !egg.Moves.Exists(m => m.Name == move.Name))
			{
				egg.Moves.Add(move.Clone());
			}
		}

		egg.RecalculateStats();
		egg.CurrentHP = egg.MaxHP;
		return egg;
	}

	public static void OnDayStarted()
	{
		// If parents in Daycare and no egg waiting, 65% chance to generate an egg
		if (DaycareParent1 != null && DaycareParent2 != null && ReadyEgg == null)
		{
			if (CanBreed(DaycareParent1, DaycareParent2) && Game1.random.NextDouble() < 0.65)
			{
				ReadyEgg = ProduceEgg(DaycareParent1, DaycareParent2);
				Game1.addHUDMessage(new HUDMessage("Um Ovo Pokémon foi encontrado no Daycare!", 2));
			}
		}
	}

	private static readonly Dictionary<string, PokemonInstance> _incubatingEggs = new(StringComparer.OrdinalIgnoreCase);

	private static string KeyForTile(Vector2 tile) => $"{(int)tile.X}_{(int)tile.Y}";

	public static void AddIncubatingEgg(Vector2 tile, PokemonInstance egg)
	{
		if (egg != null) _incubatingEggs[KeyForTile(tile)] = egg;
	}

	public static bool HasIncubatingEgg(Vector2 tile) => _incubatingEggs.ContainsKey(KeyForTile(tile));

	public static PokemonInstance GetIncubatingEgg(Vector2 tile)
	{
		string key = KeyForTile(tile);
		return _incubatingEggs.TryGetValue(key, out var egg) ? egg : null;
	}

	public static bool RemoveIncubatingEgg(Vector2 tile) => _incubatingEggs.Remove(KeyForTile(tile));

	private static int _stepCounter = 0;

	public static void OnStepTaken()
	{
		// Decrease egg steps for all eggs in player party
		if (PlayerParty.Instance?.Team == null) return;
		if (Game1.activeClickableMenu is PokemonHatchMenu) return;

		for (int i = 0; i < PlayerParty.Instance.Team.Count; i++)
		{
			var p = PlayerParty.Instance.Team[i];
			if (p != null && p.IsEgg)
			{
				p.EggSteps--;
				if (p.EggSteps <= 0)
				{
					// Sentinel to prevent multiple triggers
					p.EggSteps = 99999;
					// Trigger hatching animation!
					Game1.activeClickableMenu = new PokemonHatchMenu(p);
					return;
				}
			}
		}

		// Field damage for Poison and Burn every 4 steps
		_stepCounter++;
		if (_stepCounter >= 4)
		{
			_stepCounter = 0;
			bool tookDamage = false;
			bool isPoison = false;

			for (int i = 0; i < PlayerParty.Instance.Team.Count; i++)
			{
				var p = PlayerParty.Instance.Team[i];
				if (p == null || p.IsEgg || p.IsFainted) continue;

				if (p.Status == PokemonStatusCondition.Poison || p.Status == PokemonStatusCondition.Toxic || p.Status == PokemonStatusCondition.Burn)
				{
					if (p.CurrentHP > 1)
					{
						p.CurrentHP = Math.Max(1, p.CurrentHP - 1);
						tookDamage = true;
						if (p.Status == PokemonStatusCondition.Poison || p.Status == PokemonStatusCondition.Toxic)
						{
							isPoison = true;
						}
					}
				}
			}

			if (tookDamage)
			{
				Game1.playSound("croak");
				Game1.flashAlpha = 0.35f;
				if (isPoison)
				{
					Game1.screenGlowColor = new Color(180, 50, 220, 80);
					Game1.screenGlowAlpha = 0.4f;
					Game1.screenGlow = true;
				}
				else
				{
					Game1.screenGlowColor = new Color(255, 60, 20, 80);
					Game1.screenGlowAlpha = 0.4f;
					Game1.screenGlow = true;
				}
			}
		}
	}

	private static int GetStatIV(PokemonInstance mon, PokemonStat stat) => stat switch
	{
		PokemonStat.HP => mon.IV_HP,
		PokemonStat.Attack => mon.IV_Attack,
		PokemonStat.Defense => mon.IV_Defense,
		PokemonStat.SpAttack => mon.IV_SpAttack,
		PokemonStat.SpDefense => mon.IV_SpDefense,
		PokemonStat.Speed => mon.IV_Speed,
		_ => 31
	};

	private static void SetStatIV(PokemonInstance mon, PokemonStat stat, int val)
	{
		switch (stat)
		{
			case PokemonStat.HP: mon.IV_HP = val; break;
			case PokemonStat.Attack: mon.IV_Attack = val; break;
			case PokemonStat.Defense: mon.IV_Defense = val; break;
			case PokemonStat.SpAttack: mon.IV_SpAttack = val; break;
			case PokemonStat.SpDefense: mon.IV_SpDefense = val; break;
			case PokemonStat.Speed: mon.IV_Speed = val; break;
		}
	}
}
