using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;

namespace StardewValley.Menus;

public class JunimoNoteMenu : IClickableMenu
{
	public const int region_ingredientSlotModifier = 250;

	public const int region_ingredientListModifier = 1000;

	public const int region_bundleModifier = 5000;

	public const int region_areaNextButton = 101;

	public const int region_areaBackButton = 102;

	public const int region_backButton = 103;

	public const int region_purchaseButton = 104;

	public const int region_presentButton = 105;

	public const string noteTextureName = "LooseSprites\\JunimoNote";

	public Texture2D noteTexture;

	public bool specificBundlePage;

	public const int baseWidth = 320;

	public const int baseHeight = 180;

	public InventoryMenu inventory;

	public Item partialDonationItem;

	public List<Item> partialDonationComponents = new List<Item>();

	public BundleIngredientDescription? currentPartialIngredientDescription;

	public int currentPartialIngredientDescriptionIndex = -1;

	public Item heldItem;

	public Item hoveredItem;

	public static bool canClick = true;

	public int whichArea;

	public int gameMenuTabToReturnTo = -1;

	public IClickableMenu menuToReturnTo;

	public bool bundlesChanged;

	public static ScreenSwipe screenSwipe;

	public static string hoverText = "";

	public List<Bundle> bundles = new List<Bundle>();

	public static TemporaryAnimatedSpriteList tempSprites = new TemporaryAnimatedSpriteList();

	public List<ClickableTextureComponent> ingredientSlots = new List<ClickableTextureComponent>();

	public List<ClickableTextureComponent> ingredientList = new List<ClickableTextureComponent>();

	public bool fromGameMenu;

	public bool fromThisMenu;

	public bool scrambledText;

	private bool singleBundleMenu;

	public ClickableTextureComponent backButton;

	public ClickableTextureComponent purchaseButton;

	public ClickableTextureComponent areaNextButton;

	public ClickableTextureComponent areaBackButton;

	public ClickableAnimatedComponent presentButton;

	public Action<int> onIngredientDeposit;

	public Action<JunimoNoteMenu> onBundleComplete;

	public Action<JunimoNoteMenu> onScreenSwipeFinished;

	public Bundle currentPageBundle;

	private int oldTriggerSpot;

	public JunimoNoteMenu(bool fromGameMenu, int area = 1, bool fromThisMenu = false)
		: base(Game1.uiViewport.Width / 2 - 640, Game1.uiViewport.Height / 2 - 360, 1280, 720, showUpperRightCloseButton: true)
	{
		CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
		if (fromGameMenu && !fromThisMenu)
		{
			for (int i = 0; i < communityCenter.areasComplete.Count; i++)
			{
				if (communityCenter.shouldNoteAppearInArea(i) && !communityCenter.areasComplete[i])
				{
					area = i;
					whichArea = area;
					break;
				}
			}
			if (Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("abandonedJojaMartAccessible") && !Game1.MasterPlayer.hasOrWillReceiveMail("ccMovieTheater"))
			{
				area = 6;
			}
		}
		setUpMenu(area, communityCenter.bundlesDict());
		Game1.player.forceCanMove();
		areaNextButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 128, yPositionOnScreen, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
		{
			visible = false,
			myID = 101,
			leftNeighborID = 102,
			leftNeighborImmutable = true,
			downNeighborID = -99998
		};
		areaBackButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 64, yPositionOnScreen, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
		{
			visible = false,
			myID = 102,
			rightNeighborID = 101,
			rightNeighborImmutable = true,
			downNeighborID = -99998
		};
		int num = 6;
		for (int j = 0; j < num; j++)
		{
			if (j != area && communityCenter.shouldNoteAppearInArea(j))
			{
				areaNextButton.visible = true;
				areaBackButton.visible = true;
				break;
			}
		}
		this.fromGameMenu = fromGameMenu;
		this.fromThisMenu = fromThisMenu;
		foreach (Bundle bundle in bundles)
		{
			bundle.depositsAllowed = false;
		}
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	public JunimoNoteMenu(int whichArea, Dictionary<int, bool[]> bundlesComplete)
		: base(Game1.uiViewport.Width / 2 - 640, Game1.uiViewport.Height / 2 - 360, 1280, 720, showUpperRightCloseButton: true)
	{
		setUpMenu(whichArea, bundlesComplete);
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	public JunimoNoteMenu(Bundle b, string noteTexturePath)
		: base(Game1.uiViewport.Width / 2 - 640, Game1.uiViewport.Height / 2 - 360, 1280, 720, showUpperRightCloseButton: true)
	{
		singleBundleMenu = true;
		whichArea = -1;
		noteTexture = Game1.temporaryContent.Load<Texture2D>(noteTexturePath);
		tempSprites.Clear();
		inventory = new InventoryMenu(xPositionOnScreen + 128, yPositionOnScreen + 140, playerInventory: true, null, HighlightObjects, 36, 6, 8, 8, drawSlots: false)
		{
			capacity = 36
		};
		for (int i = 0; i < inventory.inventory.Count; i++)
		{
			if (i >= inventory.actualInventory.Count)
			{
				inventory.inventory[i].visible = false;
			}
		}
		foreach (ClickableComponent item in inventory.GetBorder(InventoryMenu.BorderSide.Bottom))
		{
			item.downNeighborID = -99998;
		}
		foreach (ClickableComponent item2 in inventory.GetBorder(InventoryMenu.BorderSide.Right))
		{
			item2.rightNeighborID = -99998;
		}
		inventory.dropItemInvisibleButton.visible = false;
		canClick = true;
		setUpBundleSpecificPage(b);
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		if (specificBundlePage)
		{
			currentlySnappedComponent = getComponentWithID(0);
		}
		else
		{
			currentlySnappedComponent = getComponentWithID(5000);
		}
		snapCursorToCurrentSnappedComponent();
	}

	protected override bool _ShouldAutoSnapPrioritizeAlignedElements()
	{
		return !specificBundlePage;
	}

	protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
	{
		if (!Game1.player.hasOrWillReceiveMail("canReadJunimoText") || oldID - 5000 < 0 || oldID - 5000 >= 10 || currentlySnappedComponent == null)
		{
			return;
		}
		int num = -1;
		int num2 = 999999;
		Point center = currentlySnappedComponent.bounds.Center;
		for (int i = 0; i < bundles.Count; i++)
		{
			if (bundles[i].myID == oldID)
			{
				continue;
			}
			int num3 = 999999;
			Point center2 = bundles[i].bounds.Center;
			switch (direction)
			{
			case 3:
				if (center2.X < center.X)
				{
					num3 = center.X - center2.X + Math.Abs(center.Y - center2.Y) * 3;
				}
				break;
			case 0:
				if (center2.Y < center.Y)
				{
					num3 = center.Y - center2.Y + Math.Abs(center.X - center2.X) * 3;
				}
				break;
			case 1:
				if (center2.X > center.X)
				{
					num3 = center2.X - center.X + Math.Abs(center.Y - center2.Y) * 3;
				}
				break;
			case 2:
				if (center2.Y > center.Y)
				{
					num3 = center2.Y - center.Y + Math.Abs(center.X - center2.X) * 3;
				}
				break;
			}
			if (num3 < 10000 && num3 < num2)
			{
				num2 = num3;
				num = i;
			}
		}
		if (num != -1)
		{
			currentlySnappedComponent = getComponentWithID(num + 5000);
			snapCursorToCurrentSnappedComponent();
			return;
		}
		switch (direction)
		{
		case 2:
			if (presentButton != null)
			{
				currentlySnappedComponent = presentButton;
				snapCursorToCurrentSnappedComponent();
				presentButton.upNeighborID = oldID;
			}
			break;
		case 3:
			if (areaBackButton != null && areaBackButton.visible)
			{
				currentlySnappedComponent = areaBackButton;
				snapCursorToCurrentSnappedComponent();
				areaBackButton.rightNeighborID = oldID;
			}
			break;
		case 1:
			if (areaNextButton != null && areaNextButton.visible)
			{
				currentlySnappedComponent = areaNextButton;
				snapCursorToCurrentSnappedComponent();
				areaNextButton.leftNeighborID = oldID;
			}
			break;
		}
	}

	public void setUpMenu(int whichArea, Dictionary<int, bool[]> bundlesComplete)
	{
		noteTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\JunimoNote");
		if (!Game1.player.hasOrWillReceiveMail("seenJunimoNote"))
		{
			Game1.player.removeQuest("26");
			Game1.player.mailReceived.Add("seenJunimoNote");
		}
		if (!Game1.player.hasOrWillReceiveMail("wizardJunimoNote"))
		{
			Game1.addMailForTomorrow("wizardJunimoNote");
		}
		if (!Game1.player.hasOrWillReceiveMail("hasSeenAbandonedJunimoNote") && whichArea == 6)
		{
			Game1.player.mailReceived.Add("hasSeenAbandonedJunimoNote");
		}
		scrambledText = !Game1.player.hasOrWillReceiveMail("canReadJunimoText");
		tempSprites.Clear();
		this.whichArea = whichArea;
		inventory = new InventoryMenu(xPositionOnScreen + 128, yPositionOnScreen + 140, playerInventory: true, null, HighlightObjects, 36, 6, 8, 8, drawSlots: false)
		{
			capacity = 36
		};
		for (int i = 0; i < inventory.inventory.Count; i++)
		{
			if (i >= inventory.actualInventory.Count)
			{
				inventory.inventory[i].visible = false;
			}
		}
		foreach (ClickableComponent item in inventory.GetBorder(InventoryMenu.BorderSide.Bottom))
		{
			item.downNeighborID = -99998;
		}
		foreach (ClickableComponent item2 in inventory.GetBorder(InventoryMenu.BorderSide.Right))
		{
			item2.rightNeighborID = -99998;
		}
		inventory.dropItemInvisibleButton.visible = false;
		Dictionary<string, string> bundleData = Game1.netWorldState.Value.BundleData;
		string areaNameFromNumber = CommunityCenter.getAreaNameFromNumber(whichArea);
		int num = 0;
		foreach (string key in bundleData.Keys)
		{
			if (key.Contains(areaNameFromNumber))
			{
				int num2 = Convert.ToInt32(key.Split('/')[1]);
				bundles.Add(new Bundle(num2, bundleData[key], bundlesComplete[num2], getBundleLocationFromNumber(num), "LooseSprites\\JunimoNote", this)
				{
					myID = num + 5000,
					rightNeighborID = -7777,
					leftNeighborID = -7777,
					upNeighborID = -7777,
					downNeighborID = -7777,
					fullyImmutable = true
				});
				num++;
			}
		}
		backButton = new ClickableTextureComponent("Back", new Rectangle(xPositionOnScreen + IClickableMenu.borderWidth * 2 + 8, yPositionOnScreen + IClickableMenu.borderWidth * 2 + 4, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 44), 1f)
		{
			myID = 103
		};
		checkForRewards();
		canClick = true;
		Game1.playSound("shwip");
		bool flag = false;
		foreach (Bundle bundle in bundles)
		{
			if (!bundle.complete && !bundle.Equals(currentPageBundle))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
			communityCenter.markAreaAsComplete(whichArea);
			exitFunction = restoreAreaOnExit;
			communityCenter.areaCompleteReward(whichArea);
		}
	}

	public virtual bool HighlightObjects(Item item)
	{
		if (currentPageBundle != null)
		{
			if (partialDonationItem != null && currentPartialIngredientDescriptionIndex >= 0)
			{
				return currentPageBundle.IsValidItemForThisIngredientDescription(item, currentPageBundle.ingredients[currentPartialIngredientDescriptionIndex]);
			}
			foreach (BundleIngredientDescription ingredient in currentPageBundle.ingredients)
			{
				if (currentPageBundle.IsValidItemForThisIngredientDescription(item, ingredient))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override bool readyToClose()
	{
		if (!specificBundlePage || singleBundleMenu)
		{
			return isReadyToCloseMenuOrBundle();
		}
		return false;
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (!canClick)
		{
			return;
		}
		base.receiveLeftClick(x, y, playSound);
		if (scrambledText)
		{
			return;
		}
		if (specificBundlePage)
		{
			if (!currentPageBundle.complete && currentPageBundle.completionTimer <= 0)
			{
				heldItem = inventory.leftClick(x, y, heldItem);
			}
			if (backButton != null && backButton.containsPoint(x, y) && heldItem == null)
			{
				closeBundlePage();
			}
			if (partialDonationItem != null)
			{
				if (heldItem != null && Game1.oldKBState.IsKeyDown(Keys.LeftShift))
				{
					for (int i = 0; i < ingredientSlots.Count; i++)
					{
						if (ingredientSlots[i].item == partialDonationItem)
						{
							HandlePartialDonation(heldItem, ingredientSlots[i]);
						}
					}
				}
				else
				{
					for (int j = 0; j < ingredientSlots.Count; j++)
					{
						if (ingredientSlots[j].containsPoint(x, y) && ingredientSlots[j].item == partialDonationItem)
						{
							if (heldItem != null)
							{
								HandlePartialDonation(heldItem, ingredientSlots[j]);
								return;
							}
							bool flag = Game1.oldKBState.IsKeyDown(Keys.LeftShift);
							ReturnPartialDonations(!flag);
							return;
						}
					}
				}
			}
			else if (heldItem != null)
			{
				if (Game1.oldKBState.IsKeyDown(Keys.LeftShift))
				{
					for (int k = 0; k < ingredientSlots.Count; k++)
					{
						if (currentPageBundle.canAcceptThisItem(heldItem, ingredientSlots[k]))
						{
							if (ingredientSlots[k].item == null)
							{
								heldItem = currentPageBundle.tryToDepositThisItem(heldItem, ingredientSlots[k], "LooseSprites\\JunimoNote", this);
								checkIfBundleIsComplete();
								return;
							}
						}
						else if (ingredientSlots[k].item == null)
						{
							HandlePartialDonation(heldItem, ingredientSlots[k]);
						}
					}
				}
				for (int l = 0; l < ingredientSlots.Count; l++)
				{
					if (ingredientSlots[l].containsPoint(x, y))
					{
						if (currentPageBundle.canAcceptThisItem(heldItem, ingredientSlots[l]))
						{
							heldItem = currentPageBundle.tryToDepositThisItem(heldItem, ingredientSlots[l], "LooseSprites\\JunimoNote", this);
							checkIfBundleIsComplete();
						}
						else if (ingredientSlots[l].item == null)
						{
							HandlePartialDonation(heldItem, ingredientSlots[l]);
						}
					}
				}
			}
			if (purchaseButton != null && purchaseButton.containsPoint(x, y))
			{
				int stack = currentPageBundle.ingredients.Last().stack;
				if (Game1.player.Money >= stack)
				{
					Game1.player.Money -= stack;
					Game1.playSound("select");
					currentPageBundle.completionAnimation(this);
					if (purchaseButton != null)
					{
						purchaseButton.scale = purchaseButton.baseScale * 0.75f;
					}
					CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
					communityCenter.bundleRewards[currentPageBundle.bundleIndex] = true;
					communityCenter.bundles.FieldDict[currentPageBundle.bundleIndex][0] = true;
					checkForRewards();
					bool flag2 = false;
					foreach (Bundle bundle in bundles)
					{
						if (!bundle.complete && !bundle.Equals(currentPageBundle))
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						communityCenter.markAreaAsComplete(whichArea);
						exitFunction = restoreAreaOnExit;
						communityCenter.areaCompleteReward(whichArea);
					}
					else
					{
						communityCenter.getJunimoForArea(whichArea)?.bringBundleBackToHut(Bundle.getColorFromColorIndex(currentPageBundle.bundleColor), Game1.RequireLocation("CommunityCenter"));
					}
					Game1.multiplayer.globalChatInfoMessage("Bundle");
				}
				else
				{
					Game1.dayTimeMoneyBox.moneyShakeTimer = 600;
				}
			}
			if (upperRightCloseButton != null && isReadyToCloseMenuOrBundle() && upperRightCloseButton.containsPoint(x, y))
			{
				closeBundlePage();
				return;
			}
		}
		else
		{
			foreach (Bundle bundle2 in bundles)
			{
				if (bundle2.canBeClicked() && bundle2.containsPoint(x, y))
				{
					setUpBundleSpecificPage(bundle2);
					Game1.playSound("shwip");
					return;
				}
			}
			if (presentButton != null && presentButton.containsPoint(x, y) && !fromGameMenu && !fromThisMenu)
			{
				openRewardsMenu();
			}
			if (fromGameMenu)
			{
				if (areaNextButton.containsPoint(x, y))
				{
					SwapPage(1);
				}
				else if (areaBackButton.containsPoint(x, y))
				{
					SwapPage(-1);
				}
			}
		}
		if (heldItem != null && !isWithinBounds(x, y) && heldItem.canBeTrashed())
		{
			Game1.playSound("throwDownITem");
			Game1.createItemDebris(heldItem, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
			heldItem = null;
		}
	}

	public virtual void ReturnPartialDonation(Item item, bool play_sound = true)
	{
		List<Item> list = new List<Item>();
		Item item2 = Game1.player.addItemToInventory(item, list);
		foreach (Item item3 in list)
		{
			inventory.ShakeItem(item3);
		}
		if (item2 != null)
		{
			Utility.CollectOrDrop(item2);
			inventory.ShakeItem(item2);
		}
		if (play_sound)
		{
			Game1.playSound("coin");
		}
	}

	public virtual void ReturnPartialDonations(bool to_hand = true)
	{
		if (partialDonationComponents.Count > 0)
		{
			bool play_sound = true;
			foreach (Item partialDonationComponent in partialDonationComponents)
			{
				if (heldItem == null && to_hand)
				{
					Game1.playSound("dwop");
					heldItem = partialDonationComponent;
				}
				else
				{
					ReturnPartialDonation(partialDonationComponent, play_sound);
					play_sound = false;
				}
			}
		}
		ResetPartialDonation();
	}

	public virtual void ResetPartialDonation()
	{
		partialDonationComponents.Clear();
		currentPartialIngredientDescription = null;
		currentPartialIngredientDescriptionIndex = -1;
		foreach (ClickableTextureComponent ingredientSlot in ingredientSlots)
		{
			if (ingredientSlot.item == partialDonationItem)
			{
				ingredientSlot.item = null;
			}
		}
		partialDonationItem = null;
	}

	public virtual bool CanBePartiallyOrFullyDonated(Item item)
	{
		if (currentPageBundle == null)
		{
			return false;
		}
		int bundleIngredientDescriptionIndexForItem = currentPageBundle.GetBundleIngredientDescriptionIndexForItem(item);
		if (bundleIngredientDescriptionIndexForItem < 0)
		{
			return false;
		}
		BundleIngredientDescription ingredient = currentPageBundle.ingredients[bundleIngredientDescriptionIndexForItem];
		int num = 0;
		if (currentPageBundle.IsValidItemForThisIngredientDescription(item, ingredient))
		{
			num += item.Stack;
		}
		foreach (Item item2 in Game1.player.Items)
		{
			if (currentPageBundle.IsValidItemForThisIngredientDescription(item2, ingredient))
			{
				num += item2.Stack;
			}
		}
		if (bundleIngredientDescriptionIndexForItem == currentPartialIngredientDescriptionIndex && partialDonationItem != null)
		{
			num += partialDonationItem.Stack;
		}
		return num >= ingredient.stack;
	}

	public virtual void HandlePartialDonation(Item item, ClickableTextureComponent slot)
	{
		if ((currentPageBundle != null && !currentPageBundle.depositsAllowed) || (partialDonationItem != null && slot.item != partialDonationItem) || !CanBePartiallyOrFullyDonated(item))
		{
			return;
		}
		if (!currentPartialIngredientDescription.HasValue)
		{
			currentPartialIngredientDescriptionIndex = currentPageBundle.GetBundleIngredientDescriptionIndexForItem(item);
			if (currentPartialIngredientDescriptionIndex != -1)
			{
				currentPartialIngredientDescription = currentPageBundle.ingredients[currentPartialIngredientDescriptionIndex];
			}
		}
		if (!currentPartialIngredientDescription.HasValue || !currentPageBundle.IsValidItemForThisIngredientDescription(item, currentPartialIngredientDescription.Value))
		{
			return;
		}
		bool flag = true;
		bool flag2 = item == heldItem;
		int num;
		if (slot.item == null)
		{
			Game1.playSound("sell");
			flag = false;
			partialDonationItem = item.getOne();
			num = Math.Min(currentPartialIngredientDescription.Value.stack, item.Stack);
			partialDonationItem.Stack = num;
			item = item.ConsumeStack(num);
			partialDonationItem.Quality = currentPartialIngredientDescription.Value.quality;
			slot.item = partialDonationItem;
			slot.sourceRect.X = 512;
			slot.sourceRect.Y = 244;
		}
		else
		{
			num = Math.Min(currentPartialIngredientDescription.Value.stack - partialDonationItem.Stack, item.Stack);
			partialDonationItem.Stack += num;
			item = item.ConsumeStack(num);
		}
		if (num > 0)
		{
			Item one = heldItem.getOne();
			one.Stack = num;
			foreach (Item partialDonationComponent in partialDonationComponents)
			{
				if (partialDonationComponent.canStackWith(heldItem))
				{
					one.Stack = partialDonationComponent.addToStack(one);
				}
			}
			if (one.Stack > 0)
			{
				partialDonationComponents.Add(one);
			}
			partialDonationComponents.Sort((Item a, Item b) => b.Stack.CompareTo(a.Stack));
		}
		if (flag2 && item == null)
		{
			heldItem = null;
		}
		if (partialDonationItem.Stack >= currentPartialIngredientDescription.Value.stack)
		{
			slot.item = null;
			partialDonationItem = currentPageBundle.tryToDepositThisItem(partialDonationItem, slot, "LooseSprites\\JunimoNote", this);
			Item item2 = partialDonationItem;
			if (item2 != null && item2.Stack > 0)
			{
				ReturnPartialDonation(partialDonationItem);
			}
			partialDonationItem = null;
			ResetPartialDonation();
			checkIfBundleIsComplete();
		}
		else if (num > 0 && flag)
		{
			Game1.playSound("sell");
		}
	}

	public bool isReadyToCloseMenuOrBundle()
	{
		if (specificBundlePage)
		{
			Bundle bundle = currentPageBundle;
			if (bundle != null && bundle.completionTimer > 0)
			{
				return false;
			}
		}
		if (heldItem != null)
		{
			return false;
		}
		return true;
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		base.receiveGamePadButton(button);
		if (specificBundlePage)
		{
			switch (button)
			{
			case Buttons.RightTrigger:
			{
				ClickableComponent clickableComponent2 = currentlySnappedComponent;
				if (clickableComponent2 == null || clickableComponent2.myID >= 50)
				{
					break;
				}
				oldTriggerSpot = currentlySnappedComponent.myID;
				int currentlySnappedComponentTo = 250;
				foreach (ClickableTextureComponent ingredientSlot in ingredientSlots)
				{
					if (ingredientSlot.item == null)
					{
						currentlySnappedComponentTo = ingredientSlot.myID;
						break;
					}
				}
				setCurrentlySnappedComponentTo(currentlySnappedComponentTo);
				snapCursorToCurrentSnappedComponent();
				break;
			}
			case Buttons.LeftTrigger:
			{
				ClickableComponent clickableComponent = currentlySnappedComponent;
				if (clickableComponent != null && clickableComponent.myID >= 250)
				{
					setCurrentlySnappedComponentTo(oldTriggerSpot);
					snapCursorToCurrentSnappedComponent();
				}
				break;
			}
			}
		}
		else if (fromGameMenu)
		{
			switch (button)
			{
			case Buttons.RightTrigger:
				SwapPage(1);
				break;
			case Buttons.LeftTrigger:
				SwapPage(-1);
				break;
			}
		}
	}

	public void SwapPage(int direction)
	{
		if ((direction > 0 && !areaNextButton.visible) || (direction < 0 && !areaBackButton.visible))
		{
			return;
		}
		CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
		int num = whichArea;
		int num2 = 6;
		for (int i = 0; i < num2; i++)
		{
			num += direction;
			if (num < 0)
			{
				num += num2;
			}
			if (num >= num2)
			{
				num -= num2;
			}
			if (communityCenter.shouldNoteAppearInArea(num))
			{
				int num3 = -1;
				if (currentlySnappedComponent != null && (currentlySnappedComponent.myID >= 5000 || currentlySnappedComponent.myID == 101 || currentlySnappedComponent.myID == 102))
				{
					num3 = currentlySnappedComponent.myID;
				}
				JunimoNoteMenu junimoNoteMenu = (JunimoNoteMenu)(Game1.activeClickableMenu = new JunimoNoteMenu(fromGameMenu: true, num, fromThisMenu: true)
				{
					gameMenuTabToReturnTo = gameMenuTabToReturnTo
				});
				if (num3 >= 0)
				{
					junimoNoteMenu.currentlySnappedComponent = junimoNoteMenu.getComponentWithID(currentlySnappedComponent.myID);
					junimoNoteMenu.snapCursorToCurrentSnappedComponent();
				}
				if (junimoNoteMenu.getComponentWithID(areaNextButton.leftNeighborID) != null)
				{
					junimoNoteMenu.areaNextButton.leftNeighborID = areaNextButton.leftNeighborID;
				}
				else
				{
					junimoNoteMenu.areaNextButton.leftNeighborID = junimoNoteMenu.areaBackButton.myID;
				}
				junimoNoteMenu.areaNextButton.rightNeighborID = areaNextButton.rightNeighborID;
				junimoNoteMenu.areaNextButton.upNeighborID = areaNextButton.upNeighborID;
				junimoNoteMenu.areaNextButton.downNeighborID = areaNextButton.downNeighborID;
				if (junimoNoteMenu.getComponentWithID(areaBackButton.rightNeighborID) != null)
				{
					junimoNoteMenu.areaBackButton.leftNeighborID = areaBackButton.leftNeighborID;
				}
				else
				{
					junimoNoteMenu.areaBackButton.leftNeighborID = junimoNoteMenu.areaNextButton.myID;
				}
				junimoNoteMenu.areaBackButton.rightNeighborID = areaBackButton.rightNeighborID;
				junimoNoteMenu.areaBackButton.upNeighborID = areaBackButton.upNeighborID;
				junimoNoteMenu.areaBackButton.downNeighborID = areaBackButton.downNeighborID;
				break;
			}
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (gameMenuTabToReturnTo != -1)
		{
			closeSound = "shwip";
		}
		base.receiveKeyPress(key);
		if (key == Keys.Delete && heldItem != null && heldItem.canBeTrashed())
		{
			Utility.trashItem(heldItem);
			heldItem = null;
		}
		if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && isReadyToCloseMenuOrBundle())
		{
			if (singleBundleMenu)
			{
				exitThisMenu(gameMenuTabToReturnTo == -1);
			}
			closeBundlePage();
		}
	}

	/// <inheritdoc />
	protected override void cleanupBeforeExit()
	{
		base.cleanupBeforeExit();
		if (gameMenuTabToReturnTo != -1)
		{
			Game1.activeClickableMenu = new GameMenu(gameMenuTabToReturnTo, -1, playOpeningSound: false);
		}
		else if (menuToReturnTo != null)
		{
			Game1.activeClickableMenu = menuToReturnTo;
		}
	}

	private void closeBundlePage()
	{
		if (partialDonationItem != null)
		{
			ReturnPartialDonations(to_hand: false);
		}
		else if (specificBundlePage)
		{
			hoveredItem = null;
			inventory.descriptionText = "";
			if (heldItem == null)
			{
				takeDownBundleSpecificPage();
				Game1.playSound("shwip");
			}
			else
			{
				heldItem = inventory.tryToAddItem(heldItem);
			}
		}
	}

	private void reOpenThisMenu()
	{
		bool num = specificBundlePage;
		JunimoNoteMenu junimoNoteMenu = ((!fromGameMenu && !fromThisMenu) ? new JunimoNoteMenu(whichArea, Game1.RequireLocation<CommunityCenter>("CommunityCenter").bundlesDict())
		{
			gameMenuTabToReturnTo = gameMenuTabToReturnTo,
			menuToReturnTo = menuToReturnTo
		} : new JunimoNoteMenu(fromGameMenu, whichArea, fromThisMenu)
		{
			gameMenuTabToReturnTo = gameMenuTabToReturnTo,
			menuToReturnTo = menuToReturnTo
		});
		if (num)
		{
			foreach (Bundle bundle in junimoNoteMenu.bundles)
			{
				if (bundle.bundleIndex == currentPageBundle.bundleIndex)
				{
					junimoNoteMenu.setUpBundleSpecificPage(bundle);
					break;
				}
			}
		}
		Game1.activeClickableMenu = junimoNoteMenu;
	}

	private void updateIngredientSlots()
	{
		int num = 0;
		foreach (BundleIngredientDescription ingredient in currentPageBundle.ingredients)
		{
			if (ingredient.completed && num < ingredientSlots.Count)
			{
				string representativeItemId = GetRepresentativeItemId(ingredient);
				if (ingredient.preservesId != null)
				{
					ingredientSlots[num].item = Utility.CreateFlavoredItem(representativeItemId, ingredient.preservesId, ingredient.quality, ingredient.stack);
				}
				else
				{
					ingredientSlots[num].item = ItemRegistry.Create(representativeItemId, ingredient.stack, ingredient.quality);
				}
				currentPageBundle.ingredientDepositAnimation(ingredientSlots[num], "LooseSprites\\JunimoNote", skipAnimation: true);
				num++;
			}
		}
	}

	/// <summary>Get the qualified item ID to draw in the bundle UI for an ingredient.</summary>
	/// <param name="ingredient">The ingredient to represent.</param>
	public static string GetRepresentativeItemId(BundleIngredientDescription ingredient)
	{
		if (ingredient.category.HasValue)
		{
			foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
			{
				if (allDatum.Category == ingredient.category)
				{
					return allDatum.QualifiedItemId;
				}
			}
			return "0";
		}
		return ingredient.id;
	}

	public static void GetBundleRewards(int area, List<Item> rewards)
	{
		CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
		Dictionary<string, string> bundleData = Game1.netWorldState.Value.BundleData;
		foreach (string key in bundleData.Keys)
		{
			if (key.Contains(CommunityCenter.getAreaNameFromNumber(area)))
			{
				int num = Convert.ToInt32(key.Split('/')[1]);
				if (communityCenter.bundleRewards[num])
				{
					Item itemFromStandardTextDescription = Utility.getItemFromStandardTextDescription(bundleData[key].Split('/')[1], Game1.player);
					itemFromStandardTextDescription.SpecialVariable = num;
					rewards.Add(itemFromStandardTextDescription);
				}
			}
		}
	}

	private void openRewardsMenu()
	{
		Game1.playSound("smallSelect");
		List<Item> rewards = new List<Item>();
		GetBundleRewards(whichArea, rewards);
		Game1.activeClickableMenu = new ItemGrabMenu(rewards, reverseGrab: false, showReceivingMenu: true, null, null, null, rewardGrabbed, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: false, 0, null, -1, this);
		Game1.activeClickableMenu.exitFunction = ((exitFunction != null) ? exitFunction : new onExit(reOpenThisMenu));
	}

	private void rewardGrabbed(Item item, Farmer who)
	{
		Game1.RequireLocation<CommunityCenter>("CommunityCenter").bundleRewards[item.SpecialVariable] = false;
	}

	private void checkIfBundleIsComplete()
	{
		ReturnPartialDonations();
		if (!specificBundlePage || currentPageBundle == null)
		{
			return;
		}
		int num = 0;
		foreach (ClickableTextureComponent ingredientSlot in ingredientSlots)
		{
			if (ingredientSlot.item != null && ingredientSlot.item != partialDonationItem)
			{
				num++;
			}
		}
		if (num < currentPageBundle.numberOfIngredientSlots)
		{
			return;
		}
		if (heldItem != null)
		{
			Game1.player.addItemToInventory(heldItem);
			heldItem = null;
		}
		if (!singleBundleMenu)
		{
			CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
			for (int i = 0; i < communityCenter.bundles[currentPageBundle.bundleIndex].Length; i++)
			{
				communityCenter.bundles.FieldDict[currentPageBundle.bundleIndex][i] = true;
			}
			communityCenter.checkForNewJunimoNotes();
			screenSwipe = new ScreenSwipe(0, -1f, -1, width, height);
			currentPageBundle.completionAnimation(this, playSound: true, 400);
			canClick = false;
			communityCenter.bundleRewards[currentPageBundle.bundleIndex] = true;
			Game1.multiplayer.globalChatInfoMessage("Bundle");
			bool flag = false;
			foreach (Bundle bundle in bundles)
			{
				if (!bundle.complete && !bundle.Equals(currentPageBundle))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				if (whichArea == 6)
				{
					exitFunction = restoreaAreaOnExit_AbandonedJojaMart;
				}
				else
				{
					communityCenter.markAreaAsComplete(whichArea);
					exitFunction = restoreAreaOnExit;
					communityCenter.areaCompleteReward(whichArea);
				}
			}
			else
			{
				communityCenter.getJunimoForArea(whichArea)?.bringBundleBackToHut(Bundle.getColorFromColorIndex(currentPageBundle.bundleColor), communityCenter);
			}
			checkForRewards();
		}
		else if (onBundleComplete != null)
		{
			onBundleComplete(this);
		}
	}

	private void restoreaAreaOnExit_AbandonedJojaMart()
	{
		Game1.RequireLocation<AbandonedJojaMart>("AbandonedJojaMart").restoreAreaCutscene();
	}

	private void restoreAreaOnExit()
	{
		if (!fromGameMenu)
		{
			Game1.RequireLocation<CommunityCenter>("CommunityCenter").restoreAreaCutscene(whichArea);
		}
	}

	public void checkForRewards()
	{
		Dictionary<string, string> bundleData = Game1.netWorldState.Value.BundleData;
		foreach (string key2 in bundleData.Keys)
		{
			if (key2.Contains(CommunityCenter.getAreaNameFromNumber(whichArea)) && bundleData[key2].Split('/')[1].Length > 1)
			{
				int key = Convert.ToInt32(key2.Split('/')[1]);
				if (Game1.RequireLocation<CommunityCenter>("CommunityCenter").bundleRewards[key])
				{
					presentButton = new ClickableAnimatedComponent(new Rectangle(xPositionOnScreen + 592, yPositionOnScreen + 512, 72, 72), "", Game1.content.LoadString("Strings\\StringsFromCSFiles:JunimoNoteMenu.cs.10783"), new TemporaryAnimatedSprite("LooseSprites\\JunimoNote", new Rectangle(548, 262, 18, 20), 70f, 4, 99999, new Vector2(-64f, -64f), flicker: false, flipped: false, 0.5f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true));
					break;
				}
			}
		}
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (!canClick)
		{
			return;
		}
		if (specificBundlePage)
		{
			heldItem = inventory.rightClick(x, y, heldItem);
			if (partialDonationItem != null)
			{
				for (int i = 0; i < ingredientSlots.Count; i++)
				{
					if (!ingredientSlots[i].containsPoint(x, y) || ingredientSlots[i].item != partialDonationItem)
					{
						continue;
					}
					if (partialDonationComponents.Count <= 0)
					{
						break;
					}
					Item one = partialDonationComponents[0].getOne();
					bool flag = false;
					if (heldItem == null)
					{
						heldItem = one;
						Game1.playSound("dwop");
						flag = true;
					}
					else if (heldItem.canStackWith(one))
					{
						heldItem.addToStack(one);
						Game1.playSound("dwop");
						flag = true;
					}
					if (!flag)
					{
						break;
					}
					if (partialDonationComponents[0].ConsumeStack(1) == null)
					{
						partialDonationComponents.RemoveAt(0);
					}
					if (partialDonationItem != null)
					{
						int num = 0;
						foreach (Item partialDonationComponent in partialDonationComponents)
						{
							num += partialDonationComponent.Stack;
						}
						partialDonationItem.Stack = num;
					}
					if (partialDonationComponents.Count == 0)
					{
						ResetPartialDonation();
					}
					break;
				}
			}
		}
		if (!specificBundlePage && isReadyToCloseMenuOrBundle())
		{
			exitThisMenu(gameMenuTabToReturnTo == -1);
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		if (specificBundlePage && currentPageBundle != null && currentPageBundle.completionTimer <= 0 && isReadyToCloseMenuOrBundle() && currentPageBundle.complete)
		{
			takeDownBundleSpecificPage();
		}
		foreach (Bundle bundle in bundles)
		{
			bundle.update(time);
		}
		tempSprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.update(time));
		presentButton?.update(time);
		if (screenSwipe != null)
		{
			canClick = false;
			if (screenSwipe.update(time))
			{
				screenSwipe = null;
				canClick = true;
				onScreenSwipeFinished?.Invoke(this);
			}
		}
		if (bundlesChanged && fromGameMenu)
		{
			reOpenThisMenu();
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		if (scrambledText)
		{
			return;
		}
		hoverText = "";
		if (specificBundlePage)
		{
			backButton?.tryHover(x, y);
			if (!currentPageBundle.complete && currentPageBundle.completionTimer <= 0)
			{
				hoveredItem = inventory.hover(x, y, heldItem);
			}
			else
			{
				hoveredItem = null;
			}
			foreach (ClickableTextureComponent ingredient in ingredientList)
			{
				if (ingredient.bounds.Contains(x, y))
				{
					hoverText = ingredient.hoverText;
					break;
				}
			}
			if (heldItem != null)
			{
				foreach (ClickableTextureComponent ingredientSlot in ingredientSlots)
				{
					if (ingredientSlot.bounds.Contains(x, y) && CanBePartiallyOrFullyDonated(heldItem) && (partialDonationItem == null || ingredientSlot.item == partialDonationItem))
					{
						ingredientSlot.sourceRect.X = 530;
						ingredientSlot.sourceRect.Y = 262;
					}
					else
					{
						ingredientSlot.sourceRect.X = 512;
						ingredientSlot.sourceRect.Y = 244;
					}
				}
			}
			purchaseButton?.tryHover(x, y);
			return;
		}
		if (presentButton != null)
		{
			hoverText = presentButton.tryHover(x, y);
		}
		foreach (Bundle bundle in bundles)
		{
			bundle.tryHoverAction(x, y);
		}
		if (fromGameMenu)
		{
			areaNextButton.tryHover(x, y);
			areaBackButton.tryHover(x, y);
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (Game1.options.showMenuBackground)
		{
			base.drawBackground(b);
		}
		else if (!Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
		}
		if (!specificBundlePage)
		{
			b.Draw(noteTexture, new Vector2(xPositionOnScreen, yPositionOnScreen), new Rectangle(0, 0, 320, 180), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.1f);
			SpriteText.drawStringHorizontallyCenteredAt(b, scrambledText ? CommunityCenter.getAreaEnglishDisplayNameFromNumber(whichArea) : CommunityCenter.getAreaDisplayNameFromNumber(whichArea), xPositionOnScreen + width / 2 + 16, yPositionOnScreen + 12, 999999, -1, 99999, 0.88f, 0.88f, scrambledText);
			if (scrambledText)
			{
				SpriteText.drawString(b, LocalizedContentManager.CurrentLanguageLatin ? Game1.content.LoadString("Strings\\StringsFromCSFiles:JunimoNoteMenu.cs.10786") : Game1.content.LoadBaseString("Strings\\StringsFromCSFiles:JunimoNoteMenu.cs.10786"), xPositionOnScreen + 96, yPositionOnScreen + 96, 999999, width - 192, 99999, 0.88f, 0.88f, junimoText: true);
				base.draw(b);
				if (!Game1.options.SnappyMenus && canClick)
				{
					drawMouse(b);
				}
				return;
			}
			foreach (Bundle bundle in bundles)
			{
				bundle.draw(b);
			}
			presentButton?.draw(b);
			foreach (TemporaryAnimatedSprite tempSprite in tempSprites)
			{
				tempSprite.draw(b, localPosition: true);
			}
			if (fromGameMenu)
			{
				if (areaNextButton.visible)
				{
					areaNextButton.draw(b);
				}
				if (areaBackButton.visible)
				{
					areaBackButton.draw(b);
				}
			}
		}
		else
		{
			b.Draw(noteTexture, new Vector2(xPositionOnScreen, yPositionOnScreen), new Rectangle(320, 0, 320, 180), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.1f);
			if (currentPageBundle != null)
			{
				int num = currentPageBundle.bundleIndex;
				Texture2D bundleTextureOverride = noteTexture;
				int num2 = 180;
				if (currentPageBundle.bundleTextureIndexOverride >= 0)
				{
					num = currentPageBundle.bundleTextureIndexOverride;
				}
				if (currentPageBundle.bundleTextureOverride != null)
				{
					bundleTextureOverride = currentPageBundle.bundleTextureOverride;
					num2 = 0;
				}
				b.Draw(bundleTextureOverride, new Vector2(xPositionOnScreen + 872, yPositionOnScreen + 88), new Rectangle(num * 16 * 2 % bundleTextureOverride.Width, num2 + 32 * (num * 16 * 2 / bundleTextureOverride.Width), 32, 32), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.15f);
				if (currentPageBundle.label != null)
				{
					float x = Game1.dialogueFont.MeasureString((!Game1.player.hasOrWillReceiveMail("canReadJunimoText")) ? "???" : Game1.content.LoadString("Strings\\UI:JunimoNote_BundleName", currentPageBundle.label)).X;
					b.Draw(noteTexture, new Vector2(xPositionOnScreen + 936 - (int)x / 2 - 16, yPositionOnScreen + 228), new Rectangle(517, 266, 4, 17), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.1f);
					b.Draw(noteTexture, new Rectangle(xPositionOnScreen + 936 - (int)x / 2, yPositionOnScreen + 228, (int)x, 68), new Rectangle(520, 266, 1, 17), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.1f);
					b.Draw(noteTexture, new Vector2(xPositionOnScreen + 936 + (int)x / 2, yPositionOnScreen + 228), new Rectangle(524, 266, 4, 17), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.1f);
					b.DrawString(Game1.dialogueFont, (!Game1.player.hasOrWillReceiveMail("canReadJunimoText")) ? "???" : Game1.content.LoadString("Strings\\UI:JunimoNote_BundleName", currentPageBundle.label), new Vector2((float)(xPositionOnScreen + 936) - x / 2f, yPositionOnScreen + 236) + new Vector2(2f, 2f), Game1.textShadowColor);
					b.DrawString(Game1.dialogueFont, (!Game1.player.hasOrWillReceiveMail("canReadJunimoText")) ? "???" : Game1.content.LoadString("Strings\\UI:JunimoNote_BundleName", currentPageBundle.label), new Vector2((float)(xPositionOnScreen + 936) - x / 2f, yPositionOnScreen + 236) + new Vector2(0f, 2f), Game1.textShadowColor);
					b.DrawString(Game1.dialogueFont, (!Game1.player.hasOrWillReceiveMail("canReadJunimoText")) ? "???" : Game1.content.LoadString("Strings\\UI:JunimoNote_BundleName", currentPageBundle.label), new Vector2((float)(xPositionOnScreen + 936) - x / 2f, yPositionOnScreen + 236) + new Vector2(2f, 0f), Game1.textShadowColor);
					b.DrawString(Game1.dialogueFont, (!Game1.player.hasOrWillReceiveMail("canReadJunimoText")) ? "???" : Game1.content.LoadString("Strings\\UI:JunimoNote_BundleName", currentPageBundle.label), new Vector2((float)(xPositionOnScreen + 936) - x / 2f, yPositionOnScreen + 236), Game1.textColor * 0.9f);
				}
			}
			if (backButton != null)
			{
				backButton.draw(b);
			}
			if (purchaseButton != null)
			{
				purchaseButton.draw(b);
				Game1.dayTimeMoneyBox.drawMoneyBox(b);
			}
			float extraAlpha = 1f;
			if (partialDonationItem != null)
			{
				extraAlpha = 0.25f;
			}
			foreach (TemporaryAnimatedSprite tempSprite2 in tempSprites)
			{
				tempSprite2.draw(b, localPosition: true, 0, 0, extraAlpha);
			}
			foreach (ClickableTextureComponent ingredientSlot in ingredientSlots)
			{
				float num3 = 1f;
				if (partialDonationItem != null && ingredientSlot.item != partialDonationItem)
				{
					num3 = 0.25f;
				}
				if (ingredientSlot.item == null || (partialDonationItem != null && ingredientSlot.item == partialDonationItem))
				{
					ingredientSlot.draw(b, (fromGameMenu ? (Color.LightGray * 0.5f) : Color.White) * num3, 0.89f);
				}
				ingredientSlot.drawItem(b, 4, 4, num3);
			}
			for (int i = 0; i < ingredientList.Count; i++)
			{
				float num4 = 1f;
				if (currentPartialIngredientDescriptionIndex >= 0 && currentPartialIngredientDescriptionIndex != i)
				{
					num4 = 0.25f;
				}
				ClickableTextureComponent clickableTextureComponent = ingredientList[i];
				bool flag = false;
				if (i < currentPageBundle?.ingredients?.Count && currentPageBundle.ingredients[i].completed)
				{
					flag = true;
				}
				if (!flag)
				{
					b.Draw(Game1.shadowTexture, new Vector2(clickableTextureComponent.bounds.Center.X - Game1.shadowTexture.Bounds.Width * 4 / 2 - 4, clickableTextureComponent.bounds.Center.Y + 4), Game1.shadowTexture.Bounds, Color.White * num4, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.1f);
				}
				if (clickableTextureComponent.item != null && clickableTextureComponent.visible)
				{
					clickableTextureComponent.item.drawInMenu(b, new Vector2(clickableTextureComponent.bounds.X, clickableTextureComponent.bounds.Y), clickableTextureComponent.scale / 4f, 1f, 0.9f, StackDrawType.Draw, Color.White * (flag ? 0.25f : num4), drawShadow: false);
				}
			}
			inventory.draw(b);
		}
		if (getRewardNameForArea(whichArea) != "")
		{
			SpriteText.drawStringWithScrollCenteredAt(b, getRewardNameForArea(whichArea), xPositionOnScreen + width / 2, Math.Min(yPositionOnScreen + height + 20, Game1.uiViewport.Height - 64 - 8));
		}
		base.draw(b);
		Game1.mouseCursorTransparency = 1f;
		if (canClick)
		{
			drawMouse(b);
		}
		heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16), 1f);
		if (inventory.descriptionText.Length > 0)
		{
			if (hoveredItem != null)
			{
				IClickableMenu.drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem);
			}
		}
		else
		{
			IClickableMenu.drawHoverText(b, (!singleBundleMenu && !Game1.player.hasOrWillReceiveMail("canReadJunimoText") && hoverText.Length > 0) ? "???" : hoverText, Game1.dialogueFont);
		}
		screenSwipe?.draw(b);
	}

	public string getRewardNameForArea(int whichArea)
	{
		return whichArea switch
		{
			-1 => "", 
			3 => Game1.content.LoadString("Strings\\UI:JunimoNote_RewardBoiler"), 
			5 => Game1.content.LoadString("Strings\\UI:JunimoNote_RewardBulletin"), 
			1 => Game1.content.LoadString("Strings\\UI:JunimoNote_RewardCrafts"), 
			0 => Game1.content.LoadString("Strings\\UI:JunimoNote_RewardPantry"), 
			4 => Game1.content.LoadString("Strings\\UI:JunimoNote_RewardVault"), 
			2 => Game1.content.LoadString("Strings\\UI:JunimoNote_RewardFishTank"), 
			_ => "???", 
		};
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		tempSprites.Clear();
		xPositionOnScreen = Game1.uiViewport.Width / 2 - 640;
		yPositionOnScreen = Game1.uiViewport.Height / 2 - 360;
		backButton = new ClickableTextureComponent("Back", new Rectangle(xPositionOnScreen + IClickableMenu.borderWidth * 2 + 8, yPositionOnScreen + IClickableMenu.borderWidth * 2 + 4, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 44), 1f);
		if (fromGameMenu)
		{
			areaNextButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 128, yPositionOnScreen, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
			{
				visible = false
			};
			areaBackButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 64, yPositionOnScreen, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
			{
				visible = false
			};
		}
		inventory = new InventoryMenu(xPositionOnScreen + 128, yPositionOnScreen + 140, playerInventory: true, null, HighlightObjects, Game1.player.maxItems.Value, 6, 8, 8, drawSlots: false);
		for (int i = 0; i < inventory.inventory.Count; i++)
		{
			if (i >= inventory.actualInventory.Count)
			{
				inventory.inventory[i].visible = false;
			}
		}
		for (int j = 0; j < bundles.Count; j++)
		{
			Point bundleLocationFromNumber = getBundleLocationFromNumber(j);
			bundles[j].bounds.X = bundleLocationFromNumber.X;
			bundles[j].bounds.Y = bundleLocationFromNumber.Y;
			bundles[j].sprite.position = new Vector2(bundleLocationFromNumber.X, bundleLocationFromNumber.Y);
		}
		if (!specificBundlePage)
		{
			return;
		}
		int numberOfIngredientSlots = currentPageBundle.numberOfIngredientSlots;
		List<Rectangle> list = new List<Rectangle>();
		addRectangleRowsToList(list, numberOfIngredientSlots, 932, 540);
		ingredientSlots.Clear();
		for (int k = 0; k < list.Count; k++)
		{
			ingredientSlots.Add(new ClickableTextureComponent(list[k], noteTexture, new Rectangle(512, 244, 18, 18), 4f));
		}
		List<Rectangle> list2 = new List<Rectangle>();
		ingredientList.Clear();
		addRectangleRowsToList(list2, currentPageBundle.ingredients.Count, 932, 364);
		for (int l = 0; l < list2.Count; l++)
		{
			BundleIngredientDescription bundleIngredientDescription = currentPageBundle.ingredients[l];
			ItemMetadata metadata = ItemRegistry.GetMetadata(bundleIngredientDescription.id);
			if (metadata?.TypeIdentifier == "(O)")
			{
				ParsedItemData parsedOrErrorData = metadata.GetParsedOrErrorData();
				Texture2D texture = parsedOrErrorData.GetTexture();
				Rectangle sourceRect = parsedOrErrorData.GetSourceRect();
				Item item = ((bundleIngredientDescription.preservesId != null) ? Utility.CreateFlavoredItem(bundleIngredientDescription.id, bundleIngredientDescription.preservesId, bundleIngredientDescription.quality, bundleIngredientDescription.stack) : ItemRegistry.Create(bundleIngredientDescription.id, bundleIngredientDescription.stack, bundleIngredientDescription.quality));
				ingredientList.Add(new ClickableTextureComponent("", list2[l], "", item.DisplayName, texture, sourceRect, 4f)
				{
					myID = l + 1000,
					item = item,
					upNeighborID = -99998,
					rightNeighborID = -99998,
					leftNeighborID = -99998,
					downNeighborID = -99998
				});
			}
		}
		updateIngredientSlots();
	}

	private void setUpBundleSpecificPage(Bundle b)
	{
		tempSprites.Clear();
		currentPageBundle = b;
		specificBundlePage = true;
		if (whichArea == 4)
		{
			if (!fromGameMenu)
			{
				purchaseButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 800, yPositionOnScreen + 504, 260, 72), noteTexture, new Rectangle(517, 286, 65, 20), 4f)
				{
					myID = 797,
					leftNeighborID = 103
				};
				if (Game1.options.SnappyMenus)
				{
					currentlySnappedComponent = purchaseButton;
					snapCursorToCurrentSnappedComponent();
				}
			}
			return;
		}
		int numberOfIngredientSlots = b.numberOfIngredientSlots;
		List<Rectangle> list = new List<Rectangle>();
		addRectangleRowsToList(list, numberOfIngredientSlots, 932, 540);
		for (int i = 0; i < list.Count; i++)
		{
			ingredientSlots.Add(new ClickableTextureComponent(list[i], noteTexture, new Rectangle(512, 244, 18, 18), 4f)
			{
				myID = i + 250,
				upNeighborID = -99998,
				rightNeighborID = -99998,
				leftNeighborID = -99998,
				downNeighborID = -99998
			});
		}
		List<Rectangle> list2 = new List<Rectangle>();
		addRectangleRowsToList(list2, b.ingredients.Count, 932, 364);
		for (int j = 0; j < list2.Count; j++)
		{
			BundleIngredientDescription ingredient = b.ingredients[j];
			string representativeItemId = GetRepresentativeItemId(ingredient);
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(representativeItemId);
			if (dataOrErrorItem.HasTypeObject())
			{
				string text = ingredient.category switch
				{
					-2 => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.569"), 
					-75 => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.570"), 
					-4 => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.571"), 
					-5 => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.572"), 
					-6 => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.573"), 
					_ => dataOrErrorItem.DisplayName, 
				};
				Item item;
				if (ingredient.preservesId != null)
				{
					item = Utility.CreateFlavoredItem(ingredient.id, ingredient.preservesId, ingredient.quality, ingredient.stack);
					text = item.DisplayName;
				}
				else
				{
					item = ItemRegistry.Create(representativeItemId, ingredient.stack, ingredient.quality);
				}
				Texture2D texture = dataOrErrorItem.GetTexture();
				Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
				ingredientList.Add(new ClickableTextureComponent("ingredient_list_slot", list2[j], "", text, texture, sourceRect, 4f)
				{
					myID = j + 1000,
					item = item,
					upNeighborID = -99998,
					rightNeighborID = -99998,
					leftNeighborID = -99998,
					downNeighborID = -99998
				});
			}
		}
		updateIngredientSlots();
		if (!Game1.options.SnappyMenus)
		{
			return;
		}
		populateClickableComponentList();
		if (inventory?.inventory != null)
		{
			for (int k = 0; k < inventory.inventory.Count; k++)
			{
				if (inventory.inventory[k] != null)
				{
					if (inventory.inventory[k].downNeighborID == 101)
					{
						inventory.inventory[k].downNeighborID = -1;
					}
					if (inventory.inventory[k].leftNeighborID == -1)
					{
						inventory.inventory[k].leftNeighborID = 103;
					}
					if (inventory.inventory[k].upNeighborID >= 1000)
					{
						inventory.inventory[k].upNeighborID = 103;
					}
				}
			}
		}
		currentlySnappedComponent = getComponentWithID(0);
		snapCursorToCurrentSnappedComponent();
	}

	public override bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
	{
		if (currentPartialIngredientDescriptionIndex >= 0)
		{
			if (ingredientSlots.Contains(b) && b.item != partialDonationItem)
			{
				return false;
			}
			if (ingredientList.Contains(b) && ingredientList.IndexOf(b as ClickableTextureComponent) != currentPartialIngredientDescriptionIndex)
			{
				return false;
			}
		}
		return (a.myID >= 5000 || a.myID == 101 || a.myID == 102) == (b.myID >= 5000 || b.myID == 101 || b.myID == 102);
	}

	private void addRectangleRowsToList(List<Rectangle> toAddTo, int numberOfItems, int centerX, int centerY)
	{
		switch (numberOfItems)
		{
		case 1:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY, 1, 72, 72, 12));
			break;
		case 2:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY, 2, 72, 72, 12));
			break;
		case 3:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY, 3, 72, 72, 12));
			break;
		case 4:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY, 4, 72, 72, 12));
			break;
		case 5:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY - 36, 3, 72, 72, 12));
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY + 40, 2, 72, 72, 12));
			break;
		case 6:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY - 36, 3, 72, 72, 12));
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY + 40, 3, 72, 72, 12));
			break;
		case 7:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY - 36, 4, 72, 72, 12));
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY + 40, 3, 72, 72, 12));
			break;
		case 8:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY - 36, 4, 72, 72, 12));
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY + 40, 4, 72, 72, 12));
			break;
		case 9:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY - 36, 5, 72, 72, 12));
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY + 40, 4, 72, 72, 12));
			break;
		case 10:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY - 36, 5, 72, 72, 12));
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY + 40, 5, 72, 72, 12));
			break;
		case 11:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY - 36, 6, 72, 72, 12));
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY + 40, 5, 72, 72, 12));
			break;
		case 12:
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY - 36, 6, 72, 72, 12));
			toAddTo.AddRange(createRowOfBoxesCenteredAt(xPositionOnScreen + centerX, yPositionOnScreen + centerY + 40, 6, 72, 72, 12));
			break;
		}
	}

	private List<Rectangle> createRowOfBoxesCenteredAt(int xStart, int yStart, int numBoxes, int boxWidth, int boxHeight, int horizontalGap)
	{
		List<Rectangle> list = new List<Rectangle>();
		int num = xStart - numBoxes * (boxWidth + horizontalGap) / 2;
		int y = yStart - boxHeight / 2;
		for (int i = 0; i < numBoxes; i++)
		{
			list.Add(new Rectangle(num + i * (boxWidth + horizontalGap), y, boxWidth, boxHeight));
		}
		return list;
	}

	public void takeDownBundleSpecificPage()
	{
		if (!isReadyToCloseMenuOrBundle())
		{
			return;
		}
		ReturnPartialDonations(to_hand: false);
		hoveredItem = null;
		if (!specificBundlePage)
		{
			return;
		}
		specificBundlePage = false;
		ingredientSlots.Clear();
		ingredientList.Clear();
		tempSprites.Clear();
		purchaseButton = null;
		if (Game1.options.SnappyMenus)
		{
			if (currentPageBundle != null)
			{
				currentlySnappedComponent = currentPageBundle;
				snapCursorToCurrentSnappedComponent();
			}
			else
			{
				snapToDefaultClickableComponent();
			}
		}
	}

	private Point getBundleLocationFromNumber(int whichBundle)
	{
		Point result = new Point(xPositionOnScreen, yPositionOnScreen);
		switch (whichBundle)
		{
		case 0:
			result.X += 592;
			result.Y += 136;
			break;
		case 1:
			result.X += 392;
			result.Y += 384;
			break;
		case 2:
			result.X += 784;
			result.Y += 388;
			break;
		case 5:
			result.X += 588;
			result.Y += 276;
			break;
		case 6:
			result.X += 588;
			result.Y += 380;
			break;
		case 3:
			result.X += 304;
			result.Y += 252;
			break;
		case 4:
			result.X += 892;
			result.Y += 252;
			break;
		case 7:
			result.X += 440;
			result.Y += 164;
			break;
		case 8:
			result.X += 776;
			result.Y += 164;
			break;
		}
		return result;
	}
}
