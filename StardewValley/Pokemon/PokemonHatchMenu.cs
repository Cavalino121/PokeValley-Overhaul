using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

public class PokemonHatchMenu : IClickableMenu
{
	private class Shard
	{
		public Vector2 Pos;
		public Vector2 Vel;
		public float Rot;
		public float RotSpeed;
		public float Scale;
	}

	private readonly PokemonInstance _egg;
	private int _hatchPhase = 0; // 0 = Wobble, 1 = Cracking, 2 = Hatched reveal
	private float _timer = 0f;
	private float _revealTimer = 0f;
	private float _wobbleAngle = 0f;
	private Texture2D _eggFront;
	private Texture2D _crackedEggTex;
	private Texture2D _breakingEggTex;
	private Texture2D _shardTex;
	private Texture2D _hatchedFront;
	private readonly List<Shard> _shards = new();

	private ClickableComponent _continueButton;

	public PokemonHatchMenu(PokemonInstance egg)
		: base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height)
	{
		_egg = egg;
		Game1.playSound("dirtyHit");
		LoadTextures();

		int centerX = width / 2;
		int centerY = height / 2 - 40;
		_continueButton = new ClickableComponent(new Rectangle(centerX - 90, centerY + 245, 180, 48), "continue", "Continuar");
	}

	public override bool readyToClose() => false;

	private void LoadTextures()
	{
		try
		{
			string p1 = PokemonUI.FindInProject("pokeemerald", "graphics", "pokemon", "egg", "front.png");
			if (p1 != null && System.IO.File.Exists(p1))
			{
				_eggFront = PokemonUI.LoadColorKeyed(p1);
			}

			string pCracked = PokemonUI.FindInProject("pokeemerald", "graphics", "battle_anims", "sprites", "cracked_egg.png");
			if (pCracked != null && System.IO.File.Exists(pCracked))
			{
				_crackedEggTex = PokemonUI.LoadColorKeyed(pCracked);
			}

			string pBreaking = PokemonUI.FindInProject("pokeemerald", "graphics", "battle_anims", "sprites", "breaking_egg.png");
			if (pBreaking != null && System.IO.File.Exists(pBreaking))
			{
				_breakingEggTex = PokemonUI.LoadColorKeyed(pBreaking);
			}

			string pShard = PokemonUI.FindInProject("pokeemerald", "graphics", "pokemon", "egg", "shard.png");
			if (pShard != null && System.IO.File.Exists(pShard))
			{
				_shardTex = PokemonUI.LoadColorKeyed(pShard);
			}
		}
		catch { }
	}

	private bool _crackSound1 = false;
	private bool _crackSound2 = false;

	public override void update(GameTime time)
	{
		base.update(time);
		float dt = (float)time.ElapsedGameTime.TotalSeconds;
		_timer += dt;

		// Update flying shards
		for (int i = _shards.Count - 1; i >= 0; i--)
		{
			var s = _shards[i];
			s.Pos += s.Vel * dt;
			s.Vel.Y += 280f * dt; // gravity
			s.Rot += s.RotSpeed * dt;
		}

		if (_hatchPhase == 0)
		{
			// Wobble back and forth
			_wobbleAngle = (float)Math.Sin(_timer * 12.0) * 0.18f;
			if (_timer > 2.2f)
			{
				_hatchPhase = 1;
				_timer = 0f;
				_crackSound1 = false;
				_crackSound2 = false;
				Game1.playSound("boulderCrack");
			}
		}
		else if (_hatchPhase == 1)
		{
			// Violent shake and cracking
			_wobbleAngle = (float)Math.Sin(_timer * 28.0) * 0.28f;
			if (_timer > 0.7f && !_crackSound1)
			{
				_crackSound1 = true;
				Game1.playSound("dirtyHit");
			}
			if (_timer > 1.4f && !_crackSound2)
			{
				_crackSound2 = true;
				Game1.playSound("boulderCrack");
			}

			if (_timer > 2.2f)
			{
				_hatchPhase = 2;
				_timer = 0f;
				_revealTimer = 0f;

				// Spawn shards
				int centerX = width / 2;
				int centerY = height / 2 - 40;
				for (int i = 0; i < 16; i++)
				{
					float angle = (float)(i * (Math.PI * 2.0 / 16.0));
					float speed = 120f + Game1.random.Next(160);
					_shards.Add(new Shard
					{
						Pos = new Vector2(centerX, centerY),
						Vel = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed - 60f),
						Rot = 0f,
						RotSpeed = (Game1.random.NextSingle() - 0.5f) * 12f,
						Scale = 2.5f + Game1.random.NextSingle() * 1.5f
					});
				}

				_egg.IsEgg = false;
				_egg.InvalidateSpriteCache();
				_egg.Nickname = _egg.Species.Name;
				_egg.RecalculateStats();
				_egg.CurrentHP = _egg.MaxHP;
				_hatchedFront = _egg.GetFrontSprite(Game1.graphics.GraphicsDevice);
				if (_hatchedFront == null)
				{
					_hatchedFront = _egg.GetIconSprite(Game1.graphics.GraphicsDevice);
				}
				PlayerParty.Instance.SaveParty();
				PokemonPokedex.Instance.RecordCaught(_egg.Species.Name);
				Game1.playSound("reward");
				PokemonMusicManager.PlayTrack(PokemonMusicTrack.VictoryFanfare, loop: false, pauseStardew: true);
			}
		}
		else if (_hatchPhase == 2)
		{
			_revealTimer += dt;
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (_hatchPhase == 2 && _revealTimer >= 0.8f)
		{
			if (_continueButton != null && _continueButton.containsPoint(x, y))
			{
				Game1.playSound("coin");
				PokemonMusicManager.StopAndRestoreStardewMusic();
				exitThisMenu();
			}
		}
	}

	public override void receiveKeyPress(Keys key)
	{
		if (_hatchPhase == 2 && _revealTimer >= 0.8f && (key == Keys.Space || key == Keys.Enter || key == Keys.Escape))
		{
			Game1.playSound("coin");
			PokemonMusicManager.StopAndRestoreStardewMusic();
			exitThisMenu();
		}
	}

	public override void draw(SpriteBatch b)
	{
		// Translucent dark cinematic vignette over the world
		Rectangle screen = new Rectangle(-500, -500, Math.Max(width, Game1.uiViewport.Width) + 1000, Math.Max(height, Game1.uiViewport.Height) + 1000);
		b.Draw(Game1.fadeToBlackRect, screen, Color.Black * 0.85f);

		int centerX = width / 2;
		int centerY = height / 2 - 40;

		if (_hatchPhase < 2)
		{
			// Draw Egg
			Texture2D eggTex = _eggFront;
			if (eggTex != null)
			{
				int eggSize = Math.Min(eggTex.Width, eggTex.Height);
				Rectangle eggSrc = new Rectangle(0, 0, eggSize, eggSize);
				Vector2 origin = new Vector2(eggSize / 2f, eggSize / 2f);
				b.Draw(eggTex, new Vector2(centerX, centerY), eggSrc, Color.White, _wobbleAngle, origin, 3.5f, SpriteEffects.None, 0.88f);
			}

			if (_hatchPhase == 1)
			{
				// Draw progressive fissure cracks over the shaking intact egg that rotate with it
				Vector2 eggCenter = new Vector2(centerX, centerY);
				float cosA = (float)Math.Cos(_wobbleAngle);
				float sinA = (float)Math.Sin(_wobbleAngle);

				Vector2 RotatePoint(Vector2 localOffset)
				{
					return new Vector2(
						eggCenter.X + localOffset.X * cosA - localOffset.Y * sinA,
						eggCenter.Y + localOffset.X * sinA + localOffset.Y * cosA
					);
				}

				void DrawCrackSegment(Vector2 localStart, Vector2 localEnd, int thickness, Color color)
				{
					Vector2 p1 = RotatePoint(localStart);
					Vector2 p2 = RotatePoint(localEnd);
					float dist = Vector2.Distance(p1, p2);
					float angle = (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
					b.Draw(Game1.staminaRect, new Rectangle((int)p1.X, (int)p1.Y, (int)dist, thickness), null, color, angle, Vector2.Zero, SpriteEffects.None, 0.89f);
				}

				Color crackShadow = new Color(40, 25, 15, 230);
				Color crackCore = Color.White * Math.Clamp(_timer / 1.6f, 0.2f, 1f);

				// Stage 1 crack: Primary vertical jagged fissure
				DrawCrackSegment(new Vector2(-6, -45), new Vector2(4, -20), 4, crackShadow);
				DrawCrackSegment(new Vector2(4, -20), new Vector2(-12, 10), 4, crackShadow);
				DrawCrackSegment(new Vector2(-12, 10), new Vector2(10, 35), 4, crackShadow);

				DrawCrackSegment(new Vector2(-5, -45), new Vector2(5, -20), 2, crackCore);
				DrawCrackSegment(new Vector2(5, -20), new Vector2(-11, 10), 2, crackCore);
				DrawCrackSegment(new Vector2(-11, 10), new Vector2(11, 35), 2, crackCore);

				// Stage 2 crack: Branching horizontal fissures
				if (_timer > 0.7f)
				{
					DrawCrackSegment(new Vector2(4, -20), new Vector2(35, -30), 3, crackShadow);
					DrawCrackSegment(new Vector2(4, -20), new Vector2(35, -30), 2, crackCore);

					DrawCrackSegment(new Vector2(-12, 10), new Vector2(-38, -2), 3, crackShadow);
					DrawCrackSegment(new Vector2(-12, 10), new Vector2(-38, -2), 2, crackCore);
				}

				// Stage 3 crack: Glowing intense fracture with radiant burst
				if (_timer > 1.4f)
				{
					DrawCrackSegment(new Vector2(-12, 10), new Vector2(-25, 42), 3, crackShadow);
					DrawCrackSegment(new Vector2(-12, 10), new Vector2(-25, 42), 2, crackCore);

					DrawCrackSegment(new Vector2(10, 35), new Vector2(38, 48), 3, crackShadow);
					DrawCrackSegment(new Vector2(10, 35), new Vector2(38, 48), 2, crackCore);

					// Radiant light pulsing through cracks
					float glow = (float)(Math.Sin(_timer * 30.0) * 0.5 + 0.5);
					Vector2 lightPos = RotatePoint(new Vector2(-2, 0));
					int glowSize = (int)(40 + glow * 30);
					b.Draw(Game1.staminaRect, new Rectangle((int)lightPos.X - glowSize / 2, (int)lightPos.Y - glowSize / 2, glowSize, glowSize), Color.Gold * (0.35f + glow * 0.35f));
				}
			}

			// Subtitle banner
			string text = _hatchPhase == 0 ? "O que foi isso? O Ovo começou a se mexer..." : "O Ovo começou a rachar!";
			int boxW = 540;
			int boxH = 64;
			int boxX = centerX - boxW / 2;
			int boxY = centerY + 140;
			IClickableMenu.drawTextureBox(b, boxX, boxY, boxW, boxH, Color.White);
			PokemonUI.Text(b, Game1.dialogueFont, text, new Rectangle(boxX + 20, boxY + 14, boxW - 40, 36), Color.DarkSlateGray, center: true, maxScale: 0.82f);
		}
		else
		{
			// Draw Glowing halo behind hatched Pokemon
			float haloPulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 4.0) * 15f;
			int haloRadius = (int)(90 + haloPulse);
			b.Draw(Game1.staminaRect, new Rectangle(centerX - haloRadius, centerY - haloRadius, haloRadius * 2, haloRadius * 2), Color.Gold * 0.25f);
			b.Draw(Game1.staminaRect, new Rectangle(centerX - haloRadius / 2, centerY - haloRadius / 2, haloRadius, haloRadius), Color.White * 0.35f);

			// Draw Shards
			foreach (var s in _shards)
			{
				if (_shardTex != null)
				{
					Vector2 orig = new Vector2(_shardTex.Width / 2f, _shardTex.Height / 2f);
					b.Draw(_shardTex, s.Pos, null, Color.White * 0.8f, s.Rot, orig, s.Scale, SpriteEffects.None, 0.85f);
				}
				else
				{
					b.Draw(Game1.staminaRect, new Rectangle((int)s.Pos.X - 4, (int)s.Pos.Y - 4, 8, 8), Color.Bisque * 0.8f);
				}
			}

			// Draw Hatched Pokemon
			if (_hatchedFront == null)
			{
				_hatchedFront = _egg.GetFrontSprite(Game1.graphics.GraphicsDevice) ?? _egg.GetIconSprite(Game1.graphics.GraphicsDevice);
			}

			if (_hatchedFront != null)
			{
				Rectangle monSrc = _egg.GetFrontSourceRect();
				Vector2 origin = new Vector2(monSrc.Width / 2f, monSrc.Height / 2f);
				b.Draw(_hatchedFront, new Vector2(centerX, centerY), monSrc, Color.White, 0f, origin, 3.5f, SpriteEffects.None, 0.88f);
			}

			// Hatched announcement
			int boxW = 660;
			int boxH = 100;
			int boxX = centerX - boxW / 2;
			int boxY = centerY + 130;
			IClickableMenu.drawTextureBox(b, boxX, boxY, boxW, boxH, Color.PaleGoldenrod);
			PokemonUI.Text(b, Game1.dialogueFont, $"O Ovo chocou!", new Rectangle(boxX + 20, boxY + 10, boxW - 40, 32), Color.DarkGoldenrod, center: true, maxScale: 0.90f);
			PokemonUI.Text(b, Game1.smallFont, $"Nasceu um saudável {_egg.Species.Name}! Natureza: {_egg.Nature} | {_egg.IVRating}", new Rectangle(boxX + 20, boxY + 46, boxW - 40, 24), Color.DarkGreen, center: true, maxScale: 0.80f);

			// Dedicated Continue Button
			if (_revealTimer >= 0.8f)
			{
				bool hov = _continueButton != null && _continueButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());
				IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), _continueButton.bounds.X, _continueButton.bounds.Y, _continueButton.bounds.Width, _continueButton.bounds.Height, hov ? Color.Wheat : Color.White, 1f, false);
				PokemonUI.Text(b, Game1.smallFont, "Continuar", _continueButton.bounds, Color.DarkSlateBlue, center: true, maxScale: 0.85f);
			}
			else
			{
				PokemonUI.Text(b, Game1.tinyFont, "★ ★ ★", new Rectangle(centerX - 50, centerY + 245, 100, 30), Color.Gray, center: true, maxScale: 0.80f);
			}
		}

		drawMouse(b);
	}
}

