using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley.Audio;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Characters;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.Quests;
using StardewValley.SpecialOrders;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using xTile.Dimensions;

namespace StardewValley;

[XmlInclude(typeof(Cat))]
[XmlInclude(typeof(Child))]
[XmlInclude(typeof(Dog))]
[XmlInclude(typeof(Horse))]
[XmlInclude(typeof(Junimo))]
[XmlInclude(typeof(JunimoHarvester))]
[XmlInclude(typeof(Pet))]
[XmlInclude(typeof(TrashBear))]
[XmlInclude(typeof(Raccoon))]
[XmlInclude(typeof(Monster))]
public class NPC : Character, IComparable
{
	public const int minimum_square_pause = 6000;

	public const int maximum_square_pause = 12000;

	public const int portrait_width = 64;

	public const int portrait_height = 64;

	public const int portrait_neutral_index = 0;

	public const int portrait_happy_index = 1;

	public const int portrait_sad_index = 2;

	public const int portrait_custom_index = 3;

	public const int portrait_blush_index = 4;

	public const int portrait_angry_index = 5;

	public const int startingFriendship = 0;

	public const int defaultSpeed = 2;

	public const int maxGiftsPerWeek = 2;

	public const int friendshipPointsPerHeartLevel = 250;

	public const int maxFriendshipPoints = 2500;

	public const int gift_taste_love = 0;

	public const int gift_taste_like = 2;

	public const int gift_taste_neutral = 8;

	public const int gift_taste_dislike = 4;

	public const int gift_taste_hate = 6;

	public const int gift_taste_stardroptea = 7;

	public const int textStyle_shake = 0;

	public const int textStyle_none = 2;

	public const int adult = 0;

	public const int teen = 1;

	public const int child = 2;

	public const int neutral = 0;

	public const int polite = 1;

	public const int rude = 2;

	public const int outgoing = 0;

	public const int shy = 1;

	public const int positive = 0;

	public const int negative = 1;

	public const string region_desert = "Desert";

	public const string region_town = "Town";

	public const string region_other = "Other";

	/// <summary>The default pixel width for the <see cref="P:StardewValley.Character.Sprite" />.</summary>
	public const int defaultSpriteWidth = 16;

	/// <summary>The default pixel height for the <see cref="P:StardewValley.Character.Sprite" />.</summary>
	public const int defaultSpriteHeight = 32;

	private Dictionary<string, string> dialogue;

	private SchedulePathDescription directionsToNewLocation;

	private int lengthOfWalkingSquareX;

	private int lengthOfWalkingSquareY;

	private int squarePauseAccumulation;

	private int squarePauseTotal;

	private int squarePauseOffset;

	public Microsoft.Xna.Framework.Rectangle lastCrossroad;

	/// <summary>The loaded portrait asset.</summary>
	/// <remarks>This is normally set via <see cref="M:StardewValley.NPC.ChooseAppearance(StardewValley.LocalizedContentManager)" />.</remarks>
	private Texture2D portrait;

	/// <summary>The last location for which <see cref="M:StardewValley.NPC.ChooseAppearance(StardewValley.LocalizedContentManager)" /> was applied.</summary>
	private string LastLocationNameForAppearance;

	/// <summary>The appearance ID from <c>Data/Characters</c> chosen by the last <see cref="M:StardewValley.NPC.ChooseAppearance(StardewValley.LocalizedContentManager)" /> call, or <c>null</c> if the last call didn't apply an appearance entry. This may not match their current textures if they were manually overridden after calling <see cref="M:StardewValley.NPC.ChooseAppearance(StardewValley.LocalizedContentManager)" />.</summary>
	[XmlIgnore]
	public string LastAppearanceId;

	private Vector2 nextSquarePosition;

	[XmlIgnore]
	public int shakeTimer;

	private bool isWalkingInSquare;

	private readonly NetBool isWalkingTowardPlayer = new NetBool();

	protected string textAboveHead;

	protected int textAboveHeadPreTimer;

	protected int textAboveHeadTimer;

	protected int textAboveHeadStyle;

	protected Color? textAboveHeadColor;

	protected float textAboveHeadAlpha;

	public int daysAfterLastBirth = -1;

	protected Dialogue extraDialogueMessageToAddThisMorning;

	[XmlElement("birthday_Season")]
	public readonly NetString birthday_Season = new NetString();

	[XmlElement("birthday_Day")]
	public readonly NetInt birthday_Day = new NetInt();

	[XmlElement("age")]
	public readonly NetInt age = new NetInt();

	[XmlElement("manners")]
	public readonly NetInt manners = new NetInt();

	[XmlElement("socialAnxiety")]
	public readonly NetInt socialAnxiety = new NetInt();

	[XmlElement("optimism")]
	public readonly NetInt optimism = new NetInt();

	/// <summary>The net-synchronized backing field for <see cref="P:StardewValley.NPC.Gender" />.</summary>
	[XmlElement("gender")]
	public readonly NetEnum<Gender> gender = new NetEnum<Gender>();

	[XmlIgnore]
	public readonly NetBool breather = new NetBool(value: true);

	[XmlIgnore]
	public readonly NetBool isSleeping = new NetBool(value: false);

	[XmlElement("sleptInBed")]
	public readonly NetBool sleptInBed = new NetBool(value: true);

	[XmlIgnore]
	public readonly NetBool hideShadow = new NetBool();

	[XmlElement("isInvisible")]
	public readonly NetBool isInvisible = new NetBool(value: false);

	[XmlElement("lastSeenMovieWeek")]
	public readonly NetInt lastSeenMovieWeek = new NetInt(-1);

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.Farmer.friendshipData" /> instead.</summary>
	public bool? datingFarmer;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.Farmer.friendshipData" /> instead.</summary>
	public bool? divorcedFromFarmer;

	[XmlElement("datable")]
	public readonly NetBool datable = new NetBool();

	[XmlIgnore]
	public bool updatedDialogueYet;

	[XmlIgnore]
	public bool immediateSpeak;

	[XmlIgnore]
	public bool ignoreScheduleToday;

	protected int defaultFacingDirection;

	private readonly NetVector2 defaultPosition = new NetVector2();

	[XmlElement("defaultMap")]
	public readonly NetString defaultMap = new NetString();

	public string loveInterest;

	public int id = -1;

	public int daysUntilNotInvisible;

	public bool followSchedule = true;

	[XmlIgnore]
	public PathFindController temporaryController;

	[XmlElement("moveTowardPlayerThreshold")]
	public readonly NetInt moveTowardPlayerThreshold = new NetInt();

	[XmlIgnore]
	public float rotation;

	[XmlIgnore]
	public float yOffset;

	[XmlIgnore]
	public float swimTimer;

	[XmlIgnore]
	public float timerSinceLastMovement;

	[XmlIgnore]
	public string mapBeforeEvent;

	[XmlIgnore]
	public Vector2 positionBeforeEvent;

	[XmlIgnore]
	public Vector2 lastPosition;

	[XmlIgnore]
	public float currentScheduleDelay;

	[XmlIgnore]
	public float scheduleDelaySeconds;

	[XmlIgnore]
	public bool layingDown;

	[XmlIgnore]
	public Vector2 appliedRouteAnimationOffset = Vector2.Zero;

	[XmlIgnore]
	public string[] routeAnimationMetadata;

	[XmlElement("hasSaidAfternoonDialogue")]
	private NetBool hasSaidAfternoonDialogue = new NetBool(value: false);

	[XmlIgnore]
	public static bool hasSomeoneWateredCrops;

	[XmlIgnore]
	public static bool hasSomeoneFedThePet;

	[XmlIgnore]
	public static bool hasSomeoneFedTheAnimals;

	[XmlIgnore]
	public static bool hasSomeoneRepairedTheFences = false;

	[XmlIgnore]
	protected bool _skipRouteEndIntro;

	[NonInstancedStatic]
	public static HashSet<string> invalidDialogueFiles = new HashSet<string>();

	[XmlIgnore]
	protected bool _hasLoadedMasterScheduleData;

	[XmlIgnore]
	protected Dictionary<string, string> _masterScheduleData;

	protected static Stack<Dialogue> _EmptyDialogue = new Stack<Dialogue>();

	/// <summary>If set to a non-null value, the dialogue to return for <see cref="P:StardewValley.NPC.CurrentDialogue" /> instead of reading <see cref="F:StardewValley.Game1.npcDialogues" />.</summary>
	[XmlIgnore]
	public Stack<Dialogue> TemporaryDialogue;

	[XmlIgnore]
	public readonly NetList<MarriageDialogueReference, NetRef<MarriageDialogueReference>> currentMarriageDialogue = new NetList<MarriageDialogueReference, NetRef<MarriageDialogueReference>>();

	public readonly NetBool hasBeenKissedToday = new NetBool(value: false);

	[XmlIgnore]
	public readonly NetRef<MarriageDialogueReference> marriageDefaultDialogue = new NetRef<MarriageDialogueReference>(null);

	[XmlIgnore]
	public readonly NetBool shouldSayMarriageDialogue = new NetBool(value: false);

	public readonly NetEvent0 removeHenchmanEvent = new NetEvent0();

	private bool isPlayingSleepingAnimation;

	public readonly NetBool shouldPlayRobinHammerAnimation = new NetBool();

	private bool isPlayingRobinHammerAnimation;

	public readonly NetBool shouldPlaySpousePatioAnimation = new NetBool();

	private bool isPlayingSpousePatioAnimation;

	public readonly NetBool shouldWearIslandAttire = new NetBool();

	private bool isWearingIslandAttire;

	public readonly NetBool isMovingOnPathFindPath = new NetBool();

	/// <summary>Whether the NPC's portrait has been explicitly overridden (e.g. using the <c>changePortrait</c> event command) and shouldn't be changed automatically.</summary>
	[XmlIgnore]
	public bool portraitOverridden;

	/// <summary>Whether the NPC's sprite has been explicitly overridden (e.g. using the <c>changeSprite</c> event command) and shouldn't be changed automatically.</summary>
	[XmlIgnore]
	public bool spriteOverridden;

	[XmlIgnore]
	public List<SchedulePathDescription> queuedSchedulePaths = new List<SchedulePathDescription>();

	[XmlIgnore]
	public int lastAttemptedSchedule = -1;

	[XmlIgnore]
	public readonly NetBool doingEndOfRouteAnimation = new NetBool();

	private bool currentlyDoingEndOfRouteAnimation;

	[XmlIgnore]
	public readonly NetBool goingToDoEndOfRouteAnimation = new NetBool();

	[XmlIgnore]
	public readonly NetString endOfRouteMessage = new NetString();

	/// <summary>The backing field for <see cref="P:StardewValley.NPC.ScheduleKey" />. Most code should use that property instead.</summary>
	[XmlElement("dayScheduleName")]
	public readonly NetString dayScheduleName = new NetString();

	[XmlElement("islandScheduleName")]
	public readonly NetString islandScheduleName = new NetString();

	private int[] routeEndIntro;

	private int[] routeEndAnimation;

	private int[] routeEndOutro;

	[XmlIgnore]
	public string nextEndOfRouteMessage;

	private string loadedEndOfRouteBehavior;

	[XmlIgnore]
	protected string _startedEndOfRouteBehavior;

	[XmlIgnore]
	protected string _finishingEndOfRouteBehavior;

	[XmlIgnore]
	protected int _beforeEndOfRouteAnimationFrame;

	public readonly NetString endOfRouteBehaviorName = new NetString();

	public Point previousEndPoint;

	public int squareMovementFacingPreference;

	protected bool returningToEndPoint;

	private bool wasKissedYesterday;

	[XmlIgnore]
	public SchedulePathDescription DirectionsToNewLocation
	{
		get
		{
			return directionsToNewLocation;
		}
		set
		{
			directionsToNewLocation = value;
		}
	}

	public int DefaultFacingDirection
	{
		get
		{
			return defaultFacingDirection;
		}
		set
		{
			defaultFacingDirection = value;
		}
	}

	/// <summary>The main dialogue data for this NPC, if available.</summary>
	[XmlIgnore]
	public Dictionary<string, string> Dialogue
	{
		get
		{
			if (this is Monster || this is Pet || this is Horse || this is Child)
			{
				LoadedDialogueKey = null;
				return null;
			}
			if (dialogue == null)
			{
				string text = "Characters\\Dialogue\\" + GetDialogueSheetName();
				if (invalidDialogueFiles.Contains(text))
				{
					LoadedDialogueKey = null;
					dialogue = new Dictionary<string, string>();
				}
				try
				{
					dialogue = Game1.content.Load<Dictionary<string, string>>(text).Select(delegate(KeyValuePair<string, string> pair)
					{
						string key = pair.Key;
						string value = StardewValley.Dialogue.applyGenderSwitch(str: pair.Value, gender: Game1.player.Gender, altTokenOnly: true);
						return new KeyValuePair<string, string>(key, value);
					}).ToDictionary((KeyValuePair<string, string> p) => p.Key, (KeyValuePair<string, string> p) => p.Value);
					LoadedDialogueKey = text;
				}
				catch (ContentLoadException)
				{
					invalidDialogueFiles.Add(text);
					dialogue = new Dictionary<string, string>();
					LoadedDialogueKey = null;
				}
			}
			return dialogue;
		}
	}

	/// <summary>The dialogue key that was loaded via <see cref="P:StardewValley.NPC.Dialogue" />, if any.</summary>
	[XmlIgnore]
	public string LoadedDialogueKey { get; private set; }

	[XmlIgnore]
	public string DefaultMap
	{
		get
		{
			return defaultMap.Value;
		}
		set
		{
			defaultMap.Value = value;
		}
	}

	public Vector2 DefaultPosition
	{
		get
		{
			return defaultPosition.Value;
		}
		set
		{
			defaultPosition.Value = value;
		}
	}

	[XmlIgnore]
	public Texture2D Portrait
	{
		get
		{
			if (portrait == null && IsVillager)
			{
				ChooseAppearance();
			}
			return portrait;
		}
		set
		{
			portrait = value;
		}
	}

	/// <summary>Whether this NPC can dynamically change appearance based on their data in <c>Data/Characters</c>. This can be disabled for temporary NPCs and event actors.</summary>
	[XmlIgnore]
	public bool AllowDynamicAppearance { get; set; } = true;

	/// <inheritdoc />
	[XmlIgnore]
	public override bool IsVillager => true;

	/// <summary>The schedule of this NPC's movements and actions today, if loaded. The key is the time of departure, and the value is a list of directions to reach the new position.</summary>
	/// <remarks>You can set the schedule using <see cref="M:StardewValley.NPC.TryLoadSchedule" /> or one of its overloads.</remarks>
	[XmlIgnore]
	public Dictionary<int, SchedulePathDescription> Schedule { get; private set; }

	/// <summary>The <see cref="P:StardewValley.NPC.Schedule" />'s key in the original data asset, if loaded.</summary>
	[XmlIgnore]
	public string ScheduleKey => dayScheduleName.Value;

	public bool IsWalkingInSquare
	{
		get
		{
			return isWalkingInSquare;
		}
		set
		{
			isWalkingInSquare = value;
		}
	}

	public bool IsWalkingTowardPlayer
	{
		get
		{
			return isWalkingTowardPlayer.Value;
		}
		set
		{
			isWalkingTowardPlayer.Value = value;
		}
	}

	[XmlIgnore]
	public virtual Stack<Dialogue> CurrentDialogue
	{
		get
		{
			if (TemporaryDialogue != null)
			{
				return TemporaryDialogue;
			}
			if (Game1.npcDialogues == null)
			{
				Game1.npcDialogues = new Dictionary<string, Stack<Dialogue>>();
			}
			if (!IsVillager)
			{
				return _EmptyDialogue;
			}
			Game1.npcDialogues.TryGetValue(base.Name, out var value);
			if (value == null)
			{
				return Game1.npcDialogues[base.Name] = loadCurrentDialogue();
			}
			return value;
		}
		set
		{
			if (TemporaryDialogue != null)
			{
				TemporaryDialogue = value;
			}
			else if (Game1.npcDialogues != null)
			{
				Game1.npcDialogues[base.Name] = value;
			}
		}
	}

	[XmlIgnore]
	public string Birthday_Season
	{
		get
		{
			return birthday_Season.Value;
		}
		set
		{
			birthday_Season.Value = value;
		}
	}

	[XmlIgnore]
	public int Birthday_Day
	{
		get
		{
			return birthday_Day.Value;
		}
		set
		{
			birthday_Day.Value = value;
		}
	}

	[XmlIgnore]
	public int Age
	{
		get
		{
			return age.Value;
		}
		set
		{
			age.Value = value;
		}
	}

	[XmlIgnore]
	public int Manners
	{
		get
		{
			return manners.Value;
		}
		set
		{
			manners.Value = value;
		}
	}

	[XmlIgnore]
	public int SocialAnxiety
	{
		get
		{
			return socialAnxiety.Value;
		}
		set
		{
			socialAnxiety.Value = value;
		}
	}

	[XmlIgnore]
	public int Optimism
	{
		get
		{
			return optimism.Value;
		}
		set
		{
			optimism.Value = value;
		}
	}

	/// <summary>The character's gender identity.</summary>
	[XmlIgnore]
	public override Gender Gender
	{
		get
		{
			return gender.Value;
		}
		set
		{
			gender.Value = value;
		}
	}

	[XmlIgnore]
	public bool Breather
	{
		get
		{
			return breather.Value;
		}
		set
		{
			breather.Value = value;
		}
	}

	[XmlIgnore]
	public bool HideShadow
	{
		get
		{
			return hideShadow.Value;
		}
		set
		{
			hideShadow.Value = value;
		}
	}

	[XmlIgnore]
	public bool HasPartnerForDance
	{
		get
		{
			foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
			{
				if (onlineFarmer.dancePartner.TryGetVillager() == this)
				{
					return true;
				}
			}
			return false;
		}
	}

	[XmlIgnore]
	public bool IsInvisible
	{
		get
		{
			return isInvisible.Value;
		}
		set
		{
			isInvisible.Value = value;
		}
	}

	/// <summary>Whether social features (like birthdays, gift giving, friendship, and an entry in the social tab) are enabled for this NPC.</summary>
	public virtual bool CanSocialize
	{
		get
		{
			if (IsVillager)
			{
				return CanSocializePerData(base.Name, base.currentLocation);
			}
			return false;
		}
	}

	public NPC()
	{
	}

	public NPC(AnimatedSprite sprite, Vector2 position, int facingDir, string name, LocalizedContentManager content = null)
		: base(sprite, position, 2, name)
	{
		faceDirection(facingDir);
		defaultPosition.Value = position;
		defaultFacingDirection = facingDir;
		lastCrossroad = new Microsoft.Xna.Framework.Rectangle((int)position.X, (int)position.Y + 64, 64, 64);
		if (content != null)
		{
			try
			{
				portrait = content.Load<Texture2D>("Portraits\\" + name);
			}
			catch (Exception)
			{
			}
		}
	}

	public NPC(AnimatedSprite sprite, Vector2 position, string defaultMap, int facingDirection, string name, bool datable, Texture2D portrait)
		: this(sprite, position, defaultMap, facingDirection, name, portrait, eventActor: false)
	{
		this.datable.Value = datable;
	}

	public NPC(AnimatedSprite sprite, Vector2 position, string defaultMap, int facingDir, string name, Texture2D portrait, bool eventActor)
		: base(sprite, position, 2, name)
	{
		this.portrait = portrait;
		faceDirection(facingDir);
		if (!eventActor)
		{
			lastCrossroad = new Microsoft.Xna.Framework.Rectangle((int)position.X, (int)position.Y + 64, 64, 64);
		}
		reloadData();
		defaultPosition.Value = position;
		this.defaultMap.Value = defaultMap;
		base.currentLocation = Game1.getLocationFromName(defaultMap);
		defaultFacingDirection = facingDir;
	}

	public virtual void reloadData()
	{
		if (this is Child)
		{
			return;
		}
		CharacterData data = GetData();
		if (data != null)
		{
			Age = (int)Utility.GetEnumOrDefault(data.Age, NpcAge.Adult);
			Manners = (int)Utility.GetEnumOrDefault(data.Manner, NpcManner.Neutral);
			SocialAnxiety = (int)Utility.GetEnumOrDefault(data.SocialAnxiety, NpcSocialAnxiety.Outgoing);
			Optimism = (int)Utility.GetEnumOrDefault(data.Optimism, NpcOptimism.Positive);
			Gender = Utility.GetEnumOrDefault(data.Gender, Gender.Male);
			datable.Value = data.CanBeRomanced;
			loveInterest = data.LoveInterest;
			Birthday_Season = (data.BirthSeason.HasValue ? Utility.getSeasonKey(data.BirthSeason.Value) : null);
			Birthday_Day = data.BirthDay;
			id = ((data.FestivalVanillaActorIndex > -1) ? data.FestivalVanillaActorIndex : Game1.hash.GetDeterministicHashCode(name.Value));
			breather.Value = data.Breather;
			if (!isMarried())
			{
				reloadDefaultLocation();
			}
			displayName = translateName();
		}
	}

	public virtual void reloadDefaultLocation()
	{
		CharacterData data = GetData();
		if (data != null && ReadNpcHomeData(data, base.currentLocation, out var locationName, out var tile, out var direction))
		{
			DefaultMap = locationName;
			DefaultPosition = new Vector2(tile.X * 64, tile.Y * 64);
			DefaultFacingDirection = direction;
		}
	}

	/// <summary>Get an NPC's home location from its data, or fallback values if it doesn't exist.</summary>
	/// <param name="data">The character data for the NPC.</param>
	/// <param name="currentLocation">The NPC's current location, if applicable.</param>
	/// <param name="locationName">The internal name of the NPC's default map.</param>
	/// <param name="tile">The NPC's default tile position within the <paramref name="locationName" />.</param>
	/// <param name="direction">The default facing direction.</param>
	/// <returns>Returns whether a valid home was found in the given character data.</returns>
	public static bool ReadNpcHomeData(CharacterData data, GameLocation currentLocation, out string locationName, out Point tile, out int direction)
	{
		if (data?.Home != null)
		{
			foreach (CharacterHomeData item in data.Home)
			{
				if (item.Condition == null || GameStateQuery.CheckConditions(item.Condition, currentLocation))
				{
					locationName = item.Location;
					tile = item.Tile;
					direction = (Utility.TryParseDirection(item.Direction, out var parsed) ? parsed : 0);
					return true;
				}
			}
		}
		locationName = "Town";
		tile = new Point(29, 67);
		direction = 2;
		return false;
	}

	public virtual bool canTalk()
	{
		return true;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(birthday_Season, "birthday_Season").AddField(birthday_Day, "birthday_Day").AddField(datable, "datable")
			.AddField(shouldPlayRobinHammerAnimation, "shouldPlayRobinHammerAnimation")
			.AddField(shouldPlaySpousePatioAnimation, "shouldPlaySpousePatioAnimation")
			.AddField(isWalkingTowardPlayer, "isWalkingTowardPlayer")
			.AddField(moveTowardPlayerThreshold, "moveTowardPlayerThreshold")
			.AddField(age, "age")
			.AddField(manners, "manners")
			.AddField(socialAnxiety, "socialAnxiety")
			.AddField(optimism, "optimism")
			.AddField(gender, "gender")
			.AddField(breather, "breather")
			.AddField(isSleeping, "isSleeping")
			.AddField(hideShadow, "hideShadow")
			.AddField(isInvisible, "isInvisible")
			.AddField(defaultMap, "defaultMap")
			.AddField(defaultPosition, "defaultPosition")
			.AddField(removeHenchmanEvent, "removeHenchmanEvent")
			.AddField(doingEndOfRouteAnimation, "doingEndOfRouteAnimation")
			.AddField(goingToDoEndOfRouteAnimation, "goingToDoEndOfRouteAnimation")
			.AddField(endOfRouteMessage, "endOfRouteMessage")
			.AddField(endOfRouteBehaviorName, "endOfRouteBehaviorName")
			.AddField(lastSeenMovieWeek, "lastSeenMovieWeek")
			.AddField(currentMarriageDialogue, "currentMarriageDialogue")
			.AddField(marriageDefaultDialogue, "marriageDefaultDialogue")
			.AddField(shouldSayMarriageDialogue, "shouldSayMarriageDialogue")
			.AddField(hasBeenKissedToday, "hasBeenKissedToday")
			.AddField(hasSaidAfternoonDialogue, "hasSaidAfternoonDialogue")
			.AddField(dayScheduleName, "dayScheduleName")
			.AddField(islandScheduleName, "islandScheduleName")
			.AddField(sleptInBed, "sleptInBed")
			.AddField(shouldWearIslandAttire, "shouldWearIslandAttire")
			.AddField(isMovingOnPathFindPath, "isMovingOnPathFindPath");
		position.Field.AxisAlignedMovement = true;
		removeHenchmanEvent.onEvent += performRemoveHenchman;
	}

	/// <summary>Reload the NPC's sprite or portrait based on their character data within the current context.</summary>
	/// <param name="content">The content manager from which to load assets, or <c>null</c> for the default content manager.</param>
	public virtual void ChooseAppearance(LocalizedContentManager content = null)
	{
		LastAppearanceId = null;
		if (base.SimpleNonVillagerNPC)
		{
			return;
		}
		content = content ?? Game1.content;
		GameLocation gameLocation = base.currentLocation;
		if (gameLocation == null)
		{
			return;
		}
		LastLocationNameForAppearance = gameLocation.NameOrUniqueName;
		bool flag = false;
		if (gameLocation.TryGetMapProperty("UniquePortrait", out var propertyValue) && ArgUtility.SplitBySpace(propertyValue).Contains(base.Name))
		{
			string text = "Portraits\\" + getTextureName() + "_" + gameLocation.Name;
			flag = TryLoadPortraits(text, out var error, content);
			if (!flag)
			{
				Game1.log.Warn($"NPC {base.Name} can't load portraits from '{text}' (per the {"UniquePortrait"} map property in '{gameLocation.NameOrUniqueName}'): {error}. Falling back to default portraits.");
			}
		}
		bool flag2 = false;
		if (gameLocation.TryGetMapProperty("UniqueSprite", out var propertyValue2) && ArgUtility.SplitBySpace(propertyValue2).Contains(base.Name))
		{
			string text2 = "Characters\\" + getTextureName() + "_" + gameLocation.Name;
			flag2 = TryLoadSprites(text2, out var error2, content);
			if (!flag2)
			{
				Game1.log.Warn($"NPC {base.Name} can't load sprites from '{text2}' (per the {"UniqueSprite"} map property in '{gameLocation.NameOrUniqueName}'): {error2}. Falling back to default sprites.");
			}
		}
		if (flag && flag2)
		{
			return;
		}
		CharacterData characterData = null;
		CharacterAppearanceData characterAppearanceData = null;
		if (!IsMonster)
		{
			characterData = GetData();
			if (characterData != null && characterData.Appearance?.Count > 0)
			{
				List<CharacterAppearanceData> list = new List<CharacterAppearanceData>();
				int num = 0;
				Random random = Utility.CreateDaySaveRandom(Game1.hash.GetDeterministicHashCode(base.Name));
				Season season = gameLocation.GetSeason();
				bool isOutdoors = gameLocation.IsOutdoors;
				int num2 = int.MaxValue;
				foreach (CharacterAppearanceData item in characterData.Appearance)
				{
					if (item.Precedence > num2 || item.IsIslandAttire != isWearingIslandAttire)
					{
						continue;
					}
					Season? season2 = item.Season;
					if ((!season2.HasValue || item.Season.Value == season) && (isOutdoors ? item.Outdoors : item.Indoors) && GameStateQuery.CheckConditions(item.Condition, gameLocation, null, null, null, random))
					{
						if (item.Precedence < num2)
						{
							num2 = item.Precedence;
							list.Clear();
							num = 0;
						}
						list.Add(item);
						num += item.Weight;
					}
				}
				switch (list.Count)
				{
				case 1:
					characterAppearanceData = list[0];
					break;
				default:
				{
					characterAppearanceData = list[list.Count - 1];
					int num3 = Utility.CreateDaySaveRandom(Game1.hash.GetDeterministicHashCode(base.Name)).Next(num + 1);
					foreach (CharacterAppearanceData item2 in list)
					{
						num3 -= item2.Weight;
						if (num3 <= 0)
						{
							characterAppearanceData = item2;
							break;
						}
					}
					break;
				}
				case 0:
					break;
				}
			}
		}
		if (!flag)
		{
			string text3 = "Portraits/" + getTextureName();
			bool flag3 = false;
			if (characterAppearanceData != null && characterAppearanceData.Portrait != null && characterAppearanceData.Portrait != text3)
			{
				flag3 = TryLoadPortraits(characterAppearanceData.Portrait, out var error3, content);
				if (!flag3)
				{
					Game1.log.Warn($"NPC {base.Name} can't load portraits from '{characterAppearanceData.Portrait}' (per appearance entry '{characterAppearanceData.Id}' in Data/Characters): {error3}. Falling back to default portraits.");
				}
			}
			if (!flag3 && isWearingIslandAttire)
			{
				string text4 = text3 + "_Beach";
				if (content.DoesAssetExist<Texture2D>(text4))
				{
					flag3 = TryLoadPortraits(text4, out var error4, content);
					if (!flag3)
					{
						Game1.log.Warn($"NPC {base.Name} can't load portraits from '{text4}' for island attire: {error4}. Falling back to default portraits.");
					}
				}
			}
			if (!flag3 && !TryLoadPortraits(text3, out var error5, content))
			{
				Game1.log.Warn($"NPC {base.Name} can't load portraits from '{text3}': {error5}.");
			}
			if (flag3)
			{
				LastAppearanceId = characterAppearanceData?.Id;
			}
		}
		if (!flag2)
		{
			string text5 = "Characters/" + getTextureName();
			bool flag4 = false;
			if (characterAppearanceData != null && characterAppearanceData.Sprite != null && characterAppearanceData.Sprite != text5)
			{
				flag4 = TryLoadSprites(characterAppearanceData.Sprite, out var error6, content);
				if (!flag4)
				{
					Game1.log.Warn($"NPC {base.Name} can't load sprites from '{characterAppearanceData.Sprite}' (per appearance entry '{characterAppearanceData.Id}' in Data/Characters): {error6}. Falling back to default sprites.");
				}
			}
			if (!flag4 && isWearingIslandAttire)
			{
				string text6 = text5 + "_Beach";
				if (content.DoesAssetExist<Texture2D>(text6))
				{
					flag4 = TryLoadSprites(text6, out var error7, content);
					if (!flag4)
					{
						Game1.log.Warn($"NPC {base.Name} can't load sprites from '{text6}' for island attire: {error7}. Falling back to default sprites.");
					}
				}
			}
			if (!flag4 && !TryLoadSprites(text5, out var error8, content))
			{
				Game1.log.Warn($"NPC {base.Name} can't load sprites from '{text5}': {error8}.");
			}
			if (flag4)
			{
				LastAppearanceId = characterAppearanceData?.Id;
			}
		}
		if (characterData != null && Sprite != null)
		{
			Sprite.SpriteWidth = characterData.Size.X;
			Sprite.SpriteHeight = characterData.Size.Y;
			Sprite.ignoreSourceRectUpdates = false;
		}
	}

	protected override string translateName()
	{
		return GetDisplayName(name.Value);
	}

	public string getName()
	{
		if (displayName != null && displayName.Length > 0)
		{
			return displayName;
		}
		return base.Name;
	}

	public virtual string getTextureName()
	{
		return getTextureNameForCharacter(base.Name);
	}

	public static string getTextureNameForCharacter(string character_name)
	{
		TryGetData(character_name, out var data);
		string text = data?.TextureName;
		if (string.IsNullOrEmpty(text))
		{
			return character_name;
		}
		return text;
	}

	public void resetSeasonalDialogue()
	{
		dialogue = null;
	}

	public void performSpecialScheduleChanges()
	{
		if (Schedule == null || !base.Name.Equals("Pam") || !Game1.MasterPlayer.mailReceived.Contains("ccVault"))
		{
			return;
		}
		bool flag = false;
		foreach (KeyValuePair<int, SchedulePathDescription> item in Schedule)
		{
			bool flag2 = false;
			switch (item.Value.targetLocationName)
			{
			case "BusStop":
				flag = true;
				break;
			case "DesertFestival":
			case "Desert":
			case "IslandSouth":
			{
				BusStop busStop = Game1.RequireLocation<BusStop>("BusStop");
				Game1.netWorldState.Value.canDriveYourselfToday.Value = true;
				Object obj = ItemRegistry.Create<Object>("(BC)TextSign");
				obj.signText.Value = TokenStringBuilder.LocalizedText((item.Value.targetLocationName == "IslandSouth") ? "Strings\\1_6_Strings:Pam_busSign_resort" : "Strings\\1_6_Strings:Pam_busSign");
				obj.SpecialVariable = 987659;
				busStop.tryPlaceObject(new Vector2(25f, 10f), obj);
				flag = true;
				flag2 = true;
				break;
			}
			}
			if (flag2)
			{
				break;
			}
		}
		if (!flag && !Game1.isGreenRain)
		{
			BusStop obj2 = Game1.getLocationFromName("BusStop") as BusStop;
			Game1.netWorldState.Value.canDriveYourselfToday.Value = true;
			Object obj3 = (Object)ItemRegistry.Create("(BC)TextSign");
			obj3.signText.Value = TokenStringBuilder.LocalizedText("Strings\\1_6_Strings:Pam_busSign_generic");
			obj3.SpecialVariable = 987659;
			obj2.tryPlaceObject(new Vector2(25f, 10f), obj3);
		}
	}

	/// <summary>Update the NPC state (including sprite, dialogue, facing direction, schedules, etc). Despite the name, this doesn't only affect the sprite.</summary>
	/// <param name="onlyAppearance">Only reload the NPC's appearance (e.g. sprite, portraits, or breather/shadow fields), don't change any other data.</param>
	public virtual void reloadSprite(bool onlyAppearance = false)
	{
		if (base.SimpleNonVillagerNPC)
		{
			return;
		}
		ChooseAppearance();
		if (onlyAppearance || (!Game1.newDay && Game1.gameMode != 6))
		{
			return;
		}
		faceDirection(DefaultFacingDirection);
		previousEndPoint = new Point((int)defaultPosition.X / 64, (int)defaultPosition.Y / 64);
		TryLoadSchedule();
		performSpecialScheduleChanges();
		resetSeasonalDialogue();
		resetCurrentDialogue();
		updateConstructionAnimation();
		try
		{
			displayName = translateName();
		}
		catch (Exception)
		{
		}
	}

	/// <summary>Try to load a portraits texture, or keep the current texture if the load fails.</summary>
	/// <param name="assetName">The asset name to load.</param>
	/// <param name="error">If loading the portrait failed, an error phrase indicating why it failed.</param>
	/// <param name="content">The content manager from which to load the asset, or <c>null</c> for the default content manager.</param>
	/// <returns>Returns whether the texture was successfully loaded.</returns>
	public bool TryLoadPortraits(string assetName, out string error, LocalizedContentManager content = null)
	{
		if (base.Name == "Raccoon" || base.Name == "MrsRaccoon")
		{
			error = null;
			return true;
		}
		if (portraitOverridden)
		{
			error = null;
			return true;
		}
		if (string.IsNullOrWhiteSpace(assetName))
		{
			error = "the asset name is empty";
			return false;
		}
		if (portrait?.Name == assetName && !portrait.IsDisposed)
		{
			error = null;
			return true;
		}
		if (content == null)
		{
			content = Game1.content;
		}
		try
		{
			portrait = content.Load<Texture2D>(assetName);
			portrait.Name = assetName;
			error = null;
			return true;
		}
		catch (Exception ex)
		{
			error = ex.ToString();
			return false;
		}
	}

	/// <summary>Try to load a sprite texture, or keep the current texture if the load fails.</summary>
	/// <param name="assetName">The asset name to load.</param>
	/// <param name="error">If loading the portrait failed, an error phrase indicating why it failed.</param>
	/// <param name="content">The content manager from which to load the asset, or <c>null</c> for the default content manager.</param>
	/// <param name="logOnFail">Whether to log a warning if the texture can't be loaded.</param>
	/// <returns>Returns whether the texture was successfully loaded.</returns>
	public bool TryLoadSprites(string assetName, out string error, LocalizedContentManager content = null)
	{
		if (spriteOverridden)
		{
			error = null;
			return true;
		}
		if (string.IsNullOrWhiteSpace(assetName))
		{
			error = "the asset name is empty";
			return false;
		}
		if (Sprite?.spriteTexture != null && ((Sprite.overrideTextureName ?? Sprite.textureName.Value) == assetName || Sprite.spriteTexture.Name == assetName) && !Sprite.spriteTexture.IsDisposed)
		{
			error = null;
			return true;
		}
		if (content == null)
		{
			content = Game1.content;
		}
		try
		{
			if (Sprite == null)
			{
				Sprite = new AnimatedSprite(content, assetName);
			}
			else
			{
				Sprite.LoadTexture(assetName, Game1.IsMasterGame);
			}
			error = null;
			return true;
		}
		catch (Exception ex)
		{
			error = ex.ToString();
			return false;
		}
	}

	private void updateConstructionAnimation()
	{
		bool flag = Utility.isFestivalDay();
		if (Game1.IsMasterGame && base.Name == "Robin" && !flag && (!Game1.isGreenRain || Game1.year > 1))
		{
			if (Game1.player.daysUntilHouseUpgrade.Value > 0)
			{
				Farm farm = Game1.getFarm();
				Game1.warpCharacter(this, farm.NameOrUniqueName, new Vector2(farm.GetMainFarmHouseEntry().X + 4, farm.GetMainFarmHouseEntry().Y - 1));
				isPlayingRobinHammerAnimation = false;
				shouldPlayRobinHammerAnimation.Value = true;
				return;
			}
			if (Game1.IsThereABuildingUnderConstruction())
			{
				Building buildingUnderConstruction = Game1.GetBuildingUnderConstruction();
				if (buildingUnderConstruction == null)
				{
					return;
				}
				GameLocation indoors = buildingUnderConstruction.GetIndoors();
				if (buildingUnderConstruction.daysUntilUpgrade.Value > 0 && indoors != null)
				{
					base.currentLocation?.characters.Remove(this);
					base.currentLocation = indoors;
					if (base.currentLocation != null && !base.currentLocation.characters.Contains(this))
					{
						base.currentLocation.addCharacter(this);
					}
					string indoorsName = buildingUnderConstruction.GetIndoorsName();
					if (indoorsName != null && indoorsName.StartsWith("Shed"))
					{
						setTilePosition(2, 2);
						position.X -= 28f;
					}
					else
					{
						setTilePosition(1, 5);
					}
				}
				else
				{
					Game1.warpCharacter(this, buildingUnderConstruction.parentLocationName.Value, new Vector2(buildingUnderConstruction.tileX.Value + buildingUnderConstruction.tilesWide.Value / 2, buildingUnderConstruction.tileY.Value + buildingUnderConstruction.tilesHigh.Value / 2));
					position.X += 16f;
					position.Y -= 32f;
				}
				isPlayingRobinHammerAnimation = false;
				shouldPlayRobinHammerAnimation.Value = true;
				return;
			}
			if (Game1.RequireLocation<Town>("Town").daysUntilCommunityUpgrade.Value > 0)
			{
				if (Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
				{
					Game1.warpCharacter(this, "Backwoods", new Vector2(41f, 23f));
					isPlayingRobinHammerAnimation = false;
					shouldPlayRobinHammerAnimation.Value = true;
				}
				else if (Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
				{
					Game1.warpCharacter(this, "Town", new Vector2(77f, 68f));
					isPlayingRobinHammerAnimation = false;
					shouldPlayRobinHammerAnimation.Value = true;
				}
				return;
			}
		}
		shouldPlayRobinHammerAnimation.Value = false;
	}

	private void doPlayRobinHammerAnimation()
	{
		Sprite.ClearAnimation();
		Sprite.AddFrame(new FarmerSprite.AnimationFrame(24, 75));
		Sprite.AddFrame(new FarmerSprite.AnimationFrame(25, 75));
		Sprite.AddFrame(new FarmerSprite.AnimationFrame(26, 300, secondaryArm: false, flip: false, robinHammerSound));
		Sprite.AddFrame(new FarmerSprite.AnimationFrame(27, 1000, secondaryArm: false, flip: false, robinVariablePause));
		ignoreScheduleToday = true;
		bool flag = Game1.player.daysUntilHouseUpgrade.Value == 1 || Game1.RequireLocation<Town>("Town").daysUntilCommunityUpgrade.Value == 1;
		CurrentDialogue.Clear();
		CurrentDialogue.Push(new Dialogue(this, flag ? "Strings\\StringsFromCSFiles:NPC.cs.3927" : "Strings\\StringsFromCSFiles:NPC.cs.3926"));
	}

	public void showTextAboveHead(string text, Color? spriteTextColor = null, int style = 2, int duration = 3000, int preTimer = 0)
	{
		if (!IsInvisible)
		{
			textAboveHeadAlpha = 0f;
			textAboveHead = StardewValley.Dialogue.applyGenderSwitchBlocks(Game1.player.Gender, text);
			textAboveHeadPreTimer = preTimer;
			textAboveHeadTimer = duration;
			textAboveHeadStyle = style;
			textAboveHeadColor = spriteTextColor;
		}
	}

	public virtual bool hitWithTool(Tool t)
	{
		return false;
	}

	/// <summary>Get whether this NPC can receive gifts from the player (regardless of whether they've already received one today).</summary>
	public bool CanReceiveGifts()
	{
		if (CanSocialize && !base.SimpleNonVillagerNPC && Game1.NPCGiftTastes.ContainsKey(base.Name))
		{
			return GetData()?.CanReceiveGifts ?? true;
		}
		return false;
	}

	/// <summary>Get how much the NPC likes receiving an item as a gift.</summary>
	/// <param name="item">The item to check.</param>
	/// <returns>Returns one of <see cref="F:StardewValley.NPC.gift_taste_hate" />, <see cref="F:StardewValley.NPC.gift_taste_dislike" />, <see cref="F:StardewValley.NPC.gift_taste_neutral" />, <see cref="F:StardewValley.NPC.gift_taste_like" />, or <see cref="F:StardewValley.NPC.gift_taste_love" />.</returns>
	public int getGiftTasteForThisItem(Item item)
	{
		if (item.QualifiedItemId == "(O)StardropTea")
		{
			return 7;
		}
		int num = 8;
		if (item is Object { Category: var category } obj)
		{
			string value = category.ToString() ?? "";
			string[] array = ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Love"]);
			string[] array2 = ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Hate"]);
			string[] array3 = ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Like"]);
			string[] array4 = ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Dislike"]);
			string[] list = ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Neutral"]);
			if (array.Contains(value))
			{
				num = 0;
			}
			else if (array2.Contains(value))
			{
				num = 6;
			}
			else if (array3.Contains(value))
			{
				num = 2;
			}
			else if (array4.Contains(value))
			{
				num = 4;
			}
			if (CheckTasteContextTags(obj, array))
			{
				num = 0;
			}
			else if (CheckTasteContextTags(obj, array2))
			{
				num = 6;
			}
			else if (CheckTasteContextTags(obj, array3))
			{
				num = 2;
			}
			else if (CheckTasteContextTags(obj, array4))
			{
				num = 4;
			}
			bool flag = false;
			bool flag2 = false;
			if (CheckTaste(array, obj))
			{
				num = 0;
				flag = true;
			}
			else if (CheckTaste(array2, obj))
			{
				num = 6;
				flag = true;
			}
			else if (CheckTaste(array3, obj))
			{
				num = 2;
				flag = true;
			}
			else if (CheckTaste(array4, obj))
			{
				num = 4;
				flag = true;
			}
			else if (CheckTaste(list, obj))
			{
				num = 8;
				flag = true;
				flag2 = true;
			}
			if (obj.Type == "Arch")
			{
				num = 4;
				if (base.Name.Equals("Penny") || name.Equals("Dwarf"))
				{
					num = 2;
				}
			}
			if (num == 8 && !flag2)
			{
				if (obj.edibility.Value != -300 && obj.edibility.Value < 0)
				{
					num = 6;
				}
				else if (obj.price.Value < 20)
				{
					num = 4;
				}
			}
			if (Game1.NPCGiftTastes.TryGetValue(base.Name, out var value2))
			{
				string[] array5 = value2.Split('/');
				List<string[]> list2 = new List<string[]>();
				for (int i = 0; i < 10; i += 2)
				{
					string[] array6 = ArgUtility.SplitBySpace(array5[i + 1]);
					string[] array7 = new string[array6.Length];
					for (int j = 0; j < array6.Length; j++)
					{
						if (array6[j].Length > 0)
						{
							array7[j] = array6[j];
						}
					}
					list2.Add(array7);
				}
				if (CheckTaste(list2[0], obj))
				{
					return 0;
				}
				if (CheckTaste(list2[3], obj))
				{
					return 6;
				}
				if (CheckTaste(list2[1], obj))
				{
					return 2;
				}
				if (CheckTaste(list2[2], obj))
				{
					return 4;
				}
				if (CheckTaste(list2[4], obj))
				{
					return 8;
				}
				if (CheckTasteContextTags(obj, list2[0]))
				{
					return 0;
				}
				if (CheckTasteContextTags(obj, list2[3]))
				{
					return 6;
				}
				if (CheckTasteContextTags(obj, list2[1]))
				{
					return 2;
				}
				if (CheckTasteContextTags(obj, list2[2]))
				{
					return 4;
				}
				if (CheckTasteContextTags(obj, list2[4]))
				{
					return 8;
				}
				if (!flag)
				{
					if (category != 0 && list2[0].Contains(value))
					{
						return 0;
					}
					if (category != 0 && list2[3].Contains(value))
					{
						return 6;
					}
					if (category != 0 && list2[1].Contains(value))
					{
						return 2;
					}
					if (category != 0 && list2[2].Contains(value))
					{
						return 4;
					}
					if (category != 0 && list2[4].Contains(value))
					{
						return 8;
					}
				}
			}
		}
		return num;
	}

	public bool CheckTaste(IEnumerable<string> list, Item item)
	{
		foreach (string item2 in list)
		{
			if (item2 != null && !item2.StartsWith('-'))
			{
				ParsedItemData data = ItemRegistry.GetData(item2);
				if (data?.ItemType != null && item.QualifiedItemId == data.QualifiedItemId)
				{
					return true;
				}
			}
		}
		return false;
	}

	public virtual bool CheckTasteContextTags(Item item, string[] list)
	{
		foreach (string text in list)
		{
			if (text != null && text.Length > 0 && !char.IsNumber(text[0]) && text[0] != '-' && item.HasContextTag(text))
			{
				return true;
			}
		}
		return false;
	}

	private void goblinDoorEndBehavior(Character c, GameLocation l)
	{
		l.characters.Remove(this);
		l.playSound("doorClose");
	}

	private void performRemoveHenchman()
	{
		Sprite.CurrentFrame = 4;
		Game1.netWorldState.Value.IsGoblinRemoved = true;
		Game1.player.removeQuest("27");
		Stack<Point> stack = new Stack<Point>();
		stack.Push(new Point(20, 21));
		stack.Push(new Point(20, 22));
		stack.Push(new Point(20, 23));
		stack.Push(new Point(20, 24));
		stack.Push(new Point(20, 25));
		stack.Push(new Point(20, 26));
		stack.Push(new Point(20, 27));
		stack.Push(new Point(20, 28));
		addedSpeed = 2f;
		controller = new PathFindController(stack, this, base.currentLocation);
		controller.endBehaviorFunction = goblinDoorEndBehavior;
		showTextAboveHead(Game1.content.LoadString("Strings\\Characters:Henchman6"));
		Game1.player.mailReceived.Add("henchmanGone");
		base.currentLocation.removeTile(20, 29, "Buildings");
	}

	private void engagementResponse(Farmer who, bool asRoommate = false)
	{
		Game1.changeMusicTrack("silence");
		who.spouse = base.Name;
		if (!asRoommate)
		{
			Game1.multiplayer.globalChatInfoMessage("Engaged", Game1.player.Name, GetTokenizedDisplayName());
		}
		Friendship friendship = who.friendshipData[base.Name];
		friendship.Status = FriendshipStatus.Engaged;
		friendship.RoommateMarriage = asRoommate;
		WorldDate worldDate = new WorldDate(Game1.Date);
		worldDate.TotalDays += 3;
		who.removeDatingActiveDialogueEvents(Game1.player.spouse);
		while (!Game1.canHaveWeddingOnDay(worldDate.DayOfMonth, worldDate.Season))
		{
			worldDate.TotalDays++;
		}
		friendship.WeddingDate = worldDate;
		CurrentDialogue.Clear();
		if (asRoommate && DataLoader.EngagementDialogue(Game1.content).ContainsKey(base.Name + "Roommate0"))
		{
			CurrentDialogue.Push(new Dialogue(this, "Data\\EngagementDialogue:" + base.Name + "Roommate0"));
			Dialogue dialogue = StardewValley.Dialogue.TryGetDialogue(this, "Strings\\StringsFromCSFiles:" + base.Name + "_EngagedRoommate");
			if (dialogue != null)
			{
				CurrentDialogue.Push(dialogue);
			}
			else
			{
				dialogue = StardewValley.Dialogue.TryGetDialogue(this, "Strings\\StringsFromCSFiles:" + base.Name + "_Engaged");
				if (dialogue != null)
				{
					CurrentDialogue.Push(dialogue);
				}
				else
				{
					CurrentDialogue.Push(new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.3980"));
				}
			}
		}
		else
		{
			Dialogue dialogue2 = StardewValley.Dialogue.TryGetDialogue(this, "Data\\EngagementDialogue:" + base.Name + "0");
			if (dialogue2 != null)
			{
				CurrentDialogue.Push(dialogue2);
			}
			dialogue2 = StardewValley.Dialogue.TryGetDialogue(this, "Strings\\StringsFromCSFiles:" + base.Name + "_Engaged");
			if (dialogue2 != null)
			{
				CurrentDialogue.Push(dialogue2);
			}
			else
			{
				CurrentDialogue.Push(new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.3980"));
			}
		}
		Dialogue obj = CurrentDialogue.Peek();
		obj.onFinish = (Action)Delegate.Combine(obj.onFinish, (Action)delegate
		{
			Game1.changeMusicTrack("none", track_interruptable: true);
			GameLocation.HandleMusicChange(null, who.currentLocation);
		});
		who.changeFriendship(1, this);
		who.reduceActiveItemByOne();
		who.completelyStopAnimatingOrDoingAction();
		Game1.drawDialogue(this);
	}

	/// <summary>Try to receive an item from the player.</summary>
	/// <param name="who">The player whose active object to receive.</param>
	/// <param name="probe">Whether to return what the method would return if called normally, but without actually accepting the item or making any changes to the NPC. This is used to accurately predict whether the NPC would accept or react to the offer.</param>
	/// <returns>Returns true if the NPC accepted the item or reacted to the offer, else false.</returns>
	public virtual bool tryToReceiveActiveObject(Farmer who, bool probe = false)
	{
		if (base.SimpleNonVillagerNPC)
		{
			return false;
		}
		Object activeObj = who.ActiveObject;
		if (activeObj == null)
		{
			return false;
		}
		if (!probe)
		{
			who.Halt();
			who.faceGeneralDirection(getStandingPosition(), 0, opposite: false, useTileCalculations: false);
		}
		if (base.Name == "Henchman" && Game1.currentLocation.NameOrUniqueName == "WitchSwamp")
		{
			if (activeObj.QualifiedItemId == "(O)308")
			{
				if (controller != null)
				{
					return false;
				}
				if (!probe)
				{
					who.currentLocation.localSound("coin");
					who.reduceActiveItemByOne();
					CurrentDialogue.Push(new Dialogue(this, "Strings\\Characters:Henchman5"));
					Game1.drawDialogue(this);
					who.freezePause = 2000;
					removeHenchmanEvent.Fire();
				}
			}
			else if (!probe)
			{
				CurrentDialogue.Push(new Dialogue(this, (activeObj.QualifiedItemId == "(O)684") ? "Strings\\Characters:Henchman4" : "Strings\\Characters:Henchman3"));
				Game1.drawDialogue(this);
			}
			return true;
		}
		if (Game1.player.team.specialOrders != null)
		{
			foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
			{
				if (specialOrder.onItemDelivered == null)
				{
					continue;
				}
				Delegate[] invocationList = specialOrder.onItemDelivered.GetInvocationList();
				for (int i = 0; i < invocationList.Length; i++)
				{
					if (((Func<Farmer, NPC, Item, bool, int>)invocationList[i])(Game1.player, this, activeObj, probe) > 0)
					{
						if (!probe && activeObj.Stack <= 0)
						{
							who.ActiveObject = null;
							who.showNotCarrying();
						}
						return true;
					}
				}
			}
		}
		if (who.NotifyQuests((Quest quest) => quest.OnItemOfferedToNpc(this, activeObj, probe), onlyOneQuest: true))
		{
			if (!probe)
			{
				who.completelyStopAnimatingOrDoingAction();
				if (Game1.random.NextDouble() < 0.3 && base.Name != "Wizard")
				{
					doEmote(32);
				}
			}
			return true;
		}
		switch (who.ActiveObject?.QualifiedItemId)
		{
		case "(O)233":
			if (name.Value == "Jas" && Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && base.currentLocation is Desert && !who.mailReceived.Contains("Jas_IceCream_DF_" + Game1.year))
			{
				if (!probe)
				{
					who.reduceActiveItemByOne();
					jump();
					doEmote(16);
					CurrentDialogue.Clear();
					setNewDialogue("Strings\\1_6_Strings:Jas_IceCream", add: true);
					Game1.drawDialogue(this);
					who.mailReceived.Add("Jas_IceCream_DF_" + Game1.year);
					who.changeFriendship(200, this);
				}
				return true;
			}
			break;
		case "(O)897":
			if (!probe)
			{
				if (base.Name == "Pierre" && !Game1.player.hasOrWillReceiveMail("PierreStocklist"))
				{
					Game1.addMail("PierreStocklist", noLetter: true, sendToEveryone: true);
					who.reduceActiveItemByOne();
					who.completelyStopAnimatingOrDoingAction();
					who.currentLocation.localSound("give_gift");
					Game1.player.team.itemsToRemoveOvernight.Add("897");
					setNewDialogue("Strings\\Characters:PierreStockListDialogue", add: true);
					Game1.drawDialogue(this);
					Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
					{
						Game1.multiplayer.globalChatInfoMessage("StockList");
					});
				}
				else
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_NoTheater", displayName)));
				}
			}
			return true;
		case "(O)71":
			if (base.Name == "Lewis" && who.hasQuest("102"))
			{
				if (!probe)
				{
					if (who.currentLocation?.NameOrUniqueName == "IslandSouth")
					{
						Game1.player.activeDialogueEvents["lucky_pants_lewis"] = 28;
					}
					who.completeQuest("102");
					string[] rawQuestFields = Quest.GetRawQuestFields("102");
					Dialogue dialogue2 = new Dialogue(this, null, ArgUtility.Get(rawQuestFields, 9, "Data\\ExtraDialogue:LostItemQuest_DefaultThankYou", allowBlank: false));
					setNewDialogue(dialogue2);
					Game1.drawDialogue(this);
					Game1.player.changeFriendship(250, this);
					who.ActiveObject = null;
				}
				return true;
			}
			return false;
		case "(O)867":
		case "(O)864":
		case "(O)865":
		case "(O)866":
		case "(O)868":
		case "(O)869":
		case "(O)870":
			if (who.hasQuest("130"))
			{
				Dialogue dialogue = TryGetDialogue("accept_" + activeObj.ItemId);
				if (dialogue != null)
				{
					if (!probe)
					{
						setNewDialogue(dialogue);
						Game1.drawDialogue(this);
						CurrentDialogue.Peek().onFinish = delegate
						{
							Object o = ItemRegistry.Create<Object>("(O)" + (activeObj.ParentSheetIndex + 1));
							o.specialItem = true;
							o.questItem.Value = true;
							who.reduceActiveItemByOne();
							DelayedAction.playSoundAfterDelay("coin", 200);
							DelayedAction.functionAfterDelay(delegate
							{
								who.addItemByMenuIfNecessary(o);
							}, 200);
							Game1.player.freezePause = 550;
							DelayedAction.functionAfterDelay(delegate
							{
								Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1919", o.DisplayName, Lexicon.getProperArticleForWord(o.DisplayName)));
							}, 550);
						};
						faceTowardFarmerForPeriod(6000, 4, faceAway: false, who);
					}
					return true;
				}
				dialogue = TryGetDialogue("reject_" + activeObj.ItemId);
				if (dialogue != null)
				{
					if (!probe)
					{
						setNewDialogue(dialogue);
						Game1.drawDialogue(this);
					}
					return true;
				}
			}
			return false;
		}
		if (activeObj.questItem.Value)
		{
			return false;
		}
		Dialogue dialogue3 = TryGetDialogue("RejectItem_" + activeObj.QualifiedItemId) ?? (from tag in activeObj.GetContextTags()
			select TryGetDialogue("RejectItem_" + tag)).FirstOrDefault((Dialogue p) => p != null) ?? (activeObj.HasTypeObject() ? TryGetDialogue("reject_" + activeObj.ItemId) : null);
		if (dialogue3 != null)
		{
			if (!probe)
			{
				setNewDialogue(dialogue3);
				Game1.drawDialogue(this);
			}
			return true;
		}
		who.friendshipData.TryGetValue(base.Name, out var value);
		bool flag = CanReceiveGifts();
		switch (activeObj.QualifiedItemId)
		{
		case "(O)809":
		{
			if (!Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheater"))
			{
				if (!probe)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_NoTheater", displayName)));
				}
				return true;
			}
			if (SpeaksDwarvish() && !who.canUnderstandDwarves)
			{
				if (!probe)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_NoTheater", displayName)));
				}
				return true;
			}
			string text2 = base.Name;
			if (!(text2 == "Krobus"))
			{
				if (text2 == "Leo" && !Game1.MasterPlayer.mailReceived.Contains("leoMoved"))
				{
					if (!probe)
					{
						Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_NoTheater", displayName)));
					}
					return true;
				}
			}
			else if (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth) == "Fri")
			{
				if (!probe)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_NoTheater", displayName)));
				}
				return true;
			}
			if (!IsVillager || !CanSocialize)
			{
				if (!probe)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_CantInvite", displayName)));
				}
				return true;
			}
			if (value == null)
			{
				if (!probe)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_NoTheater", displayName)));
				}
				return true;
			}
			if (value.IsDivorced())
			{
				if (!probe)
				{
					if (who == Game1.player)
					{
						Game1.multiplayer.globalChatInfoMessage("MovieInviteReject", Game1.player.displayName, GetTokenizedDisplayName());
					}
					CurrentDialogue.Push(TryGetDialogue("RejectMovieTicket_Divorced") ?? TryGetDialogue("RejectMovieTicket") ?? new Dialogue(this, "Strings\\Characters:Divorced_gift"));
					Game1.drawDialogue(this);
				}
				return true;
			}
			if (who.lastSeenMovieWeek.Value >= Game1.Date.TotalWeeks)
			{
				if (!probe)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_FarmerAlreadySeen")));
				}
				return true;
			}
			if (Utility.isFestivalDay())
			{
				if (!probe)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_Festival")));
				}
				return true;
			}
			if (Game1.timeOfDay > 2100)
			{
				if (!probe)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_Closed")));
				}
				return true;
			}
			foreach (MovieInvitation movieInvitation in who.team.movieInvitations)
			{
				if (movieInvitation.farmer == who)
				{
					if (!probe)
					{
						Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_AlreadyInvitedSomeone", movieInvitation.invitedNPC.displayName)));
					}
					return true;
				}
			}
			if (!probe)
			{
				faceTowardFarmerForPeriod(4000, 3, faceAway: false, who);
			}
			foreach (MovieInvitation movieInvitation2 in who.team.movieInvitations)
			{
				if (movieInvitation2.invitedNPC != this)
				{
					continue;
				}
				if (!probe)
				{
					if (who == Game1.player)
					{
						Game1.multiplayer.globalChatInfoMessage("MovieInviteReject", Game1.player.displayName, GetTokenizedDisplayName());
					}
					CurrentDialogue.Push(TryGetDialogue("RejectMovieTicket_AlreadyInvitedBySomeoneElse", movieInvitation2.farmer.displayName) ?? TryGetDialogue("RejectMovieTicket") ?? new Dialogue(this, "Strings\\Characters:MovieInvite_InvitedBySomeoneElse", GetDispositionModifiedString("Strings\\Characters:MovieInvite_InvitedBySomeoneElse", movieInvitation2.farmer.displayName)));
					Game1.drawDialogue(this);
				}
				return true;
			}
			if (lastSeenMovieWeek.Value >= Game1.Date.TotalWeeks)
			{
				if (!probe)
				{
					if (who == Game1.player)
					{
						Game1.multiplayer.globalChatInfoMessage("MovieInviteReject", Game1.player.displayName, GetTokenizedDisplayName());
					}
					CurrentDialogue.Push(TryGetDialogue("RejectMovieTicket_AlreadyWatchedThisWeek") ?? TryGetDialogue("RejectMovieTicket") ?? new Dialogue(this, "Strings\\Characters:MovieInvite_AlreadySeen", GetDispositionModifiedString("Strings\\Characters:MovieInvite_AlreadySeen")));
					Game1.drawDialogue(this);
				}
				return true;
			}
			if (MovieTheater.GetResponseForMovie(this) == "reject")
			{
				if (!probe)
				{
					if (who == Game1.player)
					{
						Game1.multiplayer.globalChatInfoMessage("MovieInviteReject", Game1.player.displayName, GetTokenizedDisplayName());
					}
					CurrentDialogue.Push(TryGetDialogue("RejectMovieTicket_DontWantToSeeThatMovie") ?? TryGetDialogue("RejectMovieTicket") ?? new Dialogue(this, "Strings\\Characters:MovieInvite_Reject", GetDispositionModifiedString("Strings\\Characters:MovieInvite_Reject")));
					Game1.drawDialogue(this);
				}
				return true;
			}
			if (!probe)
			{
				CurrentDialogue.Push(((getSpouse() == who) ? StardewValley.Dialogue.TryGetDialogue(this, "Strings\\Characters:MovieInvite_Spouse_" + name.Value) : null) ?? TryGetDialogue("MovieInvitation") ?? new Dialogue(this, "Strings\\Characters:MovieInvite_Invited", GetDispositionModifiedString("Strings\\Characters:MovieInvite_Invited")));
				Game1.drawDialogue(this);
				who.reduceActiveItemByOne();
				who.completelyStopAnimatingOrDoingAction();
				who.currentLocation.localSound("give_gift");
				MovieTheater.Invite(who, this);
				if (who == Game1.player)
				{
					Game1.multiplayer.globalChatInfoMessage("MovieInviteAccept", Game1.player.displayName, GetTokenizedDisplayName());
				}
			}
			return true;
		}
		case "(O)458":
			if (flag)
			{
				bool flag5 = who.spouse != base.Name && isMarriedOrEngaged();
				if (!datable.Value || flag5)
				{
					if (!probe)
					{
						if (Game1.random.NextBool())
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3955", displayName));
						}
						else
						{
							CurrentDialogue.Push(((!datable.Value) ? TryGetDialogue("RejectBouquet_NotDatable") : null) ?? (flag5 ? TryGetDialogue("RejectBouquet_NpcAlreadyMarried", getSpouse()?.Name) : null) ?? TryGetDialogue("RejectBouquet") ?? (Game1.random.NextBool() ? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.3956") : new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.3957", isGendered: true)));
							Game1.drawDialogue(this);
						}
					}
					return true;
				}
				if (value == null)
				{
					value = (who.friendshipData[base.Name] = new Friendship());
				}
				if (value.IsDating())
				{
					if (!probe)
					{
						Dialogue dialogue6 = TryGetDialogue($"RejectBouquet_AlreadyAccepted_{value.Status}") ?? TryGetDialogue("RejectBouquet_AlreadyAccepted");
						if (dialogue6 != null)
						{
							CurrentDialogue.Push(dialogue6);
							Game1.drawDialogue(this);
						}
						else
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:AlreadyDatingBouquet", displayName));
						}
					}
					return true;
				}
				if (value.IsDivorced())
				{
					if (!probe)
					{
						CurrentDialogue.Push(TryGetDialogue("RejectBouquet_Divorced") ?? TryGetDialogue("RejectBouquet") ?? new Dialogue(this, "Strings\\Characters:Divorced_bouquet"));
						Game1.drawDialogue(this);
					}
					return true;
				}
				if (value.Points < 1000)
				{
					if (!probe)
					{
						CurrentDialogue.Push(TryGetDialogue("RejectBouquet_VeryLowHearts") ?? TryGetDialogue("RejectBouquet") ?? (Game1.random.NextBool() ? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.3958") : new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.3959", isGendered: true)));
						Game1.drawDialogue(this);
					}
					return true;
				}
				if (value.Points < 2000)
				{
					if (!probe)
					{
						CurrentDialogue.Push(TryGetDialogue("RejectBouquet_LowHearts") ?? TryGetDialogue("RejectBouquet") ?? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs." + Game1.random.Choose("3960", "3961")));
						Game1.drawDialogue(this);
					}
					return true;
				}
				if (!probe)
				{
					value.Status = FriendshipStatus.Dating;
					Game1.multiplayer.globalChatInfoMessage("Dating", Game1.player.Name, GetTokenizedDisplayName());
					CurrentDialogue.Push(TryGetDialogue("AcceptBouquet") ?? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs." + Game1.random.Choose("3962", "3963"), isGendered: true));
					who.autoGenerateActiveDialogueEvent("dating_" + base.Name);
					who.autoGenerateActiveDialogueEvent("dating");
					who.changeFriendship(25, this);
					who.reduceActiveItemByOne();
					who.completelyStopAnimatingOrDoingAction();
					doEmote(20);
					Game1.drawDialogue(this);
				}
				return true;
			}
			return false;
		case "(O)277":
			if (flag)
			{
				if (!probe)
				{
					if (!datable.Value || value == null || !value.IsDating() || (value != null && value.IsMarried()))
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Wilted_Bouquet_Meaningless", displayName));
					}
					else
					{
						Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Wilted_Bouquet_Effect", displayName));
						Game1.multiplayer.globalChatInfoMessage("BreakUp", Game1.player.Name, GetTokenizedDisplayName());
						who.removeDatingActiveDialogueEvents(base.Name);
						who.reduceActiveItemByOne();
						value.Status = FriendshipStatus.Friendly;
						if (who.spouse == base.Name)
						{
							who.spouse = null;
						}
						value.WeddingDate = null;
						who.completelyStopAnimatingOrDoingAction();
						value.Points = Math.Min(value.Points, 1250);
						switch (name.Value)
						{
						case "Maru":
						case "Haley":
							doEmote(12);
							break;
						default:
							doEmote(28);
							break;
						case "Shane":
						case "Alex":
							break;
						}
						CurrentDialogue.Clear();
						CurrentDialogue.Push(new Dialogue(this, "Characters\\Dialogue\\" + GetDialogueSheetName() + ":breakUp"));
						Game1.drawDialogue(this);
					}
				}
				return true;
			}
			return false;
		case "(O)460":
			if (flag)
			{
				bool flag4 = value?.IsDivorced() ?? false;
				if (who.spouse == base.Name)
				{
					Dialogue dialogue5 = TryGetDialogue($"RejectMermaidPendant_AlreadyAccepted_{value?.Status}") ?? TryGetDialogue("RejectMermaidPendant_AlreadyAccepted");
					if (!probe && dialogue5 != null)
					{
						CurrentDialogue.Push(dialogue5);
						Game1.drawDialogue(this);
					}
					return dialogue5 != null;
				}
				if (who.isMarriedOrRoommates() || who.isEngaged())
				{
					if (!probe)
					{
						if (who.hasCurrentOrPendingRoommate())
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:TriedToMarryButKrobus"));
						}
						else if (who.isEngaged())
						{
							CurrentDialogue.Push(TryGetDialogue("RejectMermaidPendant_PlayerWithSomeoneElse", who.getSpouse()?.displayName ?? who.spouse) ?? TryGetDialogue("RejectMermaidPendant") ?? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs." + Game1.random.Choose("3965", "3966"), isGendered: true));
							Game1.drawDialogue(this);
						}
						else
						{
							CurrentDialogue.Push(TryGetDialogue("RejectMermaidPendant_PlayerWithSomeoneElse") ?? TryGetDialogue("RejectMermaidPendant") ?? (Game1.random.NextBool() ? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.3967") : new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.3968", isGendered: true)));
							Game1.drawDialogue(this);
						}
					}
					return true;
				}
				if (!datable.Value || isMarriedOrEngaged() || flag4 || (value != null && value.Points < 1500))
				{
					if (!probe)
					{
						if (Game1.random.NextBool())
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3969", displayName));
						}
						else
						{
							CurrentDialogue.Push(((!datable.Value) ? TryGetDialogue("RejectMermaidPendant_NotDatable") : null) ?? (flag4 ? TryGetDialogue("RejectMermaidPendant_Divorced") : null) ?? (isMarriedOrEngaged() ? TryGetDialogue("RejectMermaidPendant_NpcWithSomeoneElse", getSpouse()?.Name) : null) ?? ((datable.Value && value != null && value.Points < 1500) ? TryGetDialogue("RejectMermaidPendant_Under8Hearts") : null) ?? TryGetDialogue("RejectMermaidPendant") ?? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs." + ((Gender == Gender.Female) ? "3970" : "3971")));
							Game1.drawDialogue(this);
						}
					}
					return true;
				}
				if (datable.Value && value != null && value.Points < 2500)
				{
					if (!probe)
					{
						if (!value.ProposalRejected)
						{
							CurrentDialogue.Push(TryGetDialogue("RejectMermaidPendant_Under10Hearts") ?? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs." + Game1.random.Choose("3972", "3973")));
							Game1.drawDialogue(this);
							who.changeFriendship(-20, this);
							value.ProposalRejected = true;
						}
						else
						{
							CurrentDialogue.Push(TryGetDialogue("RejectMermaidPendant_Under10Hearts_AskedAgain") ?? TryGetDialogue("RejectMermaidPendant_Under10Hearts") ?? TryGetDialogue("RejectMermaidPendant") ?? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs." + Game1.random.Choose("3974", "3975"), isGendered: true));
							Game1.drawDialogue(this);
							who.changeFriendship(-50, this);
						}
					}
					return true;
				}
				if (datable.Value && who.houseUpgradeLevel.Value < 1)
				{
					if (!probe)
					{
						if (Game1.random.NextBool())
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3969", displayName));
						}
						else
						{
							CurrentDialogue.Push(TryGetDialogue("RejectMermaidPendant_NeedHouseUpgrade") ?? TryGetDialogue("RejectMermaidPendant") ?? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.3972"));
							Game1.drawDialogue(this);
						}
					}
					return true;
				}
				if (!probe)
				{
					engagementResponse(who);
				}
				return true;
			}
			return false;
		default:
		{
			if (flag && activeObj.HasContextTag(ItemContextTagManager.SanitizeContextTag("propose_roommate_" + base.Name)))
			{
				string text = null;
				object[] array = null;
				bool flag2 = base.Name != "Krobus";
				if (who.spouse == base.Name)
				{
					text = "RejectRoommateProposal_AlreadyAccepted";
					flag2 = false;
				}
				else if (isMarriedOrEngaged())
				{
					text = "RejectRoommateProposal_NpcWithSomeoneElse";
				}
				else if (who.isMarriedOrRoommates() || who.isEngaged())
				{
					text = "RejectRoommateProposal_PlayerWithSomeoneElse";
					array = new object[1] { who.getSpouse()?.displayName ?? who.spouse };
				}
				else if (who.getFriendshipHeartLevelForNPC(base.Name) < 10)
				{
					text = "RejectRoommateProposal_LowFriendship";
				}
				else if (who.houseUpgradeLevel.Value < 1)
				{
					text = "RejectRoommateProposal_SmallHouse";
				}
				if (text != null)
				{
					Dialogue dialogue4 = ((array != null) ? TryGetDialogue(text, array) : TryGetDialogue(text)) ?? TryGetDialogue("RejectRoommateProposal");
					if (!probe)
					{
						if (dialogue4 != null)
						{
							CurrentDialogue.Push(dialogue4);
							Game1.drawDialogue(this);
						}
						else if (flag2)
						{
							Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieInvite_NoTheater", displayName)));
						}
					}
					return dialogue4 != null || flag2;
				}
				if (!probe)
				{
					engagementResponse(who, asRoommate: true);
				}
				return true;
			}
			bool flag3 = ItemContextTagManager.HasBaseTag(activeObj.QualifiedItemId, "not_giftable");
			if (flag && activeObj.canBeGivenAsGift() && !flag3)
			{
				foreach (string key in who.activeDialogueEvents.Keys)
				{
					if (key.Contains("dumped") && Dialogue.ContainsKey(key))
					{
						if (!probe)
						{
							doEmote(12);
						}
						return true;
					}
				}
				if (!probe)
				{
					who.completeQuest("25");
				}
				if (Game1.IsGreenRainingHere() && Game1.year == 1 && !isMarried())
				{
					if (!probe)
					{
						Game1.showRedMessage(".........");
					}
					return false;
				}
				if ((value != null && value.GiftsThisWeek < 2) || who.spouse == base.Name || this is Child || isBirthday() || who.ActiveObject.QualifiedItemId == "(O)StardropTea")
				{
					if (!probe)
					{
						if (value == null)
						{
							value = (who.friendshipData[base.Name] = new Friendship());
						}
						if (value.IsDivorced())
						{
							CurrentDialogue.Push(TryGetDialogue("RejectGift_Divorced") ?? new Dialogue(this, "Strings\\Characters:Divorced_gift"));
							Game1.drawDialogue(this);
							return true;
						}
						if (value.GiftsToday == 1 && who.ActiveObject.QualifiedItemId != "(O)StardropTea")
						{
							Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3981", displayName)));
							return true;
						}
						receiveGift(who.ActiveObject, who, who.ActiveObject.QualifiedItemId != "(O)StardropTea");
						who.reduceActiveItemByOne();
						who.completelyStopAnimatingOrDoingAction();
						faceTowardFarmerForPeriod(4000, 3, faceAway: false, who);
						if (datable.Value && who.spouse != null && who.spouse != base.Name && !who.hasCurrentOrPendingRoommate() && Utility.isMale(who.spouse) == Utility.isMale(base.Name) && Game1.random.NextDouble() < 0.3 - (double)((float)who.LuckLevel / 100f) - who.DailyLuck && !isBirthday() && value.IsDating())
						{
							NPC characterFromName = Game1.getCharacterFromName(who.spouse);
							CharacterData characterData = characterFromName?.GetData();
							if (characterFromName != null && GameStateQuery.CheckConditions(characterData?.SpouseGiftJealousy, null, who, activeObj))
							{
								who.changeFriendship(characterData?.SpouseGiftJealousyFriendshipChange ?? (-30), characterFromName);
								characterFromName.CurrentDialogue.Clear();
								characterFromName.CurrentDialogue.Push(characterFromName.TryGetDialogue("SpouseGiftJealous", displayName, activeObj.DisplayName) ?? StardewValley.Dialogue.FromTranslation(characterFromName, "Strings\\StringsFromCSFiles:NPC.cs.3985", displayName));
							}
						}
					}
					return true;
				}
				if (!probe)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3987", displayName, 2)));
				}
				return true;
			}
			return false;
		}
		}
	}

	public string GetDispositionModifiedString(string path, params object[] substitutions)
	{
		List<string> list = new List<string>();
		list.Add(name.Value);
		if (Game1.player.isMarriedOrRoommates() && Game1.player.getSpouse() == this)
		{
			list.Add("spouse");
		}
		CharacterData data = GetData();
		if (data != null)
		{
			list.Add(data.Manner.ToString().ToLower());
			list.Add(data.SocialAnxiety.ToString().ToLower());
			list.Add(data.Optimism.ToString().ToLower());
			list.Add(data.Age.ToString().ToLower());
		}
		foreach (string item in list)
		{
			string text = path + "_" + Utility.capitalizeFirstLetter(item);
			string text2 = Game1.content.LoadString(text, substitutions);
			if (!(text2 == text))
			{
				return text2;
			}
		}
		return Game1.content.LoadString(path, substitutions);
	}

	public void haltMe(Farmer who)
	{
		Halt();
	}

	public virtual bool checkAction(Farmer who, GameLocation l)
	{
		if (IsInvisible)
		{
			return false;
		}
		if (isSleeping.Value)
		{
			if (!isEmoting)
			{
				doEmote(24);
			}
			shake(250);
			return false;
		}
		if (!who.CanMove)
		{
			return false;
		}
		Game1.player.friendshipData.TryGetValue(base.Name, out var value);
		if (base.Name.Equals("Henchman") && l.Name.Equals("WitchSwamp"))
		{
			if (Game1.player.mailReceived.Add("Henchman1"))
			{
				CurrentDialogue.Push(new Dialogue(this, "Strings\\Characters:Henchman1"));
				Game1.drawDialogue(this);
				Game1.player.addQuest("27");
				if (!Game1.player.friendshipData.ContainsKey("Henchman"))
				{
					Game1.player.friendshipData.Add("Henchman", value = new Friendship());
				}
			}
			else
			{
				if (who.ActiveObject != null && !who.isRidingHorse() && tryToReceiveActiveObject(who))
				{
					return true;
				}
				if (controller == null)
				{
					if (StardewValley.Pokemon.PlayerParty.Instance.HasChosenStarter)
					{
						Response[] goblinChoices = new Response[2]
						{
							new Response("Talk", "Conversar"),
							new Response("Battle", "⚔️ Desafiar o Goblin para uma Batalha Pokémon!")
						};
						l.createQuestionDialogue("Grrr... *O Goblin olha fixamente para você com olhos brilhantes e segura uma Pokébola sombria.*", goblinChoices, delegate(Farmer f, string whichAnswer)
						{
							if (whichAnswer == "Battle")
							{
								var activePoke = StardewValley.Pokemon.PlayerParty.Instance.ActivePokemon;
								if (activePoke == null || activePoke.IsFainted)
								{
									Game1.drawObjectDialogue("Todos os seus Pokémon estão desmaiados! Cure-os antes de enfrentar o Goblin.");
								}
								else
								{
									var enemyTeam = StardewValley.Pokemon.NPCTeams.GetTeam("Henchman");
									Game1.activeClickableMenu = new StardewValley.Pokemon.PokemonBattleMenu("Goblin do Pântano", enemyTeam);
								}
							}
							else
							{
								CurrentDialogue.Push(new Dialogue(this, "Strings\\Characters:Henchman2"));
								Game1.drawDialogue(this);
							}
						}, this);
					}
					else
					{
						CurrentDialogue.Push(new Dialogue(this, "Strings\\Characters:Henchman2"));
						Game1.drawDialogue(this);
					}
				}
			}
			return true;
		}
		bool flag = who.pantsItem.Value?.QualifiedItemId == "(P)15" && (base.Name == "Lewis" || base.Name == "Marnie");
		if (CanReceiveGifts() && value == null)
		{
			Game1.player.friendshipData.Add(base.Name, value = new Friendship(0));
			if (base.Name.Equals("Krobus"))
			{
				CurrentDialogue.Push(new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.3990"));
				Game1.drawDialogue(this);
				return true;
			}
		}
		if (who.NotifyQuests((Quest quest) => quest.OnNpcSocialized(this)) && Game1.dialogueUp)
		{
			faceTowardFarmerForPeriod(6000, 3, faceAway: false, who);
			return true;
		}
		if (base.Name.Equals("Krobus") && who.hasQuest("28"))
		{
			CurrentDialogue.Push(new Dialogue(this, (l is Sewer) ? "Strings\\Characters:KrobusDarkTalisman" : "Strings\\Characters:KrobusDarkTalisman_elsewhere"));
			Game1.drawDialogue(this);
			who.removeQuest("28");
			who.mailReceived.Add("krobusUnseal");
			if (l is Sewer)
			{
				DelayedAction.addTemporarySpriteAfterDelay(new TemporaryAnimatedSprite("TileSheets\\Projectiles", new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 16), 3000f, 1, 0, new Vector2(31f, 17f) * 64f, flicker: false, flipped: false)
				{
					scale = 4f,
					delayBeforeAnimationStart = 1,
					startSound = "debuffSpell",
					motion = new Vector2(-9f, 1f),
					rotationChange = (float)Math.PI / 64f,
					lightId = "Krobus_Unseal_1",
					lightRadius = 1f,
					lightcolor = new Color(150, 0, 50),
					layerDepth = 1f,
					alphaFade = 0.003f
				}, l, 200, waitUntilMenusGone: true);
				DelayedAction.addTemporarySpriteAfterDelay(new TemporaryAnimatedSprite("TileSheets\\Projectiles", new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 16), 3000f, 1, 0, new Vector2(31f, 17f) * 64f, flicker: false, flipped: false)
				{
					startSound = "debuffSpell",
					delayBeforeAnimationStart = 1,
					scale = 4f,
					motion = new Vector2(-9f, 1f),
					rotationChange = (float)Math.PI / 64f,
					lightId = "Krobus_Unseal_2",
					lightRadius = 1f,
					lightcolor = new Color(150, 0, 50),
					layerDepth = 1f,
					alphaFade = 0.003f
				}, l, 700, waitUntilMenusGone: true);
			}
			return true;
		}
		if (name.Value == "Jas" && base.currentLocation is Desert && who.mailReceived.Contains("Jas_IceCream_DF_" + Game1.year))
		{
			doEmote(32);
			return true;
		}
		if (base.Name == who.spouse && who.IsLocalPlayer && Sprite.CurrentAnimation == null)
		{
			faceDirection(-3);
			if (value != null && value.Points >= 3125 && who.mailReceived.Add("CF_Spouse"))
			{
				CurrentDialogue.Push(TryGetDialogue("SpouseStardrop") ?? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4001"));
				Object obj = ItemRegistry.Create<Object>("(O)434");
				obj.CanBeSetDown = false;
				obj.CanBeGrabbed = false;
				Game1.player.addItemByMenuIfNecessary(obj);
				shouldSayMarriageDialogue.Value = false;
				currentMarriageDialogue.Clear();
				return true;
			}
			if (!hasTemporaryMessageAvailable() && currentMarriageDialogue.Count == 0 && CurrentDialogue.Count == 0 && Game1.timeOfDay < 2200 && !isMoving() && who.ActiveObject == null)
			{
				if (faceTowardFarmerTimer <= 0)
				{
					facingDirectionBeforeSpeakingToPlayer.Value = FacingDirection;
				}
				faceGeneralDirection(who.getStandingPosition(), 0, opposite: false, useTileCalculations: false);
				who.faceGeneralDirection(getStandingPosition(), 0, opposite: false, useTileCalculations: false);
				if (FacingDirection == 3 || FacingDirection == 1)
				{
					CharacterData data = GetData();
					int frame = data?.KissSpriteIndex ?? 28;
					bool flag2 = data?.KissSpriteFacingRight ?? true;
					bool flag3 = flag2 != (FacingDirection == 1);
					if (who.getFriendshipHeartLevelForNPC(base.Name) > 9 && sleptInBed.Value)
					{
						int milliseconds = (movementPause = (Game1.IsMultiplayer ? 1000 : 10));
						faceTowardFarmerForPeriod(3000, 3, faceAway: false, who);
						Sprite.ClearAnimation();
						Sprite.AddFrame(new FarmerSprite.AnimationFrame(frame, milliseconds, secondaryArm: false, flag3, haltMe, behaviorAtEndOfFrame: true));
						if (!hasBeenKissedToday.Value)
						{
							who.changeFriendship(10, this);
							if (who.hasCurrentOrPendingRoommate())
							{
								Game1.multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite("LooseSprites\\emojis", new Microsoft.Xna.Framework.Rectangle(0, 0, 9, 9), 2000f, 1, 0, base.Tile * 64f + new Vector2(16f, -64f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
								{
									motion = new Vector2(0f, -0.5f),
									alphaFade = 0.01f
								});
							}
							else
							{
								Game1.multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(211, 428, 7, 6), 2000f, 1, 0, base.Tile * 64f + new Vector2(16f, -64f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
								{
									motion = new Vector2(0f, -0.5f),
									alphaFade = 0.01f
								});
							}
							l.playSound("dwop", null, null, SoundContext.NPC);
							who.exhausted.Value = false;
						}
						else if (Game1.random.NextDouble() < 0.1)
						{
							doEmote(20);
						}
						hasBeenKissedToday.Value = true;
						Sprite.UpdateSourceRect();
					}
					else
					{
						faceDirection(Game1.random.Choose(2, 0));
						doEmote(12);
					}
					int num = 1;
					if ((flag2 && !flag3) || (!flag2 && flag3))
					{
						num = 3;
					}
					who.PerformKiss(num);
					return true;
				}
				if (faceTowardFarmerTimer <= 0 && Game1.random.NextDouble() < 0.1)
				{
					Game1.playSound("dwop");
					if (who.getFriendshipHeartLevelForNPC(base.Name) > 9)
					{
						doEmote((Game1.random.NextDouble() < 0.5) ? 32 : 20);
					}
					else if (who.getFriendshipHeartLevelForNPC(base.Name) > 7)
					{
						doEmote((Game1.random.NextDouble() < 0.5) ? 40 : 8);
					}
					else
					{
						doEmote((Game1.random.NextDouble() < 0.5) ? 28 : 12);
					}
				}
				else if (facingDirectionBeforeSpeakingToPlayer.Value >= 0 && Math.Abs(facingDirectionBeforeSpeakingToPlayer.Value - FacingDirection) == 2 && Game1.random.NextDouble() < 0.1)
				{
					jump();
					doEmote(16);
				}
				faceTowardFarmerForPeriod(3000, 4, faceAway: false, who);
			}
		}
		if (base.SimpleNonVillagerNPC)
		{
			if (name.Value == "Fizz")
			{
				int perfectionWaivers = Game1.netWorldState.Value.PerfectionWaivers;
				if (Utility.percentGameComplete() + (float)perfectionWaivers * 0.01f >= 1f)
				{
					doEmote(56);
					shakeTimer = 250;
				}
				else
				{
					CurrentDialogue.Clear();
					if (!Game1.player.mailReceived.Contains("FizzFirstDialogue"))
					{
						Game1.player.mailReceived.Add("FizzFirstDialogue");
						CurrentDialogue.Push(new Dialogue(this, "Strings\\1_6_Strings:Fizz_Intro_1"));
						Game1.drawDialogue(this);
					}
					else
					{
						CurrentDialogue.Push(new Dialogue(this, "Strings\\1_6_Strings:Fizz_Intro_2"));
						Game1.drawDialogue(this);
						Game1.afterDialogues = delegate
						{
							Game1.currentLocation.createQuestionDialogue("", new Response[2]
							{
								new Response("Yes", Game1.content.LoadString("Strings\\1_6_Strings:Fizz_Yes")).SetHotKey(Keys.Y),
								new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")).SetHotKey(Keys.Escape)
							}, "Fizz");
						};
					}
				}
			}
			else
			{
				string text = "Strings\\SimpleNonVillagerDialogues:" + base.Name;
				string text2 = Game1.content.LoadString(text);
				if (text2 != text)
				{
					string[] array = text2.Split("||");
					if (nonVillagerNPCTimesTalked != -1 && nonVillagerNPCTimesTalked < array.Length)
					{
						Game1.drawObjectDialogue(array[nonVillagerNPCTimesTalked]);
						nonVillagerNPCTimesTalked++;
						if (nonVillagerNPCTimesTalked >= array.Length)
						{
							nonVillagerNPCTimesTalked = -1;
						}
					}
				}
			}
			return true;
		}
		bool flag4 = false;
		if (value != null)
		{
			if (getSpouse() == Game1.player && shouldSayMarriageDialogue.Value && currentMarriageDialogue.Count > 0 && currentMarriageDialogue.Count > 0)
			{
				while (currentMarriageDialogue.Count > 0)
				{
					MarriageDialogueReference marriageDialogueReference = currentMarriageDialogue[currentMarriageDialogue.Count - 1];
					if (marriageDialogueReference == marriageDefaultDialogue.Value)
					{
						marriageDefaultDialogue.Value = null;
					}
					currentMarriageDialogue.RemoveAt(currentMarriageDialogue.Count - 1);
					CurrentDialogue.Push(marriageDialogueReference.GetDialogue(this));
				}
				flag4 = true;
			}
			if (!flag4)
			{
				flag4 = checkForNewCurrentDialogue(value.Points / 250);
				if (!flag4)
				{
					flag4 = checkForNewCurrentDialogue(value.Points / 250, noPreface: true);
				}
			}
		}
		if (who.IsLocalPlayer && value != null && (endOfRouteMessage.Value != null || flag4 || (base.currentLocation != null && base.currentLocation.HasLocationOverrideDialogue(this))))
		{
			if (!flag4 && setTemporaryMessages(who))
			{
				who.NotifyQuests((Quest quest) => quest.OnNpcSocialized(this));
				return false;
			}
			Texture2D texture = Sprite.Texture;
			if (texture != null && texture.Bounds.Height > 32 && (CurrentDialogue.Count <= 0 || !CurrentDialogue.Peek().dontFaceFarmer))
			{
				faceTowardFarmerForPeriod(5000, 4, faceAway: false, who);
			}
			if (who.ActiveObject != null && !who.isRidingHorse() && tryToReceiveActiveObject(who))
			{
				who.NotifyQuests((Quest quest) => quest.OnNpcSocialized(this));
				faceTowardFarmerForPeriod(3000, 4, faceAway: false, who);
				return true;
			}
			grantConversationFriendship(who);
			Game1.drawDialogue(this);
			return true;
		}
		if (canTalk() && who.hasClubCard && base.Name.Equals("Bouncer") && who.IsLocalPlayer)
		{
			Response[] answerChoices = new Response[2]
			{
				new Response("Yes.", Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4018")),
				new Response("That's", Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4020"))
			};
			l.createQuestionDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4021"), answerChoices, "ClubCard");
		}
		else if (canTalk() && who.IsLocalPlayer && StardewValley.Pokemon.NPCTeams.HasTeam(base.Name))
		{
			if (who.ActiveObject != null && !who.isRidingHorse() && tryToReceiveActiveObject(who, probe: true))
			{
				tryToReceiveActiveObject(who);
				return true;
			}
			if (CheckPokemonInteraction(who, l))
			{
				return true;
			}
		}
		else if (canTalk() && CurrentDialogue.Count > 0)
		{
			if (who.ActiveObject != null && !who.isRidingHorse() && tryToReceiveActiveObject(who, probe: true))
			{
				if (who.IsLocalPlayer)
				{
					tryToReceiveActiveObject(who);
				}
				else
				{
					faceTowardFarmerForPeriod(3000, 4, faceAway: false, who);
				}
				return true;
			}
			if (CurrentDialogue.Count >= 1 || endOfRouteMessage.Value != null || (base.currentLocation != null && base.currentLocation.HasLocationOverrideDialogue(this)))
			{
				if (setTemporaryMessages(who))
				{
					who.NotifyQuests((Quest quest) => quest.OnNpcSocialized(this));
					return false;
				}
				Texture2D texture2 = Sprite.Texture;
				if (texture2 != null && texture2.Bounds.Height > 32 && !CurrentDialogue.Peek().dontFaceFarmer)
				{
					faceTowardFarmerForPeriod(5000, 4, faceAway: false, who);
				}
				if (who.IsLocalPlayer)
				{
					grantConversationFriendship(who);
					if (!flag)
					{
						Game1.drawDialogue(this);
						return true;
					}
				}
			}
			else if (!doingEndOfRouteAnimation.Value)
			{
				try
				{
					if (value != null)
					{
						faceTowardFarmerForPeriod(value.Points / 125 * 1000 + 1000, 4, faceAway: false, who);
					}
				}
				catch (Exception)
				{
				}
				if (Game1.random.NextDouble() < 0.1)
				{
					doEmote(8);
				}
			}
		}
		else if (canTalk() && !Game1.game1.wasAskedLeoMemory && Game1.CurrentEvent == null && name.Value == "Leo" && base.currentLocation != null && (base.currentLocation.NameOrUniqueName == "LeoTreeHouse" || base.currentLocation.NameOrUniqueName == "Mountain") && Game1.MasterPlayer.hasOrWillReceiveMail("leoMoved") && GetUnseenLeoEvent().HasValue && CanRevisitLeoMemory(GetUnseenLeoEvent()))
		{
			Game1.DrawDialogue(this, "Strings\\Characters:Leo_Memory");
			Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, new Game1.afterFadeFunction(AskLeoMemoryPrompt));
		}
		else
		{
			if (who.ActiveObject != null && !who.isRidingHorse() && tryToReceiveActiveObject(who))
			{
				faceTowardFarmerForPeriod(3000, 4, faceAway: false, who);
				return true;
			}
			string text3 = base.Name;
			if (!(text3 == "Krobus"))
			{
				if (text3 == "Dwarf" && who.canUnderstandDwarves && l is Mine)
				{
					Utility.TryOpenShopMenu("Dwarf", base.Name);
					return true;
				}
			}
			else if (l is Sewer)
			{
				Utility.TryOpenShopMenu("ShadowShop", "Krobus");
				return true;
			}
		}
		if (flag)
		{
			if (yJumpVelocity != 0f || Sprite.CurrentAnimation != null)
			{
				return true;
			}
			string text3 = base.Name;
			if (!(text3 == "Lewis"))
			{
				if (text3 == "Marnie")
				{
					faceTowardFarmerForPeriod(1000, 3, faceAway: false, who);
					Sprite.ClearAnimation();
					Sprite.AddFrame(new FarmerSprite.AnimationFrame(33, 150, secondaryArm: false, flip: false, delegate
					{
						l.playSound("dustMeep");
					}));
					Sprite.AddFrame(new FarmerSprite.AnimationFrame(34, 180));
					Sprite.AddFrame(new FarmerSprite.AnimationFrame(33, 180, secondaryArm: false, flip: false, delegate
					{
						l.playSound("dustMeep");
					}));
					Sprite.AddFrame(new FarmerSprite.AnimationFrame(34, 180));
					Sprite.AddFrame(new FarmerSprite.AnimationFrame(33, 180, secondaryArm: false, flip: false, delegate
					{
						l.playSound("dustMeep");
					}));
					Sprite.AddFrame(new FarmerSprite.AnimationFrame(34, 180));
					Sprite.AddFrame(new FarmerSprite.AnimationFrame(33, 180, secondaryArm: false, flip: false, delegate
					{
						l.playSound("dustMeep");
					}));
					Sprite.AddFrame(new FarmerSprite.AnimationFrame(34, 180));
					Sprite.loop = false;
				}
			}
			else
			{
				faceTowardFarmerForPeriod(1000, 3, faceAway: false, who);
				jump();
				Sprite.ClearAnimation();
				Sprite.AddFrame(new FarmerSprite.AnimationFrame(26, 1000, secondaryArm: false, flip: false, delegate
				{
					doEmote(12);
				}, behaviorAtEndOfFrame: true));
				Sprite.loop = false;
				shakeTimer = 1000;
				l.playSound("batScreech");
			}
			return true;
		}
		if (setTemporaryMessages(who))
		{
			return false;
		}
		if ((doingEndOfRouteAnimation.Value || !goingToDoEndOfRouteAnimation.Value) && endOfRouteMessage.Value != null)
		{
			Game1.drawDialogue(this);
			return true;
		}
		return false;
	}

	public virtual bool CheckPokemonInteraction(Farmer who, GameLocation l)
	{
		faceTowardFarmerForPeriod(5000, 4, faceAway: false, who);

		if (!StardewValley.Pokemon.PlayerParty.Instance.HasChosenStarter)
		{
			Response[] starters = new Response[7]
			{
				new Response("Bulbasaur", "Bulbasaur (Planta)"),
				new Response("Charmander", "Charmander (Fogo)"),
				new Response("Squirtle", "Squirtle (Água)"),
				new Response("Treecko", "Treecko (Planta)"),
				new Response("Torchic", "Torchic (Fogo)"),
				new Response("Mudkip", "Mudkip (Água)"),
				new Response("Pikachu", "Pikachu (Elétrico)")
			};
			l.createQuestionDialogue("Escolha seu parceiro Pokémon para começar sua jornada em Pelican Town:", starters, delegate(Farmer f, string whichAnswer)
			{
				StardewValley.Pokemon.PlayerParty.Instance.ChooseStarter(whichAnswer);
				Game1.drawObjectDialogue($"Você escolheu {whichAnswer}! Fale novamente com qualquer morador para iniciar uma batalha.");
			}, this);
			return true;
		}

		bool isHarvey = base.Name.Equals("Harvey", StringComparison.OrdinalIgnoreCase);
		bool isDwarf = base.Name.Equals("Dwarf", StringComparison.OrdinalIgnoreCase);
		bool isMarnie = base.Name.Equals("Marnie", StringComparison.OrdinalIgnoreCase);
		Response[] choices = isHarvey
			? new Response[3]
			{
				new Response("Talk", "Conversar"),
				new Response("Battle", "Batalhar Pokémon!"),
				new Response("Heal", "Tratamento Pokémon na Clínica (100g)")
			}
			: (isDwarf
				? new Response[3]
				{
					new Response("Talk", "Conversar"),
					new Response("Battle", "Batalhar Pokémon!"),
					new Response("DwarfHints", "⛏️ Comprar Dicas da Pokédex (Normal/Inicial/Lendário/Mítico)")
				}
				: (isMarnie
					? new Response[3]
					{
						new Response("Talk", "Conversar"),
						new Response("Battle", "Batalhar Pokémon!"),
						new Response("Daycare", "🐣 Creche Pokémon (Cruzamento & Ovos)")
					}
					: new Response[2]
					{
						new Response("Talk", "Conversar"),
						new Response("Battle", "Batalhar Pokémon!")
					}));

		l.createQuestionDialogue($"Olá, {who.Name}! O que gostaria de fazer?", choices, delegate(Farmer f, string whichAnswer)
		{
			if (whichAnswer == "Daycare")
			{
				Game1.activeClickableMenu = new StardewValley.Pokemon.PokemonDaycareMenu();
			}
			else if (whichAnswer == "DwarfHints")
			{
				Game1.activeClickableMenu = new StardewValley.Pokemon.PokemonDwarfHintMenu();
			}
			else if (whichAnswer == "Heal")
			{
				if (who.Money >= 100)
				{
					who.Money -= 100;
					StardewValley.Pokemon.PlayerParty.Instance.HealAll();
					Game1.playSound("heal");
					Game1.drawObjectDialogue("Dr. Harvey: Examinei sua equipe cuidadosamente e cuidei de todos os ferimentos! Seus Pokémon estão com a saúde e energia 100% restauradas.");
				}
				else
				{
					Game1.drawObjectDialogue("Dr. Harvey: O tratamento médico da equipe custa 100g. Você não possui ouro suficiente no momento.");
				}
			}
			else if (whichAnswer == "Battle")
			{
				var activePoke = StardewValley.Pokemon.PlayerParty.Instance.ActivePokemon;
				if (activePoke == null || activePoke.IsFainted)
				{
					Game1.drawObjectDialogue("Todos os seus Pokémon estão desmaiados! Cure-os com o Dr. Harvey, com Revive ou em uma Mesa de Recuperação antes de batalhar.");
				}
				else
				{
					var enemyTeam = StardewValley.Pokemon.NPCTeams.GetTeam(base.Name);
					if (enemyTeam != null && enemyTeam.Count > 0)
					{
						Game1.activeClickableMenu = new StardewValley.Pokemon.PokemonBattleMenu(base.Name, enemyTeam);
					}
					else
					{
						Game1.drawObjectDialogue($"{displayName}: Meu time de Pokémon ainda está treinando!");
					}
				}
			}
			else
			{
				grantConversationFriendship(who);
				var activePoke = StardewValley.Pokemon.PlayerParty.Instance.ActivePokemon;
				string customReaction = GetPokemonReactionDialogue(base.Name, activePoke);
				if (!string.IsNullOrEmpty(customReaction) && Game1.random.NextDouble() < 0.65)
				{
					Game1.drawObjectDialogue($"{displayName}: {customReaction}");
				}
				else if (CurrentDialogue.Count > 0)
				{
					Game1.drawDialogue(this);
				}
				else
				{
					Game1.drawObjectDialogue($"{displayName}: É um lindo dia aqui na Vila Pelicanos!");
				}
			}
		}, this);

		return true;
	}


	public static string GetPokemonReactionDialogue(string npcName, StardewValley.Pokemon.PokemonInstance poke)
	{
		if (poke == null || poke.Species == null) return null;

		string name = npcName?.Trim() ?? "";

		// Shiny Special Reactions
		if (poke.IsShiny)
		{
			if (name.Equals("Haley", StringComparison.OrdinalIgnoreCase))
				return $"Espera... o seu {poke.Nickname} brilha com estrelas douradas?! Isso daria uma foto premiada de capa de revista!";
			if (name.Equals("Emily", StringComparison.OrdinalIgnoreCase))
				return $"Uau! O brilho prismático que emana de {poke.Nickname} tem uma vibração espiritual tão pura e luminosa!";
		}

		if (name.Equals("Clint", StringComparison.OrdinalIgnoreCase))
		{
			if (poke.HasType(StardewValley.Pokemon.PokemonType.Steel) || poke.HasType(StardewValley.Pokemon.PokemonType.Rock))
				return $"Nossa, veja a têmpera metálica desse {poke.Nickname}! Essa blindagem parece mais densa e resistente que o próprio minério de irídio!";
			if (poke.HasType(StardewValley.Pokemon.PokemonType.Fire))
				return $"Esse calor vindo de {poke.Nickname}... se eu tivesse uma chama dessa na fornalha, derreteria barras de ferro em segundos.";
		}
		else if (name.Equals("Abigail", StringComparison.OrdinalIgnoreCase))
		{
			if (poke.HasType(StardewValley.Pokemon.PokemonType.Ghost) || poke.HasType(StardewValley.Pokemon.PokemonType.Dark))
				return $"Uau! O seu {poke.Nickname} tem uma energia espectral fascinante... Ele gosta de explorar o cemitério à noite comigo?";
		}
		else if (name.Equals("Willy", StringComparison.OrdinalIgnoreCase))
		{
			if (poke.HasType(StardewValley.Pokemon.PokemonType.Water))
				return $"Pelas barbas de Netuno! Que exemplar espetacular de {poke.Nickname}! Digno do orgulho de qualquer velho pescador!";
		}
		else if (name.Equals("Maru", StringComparison.OrdinalIgnoreCase))
		{
			if (poke.HasType(StardewValley.Pokemon.PokemonType.Electric) || poke.HasType(StardewValley.Pokemon.PokemonType.Steel))
				return $"Incrível! As oscilações eletromagnéticas que o {poke.Nickname} emite geram uma voltagem perfeitamente limpa. Poderíamos alimentar telescópios inteiros com isso!";
		}
		else if (name.Equals("Demetrius", StringComparison.OrdinalIgnoreCase))
		{
			if (poke.HasType(StardewValley.Pokemon.PokemonType.Dragon) || poke.HasType(StardewValley.Pokemon.PokemonType.Grass))
				return $"Fascinante! A morfologia celular e a biomecânica de {poke.Nickname} são uma verdadeira maravilha da biologia adaptativa do Vale.";
		}
		else if (name.Equals("Linus", StringComparison.OrdinalIgnoreCase))
		{
			return $"O seu {poke.Nickname} parece muito sereno e saudável ao ar livre. É bom ver alguém que trata as criaturas com o carinho que a natureza merece.";
		}
		else if (name.Equals("Wizard", StringComparison.OrdinalIgnoreCase) || name.Equals("Rasmodius", StringComparison.OrdinalIgnoreCase))
		{
			if (poke.HasType(StardewValley.Pokemon.PokemonType.Psychic) || poke.HasType(StardewValley.Pokemon.PokemonType.Fairy))
				return $"Sinto as ressonâncias do plano etéreo convergindo no seu {poke.Nickname}... Os arcanos sussurram com grande respeito ao redor dele.";
		}
		else if (name.Equals("Krobus", StringComparison.OrdinalIgnoreCase))
		{
			if (poke.HasType(StardewValley.Pokemon.PokemonType.Ghost) || poke.HasType(StardewValley.Pokemon.PokemonType.Poison) || poke.HasType(StardewValley.Pokemon.PokemonType.Dark))
				return $"O {poke.Nickname}... ele me olha sem medo no escuro. As criaturas do submundo acolhem seu parceiro com afeto.";
		}
		else if (name.Equals("Penny", StringComparison.OrdinalIgnoreCase))
		{
			return $"Oh, que doce! As crianças da escola com certeza iam adorar conhecer e fazer carinho no {poke.Nickname}!";
		}
		else if (name.Equals("Sebastian", StringComparison.OrdinalIgnoreCase))
		{
			if (poke.HasType(StardewValley.Pokemon.PokemonType.Dark) || poke.HasType(StardewValley.Pokemon.PokemonType.Ghost))
				return $"Gostei do seu {poke.Nickname}. Calmo, discreto e combina perfeitamente com um dia chuvoso.";
		}
		else if (name.Equals("Alex", StringComparison.OrdinalIgnoreCase))
		{
			return $"Olha só o porte e a agilidade do seu {poke.Nickname}! Aposto que ele aguenta uma corrida de 10km na praia comigo.";
		}
		else if (name.Equals("Elliott", StringComparison.OrdinalIgnoreCase))
		{
			if (poke.HasType(StardewValley.Pokemon.PokemonType.Water) || poke.HasType(StardewValley.Pokemon.PokemonType.Dragon))
				return $"Que criatura poética! A postura de {poke.Nickname} flui através do ar como a estrofe mais inspirada de uma balada marítima.";
		}
		else if (name.Equals("Harvey", StringComparison.OrdinalIgnoreCase))
		{
			return $"Fico muito feliz em ver seu {poke.Nickname} tão disposto e saudável. A prevenção é sempre o melhor cuidado médico!";
		}

		return null;
	}

	public void grantConversationFriendship(Farmer who, int amount = 20)
	{
		if (who.hasPlayerTalkedToNPC(base.Name) || !who.friendshipData.TryGetValue(base.Name, out var value))
		{
			return;
		}
		value.TalkedToToday = true;
		who.NotifyQuests((Quest quest) => quest.OnNpcSocialized(this));
		if (!isDivorcedFrom(who))
		{
			if (who.hasBuff("statue_of_blessings_4"))
			{
				amount = 60;
			}
			who.changeFriendship(amount, this);
		}
	}

	public virtual void AskLeoMemoryPrompt()
	{
		GameLocation gameLocation = base.currentLocation;
		Response[] answerChoices = new Response[2]
		{
			new Response("Yes", Game1.content.LoadString("Strings\\Characters:Leo_Memory_Answer_Yes")),
			new Response("No", Game1.content.LoadString("Strings\\Characters:Leo_Memory_Answer_No"))
		};
		string text = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Characters:Leo_Memory_" + GetUnseenLeoEvent().Value.Value);
		if (text == null)
		{
			text = "";
		}
		gameLocation.createQuestionDialogue(text, answerChoices, OnLeoMemoryResponse, this);
	}

	public bool CanRevisitLeoMemory(KeyValuePair<string, string>? event_data)
	{
		if (!event_data.HasValue)
		{
			return false;
		}
		string key = event_data.Value.Key;
		string value = event_data.Value.Value;
		Dictionary<string, string> dictionary;
		try
		{
			dictionary = Game1.content.Load<Dictionary<string, string>>("Data\\Events\\" + key);
		}
		catch
		{
			return false;
		}
		if (dictionary == null)
		{
			return false;
		}
		foreach (string key2 in dictionary.Keys)
		{
			if (Event.SplitPreconditions(key2)[0] == value)
			{
				GameLocation locationFromName = Game1.getLocationFromName(key);
				string text = key2;
				text = text.Replace("/e 1039573", "");
				text = text.Replace("/Hl leoMoved", "");
				string text2 = locationFromName?.checkEventPrecondition(text);
				if (locationFromName != null && string.IsNullOrEmpty(text2) && text2 != "-1")
				{
					return true;
				}
			}
		}
		return false;
	}

	public KeyValuePair<string, string>? GetUnseenLeoEvent()
	{
		if (!Game1.player.eventsSeen.Contains("6497423"))
		{
			return new KeyValuePair<string, string>("IslandWest", "6497423");
		}
		if (!Game1.player.eventsSeen.Contains("6497421"))
		{
			return new KeyValuePair<string, string>("IslandNorth", "6497421");
		}
		if (!Game1.player.eventsSeen.Contains("6497428"))
		{
			return new KeyValuePair<string, string>("IslandSouth", "6497428");
		}
		return null;
	}

	public void OnLeoMemoryResponse(Farmer who, string whichAnswer)
	{
		if (whichAnswer.EqualsIgnoreCase("yes"))
		{
			KeyValuePair<string, string>? unseenLeoEvent = GetUnseenLeoEvent();
			if (!unseenLeoEvent.HasValue)
			{
				return;
			}
			string key = unseenLeoEvent.Value.Key;
			string value = unseenLeoEvent.Value.Value;
			string eventAssetName = "Data\\Events\\" + key;
			Dictionary<string, string> location_events;
			try
			{
				location_events = Game1.content.Load<Dictionary<string, string>>(eventAssetName);
			}
			catch
			{
				return;
			}
			if (location_events == null)
			{
				return;
			}
			Point oldTile = Game1.player.TilePoint;
			string oldLocation = Game1.player.currentLocation.NameOrUniqueName;
			int oldDirection = Game1.player.FacingDirection;
			{
				foreach (string key2 in location_events.Keys)
				{
					if (Event.SplitPreconditions(key2)[0] == value)
					{
						LocationRequest location_request = Game1.getLocationRequest(key);
						Game1.warpingForForcedRemoteEvent = true;
						location_request.OnWarp += delegate
						{
							Event obj2 = new Event(location_events[key2], eventAssetName, "event_id");
							obj2.isMemory = true;
							obj2.setExitLocation(oldLocation, oldTile.X, oldTile.Y);
							Game1.player.orientationBeforeEvent = oldDirection;
							location_request.Location.currentEvent = obj2;
							location_request.Location.startEvent(obj2);
							Game1.warpingForForcedRemoteEvent = false;
						};
						int x = 8;
						int y = 8;
						Utility.getDefaultWarpLocation(location_request.Name, ref x, ref y);
						Game1.warpFarmer(location_request, x, y, Game1.player.FacingDirection);
					}
				}
				return;
			}
		}
		Game1.game1.wasAskedLeoMemory = true;
	}

	public bool isDivorcedFrom(Farmer who)
	{
		return IsDivorcedFrom(who, base.Name);
	}

	public static bool IsDivorcedFrom(Farmer player, string npcName)
	{
		if (player != null && player.friendshipData.TryGetValue(npcName, out var value))
		{
			return value.IsDivorced();
		}
		return false;
	}

	public override void MovePosition(GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation currentLocation)
	{
		if (movementPause <= 0)
		{
			faceTowardFarmerTimer = 0;
			base.MovePosition(time, viewport, currentLocation);
		}
	}

	public GameLocation getHome()
	{
		if (isMarried() && getSpouse() != null)
		{
			return Utility.getHomeOfFarmer(getSpouse());
		}
		return Game1.RequireLocation(defaultMap.Value);
	}

	public override bool canPassThroughActionTiles()
	{
		return true;
	}

	public virtual void behaviorOnFarmerPushing()
	{
	}

	public virtual void behaviorOnFarmerLocationEntry(GameLocation location, Farmer who)
	{
		if (Sprite != null && Sprite.CurrentAnimation == null && Sprite.SourceRect.Height > 32 && !base.SimpleNonVillagerNPC)
		{
			Sprite.SpriteWidth = 16;
			Sprite.SpriteHeight = 16;
			Sprite.currentFrame = 0;
		}
	}

	public virtual void behaviorOnLocalFarmerLocationEntry(GameLocation location)
	{
		shouldPlayRobinHammerAnimation.CancelInterpolation();
		shouldPlaySpousePatioAnimation.CancelInterpolation();
		shouldWearIslandAttire.CancelInterpolation();
		isSleeping.CancelInterpolation();
		doingEndOfRouteAnimation.CancelInterpolation();
		if (doingEndOfRouteAnimation.Value)
		{
			_skipRouteEndIntro = true;
		}
		else
		{
			_skipRouteEndIntro = false;
		}
		endOfRouteBehaviorName.CancelInterpolation();
		if (isSleeping.Value)
		{
			position.Field.CancelInterpolation();
		}
	}

	public override void updateMovement(GameLocation location, GameTime time)
	{
		lastPosition = base.Position;
		if (DirectionsToNewLocation != null && !Game1.newDay)
		{
			Point standingPixel = base.StandingPixel;
			if (standingPixel.X < -64 || standingPixel.X > location.map.DisplayWidth + 64 || standingPixel.Y < -64 || standingPixel.Y > location.map.DisplayHeight + 64)
			{
				IsWalkingInSquare = false;
				Game1.warpCharacter(this, DefaultMap, DefaultPosition);
				location.characters.Remove(this);
			}
			else if (IsWalkingInSquare)
			{
				returnToEndPoint();
				MovePosition(time, Game1.viewport, location);
			}
		}
		else if (IsWalkingInSquare)
		{
			randomSquareMovement(time);
			MovePosition(time, Game1.viewport, location);
		}
	}

	public void facePlayer(Farmer who)
	{
		if (facingDirectionBeforeSpeakingToPlayer.Value == -1)
		{
			facingDirectionBeforeSpeakingToPlayer.Value = getFacingDirection();
		}
		faceDirection((who.FacingDirection + 2) % 4);
	}

	public void doneFacingPlayer(Farmer who)
	{
	}

	public override void update(GameTime time, GameLocation location)
	{
		if (AllowDynamicAppearance && base.currentLocation != null && base.currentLocation.NameOrUniqueName != LastLocationNameForAppearance)
		{
			ChooseAppearance();
		}
		if (Game1.IsMasterGame && currentScheduleDelay > 0f)
		{
			currentScheduleDelay -= (float)time.ElapsedGameTime.TotalSeconds;
			if (currentScheduleDelay <= 0f)
			{
				currentScheduleDelay = -1f;
				checkSchedule(Game1.timeOfDay);
				currentScheduleDelay = 0f;
			}
		}
		removeHenchmanEvent.Poll();
		if (Game1.IsMasterGame && shouldWearIslandAttire.Value && (base.currentLocation == null || base.currentLocation.InValleyContext()))
		{
			shouldWearIslandAttire.Value = false;
		}
		if (_startedEndOfRouteBehavior == null && _finishingEndOfRouteBehavior == null && loadedEndOfRouteBehavior != endOfRouteBehaviorName.Value)
		{
			loadEndOfRouteBehavior(endOfRouteBehaviorName.Value);
		}
		if (doingEndOfRouteAnimation.Value != currentlyDoingEndOfRouteAnimation)
		{
			if (!currentlyDoingEndOfRouteAnimation)
			{
				if (string.Equals(loadedEndOfRouteBehavior, endOfRouteBehaviorName.Value, StringComparison.Ordinal))
				{
					reallyDoAnimationAtEndOfScheduleRoute();
				}
			}
			else
			{
				finishEndOfRouteAnimation();
			}
			currentlyDoingEndOfRouteAnimation = doingEndOfRouteAnimation.Value;
		}
		if (shouldWearIslandAttire.Value != isWearingIslandAttire)
		{
			if (!isWearingIslandAttire)
			{
				wearIslandAttire();
			}
			else
			{
				wearNormalClothes();
			}
		}
		if (isSleeping.Value != isPlayingSleepingAnimation)
		{
			if (!isPlayingSleepingAnimation)
			{
				playSleepingAnimation();
			}
			else
			{
				Sprite.StopAnimation();
				isPlayingSleepingAnimation = false;
			}
		}
		if (shouldPlayRobinHammerAnimation.Value != isPlayingRobinHammerAnimation)
		{
			if (!isPlayingRobinHammerAnimation)
			{
				doPlayRobinHammerAnimation();
				isPlayingRobinHammerAnimation = true;
			}
			else
			{
				Sprite.StopAnimation();
				isPlayingRobinHammerAnimation = false;
			}
		}
		if (shouldPlaySpousePatioAnimation.Value != isPlayingSpousePatioAnimation)
		{
			if (!isPlayingSpousePatioAnimation)
			{
				doPlaySpousePatioAnimation();
				isPlayingSpousePatioAnimation = true;
			}
			else
			{
				Sprite.StopAnimation();
				isPlayingSpousePatioAnimation = false;
			}
		}
		if (returningToEndPoint)
		{
			returnToEndPoint();
			MovePosition(time, Game1.viewport, location);
		}
		else if (temporaryController != null)
		{
			if (temporaryController.update(time))
			{
				bool nPCSchedule = temporaryController.NPCSchedule;
				temporaryController = null;
				if (nPCSchedule)
				{
					currentScheduleDelay = -1f;
					checkSchedule(Game1.timeOfDay);
					currentScheduleDelay = 0f;
				}
			}
			updateEmote(time);
		}
		else
		{
			base.update(time, location);
		}
		if (textAboveHeadTimer > 0)
		{
			if (textAboveHeadPreTimer > 0)
			{
				textAboveHeadPreTimer -= time.ElapsedGameTime.Milliseconds;
			}
			else
			{
				textAboveHeadTimer -= time.ElapsedGameTime.Milliseconds;
				if (textAboveHeadTimer > 500)
				{
					textAboveHeadAlpha = Math.Min(1f, textAboveHeadAlpha + 0.1f);
				}
				else
				{
					textAboveHeadAlpha = Math.Max(0f, textAboveHeadAlpha - 0.04f);
				}
			}
		}
		if (isWalkingInSquare && !returningToEndPoint)
		{
			randomSquareMovement(time);
		}
		if (Sprite?.CurrentAnimation != null && !Game1.eventUp && Game1.IsMasterGame && Sprite.animateOnce(time))
		{
			Sprite.CurrentAnimation = null;
		}
		if (movementPause > 0 && (!Game1.dialogueUp || controller != null))
		{
			freezeMotion = true;
			movementPause -= time.ElapsedGameTime.Milliseconds;
			if (movementPause <= 0)
			{
				freezeMotion = false;
			}
		}
		if (shakeTimer > 0)
		{
			shakeTimer -= time.ElapsedGameTime.Milliseconds;
		}
		if (lastPosition.Equals(base.Position))
		{
			timerSinceLastMovement += time.ElapsedGameTime.Milliseconds;
		}
		else
		{
			timerSinceLastMovement = 0f;
		}
		if (swimming.Value)
		{
			yOffset = (float)(Math.Cos(time.TotalGameTime.TotalMilliseconds / 2000.0) * 4.0);
			float num = swimTimer;
			swimTimer -= time.ElapsedGameTime.Milliseconds;
			if (timerSinceLastMovement == 0f)
			{
				if (num > 400f && swimTimer <= 400f && location.Equals(Game1.currentLocation))
				{
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), 150f - (Math.Abs(xVelocity) + Math.Abs(yVelocity)) * 3f, 8, 0, new Vector2(base.Position.X, base.StandingPixel.Y - 32), flicker: false, Game1.random.NextBool(), 0.01f, 0.01f, Color.White, 1f, 0.003f, 0f, 0f));
					location.playSound("slosh", null, null, SoundContext.NPC);
				}
				if (swimTimer < 0f)
				{
					swimTimer = 800f;
					if (location.Equals(Game1.currentLocation))
					{
						location.playSound("slosh", null, null, SoundContext.NPC);
						Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), 150f - (Math.Abs(xVelocity) + Math.Abs(yVelocity)) * 3f, 8, 0, new Vector2(base.Position.X, base.StandingPixel.Y - 32), flicker: false, Game1.random.NextBool(), 0.01f, 0.01f, Color.White, 1f, 0.003f, 0f, 0f));
					}
				}
			}
			else if (swimTimer < 0f)
			{
				swimTimer = 100f;
			}
		}
		if (Game1.IsMasterGame)
		{
			isMovingOnPathFindPath.Value = controller != null && temporaryController != null;
		}
	}

	public virtual void wearIslandAttire()
	{
		isWearingIslandAttire = true;
		ChooseAppearance();
	}

	public virtual void wearNormalClothes()
	{
		isWearingIslandAttire = false;
		ChooseAppearance();
	}

	/// <summary>Runs NPC update logic on ten in-game minute intervals (e.g. greeting players or other NPCs)</summary>
	/// <param name="timeOfDay">The new in-game time.</param>
	/// <param name="location">The location where the update is occurring.</param>
	public virtual void performTenMinuteUpdate(int timeOfDay, GameLocation location)
	{
		if (Game1.eventUp || location == null)
		{
			return;
		}
		if (Game1.random.NextDouble() < 0.1 && Dialogue != null && Dialogue.TryGetValue(location.Name + "_Ambient", out var value))
		{
			CharacterData data = GetData();
			if (data == null || data.CanGreetNearbyCharacters)
			{
				string[] options = value.Split('/');
				int preTimer = Game1.random.Next(4) * 1000;
				showTextAboveHead(Game1.random.Choose(options), null, 2, 3000, preTimer);
				return;
			}
		}
		if (!isMoving() || !location.IsOutdoors || timeOfDay >= 1800 || !(Game1.random.NextDouble() < 0.3 + ((SocialAnxiety == 0) ? 0.25 : ((SocialAnxiety != 1) ? 0.0 : ((Manners == 2) ? (-1.0) : (-0.2))))) || (Age == 1 && (Manners != 1 || SocialAnxiety != 0)) || isMarried())
		{
			return;
		}
		CharacterData data2 = GetData();
		if (data2 == null || !data2.CanGreetNearbyCharacters)
		{
			return;
		}
		Character character = Utility.isThereAFarmerOrCharacterWithinDistance(base.Tile, 4, location);
		if (character == null || character.Name == base.Name || character is Horse)
		{
			return;
		}
		NPC obj = character as NPC;
		if (obj != null && obj.GetData()?.CanGreetNearbyCharacters == false)
		{
			return;
		}
		NPC obj2 = character as NPC;
		if (obj2 == null || !obj2.SimpleNonVillagerNPC)
		{
			Dictionary<string, string> friendsAndFamily = data2.FriendsAndFamily;
			if ((friendsAndFamily == null || !friendsAndFamily.ContainsKey(character.Name)) && isFacingToward(character.Tile))
			{
				sayHiTo(character);
			}
		}
	}

	public void sayHiTo(Character c)
	{
		if (getHi(c.displayName) != null)
		{
			showTextAboveHead(getHi(c.displayName));
			if (c is NPC nPC && Game1.random.NextDouble() < 0.66 && nPC.getHi(displayName) != null)
			{
				nPC.showTextAboveHead(nPC.getHi(displayName), null, 2, 3000, 1000 + Game1.random.Next(500));
			}
		}
	}

	public string getHi(string nameToGreet)
	{
		if (Age == 2)
		{
			if (SocialAnxiety != 1)
			{
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4059");
			}
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4058");
		}
		switch (SocialAnxiety)
		{
		case 1:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs." + Game1.random.Choose("4060", "4061"));
		case 0:
			if (!(Game1.random.NextDouble() < 0.33))
			{
				if (!Game1.random.NextBool())
				{
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4068", nameToGreet);
				}
				return ((Game1.timeOfDay < 1200) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4063") : ((Game1.timeOfDay < 1700) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4064") : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4065"))) + ", " + Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4066", nameToGreet);
			}
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4062");
		default:
			if (!(Game1.random.NextDouble() < 0.33))
			{
				if (!Game1.random.NextBool())
				{
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4072");
				}
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4071", nameToGreet);
			}
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4060");
		}
	}

	public bool isFacingToward(Vector2 tileLocation)
	{
		return FacingDirection switch
		{
			0 => (float)base.TilePoint.Y > tileLocation.Y, 
			1 => (float)base.TilePoint.X < tileLocation.X, 
			2 => (float)base.TilePoint.Y < tileLocation.Y, 
			3 => (float)base.TilePoint.X > tileLocation.X, 
			_ => false, 
		};
	}

	public virtual void arriveAt(GameLocation l)
	{
		if (!Game1.eventUp && Game1.random.NextBool() && Dialogue != null && Dialogue.TryGetValue(l.name.Value + "_Entry", out var value))
		{
			showTextAboveHead(Game1.random.Choose(value.Split('/')));
		}
	}

	public override void Halt()
	{
		base.Halt();
		shouldPlaySpousePatioAnimation.Value = false;
		isPlayingSleepingAnimation = false;
		isCharging = false;
		base.speed = 2;
		addedSpeed = 0f;
		if (isSleeping.Value)
		{
			playSleepingAnimation();
			Sprite.UpdateSourceRect();
		}
	}

	public void addExtraDialogue(Dialogue dialogue)
	{
		if (updatedDialogueYet)
		{
			if (dialogue != null)
			{
				CurrentDialogue.Push(dialogue);
			}
		}
		else
		{
			extraDialogueMessageToAddThisMorning = dialogue;
		}
	}

	public void PerformDivorce()
	{
		reloadDefaultLocation();
		Game1.warpCharacter(this, defaultMap.Value, DefaultPosition / 64f);
	}

	public Dialogue tryToGetMarriageSpecificDialogue(string dialogueKey)
	{
		Dictionary<string, string> dictionary = null;
		string text = null;
		bool flag = false;
		if (isRoommate())
		{
			try
			{
				text = "Characters\\Dialogue\\MarriageDialogue" + GetDialogueSheetName() + "Roommate";
				Dictionary<string, string> dictionary2 = Game1.content.Load<Dictionary<string, string>>(text);
				if (dictionary2 != null)
				{
					flag = true;
					dictionary = dictionary2;
					if (dictionary != null && dictionary.TryGetValue(dialogueKey, out var value))
					{
						return new Dialogue(this, text + ":" + dialogueKey, value);
					}
				}
			}
			catch (Exception)
			{
				text = null;
			}
		}
		if (!flag)
		{
			try
			{
				text = "Characters\\Dialogue\\MarriageDialogue" + GetDialogueSheetName();
				dictionary = Game1.content.Load<Dictionary<string, string>>(text);
			}
			catch (Exception)
			{
				text = null;
			}
		}
		if (dictionary != null && dictionary.TryGetValue(dialogueKey, out var value2))
		{
			return new Dialogue(this, text + ":" + dialogueKey, value2);
		}
		text = "Characters\\Dialogue\\MarriageDialogue";
		dictionary = Game1.content.Load<Dictionary<string, string>>(text);
		if (isRoommate())
		{
			string key = dialogueKey + "Roommate";
			if (dictionary != null && dictionary.TryGetValue(key, out var value3))
			{
				return new Dialogue(this, text + ":" + dialogueKey, value3);
			}
		}
		if (dictionary != null && dictionary.TryGetValue(dialogueKey, out var value4))
		{
			return new Dialogue(this, text + ":" + dialogueKey, value4);
		}
		return null;
	}

	public void resetCurrentDialogue()
	{
		CurrentDialogue = null;
		shouldSayMarriageDialogue.Value = false;
		currentMarriageDialogue.Clear();
	}

	private Stack<Dialogue> loadCurrentDialogue()
	{
		updatedDialogueYet = true;
		Stack<Dialogue> stack = new Stack<Dialogue>();
		try
		{
			Friendship value;
			int num = (Game1.player.friendshipData.TryGetValue(base.Name, out value) ? (value.Points / 250) : 0);
			Random random = Utility.CreateDaySaveRandom(Game1.stats.DaysPlayed * 77, 2f + defaultPosition.X * 77f, defaultPosition.Y * 777f);
			if (base.currentLocation != null && base.currentLocation.IsGreenRainingHere())
			{
				Dialogue dialogue = null;
				if (Game1.year >= 2)
				{
					dialogue = TryGetDialogue("GreenRain_2");
				}
				if (dialogue == null)
				{
					dialogue = TryGetDialogue("GreenRain");
				}
				if (dialogue != null)
				{
					stack.Clear();
					stack.Push(dialogue);
					return stack;
				}
			}
			if (random.NextDouble() < 0.025 && num >= 1)
			{
				CharacterData data = GetData();
				if (data?.FriendsAndFamily != null && Utility.TryGetRandom(data.FriendsAndFamily, out var key, out var value2))
				{
					NPC characterFromName = Game1.getCharacterFromName(key);
					string text = characterFromName?.displayName ?? GetDisplayName(key);
					CharacterData data2;
					bool flag = ((characterFromName != null) ? (characterFromName.gender.Value == Gender.Male) : (TryGetData(key, out data2) && data2.Gender == Gender.Male));
					value2 = TokenParser.ParseText(value2);
					if (string.IsNullOrWhiteSpace(value2))
					{
						value2 = null;
					}
					IDictionary<string, string> nPCGiftTastes = Game1.NPCGiftTastes;
					if (nPCGiftTastes.TryGetValue(key, out var value3))
					{
						string[] array = value3.Split('/');
						string text2 = null;
						string text3 = null;
						string text4 = ((value2 == null || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ja) ? text : (flag ? Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4079", value2) : Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4080", value2)));
						string text5 = Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4083", text4);
						if (random.NextBool())
						{
							int num2 = 0;
							string[] options = ArgUtility.SplitBySpace(ArgUtility.Get(array, 1));
							while ((text2 == null || text2.StartsWith("-")) && num2 < 30)
							{
								text2 = random.Choose(options);
								num2++;
							}
							if (base.Name == "Penny" && key == "Pam")
							{
								while (true)
								{
									switch (text2)
									{
									case "303":
									case "346":
									case "348":
									case "459":
										goto IL_027b;
									}
									break;
									IL_027b:
									text2 = random.Choose(options);
								}
							}
							if (text2 != null)
							{
								ParsedItemData data3 = ItemRegistry.GetData(text2);
								if (data3 != null)
								{
									text3 = data3.DisplayName;
									text5 += Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4084", text3);
									if (Age == 2)
									{
										text5 = Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4086", text4, text3) + (flag ? Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4088") : Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4089"));
									}
									else
									{
										switch (random.Next(5))
										{
										case 0:
											text5 = Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4091", text4, text3);
											break;
										case 1:
											text5 = (flag ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4094", text4, text3) : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4097", text4, text3));
											break;
										case 2:
											text5 = (flag ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4100", text4, text3) : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4103", text4, text3));
											break;
										case 3:
											text5 = Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4106", text4, text3);
											break;
										}
										if (random.NextDouble() < 0.65)
										{
											switch (random.Next(5))
											{
											case 0:
												text5 += (flag ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4109") : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4111"));
												break;
											case 1:
												text5 += ((!flag) ? (random.NextBool() ? Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4115") : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4116")) : (random.NextBool() ? Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4113") : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4114")));
												break;
											case 2:
												text5 += (flag ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4118") : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4120"));
												break;
											case 3:
												text5 += Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4125");
												break;
											case 4:
												text5 += (flag ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4126") : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4128"));
												break;
											}
											if (key.Equals("Abigail") && random.NextBool())
											{
												text5 = Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4128", text, text3);
											}
										}
									}
								}
							}
						}
						else
						{
							string[] array2 = ArgUtility.SplitBySpace(ArgUtility.Get(array, 7));
							if (array2.Count() > 0)
							{
								int num3 = 0;
								while ((text2 == null || text2.StartsWith("-")) && num3 < 30)
								{
									text2 = random.Choose(array2);
									num3++;
								}
							}
							if (text2 == null)
							{
								int num4 = 0;
								while ((text2 == null || text2.StartsWith("-")) && num4 < 30)
								{
									text2 = random.Choose(ArgUtility.SplitBySpace(nPCGiftTastes["Universal_Hate"]));
									num4++;
								}
							}
							if (text2 != null)
							{
								ParsedItemData data4 = ItemRegistry.GetData(text2);
								if (data4 != null)
								{
									text3 = data4.DisplayName;
									text5 += (flag ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4135", text3, Lexicon.getRandomNegativeFoodAdjective()) : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4138", text3, Lexicon.getRandomNegativeFoodAdjective()));
									if (Age == 2)
									{
										text5 = (flag ? Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4141", text, text3) : Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4144", text, text3));
									}
									else
									{
										switch (random.Next(4))
										{
										case 0:
											text5 = (random.NextBool() ? Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4146") : "") + Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4147", text4, text3);
											break;
										case 1:
											text5 = ((!flag) ? (random.NextBool() ? Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4153", text4, text3) : Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4154", text4, text3)) : (random.NextBool() ? Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4149", text4, text3) : Game1.LoadStringByGender(Gender, "Strings\\StringsFromCSFiles:NPC.cs.4152", text4, text3)));
											break;
										case 2:
											text5 = (flag ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4161", text4, text3) : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4164", text4, text3));
											break;
										}
										if (random.NextDouble() < 0.65)
										{
											switch (random.Next(5))
											{
											case 0:
												text5 += Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4170");
												break;
											case 1:
												text5 += Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4171");
												break;
											case 2:
												text5 += (flag ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4172") : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4174"));
												break;
											case 3:
												text5 += (flag ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4176") : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4178"));
												break;
											case 4:
												text5 += Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4180");
												break;
											}
											if (base.Name.Equals("Lewis") && random.NextBool())
											{
												text5 = Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4182", text, text3);
											}
										}
									}
								}
							}
						}
						if (text3 != null)
						{
							if (Game1.getCharacterFromName(key) != null)
							{
								text5 = text5 + "%revealtaste:" + key + ":" + text2;
							}
							stack.Clear();
							if (text5.Length > 0)
							{
								try
								{
									text5 = text5.Substring(0, 1).ToUpper() + text5.Substring(1, text5.Length - 1);
								}
								catch (Exception)
								{
								}
							}
							stack.Push(new Dialogue(this, null, text5));
							return stack;
						}
					}
				}
			}
			if (Dialogue != null && Dialogue.Count != 0)
			{
				stack.Clear();
				if (Game1.player.spouse != null && Game1.player.spouse == base.Name)
				{
					if (Game1.player.isEngaged())
					{
						Dictionary<string, string> dictionary = Game1.content.Load<Dictionary<string, string>>("Data\\EngagementDialogue");
						if (Game1.player.hasCurrentOrPendingRoommate() && dictionary.ContainsKey(base.Name + "Roommate0"))
						{
							stack.Push(new Dialogue(this, "Data\\EngagementDialogue:" + base.Name + "Roommate" + random.Next(2)));
						}
						else if (dictionary.ContainsKey(base.Name + "0"))
						{
							stack.Push(new Dialogue(this, "Data\\EngagementDialogue:" + base.Name + random.Next(2)));
						}
					}
					else if (!Game1.newDay && marriageDefaultDialogue.Value != null && !shouldSayMarriageDialogue.Value)
					{
						stack.Push(marriageDefaultDialogue.Value.GetDialogue(this));
						marriageDefaultDialogue.Value = null;
					}
				}
				else
				{
					if (Game1.player.friendshipData.TryGetValue(base.Name, out var value4) && value4.IsDivorced())
					{
						Dialogue dialogue2 = StardewValley.Dialogue.TryGetDialogue(this, "Characters\\Dialogue\\" + GetDialogueSheetName() + ":divorced");
						if (dialogue2 != null)
						{
							stack.Push(dialogue2);
							return stack;
						}
					}
					if (Game1.isRaining && random.NextBool() && (base.currentLocation == null || base.currentLocation.InValleyContext()) && (!base.Name.Equals("Krobus") || !(Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth) == "Fri")) && (!base.Name.Equals("Penny") || !Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade")) && (!base.Name.Equals("Emily") || !Game1.IsFall || Game1.dayOfMonth != 15))
					{
						Dialogue dialogue3 = StardewValley.Dialogue.TryGetDialogue(this, "Characters\\Dialogue\\rainy:" + GetDialogueSheetName());
						if (dialogue3 != null)
						{
							stack.Push(dialogue3);
							return stack;
						}
					}
					Dialogue dialogue4 = tryToRetrieveDialogue(Game1.currentSeason + "_", num);
					if (dialogue4 == null)
					{
						dialogue4 = tryToRetrieveDialogue("", num);
					}
					if (dialogue4 != null)
					{
						stack.Push(dialogue4);
					}
				}
			}
			else if (base.Name.Equals("Bouncer"))
			{
				stack.Push(new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4192"));
			}
			if (extraDialogueMessageToAddThisMorning != null)
			{
				stack.Push(extraDialogueMessageToAddThisMorning);
			}
		}
		catch (Exception exception)
		{
			Game1.log.Error("NPC '" + base.Name + "' failed loading their current dialogue.", exception);
		}
		return stack;
	}

	public bool checkForNewCurrentDialogue(int heartLevel, bool noPreface = false)
	{
		if (Game1.IsGreenRainingHere())
		{
			return false;
		}
		foreach (string key in Game1.player.activeDialogueEvents.Keys)
		{
			if (key == "")
			{
				continue;
			}
			Dialogue dialogue = TryGetDialogue(key);
			if (dialogue == null)
			{
				continue;
			}
			string item = base.Name + "_" + key;
			if (dialogue != null && !Game1.player.mailReceived.Contains(item))
			{
				CurrentDialogue.Clear();
				CurrentDialogue.Push(dialogue);
				if (!key.Contains("dumped"))
				{
					Game1.player.mailReceived.Add(item);
				}
				return true;
			}
		}
		string text = ((Game1.season != Season.Spring && !noPreface) ? Game1.currentSeason : "");
		Dialogue dialogue2 = TryGetDialogue(text + Game1.currentLocation.name.Value + "_" + base.TilePoint.X + "_" + base.TilePoint.Y) ?? TryGetDialogue(text + Game1.currentLocation.name.Value + "_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth));
		int num = 10;
		while (dialogue2 == null && num >= 2)
		{
			if (heartLevel >= num)
			{
				dialogue2 = TryGetDialogue(text + Game1.currentLocation.name.Value + num);
			}
			num -= 2;
		}
		dialogue2 = dialogue2 ?? TryGetDialogue(text + Game1.currentLocation.Name);
		if (dialogue2 != null)
		{
			dialogue2.removeOnNextMove = true;
			CurrentDialogue.Push(dialogue2);
			return true;
		}
		return false;
	}

	/// <summary>Try to get a specific dialogue from the loaded <see cref="P:StardewValley.NPC.Dialogue" />.</summary>
	/// <param name="key">The dialogue key.</param>
	/// <returns>Returns the matched dialogue if found, else <c>null</c>.</returns>
	public Dialogue TryGetDialogue(string key)
	{
		Dictionary<string, string> dictionary = Dialogue;
		if (dictionary != null && dictionary.TryGetValue(key, out var value))
		{
			return new Dialogue(this, LoadedDialogueKey + ":" + key, value);
		}
		return null;
	}

	/// <summary>Try to get a specific dialogue from the loaded <see cref="P:StardewValley.NPC.Dialogue" /> using a matching gift taste tag like 'Positive'.</summary>
	/// <param name="giftTaste">The gift taste, matching a value lke <see cref="F:StardewValley.NPC.gift_taste_love" />.</param>
	/// <param name="key">Get the dialogue key to try, given a gift taste tag like 'Positive'.</param>
	/// <returns>Returns the matched dialogue if found, else <c>null</c>.</returns>
	public Dialogue TryGetDialogueByGiftTaste(int giftTaste, Func<string, string> getKey)
	{
		switch (giftTaste)
		{
		case 0:
		case 7:
			return TryGetDialogue(getKey("Loved")) ?? TryGetDialogue(getKey("Positive"));
		case 2:
			return TryGetDialogue(getKey("Liked")) ?? TryGetDialogue(getKey("Positive"));
		case 4:
			return TryGetDialogue(getKey("Disliked")) ?? TryGetDialogue(getKey("Negative"));
		case 6:
			return TryGetDialogue(getKey("Hated")) ?? TryGetDialogue(getKey("Negative"));
		default:
			return TryGetDialogue(getKey("Neutral")) ?? TryGetDialogue(getKey("Positive"));
		}
	}

	/// <summary>Try to get a specific dialogue from the loaded <see cref="P:StardewValley.NPC.Dialogue" />.</summary>
	/// <param name="key">The dialogue key.</param>
	/// <param name="substitutions">The values with which to replace placeholders like <c>{0}</c> in the loaded text.</param>
	/// <returns>Returns the matched dialogue if found, else <c>null</c>.</returns>
	public Dialogue TryGetDialogue(string key, params object[] substitutions)
	{
		Dictionary<string, string> dictionary = Dialogue;
		if (dictionary != null && dictionary.TryGetValue(key, out var value))
		{
			return new Dialogue(this, LoadedDialogueKey + ":" + key, string.Format(value, substitutions));
		}
		return null;
	}

	/// <summary>Try to get a dialogue from the loaded <see cref="P:StardewValley.NPC.Dialogue" />, applying variant rules for roommates, marriage, inlaws, dates, etc.</summary>
	/// <param name="preface">A prefix added to the translation keys to look up.</param>
	/// <param name="heartLevel">The NPC's heart level with the player.</param>
	/// <param name="appendToEnd">A suffix added to the translation keys to look up.</param>
	/// <returns>Returns the best matched dialogue if found, else <c>null</c>.</returns>
	public Dialogue tryToRetrieveDialogue(string preface, int heartLevel, string appendToEnd = "")
	{
		int num = Game1.year;
		if (Game1.year > 2)
		{
			num = 2;
		}
		if (!string.IsNullOrEmpty(Game1.player.spouse) && appendToEnd.Equals(""))
		{
			if (Game1.player.hasCurrentOrPendingRoommate())
			{
				Dialogue dialogue = tryToRetrieveDialogue(preface, heartLevel, "_roommate_" + Game1.player.spouse);
				if (dialogue != null)
				{
					return dialogue;
				}
			}
			else
			{
				Dialogue dialogue2 = tryToRetrieveDialogue(preface, heartLevel, "_inlaw_" + Game1.player.spouse);
				if (dialogue2 != null)
				{
					return dialogue2;
				}
			}
		}
		string text = Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
		if (num == 1)
		{
			Dialogue dialogue3 = TryGetDialogue(preface + Game1.dayOfMonth + appendToEnd);
			if (dialogue3 != null)
			{
				return dialogue3;
			}
		}
		Dialogue dialogue4 = TryGetDialogue(preface + Game1.dayOfMonth + "_" + num + appendToEnd);
		if (dialogue4 != null)
		{
			return dialogue4;
		}
		Dialogue dialogue5 = TryGetDialogue(preface + Game1.dayOfMonth + "_*" + appendToEnd);
		if (dialogue5 != null)
		{
			return dialogue5;
		}
		for (int num2 = 10; num2 >= 2; num2 -= 2)
		{
			if (heartLevel >= num2)
			{
				Dialogue dialogue6 = TryGetDialogue(preface + text + num2 + "_" + num + appendToEnd) ?? TryGetDialogue(preface + text + num2 + appendToEnd);
				if (dialogue6 != null)
				{
					if (num2 == 4 && preface == "fall_" && text == "Mon" && base.Name.Equals("Penny") && Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
					{
						return TryGetDialogue(preface + text + "_" + num + appendToEnd) ?? TryGetDialogue("fall_Mon");
					}
					return dialogue6;
				}
			}
		}
		Dialogue dialogue7 = TryGetDialogue(preface + text + appendToEnd);
		if (dialogue7 != null)
		{
			Dialogue dialogue8 = TryGetDialogue(preface + text + "_" + num + appendToEnd);
			if (dialogue8 != null)
			{
				dialogue7 = dialogue8;
			}
		}
		if (dialogue7 != null && base.Name.Equals("Caroline") && Game1.isLocationAccessible("CommunityCenter") && preface == "summer_" && text == "Mon")
		{
			dialogue7 = TryGetDialogue("summer_Wed");
		}
		if (dialogue7 != null)
		{
			return dialogue7;
		}
		return null;
	}

	public virtual void checkSchedule(int timeOfDay)
	{
		if (currentScheduleDelay == 0f && scheduleDelaySeconds > 0f)
		{
			currentScheduleDelay = scheduleDelaySeconds;
		}
		else
		{
			if (returningToEndPoint)
			{
				return;
			}
			updatedDialogueYet = false;
			extraDialogueMessageToAddThisMorning = null;
			if (ignoreScheduleToday || Schedule == null)
			{
				return;
			}
			SchedulePathDescription value = null;
			if (lastAttemptedSchedule < timeOfDay)
			{
				lastAttemptedSchedule = timeOfDay;
				Schedule.TryGetValue(timeOfDay, out value);
				if (value != null)
				{
					queuedSchedulePaths.Add(value);
				}
				value = null;
			}
			PathFindController pathFindController = controller;
			if (pathFindController != null && pathFindController.pathToEndPoint?.Count > 0)
			{
				return;
			}
			if (queuedSchedulePaths.Count > 0 && timeOfDay >= queuedSchedulePaths[0].time)
			{
				value = queuedSchedulePaths[0];
			}
			if (value == null)
			{
				return;
			}
			prepareToDisembarkOnNewSchedulePath();
			if (!returningToEndPoint && temporaryController == null)
			{
				directionsToNewLocation = value;
				if (queuedSchedulePaths.Count > 0)
				{
					queuedSchedulePaths.RemoveAt(0);
				}
				controller = new PathFindController(directionsToNewLocation.route, this, Utility.getGameLocationOfCharacter(this))
				{
					finalFacingDirection = directionsToNewLocation.facingDirection,
					endBehaviorFunction = getRouteEndBehaviorFunction(directionsToNewLocation.endOfRouteBehavior, directionsToNewLocation.endOfRouteMessage)
				};
				if (controller.pathToEndPoint == null || controller.pathToEndPoint.Count == 0)
				{
					controller.endBehaviorFunction?.Invoke(this, base.currentLocation);
					controller = null;
				}
				if (directionsToNewLocation?.route != null)
				{
					previousEndPoint = directionsToNewLocation.route.LastOrDefault();
				}
			}
		}
	}

	private void finishEndOfRouteAnimation()
	{
		_finishingEndOfRouteBehavior = _startedEndOfRouteBehavior;
		_startedEndOfRouteBehavior = null;
		string finishingEndOfRouteBehavior = _finishingEndOfRouteBehavior;
		if (!(finishingEndOfRouteBehavior == "change_beach"))
		{
			if (finishingEndOfRouteBehavior == "change_normal")
			{
				shouldWearIslandAttire.Value = false;
				currentlyDoingEndOfRouteAnimation = false;
			}
		}
		else
		{
			shouldWearIslandAttire.Value = true;
			currentlyDoingEndOfRouteAnimation = false;
		}
		while (CurrentDialogue.Count > 0 && CurrentDialogue.Peek().removeOnNextMove)
		{
			CurrentDialogue.Pop();
		}
		shouldSayMarriageDialogue.Value = false;
		currentMarriageDialogue.Clear();
		nextEndOfRouteMessage = null;
		endOfRouteMessage.Value = null;
		if (currentlyDoingEndOfRouteAnimation && routeEndOutro != null)
		{
			bool flag = false;
			for (int i = 0; i < routeEndOutro.Length; i++)
			{
				if (!flag)
				{
					Sprite.ClearAnimation();
					flag = true;
				}
				if (i == routeEndOutro.Length - 1)
				{
					Sprite.AddFrame(new FarmerSprite.AnimationFrame(routeEndOutro[i], 100, 0, secondaryArm: false, flip: false, routeEndAnimationFinished, behaviorAtEndOfFrame: true));
				}
				else
				{
					Sprite.AddFrame(new FarmerSprite.AnimationFrame(routeEndOutro[i], 100, 0, secondaryArm: false, flip: false));
				}
			}
			if (!flag)
			{
				routeEndAnimationFinished(null);
			}
			if (_finishingEndOfRouteBehavior != null)
			{
				finishRouteBehavior(_finishingEndOfRouteBehavior);
			}
		}
		else
		{
			routeEndAnimationFinished(null);
		}
	}

	protected virtual void prepareToDisembarkOnNewSchedulePath()
	{
		finishEndOfRouteAnimation();
		doingEndOfRouteAnimation.Value = false;
		currentlyDoingEndOfRouteAnimation = false;
		if (!isMarried())
		{
			return;
		}
		if (temporaryController == null && Utility.getGameLocationOfCharacter(this) is FarmHouse)
		{
			temporaryController = new PathFindController(this, getHome(), new Point(getHome().warps[0].X, getHome().warps[0].Y), 2, clearMarriageDialogues: true)
			{
				NPCSchedule = true
			};
			if (temporaryController.pathToEndPoint == null || temporaryController.pathToEndPoint.Count <= 0)
			{
				temporaryController = null;
				ClearSchedule();
			}
			else
			{
				followSchedule = true;
			}
		}
		else if (Utility.getGameLocationOfCharacter(this) is Farm)
		{
			temporaryController = null;
			ClearSchedule();
		}
	}

	public void checkForMarriageDialogue(int timeOfDay, GameLocation location)
	{
		if (base.Name == "Krobus" && Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth) == "Fri")
		{
			return;
		}
		switch (timeOfDay)
		{
		case 1100:
			setRandomAfternoonMarriageDialogue(1100, location);
			break;
		case 1800:
			if (location is FarmHouse)
			{
				int num = Utility.CreateDaySaveRandom(timeOfDay, getSpouse().UniqueMultiplayerID).Next(Game1.isRaining ? 7 : 6) - 1;
				string text = ((num >= 0) ? (num.ToString() ?? "") : base.Name);
				currentMarriageDialogue.Clear();
				addMarriageDialogue("MarriageDialogue", (Game1.isRaining ? "Rainy" : "Indoor") + "_Night_" + text, false);
			}
			break;
		}
	}

	private void routeEndAnimationFinished(Farmer who)
	{
		doingEndOfRouteAnimation.Value = false;
		freezeMotion = false;
		CharacterData data = GetData();
		Sprite.SpriteWidth = data?.Size.X ?? 16;
		Sprite.SpriteHeight = data?.Size.Y ?? 32;
		Sprite.UpdateSourceRect();
		Sprite.oldFrame = _beforeEndOfRouteAnimationFrame;
		Sprite.StopAnimation();
		endOfRouteMessage.Value = null;
		isCharging = false;
		base.speed = 2;
		addedSpeed = 0f;
		goingToDoEndOfRouteAnimation.Value = false;
		if (isWalkingInSquare)
		{
			returningToEndPoint = true;
		}
		if (_finishingEndOfRouteBehavior == "penny_dishes")
		{
			drawOffset = Vector2.Zero;
		}
		if (appliedRouteAnimationOffset != Vector2.Zero)
		{
			drawOffset = Vector2.Zero;
			appliedRouteAnimationOffset = Vector2.Zero;
		}
		_finishingEndOfRouteBehavior = null;
	}

	public bool isOnSilentTemporaryMessage()
	{
		if ((doingEndOfRouteAnimation.Value || !goingToDoEndOfRouteAnimation.Value) && endOfRouteMessage.Value != null && endOfRouteMessage.Value.EqualsIgnoreCase("silent"))
		{
			return true;
		}
		return false;
	}

	public bool hasTemporaryMessageAvailable()
	{
		if (isDivorcedFrom(Game1.player))
		{
			return false;
		}
		if (base.currentLocation != null && base.currentLocation.HasLocationOverrideDialogue(this))
		{
			return true;
		}
		if (endOfRouteMessage.Value != null && (doingEndOfRouteAnimation.Value || !goingToDoEndOfRouteAnimation.Value))
		{
			return true;
		}
		return false;
	}

	public bool setTemporaryMessages(Farmer who)
	{
		if (isOnSilentTemporaryMessage())
		{
			return true;
		}
		if (endOfRouteMessage.Value != null && (doingEndOfRouteAnimation.Value || !goingToDoEndOfRouteAnimation.Value))
		{
			if (!isDivorcedFrom(Game1.player) && (!endOfRouteMessage.Value.Contains("marriage") || getSpouse() == Game1.player))
			{
				_PushTemporaryDialogue(endOfRouteMessage.Value);
				return false;
			}
		}
		else if (base.currentLocation != null && base.currentLocation.HasLocationOverrideDialogue(this))
		{
			_PushTemporaryDialogue(base.currentLocation.GetLocationOverrideDialogue(this));
			return false;
		}
		return false;
	}

	/// <summary>Add a dialogue to the NPC's queue which is shown before other dialogues, and replaced if another temporary dialogue is added later.</summary>
	/// <param name="translationKey">The translation from which to take the dialogue text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	protected void _PushTemporaryDialogue(string translationKey)
	{
		string text = translationKey;
		try
		{
			if (Game1.player.friendshipData.TryGetValue(base.Name, out var value))
			{
				string text2 = $"{translationKey}_{value.Status}";
				if (Game1.content.LoadStringReturnNullIfNotFound(text2) != null)
				{
					translationKey = text2;
				}
			}
			if (CurrentDialogue.Count == 0 || CurrentDialogue.Peek().temporaryDialogueKey != translationKey)
			{
				Dialogue item = new Dialogue(this, translationKey)
				{
					removeOnNextMove = true,
					temporaryDialogueKey = translationKey
				};
				CurrentDialogue.Push(item);
			}
		}
		catch (Exception exception)
		{
			Game1.log.Error($"NPC '{base.Name}' failed setting temporary dialogue key '{translationKey}'{((translationKey != text) ? (" (from dialogue key '" + text + "')") : "")}", exception);
		}
	}

	private void walkInSquareAtEndOfRoute(Character c, GameLocation l)
	{
		startRouteBehavior(endOfRouteBehaviorName.Value);
	}

	private void doAnimationAtEndOfScheduleRoute(Character c, GameLocation l)
	{
		doingEndOfRouteAnimation.Value = true;
		reallyDoAnimationAtEndOfScheduleRoute();
		currentlyDoingEndOfRouteAnimation = true;
	}

	private void reallyDoAnimationAtEndOfScheduleRoute()
	{
		_startedEndOfRouteBehavior = loadedEndOfRouteBehavior;
		bool flag = false;
		string startedEndOfRouteBehavior = _startedEndOfRouteBehavior;
		if (startedEndOfRouteBehavior == "change_beach" || startedEndOfRouteBehavior == "change_normal")
		{
			flag = true;
		}
		if (!flag)
		{
			if (_startedEndOfRouteBehavior == "penny_dishes")
			{
				drawOffset = new Vector2(0f, 16f);
			}
			if (_startedEndOfRouteBehavior.EndsWith("_sleep"))
			{
				layingDown = true;
				HideShadow = true;
			}
			if (routeAnimationMetadata != null)
			{
				for (int i = 0; i < routeAnimationMetadata.Length; i++)
				{
					string[] array = ArgUtility.SplitBySpace(routeAnimationMetadata[i]);
					startedEndOfRouteBehavior = array[0];
					if (!(startedEndOfRouteBehavior == "laying_down"))
					{
						if (startedEndOfRouteBehavior == "offset")
						{
							appliedRouteAnimationOffset = new Vector2(int.Parse(array[1]), int.Parse(array[2]));
						}
					}
					else
					{
						layingDown = true;
						HideShadow = true;
					}
				}
			}
			if (appliedRouteAnimationOffset != Vector2.Zero)
			{
				drawOffset = appliedRouteAnimationOffset;
			}
			if (_skipRouteEndIntro)
			{
				doMiddleAnimation(null);
			}
			else
			{
				Sprite.ClearAnimation();
				for (int j = 0; j < routeEndIntro.Length; j++)
				{
					if (j == routeEndIntro.Length - 1)
					{
						Sprite.AddFrame(new FarmerSprite.AnimationFrame(routeEndIntro[j], 100, 0, secondaryArm: false, flip: false, doMiddleAnimation, behaviorAtEndOfFrame: true));
					}
					else
					{
						Sprite.AddFrame(new FarmerSprite.AnimationFrame(routeEndIntro[j], 100, 0, secondaryArm: false, flip: false));
					}
				}
			}
		}
		_skipRouteEndIntro = false;
		doingEndOfRouteAnimation.Value = true;
		freezeMotion = true;
		_beforeEndOfRouteAnimationFrame = Sprite.oldFrame;
	}

	private void doMiddleAnimation(Farmer who)
	{
		Sprite.ClearAnimation();
		for (int i = 0; i < routeEndAnimation.Length; i++)
		{
			Sprite.AddFrame(new FarmerSprite.AnimationFrame(routeEndAnimation[i], 100, 0, secondaryArm: false, flip: false));
		}
		Sprite.loop = true;
		if (_startedEndOfRouteBehavior != null)
		{
			startRouteBehavior(_startedEndOfRouteBehavior);
		}
	}

	private void startRouteBehavior(string behaviorName)
	{
		if (behaviorName.Length > 0 && behaviorName[0] == '"')
		{
			if (Game1.IsMasterGame)
			{
				endOfRouteMessage.Value = behaviorName.Replace("\"", "");
			}
			return;
		}
		if (behaviorName.Contains("square_") && Game1.IsMasterGame)
		{
			lastCrossroad = new Microsoft.Xna.Framework.Rectangle(base.TilePoint.X * 64, base.TilePoint.Y * 64, 64, 64);
			string[] array = behaviorName.Split('_');
			walkInSquare(Convert.ToInt32(array[1]), Convert.ToInt32(array[2]), 6000);
			if (array.Length > 3)
			{
				squareMovementFacingPreference = Convert.ToInt32(array[3]);
			}
			else
			{
				squareMovementFacingPreference = -1;
			}
		}
		if (behaviorName.Contains("sleep"))
		{
			isPlayingSleepingAnimation = true;
			playSleepingAnimation();
		}
		switch (behaviorName)
		{
		case "abigail_videogames":
			if (Game1.IsMasterGame)
			{
				Game1.multiplayer.broadcastSprites(Utility.getGameLocationOfCharacter(this), new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(167, 1714, 19, 14), 100f, 3, 999999, new Vector2(2f, 3f) * 64f + new Vector2(7f, 12f) * 4f, flicker: false, flipped: false, 0.0002f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					id = 688
				});
				doEmote(52);
			}
			break;
		case "dick_fish":
			extendSourceRect(0, 32);
			Sprite.tempSpriteHeight = 64;
			drawOffset = new Vector2(0f, 96f);
			Sprite.ignoreSourceRectUpdates = false;
			if (Utility.isOnScreen(Utility.Vector2ToPoint(base.Position), 64, base.currentLocation))
			{
				base.currentLocation.playSound("slosh", base.Tile);
			}
			break;
		case "clint_hammer":
			extendSourceRect(16, 0);
			Sprite.SpriteWidth = 32;
			Sprite.ignoreSourceRectUpdates = false;
			Sprite.currentFrame = 8;
			Sprite.CurrentAnimation[14] = new FarmerSprite.AnimationFrame(9, 100, 0, secondaryArm: false, flip: false, clintHammerSound);
			break;
		case "birdie_fish":
			extendSourceRect(16, 0);
			Sprite.SpriteWidth = 32;
			Sprite.ignoreSourceRectUpdates = false;
			Sprite.currentFrame = 8;
			break;
		}
	}

	public void playSleepingAnimation()
	{
		isSleeping.Value = true;
		Vector2 vector = new Vector2(0f, name.Equals("Sebastian") ? 12 : (-4));
		if (isMarried())
		{
			vector.X = -12f;
		}
		drawOffset = vector;
		if (!isPlayingSleepingAnimation)
		{
			if (DataLoader.AnimationDescriptions(Game1.content).TryGetValue(name.Value.ToLower() + "_sleep", out var value))
			{
				int frame = Convert.ToInt32(value.Split('/')[0]);
				Sprite.ClearAnimation();
				Sprite.AddFrame(new FarmerSprite.AnimationFrame(frame, 100, secondaryArm: false, flip: false));
				Sprite.loop = true;
			}
			isPlayingSleepingAnimation = true;
		}
	}

	private void finishRouteBehavior(string behaviorName)
	{
		switch (behaviorName)
		{
		case "abigail_videogames":
			Utility.getGameLocationOfCharacter(this).removeTemporarySpritesWithID(688);
			break;
		case "birdie_fish":
		case "clint_hammer":
		case "dick_fish":
		{
			reloadSprite();
			CharacterData data = GetData();
			Sprite.SpriteWidth = data?.Size.X ?? 16;
			Sprite.SpriteHeight = data?.Size.Y ?? 32;
			Sprite.UpdateSourceRect();
			drawOffset = Vector2.Zero;
			Halt();
			movementPause = 1;
			break;
		}
		}
		if (layingDown)
		{
			layingDown = false;
			HideShadow = false;
		}
	}

	public bool IsReturningToEndPoint()
	{
		return returningToEndPoint;
	}

	public void StartActivityWalkInSquare(int square_width, int square_height, int pause_offset)
	{
		Point tilePoint = base.TilePoint;
		lastCrossroad = new Microsoft.Xna.Framework.Rectangle(tilePoint.X * 64, tilePoint.Y * 64, 64, 64);
		walkInSquare(square_height, square_height, pause_offset);
	}

	public void EndActivityRouteEndBehavior()
	{
		finishEndOfRouteAnimation();
	}

	public void StartActivityRouteEndBehavior(string behavior_name, string end_message)
	{
		getRouteEndBehaviorFunction(behavior_name, end_message)?.Invoke(this, base.currentLocation);
	}

	protected PathFindController.endBehavior getRouteEndBehaviorFunction(string behaviorName, string endMessage)
	{
		if (endMessage != null || (behaviorName != null && behaviorName.Length > 0 && behaviorName[0] == '"'))
		{
			nextEndOfRouteMessage = endMessage.Replace("\"", "");
		}
		if (behaviorName != null)
		{
			if (behaviorName.Length > 0 && behaviorName.Contains("square_"))
			{
				endOfRouteBehaviorName.Value = behaviorName;
				return walkInSquareAtEndOfRoute;
			}
			Dictionary<string, string> dictionary = DataLoader.AnimationDescriptions(Game1.content);
			if (behaviorName == "change_beach" || behaviorName == "change_normal")
			{
				endOfRouteBehaviorName.Value = behaviorName;
				goingToDoEndOfRouteAnimation.Value = true;
			}
			else
			{
				if (!dictionary.ContainsKey(behaviorName))
				{
					return null;
				}
				endOfRouteBehaviorName.Value = behaviorName;
				loadEndOfRouteBehavior(endOfRouteBehaviorName.Value);
				goingToDoEndOfRouteAnimation.Value = true;
			}
			return doAnimationAtEndOfScheduleRoute;
		}
		return null;
	}

	private void loadEndOfRouteBehavior(string name)
	{
		loadedEndOfRouteBehavior = name;
		if (name.Length > 0 && name.Contains("square_"))
		{
			return;
		}
		string value = null;
		try
		{
			if (DataLoader.AnimationDescriptions(Game1.content).TryGetValue(name, out value))
			{
				string[] array = value.Split('/');
				routeEndIntro = Utility.parseStringToIntArray(array[0]);
				routeEndAnimation = Utility.parseStringToIntArray(array[1]);
				routeEndOutro = Utility.parseStringToIntArray(array[2]);
				if (array.Length > 3 && array[3] != "")
				{
					nextEndOfRouteMessage = array[3];
				}
				if (array.Length > 4)
				{
					routeAnimationMetadata = array.Skip(4).ToArray();
				}
				else
				{
					routeAnimationMetadata = null;
				}
			}
		}
		catch (Exception exception)
		{
			Game1.log.Error($"NPC {base.Name} failed to apply end-of-route behavior '{name}'{((value != null) ? (" with raw data '" + value + "'") : "")}.", exception);
		}
	}

	public void shake(int duration)
	{
		shakeTimer = duration;
	}

	public void setNewDialogue(string translationKey, bool add = false, bool clearOnMovement = false)
	{
		setNewDialogue(new Dialogue(this, translationKey), add, clearOnMovement);
	}

	public void setNewDialogue(Dialogue dialogue, bool add = false, bool clearOnMovement = false)
	{
		if (!add)
		{
			CurrentDialogue.Clear();
		}
		dialogue.removeOnNextMove = clearOnMovement;
		CurrentDialogue.Push(dialogue);
	}

	private void setNewDialogue(string dialogueSheetName, string dialogueSheetKey, bool clearOnMovement = false)
	{
		CurrentDialogue.Clear();
		string text = dialogueSheetKey + base.Name;
		if (dialogueSheetName.Contains("Marriage"))
		{
			if (getSpouse() == Game1.player)
			{
				Dialogue dialogue = tryToGetMarriageSpecificDialogue(text);
				if (dialogue == null)
				{
					Game1.log.Warn($"NPC '{base.Name}' couldn't set marriage dialogue key '{text}': not found.");
					dialogue = StardewValley.Dialogue.GetFallbackForError(this);
				}
				dialogue.removeOnNextMove = clearOnMovement;
				CurrentDialogue.Push(dialogue);
			}
			return;
		}
		string text2 = "Characters\\Dialogue\\" + dialogueSheetName + ":" + text;
		Dialogue dialogue2 = StardewValley.Dialogue.TryGetDialogue(this, text2);
		if (dialogue2 == null)
		{
			Game1.log.Warn($"NPC '{base.Name}' couldn't set dialogue key '{text2}': not found.");
			dialogue2 = StardewValley.Dialogue.GetFallbackForError(this);
		}
		if (dialogue2 != null)
		{
			dialogue2.removeOnNextMove = clearOnMovement;
			CurrentDialogue.Push(dialogue2);
		}
	}

	public string GetDialogueSheetName()
	{
		if (base.Name == "Leo" && DefaultMap != "IslandHut")
		{
			return base.Name + "Mainland";
		}
		return base.Name;
	}

	public void setSpouseRoomMarriageDialogue()
	{
		currentMarriageDialogue.Clear();
		addMarriageDialogue("MarriageDialogue", "spouseRoom_" + base.Name, false);
	}

	public void setRandomAfternoonMarriageDialogue(int time, GameLocation location, bool countAsDailyAfternoon = false)
	{
		if ((base.Name == "Krobus" && Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth) == "Fri") || hasSaidAfternoonDialogue.Value)
		{
			return;
		}
		if (countAsDailyAfternoon)
		{
			hasSaidAfternoonDialogue.Value = true;
		}
		Random random = Utility.CreateDaySaveRandom(time);
		int friendshipHeartLevelForNPC = getSpouse().getFriendshipHeartLevelForNPC(base.Name);
		if (!(location is FarmHouse))
		{
			if (location is Farm)
			{
				currentMarriageDialogue.Clear();
				if (random.NextDouble() < 0.2)
				{
					addMarriageDialogue("MarriageDialogue", "Outdoor_" + base.Name, false);
				}
				else
				{
					addMarriageDialogue("MarriageDialogue", "Outdoor_" + random.Next(5), false);
				}
			}
		}
		else if (random.NextBool())
		{
			if (friendshipHeartLevelForNPC < 9)
			{
				currentMarriageDialogue.Clear();
				addMarriageDialogue("MarriageDialogue", ((random.NextDouble() < (double)((float)friendshipHeartLevelForNPC / 11f)) ? "Neutral_" : "Bad_") + random.Next(10), false);
			}
			else if (random.NextDouble() < 0.05)
			{
				currentMarriageDialogue.Clear();
				addMarriageDialogue("MarriageDialogue", Game1.currentSeason + "_" + base.Name, false);
			}
			else if ((friendshipHeartLevelForNPC >= 10 && random.NextBool()) || (friendshipHeartLevelForNPC >= 11 && random.NextDouble() < 0.75) || (friendshipHeartLevelForNPC >= 12 && random.NextDouble() < 0.95))
			{
				currentMarriageDialogue.Clear();
				addMarriageDialogue("MarriageDialogue", "Good_" + random.Next(10), false);
			}
			else
			{
				currentMarriageDialogue.Clear();
				addMarriageDialogue("MarriageDialogue", "Neutral_" + random.Next(10), false);
			}
		}
	}

	/// <summary>Get whether it's the NPC's birthday today.</summary>
	public bool isBirthday()
	{
		if (Birthday_Season == Game1.currentSeason)
		{
			return Birthday_Day == Game1.dayOfMonth;
		}
		return false;
	}

	/// <summary>Get the NPC's first loved item for the Statue of Endless Fortune.</summary>
	public Item getFavoriteItem()
	{
		if (Game1.NPCGiftTastes.TryGetValue(base.Name, out var value))
		{
			Item item = (from id in ArgUtility.SplitBySpace(value.Split('/')[1])
				select ItemRegistry.ResolveMetadata(id)?.CreateItem()).FirstOrDefault((Item p) => p != null);
			if (item != null)
			{
				return item;
			}
		}
		return null;
	}

	/// <summary>Get the NPC's data from <see cref="F:StardewValley.Game1.characterData" />, if found.</summary>
	public CharacterData GetData()
	{
		if (!IsVillager || !TryGetData(name.Value, out var data))
		{
			return null;
		}
		return data;
	}

	/// <summary>Try to get an NPC's data from <see cref="F:StardewValley.Game1.characterData" />.</summary>
	/// <param name="name">The NPC's internal name (i.e. the key in <see cref="F:StardewValley.Game1.characterData" />).</param>
	/// <param name="data">The NPC data, if found.</param>
	/// <returns>Returns whether the NPC data was found.</returns>
	public static bool TryGetData(string name, out CharacterData data)
	{
		if (name == null)
		{
			data = null;
			return false;
		}
		return Game1.characterData.TryGetValue(name, out data);
	}

	/// <summary>Get the translated display name for an NPC from the underlying data, if any.</summary>
	/// <param name="name">The NPC's internal name.</param>
	public static string GetDisplayName(string name)
	{
		TryGetData(name, out var data);
		return TokenParser.ParseText(data?.DisplayName) ?? name;
	}

	/// <summary>Get whether social features (like birthdays, gift giving, friendship, and an entry in the social tab) should be enabled for an NPC based on their entry in <c>Data/Characters</c>.</summary>
	/// <param name="name">The NPC name to check.</param>
	/// <param name="location">The location to assume they're in when parsing conditions.</param>
	/// <remarks>Most code should use <see cref="P:StardewValley.NPC.CanSocialize" /> instead.</remarks>
	public static bool CanSocializePerData(string name, GameLocation location)
	{
		if (TryGetData(name, out var data))
		{
			return GameStateQuery.CheckConditions(data.CanSocialize, location);
		}
		return false;
	}

	/// <summary>Get a tokenized string for the NPC's display name.</summary>
	public string GetTokenizedDisplayName()
	{
		return GetData()?.DisplayName ?? displayName;
	}

	/// <summary>Get whether this NPC speaks Dwarvish, which the player can only understand after finding the Dwarvish Translation Guide.</summary>
	public bool SpeaksDwarvish()
	{
		CharacterData data = GetData();
		if (data == null)
		{
			return false;
		}
		return data.Language == NpcLanguage.Dwarvish;
	}

	public virtual void receiveGift(Object o, Farmer giver, bool updateGiftLimitInfo = true, float friendshipChangeMultiplier = 1f, bool showResponse = true)
	{
		if (CanReceiveGifts())
		{
			float num = 1f;
			switch (o.Quality)
			{
			case 1:
				num = 1.1f;
				break;
			case 2:
				num = 1.25f;
				break;
			case 4:
				num = 1.5f;
				break;
			}
			if (isBirthday())
			{
				friendshipChangeMultiplier = 8f;
			}
			if (getSpouse() != null && getSpouse().Equals(giver))
			{
				friendshipChangeMultiplier /= 2f;
			}
			giver.onGiftGiven(this, o);
			Game1.stats.GiftsGiven++;
			giver.currentLocation.localSound("give_gift");
			if (updateGiftLimitInfo)
			{
				giver.friendshipData[base.Name].GiftsToday++;
				giver.friendshipData[base.Name].GiftsThisWeek++;
				giver.friendshipData[base.Name].LastGiftDate = new WorldDate(Game1.Date);
			}
			switch (giver.FacingDirection)
			{
			case 0:
				((FarmerSprite)giver.Sprite).animateBackwardsOnce(80, 50f);
				break;
			case 1:
				((FarmerSprite)giver.Sprite).animateBackwardsOnce(72, 50f);
				break;
			case 2:
				((FarmerSprite)giver.Sprite).animateBackwardsOnce(64, 50f);
				break;
			case 3:
				((FarmerSprite)giver.Sprite).animateBackwardsOnce(88, 50f);
				break;
			}
			int giftTasteForThisItem = getGiftTasteForThisItem(o);
			switch (giftTasteForThisItem)
			{
			case 7:
				giver.changeFriendship(Math.Min(750, (int)(250f * friendshipChangeMultiplier)), this);
				doEmote(56);
				faceTowardFarmerForPeriod(15000, 4, faceAway: false, giver);
				break;
			case 0:
				giver.changeFriendship((int)(80f * friendshipChangeMultiplier * num), this);
				doEmote(20);
				faceTowardFarmerForPeriod(15000, 4, faceAway: false, giver);
				break;
			case 6:
				giver.changeFriendship((int)(-40f * friendshipChangeMultiplier), this);
				doEmote(12);
				faceTowardFarmerForPeriod(15000, 4, faceAway: true, giver);
				break;
			case 2:
				giver.changeFriendship((int)(45f * friendshipChangeMultiplier * num), this);
				faceTowardFarmerForPeriod(7000, 3, faceAway: true, giver);
				break;
			case 4:
				giver.changeFriendship((int)(-20f * friendshipChangeMultiplier), this);
				break;
			default:
				giver.changeFriendship((int)(20f * friendshipChangeMultiplier), this);
				break;
			}
			if (showResponse)
			{
				Game1.DrawDialogue(GetGiftReaction(giver, o, giftTasteForThisItem));
			}
		}
	}

	/// <summary>Get the NPC's reaction dialogue for receiving an item as a gift.</summary>
	/// <param name="giver">The player giving the gift.</param>
	/// <param name="gift">The item being gifted.</param>
	/// <param name="taste">The NPC's gift taste for this item, as returned by <see cref="M:StardewValley.NPC.getGiftTasteForThisItem(StardewValley.Item)" />.</param>
	/// <returns>Returns the dialogue if the NPC can receive gifts, else <c>null</c>.</returns>
	public virtual Dialogue GetGiftReaction(Farmer giver, Object gift, int taste)
	{
		if (!CanReceiveGifts() || !Game1.NPCGiftTastes.TryGetValue(base.Name, out var value))
		{
			return null;
		}
		Dialogue dialogue = null;
		string text = null;
		if (base.Name == "Krobus" && Game1.Date.DayOfWeek == DayOfWeek.Friday)
		{
			dialogue = TryGetDialogue("Fri") ?? StardewValley.Dialogue.GetFallbackForError(this);
		}
		else if (isBirthday())
		{
			dialogue = TryGetDialogue("AcceptBirthdayGift_" + gift.QualifiedItemId) ?? (from tag in gift.GetContextTags()
				select TryGetDialogueByGiftTaste(taste, (string tasteTag) => "AcceptBirthdayGift_" + tasteTag + "_" + tag)).FirstOrDefault((Dialogue p) => p != null) ?? (from tag in gift.GetContextTags()
				select TryGetDialogue("AcceptBirthdayGift_" + tag)).FirstOrDefault((Dialogue p) => p != null) ?? TryGetDialogueByGiftTaste(taste, (string tasteTag) => "AcceptBirthdayGift_" + tasteTag) ?? TryGetDialogue("AcceptBirthdayGift");
			switch (taste)
			{
			case 0:
			case 2:
			case 7:
				text = "$h";
				dialogue = dialogue ?? ((!Game1.random.NextBool()) ? ((Manners == 2) ? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4274", isGendered: true) : new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4275")) : ((Manners == 2) ? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4276", isGendered: true) : new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4277", isGendered: true)));
				break;
			case 4:
			case 6:
				text = "$s";
				dialogue = dialogue ?? ((Manners == 2) ? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4278", isGendered: true) : new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4279", isGendered: true));
				break;
			default:
				dialogue = dialogue ?? ((Manners == 2) ? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4280") : new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4281", isGendered: true));
				break;
			}
		}
		else
		{
			dialogue = TryGetDialogue("AcceptGift_" + gift.QualifiedItemId) ?? (from tag in gift.GetContextTags()
				select TryGetDialogueByGiftTaste(taste, (string tasteTag) => "AcceptGift_" + tasteTag + "_" + tag)).FirstOrDefault((Dialogue p) => p != null) ?? (from tag in gift.GetContextTags()
				select TryGetDialogue("AcceptGift_" + tag)).FirstOrDefault((Dialogue p) => p != null) ?? TryGetDialogueByGiftTaste(taste, (string tasteTag) => "AcceptGift_" + tasteTag) ?? TryGetDialogue("AcceptGift");
			string[] array = value.Split('/');
			switch (taste)
			{
			case 7:
				text = "$h";
				dialogue = dialogue ?? new Dialogue(this, null, ArgUtility.Get(array, 0));
				break;
			case 0:
			case 2:
				if (dialogue == null)
				{
					text = "$h";
				}
				dialogue = dialogue ?? new Dialogue(this, null, ArgUtility.Get(array, taste));
				break;
			case 4:
			case 6:
				text = "$s";
				dialogue = dialogue ?? new Dialogue(this, null, ArgUtility.Get(array, taste));
				break;
			default:
				dialogue = dialogue ?? new Dialogue(this, null, ArgUtility.Get(array, 8));
				break;
			}
		}
		if (!giver.canUnderstandDwarves && SpeaksDwarvish())
		{
			dialogue.convertToDwarvish();
		}
		else if (text != null && !dialogue.CurrentEmotionSetExplicitly)
		{
			dialogue.CurrentEmotion = text;
		}
		return dialogue;
	}

	public override void draw(SpriteBatch b, float alpha = 1f)
	{
		int y = base.StandingPixel.Y;
		float layerDepth = Math.Max(0f, drawOnTop ? 0.991f : ((float)y / 10000f));
		if (Sprite.Texture == null)
		{
			Vector2 vector = Game1.GlobalToLocal(Game1.viewport, base.Position);
			Microsoft.Xna.Framework.Rectangle screenArea = new Microsoft.Xna.Framework.Rectangle((int)vector.X, (int)vector.Y - Sprite.SpriteWidth * 4, Sprite.SpriteWidth * 4, Sprite.SpriteHeight * 4);
			Utility.DrawErrorTexture(b, screenArea, layerDepth);
		}
		else if (!IsInvisible && (Utility.isOnScreen(base.Position, 128) || (EventActor && base.currentLocation is Summit)))
		{
			if (swimming.Value)
			{
				b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(32f, 80 + yJumpOffset * 2) + ((shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero) - new Vector2(0f, yOffset), new Microsoft.Xna.Framework.Rectangle(Sprite.SourceRect.X, Sprite.SourceRect.Y, Sprite.SourceRect.Width, Sprite.SourceRect.Height / 2 - (int)(yOffset / 4f)), Color.White, rotation, new Vector2(32f, 96f) / 4f, Math.Max(0.2f, scale.Value) * 4f, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
				Vector2 localPosition = getLocalPosition(Game1.viewport);
				b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((int)localPosition.X + (int)yOffset + 8, (int)localPosition.Y - 128 + Sprite.SourceRect.Height * 4 + 48 + yJumpOffset * 2 - (int)yOffset, Sprite.SourceRect.Width * 4 - (int)yOffset * 2 - 16, 4), Game1.staminaRect.Bounds, Color.White * 0.75f, 0f, Vector2.Zero, SpriteEffects.None, (float)y / 10000f + 0.001f);
			}
			else
			{
				b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(GetSpriteWidthForPositioning() * 4 / 2, GetBoundingBox().Height / 2) + ((shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), Sprite.SourceRect, Color.White * alpha, rotation, new Vector2(Sprite.SpriteWidth / 2, (float)Sprite.SpriteHeight * 3f / 4f), Math.Max(0.2f, scale.Value) * 4f, (flip || (Sprite.CurrentAnimation != null && Sprite.CurrentAnimation[Sprite.currentAnimationIndex].flip)) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
			}
			DrawBreathing(b, alpha);
			DrawGlow(b);
			if (!Game1.eventUp)
			{
				DrawEmote(b);
			}
		}
	}

	public virtual void DrawBreathing(SpriteBatch b, float alpha = 1f)
	{
		if (!Breather || shakeTimer > 0 || swimming.Value || farmerPassesThrough)
		{
			return;
		}
		AnimatedSprite animatedSprite = Sprite;
		if (animatedSprite != null && animatedSprite.SpriteHeight > 32)
		{
			return;
		}
		AnimatedSprite animatedSprite2 = Sprite;
		if (animatedSprite2 != null && animatedSprite2.SpriteWidth > 16)
		{
			return;
		}
		AnimatedSprite animatedSprite3 = Sprite;
		if (animatedSprite3.currentFrame >= 16)
		{
			return;
		}
		CharacterData data = GetData();
		Microsoft.Xna.Framework.Rectangle sourceRect = animatedSprite3.SourceRect;
		Microsoft.Xna.Framework.Rectangle value2;
		if (data != null && data.BreathChestRect.HasValue)
		{
			Microsoft.Xna.Framework.Rectangle value = data.BreathChestRect.Value;
			value2 = new Microsoft.Xna.Framework.Rectangle(sourceRect.X + value.X, sourceRect.Y + value.Y, value.Width, value.Height);
		}
		else
		{
			value2 = new Microsoft.Xna.Framework.Rectangle(sourceRect.X + animatedSprite3.SpriteWidth / 4, sourceRect.Y + animatedSprite3.SpriteHeight / 2 + animatedSprite3.SpriteHeight / 32, animatedSprite3.SpriteHeight / 4, animatedSprite3.SpriteWidth / 2);
			if (Age == 2)
			{
				value2.Y += animatedSprite3.SpriteHeight / 6 + 1;
				value2.Height /= 2;
			}
			else if (Gender == Gender.Female)
			{
				value2.Y++;
				value2.Height /= 2;
			}
		}
		Vector2 vector;
		if (data != null && data.BreathChestPosition.HasValue)
		{
			vector = Utility.PointToVector2(data.BreathChestPosition.Value);
		}
		else
		{
			vector = new Vector2(animatedSprite3.SpriteWidth * 4 / 2, 8f);
			if (Age == 2)
			{
				vector.Y += animatedSprite3.SpriteHeight / 8 * 4;
				if (this is Child { Age: var num })
				{
					switch (num)
					{
					case 0:
						vector.X -= 12f;
						break;
					case 1:
						vector.X -= 4f;
						break;
					}
				}
			}
			else if (Gender == Gender.Female)
			{
				vector.Y -= 4f;
			}
		}
		float num2 = Math.Max(0f, (float)Math.Ceiling(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 600.0 + (double)(defaultPosition.X * 20f))) / 4f);
		int y = base.StandingPixel.Y;
		b.Draw(animatedSprite3.Texture, getLocalPosition(Game1.viewport) + vector + ((shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), value2, Color.White * alpha, rotation, new Vector2(value2.Width / 2, value2.Height / 2 + 1), Math.Max(0.2f, scale.Value) * 4f + num2, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.992f : (((float)y + 0.01f) / 10000f)));
	}

	public virtual void DrawGlow(SpriteBatch b)
	{
		int y = base.StandingPixel.Y;
		if (isGlowing)
		{
			b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(GetSpriteWidthForPositioning() * 4 / 2, GetBoundingBox().Height / 2) + ((shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), Sprite.SourceRect, glowingColor * glowingTransparency, rotation, new Vector2(Sprite.SpriteWidth / 2, (float)Sprite.SpriteHeight * 3f / 4f), Math.Max(0.2f, scale.Value) * 4f, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.99f : ((float)y / 10000f + 0.001f)));
		}
	}

	public virtual void DrawEmote(SpriteBatch b)
	{
		if (base.IsEmoting && !(this is Child) && !(this is Pet))
		{
			Point point = GetData()?.EmoteOffset ?? Point.Zero;
			Vector2 localPosition = getLocalPosition(Game1.viewport);
			localPosition = new Vector2(localPosition.X + (float)point.X + ((float)Sprite.SourceRect.Width / 2f - 8f) * 4f, localPosition.Y + (float)point.Y + (float)emoteYOffset - (float)((Sprite.SpriteHeight + 3) * 4));
			if (NeedsBirdieEmoteHack())
			{
				localPosition.X += 64f;
			}
			if (Age == 2)
			{
				localPosition.Y += 32f;
			}
			else if (Gender == Gender.Female)
			{
				localPosition.Y += 10f;
			}
			b.Draw(Game1.emoteSpriteSheet, localPosition, new Microsoft.Xna.Framework.Rectangle(base.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, base.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)base.StandingPixel.Y / 10000f);
		}
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		if (textAboveHeadTimer > 0 && textAboveHead != null)
		{
			Point standingPixel = base.StandingPixel;
			Vector2 vector = Game1.GlobalToLocal(new Vector2(standingPixel.X, standingPixel.Y - Sprite.SpriteHeight * 4 - 64 + yJumpOffset));
			if (textAboveHeadStyle == 0)
			{
				vector += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			}
			if (NeedsBirdieEmoteHack())
			{
				vector.X += -GetBoundingBox().Width / 4 + 64;
			}
			if (shouldShadowBeOffset)
			{
				vector += drawOffset;
			}
			Point tilePoint = base.TilePoint;
			SpriteText.drawStringWithScrollCenteredAt(b, textAboveHead, (int)vector.X, (int)vector.Y, "", textAboveHeadAlpha, textAboveHeadColor, 1, (float)(tilePoint.Y * 64) / 10000f + 0.001f + (float)tilePoint.X / 10000f);
		}
	}

	public bool NeedsBirdieEmoteHack()
	{
		if (Game1.eventUp && Sprite.SpriteWidth == 32 && base.Name == "Birdie")
		{
			return true;
		}
		return false;
	}

	public void warpToPathControllerDestination()
	{
		if (controller != null)
		{
			while (controller.pathToEndPoint.Count > 2)
			{
				controller.pathToEndPoint.Pop();
				controller.handleWarps(new Microsoft.Xna.Framework.Rectangle(controller.pathToEndPoint.Peek().X * 64, controller.pathToEndPoint.Peek().Y * 64, 64, 64));
				base.Position = new Vector2(controller.pathToEndPoint.Peek().X * 64, controller.pathToEndPoint.Peek().Y * 64 + 16);
				Halt();
			}
		}
	}

	/// <summary>Get the pixel area in the <see cref="P:StardewValley.Character.Sprite" /> texture to show as the NPC's icon in contexts like the calendar and social menu.</summary>
	public virtual Microsoft.Xna.Framework.Rectangle getMugShotSourceRect()
	{
		return GetData()?.MugShotSourceRect ?? new Microsoft.Xna.Framework.Rectangle(0, (Age == 2) ? 4 : 0, 16, 24);
	}

	public void getHitByPlayer(Farmer who, GameLocation location)
	{
		doEmote(12);
		if (who == null)
		{
			if (Game1.IsMultiplayer)
			{
				return;
			}
			who = Game1.player;
		}
		if (who.friendshipData.ContainsKey(base.Name))
		{
			who.changeFriendship(-30, this);
			if (who.IsLocalPlayer)
			{
				CurrentDialogue.Clear();
				CurrentDialogue.Push(TryGetDialogue("HitBySlingshot") ?? (Game1.random.NextBool() ? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4293", isGendered: true) : new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4294")));
			}
			if (Sprite.Texture != null)
			{
				location.debris.Add(new Debris(Sprite.textureName.Value, Game1.random.Next(3, 8), Utility.PointToVector2(base.StandingPixel)));
			}
		}
		if (base.Name.Equals("Bouncer"))
		{
			location.localSound("crafting");
		}
		else
		{
			location.localSound("hitEnemy");
		}
	}

	public void walkInSquare(int squareWidth, int squareHeight, int squarePauseOffset)
	{
		isWalkingInSquare = true;
		lengthOfWalkingSquareX = squareWidth;
		lengthOfWalkingSquareY = squareHeight;
		this.squarePauseOffset = squarePauseOffset;
	}

	public void moveTowardPlayer(int threshold)
	{
		isWalkingTowardPlayer.Value = true;
		moveTowardPlayerThreshold.Value = threshold;
	}

	protected virtual Farmer findPlayer()
	{
		return Game1.MasterPlayer;
	}

	public virtual bool withinPlayerThreshold()
	{
		return withinPlayerThreshold(moveTowardPlayerThreshold.Value);
	}

	public virtual bool withinPlayerThreshold(int threshold)
	{
		if (base.currentLocation != null && !base.currentLocation.farmers.Any())
		{
			return false;
		}
		Vector2 tile = findPlayer().Tile;
		Vector2 tile2 = base.Tile;
		if (Math.Abs(tile2.X - tile.X) <= (float)threshold && Math.Abs(tile2.Y - tile.Y) <= (float)threshold)
		{
			return true;
		}
		return false;
	}

	private Stack<Point> addToStackForSchedule(Stack<Point> original, Stack<Point> toAdd)
	{
		if (toAdd == null)
		{
			return original;
		}
		original = new Stack<Point>(original);
		while (original.Count > 0)
		{
			toAdd.Push(original.Pop());
		}
		return toAdd;
	}

	public virtual SchedulePathDescription pathfindToNextScheduleLocation(string scheduleKey, string startingLocation, int startingX, int startingY, string endingLocation, int endingX, int endingY, int finalFacingDirection, string endBehavior, string endMessage)
	{
		Stack<Point> stack = new Stack<Point>();
		Point point = new Point(startingX, startingY);
		if (point == Point.Zero)
		{
			throw new Exception($"NPC {base.Name} has an invalid schedule with key '{scheduleKey}': start position in {startingLocation} is at tile (0, 0), which isn't valid.");
		}
		string[] array = ((!startingLocation.Equals(endingLocation, StringComparison.Ordinal)) ? getLocationRoute(startingLocation, endingLocation) : null);
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				string key = array[i];
				foreach (string activePassiveFestival in Game1.netWorldState.Value.ActivePassiveFestivals)
				{
					if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data) && data.MapReplacements != null && data.MapReplacements.TryGetValue(key, out var value))
					{
						key = value;
						break;
					}
				}
				GameLocation gameLocation = Game1.RequireLocation(key);
				if (gameLocation.Name.Equals("Trailer") && Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
				{
					gameLocation = Game1.RequireLocation("Trailer_Big");
				}
				if (i < array.Length - 1)
				{
					Point warpPointTo = gameLocation.getWarpPointTo(array[i + 1]);
					if (warpPointTo == Point.Zero)
					{
						throw new Exception($"NPC {base.Name} has an invalid schedule with key '{scheduleKey}': it requires a warp from {gameLocation.NameOrUniqueName} to {array[i + 1]}, but none was found.");
					}
					stack = addToStackForSchedule(stack, PathFindController.findPathForNPCSchedules(point, warpPointTo, gameLocation, 30000, this));
					point = gameLocation.getWarpPointTarget(warpPointTo, this);
				}
				else
				{
					stack = addToStackForSchedule(stack, PathFindController.findPathForNPCSchedules(point, new Point(endingX, endingY), gameLocation, 30000, this));
				}
			}
		}
		else if (startingLocation.Equals(endingLocation, StringComparison.Ordinal))
		{
			string key2 = startingLocation;
			foreach (string activePassiveFestival2 in Game1.netWorldState.Value.ActivePassiveFestivals)
			{
				if (Utility.TryGetPassiveFestivalData(activePassiveFestival2, out var data2) && data2.MapReplacements != null && data2.MapReplacements.TryGetValue(key2, out var value2))
				{
					key2 = value2;
					break;
				}
			}
			GameLocation gameLocation2 = Game1.RequireLocation(key2);
			if (gameLocation2.Name.Equals("Trailer") && Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
			{
				gameLocation2 = Game1.RequireLocation("Trailer_Big");
			}
			stack = PathFindController.findPathForNPCSchedules(point, new Point(endingX, endingY), gameLocation2, 30000, this);
		}
		return new SchedulePathDescription(stack, finalFacingDirection, endBehavior, endMessage, endingLocation, new Point(endingX, endingY));
	}

	private string[] getLocationRoute(string startingLocation, string endingLocation)
	{
		return WarpPathfindingCache.GetLocationRoute(startingLocation, endingLocation, Gender);
	}

	/// <summary>
	/// returns true if location is inaccessable and should use "Default" instead.
	///
	///
	/// </summary>
	/// <param name="locationName"></param>
	/// <param name="tileX"></param>
	/// <param name="tileY"></param>
	/// <param name="facingDirection"></param>
	/// <returns></returns>
	private bool changeScheduleForLocationAccessibility(ref string locationName, ref int tileX, ref int tileY, ref int facingDirection)
	{
		switch (locationName)
		{
		case "JojaMart":
		case "Railroad":
			if (!Game1.isLocationAccessible(locationName))
			{
				if (!hasMasterScheduleEntry(locationName + "_Replacement"))
				{
					return true;
				}
				string[] array = ArgUtility.SplitBySpace(getMasterScheduleEntry(locationName + "_Replacement"));
				locationName = array[0];
				tileX = Convert.ToInt32(array[1]);
				tileY = Convert.ToInt32(array[2]);
				facingDirection = Convert.ToInt32(array[3]);
			}
			break;
		case "CommunityCenter":
			return !Game1.isLocationAccessible(locationName);
		}
		return false;
	}

	/// <inheritdoc cref="M:StardewValley.NPC.parseMasterScheduleImpl(System.String,System.String,System.Collections.Generic.List{System.String})" />
	public virtual Dictionary<int, SchedulePathDescription> parseMasterSchedule(string scheduleKey, string rawData)
	{
		return parseMasterScheduleImpl(scheduleKey, rawData, new List<string>());
	}

	/// <summary>Parse a schedule script into its component commands, handling redirection like <c>GOTO</c> automatically.</summary>
	/// <param name="scheduleKey">The schedule key being parsed.</param>
	/// <param name="rawData">The raw schedule script to parse.</param>
	/// <param name="visited">The schedule keys which led to this parse (if any).</param>
	/// <remarks>This is a low-level method. Most code should call <see cref="M:StardewValley.NPC.TryLoadSchedule(System.String)" /> instead.</remarks>
	protected virtual Dictionary<int, SchedulePathDescription> parseMasterScheduleImpl(string scheduleKey, string rawData, List<string> visited)
	{
		if (visited.Contains<string>(scheduleKey, StringComparer.OrdinalIgnoreCase))
		{
			Game1.log.Warn($"NPC {base.Name} can't load schedules because they led to an infinite loop ({string.Join(" -> ", visited)} -> {scheduleKey}).");
			return new Dictionary<int, SchedulePathDescription>();
		}
		visited.Add(scheduleKey);
		try
		{
			string[] array = SplitScheduleCommands(rawData);
			Dictionary<int, SchedulePathDescription> dictionary = new Dictionary<int, SchedulePathDescription>();
			int num = 0;
			if (array[0].Contains("GOTO"))
			{
				string text = ArgUtility.SplitBySpaceAndGet(array[0], 1);
				Dictionary<string, string> masterScheduleRawData = getMasterScheduleRawData();
				if (text.EqualsIgnoreCase("season"))
				{
					text = Game1.currentSeason;
					if (!masterScheduleRawData.ContainsKey(text))
					{
						text = "spring";
					}
				}
				try
				{
					if (masterScheduleRawData.TryGetValue(text, out var value))
					{
						return parseMasterScheduleImpl(text, value, visited);
					}
					Game1.log.Error($"Failed to load schedule '{scheduleKey}' for NPC '{base.Name}': GOTO references schedule '{text}' which doesn't exist. Falling back to 'spring'.");
				}
				catch (Exception exception)
				{
					Game1.log.Error($"Failed to load schedule '{scheduleKey}' for NPC '{base.Name}': GOTO references schedule '{text}' which couldn't be parsed. Falling back to 'spring'.", exception);
				}
				return parseMasterScheduleImpl("spring", getMasterScheduleEntry("spring"), visited);
			}
			if (array[0].Contains("NOT"))
			{
				string[] array2 = ArgUtility.SplitBySpace(array[0]);
				if (array2[1].ToLower() == "friendship")
				{
					int i = 2;
					bool flag = false;
					for (; i < array2.Length; i += 2)
					{
						string text2 = array2[i];
						if (int.TryParse(array2[i + 1], out var result))
						{
							foreach (Farmer allFarmer in Game1.getAllFarmers())
							{
								if (allFarmer.getFriendshipHeartLevelForNPC(text2) >= result)
								{
									flag = true;
									break;
								}
							}
						}
						if (flag)
						{
							break;
						}
					}
					if (flag)
					{
						return parseMasterScheduleImpl("spring", getMasterScheduleEntry("spring"), visited);
					}
					num++;
				}
			}
			else if (array[0].Contains("MAIL"))
			{
				string item = ArgUtility.SplitBySpace(array[0])[1];
				num = ((!Game1.MasterPlayer.mailReceived.Contains(item) && !NetWorldState.checkAnywhereForWorldStateID(item)) ? (num + 1) : (num + 2));
			}
			if (array[num].Contains("GOTO"))
			{
				string text3 = ArgUtility.SplitBySpaceAndGet(array[num], 1);
				string text4 = text3.ToLower();
				if (!(text4 == "season"))
				{
					if (text4 == "no_schedule")
					{
						followSchedule = false;
						return null;
					}
				}
				else
				{
					text3 = Game1.currentSeason;
				}
				return parseMasterScheduleImpl(text3, getMasterScheduleEntry(text3), visited);
			}
			Point point = (isMarried() ? new Point(10, 23) : new Point((int)defaultPosition.X / 64, (int)defaultPosition.Y / 64));
			string text5 = (isMarried() ? "BusStop" : defaultMap.Value);
			int val = 610;
			string text6 = DefaultMap;
			int num2 = (int)(defaultPosition.X / 64f);
			int num3 = (int)(defaultPosition.Y / 64f);
			bool flag2 = false;
			for (int j = num; j < array.Length; j++)
			{
				int num4 = 0;
				string[] array3 = ArgUtility.SplitBySpace(array[j]);
				bool flag3 = false;
				string text7 = array3[num4];
				if (text7.Length > 0 && array3[num4][0] == 'a')
				{
					flag3 = true;
					text7 = text7.Substring(1);
				}
				int num5 = Convert.ToInt32(text7);
				num4++;
				string locationName = array3[num4];
				string endBehavior = null;
				string endMessage = null;
				int result2 = 0;
				int tileY = 0;
				int result3 = 2;
				if (locationName == "bed")
				{
					if (isMarried())
					{
						locationName = "BusStop";
						result2 = 9;
						tileY = 23;
						result3 = 3;
					}
					else
					{
						string text8 = null;
						if (hasMasterScheduleEntry("default"))
						{
							text8 = getMasterScheduleEntry("default");
						}
						else if (hasMasterScheduleEntry("spring"))
						{
							text8 = getMasterScheduleEntry("spring");
						}
						if (text8 != null)
						{
							try
							{
								string[] array4 = ArgUtility.SplitBySpace(SplitScheduleCommands(text8)[^1]);
								locationName = array4[1];
								if (array4.Length > 3)
								{
									if (!int.TryParse(array4[2], out result2) || !int.TryParse(array4[3], out tileY))
									{
										text8 = null;
									}
								}
								else
								{
									text8 = null;
								}
							}
							catch (Exception)
							{
								text8 = null;
							}
						}
						if (text8 == null)
						{
							locationName = text6;
							result2 = num2;
							tileY = num3;
						}
					}
					num4++;
					Dictionary<string, string> dictionary2 = DataLoader.AnimationDescriptions(Game1.content);
					string text9 = name.Value.ToLower() + "_sleep";
					if (dictionary2.ContainsKey(text9))
					{
						endBehavior = text9;
					}
				}
				else
				{
					if (int.TryParse(locationName, out var _))
					{
						locationName = text5;
						num4--;
					}
					num4++;
					result2 = Convert.ToInt32(array3[num4]);
					num4++;
					tileY = Convert.ToInt32(array3[num4]);
					num4++;
					try
					{
						if (array3.Length > num4)
						{
							if (int.TryParse(array3[num4], out result3))
							{
								num4++;
							}
							else
							{
								result3 = 2;
							}
						}
					}
					catch (Exception)
					{
						result3 = 2;
					}
				}
				if (changeScheduleForLocationAccessibility(ref locationName, ref result2, ref tileY, ref result3))
				{
					string text10 = (getMasterScheduleRawData().ContainsKey("default") ? "default" : "spring");
					return parseMasterScheduleImpl(text10, getMasterScheduleEntry(text10), visited);
				}
				if (num4 < array3.Length)
				{
					if (array3[num4].Length > 0 && array3[num4][0] == '"')
					{
						endMessage = array[j].Substring(array[j].IndexOf('"'));
					}
					else
					{
						endBehavior = array3[num4];
						num4++;
						if (num4 < array3.Length && array3[num4].Length > 0 && array3[num4][0] == '"')
						{
							endMessage = array[j].Substring(array[j].IndexOf('"')).Replace("\"", "");
						}
					}
				}
				if (num5 == 0)
				{
					flag2 = true;
					text6 = locationName;
					num2 = result2;
					num3 = tileY;
					text5 = locationName;
					point.X = result2;
					point.Y = tileY;
					faceDirection(result3);
					previousEndPoint = new Point(result2, tileY);
					continue;
				}
				SchedulePathDescription schedulePathDescription = pathfindToNextScheduleLocation(scheduleKey, text5, point.X, point.Y, locationName, result2, tileY, result3, endBehavior, endMessage);
				if (flag3)
				{
					int num6 = 0;
					Point? point2 = null;
					foreach (Point item2 in schedulePathDescription.route)
					{
						if (!point2.HasValue)
						{
							point2 = item2;
							continue;
						}
						if (Math.Abs(point2.Value.X - item2.X) + Math.Abs(point2.Value.Y - item2.Y) == 1)
						{
							num6 += 64;
						}
						point2 = item2;
					}
					int num7 = num6 / 2;
					int num8 = Game1.realMilliSecondsPerGameTenMinutes / 1000 * 60;
					int num9 = (int)Math.Round((float)num7 / (float)num8) * 10;
					num5 = Math.Max(Utility.ConvertMinutesToTime(Utility.ConvertTimeToMinutes(num5) - num9), val);
				}
				schedulePathDescription.time = num5;
				dictionary.Add(num5, schedulePathDescription);
				point.X = result2;
				point.Y = tileY;
				text5 = locationName;
				val = num5;
			}
			if (Game1.IsMasterGame && flag2)
			{
				Game1.warpCharacter(this, text6, new Point(num2, num3));
			}
			return dictionary;
		}
		catch (Exception exception2)
		{
			Game1.log.Error($"NPC '{base.Name}' failed to parse master schedule '{scheduleKey}' with raw data '{rawData}'.", exception2);
			return new Dictionary<int, SchedulePathDescription>();
		}
	}

	/// <summary>Split a raw schedule script into its component commands.</summary>
	/// <param name="rawScript">The raw schedule script to split.</param>
	public static string[] SplitScheduleCommands(string rawScript)
	{
		return LegacyShims.SplitAndTrim(rawScript, '/', StringSplitOptions.RemoveEmptyEntries);
	}

	/// <summary>Try to load a schedule that applies today, or disable the schedule if none is found.</summary>
	/// <returns>Returns whether a schedule was successfully loaded.</returns>
	public bool TryLoadSchedule()
	{
		string currentSeason = Game1.currentSeason;
		int dayOfMonth = Game1.dayOfMonth;
		string text = Game1.shortDayNameFromDayOfSeason(dayOfMonth);
		int num = Math.Max(0, Utility.GetAllPlayerFriendshipLevel(this)) / 250;
		if (getMasterScheduleRawData() == null)
		{
			ClearSchedule();
			return false;
		}
		if (Game1.isGreenRain && Game1.year == 1 && TryLoadSchedule("GreenRain"))
		{
			return true;
		}
		if (!string.IsNullOrWhiteSpace(islandScheduleName.Value))
		{
			TryLoadSchedule(islandScheduleName.Value, Schedule);
			return true;
		}
		foreach (string activePassiveFestival in Game1.netWorldState.Value.ActivePassiveFestivals)
		{
			int dayOfPassiveFestival = Utility.GetDayOfPassiveFestival(activePassiveFestival);
			if (isMarried())
			{
				if (TryLoadSchedule("marriage_" + activePassiveFestival + "_" + dayOfPassiveFestival))
				{
					return true;
				}
				if (TryLoadSchedule("marriage_" + activePassiveFestival))
				{
					return true;
				}
			}
			else
			{
				if (TryLoadSchedule(activePassiveFestival + "_" + dayOfPassiveFestival))
				{
					return true;
				}
				if (TryLoadSchedule(activePassiveFestival))
				{
					return true;
				}
			}
		}
		if (isMarried())
		{
			if (TryLoadSchedule("marriage_" + currentSeason + "_" + dayOfMonth))
			{
				return true;
			}
			if (base.Name == "Penny")
			{
				switch (text)
				{
				case "Tue":
				case "Wed":
				case "Fri":
					goto IL_0205;
				}
			}
			if ((base.Name == "Maru" && (text == "Tue" || text == "Thu")) || (base.Name == "Harvey" && (text == "Tue" || text == "Thu")))
			{
				goto IL_0205;
			}
			goto IL_0214;
		}
		if (TryLoadSchedule(currentSeason + "_" + dayOfMonth))
		{
			return true;
		}
		for (int num2 = num; num2 > 0; num2--)
		{
			if (TryLoadSchedule(dayOfMonth + "_" + num2))
			{
				return true;
			}
		}
		if (TryLoadSchedule(dayOfMonth.ToString()))
		{
			return true;
		}
		if (base.Name == "Pam" && Game1.player.mailReceived.Contains("ccVault") && TryLoadSchedule("bus"))
		{
			return true;
		}
		if (base.currentLocation?.IsRainingHere() ?? false)
		{
			if (Game1.random.NextBool() && TryLoadSchedule("rain2"))
			{
				return true;
			}
			if (TryLoadSchedule("rain"))
			{
				return true;
			}
		}
		int num3;
		for (num3 = num; num3 > 0; num3--)
		{
			if (TryLoadSchedule(currentSeason + "_" + text + "_" + num3))
			{
				return true;
			}
			num3--;
		}
		if (TryLoadSchedule(currentSeason + "_" + text))
		{
			return true;
		}
		int num4;
		for (num4 = num; num4 > 0; num4--)
		{
			if (TryLoadSchedule(text + "_" + num4))
			{
				return true;
			}
			num4--;
		}
		if (TryLoadSchedule(text))
		{
			return true;
		}
		if (TryLoadSchedule(currentSeason))
		{
			return true;
		}
		if (TryLoadSchedule("spring_" + text))
		{
			return true;
		}
		if (TryLoadSchedule("spring"))
		{
			return true;
		}
		ClearSchedule();
		return false;
		IL_0205:
		if (TryLoadSchedule("marriageJob"))
		{
			return true;
		}
		goto IL_0214;
		IL_0214:
		if (!Game1.isRaining && TryLoadSchedule("marriage_" + text))
		{
			return true;
		}
		ClearSchedule();
		return false;
	}

	/// <summary>Try to load a schedule matching the the given key, or disable the schedule if it's missing or invalid.</summary>
	/// <param name="key">The key for the schedule to load.</param>
	/// <returns>Returns whether the schedule was successfully loaded.</returns>
	public bool TryLoadSchedule(string key)
	{
		try
		{
			if (hasMasterScheduleEntry(key))
			{
				TryLoadSchedule(key, parseMasterSchedule(key, getMasterScheduleEntry(key)));
				return true;
			}
		}
		catch (Exception exception)
		{
			Game1.log.Error($"Failed to load schedule key '{key}' for NPC '{base.Name}'.", exception);
		}
		ClearSchedule();
		return false;
	}

	/// <summary>Try to load a raw schedule script, or disable the schedule if it's invalid.</summary>
	/// <param name="key">The schedule's key in the data asset.</param>
	/// <param name="rawSchedule">The schedule script to load.</param>
	public bool TryLoadSchedule(string key, string rawSchedule)
	{
		Dictionary<int, SchedulePathDescription> schedule;
		try
		{
			schedule = parseMasterSchedule(key, rawSchedule);
		}
		catch (Exception exception)
		{
			Game1.log.Error($"Failed to load schedule key '{key}' from raw string for NPC '{base.Name}'.", exception);
			ClearSchedule();
			return false;
		}
		return TryLoadSchedule(key, schedule);
	}

	/// <summary>Try to load raw schedule data, or disable the schedule if it's invalid.</summary>
	/// <param name="key">The schedule's key in the data asset.</param>
	/// <param name="schedule">The schedule data to load.</param>
	public bool TryLoadSchedule(string key, Dictionary<int, SchedulePathDescription> schedule)
	{
		if (schedule == null)
		{
			ClearSchedule();
			return false;
		}
		Schedule = schedule;
		if (Game1.IsMasterGame)
		{
			dayScheduleName.Value = key;
		}
		followSchedule = true;
		return true;
	}

	/// <summary>Disable the schedule for today.</summary>
	public void ClearSchedule()
	{
		Schedule = null;
		if (Game1.IsMasterGame)
		{
			dayScheduleName.Value = null;
		}
		followSchedule = false;
	}

	public virtual void handleMasterScheduleFileLoadError(Exception e)
	{
		Game1.log.Error("NPC '" + base.Name + "' failed loading schedule file.", e);
	}

	public virtual void InvalidateMasterSchedule()
	{
		_hasLoadedMasterScheduleData = false;
	}

	public Dictionary<string, string> getMasterScheduleRawData()
	{
		if (!_hasLoadedMasterScheduleData)
		{
			_hasLoadedMasterScheduleData = true;
			string text = "Characters\\schedules\\" + base.Name;
			if (base.Name == "Leo" && DefaultMap != "IslandHut")
			{
				text += "Mainland";
			}
			try
			{
				if (Game1.content.DoesAssetExist<Dictionary<string, string>>(text))
				{
					_masterScheduleData = Game1.content.Load<Dictionary<string, string>>(text);
					_masterScheduleData = new Dictionary<string, string>(_masterScheduleData, StringComparer.OrdinalIgnoreCase);
				}
			}
			catch (Exception e)
			{
				handleMasterScheduleFileLoadError(e);
			}
		}
		return _masterScheduleData;
	}

	public string getMasterScheduleEntry(string schedule_key)
	{
		if (getMasterScheduleRawData() == null)
		{
			throw new KeyNotFoundException("The schedule file for NPC '" + base.Name + "' could not be loaded...");
		}
		if (_masterScheduleData.TryGetValue(schedule_key, out var value))
		{
			return value;
		}
		throw new KeyNotFoundException($"The schedule file for NPC '{base.Name}' has no schedule named '{schedule_key}'.");
	}

	public bool hasMasterScheduleEntry(string key)
	{
		if (getMasterScheduleRawData() == null)
		{
			return false;
		}
		return getMasterScheduleRawData().ContainsKey(key);
	}

	public virtual bool isRoommate()
	{
		if (!IsVillager)
		{
			return false;
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.spouse != null && allFarmer.spouse == base.Name && !allFarmer.isEngaged() && allFarmer.isRoommate(base.Name))
			{
				return true;
			}
		}
		return false;
	}

	public bool isMarried()
	{
		if (!IsVillager)
		{
			return false;
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.spouse != null && allFarmer.spouse == base.Name && !allFarmer.isEngaged())
			{
				return true;
			}
		}
		return false;
	}

	public bool isMarriedOrEngaged()
	{
		if (!IsVillager)
		{
			return false;
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.spouse != null && allFarmer.spouse == base.Name)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Update the NPC state when setting up the new day, before the game saves overnight.</summary>
	/// <param name="dayOfMonth">The current day of month.</param>
	/// <remarks>See also <see cref="M:StardewValley.NPC.OnDayStarted" />, which happens after saving when the day has started.</remarks>
	public virtual void dayUpdate(int dayOfMonth)
	{
		bool flag = IsVillager;
		isMovingOnPathFindPath.Value = false;
		queuedSchedulePaths.Clear();
		lastAttemptedSchedule = -1;
		drawOffset = Vector2.Zero;
		appliedRouteAnimationOffset = Vector2.Zero;
		shouldWearIslandAttire.Value = false;
		if (layingDown)
		{
			layingDown = false;
			HideShadow = false;
		}
		if (isWearingIslandAttire)
		{
			wearNormalClothes();
		}
		if (base.currentLocation != null && defaultMap.Value != null)
		{
			try
			{
				Game1.warpCharacter(this, defaultMap.Value, defaultPosition.Value / 64f);
			}
			catch (Exception exception)
			{
				Game1.log.Error($"NPC '{base.Name}' failed to warp home to '{defaultMap}' overnight.", exception);
			}
		}
		if (flag)
		{
			string text = base.Name;
			if (!(text == "Willy"))
			{
				if (text == "Elliott" && Game1.IsMasterGame && Game1.netWorldState.Value.hasWorldStateID("elliottGone"))
				{
					daysUntilNotInvisible = 7;
					Game1.netWorldState.Value.removeWorldStateID("elliottGone");
					Game1.worldStateIDs.Remove("elliottGone");
				}
			}
			else
			{
				IsInvisible = false;
			}
		}
		UpdateInvisibilityOnNewDay();
		resetForNewDay(dayOfMonth);
		ChooseAppearance();
		if (flag)
		{
			updateConstructionAnimation();
		}
		clearTextAboveHead();
	}

	/// <summary>Handle the new day starting after the player saves, loads, or connects.</summary>
	/// <remarks>See also <see cref="M:StardewValley.NPC.dayUpdate(System.Int32)" />, which happens while setting up the day before saving.</remarks>
	public void OnDayStarted()
	{
		if (Game1.IsMasterGame && isMarried() && !getSpouse().divorceTonight.Value && !IsInvisible)
		{
			marriageDuties();
		}
	}

	protected void UpdateInvisibilityOnNewDay()
	{
		if (Game1.IsMasterGame && (IsInvisible || daysUntilNotInvisible > 0))
		{
			daysUntilNotInvisible--;
			IsInvisible = daysUntilNotInvisible > 0;
			if (!IsInvisible)
			{
				daysUntilNotInvisible = 0;
			}
		}
	}

	public virtual void resetForNewDay(int dayOfMonth)
	{
		sleptInBed.Value = true;
		if (isMarried() && !isRoommate())
		{
			FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(getSpouse());
			if (homeOfFarmer != null && homeOfFarmer.GetSpouseBed() == null)
			{
				sleptInBed.Value = false;
			}
		}
		if (doingEndOfRouteAnimation.Value)
		{
			routeEndAnimationFinished(null);
		}
		Halt();
		wasKissedYesterday = hasBeenKissedToday.Value;
		hasBeenKissedToday.Value = false;
		currentMarriageDialogue.Clear();
		marriageDefaultDialogue.Value = null;
		shouldSayMarriageDialogue.Value = false;
		isSleeping.Value = false;
		drawOffset = Vector2.Zero;
		faceTowardFarmer = false;
		faceTowardFarmerTimer = 0;
		drawOffset = Vector2.Zero;
		hasSaidAfternoonDialogue.Value = false;
		isPlayingSleepingAnimation = false;
		ignoreScheduleToday = false;
		Halt();
		controller = null;
		temporaryController = null;
		directionsToNewLocation = null;
		faceDirection(DefaultFacingDirection);
		Sprite.oldFrame = Sprite.CurrentFrame;
		previousEndPoint = new Point((int)defaultPosition.X / 64, (int)defaultPosition.Y / 64);
		isWalkingInSquare = false;
		returningToEndPoint = false;
		lastCrossroad = Microsoft.Xna.Framework.Rectangle.Empty;
		_startedEndOfRouteBehavior = null;
		_finishingEndOfRouteBehavior = null;
		loadedEndOfRouteBehavior = null;
		_beforeEndOfRouteAnimationFrame = Sprite.CurrentFrame;
		if (IsVillager)
		{
			if (base.Name == "Willy" && Game1.stats.DaysPlayed < 2)
			{
				IsInvisible = true;
				daysUntilNotInvisible = 1;
			}
			TryLoadSchedule();
			performSpecialScheduleChanges();
		}
		endOfRouteMessage.Value = null;
	}

	public void returnHomeFromFarmPosition(Farm farm)
	{
		Farmer spouse = getSpouse();
		if (spouse != null)
		{
			FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(spouse);
			Point porchStandingSpot = homeOfFarmer.getPorchStandingSpot();
			if (base.TilePoint == porchStandingSpot)
			{
				drawOffset = Vector2.Zero;
				string nameOrUniqueName = getHome().NameOrUniqueName;
				base.willDestroyObjectsUnderfoot = true;
				Point warpPointTo = farm.getWarpPointTo(nameOrUniqueName, this);
				controller = new PathFindController(this, farm, warpPointTo, 0)
				{
					NPCSchedule = true
				};
			}
			else if (!shouldPlaySpousePatioAnimation.Value || !farm.farmers.Any())
			{
				drawOffset = Vector2.Zero;
				Halt();
				controller = null;
				temporaryController = null;
				ignoreScheduleToday = true;
				Game1.warpCharacter(this, homeOfFarmer, Utility.PointToVector2(homeOfFarmer.getKitchenStandingSpot()));
			}
		}
	}

	public virtual Vector2 GetSpousePatioPosition()
	{
		return Utility.PointToVector2(Game1.getFarm().spousePatioSpot);
	}

	public void setUpForOutdoorPatioActivity()
	{
		Vector2 spousePatioPosition = GetSpousePatioPosition();
		if (!checkTileOccupancyForSpouse(Game1.getFarm(), spousePatioPosition))
		{
			Game1.warpCharacter(this, "Farm", spousePatioPosition);
			popOffAnyNonEssentialItems();
			currentMarriageDialogue.Clear();
			addMarriageDialogue("MarriageDialogue", "patio_" + base.Name, false);
			setTilePosition((int)spousePatioPosition.X, (int)spousePatioPosition.Y);
			shouldPlaySpousePatioAnimation.Value = true;
		}
	}

	private void doPlaySpousePatioAnimation()
	{
		CharacterSpousePatioData characterSpousePatioData = GetData()?.SpousePatio;
		if (characterSpousePatioData == null)
		{
			return;
		}
		List<int[]> spriteAnimationFrames = characterSpousePatioData.SpriteAnimationFrames;
		if (spriteAnimationFrames == null || spriteAnimationFrames.Count <= 0)
		{
			return;
		}
		drawOffset = Utility.PointToVector2(characterSpousePatioData.SpriteAnimationPixelOffset);
		Sprite.ClearAnimation();
		for (int i = 0; i < spriteAnimationFrames.Count; i++)
		{
			int[] array = spriteAnimationFrames[i];
			if (array != null && array.Length != 0)
			{
				int frame = array[0];
				int milliseconds = (ArgUtility.HasIndex(array, 1) ? array[1] : 100);
				Sprite.AddFrame(new FarmerSprite.AnimationFrame(frame, milliseconds, 0, secondaryArm: false, flip: false));
			}
		}
	}

	/// <summary>Whether this character has dark skin for the purposes of child genetics.</summary>
	public virtual bool hasDarkSkin()
	{
		if (IsVillager)
		{
			return GetData()?.IsDarkSkinned ?? false;
		}
		return false;
	}

	/// <summary>Whether the player will need to adopt children with this spouse, instead of either the player or NPC giving birth.</summary>
	public bool isAdoptionSpouse()
	{
		Farmer spouse = getSpouse();
		if (spouse == null)
		{
			return false;
		}
		string text = GetData()?.SpouseAdopts;
		if (text != null)
		{
			return GameStateQuery.CheckConditions(text, base.currentLocation, spouse);
		}
		return Gender == spouse.Gender;
	}

	public bool canGetPregnant()
	{
		if (this is Horse || base.Name.Equals("Krobus") || isRoommate() || IsInvisible)
		{
			return false;
		}
		Farmer spouse = getSpouse();
		if (spouse == null || spouse.divorceTonight.Value)
		{
			return false;
		}
		int friendshipHeartLevelForNPC = spouse.getFriendshipHeartLevelForNPC(base.Name);
		Friendship spouseFriendship = spouse.GetSpouseFriendship();
		List<Child> children = spouse.getChildren();
		defaultMap.Value = spouse.homeLocation.Value;
		FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(spouse);
		if (homeOfFarmer.cribStyle.Value <= 0)
		{
			return false;
		}
		if (homeOfFarmer.upgradeLevel >= 2 && spouseFriendship.DaysUntilBirthing < 0 && friendshipHeartLevelForNPC >= 10 && spouse.GetDaysMarried() >= 7)
		{
			if (children.Count != 0)
			{
				if (children.Count < 2)
				{
					return children[0].Age > 2;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public void marriageDuties()
	{
		Farmer spouse = getSpouse();
		if (spouse == null)
		{
			return;
		}
		shouldSayMarriageDialogue.Value = true;
		DefaultMap = spouse.homeLocation.Value;
		FarmHouse farmHouse = Game1.RequireLocation<FarmHouse>(spouse.homeLocation.Value);
		Random random = Utility.CreateDaySaveRandom(spouse.UniqueMultiplayerID);
		int friendshipHeartLevelForNPC = spouse.getFriendshipHeartLevelForNPC(base.Name);
		if (Game1.IsMasterGame && (base.currentLocation == null || !base.currentLocation.Equals(farmHouse)))
		{
			Game1.warpCharacter(this, spouse.homeLocation.Value, farmHouse.getSpouseBedSpot(base.Name));
		}
		if (Game1.isRaining)
		{
			marriageDefaultDialogue.Value = new MarriageDialogueReference("MarriageDialogue", "Rainy_Day_" + random.Next(5), false);
		}
		else
		{
			marriageDefaultDialogue.Value = new MarriageDialogueReference("MarriageDialogue", "Indoor_Day_" + random.Next(5), false);
		}
		currentMarriageDialogue.Add(new MarriageDialogueReference(marriageDefaultDialogue.Value.DialogueFile, marriageDefaultDialogue.Value.DialogueKey, marriageDefaultDialogue.Value.IsGendered, marriageDefaultDialogue.Value.Substitutions));
		if (spouse.GetSpouseFriendship().DaysUntilBirthing == 0)
		{
			setTilePosition(farmHouse.getKitchenStandingSpot());
			currentMarriageDialogue.Clear();
			return;
		}
		if (daysAfterLastBirth >= 0)
		{
			daysAfterLastBirth--;
			switch (getSpouse().getChildrenCount())
			{
			case 1:
				setTilePosition(farmHouse.getKitchenStandingSpot());
				if (!spouseObstacleCheck(new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4406", false), farmHouse))
				{
					currentMarriageDialogue.Clear();
					addMarriageDialogue("MarriageDialogue", "OneKid_" + random.Next(4), false);
				}
				return;
			case 2:
				setTilePosition(farmHouse.getKitchenStandingSpot());
				if (!spouseObstacleCheck(new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4406", false), farmHouse))
				{
					currentMarriageDialogue.Clear();
					addMarriageDialogue("MarriageDialogue", "TwoKids_" + random.Next(4), false);
				}
				return;
			}
		}
		setTilePosition(farmHouse.getKitchenStandingSpot());
		if (!sleptInBed.Value)
		{
			currentMarriageDialogue.Clear();
			addMarriageDialogue("MarriageDialogue", "NoBed_" + random.Next(4), false);
			return;
		}
		if (tryToGetMarriageSpecificDialogue(Game1.currentSeason + "_" + Game1.dayOfMonth) != null)
		{
			if (spouse != null)
			{
				currentMarriageDialogue.Clear();
				addMarriageDialogue("MarriageDialogue", Game1.currentSeason + "_" + Game1.dayOfMonth, false);
			}
			return;
		}
		if (Schedule != null)
		{
			if (ScheduleKey == "marriage_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth))
			{
				currentMarriageDialogue.Clear();
				addMarriageDialogue("MarriageDialogue", "funLeave_" + base.Name, false);
			}
			else if (ScheduleKey == "marriageJob")
			{
				currentMarriageDialogue.Clear();
				addMarriageDialogue("MarriageDialogue", "jobLeave_" + base.Name, false);
			}
			return;
		}
		if (!Game1.isRaining && !Game1.IsWinter && Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Sat") && spouse == Game1.MasterPlayer && !base.Name.Equals("Krobus"))
		{
			setUpForOutdoorPatioActivity();
			return;
		}
		int num = 12;
		if (Game1.Date.TotalDays - spouse.GetSpouseFriendship().LastGiftDate?.TotalDays <= 1)
		{
			num--;
		}
		if (wasKissedYesterday)
		{
			num--;
		}
		if (spouse.GetDaysMarried() > 7 && random.NextDouble() < (double)(1f - (float)Math.Max(1, friendshipHeartLevelForNPC) / (float)num))
		{
			Furniture randomFurniture = farmHouse.getRandomFurniture(random);
			if (randomFurniture != null && randomFurniture.isGroundFurniture() && randomFurniture.furniture_type.Value != 15 && randomFurniture.furniture_type.Value != 12)
			{
				Point tilePosition = new Point((int)randomFurniture.tileLocation.X - 1, (int)randomFurniture.tileLocation.Y);
				if (farmHouse.CanItemBePlacedHere(new Vector2(tilePosition.X, tilePosition.Y)))
				{
					setTilePosition(tilePosition);
					faceDirection(1);
					switch (random.Next(10))
					{
					case 0:
						currentMarriageDialogue.Clear();
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4420", false);
						break;
					case 1:
						currentMarriageDialogue.Clear();
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4421", false);
						break;
					case 2:
						currentMarriageDialogue.Clear();
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4422", true);
						break;
					case 3:
						currentMarriageDialogue.Clear();
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4423", false);
						break;
					case 4:
						currentMarriageDialogue.Clear();
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4424", false);
						break;
					case 5:
						currentMarriageDialogue.Clear();
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4425", false);
						break;
					case 6:
						currentMarriageDialogue.Clear();
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4426", false);
						break;
					case 7:
						if (Gender == Gender.Female)
						{
							currentMarriageDialogue.Clear();
							addMarriageDialogue("Strings\\StringsFromCSFiles", random.Choose("NPC.cs.4427", "NPC.cs.4429"), false);
						}
						else
						{
							currentMarriageDialogue.Clear();
							addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4431", false);
						}
						break;
					case 8:
						currentMarriageDialogue.Clear();
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4432", false);
						break;
					case 9:
						currentMarriageDialogue.Clear();
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4433", false);
						break;
					}
					return;
				}
			}
			spouseObstacleCheck(new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4406", false), farmHouse, force: true);
			return;
		}
		Friendship spouseFriendship = spouse.GetSpouseFriendship();
		if (spouseFriendship.DaysUntilBirthing != -1 && spouseFriendship.DaysUntilBirthing <= 7 && random.NextBool())
		{
			if (isAdoptionSpouse())
			{
				setTilePosition(farmHouse.getKitchenStandingSpot());
				if (!spouseObstacleCheck(new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4439", false), farmHouse))
				{
					if (random.NextBool())
					{
						currentMarriageDialogue.Clear();
					}
					if (random.NextBool())
					{
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4440", false, getSpouse().displayName);
					}
					else
					{
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4441", false, "%endearment");
					}
				}
				return;
			}
			if (Gender == Gender.Female)
			{
				setTilePosition(farmHouse.getKitchenStandingSpot());
				if (!spouseObstacleCheck(random.NextBool() ? new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4442", false) : new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4443", false), farmHouse))
				{
					if (random.NextBool())
					{
						currentMarriageDialogue.Clear();
					}
					currentMarriageDialogue.Add(random.NextBool() ? new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4444", false, getSpouse().displayName) : new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4445", false, "%endearment"));
				}
				return;
			}
			setTilePosition(farmHouse.getKitchenStandingSpot());
			if (!spouseObstacleCheck(new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4446", true), farmHouse))
			{
				if (random.NextBool())
				{
					currentMarriageDialogue.Clear();
				}
				currentMarriageDialogue.Add(random.NextBool() ? new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4447", true, getSpouse().displayName) : new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4448", false, "%endearment"));
			}
			return;
		}
		if (random.NextDouble() < 0.07)
		{
			switch (getSpouse().getChildrenCount())
			{
			case 1:
				setTilePosition(farmHouse.getKitchenStandingSpot());
				if (!spouseObstacleCheck(new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4449", true), farmHouse))
				{
					currentMarriageDialogue.Clear();
					addMarriageDialogue("MarriageDialogue", "OneKid_" + random.Next(4), false);
				}
				return;
			case 2:
				setTilePosition(farmHouse.getKitchenStandingSpot());
				if (!spouseObstacleCheck(new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4452", true), farmHouse))
				{
					currentMarriageDialogue.Clear();
					addMarriageDialogue("MarriageDialogue", "TwoKids_" + random.Next(4), false);
				}
				return;
			}
		}
		Farm farm = Game1.getFarm();
		if (currentMarriageDialogue.Count > 0 && currentMarriageDialogue[0].IsItemGrabDialogue(this))
		{
			setTilePosition(farmHouse.getKitchenStandingSpot());
			spouseObstacleCheck(new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4455", true), farmHouse);
		}
		else if (!Game1.isRaining && random.NextDouble() < 0.4 && !checkTileOccupancyForSpouse(farm, Utility.PointToVector2(farmHouse.getPorchStandingSpot())) && !base.Name.Equals("Krobus"))
		{
			bool flag = false;
			if (!hasSomeoneFedThePet)
			{
				foreach (Building building in farm.buildings)
				{
					if (building is PetBowl petBowl && !petBowl.watered.Value)
					{
						flag = true;
						petBowl.watered.Value = true;
						hasSomeoneFedThePet = true;
					}
				}
			}
			if (random.NextDouble() < 0.6 && Game1.season != Season.Winter && !hasSomeoneWateredCrops)
			{
				Vector2 vector = Vector2.Zero;
				int i = 0;
				bool flag2 = false;
				for (; i < Math.Min(50, farm.terrainFeatures.Length); i++)
				{
					if (!vector.Equals(Vector2.Zero))
					{
						break;
					}
					if (Utility.TryGetRandom(farm.terrainFeatures, out var key, out var value) && value is HoeDirt hoeDirt && hoeDirt.needsWatering())
					{
						if (!hoeDirt.isWatered())
						{
							vector = key;
						}
						else
						{
							flag2 = true;
						}
					}
				}
				if (!vector.Equals(Vector2.Zero))
				{
					foreach (Vector2 vector4 in new Microsoft.Xna.Framework.Rectangle((int)vector.X - 30, (int)vector.Y - 30, 60, 60).GetVectors())
					{
						if (farm.isTileOnMap(vector4) && farm.terrainFeatures.TryGetValue(vector4, out var value2) && value2 is HoeDirt hoeDirt2 && Game1.IsMasterGame && hoeDirt2.needsWatering())
						{
							hoeDirt2.state.Value = 1;
						}
					}
					faceDirection(2);
					currentMarriageDialogue.Clear();
					addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4462", true);
					if (flag)
					{
						if (Utility.getAllPets().Count > 1 && Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.en)
						{
							addMarriageDialogue("Strings\\StringsFromCSFiles", "MultiplePetBowls_watered", false, Game1.player.getPetDisplayName());
						}
						else
						{
							addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4463", false, Game1.player.getPetDisplayName());
						}
					}
					addMarriageDialogue("MarriageDialogue", "Outdoor_" + random.Next(5), false);
					hasSomeoneWateredCrops = true;
				}
				else
				{
					faceDirection(2);
					if (flag2)
					{
						currentMarriageDialogue.Clear();
						if (Game1.gameMode == 6)
						{
							if (random.NextBool())
							{
								addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4465", false, "%endearment");
							}
							else
							{
								addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4466", false, "%endearment");
								addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4462", true);
								if (flag)
								{
									if (Utility.getAllPets().Count > 1 && Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.en)
									{
										addMarriageDialogue("Strings\\StringsFromCSFiles", "MultiplePetBowls_watered", false, Game1.player.getPetDisplayName());
									}
									else
									{
										addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4463", false, Game1.player.getPetDisplayName());
									}
								}
							}
						}
						else
						{
							currentMarriageDialogue.Clear();
							addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4470", true);
						}
					}
					else
					{
						currentMarriageDialogue.Clear();
						addMarriageDialogue("MarriageDialogue", "Outdoor_" + random.Next(5), false);
					}
				}
			}
			else if (random.NextDouble() < 0.6 && !hasSomeoneFedTheAnimals)
			{
				bool flag3 = false;
				foreach (Building building2 in farm.buildings)
				{
					if (building2.GetIndoors() is AnimalHouse animalHouse && building2.daysOfConstructionLeft.Value <= 0 && Game1.IsMasterGame)
					{
						animalHouse.feedAllAnimals();
						flag3 = true;
					}
				}
				faceDirection(2);
				if (flag3)
				{
					hasSomeoneFedTheAnimals = true;
					currentMarriageDialogue.Clear();
					addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4474", true);
					if (flag)
					{
						if (Utility.getAllPets().Count > 1 && Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.en)
						{
							addMarriageDialogue("Strings\\StringsFromCSFiles", "MultiplePetBowls_watered", false, Game1.player.getPetDisplayName());
						}
						else
						{
							addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4463", false, Game1.player.getPetDisplayName());
						}
					}
					addMarriageDialogue("MarriageDialogue", "Outdoor_" + random.Next(5), false);
				}
				else
				{
					currentMarriageDialogue.Clear();
					addMarriageDialogue("MarriageDialogue", "Outdoor_" + random.Next(5), false);
				}
				if (Game1.IsMasterGame)
				{
					foreach (Building building3 in farm.buildings)
					{
						if (building3 is PetBowl petBowl2 && !petBowl2.watered.Value)
						{
							flag = true;
							petBowl2.watered.Value = true;
							hasSomeoneFedThePet = true;
						}
					}
				}
			}
			else if (!hasSomeoneRepairedTheFences)
			{
				int j = 0;
				faceDirection(2);
				Vector2 vector2 = Vector2.Zero;
				for (; j < Math.Min(50, farm.objects.Length); j++)
				{
					if (!vector2.Equals(Vector2.Zero))
					{
						break;
					}
					if (Utility.TryGetRandom(farm.objects, out var key2, out var value3) && value3 is Fence)
					{
						vector2 = key2;
					}
				}
				if (!vector2.Equals(Vector2.Zero))
				{
					foreach (Vector2 vector5 in new Microsoft.Xna.Framework.Rectangle((int)vector2.X - 10, (int)vector2.Y - 10, 20, 20).GetVectors())
					{
						if (farm.isTileOnMap(vector5) && farm.objects.TryGetValue(vector5, out var value4) && value4 is Fence fence && Game1.IsMasterGame)
						{
							fence.repair();
						}
					}
					currentMarriageDialogue.Clear();
					addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4481", true);
					if (flag)
					{
						if (Utility.getAllPets().Count > 1 && Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.en)
						{
							addMarriageDialogue("Strings\\StringsFromCSFiles", "MultiplePetBowls_watered", false, Game1.player.getPetDisplayName());
						}
						else
						{
							addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4463", false, Game1.player.getPetDisplayName());
						}
					}
					addMarriageDialogue("MarriageDialogue", "Outdoor_" + random.Next(5), false);
					hasSomeoneRepairedTheFences = true;
				}
				else
				{
					currentMarriageDialogue.Clear();
					addMarriageDialogue("MarriageDialogue", "Outdoor_" + random.Next(5), false);
				}
			}
			Game1.warpCharacter(this, "Farm", farmHouse.getPorchStandingSpot());
			popOffAnyNonEssentialItems();
			faceDirection(2);
		}
		else if (base.Name.Equals("Krobus") && Game1.isRaining && random.NextDouble() < 0.4 && !checkTileOccupancyForSpouse(farm, Utility.PointToVector2(farmHouse.getPorchStandingSpot())))
		{
			addMarriageDialogue("MarriageDialogue", "Outdoor_" + random.Next(5), false);
			Game1.warpCharacter(this, "Farm", farmHouse.getPorchStandingSpot());
			popOffAnyNonEssentialItems();
			faceDirection(2);
		}
		else if (spouse.GetDaysMarried() >= 1 && random.NextDouble() < 0.045)
		{
			if (random.NextDouble() < 0.75)
			{
				Point randomOpenPointInHouse = farmHouse.getRandomOpenPointInHouse(random, 1);
				Furniture furniture;
				try
				{
					furniture = ItemRegistry.Create<Furniture>(Utility.getRandomSingleTileFurniture(random)).SetPlacement(randomOpenPointInHouse);
				}
				catch
				{
					furniture = null;
				}
				if (furniture != null && randomOpenPointInHouse.X > 0 && farmHouse.CanItemBePlacedHere(new Vector2(randomOpenPointInHouse.X - 1, randomOpenPointInHouse.Y)))
				{
					farmHouse.furniture.Add(furniture);
					setTilePosition(randomOpenPointInHouse.X - 1, randomOpenPointInHouse.Y);
					faceDirection(1);
					addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4486", false, "%endearmentlower");
					if (Game1.random.NextBool())
					{
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4488", true);
					}
					else
					{
						addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4489", false);
					}
				}
				else
				{
					setTilePosition(farmHouse.getKitchenStandingSpot());
					spouseObstacleCheck(new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4490", false), farmHouse);
				}
				return;
			}
			Point randomOpenPointInHouse2 = farmHouse.getRandomOpenPointInHouse(random);
			if (randomOpenPointInHouse2.X <= 0)
			{
				return;
			}
			setTilePosition(randomOpenPointInHouse2.X, randomOpenPointInHouse2.Y);
			faceDirection(0);
			if (random.NextBool())
			{
				string wallpaperID = farmHouse.GetWallpaperID(randomOpenPointInHouse2.X, randomOpenPointInHouse2.Y);
				if (wallpaperID != null)
				{
					string which = random.ChooseFrom(GetData()?.SpouseWallpapers) ?? random.Next(112).ToString();
					farmHouse.SetWallpaper(which, wallpaperID);
					addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4496", false);
				}
			}
			else
			{
				string floorRoomIdAt = farmHouse.getFloorRoomIdAt(randomOpenPointInHouse2);
				if (floorRoomIdAt != null)
				{
					string which2 = random.ChooseFrom(GetData()?.SpouseFloors) ?? random.Next(40).ToString();
					farmHouse.SetFloor(which2, floorRoomIdAt);
					addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4497", false);
				}
			}
		}
		else if (Game1.isRaining && random.NextDouble() < 0.08 && friendshipHeartLevelForNPC < 11 && spouse.GetDaysMarried() > 7 && base.Name != "Krobus")
		{
			foreach (Furniture item in farmHouse.furniture)
			{
				if (item.furniture_type.Value == 13 && farmHouse.CanItemBePlacedHere(new Vector2((int)item.tileLocation.X, (int)item.tileLocation.Y + 1)))
				{
					setTilePosition((int)item.tileLocation.X, (int)item.tileLocation.Y + 1);
					faceDirection(0);
					currentMarriageDialogue.Clear();
					addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4498", true);
					return;
				}
			}
			spouseObstacleCheck(new MarriageDialogueReference("Strings\\StringsFromCSFiles", "NPC.cs.4499", false), farmHouse, force: true);
		}
		else if (random.NextDouble() < 0.45)
		{
			Vector2 vector3 = Utility.PointToVector2(farmHouse.GetSpouseRoomSpot());
			setTilePosition((int)vector3.X, (int)vector3.Y);
			faceDirection(0);
			setSpouseRoomMarriageDialogue();
			if (name.Value == "Sebastian" && Game1.netWorldState.Value.hasWorldStateID("sebastianFrog"))
			{
				Point spouseRoomCorner = farmHouse.GetSpouseRoomCorner();
				spouseRoomCorner.X += 2;
				spouseRoomCorner.Y += 5;
				setTilePosition(spouseRoomCorner);
				faceDirection(2);
			}
		}
		else
		{
			setTilePosition(farmHouse.getKitchenStandingSpot());
			faceDirection(0);
			if (random.NextDouble() < 0.2)
			{
				setRandomAfternoonMarriageDialogue(Game1.timeOfDay, farmHouse);
			}
		}
	}

	public virtual void popOffAnyNonEssentialItems()
	{
		if (Game1.IsMasterGame && base.currentLocation != null)
		{
			Point tilePoint = base.TilePoint;
			Object objectAtTile = base.currentLocation.getObjectAtTile(tilePoint.X, tilePoint.Y);
			if (objectAtTile != null && (objectAtTile.QualifiedItemId == "(O)93" || objectAtTile is Torch))
			{
				Vector2 tileLocation = objectAtTile.TileLocation;
				objectAtTile.performRemoveAction();
				base.currentLocation.objects.Remove(tileLocation);
				objectAtTile.dropItem(base.currentLocation, tileLocation * 64f, tileLocation * 64f);
			}
		}
	}

	public static bool checkTileOccupancyForSpouse(GameLocation location, Vector2 point, string characterToIgnore = "")
	{
		return location?.IsTileOccupiedBy(point, ~(CollisionMask.Characters | CollisionMask.Farmers), CollisionMask.All) ?? true;
	}

	public void addMarriageDialogue(string dialogue_file, string dialogue_key, bool gendered = false, params string[] substitutions)
	{
		shouldSayMarriageDialogue.Value = true;
		currentMarriageDialogue.Add(new MarriageDialogueReference(dialogue_file, dialogue_key, gendered, substitutions));
	}

	public void clearTextAboveHead()
	{
		textAboveHead = null;
		textAboveHeadPreTimer = -1;
		textAboveHeadTimer = -1;
	}

	/// <summary>Get whether this is a villager NPC, regardless of whether they're present in <c>Data/Characters</c>.</summary>
	[Obsolete("Use IsVillager instead.")]
	public bool isVillager()
	{
		return IsVillager;
	}

	public override bool shouldCollideWithBuildingLayer(GameLocation location)
	{
		if (isMarried() && (Schedule == null || location is FarmHouse))
		{
			return true;
		}
		return base.shouldCollideWithBuildingLayer(location);
	}

	public virtual void arriveAtFarmHouse(FarmHouse farmHouse)
	{
		if (Game1.newDay || !isMarried() || Game1.timeOfDay <= 630 || !(base.TilePoint != farmHouse.getSpouseBedSpot(name.Value)))
		{
			return;
		}
		setTilePosition(farmHouse.getEntryLocation());
		ignoreScheduleToday = true;
		temporaryController = null;
		controller = null;
		if (Game1.timeOfDay >= 2130)
		{
			Point spouseBedSpot = farmHouse.getSpouseBedSpot(name.Value);
			bool flag = farmHouse.GetSpouseBed() != null;
			PathFindController.endBehavior endBehaviorFunction = null;
			if (flag)
			{
				endBehaviorFunction = FarmHouse.spouseSleepEndFunction;
			}
			controller = new PathFindController(this, farmHouse, spouseBedSpot, 0, endBehaviorFunction);
			if (controller.pathToEndPoint != null && flag)
			{
				foreach (Furniture item in farmHouse.furniture)
				{
					if (item is BedFurniture bedFurniture && item.GetBoundingBox().Intersects(new Microsoft.Xna.Framework.Rectangle(spouseBedSpot.X * 64, spouseBedSpot.Y * 64, 64, 64)))
					{
						bedFurniture.ReserveForNPC();
						break;
					}
				}
			}
		}
		else
		{
			controller = new PathFindController(this, farmHouse, farmHouse.getKitchenStandingSpot(), 0);
		}
		if (controller.pathToEndPoint == null)
		{
			base.willDestroyObjectsUnderfoot = true;
			controller = new PathFindController(this, farmHouse, farmHouse.getKitchenStandingSpot(), 0);
			setNewDialogue(TryGetDialogue("SpouseFarmhouseClutter") ?? new Dialogue(this, "Strings\\StringsFromCSFiles:NPC.cs.4500", isGendered: true));
		}
		else if (Game1.timeOfDay > 1300)
		{
			if (ScheduleKey == "marriage_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth))
			{
				setNewDialogue("MarriageDialogue", "funReturn_", clearOnMovement: true);
			}
			else if (ScheduleKey == "marriageJob")
			{
				setNewDialogue("MarriageDialogue", "jobReturn_");
			}
			else if (Game1.timeOfDay < 1800)
			{
				setRandomAfternoonMarriageDialogue(Game1.timeOfDay, base.currentLocation, countAsDailyAfternoon: true);
			}
		}
		if (Game1.currentLocation == farmHouse)
		{
			Game1.currentLocation.playSound("doorClose", null, null, SoundContext.NPC);
		}
	}

	public Farmer getSpouse()
	{
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.spouse != null && allFarmer.spouse == base.Name)
			{
				return allFarmer;
			}
		}
		return null;
	}

	public string getTermOfSpousalEndearment(bool happy = true)
	{
		Farmer spouse = getSpouse();
		if (spouse != null)
		{
			if (isRoommate())
			{
				return spouse.displayName;
			}
			if (spouse.getFriendshipHeartLevelForNPC(base.Name) < 9)
			{
				return spouse.displayName;
			}
			if (!happy)
			{
				return Game1.random.Next(2) switch
				{
					0 => Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4517"), 
					1 => Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4518"), 
					_ => spouse.displayName, 
				};
			}
			if (Game1.random.NextDouble() < 0.08)
			{
				switch (Game1.random.Next(8))
				{
				case 0:
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4507");
				case 1:
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4508");
				case 2:
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4509");
				case 3:
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4510");
				case 4:
					if (!spouse.IsMale)
					{
						return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4512");
					}
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4511");
				case 5:
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4513");
				case 6:
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4514");
				default:
					if (!spouse.IsMale)
					{
						return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4516");
					}
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4515");
				}
			}
			return Game1.random.Next(5) switch
			{
				0 => Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4519"), 
				1 => Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4518"), 
				2 => Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4517"), 
				3 => Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4522"), 
				_ => Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4523"), 
			};
		}
		return Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4517");
	}

	/// <summary>
	/// return true if spouse encountered obstacle.
	/// if force == true then the obstacle check will be ignored and spouse will absolutely be put into bed.
	/// </summary>
	/// <param name="backToBedMessage"></param>
	/// <param name="currentLocation"></param>
	/// <returns></returns>
	public bool spouseObstacleCheck(MarriageDialogueReference backToBedMessage, GameLocation currentLocation, bool force = false)
	{
		if (force || checkTileOccupancyForSpouse(currentLocation, base.Tile, base.Name))
		{
			Game1.warpCharacter(this, defaultMap.Value, Game1.RequireLocation<FarmHouse>(defaultMap.Value).getSpouseBedSpot(name.Value));
			faceDirection(1);
			currentMarriageDialogue.Clear();
			currentMarriageDialogue.Add(backToBedMessage);
			shouldSayMarriageDialogue.Value = true;
			return true;
		}
		return false;
	}

	public void setTilePosition(Point p)
	{
		setTilePosition(p.X, p.Y);
	}

	public void setTilePosition(int x, int y)
	{
		base.Position = new Vector2(x * 64, y * 64);
	}

	private void clintHammerSound(Farmer who)
	{
		base.currentLocation.playSound("hammer", base.Tile);
	}

	private void robinHammerSound(Farmer who)
	{
		if (Game1.currentLocation.Equals(base.currentLocation) && Utility.isOnScreen(base.Position, 256))
		{
			Game1.playSound((Game1.random.NextDouble() < 0.1) ? "clank" : "axchop");
			shakeTimer = 250;
		}
	}

	private void robinVariablePause(Farmer who)
	{
		if (Game1.random.NextDouble() < 0.4)
		{
			Sprite.CurrentAnimation[Sprite.currentAnimationIndex] = new FarmerSprite.AnimationFrame(27, 300, secondaryArm: false, flip: false, robinVariablePause);
		}
		else if (Game1.random.NextDouble() < 0.25)
		{
			Sprite.CurrentAnimation[Sprite.currentAnimationIndex] = new FarmerSprite.AnimationFrame(23, Game1.random.Next(500, 4000), secondaryArm: false, flip: false, robinVariablePause);
		}
		else
		{
			Sprite.CurrentAnimation[Sprite.currentAnimationIndex] = new FarmerSprite.AnimationFrame(27, Game1.random.Next(1000, 4000), secondaryArm: false, flip: false, robinVariablePause);
		}
	}

	public void randomSquareMovement(GameTime time)
	{
		Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
		boundingBox.Inflate(2, 2);
		Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle((int)nextSquarePosition.X * 64, (int)nextSquarePosition.Y * 64, 64, 64);
		if (nextSquarePosition.Equals(Vector2.Zero))
		{
			squarePauseAccumulation = 0;
			squarePauseTotal = Game1.random.Next(6000 + squarePauseOffset, 12000 + squarePauseOffset);
			nextSquarePosition = new Vector2(lastCrossroad.X / 64 - lengthOfWalkingSquareX / 2 + Game1.random.Next(lengthOfWalkingSquareX), lastCrossroad.Y / 64 - lengthOfWalkingSquareY / 2 + Game1.random.Next(lengthOfWalkingSquareY));
		}
		else if (rectangle.Contains(boundingBox))
		{
			Halt();
			if (squareMovementFacingPreference != -1)
			{
				faceDirection(squareMovementFacingPreference);
			}
			isCharging = false;
			base.speed = 2;
		}
		else if (boundingBox.Left <= rectangle.Left)
		{
			SetMovingOnlyRight();
		}
		else if (boundingBox.Right >= rectangle.Right)
		{
			SetMovingOnlyLeft();
		}
		else if (boundingBox.Top <= rectangle.Top)
		{
			SetMovingOnlyDown();
		}
		else if (boundingBox.Bottom >= rectangle.Bottom)
		{
			SetMovingOnlyUp();
		}
		squarePauseAccumulation += time.ElapsedGameTime.Milliseconds;
		if (squarePauseAccumulation >= squarePauseTotal && rectangle.Contains(boundingBox))
		{
			nextSquarePosition = Vector2.Zero;
			isCharging = false;
			base.speed = 2;
		}
	}

	public void returnToEndPoint()
	{
		Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
		boundingBox.Inflate(2, 2);
		if (boundingBox.Left <= lastCrossroad.Left)
		{
			SetMovingOnlyRight();
		}
		else if (boundingBox.Right >= lastCrossroad.Right)
		{
			SetMovingOnlyLeft();
		}
		else if (boundingBox.Top <= lastCrossroad.Top)
		{
			SetMovingOnlyDown();
		}
		else if (boundingBox.Bottom >= lastCrossroad.Bottom)
		{
			SetMovingOnlyUp();
		}
		boundingBox.Inflate(-2, -2);
		if (lastCrossroad.Contains(boundingBox))
		{
			isWalkingInSquare = false;
			nextSquarePosition = Vector2.Zero;
			returningToEndPoint = false;
			Halt();
		}
	}

	public void SetMovingOnlyUp()
	{
		moveUp = true;
		moveDown = false;
		moveLeft = false;
		moveRight = false;
	}

	public void SetMovingOnlyRight()
	{
		moveUp = false;
		moveDown = false;
		moveLeft = false;
		moveRight = true;
	}

	public void SetMovingOnlyDown()
	{
		moveUp = false;
		moveDown = true;
		moveLeft = false;
		moveRight = false;
	}

	public void SetMovingOnlyLeft()
	{
		moveUp = false;
		moveDown = false;
		moveLeft = true;
		moveRight = false;
	}

	public virtual int getTimeFarmerMustPushBeforePassingThrough()
	{
		return 1500;
	}

	public virtual int getTimeFarmerMustPushBeforeStartShaking()
	{
		return 400;
	}

	public int CompareTo(object obj)
	{
		if (obj is NPC nPC)
		{
			return nPC.id - id;
		}
		return 0;
	}

	public virtual void Removed()
	{
	}
}
