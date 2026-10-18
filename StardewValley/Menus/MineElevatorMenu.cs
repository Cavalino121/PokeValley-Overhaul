using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Locations;

namespace StardewValley.Menus;

public class MineElevatorMenu : IClickableMenu
{
	public List<ClickableComponent> elevators = new List<ClickableComponent>();

	public MineElevatorMenu()
		: base(0, 0, 0, 0, showUpperRightCloseButton: true)
	{
		int num = Math.Min(MineShaft.lowestLevelReached, 120) / 5;
		width = ((num > 50) ? (484 + IClickableMenu.borderWidth * 2) : Math.Min(220 + IClickableMenu.borderWidth * 2, num * 44 + IClickableMenu.borderWidth * 2));
		height = Math.Max(64 + IClickableMenu.borderWidth * 3, num * 44 / (width - IClickableMenu.borderWidth) * 44 + 64 + IClickableMenu.borderWidth * 3);
		xPositionOnScreen = Game1.uiViewport.Width / 2 - width / 2;
		yPositionOnScreen = Game1.uiViewport.Height / 2 - height / 2;
		Game1.playSound("crystal", 0);
		int num2 = width / 44 - 1;
		int num3 = xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearSideBorder * 3 / 4;
		int num4 = yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.borderWidth / 3;
		elevators.Add(new ClickableComponent(new Rectangle(num3, num4, 44, 44), 0.ToString() ?? "")
		{
			myID = 0,
			rightNeighborID = 1,
			downNeighborID = num2
		});
		num3 = num3 + 64 - 20;
		if (num3 > xPositionOnScreen + width - IClickableMenu.borderWidth)
		{
			num3 = xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearSideBorder * 3 / 4;
			num4 += 44;
		}
		for (int i = 1; i <= num; i++)
		{
			elevators.Add(new ClickableComponent(new Rectangle(num3, num4, 44, 44), (i * 5).ToString() ?? "")
			{
				myID = i,
				rightNeighborID = ((i % num2 == num2 - 1) ? (-1) : (i + 1)),
				leftNeighborID = ((i % num2 == 0) ? (-1) : (i - 1)),
				downNeighborID = i + num2,
				upNeighborID = i - num2
			});
			num3 = num3 + 64 - 20;
			if (num3 > xPositionOnScreen + width - IClickableMenu.borderWidth)
			{
				num3 = xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearSideBorder * 3 / 4;
				num4 += 44;
			}
		}
		initializeUpperRightCloseButton();
		if (Game1.options.snappyMenus && Game1.options.gamepadControls)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(0);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (isWithinBounds(x, y))
		{
			foreach (ClickableComponent elevator in elevators)
			{
				if (!elevator.containsPoint(x, y))
				{
					continue;
				}
				Game1.playSound("smallSelect");
				if (Convert.ToInt32(elevator.name) == 0)
				{
					if (!(Game1.currentLocation is MineShaft))
					{
						return;
					}
					Game1.warpFarmer("Mine", 17, 4, flip: true);
					Game1.exitActiveMenu();
					continue;
				}
				if (Convert.ToInt32(elevator.name) == Game1.CurrentMineLevel)
				{
					return;
				}
				Game1.player.ridingMineElevator = true;
				Game1.enterMine(Convert.ToInt32(elevator.name));
				Game1.exitActiveMenu();
			}
			base.receiveLeftClick(x, y);
		}
		else
		{
			Game1.exitActiveMenu();
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		foreach (ClickableComponent elevator in elevators)
		{
			if (elevator.containsPoint(x, y))
			{
				elevator.scale = 2f;
			}
			else
			{
				elevator.scale = 1f;
			}
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (!Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
		}
		Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen - 64 + 8, width + 21, height + 64, speaker: false, drawOnlyBox: true);
		foreach (ClickableComponent elevator in elevators)
		{
			b.Draw(Game1.mouseCursors, new Vector2(elevator.bounds.X - 4, elevator.bounds.Y + 4), new Rectangle((elevator.scale > 1f) ? 267 : 256, 256, 10, 10), Color.Black * 0.5f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.865f);
			b.Draw(Game1.mouseCursors, new Vector2(elevator.bounds.X, elevator.bounds.Y), new Rectangle((elevator.scale > 1f) ? 267 : 256, 256, 10, 10), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.868f);
			NumberSprite.draw(position: new Vector2(elevator.bounds.X + 16 + NumberSprite.numberOfDigits(Convert.ToInt32(elevator.name)) * 6, elevator.bounds.Y + 24 - NumberSprite.getHeight() / 4), number: Convert.ToInt32(elevator.name), b: b, c: (Game1.CurrentMineLevel == Convert.ToInt32(elevator.name)) ? (Color.Gray * 0.75f) : Color.Gold, scale: 0.5f, layerDepth: 0.86f, alpha: 1f, secondDigitOffset: 0);
		}
		base.draw(b);
		drawMouse(b);
	}
}
