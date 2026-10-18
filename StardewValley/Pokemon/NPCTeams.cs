using System;
using System.Collections.Generic;

namespace StardewValley.Pokemon;

public static class NPCTeams
{
	private static readonly Dictionary<string, List<PokemonInstance>> _teams = new(StringComparer.OrdinalIgnoreCase);

	static NPCTeams()
	{
		// Abigail: Occult, Gems, Swordsmanship, Chrono Trigger & Caves (Gen 1-9)
		RegisterTeam("Abigail",
			Create("Sableye", 26, "Shadow Sneak", "Power Gem", "Knock Off", "Will-O-Wisp"),
			Create("Gengar", 28, "Shadow Ball", "Sludge Bomb", "Dark Pulse", "Thunderbolt"),
			Create("Umbreon", 29, "Dark Pulse", "Foul Play", "Moonlight", "Quick Attack"),
			Create("Doublade", 30, "Iron Head", "Shadow Claw", "Sacred Sword", "Shadow Sneak"),
			Create("Glimmora", 31, "Power Gem", "Sludge Wave", "Earth Power", "Stealth Rock"),
			Create("Ceruledge", 33, "Bitter Blade", "Shadow Claw", "Psycho Cut", "Swords Dance")
		);

		// Marnie: Ranch & Farm Animals (Gen 1-9)
		RegisterTeam("Marnie",
			Create("Miltank", 25, "Body Slam", "Rollout", "Zen Headbutt", "Milk Drink"),
			Create("Tauros", 26, "Body Slam", "Zen Headbutt", "Earthquake", "Close Combat"),
			Create("Ampharos", 27, "Thunderbolt", "Power Gem", "Dragon Pulse", "Signal Beam"),
			Create("Mudsdale", 28, "High Horsepower", "Heavy Slam", "Body Press", "Rock Slide"),
			Create("Dubwool", 29, "Body Slam", "Body Press", "Zen Headbutt", "Wild Charge"),
			Create("Oinkologne", 30, "Body Slam", "High Horsepower", "Play Rough", "Seed Bomb")
		);

		// Shane: Blue Chickens, Peppers & Fermented Drinks (Gen 1-9)
		RegisterTeam("Shane",
			Create("Staraptor", 26, "Brave Bird", "Close Combat", "Quick Attack", "Roost"),
			Create("Honchkrow", 27, "Night Slash", "Brave Bird", "Sucker Punch", "Heat Wave"),
			Create("Talonflame", 28, "Brave Bird", "Flare Blitz", "Roost", "Steel Wing"),
			Create("Corviknight", 29, "Brave Bird", "Iron Head", "Body Press", "Roost"),
			Create("Scovillain", 30, "Flamethrower", "Energy Ball", "Crunch", "Spicy Extract"),
			Create("Blaziken", 32, "Blaze Kick", "Close Combat", "Brave Bird", "Thunder Punch")
		);

		// Harvey: Clinic, Aviation & Radio (Gen 1-9)
		RegisterTeam("Harvey",
			Create("Chansey", 26, "Body Slam", "Soft-Boiled", "Dazzling Gleam", "Thunder Wave"),
			Create("Togekiss", 27, "Air Slash", "Dazzling Gleam", "Aura Sphere", "Roost"),
			Create("Audino", 28, "Dazzling Gleam", "Hyper Voice", "Flamethrower", "Ice Beam"),
			Create("Comfey", 29, "Draining Kiss", "Giga Drain", "Dazzling Gleam", "Synthesis"),
			Create("Indeedee", 30, "Psychic", "Dazzling Gleam", "Shadow Ball", "Mystical Fire"),
			Create("Blissey", 32, "Hyper Voice", "Dazzling Gleam", "Flamethrower", "Ice Beam")
		);

		// Willy: Old Mariner & Deep Sea Fishing (Gen 1-9)
		RegisterTeam("Willy",
			Create("Pelipper", 26, "Surf", "Hurricane", "Ice Beam", "Roost"),
			Create("Kingler", 27, "Crabhammer", "Rock Slide", "X-Scissor", "Brick Break"),
			Create("Crawdaunt", 28, "Crabhammer", "Night Slash", "Aqua Jet", "Crunch"),
			Create("Drednaw", 29, "Liquidation", "Rock Slide", "Crunch", "Ice Fang"),
			Create("Dondozo", 30, "Wave Crash", "Order Up", "Body Press", "Earthquake"),
			Create("Gyarados", 32, "Waterfall", "Crunch", "Earthquake", "Ice Fang")
		);

		// Clint: Blacksmith, Furnace & Heavy Ores (Gen 1-9)
		RegisterTeam("Clint",
			Create("Golem", 26, "Earthquake", "Stone Edge", "Heavy Slam", "Fire Punch"),
			Create("Aggron", 27, "Iron Head", "Stone Edge", "Earthquake", "Body Press"),
			Create("Conkeldurr", 28, "Drain Punch", "Mach Punch", "Rock Slide", "Knock Off"),
			Create("Coalossal", 29, "Heat Crash", "Stone Edge", "Flamethrower", "Earth Power"),
			Create("Copperajah", 30, "Heavy Slam", "Play Rough", "High Horsepower", "Rock Slide"),
			Create("Tinkaton", 32, "Gigaton Hammer", "Play Rough", "Knock Off", "Rock Slide")
		);

		// Maru: Robotics, Astronomy & High-Voltage Gadgets (Gen 1-9)
		RegisterTeam("Maru",
			Create("Magnezone", 26, "Thunderbolt", "Flash Cannon", "Tri Attack", "Volt Switch"),
			Create("Rotom", 27, "Thunderbolt", "Shadow Ball", "Hydro Pump", "Will-O-Wisp"),
			Create("Reuniclus", 28, "Psychic", "Energy Ball", "Shadow Ball", "Flash Cannon"),
			Create("Vikavolt", 29, "Thunderbolt", "Bug Buzz", "Energy Ball", "Air Slash"),
			Create("Orbeetle", 30, "Psychic", "Bug Buzz", "Body Press", "Shadow Ball"),
			Create("Metagross", 32, "Meteor Mash", "Zen Headbutt", "Earthquake", "Bullet Punch")
		);

		// Sebastian: Motorcycles, Rain, Frogs & Tabletop RPGs (Gen 1-9)
		RegisterTeam("Sebastian",
			Create("Politoed", 26, "Surf", "Ice Beam", "Earth Power", "Hypnosis"),
			Create("Houndoom", 27, "Dark Pulse", "Flamethrower", "Sludge Bomb", "Nasty Plot"),
			Create("Weavile", 28, "Night Slash", "Triple Axel", "Ice Shard", "Brick Break"),
			Create("Greninja", 29, "Hydro Pump", "Dark Pulse", "Ice Beam", "U-turn"),
			Create("Corviknight", 30, "Brave Bird", "Iron Head", "Body Press", "U-turn"),
			Create("Cyclizar", 32, "Dragon Claw", "Double-Edge", "Knock Off", "U-turn")
		);

		// Linus: Harmony with Nature & Mountain Foraging (Gen 1-9)
		RegisterTeam("Linus",
			Create("Linoone", 25, "Extreme Speed", "Night Slash", "Seed Bomb", "Play Rough"),
			Create("Snorlax", 26, "Body Slam", "Crunch", "Earthquake", "Heavy Slam"),
			Create("Torterra", 27, "Wood Hammer", "Earthquake", "Stone Edge", "Crunch"),
			Create("Sawsbuck", 28, "Horn Leech", "Body Slam", "High Horsepower", "Play Rough"),
			Create("Oranguru", 29, "Psychic", "Shadow Ball", "Energy Ball", "Thunderbolt"),
			Create("Arboliva", 31, "Energy Ball", "Hyper Voice", "Earth Power", "Dazzling Gleam")
		);

		// Wizard (M. Rasmodius): Arcane Master [MEGA EVOLUTION ACE]
		RegisterTeam("Wizard",
			Create("Mismagius", 32, "Shadow Ball", "Mystical Fire", "Dazzling Gleam", "Thunderbolt"),
			Create("Sigilyph", 33, "Psychic", "Air Slash", "Heat Wave", "Ice Beam"),
			Create("Chandelure", 34, "Shadow Ball", "Flamethrower", "Energy Ball", "Will-O-Wisp"),
			Create("Delphox", 35, "Mystical Fire", "Psychic", "Dazzling Gleam", "Shadow Ball"),
			Create("Hatterene", 36, "Psychic", "Dazzling Gleam", "Mystical Fire", "Giga Drain"),
			CreateMega("Mega Alakazam", 38, "Psychic", "Shadow Ball", "Dazzling Gleam", "Energy Ball")
		);

		// Alex: Gridball Star & Strength Training (Gen 1-9)
		RegisterTeam("Alex",
			Create("Arcanine", 26, "Flare Blitz", "Extreme Speed", "Crunch", "Wild Charge"),
			Create("Machamp", 27, "Dynamic Punch", "Stone Edge", "Knock Off", "Bullet Punch"),
			Create("Hawlucha", 28, "Flying Press", "Acrobatics", "Close Combat", "Swords Dance"),
			Create("Incineroar", 29, "Darkest Lariat", "Flare Blitz", "Earthquake", "Parting Shot"),
			Create("Passimian", 30, "Close Combat", "Knock Off", "Rock Slide", "U-turn"),
			Create("Cinderace", 32, "Pyro Ball", "High Jump Kick", "Sucker Punch", "U-turn")
		);

		// Sam: Punk Rock, Skateboard & High-Energy Amps (Gen 1-9)
		RegisterTeam("Sam",
			Create("Raichu", 26, "Thunderbolt", "Surf", "Play Rough", "Nasty Plot"),
			Create("Electivire", 27, "Wild Charge", "Ice Punch", "Fire Punch", "Earthquake"),
			Create("Exploud", 28, "Boomburst", "Flamethrower", "Ice Beam", "Surf"),
			Create("Zebstrika", 29, "Wild Charge", "Overheat", "Double Kick", "Volt Switch"),
			Create("Obstagoon", 30, "Night Slash", "Facade", "Close Combat", "Ice Punch"),
			Create("Toxtricity", 32, "Overdrive", "Boomburst", "Sludge Wave", "Volt Switch")
		);

		// Penny: Literature, Schoolhouse & Gentle Guardians (Gen 1-9)
		RegisterTeam("Penny",
			Create("Gardevoir", 26, "Moonblast", "Psychic", "Mystical Fire", "Thunderbolt"),
			Create("Clefable", 27, "Moonblast", "Flamethrower", "Thunderbolt", "Soft-Boiled"),
			Create("Lilligant", 28, "Petal Dance", "Pollen Puff", "Quiver Dance", "Sleep Powder"),
			Create("Sylveon", 29, "Hyper Voice", "Mystical Fire", "Psyshock", "Quick Attack"),
			Create("Alcremie", 30, "Dazzling Gleam", "Mystical Fire", "Psychic", "Recover"),
			Create("Dachsbun", 31, "Play Rough", "Body Press", "Crunch", "Psychic Fangs")
		);

		// Pierre: General Store & Botanical Goods (Gen 1-9)
		RegisterTeam("Pierre",
			Create("Persian", 26, "Pay Day", "Slash", "Power Gem", "Night Slash"),
			Create("Kecleon", 27, "Slash", "Shadow Sneak", "Drain Punch", "Rock Slide"),
			Create("Whimsicott", 28, "Moonblast", "Energy Ball", "Hurricane", "Tailwind"),
			Create("Bounsweet", 26, "Razor Leaf", "Play Rough", "Rapid Spin", "Draining Kiss"),
			Create("Tsareena", 29, "Trop Kick", "High Jump Kick", "Play Rough", "Triple Axel"),
			Create("Maushold", 31, "Population Bomb", "Bite", "Play Rough", "Bullet Seed")
		);

		// Morris: JojaMart Director [MEGA EVOLUTION ACE]
		RegisterTeam("Morris",
			Create("Persian", 30, "Pay Day", "Night Slash", "Power Gem", "Nasty Plot"),
			Create("Weezing", 31, "Sludge Bomb", "Flamethrower", "Dark Pulse", "Will-O-Wisp"),
			Create("Porygon-Z", 32, "Tri Attack", "Thunderbolt", "Ice Beam", "Dark Pulse"),
			Create("Garbodor", 33, "Gunk Shot", "Stomping Tantrum", "Seed Bomb", "Body Press"),
			Create("Revavroom", 34, "Iron Head", "Gunk Shot", "High Horsepower", "Overheat"),
			CreateMega("Mega Aggron", 36, "Heavy Slam", "Earthquake", "Stone Edge", "Body Press")
		);

		// Gus: Stardrop Saloon Chef & Hospitality (Gen 1-9)
		RegisterTeam("Gus",
			Create("Snorlax", 26, "Body Slam", "Earthquake", "Crunch", "Heat Crash"),
			Create("Lickilicky", 27, "Body Slam", "Knock Off", "Flamethrower", "Ice Beam"),
			Create("Simisear", 28, "Flamethrower", "Grass Knot", "Rock Slide", "Nasty Plot"),
			Create("Slurpuff", 29, "Play Rough", "Flamethrower", "Surf", "Energy Ball"),
			Create("Appletun", 30, "Apple Acid", "Dragon Pulse", "Body Press", "Recover"),
			Create("Garganacl", 31, "Salt Cure", "Rock Slide", "Body Press", "Earthquake")
		);

		// Haley: Fashion, Sunflowers & Tropical Aesthetic (Gen 1-9)
		RegisterTeam("Haley",
			Create("Sunflora", 25, "Solar Beam", "Earth Power", "Sludge Bomb", "Sunny Day"),
			Create("Bellossom", 26, "Giga Drain", "Moonblast", "Quiver Dance", "Fiery Dance"),
			Create("Lopunny", 27, "Return", "Close Combat", "Ice Punch", "Play Rough"),
			Create("Furfrou", 28, "Body Slam", "Sucker Punch", "Zen Headbutt", "Wild Charge"),
			Create("Primarina", 29, "Sparkling Aria", "Moonblast", "Ice Beam", "Energy Ball"),
			Create("Florges", 31, "Moonblast", "Energy Ball", "Psychic", "Synthesis")
		);

		// Emily: Crystals, Tailoring, Parrots & Astral Dreams (Gen 1-9)
		RegisterTeam("Emily",
			Create("Espeon", 26, "Psychic", "Dazzling Gleam", "Shadow Ball", "Morning Sun"),
			Create("Medicham", 27, "High Jump Kick", "Zen Headbutt", "Ice Punch", "Bullet Punch"),
			Create("Altaria", 28, "Moonblast", "Dragon Pulse", "Flamethrower", "Roost"),
			Create("Leavanny", 29, "Leaf Blade", "X-Scissor", "Sticky Web", "Swords Dance"),
			Create("Oricorio", 30, "Revelation Dance", "Air Slash", "Quiver Dance", "Roost"),
			Create("Espathra", 31, "Lumina Crash", "Dazzling Gleam", "Shadow Ball", "Roost")
		);

		// Leah: Sculpting, Woodland Cabin & Foraged Truffles (Gen 1-9)
		RegisterTeam("Leah",
			Create("Smeargle", 25, "Extreme Speed", "Spore", "Flamethrower", "Surf"),
			Create("Sudowoodo", 26, "Head Smash", "Wood Hammer", "Earthquake", "Sucker Punch"),
			Create("Breloom", 27, "Mach Punch", "Seed Bomb", "Spore", "Rock Slide"),
			Create("Trevenant", 28, "Horn Leech", "Poltergeist", "Will-O-Wisp", "Rock Slide"),
			Create("Lurantis", 29, "Leaf Blade", "Superpower", "Night Slash", "Synthesis"),
			Create("Grafaiai", 31, "Poison Jab", "Knock Off", "Double-Edge", "U-turn")
		);

		// Elliott: Romantic Novelist, Piano & Coastal Retreat (Gen 1-9)
		RegisterTeam("Elliott",
			Create("Dragonair", 26, "Dragon Pulse", "Flamethrower", "Ice Beam", "Thunderbolt"),
			Create("Swanna", 27, "Surf", "Hurricane", "Ice Beam", "Roost"),
			Create("Chandelure", 28, "Flamethrower", "Shadow Ball", "Energy Ball", "Will-O-Wisp"),
			Create("Polteageist", 29, "Shadow Ball", "Giga Drain", "Stored Power", "Shell Smash"),
			Create("Quaquaval", 30, "Aqua Step", "Close Combat", "Brave Bird", "Ice Spinner"),
			Create("Milotic", 32, "Scald", "Ice Beam", "Dragon Pulse", "Recover")
		);

		// Demetrius: Ecological Scientist & Researcher [MEGA EVOLUTION ACE]
		RegisterTeam("Demetrius",
			Create("Magnezone", 31, "Thunderbolt", "Flash Cannon", "Tri Attack", "Volt Switch"),
			Create("Cradily", 32, "Giga Drain", "Ancient Power", "Earth Power", "Recover"),
			Create("Volcarona", 33, "Fiery Dance", "Bug Buzz", "Giga Drain", "Quiver Dance"),
			Create("genesect", 34, "Bug Buzz", "Flash Cannon", "Flamethrower", "Ice Beam"),
			Create("Armarouge", 35, "Armor Cannon", "Psychic", "Energy Ball", "Aura Sphere"),
			CreateMega("Mega Metagross", 37, "Meteor Mash", "Zen Headbutt", "Earthquake", "Bullet Punch")
		);

		// Robin: Master Carpenter & Builder (Gen 1-9)
		RegisterTeam("Robin",
			Create("Scizor", 27, "Bullet Punch", "X-Scissor", "Swords Dance", "Brick Break"),
			Create("Bibarel", 26, "Waterfall", "Superpower", "Crunch", "Quick Attack"),
			Create("Conkeldurr", 28, "Drain Punch", "Mach Punch", "Stone Edge", "Ice Punch"),
			Create("Chesnaught", 29, "Wood Hammer", "Body Press", "Stone Edge", "Spiky Shield"),
			Create("Kleavor", 30, "Stone Axe", "X-Scissor", "Close Combat", "Night Slash"),
			Create("Tinkaton", 32, "Gigaton Hammer", "Play Rough", "Rock Slide", "Knock Off")
		);

		// Caroline: Sunroom Tea Leaf Cultivator (Gen 1-9)
		RegisterTeam("Caroline",
			Create("Bellossom", 25, "Giga Drain", "Moonblast", "Quiver Dance", "Stun Spore"),
			Create("Roserade", 27, "Energy Ball", "Sludge Bomb", "Dazzling Gleam", "Shadow Ball"),
			Create("Vileplume", 26, "Giga Drain", "Sludge Bomb", "Moonblast", "Sleep Powder"),
			Create("Lilligant", 28, "Energy Ball", "Pollen Puff", "Quiver Dance", "Sleep Powder"),
			Create("Polteageist", 29, "Shadow Ball", "Giga Drain", "Shell Smash", "Stored Power"),
			Create("Sinistcha", 31, "Matcha Gotcha", "Shadow Ball", "Calm Mind", "Strength Sap")
		);

		// Jodi: Household Strategist & Devoted Mother (Gen 1-9)
		RegisterTeam("Jodi",
			Create("Kangaskhan", 26, "Body Slam", "Earthquake", "Crunch", "Sucker Punch"),
			Create("Blissey", 27, "Hyper Voice", "Flamethrower", "Ice Beam", "Soft-Boiled"),
			Create("Cinccino", 28, "Tail Slap", "Bullet Seed", "Rock Blast", "Play Rough"),
			Create("Talonflame", 29, "Brave Bird", "Flare Blitz", "U-turn", "Roost"),
			Create("Indeedee", 30, "Psychic", "Dazzling Gleam", "Mystical Fire", "Shadow Ball"),
			Create("Maushold", 31, "Population Bomb", "Play Rough", "Bite", "Tidy Up")
		);

		// Kent: Combat Veteran (Gen 1-9)
		RegisterTeam("Kent",
			Create("Electrode", 27, "Thunderbolt", "Foul Play", "Volt Switch", "Signal Beam"),
			Create("Machamp", 28, "Close Combat", "Stone Edge", "Knock Off", "Bullet Punch"),
			Create("Arcanine", 29, "Flare Blitz", "Extreme Speed", "Wild Charge", "Morning Sun"),
			Create("Braviary", 30, "Brave Bird", "Close Combat", "Crush Claw", "U-turn"),
			Create("Clawitzer", 31, "Water Pulse", "Aura Sphere", "Dark Pulse", "Dragon Pulse"),
			Create("Kingambit", 33, "Kowtow Cleave", "Iron Head", "Sucker Punch", "Brick Break")
		);

		// George: Former Coal Miner & Dynamite Expert (Gen 1-9)
		RegisterTeam("George",
			Create("Torkoal", 26, "Lava Plume", "Earth Power", "Body Press", "Stealth Rock"),
			Create("Golem", 27, "Earthquake", "Stone Edge", "Explosion", "Heavy Slam"),
			Create("Slowbro", 28, "Scald", "Psychic", "Ice Beam", "Slack Off"),
			Create("Gigalith", 29, "Stone Edge", "Earthquake", "Heavy Slam", "Body Press"),
			Create("Drampa", 30, "Hyper Voice", "Dragon Pulse", "Flamethrower", "Energy Ball"),
			Create("Coalossal", 31, "Heat Crash", "Stone Edge", "Earth Power", "Will-O-Wisp")
		);

		// Evelyn: Pelican Town's Gardener & Baker (Gen 1-9)
		RegisterTeam("Evelyn",
			Create("Meganium", 26, "Energy Ball", "Dazzling Gleam", "Earthquake", "Synthesis"),
			Create("Roserade", 27, "Giga Drain", "Sludge Bomb", "Dazzling Gleam", "Toxic Spikes"),
			Create("Florges", 28, "Moonblast", "Energy Ball", "Psychic", "Wish"),
			Create("Slurpuff", 29, "Play Rough", "Flamethrower", "Energy Ball", "Surf"),
			Create("Appletun", 30, "Apple Acid", "Dragon Pulse", "Body Press", "Recover"),
			Create("Dachsbun", 31, "Play Rough", "Body Press", "Crunch", "Wish")
		);

		// Pam: Calico Bus Driver & Pale Ale Connoisseur (Gen 1-9)
		RegisterTeam("Pam",
			Create("Snorlax", 26, "Body Slam", "Crunch", "Earthquake", "Heavy Slam"),
			Create("Slaking", 28, "Double-Edge", "Earthquake", "Night Slash", "Hammer Arm"),
			Create("Weezing", 27, "Sludge Bomb", "Flamethrower", "Thunderbolt", "Will-O-Wisp"),
			Create("Krookodile", 29, "Earthquake", "Crunch", "Stone Edge", "Close Combat"),
			Create("Mudsdale", 30, "High Horsepower", "Heavy Slam", "Body Press", "Rock Slide"),
			Create("Revavroom", 32, "Iron Head", "Gunk Shot", "Overheat", "High Horsepower")
		);

		// Jas: Fairy Toys & Jump-Rope Champion (Gen 1-9)
		RegisterTeam("Jas",
			Create("Togetic", 22, "Dazzling Gleam", "Air Slash", "Flamethrower", "Roost"),
			Create("Clefairy", 23, "Moonblast", "Flamethrower", "Thunderbolt", "Moonlight"),
			Create("Sylveon", 24, "Moonblast", "Quick Attack", "Mystical Fire", "Shadow Ball"),
			Create("Galarian Ponyta", 25, "Psybeam", "Play Rough", "Dazzling Gleam", "Morning Sun"),
			Create("Mimikyu", 26, "Play Rough", "Shadow Claw", "Shadow Sneak", "Swords Dance"),
			Create("Tinkatuff", 27, "Play Rough", "Flash Cannon", "Knock Off", "Rock Slide")
		);

		// Vincent: Bug Hunter & Beach Explorer (Gen 1-9)
		RegisterTeam("Vincent",
			Create("Pikachu", 22, "Thunderbolt", "Iron Tail", "Quick Attack", "Volt Tackle"),
			Create("Heracross", 23, "Megahorn", "Close Combat", "Rock Slide", "Night Slash"),
			Create("Torterra", 24, "Wood Hammer", "Earthquake", "Crunch", "Rock Slide"),
			Create("Crustle", 25, "X-Scissor", "Stone Edge", "Earthquake", "Shell Smash"),
			Create("Vikavolt", 26, "Thunderbolt", "Bug Buzz", "Energy Ball", "Air Slash"),
			Create("Lokix", 27, "First Impression", "Leech Life", "Sucker Punch", "Axe Kick")
		);

		// Krobus: Shadow Merchant of the Sewers [MEGA EVOLUTION ACE]
		RegisterTeam("Krobus",
			Create("Sableye", 32, "Foul Play", "Shadow Claw", "Power Gem", "Will-O-Wisp"),
			Create("Dusknoir", 33, "Poltergeist", "Shadow Sneak", "Ice Punch", "Earthquake"),
			Create("Zoroark", 34, "Night Daze", "Flamethrower", "Sludge Bomb", "Nasty Plot"),
			Create("Mimikyu", 35, "Play Rough", "Shadow Claw", "Shadow Sneak", "Swords Dance"),
			Create("Annihilape", 36, "Rage Fist", "Drain Punch", "Stone Edge", "Bulk Up"),
			CreateMega("Mega Gengar", 38, "Shadow Ball", "Sludge Wave", "Dazzling Gleam", "Thunderbolt")
		);

		// Dwarf: Ancient Underground Miner [MEGA EVOLUTION ACE]
		RegisterTeam("Dwarf",
			Create("Dugtrio", 31, "Earthquake", "Stone Edge", "Sucker Punch", "Sludge Bomb"),
			Create("Excadrill", 32, "Earthquake", "Iron Head", "Rock Slide", "Swords Dance"),
			Create("Carbink", 33, "Moonblast", "Power Gem", "Body Press", "Earth Power"),
			Create("Coalossal", 34, "Heat Crash", "Stone Edge", "Earth Power", "Will-O-Wisp"),
			Create("Glimmora", 35, "Power Gem", "Sludge Wave", "Earth Power", "Dazzling Gleam"),
			CreateMega("Mega Steelix", 37, "Heavy Slam", "Earthquake", "Stone Edge", "Ice Fang")
		);

		// Lewis: Mayor of Pelican Town [MEGA EVOLUTION ACE]
		RegisterTeam("Lewis",
			Create("Stoutland", 30, "Body Slam", "Play Rough", "Crunch", "High Horsepower"),
			Create("Persian", 31, "Pay Day", "Power Gem", "Nasty Plot", "Dark Pulse"),
			Create("Perrserker", 32, "Iron Head", "Close Combat", "Crunch", "Fake Out"),
			Create("Corviknight", 33, "Brave Bird", "Iron Head", "Body Press", "Roost"),
			Create("Gholdengo", 35, "Make It Rain", "Shadow Ball", "Power Gem", "Nasty Plot"),
			CreateMega("Mega Kangaskhan", 37, "Body Slam", "Earthquake", "Crunch", "Sucker Punch")
		);

		// Sandy: Calico Desert Oasis Shopkeeper (Gen 1-9)
		RegisterTeam("Sandy",
			Create("Maractus", 28, "Energy Ball", "Dazzling Gleam", "Sucker Punch", "Spiky Shield"),
			Create("Cacturne", 29, "Energy Ball", "Dark Pulse", "Sucker Punch", "Destiny Bond"),
			Create("Sigilyph", 30, "Psychic", "Air Slash", "Heat Wave", "Dazzling Gleam"),
			Create("Altaria", 31, "Moonblast", "Dragon Pulse", "Flamethrower", "Roost"),
			Create("Flygon", 32, "Earthquake", "Dragon Claw", "Fire Punch", "U-turn"),
			Create("Espathra", 34, "Lumina Crash", "Dazzling Gleam", "Shadow Ball", "Roost")
		);

		// Leo: Child of Ginger Island & Parrot Whisperer (Gen 1-9)
		RegisterTeam("Leo",
			Create("Chatot", 28, "Boomburst", "Chatter", "Heat Wave", "Nasty Plot"),
			Create("Tropius", 29, "Air Slash", "Giga Drain", "Dragon Pulse", "Roost"),
			Create("Hawlucha", 30, "Flying Press", "Acrobatics", "Stone Edge", "Swords Dance"),
			Create("Toucannon", 31, "Brave Bird", "Bullet Seed", "Rock Blast", "Overheat"),
			Create("Decidueye", 32, "Spirit Shackle", "Leaf Blade", "Brave Bird", "Sucker Punch"),
			Create("Kilowattrel", 34, "Thunderbolt", "Hurricane", "Volt Switch", "Weather Ball")
		);

		// Marlon: Leader of the Adventurer's Guild (Gen 1-9)
		RegisterTeam("Marlon",
			Create("Escavalier", 34, "Megahorn", "Iron Head", "Close Combat", "Knock Off"),
			Create("Haxorus", 35, "Outrage", "Earthquake", "Iron Head", "First Impression"),
			Create("Aegislash", 36, "Shadow Ball", "Flash Cannon", "Sacred Sword", "Shadow Sneak"),
			Create("Golisopod", 37, "First Impression", "Liquidation", "Leech Life", "Aqua Jet"),
			Create("Ceruledge", 38, "Bitter Blade", "Poltergeist", "Close Combat", "Shadow Sneak"),
			Create("Kingambit", 40, "Kowtow Cleave", "Iron Head", "Sucker Punch", "Swords Dance")
		);

		// Mr. Qi: Master of the Walnut Room & Casino (Gen 1-9)
		RegisterTeam("Qi",
			Create("Porygon-Z", 46, "Tri Attack", "Thunderbolt", "Ice Beam", "Dark Pulse"),
			Create("Deoxys", 47, "Psycho Boost", "Superpower", "Ice Beam", "Thunderbolt"),
			Create("Hoopa", 48, "Hyperspace Hole", "Shadow Ball", "Psychic", "Thunderbolt"),
			Create("Dragapult", 49, "Dragon Darts", "Phantom Force", "Flamethrower", "U-turn"),
			Create("Miraidon", 50, "Electro Drift", "Draco Meteor", "Dazzling Gleam", "Overheat"),
			Create("Arceus", 52, "Extreme Speed", "Earthquake", "Shadow Ball", "Recover")
		);

		// Goblin / Henchman: Witch's Swamp Guardian (Gen 1-9 Swamp & Dark)
		var goblinTeam = new[]
		{
			Create("Grimmsnarl", 36, "Spirit Break", "Darkest Lariat", "Play Rough", "Thunder Wave"),
			Create("Sableye", 37, "Knock Off", "Shadow Claw", "Power Gem", "Will-O-Wisp"),
			Create("Toxicroak", 38, "Poison Jab", "Drain Punch", "Sucker Punch", "Gunk Shot"),
			Create("Seismitoad", 39, "Hydro Pump", "Earth Power", "Sludge Wave", "Drain Punch"),
			Create("Clodsire", 40, "Earthquake", "Poison Jab", "Megahorn", "Recover"),
			Create("Gengar", 42, "Shadow Ball", "Sludge Bomb", "Focus Blast", "Dark Pulse")
		};
		RegisterTeam("Henchman", goblinTeam);
		RegisterTeam("Goblin", goblinTeam);

		// Playable Trainer Avatars (PvP Simulation)
		var brendanTeam = new[]
		{
			Create("Sceptile", 34, "Leaf Blade", "Dragon Pulse", "X-Scissor", "Quick Attack"),
			Create("Swampert", 33, "Earthquake", "Waterfall", "Ice Punch", "Rock Slide"),
			Create("Blaziken", 33, "Blaze Kick", "Close Combat", "Thunder Punch", "Brave Bird"),
			Create("Aggron", 32, "Iron Head", "Stone Edge", "Earthquake", "Body Press"),
			Create("Salamence", 34, "Dragon Claw", "Flamethrower", "Fly", "Crunch"),
			Create("Metagross", 35, "Meteor Mash", "Zen Headbutt", "Earthquake", "Bullet Punch")
		};
		RegisterTeam("Brendan", brendanTeam);
		RegisterTeam("Brendan (RS)", brendanTeam);

		var mayTeam = new[]
		{
			Create("Blaziken", 34, "Blaze Kick", "Close Combat", "Brave Bird", "Thunder Punch"),
			Create("Swampert", 33, "Waterfall", "Earthquake", "Ice Beam", "Rock Slide"),
			Create("Sceptile", 33, "Giga Drain", "Dragon Pulse", "Leaf Blade", "Quick Attack"),
			Create("Gardevoir", 33, "Moonblast", "Psychic", "Shadow Ball", "Thunderbolt"),
			Create("Altaria", 32, "Dragon Pulse", "Moonblast", "Flamethrower", "Roost"),
			Create("Milotic", 34, "Surf", "Ice Beam", "Recover", "Dragon Pulse")
		};
		RegisterTeam("May", mayTeam);
		RegisterTeam("May (RS)", mayTeam);

		RegisterTeam("Ash",
			Create("Pikachu", 35, "Thunderbolt", "Iron Tail", "Volt Tackle", "Quick Attack"),
			Create("Charizard", 34, "Flamethrower", "Dragon Claw", "Air Slash", "Overheat"),
			Create("Snorlax", 33, "Body Slam", "Heavy Slam", "Rest", "Crunch"),
			Create("Gengar", 32, "Shadow Ball", "Sludge Bomb", "Dazzling Gleam", "Thunderbolt"),
			Create("Lucario", 33, "Aura Sphere", "Close Combat", "Bullet Punch", "Dragon Pulse"),
			Create("Greninja", 35, "Water Shuriken", "Hydro Pump", "Dark Pulse", "Ice Beam")
		);

		RegisterTeam("Leaf",
			Create("Venusaur", 34, "Solar Beam", "Sludge Bomb", "Giga Drain", "Sleep Powder"),
			Create("Blastoise", 33, "Hydro Pump", "Ice Beam", "Flash Cannon", "Bite"),
			Create("Clefable", 32, "Moonblast", "Flamethrower", "Ice Beam", "Moonlight"),
			Create("Kangaskhan", 33, "Dizzy Punch", "Crunch", "Sucker Punch", "Earthquake"),
			Create("Arcanine", 34, "Flare Blitz", "Extreme Speed", "Wild Charge", "Play Rough"),
			Create("Nidoqueen", 32, "Earth Power", "Sludge Wave", "Ice Beam", "Thunderbolt")
		);

		RegisterTeam("Steven",
			Create("Skarmory", 33, "Steel Wing", "Air Slash", "Spikes", "Roost"),
			Create("Aggron", 34, "Iron Head", "Stone Edge", "Earthquake", "Thunder Punch"),
			Create("Claydol", 32, "Earth Power", "Psychic", "Ancient Power", "Ice Beam"),
			Create("Cradily", 33, "Giga Drain", "Ancient Power", "Energy Ball", "Recover"),
			Create("Armaldo", 33, "X-Scissor", "Rock Slide", "Aqua Jet", "Crush Claw"),
			Create("Metagross", 36, "Meteor Mash", "Zen Headbutt", "Earthquake", "Bullet Punch")
		);

		RegisterTeam("Wally",
			Create("Roserade", 32, "Giga Drain", "Sludge Bomb", "Dazzling Gleam", "Shadow Ball"),
			Create("Magnezone", 33, "Thunderbolt", "Flash Cannon", "Tri Attack", "Volt Switch"),
			Create("Altaria", 32, "Dragon Pulse", "Moonblast", "Flamethrower", "Roost"),
			Create("Gardevoir", 33, "Psychic", "Moonblast", "Shadow Ball", "Thunderbolt"),
			Create("Garchomp", 34, "Earthquake", "Dragon Claw", "Rock Slide", "Poison Jab"),
			Create("Gallade", 35, "Psycho Cut", "Close Combat", "Leaf Blade", "Night Slash")
		);

		var lucasTeam = new[]
		{
			Create("Torterra", 34, "Wood Hammer", "Earthquake", "Stone Edge", "Crunch"),
			Create("Infernape", 33, "Flare Blitz", "Close Combat", "Mach Punch", "Thunder Punch"),
			Create("Empoleon", 33, "Hydro Pump", "Flash Cannon", "Ice Beam", "Drill Peck"),
			Create("Staraptor", 33, "Brave Bird", "Close Combat", "Quick Attack", "U-turn"),
			Create("Lucario", 34, "Aura Sphere", "Flash Cannon", "Dragon Pulse", "Extreme Speed"),
			Create("Garchomp", 35, "Dragon Claw", "Earthquake", "Stone Edge", "Crunch")
		};
		RegisterTeam("Lucas", lucasTeam);
		RegisterTeam("Lucas (DP)", lucasTeam);

		var dawnTeam = new[]
		{
			Create("Empoleon", 34, "Surf", "Flash Cannon", "Ice Beam", "Aqua Jet"),
			Create("Infernape", 33, "Flamethrower", "Close Combat", "Grass Knot", "Mach Punch"),
			Create("Torterra", 33, "Giga Drain", "Earth Power", "Rock Slide", "Crunch"),
			Create("Togekiss", 33, "Air Slash", "Dazzling Gleam", "Aura Sphere", "Roost"),
			Create("Mamoswine", 33, "Icicle Crash", "Earthquake", "Ice Shard", "Superpower"),
			Create("Lopunny", 32, "Return", "High Jump Kick", "Ice Punch", "Quick Attack")
		};
		RegisterTeam("Dawn", dawnTeam);
		RegisterTeam("Dawn (DP)", dawnTeam);

		RegisterTeam("Barry",
			Create("Staraptor", 33, "Brave Bird", "Close Combat", "Quick Attack", "U-turn"),
			Create("Heracross", 33, "Megahorn", "Close Combat", "Stone Edge", "Night Slash"),
			Create("Snorlax", 34, "Body Slam", "Heavy Slam", "Crunch", "Earthquake"),
			Create("Roserade", 32, "Giga Drain", "Sludge Bomb", "Shadow Ball", "Dazzling Gleam"),
			Create("Rapidash", 32, "Flare Blitz", "Megahorn", "High Horsepower", "Poison Jab"),
			Create("Empoleon", 35, "Hydro Pump", "Flash Cannon", "Ice Beam", "Drill Peck")
		);

		RegisterTeam("Riley",
			Create("Lucario", 35, "Aura Sphere", "Meteor Mash", "Close Combat", "Extreme Speed"),
			Create("Dragonite", 34, "Dragon Claw", "Extreme Speed", "Fire Punch", "Outrage"),
			Create("Salamence", 34, "Dragon Claw", "Flamethrower", "Earthquake", "Crunch"),
			Create("Tyranitar", 34, "Stone Edge", "Crunch", "Earthquake", "Fire Fang"),
			Create("Metagross", 34, "Meteor Mash", "Zen Headbutt", "Bullet Punch", "Earthquake"),
			Create("Gengar", 33, "Shadow Ball", "Sludge Bomb", "Thunderbolt", "Focus Blast")
		);

		RegisterTeam("Cheryl",
			Create("Blissey", 35, "Hyper Voice", "Soft-Boiled", "Dazzling Gleam", "Thunder Wave"),
			Create("Wobbuffet", 32, "Counter", "Mirror Coat", "Encore", "Destiny Bond"),
			Create("Drifblim", 33, "Shadow Ball", "Acrobatics", "Thunderbolt", "Will-O-Wisp"),
			Create("Wailord", 33, "Water Spout", "Ice Beam", "Bounce", "Heavy Slam"),
			Create("Hariyama", 33, "Close Combat", "Knock Off", "Heavy Slam", "Bullet Punch"),
			Create("Snorlax", 34, "Body Slam", "Crunch", "Rest", "Sleep Talk")
		);

		RegisterTeam("Marley",
			Create("Ninjask", 33, "X-Scissor", "Aerial Ace", "U-turn", "Dig"),
			Create("Crobat", 34, "Cross Poison", "Brave Bird", "Leech Life", "U-turn"),
			Create("Electrode", 33, "Thunderbolt", "Volt Switch", "Foul Play", "Explosion"),
			Create("Weavile", 34, "Night Slash", "Ice Spinner", "Poison Jab", "Ice Shard"),
			Create("Jolteon", 33, "Thunderbolt", "Shadow Ball", "Volt Switch", "Quick Attack"),
			Create("Arcanine", 35, "Flare Blitz", "Extreme Speed", "Wild Charge", "Play Rough")
		);

		RegisterTeam("Buck",
			Create("Claydol", 34, "Earth Power", "Psychic", "Ancient Power", "Ice Beam"),
			Create("Shuckle", 33, "Toxic", "Infestation", "Recover", "Stone Edge"),
			Create("Torkoal", 33, "Lava Plume", "Earth Power", "Body Press", "Solar Beam"),
			Create("Dusknoir", 34, "Shadow Sneak", "Fire Punch", "Ice Punch", "Thunder Punch"),
			Create("Steelix", 34, "Iron Head", "Earthquake", "Rock Slide", "Heavy Slam"),
			Create("Umbreon", 33, "Foul Play", "Moonlight", "Toxic", "Dark Pulse")
		);

		RegisterTeam("Mira",
			Create("Alakazam", 35, "Psychic", "Focus Blast", "Shadow Ball", "Dazzling Gleam"),
			Create("Gengar", 34, "Shadow Ball", "Sludge Bomb", "Thunderbolt", "Energy Ball"),
			Create("Espeon", 33, "Psychic", "Dazzling Gleam", "Shadow Ball", "Morning Sun"),
			Create("Porygon-Z", 34, "Tri Attack", "Thunderbolt", "Ice Beam", "Dark Pulse"),
			Create("Magnezone", 34, "Thunderbolt", "Flash Cannon", "Tri Attack", "Volt Switch"),
			Create("Togekiss", 34, "Air Slash", "Dazzling Gleam", "Aura Sphere", "Flamethrower")
		);

		RegisterTeam("PokeDude",
			Create("Snorlax", 34, "Body Slam", "Crunch", "Earthquake", "Heavy Slam"),
			Create("Dragonite", 35, "Dragon Claw", "Thunderbolt", "Flamethrower", "Ice Beam"),
			Create("Machamp", 33, "Close Combat", "Bullet Punch", "Rock Slide", "Knock Off"),
			Create("Gengar", 33, "Shadow Ball", "Sludge Bomb", "Thunderbolt", "Hypnosis"),
			Create("Lapras", 33, "Surf", "Ice Beam", "Thunderbolt", "Body Slam"),
			Create("Arcanine", 34, "Flamethrower", "Extreme Speed", "Bite", "Wild Charge")
		);

		RegisterTeam("Old Man",
			Create("Weedle", 25, "Poison Sting", "Bug Bite", "Electroweb", "String Shot"),
			Create("Butterfree", 30, "Bug Buzz", "Air Slash", "Sleep Powder", "Psybeam"),
			Create("Beedrill", 30, "Poison Jab", "Twineedle", "Drill Run", "X-Scissor"),
			Create("Pidgeot", 32, "Air Slash", "Quick Attack", "Hurricane", "Steel Wing"),
			Create("Raticate", 31, "Super Fang", "Hyper Fang", "Crunch", "Sucker Punch"),
			Create("Raichu", 32, "Thunderbolt", "Quick Attack", "Iron Tail", "Focus Blast")
		);

		// Playable Protagonists / Player Avatars (PvP Simulation)
		RegisterTeam("Red",
			Create("Pikachu", 50, "Volt Tackle", "Iron Tail", "Thunderbolt", "Quick Attack"),
			Create("Charizard", 48, "Flamethrower", "Air Slash", "Dragon Pulse", "Blast Burn"),
			Create("Blastoise", 48, "Hydro Cannon", "Flash Cannon", "Ice Beam", "Aqua Jet"),
			Create("Venusaur", 48, "Frenzy Plant", "Sludge Bomb", "Giga Drain", "Sleep Powder"),
			Create("Snorlax", 47, "Body Slam", "Heavy Slam", "Crunch", "Rest"),
			Create("Lapras", 47, "Surf", "Ice Beam", "Psychic", "Thunderbolt")
		);

		var ethanTeam = new[]
		{
			Create("Typhlosion", 42, "Eruption", "Flamethrower", "Focus Blast", "Shadow Ball"),
			Create("Ampharos", 40, "Thunderbolt", "Power Gem", "Dragon Pulse", "Thunder Wave"),
			Create("Sudowoodo", 39, "Stone Edge", "Wood Hammer", "Earthquake", "Sucker Punch"),
			Create("Politoed", 40, "Surf", "Ice Beam", "Focus Blast", "Hypnosis"),
			Create("Donphan", 40, "Earthquake", "Rollout", "Knock Off", "Play Rough"),
			Create("Togekiss", 41, "Air Slash", "Dazzling Gleam", "Aura Sphere", "Roost")
		};
		RegisterTeam("Ethan", ethanTeam);

		var lyraTeam = new[]
		{
			Create("Meganium", 42, "Energy Ball", "Ancient Power", "Body Slam", "Synthesis"),
			Create("Azumarill", 41, "Play Rough", "Aqua Tail", "Superpower", "Aqua Jet"),
			Create("Girafarig", 40, "Psychic", "Hyper Voice", "Shadow Ball", "Thunderbolt"),
			Create("Bellossom", 39, "Petal Dance", "Moonblast", "Sludge Bomb", "Sleep Powder"),
			Create("Espeon", 41, "Psychic", "Dazzling Gleam", "Shadow Ball", "Morning Sun"),
			Create("Ambipom", 40, "Double Hit", "Fake Out", "U-turn", "Acrobatics")
		};
		RegisterTeam("Lyra", lyraTeam);

		var hilbertTeam = new[]
		{
			Create("Samurott", 42, "Razor Shell", "Megahorn", "Aqua Jet", "Night Slash"),
			Create("Emboar", 42, "Flare Blitz", "Hammer Arm", "Head Smash", "Wild Charge"),
			Create("Serperior", 42, "Leaf Storm", "Dragon Pulse", "Giga Drain", "Leech Seed"),
			Create("Archeops", 41, "Acrobatics", "Stone Edge", "Earthquake", "U-turn"),
			Create("Zoroark", 41, "Night Daze", "Flamethrower", "Focus Blast", "Sludge Bomb"),
			Create("Haxorus", 43, "Dragon Claw", "Earthquake", "Poison Jab", "Dragon Dance")
		};
		RegisterTeam("Hilbert", hilbertTeam);
		RegisterTeam("Hilda", hilbertTeam);

		var nateTeam = new[]
		{
			Create("Serperior", 42, "Leaf Storm", "Giga Drain", "Dragon Pulse", "Toxic"),
			Create("Lucario", 42, "Aura Sphere", "Flash Cannon", "Dragon Pulse", "Extreme Speed"),
			Create("Arcanine", 41, "Flare Blitz", "Extreme Speed", "Wild Charge", "Crunch"),
			Create("Flygon", 41, "Earthquake", "Dragon Claw", "Rock Slide", "U-turn"),
			Create("Zoroark", 41, "Dark Pulse", "Flamethrower", "Focus Blast", "Nasty Plot"),
			Create("Braviary", 42, "Brave Bird", "Superpower", "Crush Claw", "Roost")
		};
		RegisterTeam("Nate", nateTeam);
		RegisterTeam("Rosa", nateTeam);

		var calemTeam = new[]
		{
			Create("Greninja", 43, "Hydro Pump", "Dark Pulse", "Ice Beam", "Water Shuriken"),
			Create("Delphox", 43, "Mystical Fire", "Psychic", "Shadow Ball", "Dazzling Gleam"),
			Create("Chesnaught", 43, "Wood Hammer", "Hammer Arm", "Spiky Shield", "Drain Punch"),
			Create("Talonflame", 42, "Brave Bird", "Flare Blitz", "Steel Wing", "Roost"),
			Create("Aegislash", 43, "Iron Head", "Shadow Claw", "Sacred Sword", "King's Shield"),
			Create("Sylveon", 42, "Moonblast", "Hyper Voice", "Psyshock", "Shadow Ball")
		};
		RegisterTeam("Calem", calemTeam);
		RegisterTeam("Serena", calemTeam);

		var elioTeam = new[]
		{
			Create("Decidueye", 43, "Spirit Shackle", "Leaf Blade", "Brave Bird", "Sucker Punch"),
			Create("Incineroar", 43, "Darkest Lariat", "Flare Blitz", "Close Combat", "U-turn"),
			Create("Primarina", 43, "Sparkling Aria", "Moonblast", "Psychic", "Ice Beam"),
			Create("Lycanroc", 42, "Accelerock", "Stone Edge", "Crunch", "Drill Run"),
			Create("Vikavolt", 42, "Thunderbolt", "Bug Buzz", "Flash Cannon", "Energy Ball"),
			Create("Kommo-o", 44, "Clanging Scales", "Close Combat", "Poison Jab", "Dragon Dance")
		};
		RegisterTeam("Elio", elioTeam);
		RegisterTeam("Selene", elioTeam);

		var victorTeam = new[]
		{
			Create("Cinderace", 44, "Pyro Ball", "High Jump Kick", "U-turn", "Gunk Shot"),
			Create("Inteleon", 44, "Snipe Shot", "Ice Beam", "Dark Pulse", "Air Slash"),
			Create("Rillaboom", 44, "Drum Beating", "Wood Hammer", "High Horsepower", "Knock Off"),
			Create("Corviknight", 43, "Brave Bird", "Iron Head", "Body Press", "Roost"),
			Create("Toxtricity", 43, "Overdrive", "Sludge Bomb", "Boomburst", "Volt Switch"),
			Create("Dragapult", 45, "Dragon Darts", "Phantom Force", "Flamethrower", "U-turn")
		};
		RegisterTeam("Victor", victorTeam);
		RegisterTeam("Gloria", victorTeam);

		var florianTeam = new[]
		{
			Create("Meowscarada", 45, "Flower Trick", "Knock Off", "U-turn", "Play Rough"),
			Create("Skeledirge", 45, "Torch Song", "Shadow Ball", "Earth Power", "Slack Off"),
			Create("Quaquaval", 45, "Aqua Step", "Close Combat", "Brave Bird", "Ice Spinner"),
			Create("Baxcalibur", 45, "Glaive Rush", "Icicle Crash", "Earthquake", "Dragon Dance"),
			Create("Tinkaton", 44, "Gigaton Hammer", "Play Rough", "Knock Off", "Stealth Rock"),
			Create("Gholdengo", 45, "Make It Rain", "Shadow Ball", "Focus Blast", "Nasty Plot")
		};
		RegisterTeam("Florian", florianTeam);
		RegisterTeam("Juliana", florianTeam);
	}

	private static PokemonInstance Create(string speciesName, int level, params string[] moves)
	{
		PokemonSpecies species = PokemonSpecies.Get(speciesName) ?? PokemonSpecies.Get("Pikachu");
		return new PokemonInstance(species, level, moves);
	}

	private static PokemonInstance CreateMega(string megaSpeciesName, int level, params string[] moves)
	{
		PokemonSpecies species = PokemonSpecies.Get(megaSpeciesName);
		if (species == null && !string.IsNullOrWhiteSpace(megaSpeciesName) && megaSpeciesName.StartsWith("Mega ", StringComparison.OrdinalIgnoreCase))
		{
			string baseName = megaSpeciesName.Substring(5).Trim();
			var megas = PokemonSpecies.GetMegaForms(baseName);
			if (megas.Count > 0) species = megas[0];
			else species = PokemonSpecies.Get(baseName);
		}
		species ??= PokemonSpecies.Get("Alakazam");
		var inst = new PokemonInstance(species, level, moves)
		{
			IsMegaEvolved = true
		};
		if (!string.IsNullOrWhiteSpace(species.BaseSpeciesName))
		{
			inst.PreMegaSpeciesInternalName = species.BaseSpeciesName;
		}
		inst.RecalculateStats(healFull: true);
		return inst;
	}

	private static void RegisterTeam(string npcName, params PokemonInstance[] team)
	{
		_teams[npcName] = new List<PokemonInstance>(team);
	}

	private static int GetPlayerAverageLevel()
	{
		if (PlayerParty.Instance?.Team == null || PlayerParty.Instance.Team.Count == 0)
			return 20;
		int sum = 0;
		int count = 0;
		foreach (var p in PlayerParty.Instance.Team)
		{
			if (p != null && !p.IsEgg)
			{
				sum += p.Level;
				count++;
			}
		}
		return count > 0 ? Math.Clamp(sum / count, 10, 85) : 20;
	}

	public static List<PokemonInstance> GetTeam(string npcName)
	{
		if (string.IsNullOrWhiteSpace(npcName)) npcName = "Treinador";
		int playerAvg = GetPlayerAverageLevel();

		if (_teams.TryGetValue(npcName, out var originalTeam))
		{
			List<PokemonInstance> clone = new();
			for (int i = 0; i < originalTeam.Count; i++)
			{
				var p = originalTeam[i];
				int scaledLv = Math.Clamp(Math.Max(p.Level, playerAvg - 2 + i), 5, 100);
				PokemonInstance pi = new PokemonInstance(p.Species, scaledLv)
				{
					IsMegaEvolved = p.IsMegaEvolved,
					PreMegaSpeciesInternalName = p.PreMegaSpeciesInternalName
				};
				pi.Moves.Clear();
				foreach (var m in p.Moves)
				{
					pi.Moves.Add(m.Clone());
				}
				pi.RecalculateStats(healFull: true);
				clone.Add(pi);
			}
			return clone;
		}

		// Procedural deterministic 6-Pokémon team for any other NPC
		string[] pool = new[]
		{
			"Lucario", "Garchomp", "Gardevoir", "Zoroark", "Sylveon", "Greninja", "Talonflame",
			"Aegislash", "Mimikyu", "Lycanroc", "Corviknight", "Toxtricity", "Dragapult",
			"Ceruledge", "Armarouge", "Tinkaton", "Annihilape", "Baxcalibur", "Arcanine", "Gyarados",
			"Volcarona", "Excadrill", "Hatterene", "Gholdengo"
		};
		int hash = Math.Abs(npcName.GetHashCode());
		List<PokemonInstance> procTeam = new();
		for (int i = 0; i < 6; i++)
		{
			string spName = pool[(hash + i * 5) % pool.Length];
			PokemonSpecies sp = PokemonSpecies.Get(spName) ?? PokemonSpecies.Get("Pikachu");
			int lv = Math.Clamp(playerAvg - 2 + i, 12, 95);
			procTeam.Add(new PokemonInstance(sp, lv));
		}
		return procTeam;
	}

	public static bool HasTeam(string npcName)
	{
		return !string.IsNullOrWhiteSpace(npcName);
	}

	public static List<string> GetAllNPCNames()
	{
		return new List<string>(_teams.Keys);
	}
}
