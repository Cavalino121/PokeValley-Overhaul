using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace StardewValley.Pokemon;

public enum EvolutionTrigger
{
	LevelUp,
	UseItem,
	Trade,
	Friendship,
	FriendshipDay,
	FriendshipNight
}

public class EvolutionData
{
	public string SourceSpecies { get; set; }
	public string TargetSpecies { get; set; }
	public EvolutionTrigger Trigger { get; set; }
	public int Level { get; set; }
	public string Item { get; set; }
	public string Move { get; set; }
}

public static class PokemonEvolution
{
	private static readonly Dictionary<string, List<EvolutionData>> _evolutions = new(StringComparer.OrdinalIgnoreCase);
	private static bool _initialized = false;

	public static void EnsureInitialized()
	{
		if (_initialized) return;
		_initialized = true;

		// 1. Register canonical hardcoded fallback rules (covers all starters, trade evos, stone evos)
		RegisterBuiltInEvolutions();

		// 2. Scan pokeplatinum/res/pokemon/*/data.json to dynamically load any missing decompiled rules
		LoadFromDecompilation();
	}

	public static EvolutionData CheckEvolution(PokemonInstance mon, EvolutionTrigger trigger, string itemOrArg = null)
	{
		EnsureInitialized();
		if (mon == null || mon.Species == null) return null;

		string species = mon.Species.InternalName;
		if (!_evolutions.TryGetValue(species, out var list) || list == null) return null;

		// Everstone blocks evolution (LevelUp, Friendship, Trade, etc.) except manual direct Item usage
		if (!string.IsNullOrEmpty(mon.HeldItem) &&
		    (mon.HeldItem.Equals("Everstone", StringComparison.OrdinalIgnoreCase) ||
		     mon.HeldItem.Equals("Poke_Everstone", StringComparison.OrdinalIgnoreCase)))
		{
			if (trigger != EvolutionTrigger.UseItem)
				return null;
		}

		if (trigger == EvolutionTrigger.LevelUp || trigger == EvolutionTrigger.Friendship || trigger == EvolutionTrigger.FriendshipDay || trigger == EvolutionTrigger.FriendshipNight)
		{
			// Sort so lowest qualifying level evolution is tested first (e.g. Torchic -> Combusken at 16, not skipping)
			var candidates = new List<EvolutionData>(list);
			candidates.Sort((a, b) => a.Level.CompareTo(b.Level));

			bool isDay = Game1.timeOfDay >= 600 && Game1.timeOfDay < 1800 && (Game1.currentLocation == null || !Game1.isStartingToGetDarkOut(Game1.currentLocation));
			bool isNight = !isDay;

			foreach (var evo in candidates)
			{
				if (evo.Trigger == EvolutionTrigger.LevelUp)
				{
					if (trigger == EvolutionTrigger.LevelUp && mon.Level >= evo.Level)
					{
						if (!string.IsNullOrEmpty(evo.Move))
						{
							if (mon.Moves.Exists(m => string.Equals(m.Name, evo.Move, StringComparison.OrdinalIgnoreCase)))
								return evo;
						}
						else
						{
							return evo;
						}
					}
				}
				else if (evo.Trigger == EvolutionTrigger.Friendship && mon.Friendship >= 220)
				{
					if (!string.IsNullOrEmpty(evo.Move))
					{
						if (mon.Moves.Exists(m => string.Equals(m.Name, evo.Move, StringComparison.OrdinalIgnoreCase)))
							return evo;
					}
					else
					{
						return evo;
					}
				}
				else if (evo.Trigger == EvolutionTrigger.FriendshipDay && mon.Friendship >= 220 && isDay)
				{
					return evo;
				}
				else if (evo.Trigger == EvolutionTrigger.FriendshipNight && mon.Friendship >= 220 && isNight)
				{
					return evo;
				}
			}
			return null;
		}

		foreach (var evo in list)
		{
			if (trigger == EvolutionTrigger.UseItem)
			{
				if (evo.Trigger == EvolutionTrigger.UseItem && string.Equals(evo.Item, itemOrArg, StringComparison.OrdinalIgnoreCase))
					return evo;

				// Generic Link Cable triggers Trade Evolutions as well!
				string cleanItem = itemOrArg ?? "";
				if (cleanItem.StartsWith("(O)")) cleanItem = cleanItem.Substring(3);
				if (cleanItem.StartsWith("Poke_")) cleanItem = cleanItem.Substring(5);
				string normItem = cleanItem.Replace("_", "").Replace("-", "").Replace(" ", "");

				if (string.Equals(normItem, "PokemonLinkCable", StringComparison.OrdinalIgnoreCase) ||
				    string.Equals(normItem, "LinkCable", StringComparison.OrdinalIgnoreCase) ||
				    string.Equals(normItem, "LinkingCord", StringComparison.OrdinalIgnoreCase) ||
				    string.Equals(normItem, "PokemonLinkingCord", StringComparison.OrdinalIgnoreCase) ||
				    string.Equals(normItem, "LinkCord", StringComparison.OrdinalIgnoreCase) ||
				    string.Equals(normItem, "CaboDeLigacao", StringComparison.OrdinalIgnoreCase))
				{
					if (evo.Trigger == EvolutionTrigger.Trade)
						return evo;
				}

				// Direct trade item use (single-player support for item-holding trade evolutions)
				if (evo.Trigger == EvolutionTrigger.Trade)
				{
					string src = evo.SourceSpecies?.ToLowerInvariant() ?? "";
					if ((src == "onix" || src == "scyther") && string.Equals(normItem, "MetalCoat", StringComparison.OrdinalIgnoreCase)) return evo;
					if (src == "rhydon" && string.Equals(normItem, "Protector", StringComparison.OrdinalIgnoreCase)) return evo;
					if (src == "electabuzz" && string.Equals(normItem, "Electirizer", StringComparison.OrdinalIgnoreCase)) return evo;
					if (src == "magmar" && string.Equals(normItem, "Magmarizer", StringComparison.OrdinalIgnoreCase)) return evo;
					if (src == "porygon" && string.Equals(normItem, "UpGrade", StringComparison.OrdinalIgnoreCase)) return evo;
					if (src == "porygon2" && string.Equals(normItem, "DubiousDisc", StringComparison.OrdinalIgnoreCase)) return evo;
					if (src == "seadra" && string.Equals(normItem, "DragonScale", StringComparison.OrdinalIgnoreCase)) return evo;
					if ((src == "slowpoke" || src == "poliwhirl") && string.Equals(normItem, "KingsRock", StringComparison.OrdinalIgnoreCase)) return evo;
					if (src == "dusclops" && string.Equals(normItem, "ReaperCloth", StringComparison.OrdinalIgnoreCase)) return evo;
				}
			}
			else if (trigger == EvolutionTrigger.Trade && evo.Trigger == EvolutionTrigger.Trade)
			{
				return evo;
			}
		}

		return null;
	}

	public static void Evolve(PokemonInstance mon, string targetSpeciesName)
	{
		if (mon == null) return;
		PokemonSpecies targetSpecies = PokemonSpecies.Get(targetSpeciesName);
		if (targetSpecies == null) return;

		bool updateNickname = string.Equals(mon.Nickname, mon.Species.Name, StringComparison.OrdinalIgnoreCase);
		mon.Species = targetSpecies;
		if (updateNickname) mon.Nickname = targetSpecies.Name;

		mon.RecalculateStats();
		mon.InvalidateSpriteCache();
		mon.CurrentHP = Math.Min(mon.MaxHP, mon.CurrentHP + 15);
		PlayerParty.Instance.SaveParty();
	}

	private static void Register(string source, string target, EvolutionTrigger trigger, int level = 0, string item = null, string move = null)
	{
		if (!_evolutions.TryGetValue(source, out var list))
		{
			list = new List<EvolutionData>();
			_evolutions[source] = list;
		}
		list.Add(new EvolutionData
		{
			SourceSpecies = source,
			TargetSpecies = target,
			Trigger = trigger,
			Level = level,
			Item = item,
			Move = move
		});
	}

	private static void RegisterBuiltInEvolutions()
	{
		// Gen 1 Starters
		Register("bulbasaur", "ivysaur", EvolutionTrigger.LevelUp, 16);
		Register("ivysaur", "venusaur", EvolutionTrigger.LevelUp, 32);
		Register("charmander", "charmeleon", EvolutionTrigger.LevelUp, 16);
		Register("charmeleon", "charizard", EvolutionTrigger.LevelUp, 36);
		Register("squirtle", "wartortle", EvolutionTrigger.LevelUp, 16);
		Register("wartortle", "blastoise", EvolutionTrigger.LevelUp, 36);

		// Bugs & Birds
		Register("caterpie", "metapod", EvolutionTrigger.LevelUp, 7);
		Register("metapod", "butterfree", EvolutionTrigger.LevelUp, 10);
		Register("weedle", "kakuna", EvolutionTrigger.LevelUp, 7);
		Register("kakuna", "beedrill", EvolutionTrigger.LevelUp, 10);
		Register("pidgey", "pidgeotto", EvolutionTrigger.LevelUp, 18);
		Register("pidgeotto", "pidgeot", EvolutionTrigger.LevelUp, 36);
		Register("rattata", "raticate", EvolutionTrigger.LevelUp, 20);
		Register("spearow", "fearow", EvolutionTrigger.LevelUp, 20);
		Register("ekans", "arbok", EvolutionTrigger.LevelUp, 22);
		Register("sandshrew", "sandslash", EvolutionTrigger.LevelUp, 22);

		// Trade Evolutions (Classic 4 + Gen 2-4)
		Register("abra", "kadabra", EvolutionTrigger.LevelUp, 16);
		Register("kadabra", "alakazam", EvolutionTrigger.Trade);
		Register("machop", "machoke", EvolutionTrigger.LevelUp, 28);
		Register("machoke", "machamp", EvolutionTrigger.Trade);
		Register("geodude", "graveler", EvolutionTrigger.LevelUp, 25);
		Register("graveler", "golem", EvolutionTrigger.Trade);
		Register("gastly", "haunter", EvolutionTrigger.LevelUp, 25);
		Register("haunter", "gengar", EvolutionTrigger.Trade);
		Register("onix", "steelix", EvolutionTrigger.Trade);
		Register("scyther", "scizor", EvolutionTrigger.Trade);
		Register("rhydon", "rhyperior", EvolutionTrigger.Trade);
		Register("electabuzz", "electivire", EvolutionTrigger.Trade);
		Register("magmar", "magmortar", EvolutionTrigger.Trade);
		Register("porygon", "porygon2", EvolutionTrigger.Trade);
		Register("porygon2", "porygon-z", EvolutionTrigger.Trade);

		// Stone Evolutions
		Register("pikachu", "raichu", EvolutionTrigger.UseItem, item: "PokemonThunderStone");
		Register("vulpix", "ninetales", EvolutionTrigger.UseItem, item: "PokemonFireStone");
		Register("growlithe", "arcanine", EvolutionTrigger.UseItem, item: "PokemonFireStone");
		Register("poliwag", "poliwhirl", EvolutionTrigger.LevelUp, 25);
		Register("poliwhirl", "poliwrath", EvolutionTrigger.UseItem, item: "PokemonWaterStone");
		Register("nidoran_f", "nidorina", EvolutionTrigger.LevelUp, 16);
		Register("nidorina", "nidoqueen", EvolutionTrigger.UseItem, item: "PokemonMoonStone");
		Register("nidoran_m", "nidorino", EvolutionTrigger.LevelUp, 16);
		Register("nidorino", "nidoking", EvolutionTrigger.UseItem, item: "PokemonMoonStone");
		Register("clefairy", "clefable", EvolutionTrigger.UseItem, item: "PokemonMoonStone");
		Register("jigglypuff", "wigglytuff", EvolutionTrigger.UseItem, item: "PokemonMoonStone");
		Register("gloom", "vileplume", EvolutionTrigger.UseItem, item: "PokemonLeafStone");
		Register("gloom", "bellossom", EvolutionTrigger.UseItem, item: "PokemonSunStone");
		Register("weepinbell", "victreebel", EvolutionTrigger.UseItem, item: "PokemonLeafStone");
		Register("shellder", "cloyster", EvolutionTrigger.UseItem, item: "PokemonWaterStone");
		Register("staryu", "starmie", EvolutionTrigger.UseItem, item: "PokemonWaterStone");
		Register("exeggcute", "exeggutor", EvolutionTrigger.UseItem, item: "PokemonLeafStone");

		// Eeveelutions (Canonic Friendship and Stones)
		Register("eevee", "vaporeon", EvolutionTrigger.UseItem, item: "PokemonWaterStone");
		Register("eevee", "jolteon", EvolutionTrigger.UseItem, item: "PokemonThunderStone");
		Register("eevee", "flareon", EvolutionTrigger.UseItem, item: "PokemonFireStone");
		Register("eevee", "espeon", EvolutionTrigger.FriendshipDay);
		Register("eevee", "espeon", EvolutionTrigger.UseItem, item: "PokemonSunStone");
		Register("eevee", "umbreon", EvolutionTrigger.FriendshipNight);
		Register("eevee", "umbreon", EvolutionTrigger.UseItem, item: "PokemonMoonStone");
		Register("eevee", "leafeon", EvolutionTrigger.UseItem, item: "PokemonLeafStone");
		Register("eevee", "glaceon", EvolutionTrigger.UseItem, item: "PokemonIceStone");
		Register("eevee", "glaceon", EvolutionTrigger.UseItem, item: "PokemonDawnStone");
		Register("eevee", "sylveon", EvolutionTrigger.Friendship, move: "Baby-Doll Eyes");
		Register("eevee", "sylveon", EvolutionTrigger.Friendship, move: "Charm");
		Register("eevee", "sylveon", EvolutionTrigger.Friendship, move: "Disarming Voice");

		// Canonical Friendship Evolutions
		Register("riolu", "lucario", EvolutionTrigger.FriendshipDay);
		Register("budew", "roselia", EvolutionTrigger.FriendshipDay);
		Register("chingling", "chimecho", EvolutionTrigger.FriendshipNight);
		Register("pichu", "pikachu", EvolutionTrigger.Friendship);
		Register("cleffa", "clefairy", EvolutionTrigger.Friendship);
		Register("igglybuff", "jigglypuff", EvolutionTrigger.Friendship);
		Register("togepi", "togetic", EvolutionTrigger.Friendship);
		Register("azurill", "marill", EvolutionTrigger.Friendship);
		Register("buneary", "lopunny", EvolutionTrigger.Friendship);
		Register("golbat", "crobat", EvolutionTrigger.Friendship);
		Register("chansey", "blissey", EvolutionTrigger.Friendship);
		Register("munchlax", "snorlax", EvolutionTrigger.Friendship);
		Register("snom", "frosmoth", EvolutionTrigger.FriendshipNight);

		// Gen 2 Starters
		Register("chikorita", "bayleef", EvolutionTrigger.LevelUp, 16);
		Register("bayleef", "meganium", EvolutionTrigger.LevelUp, 32);
		Register("cyndaquil", "quilava", EvolutionTrigger.LevelUp, 14);
		Register("quilava", "typhlosion", EvolutionTrigger.LevelUp, 36);
		Register("totodile", "croconaw", EvolutionTrigger.LevelUp, 18);
		Register("croconaw", "feraligatr", EvolutionTrigger.LevelUp, 30);

		// Gen 3 Starters
		Register("treecko", "grovyle", EvolutionTrigger.LevelUp, 16);
		Register("grovyle", "sceptile", EvolutionTrigger.LevelUp, 36);
		Register("torchic", "combusken", EvolutionTrigger.LevelUp, 16);
		Register("combusken", "blaziken", EvolutionTrigger.LevelUp, 36);
		Register("mudkip", "marshtomp", EvolutionTrigger.LevelUp, 16);
		Register("marshtomp", "swampert", EvolutionTrigger.LevelUp, 36);

		// Gen 4 Starters & Sinnoh
		Register("turtwig", "grotle", EvolutionTrigger.LevelUp, 18);
		Register("grotle", "torterra", EvolutionTrigger.LevelUp, 32);
		Register("chimchar", "monferno", EvolutionTrigger.LevelUp, 14);
		Register("monferno", "infernape", EvolutionTrigger.LevelUp, 36);
		Register("piplup", "prinplup", EvolutionTrigger.LevelUp, 16);
		Register("prinplup", "empoleon", EvolutionTrigger.LevelUp, 36);
		Register("starly", "staravia", EvolutionTrigger.LevelUp, 14);
		Register("staravia", "staraptor", EvolutionTrigger.LevelUp, 34);
		Register("shinx", "luxio", EvolutionTrigger.LevelUp, 15);
		Register("luxio", "luxray", EvolutionTrigger.LevelUp, 30);
		Register("gible", "gabite", EvolutionTrigger.LevelUp, 24);
		Register("gabite", "garchomp", EvolutionTrigger.LevelUp, 48);
		Register("magikarp", "gyarados", EvolutionTrigger.LevelUp, 20);
		Register("dratini", "dragonair", EvolutionTrigger.LevelUp, 30);
		Register("dragonair", "dragonite", EvolutionTrigger.LevelUp, 55);

		// Sinnoh Stones
		Register("roselia", "roserade", EvolutionTrigger.UseItem, item: "PokemonShinyStone");
		Register("togetic", "togekiss", EvolutionTrigger.UseItem, item: "PokemonShinyStone");
		Register("murkrow", "honchkrow", EvolutionTrigger.UseItem, item: "PokemonDuskStone");
		Register("misdreavus", "mismagius", EvolutionTrigger.UseItem, item: "PokemonDuskStone");
		Register("kirlia", "gallade", EvolutionTrigger.UseItem, item: "PokemonDawnStone");
		Register("snorunt", "froslass", EvolutionTrigger.UseItem, item: "PokemonDawnStone");
		// Gen 5-9 Evolutions from pokeemerald-expansion
		Register("snivy", "servine", EvolutionTrigger.LevelUp, 17);
		Register("servine", "serperior", EvolutionTrigger.LevelUp, 36);
		Register("tepig", "pignite", EvolutionTrigger.LevelUp, 17);
		Register("pignite", "emboar", EvolutionTrigger.LevelUp, 36);
		Register("oshawott", "dewott", EvolutionTrigger.LevelUp, 17);
		Register("dewott", "samurott", EvolutionTrigger.LevelUp, 36);
		Register("dewott", "samurott_hisui", EvolutionTrigger.LevelUp, 36);
		Register("patrat", "watchog", EvolutionTrigger.LevelUp, 20);
		Register("lillipup", "herdier", EvolutionTrigger.LevelUp, 16);
		Register("herdier", "stoutland", EvolutionTrigger.LevelUp, 32);
		Register("purrloin", "liepard", EvolutionTrigger.LevelUp, 20);
		Register("pansage", "simisage", EvolutionTrigger.UseItem, item: "PokemonLeafStone");
		Register("pansear", "simisear", EvolutionTrigger.UseItem, item: "PokemonFireStone");
		Register("panpour", "simipour", EvolutionTrigger.UseItem, item: "PokemonWaterStone");
		Register("munna", "musharna", EvolutionTrigger.UseItem, item: "PokemonMoonStone");
		Register("pidove", "tranquill", EvolutionTrigger.LevelUp, 21);
		Register("tranquill", "unfezant", EvolutionTrigger.LevelUp, 32);
		Register("blitzle", "zebstrika", EvolutionTrigger.LevelUp, 27);
		Register("roggenrola", "boldore", EvolutionTrigger.LevelUp, 25);
		Register("boldore", "gigalith", EvolutionTrigger.Trade);
		Register("woobat", "swoobat", EvolutionTrigger.LevelUp, 0);
		Register("drilbur", "excadrill", EvolutionTrigger.LevelUp, 31);
		Register("timburr", "gurdurr", EvolutionTrigger.LevelUp, 25);
		Register("gurdurr", "conkeldurr", EvolutionTrigger.Trade);
		Register("tympole", "palpitoad", EvolutionTrigger.LevelUp, 25);
		Register("palpitoad", "seismitoad", EvolutionTrigger.LevelUp, 36);
		Register("sewaddle", "swadloon", EvolutionTrigger.LevelUp, 20);
		Register("swadloon", "leavanny", EvolutionTrigger.LevelUp, 0);
		Register("venipede", "whirlipede", EvolutionTrigger.LevelUp, 22);
		Register("whirlipede", "scolipede", EvolutionTrigger.LevelUp, 30);
		Register("cottonee", "whimsicott", EvolutionTrigger.UseItem, item: "PokemonSunStone");
		Register("petilil", "lilligant", EvolutionTrigger.UseItem, item: "PokemonSunStone");
		Register("petilil", "lilligant_hisui", EvolutionTrigger.UseItem, item: "PokemonSunStone");
		Register("sandile", "krokorok", EvolutionTrigger.LevelUp, 29);
		Register("krokorok", "krookodile", EvolutionTrigger.LevelUp, 40);
		Register("darumaka", "darmanitan_standard", EvolutionTrigger.LevelUp, 35);
		Register("dwebble", "crustle", EvolutionTrigger.LevelUp, 34);
		Register("scraggy", "scrafty", EvolutionTrigger.LevelUp, 39);
		Register("yamask", "cofagrigus", EvolutionTrigger.LevelUp, 34);
		Register("tirtouga", "carracosta", EvolutionTrigger.LevelUp, 37);
		Register("archen", "archeops", EvolutionTrigger.LevelUp, 37);
		Register("trubbish", "garbodor", EvolutionTrigger.LevelUp, 36);
		Register("zorua", "zoroark", EvolutionTrigger.LevelUp, 30);
		Register("minccino", "cinccino", EvolutionTrigger.UseItem, item: "PokemonShinyStone");
		Register("gothita", "gothorita", EvolutionTrigger.LevelUp, 32);
		Register("gothorita", "gothitelle", EvolutionTrigger.LevelUp, 41);
		Register("solosis", "duosion", EvolutionTrigger.LevelUp, 32);
		Register("duosion", "reuniclus", EvolutionTrigger.LevelUp, 41);
		Register("ducklett", "swanna", EvolutionTrigger.LevelUp, 35);
		Register("vanillite", "vanillish", EvolutionTrigger.LevelUp, 35);
		Register("vanillish", "vanilluxe", EvolutionTrigger.LevelUp, 47);
		Register("deerling", "sawsbuck_spring", EvolutionTrigger.LevelUp, 34);
		Register("karrablast", "escavalier", EvolutionTrigger.Trade);
		Register("foongus", "amoonguss", EvolutionTrigger.LevelUp, 39);
		Register("frillish", "jellicent", EvolutionTrigger.LevelUp, 40);
		Register("joltik", "galvantula", EvolutionTrigger.LevelUp, 36);
		Register("ferroseed", "ferrothorn", EvolutionTrigger.LevelUp, 40);
		Register("klink", "klang", EvolutionTrigger.LevelUp, 38);
		Register("klang", "klinklang", EvolutionTrigger.LevelUp, 49);
		Register("tynamo", "eelektrik", EvolutionTrigger.LevelUp, 39);
		Register("eelektrik", "eelektross", EvolutionTrigger.UseItem, item: "PokemonThunderStone");
		Register("elgyem", "beheeyem", EvolutionTrigger.LevelUp, 42);
		Register("litwick", "lampent", EvolutionTrigger.LevelUp, 41);
		Register("lampent", "chandelure", EvolutionTrigger.UseItem, item: "PokemonDuskStone");
		Register("axew", "fraxure", EvolutionTrigger.LevelUp, 38);
		Register("fraxure", "haxorus", EvolutionTrigger.LevelUp, 48);
		Register("cubchoo", "beartic", EvolutionTrigger.LevelUp, 37);
		Register("shelmet", "accelgor", EvolutionTrigger.Trade);
		Register("mienfoo", "mienshao", EvolutionTrigger.LevelUp, 50);
		Register("golett", "golurk", EvolutionTrigger.LevelUp, 43);
		Register("pawniard", "bisharp", EvolutionTrigger.LevelUp, 52);
		Register("bisharp", "kingambit", EvolutionTrigger.LevelUp, 0);
		Register("rufflet", "braviary", EvolutionTrigger.LevelUp, 54);
		Register("rufflet", "braviary_hisui", EvolutionTrigger.LevelUp, 54);
		Register("vullaby", "mandibuzz", EvolutionTrigger.LevelUp, 54);
		Register("deino", "zweilous", EvolutionTrigger.LevelUp, 50);
		Register("zweilous", "hydreigon", EvolutionTrigger.LevelUp, 64);
		Register("larvesta", "volcarona", EvolutionTrigger.LevelUp, 59);
		Register("chespin", "quilladin", EvolutionTrigger.LevelUp, 16);
		Register("quilladin", "chesnaught", EvolutionTrigger.LevelUp, 36);
		Register("fennekin", "braixen", EvolutionTrigger.LevelUp, 16);
		Register("braixen", "delphox", EvolutionTrigger.LevelUp, 36);
		Register("froakie", "frogadier", EvolutionTrigger.LevelUp, 16);
		Register("frogadier", "greninja", EvolutionTrigger.LevelUp, 36);
		Register("bunnelby", "diggersby", EvolutionTrigger.LevelUp, 20);
		Register("fletchling", "fletchinder", EvolutionTrigger.LevelUp, 17);
		Register("fletchinder", "talonflame", EvolutionTrigger.LevelUp, 35);
		Register("litleo", "pyroar", EvolutionTrigger.LevelUp, 35);
		Register("skiddo", "gogoat", EvolutionTrigger.LevelUp, 32);
		Register("pancham", "pangoro", EvolutionTrigger.LevelUp, 32);
		Register("espurr", "meowstic_m", EvolutionTrigger.LevelUp, 25);
		Register("espurr", "meowstic_f", EvolutionTrigger.LevelUp, 25);
		Register("honedge", "doublade", EvolutionTrigger.LevelUp, 35);
		Register("doublade", "aegislash_shield", EvolutionTrigger.UseItem, item: "PokemonDuskStone");
		Register("spritzee", "aromatisse", EvolutionTrigger.Trade);
		Register("swirlix", "slurpuff", EvolutionTrigger.Trade);
		Register("inkay", "malamar", EvolutionTrigger.LevelUp, 30);
		Register("binacle", "barbaracle", EvolutionTrigger.LevelUp, 39);
		Register("skrelp", "dragalge", EvolutionTrigger.LevelUp, 48);
		Register("clauncher", "clawitzer", EvolutionTrigger.LevelUp, 37);
		Register("helioptile", "heliolisk", EvolutionTrigger.UseItem, item: "PokemonSunStone");
		Register("tyrunt", "tyrantrum", EvolutionTrigger.LevelUp, 39);
		Register("amaura", "aurorus", EvolutionTrigger.LevelUp, 39);
		Register("goomy", "sliggoo", EvolutionTrigger.LevelUp, 40);
		Register("goomy", "sliggoo_hisui", EvolutionTrigger.LevelUp, 40);
		Register("sliggoo", "goodra", EvolutionTrigger.LevelUp, 50);
		Register("phantump", "trevenant", EvolutionTrigger.Trade);
		Register("pumpkaboo", "gourgeist_average", EvolutionTrigger.Trade);
		Register("bergmite", "avalugg", EvolutionTrigger.LevelUp, 37);
		Register("bergmite", "avalugg_hisui", EvolutionTrigger.LevelUp, 37);
		Register("noibat", "noivern", EvolutionTrigger.LevelUp, 48);
		Register("rowlet", "dartrix", EvolutionTrigger.LevelUp, 17);
		Register("dartrix", "decidueye", EvolutionTrigger.LevelUp, 34);
		Register("dartrix", "decidueye_hisui", EvolutionTrigger.LevelUp, 36);
		Register("litten", "torracat", EvolutionTrigger.LevelUp, 17);
		Register("torracat", "incineroar", EvolutionTrigger.LevelUp, 34);
		Register("popplio", "brionne", EvolutionTrigger.LevelUp, 17);
		Register("brionne", "primarina", EvolutionTrigger.LevelUp, 34);
		Register("pikipek", "trumbeak", EvolutionTrigger.LevelUp, 14);
		Register("trumbeak", "toucannon", EvolutionTrigger.LevelUp, 28);
		Register("yungoos", "gumshoos", EvolutionTrigger.LevelUp, 20);
		Register("grubbin", "charjabug", EvolutionTrigger.LevelUp, 20);
		Register("charjabug", "vikavolt", EvolutionTrigger.LevelUp, 0);
		Register("crabrawler", "crabominable", EvolutionTrigger.LevelUp, 0);
		Register("cutiefly", "ribombee", EvolutionTrigger.LevelUp, 25);
		Register("rockruff", "lycanroc_midday", EvolutionTrigger.LevelUp, 25);
		Register("rockruff", "lycanroc_midnight", EvolutionTrigger.LevelUp, 25);
		Register("mareanie", "toxapex", EvolutionTrigger.LevelUp, 38);
		Register("mudbray", "mudsdale", EvolutionTrigger.LevelUp, 30);
		Register("dewpider", "araquanid", EvolutionTrigger.LevelUp, 22);
		Register("fomantis", "lurantis", EvolutionTrigger.LevelUp, 34);
		Register("morelull", "shiinotic", EvolutionTrigger.LevelUp, 24);
		Register("salandit", "salazzle", EvolutionTrigger.LevelUp, 33);
		Register("stufful", "bewear", EvolutionTrigger.LevelUp, 27);
		Register("bounsweet", "steenee", EvolutionTrigger.LevelUp, 18);
		Register("steenee", "tsareena", EvolutionTrigger.LevelUp, 0);
		Register("wimpod", "golisopod", EvolutionTrigger.LevelUp, 30);
		Register("sandygast", "palossand", EvolutionTrigger.LevelUp, 42);
		Register("type_null", "silvally_normal", EvolutionTrigger.LevelUp, 0);
		Register("jangmo_o", "hakamo_o", EvolutionTrigger.LevelUp, 35);
		Register("hakamo_o", "kommo_o", EvolutionTrigger.LevelUp, 45);
		Register("cosmog", "cosmoem", EvolutionTrigger.LevelUp, 43);
		Register("cosmoem", "solgaleo", EvolutionTrigger.LevelUp, 53);
		Register("cosmoem", "lunala", EvolutionTrigger.LevelUp, 53);
		Register("poipole", "naganadel", EvolutionTrigger.LevelUp, 0);
		Register("grookey", "thwackey", EvolutionTrigger.LevelUp, 16);
		Register("thwackey", "rillaboom", EvolutionTrigger.LevelUp, 35);
		Register("scorbunny", "raboot", EvolutionTrigger.LevelUp, 16);
		Register("raboot", "cinderace", EvolutionTrigger.LevelUp, 35);
		Register("sobble", "drizzile", EvolutionTrigger.LevelUp, 16);
		Register("drizzile", "inteleon", EvolutionTrigger.LevelUp, 35);
		Register("skwovet", "greedent", EvolutionTrigger.LevelUp, 24);
		Register("rookidee", "corvisquire", EvolutionTrigger.LevelUp, 18);
		Register("corvisquire", "corviknight", EvolutionTrigger.LevelUp, 38);
		Register("blipbug", "dottler", EvolutionTrigger.LevelUp, 10);
		Register("dottler", "orbeetle", EvolutionTrigger.LevelUp, 30);
		Register("nickit", "thievul", EvolutionTrigger.LevelUp, 18);
		Register("gossifleur", "eldegoss", EvolutionTrigger.LevelUp, 20);
		Register("wooloo", "dubwool", EvolutionTrigger.LevelUp, 24);
		Register("chewtle", "drednaw", EvolutionTrigger.LevelUp, 22);
		Register("yamper", "boltund", EvolutionTrigger.LevelUp, 25);
		Register("rolycoly", "carkol", EvolutionTrigger.LevelUp, 18);
		Register("carkol", "coalossal", EvolutionTrigger.LevelUp, 34);
		Register("dipplin", "hydrapple", EvolutionTrigger.LevelUp, 0);
		Register("silicobra", "sandaconda", EvolutionTrigger.LevelUp, 36);
		Register("arrokuda", "barraskewda", EvolutionTrigger.LevelUp, 26);
		Register("toxel", "toxtricity_amped", EvolutionTrigger.LevelUp, 30);
		Register("toxel", "toxtricity_low_key", EvolutionTrigger.LevelUp, 30);
		Register("sizzlipede", "centiskorch", EvolutionTrigger.LevelUp, 28);
		Register("clobbopus", "grapploct", EvolutionTrigger.LevelUp, 0);
		Register("hatenna", "hattrem", EvolutionTrigger.LevelUp, 32);
		Register("hattrem", "hatterene", EvolutionTrigger.LevelUp, 42);
		Register("impidimp", "morgrem", EvolutionTrigger.LevelUp, 32);
		Register("morgrem", "grimmsnarl", EvolutionTrigger.LevelUp, 42);
		Register("snom", "frosmoth", EvolutionTrigger.LevelUp, 0);
		Register("cufant", "copperajah", EvolutionTrigger.LevelUp, 34);
		Register("dreepy", "drakloak", EvolutionTrigger.LevelUp, 50);
		Register("drakloak", "dragapult", EvolutionTrigger.LevelUp, 60);
		Register("sprigatito", "floragato", EvolutionTrigger.LevelUp, 16);
		Register("floragato", "meowscarada", EvolutionTrigger.LevelUp, 36);
		Register("fuecoco", "crocalor", EvolutionTrigger.LevelUp, 16);
		Register("crocalor", "skeledirge", EvolutionTrigger.LevelUp, 36);
		Register("quaxly", "quaxwell", EvolutionTrigger.LevelUp, 16);
		Register("quaxwell", "quaquaval", EvolutionTrigger.LevelUp, 36);
		Register("lechonk", "oinkologne_m", EvolutionTrigger.LevelUp, 18);
		Register("lechonk", "oinkologne_f", EvolutionTrigger.LevelUp, 18);
		Register("tarountula", "spidops", EvolutionTrigger.LevelUp, 15);
		Register("nymble", "lokix", EvolutionTrigger.LevelUp, 24);
		Register("pawmi", "pawmo", EvolutionTrigger.LevelUp, 18);
		Register("pawmo", "pawmot", EvolutionTrigger.LevelUp, 0);
		Register("tandemaus", "maushold_four", EvolutionTrigger.LevelUp, 25);
		Register("tandemaus", "maushold_three", EvolutionTrigger.LevelUp, 25);
		Register("fidough", "dachsbun", EvolutionTrigger.LevelUp, 26);
		Register("smoliv", "dolliv", EvolutionTrigger.LevelUp, 25);
		Register("dolliv", "arboliva", EvolutionTrigger.LevelUp, 35);
		Register("nacli", "naclstack", EvolutionTrigger.LevelUp, 24);
		Register("naclstack", "garganacl", EvolutionTrigger.LevelUp, 38);
		Register("tadbulb", "bellibolt", EvolutionTrigger.UseItem, item: "PokemonThunderStone");
		Register("wattrel", "kilowattrel", EvolutionTrigger.LevelUp, 25);
		Register("maschiff", "mabosstiff", EvolutionTrigger.LevelUp, 30);
		Register("shroodle", "grafaiai", EvolutionTrigger.LevelUp, 28);
		Register("bramblin", "brambleghast", EvolutionTrigger.LevelUp, 0);
		Register("toedscool", "toedscruel", EvolutionTrigger.LevelUp, 30);
		Register("capsakid", "scovillain", EvolutionTrigger.UseItem, item: "PokemonFireStone");
		Register("rellor", "rabsca", EvolutionTrigger.LevelUp, 0);
		Register("flittle", "espathra", EvolutionTrigger.LevelUp, 35);
		Register("tinkatink", "tinkatuff", EvolutionTrigger.LevelUp, 24);
		Register("tinkatuff", "tinkaton", EvolutionTrigger.LevelUp, 38);
		Register("wiglett", "wugtrio", EvolutionTrigger.LevelUp, 26);
		Register("finizen", "palafin_zero", EvolutionTrigger.LevelUp, 38);
		Register("varoom", "revavroom", EvolutionTrigger.LevelUp, 40);
		Register("glimmet", "glimmora", EvolutionTrigger.LevelUp, 35);
		Register("greavard", "houndstone", EvolutionTrigger.LevelUp, 30);
		Register("cetoddle", "cetitan", EvolutionTrigger.UseItem, item: "PokemonDawnStone");
		Register("frigibax", "arctibax", EvolutionTrigger.LevelUp, 35);
		Register("arctibax", "baxcalibur", EvolutionTrigger.LevelUp, 54);
		Register("gimmighoul", "gholdengo", EvolutionTrigger.LevelUp, 0);
	}

	private static void LoadFromDecompilation()
	{
		try
		{
			string[] searchPaths = new string[]
			{
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeplatinum", "res", "pokemon")
			};

			foreach (var dir in searchPaths)
			{
				if (!Directory.Exists(dir)) continue;

				foreach (var sub in Directory.GetDirectories(dir))
				{
					string dataPath = Path.Combine(sub, "data.json");
					if (!File.Exists(dataPath)) continue;

					string speciesName = Path.GetFileName(sub).ToLowerInvariant();
					if (_evolutions.ContainsKey(speciesName) && _evolutions[speciesName].Count > 0)
						continue; // Already has canonical rule

					try
					{
						string json = File.ReadAllText(dataPath);
						using var doc = JsonDocument.Parse(json);
						if (!doc.RootElement.TryGetProperty("evolutions", out var evoArr)) continue;

						foreach (var evoEntry in evoArr.EnumerateArray())
						{
							if (evoEntry.GetArrayLength() < 2) continue;
							string method = evoEntry[0].GetString();

							if (method == "EVO_LEVEL" && evoEntry.GetArrayLength() >= 3)
							{
								int lvl = evoEntry[1].GetInt32();
								string target = evoEntry[2].GetString().Replace("SPECIES_", "").ToLowerInvariant();
								Register(speciesName, target, EvolutionTrigger.LevelUp, lvl);
							}
							else if (method == "EVO_TRADE" && evoEntry.GetArrayLength() >= 2)
							{
								string target = evoEntry[1].GetString().Replace("SPECIES_", "").ToLowerInvariant();
								Register(speciesName, target, EvolutionTrigger.Trade);
							}
							else if (method == "EVO_USE_ITEM" && evoEntry.GetArrayLength() >= 3)
							{
								string itemRaw = evoEntry[1].GetString().Replace("ITEM_", "").ToLowerInvariant();
								string stoneId = itemRaw switch
								{
									"fire_stone" => "PokemonFireStone",
									"water_stone" => "PokemonWaterStone",
									"thunder_stone" => "PokemonThunderStone",
									"leaf_stone" => "PokemonLeafStone",
									"moon_stone" => "PokemonMoonStone",
									"sun_stone" => "PokemonSunStone",
									"shiny_stone" => "PokemonShinyStone",
									"dusk_stone" => "PokemonDuskStone",
									"dawn_stone" => "PokemonDawnStone",
									_ => "PokemonMoonStone"
								};
								string target = evoEntry[2].GetString().Replace("SPECIES_", "").ToLowerInvariant();
								Register(speciesName, target, EvolutionTrigger.UseItem, item: stoneId);
							}
						}
					}
					catch { }
				}
				break;
			}
		}
		catch { }
	}
}
