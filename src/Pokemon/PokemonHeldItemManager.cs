using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;

namespace StardewValley.Pokemon;

/// <summary>
/// Manager for competitive and adventure Held Items attached directly to Pokémon.
/// Supports Mega Stones, Z-Crystals, healing items, choice items, and type boosters.
/// </summary>
public static class PokemonHeldItemManager
{
	public class HeldItemDefinition
	{
		public string Id { get; set; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public int Price { get; set; } = 1000;
		public PokemonType? BoostedType { get; set; } = null;
		public float DamageMultiplier { get; set; } = 1.0f;
		public bool IsConsumable { get; set; } = false;
	}

	private static readonly Dictionary<string, HeldItemDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);

	static PokemonHeldItemManager()
	{
		Register(new HeldItemDefinition
		{
			Id = "Leftovers",
			DisplayName = "Leftovers",
			Description = "Restores 1/16 of the holder's maximum HP at the end of every turn.",
			Price = 4000
		});

		Register(new HeldItemDefinition
		{
			Id = "LifeOrb",
			DisplayName = "Life Orb",
			Description = "Boosts the damage of all attacks by 30%, but holder loses 10% max HP per hit.",
			Price = 5000,
			DamageMultiplier = 1.30f
		});

		Register(new HeldItemDefinition
		{
			Id = "ChoiceBand",
			DisplayName = "Choice Band",
			Description = "Boosts Physical Attack by 50%, but locks the holder into the first move used.",
			Price = 4500
		});

		Register(new HeldItemDefinition
		{
			Id = "ChoiceSpecs",
			DisplayName = "Choice Specs",
			Description = "Boosts Special Attack by 50%, but locks the holder into the first move used.",
			Price = 4500
		});

		Register(new HeldItemDefinition
		{
			Id = "ChoiceScarf",
			DisplayName = "Choice Scarf",
			Description = "Boosts Speed by 50%, but locks the holder into the first move used.",
			Price = 4500
		});

		Register(new HeldItemDefinition
		{
			Id = "FocusSash",
			DisplayName = "Focus Sash",
			Description = "If at full HP, endures a fatal blow with 1 HP. Single use in battle.",
			Price = 3000,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "RockyHelmet",
			DisplayName = "Rocky Helmet",
			Description = "When struck by a physical contact move, the attacker takes 1/6 max HP recoil.",
			Price = 3500
		});

		Register(new HeldItemDefinition
		{
			Id = "AssaultVest",
			DisplayName = "Assault Vest",
			Description = "Boosts Special Defense by 50%, but prevents the use of status moves.",
			Price = 4000
		});

		Register(new HeldItemDefinition
		{
			Id = "Eviolite",
			DisplayName = "Eviolite",
			Description = "Boosts Defense and Special Defense by 50% if the holder can still evolve.",
			Price = 5000
		});

		Register(new HeldItemDefinition
		{
			Id = "HeavyDutyBoots",
			DisplayName = "Heavy-Duty Boots",
			Description = "Protects the holder from the effects of entry hazards and ground obstacles.",
			Price = 4000
		});

		Register(new HeldItemDefinition
		{
			Id = "DestinyKnot",
			DisplayName = "Destiny Knot",
			Description = "When breeding, ensures 5 IVs are passed down from parents. Infatuates attackers.",
			Price = 5000
		});

		Register(new HeldItemDefinition
		{
			Id = "Everstone",
			DisplayName = "Everstone",
			Description = "Prevents evolution and guarantees passing down the holder's Nature when breeding.",
			Price = 3000
		});

		Register(new HeldItemDefinition
		{
			Id = "WeaknessPolicy",
			DisplayName = "Weakness Policy",
			Description = "Sharply boosts Attack and Sp. Atk (+2 stages) when hit by a super-effective move.",
			Price = 4500,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "LoadedDice",
			DisplayName = "Loaded Dice",
			Description = "Ensures multi-strike moves hit 4 or 5 times consistently.",
			Price = 4000
		});

		Register(new HeldItemDefinition
		{
			Id = "CovertCloak",
			DisplayName = "Covert Cloak",
			Description = "Shields the holder from the secondary effects of opponents' moves.",
			Price = 3500
		});

		Register(new HeldItemDefinition
		{
			Id = "ClearAmulet",
			DisplayName = "Clear Amulet",
			Description = "Protects the holder from having its stats lowered by opponents' moves or abilities.",
			Price = 4000
		});

		Register(new HeldItemDefinition
		{
			Id = "AirBalloon",
			DisplayName = "Air Balloon",
			Description = "Holder floats in the air with Ground immunity until popped by an attack.",
			Price = 3000,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "BlackSludge",
			DisplayName = "Black Sludge",
			Description = "Restores 1/16 HP per turn for Poison-type Pokémon; damages other types.",
			Price = 3000
		});

		Register(new HeldItemDefinition
		{
			Id = "ToxicOrb",
			DisplayName = "Toxic Orb",
			Description = "Badly poisons the holder at the end of the first turn in battle.",
			Price = 2500
		});

		Register(new HeldItemDefinition
		{
			Id = "FlameOrb",
			DisplayName = "Flame Orb",
			Description = "Burns the holder at the end of the first turn in battle.",
			Price = 2500
		});

		Register(new HeldItemDefinition
		{
			Id = "SitrusBerry",
			DisplayName = "Sitrus Berry",
			Description = "Restores 25% max HP when the holder's HP drops below 50%. Consumable.",
			Price = 800,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "LumBerry",
			DisplayName = "Lum Berry",
			Description = "Instantly cures any status condition afflicting the holder. Consumable.",
			Price = 1200,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "ScopeLens",
			DisplayName = "Scope Lens",
			Description = "Increases the critical-hit ratio of the holder by 1 stage.",
			Price = 3500
		});

		Register(new HeldItemDefinition
		{
			Id = "MuscleBand",
			DisplayName = "Muscle Band",
			Description = "Increases the damage of physical moves by 10%.",
			Price = 3000,
			DamageMultiplier = 1.10f
		});

		Register(new HeldItemDefinition
		{
			Id = "WiseGlasses",
			DisplayName = "Wise Glasses",
			Description = "Increases the damage of special moves by 10%.",
			Price = 3000,
			DamageMultiplier = 1.10f
		});

		Register(new HeldItemDefinition
		{
			Id = "ShellBell",
			DisplayName = "Shell Bell",
			Description = "Restores 1/8 of the damage dealt to opponents as HP.",
			Price = 3500
		});

		Register(new HeldItemDefinition
		{
			Id = "KingsRock",
			DisplayName = "King's Rock",
			Description = "Grants damaging moves a 10% chance to cause the target to flinch.",
			Price = 3500
		});

		Register(new HeldItemDefinition
		{
			Id = "QuickClaw",
			DisplayName = "Quick Claw",
			Description = "Grants a 20% chance to move first within the speed priority bracket.",
			Price = 3000
		});

		Register(new HeldItemDefinition
		{
			Id = "ExpertBelt",
			DisplayName = "Expert Belt",
			Description = "Boosts the damage of super-effective moves by 20%.",
			Price = 4000
		});

		Register(new HeldItemDefinition
		{
			Id = "Metronome",
			DisplayName = "Metronome",
			Description = "Boosts damage by 20% each consecutive time the same move is used (up to 100%).",
			Price = 3500
		});

		// Stat-Boosting Vitamins
		Register(new HeldItemDefinition
		{
			Id = "HPUp",
			DisplayName = "HP Up",
			Description = "Nutritious drink that permanently adds +10 HP Effort Values (EVs) to a Pokémon.",
			Price = 9800,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "Protein",
			DisplayName = "Protein",
			Description = "Nutritious drink that permanently adds +10 Attack Effort Values (EVs) to a Pokémon.",
			Price = 9800,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "Iron",
			DisplayName = "Iron",
			Description = "Nutritious drink that permanently adds +10 Defense Effort Values (EVs) to a Pokémon.",
			Price = 9800,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "Calcium",
			DisplayName = "Calcium",
			Description = "Nutritious drink that permanently adds +10 Sp. Atk Effort Values (EVs) to a Pokémon.",
			Price = 9800,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "Zinc",
			DisplayName = "Zinc",
			Description = "Nutritious drink that permanently adds +10 Sp. Def Effort Values (EVs) to a Pokémon.",
			Price = 9800,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "Carbos",
			DisplayName = "Carbos",
			Description = "Nutritious drink that permanently adds +10 Speed Effort Values (EVs) to a Pokémon.",
			Price = 9800,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "PPUp",
			DisplayName = "PP Up",
			Description = "Permanently increases the base PP of a move by 20% (up to 3 times per move).",
			Price = 10000,
			IsConsumable = true
		});

		Register(new HeldItemDefinition
		{
			Id = "PPMax",
			DisplayName = "PP Max",
			Description = "Permanently maximizes the PP of a move (+60% base PP).",
			Price = 25000,
			IsConsumable = true
		});

		// Power Items (EV training and guaranteed IV inheritance in Day Care)
		Register(new HeldItemDefinition
		{
			Id = "PowerWeight",
			DisplayName = "Power Weight",
			Description = "Item de treino que concede EVs de HP. No Day Care, garante a transmissão do IV de HP do progenitor.",
			Price = 3000
		});
		Register(new HeldItemDefinition
		{
			Id = "PowerBracer",
			DisplayName = "Power Bracer",
			Description = "Item de treino que concede EVs de Ataque. No Day Care, garante a transmissão do IV de Ataque do progenitor.",
			Price = 3000
		});
		Register(new HeldItemDefinition
		{
			Id = "PowerBelt",
			DisplayName = "Power Belt",
			Description = "Item de treino que concede EVs de Defesa. No Day Care, garante a transmissão do IV de Defesa do progenitor.",
			Price = 3000
		});
		Register(new HeldItemDefinition
		{
			Id = "PowerLens",
			DisplayName = "Power Lens",
			Description = "Item de treino que concede EVs de Sp. Atk. No Day Care, garante a transmissão do IV de Sp. Atk do progenitor.",
			Price = 3000
		});
		Register(new HeldItemDefinition
		{
			Id = "PowerBand",
			DisplayName = "Power Band",
			Description = "Item de treino que concede EVs de Sp. Def. No Day Care, garante a transmissão do IV de Sp. Def do progenitor.",
			Price = 3000
		});
		Register(new HeldItemDefinition
		{
			Id = "PowerAnklet",
			DisplayName = "Power Anklet",
			Description = "Item de treino que concede EVs de Velocidade. No Day Care, garante a transmissão do IV de Velocidade do progenitor.",
			Price = 3000
		});

		// Type boosters (+20% power)
		RegisterTypeBooster("Charcoal", "Charcoal", PokemonType.Fire);
		RegisterTypeBooster("MysticWater", "Mystic Water", PokemonType.Water);
		RegisterTypeBooster("MiracleSeed", "Miracle Seed", PokemonType.Grass);
		RegisterTypeBooster("Magnet", "Magnet", PokemonType.Electric);
		RegisterTypeBooster("HardStone", "Hard Stone", PokemonType.Rock);
		RegisterTypeBooster("SoftSand", "Soft Sand", PokemonType.Ground);
		RegisterTypeBooster("BlackGlasses", "Black Glasses", PokemonType.Dark);
		RegisterTypeBooster("SpellTag", "Spell Tag", PokemonType.Ghost);
		RegisterTypeBooster("SilkScarf", "Silk Scarf", PokemonType.Normal);
		RegisterTypeBooster("SharpBeak", "Sharp Beak", PokemonType.Flying);
		RegisterTypeBooster("PoisonBarb", "Poison Barb", PokemonType.Poison);
		RegisterTypeBooster("NeverMeltIce", "Never-Melt Ice", PokemonType.Ice);
		RegisterTypeBooster("DragonFang", "Dragon Fang", PokemonType.Dragon);
		RegisterTypeBooster("TwistedSpoon", "Twisted Spoon", PokemonType.Psychic);
		RegisterTypeBooster("BlackBelt", "Black Belt", PokemonType.Fighting);
		RegisterTypeBooster("SilverPowder", "Silver Powder", PokemonType.Bug);
		RegisterTypeBooster("MetalCoat", "Metal Coat", PokemonType.Steel);

		// Legendary Form & Primal Hold Items
		Register(new HeldItemDefinition { Id = "Gracidea", DisplayName = "Flor Gracidea", Description = "Flor abençoada que expressa gratidão. Permite a Shaymin assumir a Forma Céu durante o dia.", BoostedType = PokemonType.Flying, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "GracideaFlower", DisplayName = "Flor Gracidea", Description = "Flor abençoada que expressa gratidão. Permite a Shaymin assumir a Forma Céu durante o dia.", BoostedType = PokemonType.Flying, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "MeteoritePiece", DisplayName = "Pedaço de Meteorito", Description = "Fragmento cósmico irradiando energia estelar. Permite alternar formas de Deoxys e induz Mega Rayquaza.", BoostedType = PokemonType.Psychic, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "Rayquazite", DisplayName = "Rayquazita", Description = "Pedaço de meteorito cristalizado para induzir a Mega Evolução de Rayquaza.", BoostedType = PokemonType.Dragon, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "BlueOrb", DisplayName = "Orbe Azul", Description = "Relíquia marinha primordial que desperta a Reversão Primitiva de Kyogre em batalha.", BoostedType = PokemonType.Water, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "RedOrb", DisplayName = "Orbe Vermelho", Description = "Relíquia de magma primordial que desperta a Reversão Primitiva de Groudon em batalha.", BoostedType = PokemonType.Fire, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "ZygardeCube", DisplayName = "Cubo Zygarde", Description = "Dispositivo para coletar células de Zygarde e alterar entre as Formas 10%, 50% e Completa.", BoostedType = PokemonType.Ground, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "PrisonBottle", DisplayName = "Garrafa da Prisão", Description = "Recipiente mágico que sela o poder descomunal de Hoopa, permitindo liberar sua Forma Unbound.", BoostedType = PokemonType.Dark, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "DNA_Splicers", DisplayName = "Separadores de DNA", Description = "Ferramenta genética de Unova para fundir Kyurem com Reshiram ou Zekrom.", BoostedType = PokemonType.Ice, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "DNASplicers", DisplayName = "Separadores de DNA", Description = "Ferramenta genética de Unova para fundir Kyurem com Reshiram ou Zekrom.", BoostedType = PokemonType.Ice, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "NSolarizer", DisplayName = "N-Solarizer", Description = "Aparelho de fusão de Alola que une Necrozma a Solgaleo (Dusk Mane).", BoostedType = PokemonType.Steel, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "NLunarizer", DisplayName = "N-Lunarizer", Description = "Aparelho de fusão de Alola que une Necrozma a Lunala (Dawn Wings).", BoostedType = PokemonType.Ghost, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "ReinsOfUnity", DisplayName = "Rédeas da Unidade", Description = "Rédeas míticas de Galar para unir Calyrex ao seu nobre corcel Glastrier ou Spectrier.", BoostedType = PokemonType.Psychic, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "IndigoDisk", DisplayName = "Disco Índigo", Description = "Disco cristalizado que harmoniza a energia Teracristal de Terapagos.", BoostedType = PokemonType.Normal, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "RustedSword", DisplayName = "Espada Enferrujada", Description = "Transforma Zacian em sua forma Crowned Sword e fortalece golpes de Aço/Fada.", BoostedType = PokemonType.Steel, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "RustedShield", DisplayName = "Escudo Enferrujado", Description = "Transforma Zamazenta em sua forma Crowned Shield e fortalece golpes de Aço/Lutador.", BoostedType = PokemonType.Steel, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "GriseousCore", DisplayName = "Núcleo Gris", Description = "Transforma Giratina na Forma Origem e fortalece golpes Fantasma/Dragão em 20%.", BoostedType = PokemonType.Ghost, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "GriseousOrb", DisplayName = "Orbe Gris", Description = "Transforma Giratina na Forma Origem e fortalece golpes Fantasma/Dragão em 20%.", BoostedType = PokemonType.Ghost, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "AdamantCrystal", DisplayName = "Cristal Diamante", Description = "Transforma Dialga na Forma Origem e fortalece golpes Aço/Dragão em 20%.", BoostedType = PokemonType.Dragon, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "AdamantOrb", DisplayName = "Orbe Diamante", Description = "Transforma Dialga na Forma Origem e fortalece golpes Aço/Dragão em 20%.", BoostedType = PokemonType.Dragon, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "LustrousGlobe", DisplayName = "Globo Lustroso", Description = "Transforma Palkia na Forma Origem e fortalece golpes Água/Dragão em 20%.", BoostedType = PokemonType.Water, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "LustrousOrb", DisplayName = "Orbe Lustroso", Description = "Transforma Palkia na Forma Origem e fortalece golpes Água/Dragão em 20%.", BoostedType = PokemonType.Water, DamageMultiplier = 1.20f, Price = 25000 });
		Register(new HeldItemDefinition { Id = "TealMask", DisplayName = "Máscara Turquesa", Description = "Máscara sagrada de Ogerpon (Grass). Aumenta o dano de seus ataques em 20%.", BoostedType = PokemonType.Grass, DamageMultiplier = 1.20f, Price = 20000 });
		Register(new HeldItemDefinition { Id = "WellspringMask", DisplayName = "Máscara da Fonte", Description = "Transforma Ogerpon na Forma Fonte (Grass/Water) e aumenta ataques em 20%.", BoostedType = PokemonType.Water, DamageMultiplier = 1.20f, Price = 20000 });
		Register(new HeldItemDefinition { Id = "HearthflameMask", DisplayName = "Máscara da Chama", Description = "Transforma Ogerpon na Forma Chama (Grass/Fire) e aumenta ataques em 20%.", BoostedType = PokemonType.Fire, DamageMultiplier = 1.20f, Price = 20000 });
		Register(new HeldItemDefinition { Id = "CornerstoneMask", DisplayName = "Máscara do Alicerce", Description = "Transforma Ogerpon na Forma Alicerce (Grass/Rock) e aumenta ataques em 20%.", BoostedType = PokemonType.Rock, DamageMultiplier = 1.20f, Price = 20000 });
	}

	private static void RegisterTypeBooster(string id, string name, PokemonType type)
	{
		Register(new HeldItemDefinition
		{
			Id = id,
			DisplayName = name,
			Description = $"Fortalece o poder dos ataques do tipo {type} em 20%.",
			BoostedType = type,
			DamageMultiplier = 1.20f,
			Price = 2000
		});
	}

	private static void Register(HeldItemDefinition def)
	{
		_definitions[def.Id] = def;
	}

	public static IReadOnlyDictionary<string, HeldItemDefinition> AllHeldItems => _definitions;

	public static bool TryGet(string itemId, out HeldItemDefinition def)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			def = null;
			return false;
		}

		if (itemId.StartsWith("(O)")) itemId = itemId.Substring(3);
		if (itemId.StartsWith("Poke_")) itemId = itemId.Substring(5);

		return _definitions.TryGetValue(itemId, out def);
	}

	public static readonly Dictionary<string, string> PortugueseHeldItemDescriptions = new(StringComparer.OrdinalIgnoreCase)
	{
		["Leftovers"] = "Recupera 1/16 do HP máximo do portador ao final de cada turno de combate.",
		["LifeOrb"] = "Aumenta o dano de todos os ataques em 30%, mas o portador perde 10% do HP ao golpear.",
		["ChoiceBand"] = "Aumenta o Ataque Físico em 50%, mas prende o portador no primeiro golpe usado.",
		["ChoiceSpecs"] = "Aumenta o Ataque Especial em 50%, mas prende o portador no primeiro golpe usado.",
		["ChoiceScarf"] = "Aumenta a Velocidade em 50%, mas prende o portador no primeiro golpe usado.",
		["FocusSash"] = "Se estiver com HP cheio, sobrevive a um golpe fatal com 1 de HP. Uso único.",
		["RockyHelmet"] = "Causa dano de recuo de 1/6 do HP máximo ao atacante que fizer contato físico direto.",
		["AssaultVest"] = "Aumenta a Defesa Especial em 50%, mas impede o uso de movimentos de status.",
		["Eviolite"] = "Aumenta a Defesa e a Defesa Especial em 50% se o Pokémon ainda puder evoluir.",
		["HeavyDutyBoots"] = "Protege totalmente o portador contra Stealth Rock, Spikes e espinhos em campo.",
		["DestinyKnot"] = "No cruzamento de ovos, herda 5 IVs perfeitos dos pais. Causa atração no atacante.",
		["Everstone"] = "Impede a evolução do Pokémon e garante herança da Natureza genética em ovos.",
		["WeaknessPolicy"] = "Aumenta o Ataque e Atq. Esp. em 2 estágios (+100%) ao ser atingido por golpe super efetivo.",
		["LoadedDice"] = "Garante que ataques de múltiplos acertos (2 a 5 golpes) acertem 4 ou 5 vezes.",
		["CovertCloak"] = "Protege o portador contra efeitos secundários de golpes inimigos (ex: queimar, congelar, recuar).",
		["ClearAmulet"] = "Protege contra reduções de atributos causadas por ataques ou habilidades inimigas (ex: Intimidate).",
		["AirBalloon"] = "Concede imunidade a golpes e perigos do tipo Terrestre até que o balão estoure ao receber dano.",
		["BlackSludge"] = "Recupera 1/16 de HP por turno para Pokémon Venenosos; fere outros tipos.",
		["ToxicOrb"] = "Envenena gravemente (Badly Poisoned) o próprio portador ao final do primeiro turno.",
		["FlameOrb"] = "Causa queimadura (Burn) ao próprio portador ao final do primeiro turno.",
		["SitrusBerry"] = "Restaura 25% do HP máximo quando a vida cair abaixo da metade.",
		["LumBerry"] = "Cura imediatamente qualquer condição de status primária (queimadura, paralisia, sono, veneno, etc).",
		["ScopeLens"] = "Aumenta em 1 estágio a taxa de acertos críticos de todos os ataques.",
		["MuscleBand"] = "Aumenta o dano de ataques físicos em 10%.",
		["WiseGlasses"] = "Aumenta o dano de ataques especiais em 10%.",
		["ShellBell"] = "Recupera 1/8 do dano infligido ao oponente em cada ataque bem-sucedido.",
		["KingsRock"] = "Garante 10% de chance de fazer o adversário recuar (Flinch) ao ser atacado.",
		["QuickClaw"] = "Concede 20% de chance de atacar primeiro na sua prioridade de movimento.",
		["ExpertBelt"] = "Aumenta em 20% o dano de ataques super efetivos.",
		["Metronome"] = "Aumenta o poder em 20% a cada turno consecutivo que o mesmo golpe for usado.",
		["HPUp"] = "Vitamina que concede permanentemente +10 EVs de HP ao Pokémon.",
		["Protein"] = "Vitamina que concede permanentemente +10 EVs de Ataque Físico ao Pokémon.",
		["Iron"] = "Vitamina que concede permanentemente +10 EVs de Defesa Física ao Pokémon.",
		["Calcium"] = "Vitamina que concede permanentemente +10 EVs de Ataque Especial ao Pokémon.",
		["Zinc"] = "Vitamina que concede permanentemente +10 EVs de Defesa Especial ao Pokémon.",
		["Carbos"] = "Vitamina que concede permanentemente +10 EVs de Velocidade ao Pokémon.",
		["PPUp"] = "Aumenta permanentemente o PP máximo de um movimento em 20%.",
		["PPMax"] = "Aumenta permanentemente o PP máximo de um movimento ao limite máximo (60%).",
		["Charcoal"] = "Aumenta o poder dos movimentos do tipo Fogo em 20%.",
		["MysticWater"] = "Aumenta o poder dos movimentos do tipo Água em 20%.",
		["MiracleSeed"] = "Aumenta o poder dos movimentos do tipo Planta em 20%.",
		["Magnet"] = "Aumenta o poder dos movimentos do tipo Elétrico em 20%.",
		["HardStone"] = "Aumenta o poder dos movimentos do tipo Pedra em 20%.",
		["SoftSand"] = "Aumenta o poder dos movimentos do tipo Terrestre em 20%.",
		["BlackGlasses"] = "Aumenta o poder dos movimentos do tipo Noturno em 20%.",
		["SpellTag"] = "Aumenta o poder dos movimentos do tipo Fantasma em 20%.",
		["SilkScarf"] = "Aumenta o poder dos movimentos do tipo Normal em 20%.",
		["SharpBeak"] = "Aumenta o poder dos movimentos do tipo Voador em 20%.",
		["PoisonBarb"] = "Aumenta o poder dos movimentos do tipo Venenoso em 20%.",
		["NeverMeltIce"] = "Aumenta o poder dos movimentos do tipo Gelo em 20%.",
		["DragonFang"] = "Aumenta o poder dos movimentos do tipo Dragão em 20%.",
		["TwistedSpoon"] = "Aumenta o poder dos movimentos do tipo Psíquico em 20%.",
		["BlackBelt"] = "Aumenta o poder dos movimentos do tipo Lutador em 20%.",
		["SilverPowder"] = "Aumenta o poder dos movimentos do tipo Inseto em 20%.",
		["MetalCoat"] = "Aumenta o poder dos movimentos do tipo Aço em 20%. Facilita certas evoluções."
	};

	public static string NormalizeId(string itemId)
	{
		if (string.IsNullOrEmpty(itemId)) return "";
		string clean = itemId.Trim();
		if (clean.StartsWith("(O)", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(3).Trim();
		if (clean.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(5).Trim();
		return clean.Replace("_", "").Replace(" ", "").Replace("-", "");
	}

	public static string GetDisplayName(string itemId)
	{
		if (string.IsNullOrEmpty(itemId)) return "Nenhum";
		string id = NormalizeId(itemId);
		if (TryGet(id, out var def)) return def.DisplayName;
		if (PokemonItemManager.MegaStoneDefinitions.TryGetValue(id, out var ms)) return ms.DisplayName;
		if (!id.StartsWith("Poke_") && PokemonItemManager.MegaStoneDefinitions.TryGetValue("Poke_" + id, out ms)) return ms.DisplayName;
		string stoneByForm = PokemonItemManager.GetMegaStoneIdForMegaForm(id);
		if (!string.IsNullOrEmpty(stoneByForm) && PokemonItemManager.MegaStoneDefinitions.TryGetValue(stoneByForm, out ms)) return ms.DisplayName;
		if (PokemonItemManager.ZCrystalDefinitions.TryGetValue(id, out var zc)) return zc.DisplayName;
		if (!id.StartsWith("Poke_") && PokemonItemManager.ZCrystalDefinitions.TryGetValue("Poke_" + id, out zc)) return zc.DisplayName;
		if (PokemonItemManager.SignatureZCrystalDefinitions.TryGetValue(id, out var szc)) return szc.DisplayName;
		if (!id.StartsWith("Poke_") && PokemonItemManager.SignatureZCrystalDefinitions.TryGetValue("Poke_" + id, out szc)) return szc.DisplayName;
		return id.StartsWith("Poke_") ? id.Substring(5) : id;
	}

	public static string GetDescription(string itemId)
	{
		if (string.IsNullOrEmpty(itemId))
			return (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en) ? "No item held currently." : "Nenhum item equipado no momento.";

		bool isEn = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en;
		string id = NormalizeId(itemId);
		string bareId = id.StartsWith("Poke_") ? id.Substring(5) : id;

		if (TryGet(id, out var def))
		{
			if (!isEn && PortugueseHeldItemDescriptions.TryGetValue(bareId, out string ptDesc))
				return ptDesc;
			return def.Description;
		}

		if (PokemonItemManager.MegaStoneDefinitions.TryGetValue(id, out var ms) ||
		    (!id.StartsWith("Poke_") && PokemonItemManager.MegaStoneDefinitions.TryGetValue("Poke_" + id, out ms)) ||
		    (!string.IsNullOrEmpty(PokemonItemManager.GetMegaStoneIdForMegaForm(id)) && PokemonItemManager.MegaStoneDefinitions.TryGetValue(PokemonItemManager.GetMegaStoneIdForMegaForm(id), out ms)))
		{
			string targetMon = ms.DisplayName.Replace("Pedra Mega ", "").Replace("ite X", " X").Replace("ite Y", " Y").Replace("ite", "").Replace("nite", "");
			if (targetMon.Contains(" ")) targetMon = targetMon.Split(' ')[0];
			if (isEn)
				return $"An ancient Mega Stone compatible with {targetMon}. Allows it to Mega Evolve in battle when wearing the Mega Bracelet!";
			return $"Mega Pedra ancestral compatível com {targetMon}. Permite Mega Evoluir em combate quando segurada e com o Bracelete Mega equipado!";
		}

		if (PokemonItemManager.ZCrystalDefinitions.TryGetValue(id, out var zc) ||
		    (!id.StartsWith("Poke_") && PokemonItemManager.ZCrystalDefinitions.TryGetValue("Poke_" + id, out zc)))
		{
			if (isEn)
				return $"Elemental Z-Crystal for {zc.Type}-type moves. Converts compatible moves into powerful Z-Moves when wearing the Z-Ring!";
			return $"Cristal Z canalizador do tipo {zc.Type}. Converte ataques compatíveis em poderosos Z-Moves quando equipado com o Anel Z!";
		}

		if (PokemonItemManager.SignatureZCrystalDefinitions.TryGetValue(id, out var szc) ||
		    (!id.StartsWith("Poke_") && PokemonItemManager.SignatureZCrystalDefinitions.TryGetValue("Poke_" + id, out szc)))
		{
			if (id.Contains("UltranecroziumZ"))
			{
				if (isEn)
					return "Transcendent Z-Crystal for Necrozma. Triggers Ultra Burst (Ultra Necrozma) and the legendary Z-Move 'Light That Burns the Sky'!";
				return "Cristal Z transcendente exclusivo para Necrozma. Desperta o Ultra Burst (Ultra Necrozma) e o lendário Z-Move 'Light That Burns the Sky'!";
			}
			if (isEn)
				return $"Signature Z-Crystal for {szc.RequiredSpecies} with move {szc.RequiredBaseMove}. Unlocks the devastating Z-Move '{szc.ZMoveName}'!";
			return $"Cristal Z de assinatura exclusivo para {szc.RequiredSpecies} com o golpe {szc.RequiredBaseMove}. Libera o devastador Z-Move '{szc.ZMoveName}'!";
		}

		return isEn ? "Pokémon battle hold item." : "Item de batalha de Pokémon.";
	}

	public static bool IsMegaStone(string itemId)
	{
		if (string.IsNullOrEmpty(itemId)) return false;
		string id = NormalizeId(itemId);
		string bareId = id.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase) ? id.Substring(5) : id;
		string pokeId = id.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase) ? id : "Poke_" + id;
		if (PokemonItemManager.MegaStoneDefinitions.ContainsKey(id) || PokemonItemManager.MegaStoneDefinitions.ContainsKey(pokeId) || PokemonItemManager.MegaStoneDefinitions.ContainsKey(bareId)) return true;
		if (PokemonItemManager.GetMegaStoneIdForMegaForm(id) != null || PokemonItemManager.GetMegaStoneIdForMegaForm(bareId) != null) return true;
		return false;
	}

	public static bool IsZCrystal(string itemId)
	{
		if (string.IsNullOrEmpty(itemId)) return false;
		string id = NormalizeId(itemId);
		string bareId = id.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase) ? id.Substring(5) : id;
		string pokeId = id.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase) ? id : "Poke_" + id;
		if (PokemonItemManager.ZCrystalDefinitions.ContainsKey(id) || PokemonItemManager.SignatureZCrystalDefinitions.ContainsKey(id)) return true;
		if (PokemonItemManager.ZCrystalDefinitions.ContainsKey(pokeId) || PokemonItemManager.SignatureZCrystalDefinitions.ContainsKey(pokeId)) return true;
		if (PokemonItemManager.ZCrystalDefinitions.ContainsKey(bareId) || PokemonItemManager.SignatureZCrystalDefinitions.ContainsKey(bareId)) return true;
		return false;
	}

	public static bool IsHoldable(string itemId)
	{
		if (string.IsNullOrEmpty(itemId)) return false;
		string id = NormalizeId(itemId);
		string bareId = id.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase) ? id.Substring(5) : id;
		string pokeId = id.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase) ? id : "Poke_" + id;
		return _definitions.ContainsKey(bareId) || _definitions.ContainsKey(id) || _definitions.ContainsKey(pokeId) || IsMegaStone(id) || IsZCrystal(id);
	}

	public static PokemonType? GetTypeBoostForHeldItem(string itemId)
	{
		if (TryGet(itemId, out var def))
		{
			return def.BoostedType;
		}
		return null;
	}

	public static List<(string id, string name, string desc, int cost)> GetAllHeldItems()
	{
		var list = new List<(string id, string name, string desc, int cost)>();
		foreach (var kvp in _definitions)
		{
			list.Add((kvp.Key, kvp.Value.DisplayName, kvp.Value.Description, kvp.Value.Price));
		}
		return list;
	}

	/// <summary>
	/// Returns random held item suitable for mining/geode/dwarf drops.
	/// </summary>
	public static string GetRandomGeologicalHeldItem()
	{
		string[] items = new[]
		{
			"HardStone", "SoftSand", "MetalCoat", "NeverMeltIce",
			"RockyHelmet", "Leftovers", "AssaultVest", "Eviolite"
		};
		return items[Game1.random.Next(items.Length)];
	}

	public static string GetRandomGeodeDrop(string geodeId = null, Random rand = null)
	{
		rand ??= Game1.random;

		bool isOmni = geodeId == "749" || geodeId == "(O)749";
		bool isMagma = geodeId == "537" || geodeId == "(O)537";
		bool isFrozen = geodeId == "536" || geodeId == "(O)536";
		bool isRegular = geodeId == "535" || geodeId == "(O)535";

		double specialRoll = rand.NextDouble();
		double specialThreshold = isOmni ? 0.35 : 0.20;

		if (specialRoll < specialThreshold)
		{
			if (isMagma)
			{
				string[] magmaPool = new[]
				{
					"Poke_CharizarditeX", "Poke_CharizarditeY", "Poke_Blazikenite", "Poke_Cameruptite",
					"Poke_Tyranitarite", "Poke_Houndoominite", "Poke_Steelixite", "Poke_Aggronite",
					"Poke_Garchompite", "Poke_Lucarionite", "Poke_FiriumZ", "Poke_GroundiumZ",
					"Poke_RockiumZ", "Poke_FightiniumZ", "Poke_SteeliumZ"
				};
				return magmaPool[rand.Next(magmaPool.Length)];
			}
			else if (isFrozen)
			{
				string[] frozenPool = new[]
				{
					"Poke_Blastoisinite", "Poke_Swampertite", "Poke_Gyaradosite", "Poke_Glalitite",
					"Poke_Abomasite", "Poke_Slowbronite", "Poke_Sablenite", "Poke_Gengarite",
					"Poke_Alakazite", "Poke_Gardevoirite", "Poke_Galladite", "Poke_IciumZ",
					"Poke_WateriumZ", "Poke_GhostiumZ", "Poke_PsychiumZ"
				};
				return frozenPool[rand.Next(frozenPool.Length)];
			}
			else if (isRegular)
			{
				string[] regularPool = new[]
				{
					"Poke_Venusaurite", "Poke_Beedrillite", "Poke_Pinsirite", "Poke_Sceptilite",
					"Poke_Pidgeotite", "Poke_Kangaskhanite", "Poke_Lopunnite", "Poke_Heracronite",
					"Poke_Ampharosite", "Poke_Manectite", "Poke_GrassiumZ", "Poke_BuginiumZ",
					"Poke_NormaliumZ", "Poke_ElectriumZ", "Poke_PoisoniumZ"
				};
				return regularPool[rand.Next(regularPool.Length)];
			}
			else
			{
				var allMegas = System.Linq.Enumerable.ToList(PokemonItemManager.AllMegaStoneIds);
				var allZ = System.Linq.Enumerable.ToList(PokemonItemManager.AllZCrystalIds);
				if (rand.Next(2) == 0 && allMegas.Count > 0)
					return allMegas[rand.Next(allMegas.Count)];
				else if (allZ.Count > 0)
					return allZ[rand.Next(allZ.Count)];
			}
		}

		string[] items = new[]
		{
			"HardStone", "SoftSand", "MetalCoat", "NeverMeltIce",
			"RockyHelmet", "Leftovers", "AssaultVest", "Eviolite",
			"LifeOrb", "ChoiceBand", "ChoiceSpecs", "ChoiceScarf", "FocusSash"
		};
		return items[rand.Next(items.Length)];
	}

	public static string GetRandomGeodeDrop(Random rand) => GetRandomGeodeDrop(null, rand);

	/// <summary>
	/// Returns random held item suitable for worm / artifact spot digging.
	/// </summary>
	public static string GetRandomDiggingHeldItem()
	{
		return GetRandomDigDrop(Game1.random);
	}

	public static string GetRandomDigDrop(Random rand = null)
	{
		rand ??= Game1.random;

		// 20% chance to excavate an ancient Mega Stone or Z-Crystal from artifact worms
		if (rand.NextDouble() < 0.20)
		{
			if (rand.Next(2) == 0)
			{
				var allMegas = System.Linq.Enumerable.ToList(PokemonItemManager.AllMegaStoneIds);
				if (allMegas.Count > 0) return allMegas[rand.Next(allMegas.Count)];
			}
			else
			{
				var allZ = System.Linq.Enumerable.ToList(PokemonItemManager.AllZCrystalIds);
				if (allZ.Count > 0) return allZ[rand.Next(allZ.Count)];
			}
		}

		string[] items = new[]
		{
			"SoftSand", "HardStone", "Everstone", "DestinyKnot",
			"Leftovers", "DampRock", "HeatRock", "SmoothRock",
			"IcyRock", "QuickClaw", "RockyHelmet", "MiracleSeed",
			"Charcoal", "MysticWater", "SilkScarf", "CovertCloak",
			"HeavyDutyBoots", "WeaknessPolicy", "LoadedDice"
		};
		return items[rand.Next(items.Length)];
	}

	public static bool IsVitamin(string itemId)
	{
		if (string.IsNullOrEmpty(itemId)) return false;
		if (itemId.StartsWith("(O)")) itemId = itemId.Substring(3);
		if (itemId.StartsWith("Poke_")) itemId = itemId.Substring(5);
		return itemId is "HPUp" or "Protein" or "Iron" or "Calcium" or "Zinc" or "Carbos" or "PPUp" or "PPMax";
	}

	public static bool Matches(string heldItem, string targetId)
	{
		if (string.IsNullOrEmpty(heldItem) || string.IsNullOrEmpty(targetId)) return false;
		return string.Equals(NormalizeId(heldItem), NormalizeId(targetId), StringComparison.OrdinalIgnoreCase);
	}
}
