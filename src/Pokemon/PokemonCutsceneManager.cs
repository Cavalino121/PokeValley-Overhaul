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

		// 3. Central Awakening Title Card Banner (visible between 0.8s and 2.7s)
		if (_awakeningTimer >= 0.8f)
		{
			float bannerT = Math.Clamp((_awakeningTimer - 0.8f) / 0.3f, 0f, 1f);
			float fadeOutT = (_awakeningTimer > 2.4f) ? Math.Clamp(1f - ((_awakeningTimer - 2.4f) / 0.4f), 0f, 1f) : 1f;
			float bannerAlpha = bannerT * fadeOutT;

			int bannerW = Math.Min(780, sw - 64);
			int bannerH = 136;
			int bannerX = (sw - bannerW) / 2;
			int bannerY = (sh - bannerH) / 2 - 30;
			Rectangle bannerRect = new Rectangle(bannerX, bannerY, bannerW, bannerH);

			// Shadow & Box Background
			PokemonUI.SoftShadow(b, bannerRect, 0.45f * bannerAlpha);
			IClickableMenu.drawTextureBox(b, bannerRect.X, bannerRect.Y, bannerRect.Width, bannerRect.Height, new Color(22, 18, 30) * bannerAlpha);
			PokemonUI.DrawCardBorder(b, bannerRect, _awakeningAuraColor * bannerAlpha, 3);

			// Inner Header Accent Ribbon
			Rectangle ribbonRect = new Rectangle(bannerX + 16, bannerY + 12, bannerW - 32, 28);
			b.Draw(Game1.staminaRect, ribbonRect, new Color(_awakeningAuraColor.R / 4, _awakeningAuraColor.G / 4, _awakeningAuraColor.B / 4) * bannerAlpha);
			PokemonUI.Text(b, Game1.smallFont, $"★ {_awakeningTitle} ★", ribbonRect, _awakeningAuraColor * bannerAlpha, center: true, maxScale: 0.76f, shadow: true);

			// Titan Name in Bold Font
			string speciesUpper = _awakeningSpecies != null ? _awakeningSpecies.Name.ToUpperInvariant() : "LENDÁRIO";
			Rectangle nameRect = new Rectangle(bannerX + 16, bannerY + 44, bannerW - 32, 44);
			PokemonUI.Text(b, Game1.dialogueFont, speciesUpper, nameRect, Color.White * bannerAlpha, center: true, maxScale: 1.05f, shadow: true);

			// Subtitle / Lore Hint
			string subText = $"O poder ancestral da relíquia '{_awakeningRelicName}' ressoou através das eras!";
			Rectangle subRect = new Rectangle(bannerX + 20, bannerY + 92, bannerW - 40, 32);
			PokemonUI.Text(b, Game1.smallFont, subText, subRect, new Color(220, 230, 245) * bannerAlpha, center: true, maxScale: 0.74f, shadow: false);

			// Sparkling particles around the banner
			Vector2 center = new Vector2(bannerRect.Center.X, bannerRect.Center.Y);
			for (int i = 0; i < _awakeningParticles.Count; i++)
			{
				Vector2 pPos = center + _awakeningParticles[i] * (1.0f + (float)Math.Sin(_awakeningTimer * 4.0 + i) * 0.15f);
				b.Draw(Game1.staminaRect, new Rectangle((int)pPos.X - 3, (int)pPos.Y - 3, 6, 6), _awakeningAuraColor * bannerAlpha * 0.9f);
			}
		}
	}

	private static Color DetermineElementalColor(PokemonSpecies spec)
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
}
