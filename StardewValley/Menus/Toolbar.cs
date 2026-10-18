using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StardewValley.Menus;

public class Toolbar : IClickableMenu
{
	public List<ClickableComponent> buttons = new List<ClickableComponent>();

	public new int yPositionOnScreen;

	public Item hoverItem;

	public float transparency = 1f;

	private bool hoverDirty = true;

	public string[] slotText = new string[12]
	{
		"1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
		"-", "="
	};

	public Rectangle toolbarTextSource = new Rectangle(0, 256, 60, 60);

	public Toolbar()
		: base(Game1.uiViewport.Width / 2 - 384 - 64, Game1.uiViewport.Height, 896, 208)
	{
		for (int i = 0; i < 12; i++)
		{
			buttons.Add(new ClickableComponent(new Rectangle(Game1.uiViewport.Width / 2 - 384 + i * 64, yPositionOnScreen - 96 + 8, 64, 64), i.ToString() ?? ""));
		}
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (Game1.player.UsingTool || Game1.IsChatting || Game1.farmEvent != null)
		{
			return;
		}
		foreach (ClickableComponent button in buttons)
		{
			if (button.containsPoint(x, y))
			{
				Game1.player.CurrentToolIndex = Convert.ToInt32(button.name);
				if (Game1.player.ActiveObject != null)
				{
					Game1.player.showCarrying();
					Game1.playSound("pickUpItem");
				}
				else
				{
					Game1.player.showNotCarrying();
					Game1.playSound("stoneStep");
				}
				break;
			}
		}
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (!Game1.GetKeyboardState().IsKeyDown(Keys.LeftShift) && !Game1.GetKeyboardState().IsKeyDown(Keys.LeftControl))
		{
			return;
		}
		foreach (ClickableComponent button in buttons)
		{
			if (!button.containsPoint(x, y))
			{
				continue;
			}
			int num = Convert.ToInt32(button.name);
			if (num < Game1.player.Items.Count && Game1.player.Items[num] != null)
			{
				hoverItem = Game1.player.Items[num];
				if (hoverItem.canBeDropped())
				{
					Game1.playSound("throwDownITem");
					Game1.player.Items[num] = null;
					Game1.createItemDebris(hoverItem, Game1.player.getStandingPosition(), Game1.player.FacingDirection).DroppedByPlayerID.Value = Game1.player.UniqueMultiplayerID;
					break;
				}
			}
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		if (hoverDirty)
		{
			gameWindowSizeChanged(new Rectangle(Game1.viewport.X, Game1.viewport.Y, Game1.viewport.Width, Game1.viewport.Height), new Rectangle(Game1.viewport.X, Game1.viewport.Y, Game1.viewport.Width, Game1.viewport.Height));
			hoverDirty = false;
		}
		hoverItem = null;
		foreach (ClickableComponent button in buttons)
		{
			if (button.containsPoint(x, y))
			{
				int num = Convert.ToInt32(button.name);
				if (num < Game1.player.Items.Count && Game1.player.Items[num] != null)
				{
					button.scale = Math.Min(button.scale + 0.05f, 1.1f);
					hoverItem = Game1.player.Items[num];
				}
			}
			else
			{
				button.scale = Math.Max(button.scale - 0.025f, 1f);
			}
		}
	}

	public void shifted(bool right)
	{
		if (right)
		{
			for (int i = 0; i < buttons.Count; i++)
			{
				buttons[i].scale = 1f + (float)i * 0.03f;
			}
			return;
		}
		for (int num = buttons.Count - 1; num >= 0; num--)
		{
			buttons[num].scale = 1f + (float)(11 - num) * 0.03f;
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		for (int i = 0; i < 12; i++)
		{
			buttons[i].bounds = new Rectangle(Game1.uiViewport.Width / 2 - 384 + i * 64, yPositionOnScreen - 96 + 8, 64, 64);
		}
	}

	public override bool isWithinBounds(int x, int y)
	{
		ClickableComponent clickableComponent = buttons[0];
		return new Rectangle(clickableComponent.bounds.X, clickableComponent.bounds.Y, buttons.Last().bounds.X - clickableComponent.bounds.X + 64, 64).Contains(x, y);
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (Game1.activeClickableMenu != null)
		{
			return;
		}
		Point standingPixel = Game1.player.StandingPixel;
		Vector2 vector = Game1.GlobalToLocal(globalPosition: new Vector2(standingPixel.X, standingPixel.Y), viewport: Game1.viewport);
		bool flag;
		if (Game1.options.pinToolbarToggle)
		{
			flag = false;
			transparency = Math.Min(1f, transparency + 0.075f);
			if (vector.Y > (float)(Game1.viewport.Height - 192))
			{
				transparency = Math.Max(0.33f, transparency - 0.15f);
			}
		}
		else
		{
			flag = ((vector.Y > (float)(Game1.viewport.Height / 2 + 64)) ? true : false);
			transparency = 1f;
		}
		int num = Utility.makeSafeMarginY(8);
		int num2 = yPositionOnScreen;
		if (!flag)
		{
			yPositionOnScreen = Game1.uiViewport.Height;
			yPositionOnScreen += 8;
			yPositionOnScreen -= num;
		}
		else
		{
			yPositionOnScreen = 112;
			yPositionOnScreen -= 8;
			yPositionOnScreen += num;
		}
		if (num2 != yPositionOnScreen)
		{
			for (int i = 0; i < 12; i++)
			{
				buttons[i].bounds.Y = yPositionOnScreen - 96 + 8;
			}
		}
		IClickableMenu.drawTextureBox(b, Game1.menuTexture, toolbarTextSource, Game1.uiViewport.Width / 2 - 384 - 16, yPositionOnScreen - 96 - 8, 800, 96, Color.White * transparency, 1f, drawShadow: false);
		for (int j = 0; j < 12; j++)
		{
			Vector2 vector2 = new Vector2(Game1.uiViewport.Width / 2 - 384 + j * 64, yPositionOnScreen - 96 + 8);
			b.Draw(Game1.menuTexture, vector2, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, (Game1.player.CurrentToolIndex == j) ? 56 : 10), Color.White * transparency);
			if (!Game1.options.gamepadControls)
			{
				b.DrawString(Game1.tinyFont, slotText[j], vector2 + new Vector2(4f, -8f), Color.DimGray * transparency);
			}
		}
		for (int k = 0; k < 12; k++)
		{
			buttons[k].scale = Math.Max(1f, buttons[k].scale - 0.025f);
			Vector2 location = new Vector2(Game1.uiViewport.Width / 2 - 384 + k * 64, yPositionOnScreen - 96 + 8);
			if (Game1.player.Items.Count > k && Game1.player.Items[k] != null)
			{
				Game1.player.Items[k].drawInMenu(b, location, (Game1.player.CurrentToolIndex == k) ? 0.9f : (buttons[k].scale * 0.8f), transparency, 0.88f);
			}
		}
		if (hoverItem != null)
		{
			IClickableMenu.drawToolTip(b, hoverItem.getDescription(), hoverItem.DisplayName, hoverItem);
			hoverItem = null;
		}
	}
}
