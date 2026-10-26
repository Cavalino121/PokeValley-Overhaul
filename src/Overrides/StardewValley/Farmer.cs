using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using Netcode.Validation;
using StardewValley.Audio;
using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Companions;
using StardewValley.Constants;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData.LocationContexts;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Network.NetEvents;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.Quests;
using StardewValley.SpecialOrders;
using StardewValley.Tools;
using StardewValley.Util;
using xTile.Dimensions;
using xTile.Tiles;

namespace StardewValley;

public class Farmer : Character, IComparable
{
	public class EmoteType
	{
		public string emoteString = "";

		public int emoteIconIndex = -1;

		public FarmerSprite.AnimationFrame[] animationFrames;

		public bool hidden;

		public int facingDirection = 2;

		public string displayNameKey;

		public string displayName => Game1.content.LoadString(displayNameKey);

		public EmoteType(string emote_string = "", string display_name_key = "", int icon_index = -1, FarmerSprite.AnimationFrame[] frames = null, int facing_direction = 2, bool is_hidden = false)
		{
			emoteString = emote_string;
			emoteIconIndex = icon_index;
			animationFrames = frames;
			facingDirection = facing_direction;
			hidden = is_hidden;
			displayNameKey = "Strings\\UI:" + display_name_key;
		}
	}

	public const int millisecondsPerSpeedUnit = 64;

	public const byte halt = 64;

	public const byte up = 1;

	public const byte right = 2;

	public const byte down = 4;

	public const byte left = 8;

	public const byte run = 16;

	public const byte release = 32;

	public const int farmingSkill = 0;

	public const int miningSkill = 3;

	public const int fishingSkill = 1;

	public const int foragingSkill = 2;

	public const int combatSkill = 4;

	public const int luckSkill = 5;

	public const float interpolationConstant = 0.5f;

	public const int runningSpeed = 5;

	public const int walkingSpeed = 2;

	public const int caveNothing = 0;

	public const int caveBats = 1;

	public const int caveMushrooms = 2;

	public const int millisecondsInvincibleAfterDamage = 1200;

	public const int millisecondsPerFlickerWhenInvincible = 50;

	public const int startingStamina = 270;

	public const int totalLevels = 35;

	public const int maxInventorySpace = 36;

	public const int hotbarSize = 12;

	public const int eyesOpen = 0;

	public const int eyesHalfShut = 4;

	public const int eyesClosed = 1;

	public const int eyesRight = 2;

	public const int eyesLeft = 3;

	public const int eyesWide = 5;

	public const int rancher = 0;

	public const int tiller = 1;

	public const int butcher = 2;

	public const int shepherd = 3;

	public const int artisan = 4;

	public const int agriculturist = 5;

	public const int fisher = 6;

	public const int trapper = 7;

	public const int angler = 8;

	public const int pirate = 9;

	/// <summary>The Mariner profession, which makes crab pots no longer produce junk items.</summary>
	/// <remarks>For legacy reasons, the Luremaster and Mariner profession constants are swapped (i.e. <see cref="F:StardewValley.Farmer.mariner" /> is Luremaster and <see cref="F:StardewValley.Farmer.baitmaster" /> is Mariner).</remarks>
	public const int baitmaster = 10;

	/// <summary>The Luremaster profession, which makes crab pots no longer require bait.</summary>
	/// <inheritdoc cref="F:StardewValley.Farmer.baitmaster" path="/remarks" />
	public const int mariner = 11;

	public const int forester = 12;

	public const int gatherer = 13;

	public const int lumberjack = 14;

	public const int tapper = 15;

	public const int botanist = 16;

	public const int tracker = 17;

	public const int miner = 18;

	public const int geologist = 19;

	public const int blacksmith = 20;

	public const int burrower = 21;

	public const int excavator = 22;

	public const int gemologist = 23;

	public const int fighter = 24;

	public const int scout = 25;

	public const int brute = 26;

	public const int defender = 27;

	public const int acrobat = 28;

	public const int desperado = 29;

	public static int MaximumTrinkets = 1;

	public readonly NetObjectList<Quest> questLog = new NetObjectList<Quest>();

	public readonly NetIntHashSet professions = new NetIntHashSet();

	public readonly NetList<Point, NetPoint> newLevels = new NetList<Point, NetPoint>();

	[XmlIgnore]
	public Queue<int> newLevelSparklingTexts = new Queue<int>();

	[XmlIgnore]
	public SparklingText sparklingText;

	public readonly NetArray<int, NetInt> experiencePoints = new NetArray<int, NetInt>(6);

	/// <summary>The backing field for <see cref="P:StardewValley.Farmer.Items" />.</summary>
	[XmlElement("items")]
	public readonly NetRef<Inventory> netItems = new NetRef<Inventory>(new Inventory());

	[XmlArrayItem("int")]
	public readonly NetStringHashSet dialogueQuestionsAnswered = new NetStringHashSet();

	[XmlElement("cookingRecipes")]
	public readonly NetStringDictionary<int, NetInt> cookingRecipes = new NetStringDictionary<int, NetInt>();

	[XmlElement("craftingRecipes")]
	public readonly NetStringDictionary<int, NetInt> craftingRecipes = new NetStringDictionary<int, NetInt>();

	[XmlElement("activeDialogueEvents")]
	public readonly NetStringDictionary<int, NetInt> activeDialogueEvents = new NetStringDictionary<int, NetInt>();

	[XmlElement("previousActiveDialogueEvents")]
	public readonly NetStringDictionary<int, NetInt> previousActiveDialogueEvents = new NetStringDictionary<int, NetInt>();

	/// <summary>The trigger actions which have been run for the player.</summary>
	public readonly NetStringHashSet triggerActionsRun = new NetStringHashSet();

	/// <summary>The event IDs which the player has seen.</summary>
	[XmlArrayItem("int")]
	public readonly NetStringHashSet eventsSeen = new NetStringHashSet();

	public readonly NetIntHashSet secretNotesSeen = new NetIntHashSet();

	public HashSet<string> songsHeard = new HashSet<string>();

	public readonly NetIntHashSet achievements = new NetIntHashSet();

	[XmlArrayItem("int")]
	public readonly NetStringList specialItems = new NetStringList();

	[XmlArrayItem("int")]
	public readonly NetStringList specialBigCraftables = new NetStringList();

	/// <summary>The mail flags set on the player. This includes both actual mail letter IDs matching <c>Data/mail</c>, and non-mail flags used to track game state like <c>ccIsComplete</c> (community center complete).</summary>
	/// <remarks>See also <see cref="F:StardewValley.Farmer.mailForTomorrow" /> and <see cref="F:StardewValley.Farmer.mailbox" />.</remarks>
	public readonly NetStringHashSet mailReceived = new NetStringHashSet();

	/// <summary>The mail flags that will be added to the <see cref="F:StardewValley.Farmer.mailbox" /> tomorrow.</summary>
	public readonly NetStringHashSet mailForTomorrow = new NetStringHashSet();

	/// <summary>The mail IDs matching <c>Data/mail</c> in the player's mailbox, if any. Each time the player checks their mailbox, one letter from this set will be displayed and moved into <see cref="F:StardewValley.Farmer.mailReceived" />.</summary>
	public readonly NetStringList mailbox = new NetStringList();

	/// <summary>The internal names of locations which the player has previously visited.</summary>
	/// <remarks>This contains the <see cref="P:StardewValley.GameLocation.Name" /> field, not <see cref="P:StardewValley.GameLocation.NameOrUniqueName" />. They're equivalent for most locations, but building interiors will use their common name (like <c>Barn</c> instead of <c>Barn{unique ID}</c> for barns).</remarks>
	public readonly NetStringHashSet locationsVisited = new NetStringHashSet();

	public readonly NetInt timeWentToBed = new NetInt();

	[XmlIgnore]
	public readonly NetList<Companion, NetRef<Companion>> companions = new NetList<Companion, NetRef<Companion>>();

	/// <summary>Whether the local player has moved today.</summary>
	[XmlIgnore]
	public bool hasMoved;

	/// <summary>Whether the local player has interacted with a statue of blessings today.</summary>
	[XmlIgnore]
	public bool hasBeenBlessedByStatueToday;

	/// <summary>Whether the player slept using an item bed in a place that doesn't have an actual bed furniture item.</summary>
	public readonly NetBool sleptInTemporaryBed = new NetBool();

	[XmlIgnore]
	public readonly NetBool requestingTimePause = new NetBool
	{
		InterpolationWait = false
	};

	public Stats stats = new Stats();

	[XmlIgnore]
	public readonly NetRef<Inventory> personalShippingBin = new NetRef<Inventory>(new Inventory());

	[XmlIgnore]
	public IList<Item> displayedShippedItems = new List<Item>();

	[XmlElement("biteChime")]
	public NetInt biteChime = new NetInt(-1);

	[XmlIgnore]
	public float usernameDisplayTime;

	[XmlIgnore]
	protected NetRef<Item> _recoveredItem = new NetRef<Item>();

	public NetObjectList<Item> itemsLostLastDeath = new NetObjectList<Item>();

	public List<int> movementDirections = new List<int>();

	[XmlElement("farmName")]
	public readonly NetString farmName = new NetString("");

	[XmlElement("favoriteThing")]
	public readonly NetString favoriteThing = new NetString();

	[XmlElement("horseName")]
	public readonly NetString horseName = new NetString();

	public string slotName;

	public bool slotCanHost;

	[XmlIgnore]
	public readonly NetString tempFoodItemTextureName = new NetString();

	[XmlIgnore]
	public readonly NetRectangle tempFoodItemSourceRect = new NetRectangle();

	[XmlIgnore]
	public bool hasReceivedToolUpgradeMessageYet;

	[XmlIgnore]
	public readonly BuffManager buffs = new BuffManager();

	[XmlIgnore]
	public IList<OutgoingMessage> messageQueue = new List<OutgoingMessage>();

	[XmlIgnore]
	public readonly NetLong uniqueMultiplayerID = new NetLong(Utility.RandomLong());

	[XmlElement("userID")]
	public readonly NetString userID = new NetString("");

	[XmlIgnore]
	public string previousLocationName = "";

	[XmlIgnore]
	public readonly NetString platformType = new NetString("");

	[XmlIgnore]
	public readonly NetString platformID = new NetString("");

	[XmlIgnore]
	public readonly NetBool hasMenuOpen = new NetBool(value: false);

	[XmlIgnore]
	public readonly Color DEFAULT_SHIRT_COLOR = Color.White;

	public string defaultChatColor;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.Farmer.whichPetType" /> instead.</summary>
	[XmlElement("catPerson")]
	public bool? obsolete_catPerson;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.canUnderstandDwarves" /> instead.</summary>
	[XmlElement("canUnderstandDwarves")]
	public bool? obsolete_canUnderstandDwarves;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.hasClubCard" /> instead.</summary>
	[XmlElement("hasClubCard")]
	public bool? obsolete_hasClubCard;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.hasDarkTalisman" /> instead.</summary>
	[XmlElement("hasDarkTalisman")]
	public bool? obsolete_hasDarkTalisman;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.hasMagicInk" /> instead.</summary>
	[XmlElement("hasMagicInk")]
	public bool? obsolete_hasMagicInk;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.hasMagnifyingGlass" /> instead.</summary>
	[XmlElement("hasMagnifyingGlass")]
	public bool? obsolete_hasMagnifyingGlass;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.hasRustyKey" /> instead.</summary>
	[XmlElement("hasRustyKey")]
	public bool? obsolete_hasRustyKey;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.hasSkullKey" /> instead.</summary>
	[XmlElement("hasSkullKey")]
	public bool? obsolete_hasSkullKey;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.hasSpecialCharm" /> instead.</summary>
	[XmlElement("hasSpecialCharm")]
	public bool? obsolete_hasSpecialCharm;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.HasTownKey" /> instead.</summary>
	[XmlElement("HasTownKey")]
	public bool? obsolete_hasTownKey;

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.hasUnlockedSkullDoor" /> instead.</summary>
	[XmlElement("hasUnlockedSkullDoor")]
	public bool? obsolete_hasUnlockedSkullDoor;

	/// <summary>Obsolete since 1.3. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.Farmer.friendshipData" /> for NPC friendships or <see cref="F:StardewValley.FarmerTeam.friendshipData" /> for farmhands instead.</summary>
	[XmlElement("friendships")]
	public SerializableDictionary<string, int[]> obsolete_friendships;

	/// <summary>Obsolete since 1.3. This is only kept to preserve data from old save files. Use <see cref="M:StardewValley.Farmer.GetDaysMarried" /> instead.</summary>
	[XmlElement("daysMarried")]
	public int? obsolete_daysMarried;

	/// <summary>The preferred pet type, matching an ID in <c>Data/Pets</c>. The vanilla pet types are <see cref="F:StardewValley.Characters.Pet.type_cat" /> and <see cref="F:StardewValley.Characters.Pet.type_dog" />.</summary>
	public string whichPetType = "Cat";

	/// <summary>The selected breed ID in <c>Data/Pets</c> for the <see cref="F:StardewValley.Farmer.whichPetType" />.</summary>
	public string whichPetBreed = "0";

	[XmlIgnore]
	public bool isAnimatingMount;

	[XmlElement("acceptedDailyQuest")]
	public readonly NetBool acceptedDailyQuest = new NetBool(value: false);

	[XmlIgnore]
	public Item mostRecentlyGrabbedItem;

	[XmlIgnore]
	public Item itemToEat;

	[XmlElement("farmerRenderer")]
	private readonly NetRef<FarmerRenderer> farmerRenderer = new NetRef<FarmerRenderer>();

	[XmlIgnore]
	public readonly NetInt toolPower = new NetInt();

	[XmlIgnore]
	public readonly NetInt toolHold = new NetInt();

	public Vector2 mostRecentBed;

	public static Dictionary<int, string> hairStyleMetadataFile = null;

	public static List<int> allHairStyleIndices = null;

	[XmlIgnore]
	public static Dictionary<int, HairStyleMetadata> hairStyleMetadata = new Dictionary<int, HairStyleMetadata>();

	[XmlElement("emoteFavorites")]
	public readonly List<string> emoteFavorites = new List<string>();

	[XmlElement("performedEmotes")]
	public readonly SerializableDictionary<string, bool> performedEmotes = new SerializableDictionary<string, bool>();

	/// <summary>If set, the unqualified item ID of the <see cref="F:StardewValley.ItemRegistry.type_shirt" /> item to show this player wearing instead of the equipped <see cref="F:StardewValley.Farmer.shirtItem" />.</summary>
	[XmlElement("shirt")]
	public readonly NetString shirt = new NetString("1000");

	[XmlElement("hair")]
	public readonly NetInt hair = new NetInt(0);

	[XmlElement("skin")]
	public readonly NetInt skin = new NetInt(0);

	[XmlElement("shoes")]
	public readonly NetString shoes = new NetString("2");

	[XmlElement("accessory")]
	public readonly NetInt accessory = new NetInt(-1);

	[XmlElement("facialHair")]
	public readonly NetInt facialHair = new NetInt(-1);

	/// <summary>If set, the unqualified item ID of the <see cref="F:StardewValley.ItemRegistry.type_pants" /> item to show this player wearing instead of the equipped <see cref="F:StardewValley.Farmer.pantsItem" />.</summary>
	[XmlElement("pants")]
	public readonly NetString pants = new NetString("0");

	[XmlIgnore]
	public int currentEyes;

	[XmlIgnore]
	public int blinkTimer;

	[XmlIgnore]
	public readonly NetInt netFestivalScore = new NetInt();

	/// <summary>The last date that we submitted the Calico Egg Rating to Gil.</summary>
	public readonly NetRef<WorldDate> lastGotPrizeFromGil = new NetRef<WorldDate>();

	/// <summary>The last date that we accepted a Desert Festival fishing quest.</summary>
	public readonly NetRef<WorldDate> lastDesertFestivalFishingQuest = new NetRef<WorldDate>();

	[XmlIgnore]
	public float temporarySpeedBuff;

	[XmlElement("hairstyleColor")]
	public readonly NetColor hairstyleColor = new NetColor(new Color(193, 90, 50));

	[XmlIgnore]
	public NetBool prismaticHair = new NetBool();

	/// <summary>The color to apply when rendering <see cref="F:StardewValley.Farmer.pants" />. Most code should use <see cref="M:StardewValley.Farmer.GetPantsColor" /> instead.</summary>
	[XmlElement("pantsColor")]
	public readonly NetColor pantsColor = new NetColor(new Color(46, 85, 183));

	[XmlElement("newEyeColor")]
	public readonly NetColor newEyeColor = new NetColor(new Color(122, 68, 52));

	[XmlElement("hat")]
	public readonly NetRef<Hat> hat = new NetRef<Hat>();

	[XmlElement("boots")]
	public readonly NetRef<Boots> boots = new NetRef<Boots>();

	[XmlElement("leftRing")]
	public readonly NetRef<Ring> leftRing = new NetRef<Ring>();

	[XmlElement("rightRing")]
	public readonly NetRef<Ring> rightRing = new NetRef<Ring>();

	[XmlElement("shirtItem")]
	public readonly NetRef<Clothing> shirtItem = new NetRef<Clothing>();

	[XmlElement("pantsItem")]
	public readonly NetRef<Clothing> pantsItem = new NetRef<Clothing>();

	[XmlIgnore]
	public readonly NetDancePartner dancePartner = new NetDancePartner();

	[XmlIgnore]
	public bool ridingMineElevator;

	[XmlIgnore]
	public readonly NetBool exhausted = new NetBool();

	[XmlElement("divorceTonight")]
	public readonly NetBool divorceTonight = new NetBool();

	[XmlElement("changeWalletTypeTonight")]
	public readonly NetBool changeWalletTypeTonight = new NetBool();

	[XmlIgnore]
	public AnimatedSprite.endOfAnimationBehavior toolOverrideFunction;

	[XmlIgnore]
	public NetBool onBridge = new NetBool();

	[XmlIgnore]
	public SuspensionBridge bridge;

	private readonly NetInt netDeepestMineLevel = new NetInt();

	[XmlElement("currentToolIndex")]
	private readonly NetInt currentToolIndex = new NetInt(0);

	[XmlIgnore]
	private readonly NetRef<Item> temporaryItem = new NetRef<Item>();

	[XmlIgnore]
	private readonly NetRef<Item> cursorSlotItem = new NetRef<Item>();

	[XmlIgnore]
	public readonly NetBool netItemStowed = new NetBool(value: false);

	protected bool _itemStowed;

	public string gameVersion = "-1";

	public string gameVersionLabel;

	[XmlIgnore]
	public bool isFakeEventActor;

	[XmlElement("bibberstyke")]
	public readonly NetInt bobberStyle = new NetInt(0);

	public bool usingRandomizedBobber;

	[XmlElement("caveChoice")]
	public readonly NetInt caveChoice = new NetInt();

	[XmlElement("farmingLevel")]
	public readonly NetInt farmingLevel = new NetInt();

	[XmlElement("miningLevel")]
	public readonly NetInt miningLevel = new NetInt();

	[XmlElement("combatLevel")]
	public readonly NetInt combatLevel = new NetInt();

	[XmlElement("foragingLevel")]
	public readonly NetInt foragingLevel = new NetInt();

	[XmlElement("fishingLevel")]
	public readonly NetInt fishingLevel = new NetInt();

	[XmlElement("luckLevel")]
	public readonly NetInt luckLevel = new NetInt();

	[XmlElement("maxStamina")]
	public readonly NetInt maxStamina = new NetInt(270);

	[XmlElement("maxItems")]
	public readonly NetInt maxItems = new NetInt(12);

	[XmlElement("lastSeenMovieWeek")]
	public readonly NetInt lastSeenMovieWeek = new NetInt(-1);

	[XmlIgnore]
	public readonly NetString viewingLocation = new NetString();

	private readonly NetFloat netStamina = new NetFloat(270f);

	[XmlIgnore]
	public bool ignoreItemConsumptionThisFrame;

	[XmlIgnore]
	[NotNetField]
	public NetRoot<FarmerTeam> teamRoot = new NetRoot<FarmerTeam>(new FarmerTeam());

	public int clubCoins;

	public int trashCanLevel;

	private NetLong netMillisecondsPlayed = new NetLong
	{
		DeltaAggregateTicks = (ushort)(60 * (Game1.realMilliSecondsPerGameTenMinutes / 1000))
	};

	[XmlElement("toolBeingUpgraded")]
	public readonly NetRef<Tool> toolBeingUpgraded = new NetRef<Tool>();

	[XmlElement("daysLeftForToolUpgrade")]
	public readonly NetInt daysLeftForToolUpgrade = new NetInt();

	[XmlElement("houseUpgradeLevel")]
	public readonly NetInt houseUpgradeLevel = new NetInt(0);

	[XmlElement("daysUntilHouseUpgrade")]
	public readonly NetInt daysUntilHouseUpgrade = new NetInt(-1);

	public bool showChestColorPicker = true;

	public bool hasWateringCanEnchantment;

	[XmlIgnore]
	public List<BaseEnchantment> enchantments = new List<BaseEnchantment>();

	public readonly int BaseMagneticRadius = 128;

	public int temporaryInvincibilityTimer;

	public int currentTemporaryInvincibilityDuration = 1200;

	[XmlIgnore]
	public float rotation;

	private int craftingTime = 1000;

	private int raftPuddleCounter = 250;

	private int raftBobCounter = 1000;

	public int health = 100;

	public int maxHealth = 100;

	private readonly NetInt netTimesReachedMineBottom = new NetInt(0);

	public float difficultyModifier = 1f;

	[XmlIgnore]
	public Vector2 jitter = Vector2.Zero;

	[XmlIgnore]
	public Vector2 lastPosition;

	[XmlIgnore]
	public Vector2 lastGrabTile = Vector2.Zero;

	[XmlIgnore]
	public float jitterStrength;

	[XmlIgnore]
	public float xOffset;

	/// <summary>The net-synchronized backing field for <see cref="P:StardewValley.Farmer.Gender" />.</summary>
	[XmlElement("gender")]
	public readonly NetEnum<Gender> netGender = new NetEnum<Gender>();

	[XmlIgnore]
	public bool canMove = true;

	[XmlIgnore]
	public bool running;

	[XmlIgnore]
	public bool ignoreCollisions;

	[XmlIgnore]
	public readonly NetBool usingTool = new NetBool(value: false);

	[XmlIgnore]
	public bool isEating;

	[XmlIgnore]
	public readonly NetBool isInBed = new NetBool(value: false);

	[XmlIgnore]
	public bool forceTimePass;

	[XmlIgnore]
	public bool isRafting;

	[XmlIgnore]
	public bool usingSlingshot;

	[XmlIgnore]
	public readonly NetBool bathingClothes = new NetBool(value: false);

	[XmlIgnore]
	public bool canOnlyWalk;

	[XmlIgnore]
	public bool temporarilyInvincible;

	[XmlIgnore]
	public bool flashDuringThisTemporaryInvincibility = true;

	private readonly NetBool netCanReleaseTool = new NetBool(value: false);

	[XmlIgnore]
	public bool isCrafting;

	[XmlIgnore]
	public bool isEmoteAnimating;

	[XmlIgnore]
	public bool passedOut;

	[XmlIgnore]
	protected int _emoteGracePeriod;

	[XmlIgnore]
	private BoundingBoxGroup temporaryPassableTiles = new BoundingBoxGroup();

	[XmlIgnore]
	public readonly NetBool hidden = new NetBool();

	[XmlElement("basicShipped")]
	public readonly NetStringDictionary<int, NetInt> basicShipped = new NetStringDictionary<int, NetInt>();

	[XmlElement("mineralsFound")]
	public readonly NetStringDictionary<int, NetInt> mineralsFound = new NetStringDictionary<int, NetInt>();

	[XmlElement("recipesCooked")]
	public readonly NetStringDictionary<int, NetInt> recipesCooked = new NetStringDictionary<int, NetInt>();

	[XmlElement("fishCaught")]
	public readonly NetStringIntArrayDictionary fishCaught = new NetStringIntArrayDictionary();

	[XmlElement("archaeologyFound")]
	public readonly NetStringIntArrayDictionary archaeologyFound = new NetStringIntArrayDictionary();

	[XmlElement("callsReceived")]
	public readonly NetStringDictionary<int, NetInt> callsReceived = new NetStringDictionary<int, NetInt>();

	public SerializableDictionary<string, SerializableDictionary<string, int>> giftedItems;

	[XmlElement("tailoredItems")]
	public readonly NetStringDictionary<int, NetInt> tailoredItems = new NetStringDictionary<int, NetInt>();

	[XmlElement("friendshipData")]
	public readonly NetStringDictionary<Friendship, NetRef<Friendship>> friendshipData = new NetStringDictionary<Friendship, NetRef<Friendship>>();

	[XmlIgnore]
	public NetString locationBeforeForcedEvent = new NetString(null);

	[XmlIgnore]
	public Vector2 positionBeforeEvent;

	[XmlIgnore]
	public int orientationBeforeEvent;

	[XmlIgnore]
	public int swimTimer;

	[XmlIgnore]
	public int regenTimer;

	[XmlIgnore]
	public int timerSinceLastMovement;

	[XmlIgnore]
	public int noMovementPause;

	[XmlIgnore]
	public int freezePause;

	[XmlIgnore]
	public float yOffset;

	/// <summary>The backing field for <see cref="P:StardewValley.Farmer.spouse" />.</summary>
	protected readonly NetString netSpouse = new NetString();

	public string dateStringForSaveGame;

	public int? dayOfMonthForSaveGame;

	public int? seasonForSaveGame;

	public int? yearForSaveGame;

	[XmlIgnore]
	public Vector2 armOffset;

	[XmlIgnore]
	public readonly NetRef<Horse> netMount = new NetRef<Horse>();

	[XmlIgnore]
	public ISittable sittingFurniture;

	[XmlIgnore]
	public NetBool isSitting = new NetBool();

	[XmlIgnore]
	public NetVector2 mapChairSitPosition = new NetVector2(new Vector2(-1f, -1f));

	[XmlIgnore]
	public NetBool hasCompletedAllMonsterSlayerQuests = new NetBool(value: false);

	[XmlIgnore]
	public bool isStopSitting;

	[XmlIgnore]
	protected bool _wasSitting;

	[XmlIgnore]
	public Vector2 lerpStartPosition;

	[XmlIgnore]
	public Vector2 lerpEndPosition;

	[XmlIgnore]
	public float lerpPosition = -1f;

	[XmlIgnore]
	public float lerpDuration = -1f;

	[XmlIgnore]
	protected Item _lastSelectedItem;

	[XmlIgnore]
	protected internal Tool _lastEquippedTool;

	[XmlElement("qiGems")]
	public NetIntDelta netQiGems = new NetIntDelta
	{
		Minimum = 0
	};

	[XmlElement("JOTPKProgress")]
	public NetRef<AbigailGame.JOTPKProgress> jotpkProgress = new NetRef<AbigailGame.JOTPKProgress>();

	[XmlIgnore]
	public NetBool hasUsedDailyRevive = new NetBool(value: false);

	[XmlElement("trinketItem")]
	public readonly NetList<Trinket, NetRef<Trinket>> trinketItems = new NetList<Trinket, NetRef<Trinket>>();

	private readonly NetEvent0 fireToolEvent = new NetEvent0(interpolate: true);

	private readonly NetEvent0 beginUsingToolEvent = new NetEvent0(interpolate: true);

	private readonly NetEvent0 endUsingToolEvent = new NetEvent0(interpolate: true);

	private readonly NetEvent0 sickAnimationEvent = new NetEvent0();

	private readonly NetEvent0 passOutEvent = new NetEvent0();

	private readonly NetEvent0 haltAnimationEvent = new NetEvent0();

	private readonly NetEvent1Field<Object, NetRef<Object>> drinkAnimationEvent = new NetEvent1Field<Object, NetRef<Object>>();

	private readonly NetEvent1Field<Object, NetRef<Object>> eatAnimationEvent = new NetEvent1Field<Object, NetRef<Object>>();

	private readonly NetEvent1Field<string, NetString> doEmoteEvent = new NetEvent1Field<string, NetString>();

	private readonly NetEvent1Field<long, NetLong> kissFarmerEvent = new NetEvent1Field<long, NetLong>();

	private readonly NetEvent1Field<float, NetFloat> synchronizedJumpEvent = new NetEvent1Field<float, NetFloat>();

	public readonly NetEvent1Field<string, NetString> renovateEvent = new NetEvent1Field<string, NetString>();

	[XmlElement("chestConsumedLevels")]
	public readonly NetIntDictionary<bool, NetBool> chestConsumedMineLevels = new NetIntDictionary<bool, NetBool>();

	public int saveTime;

	[XmlIgnore]
	public float drawLayerDisambiguator;

	[XmlElement("isCustomized")]
	public readonly NetBool isCustomized = new NetBool(value: false);

	[XmlElement("homeLocation")]
	public readonly NetString homeLocation = new NetString("FarmHouse");

	[XmlElement("lastSleepLocation")]
	public readonly NetString lastSleepLocation = new NetString();

	[XmlElement("lastSleepPoint")]
	public readonly NetPoint lastSleepPoint = new NetPoint();

	[XmlElement("disconnectDay")]
	public readonly NetInt disconnectDay = new NetInt(-1);

	[XmlElement("disconnectLocation")]
	public readonly NetString disconnectLocation = new NetString();

	[XmlElement("disconnectPosition")]
	public readonly NetVector2 disconnectPosition = new NetVector2();

	public static readonly EmoteType[] EMOTES = new EmoteType[22]
	{
		new EmoteType("happy", "Emote_Happy", 32),
		new EmoteType("sad", "Emote_Sad", 28),
		new EmoteType("heart", "Emote_Heart", 20),
		new EmoteType("exclamation", "Emote_Exclamation", 16),
		new EmoteType("note", "Emote_Note", 56),
		new EmoteType("sleep", "Emote_Sleep", 24),
		new EmoteType("game", "Emote_Game", 52),
		new EmoteType("question", "Emote_Question", 8),
		new EmoteType("x", "Emote_X", 36),
		new EmoteType("pause", "Emote_Pause", 40),
		new EmoteType("blush", "Emote_Blush", 60, null, 2, is_hidden: true),
		new EmoteType("angry", "Emote_Angry", 12),
		new EmoteType("yes", "Emote_Yes", 56, new FarmerSprite.AnimationFrame[7]
		{
			new FarmerSprite.AnimationFrame(0, 250, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("jingle1");
				}
			}),
			new FarmerSprite.AnimationFrame(16, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(0, 250, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(16, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(0, 250, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(16, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(0, 250, secondaryArm: false, flip: false)
		}),
		new EmoteType("no", "Emote_No", 36, new FarmerSprite.AnimationFrame[5]
		{
			new FarmerSprite.AnimationFrame(25, 250, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("cancel");
				}
			}),
			new FarmerSprite.AnimationFrame(27, 250, secondaryArm: true, flip: false),
			new FarmerSprite.AnimationFrame(25, 250, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(27, 250, secondaryArm: true, flip: false),
			new FarmerSprite.AnimationFrame(25, 250, secondaryArm: false, flip: false)
		}),
		new EmoteType("sick", "Emote_Sick", 12, new FarmerSprite.AnimationFrame[8]
		{
			new FarmerSprite.AnimationFrame(104, 350, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("croak");
				}
			}),
			new FarmerSprite.AnimationFrame(105, 350, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(104, 350, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(105, 350, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(104, 350, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(105, 350, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(104, 350, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(105, 350, secondaryArm: false, flip: false)
		}),
		new EmoteType("laugh", "Emote_Laugh", 56, new FarmerSprite.AnimationFrame[8]
		{
			new FarmerSprite.AnimationFrame(102, 150, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("dustMeep");
				}
			}),
			new FarmerSprite.AnimationFrame(103, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(102, 150, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("dustMeep");
				}
			}),
			new FarmerSprite.AnimationFrame(103, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(102, 150, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("dustMeep");
				}
			}),
			new FarmerSprite.AnimationFrame(103, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(102, 150, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("dustMeep");
				}
			}),
			new FarmerSprite.AnimationFrame(103, 150, secondaryArm: false, flip: false)
		}),
		new EmoteType("surprised", "Emote_Surprised", 16, new FarmerSprite.AnimationFrame[1] { new FarmerSprite.AnimationFrame(94, 1500, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
		{
			if (who.ShouldHandleAnimationSound())
			{
				who.playNearbySoundLocal("batScreech");
			}
			who.jumpWithoutSound(4f);
			who.jitterStrength = 1f;
		}) }),
		new EmoteType("hi", "Emote_Hi", 56, new FarmerSprite.AnimationFrame[4]
		{
			new FarmerSprite.AnimationFrame(3, 250, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("give_gift");
				}
			}),
			new FarmerSprite.AnimationFrame(85, 250, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(3, 250, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(85, 250, secondaryArm: false, flip: false)
		}),
		new EmoteType("taunt", "Emote_Taunt", 12, new FarmerSprite.AnimationFrame[10]
		{
			new FarmerSprite.AnimationFrame(3, 250, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(102, 50, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(10, 250, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("hitEnemy");
				}
				who.jitterStrength = 1f;
			}).AddFrameEndAction(delegate(Farmer who)
			{
				who.stopJittering();
			}),
			new FarmerSprite.AnimationFrame(3, 250, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(102, 50, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(10, 250, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("hitEnemy");
				}
				who.jitterStrength = 1f;
			}).AddFrameEndAction(delegate(Farmer who)
			{
				who.stopJittering();
			}),
			new FarmerSprite.AnimationFrame(3, 250, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(102, 50, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(10, 250, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("hitEnemy");
				}
				who.jitterStrength = 1f;
			}).AddFrameEndAction(delegate(Farmer who)
			{
				who.stopJittering();
			}),
			new FarmerSprite.AnimationFrame(3, 500, secondaryArm: false, flip: false)
		}, 2, is_hidden: true),
		new EmoteType("uh", "Emote_Uh", 40, new FarmerSprite.AnimationFrame[1] { new FarmerSprite.AnimationFrame(10, 1500, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
		{
			if (who.ShouldHandleAnimationSound())
			{
				who.playNearbySoundLocal("clam_tone");
			}
		}) }),
		new EmoteType("music", "Emote_Music", 56, new FarmerSprite.AnimationFrame[9]
		{
			new FarmerSprite.AnimationFrame(98, 150, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				who.playHarpEmoteSound();
			}),
			new FarmerSprite.AnimationFrame(99, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(100, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(98, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(99, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(100, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(98, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(99, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(100, 150, secondaryArm: false, flip: false)
		}, 2, is_hidden: true),
		new EmoteType("jar", "Emote_Jar", -1, new FarmerSprite.AnimationFrame[6]
		{
			new FarmerSprite.AnimationFrame(111, 150, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(111, 300, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("fishingRodBend");
				}
				who.jitterStrength = 1f;
			}).AddFrameEndAction(delegate(Farmer who)
			{
				who.stopJittering();
			}),
			new FarmerSprite.AnimationFrame(111, 500, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(111, 300, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("fishingRodBend");
				}
				who.jitterStrength = 1f;
			}).AddFrameEndAction(delegate(Farmer who)
			{
				who.stopJittering();
			}),
			new FarmerSprite.AnimationFrame(111, 500, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(112, 1000, secondaryArm: false, flip: false).AddFrameAction(delegate(Farmer who)
			{
				if (who.ShouldHandleAnimationSound())
				{
					who.playNearbySoundLocal("coin");
				}
				who.jumpWithoutSound(4f);
			})
		}, 1, is_hidden: true)
	};

	[XmlIgnore]
	public int emoteFacingDirection = 2;

	private int toolPitchAccumulator;

	[XmlIgnore]
	public readonly NetInt toolHoldStartTime = new NetInt();

	private int charactercollisionTimer;

	private NPC collisionNPC;

	public float movementMultiplier = 0.01f;

	public bool hasVisibleQuests
	{
		get
		{
			foreach (SpecialOrder specialOrder in team.specialOrders)
			{
				if (!specialOrder.IsHidden())
				{
					return true;
				}
			}
			foreach (Quest item in questLog)
			{
				if (item != null && !item.IsHidden())
				{
					return true;
				}
			}
			return false;
		}
	}

	public Item recoveredItem
	{
		get
		{
			return _recoveredItem.Value;
		}
		set
		{
			_recoveredItem.Value = value;
		}
	}

	/// <summary>Obsolete since 1.6. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Farmer.Gender" /> or <see cref="P:StardewValley.Farmer.IsMale" /> instead.</summary>
	[XmlElement("isMale")]
	public bool? obsolete_isMale
	{
		get
		{
			return null;
		}
		set
		{
			if (value.HasValue)
			{
				Gender = ((!value.Value) ? Gender.Female : Gender.Male);
			}
		}
	}

	/// <summary>Whether the player's preferred pet type is <see cref="F:StardewValley.Characters.Pet.type_cat" />.</summary>
	/// <remarks>See also <see cref="F:StardewValley.Farmer.whichPetType" />.</remarks>
	[XmlIgnore]
	public bool catPerson => whichPetType == "Cat";

	[XmlIgnore]
	public int festivalScore
	{
		get
		{
			return netFestivalScore.Value;
		}
		set
		{
			if (team?.festivalScoreStatus != null)
			{
				team.festivalScoreStatus.UpdateState(festivalScore.ToString() ?? "");
			}
			netFestivalScore.Value = value;
		}
	}

	public int deepestMineLevel
	{
		get
		{
			return netDeepestMineLevel.Value;
		}
		set
		{
			netDeepestMineLevel.Value = value;
		}
	}

	public float stamina
	{
		get
		{
			return netStamina.Value;
		}
		set
		{
			netStamina.Value = value;
		}
	}

	[XmlIgnore]
	public FarmerTeam team
	{
		get
		{
			if (Game1.player != null && this != Game1.player)
			{
				return Game1.player.team;
			}
			return teamRoot.Value;
		}
	}

	public uint totalMoneyEarned
	{
		get
		{
			return (uint)teamRoot.Value.totalMoneyEarned.Value;
		}
		set
		{
			if (teamRoot.Value.totalMoneyEarned.Value != 0)
			{
				if (value >= 15000 && teamRoot.Value.totalMoneyEarned.Value < 15000)
				{
					Game1.multiplayer.globalChatInfoMessage("Earned15k", farmName.Value);
				}
				if (value >= 50000 && teamRoot.Value.totalMoneyEarned.Value < 50000)
				{
					Game1.multiplayer.globalChatInfoMessage("Earned50k", farmName.Value);
				}
				if (value >= 250000 && teamRoot.Value.totalMoneyEarned.Value < 250000)
				{
					Game1.multiplayer.globalChatInfoMessage("Earned250k", farmName.Value);
				}
				if (value >= 1000000 && teamRoot.Value.totalMoneyEarned.Value < 1000000)
				{
					Game1.multiplayer.globalChatInfoMessage("Earned1m", farmName.Value);
				}
				if (value >= 10000000 && teamRoot.Value.totalMoneyEarned.Value < 10000000)
				{
					Game1.multiplayer.globalChatInfoMessage("Earned10m", farmName.Value);
				}
				if (value >= 100000000 && teamRoot.Value.totalMoneyEarned.Value < 100000000)
				{
					Game1.multiplayer.globalChatInfoMessage("Earned100m", farmName.Value);
				}
			}
			teamRoot.Value.totalMoneyEarned.Value = (int)value;
		}
	}

	public ulong millisecondsPlayed
	{
		get
		{
			return (ulong)netMillisecondsPlayed.Value;
		}
		set
		{
			netMillisecondsPlayed.Value = (long)value;
		}
	}

	/// <summary>Whether <strong>any player</strong> has found the Dwarvish Translation Guide that allows speaking to dwarves.</summary>
	[XmlIgnore]
	public bool canUnderstandDwarves
	{
		get
		{
			return Game1.MasterPlayer.mailReceived.Contains("HasDwarvishTranslationGuide");
		}
		set
		{
			Game1.player.team.RequestSetMail(PlayerActionTarget.Host, "HasDwarvishTranslationGuide", MailType.Received, value);
		}
	}

	/// <summary>Whether this player has unlocked access to the casino club.</summary>
	[XmlIgnore]
	public bool hasClubCard
	{
		get
		{
			return mailReceived.Contains("HasClubCard");
		}
		set
		{
			mailReceived.Toggle("HasClubCard", value);
		}
	}

	/// <summary>Whether this player has found the dark talisman, which unblocks the railroad's northeast path.</summary>
	[XmlIgnore]
	public bool hasDarkTalisman
	{
		get
		{
			return mailReceived.Contains("HasDarkTalisman");
		}
		set
		{
			mailReceived.Toggle("HasDarkTalisman", value);
		}
	}

	/// <summary>Whether this player has found the magic ink which allows magical building construction by the Wizard.</summary>
	[XmlIgnore]
	public bool hasMagicInk
	{
		get
		{
			return mailReceived.Contains("HasMagicInk");
		}
		set
		{
			mailReceived.Toggle("HasMagicInk", value);
		}
	}

	/// <summary>Whether this player has found the magnifying glass which allows finding secret notes.</summary>
	[XmlIgnore]
	public bool hasMagnifyingGlass
	{
		get
		{
			return mailReceived.Contains("HasMagnifyingGlass");
		}
		set
		{
			mailReceived.Toggle("HasMagnifyingGlass", value);
		}
	}

	/// <summary>Whether <strong>any player</strong> has found the Rusty Key which unlocks the sewers.</summary>
	[XmlIgnore]
	public bool hasRustyKey
	{
		get
		{
			return Game1.MasterPlayer.mailReceived.Contains("HasRustyKey");
		}
		set
		{
			Game1.player.team.RequestSetMail(PlayerActionTarget.Host, "HasRustyKey", MailType.Received, value);
		}
	}

	/// <summary>Whether <strong>any player</strong> has found the Skull Key which unlocks the skull caverns.</summary>
	[XmlIgnore]
	public bool hasSkullKey
	{
		get
		{
			return Game1.MasterPlayer.mailReceived.Contains("HasSkullKey");
		}
		set
		{
			Game1.player.team.RequestSetMail(PlayerActionTarget.Host, "HasSkullKey", MailType.Received, value);
		}
	}

	/// <summary>Whether this player has the Special Charm which increases daily luck.</summary>
	[XmlIgnore]
	public bool hasSpecialCharm
	{
		get
		{
			return mailReceived.Contains("HasSpecialCharm");
		}
		set
		{
			mailReceived.Toggle("HasSpecialCharm", value);
		}
	}

	/// <summary>Whether this player has unlocked the 'Key to the Town' item which lets them enter all town buildings.</summary>
	[XmlIgnore]
	public bool HasTownKey
	{
		get
		{
			return mailReceived.Contains("HasTownKey");
		}
		set
		{
			mailReceived.Toggle("HasTownKey", value);
		}
	}

	/// <summary>Whether the player has unlocked the door to the skull caverns using <see cref="P:StardewValley.Farmer.hasSkullKey" />.</summary>
	[XmlIgnore]
	public bool hasUnlockedSkullDoor
	{
		get
		{
			return mailReceived.Contains("HasUnlockedSkullDoor");
		}
		set
		{
			mailReceived.Toggle("HasUnlockedSkullDoor", value);
		}
	}

	[XmlIgnore]
	public bool hasPendingCompletedQuests
	{
		get
		{
			foreach (SpecialOrder specialOrder in team.specialOrders)
			{
				if (specialOrder.participants.ContainsKey(UniqueMultiplayerID) && specialOrder.ShouldDisplayAsComplete())
				{
					return true;
				}
			}
			foreach (Quest item in questLog)
			{
				if (!item.IsHidden() && item.ShouldDisplayAsComplete() && !item.destroy.Value)
				{
					return true;
				}
			}
			return false;
		}
	}

	[XmlElement("useSeparateWallets")]
	public bool useSeparateWallets
	{
		get
		{
			return teamRoot.Value.useSeparateWallets.Value;
		}
		set
		{
			teamRoot.Value.useSeparateWallets.Value = value;
		}
	}

	[XmlElement("theaterBuildDate")]
	public long theaterBuildDate
	{
		get
		{
			return teamRoot.Value.theaterBuildDate.Value;
		}
		set
		{
			teamRoot.Value.theaterBuildDate.Value = value;
		}
	}

	public int timesReachedMineBottom
	{
		get
		{
			return netTimesReachedMineBottom.Value;
		}
		set
		{
			netTimesReachedMineBottom.Value = value;
		}
	}

	[XmlIgnore]
	public bool canReleaseTool
	{
		get
		{
			return netCanReleaseTool.Value;
		}
		set
		{
			netCanReleaseTool.Value = value;
		}
	}

	/// <summary>The player's NPC spouse or roommate.</summary>
	[XmlElement("spouse")]
	public string spouse
	{
		get
		{
			if (!string.IsNullOrEmpty(netSpouse.Value))
			{
				return netSpouse.Value;
			}
			return null;
		}
		set
		{
			if (value == null)
			{
				netSpouse.Value = "";
			}
			else
			{
				netSpouse.Value = value;
			}
		}
	}

	[XmlIgnore]
	public bool isUnclaimedFarmhand
	{
		get
		{
			if (!IsMainPlayer)
			{
				return !isCustomized.Value;
			}
			return false;
		}
	}

	[XmlIgnore]
	public Horse mount
	{
		get
		{
			return netMount.Value;
		}
		set
		{
			setMount(value);
		}
	}

	[XmlIgnore]
	public int MaxItems
	{
		get
		{
			return maxItems.Value;
		}
		set
		{
			maxItems.Value = value;
		}
	}

	[XmlIgnore]
	public int Level => (farmingLevel.Value + fishingLevel.Value + foragingLevel.Value + combatLevel.Value + miningLevel.Value + luckLevel.Value) / 2;

	[XmlIgnore]
	public int FarmingLevel => Math.Max(farmingLevel.Value + buffs.FarmingLevel, 0);

	[XmlIgnore]
	public int MiningLevel => Math.Max(miningLevel.Value + buffs.MiningLevel, 0);

	[XmlIgnore]
	public int CombatLevel => Math.Max(combatLevel.Value + buffs.CombatLevel, 0);

	[XmlIgnore]
	public int ForagingLevel => Math.Max(foragingLevel.Value + buffs.ForagingLevel, 0);

	[XmlIgnore]
	public int FishingLevel => Math.Max(fishingLevel.Value + buffs.FishingLevel, 0);

	[XmlIgnore]
	public int LuckLevel => Math.Max(luckLevel.Value + buffs.LuckLevel, 0);

	[XmlIgnore]
	public double DailyLuck => Math.Min(Math.Max(team.sharedDailyLuck.Value + (double)(hasSpecialCharm ? 0.025f : 0f), -0.20000000298023224), 0.20000000298023224);

	[XmlIgnore]
	public int HouseUpgradeLevel
	{
		get
		{
			return houseUpgradeLevel.Value;
		}
		set
		{
			houseUpgradeLevel.Value = value;
		}
	}

	[XmlIgnore]
	public BoundingBoxGroup TemporaryPassableTiles
	{
		get
		{
			return temporaryPassableTiles;
		}
		set
		{
			temporaryPassableTiles = value;
		}
	}

	[XmlIgnore]
	public Inventory Items => netItems.Value;

	[XmlIgnore]
	public int MagneticRadius => Math.Max(BaseMagneticRadius + buffs.MagneticRadius, 0);

	[XmlIgnore]
	public Item ActiveItem
	{
		get
		{
			if (TemporaryItem != null)
			{
				return TemporaryItem;
			}
			if (_itemStowed)
			{
				return null;
			}
			if (currentToolIndex.Value < Items.Count && Items[currentToolIndex.Value] != null)
			{
				return Items[currentToolIndex.Value];
			}
			return null;
		}
		set
		{
			netItemStowed.Set(newValue: false);
			if (value == null)
			{
				removeItemFromInventory(ActiveItem);
			}
			else
			{
				addItemToInventory(value, CurrentToolIndex);
			}
		}
	}

	[XmlIgnore]
	public Object ActiveObject
	{
		get
		{
			return ActiveItem as Object;
		}
		set
		{
			ActiveItem = value;
		}
	}

	/// <summary>The player's gender identity.</summary>
	[XmlIgnore]
	public override Gender Gender
	{
		get
		{
			return netGender.Value;
		}
		set
		{
			netGender.Value = value;
		}
	}

	[XmlIgnore]
	public bool IsMale => netGender.Value == Gender.Male;

	[XmlIgnore]
	public ISet<string> DialogueQuestionsAnswered => dialogueQuestionsAnswered;

	[XmlIgnore]
	public bool CanMove
	{
		get
		{
			return canMove;
		}
		set
		{
			canMove = value;
		}
	}

	[XmlIgnore]
	public bool UsingTool
	{
		get
		{
			return usingTool.Value;
		}
		set
		{
			usingTool.Set(value);
		}
	}

	[XmlIgnore]
	public Tool CurrentTool
	{
		get
		{
			return CurrentItem as Tool;
		}
		set
		{
			while (CurrentToolIndex >= Items.Count)
			{
				Items.Add(null);
			}
			Items[CurrentToolIndex] = value;
		}
	}

	[XmlIgnore]
	public Item TemporaryItem
	{
		get
		{
			return temporaryItem.Value;
		}
		set
		{
			temporaryItem.Value = value;
		}
	}

	public Item CursorSlotItem
	{
		get
		{
			return cursorSlotItem.Value;
		}
		set
		{
			cursorSlotItem.Value = value;
		}
	}

	[XmlIgnore]
	public Item CurrentItem
	{
		get
		{
			if (TemporaryItem != null)
			{
				return TemporaryItem;
			}
			if (_itemStowed)
			{
				return null;
			}
			if (currentToolIndex.Value >= Items.Count)
			{
				return null;
			}
			return Items[currentToolIndex.Value];
		}
	}

	[XmlIgnore]
	public int CurrentToolIndex
	{
		get
		{
			return currentToolIndex.Value;
		}
		set
		{
			netItemStowed.Set(newValue: false);
			if (currentToolIndex.Value >= 0 && CurrentItem != null && value != currentToolIndex.Value)
			{
				CurrentItem.actionWhenStopBeingHeld(this);
			}
			currentToolIndex.Set(value);
		}
	}

	[XmlIgnore]
	public float Stamina
	{
		get
		{
			return stamina;
		}
		set
		{
			if (!hasBuff("statue_of_blessings_2") || !(value < stamina))
			{
				stamina = Math.Min(MaxStamina, Math.Max(value, -16f));
			}
		}
	}

	[XmlIgnore]
	public int MaxStamina => Math.Max(maxStamina.Value + buffs.MaxStamina, 0);

	[XmlIgnore]
	public int Attack => buffs.Attack;

	[XmlIgnore]
	public int Immunity => buffs.Immunity;

	[XmlIgnore]
	public override float addedSpeed
	{
		get
		{
			return buffs.Speed + ((stats.Get("Book_Speed") != 0 && !isRidingHorse()) ? 0.25f : 0f) + ((stats.Get("Book_Speed2") != 0 && !isRidingHorse()) ? 0.25f : 0f);
		}
		[Obsolete("Player speed can't be changed directly. You can add a speed buff via applyBuff instead (and optionally mark it invisible).")]
		set
		{
		}
	}

	public long UniqueMultiplayerID
	{
		get
		{
			return uniqueMultiplayerID.Value;
		}
		set
		{
			uniqueMultiplayerID.Value = value;
		}
	}

	/// <summary>Whether this is the farmer controlled by the local player, <strong>or</strong> the main farmer in an event being viewed by the local player (even if that farmer instance is a different player).</summary>
	[XmlIgnore]
	public bool IsLocalPlayer
	{
		get
		{
			if (UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
			{
				if (Game1.CurrentEvent != null)
				{
					return Game1.CurrentEvent.farmer == this;
				}
				return false;
			}
			return true;
		}
	}

	[XmlIgnore]
	public bool IsMainPlayer
	{
		get
		{
			if (!(Game1.serverHost == null) || !IsLocalPlayer)
			{
				if (Game1.serverHost != null)
				{
					return UniqueMultiplayerID == Game1.serverHost.Value.UniqueMultiplayerID;
				}
				return false;
			}
			return true;
		}
	}

	[XmlIgnore]
	public bool IsDedicatedPlayer
	{
		get
		{
			if (Game1.HasDedicatedHost && Game1.serverHost != null)
			{
				return UniqueMultiplayerID == Game1.serverHost.Value.UniqueMultiplayerID;
			}
			return false;
		}
	}

	[XmlIgnore]
	public override AnimatedSprite Sprite
	{
		get
		{
			return base.Sprite;
		}
		set
		{
			base.Sprite = value;
		}
	}

	[XmlIgnore]
	public FarmerSprite FarmerSprite
	{
		get
		{
			return (FarmerSprite)Sprite;
		}
		set
		{
			Sprite = value;
		}
	}

	[XmlIgnore]
	public FarmerRenderer FarmerRenderer
	{
		get
		{
			return farmerRenderer.Value;
		}
		set
		{
			farmerRenderer.Set(value);
		}
	}

	[XmlElement("money")]
	public int _money
	{
		get
		{
			return teamRoot.Value.GetMoney(this).Value;
		}
		set
		{
			teamRoot.Value.GetMoney(this).Value = value;
		}
	}

	[XmlIgnore]
	public int QiGems
	{
		get
		{
			return netQiGems.Value;
		}
		set
		{
			netQiGems.Value = value;
		}
	}

	[XmlIgnore]
	public int Money
	{
		get
		{
			return _money;
		}
		set
		{
			if (Game1.player != this)
			{
				throw new Exception("Cannot change another farmer's money. Use Game1.player.team.SetIndividualMoney");
			}
			int money = _money;
			_money = value;
			if (value > money)
			{
				uint num = (uint)(value - money);
				totalMoneyEarned += num;
				if (useSeparateWallets)
				{
					stats.IndividualMoneyEarned += num;
				}
				Game1.stats.checkForMoneyAchievements();
			}
		}
	}

	public override int FacingDirection
	{
		get
		{
			if (!IsLocalPlayer && !isFakeEventActor && UsingTool && CurrentTool is FishingRod { CastDirection: >=0 } fishingRod)
			{
				return fishingRod.CastDirection;
			}
			if (isEmoteAnimating)
			{
				return emoteFacingDirection;
			}
			return facingDirection.Value;
		}
		set
		{
			facingDirection.Set(value);
		}
	}

	public void addUnearnedMoney(int money)
	{
		_money += money;
	}

	public List<string> GetEmoteFavorites()
	{
		if (emoteFavorites.Count == 0)
		{
			emoteFavorites.Add("question");
			emoteFavorites.Add("heart");
			emoteFavorites.Add("yes");
			emoteFavorites.Add("happy");
			emoteFavorites.Add("pause");
			emoteFavorites.Add("sad");
			emoteFavorites.Add("no");
			emoteFavorites.Add("angry");
		}
		return emoteFavorites;
	}

	public Farmer()
	{
		farmerInit();
		Sprite = new FarmerSprite(null);
	}

	public Farmer(FarmerSprite sprite, Vector2 position, int speed, string name, List<Item> initialTools, bool isMale)
		: base(sprite, position, speed, name)
	{
		farmerInit();
		base.Name = name;
		displayName = name;
		Gender = ((!isMale) ? Gender.Female : Gender.Male);
		stamina = maxStamina.Value;
		Items.OverwriteWith(initialTools);
		for (int i = Items.Count; i < maxItems.Value; i++)
		{
			Items.Add(null);
		}
		activeDialogueEvents["Introduction"] = 6;
		if (base.currentLocation != null)
		{
			mostRecentBed = Utility.PointToVector2((base.currentLocation as FarmHouse).GetPlayerBedSpot()) * 64f;
		}
		else
		{
			mostRecentBed = new Vector2(9f, 9f) * 64f;
		}
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(uniqueMultiplayerID, "uniqueMultiplayerID").AddField(userID, "userID").AddField(platformType, "platformType")
			.AddField(platformID, "platformID")
			.AddField(hasMenuOpen, "hasMenuOpen")
			.AddField(farmerRenderer, "farmerRenderer")
			.AddField(netGender, "netGender")
			.AddField(bathingClothes, "bathingClothes")
			.AddField(shirt, "shirt")
			.AddField(pants, "pants")
			.AddField(hair, "hair")
			.AddField(skin, "skin")
			.AddField(shoes, "shoes")
			.AddField(accessory, "accessory")
			.AddField(facialHair, "facialHair")
			.AddField(hairstyleColor, "hairstyleColor")
			.AddField(pantsColor, "pantsColor")
			.AddField(newEyeColor, "newEyeColor")
			.AddField(netItems, "netItems")
			.AddField(currentToolIndex, "currentToolIndex")
			.AddField(temporaryItem, "temporaryItem")
			.AddField(cursorSlotItem, "cursorSlotItem")
			.AddField(fireToolEvent, "fireToolEvent")
			.AddField(beginUsingToolEvent, "beginUsingToolEvent")
			.AddField(endUsingToolEvent, "endUsingToolEvent")
			.AddField(hat, "hat")
			.AddField(boots, "boots")
			.AddField(leftRing, "leftRing")
			.AddField(rightRing, "rightRing")
			.AddField(hidden, "hidden")
			.AddField(usingTool, "usingTool")
			.AddField(isInBed, "isInBed")
			.AddField(bobberStyle, "bobberStyle")
			.AddField(caveChoice, "caveChoice")
			.AddField(houseUpgradeLevel, "houseUpgradeLevel")
			.AddField(daysUntilHouseUpgrade, "daysUntilHouseUpgrade")
			.AddField(netSpouse, "netSpouse")
			.AddField(mailReceived, "mailReceived")
			.AddField(mailForTomorrow, "mailForTomorrow")
			.AddField(mailbox, "mailbox")
			.AddField(triggerActionsRun, "triggerActionsRun")
			.AddField(eventsSeen, "eventsSeen")
			.AddField(locationsVisited, "locationsVisited")
			.AddField(secretNotesSeen, "secretNotesSeen")
			.AddField(netMount.NetFields, "netMount.NetFields")
			.AddField(dancePartner.NetFields, "dancePartner.NetFields")
			.AddField(divorceTonight, "divorceTonight")
			.AddField(changeWalletTypeTonight, "changeWalletTypeTonight")
			.AddField(isCustomized, "isCustomized")
			.AddField(homeLocation, "homeLocation")
			.AddField(farmName, "farmName")
			.AddField(favoriteThing, "favoriteThing")
			.AddField(horseName, "horseName")
			.AddField(netMillisecondsPlayed, "netMillisecondsPlayed")
			.AddField(netFestivalScore, "netFestivalScore")
			.AddField(friendshipData, "friendshipData")
			.AddField(drinkAnimationEvent, "drinkAnimationEvent")
			.AddField(eatAnimationEvent, "eatAnimationEvent")
			.AddField(sickAnimationEvent, "sickAnimationEvent")
			.AddField(passOutEvent, "passOutEvent")
			.AddField(doEmoteEvent, "doEmoteEvent")
			.AddField(questLog, "questLog")
			.AddField(professions, "professions")
			.AddField(newLevels, "newLevels")
			.AddField(experiencePoints, "experiencePoints")
			.AddField(dialogueQuestionsAnswered, "dialogueQuestionsAnswered")
			.AddField(cookingRecipes, "cookingRecipes")
			.AddField(craftingRecipes, "craftingRecipes")
			.AddField(activeDialogueEvents, "activeDialogueEvents")
			.AddField(previousActiveDialogueEvents, "previousActiveDialogueEvents")
			.AddField(achievements, "achievements")
			.AddField(specialItems, "specialItems")
			.AddField(specialBigCraftables, "specialBigCraftables")
			.AddField(farmingLevel, "farmingLevel")
			.AddField(miningLevel, "miningLevel")
			.AddField(combatLevel, "combatLevel")
			.AddField(foragingLevel, "foragingLevel")
			.AddField(fishingLevel, "fishingLevel")
			.AddField(luckLevel, "luckLevel")
			.AddField(maxStamina, "maxStamina")
			.AddField(netStamina, "netStamina")
			.AddField(maxItems, "maxItems")
			.AddField(chestConsumedMineLevels, "chestConsumedMineLevels")
			.AddField(toolBeingUpgraded, "toolBeingUpgraded")
			.AddField(daysLeftForToolUpgrade, "daysLeftForToolUpgrade")
			.AddField(exhausted, "exhausted")
			.AddField(netDeepestMineLevel, "netDeepestMineLevel")
			.AddField(netTimesReachedMineBottom, "netTimesReachedMineBottom")
			.AddField(netItemStowed, "netItemStowed")
			.AddField(acceptedDailyQuest, "acceptedDailyQuest")
			.AddField(lastSeenMovieWeek, "lastSeenMovieWeek")
			.AddField(shirtItem, "shirtItem")
			.AddField(pantsItem, "pantsItem")
			.AddField(personalShippingBin, "personalShippingBin")
			.AddField(viewingLocation, "viewingLocation")
			.AddField(kissFarmerEvent, "kissFarmerEvent")
			.AddField(haltAnimationEvent, "haltAnimationEvent")
			.AddField(synchronizedJumpEvent, "synchronizedJumpEvent")
			.AddField(tailoredItems, "tailoredItems")
			.AddField(basicShipped, "basicShipped")
			.AddField(mineralsFound, "mineralsFound")
			.AddField(recipesCooked, "recipesCooked")
			.AddField(archaeologyFound, "archaeologyFound")
			.AddField(fishCaught, "fishCaught")
			.AddField(biteChime, "biteChime")
			.AddField(_recoveredItem, "_recoveredItem")
			.AddField(itemsLostLastDeath, "itemsLostLastDeath")
			.AddField(renovateEvent, "renovateEvent")
			.AddField(callsReceived, "callsReceived")
			.AddField(onBridge, "onBridge")
			.AddField(lastSleepLocation, "lastSleepLocation")
			.AddField(lastSleepPoint, "lastSleepPoint")
			.AddField(sleptInTemporaryBed, "sleptInTemporaryBed")
			.AddField(timeWentToBed, "timeWentToBed")
			.AddField(hasUsedDailyRevive, "hasUsedDailyRevive")
			.AddField(jotpkProgress, "jotpkProgress")
			.AddField(requestingTimePause, "requestingTimePause")
			.AddField(isSitting, "isSitting")
			.AddField(mapChairSitPosition, "mapChairSitPosition")
			.AddField(netQiGems, "netQiGems")
			.AddField(locationBeforeForcedEvent, "locationBeforeForcedEvent")
			.AddField(hasCompletedAllMonsterSlayerQuests, "hasCompletedAllMonsterSlayerQuests")
			.AddField(buffs.NetFields, "buffs.NetFields")
			.AddField(trinketItems, "trinketItems")
			.AddField(companions, "companions")
			.AddField(prismaticHair, "prismaticHair")
			.AddField(disconnectDay, "disconnectDay")
			.AddField(disconnectLocation, "disconnectLocation")
			.AddField(disconnectPosition, "disconnectPosition")
			.AddField(tempFoodItemTextureName, "tempFoodItemTextureName")
			.AddField(tempFoodItemSourceRect, "tempFoodItemSourceRect")
			.AddField(toolHoldStartTime, "toolHoldStartTime")
			.AddField(toolHold, "toolHold")
			.AddField(toolPower, "toolPower")
			.AddField(netCanReleaseTool, "netCanReleaseTool")
			.AddField(lastGotPrizeFromGil, "lastGotPrizeFromGil")
			.AddField(lastDesertFestivalFishingQuest, "lastDesertFestivalFishingQuest");
		fireToolEvent.onEvent += performFireTool;
		beginUsingToolEvent.onEvent += performBeginUsingTool;
		endUsingToolEvent.onEvent += performEndUsingTool;
		drinkAnimationEvent.onEvent += performDrinkAnimation;
		eatAnimationEvent.onEvent += performEatAnimation;
		sickAnimationEvent.onEvent += performSickAnimation;
		passOutEvent.onEvent += performPassOut;
		doEmoteEvent.onEvent += performPlayerEmote;
		kissFarmerEvent.onEvent += performKissFarmer;
		haltAnimationEvent.onEvent += performHaltAnimation;
		synchronizedJumpEvent.onEvent += performSynchronizedJump;
		renovateEvent.onEvent += performRenovation;
		netMount.fieldChangeEvent += delegate
		{
			ClearCachedPosition();
		};
		shirtItem.fieldChangeVisibleEvent += delegate
		{
			UpdateClothing();
		};
		pantsItem.fieldChangeVisibleEvent += delegate
		{
			UpdateClothing();
		};
		trinketItems.OnArrayReplaced += OnTrinketArrayReplaced;
		trinketItems.OnElementChanged += OnTrinketChange;
		netItems.fieldChangeEvent += delegate(NetRef<Inventory> field, Inventory oldValue, Inventory newValue)
		{
			newValue.IsLocalPlayerInventory = IsLocalPlayer;
		};
	}

	private void farmerInit()
	{
		buffs.SetOwner(this);
		FarmerRenderer = new FarmerRenderer("Characters\\Farmer\\farmer_" + (IsMale ? "" : "girl_") + "base", this);
		base.currentLocation = Game1.getLocationFromName(homeLocation.Value);
		Items.Clear();
		giftedItems = new SerializableDictionary<string, SerializableDictionary<string, int>>();
		LearnDefaultRecipes();
		songsHeard.Add("title_day");
		songsHeard.Add("title_night");
		changeShirt("1000");
		changeSkinColor(0);
		changeShoeColor("2");
		farmName.FilterStringEvent += Utility.FilterDirtyWords;
		name.FilterStringEvent += Utility.FilterDirtyWords;
	}

	public virtual void OnWarp()
	{
		foreach (Companion companion in companions)
		{
			companion.OnOwnerWarp();
		}
		autoGenerateActiveDialogueEvent("firstVisit_" + base.currentLocation.Name);
		if (!Stats.AllowRetroactiveAchievements)
		{
			switch (base.currentLocation.Name)
			{
			case "CommunityCenter":
			case "JojaMart":
				Game1.stats.checkForCommunityCenterOrJojaAchievements(isDirectUnlock: true);
				break;
			case "MasteryCave":
				Game1.stats.checkForSkillAchievements(isDirectUnlock: true);
				Game1.stats.checkForStardropAchievement(isDirectUnlock: true);
				break;
			}
		}
	}

	public Trinket getFirstTrinketWithID(string id)
	{
		foreach (Trinket trinketItem in trinketItems)
		{
			if (trinketItem != null && trinketItem.ItemId == id)
			{
				return trinketItem;
			}
		}
		return null;
	}

	public bool hasTrinketWithID(string id)
	{
		foreach (Trinket trinketItem in trinketItems)
		{
			if (trinketItem != null && trinketItem.ItemId == id)
			{
				return true;
			}
		}
		return false;
	}

	public void resetAllTrinketEffects()
	{
		UnapplyAllTrinketEffects();
		ApplyAllTrinketEffects();
	}

	public virtual void ApplyAllTrinketEffects()
	{
		foreach (Trinket trinketItem in trinketItems)
		{
			if (trinketItem != null)
			{
				trinketItem.reloadSprite();
				trinketItem.Apply(this);
			}
		}
	}

	public virtual void UnapplyAllTrinketEffects()
	{
		foreach (Trinket trinketItem in trinketItems)
		{
			trinketItem?.Unapply(this);
		}
	}

	public virtual void OnTrinketArrayReplaced(NetList<Trinket, NetRef<Trinket>> list, IList<Trinket> before, IList<Trinket> after)
	{
		if ((Game1.gameMode != 0 && Utility.ShouldIgnoreValueChangeCallback()) || (!IsLocalPlayer && !isFakeEventActor && Game1.gameMode != 0))
		{
			return;
		}
		foreach (Trinket item in before)
		{
			item?.Unapply(this);
		}
		foreach (Trinket item2 in after)
		{
			item2?.Apply(this);
		}
	}

	public virtual void OnTrinketChange(NetList<Trinket, NetRef<Trinket>> list, int index, Trinket old_value, Trinket new_value)
	{
		if ((Game1.gameMode == 0 || !Utility.ShouldIgnoreValueChangeCallback()) && (IsLocalPlayer || isFakeEventActor || Game1.gameMode == 0))
		{
			old_value?.Unapply(this);
			new_value?.Apply(this);
		}
	}

	public bool CanEmote()
	{
		if (Game1.farmEvent != null)
		{
			return false;
		}
		if (Game1.eventUp && Game1.CurrentEvent != null && !Game1.CurrentEvent.playerControlSequence && IsLocalPlayer)
		{
			return false;
		}
		if (usingSlingshot)
		{
			return false;
		}
		if (isEating)
		{
			return false;
		}
		if (UsingTool)
		{
			return false;
		}
		if (!CanMove && IsLocalPlayer)
		{
			return false;
		}
		if (IsSitting())
		{
			return false;
		}
		if (isRidingHorse())
		{
			return false;
		}
		if (bathingClothes.Value)
		{
			return false;
		}
		return true;
	}

	/// <summary>Learn the recipes that have no unlock requirements.</summary>
	public void LearnDefaultRecipes()
	{
		foreach (KeyValuePair<string, string> craftingRecipe in CraftingRecipe.craftingRecipes)
		{
			if (!craftingRecipes.ContainsKey(craftingRecipe.Key) && ArgUtility.Get(craftingRecipe.Value.Split('/'), 4) == "default")
			{
				craftingRecipes.Add(craftingRecipe.Key, 0);
			}
		}
		foreach (KeyValuePair<string, string> cookingRecipe in CraftingRecipe.cookingRecipes)
		{
			if (!cookingRecipes.ContainsKey(cookingRecipe.Key) && ArgUtility.Get(cookingRecipe.Value.Split('/'), 3) == "default")
			{
				cookingRecipes.Add(cookingRecipe.Key, 0);
			}
		}
		StardewValley.Pokemon.PokemonItemManager.EnsurePlayerKnowsRecipes(this);
	}

	/// <summary>Add any recipes and mail that should have been unlocked based on criteria like skill levels and stats.</summary>
	public void AddMissedMailAndRecipes()
	{
		bool flag = false;
		foreach (KeyValuePair<string, string> cookingRecipe in CraftingRecipe.cookingRecipes)
		{
			if (CraftingRecipe.TryParseLevelRequirement(cookingRecipe.Key, cookingRecipe.Value, isCooking: true, out var skillNumber, out var minLevel) && GetUnmodifiedSkillLevel(skillNumber) >= minLevel)
			{
				cookingRecipes.TryAdd(cookingRecipe.Key, 0);
				flag = true;
			}
		}
		foreach (KeyValuePair<string, string> craftingRecipe in CraftingRecipe.craftingRecipes)
		{
			if (CraftingRecipe.TryParseLevelRequirement(craftingRecipe.Key, craftingRecipe.Value, isCooking: false, out var skillNumber2, out var minLevel2) && GetUnmodifiedSkillLevel(skillNumber2) >= minLevel2)
			{
				craftingRecipes.TryAdd(craftingRecipe.Key, 0);
			}
		}
		if (flag && !hasOrWillReceiveMail("robinKitchenLetter"))
		{
			team.RequestSetMail(PlayerActionTarget.All, "robinKitchenLetter", MailType.Now, add: true, uniqueMultiplayerID.Value);
		}
		if (farmingLevel.Value >= 10 && !hasOrWillReceiveMail("marnieAutoGrabber"))
		{
			team.RequestSetMail(PlayerActionTarget.All, "marnieAutoGrabber", MailType.Tomorrow, add: true, uniqueMultiplayerID.Value);
		}
		if (stats.Get("completedJunimoKart") != 0 && !hasOrWillReceiveMail("JunimoKart"))
		{
			team.RequestSetMail(PlayerActionTarget.All, "JunimoKart", MailType.Tomorrow, add: true, uniqueMultiplayerID.Value);
		}
		if (stats.Get("completedPrairieKing") != 0 && !hasOrWillReceiveMail("Beat_PK"))
		{
			team.RequestSetMail(PlayerActionTarget.All, "Beat_PK", MailType.Tomorrow, add: true, uniqueMultiplayerID.Value);
		}
	}

	public void performRenovation(string location_name)
	{
		if (Game1.RequireLocation(location_name) is FarmHouse farmHouse)
		{
			farmHouse.UpdateForRenovation();
		}
	}

	public void performPlayerEmote(string emote_string)
	{
		for (int i = 0; i < EMOTES.Length; i++)
		{
			EmoteType emoteType = EMOTES[i];
			if (!(emoteType.emoteString == emote_string))
			{
				continue;
			}
			performedEmotes[emote_string] = true;
			if (emoteType.animationFrames != null)
			{
				if (!CanEmote())
				{
					break;
				}
				if (isEmoteAnimating)
				{
					EndEmoteAnimation();
				}
				else if (FarmerSprite.PauseForSingleAnimation)
				{
					break;
				}
				isEmoteAnimating = true;
				_emoteGracePeriod = 200;
				if (this == Game1.player)
				{
					noMovementPause = Math.Max(noMovementPause, 200);
				}
				emoteFacingDirection = emoteType.facingDirection;
				FarmerSprite.animateOnce(emoteType.animationFrames, OnEmoteAnimationEnd);
			}
			if (emoteType.emoteIconIndex >= 0)
			{
				isEmoting = false;
				doEmote(emoteType.emoteIconIndex, nextEventCommand: false);
			}
		}
	}

	public bool ShouldHandleAnimationSound()
	{
		if (!LocalMultiplayer.IsLocalMultiplayer(is_local_only: true))
		{
			return true;
		}
		if (IsLocalPlayer)
		{
			return true;
		}
		return false;
	}

	public static List<Item> initialTools()
	{
		return new List<Item>
		{
			ItemRegistry.Create("(T)Axe"),
			ItemRegistry.Create("(T)Hoe"),
			ItemRegistry.Create("(T)WateringCan"),
			ItemRegistry.Create("(T)Pickaxe"),
			ItemRegistry.Create("(W)47")
		};
	}

	private void playHarpEmoteSound()
	{
		int[] array = new int[4] { 1200, 1600, 1900, 2400 };
		switch (Game1.random.Next(5))
		{
		case 0:
			array = new int[4] { 1200, 1600, 1900, 2400 };
			break;
		case 1:
			array = new int[4] { 1200, 1700, 2100, 2400 };
			break;
		case 2:
			array = new int[4] { 1100, 1400, 1900, 2300 };
			break;
		case 3:
			array = new int[3] { 1600, 1900, 2400 };
			break;
		case 4:
			array = new int[3] { 700, 1200, 1900 };
			break;
		}
		if (!IsLocalPlayer)
		{
			return;
		}
		if (Game1.IsMultiplayer && UniqueMultiplayerID % 111 == 0L)
		{
			array = new int[4]
			{
				800 + Game1.random.Next(4) * 100,
				1200 + Game1.random.Next(4) * 100,
				1600 + Game1.random.Next(4) * 100,
				2000 + Game1.random.Next(4) * 100
			};
			for (int i = 0; i < array.Length; i++)
			{
				DelayedAction.playSoundAfterDelay("miniharp_note", Game1.random.Next(60, 150) * i, base.currentLocation, base.Tile, array[i]);
				if (i > 1 && Game1.random.NextDouble() < 0.25)
				{
					break;
				}
			}
		}
		else
		{
			for (int j = 0; j < array.Length; j++)
			{
				DelayedAction.playSoundAfterDelay("miniharp_note", (j > 0) ? (150 + Game1.random.Next(35, 51) * j) : 0, base.currentLocation, base.Tile, array[j]);
			}
		}
	}

	private static void removeLowestUpgradeLevelTool(List<Item> items, Type toolType)
	{
		Tool tool = null;
		foreach (Item item in items)
		{
			if (item is Tool tool2 && tool2.GetType() == toolType && (tool == null || tool2.upgradeLevel.Value < tool.upgradeLevel.Value))
			{
				tool = tool2;
			}
		}
		if (tool != null)
		{
			items.Remove(tool);
		}
	}

	public static void removeInitialTools(List<Item> items)
	{
		removeLowestUpgradeLevelTool(items, typeof(Axe));
		removeLowestUpgradeLevelTool(items, typeof(Hoe));
		removeLowestUpgradeLevelTool(items, typeof(WateringCan));
		removeLowestUpgradeLevelTool(items, typeof(Pickaxe));
		Item item = items.FirstOrDefault((Item item2) => item2 is MeleeWeapon && item2.ItemId == "47");
		if (item != null)
		{
			items.Remove(item);
		}
	}

	public Point getMailboxPosition()
	{
		foreach (Building building in Game1.getFarm().buildings)
		{
			if (building.isCabin && building.HasIndoorsName(homeLocation.Value))
			{
				return building.getMailboxPosition();
			}
		}
		return Game1.getFarm().GetMainMailboxPosition();
	}

	public void ClearBuffs()
	{
		buffs.Clear();
		stopGlowing();
	}

	public bool isActive()
	{
		if (this != Game1.player)
		{
			return Game1.otherFarmers.ContainsKey(UniqueMultiplayerID);
		}
		return true;
	}

	public string getTexture()
	{
		return "Characters\\Farmer\\farmer_" + (IsMale ? "" : "girl_") + "base" + (isBald() ? "_bald" : "");
	}

	public void unload()
	{
		FarmerRenderer?.unload();
	}

	public void setInventory(List<Item> newInventory)
	{
		Items.OverwriteWith(newInventory);
		for (int i = Items.Count; i < maxItems.Value; i++)
		{
			Items.Add(null);
		}
	}

	public void makeThisTheActiveObject(Object o)
	{
		if (freeSpotsInInventory() > 0)
		{
			Item currentItem = CurrentItem;
			ActiveObject = o;
			addItemToInventory(currentItem);
		}
	}

	public int getNumberOfChildren()
	{
		return getChildrenCount();
	}

	private void setMount(Horse mount)
	{
		if (mount != null)
		{
			netMount.Value = mount;
			xOffset = -11f;
			base.Position = Utility.PointToVector2(mount.GetBoundingBox().Location);
			position.Y -= 16f;
			position.X -= 8f;
			base.speed = 2;
			showNotCarrying();
			return;
		}
		netMount.Value = null;
		collisionNPC = null;
		running = false;
		base.speed = ((Game1.isOneOfTheseKeysDown(Game1.GetKeyboardState(), Game1.options.runButton) && !Game1.options.autoRun) ? 5 : 2);
		bool flag = base.speed == 5;
		running = flag;
		if (running)
		{
			base.speed = 5;
		}
		else
		{
			base.speed = 2;
		}
		completelyStopAnimatingOrDoingAction();
		xOffset = 0f;
	}

	public bool isRidingHorse()
	{
		if (mount != null)
		{
			return !Game1.eventUp;
		}
		return false;
	}

	public List<Child> getChildren()
	{
		return Utility.getHomeOfFarmer(this).getChildren();
	}

	public int getChildrenCount()
	{
		return Utility.getHomeOfFarmer(this).getChildrenCount();
	}

	public Tool getToolFromName(string name)
	{
		foreach (Item item in Items)
		{
			if (item is Tool tool && tool.Name.Contains(name))
			{
				return tool;
			}
		}
		return null;
	}

	public override void SetMovingDown(bool b)
	{
		setMoving((byte)(4 + ((!b) ? 32 : 0)));
	}

	public override void SetMovingRight(bool b)
	{
		setMoving((byte)(2 + ((!b) ? 32 : 0)));
	}

	public override void SetMovingUp(bool b)
	{
		setMoving((byte)(1 + ((!b) ? 32 : 0)));
	}

	public override void SetMovingLeft(bool b)
	{
		setMoving((byte)(8 + ((!b) ? 32 : 0)));
	}

	public int? tryGetFriendshipLevelForNPC(string name)
	{
		if (friendshipData.TryGetValue(name, out var value))
		{
			return value.Points;
		}
		return null;
	}

	public int getFriendshipLevelForNPC(string name)
	{
		if (friendshipData.TryGetValue(name, out var value))
		{
			return value.Points;
		}
		return 0;
	}

	public int getFriendshipHeartLevelForNPC(string name)
	{
		return getFriendshipLevelForNPC(name) / 250;
	}

	/// <summary>Get whether the player is roommates with a given NPC (excluding marriage).</summary>
	/// <param name="npc">The NPC's internal name.</param>
	/// <remarks>See also <see cref="M:StardewValley.Farmer.hasRoommate" />.</remarks>
	public bool isRoommate(string name)
	{
		if (name != null && friendshipData.TryGetValue(name, out var value))
		{
			return value.IsRoommate();
		}
		return false;
	}

	/// <summary>Get whether the player is or will soon be roommates with an NPC (excluding marriage).</summary>
	public bool hasCurrentOrPendingRoommate()
	{
		if (spouse != null && friendshipData.TryGetValue(spouse, out var value))
		{
			return value.RoommateMarriage;
		}
		return false;
	}

	/// <summary>Get whether the player is roommates with an NPC (excluding marriage).</summary>
	/// <remarks>See also <see cref="M:StardewValley.Farmer.isRoommate(System.String)" />.</remarks>
	public bool hasRoommate()
	{
		return isRoommate(spouse);
	}

	public bool hasAFriendWithFriendshipPoints(int minPoints, bool datablesOnly, int maxPoints = int.MaxValue)
	{
		bool found = false;
		Utility.ForEachVillager(delegate(NPC n)
		{
			if (!datablesOnly || n.datable.Value)
			{
				int friendshipLevelForNPC = getFriendshipLevelForNPC(n.Name);
				if (friendshipLevelForNPC >= minPoints && friendshipLevelForNPC <= maxPoints)
				{
					found = true;
				}
			}
			return !found;
		});
		return found;
	}

	public bool hasAFriendWithHeartLevel(int minHeartLevel, bool datablesOnly, int maxHeartLevel = int.MaxValue)
	{
		int minPoints = minHeartLevel * 250;
		int num = maxHeartLevel * 250;
		if (num < maxHeartLevel)
		{
			num = int.MaxValue;
		}
		return hasAFriendWithFriendshipPoints(minPoints, datablesOnly, num);
	}

	public void shippedBasic(string itemId, int number)
	{
		if (!basicShipped.TryGetValue(itemId, out var value))
		{
			value = 0;
		}
		basicShipped[itemId] = value + number;
	}

	public void shiftToolbar(bool right)
	{
		if (Items == null || Items.Count < 12 || UsingTool || Game1.dialogueUp || !CanMove || !Items.HasAny() || Game1.eventUp || Game1.farmEvent != null)
		{
			return;
		}
		Game1.playSound("shwip");
		CurrentItem?.actionWhenStopBeingHeld(this);
		if (right)
		{
			IList<Item> range = Items.GetRange(0, 12);
			Items.RemoveRange(0, 12);
			Items.AddRange(range);
		}
		else
		{
			IList<Item> range2 = Items.GetRange(Items.Count - 12, 12);
			for (int i = 0; i < Items.Count - 12; i++)
			{
				range2.Add(Items[i]);
			}
			Items.OverwriteWith(range2);
		}
		netItemStowed.Set(newValue: false);
		CurrentItem?.actionWhenBeingHeld(this);
		for (int j = 0; j < Game1.onScreenMenus.Count; j++)
		{
			if (Game1.onScreenMenus[j] is Toolbar toolbar)
			{
				toolbar.shifted(right);
				break;
			}
		}
	}

	public void foundWalnut(int stack = 1)
	{
		if (Game1.netWorldState.Value.GoldenWalnutsFound < 130)
		{
			Game1.netWorldState.Value.GoldenWalnuts += stack;
			Game1.netWorldState.Value.GoldenWalnutsFound += stack;
			Game1.PerformActionWhenPlayerFree(showNutPickup);
		}
	}

	public virtual void RemoveMail(string mail_key, bool from_broadcast_list = false)
	{
		mail_key = mail_key.Replace("%&NL&%", "");
		mailReceived.Remove(mail_key);
		mailbox.Remove(mail_key);
		mailForTomorrow.Remove(mail_key);
		mailForTomorrow.Remove(mail_key + "%&NL&%");
		if (from_broadcast_list)
		{
			team.broadcastedMail.Remove("%&SM&%" + mail_key);
			team.broadcastedMail.Remove("%&MFT&%" + mail_key);
			team.broadcastedMail.Remove("%&MB&%" + mail_key);
		}
	}

	public virtual void showNutPickup()
	{
		if (!hasOrWillReceiveMail("lostWalnutFound") && !Game1.eventUp)
		{
			Game1.addMailForTomorrow("lostWalnutFound", noLetter: true);
			completelyStopAnimatingOrDoingAction();
			holdUpItemThenMessage(ItemRegistry.Create("(O)73"));
		}
		else if (hasOrWillReceiveMail("lostWalnutFound") && !Game1.eventUp)
		{
			base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(0, 240, 16, 16), 100f, 4, 2, new Vector2(0f, -96f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
			{
				motion = new Vector2(0f, -6f),
				acceleration = new Vector2(0f, 0.2f),
				stopAcceleratingWhenVelocityIsZero = true,
				attachedCharacter = this,
				positionFollowsAttachedCharacter = true
			});
		}
	}

	/// <summary>Handle the player finding an artifact object.</summary>
	/// <param name="itemId">The unqualified item ID for an <see cref="F:StardewValley.ItemRegistry.type_object" />-type item.</param>
	/// <param name="number">The number found.</param>
	public void foundArtifact(string itemId, int number)
	{
		bool flag = false;
		if (itemId == "102")
		{
			if (!hasOrWillReceiveMail("lostBookFound"))
			{
				Game1.addMailForTomorrow("lostBookFound", noLetter: true);
				flag = true;
			}
			else
			{
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14100"));
			}
			Game1.playSound("newRecipe");
			Game1.netWorldState.Value.LostBooksFound++;
			Game1.multiplayer.globalChatInfoMessage("LostBook", displayName);
		}
		if (archaeologyFound.TryGetValue(itemId, out var value))
		{
			value[0] += number;
			value[1] += number;
			archaeologyFound[itemId] = value;
		}
		else
		{
			if (archaeologyFound.Length == 0)
			{
				if (!eventsSeen.Contains("0") && itemId != "102")
				{
					addQuest("23");
				}
				mailReceived.Add("artifactFound");
				flag = true;
			}
			archaeologyFound.Add(itemId, new int[2] { number, number });
		}
		if (flag)
		{
			holdUpItemThenMessage(ItemRegistry.Create("(O)" + itemId));
		}
	}

	public void cookedRecipe(string itemId)
	{
		if (!recipesCooked.TryGetValue(itemId, out var value))
		{
			value = 0;
		}
		recipesCooked[itemId] = value + 1;
	}

	public bool caughtFish(string itemId, int size, bool from_fish_pond = false, int numberCaught = 1)
	{
		ItemMetadata metadata = ItemRegistry.GetMetadata(itemId);
		itemId = metadata.QualifiedItemId;
		bool num = !from_fish_pond && metadata.Exists() && !ItemContextTagManager.HasBaseTag(metadata.QualifiedItemId, "trash_item") && !(itemId == "(O)167") && (metadata.GetParsedData()?.ObjectType == "Fish" || metadata.QualifiedItemId == "(O)372");
		bool result = false;
		if (num)
		{
			if (fishCaught.TryGetValue(itemId, out var value))
			{
				value[0] += numberCaught;
				Game1.stats.checkForFishingAchievements();
				if (size > fishCaught[itemId][1])
				{
					value[1] = size;
					result = true;
				}
				fishCaught[itemId] = value;
			}
			else
			{
				fishCaught.Add(itemId, new int[2] { numberCaught, size });
				Game1.stats.checkForFishingAchievements();
				autoGenerateActiveDialogueEvent("fishCaught_" + metadata.LocalItemId);
			}
			NotifyQuests((Quest quest) => quest.OnFishCaught(itemId, numberCaught, size));
			if (Utility.GetDayOfPassiveFestival("SquidFest") > 0 && itemId == "(O)151")
			{
				Game1.stats.Increment(StatKeys.SquidFestScore(Game1.dayOfMonth, Game1.year), numberCaught);
			}
		}
		return result;
	}

	public virtual void gainExperience(int which, int howMuch)
	{
		if (which == 5 || howMuch <= 0)
		{
			return;
		}
		if (!IsLocalPlayer && Game1.IsServer)
		{
			queueMessage(17, Game1.player, which, howMuch);
			return;
		}
		if (Level >= 25)
		{
			int currentMasteryLevel = MasteryTrackerMenu.getCurrentMasteryLevel();
			Game1.stats.Increment("MasteryExp", Math.Max(1, (which == 0) ? (howMuch / 2) : howMuch));
			if (MasteryTrackerMenu.getCurrentMasteryLevel() > currentMasteryLevel)
			{
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:Mastery_newlevel"));
				Game1.playSound("newArtifact");
			}
		}
		int num = checkForLevelGain(experiencePoints[which], experiencePoints[which] + howMuch);
		experiencePoints[which] += howMuch;
		int num2 = -1;
		if (num != -1)
		{
			switch (which)
			{
			case 0:
				num2 = farmingLevel.Value;
				farmingLevel.Value = num;
				break;
			case 3:
				num2 = miningLevel.Value;
				miningLevel.Value = num;
				break;
			case 1:
				num2 = fishingLevel.Value;
				fishingLevel.Value = num;
				break;
			case 2:
				num2 = foragingLevel.Value;
				foragingLevel.Value = num;
				break;
			case 5:
				num2 = luckLevel.Value;
				luckLevel.Value = num;
				break;
			case 4:
				num2 = combatLevel.Value;
				combatLevel.Value = num;
				break;
			}
		}
		if (num <= num2)
		{
			return;
		}
		for (int i = num2 + 1; i <= num; i++)
		{
			newLevels.Add(new Point(which, i));
			if (newLevels.Count == 1)
			{
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:NewIdeas"));
			}
		}
	}

	public int getEffectiveSkillLevel(int whichSkill)
	{
		if (whichSkill < 0 || whichSkill > 5)
		{
			return -1;
		}
		int[] array = new int[6] { farmingLevel.Value, fishingLevel.Value, foragingLevel.Value, miningLevel.Value, combatLevel.Value, luckLevel.Value };
		for (int i = 0; i < newLevels.Count; i++)
		{
			array[newLevels[i].X]--;
		}
		return array[whichSkill];
	}

	public static int checkForLevelGain(int oldXP, int newXP)
	{
		for (int num = 10; num >= 1; num--)
		{
			if (oldXP < getBaseExperienceForLevel(num) && newXP >= getBaseExperienceForLevel(num))
			{
				return num;
			}
		}
		return -1;
	}

	public static int getBaseExperienceForLevel(int level)
	{
		return level switch
		{
			1 => 100, 
			2 => 380, 
			3 => 770, 
			4 => 1300, 
			5 => 2150, 
			6 => 3300, 
			7 => 4800, 
			8 => 6900, 
			9 => 10000, 
			10 => 15000, 
			_ => -1, 
		};
	}

	/// <summary>Mark a gift as having been revealed to the player, even if it hasn't yet been gifted.</summary>
	/// <param name="npcName">The name of the NPC.</param>
	/// <param name="itemId">The item ID.</param>
	public void revealGiftTaste(string npcName, string itemId)
	{
		if (npcName != null)
		{
			if (!giftedItems.TryGetValue(npcName, out var value))
			{
				value = (giftedItems[npcName] = new SerializableDictionary<string, int>());
			}
			value.TryAdd(itemId, 0);
		}
	}

	public void onGiftGiven(NPC npc, Object item)
	{
		if (item.bigCraftable.Value)
		{
			return;
		}
		if (!giftedItems.TryGetValue(npc.name.Value, out var value))
		{
			value = (giftedItems[npc.name.Value] = new SerializableDictionary<string, int>());
		}
		int valueOrDefault = value.GetValueOrDefault(item.ItemId);
		value[item.ItemId] = valueOrDefault + 1;
		if (team.specialOrders == null)
		{
			return;
		}
		foreach (SpecialOrder specialOrder in team.specialOrders)
		{
			specialOrder.onGiftGiven?.Invoke(this, npc, item);
		}
	}

	public bool hasGiftTasteBeenRevealed(NPC npc, string itemId)
	{
		if (hasItemBeenGifted(npc, itemId))
		{
			return true;
		}
		if (!giftedItems.TryGetValue(npc.name.Value, out var value))
		{
			return false;
		}
		return value.ContainsKey(itemId);
	}

	public bool hasItemBeenGifted(NPC npc, string itemId)
	{
		if (!giftedItems.TryGetValue(npc.name.Value, out var value))
		{
			return false;
		}
		if (!value.TryGetValue(itemId, out var value2))
		{
			return false;
		}
		return value2 > 0;
	}

	public void MarkItemAsTailored(Item item)
	{
		if (item != null)
		{
			string standardDescriptionFromItem = Utility.getStandardDescriptionFromItem(item, 1);
			if (!tailoredItems.TryGetValue(standardDescriptionFromItem, out var value))
			{
				value = 0;
			}
			tailoredItems[standardDescriptionFromItem] = value + 1;
		}
	}

	public bool HasTailoredThisItem(Item item)
	{
		if (item == null)
		{
			return false;
		}
		string standardDescriptionFromItem = Utility.getStandardDescriptionFromItem(item, 1);
		return tailoredItems.ContainsKey(standardDescriptionFromItem);
	}

	/// <summary>Handle the player finding a mineral object.</summary>
	/// <param name="itemId">The unqualified item ID for an <see cref="F:StardewValley.ItemRegistry.type_object" />-type item.</param>
	public void foundMineral(string itemId)
	{
		if (!mineralsFound.TryGetValue(itemId, out var value))
		{
			value = 0;
		}
		mineralsFound[itemId] = value + 1;
		if (!hasOrWillReceiveMail("artifactFound"))
		{
			mailReceived.Add("artifactFound");
		}
	}

	public void increaseBackpackSize(int howMuch)
	{
		MaxItems += howMuch;
		while (Items.Count < MaxItems)
		{
			Items.Add(null);
		}
	}

	[Obsolete("Most code should use Items.CountId instead. However this method works a bit differently in that the item ID can be 858 (Qi Gems), 73 (Golden Walnuts), a category number, or -777 to match seasonal wild seeds.")]
	public int getItemCount(string itemId)
	{
		return getItemCountInList(Items, itemId);
	}

	[Obsolete("Most code should use Items.CountId instead. However this method works a bit differently in that the item ID can be a category number, or -777 to match seasonal wild seeds.")]
	public int getItemCountInList(IList<Item> list, string itemId)
	{
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] != null && CraftingRecipe.ItemMatchesForCrafting(list[i], itemId))
			{
				num += list[i].Stack;
			}
		}
		return num;
	}

	/// <summary>Cause the player to lose a random number of items based on their luck after dying. These will be added to <see cref="F:StardewValley.Farmer.itemsLostLastDeath" /> so they can recover one of them.</summary>
	/// <param name="random">The RNG to use, or <c>null</c> to create one.</param>
	/// <returns>Returns the number of items lost.</returns>
	public int LoseItemsOnDeath(Random random = null)
	{
		if (random == null)
		{
			random = Utility.CreateDaySaveRandom(Game1.timeOfDay);
		}
		double chance = 0.22 - (double)LuckLevel * 0.04 - DailyLuck;
		int num = 0;
		itemsLostLastDeath.Clear();
		for (int num2 = Items.Count - 1; num2 >= 0; num2--)
		{
			Item item = Items[num2];
			if (item != null && item.CanBeLostOnDeath() && random.NextBool(chance))
			{
				num++;
				Items[num2] = null;
				itemsLostLastDeath.Add(item);
				if (num == 3)
				{
					break;
				}
			}
		}
		return num;
	}

	public void ShowSitting()
	{
		if (!IsSitting())
		{
			return;
		}
		if (sittingFurniture != null)
		{
			FacingDirection = sittingFurniture.GetSittingDirection();
		}
		if (yJumpOffset != 0)
		{
			switch (FacingDirection)
			{
			case 0:
				FarmerSprite.setCurrentSingleFrame(12, 32000);
				break;
			case 1:
				FarmerSprite.setCurrentSingleFrame(6, 32000);
				break;
			case 3:
				FarmerSprite.setCurrentSingleFrame(6, 32000, secondaryArm: false, flip: true);
				break;
			case 2:
				FarmerSprite.setCurrentSingleFrame(0, 32000);
				break;
			}
			return;
		}
		switch (FacingDirection)
		{
		case 0:
			FarmerSprite.setCurrentSingleFrame(113, 32000);
			xOffset = 0f;
			yOffset = -40f;
			break;
		case 1:
			FarmerSprite.setCurrentSingleFrame(117, 32000);
			xOffset = -4f;
			yOffset = -32f;
			break;
		case 3:
			FarmerSprite.setCurrentSingleFrame(117, 32000, secondaryArm: false, flip: true);
			xOffset = 4f;
			yOffset = -32f;
			break;
		case 2:
			FarmerSprite.setCurrentSingleFrame(107, 32000, secondaryArm: true);
			xOffset = 0f;
			yOffset = -48f;
			break;
		}
	}

	public void showRiding()
	{
		if (!isRidingHorse())
		{
			return;
		}
		xOffset = -6f;
		switch (FacingDirection)
		{
		case 0:
			FarmerSprite.setCurrentSingleFrame(113, 32000);
			break;
		case 1:
			FarmerSprite.setCurrentSingleFrame(106, 32000);
			xOffset += 2f;
			break;
		case 3:
			FarmerSprite.setCurrentSingleFrame(106, 32000, secondaryArm: false, flip: true);
			xOffset = -12f;
			break;
		case 2:
			FarmerSprite.setCurrentSingleFrame(107, 32000);
			break;
		}
		if (isMoving())
		{
			switch (mount.Sprite.currentAnimationIndex)
			{
			case 0:
				yOffset = 0f;
				break;
			case 1:
				yOffset = -4f;
				break;
			case 2:
				yOffset = -4f;
				break;
			case 3:
				yOffset = 0f;
				break;
			case 4:
				yOffset = 4f;
				break;
			case 5:
				yOffset = 4f;
				break;
			}
		}
		else
		{
			yOffset = 0f;
		}
	}

	public void showCarrying()
	{
		if (Game1.eventUp || isRidingHorse() || Game1.killScreen || IsSitting())
		{
			return;
		}
		if (bathingClothes.Value || onBridge.Value)
		{
			showNotCarrying();
			return;
		}
		if (!FarmerSprite.PauseForSingleAnimation && !isMoving())
		{
			switch (FacingDirection)
			{
			case 0:
				FarmerSprite.setCurrentFrame(144);
				break;
			case 1:
				FarmerSprite.setCurrentFrame(136);
				break;
			case 2:
				FarmerSprite.setCurrentFrame(128);
				break;
			case 3:
				FarmerSprite.setCurrentFrame(152);
				break;
			}
		}
		if (ActiveObject != null)
		{
			mostRecentlyGrabbedItem = ActiveObject;
		}
		if (IsLocalPlayer && mostRecentlyGrabbedItem?.QualifiedItemId == "(O)434")
		{
			eatHeldObject();
		}
	}

	public void showNotCarrying()
	{
		if (!FarmerSprite.PauseForSingleAnimation && !isMoving())
		{
			bool flag = canOnlyWalk || bathingClothes.Value || onBridge.Value;
			switch (FacingDirection)
			{
			case 0:
				FarmerSprite.setCurrentFrame(flag ? 16 : 48, flag ? 1 : 0);
				break;
			case 1:
				FarmerSprite.setCurrentFrame(flag ? 8 : 40, flag ? 1 : 0);
				break;
			case 2:
				FarmerSprite.setCurrentFrame((!flag) ? 32 : 0, flag ? 1 : 0);
				break;
			case 3:
				FarmerSprite.setCurrentFrame(flag ? 24 : 56, flag ? 1 : 0);
				break;
			}
		}
	}

	public int GetDaysMarried()
	{
		return GetSpouseFriendship()?.DaysMarried ?? 0;
	}

	public Friendship GetSpouseFriendship()
	{
		long? num = team.GetSpouse(UniqueMultiplayerID);
		if (num.HasValue)
		{
			long value = num.Value;
			return team.GetFriendship(UniqueMultiplayerID, value);
		}
		if (string.IsNullOrEmpty(spouse) || !friendshipData.TryGetValue(spouse, out var value2))
		{
			return null;
		}
		return value2;
	}

	public bool hasDailyQuest()
	{
		for (int num = questLog.Count - 1; num >= 0; num--)
		{
			if (questLog[num].dailyQuest.Value)
			{
				return true;
			}
		}
		return false;
	}

	public void showToolUpgradeAvailability()
	{
		int dayOfMonth = Game1.dayOfMonth;
		if (!(toolBeingUpgraded != null) || daysLeftForToolUpgrade.Value > 0 || toolBeingUpgraded.Value == null || Utility.isFestivalDay() || (!(Game1.shortDayNameFromDayOfSeason(dayOfMonth) != "Fri") && hasCompletedCommunityCenter() && !Game1.isRaining) || hasReceivedToolUpgradeMessageYet)
		{
			return;
		}
		if (Game1.newDay)
		{
			Game1.morningQueue.Enqueue(delegate
			{
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:ToolReady", toolBeingUpgraded.Value.DisplayName));
			});
		}
		else
		{
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:ToolReady", toolBeingUpgraded.Value.DisplayName));
		}
		hasReceivedToolUpgradeMessageYet = true;
	}

	public void dayupdate(int timeWentToSleep)
	{
		if (IsSitting())
		{
			StopSitting(animate: false);
		}
		resetFriendshipsForNewDay();
		LearnDefaultRecipes();
		hasUsedDailyRevive.Value = false;
		hasBeenBlessedByStatueToday = false;
		acceptedDailyQuest.Set(newValue: false);
		dancePartner.Value = null;
		festivalScore = 0;
		forceTimePass = false;
		if (daysLeftForToolUpgrade.Value > 0)
		{
			daysLeftForToolUpgrade.Value--;
		}
		if (daysUntilHouseUpgrade.Value > 0)
		{
			daysUntilHouseUpgrade.Value--;
			if (daysUntilHouseUpgrade.Value <= 0)
			{
				FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(this);
				homeOfFarmer.moveObjectsForHouseUpgrade(houseUpgradeLevel.Value + 1);
				houseUpgradeLevel.Value++;
				daysUntilHouseUpgrade.Value = -1;
				homeOfFarmer.setMapForUpgradeLevel(houseUpgradeLevel.Value);
				Game1.stats.checkForBuildingUpgradeAchievements();
				autoGenerateActiveDialogueEvent("houseUpgrade_" + houseUpgradeLevel.Value);
			}
		}
		questLog.RemoveWhere(delegate(Quest quest)
		{
			if (quest.IsTimedQuest())
			{
				quest.daysLeft.Value--;
				if (quest.daysLeft.Value <= 0)
				{
					return !quest.completed.Value;
				}
				return false;
			}
			return false;
		});
		ClearBuffs();
		if (MaxStamina >= 508)
		{
			mailReceived.Add("gotMaxStamina");
		}
		float num = Stamina;
		Stamina = MaxStamina;
		bool value = exhausted.Value;
		if (value)
		{
			exhausted.Value = false;
			Stamina = MaxStamina / 2 + 1;
		}
		int num2 = ((timeWentToBed.Value == 0) ? timeWentToSleep : timeWentToBed.Value);
		if (num2 > 2400)
		{
			float num3 = (1f - (float)(2600 - Math.Min(2600, num2)) / 200f) * (float)(MaxStamina / 2);
			Stamina -= num3;
			if (timeWentToSleep > 2700)
			{
				Stamina /= 2f;
			}
		}
		if (timeWentToSleep < 2700 && num > Stamina && !value)
		{
			Stamina = num;
		}
		health = maxHealth;
		activeDialogueEvents.RemoveWhere(delegate(KeyValuePair<string, int> pair)
		{
			string key = pair.Key;
			if (!key.Contains("_memory_"))
			{
				previousActiveDialogueEvents.TryAdd(key, 0);
			}
			activeDialogueEvents[key]--;
			if (activeDialogueEvents[key] < 0)
			{
				if (!(key == "pennyRedecorating") || Utility.getHomeOfFarmer(this).GetSpouseBed() != null)
				{
					return true;
				}
				activeDialogueEvents[key] = 0;
			}
			return false;
		});
		foreach (string key2 in previousActiveDialogueEvents.Keys)
		{
			previousActiveDialogueEvents[key2]++;
			if (previousActiveDialogueEvents[key2] == 1)
			{
				activeDialogueEvents[key2 + "_memory_oneday"] = 4;
			}
			if (previousActiveDialogueEvents[key2] == 7)
			{
				activeDialogueEvents[key2 + "_memory_oneweek"] = 4;
			}
			if (previousActiveDialogueEvents[key2] == 14)
			{
				activeDialogueEvents[key2 + "_memory_twoweeks"] = 4;
			}
			if (previousActiveDialogueEvents[key2] == 28)
			{
				activeDialogueEvents[key2 + "_memory_fourweeks"] = 4;
			}
			if (previousActiveDialogueEvents[key2] == 56)
			{
				activeDialogueEvents[key2 + "_memory_eightweeks"] = 4;
			}
			if (previousActiveDialogueEvents[key2] == 104)
			{
				activeDialogueEvents[key2 + "_memory_oneyear"] = 4;
			}
		}
		hasMoved = false;
		if (Game1.random.NextDouble() < 0.905 && !hasOrWillReceiveMail("RarecrowSociety") && Utility.doesItemExistAnywhere("(BC)136") && Utility.doesItemExistAnywhere("(BC)137") && Utility.doesItemExistAnywhere("(BC)138") && Utility.doesItemExistAnywhere("(BC)139") && Utility.doesItemExistAnywhere("(BC)140") && Utility.doesItemExistAnywhere("(BC)126") && Utility.doesItemExistAnywhere("(BC)110") && Utility.doesItemExistAnywhere("(BC)113"))
		{
			mailbox.Add("RarecrowSociety");
		}
		timeWentToBed.Value = 0;
		stats.Set("blessingOfWaters", 0);
		if (shirtItem.Value == null || pantsItem.Value == null || (!(base.currentLocation is FarmHouse) && !(base.currentLocation is IslandFarmHouse) && !(base.currentLocation is Shed)))
		{
			return;
		}
		foreach (Object value2 in base.currentLocation.netObjects.Values)
		{
			if (value2 is Mannequin mannequin && mannequin.GetMannequinData().Cursed && Game1.random.NextDouble() < 0.005 && !mannequin.swappedWithFarmerTonight.Value)
			{
				mannequin.hat.Value = Equip(mannequin.hat.Value, hat);
				mannequin.shirt.Value = Equip(mannequin.shirt.Value, shirtItem);
				mannequin.pants.Value = Equip(mannequin.pants.Value, pantsItem);
				mannequin.boots.Value = Equip(mannequin.boots.Value, boots);
				mannequin.swappedWithFarmerTonight.Value = true;
				base.currentLocation.playSound("cursed_mannequin");
				mannequin.eyeTimer = 1000;
			}
		}
	}

	public bool hasSeenActiveDialogueEvent(string eventName)
	{
		if (!activeDialogueEvents.ContainsKey(eventName))
		{
			return previousActiveDialogueEvents.ContainsKey(eventName);
		}
		return true;
	}

	public bool autoGenerateActiveDialogueEvent(string eventName, int duration = 4)
	{
		if (!hasSeenActiveDialogueEvent(eventName))
		{
			activeDialogueEvents[eventName] = duration;
			return true;
		}
		return false;
	}

	public void removeDatingActiveDialogueEvents(string npcName)
	{
		activeDialogueEvents.Remove("dating_" + npcName);
		removeActiveDialogMemoryEvents("dating_" + npcName);
		previousActiveDialogueEvents.Remove("dating_" + npcName);
	}

	public void removeMarriageActiveDialogueEvents(string npcName)
	{
		activeDialogueEvents.Remove("married_" + npcName);
		removeActiveDialogMemoryEvents("married_" + npcName);
		previousActiveDialogueEvents.Remove("married_" + npcName);
	}

	public void removeActiveDialogMemoryEvents(string activeDialogKey)
	{
		activeDialogueEvents.Remove(activeDialogKey + "_memory_oneday");
		activeDialogueEvents.Remove(activeDialogKey + "_memory_oneweek");
		activeDialogueEvents.Remove(activeDialogKey + "_memory_twoweeks");
		activeDialogueEvents.Remove(activeDialogKey + "_memory_fourweeks");
		activeDialogueEvents.Remove(activeDialogKey + "_memory_eightweeks");
		activeDialogueEvents.Remove(activeDialogKey + "_memory_oneyear");
	}

	public void doDivorce()
	{
		divorceTonight.Value = false;
		if (!isMarriedOrRoommates())
		{
			return;
		}
		NPC nPC = null;
		if (spouse != null)
		{
			nPC = getSpouse();
			if (nPC != null)
			{
				removeMarriageActiveDialogueEvents(nPC.Name);
				if (!nPC.isRoommate())
				{
					autoGenerateActiveDialogueEvent("divorced_" + nPC.Name);
				}
				spouse = null;
				specialItems.RemoveWhere((string id) => id == "460");
				if (friendshipData.TryGetValue(nPC.name.Value, out var value))
				{
					value.Points = 0;
					value.RoommateMarriage = false;
					value.Status = FriendshipStatus.Divorced;
				}
				Utility.getHomeOfFarmer(this).showSpouseRoom();
				Game1.getFarm().UpdatePatio();
				removeQuest("126");
			}
		}
		else if (team.GetSpouse(UniqueMultiplayerID).HasValue)
		{
			long value2 = team.GetSpouse(UniqueMultiplayerID).Value;
			Friendship friendship = team.GetFriendship(UniqueMultiplayerID, value2);
			friendship.Points = 0;
			friendship.RoommateMarriage = false;
			friendship.Status = FriendshipStatus.Divorced;
		}
		if (((!(nPC?.isRoommate())) ?? true) && !autoGenerateActiveDialogueEvent("divorced_once"))
		{
			autoGenerateActiveDialogueEvent("divorced_twice");
		}
	}

	public static void showReceiveNewItemMessage(Farmer who, Item item, int countAdded)
	{
		string text = item.checkForSpecialItemHoldUpMeessage();
		if (text == null)
		{
			text = ((item.TryGetTempData<bool>("FromStarterGiftBox", out var value) && value && item.QualifiedItemId == "(O)472" && countAdded == 15) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1918") : ((!item.HasContextTag("book_item")) ? ((countAdded > 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1922", countAdded, item.DisplayName) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1919", item.DisplayName, Lexicon.getProperArticleForWord(item.DisplayName))) : Game1.content.LoadString("Strings\\1_6_Strings:FoundABook", item.DisplayName)));
		}
		Game1.drawObjectDialogue(new List<string> { text });
		who.completelyStopAnimatingOrDoingAction();
	}

	public static void showEatingItem(Farmer who)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = null;
		if (who.itemToEat == null)
		{
			return;
		}
		TemporaryAnimatedSprite temporaryAnimatedSprite2 = null;
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(who.itemToEat.QualifiedItemId);
		string textureName = dataOrErrorItem.TextureName;
		Microsoft.Xna.Framework.Rectangle rectangle = dataOrErrorItem.GetSourceRect();
		Color color = Color.White;
		Color color2 = Color.White;
		if (who.tempFoodItemTextureName.Value != null)
		{
			textureName = who.tempFoodItemTextureName.Value;
			rectangle = who.tempFoodItemSourceRect.Value;
		}
		else if ((who.itemToEat as Object)?.preservedParentSheetIndex.Value != null)
		{
			if (who.itemToEat.ItemId.Equals("SmokedFish"))
			{
				ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem("(O)" + (who.itemToEat as Object).preservedParentSheetIndex.Value);
				textureName = dataOrErrorItem2.TextureName;
				rectangle = dataOrErrorItem2.GetSourceRect();
				color = new Color(130, 100, 83);
			}
			else if (who.itemToEat is ColoredObject coloredObject)
			{
				color2 = coloredObject.color.Value;
			}
		}
		switch (who.FarmerSprite.currentAnimationIndex)
		{
		case 1:
			if (who.IsLocalPlayer && who.itemToEat.QualifiedItemId == "(O)434")
			{
				temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(368, 16, 16, 16), 62.75f, 8, 2, who.Position + new Vector2(-21f, -112f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			}
			temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, rectangle, 254f, 1, 0, who.Position + new Vector2(-21f, -112f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, color, 4f, 0f, 0f, 0f);
			if (!color2.Equals(Color.White))
			{
				rectangle.X += rectangle.Width;
				temporaryAnimatedSprite2 = new TemporaryAnimatedSprite(textureName, rectangle, 254f, 1, 0, who.Position + new Vector2(-21f, -112f), flicker: false, flipped: false, (float)(who.StandingPixel.Y + 1) / 10000f + 0.01f, 0f, color2, 4f, 0f, 0f, 0f);
			}
			break;
		case 2:
			if (who.IsLocalPlayer && who.itemToEat.QualifiedItemId == "(O)434")
			{
				temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(368, 16, 16, 16), 81.25f, 8, 0, who.Position + new Vector2(-21f, -108f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, -0.01f, 0f, 0f)
				{
					motion = new Vector2(0.8f, -11f),
					acceleration = new Vector2(0f, 0.5f)
				};
				break;
			}
			if (Game1.currentLocation == who.currentLocation)
			{
				Game1.playSound("dwop");
			}
			temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, rectangle, 650f, 1, 0, who.Position + new Vector2(-21f, -108f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, color, 4f, -0.01f, 0f, 0f)
			{
				motion = new Vector2(0.8f, -11f),
				acceleration = new Vector2(0f, 0.5f)
			};
			if (!color2.Equals(Color.White))
			{
				rectangle.X += rectangle.Width;
				temporaryAnimatedSprite2 = new TemporaryAnimatedSprite(textureName, rectangle, 650f, 1, 0, who.Position + new Vector2(-21f, -108f), flicker: false, flipped: false, (float)(who.StandingPixel.Y + 1) / 10000f + 0.01f, 0f, color2, 4f, 0f, 0f, 0f)
				{
					motion = new Vector2(0.8f, -11f),
					acceleration = new Vector2(0f, 0.5f)
				};
			}
			break;
		case 3:
			who.yJumpVelocity = 6f;
			who.yJumpOffset = 1;
			break;
		case 4:
		{
			if (Game1.currentLocation == who.currentLocation && who.ShouldHandleAnimationSound())
			{
				Game1.playSound("eat");
			}
			for (int i = 0; i < 8; i++)
			{
				int num = Game1.random.Next(2, 4);
				Microsoft.Xna.Framework.Rectangle sourceRect = rectangle.Clone();
				sourceRect.X += 8;
				sourceRect.Y += 8;
				sourceRect.Width = num;
				sourceRect.Height = num;
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 400f, 1, 0, who.Position + new Vector2(24f, -48f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, color, 4f, 0f, 0f, 0f)
				{
					motion = new Vector2((float)Game1.random.Next(-30, 31) / 10f, Game1.random.Next(-6, -3)),
					acceleration = new Vector2(0f, 0.5f)
				};
				who.currentLocation.temporarySprites.Add(temporaryAnimatedSprite);
			}
			return;
		}
		default:
			who.freezePause = 0;
			break;
		}
		if (temporaryAnimatedSprite != null)
		{
			who.currentLocation.temporarySprites.Add(temporaryAnimatedSprite);
		}
		if (temporaryAnimatedSprite2 != null)
		{
			who.currentLocation.temporarySprites.Add(temporaryAnimatedSprite2);
		}
	}

	public static void eatItem(Farmer who)
	{
	}

	/// <summary>Get whether the player has a buff applied.</summary>
	/// <param name="id">The buff ID, like <see cref="F:StardewValley.Buff.tipsy" />.</param>
	public bool hasBuff(string id)
	{
		return buffs.IsApplied(id);
	}

	/// <summary>Add a buff to the player, or refresh it if it's already applied.</summary>
	/// <param name="id">The buff ID, like <see cref="F:StardewValley.Buff.tipsy" />.</param>
	public void applyBuff(string id)
	{
		buffs.Apply(new Buff(id));
	}

	/// <summary>Add a buff to the player, or refresh it if it's already applied.</summary>
	/// <param name="id">The buff to apply.</param>
	public void applyBuff(Buff buff)
	{
		buffs.Apply(buff);
	}

	/// <summary>Get whether the player has a buff with an ID containing the given string.</summary>
	/// <param name="idSubstring">The substring to match in the buff ID.</param>
	public bool hasBuffWithNameContainingString(string idSubstr)
	{
		return buffs.HasBuffWithNameContaining(idSubstr);
	}

	public bool hasOrWillReceiveMail(string id)
	{
		if (!mailReceived.Contains(id) && !mailForTomorrow.Contains(id) && !mailbox.Contains(id))
		{
			return mailForTomorrow.Contains(id + "%&NL&%");
		}
		return true;
	}

	public static void showHoldingItem(Farmer who, Item item)
	{
		if (!(item is SpecialItem specialItem))
		{
			if (!(item is Slingshot) && !(item is MeleeWeapon) && !(item is Boots))
			{
				if (!(item is Hat))
				{
					if (!(item is Furniture))
					{
						if (!(item is Object) && !(item is Tool))
						{
							if (!(item is Ring))
							{
								if (item == null)
								{
									Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(420, 489, 25, 18), 2500f, 1, 0, who.Position + new Vector2(-20f, -152f), flicker: false, flipped: false)
									{
										motion = new Vector2(0f, -0.1f),
										scale = 4f,
										layerDepth = 1f
									});
								}
								else
								{
									TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(null, default(Microsoft.Xna.Framework.Rectangle), 2500f, 1, 0, who.Position + new Vector2(0f, -124f), flicker: false, flipped: false)
									{
										motion = new Vector2(0f, -0.1f),
										layerDepth = 1f
									};
									temporaryAnimatedSprite.CopyAppearanceFromItemId(item.QualifiedItemId);
									Game1.currentLocation.temporarySprites.Add(temporaryAnimatedSprite);
								}
							}
							else
							{
								TemporaryAnimatedSprite temporaryAnimatedSprite2 = new TemporaryAnimatedSprite(null, default(Microsoft.Xna.Framework.Rectangle), 2500f, 1, 0, who.Position + new Vector2(-4f, -124f), flicker: false, flipped: false)
								{
									motion = new Vector2(0f, -0.1f),
									layerDepth = 1f
								};
								temporaryAnimatedSprite2.CopyAppearanceFromItemId(item.QualifiedItemId);
								Game1.currentLocation.temporarySprites.Add(temporaryAnimatedSprite2);
							}
						}
						else if (item is Object obj && obj.bigCraftable.Value)
						{
							TemporaryAnimatedSprite temporaryAnimatedSprite3 = new TemporaryAnimatedSprite(null, default(Microsoft.Xna.Framework.Rectangle), 2500f, 1, 0, who.Position + new Vector2(0f, -188f), flicker: false, flipped: false)
							{
								motion = new Vector2(0f, -0.1f),
								layerDepth = 1f
							};
							temporaryAnimatedSprite3.CopyAppearanceFromItemId(item.QualifiedItemId);
							Game1.currentLocation.temporarySprites.Add(temporaryAnimatedSprite3);
						}
						else
						{
							TemporaryAnimatedSprite temporaryAnimatedSprite4 = new TemporaryAnimatedSprite(null, default(Microsoft.Xna.Framework.Rectangle), 2500f, 1, 0, who.Position + new Vector2(0f, -124f), flicker: false, flipped: false)
							{
								motion = new Vector2(0f, -0.1f),
								layerDepth = 1f
							};
							temporaryAnimatedSprite4.CopyAppearanceFromItemId(item.QualifiedItemId);
							Game1.currentLocation.temporarySprites.Add(temporaryAnimatedSprite4);
							if (who.IsLocalPlayer && item.QualifiedItemId == "(O)434")
							{
								who.eatHeldObject();
							}
						}
					}
					else
					{
						TemporaryAnimatedSprite temporaryAnimatedSprite5 = new TemporaryAnimatedSprite(null, default(Microsoft.Xna.Framework.Rectangle), 2500f, 1, 0, Vector2.Zero, flicker: false, flipped: false)
						{
							motion = new Vector2(0f, -0.1f),
							layerDepth = 1f
						};
						temporaryAnimatedSprite5.CopyAppearanceFromItemId(item.QualifiedItemId);
						temporaryAnimatedSprite5.initialPosition = (temporaryAnimatedSprite5.position = who.Position + new Vector2(32 - temporaryAnimatedSprite5.sourceRect.Width / 2 * 4, -188f));
						Game1.currentLocation.temporarySprites.Add(temporaryAnimatedSprite5);
					}
				}
				else
				{
					TemporaryAnimatedSprite temporaryAnimatedSprite6 = new TemporaryAnimatedSprite(null, default(Microsoft.Xna.Framework.Rectangle), 2500f, 1, 0, who.Position + new Vector2(-8f, -124f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						motion = new Vector2(0f, -0.1f)
					};
					temporaryAnimatedSprite6.CopyAppearanceFromItemId(item.QualifiedItemId);
					Game1.currentLocation.temporarySprites.Add(temporaryAnimatedSprite6);
				}
			}
			else
			{
				TemporaryAnimatedSprite temporaryAnimatedSprite7 = new TemporaryAnimatedSprite(null, default(Microsoft.Xna.Framework.Rectangle), 2500f, 1, 0, who.Position + new Vector2(0f, -124f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
				{
					motion = new Vector2(0f, -0.1f)
				};
				temporaryAnimatedSprite7.CopyAppearanceFromItemId(item.QualifiedItemId);
				Game1.currentLocation.temporarySprites.Add(temporaryAnimatedSprite7);
			}
		}
		else
		{
			TemporaryAnimatedSprite temporarySpriteForHoldingUp = specialItem.getTemporarySpriteForHoldingUp(who.Position + new Vector2(0f, -124f));
			temporarySpriteForHoldingUp.motion = new Vector2(0f, -0.1f);
			temporarySpriteForHoldingUp.scale = 4f;
			temporarySpriteForHoldingUp.interval = 2500f;
			temporarySpriteForHoldingUp.totalNumberOfLoops = 0;
			temporarySpriteForHoldingUp.animationLength = 1;
			Game1.currentLocation.temporarySprites.Add(temporarySpriteForHoldingUp);
		}
	}

	public void holdUpItemThenMessage(Item item, bool showMessage = true)
	{
		holdUpItemThenMessage(item, item?.Stack ?? 1, showMessage);
	}

	public void holdUpItemThenMessage(Item item, int countAdded, bool showMessage = true)
	{
		completelyStopAnimatingOrDoingAction();
		if (showMessage)
		{
			Game1.MusicDuckTimer = 2000f;
			DelayedAction.playSoundAfterDelay("getNewSpecialItem", 750);
		}
		faceDirection(2);
		freezePause = 4000;
		FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[3]
		{
			new FarmerSprite.AnimationFrame(57, 0),
			new FarmerSprite.AnimationFrame(57, 2500, secondaryArm: false, flip: false, delegate(Farmer who)
			{
				showHoldingItem(who, item);
			}),
			showMessage ? new FarmerSprite.AnimationFrame((short)FarmerSprite.CurrentFrame, 500, secondaryArm: false, flip: false, delegate(Farmer who)
			{
				showReceiveNewItemMessage(who, item, countAdded);
			}, behaviorAtEndOfFrame: true) : new FarmerSprite.AnimationFrame((short)FarmerSprite.CurrentFrame, 500, secondaryArm: false, flip: false)
		});
		mostRecentlyGrabbedItem = item;
		canMove = false;
	}

	public void resetState()
	{
		mount = null;
		ClearBuffs();
		TemporaryItem = null;
		swimming.Value = false;
		bathingClothes.Value = false;
		ignoreCollisions = false;
		resetItemStates();
		fireToolEvent.Clear();
		beginUsingToolEvent.Clear();
		endUsingToolEvent.Clear();
		sickAnimationEvent.Clear();
		passOutEvent.Clear();
		drinkAnimationEvent.Clear();
		eatAnimationEvent.Clear();
	}

	public void resetItemStates()
	{
		for (int i = 0; i < Items.Count; i++)
		{
			Items[i]?.resetState();
		}
	}

	public void clearBackpack()
	{
		for (int i = 0; i < Items.Count; i++)
		{
			Items[i] = null;
		}
	}

	public void resetFriendshipsForNewDay()
	{
		foreach (string key in friendshipData.Keys)
		{
			bool flag = false;
			NPC characterFromName = Game1.getCharacterFromName(key);
			if (characterFromName == null)
			{
				characterFromName = Game1.getCharacterFromName<Child>(key, mustBeVillager: false);
			}
			if (characterFromName != null)
			{
				if (characterFromName != null && characterFromName.datable.Value && !friendshipData[key].IsDating() && !characterFromName.isMarried())
				{
					flag = true;
				}
				if (spouse != null && key == spouse && !hasPlayerTalkedToNPC(key))
				{
					changeFriendship(-20, characterFromName);
				}
				else if (characterFromName != null && friendshipData[key].IsDating() && !hasPlayerTalkedToNPC(key) && friendshipData[key].Points < 2500)
				{
					changeFriendship(-8, characterFromName);
				}
				if (hasPlayerTalkedToNPC(key))
				{
					friendshipData[key].TalkedToToday = false;
				}
				else if ((!flag && friendshipData[key].Points < 2500) || (flag && friendshipData[key].Points < 2000))
				{
					changeFriendship(-2, characterFromName);
				}
			}
		}
		updateFriendshipGifts(Game1.Date);
	}

	public virtual int GetAppliedMagneticRadius()
	{
		return Math.Max(128, MagneticRadius);
	}

	public void updateFriendshipGifts(WorldDate date)
	{
		foreach (string key in friendshipData.Keys)
		{
			if (date.TotalDays != friendshipData[key].LastGiftDate?.TotalDays)
			{
				friendshipData[key].GiftsToday = 0;
			}
			if (date.TotalSundayWeeks != friendshipData[key].LastGiftDate?.TotalSundayWeeks)
			{
				if (friendshipData[key].GiftsThisWeek >= 2)
				{
					changeFriendship(10, Game1.getCharacterFromName(key));
				}
				friendshipData[key].GiftsThisWeek = 0;
			}
		}
	}

	public bool hasPlayerTalkedToNPC(string name)
	{
		if (!friendshipData.TryGetValue(name, out var value) && NPC.CanSocializePerData(name, base.currentLocation))
		{
			value = (friendshipData[name] = new Friendship());
		}
		return value?.TalkedToToday ?? false;
	}

	public void fuelLantern(int units)
	{
		Tool toolFromName = getToolFromName("Lantern");
		if (toolFromName != null)
		{
			((Lantern)toolFromName).fuelLeft = Math.Min(100, ((Lantern)toolFromName).fuelLeft + units);
		}
	}

	public bool IsEquippedItem(Item item)
	{
		if (item != null)
		{
			foreach (Item equippedItem in GetEquippedItems())
			{
				if (equippedItem == item)
				{
					return true;
				}
			}
		}
		return false;
	}

	public IEnumerable<Item> GetEquippedItems()
	{
		return new Item[7] { CurrentTool, hat.Value, shirtItem.Value, pantsItem.Value, boots.Value, leftRing.Value, rightRing.Value }.Where((Item item) => item != null);
	}

	public override bool collideWith(Object o)
	{
		base.collideWith(o);
		if (isRidingHorse() && o is Fence)
		{
			mount.squeezeForGate();
			switch (FacingDirection)
			{
			case 3:
				if (o.tileLocation.X > base.Tile.X)
				{
					return false;
				}
				break;
			case 1:
				if (o.tileLocation.X < base.Tile.X)
				{
					return false;
				}
				break;
			}
		}
		return true;
	}

	public void changeIntoSwimsuit()
	{
		bathingClothes.Value = true;
		Halt();
		setRunning(isRunning: false);
		canOnlyWalk = true;
	}

	public void changeOutOfSwimSuit()
	{
		bathingClothes.Value = false;
		canOnlyWalk = false;
		Halt();
		FarmerSprite.StopAnimation();
		if (Game1.options.autoRun)
		{
			setRunning(isRunning: true);
		}
	}

	public void showFrame(int frame, bool flip = false)
	{
		FarmerSprite.AnimationFrame[] currentAnimation = new FarmerSprite.AnimationFrame[1]
		{
			new FarmerSprite.AnimationFrame(Convert.ToInt32(frame), 100, secondaryArm: false, flip)
		};
		FarmerSprite.setCurrentAnimation(currentAnimation);
		FarmerSprite.loop = true;
		FarmerSprite.PauseForSingleAnimation = true;
		Sprite.currentFrame = Convert.ToInt32(frame);
	}

	public void stopShowingFrame()
	{
		FarmerSprite.loop = false;
		FarmerSprite.PauseForSingleAnimation = false;
		completelyStopAnimatingOrDoingAction();
	}

	/// <summary>Add an item to the player's inventory if there's room for it.</summary>
	/// <param name="item">The item to add.</param>
	/// <returns>If the item was fully added to the inventory, returns <c>null</c>. Else returns the input item with its stack reduced to the amount that couldn't be added.</returns>
	public Item addItemToInventory(Item item)
	{
		return addItemToInventory(item, null);
	}

	/// <summary>Add an item to the player's inventory if there's room for it.</summary>
	/// <param name="item">The item to add.</param>
	/// <param name="affected_items_list">A list to update with the inventory item stacks it was merged into, or <c>null</c> to ignore it.</param>
	/// <returns>If the item was fully added to the inventory, returns <c>null</c>. Else returns the input item with its stack reduced to the amount that couldn't be added.</returns>
	public Item addItemToInventory(Item item, List<Item> affected_items_list)
	{
		if (item == null)
		{
			return null;
		}
		GetItemReceiveBehavior(item, out var needsInventorySpace, out var _);
		if (!needsInventorySpace)
		{
			OnItemReceived(item, item.Stack, null);
			return null;
		}
		int stack = item.Stack;
		int num = stack;
		foreach (Item item2 in Items)
		{
			if (!item.canStackWith(item2))
			{
				continue;
			}
			int stack2 = item.Stack;
			num = item2.addToStack(item);
			int num2 = stack2 - num;
			if (num2 > 0)
			{
				item.Stack = num;
				OnItemReceived(item, num2, item2, hideHudNotification: true);
				affected_items_list?.Add(item2);
				if (num < 1)
				{
					break;
				}
			}
		}
		if (num > 0)
		{
			for (int i = 0; i < maxItems.Value && i < Items.Count; i++)
			{
				if (Items[i] == null)
				{
					item.onDetachedFromParent();
					Items[i] = item;
					num = 0;
					OnItemReceived(item, item.Stack, null, hideHudNotification: true);
					affected_items_list?.Add(Items[i]);
					break;
				}
			}
		}
		if (stack > num)
		{
			ShowItemReceivedHudMessageIfNeeded(item, stack - num);
		}
		if (num <= 0)
		{
			return null;
		}
		return item;
	}

	/// <summary>Add an item to the player's inventory at a specific index position. If there's already an item at that position, the stacks are merged (if possible) else they're swapped.</summary>
	/// <param name="item">The item to add.</param>
	/// <param name="position">The index position within the list at which to add the item.</param>
	/// <returns>If the item was fully added to the inventory, returns <c>null</c>. If it replaced an item stack previously at that position, returns the replaced item stack. Else returns the input item with its stack reduced to the amount that couldn't be added.</returns>
	public Item addItemToInventory(Item item, int position)
	{
		if (item == null)
		{
			return null;
		}
		GetItemReceiveBehavior(item, out var needsInventorySpace, out var _);
		if (!needsInventorySpace)
		{
			OnItemReceived(item, item.Stack, null);
			return null;
		}
		if (position >= 0 && position < Items.Count)
		{
			if (Items[position] == null)
			{
				Items[position] = item;
				OnItemReceived(item, item.Stack, null);
				return null;
			}
			if (!Items[position].canStackWith(item))
			{
				Item result = Items[position];
				Items[position] = item;
				OnItemReceived(item, item.Stack, null);
				return result;
			}
			int stack = item.Stack;
			int num = Items[position].addToStack(item);
			int num2 = stack - num;
			if (num2 > 0)
			{
				item.Stack = num;
				OnItemReceived(item, num2, Items[position]);
				if (num <= 0)
				{
					return null;
				}
				return item;
			}
		}
		return item;
	}

	/// <summary>Add an item to the player's inventory if there's room for it.</summary>
	/// <param name="item">The item to add.</param>
	/// <param name="makeActiveObject">Legacy option which may behave in unexpected ways; shouldn't be used by most code.</param>
	/// <returns>Returns whether the item was at least partially added to the inventory. The number of items added will be deducted from the <paramref name="item" />'s <see cref="P:StardewValley.Item.Stack" />.</returns>
	public bool addItemToInventoryBool(Item item, bool makeActiveObject = false)
	{
		if (item == null)
		{
			return false;
		}
		if (IsLocalPlayer)
		{
			Item item2 = null;
			GetItemReceiveBehavior(item, out var needsInventorySpace, out var _);
			if (needsInventorySpace)
			{
				item2 = addItemToInventory(item);
			}
			else
			{
				OnItemReceived(item, item.Stack, null);
			}
			bool flag = item2?.Stack != item.Stack || item is SpecialItem;
			if (makeActiveObject && flag && !(item is SpecialItem) && item2 != null && item.Stack <= 1)
			{
				int indexOfInventoryItem = getIndexOfInventoryItem(item);
				if (indexOfInventoryItem > -1)
				{
					Item value = Items[currentToolIndex.Value];
					Items[currentToolIndex.Value] = Items[indexOfInventoryItem];
					Items[indexOfInventoryItem] = value;
				}
			}
			return flag;
		}
		return false;
	}

	/// <summary>Add an item to the player's inventory if there's room for it, then show an animation of the player holding up the item above their head. If the item can't be fully added to the player's inventory, show (or queue) an item-grab menu to let the player collect the remainder.</summary>
	/// <param name="item">The item to add.</param>
	/// <param name="itemSelectedCallback">The callback to invoke when the item is added to the player's inventory.</param>
	/// <param name="forceQueue">For any remainder that can't be added to the inventory directly, whether to add the item-grab menu to <see cref="F:StardewValley.Game1.nextClickableMenu" /> even if there's no active menu currently open.</param>
	public void addItemByMenuIfNecessaryElseHoldUp(Item item, ItemGrabMenu.behaviorOnItemSelect itemSelectedCallback = null, bool forceQueue = false)
	{
		int stack = item.Stack;
		mostRecentlyGrabbedItem = item;
		addItemsByMenuIfNecessary(new List<Item> { item }, itemSelectedCallback, forceQueue);
		if (Game1.activeClickableMenu == null && item?.QualifiedItemId != "(O)434")
		{
			holdUpItemThenMessage(item, stack);
		}
	}

	/// <summary>Add an item to the player's inventory if there's room for it. If the item can't be fully added to the player's inventory, show (or queue) an item-grab menu to let the player collect the remainder.</summary>
	/// <param name="item">The item to add.</param>
	/// <param name="itemSelectedCallback">The callback to invoke when the item is added to the player's inventory.</param>
	/// <param name="forceQueue">For any remainder that can't be added to the inventory directly, whether to add the item-grab menu to <see cref="F:StardewValley.Game1.nextClickableMenu" /> even if there's no active menu currently open.</param>
	public void addItemByMenuIfNecessary(Item item, ItemGrabMenu.behaviorOnItemSelect itemSelectedCallback = null, bool forceQueue = false)
	{
		addItemsByMenuIfNecessary(new List<Item> { item }, itemSelectedCallback, forceQueue);
	}

	/// <summary>Add items to the player's inventory if there's room for them. If the items can't be fully added to the player's inventory, show (or queue) an item-grab menu to let the player collect the remainder.</summary>
	/// <param name="itemsToAdd">The items to add.</param>
	/// <param name="itemSelectedCallback">The callback to invoke when an item is added to the player's inventory.</param>
	/// <param name="forceQueue">For any items that can't be added to the inventory directly, whether to add the item-grab menu to <see cref="F:StardewValley.Game1.nextClickableMenu" /> even if there's no active menu currently open.</param>
	public void addItemsByMenuIfNecessary(List<Item> itemsToAdd, ItemGrabMenu.behaviorOnItemSelect itemSelectedCallback = null, bool forceQueue = false)
	{
		if (itemsToAdd == null || !IsLocalPlayer)
		{
			return;
		}
		if (itemsToAdd.Count > 0 && itemsToAdd[0]?.QualifiedItemId == "(O)434")
		{
			if (Game1.activeClickableMenu == null && !forceQueue)
			{
				eatObject(itemsToAdd[0] as Object, overrideFullness: true);
			}
			else
			{
				Game1.nextClickableMenu.Add(ItemGrabMenu.CreateOverflowMenu(itemsToAdd));
			}
			return;
		}
		for (int num = itemsToAdd.Count - 1; num >= 0; num--)
		{
			if (addItemToInventoryBool(itemsToAdd[num]))
			{
				itemSelectedCallback?.Invoke(itemsToAdd[num], this);
				itemsToAdd.Remove(itemsToAdd[num]);
			}
		}
		if (itemsToAdd.Count > 0 && (forceQueue || Game1.activeClickableMenu != null))
		{
			for (int i = 0; i < Game1.nextClickableMenu.Count; i++)
			{
				if (Game1.nextClickableMenu[i] is ItemGrabMenu { source: 4 } itemGrabMenu)
				{
					IList<Item> actualInventory = itemGrabMenu.ItemsToGrabMenu.actualInventory;
					int capacity = itemGrabMenu.ItemsToGrabMenu.capacity;
					bool flag = false;
					for (int j = 0; j < itemsToAdd.Count; j++)
					{
						Item item = itemsToAdd[j];
						int stack = item.Stack;
						item = (itemsToAdd[j] = Utility.addItemToThisInventoryList(item, actualInventory, capacity));
						if (stack != item?.Stack)
						{
							flag = true;
							if (item == null)
							{
								itemsToAdd.RemoveAt(j);
								j--;
							}
						}
					}
					if (flag)
					{
						Game1.nextClickableMenu[i] = ItemGrabMenu.CreateOverflowMenu(actualInventory);
					}
				}
				if (itemsToAdd.Count == 0)
				{
					break;
				}
			}
		}
		if (itemsToAdd.Count > 0)
		{
			ItemGrabMenu itemGrabMenu2 = ItemGrabMenu.CreateOverflowMenu(itemsToAdd);
			if (forceQueue || Game1.activeClickableMenu != null)
			{
				Game1.nextClickableMenu.Add(itemGrabMenu2);
			}
			else
			{
				Game1.activeClickableMenu = itemGrabMenu2;
			}
		}
	}

	public virtual void BeginSitting(ISittable furniture)
	{
		if (furniture == null || bathingClothes.Value || swimming.Value || isRidingHorse() || !CanMove || UsingTool || base.IsEmoting)
		{
			return;
		}
		Vector2? vector = furniture.AddSittingFarmer(this);
		if (!vector.HasValue)
		{
			return;
		}
		playNearbySoundAll("woodyStep");
		Halt();
		synchronizedJump(4f);
		FarmerSprite.StopAnimation();
		sittingFurniture = furniture;
		mapChairSitPosition.Value = new Vector2(-1f, -1f);
		if (sittingFurniture is MapSeat)
		{
			Vector2? sittingPosition = sittingFurniture.GetSittingPosition(this, ignore_offsets: true);
			if (sittingPosition.HasValue)
			{
				mapChairSitPosition.Value = sittingPosition.Value;
			}
		}
		isSitting.Value = true;
		LerpPosition(base.Position, new Vector2(vector.Value.X * 64f, vector.Value.Y * 64f), 0.15f);
		freezePause += 100;
	}

	public virtual void LerpPosition(Vector2 start_position, Vector2 end_position, float duration)
	{
		freezePause = (int)(duration * 1000f);
		lerpStartPosition = start_position;
		lerpEndPosition = end_position;
		lerpPosition = 0f;
		lerpDuration = duration;
	}

	public virtual void StopSitting(bool animate = true)
	{
		if (sittingFurniture == null)
		{
			return;
		}
		ISittable sittable = sittingFurniture;
		if (!animate)
		{
			mapChairSitPosition.Value = new Vector2(-1f, -1f);
			sittable.RemoveSittingFarmer(this);
		}
		bool flag = false;
		bool flag2 = false;
		Vector2 vector = base.Position;
		if (sittable.IsSeatHere(base.currentLocation))
		{
			flag = true;
			List<Vector2> list = new List<Vector2>();
			Vector2 vector2 = new Vector2(sittable.GetSeatBounds().Left, sittable.GetSeatBounds().Top);
			if (sittable.IsSittingHere(this))
			{
				vector2 = sittable.GetSittingPosition(this, ignore_offsets: true).Value;
			}
			if (sittable.GetSittingDirection() == 2)
			{
				list.Add(vector2 + new Vector2(0f, 1f));
				SortSeatExitPositions(list, vector2 + new Vector2(1f, 0f), vector2 + new Vector2(-1f, 0f), vector2 + new Vector2(0f, -1f));
			}
			else if (sittable.GetSittingDirection() == 1)
			{
				list.Add(vector2 + new Vector2(1f, 0f));
				SortSeatExitPositions(list, vector2 + new Vector2(0f, -1f), vector2 + new Vector2(0f, 1f), vector2 + new Vector2(-1f, 0f));
			}
			else if (sittable.GetSittingDirection() == 3)
			{
				list.Add(vector2 + new Vector2(-1f, 0f));
				SortSeatExitPositions(list, vector2 + new Vector2(0f, 1f), vector2 + new Vector2(0f, -1f), vector2 + new Vector2(1f, 0f));
			}
			else if (sittable.GetSittingDirection() == 0)
			{
				list.Add(vector2 + new Vector2(0f, -1f));
				SortSeatExitPositions(list, vector2 + new Vector2(-1f, 0f), vector2 + new Vector2(1f, 0f), vector2 + new Vector2(0f, 1f));
			}
			Microsoft.Xna.Framework.Rectangle seatBounds = sittable.GetSeatBounds();
			seatBounds.Inflate(1, 1);
			foreach (Vector2 item in Utility.getBorderOfThisRectangle(seatBounds))
			{
				list.Add(item);
			}
			foreach (Vector2 item2 in list)
			{
				setTileLocation(item2);
				Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
				base.Position = vector;
				Object objectAtTile = base.currentLocation.getObjectAtTile((int)item2.X, (int)item2.Y, ignorePassables: true);
				if (!base.currentLocation.isCollidingPosition(boundingBox, Game1.viewport, isFarmer: true, 0, glider: false, this) && (objectAtTile == null || objectAtTile.isPassable()))
				{
					if (animate)
					{
						playNearbySoundAll("coin");
						synchronizedJump(4f);
						LerpPosition(vector2 * 64f, item2 * 64f, 0.15f);
					}
					flag2 = true;
					break;
				}
			}
		}
		if (!flag2)
		{
			if (animate)
			{
				playNearbySoundAll("coin");
			}
			base.Position = vector;
			if (flag)
			{
				Microsoft.Xna.Framework.Rectangle seatBounds2 = sittable.GetSeatBounds();
				seatBounds2.X *= 64;
				seatBounds2.Y *= 64;
				seatBounds2.Width *= 64;
				seatBounds2.Height *= 64;
				temporaryPassableTiles.Add(seatBounds2);
			}
		}
		if (!animate)
		{
			sittingFurniture = null;
			isSitting.Value = false;
			Halt();
			showNotCarrying();
		}
		else
		{
			isStopSitting = true;
		}
		Game1.haltAfterCheck = false;
		yOffset = 0f;
		xOffset = 0f;
	}

	public void SortSeatExitPositions(List<Vector2> list, Vector2 a, Vector2 b, Vector2 c)
	{
		Vector2 mouse_pos = Utility.PointToVector2(Game1.getMousePosition(ui_scale: false)) + new Vector2(Game1.viewport.X, Game1.viewport.Y);
		Vector2 zero = Vector2.Zero;
		if (Game1.isOneOfTheseKeysDown(Game1.input.GetKeyboardState(), Game1.options.moveUpButton) || (Game1.options.gamepadControls && ((double)Game1.input.GetGamePadState().ThumbSticks.Left.Y > 0.25 || Game1.input.GetGamePadState().IsButtonDown(Buttons.DPadUp))))
		{
			zero.Y -= 1f;
		}
		else if (Game1.isOneOfTheseKeysDown(Game1.input.GetKeyboardState(), Game1.options.moveDownButton) || (Game1.options.gamepadControls && ((double)Game1.input.GetGamePadState().ThumbSticks.Left.Y < -0.25 || Game1.input.GetGamePadState().IsButtonDown(Buttons.DPadDown))))
		{
			zero.Y += 1f;
		}
		if (Game1.isOneOfTheseKeysDown(Game1.input.GetKeyboardState(), Game1.options.moveLeftButton) || (Game1.options.gamepadControls && ((double)Game1.input.GetGamePadState().ThumbSticks.Left.X < -0.25 || Game1.input.GetGamePadState().IsButtonDown(Buttons.DPadLeft))))
		{
			zero.X -= 1f;
		}
		else if (Game1.isOneOfTheseKeysDown(Game1.input.GetKeyboardState(), Game1.options.moveRightButton) || (Game1.options.gamepadControls && ((double)Game1.input.GetGamePadState().ThumbSticks.Left.X > 0.25 || Game1.input.GetGamePadState().IsButtonDown(Buttons.DPadRight))))
		{
			zero.X += 1f;
		}
		if (zero != Vector2.Zero)
		{
			mouse_pos = getStandingPosition() + zero * 64f;
		}
		mouse_pos /= 64f;
		List<Vector2> list2 = new List<Vector2> { a, b, c };
		list2.Sort((Vector2 d, Vector2 e) => (d + new Vector2(0.5f, 0.5f) - mouse_pos).Length().CompareTo((e + new Vector2(0.5f, 0.5f) - mouse_pos).Length()));
		list.AddRange(list2);
	}

	public virtual bool IsSitting()
	{
		return isSitting.Value;
	}

	public bool isInventoryFull()
	{
		for (int i = 0; i < maxItems.Value; i++)
		{
			if (Items.Count > i && Items[i] == null)
			{
				return false;
			}
		}
		return true;
	}

	public bool couldInventoryAcceptThisItem(Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (item.IsRecipe)
		{
			return true;
		}
		switch (item.QualifiedItemId)
		{
		case "(O)73":
		case "(O)930":
		case "(O)102":
		case "(O)858":
		case "(O)GoldCoin":
			return true;
		default:
		{
			for (int i = 0; i < maxItems.Value; i++)
			{
				if (Items.Count > i && (Items[i] == null || (item is Object && Items[i] is Object && Items[i].Stack + item.Stack <= Items[i].maximumStackSize() && (Items[i] as Object).canStackWith(item))))
				{
					return true;
				}
			}
			if (IsLocalPlayer && isInventoryFull() && Game1.hudMessages.Count == 0)
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
			}
			return false;
		}
		}
	}

	public bool couldInventoryAcceptThisItem(string id, int stack, int quality = 0)
	{
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(id);
		switch (dataOrErrorItem.QualifiedItemId)
		{
		case "(O)73":
		case "(O)930":
		case "(O)102":
		case "(O)858":
		case "(O)GoldCoin":
			return true;
		default:
		{
			for (int i = 0; i < maxItems.Value; i++)
			{
				if (Items.Count > i && (Items[i] == null || (Items[i].Stack + stack <= Items[i].maximumStackSize() && Items[i].QualifiedItemId == dataOrErrorItem.QualifiedItemId && Items[i].quality.Value == quality)))
				{
					return true;
				}
			}
			if (IsLocalPlayer && isInventoryFull() && Game1.hudMessages.Count == 0)
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
			}
			return false;
		}
		}
	}

	public NPC getSpouse()
	{
		if (isMarriedOrRoommates() && spouse != null)
		{
			return Game1.getCharacterFromName(spouse);
		}
		return null;
	}

	public int freeSpotsInInventory()
	{
		int num = Items.CountItemStacks();
		if (num >= maxItems.Value)
		{
			return 0;
		}
		return maxItems.Value - num;
	}

	/// <summary>Get the behavior that applies when this item is received.</summary>
	/// <param name="item">The item being received.</param>
	/// <param name="needsInventorySpace">Whether this item takes space in the player inventory. This is false for special items like Qi Gems.</param>
	/// <param name="showNotification">Whether to show a HUD notification when the item is received.</param>
	public void GetItemReceiveBehavior(Item item, out bool needsInventorySpace, out bool showNotification)
	{
		if (item is SpecialItem)
		{
			needsInventorySpace = false;
			showNotification = false;
			return;
		}
		switch (item.QualifiedItemId)
		{
		case "(O)73":
		case "(O)102":
		case "(O)858":
			needsInventorySpace = false;
			showNotification = true;
			break;
		case "(O)GoldCoin":
		case "(O)930":
			needsInventorySpace = false;
			showNotification = false;
			break;
		default:
			needsInventorySpace = true;
			showNotification = true;
			break;
		}
		if (item.IsRecipe)
		{
			needsInventorySpace = false;
			showNotification = true;
		}
	}

	/// <summary>Handle an item being added to the current player's inventory.</summary>
	/// <param name="item">The item that was added. If <see cref="!:mergedIntoStack" /> is set, this is the original item rather than the one actually in the player's inventory.</param>
	/// <param name="countAdded">The number of the item that was added. This may differ from <paramref name="item" />'s stack size if it was only partly added or split across multiple stacks.</param>
	/// <param name="mergedIntoStack">The previous item stack it was merged into, if applicable.</param>
	/// <param name="hideHudNotification">Hide the 'item received' HUD notification even if it would normally be shown. This is used when merging the item into multiple stacks, so the HUD notification is shown once.</param>
	public void OnItemReceived(Item item, int countAdded, Item mergedIntoStack, bool hideHudNotification = false)
	{
		if (!IsLocalPlayer)
		{
			return;
		}
		(item as Object)?.reloadSprite();
		if (item.HasBeenInInventory)
		{
			return;
		}
		Item actualItem = mergedIntoStack ?? item;
		if (!hideHudNotification)
		{
			GetItemReceiveBehavior(actualItem, out var _, out var showNotification);
			if (showNotification)
			{
				ShowItemReceivedHudMessage(actualItem, countAdded);
			}
		}
		if (freezePause <= 0)
		{
			mostRecentlyGrabbedItem = actualItem;
		}
		if (item.SetFlagOnPickup != null)
		{
			if (!hasOrWillReceiveMail(item.SetFlagOnPickup))
			{
				Game1.addMail(item.SetFlagOnPickup, noLetter: true);
			}
			actualItem.SetFlagOnPickup = null;
		}
		(actualItem as SpecialItem)?.actionWhenReceived(this);
		if (actualItem is Object { specialItem: not false } obj)
		{
			string item2 = (obj.IsRecipe ? ("-" + obj.ItemId) : obj.ItemId);
			if (obj.bigCraftable.Value || obj is Furniture)
			{
				if (!specialBigCraftables.Contains(item2))
				{
					specialBigCraftables.Add(item2);
				}
			}
			else if (!specialItems.Contains(item2))
			{
				specialItems.Add(item2);
			}
		}
		if (item.IsRecipe)
		{
			item.LearnRecipe();
			Game1.playSound("newRecipe");
			return;
		}
		int stack = actualItem.Stack;
		try
		{
			actualItem.Stack = countAdded;
			NotifyQuests((Quest quest) => quest.OnItemReceived(actualItem, countAdded));
			if (team.specialOrders != null)
			{
				foreach (SpecialOrder specialOrder in team.specialOrders)
				{
					specialOrder.onItemCollected?.Invoke(this, actualItem);
				}
			}
		}
		finally
		{
			actualItem.Stack = stack;
		}
		if (actualItem.HasTypeObject() && actualItem is Object obj2)
		{
			if (obj2.Category == -2 || obj2.Type == "Minerals")
			{
				foundMineral(obj2.ItemId);
			}
			else if (obj2.Type == "Arch")
			{
				foundArtifact(obj2.ItemId, 1);
			}
		}
		stats.checkForHeldItemAchievements();
		switch (actualItem.QualifiedItemId)
		{
		case "(O)GoldCoin":
		{
			Game1.playSound("moneyDial");
			int num2 = 250 * countAdded;
			if (Game1.IsSpring && Game1.dayOfMonth == 17 && base.currentLocation is Forest && base.Tile.Y > 90f)
			{
				num2 = 25;
			}
			Money += num2;
			removeItemFromInventory(item);
			Game1.dayTimeMoneyBox.gotGoldCoin(num2);
			break;
		}
		case "(O)73":
			foundWalnut(countAdded);
			removeItemFromInventory(actualItem);
			break;
		case "(O)858":
			QiGems += countAdded;
			Game1.playSound("qi_shop_purchase");
			base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("Maps\\springobjects", Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 858, 16, 16), 100f, 1, 8, new Vector2(0f, -96f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
			{
				motion = new Vector2(0f, -6f),
				acceleration = new Vector2(0f, 0.2f),
				stopAcceleratingWhenVelocityIsZero = true,
				attachedCharacter = this,
				positionFollowsAttachedCharacter = true
			});
			removeItemFromInventory(actualItem);
			break;
		case "(O)930":
		{
			int num = 10 * countAdded;
			health = Math.Min(maxHealth, health + num);
			base.currentLocation.debris.Add(new Debris(num, getStandingPosition(), Color.Lime, 1f, this));
			Game1.playSound("healSound");
			removeItemFromInventory(actualItem);
			break;
		}
		case "(O)875":
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("ectoplasmDrop") && team.SpecialOrderActive("Wizard"))
			{
				Game1.addMailForTomorrow("ectoplasmDrop", noLetter: true, sendToEveryone: true);
			}
			break;
		case "(O)876":
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("prismaticJellyDrop") && team.SpecialOrderActive("Wizard2"))
			{
				Game1.addMailForTomorrow("prismaticJellyDrop", noLetter: true, sendToEveryone: true);
			}
			break;
		case "(O)897":
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("gotMissingStocklist"))
			{
				Game1.addMailForTomorrow("gotMissingStocklist", noLetter: true, sendToEveryone: true);
			}
			break;
		case "(BC)256":
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("gotFirstJunimoChest"))
			{
				Game1.addMailForTomorrow("gotFirstJunimoChest", noLetter: true, sendToEveryone: true);
			}
			break;
		case "(O)535":
			Game1.PerformActionWhenPlayerFree(delegate
			{
				if (!hasOrWillReceiveMail("geodeFound"))
				{
					mailReceived.Add("geodeFound");
					holdUpItemThenMessage(actualItem);
				}
			});
			break;
		case "(O)428":
			if (!hasOrWillReceiveMail("clothFound"))
			{
				Game1.addMailForTomorrow("clothFound", noLetter: true);
			}
			break;
		case "(O)102":
			Game1.PerformActionWhenPlayerFree(delegate
			{
				foundArtifact(actualItem.ItemId, 1);
			});
			removeItemFromInventory(actualItem);
			stats.NotesFound++;
			break;
		case "(O)390":
			stats.StoneGathered++;
			if (stats.StoneGathered >= 100 && !hasOrWillReceiveMail("robinWell"))
			{
				Game1.addMailForTomorrow("robinWell");
			}
			break;
		case "(O)384":
			stats.GoldFound += (uint)countAdded;
			break;
		case "(O)380":
			stats.IronFound += (uint)countAdded;
			break;
		case "(O)386":
			stats.IridiumFound += (uint)countAdded;
			break;
		case "(O)378":
			stats.CopperFound += (uint)countAdded;
			if (!hasOrWillReceiveMail("copperFound"))
			{
				Game1.addMailForTomorrow("copperFound", noLetter: true);
			}
			break;
		case "(O)74":
			stats.PrismaticShardsFound++;
			break;
		case "(O)72":
			stats.DiamondsFound++;
			break;
		case "(BC)248":
			Game1.netWorldState.Value.MiniShippingBinsObtained++;
			break;
		}
		actualItem.HasBeenInInventory = true;
	}

	/// <summary>Show the item-received HUD message for an item if applicable for the item type.</summary>
	/// <param name="item">The item that was added.</param>
	/// <param name="countAdded">The number of the item that was added. This may differ from <paramref name="item" />'s stack size if it was only partly added or split across multiple stacks.</param>
	public void ShowItemReceivedHudMessageIfNeeded(Item item, int countAdded)
	{
		GetItemReceiveBehavior(item, out var _, out var showNotification);
		if (showNotification)
		{
			ShowItemReceivedHudMessage(item, countAdded);
		}
	}

	/// <summary>Show the item-received HUD message for an item.</summary>
	/// <param name="item">The item that was added.</param>
	/// <param name="countAdded">The number of the item that was added. This may differ from <paramref name="item" />'s stack size if it was only partly added or split across multiple stacks.</param>
	public void ShowItemReceivedHudMessage(Item item, int countAdded)
	{
		if (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is ItemGrabMenu))
		{
			Game1.addHUDMessage(HUDMessage.ForItemGained(item, countAdded));
		}
	}

	public int getIndexOfInventoryItem(Item item)
	{
		for (int i = 0; i < Items.Count; i++)
		{
			if (Items[i] == item || (Items[i] != null && item != null && item.canStackWith(Items[i])))
			{
				return i;
			}
		}
		return -1;
	}

	public void reduceActiveItemByOne()
	{
		if (CurrentItem != null && --CurrentItem.Stack <= 0)
		{
			removeItemFromInventory(CurrentItem);
			showNotCarrying();
		}
	}

	public void ReequipEnchantments()
	{
		Tool currentTool = CurrentTool;
		if (currentTool == null)
		{
			return;
		}
		foreach (BaseEnchantment enchantment in currentTool.enchantments)
		{
			enchantment.OnEquip(this);
		}
	}

	public void removeItemFromInventory(Item which)
	{
		if (which != null)
		{
			int num = Items.IndexOf(which);
			if (num >= 0 && num < Items.Count)
			{
				Items[num].actionWhenStopBeingHeld(this);
				Items[num] = null;
			}
		}
	}

	/// <summary>Get whether the player is married to or roommates with an NPC or player.</summary>
	public bool isMarriedOrRoommates()
	{
		if (team.IsMarried(UniqueMultiplayerID))
		{
			return true;
		}
		if (spouse != null && friendshipData.TryGetValue(spouse, out var value))
		{
			return value.IsMarried();
		}
		return false;
	}

	public bool isEngaged()
	{
		if (team.IsEngaged(UniqueMultiplayerID))
		{
			return true;
		}
		if (spouse != null && friendshipData.TryGetValue(spouse, out var value))
		{
			return value.IsEngaged();
		}
		return false;
	}

	public void removeFirstOfThisItemFromInventory(string itemId, int count = 1)
	{
		itemId = ItemRegistry.QualifyItemId(itemId);
		if (itemId == null)
		{
			return;
		}
		int num = count;
		if (ActiveObject?.QualifiedItemId == itemId)
		{
			int num2 = Math.Min(num, ActiveObject.Stack);
			num -= num2;
			if (ActiveObject.ConsumeStack(num2) == null)
			{
				ActiveObject = null;
				showNotCarrying();
			}
		}
		if (num > 0)
		{
			Items.ReduceId(itemId, num);
		}
	}

	public void rotateShirt(int direction, List<string> validIds = null)
	{
		string value = shirt.Value;
		if (validIds == null)
		{
			validIds = new List<string>(Game1.shirtData.Keys);
		}
		int num = validIds.IndexOf(value);
		if (num == -1)
		{
			value = validIds.FirstOrDefault();
			if (value != null)
			{
				changeShirt(value);
			}
		}
		else
		{
			num = Utility.WrapIndex(num + direction, validIds.Count);
			value = validIds[num];
			changeShirt(value);
		}
	}

	public void changeShirt(string itemId)
	{
		shirt.Set(itemId);
		FarmerRenderer.changeShirt(itemId);
	}

	public void rotatePantStyle(int direction, List<string> validIds = null)
	{
		string value = pants.Value;
		if (validIds == null)
		{
			validIds = new List<string>(Game1.pantsData.Keys);
		}
		int num = validIds.IndexOf(value);
		if (num == -1)
		{
			value = validIds.FirstOrDefault();
			if (value != null)
			{
				changePantStyle(value);
			}
		}
		else
		{
			num = Utility.WrapIndex(num + direction, validIds.Count);
			value = validIds[num];
			changePantStyle(value);
		}
	}

	public void changePantStyle(string itemId)
	{
		pants.Set(itemId);
		FarmerRenderer.changePants(itemId);
	}

	public void ConvertClothingOverrideToClothesItems()
	{
		if (IsOverridingPants(out var id, out var color))
		{
			if (ItemRegistry.Exists("(P)" + id))
			{
				Clothing clothing = new Clothing(id);
				clothing.clothesColor.Value = color ?? Color.White;
				Equip(clothing, pantsItem);
			}
			pants.Value = "-1";
		}
		if (IsOverridingShirt(out var id2))
		{
			if (int.TryParse(id2, out var result) && result < 1000)
			{
				id2 = (result + 1000).ToString();
			}
			if (ItemRegistry.Exists("(S)" + id2))
			{
				Clothing newItem = new Clothing(id2);
				Equip(newItem, shirtItem);
			}
			shirt.Value = "-1";
		}
	}

	public static Dictionary<int, string> GetHairStyleMetadataFile()
	{
		if (hairStyleMetadataFile == null)
		{
			hairStyleMetadataFile = DataLoader.HairData(Game1.content);
		}
		return hairStyleMetadataFile;
	}

	public static HairStyleMetadata GetHairStyleMetadata(int hair_index)
	{
		GetHairStyleMetadataFile();
		if (Farmer.hairStyleMetadata.TryGetValue(hair_index, out var value))
		{
			return value;
		}
		try
		{
			if (hairStyleMetadataFile.TryGetValue(hair_index, out var value2))
			{
				string[] array = value2.Split('/');
				HairStyleMetadata hairStyleMetadata = new HairStyleMetadata();
				hairStyleMetadata.texture = Game1.content.Load<Texture2D>("Characters\\Farmer\\" + array[0]);
				hairStyleMetadata.tileX = int.Parse(array[1]);
				hairStyleMetadata.tileY = int.Parse(array[2]);
				if (array.Length > 3 && array[3].EqualsIgnoreCase("true"))
				{
					hairStyleMetadata.usesUniqueLeftSprite = true;
				}
				else
				{
					hairStyleMetadata.usesUniqueLeftSprite = false;
				}
				if (array.Length > 4)
				{
					hairStyleMetadata.coveredIndex = int.Parse(array[4]);
				}
				if (array.Length > 5 && array[5].EqualsIgnoreCase("true"))
				{
					hairStyleMetadata.isBaldStyle = true;
				}
				else
				{
					hairStyleMetadata.isBaldStyle = false;
				}
				value = hairStyleMetadata;
			}
		}
		catch (Exception)
		{
		}
		Farmer.hairStyleMetadata[hair_index] = value;
		return value;
	}

	public static List<int> GetAllHairstyleIndices()
	{
		if (allHairStyleIndices != null)
		{
			return allHairStyleIndices;
		}
		GetHairStyleMetadataFile();
		allHairStyleIndices = new List<int>();
		int num = FarmerRenderer.hairStylesTexture.Height / 96 * 8;
		for (int i = 0; i < num; i++)
		{
			allHairStyleIndices.Add(i);
		}
		foreach (int key in hairStyleMetadataFile.Keys)
		{
			if (key >= 0 && !allHairStyleIndices.Contains(key))
			{
				allHairStyleIndices.Add(key);
			}
		}
		allHairStyleIndices.Sort();
		return allHairStyleIndices;
	}

	public static int GetLastHairStyle()
	{
		return GetAllHairstyleIndices()[GetAllHairstyleIndices().Count - 1];
	}

	public void changeHairStyle(int whichHair)
	{
		bool num = isBald();
		if (GetHairStyleMetadata(whichHair) != null)
		{
			hair.Set(whichHair);
		}
		else
		{
			if (whichHair < 0)
			{
				whichHair = GetLastHairStyle();
			}
			else if (whichHair > GetLastHairStyle())
			{
				whichHair = 0;
			}
			hair.Set(whichHair);
		}
		if (IsBaldHairStyle(whichHair))
		{
			FarmerRenderer.textureName.Set(getTexture());
		}
		if (num && !isBald())
		{
			FarmerRenderer.textureName.Set(getTexture());
		}
	}

	public virtual bool IsBaldHairStyle(int style)
	{
		if (GetHairStyleMetadata(hair.Value) != null)
		{
			return GetHairStyleMetadata(hair.Value).isBaldStyle;
		}
		if ((uint)(style - 49) <= 6u)
		{
			return true;
		}
		return false;
	}

	private bool isBald()
	{
		return IsBaldHairStyle(getHair());
	}

	/// <summary>Change the color of the player's shoes.</summary>
	/// <param name="color">The new color to set.</param>
	public void changeShoeColor(string which)
	{
		FarmerRenderer.recolorShoes(which);
		shoes.Set(which);
	}

	/// <summary>Change the color of the player's hair.</summary>
	/// <param name="color">The new color to set.</param>
	public void changeHairColor(Color c)
	{
		hairstyleColor.Set(c);
	}

	/// <summary>Change the color of the player's equipped pants.</summary>
	/// <param name="color">The new color to set.</param>
	public void changePantsColor(Color color)
	{
		pantsColor.Set(color);
		pantsItem.Value?.clothesColor.Set(color);
	}

	public void changeHat(int newHat)
	{
		if (newHat < 0)
		{
			Equip(null, hat);
		}
		else
		{
			Equip(ItemRegistry.Create<Hat>("(H)" + newHat), hat);
		}
	}

	public void changeAccessory(int which)
	{
		if (which < -1)
		{
			which = 29;
		}
		if (which >= -1)
		{
			if (which >= 30)
			{
				which = -1;
			}
			accessory.Set(which);
		}
	}

	public void changeSkinColor(int which, bool force = false)
	{
		if (which < 0)
		{
			which = 23;
		}
		else if (which >= 24)
		{
			which = 0;
		}
		skin.Set(FarmerRenderer.recolorSkin(which, force));
	}

	/// <summary>Whether this player has dark skin for the purposes of child genetics.</summary>
	public virtual bool hasDarkSkin()
	{
		if (skin.Value < 4 || skin.Value > 8 || skin.Value == 7)
		{
			return skin.Value == 14;
		}
		return true;
	}

	/// <summary>Change the color of the player's eyes.</summary>
	/// <param name="color">The new color to set.</param>
	public void changeEyeColor(Color c)
	{
		newEyeColor.Set(c);
		FarmerRenderer.recolorEyes(c);
	}

	public int getHair(bool ignore_hat = false)
	{
		if (hat.Value != null && !bathingClothes.Value && !ignore_hat)
		{
			switch ((Hat.HairDrawType)hat.Value.hairDrawType.Value)
			{
			case Hat.HairDrawType.HideHair:
				return -1;
			case Hat.HairDrawType.DrawObscuredHair:
				switch (hair.Value)
				{
				case 50:
				case 51:
				case 52:
				case 53:
				case 54:
				case 55:
					return hair.Value;
				case 48:
					return 6;
				case 49:
					return 52;
				case 3:
					return 11;
				case 1:
				case 5:
				case 6:
				case 9:
				case 11:
				case 17:
				case 20:
				case 23:
				case 24:
				case 25:
				case 27:
				case 28:
				case 29:
				case 30:
				case 32:
				case 33:
				case 34:
				case 36:
				case 39:
				case 41:
				case 43:
				case 44:
				case 45:
				case 46:
				case 47:
					return hair.Value;
				case 18:
				case 19:
				case 21:
				case 31:
					return 23;
				case 42:
					return 46;
				default:
					if (hair.Value >= 16)
					{
						if (hair.Value < 100)
						{
							return 30;
						}
						return hair.Value;
					}
					return 7;
				}
			}
		}
		return hair.Value;
	}

	public void changeGender(bool male)
	{
		if (male)
		{
			Gender = Gender.Male;
			FarmerRenderer.textureName.Set(getTexture());
			FarmerRenderer.heightOffset.Set(0);
		}
		else
		{
			Gender = Gender.Female;
			FarmerRenderer.heightOffset.Set(4);
			FarmerRenderer.textureName.Set(getTexture());
		}
		changeShirt(shirt.Value);
	}

	public void changeFriendship(int amount, NPC n)
	{
		if (n == null || (!(n is Child) && !n.IsVillager))
		{
			return;
		}
		if (amount > 0 && stats.Get("Book_Friendship") != 0)
		{
			amount = (int)((float)amount * 1.1f);
		}
		if (amount > 0 && n.SpeaksDwarvish() && !canUnderstandDwarves)
		{
			return;
		}
		if (friendshipData.TryGetValue(n.Name, out var value))
		{
			if (n.isDivorcedFrom(this) && amount > 0)
			{
				return;
			}
			if (n.Equals(getSpouse()))
			{
				amount = (int)((float)amount * 0.66f);
			}
			value.Points = Math.Max(0, Math.Min(value.Points + amount, (Utility.GetMaximumHeartsForCharacter(n) + 1) * 250 - 1));
			if (n.datable.Value && value.Points >= 2000 && !hasOrWillReceiveMail("Bouquet"))
			{
				Game1.addMailForTomorrow("Bouquet");
			}
			if (n.datable.Value && value.Points >= 2500 && !hasOrWillReceiveMail("SeaAmulet"))
			{
				Game1.addMailForTomorrow("SeaAmulet");
			}
			if (value.Points < 0)
			{
				value.Points = 0;
			}
		}
		else
		{
			Game1.debugOutput = "Tried to change friendship for a friend that wasn't there.";
		}
		Game1.stats.checkForFriendshipAchievements();
	}

	public bool knowsRecipe(string name)
	{
		if (name.EndsWith(" Recipe"))
		{
			name = name.Substring(0, name.Length - " Recipe".Length);
		}
		if (!craftingRecipes.Keys.Contains(name))
		{
			return cookingRecipes.Keys.Contains(name);
		}
		return true;
	}

	public Vector2 getUniformPositionAwayFromBox(int direction, int distance)
	{
		Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
		return FacingDirection switch
		{
			0 => new Vector2(boundingBox.Center.X, boundingBox.Y - distance), 
			1 => new Vector2(boundingBox.Right + distance, boundingBox.Center.Y), 
			2 => new Vector2(boundingBox.Center.X, boundingBox.Bottom + distance), 
			3 => new Vector2(boundingBox.X - distance, boundingBox.Center.Y), 
			_ => Vector2.Zero, 
		};
	}

	public bool hasTalkedToFriendToday(string npcName)
	{
		if (friendshipData.TryGetValue(npcName, out var value))
		{
			return value.TalkedToToday;
		}
		return false;
	}

	public void talkToFriend(NPC n, int friendshipPointChange = 20)
	{
		if (friendshipData.TryGetValue(n.Name, out var value) && !value.TalkedToToday)
		{
			changeFriendship(friendshipPointChange, n);
			value.TalkedToToday = true;
		}
	}

	public void moveRaft(GameLocation currentLocation, GameTime time)
	{
		float num = 0.2f;
		if (CanMove && Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveUpButton))
		{
			yVelocity = Math.Max(yVelocity - num, -3f + Math.Abs(xVelocity) / 2f);
			faceDirection(0);
		}
		if (CanMove && Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveRightButton))
		{
			xVelocity = Math.Min(xVelocity + num, 3f - Math.Abs(yVelocity) / 2f);
			faceDirection(1);
		}
		if (CanMove && Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveDownButton))
		{
			yVelocity = Math.Min(yVelocity + num, 3f - Math.Abs(xVelocity) / 2f);
			faceDirection(2);
		}
		if (CanMove && Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveLeftButton))
		{
			xVelocity = Math.Max(xVelocity - num, -3f + Math.Abs(yVelocity) / 2f);
			faceDirection(3);
		}
		Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle((int)base.Position.X, (int)(base.Position.Y + 64f + 16f), 64, 64);
		rectangle.X += (int)Math.Ceiling(xVelocity);
		if (!currentLocation.isCollidingPosition(rectangle, Game1.viewport, this))
		{
			position.X += xVelocity;
		}
		rectangle.X -= (int)Math.Ceiling(xVelocity);
		rectangle.Y += (int)Math.Floor(yVelocity);
		if (!currentLocation.isCollidingPosition(rectangle, Game1.viewport, this))
		{
			position.Y += yVelocity;
		}
		if (xVelocity != 0f || yVelocity != 0f)
		{
			raftPuddleCounter -= time.ElapsedGameTime.Milliseconds;
			if (raftPuddleCounter <= 0)
			{
				raftPuddleCounter = 250;
				currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), 150f - (Math.Abs(xVelocity) + Math.Abs(yVelocity)) * 3f, 8, 0, new Vector2(rectangle.X, rectangle.Y - 64), flicker: false, Game1.random.NextBool(), 0.001f, 0.01f, Color.White, 1f, 0.003f, 0f, 0f));
				if (Game1.random.NextDouble() < 0.6)
				{
					Game1.playSound("wateringCan");
				}
				if (Game1.random.NextDouble() < 0.6)
				{
					raftBobCounter /= 2;
				}
			}
		}
		raftBobCounter -= time.ElapsedGameTime.Milliseconds;
		if (raftBobCounter <= 0)
		{
			raftBobCounter = Game1.random.Next(15, 28) * 100;
			if (yOffset <= 0f)
			{
				yOffset = 4f;
				currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), 150f - (Math.Abs(xVelocity) + Math.Abs(yVelocity)) * 3f, 8, 0, new Vector2(rectangle.X, rectangle.Y - 64), flicker: false, Game1.random.NextBool(), 0.001f, 0.01f, Color.White, 1f, 0.003f, 0f, 0f));
			}
			else
			{
				yOffset = 0f;
			}
		}
		if (xVelocity > 0f)
		{
			xVelocity = Math.Max(0f, xVelocity - num / 2f);
		}
		else if (xVelocity < 0f)
		{
			xVelocity = Math.Min(0f, xVelocity + num / 2f);
		}
		if (yVelocity > 0f)
		{
			yVelocity = Math.Max(0f, yVelocity - num / 2f);
		}
		else if (yVelocity < 0f)
		{
			yVelocity = Math.Min(0f, yVelocity + num / 2f);
		}
	}

	public void warpFarmer(Warp w, int warp_collide_direction)
	{
		if (w == null || Game1.eventUp)
		{
			return;
		}
		Halt();
		int targetX = w.TargetX;
		int targetY = w.TargetY;
		if (isRidingHorse())
		{
			switch (warp_collide_direction)
			{
			case 3:
				Game1.nextFarmerWarpOffsetX = -1;
				break;
			case 0:
				Game1.nextFarmerWarpOffsetY = -1;
				break;
			}
		}
		Game1.warpFarmer(w.TargetName, targetX, targetY, w.flipFarmer.Value);
	}

	public void warpFarmer(Warp w)
	{
		warpFarmer(w, -1);
	}

	public void startToPassOut()
	{
		passOutEvent.Fire();
	}

	private void performPassOut()
	{
		if (isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
		if (!swimming.Value && bathingClothes.Value)
		{
			bathingClothes.Value = false;
		}
		if (!passedOut && !FarmerSprite.isPassingOut())
		{
			faceDirection(2);
			completelyStopAnimatingOrDoingAction();
			animateOnce(293);
		}
	}

	public static void passOutFromTired(Farmer who)
	{
		if (!who.IsLocalPlayer)
		{
			return;
		}
		if (who.IsSitting())
		{
			who.StopSitting(animate: false);
		}
		if (who.isRidingHorse())
		{
			who.mount.dismount();
		}
		if (Game1.activeClickableMenu != null)
		{
			Game1.activeClickableMenu.emergencyShutDown();
			Game1.exitActiveMenu();
		}
		who.completelyStopAnimatingOrDoingAction();
		if (who.bathingClothes.Value)
		{
			who.changeOutOfSwimSuit();
		}
		who.swimming.Value = false;
		who.CanMove = false;
		who.FarmerSprite.setCurrentSingleFrame(5, 3000);
		who.FarmerSprite.PauseForSingleAnimation = true;
		if (!who.IsDedicatedPlayer && who == Game1.player && who.team.sleepAnnounceMode.Value != FarmerTeam.SleepAnnounceModes.Off)
		{
			string text = "PassedOut";
			string text2 = "PassedOut_" + who.currentLocation.Name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			if (Game1.content.LoadStringReturnNullIfNotFound("Strings\\UI:Chat_" + text2) != null)
			{
				Game1.multiplayer.globalChatInfoMessage(text2, who.displayName);
			}
			else
			{
				int num = 0;
				for (int i = 0; i < 2; i++)
				{
					if (Game1.random.NextDouble() < 0.25)
					{
						num++;
					}
				}
				Game1.multiplayer.globalChatInfoMessage(text + num, who.displayName);
			}
		}
		if (Game1.currentLocation is FarmHouse farmHouse)
		{
			who.lastSleepLocation.Value = farmHouse.NameOrUniqueName;
			who.lastSleepPoint.Value = farmHouse.GetPlayerBedSpot();
		}
		Game1.multiplayer.sendPassoutRequest();
	}

	public static void performPassoutWarp(Farmer who, string bed_location_name, Point bed_point, bool has_bed)
	{
		GameLocation passOutLocation = who.currentLocationRef.Value;
		Vector2 vector = Utility.PointToVector2(bed_point) * 64f;
		Vector2 bed_tile = new Vector2((int)vector.X / 64, (int)vector.Y / 64);
		Vector2 bed_sleep_position = vector;
		if (!who.isInBed.Value)
		{
			LocationRequest locationRequest = Game1.getLocationRequest(bed_location_name);
			Game1.warpFarmer(locationRequest, (int)vector.X / 64, (int)vector.Y / 64, 2);
			locationRequest.OnWarp += ContinuePassOut;
			who.FarmerSprite.setCurrentSingleFrame(5, 3000);
			who.FarmerSprite.PauseForSingleAnimation = true;
		}
		else
		{
			ContinuePassOut();
		}
		void ContinuePassOut()
		{
			who.Position = bed_sleep_position;
			who.currentLocation.lastTouchActionLocation = bed_tile;
			(who.NetFields.Root as NetRoot<Farmer>)?.CancelInterpolation();
			if (!Game1.IsMultiplayer || Game1.timeOfDay >= 2600)
			{
				Game1.PassOutNewDay();
			}
			Game1.changeMusicTrack("none");
			if (!(passOutLocation is FarmHouse) && !(passOutLocation is IslandFarmHouse) && !(passOutLocation is Cellar) && !passOutLocation.HasMapPropertyWithValue("PassOutSafe"))
			{
				Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, who.UniqueMultiplayerID);
				int maxPassOutCost = passOutLocation.GetLocationContext().MaxPassOutCost;
				if (maxPassOutCost == -1)
				{
					maxPassOutCost = LocationContexts.Default.MaxPassOutCost;
				}
				int num = Math.Min(maxPassOutCost, who.Money / 10);
				List<PassOutMailData> obj = passOutLocation.GetLocationContext().PassOutMail ?? LocationContexts.Default.PassOutMail;
				PassOutMailData passOutMailData = null;
				List<PassOutMailData> list = new List<PassOutMailData>();
				foreach (PassOutMailData item in obj)
				{
					if (GameStateQuery.CheckConditions(item.Condition, passOutLocation, null, null, null, random))
					{
						if (item.SkipRandomSelection)
						{
							passOutMailData = item;
							break;
						}
						list.Add(item);
					}
				}
				if (passOutMailData == null && list.Count > 0)
				{
					passOutMailData = random.ChooseFrom(list);
				}
				string text = null;
				if (passOutMailData != null)
				{
					if (passOutMailData.MaxPassOutCost >= 0)
					{
						num = Math.Min(num, passOutMailData.MaxPassOutCost);
					}
					string mail = passOutMailData.Mail;
					if (!string.IsNullOrEmpty(mail))
					{
						Dictionary<string, string> dictionary = DataLoader.Mail(Game1.content);
						text = (dictionary.ContainsKey(mail + "_" + ((num > 0) ? "Billed" : "NotBilled") + "_" + (who.IsMale ? "Male" : "Female")) ? (mail + "_" + ((num > 0) ? "Billed" : "NotBilled") + "_" + (who.IsMale ? "Male" : "Female")) : (dictionary.ContainsKey(mail + "_" + ((num > 0) ? "Billed" : "NotBilled")) ? (mail + "_" + ((num > 0) ? "Billed" : "NotBilled")) : ((!dictionary.ContainsKey(mail)) ? "passedOut2" : mail)));
						if (text.StartsWith("passedOut"))
						{
							text = text + " " + num;
						}
					}
				}
				if (num > 0)
				{
					who.Money -= num;
				}
				if (text != null)
				{
					who.mailForTomorrow.Add(text);
				}
			}
		}
	}

	public static void doSleepEmote(Farmer who)
	{
		who.doEmote(24);
		who.yJumpVelocity = -2f;
	}

	public override Microsoft.Xna.Framework.Rectangle GetBoundingBox()
	{
		if (mount != null && !mount.dismounting.Value)
		{
			return mount.GetBoundingBox();
		}
		Vector2 vector = base.Position;
		return new Microsoft.Xna.Framework.Rectangle((int)vector.X + 8, (int)vector.Y + Sprite.getHeight() - 32, 48, 32);
	}

	public string getPetName()
	{
		foreach (NPC character in Game1.getFarm().characters)
		{
			if (character is Pet)
			{
				return character.Name;
			}
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			foreach (NPC character2 in Utility.getHomeOfFarmer(allFarmer).characters)
			{
				if (character2 is Pet)
				{
					return character2.Name;
				}
			}
		}
		return "your pet";
	}

	public Pet getPet()
	{
		foreach (NPC character in Game1.getFarm().characters)
		{
			if (character is Pet result)
			{
				return result;
			}
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			foreach (NPC character2 in Utility.getHomeOfFarmer(allFarmer).characters)
			{
				if (character2 is Pet result2)
				{
					return result2;
				}
			}
		}
		return null;
	}

	public string getPetDisplayName()
	{
		foreach (NPC character in Game1.getFarm().characters)
		{
			if (character is Pet)
			{
				return character.displayName;
			}
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			foreach (NPC character2 in Utility.getHomeOfFarmer(allFarmer).characters)
			{
				if (character2 is Pet)
				{
					return character2.displayName;
				}
			}
		}
		return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1972");
	}

	public bool hasPet()
	{
		foreach (NPC character in Game1.getFarm().characters)
		{
			if (character is Pet)
			{
				return true;
			}
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			foreach (NPC character2 in Utility.getHomeOfFarmer(allFarmer).characters)
			{
				if (character2 is Pet)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void UpdateClothing()
	{
		FarmerRenderer.MarkSpriteDirty();
	}

	/// <summary>Get whether custom pants should be drawn instead of the equipped pants item.</summary>
	/// <param name="id">The pants ID to draw, if overridden.</param>
	/// <param name="color">The pants color to draw, if overridden.</param>
	public bool IsOverridingPants(out string id, out Color? color)
	{
		if (pants.Value != null && pants.Value != "-1")
		{
			id = pants.Value;
			color = pantsColor.Value;
			return true;
		}
		id = null;
		color = null;
		return false;
	}

	/// <summary>Get whether the current pants can be dyed.</summary>
	public bool CanDyePants()
	{
		return pantsItem.Value?.dyeable.Value ?? false;
	}

	/// <summary>Get the pants to draw on the farmer.</summary>
	/// <param name="texture">The texture to render.</param>
	/// <param name="spriteIndex">The sprite index in the <paramref name="texture" />.</param>
	public void GetDisplayPants(out Texture2D texture, out int spriteIndex)
	{
		if (IsOverridingPants(out var id, out var _))
		{
			ParsedItemData data = ItemRegistry.GetData("(P)" + id);
			if (data != null && !data.IsErrorItem)
			{
				texture = data.GetTexture();
				spriteIndex = data.SpriteIndex;
				return;
			}
		}
		if (pantsItem.Value != null)
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(pantsItem.Value.QualifiedItemId);
			if (dataOrErrorItem != null && !dataOrErrorItem.IsErrorItem)
			{
				texture = dataOrErrorItem.GetTexture();
				spriteIndex = pantsItem.Value.indexInTileSheet.Value;
				return;
			}
		}
		texture = FarmerRenderer.pantsTexture;
		spriteIndex = 14;
	}

	/// <summary>Get the unqualified item ID for the displayed pants (which aren't necessarily the equipped ones).</summary>
	public string GetPantsId()
	{
		if (IsOverridingPants(out var id, out var _))
		{
			return id;
		}
		return pantsItem.Value?.ItemId ?? "14";
	}

	public int GetPantsIndex()
	{
		GetDisplayPants(out var _, out var spriteIndex);
		return spriteIndex;
	}

	/// <summary>Get whether a custom shirt should be drawn instead of the equipped shirt item.</summary>
	/// <param name="id">The shirt ID to draw, if overridden.</param>
	public bool IsOverridingShirt(out string id)
	{
		if (shirt.Value != null && shirt.Value != "-1")
		{
			id = shirt.Value;
			return true;
		}
		id = null;
		return false;
	}

	/// <summary>Get whether the current shirt can be dyed.</summary>
	public bool CanDyeShirt()
	{
		return shirtItem.Value?.dyeable.Value ?? false;
	}

	/// <summary>Get the shirt to draw on the farmer.</summary>
	/// <param name="texture">The texture to render.</param>
	/// <param name="spriteIndex">The sprite index in the <paramref name="texture" />.</param>
	public void GetDisplayShirt(out Texture2D texture, out int spriteIndex)
	{
		if (IsOverridingShirt(out var id))
		{
			ParsedItemData data = ItemRegistry.GetData("(S)" + id);
			if (data != null && !data.IsErrorItem)
			{
				texture = data.GetTexture();
				spriteIndex = data.SpriteIndex;
				return;
			}
		}
		if (shirtItem.Value != null)
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(shirtItem.Value.QualifiedItemId);
			if (dataOrErrorItem != null && !dataOrErrorItem.IsErrorItem)
			{
				texture = dataOrErrorItem.GetTexture();
				spriteIndex = shirtItem.Value.indexInTileSheet.Value;
				return;
			}
		}
		texture = FarmerRenderer.shirtsTexture;
		spriteIndex = (IsMale ? 209 : 41);
	}

	/// <summary>Get the unqualified item ID for the displayed shirt (which isn't necessarily the equipped one).</summary>
	public string GetShirtId()
	{
		if (IsOverridingShirt(out var id))
		{
			return id;
		}
		if (shirtItem.Value != null)
		{
			return shirtItem.Value.ItemId;
		}
		if (!IsMale)
		{
			return "1041";
		}
		return "1209";
	}

	public int GetShirtIndex()
	{
		GetDisplayShirt(out var _, out var spriteIndex);
		return spriteIndex;
	}

	public bool ShirtHasSleeves()
	{
		if (!IsOverridingShirt(out var id))
		{
			id = shirtItem.Value?.ItemId;
		}
		if (id != null && Game1.shirtData.TryGetValue(id, out var value))
		{
			return value.HasSleeves;
		}
		return true;
	}

	/// <summary>Get the color of the currently worn shirt.</summary>
	public Color GetShirtColor()
	{
		if (IsOverridingShirt(out var id) && Game1.shirtData.TryGetValue(id, out var value))
		{
			if (!value.IsPrismatic)
			{
				return Utility.StringToColor(value.DefaultColor) ?? Color.White;
			}
			return Utility.GetPrismaticColor();
		}
		if (shirtItem.Value != null)
		{
			if (shirtItem.Value.isPrismatic.Value)
			{
				return Utility.GetPrismaticColor();
			}
			return shirtItem.Value.clothesColor.Value;
		}
		return DEFAULT_SHIRT_COLOR;
	}

	/// <summary>Get the color of the currently worn pants.</summary>
	public Color GetPantsColor()
	{
		if (IsOverridingPants(out var _, out var color))
		{
			return color ?? Color.White;
		}
		if (pantsItem.Value != null)
		{
			if (pantsItem.Value.isPrismatic.Value)
			{
				return Utility.GetPrismaticColor();
			}
			return pantsItem.Value.clothesColor.Value;
		}
		return Color.White;
	}

	public bool movedDuringLastTick()
	{
		return !base.Position.Equals(lastPosition);
	}

	public int CompareTo(object obj)
	{
		return ((Farmer)obj).saveTime - saveTime;
	}

	public virtual void SetOnBridge(bool val)
	{
		if (onBridge.Value != val)
		{
			onBridge.Value = val;
			if (onBridge.Value)
			{
				showNotCarrying();
			}
		}
	}

	public float getDrawLayer()
	{
		if (onBridge.Value)
		{
			return (float)base.StandingPixel.Y / 10000f + drawLayerDisambiguator + 0.0256f;
		}
		if (IsSitting() && mapChairSitPosition.Value.X != -1f && mapChairSitPosition.Value.Y != -1f)
		{
			return (mapChairSitPosition.Value.Y + 1f) * 64f / 10000f;
		}
		return (float)base.StandingPixel.Y / 10000f + drawLayerDisambiguator;
	}

	public override void draw(SpriteBatch b)
	{
		if (base.currentLocation == null || (!base.currentLocation.Equals(Game1.currentLocation) && !IsLocalPlayer && !Game1.currentLocation.IsTemporary && !isFakeEventActor) || (hidden.Value && (base.currentLocation.currentEvent == null || this != base.currentLocation.currentEvent.farmer) && (!IsLocalPlayer || Game1.locationRequest == null)) || (viewingLocation.Value != null && IsLocalPlayer))
		{
			return;
		}
		float num = getDrawLayer();
		if (isRidingHorse())
		{
			mount.SyncPositionToRider();
			mount.draw(b);
			if (FacingDirection == 3 || FacingDirection == 1)
			{
				num += 0.0016f;
			}
		}
		float layerDepth = FarmerRenderer.GetLayerDepth(0f, FarmerRenderer.FarmerSpriteLayers.MAX);
		Vector2 origin = new Vector2(xOffset, (yOffset + 128f - (float)(GetBoundingBox().Height / 2)) / 4f + 4f);
		Point standingPixel = base.StandingPixel;
		Tile tile = Game1.currentLocation.Map.RequireLayer("Buildings").PickTile(new Location(standingPixel.X, standingPixel.Y), Game1.viewport.Size);
		float num2 = layerDepth * 1f;
		float num3 = layerDepth * 2f;
		if (isGlowing)
		{
			if (coloredBorder)
			{
				b.Draw(Sprite.Texture, new Vector2(getLocalPosition(Game1.viewport).X - 4f, getLocalPosition(Game1.viewport).Y - 4f), Sprite.SourceRect, glowingColor * glowingTransparency, 0f, Vector2.Zero, 1.1f, SpriteEffects.None, num + num2);
			}
			else
			{
				FarmerRenderer.draw(b, FarmerSprite, FarmerSprite.SourceRect, getLocalPosition(Game1.viewport) + jitter + new Vector2(0f, yJumpOffset), origin, num + num2, glowingColor * glowingTransparency, rotation, this);
			}
		}
		if ((!(tile?.TileIndexProperties.ContainsKey("Shadow"))) ?? true)
		{
			if (IsSitting() || !Game1.shouldTimePass() || !temporarilyInvincible || !flashDuringThisTemporaryInvincibility || temporaryInvincibilityTimer % 100 < 50)
			{
				farmerRenderer.Value.draw(b, FarmerSprite, FarmerSprite.SourceRect, getLocalPosition(Game1.viewport) + jitter + new Vector2(0f, yJumpOffset), origin, num, Color.White, rotation, this);
			}
		}
		else
		{
			farmerRenderer.Value.draw(b, FarmerSprite, FarmerSprite.SourceRect, getLocalPosition(Game1.viewport), origin, num, Color.White, rotation, this);
			farmerRenderer.Value.draw(b, FarmerSprite, FarmerSprite.SourceRect, getLocalPosition(Game1.viewport), origin, num + num3, Color.Black * 0.25f, rotation, this);
		}
		if (isRafting)
		{
			b.Draw(Game1.toolSpriteSheet, getLocalPosition(Game1.viewport) + new Vector2(0f, yOffset), Game1.getSourceRectForStandardTileSheet(Game1.toolSpriteSheet, 1), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, FarmerRenderer.GetLayerDepth(num, FarmerRenderer.FarmerSpriteLayers.ToolUp));
		}
		if (Game1.activeClickableMenu == null && !Game1.eventUp && IsLocalPlayer && CurrentTool != null && (Game1.oldKBState.IsKeyDown(Keys.LeftShift) || Game1.options.alwaysShowToolHitLocation) && CurrentTool.doesShowTileLocationMarker() && (!Game1.options.hideToolHitLocationWhenInMotion || !isMoving()))
		{
			Vector2 target_position = Utility.PointToVector2(Game1.getMousePosition()) + new Vector2(Game1.viewport.X, Game1.viewport.Y);
			Vector2 vector = Game1.GlobalToLocal(Game1.viewport, Utility.clampToTile(GetToolLocation(target_position)));
			b.Draw(Game1.mouseCursors, vector, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 29), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, vector.Y / 10000f);
		}
		if (base.IsEmoting)
		{
			Vector2 localPosition = getLocalPosition(Game1.viewport);
			localPosition.Y -= 160f;
			b.Draw(Game1.emoteSpriteSheet, localPosition, new Microsoft.Xna.Framework.Rectangle(base.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, base.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, num);
		}
		if (ActiveObject != null && IsCarrying())
		{
			Game1.drawPlayerHeldObject(this);
		}
		sparklingText?.draw(b, Game1.GlobalToLocal(Game1.viewport, base.Position + new Vector2(32f - sparklingText.textWidth / 2f, -128f)));
		if (UsingTool && CurrentTool != null)
		{
			Game1.drawTool(this);
		}
		foreach (Companion companion in companions)
		{
			companion.Draw(b);
		}
	}

	public virtual void DrawUsername(SpriteBatch b)
	{
		if (!Game1.IsMultiplayer || Game1.multiplayer == null || LocalMultiplayer.IsLocalMultiplayer(is_local_only: true) || usernameDisplayTime <= 0f)
		{
			return;
		}
		string userName = Game1.multiplayer.getUserName(UniqueMultiplayerID);
		if (userName == null)
		{
			return;
		}
		Vector2 vector = Game1.smallFont.MeasureString(userName);
		Vector2 vector2 = getLocalPosition(Game1.viewport) + new Vector2(32f, -104f) - vector / 2f;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (i != 0 || j != 0)
				{
					b.DrawString(Game1.smallFont, userName, vector2 + new Vector2(i, j) * 2f, Color.Black, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999f);
				}
			}
		}
		b.DrawString(Game1.smallFont, userName, vector2, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
	}

	public static void drinkGlug(Farmer who)
	{
		Color color = Color.LightBlue;
		if (who.itemToEat != null)
		{
			switch (ArgUtility.SplitBySpace(who.itemToEat.Name).Last())
			{
			case "Tonic":
				color = Color.Red;
				break;
			case "Remedy":
				color = Color.LimeGreen;
				break;
			case "Coffee":
			case "Cola":
			case "Espresso":
				color = new Color(46, 20, 0);
				break;
			case "Wine":
				color = Color.Purple;
				break;
			case "Beer":
				color = Color.Orange;
				break;
			case "Milk":
				color = Color.White;
				break;
			case "Juice":
			case "Tea":
				color = Color.LightGreen;
				break;
			case "Mayonnaise":
				color = ((who.itemToEat.Name == "Void Mayonnaise") ? Color.Black : Color.White);
				break;
			case "Soup":
				color = Color.LightGreen;
				break;
			}
		}
		if (Game1.currentLocation == who.currentLocation)
		{
			Game1.playSound((who.itemToEat is Object obj && obj.preserve.Value == Object.PreserveType.Pickle) ? "eat" : "gulp");
		}
		who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(653, 858, 1, 1), 9999f, 1, 1, who.Position + new Vector2(32 + Game1.random.Next(-2, 3) * 4, -48f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.001f, 0.04f, color, 5f, 0f, 0f, 0f)
		{
			acceleration = new Vector2(0f, 0.5f)
		});
	}

	public void handleDisconnect()
	{
		if (base.currentLocation != null)
		{
			rightRing.Value?.onLeaveLocation(this, base.currentLocation);
			leftRing.Value?.onLeaveLocation(this, base.currentLocation);
		}
		UnapplyAllTrinketEffects();
		disconnectDay.Value = (int)Game1.stats.DaysPlayed;
		disconnectLocation.Value = base.currentLocation?.NameOrUniqueName ?? "";
		disconnectPosition.Value = base.Position;
	}

	public bool isDivorced()
	{
		foreach (Friendship value in friendshipData.Values)
		{
			if (value.IsDivorced())
			{
				return true;
			}
		}
		return false;
	}

	public void wipeExMemories()
	{
		foreach (string key in friendshipData.Keys)
		{
			Friendship friendship = friendshipData[key];
			if (friendship.IsDivorced())
			{
				friendship.Clear();
				NPC characterFromName = Game1.getCharacterFromName(key);
				if (characterFromName != null)
				{
					characterFromName.CurrentDialogue.Clear();
					characterFromName.CurrentDialogue.Push(characterFromName.TryGetDialogue("WipedMemory") ?? new Dialogue(characterFromName, "Strings\\Characters:WipedMemory"));
					Game1.stats.Increment("exMemoriesWiped");
				}
			}
		}
	}

	public void getRidOfChildren()
	{
		FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(this);
		for (int num = homeOfFarmer.characters.Count - 1; num >= 0; num--)
		{
			if (homeOfFarmer.characters[num] is Child child)
			{
				homeOfFarmer.GetChildBed((int)child.Gender)?.mutex.ReleaseLock();
				if (child.hat.Value != null)
				{
					Hat value = child.hat.Value;
					child.hat.Value = null;
					team.returnedDonations.Add(value);
					team.newLostAndFoundItems.Value = true;
				}
				homeOfFarmer.characters.RemoveAt(num);
				Game1.stats.Increment("childrenTurnedToDoves");
			}
		}
	}

	public void animateOnce(int whichAnimation)
	{
		FarmerSprite.animateOnce(whichAnimation, 100f, 6);
		CanMove = false;
	}

	public static void showItemIntake(Farmer who)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = null;
		Object obj = ((!(who.mostRecentlyGrabbedItem is Object obj2)) ? ((who.ActiveObject == null) ? null : who.ActiveObject) : obj2);
		if (obj == null)
		{
			return;
		}
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId);
		string textureName = dataOrErrorItem.TextureName;
		Microsoft.Xna.Framework.Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
		switch (who.FacingDirection)
		{
		case 2:
			switch (who.FarmerSprite.currentAnimationIndex)
			{
			case 1:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(0f, -32f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 2:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(0f, -43f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 3:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 4:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 200f, 1, 0, who.Position + new Vector2(0f, -120f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 5:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 200f, 1, 0, who.Position + new Vector2(0f, -120f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0.02f, Color.White, 4f, -0.02f, 0f, 0f);
				break;
			}
			break;
		case 1:
			switch (who.FarmerSprite.currentAnimationIndex)
			{
			case 1:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(28f, -64f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 2:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(24f, -72f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 3:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(4f, -128f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 4:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 200f, 1, 0, who.Position + new Vector2(0f, -124f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 5:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 200f, 1, 0, who.Position + new Vector2(0f, -124f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0.02f, Color.White, 4f, -0.02f, 0f, 0f);
				break;
			}
			break;
		case 0:
			switch (who.FarmerSprite.currentAnimationIndex)
			{
			case 1:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(0f, -32f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f - 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 2:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(0f, -43f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f - 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 3:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f - 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 4:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 200f, 1, 0, who.Position + new Vector2(0f, -120f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f - 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 5:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 200f, 1, 0, who.Position + new Vector2(0f, -120f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f - 0.001f, 0.02f, Color.White, 4f, -0.02f, 0f, 0f);
				break;
			}
			break;
		case 3:
			switch (who.FarmerSprite.currentAnimationIndex)
			{
			case 1:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(-32f, -64f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 2:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(-28f, -76f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 3:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 100f, 1, 0, who.Position + new Vector2(-16f, -128f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 4:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 200f, 1, 0, who.Position + new Vector2(0f, -124f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f);
				break;
			case 5:
				temporaryAnimatedSprite = new TemporaryAnimatedSprite(textureName, sourceRect, 200f, 1, 0, who.Position + new Vector2(0f, -124f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.01f, 0.02f, Color.White, 4f, -0.02f, 0f, 0f);
				break;
			}
			break;
		}
		if (obj.QualifiedItemId == who.ActiveObject?.QualifiedItemId && who.FarmerSprite.currentAnimationIndex == 5)
		{
			temporaryAnimatedSprite = null;
		}
		if (temporaryAnimatedSprite != null)
		{
			who.currentLocation.temporarySprites.Add(temporaryAnimatedSprite);
		}
		if (who.mostRecentlyGrabbedItem is ColoredObject coloredObject && temporaryAnimatedSprite != null)
		{
			Microsoft.Xna.Framework.Rectangle sourceRect2 = ItemRegistry.GetDataOrErrorItem(coloredObject.QualifiedItemId).GetSourceRect(1);
			who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(textureName, sourceRect2, temporaryAnimatedSprite.interval, 1, 0, temporaryAnimatedSprite.Position, flicker: false, flipped: false, temporaryAnimatedSprite.layerDepth + 0.0001f, temporaryAnimatedSprite.alphaFade, coloredObject.color.Value, 4f, temporaryAnimatedSprite.scaleChange, 0f, 0f));
		}
		if (who.FarmerSprite.currentAnimationIndex == 5)
		{
			who.Halt();
			who.FarmerSprite.CurrentAnimation = null;
		}
	}

	public virtual void showSwordSwipe(Farmer who)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = null;
		Vector2 toolLocation = who.GetToolLocation(ignoreClick: true);
		bool flag = false;
		if (who.CurrentTool is MeleeWeapon meleeWeapon)
		{
			flag = meleeWeapon.type.Value == 1;
			if (!flag)
			{
				meleeWeapon.DoDamage(who.currentLocation, (int)toolLocation.X, (int)toolLocation.Y, who.FacingDirection, 1, who);
			}
		}
		int val = 20;
		switch (who.FacingDirection)
		{
		case 2:
			switch (who.FarmerSprite.currentAnimationIndex)
			{
			case 0:
				if (flag)
				{
					who.yVelocity = -0.6f;
				}
				break;
			case 1:
				who.yVelocity = (flag ? 0.5f : (-0.5f));
				break;
			case 5:
				who.yVelocity = 0.3f;
				temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(503, 256, 42, 17), who.Position + new Vector2(-16f, -2f) * 4f, flipped: false, 0.07f, Color.White)
				{
					scale = 4f,
					animationLength = 1,
					interval = Math.Max(who.FarmerSprite.CurrentAnimationFrame.milliseconds, val),
					alpha = 0.5f,
					layerDepth = (who.Position.Y + 64f) / 10000f
				};
				break;
			}
			break;
		case 1:
			switch (who.FarmerSprite.currentAnimationIndex)
			{
			case 0:
				if (flag)
				{
					who.xVelocity = 0.6f;
				}
				break;
			case 1:
				who.xVelocity = (flag ? (-0.5f) : 0.5f);
				break;
			case 5:
				who.xVelocity = -0.3f;
				temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(518, 274, 23, 31), who.Position + new Vector2(4f, -12f) * 4f, flipped: false, 0.07f, Color.White)
				{
					scale = 4f,
					animationLength = 1,
					interval = Math.Max(who.FarmerSprite.CurrentAnimationFrame.milliseconds, val),
					alpha = 0.5f
				};
				break;
			}
			break;
		case 3:
			switch (who.FarmerSprite.currentAnimationIndex)
			{
			case 0:
				if (flag)
				{
					who.xVelocity = -0.6f;
				}
				break;
			case 1:
				who.xVelocity = (flag ? 0.5f : (-0.5f));
				break;
			case 5:
				who.xVelocity = 0.3f;
				temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(518, 274, 23, 31), who.Position + new Vector2(-15f, -12f) * 4f, flipped: false, 0.07f, Color.White)
				{
					scale = 4f,
					animationLength = 1,
					interval = Math.Max(who.FarmerSprite.CurrentAnimationFrame.milliseconds, val),
					flipped = true,
					alpha = 0.5f
				};
				break;
			}
			break;
		case 0:
			switch (who.FarmerSprite.currentAnimationIndex)
			{
			case 0:
				if (flag)
				{
					who.yVelocity = 0.6f;
				}
				break;
			case 1:
				who.yVelocity = (flag ? (-0.5f) : 0.5f);
				break;
			case 5:
				who.yVelocity = -0.3f;
				temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(518, 274, 23, 31), who.Position + new Vector2(0f, -32f) * 4f, flipped: false, 0.07f, Color.White)
				{
					scale = 4f,
					animationLength = 1,
					interval = Math.Max(who.FarmerSprite.CurrentAnimationFrame.milliseconds, val),
					alpha = 0.5f,
					rotation = 3.926991f
				};
				break;
			}
			break;
		}
		if (temporaryAnimatedSprite != null)
		{
			if (who.CurrentTool?.QualifiedItemId == "(W)4")
			{
				temporaryAnimatedSprite.color = Color.HotPink;
			}
			who.currentLocation.temporarySprites.Add(temporaryAnimatedSprite);
		}
	}

	public static void showToolSwipeEffect(Farmer who)
	{
		if (!(who.CurrentTool is WateringCan))
		{
			switch (who.FacingDirection)
			{
			case 1:
				who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(15, who.Position + new Vector2(20f, -132f), Color.White, 4, flipped: false, (who.stamina <= 0f) ? 80f : 40f, 0, 128, 1f, 128)
				{
					layerDepth = (float)(who.GetBoundingBox().Bottom + 1) / 10000f
				});
				break;
			case 3:
				who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(15, who.Position + new Vector2(-92f, -132f), Color.White, 4, flipped: true, (who.stamina <= 0f) ? 80f : 40f, 0, 128, 1f, 128)
				{
					layerDepth = (float)(who.GetBoundingBox().Bottom + 1) / 10000f
				});
				break;
			case 2:
				who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(19, who.Position + new Vector2(-4f, -128f), Color.White, 4, flipped: false, (who.stamina <= 0f) ? 80f : 40f, 0, 128, 1f, 128)
				{
					layerDepth = (float)(who.GetBoundingBox().Bottom + 1) / 10000f
				});
				break;
			case 0:
				who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(18, who.Position + new Vector2(0f, -132f), Color.White, 4, flipped: false, (who.stamina <= 0f) ? 100f : 50f, 0, 64, 1f, 64)
				{
					layerDepth = (float)(who.StandingPixel.Y - 9) / 10000f
				});
				break;
			}
		}
	}

	public static void canMoveNow(Farmer who)
	{
		who.CanMove = true;
		who.UsingTool = false;
		who.usingSlingshot = false;
		who.FarmerSprite.PauseForSingleAnimation = false;
		who.yVelocity = 0f;
		who.xVelocity = 0f;
	}

	public void FireTool()
	{
		fireToolEvent.Fire();
	}

	public void synchronizedJump(float velocity)
	{
		if (IsLocalPlayer)
		{
			synchronizedJumpEvent.Fire(velocity);
			synchronizedJumpEvent.Poll();
		}
	}

	protected void performSynchronizedJump(float velocity)
	{
		yJumpVelocity = velocity;
		yJumpOffset = -1;
	}

	private void performFireTool()
	{
		if (isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
		CurrentTool?.leftClick(this);
	}

	public static void useTool(Farmer who)
	{
		if (who.toolOverrideFunction != null)
		{
			who.toolOverrideFunction(who);
		}
		else if (who.CurrentTool != null)
		{
			float oldStamina = who.stamina;
			if (who.IsLocalPlayer)
			{
				who.CurrentTool.DoFunction(who.currentLocation, (int)who.GetToolLocation().X, (int)who.GetToolLocation().Y, 1, who);
			}
			who.lastClick = Vector2.Zero;
			who.checkForExhaustion(oldStamina);
		}
	}

	public void BeginUsingTool()
	{
		beginUsingToolEvent.Fire();
	}

	private void performBeginUsingTool()
	{
		if (isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
		if (CurrentTool != null)
		{
			CanMove = false;
			UsingTool = true;
			canReleaseTool = true;
			CurrentTool.beginUsing(base.currentLocation, (int)lastClick.X, (int)lastClick.Y, this);
		}
	}

	public void EndUsingTool()
	{
		if (this == Game1.player)
		{
			endUsingToolEvent.Fire();
		}
		else
		{
			performEndUsingTool();
		}
	}

	private void performEndUsingTool()
	{
		if (isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
		CurrentTool?.endUsing(base.currentLocation, this);
	}

	public void checkForExhaustion(float oldStamina)
	{
		if (stamina <= 0f && oldStamina > 0f)
		{
			if (!exhausted.Value && IsLocalPlayer)
			{
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1986"));
			}
			setRunning(isRunning: false);
			doEmote(36);
		}
		else if (stamina <= 15f && oldStamina > 15f && IsLocalPlayer)
		{
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1987"));
		}
		if (stamina <= 0f)
		{
			exhausted.Value = true;
		}
	}

	public void setMoving(byte command)
	{
		switch (command)
		{
		case 1:
			if (movementDirections.Count < 2 && !movementDirections.Contains(0) && !movementDirections.Contains(2))
			{
				movementDirections.Insert(0, 0);
			}
			break;
		case 2:
			if (movementDirections.Count < 2 && !movementDirections.Contains(1) && !movementDirections.Contains(3))
			{
				movementDirections.Insert(0, 1);
			}
			break;
		case 4:
			if (movementDirections.Count < 2 && !movementDirections.Contains(2) && !movementDirections.Contains(0))
			{
				movementDirections.Insert(0, 2);
			}
			break;
		case 8:
			if (movementDirections.Count < 2 && !movementDirections.Contains(3) && !movementDirections.Contains(1))
			{
				movementDirections.Insert(0, 3);
			}
			break;
		case 33:
			movementDirections.Remove(0);
			break;
		case 34:
			movementDirections.Remove(1);
			break;
		case 36:
			movementDirections.Remove(2);
			break;
		case 40:
			movementDirections.Remove(3);
			break;
		case 16:
			setRunning(isRunning: true);
			break;
		case 48:
			setRunning(isRunning: false);
			break;
		}
		if ((command & 0x40) == 64)
		{
			Halt();
			running = false;
		}
	}

	public void toolPowerIncrease()
	{
		if (CurrentTool is Pan)
		{
			return;
		}
		if (toolPower.Value == 0)
		{
			toolPitchAccumulator = 0;
		}
		toolPower.Value++;
		if (CurrentTool is Pickaxe && toolPower.Value == 1)
		{
			toolPower.Value += 2;
		}
		Color color = Color.White;
		int num = ((FacingDirection == 0) ? 4 : ((FacingDirection == 2) ? 2 : 0));
		switch (toolPower.Value)
		{
		case 1:
			color = Color.Orange;
			if (!(CurrentTool is WateringCan))
			{
				FarmerSprite.CurrentFrame = 72 + num;
			}
			jitterStrength = 0.25f;
			break;
		case 2:
			color = Color.LightSteelBlue;
			if (!(CurrentTool is WateringCan))
			{
				FarmerSprite.CurrentFrame++;
			}
			jitterStrength = 0.5f;
			break;
		case 3:
			color = Color.Gold;
			jitterStrength = 1f;
			break;
		case 4:
			color = Color.Violet;
			jitterStrength = 2f;
			break;
		case 5:
			color = Color.BlueViolet;
			jitterStrength = 3f;
			break;
		}
		int num2 = ((FacingDirection == 1) ? 40 : ((FacingDirection == 3) ? (-40) : ((FacingDirection == 2) ? 32 : 0)));
		int num3 = 192;
		if (CurrentTool is WateringCan)
		{
			switch (FacingDirection)
			{
			case 3:
				num2 = 48;
				break;
			case 1:
				num2 = -48;
				break;
			case 2:
				num2 = 0;
				break;
			}
			num3 = 128;
		}
		int y = base.StandingPixel.Y;
		Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(21, base.Position - new Vector2(num2, num3), color, 8, flipped: false, 70f, 0, 64, (float)y / 10000f + 0.005f, 128));
		Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(192, 1152, 64, 64), 50f, 4, 0, base.Position - new Vector2((FacingDirection != 1) ? (-64) : 0, 128f), flicker: false, FacingDirection == 1, (float)y / 10000f, 0.01f, Color.White, 1f, 0f, 0f, 0f));
		int value = Utility.CreateRandom(Game1.dayOfMonth, (double)base.Position.X * 1000.0, base.Position.Y).Next(12, 16) * 100 + toolPower.Value * 100;
		Game1.playSound("toolCharge", value);
	}

	public void UpdateIfOtherPlayer(GameTime time)
	{
		if (base.currentLocation == null)
		{
			return;
		}
		position.UpdateExtrapolation(getMovementSpeed());
		position.Field.InterpolationEnabled = !currentLocationRef.IsChanging();
		if (Game1.ShouldShowOnscreenUsernames() && Game1.mouseCursorTransparency > 0f && base.currentLocation == Game1.currentLocation && Game1.currentMinigame == null && Game1.activeClickableMenu == null)
		{
			Vector2 localPosition = getLocalPosition(Game1.viewport);
			Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 128, 192);
			rectangle.X = (int)(localPosition.X + 32f - (float)(rectangle.Width / 2));
			rectangle.Y = (int)(localPosition.Y - (float)rectangle.Height + 48f);
			if (rectangle.Contains(Game1.getMouseX(ui_scale: false), Game1.getMouseY(ui_scale: false)))
			{
				usernameDisplayTime = 1f;
			}
		}
		if (_lastSelectedItem != CurrentItem)
		{
			_lastSelectedItem?.actionWhenStopBeingHeld(this);
			_lastSelectedItem = CurrentItem;
		}
		fireToolEvent.Poll();
		beginUsingToolEvent.Poll();
		endUsingToolEvent.Poll();
		drinkAnimationEvent.Poll();
		eatAnimationEvent.Poll();
		sickAnimationEvent.Poll();
		passOutEvent.Poll();
		doEmoteEvent.Poll();
		kissFarmerEvent.Poll();
		haltAnimationEvent.Poll();
		synchronizedJumpEvent.Poll();
		renovateEvent.Poll();
		FarmerSprite.checkForSingleAnimation(time);
		updateCommon(time, base.currentLocation);
	}

	/// <summary>Put an item into an equipment slot with appropriate updates (e.g. calling <see cref="M:StardewValley.Item.onEquip(StardewValley.Farmer)" /> or <see cref="M:StardewValley.Item.onUnequip(StardewValley.Farmer)" />).</summary>
	/// <typeparam name="TItem">The item type.</typeparam>
	/// <param name="newItem">The item to place in the equipment slot, or <c>null</c> to just unequip the old item.</param>
	/// <param name="slot">The equipment slot to update.</param>
	/// <returns>Returns the item that was previously in the equipment slot, or <c>null</c> if it was empty.</returns>
	public TItem Equip<TItem>(TItem newItem, NetRef<TItem> slot) where TItem : Item
	{
		TItem value = slot.Value;
		value?.onDetachedFromParent();
		newItem?.onDetachedFromParent();
		Equip(value, newItem, delegate(TItem val)
		{
			slot.Value = val;
		});
		return value;
	}

	/// <summary>Place an item into an equipment slot manually with appropriate updates (e.g. calling <see cref="M:StardewValley.Item.onEquip(StardewValley.Farmer)" /> or <see cref="M:StardewValley.Item.onUnequip(StardewValley.Farmer)" />).</summary>
	/// <typeparam name="TItem">The item type.</typeparam>
	/// <param name="oldItem">The item previously in the equipment slot, or <c>null</c> if it was empty.</param>
	/// <param name="newItem">The item to place in the equipment slot, or <c>null</c> to just unequip the old item.</param>
	/// <param name="equip">A callback which equips an item in the slot.</param>
	/// <remarks>Most code should use <see cref="M:StardewValley.Farmer.Equip``1(``0,Netcode.NetRef{``0})" /> instead. When calling this form, you should call <see cref="M:StardewValley.Item.onDetachedFromParent" /> on the old/new items as needed to avoid warnings.</remarks>
	public void Equip<TItem>(TItem oldItem, TItem newItem, Action<TItem> equip) where TItem : Item
	{
		bool flag = Game1.hasLoadedGame && Game1.dayOfMonth > 0 && IsLocalPlayer;
		if (flag)
		{
			oldItem?.onUnequip(this);
		}
		equip(newItem);
		if (newItem != null)
		{
			newItem.HasBeenInInventory = true;
			if (flag)
			{
				newItem.onEquip(this);
			}
		}
		if ((oldItem?.HasEquipmentBuffs() ?? false) || !((!(newItem?.HasEquipmentBuffs())) ?? true))
		{
			buffs.Dirty = true;
		}
	}

	public void forceCanMove()
	{
		forceTimePass = false;
		movementDirections.Clear();
		isEating = false;
		CanMove = true;
		Game1.freezeControls = false;
		freezePause = 0;
		UsingTool = false;
		usingSlingshot = false;
		FarmerSprite.PauseForSingleAnimation = false;
		if (CurrentTool is FishingRod fishingRod)
		{
			fishingRod.isFishing = false;
		}
	}

	public void dropItem(Item i)
	{
		if (i != null && i.canBeDropped())
		{
			Game1.createItemDebris(i.getOne(), getStandingPosition(), FacingDirection);
		}
	}

	public bool addEvent(string eventName, int daysActive)
	{
		return activeDialogueEvents.TryAdd(eventName, daysActive);
	}

	public Vector2 getMostRecentMovementVector()
	{
		return new Vector2(base.Position.X - lastPosition.X, base.Position.Y - lastPosition.Y);
	}

	public int GetSkillLevel(int index)
	{
		return index switch
		{
			0 => FarmingLevel, 
			3 => MiningLevel, 
			1 => FishingLevel, 
			2 => ForagingLevel, 
			5 => LuckLevel, 
			4 => CombatLevel, 
			_ => 0, 
		};
	}

	public int GetUnmodifiedSkillLevel(int index)
	{
		return index switch
		{
			0 => farmingLevel.Value, 
			3 => miningLevel.Value, 
			1 => fishingLevel.Value, 
			2 => foragingLevel.Value, 
			5 => luckLevel.Value, 
			4 => combatLevel.Value, 
			_ => 0, 
		};
	}

	public static string getSkillNameFromIndex(int index)
	{
		return index switch
		{
			0 => "Farming", 
			3 => "Mining", 
			1 => "Fishing", 
			2 => "Foraging", 
			5 => "Luck", 
			4 => "Combat", 
			_ => "", 
		};
	}

	public static int getSkillNumberFromName(string name)
	{
		return name.ToLower() switch
		{
			"farming" => 0, 
			"mining" => 3, 
			"fishing" => 1, 
			"foraging" => 2, 
			"luck" => 5, 
			"combat" => 4, 
			_ => -1, 
		};
	}

	public bool setSkillLevel(string nameOfSkill, int level)
	{
		int skillNumberFromName = getSkillNumberFromName(nameOfSkill);
		switch (nameOfSkill)
		{
		case "Farming":
			if (farmingLevel.Value < level)
			{
				newLevels.Add(new Point(skillNumberFromName, level - farmingLevel.Value));
				farmingLevel.Value = level;
				experiencePoints[skillNumberFromName] = getBaseExperienceForLevel(level);
				return true;
			}
			break;
		case "Fishing":
			if (fishingLevel.Value < level)
			{
				newLevels.Add(new Point(skillNumberFromName, level - fishingLevel.Value));
				fishingLevel.Value = level;
				experiencePoints[skillNumberFromName] = getBaseExperienceForLevel(level);
				return true;
			}
			break;
		case "Foraging":
			if (foragingLevel.Value < level)
			{
				newLevels.Add(new Point(skillNumberFromName, level - foragingLevel.Value));
				foragingLevel.Value = level;
				experiencePoints[skillNumberFromName] = getBaseExperienceForLevel(level);
				return true;
			}
			break;
		case "Mining":
			if (miningLevel.Value < level)
			{
				newLevels.Add(new Point(skillNumberFromName, level - miningLevel.Value));
				miningLevel.Value = level;
				experiencePoints[skillNumberFromName] = getBaseExperienceForLevel(level);
				return true;
			}
			break;
		case "Combat":
			if (combatLevel.Value < level)
			{
				newLevels.Add(new Point(skillNumberFromName, level - combatLevel.Value));
				combatLevel.Value = level;
				experiencePoints[skillNumberFromName] = getBaseExperienceForLevel(level);
				return true;
			}
			break;
		}
		return false;
	}

	public static string getSkillDisplayNameFromIndex(int index)
	{
		return index switch
		{
			0 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1991"), 
			3 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1992"), 
			1 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1993"), 
			2 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1994"), 
			5 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1995"), 
			4 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1996"), 
			_ => "", 
		};
	}

	public bool hasCompletedCommunityCenter()
	{
		if (mailReceived.Contains("ccBoilerRoom") && mailReceived.Contains("ccCraftsRoom") && mailReceived.Contains("ccPantry") && mailReceived.Contains("ccFishTank") && mailReceived.Contains("ccVault"))
		{
			return mailReceived.Contains("ccBulletin");
		}
		return false;
	}

	private bool localBusMoving()
	{
		GameLocation gameLocation = base.currentLocation;
		if (!(gameLocation is Desert desert))
		{
			if (gameLocation is BusStop busStop)
			{
				if (!busStop.drivingOff)
				{
					return busStop.drivingBack;
				}
				return true;
			}
			return false;
		}
		if (!desert.drivingOff)
		{
			return desert.drivingBack;
		}
		return true;
	}

	public virtual bool CanBeDamaged()
	{
		if (!IsDedicatedPlayer && !temporarilyInvincible && !isEating && !Game1.fadeToBlack)
		{
			return !hasBuff("21");
		}
		return false;
	}

	public void takeDamage(int damage, bool overrideParry, Monster damager)
	{
		if (Game1.eventUp || IsDedicatedPlayer || FarmerSprite.isPassingOut() || (isInBed.Value && Game1.activeClickableMenu != null && Game1.activeClickableMenu is ReadyCheckDialog))
		{
			return;
		}
		bool num = damager != null && !damager.isInvincible() && !overrideParry;
		bool flag = (damager == null || !damager.isInvincible()) && (damager == null || (!(damager is GreenSlime) && !(damager is BigSlime)) || !isWearingRing("520"));
		bool flag2 = CurrentTool is MeleeWeapon && ((MeleeWeapon)CurrentTool).isOnSpecial && ((MeleeWeapon)CurrentTool).type.Value == 3;
		bool flag3 = CanBeDamaged();
		if (num && flag2)
		{
			Rumble.rumble(0.75f, 150f);
			playNearbySoundAll("parry");
			damager.parried(damage, this);
		}
		else
		{
			if (!(flag && flag3))
			{
				return;
			}
			damager?.onDealContactDamage(this);
			damage += Game1.random.Next(Math.Min(-1, -damage / 8), Math.Max(1, damage / 8));
			int num2 = buffs.Defense;
			if (stats.Get("Book_Defense") != 0)
			{
				num2++;
			}
			if ((float)num2 >= (float)damage * 0.5f)
			{
				num2 -= (int)((float)num2 * (float)Game1.random.Next(3) / 10f);
			}
			if (damager != null && isWearingRing("839"))
			{
				Microsoft.Xna.Framework.Rectangle boundingBox = damager.GetBoundingBox();
				_ = Utility.getAwayFromPlayerTrajectory(boundingBox, this) / 2f;
				int num3 = damage;
				int num4 = Math.Max(1, damage - num2);
				if (num4 < 10)
				{
					num3 = (int)Math.Ceiling((double)(num3 + num4) / 2.0);
				}
				int numberOfWornRingsWithID = getNumberOfWornRingsWithID("839");
				num3 *= numberOfWornRingsWithID;
				base.currentLocation?.damageMonster(boundingBox, num3, num3 + 1, isBomb: false, this);
			}
			if (isWearingRing("524") && !hasBuff("21") && Game1.random.NextDouble() < (0.9 - (double)((float)health / 100f)) / (double)(3 - LuckLevel / 10) + ((health <= 15) ? 0.2 : 0.0))
			{
				playNearbySoundAll("yoba");
				applyBuff("21");
				return;
			}
			Rumble.rumble(0.75f, 150f);
			damage = Math.Max(1, damage - num2);
			if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && base.currentLocation is MineShaft && Game1.mine.getMineArea() == 121)
			{
				float num5 = 1f;
				if (team.calicoStatueEffects.TryGetValue(8, out var value))
				{
					num5 += (float)value * 0.25f;
				}
				if (team.calicoStatueEffects.TryGetValue(14, out var value2))
				{
					num5 -= (float)value2 * 0.25f;
				}
				damage = Math.Max(1, (int)((float)damage * num5));
			}
			health = Math.Max(0, health - damage);
			foreach (Trinket trinketItem in trinketItems)
			{
				trinketItem?.OnReceiveDamage(this, damage);
			}
			if (health <= 0 && GetEffectsOfRingMultiplier("863") > 0 && !hasUsedDailyRevive.Value)
			{
				startGlowing(new Color(255, 255, 0), border: false, 0.25f);
				DelayedAction.functionAfterDelay(base.stopGlowing, 500);
				Game1.playSound("yoba");
				for (int i = 0; i < 13; i++)
				{
					float num6 = Game1.random.Next(-32, 33);
					base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(114, 46, 2, 2), 200f, 5, 1, new Vector2(num6 + 32f, -96f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						attachedCharacter = this,
						positionFollowsAttachedCharacter = true,
						motion = new Vector2(num6 / 32f, -3f),
						delayBeforeAnimationStart = i * 50,
						alphaFade = 0.001f,
						acceleration = new Vector2(0f, 0.1f)
					});
				}
				base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(157, 280, 28, 19), 2000f, 1, 1, new Vector2(-20f, -16f), flicker: false, flipped: false, 1E-06f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					attachedCharacter = this,
					positionFollowsAttachedCharacter = true,
					alpha = 0.1f,
					alphaFade = -0.01f,
					alphaFadeFade = -0.00025f
				});
				health = (int)Math.Min(maxHealth, (float)maxHealth * 0.5f + (float)GetEffectsOfRingMultiplier("863"));
				hasUsedDailyRevive.Value = true;
			}
			temporarilyInvincible = true;
			flashDuringThisTemporaryInvincibility = true;
			temporaryInvincibilityTimer = 0;
			currentTemporaryInvincibilityDuration = 1200 + GetEffectsOfRingMultiplier("861") * 400;
			Point standingPixel = base.StandingPixel;
			base.currentLocation.debris.Add(new Debris(damage, new Vector2(standingPixel.X + 8, standingPixel.Y), Color.Red, 1f, this));
			playNearbySoundAll("ow");
			Game1.hitShakeTimer = 100 * damage;
		}
	}

	public int GetEffectsOfRingMultiplier(string ringId)
	{
		int num = 0;
		if (leftRing.Value != null)
		{
			num += leftRing.Value.GetEffectsOfRingMultiplier(ringId);
		}
		if (rightRing.Value != null)
		{
			num += rightRing.Value.GetEffectsOfRingMultiplier(ringId);
		}
		return num;
	}

	private void checkDamage(GameLocation location)
	{
		if (Game1.eventUp)
		{
			return;
		}
		for (int num = location.characters.Count - 1; num >= 0; num--)
		{
			if (num < location.characters.Count && location.characters[num] is Monster monster && monster.OverlapsFarmerForDamage(this))
			{
				monster.currentLocation = location;
				monster.collisionWithFarmerBehavior();
				if (monster.DamageToFarmer > 0)
				{
					if (CurrentTool is MeleeWeapon && ((MeleeWeapon)CurrentTool).isOnSpecial && ((MeleeWeapon)CurrentTool).type.Value == 3)
					{
						takeDamage(monster.DamageToFarmer, overrideParry: false, monster);
					}
					else
					{
						takeDamage(Math.Max(1, monster.DamageToFarmer + Game1.random.Next(-monster.DamageToFarmer / 4, monster.DamageToFarmer / 4)), overrideParry: false, monster);
					}
				}
			}
		}
	}

	public bool checkAction(Farmer who, GameLocation location)
	{
		if (who.isRidingHorse())
		{
			who.Halt();
		}
		if (hidden.Value)
		{
			return false;
		}
		if (Game1.CurrentEvent != null)
		{
			if (Game1.CurrentEvent.isSpecificFestival("spring24") && who.dancePartner.Value == null)
			{
				who.Halt();
				who.faceGeneralDirection(getStandingPosition(), 0, opposite: false, useTileCalculations: false);
				string question = Game1.content.LoadString("Strings\\UI:AskToDance_" + (IsMale ? "Male" : "Female"), base.Name);
				location.createQuestionDialogue(question, location.createYesNoResponses(), delegate(Farmer _, string answer)
				{
					if (answer == "Yes")
					{
						who.team.SendProposal(this, ProposalType.Dance);
						Game1.activeClickableMenu = new PendingProposalDialog();
					}
				});
				return true;
			}
			return false;
		}
		if (who.CurrentItem != null && who.CurrentItem.QualifiedItemId == "(O)801" && !isMarriedOrRoommates() && !isEngaged() && !who.isMarriedOrRoommates() && !who.isEngaged())
		{
			who.Halt();
			who.faceGeneralDirection(getStandingPosition(), 0, opposite: false, useTileCalculations: false);
			string question2 = Game1.content.LoadString("Strings\\UI:AskToMarry_" + (IsMale ? "Male" : "Female"), base.Name);
			location.createQuestionDialogue(question2, location.createYesNoResponses(), delegate(Farmer _, string answer)
			{
				if (answer == "Yes")
				{
					who.team.SendProposal(this, ProposalType.Marriage, who.CurrentItem.getOne());
					Game1.activeClickableMenu = new PendingProposalDialog();
				}
			});
			return true;
		}
		if (who.CanMove)
		{
			bool? flag = who.ActiveObject?.canBeGivenAsGift();
			if (flag.HasValue && flag == true && !who.ActiveObject.questItem.Value)
			{
				who.Halt();
				who.faceGeneralDirection(getStandingPosition(), 0, opposite: false, useTileCalculations: false);
				string question3 = Game1.content.LoadString("Strings\\UI:GiftPlayerItem_" + (IsMale ? "Male" : "Female"), who.ActiveObject.DisplayName, base.Name);
				location.createQuestionDialogue(question3, location.createYesNoResponses(), delegate(Farmer _, string answer)
				{
					if (answer == "Yes")
					{
						who.team.SendProposal(this, ProposalType.Gift, who.ActiveObject.getOne());
						Game1.activeClickableMenu = new PendingProposalDialog();
					}
				});
				return true;
			}
		}
		long? num = team.GetSpouse(UniqueMultiplayerID);
		if ((num.HasValue & (who.UniqueMultiplayerID == num)) && who.CanMove && !who.isMoving() && !isMoving() && Utility.IsHorizontalDirection(getGeneralDirectionTowards(who.getStandingPosition(), -10, opposite: false, useTileCalculations: false)))
		{
			who.Halt();
			who.faceGeneralDirection(getStandingPosition(), 0, opposite: false, useTileCalculations: false);
			who.kissFarmerEvent.Fire(UniqueMultiplayerID);
			Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(211, 428, 7, 6), 2000f, 1, 0, base.Tile * 64f + new Vector2(16f, -64f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
			{
				motion = new Vector2(0f, -0.5f),
				alphaFade = 0.01f
			});
			playNearbySoundAll("dwop", null, SoundContext.NPC);
			return true;
		}
		if (who != this)
		{
			if (StardewValley.Pokemon.PokemonPvPManager.HandleFarmerInteraction(who, this, location))
			{
				return true;
			}
		}
		return false;
	}

	public void Update(GameTime time, GameLocation location)
	{
		if (_lastEquippedTool != CurrentTool)
		{
			Equip(_lastEquippedTool, CurrentTool, delegate(Tool tool)
			{
				_lastEquippedTool = tool;
			});
		}
		buffs.SetOwner(this);
		buffs.Update(time);
		position.UpdateExtrapolation(getMovementSpeed());
		fireToolEvent.Poll();
		beginUsingToolEvent.Poll();
		endUsingToolEvent.Poll();
		drinkAnimationEvent.Poll();
		eatAnimationEvent.Poll();
		sickAnimationEvent.Poll();
		passOutEvent.Poll();
		doEmoteEvent.Poll();
		kissFarmerEvent.Poll();
		synchronizedJumpEvent.Poll();
		renovateEvent.Poll();
		if (IsLocalPlayer)
		{
			if (base.currentLocation == null)
			{
				return;
			}
			hidden.Value = IsDedicatedPlayer || localBusMoving() || (location.currentEvent != null && !location.currentEvent.isFestival) || (location.currentEvent != null && location.currentEvent.doingSecretSanta) || Game1.locationRequest != null || !Game1.displayFarmer;
			isInBed.Value = base.currentLocation.doesTileHaveProperty(base.TilePoint.X, base.TilePoint.Y, "Bed", "Back") != null || sleptInTemporaryBed.Value;
			if (!Game1.options.allowStowing)
			{
				netItemStowed.Value = false;
			}
			hasMenuOpen.Value = Game1.activeClickableMenu != null;
		}
		if (IsSitting())
		{
			movementDirections.Clear();
			if (IsSitting() && !isStopSitting)
			{
				if (!sittingFurniture.IsSeatHere(base.currentLocation))
				{
					StopSitting(animate: false);
				}
				else if (sittingFurniture is MapSeat mapSeat)
				{
					if (!base.currentLocation.mapSeats.Contains(sittingFurniture))
					{
						StopSitting(animate: false);
					}
					else if (mapSeat.IsBlocked(base.currentLocation))
					{
						StopSitting();
					}
				}
			}
		}
		if (Game1.CurrentEvent == null && !bathingClothes.Value && !onBridge.Value)
		{
			canOnlyWalk = false;
		}
		if (noMovementPause > 0)
		{
			CanMove = false;
			noMovementPause -= time.ElapsedGameTime.Milliseconds;
			if (noMovementPause <= 0)
			{
				CanMove = true;
			}
		}
		if (freezePause > 0)
		{
			CanMove = false;
			freezePause -= time.ElapsedGameTime.Milliseconds;
			if (freezePause <= 0)
			{
				CanMove = true;
			}
		}
		if (sparklingText != null && sparklingText.update(time))
		{
			sparklingText = null;
		}
		if (newLevelSparklingTexts.Count > 0 && sparklingText == null && !UsingTool && CanMove && Game1.activeClickableMenu == null)
		{
			sparklingText = new SparklingText(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2003", getSkillDisplayNameFromIndex(newLevelSparklingTexts.Peek())), Color.White, Color.White, rainbow: true);
			newLevelSparklingTexts.Dequeue();
		}
		if (lerpPosition >= 0f)
		{
			lerpPosition += (float)time.ElapsedGameTime.TotalSeconds;
			if (lerpPosition >= lerpDuration)
			{
				lerpPosition = lerpDuration;
			}
			base.Position = new Vector2(Utility.Lerp(lerpStartPosition.X, lerpEndPosition.X, lerpPosition / lerpDuration), Utility.Lerp(lerpStartPosition.Y, lerpEndPosition.Y, lerpPosition / lerpDuration));
			if (lerpPosition >= lerpDuration)
			{
				lerpPosition = -1f;
			}
		}
		if (isStopSitting && lerpPosition < 0f)
		{
			isStopSitting = false;
			if (sittingFurniture != null)
			{
				mapChairSitPosition.Value = new Vector2(-1f, -1f);
				sittingFurniture.RemoveSittingFarmer(this);
				sittingFurniture = null;
				isSitting.Value = false;
			}
		}
		if (isInBed.Value && Game1.IsMultiplayer && Game1.shouldTimePass())
		{
			regenTimer -= time.ElapsedGameTime.Milliseconds;
			if (regenTimer < 0)
			{
				regenTimer = 500;
				if (stamina < (float)MaxStamina)
				{
					stamina++;
				}
				if (health < maxHealth)
				{
					health++;
				}
			}
		}
		FarmerSprite.checkForSingleAnimation(time);
		if (CanMove)
		{
			rotation = 0f;
			if (health <= 0 && !Game1.killScreen && Game1.timeOfDay < 2600)
			{
				if (IsSitting())
				{
					StopSitting(animate: false);
				}
				CanMove = false;
				Game1.screenGlowOnce(Color.Red, hold: true);
				Game1.killScreen = true;
				faceDirection(2);
				FarmerSprite.setCurrentFrame(5);
				jitterStrength = 1f;
				Game1.pauseTime = 3000f;
				Rumble.rumbleAndFade(0.75f, 1500f);
				freezePause = 8000;
				if (Game1.currentSong != null && Game1.currentSong.IsPlaying)
				{
					Game1.currentSong.Stop(AudioStopOptions.Immediate);
				}
				Game1.changeMusicTrack("silence");
				playNearbySoundAll("death");
				Game1.dialogueUp = false;
				Game1.stats.TimesUnconscious++;
				if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && Game1.player.currentLocation is MineShaft && Game1.mine.getMineArea() == 121)
				{
					int num = 0;
					float num2 = 0.2f;
					if (Game1.player.team.calicoStatueEffects.ContainsKey(5))
					{
						num2 = 0.5f;
					}
					num = (int)(num2 * (float)Game1.player.getItemCount("CalicoEgg"));
					Game1.player.Items.ReduceId("CalicoEgg", num);
					itemsLostLastDeath.Clear();
					if (num > 0)
					{
						itemsLostLastDeath.Add(new Object("CalicoEgg", num));
					}
				}
				if (Game1.activeClickableMenu is GameMenu)
				{
					Game1.activeClickableMenu.emergencyShutDown();
					Game1.activeClickableMenu = null;
				}
			}
			if (collisionNPC != null)
			{
				collisionNPC.farmerPassesThrough = true;
			}
			NPC nPC;
			if (movementDirections.Count > 0 && !isRidingHorse() && (nPC = location.isCollidingWithCharacter(nextPosition(FacingDirection))) != null)
			{
				charactercollisionTimer += time.ElapsedGameTime.Milliseconds;
				if (charactercollisionTimer > nPC.getTimeFarmerMustPushBeforeStartShaking())
				{
					nPC.shake(50);
				}
				if (charactercollisionTimer >= nPC.getTimeFarmerMustPushBeforePassingThrough() && collisionNPC == null)
				{
					collisionNPC = nPC;
					if (collisionNPC.Name.Equals("Bouncer") && base.currentLocation != null && base.currentLocation.name.Equals("SandyHouse"))
					{
						collisionNPC.showTextAboveHead(Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2010"));
						collisionNPC = null;
						charactercollisionTimer = 0;
					}
					else if (collisionNPC.name.Equals("Henchman") && base.currentLocation != null && base.currentLocation.name.Equals("WitchSwamp"))
					{
						collisionNPC = null;
						charactercollisionTimer = 0;
					}
					else if (collisionNPC is Raccoon)
					{
						collisionNPC = null;
						charactercollisionTimer = 0;
					}
				}
			}
			else
			{
				charactercollisionTimer = 0;
				if (collisionNPC != null && location.isCollidingWithCharacter(nextPosition(FacingDirection)) == null)
				{
					collisionNPC.farmerPassesThrough = false;
					collisionNPC = null;
				}
			}
		}
		if (Game1.shouldTimePass())
		{
			MeleeWeapon.weaponsTypeUpdate(time);
		}
		if (!Game1.eventUp || movementDirections.Count <= 0 || base.currentLocation.currentEvent == null || base.currentLocation.currentEvent.playerControlSequence || (controller != null && controller.allowPlayerPathingInEvent))
		{
			lastPosition = base.Position;
			if (controller != null)
			{
				if (controller.update(time))
				{
					controller = null;
				}
			}
			else if (controller == null)
			{
				MovePosition(time, Game1.viewport, location);
			}
		}
		if (Game1.actionsWhenPlayerFree.Count > 0 && IsLocalPlayer && !IsBusyDoingSomething())
		{
			Action action = Game1.actionsWhenPlayerFree[0];
			Game1.actionsWhenPlayerFree.RemoveAt(0);
			action();
		}
		updateCommon(time, location);
		position.Paused = FarmerSprite.PauseForSingleAnimation || (UsingTool && !canStrafeForToolUse()) || isEating;
		checkDamage(location);
	}

	private void updateCommon(GameTime time, GameLocation location)
	{
		if (usernameDisplayTime > 0f)
		{
			usernameDisplayTime -= (float)time.ElapsedGameTime.TotalSeconds;
			if (usernameDisplayTime < 0f)
			{
				usernameDisplayTime = 0f;
			}
		}
		if (jitterStrength > 0f)
		{
			jitter = new Vector2((float)Game1.random.Next(-(int)(jitterStrength * 100f), (int)((jitterStrength + 1f) * 100f)) / 100f, (float)Game1.random.Next(-(int)(jitterStrength * 100f), (int)((jitterStrength + 1f) * 100f)) / 100f);
		}
		if (_wasSitting != isSitting.Value)
		{
			if (_wasSitting)
			{
				yOffset = 0f;
				xOffset = 0f;
			}
			_wasSitting = isSitting.Value;
		}
		if (yJumpOffset != 0)
		{
			yJumpVelocity -= ((UsingTool && canStrafeForToolUse() && (movementDirections.Count > 0 || (!IsLocalPlayer && IsRemoteMoving()))) ? 0.25f : 0.5f);
			yJumpOffset -= (int)yJumpVelocity;
			if (yJumpOffset >= 0)
			{
				yJumpOffset = 0;
				yJumpVelocity = 0f;
			}
		}
		updateMovementAnimation(time);
		updateEmote(time);
		updateGlow();
		currentLocationRef.Update();
		if (exhausted.Value && stamina <= 1f)
		{
			currentEyes = 4;
			blinkTimer = -1000;
		}
		blinkTimer += time.ElapsedGameTime.Milliseconds;
		if (blinkTimer > 2200 && Game1.random.NextDouble() < 0.01)
		{
			blinkTimer = -150;
			currentEyes = 4;
		}
		else if (blinkTimer > -100)
		{
			if (blinkTimer < -50)
			{
				currentEyes = 1;
			}
			else if (blinkTimer < 0)
			{
				currentEyes = 4;
			}
			else
			{
				currentEyes = 0;
			}
		}
		if (isCustomized.Value && isInBed.Value && !Game1.eventUp && ((timerSinceLastMovement >= 3000 && Game1.timeOfDay >= 630) || timeWentToBed.Value != 0))
		{
			currentEyes = 1;
			blinkTimer = -10;
		}
		UpdateItemStow();
		if (swimming.Value)
		{
			yOffset = (float)(Math.Cos(time.TotalGameTime.TotalMilliseconds / 2000.0) * 4.0);
			int num = swimTimer;
			swimTimer -= time.ElapsedGameTime.Milliseconds;
			if (timerSinceLastMovement == 0)
			{
				if (num > 400 && swimTimer <= 400 && IsLocalPlayer)
				{
					Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), 150f - (Math.Abs(xVelocity) + Math.Abs(yVelocity)) * 3f, 8, 0, new Vector2(base.Position.X, base.StandingPixel.Y - 32), flicker: false, Game1.random.NextBool(), 0.01f, 0.01f, Color.White, 1f, 0.003f, 0f, 0f));
				}
				if (swimTimer < 0)
				{
					swimTimer = 800;
					if (IsLocalPlayer)
					{
						playNearbySoundAll("slosh");
						Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), 150f - (Math.Abs(xVelocity) + Math.Abs(yVelocity)) * 3f, 8, 0, new Vector2(base.Position.X, base.StandingPixel.Y - 32), flicker: false, Game1.random.NextBool(), 0.01f, 0.01f, Color.White, 1f, 0.003f, 0f, 0f));
					}
				}
			}
			else if (!Game1.eventUp && (Game1.activeClickableMenu == null || Game1.IsMultiplayer) && !Game1.paused)
			{
				if (timerSinceLastMovement > 800)
				{
					currentEyes = 1;
				}
				else if (timerSinceLastMovement > 700)
				{
					currentEyes = 4;
				}
				if (swimTimer < 0)
				{
					swimTimer = 100;
					if (Stamina < (float)MaxStamina)
					{
						Stamina++;
					}
					if (health < maxHealth)
					{
						health++;
					}
				}
			}
		}
		if (!isMoving())
		{
			timerSinceLastMovement += time.ElapsedGameTime.Milliseconds;
		}
		else
		{
			timerSinceLastMovement = 0;
		}
		for (int num2 = Items.Count - 1; num2 >= 0; num2--)
		{
			if (Items[num2] is Tool tool)
			{
				tool.tickUpdate(time, this);
			}
		}
		if (TemporaryItem is Tool tool2)
		{
			tool2.tickUpdate(time, this);
		}
		rightRing.Value?.update(time, location, this);
		leftRing.Value?.update(time, location, this);
		if (Game1.shouldTimePass() && IsLocalPlayer)
		{
			foreach (Trinket trinketItem in trinketItems)
			{
				trinketItem?.Update(this, time, location);
			}
		}
		mount?.update(time, location);
		mount?.SyncPositionToRider();
		foreach (Companion companion in companions)
		{
			companion.Update(time, location);
		}
	}

	/// <summary>Get whether the player is engaged in any action and shouldn't be interrupted. This includes viewing a menu or event, fading to black, warping, using a tool, etc. If this returns false, we should be free to interrupt the player.</summary>
	public virtual bool IsBusyDoingSomething()
	{
		if (Game1.eventUp)
		{
			return true;
		}
		if (Game1.fadeToBlack)
		{
			return true;
		}
		if (Game1.currentMinigame != null)
		{
			return true;
		}
		if (Game1.activeClickableMenu != null)
		{
			return true;
		}
		if (Game1.isWarping)
		{
			return true;
		}
		if (UsingTool)
		{
			return true;
		}
		if (Game1.killScreen)
		{
			return true;
		}
		if (freezePause > 0)
		{
			return true;
		}
		if (!CanMove)
		{
			return true;
		}
		if (FarmerSprite.PauseForSingleAnimation)
		{
			return true;
		}
		if (usingSlingshot)
		{
			return true;
		}
		return false;
	}

	public void UpdateItemStow()
	{
		if (_itemStowed != netItemStowed.Value)
		{
			if (netItemStowed.Value && ActiveObject != null)
			{
				ActiveObject.actionWhenStopBeingHeld(this);
			}
			_itemStowed = netItemStowed.Value;
			if (!netItemStowed.Value)
			{
				ActiveObject?.actionWhenBeingHeld(this);
			}
		}
	}

	/// <summary>Add a quest to the player's quest log, or log a warning if it doesn't exist.</summary>
	/// <param name="questId">The quest ID in <c>Data/Quests</c>.</param>
	public void addQuest(string questId)
	{
		if (hasQuest(questId))
		{
			return;
		}
		Quest questFromId = Quest.getQuestFromId(questId);
		if (questFromId == null)
		{
			Game1.log.Warn("Can't add quest with ID '" + questId + "' because no such ID was found.");
			return;
		}
		questLog.Add(questFromId);
		if (!questFromId.IsHidden())
		{
			Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2011"), 2));
		}
		foreach (string constructedBuilding in Game1.player.team.constructedBuildings)
		{
			questFromId.OnBuildingExists(constructedBuilding);
		}
	}

	public void removeQuest(string questID)
	{
		questLog.RemoveWhere((Quest quest) => quest.id.Value == questID);
	}

	public void completeQuest(string questID)
	{
		for (int num = questLog.Count - 1; num >= 0; num--)
		{
			if (questLog[num].id.Value == questID)
			{
				questLog[num].questComplete();
			}
		}
	}

	public bool hasQuest(string id)
	{
		for (int num = questLog.Count - 1; num >= 0; num--)
		{
			if (questLog[num].id.Value == id)
			{
				return true;
			}
		}
		return false;
	}

	public bool hasNewQuestActivity()
	{
		foreach (SpecialOrder specialOrder in team.specialOrders)
		{
			if (!specialOrder.IsHidden() && (specialOrder.ShouldDisplayAsNew() || specialOrder.ShouldDisplayAsComplete()))
			{
				return true;
			}
		}
		foreach (Quest item in questLog)
		{
			if (!item.IsHidden() && (item.showNew.Value || (item.completed.Value && !item.destroy.Value)))
			{
				return true;
			}
		}
		return false;
	}

	public float getMovementSpeed()
	{
		if (UsingTool && canStrafeForToolUse())
		{
			return 2f;
		}
		float shoeSpeed = (Items != null && (Items.ContainsId("(O)Poke_RunningShoes") || Items.ContainsId("Poke_RunningShoes") || Items.ContainsId("(O)RunningShoes") || Items.ContainsId("RunningShoes"))) ? 1f : 0f;
		if (Game1.CurrentEvent == null || Game1.CurrentEvent.playerControlSequence)
		{
			movementMultiplier = 0.066f;
			float num = 1f;
			num = ((!isRidingHorse()) ? Math.Max(1f, ((float)base.speed + (Game1.eventUp ? 0f : (addedSpeed + temporarySpeedBuff + shoeSpeed))) * movementMultiplier * (float)Game1.currentGameTime.ElapsedGameTime.Milliseconds) : Math.Max(1f, ((float)base.speed + (Game1.eventUp ? 0f : (addedSpeed + 4.6f + (mount.ateCarrotToday ? 0.4f : 0f) + ((stats.Get("Book_Horse") != 0) ? 0.5f : 0f) + shoeSpeed))) * movementMultiplier * (float)Game1.currentGameTime.ElapsedGameTime.Milliseconds));
			if (movementDirections.Count > 1)
			{
				num *= 0.707f;
			}
			if (Game1.CurrentEvent == null && hasBuff("19"))
			{
				num = 0f;
			}
			return num;
		}
		float num2 = Math.Max(1f, (float)base.speed + (Game1.eventUp ? ((float)Math.Max(0, Game1.CurrentEvent.farmerAddedSpeed - 2)) : (addedSpeed + shoeSpeed + (isRidingHorse() ? 5f : temporarySpeedBuff))));
		if (movementDirections.Count > 1)
		{
			num2 = Math.Max(1, (int)Math.Sqrt(2f * (num2 * num2)) / 2);
		}
		return num2;
	}

	public bool isWearingRing(string itemId)
	{
		if (rightRing.Value == null || !rightRing.Value.GetsEffectOfRing(itemId))
		{
			if (leftRing.Value != null)
			{
				return leftRing.Value.GetsEffectOfRing(itemId);
			}
			return false;
		}
		return true;
	}

	public int getNumberOfWornRingsWithID(string itemId)
	{
		int num = 0;
		if (rightRing.Value != null && rightRing.Value.GetsEffectOfRing(itemId))
		{
			num++;
		}
		if (leftRing.Value != null && leftRing.Value.GetsEffectOfRing(itemId))
		{
			num++;
		}
		return num;
	}

	public override void Halt()
	{
		if (!FarmerSprite.PauseForSingleAnimation && !isRidingHorse() && !UsingTool)
		{
			base.Halt();
		}
		movementDirections.Clear();
		if (!isEmoteAnimating && !UsingTool)
		{
			stopJittering();
		}
		armOffset = Vector2.Zero;
		if (isRidingHorse())
		{
			mount.Halt();
			mount.Sprite.CurrentAnimation = null;
		}
		if (IsSitting())
		{
			ShowSitting();
		}
	}

	public void stopJittering()
	{
		jitterStrength = 0f;
		jitter = Vector2.Zero;
	}

	public override Microsoft.Xna.Framework.Rectangle nextPosition(int direction)
	{
		Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
		switch (direction)
		{
		case 0:
			boundingBox.Y -= (int)Math.Ceiling(getMovementSpeed());
			break;
		case 1:
			boundingBox.X += (int)Math.Ceiling(getMovementSpeed());
			break;
		case 2:
			boundingBox.Y += (int)Math.Ceiling(getMovementSpeed());
			break;
		case 3:
			boundingBox.X -= (int)Math.Ceiling(getMovementSpeed());
			break;
		}
		return boundingBox;
	}

	public Microsoft.Xna.Framework.Rectangle nextPositionHalf(int direction)
	{
		Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
		switch (direction)
		{
		case 0:
			boundingBox.Y -= (int)Math.Ceiling((double)getMovementSpeed() / 2.0);
			break;
		case 1:
			boundingBox.X += (int)Math.Ceiling((double)getMovementSpeed() / 2.0);
			break;
		case 2:
			boundingBox.Y += (int)Math.Ceiling((double)getMovementSpeed() / 2.0);
			break;
		case 3:
			boundingBox.X -= (int)Math.Ceiling((double)getMovementSpeed() / 2.0);
			break;
		}
		return boundingBox;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="skillType">e.g. farming, fishing, foraging</param>
	/// <param name="skillLevel">5 or 10</param>
	/// <returns></returns>
	public int getProfessionForSkill(int skillType, int skillLevel)
	{
		switch (skillLevel)
		{
		case 5:
			switch (skillType)
			{
			case 0:
				if (professions.Contains(0))
				{
					return 0;
				}
				if (professions.Contains(1))
				{
					return 1;
				}
				break;
			case 1:
				if (professions.Contains(6))
				{
					return 6;
				}
				if (professions.Contains(7))
				{
					return 7;
				}
				break;
			case 2:
				if (professions.Contains(12))
				{
					return 12;
				}
				if (professions.Contains(13))
				{
					return 13;
				}
				break;
			case 3:
				if (professions.Contains(18))
				{
					return 18;
				}
				if (professions.Contains(19))
				{
					return 19;
				}
				break;
			case 4:
				if (professions.Contains(24))
				{
					return 24;
				}
				if (professions.Contains(25))
				{
					return 25;
				}
				break;
			}
			break;
		case 10:
			switch (skillType)
			{
			case 0:
				if (professions.Contains(1))
				{
					if (professions.Contains(4))
					{
						return 4;
					}
					if (professions.Contains(5))
					{
						return 5;
					}
				}
				else
				{
					if (professions.Contains(2))
					{
						return 2;
					}
					if (professions.Contains(3))
					{
						return 3;
					}
				}
				break;
			case 1:
				if (professions.Contains(6))
				{
					if (professions.Contains(8))
					{
						return 8;
					}
					if (professions.Contains(9))
					{
						return 9;
					}
				}
				else
				{
					if (professions.Contains(10))
					{
						return 10;
					}
					if (professions.Contains(11))
					{
						return 11;
					}
				}
				break;
			case 2:
				if (professions.Contains(12))
				{
					if (professions.Contains(14))
					{
						return 14;
					}
					if (professions.Contains(15))
					{
						return 15;
					}
				}
				else
				{
					if (professions.Contains(16))
					{
						return 16;
					}
					if (professions.Contains(17))
					{
						return 17;
					}
				}
				break;
			case 3:
				if (professions.Contains(18))
				{
					if (professions.Contains(20))
					{
						return 20;
					}
					if (professions.Contains(21))
					{
						return 21;
					}
				}
				else
				{
					if (professions.Contains(23))
					{
						return 23;
					}
					if (professions.Contains(22))
					{
						return 22;
					}
				}
				break;
			case 4:
				if (professions.Contains(24))
				{
					if (professions.Contains(26))
					{
						return 26;
					}
					if (professions.Contains(27))
					{
						return 27;
					}
				}
				else
				{
					if (professions.Contains(28))
					{
						return 28;
					}
					if (professions.Contains(29))
					{
						return 29;
					}
				}
				break;
			}
			break;
		}
		return -1;
	}

	public void behaviorOnMovement(int direction)
	{
		hasMoved = true;
	}

	public void OnEmoteAnimationEnd(Farmer farmer)
	{
		if (farmer == this && isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
	}

	public void EndEmoteAnimation()
	{
		if (isEmoteAnimating)
		{
			if (jitterStrength > 0f)
			{
				stopJittering();
			}
			if (yJumpOffset != 0)
			{
				yJumpOffset = 0;
				yJumpVelocity = 0f;
			}
			FarmerSprite.PauseForSingleAnimation = false;
			FarmerSprite.StopAnimation();
			isEmoteAnimating = false;
		}
	}

	private void broadcastHaltAnimation(Farmer who)
	{
		if (IsLocalPlayer)
		{
			haltAnimationEvent.Fire();
		}
		else
		{
			completelyStopAnimating(who);
		}
	}

	private void performHaltAnimation()
	{
		completelyStopAnimatingOrDoingAction();
	}

	public void performKissFarmer(long otherPlayerID)
	{
		Farmer player = Game1.GetPlayer(otherPlayerID);
		if (player != null)
		{
			bool flag = base.StandingPixel.X < player.StandingPixel.X;
			PerformKiss(flag ? 1 : 3);
			player.PerformKiss((!flag) ? 1 : 3);
		}
	}

	public void PerformKiss(int facingDirection)
	{
		if (!Game1.eventUp && !UsingTool && (!IsLocalPlayer || Game1.activeClickableMenu == null) && !isRidingHorse() && !IsSitting() && !base.IsEmoting && CanMove)
		{
			CanMove = false;
			FarmerSprite.PauseForSingleAnimation = false;
			faceDirection(facingDirection);
			FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[2]
			{
				new FarmerSprite.AnimationFrame(101, 1000, 0, secondaryArm: false, FacingDirection == 3),
				new FarmerSprite.AnimationFrame(6, 1, secondaryArm: false, FacingDirection == 3, broadcastHaltAnimation)
			});
			if (!Stats.AllowRetroactiveAchievements)
			{
				Game1.stats.checkForFullHouseAchievement(isDirectUnlock: true);
			}
		}
	}

	public override void MovePosition(GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation currentLocation)
	{
		if (IsSitting())
		{
			return;
		}
		if (Game1.CurrentEvent == null || Game1.CurrentEvent.playerControlSequence)
		{
			if (Game1.shouldTimePass() && temporarilyInvincible)
			{
				if (temporaryInvincibilityTimer < 0)
				{
					currentTemporaryInvincibilityDuration = 1200;
				}
				temporaryInvincibilityTimer += time.ElapsedGameTime.Milliseconds;
				if (temporaryInvincibilityTimer > currentTemporaryInvincibilityDuration)
				{
					temporarilyInvincible = false;
					temporaryInvincibilityTimer = 0;
				}
			}
		}
		else if (temporarilyInvincible)
		{
			temporarilyInvincible = false;
			temporaryInvincibilityTimer = 0;
		}
		if (Game1.activeClickableMenu != null && (Game1.CurrentEvent == null || Game1.CurrentEvent.playerControlSequence))
		{
			return;
		}
		if (isRafting)
		{
			moveRaft(currentLocation, time);
			return;
		}
		if (xVelocity != 0f || yVelocity != 0f)
		{
			if (double.IsNaN(xVelocity) || double.IsNaN(yVelocity))
			{
				xVelocity = 0f;
				yVelocity = 0f;
			}
			Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
			Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(boundingBox.X + (int)Math.Floor(xVelocity), boundingBox.Y - (int)Math.Floor(yVelocity), boundingBox.Width, boundingBox.Height);
			Microsoft.Xna.Framework.Rectangle value2 = new Microsoft.Xna.Framework.Rectangle(boundingBox.X + (int)Math.Ceiling(xVelocity), boundingBox.Y - (int)Math.Ceiling(yVelocity), boundingBox.Width, boundingBox.Height);
			Microsoft.Xna.Framework.Rectangle rectangle = Microsoft.Xna.Framework.Rectangle.Union(value, value2);
			if (!currentLocation.isCollidingPosition(rectangle, viewport, isFarmer: true, -1, glider: false, this))
			{
				position.X += xVelocity;
				position.Y -= yVelocity;
				xVelocity -= xVelocity / 16f;
				yVelocity -= yVelocity / 16f;
				if (Math.Abs(xVelocity) <= 0.05f)
				{
					xVelocity = 0f;
				}
				if (Math.Abs(yVelocity) <= 0.05f)
				{
					yVelocity = 0f;
				}
			}
			else
			{
				xVelocity -= xVelocity / 16f;
				yVelocity -= yVelocity / 16f;
				if (Math.Abs(xVelocity) <= 0.05f)
				{
					xVelocity = 0f;
				}
				if (Math.Abs(yVelocity) <= 0.05f)
				{
					yVelocity = 0f;
				}
			}
		}
		if (CanMove || Game1.eventUp || controller != null || canStrafeForToolUse())
		{
			temporaryPassableTiles.ClearNonIntersecting(GetBoundingBox());
			float movementSpeed = getMovementSpeed();
			temporarySpeedBuff = 0f;
			if ((movementDirections.Contains(0) && MovePositionImpl(0, 0f, 0f - movementSpeed, time, viewport)) || (movementDirections.Contains(2) && MovePositionImpl(2, 0f, movementSpeed, time, viewport)) || (movementDirections.Contains(1) && MovePositionImpl(1, movementSpeed, 0f, time, viewport)) || (movementDirections.Contains(3) && MovePositionImpl(3, 0f - movementSpeed, 0f, time, viewport)))
			{
				return;
			}
		}
		if (movementDirections.Count > 0 && !UsingTool)
		{
			FarmerSprite.intervalModifier = 1f - (running ? 0.0255f : 0.025f) * (Math.Max(1f, ((float)base.speed + (Game1.eventUp ? 0f : ((float)(int)addedSpeed + (isRidingHorse() ? 4.6f : 0f)))) * movementMultiplier * (float)Game1.currentGameTime.ElapsedGameTime.Milliseconds) * 1.25f);
		}
		else
		{
			FarmerSprite.intervalModifier = 1f;
		}
		if (currentLocation != null && currentLocation.isFarmerCollidingWithAnyCharacter())
		{
			temporaryPassableTiles.Add(new Microsoft.Xna.Framework.Rectangle(base.TilePoint.X * 64, base.TilePoint.Y * 64, 64, 64));
		}
	}

	public bool canStrafeForToolUse()
	{
		if (toolHold.Value != 0 && canReleaseTool)
		{
			if (toolPower.Value < 1)
			{
				return toolHoldStartTime.Value - toolHold.Value > 150;
			}
			return true;
		}
		return false;
	}

	/// <summary>Handle a player's movement in a specific direction, after the game has already checked whether movement is allowed.</summary>
	/// <param name="direction">The direction the player is moving in, matching a constant like <see cref="F:StardewValley.Game1.up" />.</param>
	/// <param name="movementSpeedX">The player's movement speed along the X axis for this direction.</param>
	/// <param name="movementSpeedY">The player's movement speed along the Y axis for this direction.</param>
	/// <param name="time">The elapsed game time.</param>
	/// <param name="viewport">The pixel area being viewed relative to the top-left corner of the map.</param>
	/// <returns>Returns whether the movement was fully handled (e.g. a warp was activated), so no further movement logic should be applied.</returns>
	protected virtual bool MovePositionImpl(int direction, float movementSpeedX, float movementSpeedY, GameTime time, xTile.Dimensions.Rectangle viewport)
	{
		Microsoft.Xna.Framework.Rectangle rectangle = nextPosition(direction);
		Warp warp = Game1.currentLocation.isCollidingWithWarp(rectangle, this);
		if (warp != null && IsLocalPlayer)
		{
			if (Game1.eventUp && !((!(Game1.CurrentEvent?.isFestival)) ?? true))
			{
				Game1.CurrentEvent.TryStartEndFestivalDialogue(this);
			}
			else
			{
				warpFarmer(warp, direction);
			}
			return true;
		}
		bool flag = Game1.eventUp && !(Game1.CurrentEvent?.isFestival ?? true) && ((!(Game1.CurrentEvent?.playerControlSequence)) ?? false);
		if (!base.currentLocation.isCollidingPosition(rectangle, viewport, isFarmer: true, 0, glider: false, this) || ignoreCollisions || flag)
		{
			position.X += movementSpeedX;
			position.Y += movementSpeedY;
			behaviorOnMovement(direction);
			return false;
		}
		if (!base.currentLocation.isCollidingPosition(nextPositionHalf(direction), viewport, isFarmer: true, 0, glider: false, this))
		{
			position.X += movementSpeedX / 2f;
			position.Y += movementSpeedY / 2f;
			behaviorOnMovement(direction);
			return false;
		}
		if (movementDirections.Count == 1)
		{
			Microsoft.Xna.Framework.Rectangle rectangle2 = rectangle;
			if (direction == 0 || direction == 2)
			{
				rectangle2.Width /= 4;
				bool flag2 = base.currentLocation.isCollidingPosition(rectangle2, viewport, isFarmer: true, 0, glider: false, this);
				rectangle2.X += rectangle2.Width * 3;
				bool flag3 = base.currentLocation.isCollidingPosition(rectangle2, viewport, isFarmer: true, 0, glider: false, this);
				if (flag2 && !flag3 && !base.currentLocation.isCollidingPosition(nextPosition(1), viewport, isFarmer: true, 0, glider: false, this))
				{
					position.X += (float)base.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
				}
				else if (flag3 && !flag2 && !base.currentLocation.isCollidingPosition(nextPosition(3), viewport, isFarmer: true, 0, glider: false, this))
				{
					position.X -= (float)base.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
				}
			}
			else
			{
				rectangle2.Height /= 4;
				bool flag4 = base.currentLocation.isCollidingPosition(rectangle2, viewport, isFarmer: true, 0, glider: false, this);
				rectangle2.Y += rectangle2.Height * 3;
				bool flag5 = base.currentLocation.isCollidingPosition(rectangle2, viewport, isFarmer: true, 0, glider: false, this);
				if (flag4 && !flag5 && !base.currentLocation.isCollidingPosition(nextPosition(2), viewport, isFarmer: true, 0, glider: false, this))
				{
					position.Y += (float)base.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
				}
				else if (flag5 && !flag4 && !base.currentLocation.isCollidingPosition(nextPosition(0), viewport, isFarmer: true, 0, glider: false, this))
				{
					position.Y -= (float)base.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
				}
			}
		}
		return false;
	}

	public void updateMovementAnimation(GameTime time)
	{
		if (_emoteGracePeriod > 0)
		{
			_emoteGracePeriod -= time.ElapsedGameTime.Milliseconds;
		}
		if (isEmoteAnimating && (((IsLocalPlayer ? (movementDirections.Count > 0) : IsRemoteMoving()) && _emoteGracePeriod <= 0) || !FarmerSprite.PauseForSingleAnimation))
		{
			EndEmoteAnimation();
		}
		bool flag = IsCarrying();
		if (!isRidingHorse())
		{
			xOffset = 0f;
		}
		if (CurrentTool is FishingRod fishingRod && (fishingRod.isTimingCast || fishingRod.isCasting))
		{
			fishingRod.setTimingCastAnimation(this);
			return;
		}
		if (FarmerSprite.PauseForSingleAnimation || UsingTool)
		{
			if (UsingTool && canStrafeForToolUse() && (movementDirections.Count > 0 || (!IsLocalPlayer && IsRemoteMoving())) && yJumpOffset == 0)
			{
				jumpWithoutSound(2.5f);
			}
			return;
		}
		if (IsSitting())
		{
			ShowSitting();
			return;
		}
		if (IsLocalPlayer && !CanMove && !Game1.eventUp)
		{
			if (isRidingHorse() && mount != null && !isAnimatingMount)
			{
				showRiding();
			}
			else if (flag)
			{
				showCarrying();
			}
			return;
		}
		if (IsLocalPlayer || isFakeEventActor)
		{
			moveUp = movementDirections.Contains(0);
			moveRight = movementDirections.Contains(1);
			moveDown = movementDirections.Contains(2);
			moveLeft = movementDirections.Contains(3);
			if (moveLeft)
			{
				FacingDirection = 3;
			}
			else if (moveRight)
			{
				FacingDirection = 1;
			}
			else if (moveUp)
			{
				FacingDirection = 0;
			}
			else if (moveDown)
			{
				FacingDirection = 2;
			}
			if (isRidingHorse() && !mount.dismounting.Value)
			{
				base.speed = 2;
			}
		}
		else
		{
			moveLeft = IsRemoteMoving() && FacingDirection == 3;
			moveRight = IsRemoteMoving() && FacingDirection == 1;
			moveUp = IsRemoteMoving() && FacingDirection == 0;
			moveDown = IsRemoteMoving() && FacingDirection == 2;
			bool num = moveUp || moveRight || moveDown || moveLeft;
			float num2 = position.CurrentInterpolationSpeed() / ((float)Game1.currentGameTime.ElapsedGameTime.Milliseconds * 0.066f);
			running = Math.Abs(num2 - 5f) < Math.Abs(num2 - 2f) && !bathingClothes.Value && !onBridge.Value;
			if (!num)
			{
				FarmerSprite.StopAnimation();
			}
		}
		if (hasBuff("19"))
		{
			running = false;
			moveUp = false;
			moveDown = false;
			moveLeft = false;
			moveRight = false;
		}
		if (!FarmerSprite.PauseForSingleAnimation && !UsingTool)
		{
			if (isRidingHorse() && !mount.dismounting.Value)
			{
				showRiding();
			}
			else if (moveLeft && running && !flag)
			{
				FarmerSprite.animate(56, time);
			}
			else if (moveRight && running && !flag)
			{
				FarmerSprite.animate(40, time);
			}
			else if (moveUp && running && !flag)
			{
				FarmerSprite.animate(48, time);
			}
			else if (moveDown && running && !flag)
			{
				FarmerSprite.animate(32, time);
			}
			else if (moveLeft && running)
			{
				FarmerSprite.animate(152, time);
			}
			else if (moveRight && running)
			{
				FarmerSprite.animate(136, time);
			}
			else if (moveUp && running)
			{
				FarmerSprite.animate(144, time);
			}
			else if (moveDown && running)
			{
				FarmerSprite.animate(128, time);
			}
			else if (moveLeft && !flag)
			{
				FarmerSprite.animate(24, time);
			}
			else if (moveRight && !flag)
			{
				FarmerSprite.animate(8, time);
			}
			else if (moveUp && !flag)
			{
				FarmerSprite.animate(16, time);
			}
			else if (moveDown && !flag)
			{
				FarmerSprite.animate(0, time);
			}
			else if (moveLeft)
			{
				FarmerSprite.animate(120, time);
			}
			else if (moveRight)
			{
				FarmerSprite.animate(104, time);
			}
			else if (moveUp)
			{
				FarmerSprite.animate(112, time);
			}
			else if (moveDown)
			{
				FarmerSprite.animate(96, time);
			}
			else if (flag)
			{
				showCarrying();
			}
			else
			{
				showNotCarrying();
			}
		}
	}

	public bool IsCarrying()
	{
		if (mount != null || isAnimatingMount)
		{
			return false;
		}
		if (IsSitting())
		{
			return false;
		}
		if (onBridge.Value)
		{
			return false;
		}
		if (ActiveObject == null || Game1.eventUp || Game1.killScreen)
		{
			return false;
		}
		if (!ActiveObject.IsHeldOverHead())
		{
			return false;
		}
		return true;
	}

	public void doneEating()
	{
		isEating = false;
		tempFoodItemTextureName.Value = null;
		completelyStopAnimatingOrDoingAction();
		forceCanMove();
		if (mostRecentlyGrabbedItem == null || !IsLocalPlayer)
		{
			return;
		}
		Object obj = itemToEat as Object;
		if (obj.QualifiedItemId == "(O)434")
		{
			Game1.stats.checkForStardropAchievement(isDirectUnlock: true);
			yOffset = 0f;
			yJumpOffset = 0;
			Game1.changeMusicTrack("none");
			Game1.playSound("stardrop");
			string text = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs." + Game1.random.Choose("3094", "3095"));
			if (favoriteThing.Value != null)
			{
				text = (favoriteThing.Value.Contains("Stardew") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3097") : ((!favoriteThing.Equals("ConcernedApe")) ? (text + favoriteThing.Value) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3099")));
			}
			DelayedAction.showDialogueAfterDelay(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3100") + text + Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3101"), 6000);
			maxStamina.Value += 34;
			stamina = MaxStamina;
			FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[1]
			{
				new FarmerSprite.AnimationFrame(57, 6000)
			});
			startGlowing(new Color(200, 0, 255), border: false, 0.1f);
			jitterStrength = 1f;
			Game1.staminaShakeTimer = 12000;
			Game1.screenGlowOnce(new Color(200, 0, 255), hold: true);
			CanMove = false;
			freezePause = 8000;
			base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(368, 16, 16, 16), 60f, 8, 40, base.Position + new Vector2(-8f, -128f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0.0075f, 0f, 0f)
			{
				alpha = 0.75f,
				alphaFade = 0.0025f,
				motion = new Vector2(0f, -0.25f)
			});
			if (Game1.displayHUD && !Game1.eventUp)
			{
				for (int i = 0; i < 40; i++)
				{
					Game1.uiOverlayTempSprites.Add(new TemporaryAnimatedSprite(Game1.random.Next(10, 12), new Vector2((float)Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right / Game1.options.uiScale - 48f - 8f - (float)Game1.random.Next(64), (float)Game1.random.Next(-64, 64) + (float)Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom / Game1.options.uiScale - 224f - 16f - (float)(int)((double)(MaxStamina - 270) * 0.715)), Game1.random.Choose(Color.White, Color.Lime), 8, flipped: false, 50f)
					{
						layerDepth = 1f,
						delayBeforeAnimationStart = 200 * i,
						interval = 100f,
						local = true
					});
				}
			}
			Point tilePoint = base.TilePoint;
			Utility.addSprinklesToLocation(base.currentLocation, tilePoint.X, tilePoint.Y, 9, 9, 6000, 100, new Color(200, 0, 255), null, motionTowardCenter: true);
			DelayedAction.stopFarmerGlowing(6000);
			Utility.addSprinklesToLocation(base.currentLocation, tilePoint.X, tilePoint.Y, 9, 9, 6000, 300, Color.Cyan, null, motionTowardCenter: true);
			mostRecentlyGrabbedItem = null;
		}
		else
		{
			if (obj.HasContextTag("ginger_item"))
			{
				buffs.Remove("25");
			}
			foreach (Buff foodOrDrinkBuff in obj.GetFoodOrDrinkBuffs())
			{
				applyBuff(foodOrDrinkBuff);
			}
			switch (obj.QualifiedItemId)
			{
			case "(O)773":
				health = maxHealth;
				break;
			case "(O)351":
				exhausted.Value = false;
				break;
			case "(O)349":
				Stamina = MaxStamina;
				break;
			}
			float num = Stamina;
			int num2 = health;
			int num3 = obj.staminaRecoveredOnConsumption();
			int num4 = obj.healthRecoveredOnConsumption();
			if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && base.currentLocation is MineShaft && Game1.mine.getMineArea() == 121 && team.calicoStatueEffects.ContainsKey(6))
			{
				num3 = Math.Max(1, num3 / 2);
				num4 = Math.Max(1, num4 / 2);
			}
			Stamina = Math.Min(MaxStamina, Stamina + (float)num3);
			health = Math.Min(maxHealth, health + num4);
			if (num < Stamina)
			{
				Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3116", (int)(Stamina - num)), 4));
			}
			if (num2 < health)
			{
				Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3118", health - num2), 5));
			}
		}
		if (obj != null && obj.Edibility < 0)
		{
			CanMove = false;
			sickAnimationEvent.Fire();
		}
	}

	/// <summary>Perform an action for each active non-completed quest in the local player's log.</summary>
	/// <param name="check">The action to perform on the quest. This should return whether the quest state changed.</param>
	/// <param name="onlyOneQuest">Whether to stop after any quest is updated.</param>
	/// <returns>Returns whether a quest was updated (e.g. closer to completion).</returns>
	public virtual bool NotifyQuests(Func<Quest, bool> check, bool onlyOneQuest = false)
	{
		bool result = false;
		for (int num = questLog.Count - 1; num >= 0; num--)
		{
			Quest quest = questLog[num];
			if (!quest.completed.Value)
			{
				if (quest == null)
				{
					questLog.RemoveAt(num);
				}
				else if (check(quest))
				{
					result = true;
					if (onlyOneQuest)
					{
						break;
					}
				}
			}
		}
		return result;
	}

	public virtual void AddCompanion(Companion companion)
	{
		if (!companions.Contains(companion))
		{
			companion.InitializeCompanion(this);
			companions.Add(companion);
		}
	}

	public virtual void RemoveCompanion(Companion companion)
	{
		if (companions.Contains(companion))
		{
			companions.Remove(companion);
			companion.CleanupCompanion();
		}
	}

	public static void completelyStopAnimating(Farmer who)
	{
		who.completelyStopAnimatingOrDoingAction();
	}

	public void completelyStopAnimatingOrDoingAction()
	{
		CanMove = !Game1.eventUp;
		if (isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
		if (UsingTool)
		{
			EndUsingTool();
			if (CurrentTool is FishingRod fishingRod)
			{
				fishingRod.resetState();
			}
		}
		if (usingSlingshot && CurrentTool is Slingshot slingshot)
		{
			slingshot.finish();
		}
		UsingTool = false;
		isEating = false;
		FarmerSprite.PauseForSingleAnimation = false;
		usingSlingshot = false;
		canReleaseTool = false;
		Halt();
		Sprite.StopAnimation();
		if (CurrentTool is MeleeWeapon meleeWeapon)
		{
			meleeWeapon.isOnSpecial = false;
		}
		stopJittering();
	}

	public void doEmote(int whichEmote)
	{
		if (!Game1.eventUp && !isEmoting)
		{
			isEmoting = true;
			currentEmote = whichEmote;
			currentEmoteFrame = 0;
			emoteInterval = 0f;
		}
	}

	public void performTenMinuteUpdate()
	{
	}

	public void setRunning(bool isRunning, bool force = false)
	{
		if (canOnlyWalk || (bathingClothes.Value && !running) || (Game1.CurrentEvent != null && isRunning && !Game1.CurrentEvent.isFestival && !Game1.CurrentEvent.playerControlSequence && (controller == null || !controller.allowPlayerPathingInEvent)))
		{
			return;
		}
		if (isRidingHorse())
		{
			running = true;
		}
		else if (stamina <= 0f)
		{
			base.speed = 2;
			if (running)
			{
				Halt();
			}
			running = false;
		}
		else if (force || (CanMove && !isEating && Game1.currentLocation != null && (Game1.currentLocation.currentEvent == null || Game1.currentLocation.currentEvent.playerControlSequence) && (isRunning || !UsingTool) && (Sprite == null || !((FarmerSprite)Sprite).PauseForSingleAnimation)))
		{
			running = isRunning;
			if (running)
			{
				base.speed = 5;
			}
			else
			{
				base.speed = 2;
			}
		}
		else if (UsingTool)
		{
			running = isRunning;
			if (running)
			{
				base.speed = 5;
			}
			else
			{
				base.speed = 2;
			}
		}
	}

	public void addSeenResponse(string id)
	{
		dialogueQuestionsAnswered.Add(id);
	}

	public void eatObject(Object o, bool overrideFullness = false)
	{
		if (o?.QualifiedItemId == "(O)434")
		{
			Game1.MusicDuckTimer = 10000f;
			Game1.changeMusicTrack("none");
			Game1.multiplayer.globalChatInfoMessage("Stardrop", base.Name);
		}
		if (getFacingDirection() != 2)
		{
			faceDirection(2);
		}
		itemToEat = o;
		mostRecentlyGrabbedItem = o;
		forceCanMove();
		completelyStopAnimatingOrDoingAction();
		if (Game1.objectData.TryGetValue(o.ItemId, out var value) && value.IsDrink)
		{
			if (IsLocalPlayer && hasBuff("7") && !overrideFullness)
			{
				Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2898")));
				return;
			}
			drinkAnimationEvent.Fire(o.getOne() as Object);
		}
		else if (o.Edibility != -300)
		{
			if (hasBuff("6") && !overrideFullness)
			{
				Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2899")));
				return;
			}
			eatAnimationEvent.Fire(o.getOne() as Object);
		}
		freezePause = 20000;
		CanMove = false;
		isEating = true;
	}

	/// <inheritdoc />
	public override void DrawShadow(SpriteBatch b)
	{
		float layerDepth = getDrawLayer() - 1E-06f;
		b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(GetShadowOffset() + base.Position + new Vector2(32f, 24f)), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f - (((running || UsingTool) && FarmerSprite.currentAnimationIndex > 1) ? ((float)Math.Abs(FarmerRenderer.featureYOffsetPerFrame[FarmerSprite.CurrentFrame]) * 0.5f) : 0f), SpriteEffects.None, layerDepth);
	}

	private void performDrinkAnimation(Object item)
	{
		if (isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
		if (!IsLocalPlayer)
		{
			itemToEat = item;
		}
		FarmerSprite.animateOnce(294, 80f, 8);
		isEating = true;
		if (item != null && item.HasContextTag("mayo_item") && Utility.isThereAFarmerOrCharacterWithinDistance(base.Tile, 7, base.currentLocation) is NPC { Age: not 2 } nPC)
		{
			int num = Game1.random.Next(3);
			if (nPC.Manners == 2 || nPC.SocialAnxiety == 1)
			{
				num = 3;
			}
			if (nPC.Name == "Emily" || nPC.Name == "Sandy" || nPC.Name == "Linus" || (nPC.Name == "Krobus" && item.QualifiedItemId == "(O)308"))
			{
				num = 4;
			}
			else if (nPC.Name == "Krobus" || nPC.Name == "Dwarf" || nPC is Monster || nPC is Horse || nPC is Pet || nPC is Child)
			{
				return;
			}
			nPC.showTextAboveHead(Game1.content.LoadString("Strings\\1_6_Strings:Mayo_reaction" + num), null, 2, 3000, 500);
			nPC.faceTowardFarmerForPeriod(1500, 7, faceAway: false, this);
		}
	}

	public Farmer CreateFakeEventFarmer()
	{
		Farmer farmer = new Farmer(new FarmerSprite(FarmerSprite.textureName.Value), new Vector2(192f, 192f), 1, "", new List<Item>(), IsMale);
		farmer.Name = base.Name;
		farmer.displayName = displayName;
		farmer.isFakeEventActor = true;
		farmer.changeGender(IsMale);
		farmer.changeHairStyle(hair.Value);
		farmer.UniqueMultiplayerID = UniqueMultiplayerID;
		farmer.shirtItem.Set(shirtItem.Value);
		farmer.pantsItem.Set(pantsItem.Value);
		farmer.shirt.Set(shirt.Value);
		farmer.pants.Set(pants.Value);
		foreach (Trinket trinketItem in trinketItems)
		{
			farmer.trinketItems.Add((Trinket)(trinketItem?.getOne()));
		}
		farmer.changeShoeColor(shoes.Value);
		farmer.boots.Set(boots.Value);
		farmer.leftRing.Set(leftRing.Value);
		farmer.rightRing.Set(rightRing.Value);
		farmer.hat.Set(hat.Value);
		farmer.pantsColor.Set(pantsColor.Value);
		farmer.changeHairColor(hairstyleColor.Value);
		farmer.changeSkinColor(skin.Value);
		farmer.accessory.Set(accessory.Value);
		farmer.changeEyeColor(newEyeColor.Value);
		farmer.UpdateClothing();
		return farmer;
	}

	private void performEatAnimation(Object item)
	{
		if (isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
		if (!IsLocalPlayer)
		{
			itemToEat = item;
		}
		FarmerSprite.animateOnce(216, 80f, 8);
		isEating = true;
	}

	public void netDoEmote(string emote_type)
	{
		doEmoteEvent.Fire(emote_type);
	}

	private void performSickAnimation()
	{
		if (isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
		isEating = false;
		FarmerSprite.animateOnce(224, 350f, 4);
		doEmote(12);
	}

	public void eatHeldObject()
	{
		if (isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
		if (Game1.fadeToBlack)
		{
			return;
		}
		Item value = null;
		int value2 = 0;
		bool flag = false;
		bool flag2 = false;
		if (ActiveItem == null || ActiveItem != mostRecentlyGrabbedItem)
		{
			if (netItemStowed.Value)
			{
				flag2 = true;
				netItemStowed.Value = false;
				UpdateItemStow();
			}
			if (ActiveItem == null)
			{
				ActiveItem = mostRecentlyGrabbedItem;
			}
			else if (ActiveItem != mostRecentlyGrabbedItem)
			{
				value2 = currentToolIndex.Value;
				if (currentToolIndex.Value < 0 || currentToolIndex.Value >= Items.Count)
				{
					currentToolIndex.Value = 0;
				}
				value = Items[currentToolIndex.Value];
				Items[currentToolIndex.Value] = mostRecentlyGrabbedItem;
				OnItemReceived(mostRecentlyGrabbedItem, mostRecentlyGrabbedItem.Stack, null);
				flag = true;
			}
		}
		eatObject(ActiveObject);
		if (isEating)
		{
			reduceActiveItemByOne();
			CanMove = false;
		}
		if (flag)
		{
			Items[currentToolIndex.Value] = value;
			currentToolIndex.Value = value2;
		}
		if (flag2)
		{
			netItemStowed.Value = true;
			UpdateItemStow();
		}
	}

	public void grabObject(Object obj)
	{
		if (isEmoteAnimating)
		{
			EndEmoteAnimation();
		}
		if (obj != null)
		{
			CanMove = false;
			switch (FacingDirection)
			{
			case 2:
				((FarmerSprite)Sprite).animateOnce(64, 50f, 8);
				break;
			case 1:
				((FarmerSprite)Sprite).animateOnce(72, 50f, 8);
				break;
			case 0:
				((FarmerSprite)Sprite).animateOnce(80, 50f, 8);
				break;
			case 3:
				((FarmerSprite)Sprite).animateOnce(88, 50f, 8);
				break;
			}
			Game1.playSound("pickUpItem");
		}
	}

	public virtual void PlayFishBiteChime()
	{
		int num = biteChime.Value;
		if (num < 0)
		{
			num = Game1.game1.instanceIndex;
		}
		if (num > 3)
		{
			num = 3;
		}
		if (num == 0)
		{
			playNearbySoundLocal("fishBite");
		}
		else
		{
			playNearbySoundLocal("fishBite_alternate_" + (num - 1));
		}
	}

	public string getTitle()
	{
		int level = Level;
		if (level >= 30)
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2016");
		}
		switch (level)
		{
		case 29:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2017");
		case 27:
		case 28:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2018");
		case 25:
		case 26:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2019");
		case 23:
		case 24:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2020");
		case 21:
		case 22:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2021");
		case 19:
		case 20:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2022");
		case 17:
		case 18:
			if (!IsMale)
			{
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2024");
			}
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2023");
		case 15:
		case 16:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2025");
		case 13:
		case 14:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2026");
		case 11:
		case 12:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2027");
		case 9:
		case 10:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2028");
		case 7:
		case 8:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2029");
		case 5:
		case 6:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2030");
		case 3:
		case 4:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2031");
		default:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.2032");
		}
	}

	public void queueMessage(byte messageType, Farmer sourceFarmer, params object[] data)
	{
		queueMessage(new OutgoingMessage(messageType, sourceFarmer, data));
	}

	public void queueMessage(OutgoingMessage message)
	{
		messageQueue.Add(message);
	}
}
