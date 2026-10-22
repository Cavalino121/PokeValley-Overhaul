using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using SkiaSharp;
using StardewValley.Audio;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Delegates;
using StardewValley.Enchantments;
using StardewValley.Events;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Characters;
using StardewValley.GameData.Crops;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.FloorsAndPaths;
using StardewValley.GameData.FruitTrees;
using StardewValley.GameData.LocationContexts;
using StardewValley.GameData.Locations;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Pants;
using StardewValley.GameData.Pets;
using StardewValley.GameData.Shirts;
using StardewValley.GameData.Tools;
using StardewValley.GameData.Weapons;
using StardewValley.Hashing;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Logging;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Mods;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Network.Dedicated;
using StardewValley.Network.NetReady;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.Projectiles;
using StardewValley.Quests;
using StardewValley.SDKs.Steam;
using StardewValley.SaveMigrations;
using StardewValley.SaveSerialization;
using StardewValley.SpecialOrders;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using StardewValley.Triggers;
using StardewValley.Util;
using xTile.Dimensions;
using xTile.Display;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley;

/// <summary>
/// This is the main type for your game
/// </summary>
[InstanceStatics]
public class Game1 : InstanceGame
{
	public enum BundleType
	{
		Default,
		Remixed
	}

	public enum MineChestType
	{
		Default,
		Remixed
	}

	public delegate void afterFadeFunction();

	public const bool IncrementalLoadEnabled = false;

	public const int defaultResolutionX = 1280;

	public const int defaultResolutionY = 720;

	public const int pixelZoom = 4;

	public const int tileSize = 64;

	public const int smallestTileSize = 16;

	public const int up = 0;

	public const int right = 1;

	public const int down = 2;

	public const int left = 3;

	public const int dialogueBoxTileHeight = 5;

	public static int realMilliSecondsPerGameMinute;

	public static int realMilliSecondsPerGameTenMinutes;

	public const int rainDensity = 70;

	public const int rainLoopLength = 70;

	/// <summary>For <see cref="F:StardewValley.Game1.mouseCursor" />, a value indicating the cursor should be hidden.</summary>
	public static readonly int cursor_none;

	/// <summary>For <see cref="F:StardewValley.Game1.mouseCursor" />, a default pointer icon.</summary>
	public static readonly int cursor_default;

	/// <summary>For <see cref="F:StardewValley.Game1.mouseCursor" />, a wait icon.</summary>
	public static readonly int cursor_wait;

	/// <summary>For <see cref="F:StardewValley.Game1.mouseCursor" />, a hand icon indicating that an item can be picked up.</summary>
	public static readonly int cursor_grab;

	/// <summary>For <see cref="F:StardewValley.Game1.mouseCursor" />, a gift box icon indicating that an NPC on this tile can accept a gift.</summary>
	public static readonly int cursor_gift;

	/// <summary>For <see cref="F:StardewValley.Game1.mouseCursor" />, a speech bubble icon indicating that an NPC can be talked to.</summary>
	public static readonly int cursor_talk;

	/// <summary>For <see cref="F:StardewValley.Game1.mouseCursor" />, a magnifying glass icon indicating that something can be examined.</summary>
	public static readonly int cursor_look;

	/// <summary>For <see cref="F:StardewValley.Game1.mouseCursor" />, an icon indicating that something can be harvested.</summary>
	public static readonly int cursor_harvest;

	/// <summary>For <see cref="F:StardewValley.Game1.mouseCursor" />, a pointer icon used when hovering elements with gamepad controls.</summary>
	public static readonly int cursor_gamepad_pointer;

	public static readonly string asianSpacingRegexString;

	public const int legacy_weather_sunny = 0;

	public const int legacy_weather_rain = 1;

	public const int legacy_weather_debris = 2;

	public const int legacy_weather_lightning = 3;

	public const int legacy_weather_festival = 4;

	public const int legacy_weather_snow = 5;

	public const int legacy_weather_wedding = 6;

	public const string weather_sunny = "Sun";

	public const string weather_rain = "Rain";

	public const string weather_green_rain = "GreenRain";

	public const string weather_debris = "Wind";

	public const string weather_lightning = "Storm";

	public const string weather_festival = "Festival";

	public const string weather_snow = "Snow";

	public const string weather_wedding = "Wedding";

	/// <summary>The builder name for Robin's carpenter shop.</summary>
	public const string builder_robin = "Robin";

	/// <summary>The builder name for Wizard's magical construction shop.</summary>
	public const string builder_wizard = "Wizard";

	/// <summary>The shop ID for the Adventurer's Guild shop.</summary>
	public const string shop_adventurersGuild = "AdventureShop";

	/// <summary>The shop ID for the Adventurer's Guild item recovery shop.</summary>
	public const string shop_adventurersGuildItemRecovery = "AdventureGuildRecovery";

	/// <summary>The shop ID for Marnie's animal supply shop.</summary>
	public const string shop_animalSupplies = "AnimalShop";

	/// <summary>The shop ID for Clint's blacksmithery.</summary>
	public const string shop_blacksmith = "Blacksmith";

	/// <summary>The shop ID for Clint's tool upgrade shop.</summary>
	public const string shop_blacksmithUpgrades = "ClintUpgrade";

	/// <summary>The shop ID for the movie theater box office.</summary>
	public const string shop_boxOffice = "BoxOffice";

	/// <summary>The 'shop' ID for the floorpaper/wallpaper catalogue.</summary>
	public const string shop_catalogue = "Catalogue";

	/// <summary>The shop ID for Robin's carpenter supplies.</summary>
	public const string shop_carpenter = "Carpenter";

	/// <summary>The shop ID for the casino club shop.</summary>
	public const string shop_casino = "Casino";

	/// <summary>The shop ID for the desert trader.</summary>
	public const string shop_desertTrader = "DesertTrade";

	/// <summary>The shop ID for Dwarf's shop.</summary>
	public const string shop_dwarf = "Dwarf";

	/// <summary>The shop ID for Willy's fish shop.</summary>
	public const string shop_fish = "FishShop";

	/// <summary>The 'shop' ID for the furniture catalogue.</summary>
	public const string shop_furnitureCatalogue = "Furniture Catalogue";

	/// <summary>The shop ID for Pierre's General Store.</summary>
	public const string shop_generalStore = "SeedShop";

	/// <summary>The shop ID for the Hat Mouse shop.</summary>
	public const string shop_hatMouse = "HatMouse";

	/// <summary>The shop ID for Harvey's clinic.</summary>
	public const string shop_hospital = "Hospital";

	/// <summary>The shop ID for the ice-cream stand.</summary>
	public const string shop_iceCreamStand = "IceCreamStand";

	/// <summary>The shop ID for the island trader.</summary>
	public const string shop_islandTrader = "IslandTrade";

	/// <summary>The shop ID for Joja Mart.</summary>
	public const string shop_jojaMart = "Joja";

	/// <summary>The shop ID for Krobus' shop.</summary>
	public const string shop_krobus = "ShadowShop";

	/// <summary>The shop ID for Qi's gem shop.</summary>
	public const string shop_qiGemShop = "QiGemShop";

	/// <summary>The shop ID for the Ginger Island resort bar.</summary>
	public const string shop_resortBar = "ResortBar";

	/// <summary>The shop ID for Sandy's Oasis.</summary>
	public const string shop_sandy = "Sandy";

	/// <summary>The shop ID for the Stardrop Saloon.</summary>
	public const string shop_saloon = "Saloon";

	/// <summary>The shop ID for the traveling cart shop.</summary>
	public const string shop_travelingCart = "Traveler";

	/// <summary>The shop ID for the Volcano Dungeon shop.</summary>
	public const string shop_volcanoShop = "VolcanoShop";

	/// <summary>The shop ID for the bookseller.</summary>
	public const string shop_bookseller = "Bookseller";

	/// <summary>The shop ID for the bookseller trade-ins.</summary>
	public const string shop_bookseller_trade = "BooksellerTrade";

	/// <summary>The 'shop' ID for the joja furniture catalogue.</summary>
	public const string shop_jojaCatalogue = "JojaFurnitureCatalogue";

	/// <summary>The 'shop' ID for the wizard furniture catalogue.</summary>
	public const string shop_wizardCatalogue = "WizardFurnitureCatalogue";

	/// <summary>The 'shop' ID for the wizard furniture catalogue.</summary>
	public const string shop_junimoCatalogue = "JunimoFurnitureCatalogue";

	/// <summary>The 'shop' ID for the wizard furniture catalogue.</summary>
	public const string shop_retroCatalogue = "RetroFurnitureCatalogue";

	/// <summary>The 'shop' ID for the wizard furniture catalogue.</summary>
	public const string shop_trashCatalogue = "TrashFurnitureCatalogue";

	/// <summary>The shop ID for Marnie's pet adoption shop.</summary>
	public const string shop_petAdoption = "PetAdoption";

	public const byte singlePlayer = 0;

	public const byte multiplayerClient = 1;

	public const byte multiplayerServer = 2;

	public const byte logoScreenGameMode = 4;

	public const byte titleScreenGameMode = 0;

	public const byte loadScreenGameMode = 1;

	public const byte newGameMode = 2;

	public const byte playingGameMode = 3;

	public const byte loadingMode = 6;

	public const byte saveMode = 7;

	public const byte saveCompleteMode = 8;

	public const byte selectGameScreen = 9;

	public const byte creditsMode = 10;

	public const byte errorLogMode = 11;

	/// <summary>The name of the game's main assembly.</summary>
	public static readonly string GameAssemblyName;

	/// <summary>The semantic game version, like <c>1.6.0</c>.</summary>
	/// <remarks>
	///   <para>
	///     This mostly follows semantic versioning format with three or four numbers (without leading zeros), so
	///     1.6.7 comes before 1.6.10. The first three numbers are consistent across all platforms, while some
	///     platforms may add a fourth number for the port version. This doesn't include tags like <c>-alpha</c>
	///     or <c>-beta</c>; see <see cref="F:StardewValley.Game1.versionLabel" /> or <see cref="M:StardewValley.Game1.GetVersionString" /> for that.
	///   </para>
	///
	///   <para>Game versions can be compared using <see cref="M:StardewValley.Utility.CompareGameVersions(System.String,System.String,System.Boolean)" />.</para>
	/// </remarks>
	public static readonly string version;

	/// <summary>A human-readable label for the update, like 'modding update' or 'hotfix #3', if any.</summary>
	public static readonly string versionLabel;

	/// <summary>The game build number used to distinguish different builds with the same version number, like <c>26055</c>.</summary>
	/// <remarks>This value is platform-dependent.</remarks>
	public static readonly int versionBuildNumber;

	public const float keyPollingThreshold = 650f;

	public const float toolHoldPerPowerupLevel = 600f;

	public const float startingMusicVolume = 1f;

	/// <summary>
	/// ContentManager specifically for loading xTile.Map(s).
	/// Will be unloaded when returning to title.
	/// </summary>
	public LocalizedContentManager xTileContent;

	public static DelayedAction morningSongPlayAction;

	private static LocalizedContentManager _temporaryContent;

	[NonInstancedStatic]
	private static bool FinishedIncrementalLoad;

	[NonInstancedStatic]
	private static bool FinishedFirstLoadContent;

	[NonInstancedStatic]
	private static volatile bool FinishedFirstInitSounds;

	[NonInstancedStatic]
	private static volatile bool FinishedFirstInitSerializers;

	[NonInstancedStatic]
	private static IEnumerator<int> LoadContentEnumerator;

	[NonInstancedStatic]
	public static GraphicsDeviceManager graphics;

	[NonInstancedStatic]
	public static LocalizedContentManager content;

	public static SpriteBatch spriteBatch;

	public static float MusicDuckTimer;

	public static GamePadState oldPadState;

	public static float thumbStickSensitivity;

	public static float runThreshold;

	public static int rightStickHoldTime;

	public static int emoteMenuShowTime;

	public static int nextFarmerWarpOffsetX;

	public static int nextFarmerWarpOffsetY;

	public static KeyboardState oldKBState;

	public static MouseState oldMouseState;

	[NonInstancedStatic]
	public static Game1 keyboardFocusInstance;

	private static Farmer _player;

	public static NetFarmerRoot serverHost;

	protected static bool _isWarping;

	[NonInstancedStatic]
	public static bool hasLocalClientsOnly;

	protected bool _instanceIsPlayingBackgroundMusic;

	protected bool _instanceIsPlayingOutdoorsAmbience;

	protected bool _instanceIsPlayingNightAmbience;

	protected bool _instanceIsPlayingTownMusic;

	protected bool _instanceIsPlayingMorningSong;

	public static bool isUsingBackToFrontSorting;

	protected static StringBuilder _debugStringBuilder;

	[NonInstancedStatic]
	internal static readonly DebugTimings debugTimings;

	public static Dictionary<string, GameLocation> _locationLookup;

	public IList<GameLocation> _locations = new List<GameLocation>();

	public static Regex asianSpacingRegex;

	public static Viewport defaultDeviceViewport;

	public static LocationRequest locationRequest;

	public static bool warpingForForcedRemoteEvent;

	protected static GameLocation _PreviousNonNullLocation;

	public GameLocation instanceGameLocation;

	public static IDisplayDevice mapDisplayDevice;

	public static xTile.Dimensions.Rectangle viewport;

	public static xTile.Dimensions.Rectangle uiViewport;

	public static Texture2D objectSpriteSheet;

	public static Texture2D cropSpriteSheet;

	public static Texture2D emoteSpriteSheet;

	public static Texture2D debrisSpriteSheet;

	public static Texture2D rainTexture;

	public static Texture2D bigCraftableSpriteSheet;

	public static Texture2D buffsIcons;

	public static Texture2D daybg;

	public static Texture2D nightbg;

	public static Texture2D menuTexture;

	public static Texture2D uncoloredMenuTexture;

	public static Texture2D lantern;

	public static Texture2D windowLight;

	public static Texture2D sconceLight;

	public static Texture2D cauldronLight;

	public static Texture2D shadowTexture;

	public static Texture2D mouseCursors;

	public static Texture2D mouseCursors2;

	public static Texture2D mouseCursors_1_6;

	public static Texture2D giftboxTexture;

	public static Texture2D controllerMaps;

	public static Texture2D indoorWindowLight;

	public static Texture2D animations;

	public static Texture2D concessionsSpriteSheet;

	public static Texture2D birdsSpriteSheet;

	public static Texture2D objectSpriteSheet_2;

	public static Texture2D bobbersTexture;

	public static Dictionary<string, Stack<Dialogue>> npcDialogues;

	protected readonly List<Farmer> _farmerShadows = new List<Farmer>();

	/// <summary>Actions that are called after waking up in the morning. These aren't saved, so they're only use for "fluff".</summary>
	public static Queue<Action> morningQueue;

	[NonInstancedStatic]
	protected internal static ModHooks hooks;

	public static InputState input;

	protected internal static IInputSimulator inputSimulator;

	public const string concessionsSpriteSheetName = "LooseSprites\\Concessions";

	public const string cropSpriteSheetName = "TileSheets\\crops";

	public const string objectSpriteSheetName = "Maps\\springobjects";

	public const string animationsName = "TileSheets\\animations";

	public const string mouseCursorsName = "LooseSprites\\Cursors";

	public const string mouseCursors2Name = "LooseSprites\\Cursors2";

	public const string mouseCursors1_6Name = "LooseSprites\\Cursors_1_6";

	public const string giftboxName = "LooseSprites\\Giftbox";

	public const string toolSpriteSheetName = "TileSheets\\tools";

	public const string bigCraftableSpriteSheetName = "TileSheets\\Craftables";

	public const string debrisSpriteSheetName = "TileSheets\\debris";

	public const string parrotSheetName = "LooseSprites\\parrots";

	public const string hatsSheetName = "Characters\\Farmer\\hats";

	public const string bobbersTextureName = "TileSheets\\bobbers";

	private static Texture2D _toolSpriteSheet;

	public static Dictionary<Vector2, int> crabPotOverlayTiles;

	protected static bool _setSaveName;

	protected static string _currentSaveName;

	public static List<string> mailDeliveredFromMailForTomorrow;

	private static RenderTarget2D _lightmap;

	public static Texture2D[] dynamicPixelRects;

	public static Texture2D fadeToBlackRect;

	public static Texture2D staminaRect;

	public static Texture2D lightingRect;

	public static SpriteFont dialogueFont;

	public static SpriteFont smallFont;

	public static SpriteFont tinyFont;

	public static float screenGlowAlpha;

	public static float flashAlpha;

	public static float noteBlockTimer;

	public static int currentGemBirdIndex;

	public Dictionary<string, object> newGameSetupOptions = new Dictionary<string, object>();

	public static bool dialogueUp;

	public static bool dialogueTyping;

	public static bool isQuestion;

	public static bool newDay;

	public static bool eventUp;

	public static bool viewportFreeze;

	public static bool eventOver;

	public static bool screenGlow;

	public static bool screenGlowHold;

	public static bool screenGlowUp;

	public static bool killScreen;

	public static bool messagePause;

	public static bool weddingToday;

	public static bool exitToTitle;

	public static bool debugMode;

	public static bool displayHUD;

	public static bool displayFarmer;

	public static bool dialogueButtonShrinking;

	public static bool drawLighting;

	public static bool quit;

	public static bool drawGrid;

	public static bool freezeControls;

	public static bool saveOnNewDay;

	public static bool panMode;

	public static bool showingEndOfNightStuff;

	public static bool wasRainingYesterday;

	public static bool hasLoadedGame;

	public static bool isActionAtCurrentCursorTile;

	public static bool isInspectionAtCurrentCursorTile;

	public static bool isSpeechAtCurrentCursorTile;

	public static bool paused;

	public static bool isTimePaused;

	public static bool frameByFrame;

	public static bool lastCursorMotionWasMouse;

	public static bool showingHealth;

	public static bool cabinsSeparate;

	public static bool showingHealthBar;

	/// <summary>Whether <see cref="M:StardewValley.Game1.OnDayStarted" /> has been called at least once since this save was loaded or joined.</summary>
	public static bool hasStartedDay;

	/// <summary>The event IDs which the current player has seen since entering the location.</summary>
	public static HashSet<string> eventsSeenSinceLastLocationChange;

	internal static bool hasApplied1_3_UpdateChanges;

	internal static bool hasApplied1_4_UpdateChanges;

	private static Action postExitToTitleCallback;

	protected int _lastUsedDisplay = -1;

	public bool wasAskedLeoMemory;

	public float controllerSlingshotSafeTime;

	public static BundleType bundleType;

	public static bool isRaining;

	public static bool isSnowing;

	public static bool isLightning;

	public static bool isDebrisWeather;

	/// <summary>Internal state that tracks whether today's weather state is a green rain day.</summary>
	private static bool _isGreenRain;

	/// <summary>Whether today's weather state was green rain at any point.</summary>
	internal static bool wasGreenRain;

	/// <summary>Whether the locations affected by green rain still need cleanup. This should only be set by <see cref="M:StardewValley.Game1._newDayAfterFade" />.</summary>
	internal static bool greenRainNeedsCleanup;

	/// <summary>The season for which the debris weather fields like <see cref="F:StardewValley.Game1.debrisWeather" /> were last generated.</summary>
	public static Season? debrisWeatherSeason;

	public static string weatherForTomorrow;

	public float zoomModifier = 1f;

	private static ScreenFade screenFade;

	/// <summary>The current season of the year.</summary>
	public static Season season;

	public static SerializableDictionary<string, string> bannedUsers;

	private static object _debugOutputLock;

	private static string _debugOutput;

	public static string requestedMusicTrack;

	public static string messageAfterPause;

	public static string samBandName;

	public static string loadingMessage;

	public static string errorMessage;

	protected Dictionary<MusicContext, KeyValuePair<string, bool>> _instanceRequestedMusicTracks = new Dictionary<MusicContext, KeyValuePair<string, bool>>();

	protected MusicContext _instanceActiveMusicContext;

	public static bool requestedMusicTrackOverrideable;

	public static bool currentTrackOverrideable;

	public static bool requestedMusicDirty;

	protected bool _useUnscaledLighting;

	protected bool _didInitiateItemStow;

	public bool instanceIsOverridingTrack;

	private static string[] _shortDayDisplayName;

	public static Queue<string> currentObjectDialogue;

	public static HashSet<string> worldStateIDs;

	public static List<Response> questionChoices;

	public static int xLocationAfterWarp;

	public static int yLocationAfterWarp;

	public static int gameTimeInterval;

	public static int currentQuestionChoice;

	public static int currentDialogueCharacterIndex;

	public static int dialogueTypingInterval;

	/// <summary>The calendar day of month.</summary>
	public static int dayOfMonth;

	/// <summary>The calendar year.</summary>
	public static int year;

	public static int timeOfDay;

	public static int timeOfDayAfterFade;

	public static int dialogueWidth;

	public static int facingDirectionAfterWarp;

	public static int mouseClickPolling;

	public static int gamePadXButtonPolling;

	public static int gamePadAButtonPolling;

	public static int weatherIcon;

	public static int hitShakeTimer;

	public static int staminaShakeTimer;

	public static int pauseThenDoFunctionTimer;

	public static int cursorTileHintCheckTimer;

	public static int timerUntilMouseFade;

	public static int whichFarm;

	public static int startingCabins;

	public static ModFarmType whichModFarm;

	public static ulong? startingGameSeed;

	public static int elliottPiano;

	public static Microsoft.Xna.Framework.Rectangle viewportClampArea;

	public static SaveFixes lastAppliedSaveFix;

	public static Color eveningColor;

	public static Color unselectedOptionColor;

	public static Color screenGlowColor;

	public static NPC currentSpeaker;

	/// <summary>A default random number generator used for a wide variety of randomization in the game. This provides non-repeatable randomization (e.g. reloading the save will produce different results).</summary>
	public static Random random;

	public static Random recentMultiplayerRandom;

	/// <summary>The cached data for achievements from <c>Data/Achievements</c>.</summary>
	public static Dictionary<int, string> achievements;

	/// <summary>The cached data for <see cref="F:StardewValley.ItemRegistry.type_bigCraftable" />-type items from <c>Data/BigCraftables</c>.</summary>
	public static IDictionary<string, BigCraftableData> bigCraftableData;

	/// <summary>The cached data for buildings from <c>Data/Buildings</c>.</summary>
	public static IDictionary<string, BuildingData> buildingData;

	/// <summary>The cached data for NPCs from <c>Data/Characters</c>.</summary>
	public static IDictionary<string, CharacterData> characterData;

	/// <summary>The cached data for crops from <c>Data/Crops</c>.</summary>
	public static IDictionary<string, CropData> cropData;

	/// <summary>The cached data for farm animals from <c>Data/FarmAnimals</c>.</summary>
	public static IDictionary<string, FarmAnimalData> farmAnimalData;

	/// <summary>The cached data for flooring and path items from <c>Data/FloorsAndPaths</c>.</summary>
	public static IDictionary<string, FloorPathData> floorPathData;

	/// <summary>The cached data for fruit trees from <c>Data/FruitTrees</c>.</summary>
	public static IDictionary<string, FruitTreeData> fruitTreeData;

	/// <summary>The cached data for jukebox tracks from <c>Data/JukeboxTracks</c>.</summary>
	public static IDictionary<string, JukeboxTrackData> jukeboxTrackData;

	/// <summary>The cached data for locations from <c>Data/Locations</c>.</summary>
	public static IDictionary<string, LocationData> locationData;

	/// <summary>The cached data for location contexts from <c>Data/LocationContexts</c>.</summary>
	public static IDictionary<string, LocationContextData> locationContextData;

	/// <summary>The cached data for <c>Data/NPCGiftTastes</c>.</summary>
	public static IDictionary<string, string> NPCGiftTastes;

	/// <summary>The cached data for <see cref="F:StardewValley.ItemRegistry.type_object" />-type items from <c>Data/Objects</c>.</summary>
	public static IDictionary<string, ObjectData> objectData;

	/// <summary>The cached data for <see cref="F:StardewValley.ItemRegistry.type_pants" />-type items from <c>Data/Pants</c>.</summary>
	public static IDictionary<string, PantsData> pantsData;

	/// <summary>The cached data for pets from <c>Data/Pets</c>.</summary>
	public static IDictionary<string, PetData> petData;

	/// <summary>The cached data for <see cref="F:StardewValley.ItemRegistry.type_shirt" />-type items from <c>Data/Shirts</c>.</summary>
	public static IDictionary<string, ShirtData> shirtData;

	/// <summary>The cached data for <see cref="F:StardewValley.ItemRegistry.type_tool" />-type items from <c>Data/Tools</c>.</summary>
	public static IDictionary<string, ToolData> toolData;

	/// <summary>The cached data for <see cref="F:StardewValley.ItemRegistry.type_weapon" />-type items from <c>Data/Weapons</c>.</summary>
	public static IDictionary<string, WeaponData> weaponData;

	public static List<HUDMessage> hudMessages;

	public static float musicPlayerVolume;

	public static float ambientPlayerVolume;

	public static float pauseAccumulator;

	public static float pauseTime;

	public static float upPolling;

	public static float downPolling;

	public static float rightPolling;

	public static float leftPolling;

	public static float debrisSoundInterval;

	public static float windGust;

	public static float dialogueButtonScale;

	public ICue instanceCurrentSong;

	public static IAudioCategory musicCategory;

	public static IAudioCategory soundCategory;

	public static IAudioCategory ambientCategory;

	public static IAudioCategory footstepCategory;

	public PlayerIndex instancePlayerOneIndex;

	[NonInstancedStatic]
	public static IAudioEngine audioEngine;

	[NonInstancedStatic]
	public static WaveBank waveBank;

	[NonInstancedStatic]
	public static WaveBank waveBank1_4;

	[NonInstancedStatic]
	public static ISoundBank soundBank;

	public static Vector2 previousViewportPosition;

	public static Vector2 currentCursorTile;

	public static Vector2 lastCursorTile;

	public static Vector2 snowPos;

	public Microsoft.Xna.Framework.Rectangle localMultiplayerWindow;

	public static RainDrop[] rainDrops;

	public static ICue chargeUpSound;

	public static ICue wind;

	/// <summary>The audio cues for the current location which are continuously looping until they're stopped.</summary>
	public static LoopingCueManager loopingLocationCues;

	/// <summary>Encapsulates the game logic for playing sound effects (excluding music and background ambience).</summary>
	public static ISoundsHelper sounds;

	[NonInstancedStatic]
	public static AudioCueModificationManager CueModification;

	public static List<WeatherDebris> debrisWeather;

	public static TemporaryAnimatedSpriteList screenOverlayTempSprites;

	public static TemporaryAnimatedSpriteList uiOverlayTempSprites;

	private static byte _gameMode;

	private bool _isSaving;

	/// <summary>Handles writing game messages to the log output.</summary>
	[NonInstancedStatic]
	protected internal static IGameLogger log;

	/// <summary>Combines hash codes in a deterministic way that's consistent between both sessions and players.</summary>
	[NonInstancedStatic]
	public static IHashUtility hash;

	protected internal static Multiplayer multiplayer;

	public static byte multiplayerMode;

	public static IEnumerator<int> currentLoader;

	public static ulong uniqueIDForThisGame;

	public static int[] directionKeyPolling;

	/// <summary>The light sources to draw in the current location.</summary>
	public static Dictionary<string, LightSource> currentLightSources;

	public static Color ambientLight;

	public static Color outdoorLight;

	public static Color textColor;

	/// <summary>The default color for shadows drawn under text.</summary>
	public static Color textShadowColor;

	/// <summary>A darker version of <see cref="F:StardewValley.Game1.textShadowColor" /> used in some cases.</summary>
	public static Color textShadowDarkerColor;

	public static IClickableMenu overlayMenu;

	private static IClickableMenu _activeClickableMenu;

	/// <summary>The queue of menus to open when the <see cref="P:StardewValley.Game1.activeClickableMenu" /> is closed.</summary>
	/// <remarks>See also <see cref="P:StardewValley.Game1.activeClickableMenu" />, <see cref="F:StardewValley.Game1.onScreenMenus" />, and <see cref="F:StardewValley.Game1.overlayMenu" />.</remarks>
	public static List<IClickableMenu> nextClickableMenu;

	/// <summary>A queue of actions to perform when <see cref="M:StardewValley.Farmer.IsBusyDoingSomething" /> is false.</summary>
	/// <remarks>Most code should call <see cref="M:StardewValley.Game1.PerformActionWhenPlayerFree(System.Action)" /> instead of using this field directly.</remarks>
	public static List<Action> actionsWhenPlayerFree;

	public static bool isCheckingNonMousePlacement;

	private static IMinigame _currentMinigame;

	public static IList<IClickableMenu> onScreenMenus;

	public static BuffsDisplay buffsDisplay;

	public static DayTimeMoneyBox dayTimeMoneyBox;

	public static NetRootDictionary<long, Farmer> otherFarmers;

	private static readonly FarmerCollection _onlineFarmers;

	public static IGameServer server;

	public static Client client;

	public KeyboardDispatcher instanceKeyboardDispatcher;

	public static Background background;

	public static FarmEvent farmEvent;

	/// <summary>The farm event to play next, if a regular farm event doesn't play via <see cref="F:StardewValley.Game1.farmEvent" /> instead.</summary>
	/// <remarks>This is set via the <see cref="M:StardewValley.DebugCommands.DefaultHandlers.SetFarmEvent(System.String[],StardewValley.Logging.IGameLogger)" /> debug command.</remarks>
	public static FarmEvent farmEventOverride;

	public static afterFadeFunction afterFade;

	public static afterFadeFunction afterDialogues;

	public static afterFadeFunction afterViewport;

	public static afterFadeFunction viewportReachedTarget;

	public static afterFadeFunction afterPause;

	public static GameTime currentGameTime;

	public static IList<DelayedAction> delayedActions;

	public static Stack<IClickableMenu> endOfNightMenus;

	public Options instanceOptions;

	[NonInstancedStatic]
	public static SerializableDictionary<long, Options> splitscreenOptions;

	public static Game1 game1;

	public static Point lastMousePositionBeforeFade;

	public static int ticks;

	public static EmoteMenu emoteMenu;

	[NonInstancedStatic]
	public static SerializableDictionary<string, string> CustomData;

	/// <summary>Manages and synchronizes ready checks, which ensure all players are ready before proceeding (e.g. before sleeping).</summary>
	public static ReadySynchronizer netReady;

	/// <summary>Manages updating a fake host player when <see cref="!:Game1.IsDedicatedServer" /> is true.</summary>
	public static DedicatedServer dedicatedServer;

	public static NetRoot<NetWorldState> netWorldState;

	public static ChatBox chatBox;

	public TextEntryMenu instanceTextEntry;

	public static SpecialCurrencyDisplay specialCurrencyDisplay;

	private static string debugPresenceString;

	public static List<Action> remoteEventQueue;

	public static List<long> weddingsToday;

	public int instanceIndex;

	public int instanceId;

	public static bool overrideGameMenuReset;

	protected bool _windowResizing;

	protected Point _oldMousePosition;

	protected bool _oldGamepadConnectedState;

	protected int _oldScrollWheelValue;

	public static Point viewportCenter;

	public static Vector2 viewportTarget;

	public static float viewportSpeed;

	public static int viewportHold;

	private static bool _cursorDragEnabled;

	private static bool _cursorDragPrevEnabled;

	private static bool _cursorSpeedDirty;

	private const float CursorBaseSpeed = 16f;

	private static float _cursorSpeed;

	private static float _cursorSpeedScale;

	private static float _cursorUpdateElapsedSec;

	private static int thumbstickPollingTimer;

	public static bool toggleFullScreen;

	public static string whereIsTodaysFest;

	public const string NO_LETTER_MAIL = "%&NL&%";

	public const string BROADCAST_MAIL_FOR_TOMORROW_PREFIX = "%&MFT&%";

	public const string BROADCAST_SEEN_MAIL_PREFIX = "%&SM&%";

	public const string BROADCAST_MAILBOX_PREFIX = "%&MB&%";

	public bool isLocalMultiplayerNewDayActive;

	protected static Task _newDayTask;

	private static Action _afterNewDayAction;

	public static NewDaySynchronizer newDaySync;

	public static bool forceSnapOnNextViewportUpdate;

	public static Vector2 currentViewportTarget;

	public static Vector2 viewportPositionLerp;

	public static float screenGlowRate;

	public static float screenGlowMax;

	public static bool haltAfterCheck;

	public static bool uiMode;

	public static RenderTarget2D nonUIRenderTarget;

	public static int uiModeCount;

	protected static int _oldUIModeCount;

	internal string panModeString;

	public static bool conventionMode;

	internal static EventTest eventTest;

	internal bool panFacingDirectionWait;

	public static bool isRunningMacro;

	public static int thumbstickMotionMargin;

	public static float thumbstickMotionAccell;

	public static int triggerPolling;

	public static int rightClickPolling;

	private RenderTarget2D _screen;

	private RenderTarget2D _uiScreen;

	public static Color bgColor;

	protected readonly BlendState lightingBlend = new BlendState
	{
		ColorBlendFunction = BlendFunction.ReverseSubtract,
		ColorDestinationBlend = Blend.One,
		ColorSourceBlend = Blend.SourceColor
	};

	public bool isDrawing;

	[NonInstancedStatic]
	public static bool isRenderingScreenBuffer;

	protected bool _lastDrewMouseCursor;

	protected static int _activatedTick;

	/// <summary>The cursor icon to show, usually matching a constant like <see cref="F:StardewValley.Game1.cursor_default" />.</summary>
	public static int mouseCursor;

	private static float _mouseCursorTransparency;

	public static bool wasMouseVisibleThisFrame;

	public static NPC objectDialoguePortraitPerson;

	protected static StringBuilder _ParseTextStringBuilder;

	protected static StringBuilder _ParseTextStringBuilderLine;

	protected static StringBuilder _ParseTextStringBuilderWord;

	public bool ScreenshotBusy;

	public bool takingMapScreenshot;

	public bool IsActiveNoOverlay
	{
		get
		{
			if (!base.IsActive)
			{
				return false;
			}
			if (Program.sdk.HasOverlay)
			{
				return false;
			}
			return true;
		}
	}

	public static LocalizedContentManager temporaryContent
	{
		get
		{
			if (_temporaryContent == null)
			{
				_temporaryContent = content.CreateTemporary();
			}
			return _temporaryContent;
		}
	}

	private bool ShouldLoadIncrementally => false;

	/// <summary>The local character controlled by the current player.</summary>
	/// <remarks>Setting this field will dispose the previous value, if any. The new value must be a completely new instance which doesn't share any texture instances with the previous one to avoid errors.</remarks>
	public static Farmer player
	{
		get
		{
			return _player;
		}
		internal set
		{
			_player?.unload();
			_player = value;
			_player.Items.IsLocalPlayerInventory = true;
		}
	}

	public static bool IsPlayingBackgroundMusic
	{
		get
		{
			return game1._instanceIsPlayingBackgroundMusic;
		}
		set
		{
			game1._instanceIsPlayingBackgroundMusic = value;
		}
	}

	public static bool IsPlayingOutdoorsAmbience
	{
		get
		{
			return game1._instanceIsPlayingOutdoorsAmbience;
		}
		set
		{
			game1._instanceIsPlayingOutdoorsAmbience = value;
		}
	}

	public static bool IsPlayingNightAmbience
	{
		get
		{
			return game1._instanceIsPlayingNightAmbience;
		}
		set
		{
			game1._instanceIsPlayingNightAmbience = value;
		}
	}

	public static bool IsPlayingTownMusic
	{
		get
		{
			return game1._instanceIsPlayingTownMusic;
		}
		set
		{
			game1._instanceIsPlayingTownMusic = value;
		}
	}

	public static bool IsPlayingMorningSong
	{
		get
		{
			return game1._instanceIsPlayingMorningSong;
		}
		set
		{
			game1._instanceIsPlayingMorningSong = value;
		}
	}

	public static bool isWarping => _isWarping;

	public static IList<GameLocation> locations => game1._locations;

	public static GameLocation currentLocation
	{
		get
		{
			return game1.instanceGameLocation;
		}
		set
		{
			if (game1.instanceGameLocation != value)
			{
				if (_PreviousNonNullLocation == null)
				{
					_PreviousNonNullLocation = game1.instanceGameLocation;
				}
				game1.instanceGameLocation = value;
				if (game1.instanceGameLocation != null)
				{
					GameLocation previousNonNullLocation = _PreviousNonNullLocation;
					_PreviousNonNullLocation = null;
					OnLocationChanged(previousNonNullLocation, game1.instanceGameLocation);
				}
			}
		}
	}

	public static Texture2D toolSpriteSheet
	{
		get
		{
			if (_toolSpriteSheet == null)
			{
				ResetToolSpriteSheet();
			}
			return _toolSpriteSheet;
		}
	}

	public static RenderTarget2D lightmap => _lightmap;

	/// <summary>Get whether HUD menus like the toolbar are currently being drawn.</summary>
	public static bool IsHudDrawn
	{
		get
		{
			if ((displayHUD || eventUp) && gameMode == 3 && !freezeControls && !panMode && !HostPaused)
			{
				return !game1.takingMapScreenshot;
			}
			return false;
		}
	}

	/// <summary>Whether today's weather state is a green rain day.</summary>
	public static bool isGreenRain
	{
		get
		{
			return _isGreenRain;
		}
		set
		{
			_isGreenRain = value;
			wasGreenRain |= value;
		}
	}

	public static bool spawnMonstersAtNight
	{
		get
		{
			return player.team.spawnMonstersAtNight.Value;
		}
		set
		{
			player.team.spawnMonstersAtNight.Value = value;
		}
	}

	/// <summary>When the game makes a random choice, whether to use a simpler method that's prone to repeating patterns.</summary>
	/// <remarks>This is mainly intended for speedrunning, where full randomization might be undesirable.</remarks>
	public static bool UseLegacyRandom
	{
		get
		{
			return player.team.useLegacyRandom.Value;
		}
		set
		{
			player.team.useLegacyRandom.Value = value;
		}
	}

	public static bool fadeToBlack
	{
		get
		{
			return screenFade.fadeToBlack;
		}
		set
		{
			screenFade.fadeToBlack = value;
		}
	}

	public static bool fadeIn
	{
		get
		{
			return screenFade.fadeIn;
		}
		set
		{
			screenFade.fadeIn = value;
		}
	}

	public static bool globalFade
	{
		get
		{
			return screenFade.globalFade;
		}
		set
		{
			screenFade.globalFade = value;
		}
	}

	public static bool nonWarpFade
	{
		get
		{
			return screenFade.nonWarpFade;
		}
		set
		{
			screenFade.nonWarpFade = value;
		}
	}

	public static float fadeToBlackAlpha
	{
		get
		{
			return screenFade.fadeToBlackAlpha;
		}
		set
		{
			screenFade.fadeToBlackAlpha = value;
		}
	}

	public static float globalFadeSpeed
	{
		get
		{
			return screenFade.globalFadeSpeed;
		}
		set
		{
			screenFade.globalFadeSpeed = value;
		}
	}

	public static string CurrentSeasonDisplayName => content.LoadString("Strings\\StringsFromCSFiles:" + currentSeason);

	/// <summary>The current season of the year as a string (one of <c>spring</c>, <c>summer</c>, <c>fall</c>, or <c>winter</c>).</summary>
	/// <remarks>Most code should use <see cref="F:StardewValley.Game1.season" /> instead.</remarks>
	public static string currentSeason
	{
		get
		{
			return Utility.getSeasonKey(season);
		}
		set
		{
			if (Utility.TryParseEnum<Season>(value, out var parsed))
			{
				season = parsed;
				return;
			}
			throw new ArgumentException("Can't parse value '" + value + "' as a season name.");
		}
	}

	/// <summary>The current season of the year as a numeric index.</summary>
	/// <remarks>Most code should use <see cref="F:StardewValley.Game1.season" /> instead.</remarks>
	public static int seasonIndex => (int)season;

	public static string debugOutput
	{
		get
		{
			return _debugOutput;
		}
		set
		{
			lock (_debugOutputLock)
			{
				if (_debugOutput != value)
				{
					_debugOutput = value;
					if (!string.IsNullOrEmpty(_debugOutput))
					{
						log.Debug("DebugOutput: " + _debugOutput);
					}
				}
			}
		}
	}

	public static string elliottBookName
	{
		get
		{
			if (player != null && player.DialogueQuestionsAnswered.Contains("958699"))
			{
				return content.LoadString("Strings\\Events:ElliottBook_mystery");
			}
			if (player != null && player.DialogueQuestionsAnswered.Contains("958700"))
			{
				return content.LoadString("Strings\\Events:ElliottBook_romance");
			}
			return content.LoadString("Strings\\Events:ElliottBook_default");
		}
		set
		{
		}
	}

	protected static Dictionary<MusicContext, KeyValuePair<string, bool>> _requestedMusicTracks
	{
		get
		{
			return game1._instanceRequestedMusicTracks;
		}
		set
		{
			game1._instanceRequestedMusicTracks = value;
		}
	}

	protected static MusicContext _activeMusicContext
	{
		get
		{
			return game1._instanceActiveMusicContext;
		}
		set
		{
			game1._instanceActiveMusicContext = value;
		}
	}

	public static bool isOverridingTrack
	{
		get
		{
			return game1.instanceIsOverridingTrack;
		}
		set
		{
			game1.instanceIsOverridingTrack = value;
		}
	}

	public bool useUnscaledLighting
	{
		get
		{
			return _useUnscaledLighting;
		}
		set
		{
			if (_useUnscaledLighting != value)
			{
				_useUnscaledLighting = value;
				allocateLightmap(localMultiplayerWindow.Width, localMultiplayerWindow.Height);
			}
		}
	}

	/// <inheritdoc cref="F:StardewValley.Farmer.mailbox" />
	public static IList<string> mailbox => player.mailbox;

	public static ICue currentSong
	{
		get
		{
			return game1.instanceCurrentSong;
		}
		set
		{
			game1.instanceCurrentSong = value;
		}
	}

	public static PlayerIndex playerOneIndex
	{
		get
		{
			return game1.instancePlayerOneIndex;
		}
		set
		{
			game1.instancePlayerOneIndex = value;
		}
	}

	/// <summary>The number of ticks since <see cref="P:StardewValley.Game1.gameMode" /> changed.</summary>
	public static int gameModeTicks { get; private set; }

	public static byte gameMode
	{
		get
		{
			return _gameMode;
		}
		set
		{
			if (_gameMode != value)
			{
				log.Verbose("gameMode was '" + GameModeToString(_gameMode) + "', set to '" + GameModeToString(value) + "'.");
				_gameMode = value;
				gameModeTicks = 0;
			}
		}
	}

	public bool IsSaving
	{
		get
		{
			return _isSaving;
		}
		set
		{
			_isSaving = value;
		}
	}

	public static Multiplayer Multiplayer => multiplayer;

	public static Stats stats => player.stats;

	/// <summary>The daily quest that's shown on the billboard, if any.</summary>
	public static Quest questOfTheDay => netWorldState.Value.QuestOfTheDay;

	/// <summary>The menu which is currently handling player interactions (e.g. a letter viewer, dialogue box, inventory, etc).</summary>
	/// <remarks>See also <see cref="F:StardewValley.Game1.nextClickableMenu" />, <see cref="F:StardewValley.Game1.onScreenMenus" />, and <see cref="F:StardewValley.Game1.overlayMenu" />.</remarks>
	public static IClickableMenu activeClickableMenu
	{
		get
		{
			return _activeClickableMenu;
		}
		set
		{
			bool num = (activeClickableMenu is SaveGameMenu || activeClickableMenu is ShippingMenu) && !(value is SaveGameMenu) && !(value is ShippingMenu);
			if (_activeClickableMenu is IDisposable disposable && !_activeClickableMenu.HasDependencies())
			{
				disposable.Dispose();
			}
			if (textEntry != null && _activeClickableMenu != value)
			{
				closeTextEntry();
			}
			if (_activeClickableMenu != null && value == null)
			{
				timerUntilMouseFade = 0;
			}
			_activeClickableMenu = value;
			if (num)
			{
				OnDayStarted();
			}
			if (_activeClickableMenu != null)
			{
				if (!eventUp || (CurrentEvent != null && CurrentEvent.playerControlSequence && !player.UsingTool))
				{
					player.Halt();
				}
			}
			else if (nextClickableMenu.Count > 0)
			{
				activeClickableMenu = nextClickableMenu[0];
				nextClickableMenu.RemoveAt(0);
			}
		}
	}

	public static IMinigame currentMinigame
	{
		get
		{
			return _currentMinigame;
		}
		set
		{
			_currentMinigame = value;
			if (value == null)
			{
				if (currentLocation != null)
				{
					setRichPresence("location", currentLocation.Name);
				}
				randomizeDebrisWeatherPositions(debrisWeather);
				randomizeRainPositions();
			}
			else if (value.minigameId() != null)
			{
				setRichPresence("minigame", value.minigameId());
			}
		}
	}

	public static Object dishOfTheDay
	{
		get
		{
			return netWorldState.Value.DishOfTheDay;
		}
		set
		{
			netWorldState.Value.DishOfTheDay = value;
		}
	}

	public static KeyboardDispatcher keyboardDispatcher
	{
		get
		{
			return game1.instanceKeyboardDispatcher;
		}
		set
		{
			game1.instanceKeyboardDispatcher = value;
		}
	}

	public static Options options
	{
		get
		{
			return game1.instanceOptions;
		}
		set
		{
			game1.instanceOptions = value;
		}
	}

	public static TextEntryMenu textEntry
	{
		get
		{
			return game1.instanceTextEntry;
		}
		set
		{
			game1.instanceTextEntry = value;
		}
	}

	public static WorldDate Date => netWorldState.Value.Date;

	public static bool NetTimePaused => netWorldState.Get().IsTimePaused;

	public static bool HostPaused => netWorldState.Get().IsPaused;

	/// <summary>Whether the game is currently in multiplayer mode with at least one other player connected.</summary>
	public static bool IsMultiplayer => otherFarmers.Count > 0;

	/// <summary>Whether this game instance is a farmhand connected to a remote host in multiplayer.</summary>
	public static bool IsClient => multiplayerMode == 1;

	/// <summary>Whether this game instance is the host in multiplayer.</summary>
	public static bool IsServer => multiplayerMode == 2;

	/// <summary>Whether this game instance is the main or host player.</summary>
	public static bool IsMasterGame
	{
		get
		{
			if (multiplayerMode != 0)
			{
				return multiplayerMode == 2;
			}
			return true;
		}
	}

	/// <summary>Whether this game is hosted by an automated dedicated host</summary>
	public static bool HasDedicatedHost
	{
		get
		{
			if (multiplayerMode != 0)
			{
				return player?.team?.hasDedicatedHost.Value == true;
			}
			return false;
		}
	}

	/// <summary>Whether this game instance is acting as the host of a dedicated server.</summary>
	public static bool IsDedicatedHost
	{
		get
		{
			if (IsServer)
			{
				return HasDedicatedHost;
			}
			return false;
		}
	}

	/// <summary>The main or host player instance.</summary>
	public static Farmer MasterPlayer
	{
		get
		{
			if (!IsMasterGame)
			{
				return serverHost.Value;
			}
			return player;
		}
	}

	public static bool IsChatting
	{
		get
		{
			if (chatBox != null)
			{
				return chatBox.isActive();
			}
			return false;
		}
		set
		{
			if (value != chatBox.isActive())
			{
				if (value)
				{
					chatBox.activate();
				}
				else
				{
					chatBox.clickAway();
				}
			}
		}
	}

	public static Event CurrentEvent
	{
		get
		{
			if (currentLocation == null)
			{
				return null;
			}
			return currentLocation.currentEvent;
		}
	}

	public static MineShaft mine => (locationRequest?.Location as MineShaft) ?? (currentLocation as MineShaft);

	public static int CurrentMineLevel => (currentLocation as MineShaft)?.mineLevel ?? 0;

	public static int CurrentPlayerLimit
	{
		get
		{
			if (netWorldState?.Value != null)
			{
				_ = netWorldState.Value.CurrentPlayerLimit;
				return netWorldState.Value.CurrentPlayerLimit;
			}
			return multiplayer.playerLimit;
		}
	}

	private static float thumbstickToMouseModifier
	{
		get
		{
			if (_cursorSpeedDirty)
			{
				ComputeCursorSpeed();
			}
			return _cursorSpeed / 720f * (float)viewport.Height * (float)currentGameTime.ElapsedGameTime.TotalSeconds;
		}
	}

	public static bool isFullscreen => graphics.IsFullScreen;

	/// <summary>Get whether it's summer in the valley.</summary>
	/// <remarks>See <see cref="M:StardewValley.GameLocation.IsSummerHere" /> to handle local seasons.</remarks>
	public static bool IsSummer => season == Season.Summer;

	/// <summary>Get whether it's spring in the valley.</summary>
	/// <remarks>See <see cref="M:StardewValley.GameLocation.IsSpringHere" /> to handle local seasons.</remarks>
	public static bool IsSpring => season == Season.Spring;

	/// <summary>Get whether it's fall in the valley.</summary>
	/// <remarks>See <see cref="M:StardewValley.GameLocation.IsFallHere" /> to handle local seasons.</remarks>
	public static bool IsFall => season == Season.Fall;

	/// <summary>Get whether it's winter in the valley.</summary>
	/// <remarks>See <see cref="M:StardewValley.GameLocation.IsWinterHere" /> to handle local seasons.</remarks>
	public static bool IsWinter => season == Season.Winter;

	public RenderTarget2D screen
	{
		get
		{
			return _screen;
		}
		set
		{
			if (_screen != null)
			{
				_screen.Dispose();
				_screen = null;
			}
			_screen = value;
		}
	}

	public RenderTarget2D uiScreen
	{
		get
		{
			return _uiScreen;
		}
		set
		{
			if (_uiScreen != null)
			{
				_uiScreen.Dispose();
				_uiScreen = null;
			}
			_uiScreen = value;
		}
	}

	public static float mouseCursorTransparency
	{
		get
		{
			return _mouseCursorTransparency;
		}
		set
		{
			_mouseCursorTransparency = value;
		}
	}

	public static void GetHasRoomAnotherFarmAsync(ReportHasRoomAnotherFarmDelegate callback)
	{
		if (LocalMultiplayer.IsLocalMultiplayer())
		{
			bool hasRoomAnotherFarm = GetHasRoomAnotherFarm();
			callback(hasRoomAnotherFarm);
			return;
		}
		Task task = new Task(delegate
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			bool hasRoomAnotherFarm2 = GetHasRoomAnotherFarm();
			callback(hasRoomAnotherFarm2);
		});
		hooks.StartTask(task, "Farm_SpaceCheck");
	}

	private static string GameModeToString(byte mode)
	{
		return mode switch
		{
			4 => $"logoScreenGameMode ({mode})", 
			0 => $"titleScreenGameMode ({mode})", 
			1 => $"loadScreenGameMode ({mode})", 
			2 => $"newGameMode ({mode})", 
			3 => $"playingGameMode ({mode})", 
			6 => $"loadingMode ({mode})", 
			7 => $"saveMode ({mode})", 
			8 => $"saveCompleteMode ({mode})", 
			9 => $"selectGameScreen ({mode})", 
			10 => $"creditsMode ({mode})", 
			11 => $"errorLogMode ({mode})", 
			_ => $"unknown ({mode})", 
		};
	}

	/// <summary>Get a human-readable game version which includes the <see cref="F:StardewValley.Game1.version" />, <see cref="F:StardewValley.Game1.versionLabel" />, and <see cref="F:StardewValley.Game1.versionBuildNumber" />.</summary>
	public static string GetVersionString()
	{
		string text = version;
		if (!string.IsNullOrEmpty(versionLabel))
		{
			text = text + " '" + versionLabel + "'";
		}
		if (versionBuildNumber > 0)
		{
			text = text + " build " + versionBuildNumber;
		}
		return text;
	}

	public static void ResetToolSpriteSheet()
	{
		if (_toolSpriteSheet != null)
		{
			_toolSpriteSheet.Dispose();
			_toolSpriteSheet = null;
		}
		Texture2D texture2D = content.Load<Texture2D>("TileSheets\\tools");
		int width = texture2D.Width;
		int height = texture2D.Height;
		Texture2D obj = new Texture2D(game1.GraphicsDevice, width, height, mipmap: false, SurfaceFormat.Color)
		{
			Name = texture2D.Name
		};
		Color[] data = new Color[width * height];
		texture2D.GetData(data);
		obj.SetData(data);
		_toolSpriteSheet = obj;
	}

	public static void SetSaveName(string new_save_name)
	{
		if (new_save_name == null)
		{
			new_save_name = "";
		}
		_currentSaveName = new_save_name;
		_setSaveName = true;
	}

	public static string GetSaveGameName(bool set_value = true)
	{
		if (!_setSaveName && set_value)
		{
			string value = MasterPlayer.farmName.Value;
			string text = value;
			int num = 2;
			while (SaveGame.IsNewGameSaveNameCollision(text))
			{
				text = value + num;
				num++;
			}
			SetSaveName(text);
		}
		return _currentSaveName;
	}

	private static void allocateLightmap(int width, int height)
	{
		int num = 8;
		float num2 = 1f;
		if (options != null)
		{
			num = options.lightingQuality;
			num2 = ((!game1.useUnscaledLighting) ? options.zoomLevel : 1f);
		}
		int num3 = (int)((float)width * (1f / num2) + 64f) / (num / 2);
		int num4 = (int)((float)height * (1f / num2) + 64f) / (num / 2);
		RenderTarget2D renderTarget2D = lightmap;
		if (renderTarget2D == null || renderTarget2D.Width != num3 || lightmap.Height != num4)
		{
			_lightmap?.Dispose();
			_lightmap = new RenderTarget2D(graphics.GraphicsDevice, num3, num4, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
		}
	}

	public static bool canHaveWeddingOnDay(int day, Season season)
	{
		if (!Utility.isFestivalDay(day, season))
		{
			return !Utility.isGreenRainDay(day, season);
		}
		return false;
	}

	/// <summary>Reset the <see cref="P:StardewValley.Game1.questOfTheDay" /> for today and synchronize it to other player. In multiplayer, this can only be called on the host instance.</summary>
	public static void RefreshQuestOfTheDay()
	{
		Quest quest = ((!Utility.isFestivalDay() && !Utility.isFestivalDay(dayOfMonth + 1, season)) ? Utility.getQuestOfTheDay() : null);
		quest?.dailyQuest.Set(newValue: true);
		quest?.reloadObjective();
		quest?.reloadDescription();
		netWorldState.Value.SetQuestOfTheDay(quest);
	}

	public static void ExitToTitle(Action postExitCallback = null)
	{
		currentMinigame?.unload();
		_requestedMusicTracks.Clear();
		UpdateRequestedMusicTrack();
		changeMusicTrack("none");
		setGameMode(0);
		exitToTitle = true;
		postExitToTitleCallback = postExitCallback;
	}

	static Game1()
	{
		realMilliSecondsPerGameMinute = 700;
		realMilliSecondsPerGameTenMinutes = realMilliSecondsPerGameMinute * 10;
		cursor_none = -1;
		cursor_default = 0;
		cursor_wait = 1;
		cursor_grab = 2;
		cursor_gift = 3;
		cursor_talk = 4;
		cursor_look = 5;
		cursor_harvest = 6;
		cursor_gamepad_pointer = 44;
		asianSpacingRegexString = "\\s|[（《“‘「『(](?:[\\w,%％]+|[^…—])[々ぁぃぅぇぉっゃゅょゎゕゖァィゥェォッャュョヶー]*[）》”’」』)，。、？！：；·～,.?!:;~…]*|.[々ぁぃぅぇぉっゃゅょゎゕゖァィゥェォッャュョヶー]*[·・].[々ぁぃぅぇぉっゃゅょゎゕゖァィゥェォッャュョヶー]*|(?:[\\w,%％]+|[^…—])[々ぁぃぅぇぉっゃゅょゎゕゖァィゥェォッャュョヶー]*[）》”’」』)]?(?:[，。、？！：；·～,.?!:;~…]{1,2}[）》”’」』)]?)?|[\\w,%％]+|.[々ぁぃぅぇぉっゃゅょゎゕゖァィゥェォッャュョヶー]+|……|——|.";
		FinishedIncrementalLoad = false;
		FinishedFirstLoadContent = false;
		FinishedFirstInitSounds = false;
		FinishedFirstInitSerializers = false;
		MusicDuckTimer = 0f;
		thumbStickSensitivity = 0.1f;
		runThreshold = 0.5f;
		rightStickHoldTime = 0;
		emoteMenuShowTime = 250;
		nextFarmerWarpOffsetX = 0;
		nextFarmerWarpOffsetY = 0;
		keyboardFocusInstance = null;
		_isWarping = false;
		hasLocalClientsOnly = false;
		isUsingBackToFrontSorting = false;
		_debugStringBuilder = new StringBuilder();
		debugTimings = new DebugTimings();
		_locationLookup = new Dictionary<string, GameLocation>(StringComparer.OrdinalIgnoreCase);
		asianSpacingRegex = new Regex(asianSpacingRegexString, RegexOptions.ECMAScript);
		warpingForForcedRemoteEvent = false;
		_PreviousNonNullLocation = null;
		npcDialogues = new Dictionary<string, Stack<Dialogue>>();
		morningQueue = new Queue<Action>();
		hooks = new ModHooks();
		input = new InputState();
		inputSimulator = null;
		_toolSpriteSheet = null;
		crabPotOverlayTiles = new Dictionary<Vector2, int>();
		_setSaveName = false;
		_currentSaveName = "";
		mailDeliveredFromMailForTomorrow = new List<string>();
		dynamicPixelRects = new Texture2D[3];
		screenGlowAlpha = 0f;
		flashAlpha = 0f;
		currentGemBirdIndex = 0;
		dialogueUp = false;
		dialogueTyping = false;
		isQuestion = false;
		newDay = false;
		eventUp = false;
		viewportFreeze = false;
		eventOver = false;
		screenGlow = false;
		screenGlowHold = false;
		killScreen = false;
		displayHUD = true;
		displayFarmer = true;
		showingHealth = false;
		cabinsSeparate = false;
		showingHealthBar = false;
		hasStartedDay = false;
		eventsSeenSinceLastLocationChange = new HashSet<string>();
		hasApplied1_3_UpdateChanges = false;
		hasApplied1_4_UpdateChanges = false;
		postExitToTitleCallback = null;
		bundleType = BundleType.Default;
		isRaining = false;
		isSnowing = false;
		isLightning = false;
		isDebrisWeather = false;
		_isGreenRain = false;
		wasGreenRain = false;
		greenRainNeedsCleanup = false;
		season = Season.Spring;
		bannedUsers = new SerializableDictionary<string, string>();
		_debugOutputLock = new object();
		requestedMusicTrack = "";
		messageAfterPause = "";
		samBandName = "The Alfalfas";
		loadingMessage = "";
		errorMessage = "";
		requestedMusicDirty = false;
		_shortDayDisplayName = new string[7];
		currentObjectDialogue = new Queue<string>();
		worldStateIDs = new HashSet<string>();
		questionChoices = new List<Response>();
		dayOfMonth = 0;
		year = 1;
		timeOfDay = 600;
		timeOfDayAfterFade = -1;
		whichModFarm = null;
		startingGameSeed = null;
		elliottPiano = 0;
		viewportClampArea = Microsoft.Xna.Framework.Rectangle.Empty;
		eveningColor = new Color(255, 255, 0);
		unselectedOptionColor = new Color(100, 100, 100);
		random = new Random();
		recentMultiplayerRandom = new Random();
		hudMessages = new List<HUDMessage>();
		dialogueButtonScale = 1f;
		lastCursorTile = Vector2.Zero;
		rainDrops = new RainDrop[70];
		loopingLocationCues = new LoopingCueManager();
		sounds = new SoundsHelper();
		CueModification = new AudioCueModificationManager();
		debrisWeather = new List<WeatherDebris>();
		screenOverlayTempSprites = new TemporaryAnimatedSpriteList();
		uiOverlayTempSprites = new TemporaryAnimatedSpriteList();
		log = new DefaultLogger(!Program.releaseBuild, shouldWriteToLogFile: false);
		hash = new HashUtility();
		multiplayer = new Multiplayer();
		uniqueIDForThisGame = Utility.NewUniqueIdForThisGame();
		directionKeyPolling = new int[4];
		currentLightSources = new Dictionary<string, LightSource>();
		outdoorLight = new Color(255, 255, 0);
		textColor = new Color(34, 17, 34);
		textShadowColor = new Color(206, 156, 95);
		textShadowDarkerColor = new Color(221, 148, 84);
		nextClickableMenu = new List<IClickableMenu>();
		actionsWhenPlayerFree = new List<Action>();
		isCheckingNonMousePlacement = false;
		_currentMinigame = null;
		onScreenMenus = new List<IClickableMenu>();
		_onlineFarmers = new FarmerCollection();
		delayedActions = new List<DelayedAction>();
		endOfNightMenus = new Stack<IClickableMenu>();
		splitscreenOptions = new SerializableDictionary<long, Options>();
		CustomData = new SerializableDictionary<string, string>();
		netReady = new ReadySynchronizer();
		dedicatedServer = new DedicatedServer();
		specialCurrencyDisplay = null;
		remoteEventQueue = new List<Action>();
		weddingsToday = new List<long>();
		viewportTarget = new Vector2(-2.1474836E+09f, -2.1474836E+09f);
		viewportSpeed = 2f;
		_cursorDragEnabled = false;
		_cursorDragPrevEnabled = false;
		_cursorSpeedDirty = true;
		_cursorSpeed = 16f;
		_cursorSpeedScale = 1f;
		_cursorUpdateElapsedSec = 0f;
		newDaySync = new NewDaySynchronizer();
		forceSnapOnNextViewportUpdate = false;
		screenGlowRate = 0.005f;
		haltAfterCheck = false;
		uiMode = false;
		nonUIRenderTarget = null;
		uiModeCount = 0;
		_oldUIModeCount = 0;
		conventionMode = false;
		isRunningMacro = false;
		thumbstickMotionAccell = 1f;
		bgColor = new Color(5, 3, 4);
		isRenderingScreenBuffer = false;
		_activatedTick = 0;
		mouseCursor = cursor_default;
		_mouseCursorTransparency = 1f;
		wasMouseVisibleThisFrame = true;
		_ParseTextStringBuilder = new StringBuilder(2408);
		_ParseTextStringBuilderLine = new StringBuilder(1024);
		_ParseTextStringBuilderWord = new StringBuilder(256);
		GameAssemblyName = typeof(Game1).Assembly.GetName().Name;
		AssemblyInformationalVersionAttribute customAttribute = typeof(Game1).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
		if (!string.IsNullOrWhiteSpace(customAttribute?.InformationalVersion))
		{
			string[] array = customAttribute.InformationalVersion.Split(',');
			if (array.Length == 4)
			{
				version = array[0].Trim();
				if (!string.IsNullOrWhiteSpace(array[1]))
				{
					versionLabel = array[1].Trim();
				}
				if (!string.IsNullOrWhiteSpace(array[2]))
				{
					if (!int.TryParse(array[2], out var result))
					{
						throw new InvalidOperationException("Can't parse game build number value '" + array[2] + "' as a number.");
					}
					versionBuildNumber = result;
				}
				if (!string.IsNullOrWhiteSpace(array[3]))
				{
					Multiplayer.protocolVersionOverride = array[3].Trim();
				}
			}
		}
		if (string.IsNullOrWhiteSpace(version))
		{
			version = "1.6.15";
			versionBuildNumber = 24356;
		}
	}

	public Game1(PlayerIndex player_index, int index)
		: this()
	{
		instancePlayerOneIndex = player_index;
		instanceIndex = index;
	}

	public Game1()
	{
		instanceId = GameRunner.instance.GetNewInstanceID();
		if (Program.gamePtr == null)
		{
			Program.gamePtr = this;
		}
		_temporaryContent = CreateContentManager(base.Content.ServiceProvider, base.Content.RootDirectory);
	}

	public void TranslateFields()
	{
		LocalizedContentManager.localizedAssetNames.Clear();
		BaseEnchantment.ResetEnchantments();
		samBandName = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2156");
		elliottBookName = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2157");
		objectSpriteSheet = content.Load<Texture2D>("Maps\\springobjects");
		objectSpriteSheet_2 = content.Load<Texture2D>("TileSheets\\Objects_2");
		bobbersTexture = content.Load<Texture2D>("TileSheets\\bobbers");
		dialogueFont = content.Load<SpriteFont>("Fonts\\SpriteFont1");
		smallFont = content.Load<SpriteFont>("Fonts\\SmallFont");
		smallFont.LineSpacing = 28;
		switch (LocalizedContentManager.CurrentLanguageCode)
		{
		case LocalizedContentManager.LanguageCode.ko:
			smallFont.LineSpacing += 16;
			break;
		case LocalizedContentManager.LanguageCode.tr:
			smallFont.LineSpacing += 4;
			break;
		case LocalizedContentManager.LanguageCode.mod:
			smallFont.LineSpacing = LocalizedContentManager.CurrentModLanguage.SmallFontLineSpacing;
			break;
		}
		tinyFont = content.Load<SpriteFont>("Fonts\\tinyFont");
		objectData = DataLoader.Objects(content);
		bigCraftableData = DataLoader.BigCraftables(content);
		achievements = DataLoader.Achievements(content);
		CraftingRecipe.craftingRecipes = DataLoader.CraftingRecipes(content);
		CraftingRecipe.cookingRecipes = DataLoader.CookingRecipes(content);
		if (cropData == null) cropData = DataLoader.Crops(content);
		StardewValley.Pokemon.PokemonItemManager.Initialize(objectData, cropData, CraftingRecipe.craftingRecipes);
		ItemRegistry.ResetCache();
		MovieTheater.ClearCachedLocalizedData();
		mouseCursors = content.Load<Texture2D>("LooseSprites\\Cursors");
		mouseCursors2 = content.Load<Texture2D>("LooseSprites\\Cursors2");
		mouseCursors_1_6 = content.Load<Texture2D>("LooseSprites\\Cursors_1_6");
		giftboxTexture = content.Load<Texture2D>("LooseSprites\\Giftbox");
		controllerMaps = content.Load<Texture2D>("LooseSprites\\ControllerMaps");
		NPCGiftTastes = DataLoader.NpcGiftTastes(content);
		_shortDayDisplayName[0] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3042");
		_shortDayDisplayName[1] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3043");
		_shortDayDisplayName[2] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3044");
		_shortDayDisplayName[3] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3045");
		_shortDayDisplayName[4] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3046");
		_shortDayDisplayName[5] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3047");
		_shortDayDisplayName[6] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3048");
	}

	public void exitEvent(object sender, EventArgs e)
	{
		multiplayer.Disconnect(Multiplayer.DisconnectType.ClosedGame);
		keyboardDispatcher.Cleanup();
	}

	public void refreshWindowSettings()
	{
		GameRunner.instance.OnWindowSizeChange(null, null);
	}

	public void Window_ClientSizeChanged(object sender, EventArgs e)
	{
		if (_windowResizing)
		{
			return;
		}
		log.Verbose("Window_ClientSizeChanged(); Window.ClientBounds=" + base.Window.ClientBounds.ToString());
		if (options == null)
		{
			log.Verbose("Window_ClientSizeChanged(); options is null, returning.");
			return;
		}
		_windowResizing = true;
		int w = (graphics.IsFullScreen ? graphics.PreferredBackBufferWidth : base.Window.ClientBounds.Width);
		int h = (graphics.IsFullScreen ? graphics.PreferredBackBufferHeight : base.Window.ClientBounds.Height);
		GameRunner.instance.ExecuteForInstances(delegate(Game1 instance)
		{
			instance.SetWindowSize(w, h);
		});
		_windowResizing = false;
	}

	public virtual void SetWindowSize(int w, int h)
	{
		Microsoft.Xna.Framework.Rectangle oldBounds = new Microsoft.Xna.Framework.Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height);
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			if (w < 1280 && !graphics.IsFullScreen)
			{
				w = 1280;
			}
			if (h < 720 && !graphics.IsFullScreen)
			{
				h = 720;
			}
		}
		if (!graphics.IsFullScreen && base.Window.AllowUserResizing)
		{
			graphics.PreferredBackBufferWidth = w;
			graphics.PreferredBackBufferHeight = h;
		}
		if (base.IsMainInstance && graphics.SynchronizeWithVerticalRetrace != options.vsyncEnabled)
		{
			graphics.SynchronizeWithVerticalRetrace = options.vsyncEnabled;
			log.Verbose("Vsync toggled: " + graphics.SynchronizeWithVerticalRetrace);
		}
		graphics.ApplyChanges();
		try
		{
			if (graphics.IsFullScreen)
			{
				localMultiplayerWindow = new Microsoft.Xna.Framework.Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
			}
			else
			{
				localMultiplayerWindow = new Microsoft.Xna.Framework.Rectangle(0, 0, w, h);
			}
		}
		catch (Exception)
		{
		}
		defaultDeviceViewport = new Viewport(localMultiplayerWindow);
		List<Vector4> list = new List<Vector4>();
		if (GameRunner.instance.gameInstances.Count <= 1)
		{
			list.Add(new Vector4(0f, 0f, 1f, 1f));
		}
		else
		{
			switch (GameRunner.instance.gameInstances.Count)
			{
			case 2:
				list.Add(new Vector4(0f, 0f, 0.5f, 1f));
				list.Add(new Vector4(0.5f, 0f, 0.5f, 1f));
				break;
			case 3:
				list.Add(new Vector4(0f, 0f, 1f, 0.5f));
				list.Add(new Vector4(0f, 0.5f, 0.5f, 0.5f));
				list.Add(new Vector4(0.5f, 0.5f, 0.5f, 0.5f));
				break;
			case 4:
				list.Add(new Vector4(0f, 0f, 0.5f, 0.5f));
				list.Add(new Vector4(0.5f, 0f, 0.5f, 0.5f));
				list.Add(new Vector4(0f, 0.5f, 0.5f, 0.5f));
				list.Add(new Vector4(0.5f, 0.5f, 0.5f, 0.5f));
				break;
			}
		}
		if (GameRunner.instance.gameInstances.Count <= 1)
		{
			zoomModifier = 1f;
		}
		else
		{
			zoomModifier = 0.5f;
		}
		Vector4 vector = list[game1.instanceIndex];
		Vector2? vector2 = null;
		if (uiScreen != null)
		{
			vector2 = new Vector2(uiScreen.Width, uiScreen.Height);
		}
		localMultiplayerWindow.X = (int)((float)w * vector.X);
		localMultiplayerWindow.Y = (int)((float)h * vector.Y);
		localMultiplayerWindow.Width = (int)Math.Ceiling((float)w * vector.Z);
		localMultiplayerWindow.Height = (int)Math.Ceiling((float)h * vector.W);
		try
		{
			int width = (int)Math.Ceiling((float)localMultiplayerWindow.Width * (1f / options.zoomLevel));
			int height = (int)Math.Ceiling((float)localMultiplayerWindow.Height * (1f / options.zoomLevel));
			screen = new RenderTarget2D(graphics.GraphicsDevice, width, height, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents)
			{
				Name = "@Game1.screen"
			};
			int width2 = (int)Math.Ceiling((float)localMultiplayerWindow.Width / options.uiScale);
			int height2 = (int)Math.Ceiling((float)localMultiplayerWindow.Height / options.uiScale);
			uiScreen = new RenderTarget2D(graphics.GraphicsDevice, width2, height2, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents)
			{
				Name = "@Game1.uiScreen"
			};
		}
		catch (Exception)
		{
		}
		updateViewportForScreenSizeChange(fullscreenChange: false, localMultiplayerWindow.Width, localMultiplayerWindow.Height);
		if (vector2.HasValue && vector2.Value.X == (float)uiScreen.Width && vector2.Value.Y == (float)uiScreen.Height)
		{
			return;
		}
		PushUIMode();
		textEntry?.gameWindowSizeChanged(oldBounds, new Microsoft.Xna.Framework.Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height));
		foreach (IClickableMenu onScreenMenu in onScreenMenus)
		{
			onScreenMenu.gameWindowSizeChanged(oldBounds, new Microsoft.Xna.Framework.Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height));
		}
		currentMinigame?.changeScreenSize();
		activeClickableMenu?.gameWindowSizeChanged(oldBounds, new Microsoft.Xna.Framework.Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height));
		if (activeClickableMenu?.GetType() == typeof(GameMenu))
		{
			GameMenu obj = activeClickableMenu as GameMenu;
			IClickableMenu currentPage = obj.GetCurrentPage();
			IClickableMenu currentPage2 = ((GameMenu)(activeClickableMenu = new GameMenu(obj.currentTab))).GetCurrentPage();
			if (!(currentPage2 is CollectionsPage collectionsPage))
			{
				if (!(currentPage2 is OptionsPage optionsPage))
				{
					if (currentPage2 is SocialPage socialPage)
					{
						socialPage.postWindowSizeChange(currentPage);
					}
				}
				else
				{
					optionsPage.postWindowSizeChange(currentPage);
				}
			}
			else
			{
				collectionsPage.postWindowSizeChange(currentPage);
			}
		}
		PopUIMode();
	}

	private void Game1_Exiting(object sender, EventArgs e)
	{
		Program.sdk.Shutdown();
	}

	public static void setGameMode(byte mode)
	{
		log.Verbose("setGameMode( '" + GameModeToString(mode) + "' )");
		_gameMode = mode;
		temporaryContent?.Unload();
		if (mode != 0)
		{
			return;
		}
		bool flag = false;
		if (activeClickableMenu != null)
		{
			GameTime gameTime = currentGameTime;
			if (gameTime != null && gameTime.TotalGameTime.TotalSeconds > 10.0)
			{
				flag = true;
			}
		}
		if (game1.instanceIndex <= 0)
		{
			TitleMenu titleMenu = (TitleMenu)(activeClickableMenu = new TitleMenu());
			if (flag)
			{
				titleMenu.skipToTitleButtons();
			}
		}
	}

	public static void updateViewportForScreenSizeChange(bool fullscreenChange, int width, int height)
	{
		forceSnapOnNextViewportUpdate = true;
		if (graphics.GraphicsDevice != null)
		{
			allocateLightmap(width, height);
		}
		width = (int)Math.Ceiling((float)width / options.zoomLevel);
		height = (int)Math.Ceiling((float)height / options.zoomLevel);
		Point point = new Point(viewport.X + viewport.Width / 2, viewport.Y + viewport.Height / 2);
		bool flag = viewport.Width != width || viewport.Height != height;
		viewport = new xTile.Dimensions.Rectangle(point.X - width / 2, point.Y - height / 2, width, height);
		if (currentLocation == null)
		{
			return;
		}
		if (eventUp)
		{
			if (!IsFakedBlackScreen() && currentLocation.IsOutdoors)
			{
				clampViewportToGameMap();
			}
			return;
		}
		if (viewport.X >= 0 || !currentLocation.IsOutdoors || fullscreenChange)
		{
			point = new Point(viewport.X + viewport.Width / 2, viewport.Y + viewport.Height / 2);
			viewport = new xTile.Dimensions.Rectangle(point.X - width / 2, point.Y - height / 2, width, height);
			UpdateViewPort(overrideFreeze: true, point);
		}
		if (flag)
		{
			forceSnapOnNextViewportUpdate = true;
			randomizeRainPositions();
			randomizeDebrisWeatherPositions(debrisWeather);
		}
	}

	public void Instance_Initialize()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		Initialize();
		stopwatch.Stop();
		log.Verbose($"Instance_Initialize() finished, elapsed = '{stopwatch.Elapsed}'");
	}

	public static bool IsFading()
	{
		if (!globalFade && (!fadeIn || !(fadeToBlackAlpha > 0f)))
		{
			if (fadeToBlack)
			{
				return fadeToBlackAlpha < 1f;
			}
			return false;
		}
		return true;
	}

	public static bool IsFakedBlackScreen()
	{
		if (currentMinigame != null)
		{
			return false;
		}
		if (CurrentEvent != null && CurrentEvent.currentCustomEventScript != null)
		{
			return false;
		}
		if (!eventUp)
		{
			return false;
		}
		return (float)(int)Math.Floor((float)new Point(viewport.X + viewport.Width / 2, viewport.Y + viewport.Height / 2).X / 64f) <= -200f;
	}

	private void DoThreadedInitTask(ThreadStart initTask)
	{
		if (ShouldLoadIncrementally)
		{
			Thread thread = new Thread(initTask);
			thread.CurrentCulture = CultureInfo.InvariantCulture;
			thread.Priority = ThreadPriority.Highest;
			thread.Start();
		}
		else
		{
			initTask();
		}
	}

	/// <summary>
	/// Allows the game to perform any initialization it needs to before starting to run.
	/// This is where it can query for any required services and load any non-graphic
	/// related content.  Calling base.Initialize will enumerate through any components
	/// and initialize them as well.
	/// </summary>
	protected override void Initialize()
	{
		keyboardDispatcher = new KeyboardDispatcher(base.Window);
		screenFade = new ScreenFade(onFadeToBlackComplete, onFadedBackInComplete);
		options = new Options();
		options.musicVolumeLevel = 1f;
		options.soundVolumeLevel = 1f;
		otherFarmers = new NetRootDictionary<long, Farmer>();
		DoThreadedInitTask(InitializeSerializers);
		viewport = new xTile.Dimensions.Rectangle(new Size(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
		currentSong = null;
		DoThreadedInitTask(InitializeSounds);
		int width = graphics.GraphicsDevice.Viewport.Width;
		int height = graphics.GraphicsDevice.Viewport.Height;
		screen = new RenderTarget2D(graphics.GraphicsDevice, width, height, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
		allocateLightmap(width, height);
		previousViewportPosition = Vector2.Zero;
		PushUIMode();
		PopUIMode();
		setRichPresence("menus");
	}

	private void InitializeSounds()
	{
		if (base.IsMainInstance)
		{
			try
			{
				string rootDirectory = base.Content.RootDirectory;
				AudioEngine obj = new AudioEngine(Path.Combine(rootDirectory, "XACT", "FarmerSounds.xgs"));
				obj.GetReverbSettings()[18] = 4f;
				obj.GetReverbSettings()[17] = -12f;
				audioEngine = new AudioEngineWrapper(obj);
				waveBank = new WaveBank(audioEngine.Engine, Path.Combine(rootDirectory, "XACT", "Wave Bank.xwb"));
				waveBank1_4 = new WaveBank(audioEngine.Engine, Path.Combine(rootDirectory, "XACT", "Wave Bank(1.4).xwb"));
				soundBank = new SoundBankWrapper(new SoundBank(audioEngine.Engine, Path.Combine(rootDirectory, "XACT", "Sound Bank.xsb")));
			}
			catch (Exception exception)
			{
				log.Error("Game.Initialize() caught exception initializing XACT.", exception);
				audioEngine = new DummyAudioEngine();
				soundBank = new DummySoundBank();
			}
		}
		audioEngine.Update();
		musicCategory = audioEngine.GetCategory("Music");
		soundCategory = audioEngine.GetCategory("Sound");
		ambientCategory = audioEngine.GetCategory("Ambient");
		footstepCategory = audioEngine.GetCategory("Footsteps");
		wind = soundBank.GetCue("wind");
		chargeUpSound = soundBank.GetCue("toolCharge");
		AmbientLocationSounds.InitShared();
		FinishedFirstInitSounds = true;
	}

	private void InitializeSerializers()
	{
		otherFarmers.Serializer = SaveSerializer.GetSerializer(typeof(Farmer));
		if (StartupPreferences.serializer == null)
		{
			StartupPreferences.serializer = SaveSerializer.GetSerializer(typeof(StartupPreferences));
		}
		FinishedFirstInitSerializers = true;
	}

	public static void pauseThenDoFunction(int pauseTime, afterFadeFunction function)
	{
		afterPause = function;
		pauseThenDoFunctionTimer = pauseTime;
	}

	/// <summary>Construct a content manager to read game content files.</summary>
	/// <param name="serviceProvider">The service provider to use to locate services.</param>
	/// <param name="rootDirectory">The root directory to search for content.</param>
	protected internal virtual LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
	{
		return new LocalizedContentManager(serviceProvider, rootDirectory);
	}

	/// <summary>Create an xTile map display device.</summary>
	/// <param name="content">The content manager through which it should load tilesheet textures.</param>
	/// <param name="graphicsDevice">The XNA graphics device.</param>
	protected internal virtual IDisplayDevice CreateDisplayDevice(ContentManager content, GraphicsDevice graphicsDevice)
	{
		return new XnaDisplayDevice(content, graphicsDevice);
	}

	public void Instance_LoadContent()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		LoadContent();
		stopwatch.Stop();
		log.Verbose($"Instance_LoadContent() finished, elapsed = '{stopwatch.Elapsed}'");
	}

	/// <summary>LoadContent will be called once per game and is the place to load all of your content.</summary>
	protected override void LoadContent()
	{
		content = CreateContentManager(base.Content.ServiceProvider, base.Content.RootDirectory);
		xTileContent = CreateContentManager(content.ServiceProvider, content.RootDirectory);
		mapDisplayDevice = CreateDisplayDevice(content, base.GraphicsDevice);
		spriteBatch = new SpriteBatch(base.GraphicsDevice);
		StardewValley.FirstPerson.FirstPersonManager.Instance.Initialize(base.GraphicsDevice);
		netWorldState = new NetRoot<NetWorldState>(new NetWorldState());
		LoadContentEnumerator = GetLoadContentEnumerator();
		if (!ShouldLoadIncrementally)
		{
			while (LoadContentEnumerator.MoveNext())
			{
			}
			LoadContentEnumerator = null;
			AfterLoadContent();
		}
	}

	private void AfterLoadContent()
	{
		saveOnNewDay = true;
		if (gameMode == 4)
		{
			fadeToBlackAlpha = -0.5f;
			fadeIn = true;
		}
		if (random.NextDouble() < 0.7)
		{
			isDebrisWeather = true;
			populateDebrisWeatherArray();
		}
		resetPlayer();
		CueModification.OnStartup();
		setGameMode(0);
	}

	private IEnumerator<int> GetLoadContentEnumerator()
	{
		int step = 0;
		bigCraftableData = DataLoader.BigCraftables(content);
		int num = step + 1;
		step = num;
		yield return num;
		objectData = DataLoader.Objects(content);
		num = step + 1;
		step = num;
		yield return num;
		cropData = DataLoader.Crops(content);
		StardewValley.Pokemon.PokemonItemManager.Initialize(objectData, cropData, CraftingRecipe.craftingRecipes);
		num = step + 1;
		step = num;
		yield return num;
		characterData = DataLoader.Characters(content);
		num = step + 1;
		step = num;
		yield return num;
		pantsData = DataLoader.Pants(content);
		num = step + 1;
		step = num;
		yield return num;
		shirtData = DataLoader.Shirts(content);
		num = step + 1;
		step = num;
		yield return num;
		toolData = DataLoader.Tools(content);
		StardewValley.Pokemon.PokemonItemManager.RegisterTools(toolData);
		num = step + 1;
		step = num;
		yield return num;
		weaponData = DataLoader.Weapons(content);
		num = step + 1;
		step = num;
		yield return num;
		achievements = DataLoader.Achievements(content);
		num = step + 1;
		step = num;
		yield return num;
		buildingData = DataLoader.Buildings(content);
		num = step + 1;
		step = num;
		yield return num;
		farmAnimalData = DataLoader.FarmAnimals(content);
		num = step + 1;
		step = num;
		yield return num;
		floorPathData = DataLoader.FloorsAndPaths(content);
		num = step + 1;
		step = num;
		yield return num;
		fruitTreeData = DataLoader.FruitTrees(content);
		num = step + 1;
		step = num;
		yield return num;
		locationData = DataLoader.Locations(content);
		num = step + 1;
		step = num;
		yield return num;
		locationContextData = DataLoader.LocationContexts(content);
		num = step + 1;
		step = num;
		yield return num;
		petData = DataLoader.Pets(content);
		num = step + 1;
		step = num;
		yield return num;
		NPCGiftTastes = DataLoader.NpcGiftTastes(content);
		num = step + 1;
		step = num;
		yield return num;
		CraftingRecipe.InitShared();
		num = step + 1;
		step = num;
		yield return num;
		ItemRegistry.ResetCache();
		num = step + 1;
		step = num;
		yield return num;
		jukeboxTrackData = new Dictionary<string, JukeboxTrackData>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, JukeboxTrackData> item in DataLoader.JukeboxTracks(content))
		{
			if (!jukeboxTrackData.TryAdd(item.Key, item.Value))
			{
				log.Warn("Ignored duplicate ID '" + item.Key + "' in Data/JukeboxTracks.");
			}
		}
		num = step + 1;
		step = num;
		yield return num;
		concessionsSpriteSheet = content.Load<Texture2D>("LooseSprites\\Concessions");
		num = step + 1;
		step = num;
		yield return num;
		birdsSpriteSheet = content.Load<Texture2D>("LooseSprites\\birds");
		num = step + 1;
		step = num;
		yield return num;
		daybg = content.Load<Texture2D>("LooseSprites\\daybg");
		num = step + 1;
		step = num;
		yield return num;
		nightbg = content.Load<Texture2D>("LooseSprites\\nightbg");
		num = step + 1;
		step = num;
		yield return num;
		menuTexture = content.Load<Texture2D>("Maps\\MenuTiles");
		num = step + 1;
		step = num;
		yield return num;
		uncoloredMenuTexture = content.Load<Texture2D>("Maps\\MenuTilesUncolored");
		num = step + 1;
		step = num;
		yield return num;
		lantern = content.Load<Texture2D>("LooseSprites\\Lighting\\lantern");
		num = step + 1;
		step = num;
		yield return num;
		windowLight = content.Load<Texture2D>("LooseSprites\\Lighting\\windowLight");
		num = step + 1;
		step = num;
		yield return num;
		sconceLight = content.Load<Texture2D>("LooseSprites\\Lighting\\sconceLight");
		num = step + 1;
		step = num;
		yield return num;
		cauldronLight = content.Load<Texture2D>("LooseSprites\\Lighting\\greenLight");
		num = step + 1;
		step = num;
		yield return num;
		indoorWindowLight = content.Load<Texture2D>("LooseSprites\\Lighting\\indoorWindowLight");
		num = step + 1;
		step = num;
		yield return num;
		shadowTexture = content.Load<Texture2D>("LooseSprites\\shadow");
		num = step + 1;
		step = num;
		yield return num;
		mouseCursors = content.Load<Texture2D>("LooseSprites\\Cursors");
		num = step + 1;
		step = num;
		yield return num;
		mouseCursors2 = content.Load<Texture2D>("LooseSprites\\Cursors2");
		num = step + 1;
		step = num;
		yield return num;
		mouseCursors_1_6 = content.Load<Texture2D>("LooseSprites\\Cursors_1_6");
		num = step + 1;
		step = num;
		yield return num;
		giftboxTexture = content.Load<Texture2D>("LooseSprites\\Giftbox");
		num = step + 1;
		step = num;
		yield return num;
		controllerMaps = content.Load<Texture2D>("LooseSprites\\ControllerMaps");
		num = step + 1;
		step = num;
		yield return num;
		animations = content.Load<Texture2D>("TileSheets\\animations");
		num = step + 1;
		step = num;
		yield return num;
		objectSpriteSheet = content.Load<Texture2D>("Maps\\springobjects");
		num = step + 1;
		step = num;
		yield return num;
		objectSpriteSheet_2 = content.Load<Texture2D>("TileSheets\\Objects_2");
		num = step + 1;
		step = num;
		yield return num;
		bobbersTexture = content.Load<Texture2D>("TileSheets\\bobbers");
		num = step + 1;
		step = num;
		yield return num;
		cropSpriteSheet = content.Load<Texture2D>("TileSheets\\crops");
		num = step + 1;
		step = num;
		yield return num;
		emoteSpriteSheet = content.Load<Texture2D>("TileSheets\\emotes");
		num = step + 1;
		step = num;
		yield return num;
		debrisSpriteSheet = content.Load<Texture2D>("TileSheets\\debris");
		num = step + 1;
		step = num;
		yield return num;
		bigCraftableSpriteSheet = content.Load<Texture2D>("TileSheets\\Craftables");
		num = step + 1;
		step = num;
		yield return num;
		rainTexture = content.Load<Texture2D>("TileSheets\\rain");
		num = step + 1;
		step = num;
		yield return num;
		buffsIcons = content.Load<Texture2D>("TileSheets\\BuffsIcons");
		num = step + 1;
		step = num;
		yield return num;
		Tool.weaponsTexture = content.Load<Texture2D>("TileSheets\\weapons");
		num = step + 1;
		step = num;
		yield return num;
		FarmerRenderer.hairStylesTexture = content.Load<Texture2D>("Characters\\Farmer\\hairstyles");
		num = step + 1;
		step = num;
		yield return num;
		FarmerRenderer.shirtsTexture = content.Load<Texture2D>("Characters\\Farmer\\shirts");
		num = step + 1;
		step = num;
		yield return num;
		FarmerRenderer.pantsTexture = content.Load<Texture2D>("Characters\\Farmer\\pants");
		num = step + 1;
		step = num;
		yield return num;
		FarmerRenderer.hatsTexture = content.Load<Texture2D>("Characters\\Farmer\\hats");
		num = step + 1;
		step = num;
		yield return num;
		FarmerRenderer.accessoriesTexture = content.Load<Texture2D>("Characters\\Farmer\\accessories");
		num = step + 1;
		step = num;
		yield return num;
		MapSeat.mapChairTexture = content.Load<Texture2D>("TileSheets\\ChairTiles");
		num = step + 1;
		step = num;
		yield return num;
		SpriteText.spriteTexture = content.Load<Texture2D>("LooseSprites\\font_bold");
		num = step + 1;
		step = num;
		yield return num;
		SpriteText.coloredTexture = content.Load<Texture2D>("LooseSprites\\font_colored");
		num = step + 1;
		step = num;
		yield return num;
		Projectile.projectileSheet = content.Load<Texture2D>("TileSheets\\Projectiles");
		num = step + 1;
		step = num;
		yield return num;
		Color[] data = new Color[1] { Color.White };
		for (int i = 0; i < dynamicPixelRects.Length; i++)
		{
			dynamicPixelRects[i] = new Texture2D(base.GraphicsDevice, 1, 1, mipmap: false, SurfaceFormat.Color)
			{
				Name = $"@{"Game1"}.{"dynamicPixelRects"}[{i}]"
			};
			dynamicPixelRects[i].SetData(data);
		}
		fadeToBlackRect = dynamicPixelRects[0];
		staminaRect = dynamicPixelRects[1];
		lightingRect = dynamicPixelRects[2];
		num = step + 1;
		step = num;
		yield return num;
		onScreenMenus.Clear();
		onScreenMenus.Add(dayTimeMoneyBox = new DayTimeMoneyBox());
		onScreenMenus.Add(new Toolbar());
		onScreenMenus.Add(buffsDisplay = new BuffsDisplay());
		num = step + 1;
		step = num;
		yield return num;
		for (int j = 0; j < 70; j++)
		{
			rainDrops[j] = new RainDrop(random.Next(viewport.Width), random.Next(viewport.Height), random.Next(4), random.Next(70));
		}
		num = step + 1;
		step = num;
		yield return num;
		dialogueWidth = Math.Min(1024, graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - 256);
		dialogueFont = content.Load<SpriteFont>("Fonts\\SpriteFont1");
		dialogueFont.LineSpacing = 42;
		num = step + 1;
		step = num;
		yield return num;
		smallFont = content.Load<SpriteFont>("Fonts\\SmallFont");
		smallFont.LineSpacing = 28;
		num = step + 1;
		step = num;
		yield return num;
		tinyFont = content.Load<SpriteFont>("Fonts\\tinyFont");
		num = step + 1;
		step = num;
		yield return num;
		_shortDayDisplayName[0] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3042");
		num = step + 1;
		step = num;
		yield return num;
		_shortDayDisplayName[1] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3043");
		num = step + 1;
		step = num;
		yield return num;
		_shortDayDisplayName[2] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3044");
		num = step + 1;
		step = num;
		yield return num;
		_shortDayDisplayName[3] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3045");
		num = step + 1;
		step = num;
		yield return num;
		_shortDayDisplayName[4] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3046");
		num = step + 1;
		step = num;
		yield return num;
		_shortDayDisplayName[5] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3047");
		num = step + 1;
		step = num;
		yield return num;
		_shortDayDisplayName[6] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3048");
		yield return step + 1;
	}

	public static void resetPlayer()
	{
		List<Item> initialTools = Farmer.initialTools();
		player = new Farmer(new FarmerSprite(null), new Vector2(192f, 192f), 1, "", initialTools, isMale: true);
	}

	public static void resetVariables()
	{
		xLocationAfterWarp = 0;
		yLocationAfterWarp = 0;
		gameTimeInterval = 0;
		currentQuestionChoice = 0;
		currentDialogueCharacterIndex = 0;
		dialogueTypingInterval = 0;
		dayOfMonth = 0;
		year = 1;
		timeOfDay = 600;
		timeOfDayAfterFade = -1;
		facingDirectionAfterWarp = 0;
		dialogueWidth = 0;
		facingDirectionAfterWarp = 0;
		mouseClickPolling = 0;
		weatherIcon = 0;
		hitShakeTimer = 0;
		staminaShakeTimer = 0;
		pauseThenDoFunctionTimer = 0;
		weatherForTomorrow = "Sun";
	}

	/// <summary>Play a game sound for the local player.</summary>
	/// <param name="cueName">The sound ID to play.</param>
	/// <param name="pitch">The pitch modifier to apply, or <c>null</c> for the default pitch.</param>
	/// <returns>Returns whether the cue exists and was started successfully.</returns>
	/// <remarks>To play audio in a specific location, see <see cref="M:StardewValley.GameLocation.playSound(System.String,System.Nullable{Microsoft.Xna.Framework.Vector2},System.Nullable{System.Int32},StardewValley.Audio.SoundContext)" /> or <see cref="M:StardewValley.GameLocation.localSound(System.String,System.Nullable{Microsoft.Xna.Framework.Vector2},System.Nullable{System.Int32},StardewValley.Audio.SoundContext)" /> instead.</remarks>
	public static bool playSound(string cueName, int? pitch = null)
	{
		ICue cue;
		return sounds.PlayLocal(cueName, null, null, pitch, SoundContext.Default, out cue);
	}

	/// <summary>Play a game sound for the local player.</summary>
	/// <param name="cueName">The sound ID to play.</param>
	/// <param name="cue">The cue instance that was started, or a no-op cue if it failed.</param>
	/// <returns>Returns whether the cue exists and was started successfully.</returns>
	/// <remarks>To play audio in a specific location, see <see cref="M:StardewValley.GameLocation.playSound(System.String,System.Nullable{Microsoft.Xna.Framework.Vector2},System.Nullable{System.Int32},StardewValley.Audio.SoundContext)" /> or <see cref="M:StardewValley.GameLocation.localSound(System.String,System.Nullable{Microsoft.Xna.Framework.Vector2},System.Nullable{System.Int32},StardewValley.Audio.SoundContext)" /> instead.</remarks>
	public static bool playSound(string cueName, out ICue cue)
	{
		return sounds.PlayLocal(cueName, null, null, null, SoundContext.Default, out cue);
	}

	/// <summary>Play a game sound for the local player.</summary>
	/// <param name="cueName">The sound ID to play.</param>
	/// <param name="pitch">The pitch modifier to apply.</param>
	/// <param name="cue">The cue instance that was started, or a no-op cue if it failed.</param>
	/// <returns>Returns whether the cue exists and was started successfully.</returns>
	/// <remarks>To play audio in a specific location, see <see cref="M:StardewValley.GameLocation.playSound(System.String,System.Nullable{Microsoft.Xna.Framework.Vector2},System.Nullable{System.Int32},StardewValley.Audio.SoundContext)" /> or <see cref="M:StardewValley.GameLocation.localSound(System.String,System.Nullable{Microsoft.Xna.Framework.Vector2},System.Nullable{System.Int32},StardewValley.Audio.SoundContext)" /> instead.</remarks>
	public static bool playSound(string cueName, int pitch, out ICue cue)
	{
		return sounds.PlayLocal(cueName, null, null, pitch, SoundContext.Default, out cue);
	}

	public static void setRichPresence(string friendlyName, object argument = null)
	{
		if (friendlyName == null)
		{
			return;
		}
		switch (friendlyName.Length)
		{
		case 8:
			switch (friendlyName[0])
			{
			case 'l':
				if (friendlyName == "location")
				{
					debugPresenceString = $"At {argument}";
				}
				break;
			case 'f':
				if (friendlyName == "festival")
				{
					debugPresenceString = $"At {argument}";
				}
				break;
			case 'm':
				if (friendlyName == "minigame")
				{
					debugPresenceString = $"Playing {argument}";
				}
				break;
			case 'e':
				if (friendlyName == "earnings")
				{
					debugPresenceString = $"Made {argument}g last night";
				}
				break;
			}
			break;
		case 7:
			switch (friendlyName[0])
			{
			case 'f':
				if (friendlyName == "fishing")
				{
					debugPresenceString = $"Fishing at {argument}";
				}
				break;
			case 'w':
				if (friendlyName == "wedding")
				{
					debugPresenceString = $"Getting married to {argument}";
				}
				break;
			}
			break;
		case 5:
			if (friendlyName == "menus")
			{
				debugPresenceString = "In menus";
			}
			break;
		case 9:
			if (friendlyName == "giantcrop")
			{
				debugPresenceString = $"Just harvested a Giant {argument}";
			}
			break;
		case 6:
			break;
		}
	}

	public static void GenerateBundles(BundleType bundle_type, bool use_seed = true)
	{
		if (bundle_type == BundleType.Remixed)
		{
			Random rng = (use_seed ? Utility.CreateRandom((double)uniqueIDForThisGame * 9.0) : new Random());
			Dictionary<string, string> bundleData = new BundleGenerator().Generate(DataLoader.RandomBundles(content), rng);
			netWorldState.Value.SetBundleData(bundleData);
		}
		else
		{
			netWorldState.Value.SetBundleData(DataLoader.Bundles(content));
		}
	}

	public void SetNewGameOption<T>(string key, T val)
	{
		newGameSetupOptions[key] = val;
	}

	public T GetNewGameOption<T>(string key)
	{
		if (!newGameSetupOptions.TryGetValue(key, out var value))
		{
			return default(T);
		}
		return (T)value;
	}

	public virtual void loadForNewGame(bool loadedGame = false)
	{
		if (startingGameSeed.HasValue)
		{
			uniqueIDForThisGame = startingGameSeed.Value;
		}
		specialCurrencyDisplay = new SpecialCurrencyDisplay();
		flushLocationLookup();
		locations.Clear();
		mailbox.Clear();
		currentLightSources.Clear();
		questionChoices.Clear();
		hudMessages.Clear();
		weddingToday = false;
		timeOfDay = 600;
		season = Season.Spring;
		if (!loadedGame)
		{
			year = 1;
			StardewValley.Pokemon.PlayerParty.Instance.ResetForNewGame();
			StardewValley.Pokemon.PokemonStorage.Instance.ResetForNewGame();
			StardewValley.Pokemon.PokemonPokedex.Instance.ResetForNewGame();
		}
		dayOfMonth = 0;
		isQuestion = false;
		nonWarpFade = false;
		newDay = false;
		eventUp = false;
		viewportFreeze = false;
		eventOver = false;
		screenGlow = false;
		screenGlowHold = false;
		screenGlowUp = false;
		isRaining = false;
		wasGreenRain = false;
		killScreen = false;
		messagePause = false;
		isDebrisWeather = false;
		weddingToday = false;
		exitToTitle = false;
		dialogueUp = false;
		postExitToTitleCallback = null;
		displayHUD = true;
		messageAfterPause = "";
		samBandName = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2156");
		background = null;
		currentCursorTile = Vector2.Zero;
		if (!loadedGame)
		{
			lastAppliedSaveFix = SaveMigrator.LatestSaveFix;
		}
		resetVariables();
		player.team.sharedDailyLuck.Value = 0.001;
		if (!loadedGame)
		{
			options = new Options();
			options.LoadDefaultOptions();
			initializeVolumeLevels();
		}
		game1.CheckGamepadMode();
		onScreenMenus.Add(chatBox = new ChatBox());
		outdoorLight = Color.White;
		ambientLight = Color.White;
		UpdateDishOfTheDay();
		locations.Clear();
		Farm farm = new Farm("Maps\\" + Farm.getMapNameFromTypeInt(whichFarm), "Farm");
		locations.Add(farm);
		AddLocations();
		foreach (GameLocation location in locations)
		{
			location.AddDefaultBuildings();
		}
		forceSnapOnNextViewportUpdate = true;
		farm.onNewGame();
		if (!loadedGame)
		{
			foreach (GameLocation location2 in locations)
			{
				if (location2 is IslandLocation islandLocation)
				{
					islandLocation.AddAdditionalWalnutBushes();
				}
			}
		}
		if (!loadedGame)
		{
			hooks.CreatedInitialLocations();
		}
		else
		{
			hooks.SaveAddedLocations();
		}
		if (!loadedGame)
		{
			AddNPCs();
		}
		WarpPathfindingCache.PopulateCache();
		if (!loadedGame)
		{
			GenerateBundles(bundleType);
			foreach (string value in netWorldState.Value.BundleData.Values)
			{
				string[] array = ArgUtility.SplitBySpace(value.Split('/')[2]);
				if (!game1.GetNewGameOption<bool>("YearOneCompletable"))
				{
					continue;
				}
				for (int i = 0; i < array.Length; i += 3)
				{
					if (array[i] == "266")
					{
						int num = (16 - 2) * 2;
						num += 3;
						Random random = Utility.CreateRandom((double)uniqueIDForThisGame * 12.0);
						netWorldState.Value.VisitsUntilY1Guarantee = random.Next(2, num);
					}
				}
			}
			netWorldState.Value.ShuffleMineChests = game1.GetNewGameOption<MineChestType>("MineChests");
			if (game1.newGameSetupOptions.ContainsKey("SpawnMonstersAtNight"))
			{
				spawnMonstersAtNight = game1.GetNewGameOption<bool>("SpawnMonstersAtNight");
			}
		}
		player.ConvertClothingOverrideToClothesItems();
		player.addQuest("9");
		RefreshQuestOfTheDay();
		player.currentLocation = RequireLocation("FarmHouse");
		player.gameVersion = version;
		hudMessages.Clear();
		hasLoadedGame = true;
		setGraphicsForSeason(onLoad: true);
		if (!loadedGame)
		{
			_setSaveName = false;
		}
		game1.newGameSetupOptions.Clear();
		updateCellarAssignments();
		if (!loadedGame && netWorldState != null && netWorldState.Value != null)
		{
			netWorldState.Value.RegisterSpecialCurrencies();
		}
	}

	public bool IsLocalCoopJoinable()
	{
		if (GameRunner.instance.gameInstances.Count >= GameRunner.instance.GetMaxSimultaneousPlayers())
		{
			return false;
		}
		if (IsClient)
		{
			return false;
		}
		return true;
	}

	public static void StartLocalMultiplayerIfNecessary()
	{
		if (multiplayerMode == 0)
		{
			log.Verbose("Starting multiplayer server for local multiplayer...");
			multiplayerMode = 2;
			if (server == null)
			{
				multiplayer.StartLocalMultiplayerServer();
			}
		}
	}

	public static void EndLocalMultiplayer()
	{
	}

	public static void UpdatePassiveFestivalStates()
	{
		netWorldState.Value.ActivePassiveFestivals.Clear();
		foreach (KeyValuePair<string, PassiveFestivalData> item in DataLoader.PassiveFestivals(content))
		{
			string key = item.Key;
			PassiveFestivalData value = item.Value;
			if (dayOfMonth >= value.StartDay && dayOfMonth <= value.EndDay && season == value.Season && GameStateQuery.CheckConditions(value.Condition))
			{
				netWorldState.Value.ActivePassiveFestivals.Add(key);
			}
		}
	}

	public void Instance_UnloadContent()
	{
		UnloadContent();
	}

	/// <summary>
	/// UnloadContent will be called once per game and is the place to unload
	/// all content.
	/// </summary>
	protected override void UnloadContent()
	{
		base.UnloadContent();
		spriteBatch.Dispose();
		content.Unload();
		xTileContent.Unload();
		server?.stopServer();
	}

	public static void showRedMessage(string message, bool playSound = true)
	{
		addHUDMessage(new HUDMessage(message, 3));
		if (!message.Contains("Inventory") && playSound)
		{
			Game1.playSound("cancel");
		}
		else if (player.mailReceived.Add("BackpackTip"))
		{
			addMailForTomorrow("pierreBackpack");
		}
	}

	public static void showRedMessageUsingLoadString(string loadString, bool playSound = true)
	{
		showRedMessage(content.LoadString(loadString), playSound);
	}

	public static bool didPlayerJustLeftClick(bool ignoreNonMouseHeldInput = false)
	{
		if (input.GetMouseState().LeftButton == ButtonState.Pressed && oldMouseState.LeftButton != ButtonState.Pressed)
		{
			return true;
		}
		if (input.GetGamePadState().Buttons.X == ButtonState.Pressed && (!ignoreNonMouseHeldInput || !oldPadState.IsButtonDown(Buttons.X)))
		{
			return true;
		}
		if (isOneOfTheseKeysDown(input.GetKeyboardState(), options.useToolButton) && (!ignoreNonMouseHeldInput || areAllOfTheseKeysUp(oldKBState, options.useToolButton)))
		{
			return true;
		}
		return false;
	}

	public static bool didPlayerJustRightClick(bool ignoreNonMouseHeldInput = false)
	{
		if (input.GetMouseState().RightButton == ButtonState.Pressed && oldMouseState.RightButton != ButtonState.Pressed)
		{
			return true;
		}
		if (input.GetGamePadState().Buttons.A == ButtonState.Pressed && (!ignoreNonMouseHeldInput || !oldPadState.IsButtonDown(Buttons.A)))
		{
			return true;
		}
		if (isOneOfTheseKeysDown(input.GetKeyboardState(), options.actionButton) && (!ignoreNonMouseHeldInput || !isOneOfTheseKeysDown(oldKBState, options.actionButton)))
		{
			return true;
		}
		return false;
	}

	public static bool didPlayerJustClickAtAll(bool ignoreNonMouseHeldInput = false)
	{
		if (!didPlayerJustLeftClick(ignoreNonMouseHeldInput))
		{
			return didPlayerJustRightClick(ignoreNonMouseHeldInput);
		}
		return true;
	}

	public static void showGlobalMessage(string message)
	{
		addHUDMessage(HUDMessage.ForCornerTextbox(message));
	}

	public static void globalFadeToBlack(afterFadeFunction afterFade = null, float fadeSpeed = 0.02f)
	{
		screenFade.GlobalFadeToBlack(afterFade, fadeSpeed);
	}

	public static void globalFadeToClear(afterFadeFunction afterFade = null, float fadeSpeed = 0.02f)
	{
		screenFade.GlobalFadeToClear(afterFade, fadeSpeed);
	}

	public void CheckGamepadMode()
	{
		bool gamepadControls = options.gamepadControls;
		switch (options.gamepadMode)
		{
		case Options.GamepadModes.ForceOn:
			options.gamepadControls = true;
			return;
		case Options.GamepadModes.ForceOff:
			options.gamepadControls = false;
			return;
		}
		MouseState mouseState = input.GetMouseState();
		KeyboardState keyboardState = GetKeyboardState();
		GamePadState gamePadState = input.GetGamePadState();
		bool flag = false;
		if ((mouseState.LeftButton == ButtonState.Pressed || mouseState.MiddleButton == ButtonState.Pressed || mouseState.RightButton == ButtonState.Pressed || mouseState.ScrollWheelValue != _oldScrollWheelValue || ((mouseState.X != _oldMousePosition.X || mouseState.Y != _oldMousePosition.Y) && lastCursorMotionWasMouse) || keyboardState.GetPressedKeys().Length != 0) && (keyboardState.GetPressedKeys().Length != 1 || keyboardState.GetPressedKeys()[0] != Keys.Pause))
		{
			flag = true;
			if (Program.sdk is SteamHelper steamHelper && steamHelper.IsRunningOnSteamDeck())
			{
				flag = false;
			}
		}
		_oldScrollWheelValue = mouseState.ScrollWheelValue;
		_oldMousePosition.X = mouseState.X;
		_oldMousePosition.Y = mouseState.Y;
		bool flag2 = isAnyGamePadButtonBeingPressed() || isDPadPressed() || isGamePadThumbstickInMotion() || gamePadState.Triggers.Left != 0f || gamePadState.Triggers.Right != 0f;
		if (_oldGamepadConnectedState != gamePadState.IsConnected)
		{
			_oldGamepadConnectedState = gamePadState.IsConnected;
			if (_oldGamepadConnectedState)
			{
				options.gamepadControls = true;
				showGlobalMessage(content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2574"));
			}
			else
			{
				options.gamepadControls = false;
				if (instancePlayerOneIndex != (PlayerIndex)(-1))
				{
					showGlobalMessage(content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2575"));
					if (CanShowPauseMenu() && activeClickableMenu == null)
					{
						activeClickableMenu = new GameMenu();
					}
				}
			}
		}
		if (flag && options.gamepadControls)
		{
			options.gamepadControls = false;
		}
		if (!options.gamepadControls && flag2)
		{
			options.gamepadControls = true;
		}
		if (gamepadControls == options.gamepadControls || !options.gamepadControls)
		{
			return;
		}
		lastMousePositionBeforeFade = new Point(localMultiplayerWindow.Width / 2, localMultiplayerWindow.Height / 2);
		if (activeClickableMenu != null)
		{
			activeClickableMenu.setUpForGamePadMode();
			if (options.SnappyMenus)
			{
				activeClickableMenu.populateClickableComponentList();
				activeClickableMenu.snapToDefaultClickableComponent();
			}
		}
		timerUntilMouseFade = 0;
	}

	public void Instance_Update(GameTime gameTime)
	{
		Update(gameTime);
	}

	protected override void Update(GameTime gameTime)
	{
		GameTime gameTime2 = gameTime;
		DebugTools.BeforeGameUpdate(this, ref gameTime2);
		input.UpdateStates();
		StardewValley.FirstPerson.FirstPersonManager.Instance.Update(gameTime);
		if (input.GetGamePadState().IsButtonDown(Buttons.RightStick))
		{
			rightStickHoldTime += gameTime.ElapsedGameTime.Milliseconds;
		}
		GameMenu.bundleItemHovered = false;
		_update(gameTime2);
		if (IsMultiplayer && player != null)
		{
			player.requestingTimePause.Value = !shouldTimePass(LocalMultiplayer.IsLocalMultiplayer(is_local_only: true));
			if (IsMasterGame)
			{
				bool flag = false;
				if (LocalMultiplayer.IsLocalMultiplayer(is_local_only: true))
				{
					flag = true;
					foreach (Farmer onlineFarmer in getOnlineFarmers())
					{
						if (!onlineFarmer.requestingTimePause.Value)
						{
							flag = false;
							break;
						}
					}
				}
				netWorldState.Value.IsTimePaused = flag;
			}
		}
		Rumble.update(gameTime.ElapsedGameTime.Milliseconds);
		if (options.gamepadControls && thumbstickMotionMargin > 0)
		{
			thumbstickMotionMargin -= gameTime.ElapsedGameTime.Milliseconds;
		}
		if (!input.GetGamePadState().IsButtonDown(Buttons.RightStick))
		{
			rightStickHoldTime = 0;
		}
		base.Update(gameTime);
	}

	public void Instance_OnActivated(object sender, EventArgs args)
	{
		OnActivated(sender, args);
	}

	protected override void OnActivated(object sender, EventArgs args)
	{
		base.OnActivated(sender, args);
		_activatedTick = ticks + 1;
		input.IgnoreKeys(GetKeyboardState().GetPressedKeys());
	}

	public bool HasKeyboardFocus()
	{
		if (keyboardFocusInstance == null)
		{
			return base.IsMainInstance;
		}
		return keyboardFocusInstance == this;
	}

	/// <summary>
	/// Allows the game to run logic such as updating the world,
	/// checking for collisions, gathering input, and playing audio.
	/// </summary>
	/// <param name="gameTime">Provides a snapshot of timing values.</param>
	private void _update(GameTime gameTime)
	{
		if (graphics.GraphicsDevice == null)
		{
			return;
		}
		bool flag = false;
		gameModeTicks++;
		if (options != null && !takingMapScreenshot)
		{
			if (options.baseUIScale != options.desiredUIScale)
			{
				if (options.desiredUIScale < 0f)
				{
					options.desiredUIScale = options.desiredBaseZoomLevel;
				}
				options.baseUIScale = options.desiredUIScale;
				flag = true;
			}
			if (options.desiredBaseZoomLevel != options.baseZoomLevel)
			{
				options.baseZoomLevel = options.desiredBaseZoomLevel;
				forceSnapOnNextViewportUpdate = true;
				flag = true;
			}
		}
		if (flag)
		{
			refreshWindowSettings();
		}
		if (!ShouldLoadIncrementally)
		{
			CheckGamepadMode();
		}
		FarmAnimal.NumPathfindingThisTick = 0;
		options.reApplySetOptions();
		if (toggleFullScreen)
		{
			toggleFullscreen();
			toggleFullScreen = false;
		}
		input.Update();
		if (frameByFrame)
		{
			if (GetKeyboardState().IsKeyDown(Keys.Escape) && oldKBState.IsKeyUp(Keys.Escape))
			{
				frameByFrame = false;
			}
			if (!GetKeyboardState().IsKeyDown(Keys.G) || !oldKBState.IsKeyUp(Keys.G))
			{
				oldKBState = GetKeyboardState();
				return;
			}
		}
		if (client != null && client.timedOut)
		{
			multiplayer.clientRemotelyDisconnected(client.pendingDisconnect);
		}
		if (_newDayTask != null)
		{
			if (_newDayTask.Status == TaskStatus.Created)
			{
				hooks.StartTask(_newDayTask, "NewDay");
			}
			if (_newDayTask.Status >= TaskStatus.RanToCompletion)
			{
				if (_newDayTask.IsFaulted)
				{
					Exception baseException = _newDayTask.Exception.GetBaseException();
					if (!IsMasterGame)
					{
						if (baseException is AbortNetSynchronizerException)
						{
							log.Verbose("_newDayTask failed: client lost connection to the server");
						}
						else
						{
							log.Error("Client _newDayTask failed with an exception:", baseException);
						}
						multiplayer.clientRemotelyDisconnected(Multiplayer.DisconnectType.ClientTimeout);
						_newDayTask = null;
						Utility.CollectGarbage();
						return;
					}
					log.Error("_newDayTask failed with an exception:", baseException);
					throw new Exception($"Error on new day: \n---------------\n{baseException}\n---------------\n");
				}
				_newDayTask = null;
				Utility.CollectGarbage();
			}
			UpdateChatBox();
			return;
		}
		if (isLocalMultiplayerNewDayActive)
		{
			UpdateChatBox();
			return;
		}
		if (IsSaving)
		{
			PushUIMode();
			activeClickableMenu?.update(gameTime);
			if (overlayMenu != null)
			{
				overlayMenu.update(gameTime);
				if (overlayMenu == null)
				{
					PopUIMode();
					return;
				}
			}
			PopUIMode();
			UpdateChatBox();
			return;
		}
		if (exitToTitle)
		{
			exitToTitle = false;
			CleanupReturningToTitle();
			Utility.CollectGarbage();
			postExitToTitleCallback?.Invoke();
		}
		SetFreeCursorElapsed((float)gameTime.ElapsedGameTime.TotalSeconds);
		Program.sdk.Update();
		if (game1.IsMainInstance)
		{
			keyboardFocusInstance = game1;
			foreach (Game1 gameInstance in GameRunner.instance.gameInstances)
			{
				if (gameInstance.instanceKeyboardDispatcher.Subscriber != null && gameInstance.instanceTextEntry != null)
				{
					keyboardFocusInstance = gameInstance;
					break;
				}
			}
		}
		if (base.IsMainInstance)
		{
			int displayIndex = base.Window.GetDisplayIndex();
			if (_lastUsedDisplay != -1 && _lastUsedDisplay != displayIndex)
			{
				StartupPreferences startupPreferences = new StartupPreferences();
				startupPreferences.loadPreferences(async: false, applyLanguage: false);
				startupPreferences.displayIndex = displayIndex;
				startupPreferences.savePreferences(async: false);
			}
			_lastUsedDisplay = displayIndex;
		}
		if (HasKeyboardFocus())
		{
			keyboardDispatcher.Poll();
		}
		else
		{
			keyboardDispatcher.Discard();
		}
		if (gameMode == 6)
		{
			multiplayer.UpdateLoading();
		}
		if (gameMode == 3)
		{
			multiplayer.UpdateEarly();
			dedicatedServer.Tick();
			if (player?.team != null)
			{
				player.team.Update();
			}
		}
		if ((paused || (!IsActiveNoOverlay && Program.releaseBuild)) && (options == null || options.pauseWhenOutOfFocus || paused) && multiplayerMode == 0)
		{
			UpdateChatBox();
			return;
		}
		if (quit)
		{
			Exit();
		}
		currentGameTime = gameTime;
		if (gameMode != 11 && !ShouldLoadIncrementally)
		{
			ticks++;
			if (IsActiveNoOverlay)
			{
				checkForEscapeKeys();
			}
			updateMusic();
			updateRaindropPosition();
			if (globalFade)
			{
				screenFade.UpdateGlobalFade();
			}
			else if (pauseThenDoFunctionTimer > 0)
			{
				freezeControls = true;
				pauseThenDoFunctionTimer -= gameTime.ElapsedGameTime.Milliseconds;
				if (pauseThenDoFunctionTimer <= 0)
				{
					freezeControls = false;
					afterPause?.Invoke();
				}
			}
			if (options.gamepadControls && (activeClickableMenu?.shouldClampGamePadCursor() ?? false))
			{
				Point mousePositionRaw = getMousePositionRaw();
				Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, localMultiplayerWindow.Width, localMultiplayerWindow.Height);
				if (mousePositionRaw.X < rectangle.X)
				{
					mousePositionRaw.X = rectangle.X;
				}
				else if (mousePositionRaw.X > rectangle.Right)
				{
					mousePositionRaw.X = rectangle.Right;
				}
				if (mousePositionRaw.Y < rectangle.Y)
				{
					mousePositionRaw.Y = rectangle.Y;
				}
				else if (mousePositionRaw.Y > rectangle.Bottom)
				{
					mousePositionRaw.Y = rectangle.Bottom;
				}
				setMousePositionRaw(mousePositionRaw.X, mousePositionRaw.Y);
			}
			if (gameMode == 3 || gameMode == 2)
			{
				if (!warpingForForcedRemoteEvent && !eventUp && !dialogueUp && remoteEventQueue.Count > 0 && player != null && player.isCustomized.Value && (!fadeIn || !(fadeToBlackAlpha > 0f)))
				{
					if (activeClickableMenu != null)
					{
						activeClickableMenu.emergencyShutDown();
						exitActiveMenu();
					}
					else if (currentMinigame != null && currentMinigame.forceQuit())
					{
						currentMinigame = null;
					}
					if (activeClickableMenu == null && currentMinigame == null && player.freezePause <= 0)
					{
						Action action = remoteEventQueue[0];
						remoteEventQueue.RemoveAt(0);
						action();
					}
				}
				player.millisecondsPlayed += (uint)gameTime.ElapsedGameTime.Milliseconds;
				bool flag2 = true;
				if (currentMinigame != null && !HostPaused)
				{
					if (pauseTime > 0f)
					{
						updatePause(gameTime);
					}
					if (fadeToBlack)
					{
						screenFade.UpdateFadeAlpha(gameTime);
						if (fadeToBlackAlpha >= 1f)
						{
							fadeToBlack = false;
						}
					}
					else
					{
						if (thumbstickMotionMargin > 0)
						{
							thumbstickMotionMargin -= gameTime.ElapsedGameTime.Milliseconds;
						}
						KeyboardState keyboardState = default(KeyboardState);
						MouseState mouseState = default(MouseState);
						GamePadState padState = default(GamePadState);
						if (base.IsActive)
						{
							keyboardState = GetKeyboardState();
							mouseState = input.GetMouseState();
							padState = input.GetGamePadState();
							if ((chatBox?.isActive() ?? false) || textEntry != null)
							{
								keyboardState = default(KeyboardState);
								padState = default(GamePadState);
							}
							else
							{
								Keys[] pressedKeys = keyboardState.GetPressedKeys();
								foreach (Keys keys in pressedKeys)
								{
									if (!oldKBState.IsKeyDown(keys) && currentMinigame != null)
									{
										currentMinigame.receiveKeyPress(keys);
									}
								}
								if (options.gamepadControls)
								{
									if (currentMinigame == null)
									{
										oldMouseState = mouseState;
										oldKBState = keyboardState;
										oldPadState = padState;
										UpdateChatBox();
										return;
									}
									ButtonCollection.ButtonEnumerator enumerator2 = Utility.getPressedButtons(padState, oldPadState).GetEnumerator();
									while (enumerator2.MoveNext())
									{
										Buttons current2 = enumerator2.Current;
										currentMinigame?.receiveKeyPress(Utility.mapGamePadButtonToKey(current2));
									}
									if (currentMinigame == null)
									{
										oldMouseState = mouseState;
										oldKBState = keyboardState;
										oldPadState = padState;
										UpdateChatBox();
										return;
									}
									if (padState.ThumbSticks.Right.Y < -0.2f && oldPadState.ThumbSticks.Right.Y >= -0.2f)
									{
										currentMinigame.receiveKeyPress(Keys.Down);
									}
									if (padState.ThumbSticks.Right.Y > 0.2f && oldPadState.ThumbSticks.Right.Y <= 0.2f)
									{
										currentMinigame.receiveKeyPress(Keys.Up);
									}
									if (padState.ThumbSticks.Right.X < -0.2f && oldPadState.ThumbSticks.Right.X >= -0.2f)
									{
										currentMinigame.receiveKeyPress(Keys.Left);
									}
									if (padState.ThumbSticks.Right.X > 0.2f && oldPadState.ThumbSticks.Right.X <= 0.2f)
									{
										currentMinigame.receiveKeyPress(Keys.Right);
									}
									if (oldPadState.ThumbSticks.Right.Y < -0.2f && padState.ThumbSticks.Right.Y >= -0.2f)
									{
										currentMinigame.receiveKeyRelease(Keys.Down);
									}
									if (oldPadState.ThumbSticks.Right.Y > 0.2f && padState.ThumbSticks.Right.Y <= 0.2f)
									{
										currentMinigame.receiveKeyRelease(Keys.Up);
									}
									if (oldPadState.ThumbSticks.Right.X < -0.2f && padState.ThumbSticks.Right.X >= -0.2f)
									{
										currentMinigame.receiveKeyRelease(Keys.Left);
									}
									if (oldPadState.ThumbSticks.Right.X > 0.2f && padState.ThumbSticks.Right.X <= 0.2f)
									{
										currentMinigame.receiveKeyRelease(Keys.Right);
									}
									if (isGamePadThumbstickInMotion() && currentMinigame != null && !currentMinigame.overrideFreeMouseMovement())
									{
										setMousePosition(getMouseX() + (int)(padState.ThumbSticks.Left.X * thumbstickToMouseModifier), getMouseY() - (int)(padState.ThumbSticks.Left.Y * thumbstickToMouseModifier));
									}
									else if (getMouseX() != getOldMouseX() || getMouseY() != getOldMouseY())
									{
										lastCursorMotionWasMouse = true;
									}
								}
								pressedKeys = oldKBState.GetPressedKeys();
								foreach (Keys keys2 in pressedKeys)
								{
									if (!keyboardState.IsKeyDown(keys2) && currentMinigame != null)
									{
										currentMinigame.receiveKeyRelease(keys2);
									}
								}
								if (options.gamepadControls)
								{
									if (currentMinigame == null)
									{
										oldMouseState = mouseState;
										oldKBState = keyboardState;
										oldPadState = padState;
										UpdateChatBox();
										return;
									}
									if (padState.IsConnected)
									{
										if (padState.IsButtonDown(Buttons.X) && !oldPadState.IsButtonDown(Buttons.X))
										{
											currentMinigame.receiveRightClick(getMouseX(), getMouseY());
										}
										else if (padState.IsButtonDown(Buttons.A) && !oldPadState.IsButtonDown(Buttons.A))
										{
											currentMinigame.receiveLeftClick(getMouseX(), getMouseY());
										}
										else if (!padState.IsButtonDown(Buttons.X) && oldPadState.IsButtonDown(Buttons.X))
										{
											currentMinigame.releaseRightClick(getMouseX(), getMouseY());
										}
										else if (!padState.IsButtonDown(Buttons.A) && oldPadState.IsButtonDown(Buttons.A))
										{
											currentMinigame.releaseLeftClick(getMouseX(), getMouseY());
										}
									}
									ButtonCollection.ButtonEnumerator enumerator2 = Utility.getPressedButtons(oldPadState, padState).GetEnumerator();
									while (enumerator2.MoveNext())
									{
										Buttons current3 = enumerator2.Current;
										currentMinigame?.receiveKeyRelease(Utility.mapGamePadButtonToKey(current3));
									}
									if (padState.IsConnected && padState.IsButtonDown(Buttons.A) && currentMinigame != null)
									{
										currentMinigame.leftClickHeld(0, 0);
									}
								}
								if (currentMinigame == null)
								{
									oldMouseState = mouseState;
									oldKBState = keyboardState;
									oldPadState = padState;
									UpdateChatBox();
									return;
								}
								if (currentMinigame != null && mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton != ButtonState.Pressed)
								{
									currentMinigame.receiveLeftClick(getMouseX(), getMouseY());
								}
								if (currentMinigame != null && mouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton != ButtonState.Pressed)
								{
									currentMinigame.receiveRightClick(getMouseX(), getMouseY());
								}
								if (currentMinigame != null && mouseState.LeftButton == ButtonState.Released && oldMouseState.LeftButton == ButtonState.Pressed)
								{
									currentMinigame.releaseLeftClick(getMouseX(), getMouseY());
								}
								if (currentMinigame != null && mouseState.RightButton == ButtonState.Released && oldMouseState.RightButton == ButtonState.Pressed)
								{
									currentMinigame.releaseLeftClick(getMouseX(), getMouseY());
								}
								if (currentMinigame != null && mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Pressed)
								{
									currentMinigame.leftClickHeld(getMouseX(), getMouseY());
								}
							}
						}
						if (currentMinigame != null && currentMinigame.tick(gameTime))
						{
							oldMouseState = mouseState;
							oldKBState = keyboardState;
							oldPadState = padState;
							currentMinigame?.unload();
							currentMinigame = null;
							fadeIn = true;
							fadeToBlackAlpha = 1f;
							UpdateChatBox();
							return;
						}
						if (currentMinigame == null && IsMusicContextActive(MusicContext.MiniGame))
						{
							stopMusicTrack(MusicContext.MiniGame);
						}
						oldMouseState = mouseState;
						oldKBState = keyboardState;
						oldPadState = padState;
					}
					flag2 = IsMultiplayer || currentMinigame == null || currentMinigame.doMainGameUpdates();
				}
				else if (farmEvent != null && !HostPaused && farmEvent.tickUpdate(gameTime))
				{
					farmEvent.makeChangesToLocation();
					timeOfDay = 600;
					outdoorLight = Color.White;
					displayHUD = true;
					farmEvent = null;
					netWorldState.Value.WriteToGame1();
					currentLocation = player.currentLocation;
					LocationRequest obj = getLocationRequest(currentLocation.Name);
					obj.OnWarp += delegate
					{
						if (currentLocation is FarmHouse farmHouse)
						{
							player.Position = Utility.PointToVector2(farmHouse.GetPlayerBedSpot()) * 64f;
							BedFurniture.ShiftPositionForBed(player);
						}
						else
						{
							BedFurniture.ApplyWakeUpPosition(player);
						}
						if (player.IsSitting())
						{
							player.StopSitting(animate: false);
						}
						changeMusicTrack("none", track_interruptable: true);
						player.forceCanMove();
						freezeControls = false;
						displayFarmer = true;
						viewportFreeze = false;
						fadeToBlackAlpha = 0f;
						fadeToBlack = false;
						globalFadeToClear();
						RemoveDeliveredMailForTomorrow();
						handlePostFarmEventActions();
						showEndOfNightStuff();
					};
					warpFarmer(obj, 5, 9, player.FacingDirection);
					fadeToBlackAlpha = 1.1f;
					fadeToBlack = true;
					nonWarpFade = false;
					UpdateOther(gameTime);
				}
				if (flag2)
				{
					if (endOfNightMenus.Count > 0 && activeClickableMenu == null)
					{
						activeClickableMenu = endOfNightMenus.Pop();
						if (activeClickableMenu != null && options.SnappyMenus)
						{
							activeClickableMenu.snapToDefaultClickableComponent();
						}
					}
					specialCurrencyDisplay?.Update(gameTime);
					if (currentLocation != null && currentMinigame == null)
					{
						if (emoteMenu != null)
						{
							emoteMenu.update(gameTime);
							if (emoteMenu != null)
							{
								PushUIMode();
								emoteMenu.performHoverAction(getMouseX(), getMouseY());
								KeyboardState keyboardState2 = GetKeyboardState();
								if (input.GetMouseState().LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Released)
								{
									emoteMenu.receiveLeftClick(getMouseX(), getMouseY());
								}
								else if (input.GetMouseState().RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released)
								{
									emoteMenu.receiveRightClick(getMouseX(), getMouseY());
								}
								else if (isOneOfTheseKeysDown(keyboardState2, options.menuButton) || (isOneOfTheseKeysDown(keyboardState2, options.emoteButton) && areAllOfTheseKeysUp(oldKBState, options.emoteButton)))
								{
									emoteMenu.exitThisMenu(playSound: false);
								}
								PopUIMode();
								oldKBState = keyboardState2;
								oldMouseState = input.GetMouseState();
							}
						}
						else if (textEntry != null)
						{
							PushUIMode();
							updateTextEntry(gameTime);
							PopUIMode();
						}
						else if (activeClickableMenu != null)
						{
							PushUIMode();
							updateActiveMenu(gameTime);
							PopUIMode();
						}
						else
						{
							if (pauseTime > 0f)
							{
								updatePause(gameTime);
							}
							if (!globalFade && !freezeControls && activeClickableMenu == null && (IsActiveNoOverlay || inputSimulator != null))
							{
								UpdateControlInput(gameTime);
							}
						}
					}
					if (showingEndOfNightStuff && endOfNightMenus.Count == 0 && activeClickableMenu == null)
					{
						newDaySync.destroy();
						player.team.endOfNightStatus.WithdrawState();
						showingEndOfNightStuff = false;
						Action afterNewDayAction = _afterNewDayAction;
						if (afterNewDayAction != null)
						{
							_afterNewDayAction = null;
							afterNewDayAction();
						}
						player.ReequipEnchantments();
						globalFadeToClear(doMorningStuff);
					}
					if (currentLocation != null)
					{
						if (!HostPaused && !showingEndOfNightStuff)
						{
							if (IsMultiplayer || (activeClickableMenu == null && currentMinigame == null) || player.viewingLocation.Value != null)
							{
								UpdateGameClock(gameTime);
							}
							UpdateCharacters(gameTime);
							UpdateLocations(gameTime);
							if (currentMinigame == null)
							{
								UpdateViewPort(overrideFreeze: false, getViewportCenter());
							}
							else
							{
								previousViewportPosition.X = viewport.X;
								previousViewportPosition.Y = viewport.Y;
							}
							UpdateOther(gameTime);
						}
						if (messagePause)
						{
							KeyboardState keyboardState3 = GetKeyboardState();
							MouseState mouseState2 = input.GetMouseState();
							GamePadState gamePadState = input.GetGamePadState();
							if (isOneOfTheseKeysDown(keyboardState3, options.actionButton) && !isOneOfTheseKeysDown(oldKBState, options.actionButton))
							{
								pressActionButton(keyboardState3, mouseState2, gamePadState);
							}
							oldKBState = keyboardState3;
							oldPadState = gamePadState;
						}
					}
				}
				else if (textEntry != null)
				{
					PushUIMode();
					updateTextEntry(gameTime);
					PopUIMode();
				}
			}
			else
			{
				UpdateTitleScreen(gameTime);
				if (textEntry != null)
				{
					PushUIMode();
					updateTextEntry(gameTime);
					PopUIMode();
				}
				else if (activeClickableMenu != null)
				{
					PushUIMode();
					updateActiveMenu(gameTime);
					PopUIMode();
				}
				if (gameMode == 10)
				{
					UpdateOther(gameTime);
				}
			}
			audioEngine?.Update();
			UpdateChatBox();
			if (gameMode != 6)
			{
				multiplayer.UpdateLate();
			}
		}
		else if (ShouldLoadIncrementally)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			do
			{
				if (!LoadContentEnumerator.MoveNext())
				{
					FinishedFirstLoadContent = true;
					break;
				}
			}
			while (stopwatch.Elapsed.TotalMilliseconds < 25.0);
			if (FinishedFirstLoadContent && FinishedFirstInitSounds && FinishedFirstInitSerializers)
			{
				FinishedIncrementalLoad = true;
				AfterLoadContent();
			}
		}
		if (gameMode == 3 && gameModeTicks == 1)
		{
			OnDayStarted();
		}
	}

	/// <summary>Handle the new day starting after the player saves, loads, or connects.</summary>
	public static void OnDayStarted()
	{
		TriggerActionManager.Raise("DayStarted");
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			location.OnDayStarted();
			return true;
		});
		Utility.fixAllAnimals();
		foreach (NPC allCharacter in Utility.getAllCharacters())
		{
			allCharacter.OnDayStarted();
		}
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			foreach (FarmAnimal value in location.animals.Values)
			{
				value.OnDayStarted();
			}
			return true;
		});
		player.currentLocation.resetForPlayerEntry();
		if (!hasStartedDay)
		{
			foreach (string buildingType in player.team.constructedBuildings)
			{
				player.NotifyQuests((Quest quest) => quest.OnBuildingExists(buildingType));
			}
			if (Stats.AllowRetroactiveAchievements)
			{
				foreach (int achievement in player.achievements)
				{
					getPlatformAchievement(achievement.ToString());
				}
			}
			hasStartedDay = true;
		}
		if (IsMasterGame)
		{
			Woods.ResetLostItemsShop();
		}
	}

	public static void PerformPassiveFestivalSetup()
	{
		foreach (string activePassiveFestival in netWorldState.Value.ActivePassiveFestivals)
		{
			if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data) && data.DailySetupMethod != null)
			{
				if (StaticDelegateBuilder.TryCreateDelegate<FestivalDailySetupDelegate>(data.DailySetupMethod, out var createdDelegate, out var error))
				{
					createdDelegate();
					continue;
				}
				log.Warn($"Passive festival '{activePassiveFestival}' has invalid daily setup method '{data.DailySetupMethod}': {error}");
			}
		}
	}

	public static void showTextEntry(TextBox text_box)
	{
		timerUntilMouseFade = 0;
		PushUIMode();
		textEntry = new TextEntryMenu(text_box);
		PopUIMode();
	}

	public static void closeTextEntry()
	{
		if (textEntry != null)
		{
			textEntry = null;
		}
		if (activeClickableMenu != null && options.SnappyMenus)
		{
			if (activeClickableMenu is TitleMenu && TitleMenu.subMenu != null)
			{
				TitleMenu.subMenu.snapCursorToCurrentSnappedComponent();
			}
			else
			{
				activeClickableMenu.snapCursorToCurrentSnappedComponent();
			}
		}
	}

	public static bool isDarkOut(GameLocation location)
	{
		return timeOfDay >= getTrulyDarkTime(location);
	}

	public static bool isTimeToTurnOffLighting(GameLocation location)
	{
		return timeOfDay >= getTrulyDarkTime(location) - 100;
	}

	public static bool isStartingToGetDarkOut(GameLocation location)
	{
		return timeOfDay >= getStartingToGetDarkTime(location);
	}

	public static int getStartingToGetDarkTime(GameLocation location)
	{
		if (location != null && location.InIslandContext())
		{
			return 1800;
		}
		return season switch
		{
			Season.Fall => 1700, 
			Season.Winter => 1500, 
			_ => 1800, 
		};
	}

	public static void updateCellarAssignments()
	{
		if (!IsMasterGame)
		{
			return;
		}
		player.team.cellarAssignments[1] = MasterPlayer.UniqueMultiplayerID;
		for (int i = 2; i <= netWorldState.Value.HighestPlayerLimit; i++)
		{
			string name = "Cellar" + i;
			if (i == 1 || getLocationFromName(name) == null)
			{
				continue;
			}
			if (player.team.cellarAssignments.TryGetValue(i, out var value))
			{
				if (GetPlayer(value) != null)
				{
					continue;
				}
				player.team.cellarAssignments.Remove(i);
			}
			foreach (Farmer allFarmer in getAllFarmers())
			{
				if (!player.team.cellarAssignments.Values.Contains(allFarmer.UniqueMultiplayerID))
				{
					player.team.cellarAssignments[i] = allFarmer.UniqueMultiplayerID;
					break;
				}
			}
		}
	}

	public static int getModeratelyDarkTime(GameLocation location)
	{
		return (getTrulyDarkTime(location) + getStartingToGetDarkTime(location)) / 2;
	}

	public static int getTrulyDarkTime(GameLocation location)
	{
		return getStartingToGetDarkTime(location) + 200;
	}

	public static void playMorningSong(bool ignoreDelay = false)
	{
		LocationContextData context;
		if (!eventUp && dayOfMonth > 0)
		{
			LocationData data = currentLocation.GetData();
			if (currentLocation.GetLocationSpecificMusic() != null && (data == null || !data.MusicIsTownTheme))
			{
				changeMusicTrack("none", track_interruptable: true);
				GameLocation.HandleMusicChange(null, currentLocation);
				return;
			}
			if (IsRainingHere())
			{
				if (ignoreDelay)
				{
					PlayRain();
				}
				else
				{
					morningSongPlayAction = DelayedAction.functionAfterDelay(PlayRain, 500);
				}
				return;
			}
			context = currentLocation?.GetLocationContext();
			if (context?.DefaultMusic != null)
			{
				if (context.DefaultMusicCondition == null || GameStateQuery.CheckConditions(context.DefaultMusicCondition))
				{
					if (ignoreDelay)
					{
						PlayLocationSong();
					}
					else
					{
						morningSongPlayAction = DelayedAction.functionAfterDelay(PlayLocationSong, 500);
					}
				}
			}
			else if (ignoreDelay)
			{
				PlayDefault();
			}
			else
			{
				morningSongPlayAction = DelayedAction.functionAfterDelay(PlayDefault, 500);
			}
		}
		else if (getMusicTrackName() == "silence")
		{
			changeMusicTrack("none", track_interruptable: true);
		}
		static void PlayDefault()
		{
			changeMusicTrack(currentLocation.GetMorningSong(), track_interruptable: true);
			IsPlayingBackgroundMusic = true;
			IsPlayingMorningSong = true;
		}
		void PlayLocationSong()
		{
			if (currentLocation == null)
			{
				changeMusicTrack("none", track_interruptable: true);
			}
			else
			{
				changeMusicTrack(context.DefaultMusic, track_interruptable: true);
				IsPlayingBackgroundMusic = true;
			}
		}
		static void PlayRain()
		{
			changeMusicTrack("rain", track_interruptable: true);
		}
	}

	public static void doMorningStuff()
	{
		playMorningSong();
		DelayedAction.functionAfterDelay(delegate
		{
			while (morningQueue.Count > 0)
			{
				morningQueue.Dequeue()();
			}
		}, 1000);
		if (player.hasPendingCompletedQuests)
		{
			dayTimeMoneyBox.PingQuestLog();
		}
	}

	/// <summary>Add an action that will be called one second after fully waking up in the morning. This won't be saved, so it should only be used for "fluff" functions like sending multiplayer chat messages, etc.</summary>
	/// <param name="action">The action to perform.</param>
	public static void addMorningFluffFunction(Action action)
	{
		morningQueue.Enqueue(action);
	}

	private Point getViewportCenter()
	{
		if (viewportTarget.X != -2.1474836E+09f)
		{
			if (!(Math.Abs((float)viewportCenter.X - viewportTarget.X) <= viewportSpeed) || !(Math.Abs((float)viewportCenter.Y - viewportTarget.Y) <= viewportSpeed))
			{
				Vector2 velocityTowardPoint = Utility.getVelocityTowardPoint(viewportCenter, viewportTarget, viewportSpeed);
				viewportCenter.X += (int)Math.Round(velocityTowardPoint.X);
				viewportCenter.Y += (int)Math.Round(velocityTowardPoint.Y);
			}
			else
			{
				if (viewportReachedTarget != null)
				{
					viewportReachedTarget();
					viewportReachedTarget = null;
				}
				viewportHold -= currentGameTime.ElapsedGameTime.Milliseconds;
				if (viewportHold <= 0)
				{
					viewportTarget = new Vector2(-2.1474836E+09f, -2.1474836E+09f);
					afterViewport?.Invoke();
				}
			}
		}
		else
		{
			viewportCenter = getPlayerOrEventFarmer().StandingPixel;
		}
		return viewportCenter;
	}

	public static void afterFadeReturnViewportToPlayer()
	{
		viewportTarget = new Vector2(-2.1474836E+09f, -2.1474836E+09f);
		viewportHold = 0;
		viewportFreeze = false;
		viewportCenter = player.StandingPixel;
		globalFadeToClear();
	}

	public static bool isViewportOnCustomPath()
	{
		return viewportTarget.X != -2.1474836E+09f;
	}

	public static void moveViewportTo(Vector2 target, float speed, int holdTimer = 0, afterFadeFunction reachedTarget = null, afterFadeFunction endFunction = null)
	{
		viewportTarget = target;
		viewportSpeed = speed;
		viewportHold = holdTimer;
		afterViewport = endFunction;
		viewportReachedTarget = reachedTarget;
	}

	public static Farm getFarm()
	{
		return RequireLocation<Farm>("Farm");
	}

	public static void setMousePosition(int x, int y, bool ui_scale)
	{
		if (ui_scale)
		{
			setMousePositionRaw((int)((float)x * options.uiScale), (int)((float)y * options.uiScale));
		}
		else
		{
			setMousePositionRaw((int)((float)x * options.zoomLevel), (int)((float)y * options.zoomLevel));
		}
	}

	public static void setMousePosition(int x, int y)
	{
		setMousePosition(x, y, uiMode);
	}

	public static void setMousePosition(Point position, bool ui_scale)
	{
		setMousePosition(position.X, position.Y, ui_scale);
	}

	public static void setMousePosition(Point position)
	{
		setMousePosition(position, uiMode);
	}

	public static void setMousePositionRaw(int x, int y)
	{
		input.SetMousePosition(x, y);
		InvalidateOldMouseMovement();
		lastCursorMotionWasMouse = false;
	}

	public static Point getMousePositionRaw()
	{
		return new Point(getMouseXRaw(), getMouseYRaw());
	}

	public static Point getMousePosition(bool ui_scale)
	{
		return new Point(getMouseX(ui_scale), getMouseY(ui_scale));
	}

	public static Point getMousePosition()
	{
		return getMousePosition(uiMode);
	}

	private static void ComputeCursorSpeed()
	{
		_cursorSpeedDirty = false;
		GamePadState gamePadState = input.GetGamePadState();
		float num = 0.9f;
		bool flag = false;
		float num2 = gamePadState.ThumbSticks.Left.Length();
		float num3 = gamePadState.ThumbSticks.Right.Length();
		if (num2 > num || num3 > num)
		{
			flag = true;
		}
		float min = 0.7f;
		float max = 2f;
		float num4 = 1f;
		if (_cursorDragEnabled)
		{
			min = 0.5f;
			max = 2f;
			num4 = 1f;
		}
		if (!flag)
		{
			num4 = -5f;
		}
		if (_cursorDragPrevEnabled != _cursorDragEnabled)
		{
			_cursorSpeedScale *= 0.5f;
		}
		_cursorDragPrevEnabled = _cursorDragEnabled;
		_cursorSpeedScale += _cursorUpdateElapsedSec * num4;
		_cursorSpeedScale = MathHelper.Clamp(_cursorSpeedScale, min, max);
		float num5 = 16f / (float)game1.TargetElapsedTime.TotalSeconds * _cursorSpeedScale;
		float num6 = num5 - _cursorSpeed;
		_cursorSpeed = num5;
		_cursorUpdateElapsedSec = 0f;
		if (debugMode)
		{
			log.Verbose("_cursorSpeed=" + _cursorSpeed.ToString("0.0") + ", _cursorSpeedScale=" + _cursorSpeedScale.ToString("0.0") + ", deltaSpeed=" + num6.ToString("0.0"));
		}
	}

	private static void SetFreeCursorElapsed(float elapsedSec)
	{
		if (elapsedSec != _cursorUpdateElapsedSec)
		{
			_cursorUpdateElapsedSec = elapsedSec;
			_cursorSpeedDirty = true;
		}
	}

	public static void ResetFreeCursorDrag()
	{
		if (_cursorDragEnabled)
		{
			_cursorSpeedDirty = true;
		}
		_cursorDragEnabled = false;
	}

	public static void SetFreeCursorDrag()
	{
		if (!_cursorDragEnabled)
		{
			_cursorSpeedDirty = true;
		}
		_cursorDragEnabled = true;
	}

	public static void updateActiveMenu(GameTime gameTime)
	{
		IClickableMenu childMenu = activeClickableMenu;
		while (childMenu.GetChildMenu() != null)
		{
			childMenu = childMenu.GetChildMenu();
		}
		if (!Program.gamePtr.IsActiveNoOverlay && Program.releaseBuild)
		{
			if (childMenu != null && childMenu.IsActive())
			{
				childMenu.update(gameTime);
			}
			return;
		}
		MouseState mouseState = input.GetMouseState();
		KeyboardState keyboardState = GetKeyboardState();
		GamePadState gamePadState = input.GetGamePadState();
		if (CurrentEvent != null)
		{
			if ((mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Released) || (options.gamepadControls && gamePadState.IsButtonDown(Buttons.A) && oldPadState.IsButtonUp(Buttons.A)))
			{
				CurrentEvent.receiveMouseClick(getMouseX(), getMouseY());
			}
			else if (options.gamepadControls && gamePadState.IsButtonDown(Buttons.Back) && oldPadState.IsButtonUp(Buttons.Back) && !CurrentEvent.skipped && CurrentEvent.skippable)
			{
				CurrentEvent.skipped = true;
				CurrentEvent.skipEvent();
				freezeControls = false;
			}
			if (CurrentEvent != null && CurrentEvent.skipped)
			{
				oldMouseState = input.GetMouseState();
				oldKBState = keyboardState;
				oldPadState = gamePadState;
				return;
			}
		}
		if (options.gamepadControls && childMenu != null && childMenu.IsActive())
		{
			if (isGamePadThumbstickInMotion() && (!options.snappyMenus || childMenu.overrideSnappyMenuCursorMovementBan()))
			{
				setMousePositionRaw((int)((float)mouseState.X + gamePadState.ThumbSticks.Left.X * thumbstickToMouseModifier), (int)((float)mouseState.Y - gamePadState.ThumbSticks.Left.Y * thumbstickToMouseModifier));
			}
			if (childMenu != null && childMenu.IsActive() && (chatBox == null || !chatBox.isActive()))
			{
				ButtonCollection.ButtonEnumerator enumerator = Utility.getPressedButtons(gamePadState, oldPadState).GetEnumerator();
				while (enumerator.MoveNext())
				{
					Buttons current = enumerator.Current;
					childMenu.receiveGamePadButton(current);
					if (childMenu == null || !childMenu.IsActive())
					{
						break;
					}
				}
				enumerator = Utility.getHeldButtons(gamePadState).GetEnumerator();
				while (enumerator.MoveNext())
				{
					Buttons current2 = enumerator.Current;
					if (childMenu != null && childMenu.IsActive())
					{
						childMenu.gamePadButtonHeld(current2);
					}
					if (childMenu == null || !childMenu.IsActive())
					{
						break;
					}
				}
			}
		}
		if ((getMouseX() != getOldMouseX() || getMouseY() != getOldMouseY()) && !isGamePadThumbstickInMotion() && !isDPadPressed())
		{
			lastCursorMotionWasMouse = true;
		}
		ResetFreeCursorDrag();
		if (childMenu != null && childMenu.IsActive())
		{
			childMenu.performHoverAction(getMouseX(), getMouseY());
		}
		if (childMenu != null && childMenu.IsActive())
		{
			childMenu.update(gameTime);
		}
		if (childMenu != null && childMenu.IsActive() && mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Released)
		{
			if (chatBox != null && chatBox.isActive() && chatBox.isWithinBounds(getMouseX(), getMouseY()))
			{
				chatBox.receiveLeftClick(getMouseX(), getMouseY());
			}
			else
			{
				childMenu.receiveLeftClick(getMouseX(), getMouseY());
			}
		}
		else if (childMenu != null && childMenu.IsActive() && mouseState.RightButton == ButtonState.Pressed && (oldMouseState.RightButton == ButtonState.Released || ((float)mouseClickPolling > 650f && !(childMenu is DialogueBox))))
		{
			childMenu.receiveRightClick(getMouseX(), getMouseY());
			if ((float)mouseClickPolling > 650f)
			{
				mouseClickPolling = 600;
			}
			if ((childMenu == null || !childMenu.IsActive()) && activeClickableMenu == null)
			{
				rightClickPolling = 500;
				mouseClickPolling = 0;
			}
		}
		if (mouseState.ScrollWheelValue != oldMouseState.ScrollWheelValue && childMenu != null && childMenu.IsActive())
		{
			if (chatBox != null && chatBox.choosingEmoji && chatBox.emojiMenu.isWithinBounds(getOldMouseX(), getOldMouseY()))
			{
				chatBox.receiveScrollWheelAction(mouseState.ScrollWheelValue - oldMouseState.ScrollWheelValue);
			}
			else
			{
				childMenu.receiveScrollWheelAction(mouseState.ScrollWheelValue - oldMouseState.ScrollWheelValue);
			}
		}
		if (options.gamepadControls && childMenu != null && childMenu.IsActive())
		{
			thumbstickPollingTimer -= currentGameTime.ElapsedGameTime.Milliseconds;
			if (thumbstickPollingTimer <= 0)
			{
				if (gamePadState.ThumbSticks.Right.Y > 0.2f)
				{
					childMenu.receiveScrollWheelAction(1);
				}
				else if (gamePadState.ThumbSticks.Right.Y < -0.2f)
				{
					childMenu.receiveScrollWheelAction(-1);
				}
			}
			if (thumbstickPollingTimer <= 0)
			{
				thumbstickPollingTimer = 220 - (int)(Math.Abs(gamePadState.ThumbSticks.Right.Y) * 170f);
			}
			if (Math.Abs(gamePadState.ThumbSticks.Right.Y) < 0.2f)
			{
				thumbstickPollingTimer = 0;
			}
		}
		if (childMenu != null && childMenu.IsActive() && mouseState.LeftButton == ButtonState.Released && oldMouseState.LeftButton == ButtonState.Pressed)
		{
			childMenu.releaseLeftClick(getMouseX(), getMouseY());
		}
		else if (childMenu != null && childMenu.IsActive() && mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Pressed)
		{
			childMenu.leftClickHeld(getMouseX(), getMouseY());
		}
		Keys[] pressedKeys = keyboardState.GetPressedKeys();
		foreach (Keys keys in pressedKeys)
		{
			if (childMenu != null && childMenu.IsActive() && !oldKBState.GetPressedKeys().Contains(keys))
			{
				childMenu.receiveKeyPress(keys);
			}
		}
		if (chatBox == null || !chatBox.isActive())
		{
			if (isOneOfTheseKeysDown(oldKBState, options.moveUpButton) || (options.snappyMenus && options.gamepadControls && (Math.Abs(gamePadState.ThumbSticks.Left.X) < gamePadState.ThumbSticks.Left.Y || gamePadState.IsButtonDown(Buttons.DPadUp))))
			{
				directionKeyPolling[0] -= currentGameTime.ElapsedGameTime.Milliseconds;
			}
			else if (isOneOfTheseKeysDown(oldKBState, options.moveRightButton) || (options.snappyMenus && options.gamepadControls && (gamePadState.ThumbSticks.Left.X > Math.Abs(gamePadState.ThumbSticks.Left.Y) || gamePadState.IsButtonDown(Buttons.DPadRight))))
			{
				directionKeyPolling[1] -= currentGameTime.ElapsedGameTime.Milliseconds;
			}
			else if (isOneOfTheseKeysDown(oldKBState, options.moveDownButton) || (options.snappyMenus && options.gamepadControls && (Math.Abs(gamePadState.ThumbSticks.Left.X) < Math.Abs(gamePadState.ThumbSticks.Left.Y) || gamePadState.IsButtonDown(Buttons.DPadDown))))
			{
				directionKeyPolling[2] -= currentGameTime.ElapsedGameTime.Milliseconds;
			}
			else if (isOneOfTheseKeysDown(oldKBState, options.moveLeftButton) || (options.snappyMenus && options.gamepadControls && (Math.Abs(gamePadState.ThumbSticks.Left.X) > Math.Abs(gamePadState.ThumbSticks.Left.Y) || gamePadState.IsButtonDown(Buttons.DPadLeft))))
			{
				directionKeyPolling[3] -= currentGameTime.ElapsedGameTime.Milliseconds;
			}
			if (areAllOfTheseKeysUp(oldKBState, options.moveUpButton) && (!options.snappyMenus || !options.gamepadControls || ((double)gamePadState.ThumbSticks.Left.Y < 0.1 && gamePadState.IsButtonUp(Buttons.DPadUp))))
			{
				directionKeyPolling[0] = 250;
			}
			if (areAllOfTheseKeysUp(oldKBState, options.moveRightButton) && (!options.snappyMenus || !options.gamepadControls || ((double)gamePadState.ThumbSticks.Left.X < 0.1 && gamePadState.IsButtonUp(Buttons.DPadRight))))
			{
				directionKeyPolling[1] = 250;
			}
			if (areAllOfTheseKeysUp(oldKBState, options.moveDownButton) && (!options.snappyMenus || !options.gamepadControls || ((double)gamePadState.ThumbSticks.Left.Y > -0.1 && gamePadState.IsButtonUp(Buttons.DPadDown))))
			{
				directionKeyPolling[2] = 250;
			}
			if (areAllOfTheseKeysUp(oldKBState, options.moveLeftButton) && (!options.snappyMenus || !options.gamepadControls || ((double)gamePadState.ThumbSticks.Left.X > -0.1 && gamePadState.IsButtonUp(Buttons.DPadLeft))))
			{
				directionKeyPolling[3] = 250;
			}
			if (directionKeyPolling[0] <= 0 && childMenu != null && childMenu.IsActive())
			{
				childMenu.receiveKeyPress(options.getFirstKeyboardKeyFromInputButtonList(options.moveUpButton));
				directionKeyPolling[0] = 70;
			}
			if (directionKeyPolling[1] <= 0 && childMenu != null && childMenu.IsActive())
			{
				childMenu.receiveKeyPress(options.getFirstKeyboardKeyFromInputButtonList(options.moveRightButton));
				directionKeyPolling[1] = 70;
			}
			if (directionKeyPolling[2] <= 0 && childMenu != null && childMenu.IsActive())
			{
				childMenu.receiveKeyPress(options.getFirstKeyboardKeyFromInputButtonList(options.moveDownButton));
				directionKeyPolling[2] = 70;
			}
			if (directionKeyPolling[3] <= 0 && childMenu != null && childMenu.IsActive())
			{
				childMenu.receiveKeyPress(options.getFirstKeyboardKeyFromInputButtonList(options.moveLeftButton));
				directionKeyPolling[3] = 70;
			}
			if (options.gamepadControls && childMenu != null && childMenu.IsActive())
			{
				if (!childMenu.areGamePadControlsImplemented() && gamePadState.IsButtonDown(Buttons.A) && (!oldPadState.IsButtonDown(Buttons.A) || ((float)gamePadAButtonPolling > 650f && !(childMenu is DialogueBox))))
				{
					childMenu.receiveLeftClick(getMousePosition().X, getMousePosition().Y);
					if ((float)gamePadAButtonPolling > 650f)
					{
						gamePadAButtonPolling = 600;
					}
				}
				else if (!childMenu.areGamePadControlsImplemented() && !gamePadState.IsButtonDown(Buttons.A) && oldPadState.IsButtonDown(Buttons.A))
				{
					childMenu.releaseLeftClick(getMousePosition().X, getMousePosition().Y);
				}
				else if (!childMenu.areGamePadControlsImplemented() && gamePadState.IsButtonDown(Buttons.X) && (!oldPadState.IsButtonDown(Buttons.X) || ((float)gamePadXButtonPolling > 650f && !(childMenu is DialogueBox))))
				{
					childMenu.receiveRightClick(getMousePosition().X, getMousePosition().Y);
					if ((float)gamePadXButtonPolling > 650f)
					{
						gamePadXButtonPolling = 600;
					}
				}
				ButtonCollection.ButtonEnumerator enumerator = Utility.getPressedButtons(gamePadState, oldPadState).GetEnumerator();
				while (enumerator.MoveNext())
				{
					Buttons current3 = enumerator.Current;
					if (childMenu == null || !childMenu.IsActive())
					{
						break;
					}
					Keys key = Utility.mapGamePadButtonToKey(current3);
					if (!(childMenu is FarmhandMenu) || game1.IsMainInstance || !options.doesInputListContain(options.menuButton, key))
					{
						childMenu.receiveKeyPress(key);
					}
				}
				if (childMenu != null && childMenu.IsActive() && !childMenu.areGamePadControlsImplemented() && gamePadState.IsButtonDown(Buttons.A) && oldPadState.IsButtonDown(Buttons.A))
				{
					childMenu.leftClickHeld(getMousePosition().X, getMousePosition().Y);
				}
				if (gamePadState.IsButtonDown(Buttons.X))
				{
					gamePadXButtonPolling += gameTime.ElapsedGameTime.Milliseconds;
				}
				else
				{
					gamePadXButtonPolling = 0;
				}
				if (gamePadState.IsButtonDown(Buttons.A))
				{
					gamePadAButtonPolling += gameTime.ElapsedGameTime.Milliseconds;
				}
				else
				{
					gamePadAButtonPolling = 0;
				}
				if (!childMenu.IsActive() && activeClickableMenu == null)
				{
					rightClickPolling = 500;
					gamePadXButtonPolling = 0;
					gamePadAButtonPolling = 0;
				}
			}
		}
		if (mouseState.RightButton == ButtonState.Pressed)
		{
			mouseClickPolling += gameTime.ElapsedGameTime.Milliseconds;
		}
		else
		{
			mouseClickPolling = 0;
		}
		oldMouseState = input.GetMouseState();
		oldKBState = keyboardState;
		oldPadState = gamePadState;
	}

	public bool ShowLocalCoopJoinMenu()
	{
		if (!base.IsMainInstance)
		{
			return false;
		}
		if (gameMode != 3)
		{
			return false;
		}
		int free_farmhands = 0;
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			if (location is Cabin cabin && (!cabin.HasOwner || !cabin.IsOwnerActivated))
			{
				free_farmhands++;
			}
			return true;
		});
		if (free_farmhands == 0)
		{
			showRedMessage(content.LoadString("Strings\\UI:CoopMenu_NoSlots"));
			return false;
		}
		if (currentMinigame != null)
		{
			return false;
		}
		if (activeClickableMenu != null)
		{
			return false;
		}
		if (!IsLocalCoopJoinable())
		{
			return false;
		}
		playSound("bigSelect");
		activeClickableMenu = new LocalCoopJoinMenu();
		return true;
	}

	public static void updateTextEntry(GameTime gameTime)
	{
		MouseState mouseState = input.GetMouseState();
		KeyboardState keyboardState = GetKeyboardState();
		GamePadState gamePadState = input.GetGamePadState();
		if (options.gamepadControls && textEntry != null && textEntry != null)
		{
			ButtonCollection.ButtonEnumerator enumerator = Utility.getPressedButtons(gamePadState, oldPadState).GetEnumerator();
			while (enumerator.MoveNext())
			{
				Buttons current = enumerator.Current;
				textEntry.receiveGamePadButton(current);
				if (textEntry == null)
				{
					break;
				}
			}
			enumerator = Utility.getHeldButtons(gamePadState).GetEnumerator();
			while (enumerator.MoveNext())
			{
				Buttons current2 = enumerator.Current;
				textEntry?.gamePadButtonHeld(current2);
				if (textEntry == null)
				{
					break;
				}
			}
		}
		textEntry?.performHoverAction(getMouseX(), getMouseY());
		textEntry?.update(gameTime);
		if (textEntry != null && mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Released)
		{
			textEntry.receiveLeftClick(getMouseX(), getMouseY());
		}
		else if (textEntry != null && mouseState.RightButton == ButtonState.Pressed && (oldMouseState.RightButton == ButtonState.Released || (float)mouseClickPolling > 650f))
		{
			textEntry.receiveRightClick(getMouseX(), getMouseY());
			if ((float)mouseClickPolling > 650f)
			{
				mouseClickPolling = 600;
			}
			if (textEntry == null)
			{
				rightClickPolling = 500;
				mouseClickPolling = 0;
			}
		}
		if (mouseState.ScrollWheelValue != oldMouseState.ScrollWheelValue && textEntry != null)
		{
			if (chatBox != null && chatBox.choosingEmoji && chatBox.emojiMenu.isWithinBounds(getOldMouseX(), getOldMouseY()))
			{
				chatBox.receiveScrollWheelAction(mouseState.ScrollWheelValue - oldMouseState.ScrollWheelValue);
			}
			else
			{
				textEntry.receiveScrollWheelAction(mouseState.ScrollWheelValue - oldMouseState.ScrollWheelValue);
			}
		}
		if (options.gamepadControls && textEntry != null)
		{
			thumbstickPollingTimer -= currentGameTime.ElapsedGameTime.Milliseconds;
			if (thumbstickPollingTimer <= 0)
			{
				if (gamePadState.ThumbSticks.Right.Y > 0.2f)
				{
					textEntry.receiveScrollWheelAction(1);
				}
				else if (gamePadState.ThumbSticks.Right.Y < -0.2f)
				{
					textEntry.receiveScrollWheelAction(-1);
				}
			}
			if (thumbstickPollingTimer <= 0)
			{
				thumbstickPollingTimer = 220 - (int)(Math.Abs(gamePadState.ThumbSticks.Right.Y) * 170f);
			}
			if (Math.Abs(gamePadState.ThumbSticks.Right.Y) < 0.2f)
			{
				thumbstickPollingTimer = 0;
			}
		}
		if (textEntry != null && mouseState.LeftButton == ButtonState.Released && oldMouseState.LeftButton == ButtonState.Pressed)
		{
			textEntry.releaseLeftClick(getMouseX(), getMouseY());
		}
		else if (textEntry != null && mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Pressed)
		{
			textEntry.leftClickHeld(getMouseX(), getMouseY());
		}
		Keys[] pressedKeys = keyboardState.GetPressedKeys();
		foreach (Keys keys in pressedKeys)
		{
			if (textEntry != null && !oldKBState.GetPressedKeys().Contains(keys))
			{
				textEntry.receiveKeyPress(keys);
			}
		}
		if (isOneOfTheseKeysDown(oldKBState, options.moveUpButton) || (options.snappyMenus && options.gamepadControls && (Math.Abs(gamePadState.ThumbSticks.Left.X) < gamePadState.ThumbSticks.Left.Y || gamePadState.IsButtonDown(Buttons.DPadUp))))
		{
			directionKeyPolling[0] -= currentGameTime.ElapsedGameTime.Milliseconds;
		}
		else if (isOneOfTheseKeysDown(oldKBState, options.moveRightButton) || (options.snappyMenus && options.gamepadControls && (gamePadState.ThumbSticks.Left.X > Math.Abs(gamePadState.ThumbSticks.Left.Y) || gamePadState.IsButtonDown(Buttons.DPadRight))))
		{
			directionKeyPolling[1] -= currentGameTime.ElapsedGameTime.Milliseconds;
		}
		else if (isOneOfTheseKeysDown(oldKBState, options.moveDownButton) || (options.snappyMenus && options.gamepadControls && (Math.Abs(gamePadState.ThumbSticks.Left.X) < Math.Abs(gamePadState.ThumbSticks.Left.Y) || gamePadState.IsButtonDown(Buttons.DPadDown))))
		{
			directionKeyPolling[2] -= currentGameTime.ElapsedGameTime.Milliseconds;
		}
		else if (isOneOfTheseKeysDown(oldKBState, options.moveLeftButton) || (options.snappyMenus && options.gamepadControls && (Math.Abs(gamePadState.ThumbSticks.Left.X) > Math.Abs(gamePadState.ThumbSticks.Left.Y) || gamePadState.IsButtonDown(Buttons.DPadLeft))))
		{
			directionKeyPolling[3] -= currentGameTime.ElapsedGameTime.Milliseconds;
		}
		if (areAllOfTheseKeysUp(oldKBState, options.moveUpButton) && (!options.snappyMenus || !options.gamepadControls || ((double)gamePadState.ThumbSticks.Left.Y < 0.1 && gamePadState.IsButtonUp(Buttons.DPadUp))))
		{
			directionKeyPolling[0] = 250;
		}
		if (areAllOfTheseKeysUp(oldKBState, options.moveRightButton) && (!options.snappyMenus || !options.gamepadControls || ((double)gamePadState.ThumbSticks.Left.X < 0.1 && gamePadState.IsButtonUp(Buttons.DPadRight))))
		{
			directionKeyPolling[1] = 250;
		}
		if (areAllOfTheseKeysUp(oldKBState, options.moveDownButton) && (!options.snappyMenus || !options.gamepadControls || ((double)gamePadState.ThumbSticks.Left.Y > -0.1 && gamePadState.IsButtonUp(Buttons.DPadDown))))
		{
			directionKeyPolling[2] = 250;
		}
		if (areAllOfTheseKeysUp(oldKBState, options.moveLeftButton) && (!options.snappyMenus || !options.gamepadControls || ((double)gamePadState.ThumbSticks.Left.X > -0.1 && gamePadState.IsButtonUp(Buttons.DPadLeft))))
		{
			directionKeyPolling[3] = 250;
		}
		if (directionKeyPolling[0] <= 0 && textEntry != null)
		{
			textEntry.receiveKeyPress(options.getFirstKeyboardKeyFromInputButtonList(options.moveUpButton));
			directionKeyPolling[0] = 70;
		}
		if (directionKeyPolling[1] <= 0 && textEntry != null)
		{
			textEntry.receiveKeyPress(options.getFirstKeyboardKeyFromInputButtonList(options.moveRightButton));
			directionKeyPolling[1] = 70;
		}
		if (directionKeyPolling[2] <= 0 && textEntry != null)
		{
			textEntry.receiveKeyPress(options.getFirstKeyboardKeyFromInputButtonList(options.moveDownButton));
			directionKeyPolling[2] = 70;
		}
		if (directionKeyPolling[3] <= 0 && textEntry != null)
		{
			textEntry.receiveKeyPress(options.getFirstKeyboardKeyFromInputButtonList(options.moveLeftButton));
			directionKeyPolling[3] = 70;
		}
		if (options.gamepadControls && textEntry != null)
		{
			if (!textEntry.areGamePadControlsImplemented() && gamePadState.IsButtonDown(Buttons.A) && (!oldPadState.IsButtonDown(Buttons.A) || (float)gamePadAButtonPolling > 650f))
			{
				textEntry.receiveLeftClick(getMousePosition().X, getMousePosition().Y);
				if ((float)gamePadAButtonPolling > 650f)
				{
					gamePadAButtonPolling = 600;
				}
			}
			else if (!textEntry.areGamePadControlsImplemented() && !gamePadState.IsButtonDown(Buttons.A) && oldPadState.IsButtonDown(Buttons.A))
			{
				textEntry.releaseLeftClick(getMousePosition().X, getMousePosition().Y);
			}
			else if (!textEntry.areGamePadControlsImplemented() && gamePadState.IsButtonDown(Buttons.X) && (!oldPadState.IsButtonDown(Buttons.X) || (float)gamePadXButtonPolling > 650f))
			{
				textEntry.receiveRightClick(getMousePosition().X, getMousePosition().Y);
				if ((float)gamePadXButtonPolling > 650f)
				{
					gamePadXButtonPolling = 600;
				}
			}
			ButtonCollection.ButtonEnumerator enumerator = Utility.getPressedButtons(gamePadState, oldPadState).GetEnumerator();
			while (enumerator.MoveNext())
			{
				Buttons current3 = enumerator.Current;
				if (textEntry == null)
				{
					break;
				}
				textEntry.receiveKeyPress(Utility.mapGamePadButtonToKey(current3));
			}
			if (textEntry != null && !textEntry.areGamePadControlsImplemented() && gamePadState.IsButtonDown(Buttons.A) && oldPadState.IsButtonDown(Buttons.A))
			{
				textEntry.leftClickHeld(getMousePosition().X, getMousePosition().Y);
			}
			if (gamePadState.IsButtonDown(Buttons.X))
			{
				gamePadXButtonPolling += gameTime.ElapsedGameTime.Milliseconds;
			}
			else
			{
				gamePadXButtonPolling = 0;
			}
			if (gamePadState.IsButtonDown(Buttons.A))
			{
				gamePadAButtonPolling += gameTime.ElapsedGameTime.Milliseconds;
			}
			else
			{
				gamePadAButtonPolling = 0;
			}
			if (textEntry == null)
			{
				rightClickPolling = 500;
				gamePadAButtonPolling = 0;
				gamePadXButtonPolling = 0;
			}
		}
		if (mouseState.RightButton == ButtonState.Pressed)
		{
			mouseClickPolling += gameTime.ElapsedGameTime.Milliseconds;
		}
		else
		{
			mouseClickPolling = 0;
		}
		oldMouseState = input.GetMouseState();
		oldKBState = keyboardState;
		oldPadState = gamePadState;
	}

	public static string DateCompiled()
	{
		Version version = Assembly.GetExecutingAssembly().GetName().Version;
		return version.Major + "." + version.Minor + "." + version.Build + "." + version.Revision;
	}

	public static void updatePause(GameTime gameTime)
	{
		if (IsDedicatedHost)
		{
			pauseTime = 0f;
		}
		pauseTime -= gameTime.ElapsedGameTime.Milliseconds;
		if (player.isCrafting && random.NextDouble() < 0.007)
		{
			playSound("crafting");
		}
		if (!(pauseTime <= 0f))
		{
			return;
		}
		if (currentObjectDialogue.Count == 0)
		{
			messagePause = false;
		}
		pauseTime = 0f;
		if (!string.IsNullOrEmpty(messageAfterPause))
		{
			player.isCrafting = false;
			drawObjectDialogue(messageAfterPause);
			messageAfterPause = "";
			if (killScreen)
			{
				killScreen = false;
				player.health = 10;
			}
		}
		else if (killScreen)
		{
			multiplayer.globalChatInfoMessage("PlayerDeath", player.Name);
			screenGlow = false;
			bool flag = false;
			if (currentLocation.GetLocationContext().ReviveLocations != null)
			{
				foreach (ReviveLocation reviveLocation in currentLocation.GetLocationContext().ReviveLocations)
				{
					if (GameStateQuery.CheckConditions(reviveLocation.Condition, null, player))
					{
						warpFarmer(reviveLocation.Location, reviveLocation.Position.X, reviveLocation.Position.Y, flip: false);
						flag = true;
						break;
					}
				}
			}
			else
			{
				foreach (ReviveLocation reviveLocation2 in LocationContexts.Default.ReviveLocations)
				{
					if (GameStateQuery.CheckConditions(reviveLocation2.Condition, null, player))
					{
						warpFarmer(reviveLocation2.Location, reviveLocation2.Position.X, reviveLocation2.Position.Y, flip: false);
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				warpFarmer("Hospital", 20, 12, flip: false);
			}
		}
		if (currentLocation.currentEvent != null)
		{
			currentLocation.currentEvent.CurrentCommand++;
		}
	}

	public static void CheckValidFullscreenResolution(ref int width, ref int height)
	{
		int num = width;
		int num2 = height;
		foreach (DisplayMode supportedDisplayMode in graphics.GraphicsDevice.Adapter.SupportedDisplayModes)
		{
			if (supportedDisplayMode.Width >= 1280 && supportedDisplayMode.Width == num && supportedDisplayMode.Height == num2)
			{
				width = num;
				height = num2;
				return;
			}
		}
		foreach (DisplayMode supportedDisplayMode2 in graphics.GraphicsDevice.Adapter.SupportedDisplayModes)
		{
			if (supportedDisplayMode2.Width >= 1280 && supportedDisplayMode2.Width == graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Width && supportedDisplayMode2.Height == graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Height)
			{
				width = graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
				height = graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
				return;
			}
		}
		bool flag = false;
		foreach (DisplayMode supportedDisplayMode3 in graphics.GraphicsDevice.Adapter.SupportedDisplayModes)
		{
			if (supportedDisplayMode3.Width >= 1280 && num > supportedDisplayMode3.Width)
			{
				width = supportedDisplayMode3.Width;
				height = supportedDisplayMode3.Height;
				flag = true;
			}
		}
		if (!flag)
		{
			log.Warn("Requested fullscreen resolution not valid, switching to windowed.");
			width = 1280;
			height = 720;
			options.fullscreen = false;
		}
	}

	public static void toggleNonBorderlessWindowedFullscreen()
	{
		int width = options.preferredResolutionX;
		int height = options.preferredResolutionY;
		graphics.HardwareModeSwitch = options.fullscreen && !options.windowedBorderlessFullscreen;
		if (options.fullscreen && !options.windowedBorderlessFullscreen)
		{
			CheckValidFullscreenResolution(ref width, ref height);
		}
		if (!options.fullscreen && !options.windowedBorderlessFullscreen)
		{
			width = 1280;
			height = 720;
		}
		graphics.PreferredBackBufferWidth = width;
		graphics.PreferredBackBufferHeight = height;
		if (options.fullscreen != graphics.IsFullScreen)
		{
			graphics.ToggleFullScreen();
		}
		graphics.ApplyChanges();
		updateViewportForScreenSizeChange(fullscreenChange: true, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
		GameRunner.instance.OnWindowSizeChange(null, null);
	}

	public static void toggleFullscreen()
	{
		if (options.windowedBorderlessFullscreen)
		{
			graphics.HardwareModeSwitch = false;
			graphics.IsFullScreen = true;
			graphics.ApplyChanges();
			graphics.PreferredBackBufferWidth = Program.gamePtr.Window.ClientBounds.Width;
			graphics.PreferredBackBufferHeight = Program.gamePtr.Window.ClientBounds.Height;
		}
		else
		{
			toggleNonBorderlessWindowedFullscreen();
		}
		GameRunner.instance.OnWindowSizeChange(null, null);
	}

	private void checkForEscapeKeys()
	{
		KeyboardState keyboardState = input.GetKeyboardState();
		if (!base.IsMainInstance)
		{
			return;
		}
		if (keyboardState.IsKeyDown(Keys.LeftAlt) && keyboardState.IsKeyDown(Keys.Enter) && (oldKBState.IsKeyUp(Keys.LeftAlt) || oldKBState.IsKeyUp(Keys.Enter)))
		{
			if (options.isCurrentlyFullscreen() || options.isCurrentlyWindowedBorderless())
			{
				options.setWindowedOption(1);
			}
			else
			{
				options.setWindowedOption(0);
			}
		}
		if ((player.UsingTool || freezeControls) && keyboardState.IsKeyDown(Keys.RightShift) && keyboardState.IsKeyDown(Keys.R) && keyboardState.IsKeyDown(Keys.Delete))
		{
			freezeControls = false;
			player.forceCanMove();
			player.completelyStopAnimatingOrDoingAction();
			player.UsingTool = false;
		}
	}

	public static bool IsPressEvent(ref KeyboardState state, Keys key)
	{
		if (state.IsKeyDown(key) && !oldKBState.IsKeyDown(key))
		{
			oldKBState = state;
			return true;
		}
		return false;
	}

	public static bool IsPressEvent(ref GamePadState state, Buttons btn)
	{
		if (state.IsConnected && state.IsButtonDown(btn) && !oldPadState.IsButtonDown(btn))
		{
			oldPadState = state;
			return true;
		}
		return false;
	}

	public static bool isOneOfTheseKeysDown(KeyboardState state, InputButton[] keys)
	{
		for (int i = 0; i < keys.Length; i++)
		{
			InputButton inputButton = keys[i];
			if (inputButton.key != Keys.None && state.IsKeyDown(inputButton.key))
			{
				return true;
			}
		}
		return false;
	}

	public static bool areAllOfTheseKeysUp(KeyboardState state, InputButton[] keys)
	{
		for (int i = 0; i < keys.Length; i++)
		{
			InputButton inputButton = keys[i];
			if (inputButton.key != Keys.None && !state.IsKeyUp(inputButton.key))
			{
				return false;
			}
		}
		return true;
	}

	internal void UpdateTitleScreen(GameTime time)
	{
		if (quit)
		{
			Exit();
			changeMusicTrack("none");
		}
		switch (gameMode)
		{
		case 6:
			UpdateTitleScreenDuringLoadingMode();
			return;
		case 7:
			currentLoader.MoveNext();
			return;
		case 8:
			pauseAccumulator -= time.ElapsedGameTime.Milliseconds;
			if (pauseAccumulator <= 0f)
			{
				pauseAccumulator = 0f;
				setGameMode(3);
				if (currentObjectDialogue.Count > 0)
				{
					messagePause = true;
					pauseTime = 1E+10f;
					fadeToBlackAlpha = 1f;
					player.CanMove = false;
				}
			}
			return;
		}
		if (game1.instanceIndex > 0)
		{
			if (activeClickableMenu == null && ticks > 1)
			{
				activeClickableMenu = new FarmhandMenu(multiplayer.InitClient(new LidgrenClient("localhost")));
				activeClickableMenu.populateClickableComponentList();
				if (options.SnappyMenus)
				{
					activeClickableMenu.snapToDefaultClickableComponent();
				}
			}
			return;
		}
		if (fadeToBlackAlpha < 1f && fadeIn)
		{
			fadeToBlackAlpha += 0.02f;
		}
		else if (fadeToBlackAlpha > 0f && fadeToBlack)
		{
			fadeToBlackAlpha -= 0.02f;
		}
		if (pauseTime > 0f)
		{
			pauseTime = Math.Max(0f, pauseTime - (float)time.ElapsedGameTime.Milliseconds);
		}
		if (fadeToBlackAlpha >= 1f)
		{
			switch (gameMode)
			{
			case 4:
				if (!fadeToBlack)
				{
					fadeIn = false;
					fadeToBlack = true;
					fadeToBlackAlpha = 2.5f;
				}
				break;
			case 0:
				if (currentSong == null && pauseTime <= 0f && base.IsMainInstance)
				{
					playSound("spring_day_ambient", out var cue);
					currentSong = cue;
				}
				if (activeClickableMenu == null && !quit)
				{
					activeClickableMenu = new TitleMenu();
				}
				break;
			}
			return;
		}
		if (!(fadeToBlackAlpha <= 0f))
		{
			return;
		}
		switch (gameMode)
		{
		case 4:
			if (fadeToBlack)
			{
				fadeIn = true;
				fadeToBlack = false;
				setGameMode(0);
				pauseTime = 2000f;
			}
			break;
		case 0:
			if (fadeToBlack)
			{
				currentLoader = Utility.generateNewFarm(IsClient);
				setGameMode(6);
				loadingMessage = (IsClient ? content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2574", client.serverName) : content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2575"));
				exitActiveMenu();
			}
			break;
		}
	}

	/// <summary>Update while the <see cref="P:StardewValley.Game1.gameMode" /> is <see cref="F:StardewValley.Game1.loadingMode" /> and the <see cref="F:StardewValley.Game1.currentLoader" /> is running.</summary>
	/// <remarks>This is a low-level method; most code should call <see cref="M:StardewValley.Game1.UpdateTitleScreen(Microsoft.Xna.Framework.GameTime)" /> instead.</remarks>
	internal void UpdateTitleScreenDuringLoadingMode()
	{
		if (_requestedMusicTracks.Count > 0)
		{
			_requestedMusicTracks = new Dictionary<MusicContext, KeyValuePair<string, bool>>();
		}
		requestedMusicTrack = "none";
		requestedMusicTrackOverrideable = false;
		requestedMusicDirty = true;
		if (currentLoader != null && !currentLoader.MoveNext())
		{
			currentLoader = null;
			if (gameMode == 3)
			{
				setGameMode(3);
				fadeIn = true;
				fadeToBlackAlpha = 0.99f;
			}
			else
			{
				ExitToTitle();
			}
		}
	}

	/// <summary>Get whether the given NPC is currently constructing a building anywhere in the world.</summary>
	/// <param name="builder">The NPC constructing the building, usually <see cref="F:StardewValley.Game1.builder_robin" /> or <see cref="F:StardewValley.Game1.builder_wizard" />.</param>
	public static bool IsThereABuildingUnderConstruction(string builder = "Robin")
	{
		if (netWorldState.Value.GetBuilderData(builder) != null)
		{
			return true;
		}
		return false;
	}

	/// <summary>Get the building currently being constructed by a given builder.</summary>
	/// <param name="builder">The NPC constructing the building, usually <see cref="F:StardewValley.Game1.builder_robin" /> or <see cref="F:StardewValley.Game1.builder_wizard" />.</param>
	public static Building GetBuildingUnderConstruction(string builder = "Robin")
	{
		BuilderData builderData = netWorldState.Value.GetBuilderData(builder);
		if (builderData == null)
		{
			return null;
		}
		GameLocation locationFromName = getLocationFromName(builderData.buildingLocation.Value);
		if (locationFromName == null)
		{
			return null;
		}
		if (client != null && !multiplayer.isActiveLocation(locationFromName))
		{
			return null;
		}
		return locationFromName.getBuildingAt(Utility.PointToVector2(builderData.buildingTile.Value));
	}

	/// <summary>Get whether a building type was constructed anywhere in the world.</summary>
	/// <param name="name">The building type's ID in <c>Data/Buildings</c>.</param>
	public static bool IsBuildingConstructed(string name)
	{
		return GetNumberBuildingsConstructed(name) > 0;
	}

	/// <summary>Get the number of buildings of all types constructed anywhere in the world.</summary>
	/// <param name="includeUnderConstruction">Whether to count buildings that haven't finished construction yet.</param>
	public static int GetNumberBuildingsConstructed(bool includeUnderConstruction = false)
	{
		int num = 0;
		foreach (string locationsWithBuilding in netWorldState.Value.LocationsWithBuildings)
		{
			num += getLocationFromName(locationsWithBuilding)?.getNumberBuildingsConstructed(includeUnderConstruction) ?? 0;
		}
		return num;
	}

	/// <summary>Get the number of buildings of a given type constructed anywhere in the world.</summary>
	/// <param name="name">The building type's ID in <c>Data/Buildings</c>.</param>
	/// <param name="includeUnderConstruction">Whether to count buildings that haven't finished construction yet.</param>
	public static int GetNumberBuildingsConstructed(string name, bool includeUnderConstruction = false)
	{
		int num = 0;
		foreach (string locationsWithBuilding in netWorldState.Value.LocationsWithBuildings)
		{
			num += getLocationFromName(locationsWithBuilding)?.getNumberBuildingsConstructed(name, includeUnderConstruction) ?? 0;
		}
		return num;
	}

	private void UpdateLocations(GameTime time)
	{
		loopingLocationCues.Update(currentLocation);
		if (IsClient)
		{
			currentLocation.UpdateWhenCurrentLocation(time);
			{
				foreach (GameLocation item in multiplayer.activeLocations())
				{
					item.updateEvenIfFarmerIsntHere(time);
				}
				return;
			}
		}
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			_UpdateLocation(location, time);
			return true;
		});
		if (currentLocation.IsTemporary)
		{
			_UpdateLocation(currentLocation, time);
		}
		MineShaft.UpdateMines(time);
		VolcanoDungeon.UpdateLevels(time);
	}

	protected void _UpdateLocation(GameLocation location, GameTime time)
	{
		bool flag = location.farmers.Any();
		if (!flag && location.CanBeRemotedlyViewed())
		{
			if (player.currentLocation == location)
			{
				flag = true;
			}
			else
			{
				foreach (Farmer value in otherFarmers.Values)
				{
					if (value.viewingLocation.Value != null && value.viewingLocation.Value.Equals(location.NameOrUniqueName))
					{
						flag = true;
						break;
					}
				}
			}
		}
		if (flag)
		{
			location.UpdateWhenCurrentLocation(time);
		}
		location.updateEvenIfFarmerIsntHere(time);
		if (location.wasInhabited != flag)
		{
			location.wasInhabited = flag;
			if (IsMasterGame)
			{
				location.cleanupForVacancy();
			}
		}
	}

	public static void performTenMinuteClockUpdate()
	{
		hooks.OnGame1_PerformTenMinuteClockUpdate(delegate
		{
			int num = getTrulyDarkTime(currentLocation) - 100;
			gameTimeInterval = 0;
			if (IsMasterGame)
			{
				timeOfDay += 10;
			}
			if (timeOfDay % 100 >= 60)
			{
				timeOfDay = timeOfDay - timeOfDay % 100 + 100;
			}
			timeOfDay = Math.Min(timeOfDay, 2600);
			if (isLightning && timeOfDay < 2400 && IsMasterGame)
			{
				Utility.performLightningUpdate(timeOfDay);
			}
			if (timeOfDay == num)
			{
				currentLocation.switchOutNightTiles();
			}
			else if (timeOfDay == getModeratelyDarkTime(currentLocation) && currentLocation.IsOutdoors && !currentLocation.IsRainingHere())
			{
				ambientLight = Color.White;
			}
			if (!eventUp && isDarkOut(currentLocation) && IsPlayingBackgroundMusic)
			{
				changeMusicTrack("none", track_interruptable: true);
			}
			if (weatherIcon == 1)
			{
				Dictionary<string, string> dictionary = temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + currentSeason + dayOfMonth);
				string[] array = dictionary["conditions"].Split('/');
				int num2 = Convert.ToInt32(ArgUtility.SplitBySpaceAndGet(array[1], 0));
				if (whereIsTodaysFest == null)
				{
					whereIsTodaysFest = array[0];
				}
				if (timeOfDay == num2)
				{
					if (dictionary.TryGetValue("startedMessage", out var value))
					{
						showGlobalMessage(TokenParser.ParseText(value));
					}
					else
					{
						if (!dictionary.TryGetValue("locationDisplayName", out var value2))
						{
							value2 = array[0];
							value2 = value2 switch
							{
								"Forest" => IsWinter ? content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2634") : content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2635"), 
								"Town" => content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2637"), 
								"Beach" => content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2639"), 
								_ => TokenParser.ParseText(GameLocation.GetData(value2)?.DisplayName) ?? value2, 
							};
						}
						showGlobalMessage(content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2640", dictionary["name"]) + value2);
					}
				}
			}
			player.performTenMinuteUpdate();
			switch (timeOfDay)
			{
			case 1200:
				if (currentLocation.isOutdoors.Value && !currentLocation.IsRainingHere() && (IsPlayingOutdoorsAmbience || currentSong == null || isMusicContextActiveButNotPlaying()))
				{
					playMorningSong();
				}
				break;
			case 2000:
				if (IsPlayingTownMusic)
				{
					changeMusicTrack("none", track_interruptable: true);
				}
				break;
			case 2400:
				dayTimeMoneyBox.timeShakeTimer = 2000;
				player.doEmote(24);
				showGlobalMessage(content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2652"));
				break;
			case 2500:
				dayTimeMoneyBox.timeShakeTimer = 2000;
				player.doEmote(24);
				break;
			case 2600:
				dayTimeMoneyBox.timeShakeTimer = 2000;
				player.mount?.dismount();
				if (player.IsSitting())
				{
					player.StopSitting(animate: false);
				}
				if (player.UsingTool && (!(player.CurrentTool is FishingRod fishingRod) || (!fishingRod.isReeling && !fishingRod.pullingOutOfWater)))
				{
					if (player.UsingTool && player.CurrentTool != null && player.CurrentTool is FishingRod { fishCaught: not false } fishingRod2)
					{
						fishingRod2.doneHoldingFish(player, endOfNight: true);
					}
					else
					{
						player.completelyStopAnimatingOrDoingAction();
					}
				}
				break;
			case 2800:
				if (activeClickableMenu != null)
				{
					activeClickableMenu.emergencyShutDown();
					exitActiveMenu();
				}
				player.startToPassOut();
				player.mount?.dismount();
				break;
			}
			foreach (string activePassiveFestival in netWorldState.Value.ActivePassiveFestivals)
			{
				if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data) && timeOfDay == data.StartTime && (!data.OnlyShowMessageOnFirstDay || Utility.GetDayOfPassiveFestival(activePassiveFestival) == 1))
				{
					showGlobalMessage(TokenParser.ParseText(data.StartMessage));
				}
			}
			foreach (GameLocation location in locations)
			{
				GameLocation current2 = location;
				if (current2.NameOrUniqueName == currentLocation.NameOrUniqueName)
				{
					current2 = currentLocation;
				}
				current2.performTenMinuteUpdate(timeOfDay);
				current2.timeUpdate(10);
			}
			MineShaft.UpdateMines10Minutes(timeOfDay);
			VolcanoDungeon.UpdateLevels10Minutes(timeOfDay);
			if (IsMasterGame && farmEvent == null)
			{
				netWorldState.Value.UpdateFromGame1();
			}
			currentLightSources.RemoveWhere((KeyValuePair<string, LightSource> p) => p.Value.color.A <= 0);
		});
	}

	public static bool shouldPlayMorningSong(bool loading_game = false)
	{
		if (eventUp)
		{
			return false;
		}
		if ((double)options.musicVolumeLevel <= 0.025)
		{
			return false;
		}
		if (timeOfDay >= 1200)
		{
			return false;
		}
		if (!loading_game)
		{
			if (currentSong != null)
			{
				return IsPlayingOutdoorsAmbience;
			}
			return false;
		}
		return true;
	}

	public static void UpdateGameClock(GameTime time)
	{
		if (shouldTimePass() && !IsClient)
		{
			gameTimeInterval += time.ElapsedGameTime.Milliseconds;
		}
		if (timeOfDay >= getTrulyDarkTime(currentLocation))
		{
			int num = (int)((float)(timeOfDay - timeOfDay % 100) + (float)(timeOfDay % 100 / 10) * 16.66f);
			float num2 = Math.Min(0.93f, 0.75f + ((float)(num - getTrulyDarkTime(currentLocation)) + (float)gameTimeInterval / (float)realMilliSecondsPerGameTenMinutes * 16.6f) * 0.000625f);
			outdoorLight = (IsRainingHere() ? ambientLight : eveningColor) * num2;
		}
		else if (timeOfDay >= getStartingToGetDarkTime(currentLocation))
		{
			int num3 = (int)((float)(timeOfDay - timeOfDay % 100) + (float)(timeOfDay % 100 / 10) * 16.66f);
			float num4 = Math.Min(0.93f, 0.3f + ((float)(num3 - getStartingToGetDarkTime(currentLocation)) + (float)gameTimeInterval / (float)realMilliSecondsPerGameTenMinutes * 16.6f) * 0.00225f);
			outdoorLight = (IsRainingHere() ? ambientLight : eveningColor) * num4;
		}
		else if (IsRainingHere())
		{
			outdoorLight = ambientLight * 0.3f;
		}
		else
		{
			outdoorLight = ambientLight;
		}
		int num5 = gameTimeInterval;
		int num6 = realMilliSecondsPerGameTenMinutes;
		GameLocation gameLocation = currentLocation;
		if (num5 > num6 + ((gameLocation != null) ? new int?(gameLocation.ExtraMillisecondsPerInGameMinute * 10) : ((int?)null)))
		{
			if (panMode)
			{
				gameTimeInterval = 0;
			}
			else
			{
				performTenMinuteClockUpdate();
			}
		}
	}

	public static Event getAvailableWeddingEvent()
	{
		if (weddingsToday.Count > 0)
		{
			long id = weddingsToday[0];
			weddingsToday.RemoveAt(0);
			Farmer farmer = GetPlayer(id);
			if (farmer == null)
			{
				return null;
			}
			if (farmer.hasRoommate())
			{
				return null;
			}
			if (farmer.spouse != null)
			{
				return Utility.getWeddingEvent(farmer);
			}
			long? spouse = farmer.team.GetSpouse(farmer.UniqueMultiplayerID);
			Farmer farmer2 = GetPlayer(spouse.Value);
			if (farmer2 == null)
			{
				return null;
			}
			if (!getOnlineFarmers().Contains(farmer) || !getOnlineFarmers().Contains(farmer2))
			{
				return null;
			}
			player.team.GetFriendship(farmer.UniqueMultiplayerID, spouse.Value).Status = FriendshipStatus.Married;
			player.team.GetFriendship(farmer.UniqueMultiplayerID, spouse.Value).WeddingDate = new WorldDate(Date);
			return Utility.getWeddingEvent(farmer);
		}
		return null;
	}

	public static void exitActiveMenu()
	{
		activeClickableMenu = null;
	}

	/// <summary>Perform an action when <see cref="M:StardewValley.Farmer.IsBusyDoingSomething" /> becomes false for the current player (or do it immediately if it's already false).</summary>
	/// <param name="action">The action to perform.</param>
	public static void PerformActionWhenPlayerFree(Action action)
	{
		if (player.IsBusyDoingSomething())
		{
			actionsWhenPlayerFree.Add(action);
		}
		else
		{
			action();
		}
	}

	public static void fadeScreenToBlack()
	{
		screenFade.FadeScreenToBlack();
	}

	public static void fadeClear()
	{
		screenFade.FadeClear();
	}

	private bool onFadeToBlackComplete()
	{
		bool result = false;
		if (killScreen)
		{
			viewportFreeze = true;
			viewport.X = -10000;
		}
		if (exitToTitle)
		{
			setGameMode(4);
			fadeIn = false;
			fadeToBlack = true;
			fadeToBlackAlpha = 0.01f;
			exitToTitle = false;
			changeMusicTrack("none");
			debrisWeather.Clear();
			return true;
		}
		if (timeOfDayAfterFade != -1)
		{
			timeOfDay = timeOfDayAfterFade;
			timeOfDayAfterFade = -1;
		}
		if (!nonWarpFade && locationRequest != null)
		{
			if (IsMasterGame && locationRequest.Location == null)
			{
				log.Error("Warp to " + locationRequest.Name + " failed: location wasn't found or couldn't be loaded.");
				locationRequest = null;
			}
			if (locationRequest != null)
			{
				GameLocation location = currentLocation;
				emoteMenu?.exitThisMenuNoSound();
				if (client != null)
				{
					currentLocation?.StoreCachedMultiplayerMap(multiplayer.cachedMultiplayerMaps);
				}
				currentLocation.cleanupBeforePlayerExit();
				multiplayer.broadcastLocationDelta(currentLocation);
				bool flag = false;
				displayFarmer = true;
				if (eventOver)
				{
					eventFinished();
					if (dayOfMonth == 0)
					{
						newDayAfterFade(delegate
						{
							player.Position = new Vector2(320f, 320f);
						});
					}
					return true;
				}
				if (locationRequest.IsRequestFor(currentLocation) && player.previousLocationName != "" && !eventUp && !MineShaft.IsGeneratedLevel(currentLocation))
				{
					player.Position = new Vector2(xLocationAfterWarp * 64, yLocationAfterWarp * 64 - (player.Sprite.getHeight() - 32) + 16);
					viewportFreeze = false;
					currentLocation.resetForPlayerEntry();
					flag = true;
				}
				else
				{
					if (MineShaft.IsGeneratedLevel(locationRequest.Name))
					{
						MineShaft mineShaft = locationRequest.Location as MineShaft;
						if (player.IsSitting())
						{
							player.StopSitting(animate: false);
						}
						player.Halt();
						player.forceCanMove();
						if (!IsClient || locationRequest.Location?.Root != null)
						{
							currentLocation = mineShaft;
							mineShaft.resetForPlayerEntry();
							flag = true;
						}
						currentLocation.Map.LoadTileSheets(mapDisplayDevice);
						checkForRunButton(GetKeyboardState());
					}
					if (!eventUp)
					{
						player.Position = new Vector2(xLocationAfterWarp * 64, yLocationAfterWarp * 64 - (player.Sprite.getHeight() - 32) + 16);
					}
					if (!MineShaft.IsGeneratedLevel(locationRequest.Name) && locationRequest.Location != null)
					{
						currentLocation = locationRequest.Location;
						if (!IsClient)
						{
							locationRequest.Loaded(locationRequest.Location);
							currentLocation.resetForPlayerEntry();
							flag = true;
						}
						currentLocation.Map.LoadTileSheets(mapDisplayDevice);
						if (!viewportFreeze && currentLocation.Map.DisplayWidth <= viewport.Width)
						{
							viewport.X = (currentLocation.Map.DisplayWidth - viewport.Width) / 2;
						}
						if (!viewportFreeze && currentLocation.Map.DisplayHeight <= viewport.Height)
						{
							viewport.Y = (currentLocation.Map.DisplayHeight - viewport.Height) / 2;
						}
						checkForRunButton(GetKeyboardState(), ignoreKeyPressQualifier: true);
					}
					if (!eventUp)
					{
						viewportFreeze = false;
					}
				}
				forceSnapOnNextViewportUpdate = true;
				player.FarmerSprite.PauseForSingleAnimation = false;
				player.faceDirection(facingDirectionAfterWarp);
				_isWarping = false;
				if (player.ActiveObject != null)
				{
					player.showCarrying();
				}
				else
				{
					player.showNotCarrying();
				}
				if (IsClient)
				{
					if (locationRequest.Location != null && locationRequest.Location.Root != null && multiplayer.isActiveLocation(locationRequest.Location))
					{
						if (HasDedicatedHost)
						{
							notifyServerOfWarp(needsLocationInfo: false);
						}
						currentLocation = locationRequest.Location;
						locationRequest.Loaded(locationRequest.Location);
						if (!flag)
						{
							currentLocation.resetForPlayerEntry();
						}
						player.currentLocation = currentLocation;
						locationRequest.Warped(currentLocation);
						currentLocation.updateSeasonalTileSheets();
						if (IsDebrisWeatherHere())
						{
							populateDebrisWeatherArray();
						}
						warpingForForcedRemoteEvent = false;
						locationRequest = null;
					}
					else
					{
						requestLocationInfoFromServer();
						if (currentLocation == null)
						{
							return true;
						}
					}
				}
				else
				{
					player.currentLocation = locationRequest.Location;
					locationRequest.Warped(locationRequest.Location);
					locationRequest = null;
				}
				if (locationRequest == null && currentLocation.Name == "Farm" && !eventUp)
				{
					if (player.position.X / 64f >= (float)(currentLocation.map.Layers[0].LayerWidth - 1))
					{
						player.position.X -= 64f;
					}
					else if (player.position.Y / 64f >= (float)(currentLocation.map.Layers[0].LayerHeight - 1))
					{
						player.position.Y -= 32f;
					}
					if (player.position.Y / 64f >= (float)(currentLocation.map.Layers[0].LayerHeight - 2))
					{
						player.position.X -= 48f;
					}
				}
				if (MineShaft.IsGeneratedLevel(location) && currentLocation != null && !MineShaft.IsGeneratedLevel(currentLocation))
				{
					MineShaft.OnLeftMines();
				}
				player.OnWarp();
				result = true;
			}
		}
		if (newDay)
		{
			newDayAfterFade(AfterNewDay);
			return true;
		}
		if (eventOver)
		{
			eventFinished();
			if (dayOfMonth == 0)
			{
				newDayAfterFade(AfterEventOver);
			}
			return true;
		}
		if (currentSong?.Name == "rain" && currentLocation.IsRainingHere())
		{
			if (currentLocation.IsOutdoors)
			{
				currentSong.SetVariable("Frequency", 100f);
			}
			else if (!MineShaft.IsGeneratedLevel(currentLocation.Name))
			{
				currentSong.SetVariable("Frequency", 15f);
			}
		}
		return result;
		static void AfterEventOver()
		{
			currentLocation.resetForPlayerEntry();
			nonWarpFade = false;
			fadeIn = false;
		}
		static void AfterNewDay()
		{
			if (eventOver)
			{
				eventFinished();
				if (dayOfMonth == 0)
				{
					newDayAfterFade(delegate
					{
						player.Position = new Vector2(320f, 320f);
					});
				}
			}
			nonWarpFade = false;
			fadeIn = false;
		}
	}

	/// <summary>Update game state when the current player finishes warping to a new location.</summary>
	/// <param name="oldLocation">The location which the player just left (or <c>null</c> for the first location after loading the save).</param>
	/// <param name="newLocation">The location which the player just arrived in.</param>
	public static void OnLocationChanged(GameLocation oldLocation, GameLocation newLocation)
	{
		if (!hasLoadedGame)
		{
			return;
		}
		eventsSeenSinceLastLocationChange.Clear();
		if (newLocation.Name != null && !MineShaft.IsGeneratedLevel(newLocation) && !VolcanoDungeon.IsGeneratedLevel(newLocation.Name))
		{
			player.locationsVisited.Add(newLocation.Name);
		}
		if (newLocation.IsOutdoors && !newLocation.ignoreDebrisWeather.Value && newLocation.IsDebrisWeatherHere() && GetSeasonForLocation(newLocation) != debrisWeatherSeason)
		{
			windGust = 0f;
			WeatherDebris.globalWind = 0f;
			populateDebrisWeatherArray();
			if (wind != null)
			{
				wind.Stop(AudioStopOptions.AsAuthored);
				wind = null;
			}
		}
		GameLocation.HandleMusicChange(oldLocation, newLocation);
		TriggerActionManager.Raise("LocationChanged");
	}

	private static void onFadedBackInComplete()
	{
		if (killScreen)
		{
			pauseThenMessage(1500, "..." + player.Name + "?");
		}
		else if (!eventUp)
		{
			player.CanMove = true;
		}
		checkForRunButton(oldKBState, ignoreKeyPressQualifier: true);
	}

	public static void UpdateOther(GameTime time)
	{
		if (currentLocation == null || (!player.passedOut && screenFade.UpdateFade(time)))
		{
			return;
		}
		StardewValley.Pokemon.PokemonHUD.Instance.Update(time);
		if (dialogueUp)
		{
			player.CanMove = false;
		}
		for (int num = delayedActions.Count - 1; num >= 0; num--)
		{
			DelayedAction delayedAction = delayedActions[num];
			if (delayedAction.update(time) && delayedActions.Contains(delayedAction))
			{
				delayedActions.Remove(delayedAction);
			}
		}
		if (timeOfDay >= 2600 || player.stamina <= -15f)
		{
			if (currentMinigame != null && currentMinigame.forceQuit())
			{
				currentMinigame = null;
			}
			if (currentMinigame == null && player.canMove && player.freezePause <= 0 && !player.UsingTool && !eventUp && (IsMasterGame || player.isCustomized.Value) && locationRequest == null && activeClickableMenu == null)
			{
				player.startToPassOut();
				player.freezePause = 7000;
			}
		}
		screenOverlayTempSprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.update(time));
		uiOverlayTempSprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.update(time));
		if ((player.CanMove || player.UsingTool) && shouldTimePass())
		{
			buffsDisplay.update(time);
		}
		player.CurrentItem?.actionWhenBeingHeld(player);
		float num2 = dialogueButtonScale;
		dialogueButtonScale = (float)(16.0 * Math.Sin(time.TotalGameTime.TotalMilliseconds % 1570.0 / 500.0));
		if (num2 > dialogueButtonScale && !dialogueButtonShrinking)
		{
			dialogueButtonShrinking = true;
		}
		else if (num2 < dialogueButtonScale && dialogueButtonShrinking)
		{
			dialogueButtonShrinking = false;
		}
		if (screenGlow)
		{
			if (screenGlowUp || screenGlowHold)
			{
				if (screenGlowHold)
				{
					screenGlowAlpha = Math.Min(screenGlowAlpha + screenGlowRate, screenGlowMax);
				}
				else
				{
					screenGlowAlpha = Math.Min(screenGlowAlpha + 0.03f, 0.6f);
					if (screenGlowAlpha >= 0.6f)
					{
						screenGlowUp = false;
					}
				}
			}
			else
			{
				screenGlowAlpha -= 0.01f;
				if (screenGlowAlpha <= 0f)
				{
					screenGlow = false;
				}
			}
		}
		hudMessages.RemoveAll((HUDMessage hudMessage) => hudMessage.update(time));
		updateWeather(time);
		if (!fadeToBlack)
		{
			currentLocation.checkForMusic(time);
		}
		if (debrisSoundInterval > 0f)
		{
			debrisSoundInterval -= time.ElapsedGameTime.Milliseconds;
		}
		noteBlockTimer += time.ElapsedGameTime.Milliseconds;
		if (noteBlockTimer > 1000f)
		{
			noteBlockTimer = 0f;
			if (player.health < 20 && CurrentEvent == null)
			{
				hitShakeTimer = 250;
				if (player.health <= 10)
				{
					hitShakeTimer = 500;
					if (showingHealthBar && fadeToBlackAlpha <= 0f)
					{
						for (int num3 = 0; num3 < 3; num3++)
						{
							uiOverlayTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(366, 412, 5, 6), new Vector2(random.Next(32) + uiViewport.Width - 112, uiViewport.Height - 224 - (player.maxHealth - 100) - 16 + 4), flipped: false, 0.017f, Color.Red)
							{
								motion = new Vector2(-1.5f, -8 + random.Next(-1, 2)),
								acceleration = new Vector2(0f, 0.5f),
								local = true,
								scale = 4f,
								delayBeforeAnimationStart = num3 * 150
							});
						}
					}
				}
			}
		}
		drawLighting = (currentLocation.IsOutdoors && !outdoorLight.Equals(Color.White)) || !ambientLight.Equals(Color.White) || (currentLocation is MineShaft && !((MineShaft)currentLocation).getLightingColor(time).Equals(Color.White));
		if (player.hasBuff("26"))
		{
			drawLighting = true;
		}
		if (hitShakeTimer > 0)
		{
			hitShakeTimer -= time.ElapsedGameTime.Milliseconds;
		}
		if (staminaShakeTimer > 0)
		{
			staminaShakeTimer -= time.ElapsedGameTime.Milliseconds;
		}
		background?.update(viewport);
		cursorTileHintCheckTimer -= (int)time.ElapsedGameTime.TotalMilliseconds;
		currentCursorTile.X = (viewport.X + getOldMouseX()) / 64;
		currentCursorTile.Y = (viewport.Y + getOldMouseY()) / 64;
		if (cursorTileHintCheckTimer <= 0 || !currentCursorTile.Equals(lastCursorTile))
		{
			cursorTileHintCheckTimer = 250;
			updateCursorTileHint();
			if (player.CanMove)
			{
				checkForRunButton(oldKBState, ignoreKeyPressQualifier: true);
			}
		}
		if (!MineShaft.IsGeneratedLevel(currentLocation.Name))
		{
			MineShaft.timeSinceLastMusic = 200000;
		}
		if (activeClickableMenu == null && farmEvent == null && keyboardDispatcher != null && !IsChatting)
		{
			keyboardDispatcher.Subscriber = null;
		}
	}

	public static void updateWeather(GameTime time)
	{
		if (currentLocation.IsOutdoors && currentLocation.IsSnowingHere())
		{
			snowPos = updateFloatingObjectPositionForMovement(current: new Vector2(viewport.X, viewport.Y), w: snowPos, previous: previousViewportPosition, speed: -1f);
			return;
		}
		if (currentLocation.IsOutdoors && currentLocation.IsRainingHere())
		{
			for (int i = 0; i < rainDrops.Length; i++)
			{
				if (rainDrops[i].frame == 0)
				{
					rainDrops[i].accumulator += time.ElapsedGameTime.Milliseconds;
					if (rainDrops[i].accumulator < 70)
					{
						continue;
					}
					rainDrops[i].position += new Vector2(-16 + i * 8 / rainDrops.Length, 32 - i * 8 / rainDrops.Length);
					rainDrops[i].accumulator = 0;
					if (random.NextDouble() < 0.1)
					{
						rainDrops[i].frame++;
					}
					if (currentLocation is IslandNorth || currentLocation is Caldera)
					{
						Point tile = new Point((int)(rainDrops[i].position.X + (float)viewport.X) / 64, (int)(rainDrops[i].position.Y + (float)viewport.Y) / 64);
						tile.Y--;
						if (currentLocation.isTileOnMap(tile.X, tile.Y) && !currentLocation.hasTileAt(tile, "Back") && !currentLocation.hasTileAt(tile, "Buildings"))
						{
							rainDrops[i].frame = 0;
						}
					}
					if (rainDrops[i].position.Y > (float)(viewport.Height + 64))
					{
						rainDrops[i].position.Y = -64f;
					}
					continue;
				}
				rainDrops[i].accumulator += time.ElapsedGameTime.Milliseconds;
				if (rainDrops[i].accumulator > 70)
				{
					rainDrops[i].frame = (rainDrops[i].frame + 1) % 4;
					rainDrops[i].accumulator = 0;
					if (rainDrops[i].frame == 0)
					{
						rainDrops[i].position = new Vector2(random.Next(viewport.Width), random.Next(viewport.Height));
					}
				}
			}
			return;
		}
		if (currentLocation.IsOutdoors && !currentLocation.ignoreDebrisWeather.Value && currentLocation.IsDebrisWeatherHere())
		{
			if (currentLocation.GetSeason() == Season.Fall)
			{
				if (WeatherDebris.globalWind == 0f)
				{
					WeatherDebris.globalWind = -0.5f;
				}
				if (random.NextDouble() < 0.001 && windGust == 0f && WeatherDebris.globalWind >= -0.5f)
				{
					windGust += (float)random.Next(-10, -1) / 100f;
					playSound("wind", out wind);
				}
				else if (windGust != 0f)
				{
					windGust = Math.Max(-5f, windGust * 1.02f);
					WeatherDebris.globalWind = -0.5f + windGust;
					if (windGust < -0.2f && random.NextDouble() < 0.007)
					{
						windGust = 0f;
					}
				}
				if (WeatherDebris.globalWind < -0.5f)
				{
					WeatherDebris.globalWind = Math.Min(-0.5f, WeatherDebris.globalWind + 0.015f);
					if (wind != null)
					{
						wind.SetVariable("Volume", (0f - WeatherDebris.globalWind) * 20f);
						wind.SetVariable("Frequency", (0f - WeatherDebris.globalWind) * 20f);
						if (WeatherDebris.globalWind == -0.5f)
						{
							wind.Stop(AudioStopOptions.AsAuthored);
						}
					}
				}
			}
			else
			{
				if (WeatherDebris.globalWind == 0f)
				{
					WeatherDebris.globalWind = -0.25f;
				}
				if (wind != null)
				{
					wind.Stop(AudioStopOptions.AsAuthored);
					wind = null;
				}
			}
			{
				foreach (WeatherDebris item in debrisWeather)
				{
					item.update();
				}
				return;
			}
		}
		if (wind != null)
		{
			wind.Stop(AudioStopOptions.AsAuthored);
			wind = null;
		}
	}

	public static void updateCursorTileHint()
	{
		if (activeClickableMenu != null)
		{
			return;
		}
		mouseCursorTransparency = 1f;
		isActionAtCurrentCursorTile = false;
		isInspectionAtCurrentCursorTile = false;
		isSpeechAtCurrentCursorTile = false;
		int xTile = (viewport.X + getOldMouseX()) / 64;
		int num = (viewport.Y + getOldMouseY()) / 64;
		if (currentLocation != null)
		{
			isActionAtCurrentCursorTile = currentLocation.isActionableTile(xTile, num, player);
			if (!isActionAtCurrentCursorTile)
			{
				isActionAtCurrentCursorTile = currentLocation.isActionableTile(xTile, num + 1, player);
			}
		}
		lastCursorTile = currentCursorTile;
	}

	public static void updateMusic()
	{
		if (game1.IsMainInstance)
		{
			Game1 game = null;
			string text = null;
			int num = 1;
			int num2 = 2;
			int num3 = 5;
			int num4 = 6;
			int num5 = 7;
			int num6 = 0;
			float num7 = GetDefaultSongPriority(getMusicTrackName(), game1.instanceIsOverridingTrack, game1);
			MusicContext musicContext = MusicContext.Default;
			foreach (Game1 gameInstance in GameRunner.instance.gameInstances)
			{
				MusicContext instanceActiveMusicContext = gameInstance._instanceActiveMusicContext;
				if (gameInstance.IsMainInstance)
				{
					musicContext = instanceActiveMusicContext;
				}
				string text2 = null;
				string text3 = null;
				if (gameInstance._instanceRequestedMusicTracks.TryGetValue(instanceActiveMusicContext, out var value))
				{
					text2 = value.Key;
				}
				if (gameInstance.instanceIsOverridingTrack && gameInstance.instanceCurrentSong != null)
				{
					text3 = gameInstance.instanceCurrentSong.Name;
				}
				switch (instanceActiveMusicContext)
				{
				case MusicContext.Event:
					if (num6 < num4 && text2 != null)
					{
						num6 = num4;
						game = gameInstance;
						text = text2;
					}
					break;
				case MusicContext.MiniGame:
					if (num6 < num3 && text2 != null)
					{
						num6 = num3;
						game = gameInstance;
						text = text2;
					}
					break;
				case MusicContext.SubLocation:
					if (num6 < num && text2 != null)
					{
						num6 = num;
						game = gameInstance;
						text = ((text3 == null) ? text2 : text3);
					}
					break;
				case MusicContext.Default:
					if (text2 == "mermaidSong")
					{
						num6 = num5;
						game = gameInstance;
						text = text2;
					}
					if (musicContext <= instanceActiveMusicContext && text2 != null)
					{
						float num8 = GetDefaultSongPriority(text2, gameInstance.instanceIsOverridingTrack, gameInstance);
						if (num7 < num8)
						{
							num7 = num8;
							num6 = num2;
							game = gameInstance;
							text = ((text3 == null) ? text2 : text3);
						}
					}
					break;
				}
			}
			if (game == null || game == game1)
			{
				if (doesMusicContextHaveTrack(MusicContext.ImportantSplitScreenMusic))
				{
					stopMusicTrack(MusicContext.ImportantSplitScreenMusic);
				}
			}
			else if (text == null && doesMusicContextHaveTrack(MusicContext.ImportantSplitScreenMusic))
			{
				stopMusicTrack(MusicContext.ImportantSplitScreenMusic);
			}
			else if (text != null && getMusicTrackName(MusicContext.ImportantSplitScreenMusic) != text)
			{
				changeMusicTrack(text, track_interruptable: false, MusicContext.ImportantSplitScreenMusic);
			}
		}
		string text4 = null;
		bool flag = false;
		bool flag2 = false;
		if (currentLocation != null && currentLocation.IsMiniJukeboxPlaying() && (!requestedMusicDirty || requestedMusicTrackOverrideable) && currentTrackOverrideable)
		{
			text4 = null;
			flag2 = true;
			string text5 = currentLocation.miniJukeboxTrack.Value;
			if (text5 == "random")
			{
				text5 = ((currentLocation.randomMiniJukeboxTrack.Value != null) ? currentLocation.randomMiniJukeboxTrack.Value : "");
			}
			if (currentSong == null || !currentSong.IsPlaying || currentSong.Name != text5)
			{
				if (!soundBank.Exists(text5))
				{
					log.Error($"Location {currentLocation.NameOrUniqueName} has invalid jukebox track '{text5}' selected, turning off jukebox.");
					player.currentLocation.miniJukeboxTrack.Value = "";
				}
				else
				{
					text4 = text5;
					requestedMusicDirty = false;
					flag = true;
				}
			}
		}
		if (isOverridingTrack != flag2)
		{
			isOverridingTrack = flag2;
			if (!isOverridingTrack)
			{
				requestedMusicDirty = true;
			}
		}
		if (requestedMusicDirty)
		{
			text4 = requestedMusicTrack;
			flag = requestedMusicTrackOverrideable;
		}
		if (!string.IsNullOrEmpty(text4))
		{
			musicPlayerVolume = Math.Max(0f, Math.Min(options.musicVolumeLevel, musicPlayerVolume - 0.01f));
			ambientPlayerVolume = Math.Max(0f, Math.Min(options.musicVolumeLevel, ambientPlayerVolume - 0.01f));
			if (game1.IsMainInstance)
			{
				musicCategory.SetVolume(musicPlayerVolume);
				ambientCategory.SetVolume(ambientPlayerVolume);
			}
			if (musicPlayerVolume != 0f || ambientPlayerVolume != 0f)
			{
				return;
			}
			if (text4 == "none" || text4 == "silence")
			{
				if (game1.IsMainInstance && currentSong != null)
				{
					currentSong.Stop(AudioStopOptions.Immediate);
					currentSong.Dispose();
					currentSong = null;
				}
			}
			else if ((options.musicVolumeLevel != 0f || options.ambientVolumeLevel != 0f) && (text4 != "rain" || endOfNightMenus.Count == 0))
			{
				if (game1.IsMainInstance && currentSong != null)
				{
					currentSong.Stop(AudioStopOptions.Immediate);
					currentSong.Dispose();
					currentSong = null;
				}
				currentSong = soundBank.GetCue(text4);
				if (game1.IsMainInstance)
				{
					currentSong.Play();
				}
				if (game1.IsMainInstance && currentSong != null && currentSong.Name == "rain" && currentLocation != null)
				{
					if (IsRainingHere())
					{
						if (currentLocation.IsOutdoors)
						{
							currentSong.SetVariable("Frequency", 100f);
						}
						else if (!MineShaft.IsGeneratedLevel(currentLocation))
						{
							currentSong.SetVariable("Frequency", 15f);
						}
					}
					else if (eventUp)
					{
						currentSong.SetVariable("Frequency", 100f);
					}
				}
			}
			else
			{
				currentSong?.Stop(AudioStopOptions.Immediate);
			}
			currentTrackOverrideable = flag;
			requestedMusicDirty = false;
		}
		else if (MusicDuckTimer > 0f)
		{
			MusicDuckTimer -= (float)currentGameTime.ElapsedGameTime.TotalMilliseconds;
			musicPlayerVolume = Math.Max(musicPlayerVolume - options.musicVolumeLevel / 33f, options.musicVolumeLevel / 12f);
			if (game1.IsMainInstance)
			{
				musicCategory.SetVolume(musicPlayerVolume);
			}
		}
		else if (musicPlayerVolume < options.musicVolumeLevel || ambientPlayerVolume < options.ambientVolumeLevel)
		{
			if (musicPlayerVolume < options.musicVolumeLevel)
			{
				musicPlayerVolume = Math.Min(1f, musicPlayerVolume += 0.01f);
				if (game1.IsMainInstance)
				{
					musicCategory.SetVolume(musicPlayerVolume);
				}
			}
			if (ambientPlayerVolume < options.ambientVolumeLevel)
			{
				ambientPlayerVolume = Math.Min(1f, ambientPlayerVolume += 0.015f);
				if (game1.IsMainInstance)
				{
					ambientCategory.SetVolume(ambientPlayerVolume);
				}
			}
		}
		else if (currentSong != null && !currentSong.IsPlaying && !currentSong.IsStopped)
		{
			currentSong = soundBank.GetCue(currentSong.Name);
			if (game1.IsMainInstance)
			{
				currentSong.Play();
			}
		}
	}

	public static int GetDefaultSongPriority(string song_name, bool is_playing_override, Game1 instance)
	{
		if (is_playing_override)
		{
			return 9;
		}
		if (song_name == "none")
		{
			return 0;
		}
		if (instance._instanceIsPlayingOutdoorsAmbience || instance._instanceIsPlayingNightAmbience || song_name == "rain")
		{
			return 1;
		}
		if (instance._instanceIsPlayingMorningSong)
		{
			return 2;
		}
		if (instance._instanceIsPlayingTownMusic)
		{
			return 3;
		}
		if (song_name == "jungle_ambience")
		{
			return 7;
		}
		if (instance._instanceIsPlayingBackgroundMusic)
		{
			return 8;
		}
		if (instance.instanceGameLocation is MineShaft)
		{
			if (song_name.Contains("Ambient"))
			{
				return 7;
			}
			if (song_name.EndsWith("Mine"))
			{
				return 20;
			}
		}
		return 10;
	}

	public static void updateRainDropPositionForPlayerMovement(int direction, float speed)
	{
		if (currentLocation.IsRainingHere())
		{
			for (int i = 0; i < rainDrops.Length; i++)
			{
				switch (direction)
				{
				case 0:
					rainDrops[i].position.Y += speed;
					if (rainDrops[i].position.Y > (float)(viewport.Height + 64))
					{
						rainDrops[i].position.Y = -64f;
					}
					break;
				case 1:
					rainDrops[i].position.X -= speed;
					if (rainDrops[i].position.X < -64f)
					{
						rainDrops[i].position.X = viewport.Width;
					}
					break;
				case 2:
					rainDrops[i].position.Y -= speed;
					if (rainDrops[i].position.Y < -64f)
					{
						rainDrops[i].position.Y = viewport.Height;
					}
					break;
				case 3:
					rainDrops[i].position.X += speed;
					if (rainDrops[i].position.X > (float)(viewport.Width + 64))
					{
						rainDrops[i].position.X = -64f;
					}
					break;
				}
			}
		}
		else
		{
			updateDebrisWeatherForMovement(debrisWeather, direction, speed);
		}
	}

	public static void initializeVolumeLevels()
	{
		if (!LocalMultiplayer.IsLocalMultiplayer() || game1.IsMainInstance)
		{
			soundCategory.SetVolume(options.soundVolumeLevel);
			musicCategory.SetVolume(options.musicVolumeLevel);
			ambientCategory.SetVolume(options.ambientVolumeLevel);
			footstepCategory.SetVolume(options.footstepVolumeLevel);
		}
	}

	public static void updateDebrisWeatherForMovement(List<WeatherDebris> debris, int direction, float speed)
	{
		if (!(fadeToBlackAlpha <= 0f) || debris == null)
		{
			return;
		}
		foreach (WeatherDebris debri in debris)
		{
			switch (direction)
			{
			case 0:
				debri.position.Y += speed;
				if (debri.position.Y > (float)(viewport.Height + 64))
				{
					debri.position.Y = -64f;
				}
				break;
			case 1:
				debri.position.X -= speed;
				if (debri.position.X < -64f)
				{
					debri.position.X = viewport.Width;
				}
				break;
			case 2:
				debri.position.Y -= speed;
				if (debri.position.Y < -64f)
				{
					debri.position.Y = viewport.Height;
				}
				break;
			case 3:
				debri.position.X += speed;
				if (debri.position.X > (float)(viewport.Width + 64))
				{
					debri.position.X = -64f;
				}
				break;
			}
		}
	}

	public static Vector2 updateFloatingObjectPositionForMovement(Vector2 w, Vector2 current, Vector2 previous, float speed)
	{
		if (current.Y < previous.Y)
		{
			w.Y -= Math.Abs(current.Y - previous.Y) * speed;
		}
		else if (current.Y > previous.Y)
		{
			w.Y += Math.Abs(current.Y - previous.Y) * speed;
		}
		if (current.X > previous.X)
		{
			w.X += Math.Abs(current.X - previous.X) * speed;
		}
		else if (current.X < previous.X)
		{
			w.X -= Math.Abs(current.X - previous.X) * speed;
		}
		return w;
	}

	public static void updateRaindropPosition()
	{
		if (HostPaused)
		{
			return;
		}
		if (IsRainingHere())
		{
			int num = viewport.X - (int)previousViewportPosition.X;
			int num2 = viewport.Y - (int)previousViewportPosition.Y;
			for (int i = 0; i < rainDrops.Length; i++)
			{
				rainDrops[i].position.X -= (float)num * 1f;
				rainDrops[i].position.Y -= (float)num2 * 1f;
				if (rainDrops[i].position.Y > (float)(viewport.Height + 64))
				{
					rainDrops[i].position.Y = -64f;
				}
				else if (rainDrops[i].position.X < -64f)
				{
					rainDrops[i].position.X = viewport.Width;
				}
				else if (rainDrops[i].position.Y < -64f)
				{
					rainDrops[i].position.Y = viewport.Height;
				}
				else if (rainDrops[i].position.X > (float)(viewport.Width + 64))
				{
					rainDrops[i].position.X = -64f;
				}
			}
		}
		else
		{
			updateDebrisWeatherForMovement(debrisWeather);
		}
	}

	public static void updateDebrisWeatherForMovement(List<WeatherDebris> debris)
	{
		if (HostPaused || debris == null || !(fadeToBlackAlpha < 1f))
		{
			return;
		}
		int num = viewport.X - (int)previousViewportPosition.X;
		int num2 = viewport.Y - (int)previousViewportPosition.Y;
		if (Math.Abs(num) > 100 || Math.Abs(num2) > 80)
		{
			return;
		}
		int num3 = 16;
		foreach (WeatherDebris debri in debris)
		{
			debri.position.X -= (float)num * 1f;
			debri.position.Y -= (float)num2 * 1f;
			if (debri.position.Y > (float)(viewport.Height + 64 + num3))
			{
				debri.position.Y = -64f;
			}
			else if (debri.position.X < (float)(-64 - num3))
			{
				debri.position.X = viewport.Width;
			}
			else if (debri.position.Y < (float)(-64 - num3))
			{
				debri.position.Y = viewport.Height;
			}
			else if (debri.position.X > (float)(viewport.Width + 64 + num3))
			{
				debri.position.X = -64f;
			}
		}
	}

	public static void randomizeRainPositions()
	{
		for (int i = 0; i < 70; i++)
		{
			rainDrops[i] = new RainDrop(random.Next(viewport.Width), random.Next(viewport.Height), random.Next(4), random.Next(70));
		}
	}

	public static void randomizeDebrisWeatherPositions(List<WeatherDebris> debris)
	{
		if (debris == null)
		{
			return;
		}
		foreach (WeatherDebris debri in debris)
		{
			debri.position = Utility.getRandomPositionOnScreen();
		}
	}

	public static void eventFinished()
	{
		player.canOnlyWalk = false;
		if (player.bathingClothes.Value)
		{
			player.canOnlyWalk = true;
		}
		eventOver = false;
		eventUp = false;
		player.CanMove = true;
		displayHUD = true;
		player.faceDirection(player.orientationBeforeEvent);
		player.completelyStopAnimatingOrDoingAction();
		viewportFreeze = false;
		Action action = null;
		if (currentLocation.currentEvent?.onEventFinished != null)
		{
			action = currentLocation.currentEvent.onEventFinished;
			currentLocation.currentEvent.onEventFinished = null;
		}
		LocationRequest locationRequest = null;
		if (currentLocation.currentEvent != null)
		{
			locationRequest = currentLocation.currentEvent.exitLocation;
			currentLocation.currentEvent.cleanup();
			currentLocation.currentEvent = null;
		}
		if (player.ActiveObject != null)
		{
			player.showCarrying();
		}
		if (dayOfMonth != 0)
		{
			currentLightSources.Clear();
		}
		if (locationRequest == null && currentLocation != null && Game1.locationRequest == null)
		{
			locationRequest = new LocationRequest(currentLocation.NameOrUniqueName, currentLocation.isStructure.Value, currentLocation);
		}
		if (locationRequest != null)
		{
			if (locationRequest.Location is Farm && player.positionBeforeEvent.Y == 64f)
			{
				player.positionBeforeEvent.X += 1f;
			}
			locationRequest.OnWarp += delegate
			{
				player.locationBeforeForcedEvent.Value = null;
			};
			if (locationRequest.Location == currentLocation)
			{
				GameLocation.HandleMusicChange(currentLocation, currentLocation);
			}
			warpFarmer(locationRequest, (int)player.positionBeforeEvent.X, (int)player.positionBeforeEvent.Y, player.orientationBeforeEvent);
		}
		else
		{
			GameLocation.HandleMusicChange(currentLocation, currentLocation);
			player.setTileLocation(player.positionBeforeEvent);
			player.locationBeforeForcedEvent.Value = null;
		}
		nonWarpFade = false;
		fadeToBlackAlpha = 1f;
		action?.Invoke();
	}

	public static void populateDebrisWeatherArray()
	{
		Season seasonForLocation = GetSeasonForLocation(currentLocation);
		int num = random.Next(16, 64);
		int which = seasonForLocation switch
		{
			Season.Fall => 2, 
			Season.Winter => 3, 
			Season.Summer => 1, 
			_ => 0, 
		};
		isDebrisWeather = true;
		debrisWeatherSeason = seasonForLocation;
		debrisWeather.Clear();
		for (int i = 0; i < num; i++)
		{
			debrisWeather.Add(new WeatherDebris(new Vector2(random.Next(0, viewport.Width), random.Next(0, viewport.Height)), which, (float)random.Next(15) / 500f, (float)random.Next(-10, 0) / 50f, (float)random.Next(10) / 50f));
		}
	}

	private static void OnNewSeason()
	{
		setGraphicsForSeason();
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			location.seasonUpdate();
			return true;
		});
	}

	public static void prepareSpouseForWedding(Farmer farmer)
	{
		NPC nPC = RequireCharacter(farmer.spouse);
		nPC.ClearSchedule();
		nPC.DefaultMap = farmer.homeLocation.Value;
		nPC.DefaultPosition = Utility.PointToVector2(RequireLocation<FarmHouse>(farmer.homeLocation.Value).getSpouseBedSpot(farmer.spouse)) * 64f;
		nPC.DefaultFacingDirection = 2;
	}

	public static bool AddCharacterIfNecessary(string characterId, bool bypassConditions = false)
	{
		if (!NPC.TryGetData(characterId, out var data))
		{
			return false;
		}
		bool result = false;
		if (getCharacterFromName(characterId) == null)
		{
			if (!bypassConditions && !GameStateQuery.CheckConditions(data.UnlockConditions))
			{
				return false;
			}
			NPC.ReadNpcHomeData(data, null, out var locationName, out var tile, out var direction);
			bool canBeRomanced = data.CanBeRomanced;
			Point size = data.Size;
			GameLocation locationFromNameInLocationsList = getLocationFromNameInLocationsList(locationName);
			if (locationFromNameInLocationsList == null)
			{
				return false;
			}
			string textureNameForCharacter = NPC.getTextureNameForCharacter(characterId);
			NPC nPC;
			try
			{
				nPC = new NPC(new AnimatedSprite("Characters\\" + textureNameForCharacter, 0, size.X, size.Y), new Vector2(tile.X * 64, tile.Y * 64), locationName, direction, characterId, canBeRomanced, content.Load<Texture2D>("Portraits\\" + textureNameForCharacter));
			}
			catch (Exception exception)
			{
				log.Error("Failed to spawn NPC '" + characterId + "'.", exception);
				return false;
			}
			nPC.Breather = data.Breather;
			locationFromNameInLocationsList.addCharacter(nPC);
			result = true;
		}
		if (data.SocialTab == SocialTabBehavior.AlwaysShown && !player.friendshipData.ContainsKey(characterId))
		{
			player.friendshipData.Add(characterId, new Friendship());
		}
		return result;
	}

	public static GameLocation CreateGameLocation(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		LocationData value;
		CreateLocationData createData = (locationData.TryGetValue(id, out value) ? value.CreateOnLoad : null);
		return CreateGameLocation(id, createData);
	}

	public static GameLocation CreateGameLocation(string id, CreateLocationData createData)
	{
		if (createData == null)
		{
			return null;
		}
		GameLocation gameLocation = ((createData.Type == null) ? new GameLocation(createData.MapPath, id) : ((GameLocation)Activator.CreateInstance(Type.GetType(createData.Type) ?? throw new Exception("Invalid type for location " + id + ": " + createData.Type), createData.MapPath, id)));
		gameLocation.isAlwaysActive.Value = createData.AlwaysActive;
		return gameLocation;
	}

	public static void AddLocations()
	{
		bool flag = false;
		foreach (KeyValuePair<string, LocationData> locationDatum in locationData)
		{
			if (locationDatum.Value.CreateOnLoad == null)
			{
				continue;
			}
			GameLocation gameLocation;
			try
			{
				gameLocation = CreateGameLocation(locationDatum.Key, locationDatum.Value.CreateOnLoad);
			}
			catch (Exception exception)
			{
				log.Error("Couldn't create the '" + locationDatum.Key + "' location. Is its data in Data/Locations invalid?", exception);
				continue;
			}
			if (gameLocation == null)
			{
				log.Error("Couldn't create the '" + locationDatum.Key + "' location. Is its data in Data/Locations invalid?");
				continue;
			}
			if (!flag)
			{
				try
				{
					gameLocation.map.LoadTileSheets(mapDisplayDevice);
					currentLocation = gameLocation;
					flag = true;
				}
				catch (Exception exception2)
				{
					log.Error("Couldn't load tilesheets for the '" + locationDatum.Key + "' location.", exception2);
				}
			}
			locations.Add(gameLocation);
		}
		for (int i = 1; i < netWorldState.Value.HighestPlayerLimit; i++)
		{
			GameLocation gameLocation2 = CreateGameLocation("Cellar");
			gameLocation2.name.Value += i + 1;
			locations.Add(gameLocation2);
		}
	}

	public static void AddNPCs()
	{
		foreach (KeyValuePair<string, CharacterData> characterDatum in characterData)
		{
			if (characterDatum.Value.SpawnIfMissing)
			{
				AddCharacterIfNecessary(characterDatum.Key);
			}
		}
		GameLocation locationFromNameInLocationsList = getLocationFromNameInLocationsList("QiNutRoom");
		if (locationFromNameInLocationsList.getCharacterFromName("Mister Qi") == null)
		{
			AnimatedSprite sprite = new AnimatedSprite("Characters\\MrQi", 0, 16, 32);
			locationFromNameInLocationsList.addCharacter(new NPC(sprite, new Vector2(448f, 256f), "QiNutRoom", 0, "Mister Qi", datable: false, content.Load<Texture2D>("Portraits\\MrQi")));
		}
	}

	public static void AddModNPCs()
	{
	}

	public static void fixProblems()
	{
		if (!IsMasterGame)
		{
			return;
		}
		foreach (Farmer allFarmer in getAllFarmers())
		{
			allFarmer.LearnDefaultRecipes();
			allFarmer.AddMissedMailAndRecipes();
			LevelUpMenu.RevalidateHealth(allFarmer);
			LevelUpMenu.AddMissedProfessionChoices(allFarmer);
		}
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			location.characters.RemoveWhere(delegate(NPC npc)
			{
				if (npc == null)
				{
					log.Warn("Removed broken NPC in " + location.NameOrUniqueName + ": null instance.");
					return true;
				}
				if (npc.IsVillager && npc.GetData() == null)
				{
					try
					{
						if (npc.Sprite.Texture == null)
						{
							log.Warn($"Removed broken NPC '{npc.Name}' in {location.NameOrUniqueName}: villager with no data or sprites.");
							return true;
						}
					}
					catch
					{
						log.Warn($"Removed broken NPC '{npc.Name}' in {location.NameOrUniqueName}: villager with no data or sprites.");
						return true;
					}
				}
				return false;
			});
			return true;
		});
		AddNPCs();
		List<NPC> divorced = null;
		Utility.ForEachVillager(delegate(NPC n)
		{
			if (!n.datable.Value || n.getSpouse() != null)
			{
				return true;
			}
			if (n.DefaultMap == null || !n.DefaultMap.ContainsIgnoreCase("cabin") || n.DefaultMap != "FarmHouse")
			{
				return true;
			}
			CharacterData data = n.GetData();
			if (data == null)
			{
				return true;
			}
			NPC.ReadNpcHomeData(data, n.currentLocation, out var locationName, out var _, out var _);
			if (n.DefaultMap != locationName)
			{
				if (divorced == null)
				{
					divorced = new List<NPC>();
				}
				divorced.Add(n);
			}
			return true;
		});
		if (divorced != null)
		{
			foreach (NPC item3 in divorced)
			{
				log.Warn("Fixing " + item3.Name + " who was improperly divorced and left stranded");
				item3.PerformDivorce();
			}
		}
		foreach (Farmer allFarmer2 in getAllFarmers())
		{
			if (allFarmer2.hasQuest("130"))
			{
				HashSet<string> requiredQuestItems = new HashSet<string> { "(O)864", "(O)865", "(O)866", "(O)867", "(O)868", "(O)869", "(O)870" };
				bool found = false;
				foreach (string item4 in requiredQuestItems)
				{
					if (allFarmer2.Items.ContainsId(item4))
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					Utility.ForEachItem(delegate(Item item2)
					{
						found = requiredQuestItems.Contains(item2.QualifiedItemId);
						return !found;
					});
				}
				if (!found)
				{
					Object obj = ItemRegistry.Create<Object>("(O)864");
					obj.specialItem = true;
					obj.questItem.Value = true;
					if (!allFarmer2.addItemToInventoryBool(obj))
					{
						allFarmer2.team.returnedDonations.Add(obj);
						allFarmer2.team.newLostAndFoundItems.Value = true;
					}
				}
			}
			else if (!allFarmer2.craftingRecipes.ContainsKey("Fairy Dust") && allFarmer2.mailReceived.Contains("birdieQuestBegun"))
			{
				allFarmer2.mailReceived.Remove("birdieQuestBegun");
			}
		}
		int num = getAllFarmers().Count();
		Dictionary<Type, int> missingTools = new Dictionary<Type, int>
		{
			[typeof(Axe)] = num,
			[typeof(Pickaxe)] = num,
			[typeof(Hoe)] = num,
			[typeof(WateringCan)] = num,
			[typeof(Wand)] = 0
		};
		foreach (Farmer allFarmer3 in getAllFarmers())
		{
			if (allFarmer3.hasOrWillReceiveMail("ReturnScepter"))
			{
				missingTools[typeof(Wand)]++;
			}
		}
		int missingScythes = num;
		foreach (Farmer allFarmer4 in getAllFarmers())
		{
			if (allFarmer4.toolBeingUpgraded.Value != null)
			{
				allFarmer4.toolBeingUpgraded.Value.FixStackSize();
				Type type = allFarmer4.toolBeingUpgraded.Value.GetType();
				if (missingTools.TryGetValue(type, out var value))
				{
					missingTools[type] = value - 1;
				}
			}
			for (int num2 = 0; num2 < allFarmer4.Items.Count; num2++)
			{
				if (allFarmer4.Items[num2] != null)
				{
					checkIsMissingTool(missingTools, ref missingScythes, allFarmer4.Items[num2]);
				}
			}
		}
		bool flag = true;
		foreach (int value2 in missingTools.Values)
		{
			if (value2 > 0)
			{
				flag = false;
				break;
			}
		}
		if (missingScythes > 0)
		{
			flag = false;
		}
		if (flag)
		{
			return;
		}
		Utility.ForEachLocation(delegate(GameLocation l)
		{
			List<Debris> list2 = new List<Debris>();
			foreach (Debris debri in l.debris)
			{
				Item item2 = debri.item;
				if (item2 != null)
				{
					foreach (Type key in missingTools.Keys)
					{
						if (item2.GetType() == key)
						{
							list2.Add(debri);
						}
					}
					if (item2.QualifiedItemId == "(W)47")
					{
						list2.Add(debri);
					}
				}
			}
			foreach (Debris item5 in list2)
			{
				l.debris.Remove(item5);
			}
			return true;
		});
		Utility.iterateChestsAndStorage(delegate(Item item2)
		{
			checkIsMissingTool(missingTools, ref missingScythes, item2);
		});
		List<string> list = new List<string>();
		foreach (KeyValuePair<Type, int> item6 in missingTools)
		{
			if (item6.Value > 0)
			{
				for (int num3 = 0; num3 < item6.Value; num3++)
				{
					list.Add(item6.Key.ToString());
				}
			}
		}
		for (int num4 = 0; num4 < missingScythes; num4++)
		{
			list.Add("Scythe");
		}
		if (list.Count > 0)
		{
			addMailForTomorrow("foundLostTools");
		}
		for (int num5 = 0; num5 < list.Count; num5++)
		{
			Item item = null;
			switch (list[num5])
			{
			case "StardewValley.Tools.Axe":
				item = ItemRegistry.Create("(T)Axe");
				break;
			case "StardewValley.Tools.Hoe":
				item = ItemRegistry.Create("(T)Hoe");
				break;
			case "StardewValley.Tools.WateringCan":
				item = ItemRegistry.Create("(T)WateringCan");
				break;
			case "Scythe":
				item = ItemRegistry.Create("(W)47");
				break;
			case "StardewValley.Tools.Pickaxe":
				item = ItemRegistry.Create("(T)Pickaxe");
				break;
			case "StardewValley.Tools.Wand":
				item = ItemRegistry.Create("(T)ReturnScepter");
				break;
			}
			if (item != null)
			{
				if (newDaySync.hasInstance())
				{
					player.team.newLostAndFoundItems.Value = true;
				}
				player.team.returnedDonations.Add(item);
			}
		}
	}

	private static void checkIsMissingTool(Dictionary<Type, int> missingTools, ref int missingScythes, Item item)
	{
		foreach (Type key in missingTools.Keys)
		{
			if (item.GetType() == key)
			{
				missingTools[key]--;
			}
		}
		if (item.QualifiedItemId == "(W)47")
		{
			missingScythes--;
		}
	}

	public static void newDayAfterFade(Action after)
	{
		StardewValley.Pokemon.PokemonBreeding.OnDayStarted();

		if (player.currentLocation != null)
		{
			if (player.rightRing.Value != null)
			{
				player.rightRing.Value.onLeaveLocation(player, player.currentLocation);
			}
			if (player.leftRing.Value != null)
			{
				player.leftRing.Value.onLeaveLocation(player, player.currentLocation);
			}
		}
		if (LocalMultiplayer.IsLocalMultiplayer())
		{
			hooks.OnGame1_NewDayAfterFade(delegate
			{
				game1.isLocalMultiplayerNewDayActive = true;
				_afterNewDayAction = after;
				GameRunner.instance.activeNewDayProcesses.Add(new KeyValuePair<Game1, IEnumerator<int>>(game1, _newDayAfterFade()));
			});
			return;
		}
		hooks.OnGame1_NewDayAfterFade(delegate
		{
			_afterNewDayAction = after;
			if (_newDayTask != null)
			{
				log.Warn("Warning: There is already a _newDayTask; unusual code path.\n" + StackTraceHelper.StackTrace);
			}
			else
			{
				_newDayTask = new Task(delegate
				{
					Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
					IEnumerator<int> enumerator = _newDayAfterFade();
					while (enumerator.MoveNext())
					{
					}
				});
			}
		});
	}

	public static bool CanAcceptDailyQuest()
	{
		if (questOfTheDay == null)
		{
			return false;
		}
		if (player.acceptedDailyQuest.Value)
		{
			return false;
		}
		if (string.IsNullOrEmpty(questOfTheDay.questDescription))
		{
			return false;
		}
		return true;
	}

	private static IEnumerator<int> _newDayAfterFade()
	{
		TriggerActionManager.Raise("DayEnding");
		newDaySync.start();
		while (!newDaySync.hasStarted())
		{
			yield return 0;
		}
		int timeWentToSleep = timeOfDay;
		newDaySync.barrier("start");
		while (!newDaySync.isBarrierReady("start"))
		{
			yield return 0;
		}
		int overnightMinutesElapsed = Utility.CalculateMinutesUntilMorning(timeWentToSleep);
		stats.AverageBedtime = (uint)timeWentToSleep;
		if (IsMasterGame)
		{
			dayOfMonth++;
			stats.DaysPlayed++;
			if (dayOfMonth > 28)
			{
				dayOfMonth = 1;
				switch (season)
				{
				case Season.Spring:
					season = Season.Summer;
					break;
				case Season.Summer:
					season = Season.Fall;
					break;
				case Season.Fall:
					season = Season.Winter;
					break;
				case Season.Winter:
					season = Season.Spring;
					year++;
					MineShaft.yearUpdate();
					break;
				}
			}
			timeOfDay = 600;
			netWorldState.Value.UpdateFromGame1();
		}
		newDaySync.barrier("date");
		while (!newDaySync.isBarrierReady("date"))
		{
			yield return 0;
		}
		player.dayOfMonthForSaveGame = dayOfMonth;
		player.seasonForSaveGame = seasonIndex;
		player.yearForSaveGame = year;
		flushLocationLookup();
		Event.OnNewDay();
		try
		{
			fixProblems();
		}
		catch (Exception)
		{
		}
		foreach (Farmer allFarmer in getAllFarmers())
		{
			allFarmer.FarmerSprite.PauseForSingleAnimation = false;
		}
		whereIsTodaysFest = null;
		if (wind != null)
		{
			wind.Stop(AudioStopOptions.Immediate);
			wind = null;
		}
		player.chestConsumedMineLevels.RemoveWhere((KeyValuePair<int, bool> pair) => pair.Key > 120);
		player.currentEyes = 0;
		int num;
		if (IsMasterGame)
		{
			player.team.announcedSleepingFarmers.Clear();
			num = Utility.CreateRandomSeed(uniqueIDForThisGame / 100, stats.DaysPlayed * 10 + 1, stats.StepsTaken);
			newDaySync.sendVar<NetInt, int>("seed", num);
		}
		else
		{
			while (!newDaySync.isVarReady("seed"))
			{
				yield return 0;
			}
			num = newDaySync.waitForVar<NetInt, int>("seed");
		}
		random = Utility.CreateRandom(num);
		for (int num2 = 0; num2 < dayOfMonth; num2++)
		{
			random.Next();
		}
		player.team.endOfNightStatus.UpdateState("sleep");
		newDaySync.barrier("sleep");
		while (!newDaySync.isBarrierReady("sleep"))
		{
			yield return 0;
		}
		gameTimeInterval = 0;
		game1.wasAskedLeoMemory = false;
		player.team.Update();
		player.team.NewDay();
		player.passedOut = false;
		player.CanMove = true;
		player.FarmerSprite.PauseForSingleAnimation = false;
		player.FarmerSprite.StopAnimation();
		player.completelyStopAnimatingOrDoingAction();
		changeMusicTrack("silence");
		if (IsMasterGame)
		{
			UpdateDishOfTheDay();
		}
		newDaySync.barrier("dishOfTheDay");
		while (!newDaySync.isBarrierReady("dishOfTheDay"))
		{
			yield return 0;
		}
		npcDialogues = null;
		Utility.ForEachCharacter(delegate(NPC n)
		{
			n.updatedDialogueYet = false;
			return true;
		});
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			location.currentEvent = null;
			if (IsMasterGame)
			{
				location.passTimeForObjects(overnightMinutesElapsed);
			}
			return true;
		});
		outdoorLight = Color.White;
		ambientLight = Color.White;
		if (isLightning && IsMasterGame)
		{
			Utility.overnightLightning(timeWentToSleep);
		}
		if (MasterPlayer.hasOrWillReceiveMail("ccBulletinThankYou") && !player.hasOrWillReceiveMail("ccBulletinThankYou"))
		{
			addMailForTomorrow("ccBulletinThankYou");
		}
		ReceiveMailForTomorrow();
		if (Utility.TryGetRandom(player.friendshipData, out var key, out var value) && random.NextBool((double)(value.Points / 250) * 0.1) && player.spouse != key && DataLoader.Mail(content).ContainsKey(key))
		{
			mailbox.Add(key);
		}
		MineShaft.clearActiveMines();
		VolcanoDungeon.ClearAllLevels();
		netWorldState.Value.CheckedGarbage.Clear();
		for (int num3 = player.enchantments.Count - 1; num3 >= 0; num3--)
		{
			player.enchantments[num3].OnUnequip(player);
		}
		player.dayupdate(timeWentToSleep);
		if (IsMasterGame)
		{
			player.team.sharedDailyLuck.Value = Math.Min(0.10000000149011612, (double)random.Next(-100, 101) / 1000.0);
		}
		player.showToolUpgradeAvailability();
		if (IsMasterGame)
		{
			queueWeddingsForToday();
			newDaySync.sendVar<NetRef<NetLongList>, NetLongList>("weddingsToday", new NetLongList(weddingsToday));
		}
		else
		{
			while (!newDaySync.isVarReady("weddingsToday"))
			{
				yield return 0;
			}
			weddingsToday = new List<long>(newDaySync.waitForVar<NetRef<NetLongList>, NetLongList>("weddingsToday"));
		}
		weddingToday = false;
		foreach (long item in weddingsToday)
		{
			Farmer farmer = GetPlayer(item);
			if (farmer != null && !farmer.hasCurrentOrPendingRoommate())
			{
				weddingToday = true;
				break;
			}
		}
		if (player.spouse != null && player.isEngaged() && weddingsToday.Contains(player.UniqueMultiplayerID))
		{
			Friendship friendship = player.friendshipData[player.spouse];
			friendship.Status = FriendshipStatus.Married;
			friendship.WeddingDate = new WorldDate(Date);
			prepareSpouseForWedding(player);
			if (!player.getSpouse().isRoommate())
			{
				player.autoGenerateActiveDialogueEvent("married_" + player.spouse);
				if (!player.autoGenerateActiveDialogueEvent("married"))
				{
					player.autoGenerateActiveDialogueEvent("married_twice");
				}
			}
			else
			{
				player.autoGenerateActiveDialogueEvent("roommates_" + player.spouse);
			}
		}
		NetLongDictionary<NetList<Item, NetRef<Item>>, NetRef<NetList<Item, NetRef<Item>>>> additional_shipped_items = new NetLongDictionary<NetList<Item, NetRef<Item>>, NetRef<NetList<Item, NetRef<Item>>>>();
		if (IsMasterGame)
		{
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				foreach (Object value4 in location.objects.Values)
				{
					if (value4 is Chest { SpecialChestType: Chest.SpecialChestTypes.MiniShippingBin } chest)
					{
						chest.clearNulls();
						if (player.team.useSeparateWallets.Value)
						{
							foreach (long key3 in chest.separateWalletItems.Keys)
							{
								if (!additional_shipped_items.ContainsKey(key3))
								{
									additional_shipped_items[key3] = new NetList<Item, NetRef<Item>>();
								}
								List<Item> list3 = new List<Item>(chest.separateWalletItems[key3]);
								chest.separateWalletItems[key3].Clear();
								foreach (Item item2 in list3)
								{
									item2.onDetachedFromParent();
									additional_shipped_items[key3].Add(item2);
								}
							}
						}
						else
						{
							IInventory shippingBin4 = getFarm().getShippingBin(player);
							shippingBin4.RemoveEmptySlots();
							foreach (Item item3 in chest.Items)
							{
								item3.onDetachedFromParent();
								shippingBin4.Add(item3);
							}
						}
						chest.Items.Clear();
						chest.separateWalletItems.Clear();
					}
				}
				return true;
			});
		}
		if (IsMasterGame)
		{
			newDaySync.sendVar<NetRef<NetLongDictionary<NetList<Item, NetRef<Item>>, NetRef<NetList<Item, NetRef<Item>>>>>, NetLongDictionary<NetList<Item, NetRef<Item>>, NetRef<NetList<Item, NetRef<Item>>>>>("additional_shipped_items", additional_shipped_items);
		}
		else
		{
			while (!newDaySync.isVarReady("additional_shipped_items"))
			{
				yield return 0;
			}
			additional_shipped_items = newDaySync.waitForVar<NetRef<NetLongDictionary<NetList<Item, NetRef<Item>>, NetRef<NetList<Item, NetRef<Item>>>>>, NetLongDictionary<NetList<Item, NetRef<Item>>, NetRef<NetList<Item, NetRef<Item>>>>>("additional_shipped_items");
		}
		if (player.team.useSeparateWallets.Value)
		{
			IInventory shippingBin = getFarm().getShippingBin(player);
			if (additional_shipped_items.TryGetValue(player.UniqueMultiplayerID, out var value2))
			{
				foreach (Item item4 in value2)
				{
					shippingBin.Add(item4);
				}
			}
		}
		newDaySync.barrier("handleMiniShippingBins");
		while (!newDaySync.isBarrierReady("handleMiniShippingBins"))
		{
			yield return 0;
		}
		IInventory shippingBin2 = getFarm().getShippingBin(player);
		shippingBin2.RemoveEmptySlots();
		foreach (Item item5 in shippingBin2)
		{
			player.displayedShippedItems.Add(item5);
		}
		if (player.useSeparateWallets || player.IsMainPlayer)
		{
			int num4 = 0;
			foreach (Item item6 in shippingBin2)
			{
				int num5 = 0;
				if (item6 is Object obj)
				{
					num5 = obj.sellToStorePrice(-1L) * obj.Stack;
					num4 += num5;
				}
				if (player.team.specialOrders == null)
				{
					continue;
				}
				foreach (SpecialOrder specialOrder in player.team.specialOrders)
				{
					specialOrder.onItemShipped?.Invoke(player, item6, num5);
				}
			}
			player.Money += num4;
		}
		if (IsMasterGame)
		{
			if (IsWinter && dayOfMonth == 18)
			{
				GameLocation gameLocation = RequireLocation("Submarine");
				if (gameLocation.objects.Length >= 0)
				{
					Utility.transferPlacedObjectsFromOneLocationToAnother(gameLocation, null, new Vector2(20f, 20f), getLocationFromName("Beach"));
				}
				gameLocation = RequireLocation("MermaidHouse");
				if (gameLocation.objects.Length >= 0)
				{
					Utility.transferPlacedObjectsFromOneLocationToAnother(gameLocation, null, new Vector2(21f, 20f), getLocationFromName("Beach"));
				}
			}
			if (player.hasOrWillReceiveMail("pamHouseUpgrade") && !player.hasOrWillReceiveMail("transferredObjectsPamHouse"))
			{
				addMailForTomorrow("transferredObjectsPamHouse", noLetter: true);
				GameLocation gameLocation2 = RequireLocation("Trailer");
				GameLocation locationFromName = getLocationFromName("Trailer_Big");
				if (gameLocation2.objects.Length >= 0)
				{
					Utility.transferPlacedObjectsFromOneLocationToAnother(gameLocation2, locationFromName, new Vector2(14f, 23f));
				}
			}
			if (Utility.HasAnyPlayerSeenEvent("191393") && !player.hasOrWillReceiveMail("transferredObjectsJojaMart"))
			{
				addMailForTomorrow("transferredObjectsJojaMart", noLetter: true);
				GameLocation gameLocation3 = RequireLocation("JojaMart");
				if (gameLocation3.objects.Length >= 0)
				{
					Utility.transferPlacedObjectsFromOneLocationToAnother(gameLocation3, null, new Vector2(89f, 51f), getLocationFromName("Town"));
				}
			}
		}
		if (player.useSeparateWallets && player.IsMainPlayer)
		{
			foreach (Farmer offlineFarmhand in getOfflineFarmhands())
			{
				if (offlineFarmhand.isUnclaimedFarmhand)
				{
					continue;
				}
				int num6 = 0;
				IInventory shippingBin3 = getFarm().getShippingBin(offlineFarmhand);
				shippingBin3.RemoveEmptySlots();
				foreach (Item item7 in shippingBin3)
				{
					int num7 = 0;
					if (item7 is Object obj2)
					{
						num7 = obj2.sellToStorePrice(offlineFarmhand.UniqueMultiplayerID) * obj2.Stack;
						num6 += num7;
					}
					if (player.team.specialOrders == null)
					{
						continue;
					}
					foreach (SpecialOrder specialOrder2 in player.team.specialOrders)
					{
						specialOrder2.onItemShipped?.Invoke(player, item7, num7);
					}
				}
				player.team.AddIndividualMoney(offlineFarmhand, num6);
				shippingBin3.Clear();
			}
		}
		List<NPC> divorceNPCs = new List<NPC>();
		if (IsMasterGame)
		{
			foreach (Farmer allFarmer2 in getAllFarmers())
			{
				if (allFarmer2.isActive() && allFarmer2.divorceTonight.Value && allFarmer2.getSpouse() != null)
				{
					divorceNPCs.Add(allFarmer2.getSpouse());
				}
			}
		}
		newDaySync.barrier("player.dayupdate");
		while (!newDaySync.isBarrierReady("player.dayupdate"))
		{
			yield return 0;
		}
		if (player.divorceTonight.Value)
		{
			player.doDivorce();
		}
		newDaySync.barrier("player.divorce");
		while (!newDaySync.isBarrierReady("player.divorce"))
		{
			yield return 0;
		}
		if (IsMasterGame)
		{
			foreach (NPC item8 in divorceNPCs)
			{
				if (item8.getSpouse() == null)
				{
					item8.PerformDivorce();
				}
			}
		}
		newDaySync.barrier("player.finishDivorce");
		while (!newDaySync.isBarrierReady("player.finishDivorce"))
		{
			yield return 0;
		}
		if (IsMasterGame)
		{
			Utility.ForEachBuilding(delegate(Building building)
			{
				if (building?.GetIndoors() is Cabin cabin)
				{
					cabin.updateFarmLayout();
				}
				return true;
			});
		}
		newDaySync.barrier("updateFarmLayout");
		while (!newDaySync.isBarrierReady("updateFarmLayout"))
		{
			yield return 0;
		}
		if (IsMasterGame && player.changeWalletTypeTonight.Value)
		{
			if (player.useSeparateWallets)
			{
				ManorHouse.MergeWallets();
			}
			else
			{
				ManorHouse.SeparateWallets();
			}
		}
		newDaySync.barrier("player.wallets");
		while (!newDaySync.isBarrierReady("player.wallets"))
		{
			yield return 0;
		}
		getFarm().lastItemShipped = null;
		getFarm().getShippingBin(player).Clear();
		newDaySync.barrier("clearShipping");
		while (!newDaySync.isBarrierReady("clearShipping"))
		{
			yield return 0;
		}
		if (IsClient)
		{
			multiplayer.sendFarmhand();
			newDaySync.processMessages();
		}
		newDaySync.barrier("sendFarmhands");
		while (!newDaySync.isBarrierReady("sendFarmhands"))
		{
			yield return 0;
		}
		if (IsMasterGame)
		{
			multiplayer.saveFarmhands();
		}
		newDaySync.barrier("saveFarmhands");
		while (!newDaySync.isBarrierReady("saveFarmhands"))
		{
			yield return 0;
		}
		if (IsMasterGame)
		{
			UpdatePassiveFestivalStates();
			if (Utility.IsPassiveFestivalDay("NightMarket") && IsMasterGame && netWorldState.Value.VisitsUntilY1Guarantee >= 0)
			{
				netWorldState.Value.VisitsUntilY1Guarantee--;
			}
		}
		if (dayOfMonth == 1)
		{
			OnNewSeason();
		}
		if (IsMasterGame && (dayOfMonth == 1 || dayOfMonth == 8 || dayOfMonth == 15 || dayOfMonth == 22))
		{
			SpecialOrder.UpdateAvailableSpecialOrders("", forceRefresh: true);
			SpecialOrder.UpdateAvailableSpecialOrders("Qi", forceRefresh: true);
		}
		if (IsMasterGame)
		{
			netWorldState.Value.UpdateFromGame1();
		}
		newDaySync.barrier("specialOrders");
		while (!newDaySync.isBarrierReady("specialOrders"))
		{
			yield return 0;
		}
		if (IsMasterGame)
		{
			player.team.specialOrders.RemoveWhere(delegate(SpecialOrder order)
			{
				if (order.questState.Value != SpecialOrderStatus.Complete && order.GetDaysLeft() <= 0)
				{
					order.OnFail();
					return true;
				}
				return false;
			});
		}
		newDaySync.barrier("processOrders");
		while (!newDaySync.isBarrierReady("processOrders"))
		{
			yield return 0;
		}
		if (IsMasterGame)
		{
			foreach (string item9 in player.team.specialRulesRemovedToday)
			{
				SpecialOrder.RemoveSpecialRuleAtEndOfDay(item9);
			}
		}
		player.team.specialRulesRemovedToday.Clear();
		if (DataLoader.Mail(content).ContainsKey(currentSeason + "_" + dayOfMonth + "_" + year))
		{
			mailbox.Add(currentSeason + "_" + dayOfMonth + "_" + year);
		}
		else if (DataLoader.Mail(content).ContainsKey(currentSeason + "_" + dayOfMonth))
		{
			mailbox.Add(currentSeason + "_" + dayOfMonth);
		}
		if (MasterPlayer.mailReceived.Contains("ccVault") && IsSpring && dayOfMonth == 14)
		{
			mailbox.Add("DesertFestival");
		}
		if (IsMasterGame)
		{
			if (player.team.toggleMineShrineOvernight.Value)
			{
				player.team.toggleMineShrineOvernight.Value = false;
				player.team.mineShrineActivated.Value = !player.team.mineShrineActivated.Value;
				if (player.team.mineShrineActivated.Value)
				{
					netWorldState.Value.MinesDifficulty++;
				}
				else
				{
					netWorldState.Value.MinesDifficulty--;
				}
			}
			if (player.team.toggleSkullShrineOvernight.Value)
			{
				player.team.toggleSkullShrineOvernight.Value = false;
				player.team.skullShrineActivated.Value = !player.team.skullShrineActivated.Value;
				if (player.team.skullShrineActivated.Value)
				{
					netWorldState.Value.SkullCavesDifficulty++;
				}
				else
				{
					netWorldState.Value.SkullCavesDifficulty--;
				}
			}
		}
		if (IsMasterGame)
		{
			if (!player.team.SpecialOrderRuleActive("MINE_HARD") && netWorldState.Value.MinesDifficulty > 1)
			{
				netWorldState.Value.MinesDifficulty = 1;
			}
			if (!player.team.SpecialOrderRuleActive("SC_HARD") && netWorldState.Value.SkullCavesDifficulty > 1)
			{
				netWorldState.Value.SkullCavesDifficulty = 1;
			}
		}
		if (IsMasterGame)
		{
			RefreshQuestOfTheDay();
		}
		newDaySync.barrier("questOfTheDay");
		while (!newDaySync.isBarrierReady("questOfTheDay"))
		{
			yield return 0;
		}
		bool yesterdayWasGreenRain = wasGreenRain;
		wasGreenRain = false;
		UpdateWeatherForNewDay();
		newDaySync.barrier("updateWeather");
		while (!newDaySync.isBarrierReady("updateWeather"))
		{
			yield return 0;
		}
		ApplyWeatherForNewDay();
		if (isGreenRain)
		{
			morningQueue.Enqueue(delegate
			{
				showGlobalMessage(content.LoadString("Strings\\1_6_Strings:greenrainmessage"));
			});
			if (year == 1 && !player.hasOrWillReceiveMail("GreenRainGus"))
			{
				mailbox.Add("GreenRainGus");
			}
			if (IsMasterGame)
			{
				Utility.ForEachLocation(delegate(GameLocation location)
				{
					location.performGreenRainUpdate();
					return true;
				});
			}
		}
		else if (yesterdayWasGreenRain)
		{
			if (IsMasterGame)
			{
				Utility.ForEachLocation(delegate(GameLocation location)
				{
					location.performDayAfterGreenRainUpdate();
					return true;
				});
			}
			if (year == 1)
			{
				player.activeDialogueEvents.TryAdd("GreenRainFinished", 1);
			}
		}
		if (Utility.getDaysOfBooksellerThisSeason().Contains(dayOfMonth))
		{
			addMorningFluffFunction(delegate
			{
				showGlobalMessage(content.LoadString("Strings\\1_6_Strings:BooksellerInTown"));
			});
		}
		WeatherDebris.globalWind = 0f;
		windGust = 0f;
		AddNPCs();
		Utility.ForEachVillager(delegate(NPC n)
		{
			player.mailReceived.Remove(n.Name);
			player.mailReceived.Remove(n.Name + "Cooking");
			n.drawOffset = Vector2.Zero;
			if (!IsMasterGame)
			{
				n.ChooseAppearance();
			}
			return true;
		});
		FarmAnimal.reservedGrass.Clear();
		if (IsMasterGame)
		{
			NPC.hasSomeoneRepairedTheFences = false;
			NPC.hasSomeoneFedTheAnimals = false;
			NPC.hasSomeoneFedThePet = false;
			NPC.hasSomeoneWateredCrops = false;
			foreach (GameLocation location in locations)
			{
				location.ResetCharacterDialogues();
				location.DayUpdate(dayOfMonth);
			}
			netWorldState.Value.UpdateUnderConstruction();
			UpdateHorseOwnership();
			foreach (NPC allCharacter in Utility.getAllCharacters())
			{
				if (allCharacter.IsVillager)
				{
					allCharacter.islandScheduleName.Value = null;
					allCharacter.currentScheduleDelay = 0f;
				}
				allCharacter.dayUpdate(dayOfMonth);
			}
			IslandSouth.SetupIslandSchedules();
			HashSet<NPC> purchased_item_npcs = new HashSet<NPC>();
			UpdateShopPlayerItemInventory("SeedShop", purchased_item_npcs);
			UpdateShopPlayerItemInventory("FishShop", purchased_item_npcs);
		}
		if (IsMasterGame && netWorldState.Value.GetWeatherForLocation("Island").IsRaining)
		{
			Vector2 tile_position = new Vector2(0f, 0f);
			IslandLocation islandLocation = null;
			List<int> list = new List<int>();
			for (int num8 = 0; num8 < 4; num8++)
			{
				list.Add(num8);
			}
			Utility.Shuffle(Utility.CreateRandom(uniqueIDForThisGame), list);
			switch (list[currentGemBirdIndex])
			{
			case 0:
				islandLocation = getLocationFromName("IslandSouth") as IslandLocation;
				tile_position = new Vector2(10f, 30f);
				break;
			case 1:
				islandLocation = getLocationFromName("IslandNorth") as IslandLocation;
				tile_position = new Vector2(56f, 56f);
				break;
			case 2:
				islandLocation = getLocationFromName("Islandwest") as IslandLocation;
				tile_position = new Vector2(53f, 51f);
				break;
			case 3:
				islandLocation = getLocationFromName("IslandEast") as IslandLocation;
				tile_position = new Vector2(21f, 35f);
				break;
			}
			currentGemBirdIndex = (currentGemBirdIndex + 1) % 4;
			if (islandLocation != null)
			{
				islandLocation.locationGemBird.Value = new IslandGemBird(tile_position, IslandGemBird.GetBirdTypeForLocation(islandLocation.Name));
			}
		}
		if (IsMasterGame)
		{
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (location.IsOutdoors && location.IsRainingHere())
				{
					foreach (Building building in location.buildings)
					{
						if (building is PetBowl petBowl)
						{
							petBowl.watered.Value = true;
						}
					}
					foreach (KeyValuePair<Vector2, TerrainFeature> pair in location.terrainFeatures.Pairs)
					{
						if (pair.Value is HoeDirt hoeDirt && hoeDirt.state.Value != 2)
						{
							hoeDirt.state.Value = 1;
						}
					}
				}
				return true;
			});
		}
		WorldDate worldDate = new WorldDate(Date);
		worldDate.TotalDays--;
		foreach (KeyValuePair<string, PassiveFestivalData> item10 in DataLoader.PassiveFestivals(content))
		{
			string key2 = item10.Key;
			PassiveFestivalData value3 = item10.Value;
			if (worldDate.DayOfMonth == value3.EndDay && worldDate.Season == value3.Season && GameStateQuery.CheckConditions(value3.Condition) && value3 != null && value3.CleanupMethod != null)
			{
				if (StaticDelegateBuilder.TryCreateDelegate<FestivalCleanupDelegate>(value3.CleanupMethod, out var createdDelegate, out var error))
				{
					createdDelegate();
					continue;
				}
				log.Warn($"Passive festival '{key2}' has invalid cleanup method '{value3.CleanupMethod}': {error}");
			}
		}
		PerformPassiveFestivalSetup();
		newDaySync.barrier("buildingUpgrades");
		while (!newDaySync.isBarrierReady("buildingUpgrades"))
		{
			yield return 0;
		}
		List<string> list2 = new List<string>(player.team.mailToRemoveOvernight);
		foreach (string item11 in new List<string>(player.team.itemsToRemoveOvernight))
		{
			if (IsMasterGame)
			{
				game1._PerformRemoveNormalItemFromWorldOvernight(item11);
				foreach (Farmer offlineFarmhand2 in getOfflineFarmhands())
				{
					game1._PerformRemoveNormalItemFromFarmerOvernight(offlineFarmhand2, item11);
				}
			}
			game1._PerformRemoveNormalItemFromFarmerOvernight(player, item11);
		}
		foreach (string item12 in list2)
		{
			if (IsMasterGame)
			{
				foreach (Farmer allFarmer3 in getAllFarmers())
				{
					allFarmer3.RemoveMail(item12, allFarmer3 == MasterPlayer);
				}
			}
			else
			{
				player.RemoveMail(item12);
			}
		}
		newDaySync.barrier("removeItemsFromWorld");
		while (!newDaySync.isBarrierReady("removeItemsFromWorld"))
		{
			yield return 0;
		}
		if (IsMasterGame)
		{
			player.team.itemsToRemoveOvernight.Clear();
			player.team.mailToRemoveOvernight.Clear();
		}
		newDay = false;
		if (IsMasterGame)
		{
			netWorldState.Value.UpdateFromGame1();
		}
		if (player.currentLocation != null)
		{
			player.currentLocation.resetForPlayerEntry();
			BedFurniture.ApplyWakeUpPosition(player);
			forceSnapOnNextViewportUpdate = true;
			UpdateViewPort(overrideFreeze: false, player.StandingPixel);
			previousViewportPosition = new Vector2(viewport.X, viewport.Y);
		}
		displayFarmer = true;
		updateWeatherIcon();
		freezeControls = false;
		if (stats.DaysPlayed > 1 || !IsMasterGame)
		{
			farmEvent = null;
			if (IsMasterGame)
			{
				farmEvent = Utility.pickFarmEvent() ?? farmEventOverride;
				farmEventOverride = null;
				newDaySync.sendVar<NetRef<FarmEvent>, FarmEvent>("farmEvent", farmEvent);
			}
			else
			{
				while (!newDaySync.isVarReady("farmEvent"))
				{
					yield return 0;
				}
				farmEvent = newDaySync.waitForVar<NetRef<FarmEvent>, FarmEvent>("farmEvent");
			}
			if (farmEvent == null)
			{
				farmEvent = Utility.pickPersonalFarmEvent();
			}
			if (farmEvent != null && farmEvent.setUp())
			{
				farmEvent = null;
			}
		}
		if (farmEvent == null)
		{
			RemoveDeliveredMailForTomorrow();
		}
		if (player.team.newLostAndFoundItems.Value)
		{
			morningQueue.Enqueue(delegate
			{
				showGlobalMessage(content.LoadString("Strings\\StringsFromCSFiles:NewLostAndFoundItems"));
			});
		}
		newDaySync.barrier("mail");
		while (!newDaySync.isBarrierReady("mail"))
		{
			yield return 0;
		}
		if (IsMasterGame)
		{
			player.team.newLostAndFoundItems.Value = false;
		}
		Utility.ForEachBuilding(delegate(Building building)
		{
			if (building.GetIndoors() is Cabin)
			{
				player.slotCanHost = true;
				return false;
			}
			return true;
		});
		if (Utility.percentGameComplete() + (float)netWorldState.Value.PerfectionWaivers * 0.01f >= 1f)
		{
			player.team.farmPerfect.Value = true;
		}
		newDaySync.barrier("checkcompletion");
		while (!newDaySync.isBarrierReady("checkcompletion"))
		{
			yield return 0;
		}
		UpdateFarmPerfection();
		if (farmEvent == null)
		{
			handlePostFarmEventActions();
			showEndOfNightStuff();
		}
		if (server != null)
		{
			server.updateLobbyData();
		}
	}

	/// <summary>Reset the Saloon's dish of the day.</summary>
	public static void UpdateDishOfTheDay()
	{
		string text;
		do
		{
			text = random.Next(194, 240).ToString();
		}
		while (Utility.IsForbiddenDishOfTheDay(text));
		int amount = random.Next(1, 4 + ((random.NextDouble() < 0.08) ? 10 : 0));
		dishOfTheDay = ItemRegistry.Create<Object>("(O)" + text, amount);
	}

	/// <summary>Apply updates overnight if this save has completed perfection.</summary>
	/// <remarks>See also <see cref="M:StardewValley.Utility.percentGameComplete" /> to check if the save has reached perfection.</remarks>
	public static void UpdateFarmPerfection()
	{
		if (MasterPlayer.mailReceived.Contains("Farm_Eternal") || (!MasterPlayer.hasCompletedCommunityCenter() && !Utility.hasFinishedJojaRoute()) || !player.team.farmPerfect.Value)
		{
			return;
		}
		addMorningFluffFunction(delegate
		{
			changeMusicTrack("none", track_interruptable: true);
			if (IsMasterGame)
			{
				multiplayer.globalChatInfoMessageEvenInSinglePlayer("Eternal1");
			}
			playSound("discoverMineral");
			if (IsMasterGame)
			{
				DelayedAction.functionAfterDelay(delegate
				{
					multiplayer.globalChatInfoMessageEvenInSinglePlayer("Eternal2", MasterPlayer.farmName.Value);
				}, 4000);
			}
			player.mailReceived.Add("Farm_Eternal");
			DelayedAction.functionAfterDelay(delegate
			{
				playSound("thunder_small");
				if (IsMultiplayer)
				{
					if (IsMasterGame)
					{
						multiplayer.globalChatInfoMessage("Eternal3");
					}
				}
				else
				{
					showGlobalMessage(content.LoadString("Strings\\UI:Chat_Eternal3"));
				}
			}, 12000);
		});
	}

	/// <summary>Get whether it's green raining in the given location's context (regardless of whether the player is currently indoors and sheltered from the green rain).</summary>
	/// <param name="location">The location to check, or <c>null</c> to use <see cref="P:StardewValley.Game1.currentLocation" />.</param>
	public static bool IsGreenRainingHere(GameLocation location = null)
	{
		if (location == null)
		{
			location = currentLocation;
		}
		if (location != null && netWorldState != null)
		{
			return location.IsGreenRainingHere();
		}
		return false;
	}

	/// <summary>Get whether it's raining in the given location's context (regardless of whether the player is currently indoors and sheltered from the rain).</summary>
	/// <param name="location">The location to check, or <c>null</c> to use <see cref="P:StardewValley.Game1.currentLocation" />.</param>
	public static bool IsRainingHere(GameLocation location = null)
	{
		if (location == null)
		{
			location = currentLocation;
		}
		if (location != null && netWorldState != null)
		{
			return location.IsRainingHere();
		}
		return false;
	}

	/// <summary>Get whether it's storming in the given location's context (regardless of whether the player is currently indoors and sheltered from the storm).</summary>
	/// <param name="location">The location to check, or <c>null</c> to use <see cref="P:StardewValley.Game1.currentLocation" />.</param>
	public static bool IsLightningHere(GameLocation location = null)
	{
		if (location == null)
		{
			location = currentLocation;
		}
		if (location != null && netWorldState != null)
		{
			return location.IsLightningHere();
		}
		return false;
	}

	/// <summary>Get whether it's snowing in the given location's context (regardless of whether the player is currently indoors and sheltered from the snow).</summary>
	/// <param name="location">The location to check, or <c>null</c> to use <see cref="P:StardewValley.Game1.currentLocation" />.</param>
	public static bool IsSnowingHere(GameLocation location = null)
	{
		if (location == null)
		{
			location = currentLocation;
		}
		if (location != null && netWorldState != null)
		{
			return location.IsSnowingHere();
		}
		return false;
	}

	/// <summary>Get whether it's blowing debris like leaves in the given location's context (regardless of whether the player is currently indoors and sheltered from the wind).</summary>
	/// <param name="location">The location to check, or <c>null</c> to use <see cref="P:StardewValley.Game1.currentLocation" />.</param>
	public static bool IsDebrisWeatherHere(GameLocation location = null)
	{
		if (location == null)
		{
			location = currentLocation;
		}
		if (location != null && netWorldState != null)
		{
			return location.IsDebrisWeatherHere();
		}
		return false;
	}

	public static string getWeatherModificationsForDate(WorldDate date, string default_weather)
	{
		string result = default_weather;
		int num = date.TotalDays - Date.TotalDays;
		if (date.DayOfMonth == 1 || stats.DaysPlayed + num <= 4)
		{
			result = "Sun";
		}
		if (stats.DaysPlayed + num == 3)
		{
			result = "Rain";
		}
		if (Utility.isGreenRainDay(date.DayOfMonth, date.Season))
		{
			result = "GreenRain";
		}
		if (date.Season == Season.Summer && date.DayOfMonth % 13 == 0)
		{
			result = "Storm";
		}
		if (Utility.isFestivalDay(date.DayOfMonth, date.Season))
		{
			result = "Festival";
		}
		foreach (PassiveFestivalData value in DataLoader.PassiveFestivals(content).Values)
		{
			if (date.DayOfMonth < value.StartDay || date.DayOfMonth > value.EndDay || date.Season != value.Season || !GameStateQuery.CheckConditions(value.Condition) || value.MapReplacements == null)
			{
				continue;
			}
			foreach (string key in value.MapReplacements.Keys)
			{
				GameLocation locationFromName = getLocationFromName(key);
				if (locationFromName != null && locationFromName.InValleyContext())
				{
					result = "Sun";
					break;
				}
			}
		}
		return result;
	}

	public static void UpdateWeatherForNewDay()
	{
		weatherForTomorrow = getWeatherModificationsForDate(Date, weatherForTomorrow);
		if (weddingToday)
		{
			weatherForTomorrow = "Wedding";
		}
		if (IsMasterGame)
		{
			netWorldState.Value.GetWeatherForLocation("Default").WeatherForTomorrow = weatherForTomorrow;
		}
		wasRainingYesterday = isRaining || isLightning;
		debrisWeather.Clear();
		if (!IsMasterGame)
		{
			return;
		}
		foreach (KeyValuePair<string, LocationContextData> locationContextDatum in locationContextData)
		{
			netWorldState.Value.GetWeatherForLocation(locationContextDatum.Key).UpdateDailyWeather(locationContextDatum.Key, locationContextDatum.Value, random);
		}
		foreach (KeyValuePair<string, LocationContextData> locationContextDatum2 in locationContextData)
		{
			string copyWeatherFromLocation = locationContextDatum2.Value.CopyWeatherFromLocation;
			if (copyWeatherFromLocation != null)
			{
				try
				{
					LocationWeather weatherForLocation = netWorldState.Value.GetWeatherForLocation(locationContextDatum2.Key);
					LocationWeather weatherForLocation2 = netWorldState.Value.GetWeatherForLocation(copyWeatherFromLocation);
					weatherForLocation.CopyFrom(weatherForLocation2);
				}
				catch
				{
				}
			}
		}
	}

	public static void ApplyWeatherForNewDay()
	{
		LocationWeather weatherForLocation = netWorldState.Value.GetWeatherForLocation("Default");
		weatherForTomorrow = weatherForLocation.WeatherForTomorrow;
		isRaining = weatherForLocation.IsRaining;
		isSnowing = weatherForLocation.IsSnowing;
		isLightning = weatherForLocation.IsLightning;
		isDebrisWeather = weatherForLocation.IsDebrisWeather;
		isGreenRain = weatherForLocation.IsGreenRain;
		if (isDebrisWeather)
		{
			populateDebrisWeatherArray();
		}
		if (!IsMasterGame)
		{
			return;
		}
		foreach (string key in netWorldState.Value.LocationWeather.Keys)
		{
			LocationWeather locationWeather = netWorldState.Value.LocationWeather[key];
			if (dayOfMonth == 1)
			{
				locationWeather.monthlyNonRainyDayCount.Value = 0;
			}
			if (!locationWeather.IsRaining)
			{
				locationWeather.monthlyNonRainyDayCount.Value++;
			}
		}
	}

	public static void UpdateShopPlayerItemInventory(string location_name, HashSet<NPC> purchased_item_npcs)
	{
		if (!(getLocationFromName(location_name) is ShopLocation { itemsFromPlayerToSell: var itemsFromPlayerToSell } shopLocation))
		{
			return;
		}
		for (int num = itemsFromPlayerToSell.Count - 1; num >= 0; num--)
		{
			if (!(itemsFromPlayerToSell[num] is Object obj))
			{
				itemsFromPlayerToSell.RemoveAt(num);
			}
			else
			{
				for (int i = 0; i < obj.Stack; i++)
				{
					bool flag = false;
					if (obj.edibility.Value != -300 && random.NextDouble() < 0.04)
					{
						NPC randomNpc = Utility.GetRandomNpc((string name, CharacterData data) => data.CanCommentOnPurchasedShopItems ?? (data.HomeRegion == "Town"));
						if (randomNpc.Age != 2 && randomNpc.getSpouse() == null)
						{
							if (!purchased_item_npcs.Contains(randomNpc))
							{
								Dialogue purchasedItemDialogueForNPC = shopLocation.getPurchasedItemDialogueForNPC(obj, randomNpc);
								if (purchasedItemDialogueForNPC != null)
								{
									randomNpc.addExtraDialogue(purchasedItemDialogueForNPC);
									purchased_item_npcs.Add(randomNpc);
								}
							}
							itemsFromPlayerToSell[num] = obj.ConsumeStack(1);
							flag = true;
						}
					}
					if (!flag && random.NextDouble() < 0.15)
					{
						itemsFromPlayerToSell[num] = obj.ConsumeStack(1);
					}
					if (itemsFromPlayerToSell[num] == null)
					{
						itemsFromPlayerToSell.RemoveAt(num);
						break;
					}
				}
			}
		}
	}

	private static void handlePostFarmEventActions()
	{
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			foreach (Action postFarmEventOvernightAction in location.postFarmEventOvernightActions)
			{
				postFarmEventOvernightAction();
			}
			location.postFarmEventOvernightActions.Clear();
			return true;
		});
		if (IsMasterGame)
		{
			Mountain mountain = RequireLocation<Mountain>("Mountain");
			mountain.ApplyTreehouseIfNecessary();
			if (mountain.treehouseDoorDirty)
			{
				mountain.treehouseDoorDirty = false;
				WarpPathfindingCache.PopulateCache();
			}
		}
	}

	public static void ReceiveMailForTomorrow(string mail_to_transfer = null)
	{
		foreach (string item in player.mailForTomorrow)
		{
			if (item == null)
			{
				continue;
			}
			string text = item.Replace("%&NL&%", "");
			if (mail_to_transfer == null || !(mail_to_transfer != item) || !(mail_to_transfer != text))
			{
				mailDeliveredFromMailForTomorrow.Add(item);
				if (item.Contains("%&NL&%"))
				{
					player.mailReceived.Add(text);
				}
				else
				{
					mailbox.Add(item);
				}
			}
		}
	}

	public static void RemoveDeliveredMailForTomorrow()
	{
		ReceiveMailForTomorrow("abandonedJojaMartAccessible");
		foreach (string item in mailDeliveredFromMailForTomorrow)
		{
			player.mailForTomorrow.Remove(item);
		}
		mailDeliveredFromMailForTomorrow.Clear();
	}

	public static void queueWeddingsForToday()
	{
		weddingsToday.Clear();
		weddingToday = false;
		if (!canHaveWeddingOnDay(dayOfMonth, season))
		{
			return;
		}
		foreach (Farmer item in from farmer2 in getOnlineFarmers()
			orderby farmer2.UniqueMultiplayerID
			select farmer2)
		{
			if (item.spouse != null && item.isEngaged() && item.friendshipData[item.spouse].CountdownToWedding < 1)
			{
				weddingsToday.Add(item.UniqueMultiplayerID);
			}
			if (!item.team.IsEngaged(item.UniqueMultiplayerID))
			{
				continue;
			}
			long? spouse = item.team.GetSpouse(item.UniqueMultiplayerID);
			if (spouse.HasValue && !weddingsToday.Contains(spouse.Value))
			{
				Farmer farmer = GetPlayer(spouse.Value);
				if (farmer != null && getOnlineFarmers().Contains(farmer) && getOnlineFarmers().Contains(item) && player.team.GetFriendship(item.UniqueMultiplayerID, spouse.Value).CountdownToWedding < 1)
				{
					weddingsToday.Add(item.UniqueMultiplayerID);
				}
			}
		}
	}

	public static bool PollForEndOfNewDaySync()
	{
		if (!IsMultiplayer)
		{
			newDaySync.destroy();
			currentLocation.resetForPlayerEntry();
			return true;
		}
		if (newDaySync.readyForFinish())
		{
			if (IsMasterGame && newDaySync.hasInstance() && !newDaySync.hasFinished())
			{
				newDaySync.finish();
			}
			if (IsClient)
			{
				player.sleptInTemporaryBed.Value = false;
			}
			if (newDaySync.hasInstance() && newDaySync.hasFinished())
			{
				newDaySync.destroy();
				currentLocation.resetForPlayerEntry();
				return true;
			}
		}
		return false;
	}

	public static void updateWeatherIcon()
	{
		if (IsSnowingHere())
		{
			weatherIcon = 7;
		}
		else if (IsRainingHere())
		{
			weatherIcon = 4;
		}
		else if (IsDebrisWeatherHere() && IsSpring)
		{
			weatherIcon = 3;
		}
		else if (IsDebrisWeatherHere() && IsFall)
		{
			weatherIcon = 6;
		}
		else if (IsDebrisWeatherHere() && IsWinter)
		{
			weatherIcon = 7;
		}
		else if (weddingToday)
		{
			weatherIcon = 0;
		}
		else
		{
			weatherIcon = 2;
		}
		if (IsLightningHere())
		{
			weatherIcon = 5;
		}
		if (Utility.isFestivalDay())
		{
			weatherIcon = 1;
		}
		if (IsGreenRainingHere())
		{
			weatherIcon = 999;
		}
	}

	public static void showEndOfNightStuff()
	{
		hooks.OnGame1_ShowEndOfNightStuff(delegate
		{
			if (!IsDedicatedHost)
			{
				bool flag = false;
				if (player.displayedShippedItems.Count > 0)
				{
					endOfNightMenus.Push(new ShippingMenu(player.displayedShippedItems));
					player.displayedShippedItems.Clear();
					flag = true;
				}
				bool flag2 = false;
				if (player.newLevels.Count > 0 && !flag)
				{
					endOfNightMenus.Push(new SaveGameMenu());
				}
				for (int num = player.newLevels.Count - 1; num >= 0; num--)
				{
					endOfNightMenus.Push(new LevelUpMenu(player.newLevels[num].X, player.newLevels[num].Y));
					flag2 = true;
				}
				if (player.farmingLevel.Value == 10 && player.miningLevel.Value == 10 && player.fishingLevel.Value == 10 && player.foragingLevel.Value == 10 && player.combatLevel.Value == 10 && player.mailReceived.Add("gotMasteryHint") && !player.locationsVisited.Contains("MasteryCave"))
				{
					morningQueue.Enqueue(delegate
					{
						showGlobalMessage(content.LoadString("Strings\\1_6_Strings:MasteryHint"));
					});
				}
				if (flag2)
				{
					playSound("newRecord");
				}
				if (client != null && client.timedOut)
				{
					return;
				}
			}
			if (endOfNightMenus.Count > 0)
			{
				showingEndOfNightStuff = true;
				activeClickableMenu = endOfNightMenus.Pop();
			}
			else
			{
				showingEndOfNightStuff = true;
				activeClickableMenu = new SaveGameMenu();
			}
		});
	}

	/// <summary>Update the game state when the season changes. Despite the name, this may update more than graphics (e.g. it'll remove grass in winter).</summary>
	/// <param name="onLoad">Whether the season is being initialized as part of loading the save, instead of an actual in-game season change.</param>
	public static void setGraphicsForSeason(bool onLoad = false)
	{
		foreach (GameLocation location in locations)
		{
			Season season = location.GetSeason();
			location.seasonUpdate(onLoad);
			location.updateSeasonalTileSheets();
			if (!location.IsOutdoors)
			{
				continue;
			}
			switch (season)
			{
			case Season.Spring:
				eveningColor = new Color(255, 255, 0);
				break;
			case Season.Summer:
				foreach (Object value2 in location.Objects.Values)
				{
					if (!value2.IsWeeds())
					{
						continue;
					}
					switch (value2.QualifiedItemId)
					{
					case "(O)792":
						value2.SetIdAndSprite(value2.ParentSheetIndex + 1);
						continue;
					case "(O)882":
					case "(O)883":
					case "(O)884":
						continue;
					}
					if (random.NextDouble() < 0.3)
					{
						value2.SetIdAndSprite(676);
					}
					else if (random.NextDouble() < 0.3)
					{
						value2.SetIdAndSprite(677);
					}
				}
				eveningColor = new Color(255, 255, 0);
				break;
			case Season.Fall:
				foreach (Object value3 in location.Objects.Values)
				{
					if (value3.IsWeeds())
					{
						switch (value3.QualifiedItemId)
						{
						case "(O)793":
							value3.SetIdAndSprite(value3.ParentSheetIndex + 1);
							break;
						default:
							value3.SetIdAndSprite(random.Choose(678, 679));
							break;
						case "(O)882":
						case "(O)883":
						case "(O)884":
							break;
						}
					}
				}
				eveningColor = new Color(255, 255, 0);
				foreach (WeatherDebris item in debrisWeather)
				{
					item.which = 2;
				}
				break;
			case Season.Winter:
			{
				KeyValuePair<Vector2, Object>[] array = location.Objects.Pairs.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					KeyValuePair<Vector2, Object> keyValuePair = array[i];
					Object value = keyValuePair.Value;
					if (value.IsWeeds())
					{
						switch (value.QualifiedItemId)
						{
						case "(O)882":
						case "(O)883":
						case "(O)884":
							continue;
						}
						location.Objects.Remove(keyValuePair.Key);
					}
				}
				foreach (WeatherDebris item2 in debrisWeather)
				{
					item2.which = 3;
				}
				eveningColor = new Color(245, 225, 170);
				break;
			}
			}
		}
	}

	public static void pauseThenMessage(int millisecondsPause, string message)
	{
		messageAfterPause = message;
		pauseTime = millisecondsPause;
	}

	public static bool IsVisitingIslandToday(string npc_name)
	{
		return netWorldState.Value.IslandVisitors.Contains(npc_name);
	}

	public static bool shouldTimePass(bool ignore_multiplayer = false)
	{
		if (isFestival())
		{
			return false;
		}
		if (CurrentEvent != null && CurrentEvent.isWedding)
		{
			return false;
		}
		if (farmEvent != null)
		{
			return false;
		}
		if (IsMultiplayer && !ignore_multiplayer)
		{
			return !netWorldState.Value.IsTimePaused;
		}
		if (paused || freezeControls || overlayMenu != null || isTimePaused)
		{
			return false;
		}
		if (eventUp)
		{
			return false;
		}
		if (activeClickableMenu != null && !(activeClickableMenu is BobberBar))
		{
			return false;
		}
		if (!player.CanMove && !player.UsingTool)
		{
			return player.forceTimePass;
		}
		return true;
	}

	public static Farmer getPlayerOrEventFarmer()
	{
		if (eventUp && CurrentEvent != null && !CurrentEvent.isFestival && CurrentEvent.farmer != null)
		{
			return CurrentEvent.farmer;
		}
		return player;
	}

	public static void UpdateViewPort(bool overrideFreeze, Point centerPoint)
	{
		previousViewportPosition.X = viewport.X;
		previousViewportPosition.Y = viewport.Y;
		Farmer playerOrEventFarmer = getPlayerOrEventFarmer();
		if (currentLocation == null)
		{
			return;
		}
		if (!viewportFreeze || overrideFreeze)
		{
			Microsoft.Xna.Framework.Rectangle rectangle = ((viewportClampArea == Microsoft.Xna.Framework.Rectangle.Empty) ? new Microsoft.Xna.Framework.Rectangle(0, 0, currentLocation.Map.DisplayWidth, currentLocation.Map.DisplayHeight) : viewportClampArea);
			Point standingPixel = playerOrEventFarmer.StandingPixel;
			bool flag = forceSnapOnNextViewportUpdate || Math.Abs(currentViewportTarget.X + (float)(viewport.Width / 2) + (float)rectangle.X - (float)standingPixel.X) > 64f || Math.Abs(currentViewportTarget.Y + (float)(viewport.Height / 2) + (float)rectangle.Y - (float)standingPixel.Y) > 64f;
			if (centerPoint.X >= rectangle.X + viewport.Width / 2 && centerPoint.X <= rectangle.X + rectangle.Width - viewport.Width / 2)
			{
				if (playerOrEventFarmer.isRafting || flag)
				{
					currentViewportTarget.X = centerPoint.X - viewport.Width / 2;
				}
				else if (Math.Abs(currentViewportTarget.X - (currentViewportTarget.X = centerPoint.X - viewport.Width / 2 + rectangle.X)) > playerOrEventFarmer.getMovementSpeed())
				{
					currentViewportTarget.X += (float)Math.Sign(currentViewportTarget.X - (currentViewportTarget.X = centerPoint.X - viewport.Width / 2 + rectangle.X)) * playerOrEventFarmer.getMovementSpeed();
				}
			}
			else if (centerPoint.X < viewport.Width / 2 + rectangle.X && viewport.Width <= rectangle.Width)
			{
				if (playerOrEventFarmer.isRafting || flag)
				{
					currentViewportTarget.X = rectangle.X;
				}
				else if (Math.Abs(currentViewportTarget.X - (float)rectangle.X) > playerOrEventFarmer.getMovementSpeed())
				{
					currentViewportTarget.X -= (float)Math.Sign(currentViewportTarget.X - (float)rectangle.X) * playerOrEventFarmer.getMovementSpeed();
				}
			}
			else if (viewport.Width <= rectangle.Width)
			{
				if (playerOrEventFarmer.isRafting || flag)
				{
					currentViewportTarget.X = rectangle.X + rectangle.Width - viewport.Width;
				}
				else if (!(Math.Abs(currentViewportTarget.X - (float)(rectangle.Width - viewport.Width)) > playerOrEventFarmer.getMovementSpeed()))
				{
				}
			}
			else if (rectangle.Width < viewport.Width)
			{
				if (playerOrEventFarmer.isRafting || flag)
				{
					currentViewportTarget.X = (rectangle.Width - viewport.Width) / 2 + rectangle.X;
				}
				else
				{
					Math.Abs(currentViewportTarget.X - (float)((rectangle.Width + rectangle.X - viewport.Width) / 2));
					playerOrEventFarmer.getMovementSpeed();
				}
			}
			if (centerPoint.Y >= viewport.Height / 2 && centerPoint.Y <= currentLocation.Map.DisplayHeight - viewport.Height / 2)
			{
				if (playerOrEventFarmer.isRafting || flag)
				{
					currentViewportTarget.Y = centerPoint.Y - viewport.Height / 2;
				}
				else if (Math.Abs(currentViewportTarget.Y - (float)(centerPoint.Y - viewport.Height / 2)) >= playerOrEventFarmer.getMovementSpeed())
				{
					currentViewportTarget.Y -= (float)Math.Sign(currentViewportTarget.Y - (float)(centerPoint.Y - viewport.Height / 2)) * playerOrEventFarmer.getMovementSpeed();
				}
			}
			else if (centerPoint.Y < viewport.Height / 2 && viewport.Height <= currentLocation.Map.DisplayHeight)
			{
				if (playerOrEventFarmer.isRafting || flag)
				{
					currentViewportTarget.Y = 0f;
				}
				else if (Math.Abs(currentViewportTarget.Y - 0f) > playerOrEventFarmer.getMovementSpeed())
				{
					currentViewportTarget.Y -= (float)Math.Sign(currentViewportTarget.Y - 0f) * playerOrEventFarmer.getMovementSpeed();
				}
				currentViewportTarget.Y = 0f;
			}
			else if (viewport.Height <= currentLocation.Map.DisplayHeight)
			{
				if (playerOrEventFarmer.isRafting || flag)
				{
					currentViewportTarget.Y = currentLocation.Map.DisplayHeight - viewport.Height;
				}
				else if (Math.Abs(currentViewportTarget.Y - (float)(currentLocation.Map.DisplayHeight - viewport.Height)) > playerOrEventFarmer.getMovementSpeed())
				{
					currentViewportTarget.Y -= (float)Math.Sign(currentViewportTarget.Y - (float)(currentLocation.Map.DisplayHeight - viewport.Height)) * playerOrEventFarmer.getMovementSpeed();
				}
			}
			else if (currentLocation.Map.DisplayHeight < viewport.Height)
			{
				if (playerOrEventFarmer.isRafting || flag)
				{
					currentViewportTarget.Y = (currentLocation.Map.DisplayHeight - viewport.Height) / 2;
				}
				else if (Math.Abs(currentViewportTarget.Y - (float)((currentLocation.Map.DisplayHeight - viewport.Height) / 2)) > playerOrEventFarmer.getMovementSpeed())
				{
					currentViewportTarget.Y -= (float)Math.Sign(currentViewportTarget.Y - (float)((currentLocation.Map.DisplayHeight - viewport.Height) / 2)) * playerOrEventFarmer.getMovementSpeed();
				}
			}
		}
		if (currentLocation.forceViewportPlayerFollow)
		{
			currentViewportTarget.X = playerOrEventFarmer.Position.X - (float)(viewport.Width / 2);
			currentViewportTarget.Y = playerOrEventFarmer.Position.Y - (float)(viewport.Height / 2);
		}
		bool flag2 = forceSnapOnNextViewportUpdate;
		forceSnapOnNextViewportUpdate = false;
		if (currentViewportTarget.X != -2.1474836E+09f && (!viewportFreeze || overrideFreeze))
		{
			int num = (int)(currentViewportTarget.X - (float)viewport.X);
			if (Math.Abs(num) > 128)
			{
				viewportPositionLerp.X = currentViewportTarget.X;
			}
			else
			{
				viewportPositionLerp.X += (float)num * playerOrEventFarmer.getMovementSpeed() * 0.03f;
			}
			num = (int)(currentViewportTarget.Y - (float)viewport.Y);
			if (Math.Abs(num) > 128)
			{
				viewportPositionLerp.Y = (int)currentViewportTarget.Y;
			}
			else
			{
				viewportPositionLerp.Y += (float)num * playerOrEventFarmer.getMovementSpeed() * 0.03f;
			}
			if (flag2)
			{
				viewportPositionLerp.X = (int)currentViewportTarget.X;
				viewportPositionLerp.Y = (int)currentViewportTarget.Y;
			}
			viewport.X = (int)viewportPositionLerp.X;
			viewport.Y = (int)viewportPositionLerp.Y;
		}
	}

	private void UpdateCharacters(GameTime time)
	{
		if (CurrentEvent?.farmer != null && CurrentEvent.farmer != player)
		{
			CurrentEvent.farmer.Update(time, currentLocation);
		}
		player.Update(time, currentLocation);
		foreach (KeyValuePair<long, Farmer> otherFarmer in otherFarmers)
		{
			if (otherFarmer.Key != player.UniqueMultiplayerID)
			{
				otherFarmer.Value.UpdateIfOtherPlayer(time);
			}
		}
	}

	public static void addMail(string mailName, bool noLetter = false, bool sendToEveryone = false)
	{
		if (sendToEveryone)
		{
			multiplayer.broadcastPartyWideMail(mailName, Multiplayer.PartyWideMessageQueue.SeenMail, noLetter);
			return;
		}
		mailName = mailName.Trim();
		mailName = mailName.Replace(Environment.NewLine, "");
		if (!player.hasOrWillReceiveMail(mailName))
		{
			if (noLetter)
			{
				player.mailReceived.Add(mailName);
			}
			else
			{
				player.mailbox.Add(mailName);
			}
		}
	}

	public static void addMailForTomorrow(string mailName, bool noLetter = false, bool sendToEveryone = false)
	{
		if (sendToEveryone)
		{
			multiplayer.broadcastPartyWideMail(mailName, Multiplayer.PartyWideMessageQueue.MailForTomorrow, noLetter);
			return;
		}
		mailName = mailName.Trim();
		mailName = mailName.Replace(Environment.NewLine, "");
		if (player.hasOrWillReceiveMail(mailName))
		{
			return;
		}
		if (noLetter)
		{
			mailName += "%&NL&%";
		}
		player.mailForTomorrow.Add(mailName);
		if (!sendToEveryone || !IsMultiplayer)
		{
			return;
		}
		foreach (Farmer value in otherFarmers.Values)
		{
			if (value != player && !player.hasOrWillReceiveMail(mailName))
			{
				value.mailForTomorrow.Add(mailName);
			}
		}
	}

	public static void drawDialogue(NPC speaker)
	{
		if (speaker.CurrentDialogue.Count == 0)
		{
			return;
		}
		activeClickableMenu = new DialogueBox(speaker.CurrentDialogue.Peek());
		if (activeClickableMenu is DialogueBox { dialogueFinished: not false })
		{
			activeClickableMenu = null;
			return;
		}
		dialogueUp = true;
		if (!eventUp)
		{
			player.Halt();
			player.CanMove = false;
		}
		if (speaker != null)
		{
			currentSpeaker = speaker;
		}
	}

	public static void multipleDialogues(string[] messages)
	{
		activeClickableMenu = new DialogueBox(messages.ToList());
		dialogueUp = true;
		player.CanMove = false;
	}

	public static void drawDialogueNoTyping(string dialogue)
	{
		drawObjectDialogue(dialogue);
		if (activeClickableMenu is DialogueBox dialogueBox)
		{
			dialogueBox.showTyping = false;
		}
	}

	public static void drawDialogueNoTyping(List<string> dialogues)
	{
		drawObjectDialogue(dialogues);
		if (activeClickableMenu is DialogueBox dialogueBox)
		{
			dialogueBox.showTyping = false;
		}
	}

	/// <summary>Show a dialogue box with text from an NPC's answering machine.</summary>
	/// <param name="npc">The NPC whose answering machine to display.</param>
	/// <param name="translationKey">The translation key for the message text.</param>
	/// <param name="substitutions">The token substitutions for placeholders in the translation text, if any.</param>
	public static void DrawAnsweringMachineDialogue(NPC npc, string translationKey, params object[] substitutions)
	{
		Dialogue dialogue = Dialogue.FromTranslation(npc, translationKey, substitutions);
		dialogue.overridePortrait = temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine");
		DrawDialogue(dialogue);
	}

	/// <summary>Show a dialogue box with text from an NPC.</summary>
	/// <param name="npc">The NPC whose dialogue to display.</param>
	/// <param name="translationKey">The translation from which to take the dialogue text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	public static void DrawDialogue(NPC npc, string translationKey)
	{
		DrawDialogue(new Dialogue(npc, translationKey));
	}

	/// <summary>Show a dialogue box with text from an NPC.</summary>
	/// <param name="npc">The NPC whose dialogue to display.</param>
	/// <param name="translationKey">The translation from which to take the dialogue text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="substitutions">The values with which to replace placeholders like <c>{0}</c> in the loaded text.</param>
	public static void DrawDialogue(NPC npc, string translationKey, params object[] substitutions)
	{
		DrawDialogue(Dialogue.FromTranslation(npc, translationKey, substitutions));
	}

	/// <summary>Show a dialogue box with text from an NPC.</summary>
	/// <param name="dialogue">The dialogue to display.</param>
	public static void DrawDialogue(Dialogue dialogue)
	{
		if (dialogue.speaker != null)
		{
			dialogue.speaker.CurrentDialogue.Push(dialogue);
			drawDialogue(dialogue.speaker);
			return;
		}
		activeClickableMenu = new DialogueBox(dialogue);
		dialogueUp = true;
		if (!eventUp)
		{
			player.Halt();
			player.CanMove = false;
		}
	}

	private static void checkIfDialogueIsQuestion()
	{
		if (currentSpeaker != null && currentSpeaker.CurrentDialogue.Count > 0 && currentSpeaker.CurrentDialogue.Peek().isCurrentDialogueAQuestion())
		{
			questionChoices.Clear();
			isQuestion = true;
			List<NPCDialogueResponse> nPCResponseOptions = currentSpeaker.CurrentDialogue.Peek().getNPCResponseOptions();
			for (int i = 0; i < nPCResponseOptions.Count; i++)
			{
				questionChoices.Add(nPCResponseOptions[i]);
			}
		}
	}

	public static void drawLetterMessage(string message)
	{
		activeClickableMenu = new LetterViewerMenu(message);
	}

	public static void drawObjectDialogue(string dialogue)
	{
		activeClickableMenu?.emergencyShutDown();
		activeClickableMenu = new DialogueBox(dialogue);
		player.CanMove = false;
		dialogueUp = true;
	}

	public static void drawObjectDialogue(List<string> dialogue)
	{
		activeClickableMenu?.emergencyShutDown();
		activeClickableMenu = new DialogueBox(dialogue);
		player.CanMove = false;
		dialogueUp = true;
	}

	public static void drawObjectQuestionDialogue(string dialogue, Response[] choices, int width)
	{
		activeClickableMenu = new DialogueBox(dialogue, choices, width);
		dialogueUp = true;
		player.CanMove = false;
	}

	public static void drawObjectQuestionDialogue(string dialogue, Response[] choices)
	{
		activeClickableMenu = new DialogueBox(dialogue, choices);
		dialogueUp = true;
		player.CanMove = false;
	}

	public static void warpCharacter(NPC character, string targetLocationName, Point position)
	{
		warpCharacter(character, targetLocationName, new Vector2(position.X, position.Y));
	}

	public static void warpCharacter(NPC character, string targetLocationName, Vector2 position)
	{
		warpCharacter(character, RequireLocation(targetLocationName), position);
	}

	public static void warpCharacter(NPC character, GameLocation targetLocation, Vector2 position)
	{
		foreach (string activePassiveFestival in netWorldState.Value.ActivePassiveFestivals)
		{
			if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data) && dayOfMonth >= data.StartDay && dayOfMonth <= data.EndDay && data.Season == season && data.MapReplacements != null && data.MapReplacements.TryGetValue(targetLocation.name.Value, out var value))
			{
				targetLocation = RequireLocation(value);
			}
		}
		if (targetLocation.name.Equals("Trailer") && MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
		{
			targetLocation = RequireLocation("Trailer_Big");
			if (position.X == 12f && position.Y == 9f)
			{
				position.X = 13f;
				position.Y = 24f;
			}
		}
		if (IsClient)
		{
			multiplayer.requestCharacterWarp(character, targetLocation, position);
			return;
		}
		if (!targetLocation.characters.Contains(character))
		{
			character.currentLocation?.characters.Remove(character);
			targetLocation.addCharacter(character);
		}
		character.isCharging = false;
		character.speed = 2;
		character.blockedInterval = 0;
		NPC.getTextureNameForCharacter(character.Name);
		character.position.X = position.X * 64f;
		character.position.Y = position.Y * 64f;
		if (character.CurrentDialogue.Count > 0 && character.CurrentDialogue.Peek().removeOnNextMove && character.Tile != character.DefaultPosition / 64f)
		{
			character.CurrentDialogue.Pop();
		}
		if (targetLocation is FarmHouse farmHouse)
		{
			character.arriveAtFarmHouse(farmHouse);
		}
		else
		{
			character.arriveAt(targetLocation);
		}
		if (character.currentLocation != null && !character.currentLocation.Equals(targetLocation))
		{
			character.currentLocation.characters.Remove(character);
		}
		character.currentLocation = targetLocation;
	}

	public static LocationRequest getLocationRequest(string locationName, bool isStructure = false)
	{
		if (locationName == null)
		{
			throw new ArgumentException();
		}
		return new LocationRequest(locationName, isStructure, getLocationFromName(locationName, isStructure));
	}

	public static void warpHome()
	{
		LocationRequest obj = getLocationRequest(player.homeLocation.Value);
		obj.OnWarp += delegate
		{
			player.position.Set(Utility.PointToVector2((currentLocation as FarmHouse).GetPlayerBedSpot()) * 64f);
		};
		warpFarmer(obj, 5, 9, player.FacingDirection);
	}

	public static void warpFarmer(string locationName, int tileX, int tileY, bool flip)
	{
		warpFarmer(getLocationRequest(locationName), tileX, tileY, flip ? ((player.FacingDirection + 2) % 4) : player.FacingDirection);
	}

	public static void warpFarmer(string locationName, int tileX, int tileY, int facingDirectionAfterWarp)
	{
		warpFarmer(getLocationRequest(locationName), tileX, tileY, facingDirectionAfterWarp);
	}

	public static void warpFarmer(string locationName, int tileX, int tileY, int facingDirectionAfterWarp, bool isStructure)
	{
		warpFarmer(getLocationRequest(locationName, isStructure), tileX, tileY, facingDirectionAfterWarp);
	}

	public virtual bool ShouldDismountOnWarp(Horse mount, GameLocation old_location, GameLocation new_location)
	{
		if (mount == null)
		{
			return false;
		}
		if (currentLocation != null && currentLocation.IsOutdoors && new_location != null)
		{
			return !new_location.IsOutdoors;
		}
		return false;
	}

	public static void warpFarmer(LocationRequest locationRequest, int tileX, int tileY, int facingDirectionAfterWarp)
	{
		int warp_offset_x = nextFarmerWarpOffsetX;
		int warp_offset_y = nextFarmerWarpOffsetY;
		nextFarmerWarpOffsetX = 0;
		nextFarmerWarpOffsetY = 0;
		foreach (string activePassiveFestival in netWorldState.Value.ActivePassiveFestivals)
		{
			if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data) && dayOfMonth >= data.StartDay && dayOfMonth <= data.EndDay && data.Season == season && data.MapReplacements != null && data.MapReplacements.TryGetValue(locationRequest.Name, out var value))
			{
				locationRequest = getLocationRequest(value);
			}
		}
		switch (locationRequest.Name)
		{
		case "BusStop":
			if (tileX < 10)
			{
				tileX = 10;
			}
			break;
		case "Farm":
			switch (currentLocation?.NameOrUniqueName)
			{
			case "FarmCave":
			{
				if (tileX != 34 || tileY != 6)
				{
					break;
				}
				if (getFarm().TryGetMapPropertyAs("FarmCaveEntry", out Point parsed3, required: false))
				{
					tileX = parsed3.X;
					tileY = parsed3.Y;
					break;
				}
				switch (whichFarm)
				{
				case 6:
					tileX = 34;
					tileY = 16;
					break;
				case 5:
					tileX = 30;
					tileY = 36;
					break;
				}
				break;
			}
			case "Forest":
			{
				if (tileX != 41 || tileY != 64)
				{
					break;
				}
				if (getFarm().TryGetMapPropertyAs("ForestEntry", out Point parsed4, required: false))
				{
					tileX = parsed4.X;
					tileY = parsed4.Y;
					break;
				}
				switch (whichFarm)
				{
				case 6:
					tileX = 82;
					tileY = 103;
					break;
				case 5:
					tileX = 40;
					tileY = 64;
					break;
				}
				break;
			}
			case "BusStop":
			{
				if (tileX == 79 && tileY == 17 && getFarm().TryGetMapPropertyAs("BusStopEntry", out Point parsed2, required: false))
				{
					tileX = parsed2.X;
					tileY = parsed2.Y;
				}
				break;
			}
			case "Backwoods":
			{
				if (tileX == 40 && tileY == 0 && getFarm().TryGetMapPropertyAs("BackwoodsEntry", out Point parsed, required: false))
				{
					tileX = parsed.X;
					tileY = parsed.Y;
				}
				break;
			}
			}
			break;
		case "IslandSouth":
			if (tileX <= 15 && tileY <= 6)
			{
				tileX = 21;
				tileY = 43;
			}
			break;
		case "Trailer":
			if (MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
			{
				locationRequest = getLocationRequest("Trailer_Big");
				tileX = 13;
				tileY = 24;
			}
			break;
		case "Club":
			if (player.hasClubCard)
			{
				break;
			}
			locationRequest = getLocationRequest("SandyHouse");
			locationRequest.OnWarp += delegate
			{
				NPC characterFromName = currentLocation.getCharacterFromName("Bouncer");
				if (characterFromName != null)
				{
					Vector2 vector = new Vector2(17f, 4f);
					characterFromName.showTextAboveHead(content.LoadString("Strings\\Locations:Club_Bouncer_TextAboveHead" + (random.Next(2) + 1)));
					int num = random.Next();
					currentLocation.playSound("thudStep");
					multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(288, 100f, 1, 24, vector * 64f, flicker: true, flipped: false, currentLocation, player)
					{
						shakeIntensity = 0.5f,
						shakeIntensityChange = 0.002f,
						extraInfoForEndBehavior = num,
						endFunction = currentLocation.removeTemporarySpritesWithID
					}, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f + new Vector2(5f, 0f) * 4f, flicker: true, flipped: false, 0.0263f, 0f, Color.Yellow, 4f, 0f, 0f, 0f)
					{
						id = num
					}, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f + new Vector2(5f, 0f) * 4f, flicker: true, flipped: true, 0.0263f, 0f, Color.Orange, 4f, 0f, 0f, 0f)
					{
						delayBeforeAnimationStart = 100,
						id = num
					}, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f + new Vector2(5f, 0f) * 4f, flicker: true, flipped: false, 0.0263f, 0f, Color.White, 3f, 0f, 0f, 0f)
					{
						delayBeforeAnimationStart = 200,
						id = num
					});
					currentLocation.netAudio.StartPlaying("fuse");
				}
			};
			tileX = 17;
			tileY = 4;
			break;
		}
		if (VolcanoDungeon.IsGeneratedLevel(locationRequest.Name))
		{
			warp_offset_x = 0;
			warp_offset_y = 0;
		}
		if (player.isRidingHorse() && currentLocation != null)
		{
			GameLocation gameLocation = locationRequest.Location;
			if (gameLocation == null)
			{
				gameLocation = getLocationFromName(locationRequest.Name);
			}
			if (game1.ShouldDismountOnWarp(player.mount, currentLocation, gameLocation))
			{
				player.mount.dismount();
				warp_offset_x = 0;
				warp_offset_y = 0;
			}
		}
		if (weatherIcon == 1 && whereIsTodaysFest != null && locationRequest.Name.Equals(whereIsTodaysFest) && !warpingForForcedRemoteEvent)
		{
			string[] array = ArgUtility.SplitBySpace(temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + currentSeason + dayOfMonth)["conditions"].Split('/')[1]);
			if (timeOfDay <= Convert.ToInt32(array[1]))
			{
				if (timeOfDay < Convert.ToInt32(array[0]))
				{
					if (!(currentLocation?.Name == "Hospital"))
					{
						player.Position = player.lastPosition;
						drawObjectDialogue(content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2973"));
						return;
					}
					locationRequest = getLocationRequest("BusStop");
					tileX = 34;
					tileY = 23;
				}
				else
				{
					if (IsMultiplayer)
					{
						netReady.SetLocalReady("festivalStart", ready: true);
						activeClickableMenu = new ReadyCheckDialog("festivalStart", allowCancel: true, delegate
						{
							exitActiveMenu();
							if (player.mount != null)
							{
								player.mount.dismount();
								warp_offset_x = 0;
								warp_offset_y = 0;
							}
							performWarpFarmer(locationRequest, tileX, tileY, facingDirectionAfterWarp);
						});
						return;
					}
					if (player.mount != null)
					{
						player.mount.dismount();
						warp_offset_x = 0;
						warp_offset_y = 0;
					}
				}
			}
		}
		tileX += warp_offset_x;
		tileY += warp_offset_y;
		performWarpFarmer(locationRequest, tileX, tileY, facingDirectionAfterWarp);
	}

	private static void performWarpFarmer(LocationRequest locationRequest, int tileX, int tileY, int facingDirectionAfterWarp)
	{
		if (locationRequest.Location != null)
		{
			if (tileX >= locationRequest.Location.Map.Layers[0].LayerWidth - 1)
			{
				tileX--;
			}
			if (IsMasterGame)
			{
				locationRequest.Location.hostSetup();
			}
		}
		log.Verbose("Warping to " + locationRequest.Name);
		if (player.IsSitting())
		{
			player.StopSitting(animate: false);
		}
		if (player.UsingTool)
		{
			player.completelyStopAnimatingOrDoingAction();
		}
		player.previousLocationName = ((player.currentLocation != null) ? player.currentLocation.name.Value : "");
		Game1.locationRequest = locationRequest;
		xLocationAfterWarp = tileX;
		yLocationAfterWarp = tileY;
		_isWarping = true;
		Game1.facingDirectionAfterWarp = facingDirectionAfterWarp;
		fadeScreenToBlack();
		setRichPresence("location", locationRequest.Name);
		if (IsDedicatedHost)
		{
			fadeToBlackAlpha = 1.1f;
			fadeToBlack = true;
			nonWarpFade = false;
		}
	}

	private static void notifyServerOfWarp(bool needsLocationInfo)
	{
		if (locationRequest != null)
		{
			byte b = (byte)((locationRequest.IsStructure ? 1u : 0u) | (uint)(warpingForForcedRemoteEvent ? 2 : 0) | (uint)(needsLocationInfo ? 4 : 0));
			b = facingDirectionAfterWarp switch
			{
				1 => (byte)(b | 0x10), 
				2 => (byte)(b | 0x20), 
				3 => (byte)(b | 0x40), 
				_ => (byte)(b | 8), 
			};
			client.sendMessage(5, (short)xLocationAfterWarp, (short)yLocationAfterWarp, locationRequest.Name, b);
		}
	}

	public static void requestLocationInfoFromServer()
	{
		notifyServerOfWarp(needsLocationInfo: true);
		currentLocation = null;
		player.Position = new Vector2(xLocationAfterWarp * 64, yLocationAfterWarp * 64 - (player.Sprite.getHeight() - 32) + 16);
		player.faceDirection(facingDirectionAfterWarp);
	}

	/// <summary>Get the first NPC which matches a condition.</summary>
	/// <typeparam name="T">The expected NPC type.</typeparam>
	/// <param name="check">The condition to check on each NPC.</param>
	/// <param name="includeEventActors">Whether to match temporary event actors.</param>
	/// <returns>Returns the matching NPC if found, else <c>null</c>.</returns>
	public static T GetCharacterWhere<T>(Func<T, bool> check, bool includeEventActors = false) where T : NPC
	{
		T match = null;
		T fallback = null;
		Utility.ForEachCharacter(delegate(NPC rawNpc)
		{
			if (rawNpc is T val && check(val))
			{
				if (val.currentLocation?.IsActiveLocation() ?? false)
				{
					match = val;
					return false;
				}
				fallback = val;
			}
			return true;
		}, includeEventActors);
		return match ?? fallback;
	}

	/// <summary>Get the first NPC of the given type.</summary>
	/// <typeparam name="T">The expected NPC type.</typeparam>
	/// <param name="includeEventActors">Whether to match temporary event actors.</param>
	/// <returns>Returns the matching NPC if found, else <c>null</c>.</returns>
	public static T GetCharacterOfType<T>(bool includeEventActors = false) where T : NPC
	{
		T match = null;
		T fallback = null;
		Utility.ForEachCharacter(delegate(NPC rawNpc)
		{
			if (rawNpc is T val)
			{
				if (rawNpc.currentLocation?.IsActiveLocation() ?? false)
				{
					match = val;
					return false;
				}
				fallback = val;
			}
			return true;
		}, includeEventActors);
		return match ?? fallback;
	}

	/// <summary>Get an NPC by its name.</summary>
	/// <typeparam name="T">The expected NPC type.</typeparam>
	/// <param name="name">The NPC name.</param>
	/// <param name="mustBeVillager">Whether to only match NPCs which return true for <see cref="P:StardewValley.NPC.IsVillager" />.</param>
	/// <param name="includeEventActors">Whether to match temporary event actors.</param>
	/// <returns>Returns the matching NPC if found, else <c>null</c>.</returns>
	public static T getCharacterFromName<T>(string name, bool mustBeVillager = true, bool includeEventActors = false) where T : NPC
	{
		T match = null;
		T fallback = null;
		Utility.ForEachCharacter(delegate(NPC rawNpc)
		{
			if (rawNpc is T val && val.Name == name && (!mustBeVillager || val.IsVillager))
			{
				if (val.currentLocation?.IsActiveLocation() ?? false)
				{
					match = val;
					return false;
				}
				fallback = val;
			}
			return true;
		}, includeEventActors);
		return match ?? fallback;
	}

	/// <summary>Get an NPC by its name.</summary>
	/// <param name="name">The NPC name.</param>
	/// <param name="mustBeVillager">Whether to only match NPCs which return true for <see cref="P:StardewValley.NPC.IsVillager" />.</param>
	/// <param name="includeEventActors">Whether to match temporary event actors.</param>
	/// <returns>Returns the matching NPC if found, else <c>null</c>.</returns>
	public static NPC getCharacterFromName(string name, bool mustBeVillager = true, bool includeEventActors = false)
	{
		NPC match = null;
		NPC fallback = null;
		Utility.ForEachCharacter(delegate(NPC npc)
		{
			if (npc.Name == name && (!mustBeVillager || npc.IsVillager))
			{
				if (npc.currentLocation?.IsActiveLocation() ?? false)
				{
					match = npc;
					return false;
				}
				fallback = npc;
			}
			return true;
		}, includeEventActors);
		return match ?? fallback;
	}

	/// <summary>Get an NPC by its name, or throw an exception if it's not found.</summary>
	/// <param name="name">The NPC name.</param>
	/// <param name="mustBeVillager">Whether to only match NPCs which return true for <see cref="P:StardewValley.NPC.IsVillager" />.</param>
	public static NPC RequireCharacter(string name, bool mustBeVillager = true)
	{
		return getCharacterFromName(name, mustBeVillager) ?? throw new KeyNotFoundException($"Required {(mustBeVillager ? "villager" : "NPC")} '{name}' not found.");
	}

	/// <summary>Get an NPC by its name, or throw an exception if it's not found.</summary>
	/// <typeparam name="T">The expected NPC type.</typeparam>
	/// <param name="name">The NPC name.</param>
	/// <param name="mustBeVillager">Whether to only match NPCs which return true for <see cref="P:StardewValley.NPC.IsVillager" />.</param>
	/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">There's no NPC matching the given arguments.</exception>
	/// <exception cref="T:System.InvalidCastException">The NPC found can't be converted to <typeparamref name="T" />.</exception>
	public static T RequireCharacter<T>(string name, bool mustBeVillager = true) where T : NPC
	{
		NPC characterFromName = getCharacterFromName(name, mustBeVillager);
		if (!(characterFromName is T result))
		{
			if (characterFromName == null)
			{
				throw new KeyNotFoundException($"Required {(mustBeVillager ? "villager" : "NPC")} '{name}' not found.");
			}
			throw new InvalidCastException($"Can't convert NPC '{name}' from '{characterFromName?.GetType().FullName}' to the required '{typeof(T).FullName}'.");
		}
		return result;
	}

	/// <summary>Get a location by its name, or throw an exception if it's not found.</summary>
	/// <param name="name">The location name.</param>
	/// <param name="isStructure">Whether the location is an interior structure.</param>
	/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">There's no location matching the given arguments.</exception>
	public static GameLocation RequireLocation(string name, bool isStructure = false)
	{
		return getLocationFromName(name, isStructure) ?? throw new KeyNotFoundException($"Required {(isStructure ? "structure " : "")}location '{name}' not found.");
	}

	/// <summary>Get a location by its name, or throw an exception if it's not found.</summary>
	/// <typeparam name="TLocation">The expected location type.</typeparam>
	/// <param name="name">The location name.</param>
	/// <param name="isStructure">Whether the location is an interior structure.</param>
	/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">There's no location matching the given arguments.</exception>
	/// <exception cref="T:System.InvalidCastException">The location found can't be converted to <typeparamref name="TLocation" />.</exception>
	public static TLocation RequireLocation<TLocation>(string name, bool isStructure = false) where TLocation : GameLocation
	{
		GameLocation locationFromName = getLocationFromName(name, isStructure);
		if (!(locationFromName is TLocation result))
		{
			if (locationFromName == null)
			{
				throw new KeyNotFoundException($"Required {(isStructure ? "structure " : "")}location '{name}' not found.");
			}
			throw new InvalidCastException($"Can't convert location {name} from '{locationFromName?.GetType().FullName}' to the required '{typeof(TLocation).FullName}'.");
		}
		return result;
	}

	/// <summary>Get a location by its name, or <c>null</c> if it's not found.</summary>
	/// <param name="name">The location name.</param>
	public static GameLocation getLocationFromName(string name)
	{
		return getLocationFromName(name, isStructure: false);
	}

	/// <summary>Get a location by its name, or <c>null</c> if it's not found.</summary>
	/// <param name="name">The location name.</param>
	/// <param name="isStructure">Whether the location is an interior structure.</param>
	public static GameLocation getLocationFromName(string name, bool isStructure)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (currentLocation != null)
		{
			if (!isStructure)
			{
				if (currentLocation.name.Value.EqualsIgnoreCase(name))
				{
					return currentLocation;
				}
				if (currentLocation.isStructure.Value && currentLocation.Root != null && currentLocation.Root.Value.NameOrUniqueName.EqualsIgnoreCase(name))
				{
					return currentLocation.Root.Value;
				}
			}
			else if (currentLocation.NameOrUniqueName == name)
			{
				return currentLocation;
			}
		}
		if (_locationLookup.TryGetValue(name, out var value))
		{
			return value;
		}
		return getLocationFromNameInLocationsList(name, isStructure);
	}

	/// <summary>Get a location by its name (ignoring the cache and current location), or <c>null</c> if it's not found.</summary>
	/// <param name="name">The location name.</param>
	/// <param name="isStructure">Whether the location is an interior structure.</param>
	public static GameLocation getLocationFromNameInLocationsList(string name, bool isStructure = false)
	{
		for (int i = 0; i < locations.Count; i++)
		{
			GameLocation gameLocation = locations[i];
			if (!isStructure)
			{
				if (gameLocation.Name.EqualsIgnoreCase(name))
				{
					_locationLookup[gameLocation.Name] = gameLocation;
					return gameLocation;
				}
				continue;
			}
			GameLocation gameLocation2 = findStructure(gameLocation, name);
			if (gameLocation2 != null)
			{
				_locationLookup[name] = gameLocation2;
				return gameLocation2;
			}
		}
		if (MineShaft.IsGeneratedLevel(name))
		{
			return MineShaft.GetMine(name);
		}
		if (VolcanoDungeon.IsGeneratedLevel(name))
		{
			return VolcanoDungeon.GetLevel(name);
		}
		if (!isStructure)
		{
			return getLocationFromName(name, isStructure: true);
		}
		return null;
	}

	public static void flushLocationLookup()
	{
		_locationLookup.Clear();
	}

	public static void removeLocationFromLocationLookup(string nameOrUniqueName)
	{
		_locationLookup.RemoveWhere((KeyValuePair<string, GameLocation> p) => p.Value.NameOrUniqueName == nameOrUniqueName);
	}

	public static void removeLocationFromLocationLookup(GameLocation location)
	{
		_locationLookup.RemoveWhere((KeyValuePair<string, GameLocation> p) => p.Value == location);
	}

	public static GameLocation findStructure(GameLocation parentLocation, string name)
	{
		foreach (Building building in parentLocation.buildings)
		{
			if (building.HasIndoorsName(name))
			{
				return building.GetIndoors();
			}
		}
		return null;
	}

	public static void addNewFarmBuildingMaps()
	{
		FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(player);
		if (player.HouseUpgradeLevel >= 1 && homeOfFarmer.Map.Id.Equals("FarmHouse"))
		{
			homeOfFarmer.updateMap();
		}
	}

	public static void PassOutNewDay()
	{
		player.lastSleepLocation.Value = currentLocation.NameOrUniqueName;
		player.lastSleepPoint.Value = player.TilePoint;
		if (!IsMultiplayer)
		{
			NewDay(0f);
			return;
		}
		player.FarmerSprite.setCurrentSingleFrame(5, 3000);
		player.FarmerSprite.PauseForSingleAnimation = true;
		player.passedOut = true;
		if (activeClickableMenu != null)
		{
			activeClickableMenu.emergencyShutDown();
			exitActiveMenu();
		}
		activeClickableMenu = new ReadyCheckDialog("sleep", allowCancel: false, delegate
		{
			NewDay(0f);
		});
	}

	public static void NewDay(float timeToPause)
	{
		if (activeClickableMenu is ReadyCheckDialog { checkName: "sleep" } readyCheckDialog && !readyCheckDialog.isCancelable())
		{
			readyCheckDialog.confirm();
		}
		currentMinigame = null;
		newDay = true;
		newDaySync.create();
		if (player.isInBed.Value || player.passedOut)
		{
			nonWarpFade = true;
			screenFade.FadeScreenToBlack(player.passedOut ? 1.1f : 0f);
			player.Halt();
			player.currentEyes = 1;
			player.blinkTimer = -4000;
			player.CanMove = false;
			player.passedOut = false;
			pauseTime = timeToPause;
		}
		if (activeClickableMenu != null && !dialogueUp)
		{
			activeClickableMenu.emergencyShutDown();
			exitActiveMenu();
		}
	}

	public static void screenGlowOnce(Color glowColor, bool hold, float rate = 0.005f, float maxAlpha = 0.3f)
	{
		screenGlowMax = maxAlpha;
		screenGlowRate = rate;
		screenGlowAlpha = 0f;
		screenGlowUp = true;
		screenGlowColor = glowColor;
		screenGlow = true;
		screenGlowHold = hold;
	}

	public static string shortDayNameFromDayOfSeason(int dayOfSeason)
	{
		return (dayOfSeason % 7) switch
		{
			0 => "Sun", 
			1 => "Mon", 
			2 => "Tue", 
			3 => "Wed", 
			4 => "Thu", 
			5 => "Fri", 
			6 => "Sat", 
			_ => "", 
		};
	}

	public static string shortDayDisplayNameFromDayOfSeason(int dayOfSeason)
	{
		if (dayOfSeason < 0)
		{
			return string.Empty;
		}
		return _shortDayDisplayName[dayOfSeason % 7];
	}

	public static void runTestEvent()
	{
		StreamReader streamReader = new StreamReader("test_event.txt");
		string? locationName = streamReader.ReadLine();
		string event_string = streamReader.ReadToEnd();
		event_string = event_string.Replace("\r\n", "/").Replace("\n", "/");
		log.Verbose("Running test event: " + event_string);
		LocationRequest locationRequest = getLocationRequest(locationName);
		locationRequest.OnWarp += delegate
		{
			currentLocation.currentEvent = new Event(event_string);
			currentLocation.checkForEvents();
		};
		int x = 8;
		int y = 8;
		Utility.getDefaultWarpLocation(locationName, ref x, ref y);
		warpFarmer(locationRequest, x, y, player.FacingDirection);
	}

	public static bool isMusicContextActiveButNotPlaying(MusicContext music_context = MusicContext.Default)
	{
		if (_activeMusicContext != music_context)
		{
			return false;
		}
		if (morningSongPlayAction != null)
		{
			return false;
		}
		string musicTrackName = getMusicTrackName(music_context);
		if (musicTrackName == "none")
		{
			return true;
		}
		if (currentSong != null && currentSong.Name == musicTrackName && !currentSong.IsPlaying)
		{
			return true;
		}
		return false;
	}

	public static bool IsMusicContextActive(MusicContext music_context = MusicContext.Default)
	{
		if (_activeMusicContext != music_context)
		{
			return true;
		}
		return false;
	}

	public static bool doesMusicContextHaveTrack(MusicContext music_context = MusicContext.Default)
	{
		return _requestedMusicTracks.ContainsKey(music_context);
	}

	public static string getMusicTrackName(MusicContext music_context = MusicContext.Default)
	{
		if (_requestedMusicTracks.TryGetValue(music_context, out var value))
		{
			return value.Key;
		}
		if (music_context == MusicContext.Default)
		{
			return getMusicTrackName(MusicContext.SubLocation);
		}
		return "none";
	}

	public static void stopMusicTrack(MusicContext music_context)
	{
		if (_requestedMusicTracks.Remove(music_context))
		{
			if (music_context == MusicContext.Default)
			{
				stopMusicTrack(MusicContext.SubLocation);
			}
			UpdateRequestedMusicTrack();
		}
	}

	public static void changeMusicTrack(string newTrackName, bool track_interruptable = false, MusicContext music_context = MusicContext.Default)
	{
		if (newTrackName == null)
		{
			return;
		}
		if (music_context == MusicContext.Default)
		{
			if (morningSongPlayAction != null)
			{
				if (delayedActions.Contains(morningSongPlayAction))
				{
					delayedActions.Remove(morningSongPlayAction);
				}
				morningSongPlayAction = null;
			}
			if (IsGreenRainingHere() && !currentLocation.InIslandContext() && IsRainingHere(currentLocation) && !newTrackName.Equals("rain"))
			{
				return;
			}
		}
		if (music_context == MusicContext.Default || music_context == MusicContext.SubLocation)
		{
			IsPlayingBackgroundMusic = false;
			IsPlayingOutdoorsAmbience = false;
			IsPlayingNightAmbience = false;
			IsPlayingTownMusic = false;
			IsPlayingMorningSong = false;
		}
		if (music_context != MusicContext.ImportantSplitScreenMusic && !player.songsHeard.Contains(newTrackName))
		{
			Utility.farmerHeardSong(newTrackName);
		}
		_requestedMusicTracks[music_context] = new KeyValuePair<string, bool>(newTrackName, track_interruptable);
		UpdateRequestedMusicTrack();
	}

	public static void UpdateRequestedMusicTrack()
	{
		_activeMusicContext = MusicContext.Default;
		KeyValuePair<string, bool> keyValuePair = new KeyValuePair<string, bool>("none", value: true);
		for (int i = 0; i < 6; i++)
		{
			MusicContext musicContext = (MusicContext)i;
			if (_requestedMusicTracks.TryGetValue(musicContext, out var value))
			{
				if (musicContext != MusicContext.ImportantSplitScreenMusic)
				{
					_activeMusicContext = musicContext;
				}
				keyValuePair = value;
			}
		}
		if (keyValuePair.Key != requestedMusicTrack || keyValuePair.Value != requestedMusicTrackOverrideable)
		{
			requestedMusicDirty = true;
			requestedMusicTrack = keyValuePair.Key;
			requestedMusicTrackOverrideable = keyValuePair.Value;
		}
	}

	/// <summary>Warp the player into a generated mine level.</summary>
	/// <param name="whatLevel">The mine level.</param>
	/// <param name="forceLayout">The layout in <c>Maps/Mines</c> to use, or <c>null</c> to choose a random one based on the level.</param>
	public static void enterMine(int whatLevel, int? forceLayout = null)
	{
		warpFarmer(MineShaft.GetLevelName(whatLevel, forceLayout), 6, 6, 2);
		player.temporarilyInvincible = true;
		player.temporaryInvincibilityTimer = 0;
		player.flashDuringThisTemporaryInvincibility = false;
		player.currentTemporaryInvincibilityDuration = 1000;
	}

	/// <summary>Get the season which currently applies to a location.</summary>
	/// <param name="location">The location to check, or <c>null</c> for the global season.</param>
	public static Season GetSeasonForLocation(GameLocation location)
	{
		return location?.GetSeason() ?? season;
	}

	/// <summary>Get the season which currently applies to a location as a numeric index.</summary>
	/// <param name="location">The location to check, or <c>null</c> for the global season.</param>
	/// <remarks>Most code should use <see cref="M:StardewValley.Game1.GetSeasonForLocation(StardewValley.GameLocation)" /> instead.</remarks>
	public static int GetSeasonIndexForLocation(GameLocation location)
	{
		return location?.GetSeasonIndex() ?? seasonIndex;
	}

	/// <summary>Get the season which currently applies to a location as a string.</summary>
	/// <param name="location">The location to check, or <c>null</c> for the global season.</param>
	/// <remarks>Most code should use <see cref="M:StardewValley.Game1.GetSeasonForLocation(StardewValley.GameLocation)" /> instead.</remarks>
	public static string GetSeasonKeyForLocation(GameLocation location)
	{
		return location?.GetSeasonKey() ?? currentSeason;
	}

	/// <summary>Unlock an achievement for the current platform.</summary>
	/// <param name="which">The achievement to unlock.</param>
	public static void getPlatformAchievement(string which)
	{
		Program.sdk.GetAchievement(which);
	}

	public static void getSteamAchievement(string which)
	{
		if (which.Equals("0"))
		{
			which = "a0";
		}
		getPlatformAchievement(which);
	}

	public static void getAchievement(int which, bool allowBroadcasting = true)
	{
		if (player.achievements.Contains(which) || gameMode != 3 || !achievements.TryGetValue(which, out var value))
		{
			return;
		}
		string achievementName = value.Split('^')[0];
		player.achievements.Add(which);
		if (which < 32 && allowBroadcasting)
		{
			if (stats.isSharedAchievement(which))
			{
				multiplayer.sendSharedAchievementMessage(which);
			}
			else
			{
				string text = player.Name;
				if (text == "")
				{
					text = TokenStringBuilder.LocalizedText("Strings\\UI:Chat_PlayerJoinedNewName");
				}
				multiplayer.globalChatInfoMessage("Achievement", text, TokenStringBuilder.AchievementName(which));
			}
		}
		playSound("achievement");
		addHUDMessage(HUDMessage.ForAchievement(achievementName));
		player.autoGenerateActiveDialogueEvent("achievement_" + which);
		getPlatformAchievement(which.ToString());
		if (!player.hasOrWillReceiveMail("hatter"))
		{
			addMailForTomorrow("hatter");
		}
	}

	public static void createMultipleObjectDebris(string id, int xTile, int yTile, int number)
	{
		for (int i = 0; i < number; i++)
		{
			createObjectDebris(id, xTile, yTile);
		}
	}

	public static void createMultipleObjectDebris(string id, int xTile, int yTile, int number, GameLocation location)
	{
		for (int i = 0; i < number; i++)
		{
			createObjectDebris(id, xTile, yTile, -1, 0, 1f, location);
		}
	}

	public static void createMultipleObjectDebris(string id, int xTile, int yTile, int number, float velocityMultiplier)
	{
		for (int i = 0; i < number; i++)
		{
			createObjectDebris(id, xTile, yTile, -1, 0, velocityMultiplier);
		}
	}

	public static void createMultipleObjectDebris(string id, int xTile, int yTile, int number, long who)
	{
		for (int i = 0; i < number; i++)
		{
			createObjectDebris(id, xTile, yTile, who);
		}
	}

	public static void createMultipleObjectDebris(string id, int xTile, int yTile, int number, long who, GameLocation location)
	{
		for (int i = 0; i < number; i++)
		{
			createObjectDebris(id, xTile, yTile, who, location);
		}
	}

	public static void createDebris(int debrisType, int xTile, int yTile, int numberOfChunks)
	{
		createDebris(debrisType, xTile, yTile, numberOfChunks, currentLocation);
	}

	public static void createDebris(int debrisType, int xTile, int yTile, int numberOfChunks, GameLocation location)
	{
		if (location == null)
		{
			location = currentLocation;
		}
		location.debris.Add(new Debris(debrisType, numberOfChunks, new Vector2(xTile * 64 + 32, yTile * 64 + 32), player.getStandingPosition()));
	}

	public static Debris createItemDebris(Item item, Vector2 pixelOrigin, int direction, GameLocation location = null, int groundLevel = -1, bool flopFish = false)
	{
		if (location == null)
		{
			location = currentLocation;
		}
		Vector2 targetLocation = new Vector2(pixelOrigin.X, pixelOrigin.Y);
		switch (direction)
		{
		case 0:
			pixelOrigin.Y -= 16f + (float)recentMultiplayerRandom.Next(32);
			targetLocation.Y -= 35.2f;
			break;
		case 1:
			pixelOrigin.X += 16f;
			pixelOrigin.Y -= 32 - recentMultiplayerRandom.Next(8);
			targetLocation.X += 128f;
			break;
		case 2:
			pixelOrigin.Y += recentMultiplayerRandom.Next(16);
			targetLocation.Y += 64f;
			break;
		case 3:
			pixelOrigin.X -= 16f;
			pixelOrigin.Y -= 32 - recentMultiplayerRandom.Next(8);
			targetLocation.X -= 128f;
			break;
		case -1:
			targetLocation = player.getStandingPosition();
			break;
		}
		Debris debris = new Debris(item, pixelOrigin, targetLocation);
		if (flopFish && item.Category == -4)
		{
			debris.floppingFish.Value = true;
		}
		if (groundLevel != -1)
		{
			debris.chunkFinalYLevel = groundLevel;
		}
		location.debris.Add(debris);
		return debris;
	}

	public static void createMultipleItemDebris(Item item, Vector2 pixelOrigin, int direction, GameLocation location = null, int groundLevel = -1, bool flopFish = false)
	{
		int stack = item.Stack;
		item.Stack = 1;
		createItemDebris(item, pixelOrigin, (direction == -1) ? random.Next(4) : direction, location, groundLevel, flopFish);
		for (int i = 1; i < stack; i++)
		{
			createItemDebris(item.getOne(), pixelOrigin, (direction == -1) ? random.Next(4) : direction, location, groundLevel, flopFish);
		}
	}

	public static void createRadialDebris(GameLocation location, int debrisType, int xTile, int yTile, int numberOfChunks, bool resource, int groundLevel = -1, bool item = false, Color? color = null)
	{
		if (groundLevel == -1)
		{
			groundLevel = yTile * 64 + 32;
		}
		Vector2 vector = new Vector2(xTile * 64 + 64, yTile * 64 + 64);
		if (item)
		{
			while (numberOfChunks > 0)
			{
				Vector2 vector2 = random.Next(4) switch
				{
					0 => new Vector2(-64f, 0f), 
					1 => new Vector2(64f, 0f), 
					2 => new Vector2(0f, 64f), 
					_ => new Vector2(0f, -64f), 
				};
				Item item2 = ItemRegistry.Create("(O)" + debrisType);
				location.debris.Add(new Debris(item2, vector, vector + vector2));
				numberOfChunks--;
			}
		}
		if (resource)
		{
			location.debris.Add(new Debris(debrisType, numberOfChunks / 4, vector, vector + new Vector2(-64f, 0f)));
			numberOfChunks++;
			location.debris.Add(new Debris(debrisType, numberOfChunks / 4, vector, vector + new Vector2(64f, 0f)));
			numberOfChunks++;
			location.debris.Add(new Debris(debrisType, numberOfChunks / 4, vector, vector + new Vector2(0f, -64f)));
			numberOfChunks++;
			location.debris.Add(new Debris(debrisType, numberOfChunks / 4, vector, vector + new Vector2(0f, 64f)));
		}
		else
		{
			location.debris.Add(new Debris(debrisType, numberOfChunks / 4, vector, vector + new Vector2(-64f, 0f), groundLevel, color));
			numberOfChunks++;
			location.debris.Add(new Debris(debrisType, numberOfChunks / 4, vector, vector + new Vector2(64f, 0f), groundLevel, color));
			numberOfChunks++;
			location.debris.Add(new Debris(debrisType, numberOfChunks / 4, vector, vector + new Vector2(0f, -64f), groundLevel, color));
			numberOfChunks++;
			location.debris.Add(new Debris(debrisType, numberOfChunks / 4, vector, vector + new Vector2(0f, 64f), groundLevel, color));
		}
	}

	public static void createRadialDebris(GameLocation location, string texture, Microsoft.Xna.Framework.Rectangle sourcerectangle, int xTile, int yTile, int numberOfChunks)
	{
		createRadialDebris(location, texture, sourcerectangle, xTile, yTile, numberOfChunks, yTile);
	}

	public static void createRadialDebris(GameLocation location, string texture, Microsoft.Xna.Framework.Rectangle sourcerectangle, int xTile, int yTile, int numberOfChunks, int groundLevelTile)
	{
		createRadialDebris(location, texture, sourcerectangle, 8, xTile * 64 + 32 + random.Next(32), yTile * 64 + 32 + random.Next(32), numberOfChunks, groundLevelTile);
	}

	public static void createRadialDebris(GameLocation location, string texture, Microsoft.Xna.Framework.Rectangle sourcerectangle, int sizeOfSourceRectSquares, int xPosition, int yPosition, int numberOfChunks, int groundLevelTile)
	{
		Vector2 vector = new Vector2(xPosition, yPosition);
		location.debris.Add(new Debris(texture, sourcerectangle, numberOfChunks / 4, vector, vector + new Vector2(-64f, 0f), groundLevelTile * 64, sizeOfSourceRectSquares));
		location.debris.Add(new Debris(texture, sourcerectangle, numberOfChunks / 4, vector, vector + new Vector2(64f, 0f), groundLevelTile * 64, sizeOfSourceRectSquares));
		location.debris.Add(new Debris(texture, sourcerectangle, numberOfChunks / 4, vector, vector + new Vector2(0f, -64f), groundLevelTile * 64, sizeOfSourceRectSquares));
		location.debris.Add(new Debris(texture, sourcerectangle, numberOfChunks / 4, vector, vector + new Vector2(0f, 64f), groundLevelTile * 64, sizeOfSourceRectSquares));
	}

	public static void createRadialDebris_MoreNatural(GameLocation location, string texture, Microsoft.Xna.Framework.Rectangle sourcerectangle, int sizeOfSourceRectSquares, int xPosition, int yPosition, int numberOfChunks, int groundLevel)
	{
		Vector2 vector = new Vector2(xPosition, yPosition);
		for (int i = 0; i < numberOfChunks; i++)
		{
			location.debris.Add(new Debris(texture, sourcerectangle, numberOfChunks / 4, vector, vector + new Vector2(random.Next(-64, 64), random.Next(-64, 64)), groundLevel + random.Next(-32, 32), sizeOfSourceRectSquares));
		}
	}

	public static void createRadialDebris(GameLocation location, string texture, Microsoft.Xna.Framework.Rectangle sourcerectangle, int sizeOfSourceRectSquares, int xPosition, int yPosition, int numberOfChunks, int groundLevelTile, Color color)
	{
		createRadialDebris(location, texture, sourcerectangle, sizeOfSourceRectSquares, xPosition, yPosition, numberOfChunks, groundLevelTile, color, 1f);
	}

	public static void createRadialDebris(GameLocation location, string texture, Microsoft.Xna.Framework.Rectangle sourcerectangle, int sizeOfSourceRectSquares, int xPosition, int yPosition, int numberOfChunks, int groundLevelTile, Color color, float scale)
	{
		Vector2 vector = new Vector2(xPosition, yPosition);
		while (numberOfChunks > 0)
		{
			switch (random.Next(4))
			{
			case 0:
			{
				Debris debris = new Debris(texture, sourcerectangle, 1, vector, vector + new Vector2(-64f, 0f), groundLevelTile * 64, sizeOfSourceRectSquares);
				debris.nonSpriteChunkColor.Value = color;
				location?.debris.Add(debris);
				debris.Chunks[0].scale = scale;
				break;
			}
			case 1:
			{
				Debris debris = new Debris(texture, sourcerectangle, 1, vector, vector + new Vector2(64f, 0f), groundLevelTile * 64, sizeOfSourceRectSquares);
				debris.nonSpriteChunkColor.Value = color;
				location?.debris.Add(debris);
				debris.Chunks[0].scale = scale;
				break;
			}
			case 2:
			{
				Debris debris = new Debris(texture, sourcerectangle, 1, vector, vector + new Vector2(random.Next(-64, 64), -64f), groundLevelTile * 64, sizeOfSourceRectSquares);
				debris.nonSpriteChunkColor.Value = color;
				location?.debris.Add(debris);
				debris.Chunks[0].scale = scale;
				break;
			}
			case 3:
			{
				Debris debris = new Debris(texture, sourcerectangle, 1, vector, vector + new Vector2(random.Next(-64, 64), 64f), groundLevelTile * 64, sizeOfSourceRectSquares);
				debris.nonSpriteChunkColor.Value = color;
				location?.debris.Add(debris);
				debris.Chunks[0].scale = scale;
				break;
			}
			}
			numberOfChunks--;
		}
	}

	public static void createObjectDebris(string id, int xTile, int yTile, long whichPlayer)
	{
		Farmer farmer = GetPlayer(whichPlayer) ?? player;
		currentLocation.debris.Add(new Debris(id, new Vector2(xTile * 64 + 32, yTile * 64 + 32), farmer.getStandingPosition()));
	}

	public static void createObjectDebris(string id, int xTile, int yTile, long whichPlayer, GameLocation location)
	{
		Farmer farmer = GetPlayer(whichPlayer) ?? player;
		location.debris.Add(new Debris(id, new Vector2(xTile * 64 + 32, yTile * 64 + 32), farmer.getStandingPosition()));
	}

	public static void createObjectDebris(string id, int xTile, int yTile, GameLocation location)
	{
		createObjectDebris(id, xTile, yTile, -1, 0, 1f, location);
	}

	public static void createObjectDebris(string id, int xTile, int yTile, int groundLevel = -1, int itemQuality = 0, float velocityMultiplyer = 1f, GameLocation location = null)
	{
		if (location == null)
		{
			location = currentLocation;
		}
		Debris debris = new Debris(id, new Vector2(xTile * 64 + 32, yTile * 64 + 32), player.getStandingPosition())
		{
			itemQuality = itemQuality
		};
		foreach (Chunk chunk in debris.Chunks)
		{
			chunk.xVelocity.Value *= velocityMultiplyer;
			chunk.yVelocity.Value *= velocityMultiplyer;
		}
		if (groundLevel != -1)
		{
			debris.chunkFinalYLevel = groundLevel;
		}
		location.debris.Add(debris);
	}

	[Obsolete("Use GetPlayer instead. Equivalent usage: `GetPlayer(id, onlineOnly: true) ?? Game1.MasterPlayer`.")]
	public static Farmer getFarmer(long id)
	{
		return GetPlayer(id, onlyOnline: true) ?? MasterPlayer;
	}

	[Obsolete("Use GetPlayer instead.")]
	public static Farmer getFarmerMaybeOffline(long id)
	{
		return GetPlayer(id);
	}

	/// <summary>Get the player matching a unique multiplayer ID, if it's valid.</summary>
	/// <param name="id">The unique multiplayer ID.</param>
	/// <param name="onlyOnline">Whether to only check online players.</param>
	/// <returns>Returns the matching player, or <c>null</c> if the ID isn't valid.</returns>
	public static Farmer? GetPlayer(long id, bool onlyOnline = false)
	{
		if (MasterPlayer.UniqueMultiplayerID == id)
		{
			return MasterPlayer;
		}
		if (otherFarmers.TryGetValue(id, out var value))
		{
			return value;
		}
		if (!onlyOnline && netWorldState.Value.farmhandData.TryGetValue(id, out var value2))
		{
			return value2;
		}
		return null;
	}

	/// <summary>Get all players including the host, online farmhands, and offline farmhands.</summary>
	public static IEnumerable<Farmer> getAllFarmers()
	{
		return Enumerable.Repeat(MasterPlayer, 1).Concat(getAllFarmhands());
	}

	/// <summary>Get all players who are currently connected, including the host player.</summary>
	public static FarmerCollection getOnlineFarmers()
	{
		return _onlineFarmers;
	}

	/// <summary>Get online and offline farmhands.</summary>
	public static IEnumerable<Farmer> getAllFarmhands()
	{
		foreach (Farmer value in netWorldState.Value.farmhandData.Values)
		{
			if (value.isActive())
			{
				yield return otherFarmers[value.UniqueMultiplayerID];
			}
			else
			{
				yield return value;
			}
		}
	}

	/// <summary>Get farmhands which aren't currently connected.</summary>
	public static IEnumerable<Farmer> getOfflineFarmhands()
	{
		foreach (Farmer value in netWorldState.Value.farmhandData.Values)
		{
			if (!value.isActive())
			{
				yield return value;
			}
		}
	}

	public static void farmerFindsArtifact(string itemId)
	{
		Item item = ItemRegistry.Create(itemId);
		player.addItemToInventoryBool(item);
	}

	public static bool doesHUDMessageExist(string s)
	{
		for (int i = 0; i < hudMessages.Count; i++)
		{
			if (s.Equals(hudMessages[i].message))
			{
				return true;
			}
		}
		return false;
	}

	public static void addHUDMessage(HUDMessage message)
	{
		if (message.type != null || message.whatType != 0)
		{
			for (int i = 0; i < hudMessages.Count; i++)
			{
				if (message.type != null && message.type == hudMessages[i].type)
				{
					hudMessages[i].number = hudMessages[i].number + message.number;
					hudMessages[i].timeLeft = 3500f;
					hudMessages[i].transparency = 1f;
					if (hudMessages[i].number > 50000)
					{
						HUDMessage.numbersEasterEgg(hudMessages[i].number);
					}
					return;
				}
				if (message.whatType == hudMessages[i].whatType && message.whatType != 1 && message.message != null && message.message.Equals(hudMessages[i].message))
				{
					hudMessages[i].timeLeft = message.timeLeft;
					hudMessages[i].transparency = 1f;
					return;
				}
			}
		}
		hudMessages.Add(message);
		for (int num = hudMessages.Count - 1; num >= 0; num--)
		{
			if (hudMessages[num].noIcon)
			{
				HUDMessage item = hudMessages[num];
				hudMessages.RemoveAt(num);
				hudMessages.Add(item);
			}
		}
	}

	public static void showSwordswipeAnimation(int direction, Vector2 source, float animationSpeed, bool flip)
	{
		switch (direction)
		{
		case 0:
			currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(-1, animationSpeed, 5, 1, new Vector2(source.X + 32f, source.Y), flicker: false, flipped: false, !flip, -(float)Math.PI / 2f));
			break;
		case 1:
			currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(-1, animationSpeed, 5, 1, new Vector2(source.X + 96f + 16f, source.Y + 48f), flicker: false, flip, verticalFlipped: false, flip ? (-(float)Math.PI) : 0f));
			break;
		case 2:
			currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(-1, animationSpeed, 5, 1, new Vector2(source.X + 32f, source.Y + 128f), flicker: false, flipped: false, !flip, (float)Math.PI / 2f));
			break;
		case 3:
			currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(-1, animationSpeed, 5, 1, new Vector2(source.X - 32f - 16f, source.Y + 48f), flicker: false, !flip, verticalFlipped: false, flip ? (-(float)Math.PI) : 0f));
			break;
		}
	}

	public static void removeDebris(Debris.DebrisType type)
	{
		currentLocation.debris.RemoveWhere((Debris debris) => debris.debrisType.Value == type);
	}

	public static void toolAnimationDone(Farmer who)
	{
		float stamina = player.Stamina;
		if (who.CurrentTool == null)
		{
			return;
		}
		if (who.Stamina > 0f)
		{
			int power = 1;
			Vector2 toolLocation = who.GetToolLocation();
			if (who.CurrentTool is FishingRod { isFishing: not false })
			{
				who.canReleaseTool = false;
			}
			else if (!(who.CurrentTool is FishingRod))
			{
				who.UsingTool = false;
				if (who.CurrentTool.QualifiedItemId == "(T)WateringCan")
				{
					switch (who.FacingDirection)
					{
					case 0:
					case 2:
						who.CurrentTool.DoFunction(currentLocation, (int)toolLocation.X, (int)toolLocation.Y, power, who);
						break;
					case 1:
					case 3:
						who.CurrentTool.DoFunction(currentLocation, (int)toolLocation.X, (int)toolLocation.Y, power, who);
						break;
					}
				}
				else if (who.CurrentTool is MeleeWeapon)
				{
					who.CurrentTool.CurrentParentTileIndex = who.CurrentTool.IndexOfMenuItemView;
				}
				else
				{
					if (who.CurrentTool.QualifiedItemId == "(T)ReturnScepter")
					{
						who.CurrentTool.CurrentParentTileIndex = who.CurrentTool.IndexOfMenuItemView;
					}
					who.CurrentTool.DoFunction(currentLocation, (int)toolLocation.X, (int)toolLocation.Y, power, who);
				}
			}
			else
			{
				who.UsingTool = false;
			}
		}
		else if (who.CurrentTool.instantUse.Value)
		{
			who.CurrentTool.DoFunction(currentLocation, 0, 0, 0, who);
		}
		else
		{
			who.UsingTool = false;
		}
		who.lastClick = Vector2.Zero;
		if (who.IsLocalPlayer && !GetKeyboardState().IsKeyDown(Keys.LeftShift))
		{
			who.setRunning(options.autoRun);
		}
		if (!who.UsingTool && who.FarmerSprite.PauseForSingleAnimation)
		{
			who.FarmerSprite.StopAnimation();
		}
		if (player.Stamina <= 0f && stamina > 0f)
		{
			player.doEmote(36);
		}
	}

	public static bool pressActionButton(KeyboardState currentKBState, MouseState currentMouseState, GamePadState currentPadState)
	{
		if (IsChatting)
		{
			currentKBState = default(KeyboardState);
		}
		if (dialogueTyping)
		{
			bool flag = true;
			dialogueTyping = false;
			if (currentSpeaker != null)
			{
				currentDialogueCharacterIndex = currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue().Length;
			}
			else if (currentObjectDialogue.Count > 0)
			{
				currentDialogueCharacterIndex = currentObjectDialogue.Peek().Length;
			}
			else
			{
				flag = false;
			}
			dialogueTypingInterval = 0;
			oldKBState = currentKBState;
			oldMouseState = input.GetMouseState();
			oldPadState = currentPadState;
			if (flag)
			{
				playSound("dialogueCharacterClose");
				return false;
			}
		}
		if (dialogueUp)
		{
			if (isQuestion)
			{
				isQuestion = false;
				if (currentSpeaker != null)
				{
					if (currentSpeaker.CurrentDialogue.Peek().chooseResponse(questionChoices[currentQuestionChoice]))
					{
						currentDialogueCharacterIndex = 1;
						dialogueTyping = true;
						oldKBState = currentKBState;
						oldMouseState = input.GetMouseState();
						oldPadState = currentPadState;
						return false;
					}
				}
				else
				{
					dialogueUp = false;
					if (eventUp && currentLocation.afterQuestion == null)
					{
						currentLocation.currentEvent.answerDialogue(currentLocation.lastQuestionKey, currentQuestionChoice);
						currentQuestionChoice = 0;
						oldKBState = currentKBState;
						oldMouseState = input.GetMouseState();
						oldPadState = currentPadState;
					}
					else if (currentLocation.answerDialogue(questionChoices[currentQuestionChoice]))
					{
						currentQuestionChoice = 0;
						oldKBState = currentKBState;
						oldMouseState = input.GetMouseState();
						oldPadState = currentPadState;
						return false;
					}
					if (dialogueUp)
					{
						currentDialogueCharacterIndex = 1;
						dialogueTyping = true;
						oldKBState = currentKBState;
						oldMouseState = input.GetMouseState();
						oldPadState = currentPadState;
						return false;
					}
				}
				currentQuestionChoice = 0;
			}
			string text = null;
			if (currentSpeaker != null)
			{
				if (currentSpeaker.immediateSpeak)
				{
					currentSpeaker.immediateSpeak = false;
					return false;
				}
				text = ((currentSpeaker.CurrentDialogue.Count > 0) ? currentSpeaker.CurrentDialogue.Peek().exitCurrentDialogue() : null);
			}
			if (text == null)
			{
				if (currentSpeaker != null && currentSpeaker.CurrentDialogue.Count > 0 && currentSpeaker.CurrentDialogue.Peek().isOnFinalDialogue() && currentSpeaker.CurrentDialogue.Count > 0)
				{
					currentSpeaker.CurrentDialogue.Pop();
				}
				dialogueUp = false;
				if (messagePause)
				{
					pauseTime = 500f;
				}
				if (currentObjectDialogue.Count > 0)
				{
					currentObjectDialogue.Dequeue();
				}
				currentDialogueCharacterIndex = 0;
				if (currentObjectDialogue.Count > 0)
				{
					dialogueUp = true;
					questionChoices.Clear();
					oldKBState = currentKBState;
					oldMouseState = input.GetMouseState();
					oldPadState = currentPadState;
					dialogueTyping = true;
					return false;
				}
				if (currentSpeaker != null && !currentSpeaker.Name.Equals("Gunther") && !eventUp && !currentSpeaker.doingEndOfRouteAnimation.Value)
				{
					currentSpeaker.doneFacingPlayer(player);
				}
				currentSpeaker = null;
				if (!eventUp)
				{
					player.CanMove = true;
				}
				else if (currentLocation.currentEvent.CurrentCommand > 0 || currentLocation.currentEvent.specialEventVariable1)
				{
					if (!isFestival() || !currentLocation.currentEvent.canMoveAfterDialogue())
					{
						currentLocation.currentEvent.CurrentCommand++;
					}
					else
					{
						player.CanMove = true;
					}
				}
				questionChoices.Clear();
				playSound("smallSelect");
			}
			else
			{
				playSound("smallSelect");
				currentDialogueCharacterIndex = 0;
				dialogueTyping = true;
				checkIfDialogueIsQuestion();
			}
			oldKBState = currentKBState;
			oldMouseState = input.GetMouseState();
			oldPadState = currentPadState;
			return false;
		}
		if (!player.UsingTool && (!eventUp || (currentLocation.currentEvent != null && currentLocation.currentEvent.playerControlSequence)) && !fadeToBlack)
		{
			if (wasMouseVisibleThisFrame && currentLocation.animals.Length > 0)
			{
				Vector2 position = new Vector2(getOldMouseX() + viewport.X, getOldMouseY() + viewport.Y);
				if (Utility.withinRadiusOfPlayer((int)position.X, (int)position.Y, 1, player))
				{
					if (currentLocation.CheckPetAnimal(position, player))
					{
						return true;
					}
					if (didPlayerJustRightClick(ignoreNonMouseHeldInput: true) && currentLocation.CheckInspectAnimal(position, player))
					{
						return true;
					}
				}
			}
			Vector2 vector = new Vector2(getOldMouseX() + viewport.X, getOldMouseY() + viewport.Y) / 64f;
			Vector2 vector2 = vector;
			bool flag2 = false;
			if (!wasMouseVisibleThisFrame || mouseCursorTransparency == 0f || !Utility.tileWithinRadiusOfPlayer((int)vector.X, (int)vector.Y, 1, player))
			{
				vector = player.GetGrabTile();
				flag2 = true;
			}
			bool flag3 = false;
			if (eventUp && !isFestival())
			{
				CurrentEvent?.receiveActionPress((int)vector.X, (int)vector.Y);
				oldKBState = currentKBState;
				oldMouseState = input.GetMouseState();
				oldPadState = currentPadState;
				return false;
			}
			if (tryToCheckAt(vector, player))
			{
				return false;
			}
			if (player.isRidingHorse())
			{
				player.mount.checkAction(player, player.currentLocation);
				return false;
			}
			if (!player.canMove)
			{
				return false;
			}
			if (!flag3 && player.currentLocation.isCharacterAtTile(vector) != null)
			{
				flag3 = true;
			}
			bool flag4 = false;
			if (player.ActiveObject != null && !(player.ActiveObject is Furniture))
			{
				if (player.ActiveObject.performUseAction(currentLocation))
				{
					player.reduceActiveItemByOne();
					oldKBState = currentKBState;
					oldMouseState = input.GetMouseState();
					oldPadState = currentPadState;
					return false;
				}
				int stack = player.ActiveObject.Stack;
				isCheckingNonMousePlacement = !IsPerformingMousePlacement();
				if (flag2)
				{
					isCheckingNonMousePlacement = true;
				}
				if (isOneOfTheseKeysDown(currentKBState, options.actionButton))
				{
					isCheckingNonMousePlacement = true;
				}
				Vector2 nearbyValidPlacementPosition = Utility.GetNearbyValidPlacementPosition(player, currentLocation, player.ActiveObject, (int)vector.X * 64 + 32, (int)vector.Y * 64 + 32);
				if (!isCheckingNonMousePlacement && player.ActiveObject is Wallpaper && Utility.tryToPlaceItem(currentLocation, player.ActiveObject, (int)vector2.X * 64, (int)vector2.Y * 64))
				{
					isCheckingNonMousePlacement = false;
					return true;
				}
				if (Utility.tryToPlaceItem(currentLocation, player.ActiveObject, (int)nearbyValidPlacementPosition.X, (int)nearbyValidPlacementPosition.Y))
				{
					isCheckingNonMousePlacement = false;
					return true;
				}
				if (!eventUp && (player.ActiveObject == null || player.ActiveObject.Stack < stack || player.ActiveObject.isPlaceable()))
				{
					flag4 = true;
				}
				isCheckingNonMousePlacement = false;
			}
			if (!flag4 && !flag3)
			{
				vector.Y += 1f;
				if (player.FacingDirection >= 0 && player.FacingDirection <= 3)
				{
					Vector2 value = vector - player.Tile;
					if (value.X > 0f || value.Y > 0f)
					{
						value.Normalize();
					}
					if (Vector2.Dot(Utility.DirectionsTileVectors[player.FacingDirection], value) >= 0f && tryToCheckAt(vector, player))
					{
						return false;
					}
				}
				if (!eventUp && player.ActiveObject is Furniture furniture)
				{
					furniture.rotate();
					playSound("dwoop");
					oldKBState = currentKBState;
					oldMouseState = input.GetMouseState();
					oldPadState = currentPadState;
					return false;
				}
				vector.Y -= 2f;
				if (player.FacingDirection >= 0 && player.FacingDirection <= 3 && !flag3)
				{
					Vector2 value2 = vector - player.Tile;
					if (value2.X > 0f || value2.Y > 0f)
					{
						value2.Normalize();
					}
					if (Vector2.Dot(Utility.DirectionsTileVectors[player.FacingDirection], value2) >= 0f && tryToCheckAt(vector, player))
					{
						return false;
					}
				}
				if (!eventUp && player.ActiveObject is Furniture furniture2)
				{
					furniture2.rotate();
					playSound("dwoop");
					oldKBState = currentKBState;
					oldMouseState = input.GetMouseState();
					oldPadState = currentPadState;
					return false;
				}
				vector = player.Tile;
				if (tryToCheckAt(vector, player))
				{
					return false;
				}
				if (!eventUp && player.ActiveObject is Furniture furniture3)
				{
					furniture3.rotate();
					playSound("dwoop");
					oldKBState = currentKBState;
					oldMouseState = input.GetMouseState();
					oldPadState = currentPadState;
					return false;
				}
			}
			if (!player.isEating && player.ActiveObject != null && !dialogueUp && !eventUp && !player.canOnlyWalk && !player.FarmerSprite.PauseForSingleAnimation && !fadeToBlack && player.ActiveObject.Edibility != -300 && didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
			{
				if (player.team.SpecialOrderRuleActive("SC_NO_FOOD"))
				{
					MineShaft obj = player.currentLocation as MineShaft;
					if (obj != null && obj.getMineArea() == 121)
					{
						addHUDMessage(new HUDMessage(content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"), 3));
						return false;
					}
				}
				if (player.hasBuff("25") && player.ActiveObject != null && !player.ActiveObject.HasContextTag("ginger_item"))
				{
					addHUDMessage(new HUDMessage(content.LoadString("Strings\\StringsFromCSFiles:Nauseous_CantEat"), 3));
					return false;
				}
				player.faceDirection(2);
				player.itemToEat = player.ActiveObject;
				player.FarmerSprite.setCurrentSingleAnimation(304);
				if (objectData.TryGetValue(player.ActiveObject.ItemId, out var value3))
				{
					currentLocation.createQuestionDialogue((value3.IsDrink && player.ActiveObject.preserve.Value != Object.PreserveType.Pickle) ? content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3159", player.ActiveObject.DisplayName) : content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3160", player.ActiveObject.DisplayName), currentLocation.createYesNoResponses(), "Eat");
				}
				oldKBState = currentKBState;
				oldMouseState = input.GetMouseState();
				oldPadState = currentPadState;
				return false;
			}
		}
		if (player.CurrentTool is MeleeWeapon && player.CanMove && !player.canOnlyWalk && !eventUp && !player.onBridge.Value && didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
		{
			((MeleeWeapon)player.CurrentTool).animateSpecialMove(player);
			return false;
		}
		return true;
	}

	public static bool IsPerformingMousePlacement()
	{
		if (mouseCursorTransparency == 0f || !wasMouseVisibleThisFrame || (!lastCursorMotionWasMouse && (player.ActiveObject == null || (!player.ActiveObject.isPlaceable() && player.ActiveObject.Category != -74 && !player.ActiveObject.isSapling()))))
		{
			return false;
		}
		return true;
	}

	public static Vector2 GetPlacementGrabTile()
	{
		if (!IsPerformingMousePlacement())
		{
			return player.GetGrabTile();
		}
		return new Vector2(getOldMouseX() + viewport.X, getOldMouseY() + viewport.Y) / 64f;
	}

	public static bool tryToCheckAt(Vector2 grabTile, Farmer who)
	{
		if (player.onBridge.Value)
		{
			return false;
		}
		haltAfterCheck = true;
		if (Utility.tileWithinRadiusOfPlayer((int)grabTile.X, (int)grabTile.Y, 1, player) && hooks.OnGameLocation_CheckAction(currentLocation, new Location((int)grabTile.X, (int)grabTile.Y), viewport, who, () => currentLocation.checkAction(new Location((int)grabTile.X, (int)grabTile.Y), viewport, who)))
		{
			updateCursorTileHint();
			who.lastGrabTile = grabTile;
			if (who.CanMove && haltAfterCheck)
			{
				who.faceGeneralDirection(grabTile * 64f);
				who.Halt();
			}
			oldKBState = GetKeyboardState();
			oldMouseState = input.GetMouseState();
			oldPadState = input.GetGamePadState();
			return true;
		}
		return false;
	}

	public static void pressSwitchToolButton()
	{
		if (player.netItemStowed.Value)
		{
			player.netItemStowed.Set(newValue: false);
			player.UpdateItemStow();
		}
		int num = ((input.GetMouseState().ScrollWheelValue > oldMouseState.ScrollWheelValue) ? (-1) : ((input.GetMouseState().ScrollWheelValue < oldMouseState.ScrollWheelValue) ? 1 : 0));
		if (options.gamepadControls && num == 0)
		{
			if (input.GetGamePadState().IsButtonDown(Buttons.LeftTrigger))
			{
				num = -1;
			}
			else if (input.GetGamePadState().IsButtonDown(Buttons.RightTrigger))
			{
				num = 1;
			}
		}
		if (options.invertScrollDirection)
		{
			num *= -1;
		}
		if (num == 0)
		{
			return;
		}
		player.CurrentToolIndex = (player.CurrentToolIndex + num) % 12;
		if (player.CurrentToolIndex < 0)
		{
			player.CurrentToolIndex = 11;
		}
		for (int i = 0; i < 12; i++)
		{
			if (player.CurrentItem != null)
			{
				break;
			}
			player.CurrentToolIndex = (num + player.CurrentToolIndex) % 12;
			if (player.CurrentToolIndex < 0)
			{
				player.CurrentToolIndex = 11;
			}
		}
		playSound("toolSwap");
		if (player.ActiveObject != null)
		{
			player.showCarrying();
		}
		else
		{
			player.showNotCarrying();
		}
	}

	public static bool pressUseToolButton()
	{
		bool didInitiateItemStow = game1._didInitiateItemStow;
		game1._didInitiateItemStow = false;
		if (fadeToBlack)
		{
			return false;
		}
		player.toolPower.Value = 0;
		player.toolHold.Value = 0;
		bool flag = false;
		if (player.CurrentTool == null && player.ActiveObject == null)
		{
			Vector2 key = player.GetToolLocation() / 64f;
			key.X = (int)key.X;
			key.Y = (int)key.Y;
			if (currentLocation.Objects.TryGetValue(key, out var value) && !value.readyForHarvest.Value && value.heldObject.Value == null && !(value is Fence) && !(value is CrabPot) && (value.Type == "Crafting" || value.Type == "interactive") && !value.IsTwig())
			{
				flag = true;
				value.setHealth(value.getHealth() - 1);
				value.shakeTimer = 300;
				value.playNearbySoundAll("hammer");
				if (value.getHealth() < 2)
				{
					value.playNearbySoundAll("hammer");
					if (value.getHealth() < 1)
					{
						Tool tool = ItemRegistry.Create<Tool>("(T)Pickaxe");
						tool.DoFunction(currentLocation, -1, -1, 0, player);
						if (value.performToolAction(tool))
						{
							value.performRemoveAction();
							if (value.Type == "Crafting" && value.fragility.Value != 2)
							{
								currentLocation.debris.Add(new Debris(value.QualifiedItemId, player.GetToolLocation(), Utility.PointToVector2(player.StandingPixel)));
							}
							currentLocation.Objects.Remove(key);
							return true;
						}
					}
				}
			}
		}
		if (currentMinigame == null && !player.UsingTool && (player.IsSitting() || player.isRidingHorse() || player.onBridge.Value || dialogueUp || (eventUp && !CurrentEvent.canPlayerUseTool() && (!currentLocation.currentEvent.playerControlSequence || (activeClickableMenu == null && currentMinigame == null))) || (player.CurrentTool != null && (currentLocation.doesPositionCollideWithCharacter(Utility.getRectangleCenteredAt(player.GetToolLocation(), 64), ignoreMonsters: true)?.IsVillager ?? false))))
		{
			pressActionButton(GetKeyboardState(), input.GetMouseState(), input.GetGamePadState());
			return false;
		}
		if (player.canOnlyWalk)
		{
			return true;
		}
		Vector2 position = ((!wasMouseVisibleThisFrame) ? player.GetToolLocation() : new Vector2(getOldMouseX() + viewport.X, getOldMouseY() + viewport.Y));
		if (Utility.canGrabSomethingFromHere((int)position.X, (int)position.Y, player))
		{
			Vector2 tile = new Vector2(position.X / 64f, position.Y / 64f);
			if (hooks.OnGameLocation_CheckAction(currentLocation, new Location((int)tile.X, (int)tile.Y), viewport, player, () => currentLocation.checkAction(new Location((int)tile.X, (int)tile.Y), viewport, player)))
			{
				updateCursorTileHint();
				return true;
			}
			if (currentLocation.terrainFeatures.TryGetValue(tile, out var value2))
			{
				value2.performUseAction(tile);
				return true;
			}
			return false;
		}
		if (currentLocation.leftClick((int)position.X, (int)position.Y, player))
		{
			return true;
		}
		isCheckingNonMousePlacement = !IsPerformingMousePlacement();
		if (player.ActiveObject != null)
		{
			if (options.allowStowing && CanPlayerStowItem(GetPlacementGrabTile()))
			{
				if (didPlayerJustLeftClick() || didInitiateItemStow)
				{
					game1._didInitiateItemStow = true;
					playSound("stoneStep");
					player.netItemStowed.Set(newValue: true);
					return true;
				}
				return true;
			}
			if (Utility.withinRadiusOfPlayer((int)position.X, (int)position.Y, 1, player) && hooks.OnGameLocation_CheckAction(currentLocation, new Location((int)position.X / 64, (int)position.Y / 64), viewport, player, () => currentLocation.checkAction(new Location((int)position.X / 64, (int)position.Y / 64), viewport, player)))
			{
				return true;
			}
			Vector2 placementGrabTile = GetPlacementGrabTile();
			Vector2 nearbyValidPlacementPosition = Utility.GetNearbyValidPlacementPosition(player, currentLocation, player.ActiveObject, (int)placementGrabTile.X * 64, (int)placementGrabTile.Y * 64);
			if (Utility.tryToPlaceItem(currentLocation, player.ActiveObject, (int)nearbyValidPlacementPosition.X, (int)nearbyValidPlacementPosition.Y))
			{
				isCheckingNonMousePlacement = false;
				return true;
			}
			isCheckingNonMousePlacement = false;
		}
		if (currentLocation.LowPriorityLeftClick((int)position.X, (int)position.Y, player))
		{
			return true;
		}
		if (options.allowStowing && player.netItemStowed.Value && !flag && (didInitiateItemStow || didPlayerJustLeftClick(ignoreNonMouseHeldInput: true)))
		{
			game1._didInitiateItemStow = true;
			playSound("toolSwap");
			player.netItemStowed.Set(newValue: false);
			return true;
		}
		if (player.UsingTool)
		{
			player.lastClick = new Vector2((int)position.X, (int)position.Y);
			player.CurrentTool.DoFunction(player.currentLocation, (int)player.lastClick.X, (int)player.lastClick.Y, 1, player);
			return true;
		}
		if (player.ActiveObject == null && !player.isEating && player.CurrentTool != null)
		{
			if (player.Stamina <= 20f && player.CurrentTool != null && !(player.CurrentTool is MeleeWeapon) && !eventUp)
			{
				staminaShakeTimer = 1000;
				for (int num = 0; num < 4; num++)
				{
					uiOverlayTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(366, 412, 5, 6), new Vector2(random.Next(32) + uiViewport.Width - 56, uiViewport.Height - 224 - 16 - (int)((double)(player.MaxStamina - 270) * 0.715)), flipped: false, 0.012f, Color.SkyBlue)
					{
						motion = new Vector2(-2f, -10f),
						acceleration = new Vector2(0f, 0.5f),
						local = true,
						scale = 4 + random.Next(-1, 0),
						delayBeforeAnimationStart = num * 30
					});
				}
			}
			if (!(player.CurrentTool is MeleeWeapon) || didPlayerJustLeftClick(ignoreNonMouseHeldInput: true))
			{
				int facingDirection = player.FacingDirection;
				Vector2 toolLocation = player.GetToolLocation(position);
				player.FacingDirection = player.getGeneralDirectionTowards(new Vector2((int)toolLocation.X, (int)toolLocation.Y));
				player.lastClick = new Vector2((int)position.X, (int)position.Y);
				player.BeginUsingTool();
				if (!player.usingTool.Value)
				{
					player.FacingDirection = facingDirection;
				}
				else if (player.FarmerSprite.IsPlayingBasicAnimation(facingDirection, carrying: true) || player.FarmerSprite.IsPlayingBasicAnimation(facingDirection, carrying: false))
				{
					player.FarmerSprite.StopAnimation();
				}
			}
		}
		return false;
	}

	public static bool CanPlayerStowItem(Vector2 position)
	{
		if (player.ActiveObject == null)
		{
			return false;
		}
		if (player.ActiveObject.bigCraftable.Value)
		{
			return false;
		}
		Object activeObject = player.ActiveObject;
		if (!(activeObject is Furniture))
		{
			if (activeObject != null && (player.ActiveObject.Category == -74 || player.ActiveObject.Category == -19))
			{
				Vector2 nearbyValidPlacementPosition = Utility.GetNearbyValidPlacementPosition(player, currentLocation, player.ActiveObject, (int)position.X * 64, (int)position.Y * 64);
				if (Utility.playerCanPlaceItemHere(player.currentLocation, player.ActiveObject, (int)nearbyValidPlacementPosition.X, (int)nearbyValidPlacementPosition.Y, player) && (!player.ActiveObject.isSapling() || IsPerformingMousePlacement()))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public static int getMouseXRaw()
	{
		return input.GetMouseState().X;
	}

	public static int getMouseYRaw()
	{
		return input.GetMouseState().Y;
	}

	public static bool IsOnMainThread()
	{
		if (Thread.CurrentThread != null)
		{
			return !Thread.CurrentThread.IsBackground;
		}
		return false;
	}

	public static void PushUIMode()
	{
		if (!IsOnMainThread())
		{
			return;
		}
		uiModeCount++;
		if (uiModeCount <= 0 || uiMode)
		{
			return;
		}
		uiMode = true;
		if (game1.isDrawing && IsOnMainThread())
		{
			if (game1.uiScreen != null && !game1.uiScreen.IsDisposed)
			{
				RenderTargetBinding[] renderTargets = graphics.GraphicsDevice.GetRenderTargets();
				if (renderTargets.Length != 0)
				{
					nonUIRenderTarget = renderTargets[0].RenderTarget as RenderTarget2D;
				}
				else
				{
					nonUIRenderTarget = null;
				}
				SetRenderTarget(game1.uiScreen);
			}
			if (isRenderingScreenBuffer)
			{
				SetRenderTarget(null);
			}
		}
		xTile.Dimensions.Rectangle rectangle = new xTile.Dimensions.Rectangle(0, 0, (int)Math.Ceiling((float)viewport.Width * options.zoomLevel / options.uiScale), (int)Math.Ceiling((float)viewport.Height * options.zoomLevel / options.uiScale));
		rectangle.X = viewport.X;
		rectangle.Y = viewport.Y;
		uiViewport = rectangle;
	}

	public static void PopUIMode()
	{
		if (!IsOnMainThread())
		{
			return;
		}
		uiModeCount--;
		if (uiModeCount > 0 || !uiMode)
		{
			return;
		}
		if (game1.isDrawing)
		{
			if (graphics.GraphicsDevice.GetRenderTargets().Length != 0 && graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget == game1.uiScreen)
			{
				if (nonUIRenderTarget != null && !nonUIRenderTarget.IsDisposed)
				{
					SetRenderTarget(nonUIRenderTarget);
				}
				else
				{
					SetRenderTarget(null);
				}
			}
			if (isRenderingScreenBuffer)
			{
				SetRenderTarget(null);
			}
		}
		nonUIRenderTarget = null;
		uiMode = false;
	}

	public static void SetRenderTarget(RenderTarget2D target)
	{
		if (!isRenderingScreenBuffer && IsOnMainThread())
		{
			graphics.GraphicsDevice.SetRenderTarget(target);
		}
	}

	public static void InUIMode(Action action)
	{
		PushUIMode();
		try
		{
			action();
		}
		finally
		{
			PopUIMode();
		}
	}

	public static void StartWorldDrawInUI(SpriteBatch b)
	{
		_oldUIModeCount = 0;
		if (uiMode)
		{
			_oldUIModeCount = uiModeCount;
			b?.End();
			while (uiModeCount > 0)
			{
				PopUIMode();
			}
			b?.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		}
	}

	public static void EndWorldDrawInUI(SpriteBatch b)
	{
		if (_oldUIModeCount > 0)
		{
			b?.End();
			for (int i = 0; i < _oldUIModeCount; i++)
			{
				PushUIMode();
			}
			b?.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		}
		_oldUIModeCount = 0;
	}

	public static int getMouseX()
	{
		return getMouseX(uiMode);
	}

	public static int getMouseX(bool ui_scale)
	{
		if (ui_scale)
		{
			return (int)((float)input.GetMouseState().X / options.uiScale);
		}
		return (int)((float)input.GetMouseState().X * (1f / options.zoomLevel));
	}

	public static int getOldMouseX()
	{
		return getOldMouseX(uiMode);
	}

	public static int getOldMouseX(bool ui_scale)
	{
		if (ui_scale)
		{
			return (int)((float)oldMouseState.X / options.uiScale);
		}
		return (int)((float)oldMouseState.X * (1f / options.zoomLevel));
	}

	public static int getMouseY()
	{
		return getMouseY(uiMode);
	}

	public static int getMouseY(bool ui_scale)
	{
		if (ui_scale)
		{
			return (int)((float)input.GetMouseState().Y / options.uiScale);
		}
		return (int)((float)input.GetMouseState().Y * (1f / options.zoomLevel));
	}

	public static int getOldMouseY()
	{
		return getOldMouseY(uiMode);
	}

	public static int getOldMouseY(bool ui_scale)
	{
		if (ui_scale)
		{
			return (int)((float)oldMouseState.Y / options.uiScale);
		}
		return (int)((float)oldMouseState.Y * (1f / options.zoomLevel));
	}

	public static bool PlayEvent(string eventId, GameLocation location, out bool validEvent, bool checkPreconditions = true, bool checkSeen = true)
	{
		string eventAssetName;
		Dictionary<string, string> locationEvents;
		try
		{
			if (!location.TryGetLocationEvents(out eventAssetName, out locationEvents))
			{
				validEvent = false;
				return false;
			}
		}
		catch
		{
			validEvent = false;
			return false;
		}
		if (locationEvents == null)
		{
			validEvent = false;
			return false;
		}
		foreach (string key in locationEvents.Keys)
		{
			if (!(key.Split('/')[0] == eventId))
			{
				continue;
			}
			validEvent = true;
			if (checkSeen && (player.eventsSeen.Contains(eventId) || eventsSeenSinceLastLocationChange.Contains(eventId)))
			{
				return false;
			}
			string id = eventId;
			if (checkPreconditions)
			{
				id = location.checkEventPrecondition(key, check_seen: false);
			}
			if (!string.IsNullOrEmpty(id) && id != "-1")
			{
				if (location.Name != currentLocation.Name)
				{
					LocationRequest obj2 = getLocationRequest(location.Name);
					obj2.OnLoad += delegate
					{
						currentLocation.currentEvent = new Event(locationEvents[key], eventAssetName, id);
					};
					int x = 8;
					int y = 8;
					Utility.getDefaultWarpLocation(obj2.Name, ref x, ref y);
					warpFarmer(obj2, x, y, player.FacingDirection);
				}
				else
				{
					globalFadeToBlack(delegate
					{
						forceSnapOnNextViewportUpdate = true;
						currentLocation.startEvent(new Event(locationEvents[key], eventAssetName, id));
						globalFadeToClear();
					});
				}
				return true;
			}
			return false;
		}
		validEvent = false;
		return false;
	}

	public static bool PlayEvent(string eventId, bool checkPreconditions = true, bool checkSeen = true)
	{
		if (checkSeen && (player.eventsSeen.Contains(eventId) || eventsSeenSinceLastLocationChange.Contains(eventId)))
		{
			return false;
		}
		if (PlayEvent(eventId, currentLocation, out var validEvent, checkPreconditions, checkSeen))
		{
			return true;
		}
		if (validEvent)
		{
			return false;
		}
		foreach (GameLocation location in locations)
		{
			if (location != currentLocation)
			{
				if (PlayEvent(eventId, location, out validEvent, checkPreconditions, checkSeen))
				{
					return true;
				}
				if (validEvent)
				{
					return false;
				}
			}
		}
		return false;
	}

	public static int numberOfPlayers()
	{
		return _onlineFarmers.Count;
	}

	public static bool isFestival()
	{
		return currentLocation?.currentEvent?.isFestival ?? false;
	}

	/// <summary>Parse a raw debug command and run it if it's valid.</summary>
	/// <param name="debugInput">The full debug command, including the command name and arguments.</param>
	/// <param name="log">The log to which to write command output, or <c>null</c> to use <see cref="F:StardewValley.Game1.log" />.</param>
	/// <returns>Returns whether the command was found and executed, regardless of whether the command logic succeeded.</returns>
	public bool parseDebugInput(string debugInput, IGameLogger log = null)
	{
		debugInput = debugInput.Trim();
		string[] command = ArgUtility.SplitBySpaceQuoteAware(debugInput);
		try
		{
			return DebugCommands.TryHandle(command, log);
		}
		catch (Exception ex)
		{
			Game1.log.Error("Debug command error.", ex);
			debugOutput = ex.Message;
			return false;
		}
	}

	public void RecountWalnuts()
	{
		if (!IsMasterGame || netWorldState.Value.ActivatedGoldenParrot || !(getLocationFromName("IslandHut") is IslandHut islandHut))
		{
			return;
		}
		int num = islandHut.ShowNutHint();
		int num2 = 130 - num;
		netWorldState.Value.GoldenWalnutsFound = num2;
		foreach (GameLocation location in locations)
		{
			if (!(location is IslandLocation islandLocation))
			{
				continue;
			}
			foreach (ParrotUpgradePerch parrotUpgradePerch in islandLocation.parrotUpgradePerches)
			{
				if (parrotUpgradePerch.currentState.Value == ParrotUpgradePerch.UpgradeState.Complete)
				{
					num2 -= parrotUpgradePerch.requiredNuts.Value;
				}
			}
		}
		if (MasterPlayer.hasOrWillReceiveMail("Island_VolcanoShortcutOut"))
		{
			num2 -= 5;
		}
		if (MasterPlayer.hasOrWillReceiveMail("Island_VolcanoBridge"))
		{
			num2 -= 5;
		}
		netWorldState.Value.GoldenWalnuts = num2;
	}

	public void ResetIslandLocations()
	{
		netWorldState.Value.GoldenWalnutsFound = 0;
		player.team.collectedNutTracker.Clear();
		NetStringHashSet[] array = new NetStringHashSet[3]
		{
			player.mailReceived,
			player.mailForTomorrow,
			player.team.broadcastedMail
		};
		foreach (NetStringHashSet obj in array)
		{
			obj.Remove("birdieQuestBegun");
			obj.Remove("birdieQuestFinished");
			obj.Remove("tigerSlimeNut");
			obj.Remove("Island_W_BuriedTreasureNut");
			obj.Remove("Island_W_BuriedTreasure");
			obj.Remove("islandNorthCaveOpened");
			obj.Remove("Saw_Flame_Sprite_North_North");
			obj.Remove("Saw_Flame_Sprite_North_South");
			obj.Remove("Island_N_BuriedTreasureNut");
			obj.Remove("Island_W_BuriedTreasure");
			obj.Remove("Saw_Flame_Sprite_South");
			obj.Remove("Visited_Island");
			obj.Remove("Island_FirstParrot");
			obj.Remove("gotBirdieReward");
			obj.RemoveWhere((string key) => key.StartsWith("Island_Upgrade"));
		}
		player.secretNotesSeen.RemoveWhere((int id) => id >= GameLocation.JOURNAL_INDEX);
		player.team.limitedNutDrops.Clear();
		netWorldState.Value.GoldenCoconutCracked = false;
		netWorldState.Value.GoldenWalnuts = 0;
		netWorldState.Value.ParrotPlatformsUnlocked = false;
		netWorldState.Value.FoundBuriedNuts.Clear();
		for (int num = 0; num < locations.Count; num++)
		{
			GameLocation gameLocation = locations[num];
			if (gameLocation.InIslandContext())
			{
				_locationLookup.Clear();
				string value = gameLocation.mapPath.Value;
				string value2 = gameLocation.name.Value;
				object[] args = new object[2] { value, value2 };
				try
				{
					locations[num] = Activator.CreateInstance(gameLocation.GetType(), args) as GameLocation;
				}
				catch
				{
					locations[num] = Activator.CreateInstance(gameLocation.GetType()) as GameLocation;
				}
				_locationLookup.Clear();
			}
		}
		AddCharacterIfNecessary("Birdie");
	}

	public void ShowTelephoneMenu()
	{
		playSound("openBox");
		if (IsGreenRainingHere())
		{
			drawObjectDialogue("...................");
			return;
		}
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		foreach (IPhoneHandler phoneHandler in Phone.PhoneHandlers)
		{
			list.AddRange(phoneHandler.GetOutgoingNumbers());
		}
		list.Add(new KeyValuePair<string, string>("HangUp", content.LoadString("Strings\\Locations:MineCart_Destination_Cancel")));
		currentLocation.ShowPagedResponses(content.LoadString("Strings\\Characters:Phone_SelectNumber"), list, delegate(string callId)
		{
			if (callId == "HangUp")
			{
				Phone.HangUp();
			}
			else
			{
				foreach (IPhoneHandler phoneHandler2 in Phone.PhoneHandlers)
				{
					if (phoneHandler2.TryHandleOutgoingCall(callId))
					{
						return;
					}
				}
				Phone.HangUp();
			}
		}, auto_select_single_choice: false, addCancel: false, 6);
	}

	public void requestDebugInput()
	{
		chatBox.activate();
		chatBox.setText("/");
	}

	private void panModeSuccess(KeyboardState currentKBState)
	{
		panFacingDirectionWait = false;
		playSound("smallSelect");
		if (currentKBState.IsKeyDown(Keys.LeftShift))
		{
			panModeString += " (animation_name_here)";
		}
		debugOutput = panModeString;
	}

	private void updatePanModeControls(MouseState currentMouseState, KeyboardState currentKBState)
	{
		if (currentKBState.IsKeyDown(Keys.F8) && !oldKBState.IsKeyDown(Keys.F8))
		{
			requestDebugInput();
			return;
		}
		if (!panFacingDirectionWait)
		{
			if (currentKBState.IsKeyDown(Keys.W))
			{
				viewport.Y -= 16;
			}
			if (currentKBState.IsKeyDown(Keys.A))
			{
				viewport.X -= 16;
			}
			if (currentKBState.IsKeyDown(Keys.S))
			{
				viewport.Y += 16;
			}
			if (currentKBState.IsKeyDown(Keys.D))
			{
				viewport.X += 16;
			}
		}
		else
		{
			if (currentKBState.IsKeyDown(Keys.W))
			{
				panModeString += "0";
				panModeSuccess(currentKBState);
			}
			if (currentKBState.IsKeyDown(Keys.A))
			{
				panModeString += "3";
				panModeSuccess(currentKBState);
			}
			if (currentKBState.IsKeyDown(Keys.S))
			{
				panModeString += "2";
				panModeSuccess(currentKBState);
			}
			if (currentKBState.IsKeyDown(Keys.D))
			{
				panModeString += "1";
				panModeSuccess(currentKBState);
			}
		}
		if (getMouseX(ui_scale: false) < 192)
		{
			viewport.X -= 8;
			viewport.X -= (192 - getMouseX()) / 8;
		}
		if (getMouseX(ui_scale: false) > viewport.Width - 192)
		{
			viewport.X += 8;
			viewport.X += (getMouseX() - viewport.Width + 192) / 8;
		}
		if (getMouseY(ui_scale: false) < 192)
		{
			viewport.Y -= 8;
			viewport.Y -= (192 - getMouseY()) / 8;
		}
		if (getMouseY(ui_scale: false) > viewport.Height - 192)
		{
			viewport.Y += 8;
			viewport.Y += (getMouseY() - viewport.Height + 192) / 8;
		}
		if (currentMouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Released)
		{
			string text = panModeString;
			if (text != null && text.Length > 0)
			{
				int num = (getMouseX() + viewport.X) / 64;
				int num2 = (getMouseY() + viewport.Y) / 64;
				panModeString = panModeString + currentLocation.Name + " " + num + " " + num2 + " ";
				panFacingDirectionWait = true;
				currentLocation.playTerrainSound(new Vector2(num, num2));
				debugOutput = panModeString;
			}
		}
		if (currentMouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released)
		{
			int x = getMouseX() + viewport.X;
			int y = getMouseY() + viewport.Y;
			Warp warp = currentLocation.isCollidingWithWarpOrDoor(new Microsoft.Xna.Framework.Rectangle(x, y, 1, 1));
			if (warp != null)
			{
				currentLocation = RequireLocation(warp.TargetName);
				currentLocation.map.LoadTileSheets(mapDisplayDevice);
				viewport.X = warp.TargetX * 64 - viewport.Width / 2;
				viewport.Y = warp.TargetY * 64 - viewport.Height / 2;
				playSound("dwop");
			}
		}
		if (currentKBState.IsKeyDown(Keys.Escape) && !oldKBState.IsKeyDown(Keys.Escape))
		{
			Warp warp2 = currentLocation.warps[0];
			currentLocation = RequireLocation(warp2.TargetName);
			currentLocation.map.LoadTileSheets(mapDisplayDevice);
			viewport.X = warp2.TargetX * 64 - viewport.Width / 2;
			viewport.Y = warp2.TargetY * 64 - viewport.Height / 2;
			playSound("dwop");
		}
		if (viewport.X < -64)
		{
			viewport.X = -64;
		}
		if (viewport.X + viewport.Width > currentLocation.Map.Layers[0].LayerWidth * 64 + 128)
		{
			viewport.X = currentLocation.Map.Layers[0].LayerWidth * 64 + 128 - viewport.Width;
		}
		if (viewport.Y < -64)
		{
			viewport.Y = -64;
		}
		if (viewport.Y + viewport.Height > currentLocation.Map.Layers[0].LayerHeight * 64 + 128)
		{
			viewport.Y = currentLocation.Map.Layers[0].LayerHeight * 64 + 128 - viewport.Height;
		}
		oldMouseState = input.GetMouseState();
		oldKBState = currentKBState;
	}

	public static bool isLocationAccessible(string locationName)
	{
		switch (locationName)
		{
		case "Desert":
			if (MasterPlayer.mailReceived.Contains("ccVault"))
			{
				return true;
			}
			break;
		case "CommunityCenter":
			if (player.eventsSeen.Contains("191393"))
			{
				return true;
			}
			break;
		case "JojaMart":
			if (!Utility.HasAnyPlayerSeenEvent("191393"))
			{
				return true;
			}
			break;
		case "Railroad":
			if (stats.DaysPlayed > 31)
			{
				return true;
			}
			break;
		default:
			return true;
		}
		return false;
	}

	public static bool isDPadPressed()
	{
		return isDPadPressed(input.GetGamePadState());
	}

	public static bool isDPadPressed(GamePadState pad_state)
	{
		if (pad_state.DPad.Up == ButtonState.Pressed || pad_state.DPad.Down == ButtonState.Pressed || pad_state.DPad.Left == ButtonState.Pressed || pad_state.DPad.Right == ButtonState.Pressed)
		{
			return true;
		}
		return false;
	}

	public static bool isGamePadThumbstickInMotion(double threshold = 0.2)
	{
		bool flag = false;
		GamePadState gamePadState = input.GetGamePadState();
		if ((double)gamePadState.ThumbSticks.Left.X < 0.0 - threshold || gamePadState.IsButtonDown(Buttons.LeftThumbstickLeft))
		{
			flag = true;
		}
		if ((double)gamePadState.ThumbSticks.Left.X > threshold || gamePadState.IsButtonDown(Buttons.LeftThumbstickRight))
		{
			flag = true;
		}
		if ((double)gamePadState.ThumbSticks.Left.Y < 0.0 - threshold || gamePadState.IsButtonDown(Buttons.LeftThumbstickUp))
		{
			flag = true;
		}
		if ((double)gamePadState.ThumbSticks.Left.Y > threshold || gamePadState.IsButtonDown(Buttons.LeftThumbstickDown))
		{
			flag = true;
		}
		if ((double)gamePadState.ThumbSticks.Right.X < 0.0 - threshold)
		{
			flag = true;
		}
		if ((double)gamePadState.ThumbSticks.Right.X > threshold)
		{
			flag = true;
		}
		if ((double)gamePadState.ThumbSticks.Right.Y < 0.0 - threshold)
		{
			flag = true;
		}
		if ((double)gamePadState.ThumbSticks.Right.Y > threshold)
		{
			flag = true;
		}
		if (flag)
		{
			thumbstickMotionMargin = 50;
		}
		return thumbstickMotionMargin > 0;
	}

	public static bool isAnyGamePadButtonBeingPressed()
	{
		return Utility.getPressedButtons(input.GetGamePadState(), oldPadState).Count > 0;
	}

	public static bool isAnyGamePadButtonBeingHeld()
	{
		return Utility.getHeldButtons(input.GetGamePadState()).Count > 0;
	}

	private static void UpdateChatBox()
	{
		if (chatBox == null)
		{
			return;
		}
		KeyboardState keyboardState = input.GetKeyboardState();
		GamePadState gamePadState = input.GetGamePadState();
		if (IsChatting)
		{
			if (textEntry != null)
			{
				return;
			}
			if (gamePadState.IsButtonDown(Buttons.A))
			{
				MouseState mouseState = input.GetMouseState();
				if (chatBox != null && chatBox.isActive() && !chatBox.isHoveringOverClickable(mouseState.X, mouseState.Y))
				{
					oldPadState = gamePadState;
					oldKBState = keyboardState;
					showTextEntry(chatBox.chatBox);
				}
			}
			if (keyboardState.IsKeyDown(Keys.Escape) || gamePadState.IsButtonDown(Buttons.B) || gamePadState.IsButtonDown(Buttons.Back))
			{
				chatBox.clickAway();
				oldKBState = keyboardState;
			}
		}
		else if (keyboardDispatcher.Subscriber == null && ((isOneOfTheseKeysDown(keyboardState, options.chatButton) && game1.HasKeyboardFocus()) || (!gamePadState.IsButtonDown(Buttons.RightStick) && rightStickHoldTime > 0 && rightStickHoldTime < emoteMenuShowTime)))
		{
			chatBox.activate();
			if (keyboardState.IsKeyDown(Keys.OemQuestion))
			{
				chatBox.setText("/");
			}
		}
	}

	public static KeyboardState GetKeyboardState()
	{
		KeyboardState keyboardState = input.GetKeyboardState();
		if (chatBox != null)
		{
			if (IsChatting)
			{
				return default(KeyboardState);
			}
			if (keyboardDispatcher.Subscriber == null && isOneOfTheseKeysDown(keyboardState, options.chatButton) && game1.HasKeyboardFocus())
			{
				return default(KeyboardState);
			}
		}
		return keyboardState;
	}

	private void UpdateControlInput(GameTime time)
	{
		KeyboardState currentKBState = GetKeyboardState();
		MouseState currentMouseState = input.GetMouseState();
		GamePadState currentPadState = input.GetGamePadState();
		if (ticks < _activatedTick + 2 && oldKBState.IsKeyDown(Keys.Tab) != currentKBState.IsKeyDown(Keys.Tab))
		{
			List<Keys> list = oldKBState.GetPressedKeys().ToList();
			if (currentKBState.IsKeyDown(Keys.Tab))
			{
				list.Add(Keys.Tab);
			}
			else
			{
				list.Remove(Keys.Tab);
			}
			oldKBState = new KeyboardState(list.ToArray());
		}
		hooks.OnGame1_UpdateControlInput(ref currentKBState, ref currentMouseState, ref currentPadState, delegate
		{
			if (options.gamepadControls)
			{
				bool flag = false;
				if (Math.Abs(currentPadState.ThumbSticks.Right.X) > 0f || Math.Abs(currentPadState.ThumbSticks.Right.Y) > 0f)
				{
					setMousePositionRaw((int)((float)currentMouseState.X + currentPadState.ThumbSticks.Right.X * thumbstickToMouseModifier), (int)((float)currentMouseState.Y - currentPadState.ThumbSticks.Right.Y * thumbstickToMouseModifier));
					flag = true;
				}
				if (IsChatting)
				{
					flag = true;
				}
				if (((getMouseX() != getOldMouseX() || getMouseY() != getOldMouseY()) && getMouseX() != 0 && getMouseY() != 0) || flag)
				{
					if (flag)
					{
						if (timerUntilMouseFade <= 0)
						{
							lastMousePositionBeforeFade = new Point(localMultiplayerWindow.Width / 2, localMultiplayerWindow.Height / 2);
						}
					}
					else
					{
						lastCursorMotionWasMouse = true;
					}
					if (timerUntilMouseFade <= 0 && !lastCursorMotionWasMouse)
					{
						setMousePositionRaw(lastMousePositionBeforeFade.X, lastMousePositionBeforeFade.Y);
					}
					timerUntilMouseFade = 4000;
				}
			}
			else if (getMouseX() != getOldMouseX() || getMouseY() != getOldMouseY())
			{
				lastCursorMotionWasMouse = true;
			}
			bool actionButtonPressed = false;
			bool switchToolButtonPressed = false;
			bool useToolButtonPressed = false;
			bool useToolButtonReleased = false;
			bool addItemToInventoryButtonPressed = false;
			bool cancelButtonPressed = false;
			bool moveUpPressed = false;
			bool moveRightPressed = false;
			bool moveLeftPressed = false;
			bool moveDownPressed = false;
			bool moveUpReleased = false;
			bool moveRightReleased = false;
			bool moveDownReleased = false;
			bool moveLeftReleased = false;
			bool moveUpHeld = false;
			bool moveRightHeld = false;
			bool moveDownHeld = false;
			bool moveLeftHeld = false;
			bool flag2 = false;
			if ((isOneOfTheseKeysDown(currentKBState, options.actionButton) && areAllOfTheseKeysUp(oldKBState, options.actionButton)) || (currentMouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released))
			{
				actionButtonPressed = true;
				rightClickPolling = 250;
			}
			if ((isOneOfTheseKeysDown(currentKBState, options.useToolButton) && areAllOfTheseKeysUp(oldKBState, options.useToolButton)) || (currentMouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Released))
			{
				useToolButtonPressed = true;
			}
			if ((areAllOfTheseKeysUp(currentKBState, options.useToolButton) && isOneOfTheseKeysDown(oldKBState, options.useToolButton)) || (currentMouseState.LeftButton == ButtonState.Released && oldMouseState.LeftButton == ButtonState.Pressed))
			{
				useToolButtonReleased = true;
			}
			if (currentMouseState.ScrollWheelValue != oldMouseState.ScrollWheelValue)
			{
				switchToolButtonPressed = true;
			}
			if ((isOneOfTheseKeysDown(currentKBState, options.cancelButton) && areAllOfTheseKeysUp(oldKBState, options.cancelButton)) || (currentMouseState.RightButton == ButtonState.Pressed && oldMouseState.RightButton == ButtonState.Released))
			{
				cancelButtonPressed = true;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.moveUpButton) && areAllOfTheseKeysUp(oldKBState, options.moveUpButton))
			{
				moveUpPressed = true;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.moveRightButton) && areAllOfTheseKeysUp(oldKBState, options.moveRightButton))
			{
				moveRightPressed = true;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.moveDownButton) && areAllOfTheseKeysUp(oldKBState, options.moveDownButton))
			{
				moveDownPressed = true;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.moveLeftButton) && areAllOfTheseKeysUp(oldKBState, options.moveLeftButton))
			{
				moveLeftPressed = true;
			}
			if (areAllOfTheseKeysUp(currentKBState, options.moveUpButton) && isOneOfTheseKeysDown(oldKBState, options.moveUpButton))
			{
				moveUpReleased = true;
			}
			if (areAllOfTheseKeysUp(currentKBState, options.moveRightButton) && isOneOfTheseKeysDown(oldKBState, options.moveRightButton))
			{
				moveRightReleased = true;
			}
			if (areAllOfTheseKeysUp(currentKBState, options.moveDownButton) && isOneOfTheseKeysDown(oldKBState, options.moveDownButton))
			{
				moveDownReleased = true;
			}
			if (areAllOfTheseKeysUp(currentKBState, options.moveLeftButton) && isOneOfTheseKeysDown(oldKBState, options.moveLeftButton))
			{
				moveLeftReleased = true;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.moveUpButton))
			{
				moveUpHeld = true;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.moveRightButton))
			{
				moveRightHeld = true;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.moveDownButton))
			{
				moveDownHeld = true;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.moveLeftButton))
			{
				moveLeftHeld = true;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.useToolButton) || currentMouseState.LeftButton == ButtonState.Pressed)
			{
				flag2 = true;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.actionButton) || currentMouseState.RightButton == ButtonState.Pressed)
			{
				rightClickPolling -= time.ElapsedGameTime.Milliseconds;
				if (rightClickPolling <= 0)
				{
					rightClickPolling = 100;
					actionButtonPressed = true;
				}
			}
			if (options.gamepadControls)
			{
				if (currentKBState.GetPressedKeys().Length != 0 || currentMouseState.LeftButton == ButtonState.Pressed || currentMouseState.RightButton == ButtonState.Pressed)
				{
					timerUntilMouseFade = 4000;
				}
				if (currentPadState.IsButtonDown(Buttons.A) && !oldPadState.IsButtonDown(Buttons.A))
				{
					actionButtonPressed = true;
					lastCursorMotionWasMouse = false;
					rightClickPolling = 250;
				}
				if (currentPadState.IsButtonDown(Buttons.X) && !oldPadState.IsButtonDown(Buttons.X))
				{
					useToolButtonPressed = true;
					lastCursorMotionWasMouse = false;
				}
				if (!currentPadState.IsButtonDown(Buttons.X) && oldPadState.IsButtonDown(Buttons.X))
				{
					useToolButtonReleased = true;
				}
				if (currentPadState.IsButtonDown(Buttons.RightTrigger) && !oldPadState.IsButtonDown(Buttons.RightTrigger))
				{
					switchToolButtonPressed = true;
					triggerPolling = 300;
				}
				else if (currentPadState.IsButtonDown(Buttons.LeftTrigger) && !oldPadState.IsButtonDown(Buttons.LeftTrigger))
				{
					switchToolButtonPressed = true;
					triggerPolling = 300;
				}
				if (currentPadState.IsButtonDown(Buttons.X))
				{
					flag2 = true;
				}
				if (currentPadState.IsButtonDown(Buttons.A))
				{
					rightClickPolling -= time.ElapsedGameTime.Milliseconds;
					if (rightClickPolling <= 0)
					{
						rightClickPolling = 100;
						actionButtonPressed = true;
					}
				}
				if (currentPadState.IsButtonDown(Buttons.RightTrigger) || currentPadState.IsButtonDown(Buttons.LeftTrigger))
				{
					triggerPolling -= time.ElapsedGameTime.Milliseconds;
					if (triggerPolling <= 0)
					{
						triggerPolling = 100;
						switchToolButtonPressed = true;
					}
				}
				if (currentPadState.IsButtonDown(Buttons.RightShoulder) && !oldPadState.IsButtonDown(Buttons.RightShoulder) && IsHudDrawn)
				{
					player.shiftToolbar(right: true);
				}
				if (currentPadState.IsButtonDown(Buttons.LeftShoulder) && !oldPadState.IsButtonDown(Buttons.LeftShoulder) && IsHudDrawn)
				{
					player.shiftToolbar(right: false);
				}
				if (currentPadState.IsButtonDown(Buttons.DPadUp) && !oldPadState.IsButtonDown(Buttons.DPadUp))
				{
					moveUpPressed = true;
				}
				else if (!currentPadState.IsButtonDown(Buttons.DPadUp) && oldPadState.IsButtonDown(Buttons.DPadUp))
				{
					moveUpReleased = true;
				}
				if (currentPadState.IsButtonDown(Buttons.DPadRight) && !oldPadState.IsButtonDown(Buttons.DPadRight))
				{
					moveRightPressed = true;
				}
				else if (!currentPadState.IsButtonDown(Buttons.DPadRight) && oldPadState.IsButtonDown(Buttons.DPadRight))
				{
					moveRightReleased = true;
				}
				if (currentPadState.IsButtonDown(Buttons.DPadDown) && !oldPadState.IsButtonDown(Buttons.DPadDown))
				{
					moveDownPressed = true;
				}
				else if (!currentPadState.IsButtonDown(Buttons.DPadDown) && oldPadState.IsButtonDown(Buttons.DPadDown))
				{
					moveDownReleased = true;
				}
				if (currentPadState.IsButtonDown(Buttons.DPadLeft) && !oldPadState.IsButtonDown(Buttons.DPadLeft))
				{
					moveLeftPressed = true;
				}
				else if (!currentPadState.IsButtonDown(Buttons.DPadLeft) && oldPadState.IsButtonDown(Buttons.DPadLeft))
				{
					moveLeftReleased = true;
				}
				if (currentPadState.IsButtonDown(Buttons.DPadUp))
				{
					moveUpHeld = true;
				}
				if (currentPadState.IsButtonDown(Buttons.DPadRight))
				{
					moveRightHeld = true;
				}
				if (currentPadState.IsButtonDown(Buttons.DPadDown))
				{
					moveDownHeld = true;
				}
				if (currentPadState.IsButtonDown(Buttons.DPadLeft))
				{
					moveLeftHeld = true;
				}
				if ((double)currentPadState.ThumbSticks.Left.X < -0.2)
				{
					moveLeftPressed = true;
					moveLeftHeld = true;
				}
				else if ((double)currentPadState.ThumbSticks.Left.X > 0.2)
				{
					moveRightPressed = true;
					moveRightHeld = true;
				}
				if ((double)currentPadState.ThumbSticks.Left.Y < -0.2)
				{
					moveDownPressed = true;
					moveDownHeld = true;
				}
				else if ((double)currentPadState.ThumbSticks.Left.Y > 0.2)
				{
					moveUpPressed = true;
					moveUpHeld = true;
				}
				if ((double)oldPadState.ThumbSticks.Left.X < -0.2 && !moveLeftHeld)
				{
					moveLeftReleased = true;
				}
				if ((double)oldPadState.ThumbSticks.Left.X > 0.2 && !moveRightHeld)
				{
					moveRightReleased = true;
				}
				if ((double)oldPadState.ThumbSticks.Left.Y < -0.2 && !moveDownHeld)
				{
					moveDownReleased = true;
				}
				if ((double)oldPadState.ThumbSticks.Left.Y > 0.2 && !moveUpHeld)
				{
					moveUpReleased = true;
				}
				if (controllerSlingshotSafeTime > 0f)
				{
					if (!currentPadState.IsButtonDown(Buttons.DPadUp) && !currentPadState.IsButtonDown(Buttons.DPadDown) && !currentPadState.IsButtonDown(Buttons.DPadLeft) && !currentPadState.IsButtonDown(Buttons.DPadRight) && (double)Math.Abs(currentPadState.ThumbSticks.Left.X) < 0.04 && (double)Math.Abs(currentPadState.ThumbSticks.Left.Y) < 0.04)
					{
						controllerSlingshotSafeTime = 0f;
					}
					if (controllerSlingshotSafeTime <= 0f)
					{
						controllerSlingshotSafeTime = 0f;
					}
					else
					{
						controllerSlingshotSafeTime -= (float)time.ElapsedGameTime.TotalSeconds;
						moveUpPressed = false;
						moveDownPressed = false;
						moveLeftPressed = false;
						moveRightPressed = false;
						moveUpHeld = false;
						moveDownHeld = false;
						moveLeftHeld = false;
						moveRightHeld = false;
					}
				}
			}
			else
			{
				controllerSlingshotSafeTime = 0f;
			}
			ResetFreeCursorDrag();
			if (flag2)
			{
				mouseClickPolling += time.ElapsedGameTime.Milliseconds;
			}
			else
			{
				mouseClickPolling = 0;
			}
			if (isOneOfTheseKeysDown(currentKBState, options.toolbarSwap) && areAllOfTheseKeysUp(oldKBState, options.toolbarSwap) && IsHudDrawn)
			{
				player.shiftToolbar(!currentKBState.IsKeyDown(Keys.LeftControl));
			}
			if (mouseClickPolling > 250 && (!(player.CurrentTool is FishingRod) || player.CurrentTool.upgradeLevel.Value <= 0))
			{
				useToolButtonPressed = true;
				mouseClickPolling = 100;
			}
			PushUIMode();
			foreach (IClickableMenu onScreenMenu in onScreenMenus)
			{
				if ((IsHudDrawn || onScreenMenu == chatBox) && wasMouseVisibleThisFrame && onScreenMenu.isWithinBounds(getMouseX(), getMouseY()))
				{
					onScreenMenu.performHoverAction(getMouseX(), getMouseY());
				}
			}
			PopUIMode();
			if (chatBox != null && chatBox.chatBox.Selected && oldMouseState.ScrollWheelValue != currentMouseState.ScrollWheelValue)
			{
				chatBox.receiveScrollWheelAction(currentMouseState.ScrollWheelValue - oldMouseState.ScrollWheelValue);
			}
			if (panMode)
			{
				updatePanModeControls(currentMouseState, currentKBState);
			}
			else
			{
				if (inputSimulator != null)
				{
					if (currentKBState.IsKeyDown(Keys.Escape))
					{
						inputSimulator = null;
					}
					else
					{
						inputSimulator.SimulateInput(ref actionButtonPressed, ref switchToolButtonPressed, ref useToolButtonPressed, ref useToolButtonReleased, ref addItemToInventoryButtonPressed, ref cancelButtonPressed, ref moveUpPressed, ref moveRightPressed, ref moveLeftPressed, ref moveDownPressed, ref moveUpReleased, ref moveRightReleased, ref moveLeftReleased, ref moveDownReleased, ref moveUpHeld, ref moveRightHeld, ref moveLeftHeld, ref moveDownHeld);
					}
				}
				if (useToolButtonReleased && player.CurrentTool != null && CurrentEvent == null && pauseTime <= 0f && player.CurrentTool.onRelease(currentLocation, getMouseX(), getMouseY(), player))
				{
					oldMouseState = input.GetMouseState();
					oldKBState = currentKBState;
					oldPadState = currentPadState;
					player.usingSlingshot = false;
					player.canReleaseTool = true;
					player.UsingTool = false;
					player.CanMove = true;
				}
				else
				{
					if (((useToolButtonPressed && !isAnyGamePadButtonBeingPressed()) || (actionButtonPressed && isAnyGamePadButtonBeingPressed())) && pauseTime <= 0f && wasMouseVisibleThisFrame)
					{
						if (debugMode)
						{
							Console.WriteLine(getMouseX() + viewport.X + ", " + (getMouseY() + viewport.Y));
						}
						PushUIMode();
						foreach (IClickableMenu onScreenMenu2 in onScreenMenus)
						{
							if (IsHudDrawn || onScreenMenu2 == chatBox)
							{
								if ((!IsChatting || onScreenMenu2 == chatBox) && !(onScreenMenu2 is LevelUpMenu { informationUp: false }) && onScreenMenu2.isWithinBounds(getMouseX(), getMouseY()))
								{
									onScreenMenu2.receiveLeftClick(getMouseX(), getMouseY());
									PopUIMode();
									oldMouseState = input.GetMouseState();
									oldKBState = currentKBState;
									oldPadState = currentPadState;
									return;
								}
								if (onScreenMenu2 == chatBox && options.gamepadControls && IsChatting)
								{
									oldMouseState = input.GetMouseState();
									oldKBState = currentKBState;
									oldPadState = currentPadState;
									PopUIMode();
									return;
								}
								onScreenMenu2.clickAway();
							}
						}
						PopUIMode();
					}
					if (IsChatting || player.freezePause > 0)
					{
						if (IsChatting)
						{
							ButtonCollection.ButtonEnumerator enumerator2 = Utility.getPressedButtons(currentPadState, oldPadState).GetEnumerator();
							while (enumerator2.MoveNext())
							{
								Buttons current3 = enumerator2.Current;
								chatBox.receiveGamePadButton(current3);
							}
						}
						oldMouseState = input.GetMouseState();
						oldKBState = currentKBState;
						oldPadState = currentPadState;
					}
					else
					{
						if (paused || HostPaused)
						{
							if (!HostPaused || !IsMasterGame || (!isOneOfTheseKeysDown(currentKBState, options.menuButton) && !currentPadState.IsButtonDown(Buttons.B) && !currentPadState.IsButtonDown(Buttons.Back)))
							{
								oldMouseState = input.GetMouseState();
								return;
							}
							netWorldState.Value.IsPaused = false;
							chatBox?.globalInfoMessage("Resumed");
						}
						if (eventUp)
						{
							if (currentLocation.currentEvent == null && locationRequest == null)
							{
								eventUp = false;
							}
							else if (actionButtonPressed || useToolButtonPressed)
							{
								CurrentEvent?.receiveMouseClick(getMouseX(), getMouseY());
							}
						}
						bool flag3 = eventUp || farmEvent != null;
						if (actionButtonPressed || (dialogueUp && useToolButtonPressed))
						{
							PushUIMode();
							if (StardewValley.Pokemon.PokemonHUD.Instance.IsMouseOverHUD(getMouseX(), getMouseY()))
							{
								StardewValley.Pokemon.PokemonHUD.Instance.ReceiveRightClick(getMouseX(), getMouseY());
								PopUIMode();
								oldMouseState = input.GetMouseState();
								oldKBState = currentKBState;
								oldPadState = currentPadState;
								return;
							}
							foreach (IClickableMenu onScreenMenu3 in onScreenMenus)
							{
								if (wasMouseVisibleThisFrame && (IsHudDrawn || onScreenMenu3 == chatBox) && onScreenMenu3.isWithinBounds(getMouseX(), getMouseY()) && !(onScreenMenu3 is LevelUpMenu { informationUp: false }))
								{
									onScreenMenu3.receiveRightClick(getMouseX(), getMouseY());
									oldMouseState = input.GetMouseState();
									if (!isAnyGamePadButtonBeingPressed())
									{
										PopUIMode();
										oldKBState = currentKBState;
										oldPadState = currentPadState;
										return;
									}
								}
							}
							PopUIMode();
							if (!pressActionButton(currentKBState, currentMouseState, currentPadState))
							{
								oldKBState = currentKBState;
								oldMouseState = input.GetMouseState();
								oldPadState = currentPadState;
								return;
							}
						}
						if (useToolButtonPressed && (!player.UsingTool || player.CurrentTool is MeleeWeapon) && !player.isEating && !dialogueUp && farmEvent == null && (player.CanMove || player.CurrentTool is MeleeWeapon))
						{
							if (StardewValley.Pokemon.PokemonHUD.Instance.IsMouseOverHUD(getMouseX(true), getMouseY(true)))
							{
								// Clicking on Pokemon lateral HUD: process click and consume without swinging tool or placing item!
								StardewValley.Pokemon.PokemonHUD.Instance.ReceiveLeftClick(getMouseX(true), getMouseY(true));
								oldMouseState = input.GetMouseState();
								oldKBState = currentKBState;
								oldPadState = currentPadState;
								return;
							}

							if (player.CurrentTool != null && (!(player.CurrentTool is MeleeWeapon) || didPlayerJustLeftClick(ignoreNonMouseHeldInput: true)))
							{
								player.FireTool();
							}
							if (!pressUseToolButton() && player.canReleaseTool && player.UsingTool)
							{
								_ = player.CurrentTool;
							}
							if (player.UsingTool)
							{
								oldMouseState = input.GetMouseState();
								oldKBState = currentKBState;
								oldPadState = currentPadState;
								return;
							}
						}
						if (useToolButtonReleased && _didInitiateItemStow)
						{
							_didInitiateItemStow = false;
						}
						if (useToolButtonReleased && player.canReleaseTool && player.UsingTool && player.CurrentTool != null)
						{
							player.EndUsingTool();
						}
						if (switchToolButtonPressed && !player.UsingTool && !dialogueUp && player.CanMove && player.Items.HasAny() && !flag3)
						{
							pressSwitchToolButton();
						}
						if (player.CurrentTool != null && flag2 && player.canReleaseTool && !flag3 && !dialogueUp && player.Stamina >= 1f && !(player.CurrentTool is FishingRod))
						{
							int num = (player.CurrentTool.hasEnchantmentOfType<ReachingToolEnchantment>() ? 1 : 0);
							if (player.toolHold.Value <= 0 && player.CurrentTool.upgradeLevel.Value + num > player.toolPower.Value)
							{
								float num2 = 1f;
								if (player.CurrentTool != null)
								{
									num2 = player.CurrentTool.AnimationSpeedModifier;
								}
								player.toolHold.Value = (int)(600f * num2);
								player.toolHoldStartTime.Value = player.toolHold.Value;
							}
							else if (player.CurrentTool.upgradeLevel.Value + num > player.toolPower.Value)
							{
								player.toolHold.Value -= time.ElapsedGameTime.Milliseconds;
								if (player.toolHold.Value <= 0)
								{
									player.toolPowerIncrease();
								}
							}
						}
						if (upPolling >= 650f)
						{
							moveUpPressed = true;
							upPolling -= 100f;
						}
						else if (downPolling >= 650f)
						{
							moveDownPressed = true;
							downPolling -= 100f;
						}
						else if (rightPolling >= 650f)
						{
							moveRightPressed = true;
							rightPolling -= 100f;
						}
						else if (leftPolling >= 650f)
						{
							moveLeftPressed = true;
							leftPolling -= 100f;
						}
						else if (pauseTime <= 0f && locationRequest == null && (!player.UsingTool || player.canStrafeForToolUse()) && (!flag3 || (CurrentEvent != null && CurrentEvent.playerControlSequence)))
						{
							FirstPerson.FirstPersonManager.Instance.ProcessMovement(ref moveUpHeld, ref moveRightHeld, ref moveDownHeld, ref moveLeftHeld);
							if (player.movementDirections.Count < 2)
							{
								if (moveUpHeld)
								{
									player.setMoving(1);
								}
								if (moveRightHeld)
								{
									player.setMoving(2);
								}
								if (moveDownHeld)
								{
									player.setMoving(4);
								}
								if (moveLeftHeld)
								{
									player.setMoving(8);
								}
							}
							if (moveUpReleased || (player.movementDirections.Contains(0) && !moveUpHeld))
							{
								player.setMoving(33);
								if (player.movementDirections.Count == 0)
								{
									player.setMoving(64);
								}
							}
							if (moveRightReleased || (player.movementDirections.Contains(1) && !moveRightHeld))
							{
								player.setMoving(34);
								if (player.movementDirections.Count == 0)
								{
									player.setMoving(64);
								}
							}
							if (moveDownReleased || (player.movementDirections.Contains(2) && !moveDownHeld))
							{
								player.setMoving(36);
								if (player.movementDirections.Count == 0)
								{
									player.setMoving(64);
								}
							}
							if (moveLeftReleased || (player.movementDirections.Contains(3) && !moveLeftHeld))
							{
								player.setMoving(40);
								if (player.movementDirections.Count == 0)
								{
									player.setMoving(64);
								}
							}
							if ((!moveUpHeld && !moveRightHeld && !moveDownHeld && !moveLeftHeld && !player.UsingTool) || activeClickableMenu != null)
							{
								player.Halt();
							}
						}
						else if (isQuestion)
						{
							if (moveUpPressed)
							{
								currentQuestionChoice = Math.Max(currentQuestionChoice - 1, 0);
								playSound("toolSwap");
							}
							else if (moveDownPressed)
							{
								currentQuestionChoice = Math.Min(currentQuestionChoice + 1, questionChoices.Count - 1);
								playSound("toolSwap");
							}
						}
						if (moveUpHeld && !player.CanMove)
						{
							upPolling += time.ElapsedGameTime.Milliseconds;
						}
						else if (moveDownHeld && !player.CanMove)
						{
							downPolling += time.ElapsedGameTime.Milliseconds;
						}
						else if (moveRightHeld && !player.CanMove)
						{
							rightPolling += time.ElapsedGameTime.Milliseconds;
						}
						else if (moveLeftHeld && !player.CanMove)
						{
							leftPolling += time.ElapsedGameTime.Milliseconds;
						}
						else if (moveUpReleased)
						{
							upPolling = 0f;
						}
						else if (moveDownReleased)
						{
							downPolling = 0f;
						}
						else if (moveRightReleased)
						{
							rightPolling = 0f;
						}
						else if (moveLeftReleased)
						{
							leftPolling = 0f;
						}
						if (debugMode)
						{
							if (currentKBState.IsKeyDown(Keys.Q))
							{
								oldKBState.IsKeyDown(Keys.Q);
							}
							if (currentKBState.IsKeyDown(Keys.P) && !oldKBState.IsKeyDown(Keys.P))
							{
								NewDay(0f);
							}
							if (currentKBState.IsKeyDown(Keys.M) && !oldKBState.IsKeyDown(Keys.M))
							{
								dayOfMonth = 28;
								NewDay(0f);
							}
							if (currentKBState.IsKeyDown(Keys.T) && !oldKBState.IsKeyDown(Keys.T))
							{
								addHour();
							}
							if (currentKBState.IsKeyDown(Keys.Y) && !oldKBState.IsKeyDown(Keys.Y))
							{
								addMinute();
							}
							if (currentKBState.IsKeyDown(Keys.D1) && !oldKBState.IsKeyDown(Keys.D1))
							{
								warpFarmer("Mountain", 15, 35, flip: false);
							}
							if (currentKBState.IsKeyDown(Keys.D2) && !oldKBState.IsKeyDown(Keys.D2))
							{
								warpFarmer("Town", 35, 35, flip: false);
							}
							if (currentKBState.IsKeyDown(Keys.D3) && !oldKBState.IsKeyDown(Keys.D3))
							{
								warpFarmer("Farm", 64, 15, flip: false);
							}
							if (currentKBState.IsKeyDown(Keys.D4) && !oldKBState.IsKeyDown(Keys.D4))
							{
								warpFarmer("Forest", 34, 13, flip: false);
							}
							if (currentKBState.IsKeyDown(Keys.D5) && !oldKBState.IsKeyDown(Keys.D4))
							{
								warpFarmer("Beach", 34, 10, flip: false);
							}
							if (currentKBState.IsKeyDown(Keys.D6) && !oldKBState.IsKeyDown(Keys.D6))
							{
								warpFarmer("Mine", 18, 12, flip: false);
							}
							if (currentKBState.IsKeyDown(Keys.D7) && !oldKBState.IsKeyDown(Keys.D7))
							{
								warpFarmer("SandyHouse", 16, 3, flip: false);
							}
							if (currentKBState.IsKeyDown(Keys.K) && !oldKBState.IsKeyDown(Keys.K))
							{
								enterMine(mine.mineLevel + 1);
							}
							if (currentKBState.IsKeyDown(Keys.H) && !oldKBState.IsKeyDown(Keys.H))
							{
								player.changeHat(random.Next(FarmerRenderer.hatsTexture.Height / 80 * 12));
							}
							if (currentKBState.IsKeyDown(Keys.I) && !oldKBState.IsKeyDown(Keys.I))
							{
								player.changeHairStyle(random.Next(FarmerRenderer.hairStylesTexture.Height / 96 * 8));
							}
							if (currentKBState.IsKeyDown(Keys.J) && !oldKBState.IsKeyDown(Keys.J))
							{
								player.changeShirt(random.Next(1000, 1040).ToString());
								player.changePantsColor(new Color(random.Next(255), random.Next(255), random.Next(255)));
							}
							if (currentKBState.IsKeyDown(Keys.L) && !oldKBState.IsKeyDown(Keys.L))
							{
								player.changeShirt(random.Next(1000, 1040).ToString());
								player.changePantsColor(new Color(random.Next(255), random.Next(255), random.Next(255)));
								player.changeHairStyle(random.Next(FarmerRenderer.hairStylesTexture.Height / 96 * 8));
								if (random.NextBool())
								{
									player.changeHat(random.Next(-1, FarmerRenderer.hatsTexture.Height / 80 * 12));
								}
								else
								{
									player.changeHat(-1);
								}
								player.changeHairColor(new Color(random.Next(255), random.Next(255), random.Next(255)));
								player.changeSkinColor(random.Next(16));
							}
							if (currentKBState.IsKeyDown(Keys.U) && !oldKBState.IsKeyDown(Keys.U))
							{
								FarmHouse farmHouse = RequireLocation<FarmHouse>("FarmHouse");
								farmHouse.SetWallpaper(random.Next(112).ToString(), null);
								farmHouse.SetFloor(random.Next(40).ToString(), null);
							}
							if (currentKBState.IsKeyDown(Keys.F2))
							{
								oldKBState.IsKeyDown(Keys.F2);
							}
							// F5 disabled for Pokemon overhaul clean controls
							/*
							if (currentKBState.IsKeyDown(Keys.F5) && !oldKBState.IsKeyDown(Keys.F5))
							{
								displayFarmer = !displayFarmer;
							}
							*/
							if (currentKBState.IsKeyDown(Keys.F6))
							{
								oldKBState.IsKeyDown(Keys.F6);
							}
							if (currentKBState.IsKeyDown(Keys.F7) && !oldKBState.IsKeyDown(Keys.F7))
							{
								drawGrid = !drawGrid;
							}
							if (currentKBState.IsKeyDown(Keys.B) && !oldKBState.IsKeyDown(Keys.B) && IsHudDrawn)
							{
								player.shiftToolbar(right: false);
							}
							if (currentKBState.IsKeyDown(Keys.N) && !oldKBState.IsKeyDown(Keys.N) && IsHudDrawn)
							{
								player.shiftToolbar(right: true);
							}
							if (currentKBState.IsKeyDown(Keys.F10) && !oldKBState.IsKeyDown(Keys.F10) && server == null)
							{
								multiplayer.StartServer();
							}
						}
						else if (!player.UsingTool)
						{
							if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot1) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot1))
							{
								player.CurrentToolIndex = 0;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot2) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot2))
							{
								player.CurrentToolIndex = 1;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot3) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot3))
							{
								player.CurrentToolIndex = 2;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot4) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot4))
							{
								player.CurrentToolIndex = 3;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot5) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot5))
							{
								player.CurrentToolIndex = 4;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot6) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot6))
							{
								player.CurrentToolIndex = 5;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot7) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot7))
							{
								player.CurrentToolIndex = 6;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot8) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot8))
							{
								player.CurrentToolIndex = 7;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot9) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot9))
							{
								player.CurrentToolIndex = 8;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot10) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot10))
							{
								player.CurrentToolIndex = 9;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot11) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot11))
							{
								player.CurrentToolIndex = 10;
							}
							else if (isOneOfTheseKeysDown(currentKBState, options.inventorySlot12) && areAllOfTheseKeysUp(oldKBState, options.inventorySlot12))
							{
								player.CurrentToolIndex = 11;
							}
						}
						if (((options.gamepadControls && rightStickHoldTime >= emoteMenuShowTime && activeClickableMenu == null) || (isOneOfTheseKeysDown(input.GetKeyboardState(), options.emoteButton) && areAllOfTheseKeysUp(oldKBState, options.emoteButton))) && !debugMode && player.CanEmote())
						{
							if (player.CanMove)
							{
								player.Halt();
							}
							emoteMenu = new EmoteMenu();
							emoteMenu.gamepadMode = options.gamepadControls && rightStickHoldTime >= emoteMenuShowTime;
							timerUntilMouseFade = 0;
						}
						if (!Program.releaseBuild)
						{
							if (IsPressEvent(ref currentKBState, Keys.F3) || IsPressEvent(ref currentPadState, Buttons.LeftStick))
							{
								debugMode = !debugMode;
								if (gameMode == 11)
								{
									gameMode = 3;
								}
							}
							if (IsPressEvent(ref currentKBState, Keys.F8))
							{
								requestDebugInput();
							}
						}
						if (currentKBState.IsKeyDown(Keys.F4) && !oldKBState.IsKeyDown(Keys.F4))
						{
							displayHUD = !displayHUD;
							playSound("smallSelect");
							if (!displayHUD)
							{
								showGlobalMessage(content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3666"));
							}
						}
						bool flag4 = isOneOfTheseKeysDown(currentKBState, options.menuButton) && areAllOfTheseKeysUp(oldKBState, options.menuButton);
						bool flag5 = isOneOfTheseKeysDown(currentKBState, options.journalButton) && areAllOfTheseKeysUp(oldKBState, options.journalButton);
						bool flag6 = isOneOfTheseKeysDown(currentKBState, options.mapButton) && areAllOfTheseKeysUp(oldKBState, options.mapButton);
						if (options.gamepadControls && !flag4)
						{
							flag4 = (currentPadState.IsButtonDown(Buttons.Start) && !oldPadState.IsButtonDown(Buttons.Start)) || (currentPadState.IsButtonDown(Buttons.B) && !oldPadState.IsButtonDown(Buttons.B));
						}
						if (options.gamepadControls && !flag5)
						{
							flag5 = currentPadState.IsButtonDown(Buttons.Back) && !oldPadState.IsButtonDown(Buttons.Back);
						}
						if (options.gamepadControls && !flag6)
						{
							flag6 = currentPadState.IsButtonDown(Buttons.Y) && !oldPadState.IsButtonDown(Buttons.Y);
						}
						if (flag4 && CanShowPauseMenu())
						{
							if (activeClickableMenu == null)
							{
								PushUIMode();
								activeClickableMenu = new GameMenu();
								PopUIMode();
							}
							else if (activeClickableMenu.readyToClose())
							{
								exitActiveMenu();
							}
						}
						if (dayOfMonth > 0 && player.CanMove && flag5 && !dialogueUp && !flag3)
						{
							if (activeClickableMenu == null)
							{
								activeClickableMenu = new QuestLog();
							}
						}
						else if (flag3 && CurrentEvent != null && flag5 && !CurrentEvent.skipped && CurrentEvent.skippable)
						{
							CurrentEvent.skipped = true;
							CurrentEvent.skipEvent();
							freezeControls = false;
						}
						if (options.gamepadControls && dayOfMonth > 0 && player.CanMove && isAnyGamePadButtonBeingPressed() && flag6 && !dialogueUp && !flag3)
						{
							if (activeClickableMenu == null)
							{
								PushUIMode();
								activeClickableMenu = new GameMenu(GameMenu.craftingTab);
								PopUIMode();
							}
						}
						else if (dayOfMonth > 0 && player.CanMove && flag6 && !dialogueUp && !flag3 && activeClickableMenu == null)
						{
							PushUIMode();
							activeClickableMenu = new GameMenu(GameMenu.mapTab);
							PopUIMode();
						}
						checkForRunButton(currentKBState);
						oldKBState = currentKBState;
						oldMouseState = input.GetMouseState();
						oldPadState = currentPadState;
					}
				}
			}
		});
	}

	public static bool CanShowPauseMenu()
	{
		if (dayOfMonth > 0 && player.CanMove && !dialogueUp && (!eventUp || (isFestival() && CurrentEvent.festivalTimer <= 0)) && currentMinigame == null)
		{
			return farmEvent == null;
		}
		return false;
	}

	internal static void addHour()
	{
		timeOfDay += 100;
		foreach (GameLocation location in locations)
		{
			for (int i = 0; i < location.characters.Count; i++)
			{
				NPC nPC = location.characters[i];
				nPC.checkSchedule(timeOfDay);
				nPC.checkSchedule(timeOfDay - 50);
				nPC.checkSchedule(timeOfDay - 60);
				nPC.checkSchedule(timeOfDay - 70);
				nPC.checkSchedule(timeOfDay - 80);
				nPC.checkSchedule(timeOfDay - 90);
			}
		}
		switch (timeOfDay)
		{
		case 1900:
			currentLocation.switchOutNightTiles();
			break;
		case 2000:
			if (!currentLocation.IsRainingHere())
			{
				changeMusicTrack("none");
			}
			break;
		}
	}

	internal static void addMinute()
	{
		if (GetKeyboardState().IsKeyDown(Keys.LeftShift))
		{
			timeOfDay -= 10;
		}
		else
		{
			timeOfDay += 10;
		}
		if (timeOfDay % 100 == 60)
		{
			timeOfDay += 40;
		}
		if (timeOfDay % 100 == 90)
		{
			timeOfDay -= 40;
		}
		currentLocation.performTenMinuteUpdate(timeOfDay);
		foreach (GameLocation location in locations)
		{
			for (int i = 0; i < location.characters.Count; i++)
			{
				location.characters[i].checkSchedule(timeOfDay);
			}
		}
		if (isLightning && IsMasterGame)
		{
			Utility.performLightningUpdate(timeOfDay);
		}
		switch (timeOfDay)
		{
		case 1750:
			outdoorLight = Color.White;
			break;
		case 1900:
			currentLocation.switchOutNightTiles();
			break;
		case 2000:
			if (!currentLocation.IsRainingHere())
			{
				changeMusicTrack("none");
			}
			break;
		}
	}

	public static void checkForRunButton(KeyboardState kbState, bool ignoreKeyPressQualifier = false)
	{
		bool running = player.running;
		bool flag = isOneOfTheseKeysDown(kbState, options.runButton) && (!isOneOfTheseKeysDown(oldKBState, options.runButton) || ignoreKeyPressQualifier);
		bool flag2 = !isOneOfTheseKeysDown(kbState, options.runButton) && (isOneOfTheseKeysDown(oldKBState, options.runButton) || ignoreKeyPressQualifier);
		if (options.gamepadControls)
		{
			if (!options.autoRun && Math.Abs(Vector2.Distance(input.GetGamePadState().ThumbSticks.Left, Vector2.Zero)) > 0.9f)
			{
				flag = true;
			}
			else if (Math.Abs(Vector2.Distance(oldPadState.ThumbSticks.Left, Vector2.Zero)) > 0.9f && Math.Abs(Vector2.Distance(input.GetGamePadState().ThumbSticks.Left, Vector2.Zero)) <= 0.9f)
			{
				flag2 = true;
			}
		}
		if (flag && !player.canOnlyWalk)
		{
			player.setRunning(!options.autoRun);
			player.setMoving((byte)(player.running ? 16u : 48u));
		}
		else if (flag2 && !player.canOnlyWalk)
		{
			player.setRunning(options.autoRun);
			player.setMoving((byte)(player.running ? 16u : 48u));
		}
		if (player.running != running && !player.UsingTool)
		{
			player.Halt();
		}
	}

	public static Vector2 getMostRecentViewportMotion()
	{
		return new Vector2((float)viewport.X - previousViewportPosition.X, (float)viewport.Y - previousViewportPosition.Y);
	}

	protected virtual void DrawOverlays(GameTime time, RenderTarget2D target_screen)
	{
		if (takingMapScreenshot)
		{
			return;
		}
		PushUIMode();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		if (hooks.OnRendering(RenderSteps.Overlays, spriteBatch, time, target_screen))
		{
			specialCurrencyDisplay?.Draw(spriteBatch);
			emoteMenu?.draw(spriteBatch);
			currentLocation?.drawOverlays(spriteBatch);
			if (HostPaused && !takingMapScreenshot)
			{
				string s = content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10378");
				SpriteText.drawStringWithScrollBackground(spriteBatch, s, 96, 32);
			}
			if (overlayMenu != null)
			{
				if (hooks.OnRendering(RenderSteps.Overlays_OverlayMenu, spriteBatch, time, target_screen))
				{
					overlayMenu.draw(spriteBatch);
				}
				hooks.OnRendered(RenderSteps.Overlays_OverlayMenu, spriteBatch, time, target_screen);
			}
			if (chatBox != null)
			{
				if (hooks.OnRendering(RenderSteps.Overlays_Chatbox, spriteBatch, time, target_screen))
				{
					chatBox.update(currentGameTime);
					chatBox.draw(spriteBatch);
				}
				hooks.OnRendered(RenderSteps.Overlays_Chatbox, spriteBatch, time, target_screen);
			}
			if (textEntry != null)
			{
				if (hooks.OnRendering(RenderSteps.Overlays_OnscreenKeyboard, spriteBatch, time, target_screen))
				{
					textEntry.draw(spriteBatch);
				}
				hooks.OnRendered(RenderSteps.Overlays_OnscreenKeyboard, spriteBatch, time, target_screen);
			}
			if ((displayHUD || eventUp || currentLocation is Summit) && gameMode == 3 && !freezeControls && !panMode)
			{
				drawMouseCursor();
			}
		}
		hooks.OnRendered(RenderSteps.Overlays, spriteBatch, time, target_screen);
		spriteBatch.End();
		PopUIMode();
	}

	public static void setBGColor(byte r, byte g, byte b)
	{
		bgColor.R = r;
		bgColor.G = g;
		bgColor.B = b;
	}

	public void Instance_Draw(GameTime gameTime)
	{
		Draw(gameTime);
	}

	/// <summary>
	/// This is called when the game should draw itself.
	/// </summary>
	/// <param name="gameTime">Provides a snapshot of timing values.</param>
	protected override void Draw(GameTime gameTime)
	{
		isDrawing = true;
		RenderTarget2D renderTarget2D = null;
		if (ShouldDrawOnBuffer())
		{
			renderTarget2D = screen;
		}
		if (uiScreen != null)
		{
			SetRenderTarget(uiScreen);
			base.GraphicsDevice.Clear(Color.Transparent);
			SetRenderTarget(renderTarget2D);
		}
		GameTime time = gameTime;
		DebugTools.BeforeGameDraw(this, ref time);
		_draw(time, renderTarget2D);
		isRenderingScreenBuffer = true;
		renderScreenBuffer(renderTarget2D);
		isRenderingScreenBuffer = false;
		if (uiModeCount != 0)
		{
			log.Warn("WARNING: Mismatched UI Mode Push/Pop counts. Correcting.");
			while (uiModeCount < 0)
			{
				PushUIMode();
			}
			while (uiModeCount > 0)
			{
				PopUIMode();
			}
		}
		base.Draw(gameTime);
		isDrawing = false;
	}

	public virtual bool ShouldDrawOnBuffer()
	{
		if (LocalMultiplayer.IsLocalMultiplayer())
		{
			return true;
		}
		if (options.zoomLevel != 1f)
		{
			return true;
		}
		return false;
	}

	public static bool ShouldShowOnscreenUsernames()
	{
		return false;
	}

	public virtual bool checkCharacterTilesForShadowDrawFlag(Character character)
	{
		if (character is Farmer farmer && farmer.onBridge.Value)
		{
			return true;
		}
		Microsoft.Xna.Framework.Rectangle boundingBox = character.GetBoundingBox();
		boundingBox.Height += 8;
		int num = boundingBox.Right / 64;
		int num2 = boundingBox.Bottom / 64;
		int num3 = boundingBox.Left / 64;
		int num4 = boundingBox.Top / 64;
		for (int i = num3; i <= num; i++)
		{
			for (int j = num4; j <= num2; j++)
			{
				if (currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(new Vector2(i, j)))
				{
					return true;
				}
			}
		}
		return false;
	}

	protected virtual void _draw(GameTime gameTime, RenderTarget2D target_screen)
	{
		debugTimings.StartDrawTimer();
		showingHealthBar = false;
		if (_newDayTask != null || isLocalMultiplayerNewDayActive || ShouldLoadIncrementally)
		{
			base.GraphicsDevice.Clear(bgColor);
			return;
		}
		if (target_screen != null)
		{
			SetRenderTarget(target_screen);
		}
		if (IsSaving)
		{
			base.GraphicsDevice.Clear(bgColor);
			DrawMenu(gameTime, target_screen);
			PushUIMode();
			if (overlayMenu != null)
			{
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				overlayMenu.draw(spriteBatch);
				spriteBatch.End();
			}
			PopUIMode();
			return;
		}
		base.GraphicsDevice.Clear(bgColor);
		if (hooks.OnRendering(RenderSteps.FullScene, spriteBatch, gameTime, target_screen))
		{
			if (gameMode == 11)
			{
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				spriteBatch.DrawString(dialogueFont, content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3685"), new Vector2(16f, 16f), Color.HotPink);
				spriteBatch.DrawString(dialogueFont, content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3686"), new Vector2(16f, 32f), new Color(0, 255, 0));
				spriteBatch.DrawString(dialogueFont, parseText(errorMessage, dialogueFont, graphics.GraphicsDevice.Viewport.Width), new Vector2(16f, 48f), Color.White);
				spriteBatch.End();
				return;
			}
			bool flag = true;
			if (activeClickableMenu != null && options.showMenuBackground && activeClickableMenu.showWithoutTransparencyIfOptionIsSet() && !takingMapScreenshot)
			{
				PushUIMode();
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				if (hooks.OnRendering(RenderSteps.MenuBackground, spriteBatch, gameTime, target_screen))
				{
					activeClickableMenu.drawBackground(spriteBatch);
					flag = false;
				}
				hooks.OnRendered(RenderSteps.MenuBackground, spriteBatch, gameTime, target_screen);
				spriteBatch.End();
				PopUIMode();
			}
			if (currentMinigame != null)
			{
				if (hooks.OnRendering(RenderSteps.Minigame, spriteBatch, gameTime, target_screen))
				{
					currentMinigame.draw(spriteBatch);
					flag = false;
				}
				hooks.OnRendered(RenderSteps.Minigame, spriteBatch, gameTime, target_screen);
			}
			if (gameMode == 6 || (gameMode == 3 && currentLocation == null))
			{
				if (hooks.OnRendering(RenderSteps.LoadingScreen, spriteBatch, gameTime, target_screen))
				{
					DrawLoadScreen(gameTime, target_screen);
				}
				hooks.OnRendered(RenderSteps.LoadingScreen, spriteBatch, gameTime, target_screen);
				flag = false;
			}
			if (showingEndOfNightStuff)
			{
				flag = false;
			}
			else if (gameMode == 0)
			{
				flag = false;
			}
			if (gameMode == 3 && dayOfMonth == 0 && newDay)
			{
				base.Draw(gameTime);
				return;
			}
			if (flag)
			{
				DrawWorld(gameTime, target_screen);
				PushUIMode();
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				if (hooks.OnRendering(RenderSteps.HUD, spriteBatch, gameTime, target_screen))
				{
					if (IsHudDrawn)
					{
						drawHUD();
					}
					if (hudMessages.Count > 0 && !takingMapScreenshot)
					{
						int heightUsed = 0;
						for (int num = hudMessages.Count - 1; num >= 0; num--)
						{
							hudMessages[num].draw(spriteBatch, num, ref heightUsed);
						}
					}
					StardewValley.FirstPerson.FirstPersonManager.Instance.DrawHUD(spriteBatch, gameTime);
					StardewValley.Pokemon.PokemonHUD.Instance.Draw(spriteBatch, gameTime);
				}
				hooks.OnRendered(RenderSteps.HUD, spriteBatch, gameTime, target_screen);
				debugTimings.Draw();
				spriteBatch.End();
				PopUIMode();
			}
			bool flag2 = false;
			if (!takingMapScreenshot)
			{
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				PushUIMode();
				if ((messagePause || globalFade) && dialogueUp)
				{
					flag2 = true;
				}
				else if (dialogueUp && !messagePause && (activeClickableMenu == null || !(activeClickableMenu is DialogueBox)))
				{
					if (hooks.OnRendering(RenderSteps.DialogueBox, spriteBatch, gameTime, target_screen))
					{
						drawDialogueBox();
					}
					hooks.OnRendered(RenderSteps.DialogueBox, spriteBatch, gameTime, target_screen);
				}
				spriteBatch.End();
				PopUIMode();
				DrawGlobalFade(gameTime, target_screen);
				if (flag2)
				{
					spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
					PushUIMode();
					if (hooks.OnRendering(RenderSteps.DialogueBox, spriteBatch, gameTime, target_screen))
					{
						drawDialogueBox();
					}
					hooks.OnRendered(RenderSteps.DialogueBox, spriteBatch, gameTime, target_screen);
					spriteBatch.End();
					PopUIMode();
				}
				DrawScreenOverlaySprites(gameTime, target_screen);
				if (debugMode)
				{
					DrawDebugUIs(gameTime, target_screen);
				}
				DrawMenu(gameTime, target_screen);
			}
			farmEvent?.drawAboveEverything(spriteBatch);
			DrawOverlays(gameTime, target_screen);
		}
		hooks.OnRendered(RenderSteps.FullScene, spriteBatch, gameTime, target_screen);
		debugTimings.StopDrawTimer();
	}

	public virtual void DrawLoadScreen(GameTime time, RenderTarget2D target_screen)
	{
		PushUIMode();
		base.GraphicsDevice.Clear(bgColor);
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		string text = "".PadRight((int)Math.Ceiling(time.TotalGameTime.TotalMilliseconds % 999.0 / 333.0), '.');
		string text2 = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3688");
		string s = text2 + text;
		string text3 = text2 + "... ";
		int widthOfString = SpriteText.getWidthOfString(text3);
		int num = 64;
		int x = 64;
		int y = graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - num;
		SpriteText.drawString(spriteBatch, s, x, y, 999999, widthOfString, num, 1f, 0.88f, junimoText: false, 0, text3);
		spriteBatch.End();
		PopUIMode();
	}

	public virtual void DrawMenu(GameTime time, RenderTarget2D target_screen)
	{
		PushUIMode();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		if (hooks.OnRendering(RenderSteps.Menu, spriteBatch, time, target_screen))
		{
			IClickableMenu menu = activeClickableMenu;
			while (menu != null && hooks.TryDrawMenu(menu, delegate
			{
				menu.draw(spriteBatch);
			}))
			{
				menu = menu.GetChildMenu();
			}
		}
		hooks.OnRendered(RenderSteps.Menu, spriteBatch, time, target_screen);
		spriteBatch.End();
		PopUIMode();
	}

	public virtual void DrawScreenOverlaySprites(GameTime time, RenderTarget2D target_screen)
	{
		if (hooks.OnRendering(RenderSteps.OverlayTemporarySprites, spriteBatch, time, target_screen))
		{
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			foreach (TemporaryAnimatedSprite screenOverlayTempSprite in screenOverlayTempSprites)
			{
				screenOverlayTempSprite.draw(spriteBatch, localPosition: true);
			}
			spriteBatch.End();
			PushUIMode();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			foreach (TemporaryAnimatedSprite uiOverlayTempSprite in uiOverlayTempSprites)
			{
				uiOverlayTempSprite.draw(spriteBatch, localPosition: true);
			}
			spriteBatch.End();
			PopUIMode();
		}
		hooks.OnRendered(RenderSteps.OverlayTemporarySprites, spriteBatch, time, target_screen);
	}

	public virtual void DrawWorld(GameTime time, RenderTarget2D target_screen)
	{
		if (StardewValley.FirstPerson.FirstPersonManager.Instance.ShouldOverrideWorldRender())
		{
			base.GraphicsDevice.Clear(bgColor);
			StardewValley.FirstPerson.FirstPersonManager.Instance.DrawWorld3D(time);
			return;
		}
		if (hooks.OnRendering(RenderSteps.World, spriteBatch, time, target_screen))
		{
			mapDisplayDevice.BeginScene(spriteBatch);
			if (drawLighting)
			{
				DrawLighting(time, target_screen);
			}
			base.GraphicsDevice.Clear(bgColor);
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			if (hooks.OnRendering(RenderSteps.World_Background, spriteBatch, time, target_screen))
			{
				background?.draw(spriteBatch);
				currentLocation.drawBackground(spriteBatch);
				spriteBatch.End();
				for (int i = 0; i < currentLocation.backgroundLayers.Count; i++)
				{
					spriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.PointClamp);
					currentLocation.backgroundLayers[i].Key.Draw(mapDisplayDevice, viewport, Location.Origin, wrapAround: false, 4, -1f);
					spriteBatch.End();
				}
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				currentLocation.drawWater(spriteBatch);
				spriteBatch.End();
				spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
				currentLocation.drawFloorDecorations(spriteBatch);
				spriteBatch.End();
			}
			hooks.OnRendered(RenderSteps.World_Background, spriteBatch, time, target_screen);
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			_farmerShadows.Clear();
			if (currentLocation.currentEvent != null && !currentLocation.currentEvent.isFestival && currentLocation.currentEvent.farmerActors.Count > 0)
			{
				foreach (Farmer farmerActor in currentLocation.currentEvent.farmerActors)
				{
					if ((farmerActor.IsLocalPlayer && displayFarmer) || !farmerActor.hidden.Value)
					{
						_farmerShadows.Add(farmerActor);
					}
				}
			}
			else
			{
				foreach (Farmer farmer in currentLocation.farmers)
				{
					if ((farmer.IsLocalPlayer && displayFarmer) || !farmer.hidden.Value)
					{
						_farmerShadows.Add(farmer);
					}
				}
			}
			if (!currentLocation.shouldHideCharacters())
			{
				if (CurrentEvent == null)
				{
					foreach (NPC character in currentLocation.characters)
					{
						if (!character.swimming.Value && !character.HideShadow && !character.IsInvisible && !checkCharacterTilesForShadowDrawFlag(character))
						{
							character.DrawShadow(spriteBatch);
						}
					}
				}
				else
				{
					foreach (NPC actor in CurrentEvent.actors)
					{
						if ((CurrentEvent == null || !CurrentEvent.ShouldHideCharacter(actor)) && !actor.swimming.Value && !actor.HideShadow && !checkCharacterTilesForShadowDrawFlag(actor))
						{
							actor.DrawShadow(spriteBatch);
						}
					}
				}
				foreach (Farmer farmerShadow in _farmerShadows)
				{
					if (!multiplayer.isDisconnecting(farmerShadow.UniqueMultiplayerID) && !farmerShadow.swimming.Value && !farmerShadow.isRidingHorse() && !farmerShadow.IsSitting() && (currentLocation == null || !checkCharacterTilesForShadowDrawFlag(farmerShadow)))
					{
						farmerShadow.DrawShadow(spriteBatch);
					}
				}
			}
			float num = 0.1f;
			for (int j = 0; j < currentLocation.buildingLayers.Count; j++)
			{
				float num2 = 0f;
				if (currentLocation.buildingLayers.Count > 1)
				{
					num2 = (float)j / (float)(currentLocation.buildingLayers.Count - 1);
				}
				currentLocation.buildingLayers[j].Key.Draw(mapDisplayDevice, viewport, Location.Origin, wrapAround: false, 4, num * num2);
			}
			Layer layer = currentLocation.Map.RequireLayer("Buildings");
			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
			if (hooks.OnRendering(RenderSteps.World_Sorted, spriteBatch, time, target_screen))
			{
				if (!currentLocation.shouldHideCharacters())
				{
					if (CurrentEvent == null)
					{
						foreach (NPC character2 in currentLocation.characters)
						{
							if (!character2.swimming.Value && !character2.HideShadow && !character2.isInvisible.Value && checkCharacterTilesForShadowDrawFlag(character2))
							{
								character2.DrawShadow(spriteBatch);
							}
						}
					}
					else
					{
						foreach (NPC actor2 in CurrentEvent.actors)
						{
							if ((CurrentEvent == null || !CurrentEvent.ShouldHideCharacter(actor2)) && !actor2.swimming.Value && !actor2.HideShadow && checkCharacterTilesForShadowDrawFlag(actor2))
							{
								actor2.DrawShadow(spriteBatch);
							}
						}
					}
					foreach (Farmer farmerShadow2 in _farmerShadows)
					{
						if (!farmerShadow2.swimming.Value && !farmerShadow2.isRidingHorse() && !farmerShadow2.IsSitting() && currentLocation != null && checkCharacterTilesForShadowDrawFlag(farmerShadow2))
						{
							farmerShadow2.DrawShadow(spriteBatch);
						}
					}
				}
				if ((eventUp || killScreen) && !killScreen && currentLocation.currentEvent != null)
				{
					currentLocation.currentEvent.draw(spriteBatch);
				}
				currentLocation.draw(spriteBatch);
				foreach (Vector2 key in crabPotOverlayTiles.Keys)
				{
					Tile tile = layer.Tiles[(int)key.X, (int)key.Y];
					if (tile != null)
					{
						Vector2 vector = GlobalToLocal(viewport, key * 64f);
						Location location = new Location((int)vector.X, (int)vector.Y);
						mapDisplayDevice.DrawTile(tile, location, (key.Y * 64f - 1f) / 10000f);
					}
				}
				if (player.ActiveObject == null && player.UsingTool && player.CurrentTool != null)
				{
					drawTool(player);
				}
				if (panMode)
				{
					spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((int)Math.Floor((double)(getOldMouseX() + viewport.X) / 64.0) * 64 - viewport.X, (int)Math.Floor((double)(getOldMouseY() + viewport.Y) / 64.0) * 64 - viewport.Y, 64, 64), Color.Lime * 0.75f);
					foreach (Warp warp in currentLocation.warps)
					{
						spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(warp.X * 64 - viewport.X, warp.Y * 64 - viewport.Y, 64, 64), Color.Red * 0.75f);
					}
				}
				for (int k = 0; k < currentLocation.frontLayers.Count; k++)
				{
					float num3 = 0f;
					if (currentLocation.frontLayers.Count > 1)
					{
						num3 = (float)k / (float)(currentLocation.frontLayers.Count - 1);
					}
					currentLocation.frontLayers[k].Key.Draw(mapDisplayDevice, viewport, Location.Origin, wrapAround: false, 4, 64f + num * num3);
				}
				currentLocation.drawAboveFrontLayer(spriteBatch);
			}
			hooks.OnRendered(RenderSteps.World_Sorted, spriteBatch, time, target_screen);
			spriteBatch.End();
			if (hooks.OnRendering(RenderSteps.World_AlwaysFront, spriteBatch, time, target_screen))
			{
				for (int l = 0; l < currentLocation.alwaysFrontLayers.Count; l++)
				{
					spriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.PointClamp);
					currentLocation.alwaysFrontLayers[l].Key.Draw(mapDisplayDevice, viewport, Location.Origin, wrapAround: false, 4, -1f);
					spriteBatch.End();
				}
			}
			hooks.OnRendered(RenderSteps.World_AlwaysFront, spriteBatch, time, target_screen);
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			if (currentLocation.LightLevel > 0f && timeOfDay < 2000)
			{
				spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, Color.Black * currentLocation.LightLevel);
			}
			if (screenGlow)
			{
				spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, screenGlowColor * screenGlowAlpha);
			}
			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			currentLocation.drawAboveAlwaysFrontLayer(spriteBatch);
			if (!IsFakedBlackScreen())
			{
				spriteBatch.End();
				drawWeather(time, target_screen);
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			}
			if (player.CurrentTool is FishingRod fishingRod && (fishingRod.isTimingCast || fishingRod.castingChosenCountdown > 0f || fishingRod.fishCaught || fishingRod.showingTreasure))
			{
				player.CurrentTool.draw(spriteBatch);
			}
			spriteBatch.End();
			DrawCharacterEmotes(time, target_screen);
			mapDisplayDevice.EndScene();
			if (drawLighting && !IsFakedBlackScreen())
			{
				DrawLightmapOnScreen(time, target_screen);
			}
			if (!eventUp && farmEvent == null && gameMode == 3 && !takingMapScreenshot && isOutdoorMapSmallerThanViewport())
			{
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(0, 0, -viewport.X, graphics.GraphicsDevice.Viewport.Height), Color.Black);
				spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(-viewport.X + currentLocation.map.Layers[0].LayerWidth * 64, 0, graphics.GraphicsDevice.Viewport.Width - (-viewport.X + currentLocation.map.Layers[0].LayerWidth * 64), graphics.GraphicsDevice.Viewport.Height), Color.Black);
				spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(0, 0, graphics.GraphicsDevice.Viewport.Width, -viewport.Y), Color.Black);
				spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(0, -viewport.Y + currentLocation.map.Layers[0].LayerHeight * 64, graphics.GraphicsDevice.Viewport.Width, graphics.GraphicsDevice.Viewport.Height - (-viewport.Y + currentLocation.map.Layers[0].LayerHeight * 64)), Color.Black);
				spriteBatch.End();
			}
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			if (currentLocation != null && currentLocation.isOutdoors.Value && !IsFakedBlackScreen() && currentLocation.IsRainingHere())
			{
				bool flag = IsGreenRainingHere();
				spriteBatch.Draw(staminaRect, graphics.GraphicsDevice.Viewport.Bounds, flag ? (new Color(0, 120, 150) * 0.22f) : (Color.Blue * 0.2f));
			}
			spriteBatch.End();
			if (farmEvent != null)
			{
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				farmEvent.draw(spriteBatch);
				spriteBatch.End();
			}
			if (eventUp && currentLocation?.currentEvent != null)
			{
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				currentLocation.currentEvent.drawAfterMap(spriteBatch);
				spriteBatch.End();
			}
			if (!takingMapScreenshot)
			{
				if (drawGrid)
				{
					spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
					int num4 = -viewport.X % 64;
					float num5 = -viewport.Y % 64;
					for (int m = num4; m < graphics.GraphicsDevice.Viewport.Width; m += 64)
					{
						spriteBatch.Draw(staminaRect, new Microsoft.Xna.Framework.Rectangle(m, (int)num5, 1, graphics.GraphicsDevice.Viewport.Height), Color.Red * 0.5f);
					}
					for (float num6 = num5; num6 < (float)graphics.GraphicsDevice.Viewport.Height; num6 += 64f)
					{
						spriteBatch.Draw(staminaRect, new Microsoft.Xna.Framework.Rectangle(num4, (int)num6, graphics.GraphicsDevice.Viewport.Width, 1), Color.Red * 0.5f);
					}
					spriteBatch.End();
				}
				if (ShouldShowOnscreenUsernames() && currentLocation != null)
				{
					spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
					currentLocation.DrawFarmerUsernames(spriteBatch);
					spriteBatch.End();
				}
				if (flashAlpha > 0f)
				{
					if (options.screenFlash)
					{
						spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
						spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, Color.White * Math.Min(1f, flashAlpha));
						spriteBatch.End();
					}
					flashAlpha -= 0.1f;
				}
			}
		}
		hooks.OnRendered(RenderSteps.World, spriteBatch, time, target_screen);
	}

	public virtual void DrawCharacterEmotes(GameTime time, RenderTarget2D target_screen)
	{
		if (!eventUp || currentLocation.currentEvent == null)
		{
			return;
		}
		spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
		foreach (NPC actor in currentLocation.currentEvent.actors)
		{
			actor.DrawEmote(spriteBatch);
		}
		spriteBatch.End();
	}

	public virtual void DrawLightmapOnScreen(GameTime time, RenderTarget2D target_screen)
	{
		if (hooks.OnRendering(RenderSteps.World_DrawLightmapOnScreen, spriteBatch, time, target_screen))
		{
			spriteBatch.Begin(SpriteSortMode.Deferred, lightingBlend, SamplerState.LinearClamp);
			Viewport viewport = base.GraphicsDevice.Viewport;
			viewport.Bounds = target_screen?.Bounds ?? base.GraphicsDevice.PresentationParameters.Bounds;
			base.GraphicsDevice.Viewport = viewport;
			float num = options.lightingQuality / 2;
			if (useUnscaledLighting)
			{
				num /= options.zoomLevel;
			}
			spriteBatch.Draw(lightmap, Vector2.Zero, lightmap.Bounds, Color.White, 0f, Vector2.Zero, num, SpriteEffects.None, 1f);
			if (currentLocation.isOutdoors.Value && currentLocation.IsRainingHere())
			{
				spriteBatch.Draw(lightingRect, viewport.Bounds, Color.OrangeRed * 0.45f);
			}
		}
		hooks.OnRendered(RenderSteps.World_DrawLightmapOnScreen, spriteBatch, time, target_screen);
		spriteBatch.End();
	}

	public virtual void DrawDebugUIs(GameTime time, RenderTarget2D target_screen)
	{
		StringBuilder debugStringBuilder = _debugStringBuilder;
		debugStringBuilder.Clear();
		if (panMode)
		{
			debugStringBuilder.Append((getOldMouseX() + viewport.X) / 64);
			debugStringBuilder.Append(",");
			debugStringBuilder.Append((getOldMouseY() + viewport.Y) / 64);
		}
		else
		{
			Point standingPixel = player.StandingPixel;
			debugStringBuilder.Append("player: ");
			debugStringBuilder.Append(standingPixel.X / 64);
			debugStringBuilder.Append(", ");
			debugStringBuilder.Append(standingPixel.Y / 64);
		}
		debugStringBuilder.Append(" mouseTransparency: ");
		debugStringBuilder.Append(mouseCursorTransparency);
		debugStringBuilder.Append(" mousePosition: ");
		debugStringBuilder.Append(getMouseX());
		debugStringBuilder.Append(",");
		debugStringBuilder.Append(getMouseY());
		debugStringBuilder.Append(Environment.NewLine);
		debugStringBuilder.Append(" mouseWorldPosition: ");
		debugStringBuilder.Append(getMouseX() + viewport.X);
		debugStringBuilder.Append(",");
		debugStringBuilder.Append(getMouseY() + viewport.Y);
		debugStringBuilder.Append("  debugOutput: ");
		debugStringBuilder.Append(debugOutput);
		PushUIMode();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		spriteBatch.DrawString(smallFont, debugStringBuilder, new Vector2(base.GraphicsDevice.Viewport.GetTitleSafeArea().X, base.GraphicsDevice.Viewport.GetTitleSafeArea().Y + smallFont.LineSpacing * 8), Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999999f);
		spriteBatch.End();
		PopUIMode();
	}

	public virtual void DrawGlobalFade(GameTime time, RenderTarget2D target_screen)
	{
		if ((fadeToBlack || globalFade) && !takingMapScreenshot)
		{
			PushUIMode();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			if (hooks.OnRendering(RenderSteps.GlobalFade, spriteBatch, time, target_screen))
			{
				spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, Color.Black * ((gameMode == 0) ? (1f - fadeToBlackAlpha) : fadeToBlackAlpha));
			}
			hooks.OnRendered(RenderSteps.GlobalFade, spriteBatch, time, target_screen);
			spriteBatch.End();
			PopUIMode();
		}
	}

	public virtual void DrawLighting(GameTime time, RenderTarget2D target_screen)
	{
		SetRenderTarget(lightmap);
		base.GraphicsDevice.Clear(Color.White * 0f);
		Matrix value = Matrix.Identity;
		if (useUnscaledLighting)
		{
			value = Matrix.CreateScale(options.zoomLevel);
		}
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, value);
		if (hooks.OnRendering(RenderSteps.World_RenderLightmap, spriteBatch, time, target_screen))
		{
			Color color = ((!(currentLocation is MineShaft mineShaft)) ? ((ambientLight.Equals(Color.White) || (currentLocation.isOutdoors.Value && currentLocation.IsRainingHere())) ? outdoorLight : ambientLight) : mineShaft.getLightingColor(time));
			float lightMultiplier = 1f;
			if (player.hasBuff("26"))
			{
				if (color == Color.White)
				{
					color = new Color(0.75f, 0.75f, 0.75f);
				}
				else
				{
					color.R = (byte)Utility.Lerp((int)color.R, 255f, 0.5f);
					color.G = (byte)Utility.Lerp((int)color.G, 255f, 0.5f);
					color.B = (byte)Utility.Lerp((int)color.B, 255f, 0.5f);
				}
				lightMultiplier = 0.33f;
			}
			if (IsGreenRainingHere())
			{
				color.R = (byte)Utility.Lerp((int)color.R, 255f, 0.25f);
				color.G = (byte)Utility.Lerp((int)color.R, 0f, 0.25f);
			}
			spriteBatch.Draw(staminaRect, lightmap.Bounds, color);
			foreach (KeyValuePair<string, LightSource> currentLightSource in currentLightSources)
			{
				currentLightSource.Value.Draw(spriteBatch, currentLocation, lightMultiplier);
			}
		}
		hooks.OnRendered(RenderSteps.World_RenderLightmap, spriteBatch, time, target_screen);
		spriteBatch.End();
		SetRenderTarget(target_screen);
	}

	public virtual void drawWeather(GameTime time, RenderTarget2D target_screen)
	{
		spriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.PointClamp);
		if (hooks.OnRendering(RenderSteps.World_Weather, spriteBatch, time, target_screen) && currentLocation.IsOutdoors)
		{
			if (currentLocation.IsSnowingHere())
			{
				snowPos.X %= 64f;
				Vector2 position = default(Vector2);
				for (float num = -64f + snowPos.X % 64f; num < (float)viewport.Width; num += 64f)
				{
					for (float num2 = -64f + snowPos.Y % 64f; num2 < (float)viewport.Height; num2 += 64f)
					{
						position.X = (int)num;
						position.Y = (int)num2;
						spriteBatch.Draw(mouseCursors, position, new Microsoft.Xna.Framework.Rectangle(368 + (int)(currentGameTime.TotalGameTime.TotalMilliseconds % 1200.0) / 75 * 16, 192, 16, 16), Color.White * 0.8f * options.snowTransparency, 0f, Vector2.Zero, 4.001f, SpriteEffects.None, 1f);
					}
				}
			}
			if (!currentLocation.ignoreDebrisWeather.Value && currentLocation.IsDebrisWeatherHere())
			{
				if (takingMapScreenshot)
				{
					if (debrisWeather != null)
					{
						foreach (WeatherDebris item in debrisWeather)
						{
							Vector2 position2 = item.position;
							item.position = new Vector2(random.Next(viewport.Width - item.sourceRect.Width * 3), random.Next(viewport.Height - item.sourceRect.Height * 3));
							item.draw(spriteBatch);
							item.position = position2;
						}
					}
				}
				else if (viewport.X > -viewport.Width)
				{
					foreach (WeatherDebris item2 in debrisWeather)
					{
						item2.draw(spriteBatch);
					}
				}
			}
			if (currentLocation.IsRainingHere() && !(currentLocation is Summit) && (!eventUp || currentLocation.isTileOnMap(new Vector2(viewport.X / 64, viewport.Y / 64))))
			{
				bool flag = IsGreenRainingHere();
				Color color = (flag ? Color.LimeGreen : Color.White);
				int num3 = ((!flag) ? 1 : 2);
				for (int i = 0; i < rainDrops.Length; i++)
				{
					for (int j = 0; j < num3; j++)
					{
						spriteBatch.Draw(rainTexture, rainDrops[i].position, getSourceRectForStandardTileSheet(rainTexture, rainDrops[i].frame + (flag ? 4 : 0), 16, 16), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
					}
				}
			}
		}
		hooks.OnRendered(RenderSteps.World_Weather, spriteBatch, time, target_screen);
		spriteBatch.End();
	}

	protected virtual void renderScreenBuffer(RenderTarget2D target_screen)
	{
		graphics.GraphicsDevice.SetRenderTarget(null);
		if (!takingMapScreenshot && !LocalMultiplayer.IsLocalMultiplayer() && (target_screen == null || !target_screen.IsContentLost))
		{
			if (ShouldDrawOnBuffer() && target_screen != null)
			{
				base.GraphicsDevice.Clear(bgColor);
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
				spriteBatch.Draw(target_screen, new Vector2(0f, 0f), target_screen.Bounds, Color.White, 0f, Vector2.Zero, options.zoomLevel, SpriteEffects.None, 1f);
				spriteBatch.End();
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
				spriteBatch.Draw(uiScreen, new Vector2(0f, 0f), uiScreen.Bounds, Color.White, 0f, Vector2.Zero, options.uiScale, SpriteEffects.None, 1f);
				spriteBatch.End();
			}
			else
			{
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
				spriteBatch.Draw(uiScreen, new Vector2(0f, 0f), uiScreen.Bounds, Color.White, 0f, Vector2.Zero, options.uiScale, SpriteEffects.None, 1f);
				spriteBatch.End();
			}
		}
	}

	public virtual void DrawSplitScreenWindow()
	{
		if (!LocalMultiplayer.IsLocalMultiplayer())
		{
			return;
		}
		graphics.GraphicsDevice.SetRenderTarget(null);
		if (screen == null || !screen.IsContentLost)
		{
			Viewport viewport = base.GraphicsDevice.Viewport;
			GraphicsDevice graphicsDevice = base.GraphicsDevice;
			Viewport viewport2 = (base.GraphicsDevice.Viewport = defaultDeviceViewport);
			graphicsDevice.Viewport = viewport2;
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
			spriteBatch.Draw(screen, new Vector2(localMultiplayerWindow.X, localMultiplayerWindow.Y), screen.Bounds, Color.White, 0f, Vector2.Zero, instanceOptions.zoomLevel, SpriteEffects.None, 1f);
			if (uiScreen != null)
			{
				spriteBatch.Draw(uiScreen, new Vector2(localMultiplayerWindow.X, localMultiplayerWindow.Y), uiScreen.Bounds, Color.White, 0f, Vector2.Zero, instanceOptions.uiScale, SpriteEffects.None, 1f);
			}
			spriteBatch.End();
			base.GraphicsDevice.Viewport = viewport;
		}
	}

	/// ###########################
	/// METHODS FOR DRAWING THINGS.
	/// ############################
	public static void drawWithBorder(string message, Color borderColor, Color insideColor, Vector2 position)
	{
		drawWithBorder(message, borderColor, insideColor, position, 0f, 1f, 1f, tiny: false);
	}

	public static void drawWithBorder(string message, Color borderColor, Color insideColor, Vector2 position, float rotate, float scale, float layerDepth)
	{
		drawWithBorder(message, borderColor, insideColor, position, rotate, scale, layerDepth, tiny: false);
	}

	public static void drawWithBorder(string message, Color borderColor, Color insideColor, Vector2 position, float rotate, float scale, float layerDepth, bool tiny)
	{
		string[] array = ArgUtility.SplitBySpace(message);
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Contains('='))
			{
				spriteBatch.DrawString(tiny ? tinyFont : dialogueFont, array[i], new Vector2(position.X + (float)num, position.Y), Color.Purple, rotate, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
				num += (int)((tiny ? tinyFont : dialogueFont).MeasureString(array[i]).X + 8f);
			}
			else
			{
				spriteBatch.DrawString(tiny ? tinyFont : dialogueFont, array[i], new Vector2(position.X + (float)num, position.Y), insideColor, rotate, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
				num += (int)((tiny ? tinyFont : dialogueFont).MeasureString(array[i]).X + 8f);
			}
		}
	}

	public static bool isOutdoorMapSmallerThanViewport()
	{
		if (uiMode)
		{
			return false;
		}
		if (currentLocation != null && currentLocation.IsOutdoors && !(currentLocation is Summit))
		{
			if (currentLocation.map.Layers[0].LayerWidth * 64 >= viewport.Width)
			{
				return currentLocation.map.Layers[0].LayerHeight * 64 < viewport.Height;
			}
			return true;
		}
		return false;
	}

	protected virtual void drawHUD()
	{
		if (eventUp || farmEvent != null)
		{
			return;
		}
		float num = 0.625f;
		Vector2 vector = new Vector2(graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Right - 48 - 8, graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 224 - 16 - (int)((float)(player.MaxStamina - 270) * num));
		if (isOutdoorMapSmallerThanViewport())
		{
			vector.X = Math.Min(vector.X, -viewport.X + currentLocation.map.Layers[0].LayerWidth * 64 - 48);
		}
		if (staminaShakeTimer > 0)
		{
			vector.X += random.Next(-3, 4);
			vector.Y += random.Next(-3, 4);
		}
		spriteBatch.Draw(mouseCursors, vector, new Microsoft.Xna.Framework.Rectangle(256, 408, 12, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		spriteBatch.Draw(mouseCursors, new Microsoft.Xna.Framework.Rectangle((int)vector.X, (int)(vector.Y + 64f), 48, graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 64 - 16 - (int)(vector.Y + 64f - 8f)), new Microsoft.Xna.Framework.Rectangle(256, 424, 12, 16), Color.White);
		spriteBatch.Draw(mouseCursors, new Vector2(vector.X, vector.Y + 224f + (float)(int)((float)(player.MaxStamina - 270) * num) - 64f), new Microsoft.Xna.Framework.Rectangle(256, 448, 12, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		Microsoft.Xna.Framework.Rectangle destinationRectangle = new Microsoft.Xna.Framework.Rectangle((int)vector.X + 12, (int)vector.Y + 16 + 32 + (int)((float)player.MaxStamina * num) - (int)(Math.Max(0f, player.Stamina) * num), 24, (int)(player.Stamina * num) - 1);
		if ((float)getOldMouseX() >= vector.X && (float)getOldMouseY() >= vector.Y)
		{
			drawWithBorder((int)Math.Max(0f, player.Stamina) + "/" + player.MaxStamina, Color.Black * 0f, Color.White, vector + new Vector2(0f - dialogueFont.MeasureString("999/999").X - 16f - (float)(showingHealth ? 64 : 0), 64f));
		}
		Color redToGreenLerpColor = Utility.getRedToGreenLerpColor(player.stamina / (float)player.maxStamina.Value);
		spriteBatch.Draw(staminaRect, destinationRectangle, redToGreenLerpColor);
		destinationRectangle.Height = 4;
		redToGreenLerpColor.R = (byte)Math.Max(0, redToGreenLerpColor.R - 50);
		redToGreenLerpColor.G = (byte)Math.Max(0, redToGreenLerpColor.G - 50);
		spriteBatch.Draw(staminaRect, destinationRectangle, redToGreenLerpColor);
		if (player.exhausted.Value)
		{
			spriteBatch.Draw(mouseCursors, vector - new Vector2(0f, 11f) * 4f, new Microsoft.Xna.Framework.Rectangle(191, 406, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			if ((float)getOldMouseX() >= vector.X && (float)getOldMouseY() >= vector.Y - 44f)
			{
				drawWithBorder(content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3747"), Color.Black * 0f, Color.White, vector + new Vector2(0f - dialogueFont.MeasureString(content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3747")).X - 16f - (float)(showingHealth ? 64 : 0), 96f));
			}
		}
		if (currentLocation is MineShaft || currentLocation is Woods || currentLocation is SlimeHutch || currentLocation is VolcanoDungeon || player.health < player.maxHealth)
		{
			showingHealthBar = true;
			showingHealth = true;
			int num2 = 168 + (player.maxHealth - 100);
			int num3 = (int)((float)player.health / (float)player.maxHealth * (float)num2);
			vector.X -= 56 + ((hitShakeTimer > 0) ? random.Next(-3, 4) : 0);
			vector.Y = graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 224 - 16 - (player.maxHealth - 100);
			spriteBatch.Draw(mouseCursors, vector, new Microsoft.Xna.Framework.Rectangle(268, 408, 12, 16), (player.health < 20) ? (Color.Pink * ((float)Math.Sin(currentGameTime.TotalGameTime.TotalMilliseconds / (double)((float)player.health * 50f)) / 4f + 0.9f)) : Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			spriteBatch.Draw(mouseCursors, new Microsoft.Xna.Framework.Rectangle((int)vector.X, (int)(vector.Y + 64f), 48, graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 64 - 16 - (int)(vector.Y + 64f)), new Microsoft.Xna.Framework.Rectangle(268, 424, 12, 16), (player.health < 20) ? (Color.Pink * ((float)Math.Sin(currentGameTime.TotalGameTime.TotalMilliseconds / (double)((float)player.health * 50f)) / 4f + 0.9f)) : Color.White);
			spriteBatch.Draw(mouseCursors, new Vector2(vector.X, vector.Y + 224f + (float)(player.maxHealth - 100) - 64f), new Microsoft.Xna.Framework.Rectangle(268, 448, 12, 16), (player.health < 20) ? (Color.Pink * ((float)Math.Sin(currentGameTime.TotalGameTime.TotalMilliseconds / (double)((float)player.health * 50f)) / 4f + 0.9f)) : Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			Microsoft.Xna.Framework.Rectangle destinationRectangle2 = new Microsoft.Xna.Framework.Rectangle((int)vector.X + 12, (int)vector.Y + 16 + 32 + num2 - num3, 24, num3);
			redToGreenLerpColor = Utility.getRedToGreenLerpColor((float)player.health / (float)player.maxHealth);
			spriteBatch.Draw(staminaRect, destinationRectangle2, staminaRect.Bounds, redToGreenLerpColor, 0f, Vector2.Zero, SpriteEffects.None, 1f);
			redToGreenLerpColor.R = (byte)Math.Max(0, redToGreenLerpColor.R - 50);
			redToGreenLerpColor.G = (byte)Math.Max(0, redToGreenLerpColor.G - 50);
			if ((float)getOldMouseX() >= vector.X && (float)getOldMouseY() >= vector.Y && (float)getOldMouseX() < vector.X + 32f)
			{
				drawWithBorder(Math.Max(0, player.health) + "/" + player.maxHealth, Color.Black * 0f, Color.Red, vector + new Vector2(0f - dialogueFont.MeasureString("999/999").X - 32f, 64f));
			}
			destinationRectangle2.Height = 4;
			spriteBatch.Draw(staminaRect, destinationRectangle2, staminaRect.Bounds, redToGreenLerpColor, 0f, Vector2.Zero, SpriteEffects.None, 1f);
		}
		else
		{
			showingHealth = false;
		}
		foreach (IClickableMenu onScreenMenu in onScreenMenus)
		{
			if (onScreenMenu != chatBox)
			{
				onScreenMenu.update(currentGameTime);
				onScreenMenu.draw(spriteBatch);
			}
		}
		if (!player.professions.Contains(17) || !currentLocation.IsOutdoors)
		{
			return;
		}
		foreach (KeyValuePair<Vector2, Object> pair in currentLocation.objects.Pairs)
		{
			if ((pair.Value.isSpawnedObject.Value || pair.Value.QualifiedItemId == "(O)590") && !Utility.isOnScreen(pair.Key * 64f + new Vector2(32f, 32f), 64))
			{
				Microsoft.Xna.Framework.Rectangle bounds = graphics.GraphicsDevice.Viewport.Bounds;
				Vector2 renderPos = default(Vector2);
				float num4 = 0f;
				if (pair.Key.X * 64f > (float)(viewport.MaxCorner.X - 64))
				{
					renderPos.X = bounds.Right - 8;
					num4 = (float)Math.PI / 2f;
				}
				else if (pair.Key.X * 64f < (float)viewport.X)
				{
					renderPos.X = 8f;
					num4 = -(float)Math.PI / 2f;
				}
				else
				{
					renderPos.X = pair.Key.X * 64f - (float)viewport.X;
				}
				if (pair.Key.Y * 64f > (float)(viewport.MaxCorner.Y - 64))
				{
					renderPos.Y = bounds.Bottom - 8;
					num4 = (float)Math.PI;
				}
				else if (pair.Key.Y * 64f < (float)viewport.Y)
				{
					renderPos.Y = 8f;
				}
				else
				{
					renderPos.Y = pair.Key.Y * 64f - (float)viewport.Y;
				}
				if (renderPos.X == 8f && renderPos.Y == 8f)
				{
					num4 += (float)Math.PI / 4f;
				}
				if (renderPos.X == 8f && renderPos.Y == (float)(bounds.Bottom - 8))
				{
					num4 += (float)Math.PI / 4f;
				}
				if (renderPos.X == (float)(bounds.Right - 8) && renderPos.Y == 8f)
				{
					num4 -= (float)Math.PI / 4f;
				}
				if (renderPos.X == (float)(bounds.Right - 8) && renderPos.Y == (float)(bounds.Bottom - 8))
				{
					num4 -= (float)Math.PI / 4f;
				}
				Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(412, 495, 5, 4);
				float num5 = 4f;
				Vector2 position = Utility.makeSafe(renderSize: new Vector2((float)value.Width * num5, (float)value.Height * num5), renderPos: renderPos);
				spriteBatch.Draw(mouseCursors, position, value, Color.White, num4, new Vector2(2f, 2f), num5, SpriteEffects.None, 1f);
			}
		}
		if (!currentLocation.orePanPoint.Equals(Point.Zero) && !Utility.isOnScreen(Utility.PointToVector2(currentLocation.orePanPoint.Value) * 64f + new Vector2(32f, 32f), 64))
		{
			Vector2 position2 = default(Vector2);
			float num6 = 0f;
			if (currentLocation.orePanPoint.X * 64 > viewport.MaxCorner.X - 64)
			{
				position2.X = graphics.GraphicsDevice.Viewport.Bounds.Right - 8;
				num6 = (float)Math.PI / 2f;
			}
			else if (currentLocation.orePanPoint.X * 64 < viewport.X)
			{
				position2.X = 8f;
				num6 = -(float)Math.PI / 2f;
			}
			else
			{
				position2.X = currentLocation.orePanPoint.X * 64 - viewport.X;
			}
			if (currentLocation.orePanPoint.Y * 64 > viewport.MaxCorner.Y - 64)
			{
				position2.Y = graphics.GraphicsDevice.Viewport.Bounds.Bottom - 8;
				num6 = (float)Math.PI;
			}
			else if (currentLocation.orePanPoint.Y * 64 < viewport.Y)
			{
				position2.Y = 8f;
			}
			else
			{
				position2.Y = currentLocation.orePanPoint.Y * 64 - viewport.Y;
			}
			if (position2.X == 8f && position2.Y == 8f)
			{
				num6 += (float)Math.PI / 4f;
			}
			if (position2.X == 8f && position2.Y == (float)(graphics.GraphicsDevice.Viewport.Bounds.Bottom - 8))
			{
				num6 += (float)Math.PI / 4f;
			}
			if (position2.X == (float)(graphics.GraphicsDevice.Viewport.Bounds.Right - 8) && position2.Y == 8f)
			{
				num6 -= (float)Math.PI / 4f;
			}
			if (position2.X == (float)(graphics.GraphicsDevice.Viewport.Bounds.Right - 8) && position2.Y == (float)(graphics.GraphicsDevice.Viewport.Bounds.Bottom - 8))
			{
				num6 -= (float)Math.PI / 4f;
			}
			spriteBatch.Draw(mouseCursors, position2, new Microsoft.Xna.Framework.Rectangle(412, 495, 5, 4), Color.Cyan, num6, new Vector2(2f, 2f), 4f, SpriteEffects.None, 1f);
		}
	}

	public static void InvalidateOldMouseMovement()
	{
		MouseState mouseState = input.GetMouseState();
		oldMouseState = new MouseState(mouseState.X, mouseState.Y, oldMouseState.ScrollWheelValue, oldMouseState.LeftButton, oldMouseState.MiddleButton, oldMouseState.RightButton, oldMouseState.XButton1, oldMouseState.XButton2);
	}

	public static bool IsRenderingNonNativeUIScale()
	{
		return options.uiScale != options.zoomLevel;
	}

	public virtual void drawMouseCursor()
	{
		if (FirstPerson.FirstPersonManager.Instance.IsEnabled && FirstPerson.FirstPersonManager.Instance.Controller.IsMouseLocked)
		{
			return;
		}
		if (activeClickableMenu == null && timerUntilMouseFade > 0)
		{
			timerUntilMouseFade -= currentGameTime.ElapsedGameTime.Milliseconds;
			lastMousePositionBeforeFade = getMousePosition();
		}
		if (options.gamepadControls && timerUntilMouseFade <= 0 && activeClickableMenu == null && (emoteMenu == null || emoteMenu.gamepadMode))
		{
			mouseCursorTransparency = 0f;
		}
		if (activeClickableMenu == null && mouseCursor > cursor_none && currentLocation != null)
		{
			if (IsRenderingNonNativeUIScale())
			{
				spriteBatch.End();
				PopUIMode();
				if (ShouldDrawOnBuffer())
				{
					SetRenderTarget(screen);
				}
				else
				{
					SetRenderTarget(null);
				}
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			}
			if (!(mouseCursorTransparency > 0f) || !Utility.canGrabSomethingFromHere(getOldMouseX() + viewport.X, getOldMouseY() + viewport.Y, player) || mouseCursor == cursor_gift)
			{
				if (player.ActiveObject != null && mouseCursor != cursor_gift && !eventUp && currentMinigame == null && !player.isRidingHorse() && player.CanMove && displayFarmer)
				{
					if (mouseCursorTransparency > 0f || options.showPlacementTileForGamepad)
					{
						player.ActiveObject.drawPlacementBounds(spriteBatch, currentLocation);
						if (mouseCursorTransparency > 0f)
						{
							spriteBatch.End();
							PushUIMode();
							spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
							bool flag = Utility.playerCanPlaceItemHere(currentLocation, player.CurrentItem, getMouseX() + viewport.X, getMouseY() + viewport.Y, player) || (Utility.isThereAnObjectHereWhichAcceptsThisItem(currentLocation, player.CurrentItem, getMouseX() + viewport.X, getMouseY() + viewport.Y) && Utility.withinRadiusOfPlayer(getMouseX() + viewport.X, getMouseY() + viewport.Y, 1, player));
							player.CurrentItem?.drawInMenu(spriteBatch, new Vector2(getMouseX() + 16, getMouseY() + 16), flag ? (dialogueButtonScale / 75f + 1f) : 1f, flag ? 1f : 0.5f, 0.999f);
							spriteBatch.End();
							PopUIMode();
							spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
						}
					}
				}
				else if (mouseCursor == cursor_default && isActionAtCurrentCursorTile && currentMinigame == null)
				{
					mouseCursor = (isSpeechAtCurrentCursorTile ? cursor_talk : (isInspectionAtCurrentCursorTile ? cursor_look : cursor_grab));
				}
				else if (mouseCursorTransparency > 0f)
				{
					NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>> animals = currentLocation.animals;
					if (animals != null)
					{
						Vector2 vector = new Vector2(getOldMouseX() + uiViewport.X, getOldMouseY() + uiViewport.Y);
						bool flag2 = Utility.withinRadiusOfPlayer((int)vector.X, (int)vector.Y, 1, player);
						foreach (KeyValuePair<long, FarmAnimal> pair in animals.Pairs)
						{
							Microsoft.Xna.Framework.Rectangle cursorPetBoundingBox = pair.Value.GetCursorPetBoundingBox();
							if (!pair.Value.wasPet.Value && cursorPetBoundingBox.Contains((int)vector.X, (int)vector.Y))
							{
								mouseCursor = cursor_grab;
								if (!flag2)
								{
									mouseCursorTransparency = 0.5f;
								}
								break;
							}
						}
					}
				}
			}
			if (IsRenderingNonNativeUIScale())
			{
				spriteBatch.End();
				PushUIMode();
				SetRenderTarget(uiScreen);
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			}
			if (currentMinigame != null)
			{
				mouseCursor = cursor_default;
			}
			if (!freezeControls && !options.hardwareCursor)
			{
				spriteBatch.Draw(mouseCursors, new Vector2(getMouseX(), getMouseY()), getSourceRectForStandardTileSheet(mouseCursors, mouseCursor, 16, 16), Color.White * mouseCursorTransparency, 0f, Vector2.Zero, 4f + dialogueButtonScale / 150f, SpriteEffects.None, 1f);
			}
			wasMouseVisibleThisFrame = mouseCursorTransparency > 0f;
			_lastDrewMouseCursor = wasMouseVisibleThisFrame;
		}
		mouseCursor = cursor_default;
		if (!isActionAtCurrentCursorTile && activeClickableMenu == null)
		{
			mouseCursorTransparency = 1f;
		}
	}

	public static void panScreen(int x, int y)
	{
		int num = uiModeCount;
		while (uiModeCount > 0)
		{
			PopUIMode();
		}
		previousViewportPosition.X = viewport.Location.X;
		previousViewportPosition.Y = viewport.Location.Y;
		viewport.X += x;
		viewport.Y += y;
		clampViewportToGameMap();
		updateRaindropPosition();
		for (int i = 0; i < num; i++)
		{
			PushUIMode();
		}
	}

	public static void clampViewportToGameMap()
	{
		if (viewport.X < 0)
		{
			viewport.X = 0;
		}
		if (viewport.X > currentLocation.map.DisplayWidth - viewport.Width)
		{
			viewport.X = currentLocation.map.DisplayWidth - viewport.Width;
		}
		if (viewport.Y < 0)
		{
			viewport.Y = 0;
		}
		if (viewport.Y > currentLocation.map.DisplayHeight - viewport.Height)
		{
			viewport.Y = currentLocation.map.DisplayHeight - viewport.Height;
		}
	}

	protected void drawDialogueBox()
	{
		if (currentSpeaker != null)
		{
			int val = (int)dialogueFont.MeasureString(currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue()).Y;
			val = Math.Max(val, 320);
			drawDialogueBox((base.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Math.Min(1280, base.GraphicsDevice.Viewport.GetTitleSafeArea().Width - 128)) / 2, base.GraphicsDevice.Viewport.GetTitleSafeArea().Height - val, Math.Min(1280, base.GraphicsDevice.Viewport.GetTitleSafeArea().Width - 128), val, speaker: true, drawOnlyBox: false, null, objectDialoguePortraitPerson != null && currentSpeaker == null);
		}
	}

	public static void drawDialogueBox(string message)
	{
		drawDialogueBox(viewport.Width / 2, viewport.Height / 2, speaker: false, drawOnlyBox: false, message);
	}

	public static void drawDialogueBox(int centerX, int centerY, bool speaker, bool drawOnlyBox, string message)
	{
		string text = null;
		if (speaker && currentSpeaker != null)
		{
			text = currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue();
		}
		else if (message != null)
		{
			text = message;
		}
		else if (currentObjectDialogue.Count > 0)
		{
			text = currentObjectDialogue.Peek();
		}
		if (text != null)
		{
			Vector2 vector = dialogueFont.MeasureString(text);
			int num = (int)vector.X + 128;
			int num2 = (int)vector.Y + 128;
			int x = centerX - num / 2;
			int y = centerY - num2 / 2;
			drawDialogueBox(x, y, num, num2, speaker, drawOnlyBox, message, objectDialoguePortraitPerson != null && !speaker);
		}
	}

	public static void DrawBox(int x, int y, int width, int height, Color? color = null)
	{
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64);
		value.X = 64;
		value.Y = 128;
		Texture2D texture = menuTexture;
		Color color2 = Color.White;
		Color color3 = Color.White;
		if (color.HasValue)
		{
			color2 = color.Value;
			texture = uncoloredMenuTexture;
			color3 = new Color((int)Utility.Lerp((int)color2.R, Math.Min(255, color2.R + 150), 0.65f), (int)Utility.Lerp((int)color2.G, Math.Min(255, color2.G + 150), 0.65f), (int)Utility.Lerp((int)color2.B, Math.Min(255, color2.B + 150), 0.65f));
		}
		spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(x, y, width, height), value, color3);
		value.Y = 0;
		Vector2 vector = new Vector2((float)(-value.Width) * 0.5f, (float)(-value.Height) * 0.5f);
		value.X = 0;
		spriteBatch.Draw(texture, new Vector2((float)x + vector.X, (float)y + vector.Y), value, color2);
		value.X = 192;
		spriteBatch.Draw(texture, new Vector2((float)x + vector.X + (float)width, (float)y + vector.Y), value, color2);
		value.Y = 192;
		spriteBatch.Draw(texture, new Vector2((float)(x + width) + vector.X, (float)(y + height) + vector.Y), value, color2);
		value.X = 0;
		spriteBatch.Draw(texture, new Vector2((float)x + vector.X, (float)(y + height) + vector.Y), value, color2);
		value.X = 128;
		value.Y = 0;
		spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(64 + x + (int)vector.X, y + (int)vector.Y, width - 64, 64), value, color2);
		value.Y = 192;
		spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(64 + x + (int)vector.X, y + (int)vector.Y + height, width - 64, 64), value, color2);
		value.Y = 128;
		value.X = 0;
		spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(x + (int)vector.X, y + (int)vector.Y + 64, 64, height - 64), value, color2);
		value.X = 192;
		spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(x + width + (int)vector.X, y + (int)vector.Y + 64, 64, height - 64), value, color2);
	}

	public static void drawDialogueBox(int x, int y, int width, int height, bool speaker, bool drawOnlyBox, string message = null, bool objectDialogueWithPortrait = false, bool ignoreTitleSafe = true, int r = -1, int g = -1, int b = -1)
	{
		if (!drawOnlyBox)
		{
			return;
		}
		Microsoft.Xna.Framework.Rectangle titleSafeArea = graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
		int height2 = titleSafeArea.Height;
		int width2 = titleSafeArea.Width;
		int num = 0;
		int num2 = 0;
		if (!ignoreTitleSafe)
		{
			num2 = ((y <= titleSafeArea.Y) ? (titleSafeArea.Y - y) : 0);
		}
		int num3 = 0;
		width = Math.Min(titleSafeArea.Width, width);
		if (!isQuestion && currentSpeaker == null && currentObjectDialogue.Count > 0 && !drawOnlyBox)
		{
			width = (int)dialogueFont.MeasureString(currentObjectDialogue.Peek()).X + 128;
			height = (int)dialogueFont.MeasureString(currentObjectDialogue.Peek()).Y + 64;
			x = width2 / 2 - width / 2;
			num3 = ((height > 256) ? (-(height - 256)) : 0);
		}
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64);
		int num4 = -1;
		if (questionChoices.Count >= 3)
		{
			num4 = questionChoices.Count - 3;
		}
		if (!drawOnlyBox && currentObjectDialogue.Count > 0)
		{
			if (dialogueFont.MeasureString(currentObjectDialogue.Peek()).Y >= (float)(height - 128))
			{
				num4 -= (int)(((float)(height - 128) - dialogueFont.MeasureString(currentObjectDialogue.Peek()).Y) / 64f) - 1;
			}
			else
			{
				height += (int)dialogueFont.MeasureString(currentObjectDialogue.Peek()).Y / 2;
				num3 -= (int)dialogueFont.MeasureString(currentObjectDialogue.Peek()).Y / 2;
				if ((int)dialogueFont.MeasureString(currentObjectDialogue.Peek()).Y / 2 > 64)
				{
					num4 = 0;
				}
			}
		}
		if (currentSpeaker != null && isQuestion && currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue().Substring(0, currentDialogueCharacterIndex)
			.Contains(Environment.NewLine))
		{
			num4++;
		}
		value.Width = 64;
		value.Height = 64;
		value.X = 64;
		value.Y = 128;
		Color color = ((r == -1) ? Color.White : new Color(r, g, b));
		Texture2D texture = ((r == -1) ? menuTexture : uncoloredMenuTexture);
		spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(28 + x + num, 28 + y - 64 * num4 + num2 + num3, width - 64, height - 64 + num4 * 64), value, (r == -1) ? color : new Color((int)Utility.Lerp(r, Math.Min(255, r + 150), 0.65f), (int)Utility.Lerp(g, Math.Min(255, g + 150), 0.65f), (int)Utility.Lerp(b, Math.Min(255, b + 150), 0.65f)));
		value.Y = 0;
		value.X = 0;
		spriteBatch.Draw(texture, new Vector2(x + num, y - 64 * num4 + num2 + num3), value, color);
		value.X = 192;
		spriteBatch.Draw(texture, new Vector2(x + width + num - 64, y - 64 * num4 + num2 + num3), value, color);
		value.Y = 192;
		spriteBatch.Draw(texture, new Vector2(x + width + num - 64, y + height + num2 - 64 + num3), value, color);
		value.X = 0;
		spriteBatch.Draw(texture, new Vector2(x + num, y + height + num2 - 64 + num3), value, color);
		value.X = 128;
		value.Y = 0;
		spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(64 + x + num, y - 64 * num4 + num2 + num3, width - 128, 64), value, color);
		value.Y = 192;
		spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(64 + x + num, y + height + num2 - 64 + num3, width - 128, 64), value, color);
		value.Y = 128;
		value.X = 0;
		spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(x + num, y - 64 * num4 + num2 + 64 + num3, 64, height - 128 + num4 * 64), value, color);
		value.X = 192;
		spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(x + width + num - 64, y - 64 * num4 + num2 + 64 + num3, 64, height - 128 + num4 * 64), value, color);
		NPC nPC;
		string text;
		Microsoft.Xna.Framework.Rectangle value2;
		if ((objectDialogueWithPortrait && objectDialoguePortraitPerson != null) || (speaker && currentSpeaker != null && currentSpeaker.CurrentDialogue.Count > 0 && currentSpeaker.CurrentDialogue.Peek().showPortrait))
		{
			nPC = (objectDialogueWithPortrait ? objectDialoguePortraitPerson : currentSpeaker);
			text = ((!objectDialogueWithPortrait) ? nPC.CurrentDialogue.Peek().CurrentEmotion : ((objectDialoguePortraitPerson.Name == player.spouse) ? "$l" : "$neutral"));
			if (text != null)
			{
				int length = text.Length;
				if (length != 2)
				{
					if (length == 8 && text == "$neutral")
					{
						goto IL_0705;
					}
				}
				else
				{
					switch (text[1])
					{
					case 'h':
						break;
					case 's':
						goto IL_063e;
					case 'u':
						goto IL_0651;
					case 'l':
						goto IL_0664;
					case 'a':
						goto IL_0677;
					case 'k':
						goto IL_068a;
					default:
						goto IL_0714;
					}
					if (text == "$h")
					{
						value2 = new Microsoft.Xna.Framework.Rectangle(64, 0, 64, 64);
						goto IL_0740;
					}
				}
			}
			goto IL_0714;
		}
		goto IL_0a07;
		IL_0714:
		value2 = getSourceRectForStandardTileSheet(nPC.Portrait, Convert.ToInt32(nPC.CurrentDialogue.Peek().CurrentEmotion.Substring(1)));
		goto IL_0740;
		IL_0651:
		if (!(text == "$u"))
		{
			goto IL_0714;
		}
		value2 = new Microsoft.Xna.Framework.Rectangle(64, 64, 64, 64);
		goto IL_0740;
		IL_0a07:
		if (drawOnlyBox)
		{
			return;
		}
		string text2 = "";
		if (currentSpeaker != null && currentSpeaker.CurrentDialogue.Count > 0)
		{
			if (currentSpeaker.CurrentDialogue.Peek() == null || currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue().Length < currentDialogueCharacterIndex - 1)
			{
				dialogueUp = false;
				currentDialogueCharacterIndex = 0;
				playSound("dialogueCharacterClose");
				player.forceCanMove();
				return;
			}
			text2 = currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue().Substring(0, currentDialogueCharacterIndex);
		}
		else if (message != null)
		{
			text2 = message;
		}
		else if (currentObjectDialogue.Count > 0)
		{
			text2 = ((currentObjectDialogue.Peek().Length <= 1) ? "" : currentObjectDialogue.Peek().Substring(0, currentDialogueCharacterIndex));
		}
		Vector2 vector = ((dialogueFont.MeasureString(text2).X > (float)(width2 - 256 - num)) ? new Vector2(128 + num, height2 - 64 * num4 - 256 - 16 + num2 + num3) : ((currentSpeaker != null && currentSpeaker.CurrentDialogue.Count > 0) ? new Vector2((float)(width2 / 2) - dialogueFont.MeasureString(currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue()).X / 2f + (float)num, height2 - 64 * num4 - 256 - 16 + num2 + num3) : ((message != null) ? new Vector2((float)(width2 / 2) - dialogueFont.MeasureString(text2).X / 2f + (float)num, y + 96 + 4) : ((!isQuestion) ? new Vector2((float)(width2 / 2) - dialogueFont.MeasureString((currentObjectDialogue.Count == 0) ? "" : currentObjectDialogue.Peek()).X / 2f + (float)num, y + 4 + num3) : new Vector2((float)(width2 / 2) - dialogueFont.MeasureString((currentObjectDialogue.Count == 0) ? "" : currentObjectDialogue.Peek()).X / 2f + (float)num, height2 - 64 * num4 - 256 - (16 + (questionChoices.Count - 2) * 64) + num2 + num3)))));
		if (!drawOnlyBox)
		{
			spriteBatch.DrawString(dialogueFont, text2, vector + new Vector2(3f, 0f), textShadowColor);
			spriteBatch.DrawString(dialogueFont, text2, vector + new Vector2(3f, 3f), textShadowColor);
			spriteBatch.DrawString(dialogueFont, text2, vector + new Vector2(0f, 3f), textShadowColor);
			spriteBatch.DrawString(dialogueFont, text2, vector, textColor);
		}
		if (dialogueFont.MeasureString(text2).Y <= 64f)
		{
			num2 += 64;
		}
		if (isQuestion && !dialogueTyping)
		{
			for (int i = 0; i < questionChoices.Count; i++)
			{
				if (currentQuestionChoice == i)
				{
					vector.X = 80 + num + x;
					vector.Y = (float)(height2 - (5 + num4 + 1) * 64) + ((text2.Trim().Length > 0) ? dialogueFont.MeasureString(text2).Y : 0f) + 128f + (float)(48 * i) - (float)(16 + (questionChoices.Count - 2) * 64) + (float)num2 + (float)num3;
					spriteBatch.End();
					spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);
					spriteBatch.Draw(objectSpriteSheet, vector + new Vector2((float)Math.Cos((double)currentGameTime.TotalGameTime.Milliseconds * Math.PI / 512.0) * 3f, 0f), GameLocation.getSourceRectForObject(26), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
					spriteBatch.End();
					spriteBatch.Begin();
					vector.X = 160 + num + x;
					vector.Y = (float)(height2 - (5 + num4 + 1) * 64) + ((text2.Trim().Length > 1) ? dialogueFont.MeasureString(text2).Y : 0f) + 128f - (float)((questionChoices.Count - 2) * 64) + (float)(48 * i) + (float)num2 + (float)num3;
					spriteBatch.DrawString(dialogueFont, questionChoices[i].responseText, vector, textColor);
				}
				else
				{
					vector.X = 128 + num + x;
					vector.Y = (float)(height2 - (5 + num4 + 1) * 64) + ((text2.Trim().Length > 1) ? dialogueFont.MeasureString(text2).Y : 0f) + 128f - (float)((questionChoices.Count - 2) * 64) + (float)(48 * i) + (float)num2 + (float)num3;
					spriteBatch.DrawString(dialogueFont, questionChoices[i].responseText, vector, unselectedOptionColor);
				}
			}
		}
		if (!drawOnlyBox && !dialogueTyping && message == null)
		{
			spriteBatch.Draw(mouseCursors, new Vector2(x + num + width - 96, (float)(y + height + num2 + num3 - 96) - dialogueButtonScale), getSourceRectForStandardTileSheet(mouseCursors, (!dialogueButtonShrinking && dialogueButtonScale < 8f) ? 3 : 2), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999999f);
		}
		return;
		IL_063e:
		if (!(text == "$s"))
		{
			goto IL_0714;
		}
		value2 = new Microsoft.Xna.Framework.Rectangle(0, 64, 64, 64);
		goto IL_0740;
		IL_0705:
		value2 = new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64);
		goto IL_0740;
		IL_0664:
		if (!(text == "$l"))
		{
			goto IL_0714;
		}
		value2 = new Microsoft.Xna.Framework.Rectangle(0, 128, 64, 64);
		goto IL_0740;
		IL_0740:
		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);
		if (nPC.Portrait != null)
		{
			spriteBatch.Draw(mouseCursors, new Vector2(num + x + 768, height2 - 320 - 64 * num4 - 256 + num2 + 16 - 60 + num3), new Microsoft.Xna.Framework.Rectangle(333, 305, 80, 87), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.98f);
			spriteBatch.Draw(nPC.Portrait, new Vector2(num + x + 768 + 32, height2 - 320 - 64 * num4 - 256 + num2 + 16 - 60 + num3), value2, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.99f);
		}
		spriteBatch.End();
		spriteBatch.Begin();
		if (isQuestion)
		{
			spriteBatch.DrawString(dialogueFont, nPC.displayName, new Vector2(928f - dialogueFont.MeasureString(nPC.displayName).X / 2f + (float)num + (float)x, (float)(height2 - 320 - 64 * num4) - dialogueFont.MeasureString(nPC.displayName).Y + (float)num2 + 21f + (float)num3) + new Vector2(2f, 2f), new Color(150, 150, 150));
		}
		spriteBatch.DrawString(dialogueFont, nPC.Name.Equals("Lewis") ? content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3756") : nPC.displayName, new Vector2((float)(num + x + 896 + 32) - dialogueFont.MeasureString(nPC.Name.Equals("Lewis") ? content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3756") : nPC.displayName).X / 2f, (float)(height2 - 320 - 64 * num4) - dialogueFont.MeasureString(nPC.Name.Equals("Lewis") ? content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3756") : nPC.displayName).Y + (float)num2 + 21f + 8f + (float)num3), textColor);
		goto IL_0a07;
		IL_068a:
		if (text == "$k")
		{
			goto IL_0705;
		}
		goto IL_0714;
		IL_0677:
		if (!(text == "$a"))
		{
			goto IL_0714;
		}
		value2 = new Microsoft.Xna.Framework.Rectangle(64, 128, 64, 64);
		goto IL_0740;
	}

	public static void drawPlayerHeldObject(Farmer f)
	{
		if ((!eventUp || (currentLocation.currentEvent != null && currentLocation.currentEvent.showActiveObject)) && !f.FarmerSprite.PauseForSingleAnimation && !f.isRidingHorse() && !f.bathingClothes.Value && !f.onBridge.Value)
		{
			float num = f.getLocalPosition(viewport).X + (float)((f.rotation < 0f) ? (-8) : ((f.rotation > 0f) ? 8 : 0)) + (float)(f.FarmerSprite.CurrentAnimationFrame.xOffset * 4);
			float num2 = f.getLocalPosition(viewport).Y - 128f + (float)(f.FarmerSprite.CurrentAnimationFrame.positionOffset * 4) + (float)(FarmerRenderer.featureYOffsetPerFrame[f.FarmerSprite.CurrentFrame] * 4);
			if (f.ActiveObject.bigCraftable.Value)
			{
				num2 -= 64f;
			}
			if (f.isEating)
			{
				num = f.getLocalPosition(viewport).X - 21f;
				num2 = f.getLocalPosition(viewport).Y - 128f + 12f;
			}
			if (!f.isEating || (f.isEating && f.Sprite.currentFrame <= 218))
			{
				f.ActiveObject.drawWhenHeld(spriteBatch, new Vector2((int)num, (int)num2), f);
			}
		}
	}

	public static void drawTool(Farmer f)
	{
		drawTool(f, f.CurrentTool.CurrentParentTileIndex);
	}

	public static void drawTool(Farmer f, int currentToolIndex)
	{
		Vector2 playerPosition = f.getLocalPosition(viewport) + f.jitter + f.armOffset;
		FarmerSprite farmerSprite = (FarmerSprite)f.Sprite;
		if (f.CurrentTool is MeleeWeapon meleeWeapon)
		{
			meleeWeapon.drawDuringUse(farmerSprite.currentAnimationIndex, f.FacingDirection, spriteBatch, playerPosition, f);
			return;
		}
		if (f.FarmerSprite.isUsingWeapon())
		{
			MeleeWeapon.drawDuringUse(farmerSprite.currentAnimationIndex, f.FacingDirection, spriteBatch, playerPosition, f, f.FarmerSprite.CurrentToolIndex.ToString(), f.FarmerSprite.getWeaponTypeFromAnimation(), isOnSpecial: false);
			return;
		}
		Tool currentTool = f.CurrentTool;
		if (!(currentTool is Slingshot) && !(currentTool is Shears) && !(currentTool is MilkPail) && !(currentTool is Pan))
		{
			if (!(currentTool is FishingRod) && !(currentTool is WateringCan) && f != player)
			{
				if (farmerSprite.currentSingleAnimation < 160 || farmerSprite.currentSingleAnimation >= 192)
				{
					return;
				}
				if (f.CurrentTool != null)
				{
					f.CurrentTool.Update(f.FacingDirection, 0, f);
					currentToolIndex = f.CurrentTool.CurrentParentTileIndex;
				}
			}
			Texture2D texture2D = ItemRegistry.GetData(f.CurrentTool?.QualifiedItemId)?.GetTexture() ?? toolSpriteSheet;
			Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(currentToolIndex * 16 % texture2D.Width, currentToolIndex * 16 / texture2D.Width * 16, 16, 32);
			float drawLayer = f.getDrawLayer();
			if (f.CurrentTool is FishingRod fishingRod)
			{
				if (fishingRod.fishCaught || fishingRod.showingTreasure)
				{
					f.CurrentTool.draw(spriteBatch);
					return;
				}
				value = new Microsoft.Xna.Framework.Rectangle(farmerSprite.currentAnimationIndex * 48, 288, 48, 48);
				if (f.FacingDirection == 2 || f.FacingDirection == 0)
				{
					value.Y += 48;
				}
				else if (fishingRod.isFishing && (!fishingRod.isReeling || fishingRod.hit))
				{
					playerPosition.Y += 8f;
				}
				if (fishingRod.isFishing)
				{
					value.X += (5 - farmerSprite.currentAnimationIndex) * 48;
				}
				if (fishingRod.isReeling)
				{
					if (f.FacingDirection == 2 || f.FacingDirection == 0)
					{
						value.X = 288;
						if (f.IsLocalPlayer && didPlayerJustClickAtAll())
						{
							value.X = 0;
						}
					}
					else
					{
						value.X = 288;
						value.Y = 240;
						if (f.IsLocalPlayer && didPlayerJustClickAtAll())
						{
							value.Y += 48;
						}
					}
				}
				if (f.FarmerSprite.CurrentFrame == 57)
				{
					value.Height = 0;
				}
				if (f.FacingDirection == 0)
				{
					playerPosition.X += 16f;
				}
			}
			f.CurrentTool?.draw(spriteBatch);
			int num = 0;
			int num2 = 0;
			if (f.CurrentTool is WateringCan)
			{
				num += 80;
				num2 = ((f.FacingDirection == 1) ? 32 : ((f.FacingDirection == 3) ? (-32) : 0));
				if (farmerSprite.currentAnimationIndex == 0 || farmerSprite.currentAnimationIndex == 1)
				{
					num2 = num2 * 3 / 2;
				}
			}
			num += f.yJumpOffset;
			float num3 = FarmerRenderer.GetLayerDepth(drawLayer, f.FacingDirection switch
			{
				0 => FarmerRenderer.FarmerSpriteLayers.ToolUp, 
				2 => FarmerRenderer.FarmerSpriteLayers.ToolDown, 
				_ => FarmerRenderer.FarmerSpriteLayers.TOOL_IN_USE_SIDE, 
			});
			switch (f.FacingDirection)
			{
			case 1:
			{
				if (farmerSprite.currentAnimationIndex > 2)
				{
					Point tilePoint3 = f.TilePoint;
					tilePoint3.X++;
					tilePoint3.Y--;
					if (!(f.CurrentTool is WateringCan) && f.currentLocation.hasTileAt(tilePoint3, "Front"))
					{
						return;
					}
					tilePoint3.Y++;
				}
				currentTool = f.CurrentTool;
				if (!(currentTool is FishingRod fishingRod3))
				{
					if (currentTool is WateringCan)
					{
						if (farmerSprite.currentAnimationIndex == 1)
						{
							Point tilePoint4 = f.TilePoint;
							tilePoint4.X--;
							tilePoint4.Y--;
							if (f.currentLocation.hasTileAt(tilePoint4, "Front") && f.Position.Y % 64f < 32f)
							{
								return;
							}
						}
						switch (farmerSprite.currentAnimationIndex)
						{
						case 0:
						case 1:
							spriteBatch.Draw(texture2D, new Vector2((int)(playerPosition.X + (float)num2 - 4f), (int)(playerPosition.Y - 128f + 8f + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
							break;
						case 2:
							spriteBatch.Draw(texture2D, new Vector2((int)playerPosition.X + num2 + 24, (int)(playerPosition.Y - 128f - 8f + (float)num)), value, Color.White, (float)Math.PI / 12f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
							break;
						case 3:
							value.X += 16;
							spriteBatch.Draw(texture2D, new Vector2((int)(playerPosition.X + (float)num2 + 8f), (int)(playerPosition.Y - 128f - 24f + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
							break;
						}
						return;
					}
					switch (farmerSprite.currentAnimationIndex)
					{
					case 0:
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 32f - 4f + (float)num2 - (float)Math.Min(8, f.toolPower.Value * 4), playerPosition.Y - 128f + 24f + (float)num + (float)Math.Min(8, f.toolPower.Value * 4))), value, Color.White, -(float)Math.PI / 12f - (float)Math.Min(f.toolPower.Value, 2) * ((float)Math.PI / 64f), new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
						break;
					case 1:
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + 32f - 24f + (float)num2, playerPosition.Y - 124f + (float)num + 64f)), value, Color.White, (float)Math.PI / 12f, new Vector2(0f, 32f), 4f, SpriteEffects.None, num3);
						break;
					case 2:
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + 32f + (float)num2 - 4f, playerPosition.Y - 132f + (float)num + 64f)), value, Color.White, (float)Math.PI / 4f, new Vector2(0f, 32f), 4f, SpriteEffects.None, num3);
						break;
					case 3:
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + 32f + 28f + (float)num2, playerPosition.Y - 64f + (float)num)), value, Color.White, (float)Math.PI * 7f / 12f, new Vector2(0f, 32f), 4f, SpriteEffects.None, num3);
						break;
					case 4:
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + 32f + 28f + (float)num2, playerPosition.Y - 64f + 4f + (float)num)), value, Color.White, (float)Math.PI * 7f / 12f, new Vector2(0f, 32f), 4f, SpriteEffects.None, num3);
						break;
					case 5:
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + 64f + 12f + (float)num2, playerPosition.Y - 128f + 32f + (float)num + 128f)), value, Color.White, (float)Math.PI / 4f, new Vector2(0f, 32f), 4f, SpriteEffects.None, num3);
						break;
					case 6:
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + 42f + 8f + (float)num2, playerPosition.Y - 64f + 24f + (float)num + 128f)), value, Color.White, 0f, new Vector2(0f, 128f), 4f, SpriteEffects.None, num3);
						break;
					}
					return;
				}
				Color color2 = fishingRod3.getColor();
				switch (farmerSprite.currentAnimationIndex)
				{
				case 0:
					if (fishingRod3.isReeling || fishingRod3.isFishing || fishingRod3.doneWithAnimation || !fishingRod3.hasDoneFucntionYet || fishingRod3.pullingOutOfWater)
					{
						spriteBatch.Draw(texture2D, new Vector2(playerPosition.X - 64f + (float)num2, playerPosition.Y - 160f + (float)num), value, color2, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
					}
					break;
				case 1:
					spriteBatch.Draw(texture2D, new Vector2(playerPosition.X - 64f + (float)num2, playerPosition.Y - 160f + 8f + (float)num), value, color2, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
					break;
				case 2:
					spriteBatch.Draw(texture2D, new Vector2(playerPosition.X - 96f + 32f + (float)num2, playerPosition.Y - 128f - 24f + (float)num), value, color2, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
					break;
				case 3:
					spriteBatch.Draw(texture2D, new Vector2(playerPosition.X - 96f + 24f + (float)num2, playerPosition.Y - 128f - 32f + (float)num), value, color2, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
					break;
				case 4:
					if (fishingRod3.isFishing || fishingRod3.doneWithAnimation)
					{
						spriteBatch.Draw(texture2D, new Vector2(playerPosition.X - 64f + (float)num2, playerPosition.Y - 160f + (float)num), value, color2, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
					}
					else
					{
						spriteBatch.Draw(texture2D, new Vector2(playerPosition.X - 64f + (float)num2, playerPosition.Y - 160f + 4f + (float)num), value, color2, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
					}
					break;
				case 5:
					spriteBatch.Draw(texture2D, new Vector2(playerPosition.X - 64f + (float)num2, playerPosition.Y - 160f + (float)num), value, color2, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
					break;
				}
				return;
			}
			case 3:
			{
				if (farmerSprite.currentAnimationIndex > 2)
				{
					Point tilePoint = f.TilePoint;
					tilePoint.X--;
					tilePoint.Y--;
					if (!(f.CurrentTool is WateringCan) && f.currentLocation.hasTileAt(tilePoint, "Front") && f.Position.Y % 64f < 32f)
					{
						return;
					}
					tilePoint.Y++;
				}
				currentTool = f.CurrentTool;
				if (!(currentTool is FishingRod fishingRod2))
				{
					if (currentTool is WateringCan)
					{
						if (farmerSprite.currentAnimationIndex == 1)
						{
							Point tilePoint2 = f.TilePoint;
							tilePoint2.X--;
							tilePoint2.Y--;
							if (f.currentLocation.hasTileAt(tilePoint2, "Front") && f.Position.Y % 64f < 32f)
							{
								return;
							}
						}
						switch (farmerSprite.currentAnimationIndex)
						{
						case 0:
						case 1:
							spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2 - 4f, playerPosition.Y - 128f + 8f + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.FlipHorizontally, num3);
							break;
						case 2:
							spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2 - 16f, playerPosition.Y - 128f + (float)num)), value, Color.White, -(float)Math.PI / 12f, new Vector2(0f, 16f), 4f, SpriteEffects.FlipHorizontally, num3);
							break;
						case 3:
							value.X += 16;
							spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2 - 16f, playerPosition.Y - 128f - 24f + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.FlipHorizontally, num3);
							break;
						}
					}
					else
					{
						switch (farmerSprite.currentAnimationIndex)
						{
						case 0:
							spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + 32f + 8f + (float)num2 + (float)Math.Min(8, f.toolPower.Value * 4), playerPosition.Y - 128f + 8f + (float)num + (float)Math.Min(8, f.toolPower.Value * 4))), value, Color.White, (float)Math.PI / 12f + (float)Math.Min(f.toolPower.Value, 2) * ((float)Math.PI / 64f), new Vector2(0f, 16f), 4f, SpriteEffects.FlipHorizontally, num3);
							break;
						case 1:
							spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 16f + (float)num2, playerPosition.Y - 128f + 16f + (float)num)), value, Color.White, -(float)Math.PI / 12f, new Vector2(0f, 16f), 4f, SpriteEffects.FlipHorizontally, num3);
							break;
						case 2:
							spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f + 4f + (float)num2, playerPosition.Y - 128f + 60f + (float)num)), value, Color.White, -(float)Math.PI / 4f, new Vector2(0f, 16f), 4f, SpriteEffects.FlipHorizontally, num3);
							break;
						case 3:
							spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f + 20f + (float)num2, playerPosition.Y - 64f + 76f + (float)num)), value, Color.White, (float)Math.PI * -7f / 12f, new Vector2(0f, 16f), 4f, SpriteEffects.FlipHorizontally, num3);
							break;
						case 4:
							spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f + 24f + (float)num2, playerPosition.Y + 24f + (float)num)), value, Color.White, (float)Math.PI * -7f / 12f, new Vector2(0f, 16f), 4f, SpriteEffects.FlipHorizontally, num3);
							break;
						}
					}
					return;
				}
				Color color = fishingRod2.getColor();
				switch (farmerSprite.currentAnimationIndex)
				{
				case 0:
					if (fishingRod2.isReeling || fishingRod2.isFishing || fishingRod2.doneWithAnimation || !fishingRod2.hasDoneFucntionYet || fishingRod2.pullingOutOfWater)
					{
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f + (float)num2, playerPosition.Y - 160f + (float)num)), value, color, 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, num3);
					}
					break;
				case 1:
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f + (float)num2, playerPosition.Y - 160f + 8f + (float)num)), value, color, 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, num3);
					break;
				case 2:
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 96f + 32f + (float)num2, playerPosition.Y - 128f - 24f + (float)num)), value, color, 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, num3);
					break;
				case 3:
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 96f + 24f + (float)num2, playerPosition.Y - 128f - 32f + (float)num)), value, color, 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, num3);
					break;
				case 4:
					if (fishingRod2.isFishing || fishingRod2.doneWithAnimation)
					{
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f + (float)num2, playerPosition.Y - 160f + (float)num)), value, color, 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, num3);
					}
					else
					{
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f + (float)num2, playerPosition.Y - 160f + 4f + (float)num)), value, color, 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, num3);
					}
					break;
				case 5:
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f + (float)num2, playerPosition.Y - 160f + (float)num)), value, color, 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, num3);
					break;
				}
				return;
			}
			}
			if (farmerSprite.currentAnimationIndex > 2 && !(f.CurrentTool is FishingRod { isCasting: false, castedButBobberStillInAir: false, isTimingCast: false }))
			{
				Point tilePoint5 = f.TilePoint;
				if (f.currentLocation.hasTileAt(tilePoint5, "Front") && f.Position.Y % 64f < 32f && f.Position.Y % 64f > 16f)
				{
					return;
				}
			}
			currentTool = f.CurrentTool;
			if (!(currentTool is FishingRod fishingRod5))
			{
				if (currentTool is WateringCan)
				{
					switch (farmerSprite.currentAnimationIndex)
					{
					case 0:
					case 1:
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2, playerPosition.Y - 128f + 16f + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
						break;
					case 2:
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2, playerPosition.Y - 128f - (float)((f.FacingDirection == 2) ? (-4) : 32) + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
						break;
					case 3:
						if (f.FacingDirection == 2)
						{
							value.X += 16;
						}
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2 - (float)((f.FacingDirection == 2) ? 4 : 0), playerPosition.Y - 128f - (float)((f.FacingDirection == 2) ? (-24) : 64) + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
						break;
					}
					return;
				}
				switch (farmerSprite.currentAnimationIndex)
				{
				case 0:
					if (f.FacingDirection == 0)
					{
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2, playerPosition.Y - 128f - 8f + (float)num + (float)Math.Min(8, f.toolPower.Value * 4))), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
					}
					else
					{
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2 - 20f, playerPosition.Y - 128f + 12f + (float)num + (float)Math.Min(8, f.toolPower.Value * 4))), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
					}
					break;
				case 1:
					if (f.FacingDirection == 0)
					{
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2 + 4f, playerPosition.Y - 128f + 40f + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
					}
					else
					{
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2 - 12f, playerPosition.Y - 128f + 32f + (float)num)), value, Color.White, -(float)Math.PI / 24f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
					}
					break;
				case 2:
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2, playerPosition.Y - 128f + 64f + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
					break;
				case 3:
					if (f.FacingDirection != 0)
					{
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2, playerPosition.Y - 64f + 44f + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
					}
					break;
				case 4:
					if (f.FacingDirection != 0)
					{
						spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2, playerPosition.Y - 64f + 48f + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
					}
					break;
				case 5:
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X + (float)num2, playerPosition.Y - 64f + 32f + (float)num)), value, Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, num3);
					break;
				}
				return;
			}
			if (farmerSprite.currentAnimationIndex <= 2)
			{
				Point tilePoint6 = f.TilePoint;
				tilePoint6.Y--;
				if (f.currentLocation.hasTileAt(tilePoint6, "Front"))
				{
					return;
				}
			}
			if (f.FacingDirection == 2)
			{
				num3 += 0.01f;
			}
			Color color3 = fishingRod5.getColor();
			switch (farmerSprite.currentAnimationIndex)
			{
			case 0:
				if (!fishingRod5.showingTreasure && !fishingRod5.fishCaught && (f.FacingDirection != 0 || !fishingRod5.isFishing || fishingRod5.isReeling))
				{
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f, playerPosition.Y - 128f + 4f)), value, color3, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
				}
				break;
			case 1:
				spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f, playerPosition.Y - 128f + 4f)), value, color3, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
				break;
			case 2:
				spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f, playerPosition.Y - 128f + 4f)), value, color3, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
				break;
			case 3:
				if (f.FacingDirection == 2)
				{
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f, playerPosition.Y - 128f + 4f)), value, color3, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
				}
				break;
			case 4:
				if (f.FacingDirection == 0 && fishingRod5.isFishing)
				{
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 80f, playerPosition.Y - 96f)), value, color3, 0f, Vector2.Zero, 4f, SpriteEffects.FlipVertically, num3);
				}
				else if (f.FacingDirection == 2)
				{
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f, playerPosition.Y - 128f + 4f)), value, color3, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
				}
				break;
			case 5:
				if (f.FacingDirection == 2 && !fishingRod5.showingTreasure && !fishingRod5.fishCaught)
				{
					spriteBatch.Draw(texture2D, Utility.snapToInt(new Vector2(playerPosition.X - 64f, playerPosition.Y - 128f + 4f)), value, color3, 0f, Vector2.Zero, 4f, SpriteEffects.None, num3);
				}
				break;
			}
		}
		else
		{
			f.CurrentTool.draw(spriteBatch);
		}
	}

	/// ####################
	/// OTHER HELPER METHODS
	/// ####################
	public static Vector2 GlobalToLocal(xTile.Dimensions.Rectangle viewport, Vector2 globalPosition)
	{
		return new Vector2(globalPosition.X - (float)viewport.X, globalPosition.Y - (float)viewport.Y);
	}

	public static bool IsEnglish()
	{
		return content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.en;
	}

	public static Vector2 GlobalToLocal(Vector2 globalPosition)
	{
		return new Vector2(globalPosition.X - (float)viewport.X, globalPosition.Y - (float)viewport.Y);
	}

	public static Microsoft.Xna.Framework.Rectangle GlobalToLocal(xTile.Dimensions.Rectangle viewport, Microsoft.Xna.Framework.Rectangle globalPosition)
	{
		return new Microsoft.Xna.Framework.Rectangle(globalPosition.X - viewport.X, globalPosition.Y - viewport.Y, globalPosition.Width, globalPosition.Height);
	}

	public static string parseText(string text, SpriteFont whichFont, int width)
	{
		if (text == null)
		{
			return "";
		}
		text = Dialogue.applyGenderSwitchBlocks(player.Gender, text);
		_ParseTextStringBuilder.Clear();
		_ParseTextStringBuilderLine.Clear();
		_ParseTextStringBuilderWord.Clear();
		float num = 0f;
		LocalizedContentManager.LanguageCode currentLanguageCode = LocalizedContentManager.CurrentLanguageCode;
		if (currentLanguageCode == LocalizedContentManager.LanguageCode.ja || currentLanguageCode == LocalizedContentManager.LanguageCode.zh || currentLanguageCode == LocalizedContentManager.LanguageCode.th)
		{
			foreach (object item in asianSpacingRegex.Matches(text))
			{
				string text2 = item.ToString();
				float num2 = whichFont.MeasureString(text2).X + whichFont.Spacing;
				if (num + num2 > (float)width || text2.Equals(Environment.NewLine) || text2.Equals("\n"))
				{
					_ParseTextStringBuilder.Append(_ParseTextStringBuilderLine);
					_ParseTextStringBuilder.Append(Environment.NewLine);
					_ParseTextStringBuilderLine.Clear();
					num = 0f;
				}
				if (!text2.Equals(Environment.NewLine) && !text2.Equals("\n"))
				{
					_ParseTextStringBuilderLine.Append(text2);
					num += num2;
				}
			}
			_ParseTextStringBuilder.Append(_ParseTextStringBuilderLine);
			return _ParseTextStringBuilder.ToString();
		}
		num = 0f;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			bool flag;
			if (c != '\n')
			{
				if (c == '\r')
				{
					continue;
				}
				if (c == ' ')
				{
					flag = true;
				}
				else
				{
					_ParseTextStringBuilderWord.Append(c);
					flag = i == text.Length - 1;
				}
			}
			else
			{
				flag = true;
			}
			if (!flag)
			{
				continue;
			}
			try
			{
				float num3 = whichFont.MeasureString(_ParseTextStringBuilderWord).X + whichFont.Spacing;
				if (num + num3 > (float)width)
				{
					_ParseTextStringBuilder.Append(_ParseTextStringBuilderLine);
					_ParseTextStringBuilder.Append(Environment.NewLine);
					_ParseTextStringBuilderLine.Clear();
					num = 0f;
				}
				if (c == '\n')
				{
					_ParseTextStringBuilderLine.Append(_ParseTextStringBuilderWord);
					_ParseTextStringBuilder.Append(_ParseTextStringBuilderLine);
					_ParseTextStringBuilder.Append(Environment.NewLine);
					_ParseTextStringBuilderLine.Clear();
					_ParseTextStringBuilderWord.Clear();
					num = 0f;
					continue;
				}
				_ParseTextStringBuilderLine.Append(_ParseTextStringBuilderWord);
				_ParseTextStringBuilderLine.Append(" ");
				float num4 = whichFont.MeasureString(" ").X + whichFont.Spacing;
				num += num3 + num4;
			}
			catch (Exception exception)
			{
				log.Error("Exception measuring string: ", exception);
			}
			_ParseTextStringBuilderWord.Clear();
		}
		_ParseTextStringBuilderLine.Append(_ParseTextStringBuilderWord);
		_ParseTextStringBuilder.Append(_ParseTextStringBuilderLine);
		return _ParseTextStringBuilder.ToString();
	}

	public static void UpdateHorseOwnership()
	{
		bool flag = false;
		Dictionary<long, Horse> dictionary = new Dictionary<long, Horse>();
		HashSet<Horse> hashSet = new HashSet<Horse>();
		List<Stable> stables = new List<Stable>();
		Utility.ForEachBuilding(delegate(Stable stable)
		{
			stables.Add(stable);
			return true;
		});
		foreach (Stable item in stables)
		{
			if (item.owner.Value == -6666666 && GetPlayer(-6666666L) == null)
			{
				item.owner.Value = player.UniqueMultiplayerID;
			}
			item.grabHorse();
		}
		foreach (Stable item2 in stables)
		{
			Horse stableHorse = item2.getStableHorse();
			if (stableHorse != null && !hashSet.Contains(stableHorse) && stableHorse.getOwner() != null && !dictionary.ContainsKey(stableHorse.getOwner().UniqueMultiplayerID) && stableHorse.getOwner().horseName.Value != null && stableHorse.getOwner().horseName.Value.Length > 0 && stableHorse.Name == stableHorse.getOwner().horseName.Value)
			{
				dictionary[stableHorse.getOwner().UniqueMultiplayerID] = stableHorse;
				hashSet.Add(stableHorse);
				if (flag)
				{
					log.Verbose("Assigned horse " + stableHorse.Name + " to " + stableHorse.getOwner().Name + " (Exact match)");
				}
			}
		}
		Dictionary<string, Farmer> dictionary2 = new Dictionary<string, Farmer>();
		foreach (Farmer allFarmer in getAllFarmers())
		{
			if (string.IsNullOrEmpty(allFarmer?.horseName.Value))
			{
				continue;
			}
			bool flag2 = false;
			foreach (Horse item3 in hashSet)
			{
				if (item3.getOwner() == allFarmer)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				dictionary2[allFarmer.horseName.Value] = allFarmer;
			}
		}
		foreach (Stable item4 in stables)
		{
			Horse stableHorse2 = item4.getStableHorse();
			if (stableHorse2 != null && !hashSet.Contains(stableHorse2) && stableHorse2.getOwner() != null && stableHorse2.Name != null && stableHorse2.Name.Length > 0 && dictionary2.TryGetValue(stableHorse2.Name, out var value) && !dictionary.ContainsKey(value.UniqueMultiplayerID))
			{
				item4.owner.Value = value.UniqueMultiplayerID;
				item4.updateHorseOwnership();
				dictionary[stableHorse2.getOwner().UniqueMultiplayerID] = stableHorse2;
				hashSet.Add(stableHorse2);
				if (flag)
				{
					log.Verbose("Assigned horse " + stableHorse2.Name + " to " + stableHorse2.getOwner().Name + " (Name match from different owner.)");
				}
			}
		}
		foreach (Stable item5 in stables)
		{
			Horse stableHorse3 = item5.getStableHorse();
			if (stableHorse3 != null && !hashSet.Contains(stableHorse3) && stableHorse3.getOwner() != null && !dictionary.ContainsKey(stableHorse3.getOwner().UniqueMultiplayerID))
			{
				dictionary[stableHorse3.getOwner().UniqueMultiplayerID] = stableHorse3;
				hashSet.Add(stableHorse3);
				item5.updateHorseOwnership();
				if (flag)
				{
					log.Verbose("Assigned horse " + stableHorse3.Name + " to " + stableHorse3.getOwner().Name + " (Owner's only stable)");
				}
			}
		}
		foreach (Stable item6 in stables)
		{
			Horse stableHorse4 = item6.getStableHorse();
			if (stableHorse4 == null || hashSet.Contains(stableHorse4))
			{
				continue;
			}
			foreach (Horse item7 in hashSet)
			{
				if (stableHorse4.ownerId == item7.ownerId)
				{
					item6.owner.Value = 0L;
					item6.updateHorseOwnership();
					if (flag)
					{
						log.Verbose("Unassigned horse (stable owner already has a horse).");
					}
					break;
				}
			}
		}
	}

	public static string LoadStringByGender(Gender npcGender, string key)
	{
		if (npcGender == Gender.Male)
		{
			return content.LoadString(key).Split('/')[0];
		}
		return content.LoadString(key).Split('/').Last();
	}

	public static string LoadStringByGender(Gender npcGender, string key, params object[] substitutions)
	{
		string text;
		if (npcGender == Gender.Male)
		{
			text = content.LoadString(key).Split('/')[0];
			if (substitutions.Length != 0)
			{
				try
				{
					return string.Format(text, substitutions);
				}
				catch
				{
					return text;
				}
			}
		}
		text = content.LoadString(key).Split('/').Last();
		if (substitutions.Length != 0)
		{
			try
			{
				return string.Format(text, substitutions);
			}
			catch
			{
				return text;
			}
		}
		return text;
	}

	public static string parseText(string text)
	{
		return parseText(text, dialogueFont, dialogueWidth);
	}

	public static Microsoft.Xna.Framework.Rectangle getSourceRectForStandardTileSheet(Texture2D tileSheet, int tilePosition, int width = -1, int height = -1)
	{
		if (width == -1)
		{
			width = 64;
		}
		if (height == -1)
		{
			height = 64;
		}
		return new Microsoft.Xna.Framework.Rectangle(tilePosition * width % tileSheet.Width, tilePosition * width / tileSheet.Width * height, width, height);
	}

	public static Microsoft.Xna.Framework.Rectangle getSquareSourceRectForNonStandardTileSheet(Texture2D tileSheet, int tileWidth, int tileHeight, int tilePosition)
	{
		return new Microsoft.Xna.Framework.Rectangle(tilePosition * tileWidth % tileSheet.Width, tilePosition * tileWidth / tileSheet.Width * tileHeight, tileWidth, tileHeight);
	}

	public static Microsoft.Xna.Framework.Rectangle getArbitrarySourceRect(Texture2D tileSheet, int tileWidth, int tileHeight, int tilePosition)
	{
		if (tileSheet != null)
		{
			return new Microsoft.Xna.Framework.Rectangle(tilePosition * tileWidth % tileSheet.Width, tilePosition * tileWidth / tileSheet.Width * tileHeight, tileWidth, tileHeight);
		}
		return Microsoft.Xna.Framework.Rectangle.Empty;
	}

	public static string getTimeOfDayString(int time)
	{
		string text = ((time % 100 == 0) ? "0" : string.Empty);
		string text2;
		switch (LocalizedContentManager.CurrentLanguageCode)
		{
		default:
			text2 = ((time / 100 % 12 == 0) ? "12" : (time / 100 % 12).ToString());
			break;
		case LocalizedContentManager.LanguageCode.ja:
			text2 = ((time / 100 % 12 == 0) ? "0" : (time / 100 % 12).ToString());
			break;
		case LocalizedContentManager.LanguageCode.zh:
			text2 = (time / 100 % 24).ToString();
			break;
		case LocalizedContentManager.LanguageCode.ru:
		case LocalizedContentManager.LanguageCode.pt:
		case LocalizedContentManager.LanguageCode.es:
		case LocalizedContentManager.LanguageCode.de:
		case LocalizedContentManager.LanguageCode.th:
		case LocalizedContentManager.LanguageCode.fr:
		case LocalizedContentManager.LanguageCode.tr:
		case LocalizedContentManager.LanguageCode.hu:
			text2 = (time / 100 % 24).ToString();
			text2 = ((time / 100 % 24 <= 9) ? ("0" + text2) : text2);
			break;
		}
		string text3 = text2 + ":" + time % 100 + text;
		switch (LocalizedContentManager.CurrentLanguageCode)
		{
		case LocalizedContentManager.LanguageCode.en:
			return text3 + " " + ((time < 1200 || time >= 2400) ? content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10370") : content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10371"));
		case LocalizedContentManager.LanguageCode.ja:
			if (time >= 1200 && time < 2400)
			{
				return content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10371") + " " + text3;
			}
			return content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10370") + " " + text3;
		case LocalizedContentManager.LanguageCode.fr:
			if (time % 100 != 0)
			{
				return text2 + "h" + time % 100;
			}
			return text2 + "h";
		case LocalizedContentManager.LanguageCode.mod:
			return LocalizedContentManager.FormatTimeString(time, LocalizedContentManager.CurrentModLanguage.TimeFormat).ToString();
		default:
			return text3;
		}
	}

	public static bool[,] getCircleOutlineGrid(int radius)
	{
		bool[,] array = new bool[radius * 2 + 1, radius * 2 + 1];
		int num = 1 - radius;
		int num2 = 1;
		int num3 = -2 * radius;
		int num4 = 0;
		int num5 = radius;
		array[radius, radius + radius] = true;
		array[radius, radius - radius] = true;
		array[radius + radius, radius] = true;
		array[radius - radius, radius] = true;
		while (num4 < num5)
		{
			if (num >= 0)
			{
				num5--;
				num3 += 2;
				num += num3;
			}
			num4++;
			num2 += 2;
			num += num2;
			array[radius + num4, radius + num5] = true;
			array[radius - num4, radius + num5] = true;
			array[radius + num4, radius - num5] = true;
			array[radius - num4, radius - num5] = true;
			array[radius + num5, radius + num4] = true;
			array[radius - num5, radius + num4] = true;
			array[radius + num5, radius - num4] = true;
			array[radius - num5, radius - num4] = true;
		}
		return array;
	}

	/// <summary>Get the internal identifier for the current farm type. This is either the numeric index for a vanilla farm, or the <see cref="F:StardewValley.GameData.ModFarmType.Id" /> field for a custom type.</summary>
	public static string GetFarmTypeID()
	{
		if (whichFarm != 7 || whichModFarm == null)
		{
			return whichFarm.ToString();
		}
		return whichModFarm.Id;
	}

	/// <summary>Get the human-readable identifier for the current farm type. For a custom farm type, this is equivalent to <see cref="M:StardewValley.Game1.GetFarmTypeID" />.</summary>
	public static string GetFarmTypeKey()
	{
		return whichFarm switch
		{
			0 => "Standard", 
			1 => "Riverland", 
			2 => "Forest", 
			3 => "Hilltop", 
			4 => "Wilderness", 
			5 => "FourCorners", 
			6 => "Beach", 
			_ => GetFarmTypeID(), 
		};
	}

	public void _PerformRemoveNormalItemFromWorldOvernight(string itemId)
	{
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			_RecursiveRemoveThisNormalItemLocation(location, itemId);
			return true;
		}, includeInteriors: true, includeGenerated: true);
		player.team.returnedDonations.RemoveWhere((Item item) => _RecursiveRemoveThisNormalItemItem(item, itemId));
		foreach (Inventory value in player.team.globalInventories.Values)
		{
			value.RemoveWhere((Item item) => _RecursiveRemoveThisNormalItemItem(item, itemId));
		}
		foreach (SpecialOrder specialOrder in player.team.specialOrders)
		{
			for (int num = 0; num < specialOrder.donatedItems.Count; num++)
			{
				Item this_item = specialOrder.donatedItems[num];
				if (_RecursiveRemoveThisNormalItemItem(this_item, itemId))
				{
					specialOrder.donatedItems[num] = null;
				}
			}
		}
	}

	protected virtual void _PerformRemoveNormalItemFromFarmerOvernight(Farmer farmer, string itemId)
	{
		for (int i = 0; i < farmer.Items.Count; i++)
		{
			if (_RecursiveRemoveThisNormalItemItem(farmer.Items[i], itemId))
			{
				farmer.Items[i] = null;
			}
		}
		farmer.itemsLostLastDeath.RemoveWhere((Item item) => _RecursiveRemoveThisNormalItemItem(item, itemId));
		if (farmer.recoveredItem != null && _RecursiveRemoveThisNormalItemItem(farmer.recoveredItem, itemId))
		{
			farmer.recoveredItem = null;
			farmer.mailbox.Remove("MarlonRecovery");
			farmer.mailForTomorrow.Remove("MarlonRecovery");
		}
		if (farmer.toolBeingUpgraded.Value != null && _RecursiveRemoveThisNormalItemItem(farmer.toolBeingUpgraded.Value, itemId))
		{
			farmer.toolBeingUpgraded.Value = null;
		}
	}

	protected virtual bool _RecursiveRemoveThisNormalItemItem(Item this_item, string itemId)
	{
		if (this_item != null)
		{
			if (this_item is Object obj)
			{
				if (obj.heldObject.Value != null && _RecursiveRemoveThisNormalItemItem(obj.heldObject.Value, itemId))
				{
					obj.ResetParentSheetIndex();
					obj.heldObject.Value = null;
					obj.readyForHarvest.Value = false;
					obj.showNextIndex.Value = false;
				}
				if (!(obj is StorageFurniture storageFurniture))
				{
					if (!(obj is IndoorPot indoorPot))
					{
						if (obj is Chest chest)
						{
							bool flag = false;
							IInventory items = chest.Items;
							for (int i = 0; i < items.Count; i++)
							{
								Item item = items[i];
								if (item != null && _RecursiveRemoveThisNormalItemItem(item, itemId))
								{
									items[i] = null;
									flag = true;
								}
							}
							if (flag)
							{
								chest.clearNulls();
							}
						}
					}
					else if (indoorPot.hoeDirt.Value != null)
					{
						_RecursiveRemoveThisNormalItemDirt(indoorPot.hoeDirt.Value, null, Vector2.Zero, itemId);
					}
				}
				else
				{
					bool flag2 = false;
					for (int j = 0; j < storageFurniture.heldItems.Count; j++)
					{
						Item item2 = storageFurniture.heldItems[j];
						if (item2 != null && _RecursiveRemoveThisNormalItemItem(item2, itemId))
						{
							storageFurniture.heldItems[j] = null;
							flag2 = true;
						}
					}
					if (flag2)
					{
						storageFurniture.ClearNulls();
					}
				}
				if (obj.heldObject.Value != null && _RecursiveRemoveThisNormalItemItem(obj.heldObject.Value, itemId))
				{
					obj.heldObject.Value = null;
				}
			}
			return Utility.IsNormalObjectAtParentSheetIndex(this_item, itemId);
		}
		return false;
	}

	protected virtual void _RecursiveRemoveThisNormalItemDirt(HoeDirt dirt, GameLocation location, Vector2 coord, string itemId)
	{
		if (dirt.crop != null && dirt.crop.indexOfHarvest.Value == itemId)
		{
			dirt.destroyCrop(showAnimation: false);
		}
	}

	protected virtual void _RecursiveRemoveThisNormalItemLocation(GameLocation l, string itemId)
	{
		if (l == null)
		{
			return;
		}
		List<Guid> list = new List<Guid>();
		foreach (Furniture item3 in l.furniture)
		{
			if (_RecursiveRemoveThisNormalItemItem(item3, itemId))
			{
				list.Add(l.furniture.GuidOf(item3));
			}
		}
		foreach (Guid item4 in list)
		{
			l.furniture.Remove(item4);
		}
		foreach (NPC character in l.characters)
		{
			if (character is Monster monster)
			{
				monster.objectsToDrop?.RemoveWhere((string id) => id == itemId);
			}
		}
		Chest fridge = l.GetFridge(onlyUnlocked: false);
		if (fridge != null)
		{
			IInventory items = fridge.Items;
			for (int num = 0; num < items.Count; num++)
			{
				Item item = items[num];
				if (item != null && _RecursiveRemoveThisNormalItemItem(item, itemId))
				{
					items[num] = null;
				}
			}
		}
		foreach (Vector2 key2 in l.terrainFeatures.Keys)
		{
			if (l.terrainFeatures[key2] is HoeDirt dirt)
			{
				_RecursiveRemoveThisNormalItemDirt(dirt, l, key2, itemId);
			}
		}
		foreach (Building building in l.buildings)
		{
			foreach (Chest buildingChest in building.buildingChests)
			{
				bool flag = false;
				for (int num2 = 0; num2 < buildingChest.Items.Count; num2++)
				{
					Item item2 = buildingChest.Items[num2];
					if (item2 != null && _RecursiveRemoveThisNormalItemItem(item2, itemId))
					{
						buildingChest.Items[num2] = null;
						flag = true;
					}
				}
				if (flag)
				{
					buildingChest.clearNulls();
				}
			}
		}
		Vector2[] array = l.objects.Keys.ToArray();
		foreach (Vector2 key in array)
		{
			Object obj = l.objects[key];
			if (obj != fridge && _RecursiveRemoveThisNormalItemItem(obj, itemId))
			{
				l.objects.Remove(key);
			}
		}
		l.debris.RemoveWhere((Debris debris) => debris.item != null && _RecursiveRemoveThisNormalItemItem(debris.item, itemId));
		if (l is ShopLocation shopLocation)
		{
			shopLocation.itemsFromPlayerToSell.RemoveWhere((Item this_item) => _RecursiveRemoveThisNormalItemItem(this_item, itemId));
			shopLocation.itemsToStartSellingTomorrow.RemoveWhere((Item this_item) => _RecursiveRemoveThisNormalItemItem(this_item, itemId));
		}
	}

	public static bool GetHasRoomAnotherFarm()
	{
		return true;
	}

	/// <summary>Reset most game state when returning to the title screen from a title sub-menu.</summary>
	/// <remarks>
	///   <para>This is specialized code that normally shouldn't be called directly. This differs from <see cref="M:StardewValley.Game1.CleanupReturningToTitle" /> in that it doesn't exit split screens, reset the title menu, or reset the game window.</para>
	///   <para>This is called automatically from <see cref="M:StardewValley.Game1.CleanupReturningToTitle" />.</para>
	/// </remarks>
	public virtual void ResetGameStateOnTitleScreen()
	{
		LocalizedContentManager.localizedAssetNames.Clear();
		Event.invalidFestivals.Clear();
		NPC.invalidDialogueFiles.Clear();
		SaveGame.CancelToTitle = false;
		overlayMenu = null;
		multiplayer.cachedMultiplayerMaps.Clear();
		keyboardFocusInstance = null;
		BuildingPaintMenu.savedColors = null;
		startingGameSeed = null;
		UseLegacyRandom = false;
		_afterNewDayAction = null;
		_currentMinigame = null;
		gameMode = 0;
		_isSaving = false;
		_mouseCursorTransparency = 1f;
		_newDayTask = null;
		newDaySync.destroy();
		netReady.Reset();
		dedicatedServer.Reset();
		resetPlayer();
		afterDialogues = null;
		afterFade = null;
		afterPause = null;
		afterViewport = null;
		ambientLight = new Color(0, 0, 0, 0);
		background = null;
		chatBox = null;
		specialCurrencyDisplay?.Cleanup();
		GameLocation.PlayedNewLocationContextMusic = false;
		IsPlayingBackgroundMusic = false;
		IsPlayingNightAmbience = false;
		IsPlayingOutdoorsAmbience = false;
		IsPlayingMorningSong = false;
		IsPlayingTownMusic = false;
		specialCurrencyDisplay = null;
		conventionMode = false;
		currentCursorTile = Vector2.Zero;
		currentDialogueCharacterIndex = 0;
		currentLightSources.Clear();
		currentLoader = null;
		currentLocation = null;
		_PreviousNonNullLocation = null;
		currentObjectDialogue.Clear();
		currentQuestionChoice = 0;
		season = Season.Spring;
		currentSpeaker = null;
		currentViewportTarget = Vector2.Zero;
		cursorTileHintCheckTimer = 0;
		CustomData = new SerializableDictionary<string, string>();
		player.team.sharedDailyLuck.Value = 0.001;
		dayOfMonth = 0;
		debrisSoundInterval = 0f;
		debrisWeather.Clear();
		debugMode = false;
		debugOutput = null;
		debugPresenceString = "In menus";
		delayedActions.Clear();
		morningSongPlayAction = null;
		dialogueButtonScale = 1f;
		dialogueButtonShrinking = false;
		dialogueTyping = false;
		dialogueTypingInterval = 0;
		dialogueUp = false;
		dialogueWidth = 1024;
		displayFarmer = true;
		displayHUD = true;
		downPolling = 0f;
		drawGrid = false;
		drawLighting = false;
		elliottBookName = "Blue Tower";
		endOfNightMenus.Clear();
		errorMessage = "";
		eveningColor = new Color(255, 255, 0, 255);
		eventOver = false;
		eventUp = false;
		exitToTitle = false;
		facingDirectionAfterWarp = 0;
		fadeIn = true;
		fadeToBlack = false;
		fadeToBlackAlpha = 1.02f;
		farmEvent = null;
		flashAlpha = 0f;
		freezeControls = false;
		gamePadAButtonPolling = 0;
		gameTimeInterval = 0;
		globalFade = false;
		globalFadeSpeed = 0f;
		haltAfterCheck = false;
		hasLoadedGame = false;
		hasStartedDay = false;
		hitShakeTimer = 0;
		hudMessages.Clear();
		isActionAtCurrentCursorTile = false;
		isDebrisWeather = false;
		isInspectionAtCurrentCursorTile = false;
		isLightning = false;
		isQuestion = false;
		isRaining = false;
		wasGreenRain = false;
		isSnowing = false;
		killScreen = false;
		lastCursorMotionWasMouse = true;
		lastCursorTile = Vector2.Zero;
		lastMousePositionBeforeFade = Point.Zero;
		leftPolling = 0f;
		loadingMessage = "";
		locationRequest = null;
		warpingForForcedRemoteEvent = false;
		locations.Clear();
		mailbox.Clear();
		mapDisplayDevice = CreateDisplayDevice(content, base.GraphicsDevice);
		messageAfterPause = "";
		messagePause = false;
		mouseClickPolling = 0;
		mouseCursor = cursor_default;
		multiplayerMode = 0;
		netWorldState = new NetRoot<NetWorldState>(new NetWorldState());
		newDay = false;
		nonWarpFade = false;
		noteBlockTimer = 0f;
		npcDialogues = null;
		objectDialoguePortraitPerson = null;
		hasApplied1_3_UpdateChanges = false;
		hasApplied1_4_UpdateChanges = false;
		remoteEventQueue.Clear();
		bannedUsers?.Clear();
		nextClickableMenu.Clear();
		actionsWhenPlayerFree.Clear();
		onScreenMenus.Clear();
		onScreenMenus.Add(new Toolbar());
		dayTimeMoneyBox = new DayTimeMoneyBox();
		onScreenMenus.Add(dayTimeMoneyBox);
		buffsDisplay = new BuffsDisplay();
		onScreenMenus.Add(buffsDisplay);
		bool gamepadControls = options.gamepadControls;
		bool snappyMenus = options.snappyMenus;
		options = new Options();
		options.gamepadControls = gamepadControls;
		options.snappyMenus = snappyMenus;
		foreach (KeyValuePair<long, Farmer> otherFarmer in otherFarmers)
		{
			otherFarmer.Value.unload();
		}
		otherFarmers.Clear();
		outdoorLight = new Color(255, 255, 0, 255);
		overlayMenu = null;
		panFacingDirectionWait = false;
		panMode = false;
		panModeString = null;
		pauseAccumulator = 0f;
		paused = false;
		pauseThenDoFunctionTimer = 0;
		pauseTime = 0f;
		previousViewportPosition = Vector2.Zero;
		questionChoices.Clear();
		quit = false;
		rightClickPolling = 0;
		rightPolling = 0f;
		runThreshold = 0.5f;
		samBandName = "The Alfalfas";
		saveOnNewDay = true;
		startingCabins = 0;
		cabinsSeparate = false;
		screenGlow = false;
		screenGlowAlpha = 0f;
		screenGlowColor = new Color(0, 0, 0, 0);
		screenGlowHold = false;
		screenGlowMax = 0f;
		screenGlowRate = 0.005f;
		screenGlowUp = false;
		screenOverlayTempSprites.Clear();
		uiOverlayTempSprites.Clear();
		newGameSetupOptions.Clear();
		showingEndOfNightStuff = false;
		spawnMonstersAtNight = false;
		staminaShakeTimer = 0;
		textColor = new Color(34, 17, 34, 255);
		textShadowColor = new Color(206, 156, 95, 255);
		thumbstickMotionAccell = 1f;
		thumbstickMotionMargin = 0;
		thumbstickPollingTimer = 0;
		thumbStickSensitivity = 0.1f;
		timeOfDay = 600;
		timeOfDayAfterFade = -1;
		timerUntilMouseFade = 0;
		toggleFullScreen = false;
		ResetToolSpriteSheet();
		triggerPolling = 0;
		uniqueIDForThisGame = (ulong)(DateTime.UtcNow - new DateTime(2012, 6, 22)).TotalSeconds;
		upPolling = 0f;
		viewportFreeze = false;
		viewportHold = 0;
		viewportPositionLerp = Vector2.Zero;
		viewportReachedTarget = null;
		viewportSpeed = 2f;
		viewportTarget = new Vector2(-2.1474836E+09f, -2.1474836E+09f);
		wasMouseVisibleThisFrame = true;
		wasRainingYesterday = false;
		weatherForTomorrow = "Sun";
		elliottPiano = 0;
		weatherIcon = 0;
		weddingToday = false;
		whereIsTodaysFest = null;
		worldStateIDs.Clear();
		whichFarm = 0;
		whichModFarm = null;
		windGust = 0f;
		xLocationAfterWarp = 0;
		game1.xTileContent.Dispose();
		game1.xTileContent = CreateContentManager(content.ServiceProvider, content.RootDirectory);
		year = 1;
		yLocationAfterWarp = 0;
		mailDeliveredFromMailForTomorrow.Clear();
		bundleType = BundleType.Default;
		JojaMart.Morris = null;
		AmbientLocationSounds.onLocationLeave();
		WeatherDebris.globalWind = -0.25f;
		Utility.killAllStaticLoopingSoundCues();
		OptionsDropDown.selected = null;
		JunimoNoteMenu.tempSprites.Clear();
		JunimoNoteMenu.screenSwipe = null;
		JunimoNoteMenu.canClick = true;
		GameMenu.forcePreventClose = false;
		Club.timesPlayedCalicoJack = 0;
		MineShaft.activeMines.RemoveAll(delegate(MineShaft level)
		{
			level.OnRemoved();
			return true;
		});
		MineShaft.permanentMineChanges.Clear();
		MineShaft.numberOfCraftedStairsUsedThisRun = 0;
		MineShaft.mushroomLevelsGeneratedToday.Clear();
		VolcanoDungeon.activeLevels.RemoveAll(delegate(VolcanoDungeon level)
		{
			level.OnRemoved();
			return true;
		});
		ItemRegistry.ResetCache();
		Rumble.stopRumbling();
	}

	/// <summary>Reset all game state when returning to the title screen from a loaded save.</summary>
	/// <remarks>This is specialized code that should normally not be called directly. See also <see cref="M:StardewValley.Game1.ResetGameStateOnTitleScreen" />.</remarks>
	public virtual void CleanupReturningToTitle()
	{
		if (game1.IsMainInstance)
		{
			foreach (Game1 gameInstance in GameRunner.instance.gameInstances)
			{
				if (gameInstance != this)
				{
					GameRunner.instance.RemoveGameInstance(gameInstance);
				}
			}
		}
		else
		{
			GameRunner.instance.RemoveGameInstance(this);
		}
		multiplayer.Disconnect(Multiplayer.DisconnectType.ExitedToMainMenu);
		ResetGameStateOnTitleScreen();
		serverHost = null;
		client = null;
		server = null;
		TitleMenu.subMenu = null;
		game1.refreshWindowSettings();
		if (activeClickableMenu is TitleMenu titleMenu)
		{
			titleMenu.applyPreferences();
			activeClickableMenu.gameWindowSizeChanged(graphics.GraphicsDevice.Viewport.Bounds, graphics.GraphicsDevice.Viewport.Bounds);
		}
	}

	public bool CanTakeScreenshots()
	{
		return true;
	}

	/// <summary>Get the absolute path to the folder containing screenshots.</summary>
	/// <param name="createIfMissing">Whether to create the folder if it doesn't exist already.</param>
	public string GetScreenshotFolder(bool createIfMissing = true)
	{
		return Program.GetLocalAppDataFolder("Screenshots", createIfMissing);
	}

	public bool CanBrowseScreenshots()
	{
		return Directory.Exists(GetScreenshotFolder(createIfMissing: false));
	}

	public bool CanZoomScreenshots()
	{
		return true;
	}

	public void BrowseScreenshots()
	{
		string screenshotFolder = GetScreenshotFolder(createIfMissing: false);
		if (Directory.Exists(screenshotFolder))
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = screenshotFolder,
					UseShellExecute = true,
					Verb = "open"
				});
			}
			catch (Exception exception)
			{
				log.Error("Failed to open screenshot folder.", exception);
			}
		}
	}

	public string takeMapScreenshot(float? in_scale, string screenshot_name, Action onDone)
	{
		if (currentLocation == null)
		{
			return null;
		}
		float scale = 1f;
		if (in_scale.HasValue)
		{
			scale = in_scale.Value;
		}
		string screenshot_name2 = screenshot_name;
		if (string.IsNullOrWhiteSpace(screenshot_name))
		{
			DateTime utcNow = DateTime.UtcNow;
			screenshot_name2 = SaveGame.FilterFileName(player.name.Value) + $"_{utcNow.Month}-{utcNow.Day}-{utcNow.Year}_{(int)utcNow.TimeOfDay.TotalMilliseconds}";
		}
		return takeMapScreenshot(currentLocation, scale, screenshot_name2, onDone);
	}

	private unsafe string takeMapScreenshot(GameLocation screenshotLocation, float scale, string screenshot_name, Action onDone)
	{
		string text = screenshot_name + ".png";
		GetScreenshotRegion(screenshotLocation, out var startX, out var startY, out var width, out var height);
		SKSurface sKSurface = null;
		bool flag;
		int num;
		int num2;
		do
		{
			flag = false;
			num = (int)((float)width * scale);
			num2 = (int)((float)height * scale);
			try
			{
				sKSurface = SKSurface.Create(num, num2, SKColorType.Rgb888x, SKAlphaType.Opaque);
			}
			catch (Exception exception)
			{
				log.Error("Map Screenshot: Error trying to create Bitmap.", exception);
				flag = true;
			}
			if (flag)
			{
				scale -= 0.25f;
			}
			if (scale <= 0f)
			{
				return null;
			}
		}
		while (flag);
		int num3 = 2048;
		int num4 = (int)((float)num3 * scale);
		xTile.Dimensions.Rectangle rectangle = viewport;
		bool flag2 = displayHUD;
		takingMapScreenshot = true;
		float baseZoomLevel = options.baseZoomLevel;
		options.baseZoomLevel = 1f;
		RenderTarget2D renderTarget2D = _lightmap;
		_lightmap = null;
		bool flag3 = false;
		try
		{
			allocateLightmap(num3, num3);
			int num5 = (int)Math.Ceiling((float)num / (float)num4);
			int num6 = (int)Math.Ceiling((float)num2 / (float)num4);
			for (int i = 0; i < num6; i++)
			{
				for (int j = 0; j < num5; j++)
				{
					int num7 = num4;
					int num8 = num4;
					int num9 = j * num4;
					int num10 = i * num4;
					if (num9 + num4 > num)
					{
						num7 += num - (num9 + num4);
					}
					if (num10 + num4 > num2)
					{
						num8 += num2 - (num10 + num4);
					}
					if (num8 <= 0 || num7 <= 0)
					{
						continue;
					}
					Microsoft.Xna.Framework.Rectangle rectangle2 = new Microsoft.Xna.Framework.Rectangle(num9, num10, num7, num8);
					RenderTarget2D renderTarget2D2 = new RenderTarget2D(graphics.GraphicsDevice, num3, num3, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
					viewport = new xTile.Dimensions.Rectangle(j * num3 + startX, i * num3 + startY, num3, num3);
					_draw(currentGameTime, renderTarget2D2);
					RenderTarget2D renderTarget2D3 = new RenderTarget2D(graphics.GraphicsDevice, num7, num8, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
					base.GraphicsDevice.SetRenderTarget(renderTarget2D3);
					spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
					Color white = Color.White;
					spriteBatch.Draw(renderTarget2D2, Vector2.Zero, renderTarget2D2.Bounds, white, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
					spriteBatch.End();
					renderTarget2D2.Dispose();
					base.GraphicsDevice.SetRenderTarget(null);
					Color[] array = new Color[num7 * num8];
					renderTarget2D3.GetData(array);
					SKBitmap sKBitmap = new SKBitmap(rectangle2.Width, rectangle2.Height, SKColorType.Rgb888x, SKAlphaType.Opaque);
					byte* ptr = (byte*)sKBitmap.GetPixels().ToPointer();
					for (int k = 0; k < num8; k++)
					{
						for (int l = 0; l < num7; l++)
						{
							*(ptr++) = array[l + k * num7].R;
							*(ptr++) = array[l + k * num7].G;
							*(ptr++) = array[l + k * num7].B;
							*(ptr++) = byte.MaxValue;
						}
					}
					SKPaint paint = new SKPaint();
					sKSurface.Canvas.DrawBitmap(sKBitmap, SKRect.Create(rectangle2.X, rectangle2.Y, num7, num8), paint);
					sKBitmap.Dispose();
					renderTarget2D3.Dispose();
				}
			}
			string path = Path.Combine(GetScreenshotFolder(), text);
			sKSurface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).SaveTo(new FileStream(path, FileMode.OpenOrCreate));
			sKSurface.Dispose();
		}
		catch (Exception exception2)
		{
			log.Error("Map Screenshot: Error taking screenshot.", exception2);
			base.GraphicsDevice.SetRenderTarget(null);
			flag3 = true;
		}
		if (_lightmap != null)
		{
			_lightmap.Dispose();
			_lightmap = null;
		}
		_lightmap = renderTarget2D;
		options.baseZoomLevel = baseZoomLevel;
		takingMapScreenshot = false;
		displayHUD = flag2;
		viewport = rectangle;
		if (flag3)
		{
			return null;
		}
		return text;
	}

	private static void GetScreenshotRegion(GameLocation screenshotLocation, out int startX, out int startY, out int width, out int height)
	{
		startX = 0;
		startY = 0;
		width = screenshotLocation.map.DisplayWidth;
		height = screenshotLocation.map.DisplayHeight;
		try
		{
			string[] mapPropertySplitBySpaces = screenshotLocation.GetMapPropertySplitBySpaces("ScreenshotRegion");
			if (mapPropertySplitBySpaces.Length != 0)
			{
				if (!ArgUtility.TryGetInt(mapPropertySplitBySpaces, 0, out var value, out var error, "int topLeftX") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces, 1, out var value2, out error, "int topLeftY") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces, 2, out var value3, out error, "int bottomRightX") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces, 3, out var value4, out error, "int bottomRightY"))
				{
					screenshotLocation.LogMapPropertyError("ScreenshotRegion", mapPropertySplitBySpaces, error);
					return;
				}
				startX = value * 64;
				startY = value2 * 64;
				width = (value3 + 1) * 64 - startX;
				height = (value4 + 1) * 64 - startY;
			}
		}
		catch (Exception exception)
		{
			log.Error("GetScreenshotRegion failed with exception:", exception);
		}
	}
}
