using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.GameData.Crops;
using StardewValley.GameData.Objects;
using StardewValley.Internal;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

public static class PokemonItemManager
{
	public static readonly string[] ApricornColors = new[]
	{
		"Red", "Blue", "Yellow", "Green", "Black", "White", "Pink"
	};

	public static readonly Dictionary<string, string> PortugueseColorNames = new()
	{
		["Red"] = "Vermelho",
		["Blue"] = "Azul",
		["Yellow"] = "Amarelo",
		["Green"] = "Verde",
		["Black"] = "Preto",
		["White"] = "Branco",
		["Pink"] = "Rosa"
	};

	// Springobjects sprite indices fallback
	private static readonly Dictionary<string, int> RawApricornSprites = new()
	{
		["Red"] = 258,
		["Blue"] = 398,
		["Yellow"] = 634,
		["Green"] = 400,
		["Black"] = 418,
		["White"] = 421,
		["Pink"] = 636
	};

	private static readonly Dictionary<string, int> SeedSprites = new()
	{
		["Red"] = 482,
		["Blue"] = 481,
		["Yellow"] = 487,
		["Green"] = 477,
		["Black"] = 488,
		["White"] = 474,
		["Pink"] = 479
	};

	private static readonly Dictionary<string, int> CropGrowthRows = new()
	{
		["Red"] = 10,
		["Blue"] = 27,
		["Yellow"] = 15,
		["Green"] = 5,
		["Black"] = 16,
		["White"] = 2,
		["Pink"] = 7
	};

	// Custom item icon cache
	private static readonly Dictionary<string, Texture2D> _itemTextures = new(StringComparer.OrdinalIgnoreCase);
	private static string _iconsBasePath;

	public static void Initialize(
		IDictionary<string, ObjectData> objectData,
		IDictionary<string, CropData> cropData,
		IDictionary<string, string> craftingRecipes)
	{
		if (objectData == null || cropData == null) return;

		// 1. Register Apricorns, Seeds & Cooked Apricorns
		foreach (string color in ApricornColors)
		{
			string ptColor = PortugueseColorNames[color];

			// Raw Apricorn
			string rawId = $"Poke{color}Apricorn";
			if (!objectData.ContainsKey(rawId))
			{
				objectData[rawId] = new ObjectData
				{
					Name = rawId,
					DisplayName = $"Apricorn {ptColor}",
					Description = $"Um fruto exótico do vale. Cozinhe na fornalha com carvão para preparar o material de confecção de Poké Bolas.",
					Type = "Basic",
					Category = StardewValley.Object.GreensCategory,
					Price = 35,
					SpriteIndex = RawApricornSprites[color]
				};
			}

			// Cooked Apricorn
			string cookedId = $"Cooked{color}Apricorn";
			if (!objectData.ContainsKey(cookedId))
			{
				objectData[cookedId] = new ObjectData
				{
					Name = cookedId,
					DisplayName = $"Apricorn {ptColor} Assado",
					Description = $"Um apricorn {ptColor.ToLowerInvariant()} devidamente aquecido e endurecido na fornalha. Pronto para forjar Poké Bolas.",
					Type = "Crafting",
					Category = StardewValley.Object.CraftingCategory,
					Price = 60,
					SpriteIndex = 342
				};
			}

			// Apricorn Seed / Crop (Semente de Apricorn)
			string seedId = $"Poke{color}Seed";
			if (!objectData.ContainsKey(seedId))
			{
				objectData[seedId] = new ObjectData
				{
					Name = seedId,
					DisplayName = $"Semente de Apricorn {ptColor}",
					Description = $"Semente de Apricorn. Plante na terra arada em qualquer estação exceto no inverno (primavera, verão e outono). Leva 4 dias para amadurecer e produz Apricorns a cada 2 dias.",
					Type = "Seeds",
					Category = StardewValley.Object.SeedsCategory,
					Price = 40,
					SpriteIndex = SeedSprites[color]
				};
			}

			// Crop Data (4 days to mature in non-winter, regrows every 2 days)
			if (!cropData.ContainsKey(seedId))
			{
				cropData[seedId] = new CropData
				{
					Seasons = new List<Season> { Season.Spring, Season.Summer, Season.Fall },
					DaysInPhase = new List<int> { 1, 1, 1, 1 },
					RegrowDays = 2,
					HarvestItemId = rawId,
					HarvestMinStack = 1,
					HarvestMaxStack = 2,
					ExtraHarvestChance = 0.15,
					SpriteIndex = CropGrowthRows[color]
				};
			}
		}

		// 2. Register Poké Balls (Names strictly UNTRANSLATED in English as requested)
		RegisterBall(objectData, "PokeBall", "Poké Ball", "Um dispositivo esférico para capturar e abrigar Pokémon selvagens.", 688, 100);
		RegisterBall(objectData, "GreatBall", "Great Ball", "Uma Poké Bola de alto desempenho com taxa de captura superior.", 690, 250);
		RegisterBall(objectData, "UltraBall", "Ultra Ball", "Uma Poké Bola ultra eficiente com altíssima taxa de sucesso.", 689, 500);
		RegisterBall(objectData, "MasterBall", "Master Ball", "A melhor Poké Bola com taxa de captura infalível de 100%.", 689, 10000);
		RegisterBall(objectData, "PremierBall", "Premier Ball", "Uma Poké Bola comemorativa rara feita com puro Apricorn Branco.", 688, 200);
		RegisterBall(objectData, "FastBall", "Fast Ball", "Uma Poké Bola rápida ideal para capturar Pokémon velozes.", 689, 300);
		RegisterBall(objectData, "HeavyBall", "Heavy Ball", "Uma Poké Bola pesada projetada para Pokémon corpulentos.", 690, 300);
		RegisterBall(objectData, "HealBall", "Heal Ball", "Uma Poké Bola restauradora que recupera a vida do Pokémon capturado.", 342, 300);
		RegisterBall(objectData, "LuxuryBall", "Luxury Ball", "Uma Poké Bola extremamente confortável que acelera o ganho de amizade do Pokémon.", 689, 1000);
		RegisterBall(objectData, "NetBall", "Net Ball", "Uma Poké Bola especializada em capturar Pokémon do tipo Água ou Inseto.", 688, 300);
		RegisterBall(objectData, "NestBall", "Nest Ball", "Uma Poké Bola que funciona melhor quanto menor o nível do Pokémon selvagem.", 688, 300);
		RegisterBall(objectData, "DuskBall", "Dusk Ball", "Uma Poké Bola noturna com altíssima taxa de sucesso em cavernas ou durante a noite.", 690, 500);
		RegisterBall(objectData, "DiveBall", "Dive Ball", "Uma Poké Bola subaquática com grande bônus de captura no oceano ou na pesca.", 688, 300);
		RegisterBall(objectData, "QuickBall", "Quick Ball", "Uma Poké Bola rápida com taxa de captura 5x maior no primeiro turno da batalha.", 689, 500);
		RegisterBall(objectData, "TimerBall", "Timer Ball", "Uma Poké Bola cuja eficácia aumenta a cada turno que passa na batalha.", 688, 400);
		RegisterBall(objectData, "RepeatBall", "Repeat Ball", "Uma Poké Bola que facilita capturar espécies já registradas na Pokédex.", 688, 300);
		RegisterBall(objectData, "MoonBall", "Moon Ball", "Uma Poké Bola de Apricorn Preto e Amarelo, perfeita para evoluções por pedra ou amizade noturna.", 690, 300);
		RegisterBall(objectData, "LoveBall", "Love Ball", "Uma Poké Bola de Apricorn Rosa que facilita capturar Pokémon do gênero oposto.", 342, 300);
		RegisterBall(objectData, "LevelBall", "Level Ball", "Uma Poké Bola de Apricorn Vermelho e Amarelo com taxa superior para Pokémon de nível mais baixo.", 688, 300);
		RegisterBall(objectData, "FriendBall", "Friend Ball", "Uma Poké Bola de Apricorn Verde que faz o Pokémon iniciar com 120 de amizade imediata.", 688, 300);
		RegisterBall(objectData, "LureBall", "Lure Ball", "Uma Poké Bola de Apricorn Azul que captura facilmente Pokémon pescados ou na água.", 688, 300);

		RegisterSimple(objectData, "PokemonFriendshipCandy", "Friendship Candy", "Um doce especial cristalizado com afeto supremo. Eleva instantaneamente a Amizade do Pokémon com seu treinador ao valor máximo (255). Obtível em modo debug.", 1, 74);

		// 3. Register Potions & Revives
		if (!objectData.ContainsKey("PokemonPotion"))
		{
			objectData["PokemonPotion"] = new ObjectData
			{
				Name = "PokemonPotion",
				DisplayName = "Potion",
				Description = "Um spray medicinal que restaura 50 pontos de vida do seu Pokémon ativo em combate.",
				Type = "Cooking",
				Category = StardewValley.Object.CookingCategory,
				Price = 150,
				SpriteIndex = 349,
				Edibility = -300
			};
		}
		if (!objectData.ContainsKey("PokemonSuperPotion"))
		{
			objectData["PokemonSuperPotion"] = new ObjectData
			{
				Name = "PokemonSuperPotion",
				DisplayName = "Super Potion",
				Description = "Um spray medicinal concentrado que restaura 100 pontos de vida do seu Pokémon.",
				Type = "Cooking",
				Category = StardewValley.Object.CookingCategory,
				Price = 350,
				SpriteIndex = 350,
				Edibility = -300
			};
		}
		if (!objectData.ContainsKey("PokemonRevive"))
		{
			objectData["PokemonRevive"] = new ObjectData
			{
				Name = "PokemonRevive",
				DisplayName = "Revive",
				Description = "Um medicamento medicinal precioso que reanima um Pokémon desmaiado, restaurando metade do seu HP máximo.",
				Type = "Cooking",
				Category = StardewValley.Object.CookingCategory,
				Price = 1000,
				SpriteIndex = 348,
				Edibility = -300
			};
		}

		// 4. Status Cure Medicines & Potions
		RegisterSimple(objectData, "PokemonAntidote", "Antidote", "Spray medicinal que neutraliza o envenenamento (Poison ou Toxic) de um Pokémon.", 100, 349);
		RegisterSimple(objectData, "PokemonParalyzeHeal", "Paralyze Heal", "Spray medicinal que cura paralisia de um Pokémon.", 150, 349);
		RegisterSimple(objectData, "PokemonAwakening", "Awakening", "Spray medicinal que desperta um Pokémon adormecido.", 100, 349);
		RegisterSimple(objectData, "PokemonBurnHeal", "Burn Heal", "Spray medicinal refrescante que cura queimaduras de um Pokémon.", 150, 349);
		RegisterSimple(objectData, "PokemonIceHeal", "Ice Heal", "Spray medicinal descongelante que dissolve o gelo de um Pokémon congelado.", 150, 349);
		RegisterSimple(objectData, "PokemonFullHeal", "Full Heal", "Medicamento completo de amplo espectro que cura qualquer alteração de status de um Pokémon.", 400, 349);
		RegisterSimple(objectData, "PokemonMaxPotion", "Max Potion", "Spray medicinal de potência máxima que recupera 100% do HP de um Pokémon ferido.", 1200, 350);

		// 4b. Rare Candy, Repels and TMs
		RegisterSimple(objectData, "PokemonRareCandy", "Rare Candy", "Um doce misterioso e raríssimo que eleva um Pokémon em 1 nível instantaneamente.", 15000, 72);
		RegisterSimple(objectData, "PokemonFriendshipCandy", "Friendship Candy", "Um doce celestial que maximiza a amizade do Pokémon instantaneamente para 255. (Item exclusivo de Debug)", 99999, 72);
		RegisterSimple(objectData, "PokemonRepel", "Repel", "Afasta Pokémon selvagens por 100 passos.", 350, 382);
		RegisterSimple(objectData, "PokemonSuperRepel", "Super Repel", "Afasta Pokémon selvagens por 200 passos.", 500, 382);
		RegisterSimple(objectData, "PokemonMaxRepel", "Max Repel", "Afasta Pokémon selvagens por 250 passos.", 700, 382);

		foreach (string moveName in PokemonMove.AllMoveNames())
		{
			string tmId = PokemonItemUse.TmId(moveName);
			if (objectData.ContainsKey(tmId)) continue;
			var mv = PokemonMove.Get(moveName);
			RegisterSimple(objectData, tmId, "TM " + moveName, $"Disco de técnica. Ensina {moveName} ({mv.Type}, poder {mv.Power}) a um Pokémon da sua equipe.", 1500 + mv.Power * 25, 74);
		}

		// 5. Register Healing Machine, PC, and Trade Terminal
		if (!objectData.ContainsKey("PokemonHealingMachine"))
		{
			objectData["PokemonHealingMachine"] = new ObjectData
			{
				Name = "PokemonHealingMachine",
				DisplayName = "Mesa de Recuperação Pokémon",
				Description = "Estação médica portátil que cura completamente todos os Pokémon da sua equipe ao ser utilizada.",
				Type = "Crafting",
				Category = StardewValley.Object.CraftingCategory,
				Price = 2500,
				SpriteIndex = 21
			};
		}
		if (!objectData.ContainsKey("PokemonPC"))
		{
			objectData["PokemonPC"] = new ObjectData
			{
				Name = "PokemonPC",
				DisplayName = "PC de Armazenamento Pokémon",
				Description = "Terminal de computador para gerenciar caixas do PC, guardar e retirar Pokémon.",
				Type = "Crafting",
				Category = StardewValley.Object.CraftingCategory,
				Price = 3000,
				SpriteIndex = 21
			};
		}
		if (!objectData.ContainsKey("PokemonTradeTerminal"))
		{
			objectData["PokemonTradeTerminal"] = new ObjectData
			{
				Name = "PokemonTradeTerminal",
				DisplayName = "Terminal de Troca Pokémon",
				Description = "Terminal de Cabo Link para trocar Pokémon com amigos no Co-op e acionar evoluções de troca.",
				Type = "Crafting",
				Category = StardewValley.Object.CraftingCategory,
				Price = 3500,
				SpriteIndex = 21
			};
		}

		// Register Evolution Stones & Link Cable
		RegisterSimple(objectData, "PokemonWaterStone", "Pedra da Água", "Uma pedra peculiar que faz certas espécies de Pokémon aquáticos evoluírem.", 2000, 84);
		RegisterSimple(objectData, "PokemonFireStone", "Pedra do Fogo", "Uma pedra peculiar que faz certas espécies de Pokémon de fogo evoluírem.", 2000, 84);
		RegisterSimple(objectData, "PokemonThunderStone", "Pedra do Trovão", "Uma pedra peculiar que faz certas espécies de Pokémon elétricos evoluírem.", 2000, 84);
		RegisterSimple(objectData, "PokemonLeafStone", "Pedra da Folha", "Uma pedra peculiar que faz certas espécies de Pokémon de planta evoluírem.", 2000, 84);
		RegisterSimple(objectData, "PokemonMoonStone", "Pedra da Lua", "Uma pedra escura e misteriosa que induz evolução em certos Pokémon.", 2500, 84);
		RegisterSimple(objectData, "PokemonSunStone", "Pedra do Sol", "Uma pedra solar brilhante que induz evolução em certos Pokémon.", 2500, 84);
		RegisterSimple(objectData, "PokemonShinyStone", "Pedra do Brilho", "Uma pedra deslumbrante que induz evolução em certos Pokémon.", 3000, 84);
		RegisterSimple(objectData, "PokemonDuskStone", "Pedra do Crepúsculo", "Uma pedra com sombras etéreas que induz evolução em certos Pokémon.", 3000, 84);
		RegisterSimple(objectData, "PokemonDawnStone", "Pedra da Alvorada", "Uma pedra com brilho matinal que induz evolução em certos Pokémon.", 3000, 84);
		RegisterSimple(objectData, "PokemonIceStone", "Pedra do Gelo", "Uma pedra cristalina congelante que induz evolução em certos Pokémon.", 3000, 84);
		RegisterSimple(objectData, "PokemonLinkCable", "Cabo de Ligação", "Dispositivo misterioso que emite energia de troca, permitindo evoluções de troca instantaneamente.", 3500, 84);

		// Register Exp. Share & Gimmick Rings as Equippable Stardew Valley Rings (Category -96, Type "Ring")
		RegisterRing(objectData, "PokemonExpShare", "Anel Exp. Share", "Equipamento especial na categoria Anel. Enquanto equipado no jogador, compartilha a experiência das batalhas Pokémon com toda a equipe (estilo Gen 6+).", 5000, 517);
		RegisterRing(objectData, "Poke_MegaBracelet", "Bracelete Mega", "Bracelete incrustado com uma Key Stone. Equipado no slot de Anel junto com a Mega Pedra específica no inventário, permite Mega Evoluir em batalha.", 10000, 527);
		RegisterRing(objectData, "Poke_ZRing", "Anel Z (Z-Ring)", "Bracelete místico de Alola. Equipado no slot de Anel junto a um Cristal Z compatível no inventário, permite usar Z-Moves em batalha.", 10000, 524);
		RegisterRing(objectData, "Poke_ZPowerRing", "Super Anel Z (Z-Power Ring)", "Versão suprema do Anel Z de Alola. Equipado no slot de Anel junto a um Cristal Z compatível no inventário, libera Z-Moves em batalha.", 12000, 524);
		RegisterRing(objectData, "Poke_DynamaxBand", "Bracelete Dynamax", "Bracelete energético de Galar forjado com Estrela Desejante. Equipado no slot de Anel, permite Dynamax e Gigantamax em batalha.", 12000, 534);

		RegisterRing(objectData, "Poke_TeraOrb", "Orbe Tera (Tera Orb)", "Esfera cristalina lendária de Paldea. Equipada no slot de Anel, permite a Terastallization em batalha, alterando o tipo defensivo do Pokémon e concedendo STAB aprimorado.", 15000, 524);

		// Register 18 Type Z-Crystals
		foreach (var kv in ZCrystalDefinitions)
		{
			RegisterSimple(objectData, kv.Key, kv.Value.DisplayName, $"Cristal Z elemental ({kv.Value.Type}). Mantenha na mochila com o Anel Z equipado para liberar Z-Moves do tipo {kv.Value.Type}.", 4000, 72);
		}

		// Register 17 Signature Z-Crystals
		foreach (var kv in SignatureZCrystalDefinitions)
		{
			RegisterSimple(objectData, kv.Key, kv.Value.DisplayName, $"Cristal Z de assinatura para {kv.Value.RequiredSpecies}. Requer o Z-Power Ring equipado e o golpe {kv.Value.RequiredBaseMove} para disparar {kv.Value.ZMoveName}.", 6000, 72);
		}

		// Register 19 Tera Shards
		foreach (PokemonType t in (PokemonType[])Enum.GetValues(typeof(PokemonType)))
		{
			string shardId = $"Poke_TeraShard_{t}";
			RegisterSimple(objectData, shardId, $"Tera Shard ({t})", $"Fragmento cristalino de tipo {t}. Junte 50 para lapidar e trocar o Tera Type de um Pokémon no ferreiro Clint.", 500, 72);
		}
		RegisterSimple(objectData, "Poke_TeraShard_Stellar", "Tera Shard (Stellar)", "Fragmento cristalino prismático supremo. Permite despertar a forma estelar.", 1500, 72);

		// Register 14 Legendary Encounter Relics
		RegisterSimple(objectData, "Poke_FreezingFeather", "Pena Glacial", "Pena ancestral emanando ar gélido a zero absoluto. Dizem atrair o pássaro lendário Articuno nas profundezas geladas.", 8000, 74);
		RegisterSimple(objectData, "Poke_ElectricFeather", "Pena Elétrica", "Pena crepitante com eletricidade estática. Capaz de invocar o trovão de Zapdos em tempestades.", 8000, 74);
		RegisterSimple(objectData, "Poke_FlameFeather", "Pena de Chamas", "Pena eternamente em brasa. Capaz de despertar Moltres no coração vulcânico.", 8000, 74);
		RegisterSimple(objectData, "Poke_BerserkGene", "Gene Berserk", "Material genético modificado de altíssima frequência psíquica. Ressona com Mewtwo na Caverna da Caveira.", 15000, 74);
		RegisterSimple(objectData, "Poke_OldSeaMap", "Mapa do Mar Antigo", "Um mapa desgastado indicando uma ilha isolada e o rastro do mítico Mew.", 10000, 74);
		RegisterSimple(objectData, "Poke_ClearBell", "Sino Claro", "Um sino de cristal que tilinta com pureza espiritual, despertando os cães lendários Raikou, Entei e Suicune.", 10000, 74);
		RegisterSimple(objectData, "Poke_SilverWing", "Asa Prateada", "Asa reluzente guardiã dos mares profundos. Desperta o lendário Lugia na fossa oceânica.", 12000, 74);
		RegisterSimple(objectData, "Poke_RainbowWing", "Asa Arco-Íris", "Asa brilhante com as sete cores do prisma. Invoca o pássaro arco-íris Ho-Oh no Pico da Montanha.", 12000, 74);
		RegisterSimple(objectData, "Poke_BlueOrb", "Esfera Azul", "Esfera ancestral pulsando com o poder das marés oceânicas de Kyogre.", 15000, 74);
		RegisterSimple(objectData, "Poke_RedOrb", "Esfera Vermelha", "Esfera ancestral ardente capaz de canalizar o poder telúrico de Groudon.", 15000, 74);
		RegisterSimple(objectData, "Poke_JadeOrb", "Esfera de Jade", "Esfera das nuvens que comanda o dragão dos céus Rayquaza.", 20000, 74);
		RegisterSimple(objectData, "Poke_AdamantOrb", "Adamant Orb", "Esfera brilhante do tempo que ressoa com Dialga.", 15000, 74);
		RegisterSimple(objectData, "Poke_LustrousOrb", "Lustrous Orb", "Esfera reluzente do espaço que ressoa com Palkia.", 15000, 74);
		RegisterSimple(objectData, "Poke_GriseousCore", "Griseous Core", "Núcleo misterioso do Mundo Distorcido pertencente a Giratina.", 15000, 74);
		RegisterSimple(objectData, "Poke_IndigoDisk", "Disco Índigo", "Disco tecnológico e cristalino de Teracristalização profunda pertencente a Terapagos.", 20000, 74);

		// Register Berries as Items and Crops
		// Register Berries as Items and Crops (Valid TileSheets\crops sprite rows and all 4 seasons)
		var berrySpriteRows = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			["OranBerry"] = 12,    // Blueberry bush
			["SitrusBerry"] = 27,  // Starfruit
			["LumBerry"] = 38,     // Sweet Gem Berry
			["CheriBerry"] = 16,   // Cranberry (red bush)
			["ChestoBerry"] = 14,  // Hops
			["PechaBerry"] = 21,   // Strawberry (pink/peach bush)
			["RawstBerry"] = 5,    // Green crop
			["AspearBerry"] = 24,  // Icy Bok Choy
			["LeppaBerry"] = 11,   // Tomato bush
			["PersimBerry"] = 26   // Hot Pepper
		};

		var berryDefs = new (string Id, string Name, string Desc, int Price)[]
		{
			("OranBerry", "Oran Berry", "Baga azul suculenta. Restaura 15 de HP de um Pokémon em batalha. Planta-se em qualquer estação; produz a cada 2 dias.", 60),
			("SitrusBerry", "Sitrus Berry", "Baga amarela energizante. Restaura 25% do HP máximo de um Pokémon em batalha. Planta-se em qualquer estação; produz a cada 2 dias.", 350),
			("LumBerry", "Lum Berry", "Baga miraculosa. Cura qualquer condição de status primária instantaneamente. Planta-se em qualquer estação; produz a cada 2 dias.", 500),
			("CheriBerry", "Cheri Berry", "Baga vermelha picante. Cura paralisia instantaneamente. Planta-se em qualquer estação; produz a cada 2 dias.", 80),
			("ChestoBerry", "Chesto Berry", "Baga seca revigorante. Desperta Pokémon adormecidos. Planta-se em qualquer estação; produz a cada 2 dias.", 80),
			("PechaBerry", "Pecha Berry", "Baga doce e macia. Neutraliza veneno comum ou grave. Planta-se em qualquer estação; produz a cada 2 dias.", 80),
			("RawstBerry", "Rawst Berry", "Baga amarga refrescante. Cura queimaduras. Planta-se em qualquer estação; produz a cada 2 dias.", 80),
			("AspearBerry", "Aspear Berry", "Baga rígida e azeda. Descongela Pokémon solidificados. Planta-se em qualquer estação; produz a cada 2 dias.", 80),
			("LeppaBerry", "Leppa Berry", "Baga de aroma marcante. Restaura 10 PP de um movimento esgotado. Planta-se em qualquer estação; produz a cada 2 dias.", 150),
			("PersimBerry", "Persim Berry", "Baga aromática relaxante. Dissipa a confusão mental. Planta-se em qualquer estação; produz a cada 2 dias.", 120)
		};

		var allSeasons = new List<Season> { Season.Spring, Season.Summer, Season.Fall, Season.Winter };

		foreach (var b in berryDefs)
		{
			RegisterSimple(objectData, b.Id, b.Name, b.Desc, b.Price, 72);
			RegisterSimple(objectData, "Poke_" + b.Id, b.Name, b.Desc, b.Price, 72);
			if (objectData.TryGetValue(b.Id, out var oData1))
			{
				oData1.Category = StardewValley.Object.SeedsCategory;
				oData1.Type = "Seeds";
			}
			if (objectData.TryGetValue("Poke_" + b.Id, out var oData2))
			{
				oData2.Category = StardewValley.Object.SeedsCategory;
				oData2.Type = "Seeds";
			}

			int spriteRow = berrySpriteRows.TryGetValue(b.Id, out int sRow) ? sRow : 12;

			var cd = new CropData
			{
				Seasons = allSeasons,
				DaysInPhase = new List<int> { 1, 1, 1, 1 },
				RegrowDays = 2,
				HarvestItemId = "(O)Poke_" + b.Id,
				HarvestMinStack = 2,
				HarvestMaxStack = 4,
				ExtraHarvestChance = 0.2,
				SpriteIndex = spriteRow
			};
			cropData[b.Id] = cd;
			cropData["Poke_" + b.Id] = cd;
			cropData["(O)" + b.Id] = cd;
			cropData["(O)Poke_" + b.Id] = cd;
		}

		// Register Mega Stones & Primal Orbs
		foreach (var kv in MegaStoneDefinitions)
		{
			string targetMon = kv.Value.DisplayName.Replace("Pedra Mega ", "").Replace("ite X", " X").Replace("ite Y", " Y").Replace("ite", "").Replace("nite", "");
			RegisterSimple(objectData, kv.Key, kv.Value.DisplayName, $"Mega Pedra ancestral ressonante com {targetMon}. Mantenha no inventário com o Bracelete Mega equipado para Mega Evoluir em batalha.", 5000, 74);
		}

		// Register all Competitive Held Items & Stat Vitamins in Game1.objectData
		foreach (var kv in PokemonHeldItemManager.AllHeldItems)
		{
			RegisterSimple(objectData, kv.Key, kv.Value.DisplayName, kv.Value.Description, kv.Value.Price, 72);
			RegisterSimple(objectData, "Poke_" + kv.Key, kv.Value.DisplayName, kv.Value.Description, kv.Value.Price, 72);
		}

		// 6. Register Crafting Recipes
		if (craftingRecipes != null)
		{
			craftingRecipes["PokeBall"] = "(O)CookedRedApricorn 2 (O)CookedWhiteApricorn 1 (O)335 1//(O)PokeBall 3/false/default/Poké Ball";
			craftingRecipes["GreatBall"] = "(O)CookedBlueApricorn 2 (O)CookedRedApricorn 1 (O)335 1//(O)GreatBall 3/false/default/Great Ball";
			craftingRecipes["UltraBall"] = "(O)CookedYellowApricorn 2 (O)CookedBlackApricorn 1 (O)336 1//(O)UltraBall 3/false/default/Ultra Ball";
			craftingRecipes["PremierBall"] = "(O)CookedWhiteApricorn 3 (O)335 1//(O)PremierBall 3/false/default/Premier Ball";
			craftingRecipes["LuxuryBall"] = "(O)CookedBlackApricorn 2 (O)CookedYellowApricorn 1 (O)336 1//(O)LuxuryBall 3/false/default/Luxury Ball";
			craftingRecipes["NetBall"] = "(O)CookedBlueApricorn 2 (O)CookedGreenApricorn 1 (O)335 1//(O)NetBall 3/false/default/Net Ball";
			craftingRecipes["NestBall"] = "(O)CookedGreenApricorn 2 (O)CookedYellowApricorn 1 (O)335 1//(O)NestBall 3/false/default/Nest Ball";
			craftingRecipes["DuskBall"] = "(O)CookedGreenApricorn 2 (O)CookedBlackApricorn 1 (O)336 1//(O)DuskBall 3/false/default/Dusk Ball";
			craftingRecipes["DiveBall"] = "(O)CookedBlueApricorn 3 (O)335 1//(O)DiveBall 3/false/default/Dive Ball";
			craftingRecipes["QuickBall"] = "(O)CookedYellowApricorn 2 (O)CookedBlueApricorn 1 (O)335 1//(O)QuickBall 3/false/default/Quick Ball";
			craftingRecipes["TimerBall"] = "(O)CookedWhiteApricorn 2 (O)CookedYellowApricorn 1 (O)335 1//(O)TimerBall 3/false/default/Timer Ball";
			craftingRecipes["RepeatBall"] = "(O)CookedRedApricorn 2 (O)CookedYellowApricorn 1 (O)335 1//(O)RepeatBall 3/false/default/Repeat Ball";
			craftingRecipes["MoonBall"] = "(O)CookedBlackApricorn 2 (O)CookedYellowApricorn 1 (O)335 1//(O)MoonBall 3/false/default/Moon Ball";
			craftingRecipes["LoveBall"] = "(O)CookedPinkApricorn 3 (O)334 1//(O)LoveBall 3/false/default/Love Ball";
			craftingRecipes["LevelBall"] = "(O)CookedRedApricorn 2 (O)CookedYellowApricorn 1 (O)335 1//(O)LevelBall 3/false/default/Level Ball";
			craftingRecipes["FriendBall"] = "(O)CookedGreenApricorn 2 (O)CookedPinkApricorn 1 (O)334 1//(O)FriendBall 3/false/default/Friend Ball";
			craftingRecipes["LureBall"] = "(O)CookedBlueApricorn 2 (O)CookedRedApricorn 1 (O)334 1//(O)LureBall 3/false/default/Lure Ball";
			craftingRecipes["FastBall"] = "(O)CookedYellowApricorn 2 (O)CookedWhiteApricorn 1 (O)334 1//(O)FastBall 3/false/default/Fast Ball";
			craftingRecipes["HeavyBall"] = "(O)CookedBlackApricorn 2 (O)CookedBlueApricorn 1 (O)335 1//(O)HeavyBall 3/false/default/Heavy Ball";
			craftingRecipes["HealBall"] = "(O)CookedPinkApricorn 2 (O)CookedWhiteApricorn 1 (O)335 1//(O)HealBall 3/false/default/Heal Ball";
			craftingRecipes["PokemonSuperRod"] = "(O)335 5 (O)336 2 (O)688 1 (O)771 10//(T)PokemonSuperRod 1/false/default/Super Rod";
			craftingRecipes["PokemonRareCandy"] = "(O)74 1 (O)72 3 (O)337 5 (O)PokemonRevive 2//(O)PokemonRareCandy 1/false/default/Rare Candy";
			craftingRecipes["PokemonRepel"] = "(O)382 1 (O)771 5//(O)PokemonRepel 1/false/default/Repel";
			craftingRecipes["PokemonSuperRepel"] = "(O)382 2 (O)335 1 (O)771 5//(O)PokemonSuperRepel 1/false/default/Super Repel";
			craftingRecipes["PokemonMaxRepel"] = "(O)382 3 (O)336 1 (O)771 10//(O)PokemonMaxRepel 1/false/default/Max Repel";
			craftingRecipes["PokemonHealingMachine"] = "(O)335 5 (O)336 2 (O)787 1 (O)338 5//(O)PokemonHealingMachine 1/false/default/Mesa de Recuperação Pokémon";
			craftingRecipes["PokemonPC"] = "(O)335 5 (O)336 2 (O)787 1 (O)338 5//(O)PokemonPC 1/false/default/PC de Armazenamento Pokémon";
			craftingRecipes["PokemonTradeTerminal"] = "(O)335 5 (O)336 2 (O)787 1 (O)338 5//(O)PokemonTradeTerminal 1/false/default/Terminal de Troca Pokémon";
			craftingRecipes["PokemonLinkCable"] = "(O)335 2 (O)336 1 (O)787 1//(O)PokemonLinkCable 1/false/default/Cabo de Ligação";
			craftingRecipes["PokemonExpShare"] = "(O)336 2 (O)337 1 (O)787 1//(O)PokemonExpShare 1/false/default/Anel Exp. Share";
			craftingRecipes["Poke_MegaBracelet"] = "(O)337 2 (O)74 1 (O)787 1//(O)Poke_MegaBracelet 1/false/default/Bracelete Mega";
			craftingRecipes["Poke_ZRing"] = "(O)337 2 (O)72 2 (O)787 1//(O)Poke_ZRing 1/false/default/Anel Z (Z-Ring)";
			craftingRecipes["Poke_ZPowerRing"] = "(O)Poke_ZRing 1 (O)337 5 (O)74 2//(O)Poke_ZPowerRing 1/false/default/Super Anel Z (Z-Power Ring)";
			craftingRecipes["Poke_DynamaxBand"] = "(O)337 2 (O)84 2 (O)787 1//(O)Poke_DynamaxBand 1/false/default/Bracelete Dynamax";
			craftingRecipes["Poke_TeraOrb"] = "(O)337 5 (O)74 2 (O)Poke_TeraShard_Normal 10//(O)Poke_TeraOrb 1/false/default/Orbe Tera (Tera Orb)";
		}

		ItemRegistry.ResetCache();
	}

	private static void RegisterRing(IDictionary<string, ObjectData> objectData, string id, string displayName, string description, int price, int spriteIndex)
	{
		if (objectData.ContainsKey(id)) return;
		objectData[id] = new ObjectData
		{
			Name = id,
			DisplayName = displayName,
			Description = description,
			Type = "Ring",
			Category = StardewValley.Object.ringCategory,
			Price = price,
			SpriteIndex = spriteIndex,
			Edibility = -300,
			ContextTags = new List<string> { "item_type_ring", "ring_item" }
		};
	}

	public static bool IsWearingRingId(Farmer player, string ringId)
	{
		if (player == null || string.IsNullOrEmpty(ringId)) return false;
		if (player.isWearingRing(ringId)) return true;
		if (player.leftRing.Value != null && (player.leftRing.Value.ItemId == ringId || player.leftRing.Value.GetsEffectOfRing(ringId)))
			return true;
		if (player.rightRing.Value != null && (player.rightRing.Value.ItemId == ringId || player.rightRing.Value.GetsEffectOfRing(ringId)))
			return true;
		return false;
	}

	public static bool IsWearingExpShare(Farmer player) => IsWearingRingId(player, "PokemonExpShare");

	public static bool IsWearingMegaBracelet(Farmer player) => IsWearingRingId(player, "Poke_MegaBracelet");

	public static bool IsWearingZRing(Farmer player) => IsWearingRingId(player, "Poke_ZRing") || IsWearingRingId(player, "Poke_ZPowerRing");

	public static bool IsWearingZPowerRing(Farmer player) => IsWearingRingId(player, "Poke_ZPowerRing");

	public static bool IsWearingTeraOrb(Farmer player) => IsWearingRingId(player, "Poke_TeraOrb") || (player?.Items != null && player.Items.ContainsId("(O)Poke_TeraOrb"));

	public static bool IsWearingDynamaxBand(Farmer player) => IsWearingRingId(player, "Poke_DynamaxBand");

	public static bool IsPokemonItemId(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId)) return false;
		if (itemId.StartsWith("(O)")) itemId = itemId.Substring(3);
		return itemId.StartsWith("Poke_") ||
		       itemId.StartsWith("Pokemon") ||
		       PokemonHeldItemManager.IsHoldable(itemId) ||
		       PokemonHeldItemManager.TryGet(itemId, out _) ||
		       PokemonItemUse.IsTm(itemId);
	}

	public static readonly Dictionary<string, (PokemonType Type, string DisplayName, string IconFile)> ZCrystalDefinitions = new(StringComparer.OrdinalIgnoreCase)
	{
		["Poke_NormaliumZ"] = (PokemonType.Normal, "Normalium Z", "normalium_z.png"),
		["Poke_FiriumZ"] = (PokemonType.Fire, "Firium Z", "firium_z.png"),
		["Poke_WateriumZ"] = (PokemonType.Water, "Waterium Z", "waterium_z.png"),
		["Poke_ElectriumZ"] = (PokemonType.Electric, "Electrium Z", "electrium_z.png"),
		["Poke_GrassiumZ"] = (PokemonType.Grass, "Grassium Z", "grassium_z.png"),
		["Poke_IciumZ"] = (PokemonType.Ice, "Icium Z", "icium_z.png"),
		["Poke_FightiniumZ"] = (PokemonType.Fighting, "Fightinium Z", "fightinium_z.png"),
		["Poke_PoisoniumZ"] = (PokemonType.Poison, "Poisonium Z", "poisonium_z.png"),
		["Poke_GroundiumZ"] = (PokemonType.Ground, "Groundium Z", "groundium_z.png"),
		["Poke_FlyiniumZ"] = (PokemonType.Flying, "Flyinium Z", "flyinium_z.png"),
		["Poke_PsychiumZ"] = (PokemonType.Psychic, "Psychium Z", "psychium_z.png"),
		["Poke_BuginiumZ"] = (PokemonType.Bug, "Buginium Z", "buginium_z.png"),
		["Poke_RockiumZ"] = (PokemonType.Rock, "Rockium Z", "rockium_z.png"),
		["Poke_GhostiumZ"] = (PokemonType.Ghost, "Ghostium Z", "ghostium_z.png"),
		["Poke_DragoniumZ"] = (PokemonType.Dragon, "Dragonium Z", "dragonium_z.png"),
		["Poke_DarkiniumZ"] = (PokemonType.Dark, "Darkinium Z", "darkinium_z.png"),
		["Poke_SteeliumZ"] = (PokemonType.Steel, "Steelium Z", "steelium_z.png"),
		["Poke_FairiumZ"] = (PokemonType.Fairy, "Fairium Z", "fairium_z.png")
	};

	public static readonly Dictionary<string, (string RequiredSpecies, string RequiredBaseMove, string ZMoveName, int Power, PokemonType Type, string DisplayName, string IconFile)> SignatureZCrystalDefinitions = new(StringComparer.OrdinalIgnoreCase)
	{
		["Poke_PikaniumZ"] = ("pikachu", "Volt Tackle", "Catastropika", 210, PokemonType.Electric, "Pikanium Z", "pikanium_z.png"),
		["Poke_AloraichiumZ"] = ("raichu", "Thunderbolt", "Stoked Sparksurfer", 175, PokemonType.Electric, "Aloraichium Z", "aloraichium_z.png"),
		["Poke_DecidiumZ"] = ("decidueye", "Spirit Shackle", "Sinister Arrow Raid", 180, PokemonType.Ghost, "Decidium Z", "decidium_z.png"),
		["Poke_InciniumZ"] = ("incineroar", "Darkest Lariat", "Malicious Moonsault", 180, PokemonType.Dark, "Incinium Z", "incinium_z.png"),
		["Poke_PrimariumZ"] = ("primarina", "Sparkling Aria", "Oceanic Operetta", 195, PokemonType.Water, "Primarium Z", "primarium_z.png"),
		["Poke_EeviumZ"] = ("eevee", "Last Resort", "Extreme Evoboost", 0, PokemonType.Normal, "Eevium Z", "eevium_z.png"),
		["Poke_SnorliumZ"] = ("snorlax", "Giga Impact", "Pulverizing Pancake", 210, PokemonType.Normal, "Snorlium Z", "snorlium_z.png"),
		["Poke_MewniumZ"] = ("mew", "Psychic", "Genesis Supernova", 185, PokemonType.Psychic, "Mewnium Z", "mewnium_z.png"),
		["Poke_TapuniumZ"] = ("tapu", "Nature's Madness", "Guardian of Alola", 0, PokemonType.Fairy, "Tapunium Z", "tapunium_z.png"),
		["Poke_MarshadiumZ"] = ("marshadow", "Spectral Thief", "Soul-Stealing 7-Star Strike", 195, PokemonType.Ghost, "Marshadium Z", "marshadium_z.png"),
		["Poke_KommoniumZ"] = ("kommo-o", "Clanging Scales", "Clangorous Soulblaze", 185, PokemonType.Dragon, "Kommonium Z", "kommonium_z.png"),
		["Poke_LycaniumZ"] = ("lycanroc", "Stone Edge", "Splintered Stormshards", 190, PokemonType.Rock, "Lycanium Z", "lycanium_z.png"),
		["Poke_MimikiumZ"] = ("mimikyu", "Play Rough", "Let's Snuggle Forever", 190, PokemonType.Fairy, "Mimikium Z", "mimikium_z.png"),
		["Poke_SolganiumZ"] = ("solgaleo", "Sunsteel Strike", "Searing Sunraze Smash", 200, PokemonType.Steel, "Solganium Z", "solganium_z.png"),
		["Poke_LunaliumZ"] = ("lunala", "Moongeist Beam", "Menacing Moonraze Maelstrom", 200, PokemonType.Ghost, "Lunalium Z", "lunalium_z.png"),
		["Poke_UltranecroziumZ"] = ("necrozma", "Photon Geyser", "Light That Burns the Sky", 200, PokemonType.Psychic, "Ultranecrozium Z", "ultranecrozium_z.png")
	};

	public static readonly Dictionary<string, (string TargetMegaInternal, string DisplayName, string IconFile)> MegaStoneDefinitions = new(StringComparer.OrdinalIgnoreCase)
	{
		["Poke_Venusaurite"] = ("venusaur/mega", "Venusaurite", "venusaurite.png"),
		["Poke_CharizarditeX"] = ("charizard/mega_x", "Charizardite X", "charizardite_x.png"),
		["Poke_CharizarditeY"] = ("charizard/mega_y", "Charizardite Y", "charizardite_y.png"),
		["Poke_Blastoisinite"] = ("blastoise/mega", "Blastoisinite", "blastoisinite.png"),
		["Poke_Beedrillite"] = ("beedrill/mega", "Beedrillite", "beedrillite.png"),
		["Poke_Pidgeotite"] = ("pidgeot/mega", "Pidgeotite", "pidgeotite.png"),
		["Poke_RaichuniteX"] = ("raichu/mega_x", "Raichunite X", "raichunite_x.png"),
		["Poke_RaichuniteY"] = ("raichu/mega_y", "Raichunite Y", "raichunite_y.png"),
		["Poke_Clefablite"] = ("clefable/mega", "Clefablite", "clefablite.png"),
		["Poke_Alakazite"] = ("alakazam/mega", "Alakazite", "alakazite.png"),
		["Poke_Victreebelite"] = ("victreebel/mega", "Victreebelite", "victreebelite.png"),
		["Poke_Slowbronite"] = ("slowbro/mega", "Slowbronite", "slowbronite.png"),
		["Poke_Gengarite"] = ("gengar/mega", "Gengarite", "gengarite.png"),
		["Poke_Kangaskhanite"] = ("kangaskhan/mega", "Kangaskhanite", "kangaskhanite.png"),
		["Poke_Starminite"] = ("starmie/mega", "Starminite", "starminite.png"),
		["Poke_Pinsirite"] = ("pinsir/mega", "Pinsirite", "pinsirite.png"),
		["Poke_Gyaradosite"] = ("gyarados/mega", "Gyaradosite", "gyaradosite.png"),
		["Poke_Aerodactylite"] = ("aerodactyl/mega", "Aerodactylite", "aerodactylite.png"),
		["Poke_Dragoninite"] = ("dragonite/mega", "Dragoninite", "dragoninite.png"),
		["Poke_MewtwoniteX"] = ("mewtwo/mega_x", "Mewtwonite X", "mewtwonite_x.png"),
		["Poke_MewtwoniteY"] = ("mewtwo/mega_y", "Mewtwonite Y", "mewtwonite_y.png"),
		["Poke_Meganiumite"] = ("meganium/mega", "Meganiumite", "meganiumite.png"),
		["Poke_Feraligite"] = ("feraligatr/mega", "Feraligite", "feraligite.png"),
		["Poke_Ampharosite"] = ("ampharos/mega", "Ampharosite", "ampharosite.png"),
		["Poke_Steelixite"] = ("steelix/mega", "Steelixite", "steelixite.png"),
		["Poke_Scizorite"] = ("scizor/mega", "Scizorite", "scizorite.png"),
		["Poke_Heracronite"] = ("heracross/mega", "Heracronite", "heracronite.png"),
		["Poke_Skarmorite"] = ("skarmory/mega", "Skarmorite", "skarmorite.png"),
		["Poke_Houndoominite"] = ("houndoom/mega", "Houndoominite", "houndoominite.png"),
		["Poke_Tyranitarite"] = ("tyranitar/mega", "Tyranitarite", "tyranitarite.png"),
		["Poke_Sceptilite"] = ("sceptile/mega", "Sceptilite", "sceptilite.png"),
		["Poke_Blazikenite"] = ("blaziken/mega", "Blazikenite", "blazikenite.png"),
		["Poke_Swampertite"] = ("swampert/mega", "Swampertite", "swampertite.png"),
		["Poke_Gardevoirite"] = ("gardevoir/mega", "Gardevoirite", "gardevoirite.png"),
		["Poke_Sablenite"] = ("sableye/mega", "Sablenite", "sablenite.png"),
		["Poke_Mawilite"] = ("mawile/mega", "Mawilite", "mawilite.png"),
		["Poke_Aggronite"] = ("aggron/mega", "Aggronite", "aggronite.png"),
		["Poke_Medichamite"] = ("medicham/mega", "Medichamite", "medichamite.png"),
		["Poke_Manectite"] = ("manectric/mega", "Manectite", "manectite.png"),
		["Poke_Sharpedonite"] = ("sharpedo/mega", "Sharpedonite", "sharpedonite.png"),
		["Poke_Cameruptite"] = ("camerupt/mega", "Cameruptite", "cameruptite.png"),
		["Poke_Altarianite"] = ("altaria/mega", "Altarianite", "altarianite.png"),
		["Poke_Banettite"] = ("banette/mega", "Banettite", "banettite.png"),
		["Poke_Chimechite"] = ("chimecho/mega", "Chimechite", "chimechite.png"),
		["Poke_Absolite"] = ("absol/mega", "Absolite", "absolite.png"),
		["Poke_Glalitite"] = ("glalie/mega", "Glalitite", "glalitite.png"),
		["Poke_Salamencite"] = ("salamence/mega", "Salamencite", "salamencite.png"),
		["Poke_Metagrossite"] = ("metagross/mega", "Metagrossite", "metagrossite.png"),
		["Poke_Latiasite"] = ("latias/mega", "Latiasite", "latiasite.png"),
		["Poke_Latiosite"] = ("latios/mega", "Latiosite", "latiosite.png"),
		["Poke_RedOrb"] = ("groudon/primal", "Red Orb", "red_orb.png"),
		["Poke_BlueOrb"] = ("kyogre/primal", "Blue Orb", "blue_orb.png"),
		["Poke_Rayquazite"] = ("rayquaza/mega", "Meteorite (Rayquaza)", "meteorite.png"),
		["Poke_Staraptite"] = ("staraptor/mega", "Staraptite", "staraptite.png"),
		["Poke_Lopunnite"] = ("lopunny/mega", "Lopunnite", "lopunnite.png"),
		["Poke_Garchompite"] = ("garchomp/mega", "Garchompite", "garchompite.png"),
		["Poke_Lucarionite"] = ("lucario/mega", "Lucarionite", "lucarionite.png"),
		["Poke_Abomasite"] = ("abomasnow/mega", "Abomasite", "abomasite.png"),
		["Poke_Galladite"] = ("gallade/mega", "Galladite", "galladite.png"),
		["Poke_Froslassite"] = ("froslass/mega", "Froslassite", "froslassite.png"),
		["Poke_Heatranite"] = ("heatran/mega", "Heatranite", "heatranite.png"),
		["Poke_Darkranite"] = ("darkrai/mega", "Darkranite", "darkranite.png"),
		["Poke_Emboarite"] = ("emboar/mega", "Emboarite", "emboarite.png"),
		["Poke_Excadrite"] = ("excadrill/mega", "Excadrite", "excadrite.png"),
		["Poke_Audinite"] = ("audino/mega", "Audinite", "audinite.png"),
		["Poke_Scolipite"] = ("scolipede/mega", "Scolipite", "scolipite.png"),
		["Poke_Scraftinite"] = ("scrafty/mega", "Scraftinite", "scraftinite.png"),
		["Poke_Eelektrossite"] = ("eelektross/mega", "Eelektrossite", "eelektrossite.png"),
		["Poke_Chandelurite"] = ("chandelure/mega", "Chandelurite", "chandelurite.png"),
		["Poke_Golurkite"] = ("golurk/mega", "Golurkite", "golurkite.png"),
		["Poke_Chesnaughtite"] = ("chesnaught/mega", "Chesnaughtite", "chesnaughtite.png"),
		["Poke_Delphoxite"] = ("delphox/mega", "Delphoxite", "delphoxite.png"),
		["Poke_Greninjite"] = ("greninja/mega", "Greninjite", "greninjite.png"),
		["Poke_Pyroarite"] = ("pyroar/mega", "Pyroarite", "pyroarite.png"),
		["Poke_Floettite"] = ("floette/mega", "Floettite", "floettite.png"),
		["Poke_Meowsticite"] = ("meowstic/mega", "Meowsticite", "meowsticite.png"),
		["Poke_Malamarite"] = ("malamar/mega", "Malamarite", "malamarite.png"),
		["Poke_Barbaracite"] = ("barbaracle/mega", "Barbaracite", "barbaracite.png"),
		["Poke_Dragalgite"] = ("dragalge/mega", "Dragalgite", "dragalgite.png"),
		["Poke_Hawluchanite"] = ("hawlucha/mega", "Hawluchanite", "hawluchanite.png"),
		["Poke_Zygardite"] = ("zygarde/mega", "Zygardite", "zygardite.png"),
		["Poke_Diancite"] = ("diancie/mega", "Diancite", "diancite.png"),
		["Poke_Crabominite"] = ("crabominable/mega", "Crabominite", "crabominite.png"),
		["Poke_Golisopite"] = ("golisopod/mega", "Golisopite", "golisopite.png"),
		["Poke_Drampanite"] = ("drampa/mega", "Drampanite", "drampanite.png"),
		["Poke_Magearnite"] = ("magearna/mega", "Magearnite", "magearnite.png"),
		["Poke_Zeraorite"] = ("zeraora/mega", "Zeraorite", "zeraorite.png"),
		["Poke_Falinksite"] = ("falinks/mega", "Falinksite", "falinksite.png"),
		["Poke_Scovillainite"] = ("scovillain/mega", "Scovillainite", "scovillainite.png"),
		["Poke_Glimmoranite"] = ("glimmora/mega", "Glimmoranite", "glimmoranite.png"),
		["Poke_Tatsugirinite"] = ("tatsugiri/mega", "Tatsugirinite", "tatsugirinite.png"),
		["Poke_Baxcalibrite"] = ("baxcalibur/mega", "Baxcalibrite", "baxcalibrite.png")
	};

	public static IEnumerable<string> AllMegaStoneIds => MegaStoneDefinitions.Keys;
	public static IEnumerable<string> AllZCrystalIds => ZCrystalDefinitions.Keys.Concat(SignatureZCrystalDefinitions.Keys);
	public static readonly List<string> AllTeraShardIds = Enum.GetValues<PokemonType>()
		.Select(t => $"Poke_TeraShard_{t}")
		.Append("Poke_TeraShard_Stellar")
		.ToList();

	public static string GetMegaStoneIdForMegaForm(string megaInternalName)
	{
		if (string.IsNullOrEmpty(megaInternalName)) return null;
		foreach (var kv in MegaStoneDefinitions)
		{
			if (string.Equals(kv.Value.TargetMegaInternal, megaInternalName, StringComparison.OrdinalIgnoreCase))
				return kv.Key;
		}
		return null;
	}

	public static string GetMegaStoneIdForSpecies(string baseInternalName, string megaInternalName = null)
	{
		if (!string.IsNullOrEmpty(megaInternalName))
		{
			string exact = GetMegaStoneIdForMegaForm(megaInternalName);
			if (!string.IsNullOrEmpty(exact)) return exact;
		}
		if (!string.IsNullOrEmpty(baseInternalName))
		{
			var forms = PokemonSpecies.GetMegaForms(baseInternalName);
			if (forms != null && forms.Count > 0)
			{
				string byFirst = GetMegaStoneIdForMegaForm(forms[0].InternalName);
				if (!string.IsNullOrEmpty(byFirst)) return byFirst;
			}
		}
		return null;
	}

	public static bool GetAvailableMegaEvolution(Farmer player, PokemonInstance mon, out PokemonSpecies targetMega, out string consumedStoneId)
	{
		targetMega = null;
		consumedStoneId = null;
		if (player == null || mon?.Species == null || mon.IsMegaEvolved || mon.Species.IsMega) return false;
		if (!IsWearingMegaBracelet(player)) return false;

		var megaForms = PokemonSpecies.GetMegaForms(mon.Species.InternalName);
		if (megaForms == null || megaForms.Count == 0) return false;

		foreach (var mf in megaForms)
		{
			string stoneId = GetMegaStoneIdForMegaForm(mf.InternalName);
			if (!string.IsNullOrEmpty(stoneId))
			{
				// Check if equipped as Held Item on the Pokémon itself, or carried in player inventory
				bool monHoldingStone = string.Equals(mon.HeldItem, stoneId, StringComparison.OrdinalIgnoreCase);
				bool playerHasStone = player.Items != null && player.Items.ContainsId("(O)" + stoneId);
				if (monHoldingStone || playerHasStone)
				{
					targetMega = mf;
					consumedStoneId = stoneId;
					return true;
				}
			}
		}
		return false;
	}

	public static string GetZCrystalIdForType(PokemonType type)
	{
		foreach (var kv in ZCrystalDefinitions)
		{
			if (kv.Value.Type == type) return kv.Key;
		}
		return "Poke_NormaliumZ";
	}

	public static bool HasZCrystalForType(Farmer player, PokemonType type, PokemonInstance mon = null)
	{
		string crystalId = GetZCrystalIdForType(type);
		if (mon != null && string.Equals(mon.HeldItem, crystalId, StringComparison.OrdinalIgnoreCase))
			return true;
		if (player?.Items == null) return false;
		return player.Items.ContainsId("(O)" + crystalId);
	}

	public static bool HasAnyMatchingZCrystal(Farmer player, PokemonInstance mon)
	{
		if (mon?.Moves == null) return false;
		if (!IsWearingZRing(player)) return false;
		foreach (var mv in mon.Moves)
		{
			if (mv != null && HasZCrystalForType(player, mv.Type, mon))
				return true;
			if (mv != null && CanUseSignatureZMove(player, mon, mv, out _, out _, out _))
				return true;
		}
		return false;
	}

	public static bool CanUseSignatureZMove(Farmer player, PokemonInstance mon, PokemonMove baseMove, out string zMoveName, out int zPower, out PokemonType zType)
	{
		zMoveName = null;
		zPower = 0;
		zType = PokemonType.Normal;
		if (player == null || mon?.Species == null || baseMove == null) return false;
		if (!IsWearingZPowerRing(player)) return false; // Signature Z-Moves require Z-Power Ring!

		string sp = mon.Species.InternalName?.ToLowerInvariant() ?? "";
		foreach (var kv in SignatureZCrystalDefinitions)
		{
			var def = kv.Value;
			if (sp.Contains(def.RequiredSpecies) && string.Equals(baseMove.Name, def.RequiredBaseMove, StringComparison.OrdinalIgnoreCase))
			{
				bool holdsCrystal = string.Equals(mon.HeldItem, kv.Key, StringComparison.OrdinalIgnoreCase);
				bool hasInInventory = player.Items != null && player.Items.ContainsId("(O)" + kv.Key);
				if (holdsCrystal || hasInInventory)
				{
					zMoveName = def.ZMoveName;
					zPower = def.Power;
					zType = def.Type;
					return true;
				}
			}
		}
		return false;
	}

	public static bool CanUseUltraBurst(Farmer player, PokemonInstance mon)
	{
		if (player == null || mon?.Species == null || mon.IsMegaEvolved) return false;
		if (!IsWearingZPowerRing(player)) return false;
		string sp = mon.Species.InternalName?.ToLowerInvariant() ?? "";
		if (!sp.Contains("necrozma")) return false;
		return string.Equals(mon.HeldItem, "Poke_UltranecroziumZ", StringComparison.OrdinalIgnoreCase) ||
		       (player.Items != null && player.Items.ContainsId("(O)Poke_UltranecroziumZ"));
	}

	public static bool CanTerastallize(Farmer player, PokemonInstance mon)
	{
		if (player == null || mon == null || mon.IsFainted || mon.IsTerastallized) return false;
		return IsWearingTeraOrb(player);
	}

	private static void RegisterSimple(IDictionary<string, ObjectData> objectData, string id, string name, string desc, int price, int spriteIndex)
	{
		if (objectData.ContainsKey(id)) return;
		objectData[id] = new ObjectData
		{
			Name = id,
			DisplayName = name,
			Description = desc,
			Type = "Basic",
			Category = StardewValley.Object.CraftingCategory,
			Price = price,
			SpriteIndex = spriteIndex,
			Edibility = -300
		};
	}

	/// <summary>Registers the Super Rod as a REAL fishing rod tool (held and cast exactly like the vanilla rods).</summary>
	public static void RegisterTools(IDictionary<string, StardewValley.GameData.Tools.ToolData> toolData)
	{
		if (toolData == null || toolData.ContainsKey("PokemonSuperRod")) return;
		StardewValley.GameData.Tools.ToolData src = null;
		toolData.TryGetValue("IridiumRod", out src);
		toolData["PokemonSuperRod"] = new StardewValley.GameData.Tools.ToolData
		{
			ClassName = "FishingRod",
			Name = "PokemonSuperRod",
			DisplayName = "Super Rod",
			Description = "Uma vara lendária. Lance na água e fisgue Pokémon aquáticos selvagens — funciona como a vara de pesca normal.",
			Texture = src?.Texture ?? "TileSheets\\Tools",
			SpriteIndex = src?.SpriteIndex ?? 0,
			MenuSpriteIndex = src?.MenuSpriteIndex ?? -1,
			UpgradeLevel = src?.UpgradeLevel ?? 3,
			AttachmentSlots = src?.AttachmentSlots ?? 2,
			SalePrice = 2500,
			CanBeLostOnDeath = false
		};
	}

	private static void RegisterBall(IDictionary<string, ObjectData> objectData, string id, string name, string desc, int spriteIndex, int price)
	{
		if (!objectData.ContainsKey(id))
		{
			objectData[id] = new ObjectData
			{
				Name = id,
				DisplayName = name,
				Description = desc,
				Type = "Crafting",
				Category = StardewValley.Object.CraftingCategory,
				Price = price,
				SpriteIndex = spriteIndex
			};
		}
	}

	public static bool IsRawApricorn(string qualifiedOrUnqualifiedId)
	{
		if (string.IsNullOrEmpty(qualifiedOrUnqualifiedId)) return false;
		string id = ItemRegistry.GetMetadata(qualifiedOrUnqualifiedId)?.LocalItemId ?? qualifiedOrUnqualifiedId;
		return id.StartsWith("Poke", StringComparison.OrdinalIgnoreCase) && id.EndsWith("Apricorn", StringComparison.OrdinalIgnoreCase);
	}

	public static string GetCookedApricornId(string rawApricornId)
	{
		string id = ItemRegistry.GetMetadata(rawApricornId)?.LocalItemId ?? rawApricornId;
		if (id.StartsWith("Poke", StringComparison.OrdinalIgnoreCase))
		{
			return "(O)Cooked" + id.Substring(4);
		}
		return "(O)CookedRedApricorn";
	}

	public static void EnsurePlayerKnowsRecipes(Farmer player)
	{
		if (player?.craftingRecipes == null) return;

		_failedLookups.Clear();
		string[] recipes = new[]
		{
			"PokeBall", "GreatBall", "UltraBall", "PremierBall", "LuxuryBall", "NetBall", "NestBall",
			"DuskBall", "DiveBall", "QuickBall", "TimerBall", "RepeatBall", "MoonBall", "LoveBall",
			"LevelBall", "FriendBall", "LureBall", "FastBall", "HeavyBall", "HealBall",
			"PokemonSuperRod", "PokemonHealingMachine", "PokemonPC", "PokemonTradeTerminal", "PokemonLinkCable",
			"PokemonRareCandy", "PokemonRepel", "PokemonSuperRepel", "PokemonMaxRepel", "PokemonExpShare",
			"Poke_MegaBracelet", "Poke_ZRing", "Poke_ZPowerRing", "Poke_DynamaxBand", "Poke_TeraOrb"
		};

		foreach (string recipe in recipes)
		{
			if (!player.craftingRecipes.ContainsKey(recipe))
			{
				player.craftingRecipes.Add(recipe, 0);
			}
		}
	}

	public static void AddPierreStock(Dictionary<ISalable, ItemStockInformation> stock)
	{
		AddStockItem(stock, "PokeBall", 200);
		AddStockItem(stock, "GreatBall", 600);
		AddStockItem(stock, "PokemonPotion", 300);
		AddStockItem(stock, "PokemonSuperPotion", 700);
		AddStockItem(stock, "PokemonRevive", 1500);
		AddStockItem(stock, "PokemonSuperRod", 2500);
		AddStockItem(stock, "PokemonRepel", 350);
		AddStockItem(stock, "PokemonSuperRepel", 500);
		AddStockItem(stock, "PokemonMaxRepel", 700);
		AddStockItem(stock, "PokemonRareCandy", 15000);
		AddStockItem(stock, "PokemonExpShare", 5000);
		AddStockItem(stock, "Poke_MegaBracelet", 10000);
		AddStockItem(stock, "Poke_ZRing", 10000);
		AddStockItem(stock, "Poke_ZPowerRing", 12000);
		AddStockItem(stock, "Poke_DynamaxBand", 12000);

		foreach (string color in ApricornColors)
		{
			AddStockItem(stock, $"Poke{color}Seed", 100);
		}
	}

	public static void AddJojaStock(Dictionary<ISalable, ItemStockInformation> stock)
	{
		AddStockItem(stock, "PokeBall", 180);
		AddStockItem(stock, "GreatBall", 550);
		AddStockItem(stock, "UltraBall", 1000);
		AddStockItem(stock, "PokemonPotion", 250);
		AddStockItem(stock, "PokemonSuperPotion", 650);
		AddStockItem(stock, "PokemonRevive", 1300);
		AddStockItem(stock, "PokemonRepel", 300);
		AddStockItem(stock, "PokemonSuperRepel", 450);
		AddStockItem(stock, "PokemonMaxRepel", 650);
		AddStockItem(stock, "PokemonRareCandy", 14000);
		AddStockItem(stock, "PokemonExpShare", 4500);
		AddStockItem(stock, "Poke_MegaBracelet", 9500);
		AddStockItem(stock, "Poke_ZRing", 9500);
		AddStockItem(stock, "Poke_ZPowerRing", 11500);
		AddStockItem(stock, "Poke_DynamaxBand", 11500);

		foreach (string color in ApricornColors)
		{
			AddStockItem(stock, $"Poke{color}Seed", 90);
		}
	}

	private static void AddStockItem(Dictionary<ISalable, ItemStockInformation> stock, string id, int price)
	{
		string qId = (id == "PokemonSuperRod") ? "(T)" + id : "(O)" + id;
		Item item = ItemRegistry.Create(qId, 1, 0, allowNull: true);
		if (item != null)
		{
			stock[item] = new ItemStockInformation(price, int.MaxValue);
		}
	}

	public static void FixInventoryRods(Farmer farmer)
	{
		if (farmer?.Items == null) return;
		for (int i = 0; i < farmer.Items.Count; i++)
		{
			var item = farmer.Items[i];
			if (item != null && item.ItemId == "PokemonSuperRod" && !(item is StardewValley.Tools.FishingRod))
			{
				farmer.Items[i] = ItemRegistry.Create("(T)PokemonSuperRod");
			}
			else if (item != null && (item.ItemId == "PokemonExpShare" || item.ItemId == "Poke_MegaBracelet" || item.ItemId == "Poke_ZRing" || item.ItemId == "Poke_ZPowerRing" || item.ItemId == "Poke_DynamaxBand") && !(item is StardewValley.Objects.Ring))
			{
				farmer.Items[i] = new StardewValley.Objects.Ring(item.ItemId);
			}
		}
	}

	public static string GetIconsBasePath()
	{
		if (_iconsBasePath != null && Directory.Exists(_iconsBasePath)) return _iconsBasePath;

		string[] roots = new[]
		{
			AppDomain.CurrentDomain.BaseDirectory,
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".."),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")
		};

		foreach (var r in roots)
		{
			try
			{
				string full = Path.GetFullPath(r);
				string expCandidate = Path.Combine(full, "pokeemerald-expansion", "graphics", "items", "icons");
				if (Directory.Exists(expCandidate))
				{
					_iconsBasePath = expCandidate;
					return _iconsBasePath;
				}
				string candidate = Path.Combine(full, "pokeemerald", "graphics", "items", "icons");
				if (Directory.Exists(candidate))
				{
					_iconsBasePath = candidate;
					return _iconsBasePath;
				}
			}
			catch { }
		}

		return null;
	}

	private static string MapItemToIconFileName(string localId)
	{
		if (string.IsNullOrEmpty(localId)) return null;

		string cleanId = localId;
		if (cleanId.StartsWith("Pokemon", StringComparison.OrdinalIgnoreCase))
			cleanId = cleanId.Substring(7);
		else if (cleanId.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase))
			cleanId = cleanId.Substring(5);

		if (ZCrystalDefinitions.TryGetValue(localId, out var zDef) ||
		    ZCrystalDefinitions.TryGetValue("Poke_" + cleanId, out zDef) ||
		    ZCrystalDefinitions.TryGetValue(cleanId, out zDef))
			return zDef.IconFile;

		if (SignatureZCrystalDefinitions.TryGetValue(localId, out var sigZDef) ||
		    SignatureZCrystalDefinitions.TryGetValue("Poke_" + cleanId, out sigZDef) ||
		    SignatureZCrystalDefinitions.TryGetValue(cleanId, out sigZDef))
			return sigZDef.IconFile;

		if (MegaStoneDefinitions.TryGetValue(localId, out var mDef) ||
		    MegaStoneDefinitions.TryGetValue("Poke_" + cleanId, out mDef) ||
		    MegaStoneDefinitions.TryGetValue(cleanId, out mDef))
			return mDef.IconFile;

		string heldCandidate = cleanId switch
		{
			"Leftovers" => "leftovers.png",
			"LifeOrb" => "life_orb.png",
			"ChoiceBand" => "choice_band.png",
			"ChoiceSpecs" => "choice_specs.png",
			"ChoiceScarf" => "choice_scarf.png",
			"FocusSash" => "focus_sash.png",
			"RockyHelmet" => "rocky_helmet.png",
			"AssaultVest" => "assault_vest.png",
			"Eviolite" => "eviolite.png",
			"HeavyDutyBoots" => "heavy_duty_boots.png",
			"DestinyKnot" => "destiny_knot.png",
			"Everstone" => "everstone.png",
			"WeaknessPolicy" => "weakness_policy.png",
			"LoadedDice" => "loaded_dice.png",
			"CovertCloak" => "covert_cloak.png",
			"ClearAmulet" => "clear_amulet.png",
			"AirBalloon" => "air_balloon.png",
			"BlackSludge" => "black_sludge.png",
			"ToxicOrb" => "toxic_orb.png",
			"FlameOrb" => "flame_orb.png",
			"SitrusBerry" => "sitrus_berry.png",
			"LumBerry" => "lum_berry.png",
			"ScopeLens" => "scope_lens.png",
			"MuscleBand" => "muscle_band.png",
			"WiseGlasses" => "wise_glasses.png",
			"ShellBell" => "shell_bell.png",
			"KingsRock" => "kings_rock.png",
			"QuickClaw" => "quick_claw.png",
			"ExpertBelt" => "expert_belt.png",
			"Metronome" => "metronome.png",
			"HPUp" => "hp_up.png",
			"Protein" => "protein.png",
			"Iron" => "iron.png",
			"Calcium" => "calcium.png",
			"Zinc" => "zinc.png",
			"Carbos" => "carbos.png",
			"PPUp" => "pp_up.png",
			"PPMax" => "pp_max.png",
			"PowerWeight" => "power_weight.png",
			"PowerBracer" => "power_bracer.png",
			"PowerBelt" => "power_belt.png",
			"PowerLens" => "power_lens.png",
			"PowerBand" => "power_band.png",
			"PowerAnklet" => "power_anklet.png",
			"Charcoal" => "charcoal.png",
			"MysticWater" => "mystic_water.png",
			"MiracleSeed" => "miracle_seed.png",
			"Magnet" => "magnet.png",
			"HardStone" => "hard_stone.png",
			"SoftSand" => "soft_sand.png",
			"BlackGlasses" => "black_glasses.png",
			"SpellTag" => "spell_tag.png",
			"SilkScarf" => "silk_scarf.png",
			"SharpBeak" => "sharp_beak.png",
			"PoisonBarb" => "poison_barb.png",
			"NeverMeltIce" => "never_melt_ice.png",
			"DragonFang" => "dragon_fang.png",
			"TwistedSpoon" => "twisted_spoon.png",
			"BlackBelt" => "black_belt.png",
			"SilverPowder" => "silver_powder.png",
			"MetalCoat" => "metal_coat.png",
			_ => null
		};
		if (heldCandidate != null) return heldCandidate;

		return cleanId switch
		{
			"PokeBall" => "poke_ball.png",
			"GreatBall" => "great_ball.png",
			"UltraBall" => "ultra_ball.png",
			"PremierBall" => "premier_ball.png",
			"LuxuryBall" => "luxury_ball.png",
			"NetBall" => "net_ball.png",
			"NestBall" => "nest_ball.png",
			"DuskBall" => "dusk_ball.png",
			"DiveBall" => "dive_ball.png",
			"QuickBall" => "quick_ball.png",
			"TimerBall" => "timer_ball.png",
			"RepeatBall" => "repeat_ball.png",
			"MoonBall" => "moon_ball.png",
			"LoveBall" => "love_ball.png",
			"LevelBall" => "level_ball.png",
			"FriendBall" => "friend_ball.png",
			"LureBall" => "lure_ball.png",
			"FastBall" => "fast_ball.png",
			"HeavyBall" => "heavy_ball.png",
			"HealBall" => "heal_ball.png",
			"MasterBall" => "master_ball.png",

			"PokemonFriendshipCandy" or "FriendshipCandy" => "rare_candy.png",
			"PokemonRareCandy" or "RareCandy" => "rare_candy.png",
			"PokemonRepel" or "Repel" or "PokemonSuperRepel" or "SuperRepel" or "PokemonMaxRepel" or "MaxRepel" => "repel.png",
			"PokemonExpShare" or "ExpShare" => "exp_share.png",

			"PokemonPotion" or "Potion" => "potion.png",
			"PokemonSuperPotion" or "SuperPotion" or "PokemonHyperPotion" or "HyperPotion" => "large_potion.png",
			"PokemonRevive" or "Revive" => "revive.png",
			"PokemonMaxRevive" or "MaxRevive" => "max_revive.png",
			"PokemonSuperRod" or "SuperRod" => "super_rod.png",
			"PokemonOldRod" or "OldRod" => "old_rod.png",
			"PokemonGoodRod" or "GoodRod" => "good_rod.png",

			"Poke_TeraOrb" or "TeraOrb" => "tera_orb.png",
			_ when cleanId.Contains("Stellar") => "stellar_tera_shard.png",
			_ when cleanId.StartsWith("TeraShard_") || localId.Contains("TeraShard_") => "tera_shard.png",

			"Poke_FreezingFeather" or "FreezingFeather" => "never_melt_ice.png",
			"Poke_ElectricFeather" or "ElectricFeather" => "magnet.png",
			"Poke_FlameFeather" or "FlameFeather" => "charcoal.png",
			"Poke_BerserkGene" or "BerserkGene" => "twisted_spoon.png",
			"Poke_OldSeaMap" or "OldSeaMap" => "sea_incense.png",
			"Poke_ClearBell" or "ClearBell" => "soothe_bell.png",
			"Poke_SilverWing" or "SilverWing" => "silver_powder.png",
			"Poke_RainbowWing" or "RainbowWing" => "rainbow_wing.png",
			"Poke_BlueOrb" or "BlueOrb" => "blue_orb.png",
			"Poke_RedOrb" or "RedOrb" => "red_orb.png",
			"Poke_JadeOrb" or "JadeOrb" => "jade_orb.png",
			"Poke_AdamantOrb" or "AdamantOrb" => "adamant_orb.png",
			"Poke_LustrousOrb" or "LustrousOrb" => "lustrous_orb.png",
			"Poke_GriseousCore" or "GriseousCore" => "griseous_orb.png",
			"Poke_IndigoDisk" or "IndigoDisk" => "up_grade.png",

			"OranBerry" or "Poke_OranBerry" => "oran_berry.png",
			"SitrusBerry" or "Poke_SitrusBerry" => "sitrus_berry.png",
			"LumBerry" or "Poke_LumBerry" => "lum_berry.png",
			"CheriBerry" or "Poke_CheriBerry" => "cheri_berry.png",
			"ChestoBerry" or "Poke_ChestoBerry" => "chesto_berry.png",
			"PechaBerry" or "Poke_PechaBerry" => "pecha_berry.png",
			"RawstBerry" or "Poke_RawstBerry" => "rawst_berry.png",
			"AspearBerry" or "Poke_AspearBerry" => "aspear_berry.png",
			"LeppaBerry" or "Poke_LeppaBerry" => "leppa_berry.png",
			"PersimBerry" or "Poke_PersimBerry" => "persim_berry.png",

			"Poke_MegaBracelet" or "MegaBracelet" => "mega_bracelet.png",
			"Poke_ZRing" or "ZRing" => "z_ring.png",
			"Poke_ZPowerRing" or "ZPowerRing" => "z_power_ring.png",
			"Poke_DynamaxBand" or "DynamaxBand" => "dynamax_band.png",

			_ when cleanId.StartsWith("TM_") || localId.Contains("TM_") => "tm.png",
			_ when cleanId.StartsWith("HM_") || localId.Contains("HM_") => "hm.png",

			"PokeRedApricorn" or "RedApricorn" or "CookedRedApricorn" => "red_apricorn.png",
			"PokeBlueApricorn" or "BlueApricorn" or "CookedBlueApricorn" => "blue_apricorn.png",
			"PokeYellowApricorn" or "YellowApricorn" or "CookedYellowApricorn" => "yellow_apricorn.png",
			"PokeGreenApricorn" or "GreenApricorn" or "CookedGreenApricorn" => "green_apricorn.png",
			"PokeBlackApricorn" or "BlackApricorn" or "CookedBlackApricorn" => "black_apricorn.png",
			"PokeWhiteApricorn" or "WhiteApricorn" or "CookedWhiteApricorn" => "white_apricorn.png",
			"PokePinkApricorn" or "PinkApricorn" or "CookedPinkApricorn" => "pink_apricorn.png",

			"PokemonHealingMachine" or "HealingMachine" => "full_heal.png",
			"PokemonPC" or "PC" => "pokemon_box_link.png",
			"PokemonTradeTerminal" or "TradeTerminal" => "trade_terminal.png",
			"PokemonLinkCable" or "LinkCable" => "linking_cord.png",

			"PokemonAntidote" or "Antidote" => "antidote.png",
			"PokemonParalyzeHeal" or "ParalyzeHeal" => "paralyze_heal.png",
			"PokemonAwakening" or "Awakening" => "awakening.png",
			"PokemonBurnHeal" or "BurnHeal" => "burn_heal.png",
			"PokemonIceHeal" or "IceHeal" => "ice_heal.png",
			"PokemonFullHeal" or "FullHeal" => "full_heal.png",
			"PokemonMaxPotion" or "MaxPotion" => "max_potion.png",

			"PokemonWaterStone" or "WaterStone" => "water_stone.png",
			"PokemonFireStone" or "FireStone" => "fire_stone.png",
			"PokemonThunderStone" or "ThunderStone" => "thunder_stone.png",
			"PokemonLeafStone" or "LeafStone" => "leaf_stone.png",
			"PokemonMoonStone" or "MoonStone" => "moon_stone.png",
			"PokemonSunStone" or "SunStone" => "sun_stone.png",
			"PokemonShinyStone" or "ShinyStone" => "shiny_stone.png",
			"PokemonDuskStone" or "DuskStone" => "dusk_stone.png",
			"PokemonDawnStone" or "DawnStone" => "dawn_stone.png",
			"PokemonIceStone" or "IceStone" => "ice_stone.png",
			"PokemonOvalStone" or "OvalStone" => "oval_stone.png",

			"PokeRedSeed" or "RedApricornSeed" or "RedSeed" => "red_apricorn_seed.png",
			"PokeBlueSeed" or "BlueApricornSeed" or "BlueSeed" => "blue_apricorn_seed.png",
			"PokeYellowSeed" or "YellowApricornSeed" or "YellowSeed" => "yellow_apricorn_seed.png",
			"PokeGreenSeed" or "GreenApricornSeed" or "GreenSeed" => "green_apricorn_seed.png",
			"PokeBlackSeed" or "BlackApricornSeed" or "BlackSeed" => "black_apricorn_seed.png",
			"PokeWhiteSeed" or "WhiteApricornSeed" or "WhiteSeed" => "white_apricorn_seed.png",
			"PokePinkSeed" or "PinkApricornSeed" or "PinkSeed" => "pink_apricorn_seed.png",

			_ when cleanId.EndsWith("Seed") || cleanId.EndsWith("Sapling") => "apricorn.png",
			_ => null
		};
	}

	private static readonly HashSet<string> _failedLookups = new(StringComparer.OrdinalIgnoreCase);

	public static bool TryGetCustomTexture(string qualifiedOrLocalId, out Texture2D texture, out Rectangle sourceRect)
	{
		texture = null;
		sourceRect = Rectangle.Empty;

		if (string.IsNullOrEmpty(qualifiedOrLocalId)) return false;

		string localId = ItemRegistry.GetMetadata(qualifiedOrLocalId)?.LocalItemId ?? qualifiedOrLocalId;
		if (localId.StartsWith("(O)") || localId.StartsWith("(T)")) localId = localId.Substring(3);
		else if (localId.StartsWith("(BC)")) localId = localId.Substring(4);

		if (_failedLookups.Contains(localId)) return false;

		string fileName = MapItemToIconFileName(localId);
		if (fileName == null)
		{
			_failedLookups.Add(localId);
			return false;
		}

		if (_itemTextures.TryGetValue(localId, out var cachedTex) && cachedTex != null && !cachedTex.IsDisposed)
		{
			texture = cachedTex;
			sourceRect = new Rectangle(0, 0, cachedTex.Width, cachedTex.Height);
			return true;
		}

		string[] searchFolders = new[]
		{
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Pokemon"),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald-expansion", "graphics", "items", "icons"),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald", "graphics", "items", "icons"),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeplatinum", "res", "items", "icons")
		};

		string fullPath = null;
		foreach (var folder in searchFolders)
		{
			try
			{
				if (!Directory.Exists(folder)) continue;
				string cand = Path.Combine(folder, fileName);
				if (File.Exists(cand))
				{
					fullPath = cand;
					break;
				}
				if (fileName == "super_potion.png")
				{
					string fallback = Path.Combine(folder, "large_potion.png");
					if (File.Exists(fallback))
					{
						fullPath = fallback;
						break;
					}
				}
			}
			catch { }
		}

		if (fullPath == null || !File.Exists(fullPath))
		{
			_failedLookups.Add(localId);
			return false;
		}

		try
		{
			Texture2D loaded = PokemonUI.LoadColorKeyed(fullPath);
			if (loaded == null) return false;
			loaded = NormalizeIconCanvas(loaded);
			_itemTextures[localId] = loaded;
			texture = loaded;
			sourceRect = new Rectangle(0, 0, loaded.Width, loaded.Height);
			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Recenters the opaque pixels of any item icon onto a consistent 24x24 canvas
	/// so all evolution stones and items share the exact same visual center and baseline in inventory and when held.
	/// </summary>
	private static Texture2D NormalizeIconCanvas(Texture2D src)
	{
		if (src == null || Game1.graphics?.GraphicsDevice == null) return src;

		try
		{
			int w = src.Width;
			int h = src.Height;
			Color[] data = new Color[w * h];
			src.GetData(data);

			int minX = w, minY = h, maxX = -1, maxY = -1;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					if (data[y * w + x].A > 10)
					{
						if (x < minX) minX = x;
						if (x > maxX) maxX = x;
						if (y < minY) minY = y;
						if (y > maxY) maxY = y;
					}
				}
			}

			if (maxX < minX || maxY < minY) return src;

			int contentW = maxX - minX + 1;
			int contentH = maxY - minY + 1;
			int targetSize = Math.Max(24, Math.Max(contentW, contentH));
			Color[] dstData = new Color[targetSize * targetSize];

			int offsetX = (targetSize - contentW) / 2;
			int offsetY = (targetSize - contentH) / 2;

			for (int cy = 0; cy < contentH; cy++)
			{
				for (int cx = 0; cx < contentW; cx++)
				{
					dstData[(offsetY + cy) * targetSize + (offsetX + cx)] = data[(minY + cy) * w + (minX + cx)];
				}
			}

			Texture2D normalized = new Texture2D(Game1.graphics.GraphicsDevice, targetSize, targetSize);
			normalized.SetData(dstData);
			return normalized;
		}
		catch
		{
			return src;
		}
	}

	private static Texture2D _itemBallTexture;

	public static Texture2D GetItemBallTexture()
	{
		if (_itemBallTexture != null && !_itemBallTexture.IsDisposed) return _itemBallTexture;

		string[] candidates = new[]
		{
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald", "graphics", "object_events", "pics", "misc", "item_ball.png"),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokefirered", "graphics", "object_events", "pics", "misc", "item_ball.png")
		};

		foreach (var path in candidates)
		{
			if (File.Exists(path))
			{
				try
				{
					_itemBallTexture = PokemonUI.LoadColorKeyed(path);
					if (_itemBallTexture != null) return _itemBallTexture;
				}
				catch { }
			}
		}

		return null;
	}

	public static bool IsPokeBall(string qualifiedOrLocalId)
	{
		if (string.IsNullOrEmpty(qualifiedOrLocalId)) return false;
		string id = ItemRegistry.GetMetadata(qualifiedOrLocalId)?.LocalItemId ?? qualifiedOrLocalId;
		if (id.StartsWith("(O)")) id = id.Substring(3);
		return id switch
		{
			"PokeBall" or "GreatBall" or "UltraBall" or "MasterBall" or "PremierBall" or "FastBall" or "HeavyBall" or "HealBall" => true,
			_ => false
		};
	}
}
