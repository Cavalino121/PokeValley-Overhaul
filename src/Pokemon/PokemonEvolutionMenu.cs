using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

/// <summary>
/// Classic authentic Pokémon Evolution sequence screen.
/// Displays pulsating silhouette morphing, sound effects, celebration fanfare, and new form reveal.
/// </summary>
public class PokemonEvolutionMenu : IClickableMenu
{
	private readonly PokemonInstance _mon;
	private readonly PokemonSpecies _oldSpecies;
	private readonly PokemonSpecies _targetSpecies;
	private readonly Action _onComplete;

	private enum EvoState
	{
		Intro,
		Morphing,
		FlashBurst,
		Revealed,
		Cancelled,
		Finished
	}

	private EvoState _state = EvoState.Intro;
	private float _timer = 0f;
	private float _morphFrequency = 2.0f; // pulses per second, speeds up!
	private bool _showTargetFrame = false;
	private float _scale = 3.5f;
	private Color _tint = Color.White;
	private readonly List<Vector2> _sparkles = new();
	private bool _cancelled = false;

	private Texture2D _oldFrontTex;
	private Texture2D _newFrontTex;

	public override bool areGamePadControlsImplemented() => true;

	public override bool readyToClose() => _state == EvoState.Finished;

	public PokemonEvolutionMenu(PokemonInstance mon, PokemonSpecies targetSpecies, Action onComplete = null)
		: base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height)
	{
		_mon = mon;
		_oldSpecies = mon.Species;
		_targetSpecies = targetSpecies;
		_onComplete = onComplete;

		_oldFrontTex = _mon.GetFrontSprite(Game1.graphics.GraphicsDevice);

		// Temporarily create an instance of target species to fetch its front sprite
		var tempNew = new PokemonInstance(_targetSpecies, _mon.Level);
		_newFrontTex = tempNew.GetFrontSprite(Game1.graphics.GraphicsDevice);

		Game1.playSound("wand");
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		// Clicks do not cancel evolution anymore; only key 'B' or Escape cancels.
		if (_state == EvoState.Revealed || _state == EvoState.Cancelled)
		{
			_state = EvoState.Finished;
			Game1.playSound("bigDeSelect");
			exitThisMenu();
			_onComplete?.Invoke();
		}
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.B || key == Keys.Escape || key == Keys.Space || key == Keys.Enter)
		{
			if (_state == EvoState.Morphing && (key == Keys.B || key == Keys.Escape))
			{
				CancelEvolution();
				return;
			}
			if (_state == EvoState.Revealed || _state == EvoState.Cancelled)
			{
				_state = EvoState.Finished;
				exitThisMenu();
				_onComplete?.Invoke();
				return;
			}
		}
		base.receiveKeyPress(key);
	}

	public override void receiveGamePadButton(Buttons button)
	{
		if (_state == EvoState.Morphing && button == Buttons.B)
		{
			CancelEvolution();
			return;
		}
		if ((_state == EvoState.Revealed || _state == EvoState.Cancelled) && (button == Buttons.A || button == Buttons.B || button == Buttons.Start))
		{
			_state = EvoState.Finished;
			Game1.playSound("bigDeSelect");
			exitThisMenu();
			_onComplete?.Invoke();
		}
	}

	private void CancelEvolution()
	{
		if (_cancelled) return;
		_cancelled = true;
		_state = EvoState.Cancelled;
		_timer = 0f;
		_scale = 3.5f;
		_tint = Color.White;
		Game1.playSound("cancel");
	}

	public override void update(GameTime time)
	{
		base.update(time);
		float dt = (float)time.ElapsedGameTime.TotalSeconds;
		_timer += dt;

		if (_state == EvoState.Intro)
		{
			if (_timer >= 1.8f)
			{
				_state = EvoState.Morphing;
				_timer = 0f;
				Game1.playSound("reward");
			}
		}
		else if (_state == EvoState.Morphing)
		{
			// Speed up pulsating morphing
			_morphFrequency = MathHelper.Lerp(2.5f, 16.0f, Math.Min(1f, _timer / 5.0f));
			_showTargetFrame = ((int)(_timer * _morphFrequency) % 2 == 1);

			// Pulsing tint and slight breathing scale
			float pulse = (float)Math.Sin(_timer * _morphFrequency * Math.PI);
			_tint = Color.Lerp(Color.White, Color.Cyan * 2f, Math.Abs(pulse));
			_scale = 3.5f + pulse * 0.15f;

			if (_timer >= 5.5f)
			{
				_state = EvoState.FlashBurst;
				_timer = 0f;
				Game1.playSound("yoba");
				Game1.playSound("reward");

				// Actually evolve the Pokemon!
				PokemonEvolution.Evolve(_mon, _targetSpecies.InternalName);
			}
		}
		else if (_state == EvoState.FlashBurst)
		{
			if (_timer >= 0.6f)
			{
				_state = EvoState.Revealed;
				_timer = 0f;
				_scale = 3.6f;
				_tint = Color.White;

				// Spawn celebration sparkles
				_sparkles.Clear();
				for (int i = 0; i < 30; i++)
				{
					float angle = (float)(Game1.random.NextDouble() * Math.PI * 2.0);
					float dist = Game1.random.Next(60, 260);
					_sparkles.Add(new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist));
				}
			}
		}
		else if (_state == EvoState.Revealed)
		{
			// Gentle breathing float
			_scale = 3.5f + (float)Math.Sin(_timer * 3.5f) * 0.08f;
		}
	}

	public override void draw(SpriteBatch b)
	{
		// Dark background with starry night hue
		b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Color(12, 16, 28));

		Vector2 center = new Vector2(Game1.uiViewport.Width / 2f, Game1.uiViewport.Height / 2f - 40);

		// Glow aura behind Pokemon
		float auraSize = (_state == EvoState.FlashBurst) ? 400f : 240f;
		Color auraColor = (_state == EvoState.Revealed) ? Color.Gold * 0.25f : Color.Cyan * 0.20f;
		PokemonUI.SoftShadow(b, (int)center.X, (int)center.Y + 110, (int)auraSize, 40, 0.45f);

		// Draw Pokemon Sprite: during FlashBurst and Revealed, strictly show newFrontTex!
		Texture2D currentTex = (_state == EvoState.Revealed || _state == EvoState.FlashBurst || (_state == EvoState.Morphing && _showTargetFrame)) ? (_newFrontTex ?? _oldFrontTex) : _oldFrontTex;
		if (currentTex != null)
		{
			Rectangle srcRect = new Rectangle(0, 0, Math.Min(64, currentTex.Width), Math.Min(64, currentTex.Height));
			Vector2 origin = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
			b.Draw(currentTex, center, srcRect, _tint, 0f, origin, _scale, SpriteEffects.None, 0.9f);
		}

		// Flash burst overlay
		if (_state == EvoState.FlashBurst)
		{
			float flashAlpha = Math.Clamp(1f - (_timer / 0.6f), 0f, 1f);
			b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.White * flashAlpha);
		}

		// Celebration sparkles during revealed state
		if (_state == EvoState.Revealed)
		{
			for (int i = 0; i < _sparkles.Count; i++)
			{
				Vector2 sp = center + _sparkles[i] * (1f + (float)Math.Sin(_timer * 2.0 + i) * 0.1f);
				b.Draw(Game1.staminaRect, new Rectangle((int)sp.X - 3, (int)sp.Y - 3, 6, 6), Color.Gold * 0.85f);
			}
		}

		// Bottom Dialogue Box
		int boxW = Math.Min(860, Game1.uiViewport.Width - 64);
		int boxH = 120;
		int boxX = (Game1.uiViewport.Width - boxW) / 2;
		int boxY = Game1.uiViewport.Height - boxH - 36;
		Rectangle boxRect = new Rectangle(boxX, boxY, boxW, boxH);

		PokemonUI.Border(b, boxRect, 4, new Color(24, 48, 56));
		b.Draw(Game1.staminaRect, new Rectangle(boxX + 4, boxY + 4, boxW - 8, boxH - 8), new Color(248, 252, 250));

		string message = "";
		if (_state == EvoState.Intro || _state == EvoState.Morphing)
		{
			message = $"O quê?! {_mon.Nickname} está evoluindo!\n[Pressione B para interromper]";
		}
		else if (_state == EvoState.FlashBurst || _state == EvoState.Revealed)
		{
			message = $"Parabéns! Seu {_oldSpecies.Name} evoluiu para {_targetSpecies.Name}!\n[Clique em qualquer lugar para continuar]";
		}
		else if (_state == EvoState.Cancelled)
		{
			message = $"Hein?! {_mon.Nickname} parou de evoluir!\n[Clique ou aperte espaço para continuar]";
		}

		PokemonUI.Text(b, Game1.dialogueFont, message, new Rectangle(boxX + 24, boxY + 16, boxW - 48, boxH - 32), new Color(24, 32, 40), center: true, maxScale: 0.90f);

		drawMouse(b);
	}
}
