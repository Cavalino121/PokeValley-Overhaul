using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.GameData.Shops;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TokenizableStrings;
using StardewValley.Triggers;

namespace StardewValley.Menus;

public class ShopMenu : IClickableMenu
{
	/// <summary>A callback to invoke when the player purchases an item.</summary>
	/// <param name="salable">The shop entry that was purchased. The entry's <see cref="P:StardewValley.Item.Stack" /> represents the number purchased per click, *not* the number bought (which is <paramref name="countTaken" />) or remaining stock (which is <paramref name="stock" />).</param>
	/// <param name="who">The player who purchased the item.</param>
	/// <param name="countTaken">The stack size that was taken out of the shop.</param>
	/// <param name="stock">The remaining stock in the shop. The <paramref name="countTaken" /> is already deducted from this value.</param>
	/// <returns>Returns whether to immediately exit the menu.</returns>
	public delegate bool OnPurchaseDelegate(ISalable salable, Farmer who, int countTaken, ItemStockInformation stock);

	/// <summary>A cached visual theme for the <see cref="T:StardewValley.Menus.ShopMenu" />.</summary>
	public class ShopCachedTheme
	{
		/// <summary>The visual theme data from <c>Data/Shops</c>, if applicable.</summary>
		public ShopThemeData ThemeData { get; }

		/// <summary>The texture for the shop window border.</summary>
		public Texture2D WindowBorderTexture { get; }

		/// <summary>The pixel area within the <see cref="P:StardewValley.Menus.ShopMenu.ShopCachedTheme.WindowBorderSourceRect" /> for the shop window border. This should be an 18x18 pixel area.</summary>
		public Rectangle WindowBorderSourceRect { get; }

		/// <summary>The texture for the NPC portrait background.</summary>
		public Texture2D PortraitBackgroundTexture { get; }

		/// <summary>The pixel area within the <see cref="P:StardewValley.Menus.ShopMenu.ShopCachedTheme.PortraitBackgroundTexture" /> for the NPC portrait background. This should be a 74x47 pixel area.</summary>
		public Rectangle PortraitBackgroundSourceRect { get; }

		/// <summary>The texture for the NPC dialogue background.</summary>
		public Texture2D DialogueBackgroundTexture { get; }

		/// <summary>The pixel area within the <see cref="P:StardewValley.Menus.ShopMenu.ShopCachedTheme.DialogueBackgroundTexture" /> for the NPC dialogue background. This should be a 60x60 pixel area.</summary>
		public Rectangle DialogueBackgroundSourceRect { get; }

		/// <summary>The sprite text color for the dialogue text, or <c>null</c> for the default color.</summary>
		public Color? DialogueColor { get; }

		/// <summary>The sprite text shadow color for the dialogue text, or <c>null</c> for the default color.</summary>
		public Color? DialogueShadowColor { get; }

		/// <summary>The texture for the item row background.</summary>
		public Texture2D ItemRowBackgroundTexture { get; }

		/// <summary>The pixel area within the <see cref="P:StardewValley.Menus.ShopMenu.ShopCachedTheme.ItemRowBackgroundTexture" /> for the item row background. This should be a 15x15 pixel area.</summary>
		public Rectangle ItemRowBackgroundSourceRect { get; }

		/// <summary>The color tint to apply to the item row background when the cursor is hovering over it</summary>
		public Color ItemRowBackgroundHoverColor { get; }

		/// <summary>The sprite text color for the item text, or <c>null</c> for the default color.</summary>
		public Color? ItemRowTextColor { get; }

		/// <summary>The texture for the box behind the item icons.</summary>
		public Texture2D ItemIconBackgroundTexture { get; }

		/// <summary>The pixel area within the <see cref="P:StardewValley.Menus.ShopMenu.ShopCachedTheme.ItemIconBackgroundTexture" /> for the item icon background. This should be an 18x18 pixel area.</summary>
		public Rectangle ItemIconBackgroundSourceRect { get; }

		/// <summary>The texture for the scroll up icon.</summary>
		public Texture2D ScrollUpTexture { get; }

		/// <summary>The pixel area within the <see cref="P:StardewValley.Menus.ShopMenu.ShopCachedTheme.ScrollUpTexture" /> for the scroll up icon. This should be an 11x12 pixel area.</summary>
		public Rectangle ScrollUpSourceRect { get; }

		/// <summary>The texture for the scroll down icon.</summary>
		public Texture2D ScrollDownTexture { get; }

		/// <summary>The pixel area within the <see cref="P:StardewValley.Menus.ShopMenu.ShopCachedTheme.ScrollDownTexture" /> for the scroll down icon. This should be an 11x12 pixel area.</summary>
		public Rectangle ScrollDownSourceRect { get; }

		/// <summary>The texture for the scrollbar foreground texture.</summary>
		public Texture2D ScrollBarFrontTexture { get; }

		/// <summary>The pixel area within the <see cref="P:StardewValley.Menus.ShopMenu.ShopCachedTheme.ScrollBarFrontTexture" /> for the scroll foreground. This should be a 6x10 pixel area.</summary>
		public Rectangle ScrollBarFrontSourceRect { get; }

		/// <summary>The texture for the scrollbar background texture.</summary>
		public Texture2D ScrollBarBackTexture { get; }

		/// <summary>The pixel area within the <see cref="P:StardewValley.Menus.ShopMenu.ShopCachedTheme.ScrollBarBackTexture" /> for the scroll background. This should be a 6x6 pixel area.</summary>
		public Rectangle ScrollBarBackSourceRect { get; }

		/// <summary>Construct an instance.</summary>
		/// <param name="theme">The visual theme data, or <c>null</c> for the default shop theme.</param>
		public ShopCachedTheme(ShopThemeData theme)
		{
			ThemeData = theme;
			WindowBorderTexture = LoadThemeTexture(theme?.WindowBorderTexture, Game1.mouseCursors);
			WindowBorderSourceRect = theme?.WindowBorderSourceRect ?? new Rectangle(384, 373, 18, 18);
			PortraitBackgroundTexture = LoadThemeTexture(theme?.PortraitBackgroundTexture, Game1.mouseCursors);
			PortraitBackgroundSourceRect = theme?.PortraitBackgroundSourceRect ?? new Rectangle(603, 414, 74, 74);
			DialogueBackgroundTexture = LoadThemeTexture(theme?.DialogueBackgroundTexture, Game1.menuTexture);
			DialogueBackgroundSourceRect = theme?.DialogueBackgroundSourceRect ?? new Rectangle(0, 256, 60, 60);
			DialogueColor = Utility.StringToColor(theme?.DialogueColor);
			DialogueShadowColor = Utility.StringToColor(theme?.DialogueShadowColor);
			ItemRowBackgroundTexture = LoadThemeTexture(theme?.ItemRowBackgroundTexture, Game1.mouseCursors);
			ItemRowBackgroundSourceRect = theme?.ItemRowBackgroundSourceRect ?? new Rectangle(384, 396, 15, 15);
			ItemRowBackgroundHoverColor = Utility.StringToColor(theme?.ItemRowBackgroundHoverColor) ?? Color.Wheat;
			ItemRowTextColor = Utility.StringToColor(theme?.ItemRowTextColor);
			ItemIconBackgroundTexture = LoadThemeTexture(theme?.ItemIconBackgroundTexture, Game1.mouseCursors);
			ItemIconBackgroundSourceRect = theme?.ItemIconBackgroundSourceRect ?? new Rectangle(296, 363, 18, 18);
			ScrollUpTexture = LoadThemeTexture(theme?.ScrollUpTexture, Game1.mouseCursors);
			ScrollUpSourceRect = theme?.ScrollUpSourceRect ?? new Rectangle(421, 459, 11, 12);
			ScrollDownTexture = LoadThemeTexture(theme?.ScrollDownTexture, Game1.mouseCursors);
			ScrollDownSourceRect = theme?.ScrollDownSourceRect ?? new Rectangle(421, 472, 11, 12);
			ScrollBarFrontTexture = LoadThemeTexture(theme?.ScrollBarFrontTexture, Game1.mouseCursors);
			ScrollBarFrontSourceRect = theme?.ScrollBarFrontSourceRect ?? new Rectangle(435, 463, 6, 10);
			ScrollBarBackTexture = LoadThemeTexture(theme?.ScrollBarBackTexture, Game1.mouseCursors);
			ScrollBarBackSourceRect = theme?.ScrollBarBackSourceRect ?? new Rectangle(403, 383, 6, 6);
		}

		/// <summary>Load a theme texture if it's non-null and exists, else get the default texture.</summary>
		/// <param name="customTextureName">The custom texture asset name to load.</param>
		/// <param name="defaultTexture">The default texture.</param>
		private Texture2D LoadThemeTexture(string customTextureName, Texture2D defaultTexture)
		{
			if (customTextureName == null || !Game1.content.DoesAssetExist<Texture2D>(customTextureName))
			{
				return defaultTexture;
			}
			return Game1.content.Load<Texture2D>(customTextureName);
		}
	}

	/// <summary>A clickable component representing a shop tab, which applies a filter to the list of displayed shop items when clicked.</summary>
	public class ShopTabClickableTextureComponent : ClickableTextureComponent
	{
		/// <summary>Matches items to show when this tab is selected.</summary>
		public Func<ISalable, bool> Filter;

		public ShopTabClickableTextureComponent(string name, Rectangle bounds, string label, string hoverText, Texture2D texture, Rectangle sourceRect, float scale, bool drawShadow = false)
			: base(name, bounds, label, hoverText, texture, sourceRect, scale, drawShadow)
		{
		}

		public ShopTabClickableTextureComponent(Rectangle bounds, Texture2D texture, Rectangle sourceRect, float scale, bool drawShadow = false)
			: base(bounds, texture, sourceRect, scale, drawShadow)
		{
		}
	}

	public const int region_shopButtonModifier = 3546;

	public const int region_upArrow = 97865;

	public const int region_downArrow = 97866;

	public const int region_tabStartIndex = 99999;

	public const int infiniteStock = int.MaxValue;

	public const int itemsPerPage = 4;

	public const int numberRequiredForExtraItemTrade = 5;

	public string hoverText = "";

	public string boldTitleText = "";

	/// <summary>The sound played when the shop menu is opened.</summary>
	public string openMenuSound = "dwop";

	/// <summary>The sound played when an item is purchased normally.</summary>
	public string purchaseSound = "purchaseClick";

	/// <summary>The repeating sound played when accumulating a stack to purchase (e.g. by holding right-click on PC).</summary>
	public string purchaseRepeatSound = "purchaseRepeat";

	/// <summary>A key which identifies the current shop. This may be the unique shop ID in <c>Data/Shops</c> for a standard shop, <c>Dresser</c> or <c>FishTank</c> for furniture, etc.</summary>
	public string ShopId;

	/// <summary>The underlying shop data, if this is a standard shop from <c>Data/Shops</c>.</summary>
	public ShopData ShopData;

	public InventoryMenu inventory;

	public ISalable heldItem;

	public ISalable hoveredItem;

	/// <summary>How to draw stack size numbers in the shop list by default. If set, this overrides <see cref="F:StardewValley.GameData.Shops.ShopData.StackSizeVisibility" />.</summary>
	public StackDrawType? DefaultStackDrawType;

	private TemporaryAnimatedSprite poof;

	private Rectangle scrollBarRunner;

	/// <summary>The items sold in the shop.</summary>
	public List<ISalable> forSale = new List<ISalable>();

	public List<ClickableComponent> forSaleButtons = new List<ClickableComponent>();

	public List<int> categoriesToSellHere = new List<int>();

	public List<List<string>> tagsToSellHere = new List<List<string>>();

	/// <summary>The stock info for each item in <see cref="F:StardewValley.Menus.ShopMenu.forSale" />.</summary>
	public Dictionary<ISalable, ItemStockInformation> itemPriceAndStock = new Dictionary<ISalable, ItemStockInformation>();

	private float sellPercentage = 1f;

	private TemporaryAnimatedSpriteList animations = new TemporaryAnimatedSpriteList();

	public int hoverPrice = -1;

	public int currentItemIndex;

	/// <summary>The currency in which all items in the shop should be priced. The valid values are 0 (money), 1 (star tokens), 2 (Qi coins), and 4 (Qi gems).</summary>
	public int currency;

	public ClickableTextureComponent upArrow;

	public ClickableTextureComponent downArrow;

	public ClickableTextureComponent scrollBar;

	public Texture2D portraitTexture;

	public string potraitPersonDialogue;

	public object source;

	private bool scrolling;

	/// <summary>A callback to invoke when the player purchases an item, if any.</summary>
	public OnPurchaseDelegate onPurchase;

	/// <summary>A callback to invoke when the player sells an item, if any.</summary>
	public Func<ISalable, bool> onSell;

	public Func<int, bool> canPurchaseCheck;

	public List<ShopTabClickableTextureComponent> tabButtons = new List<ShopTabClickableTextureComponent>();

	protected int currentTab;

	protected bool _isStorageShop;

	public bool readOnly;

	public HashSet<ISalable> buyBackItems = new HashSet<ISalable>();

	public Dictionary<ISalable, ISalable> buyBackItemsToResellTomorrow = new Dictionary<ISalable, ISalable>();

	/// <summary>The number of milliseconds until the menu will allow buying or selling items, to help avoid doing so accidentally.</summary>
	public int safetyTimer = 250;

	/// <summary>The visual theme applied to the shop UI.</summary>
	/// <remarks>This can be set via <see cref="M:StardewValley.Menus.ShopMenu.SetVisualTheme(StardewValley.GameData.Shops.ShopThemeData)" />.</remarks>
	public ShopCachedTheme VisualTheme { get; private set; }

	/// <summary>Construct an instance.</summary>
	/// <param name="shopId">The unique shop ID in <c>Data\Shops</c>.</param>
	/// <param name="shopData">The shop data from <c>Data/Shops</c>.</param>
	/// <param name="ownerData">The owner entry for the shop portrait and dialogue, or <c>null</c> to disable those.</param>
	/// <param name="owner">The NPC matching <paramref name="ownerData" /> whose portrait to show, if applicable.</param>
	/// <param name="onPurchase">A callback to invoke when the player purchases an item, if any.</param>
	/// <param name="onSell">A callback to invoke when the player sells an item, if any.</param>
	/// <param name="playOpenSound">Whether to play the open-menu sound.</param>
	public ShopMenu(string shopId, ShopData shopData, ShopOwnerData ownerData, NPC owner = null, OnPurchaseDelegate onPurchase = null, Func<ISalable, bool> onSell = null, bool playOpenSound = true)
	{
		ShopId = shopId ?? throw new ArgumentNullException("shopId");
		foreach (KeyValuePair<ISalable, ItemStockInformation> item in ShopBuilder.GetShopStock(shopId, shopData))
		{
			AddForSale(item.Key, item.Value);
		}
		ShopData = shopData;
		if (shopData.SalableItemTags != null)
		{
			foreach (string salableItemTag in shopData.SalableItemTags)
			{
				List<string> list = new List<string>();
				string[] array = salableItemTag.Split(',');
				foreach (string text in array)
				{
					list.Add(text.Trim());
				}
				tagsToSellHere.Add(list);
			}
		}
		openMenuSound = shopData.OpenSound ?? openMenuSound;
		purchaseSound = shopData.PurchaseSound ?? purchaseSound;
		purchaseRepeatSound = shopData.PurchaseRepeatSound ?? purchaseRepeatSound;
		SetVisualTheme(shopData.VisualTheme?.FirstOrDefault((ShopThemeData theme) => GameStateQuery.CheckConditions(theme.Condition)));
		SetUpShopOwner(ownerData, owner);
		Initialize(shopData.Currency, onPurchase, onSell, playOpenSound);
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="shopId">A key which identifies the current shop.</param>
	/// <param name="itemPriceAndStock">The items to sell in the shop.</param>
	/// <param name="currency">The currency in which all items in the shop should be priced. The valid values are 0 (money), 1 (star tokens), 2 (Qi coins), and 4 (Qi gems).</param>
	/// <param name="who">The internal name for the NPC running the shop, if any.</param>
	/// <param name="on_purchase">A callback to invoke when the player purchases an item, if any.</param>
	/// <param name="on_sell">A callback to invoke when the player sells an item, if any.</param>
	/// <param name="playOpenSound">Whether to play the open-menu sound.</param>
	public ShopMenu(string shopId, Dictionary<ISalable, ItemStockInformation> itemPriceAndStock, int currency = 0, string who = null, OnPurchaseDelegate on_purchase = null, Func<ISalable, bool> on_sell = null, bool playOpenSound = true)
	{
		ShopId = shopId ?? throw new ArgumentNullException("shopId");
		foreach (KeyValuePair<ISalable, ItemStockInformation> item in itemPriceAndStock)
		{
			AddForSale(item.Key, item.Value);
		}
		SetVisualTheme(null);
		setUpShopOwner(who, shopId);
		Initialize(currency, on_purchase, on_sell, playOpenSound);
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="shopId">A key which identifies the current shop.</param>
	/// <param name="itemsForSale">The items to sell in the shop.</param>
	/// <param name="currency">The currency in which all items in the shop should be priced. The valid values are 0 (money), 1 (star tokens), 2 (Qi coins), and 4 (Qi gems).</param>
	/// <param name="who">The internal name for the NPC running the shop, if any.</param>
	/// <param name="on_purchase">A callback to invoke when the player purchases an item, if any.</param>
	/// <param name="on_sell">A callback to invoke when the player sells an item, if any.</param>
	/// <param name="playOpenSound">Whether to play the open-menu sound.</param>
	public ShopMenu(string shopId, List<ISalable> itemsForSale, int currency = 0, string who = null, OnPurchaseDelegate on_purchase = null, Func<ISalable, bool> on_sell = null, bool playOpenSound = true)
		: base(Game1.uiViewport.Width / 2 - (800 + IClickableMenu.borderWidth * 2) / 2, Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 1000 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, showUpperRightCloseButton: true)
	{
		ShopId = shopId ?? throw new ArgumentNullException("shopId");
		foreach (ISalable item in itemsForSale)
		{
			AddForSale(item);
		}
		SetVisualTheme(null);
		setUpShopOwner(who, shopId);
		Initialize(currency, on_purchase, on_sell, playOpenSound);
	}

	/// <summary>Set the visual theme for the shop menu.</summary>
	/// <param name="theme">The visual theme to display, or <c>null</c> for the default theme.</param>
	/// <remarks>The visual theme is usually set in <c>Data/Shops</c> instead of calling this method directly.</remarks>
	public void SetVisualTheme(ShopThemeData theme)
	{
		VisualTheme = new ShopCachedTheme(theme);
		if (upArrow != null)
		{
			Rectangle rectangle = new Rectangle(Game1.uiViewport.X, Game1.uiViewport.Y, Game1.uiViewport.Width, Game1.uiViewport.Height);
			gameWindowSizeChanged(rectangle, rectangle);
		}
	}

	/// <summary>Initialize the shop menu after the stock has been constructed.</summary>
	/// <param name="currency">The currency in which all items in the shop should be priced. The valid values are 0 (money), 1 (star tokens), 2 (Qi coins), and 4 (Qi gems).</param>
	/// <param name="onPurchase">A callback to invoke when the player purchases an item, if any.</param>
	/// <param name="onSell">A callback to invoke when the player sells an item, if any.</param>
	/// <param name="playOpenSound">Whether to play the open-menu sound.</param>
	private void Initialize(int currency, OnPurchaseDelegate onPurchase, Func<ISalable, bool> onSell, bool playOpenSound)
	{
		ShopCachedTheme visualTheme = VisualTheme;
		updatePosition();
		upperRightCloseButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 36, yPositionOnScreen - 8, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);
		this.currency = currency;
		this.onPurchase = onPurchase;
		this.onSell = onSell;
		Game1.player.forceCanMove();
		if (playOpenSound)
		{
			PlayOpenSound();
		}
		inventory = new InventoryMenu(xPositionOnScreen + width, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + 320 + 40, playerInventory: false, null, highlightItemToSell)
		{
			showGrayedOutSlots = true
		};
		inventory.movePosition(-inventory.width - 32, 0);
		upArrow = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + 16, 44, 48), visualTheme.ScrollUpTexture, visualTheme.ScrollUpSourceRect, 4f)
		{
			myID = 97865,
			downNeighborID = 106,
			leftNeighborID = 3546
		};
		downArrow = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 64, 44, 48), visualTheme.ScrollDownTexture, visualTheme.ScrollDownSourceRect, 4f)
		{
			myID = 106,
			upNeighborID = 97865,
			leftNeighborID = 3546
		};
		scrollBar = new ClickableTextureComponent(new Rectangle(upArrow.bounds.X + 12, upArrow.bounds.Y + upArrow.bounds.Height + 4, 24, 40), visualTheme.ScrollBarFrontTexture, visualTheme.ScrollBarFrontSourceRect, 4f);
		scrollBarRunner = new Rectangle(scrollBar.bounds.X, upArrow.bounds.Y + upArrow.bounds.Height + 4, scrollBar.bounds.Width, height - 64 - upArrow.bounds.Height - 28);
		for (int i = 0; i < 4; i++)
		{
			forSaleButtons.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 16 + i * ((height - 256) / 4), width - 32, (height - 256) / 4 + 4), i.ToString() ?? "")
			{
				myID = i + 3546,
				rightNeighborID = 97865,
				fullyImmutable = true
			});
		}
		updateSaleButtonNeighbors();
		setUpStoreForContext();
		if (tabButtons.Count > 0)
		{
			foreach (ClickableComponent forSaleButton in forSaleButtons)
			{
				forSaleButton.leftNeighborID = -99998;
			}
		}
		applyTab();
		foreach (ClickableComponent item in inventory.GetBorder(InventoryMenu.BorderSide.Top))
		{
			item.upNeighborID = -99998;
		}
		if (Game1.options.snappyMenus && Game1.options.gamepadControls)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
		if (currency == 4)
		{
			int tickOpened = Game1.ticks;
			Game1.specialCurrencyDisplay.ShowCurrency("qiGems", () => Game1.ticks == tickOpened || (Game1.activeClickableMenu == this && this.currency == 4), 0f);
		}
	}

	/// <summary>Add an item to sell in the menu.</summary>
	/// <param name="item">The item instance to sell.</param>
	/// <param name="stock">The stock information, or <c>null</c> to create it automatically.</param>
	public void AddForSale(ISalable item, ItemStockInformation stock = null)
	{
		if (item.IsRecipe)
		{
			if (Game1.player.knowsRecipe(item.Name))
			{
				return;
			}
			item.Stack = 1;
		}
		forSale.Add(item);
		itemPriceAndStock.Add(item, stock ?? new ItemStockInformation(item.salePrice(), item.Stack));
	}

	public void updateSaleButtonNeighbors()
	{
		ClickableComponent clickableComponent = forSaleButtons[0];
		for (int i = 0; i < forSaleButtons.Count; i++)
		{
			ClickableComponent clickableComponent2 = forSaleButtons[i];
			clickableComponent2.upNeighborImmutable = true;
			clickableComponent2.downNeighborImmutable = true;
			clickableComponent2.upNeighborID = ((i > 0) ? (i + 3546 - 1) : (-7777));
			clickableComponent2.downNeighborID = ((i < 3 && i < forSale.Count - 1) ? (i + 3546 + 1) : (-7777));
			if (i >= forSale.Count)
			{
				if (clickableComponent2 == currentlySnappedComponent)
				{
					currentlySnappedComponent = clickableComponent;
					if (Game1.options.SnappyMenus)
					{
						snapCursorToCurrentSnappedComponent();
					}
				}
			}
			else
			{
				clickableComponent = clickableComponent2;
			}
		}
	}

	public virtual void setUpStoreForContext()
	{
		tabButtons = null;
		switch (ShopId)
		{
		case "JunimoFurnitureCatalogue":
		case "WizardFurnitureCatalogue":
		case "RetroFurnitureCatalogue":
		case "TrashFurnitureCatalogue":
		case "Furniture Catalogue":
		case "JojaFurnitureCatalogue":
			UseFurnitureCatalogueTabs();
			break;
		case "Catalogue":
			UseCatalogueTabs();
			break;
		case "ReturnedDonations":
			UseNoTabs();
			_isStorageShop = true;
			break;
		case "FishTank":
			UseNoTabs();
			_isStorageShop = true;
			break;
		case "Dresser":
			categoriesToSellHere.AddRange(new int[4] { -95, -100, -97, -96 });
			UseDresserTabs();
			_isStorageShop = true;
			break;
		default:
			UseNoTabs();
			break;
		}
		if (_isStorageShop)
		{
			purchaseSound = null;
			purchaseRepeatSound = null;
		}
		if (ShopId == "Dwarf" || ShopId == "ShadowShop" || ShopId == "Traveler")
		{
			InjectPokemonHeldItems();
		}
	}

	private void InjectPokemonHeldItems()
	{
		if (ShopId == "Dwarf")
		{
			string[] dwarfItems = new[] { "Poke_HardStone", "Poke_RockyHelmet", "Poke_AssaultVest", "Poke_Eviolite", "Poke_FocusSash", "Poke_Magnet" };
			int[] dwarfPrices = new[] { 2000, 5000, 7500, 8000, 4000, 2500 };
			for (int i = 0; i < dwarfItems.Length; i++)
			{
				string id = dwarfItems[i];
				Item item = ItemRegistry.Create("(O)" + id, 1, 0, allowNull: true) ?? new StardewValley.Object(id, 1);
				if (item != null && !forSale.Any(s => s is Item it && it.QualifiedItemId == item.QualifiedItemId))
				{
					AddForSale(item, new ItemStockInformation(dwarfPrices[i], int.MaxValue));
				}
			}
		}
		else if (ShopId == "ShadowShop")
		{
			string[] krobusItems = new[] { "Poke_BlackGlasses", "Poke_SpellTag", "Poke_BlackSludge", "Poke_Leftovers", "Poke_LifeOrb", "Poke_ChoiceBand", "Poke_ChoiceSpecs" };
			int[] krobusPrices = new[] { 3000, 3000, 4500, 6000, 8000, 9000, 9000 };
			for (int i = 0; i < krobusItems.Length; i++)
			{
				string id = krobusItems[i];
				Item item = ItemRegistry.Create("(O)" + id, 1, 0, allowNull: true) ?? new StardewValley.Object(id, 1);
				if (item != null && !forSale.Any(s => s is Item it && it.QualifiedItemId == item.QualifiedItemId))
				{
					AddForSale(item, new ItemStockInformation(krobusPrices[i], int.MaxValue));
				}
			}
		}
		else if (ShopId == "Traveler")
		{
			Random rand = Utility.CreateDaySaveRandom(Game1.stats.DaysPlayed, 88877);
			string[] pool = new[]
			{
				"Poke_Leftovers", "Poke_LifeOrb", "Poke_ChoiceBand", "Poke_ChoiceSpecs", "Poke_ChoiceScarf",
				"Poke_FocusSash", "Poke_AssaultVest", "Poke_Eviolite", "Poke_RockyHelmet", "Poke_HeavyDutyBoots",
				"Poke_ExpertBelt", "Poke_MuscleBand", "Poke_WiseGlasses", "Poke_ScopeLens", "Poke_Charcoal",
				"Poke_MysticWater", "Poke_MiracleSeed", "Poke_Magnet", "Poke_SharpBeak", "Poke_PoisonBarb"
			};
			int count = rand.Next(2, 4);
			for (int i = 0; i < count; i++)
			{
				string id = pool[rand.Next(pool.Length)];
				Item item = ItemRegistry.Create("(O)" + id, 1, 0, allowNull: true) ?? new StardewValley.Object(id, 1);
				if (item != null && !forSale.Any(s => s is Item it && it.QualifiedItemId == item.QualifiedItemId))
				{
					int price = rand.Next(4, 12) * 1000;
					AddForSale(item, new ItemStockInformation(price, 1));
				}
			}
		}
	}

	/// <summary>Remove the filter tabs, if any.</summary>
	public void UseNoTabs()
	{
		tabButtons = new List<ShopTabClickableTextureComponent>();
		repositionTabs();
	}

	/// <summary>Add the filter tabs for a furniture catalogue (e.g. tables, seats, paintings, etc).</summary>
	public void UseFurnitureCatalogueTabs()
	{
		tabButtons = new List<ShopTabClickableTextureComponent>
		{
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(96, 48, 16, 16), 4f)
			{
				myID = 99999,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable _) => true
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(80, 48, 16, 16), 4f)
			{
				myID = 100000,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable item) => item is Furniture furniture && (furniture.IsTable() || furniture.furniture_type.Value == 4)
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(64, 48, 16, 16), 4f)
			{
				myID = 100001,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable item) => item is Furniture furniture && (furniture.furniture_type.Value == 0 || furniture.furniture_type.Value == 1 || furniture.furniture_type.Value == 2 || furniture.furniture_type.Value == 3)
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(64, 64, 16, 16), 4f)
			{
				myID = 100002,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable item) => item is Furniture furniture && (furniture.furniture_type.Value == 6 || furniture.furniture_type.Value == 13)
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(96, 64, 16, 16), 4f)
			{
				myID = 100003,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable item) => item is Furniture furniture && furniture.furniture_type.Value == 12
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(80, 64, 16, 16), 4f)
			{
				myID = 100004,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable item) => item is Furniture furniture && (furniture.furniture_type.Value == 7 || furniture.furniture_type.Value == 17 || furniture.furniture_type.Value == 10 || furniture.furniture_type.Value == 8 || furniture.furniture_type.Value == 9 || furniture.furniture_type.Value == 14)
			}
		};
		repositionTabs();
	}

	/// <summary>Add the filter tabs for a catalogue (e.g. flooring and wallpaper).</summary>
	public void UseCatalogueTabs()
	{
		tabButtons = new List<ShopTabClickableTextureComponent>
		{
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(96, 48, 16, 16), 4f)
			{
				myID = 99999,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable item) => true
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(48, 64, 16, 16), 4f)
			{
				myID = 100000,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable item) => item is Wallpaper wallpaper && wallpaper.isFloor.Value
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(32, 64, 16, 16), 4f)
			{
				myID = 100001,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable item) => item is Wallpaper wallpaper && !wallpaper.isFloor.Value
			}
		};
		repositionTabs();
	}

	/// <summary>Add the filter tabs for a dresser (e.g. hats, shirts, pants, etc).</summary>
	public void UseDresserTabs()
	{
		tabButtons = new List<ShopTabClickableTextureComponent>
		{
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(0, 48, 16, 16), 4f)
			{
				myID = 99999,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable item) => true
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(16, 48, 16, 16), 4f)
			{
				myID = 100000,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable salable) => salable is Item item && item.Category == -95
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(32, 48, 16, 16), 4f)
			{
				myID = 100001,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable salable) => salable is Clothing clothing && clothing.clothesType.Value == Clothing.ClothesType.SHIRT
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(48, 48, 16, 16), 4f)
			{
				myID = 100002,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable salable) => salable is Clothing clothing && clothing.clothesType.Value == Clothing.ClothesType.PANTS
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(0, 64, 16, 16), 4f)
			{
				myID = 100003,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable salable) => salable is Item item && item.Category == -97
			},
			new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(16, 64, 16, 16), 4f)
			{
				myID = 100004,
				upNeighborID = -99998,
				downNeighborID = -99998,
				rightNeighborID = 3546,
				Filter = (ISalable salable) => salable is Item item && item.Category == -96
			}
		};
		repositionTabs();
	}

	public void repositionTabs()
	{
		for (int i = 0; i < tabButtons.Count; i++)
		{
			if (i == currentTab)
			{
				tabButtons[i].bounds.X = xPositionOnScreen - 56;
			}
			else
			{
				tabButtons[i].bounds.X = xPositionOnScreen - 64;
			}
			tabButtons[i].bounds.Y = yPositionOnScreen + i * 16 * 4 + 16;
		}
	}

	protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
	{
		switch (direction)
		{
		case 2:
		{
			if (currentItemIndex < Math.Max(0, forSale.Count - 4))
			{
				downArrowPressed();
				break;
			}
			int num = -1;
			for (int i = 0; i < 12; i++)
			{
				inventory.inventory[i].upNeighborID = oldID;
				if (num == -1 && heldItem != null)
				{
					IList<Item> actualInventory = inventory.actualInventory;
					if (actualInventory != null && actualInventory.Count > i && inventory.actualInventory[i] == null)
					{
						num = i;
					}
				}
			}
			currentlySnappedComponent = getComponentWithID((num != -1) ? num : 0);
			snapCursorToCurrentSnappedComponent();
			break;
		}
		case 0:
			if (currentItemIndex > 0)
			{
				upArrowPressed();
				currentlySnappedComponent = getComponentWithID(3546);
				snapCursorToCurrentSnappedComponent();
			}
			break;
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(3546);
		snapCursorToCurrentSnappedComponent();
	}

	public void setUpShopOwner(string who, string shopId)
	{
		if (!DataLoader.Shops(Game1.content).TryGetValue(shopId, out var value))
		{
			return;
		}
		foreach (ShopOwnerData currentOwner in ShopBuilder.GetCurrentOwners(value))
		{
			if (currentOwner.IsValid(who))
			{
				SetUpShopOwner(currentOwner);
				break;
			}
		}
	}

	/// <summary>Set the shop portrait and dialogue.</summary>
	/// <param name="ownerData">The owner entry in the shop data.</param>
	/// <param name="owner">The specific NPC which matches the <paramref name="ownerData" />, if set.</param>
	public void SetUpShopOwner(ShopOwnerData ownerData, NPC owner = null)
	{
		if (ownerData == null)
		{
			portraitTexture = null;
			potraitPersonDialogue = null;
			return;
		}
		string text = null;
		bool flag = false;
		if (ownerData.Dialogues != null)
		{
			Random random = (ownerData.RandomizeDialogueOnOpen ? Game1.random : Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed));
			foreach (ShopDialogueData dialogue in ownerData.Dialogues)
			{
				if (GameStateQuery.CheckConditions(dialogue.Condition))
				{
					string text2 = dialogue.Dialogue;
					List<string> randomDialogue = dialogue.RandomDialogue;
					if (randomDialogue != null && randomDialogue.Any())
					{
						text2 = random.ChooseFrom(dialogue.RandomDialogue);
					}
					text = TokenParser.ParseText(text2, random, ParseDialogueSubstitution);
					break;
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				flag = true;
			}
		}
		portraitTexture = TryLoadPortrait(ownerData, owner);
		if (!flag)
		{
			potraitPersonDialogue = Game1.parseText(text ?? Game1.content.LoadString("Strings\\StringsFromCSFiles:ShopMenu.cs.11457"), Game1.dialogueFont, 304);
		}
	}

	/// <summary>Get the portrait to show for the selected NPC, if any.</summary>
	/// <param name="ownerData">The shop owner data.</param>
	/// <param name="owner">The specific NPC which matches the <paramref name="ownerData" />, if set.</param>
	public Texture2D TryLoadPortrait(ShopOwnerData ownerData, NPC owner)
	{
		if (ownerData.Type == ShopOwnerType.None)
		{
			return null;
		}
		if (ownerData.Portrait != null)
		{
			if (!string.IsNullOrWhiteSpace(ownerData.Portrait))
			{
				if (Game1.content.DoesAssetExist<Texture2D>(ownerData.Portrait))
				{
					return Game1.content.Load<Texture2D>(ownerData.Portrait);
				}
				NPC characterFromName = Game1.getCharacterFromName(ownerData.Portrait);
				if (characterFromName?.Portrait != null)
				{
					return characterFromName.Portrait;
				}
			}
			return null;
		}
		if (owner?.Portrait != null)
		{
			return owner.Portrait;
		}
		if (ownerData.Type == ShopOwnerType.NamedNpc && !string.IsNullOrWhiteSpace(ownerData.Name))
		{
			NPC characterFromName2 = Game1.getCharacterFromName(ownerData.Name);
			if (characterFromName2?.Portrait != null)
			{
				return characterFromName2.Portrait;
			}
		}
		return null;
	}

	public bool ParseDialogueSubstitution(string[] query, out string replacement, Random random, Farmer player)
	{
		if (query[0] == "SuggestedItem")
		{
			string interval = ArgUtility.Get(query, 1, "day");
			string key = ArgUtility.Get(query, 2, ShopId);
			if (!Utility.TryCreateIntervalRandom(interval, key, out random, out var error))
			{
				Game1.log.Error($"Failed parsing [SuggestedItem {string.Join(" ", query)}] in dialogue shop '{ShopId}': {error}");
				random = Utility.CreateRandom(Game1.ticks);
			}
			if (Utility.TryGetRandom(itemPriceAndStock, out var key2, out var _, random))
			{
				replacement = key2.DisplayName;
				return true;
			}
		}
		replacement = null;
		return false;
	}

	public bool highlightItemToSell(Item i)
	{
		if (heldItem != null)
		{
			return heldItem.canStackWith(i);
		}
		if (categoriesToSellHere.Contains(i.Category))
		{
			return true;
		}
		foreach (List<string> item in tagsToSellHere)
		{
			bool flag = false;
			foreach (string item2 in item)
			{
				if (!i.HasContextTag(item2))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return true;
			}
		}
		return false;
	}

	public static int getPlayerCurrencyAmount(Farmer who, int currencyType)
	{
		return currencyType switch
		{
			0 => who.Money, 
			1 => who.festivalScore, 
			2 => who.clubCoins, 
			4 => who.QiGems, 
			_ => 0, 
		};
	}

	/// <inheritdoc />
	public override void leftClickHeld(int x, int y)
	{
		base.leftClickHeld(x, y);
		if (scrolling)
		{
			int y2 = scrollBar.bounds.Y;
			scrollBar.bounds.Y = Math.Min(yPositionOnScreen + height - 64 - 12 - scrollBar.bounds.Height, Math.Max(y, yPositionOnScreen + upArrow.bounds.Height + 20));
			float num = (float)(y - scrollBarRunner.Y) / (float)scrollBarRunner.Height;
			currentItemIndex = Math.Min(Math.Max(0, forSale.Count - 4), Math.Max(0, (int)((float)forSale.Count * num)));
			setScrollBarToCurrentIndex();
			updateSaleButtonNeighbors();
			if (y2 != scrollBar.bounds.Y)
			{
				Game1.playSound("shiny4");
			}
		}
	}

	/// <inheritdoc />
	public override void releaseLeftClick(int x, int y)
	{
		base.releaseLeftClick(x, y);
		scrolling = false;
	}

	private void setScrollBarToCurrentIndex()
	{
		if (forSale.Count > 0)
		{
			float num = (float)scrollBarRunner.Height / (float)Math.Max(1, forSale.Count - 4 + 1);
			scrollBar.bounds.Y = (int)(num * (float)currentItemIndex + (float)upArrow.bounds.Bottom + 4f);
			if (currentItemIndex == forSale.Count - 4)
			{
				scrollBar.bounds.Y = downArrow.bounds.Y - scrollBar.bounds.Height - 4;
			}
		}
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		base.receiveScrollWheelAction(direction);
		if (direction > 0 && currentItemIndex > 0)
		{
			upArrowPressed();
			Game1.playSound("shiny4");
		}
		else if (direction < 0 && currentItemIndex < Math.Max(0, forSale.Count - 4))
		{
			downArrowPressed();
			Game1.playSound("shiny4");
		}
	}

	private void downArrowPressed()
	{
		downArrow.scale = downArrow.baseScale;
		currentItemIndex++;
		setScrollBarToCurrentIndex();
		updateSaleButtonNeighbors();
	}

	private void upArrowPressed()
	{
		upArrow.scale = upArrow.baseScale;
		currentItemIndex--;
		setScrollBarToCurrentIndex();
		updateSaleButtonNeighbors();
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && heldItem is Item item)
		{
			heldItem = null;
			if (Utility.CollectOrDrop(item))
			{
				Game1.playSound("stoneStep");
			}
			else
			{
				Game1.playSound("throwDownITem");
			}
		}
		else
		{
			base.receiveKeyPress(key);
		}
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y);
		if (Game1.activeClickableMenu == null)
		{
			return;
		}
		Vector2 vector = inventory.snapToClickableComponent(x, y);
		if (downArrow.containsPoint(x, y) && currentItemIndex < Math.Max(0, forSale.Count - 4))
		{
			downArrowPressed();
			Game1.playSound("shwip");
		}
		else if (upArrow.containsPoint(x, y) && currentItemIndex > 0)
		{
			upArrowPressed();
			Game1.playSound("shwip");
		}
		else if (scrollBar.containsPoint(x, y))
		{
			scrolling = true;
		}
		else if (!downArrow.containsPoint(x, y) && x > xPositionOnScreen + width && x < xPositionOnScreen + width + 128 && y > yPositionOnScreen && y < yPositionOnScreen + height)
		{
			scrolling = true;
			leftClickHeld(x, y);
			releaseLeftClick(x, y);
		}
		for (int i = 0; i < tabButtons.Count; i++)
		{
			if (tabButtons[i].containsPoint(x, y))
			{
				switchTab(i);
			}
		}
		currentItemIndex = Math.Max(0, Math.Min(forSale.Count - 4, currentItemIndex));
		if (safetyTimer <= 0)
		{
			if (heldItem == null && !readOnly)
			{
				Item item = inventory.leftClick(x, y, null, playSound: false);
				if (item != null)
				{
					if (onSell != null)
					{
						onSell(item);
					}
					else
					{
						int num = (int)((float)item.sellToStorePrice(-1L) * sellPercentage);
						chargePlayer(Game1.player, currency, -num * item.Stack);
						int num2 = item.Stack / 8 + 2;
						for (int j = 0; j < num2; j++)
						{
							animations.Add(new TemporaryAnimatedSprite("TileSheets\\debris", new Rectangle(Game1.random.Next(2) * 16, 64, 16, 16), 9999f, 1, 999, vector + new Vector2(32f, 32f), flicker: false, flipped: false)
							{
								alphaFade = 0.025f,
								motion = new Vector2(Game1.random.Next(-3, 4), -4f),
								acceleration = new Vector2(0f, 0.5f),
								delayBeforeAnimationStart = j * 25,
								scale = 2f
							});
							animations.Add(new TemporaryAnimatedSprite("TileSheets\\debris", new Rectangle(Game1.random.Next(2) * 16, 64, 16, 16), 9999f, 1, 999, vector + new Vector2(32f, 32f), flicker: false, flipped: false)
							{
								scale = 4f,
								alphaFade = 0.025f,
								delayBeforeAnimationStart = j * 50,
								motion = Utility.getVelocityTowardPoint(new Point((int)vector.X + 32, (int)vector.Y + 32), new Vector2(xPositionOnScreen - 36, yPositionOnScreen + height - inventory.height - 16), 8f),
								acceleration = Utility.getVelocityTowardPoint(new Point((int)vector.X + 32, (int)vector.Y + 32), new Vector2(xPositionOnScreen - 36, yPositionOnScreen + height - inventory.height - 16), 0.5f)
							});
						}
						ISalable salable = null;
						if (CanBuyback())
						{
							salable = AddBuybackItem(item, num, item.Stack);
						}
						if (item is Object obj && obj.edibility.Value != -300)
						{
							Item one = obj.getOne();
							one.Stack = obj.Stack;
							if (salable != null && buyBackItemsToResellTomorrow.TryGetValue(salable, out var value))
							{
								value.Stack += obj.Stack;
							}
							else if (Game1.currentLocation is ShopLocation shopLocation)
							{
								if (salable != null)
								{
									buyBackItemsToResellTomorrow[salable] = one;
								}
								shopLocation.itemsToStartSellingTomorrow.Add(one);
							}
						}
						Game1.playSound("sell");
						Game1.playSound("purchase");
						if (inventory.getItemAt(x, y) == null)
						{
							animations.Add(new TemporaryAnimatedSprite(5, vector + new Vector2(32f, 32f), Color.White)
							{
								motion = new Vector2(0f, -0.5f)
							});
						}
					}
					updateSaleButtonNeighbors();
				}
			}
			else
			{
				heldItem = inventory.leftClick(x, y, heldItem as Item);
			}
			for (int k = 0; k < forSaleButtons.Count; k++)
			{
				if (currentItemIndex + k >= forSale.Count || !forSaleButtons[k].containsPoint(x, y))
				{
					continue;
				}
				int num3 = currentItemIndex + k;
				if (forSale[num3] != null)
				{
					int val = ((!Game1.oldKBState.IsKeyDown(Keys.LeftShift)) ? 1 : Math.Min(Math.Min((!Game1.oldKBState.IsKeyDown(Keys.LeftControl)) ? 5 : (Game1.oldKBState.IsKeyDown(Keys.D1) ? 999 : 25), getPlayerCurrencyAmount(Game1.player, currency) / Math.Max(1, itemPriceAndStock[forSale[num3]].Price)), Math.Max(1, itemPriceAndStock[forSale[num3]].Stock)));
					if (ShopId == "ReturnedDonations")
					{
						val = itemPriceAndStock[forSale[num3]].Stock;
					}
					val = Math.Min(val, forSale[num3].maximumStackSize());
					if (val == -1)
					{
						val = 1;
					}
					if (canPurchaseCheck != null && !canPurchaseCheck(num3))
					{
						return;
					}
					if (val > 0 && tryToPurchaseItem(forSale[num3], heldItem, val, x, y))
					{
						itemPriceAndStock.Remove(forSale[num3]);
						forSale.RemoveAt(num3);
					}
					else if (val <= 0)
					{
						if (itemPriceAndStock[forSale[num3]].Price > 0)
						{
							Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
						}
						Game1.playSound("cancel");
					}
					if (heldItem != null && (_isStorageShop || Game1.options.SnappyMenus || (Game1.oldKBState.IsKeyDown(Keys.LeftShift) && (heldItem.maximumStackSize() == 1 || heldItem.Stack == 999))) && Game1.activeClickableMenu is ShopMenu && Game1.player.addItemToInventoryBool(heldItem as Item))
					{
						heldItem = null;
						DelayedAction.playSoundAfterDelay("coin", 100);
					}
				}
				currentItemIndex = Math.Max(0, Math.Min(forSale.Count - 4, currentItemIndex));
				updateSaleButtonNeighbors();
				setScrollBarToCurrentIndex();
				return;
			}
		}
		if (readyToClose() && (x < xPositionOnScreen - 64 || y < yPositionOnScreen - 64 || x > xPositionOnScreen + width + 128 || y > yPositionOnScreen + height + 64))
		{
			exitThisMenu();
		}
	}

	public virtual bool CanBuyback()
	{
		return true;
	}

	public virtual void BuyBuybackItem(ISalable bought_item, int price, int stack)
	{
		Game1.player.totalMoneyEarned -= (uint)price;
		if (Game1.player.useSeparateWallets)
		{
			Game1.player.stats.IndividualMoneyEarned -= (uint)price;
		}
		if (buyBackItemsToResellTomorrow.TryGetValue(bought_item, out var value))
		{
			value.Stack -= stack;
			if (value.Stack <= 0)
			{
				buyBackItemsToResellTomorrow.Remove(bought_item);
				(Game1.currentLocation as ShopLocation).itemsToStartSellingTomorrow.Remove(value as Item);
			}
		}
	}

	public virtual ISalable AddBuybackItem(ISalable sold_item, int sell_unit_price, int stack)
	{
		ISalable salable = null;
		while (stack > 0)
		{
			salable = null;
			foreach (ISalable buyBackItem in buyBackItems)
			{
				if (buyBackItem.canStackWith(sold_item) && buyBackItem.Stack < buyBackItem.maximumStackSize())
				{
					salable = buyBackItem;
					break;
				}
			}
			if (salable == null)
			{
				salable = sold_item.GetSalableInstance();
				int num = Math.Min(stack, salable.maximumStackSize());
				buyBackItems.Add(salable);
				itemPriceAndStock.Add(salable, new ItemStockInformation(sell_unit_price, num));
				salable.Stack = 1;
				stack -= num;
			}
			else
			{
				int num2 = Math.Min(stack, salable.maximumStackSize() - salable.Stack);
				ItemStockInformation itemStockInformation = itemPriceAndStock[salable];
				itemStockInformation.Stock += num2;
				itemPriceAndStock[salable] = itemStockInformation;
				salable.Stack = 1;
				stack -= num2;
			}
		}
		forSale = itemPriceAndStock.Keys.ToList();
		return salable;
	}

	public override bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
	{
		if (direction == 1 && tabButtons.Contains(a) && tabButtons.Contains(b))
		{
			return false;
		}
		return base.IsAutomaticSnapValid(direction, a, b);
	}

	public virtual void switchTab(int new_tab)
	{
		currentTab = new_tab;
		Game1.playSound("shwip");
		applyTab();
		if (Game1.options.snappyMenus && Game1.options.gamepadControls)
		{
			snapCursorToCurrentSnappedComponent();
		}
	}

	public virtual void applyTab()
	{
		if (currentTab < 0 || currentTab >= tabButtons.Count)
		{
			forSale = itemPriceAndStock.Keys.ToList();
			return;
		}
		ShopTabClickableTextureComponent shopTabClickableTextureComponent = tabButtons[currentTab];
		if (shopTabClickableTextureComponent.Filter == null)
		{
			shopTabClickableTextureComponent.Filter = (ISalable _) => true;
		}
		forSale.Clear();
		foreach (ISalable key in itemPriceAndStock.Keys)
		{
			if (shopTabClickableTextureComponent.Filter(key))
			{
				forSale.Add(key);
			}
		}
		currentItemIndex = 0;
		setScrollBarToCurrentIndex();
		updateSaleButtonNeighbors();
	}

	public override bool readyToClose()
	{
		if (heldItem == null)
		{
			return animations.Count == 0;
		}
		return false;
	}

	public override void emergencyShutDown()
	{
		base.emergencyShutDown();
		if (heldItem != null)
		{
			Game1.player.addItemToInventoryBool(heldItem as Item);
			Game1.playSound("coin");
		}
	}

	/// <summary>Play the open-menu sound.</summary>
	public void PlayOpenSound()
	{
		Game1.playSound(openMenuSound);
	}

	/// <summary>Get whether all items in the shop have been purchased.</summary>
	public bool IsOutOfStock()
	{
		if (!_isStorageShop)
		{
			return forSale.Count == 0;
		}
		return false;
	}

	public static void chargePlayer(Farmer who, int currencyType, int amount)
	{
		switch (currencyType)
		{
		case 0:
			who.Money -= amount;
			break;
		case 1:
			who.festivalScore -= amount;
			break;
		case 2:
			who.clubCoins -= amount;
			break;
		case 4:
			who.QiGems -= amount;
			break;
		case 3:
			break;
		}
	}

	public virtual void HandleSynchedItemPurchase(ISalable item, Farmer who, int number_purchased)
	{
		if (itemPriceAndStock.ContainsKey(item))
		{
			who.team.synchronizedShopStock.OnItemPurchased(ShopId, item, itemPriceAndStock, number_purchased);
		}
	}

	private bool tryToPurchaseItem(ISalable item, ISalable held_item, int stockToBuy, int x, int y)
	{
		if (readOnly)
		{
			return false;
		}
		ItemStockInformation itemStockInformation = itemPriceAndStock[item];
		if (held_item == null)
		{
			if (itemStockInformation.Stock == 0)
			{
				hoveredItem = null;
				return true;
			}
			if (stockToBuy > item.GetSalableInstance().maximumStackSize())
			{
				stockToBuy = Math.Max(1, item.GetSalableInstance().maximumStackSize());
			}
			int num = itemStockInformation.Price * stockToBuy;
			string text = null;
			int num2 = 5;
			int stack = stockToBuy * item.Stack;
			if (itemStockInformation.TradeItem != null)
			{
				text = itemStockInformation.TradeItem;
				if (itemStockInformation.TradeItemCount.HasValue)
				{
					num2 = itemStockInformation.TradeItemCount.Value;
				}
				num2 *= stockToBuy;
			}
			if (getPlayerCurrencyAmount(Game1.player, currency) >= num && (text == null || HasTradeItem(text, num2)))
			{
				heldItem = item.GetSalableInstance();
				heldItem.Stack = stack;
				if (!heldItem.CanBuyItem(Game1.player) && !item.IsInfiniteStock() && !item.IsRecipe)
				{
					Game1.playSound("smallSelect");
					heldItem = null;
					return false;
				}
				if (CanBuyback() && buyBackItems.Contains(item))
				{
					BuyBuybackItem(item, num, stack);
				}
				chargePlayer(Game1.player, currency, num);
				if (!string.IsNullOrEmpty(text))
				{
					ConsumeTradeItem(text, num2);
				}
				if (!_isStorageShop && item.actionWhenPurchased(ShopId))
				{
					if (item.IsRecipe)
					{
						(item as Item)?.LearnRecipe();
						Game1.playSound("newRecipe");
					}
					held_item = null;
					heldItem = null;
				}
				else
				{
					if ((heldItem as Item)?.QualifiedItemId == "(O)858")
					{
						Game1.player.team.addQiGemsToTeam.Fire(heldItem.Stack);
						heldItem = null;
					}
					if (Game1.mouseClickPolling > 300)
					{
						if (purchaseRepeatSound != null)
						{
							Game1.playSound(purchaseRepeatSound);
						}
					}
					else if (purchaseSound != null)
					{
						Game1.playSound(purchaseSound);
					}
				}
				if (itemStockInformation.Stock != int.MaxValue && !item.IsInfiniteStock())
				{
					HandleSynchedItemPurchase(item, Game1.player, stockToBuy);
					if (itemStockInformation.ItemToSyncStack != null)
					{
						itemStockInformation.ItemToSyncStack.Stack = itemStockInformation.Stock;
					}
				}
				List<string> actionsOnPurchase = itemStockInformation.ActionsOnPurchase;
				if (actionsOnPurchase != null && actionsOnPurchase.Count > 0)
				{
					foreach (string item2 in itemStockInformation.ActionsOnPurchase)
					{
						if (!TriggerActionManager.TryRunAction(item2, out var error, out var exception))
						{
							Game1.log.Error($"Shop {ShopId} ignored invalid action '{item2}' on purchase of item '{item.QualifiedItemId}': {error}", exception);
						}
					}
				}
				if (onPurchase != null && onPurchase(item, Game1.player, stockToBuy, itemStockInformation))
				{
					exitThisMenu();
				}
			}
			else
			{
				if (num > 0)
				{
					Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
				}
				Game1.playSound("cancel");
			}
		}
		else if (held_item.canStackWith(item))
		{
			stockToBuy = Math.Min(stockToBuy, (held_item.maximumStackSize() - held_item.Stack) / item.Stack);
			int num3 = stockToBuy * item.Stack;
			if (stockToBuy > 0)
			{
				int num4 = itemStockInformation.Price * stockToBuy;
				string text2 = null;
				int num5 = 5;
				if (itemStockInformation.TradeItem != null)
				{
					text2 = itemStockInformation.TradeItem;
					if (itemStockInformation.TradeItemCount.HasValue)
					{
						num5 = itemStockInformation.TradeItemCount.Value;
					}
					num5 *= stockToBuy;
				}
				ISalable salableInstance = item.GetSalableInstance();
				salableInstance.Stack = num3;
				if (!salableInstance.CanBuyItem(Game1.player))
				{
					Game1.playSound("cancel");
					return false;
				}
				if (getPlayerCurrencyAmount(Game1.player, currency) >= num4 && (text2 == null || HasTradeItem(text2, num5)))
				{
					heldItem.Stack += num3;
					if (CanBuyback() && buyBackItems.Contains(item))
					{
						BuyBuybackItem(item, num4, num3);
					}
					chargePlayer(Game1.player, currency, num4);
					if (Game1.mouseClickPolling > 300)
					{
						if (purchaseRepeatSound != null)
						{
							Game1.playSound(purchaseRepeatSound);
						}
					}
					else if (purchaseSound != null)
					{
						Game1.playSound(purchaseSound);
					}
					if (text2 != null)
					{
						ConsumeTradeItem(text2, num5);
					}
					if (!_isStorageShop && item.actionWhenPurchased(ShopId))
					{
						heldItem = null;
					}
					if (itemStockInformation.Stock != int.MaxValue && !item.IsInfiniteStock())
					{
						HandleSynchedItemPurchase(item, Game1.player, stockToBuy);
						if (itemStockInformation.ItemToSyncStack != null)
						{
							itemStockInformation.ItemToSyncStack.Stack = itemStockInformation.Stock;
						}
					}
					if (onPurchase != null && onPurchase(item, Game1.player, stockToBuy, itemStockInformation))
					{
						exitThisMenu();
					}
				}
				else
				{
					if (num4 > 0)
					{
						Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
					}
					Game1.playSound("cancel");
				}
			}
		}
		if (itemStockInformation.Stock <= 0)
		{
			buyBackItems.Remove(item);
			hoveredItem = null;
			return true;
		}
		return false;
	}

	/// <summary>Get whether the player's inventory contains a minimum number of a trade item.</summary>
	/// <param name="itemId">The qualified or unqualified item ID to find.</param>
	/// <param name="count">The number needed.</param>
	public bool HasTradeItem(string itemId, int count)
	{
		itemId = ItemRegistry.QualifyItemId(itemId);
		if (!(itemId == "(O)858"))
		{
			if (itemId == "(O)73")
			{
				return Game1.netWorldState.Value.GoldenWalnuts >= count;
			}
			return Game1.player.Items.ContainsId(itemId, count);
		}
		return Game1.player.QiGems >= count;
	}

	/// <summary>Reduce the number of an item held by the player.</summary>
	/// <param name="itemId">The qualified or unqualified item ID.</param>
	/// <param name="count">The number to remove.</param>
	public void ConsumeTradeItem(string itemId, int count)
	{
		itemId = ItemRegistry.QualifyItemId(itemId);
		if (!(itemId == "(O)858"))
		{
			if (itemId == "(O)73")
			{
				Game1.netWorldState.Value.GoldenWalnuts = Math.Max(0, Game1.netWorldState.Value.GoldenWalnuts - count);
			}
			else
			{
				Game1.player.Items.ReduceId(itemId, count);
			}
		}
		else
		{
			Game1.player.QiGems = Math.Max(0, Game1.player.QiGems - count);
		}
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		Vector2 vector = inventory.snapToClickableComponent(x, y);
		if (safetyTimer > 0)
		{
			return;
		}
		if (heldItem == null && !readOnly)
		{
			ISalable salable = inventory.rightClick(x, y, null, playSound: false);
			if (salable != null)
			{
				if (onSell != null)
				{
					onSell(salable);
				}
				else
				{
					int num = (int)((float)salable.sellToStorePrice(-1L) * sellPercentage);
					int stack = salable.Stack;
					ISalable salable2 = salable;
					chargePlayer(Game1.player, currency, -num * stack);
					ISalable salable3 = null;
					if (CanBuyback())
					{
						salable3 = AddBuybackItem(salable, num, stack);
					}
					salable = null;
					if (Game1.mouseClickPolling > 300)
					{
						if (purchaseRepeatSound != null)
						{
							Game1.playSound(purchaseRepeatSound);
						}
					}
					else if (purchaseSound != null)
					{
						Game1.playSound(purchaseSound);
					}
					int num2 = 2;
					for (int i = 0; i < num2; i++)
					{
						animations.Add(new TemporaryAnimatedSprite("TileSheets\\debris", new Rectangle(Game1.random.Next(2) * 16, 64, 16, 16), 9999f, 1, 999, vector + new Vector2(32f, 32f), flicker: false, flipped: false)
						{
							alphaFade = 0.025f,
							motion = new Vector2(Game1.random.Next(-3, 4), -4f),
							acceleration = new Vector2(0f, 0.5f),
							delayBeforeAnimationStart = i * 25,
							scale = 2f
						});
						animations.Add(new TemporaryAnimatedSprite("TileSheets\\debris", new Rectangle(Game1.random.Next(2) * 16, 64, 16, 16), 9999f, 1, 999, vector + new Vector2(32f, 32f), flicker: false, flipped: false)
						{
							scale = 4f,
							alphaFade = 0.025f,
							delayBeforeAnimationStart = i * 50,
							motion = Utility.getVelocityTowardPoint(new Point((int)vector.X + 32, (int)vector.Y + 32), new Vector2(xPositionOnScreen - 36, yPositionOnScreen + height - inventory.height - 16), 8f),
							acceleration = Utility.getVelocityTowardPoint(new Point((int)vector.X + 32, (int)vector.Y + 32), new Vector2(xPositionOnScreen - 36, yPositionOnScreen + height - inventory.height - 16), 0.5f)
						});
					}
					if (salable3 != null && buyBackItemsToResellTomorrow.TryGetValue(salable3, out var value))
					{
						value.Stack += stack;
					}
					else if (salable2 is Object obj && obj.edibility.Value != -300 && Game1.random.NextDouble() < 0.03999999910593033 && Game1.currentLocation is ShopLocation shopLocation)
					{
						ISalable salableInstance = salable2.GetSalableInstance();
						if (salable3 != null)
						{
							buyBackItemsToResellTomorrow[salable3] = salableInstance;
						}
						shopLocation.itemsToStartSellingTomorrow.Add(salableInstance as Item);
					}
					if (inventory.getItemAt(x, y) == null)
					{
						Game1.playSound("sell");
						animations.Add(new TemporaryAnimatedSprite(5, vector + new Vector2(32f, 32f), Color.White)
						{
							motion = new Vector2(0f, -0.5f)
						});
					}
				}
			}
		}
		else
		{
			heldItem = inventory.rightClick(x, y, heldItem as Item);
		}
		for (int j = 0; j < forSaleButtons.Count; j++)
		{
			if (currentItemIndex + j >= forSale.Count || !forSaleButtons[j].containsPoint(x, y))
			{
				continue;
			}
			int num3 = currentItemIndex + j;
			if (forSale[num3] == null)
			{
				break;
			}
			int num4 = 1;
			if (itemPriceAndStock[forSale[num3]].Price > 0)
			{
				num4 = ((!Game1.oldKBState.IsKeyDown(Keys.LeftShift)) ? 1 : Math.Min(Math.Min((!Game1.oldKBState.IsKeyDown(Keys.LeftControl)) ? 5 : (Game1.oldKBState.IsKeyDown(Keys.OemTilde) ? 999 : 25), getPlayerCurrencyAmount(Game1.player, currency) / itemPriceAndStock[forSale[num3]].Price), itemPriceAndStock[forSale[num3]].Stock));
			}
			if (canPurchaseCheck == null || canPurchaseCheck(num3))
			{
				if (num4 > 0 && tryToPurchaseItem(forSale[num3], heldItem, num4, x, y))
				{
					itemPriceAndStock.Remove(forSale[num3]);
					forSale.RemoveAt(num3);
				}
				if (heldItem != null && (_isStorageShop || Game1.options.SnappyMenus) && Game1.activeClickableMenu is ShopMenu && Game1.player.addItemToInventoryBool(heldItem as Item))
				{
					heldItem = null;
					DelayedAction.playSoundAfterDelay("coin", 100);
				}
				setScrollBarToCurrentIndex();
			}
			break;
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		hoverText = "";
		hoveredItem = null;
		hoverPrice = -1;
		boldTitleText = "";
		upArrow.tryHover(x, y);
		downArrow.tryHover(x, y);
		scrollBar.tryHover(x, y);
		if (scrolling)
		{
			return;
		}
		for (int i = 0; i < forSaleButtons.Count; i++)
		{
			if (currentItemIndex + i < forSale.Count && forSaleButtons[i].containsPoint(x, y))
			{
				ISalable salable = forSale[currentItemIndex + i];
				if (canPurchaseCheck == null || canPurchaseCheck(currentItemIndex + i))
				{
					hoverText = salable.getDescription();
					boldTitleText = salable.DisplayName;
					if (!_isStorageShop)
					{
						hoverPrice = ((itemPriceAndStock != null && itemPriceAndStock.TryGetValue(salable, out var value)) ? value.Price : salable.salePrice());
					}
					hoveredItem = salable;
					forSaleButtons[i].scale = Math.Min(forSaleButtons[i].scale + 0.03f, 1.1f);
				}
			}
			else
			{
				forSaleButtons[i].scale = Math.Max(1f, forSaleButtons[i].scale - 0.03f);
			}
		}
		if (heldItem != null)
		{
			return;
		}
		foreach (ClickableComponent item in inventory.inventory)
		{
			if (!item.containsPoint(x, y))
			{
				continue;
			}
			Item itemFromClickableComponent = inventory.getItemFromClickableComponent(item);
			if (itemFromClickableComponent == null || (inventory.highlightMethod != null && !inventory.highlightMethod(itemFromClickableComponent)))
			{
				continue;
			}
			if (_isStorageShop)
			{
				hoverText = itemFromClickableComponent.getDescription();
				boldTitleText = itemFromClickableComponent.DisplayName;
				hoveredItem = itemFromClickableComponent;
				continue;
			}
			hoverText = itemFromClickableComponent.DisplayName + " x" + itemFromClickableComponent.Stack;
			if (itemFromClickableComponent is Object obj && obj.needsToBeDonated())
			{
				hoverText = hoverText + "\n\n" + itemFromClickableComponent.getDescription() + "\n";
			}
			hoverPrice = (int)((float)itemFromClickableComponent.sellToStorePrice(-1L) * sellPercentage) * itemFromClickableComponent.Stack;
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		if (safetyTimer > 0)
		{
			safetyTimer -= time.ElapsedGameTime.Milliseconds;
		}
		if (poof != null && poof.update(time))
		{
			poof = null;
		}
		repositionTabs();
	}

	public void drawCurrency(SpriteBatch b)
	{
		if (!_isStorageShop && currency == 0)
		{
			Game1.dayTimeMoneyBox.drawMoneyBox(b, xPositionOnScreen - 36, yPositionOnScreen + height - inventory.height - 12);
		}
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		base.receiveGamePadButton(button);
		if (button != Buttons.RightTrigger && button != Buttons.LeftTrigger)
		{
			return;
		}
		ClickableComponent clickableComponent = currentlySnappedComponent;
		if (clickableComponent != null && clickableComponent.myID >= 3546)
		{
			int num = -1;
			for (int i = 0; i < 12; i++)
			{
				inventory.inventory[i].upNeighborID = 3546 + forSaleButtons.Count - 1;
				if (num == -1 && heldItem != null)
				{
					IList<Item> actualInventory = inventory.actualInventory;
					if (actualInventory != null && actualInventory.Count > i && inventory.actualInventory[i] == null)
					{
						num = i;
					}
				}
			}
			currentlySnappedComponent = getComponentWithID((num != -1) ? num : 0);
			snapCursorToCurrentSnappedComponent();
		}
		else
		{
			snapToDefaultClickableComponent();
		}
		Game1.playSound("shiny4");
	}

	private string getHoveredItemExtraItemIndex()
	{
		if (hoveredItem != null && itemPriceAndStock != null && itemPriceAndStock.TryGetValue(hoveredItem, out var value) && value.TradeItem != null)
		{
			return value.TradeItem;
		}
		return null;
	}

	private int getHoveredItemExtraItemAmount()
	{
		if (hoveredItem != null && itemPriceAndStock != null && itemPriceAndStock.TryGetValue(hoveredItem, out var value) && value.TradeItem != null && value.TradeItemCount.HasValue)
		{
			return value.TradeItemCount.Value;
		}
		return 5;
	}

	public void updatePosition()
	{
		width = 1000 + IClickableMenu.borderWidth * 2;
		height = 600 + IClickableMenu.borderWidth * 2;
		xPositionOnScreen = Game1.uiViewport.Width / 2 - (800 + IClickableMenu.borderWidth * 2) / 2;
		yPositionOnScreen = Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2;
		int num = xPositionOnScreen - 320;
		if ((portraitTexture == null && string.IsNullOrEmpty(potraitPersonDialogue)) || num <= 0 || !Game1.options.showMerchantPortraits)
		{
			xPositionOnScreen = Game1.uiViewport.Width / 2 - (1000 + IClickableMenu.borderWidth * 2) / 2;
			yPositionOnScreen = Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2;
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		ShopCachedTheme visualTheme = VisualTheme;
		updatePosition();
		initializeUpperRightCloseButton();
		Game1.player.forceCanMove();
		inventory = new InventoryMenu(xPositionOnScreen + width, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + 320 + 40, playerInventory: false, null, highlightItemToSell)
		{
			showGrayedOutSlots = true
		};
		inventory.movePosition(-inventory.width - 32, 0);
		upArrow = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + 16, 44, 48), visualTheme.ScrollUpTexture, visualTheme.ScrollUpSourceRect, 4f);
		downArrow = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 64, 44, 48), visualTheme.ScrollDownTexture, visualTheme.ScrollDownSourceRect, 4f);
		scrollBar = new ClickableTextureComponent(new Rectangle(upArrow.bounds.X + 12, upArrow.bounds.Y + upArrow.bounds.Height + 4, 24, 40), visualTheme.ScrollBarFrontTexture, visualTheme.ScrollBarFrontSourceRect, 4f);
		scrollBarRunner = new Rectangle(scrollBar.bounds.X, upArrow.bounds.Y + upArrow.bounds.Height + 4, scrollBar.bounds.Width, height - 64 - upArrow.bounds.Height - 28);
		forSaleButtons.Clear();
		for (int i = 0; i < 4; i++)
		{
			forSaleButtons.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 16 + i * ((height - 256) / 4), width - 32, (height - 256) / 4 + 4), i.ToString() ?? ""));
		}
		if (tabButtons.Count > 0)
		{
			foreach (ClickableComponent forSaleButton in forSaleButtons)
			{
				forSaleButton.leftNeighborID = -99998;
			}
		}
		repositionTabs();
		foreach (ClickableComponent item in inventory.GetBorder(InventoryMenu.BorderSide.Top))
		{
			item.upNeighborID = -99998;
		}
	}

	public void setItemPriceAndStock(Dictionary<ISalable, ItemStockInformation> new_stock)
	{
		itemPriceAndStock = new_stock;
		forSale = itemPriceAndStock.Keys.ToList();
		applyTab();
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (!Game1.options.showMenuBackground && !Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
		}
		ShopCachedTheme visualTheme = VisualTheme;
		IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), xPositionOnScreen + width - inventory.width - 32 - 24, yPositionOnScreen + height - 256 + 40, inventory.width + 56, height - 448 + 20, Color.White, 4f);
		IClickableMenu.drawTextureBox(b, visualTheme.WindowBorderTexture, visualTheme.WindowBorderSourceRect, xPositionOnScreen, yPositionOnScreen, width, height - 256 + 32 + 4, Color.White, 4f);
		drawCurrency(b);
		for (int i = 0; i < forSaleButtons.Count; i++)
		{
			ClickableComponent clickableComponent = forSaleButtons[i];
			if (currentItemIndex + i >= forSale.Count)
			{
				continue;
			}
			bool flag = canPurchaseCheck != null && !canPurchaseCheck(currentItemIndex + i);
			ISalable salable = forSale[currentItemIndex + i];
			ItemStockInformation itemStockInformation = itemPriceAndStock[salable];
			StackDrawType stackDrawType = GetStackDrawType(itemStockInformation, salable);
			string text = salable.DisplayName;
			IClickableMenu.drawTextureBox(b, visualTheme.ItemRowBackgroundTexture, visualTheme.ItemRowBackgroundSourceRect, clickableComponent.bounds.X, clickableComponent.bounds.Y, clickableComponent.bounds.Width, clickableComponent.bounds.Height, (clickableComponent.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) && !scrolling) ? visualTheme.ItemRowBackgroundHoverColor : Color.White, 4f, drawShadow: false);
			if (salable.Stack > 1)
			{
				text = text + " x" + salable.Stack;
			}
			if (salable.ShouldDrawIcon())
			{
				b.Draw(visualTheme.ItemIconBackgroundTexture, new Vector2(clickableComponent.bounds.X + 32 - 12, clickableComponent.bounds.Y + 24 - 4), visualTheme.ItemIconBackgroundSourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				Vector2 vector = new Vector2(clickableComponent.bounds.X + 32 - 8, clickableComponent.bounds.Y + 24);
				Color color = Color.White * ((!flag) ? 1f : 0.25f);
				int stock = itemStockInformation.Stock;
				salable.drawInMenu(b, vector, 1f, 1f, 0.9f, StackDrawType.HideButShowQuality, color, drawShadow: true);
				if (stock != int.MaxValue && ShopId != "ClintUpgrade" && ((stackDrawType == StackDrawType.Draw && stock > 1) || stackDrawType == StackDrawType.Draw_OneInclusive))
				{
					Utility.drawTinyDigits(stock, b, vector + new Vector2(64 - Utility.getWidthOfTinyDigitString(stock, 3f) + 3, 47f), 3f, 1f, color);
				}
				if (buyBackItems.Contains(salable))
				{
					b.Draw(Game1.mouseCursors2, new Vector2(clickableComponent.bounds.X + 32 - 8, clickableComponent.bounds.Y + 24), new Rectangle(64, 240, 16, 16), Color.White * ((!flag) ? 1f : 0.25f), 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, 1f);
				}
				string text2 = text;
				bool flag2 = itemStockInformation.Price > 0;
				if (SpriteText.getWidthOfString(text2) > width - (flag2 ? (150 + SpriteText.getWidthOfString(itemStockInformation.Price + " ")) : 100) && text2.Length > (flag2 ? 27 : 37))
				{
					text2 = text2.Substring(0, flag2 ? 27 : 37);
					text2 += "...";
				}
				SpriteText.drawString(b, text2, clickableComponent.bounds.X + 96 + 8, clickableComponent.bounds.Y + 28, 999999, -1, 999999, flag ? 0.5f : 1f, 0.88f, junimoText: false, -1, "", visualTheme.ItemRowTextColor);
			}
			else
			{
				SpriteText.drawString(b, text, clickableComponent.bounds.X + 32 + 8, clickableComponent.bounds.Y + 28, 999999, -1, 999999, flag ? 0.5f : 1f, 0.88f, junimoText: false, -1, "", visualTheme.ItemRowTextColor);
			}
			int num = clickableComponent.bounds.Right;
			int num2 = clickableComponent.bounds.Y + 28 - 4;
			int y = clickableComponent.bounds.Y + 44;
			if (itemStockInformation.Price > 0)
			{
				SpriteText.drawString(b, itemStockInformation.Price + " ", num - SpriteText.getWidthOfString(itemStockInformation.Price + " ") - 60, clickableComponent.bounds.Y + 28, 999999, -1, 999999, (getPlayerCurrencyAmount(Game1.player, currency) >= itemStockInformation.Price && !flag) ? 1f : 0.5f, 0.88f, junimoText: false, -1, "", visualTheme.ItemRowTextColor);
				Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(clickableComponent.bounds.Right - 52, clickableComponent.bounds.Y + 40 - 4), new Rectangle(193 + currency * 9, 373, 9, 10), Color.White * ((!flag) ? 1f : 0.25f), 0f, Vector2.Zero, 4f, flipped: false, -1f, -1, -1, (!flag) ? 0.35f : 0f);
				num -= SpriteText.getWidthOfString(itemStockInformation.Price + " ") + 96;
				num2 = clickableComponent.bounds.Y + 20;
				y = clickableComponent.bounds.Y + 28;
			}
			if (itemStockInformation.TradeItem != null)
			{
				int count = 5;
				string tradeItem = itemStockInformation.TradeItem;
				if (tradeItem != null && itemStockInformation.TradeItemCount.HasValue)
				{
					count = itemStockInformation.TradeItemCount.Value;
				}
				bool flag3 = HasTradeItem(tradeItem, count);
				if (canPurchaseCheck != null && !canPurchaseCheck(currentItemIndex + i))
				{
					flag3 = false;
				}
				float num3 = SpriteText.getWidthOfString("x" + count);
				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(tradeItem);
				Texture2D texture = dataOrErrorItem.GetTexture();
				Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
				Utility.drawWithShadow(b, texture, new Vector2((float)(num - 88) - num3, num2), sourceRect, Color.White * (flag3 ? 1f : 0.25f), 0f, Vector2.Zero, -1f, flipped: false, -1f, -1, -1, flag3 ? 0.35f : 0f);
				SpriteText.drawString(b, "x" + count, num - (int)num3 - 16, y, 999999, -1, 999999, flag3 ? 1f : 0.5f, 0.88f, junimoText: false, -1, "", visualTheme.ItemRowTextColor);
			}
		}
		if (IsOutOfStock())
		{
			string s = Game1.content.LoadString("Strings\\StringsFromCSFiles:ShopMenu.cs.11583");
			SpriteText.drawString(b, s, xPositionOnScreen + width / 2 - SpriteText.getWidthOfString(s) / 2, yPositionOnScreen + height / 2 - 128, 999999, -1, 999999, 1f, 0.88f, junimoText: false, -1, "", visualTheme?.ItemRowTextColor);
		}
		inventory.draw(b);
		for (int num4 = animations.Count - 1; num4 >= 0; num4--)
		{
			if (animations[num4].update(Game1.currentGameTime))
			{
				animations.RemoveAt(num4);
			}
			else
			{
				animations[num4].draw(b, localPosition: true);
			}
		}
		poof?.draw(b);
		upArrow.draw(b);
		downArrow.draw(b);
		foreach (ShopTabClickableTextureComponent tabButton in tabButtons)
		{
			tabButton.draw(b);
		}
		if (forSale.Count > 4)
		{
			IClickableMenu.drawTextureBox(b, visualTheme.ScrollBarBackTexture, visualTheme.ScrollBarBackSourceRect, scrollBarRunner.X, scrollBarRunner.Y, scrollBarRunner.Width, scrollBarRunner.Height, Color.White, 4f);
			scrollBar.draw(b);
		}
		if (hoverText != "")
		{
			Item item = hoveredItem as Item;
			ISalable salable2 = hoveredItem;
			if (salable2 != null && salable2.IsRecipe)
			{
				IClickableMenu.drawToolTip(b, " ", boldTitleText, item, heldItem != null, -1, currency, getHoveredItemExtraItemIndex(), getHoveredItemExtraItemAmount(), new CraftingRecipe(item?.BaseName ?? hoveredItem.Name), (hoverPrice > 0) ? hoverPrice : (-1));
			}
			else
			{
				IClickableMenu.drawToolTip(b, hoverText, boldTitleText, item, heldItem != null, -1, currency, getHoveredItemExtraItemIndex(), getHoveredItemExtraItemAmount(), null, (hoverPrice > 0) ? hoverPrice : (-1));
			}
		}
		heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f, 1f, 0.9f, StackDrawType.Draw, Color.White, drawShadow: true);
		base.draw(b);
		int num5 = xPositionOnScreen - 320;
		if (num5 > 0 && Game1.options.showMerchantPortraits)
		{
			if (portraitTexture != null)
			{
				Utility.drawWithShadow(b, visualTheme.PortraitBackgroundTexture, new Vector2(num5, yPositionOnScreen), visualTheme.PortraitBackgroundSourceRect, Color.White, 0f, Vector2.Zero, 4f, flipped: false, 0.91f);
				if (portraitTexture != null)
				{
					b.Draw(portraitTexture, new Vector2(num5 + 20, yPositionOnScreen + 20), new Rectangle(0, 0, 64, 64), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.92f);
				}
			}
			if (potraitPersonDialogue != null)
			{
				num5 = xPositionOnScreen - (int)Game1.dialogueFont.MeasureString(potraitPersonDialogue).X - 64;
				if (num5 > 0)
				{
					IClickableMenu.drawHoverText(b, potraitPersonDialogue, Game1.dialogueFont, 0, 0, -1, null, -1, null, null, 0, null, -1, num5, yPositionOnScreen + ((portraitTexture != null) ? 312 : 0), 1f, null, null, visualTheme.DialogueBackgroundTexture, visualTheme.DialogueBackgroundSourceRect, visualTheme.DialogueColor, visualTheme.DialogueShadowColor);
				}
			}
		}
		drawMouse(b);
	}

	/// <summary>Get how the stack size for a shop entry should be drawn.</summary>
	/// <param name="stockInfo">The shop entry's stock information.</param>
	/// <param name="item">The spawned item instance.</param>
	public StackDrawType GetStackDrawType(ItemStockInformation stockInfo, ISalable item)
	{
		if (item.IsRecipe)
		{
			return StackDrawType.Hide;
		}
		if (stockInfo.StackDrawType.HasValue)
		{
			return stockInfo.StackDrawType.Value;
		}
		if (stockInfo.Stock == int.MaxValue)
		{
			return StackDrawType.HideButShowQuality;
		}
		if (DefaultStackDrawType.HasValue)
		{
			return DefaultStackDrawType.Value;
		}
		ShopData shopData = ShopData;
		if (shopData != null && shopData.StackSizeVisibility.HasValue)
		{
			return ShopData.StackSizeVisibility switch
			{
				StackSizeVisibility.Hide => StackDrawType.HideButShowQuality, 
				StackSizeVisibility.ShowIfMultiple => StackDrawType.Draw, 
				_ => StackDrawType.Draw_OneInclusive, 
			};
		}
		if (!_isStorageShop)
		{
			return StackDrawType.Draw_OneInclusive;
		}
		return StackDrawType.Draw;
	}
}
