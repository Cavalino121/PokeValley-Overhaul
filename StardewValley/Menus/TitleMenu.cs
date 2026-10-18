using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Minigames;

namespace StardewValley.Menus;

public class TitleMenu : IClickableMenu, IDisposable
{
	public static bool SkipSplashScreens = false;

	public const int region_muteMusic = 81111;

	public const int region_windowedButton = 81112;

	public const int region_aboutButton = 81113;

	public const int region_backButton = 81114;

	public const int region_newButton = 81115;

	public const int region_loadButton = 81116;

	public const int region_coopButton = 81119;

	public const int region_exitButton = 81117;

	public const int region_languagesButton = 81118;

	public const int fadeFromWhiteDuration = 2000;

	public const int viewportFinalPosition = -1000;

	public const int logoSwipeDuration = 1000;

	public const int numberOfButtons = 4;

	public const int spaceBetweenButtons = 8;

	public const float bigCloudDX = 0.1f;

	public const float mediumCloudDX = 0.2f;

	public const float smallCloudDX = 0.3f;

	public const float bgmountainsParallaxSpeed = 0.66f;

	public const float mountainsParallaxSpeed = 1f;

	public const float foregroundJungleParallaxSpeed = 2f;

	public const float cloudsParallaxSpeed = 0.5f;

	public static int pixelZoom = 3;

	public const string titleButtonsTextureName = "Minigames\\TitleButtons";

	public LocalizedContentManager menuContent = Game1.content.CreateTemporary();

	public Texture2D cloudsTexture;

	public Texture2D titleButtonsTexture;

	public bool specialSurprised;

	public float specialSurprisedTimeStamp;

	private Texture2D amuzioTexture;

	private List<float> bigClouds = new List<float>();

	private List<float> smallClouds = new List<float>();

	private TemporaryAnimatedSpriteList tempSprites = new TemporaryAnimatedSpriteList();

	private TemporaryAnimatedSpriteList behindSignTempSprites = new TemporaryAnimatedSpriteList();

	public List<ClickableTextureComponent> buttons = new List<ClickableTextureComponent>();

	public ClickableTextureComponent backButton;

	public ClickableTextureComponent muteMusicButton;

	public ClickableTextureComponent aboutButton;

	public ClickableTextureComponent languageButton;

	public ClickableTextureComponent windowedButton;

	public ClickableComponent skipButton;

	protected bool _movedCursor;

	public TemporaryAnimatedSpriteList birds = new TemporaryAnimatedSpriteList();

	private Rectangle eRect;

	private Rectangle screwRect;

	private Rectangle cornerRect;

	private Rectangle r_hole_rect;

	private Rectangle r_hole_rect2;

	private List<Rectangle> leafRects;

	[InstancedStatic]
	internal static IClickableMenu _subMenu;

	public readonly StartupPreferences startupPreferences;

	public int globalXOffset;

	public float viewportY;

	public float viewportDY;

	public float logoSwipeTimer;

	public float globalCloudAlpha = 1f;

	public float cornerClickEndTimer;

	public float cornerClickParrotTimer;

	public float cornerClickSoundEffectTimer;

	private bool? hasRoomAnotherFarm = false;

	public int fadeFromWhiteTimer;

	public int pauseBeforeViewportRiseTimer;

	public int buttonsToShow;

	public int showButtonsTimer;

	public int logoFadeTimer;

	public int logoSurprisedTimer;

	public int clicksOnE;

	public int clicksOnLeaf;

	public int clicksOnScrew;

	public int cornerClicks;

	public int buttonsDX;

	public bool titleInPosition;

	public bool isTransitioningButtons;

	public bool shades;

	public bool cornerPhaseHolding;

	public bool showCornerClickEasterEgg;

	public bool transitioningCharacterCreationMenu;

	private int amuzioTimer;

	internal static int windowNumber = 3;

	public string startupMessage = "";

	public Color startupMessageColor = Color.DeepSkyBlue;

	public string debugSaveFileToTry;

	private int bCount;

	private string whichSubMenu = "";

	private int quitTimer;

	private bool transitioningFromLoadScreen;

	[NonInstancedStatic]
	public static int ticksUntilLanguageLoad = 1;

	private bool disposedValue;

	/// <summary>The sub-menu to show instead of the main title screen.</summary>
	/// <remarks>When returning to the main title screen from a submenu, call <see cref="M:StardewValley.Menus.TitleMenu.ReturnToMainTitleScreen" /> instead of setting it to null to allow cleanup.</remarks>
	public static IClickableMenu subMenu
	{
		get
		{
			return _subMenu;
		}
		set
		{
			if (_subMenu != null)
			{
				_subMenu.exitFunction = null;
				if (_subMenu is IDisposable disposable && !subMenu.HasDependencies())
				{
					disposable.Dispose();
				}
			}
			_subMenu = value;
			if (_subMenu != null)
			{
				if (Game1.activeClickableMenu is TitleMenu titleMenu)
				{
					IClickableMenu clickableMenu = _subMenu;
					clickableMenu.exitFunction = (onExit)Delegate.Combine(clickableMenu.exitFunction, new onExit(titleMenu.CloseSubMenu));
				}
				if (Game1.options.snappyMenus && Game1.options.gamepadControls)
				{
					_subMenu.snapToDefaultClickableComponent();
				}
			}
		}
	}

	public bool HasActiveUser => true;

	/// <summary>An event raised when the player clicks the button to start after creating their new main character.</summary>
	public static event Action OnCreatedNewCharacter;

	/// <summary>Exit the current sub-menu and return to the main title screen.</summary>
	public static void ReturnToMainTitleScreen()
	{
		subMenu = null;
		Game1.game1.ResetGameStateOnTitleScreen();
	}

	public void ForceSubmenu(IClickableMenu menu)
	{
		skipToTitleButtons();
		subMenu = menu;
		moveFeatures(1920, 0);
		globalXOffset = 1920;
		buttonsToShow = 4;
		showButtonsTimer = 0;
		viewportDY = 0f;
		logoSwipeTimer = 0f;
		titleInPosition = true;
	}

	public TitleMenu()
		: base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height)
	{
		LocalizedContentManager.OnLanguageChange += OnLanguageChange;
		cloudsTexture = menuContent.Load<Texture2D>("Minigames\\Clouds");
		titleButtonsTexture = menuContent.Load<Texture2D>("Minigames\\TitleButtons");
		if (Program.sdk.IsJapaneseRegionRelease)
		{
			amuzioTexture = menuContent.Load<Texture2D>("Minigames\\Amuzio");
		}
		viewportY = 0f;
		fadeFromWhiteTimer = 4000;
		logoFadeTimer = 5000;
		if (Program.sdk.IsJapaneseRegionRelease)
		{
			amuzioTimer = 4000;
		}
		bigClouds.Add(width * 3 / 4);
		shades = Game1.random.NextBool();
		smallClouds.Add(width - 1);
		smallClouds.Add(width - 1 + 230 * pixelZoom);
		smallClouds.Add(width * 2 / 3);
		smallClouds.Add(width / 8);
		smallClouds.Add(width - 1 + 430 * pixelZoom);
		smallClouds.Add(width * 3 / 4);
		smallClouds.Add(1f);
		smallClouds.Add(width / 2 + 150 * pixelZoom);
		smallClouds.Add(width - 1 + 630 * pixelZoom);
		smallClouds.Add(width - 1 + 130 * pixelZoom);
		smallClouds.Add(width / 3 + 190 * pixelZoom);
		smallClouds.Add(1 + 100 * pixelZoom);
		smallClouds.Add(width / 2 + 830 * pixelZoom);
		smallClouds.Add(width * 2 / 3 + 120 * pixelZoom);
		smallClouds.Add(width * 3 / 4 + 170 * pixelZoom);
		smallClouds.Add(width / 4 + 220 * pixelZoom);
		for (int i = 0; i < smallClouds.Count; i++)
		{
			smallClouds[i] += Game1.random.Next(400);
		}
		birds.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(296, 227, 26, 21), new Vector2(width - 70 * pixelZoom, height - 130 * pixelZoom), flipped: false, 0f, Color.White)
		{
			scale = pixelZoom,
			pingPong = true,
			animationLength = 4,
			interval = 100f,
			totalNumberOfLoops = 9999,
			local = true,
			motion = new Vector2(-1f, 0f),
			layerDepth = 0.25f
		});
		birds.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(296, 227, 26, 21), new Vector2(width - 40 * pixelZoom, height - 120 * pixelZoom), flipped: false, 0f, Color.White)
		{
			scale = pixelZoom,
			pingPong = true,
			animationLength = 4,
			interval = 100f,
			totalNumberOfLoops = 9999,
			local = true,
			delayBeforeAnimationStart = 100,
			motion = new Vector2(-1f, 0f),
			layerDepth = 0.25f
		});
		setUpIcons();
		muteMusicButton = new ClickableTextureComponent(new Rectangle(16, 16, 36, 36), Game1.mouseCursors, new Rectangle(128, 384, 9, 9), 4f)
		{
			myID = 81111,
			downNeighborID = 81115,
			rightNeighborID = 81112
		};
		windowedButton = new ClickableTextureComponent(new Rectangle(Game1.uiViewport.Width - 36 - 16, 16, 36, 36), Game1.mouseCursors, new Rectangle((Game1.options != null && !Game1.options.isCurrentlyWindowed()) ? 155 : 146, 384, 9, 9), 4f)
		{
			myID = 81112,
			leftNeighborID = 81111,
			downNeighborID = 81113
		};
		startupPreferences = new StartupPreferences();
		startupPreferences.loadPreferences(async: false, applyLanguage: false);
		applyPreferences();
		switch (startupPreferences.timesPlayed)
		{
		case 3:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11718");
			break;
		case 5:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11720");
			break;
		case 7:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11722");
			break;
		case 2:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11717");
			break;
		case 4:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11719");
			break;
		case 6:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11721");
			break;
		case 8:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11723");
			break;
		case 9:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11724");
			break;
		case 10:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11725");
			break;
		case 15:
			if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en)
			{
				string randomNoun = Dialogue.getRandomNoun();
				string randomNoun2 = Dialogue.getRandomNoun();
				startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11726") + Environment.NewLine + "The " + Dialogue.getRandomAdjective() + " " + randomNoun + " " + Dialogue.getRandomVerb() + " " + Dialogue.getRandomPositional() + " the " + (randomNoun.Equals(randomNoun2) ? ("other " + randomNoun2) : randomNoun2);
			}
			else
			{
				int num = Game1.random.Next(1, 15);
				startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11726") + menuContent.LoadString("Strings\\StringsFromCSFiles:RandomSentence." + num);
			}
			break;
		case 20:
			startupMessage = "<";
			break;
		case 30:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11731");
			break;
		case 100:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11732");
			break;
		case 1000:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11733");
			break;
		case 10000:
			startupMessage = menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11734");
			break;
		}
		startupPreferences.savePreferences(async: false);
		Game1.setRichPresence("menus");
		if (Game1.options.snappyMenus && Game1.options.gamepadControls)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
		if (SkipSplashScreens)
		{
			skipToTitleButtons();
		}
		else
		{
			SkipSplashScreens = true;
		}
	}

	private bool alternativeTitleGraphic()
	{
		return LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh;
	}

	public void applyPreferences()
	{
		if (startupPreferences.playerLimit > 0)
		{
			Game1.multiplayer.playerLimit = startupPreferences.playerLimit;
		}
		if (startupPreferences.startMuted)
		{
			if (Utility.toggleMuteMusic())
			{
				muteMusicButton.sourceRect.X = 137;
			}
			else
			{
				muteMusicButton.sourceRect.X = 128;
			}
		}
		if (startupPreferences.skipWindowPreparation && windowNumber == 3)
		{
			windowNumber = -1;
		}
		if (startupPreferences.windowMode == 2 && startupPreferences.fullscreenResolutionX != 0 && startupPreferences.fullscreenResolutionY != 0)
		{
			Game1.options.preferredResolutionX = startupPreferences.fullscreenResolutionX;
			Game1.options.preferredResolutionY = startupPreferences.fullscreenResolutionY;
		}
		Game1.options.gamepadMode = startupPreferences.gamepadMode;
		Game1.game1.CheckGamepadMode();
		if (Game1.options.gamepadControls && Game1.options.snappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	private void OnLanguageChange(LocalizedContentManager.LanguageCode code)
	{
		titleButtonsTexture = menuContent.Load<Texture2D>("Minigames\\TitleButtons");
		setUpIcons();
		tempSprites.Clear();
		startupPreferences.OnLanguageChange(code);
	}

	public void skipToTitleButtons()
	{
		logoFadeTimer = 0;
		logoSwipeTimer = 0f;
		titleInPosition = false;
		pauseBeforeViewportRiseTimer = 0;
		fadeFromWhiteTimer = 0;
		viewportY = -999f;
		viewportDY = -0.01f;
		birds.Clear();
		logoSwipeTimer = 1f;
		amuzioTimer = 0;
		Game1.changeMusicTrack("MainTheme");
		if (Game1.options.SnappyMenus && Game1.options.gamepadControls)
		{
			snapToDefaultClickableComponent();
		}
	}

	public void setUpIcons()
	{
		buttons.Clear();
		int num = 74;
		int num2 = num * 4 * pixelZoom;
		num2 += 24 * pixelZoom;
		int num3 = width / 2 - num2 / 2;
		buttons.Add(new ClickableTextureComponent("New", new Rectangle(num3, height - 58 * pixelZoom - 8 * pixelZoom, num * pixelZoom, 58 * pixelZoom), null, "", titleButtonsTexture, new Rectangle(0, 187, 74, 58), pixelZoom)
		{
			myID = 81115,
			rightNeighborID = 81116,
			upNeighborID = 81111
		});
		num3 += (num + 8) * pixelZoom;
		buttons.Add(new ClickableTextureComponent("Load", new Rectangle(num3, height - 58 * pixelZoom - 8 * pixelZoom, 74 * pixelZoom, 58 * pixelZoom), null, "", titleButtonsTexture, new Rectangle(74, 187, 74, 58), pixelZoom)
		{
			myID = 81116,
			leftNeighborID = 81115,
			rightNeighborID = -7777,
			upNeighborID = 81111
		});
		num3 += (num + 8) * pixelZoom;
		buttons.Add(new ClickableTextureComponent("Co-op", new Rectangle(num3, height - 58 * pixelZoom - 8 * pixelZoom, 74 * pixelZoom, 58 * pixelZoom), null, "", titleButtonsTexture, new Rectangle(148, 187, 74, 58), pixelZoom)
		{
			myID = 81119,
			leftNeighborID = 81116,
			rightNeighborID = 81117
		});
		num3 += (num + 8) * pixelZoom;
		buttons.Add(new ClickableTextureComponent("Exit", new Rectangle(num3, height - 58 * pixelZoom - 8 * pixelZoom, 74 * pixelZoom, 58 * pixelZoom), null, "", titleButtonsTexture, new Rectangle(222, 187, 74, 58), pixelZoom)
		{
			myID = 81117,
			leftNeighborID = 81119,
			rightNeighborID = 81118,
			upNeighborID = 81111
		});
		int num4 = (ShouldShrinkLogo() ? 2 : pixelZoom);
		eRect = new Rectangle(width / 2 - 200 * num4 + 251 * num4, -300 * num4 - (int)(viewportY / 3f) * num4 + 26 * num4, 42 * num4, 68 * num4);
		screwRect = new Rectangle(width / 2 + 150 * num4, -300 * num4 - (int)(viewportY / 3f) * num4 + 80 * num4, 5 * num4, 5 * num4);
		cornerRect = new Rectangle(width / 2 - 200 * num4, -300 * num4 - (int)(viewportY / 3f) * num4 + 165 * num4, 20 * num4, 20 * num4);
		r_hole_rect = new Rectangle(width / 2 - 21 * num4, -300 * num4 - (int)(viewportY / 3f) * num4 + 39 * num4, 10 * num4, 11 * num4);
		r_hole_rect2 = new Rectangle(width / 2 - 35 * num4, -300 * num4 - (int)(viewportY / 3f) * num4 + 24 * num4, 7 * num4, 7 * num4);
		populateLeafRects();
		backButton = new ClickableTextureComponent(menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11739"), new Rectangle(width + -66 * pixelZoom - 8 * pixelZoom * 2, height - 27 * pixelZoom - 8 * pixelZoom, 66 * pixelZoom, 27 * pixelZoom), null, "", titleButtonsTexture, new Rectangle(296, 252, 66, 27), pixelZoom)
		{
			myID = 81114
		};
		aboutButton = new ClickableTextureComponent(menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11740"), new Rectangle(width + -22 * pixelZoom - 8 * pixelZoom * 2, height - 25 * pixelZoom - 8 * pixelZoom, 22 * pixelZoom, 25 * pixelZoom), null, "", titleButtonsTexture, new Rectangle(8, 458, 22, 25), pixelZoom)
		{
			myID = 81113,
			upNeighborID = 81118,
			leftNeighborID = -7777
		};
		languageButton = new ClickableTextureComponent(menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11740"), new Rectangle(width + -22 * pixelZoom - 8 * pixelZoom * 2, height - 25 * pixelZoom * 2 - 16 * pixelZoom, 27 * pixelZoom, 25 * pixelZoom), null, "", titleButtonsTexture, new Rectangle(52, 458, 27, 25), pixelZoom)
		{
			myID = 81118,
			downNeighborID = 81113,
			leftNeighborID = -7777,
			upNeighborID = 81112
		};
		skipButton = new ClickableComponent(new Rectangle(width / 2 - 87 * pixelZoom, height / 2 - 34 * pixelZoom, 83 * pixelZoom, 67 * pixelZoom), menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11741"));
		if (globalXOffset > width)
		{
			globalXOffset = width;
		}
		foreach (ClickableTextureComponent button in buttons)
		{
			button.bounds.X += globalXOffset;
		}
		if (Game1.options.gamepadControls && Game1.options.snappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		if (subMenu != null)
		{
			subMenu.snapToDefaultClickableComponent();
			return;
		}
		StartupPreferences obj = startupPreferences;
		currentlySnappedComponent = getComponentWithID((obj != null && obj.timesPlayed > 0) ? 81116 : 81115);
		snapCursorToCurrentSnappedComponent();
	}

	protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
	{
		if (oldID == 81116 && direction == 1)
		{
			if (getComponentWithID(81119) != null)
			{
				setCurrentlySnappedComponentTo(81119);
				snapCursorToCurrentSnappedComponent();
			}
			else if (getComponentWithID(81117) != null)
			{
				setCurrentlySnappedComponentTo(81117);
				snapCursorToCurrentSnappedComponent();
			}
			else
			{
				setCurrentlySnappedComponentTo(81118);
				snapCursorToCurrentSnappedComponent();
			}
		}
		else if ((oldID == 81118 || oldID == 81113) && direction == 3)
		{
			if (getComponentWithID(81117) != null)
			{
				setCurrentlySnappedComponentTo(81117);
				snapCursorToCurrentSnappedComponent();
			}
			else
			{
				setCurrentlySnappedComponentTo(81116);
				snapCursorToCurrentSnappedComponent();
			}
		}
	}

	public void populateLeafRects()
	{
		int num = (ShouldShrinkLogo() ? 2 : pixelZoom);
		leafRects = new List<Rectangle>
		{
			new Rectangle(width / 2 - 200 * num + 251 * num - 196 * num, -300 * num - (int)(viewportY / 3f) * num + 26 * num + 109 * num, 17 * num, 30 * num),
			new Rectangle(width / 2 - 200 * num + 251 * num + 91 * num, -300 * num - (int)(viewportY / 3f) * num + 26 * num - 26 * num, 17 * num, 31 * num),
			new Rectangle(width / 2 - 200 * num + 251 * num + 79 * num, -300 * num - (int)(viewportY / 3f) * num + 26 * num + 83 * num, 25 * num, 17 * num),
			new Rectangle(width / 2 - 200 * num + 251 * num - 213 * num, -300 * num - (int)(viewportY / 3f) * num + 26 * num - 24 * num, 14 * num, 23 * num),
			new Rectangle(width / 2 - 200 * num + 251 * num - 234 * num, -300 * num - (int)(viewportY / 3f) * num + 26 * num - 11 * num, 18 * num, 12 * num)
		};
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (ShouldAllowInteraction() && !transitioningCharacterCreationMenu)
		{
			subMenu?.receiveRightClick(x, y);
		}
	}

	public override bool readyToClose()
	{
		return false;
	}

	/// <inheritdoc />
	public override bool overrideSnappyMenuCursorMovementBan()
	{
		return !titleInPosition;
	}

	/// <inheritdoc />
	public override void leftClickHeld(int x, int y)
	{
		if (!transitioningCharacterCreationMenu)
		{
			base.leftClickHeld(x, y);
			if (subMenu != null)
			{
				subMenu.leftClickHeld(x, y);
			}
		}
	}

	/// <inheritdoc />
	public override void releaseLeftClick(int x, int y)
	{
		if (!transitioningCharacterCreationMenu && !transitioningCharacterCreationMenu)
		{
			base.releaseLeftClick(x, y);
			subMenu?.releaseLeftClick(x, y);
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (transitioningCharacterCreationMenu)
		{
			return;
		}
		switch (key)
		{
		case Keys.N:
			if (!Program.releaseBuild && Game1.oldKBState.IsKeyDown(Keys.RightShift) && Game1.oldKBState.IsKeyDown(Keys.LeftControl))
			{
				Season season = Season.Spring;
				if (Game1.oldKBState.IsKeyDown(Keys.D1))
				{
					Game1.whichFarm = 1;
				}
				else if (Game1.oldKBState.IsKeyDown(Keys.D2))
				{
					Game1.whichFarm = 2;
				}
				else if (Game1.oldKBState.IsKeyDown(Keys.D3))
				{
					Game1.whichFarm = 3;
				}
				else if (Game1.oldKBState.IsKeyDown(Keys.D4))
				{
					Game1.whichFarm = 4;
				}
				else if (Game1.oldKBState.IsKeyDown(Keys.D5))
				{
					Game1.whichFarm = 5;
				}
				else if (Game1.oldKBState.IsKeyDown(Keys.D6))
				{
					Game1.whichFarm = 6;
				}
				if (Game1.oldKBState.IsKeyDown(Keys.C))
				{
					Game1.whichFarm = Game1.random.Next(6);
					Game1.season = (Season)Game1.random.Next(4);
				}
				Game1.game1.loadForNewGame();
				Game1.saveOnNewDay = false;
				Game1.player.eventsSeen.Add("60367");
				Game1.player.currentLocation = Utility.getHomeOfFarmer(Game1.player);
				Game1.player.Position = new Vector2(9f, 9f) * 64f;
				Game1.player.isInBed.Value = true;
				Game1.player.farmName.Value = "Test";
				if (Game1.oldKBState.IsKeyDown(Keys.C))
				{
					Game1.season = season;
					Game1.setGraphicsForSeason(onLoad: true);
				}
				Game1.player.mailReceived.Add("button_tut_1");
				Game1.player.mailReceived.Add("button_tut_2");
				Game1.NewDay(0f);
				Game1.exitActiveMenu();
				Game1.setGameMode(3);
				return;
			}
			break;
		case Keys.Escape:
		case Keys.B:
			if (logoFadeTimer > 0)
			{
				bCount++;
				if (key == Keys.Escape)
				{
					bCount += 3;
				}
				if (bCount >= 3)
				{
					Game1.playSound("bigDeSelect");
					logoFadeTimer = 0;
					fadeFromWhiteTimer = 0;
					Game1.delayedActions.Clear();
					Game1.morningSongPlayAction = null;
					pauseBeforeViewportRiseTimer = 0;
					fadeFromWhiteTimer = 0;
					viewportY = -999f;
					viewportDY = -0.01f;
					birds.Clear();
					logoSwipeTimer = 1f;
					amuzioTimer = 0;
					Game1.changeMusicTrack("MainTheme");
				}
			}
			break;
		}
		if (!Game1.options.doesInputListContain(Game1.options.menuButton, key) && ShouldAllowInteraction())
		{
			subMenu?.receiveKeyPress(key);
			if (Game1.options.snappyMenus && Game1.options.gamepadControls && subMenu == null)
			{
				base.receiveKeyPress(key);
			}
		}
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		base.receiveGamePadButton(button);
		subMenu?.receiveGamePadButton(button);
		if (button != Buttons.B || !titleInPosition || logoFadeTimer > 0 || fadeFromWhiteTimer > 0)
		{
			return;
		}
		IClickableMenu clickableMenu = subMenu;
		if (!(clickableMenu is LoadGameMenu loadGameMenu))
		{
			if (clickableMenu is CharacterCustomization characterCustomization)
			{
				if (!characterCustomization.showingCoopHelp)
				{
					backButtonPressed();
				}
			}
			else
			{
				backButtonPressed();
			}
		}
		else if (!loadGameMenu.deleteConfirmationScreen)
		{
			backButtonPressed();
		}
	}

	/// <inheritdoc />
	public override void gamePadButtonHeld(Buttons b)
	{
		if (!Game1.lastCursorMotionWasMouse)
		{
			_movedCursor = true;
		}
		subMenu?.gamePadButtonHeld(b);
	}

	public void backButtonPressed()
	{
		if (subMenu == null || !subMenu.readyToClose())
		{
			return;
		}
		Game1.playSound("bigDeSelect");
		buttonsDX = -1;
		if (subMenu is AboutMenu)
		{
			ReturnToMainTitleScreen();
			buttonsDX = 0;
			if (Game1.options.SnappyMenus)
			{
				setCurrentlySnappedComponentTo(81113);
				snapCursorToCurrentSnappedComponent();
			}
		}
		else if (subMenu is TitleTextInputMenu { context: "join_menu" } || subMenu is FarmhandMenu)
		{
			buttonsDX = 0;
			((CoopMenu)(subMenu = new CoopMenu(tooManyFarms: false))).SetTab(CoopMenu.Tab.JOIN_TAB, playSound: false);
			if (Game1.options.SnappyMenus)
			{
				subMenu.snapToDefaultClickableComponent();
			}
		}
		else if (subMenu is CharacterCustomization { source: CharacterCustomization.Source.HostNewFarm })
		{
			buttonsDX = 0;
			((CoopMenu)(subMenu = new CoopMenu(tooManyFarms: false))).SetTab(CoopMenu.Tab.HOST_TAB, playSound: false);
			Game1.changeMusicTrack("title_night");
			if (Game1.options.SnappyMenus)
			{
				subMenu.snapToDefaultClickableComponent();
			}
		}
		else
		{
			isTransitioningButtons = true;
			if (subMenu is LoadGameMenu)
			{
				transitioningFromLoadScreen = true;
			}
			ReturnToMainTitleScreen();
			Game1.changeMusicTrack("spring_day_ambient");
		}
	}

	private void UpdateHasRoomAnotherFarm()
	{
		lock (this)
		{
			hasRoomAnotherFarm = null;
		}
		Game1.GetHasRoomAnotherFarmAsync(delegate(bool yes)
		{
			lock (this)
			{
				hasRoomAnotherFarm = yes;
			}
		});
	}

	protected void CloseSubMenu()
	{
		if (!subMenu.readyToClose())
		{
			return;
		}
		buttonsDX = -1;
		if (subMenu is AboutMenu || subMenu is LanguageSelectionMenu)
		{
			subMenu = null;
			buttonsDX = 0;
			return;
		}
		isTransitioningButtons = true;
		if (subMenu is LoadGameMenu)
		{
			transitioningFromLoadScreen = true;
		}
		subMenu = null;
		Game1.changeMusicTrack("spring_day_ambient");
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (HasActiveUser && muteMusicButton.containsPoint(x, y))
		{
			startupPreferences.startMuted = Utility.toggleMuteMusic();
			if (muteMusicButton.sourceRect.X == 128)
			{
				muteMusicButton.sourceRect.X = 137;
			}
			else
			{
				muteMusicButton.sourceRect.X = 128;
			}
			Game1.playSound("drumkit6");
			startupPreferences.savePreferences(async: false);
			return;
		}
		if (HasActiveUser && windowedButton.containsPoint(x, y))
		{
			if (!Game1.options.isCurrentlyWindowed())
			{
				Game1.options.setWindowedOption("Windowed");
				windowedButton.sourceRect.X = 146;
				startupPreferences.windowMode = 1;
			}
			else
			{
				Game1.options.setWindowedOption("Windowed Borderless");
				windowedButton.sourceRect.X = 155;
				startupPreferences.windowMode = 0;
			}
			startupPreferences.savePreferences(async: false);
			Game1.playSound("drumkit6");
			return;
		}
		if (logoFadeTimer > 0 && skipButton != null && skipButton.containsPoint(x, y))
		{
			if (logoSurprisedTimer <= 0)
			{
				int value = 1200;
				logoSurprisedTimer = 1500;
				string cueName = "fishSlap";
				Game1.changeMusicTrack("none");
				switch (Game1.random.Next(2))
				{
				case 0:
					cueName = "Duck";
					value = 0;
					break;
				case 1:
					cueName = "fishSlap";
					break;
				}
				if (Game1.random.NextDouble() < 0.02)
				{
					specialSurprised = true;
					Game1.playSound("moss_cut");
					fadeFromWhiteTimer = 3000;
				}
				else
				{
					Game1.playSound(cueName, value);
				}
			}
			else if (logoSurprisedTimer > 1)
			{
				logoSurprisedTimer = Math.Max(1, logoSurprisedTimer - 500);
			}
		}
		if (amuzioTimer > 500)
		{
			amuzioTimer = 500;
		}
		if (logoFadeTimer > 0 || fadeFromWhiteTimer > 0 || transitioningCharacterCreationMenu)
		{
			return;
		}
		if (subMenu != null)
		{
			bool flag = false;
			if (Game1.options.SnappyMenus && subMenu.currentlySnappedComponent != null && subMenu.currentlySnappedComponent.myID != 81114)
			{
				flag = true;
			}
			bool flag2 = false;
			if (subMenu.readyToClose() && backButton != null && backButton.containsPoint(x, y) && !flag)
			{
				backButtonPressed();
				flag2 = true;
			}
			else if (!isTransitioningButtons)
			{
				subMenu.receiveLeftClick(x, y);
			}
			if (flag2 || subMenu == null || !subMenu.readyToClose() || (!(subMenu is TooManyFarmsMenu) && (backButton == null || !backButton.containsPoint(x, y))) || flag)
			{
				return;
			}
			Game1.playSound("bigDeSelect");
			buttonsDX = -1;
			if (subMenu is AboutMenu || subMenu is LanguageSelectionMenu)
			{
				ReturnToMainTitleScreen();
				buttonsDX = 0;
				return;
			}
			isTransitioningButtons = true;
			if (subMenu is LoadGameMenu)
			{
				transitioningFromLoadScreen = true;
			}
			ReturnToMainTitleScreen();
			Game1.changeMusicTrack("spring_day_ambient");
			return;
		}
		if (logoFadeTimer <= 0 && !titleInPosition && logoSwipeTimer == 0f)
		{
			pauseBeforeViewportRiseTimer = 0;
			fadeFromWhiteTimer = 0;
			viewportY = -999f;
			viewportDY = -0.01f;
			birds.Clear();
			logoSwipeTimer = 1f;
			return;
		}
		if (!alternativeTitleGraphic())
		{
			if (clicksOnLeaf >= 10 && Game1.random.NextDouble() < 0.001)
			{
				Game1.playSound("junimoMeep1");
			}
			if (titleInPosition && eRect.Contains(x, y) && clicksOnE < 10)
			{
				clicksOnE++;
				Game1.playSound("woodyStep");
				if (clicksOnE == 10)
				{
					int num = (ShouldShrinkLogo() ? 2 : pixelZoom);
					Game1.playSound("openChest");
					tempSprites.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(0, 491, 42, 68), new Vector2(width / 2 - 200 * num + 251 * num, -300 * num - (int)(viewportY / 3f) * num + 26 * num), flipped: false, 0f, Color.White)
					{
						scale = num,
						animationLength = 9,
						interval = 200f,
						local = true,
						holdLastFrame = true
					});
				}
			}
			else if (titleInPosition)
			{
				bool flag3 = false;
				foreach (Rectangle leafRect in leafRects)
				{
					if (leafRect.Contains(x, y))
					{
						flag3 = true;
						break;
					}
				}
				if (screwRect.Contains(x, y) && clicksOnScrew < 10)
				{
					Game1.playSound("cowboy_monsterhit");
					clicksOnScrew++;
					if (clicksOnScrew == 10)
					{
						showButterflies();
					}
				}
				if (Game1.content.GetCurrentLanguage() != LocalizedContentManager.LanguageCode.zh)
				{
					if (cornerPhaseHolding && (r_hole_rect.Contains(x, y) || r_hole_rect2.Contains(x, y)) && cornerClicks < 999)
					{
						Game1.playSound("coin");
						cornerClickEndTimer = 1000f;
						cornerClickSoundEffectTimer = 400f;
						cornerClicks = 9999;
						showCornerClickEasterEgg = true;
					}
					else if (cornerRect.Contains(x, y) && !cornerPhaseHolding)
					{
						int num2 = (ShouldShrinkLogo() ? 2 : pixelZoom);
						cornerClicks++;
						if (cornerClicks > 5)
						{
							if (!cornerPhaseHolding)
							{
								Game1.playSound("coin");
								cornerClicks = 0;
								cornerPhaseHolding = true;
							}
						}
						else
						{
							Game1.playSound("hammer");
							for (int i = 0; i < 3; i++)
							{
								tempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(280 + Game1.random.Choose(8, 0), 1954, 8, 8), 1000f, 1, 99, new Vector2(width / 2 - 190 * num2, -300 * num2 - (int)(viewportY / 3f) * num2 + 175 * num2), flicker: false, flipped: false, 1f, 0f, Color.White, pixelZoom, 0f, 0f, (float)Game1.random.Next(-10, 11) / 100f)
								{
									motion = new Vector2(Game1.random.Next(-4, 5), -8f + (float)Game1.random.Next(-10, 1) / 100f),
									acceleration = new Vector2(0f, 0.3f),
									local = true,
									delayBeforeAnimationStart = i * 15
								});
							}
						}
					}
				}
				if (flag3)
				{
					clicksOnLeaf++;
					if (clicksOnLeaf == 10)
					{
						int num3 = (ShouldShrinkLogo() ? 2 : pixelZoom);
						Game1.playSound("discoverMineral");
						tempSprites.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(264, 464, 16, 16), new Vector2(width / 2 - 200 * num3 + 80 * num3, -300 * num3 - (int)(viewportY / 3f) * num3 + 10 * num3 + 2), flipped: false, 0f, Color.White)
						{
							scale = num3,
							animationLength = 8,
							interval = 80f,
							totalNumberOfLoops = 999999,
							local = true,
							holdLastFrame = false,
							delayBeforeAnimationStart = 200
						});
						tempSprites.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(136, 448, 16, 16), new Vector2(width / 2 - 200 * num3 + 80 * num3, -300 * num3 - (int)(viewportY / 3f) * num3 + 10 * num3), flipped: false, 0f, Color.White)
						{
							scale = num3,
							animationLength = 8,
							interval = 50f,
							local = true,
							holdLastFrame = false
						});
						tempSprites.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(200, 464, 16, 16), new Vector2(width / 2 - 200 * num3 + 178 * num3, -300 * num3 - (int)(viewportY / 3f) * num3 + 141 * num3 + 2), flipped: false, 0f, Color.White)
						{
							scale = num3,
							animationLength = 4,
							interval = 150f,
							totalNumberOfLoops = 999999,
							local = true,
							holdLastFrame = false,
							delayBeforeAnimationStart = 400
						});
						tempSprites.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(136, 448, 16, 16), new Vector2(width / 2 - 200 * num3 + 178 * num3, -300 * num3 - (int)(viewportY / 3f) * num3 + 141 * num3), flipped: false, 0f, Color.White)
						{
							scale = num3,
							animationLength = 8,
							interval = 50f,
							local = true,
							holdLastFrame = false,
							delayBeforeAnimationStart = 200
						});
						tempSprites.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(136, 464, 16, 16), new Vector2(width / 2 - 200 * num3 + 294 * num3, -300 * num3 - (int)(viewportY / 3f) * num3 + 89 * num3 + 2), flipped: false, 0f, Color.White)
						{
							scale = num3,
							animationLength = 4,
							interval = 150f,
							totalNumberOfLoops = 999999,
							local = true,
							holdLastFrame = false,
							delayBeforeAnimationStart = 600
						});
						tempSprites.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(136, 448, 16, 16), new Vector2(width / 2 - 200 * num3 + 294 * num3, -300 * num3 - (int)(viewportY / 3f) * num3 + 89 * num3), flipped: false, 0f, Color.White)
						{
							scale = num3,
							animationLength = 8,
							interval = 50f,
							local = true,
							holdLastFrame = false,
							delayBeforeAnimationStart = 400
						});
					}
					else
					{
						Game1.playSound("leafrustle");
						int num4 = (ShouldShrinkLogo() ? 2 : pixelZoom);
						for (int j = 0; j < 2; j++)
						{
							tempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(355, 1199 + Game1.random.Next(-1, 2) * 16, 16, 16), new Vector2(x + Game1.random.Next(-8, 9), y + Game1.random.Next(-8, 9)), Game1.random.NextBool(), 0f, Color.White)
							{
								scale = num4,
								animationLength = 11,
								interval = 50 + Game1.random.Next(50),
								totalNumberOfLoops = 999,
								motion = new Vector2((float)Game1.random.Next(-100, 101) / 100f, 1f + (float)Game1.random.Next(-100, 100) / 500f),
								xPeriodic = Game1.random.NextBool(),
								xPeriodicLoopTime = Game1.random.Next(6000, 16000),
								xPeriodicRange = Game1.random.Next(64, 192),
								alphaFade = 0.001f,
								local = true,
								holdLastFrame = false,
								delayBeforeAnimationStart = j * 20
							});
						}
					}
				}
			}
		}
		if (!ShouldAllowInteraction() || !HasActiveUser || (subMenu != null && !subMenu.readyToClose()) || isTransitioningButtons)
		{
			return;
		}
		for (int k = 0; k < buttons.Count; k++)
		{
			ClickableTextureComponent clickableTextureComponent = buttons[k];
			if (clickableTextureComponent.containsPoint(x, y))
			{
				performButtonAction(clickableTextureComponent.name);
			}
		}
		if (aboutButton.containsPoint(x, y))
		{
			subMenu = new AboutMenu();
			Game1.playSound("newArtifact");
		}
		if (languageButton.visible && languageButton.containsPoint(x, y))
		{
			subMenu = new LanguageSelectionMenu();
			Game1.playSound("newArtifact");
		}
	}

	public void performButtonAction(string which)
	{
		whichSubMenu = which;
		switch (which)
		{
		case "New":
			buttonsDX = 1;
			isTransitioningButtons = true;
			Game1.playSound("select");
			foreach (TemporaryAnimatedSprite tempSprite in tempSprites)
			{
				tempSprite.pingPong = false;
			}
			UpdateHasRoomAnotherFarm();
			break;
		case "Co-op":
			buttonsDX = 1;
			isTransitioningButtons = true;
			Game1.playSound("select");
			UpdateHasRoomAnotherFarm();
			break;
		case "Load":
		case "Invite":
			buttonsDX = 1;
			isTransitioningButtons = true;
			Game1.playSound("select");
			break;
		case "Exit":
			Game1.playSound("bigDeSelect");
			Game1.changeMusicTrack("none");
			quitTimer = 500;
			break;
		}
	}

	private void addRightLeafGust()
	{
		if (!isTransitioningButtons && tempSprites.Count <= 0 && !alternativeTitleGraphic())
		{
			int num = (ShouldShrinkLogo() ? 2 : pixelZoom);
			tempSprites.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(296, 187, 27, 21), new Vector2(width / 2 - 200 * num + 327 * num, (float)(-300 * num) - viewportY / 3f * (float)num + (float)(107 * num)), flipped: false, 0f, Color.White)
			{
				scale = num,
				pingPong = true,
				animationLength = 3,
				interval = 100f,
				totalNumberOfLoops = 3,
				local = true
			});
		}
	}

	public bool ShouldShrinkLogo()
	{
		return height <= 850;
	}

	private void addLeftLeafGust()
	{
		if (!isTransitioningButtons && tempSprites.Count <= 0 && !alternativeTitleGraphic())
		{
			int num = (ShouldShrinkLogo() ? 2 : pixelZoom);
			tempSprites.Add(new TemporaryAnimatedSprite("Minigames\\TitleButtons", new Rectangle(296, 208, 22, 18), new Vector2(width / 2 - 200 * num + 16 * num, (float)(-300 * num) - viewportY / 3f * (float)num + (float)(16 * num)), flipped: false, 0f, Color.White)
			{
				scale = num,
				pingPong = true,
				animationLength = 3,
				interval = 100f,
				totalNumberOfLoops = 3,
				local = true
			});
		}
	}

	public void createdNewCharacter(bool skipIntro)
	{
		TitleMenu.OnCreatedNewCharacter?.Invoke();
		Game1.playSound("smallSelect");
		subMenu = null;
		transitioningCharacterCreationMenu = true;
		if (skipIntro)
		{
			Game1.game1.loadForNewGame();
			Game1.saveOnNewDay = true;
			Game1.player.eventsSeen.Add("60367");
			Game1.player.currentLocation = Utility.getHomeOfFarmer(Game1.player);
			Game1.player.Position = new Vector2(9f, 9f) * 64f;
			Game1.player.isInBed.Value = true;
			Game1.NewDay(0f);
			Game1.exitActiveMenu();
			Game1.setGameMode(3);
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		if (Game1.game1.IsMainInstance)
		{
			if (ticksUntilLanguageLoad > 0)
			{
				ticksUntilLanguageLoad--;
			}
			else if (ticksUntilLanguageLoad == 0)
			{
				ticksUntilLanguageLoad--;
				startupPreferences.loadPreferences(async: false, applyLanguage: true);
			}
		}
		if (windowNumber > 0)
		{
			if (startupPreferences.displayIndex >= 0 && !GameRunner.instance.Window.CenterOnDisplay(startupPreferences.displayIndex))
			{
				Game1.log.Error("Error: Couldn't find display with index " + startupPreferences.displayIndex + ". Reverting to windowed mode on display 0.");
				startupPreferences.windowMode = 1;
			}
			Game1.options.setWindowedOption(startupPreferences.windowMode);
			windowNumber = 0;
		}
		if (!Game1.options.isCurrentlyWindowed())
		{
			Vector2 position = new Vector2(Game1.viewport.Width - 36 - 16, 16f);
			position.X = Math.Min(GameRunner.instance.Window.GetDisplayBounds(GameRunner.instance.Window.GetDisplayIndex()).Right - GameRunner.instance.Window.ClientBounds.Left, Game1.viewport.Width) - 36 - 16;
			windowedButton.setPosition(position);
		}
		base.update(time);
		subMenu?.update(time);
		if (transitioningCharacterCreationMenu)
		{
			globalCloudAlpha -= (float)time.ElapsedGameTime.Milliseconds * 0.001f;
			if (globalCloudAlpha <= 0f)
			{
				transitioningCharacterCreationMenu = false;
				globalCloudAlpha = 0f;
				subMenu = null;
				Game1.currentMinigame = new GrandpaStory();
				Game1.exitActiveMenu();
				Game1.setGameMode(3);
			}
		}
		if (quitTimer > 0)
		{
			quitTimer -= time.ElapsedGameTime.Milliseconds;
			if (quitTimer <= 0)
			{
				Game1.quit = true;
				Game1.exitActiveMenu();
			}
		}
		if (amuzioTimer > 0)
		{
			amuzioTimer -= time.ElapsedGameTime.Milliseconds;
		}
		else if (logoFadeTimer > 0)
		{
			if (logoSurprisedTimer > 0)
			{
				logoSurprisedTimer -= time.ElapsedGameTime.Milliseconds;
				if (logoSurprisedTimer <= 0)
				{
					logoFadeTimer = 1;
				}
			}
			else
			{
				int num = logoFadeTimer;
				logoFadeTimer -= time.ElapsedGameTime.Milliseconds;
				if (logoFadeTimer < 4000 && num >= 4000)
				{
					Game1.playSound("mouseClick");
				}
				if (logoFadeTimer < 2500 && num >= 2500)
				{
					Game1.playSound("mouseClick");
				}
				if (logoFadeTimer < 2000 && num >= 2000)
				{
					Game1.playSound("mouseClick");
				}
				if (logoFadeTimer <= 0)
				{
					Game1.changeMusicTrack("MainTheme");
				}
			}
		}
		else if (fadeFromWhiteTimer > 0)
		{
			fadeFromWhiteTimer -= time.ElapsedGameTime.Milliseconds;
			if (fadeFromWhiteTimer <= 0)
			{
				pauseBeforeViewportRiseTimer = 3500;
			}
		}
		else if (pauseBeforeViewportRiseTimer > 0)
		{
			pauseBeforeViewportRiseTimer -= time.ElapsedGameTime.Milliseconds;
			if (pauseBeforeViewportRiseTimer <= 0)
			{
				viewportDY = -0.05f;
			}
		}
		viewportY += viewportDY;
		if (viewportDY < 0f)
		{
			viewportDY -= 0.006f;
		}
		if (viewportY <= -1000f)
		{
			if (viewportDY != 0f)
			{
				logoSwipeTimer = 1000f;
				showButtonsTimer = 200;
			}
			viewportDY = 0f;
		}
		if (logoSwipeTimer > 0f)
		{
			logoSwipeTimer -= time.ElapsedGameTime.Milliseconds;
			if (logoSwipeTimer <= 0f)
			{
				addLeftLeafGust();
				addRightLeafGust();
				titleInPosition = true;
				int num2 = (ShouldShrinkLogo() ? 2 : pixelZoom);
				eRect = new Rectangle(width / 2 - 200 * num2 + 251 * num2, -300 * num2 - (int)(viewportY / 3f) * num2 + 26 * num2, 42 * num2, 68 * num2);
				screwRect = new Rectangle(width / 2 + 150 * num2, -300 * num2 - (int)(viewportY / 3f) * num2 + 80 * num2, 5 * num2, 5 * num2);
				cornerRect = new Rectangle(width / 2 - 200 * num2, -300 * num2 - (int)(viewportY / 3f) * num2 + 165 * num2, 20 * num2, 20 * num2);
				r_hole_rect = new Rectangle(width / 2 - 21 * num2, -300 * num2 - (int)(viewportY / 3f) * num2 + 39 * num2, 10 * num2, 11 * num2);
				r_hole_rect2 = new Rectangle(width / 2 - 35 * num2, -300 * num2 - (int)(viewportY / 3f) * num2 + 24 * num2, 7 * num2, 7 * num2);
				populateLeafRects();
			}
		}
		if (showButtonsTimer > 0 && HasActiveUser && subMenu == null)
		{
			showButtonsTimer -= time.ElapsedGameTime.Milliseconds;
			if (showButtonsTimer <= 0)
			{
				if (buttonsToShow < 4)
				{
					buttonsToShow++;
					Game1.playSound("Cowboy_gunshot");
					showButtonsTimer = 200;
				}
				else if (Game1.options.gamepadControls && Game1.options.snappyMenus)
				{
					populateClickableComponentList();
					snapToDefaultClickableComponent();
				}
			}
		}
		if (titleInPosition && !isTransitioningButtons && globalXOffset == 0 && Game1.random.NextDouble() < 0.005)
		{
			if (Game1.random.NextBool())
			{
				addLeftLeafGust();
			}
			else
			{
				addRightLeafGust();
			}
		}
		if (titleInPosition)
		{
			if (isTransitioningButtons)
			{
				int num3 = buttonsDX * (int)time.ElapsedGameTime.TotalMilliseconds;
				int num4 = globalXOffset + num3;
				int num5 = num4 - width;
				if (num5 > 0)
				{
					num4 -= num5;
					num3 -= num5;
				}
				globalXOffset = num4;
				moveFeatures(num3, 0);
				if (buttonsDX > 0 && globalXOffset >= width)
				{
					if (subMenu != null)
					{
						if (subMenu.readyToClose())
						{
							isTransitioningButtons = false;
							buttonsDX = 0;
						}
					}
					else
					{
						switch (whichSubMenu)
						{
						case "Load":
							subMenu = new LoadGameMenu();
							Game1.changeMusicTrack("title_night");
							buttonsDX = 0;
							isTransitioningButtons = false;
							break;
						case "Co-op":
							if (!hasRoomAnotherFarm.HasValue)
							{
								break;
							}
							buttonsDX = 0;
							isTransitioningButtons = false;
							if (true)
							{
								subMenu = new CoopMenu(!hasRoomAnotherFarm.Value);
								Game1.changeMusicTrack("title_night");
								break;
							}
							Game1.playSound("bigDeSelect");
							if (Game1.options.SnappyMenus)
							{
								setCurrentlySnappedComponentTo(81119);
								snapCursorToCurrentSnappedComponent();
							}
							break;
						case "Invite":
							subMenu = new FarmhandMenu();
							Game1.changeMusicTrack("title_night");
							buttonsDX = 0;
							isTransitioningButtons = false;
							break;
						case "New":
							if (!hasRoomAnotherFarm.HasValue)
							{
								break;
							}
							if (!hasRoomAnotherFarm.Value)
							{
								subMenu = new TooManyFarmsMenu();
								Game1.playSound("newArtifact");
								buttonsDX = 0;
								isTransitioningButtons = false;
								break;
							}
							Game1.resetPlayer();
							subMenu = new CharacterCustomization(CharacterCustomization.Source.NewGame);
							if (startupPreferences.timesPlayed > 1 && !startupPreferences.sawAdvancedCharacterCreationIndicator)
							{
								if (subMenu is CharacterCustomization characterCustomization)
								{
									characterCustomization.showAdvancedCharacterCreationHighlight();
								}
								startupPreferences.sawAdvancedCharacterCreationIndicator = true;
								startupPreferences.savePreferences(async: false);
							}
							Game1.playSound("select");
							Game1.changeMusicTrack("CloudCountry");
							Game1.player.favoriteThing.Value = "";
							buttonsDX = 0;
							isTransitioningButtons = false;
							break;
						}
					}
					if (!isTransitioningButtons)
					{
						whichSubMenu = "";
					}
				}
				else if (buttonsDX < 0 && globalXOffset <= 0)
				{
					globalXOffset = 0;
					isTransitioningButtons = false;
					buttonsDX = 0;
					setUpIcons();
					whichSubMenu = "";
					transitioningFromLoadScreen = false;
				}
			}
			if (cornerClickEndTimer > 0f)
			{
				cornerClickEndTimer -= (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
				if (cornerClickEndTimer <= 0f)
				{
					cornerClickParrotTimer = 400f;
				}
			}
			if (cornerClickSoundEffectTimer > 0f)
			{
				cornerClickSoundEffectTimer -= (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
				if (cornerClickSoundEffectTimer <= 0f)
				{
					Game1.playSound("goldenWalnut");
				}
			}
			if (cornerClickParrotTimer > 0f)
			{
				cornerClickParrotTimer -= (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
				if (cornerClickParrotTimer <= 0f)
				{
					int num6 = (ShouldShrinkLogo() ? 2 : pixelZoom);
					behindSignTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Rectangle(120, 0, 24, 24), 100f, 3, 999, new Vector2(globalXOffset + width / 2 - 200 * num6, (float)(-300 * num6) - viewportY / 3f * (float)num6 + (float)(100 * num6)), flicker: false, flipped: false, 0.2f, 0f, Color.White, num6, 0.01f, 0f, 0f, local: true)
					{
						pingPong = true,
						motion = new Vector2(-6f, -1f),
						acceleration = new Vector2(0.02f, 0.02f)
					});
					behindSignTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Rectangle(120, 48, 24, 24), 95f, 3, 999, new Vector2(globalXOffset + width / 2 - 200 * num6, (float)(-300 * num6) - viewportY / 3f * (float)num6 + (float)(120 * num6)), flicker: false, flipped: false, 0.2f, 0f, Color.White, num6, 0.01f, 0f, 0f, local: true)
					{
						pingPong = true,
						motion = new Vector2(-6f, -1f),
						acceleration = new Vector2(0.02f, 0.02f),
						delayBeforeAnimationStart = 300,
						startSound = "leafrustle"
					});
					behindSignTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Rectangle(120, 24, 24, 24), 100f, 3, 999, new Vector2(globalXOffset + width / 2 - 200 * num6, (float)(-300 * num6) - viewportY / 3f * (float)num6 + (float)(100 * num6)), flicker: false, flipped: false, 0.2f, 0f, Color.White, num6, 0.01f, 0f, 0f, local: true)
					{
						pingPong = true,
						motion = new Vector2(-6f, -1f),
						acceleration = new Vector2(0.02f, 0.02f),
						delayBeforeAnimationStart = 600,
						startSound = "parrot_squawk"
					});
					behindSignTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Rectangle(120, 72, 24, 24), 95f, 3, 999, new Vector2(globalXOffset + width / 2 - 200 * num6, (float)(-300 * num6) - viewportY / 3f * (float)num6 + (float)(120 * num6)), flicker: false, flipped: false, 0.2f, 0f, Color.White, num6, 0.01f, 0f, 0f, local: true)
					{
						pingPong = true,
						motion = new Vector2(-6f, -1f),
						acceleration = new Vector2(0.02f, 0.02f),
						delayBeforeAnimationStart = 1300,
						startSound = "leafrustle"
					});
					behindSignTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Rectangle(120, 0, 24, 24), 100f, 3, 999, new Vector2(globalXOffset + width / 2 + 200 * num6 - 24 * num6, (float)(-300 * num6) - viewportY / 3f * (float)num6 + (float)(100 * num6)), flicker: false, flipped: true, 0.2f, 0f, Color.White, num6, 0.01f, 0f, 0f, local: true)
					{
						pingPong = true,
						motion = new Vector2(6f, -1f),
						acceleration = new Vector2(-0.02f, -0.02f),
						delayBeforeAnimationStart = 600
					});
					behindSignTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Rectangle(120, 48, 24, 24), 95f, 3, 999, new Vector2(globalXOffset + width / 2 + 200 * num6 - 24 * num6, (float)(-300 * num6) - viewportY / 3f * (float)num6 + (float)(120 * num6)), flicker: false, flipped: true, 0.2f, 0f, Color.White, num6, 0.01f, 0f, 0f, local: true)
					{
						pingPong = true,
						motion = new Vector2(6f, -1f),
						acceleration = new Vector2(-0.02f, -0.02f),
						delayBeforeAnimationStart = 900,
						startSound = "leafrustle"
					});
					behindSignTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Rectangle(120, 24, 24, 24), 100f, 3, 999, new Vector2(globalXOffset + width / 2 + 200 * num6 - 24 * num6, (float)(-300 * num6) - viewportY / 3f * (float)num6 + (float)(100 * num6)), flicker: false, flipped: true, 0.2f, 0f, Color.White, num6, 0.01f, 0f, 0f, local: true)
					{
						pingPong = true,
						motion = new Vector2(6f, -1f),
						acceleration = new Vector2(-0.02f, -0.02f),
						delayBeforeAnimationStart = 1200
					});
					behindSignTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Rectangle(120, 72, 24, 24), 95f, 3, 999, new Vector2(globalXOffset + width / 2 + 200 * num6 - 24 * num6, (float)(-300 * num6) - viewportY / 3f * (float)num6 + (float)(120 * num6)), flicker: false, flipped: true, 0.2f, 0f, Color.White, num6, 0.01f, 0f, 0f, local: true)
					{
						pingPong = true,
						motion = new Vector2(6f, -1f),
						acceleration = new Vector2(-0.02f, -0.02f),
						delayBeforeAnimationStart = 1500
					});
					for (int i = 0; i < 14; i++)
					{
						tempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(355, 1199, 16, 16), new Vector2(globalXOffset + width / 2 - 220 * num6, (float)(-300 * num6) - viewportY / 3f * (float)num6 + (float)(60 * num6) + (float)(Game1.random.Next(100) * num6)), Game1.random.NextBool(), 0f, new Color(180, 180, 240))
						{
							scale = num6,
							animationLength = 11,
							interval = 50 + Game1.random.Next(50),
							totalNumberOfLoops = 999,
							motion = new Vector2((float)Game1.random.Next(-100, 101) / 100f, 1f + (float)Game1.random.Next(-100, 100) / 500f),
							xPeriodic = Game1.random.NextBool(),
							xPeriodicLoopTime = Game1.random.Next(6000, 16000),
							xPeriodicRange = Game1.random.Next(64, 192),
							alphaFade = 0.001f,
							local = true,
							holdLastFrame = false,
							delayBeforeAnimationStart = 100 + i * 20
						});
					}
					for (int j = 0; j < 14; j++)
					{
						tempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(355, 1199, 16, 16), new Vector2(globalXOffset + width / 2 + 220 * num6, (float)(-300 * num6) - viewportY / 3f * (float)num6 + (float)(60 * num6) + (float)(Game1.random.Next(100) * num6)), Game1.random.NextBool(), 0f, new Color(180, 180, 240))
						{
							scale = num6,
							animationLength = 11,
							interval = 50 + Game1.random.Next(50),
							totalNumberOfLoops = 999,
							motion = new Vector2((float)Game1.random.Next(-100, 101) / 100f, 1f + (float)Game1.random.Next(-100, 100) / 500f),
							xPeriodic = Game1.random.NextBool(),
							xPeriodicLoopTime = Game1.random.Next(6000, 16000),
							xPeriodicRange = Game1.random.Next(64, 192),
							alphaFade = 0.001f,
							local = true,
							holdLastFrame = false,
							delayBeforeAnimationStart = 900 + j * 20
						});
					}
				}
			}
		}
		for (int num7 = bigClouds.Count - 1; num7 >= 0; num7--)
		{
			bigClouds[num7] -= 0.1f;
			bigClouds[num7] += buttonsDX * time.ElapsedGameTime.Milliseconds / 2;
			if (bigClouds[num7] < (float)(-512 * pixelZoom))
			{
				bigClouds[num7] = width;
			}
		}
		for (int num8 = smallClouds.Count - 1; num8 >= 0; num8--)
		{
			smallClouds[num8] -= 0.3f;
			smallClouds[num8] += buttonsDX * time.ElapsedGameTime.Milliseconds / 2;
			if (smallClouds[num8] < (float)(-149 * pixelZoom))
			{
				smallClouds[num8] = width;
			}
		}
		tempSprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.update(time));
		behindSignTempSprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.update(time));
		birds.RemoveWhere(delegate(TemporaryAnimatedSprite bird)
		{
			bird.position.Y -= viewportDY * 2f;
			return bird.update(time);
		});
	}

	private void moveFeatures(int dx, int dy)
	{
		foreach (TemporaryAnimatedSprite tempSprite in tempSprites)
		{
			tempSprite.position.X += dx;
			tempSprite.position.Y += dy;
		}
		foreach (TemporaryAnimatedSprite behindSignTempSprite in behindSignTempSprites)
		{
			behindSignTempSprite.position.X += dx;
			behindSignTempSprite.position.Y += dy;
		}
		foreach (ClickableTextureComponent button in buttons)
		{
			button.bounds.X += dx;
			button.bounds.Y += dy;
		}
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		if (ShouldAllowInteraction())
		{
			base.receiveScrollWheelAction(direction);
			subMenu?.receiveScrollWheelAction(direction);
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		if (!ShouldAllowInteraction())
		{
			x = int.MinValue;
			y = int.MinValue;
		}
		base.performHoverAction(x, y);
		muteMusicButton.tryHover(x, y);
		if (subMenu != null)
		{
			subMenu.performHoverAction(x, y);
			if (backButton == null || !subMenu.readyToClose())
			{
				return;
			}
			if (backButton.containsPoint(x, y))
			{
				if (backButton.sourceRect.Y == 252)
				{
					Game1.playSound("Cowboy_Footstep");
				}
				backButton.sourceRect.Y = 279;
			}
			else
			{
				backButton.sourceRect.Y = 252;
			}
			backButton.tryHover(x, y, 0.25f);
		}
		else
		{
			if (!titleInPosition || !HasActiveUser)
			{
				return;
			}
			foreach (ClickableTextureComponent button in buttons)
			{
				if (button.containsPoint(x, y))
				{
					if (button.sourceRect.Y == 187)
					{
						Game1.playSound("Cowboy_Footstep");
					}
					button.sourceRect.Y = 245;
				}
				else
				{
					button.sourceRect.Y = 187;
				}
				button.tryHover(x, y, 0.25f);
			}
			aboutButton.tryHover(x, y, 0.25f);
			if (aboutButton.containsPoint(x, y))
			{
				if (aboutButton.sourceRect.X == 8)
				{
					Game1.playSound("Cowboy_Footstep");
				}
				aboutButton.sourceRect.X = 30;
			}
			else
			{
				aboutButton.sourceRect.X = 8;
			}
			if (!languageButton.visible)
			{
				return;
			}
			languageButton.tryHover(x, y, 0.25f);
			if (languageButton.containsPoint(x, y))
			{
				if (languageButton.sourceRect.X == 52)
				{
					Game1.playSound("Cowboy_Footstep");
				}
				languageButton.sourceRect.X = 79;
			}
			else
			{
				languageButton.sourceRect.X = 52;
			}
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		bool flag = subMenu == null || subMenu is AboutMenu || subMenu is LanguageSelectionMenu;
		b.Draw(Game1.staminaRect, new Rectangle(0, 0, width, height), new Color(64, 136, 248));
		b.Draw(Game1.mouseCursors, new Rectangle(0, (int)((float)(-300 * pixelZoom) - viewportY * 0.66f), width, 300 * pixelZoom + height - 120 * pixelZoom), new Rectangle(703, 1912, 1, 264), Color.White);
		if (!whichSubMenu.Equals("Load"))
		{
			for (int i = -10; i < width; i += 638)
			{
				b.Draw(Game1.mouseCursors, new Vector2(i * pixelZoom, (float)(-360 * pixelZoom) - viewportY * 0.66f), new Rectangle(0, 1453, 638, 195), Color.White * (1f - (float)globalXOffset / 1200f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.8f);
			}
		}
		foreach (float bigCloud in bigClouds)
		{
			b.Draw(cloudsTexture, new Vector2(bigCloud, (float)(height - 250 * pixelZoom) - viewportY * 0.5f), new Rectangle(0, 0, 512, 337), Color.White * globalCloudAlpha, 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.01f);
		}
		b.Draw(Game1.mouseCursors, new Vector2(-30 * pixelZoom, (float)(height - 158 * pixelZoom) - viewportY * 0.66f), new Rectangle(0, 886, 639, 148), Color.White, 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.08f);
		b.Draw(Game1.mouseCursors, new Vector2(-30 * pixelZoom + 639 * pixelZoom, (float)(height - 158 * pixelZoom) - viewportY * 0.66f), new Rectangle(0, 886, 640, 148), Color.White, 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.08f);
		for (int j = 0; j < smallClouds.Count; j++)
		{
			b.Draw(cloudsTexture, new Vector2(smallClouds[j], (float)(height - 300 * pixelZoom - j * 12 * pixelZoom) - viewportY * 0.5f), (j % 3 == 0) ? new Rectangle(152, 447, 123, 55) : ((j % 3 == 1) ? new Rectangle(0, 471, 149, 66) : new Rectangle(410, 467, 63, 37)), Color.White * globalCloudAlpha, 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.01f);
		}
		b.Draw(Game1.mouseCursors, new Vector2(0f, (float)(height - 148 * pixelZoom) - viewportY * 1f), new Rectangle(0, 737, 639, 148), Color.White, 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.1f);
		b.Draw(Game1.mouseCursors, new Vector2(639 * pixelZoom, (float)(height - 148 * pixelZoom) - viewportY * 1f), new Rectangle(0, 737, 640, 148), Color.White, 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.1f);
		foreach (TemporaryAnimatedSprite bird in birds)
		{
			bird.draw(b);
		}
		b.Draw(cloudsTexture, new Vector2(0f, (float)(height - 142 * pixelZoom) - viewportY * 2f), new Rectangle(0, 554, 165, 142), Color.White, 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.2f);
		b.Draw(cloudsTexture, new Vector2(width - 122 * pixelZoom, (float)(height - 153 * pixelZoom) - viewportY * 2f), new Rectangle(390, 543, 122, 153), Color.White, 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.2f);
		int num = (ShouldShrinkLogo() ? 2 : pixelZoom);
		if (!whichSubMenu.Equals("Load") && !whichSubMenu.Equals("Co-op") && !(subMenu is LoadGameMenu))
		{
			CharacterCustomization obj = subMenu as CharacterCustomization;
			if ((obj == null || obj.source != CharacterCustomization.Source.HostNewFarm) && !transitioningFromLoadScreen)
			{
				goto IL_073a;
			}
		}
		Texture2D mouseCursors = Game1.mouseCursors;
		Rectangle destinationRectangle = new Rectangle(0, 0, width, height);
		Rectangle value = new Rectangle(702, 1912, 1, 264);
		b.Draw(mouseCursors, destinationRectangle, value, Color.White * ((float)globalXOffset / 1200f));
		SpriteEffects spriteEffects = SpriteEffects.None;
		for (int k = 0; k < height; k += 195)
		{
			for (int l = 0; l < width; l += 638)
			{
				b.Draw(Game1.mouseCursors, new Vector2(l, k) * 4f, new Rectangle(0, 1453, 638, 195), Color.White * ((float)globalXOffset / 1200f), 0f, Vector2.Zero, 4f, spriteEffects, 0.8f);
			}
			spriteEffects = ((spriteEffects == SpriteEffects.None) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
		}
		goto IL_073a;
		IL_073a:
		if (flag)
		{
			foreach (TemporaryAnimatedSprite behindSignTempSprite in behindSignTempSprites)
			{
				behindSignTempSprite.draw(b);
			}
			if (showCornerClickEasterEgg && Game1.content.GetCurrentLanguage() != LocalizedContentManager.LanguageCode.zh)
			{
				float num2 = 1f - Math.Min(1f, 1f - cornerClickEndTimer / 700f);
				float num3 = (float)(40 * num) * num2;
				Vector2 vector = new Vector2(globalXOffset + width / 2 - 200 * num, (float)(-300 * num) - viewportY / 3f * (float)num);
				b.Draw(Game1.mouseCursors2, vector + new Vector2(80 * num, (float)(-10 * num) + num3), new Rectangle(224, 148, 32, 21), Color.White, 0f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				b.Draw(Game1.mouseCursors2, vector + new Vector2(120 * num, (float)(-15 * num) + num3), new Rectangle(224, 148, 32, 21), Color.White, 0f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				b.Draw(Game1.mouseCursors, vector + new Vector2(160 * num, (float)(-25 * num) + num3), new Rectangle(646, 895, 55, 48), Color.White, 0f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				b.Draw(Game1.mouseCursors2, vector + new Vector2(220 * num, (float)(-15 * num) + num3), new Rectangle(224, 148, 32, 21), Color.White, 0f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				b.Draw(Game1.mouseCursors2, vector + new Vector2(260 * num, (float)(-5 * num) + num3), new Rectangle(224, 148, 32, 21), Color.White, 0f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				float num4 = (float)(40 * num) * num2;
				b.Draw(Game1.mouseCursors2, vector + new Vector2((float)(-10 * num) + num4, 70 * num), new Rectangle(224, 148, 32, 21), Color.White, -(float)Math.PI / 2f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				b.Draw(Game1.mouseCursors2, vector + new Vector2((float)(-5 * num) + num4, 100 * num), new Rectangle(224, 148, 32, 21), Color.White, -(float)Math.PI / 2f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				b.Draw(Game1.mouseCursors2, vector + new Vector2((float)(-12 * num) + num4, 130 * num), new Rectangle(224, 148, 32, 21), Color.White, -(float)Math.PI / 2f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				b.Draw(Game1.mouseCursors2, vector + new Vector2((float)(-10 * num) + num4, 160 * num), new Rectangle(224, 148, 32, 21), Color.White, -(float)Math.PI / 2f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				num4 = (float)(-40 * num) * num2;
				b.Draw(Game1.mouseCursors2, vector + new Vector2((float)(410 * num) + num4, 40 * num), new Rectangle(224, 148, 32, 21), Color.White, (float)Math.PI / 2f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				b.Draw(Game1.mouseCursors2, vector + new Vector2((float)(415 * num) + num4, 70 * num), new Rectangle(224, 148, 32, 21), Color.White, (float)Math.PI / 2f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				b.Draw(Game1.mouseCursors2, vector + new Vector2((float)(405 * num) + num4, 100 * num), new Rectangle(224, 148, 32, 21), Color.White, (float)Math.PI / 2f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
				b.Draw(Game1.mouseCursors2, vector + new Vector2((float)(410 * num) + num4, 130 * num), new Rectangle(224, 148, 32, 21), Color.White, (float)Math.PI / 2f, Vector2.Zero, num, SpriteEffects.None, 0.01f);
			}
			b.Draw(titleButtonsTexture, new Vector2(globalXOffset + width / 2 - 200 * num, (float)(-300 * num) - viewportY / 3f * (float)num), new Rectangle(0, 0, 400, 187), Color.White, 0f, Vector2.Zero, num, SpriteEffects.None, 0.2f);
			if (logoSwipeTimer > 0f)
			{
				b.Draw(titleButtonsTexture, new Vector2(globalXOffset + width / 2, (float)(-300 * num) - viewportY / 3f * (float)num + (float)(93 * num)), new Rectangle(0, 0, 400, 187), Color.White, 0f, new Vector2(200f, 93f), (float)num + (0.5f - Math.Abs(logoSwipeTimer / 1000f - 0.5f)) * 0.1f, SpriteEffects.None, 0.2f);
			}
			if (cornerPhaseHolding && cornerClicks > 999 && Game1.content.GetCurrentLanguage() != LocalizedContentManager.LanguageCode.zh)
			{
				b.Draw(Game1.mouseCursors2, new Vector2(globalXOffset + r_hole_rect.X + num, r_hole_rect.Y - 2), new Rectangle(131, 196, 9, 10), Color.White, 0f, Vector2.Zero, num, SpriteEffects.None, 0.24f);
			}
		}
		if (flag)
		{
			bool flag2 = subMenu is AboutMenu || subMenu is LanguageSelectionMenu;
			for (int m = 0; m < buttonsToShow; m++)
			{
				if (buttons.Count > m)
				{
					buttons[m].draw(b, (subMenu == null || !flag2) ? Color.White : (Color.LightGray * 0.8f), 1f);
				}
			}
			if (subMenu == null)
			{
				foreach (TemporaryAnimatedSprite tempSprite in tempSprites)
				{
					tempSprite.draw(b);
				}
			}
		}
		if (subMenu != null && !isTransitioningButtons)
		{
			if (backButton != null && subMenu.readyToClose())
			{
				backButton.draw(b);
			}
			subMenu.draw(b);
			if (backButton != null && !(subMenu is CharacterCustomization) && subMenu.readyToClose())
			{
				backButton.draw(b);
			}
		}
		else if (subMenu == null && isTransitioningButtons && (whichSubMenu.Equals("Load") || whichSubMenu.Equals("New")))
		{
			int x = 84;
			int y = Game1.uiViewport.Height - 64;
			int num5 = 0;
			int num6 = 64;
			Utility.makeSafe(ref x, ref y, num5, num6);
			SpriteText.drawStringWithScrollBackground(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3689"), x, y);
		}
		else if (subMenu == null && !isTransitioningButtons && titleInPosition && !transitioningCharacterCreationMenu && HasActiveUser && flag)
		{
			aboutButton.draw(b);
			languageButton.draw(b);
		}
		if (amuzioTimer > 0)
		{
			b.Draw(Game1.staminaRect, new Rectangle(0, 0, width, height), Color.White);
			Vector2 position = new Vector2(width / 2 - amuzioTexture.Width / 2 * 4, height / 2 - amuzioTexture.Height / 2 * 4);
			position.X = MathHelper.Lerp(position.X, -amuzioTexture.Width * 4, (float)Math.Max(0, amuzioTimer - 3750) / 250f);
			b.Draw(amuzioTexture, position, null, Color.White * Math.Min(1f, (float)amuzioTimer / 500f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.2f);
		}
		else if (logoFadeTimer > 0 || fadeFromWhiteTimer > 0)
		{
			b.Draw(Game1.staminaRect, new Rectangle(0, 0, width, height), Color.White * ((float)fadeFromWhiteTimer / 2000f));
			if (!specialSurprised)
			{
				b.Draw(titleButtonsTexture, new Vector2(width / 2, height / 2 - 30 * pixelZoom), new Rectangle(171 + ((logoFadeTimer / 100 % 2 == 0 && logoSurprisedTimer <= 0) ? 111 : 0), 311, 111, 60), Color.White * ((logoFadeTimer < 500) ? ((float)logoFadeTimer / 500f) : ((logoFadeTimer > 4500) ? (1f - (float)(logoFadeTimer - 4500) / 500f) : 1f)), 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.2f);
			}
			if (logoSurprisedTimer <= 0)
			{
				b.Draw(titleButtonsTexture, new Vector2(width / 2 - 87 * pixelZoom, height / 2 - 34 * pixelZoom), new Rectangle((logoFadeTimer / 100 % 2 == 0) ? 85 : 0, 306 + (shades ? 69 : 0), 85, 69), Color.White * ((logoFadeTimer < 500) ? ((float)logoFadeTimer / 500f) : ((logoFadeTimer > 4500) ? (1f - (float)(logoFadeTimer - 4500) / 500f) : 1f)), 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.2f);
			}
			if (specialSurprised)
			{
				if (logoFadeTimer > 0)
				{
					b.Draw(Game1.staminaRect, new Rectangle(0, 0, width, height), new Color(221, 255, 198));
				}
				b.Draw(Game1.staminaRect, new Rectangle(0, 0, width, height), new Color(221, 255, 198) * ((float)fadeFromWhiteTimer / 2000f));
				int num7 = (int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
				for (int n = 64; n < width + 1000; n += 192)
				{
					for (int num8 = -1000; num8 < height; num8 += 192)
					{
						b.Draw(Game1.mouseCursors, new Vector2(n, num8) + new Vector2((float)(-num7) / 20f, (float)num7 / 20f), new Rectangle(355 + (num7 + n * 77 + num8 * 77) / 12 % 110 / 11 * 16, 1200, 16, 16), Color.White * 0.66f * ((float)(fadeFromWhiteTimer - (2000 - fadeFromWhiteTimer)) / 2000f), 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.18f);
					}
				}
				b.Draw(titleButtonsTexture, new Vector2(width / 2, height / 2 - 30 * pixelZoom), new Rectangle(171 + ((num7 / 200 % 2 == 0) ? 111 : 0), 563, 111, 60), Color.White * ((float)(fadeFromWhiteTimer - (2000 - fadeFromWhiteTimer)) / 2000f), 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.2f);
				specialSurprisedTimeStamp += (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
				Utility.drawWithShadow(b, titleButtonsTexture, new Vector2(width / 2 - 87 * pixelZoom, height / 2 - 34 * pixelZoom), new Rectangle((num7 / 200 % 2 == 0) ? 85 : 0, 559, 85, 69), Color.White * ((float)(fadeFromWhiteTimer - (2000 - fadeFromWhiteTimer)) / 2000f), 0f, Vector2.Zero, pixelZoom, flipped: false, 0.2f, -4, -4, 0f);
			}
			else if (logoSurprisedTimer > 0)
			{
				b.Draw(titleButtonsTexture, new Vector2(width / 2 - 87 * pixelZoom, height / 2 - 34 * pixelZoom), new Rectangle((logoSurprisedTimer > 800 || logoSurprisedTimer < 400) ? 176 : 260, 375, 85, 69), Color.White * ((logoSurprisedTimer < 200) ? ((float)logoSurprisedTimer / 200f) : 1f), 0f, Vector2.Zero, pixelZoom, SpriteEffects.None, 0.22f);
			}
			if (startupMessage.Length > 0 && logoFadeTimer > 0)
			{
				b.DrawString(Game1.smallFont, Game1.parseText(startupMessage, Game1.smallFont, 640), new Vector2(8f, (float)Game1.uiViewport.Height - Game1.smallFont.MeasureString(Game1.parseText(startupMessage, Game1.smallFont, 640)).Y - 4f), startupMessageColor * ((logoFadeTimer < 500) ? ((float)logoFadeTimer / 500f) : ((logoFadeTimer > 4500) ? (1f - (float)(logoFadeTimer - 4500) / 500f) : 1f)));
			}
		}
		if (quitTimer > 0)
		{
			b.Draw(Game1.staminaRect, new Rectangle(0, 0, width, height), Color.Black * (1f - (float)quitTimer / 500f));
		}
		if (HasActiveUser)
		{
			muteMusicButton.draw(b);
			windowedButton.draw(b);
		}
		if (ShouldDrawCursor())
		{
			int cursor = -1;
			if (subMenu is LoadGameMenu)
			{
				cursor = ((subMenu as LoadGameMenu).IsDoingTask() ? 1 : (-1));
			}
			drawMouse(b, ignore_transparency: false, cursor);
			if (cornerPhaseHolding && cornerClicks < 100)
			{
				b.Draw(Game1.mouseCursors2, new Vector2(Game1.getMouseX() + 32 + 4, Game1.getMouseY() + 32 + 4), new Rectangle(131, 196, 9, 10), Color.White, 0f, Vector2.Zero, num, SpriteEffects.None, 0.9999f);
			}
		}
	}

	protected bool ShouldAllowInteraction()
	{
		if (quitTimer > 0)
		{
			return false;
		}
		if (isTransitioningButtons)
		{
			return false;
		}
		if (showButtonsTimer > 0 && HasActiveUser && subMenu == null)
		{
			return false;
		}
		if (subMenu != null)
		{
			if (subMenu is LoadGameMenu loadGameMenu && loadGameMenu.IsDoingTask())
			{
				return false;
			}
		}
		else if (!titleInPosition)
		{
			return false;
		}
		return true;
	}

	protected bool ShouldDrawCursor()
	{
		if (!Game1.options.gamepadControls || !Game1.options.snappyMenus)
		{
			return true;
		}
		if (pauseBeforeViewportRiseTimer > 0)
		{
			return false;
		}
		if (logoSwipeTimer > 0f)
		{
			return false;
		}
		if (logoFadeTimer > 0)
		{
			if (_movedCursor)
			{
				return true;
			}
			return false;
		}
		if (fadeFromWhiteTimer > 0)
		{
			return false;
		}
		if (!titleInPosition)
		{
			return false;
		}
		if (viewportDY != 0f)
		{
			return false;
		}
		if (_subMenu is TooManyFarmsMenu)
		{
			return false;
		}
		if (!ShouldAllowInteraction())
		{
			return false;
		}
		return true;
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		if (globalXOffset >= width)
		{
			globalXOffset = Game1.uiViewport.Width;
		}
		width = Game1.uiViewport.Width;
		height = Game1.uiViewport.Height;
		setUpIcons();
		subMenu?.gameWindowSizeChanged(oldBounds, newBounds);
		backButton = new ClickableTextureComponent(menuContent.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11739"), new Rectangle(width + -66 * pixelZoom - 8 * pixelZoom * 2, height - 27 * pixelZoom - 8 * pixelZoom, 66 * pixelZoom, 27 * pixelZoom), null, "", titleButtonsTexture, new Rectangle(296, 252, 66, 27), pixelZoom)
		{
			myID = 81114
		};
		tempSprites.Clear();
		if (birds.Count > 0 && !titleInPosition)
		{
			for (int i = 0; i < birds.Count; i++)
			{
				birds[i].position = ((i % 2 == 0) ? new Vector2(width - 70 * pixelZoom, height - 120 * pixelZoom) : new Vector2(width - 40 * pixelZoom, height - 110 * pixelZoom));
			}
		}
		windowedButton = new ClickableTextureComponent(new Rectangle(Game1.viewport.Width - 36 - 16, 16, 36, 36), Game1.mouseCursors, new Rectangle((Game1.options != null && !Game1.options.isCurrentlyWindowed()) ? 155 : 146, 384, 9, 9), 4f)
		{
			myID = 81112,
			leftNeighborID = 81111,
			downNeighborID = 81113
		};
		if (Game1.options.SnappyMenus)
		{
			int id = ((currentlySnappedComponent != null) ? currentlySnappedComponent.myID : 81115);
			populateClickableComponentList();
			currentlySnappedComponent = getComponentWithID(id);
			if (_subMenu != null)
			{
				_subMenu.snapCursorToCurrentSnappedComponent();
			}
			else
			{
				snapCursorToCurrentSnappedComponent();
			}
		}
	}

	private void showButterflies()
	{
		Game1.playSound("yoba");
		int num = (ShouldShrinkLogo() ? 2 : pixelZoom);
		tempSprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Rectangle(128, 96, 16, 16), new Vector2(width / 2 - 240 * num, -300 * num - (int)(viewportY / 3f) * num + 86 * num), flipped: false, 0f, Color.White)
		{
			scale = num,
			animationLength = 4,
			totalNumberOfLoops = 999999,
			pingPong = true,
			interval = 75f,
			local = true,
			yPeriodic = true,
			yPeriodicLoopTime = 3200f,
			yPeriodicRange = 16f,
			xPeriodic = true,
			xPeriodicLoopTime = 5000f,
			xPeriodicRange = 21f,
			alpha = 0.001f,
			alphaFade = -0.03f
		});
		TemporaryAnimatedSpriteList temporaryAnimatedSpriteList = Utility.sparkleWithinArea(new Rectangle(width / 2 - 240 * num - 8 * num, -300 * num - (int)(viewportY / 3f) * num + 86 * num - 8 * num, 80, 64), 2, Color.White * 0.75f);
		foreach (TemporaryAnimatedSprite item in temporaryAnimatedSpriteList)
		{
			item.local = true;
			item.scale = (float)num / 4f;
		}
		tempSprites.AddRange(temporaryAnimatedSpriteList);
		tempSprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Rectangle(192, 96, 16, 16), new Vector2(width / 2 + 220 * num, -300 * num - (int)(viewportY / 3f) * num + 15 * num), flipped: false, 0f, Color.White)
		{
			scale = num,
			animationLength = 4,
			totalNumberOfLoops = 999999,
			pingPong = true,
			delayBeforeAnimationStart = 10,
			interval = 70f,
			local = true,
			yPeriodic = true,
			yPeriodicLoopTime = 2800f,
			yPeriodicRange = 12f,
			xPeriodic = true,
			xPeriodicLoopTime = 4000f,
			xPeriodicRange = 16f,
			alpha = 0.001f,
			alphaFade = -0.03f
		});
		temporaryAnimatedSpriteList = Utility.sparkleWithinArea(new Rectangle(width / 2 + 220 * num - 8 * num, -300 * num - (int)(viewportY / 3f) * num + 15 * num - 8 * num, 80, 64), 2, Color.White * 0.75f);
		foreach (TemporaryAnimatedSprite item2 in temporaryAnimatedSpriteList)
		{
			item2.local = true;
			item2.scale = (float)num / 4f;
		}
		tempSprites.AddRange(temporaryAnimatedSpriteList);
		tempSprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Rectangle(256, 96, 16, 16), new Vector2(width / 2 - 250 * num, -300 * num - (int)(viewportY / 3f) * num + 35 * num), flipped: false, 0f, Color.White)
		{
			scale = num,
			animationLength = 4,
			totalNumberOfLoops = 999999,
			pingPong = true,
			delayBeforeAnimationStart = 20,
			interval = 65f,
			local = true,
			yPeriodic = true,
			yPeriodicLoopTime = 3500f,
			yPeriodicRange = 16f,
			xPeriodic = true,
			xPeriodicLoopTime = 3000f,
			xPeriodicRange = 10f,
			alpha = 0.001f,
			alphaFade = -0.03f
		});
		temporaryAnimatedSpriteList = Utility.sparkleWithinArea(new Rectangle(width / 2 - 250 * num - 8 * num, -300 * num - (int)(viewportY / 3f) * num + 35 * num - 8 * num, 80, 64), 2, Color.White * 0.75f);
		foreach (TemporaryAnimatedSprite item3 in temporaryAnimatedSpriteList)
		{
			item3.local = true;
			item3.scale = (float)num / 4f;
		}
		tempSprites.AddRange(temporaryAnimatedSpriteList);
		tempSprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Rectangle(256, 112, 16, 16), new Vector2(width / 2 + 250 * num, -300 * num - (int)(viewportY / 3f) * num + 60 * num), flipped: false, 0f, Color.White)
		{
			scale = num,
			animationLength = 4,
			totalNumberOfLoops = 999999,
			yPeriodic = true,
			yPeriodicLoopTime = 3000f,
			yPeriodicRange = 16f,
			pingPong = true,
			delayBeforeAnimationStart = 30,
			interval = 85f,
			local = true,
			xPeriodic = true,
			xPeriodicLoopTime = 5000f,
			xPeriodicRange = 16f,
			alpha = 0.001f,
			alphaFade = -0.03f
		});
		temporaryAnimatedSpriteList = Utility.sparkleWithinArea(new Rectangle(width / 2 + 250 * num - 8 * num, -300 * num - (int)(viewportY / 3f) * num + 60 * num - 8 * num, 80, 64), 2, Color.White * 0.75f);
		foreach (TemporaryAnimatedSprite item4 in temporaryAnimatedSpriteList)
		{
			item4.local = true;
			item4.scale = (float)num / 4f;
		}
		tempSprites.AddRange(temporaryAnimatedSpriteList);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposedValue)
		{
			return;
		}
		if (disposing)
		{
			tempSprites?.Clear();
			if (menuContent != null)
			{
				menuContent.Dispose();
				menuContent = null;
			}
			LocalizedContentManager.OnLanguageChange -= OnLanguageChange;
			subMenu = null;
		}
		disposedValue = true;
	}

	~TitleMenu()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
