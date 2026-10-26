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
				RegisterAbility("Adaptability", "Adaptabilidade (Adaptability)", "Aumenta o bônus de mesmo tipo (STAB) de 1.5x para 2.0x.");
		RegisterAbility("Aerilate", "Pele Celeste (Aerilate)", "Converte ataques Normais em tipo Voador com 20% mais poder.");
		RegisterAbility("Aftermath", "Póstumo (Aftermath)", "Causa dano de 25% do HP ao atacante quando o portador é nocauteado por golpe de contato.");
		RegisterAbility("Analytic", "Analítico (Analytic)", "Aumenta o dano em 30% caso o usuário ataque por último no turno.");
		RegisterAbility("AngerShell", "Carapaça Furiosa (Anger Shell)", "Ao cair abaixo de 50% do HP, reduz Defesa e Def. Esp., mas sobe muito Ataque, Atq. Esp. e Velocidade.");
		RegisterAbility("Anticipation", "Antecipação (Anticipation)", "Sente o perigo de golpes super-efetivos ou de nocaute imediato do oponente ao entrar em campo.");
		RegisterAbility("AromaVeil", "Véu Aromático (Aroma Veil)", "Protege o portador e aliados contra movimentos que limitem o uso de golpes (como Taunt ou Torment).");
		RegisterAbility("AsOneShadowRider", "União Sombria (As One)", "Combina Unnerve e Grim Neigh, aumentando o Atq. Esp. a cada oponente nocauteado.");
		RegisterAbility("AuraBreak", "Quebra de Aura (Aura Break)", "Inverte os efeitos de Fairy Aura e Dark Aura, enfraquecendo seus respectivos golpes.");
		RegisterAbility("AuraGuard", "Guarda de Aura (Aura Guard)", "Reduz o dano de ataques sofridos em 25%.");
		RegisterAbility("BadDreams", "Pesadelos (Bad Dreams)", "Reduz o HP de Pokémon adversários adormecidos.");
		RegisterAbility("BallFetch", "Recuperar Bola (Ball Fetch)", "Recupera a primeira Poké Bola perdida em uma tentativa falha de captura fora de batalha.");
		RegisterAbility("Battery", "Bateria (Battery)", "Aumenta o poder dos ataques especiais dos aliados em combate.");
		RegisterAbility("BattleArmor", "Armadura de Batalha (Battle Armor)", "A carapaça rígida do portador impede completamente que ele receba golpes críticos.");
		RegisterAbility("BattleBond", "Vínculo de Batalha (Battle Bond)", "Aumenta os atributos após derrotar um oponente.");
		RegisterAbility("BeadsOfRuin", "Contas da Ruína (Beads of Ruin)", "O poder sinistro das contas reduz a Defesa Especial de todos os outros Pokémon em 25%.");
		RegisterAbility("BeastBoost", "Impulso Bestial (Beast Boost)", "Aumenta o atributo mais alto do portador em 1 estágio toda vez que derrota um oponente.");
		RegisterAbility("Berserk", "Frenesi (Berserk)", "Aumenta o Ataque Especial ao sofrer dano que deixe o HP abaixo de 50%.");
		RegisterAbility("BigPecks", "Peitoral Rígido (Big Pecks)", "Impede que a Defesa do Pokémon seja reduzida pelo adversário.");
		RegisterAbility("Blaze", "Chama (Blaze)", "Aumenta o poder de golpes de Fogo em 50% quando o HP está crítico.");
		RegisterAbility("Bulletproof", "À Prova de Balas (Bulletproof)", "Imunidade a ataques baseados em bombas, esferas e canhões.");
		RegisterAbility("CheekPouch", "Bochechas Cheias (Cheek Pouch)", "Restaura 1/3 do HP máximo adicional sempre que consome uma Fruta (Berrie).");
		RegisterAbility("ChillingNeigh", "Relincho Glacial (Chilling Neigh)", "Ao nocautear um Pokémon adversário, seu Ataque Físico sobe em 1 estágio.");
		RegisterAbility("Chlorophyll", "Clorofila (Chlorophyll)", "Dobra a Velocidade sob luz solar intensa.");
		RegisterAbility("ClearBody", "Corpo Límpido (Clear Body)", "Impede que oponentes reduzam quaisquer atributos do portador.");
		RegisterAbility("ColorChange", "Mimetismo (Color Change)", "Muda o tipo do Pokémon para o tipo do último ataque danoso sofrido.");
		RegisterAbility("Comatose", "Comatoso (Comatose)", "O Pokémon age normalmente acordado, mas é imune a todas as condições de status comuns.");
		RegisterAbility("Commander", "Comandante (Commander)", "Assume o comando tático do parceiro em batalhas duplas.");
		RegisterAbility("Competitive", "Competitividade (Competitive)", "Aumenta o Atq. Especial em 2 estágios sempre que qualquer atributo seu for reduzido pelo oponente.");
		RegisterAbility("CompoundEyes", "Olhos Compostos (Compound Eyes)", "Aumenta a precisão de todos os movimentos do Pokémon em 30%.");
		RegisterAbility("Contrary", "Contrário (Contrary)", "Inverte modificadores de atributos (aumentos viram reduções e vice-versa).");
		RegisterAbility("Corrosion", "Corrosão (Corrosion)", "Permite envenenar ou intoxicar oponentes mesmo sendo do tipo Aço ou Veneno.");
		RegisterAbility("CottonDown", "Plumas de Algodão (Cotton Down)", "Ao ser atingido por um ataque, espalha algodão reduzindo a Velocidade de todos os Pokémon em campo.");
		RegisterAbility("CudChew", "Ruminação (Cud Chew)", "Permite consumir a mesma Fruta (Berrie) novamente no final do turno seguinte ao seu consumo.");
		RegisterAbility("CuriousMedicine", "Medicina Curiosa (Curious Medicine)", "Ao entrar em batalha, remove todos os modificadores de atributos dos aliados.");
		RegisterAbility("CursedBody", "Corpo Amaldiçoado (Cursed Body)", "Ao sofrer um ataque, 30% de chance de desativar o golpe sofrido por alguns turnos.");
		RegisterAbility("CuteCharm", "Charme Fofo (Cute Charm)", "Ao sofrer um ataque de contato, tem 30% de chance de apaixonar o agressor.");
		RegisterAbility("Damp", "Umidade (Damp)", "Previne que qualquer Pokémon use movimentos autodestrutivos como Self-Destruct ou Explosion.");
		RegisterAbility("Dancer", "Dançarino (Dancer)", "Copia e executa imediatamente qualquer movimento de dança usado por outro Pokémon em combate.");
		RegisterAbility("DarkAura", "Aura Sombria (Dark Aura)", "Aumenta o poder dos movimentos do tipo Sombrio de todos os Pokémon em 33%.");
		RegisterAbility("DauntlessShield", "Escudo Destemido (Dauntless Shield)", "Eleva a Defesa em 1 estágio na primeira vez que entra na batalha.");
		RegisterAbility("Dazzling", "Deslumbrante (Dazzling)", "Surpreende o oponente e anula movimentos com prioridade positiva direcionados ao portador.");
		RegisterAbility("Defeatist", "Derrotista (Defeatist)", "Reduz o Ataque e o Ataque Especial pela metade quando o HP cai para 50% ou menos.");
		RegisterAbility("Defiant", "Competição Feroz (Defiant)", "Aumenta o Ataque Físico em 2 estágios sempre que qualquer atributo seu for reduzido pelo oponente.");
		RegisterAbility("DeltaStream", "Fluxo Delta (Delta Stream)", "Invoca ventos misteriosos que eliminam fraquezas elementais de Pokémon Voadores.");
		RegisterAbility("DesolateLand", "Terra Desolada (Desolate Land)", "Invoca sol abrasador extremo que evapora instantaneamente qualquer ataque do tipo Água.");
		RegisterAbility("Disguise", "Disfarce (Disguise)", "Protege contra o primeiro golpe que causaria dano na batalha, perdendo uma fração de HP ao romper.");
		RegisterAbility("Dragonize", "Dragonizar (Dragonize)", "Converte ataques do tipo Normal em golpes do tipo Dragão com 20% mais poder.");
		RegisterAbility("DragonsMaw", "Mandíbula do Dragão (Dragon's Maw)", "Aumenta o poder de movimentos do tipo Dragão em 50%.");
		RegisterAbility("Drizzle", "Garoa (Drizzle)", "Invoca chuva torrencial duradoura no campo assim que o portador entra na batalha.");
		RegisterAbility("Drought", "Seca (Drought)", "Invoca luz solar intensa e duradoura no campo assim que o portador entra na batalha.");
		RegisterAbility("DrySkin", "Pele Seca (Dry Skin)", "Restaura HP sob chuva e ao sofrer golpes de Água, mas sofre mais dano de Fogo e sol.");
		RegisterAbility("EarlyBird", "Madrugador (Early Bird)", "Acorda da condição de Sono duas vezes mais rápido do que o normal.");
		RegisterAbility("EarthEater", "Devorador de Terra (Earth Eater)", "Absorve ataques do tipo Terrestre, recuperando 25% do HP máximo em vez de sofrer dano.");
		RegisterAbility("Eelevate", "Eelevação (Eelevate)", "O corpo eletrizado levita sobre o solo, conferindo imunidade completa a ataques Terrestres.");
		RegisterAbility("EffectSpore", "Esporos de Efeito (Effect Spore)", "Ao sofrer um golpe de contato, tem 30% de chance de causar Sono, Veneno ou Paralisia no atacante.");
		RegisterAbility("ElectricSurge", "Campo Elétrico (Electric Surge)", "Eleva o Terreno Elétrico ao entrar em batalha por 5 turnos, impedindo o sono e fortalecendo a eletricidade.");
		RegisterAbility("Electromorphosis", "Eletromorfose (Electromorphosis)", "Fica carregado de energia ao sofrer dano, dobrando o poder do seu próximo ataque elétrico.");
		RegisterAbility("EmergencyExit", "Saída de Emergência (Emergency Exit)", "Retorna à Poké Bola automaticamente quando seu HP cai para 50% ou menos.");
		RegisterAbility("FairyAura", "Aura das Fadas (Fairy Aura)", "Aumenta o poder dos movimentos do tipo Fada de todos os Pokémon em 33%.");
		RegisterAbility("Filter", "Filtro (Filter)", "Reduz em 25% o dano de ataques super-efetivos sofridos.");
		RegisterAbility("FireMane", "Juba de Fogo (Fire Mane)", "Aumenta o dano de golpes de Fogo em 30% e previne queimaduras.");
		RegisterAbility("FlameBody", "Corpo Flamejante (Flame Body)", "Ao sofrer um golpe de contato, tem 30% de chance de queimar o atacante.");
		RegisterAbility("FlashFire", "Fogo Reluzente (Flash Fire)", "Imunidade a ataques de Fogo; absorve as chamas para fortalecer seus próprios golpes de Fogo em 50%.");
		RegisterAbility("FlowerGift", "Presente Floral (Flower Gift)", "Sob sol forte, aumenta o Ataque e a Defesa Especial do portador e de seus aliados em 50%.");
		RegisterAbility("FlowerVeil", "Véu de Flores (Flower Veil)", "Impede a redução de atributos e problemas de status em Pokémon aliados do tipo Planta.");
		RegisterAbility("Fluffy", "Fofura (Fluffy)", "Reduz à metade o dano sofrido por golpes de contato, mas dobra o dano sofrido de ataques de Fogo.");
		RegisterAbility("Forecast", "Previsão do Tempo (Forecast)", "Transforma a forma e o tipo elemental de Castform de acordo com as condições climáticas em campo.");
		RegisterAbility("Forewarn", "Premonição (Forewarn)", "Identifica o golpe mais forte do Pokémon adversário no início da batalha.");
		RegisterAbility("FriendGuard", "Guarda Amiga (Friend Guard)", "Reduz em 25% todo o dano sofrido pelos aliados em combate.");
		RegisterAbility("Frisk", "Revista (Frisk)", "Identifica o item segurado pelo adversário ao entrar em batalha.");
		RegisterAbility("FullMetalBody", "Corpo Metálico (Full Metal Body)", "Impede que movimentos ou habilidades dos adversários reduzam os atributos do portador.");
		RegisterAbility("FurCoat", "Manto de Pelos (Fur Coat)", "Dobra a Defesa Física efetiva do Pokémon contra todos os ataques físicos.");
		RegisterAbility("Gluttony", "Gula (Gluttony)", "Consome Frutas (Berries) quando o HP atinge 50% em vez de esperar pelos 25% usuais.");
		RegisterAbility("GoodAsGold", "Bom como Ouro (Good as Gold)", "O corpo de ouro puro confere imunidade absoluta a todos os movimentos de status adversários.");
		RegisterAbility("Gooey", "Viscosidade (Gooey)", "Reduz a Velocidade do adversário em 1 estágio ao sofrer um ataque de contato.");
		RegisterAbility("GorillaTactics", "Táticas de Gorila (Gorilla Tactics)", "Aumenta o Ataque em 50%, mas limita o Pokémon a usar apenas o primeiro movimento escolhido.");
		RegisterAbility("GrassySurge", "Campo de Grama (Grassy Surge)", "Espalha um manto de grama revitalizante por 5 turnos, curando HP e fortalecendo golpes de Planta.");
		RegisterAbility("GrimNeigh", "Relincho Sinistro (Grim Neigh)", "Ao nocautear um oponente, seu Ataque Especial aumenta em 1 estágio.");
		RegisterAbility("GulpMissile", "Míssil Voraz (Gulp Missile)", "Apanha uma presa na boca ao usar Surf ou Dive e cospe-a no atacante causando dano e status.");
		RegisterAbility("Guts", "Bravura (Guts)", "Aumenta o Ataque em 50% sob problemas de status, ignorando a perda por queimadura.");
		RegisterAbility("HadronEngine", "Motor de Hadrons (Hadron Engine)", "Invoca Terreno Elétrico ao entrar na batalha e aumenta o Ataque Especial em 33% sobre ele.");
		RegisterAbility("Healer", "Curandeiro (Healer)", "Chance de curar problemas de status dos aliados.");
		RegisterAbility("HoneyGather", "Coleta de Mel (Honey Gather)", "O Pokémon pode colher mel doce e perfumado durante viagens e após batalhas.");
		RegisterAbility("Hospitality", "Hospitalidade (Hospitality)", "Ao entrar em batalha, serve um chá restaurador que recupera 25% do HP do aliado.");
		RegisterAbility("HugePower", "Poder Gigante (Huge Power)", "Dobra o Ataque Físico efetivo do portador.");
		RegisterAbility("HungerSwitch", "Alternância de Fome (Hunger Switch)", "Muda entre a Forma Satisfeita e a Forma Voraz no final de cada turno.");
		RegisterAbility("Hustle", "Entusiasmo (Hustle)", "Aumenta o Ataque em 50%, mas reduz a precisão dos ataques físicos em 20%.");
		RegisterAbility("Hydration", "Hidratação (Hydration)", "Cura todas as condições de status adversas no final do turno se estiver chovendo.");
		RegisterAbility("HyperCutter", "Hipercorte (Hyper Cutter)", "As pinças poderosas impedem que outros Pokémon reduzam o Ataque do portador.");
		RegisterAbility("IceBody", "Corpo de Gelo (Ice Body)", "Regenera 1/16 do HP máximo no final de cada turno durante tempestades de neve ou granizo.");
		RegisterAbility("IceFace", "Rosto de Gelo (Ice Face)", "O bloco de gelo na cabeça absorve completamente um ataque físico; regenera-se durante a neve.");
		RegisterAbility("Illuminate", "Iluminação (Illuminate)", "A luz emitida pelo corpo previne a perda de precisão e atrai mais Pokémon selvagens.");
		RegisterAbility("Illusion", "Ilusão (Illusion)", "Entra em batalha disfarçado como o último Pokémon da sua equipe até receber dano direto.");
		RegisterAbility("Immunity", "Imunidade (Immunity)", "O sistema imunológico previne completamente que o Pokémon seja envenenado.");
		RegisterAbility("Infiltrator", "Infiltrador (Infiltrator)", "Ignora barreiras protetoras adversárias, como Reflect, Light Screen, Aurora Veil e Safeguard.");
		RegisterAbility("InnardsOut", "Entranhas Expostas (Innards Out)", "Ao ser nocauteado, causa dano ao adversário igual à quantidade exata de HP que restava.");
		RegisterAbility("InnerFocus", "Foco Interno (Inner Focus)", "Impedimento total de recuar por medo (Flinch).");
		RegisterAbility("Insomnia", "Insônia (Insomnia)", "Imunidade completa à condição de Sono.");
		RegisterAbility("Intimidate", "Intimidação (Intimidate)", "Reduz o Ataque do oponente ao entrar em batalha.");
		RegisterAbility("IntrepidSword", "Espada Intrépida (Intrepid Sword)", "Eleva o Ataque Físico em 1 estágio na primeira vez que entra na batalha.");
		RegisterAbility("IronBarbs", "Farpas de Ferro (Iron Barbs)", "Causa dano de 1/8 do HP máximo ao adversário sempre que ele desferir um golpe de contato.");
		RegisterAbility("IronFist", "Punho de Ferro (Iron Fist)", "Aumenta o poder de todos os ataques baseados em socos em 20%.");
		RegisterAbility("Justified", "Justificado (Justified)", "Aumenta o Ataque em 1 estágio toda vez que o portador é atingido por um golpe do tipo Sombrio.");
		RegisterAbility("KeenEye", "Olho Vivo (Keen Eye)", "Impede a perda de precisão e ignora aumentos na evasão do oponente.");
		RegisterAbility("LeafGuard", "Guarda de Folhas (Leaf Guard)", "Protege contra todas as condições de status enquanto houver luz solar intensa.");
		RegisterAbility("Levitate", "Levitação (Levitate)", "Imunidade completa a ataques do tipo Terrestre.");
		RegisterAbility("LightMetal", "Metal Leve (Light Metal)", "Reduz o peso corporal do Pokémon pela metade, diminuindo o dano sofrido de golpes baseados em peso.");
		RegisterAbility("LightningRod", "Para-raios (Lightning Rod)", "Atrai e anula golpes Elétricos, aumentando o Ataque Especial.");
		RegisterAbility("Limber", "Flexibilidade (Limber)", "O corpo flexível concede imunidade total à condição de Paralisia.");
		RegisterAbility("LiquidOoze", "Lodo Tóxico (Liquid Ooze)", "Oponentes que tentem drenar HP deste Pokémon sofrem dano em vez de se curarem.");
		RegisterAbility("MagicBounce", "Reflexo Mágico (Magic Bounce)", "Reflete movimentos de status e armadilhas de volta ao adversário.");
		RegisterAbility("MagicGuard", "Guarda Mágica (Magic Guard)", "Protege contra danos indiretos de veneno, queima e recuo.");
		RegisterAbility("Magician", "Mágico (Magician)", "Rouba o item segurado pelo alvo atingido quando o portador não estiver segurando nenhum item.");
		RegisterAbility("MagmaArmor", "Armadura de Magma (Magma Armor)", "O calor interno do magma concede imunidade completa ao congelamento.");
		RegisterAbility("MagnetPull", "Atração Magnética (Magnet Pull)", "Impede que Pokémon adversários do tipo Aço fujam ou sejam trocados durante a batalha.");
		RegisterAbility("MarvelScale", "Escama Maravilha (Marvel Scale)", "Aumenta a Defesa Física em 50% quando o Pokémon estiver com algum problema de status.");
		RegisterAbility("MegaLauncher", "Mega Disparador (Mega Launcher)", "Aumenta o poder de golpes de pulso e esfera em 50%.");
		RegisterAbility("MegaSol", "Mega Sol (Mega Sol)", "Intensifica a luz solar em 50%, fortalecendo golpes de Fogo e Planta.");
		RegisterAbility("Merciless", "Impiedoso (Merciless)", "Garante acertos críticos automáticos contra alvos que estejam envenenados ou intoxicados.");
		RegisterAbility("Mimicry", "Mimetismo de Terreno (Mimicry)", "Muda o tipo elemental do Pokémon de acordo com o terreno em campo.");
		RegisterAbility("MindsEye", "Olho da Mente (Mind's Eye)", "Ignora aumentos de evasão e permite acertar Pokémon do tipo Fantasma com golpes Normais e Lutadores.");
		RegisterAbility("Minus", "Negativo (Minus)", "Aumenta o Ataque Especial em 50% quando acompanhado de um aliado com Plus ou Minus.");
		RegisterAbility("MistySurge", "Campo de Névoa (Misty Surge)", "Espalha névoa mística por 5 turnos, prevenindo condições de status e reduzindo o dano de dragões.");
		RegisterAbility("MoldBreaker", "Quebra-Molde (Mold Breaker)", "Os movimentos do portador ignoram completamente as habilidades defensivas do alvo.");
		RegisterAbility("Moody", "Temperamental (Moody)", "Aumenta muito um atributo aleatório em 2 estágios e reduz outro em 1 estágio no final de cada turno.");
		RegisterAbility("MotorDrive", "Motor Elétrico (Motor Drive)", "Imunidade a golpes Elétricos; ao ser atingido por um, aumenta a sua Velocidade em 1 estágio.");
		RegisterAbility("Moxie", "Arrogância (Moxie)", "Aumenta o Ataque em 1 estágio toda vez que nocauteia um Pokémon adversário.");
		RegisterAbility("Multiscale", "Multiescamas (Multiscale)", "Reduz o dano sofrido em 50% quando o HP do portador está completamente cheio.");
		RegisterAbility("Mummy", "Múmia (Mummy)", "Ao receber um golpe de contato, substitui a habilidade do atacante pela habilidade Mummy.");
		RegisterAbility("MyceliumMight", "Força Micelial (Mycelium Might)", "Movimentos de status agem mais devagar, mas ignoram totalmente as habilidades protetoras do alvo.");
		RegisterAbility("NaturalCure", "Cura Natural (Natural Cure)", "Cura todas as condições de status adversas automaticamente ao ser recolhido para a Poké Bola.");
		RegisterAbility("Neuroforce", "Neuroforça (Neuroforce)", "Aumenta o poder de ataques super-efetivos desferidos pelo portador em 25%.");
		RegisterAbility("NoGuard", "Sem Guarda (No Guard)", "Garante que todos os ataques usados e recebidos tenham 100% de precisão.");
		RegisterAbility("Oblivious", "Alheio (Oblivious)", "Imunidade total à atração, paixão, provocação (Taunt) e intimidação.");
		RegisterAbility("Opportunist", "Oportunista (Opportunist)", "Copia imediatamente qualquer aumento de estágio de atributos obtido pelo adversário.");
		RegisterAbility("OrichalcumPulse", "Pulso de Oricálcio (Orichalcum Pulse)", "Invoca sol intenso ao entrar na batalha e aumenta o Ataque Físico em 33% sob ele.");
		RegisterAbility("Overcoat", "Sobretudo (Overcoat)", "Protege contra o dano de tempestades de areia, granizo e contra esporos e pós aromáticos.");
		RegisterAbility("Overgrow", "Supercrescimento (Overgrow)", "Aumenta o poder de golpes de Planta em 50% quando o HP está crítico.");
		RegisterAbility("OwnTempo", "Ritmo Próprio (Own Tempo)", "Mantém a compostura e impede completamente a condição de Confusão e Intimidação.");
		RegisterAbility("ParentalBond", "Vínculo Parental (Parental Bond)", "Ataca duas vezes no mesmo turno (o segundo golpe causa 25% de dano).");
		RegisterAbility("Pickup", "Coleta (Pickup)", "Pode encontrar e coletar itens raros pelo mapa e após batalhas.");
		RegisterAbility("PiercingDrill", "Broca Perfurante (Piercing Drill)", "Aumenta o dano de ataques perfurantes e golpes de Terra e Aço em 20%.");
		RegisterAbility("Pixilate", "Pele de Fada (Pixilate)", "Converte ataques Normais em tipo Fada com 20% mais poder.");
		RegisterAbility("Plus", "Positivo (Plus)", "Aumenta o Ataque Especial em 50% quando acompanhado de um aliado com Minus ou Plus.");
		RegisterAbility("PoisonHeal", "Cura por Veneno (Poison Heal)", "Em vez de sofrer dano por veneno, regenera 1/8 de HP por turno.");
		RegisterAbility("PoisonPoint", "Ponto Venenoso (Poison Point)", "Ao sofrer um golpe de contato, tem 30% de chance de envenenar o agressor.");
		RegisterAbility("PoisonPuppeteer", "Titereiro Venenoso (Poison Puppeteer)", "Adversários envenenados pelo portador também ficam confusos instantaneamente.");
		RegisterAbility("PoisonTouch", "Toque Venenoso (Poison Touch)", "Tem 30% de chance de envenenar o alvo ao acertar um ataque que faça contato.");
		RegisterAbility("PowerConstruct", "Construto de Poder (Power Construct)", "Transforma-se na forma Completa quando atinge HP crítico.");
		RegisterAbility("PowerSpot", "Ponto de Poder (Power Spot)", "A presença do Pokémon energiza os aliados, aumentando o poder de seus ataques em 30%.");
		RegisterAbility("Prankster", "Travessura (Prankster)", "Concede prioridade +1 para movimentos de status.");
		RegisterAbility("Pressure", "Pressão (Pressure)", "Pressiona o adversário pelo seu poder opulento, dobrando o consumo de PP de seus golpes.");
		RegisterAbility("PrimordialSea", "Mar Primordial (Primordial Sea)", "Invoca chuva torrencial inalterável por habilidades comuns.");
		RegisterAbility("Protean", "Proteano (Protean)", "Muda o tipo do Pokémon para o tipo do movimento que ele estiver prestes a executar.");
		RegisterAbility("Protosynthesis", "Protossíntese (Protosynthesis)", "Sob sol forte ou com Booster Energy, eleva o atributo mais alto do portador.");
		RegisterAbility("PsychicSurge", "Campo Psíquico (Psychic Surge)", "Cria um Terreno Psíquico por 5 turnos, bloqueando ataques de prioridade e fortalecendo o poder psíquico.");
		RegisterAbility("PunkRock", "Punk Rock (Punk Rock)", "Aumenta o dano de golpes sonoros em 30% e reduz à metade o dano de golpes sonoros sofridos.");
		RegisterAbility("PurePower", "Poder Puro (Pure Power)", "Canaliza energia pura interior dobrando o Ataque Físico efetivo do portador.");
		RegisterAbility("PurifyingSalt", "Sal Purificador (Purifying Salt)", "Imunidade a todas as condições de status e reduz à metade o dano sofrido de golpes Fantasma.");
		RegisterAbility("QuarkDrive", "Propulsor de Quarks (Quark Drive)", "No Terreno Elétrico ou com Booster Energy, eleva o atributo mais alto do portador.");
		RegisterAbility("QuickDraw", "Saque Rápido (Quick Draw)", "Tem 30% de chance de agir primeiro dentro de sua faixa de prioridade de movimento.");
		RegisterAbility("Rattled", "Assustadiço (Rattled)", "Aumenta a Velocidade em 1 estágio ao sofrer ataques dos tipos Inseto, Fantasma ou Sombrio.");
		RegisterAbility("Receiver", "Receptor (Receiver)", "Herda e assume a habilidade de um aliado assim que ele for derrotado em combate duplo.");
		RegisterAbility("Reckless", "Audácia (Reckless)", "Aumenta o dano de ataques com dano de recuo em 20%.");
		RegisterAbility("Refrigerate", "Refrigeração (Refrigerate)", "Converte ataques Normais em tipo Gelo com 20% mais poder.");
		RegisterAbility("Regenerator", "Regenerador (Regenerator)", "Restaura 1/3 do HP máximo ao ser recolhido do campo.");
		RegisterAbility("Ripen", "Amadurecimento (Ripen)", "Dobra os efeitos e a recuperação concedidos pelo consumo de qualquer Fruta (Berrie).");
		RegisterAbility("Rivalry", "Rivalidade (Rivalry)", "Aumenta o dano em 25% contra oponentes do mesmo sexo, mas causa 25% menos contra o sexo oposto.");
		RegisterAbility("RockHead", "Cabeça Dura (Rock Head)", "Protege o Pokémon contra qualquer dano de recuo de seus próprios ataques potentes.");
		RegisterAbility("RoughSkin", "Pele Áspera (Rough Skin)", "Causa dano de 1/8 do HP máximo ao adversário toda vez que ele desferir um golpe de contato.");
		RegisterAbility("RunAway", "Fuga Rápida (Run Away)", "Garante fuga infalível de qualquer batalha contra Pokémon selvagens.");
		RegisterAbility("SandForce", "Força da Areia (Sand Force)", "Sob Tempestade de Areia, aumenta o dano de golpes Terrestre, Pedra e Aço em 30%.");
		RegisterAbility("SandRush", "Ímpeto na Areia (Sand Rush)", "Dobra a Velocidade durante Tempestades de Areia.");
		RegisterAbility("SandSpit", "Cuspe de Areia (Sand Spit)", "Invoca uma tempestade de areia imediatamente ao ser atingido por um ataque.");
		RegisterAbility("SandStream", "Fluxo de Areia (Sand Stream)", "Invoca uma violenta tempestade de areia duradoura ao entrar no campo de batalha.");
		RegisterAbility("SandVeil", "Véu de Areia (Sand Veil)", "Aumenta a evasão em 20% e concede imunidade ao dano durante tempestades de areia.");
		RegisterAbility("SapSipper", "Sugador de Seiva (Sap Sipper)", "Imunidade a golpes do tipo Planta; ao ser atingido por um, aumenta seu Ataque em 1 estágio.");
		RegisterAbility("Schooling", "Cardume (Schooling)", "Quando o HP está acima de 25%, forma um colossal e temível cardume de combate de Wishiwashi.");
		RegisterAbility("Scrappy", "Combatente (Scrappy)", "Permite acertar Pokémon do tipo Fantasma com movimentos Normais e Lutadores, e previne Intimidação.");
		RegisterAbility("SeedSower", "Semeador (Seed Sower)", "Cria um Campo de Grama por 5 turnos imediatamente ao sofrer dano de um ataque.");
		RegisterAbility("SereneGrace", "Graça Serena (Serene Grace)", "Dobra a chance de ativação dos efeitos secundários de todos os movimentos do portador.");
		RegisterAbility("ShadowShield", "Escudo Sombrio (Shadow Shield)", "Reduz o dano sofrido em 50% quando o HP do portador estiver completamente cheio.");
		RegisterAbility("ShadowTag", "Armadilha das Sombras (Shadow Tag)", "Impede a fuga ou troca dos Pokémon adversários.");
		RegisterAbility("Sharpness", "Fio Afiado (Sharpness)", "Aumenta o poder de todos os movimentos cortantes e lâminas afiadas em 50%.");
		RegisterAbility("ShedSkin", "Troca de Pele (Shed Skin)", "Tem 33% de chance de curar qualquer condição de status adversa no final de cada turno.");
		RegisterAbility("SheerForce", "Força Bruta (Sheer Force)", "Aumenta o poder dos ataques com efeitos secundários em 30%, mas anula esses efeitos secundários.");
		RegisterAbility("ShellArmor", "Armadura de Concha (Shell Armor)", "A concha robusta protege o Pokémon completamente contra golpes críticos adversários.");
		RegisterAbility("ShieldDust", "Pó Blindado (Shield Dust)", "O pó de escamas bloqueia todos os efeitos secundários de ataques adversários sofridos.");
		RegisterAbility("Simple", "Simplicidade (Simple)", "Dobra todos os aumentos e reduções de estágios de atributos recebidos pelo Pokémon.");
		RegisterAbility("SkillLink", "Sequência Perfeita (Skill Link)", "Garante que ataques de múltiplos golpes (como Bullet Seed) sempre atinjam o número máximo de 5 vezes.");
		RegisterAbility("SlowStart", "Começo Lento (Slow Start)", "Reduz o Ataque e a Velocidade pela metade durante os primeiros 5 turnos na batalha.");
		RegisterAbility("SlushRush", "Ímpeto na Neve (Slush Rush)", "Dobra a Velocidade do portador enquanto estiver sob clima de neve ou granizo.");
		RegisterAbility("SnowCloak", "Manto de Neve (Snow Cloak)", "Aumenta a evasão em 20% e concede imunidade a danos causados pelo clima de neve/granizo.");
		RegisterAbility("SnowWarning", "Alerta de Neve (Snow Warning)", "Invoca uma tempestade de neve densa e duradoura ao entrar no campo de batalha.");
		RegisterAbility("SolarPower", "Poder Solar (Solar Power)", "Sob sol forte, aumenta o Atq. Especial em 50%, mas perde 1/8 do HP máximo a cada turno.");
		RegisterAbility("SolidRock", "Rocha Sólida (Solid Rock)", "Reduz em 25% o dano sofrido de ataques super-efetivos.");
		RegisterAbility("SoulHeart", "Coração da Alma (Soul-Heart)", "Aumenta o Ataque Especial em 1 estágio toda vez que qualquer Pokémon em campo é derrotado.");
		RegisterAbility("Soundproof", "À Prova de Som (Soundproof)", "Confere imunidade absoluta a todos os ataques baseados em som (como Hyper Voice ou Boomburst).");
		RegisterAbility("SpeedBoost", "Aceleração (Speed Boost)", "Aumenta a Velocidade do Pokémon em 1 estágio no final de cada turno em batalha.");
		RegisterAbility("SpicySpray", "Spray Apimentado (Spicy Spray)", "Reduz a Defesa do alvo em 1 estágio ao usar movimentos picantes ou de Fogo.");
		RegisterAbility("Stakeout", "Vigia (Stakeout)", "Dobra o dano causado contra um oponente que acabou de ser trocado para o campo.");
		RegisterAbility("Stalwart", "Inabalável (Stalwart)", "Ignora movimentos e habilidades dos adversários que redirecionariam os seus ataques.");
		RegisterAbility("Stamina", "Vigor (Stamina)", "Aumenta a Defesa em 1 estágio toda vez que o portador recebe dano de um ataque.");
		RegisterAbility("StanceChange", "Mudança de Postura (Stance Change)", "Alterna Aegislash entre a Forma de Escudo ao defender e a Forma de Espada ao desferir ataques.");
		RegisterAbility("Static", "Estática (Static)", "Ao sofrer um ataque de contato, tem 30% de chance de paralisar o atacante.");
		RegisterAbility("Steadfast", "Firmeza (Steadfast)", "Aumenta a Velocidade em 1 estágio sempre que o portador recuar por medo (Flinch).");
		RegisterAbility("SteamEngine", "Máquina a Vapor (Steam Engine)", "Ao ser atingido por um ataque de Fogo ou Água, sua Velocidade atinge instantaneamente o nível máximo (+6).");
		RegisterAbility("Steelworker", "Metalúrgico (Steelworker)", "Aumenta o poder de todos os movimentos do tipo Aço em 50%.");
		RegisterAbility("Stench", "Mau Cheiro (Stench)", "O odor desagradável tem 10% de chance de fazer o adversário recuar (Flinch) ao ser atacado.");
		RegisterAbility("StickyHold", "Adesão Firme (Sticky Hold)", "A pele grudenta impede completamente que o item segurado pelo Pokémon seja roubado ou removido.");
		RegisterAbility("StormDrain", "Drenagem Aquática (Storm Drain)", "Atrai e anula ataques do tipo Água, aumentando o Ataque Especial em 1 estágio.");
		RegisterAbility("StrongJaw", "Mandíbula Forte (Strong Jaw)", "Aumenta o poder de todos os movimentos baseados em mordidas (como Crunch) em 50%.");
		RegisterAbility("Sturdy", "Robustez (Sturdy)", "Imunidade a golpes de nocaute imediato (OHKO) e sobrevive com 1 HP a golpes que o derrotariam com HP cheio.");
		RegisterAbility("SuctionCups", "Ventosas (Suction Cups)", "Fixa o Pokémon firmemente no chão, impedindo que seja forçado a sair do combate.");
		RegisterAbility("SupersweetSyrup", "Xarope Superdoce (Supersweet Syrup)", "Ao entrar em batalha, espalha um xarope doce que reduz a evasão do oponente em 1 estágio.");
		RegisterAbility("SurgeSurfer", "Surfista de Onda (Surge Surfer)", "Dobra a Velocidade do portador enquanto estiver sobre um Terreno Elétrico.");
		RegisterAbility("Swarm", "Enxame (Swarm)", "Aumenta o poder de movimentos do tipo Inseto em 50% quando o HP está em 1/3 ou menos.");
		RegisterAbility("SweetVeil", "Véu Doce (Sweet Veil)", "Previne que o portador e seus aliados entrem na condição de Sono.");
		RegisterAbility("SwiftSwim", "Nado Rápido (Swift Swim)", "Dobra a Velocidade durante chuva torrencial.");
		RegisterAbility("SwordOfRuin", "Espada da Ruína (Sword of Ruin)", "O poder nefasto da espada ancestral reduz a Defesa Física de todos os outros Pokémon em 25%.");
		RegisterAbility("Synchronize", "Sincronia (Synchronize)", "Repassa automaticamente as condições de Queimadura, Paralisia ou Veneno para o adversário que as causou.");
		RegisterAbility("TabletsOfRuin", "Tabelas da Ruína (Tablets of Ruin)", "As escrituras antigas de ruína reduzem o Ataque Físico de todos os outros Pokémon em 25%.");
		RegisterAbility("TangledFeet", "Pés Desengonçados (Tangled Feet)", "Dobra a taxa de evasão do portador enquanto ele estiver na condição de Confusão.");
		RegisterAbility("Technician", "Técnico (Technician)", "Aumenta em 50% o poder de todos os movimentos com poder base menor ou igual a 60.");
		RegisterAbility("Telepathy", "Telepatia (Telepathy)", "Antecipa e esquiva-se completamente de ataques desferidos pelos próprios aliados em combate.");
		RegisterAbility("TeraformZero", "Teraforma Zero (Teraform Zero)", "Ao assumir a Forma Estelar, elimina instantaneamente todos os efeitos de clima e terreno ativos.");
		RegisterAbility("Teravolt", "Teravolt (Teravolt)", "Os movimentos do portador ignoram completamente todas as habilidades protetoras dos adversários.");
		RegisterAbility("ThermalExchange", "Troca Térmica (Thermal Exchange)", "Eleva o Ataque em 1 estágio ao ser atingido por Fogo e concede imunidade absoluta a queimaduras.");
		RegisterAbility("ThickFat", "Gordura Espessa (Thick Fat)", "A camada espessa de gordura reduz à metade o dano sofrido de ataques de Fogo e Gelo.");
		RegisterAbility("Torrent", "Torrente (Torrent)", "Aumenta o poder de movimentos do tipo Água em 50% quando o HP está em 1/3 ou menos.");
		RegisterAbility("ToughClaws", "Garras Poderosas (Tough Claws)", "Aumenta o poder de todos os ataques que fazem contato físico em 30%.");
		RegisterAbility("ToxicChain", "Corrente Tóxica (Toxic Chain)", "Os ataques do portador têm 30% de chance de intoxicar gravemente o alvo.");
		RegisterAbility("ToxicDebris", "Detritos Tóxicos (Toxic Debris)", "Ao sofrer dano físico, espalha uma camada de Toxic Spikes no campo adversário.");
		RegisterAbility("Trace", "Rastreio (Trace)", "Copia e assume a habilidade do Pokémon adversário assim que entra na batalha.");
		RegisterAbility("Transistor", "Transistor (Transistor)", "Aumenta o poder de todos os movimentos do tipo Elétrico em 30%.");
		RegisterAbility("Triage", "Triagem (Triage)", "Concede prioridade máxima (+3) a todos os movimentos que recuperam HP.");
		RegisterAbility("Truant", "Preguiçoso (Truant)", "O Pokémon fica descansando e só consegue agir a cada dois turnos.");
		RegisterAbility("Turboblaze", "Turbobolha de Fogo (Turboblaze)", "Os movimentos de fogo e ataques do portador ignoram completamente as habilidades protetoras do alvo.");
		RegisterAbility("Unaware", "Distraído (Unaware)", "Ignora todos os aumentos e reduções de atributos do oponente ao atacar ou defender.");
		RegisterAbility("Unburden", "Desembaraço (Unburden)", "Dobra a Velocidade assim que consome ou perde o seu item segurado.");
		RegisterAbility("UnseenFist", "Punho Oculto (Unseen Fist)", "Ataques de contato físico causam dano mesmo se o oponente usar Protect ou Detect.");
		RegisterAbility("VesselOfRuin", "Vaso da Ruína (Vessel of Ruin)", "O poder antigo do vaso sagrado reduz o Ataque Especial de todos os outros Pokémon em 25%.");
		RegisterAbility("VictoryStar", "Estrela da Vitória (Victory Star)", "Aumenta a precisão de todos os movimentos do portador e de seus aliados em 10%.");
		RegisterAbility("VitalSpirit", "Espírito Vital (Vital Spirit)", "O vigor constante concede imunidade absoluta à condição de Sono.");
		RegisterAbility("VoltAbsorb", "Absorção de Voltagem (Volt Absorb)", "Imunidade a ataques Elétricos; absorve a eletricidade recuperando 25% do HP máximo.");
		RegisterAbility("WanderingSpirit", "Espírito Errante (Wandering Spirit)", "Troca de habilidade com o atacante sempre que for atingido por um golpe de contato.");
		RegisterAbility("WaterAbsorb", "Absorção de Água (Water Absorb)", "Imunidade a ataques de Água; absorve o líquido recuperando 25% do HP máximo.");
		RegisterAbility("WaterBubble", "Bolha de Água (Water Bubble)", "Dobra o dano de golpes de Água, reduz o dano de Fogo pela metade e impede queimaduras.");
		RegisterAbility("WaterCompaction", "Compactação Aquática (Water Compaction)", "Aumenta a Defesa Física em 2 estágios sempre que for atingido por um golpe de Água.");
		RegisterAbility("WaterVeil", "Véu Aquático (Water Veil)", "O manto de água pura concede imunidade completa à condição de Queimadura.");
		RegisterAbility("WeakArmor", "Armadura Frágil (Weak Armor)", "Ao sofrer um ataque físico, reduz sua Defesa em 1 estágio, mas aumenta sua Velocidade em 2 estágios.");
		RegisterAbility("WellBakedBody", "Corpo Bem Assado (Well-Baked Body)", "Imunidade a golpes de Fogo; ao ser atingido por um, aumenta sua Defesa em 2 estágios.");
		RegisterAbility("WhiteSmoke", "Fumaça Branca (White Smoke)", "A fumaça protetora impede que os oponentes reduzam quaisquer atributos do portador.");
		RegisterAbility("WimpOut", "Covardia (Wimp Out)", "Foge da batalha ou troca de lugar na equipe assim que seu HP cai para 50% ou menos.");
		RegisterAbility("WindPower", "Poder dos Ventos (Wind Power)", "Fica carregado de energia ao sofrer um golpe de vento, fortalecendo seu próximo golpe Elétrico.");
		RegisterAbility("WindRider", "Cavaleiro do Vento (Wind Rider)", "Imunidade a ataques baseados em vento; ao ser atingido ou sob vento, aumenta o Ataque em 1 estágio.");
		RegisterAbility("WonderGuard", "Guarda Maravilha (Wonder Guard)", "Barreira misteriosa que concede imunidade total a qualquer ataque que não seja super-efetivo.");
		RegisterAbility("WonderSkin", "Pele Misteriosa (Wonder Skin)", "Faz com que movimentos de status direcionados ao portador tenham precisão máxima reduzida para 50%.");
		RegisterAbility("ZeroToHero", "Do Zero a Herói (Zero to Hero)", "Transforma-se na poderosa Forma Heroica ao ser trocado e retornar à batalha.");

		// 2. Canonical Species Abilities mapping (Gen 1-9)
		MapBase("Abomasnow", "SnowWarning");
		MapBase("Abra", "Synchronize");
		MapBase("Absol", "Sharpness");
		MapBase("Accelgor", "Hydration");
		MapBase("Aegislash", "StanceChange");
		MapBase("Aerodactyl", "ToughClaws");
		MapBase("Aggron", "Filter");
		MapBase("Aipom", "RunAway");
		MapBase("Alakazam", "Trace");
		MapBase("Alomomola", "Healer");
		MapBase("Altaria", "Pixilate");
		MapBase("Amaura", "Refrigerate");
		MapBase("Ambipom", "Technician");
		MapBase("Amoonguss", "EffectSpore");
		MapBase("Ampharos", "MoldBreaker");
		MapBase("Annihilape", "VitalSpirit");
		MapBase("Anorith", "BattleArmor");
		MapBase("Appletun", "Ripen");
		MapBase("Applin", "Ripen");
		MapBase("Araquanid", "WaterBubble");
		MapBase("Arbok", "Intimidate");
		MapBase("Arboliva", "SeedSower");
		MapBase("Arcanine", "Intimidate");
		MapBase("Archaludon", "Stamina");
		MapBase("Archen", "Defeatist");
		MapBase("Archeops", "Defeatist");
		MapBase("Arctibax", "ThermalExchange");
		MapBase("Arctovish", "WaterAbsorb");
		MapBase("Arctozolt", "VoltAbsorb");
		MapBase("Ariados", "Swarm");
		MapBase("Armaldo", "BattleArmor");
		MapBase("Armarouge", "FlashFire");
		MapBase("Aromatisse", "Healer");
		MapBase("Aron", "Sturdy");
		MapBase("Arrokuda", "SwiftSwim");
		MapBase("Articuno", "Competitive");
		MapBase("Audino", "Healer");
		MapBase("Aurorus", "Refrigerate");
		MapBase("Avalugg", "StrongJaw");
		MapBase("Axew", "Rivalry");
		MapBase("Azelf", "Levitate");
		MapBase("Azumarill", "ThickFat");
		MapBase("Azurill", "ThickFat");
		MapBase("Bagon", "RockHead");
		MapBase("Baltoy", "Levitate");
		MapBase("Banette", "Prankster");
		MapBase("Barbaracle", "ToughClaws");
		MapBase("Barboach", "Oblivious");
		MapBase("Barraskewda", "SwiftSwim");
		MapBase("Basculegion", "SwiftSwim");
		MapBase("Basculin", "Rattled");
		MapBase("Bastiodon", "Sturdy");
		MapBase("Baxcalibur", "ThermalExchange");
		MapBase("Bayleef", "Overgrow");
		MapBase("Beartic", "SnowCloak");
		MapBase("Beautifly", "Swarm");
		MapBase("Beedrill", "Adaptability");
		MapBase("Beheeyem", "Telepathy");
		MapBase("Beldum", "ClearBody");
		MapBase("Bellibolt", "Electromorphosis");
		MapBase("Bellossom", "Chlorophyll");
		MapBase("Bellsprout", "Chlorophyll");
		MapBase("Bergmite", "OwnTempo");
		MapBase("Bewear", "Fluffy");
		MapBase("Bibarel", "Simple");
		MapBase("Bidoof", "Simple");
		MapBase("Binacle", "ToughClaws");
		MapBase("Bisharp", "Defiant");
		MapBase("Blacephalon", "BeastBoost");
		MapBase("Blastoise", "Torrent");
		MapBase("Blaziken", "SpeedBoost");
		MapBase("Blipbug", "Swarm");
		MapBase("Blissey", "NaturalCure");
		MapBase("Blitzle", "LightningRod");
		MapBase("Boldore", "Sturdy");
		MapBase("Boltund", "StrongJaw");
		MapBase("Bombirdier", "BigPecks");
		MapBase("Bonsly", "Sturdy");
		MapBase("Bouffalant", "Reckless");
		MapBase("Bounsweet", "LeafGuard");
		MapBase("Braixen", "Blaze");
		MapBase("Brambleghast", "WindRider");
		MapBase("Bramblin", "WindRider");
		MapBase("Braviary", "KeenEye");
		MapBase("Breloom", "EffectSpore");
		MapBase("Brionne", "Torrent");
		MapBase("Bronzong", "Levitate");
		MapBase("Bronzor", "Levitate");
		MapBase("Brute Bonnet", "Protosynthesis");
		MapBase("Bruxish", "Dazzling");
		MapBase("Budew", "NaturalCure");
		MapBase("Buizel", "SwiftSwim");
		MapBase("Bulbasaur", "Overgrow");
		MapBase("Buneary", "RunAway");
		MapBase("Bunnelby", "Pickup");
		MapBase("Burmy", "ShedSkin");
		MapBase("Butterfree", "CompoundEyes");
		MapBase("Buzzwole", "BeastBoost");
		MapBase("Cacnea", "SandVeil");
		MapBase("Cacturne", "SandVeil");
		MapBase("Calyrex", "AsOneShadowRider");
		MapBase("Camerupt", "SheerForce");
		MapBase("Capsakid", "Chlorophyll");
		MapBase("Carbink", "ClearBody");
		MapBase("Carkol", "SteamEngine");
		MapBase("Carnivine", "Levitate");
		MapBase("Carracosta", "SolidRock");
		MapBase("Carvanha", "RoughSkin");
		MapBase("Cascoon", "ShedSkin");
		MapBase("Castform", "Forecast");
		MapBase("Caterpie", "ShieldDust");
		MapBase("Celebi", "NaturalCure");
		MapBase("Celesteela", "BeastBoost");
		MapBase("Centiskorch", "FlashFire");
		MapBase("Ceruledge", "FlashFire");
		MapBase("Cetitan", "ThickFat");
		MapBase("Cetoddle", "ThickFat");
		MapBase("Chandelure", "Infiltrator");
		MapBase("Chansey", "NaturalCure");
		MapBase("Charcadet", "FlashFire");
		MapBase("Charizard", "Blaze");
		MapBase("Charjabug", "Battery");
		MapBase("Charmander", "Blaze");
		MapBase("Charmeleon", "Blaze");
		MapBase("Chatot", "KeenEye");
		MapBase("Cherrim", "FlowerGift");
		MapBase("Cherubi", "Chlorophyll");
		MapBase("Chesnaught", "Bulletproof");
		MapBase("Chespin", "Overgrow");
		MapBase("Chewtle", "StrongJaw");
		MapBase("Chi-Yu", "BeadsOfRuin");
		MapBase("Chien-Pao", "SwordOfRuin");
		MapBase("Chikorita", "Overgrow");
		MapBase("Chimchar", "Blaze");
		MapBase("Chimecho", "Levitate");
		MapBase("Chinchou", "VoltAbsorb");
		MapBase("Chingling", "Levitate");
		MapBase("Cinccino", "CuteCharm");
		MapBase("Cinderace", "Blaze");
		MapBase("Clamperl", "ShellArmor");
		MapBase("Clauncher", "MegaLauncher");
		MapBase("Clawitzer", "MegaLauncher");
		MapBase("Claydol", "Levitate");
		MapBase("Clefable", "MagicBounce");
		MapBase("Clefairy", "CuteCharm");
		MapBase("Cleffa", "CuteCharm");
		MapBase("Clobbopus", "Limber");
		MapBase("Clodsire", "PoisonPoint");
		MapBase("Cloyster", "ShellArmor");
		MapBase("Coalossal", "SteamEngine");
		MapBase("Cobalion", "Justified");
		MapBase("Cofagrigus", "Mummy");
		MapBase("Combee", "HoneyGather");
		MapBase("Combusken", "Blaze");
		MapBase("Comfey", "FlowerVeil");
		MapBase("Conkeldurr", "Guts");
		MapBase("Copperajah", "SheerForce");
		MapBase("Corphish", "HyperCutter");
		MapBase("Corsola", "WeakArmor");
		MapBase("Corviknight", "Pressure");
		MapBase("Corvisquire", "KeenEye");
		MapBase("Cosmoem", "Sturdy");
		MapBase("Cosmog", "Unaware");
		MapBase("Cottonee", "Prankster");
		MapBase("Crabominable", "IronFist");
		MapBase("Crabrawler", "HyperCutter");
		MapBase("Cradily", "SuctionCups");
		MapBase("Cramorant", "GulpMissile");
		MapBase("Cranidos", "MoldBreaker");
		MapBase("Crawdaunt", "HyperCutter");
		MapBase("Cresselia", "Levitate");
		MapBase("Croagunk", "Anticipation");
		MapBase("Crobat", "InnerFocus");
		MapBase("Crocalor", "Blaze");
		MapBase("Croconaw", "Torrent");
		MapBase("Crustle", "Sturdy");
		MapBase("Cryogonal", "Levitate");
		MapBase("Cubchoo", "SnowCloak");
		MapBase("Cubone", "RockHead");
		MapBase("Cufant", "SheerForce");
		MapBase("Cursola", "WeakArmor");
		MapBase("Cutiefly", "HoneyGather");
		MapBase("Cyclizar", "ShedSkin");
		MapBase("Cyndaquil", "Blaze");
		MapBase("Dachsbun", "WellBakedBody");
		MapBase("Darkrai", "BadDreams");
		MapBase("Darmanitan", "GorillaTactics");
		MapBase("Dartrix", "Overgrow");
		MapBase("Darumaka", "Hustle");
		MapBase("Decidueye", "Overgrow");
		MapBase("Dedenne", "CheekPouch");
		MapBase("Deerling", "Chlorophyll");
		MapBase("Deino", "Hustle");
		MapBase("Delcatty", "CuteCharm");
		MapBase("Delibird", "VitalSpirit");
		MapBase("Delphox", "Levitate");
		MapBase("Deoxys", "Pressure");
		MapBase("Dewgong", "ThickFat");
		MapBase("Dewott", "Torrent");
		MapBase("Dewpider", "WaterBubble");
		MapBase("Dhelmise", "Steelworker");
		MapBase("Dialga", "Pressure");
		MapBase("Diancie", "MagicBounce");
		MapBase("Diggersby", "Pickup");
		MapBase("Diglett", "SandVeil");
		MapBase("Dipplin", "SupersweetSyrup");
		MapBase("Ditto", "Limber");
		MapBase("Dodrio", "RunAway");
		MapBase("Doduo", "RunAway");
		MapBase("Dolliv", "EarlyBird");
		MapBase("Dondozo", "Unaware");
		MapBase("Donphan", "Sturdy");
		MapBase("Dottler", "Swarm");
		MapBase("Doublade", "NoGuard");
		MapBase("Dracovish", "WaterAbsorb");
		MapBase("Dracozolt", "VoltAbsorb");
		MapBase("Dragalge", "Regenerator");
		MapBase("Dragapult", "ClearBody");
		MapBase("Dragonair", "ShedSkin");
		MapBase("Dragonite", "Multiscale");
		MapBase("Drakloak", "ClearBody");
		MapBase("Drampa", "Berserk");
		MapBase("Drapion", "BattleArmor");
		MapBase("Dratini", "ShedSkin");
		MapBase("Drednaw", "StrongJaw");
		MapBase("Dreepy", "ClearBody");
		MapBase("Drifblim", "Aftermath");
		MapBase("Drifloon", "Aftermath");
		MapBase("Drilbur", "SandRush");
		MapBase("Drizzile", "Torrent");
		MapBase("Drowzee", "Insomnia");
		MapBase("Druddigon", "RoughSkin");
		MapBase("Dubwool", "Fluffy");
		MapBase("Ducklett", "KeenEye");
		MapBase("Dudunsparce", "SereneGrace");
		MapBase("Dugtrio", "SandVeil");
		MapBase("Dunsparce", "SereneGrace");
		MapBase("Duosion", "Overcoat");
		MapBase("Duraludon", "LightMetal");
		MapBase("Durant", "Swarm");
		MapBase("Dusclops", "Pressure");
		MapBase("Dusknoir", "Pressure");
		MapBase("Duskull", "Levitate");
		MapBase("Dustox", "ShieldDust");
		MapBase("Dwebble", "Sturdy");
		MapBase("Eelektrik", "Levitate");
		MapBase("Eelektross", "Eelevate");
		MapBase("Eevee", "RunAway");
		MapBase("Eiscue", "IceFace");
		MapBase("Ekans", "Intimidate");
		MapBase("Eldegoss", "CottonDown");
		MapBase("Electabuzz", "Static");
		MapBase("Electivire", "MotorDrive");
		MapBase("Electrike", "Static");
		MapBase("Electrode", "Soundproof");
		MapBase("Elekid", "Static");
		MapBase("Elgyem", "Telepathy");
		MapBase("Emboar", "MoldBreaker");
		MapBase("Emolga", "Static");
		MapBase("Empoleon", "Torrent");
		MapBase("Enamorus", "Overcoat");
		MapBase("Entei", "Pressure");
		MapBase("Escavalier", "Swarm");
		MapBase("Espathra", "Opportunist");
		MapBase("Espeon", "Synchronize");
		MapBase("Espurr", "KeenEye");
		MapBase("Eternatus", "Pressure");
		MapBase("Excadrill", "PiercingDrill");
		MapBase("Exeggcute", "Chlorophyll");
		MapBase("Exeggutor", "Frisk");
		MapBase("Exploud", "Soundproof");
		MapBase("Falinks", "BattleArmor");
		MapBase("Farfetch'd", "Steadfast");
		MapBase("Farigiraf", "CudChew");
		MapBase("Fearow", "KeenEye");
		MapBase("Feebas", "SwiftSwim");
		MapBase("Fennekin", "Blaze");
		MapBase("Feraligatr", "Dragonize");
		MapBase("Ferroseed", "IronBarbs");
		MapBase("Ferrothorn", "IronBarbs");
		MapBase("Fezandipiti", "ToxicChain");
		MapBase("Fidough", "OwnTempo");
		MapBase("Finizen", "WaterVeil");
		MapBase("Finneon", "SwiftSwim");
		MapBase("Flaaffy", "Static");
		MapBase("Flamigo", "Scrappy");
		MapBase("Flapple", "Ripen");
		MapBase("Flareon", "FlashFire");
		MapBase("Fletchinder", "FlameBody");
		MapBase("Fletchling", "BigPecks");
		MapBase("Flittle", "Anticipation");
		MapBase("Floatzel", "SwiftSwim");
		MapBase("Floette", "FairyAura");
		MapBase("Floragato", "Overgrow");
		MapBase("Flutter Mane", "Protosynthesis");
		MapBase("Flygon", "Levitate");
		MapBase("Fomantis", "LeafGuard");
		MapBase("Foongus", "EffectSpore");
		MapBase("Forretress", "Sturdy");
		MapBase("Fraxure", "Rivalry");
		MapBase("Frigibax", "ThermalExchange");
		MapBase("Frillish", "WaterAbsorb");
		MapBase("Froakie", "Torrent");
		MapBase("Frogadier", "Torrent");
		MapBase("Froslass", "SnowWarning");
		MapBase("Frosmoth", "ShieldDust");
		MapBase("Fuecoco", "Blaze");
		MapBase("Furret", "RunAway");
		MapBase("Gabite", "SandVeil");
		MapBase("Gallade", "InnerFocus");
		MapBase("Galvantula", "CompoundEyes");
		MapBase("Garbodor", "Stench");
		MapBase("Garchomp", "Levitate");
		MapBase("Gardevoir", "Pixilate");
		MapBase("Garganacl", "PurifyingSalt");
		MapBase("Gastly", "Levitate");
		MapBase("Gastrodon", "StickyHold");
		MapBase("Geodude", "MagnetPull");
		MapBase("Gholdengo", "GoodAsGold");
		MapBase("Gible", "SandVeil");
		MapBase("Gigalith", "Sturdy");
		MapBase("Gimmighoul", "RunAway");
		MapBase("Girafarig", "InnerFocus");
		MapBase("Giratina", "Levitate");
		MapBase("Glaceon", "SnowCloak");
		MapBase("Glalie", "Refrigerate");
		MapBase("Glameow", "Limber");
		MapBase("Glastrier", "ChillingNeigh");
		MapBase("Gligar", "HyperCutter");
		MapBase("Glimmet", "ToxicDebris");
		MapBase("Glimmora", "Adaptability");
		MapBase("Gliscor", "HyperCutter");
		MapBase("Gloom", "Chlorophyll");
		MapBase("Gogoat", "SapSipper");
		MapBase("Golbat", "InnerFocus");
		MapBase("Goldeen", "SwiftSwim");
		MapBase("Golduck", "Damp");
		MapBase("Golem", "MagnetPull");
		MapBase("Golett", "IronFist");
		MapBase("Golisopod", "ToughClaws");
		MapBase("Golurk", "UnseenFist");
		MapBase("Goodra", "SapSipper");
		MapBase("Goomy", "SapSipper");
		MapBase("Gorebyss", "SwiftSwim");
		MapBase("Gossifleur", "CottonDown");
		MapBase("Gothita", "Frisk");
		MapBase("Gothitelle", "Frisk");
		MapBase("Gothorita", "Frisk");
		MapBase("Gouging Fire", "Protosynthesis");
		MapBase("Gourgeist", "Pickup");
		MapBase("Grafaiai", "Unburden");
		MapBase("Granbull", "Intimidate");
		MapBase("Grapploct", "Limber");
		MapBase("Graveler", "MagnetPull");
		MapBase("Great Tusk", "Protosynthesis");
		MapBase("Greavard", "Pickup");
		MapBase("Greedent", "CheekPouch");
		MapBase("Greninja", "Protean");
		MapBase("Grimer", "PoisonTouch");
		MapBase("Grimmsnarl", "Prankster");
		MapBase("Grookey", "Overgrow");
		MapBase("Grotle", "Overgrow");
		MapBase("Groudon", "DesolateLand");
		MapBase("Grovyle", "Overgrow");
		MapBase("Growlithe", "Intimidate");
		MapBase("Grubbin", "Swarm");
		MapBase("Grumpig", "ThickFat");
		MapBase("Gulpin", "LiquidOoze");
		MapBase("Gumshoos", "Adaptability");
		MapBase("Gurdurr", "Guts");
		MapBase("Guzzlord", "BeastBoost");
		MapBase("Gyarados", "MoldBreaker");
		MapBase("Hakamo-o", "Bulletproof");
		MapBase("Happiny", "NaturalCure");
		MapBase("Hariyama", "ThickFat");
		MapBase("Hatenna", "Healer");
		MapBase("Hatterene", "Healer");
		MapBase("Hattrem", "Healer");
		MapBase("Haunter", "Levitate");
		MapBase("Hawlucha", "NoGuard");
		MapBase("Haxorus", "Rivalry");
		MapBase("Heatmor", "Gluttony");
		MapBase("Heatran", "FlashFire");
		MapBase("Heliolisk", "DrySkin");
		MapBase("Helioptile", "DrySkin");
		MapBase("Heracross", "SkillLink");
		MapBase("Herdier", "Intimidate");
		MapBase("Hippopotas", "SandStream");
		MapBase("Hippowdon", "SandStream");
		MapBase("Hitmonchan", "KeenEye");
		MapBase("Hitmonlee", "Limber");
		MapBase("Hitmontop", "Intimidate");
		MapBase("Ho-Oh", "Pressure");
		MapBase("Honchkrow", "Insomnia");
		MapBase("Honedge", "NoGuard");
		MapBase("Hoopa", "Magician");
		MapBase("Hoothoot", "Insomnia");
		MapBase("Hoppip", "Chlorophyll");
		MapBase("Horsea", "SwiftSwim");
		MapBase("Houndoom", "SolarPower");
		MapBase("Houndour", "EarlyBird");
		MapBase("Houndstone", "SandRush");
		MapBase("Huntail", "SwiftSwim");
		MapBase("Hydrapple", "SupersweetSyrup");
		MapBase("Hydreigon", "Levitate");
		MapBase("Hypno", "Insomnia");
		MapBase("Igglybuff", "CuteCharm");
		MapBase("Illumise", "Oblivious");
		MapBase("Impidimp", "Prankster");
		MapBase("Incineroar", "Blaze");
		MapBase("Indeedee", "OwnTempo");
		MapBase("Infernape", "Blaze");
		MapBase("Inkay", "Contrary");
		MapBase("Inteleon", "Torrent");
		MapBase("Iron Boulder", "QuarkDrive");
		MapBase("Iron Bundle", "QuarkDrive");
		MapBase("Iron Crown", "QuarkDrive");
		MapBase("Iron Hands", "QuarkDrive");
		MapBase("Iron Jugulis", "QuarkDrive");
		MapBase("Iron Leaves", "QuarkDrive");
		MapBase("Iron Moth", "QuarkDrive");
		MapBase("Iron Thorns", "QuarkDrive");
		MapBase("Iron Treads", "QuarkDrive");
		MapBase("Iron Valiant", "QuarkDrive");
		MapBase("Ivysaur", "Overgrow");
		MapBase("Jangmo-o", "Bulletproof");
		MapBase("Jellicent", "WaterAbsorb");
		MapBase("Jigglypuff", "CuteCharm");
		MapBase("Jirachi", "SereneGrace");
		MapBase("Jolteon", "VoltAbsorb");
		MapBase("Joltik", "CompoundEyes");
		MapBase("Jumpluff", "Chlorophyll");
		MapBase("Jynx", "Oblivious");
		MapBase("Kabuto", "SwiftSwim");
		MapBase("Kabutops", "SwiftSwim");
		MapBase("Kadabra", "Synchronize");
		MapBase("Kakuna", "ShedSkin");
		MapBase("Kangaskhan", "ParentalBond");
		MapBase("Karrablast", "Swarm");
		MapBase("Kartana", "BeastBoost");
		MapBase("Kecleon", "ColorChange");
		MapBase("Keldeo", "Justified");
		MapBase("Kilowattrel", "WindPower");
		MapBase("Kingambit", "Defiant");
		MapBase("Kingdra", "SwiftSwim");
		MapBase("Kingler", "HyperCutter");
		MapBase("Kirlia", "Synchronize");
		MapBase("Klang", "Plus");
		MapBase("Klawf", "AngerShell");
		MapBase("Kleavor", "Swarm");
		MapBase("Klefki", "Prankster");
		MapBase("Klink", "Plus");
		MapBase("Klinklang", "Plus");
		MapBase("Koffing", "Levitate");
		MapBase("Komala", "Comatose");
		MapBase("Kommo-o", "Overcoat");
		MapBase("Koraidon", "OrichalcumPulse");
		MapBase("Krabby", "HyperCutter");
		MapBase("Kricketot", "ShedSkin");
		MapBase("Kricketune", "Swarm");
		MapBase("Krokorok", "Intimidate");
		MapBase("Krookodile", "Intimidate");
		MapBase("Kubfu", "InnerFocus");
		MapBase("Kyogre", "PrimordialSea");
		MapBase("Kyurem", "Teravolt");
		MapBase("Lairon", "Sturdy");
		MapBase("Lampent", "FlashFire");
		MapBase("Landorus", "Intimidate");
		MapBase("Lanturn", "VoltAbsorb");
		MapBase("Lapras", "WaterAbsorb");
		MapBase("Larvesta", "FlameBody");
		MapBase("Larvitar", "Guts");
		MapBase("Latias", "Levitate");
		MapBase("Latios", "Levitate");
		MapBase("Leafeon", "LeafGuard");
		MapBase("Leavanny", "Swarm");
		MapBase("Lechonk", "AromaVeil");
		MapBase("Ledian", "Swarm");
		MapBase("Ledyba", "Swarm");
		MapBase("Lickilicky", "OwnTempo");
		MapBase("Lickitung", "OwnTempo");
		MapBase("Liepard", "Limber");
		MapBase("Lileep", "SuctionCups");
		MapBase("Lilligant", "Chlorophyll");
		MapBase("Lillipup", "VitalSpirit");
		MapBase("Linoone", "Pickup");
		MapBase("Litleo", "Rivalry");
		MapBase("Litten", "Blaze");
		MapBase("Litwick", "FlashFire");
		MapBase("Lokix", "Swarm");
		MapBase("Lombre", "SwiftSwim");
		MapBase("Lopunny", "Scrappy");
		MapBase("Lotad", "SwiftSwim");
		MapBase("Loudred", "Soundproof");
		MapBase("Lucario", "AuraGuard");
		MapBase("Ludicolo", "SwiftSwim");
		MapBase("Lugia", "Pressure");
		MapBase("Lumineon", "SwiftSwim");
		MapBase("Lunala", "ShadowShield");
		MapBase("Lunatone", "Levitate");
		MapBase("Lurantis", "LeafGuard");
		MapBase("Luvdisc", "SwiftSwim");
		MapBase("Luxio", "Rivalry");
		MapBase("Luxray", "Rivalry");
		MapBase("Lycanroc", "ToughClaws");
		MapBase("Mabosstiff", "Intimidate");
		MapBase("Machamp", "Guts");
		MapBase("Machoke", "Guts");
		MapBase("Machop", "Guts");
		MapBase("Magby", "FlameBody");
		MapBase("Magcargo", "MagmaArmor");
		MapBase("Magearna", "SoulHeart");
		MapBase("Magikarp", "SwiftSwim");
		MapBase("Magmar", "FlameBody");
		MapBase("Magmortar", "FlameBody");
		MapBase("Magnemite", "MagnetPull");
		MapBase("Magneton", "MagnetPull");
		MapBase("Magnezone", "MagnetPull");
		MapBase("Makuhita", "ThickFat");
		MapBase("Malamar", "Contrary");
		MapBase("Mamoswine", "Oblivious");
		MapBase("Manaphy", "Hydration");
		MapBase("Mandibuzz", "BigPecks");
		MapBase("Manectric", "Intimidate");
		MapBase("Mankey", "VitalSpirit");
		MapBase("Mantine", "SwiftSwim");
		MapBase("Mantyke", "SwiftSwim");
		MapBase("Maractus", "WaterAbsorb");
		MapBase("Mareanie", "Merciless");
		MapBase("Mareep", "Static");
		MapBase("Marill", "ThickFat");
		MapBase("Marowak", "RockHead");
		MapBase("Marshadow", "Technician");
		MapBase("Marshtomp", "Torrent");
		MapBase("Maschiff", "Intimidate");
		MapBase("Masquerain", "Intimidate");
		MapBase("Maushold", "FriendGuard");
		MapBase("Mawile", "HugePower");
		MapBase("Medicham", "PurePower");
		MapBase("Meditite", "PurePower");
		MapBase("Meganium", "MegaSol");
		MapBase("Melmetal", "IronFist");
		MapBase("Meloetta", "SereneGrace");
		MapBase("Meltan", "MagnetPull");
		MapBase("Meowscarada", "Overgrow");
		MapBase("Meowstic", "Trace");
		MapBase("Mesprit", "Levitate");
		MapBase("Metagross", "ToughClaws");
		MapBase("Metang", "ClearBody");
		MapBase("Metapod", "ShedSkin");
		MapBase("Mew", "Synchronize");
		MapBase("Mewtwo", "Insomnia");
		MapBase("Mienfoo", "InnerFocus");
		MapBase("Mienshao", "InnerFocus");
		MapBase("Mightyena", "Intimidate");
		MapBase("Milcery", "SweetVeil");
		MapBase("Milotic", "MarvelScale");
		MapBase("Miltank", "ThickFat");
		MapBase("Mime Jr.", "Soundproof");
		MapBase("Mimikyu", "Disguise");
		MapBase("Minccino", "CuteCharm");
		MapBase("Minun", "Minus");
		MapBase("Miraidon", "HadronEngine");
		MapBase("Misdreavus", "Levitate");
		MapBase("Mismagius", "Levitate");
		MapBase("Moltres", "Berserk");
		MapBase("Monferno", "Blaze");
		MapBase("Morelull", "Illuminate");
		MapBase("Morgrem", "Prankster");
		MapBase("Morpeko", "HungerSwitch");
		MapBase("Mr. Mime", "VitalSpirit");
		MapBase("Mr. Rime", "TangledFeet");
		MapBase("Mudbray", "OwnTempo");
		MapBase("Mudkip", "Torrent");
		MapBase("Mudsdale", "OwnTempo");
		MapBase("Muk", "PoisonTouch");
		MapBase("Munchlax", "Pickup");
		MapBase("Munkidori", "ToxicChain");
		MapBase("Munna", "Forewarn");
		MapBase("Murkrow", "Insomnia");
		MapBase("Musharna", "Forewarn");
		MapBase("Nacli", "PurifyingSalt");
		MapBase("Naclstack", "PurifyingSalt");
		MapBase("Naganadel", "BeastBoost");
		MapBase("Natu", "Synchronize");
		MapBase("Necrozma", "Neuroforce");
		MapBase("Nickit", "RunAway");
		MapBase("Nidoking", "PoisonPoint");
		MapBase("Nidoqueen", "PoisonPoint");
		MapBase("Nidoran♀", "PoisonPoint");
		MapBase("Nidoran♂", "PoisonPoint");
		MapBase("Nidorina", "PoisonPoint");
		MapBase("Nidorino", "PoisonPoint");
		MapBase("Nihilego", "BeastBoost");
		MapBase("Nincada", "CompoundEyes");
		MapBase("Ninetales", "SnowCloak");
		MapBase("Ninjask", "SpeedBoost");
		MapBase("Noctowl", "Insomnia");
		MapBase("Noibat", "Frisk");
		MapBase("Noivern", "Frisk");
		MapBase("Nosepass", "Sturdy");
		MapBase("Numel", "Oblivious");
		MapBase("Nuzleaf", "Chlorophyll");
		MapBase("Nymble", "Swarm");
		MapBase("Obstagoon", "Reckless");
		MapBase("Octillery", "SuctionCups");
		MapBase("Oddish", "Chlorophyll");
		MapBase("Oinkologne", "AromaVeil");
		MapBase("Okidogi", "ToxicChain");
		MapBase("Omanyte", "SwiftSwim");
		MapBase("Omastar", "SwiftSwim");
		MapBase("Onix", "RockHead");
		MapBase("Oranguru", "InnerFocus");
		MapBase("Orbeetle", "Swarm");
		MapBase("Oricorio", "Dancer");
		MapBase("Orthworm", "EarthEater");
		MapBase("Oshawott", "Torrent");
		MapBase("Overqwil", "PoisonPoint");
		MapBase("Pachirisu", "RunAway");
		MapBase("Palafin", "ZeroToHero");
		MapBase("Palkia", "Pressure");
		MapBase("Palossand", "WaterCompaction");
		MapBase("Palpitoad", "SwiftSwim");
		MapBase("Pancham", "IronFist");
		MapBase("Pangoro", "IronFist");
		MapBase("Panpour", "Gluttony");
		MapBase("Pansage", "Gluttony");
		MapBase("Pansear", "Gluttony");
		MapBase("Paras", "EffectSpore");
		MapBase("Parasect", "EffectSpore");
		MapBase("Passimian", "Receiver");
		MapBase("Patrat", "RunAway");
		MapBase("Pawmi", "Static");
		MapBase("Pawmo", "VoltAbsorb");
		MapBase("Pawmot", "VoltAbsorb");
		MapBase("Pawniard", "Defiant");
		MapBase("Pecharunt", "PoisonPuppeteer");
		MapBase("Pelipper", "KeenEye");
		MapBase("Perrserker", "BattleArmor");
		MapBase("Persian", "FurCoat");
		MapBase("Petilil", "Chlorophyll");
		MapBase("Phanpy", "Pickup");
		MapBase("Phantump", "NaturalCure");
		MapBase("Pheromosa", "BeastBoost");
		MapBase("Phione", "Hydration");
		MapBase("Pichu", "Static");
		MapBase("Pidgeot", "NoGuard");
		MapBase("Pidgeotto", "KeenEye");
		MapBase("Pidgey", "KeenEye");
		MapBase("Pidove", "BigPecks");
		MapBase("Pignite", "Blaze");
		MapBase("Pikachu", "Static");
		MapBase("Pikipek", "KeenEye");
		MapBase("Piloswine", "Oblivious");
		MapBase("Pincurchin", "LightningRod");
		MapBase("Pineco", "Sturdy");
		MapBase("Pinsir", "Aerilate");
		MapBase("Piplup", "Torrent");
		MapBase("Plusle", "Plus");
		MapBase("Poipole", "BeastBoost");
		MapBase("Politoed", "WaterAbsorb");
		MapBase("Poliwag", "WaterAbsorb");
		MapBase("Poliwhirl", "WaterAbsorb");
		MapBase("Poliwrath", "WaterAbsorb");
		MapBase("Poltchageist", "Hospitality");
		MapBase("Polteageist", "WeakArmor");
		MapBase("Ponyta", "RunAway");
		MapBase("Poochyena", "RunAway");
		MapBase("Popplio", "Torrent");
		MapBase("Porygon", "Trace");
		MapBase("Porygon-Z", "Adaptability");
		MapBase("Porygon2", "Trace");
		MapBase("Primarina", "Torrent");
		MapBase("Primeape", "VitalSpirit");
		MapBase("Prinplup", "Torrent");
		MapBase("Probopass", "Sturdy");
		MapBase("Psyduck", "Damp");
		MapBase("Pumpkaboo", "Pickup");
		MapBase("Pupitar", "ShedSkin");
		MapBase("Purrloin", "Limber");
		MapBase("Purugly", "ThickFat");
		MapBase("Pyroar", "FireMane");
		MapBase("Pyukumuku", "InnardsOut");
		MapBase("Quagsire", "Damp");
		MapBase("Quaquaval", "Torrent");
		MapBase("Quaxly", "Torrent");
		MapBase("Quaxwell", "Torrent");
		MapBase("Quilava", "Blaze");
		MapBase("Quilladin", "Overgrow");
		MapBase("Qwilfish", "PoisonPoint");
		MapBase("Raboot", "Blaze");
		MapBase("Rabsca", "Synchronize");
		MapBase("Raging Bolt", "Protosynthesis");
		MapBase("Raichu", "NoGuard");
		MapBase("Raikou", "Pressure");
		MapBase("Ralts", "Synchronize");
		MapBase("Rampardos", "MoldBreaker");
		MapBase("Rapidash", "RunAway");
		MapBase("Raticate", "ThickFat");
		MapBase("Rattata", "Gluttony");
		MapBase("Rayquaza", "DeltaStream");
		MapBase("Regice", "ClearBody");
		MapBase("Regidrago", "DragonsMaw");
		MapBase("Regieleki", "Transistor");
		MapBase("Regigigas", "SlowStart");
		MapBase("Regirock", "ClearBody");
		MapBase("Registeel", "ClearBody");
		MapBase("Relicanth", "SwiftSwim");
		MapBase("Rellor", "CompoundEyes");
		MapBase("Remoraid", "Hustle");
		MapBase("Reshiram", "Turboblaze");
		MapBase("Reuniclus", "Overcoat");
		MapBase("Revavroom", "Overcoat");
		MapBase("Rhydon", "LightningRod");
		MapBase("Rhyhorn", "LightningRod");
		MapBase("Rhyperior", "LightningRod");
		MapBase("Ribombee", "SweetVeil");
		MapBase("Rillaboom", "Overgrow");
		MapBase("Riolu", "Steadfast");
		MapBase("Roaring Moon", "Protosynthesis");
		MapBase("Rockruff", "OwnTempo");
		MapBase("Roggenrola", "Sturdy");
		MapBase("Rolycoly", "SteamEngine");
		MapBase("Rookidee", "KeenEye");
		MapBase("Roselia", "NaturalCure");
		MapBase("Roserade", "NaturalCure");
		MapBase("Rotom", "Levitate");
		MapBase("Rowlet", "Overgrow");
		MapBase("Rufflet", "KeenEye");
		MapBase("Runerigus", "WanderingSpirit");
		MapBase("Sableye", "MagicBounce");
		MapBase("Salamence", "Aerilate");
		MapBase("Salandit", "Corrosion");
		MapBase("Salazzle", "Corrosion");
		MapBase("Samurott", "Torrent");
		MapBase("Sandaconda", "SandSpit");
		MapBase("Sandile", "Intimidate");
		MapBase("Sandshrew", "SnowCloak");
		MapBase("Sandslash", "SnowCloak");
		MapBase("Sandy Shocks", "Protosynthesis");
		MapBase("Sandygast", "WaterCompaction");
		MapBase("Sawk", "Sturdy");
		MapBase("Sawsbuck", "Chlorophyll");
		MapBase("Sceptile", "LightningRod");
		MapBase("Scizor", "Technician");
		MapBase("Scolipede", "ShellArmor");
		MapBase("Scorbunny", "Blaze");
		MapBase("Scovillain", "SpicySpray");
		MapBase("Scrafty", "Intimidate");
		MapBase("Scraggy", "ShedSkin");
		MapBase("Scream Tail", "Protosynthesis");
		MapBase("Scyther", "Swarm");
		MapBase("Seadra", "PoisonPoint");
		MapBase("Seaking", "SwiftSwim");
		MapBase("Sealeo", "ThickFat");
		MapBase("Seedot", "Chlorophyll");
		MapBase("Seel", "ThickFat");
		MapBase("Seismitoad", "SwiftSwim");
		MapBase("Sentret", "RunAway");
		MapBase("Serperior", "Overgrow");
		MapBase("Servine", "Overgrow");
		MapBase("Seviper", "ShedSkin");
		MapBase("Sewaddle", "Swarm");
		MapBase("Sharpedo", "StrongJaw");
		MapBase("Shaymin", "SereneGrace");
		MapBase("Shedinja", "WonderGuard");
		MapBase("Shelgon", "RockHead");
		MapBase("Shellder", "ShellArmor");
		MapBase("Shellos", "StickyHold");
		MapBase("Shelmet", "Hydration");
		MapBase("Shieldon", "Sturdy");
		MapBase("Shiftry", "Chlorophyll");
		MapBase("Shiinotic", "Illuminate");
		MapBase("Shinx", "Rivalry");
		MapBase("Shroodle", "Unburden");
		MapBase("Shroomish", "EffectSpore");
		MapBase("Shuckle", "Sturdy");
		MapBase("Shuppet", "Insomnia");
		MapBase("Sigilyph", "WonderSkin");
		MapBase("Silcoon", "ShedSkin");
		MapBase("Silicobra", "SandSpit");
		MapBase("Simipour", "Gluttony");
		MapBase("Simisage", "Gluttony");
		MapBase("Simisear", "Gluttony");
		MapBase("Sinistcha", "Hospitality");
		MapBase("Sinistea", "WeakArmor");
		MapBase("Sirfetch'd", "Steadfast");
		MapBase("Sizzlipede", "FlashFire");
		MapBase("Skarmory", "Stalwart");
		MapBase("Skeledirge", "Blaze");
		MapBase("Skiddo", "SapSipper");
		MapBase("Skiploom", "Chlorophyll");
		MapBase("Skitty", "CuteCharm");
		MapBase("Skorupi", "BattleArmor");
		MapBase("Skrelp", "PoisonPoint");
		MapBase("Skuntank", "Stench");
		MapBase("Skwovet", "CheekPouch");
		MapBase("Slaking", "Truant");
		MapBase("Slakoth", "Truant");
		MapBase("Sliggoo", "SapSipper");
		MapBase("Slither Wing", "Protosynthesis");
		MapBase("Slowbro", "QuickDraw");
		MapBase("Slowking", "CuriousMedicine");
		MapBase("Slowpoke", "Gluttony");
		MapBase("Slugma", "MagmaArmor");
		MapBase("Slurpuff", "SweetVeil");
		MapBase("Smeargle", "OwnTempo");
		MapBase("Smoliv", "EarlyBird");
		MapBase("Smoochum", "Oblivious");
		MapBase("Sneasel", "InnerFocus");
		MapBase("Sneasler", "Pressure");
		MapBase("Snivy", "Overgrow");
		MapBase("Snom", "ShieldDust");
		MapBase("Snorlax", "Immunity");
		MapBase("Snorunt", "InnerFocus");
		MapBase("Snover", "SnowWarning");
		MapBase("Snubbull", "Intimidate");
		MapBase("Sobble", "Torrent");
		MapBase("Solgaleo", "FullMetalBody");
		MapBase("Solosis", "Overcoat");
		MapBase("Solrock", "Levitate");
		MapBase("Spearow", "KeenEye");
		MapBase("Spectrier", "GrimNeigh");
		MapBase("Spheal", "ThickFat");
		MapBase("Spidops", "Insomnia");
		MapBase("Spinarak", "Swarm");
		MapBase("Spinda", "OwnTempo");
		MapBase("Spiritomb", "Pressure");
		MapBase("Spoink", "ThickFat");
		MapBase("Sprigatito", "Overgrow");
		MapBase("Spritzee", "Healer");
		MapBase("Squawkabilly", "Intimidate");
		MapBase("Squirtle", "Torrent");
		MapBase("Stakataka", "BeastBoost");
		MapBase("Stantler", "Intimidate");
		MapBase("Staraptor", "Contrary");
		MapBase("Staravia", "Intimidate");
		MapBase("Starly", "KeenEye");
		MapBase("Starmie", "HugePower");
		MapBase("Staryu", "Illuminate");
		MapBase("Steelix", "SandForce");
		MapBase("Steenee", "LeafGuard");
		MapBase("Stonjourner", "PowerSpot");
		MapBase("Stoutland", "Intimidate");
		MapBase("Stufful", "Fluffy");
		MapBase("Stunfisk", "Mimicry");
		MapBase("Stunky", "Stench");
		MapBase("Sudowoodo", "Sturdy");
		MapBase("Suicune", "Pressure");
		MapBase("Sunflora", "Chlorophyll");
		MapBase("Sunkern", "Chlorophyll");
		MapBase("Surskit", "SwiftSwim");
		MapBase("Swablu", "NaturalCure");
		MapBase("Swadloon", "LeafGuard");
		MapBase("Swalot", "LiquidOoze");
		MapBase("Swampert", "SwiftSwim");
		MapBase("Swanna", "KeenEye");
		MapBase("Swellow", "Guts");
		MapBase("Swinub", "Oblivious");
		MapBase("Swirlix", "SweetVeil");
		MapBase("Swoobat", "Unaware");
		MapBase("Sylveon", "CuteCharm");
		MapBase("Tadbulb", "OwnTempo");
		MapBase("Taillow", "Guts");
		MapBase("Talonflame", "FlameBody");
		MapBase("Tandemaus", "RunAway");
		MapBase("Tangela", "Chlorophyll");
		MapBase("Tangrowth", "Chlorophyll");
		MapBase("Tapu Bulu", "GrassySurge");
		MapBase("Tapu Fini", "MistySurge");
		MapBase("Tapu Koko", "ElectricSurge");
		MapBase("Tapu Lele", "PsychicSurge");
		MapBase("Tarountula", "Insomnia");
		MapBase("Tatsugiri", "StormDrain");
		MapBase("Tauros", "Intimidate");
		MapBase("Teddiursa", "Pickup");
		MapBase("Tentacool", "ClearBody");
		MapBase("Tentacruel", "ClearBody");
		MapBase("Tepig", "Blaze");
		MapBase("Terapagos", "TeraformZero");
		MapBase("Terrakion", "Justified");
		MapBase("Thievul", "RunAway");
		MapBase("Throh", "Guts");
		MapBase("Thundurus", "VoltAbsorb");
		MapBase("Thwackey", "Overgrow");
		MapBase("Timburr", "Guts");
		MapBase("Ting-Lu", "VesselOfRuin");
		MapBase("Tinkatink", "MoldBreaker");
		MapBase("Tinkaton", "MoldBreaker");
		MapBase("Tinkatuff", "MoldBreaker");
		MapBase("Tirtouga", "SolidRock");
		MapBase("Toedscool", "MyceliumMight");
		MapBase("Toedscruel", "MyceliumMight");
		MapBase("Togedemaru", "Sturdy");
		MapBase("Togekiss", "Hustle");
		MapBase("Togepi", "Hustle");
		MapBase("Togetic", "Hustle");
		MapBase("Torchic", "Blaze");
		MapBase("Torkoal", "WhiteSmoke");
		MapBase("Tornadus", "Regenerator");
		MapBase("Torracat", "Blaze");
		MapBase("Torterra", "Overgrow");
		MapBase("Totodile", "Torrent");
		MapBase("Toucannon", "KeenEye");
		MapBase("Toxapex", "Merciless");
		MapBase("Toxel", "Rattled");
		MapBase("Toxicroak", "Anticipation");
		MapBase("Toxtricity", "PunkRock");
		MapBase("Tranquill", "BigPecks");
		MapBase("Trapinch", "HyperCutter");
		MapBase("Treecko", "Overgrow");
		MapBase("Trevenant", "NaturalCure");
		MapBase("Tropius", "Chlorophyll");
		MapBase("Trubbish", "Stench");
		MapBase("Trumbeak", "KeenEye");
		MapBase("Tsareena", "LeafGuard");
		MapBase("Turtonator", "ShellArmor");
		MapBase("Turtwig", "Overgrow");
		MapBase("Tympole", "SwiftSwim");
		MapBase("Tynamo", "Levitate");
		MapBase("Type: Null", "BattleArmor");
		MapBase("Typhlosion", "Blaze");
		MapBase("Tyranitar", "SandStream");
		MapBase("Tyrantrum", "StrongJaw");
		MapBase("Tyrogue", "Guts");
		MapBase("Tyrunt", "StrongJaw");
		MapBase("Umbreon", "Synchronize");
		MapBase("Unfezant", "BigPecks");
		MapBase("Ursaluna", "MindsEye");
		MapBase("Ursaring", "Guts");
		MapBase("Urshifu", "UnseenFist");
		MapBase("Uxie", "Levitate");
		MapBase("Vanillish", "IceBody");
		MapBase("Vanillite", "IceBody");
		MapBase("Vanilluxe", "IceBody");
		MapBase("Vaporeon", "WaterAbsorb");
		MapBase("Varoom", "Overcoat");
		MapBase("Veluza", "MoldBreaker");
		MapBase("Venipede", "PoisonPoint");
		MapBase("Venomoth", "ShieldDust");
		MapBase("Venonat", "CompoundEyes");
		MapBase("Venusaur", "Overgrow");
		MapBase("Vespiquen", "Pressure");
		MapBase("Vibrava", "Levitate");
		MapBase("Victini", "VictoryStar");
		MapBase("Victreebel", "InnardsOut");
		MapBase("Vigoroth", "VitalSpirit");
		MapBase("Vikavolt", "Levitate");
		MapBase("Vileplume", "Chlorophyll");
		MapBase("Virizion", "Justified");
		MapBase("Volbeat", "Illuminate");
		MapBase("Volcanion", "WaterAbsorb");
		MapBase("Volcarona", "FlameBody");
		MapBase("Voltorb", "Soundproof");
		MapBase("Vullaby", "BigPecks");
		MapBase("Vulpix", "SnowCloak");
		MapBase("Wailmer", "WaterVeil");
		MapBase("Wailord", "WaterVeil");
		MapBase("Walking Wake", "Protosynthesis");
		MapBase("Walrein", "ThickFat");
		MapBase("Wartortle", "Torrent");
		MapBase("Watchog", "Illuminate");
		MapBase("Wattrel", "WindPower");
		MapBase("Weavile", "Pressure");
		MapBase("Weedle", "ShieldDust");
		MapBase("Weepinbell", "Chlorophyll");
		MapBase("Weezing", "Levitate");
		MapBase("Whimsicott", "Prankster");
		MapBase("Whirlipede", "PoisonPoint");
		MapBase("Whiscash", "Oblivious");
		MapBase("Whismur", "Soundproof");
		MapBase("Wigglytuff", "CuteCharm");
		MapBase("Wiglett", "Gooey");
		MapBase("Wimpod", "WimpOut");
		MapBase("Wingull", "KeenEye");
		MapBase("Wishiwashi", "Schooling");
		MapBase("Wo-Chien", "TabletsOfRuin");
		MapBase("Wobbuffet", "ShadowTag");
		MapBase("Woobat", "Unaware");
		MapBase("Wooloo", "Fluffy");
		MapBase("Wooper", "PoisonPoint");
		MapBase("Wormadam", "Anticipation");
		MapBase("Wugtrio", "Gooey");
		MapBase("Wurmple", "ShieldDust");
		MapBase("Wynaut", "ShadowTag");
		MapBase("Wyrdeer", "Intimidate");
		MapBase("Xatu", "Synchronize");
		MapBase("Xerneas", "FairyAura");
		MapBase("Xurkitree", "BeastBoost");
		MapBase("Yamask", "WanderingSpirit");
		MapBase("Yamper", "BallFetch");
		MapBase("Yanma", "SpeedBoost");
		MapBase("Yanmega", "SpeedBoost");
		MapBase("Yungoos", "Stakeout");
		MapBase("Yveltal", "DarkAura");
		MapBase("Zacian", "IntrepidSword");
		MapBase("Zamazenta", "DauntlessShield");
		MapBase("Zangoose", "Immunity");
		MapBase("Zapdos", "Defiant");
		MapBase("Zarude", "LeafGuard");
		MapBase("Zebstrika", "LightningRod");
		MapBase("Zekrom", "Teravolt");
		MapBase("Zeraora", "VoltAbsorb");
		MapBase("Zigzagoon", "Pickup");
		MapBase("Zoroark", "Illusion");
		MapBase("Zorua", "Illusion");
		MapBase("Zubat", "InnerFocus");
		MapBase("Zweilous", "Hustle");
		MapBase("Zygarde", "AuraBreak");

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

	private static readonly Dictionary<string, string> _speciesHiddenAbilities = new(StringComparer.OrdinalIgnoreCase)
	{
		["Venusaur"] = "Chlorophyll",
		["Ivysaur"] = "Chlorophyll",
		["Bulbasaur"] = "Chlorophyll",
		["Charizard"] = "Solar Power",
		["Charmeleon"] = "Solar Power",
		["Charmander"] = "Solar Power",
		["Blastoise"] = "Rain Dish",
		["Wartortle"] = "Rain Dish",
		["Squirtle"] = "Rain Dish",
		["Dragonite"] = "Multiscale",
		["Dragonair"] = "Marvel Scale",
		["Dratini"] = "Marvel Scale",
		["Alakazam"] = "Magic Guard",
		["Kadabra"] = "Magic Guard",
		["Abra"] = "Magic Guard",
		["Gengar"] = "Cursed Body",
		["Gyarados"] = "Moxie",
		["Magikarp"] = "Rattled",
		["Nidoking"] = "Sheer Force",
		["Nidoqueen"] = "Sheer Force",
		["Ninetales"] = "Drought",
		["Vulpix"] = "Drought",
		["Politoed"] = "Drizzle",
		["Poliwrath"] = "Swift Swim",
		["Espeon"] = "Magic Bounce",
		["Umbreon"] = "Inner Focus",
		["Sylveon"] = "Pixilate",
		["Eevee"] = "Anticipation",
		["Vaporeon"] = "Hydration",
		["Jolteon"] = "Quick Feet",
		["Flareon"] = "Guts",
		["Leafeon"] = "Chlorophyll",
		["Glaceon"] = "Ice Body",
		["Snorlax"] = "Gluttony",
		["Tyranitar"] = "Unnerve",
		["Blaziken"] = "Speed Boost",
		["Combusken"] = "Speed Boost",
		["Torchic"] = "Speed Boost",
		["Swampert"] = "Damp",
		["Sceptile"] = "Unburden",
		["Breloom"] = "Technician",
		["Shroomish"] = "Quick Feet",
		["Salamence"] = "Moxie",
		["Bagon"] = "Sheer Force",
		["Shelgon"] = "Overcoat",
		["Metagross"] = "Light Metal",
		["Garchomp"] = "Rough Skin",
		["Gabite"] = "Rough Skin",
		["Gible"] = "Rough Skin",
		["Lucario"] = "Justified",
		["Riolu"] = "Prankster",
		["Togekiss"] = "Super Luck",
		["Weavile"] = "Pickpocket",
		["Gliscor"] = "Poison Heal",
		["Mamoswine"] = "Thick Fat",
		["Serperior"] = "Contrary",
		["Servine"] = "Contrary",
		["Snivy"] = "Contrary",
		["Greninja"] = "Protean",
		["Frogadier"] = "Protean",
		["Froakie"] = "Protean",
		["Talonflame"] = "Gale Wings",
		["Incineroar"] = "Intimidate",
		["Torracat"] = "Intimidate",
		["Litten"] = "Intimidate",
		["Toxapex"] = "Regenerator",
		["Mareanie"] = "Regenerator",
		["Corviknight"] = "Mirror Armor",
		["Cinderace"] = "Libero",
		["Rillaboom"] = "Grassy Surge",
		["Inteleon"] = "Sniper",
		["Skeledirge"] = "Unaware",
		["Meowscarada"] = "Protean",
		["Quaquaval"] = "Moxie",
		["Espathra"] = "Speed Boost"
	};

	public static string GetHiddenAbility(PokemonSpecies species)
	{
		if (species == null) return null;
		if (_speciesHiddenAbilities.TryGetValue(species.Name, out var ha)) return ha;
		if (!string.IsNullOrEmpty(species.InternalName) && _speciesHiddenAbilities.TryGetValue(NormalizeKey(species.InternalName), out var haNorm)) return haNorm;
		if (!string.IsNullOrEmpty(species.BaseSpeciesName) && _speciesHiddenAbilities.TryGetValue(species.BaseSpeciesName, out var parentHa)) return parentHa;

		return species.PrimaryType switch
		{
			PokemonType.Fire => "Flash Fire",
			PokemonType.Water => "Swift Swim",
			PokemonType.Grass => "Chlorophyll",
			PokemonType.Electric => "Motor Drive",
			PokemonType.Fighting => "Iron Fist",
			PokemonType.Dragon => "Multiscale",
			PokemonType.Dark => "Prankster",
			PokemonType.Ghost => "Cursed Body",
			PokemonType.Steel => "Heavy Metal",
			PokemonType.Fairy => "Pixilate",
			PokemonType.Psychic => "Magic Bounce",
			PokemonType.Normal => "Scrappy",
			_ => "Inner Focus"
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
