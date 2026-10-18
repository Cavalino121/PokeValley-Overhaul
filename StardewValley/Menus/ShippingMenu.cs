using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;

namespace StardewValley.Menus;

public class ShippingMenu : IClickableMenu
{
	public const int region_okbutton = 101;

	public const int region_forwardButton = 102;

	public const int region_backButton = 103;

	public const int farming_category = 0;

	public const int foraging_category = 1;

	public const int fishing_category = 2;

	public const int mining_category = 3;

	public const int other_category = 4;

	public const int total_category = 5;

	public const int timePerIntroCategory = 500;

	public const int outroFadeTime = 800;

	public const int smokeRate = 100;

	public const int categorylabelHeight = 25;

	public int itemsPerCategoryPage = 9;

	public int currentPage = -1;

	public int currentTab;

	public List<ClickableTextureComponent> categories = new List<ClickableTextureComponent>();

	public ClickableTextureComponent okButton;

	public ClickableTextureComponent forwardButton;

	public ClickableTextureComponent backButton;

	private List<int> categoryTotals = new List<int>();

	private List<MoneyDial> categoryDials = new List<MoneyDial>();

	private Dictionary<Item, int> itemValues = new Dictionary<Item, int>();

	private Dictionary<Item, int> singleItemValues = new Dictionary<Item, int>();

	private List<List<Item>> categoryItems = new List<List<Item>>();

	private int categoryLabelsWidth;

	private int plusButtonWidth;

	private int itemSlotWidth;

	private int itemAndPlusButtonWidth;

	private int totalWidth;

	private int centerX;

	private int centerY;

	private int introTimer = 3500;

	private int outroFadeTimer;

	private int outroPauseBeforeDateChange;

	private int finalOutroTimer;

	private int smokeTimer;

	private int dayPlaqueY;

	private int moonShake = -1;

	private int timesPokedMoon;

	private float weatherX;

	private bool outro;

	private bool newDayPlaque;

	private bool savedYet;

	public TemporaryAnimatedSpriteList animations = new TemporaryAnimatedSpriteList();

	private SaveGameMenu saveGameMenu;

	protected bool _hasFinished;

	public bool _activated;

	private bool wasGreenRain;

	public ShippingMenu(IList<Item> items)
		: base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height)
	{
		_activated = false;
		parseItems(items);
		if (!Game1.wasRainingYesterday)
		{
			Game1.changeMusicTrack(Game1.IsSummer ? "nightTime" : "none");
		}
		wasGreenRain = Utility.isGreenRainDay(Game1.dayOfMonth - 1, Game1.season);
		categoryLabelsWidth = 512;
		plusButtonWidth = 40;
		itemSlotWidth = 96;
		itemAndPlusButtonWidth = plusButtonWidth + itemSlotWidth + 8;
		totalWidth = categoryLabelsWidth + itemAndPlusButtonWidth;
		centerX = Game1.uiViewport.Width / 2;
		centerY = Game1.uiViewport.Height / 2;
		_hasFinished = false;
		int num = ((Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ru) ? 64 : 0);
		int num2 = -1;
		for (int i = 0; i < 6; i++)
		{
			categories.Add(new ClickableTextureComponent("", new Rectangle(centerX + num + totalWidth / 2 - plusButtonWidth, centerY - 300 + i * 27 * 4, plusButtonWidth, 44), "", getCategoryName(i), Game1.mouseCursors, new Rectangle(392, 361, 10, 11), 4f)
			{
				visible = (i < 5 && categoryItems[i].Count > 0),
				myID = i,
				downNeighborID = ((i < 4) ? (i + 1) : 101),
				upNeighborID = ((i > 0) ? num2 : (-1)),
				upNeighborImmutable = true
			});
			num2 = ((i < 5 && categoryItems[i].Count > 0) ? i : num2);
		}
		dayPlaqueY = categories[0].bounds.Y - 128;
		okButton = new ClickableTextureComponent(bounds: new Rectangle(centerX + num + totalWidth / 2 - itemAndPlusButtonWidth + 32, centerY + 300 - 64, 64, 64), name: Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11382"), label: null, hoverText: Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11382"), texture: Game1.mouseCursors, sourceRect: new Rectangle(128, 256, 64, 64), scale: 1f)
		{
			myID = 101,
			upNeighborID = num2
		};
		backButton = new ClickableTextureComponent("", new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + height - 64, 48, 44), null, "", Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
		{
			myID = 103,
			rightNeighborID = -7777
		};
		forwardButton = new ClickableTextureComponent("", new Rectangle(xPositionOnScreen + width - 32 - 48, yPositionOnScreen + height - 64, 48, 44), null, "", Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
		{
			myID = 102,
			leftNeighborID = 103
		};
		if (Game1.dayOfMonth == 25 && Game1.season == Season.Winter)
		{
			Vector2 position = new Vector2(Game1.uiViewport.Width, Game1.random.Next(0, 200));
			Rectangle sourceRect = new Rectangle(640, 800, 32, 16);
			int numberOfLoops = 1000;
			TemporaryAnimatedSprite item = new TemporaryAnimatedSprite("LooseSprites\\Cursors", sourceRect, 80f, 2, numberOfLoops, position, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true)
			{
				motion = new Vector2(-4f, 0f),
				delayBeforeAnimationStart = 3000
			};
			animations.Add(item);
		}
		Game1.stats.checkForShippingAchievements();
		RepositionItems();
		populateClickableComponentList();
		if (Game1.options.SnappyMenus)
		{
			snapToDefaultClickableComponent();
		}
	}

	public void RepositionItems()
	{
		centerX = Game1.uiViewport.Width / 2;
		centerY = Game1.uiViewport.Height / 2;
		int num = Game1.uiViewport.Width;
		int num2 = Game1.uiViewport.Height;
		num = Math.Min(width, 1280);
		num2 = Math.Min(height, 920);
		int num3 = ((Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ru) ? 64 : 0);
		for (int i = 0; i < 6; i++)
		{
			categories[i].bounds = new Rectangle(centerX + num3 + totalWidth / 2 - plusButtonWidth, centerY - 300 + i * 27 * 4, plusButtonWidth, 44);
		}
		dayPlaqueY = categories[0].bounds.Y - 128;
		if (dayPlaqueY < 0)
		{
			dayPlaqueY = -64;
		}
		backButton.bounds.X = centerX - num / 2 - 64;
		backButton.bounds.Y = centerY + num2 / 2 - 48;
		if (backButton.bounds.X < 0)
		{
			backButton.bounds.X = xPositionOnScreen + 32;
		}
		if (backButton.bounds.Y > Game1.uiViewport.Height - 32)
		{
			backButton.bounds.Y = Game1.uiViewport.Height - 80;
		}
		forwardButton.bounds.X = centerX + num / 2 + 8;
		forwardButton.bounds.Y = centerY + num2 / 2 - 48;
		if (forwardButton.bounds.X > Game1.uiViewport.Width - 32)
		{
			forwardButton.bounds.X = xPositionOnScreen + width - 32 - 48;
		}
		if (forwardButton.bounds.Y > Game1.uiViewport.Height - 32)
		{
			forwardButton.bounds.Y = Game1.uiViewport.Height - 80;
		}
		Rectangle bounds = new Rectangle(centerX + num3 + totalWidth / 2 - itemAndPlusButtonWidth + 32, centerY + 300 - 64, 64, 64);
		okButton.bounds = bounds;
		int num4 = Math.Min(height, 920);
		float num5 = yPositionOnScreen + num4 - 64 - (yPositionOnScreen + 32);
		itemsPerCategoryPage = (int)(num5 / 68f);
		if (currentPage >= 0)
		{
			currentTab = Utility.Clamp(currentTab, 0, (categoryItems[currentPage].Count - 1) / itemsPerCategoryPage);
		}
	}

	protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
	{
		if (oldID == 103 && direction == 1 && showForwardButton())
		{
			currentlySnappedComponent = getComponentWithID(102);
			snapCursorToCurrentSnappedComponent();
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		if (currentPage != -1)
		{
			currentlySnappedComponent = getComponentWithID(103);
		}
		else
		{
			currentlySnappedComponent = getComponentWithID(101);
		}
		snapCursorToCurrentSnappedComponent();
	}

	public void parseItems(IList<Item> items)
	{
		Utility.consolidateStacks(items);
		for (int i = 0; i < 6; i++)
		{
			categoryItems.Add(new List<Item>());
			categoryTotals.Add(0);
			categoryDials.Add(new MoneyDial(7, i == 5));
		}
		foreach (Item item in items)
		{
			if (item != null)
			{
				int categoryIndexForObject = getCategoryIndexForObject(item);
				categoryItems[categoryIndexForObject].Add(item);
				int num = item.sellToStorePrice(-1L);
				int num2 = num * item.Stack;
				categoryTotals[categoryIndexForObject] += num2;
				itemValues[item] = num2;
				singleItemValues[item] = num;
				Game1.stats.ItemsShipped += (uint)item.Stack;
				if (item.Category == -75 || item.Category == -79)
				{
					Game1.stats.CropsShipped += (uint)item.Stack;
				}
				if (item is Object obj && obj.countsForShippedCollection())
				{
					Game1.player.shippedBasic(obj.ItemId, obj.stack.Value);
				}
			}
		}
		for (int j = 0; j < 5; j++)
		{
			categoryTotals[5] += categoryTotals[j];
			categoryItems[5].AddRange(categoryItems[j]);
			categoryDials[j].currentValue = categoryTotals[j];
			categoryDials[j].previousTargetValue = categoryDials[j].currentValue;
		}
		categoryDials[5].currentValue = categoryTotals[5];
		Game1.setRichPresence("earnings", categoryTotals[5]);
	}

	public int getCategoryIndexForObject(Item item)
	{
		switch (item.QualifiedItemId)
		{
		case "(O)396":
		case "(O)406":
		case "(O)296":
		case "(O)402":
		case "(O)418":
		case "(O)414":
		case "(O)410":
			return 1;
		default:
			if (item is Object obj && (obj.preserve.Value == Object.PreserveType.SmokedFish || obj.preserve.Value == Object.PreserveType.AgedRoe || obj.preserve.Value == Object.PreserveType.Roe))
			{
				return 2;
			}
			switch (item.Category)
			{
			case -80:
			case -79:
			case -75:
			case -26:
			case -14:
			case -6:
			case -5:
				return 0;
			case -21:
			case -20:
			case -4:
				return 2;
			case -81:
			case -27:
			case -23:
				return 1;
			case -15:
			case -12:
			case -2:
				return 3;
			default:
				return 4;
			}
		}
	}

	public string getCategoryName(int index)
	{
		return index switch
		{
			0 => Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11389"), 
			1 => Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11390"), 
			2 => Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11391"), 
			3 => Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11392"), 
			4 => Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11393"), 
			5 => Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11394"), 
			_ => "", 
		};
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		if (!_activated)
		{
			_activated = true;
			Game1.player.team.endOfNightStatus.UpdateState("shipment");
		}
		if (_hasFinished)
		{
			if (Game1.PollForEndOfNewDaySync())
			{
				exitThisMenu(playSound: false);
			}
			return;
		}
		if (saveGameMenu != null)
		{
			saveGameMenu.update(time);
			if (saveGameMenu.quit)
			{
				saveGameMenu = null;
				savedYet = true;
			}
		}
		weatherX += (float)time.ElapsedGameTime.Milliseconds * 0.03f;
		animations.RemoveWhere((TemporaryAnimatedSprite animation) => animation.update(time));
		if (outro)
		{
			if (outroFadeTimer > 0)
			{
				outroFadeTimer -= time.ElapsedGameTime.Milliseconds;
			}
			else if (outroFadeTimer <= 0 && dayPlaqueY < centerY - 64)
			{
				if (animations.Count > 0)
				{
					animations.Clear();
				}
				dayPlaqueY += (int)Math.Ceiling((float)time.ElapsedGameTime.Milliseconds * 0.35f);
				if (dayPlaqueY >= centerY - 64)
				{
					outroPauseBeforeDateChange = 700;
				}
			}
			else if (outroPauseBeforeDateChange > 0)
			{
				outroPauseBeforeDateChange -= time.ElapsedGameTime.Milliseconds;
				if (outroPauseBeforeDateChange <= 0)
				{
					newDayPlaque = true;
					Game1.playSound("newRecipe");
					if (Game1.season != Season.Winter && Game1.game1.IsMainInstance)
					{
						DelayedAction.playSoundAfterDelay(Game1.IsRainingHere() ? "rainsound" : "rooster", 1500);
					}
					finalOutroTimer = 2000;
					animations.Clear();
					if (!savedYet)
					{
						if (saveGameMenu == null)
						{
							saveGameMenu = new SaveGameMenu();
						}
						return;
					}
				}
			}
			else if (finalOutroTimer > 0 && savedYet)
			{
				finalOutroTimer -= time.ElapsedGameTime.Milliseconds;
				if (finalOutroTimer <= 0)
				{
					_hasFinished = true;
				}
			}
		}
		if (introTimer >= 0)
		{
			int num = introTimer;
			introTimer -= time.ElapsedGameTime.Milliseconds * ((Game1.oldMouseState.LeftButton != ButtonState.Pressed) ? 1 : 3);
			if (num % 500 < introTimer % 500 && introTimer <= 3000)
			{
				int num2 = 4 - introTimer / 500;
				if (num2 < 6 && num2 > -1)
				{
					if (categoryItems[num2].Count > 0)
					{
						Game1.playSound(getCategorySound(num2));
						categoryDials[num2].currentValue = 0;
						categoryDials[num2].previousTargetValue = 0;
					}
					else
					{
						Game1.playSound("stoneStep");
					}
				}
			}
			if (introTimer < 0)
			{
				if (Game1.options.SnappyMenus)
				{
					snapToDefaultClickableComponent();
				}
				Game1.playSound("money");
				categoryDials[5].currentValue = 0;
				categoryDials[5].previousTargetValue = 0;
			}
		}
		else if (Game1.dayOfMonth != 28 && !outro)
		{
			if (!Game1.wasRainingYesterday)
			{
				Vector2 position = new Vector2(Game1.uiViewport.Width, Game1.random.Next(200));
				Rectangle sourceRect = new Rectangle(640, 752, 16, 16);
				int num3 = Game1.random.Next(1, 4);
				if (Game1.random.NextDouble() < 0.001)
				{
					bool flag = Game1.random.NextBool();
					if (Game1.random.NextBool())
					{
						animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(640, 826, 16, 8), 40f, 4, 0, new Vector2(Game1.random.Next(centerX * 2), Game1.random.Next(centerY)), flicker: false, flag)
						{
							rotation = (float)Math.PI,
							scale = 4f,
							motion = new Vector2(flag ? (-8) : 8, 8f),
							local = true
						});
					}
					else
					{
						animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(258, 1680, 16, 16), 40f, 4, 0, new Vector2(Game1.random.Next(centerX * 2), Game1.random.Next(centerY)), flicker: false, flag)
						{
							scale = 4f,
							motion = new Vector2(flag ? (-8) : 8, 8f),
							local = true
						});
					}
				}
				else if (Game1.random.NextDouble() < 0.0002)
				{
					TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(position: new Vector2(Game1.uiViewport.Width, Game1.random.Next(4, 256)), textureName: "", sourceRect: new Rectangle(0, 0, 1, 1), animationInterval: 9999f, animationLength: 1, numberOfLoops: 10000, flicker: false, flipped: false, layerDepth: 0.01f, alphaFade: 0f, color: Color.White * (0.25f + (float)Game1.random.NextDouble()), scale: 4f, scaleChange: 0f, rotation: 0f, rotationChange: 0f, local: true);
					temporaryAnimatedSprite.motion = new Vector2(-0.25f, 0f);
					animations.Add(temporaryAnimatedSprite);
				}
				else if (Game1.random.NextDouble() < 5E-05)
				{
					position = new Vector2(Game1.uiViewport.Width, Game1.uiViewport.Height - 192);
					for (int num4 = 0; num4 < num3; num4++)
					{
						TemporaryAnimatedSprite temporaryAnimatedSprite2 = new TemporaryAnimatedSprite("LooseSprites\\Cursors", sourceRect, Game1.random.Next(60, 101), 4, 100, position + new Vector2((num4 + 1) * Game1.random.Next(15, 18), (num4 + 1) * -20), flicker: false, flipped: false, 0.01f, 0f, Color.Black, 4f, 0f, 0f, 0f, local: true);
						temporaryAnimatedSprite2.motion = new Vector2(-1f, 0f);
						animations.Add(temporaryAnimatedSprite2);
						temporaryAnimatedSprite2 = new TemporaryAnimatedSprite("LooseSprites\\Cursors", sourceRect, Game1.random.Next(60, 101), 4, 100, position + new Vector2((num4 + 1) * Game1.random.Next(15, 18), (num4 + 1) * 20), flicker: false, flipped: false, 0.01f, 0f, Color.Black, 4f, 0f, 0f, 0f, local: true);
						temporaryAnimatedSprite2.motion = new Vector2(-1f, 0f);
						animations.Add(temporaryAnimatedSprite2);
					}
				}
				else if (Game1.random.NextDouble() < 1E-05)
				{
					sourceRect = new Rectangle(640, 784, 16, 16);
					TemporaryAnimatedSprite temporaryAnimatedSprite3 = new TemporaryAnimatedSprite("LooseSprites\\Cursors", sourceRect, 75f, 4, 1000, position, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true);
					temporaryAnimatedSprite3.motion = new Vector2(-3f, 0f);
					temporaryAnimatedSprite3.yPeriodic = true;
					temporaryAnimatedSprite3.yPeriodicLoopTime = 1000f;
					temporaryAnimatedSprite3.yPeriodicRange = 8f;
					temporaryAnimatedSprite3.shakeIntensity = 0.5f;
					animations.Add(temporaryAnimatedSprite3);
				}
			}
			smokeTimer -= time.ElapsedGameTime.Milliseconds;
			if (smokeTimer <= 0)
			{
				smokeTimer = 50;
				animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(684, 1075, 1, 1), 1000f, 1, 1000, new Vector2(188f, Game1.uiViewport.Height - 128 + 20), flicker: false, flipped: false)
				{
					color = (Game1.wasRainingYesterday ? Color.SlateGray : Color.White),
					scale = 4f,
					scaleChange = 0f,
					alphaFade = 0.0025f,
					motion = new Vector2(0f, (float)(-Game1.random.Next(25, 75)) / 100f / 4f),
					acceleration = new Vector2(-0.001f, 0f)
				});
			}
		}
		if (moonShake > 0)
		{
			moonShake -= time.ElapsedGameTime.Milliseconds;
		}
	}

	public string getCategorySound(int which)
	{
		switch (which)
		{
		case 0:
			if ((!((categoryItems[0][0] as Object)?.isAnimalProduct())) ?? true)
			{
				return "harvest";
			}
			return "cluck";
		case 2:
			return "button1";
		case 3:
			return "hammer";
		case 1:
			return "leafrustle";
		case 4:
			return "coin";
		case 5:
			return "money";
		default:
			return "stoneStep";
		}
	}

	public override void applyMovementKey(int direction)
	{
		if (CanReceiveInput())
		{
			base.applyMovementKey(direction);
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		if (!CanReceiveInput())
		{
			return;
		}
		base.performHoverAction(x, y);
		if (currentPage == -1)
		{
			okButton.tryHover(x, y);
			{
				foreach (ClickableTextureComponent category in categories)
				{
					if (category.containsPoint(x, y))
					{
						category.sourceRect.X = 402;
					}
					else
					{
						category.sourceRect.X = 392;
					}
				}
				return;
			}
		}
		backButton.tryHover(x, y, 0.5f);
		forwardButton.tryHover(x, y, 0.5f);
	}

	public bool CanReceiveInput()
	{
		if (introTimer > 0)
		{
			return false;
		}
		if (saveGameMenu != null)
		{
			return false;
		}
		if (outro)
		{
			return false;
		}
		return true;
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (!CanReceiveInput())
		{
			return;
		}
		if (introTimer <= 0 && !Game1.options.gamepadControls && (key.Equals(Keys.Escape) || Game1.options.doesInputListContain(Game1.options.menuButton, key)))
		{
			if (currentPage == -1)
			{
				receiveLeftClick(okButton.bounds.Center.X, okButton.bounds.Center.Y);
			}
			else
			{
				receiveLeftClick(backButton.bounds.Center.X, backButton.bounds.Center.Y);
			}
		}
		else if (introTimer <= 0 && (!Game1.options.gamepadControls || !Game1.options.doesInputListContain(Game1.options.menuButton, key)))
		{
			base.receiveKeyPress(key);
		}
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		if (!CanReceiveInput())
		{
			return;
		}
		base.receiveGamePadButton(button);
		if (button != Buttons.Start && button != Buttons.B)
		{
			return;
		}
		if (button == Buttons.B && currentPage != -1)
		{
			if (currentTab == 0)
			{
				if (Game1.options.SnappyMenus)
				{
					currentlySnappedComponent = getComponentWithID(currentPage);
					snapCursorToCurrentSnappedComponent();
				}
				currentPage = -1;
			}
			else
			{
				currentTab--;
			}
			Game1.playSound("shwip");
		}
		else if (currentPage == -1 && !outro)
		{
			if (introTimer <= 0)
			{
				okClicked();
			}
			else
			{
				introTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds * 2;
			}
		}
	}

	private void okClicked()
	{
		outro = true;
		outroFadeTimer = 800;
		Game1.playSound("bigDeSelect");
		Game1.changeMusicTrack("none");
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (!CanReceiveInput() || (outro && !savedYet) || savedYet)
		{
			return;
		}
		base.receiveLeftClick(x, y, playSound);
		if (currentPage == -1 && introTimer <= 0 && okButton.containsPoint(x, y))
		{
			okClicked();
		}
		if (currentPage == -1)
		{
			for (int i = 0; i < categories.Count; i++)
			{
				if (categories[i].visible && categories[i].containsPoint(x, y))
				{
					currentPage = i;
					Game1.playSound("shwip");
					if (Game1.options.SnappyMenus)
					{
						currentlySnappedComponent = getComponentWithID(103);
						snapCursorToCurrentSnappedComponent();
					}
					break;
				}
			}
			if (Game1.dayOfMonth == 28 && timesPokedMoon <= 10 && new Rectangle(Game1.uiViewport.Width - 176, 4, 172, 172).Contains(x, y))
			{
				moonShake = 100;
				timesPokedMoon++;
				if (timesPokedMoon > 10)
				{
					Game1.playSound("shadowDie");
				}
				else
				{
					Game1.playSound("thudStep");
				}
			}
		}
		else if (backButton.containsPoint(x, y))
		{
			if (currentTab == 0)
			{
				if (Game1.options.SnappyMenus)
				{
					currentlySnappedComponent = getComponentWithID(currentPage);
					snapCursorToCurrentSnappedComponent();
				}
				currentPage = -1;
			}
			else
			{
				currentTab--;
			}
			Game1.playSound("shwip");
		}
		else if (showForwardButton() && forwardButton.containsPoint(x, y))
		{
			currentTab++;
			Game1.playSound("shwip");
		}
	}

	public bool showForwardButton()
	{
		return categoryItems[currentPage].Count > itemsPerCategoryPage * (currentTab + 1);
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		initialize(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height);
		RepositionItems();
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		bool flag = Game1.season == Season.Winter;
		if (Game1.wasRainingYesterday)
		{
			b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(wasGreenRain ? 640 : 639, 858, 1, 184), (flag ? Color.LightSlateGray : (wasGreenRain ? Color.LightGreen : Color.SlateGray)) * (1f - (float)introTimer / 3500f));
			if (wasGreenRain)
			{
				b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(wasGreenRain ? 640 : 639, 858, 1, 184), Color.DimGray * 0.8f * (1f - (float)introTimer / 3500f));
			}
			for (int i = -244; i < Game1.uiViewport.Width + 244; i += 244)
			{
				b.Draw(Game1.mouseCursors, new Vector2((float)i + weatherX / 2f % 244f, 32f), new Rectangle(643, 1142, 61, 53), Color.DarkSlateGray * 1f * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			}
			for (int j = 0; j < width; j += 639)
			{
				b.Draw(Game1.mouseCursors, new Vector2(j * 4, Game1.uiViewport.Height - 192), new Rectangle(0, flag ? 1034 : 737, 639, 48), (flag ? (Color.White * 0.25f) : new Color(30, 62, 50)) * (0.5f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, 1f);
				b.Draw(Game1.mouseCursors, new Vector2(j * 4, Game1.uiViewport.Height - 128), new Rectangle(0, flag ? 1034 : 737, 639, 32), (flag ? (Color.White * 0.5f) : new Color(30, 62, 50)) * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			}
			b.Draw(Game1.mouseCursors, new Vector2(160f, Game1.uiViewport.Height - 128 + 16 + 8), new Rectangle(653, 880, 10, 10), Color.White * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			for (int k = -244; k < Game1.uiViewport.Width + 244; k += 244)
			{
				b.Draw(Game1.mouseCursors, new Vector2((float)k + weatherX % 244f, -32f), new Rectangle(643, 1142, 61, 53), Color.SlateGray * 0.85f * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
			}
			foreach (TemporaryAnimatedSprite animation in animations)
			{
				animation.draw(b, localPosition: true);
			}
			for (int l = -244; l < Game1.uiViewport.Width + 244; l += 244)
			{
				b.Draw(Game1.mouseCursors, new Vector2((float)l + weatherX * 1.5f % 244f, -128f), new Rectangle(643, 1142, 61, 53), Color.LightSlateGray * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
			}
		}
		else
		{
			b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(639, 858, 1, 184), Color.White * (1f - (float)introTimer / 3500f));
			for (int m = 0; m < width; m += 639)
			{
				b.Draw(Game1.mouseCursors, new Vector2(m * 4, 0f), new Rectangle(0, 1453, 639, 195), Color.White * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			}
			if (Game1.dayOfMonth == 28)
			{
				b.Draw(Game1.mouseCursors, new Vector2(Game1.uiViewport.Width - 176, 4f) + ((moonShake > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Rectangle(642, 835, 43, 43), Color.White * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				if (timesPokedMoon > 10)
				{
					b.Draw(Game1.mouseCursors, new Vector2(Game1.uiViewport.Width - 136, 48f) + ((moonShake > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Rectangle(685, 844 + ((Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 4000.0 < 200.0 || (Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 8000.0 > 7600.0 && Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 8000.0 < 7800.0)) ? 21 : 0), 19, 21), Color.White * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				}
			}
			b.Draw(Game1.mouseCursors, new Vector2(0f, Game1.uiViewport.Height - 192), new Rectangle(0, flag ? 1034 : 737, 639, 48), (flag ? (Color.White * 0.25f) : new Color(0, 20, 40)) * (0.65f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, 1f);
			b.Draw(Game1.mouseCursors, new Vector2(2556f, Game1.uiViewport.Height - 192), new Rectangle(0, flag ? 1034 : 737, 639, 48), (flag ? (Color.White * 0.25f) : new Color(0, 20, 40)) * (0.65f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, 1f);
			b.Draw(Game1.mouseCursors, new Vector2(0f, Game1.uiViewport.Height - 128), new Rectangle(0, flag ? 1034 : 737, 639, 32), (flag ? (Color.White * 0.5f) : new Color(0, 32, 20)) * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			b.Draw(Game1.mouseCursors, new Vector2(2556f, Game1.uiViewport.Height - 128), new Rectangle(0, flag ? 1034 : 737, 639, 32), (flag ? (Color.White * 0.5f) : new Color(0, 32, 20)) * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			b.Draw(Game1.mouseCursors, new Vector2(160f, Game1.uiViewport.Height - 128 + 16 + 8), new Rectangle(653, 880, 10, 10), Color.White * (1f - (float)introTimer / 3500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		}
		if (!outro && !Game1.wasRainingYesterday)
		{
			foreach (TemporaryAnimatedSprite animation2 in animations)
			{
				animation2.draw(b, localPosition: true);
			}
		}
		if (wasGreenRain)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Green * 0.1f);
		}
		if (currentPage == -1)
		{
			int num = categories[0].bounds.Y - 128;
			if (num >= 0)
			{
				SpriteText.drawStringWithScrollCenteredAt(b, Utility.getYesterdaysDate(), Game1.uiViewport.Width / 2, num);
			}
			int num2 = ((Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ru) ? 64 : 0);
			int num3 = -20;
			int num4 = 0;
			foreach (ClickableTextureComponent category in categories)
			{
				if (introTimer < 2500 - num4 * 500)
				{
					Vector2 vector = category.getVector2() + new Vector2(12 - num2, -8f);
					if (category.visible)
					{
						category.draw(b);
						b.Draw(Game1.mouseCursors, vector + new Vector2(-104 + num2, num3 + 4), new Rectangle(293, 360, 24, 24), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
						categoryItems[num4][0].drawInMenu(b, vector + new Vector2(-88 + num2, num3 + 16), 1f, 1f, 0.9f, StackDrawType.Hide);
					}
					IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), (int)(vector.X + (float)(-itemSlotWidth) - (float)categoryLabelsWidth - 12f), (int)(vector.Y + (float)num3), categoryLabelsWidth + num2, 104, Color.White, 4f, drawShadow: false);
					SpriteText.drawString(b, category.hoverText, (int)vector.X - itemSlotWidth - categoryLabelsWidth + 8, (int)vector.Y + 4);
					for (int n = 0; n < 6; n++)
					{
						b.Draw(Game1.mouseCursors, vector + new Vector2(-itemSlotWidth + num2 - 192 - 24 + n * 6 * 4, 12f), new Rectangle(355, 476, 7, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
					}
					categoryDials[num4].draw(b, vector + new Vector2(-itemSlotWidth + num2 - 192 - 48 + 4, 20f), categoryTotals[num4]);
					b.Draw(Game1.mouseCursors, vector + new Vector2(-itemSlotWidth + num2 - 64 - 4, 12f), new Rectangle(408, 476, 9, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
				}
				num4++;
			}
			if (introTimer <= 0)
			{
				okButton.draw(b);
			}
		}
		else
		{
			int num5 = Game1.uiViewport.Width;
			int num6 = Game1.uiViewport.Height;
			num5 = Math.Min(width, 1280);
			num6 = Math.Min(height, 920);
			int num7 = Game1.uiViewport.Width / 2 - num5 / 2;
			int num8 = Game1.uiViewport.Height / 2 - num6 / 2;
			IClickableMenu.drawTextureBox(b, num7, num8, num5, num6, Color.White);
			Vector2 location = new Vector2(num7 + 32, num8 + 32);
			for (int num9 = currentTab * itemsPerCategoryPage; num9 < currentTab * itemsPerCategoryPage + itemsPerCategoryPage; num9++)
			{
				if (categoryItems[currentPage].Count > num9)
				{
					Item item = categoryItems[currentPage][num9];
					item.drawInMenu(b, location, 1f, 1f, 1f, StackDrawType.Draw);
					string text = item.DisplayName + " x" + Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", singleItemValues[item]);
					string text2 = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", Utility.getNumberWithCommas(itemValues[item]));
					string text3 = text;
					int x = (int)location.X + num5 - 64 - SpriteText.getWidthOfString(text2);
					while (SpriteText.getWidthOfString(text3 + text2) < num5 - 192)
					{
						text3 += " .";
					}
					if (SpriteText.getWidthOfString(text3 + text2) >= num5)
					{
						text3 = text3.Remove(text3.Length - 1);
					}
					SpriteText.drawString(b, text3, (int)location.X + 64 + 12, (int)location.Y + 12);
					SpriteText.drawString(b, text2, x, (int)location.Y + 12);
					location.Y += 68f;
				}
			}
			backButton.draw(b);
			if (showForwardButton())
			{
				forwardButton.draw(b);
			}
		}
		if (outro)
		{
			b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(639, 858, 1, 184), Color.Black * (1f - (float)outroFadeTimer / 800f));
			SpriteText.drawStringWithScrollCenteredAt(b, newDayPlaque ? Utility.getDateString() : Utility.getYesterdaysDate(), Game1.uiViewport.Width / 2, dayPlaqueY);
			foreach (TemporaryAnimatedSprite animation3 in animations)
			{
				animation3.draw(b, localPosition: true);
			}
			if (finalOutroTimer > 0 || _hasFinished)
			{
				b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(0, 0, 1, 1), Color.Black * (1f - (float)finalOutroTimer / 2000f));
			}
		}
		saveGameMenu?.draw(b);
		if (!Game1.options.SnappyMenus || (introTimer <= 0 && !outro))
		{
			Game1.mouseCursorTransparency = 1f;
			drawMouse(b);
		}
	}
}
