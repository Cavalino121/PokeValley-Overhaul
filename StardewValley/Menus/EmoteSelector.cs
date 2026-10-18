using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Menus;

public class EmoteSelector : IClickableMenu
{
	public Rectangle scrollView;

	public List<ClickableTextureComponent> emoteButtons;

	public ClickableTextureComponent okButton;

	public float scrollY;

	public int emoteIndex;

	protected ClickableTextureComponent _selectedEmote;

	protected ClickableTextureComponent _hoveredEmote;

	protected Texture2D emoteTexture;

	public EmoteSelector(int emote_index, string selected_emote = "")
		: base(Game1.uiViewport.Width / 2 - (800 + IClickableMenu.borderWidth * 2) / 2, Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2 - 64, 800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2 + 64)
	{
		emoteTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\EmoteMenu");
		Game1.playSound("shwip");
		emoteIndex = emote_index;
		Game1.player.faceDirection(2);
		Game1.player.FarmerSprite.StopAnimation();
		emoteButtons = new List<ClickableTextureComponent>();
		currentlySnappedComponent = null;
		for (int i = 0; i < Farmer.EMOTES.Length; i++)
		{
			Farmer.EmoteType emoteType = Farmer.EMOTES[i];
			if (!emoteType.hidden || Game1.player.performedEmotes.ContainsKey(emoteType.emoteString))
			{
				ClickableTextureComponent clickableTextureComponent = new ClickableTextureComponent(new Rectangle(0, 0, 80, 68), emoteTexture, EmoteMenu.GetEmoteNonBubbleSpriteRect(i), 4f, drawShadow: true)
				{
					leftNeighborID = -99998,
					rightNeighborID = -99998,
					upNeighborID = -99998,
					downNeighborID = -99998,
					myID = i
				};
				clickableTextureComponent.label = emoteType.displayName;
				clickableTextureComponent.name = emoteType.emoteString;
				clickableTextureComponent.drawLabelWithShadow = true;
				clickableTextureComponent.hoverText = ((emoteType.animationFrames != null) ? "animated" : "");
				emoteButtons.Add(clickableTextureComponent);
				if (currentlySnappedComponent == null)
				{
					currentlySnappedComponent = clickableTextureComponent;
				}
				if (selected_emote != "" && selected_emote == clickableTextureComponent.name)
				{
					currentlySnappedComponent = clickableTextureComponent;
					_selectedEmote = clickableTextureComponent;
				}
			}
		}
		okButton = new ClickableTextureComponent("OK", new Rectangle(xPositionOnScreen + width - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 64, yPositionOnScreen + height - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder + 16, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
		{
			upNeighborID = -99998,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			downNeighborID = -99998,
			myID = 1000,
			drawShadow = true
		};
		RepositionElements();
		populateClickableComponentList();
		if (Game1.options.snappyMenus && Game1.options.gamepadControls)
		{
			snapToDefaultClickableComponent();
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		xPositionOnScreen = Game1.uiViewport.Width / 2 - (632 + IClickableMenu.borderWidth * 2) / 2;
		yPositionOnScreen = Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2 - 64;
		RepositionElements();
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		ClickableTextureComponent hoveredEmote = _hoveredEmote;
		_hoveredEmote = null;
		okButton.tryHover(x, y);
		foreach (ClickableTextureComponent emoteButton in emoteButtons)
		{
			int num = emoteButton.bounds.Width;
			emoteButton.bounds.Width = scrollView.Width / 3;
			emoteButton.tryHover(x, y);
			if (emoteButton != _selectedEmote && emoteButton.bounds.Contains(x, y) && scrollView.Contains(x, y))
			{
				_hoveredEmote = emoteButton;
			}
			emoteButton.bounds.Width = num;
		}
		if (_hoveredEmote != null && _hoveredEmote != hoveredEmote)
		{
			Game1.playSound("shiny4");
		}
	}

	private void RepositionElements()
	{
		scrollView = new Rectangle(xPositionOnScreen + 64, yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - 4, width - 128, height - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder - 64 + 8);
		RepositionScrollElements();
	}

	public void RepositionScrollElements()
	{
		int num = (int)scrollY + 4;
		if (scrollY > 0f)
		{
			scrollY = 0f;
		}
		int num2 = 8;
		foreach (ClickableTextureComponent emoteButton in emoteButtons)
		{
			emoteButton.bounds.X = scrollView.X + num2;
			emoteButton.bounds.Y = scrollView.Y + num;
			if (emoteButton.bounds.Bottom > scrollView.Bottom)
			{
				num = 4;
				num2 += scrollView.Width / 3;
				emoteButton.bounds.X = scrollView.X + num2;
				emoteButton.bounds.Y = scrollView.Y + num;
			}
			num += emoteButton.bounds.Height;
			if (scrollView.Intersects(emoteButton.bounds))
			{
				emoteButton.visible = true;
			}
			else
			{
				emoteButton.visible = false;
			}
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		foreach (ClickableTextureComponent emoteButton in emoteButtons)
		{
			int num = emoteButton.bounds.Width;
			emoteButton.bounds.Width = scrollView.Width / 3;
			if (emoteButton.bounds.Contains(x, y) && scrollView.Contains(x, y))
			{
				emoteButton.bounds.Width = num;
				if (emoteIndex < Game1.player.GetEmoteFavorites().Count)
				{
					Game1.player.GetEmoteFavorites()[emoteIndex] = emoteButton.name;
				}
				exitThisMenu(playSound: false);
				Game1.playSound("drumkit6");
				if (!Game1.options.gamepadControls)
				{
					Game1.emoteMenu = new EmoteMenu();
				}
				return;
			}
			emoteButton.bounds.Width = num;
		}
		if (okButton.containsPoint(x, y))
		{
			exitThisMenu();
		}
	}

	public bool canLeaveMenu()
	{
		return true;
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), xPositionOnScreen - 128 - 8, yPositionOnScreen + 128 - 8, 192, 164, Color.White, 1f, drawShadow: false);
		Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, speaker: false, drawOnlyBox: true);
		foreach (ClickableTextureComponent emoteButton in emoteButtons)
		{
			if (emoteButton == currentlySnappedComponent && Game1.options.gamepadControls && emoteButton != _selectedEmote && emoteButton == _hoveredEmote)
			{
				IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(64, 320, 60, 60), emoteButton.bounds.X + 64 + 8, emoteButton.bounds.Y + 8, scrollView.Width / 3 - 64 - 16, emoteButton.bounds.Height - 16, Color.White, 1f, drawShadow: false);
				Utility.drawWithShadow(b, emoteTexture, emoteButton.getVector2() - new Vector2(4f, 4f), new Rectangle(83, 0, 18, 18), Color.White, 0f, Vector2.Zero, 4f);
			}
			emoteButton.draw(b, Color.White * ((emoteButton == _selectedEmote) ? 0.4f : 1f), 0.87f);
			if (emoteButton != _selectedEmote && emoteButton.hoverText != "" && Game1.currentGameTime.TotalGameTime.Milliseconds % 500 < 250)
			{
				b.Draw(emoteButton.texture, emoteButton.getVector2(), new Rectangle(emoteButton.sourceRect.X + 80, emoteButton.sourceRect.Y, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.99f);
			}
		}
		if (_selectedEmote != null)
		{
			for (int i = 0; i < 8; i++)
			{
				float num = Utility.Lerp(0f, (float)Math.PI * 2f, (float)i / 8f);
				Vector2 zero = Vector2.Zero;
				zero.X = (int)((float)(xPositionOnScreen - 64 + (int)(Math.Cos(num) * 12.0) * 4) - 3.5f);
				zero.Y = (int)((float)(yPositionOnScreen + 192 + (int)((0.0 - Math.Sin(num)) * 12.0) * 4) - 3.5f);
				Utility.drawWithShadow(b, emoteTexture, zero, new Rectangle(64 + ((i == emoteIndex) ? 8 : 0), 48, 8, 8), Color.White, 0f, Vector2.Zero);
			}
		}
		okButton.draw(b);
		drawMouse(b);
	}

	/// <inheritdoc />
	protected override void cleanupBeforeExit()
	{
		base.cleanupBeforeExit();
		Game1.player.noMovementPause = Math.Max(Game1.player.noMovementPause, 200);
	}
}
