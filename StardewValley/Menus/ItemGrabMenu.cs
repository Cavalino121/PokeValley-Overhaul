using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace StardewValley.Menus;

public class ItemGrabMenu : MenuWithInventory
{
	public delegate void behaviorOnItemSelect(Item item, Farmer who);

	public class TransferredItemSprite
	{
		public Item item;

		public Vector2 position;

		public float age;

		public float alpha = 1f;

		public TransferredItemSprite(Item transferred_item, int start_x, int start_y)
		{
			item = transferred_item;
			position.X = start_x;
			position.Y = start_y;
		}

		public bool Update(GameTime time)
		{
			float num = 0.15f;
			position.Y -= (float)time.ElapsedGameTime.TotalSeconds * 128f;
			age += (float)time.ElapsedGameTime.TotalSeconds;
			alpha = 1f - age / num;
			if (age >= num)
			{
				return true;
			}
			return false;
		}

		public void Draw(SpriteBatch b)
		{
			item.drawInMenu(b, position, 1f, alpha, 0.9f, StackDrawType.Hide, Color.White, drawShadow: false);
		}
	}

	public const int region_organizationButtons = 15923;

	public const int region_itemsToGrabMenuModifier = 53910;

	public const int region_fillStacksButton = 12952;

	public const int region_organizeButton = 106;

	public const int region_colorPickToggle = 27346;

	public const int region_specialButton = 12485;

	public const int region_lastShippedHolder = 12598;

	/// <summary>The <see cref="F:StardewValley.Menus.ItemGrabMenu.source" /> value when a specific value doesn't apply.</summary>
	public const int source_none = 0;

	/// <summary>The <see cref="F:StardewValley.Menus.ItemGrabMenu.source" /> value when collecting items from a chest.</summary>
	public const int source_chest = 1;

	/// <summary>The <see cref="F:StardewValley.Menus.ItemGrabMenu.source" /> value when collecting items which couldn't be added directly to the player's inventory (e.g. from NPC dialogue).</summary>
	public const int source_gift = 2;

	/// <summary>The <see cref="F:StardewValley.Menus.ItemGrabMenu.source" /> value when collecting treasure found while fishing.</summary>
	public const int source_fishingChest = 3;

	/// <summary>The <see cref="F:StardewValley.Menus.ItemGrabMenu.source" /> value when collecting items which couldn't be added directly to the player's inventory via <see cref="M:StardewValley.Farmer.addItemByMenuIfNecessary(StardewValley.Item,StardewValley.Menus.ItemGrabMenu.behaviorOnItemSelect,System.Boolean)" />.</summary>
	public const int source_overflow = 4;

	public const int specialButton_junimotoggle = 1;

	/// <summary>The inventory from which the player can collect items.</summary>
	public InventoryMenu ItemsToGrabMenu;

	public TemporaryAnimatedSprite poof;

	public bool reverseGrab;

	public bool showReceivingMenu = true;

	public bool drawBG = true;

	public bool destroyItemOnClick;

	public bool canExitOnKey;

	public bool playRightClickSound;

	public bool allowRightClick;

	public bool shippingBin;

	public string message;

	/// <summary>The callback invoked when taking something out of the player inventory (e.g. putting something in the Luau soup), if any.</summary>
	public behaviorOnItemSelect behaviorFunction;

	/// <summary>The callback invoked when taking something from the menu (e.g. to put in the player's inventory), if any.</summary>
	public behaviorOnItemSelect behaviorOnItemGrab;

	/// <summary>The item for which the item menu was opened (e.g. the chest or storage furniture item being checked), if applicable.</summary>
	public Item sourceItem;

	public ClickableTextureComponent fillStacksButton;

	public ClickableTextureComponent organizeButton;

	public ClickableTextureComponent colorPickerToggleButton;

	public ClickableTextureComponent specialButton;

	public ClickableTextureComponent lastShippedHolder;

	public List<ClickableComponent> discreteColorPickerCC;

	/// <summary>The reason this menu was opened, usually matching a constant like <see cref="F:StardewValley.Menus.ItemGrabMenu.source_chest" />.</summary>
	public int source;

	public int whichSpecialButton;

	/// <summary>A contextual value for what opened the menu. This may be a chest, event, fishing rod, location, etc.</summary>
	public object context;

	public bool snappedtoBottom;

	public DiscreteColorPicker chestColorPicker;

	public bool essential;

	public bool superEssential;

	public int storageSpaceTopBorderOffset;

	/// <summary>Whether <see cref="M:StardewValley.Menus.ItemGrabMenu.update(Microsoft.Xna.Framework.GameTime)" /> has run at least once yet.</summary>
	private bool HasUpdateTicked;

	public List<TransferredItemSprite> _transferredItemSprites = new List<TransferredItemSprite>();

	/// <summary>Whether the source item was placed in the current location when the menu is opened.</summary>
	public bool _sourceItemInCurrentLocation;

	public ClickableTextureComponent junimoNoteIcon;

	public int junimoNotePulser;

	/// <summary>Construct an instance.</summary>
	/// <param name="inventory">The items that can be collected by the player.</param>
	/// <param name="context">A contextual value for what opened the menu. This may be a chest, event, fishing rod, location, etc.</param>
	public ItemGrabMenu(IList<Item> inventory, object context = null)
		: base(null, okButton: true, trashCan: true)
	{
		this.context = context;
		ItemsToGrabMenu = new InventoryMenu(xPositionOnScreen + 32, yPositionOnScreen, playerInventory: false, inventory);
		trashCan.myID = 106;
		ItemsToGrabMenu.populateClickableComponentList();
		for (int i = 0; i < ItemsToGrabMenu.inventory.Count; i++)
		{
			if (ItemsToGrabMenu.inventory[i] != null)
			{
				ItemsToGrabMenu.inventory[i].myID += 53910;
				ItemsToGrabMenu.inventory[i].upNeighborID += 53910;
				ItemsToGrabMenu.inventory[i].rightNeighborID += 53910;
				ItemsToGrabMenu.inventory[i].downNeighborID = -7777;
				ItemsToGrabMenu.inventory[i].leftNeighborID += 53910;
				ItemsToGrabMenu.inventory[i].fullyImmutable = true;
				if (i % (ItemsToGrabMenu.capacity / ItemsToGrabMenu.rows) == 0)
				{
					ItemsToGrabMenu.inventory[i].leftNeighborID = dropItemInvisibleButton.myID;
				}
				if (i % (ItemsToGrabMenu.capacity / ItemsToGrabMenu.rows) == ItemsToGrabMenu.capacity / ItemsToGrabMenu.rows - 1)
				{
					ItemsToGrabMenu.inventory[i].rightNeighborID = trashCan.myID;
				}
			}
		}
		for (int j = 0; j < GetColumnCount(); j++)
		{
			if (base.inventory?.inventory?.Count >= GetColumnCount())
			{
				base.inventory.inventory[j].upNeighborID = (shippingBin ? 12598 : (-7777));
			}
		}
		if (!shippingBin)
		{
			for (int k = 0; k < GetColumnCount() * 3; k++)
			{
				InventoryMenu inventoryMenu = base.inventory;
				if (inventoryMenu != null && inventoryMenu.inventory?.Count > k)
				{
					base.inventory.inventory[k].upNeighborID = -7777;
					base.inventory.inventory[k].upNeighborImmutable = true;
				}
			}
		}
		if (trashCan != null)
		{
			trashCan.leftNeighborID = 11;
		}
		if (okButton != null)
		{
			okButton.leftNeighborID = 11;
		}
		populateClickableComponentList();
		if (Game1.options.SnappyMenus)
		{
			snapToDefaultClickableComponent();
		}
		base.inventory.showGrayedOutSlots = true;
		SetupBorderNeighbors();
	}

	/// <summary>Drop any remaining items that weren't grabbed by the player onto the ground at their feet.</summary>
	public virtual void DropRemainingItems()
	{
		if (ItemsToGrabMenu?.actualInventory == null)
		{
			return;
		}
		foreach (Item item in ItemsToGrabMenu.actualInventory)
		{
			if (item != null)
			{
				Game1.createItemDebris(item, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
			}
		}
		ItemsToGrabMenu.actualInventory.Clear();
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="menu">The menu whose state to copy.</param>
	public ItemGrabMenu(ItemGrabMenu menu)
		: this(menu.ItemsToGrabMenu.actualInventory, menu.reverseGrab, menu.showReceivingMenu, menu.inventory.highlightMethod, menu.behaviorFunction, menu.message, menu.behaviorOnItemGrab, snapToBottom: false, menu.canExitOnKey, menu.playRightClickSound, menu.allowRightClick, menu.organizeButton != null, menu.source, menu.sourceItem, menu.whichSpecialButton, menu.context, menu.HeldItemExitBehavior, menu.AllowExitWithHeldItem)
	{
		setEssential(menu.essential);
		if (menu.currentlySnappedComponent != null)
		{
			setCurrentlySnappedComponentTo(menu.currentlySnappedComponent.myID);
			if (Game1.options.SnappyMenus)
			{
				snapCursorToCurrentSnappedComponent();
			}
		}
		base.heldItem = menu.heldItem;
	}

	public ItemGrabMenu(IList<Item> inventory, bool reverseGrab, bool showReceivingMenu, InventoryMenu.highlightThisItem highlightFunction, behaviorOnItemSelect behaviorOnItemSelectFunction, string message, behaviorOnItemSelect behaviorOnItemGrab = null, bool snapToBottom = false, bool canBeExitedWithKey = false, bool playRightClickSound = true, bool allowRightClick = true, bool showOrganizeButton = false, int source = 0, Item sourceItem = null, int whichSpecialButton = -1, object context = null, ItemExitBehavior heldItemExitBehavior = ItemExitBehavior.ReturnToPlayer, bool allowExitWithHeldItem = false)
		: base(highlightFunction, okButton: true, trashCan: true, 0, 0, 64, heldItemExitBehavior, allowExitWithHeldItem)
	{
		this.source = source;
		this.message = message;
		this.reverseGrab = reverseGrab;
		this.showReceivingMenu = showReceivingMenu;
		this.playRightClickSound = playRightClickSound;
		this.allowRightClick = allowRightClick;
		base.inventory.showGrayedOutSlots = true;
		this.sourceItem = sourceItem;
		this.whichSpecialButton = whichSpecialButton;
		this.context = context;
		if (sourceItem != null && Game1.currentLocation.objects.Values.Contains(sourceItem))
		{
			_sourceItemInCurrentLocation = true;
		}
		else
		{
			_sourceItemInCurrentLocation = false;
		}
		if (sourceItem is Chest chest)
		{
			if (CanHaveColorPicker())
			{
				Chest chest2 = new Chest(playerChest: true, sourceItem.ItemId);
				chestColorPicker = new DiscreteColorPicker(xPositionOnScreen, yPositionOnScreen - 64 - IClickableMenu.borderWidth * 2, chest.playerChoiceColor.Value, chest2);
				chest2.playerChoiceColor.Value = DiscreteColorPicker.getColorFromSelection(chestColorPicker.colorSelection);
				colorPickerToggleButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width, yPositionOnScreen + height / 3 - 64 + -160, 64, 64), Game1.mouseCursors, new Rectangle(119, 469, 16, 16), 4f)
				{
					hoverText = Game1.content.LoadString("Strings\\UI:Toggle_ColorPicker"),
					myID = 27346,
					downNeighborID = -99998,
					leftNeighborID = 53921,
					region = 15923
				};
			}
			if (source == 1 && (chest.SpecialChestType == Chest.SpecialChestTypes.None || chest.SpecialChestType == Chest.SpecialChestTypes.BigChest) && InventoryPage.ShouldShowJunimoNoteIcon())
			{
				junimoNoteIcon = new ClickableTextureComponent("", new Rectangle(xPositionOnScreen + width, yPositionOnScreen + height / 3 - 64 + -216, 64, 64), "", Game1.content.LoadString("Strings\\UI:GameMenu_JunimoNote_Hover"), Game1.mouseCursors, new Rectangle(331, 374, 15, 14), 4f)
				{
					myID = 898,
					leftNeighborID = 11,
					downNeighborID = 106
				};
			}
		}
		if (whichSpecialButton == 1)
		{
			specialButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width, yPositionOnScreen + height / 3 - 64 + -160, 64, 64), Game1.mouseCursors, new Rectangle(108, 491, 16, 16), 4f)
			{
				myID = 12485,
				downNeighborID = (showOrganizeButton ? 12952 : 5948),
				region = 15923,
				leftNeighborID = 53921
			};
			if (context is JunimoHut junimoHut)
			{
				specialButton.sourceRect.X = (junimoHut.noHarvest.Value ? 124 : 108);
			}
		}
		if (snapToBottom)
		{
			movePosition(0, Game1.uiViewport.Height - (yPositionOnScreen + height - IClickableMenu.spaceToClearTopBorder));
			snappedtoBottom = true;
		}
		if (source == 1 && sourceItem is Chest chest3 && chest3.GetActualCapacity() != 36)
		{
			int actualCapacity = chest3.GetActualCapacity();
			int num = ((actualCapacity >= 70) ? 5 : 3);
			if (actualCapacity < 9)
			{
				num = 1;
			}
			int num2 = 64 * (actualCapacity / num);
			ItemsToGrabMenu = new InventoryMenu(Game1.uiViewport.Width / 2 - num2 / 2, yPositionOnScreen + ((actualCapacity < 70) ? 64 : (-21)), playerInventory: false, inventory, highlightFunction, actualCapacity, num);
			if (chest3.SpecialChestType == Chest.SpecialChestTypes.MiniShippingBin)
			{
				base.inventory.moveItemSound = "Ship";
			}
			if (num > 3)
			{
				yPositionOnScreen += 42;
				base.inventory.SetPosition(base.inventory.xPositionOnScreen, base.inventory.yPositionOnScreen + 38 + 4);
				ItemsToGrabMenu.SetPosition(ItemsToGrabMenu.xPositionOnScreen - 32 + 8, ItemsToGrabMenu.yPositionOnScreen);
				storageSpaceTopBorderOffset = 20;
				trashCan.bounds.X = ItemsToGrabMenu.width + ItemsToGrabMenu.xPositionOnScreen + IClickableMenu.borderWidth * 2;
				okButton.bounds.X = ItemsToGrabMenu.width + ItemsToGrabMenu.xPositionOnScreen + IClickableMenu.borderWidth * 2;
			}
		}
		else
		{
			ItemsToGrabMenu = new InventoryMenu(xPositionOnScreen + 32, yPositionOnScreen, playerInventory: false, inventory, highlightFunction);
		}
		ItemsToGrabMenu.populateClickableComponentList();
		for (int i = 0; i < ItemsToGrabMenu.inventory.Count; i++)
		{
			if (ItemsToGrabMenu.inventory[i] != null)
			{
				ItemsToGrabMenu.inventory[i].myID += 53910;
				ItemsToGrabMenu.inventory[i].upNeighborID += 53910;
				ItemsToGrabMenu.inventory[i].rightNeighborID += 53910;
				ItemsToGrabMenu.inventory[i].downNeighborID = -7777;
				ItemsToGrabMenu.inventory[i].leftNeighborID += 53910;
				ItemsToGrabMenu.inventory[i].fullyImmutable = true;
			}
		}
		behaviorFunction = behaviorOnItemSelectFunction;
		this.behaviorOnItemGrab = behaviorOnItemGrab;
		canExitOnKey = canBeExitedWithKey;
		if (showOrganizeButton)
		{
			fillStacksButton = new ClickableTextureComponent("", new Rectangle(xPositionOnScreen + width, yPositionOnScreen + height / 3 - 64 - 64 - 16, 64, 64), "", Game1.content.LoadString("Strings\\UI:ItemGrab_FillStacks"), Game1.mouseCursors, new Rectangle(103, 469, 16, 16), 4f)
			{
				myID = 12952,
				upNeighborID = ((colorPickerToggleButton != null) ? 27346 : ((specialButton != null) ? 12485 : (-500))),
				downNeighborID = 106,
				leftNeighborID = 53921,
				region = 15923
			};
			organizeButton = new ClickableTextureComponent("", new Rectangle(xPositionOnScreen + width, yPositionOnScreen + height / 3 - 64, 64, 64), "", Game1.content.LoadString("Strings\\UI:ItemGrab_Organize"), Game1.mouseCursors, new Rectangle(162, 440, 16, 16), 4f)
			{
				myID = 106,
				upNeighborID = 12952,
				downNeighborID = 5948,
				leftNeighborID = 53921,
				region = 15923
			};
		}
		RepositionSideButtons();
		if (chestColorPicker != null)
		{
			discreteColorPickerCC = new List<ClickableComponent>();
			for (int j = 0; j < DiscreteColorPicker.totalColors; j++)
			{
				List<ClickableComponent> list = discreteColorPickerCC;
				ClickableComponent obj = new ClickableComponent(new Rectangle(chestColorPicker.xPositionOnScreen + IClickableMenu.borderWidth / 2 + j * 9 * 4, chestColorPicker.yPositionOnScreen + IClickableMenu.borderWidth / 2, 36, 28), "")
				{
					myID = j + 4343,
					rightNeighborID = ((j < DiscreteColorPicker.totalColors - 1) ? (j + 4343 + 1) : (-1)),
					leftNeighborID = ((j > 0) ? (j + 4343 - 1) : (-1))
				};
				InventoryMenu itemsToGrabMenu = ItemsToGrabMenu;
				obj.downNeighborID = ((itemsToGrabMenu != null && itemsToGrabMenu.inventory.Count > 0) ? 53910 : 0);
				list.Add(obj);
			}
		}
		if (organizeButton != null)
		{
			foreach (ClickableComponent item in ItemsToGrabMenu.GetBorder(InventoryMenu.BorderSide.Right))
			{
				item.rightNeighborID = organizeButton.myID;
			}
		}
		if (trashCan != null && base.inventory.inventory.Count >= 12 && base.inventory.inventory[11] != null)
		{
			base.inventory.inventory[11].rightNeighborID = 5948;
		}
		if (trashCan != null)
		{
			trashCan.leftNeighborID = 11;
		}
		if (okButton != null)
		{
			okButton.leftNeighborID = 11;
		}
		ClickableComponent clickableComponent = ItemsToGrabMenu.GetBorder(InventoryMenu.BorderSide.Right).FirstOrDefault();
		if (clickableComponent != null)
		{
			if (organizeButton != null)
			{
				organizeButton.leftNeighborID = clickableComponent.myID;
			}
			if (specialButton != null)
			{
				specialButton.leftNeighborID = clickableComponent.myID;
			}
			if (fillStacksButton != null)
			{
				fillStacksButton.leftNeighborID = clickableComponent.myID;
			}
			if (junimoNoteIcon != null)
			{
				junimoNoteIcon.leftNeighborID = clickableComponent.myID;
			}
		}
		populateClickableComponentList();
		if (Game1.options.SnappyMenus)
		{
			snapToDefaultClickableComponent();
		}
		SetupBorderNeighbors();
	}

	/// <summary>Create an item grab menu to collect items which couldn't be added to the player's inventory directly.</summary>
	/// <param name="items">The items to collect.</param>
	/// <param name="onCollectItem">The callback to invoke when an item is retrieved.</param>
	public static ItemGrabMenu CreateOverflowMenu(IList<Item> items, behaviorOnItemSelect onCollectItem = null)
	{
		ItemGrabMenu itemGrabMenu = new ItemGrabMenu(items).setEssential(essential: true);
		itemGrabMenu.inventory.showGrayedOutSlots = true;
		itemGrabMenu.inventory.onAddItem = onCollectItem;
		itemGrabMenu.source = 4;
		return itemGrabMenu;
	}

	/// <summary>Position the buttons that appear on the right side of the screen (e.g. to organize or fill stacks), and update their neighbor IDs.</summary>
	public virtual void RepositionSideButtons()
	{
		List<ClickableComponent> list = new List<ClickableComponent>();
		int num = ItemsToGrabMenu.capacity / ItemsToGrabMenu.rows;
		if (organizeButton != null)
		{
			organizeButton.leftNeighborID = num - 1 + 53910;
			list.Add(organizeButton);
		}
		if (fillStacksButton != null)
		{
			fillStacksButton.leftNeighborID = num - 1 + 53910;
			list.Add(fillStacksButton);
		}
		if (colorPickerToggleButton != null)
		{
			colorPickerToggleButton.leftNeighborID = num - 1 + 53910;
			list.Add(colorPickerToggleButton);
		}
		if (specialButton != null)
		{
			list.Add(specialButton);
		}
		if (junimoNoteIcon != null)
		{
			junimoNoteIcon.leftNeighborID = num - 1;
			list.Add(junimoNoteIcon);
		}
		int num2 = 80;
		if (list.Count >= 4)
		{
			num2 = 72;
		}
		for (int i = 0; i < list.Count; i++)
		{
			ClickableComponent clickableComponent = list[i];
			if (i > 0 && list.Count > 1)
			{
				clickableComponent.downNeighborID = list[i - 1].myID;
			}
			if (i < list.Count - 1 && list.Count > 1)
			{
				clickableComponent.upNeighborID = list[i + 1].myID;
			}
			clickableComponent.bounds.X = ItemsToGrabMenu.xPositionOnScreen + ItemsToGrabMenu.width + IClickableMenu.borderWidth * 2;
			clickableComponent.bounds.Y = ItemsToGrabMenu.yPositionOnScreen + height / 3 - 64 - num2 * i;
		}
	}

	public void SetupBorderNeighbors()
	{
		List<ClickableComponent> border = inventory.GetBorder(InventoryMenu.BorderSide.Right);
		foreach (ClickableComponent item in border)
		{
			item.rightNeighborID = -99998;
			item.rightNeighborImmutable = true;
		}
		border = ItemsToGrabMenu.GetBorder(InventoryMenu.BorderSide.Right);
		bool flag = false;
		foreach (ClickableComponent allClickableComponent in allClickableComponents)
		{
			if (allClickableComponent.region == 15923)
			{
				flag = true;
				break;
			}
		}
		foreach (ClickableComponent item2 in border)
		{
			if (flag)
			{
				item2.rightNeighborID = -99998;
				item2.rightNeighborImmutable = true;
			}
			else
			{
				item2.rightNeighborID = -1;
			}
		}
		for (int i = 0; i < GetColumnCount(); i++)
		{
			InventoryMenu inventoryMenu = inventory;
			ClickableComponent clickableComponent;
			int upNeighborID;
			if (inventoryMenu != null && inventoryMenu.inventory?.Count >= 12)
			{
				clickableComponent = inventory.inventory[i];
				if (!shippingBin)
				{
					if (discreteColorPickerCC != null)
					{
						InventoryMenu itemsToGrabMenu = ItemsToGrabMenu;
						if (itemsToGrabMenu != null && itemsToGrabMenu.inventory.Count <= i && Game1.player.showChestColorPicker)
						{
							upNeighborID = 4343;
							goto IL_01b0;
						}
					}
					upNeighborID = ((ItemsToGrabMenu.inventory.Count > i) ? (53910 + i) : 53910);
				}
				else
				{
					upNeighborID = 12598;
				}
				goto IL_01b0;
			}
			goto IL_01b5;
			IL_01b5:
			if (discreteColorPickerCC != null)
			{
				InventoryMenu itemsToGrabMenu2 = ItemsToGrabMenu;
				if (itemsToGrabMenu2 != null && itemsToGrabMenu2.inventory.Count > i && Game1.player.showChestColorPicker)
				{
					ItemsToGrabMenu.inventory[i].upNeighborID = 4343;
					continue;
				}
			}
			ItemsToGrabMenu.inventory[i].upNeighborID = -1;
			continue;
			IL_01b0:
			clickableComponent.upNeighborID = upNeighborID;
			goto IL_01b5;
		}
		if (shippingBin)
		{
			return;
		}
		for (int j = 0; j < 36; j++)
		{
			InventoryMenu inventoryMenu2 = inventory;
			if (inventoryMenu2 != null && inventoryMenu2.inventory?.Count > j)
			{
				inventory.inventory[j].upNeighborID = -7777;
				inventory.inventory[j].upNeighborImmutable = true;
			}
		}
	}

	/// <summary>Get whether the chest can display a color picker (regardless of whether it's currently shown).</summary>
	public virtual bool CanHaveColorPicker()
	{
		if (source == 1 && sourceItem is Chest chest && (chest.SpecialChestType == Chest.SpecialChestTypes.None || chest.SpecialChestType == Chest.SpecialChestTypes.BigChest))
		{
			return !chest.fridge.Value;
		}
		return false;
	}

	public virtual int GetColumnCount()
	{
		return ItemsToGrabMenu.capacity / ItemsToGrabMenu.rows;
	}

	/// <summary>Set whether to rescue items from the menu when it's force-closed (e.g. from passing out at 2am). Rescued items will be added to the player's inventory if possible, else dropped onto the ground at their feet.</summary>
	/// <param name="essential">Whether to rescue items on force-close.</param>
	/// <param name="superEssential">Whether to rescue items on normal close.</param>
	public ItemGrabMenu setEssential(bool essential, bool superEssential = false)
	{
		this.essential = essential || superEssential;
		this.superEssential = superEssential;
		return this;
	}

	public void initializeShippingBin()
	{
		shippingBin = true;
		lastShippedHolder = new ClickableTextureComponent("", new Rectangle(xPositionOnScreen + width / 2 - 48, yPositionOnScreen + height / 2 - 80 - 64, 96, 96), "", Game1.content.LoadString("Strings\\UI:ShippingBin_LastItem"), Game1.mouseCursors, new Rectangle(293, 360, 24, 24), 4f)
		{
			myID = 12598,
			region = 12598
		};
		for (int i = 0; i < GetColumnCount(); i++)
		{
			if (inventory?.inventory?.Count >= GetColumnCount())
			{
				inventory.inventory[i].upNeighborID = -7777;
				if (i == 11)
				{
					inventory.inventory[i].rightNeighborID = 5948;
				}
			}
		}
		populateClickableComponentList();
		if (Game1.options.SnappyMenus)
		{
			snapToDefaultClickableComponent();
		}
	}

	protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
	{
		switch (direction)
		{
		case 2:
		{
			for (int j = 0; j < 12; j++)
			{
				if (inventory?.inventory?.Count >= GetColumnCount() && shippingBin)
				{
					inventory.inventory[j].upNeighborID = (shippingBin ? 12598 : (Math.Min(j, ItemsToGrabMenu.inventory.Count - 1) + 53910));
				}
			}
			if (!shippingBin && oldID >= 53910)
			{
				int num4 = oldID - 53910;
				if (num4 + GetColumnCount() <= ItemsToGrabMenu.inventory.Count - 1)
				{
					currentlySnappedComponent = getComponentWithID(num4 + GetColumnCount() + 53910);
					snapCursorToCurrentSnappedComponent();
					break;
				}
			}
			if (inventory != null)
			{
				int num5 = inventory.capacity / inventory.rows;
				int num6 = GetColumnCount() - num5;
				currentlySnappedComponent = getComponentWithID((oldRegion != 12598) ? Math.Max(0, Math.Min((oldID - 53910) % GetColumnCount() - num6 / 2, inventory.capacity / inventory.rows - num6 / 2)) : 0);
			}
			else
			{
				currentlySnappedComponent = getComponentWithID((oldRegion != 12598) ? ((oldID - 53910) % GetColumnCount()) : 0);
			}
			snapCursorToCurrentSnappedComponent();
			break;
		}
		case 0:
		{
			if (shippingBin && Game1.getFarm().lastItemShipped != null && oldID < 12)
			{
				currentlySnappedComponent = getComponentWithID(12598);
				currentlySnappedComponent.downNeighborID = oldID;
				snapCursorToCurrentSnappedComponent();
				break;
			}
			if (oldID < 53910 && oldID >= 12)
			{
				currentlySnappedComponent = getComponentWithID(oldID - 12);
				break;
			}
			int num = oldID + GetColumnCount() * (ItemsToGrabMenu.rows - 1);
			for (int i = 0; i < 3; i++)
			{
				if (ItemsToGrabMenu.inventory.Count > num)
				{
					break;
				}
				num -= GetColumnCount();
			}
			if (showReceivingMenu)
			{
				if (num < 0)
				{
					if (ItemsToGrabMenu.inventory.Count > 0)
					{
						currentlySnappedComponent = getComponentWithID(53910 + ItemsToGrabMenu.inventory.Count - 1);
					}
					else if (discreteColorPickerCC != null)
					{
						currentlySnappedComponent = getComponentWithID(4343);
					}
				}
				else
				{
					int num2 = inventory.capacity / inventory.rows;
					int num3 = GetColumnCount() - num2;
					currentlySnappedComponent = getComponentWithID(num + 53910 + num3 / 2);
					if (currentlySnappedComponent == null)
					{
						currentlySnappedComponent = getComponentWithID(53910);
					}
				}
			}
			snapCursorToCurrentSnappedComponent();
			break;
		}
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		if (shippingBin)
		{
			currentlySnappedComponent = getComponentWithID(0);
		}
		else if (source == 1 && sourceItem is Chest { SpecialChestType: Chest.SpecialChestTypes.MiniShippingBin })
		{
			currentlySnappedComponent = getComponentWithID(0);
		}
		else
		{
			currentlySnappedComponent = getComponentWithID((ItemsToGrabMenu.inventory.Count > 0 && showReceivingMenu) ? 53910 : 0);
		}
		snapCursorToCurrentSnappedComponent();
	}

	public void setSourceItem(Item item)
	{
		sourceItem = item;
		chestColorPicker = null;
		colorPickerToggleButton = null;
		if (CanHaveColorPicker() && sourceItem is Chest chest)
		{
			Chest chest2 = new Chest(playerChest: true, sourceItem.ItemId);
			chestColorPicker = new DiscreteColorPicker(xPositionOnScreen, yPositionOnScreen - 64 - IClickableMenu.borderWidth * 2, chest.playerChoiceColor.Value, chest2);
			if (chest.SpecialChestType == Chest.SpecialChestTypes.BigChest)
			{
				chestColorPicker.yPositionOnScreen -= 42;
			}
			chest2.playerChoiceColor.Value = DiscreteColorPicker.getColorFromSelection(chestColorPicker.colorSelection);
			colorPickerToggleButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width, yPositionOnScreen + height / 3 - 64 + -160, 64, 64), Game1.mouseCursors, new Rectangle(119, 469, 16, 16), 4f)
			{
				hoverText = Game1.content.LoadString("Strings\\UI:Toggle_ColorPicker")
			};
		}
		RepositionSideButtons();
	}

	public override bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
	{
		if (direction == 1 && ItemsToGrabMenu.inventory.Contains(a) && inventory.inventory.Contains(b))
		{
			return false;
		}
		return base.IsAutomaticSnapValid(direction, a, b);
	}

	public void setBackgroundTransparency(bool b)
	{
		drawBG = b;
	}

	public void setDestroyItemOnClick(bool b)
	{
		destroyItemOnClick = b;
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (!allowRightClick)
		{
			receiveRightClickOnlyToolAttachments(x, y);
			return;
		}
		base.receiveRightClick(x, y, playSound && playRightClickSound);
		if (base.heldItem == null && showReceivingMenu)
		{
			base.heldItem = ItemsToGrabMenu.rightClick(x, y, base.heldItem, playSound: false);
			if (base.heldItem != null && behaviorOnItemGrab != null)
			{
				behaviorOnItemGrab(base.heldItem, Game1.player);
				if (Game1.activeClickableMenu is ItemGrabMenu itemGrabMenu)
				{
					itemGrabMenu.setSourceItem(sourceItem);
					if (Game1.options.SnappyMenus)
					{
						itemGrabMenu.currentlySnappedComponent = currentlySnappedComponent;
						itemGrabMenu.snapCursorToCurrentSnappedComponent();
					}
				}
			}
			if (base.heldItem?.QualifiedItemId == "(O)326")
			{
				base.heldItem = null;
				Game1.player.canUnderstandDwarves = true;
				poof = new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, new Vector2(x - x % 64 + 16, y - y % 64 + 16), flicker: false, flipped: false);
				Game1.playSound("fireball");
			}
			else if (base.heldItem is Object obj && obj?.QualifiedItemId == "(O)434")
			{
				base.heldItem = null;
				exitThisMenu(playSound: false);
				Game1.player.eatObject(obj, overrideFullness: true);
			}
			else if (base.heldItem != null && base.heldItem.IsRecipe)
			{
				base.heldItem.LearnRecipe();
				poof = new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, new Vector2(x - x % 64 + 16, y - y % 64 + 16), flicker: false, flipped: false);
				Game1.playSound("newRecipe");
				base.heldItem = null;
			}
			else if (Game1.player.addItemToInventoryBool(base.heldItem))
			{
				base.heldItem = null;
				Game1.playSound("coin");
			}
		}
		else if (reverseGrab || behaviorFunction != null)
		{
			behaviorFunction(base.heldItem, Game1.player);
			if (Game1.activeClickableMenu is ItemGrabMenu itemGrabMenu2)
			{
				itemGrabMenu2.setSourceItem(sourceItem);
			}
			if (destroyItemOnClick)
			{
				base.heldItem = null;
			}
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		if (snappedtoBottom)
		{
			movePosition((newBounds.Width - oldBounds.Width) / 2, Game1.uiViewport.Height - (yPositionOnScreen + height - IClickableMenu.spaceToClearTopBorder));
		}
		else
		{
			movePosition((newBounds.Width - oldBounds.Width) / 2, (newBounds.Height - oldBounds.Height) / 2);
		}
		ItemsToGrabMenu?.gameWindowSizeChanged(oldBounds, newBounds);
		RepositionSideButtons();
		if (CanHaveColorPicker() && sourceItem is Chest chest)
		{
			chestColorPicker = new DiscreteColorPicker(xPositionOnScreen, yPositionOnScreen - 64 - IClickableMenu.borderWidth * 2, chest.playerChoiceColor.Value, new Chest(playerChest: true, sourceItem.ItemId));
		}
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, !destroyItemOnClick);
		if (shippingBin && lastShippedHolder.containsPoint(x, y))
		{
			if (Game1.getFarm().lastItemShipped == null)
			{
				return;
			}
			Game1.getFarm().getShippingBin(Game1.player).Remove(Game1.getFarm().lastItemShipped);
			if (Game1.player.addItemToInventoryBool(Game1.getFarm().lastItemShipped))
			{
				Game1.playSound("coin");
				Game1.getFarm().lastItemShipped = null;
				if (Game1.player.ActiveObject != null)
				{
					Game1.player.showCarrying();
					Game1.player.Halt();
				}
			}
			else
			{
				Game1.getFarm().getShippingBin(Game1.player).Add(Game1.getFarm().lastItemShipped);
			}
			return;
		}
		if (chestColorPicker != null)
		{
			chestColorPicker.receiveLeftClick(x, y);
			if (sourceItem is Chest chest)
			{
				chest.playerChoiceColor.Value = DiscreteColorPicker.getColorFromSelection(chestColorPicker.colorSelection);
			}
		}
		if (colorPickerToggleButton != null && colorPickerToggleButton.containsPoint(x, y))
		{
			Game1.player.showChestColorPicker = !Game1.player.showChestColorPicker;
			chestColorPicker.visible = Game1.player.showChestColorPicker;
			try
			{
				Game1.playSound("drumkit6");
			}
			catch (Exception)
			{
			}
			SetupBorderNeighbors();
			return;
		}
		if (whichSpecialButton != -1 && specialButton != null && specialButton.containsPoint(x, y))
		{
			Game1.playSound("drumkit6");
			if (whichSpecialButton == 1 && context is JunimoHut junimoHut)
			{
				junimoHut.noHarvest.Value = !junimoHut.noHarvest.Value;
				specialButton.sourceRect.X = (junimoHut.noHarvest.Value ? 124 : 108);
			}
			return;
		}
		if (base.heldItem == null && showReceivingMenu)
		{
			base.heldItem = ItemsToGrabMenu.leftClick(x, y, base.heldItem, playSound: false);
			if (base.heldItem != null && behaviorOnItemGrab != null)
			{
				behaviorOnItemGrab(base.heldItem, Game1.player);
				if (Game1.activeClickableMenu is ItemGrabMenu itemGrabMenu)
				{
					itemGrabMenu.setSourceItem(sourceItem);
					if (Game1.options.SnappyMenus)
					{
						itemGrabMenu.currentlySnappedComponent = currentlySnappedComponent;
						itemGrabMenu.snapCursorToCurrentSnappedComponent();
					}
				}
			}
			string text = base.heldItem?.QualifiedItemId;
			if (!(text == "(O)326"))
			{
				if (text == "(O)102")
				{
					base.heldItem = null;
					Game1.player.foundArtifact("102", 1);
					poof = new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, new Vector2(x - x % 64 + 16, y - y % 64 + 16), flicker: false, flipped: false);
					Game1.playSound("fireball");
				}
			}
			else
			{
				base.heldItem = null;
				Game1.player.canUnderstandDwarves = true;
				poof = new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, new Vector2(x - x % 64 + 16, y - y % 64 + 16), flicker: false, flipped: false);
				Game1.playSound("fireball");
			}
			if (base.heldItem is Object obj && obj?.QualifiedItemId == "(O)434")
			{
				base.heldItem = null;
				exitThisMenu(playSound: false);
				Game1.player.eatObject(obj, overrideFullness: true);
			}
			else if (base.heldItem != null && base.heldItem.IsRecipe)
			{
				base.heldItem.LearnRecipe();
				poof = new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, new Vector2(x - x % 64 + 16, y - y % 64 + 16), flicker: false, flipped: false);
				Game1.playSound("newRecipe");
				base.heldItem = null;
			}
			else if (Game1.player.addItemToInventoryBool(base.heldItem))
			{
				base.heldItem = null;
				Game1.playSound("coin");
			}
		}
		else if ((reverseGrab || behaviorFunction != null) && isWithinBounds(x, y))
		{
			behaviorFunction(base.heldItem, Game1.player);
			if (Game1.activeClickableMenu is ItemGrabMenu itemGrabMenu2)
			{
				itemGrabMenu2.setSourceItem(sourceItem);
				if (Game1.options.SnappyMenus)
				{
					itemGrabMenu2.currentlySnappedComponent = currentlySnappedComponent;
					itemGrabMenu2.snapCursorToCurrentSnappedComponent();
				}
			}
			if (destroyItemOnClick)
			{
				base.heldItem = null;
				return;
			}
		}
		if (organizeButton != null && organizeButton.containsPoint(x, y))
		{
			organizeItemsInList(ItemsToGrabMenu.actualInventory);
			Game1.activeClickableMenu = new ItemGrabMenu(this);
			Game1.playSound("Ship");
		}
		else if (fillStacksButton != null && fillStacksButton.containsPoint(x, y))
		{
			FillOutStacks();
			Game1.playSound("Ship");
		}
		else if (junimoNoteIcon != null && junimoNoteIcon.containsPoint(x, y))
		{
			if (readyToClose())
			{
				Game1.activeClickableMenu = new JunimoNoteMenu(fromGameMenu: true)
				{
					menuToReturnTo = this
				};
			}
		}
		else if (base.heldItem != null && !isWithinBounds(x, y) && base.heldItem.canBeTrashed())
		{
			DropHeldItem();
		}
	}

	/// <summary>Merge any items from the player inventory into an equivalent stack in the chest where possible.</summary>
	public void FillOutStacks()
	{
		IList<Item> actualInventory = inventory.actualInventory;
		IList<Item> actualInventory2 = ItemsToGrabMenu.actualInventory;
		HashSet<int> hashSet = new HashSet<int>();
		ILookup<string, Item> lookup = actualInventory2.Where((Item item5) => item5 != null).ToLookup((Item item5) => item5.QualifiedItemId);
		if (lookup.Count == 0)
		{
			return;
		}
		for (int num = 0; num < actualInventory.Count; num++)
		{
			Item item = actualInventory[num];
			if (item == null)
			{
				continue;
			}
			bool flag = false;
			foreach (Item item5 in lookup[item.QualifiedItemId])
			{
				flag = item5.canStackWith(item);
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			Item item2 = item;
			bool flag2 = false;
			int num2 = -1;
			for (int num3 = 0; num3 < actualInventory2.Count; num3++)
			{
				Item item3 = actualInventory2[num3];
				if (item3 == null)
				{
					if (num2 == -1)
					{
						num2 = num3;
					}
				}
				else
				{
					if (!item3.canStackWith(item))
					{
						continue;
					}
					int num4 = item.Stack - item3.addToStack(item);
					if (num4 > 0)
					{
						flag2 = true;
						hashSet.Add(num3);
						item = item.ConsumeStack(num4);
						if (item == null)
						{
							actualInventory[num] = null;
							break;
						}
					}
				}
			}
			if (item != null)
			{
				if (num2 == -1 && actualInventory2.Count < ItemsToGrabMenu.capacity)
				{
					num2 = actualInventory2.Count;
					actualInventory2.Add(null);
				}
				if (num2 > -1)
				{
					flag2 = true;
					hashSet.Add(num2);
					item.onDetachedFromParent();
					actualInventory2[num2] = item;
					actualInventory[num] = null;
				}
			}
			if (flag2)
			{
				TransferredItemSprite item4 = new TransferredItemSprite(item2.getOne(), inventory.inventory[num].bounds.X, inventory.inventory[num].bounds.Y);
				_transferredItemSprites.Add(item4);
			}
		}
		foreach (int item6 in hashSet)
		{
			ItemsToGrabMenu.ShakeItem(item6);
		}
	}

	/// <summary>Consolidate and sort item stacks in an item list.</summary>
	/// <param name="items">The item list to change.</param>
	public static void organizeItemsInList(IList<Item> items)
	{
		List<Item> list = new List<Item>(items);
		List<Item> tools = new List<Item>();
		list.RemoveAll(delegate(Item item3)
		{
			if (item3 == null)
			{
				return true;
			}
			if (item3 is Tool)
			{
				tools.Add(item3);
				return true;
			}
			return false;
		});
		for (int num = 0; num < list.Count; num++)
		{
			Item item = list[num];
			if (item.getRemainingStackSpace() <= 0)
			{
				continue;
			}
			for (int num2 = num + 1; num2 < list.Count; num2++)
			{
				Item item2 = list[num2];
				if (item.canStackWith(item2))
				{
					item2.Stack = item.addToStack(item2);
					if (item2.Stack == 0)
					{
						list.RemoveAt(num2);
						num2--;
					}
				}
			}
		}
		list.Sort();
		list.InsertRange(0, tools);
		for (int num3 = 0; num3 < items.Count; num3++)
		{
			items[num3] = null;
		}
		for (int num4 = 0; num4 < list.Count; num4++)
		{
			items[num4] = list[num4];
		}
	}

	public bool areAllItemsTaken()
	{
		for (int i = 0; i < ItemsToGrabMenu.actualInventory.Count; i++)
		{
			if (ItemsToGrabMenu.actualInventory[i] != null)
			{
				return false;
			}
		}
		return true;
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		base.receiveGamePadButton(button);
		switch (button)
		{
		case Buttons.Back:
			if (organizeButton != null)
			{
				organizeItemsInList(Game1.player.Items);
				Game1.playSound("Ship");
			}
			break;
		case Buttons.RightShoulder:
		{
			ClickableComponent componentWithID2 = getComponentWithID(12952);
			if (componentWithID2 != null)
			{
				setCurrentlySnappedComponentTo(componentWithID2.myID);
				snapCursorToCurrentSnappedComponent();
				break;
			}
			int num = -1;
			ClickableComponent clickableComponent = null;
			foreach (ClickableComponent allClickableComponent in allClickableComponents)
			{
				if (allClickableComponent.region == 15923 && (num == -1 || allClickableComponent.bounds.Y < num))
				{
					num = allClickableComponent.bounds.Y;
					clickableComponent = allClickableComponent;
				}
			}
			if (clickableComponent != null)
			{
				setCurrentlySnappedComponentTo(clickableComponent.myID);
				snapCursorToCurrentSnappedComponent();
			}
			break;
		}
		case Buttons.LeftShoulder:
		{
			if (shippingBin)
			{
				break;
			}
			ClickableComponent componentWithID = getComponentWithID(53910);
			if (componentWithID != null)
			{
				setCurrentlySnappedComponentTo(componentWithID.myID);
				snapCursorToCurrentSnappedComponent();
				break;
			}
			componentWithID = getComponentWithID(0);
			if (componentWithID != null)
			{
				setCurrentlySnappedComponentTo(0);
				snapCursorToCurrentSnappedComponent();
			}
			break;
		}
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (Game1.options.snappyMenus && Game1.options.gamepadControls)
		{
			applyMovementKey(key);
		}
		if ((canExitOnKey || areAllItemsTaken()) && Game1.options.doesInputListContain(Game1.options.menuButton, key) && readyToClose())
		{
			exitThisMenu();
			Event currentEvent = Game1.currentLocation.currentEvent;
			if (currentEvent != null && currentEvent.CurrentCommand > 0)
			{
				Game1.currentLocation.currentEvent.CurrentCommand++;
			}
		}
		else if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && base.heldItem != null)
		{
			Game1.setMousePosition(trashCan.bounds.Center);
		}
		if (key == Keys.Delete && base.heldItem != null && base.heldItem.canBeTrashed())
		{
			Utility.trashItem(base.heldItem);
			base.heldItem = null;
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		if (!HasUpdateTicked)
		{
			HasUpdateTicked = true;
			if (source == 4)
			{
				IList<Item> actualInventory = ItemsToGrabMenu.actualInventory;
				for (int i = 0; i < actualInventory.Count; i++)
				{
					if (actualInventory[i]?.QualifiedItemId == "(O)434")
					{
						List<Item> list = new List<Item>(actualInventory);
						list.RemoveAt(i);
						list.RemoveAll((Item p) => p == null);
						if (list.Count > 0)
						{
							Game1.nextClickableMenu.Insert(0, CreateOverflowMenu(list, inventory.onAddItem));
						}
						essential = false;
						superEssential = false;
						exitThisMenu(playSound: false);
						Game1.player.eatObject(actualInventory[i] as Object, overrideFullness: true);
						return;
					}
				}
			}
		}
		if (poof != null && poof.update(time))
		{
			poof = null;
		}
		chestColorPicker?.update(time);
		if (sourceItem is Chest chest && _sourceItemInCurrentLocation)
		{
			Vector2 value = chest.tileLocation.Value;
			if (value != Vector2.Zero && !Game1.currentLocation.objects.ContainsKey(value))
			{
				if (Game1.activeClickableMenu != null)
				{
					Game1.activeClickableMenu.emergencyShutDown();
				}
				Game1.exitActiveMenu();
			}
		}
		_transferredItemSprites.RemoveAll((TransferredItemSprite sprite) => sprite.Update(time));
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoveredItem = null;
		hoverText = "";
		base.performHoverAction(x, y);
		if (colorPickerToggleButton != null)
		{
			colorPickerToggleButton.tryHover(x, y, 0.25f);
			if (colorPickerToggleButton.containsPoint(x, y))
			{
				hoverText = colorPickerToggleButton.hoverText;
			}
		}
		if (organizeButton != null)
		{
			organizeButton.tryHover(x, y, 0.25f);
			if (organizeButton.containsPoint(x, y))
			{
				hoverText = organizeButton.hoverText;
			}
		}
		if (fillStacksButton != null)
		{
			fillStacksButton.tryHover(x, y, 0.25f);
			if (fillStacksButton.containsPoint(x, y))
			{
				hoverText = fillStacksButton.hoverText;
			}
		}
		specialButton?.tryHover(x, y, 0.25f);
		if (showReceivingMenu)
		{
			Item item = ItemsToGrabMenu.hover(x, y, base.heldItem);
			if (item != null)
			{
				hoveredItem = item;
			}
		}
		if (junimoNoteIcon != null)
		{
			junimoNoteIcon.tryHover(x, y);
			if (junimoNoteIcon.containsPoint(x, y))
			{
				hoverText = junimoNoteIcon.hoverText;
			}
			if (GameMenu.bundleItemHovered)
			{
				junimoNoteIcon.scale = junimoNoteIcon.baseScale + (float)Math.Sin((float)junimoNotePulser / 100f) / 4f;
				junimoNotePulser += (int)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
			}
			else
			{
				junimoNotePulser = 0;
				junimoNoteIcon.scale = junimoNoteIcon.baseScale;
			}
		}
		if (hoverText != null)
		{
			return;
		}
		if (organizeButton != null)
		{
			hoverText = null;
			organizeButton.tryHover(x, y);
			if (organizeButton.containsPoint(x, y))
			{
				hoverText = organizeButton.hoverText;
			}
		}
		if (shippingBin)
		{
			hoverText = null;
			if (lastShippedHolder.containsPoint(x, y) && Game1.getFarm().lastItemShipped != null)
			{
				hoverText = lastShippedHolder.hoverText;
			}
		}
		chestColorPicker?.performHoverAction(x, y);
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (drawBG && !Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
		}
		base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);
		if (showReceivingMenu)
		{
			b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen - 64, yPositionOnScreen + height / 2 + 64 + 16), new Rectangle(16, 368, 12, 16), Color.White, 4.712389f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen - 64, yPositionOnScreen + height / 2 + 64 - 16), new Rectangle(21, 368, 11, 16), Color.White, 4.712389f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen - 40, yPositionOnScreen + height / 2 + 64 - 44), new Rectangle(4, 372, 8, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			Game1.drawDialogueBox(ItemsToGrabMenu.xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder, ItemsToGrabMenu.yPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder + storageSpaceTopBorderOffset, ItemsToGrabMenu.width + IClickableMenu.borderWidth * 2 + IClickableMenu.spaceToClearSideBorder * 2, ItemsToGrabMenu.height + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth * 2 - storageSpaceTopBorderOffset, speaker: false, drawOnlyBox: true);
			if ((source != 1 || !(sourceItem is Chest chest) || (chest.SpecialChestType != Chest.SpecialChestTypes.MiniShippingBin && chest.SpecialChestType != Chest.SpecialChestTypes.JunimoChest && chest.SpecialChestType != Chest.SpecialChestTypes.Enricher)) && source != 0)
			{
				b.Draw(Game1.mouseCursors, new Vector2(ItemsToGrabMenu.xPositionOnScreen - 100, yPositionOnScreen + 64 + 16), new Rectangle(16, 368, 12, 16), Color.White, 4.712389f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				b.Draw(Game1.mouseCursors, new Vector2(ItemsToGrabMenu.xPositionOnScreen - 100, yPositionOnScreen + 64 - 16), new Rectangle(21, 368, 11, 16), Color.White, 4.712389f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				Rectangle value = new Rectangle(127, 412, 10, 11);
				switch (source)
				{
				case 3:
					value.X += 10;
					break;
				case 4:
					value.X += 20;
					break;
				}
				b.Draw(Game1.mouseCursors, new Vector2(ItemsToGrabMenu.xPositionOnScreen - 80, yPositionOnScreen + 64 - 44), value, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			}
			ItemsToGrabMenu.draw(b);
		}
		else if (message != null)
		{
			Game1.drawDialogueBox(Game1.uiViewport.Width / 2, ItemsToGrabMenu.yPositionOnScreen + ItemsToGrabMenu.height / 2, speaker: false, drawOnlyBox: false, message);
		}
		poof?.draw(b, localPosition: true);
		foreach (TransferredItemSprite transferredItemSprite in _transferredItemSprites)
		{
			transferredItemSprite.Draw(b);
		}
		if (shippingBin && Game1.getFarm().lastItemShipped != null)
		{
			lastShippedHolder.draw(b);
			Game1.getFarm().lastItemShipped.drawInMenu(b, new Vector2(lastShippedHolder.bounds.X + 16, lastShippedHolder.bounds.Y + 16), 1f);
			b.Draw(Game1.mouseCursors, new Vector2(lastShippedHolder.bounds.X + -8, lastShippedHolder.bounds.Bottom - 100), new Rectangle(325, 448, 5, 14), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			b.Draw(Game1.mouseCursors, new Vector2(lastShippedHolder.bounds.X + 84, lastShippedHolder.bounds.Bottom - 100), new Rectangle(325, 448, 5, 14), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			b.Draw(Game1.mouseCursors, new Vector2(lastShippedHolder.bounds.X + -8, lastShippedHolder.bounds.Bottom - 44), new Rectangle(325, 452, 5, 13), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			b.Draw(Game1.mouseCursors, new Vector2(lastShippedHolder.bounds.X + 84, lastShippedHolder.bounds.Bottom - 44), new Rectangle(325, 452, 5, 13), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		}
		if (colorPickerToggleButton != null)
		{
			colorPickerToggleButton.draw(b);
		}
		else
		{
			specialButton?.draw(b);
		}
		chestColorPicker?.draw(b);
		organizeButton?.draw(b);
		fillStacksButton?.draw(b);
		junimoNoteIcon?.draw(b);
		if (hoverText != null && (hoveredItem == null || ItemsToGrabMenu == null))
		{
			if (hoverAmount > 0)
			{
				IClickableMenu.drawToolTip(b, hoverText, "", null, heldItem: true, -1, 0, null, -1, null, hoverAmount);
			}
			else
			{
				IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);
			}
		}
		if (hoveredItem != null)
		{
			IClickableMenu.drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem, base.heldItem != null);
		}
		else if (hoveredItem != null && ItemsToGrabMenu != null)
		{
			IClickableMenu.drawToolTip(b, ItemsToGrabMenu.descriptionText, ItemsToGrabMenu.descriptionTitle, hoveredItem, base.heldItem != null);
		}
		base.heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
		Game1.mouseCursorTransparency = 1f;
		drawMouse(b);
	}

	/// <inheritdoc />
	protected override void cleanupBeforeExit()
	{
		base.cleanupBeforeExit();
		if (superEssential)
		{
			DropRemainingItems();
		}
	}

	public override void emergencyShutDown()
	{
		base.emergencyShutDown();
		if (!essential)
		{
			return;
		}
		foreach (Item item2 in ItemsToGrabMenu.actualInventory)
		{
			if (item2 != null)
			{
				Item item = Game1.player.addItemToInventory(item2);
				if (item != null)
				{
					Game1.createItemDebris(item, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
				}
			}
		}
	}
}
