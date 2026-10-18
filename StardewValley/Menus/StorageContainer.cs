using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StardewValley.Menus;

public class StorageContainer : MenuWithInventory
{
	public delegate bool behaviorOnItemChange(Item i, int position, Item old, StorageContainer container, bool onRemoval = false);

	public InventoryMenu ItemsToGrabMenu;

	private TemporaryAnimatedSprite poof;

	private behaviorOnItemChange itemChangeBehavior;

	public StorageContainer(IList<Item> inventory, int capacity, int rows = 3, behaviorOnItemChange itemChangeBehavior = null, InventoryMenu.highlightThisItem highlightMethod = null)
		: base(highlightMethod, okButton: true, trashCan: true)
	{
		this.itemChangeBehavior = itemChangeBehavior;
		int num = 64 * (capacity / rows);
		ItemsToGrabMenu = new InventoryMenu(Game1.uiViewport.Width / 2 - num / 2, yPositionOnScreen + 64, playerInventory: false, inventory, null, capacity, rows);
		for (int i = 0; i < ItemsToGrabMenu.actualInventory.Count; i++)
		{
			if (i >= ItemsToGrabMenu.actualInventory.Count - ItemsToGrabMenu.capacity / ItemsToGrabMenu.rows)
			{
				ItemsToGrabMenu.inventory[i].downNeighborID = i + 53910;
			}
		}
		for (int j = 0; j < base.inventory.inventory.Count; j++)
		{
			base.inventory.inventory[j].myID = j + 53910;
			if (base.inventory.inventory[j].downNeighborID != -1)
			{
				base.inventory.inventory[j].downNeighborID += 53910;
			}
			if (base.inventory.inventory[j].rightNeighborID != -1)
			{
				base.inventory.inventory[j].rightNeighborID += 53910;
			}
			if (base.inventory.inventory[j].leftNeighborID != -1)
			{
				base.inventory.inventory[j].leftNeighborID += 53910;
			}
			if (base.inventory.inventory[j].upNeighborID != -1)
			{
				base.inventory.inventory[j].upNeighborID += 53910;
			}
			if (j < 12)
			{
				base.inventory.inventory[j].upNeighborID = ItemsToGrabMenu.actualInventory.Count - ItemsToGrabMenu.capacity / ItemsToGrabMenu.rows;
			}
		}
		dropItemInvisibleButton.myID = -500;
		ItemsToGrabMenu.dropItemInvisibleButton.myID = -500;
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			setCurrentlySnappedComponentTo(53910);
			snapCursorToCurrentSnappedComponent();
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		int num = 64 * (ItemsToGrabMenu.capacity / ItemsToGrabMenu.rows);
		ItemsToGrabMenu = new InventoryMenu(Game1.uiViewport.Width / 2 - num / 2, yPositionOnScreen + 64, playerInventory: false, ItemsToGrabMenu.actualInventory, null, ItemsToGrabMenu.capacity, ItemsToGrabMenu.rows);
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		Item item = base.heldItem;
		int num = item?.Stack ?? (-1);
		if (base.isWithinBounds(x, y))
		{
			base.receiveLeftClick(x, y, playSound: false);
			if (itemChangeBehavior == null && item == null && base.heldItem != null && Game1.oldKBState.IsKeyDown(Keys.LeftShift))
			{
				base.heldItem = ItemsToGrabMenu.tryToAddItem(base.heldItem, "Ship");
			}
		}
		bool flag = true;
		if (ItemsToGrabMenu.isWithinBounds(x, y))
		{
			base.heldItem = ItemsToGrabMenu.leftClick(x, y, base.heldItem, playSound: false);
			if ((base.heldItem != null && item == null) || (base.heldItem != null && item != null && !base.heldItem.Equals(item)))
			{
				if (itemChangeBehavior != null)
				{
					flag = itemChangeBehavior(base.heldItem, ItemsToGrabMenu.getInventoryPositionOfClick(x, y), item, this, onRemoval: true);
				}
				if (flag)
				{
					Game1.playSound("dwop");
				}
			}
			if ((base.heldItem == null && item != null) || (base.heldItem != null && item != null && !base.heldItem.Equals(item)))
			{
				Item one = base.heldItem;
				if (base.heldItem == null && ItemsToGrabMenu.getItemAt(x, y) != null && num < ItemsToGrabMenu.getItemAt(x, y).Stack)
				{
					one = item.getOne();
					one.Stack = num;
				}
				if (itemChangeBehavior != null)
				{
					flag = itemChangeBehavior(item, ItemsToGrabMenu.getInventoryPositionOfClick(x, y), one, this);
				}
				if (flag)
				{
					Game1.playSound("Ship");
				}
			}
			Item item2 = base.heldItem;
			if (item2 != null && item2.IsRecipe)
			{
				base.heldItem.LearnRecipe();
				poof = new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, new Vector2(x - x % 64 + 16, y - y % 64 + 16), flicker: false, flipped: false);
				Game1.playSound("newRecipe");
				base.heldItem = null;
			}
			else if (Game1.oldKBState.IsKeyDown(Keys.LeftShift) && Game1.player.addItemToInventoryBool(base.heldItem))
			{
				base.heldItem = null;
				if (itemChangeBehavior != null)
				{
					flag = itemChangeBehavior(base.heldItem, ItemsToGrabMenu.getInventoryPositionOfClick(x, y), item, this, onRemoval: true);
				}
				if (flag)
				{
					Game1.playSound("coin");
				}
			}
		}
		if (okButton.containsPoint(x, y) && readyToClose())
		{
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
		}
		if (trashCan.containsPoint(x, y) && base.heldItem != null && base.heldItem.canBeTrashed())
		{
			Utility.trashItem(base.heldItem);
			base.heldItem = null;
		}
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		int num = ((base.heldItem != null) ? base.heldItem.Stack : 0);
		Item item = base.heldItem;
		if (base.isWithinBounds(x, y))
		{
			base.receiveRightClick(x, y, playSound: true);
			if (itemChangeBehavior == null && item == null && base.heldItem != null && Game1.oldKBState.IsKeyDown(Keys.LeftShift))
			{
				base.heldItem = ItemsToGrabMenu.tryToAddItem(base.heldItem, "Ship");
			}
		}
		if (ItemsToGrabMenu.isWithinBounds(x, y))
		{
			base.heldItem = ItemsToGrabMenu.rightClick(x, y, base.heldItem, playSound: false);
			if ((base.heldItem != null && item == null) || (base.heldItem != null && item != null && !base.heldItem.Equals(item)) || (base.heldItem != null && item != null && base.heldItem.Equals(item) && base.heldItem.Stack != num))
			{
				itemChangeBehavior?.Invoke(base.heldItem, ItemsToGrabMenu.getInventoryPositionOfClick(x, y), item, this, onRemoval: true);
				Game1.playSound("dwop");
			}
			if ((base.heldItem == null && item != null) || (base.heldItem != null && item != null && !base.heldItem.Equals(item)))
			{
				itemChangeBehavior?.Invoke(item, ItemsToGrabMenu.getInventoryPositionOfClick(x, y), base.heldItem, this);
				Game1.playSound("Ship");
			}
			Item item2 = base.heldItem;
			if (item2 != null && item2.IsRecipe)
			{
				base.heldItem.LearnRecipe();
				poof = new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, new Vector2(x - x % 64 + 16, y - y % 64 + 16), flicker: false, flipped: false);
				Game1.playSound("newRecipe");
				base.heldItem = null;
			}
			else if (Game1.oldKBState.IsKeyDown(Keys.LeftShift) && Game1.player.addItemToInventoryBool(base.heldItem))
			{
				base.heldItem = null;
				Game1.playSound("coin");
				itemChangeBehavior?.Invoke(base.heldItem, ItemsToGrabMenu.getInventoryPositionOfClick(x, y), item, this, onRemoval: true);
			}
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		if (poof != null && poof.update(time))
		{
			poof = null;
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		ItemsToGrabMenu.hover(x, y, base.heldItem);
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
		base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);
		Game1.drawDialogueBox(ItemsToGrabMenu.xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder, ItemsToGrabMenu.yPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder, ItemsToGrabMenu.width + IClickableMenu.borderWidth * 2 + IClickableMenu.spaceToClearSideBorder * 2, ItemsToGrabMenu.height + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth * 2, speaker: false, drawOnlyBox: true);
		ItemsToGrabMenu.draw(b);
		poof?.draw(b, localPosition: true);
		if (!hoverText.Equals(""))
		{
			IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);
		}
		base.heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16), 1f);
		drawMouse(b);
		string text = ItemsToGrabMenu.descriptionTitle;
		if (text != null && text.Length > 1)
		{
			IClickableMenu.drawHoverText(b, ItemsToGrabMenu.descriptionTitle, Game1.smallFont, 32 + ((base.heldItem != null) ? 16 : (-21)), 32 + ((base.heldItem != null) ? 16 : (-21)));
		}
	}
}
