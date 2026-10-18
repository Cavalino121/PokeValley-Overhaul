using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Menus;

public class AboutMenu : IClickableMenu
{
	public const int region_upArrow = 94444;

	public const int region_downArrow = 95555;

	public new const int height = 700;

	public ClickableComponent backButton;

	public ClickableTextureComponent upButton;

	public ClickableTextureComponent downButton;

	public List<ICreditsBlock> credits = new List<ICreditsBlock>();

	private int currentCreditsIndex;

	public AboutMenu()
	{
		width = 1280;
		base.height = 700;
		SetUpCredits();
		if (Game1.options.snappyMenus && Game1.options.gamepadControls)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	public void SetUpCredits()
	{
		foreach (string item in Game1.temporaryContent.Load<List<string>>("Strings\\credits"))
		{
			if (item != null && item.Length >= 6 && item.StartsWith("[image"))
			{
				string[] array = ArgUtility.SplitBySpace(item);
				string assetName = array[1];
				int x = Convert.ToInt32(array[2]);
				int y = Convert.ToInt32(array[3]);
				int num = Convert.ToInt32(array[4]);
				int num2 = Convert.ToInt32(array[5]);
				int pixelZoom = Convert.ToInt32(array[6]);
				int animationFrames = ((array.Length <= 7) ? 1 : Convert.ToInt32(array[7]));
				Texture2D texture2D = null;
				try
				{
					texture2D = Game1.temporaryContent.Load<Texture2D>(assetName);
				}
				catch (Exception)
				{
				}
				if (texture2D != null)
				{
					if (num == -1)
					{
						num = texture2D.Width;
						num2 = texture2D.Height;
					}
					credits.Add(new ImageCreditsBlock(texture2D, new Rectangle(x, y, num, num2), pixelZoom, animationFrames));
				}
			}
			else if (item != null && item.Length >= 6 && item.StartsWith("[link"))
			{
				string[] array2 = ArgUtility.SplitBySpace(item, 3);
				string url = array2[1];
				string text = array2[2];
				credits.Add(new LinkCreditsBlock(text, url));
			}
			else
			{
				credits.Add(new TextCreditsBlock(item));
			}
		}
		Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(width, base.height);
		xPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.X;
		yPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.Y;
		upButton = new ClickableTextureComponent(new Rectangle((int)topLeftPositionForCenteringOnScreen.X + width - 80, (int)topLeftPositionForCenteringOnScreen.Y + 64 + 16, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 12), 0.8f)
		{
			myID = 94444,
			downNeighborID = 95555,
			rightNeighborID = -99998,
			leftNeighborID = -99998
		};
		downButton = new ClickableTextureComponent(new Rectangle((int)topLeftPositionForCenteringOnScreen.X + width - 80, (int)topLeftPositionForCenteringOnScreen.Y + base.height - 32, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 11), 0.8f)
		{
			myID = 95555,
			upNeighborID = -99998,
			rightNeighborID = -99998,
			leftNeighborID = -99998
		};
		backButton = new ClickableComponent(new Rectangle(Game1.uiViewport.Width + -66 * TitleMenu.pixelZoom - 8 * TitleMenu.pixelZoom * 2, Game1.uiViewport.Height - 27 * TitleMenu.pixelZoom - 8 * TitleMenu.pixelZoom, 66 * TitleMenu.pixelZoom, 27 * TitleMenu.pixelZoom), "")
		{
			myID = 81114,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			upNeighborID = 95555
		};
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(81114);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);
		if (upButton.containsPoint(x, y))
		{
			if (currentCreditsIndex > 0)
			{
				currentCreditsIndex--;
				Game1.playSound("shiny4");
				upButton.scale = upButton.baseScale;
			}
		}
		else if (downButton.containsPoint(x, y))
		{
			if (currentCreditsIndex < credits.Count - 1)
			{
				currentCreditsIndex++;
				Game1.playSound("shiny4");
				downButton.scale = downButton.baseScale;
			}
		}
		else
		{
			if (!isWithinBounds(x, y))
			{
				return;
			}
			int num = yPositionOnScreen + 96;
			int num2 = num;
			int num3 = 0;
			while (num < yPositionOnScreen + base.height - 64 && credits.Count > currentCreditsIndex + num3)
			{
				num += credits[currentCreditsIndex + num3].getHeight(width - 64) + ((credits.Count <= currentCreditsIndex + num3 + 1 || !(credits[currentCreditsIndex + num3 + 1] is ImageCreditsBlock)) ? 8 : 0);
				if (y >= num2 && y < num)
				{
					credits[currentCreditsIndex + num3].clicked();
					break;
				}
				num3++;
				num2 = num;
			}
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		upButton.visible = currentCreditsIndex > 0;
		downButton.visible = currentCreditsIndex < credits.Count - 1;
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		if (direction > 0 && currentCreditsIndex > 0)
		{
			currentCreditsIndex--;
			Game1.playSound("shiny4");
		}
		else if (direction < 0 && currentCreditsIndex < credits.Count - 1)
		{
			currentCreditsIndex++;
			Game1.playSound("shiny4");
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		upButton.tryHover(x, y);
		downButton.tryHover(x, y);
		if (!isWithinBounds(x, y))
		{
			return;
		}
		int num = yPositionOnScreen + 96;
		int num2 = num;
		int num3 = 0;
		while (num < yPositionOnScreen + base.height - 64 && credits.Count > currentCreditsIndex + num3)
		{
			num += credits[currentCreditsIndex + num3].getHeight(width - 64) + ((credits.Count <= currentCreditsIndex + num3 + 1 || !(credits[currentCreditsIndex + num3 + 1] is ImageCreditsBlock)) ? 8 : 0);
			if (y >= num2 && y < num)
			{
				credits[currentCreditsIndex + num3].hovered();
				break;
			}
			num3++;
			num2 = num;
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(width, base.height - 100);
		if (!Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
		}
		IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(473, 36, 24, 24), (int)topLeftPositionForCenteringOnScreen.X, (int)topLeftPositionForCenteringOnScreen.Y, width, base.height, Color.White, 4f, drawShadow: false);
		int num = yPositionOnScreen + 96;
		int num2 = 0;
		while (num < yPositionOnScreen + base.height - 64 && credits.Count > currentCreditsIndex + num2)
		{
			credits[currentCreditsIndex + num2].draw(xPositionOnScreen + 32, num, width - 64, b);
			num += credits[currentCreditsIndex + num2].getHeight(width - 64) + ((credits.Count <= currentCreditsIndex + num2 + 1 || !(credits[currentCreditsIndex + num2 + 1] is ImageCreditsBlock)) ? 8 : 0);
			num2++;
		}
		if (currentCreditsIndex > 0)
		{
			upButton.draw(b);
		}
		if (currentCreditsIndex < credits.Count - 1)
		{
			downButton.draw(b);
		}
		string text = "v" + Game1.GetVersionString();
		float y = Game1.smallFont.MeasureString(text).Y;
		b.DrawString(Game1.smallFont, text, new Vector2(16f, (float)Game1.uiViewport.Height - y - 8f), Color.White);
		if (Game1.activeClickableMenu is TitleMenu titleMenu && !string.IsNullOrWhiteSpace(titleMenu.startupMessage))
		{
			string text2 = Game1.parseText(titleMenu.startupMessage, Game1.smallFont, 640);
			float y2 = Game1.smallFont.MeasureString(text2).Y;
			b.DrawString(Game1.smallFont, text2, new Vector2(8f, (float)Game1.uiViewport.Height - y - y2 - 4f), Color.White);
		}
		base.draw(b);
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		SetUpCredits();
		if (Game1.options.snappyMenus && Game1.options.gamepadControls)
		{
			int id = ((currentlySnappedComponent != null) ? currentlySnappedComponent.myID : 81114);
			populateClickableComponentList();
			currentlySnappedComponent = getComponentWithID(id);
			snapCursorToCurrentSnappedComponent();
		}
	}
}
