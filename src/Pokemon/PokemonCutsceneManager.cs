using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

/// <summary>
/// Cinematic orchestrator for Legendary awakenings, boss battle intros, and milestone cutscenes.
/// Integrates authentic GBA/FRLG audio, rumbling, letterboxing, and dynamic atmospheric flares.
/// </summary>
public static class PokemonCutsceneManager
{
	private static bool _isAwakeningActive = false;
	private static float _awakeningTimer = 0f;
	private static float _awakeningDuration = 2.8f;
	private static PokemonSpecies _awakeningSpecies;
	private static string _awakeningRelicName;
	private static GameLocation _awakeningLocation;
	private static Point _awakeningTile;
	private static Action _awakeningOnFinished;
	private static Color _awakeningAuraColor = Color.Gold;
	private static string _awakeningTitle = "O DESPERTAR DO TITÃ ANCESTRAL";
	private static readonly List<Vector2> _awakeningParticles = new();

	public static bool IsCutsceneActive => _isAwakeningActive;

	public static void PlayLegendaryAwakening(GameLocation loc, Point tile, PokemonSpecies species, string relicName, Action onFinished)
	{
		if (species == null)
		{
			onFinished?.Invoke();
			return;
		}

		_isAwakeningActive = true;
		_awakeningTimer = 0f;
		_awakeningDuration = 2.8f;
		_awakeningSpecies = species;
		_awakeningRelicName = relicName ?? "Relíquia Ancestral";
		_awakeningLocation = loc;
		_awakeningTile = tile;
		_awakeningOnFinished = onFinished;
		_awakeningAuraColor = DetermineElementalColor(species);
		_awakeningTitle = DetermineTitle(species);

		// Freeze player movement for cinematic presentation
		if (Game1.player != null)
		{
			Game1.player.Halt();
			Game1.player.CanMove = false;
		}

		// Initial rumble & auditory cue
		Rumble.rumble(0.85f, 600f);
		Game1.playSound("wand");
		Game1.screenGlowOnce(_awakeningAuraColor, hold: false);

		// Create sparkle particle offsets
		_awakeningParticles.Clear();
		for (int i = 0; i < 28; i++)
		{
			float angle = (float)(Game1.random.NextDouble() * Math.PI * 2.0);
			float dist = Game1.random.Next(20, 180);
			_awakeningParticles.Add(new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist));
		}
	}

	public static void Update(GameTime time)
	{
		if (!_isAwakeningActive) return;

		float dt = (float)time.ElapsedGameTime.TotalSeconds;
		float prevTimer = _awakeningTimer;
		_awakeningTimer += dt;

		// 1. Climax at 0.8s: Thunder, ancestral Roar, Cry, screen shake
		if (prevTimer < 0.8f && _awakeningTimer >= 0.8f)
		{
			Game1.playSound("thunder");
			Game1.playSound("roar");
			Rumble.rumble(1.0f, 900f);
			Game1.screenGlowOnce(_awakeningAuraColor * 1.5f, hold: false);

			if (_awakeningSpecies != null)
			{
				PokemonCryManager.PlayCry(_awakeningSpecies.Name);
			}

			// Spawn light pillar particles at summon tile in overworld
			if (_awakeningLocation != null)
			{
				Vector2 tileWorldPos = new Vector2(_awakeningTile.X * 64f + 32f, _awakeningTile.Y * 64f + 32f);
				for (int i = 0; i < 16; i++)
				{
					_awakeningLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
						"TileSheets\\animations",
						new Rectangle(0, 0, 64, 64),
						80f, 8, 0,
						tileWorldPos + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)),
						flicker: false,
						Game1.random.NextDouble() < 0.5,
						0.001f, 0.01f,
						_awakeningAuraColor,
						1.5f, 0.005f, 0f, 0f)
					{
						motion = new Vector2((float)(Game1.random.NextDouble() - 0.5) * 4f, -4f - (float)Game1.random.NextDouble() * 3f)
					});
				}
			}
		}

		// 2. Cutscene completion at duration
		if (_awakeningTimer >= _awakeningDuration)
		{
			_isAwakeningActive = false;
			if (Game1.player != null)
			{
				Game1.player.CanMove = true;
			}
			Game1.playSound("reward");
			_awakeningOnFinished?.Invoke();
			_awakeningOnFinished = null;
		}
	}

	public static void DrawOverworldOverlay(SpriteBatch b)
	{
		if (!_isAwakeningActive) return;

		int sw = Game1.uiViewport.Width;
		int sh = Game1.uiViewport.Height;

		float progress = Math.Clamp(_awakeningTimer / _awakeningDuration, 0f, 1f);

		// 1. Cinematic Widescreen Letterbox Bars (Top & Bottom)
		int maxBarHeight = 76;
		int curBarHeight;
		if (progress < 0.15f)
		{
			curBarHeight = (int)MathHelper.Lerp(0, maxBarHeight, progress / 0.15f);
		}
		else if (progress > 0.85f)
		{
			curBarHeight = (int)MathHelper.Lerp(maxBarHeight, 0, (progress - 0.85f) / 0.15f);
		}
		else
		{
			curBarHeight = maxBarHeight;
		}

		b.Draw(Game1.staminaRect, new Rectangle(0, 0, sw, curBarHeight), Color.Black);
		b.Draw(Game1.staminaRect, new Rectangle(0, sh - curBarHeight, sw, curBarHeight), Color.Black);

		// Gold border lines for letterbox
		b.Draw(Game1.staminaRect, new Rectangle(0, curBarHeight - 2, sw, 2), _awakeningAuraColor * 0.8f);
		b.Draw(Game1.staminaRect, new Rectangle(0, sh - curBarHeight, sw, 2), _awakeningAuraColor * 0.8f);

		// 2. Ambient Screen Tint & Flashes
		if (_awakeningTimer >= 0.8f && _awakeningTimer < 1.05f)
		{
			float flashAlpha = 1.0f - ((_awakeningTimer - 0.8f) / 0.25f);
			b.Draw(Game1.staminaRect, new Rectangle(0, 0, sw, sh), Color.White * flashAlpha * 0.85f);
		}
		else
		{
			float darkAlpha = 0.35f;
			if (progress < 0.15f) darkAlpha = (progress / 0.15f) * 0.35f;
			else if (progress > 0.85f) darkAlpha = (1f - (progress - 0.85f) / 0.15f) * 0.35f;
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, sw, sh), Color.Black * darkAlpha);
		}

		// 3. Overworld Aura Particles (Floating around player without obstructive central box)
		if (_awakeningTimer >= 0.5f)
		{
			float auraAlpha = Math.Clamp((_awakeningTimer - 0.5f) / 0.4f, 0f, 1f);
			if (_awakeningTimer > 2.2f) auraAlpha = Math.Clamp(1f - ((_awakeningTimer - 2.2f) / 0.6f), 0f, 1f);

			Vector2 center = new Vector2(sw / 2f, sh / 2f);
			for (int i = 0; i < _awakeningParticles.Count; i++)
			{
				Vector2 pPos = center + _awakeningParticles[i] * (1.0f + (float)Math.Sin(_awakeningTimer * 4.0 + i) * 0.25f);
				b.Draw(Game1.staminaRect, new Rectangle((int)pPos.X - 3, (int)pPos.Y - 3, 6, 6), _awakeningAuraColor * auraAlpha * 0.85f);
			}
		}
	}

	public static Color DetermineElementalColor(PokemonSpecies spec)
	{
		if (spec == null) return Color.Gold;
		string name = spec.Name;

		return name switch
		{
			"Moltres" or "Entei" or "Ho-Oh" or "Groudon" or "Heatran" or "Reshiram" or "Volcanion" or "Gouging Fire" or "Chi-Yu"
				=> new Color(255, 90, 40),

			"Zapdos" or "Raikou" or "Regieleki" or "Thundurus" or "Zekrom" or "Tapu Koko" or "Zeraora" or "Raging Bolt" or "Miraidon"
				=> new Color(255, 225, 45),

			"Articuno" or "Suicune" or "Lugia" or "Kyogre" or "Regice" or "Phione" or "Manaphy" or "Glastrier" or "Chien-Pao" or "Walking Wake"
				=> new Color(60, 190, 255),

			"Mew" or "Mewtwo" or "Celebi" or "Jirachi" or "Deoxys" or "Uxie" or "Mesprit" or "Azelf" or "Cresselia" or "Solgaleo" or "Lunala" or "Necrozma" or "Pecharunt" or "Terapagos"
				=> new Color(215, 95, 255),

			"Rayquaza" or "Dialga" or "Palkia" or "Giratina" or "Arceus" or "Kyurem" or "Zygarde" or "Eternatus" or "Koraidon"
				=> new Color(255, 185, 30),

			"Darkrai" or "Yveltal" or "Marshadow" or "Spectrier" or "Calyrex" or "Ting-Lu" or "Wo-Chien"
				=> new Color(130, 45, 185),

			_ => new Color(255, 215, 65)
		};
	}

	private static string DetermineTitle(PokemonSpecies spec)
	{
		if (spec == null) return "O DESPERTAR DO TITÃ ANCESTRAL";
		string name = spec.Name;

		return name switch
		{
			"Arceus" => "O CRIADOR DO UNIVERSO DESPERTOU",
			"Giratina" => "O REGENTE DO MUNDO DA DISTORÇÃO",
			"Dialga" => "O SOBERANO DO TEMPO EMERGIU",
			"Palkia" => "O SOBERANO DO ESPAÇO EMERGIU",
			"Rayquaza" => "O GUARDIÃO DOS CÉUS DESCENDEU",
			"Groudon" => "O TITÃ DOS CONTINENTES DESPERTOU",
			"Kyogre" => "O TITÃ DAS PROFUNDEZAS OCEÂNICAS",
			"Lugia" => "O GUARDIÃO DAS MARÉS RESSOOU",
			"Ho-Oh" => "A AVE ARCO-ÍRIS DA RESSURREIÇÃO",
			"Mewtwo" => "A ENTIDADE GENÉTICA DEFINITIVA",
			"Mew" => "A MATRIZ DA VIDA POKÉMON",
			"Deoxys" => "A ABERRAÇÃO CÓSMICA MUTANTE",
			"Eternatus" => "O CATACLISMO DO GIGANTAMAX",
			"Terapagos" => "O NÚCLEO DO DISCO ÍNDIGO",
			"Pecharunt" => "O SENHOR DO MOCHI VENENOSO",
			_ => "O DESPERTAR DO TITÃ ANCESTRAL"
		};
	}

	public static string GetCustomSummonMessage(string playerName, string name)
	{
		string p = playerName ?? "Alguém";
		string n = name ?? "Lendário";
		return n switch
		{
			"Mewtwo" => $"✦ {p} rompeu os selos psíquicos! Mewtwo despertou com fúria telecinética!",
			"Mew" => $"✦ {p} tocou uma flauta suave... Mew surgiu entre as pétalas flutuantes!",
			"Articuno" => $"✦ {p} ergueu a pena congelada! O bater de asas gélido de Articuno sopra no vale!",
			"Zapdos" => $"✦ {p} canalizou alta voltagem! Trovões estrondosos anunciam a chegada de Zapdos!",
			"Moltres" => $"✦ {p} acendeu as cinzas sagradas! Labaredas escarlates cobrem os céus com Moltres!",
			"Raikou" => $"✦ {p} ouviu o trovão rugir! Raikou corre veloz como uma tempestade de raios!",
			"Entei" => $"✦ {p} atiçou as brasas vulcânicas! O rugido de magma de Entei ecoa pela terra!",
			"Suicune" => $"✦ {p} purificou as correntes d'água! As brisas serenas de Suicune tocam as águas!",
			"Lugia" => $"✦ {p} tocou a Silver Feather! O Guardião dos Mares, Lugia, emergiu das profundezas!",
			"Ho-Oh" => $"✦ {p} ergueu a Rainbow Wing! O arco-íris eterno resplandece com as asas de Ho-Oh!",
			"Celebi" => $"✦ {p} tocou o altar antigo! Ondulações no próprio tempo revelam Celebi!",
			"Kyogre" => $"✦ {p} ergueu a Blue Orb! Chuvas torrenciais inundam a terra com a fúria de Kyogre!",
			"Groudon" => $"✦ {p} ergueu a Red Orb! O magma racha o chão sob os passos de Groudon!",
			"Rayquaza" => $"✦ {p} libertou o meteorito! O soberano dos céus, Rayquaza, desce da estratosfera!",
			"Jirachi" => $"✦ {p} encontrou a estrela cadente! O realizador de desejos, Jirachi, despertou!",
			"Deoxys" => $"✦ {p} decodificou o sinal alienígena! A forma de Deoxys se materializou!",
			"Dialga" => $"✦ {p} ativou a Adamant Orb! As engrenagens do Tempo pararam sob o olhar de Dialga!",
			"Palkia" => $"✦ {p} ativou a Lustrous Orb! As fronteiras do Espaço se curvam diante de Palkia!",
			"Giratina" => $"✦ {p} abriu o portal da Griseous Orb! As sombras do Distortion World trazem Giratina!",
			"Darkrai" => $"✦ {p} tocou a pena do pesadelo! Sombras eternas engolem a luz com Darkrai!",
			"Cresselia" => $"✦ {p} ergueu a Lunar Feather! A luz prateada dos sonhos desce com Cresselia!",
			"Reshiram" => $"✦ {p} acendeu a Light Stone! As chamas da Verdade de Reshiram iluminam o mundo!",
			"Zekrom" => $"✦ {p} canalizou a Dark Stone! Os raios dos Ideais de Zekrom estalam nos céus!",
			"Kyurem" => $"✦ {p} utilizou o DNA Splicers! O frio absoluto de Kyurem congela o ambiente!",
			"Xerneas" => $"✦ {p} tocou a raiz sagrada! A árvore da Vida renasce com os chifres de Xerneas!",
			"Yveltal" => $"✦ {p} despertou o casulo da destruição! As asas da Morte de Yveltal cobrem o horizonte!",
			"Zygarde" => $"✦ {p} reuniu os núcleos de Zygarde! A ordem do ecossistema se manifesta!",
			"Solgaleo" => $"✦ {p} canalizou a luz solar! O leão Solgaleo ruge com o brilho de supernovas!",
			"Lunala" => $"✦ {p} ergueu o espelho lunar! A emissária da lua, Lunala, abre suas asas astrais!",
			"Necrozma" => $"✦ {p} libertou o devorador de luz! O prisma sombrio de Necrozma absorve a luminosidade!",
			"Zacian" => $"✦ {p} cravou a Rusted Sword! A espada lendária de Zacian corta o próprio ar!",
			"Zamazenta" => $"✦ {p} empunhou o Rusted Shield! O escudo inabalável de Zamazenta defende a terra!",
			"Eternatus" => $"✦ {p} tocou o núcleo Dynamax! O dragão alienígena Eternatus escurece a estratosfera!",
			"Koraidon" => $"✦ {p} abriu a fenda do passado ancestral! O rei escarlate Koraidon ruge com poder primordial!",
			"Miraidon" => $"✦ {p} sintonizou a frequência do futuro! O dragão de ferro Miraidon acelera com pura energia!",
			"Terapagos" => $"✦ {p} revelou a coroa estelar! A tartaruga celestial Terapagos brilha com o poder Tera!",
			"Arceus" => $"✦ {p} reuniu as placas cósmicas! O Criador do Universo, Arceus, desceu ao Vale!",
			_ => $"✦ {p} realizou o ritual ancestral e invocou {n}!"
		};
	}

	public static string GetCustomWildSpawnMessage(string name)
	{
		string n = name ?? "Lendário";
		return n switch
		{
			"Mewtwo" => "✦ Uma tempestade psíquica distorce o ar em algum lugar isolado do vale...",
			"Mew" => "✦ Um brilho rosado e brincalhão foi avistado cruzando as árvores do vale...",
			"Articuno" => "✦ Uma nevasca repentina e cortante se formou em algum pico montanhoso do vale...",
			"Zapdos" => "✦ Relâmpagos dourados rasgam os céus nublados de alguma área do vale...",
			"Moltres" => "✦ Um calor sufocante e brasas incandescentes surgem em algum canto remoto do vale...",
			"Raikou" => "✦ O rugido de um trovão terrestre ecoou pelas terras do vale...",
			"Entei" => "✦ A terra treme como se um vulcão estivesse despertando em algum lugar do vale...",
			"Suicune" => "✦ As águas de um lago distante tornaram-se puras e brilhantes como cristal...",
			"Lugia" => "✦ Grandes ondas e uma canção marinha ecoam de algum abismo aquático do vale...",
			"Ho-Oh" => "✦ Um arco-íris radiante se projeta no céu em algum lugar sagrado do vale...",
			"Celebi" => "✦ Uma brisa com perfume de séculos passados viajou por algum bosque do vale...",
			"Kyogre" => "✦ Nuvens densas e uma chuva sem fim começam a cair em alguma depressão do vale...",
			"Groudon" => "✦ O solo ferve e o calor atinge níveis extremos em alguma fenda do vale...",
			"Rayquaza" => "✦ Uma serpente esmeralda cortou a alta atmosfera sobre algum cume do vale...",
			"Jirachi" => "✦ Uma estrela cadente tocou a terra suavemente em algum recanto do vale...",
			"Deoxys" => "✦ Uma estranha aurora cósmica pulsou em padrões digitais em algum local do vale...",
			"Dialga" => "✦ O fluxo dos segundos parece desacelerar estranhamente em algum ponto do vale...",
			"Palkia" => "✦ Fendas dimensionais misteriosas distorcem a gravidade em algum lugar do vale...",
			"Giratina" => "✦ A sombra do vale escureceu e a gravidade parece falhar em algum abismo...",
			"Darkrai" => "✦ Pesadelos profundos e névoa escura se acumulam em algum recanto escuro do vale...",
			"Cresselia" => "✦ Uma aura de luar puro e sonhos tranquilos ilumina um ponto distante do vale...",
			"Reshiram" => "✦ Labaredas brancas e puras foram vistas queimando os céus em algum lugar do vale...",
			"Zekrom" => "✦ Trovões negros e azulados eletrizam o horizonte em algum ponto ermo do vale...",
			"Kyurem" => "✦ Uma camada de gelo seco e ar estático congelou tudo ao redor em algum ponto do vale...",
			"Xerneas" => "✦ Flores silvestres desabrocham instantaneamente em um bosque secreto do vale...",
			"Yveltal" => "✦ As folhas secam e a vitalidade esvai-se misteriosamente em alguma clareira do vale...",
			"Zygarde" => "✦ O equilíbrio natural do ecossistema foi abalado em alguma caverna do vale...",
			"Solgaleo" => "✦ Uma luz dourada tão intensa quanto o sol do meio-dia surgiu em algum pico do vale...",
			"Lunala" => "✦ Uma noite sem estrelas e asas translúcidas cobriram o céu em algum recanto do vale...",
			"Necrozma" => "✦ Toda a luz ambiente foi repentinamente drenada em algum local desolado do vale...",
			"Zacian" => "✦ O som de uma lâmina celestial cortando o vento ecoou por alguma campina do vale...",
			"Zamazenta" => "✦ Um impacto titânico de defesa fez a terra vibrar em algum planalto do vale...",
			"Eternatus" => "✦ Um feixe de energia púrpura e instável disparou em direção aos céus em algum lugar do vale...",
			"Koraidon" => "✦ Pegadas titânicas pré-históricas foram sentidas abalando o solo em algum local do vale...",
			"Miraidon" => "✦ Zumbidos de plasma e circuitos futuristas foram ouvidos em alguma área isolada do vale...",
			"Terapagos" => "✦ Cristais cintilantes multicoloridos brotaram do chão em alguma ravina do vale...",
			"Arceus" => "✦ Um silêncio cósmico e solene pairou sobre todo o Vale...",
			_ => $"✦ Um {n} ancestral surgiu em algum lugar do vale..."
		};
	}

}
