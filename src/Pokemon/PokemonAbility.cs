using System;
using System.Collections.Generic;

namespace StardewValley.Pokemon;

/// <summary>
/// Passive Combat Abilities engine for base Pokémon and Mega Evolutions.
/// Triggers on switch-in, during attack damage calculation, on contact, and at end of turn.
/// </summary>
public static class PokemonAbility
{
	public class AbilityInfo
	{
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
	}

	private static readonly Dictionary<string, AbilityInfo> _abilityData = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> _speciesBaseAbilities = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> _speciesMegaAbilities = new(StringComparer.OrdinalIgnoreCase);

	static PokemonAbility()
	{
		// 1. Register Ability Descriptions
		RegisterAbility("Intimidate", "Intimidação (Intimidate)", "Reduz o Ataque do oponente ao entrar em batalha.");
		RegisterAbility("Levitate", "Levitação (Levitate)", "Imunidade completa a ataques do tipo Terrestre.");
		RegisterAbility("Drizzle", "Garoa (Drizzle)", "Invoca chuva torrencial instantaneamente ao entrar em campo.");
		RegisterAbility("Drought", "Seca (Drought)", "Invoca sol escaldante instantaneamente ao entrar em campo.");
		RegisterAbility("SandStream", "Corrente de Areia (Sand Stream)", "Invoca uma tempestade de areia ao entrar em campo.");
		RegisterAbility("SnowWarning", "Nevasca (Snow Warning)", "Invoca tempestade de neve ao entrar em campo.");
		RegisterAbility("Blaze", "Chama (Blaze)", "Aumenta o poder de golpes de Fogo em 50% quando o HP está crítico.");
		RegisterAbility("Torrent", "Torrente (Torrent)", "Aumenta o poder de golpes de Água em 50% quando o HP está crítico.");
		RegisterAbility("Overgrow", "Supercrescimento (Overgrow)", "Aumenta o poder de golpes de Planta em 50% quando o HP está crítico.");
		RegisterAbility("ToughClaws", "Garras Firmes (Tough Claws)", "Aumenta o poder de ataques físicos de contato em 30%.");
		RegisterAbility("Pixilate", "Pele de Fada (Pixilate)", "Converte ataques Normais em tipo Fada com 20% mais poder.");
		RegisterAbility("Aerilate", "Pele Celeste (Aerilate)", "Converte ataques Normais em tipo Voador com 20% mais poder.");
		RegisterAbility("HugePower", "Poder Gigante (Huge Power)", "Dobra o Ataque Físico efetivo do portador.");
		RegisterAbility("Adaptability", "Adaptabilidade (Adaptability)", "Aumenta o bônus de mesmo tipo (STAB) de 1.5x para 2.0x.");
		RegisterAbility("SpeedBoost", "Impulso de Velocidade (Speed Boost)", "Aumenta a Velocidade em 50% no final de cada turno.");
		RegisterAbility("MagicGuard", "Guarda Mágica (Magic Guard)", "Protege contra danos indiretos de veneno, queima e recuo.");
		RegisterAbility("PoisonHeal", "Cura por Veneno (Poison Heal)", "Em vez de sofrer dano por veneno, regenera 1/8 de HP por turno.");
		RegisterAbility("Guts", "Bravura (Guts)", "Aumenta o Ataque em 50% sob problemas de status, ignorando a perda por queimadura.");
		RegisterAbility("Technician", "Técnico (Technician)", "Aumenta o dano de golpes com poder até 60 em 50%.");
		RegisterAbility("SheerForce", "Força Bruta (Sheer Force)", "Aumenta o dano de ataques com efeitos secundários em 30%, mas anula o efeito.");
		RegisterAbility("ParentalBond", "Vínculo Parental (Parental Bond)", "Ataca duas vezes no mesmo turno (o segundo golpe causa 25% de dano).");
		RegisterAbility("Regenerator", "Regenerador (Regenerator)", "Restaura 1/3 do HP máximo ao ser recolhido do campo.");
		RegisterAbility("Static", "Estática (Static)", "30% de chance de paralisar adversários que fizerem contato físico.");
		RegisterAbility("FlameBody", "Corpo em Chamas (Flame Body)", "30% de chance de queimar adversários que fizerem contato físico.");
		RegisterAbility("ShadowTag", "Armadilha das Sombras (Shadow Tag)", "Impede a fuga ou troca dos Pokémon adversários.");
		RegisterAbility("MoldBreaker", "Quebra-Molde (Mold Breaker)", "Ignora habilidades defensivas do adversário ao atacar.");
		RegisterAbility("CursedBody", "Corpo Amaldiçoado (Cursed Body)", "Ao sofrer um ataque, 30% de chance de desativar o golpe sofrido por alguns turnos.");
		RegisterAbility("ThickFat", "Gordura Espessa (Thick Fat)", "Reduz o dano recebido de ataques dos tipos Fogo e Gelo em 50%.");
		RegisterAbility("MegaLauncher", "Mega Disparador (Mega Launcher)", "Aumenta o poder de golpes de pulso e esfera em 50%.");
		RegisterAbility("Steadfast", "Inabalável (Steadfast)", "Aumenta a Velocidade ao recuar por medo (Flinch).");
		RegisterAbility("Insomnia", "Insônia (Insomnia)", "Imunidade completa à condição de Sono.");
		RegisterAbility("SkillLink", "Elo de Habilidade (Skill Link)", "Golpes de múltiplos acertos sempre acertam o número máximo de vezes.");
		RegisterAbility("SolarPower", "Poder Solar (Solar Power)", "Sob Sol intenso, aumenta o Ataque Especial em 50%, mas perde 1/8 de HP por turno.");
		RegisterAbility("Filter", "Filtro (Filter)", "Reduz o dano de ataques super efetivos recebidos em 25%.");
		RegisterAbility("Prankster", "Travessura (Prankster)", "Concede prioridade +1 para movimentos de status.");
		RegisterAbility("MagicBounce", "Reflexo Mágico (Magic Bounce)", "Reflete movimentos de status e armadilhas de volta ao adversário.");
		RegisterAbility("DeltaStream", "Corrente Delta (Delta Stream)", "Invoca ventos misteriosos que eliminam fraquezas do tipo Voador.");
		RegisterAbility("NoGuard", "Sem Guarda (No Guard)", "Garante que todos os ataques usados e recebidos tenham 100% de precisão.");
		RegisterAbility("ShellArmor", "Armadura de Concha (Shell Armor)", "Protege totalmente o Pokémon contra acertos críticos.");
		RegisterAbility("SandForce", "Força da Areia (Sand Force)", "Sob Tempestade de Areia, aumenta o dano de golpes Terrestre, Pedra e Aço em 30%.");
		RegisterAbility("LightningRod", "Para-raios (Lightning Rod)", "Atrai e anula golpes Elétricos, aumentando o Ataque Especial.");
		RegisterAbility("SwiftSwim", "Nado Rápido (Swift Swim)", "Dobra a Velocidade durante chuva torrencial.");
		RegisterAbility("StrongJaw", "Mandíbula Forte (Strong Jaw)", "Aumenta o dano de movimentos de mordida em 50%.");
		RegisterAbility("Refrigerate", "Refrigeração (Refrigerate)", "Converte ataques Normais em tipo Gelo com 20% mais poder.");
		RegisterAbility("Scrappy", "Sem Filtro (Scrappy)", "Permite atingir Pokémon do tipo Fantasma com golpes Normal e Lutador.");
		RegisterAbility("InnerFocus", "Foco Interno (Inner Focus)", "Impedimento total de recuar por medo (Flinch).");
		RegisterAbility("Healer", "Curandeiro (Healer)", "Chance de curar problemas de status dos aliados.");
		RegisterAbility("PrimordialSea", "Mar Primordial (Primordial Sea)", "Invoca chuva torrencial inalterável por habilidades comuns.");
		RegisterAbility("DesolateLand", "Terra Desolada (Desolate Land)", "Invoca sol escaldante inalterável por habilidades comuns.");
		RegisterAbility("BadDreams", "Pesadelos (Bad Dreams)", "Reduz o HP de Pokémon adversários adormecidos.");
		RegisterAbility("Reckless", "Audácia (Reckless)", "Aumenta o dano de ataques com dano de recuo em 20%.");
		RegisterAbility("SandRush", "Ímpeto na Areia (Sand Rush)", "Dobra a Velocidade durante Tempestades de Areia.");
		RegisterAbility("SlushRush", "Ímpeto na Neve (Slush Rush)", "Dobra a Velocidade durante Nevascas.");
		RegisterAbility("Bulletproof", "À Prova de Balas (Bulletproof)", "Imunidade a ataques baseados em bombas, esferas e canhões.");
		RegisterAbility("BattleBond", "Vínculo de Batalha (Battle Bond)", "Aumenta os atributos após derrotar um oponente.");
		RegisterAbility("Contrary", "Contrário (Contrary)", "Inverte modificadores de atributos (aumentos viram reduções e vice-versa).");
		RegisterAbility("Unburden", "Sem Carga (Unburden)", "Dobra a Velocidade quando consome ou perde o item segurado.");
		RegisterAbility("PowerConstruct", "Construto de Poder (Power Construct)", "Transforma-se na forma Completa quando atinge HP crítico.");
		RegisterAbility("EmergencyExit", "Saída de Emergência (Emergency Exit)", "Retorna automaticamente à Pokébola ao cair abaixo de 50% de HP.");
		RegisterAbility("Berserk", "Frenesi (Berserk)", "Aumenta o Ataque Especial ao sofrer dano que deixe o HP abaixo de 50%.");
		RegisterAbility("SoulHeart", "Coração das Almas (Soul-Heart)", "Aumenta o Ataque Especial sempre que outro Pokémon é nocauteado.");
		RegisterAbility("VoltAbsorb", "Absorção Elétrica (Volt Absorb)", "Cura 25% do HP ao sofrer ataques Elétricos em vez de tomar dano.");
		RegisterAbility("WaterAbsorb", "Absorção de Água (Water Absorb)", "Cura 25% do HP ao sofrer ataques de Água em vez de tomar dano.");
		RegisterAbility("FlashFire", "Absorção de Fogo (Flash Fire)", "Imunidade a fogo e fortalece seus próprios ataques de Fogo.");
		RegisterAbility("Defiant", "Desafiante (Defiant)", "Aumenta o Ataque em +2 estágios ao sofrer redução de atributos.");
		RegisterAbility("Moody", "Instável (Moody)", "Aumenta um atributo em +2 e reduz outro em -1 ao final de cada turno.");
		RegisterAbility("ToxicDebris", "Detritos Tóxicos (Toxic Debris)", "Espalha espinhos venenosos ao sofrer golpes de contato físico.");
		RegisterAbility("Commander", "Comandante (Commander)", "Assume o comando tático do parceiro em batalhas duplas.");
		RegisterAbility("ThermalExchange", "Troca Térmica (Thermal Exchange)", "Imune a queimadura; ataques de Fogo aumentam o Ataque em +1 estágio.");
		RegisterAbility("Triage", "Triagem (Triage)", "Concede prioridade máxima (+3) para movimentos de cura.");
		RegisterAbility("SurgeSurfer", "Surfe Elétrico (Surge Surfer)", "Dobra a Velocidade em terreno elétrico.");
		RegisterAbility("Multiscale", "Multiescala (Multiscale)", "Reduz pela metade o dano recebido quando com HP cheio.");
		RegisterAbility("Analytic", "Analítico (Analytic)", "Aumenta o dano em 30% caso o usuário ataque por último no turno.");
		RegisterAbility("DrySkin", "Pele Seca (Dry Skin)", "Recupera HP com Água e na Chuva, mas sofre mais dano de Fogo.");
		RegisterAbility("Chlorophyll", "Clorofila (Chlorophyll)", "Dobra a Velocidade sob luz solar intensa.");
		RegisterAbility("FlowerVeil", "Véu Floral (Flower Veil)", "Protege aliados do tipo Planta contra reduções de status.");
		RegisterAbility("Moxie", "Autoconfiança (Moxie)", "Aumenta o Ataque em +1 estágio ao derrotar um oponente.");

		// 2. Canonical Species Abilities mapping
		MapBase("Gyarados", "Intimidate");
		MapBase("Arcanine", "Intimidate");
		MapBase("Salamence", "Intimidate");
		MapBase("Staraptor", "Intimidate");
		MapBase("Krookodile", "Intimidate");
		MapBase("Luxray", "Intimidate");
		MapBase("Incineroar", "Intimidate");

		MapBase("Gengar", "CursedBody");
		MapBase("Flygon", "Levitate");
		MapBase("Rotom", "Levitate");
		MapBase("Hydreigon", "Levitate");
		MapBase("Bronzong", "Levitate");
		MapBase("Weezing", "Levitate");
		MapBase("Latios", "Levitate");
		MapBase("Latias", "Levitate");

		MapBase("Pelipper", "Drizzle");
		MapBase("Politoed", "Drizzle");
		MapBase("Kyogre", "Drizzle");

		MapBase("Torkoal", "Drought");
		MapBase("Ninetales", "Drought");
		MapBase("Groudon", "Drought");

		MapBase("Tyranitar", "SandStream");
		MapBase("Hippowdon", "SandStream");
		MapBase("Gigalith", "SandStream");

		MapBase("Abomasnow", "SnowWarning");
		MapBase("Ninetales-Alola", "SnowWarning");
		MapBase("Aurorus", "SnowWarning");

		MapBase("Charizard", "Blaze");
		MapBase("Blaziken", "Blaze");
		MapBase("Infernape", "Blaze");
		MapBase("Typhlosion", "Blaze");
		MapBase("Cinderace", "Blaze");
		MapBase("Emboar", "Blaze");
		MapBase("Delphox", "Blaze");
		MapBase("Incineroar", "Blaze");
		MapBase("Skeledirge", "Blaze");

		MapBase("Blastoise", "Torrent");
		MapBase("Swampert", "Torrent");
		MapBase("Greninja", "Torrent");
		MapBase("Feraligatr", "Torrent");
		MapBase("Empoleon", "Torrent");
		MapBase("Samurott", "Torrent");
		MapBase("Primarina", "Torrent");
		MapBase("Inteleon", "Torrent");
		MapBase("Quaquaval", "Torrent");

		MapBase("Venusaur", "Overgrow");
		MapBase("Sceptile", "Overgrow");
		MapBase("Torterra", "Overgrow");
		MapBase("Serperior", "Overgrow");
		MapBase("Chesnaught", "Overgrow");
		MapBase("Decidueye", "Overgrow");
		MapBase("Rillaboom", "Overgrow");
		MapBase("Meowscarada", "Overgrow");

		MapBase("Pikachu", "Static");
		MapBase("Raichu", "Static");
		MapBase("Zapdos", "Static");
		MapBase("Ampharos", "Static");

		MapBase("Talonflame", "FlameBody");
		MapBase("Magmortar", "FlameBody");
		MapBase("Chandelure", "FlameBody");
		MapBase("Volcarona", "FlameBody");

		MapBase("Machamp", "Guts");
		MapBase("Heracross", "Guts");
		MapBase("Ursaring", "Guts");
		MapBase("Conkeldurr", "Guts");
		MapBase("Flareon", "Guts");

		MapBase("Clefable", "MagicGuard");
		MapBase("Alakazam", "MagicGuard");
		MapBase("Reuniclus", "MagicGuard");

		MapBase("Gliscor", "PoisonHeal");
		MapBase("Breloom", "PoisonHeal");

		MapBase("Azumarill", "HugePower");
		MapBase("Diggersby", "HugePower");
		MapBase("Medicham", "HugePower");

		MapBase("Scizor", "Technician");
		MapBase("Ambipom", "Technician");
		MapBase("Persian", "Technician");

		MapBase("Sylveon", "Pixilate");

		// 3. Complete Canon & Fangame Mega Evolution Abilities
		MapMega("venusaur/mega", "ThickFat");
		MapMega("charizard/mega_x", "ToughClaws");
		MapMega("charizard/mega_y", "Drought");
		MapMega("blastoise/mega", "MegaLauncher");
		MapMega("beedrill/mega", "Adaptability");
		MapMega("pidgeot/mega", "NoGuard");
		MapMega("raichu/mega_x", "LightningRod");
		MapMega("raichu/mega_y", "SurgeSurfer");
		MapMega("clefable/mega", "MagicGuard");
		MapMega("alakazam/mega", "Trace");
		MapMega("slowbro/mega", "ShellArmor");
		MapMega("gengar/mega", "ShadowTag");
		MapMega("kangaskhan/mega", "ParentalBond");
		MapMega("pinsir/mega", "Aerilate");
		MapMega("gyarados/mega", "MoldBreaker");
		MapMega("aerodactyl/mega", "ToughClaws");
		MapMega("victreebel/mega", "Chlorophyll");
		MapMega("starmie/mega", "Analytic");
		MapMega("dragonite/mega", "Multiscale");
		MapMega("mewtwo/mega_x", "Steadfast");
		MapMega("mewtwo/mega_y", "Insomnia");
		MapMega("meganium/mega", "Triage");
		MapMega("feraligatr/mega", "SheerForce");
		MapMega("ampharos/mega", "MoldBreaker");
		MapMega("steelix/mega", "SandForce");
		MapMega("scizor/mega", "Technician");
		MapMega("heracross/mega", "SkillLink");
		MapMega("houndoom/mega", "SolarPower");
		MapMega("tyranitar/mega", "SandStream");
		MapMega("skarmory/mega", "Sturdy");
		MapMega("sceptile/mega", "LightningRod");
		MapMega("blaziken/mega", "SpeedBoost");
		MapMega("swampert/mega", "SwiftSwim");
		MapMega("gardevoir/mega", "Pixilate");
		MapMega("sableye/mega", "MagicBounce");
		MapMega("mawile/mega", "HugePower");
		MapMega("aggron/mega", "Filter");
		MapMega("medicham/mega", "HugePower");
		MapMega("manectric/mega", "Intimidate");
		MapMega("sharpedo/mega", "StrongJaw");
		MapMega("camerupt/mega", "SheerForce");
		MapMega("altaria/mega", "Pixilate");
		MapMega("banette/mega", "Prankster");
		MapMega("chimecho/mega", "Levitate");
		MapMega("absol/mega", "MagicBounce");
		MapMega("glalie/mega", "Refrigerate");
		MapMega("salamence/mega", "Aerilate");
		MapMega("metagross/mega", "ToughClaws");
		MapMega("latias/mega", "Levitate");
		MapMega("latios/mega", "Levitate");
		MapMega("kyogre/primal", "PrimordialSea");
		MapMega("groudon/primal", "DesolateLand");
		MapMega("rayquaza/mega", "DeltaStream");
		MapMega("staraptor/mega", "Reckless");
		MapMega("lopunny/mega", "Scrappy");
		MapMega("garchomp/mega", "SandForce");
		MapMega("lucario/mega", "Adaptability");
		MapMega("abomasnow/mega", "SnowWarning");
		MapMega("gallade/mega", "InnerFocus");
		MapMega("froslass/mega", "CursedBody");
		MapMega("heatran/mega", "FlameBody");
		MapMega("darkrai/mega", "BadDreams");
		MapMega("emboar/mega", "Reckless");
		MapMega("excadrill/mega", "SandRush");
		MapMega("audino/mega", "Healer");
		MapMega("scolipede/mega", "SpeedBoost");
		MapMega("scrafty/mega", "Intimidate");
		MapMega("eelektross/mega", "Levitate");
		MapMega("chandelure/mega", "ShadowTag");
		MapMega("golurk/mega", "IronFist");
		MapMega("chesnaught/mega", "Bulletproof");
		MapMega("delphox/mega", "MagicGuard");
		MapMega("greninja/mega", "BattleBond");
		MapMega("pyroar/mega", "Moxie");
		MapMega("floette/mega", "FlowerVeil");
		MapMega("meowstic/mega", "Prankster");
		MapMega("malamar/mega", "Contrary");
		MapMega("barbaracle/mega", "ToughClaws");
		MapMega("dragalge/mega", "Adaptability");
		MapMega("hawlucha/mega", "Unburden");
		MapMega("zygarde/mega", "PowerConstruct");
		MapMega("diancie/mega", "MagicBounce");
		MapMega("crabominable/mega", "IronFist");
		MapMega("golisopod/mega", "EmergencyExit");
		MapMega("drampa/mega", "Berserk");
		MapMega("magearna/mega", "SoulHeart");
		MapMega("zeraora/mega", "VoltAbsorb");
		MapMega("falinks/mega", "Defiant");
		MapMega("scovillain/mega", "Moody");
		MapMega("glimmora/mega", "ToxicDebris");
		MapMega("tatsugiri/mega", "Commander");
		MapMega("baxcalibur/mega", "ThermalExchange");
	}

	public static string NormalizeKey(string key)
	{
		if (string.IsNullOrEmpty(key)) return "";
		return key.Replace("/", "").Replace("-", "").Replace("_", "").Trim().ToLowerInvariant();
	}

	private static void RegisterAbility(string name, string display, string desc)
	{
		_abilityData[name] = new AbilityInfo { Name = name, DisplayName = display, Description = desc };
	}

	private static void MapBase(string species, string ability)
	{
		_speciesBaseAbilities[species] = ability;
		_speciesBaseAbilities[NormalizeKey(species)] = ability;
	}

	private static void MapMega(string megaInternal, string ability)
	{
		_speciesMegaAbilities[megaInternal] = ability;
		_speciesMegaAbilities[NormalizeKey(megaInternal)] = ability;
		string slashVariant = megaInternal.Replace('-', '/').Replace('_', '/');
		_speciesMegaAbilities[slashVariant] = ability;
		string dashVariant = megaInternal.Replace('/', '-').Replace('_', '-');
		_speciesMegaAbilities[dashVariant] = ability;
		string underscoreVariant = megaInternal.Replace('/', '_').Replace('-', '_');
		_speciesMegaAbilities[underscoreVariant] = ability;
	}

	public static string GetDefaultAbility(PokemonSpecies species, bool isMega = false)
	{
		if (species == null) return "None";

		if (isMega || species.IsMega)
		{
			string internalKey = (species.InternalName ?? "").Trim();
			if (_speciesMegaAbilities.TryGetValue(internalKey, out var megaAb))
				return megaAb;

			if (_speciesMegaAbilities.TryGetValue(NormalizeKey(internalKey), out var megaAbNorm))
				return megaAbNorm;

			string dashKey = internalKey.Replace('/', '-').Replace('_', '-');
			if (_speciesMegaAbilities.TryGetValue(dashKey, out var megaAb2))
				return megaAb2;

			string slashKey = internalKey.Replace('-', '/').Replace('_', '/');
			if (_speciesMegaAbilities.TryGetValue(slashKey, out var megaAb3))
				return megaAb3;

			if (!dashKey.EndsWith("-mega") && _speciesMegaAbilities.TryGetValue(dashKey + "-mega", out var megaAb4))
				return megaAb4;
			if (_speciesMegaAbilities.TryGetValue(NormalizeKey(dashKey + "mega"), out var megaAb5))
				return megaAb5;
		}

		if (_speciesBaseAbilities.TryGetValue(species.Name, out var baseAb))
		{
			return baseAb;
		}

		if (!string.IsNullOrEmpty(species.InternalName) && _speciesBaseAbilities.TryGetValue(NormalizeKey(species.InternalName), out var baseAbNorm))
		{
			return baseAbNorm;
		}

		if (!string.IsNullOrEmpty(species.BaseSpeciesName) && _speciesBaseAbilities.TryGetValue(species.BaseSpeciesName, out var parentAb))
		{
			return parentAb;
		}

		// Authentic type defaults
		return species.PrimaryType switch
		{
			PokemonType.Fire => "Blaze",
			PokemonType.Water => "Torrent",
			PokemonType.Grass => "Overgrow",
			PokemonType.Electric => "Static",
			PokemonType.Flying => "KeenEye",
			PokemonType.Fighting => "Guts",
			PokemonType.Psychic => "InnerFocus",
			PokemonType.Poison => "PoisonPoint",
			PokemonType.Ground => "SandVeil",
			PokemonType.Rock => "RockHead",
			PokemonType.Bug => "Swarm",
			PokemonType.Ghost => "Levitate",
			PokemonType.Dragon => "Pressure",
			PokemonType.Steel => "Sturdy",
			PokemonType.Dark => "Intimidate",
			PokemonType.Ice => "IceBody",
			PokemonType.Fairy => "CuteCharm",
			_ => "RunAway"
		};
	}

	public static AbilityInfo GetInfo(string abilityName)
	{
		if (string.IsNullOrEmpty(abilityName)) return null;
		if (_abilityData.TryGetValue(abilityName, out var info)) return info;
		return new AbilityInfo { Name = abilityName, DisplayName = abilityName, Description = "Habilidade passiva de Pokémon." };
	}

	public static string GetDisplayName(string abilityName)
	{
		var info = GetInfo(abilityName);
		return info != null ? info.DisplayName : (abilityName ?? "Nenhuma");
	}

	public static string GetDescription(string abilityName)
	{
		var info = GetInfo(abilityName);
		return info != null ? info.Description : "Nenhuma habilidade passiva atribuída.";
	}
}
