using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StardewValley.Pokemon;

public static class PokemonBreeding
{
	public static PokemonInstance DaycareParent1 { get; set; }
	public static PokemonInstance DaycareParent2 { get; set; }
	public static PokemonInstance ReadyEgg { get; set; }

	// Unbreedable legendaries / mythicals / ultra beasts / paradox / babies (Gen 1-9)
	private static readonly HashSet<string> UndiscoveredGroup = new(StringComparer.OrdinalIgnoreCase)
	{
		"Arctovish",
		"Arctozolt",
		"Articuno",
		"Azelf",
		"Azurill",
		"Blacephalon",
		"Bonsly",
		"Brute Bonnet",
		"Budew",
		"Buzzwole",
		"Calyrex",
		"Celebi",
		"Celesteela",
		"Chi-Yu",
		"Chien-Pao",
		"Chingling",
		"Cleffa",
		"Cobalion",
		"Cosmoem",
		"Cosmog",
		"Cresselia",
		"Darkrai",
		"Deoxys",
		"Dialga",
		"Diancie",
		"Dracovish",
		"Dracozolt",
		"Eevee",
		"Elekid",
		"Enamorus",
		"Entei",
		"Eternatus",
		"Fezandipiti",
		"Floette",
		"Flutter Mane",
		"Genesect",
		"Gholdengo",
		"Gimmighoul",
		"Giratina",
		"Glastrier",
		"Gouging Fire",
		"Great Tusk",
		"Groudon",
		"Guzzlord",
		"Happiny",
		"Heatran",
		"Ho-Oh",
		"Hoopa",
		"Igglybuff",
		"Iron Boulder",
		"Iron Bundle",
		"Iron Crown",
		"Iron Hands",
		"Iron Jugulis",
		"Iron Leaves",
		"Iron Moth",
		"Iron Thorns",
		"Iron Treads",
		"Iron Valiant",
		"Jirachi",
		"Kartana",
		"Keldeo",
		"Koraidon",
		"Kubfu",
		"Kyogre",
		"Kyurem",
		"Landorus",
		"Latias",
		"Latios",
		"Lugia",
		"Lunala",
		"Magby",
		"Magearna",
		"Mantyke",
		"Marshadow",
		"Melmetal",
		"Meloetta",
		"Meltan",
		"Mesprit",
		"Mew",
		"Mewtwo",
		"Mime Jr.",
		"Miraidon",
		"Moltres",
		"Munchlax",
		"Munkidori",
		"Naganadel",
		"Necrozma",
		"Nidoqueen",
		"Nidorina",
		"Nihilego",
		"Ogerpon",
		"Okidogi",
		"Palkia",
		"Pecharunt",
		"Pheromosa",
		"Pichu",
		"Pikachu",
		"Poipole",
		"Raging Bolt",
		"Raikou",
		"Rayquaza",
		"Regice",
		"Regidrago",
		"Regieleki",
		"Regigigas",
		"Regirock",
		"Registeel",
		"Reshiram",
		"Riolu",
		"Roaring Moon",
		"Sandy Shocks",
		"Scream Tail",
		"Shaymin",
		"Silvally",
		"Slither Wing",
		"Smoochum",
		"Solgaleo",
		"Spectrier",
		"Stakataka",
		"Suicune",
		"Tapu Bulu",
		"Tapu Fini",
		"Tapu Koko",
		"Tapu Lele",
		"Terapagos",
		"Terrakion",
		"Thundurus",
		"Ting-Lu",
		"Togepi",
		"Tornadus",
		"Toxel",
		"Type Null",
		"Type: Null",
		"Tyrogue",
		"Urshifu",
		"Uxie",
		"Victini",
		"Virizion",
		"Volcanion",
		"Walking Wake",
		"Wo-Chien",
		"Wynaut",
		"Xerneas",
		"Xurkitree",
		"Yveltal",
		"Zacian",
		"Zamazenta",
		"Zapdos",
		"Zarude",
		"Zekrom",
		"Zeraora",
		"Zygarde",
	};

	// Complete Gen 1-9 Base / Baby species mapping (evolution backwards mapping)
	private static readonly Dictionary<string, string> BaseSpeciesMap = new(StringComparer.OrdinalIgnoreCase)
	{
		{ "Abomasnow", "Snover" },
		{ "Accelgor", "Shelmet" },
		{ "Aegislash", "Honedge" },
		{ "Aggron", "Aron" },
		{ "Alakazam", "Abra" },
		{ "Alcremie", "Milcery" },
		{ "Altaria", "Swablu" },
		{ "Ambipom", "Aipom" },
		{ "Amoonguss", "Foongus" },
		{ "Ampharos", "Mareep" },
		{ "Annihilape", "Mankey" },
		{ "Appletun", "Applin" },
		{ "Araquanid", "Dewpider" },
		{ "Arbok", "Ekans" },
		{ "Arboliva", "Smoliv" },
		{ "Arcanine", "Growlithe" },
		{ "Archaludon", "Duraludon" },
		{ "Archeops", "Archen" },
		{ "Arctibax", "Frigibax" },
		{ "Ariados", "Spinarak" },
		{ "Armaldo", "Anorith" },
		{ "Armarouge", "Charcadet" },
		{ "Aromatisse", "Spritzee" },
		{ "Aurorus", "Amaura" },
		{ "Avalugg", "Bergmite" },
		{ "Azumarill", "Azurill" },
		{ "Banette", "Shuppet" },
		{ "Barbaracle", "Binacle" },
		{ "Barraskewda", "Arrokuda" },
		{ "Basculegion", "Basculin" },
		{ "Bastiodon", "Shieldon" },
		{ "Baxcalibur", "Frigibax" },
		{ "Bayleef", "Chikorita" },
		{ "Beartic", "Cubchoo" },
		{ "Beautifly", "Wurmple" },
		{ "Beedrill", "Weedle" },
		{ "Beheeyem", "Elgyem" },
		{ "Bellibolt", "Tadbulb" },
		{ "Bellossom", "Oddish" },
		{ "Bewear", "Stufful" },
		{ "Bibarel", "Bidoof" },
		{ "Bisharp", "Pawniard" },
		{ "Blastoise", "Squirtle" },
		{ "Blaziken", "Torchic" },
		{ "Blissey", "Happiny" },
		{ "Boldore", "Roggenrola" },
		{ "Boltund", "Yamper" },
		{ "Braixen", "Fennekin" },
		{ "Brambleghast", "Bramblin" },
		{ "Braviary", "Rufflet" },
		{ "Breloom", "Shroomish" },
		{ "Brionne", "Popplio" },
		{ "Bronzong", "Bronzor" },
		{ "Butterfree", "Caterpie" },
		{ "Cacturne", "Cacnea" },
		{ "Camerupt", "Numel" },
		{ "Carkol", "Rolycoly" },
		{ "Carracosta", "Tirtouga" },
		{ "Cascoon", "Wurmple" },
		{ "Centiskorch", "Sizzlipede" },
		{ "Ceruledge", "Charcadet" },
		{ "Cetitan", "Cetoddle" },
		{ "Chandelure", "Litwick" },
		{ "Chansey", "Happiny" },
		{ "Charizard", "Charmander" },
		{ "Charjabug", "Grubbin" },
		{ "Charmeleon", "Charmander" },
		{ "Cherrim", "Cherubi" },
		{ "Chesnaught", "Chespin" },
		{ "Chimecho", "Chingling" },
		{ "Cinccino", "Minccino" },
		{ "Cinderace", "Scorbunny" },
		{ "Clawitzer", "Clauncher" },
		{ "Claydol", "Baltoy" },
		{ "Clefable", "Cleffa" },
		{ "Clefairy", "Cleffa" },
		{ "Clodsire", "Wooper" },
		{ "Cloyster", "Shellder" },
		{ "Coalossal", "Rolycoly" },
		{ "Cofagrigus", "Yamask" },
		{ "Combusken", "Torchic" },
		{ "Conkeldurr", "Timburr" },
		{ "Copperajah", "Cufant" },
		{ "Corviknight", "Rookidee" },
		{ "Corvisquire", "Rookidee" },
		{ "Cosmoem", "Cosmog" },
		{ "Crabominable", "Crabrawler" },
		{ "Cradily", "Lileep" },
		{ "Crawdaunt", "Corphish" },
		{ "Crobat", "Zubat" },
		{ "Crocalor", "Fuecoco" },
		{ "Croconaw", "Totodile" },
		{ "Crustle", "Dwebble" },
		{ "Cursola", "Corsola" },
		{ "Dachsbun", "Fidough" },
		{ "Darmanitan", "Darumaka" },
		{ "Dartrix", "Rowlet" },
		{ "Decidueye", "Rowlet" },
		{ "Delcatty", "Skitty" },
		{ "Delphox", "Fennekin" },
		{ "Dewgong", "Seel" },
		{ "Dewott", "Oshawott" },
		{ "Diggersby", "Bunnelby" },
		{ "Dipplin", "Applin" },
		{ "Dodrio", "Doduo" },
		{ "Dolliv", "Smoliv" },
		{ "Donphan", "Phanpy" },
		{ "Dottler", "Blipbug" },
		{ "Doublade", "Honedge" },
		{ "Dragalge", "Skrelp" },
		{ "Dragapult", "Dreepy" },
		{ "Dragonair", "Dratini" },
		{ "Dragonite", "Dratini" },
		{ "Drakloak", "Dreepy" },
		{ "Drapion", "Skorupi" },
		{ "Drednaw", "Chewtle" },
		{ "Drifblim", "Drifloon" },
		{ "Drizzile", "Sobble" },
		{ "Dubwool", "Wooloo" },
		{ "Dudunsparce", "Dunsparce" },
		{ "Dugtrio", "Diglett" },
		{ "Duosion", "Solosis" },
		{ "Dusclops", "Duskull" },
		{ "Dusknoir", "Duskull" },
		{ "Dustox", "Wurmple" },
		{ "Eelektrik", "Tynamo" },
		{ "Eelektross", "Tynamo" },
		{ "Eldegoss", "Gossifleur" },
		{ "Electabuzz", "Elekid" },
		{ "Electivire", "Elekid" },
		{ "Electrode", "Voltorb" },
		{ "Emboar", "Tepig" },
		{ "Empoleon", "Piplup" },
		{ "Escavalier", "Karrablast" },
		{ "Espathra", "Flittle" },
		{ "Espeon", "Eevee" },
		{ "Excadrill", "Drilbur" },
		{ "Exeggutor", "Exeggcute" },
		{ "Exploud", "Whismur" },
		{ "Farigiraf", "Girafarig" },
		{ "Fearow", "Spearow" },
		{ "Feraligatr", "Totodile" },
		{ "Ferrothorn", "Ferroseed" },
		{ "Flaaffy", "Mareep" },
		{ "Flapple", "Applin" },
		{ "Flareon", "Eevee" },
		{ "Fletchinder", "Fletchling" },
		{ "Floatzel", "Buizel" },
		{ "Floragato", "Sprigatito" },
		{ "Flygon", "Trapinch" },
		{ "Forretress", "Pineco" },
		{ "Fraxure", "Axew" },
		{ "Frogadier", "Froakie" },
		{ "Froslass", "Snorunt" },
		{ "Frosmoth", "Snom" },
		{ "Furret", "Sentret" },
		{ "Gabite", "Gible" },
		{ "Gallade", "Ralts" },
		{ "Galvantula", "Joltik" },
		{ "Garbodor", "Trubbish" },
		{ "Garchomp", "Gible" },
		{ "Gardevoir", "Ralts" },
		{ "Garganacl", "Nacli" },
		{ "Gastrodon", "Shellos" },
		{ "Gengar", "Gastly" },
		{ "Gholdengo", "Gimmighoul" },
		{ "Gigalith", "Roggenrola" },
		{ "Glaceon", "Eevee" },
		{ "Glalie", "Snorunt" },
		{ "Glimmora", "Glimmet" },
		{ "Gliscor", "Gligar" },
		{ "Gloom", "Oddish" },
		{ "Gogoat", "Skiddo" },
		{ "Golbat", "Zubat" },
		{ "Golduck", "Psyduck" },
		{ "Golem", "Geodude" },
		{ "Golisopod", "Wimpod" },
		{ "Golurk", "Golett" },
		{ "Goodra", "Goomy" },
		{ "Gorebyss", "Clamperl" },
		{ "Gothitelle", "Gothita" },
		{ "Gothorita", "Gothita" },
		{ "Gourgeist", "Pumpkaboo" },
		{ "Grafaiai", "Shroodle" },
		{ "Granbull", "Snubbull" },
		{ "Grapploct", "Clobbopus" },
		{ "Graveler", "Geodude" },
		{ "Greedent", "Skwovet" },
		{ "Greninja", "Froakie" },
		{ "Grimmsnarl", "Impidimp" },
		{ "Grotle", "Turtwig" },
		{ "Grovyle", "Treecko" },
		{ "Grumpig", "Spoink" },
		{ "Gumshoos", "Yungoos" },
		{ "Gurdurr", "Timburr" },
		{ "Gyarados", "Magikarp" },
		{ "Hakamo-o", "Jangmo-o" },
		{ "Hariyama", "Makuhita" },
		{ "Hatterene", "Hatenna" },
		{ "Hattrem", "Hatenna" },
		{ "Haunter", "Gastly" },
		{ "Haxorus", "Axew" },
		{ "Heliolisk", "Helioptile" },
		{ "Herdier", "Lillipup" },
		{ "Hippowdon", "Hippopotas" },
		{ "Hitmonchan", "Tyrogue" },
		{ "Hitmonlee", "Tyrogue" },
		{ "Hitmontop", "Tyrogue" },
		{ "Honchkrow", "Murkrow" },
		{ "Houndoom", "Houndour" },
		{ "Houndstone", "Greavard" },
		{ "Huntail", "Clamperl" },
		{ "Hydrapple", "Applin" },
		{ "Hydreigon", "Deino" },
		{ "Hypno", "Drowzee" },
		{ "Illumise", "Volbeat" },
		{ "Incineroar", "Litten" },
		{ "Infernape", "Chimchar" },
		{ "Inteleon", "Sobble" },
		{ "Ivysaur", "Bulbasaur" },
		{ "Jellicent", "Frillish" },
		{ "Jigglypuff", "Igglybuff" },
		{ "Jolteon", "Eevee" },
		{ "Jumpluff", "Hoppip" },
		{ "Jynx", "Smoochum" },
		{ "Kabutops", "Kabuto" },
		{ "Kadabra", "Abra" },
		{ "Kakuna", "Weedle" },
		{ "Kilowattrel", "Wattrel" },
		{ "Kingambit", "Pawniard" },
		{ "Kingdra", "Horsea" },
		{ "Kingler", "Krabby" },
		{ "Kirlia", "Ralts" },
		{ "Klang", "Klink" },
		{ "Kleavor", "Scyther" },
		{ "Klinklang", "Klink" },
		{ "Kommo-o", "Jangmo-o" },
		{ "Kricketune", "Kricketot" },
		{ "Krokorok", "Sandile" },
		{ "Krookodile", "Sandile" },
		{ "Lairon", "Aron" },
		{ "Lampent", "Litwick" },
		{ "Lanturn", "Chinchou" },
		{ "Leafeon", "Eevee" },
		{ "Leavanny", "Sewaddle" },
		{ "Ledian", "Ledyba" },
		{ "Lickilicky", "Lickitung" },
		{ "Liepard", "Purrloin" },
		{ "Lilligant", "Petilil" },
		{ "Linoone", "Zigzagoon" },
		{ "Lokix", "Nymble" },
		{ "Lombre", "Lotad" },
		{ "Lopunny", "Buneary" },
		{ "Loudred", "Whismur" },
		{ "Lucario", "Riolu" },
		{ "Ludicolo", "Lotad" },
		{ "Lumineon", "Finneon" },
		{ "Lunala", "Cosmog" },
		{ "Lurantis", "Fomantis" },
		{ "Luxio", "Shinx" },
		{ "Luxray", "Shinx" },
		{ "Lycanroc", "Rockruff" },
		{ "Mabosstiff", "Maschiff" },
		{ "Machamp", "Machop" },
		{ "Machoke", "Machop" },
		{ "Magcargo", "Slugma" },
		{ "Magmar", "Magby" },
		{ "Magmortar", "Magby" },
		{ "Magneton", "Magnemite" },
		{ "Magnezone", "Magnemite" },
		{ "Malamar", "Inkay" },
		{ "Mamoswine", "Swinub" },
		{ "Manaphy", "Phione" },
		{ "Mandibuzz", "Vullaby" },
		{ "Manectric", "Electrike" },
		{ "Mantine", "Mantyke" },
		{ "Marill", "Azurill" },
		{ "Marowak", "Cubone" },
		{ "Marshtomp", "Mudkip" },
		{ "Masquerain", "Surskit" },
		{ "Maushold", "Tandemaus" },
		{ "Medicham", "Meditite" },
		{ "Meganium", "Chikorita" },
		{ "Melmetal", "Meltan" },
		{ "Meowscarada", "Sprigatito" },
		{ "Meowstic", "Espurr" },
		{ "Metagross", "Beldum" },
		{ "Metang", "Beldum" },
		{ "Metapod", "Caterpie" },
		{ "Mienshao", "Mienfoo" },
		{ "Mightyena", "Poochyena" },
		{ "Milotic", "Feebas" },
		{ "Mismagius", "Misdreavus" },
		{ "Monferno", "Chimchar" },
		{ "Morgrem", "Impidimp" },
		{ "Mr. Mime", "Mime Jr." },
		{ "Mr. Rime", "Mime Jr." },
		{ "Mudsdale", "Mudbray" },
		{ "Muk", "Grimer" },
		{ "Musharna", "Munna" },
		{ "Naclstack", "Nacli" },
		{ "Naganadel", "Poipole" },
		{ "Nidoking", "Nidoran♀" },
		{ "Nidoqueen", "Nidoran♀" },
		{ "Nidoran♂", "Nidoran♀" },
		{ "Nidorina", "Nidoran♀" },
		{ "Nidorino", "Nidoran♀" },
		{ "Ninetales", "Vulpix" },
		{ "Ninjask", "Nincada" },
		{ "Noctowl", "Hoothoot" },
		{ "Noivern", "Noibat" },
		{ "Nuzleaf", "Seedot" },
		{ "Obstagoon", "Zigzagoon" },
		{ "Octillery", "Remoraid" },
		{ "Oinkologne", "Lechonk" },
		{ "Omastar", "Omanyte" },
		{ "Orbeetle", "Blipbug" },
		{ "Overqwil", "Qwilfish" },
		{ "Palafin", "Finizen" },
		{ "Palossand", "Sandygast" },
		{ "Palpitoad", "Tympole" },
		{ "Pangoro", "Pancham" },
		{ "Parasect", "Paras" },
		{ "Pawmo", "Pawmi" },
		{ "Pawmot", "Pawmi" },
		{ "Pelipper", "Wingull" },
		{ "Perrserker", "Meowth" },
		{ "Persian", "Meowth" },
		{ "Pidgeot", "Pidgey" },
		{ "Pidgeotto", "Pidgey" },
		{ "Pignite", "Tepig" },
		{ "Pikachu", "Pichu" },
		{ "Piloswine", "Swinub" },
		{ "Politoed", "Poliwag" },
		{ "Poliwhirl", "Poliwag" },
		{ "Poliwrath", "Poliwag" },
		{ "Polteageist", "Sinistea" },
		{ "Porygon-Z", "Porygon" },
		{ "Porygon2", "Porygon" },
		{ "Primarina", "Popplio" },
		{ "Primeape", "Mankey" },
		{ "Prinplup", "Piplup" },
		{ "Probopass", "Nosepass" },
		{ "Pupitar", "Larvitar" },
		{ "Purugly", "Glameow" },
		{ "Pyroar", "Litleo" },
		{ "Quagsire", "Wooper" },
		{ "Quaquaval", "Quaxly" },
		{ "Quaxwell", "Quaxly" },
		{ "Quilava", "Cyndaquil" },
		{ "Quilladin", "Chespin" },
		{ "Raboot", "Scorbunny" },
		{ "Rabsca", "Rellor" },
		{ "Raichu", "Pichu" },
		{ "Rampardos", "Cranidos" },
		{ "Rapidash", "Ponyta" },
		{ "Raticate", "Rattata" },
		{ "Reuniclus", "Solosis" },
		{ "Revavroom", "Varoom" },
		{ "Rhydon", "Rhyhorn" },
		{ "Rhyperior", "Rhyhorn" },
		{ "Ribombee", "Cutiefly" },
		{ "Rillaboom", "Grookey" },
		{ "Roselia", "Budew" },
		{ "Roserade", "Budew" },
		{ "Runerigus", "Yamask" },
		{ "Salamence", "Bagon" },
		{ "Salazzle", "Salandit" },
		{ "Samurott", "Oshawott" },
		{ "Sandaconda", "Silicobra" },
		{ "Sandslash", "Sandshrew" },
		{ "Sawsbuck", "Deerling" },
		{ "Sceptile", "Treecko" },
		{ "Scizor", "Scyther" },
		{ "Scolipede", "Venipede" },
		{ "Scovillain", "Capsakid" },
		{ "Scrafty", "Scraggy" },
		{ "Seadra", "Horsea" },
		{ "Seaking", "Goldeen" },
		{ "Sealeo", "Spheal" },
		{ "Seismitoad", "Tympole" },
		{ "Serperior", "Snivy" },
		{ "Servine", "Snivy" },
		{ "Sharpedo", "Carvanha" },
		{ "Shedinja", "Nincada" },
		{ "Shelgon", "Bagon" },
		{ "Shiftry", "Seedot" },
		{ "Shiinotic", "Morelull" },
		{ "Silcoon", "Wurmple" },
		{ "Simipour", "Panpour" },
		{ "Simisage", "Pansage" },
		{ "Simisear", "Pansear" },
		{ "Sinistcha", "Poltchageist" },
		{ "Sirfetch'd", "Farfetch'd" },
		{ "Skeledirge", "Fuecoco" },
		{ "Skiploom", "Hoppip" },
		{ "Skuntank", "Stunky" },
		{ "Slaking", "Slakoth" },
		{ "Sliggoo", "Goomy" },
		{ "Slowbro", "Slowpoke" },
		{ "Slowking", "Slowpoke" },
		{ "Slurpuff", "Swirlix" },
		{ "Sneasler", "Sneasel" },
		{ "Snorlax", "Munchlax" },
		{ "Solgaleo", "Cosmog" },
		{ "Spidops", "Tarountula" },
		{ "Staraptor", "Starly" },
		{ "Staravia", "Starly" },
		{ "Starmie", "Staryu" },
		{ "Steelix", "Onix" },
		{ "Steenee", "Bounsweet" },
		{ "Stoutland", "Lillipup" },
		{ "Sudowoodo", "Bonsly" },
		{ "Sunflora", "Sunkern" },
		{ "Swadloon", "Sewaddle" },
		{ "Swalot", "Gulpin" },
		{ "Swampert", "Mudkip" },
		{ "Swanna", "Ducklett" },
		{ "Swellow", "Taillow" },
		{ "Swoobat", "Woobat" },
		{ "Sylveon", "Eevee" },
		{ "Talonflame", "Fletchling" },
		{ "Tangrowth", "Tangela" },
		{ "Tentacruel", "Tentacool" },
		{ "Thievul", "Nickit" },
		{ "Thwackey", "Grookey" },
		{ "Tinkaton", "Tinkatink" },
		{ "Tinkatuff", "Tinkatink" },
		{ "Toedscruel", "Toedscool" },
		{ "Togekiss", "Togepi" },
		{ "Togetic", "Togepi" },
		{ "Torracat", "Litten" },
		{ "Torterra", "Turtwig" },
		{ "Toucannon", "Pikipek" },
		{ "Toxapex", "Mareanie" },
		{ "Toxicroak", "Croagunk" },
		{ "Toxtricity", "Toxel" },
		{ "Tranquill", "Pidove" },
		{ "Trevenant", "Phantump" },
		{ "Trumbeak", "Pikipek" },
		{ "Tsareena", "Bounsweet" },
		{ "Typhlosion", "Cyndaquil" },
		{ "Tyranitar", "Larvitar" },
		{ "Tyrantrum", "Tyrunt" },
		{ "Umbreon", "Eevee" },
		{ "Unfezant", "Pidove" },
		{ "Ursaluna", "Teddiursa" },
		{ "Ursaring", "Teddiursa" },
		{ "Urshifu", "Kubfu" },
		{ "Vanillish", "Vanillite" },
		{ "Vanilluxe", "Vanillite" },
		{ "Vaporeon", "Eevee" },
		{ "Venomoth", "Venonat" },
		{ "Venusaur", "Bulbasaur" },
		{ "Vespiquen", "Combee" },
		{ "Vibrava", "Trapinch" },
		{ "Victreebel", "Bellsprout" },
		{ "Vigoroth", "Slakoth" },
		{ "Vikavolt", "Grubbin" },
		{ "Vileplume", "Oddish" },
		{ "Volcarona", "Larvesta" },
		{ "Wailord", "Wailmer" },
		{ "Walrein", "Spheal" },
		{ "Wartortle", "Squirtle" },
		{ "Watchog", "Patrat" },
		{ "Weavile", "Sneasel" },
		{ "Weepinbell", "Bellsprout" },
		{ "Weezing", "Koffing" },
		{ "Whimsicott", "Cottonee" },
		{ "Whirlipede", "Venipede" },
		{ "Whiscash", "Barboach" },
		{ "Wigglytuff", "Igglybuff" },
		{ "Wobbuffet", "Wynaut" },
		{ "Wormadam", "Burmy" },
		{ "Wugtrio", "Wiglett" },
		{ "Wyrdeer", "Stantler" },
		{ "Xatu", "Natu" },
		{ "Yanmega", "Yanma" },
		{ "Zebstrika", "Blitzle" },
		{ "Zoroark", "Zorua" },
		{ "Zweilous", "Deino" },
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
		// Decrease egg steps for all eggs in player party (Canonical Flame Body / Magma Armor 2x speed)
		if (PlayerParty.Instance?.Team == null) return;
		if (Game1.activeClickableMenu is PokemonHatchMenu) return;

		bool hasHatchBooster = false;
		for (int i = 0; i < PlayerParty.Instance.Team.Count; i++)
		{
			var mon = PlayerParty.Instance.Team[i];
			if (mon != null && !mon.IsEgg && (string.Equals(mon.Ability, "FlameBody", StringComparison.OrdinalIgnoreCase) ||
			                                  string.Equals(mon.Ability, "Flame Body", StringComparison.OrdinalIgnoreCase) ||
			                                  string.Equals(mon.Ability, "MagmaArmor", StringComparison.OrdinalIgnoreCase) ||
			                                  string.Equals(mon.Ability, "Magma Armor", StringComparison.OrdinalIgnoreCase) ||
			                                  string.Equals(mon.Ability, "SteamEngine", StringComparison.OrdinalIgnoreCase) ||
			                                  string.Equals(mon.Ability, "Steam Engine", StringComparison.OrdinalIgnoreCase)))
			{
				hasHatchBooster = true;
				break;
			}
		}

		int stepDecrement = hasHatchBooster ? 2 : 1;

		for (int i = 0; i < PlayerParty.Instance.Team.Count; i++)
		{
			var p = PlayerParty.Instance.Team[i];
			if (p != null && p.IsEgg)
			{
				p.EggSteps -= stepDecrement;
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
