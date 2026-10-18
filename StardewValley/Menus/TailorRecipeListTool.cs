using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.GameData.Crafting;
using StardewValley.Objects;

namespace StardewValley.Menus;

public class TailorRecipeListTool : IClickableMenu
{
	public Rectangle scrollView;

	public List<ClickableTextureComponent> recipeComponents = new List<ClickableTextureComponent>();

	public ClickableTextureComponent okButton;

	public float scrollY;

	public Dictionary<string, KeyValuePair<Item, Item>> _recipeLookup = new Dictionary<string, KeyValuePair<Item, Item>>();

	public Item hoveredItem;

	public string hoverText = "";

	public Dictionary<string, string> _recipeHoverTexts = new Dictionary<string, string>();

	public Dictionary<string, string> _recipeOutputIds = new Dictionary<string, string>();

	public Dictionary<string, Color> _recipeColors = new Dictionary<string, Color>();

	public TailorRecipeListTool()
		: base(Game1.uiViewport.Width / 2 - (632 + IClickableMenu.borderWidth * 2) / 2, Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2 - 64, 632 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2 + 64)
	{
		TailoringMenu tailoringMenu = new TailoringMenu();
		Game1.player.faceDirection(2);
		Game1.player.FarmerSprite.StopAnimation();
		Item item = ItemRegistry.Create<Object>("(O)428");
		foreach (string allId in ItemRegistry.GetObjectTypeDefinition().GetAllIds())
		{
			Object obj = new Object(allId, 1);
			if (obj.Name.Contains("Seeds") || obj.Name.Contains("Floor") || obj.Name.Equals("Lumber") || obj.Name.Contains("Fence") || obj.Name.Equals("Gate") || obj.Name.Contains("Starter") || obj.Name.Equals("Secret Note") || obj.Name.Contains("Guide") || obj.Name.Contains("Path") || obj.Name.Contains("Ring") || obj.category.Value == -22 || obj.Category == -999 || obj.isSapling())
			{
				continue;
			}
			Item item2 = tailoringMenu.CraftItem(item, obj);
			TailorItemRecipe recipeForItems = tailoringMenu.GetRecipeForItems(item, obj);
			KeyValuePair<Item, Item> value = new KeyValuePair<Item, Item>(obj, item2);
			_recipeLookup[Utility.getStandardDescriptionFromItem(obj, 1)] = value;
			string text = "";
			Color? dyeColor = TailoringMenu.GetDyeColor(obj);
			if (dyeColor.HasValue)
			{
				_recipeColors[Utility.getStandardDescriptionFromItem(obj, 1)] = dyeColor.Value;
			}
			if (recipeForItems != null)
			{
				text = "clothes id: " + recipeForItems.CraftedItemId + " from ";
				foreach (string secondItemTag in recipeForItems.SecondItemTags)
				{
					text = text + secondItemTag + " ";
				}
				text.Trim();
			}
			_recipeOutputIds[Utility.getStandardDescriptionFromItem(obj, 1)] = TailoringMenu.ConvertLegacyItemId(recipeForItems?.CraftedItemId) ?? item2.QualifiedItemId;
			_recipeHoverTexts[Utility.getStandardDescriptionFromItem(obj, 1)] = text;
			ClickableTextureComponent item3 = new ClickableTextureComponent(new Rectangle(0, 0, 64, 64), null, default(Rectangle), 1f)
			{
				myID = 0,
				name = Utility.getStandardDescriptionFromItem(obj, 1),
				label = obj.DisplayName
			};
			recipeComponents.Add(item3);
		}
		okButton = new ClickableTextureComponent("OK", new Rectangle(xPositionOnScreen + width - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 64, yPositionOnScreen + height - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder + 16, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
		{
			upNeighborID = -99998,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			downNeighborID = -99998
		};
		RepositionElements();
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		xPositionOnScreen = Game1.uiViewport.Width / 2 - (632 + IClickableMenu.borderWidth * 2) / 2;
		yPositionOnScreen = Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2 - 64;
		RepositionElements();
	}

	private void RepositionElements()
	{
		scrollView = new Rectangle(xPositionOnScreen + IClickableMenu.borderWidth, yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder, width - IClickableMenu.borderWidth, 500);
		if (scrollView.Left < Game1.graphics.GraphicsDevice.ScissorRectangle.Left)
		{
			int num = Game1.graphics.GraphicsDevice.ScissorRectangle.Left - scrollView.Left;
			scrollView.X += num;
			scrollView.Width -= num;
		}
		if (scrollView.Right > Game1.graphics.GraphicsDevice.ScissorRectangle.Right)
		{
			int num2 = scrollView.Right - Game1.graphics.GraphicsDevice.ScissorRectangle.Right;
			scrollView.X -= num2;
			scrollView.Width -= num2;
		}
		if (scrollView.Top < Game1.graphics.GraphicsDevice.ScissorRectangle.Top)
		{
			int num3 = Game1.graphics.GraphicsDevice.ScissorRectangle.Top - scrollView.Top;
			scrollView.Y += num3;
			scrollView.Width -= num3;
		}
		if (scrollView.Bottom > Game1.graphics.GraphicsDevice.ScissorRectangle.Bottom)
		{
			int num4 = scrollView.Bottom - Game1.graphics.GraphicsDevice.ScissorRectangle.Bottom;
			scrollView.Y -= num4;
			scrollView.Width -= num4;
		}
		RepositionScrollElements();
	}

	public void RepositionScrollElements()
	{
		int num = (int)scrollY;
		if (scrollY > 0f)
		{
			scrollY = 0f;
		}
		foreach (ClickableTextureComponent recipeComponent in recipeComponents)
		{
			recipeComponent.bounds.X = scrollView.X;
			recipeComponent.bounds.Y = scrollView.Y + num;
			num += recipeComponent.bounds.Height;
			if (scrollView.Intersects(recipeComponent.bounds))
			{
				recipeComponent.visible = true;
			}
			else
			{
				recipeComponent.visible = false;
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
		foreach (ClickableTextureComponent recipeComponent in recipeComponents)
		{
			if (!recipeComponent.bounds.Contains(x, y) || !scrollView.Contains(x, y))
			{
				continue;
			}
			try
			{
				Item item = ItemRegistry.Create(_recipeOutputIds[recipeComponent.name]);
				if (item is Clothing clothing && _recipeColors.TryGetValue(recipeComponent.name, out var value))
				{
					clothing.Dye(value, 1f);
				}
				Game1.player.addItemToInventoryBool(item);
			}
			catch (Exception)
			{
			}
		}
		if (okButton.containsPoint(x, y))
		{
			exitThisMenu();
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		scrollY += direction;
		RepositionScrollElements();
		base.receiveScrollWheelAction(direction);
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoveredItem = null;
		hoverText = "";
		foreach (ClickableTextureComponent recipeComponent in recipeComponents)
		{
			if (recipeComponent.containsPoint(x, y))
			{
				hoveredItem = _recipeLookup[recipeComponent.name].Value;
				hoverText = _recipeHoverTexts[recipeComponent.name];
			}
		}
	}

	public bool canLeaveMenu()
	{
		return true;
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, speaker: false, drawOnlyBox: true);
		b.End();
		Rectangle scissorRectangle = b.GraphicsDevice.ScissorRectangle;
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, Utility.ScissorEnabled);
		b.GraphicsDevice.ScissorRectangle = scrollView;
		foreach (ClickableTextureComponent recipeComponent in recipeComponents)
		{
			if (recipeComponent.visible)
			{
				drawHorizontalPartition(b, recipeComponent.bounds.Bottom - 32, small: true);
				KeyValuePair<Item, Item> keyValuePair = _recipeLookup[recipeComponent.name];
				recipeComponent.draw(b);
				keyValuePair.Key.drawInMenu(b, new Vector2(recipeComponent.bounds.X, recipeComponent.bounds.Y), 1f);
				if (_recipeColors.TryGetValue(recipeComponent.name, out var value))
				{
					int num = 24;
					b.Draw(Game1.staminaRect, new Rectangle(scrollView.Left + scrollView.Width / 2 - num / 2, recipeComponent.bounds.Center.Y - num / 2, num, num), value);
				}
				keyValuePair.Value?.drawInMenu(b, new Vector2(scrollView.Left + scrollView.Width - 128, recipeComponent.bounds.Y), 1f);
			}
		}
		b.End();
		b.GraphicsDevice.ScissorRectangle = scissorRectangle;
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		okButton.draw(b);
		drawMouse(b);
		if (hoveredItem != null)
		{
			Utility.drawTextWithShadow(b, hoverText, Game1.smallFont, new Vector2(xPositionOnScreen + IClickableMenu.borderWidth, yPositionOnScreen + height - 64), Color.Black);
			if (!Game1.oldKBState.IsKeyDown(Keys.LeftShift))
			{
				IClickableMenu.drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem);
			}
		}
	}
}
