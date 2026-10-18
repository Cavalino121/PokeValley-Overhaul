using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Menus;

public class QuestContainerMenu : MenuWithInventory
{
	public enum ChangeType
	{
		None,
		Place,
		Grab
	}

	public InventoryMenu ItemsToGrabMenu;

	public Func<Item, int> stackCapacityCheck;

	public Action onItemChanged;

	public Action onConfirm;

	public QuestContainerMenu(IList<Item> inventory, int rows = 3, InventoryMenu.highlightThisItem highlight_method = null, Func<Item, int> stack_capacity_check = null, Action on_item_changed = null, Action on_confirm = null)
		: base(highlight_method, okButton: true)
	{
		onItemChanged = (Action)Delegate.Combine(onItemChanged, on_item_changed);
		onConfirm = (Action)Delegate.Combine(onConfirm, on_confirm);
		int count = inventory.Count;
		int num = 64 * (count / rows);
		ItemsToGrabMenu = new InventoryMenu(Game1.uiViewport.Width / 2 - num / 2, yPositionOnScreen + 64, playerInventory: false, inventory, null, count, rows);
		stackCapacityCheck = stack_capacity_check;
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
			foreach (ClickableComponent item in base.inventory.GetBorder(InventoryMenu.BorderSide.Right))
			{
				item.rightNeighborID = okButton.myID;
			}
		}
		dropItemInvisibleButton.myID = -500;
		ItemsToGrabMenu.dropItemInvisibleButton.myID = -500;
		populateClickableComponentList();
		if (Game1.options.SnappyMenus)
		{
			setCurrentlySnappedComponentTo(53910);
			snapCursorToCurrentSnappedComponent();
		}
	}

	public virtual int GetDonatableAmount(Item item)
	{
		if (item == null)
		{
			return 0;
		}
		int num = item.Stack;
		if (stackCapacityCheck != null)
		{
			num = Math.Min(num, stackCapacityCheck(item));
		}
		return num;
	}

	public virtual Item TryToGrab(Item item, int amount)
	{
		int num = Math.Min(amount, item.Stack);
		if (num == 0)
		{
			return item;
		}
		Item one = item.getOne();
		one.Stack = num;
		item.Stack -= num;
		InventoryMenu.highlightThisItem highlightMethod = inventory.highlightMethod;
		inventory.highlightMethod = InventoryMenu.highlightAllItems;
		Item item2 = inventory.tryToAddItem(one);
		inventory.highlightMethod = highlightMethod;
		if (item2 != null)
		{
			item.Stack += item2.Stack;
		}
		onItemChanged?.Invoke();
		if (item.Stack <= 0)
		{
			return null;
		}
		return item;
	}

	public virtual Item TryToPlace(Item item, int amount)
	{
		int num = Math.Min(amount, GetDonatableAmount(item));
		if (num == 0)
		{
			return item;
		}
		Item one = item.getOne();
		one.Stack = num;
		item.Stack -= num;
		Item item2 = ItemsToGrabMenu.tryToAddItem(one, "Ship");
		if (item2 != null)
		{
			item.Stack += item2.Stack;
		}
		onItemChanged?.Invoke();
		if (item.Stack <= 0)
		{
			return null;
		}
		return item;
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (base.isWithinBounds(x, y))
		{
			Item itemAt = inventory.getItemAt(x, y);
			if (itemAt != null)
			{
				int inventoryPositionOfClick = inventory.getInventoryPositionOfClick(x, y);
				inventory.actualInventory[inventoryPositionOfClick] = TryToPlace(itemAt, itemAt.Stack);
			}
		}
		if (ItemsToGrabMenu.isWithinBounds(x, y))
		{
			Item itemAt2 = ItemsToGrabMenu.getItemAt(x, y);
			if (itemAt2 != null)
			{
				int inventoryPositionOfClick2 = ItemsToGrabMenu.getInventoryPositionOfClick(x, y);
				ItemsToGrabMenu.actualInventory[inventoryPositionOfClick2] = TryToGrab(itemAt2, itemAt2.Stack);
			}
		}
		if (okButton.containsPoint(x, y) && readyToClose())
		{
			exitThisMenu();
		}
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (base.isWithinBounds(x, y))
		{
			Item itemAt = inventory.getItemAt(x, y);
			if (itemAt != null)
			{
				int inventoryPositionOfClick = inventory.getInventoryPositionOfClick(x, y);
				inventory.actualInventory[inventoryPositionOfClick] = TryToPlace(itemAt, 1);
			}
		}
		if (ItemsToGrabMenu.isWithinBounds(x, y))
		{
			Item itemAt2 = ItemsToGrabMenu.getItemAt(x, y);
			if (itemAt2 != null)
			{
				int inventoryPositionOfClick2 = ItemsToGrabMenu.getInventoryPositionOfClick(x, y);
				ItemsToGrabMenu.actualInventory[inventoryPositionOfClick2] = TryToGrab(itemAt2, 1);
			}
		}
	}

	/// <inheritdoc />
	protected override void cleanupBeforeExit()
	{
		onConfirm?.Invoke();
		base.cleanupBeforeExit();
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
