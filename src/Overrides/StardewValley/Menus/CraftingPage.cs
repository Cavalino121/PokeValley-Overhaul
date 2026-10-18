using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Buffs;
using StardewValley.Extensions;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Quests;

namespace StardewValley.Menus;

public class CraftingPage : IClickableMenu
{
	public const int howManyRecipesFitOnPage = 40;

	public const int numInRow = 10;

	public const int numInCol = 4;

	public const int region_upArrow = 88;

	public const int region_downArrow = 89;

	public const int region_craftingSelectionArea = 8000;

	public const int region_craftingModifier = 200;

	public string hoverText = "";

	public Item hoverItem;

	public Item lastCookingHover;

	public InventoryMenu inventory;

	public Item heldItem;

	[SkipForClickableAggregation]
	public List<Dictionary<ClickableTextureComponent, CraftingRecipe>> pagesOfCraftingRecipes = new List<Dictionary<ClickableTextureComponent, CraftingRecipe>>();

	public int currentCraftingPage;

	public CraftingRecipe hoverRecipe;

	public ClickableTextureComponent upButton;

	public ClickableTextureComponent downButton;

	public bool cooking;

	public ClickableTextureComponent trashCan;

	public ClickableComponent dropItemInvisibleButton;

	public float trashCanLidRotation;

	public List<IInventory> _materialContainers;

	protected bool _standaloneMenu;

	public int hoverAmount;

	public List<ClickableComponent> currentPageClickableComponents = new List<ClickableComponent>();

	private string hoverTitle = "";

	public CraftingPage(int x, int y, int width, int height, bool cooking = false, bool standaloneMenu = false, List<IInventory> materialContainers = null)
		: base(x, y, width, height)
	{
		_standaloneMenu = standaloneMenu;
		this.cooking = cooking;
		inventory = new InventoryMenu(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + 320 - 16, playerInventory: false);
		inventory.showGrayedOutSlots = true;
		currentPageClickableComponents = new List<ClickableComponent>();
		foreach (ClickableComponent item in inventory.GetBorder(InventoryMenu.BorderSide.Top))
		{
			item.upNeighborID = -99998;
		}
		_materialContainers = materialContainers;
		if (_standaloneMenu)
		{
			initializeUpperRightCloseButton();
		}
		trashCan = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 4, yPositionOnScreen + height - 192 - 32 - IClickableMenu.borderWidth - 104, 64, 104), Game1.mouseCursors, new Rectangle(564 + Game1.player.trashCanLevel * 18, 102, 18, 26), 4f)
		{
			myID = 106
		};
		dropItemInvisibleButton = new ClickableComponent(new Rectangle(xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 64, trashCan.bounds.Y, 64, 64), "")
		{
			myID = 107,
			rightNeighborID = 0
		};
		if (_standaloneMenu)
		{
			Game1.playSound("bigSelect");
		}
		RepositionElements();
		if (Game1.options.SnappyMenus)
		{
			snapToDefaultClickableComponent();
		}
	}

	/// <summary>Get the recipes to display in the menu.</summary>
	protected virtual List<string> GetRecipesToDisplay()
	{
		List<string> list = new List<string>();
		if (!cooking)
		{
			foreach (string key in CraftingRecipe.craftingRecipes.Keys)
			{
				if (Game1.player.craftingRecipes.ContainsKey(key))
				{
					list.Add(key);
				}
			}
		}
		else
		{
			foreach (string key2 in CraftingRecipe.cookingRecipes.Keys)
			{
				if (!key2.Equals("Moss Soup"))
				{
					list.Add(key2);
				}
			}
			list.Sort(delegate(string a, string b)
			{
				int num = -1;
				int value = -1;
				if (a != null && CraftingRecipe.cookingRecipes.TryGetValue(a, out var value2))
				{
					num = ArgUtility.GetInt(value2.Split('/'), 2, -1);
				}
				if (b != null && CraftingRecipe.cookingRecipes.TryGetValue(b, out var value3))
				{
					value = ArgUtility.GetInt(value3.Split('/'), 2, -1);
				}
				return num.CompareTo(value);
			});
			list.Add("Moss Soup");
		}
		return list;
	}

	protected virtual IList<Item> getContainerContents()
	{
		if (_materialContainers == null)
		{
			return null;
		}
		List<Item> list = new List<Item>();
		foreach (IInventory materialContainer in _materialContainers)
		{
			list.AddRange(materialContainer);
		}
		return list;
	}

	private int craftingPageY()
	{
		return yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - 16;
	}

	private ClickableTextureComponent[,] createNewPageLayout()
	{
		return new ClickableTextureComponent[10, 4];
	}

	private Dictionary<ClickableTextureComponent, CraftingRecipe> createNewPage()
	{
		Dictionary<ClickableTextureComponent, CraftingRecipe> dictionary = new Dictionary<ClickableTextureComponent, CraftingRecipe>();
		pagesOfCraftingRecipes.Add(dictionary);
		return dictionary;
	}

	private bool spaceOccupied(ClickableTextureComponent[,] pageLayout, int x, int y, CraftingRecipe recipe)
	{
		if (pageLayout[x, y] != null)
		{
			return true;
		}
		if (!recipe.bigCraftable)
		{
			return false;
		}
		if (y + 1 < 4)
		{
			return pageLayout[x, y + 1] != null;
		}
		return true;
	}

	private void layoutRecipes(List<string> playerRecipes)
	{
		int num = xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth - 16;
		int num2 = 8;
		Dictionary<ClickableTextureComponent, CraftingRecipe> dictionary = createNewPage();
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		ClickableTextureComponent[,] array = createNewPageLayout();
		List<ClickableTextureComponent[,]> list = new List<ClickableTextureComponent[,]>();
		list.Add(array);
		foreach (string playerRecipe in playerRecipes)
		{
			num5++;
			CraftingRecipe craftingRecipe = new CraftingRecipe(playerRecipe, cooking);
			while (spaceOccupied(array, num3, num4, craftingRecipe))
			{
				num3++;
				if (num3 >= 10)
				{
					num3 = 0;
					num4++;
					if (num4 >= 4)
					{
						dictionary = createNewPage();
						array = createNewPageLayout();
						list.Add(array);
						num3 = 0;
						num4 = 0;
					}
				}
			}
			int myID = 200 + num5;
			ParsedItemData itemData = craftingRecipe.GetItemData(useFirst: true);
			Texture2D texture = itemData.GetTexture();
			Rectangle sourceRect = itemData.GetSourceRect();
			float iconScale = 4f;
			if (StardewValley.Pokemon.PokemonItemManager.TryGetCustomTexture(itemData.QualifiedItemId, out Texture2D customTex, out Rectangle customRect))
			{
				texture = customTex;
				sourceRect = customRect;
				iconScale = 64f / Math.Max(customRect.Width, customRect.Height);
			}
			ClickableTextureComponent clickableTextureComponent = new ClickableTextureComponent("", new Rectangle(num + num3 * (64 + num2), craftingPageY() + num4 * 72, 64, craftingRecipe.bigCraftable ? 128 : 64), null, (cooking && !Game1.player.cookingRecipes.ContainsKey(craftingRecipe.name)) ? "ghosted" : "", texture, sourceRect, iconScale)
			{
				myID = myID,
				rightNeighborID = -99998,
				leftNeighborID = -99998,
				upNeighborID = -99998,
				downNeighborID = -99998,
				fullyImmutable = true,
				region = 8000
			};
			dictionary.Add(clickableTextureComponent, craftingRecipe);
			array[num3, num4] = clickableTextureComponent;
			if (craftingRecipe.bigCraftable)
			{
				array[num3, num4 + 1] = clickableTextureComponent;
			}
		}
	}

	protected override void noSnappedComponentFound(int direction, int oldRegion, int oldID)
	{
		base.noSnappedComponentFound(direction, oldRegion, oldID);
		if (oldRegion == 8000 && direction == 2)
		{
			currentlySnappedComponent = getComponentWithID(oldID % 10);
			currentlySnappedComponent.upNeighborID = oldID;
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = ((currentCraftingPage < pagesOfCraftingRecipes.Count) ? pagesOfCraftingRecipes[currentCraftingPage].First().Key : null);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	protected override void actionOnRegionChange(int oldRegion, int newRegion)
	{
		base.actionOnRegionChange(oldRegion, newRegion);
		if (newRegion != 9000 || oldRegion == 0)
		{
			return;
		}
		for (int i = 0; i < 10; i++)
		{
			if (inventory.inventory.Count > i)
			{
				inventory.inventory[i].upNeighborID = currentlySnappedComponent.upNeighborID;
			}
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		base.receiveKeyPress(key);
		if (key == Keys.Delete && heldItem != null && heldItem.canBeTrashed())
		{
			Utility.trashItem(heldItem);
			heldItem = null;
		}
		if (Game1.isAnyGamePadButtonBeingPressed() && Game1.options.doesInputListContain(Game1.options.menuButton, key) && heldItem != null)
		{
			Game1.setMousePosition(trashCan.bounds.Center);
		}
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		base.receiveScrollWheelAction(direction);
		if (direction > 0 && currentCraftingPage > 0)
		{
			currentCraftingPage--;
			_UpdateCurrentPageButtons();
			Game1.playSound("shwip");
			if (Game1.options.SnappyMenus)
			{
				setCurrentlySnappedComponentTo(88);
				snapCursorToCurrentSnappedComponent();
			}
		}
		else if (direction < 0 && currentCraftingPage < pagesOfCraftingRecipes.Count - 1)
		{
			currentCraftingPage++;
			_UpdateCurrentPageButtons();
			Game1.playSound("shwip");
			if (Game1.options.SnappyMenus)
			{
				setCurrentlySnappedComponentTo(89);
				snapCursorToCurrentSnappedComponent();
			}
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		RepositionElements();
	}

	public virtual void RepositionElements()
	{
		List<string> recipesToDisplay = GetRecipesToDisplay();
		pagesOfCraftingRecipes.Clear();
		layoutRecipes(recipesToDisplay);
		if (pagesOfCraftingRecipes.Count > 1)
		{
			upButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 768 + 32, craftingPageY(), 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 12), 0.8f)
			{
				myID = 88,
				downNeighborID = 89,
				rightNeighborID = 106,
				leftNeighborID = -99998
			};
			downButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 768 + 32, craftingPageY() + 192 + 32, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 11), 0.8f)
			{
				myID = 89,
				upNeighborID = 88,
				rightNeighborID = 106,
				leftNeighborID = -99998
			};
		}
		inventory.SetPosition(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + 320 - 16);
		trashCan.bounds.X = xPositionOnScreen + width + 4;
		trashCan.bounds.Y = yPositionOnScreen + height - 192 - 32 - IClickableMenu.borderWidth - 104;
		dropItemInvisibleButton.bounds.X = xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 64;
		dropItemInvisibleButton.bounds.Y = trashCan.bounds.Y;
		if (upButton != null)
		{
			upButton.bounds.X = xPositionOnScreen + 768 + 32;
			upButton.bounds.Y = craftingPageY();
		}
		if (downButton != null)
		{
			downButton.bounds.X = xPositionOnScreen + 768 + 32;
			downButton.bounds.Y = craftingPageY() + 192 + 32;
		}
		_UpdateCurrentPageButtons();
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y);
		heldItem = inventory.leftClick(x, y, heldItem);
		if (upButton != null && upButton.containsPoint(x, y) && currentCraftingPage > 0)
		{
			Game1.playSound("coin");
			currentCraftingPage = Math.Max(0, currentCraftingPage - 1);
			_UpdateCurrentPageButtons();
			upButton.scale = upButton.baseScale;
		}
		if (downButton != null && downButton.containsPoint(x, y) && currentCraftingPage < pagesOfCraftingRecipes.Count - 1)
		{
			Game1.playSound("coin");
			currentCraftingPage = Math.Min(pagesOfCraftingRecipes.Count - 1, currentCraftingPage + 1);
			_UpdateCurrentPageButtons();
			downButton.scale = downButton.baseScale;
		}
		foreach (ClickableTextureComponent key in pagesOfCraftingRecipes[currentCraftingPage].Keys)
		{
			int num = ((!Game1.oldKBState.IsKeyDown(Keys.LeftShift)) ? 1 : (Game1.oldKBState.IsKeyDown(Keys.LeftControl) ? 25 : 5));
			for (int i = 0; i < num; i++)
			{
				if (key.containsPoint(x, y, 4) && !key.hoverText.Equals("ghosted") && pagesOfCraftingRecipes[currentCraftingPage][key].doesFarmerHaveIngredientsInInventory(getContainerContents()))
				{
					clickCraftingRecipe(key, i == 0);
				}
			}
			if (heldItem != null && Game1.oldKBState.IsKeyDown(Keys.LeftShift) && heldItem.maximumStackSize() == 1 && Game1.player.couldInventoryAcceptThisItem(heldItem))
			{
				Game1.player.addItemToInventoryBool(heldItem);
				heldItem = null;
			}
		}
		if (trashCan != null && trashCan.containsPoint(x, y) && heldItem != null && heldItem.canBeTrashed())
		{
			Utility.trashItem(heldItem);
			heldItem = null;
		}
		else if (heldItem != null && !isWithinBounds(x, y) && heldItem.canBeTrashed())
		{
			Game1.playSound("throwDownITem");
			Game1.createItemDebris(heldItem, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
			heldItem = null;
		}
	}

	protected void _UpdateCurrentPageButtons()
	{
		currentPageClickableComponents.Clear();
		foreach (ClickableTextureComponent key in pagesOfCraftingRecipes[currentCraftingPage].Keys)
		{
			currentPageClickableComponents.Add(key);
		}
		populateClickableComponentList();
	}

	private void clickCraftingRecipe(ClickableTextureComponent c, bool playSound = true)
	{
		CraftingRecipe recipe = pagesOfCraftingRecipes[currentCraftingPage][c];
		Item crafted = recipe.createItem();
		List<KeyValuePair<string, int>> list = null;
		if (cooking && crafted.Quality == 0)
		{
			list = new List<KeyValuePair<string, int>>();
			list.Add(new KeyValuePair<string, int>("917", 1));
			if (CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory(list, getContainerContents()))
			{
				crafted.Quality = 2;
			}
			else
			{
				list = null;
			}
		}
		if (heldItem == null)
		{
			recipe.consumeIngredients(_materialContainers);
			heldItem = crafted;
			if (playSound)
			{
				Game1.playSound("coin");
			}
		}
		else
		{
			if (!(heldItem.Name == crafted.Name) || !heldItem.getOne().canStackWith(crafted.getOne()) || heldItem.Stack + recipe.numberProducedPerCraft - 1 >= heldItem.maximumStackSize())
			{
				return;
			}
			heldItem.Stack += recipe.numberProducedPerCraft;
			recipe.consumeIngredients(_materialContainers);
			if (playSound)
			{
				Game1.playSound("coin");
			}
		}
		if (list != null)
		{
			if (playSound)
			{
				Game1.playSound("breathin");
			}
			CraftingRecipe.ConsumeAdditionalIngredients(list, _materialContainers);
			if (!CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory(list, getContainerContents()))
			{
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Seasoning_UsedLast"));
			}
		}
		Game1.player.NotifyQuests((Quest quest) => quest.OnRecipeCrafted(recipe, crafted));
		if (!cooking && Game1.player.craftingRecipes.ContainsKey(recipe.name))
		{
			Game1.player.craftingRecipes[recipe.name] += recipe.numberProducedPerCraft;
		}
		if (cooking)
		{
			Game1.player.cookedRecipe(heldItem.ItemId);
			Game1.stats.checkForCookingAchievements();
		}
		else
		{
			Game1.stats.checkForCraftingAchievements();
		}
		if (Game1.options.gamepadControls && heldItem != null && Game1.player.couldInventoryAcceptThisItem(heldItem))
		{
			Game1.player.addItemToInventoryBool(heldItem);
			heldItem = null;
		}
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		heldItem = inventory.rightClick(x, y, heldItem);
		foreach (ClickableTextureComponent key in pagesOfCraftingRecipes[currentCraftingPage].Keys)
		{
			if (key.containsPoint(x, y) && !key.hoverText.Equals("ghosted") && pagesOfCraftingRecipes[currentCraftingPage][key].doesFarmerHaveIngredientsInInventory(getContainerContents()))
			{
				clickCraftingRecipe(key);
			}
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		CraftingRecipe craftingRecipe = hoverRecipe;
		base.performHoverAction(x, y);
		hoverTitle = "";
		hoverText = "";
		hoverRecipe = null;
		hoverItem = inventory.hover(x, y, hoverItem);
		hoverAmount = -1;
		if (hoverItem != null)
		{
			hoverTitle = inventory.hoverTitle;
			hoverText = inventory.hoverText;
		}
		foreach (ClickableTextureComponent key in pagesOfCraftingRecipes[currentCraftingPage].Keys)
		{
			if (key.containsPoint(x, y, 4))
			{
				if (key.hoverText.Equals("ghosted"))
				{
					hoverText = "???";
					continue;
				}
				hoverRecipe = pagesOfCraftingRecipes[currentCraftingPage][key];
				if (craftingRecipe == null || craftingRecipe.name != hoverRecipe.name)
				{
					lastCookingHover = hoverRecipe.createItem();
				}
				key.scale = Math.Min(key.scale + 0.02f, key.baseScale + 0.1f);
			}
			else
			{
				key.scale = Math.Max(key.scale - 0.02f, key.baseScale);
			}
		}
		if (upButton != null)
		{
			if (upButton.containsPoint(x, y))
			{
				upButton.scale = Math.Min(upButton.scale + 0.02f, upButton.baseScale + 0.1f);
			}
			else
			{
				upButton.scale = Math.Max(upButton.scale - 0.02f, upButton.baseScale);
			}
		}
		if (downButton != null)
		{
			if (downButton.containsPoint(x, y))
			{
				downButton.scale = Math.Min(downButton.scale + 0.02f, downButton.baseScale + 0.1f);
			}
			else
			{
				downButton.scale = Math.Max(downButton.scale - 0.02f, downButton.baseScale);
			}
		}
		if (trashCan == null)
		{
			return;
		}
		if (trashCan.containsPoint(x, y))
		{
			if (trashCanLidRotation <= 0f)
			{
				Game1.playSound("trashcanlid");
			}
			trashCanLidRotation = Math.Min(trashCanLidRotation + (float)Math.PI / 48f, (float)Math.PI / 2f);
			if (heldItem != null && Utility.getTrashReclamationPrice(heldItem, Game1.player) > 0)
			{
				hoverText = Game1.content.LoadString("Strings\\UI:TrashCanSale");
				hoverAmount = Utility.getTrashReclamationPrice(heldItem, Game1.player);
			}
		}
		else
		{
			trashCanLidRotation = Math.Max(trashCanLidRotation - (float)Math.PI / 48f, 0f);
		}
	}

	public override bool readyToClose()
	{
		return heldItem == null;
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (_standaloneMenu)
		{
			Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, speaker: false, drawOnlyBox: true);
		}
		drawHorizontalPartition(b, yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 256);
		inventory.draw(b);
		if (trashCan != null)
		{
			trashCan.draw(b);
			b.Draw(Game1.mouseCursors, new Vector2(trashCan.bounds.X + 60, trashCan.bounds.Y + 40), new Rectangle(564 + Game1.player.trashCanLevel * 18, 129, 18, 10), Color.White, trashCanLidRotation, new Vector2(16f, 10f), 4f, SpriteEffects.None, 0.86f);
		}
		b.End();
		b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
		foreach (ClickableTextureComponent key in pagesOfCraftingRecipes[currentCraftingPage].Keys)
		{
			if (key.hoverText.Equals("ghosted"))
			{
				key.draw(b, Color.Black * 0.35f, 0.89f);
			}
			else if (!pagesOfCraftingRecipes[currentCraftingPage][key].doesFarmerHaveIngredientsInInventory(getContainerContents()))
			{
				key.draw(b, Color.DimGray * 0.4f, 0.89f);
				if (pagesOfCraftingRecipes[currentCraftingPage][key].numberProducedPerCraft > 1)
				{
					NumberSprite.draw(pagesOfCraftingRecipes[currentCraftingPage][key].numberProducedPerCraft, b, new Vector2(key.bounds.X + 64 - 2, key.bounds.Y + 64 - 2), Color.LightGray * 0.75f, 0.5f * (key.scale / 4f), 0.97f, 1f, 0);
				}
			}
			else
			{
				key.draw(b);
				if (pagesOfCraftingRecipes[currentCraftingPage][key].numberProducedPerCraft > 1)
				{
					NumberSprite.draw(pagesOfCraftingRecipes[currentCraftingPage][key].numberProducedPerCraft, b, new Vector2(key.bounds.X + 64 - 2, key.bounds.Y + 64 - 2), Color.White, 0.5f * (key.scale / 4f), 0.97f, 1f, 0);
				}
			}
		}
		b.End();
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		if (hoverItem != null)
		{
			IClickableMenu.drawToolTip(b, hoverText, hoverTitle, hoverItem, heldItem != null);
		}
		else if (!string.IsNullOrEmpty(hoverText))
		{
			if (hoverAmount > 0)
			{
				IClickableMenu.drawToolTip(b, hoverText, hoverTitle, null, heldItem: true, -1, 0, null, -1, null, hoverAmount);
			}
			else
			{
				IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont, (heldItem != null) ? 64 : 0, (heldItem != null) ? 64 : 0);
			}
		}
		heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16), 1f);
		base.draw(b);
		if (downButton != null && currentCraftingPage < pagesOfCraftingRecipes.Count - 1)
		{
			downButton.draw(b);
		}
		if (upButton != null && currentCraftingPage > 0)
		{
			upButton.draw(b);
		}
		if (_standaloneMenu)
		{
			Game1.mouseCursorTransparency = 1f;
			drawMouse(b);
		}
		if (hoverRecipe == null)
		{
			return;
		}
		if (cooking && lastCookingHover.HasTypeObject() && Game1.objectData.TryGetValue(lastCookingHover.ItemId, out var value))
		{
			BuffEffects buffEffects = new BuffEffects();
			foreach (Buff item in Object.TryCreateBuffsFromData(value, lastCookingHover.Name, lastCookingHover.DisplayName, 1f, lastCookingHover.ModifyItemBuffs))
			{
				buffEffects.Add(item.effects);
			}
			if (buffEffects.HasAnyValue())
			{
				buffEffects.ToLegacyAttributeFormat();
			}
		}
		IClickableMenu.drawToolTip(b, " ", hoverRecipe.DisplayName + ((hoverRecipe.numberProducedPerCraft > 1) ? (" x" + hoverRecipe.numberProducedPerCraft) : ""), lastCookingHover, heldItem != null, -1, 0, null, -1, hoverRecipe, -1, getContainerContents());
	}

	protected override bool _ShouldAutoSnapPrioritizeAlignedElements()
	{
		return false;
	}

	public override bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
	{
		if ((a == downButton || a == upButton) && direction == 3 && b.region != 8000)
		{
			return false;
		}
		if (a.region == 8000 && (direction == 3 || direction == 1) && b.region == 9000)
		{
			return false;
		}
		if (a.region == 8000 && direction == 2 && (b == upButton || b == downButton))
		{
			return false;
		}
		return base.IsAutomaticSnapValid(direction, a, b);
	}

	public override void emergencyShutDown()
	{
		base.emergencyShutDown();
		if (heldItem != null)
		{
			Item item = heldItem;
			heldItem = null;
			Utility.CollectOrDrop(item);
		}
	}
}
