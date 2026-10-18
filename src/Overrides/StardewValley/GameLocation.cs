using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using Netcode.Validation;
using StardewValley.Audio;
using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Constants;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Characters;
using StardewValley.GameData.GarbageCans;
using StardewValley.GameData.LocationContexts;
using StardewValley.GameData.Locations;
using StardewValley.GameData.Minecarts;
using StardewValley.GameData.Movies;
using StardewValley.GameData.WildTrees;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Mods;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Network.NetEvents;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.Pathfinding;
using StardewValley.Projectiles;
using StardewValley.Quests;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using StardewValley.Util;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace StardewValley;

[XmlInclude(typeof(AbandonedJojaMart))]
[XmlInclude(typeof(AdventureGuild))]
[XmlInclude(typeof(AnimalHouse))]
[XmlInclude(typeof(BathHousePool))]
[XmlInclude(typeof(Beach))]
[XmlInclude(typeof(BeachNightMarket))]
[XmlInclude(typeof(BoatTunnel))]
[XmlInclude(typeof(BugLand))]
[XmlInclude(typeof(BusStop))]
[XmlInclude(typeof(Cabin))]
[XmlInclude(typeof(Caldera))]
[XmlInclude(typeof(Cellar))]
[XmlInclude(typeof(Club))]
[XmlInclude(typeof(CommunityCenter))]
[XmlInclude(typeof(DecoratableLocation))]
[XmlInclude(typeof(Desert))]
[XmlInclude(typeof(DesertFestival))]
[XmlInclude(typeof(Farm))]
[XmlInclude(typeof(FarmCave))]
[XmlInclude(typeof(FarmHouse))]
[XmlInclude(typeof(FishShop))]
[XmlInclude(typeof(Forest))]
[XmlInclude(typeof(IslandEast))]
[XmlInclude(typeof(IslandFarmCave))]
[XmlInclude(typeof(IslandFarmHouse))]
[XmlInclude(typeof(IslandFieldOffice))]
[XmlInclude(typeof(IslandForestLocation))]
[XmlInclude(typeof(IslandHut))]
[XmlInclude(typeof(IslandLocation))]
[XmlInclude(typeof(IslandNorth))]
[XmlInclude(typeof(IslandSecret))]
[XmlInclude(typeof(IslandShrine))]
[XmlInclude(typeof(IslandSouth))]
[XmlInclude(typeof(IslandSouthEast))]
[XmlInclude(typeof(IslandSouthEastCave))]
[XmlInclude(typeof(IslandWest))]
[XmlInclude(typeof(IslandWestCave1))]
[XmlInclude(typeof(JojaMart))]
[XmlInclude(typeof(LibraryMuseum))]
[XmlInclude(typeof(ManorHouse))]
[XmlInclude(typeof(MermaidHouse))]
[XmlInclude(typeof(Mine))]
[XmlInclude(typeof(MineShaft))]
[XmlInclude(typeof(Mountain))]
[XmlInclude(typeof(MovieTheater))]
[XmlInclude(typeof(Railroad))]
[XmlInclude(typeof(SeedShop))]
[XmlInclude(typeof(Sewer))]
[XmlInclude(typeof(Shed))]
[XmlInclude(typeof(ShopLocation))]
[XmlInclude(typeof(SlimeHutch))]
[XmlInclude(typeof(Submarine))]
[XmlInclude(typeof(Summit))]
[XmlInclude(typeof(Town))]
[XmlInclude(typeof(WizardHouse))]
[XmlInclude(typeof(Woods))]
[InstanceStatics]
[NotImplicitNetField]
public class GameLocation : INetObject<NetFields>, IEquatable<GameLocation>, IAnimalLocation, IHaveModData
{
	public delegate void afterQuestionBehavior(Farmer who, string whichAnswer);

	/// <summary>A request to damage players who overlap a bounding box within the current location.</summary>
	private struct DamagePlayersEventArg : NetEventArg
	{
		/// <summary>The location pixel area where players will take damage.</summary>
		public Microsoft.Xna.Framework.Rectangle Area;

		/// <summary>The amount of damage the player should take.</summary>
		public int Damage;

		/// <summary>Whether the damage source was a bomb.</summary>
		public bool IsBomb;

		/// <summary>Reads the request data from a net-sync stream.</summary>
		/// <param name="reader">The binary stream to read.</param>
		public void Read(BinaryReader reader)
		{
			Area = reader.ReadRectangle();
			Damage = reader.ReadInt32();
			IsBomb = reader.ReadBoolean();
		}

		/// <summary>Writes the request data to a net-sync stream.</summary>
		/// <param name="writer">The binary stream to write to.</param>
		public void Write(BinaryWriter writer)
		{
			writer.WriteRectangle(Area);
			writer.Write(Damage);
			writer.Write(IsBomb);
		}
	}

	public const int maxTriesForDebrisPlacement = 3;

	/// <summary>The default ID for a map tile sheet. This often (but not always) matches the main tile sheet for Stardew Valley maps.</summary>
	public const string DefaultTileSheetId = "untitled tile sheet";

	public const string OVERRIDE_MAP_TILESHEET_PREFIX = "zzzzz";

	public const string PHONE_DIAL_SOUND = "telephone_buttonPush";

	public const int PHONE_RING_DURATION = 4950;

	public const string PHONE_PICKUP_SOUND = "bigSelect";

	public const string PHONE_HANGUP_SOUND = "openBox";

	/// <summary>The ocean fish types.</summary>
	public static readonly IList<string> OceanCrabPotFishTypes = new string[1] { "ocean" };

	/// <summary>The default fish types caught by crab pots in all locations which don't have a specific value in <c>Data/Locations</c>.</summary>
	public static readonly IList<string> DefaultCrabPotFishTypes = new string[1] { "freshwater" };

	/// <summary>The cached value for <see cref="M:StardewValley.GameLocation.GetSeason" />.</summary>
	/// <remarks>Most code should use <see cref="M:StardewValley.GameLocation.GetSeason" /> instead.</remarks>
	[XmlIgnore]
	private Lazy<Season?> seasonOverride;

	[XmlIgnore]
	public bool? isMusicTownMusic;

	/// <summary>The cached location context ID for <see cref="M:StardewValley.GameLocation.GetLocationContextId" />.</summary>
	/// <remarks>Most code should use <see cref="M:StardewValley.GameLocation.GetLocationContextId" /> or <see cref="M:StardewValley.GameLocation.GetLocationContext" /> instead.</remarks>
	[XmlIgnore]
	public string locationContextId;

	public readonly NetCollection<Building> buildings = new NetCollection<Building>
	{
		InterpolationWait = false
	};

	[XmlElement("animals")]
	public readonly NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>> animals = new NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>();

	[XmlElement("piecesOfHay")]
	public readonly NetInt piecesOfHay = new NetInt(0);

	private readonly List<KeyValuePair<long, FarmAnimal>> tempAnimals = new List<KeyValuePair<long, FarmAnimal>>();

	/// <summary>The unique name of the parent location, if applicable.</summary>
	[XmlIgnore]
	public readonly NetString parentLocationName = new NetString();

	/// <summary>The building which contains this location, if applicable.</summary>
	[XmlIgnore]
	public Building ParentBuilding;

	[XmlIgnore]
	public List<KeyValuePair<Layer, int>> backgroundLayers = new List<KeyValuePair<Layer, int>>();

	[XmlIgnore]
	public List<KeyValuePair<Layer, int>> buildingLayers = new List<KeyValuePair<Layer, int>>();

	[XmlIgnore]
	public List<KeyValuePair<Layer, int>> frontLayers = new List<KeyValuePair<Layer, int>>();

	[XmlIgnore]
	public List<KeyValuePair<Layer, int>> alwaysFrontLayers = new List<KeyValuePair<Layer, int>>();

	[NonInstancedStatic]
	[XmlIgnore]
	protected static Dictionary<string, Action<GameLocation, string[], Farmer, Vector2>> registeredTouchActions = new Dictionary<string, Action<GameLocation, string[], Farmer, Vector2>>();

	[NonInstancedStatic]
	[XmlIgnore]
	protected static Dictionary<string, Func<GameLocation, string[], Farmer, Point, bool>> registeredTileActions = new Dictionary<string, Func<GameLocation, string[], Farmer, Point, bool>>();

	/// <summary>Whether this location should always be synchronized in multiplayer. </summary>
	/// <remarks>
	///   <para>This value should only be set when the location is instantiated, it shouldn't be modified during gameplay.</para>
	///
	///   <para>Most code should call <see cref="M:StardewValley.Multiplayer.isAlwaysActiveLocation(StardewValley.GameLocation)" /> instead.</para>
	/// </remarks>
	[XmlIgnore]
	public NetBool isAlwaysActive = new NetBool();

	[XmlIgnore]
	public afterQuestionBehavior afterQuestion;

	[XmlIgnore]
	public Map map;

	[XmlIgnore]
	public readonly NetString mapPath = new NetString().Interpolated(interpolate: false, wait: false);

	[XmlIgnore]
	protected string loadedMapPath;

	public readonly NetCollection<NPC> characters = new NetCollection<NPC>();

	[XmlIgnore]
	public readonly NetVector2Dictionary<Object, NetRef<Object>> netObjects = new NetVector2Dictionary<Object, NetRef<Object>>();

	[XmlIgnore]
	public readonly OverlayDictionary<Vector2, Object> overlayObjects = new OverlayDictionary<Vector2, Object>(tilePositionComparer);

	[XmlElement("objects")]
	public readonly OverlaidDictionary objects;

	[XmlIgnore]
	public NetList<MapSeat, NetRef<MapSeat>> mapSeats = new NetList<MapSeat, NetRef<MapSeat>>();

	protected bool _mapSeatsDirty;

	[XmlIgnore]
	public TemporaryAnimatedSpriteList temporarySprites = new TemporaryAnimatedSpriteList();

	[XmlIgnore]
	public List<Action> postFarmEventOvernightActions = new List<Action>();

	[XmlIgnore]
	public readonly NetObjectList<Warp> warps = new NetObjectList<Warp>();

	[XmlIgnore]
	public readonly NetPointDictionary<string, NetString> doors = new NetPointDictionary<string, NetString>();

	[XmlIgnore]
	public readonly InteriorDoorDictionary interiorDoors;

	[XmlIgnore]
	public readonly FarmerCollection farmers;

	[XmlIgnore]
	public readonly NetCollection<Projectile> projectiles = new NetCollection<Projectile>();

	public readonly NetCollection<ResourceClump> resourceClumps = new NetCollection<ResourceClump>();

	public readonly NetCollection<LargeTerrainFeature> largeTerrainFeatures = new NetCollection<LargeTerrainFeature>();

	/// <summary>The terrain features whose <see cref="M:StardewValley.TerrainFeatures.TerrainFeature.tickUpdate(Microsoft.Xna.Framework.GameTime)" /> method should be called on each tick.</summary>
	[XmlIgnore]
	public List<TerrainFeature> _activeTerrainFeatures = new List<TerrainFeature>();

	[XmlIgnore]
	public List<Critter> critters;

	[XmlElement("terrainFeatures")]
	public readonly NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>> terrainFeatures = new NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>>();

	[XmlIgnore]
	public readonly NetCollection<Debris> debris = new NetCollection<Debris>();

	[XmlIgnore]
	public readonly NetPoint fishSplashPoint = new NetPoint(Point.Zero);

	private int fishSplashPointTime;

	[XmlIgnore]
	public readonly NetString fishFrenzyFish = new NetString();

	[XmlIgnore]
	public readonly NetPoint orePanPoint = new NetPoint(Point.Zero);

	[XmlIgnore]
	public TemporaryAnimatedSprite fishSplashAnimation;

	[XmlIgnore]
	public TemporaryAnimatedSprite orePanAnimation;

	[XmlIgnore]
	public WaterTiles waterTiles;

	[XmlIgnore]
	protected HashSet<string> _appliedMapOverrides;

	[XmlElement("uniqueName")]
	public readonly NetString uniqueName = new NetString();

	[XmlIgnore]
	protected string _displayName;

	[XmlElement("name")]
	public readonly NetString name = new NetString();

	[XmlElement("waterColor")]
	public readonly NetColor waterColor = new NetColor(Color.White * 0.33f);

	[XmlIgnore]
	public string lastQuestionKey;

	[XmlIgnore]
	public Vector2 lastTouchActionLocation = Vector2.Zero;

	[XmlElement("lightLevel")]
	protected readonly NetFloat lightLevel = new NetFloat(0f);

	[XmlElement("isFarm")]
	public readonly NetBool isFarm = new NetBool();

	[XmlElement("isOutdoors")]
	public readonly NetBool isOutdoors = new NetBool();

	[XmlIgnore]
	public readonly NetBool isGreenhouse = new NetBool();

	[XmlElement("isStructure")]
	public readonly NetBool isStructure = new NetBool();

	[XmlElement("ignoreDebrisWeather")]
	public readonly NetBool ignoreDebrisWeather = new NetBool();

	[XmlElement("ignoreOutdoorLighting")]
	public readonly NetBool ignoreOutdoorLighting = new NetBool();

	[XmlElement("ignoreLights")]
	public readonly NetBool ignoreLights = new NetBool();

	[XmlElement("treatAsOutdoors")]
	public readonly NetBool treatAsOutdoors = new NetBool();

	[XmlIgnore]
	public bool wasUpdated;

	public int numberOfSpawnedObjectsOnMap;

	[XmlIgnore]
	public bool showDropboxIndicator;

	[XmlIgnore]
	public Vector2 dropBoxIndicatorLocation;

	[XmlElement("miniJukeboxCount")]
	public readonly NetInt miniJukeboxCount = new NetInt();

	[XmlElement("miniJukeboxTrack")]
	public readonly NetString miniJukeboxTrack = new NetString("");

	[XmlIgnore]
	public readonly NetString randomMiniJukeboxTrack = new NetString();

	[XmlIgnore]
	public Event currentEvent;

	[XmlIgnore]
	public Object actionObjectForQuestionDialogue;

	[XmlIgnore]
	public int waterAnimationIndex;

	[XmlIgnore]
	public int waterAnimationTimer;

	[XmlIgnore]
	public bool waterTileFlip;

	[XmlIgnore]
	public bool forceViewportPlayerFollow;

	[XmlIgnore]
	public bool forceLoadPathLayerLights;

	[XmlIgnore]
	public float waterPosition;

	[XmlIgnore]
	public readonly NetAudio netAudio;

	/// <summary>The light sources to draw for players in this location.</summary>
	[XmlIgnore]
	public readonly NetStringDictionary<LightSource, NetRef<LightSource>> sharedLights = new NetStringDictionary<LightSource, NetRef<LightSource>>();

	private readonly NetEvent1Field<int, NetInt> removeTemporarySpritesWithIDEvent = new NetEvent1Field<int, NetInt>();

	private readonly NetEvent1Field<int, NetInt> rumbleAndFadeEvent = new NetEvent1Field<int, NetInt>();

	/// <summary>An event raised to damage players within the current location.</summary>
	private readonly NetEvent1<DamagePlayersEventArg> damagePlayersEvent = new NetEvent1<DamagePlayersEventArg>();

	[XmlIgnore]
	public NetVector2HashSet lightGlows = new NetVector2HashSet();

	public static readonly int JOURNAL_INDEX = 1000;

	public static readonly float FIRST_SECRET_NOTE_CHANCE = 0.8f;

	public static readonly float LAST_SECRET_NOTE_CHANCE = 0.12f;

	public static readonly int NECKLACE_SECRET_NOTE_INDEX = 25;

	public static readonly string CAROLINES_NECKLACE_ITEM_QID = "(O)191";

	public static readonly string CAROLINES_NECKLACE_MAIL = "carolinesNecklace";

	public static TilePositionComparer tilePositionComparer = new TilePositionComparer();

	protected List<Vector2> _startingCabinLocations = new List<Vector2>();

	[XmlIgnore]
	public bool wasInhabited;

	[XmlIgnore]
	protected bool _madeMapModifications;

	public readonly NetCollection<Furniture> furniture = new NetCollection<Furniture>
	{
		InterpolationWait = false
	};

	protected readonly NetMutexQueue<Guid> furnitureToRemove = new NetMutexQueue<Guid>();

	protected bool _mapPathDirty = true;

	protected LocalizedContentManager _structureMapLoader;

	protected bool ignoreWarps;

	protected HashSet<Vector2> _visitedCollisionTiles = new HashSet<Vector2>();

	protected bool _looserBuildRestrictions;

	protected Microsoft.Xna.Framework.Rectangle? _buildableTileRect;

	private bool showedBuildableButNotAlwaysActiveWarning;

	public static bool PlayedNewLocationContextMusic = false;

	private const int fireIDBase = 944468;

	protected Color indoorLightingColor = new Color(100, 120, 30);

	protected Color indoorLightingNightColor = new Color(150, 150, 30);

	protected static List<KeyValuePair<string, string>> _PagedResponses = new List<KeyValuePair<string, string>>();

	protected static int _PagedResponsePage = 0;

	protected static int _PagedResponseItemsPerPage;

	public static bool _PagedResponseAddCancel;

	protected static string _PagedResponsePrompt;

	protected static Action<string> _OnPagedResponse;

	protected string _constructLocationBuilderName;

	protected List<Farmer> _currentLocationFarmersForDisambiguating = new List<Farmer>();

	[XmlIgnore]
	public Dictionary<Vector2, float> lightGlowLayerCache = new Dictionary<Vector2, float>();

	public NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>> Animals => animals;

	[XmlIgnore]
	public NetFields NetFields { get; }

	[XmlIgnore]
	public NetRoot<GameLocation> Root => NetFields.Root as NetRoot<GameLocation>;

	/// <summary>The number of milliseconds to add to <see cref="F:StardewValley.Game1.realMilliSecondsPerGameMinute" /> when calculating the flow of time within this location.</summary>
	[XmlIgnore]
	public int ExtraMillisecondsPerInGameMinute { get; set; }

	[XmlIgnore]
	public string DisplayName
	{
		get
		{
			if (_displayName == null)
			{
				_displayName = GetDisplayName();
			}
			if (_displayName == null)
			{
				string text = GetParentLocation()?.DisplayName;
				if (text != null)
				{
					return text;
				}
				return Name;
			}
			return _displayName;
		}
		set
		{
			_displayName = value;
		}
	}

	[XmlIgnore]
	public string NameOrUniqueName
	{
		get
		{
			if (uniqueName.Value != null)
			{
				return uniqueName.Value;
			}
			return name.Value;
		}
	}

	/// <summary>Whether this is a temporary location for a festival or event.</summary>
	/// <remarks>This is set automatically based on <see cref="M:StardewValley.GameLocation.IsTemporaryName(System.String)" />.</remarks>
	[XmlIgnore]
	public bool IsTemporary { get; protected set; }

	[XmlIgnore]
	public float LightLevel
	{
		get
		{
			return lightLevel.Value;
		}
		set
		{
			lightLevel.Value = value;
		}
	}

	[XmlIgnore]
	public Map Map
	{
		get
		{
			updateMap();
			return map;
		}
		set
		{
			map = value;
		}
	}

	[XmlIgnore]
	public OverlaidDictionary Objects => objects;

	[XmlIgnore]
	public TemporaryAnimatedSpriteList TemporarySprites => temporarySprites;

	public string Name => name.Value;

	[XmlIgnore]
	public bool IsFarm
	{
		get
		{
			return isFarm.Value;
		}
		set
		{
			isFarm.Value = value;
		}
	}

	[XmlIgnore]
	public bool IsOutdoors
	{
		get
		{
			return isOutdoors.Value;
		}
		set
		{
			isOutdoors.Value = value;
		}
	}

	public bool IsGreenhouse
	{
		get
		{
			return isGreenhouse.Value;
		}
		set
		{
			isGreenhouse.Value = value;
		}
	}

	/// <inheritdoc />
	[XmlIgnore]
	public ModDataDictionary modData { get; } = new ModDataDictionary();

	/// <inheritdoc />
	[XmlElement("modData")]
	public ModDataDictionary modDataForSerialization
	{
		get
		{
			return modData.GetForSerialization();
		}
		set
		{
			modData.SetFromSerialization(value);
		}
	}

	public virtual string GetDisplayName()
	{
		string text = GetData()?.DisplayName;
		if (text == null)
		{
			return null;
		}
		return TokenParser.ParseText(text);
	}

	/// <summary>Whether seeds and sapling can be planted and grown in any season here.</summary>
	public virtual bool SeedsIgnoreSeasonsHere()
	{
		return IsGreenhouse;
	}

	/// <summary>Get whether crop seeds can be planted in this location.</summary>
	/// <param name="itemId">The qualified or unqualified item ID for the seed being planted.</param>
	/// <param name="tileX">The X tile position for which to apply location-specific overrides.</param>
	/// <param name="tileY">The Y tile position for which to apply location-specific overrides.</param>
	/// <param name="isGardenPot">Whether the item is being planted in a garden pot.</param>
	/// <param name="deniedMessage">The translated message to show to the user indicating why it can't be planted, if applicable.</param>
	public virtual bool CanPlantSeedsHere(string itemId, int tileX, int tileY, bool isGardenPot, out string deniedMessage)
	{
		return CheckItemPlantRules(itemId, isGardenPot, GetData()?.CanPlantHere ?? IsFarm, out deniedMessage);
	}

	/// <summary>Get whether tree saplings can be planted in this location.</summary>
	/// <param name="itemId">The qualified or unqualified item ID for the sapling being planted.</param>
	/// <param name="tileX">The X tile position for which to apply location-specific overrides.</param>
	/// <param name="tileY">The Y tile position for which to apply location-specific overrides.</param>
	/// <param name="deniedMessage">The translated message to show to the user indicating why it can't be planted, if applicable.</param>
	public virtual bool CanPlantTreesHere(string itemId, int tileX, int tileY, out string deniedMessage)
	{
		return CheckItemPlantRules(itemId, isGardenPot: false, IsGreenhouse || IsFarm || (GetData()?.CanPlantHere ?? false) || (Object.isWildTreeSeed(itemId) && IsOutdoors && doesTileHavePropertyNoNull(tileX, tileY, "Type", "Back") == "Dirt") || (map?.Properties.ContainsKey("ForceAllowTreePlanting") ?? false), out deniedMessage);
	}

	/// <summary>Get whether a crop or tree can be planted here according to the planting rules in its data.</summary>
	/// <param name="itemId">The qualified or unqualified item ID for the seed or sapling being planted.</param>
	/// <param name="isGardenPot">Whether the item is being planted in a garden pot.</param>
	/// <param name="defaultAllowed">The result to return when no rules apply, or the selected rule uses <see cref="F:StardewValley.GameData.PlantableResult.Default" />.</param>
	/// <param name="deniedMessage">The translated message to show to the user indicating why it can't be planted, if applicable.</param>
	/// <remarks>This is a low-level method which doesn't check higher-level requirements. Most code should call <see cref="M:StardewValley.GameLocation.CanPlantSeedsHere(System.String,System.Int32,System.Int32,System.Boolean,System.String@)" /> or <see cref="M:StardewValley.GameLocation.CanPlantTreesHere(System.String,System.Int32,System.Int32,System.String@)" /> instead.</remarks>
	public bool CheckItemPlantRules(string itemId, bool isGardenPot, bool defaultAllowed, out string deniedMessage)
	{
		ItemMetadata metadata = ItemRegistry.GetMetadata(itemId);
		if (metadata != null && metadata.TypeIdentifier == "(O)")
		{
			itemId = metadata.LocalItemId;
			if (Crop.TryGetData(itemId, out var data))
			{
				return CheckItemPlantRules(data.PlantableLocationRules, isGardenPot, defaultAllowed, out deniedMessage);
			}
			string text = Tree.ResolveTreeTypeFromSeed(metadata.QualifiedItemId);
			if (text != null && Tree.TryGetData(text, out var data2))
			{
				return CheckItemPlantRules(data2.PlantableLocationRules, isGardenPot, defaultAllowed, out deniedMessage);
			}
			if (FruitTree.TryGetData(itemId, out var data3))
			{
				return CheckItemPlantRules(data3.PlantableLocationRules, isGardenPot, defaultAllowed, out deniedMessage);
			}
		}
		deniedMessage = null;
		return defaultAllowed;
	}

	/// <summary>Get whether a crop or tree can be planted here according to the planting rules in its data.</summary>
	/// <param name="rules">The plantable rules to check.</param>
	/// <param name="isGardenPot">Whether the item is being planted in a garden pot.</param>
	/// <param name="defaultAllowed">The result to return when no rules apply, or the selected rule uses <see cref="F:StardewValley.GameData.PlantableResult.Default" />.</param>
	/// <param name="deniedMessage">The translated message to show to the user indicating why it can't be planted, if applicable.</param>
	/// <remarks>This is a low-level method which doesn't check higher-level requirements. Most code should call <see cref="M:StardewValley.GameLocation.CanPlantSeedsHere(System.String,System.Int32,System.Int32,System.Boolean,System.String@)" /> or <see cref="M:StardewValley.GameLocation.CanPlantTreesHere(System.String,System.Int32,System.Int32,System.String@)" /> instead.</remarks>
	private bool CheckItemPlantRules(List<PlantableRule> rules, bool isGardenPot, bool defaultAllowed, out string deniedMessage)
	{
		if (rules != null && rules.Count > 0)
		{
			foreach (PlantableRule rule in rules)
			{
				if (rule.ShouldApplyWhen(isGardenPot) && GameStateQuery.CheckConditions(rule.Condition, this))
				{
					switch (rule.Result)
					{
					case PlantableResult.Allow:
						deniedMessage = null;
						return true;
					case PlantableResult.Deny:
						deniedMessage = TokenParser.ParseText(rule.DeniedMessage);
						return false;
					default:
						deniedMessage = ((!defaultAllowed) ? TokenParser.ParseText(rule.DeniedMessage) : null);
						return defaultAllowed;
					}
				}
			}
		}
		deniedMessage = null;
		return defaultAllowed;
	}

	protected virtual void initNetFields()
	{
		NetFields.SetOwner(this).AddField(mapPath, "mapPath").AddField(uniqueName, "uniqueName")
			.AddField(name, "name")
			.AddField(lightLevel, "lightLevel")
			.AddField(sharedLights, "sharedLights")
			.AddField(isFarm, "isFarm")
			.AddField(isOutdoors, "isOutdoors")
			.AddField(isStructure, "isStructure")
			.AddField(ignoreDebrisWeather, "ignoreDebrisWeather")
			.AddField(ignoreOutdoorLighting, "ignoreOutdoorLighting")
			.AddField(ignoreLights, "ignoreLights")
			.AddField(treatAsOutdoors, "treatAsOutdoors")
			.AddField(warps, "warps")
			.AddField(doors, "doors")
			.AddField(interiorDoors, "interiorDoors")
			.AddField(waterColor, "waterColor")
			.AddField(netObjects, "netObjects")
			.AddField(projectiles, "projectiles")
			.AddField(largeTerrainFeatures, "largeTerrainFeatures")
			.AddField(terrainFeatures, "terrainFeatures")
			.AddField(characters, "characters")
			.AddField(debris, "debris")
			.AddField(netAudio.NetFields, "netAudio.NetFields")
			.AddField(removeTemporarySpritesWithIDEvent, "removeTemporarySpritesWithIDEvent")
			.AddField(rumbleAndFadeEvent, "rumbleAndFadeEvent")
			.AddField(damagePlayersEvent, "damagePlayersEvent")
			.AddField(lightGlows, "lightGlows")
			.AddField(fishSplashPoint, "fishSplashPoint")
			.AddField(fishFrenzyFish, "fishFrenzyFish")
			.AddField(orePanPoint, "orePanPoint")
			.AddField(isGreenhouse, "isGreenhouse")
			.AddField(miniJukeboxCount, "miniJukeboxCount")
			.AddField(miniJukeboxTrack, "miniJukeboxTrack")
			.AddField(randomMiniJukeboxTrack, "randomMiniJukeboxTrack")
			.AddField(resourceClumps, "resourceClumps")
			.AddField(isAlwaysActive, "isAlwaysActive")
			.AddField(furniture, "furniture")
			.AddField(furnitureToRemove.NetFields, "furnitureToRemove.NetFields")
			.AddField(parentLocationName, "parentLocationName")
			.AddField(buildings, "buildings")
			.AddField(animals, "animals")
			.AddField(piecesOfHay, "piecesOfHay")
			.AddField(mapSeats, "mapSeats")
			.AddField(modData, "modData");
		mapPath.fieldChangeVisibleEvent += delegate
		{
			_mapPathDirty = true;
		};
		name.fieldChangeVisibleEvent += delegate
		{
			OnNameChanged();
		};
		uniqueName.fieldChangeVisibleEvent += delegate
		{
			OnNameChanged();
		};
		parentLocationName.fieldChangeVisibleEvent += delegate
		{
			OnParentLocationChanged();
		};
		buildings.OnValueAdded += delegate(Building b)
		{
			if (b != null)
			{
				b.parentLocationName.Value = NameOrUniqueName;
				b.updateInteriorWarps();
			}
			if (Game1.IsMasterGame)
			{
				Game1.netWorldState.Value.UpdateBuildingCache(this);
			}
		};
		buildings.OnValueRemoved += delegate(Building b)
		{
			if (b != null)
			{
				b.parentLocationName.Value = null;
			}
			if (Game1.IsMasterGame)
			{
				Game1.netWorldState.Value.UpdateBuildingCache(this);
			}
		};
		isStructure.fieldChangeVisibleEvent += delegate
		{
			if (mapPath.Value != null)
			{
				InvalidateCachedMultiplayerMap(Game1.multiplayer.cachedMultiplayerMaps);
				reloadMap();
			}
		};
		sharedLights.OnValueAdded += delegate(string identifier, LightSource light)
		{
			if (Game1.currentLocation == this)
			{
				Game1.currentLightSources.Add(light);
			}
		};
		sharedLights.OnValueRemoved += delegate(string identifier, LightSource light)
		{
			if (Game1.currentLocation == this)
			{
				Game1.currentLightSources.Remove(light?.Id);
			}
		};
		netObjects.OnConflictResolve += delegate(Vector2 pos, NetRef<Object> rejected, NetRef<Object> accepted)
		{
			if (Game1.IsMasterGame)
			{
				Object value = rejected.Value;
				if (value != null)
				{
					value.onDetachedFromParent();
					value.dropItem(this, pos * 64f, pos * 64f);
				}
			}
		};
		netObjects.OnValueAdded += OnObjectAdded;
		overlayObjects.onValueAdded += OnObjectAdded;
		removeTemporarySpritesWithIDEvent.onEvent += removeTemporarySpritesWithIDLocal;
		rumbleAndFadeEvent.onEvent += performRumbleAndFade;
		damagePlayersEvent.onEvent += performDamagePlayers;
		fishSplashPoint.fieldChangeVisibleEvent += delegate
		{
			updateFishSplashAnimation();
		};
		orePanPoint.fieldChangeVisibleEvent += delegate
		{
			updateOrePanAnimation();
		};
		characters.OnValueRemoved += delegate(NPC npc)
		{
			npc.Removed();
		};
		terrainFeatures.OnValueAdded += delegate(Vector2 tile, TerrainFeature feature)
		{
			OnTerrainFeatureAdded(feature, tile);
		};
		terrainFeatures.OnValueRemoved += delegate(Vector2 tile, TerrainFeature feature)
		{
			OnTerrainFeatureRemoved(feature);
		};
		largeTerrainFeatures.OnValueAdded += delegate(LargeTerrainFeature feature)
		{
			OnTerrainFeatureAdded(feature, feature.Tile);
		};
		largeTerrainFeatures.OnValueRemoved += OnTerrainFeatureRemoved;
		resourceClumps.OnValueAdded += OnResourceClumpAdded;
		resourceClumps.OnValueRemoved += OnResourceClumpRemoved;
		furniture.OnValueAdded += delegate(Furniture f)
		{
			f.Location = this;
			f.OnAdded(this, f.TileLocation);
		};
		furniture.OnValueRemoved += delegate(Furniture f)
		{
			f.OnRemoved(this, f.TileLocation);
		};
		furnitureToRemove.Processor = removeQueuedFurniture;
	}

	public virtual void InvalidateCachedMultiplayerMap(Dictionary<string, CachedMultiplayerMap> cached_data)
	{
		if (!Game1.IsMasterGame)
		{
			cached_data.Remove(NameOrUniqueName);
		}
	}

	public virtual void MakeMapModifications(bool force = false)
	{
		if (force)
		{
			_appliedMapOverrides.Clear();
		}
		interiorDoors.MakeMapModifications();
		string value = name.Value;
		if (value == null)
		{
			return;
		}
		switch (value.Length)
		{
		case 10:
			switch (value[0])
			{
			case 'W':
				if (value == "WitchSwamp")
				{
					if (Game1.MasterPlayer.mailReceived.Contains("henchmanGone"))
					{
						removeTile(20, 29, "Buildings");
					}
					else
					{
						setMapTile(20, 29, 10, "Buildings", "wt");
					}
				}
				break;
			case 'H':
				if (value == "HaleyHouse" && Game1.player.eventsSeen.Contains("463391") && Game1.player.spouse != "Emily")
				{
					setMapTile(14, 4, 2173, "Buildings", "1");
					setMapTile(14, 3, 2141, "Buildings", "1");
					setMapTile(14, 3, 219, "Back", "1");
				}
				break;
			}
			break;
		case 9:
			switch (value[0])
			{
			case 'B':
				if (!(value == "Backwoods"))
				{
					break;
				}
				if (Game1.netWorldState.Value.hasWorldStateID("golemGrave"))
				{
					ApplyMapOverride("Backwoods_GraveSite");
				}
				if (Game1.MasterPlayer.mailReceived.Contains("communityUpgradeShortcuts") && !_appliedMapOverrides.Contains("Backwoods_Staircase"))
				{
					ApplyMapOverride("Backwoods_Staircase");
					LargeTerrainFeature largeTerrainFeature = null;
					foreach (LargeTerrainFeature largeTerrainFeature2 in largeTerrainFeatures)
					{
						if (largeTerrainFeature2.Tile == new Vector2(37f, 16f))
						{
							largeTerrainFeature = largeTerrainFeature2;
							break;
						}
					}
					if (largeTerrainFeature != null)
					{
						largeTerrainFeatures.Remove(largeTerrainFeature);
					}
				}
				if (!Game1.player.mailReceived.Contains("asdlkjfg1") || Game1.random.NextDouble() < 0.01)
				{
					setTileProperty(13, 29, "Back", "TouchAction", "asdlfkjg");
					setTileProperty(14, 29, "Back", "TouchAction", "asdlfkjg");
					setTileProperty(15, 29, "Back", "TouchAction", "asdlfkjg");
				}
				else if (Utility.doesAnyFarmerHaveMail("asdlkjfg1") && Utility.CreateDaySaveRandom(1244.0).NextDouble() < 0.02)
				{
					if (!IsTileOccupiedBy(new Vector2(13f, 26f)))
					{
						temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(495, 412, 16, 16), new Vector2(13f, 26f) * 64f, flipped: false, 0.003f, Color.White)
						{
							scale = 4f,
							layerDepth = 0f
						});
					}
					if (!IsTileOccupiedBy(new Vector2(12f, 25f)))
					{
						temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(495, 412, 16, 16), new Vector2(12f, 25f) * 64f, flipped: true, 0.003f, Color.White)
						{
							scale = 4f,
							layerDepth = 0f
						});
					}
					if (!IsTileOccupiedBy(new Vector2(13f, 24f)))
					{
						temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(495, 412, 16, 16), new Vector2(13f, 24f) * 64f, flipped: false, 0.003f, Color.White)
						{
							scale = 4f,
							layerDepth = 0f
						});
					}
					if (!IsTileOccupiedBy(new Vector2(13f, 23f)))
					{
						temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(495, 412, 16, 16), new Vector2(12f, 23f) * 64f, flipped: true, 0.003f, Color.White * 0.66f)
						{
							scale = 4f,
							layerDepth = 0f
						});
					}
					if (!IsTileOccupiedBy(new Vector2(13f, 22f)))
					{
						temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(495, 412, 16, 16), new Vector2(13f, 22f) * 64f, flipped: false, 0.003f, Color.White * 0.33f)
						{
							scale = 4f,
							layerDepth = 0f
						});
					}
				}
				if (Game1.timeOfDay >= 2400)
				{
					Random random = Utility.CreateDaySaveRandom(124.0);
					int num = Utility.ModifyTime(2400, random.Next(12) * 10);
					if (Game1.timeOfDay == num && random.NextDouble() < 0.33)
					{
						temporarySprites.Add(new TemporaryAnimatedSprite("Characters\\asldkfjsquaskutanfsldk", new Microsoft.Xna.Framework.Rectangle(0, 48, 32, 48), new Vector2(60f, -260f), flipped: true, 0f, Color.White)
						{
							animationLength = 8,
							totalNumberOfLoops = 99,
							interval = 120f,
							scale = 4f,
							motion = new Vector2(0.5f, 1f),
							yStopCoordinate = 256,
							xStopCoordinate = 256,
							delayBeforeAnimationStart = 1000
						});
					}
				}
				break;
			case 'S':
				if (value == "SkullCave")
				{
					bool flag = Game1.player.team.skullShrineActivated.Value || Game1.player.team.SpecialOrderRuleActive("SC_HARD");
					if (Game1.player.team.toggleSkullShrineOvernight.Value)
					{
						flag = !flag;
					}
					if (flag)
					{
						_appliedMapOverrides.Remove("SkullCaveAltarDeactivated");
						ApplyMapOverride("SkullCaveAltar", new Microsoft.Xna.Framework.Rectangle(0, 0, 5, 4), new Microsoft.Xna.Framework.Rectangle(10, 1, 5, 4));
						Game1.currentLightSources.Add(new LightSource("SkullCaveAltar", 4, new Vector2(12f, 3f) * 64f, 1f, LightSource.LightContext.MapLight, 0L, NameOrUniqueName));
						AmbientLocationSounds.addSound(new Vector2(12f, 3f), 1);
					}
					else
					{
						_appliedMapOverrides.Remove("SkullCaveAltar");
						ApplyMapOverride(Game1.temporaryContent.Load<Map>("Maps\\SkullCave"), "SkullCaveAltarDeactivated", new Microsoft.Xna.Framework.Rectangle(10, 1, 5, 4), new Microsoft.Xna.Framework.Rectangle(10, 1, 5, 4));
						Game1.currentLightSources.Remove("SkullCaveAltar");
						AmbientLocationSounds.removeSound(new Vector2(12f, 3f));
					}
				}
				break;
			}
			break;
		case 16:
			if (value == "IslandNorthCave1" && Game1.player.mailReceived.Contains("FizzIntro"))
			{
				if (getCharacterFromName("Fizz") == null)
				{
					characters.Add(new NPC(new AnimatedSprite("Characters\\Fizz", 0, 16, 32), new Vector2(6f, 3f) * 64f, 2, "Fizz")
					{
						SimpleNonVillagerNPC = true,
						Portrait = Game1.content.Load<Texture2D>("Portraits\\Fizz"),
						displayName = Game1.content.LoadString("Strings\\NPCNames:Fizz")
					});
					removeObjectsAndSpawned(6, 3, 1, 1);
				}
				else
				{
					getCharacterFromName("Fizz").SimpleNonVillagerNPC = true;
					getCharacterFromName("Fizz").Sprite.SpriteHeight = 32;
					getCharacterFromName("Fizz").Sprite.UpdateSourceRect();
				}
				Game1.currentLightSources.Add(new LightSource("IslandNorthCave1", 1, new Vector2(6f, 3f) * 64f + new Vector2(32f), 2f, LightSource.LightContext.None, 0L, NameOrUniqueName));
			}
			break;
		case 11:
			if (value == "MasteryCave")
			{
				Game1.stats.Get("MasteryExp");
				int currentMasteryLevel = MasteryTrackerMenu.getCurrentMasteryLevel();
				int levelsNotSpent = currentMasteryLevel - (int)Game1.stats.Get("masteryLevelsSpent");
				void ShowSkillMastery(int which, Vector2 pos)
				{
					if (levelsNotSpent > 0 && Game1.player.stats.Get(StatKeys.Mastery(which)) == 0)
					{
						temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(0, 81, 16, 16), pos * 4f, flipped: false, 0f, Color.White)
						{
							interval = 100f,
							animationLength = 4,
							totalNumberOfLoops = 99999,
							scale = 4f,
							id = 8765 + which
						});
					}
				}
				ShowSkillMastery(4, new Vector2(54f, 98f));
				ShowSkillMastery(2, new Vector2(84f, 82f));
				ShowSkillMastery(0, new Vector2(116f, 82f));
				ShowSkillMastery(0, new Vector2(116f, 82f));
				ShowSkillMastery(1, new Vector2(148f, 82f));
				ShowSkillMastery(3, new Vector2(179f, 98f));
				if (MasteryTrackerMenu.hasCompletedAllMasteryPlaques())
				{
					MasteryTrackerMenu.addSpiritCandles(instant: true);
					Game1.changeMusicTrack("grandpas_theme");
				}
			}
			break;
		case 19:
			if (value == "WizardHouseBasement" && Game1.player.mailReceived.Contains("hasActivatedForestPylon"))
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(0, 106, 14, 22), new Vector2(16.6f, 2.5f) * 64f, flipped: false, 0f, Color.White)
				{
					animationLength = 8,
					interval = 100f,
					totalNumberOfLoops = 9999,
					scale = 4f
				});
			}
			break;
		case 7:
			if (value == "Sunroom")
			{
				TileSheet tileSheet = map.RequireTileSheet(1, "2");
				string text = Path.GetDirectoryName(tileSheet.ImageSource);
				if (string.IsNullOrWhiteSpace(text))
				{
					text = "Maps";
				}
				tileSheet.ImageSource = Path.Combine(text, "CarolineGreenhouseTiles" + ((IsRainingHere() || Game1.timeOfDay > Game1.getTrulyDarkTime(this)) ? "_rainy" : ""));
				map.DisposeTileSheets(Game1.mapDisplayDevice);
				map.LoadTileSheets(Game1.mapDisplayDevice);
			}
			break;
		case 17:
			if (value == "AbandonedJojaMart")
			{
				if (!Game1.MasterPlayer.hasOrWillReceiveMail("ccMovieTheater"))
				{
					StaticTile[] junimoNoteTileFrames = CommunityCenter.getJunimoNoteTileFrames(0, map);
					string layerId = "Buildings";
					Point point = new Point(8, 8);
					map.RequireLayer(layerId).Tiles[point.X, point.Y] = new AnimatedTile(map.RequireLayer(layerId), junimoNoteTileFrames, 70L);
				}
				else
				{
					removeTile(8, 8, "Buildings");
				}
			}
			break;
		case 8:
			if (value == "WitchHut" && Game1.player.mailReceived.Contains("hasPickedUpMagicInk"))
			{
				setMapTile(4, 11, 113, "Buildings", "untitled tile sheet").Properties.Remove("Action");
			}
			break;
		case 6:
			if (value == "Saloon" && NetWorldState.checkAnywhereForWorldStateID("saloonSportsRoom"))
			{
				ApplyMapOverride("RefurbishedSaloonRoom", (Microsoft.Xna.Framework.Rectangle?)null, (Microsoft.Xna.Framework.Rectangle?)new Microsoft.Xna.Framework.Rectangle(32, 1, 6, 8));
				Game1.currentLightSources.Add(new LightSource("Saloon_1", 1, new Vector2(33f, 7f) * 64f, 4f, LightSource.LightContext.None, 0L, NameOrUniqueName));
				Game1.currentLightSources.Add(new LightSource("Saloon_2", 1, new Vector2(36f, 7f) * 64f, 4f, LightSource.LightContext.None, 0L, NameOrUniqueName));
				Game1.currentLightSources.Add(new LightSource("Saloon_3", 1, new Vector2(34f, 5f) * 64f, 4f, LightSource.LightContext.None, 0L, NameOrUniqueName));
			}
			break;
		case 12:
		case 13:
		case 14:
		case 15:
		case 18:
			break;
		}
	}

	public virtual bool ApplyCachedMultiplayerMap(Dictionary<string, CachedMultiplayerMap> cached_data, string requested_map_path)
	{
		if (Game1.IsMasterGame)
		{
			return false;
		}
		if (cached_data.TryGetValue(NameOrUniqueName, out var value))
		{
			if (value.mapPath == requested_map_path)
			{
				_appliedMapOverrides = value.appliedMapOverrides;
				map = value.map;
				loadedMapPath = value.loadedMapPath;
				return true;
			}
			cached_data.Remove(NameOrUniqueName);
			return false;
		}
		return false;
	}

	public virtual void StoreCachedMultiplayerMap(Dictionary<string, CachedMultiplayerMap> cached_data)
	{
		if (!Game1.IsMasterGame && !(this is VolcanoDungeon) && !(this is MineShaft))
		{
			CachedMultiplayerMap cachedMultiplayerMap = new CachedMultiplayerMap();
			cachedMultiplayerMap.map = map;
			cachedMultiplayerMap.appliedMapOverrides = _appliedMapOverrides;
			cachedMultiplayerMap.mapPath = mapPath.Value;
			cachedMultiplayerMap.loadedMapPath = loadedMapPath;
			cached_data[NameOrUniqueName] = cachedMultiplayerMap;
		}
	}

	public virtual void TransferDataFromSavedLocation(GameLocation l)
	{
		modData.Clear();
		if (l.modData != null)
		{
			foreach (string key in l.modData.Keys)
			{
				modData[key] = l.modData[key];
			}
		}
		miniJukeboxCount.Value = l.miniJukeboxCount.Value;
		miniJukeboxTrack.Value = l.miniJukeboxTrack.Value;
		SelectRandomMiniJukeboxTrack();
		UpdateMapSeats();
	}

	/// <summary>Reset cached data when the name or unique name changes.</summary>
	private void OnNameChanged()
	{
		IsTemporary = IsTemporaryName(Name);
	}

	/// <summary>Reset cached data when the parent location changes.</summary>
	private void OnParentLocationChanged()
	{
		locationContextId = null;
		if (seasonOverride == null || seasonOverride.IsValueCreated)
		{
			seasonOverride = new Lazy<Season?>(LoadSeasonOverride);
		}
	}

	/// <summary>Update when the building containing this location is upgraded, if applicable.</summary>
	/// <param name="building">The building containing this location.</param>
	public virtual void OnParentBuildingUpgraded(Building building)
	{
	}

	/// <summary>Update when this location is removed from the game (e.g. a mine level that was unloaded).</summary>
	public virtual void OnRemoved()
	{
		for (int num = characters.Count - 1; num >= 0; num--)
		{
			characters[num].OnLocationRemoved();
		}
	}

	/// <summary>Handle an object added to the location.</summary>
	/// <param name="tile">The tile position.</param>
	/// <param name="obj">The object that was added.</param>
	protected virtual void OnObjectAdded(Vector2 tile, Object obj)
	{
		obj.Location = this;
		obj.TileLocation = tile;
	}

	/// <summary>Handle a resource clump added to the location.</summary>
	/// <param name="obj">The resource clump that was added.</param>
	public virtual void OnResourceClumpAdded(ResourceClump resourceClump)
	{
		resourceClump.Location = this;
		resourceClump.OnAddedToLocation(this, resourceClump.Tile);
	}

	/// <summary>Handle a resource clump removed from the location.</summary>
	/// <param name="tile">The tile position.</param>
	/// <param name="obj">The resource clump that was removed.</param>
	public virtual void OnResourceClumpRemoved(ResourceClump resourceClump)
	{
		resourceClump.Location = null;
	}

	/// <summary>Handle a terrain feature added to the location.</summary>
	/// <param name="tile">The tile position.</param>
	/// <param name="obj">The terrain feature that was added.</param>
	public virtual void OnTerrainFeatureAdded(TerrainFeature feature, Vector2 location)
	{
		if (feature == null)
		{
			return;
		}
		if (!(feature is Flooring flooring))
		{
			if (feature is HoeDirt hoeDirt)
			{
				hoeDirt.OnAdded(this, location);
			}
		}
		else
		{
			flooring.OnAdded(this, location);
		}
		feature.Location = this;
		feature.Tile = location;
		feature.OnAddedToLocation(this, location);
		UpdateTerrainFeatureUpdateSubscription(feature);
	}

	/// <summary>Handle a terrain feature removed from the location.</summary>
	/// <param name="tile">The tile position.</param>
	/// <param name="obj">The terrain feature that was removed.</param>
	public virtual void OnTerrainFeatureRemoved(TerrainFeature feature)
	{
		if (feature == null)
		{
			return;
		}
		if (!(feature is Flooring flooring))
		{
			if (!(feature is HoeDirt hoeDirt))
			{
				if (feature is LargeTerrainFeature largeTerrainFeature)
				{
					largeTerrainFeature.onDestroy();
				}
			}
			else
			{
				hoeDirt.OnRemoved();
			}
		}
		else
		{
			flooring.OnRemoved();
		}
		if (feature.NeedsUpdate)
		{
			_activeTerrainFeatures.Remove(feature);
		}
		feature.Location = null;
	}

	public virtual void UpdateTerrainFeatureUpdateSubscription(TerrainFeature feature)
	{
		if (feature.NeedsUpdate)
		{
			_activeTerrainFeatures.Add(feature);
		}
		else
		{
			_activeTerrainFeatures.Remove(feature);
		}
	}

	/// <summary>Get the season which currently applies to this location as a numeric index.</summary>
	/// <remarks>Most code should use <see cref="M:StardewValley.GameLocation.GetSeason" /> instead.</remarks>
	public int GetSeasonIndex()
	{
		return (int)GetSeason();
	}

	/// <summary>Read the override season from the map or location context.</summary>
	private Season? LoadSeasonOverride()
	{
		if (map == null && mapPath.Value != null)
		{
			reloadMap();
		}
		if (map != null && map.Properties.TryGetValue("SeasonOverride", out var value) && !string.IsNullOrWhiteSpace(value))
		{
			if (Utility.TryParseEnum<Season>(value, out var parsed))
			{
				return parsed;
			}
			Game1.log.Error($"Unable to read SeasonOverride map property value '{value}' for location '{NameOrUniqueName}', not a valid season name.");
		}
		return GetLocationContext()?.SeasonOverride;
	}

	/// <summary>Get the season which currently applies to this location.</summary>
	public Season GetSeason()
	{
		return seasonOverride.Value ?? GetParentLocation()?.GetSeason() ?? Game1.season;
	}

	/// <summary>Get the season which currently applies to this location as a string.</summary>
	/// <remarks>Most code should use <see cref="M:StardewValley.GameLocation.GetSeason" /> instead.</remarks>
	public string GetSeasonKey()
	{
		return Utility.getSeasonKey(GetSeason());
	}

	/// <summary>Get whether it's spring in this location's context.</summary>
	/// <remarks>This is a shortcut for convenience. When checking multiple season, consider caching the result from <see cref="M:StardewValley.GameLocation.GetSeason" /> instead.</remarks>
	public bool IsSpringHere()
	{
		return GetSeason() == Season.Spring;
	}

	/// <summary>Get whether it's summer in this location's context.</summary>
	/// <inheritdoc cref="M:StardewValley.GameLocation.IsSpringHere" path="/remarks" />
	public bool IsSummerHere()
	{
		return GetSeason() == Season.Summer;
	}

	/// <summary>Get whether it's fall in this location's context.</summary>
	/// <inheritdoc cref="M:StardewValley.GameLocation.IsSpringHere" path="/remarks" />
	public bool IsFallHere()
	{
		return GetSeason() == Season.Fall;
	}

	/// <summary>Get whether it's winter in this location's context.</summary>
	/// <inheritdoc cref="M:StardewValley.GameLocation.IsSpringHere" path="/remarks" />
	public bool IsWinterHere()
	{
		return GetSeason() == Season.Winter;
	}

	/// <summary>Get the weather which applies in this location's context.</summary>
	public LocationWeather GetWeather()
	{
		return Game1.netWorldState.Value.GetWeatherForLocation(GetLocationContextId());
	}

	/// <summary>Get whether it's raining in this location's context (regardless of whether the player is currently indoors and sheltered from the rain).</summary>
	public bool IsRainingHere()
	{
		return GetWeather().IsRaining;
	}

	/// <summary>Get whether it's green raining in this location's context (regardless of whether the player is currently indoors and sheltered from the green rain).</summary>
	public bool IsGreenRainingHere()
	{
		if (IsRainingHere())
		{
			return GetWeather().IsGreenRain;
		}
		return false;
	}

	/// <summary>Get whether it's storming in this location's context (regardless of whether the player is currently indoors and sheltered from the storm).</summary>
	public bool IsLightningHere()
	{
		return GetWeather().IsLightning;
	}

	/// <summary>Get whether it's snowing in this location's context (regardless of whether the player is currently indoors and sheltered from the snow).</summary>
	public bool IsSnowingHere()
	{
		return GetWeather().IsSnowing;
	}

	/// <summary>Get whether it's blowing debris like leaves in this location's context (regardless of whether the player is currently indoors and sheltered from the wind).</summary>
	public bool IsDebrisWeatherHere()
	{
		return GetWeather().IsDebrisWeather;
	}

	/// <summary>Get whether a location name matches the pattern used by temporary locations for events or minigames.</summary>
	/// <param name="name">The location name to check.</param>
	public static bool IsTemporaryName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (!name.StartsWith("Temp", StringComparison.Ordinal) && !(name == "fishingGame"))
		{
			return name == "tent";
		}
		return true;
	}

	private void updateFishSplashAnimation()
	{
		if (fishSplashPoint.Value == Point.Zero)
		{
			fishSplashAnimation = null;
			return;
		}
		fishSplashAnimation = new TemporaryAnimatedSprite(51, new Vector2(fishSplashPoint.X * 64, fishSplashPoint.Y * 64), Color.White, 10, flipped: false, 80f, 999999)
		{
			layerDepth = (float)(fishSplashPoint.Y * 64 - 64 - 1) / 10000f
		};
	}

	private void updateOrePanAnimation()
	{
		if (orePanPoint.Value == Point.Zero)
		{
			orePanAnimation = null;
			return;
		}
		orePanAnimation = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(432, 1435, 16, 16), new Vector2(orePanPoint.X * 64 + 32, orePanPoint.Y * 64 + 32), flipped: false, 0f, Color.White)
		{
			totalNumberOfLoops = 9999999,
			interval = 100f,
			scale = 3f,
			animationLength = 6
		};
	}

	public GameLocation()
	{
		NetFields = new NetFields(NetFields.GetNameForInstance(this));
		farmers = new FarmerCollection(this);
		interiorDoors = new InteriorDoorDictionary(this);
		netAudio = new NetAudio(this);
		objects = new OverlaidDictionary(netObjects, overlayObjects);
		_appliedMapOverrides = new HashSet<string>();
		terrainFeatures.SetEqualityComparer(tilePositionComparer);
		netObjects.SetEqualityComparer(tilePositionComparer);
		objects.SetEqualityComparer(tilePositionComparer, ref netObjects, ref overlayObjects);
		seasonOverride = new Lazy<Season?>(LoadSeasonOverride);
		initNetFields();
	}

	public GameLocation(string mapPath, string name)
		: this()
	{
		this.mapPath.Set(mapPath);
		this.name.Value = name;
		if (name.Contains("Farm") || name.Contains("Coop") || name.Contains("Barn") || name.Equals("SlimeHutch"))
		{
			isFarm.Value = true;
		}
		if (name == "Greenhouse")
		{
			IsGreenhouse = true;
		}
		reloadMap();
		loadObjects();
	}

	/// <summary>Add the default buildings which should always exist on the farm, if missing.</summary>
	/// <param name="load">Whether to call <see cref="M:StardewValley.Buildings.Building.load" />. This should be true unless you'll be calling it separately.</param>
	public virtual void AddDefaultBuildings(bool load = true)
	{
	}

	/// <summary>Add a default building which should always exist on the farm, if it's missing.</summary>
	/// <param name="id">The building ID in <c>Data/Buildings</c>.</param>
	/// <param name="tile">The tile position at which to construct it.</param>
	/// <param name="load">Whether to call <see cref="M:StardewValley.Buildings.Building.load" />. This should be true unless you'll be calling it separately.</param>
	public virtual void AddDefaultBuilding(string id, Vector2 tile, bool load = true)
	{
		foreach (Building building2 in buildings)
		{
			if (building2.buildingType.Value == id)
			{
				return;
			}
		}
		Building building = Building.CreateInstanceFromId(id, tile);
		if (load)
		{
			building.load();
		}
		buildings.Add(building);
	}

	/// <summary>Play a sound for each online player in the location if they can hear it.</summary>
	/// <param name="audioName">The sound ID to play.</param>
	/// <param name="position">The tile position from which to play the sound, or <c>null</c> if it should be played throughout the location.</param>
	/// <param name="pitch">The pitch modifier to apply, or <c>null</c> to keep it as-is.</param>
	/// <param name="context">The source which triggered the sound.</param>
	public void playSound(string audioName, Vector2? position = null, int? pitch = null, SoundContext context = SoundContext.Default)
	{
		Game1.sounds.PlayAll(audioName, this, position, pitch, context);
	}

	/// <summary>Play a sound for the current player only, if they can hear it.</summary>
	/// <param name="audioName">The sound ID to play.</param>
	/// <param name="position">The tile position from which to play the sound, or <c>null</c> if not applicable.</param>
	/// <param name="pitch">The pitch modifier to apply, or <c>null</c> to keep it as-is.</param>
	/// <param name="context">The source which triggered the sound.</param>
	public void localSound(string audioName, Vector2? position = null, int? pitch = null, SoundContext context = SoundContext.Default)
	{
		Game1.sounds.PlayLocal(audioName, this, position, pitch, context, out var _);
	}

	protected virtual LocalizedContentManager getMapLoader()
	{
		if (isStructure.Value)
		{
			if (_structureMapLoader == null)
			{
				_structureMapLoader = Game1.game1.xTileContent.CreateTemporary();
			}
			return _structureMapLoader;
		}
		return Game1.game1.xTileContent;
	}

	/// <summary>Destroy any organic material like weeds or twigs, and send any player items to the lost and found. Used to clean up areas before map overrides.</summary>
	/// <param name="tile">The tile position to clean up.</param>
	public void cleanUpTileForMapOverride(Point tile)
	{
		cleanUpTileForMapOverride(tile, null);
	}

	/// <summary>Destroy any organic material like weeds or twigs, and send any player items to the lost and found. Used to clean up areas before map overrides.</summary>
	/// <param name="tile">The tile position to clean up.</param>
	/// <param name="exceptItemId">If set, an item on this spot won't be moved if its item ID matches this one.</param>
	public void cleanUpTileForMapOverride(Point tile, string exceptItemId)
	{
		Vector2 vector = Utility.PointToVector2(tile);
		Point tileCenterPoint = Utility.Vector2ToPoint(vector * new Vector2(64f) + new Vector2(32f, 32f));
		NetCollection<Item> lostAndFound = Game1.player.team.returnedDonations;
		if (Objects.TryGetValue(vector, out var value) && (exceptItemId == null || !ItemRegistry.HasItemId(value, exceptItemId)))
		{
			if (value != null && (value.HasBeenInInventory || (!value.isDebrisOrForage() && value.QualifiedItemId != "(O)590" && value.QualifiedItemId != "(O)SeedSpot")))
			{
				if (value is Chest chest)
				{
					foreach (Item item in chest.Items)
					{
						lostAndFound.Add(item);
					}
					chest.Items.Clear();
				}
				else if (value.readyForHarvest.Value && value.heldObject != null)
				{
					lostAndFound.Add(value.heldObject.Value);
					value.heldObject.Value = null;
				}
				lostAndFound.Add(value);
				Game1.player.team.newLostAndFoundItems.Value = true;
			}
			objects.Remove(vector);
		}
		furniture.RemoveWhere(delegate(Furniture item)
		{
			if (!item.GetBoundingBox().Contains(tileCenterPoint) || (exceptItemId != null && ItemRegistry.HasItemId(item, exceptItemId)))
			{
				return false;
			}
			if (item.heldObject.Value != null)
			{
				lostAndFound.Add(item.heldObject.Value);
				item.heldObject.Value = null;
			}
			lostAndFound.Add(item);
			return true;
		});
		terrainFeatures.Remove(vector);
		largeTerrainFeatures.RemoveWhere((LargeTerrainFeature feature) => feature.getBoundingBox().Contains(tileCenterPoint));
		resourceClumps.RemoveWhere((ResourceClump clump) => clump.getBoundingBox().Contains(tileCenterPoint));
	}

	public void ApplyMapOverride(Map override_map, string override_key, Microsoft.Xna.Framework.Rectangle? source_rect = null, Microsoft.Xna.Framework.Rectangle? dest_rect = null, Action<Point> perTileCustomAction = null)
	{
		if (_appliedMapOverrides.Contains(override_key))
		{
			return;
		}
		_appliedMapOverrides.Add(override_key);
		updateSeasonalTileSheets(override_map);
		Dictionary<TileSheet, TileSheet> dictionary = new Dictionary<TileSheet, TileSheet>();
		foreach (TileSheet tileSheet2 in override_map.TileSheets)
		{
			TileSheet tileSheet = map.GetTileSheet(tileSheet2.Id);
			string text = "";
			string text2 = "";
			if (tileSheet != null)
			{
				text = tileSheet.ImageSource;
			}
			if (text2 != null)
			{
				text2 = tileSheet2.ImageSource;
			}
			if (tileSheet == null || text2 != text)
			{
				tileSheet = new TileSheet(GetAddedMapOverrideTilesheetId(override_key, tileSheet2.Id), map, tileSheet2.ImageSource, tileSheet2.SheetSize, tileSheet2.TileSize);
				for (int i = 0; i < tileSheet2.TileCount; i++)
				{
					tileSheet.TileIndexProperties[i].CopyFrom(tileSheet2.TileIndexProperties[i]);
				}
				map.AddTileSheet(tileSheet);
			}
			else if (tileSheet.TileCount < tileSheet2.TileCount)
			{
				int tileCount = tileSheet.TileCount;
				tileSheet.SheetWidth = tileSheet2.SheetWidth;
				tileSheet.SheetHeight = tileSheet2.SheetHeight;
				for (int j = tileCount; j < tileSheet2.TileCount; j++)
				{
					tileSheet.TileIndexProperties[j].CopyFrom(tileSheet2.TileIndexProperties[j]);
				}
			}
			dictionary[tileSheet2] = tileSheet;
		}
		Dictionary<Layer, Layer> dictionary2 = new Dictionary<Layer, Layer>();
		int num = 0;
		int num2 = 0;
		for (int k = 0; k < override_map.Layers.Count; k++)
		{
			num = Math.Max(num, override_map.Layers[k].LayerWidth);
			num2 = Math.Max(num2, override_map.Layers[k].LayerHeight);
		}
		if (!source_rect.HasValue)
		{
			source_rect = new Microsoft.Xna.Framework.Rectangle(0, 0, num, num2);
		}
		num = 0;
		num2 = 0;
		for (int l = 0; l < map.Layers.Count; l++)
		{
			num = Math.Max(num, map.Layers[l].LayerWidth);
			num2 = Math.Max(num2, map.Layers[l].LayerHeight);
		}
		bool flag = false;
		for (int m = 0; m < override_map.Layers.Count; m++)
		{
			Layer layer = map.GetLayer(override_map.Layers[m].Id);
			if (layer == null)
			{
				layer = new Layer(override_map.Layers[m].Id, map, new Size(num, num2), override_map.Layers[m].TileSize);
				map.AddLayer(layer);
				flag = true;
			}
			dictionary2[override_map.Layers[m]] = layer;
		}
		if (flag)
		{
			SortLayers();
		}
		if (!dest_rect.HasValue)
		{
			dest_rect = new Microsoft.Xna.Framework.Rectangle(0, 0, num, num2);
		}
		int x = source_rect.Value.X;
		int y = source_rect.Value.Y;
		int x2 = dest_rect.Value.X;
		int y2 = dest_rect.Value.Y;
		for (int n = 0; n < source_rect.Value.Width; n++)
		{
			for (int num3 = 0; num3 < source_rect.Value.Height; num3++)
			{
				Point point = new Point(x + n, y + num3);
				Point obj = new Point(x2 + n, y2 + num3);
				perTileCustomAction?.Invoke(obj);
				bool flag2 = false;
				for (int num4 = 0; num4 < override_map.Layers.Count; num4++)
				{
					Layer layer2 = override_map.Layers[num4];
					Layer layer3 = dictionary2[layer2];
					if (layer3 == null || obj.X >= layer3.LayerWidth || obj.Y >= layer3.LayerHeight || (!flag2 && override_map.Layers[num4].Tiles[point.X, point.Y] == null))
					{
						continue;
					}
					flag2 = true;
					if (point.X >= layer2.LayerWidth || point.Y >= layer2.LayerHeight)
					{
						continue;
					}
					if (layer2.Tiles[point.X, point.Y] == null)
					{
						layer3.Tiles[obj.X, obj.Y] = null;
						continue;
					}
					Tile tile = layer2.Tiles[point.X, point.Y];
					Tile tile2 = null;
					if (!(tile is StaticTile))
					{
						if (tile is AnimatedTile animatedTile)
						{
							StaticTile[] array = new StaticTile[animatedTile.TileFrames.Length];
							for (int num5 = 0; num5 < animatedTile.TileFrames.Length; num5++)
							{
								StaticTile staticTile = animatedTile.TileFrames[num5];
								array[num5] = new StaticTile(layer3, dictionary[staticTile.TileSheet], staticTile.BlendMode, staticTile.TileIndex);
							}
							tile2 = new AnimatedTile(layer3, array, animatedTile.FrameInterval);
						}
					}
					else
					{
						tile2 = new StaticTile(layer3, dictionary[tile.TileSheet], tile.BlendMode, tile.TileIndex);
					}
					tile2?.Properties.CopyFrom(tile.Properties);
					layer3.Tiles[obj.X, obj.Y] = tile2;
				}
			}
		}
		map.LoadTileSheets(Game1.mapDisplayDevice);
		if (Game1.IsMasterGame || IsTemporary)
		{
			_mapSeatsDirty = true;
		}
	}

	/// <summary>Get the generated tilesheet ID for a tilesheet added to the map via <see cref="M:StardewValley.GameLocation.ApplyMapOverride(xTile.Map,System.String,System.Nullable{Microsoft.Xna.Framework.Rectangle},System.Nullable{Microsoft.Xna.Framework.Rectangle},System.Action{Microsoft.Xna.Framework.Point})" />.</summary>
	/// <param name="overrideKey">The map override ID.</param>
	/// <param name="tilesheetId">The tilesheet ID in the applied override map.</param>
	/// <remarks>Note that this tilesheet ID is only used when adding a new tilesheet to the map. If the tilesheet already exists in the base map with the same asset path, it's reused as-is.</remarks>
	public static string GetAddedMapOverrideTilesheetId(string overrideKey, string tilesheetId)
	{
		return $"{"zzzzz"}_{overrideKey}_{tilesheetId}";
	}

	public virtual bool RunLocationSpecificEventCommand(Event current_event, string command_string, bool first_run, params string[] args)
	{
		return true;
	}

	public bool hasActiveFireplace()
	{
		for (int i = 0; i < furniture.Count; i++)
		{
			if (furniture[i].furniture_type.Value == 14 && furniture[i].isOn.Value)
			{
				return true;
			}
		}
		return false;
	}

	public void ApplyMapOverride(string map_name, Microsoft.Xna.Framework.Rectangle? source_rect = null, Microsoft.Xna.Framework.Rectangle? destination_rect = null)
	{
		if (!_appliedMapOverrides.Contains(map_name))
		{
			Map override_map = Game1.game1.xTileContent.Load<Map>("Maps\\" + map_name);
			ApplyMapOverride(override_map, map_name, source_rect, destination_rect);
		}
	}

	public void ApplyMapOverride(string map_name, string override_key_name, Microsoft.Xna.Framework.Rectangle? source_rect = null, Microsoft.Xna.Framework.Rectangle? destination_rect = null)
	{
		if (!_appliedMapOverrides.Contains(override_key_name))
		{
			Map override_map = Game1.game1.xTileContent.Load<Map>("Maps\\" + map_name);
			ApplyMapOverride(override_map, override_key_name, source_rect, destination_rect);
		}
	}

	public virtual void UpdateMapSeats()
	{
		_mapSeatsDirty = false;
		if (!Game1.IsMasterGame && !IsTemporary)
		{
			return;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		Dictionary<string, string> dictionary2 = DataLoader.ChairTiles(Game1.content);
		mapSeats.Clear();
		Layer layer = map.GetLayer("Buildings");
		if (layer == null)
		{
			return;
		}
		for (int i = 0; i < layer.LayerWidth; i++)
		{
			for (int j = 0; j < layer.LayerHeight; j++)
			{
				Tile tile = layer.Tiles[i, j];
				if (tile == null)
				{
					continue;
				}
				string text = Path.GetFileNameWithoutExtension(tile.TileSheet.ImageSource);
				if (dictionary.TryGetValue(text, out var value))
				{
					text = value;
				}
				else
				{
					if (text.StartsWith("summer_") || text.StartsWith("winter_") || text.StartsWith("fall_"))
					{
						text = "spring_" + text.Substring(text.IndexOf('_') + 1);
					}
					dictionary[text] = text;
				}
				int sheetWidth = tile.TileSheet.SheetWidth;
				int num = tile.TileIndex % sheetWidth;
				int num2 = tile.TileIndex / sheetWidth;
				string key = text + "/" + num + "/" + num2;
				if (dictionary2.TryGetValue(key, out var value2))
				{
					MapSeat mapSeat = MapSeat.FromData(value2, i, j);
					if (mapSeat != null)
					{
						mapSeats.Add(mapSeat);
					}
				}
			}
		}
	}

	public virtual void SortLayers()
	{
		backgroundLayers.Clear();
		buildingLayers.Clear();
		frontLayers.Clear();
		alwaysFrontLayers.Clear();
		Dictionary<string, List<KeyValuePair<Layer, int>>> dictionary = new Dictionary<string, List<KeyValuePair<Layer, int>>>();
		dictionary["Back"] = backgroundLayers;
		dictionary["Buildings"] = buildingLayers;
		dictionary["Front"] = frontLayers;
		dictionary["AlwaysFront"] = alwaysFrontLayers;
		foreach (Layer layer in map.Layers)
		{
			foreach (string key in dictionary.Keys)
			{
				if (layer.Id.StartsWith(key))
				{
					int result = 0;
					string text = layer.Id.Substring(key.Length);
					if (text.Length <= 0 || int.TryParse(text, out result))
					{
						dictionary[key].Add(new KeyValuePair<Layer, int>(layer, result));
						break;
					}
				}
			}
		}
		foreach (List<KeyValuePair<Layer, int>> value in dictionary.Values)
		{
			value.Sort((KeyValuePair<Layer, int> a, KeyValuePair<Layer, int> b) => a.Value.CompareTo(b.Value));
		}
	}

	public virtual void OnMapLoad(Map map)
	{
	}

	public void loadMap(string mapPath, bool force_reload = false)
	{
		if (force_reload)
		{
			LocalizedContentManager localizedContentManager = Program.gamePtr.CreateContentManager(Game1.content.ServiceProvider, Game1.content.RootDirectory);
			map = localizedContentManager.Load<Map>(mapPath);
			localizedContentManager.Unload();
			InvalidateCachedMultiplayerMap(Game1.multiplayer.cachedMultiplayerMaps);
		}
		else if (!ApplyCachedMultiplayerMap(Game1.multiplayer.cachedMultiplayerMaps, mapPath))
		{
			map = getMapLoader().Load<Map>(mapPath);
		}
		loadedMapPath = mapPath;
		OnMapLoad(map);
		SortLayers();
		if (map.Properties.ContainsKey("Outdoors"))
		{
			isOutdoors.Value = true;
		}
		if (map.Properties.ContainsKey("IsFarm"))
		{
			isFarm.Value = true;
		}
		if (map.Properties.ContainsKey("IsGreenhouse"))
		{
			isGreenhouse.Value = true;
		}
		if (HasMapPropertyWithValue("forceLoadPathLayerLights"))
		{
			forceLoadPathLayerLights = true;
		}
		if (HasMapPropertyWithValue("TreatAsOutdoors"))
		{
			treatAsOutdoors.Value = true;
		}
		updateSeasonalTileSheets(map);
		map.LoadTileSheets(Game1.mapDisplayDevice);
		if (Game1.IsMasterGame || IsTemporary)
		{
			_mapSeatsDirty = true;
		}
		if ((isOutdoors.Value || HasMapPropertyWithValue("indoorWater") || this is Sewer || this is Submarine) && !(this is Desert))
		{
			waterTiles = new WaterTiles(map.Layers[0].LayerWidth, map.Layers[0].LayerHeight);
			bool flag = false;
			for (int i = 0; i < map.Layers[0].LayerWidth; i++)
			{
				for (int j = 0; j < map.Layers[0].LayerHeight; j++)
				{
					string text = doesTileHaveProperty(i, j, "Water", "Back");
					if (text != null)
					{
						flag = true;
						if (text == "I")
						{
							waterTiles.waterTiles[i, j] = new WaterTiles.WaterTileData(is_water: true, is_visible: false);
						}
						else
						{
							waterTiles[i, j] = true;
						}
					}
				}
			}
			if (!flag)
			{
				waterTiles = null;
			}
		}
		if (isOutdoors.Value)
		{
			critters = new List<Critter>();
		}
		loadLights();
	}

	public virtual void HandleGrassGrowth(int dayOfMonth)
	{
		if (dayOfMonth == 1)
		{
			if (this is Farm || HasMapPropertyWithValue("ClearEmptyDirtOnNewMonth"))
			{
				terrainFeatures.RemoveWhere((KeyValuePair<Vector2, TerrainFeature> pair) => pair.Value is HoeDirt { crop: null } && Game1.random.NextDouble() < 0.8);
			}
			if (this is Farm || HasMapPropertyWithValue("SpawnDebrisOnNewMonth"))
			{
				spawnWeedsAndStones(20, weedsOnly: false, spawnFromOldWeeds: false);
			}
			if (Game1.IsSpring && Game1.stats.DaysPlayed > 1)
			{
				if (this is Farm || HasMapPropertyWithValue("SpawnDebrisOnNewYear"))
				{
					spawnWeedsAndStones(40, weedsOnly: false, spawnFromOldWeeds: false);
					spawnWeedsAndStones(40, weedsOnly: true, spawnFromOldWeeds: false);
				}
				if (this is Farm || HasMapPropertyWithValue("SpawnRandomGrassOnNewYear"))
				{
					for (int num = 0; num < 15; num++)
					{
						int num2 = Game1.random.Next(map.DisplayWidth / 64);
						int num3 = Game1.random.Next(map.DisplayHeight / 64);
						Vector2 vector = new Vector2(num2, num3);
						objects.TryGetValue(vector, out var value);
						if (value == null && doesTileHaveProperty(num2, num3, "Diggable", "Back") != null && !IsNoSpawnTile(vector) && isTileLocationOpen(new Location(num2, num3)) && !IsTileOccupiedBy(vector) && !isWaterTile(num2, num3))
						{
							int which = 1;
							if (Game1.GetFarmTypeID() == "MeadowlandsFarm" && Game1.random.NextDouble() < 0.2)
							{
								which = 7;
							}
							terrainFeatures.Add(vector, new Grass(which, 4));
						}
					}
					growWeedGrass(40);
				}
				if (HasMapPropertyWithValue("SpawnGrassFromPathsOnNewYear"))
				{
					Layer layer = map.GetLayer("Paths");
					if (layer != null)
					{
						for (int num4 = 0; num4 < layer.LayerWidth; num4++)
						{
							for (int num5 = 0; num5 < layer.LayerHeight; num5++)
							{
								Vector2 vector2 = new Vector2(num4, num5);
								objects.TryGetValue(vector2, out var value2);
								if (value2 == null && getTileIndexAt(num4, num5, "Paths") == 22 && isTileLocationOpen(vector2) && !IsTileOccupiedBy(vector2))
								{
									terrainFeatures.Add(vector2, new Grass(1, 4));
								}
							}
						}
					}
				}
			}
		}
		if ((this is Farm || HasMapPropertyWithValue("EnableGrassSpread")) && (!IsWinterHere() || HasMapPropertyWithValue("AllowGrassGrowInWinter")))
		{
			growWeedGrass(1);
		}
	}

	public void reloadMap()
	{
		if (mapPath.Value != null)
		{
			loadMap(mapPath.Value);
		}
		else
		{
			map = null;
		}
		loadedMapPath = mapPath.Value;
	}

	public virtual bool canSlimeMateHere()
	{
		return true;
	}

	public virtual bool canSlimeHatchHere()
	{
		return true;
	}

	public void addCharacter(NPC character)
	{
		characters.Add(character);
	}

	public static Microsoft.Xna.Framework.Rectangle getSourceRectForObject(int tileIndex)
	{
		return new Microsoft.Xna.Framework.Rectangle(tileIndex * 16 % Game1.objectSpriteSheet.Width, tileIndex * 16 / Game1.objectSpriteSheet.Width * 16, 16, 16);
	}

	public Warp isCollidingWithWarp(Microsoft.Xna.Framework.Rectangle position, Character character)
	{
		if (ignoreWarps)
		{
			return null;
		}
		foreach (Warp warp in warps)
		{
			if ((!(character is NPC) && warp.npcOnly.Value) || (warp.X != (int)Math.Floor((double)position.Left / 64.0) && warp.X != (int)Math.Floor((double)position.Right / 64.0)) || (warp.Y != (int)Math.Floor((double)position.Top / 64.0) && warp.Y != (int)Math.Floor((double)position.Bottom / 64.0)))
			{
				continue;
			}
			string targetName = warp.TargetName;
			if (!(targetName == "BoatTunnel"))
			{
				if (targetName == "VolcanoEntrance")
				{
					return new Warp(warp.X, warp.Y, VolcanoDungeon.GetLevelName(0), warp.TargetX, warp.TargetY, flipFarmer: false);
				}
			}
			else if (character is NPC)
			{
				return new Warp(warp.X, warp.Y, "IslandSouth", 17, 43, flipFarmer: false);
			}
			return warp;
		}
		return null;
	}

	public Warp isCollidingWithWarpOrDoor(Microsoft.Xna.Framework.Rectangle position, Character character = null)
	{
		Warp warp = isCollidingWithWarp(position, character);
		if (warp == null)
		{
			warp = isCollidingWithDoors(position, character);
		}
		return warp;
	}

	public virtual Warp isCollidingWithDoors(Microsoft.Xna.Framework.Rectangle position, Character character = null)
	{
		for (int i = 0; i < 4; i++)
		{
			Vector2 cornersOfThisRectangle = Utility.getCornersOfThisRectangle(ref position, i);
			Point point = new Point((int)cornersOfThisRectangle.X / 64, (int)cornersOfThisRectangle.Y / 64);
			foreach (KeyValuePair<Point, string> pair in doors.Pairs)
			{
				Point key = pair.Key;
				if (point == key)
				{
					Warp warpFromDoor = getWarpFromDoor(key, character);
					if (warpFromDoor != null)
					{
						return warpFromDoor;
					}
				}
			}
			foreach (Building building in buildings)
			{
				if (!building.HasIndoors())
				{
					continue;
				}
				Point pointForHumanDoor = building.getPointForHumanDoor();
				if (point == pointForHumanDoor)
				{
					Warp warpFromDoor2 = getWarpFromDoor(pointForHumanDoor, character);
					if (warpFromDoor2 != null)
					{
						return warpFromDoor2;
					}
				}
			}
		}
		return null;
	}

	public virtual Warp getWarpFromDoor(Point door, Character character = null)
	{
		foreach (Building building in buildings)
		{
			if (door == building.getPointForHumanDoor())
			{
				GameLocation indoors = building.GetIndoors();
				if (indoors != null)
				{
					return new Warp(door.X, door.Y, indoors.NameOrUniqueName, indoors.warps[0].X, indoors.warps[0].Y - 1, flipFarmer: false);
				}
			}
		}
		string[] tilePropertySplitBySpaces = GetTilePropertySplitBySpaces("Action", "Buildings", door.X, door.Y);
		string text = ArgUtility.Get(tilePropertySplitBySpaces, 0, "");
		switch (text)
		{
		case "WarpCommunityCenter":
			return new Warp(door.X, door.Y, "CommunityCenter", 32, 23, flipFarmer: false);
		case "Warp_Sunroom_Door":
			return new Warp(door.X, door.Y, "Sunroom", 5, 13, flipFarmer: false);
		case "WarpBoatTunnel":
			if (!(character is NPC))
			{
				return new Warp(door.X, door.Y, "BoatTunnel", 6, 11, flipFarmer: false);
			}
			return new Warp(door.X, door.Y, "IslandSouth", 17, 43, flipFarmer: false);
		case "WarpMensLocker":
		case "LockedDoorWarp":
		case "Warp":
		case "WarpWomensLocker":
		{
			if (!ArgUtility.TryGetPoint(tilePropertySplitBySpaces, 1, out var value, out var error, "Point tile") || !ArgUtility.TryGet(tilePropertySplitBySpaces, 3, out var value2, out error, allowBlank: true, "string locationName"))
			{
				LogTileActionError(tilePropertySplitBySpaces, door.X, door.Y, error);
				return null;
			}
			if (!(value2 == "BoatTunnel") || !(character is NPC))
			{
				return new Warp(door.X, door.Y, value2, value.X, value.Y, flipFarmer: false);
			}
			return new Warp(door.X, door.Y, "IslandSouth", 17, 43, flipFarmer: false);
		}
		default:
			if (text.Contains("Warp"))
			{
				Game1.log.Warn($"Door in {NameOrUniqueName} ({door}) has unknown warp property '{string.Join(" ", tilePropertySplitBySpaces)}', parsing with legacy logic.");
				goto case "WarpMensLocker";
			}
			return null;
		}
	}

	/// <summary>Get the first warp which the player can use to leave the location, accounting for any gender restrictions and NPC-only flags if possible.</summary>
	public Warp GetFirstPlayerWarp()
	{
		Warp warp = null;
		foreach (Warp warp2 in warps)
		{
			if (!warp2.npcOnly.Value)
			{
				if (!WarpPathfindingCache.GenderRestrictions.TryGetValue(warp2.TargetName, out var value) || value == Game1.player.Gender)
				{
					return warp2;
				}
				if (warp == null)
				{
					warp = warp2;
				}
			}
		}
		return warp ?? warps.FirstOrDefault();
	}

	public void addResourceClumpAndRemoveUnderlyingTerrain(int resourceClumpIndex, int width, int height, Vector2 tile)
	{
		removeObjectsAndSpawned((int)tile.X, (int)tile.Y, width, height);
		resourceClumps.Add(new ResourceClump(resourceClumpIndex, width, height, tile));
	}

	public virtual bool canFishHere()
	{
		return true;
	}

	/// <summary>Get whether a player can resume in this location after waking up, instead of being warped home.</summary>
	/// <param name="who">The player who's waking up here.</param>
	/// <param name="tile">The tile at which they're waking up, or <c>null</c> to use <see cref="F:StardewValley.Farmer.lastSleepPoint" />.</param>
	public virtual bool CanWakeUpHere(Farmer who, Point? tile = null)
	{
		Point point = tile ?? who.lastSleepPoint.Value;
		bool parsed;
		if (!BedFurniture.IsBedHere(this, point.X, point.Y) && !who.sleptInTemporaryBed.Value && !(this is IslandFarmHouse))
		{
			return TryGetMapPropertyAs("AllowWakeUpWithoutBed", out parsed, required: false) && parsed;
		}
		return true;
	}

	public virtual bool CanRefillWateringCanOnTile(int tileX, int tileY)
	{
		Vector2 tile = new Vector2(tileX, tileY);
		Building buildingAt = getBuildingAt(tile);
		if (buildingAt != null && buildingAt.CanRefillWateringCan())
		{
			return true;
		}
		if (!isWaterTile(tileX, tileY) && doesTileHaveProperty(tileX, tileY, "WaterSource", "Back") == null)
		{
			if (!isOutdoors.Value && doesTileHaveProperty(tileX, tileY, "Action", "Buildings") == "kitchen")
			{
				if (getTileIndexAt(tileX, tileY, "Buildings", "untitled tile sheet") != 172)
				{
					return getTileIndexAt(tileX, tileY, "Buildings", "untitled tile sheet") == 257;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public virtual bool isTileBuildingFishable(int tileX, int tileY)
	{
		Vector2 tile = new Vector2(tileX, tileY);
		foreach (Building building in buildings)
		{
			if (building.isTileFishable(tile))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool isTileFishable(int tileX, int tileY)
	{
		if (isTileBuildingFishable(tileX, tileY))
		{
			return true;
		}
		if (!isWaterTile(tileX, tileY) || doesTileHaveProperty(tileX, tileY, "NoFishing", "Back") != null || hasTileAt(tileX, tileY, "Buildings"))
		{
			return doesTileHaveProperty(tileX, tileY, "Water", "Buildings") != null;
		}
		return true;
	}

	public bool isFarmerCollidingWithAnyCharacter()
	{
		if (characters.Count > 0)
		{
			Microsoft.Xna.Framework.Rectangle boundingBox = Game1.player.GetBoundingBox();
			foreach (NPC character in characters)
			{
				if (character != null && boundingBox.Intersects(character.GetBoundingBox()))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool isCollidingPosition(Microsoft.Xna.Framework.Rectangle position, xTile.Dimensions.Rectangle viewport, Character character)
	{
		return isCollidingPosition(position, viewport, character is Farmer, 0, glider: false, character, pathfinding: false);
	}

	public virtual bool isCollidingPosition(Microsoft.Xna.Framework.Rectangle position, xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character)
	{
		return isCollidingPosition(position, viewport, character is Farmer, damagesFarmer, glider, character, pathfinding: false);
	}

	protected bool _TestCornersWorld(int top, int bottom, int left, int right, Func<int, int, bool> action)
	{
		if (action(right, top))
		{
			return true;
		}
		if (action(right, bottom))
		{
			return true;
		}
		if (action(left, top))
		{
			return true;
		}
		if (action(left, bottom))
		{
			return true;
		}
		return false;
	}

	protected bool _TestCornersTiles(Vector2 top_right, Vector2 top_left, Vector2 bottom_right, Vector2 bottom_left, Vector2 top_mid, Vector2 bottom_mid, Vector2? player_top_right, Vector2? player_top_left, Vector2? player_bottom_right, Vector2? player_bottom_left, Vector2? player_top_mid, Vector2? player_bottom_mid, bool bigger_than_tile, Func<Vector2, bool> action)
	{
		_visitedCollisionTiles.Clear();
		if (player_top_right != top_right && _visitedCollisionTiles.Add(top_right) && action(top_right))
		{
			return true;
		}
		if (player_top_left != top_left && _visitedCollisionTiles.Add(top_left) && action(top_left))
		{
			return true;
		}
		if (bottom_left != player_bottom_left && _visitedCollisionTiles.Add(bottom_left) && action(bottom_left))
		{
			return true;
		}
		if (bottom_right != player_bottom_right && _visitedCollisionTiles.Add(bottom_right) && action(bottom_right))
		{
			return true;
		}
		if (bigger_than_tile)
		{
			if (player_top_mid != top_mid && _visitedCollisionTiles.Add(top_mid) && action(top_mid))
			{
				return true;
			}
			if (player_bottom_mid != bottom_mid && _visitedCollisionTiles.Add(bottom_mid) && action(bottom_mid))
			{
				return true;
			}
		}
		return false;
	}

	public Furniture GetFurnitureAt(Vector2 tile_position)
	{
		Point value = new Point
		{
			X = (int)((float)(int)tile_position.X + 0.5f) * 64,
			Y = (int)((float)(int)tile_position.Y + 0.5f) * 64
		};
		foreach (Furniture item in furniture)
		{
			if (!item.isPassable() && item.GetBoundingBox().Contains(value))
			{
				return item;
			}
		}
		foreach (Furniture item2 in furniture)
		{
			if (item2.isPassable() && item2.GetBoundingBox().Contains(value))
			{
				return item2;
			}
		}
		return null;
	}

	public virtual Microsoft.Xna.Framework.Rectangle GetBuildableRectangle()
	{
		if (!_buildableTileRect.HasValue)
		{
			_buildableTileRect = (TryGetMapPropertyAs("ValidBuildRect", out Microsoft.Xna.Framework.Rectangle parsed, required: false) ? parsed : Microsoft.Xna.Framework.Rectangle.Empty);
			_looserBuildRestrictions = HasMapPropertyWithValue("LooserBuildRestrictions");
		}
		return _buildableTileRect.Value;
	}

	public virtual bool IsBuildableLocation()
	{
		if (HasMapPropertyWithValue("CanBuildHere"))
		{
			if (!Game1.multiplayer.isAlwaysActiveLocation(this))
			{
				if (!showedBuildableButNotAlwaysActiveWarning)
				{
					Game1.log.Warn($"Location {NameOrUniqueName} has the CanBuildHere map property set, but its {"AlwaysActive"} option is disabled, so building is disabled here.");
					showedBuildableButNotAlwaysActiveWarning = true;
				}
				return false;
			}
			string mapProperty = getMapProperty("BuildConditions");
			if (string.IsNullOrEmpty(mapProperty) || GameStateQuery.CheckConditions(mapProperty, this))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Get whether a pixel area is fully outside the bounds of the map.</summary>
	/// <param name="pixelPosition">The pixel position.</param>
	public virtual bool IsOutOfBounds(Microsoft.Xna.Framework.Rectangle pixelPosition)
	{
		if (pixelPosition.Right < 0 || pixelPosition.Bottom < 0)
		{
			return true;
		}
		Layer layer = map.Layers[0];
		if (pixelPosition.X <= layer.DisplayWidth)
		{
			return pixelPosition.Top > layer.DisplayHeight;
		}
		return true;
	}

	public virtual bool isCollidingPosition(Microsoft.Xna.Framework.Rectangle position, xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character, bool pathfinding, bool projectile = false, bool ignoreCharacterRequirement = false, bool skipCollisionEffects = false)
	{
		bool flag = Game1.eventUp;
		if (flag && Game1.CurrentEvent != null && !Game1.CurrentEvent.ignoreObjectCollisions)
		{
			flag = false;
		}
		updateMap();
		if (IsOutOfBounds(position))
		{
			if (isFarmer && Game1.eventUp)
			{
				bool? flag2 = currentEvent?.isFestival;
				if (flag2.HasValue && flag2 == true && currentEvent.checkForCollision(position, (character as Farmer) ?? Game1.player))
				{
					return true;
				}
			}
			return false;
		}
		if (character == null && !ignoreCharacterRequirement)
		{
			return true;
		}
		Vector2 vector = new Vector2(position.Right / 64, position.Top / 64);
		Vector2 vector2 = new Vector2(position.Left / 64, position.Top / 64);
		Vector2 vector3 = new Vector2(position.Right / 64, position.Bottom / 64);
		Vector2 vector4 = new Vector2(position.Left / 64, position.Bottom / 64);
		bool bigger_than_tile = position.Width > 64;
		Vector2 bottom_mid = new Vector2(position.Center.X / 64, position.Bottom / 64);
		Vector2 top_mid = new Vector2(position.Center.X / 64, position.Top / 64);
		BoundingBoxGroup passableTiles = null;
		Farmer farmer = character as Farmer;
		Microsoft.Xna.Framework.Rectangle? rectangle;
		if (farmer != null)
		{
			isFarmer = true;
			rectangle = farmer.GetBoundingBox();
			passableTiles = farmer.TemporaryPassableTiles;
		}
		else
		{
			farmer = null;
			isFarmer = false;
			rectangle = null;
		}
		Vector2? player_top_right = null;
		Vector2? player_top_left = null;
		Vector2? player_bottom_right = null;
		Vector2? player_bottom_left = null;
		Vector2? player_bottom_mid = null;
		Vector2? player_top_mid = null;
		if (rectangle.HasValue)
		{
			player_top_right = new Vector2((rectangle.Value.Right - 1) / 64, rectangle.Value.Top / 64);
			player_top_left = new Vector2(rectangle.Value.Left / 64, rectangle.Value.Top / 64);
			player_bottom_right = new Vector2((rectangle.Value.Right - 1) / 64, (rectangle.Value.Bottom - 1) / 64);
			player_bottom_left = new Vector2(rectangle.Value.Left / 64, (rectangle.Value.Bottom - 1) / 64);
			player_bottom_mid = new Vector2(rectangle.Value.Center.X / 64, (rectangle.Value.Bottom - 1) / 64);
			player_top_mid = new Vector2(rectangle.Value.Center.X / 64, rectangle.Value.Top / 64);
		}
		if (farmer?.bridge != null && farmer.onBridge.Value && position.Right >= farmer.bridge.bridgeBounds.X && position.Left <= farmer.bridge.bridgeBounds.Right)
		{
			if (_TestCornersWorld(position.Top, position.Bottom, position.Left, position.Right, (int x, int y) => (y > farmer.bridge.bridgeBounds.Bottom || y < farmer.bridge.bridgeBounds.Top) ? true : false))
			{
				return true;
			}
			return false;
		}
		if (!glider)
		{
			if (character != null && animals.FieldDict.Count > 0 && !(character is FarmAnimal))
			{
				foreach (FarmAnimal value2 in animals.Values)
				{
					Microsoft.Xna.Framework.Rectangle boundingBox = value2.GetBoundingBox();
					if (position.Intersects(boundingBox) && (!rectangle.HasValue || !rectangle.Value.Intersects(boundingBox)) && (passableTiles == null || !passableTiles.Intersects(position)))
					{
						if (!skipCollisionEffects)
						{
							value2.farmerPushing();
						}
						return true;
					}
				}
			}
			if (buildings.Count > 0)
			{
				foreach (Building building in buildings)
				{
					if (!building.intersects(position) || (rectangle.HasValue && building.intersects(rectangle.Value)))
					{
						continue;
					}
					if (!(character is FarmAnimal) && !(character is JunimoHarvester))
					{
						if (!(character is NPC))
						{
							return true;
						}
						Microsoft.Xna.Framework.Rectangle rectForHumanDoor = building.getRectForHumanDoor();
						rectForHumanDoor.Height += 64;
						if (!rectForHumanDoor.Contains(position))
						{
							return true;
						}
					}
					else
					{
						Microsoft.Xna.Framework.Rectangle rectForAnimalDoor = building.getRectForAnimalDoor();
						rectForAnimalDoor.Height += 64;
						if (!rectForAnimalDoor.Contains(position))
						{
							return true;
						}
						if (character is FarmAnimal farmAnimal && !farmAnimal.CanLiveIn(building))
						{
							return true;
						}
					}
				}
			}
			if (resourceClumps.Count > 0)
			{
				foreach (ResourceClump resourceClump in resourceClumps)
				{
					Microsoft.Xna.Framework.Rectangle boundingBox2 = resourceClump.getBoundingBox();
					if (boundingBox2.Intersects(position) && (!rectangle.HasValue || !boundingBox2.Intersects(rectangle.Value)))
					{
						return true;
					}
				}
			}
			if (!flag && furniture.Count > 0)
			{
				foreach (Furniture item in furniture)
				{
					if (item.furniture_type.Value != 12 && item.IntersectsForCollision(position) && (!rectangle.HasValue || !item.IntersectsForCollision(rectangle.Value)))
					{
						return true;
					}
				}
			}
			NetCollection<LargeTerrainFeature> netCollection = largeTerrainFeatures;
			if (netCollection != null && netCollection.Count > 0)
			{
				foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
				{
					Microsoft.Xna.Framework.Rectangle boundingBox3 = largeTerrainFeature.getBoundingBox();
					if (boundingBox3.Intersects(position) && (!rectangle.HasValue || !boundingBox3.Intersects(rectangle.Value)))
					{
						return true;
					}
				}
			}
		}
		if (!glider)
		{
			StardewValley.Object value;
			if ((!flag || (character != null && !isFarmer && (!pathfinding || !character.willDestroyObjectsUnderfoot))) && _TestCornersTiles(vector, vector2, vector3, vector4, top_mid, bottom_mid, player_top_right, player_top_left, player_bottom_right, player_bottom_left, player_top_mid, player_bottom_mid, bigger_than_tile, delegate(Vector2 corner)
			{
				if (objects.TryGetValue(corner, out value) && value != null)
				{
					if (value.isPassable())
					{
						return false;
					}
					Microsoft.Xna.Framework.Rectangle boundingBox5 = value.GetBoundingBox();
					if (boundingBox5.Intersects(position) && (character == null || character.collideWith(value)))
					{
						if (character is FarmAnimal && value.isAnimalProduct())
						{
							return false;
						}
						if (passableTiles != null && passableTiles.Intersects(boundingBox5))
						{
							return false;
						}
						return true;
					}
				}
				return false;
			}))
			{
				return true;
			}
			TerrainFeature tf;
			_TestCornersTiles(vector, vector2, vector3, vector4, top_mid, bottom_mid, null, null, null, null, null, null, bigger_than_tile, delegate(Vector2 corner)
			{
				if (terrainFeatures.TryGetValue(corner, out tf) && tf != null && tf.getBoundingBox().Intersects(position) && !pathfinding && character != null && !skipCollisionEffects)
				{
					tf.doCollisionAction(position, (int)((float)character.speed + character.addedSpeed), corner, character);
				}
				return false;
			});
			if (_TestCornersTiles(vector, vector2, vector3, vector4, top_mid, bottom_mid, player_top_right, player_top_left, player_bottom_right, player_bottom_left, player_top_mid, player_bottom_mid, bigger_than_tile, (Vector2 corner) => (terrainFeatures.TryGetValue(corner, out tf) && tf != null && tf.getBoundingBox().Intersects(position) && !tf.isPassable(character)) ? true : false))
			{
				return true;
			}
		}
		if (character != null && character.hasSpecialCollisionRules() && (character.isColliding(this, vector) || character.isColliding(this, vector2) || character.isColliding(this, vector3) || character.isColliding(this, vector4)))
		{
			return true;
		}
		if (((isFarmer && (currentEvent == null || currentEvent.playerControlSequence)) || (character != null && character.collidesWithOtherCharacters.Value)) && !pathfinding)
		{
			for (int num = characters.Count - 1; num >= 0; num--)
			{
				NPC nPC = characters[num];
				if (nPC != null && (character == null || !character.Equals(nPC)))
				{
					Microsoft.Xna.Framework.Rectangle boundingBox4 = nPC.GetBoundingBox();
					if (nPC.layingDown)
					{
						boundingBox4.Y -= 64;
						boundingBox4.Height += 64;
					}
					if (boundingBox4.Intersects(position) && !Game1.player.temporarilyInvincible && !skipCollisionEffects)
					{
						nPC.behaviorOnFarmerPushing();
					}
					if (isFarmer)
					{
						if (!flag && !nPC.farmerPassesThrough && boundingBox4.Intersects(position) && !Game1.player.temporarilyInvincible && Game1.player.TemporaryPassableTiles.IsEmpty() && (!nPC.IsMonster || (!((Monster)nPC).isGlider.Value && !Game1.player.GetBoundingBox().Intersects(nPC.GetBoundingBox()))) && !nPC.IsInvisible && !Game1.player.GetBoundingBox().Intersects(boundingBox4))
						{
							return true;
						}
					}
					else if (boundingBox4.Intersects(position))
					{
						return true;
					}
				}
			}
		}
		Layer back_layer = map.RequireLayer("Back");
		Layer buildings_layer = map.RequireLayer("Buildings");
		Tile t;
		if (isFarmer)
		{
			Event obj = currentEvent;
			if (obj != null && obj.checkForCollision(position, (character as Farmer) ?? Game1.player))
			{
				return true;
			}
		}
		else
		{
			if (!pathfinding && !(character is Monster) && damagesFarmer == 0 && !glider)
			{
				foreach (Farmer farmer2 in farmers)
				{
					if (position.Intersects(farmer2.GetBoundingBox()))
					{
						return true;
					}
				}
			}
			if ((isFarm.Value || MineShaft.IsGeneratedLevel(this) || this is IslandLocation) && character != null && !character.Name.Contains("NPC") && !character.EventActor && !glider)
			{
				if (_TestCornersTiles(vector, vector2, vector3, vector4, top_mid, bottom_mid, player_top_right, player_top_left, player_bottom_right, player_bottom_left, player_top_mid, player_bottom_mid, bigger_than_tile, delegate(Vector2 vector5)
				{
					t = back_layer.Tiles[(int)vector5.X, (int)vector5.Y];
					return (t != null && t.Properties.ContainsKey("NPCBarrier")) ? true : false;
				}))
				{
					return true;
				}
			}
			if (glider && !projectile)
			{
				return false;
			}
		}
		if (!isFarmer || !Game1.player.isRafting)
		{
			if (_TestCornersTiles(vector, vector2, vector3, vector4, top_mid, bottom_mid, player_top_right, player_top_left, player_bottom_right, player_bottom_left, player_top_mid, player_bottom_mid, bigger_than_tile, delegate(Vector2 vector5)
			{
				t = back_layer.Tiles[(int)vector5.X, (int)vector5.Y];
				return (t != null && t.Properties.ContainsKey("TemporaryBarrier")) ? true : false;
			}))
			{
				return true;
			}
		}
		if (!isFarmer || !Game1.player.isRafting)
		{
			if ((!(character is FarmAnimal farmAnimal2) || !farmAnimal2.IsActuallySwimming()) && _TestCornersTiles(vector, vector2, vector3, vector4, top_mid, bottom_mid, player_top_right, player_top_left, player_bottom_right, player_bottom_left, player_top_mid, player_bottom_mid, bigger_than_tile, delegate(Vector2 vector5)
			{
				Tile tile2 = back_layer.Tiles[(int)vector5.X, (int)vector5.Y];
				if (tile2 != null)
				{
					bool flag3 = tile2.TileIndexProperties.ContainsKey("Passable");
					if (!flag3)
					{
						flag3 = tile2.Properties.ContainsKey("Passable");
					}
					if (flag3)
					{
						if (passableTiles != null && passableTiles.Contains((int)vector5.X, (int)vector5.Y))
						{
							return false;
						}
						return true;
					}
				}
				return false;
			}))
			{
				return true;
			}
			if (character == null || character.shouldCollideWithBuildingLayer(this))
			{
				Tile tmp;
				if (_TestCornersTiles(vector, vector2, vector3, vector4, top_mid, bottom_mid, player_top_right, player_top_left, player_bottom_right, player_bottom_left, player_top_mid, player_bottom_mid, bigger_than_tile, delegate(Vector2 vector5)
				{
					tmp = buildings_layer.Tiles[(int)vector5.X, (int)vector5.Y];
					if (tmp != null)
					{
						if (projectile && this is VolcanoDungeon)
						{
							Tile tile2 = back_layer.Tiles[(int)vector5.X, (int)vector5.Y];
							if (tile2 != null)
							{
								if (tile2.TileIndexProperties.ContainsKey("Water"))
								{
									return false;
								}
								if (tile2.Properties.ContainsKey("Water"))
								{
									return false;
								}
							}
						}
						if (!tmp.TileIndexProperties.ContainsKey("Shadow") && !tmp.TileIndexProperties.ContainsKey("Passable") && !tmp.Properties.ContainsKey("Passable") && (!projectile || (!tmp.TileIndexProperties.ContainsKey("ProjectilePassable") && !tmp.Properties.ContainsKey("ProjectilePassable"))))
						{
							if (isFarmer)
							{
								goto IL_01c9;
							}
							if (!tmp.TileIndexProperties.ContainsKey("NPCPassable") && !tmp.Properties.ContainsKey("NPCPassable"))
							{
								bool? flag3 = character?.canPassThroughActionTiles();
								if (!flag3.HasValue || flag3 != true || !tmp.Properties.ContainsKey("Action"))
								{
									goto IL_01c9;
								}
							}
						}
					}
					return false;
					IL_01c9:
					if (passableTiles != null)
					{
						return !passableTiles.Contains((int)vector5.X, (int)vector5.Y);
					}
					return true;
				}))
				{
					return true;
				}
			}
			if (!isFarmer && character?.controller != null && !skipCollisionEffects)
			{
				Point point = new Point(position.Center.X / 64, position.Bottom / 64);
				Tile tile = buildings_layer.Tiles[point.X, point.Y];
				if (tile != null && tile.Properties.ContainsKey("Action"))
				{
					openDoor(new Location(point.X, point.Y), Game1.currentLocation.Equals(this));
				}
				else
				{
					point = new Point(position.Center.X / 64, position.Top / 64);
					tile = buildings_layer.Tiles[point.X, point.Y];
					if (tile != null && tile.Properties.ContainsKey("Action"))
					{
						openDoor(new Location(point.X, point.Y), Game1.currentLocation.Equals(this));
					}
				}
			}
			return false;
		}
		if (_TestCornersTiles(vector, vector2, vector3, vector4, top_mid, bottom_mid, player_top_right, player_top_left, player_bottom_right, player_bottom_left, player_top_mid, player_bottom_mid, bigger_than_tile, delegate(Vector2 vector5)
		{
			t = back_layer.Tiles[(int)vector5.X, (int)vector5.Y];
			if ((!(t?.TileIndexProperties.ContainsKey("Water"))) ?? true)
			{
				int num2 = (int)vector5.X;
				int num3 = (int)vector5.Y;
				if (IsTileBlockedBy(new Vector2(num2, num3)))
				{
					Game1.player.isRafting = false;
					Game1.player.Position = new Vector2(num2 * 64, num3 * 64 - 32);
					Game1.player.setTrajectory(0, 0);
				}
				return true;
			}
			return false;
		}))
		{
			return true;
		}
		return false;
	}

	public bool isTilePassable(Vector2 tileLocation)
	{
		Tile tile = map.RequireLayer("Back").Tiles[(int)tileLocation.X, (int)tileLocation.Y];
		if (tile != null && tile.TileIndexProperties.ContainsKey("Passable"))
		{
			return false;
		}
		Tile tile2 = map.RequireLayer("Buildings").Tiles[(int)tileLocation.X, (int)tileLocation.Y];
		if (tile2 != null && !tile2.TileIndexProperties.ContainsKey("Shadow") && !tile2.TileIndexProperties.ContainsKey("Passable"))
		{
			return false;
		}
		return true;
	}

	public bool isTilePassable(Location tileLocation, xTile.Dimensions.Rectangle viewport)
	{
		return isTilePassable(new Vector2(tileLocation.X, tileLocation.Y));
	}

	public bool isPointPassable(Location location, xTile.Dimensions.Rectangle viewport)
	{
		return isTilePassable(new Location(location.X / 64, location.Y / 64), viewport);
	}

	public bool isTilePassable(Microsoft.Xna.Framework.Rectangle nextPosition, xTile.Dimensions.Rectangle viewport)
	{
		if (isPointPassable(new Location(nextPosition.Left, nextPosition.Top), viewport) && isPointPassable(new Location(nextPosition.Right, nextPosition.Bottom), viewport) && isPointPassable(new Location(nextPosition.Left, nextPosition.Bottom), viewport))
		{
			return isPointPassable(new Location(nextPosition.Right, nextPosition.Top), viewport);
		}
		return false;
	}

	public bool isTileOnMap(Vector2 position)
	{
		if (position.X >= 0f && position.X < (float)map.Layers[0].LayerWidth && position.Y >= 0f)
		{
			return position.Y < (float)map.Layers[0].LayerHeight;
		}
		return false;
	}

	public bool isTileOnMap(Point tile)
	{
		return isTileOnMap(tile.X, tile.Y);
	}

	public bool isTileOnMap(int x, int y)
	{
		if (x >= 0 && x < map.Layers[0].LayerWidth && y >= 0)
		{
			return y < map.Layers[0].LayerHeight;
		}
		return false;
	}

	public int numberOfObjectsWithName(string name)
	{
		int num = 0;
		foreach (Object value in objects.Values)
		{
			if (value.Name.Equals(name))
			{
				num++;
			}
		}
		return num;
	}

	public virtual Point getWarpPointTo(string location, Character character = null)
	{
		foreach (Building building in buildings)
		{
			if (building.HasIndoorsName(location))
			{
				return building.getPointForHumanDoor();
			}
		}
		foreach (Warp warp in warps)
		{
			if (warp.TargetName.Equals(location))
			{
				return new Point(warp.X, warp.Y);
			}
			if (warp.TargetName.Equals("BoatTunnel") && location == "IslandSouth")
			{
				return new Point(warp.X, warp.Y);
			}
		}
		foreach (KeyValuePair<Point, string> pair in doors.Pairs)
		{
			if (pair.Value.Equals("BoatTunnel") && location == "IslandSouth")
			{
				return pair.Key;
			}
			if (pair.Value.Equals(location))
			{
				return pair.Key;
			}
		}
		return Point.Zero;
	}

	public Point getWarpPointTarget(Point warpPointLocation, Character character = null)
	{
		foreach (Warp warp in warps)
		{
			if (warp.X == warpPointLocation.X && warp.Y == warpPointLocation.Y)
			{
				return new Point(warp.TargetX, warp.TargetY);
			}
		}
		foreach (KeyValuePair<Point, string> pair in doors.Pairs)
		{
			if (!pair.Key.Equals(warpPointLocation))
			{
				continue;
			}
			string[] tilePropertySplitBySpaces = GetTilePropertySplitBySpaces("Action", "Buildings", warpPointLocation.X, warpPointLocation.Y);
			string text = ArgUtility.Get(tilePropertySplitBySpaces, 0, "");
			switch (text)
			{
			case "WarpCommunityCenter":
				return new Point(32, 23);
			case "Warp_Sunroom_Door":
				return new Point(5, 13);
			case "WarpBoatTunnel":
				return new Point(17, 43);
			case "WarpMensLocker":
			case "LockedDoorWarp":
			case "Warp":
			case "WarpWomensLocker":
				break;
			default:
				if (!text.Contains("Warp"))
				{
					continue;
				}
				Game1.log.Warn($"Door in {NameOrUniqueName} ({pair.Key}) has unknown warp property '{string.Join(" ", tilePropertySplitBySpaces)}', parsing with legacy logic.");
				break;
			}
			if (!ArgUtility.TryGetPoint(tilePropertySplitBySpaces, 1, out var value, out var error, "Point tile") || !ArgUtility.TryGet(tilePropertySplitBySpaces, 3, out var value2, out error, allowBlank: true, "string locationName"))
			{
				LogTileActionError(tilePropertySplitBySpaces, warpPointLocation.X, warpPointLocation.Y, error);
				continue;
			}
			if (!(value2 == "BoatTunnel"))
			{
				if (value2 == "Trailer" && Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
				{
					return new Point(13, 24);
				}
				return new Point(value.X, value.Y);
			}
			return new Point(17, 43);
		}
		return Point.Zero;
	}

	public virtual bool HasLocationOverrideDialogue(NPC character)
	{
		return false;
	}

	public virtual string GetLocationOverrideDialogue(NPC character)
	{
		if (!HasLocationOverrideDialogue(character))
		{
			return null;
		}
		return "";
	}

	public NPC doesPositionCollideWithCharacter(Microsoft.Xna.Framework.Rectangle r, bool ignoreMonsters = false)
	{
		foreach (NPC character in characters)
		{
			if (character.GetBoundingBox().Intersects(r) && (!character.IsMonster || !ignoreMonsters))
			{
				return character;
			}
		}
		return null;
	}

	public void switchOutNightTiles()
	{
		string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces("NightTiles");
		for (int i = 0; i < mapPropertySplitBySpaces.Length; i += 4)
		{
			if (!ArgUtility.TryGet(mapPropertySplitBySpaces, i, out var value, out var error, allowBlank: true, "string layerId") || !ArgUtility.TryGetPoint(mapPropertySplitBySpaces, i + 1, out var value2, out error, "Point position") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces, i + 3, out var value3, out error, "int tileIndex"))
			{
				LogMapPropertyError("NightTiles", mapPropertySplitBySpaces, error);
			}
			else if ((value3 != 726 && value3 != 720) || !Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
			{
				Tile tile = map.RequireLayer(value).Tiles[value2.X, value2.Y];
				if (tile == null)
				{
					LogMapPropertyError("NightTiles", mapPropertySplitBySpaces, $"there's no tile at position ({value2})");
				}
				else
				{
					tile.TileIndex = value3;
				}
			}
		}
		if (!(this is MineShaft) && !(this is Woods))
		{
			lightGlows.Clear();
		}
	}

	public string GetMorningSong()
	{
		LocationWeather weather = GetWeather();
		if (weather.IsRaining)
		{
			return "rain";
		}
		List<string> list = new List<string>();
		List<LocationMusicData> list2 = GetLocationContext().Music;
		if (list2 == null || list2.Count <= 0)
		{
			list2 = LocationContexts.Default.Music ?? new List<LocationMusicData>();
		}
		foreach (LocationMusicData item in list2)
		{
			if (GameStateQuery.CheckConditions(item.Condition, this))
			{
				list.Add(item.Track);
			}
		}
		if (list.Count == 0)
		{
			return "none";
		}
		int num = weather.monthlyNonRainyDayCount.Value - 1;
		if (num < 0)
		{
			num = 0;
		}
		return list[num % list.Count];
	}

	/// <summary>Update the music when the player changes location.</summary>
	/// <param name="oldLocation">The location the player just left.</param>
	/// <param name="newLocation">The location the player just arrived in.</param>
	/// <remarks>For changes to music while a location is active, see <see cref="M:StardewValley.GameLocation.checkForMusic(Microsoft.Xna.Framework.GameTime)" />.</remarks>
	public static void HandleMusicChange(GameLocation oldLocation, GameLocation newLocation)
	{
		string musicTrackName = Game1.getMusicTrackName();
		if (!newLocation.IsOutdoors && Game1.IsPlayingOutdoorsAmbience)
		{
			Game1.changeMusicTrack("none", track_interruptable: true);
		}
		if (musicTrackName == "rain")
		{
			if (!Game1.IsRainingHere(newLocation))
			{
				Game1.stopMusicTrack(MusicContext.Default);
			}
			else if (newLocation is MineShaft && !(oldLocation is MineShaft))
			{
				Game1.stopMusicTrack(MusicContext.Default);
			}
		}
		if (Game1.getMusicTrackName() == "sam_acoustic1")
		{
			Game1.stopMusicTrack(MusicContext.Default);
		}
		if (newLocation is MineShaft)
		{
			return;
		}
		string text = oldLocation?.GetLocationContextId();
		LocationContextData locationContextData = oldLocation?.GetLocationContext();
		LocationData locationData = newLocation?.GetData();
		string text2 = newLocation?.GetLocationContextId();
		LocationContextData locationContextData2 = newLocation?.GetLocationContext();
		string text3 = newLocation?.GetLocationSpecificMusic();
		MusicContext musicContext = locationData?.MusicContext ?? MusicContext.Default;
		bool flag = false;
		if (newLocation != null)
		{
			if (text3 != null)
			{
				flag = locationData?.MusicIsTownTheme ?? false;
				newLocation.isMusicTownMusic = flag;
			}
			else
			{
				newLocation.isMusicTownMusic = false;
			}
		}
		if (text3 == null || musicContext == MusicContext.Default)
		{
			Game1.stopMusicTrack(MusicContext.SubLocation);
		}
		if (text3 == null && Game1.IsRainingHere(newLocation))
		{
			text3 = "rain";
		}
		else if (Game1.IsPlayingMorningSong && oldLocation != null && oldLocation.GetMorningSong() != newLocation.GetMorningSong() && Game1.shouldPlayMorningSong(loading_game: true))
		{
			Game1.playMorningSong(ignoreDelay: true);
			return;
		}
		if (text3 == null && !Game1.IsPlayingBackgroundMusic && newLocation.isOutdoors.Value && Game1.shouldPlayMorningSong())
		{
			Game1.playMorningSong();
			return;
		}
		if (text != text2)
		{
			PlayedNewLocationContextMusic = false;
		}
		if (!locationContextData2.DefaultMusicDelayOneScreen)
		{
			PlayedNewLocationContextMusic = false;
		}
		if (Game1.IsPlayingTownMusic && newLocation.IsOutdoors && (!flag || text3 != musicTrackName))
		{
			Game1.IsPlayingTownMusic = false;
			Game1.changeMusicTrack("none", track_interruptable: true);
		}
		if (flag)
		{
			if (text3 == musicTrackName)
			{
				return;
			}
			text3 = null;
		}
		if (text3 == null)
		{
			if (locationContextData != null && locationContextData2.DefaultMusic != locationContextData.DefaultMusic)
			{
				Game1.stopMusicTrack(MusicContext.Default);
			}
			if (!PlayedNewLocationContextMusic)
			{
				if (locationContextData2.DefaultMusic != null)
				{
					if (Game1.isDarkOut(newLocation) || Game1.isStartingToGetDarkOut(newLocation) || Game1.IsRainingHere(newLocation))
					{
						PlayedNewLocationContextMusic = true;
					}
					else if (locationContextData2.DefaultMusicCondition == null || GameStateQuery.CheckConditions(locationContextData2.DefaultMusicCondition, newLocation))
					{
						Game1.changeMusicTrack(locationContextData2.DefaultMusic, track_interruptable: true);
						Game1.IsPlayingBackgroundMusic = true;
						PlayedNewLocationContextMusic = true;
					}
				}
				else
				{
					PlayedNewLocationContextMusic = true;
					if (!flag && Game1.shouldPlayMorningSong(loading_game: true))
					{
						Game1.playMorningSong();
						return;
					}
				}
			}
		}
		if (!(musicTrackName != text3))
		{
			return;
		}
		if (text3 == null)
		{
			if (!Game1.IsPlayingBackgroundMusic && !Game1.IsPlayingOutdoorsAmbience)
			{
				Game1.stopMusicTrack(MusicContext.Default);
			}
		}
		else
		{
			Game1.changeMusicTrack(text3, track_interruptable: true, musicContext);
		}
	}

	/// <summary>Check for music changes while the level is active.</summary>
	/// <param name="time">The current game time.</param>
	/// <remarks>This should only be used for music changes while a location is active. Other music changes should be in <see cref="M:StardewValley.GameLocation.HandleMusicChange(StardewValley.GameLocation,StardewValley.GameLocation)" />.</remarks>
	public virtual void checkForMusic(GameTime time)
	{
		if (Game1.getMusicTrackName() == "sam_acoustic1" && Game1.isMusicContextActiveButNotPlaying())
		{
			Game1.changeMusicTrack("none", track_interruptable: true);
		}
		if (isMusicTownMusic.HasValue && isMusicTownMusic.Value && !Game1.eventUp && Game1.timeOfDay < 1800 && (Game1.isMusicContextActiveButNotPlaying() || Game1.IsPlayingOutdoorsAmbience))
		{
			string locationSpecificMusic = GetLocationSpecificMusic();
			if (locationSpecificMusic != null)
			{
				MusicContext music_context = GetData()?.MusicContext ?? MusicContext.Default;
				Game1.changeMusicTrack(locationSpecificMusic, track_interruptable: false, music_context);
				Game1.IsPlayingBackgroundMusic = true;
				Game1.IsPlayingTownMusic = true;
			}
		}
		if (IsOutdoors && !IsRainingHere() && !Game1.eventUp)
		{
			bool flag = Game1.isDarkOut(this);
			if (flag && Game1.IsPlayingOutdoorsAmbience && !Game1.IsPlayingNightAmbience)
			{
				Game1.changeMusicTrack("none", track_interruptable: true);
			}
			if (!Game1.isMusicContextActiveButNotPlaying())
			{
				return;
			}
			if (!flag)
			{
				LocationContextData locationContext = GetLocationContext();
				if (locationContext.DayAmbience != null)
				{
					Game1.changeMusicTrack(locationContext.DayAmbience, track_interruptable: true);
				}
				else
				{
					switch (GetSeason())
					{
					case Season.Spring:
						Game1.changeMusicTrack("spring_day_ambient", track_interruptable: true);
						break;
					case Season.Summer:
						Game1.changeMusicTrack("summer_day_ambient", track_interruptable: true);
						break;
					case Season.Fall:
						Game1.changeMusicTrack("fall_day_ambient", track_interruptable: true);
						break;
					case Season.Winter:
						Game1.changeMusicTrack("winter_day_ambient", track_interruptable: true);
						break;
					}
				}
				Game1.IsPlayingOutdoorsAmbience = true;
			}
			else
			{
				if (Game1.timeOfDay >= 2500)
				{
					return;
				}
				LocationContextData locationContext2 = GetLocationContext();
				if (locationContext2.NightAmbience != null)
				{
					Game1.changeMusicTrack(locationContext2.NightAmbience, track_interruptable: true);
				}
				else
				{
					switch (GetSeason())
					{
					case Season.Spring:
						Game1.changeMusicTrack("spring_night_ambient", track_interruptable: true);
						break;
					case Season.Summer:
						Game1.changeMusicTrack("spring_night_ambient", track_interruptable: true);
						break;
					case Season.Fall:
						Game1.changeMusicTrack("spring_night_ambient", track_interruptable: true);
						break;
					case Season.Winter:
						Game1.changeMusicTrack("none", track_interruptable: true);
						break;
					}
				}
				Game1.IsPlayingNightAmbience = true;
				Game1.IsPlayingOutdoorsAmbience = true;
			}
		}
		else if (IsRainingHere() && !Game1.showingEndOfNightStuff && Game1.isMusicContextActiveButNotPlaying())
		{
			Game1.changeMusicTrack("rain", track_interruptable: true);
		}
	}

	public virtual string GetLocationSpecificMusic()
	{
		LocationData data = GetData();
		if (data != null)
		{
			if (data.MusicIgnoredInRain && IsRainingHere())
			{
				return null;
			}
			Season season = GetSeason();
			bool flag = false;
			switch (season)
			{
			case Season.Spring:
				flag = data.MusicIgnoredInSpring;
				break;
			case Season.Summer:
				flag = data.MusicIgnoredInSummer;
				break;
			case Season.Fall:
				flag = data.MusicIgnoredInFall;
				break;
			case Season.Winter:
				flag = data.MusicIgnoredInWinter;
				break;
			}
			if (flag)
			{
				return null;
			}
			if (season == Season.Fall && IsDebrisWeatherHere() && data.MusicIgnoredInFallDebris)
			{
				return null;
			}
			List<LocationMusicData> music = data.Music;
			if (music != null && music.Count > 0)
			{
				foreach (LocationMusicData item in data.Music)
				{
					if (GameStateQuery.CheckConditions(item.Condition, this))
					{
						return item.Track;
					}
				}
			}
			if (data.MusicDefault != null)
			{
				return data.MusicDefault;
			}
		}
		string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces("Music");
		if (mapPropertySplitBySpaces.Length != 0)
		{
			if (mapPropertySplitBySpaces.Length > 1)
			{
				if (!ArgUtility.TryGetInt(mapPropertySplitBySpaces, 0, out var value, out var error, "int startTime") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces, 1, out var value2, out error, "int endTime") || !ArgUtility.TryGet(mapPropertySplitBySpaces, 2, out var value3, out error, allowBlank: true, "string musicId"))
				{
					LogMapPropertyError("Music", mapPropertySplitBySpaces, error);
					return null;
				}
				if (Game1.timeOfDay < value || (value2 != 0 && Game1.timeOfDay >= value2))
				{
					return null;
				}
				return value3;
			}
			return mapPropertySplitBySpaces[0];
		}
		return null;
	}

	public NPC isCollidingWithCharacter(Microsoft.Xna.Framework.Rectangle box)
	{
		if (Game1.isFestival() && currentEvent != null)
		{
			foreach (NPC actor in currentEvent.actors)
			{
				if (actor.GetBoundingBox().Intersects(box))
				{
					return actor;
				}
			}
		}
		foreach (NPC character in characters)
		{
			if (character.GetBoundingBox().Intersects(box))
			{
				return character;
			}
		}
		return null;
	}

	public virtual void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		if (critters != null && Game1.farmEvent == null)
		{
			for (int i = 0; i < critters.Count; i++)
			{
				critters[i].drawAboveFrontLayer(b);
			}
		}
		foreach (NPC character in characters)
		{
			character.drawAboveAlwaysFrontLayer(b);
		}
		if (!(this is MineShaft))
		{
			foreach (NPC character2 in characters)
			{
				(character2 as Monster)?.drawAboveAllLayers(b);
			}
		}
		if (TemporarySprites.Count > 0)
		{
			foreach (TemporaryAnimatedSprite temporarySprite in TemporarySprites)
			{
				if (temporarySprite.drawAboveAlwaysFront)
				{
					temporarySprite.draw(b);
				}
			}
		}
		if (projectiles.Count <= 0)
		{
			return;
		}
		foreach (Projectile projectile in projectiles)
		{
			projectile.draw(b);
		}
	}

	/// <summary>Move objects and furniture covering a tile to another position.</summary>
	/// <param name="oldX">The X tile coordinate from which to move items.</param>
	/// <param name="oldY">The Y tile coordinate from which to move items.</param>
	/// <param name="newX">The X tile coordinate at which to place moved items.</param>
	/// <param name="newY">The Y tile coordinate at which to place moved items.</param>
	/// <param name="unlessItemId">If set, an item won't be moved if its item ID matches this one.</param>
	/// <returns>Returns whether any items were moved.</returns>
	/// <remarks>Multi-tile furniture which cover the old tile will be placed at an equivalent position relative to the new tile.</remarks>
	public bool moveContents(int oldX, int oldY, int newX, int newY, string unlessItemId)
	{
		Vector2 vector = new Vector2(oldX, oldY);
		Vector2 vector2 = new Vector2(newX, newY);
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(oldX * 64, oldY * 64, 64, 64);
		Microsoft.Xna.Framework.Rectangle newPixelArea = new Microsoft.Xna.Framework.Rectangle(newX * 64, newY * 64, 64, 64);
		bool result = false;
		if (objects.TryGetValue(vector, out var value2) && !objects.ContainsKey(vector2) && (unlessItemId == null || !ItemRegistry.HasItemId(value2, unlessItemId)))
		{
			objects.Remove(vector);
			objects.Add(vector2, value2);
			result = true;
		}
		for (int num = this.furniture.Count - 1; num >= 0; num--)
		{
			Furniture furniture = this.furniture[num];
			if (furniture.boundingBox.Value.Intersects(value) && (unlessItemId == null || !ItemRegistry.HasItemId(furniture, unlessItemId)) && !this.furniture.Any((Furniture p) => p.boundingBox.Value.Intersects(newPixelArea)))
			{
				Vector2 vector3 = furniture.TileLocation - vector;
				this.furniture.RemoveAt(num);
				furniture.TileLocation = vector2 + vector3;
				this.furniture.Add(furniture);
				result = true;
			}
		}
		return result;
	}

	private void getGalaxySword()
	{
		Item item = ItemRegistry.Create("(W)4");
		Game1.flashAlpha = 1f;
		Game1.player.holdUpItemThenMessage(item);
		Game1.player.reduceActiveItemByOne();
		if (!Game1.player.addItemToInventoryBool(item))
		{
			Game1.createItemDebris(item, Game1.player.getStandingPosition(), 1);
		}
		Game1.player.mailReceived.Add("galaxySword");
		Game1.player.jitterStrength = 0f;
		Game1.screenGlowHold = false;
		Game1.multiplayer.globalChatInfoMessage("GalaxySword", Game1.player.Name);
	}

	public static void RegisterTouchAction(string key, Action<GameLocation, string[], Farmer, Vector2> action)
	{
		if (action == null)
		{
			registeredTouchActions.Remove(key);
		}
		else
		{
			registeredTouchActions[key] = action;
		}
	}

	public static void RegisterTileAction(string key, Func<GameLocation, string[], Farmer, Point, bool> action)
	{
		if (action == null)
		{
			registeredTileActions.Remove(key);
		}
		else
		{
			registeredTileActions[key] = action;
		}
	}

	/// <summary>Whether to ignore any touch actions the player walks over.</summary>
	public virtual bool IgnoreTouchActions()
	{
		return Game1.eventUp;
	}

	/// <summary>Handle a <c>TouchAction</c> property from a <c>Back</c> map tile in the location when a player steps on the tile.</summary>
	/// <param name="fullActionString">The full action string to parse, including the <c>TouchAction</c> prefix.</param>
	/// <param name="playerStandingPosition">The tile coordinate containing the tile which was stepped on.</param>
	public virtual void performTouchAction(string fullActionString, Vector2 playerStandingPosition)
	{
		string[] action = ArgUtility.SplitBySpace(fullActionString);
		performTouchAction(action, playerStandingPosition);
	}

	/// <summary>Handle a <c>TouchAction</c> property from a <c>Back</c> map tile in the location when a player steps on the tile.</summary>
	/// <param name="action">The action arguments to parse, including the <c>TouchAction</c> prefix.</param>
	/// <param name="playerStandingPosition">The tile coordinate containing the tile which was stepped on.</param>
	public virtual void performTouchAction(string[] action, Vector2 playerStandingPosition)
	{
		if (IgnoreTouchActions())
		{
			return;
		}
		try
		{
			Action<GameLocation, string[], Farmer, Vector2> value2;
			if (!ArgUtility.TryGet(action, 0, out var value, out var error, allowBlank: true, "string actionType"))
			{
				LogError(error);
			}
			else if (registeredTouchActions.TryGetValue(value, out value2))
			{
				value2(this, action, Game1.player, playerStandingPosition);
			}
			else
			{
				if (value == null)
				{
					return;
				}
				switch (value.Length)
				{
				case 9:
					switch (value[0])
					{
					case 'P':
						if (value == "PlayEvent")
						{
							if (!ArgUtility.TryGet(action, 1, out var value4, out error, allowBlank: true, "string eventId") || !ArgUtility.TryGetOptionalBool(action, 2, out var value5, out error, defaultValue: true, "bool checkPreconditions") || !ArgUtility.TryGetOptionalBool(action, 3, out var value6, out error, defaultValue: true, "bool checkSeen") || !ArgUtility.TryGetOptionalRemainder(action, 4, out var value7))
							{
								LogError(error);
							}
							else if (!Game1.PlayEvent(value4, value5, value6) && value7 != null)
							{
								performAction(value7, Game1.player, new Location((int)playerStandingPosition.X, (int)playerStandingPosition.Y));
							}
						}
						break;
					case 'M':
					{
						if (!(value == "MagicWarp"))
						{
							break;
						}
						if (!ArgUtility.TryGet(action, 1, out var locationToWarp, out error, allowBlank: true, "string locationToWarp") || !ArgUtility.TryGetPoint(action, 2, out var tile, out error, "Point tile") || !ArgUtility.TryGetOptional(action, 4, out var value3, out error, null, allowBlank: true, "string mailRequired"))
						{
							LogError(error);
						}
						else if (value3 == null || Game1.player.mailReceived.Contains(value3))
						{
							for (int j = 0; j < 12; j++)
							{
								Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(354, Game1.random.Next(25, 75), 6, 1, new Vector2(Game1.random.Next((int)Game1.player.position.X - 256, (int)Game1.player.position.X + 192), Game1.random.Next((int)Game1.player.position.Y - 256, (int)Game1.player.position.Y + 192)), flicker: false, Game1.random.NextBool()));
							}
							playSound("wand");
							Game1.freezeControls = true;
							Game1.displayFarmer = false;
							Game1.player.CanMove = false;
							Game1.flashAlpha = 1f;
							DelayedAction.fadeAfterDelay(delegate
							{
								Game1.warpFarmer(locationToWarp, tile.X, tile.Y, flip: false);
								Game1.fadeToBlackAlpha = 0.99f;
								Game1.screenGlow = false;
								Game1.displayFarmer = true;
								Game1.player.CanMove = true;
								Game1.freezeControls = false;
							}, 1000);
							Microsoft.Xna.Framework.Rectangle boundingBox = Game1.player.GetBoundingBox();
							new Microsoft.Xna.Framework.Rectangle(boundingBox.X, boundingBox.Y, 64, 64).Inflate(192, 192);
							int num = 0;
							Point tilePoint = Game1.player.TilePoint;
							for (int num2 = tilePoint.X + 8; num2 >= tilePoint.X - 8; num2--)
							{
								Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(6, new Vector2(num2, tilePoint.Y) * 64f, Color.White, 8, flipped: false, 50f)
								{
									layerDepth = 1f,
									delayBeforeAnimationStart = num * 25,
									motion = new Vector2(-0.25f, 0f)
								});
								num++;
							}
						}
						break;
					}
					}
					break;
				case 4:
					switch (value[0])
					{
					case 'W':
						if (value == "Warp")
						{
							if (!ArgUtility.TryGet(action, 1, out var value8, out error, allowBlank: true, "string locationToWarp") || !ArgUtility.TryGetPoint(action, 2, out var value9, out error, "Point tile") || !ArgUtility.TryGetOptional(action, 4, out var value10, out error, null, allowBlank: true, "string mailRequired"))
							{
								LogError(error);
							}
							else if (value10 == null || Game1.player.mailReceived.Contains(value10))
							{
								Game1.warpFarmer(value8, value9.X, value9.Y, flip: false);
							}
						}
						break;
					case 'D':
					{
						if (!(value == "Door"))
						{
							break;
						}
						for (int num3 = 1; num3 < action.Length && (!(action[num3] == "Sebastian") || !IsGreenRainingHere() || Game1.year != 1); num3++)
						{
							if (Game1.player.getFriendshipHeartLevelForNPC(action[num3]) < 2 && num3 == action.Length - 1)
							{
								Game1.player.Position -= Game1.player.getMostRecentMovementVector() * 2f;
								Game1.player.yVelocity = 0f;
								Game1.player.Halt();
								Game1.player.TemporaryPassableTiles.Clear();
								if (Game1.player.Tile == lastTouchActionLocation)
								{
									if (Game1.player.Position.Y > lastTouchActionLocation.Y * 64f + 32f)
									{
										Game1.player.position.Y += 4f;
									}
									else
									{
										Game1.player.position.Y -= 4f;
									}
									lastTouchActionLocation = Vector2.Zero;
								}
								if ((!Game1.player.mailReceived.Contains("doorUnlock" + action[1]) || (action.Length != 2 && !Game1.player.mailReceived.Contains("doorUnlock" + action[2]))) && (action.Length != 3 || !Game1.player.mailReceived.Contains("doorUnlock" + action[2])))
								{
									ShowLockedDoorMessage(action);
								}
								break;
							}
							if (num3 != action.Length - 1 && Game1.player.getFriendshipHeartLevelForNPC(action[num3]) >= 2)
							{
								Game1.player.mailReceived.Add("doorUnlock" + action[num3]);
								break;
							}
							if (num3 == action.Length - 1 && Game1.player.getFriendshipHeartLevelForNPC(action[num3]) >= 2)
							{
								Game1.player.mailReceived.Add("doorUnlock" + action[num3]);
								break;
							}
						}
						break;
					}
					}
					break;
				case 5:
					switch (value[0])
					{
					case 'S':
						if (value == "Sleep" && !Game1.newDay && Game1.shouldTimePass() && Game1.player.hasMoved && !Game1.player.passedOut)
						{
							createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:FarmHouse_Bed_GoToSleep"), createYesNoResponses(), "Sleep", null);
						}
						break;
					case 'E':
						if (value == "Emote")
						{
							if (!ArgUtility.TryGet(action, 1, out var value11, out error, allowBlank: true, "string npcName") || !ArgUtility.TryGetInt(action, 2, out var value12, out error, "int emote"))
							{
								LogError(error);
							}
							else
							{
								getCharacterFromName(value11)?.doEmote(value12);
							}
						}
						break;
					}
					break;
				case 12:
					switch (value[0])
					{
					case 'W':
						if (value == "WomensLocker" && Game1.player.IsMale)
						{
							Game1.player.position.Y += ((float)Game1.player.Speed + Game1.player.addedSpeed) * 2f;
							Game1.player.Halt();
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:WomensLocker_WrongGender"));
						}
						break;
					case 'P':
						if (value == "PoolEntrance")
						{
							if (!Game1.player.swimming.Value)
							{
								Game1.player.swimTimer = 800;
								Game1.player.swimming.Value = true;
								Game1.player.position.Y += 16f;
								Game1.player.yVelocity = -8f;
								playSound("pullItemFromWater");
								Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(27, 100f, 4, 0, new Vector2(Game1.player.Position.X, Game1.player.StandingPixel.Y - 40), flicker: false, flipped: false)
								{
									layerDepth = 1f,
									motion = new Vector2(0f, 2f)
								});
							}
							else
							{
								Game1.player.jump();
								Game1.player.swimTimer = 800;
								Game1.player.position.X = playerStandingPosition.X * 64f;
								playSound("pullItemFromWater");
								Game1.player.yVelocity = 8f;
								Game1.player.swimming.Value = false;
							}
							Game1.player.noMovementPause = 500;
						}
						break;
					}
					break;
				case 8:
					if (value == "asdlfkjg")
					{
						removeTileProperty(13, 29, "Back", "TouchAction");
						removeTileProperty(14, 29, "Back", "TouchAction");
						removeTileProperty(15, 29, "Back", "TouchAction");
						if (Game1.timeOfDay >= 1920 && Game1.timeOfDay < 2020 && farmers.Count == 1 && Game1.stats.DaysPlayed > 3 && !Game1.isRaining && Game1.random.NextDouble() < 0.025)
						{
							Game1.player.mailReceived.Add("asdlkjfg1");
							playSound("shadowDie");
							DelayedAction.playSoundAfterDelay("grassyStep", 500, this);
							DelayedAction.playSoundAfterDelay("grassyStep", 1000, this);
							DelayedAction.playSoundAfterDelay("grassyStep", 1500, this);
							temporarySprites.Add(new TemporaryAnimatedSprite("Characters\\asldkfjsquaskutanfsldk", new Microsoft.Xna.Framework.Rectangle(0, 0, 32, 48), new Vector2(390f, 1980f), flipped: true, 0f, Color.White)
							{
								animationLength = 8,
								totalNumberOfLoops = 99,
								interval = 100f,
								motion = new Vector2(-5f, -1f),
								scale = 5.5f
							});
						}
					}
					break;
				case 11:
				{
					if (!(value == "MagicalSeal") || Game1.player.mailReceived.Contains("krobusUnseal"))
					{
						break;
					}
					Game1.player.Position -= Game1.player.getMostRecentMovementVector() * 2f;
					Game1.player.yVelocity = 0f;
					Game1.player.Halt();
					Game1.player.TemporaryPassableTiles.Clear();
					if (Game1.player.Tile == lastTouchActionLocation)
					{
						if (Game1.player.position.Y > lastTouchActionLocation.Y * 64f + 32f)
						{
							Game1.player.position.Y += 4f;
						}
						else
						{
							Game1.player.position.Y -= 4f;
						}
						lastTouchActionLocation = Vector2.Zero;
					}
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_MagicSeal"));
					for (int i = 0; i < 40; i++)
					{
						Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(666, 1851, 8, 8), 25f, 4, 2, new Vector2(3f, 19f) * 64f + new Vector2(-8 + i % 4 * 16, -(i / 4) * 64 / 4), flicker: false, flipped: false)
						{
							layerDepth = 0.1152f + (float)i / 10000f,
							color = new Color(100 + i * 4, i * 5, 120 + i * 4),
							pingPong = true,
							delayBeforeAnimationStart = i * 10,
							scale = 4f,
							alphaFade = 0.01f
						});
						Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(666, 1851, 8, 8), 25f, 4, 2, new Vector2(3f, 17f) * 64f + new Vector2(-8 + i % 4 * 16, i / 4 * 64 / 4), flicker: false, flipped: false)
						{
							layerDepth = 0.1152f + (float)i / 10000f,
							color = new Color(232 - i * 4, 192 - i * 6, 255 - i * 4),
							pingPong = true,
							delayBeforeAnimationStart = 320 + i * 10,
							scale = 4f,
							alphaFade = 0.01f
						});
						Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(666, 1851, 8, 8), 25f, 4, 2, new Vector2(3f, 19f) * 64f + new Vector2(-8 + i % 4 * 16, -(i / 4) * 64 / 4), flicker: false, flipped: false)
						{
							layerDepth = 0.1152f + (float)i / 10000f,
							color = new Color(100 + i * 4, i * 6, 120 + i * 4),
							pingPong = true,
							delayBeforeAnimationStart = 640 + i * 10,
							scale = 4f,
							alphaFade = 0.01f
						});
					}
					Game1.player.jitterStrength = 2f;
					Game1.player.freezePause = 500;
					playSound("debuffHit");
					break;
				}
				case 15:
				{
					if (!(value == "ConditionalDoor") || action.Length <= 1 || Game1.eventUp || GameStateQuery.CheckConditions(ArgUtility.UnsplitQuoteAware(action, ' ', 1)))
					{
						break;
					}
					Game1.player.Position -= Game1.player.getMostRecentMovementVector() * 2f;
					Game1.player.yVelocity = 0f;
					Game1.player.Halt();
					Game1.player.TemporaryPassableTiles.Clear();
					if (Game1.player.Tile == lastTouchActionLocation)
					{
						if (Game1.player.Position.Y > lastTouchActionLocation.Y * 64f + 32f)
						{
							Game1.player.position.Y += 4f;
						}
						else
						{
							Game1.player.position.Y -= 4f;
						}
						lastTouchActionLocation = Vector2.Zero;
					}
					string text = doesTileHaveProperty((int)playerStandingPosition.X / 64, (int)playerStandingPosition.Y / 64, "LockedDoorMessage", "Back");
					if (text != null)
					{
						Game1.drawObjectDialogue(Game1.content.LoadString(TokenParser.ParseText(text)));
					}
					else
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:LockedDoor"));
					}
					break;
				}
				case 13:
					if (value == "FaceDirection")
					{
						if (!ArgUtility.TryGet(action, 1, out var value13, out error, allowBlank: true, "string npcName") || !ArgUtility.TryGetInt(action, 2, out var value14, out error, "int direction"))
						{
							LogError(error);
						}
						else
						{
							getCharacterFromName(value13)?.faceDirection(value14);
						}
					}
					break;
				case 14:
					if (!(value == "legendarySword"))
					{
						break;
					}
					if (Game1.player.ActiveObject?.QualifiedItemId == "(O)74" && !Game1.player.mailReceived.Contains("galaxySword"))
					{
						Game1.player.Halt();
						Game1.player.faceDirection(2);
						Game1.player.showCarrying();
						Game1.player.jitterStrength = 1f;
						Game1.pauseThenDoFunction(7000, getGalaxySword);
						Game1.changeMusicTrack("none", track_interruptable: false, MusicContext.Event);
						playSound("crit");
						Game1.screenGlowOnce(new Color(30, 0, 150), hold: true, 0.01f, 0.999f);
						DelayedAction.playSoundAfterDelay("stardrop", 1500);
						Game1.screenOverlayTempSprites.AddRange(Utility.sparkleWithinArea(new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), 500, Color.White, 10, 2000));
						Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
						{
							Game1.stopMusicTrack(MusicContext.Event);
						});
					}
					else if (!Game1.player.mailReceived.Contains("galaxySword"))
					{
						localSound("SpringBirds");
					}
					break;
				case 10:
					if (value == "MensLocker" && !Game1.player.IsMale)
					{
						Game1.player.position.Y += ((float)Game1.player.Speed + Game1.player.addedSpeed) * 2f;
						Game1.player.Halt();
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:MensLocker_WrongGender"));
					}
					break;
				case 18:
					if (value == "ChangeIntoSwimsuit")
					{
						Game1.player.changeIntoSwimsuit();
					}
					break;
				case 19:
					if (value == "ChangeOutOfSwimsuit")
					{
						Game1.player.changeOutOfSwimSuit();
					}
					break;
				case 6:
				case 7:
				case 16:
				case 17:
					break;
				}
			}
		}
		catch (Exception)
		{
		}
		void LogError(string errorPhrase)
		{
			LogTileTouchActionError(action, playerStandingPosition, errorPhrase);
		}
	}

	public virtual void updateMap()
	{
		if (_mapPathDirty)
		{
			_mapPathDirty = false;
			if (!string.Equals(mapPath.Value, loadedMapPath, StringComparison.Ordinal))
			{
				reloadMap();
				updateLayout();
			}
		}
	}

	public virtual void updateLayout()
	{
		if (Game1.IsMasterGame)
		{
			updateDoors();
			updateWarps();
		}
	}

	public LargeTerrainFeature getLargeTerrainFeatureAt(int tileX, int tileY)
	{
		foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
		{
			if (largeTerrainFeature.getBoundingBox().Contains(tileX * 64 + 32, tileY * 64 + 32))
			{
				return largeTerrainFeature;
			}
		}
		return null;
	}

	public virtual void UpdateWhenCurrentLocation(GameTime time)
	{
		updateMap();
		if (wasUpdated)
		{
			return;
		}
		wasUpdated = true;
		if (_mapSeatsDirty)
		{
			UpdateMapSeats();
		}
		furnitureToRemove.Update(this);
		if (Game1.player.currentLocation.Equals(this))
		{
			_updateAmbientLighting();

			if (Game1.player.isMoving())
			{
				if (Game1.ticks % 20 == 0)
				{
					StardewValley.Pokemon.PokemonBreeding.OnStepTaken();
				}

				string lName = Name ?? "";
				if (this is StardewValley.Locations.MineShaft || this is StardewValley.Locations.Desert || this is StardewValley.Locations.Beach || lName.Contains("Cave") || lName.Contains("Mine") || lName.Contains("Volcano") || lName.Contains("Desert") || lName.Contains("Beach"))
				{
					StardewValley.Pokemon.PokemonEncounterManager.CheckStepEncounter(this, Game1.player);
				}
			}
		}
		for (int i = 0; i < furniture.Count; i++)
		{
			furniture[i].updateWhenCurrentLocation(time);
		}
		AmbientLocationSounds.update(time);
		critters?.RemoveAll((Critter critter) => critter.update(time, this));
		if (fishSplashAnimation != null)
		{
			fishSplashAnimation.update(time);
			bool flag = fishFrenzyFish.Value != null && !fishFrenzyFish.Value.Equals("");
			double num = (flag ? 0.1 : 0.02);
			ICue cue;
			if (Game1.random.NextDouble() < num)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite(0, fishSplashAnimation.position + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)), Color.White * 0.3f)
				{
					layerDepth = (fishSplashAnimation.position.Y - 64f) / 10000f
				});
				if (flag)
				{
					temporarySprites.Add(new TemporaryAnimatedSprite(0, fishSplashAnimation.position + new Vector2(Game1.random.Next(-64, 64), Game1.random.Next(-64, 64)), Color.White * 0.3f)
					{
						layerDepth = (fishSplashAnimation.position.Y - 64f) / 10000f
					});
					if (Game1.random.NextDouble() < 0.1)
					{
						Game1.sounds.PlayLocal("slosh", this, fishSplashAnimation.Position / 64f, null, SoundContext.Default, out cue);
					}
				}
			}
			if (flag && Game1.random.NextDouble() < 0.005)
			{
				Vector2 position = fishSplashAnimation.position + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32));
				Action<Vector2> splashAnimation = delegate(Vector2 pos)
				{
					TemporarySprites.Add(new TemporaryAnimatedSprite(28, 100f, 2, 1, pos, flicker: false, flipped: false)
					{
						delayBeforeAnimationStart = 0,
						layerDepth = (pos.Y + 1f) / 10000f
					});
				};
				Game1.sounds.PlayLocal("slosh", this, fishSplashAnimation.Position / 64f, null, SoundContext.Default, out cue);
				splashAnimation(position);
				ParsedItemData data = ItemRegistry.GetData(fishFrenzyFish.Value);
				int spriteID = 982648 + Game1.random.Next(99999);
				bool flag2 = Game1.random.NextDouble() < 0.5;
				float num2 = (float)Game1.random.Next(10, 20) / 10f;
				if (Game1.random.NextDouble() < 0.9)
				{
					num2 *= 0.75f;
				}
				TemporarySprites.Add(new TemporaryAnimatedSprite(data.GetTextureName(), data.GetSourceRect(), position, flag2, 0f, Color.White)
				{
					scale = 4f,
					motion = new Vector2((float)((!flag2) ? 1 : (-1)) * ((float)Game1.random.Next(11) * num2 + num2 * 5f) / 20f, (0f - (float)Game1.random.Next(30, 41) * num2) / 10f),
					acceleration = new Vector2(0f, 0.1f),
					rotationChange = (float)((!flag2) ? 1 : (-1)) * ((float)Game1.random.Next(5, 10) * num2) / 800f,
					yStopCoordinate = (int)position.Y + 1,
					id = spriteID,
					layerDepth = position.Y / 10000f,
					reachedStopCoordinateSprite = delegate(TemporaryAnimatedSprite x)
					{
						removeTemporarySpritesWithID(spriteID);
						Game1.sounds.PlayLocal("dropItemInWater", this, position / 64f, null, SoundContext.Default, out var _);
						splashAnimation(x.Position);
					}
				});
			}
		}
		if (orePanAnimation != null)
		{
			orePanAnimation.update(time);
			if (Game1.random.NextDouble() < 0.05)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(432, 1435, 16, 16), orePanAnimation.position + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)), flipped: false, 0.02f, Color.White * 0.8f)
				{
					scale = 2f,
					animationLength = 6,
					interval = 100f
				});
			}
		}
		interiorDoors.Update(time);
		updateWater(time);
		Map.Update(time.ElapsedGameTime.Milliseconds);
		debris.RemoveWhere((Debris d) => d.updateChunks(time, this));
		if (Game1.shouldTimePass() || Game1.isFestival())
		{
			projectiles.RemoveWhere((Projectile projectile) => projectile.update(time, this));
		}
		for (int num3 = _activeTerrainFeatures.Count - 1; num3 >= 0; num3--)
		{
			TerrainFeature terrainFeature = _activeTerrainFeatures[num3];
			if (terrainFeature.tickUpdate(time))
			{
				terrainFeatures.Remove(terrainFeature.Tile);
			}
		}
		largeTerrainFeatures?.RemoveWhere((LargeTerrainFeature feature) => feature.tickUpdate(time));
		foreach (ResourceClump resourceClump in resourceClumps)
		{
			resourceClump.tickUpdate(time);
		}
		if (currentEvent != null)
		{
			bool flag3;
			do
			{
				int currentCommand = currentEvent.CurrentCommand;
				currentEvent.Update(this, time);
				if (currentEvent != null)
				{
					flag3 = currentEvent.simultaneousCommand;
					if (currentCommand == currentEvent.CurrentCommand)
					{
						flag3 = false;
					}
				}
				else
				{
					flag3 = false;
				}
			}
			while (flag3);
		}
		objects.Lock();
		foreach (Object value4 in objects.Values)
		{
			value4.updateWhenCurrentLocation(time);
		}
		objects.Unlock();
		Vector2 player_position;
		if (Game1.gameMode == 3 && this == Game1.currentLocation)
		{
			if (Game1.currentLocation.GetLocationContext().PlayRandomAmbientSounds && isOutdoors.Value)
			{
				if (!IsRainingHere())
				{
					if (Game1.timeOfDay < 2000)
					{
						if (Game1.isMusicContextActiveButNotPlaying() && !IsWinterHere() && Game1.random.NextDouble() < 0.002)
						{
							localSound("SpringBirds");
						}
					}
					else if (Game1.timeOfDay > 2100 && !(this is Beach) && IsSummerHere() && !IsTemporary && Game1.random.NextDouble() < 0.0005)
					{
						localSound("crickets");
					}
				}
				else if (!Game1.eventUp && Game1.options.musicVolumeLevel > 0f && Game1.random.NextDouble() < 0.00015 && !name.Equals("Town"))
				{
					localSound("rainsound");
				}
			}
			Vector2 tile = Game1.player.Tile;
			if (lastTouchActionLocation.Equals(Vector2.Zero))
			{
				string text = doesTileHaveProperty((int)tile.X, (int)tile.Y, "TouchAction", "Back");
				lastTouchActionLocation = tile;
				if (text != null)
				{
					performTouchAction(text, tile);
				}
			}
			else if (!lastTouchActionLocation.Equals(tile))
			{
				lastTouchActionLocation = Vector2.Zero;
			}
			foreach (Farmer farmer in farmers)
			{
				Vector2 tile2 = farmer.Tile;
				Vector2[] directionsTileVectorsWithDiagonals = Utility.DirectionsTileVectorsWithDiagonals;
				for (int num4 = 0; num4 < directionsTileVectorsWithDiagonals.Length; num4++)
				{
					Vector2 vector = directionsTileVectorsWithDiagonals[num4];
					Vector2 key = tile2 + vector;
					if (objects.TryGetValue(key, out var value))
					{
						value.farmerAdjacentAction(farmer, vector.X != 0f && vector.Y != 0f);
					}
				}
			}
			if (Game1.player != null)
			{
				int value2 = Game1.player.facingDirection.Value;
				player_position = Game1.player.Tile;
				Object obj = null;
				if (value2 >= 0 && value2 < 4)
				{
					Vector2 vector2 = Utility.DirectionsTileVectors[value2];
					obj = CheckForSign((int)vector2.X, (int)vector2.Y);
				}
				if (obj == null)
				{
					obj = CheckForSign(0, -1) ?? CheckForSign(0, 1) ?? CheckForSign(-1, 0) ?? CheckForSign(1, 0) ?? CheckForSign(-1, -1) ?? CheckForSign(1, -1) ?? CheckForSign(-1, 1) ?? CheckForSign(1, 1);
				}
				if (obj != null)
				{
					obj.shouldShowSign = true;
				}
			}
		}
		foreach (KeyValuePair<long, FarmAnimal> pair in animals.Pairs)
		{
			tempAnimals.Add(pair);
		}
		foreach (KeyValuePair<long, FarmAnimal> tempAnimal in tempAnimals)
		{
			if (tempAnimal.Value.updateWhenCurrentLocation(time, this))
			{
				animals.Remove(tempAnimal.Key);
			}
		}
		tempAnimals.Clear();
		foreach (Building building in buildings)
		{
			building.Update(time);
		}
		Object CheckForSign(int offsetX, int offsetY)
		{
			if (!objects.TryGetValue(player_position + new Vector2(offsetX, offsetY), out var value3) || !value3.IsTextSign())
			{
				return null;
			}
			return value3;
		}
	}

	public void updateWater(GameTime time)
	{
		waterAnimationTimer -= time.ElapsedGameTime.Milliseconds;
		if (waterAnimationTimer <= 0)
		{
			waterAnimationIndex = (waterAnimationIndex + 1) % 10;
			waterAnimationTimer = 200;
		}
		waterPosition += ((!isFarm.Value) ? ((float)((Math.Sin((float)time.TotalGameTime.Milliseconds / 1000f) + 1.0) * 0.15000000596046448)) : 0.1f);
		if (waterPosition >= 64f)
		{
			waterPosition -= 64f;
			waterTileFlip = !waterTileFlip;
		}
	}

	public NPC getCharacterFromName(string name)
	{
		NPC result = null;
		foreach (NPC character in characters)
		{
			if (character.Name.Equals(name))
			{
				return character;
			}
		}
		return result;
	}

	protected virtual void updateCharacters(GameTime time)
	{
		bool flag = Game1.shouldTimePass();
		for (int num = characters.Count - 1; num >= 0; num--)
		{
			NPC nPC = characters[num];
			if (nPC != null && (flag || nPC is Horse || nPC.forceUpdateTimer > 0))
			{
				nPC.currentLocation = this;
				nPC.update(time, this);
				if (num < characters.Count && nPC is Monster monster && monster.ShouldMonsterBeRemoved())
				{
					characters.RemoveAt(num);
				}
			}
			else if (nPC != null)
			{
				if (nPC.hasJustStartedFacingPlayer)
				{
					nPC.updateFaceTowardsFarmer(time, this);
				}
				nPC.updateEmote(time);
			}
		}
	}

	public Projectile getProjectileFromID(int uniqueID)
	{
		foreach (Projectile projectile in projectiles)
		{
			if (projectile.uniqueID.Value == uniqueID)
			{
				return projectile;
			}
		}
		return null;
	}

	public virtual void updateEvenIfFarmerIsntHere(GameTime time, bool ignoreWasUpdatedFlush = false)
	{
		netAudio.Update();
		removeTemporarySpritesWithIDEvent.Poll();
		rumbleAndFadeEvent.Poll();
		damagePlayersEvent.Poll();
		if (!ignoreWasUpdatedFlush)
		{
			wasUpdated = false;
		}
		updateCharacters(time);
		for (int num = temporarySprites.Count - 1; num >= 0; num--)
		{
			TemporaryAnimatedSprite temporaryAnimatedSprite = ((num < temporarySprites.Count) ? temporarySprites[num] : null);
			if (num < temporarySprites.Count && temporaryAnimatedSprite != null && temporaryAnimatedSprite.update(time) && num < temporarySprites.Count)
			{
				temporarySprites.RemoveAt(num);
			}
		}
		foreach (Building building in buildings)
		{
			building.updateWhenFarmNotCurrentLocation(time);
		}
		if (!Game1.currentLocation.Equals(this) && animals.Length > 0)
		{
			Building parentBuilding = ParentBuilding;
			FarmAnimal[] array = animals.Values.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].updateWhenNotCurrentLocation(parentBuilding, time, this);
			}
		}
	}

	/// <summary>Get the location which contains this one, if applicable.</summary>
	/// <remarks>
	///   <para>For example, the interior for a farm building will have the farm as its root location.</para>
	///   <para>See also <see cref="M:StardewValley.GameLocation.GetRootLocation" />.</para>
	/// </remarks>
	public GameLocation GetParentLocation()
	{
		if (parentLocationName.Value == null)
		{
			return null;
		}
		return Game1.getLocationFromName(parentLocationName.Value);
	}

	/// <summary>Get the parent location which contains this one, or the current location if it has no parent.</summary>
	/// <remarks>See also <see cref="M:StardewValley.GameLocation.GetParentLocation" />.</remarks>
	public GameLocation GetRootLocation()
	{
		return GetParentLocation() ?? this;
	}

	public Response[] createYesNoResponses()
	{
		return new Response[2]
		{
			new Response("Yes", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes")).SetHotKey(Keys.Y),
			new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")).SetHotKey(Keys.Escape)
		};
	}

	public virtual void customQuestCompleteBehavior(string questId)
	{
	}

	public void createQuestionDialogue(string question, Response[] answerChoices, string dialogKey)
	{
		lastQuestionKey = dialogKey;
		Game1.drawObjectQuestionDialogue(question, answerChoices);
	}

	public void createQuestionDialogueWithCustomWidth(string question, Response[] answerChoices, string dialogKey)
	{
		int width = SpriteText.getWidthOfString(question) + 64;
		lastQuestionKey = dialogKey;
		Game1.drawObjectQuestionDialogue(question, answerChoices, width);
	}

	public void createQuestionDialogue(string question, Response[] answerChoices, afterQuestionBehavior afterDialogueBehavior, NPC speaker = null)
	{
		lastQuestionKey = null;
		afterQuestion = afterDialogueBehavior;
		Game1.drawObjectQuestionDialogue(question, answerChoices);
		if (speaker != null)
		{
			Game1.objectDialoguePortraitPerson = speaker;
		}
	}

	public void createQuestionDialogue(string question, Response[] answerChoices, string dialogKey, Object actionObject)
	{
		lastQuestionKey = dialogKey;
		Game1.drawObjectQuestionDialogue(question, answerChoices);
		actionObjectForQuestionDialogue = actionObject;
	}

	public virtual void monsterDrop(Monster monster, int x, int y, Farmer who)
	{
		IList<string> objectsToDrop = monster.objectsToDrop;
		Vector2 vector = Utility.PointToVector2(who.StandingPixel);
		List<Item> extraDropItems = monster.getExtraDropItems();
		if (who.isWearingRing("526") && DataLoader.Monsters(Game1.content).TryGetValue(monster.Name, out var value))
		{
			string[] array = ArgUtility.SplitBySpace(value.Split('/')[6]);
			for (int i = 0; i < array.Length; i += 2)
			{
				if (Game1.random.NextDouble() < Convert.ToDouble(array[i + 1]))
				{
					objectsToDrop.Add(array[i]);
				}
			}
		}
		List<Debris> list = new List<Debris>();
		for (int j = 0; j < objectsToDrop.Count; j++)
		{
			string text = objectsToDrop[j];
			if (text != null && text.StartsWith('-') && int.TryParse(text, out var result))
			{
				list.Add(monster.ModifyMonsterLoot(new Debris(Math.Abs(result), Game1.random.Next(1, 4), new Vector2(x, y), vector)));
			}
			else
			{
				list.Add(monster.ModifyMonsterLoot(new Debris(text, new Vector2(x, y), vector)));
			}
		}
		for (int k = 0; k < extraDropItems.Count; k++)
		{
			list.Add(monster.ModifyMonsterLoot(new Debris(extraDropItems[k], new Vector2(x, y), vector)));
		}
		Trinket.TrySpawnTrinket(this, monster, monster.getStandingPosition());
		if (who.isWearingRing("526"))
		{
			extraDropItems = monster.getExtraDropItems();
			for (int l = 0; l < extraDropItems.Count; l++)
			{
				Item one = extraDropItems[l].getOne();
				one.Stack = extraDropItems[l].Stack;
				one.HasBeenInInventory = false;
				list.Add(monster.ModifyMonsterLoot(new Debris(one, new Vector2(x, y), vector)));
			}
		}
		foreach (Debris item2 in list)
		{
			debris.Add(item2);
		}
		if (who.stats.Get("Book_Void") != 0 && Game1.random.NextDouble() < 0.03 && list != null && monster != null)
		{
			foreach (Debris item3 in list)
			{
				if (item3.item != null)
				{
					Item one2 = item3.item.getOne();
					if (one2 != null)
					{
						one2.Stack = item3.item.Stack;
						one2.HasBeenInInventory = false;
						debris.Add(monster.ModifyMonsterLoot(new Debris(one2, new Vector2(x, y), vector)));
					}
				}
				else if (item3.itemId.Value != null && item3.itemId.Value.Length > 0)
				{
					Item item = ItemRegistry.Create(item3.itemId.Value);
					item.HasBeenInInventory = false;
					debris.Add(monster.ModifyMonsterLoot(new Debris(item, new Vector2(x, y), vector)));
				}
			}
		}
		if (HasUnlockedAreaSecretNotes(who) && Game1.random.NextDouble() < 0.033)
		{
			Object obj = tryToCreateUnseenSecretNote(who);
			if (obj != null)
			{
				monster.ModifyMonsterLoot(Game1.createItemDebris(obj, new Vector2(x, y), -1, this));
			}
		}
		Utility.trySpawnRareObject(who, new Vector2(x, y), this, 1.5);
		if (Utility.tryRollMysteryBox(0.01 + who.team.AverageDailyLuck() / 10.0 + (double)who.LuckLevel * 0.008))
		{
			monster.ModifyMonsterLoot(Game1.createItemDebris(ItemRegistry.Create((who.stats.Get(StatKeys.Mastery(2)) != 0) ? "(O)GoldenMysteryBox" : "(O)MysteryBox"), new Vector2(x, y), -1, this));
		}
		if (who.stats.MonstersKilled > 10 && Game1.random.NextDouble() < 0.0001 + ((!who.mailReceived.Contains("voidBookDropped")) ? ((double)who.stats.MonstersKilled * 1.5E-05) : 0.0004))
		{
			monster.ModifyMonsterLoot(Game1.createItemDebris(ItemRegistry.Create("(O)Book_Void"), new Vector2(x, y), -1, this));
			who.mailReceived.Add("voidBookDropped");
		}
		if (this is Woods && Game1.random.NextDouble() < 0.1)
		{
			monster.ModifyMonsterLoot(Game1.createItemDebris(ItemRegistry.Create("(O)292"), new Vector2(x, y), -1, this));
		}
		if (Game1.netWorldState.Value.GoldenWalnutsFound >= 100)
		{
			if (monster.isHardModeMonster.Value && Game1.stats.Get("hardModeMonstersKilled") > 50 && Game1.random.NextDouble() < 0.001 + (double)((float)who.LuckLevel * 0.0002f))
			{
				monster.ModifyMonsterLoot(Game1.createItemDebris(ItemRegistry.Create("(O)896"), new Vector2(x, y), -1, this));
			}
			else if (monster.isHardModeMonster.Value && Game1.random.NextDouble() < 0.008 + (double)((float)who.LuckLevel * 0.002f))
			{
				monster.ModifyMonsterLoot(Game1.createItemDebris(ItemRegistry.Create("(O)858"), new Vector2(x, y), -1, this));
			}
		}
	}

	public virtual bool HasUnlockedAreaSecretNotes(Farmer who)
	{
		if (!InIslandContext())
		{
			return who.hasMagnifyingGlass;
		}
		return true;
	}

	public bool damageMonster(Microsoft.Xna.Framework.Rectangle areaOfEffect, int minDamage, int maxDamage, bool isBomb, Farmer who, bool isProjectile = false)
	{
		return damageMonster(areaOfEffect, minDamage, maxDamage, isBomb, 1f, 0, 0f, 1f, triggerMonsterInvincibleTimer: false, who, isProjectile);
	}

	private bool isMonsterDamageApplicable(Farmer who, Monster monster, bool horizontalBias = true)
	{
		if (!monster.isGlider.Value && !(who.CurrentTool is Slingshot) && !monster.ignoreDamageLOS.Value)
		{
			Point tilePoint = who.TilePoint;
			Point tilePoint2 = monster.TilePoint;
			if (Math.Abs(tilePoint.X - tilePoint2.X) + Math.Abs(tilePoint.Y - tilePoint2.Y) > 1)
			{
				int num = tilePoint2.X - tilePoint.X;
				int num2 = tilePoint2.Y - tilePoint.Y;
				Vector2 key = new Vector2(tilePoint.X, tilePoint.Y);
				while (num != 0 || num2 != 0)
				{
					if (horizontalBias)
					{
						if (Math.Abs(num) >= Math.Abs(num2))
						{
							key.X += Math.Sign(num);
							num -= Math.Sign(num);
						}
						else
						{
							key.Y += Math.Sign(num2);
							num2 -= Math.Sign(num2);
						}
					}
					else if (Math.Abs(num2) >= Math.Abs(num))
					{
						key.Y += Math.Sign(num2);
						num2 -= Math.Sign(num2);
					}
					else
					{
						key.X += Math.Sign(num);
						num -= Math.Sign(num);
					}
					if ((objects.TryGetValue(key, out var value) && !value.isPassable()) || BlocksDamageLOS((int)key.X, (int)key.Y))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public virtual bool BlocksDamageLOS(int x, int y)
	{
		if (hasTileAt(x, y, "Buildings") && doesTileHaveProperty(x, y, "Passable", "Buildings") == null)
		{
			return true;
		}
		return false;
	}

	public bool damageMonster(Microsoft.Xna.Framework.Rectangle areaOfEffect, int minDamage, int maxDamage, bool isBomb, float knockBackModifier, int addedPrecision, float critChance, float critMultiplier, bool triggerMonsterInvincibleTimer, Farmer who, bool isProjectile = false)
	{
		bool result = false;
		for (int num = characters.Count - 1; num >= 0; num--)
		{
			if (num < characters.Count && characters[num] is Monster { IsMonster: not false, Health: >0 } monster && monster.TakesDamageFromHitbox(areaOfEffect))
			{
				if (monster.currentLocation == null)
				{
					monster.currentLocation = this;
				}
				if (!monster.IsInvisible && !monster.isInvincible() && (isBomb || isProjectile || isMonsterDamageApplicable(who, monster) || isMonsterDamageApplicable(who, monster, horizontalBias: false)))
				{
					bool flag = !isBomb && who?.CurrentTool is MeleeWeapon meleeWeapon && meleeWeapon.type.Value == 1;
					bool flag2 = false;
					if (flag && MeleeWeapon.daggerHitsLeft > 1)
					{
						flag2 = true;
					}
					if (flag2)
					{
						triggerMonsterInvincibleTimer = false;
					}
					result = true;
					if (Game1.currentLocation == this)
					{
						Rumble.rumble(0.1f + (float)(Game1.random.NextDouble() / 8.0), 200 + Game1.random.Next(-50, 50));
					}
					Microsoft.Xna.Framework.Rectangle boundingBox = monster.GetBoundingBox();
					Vector2 trajectory = Utility.getAwayFromPlayerTrajectory(boundingBox, who);
					if (knockBackModifier > 0f)
					{
						trajectory *= knockBackModifier;
					}
					else
					{
						trajectory = new Vector2(monster.xVelocity, monster.yVelocity);
					}
					if (monster.Slipperiness == -1)
					{
						trajectory = Vector2.Zero;
					}
					bool flag3 = false;
					if (who?.CurrentTool != null && monster.hitWithTool(who.CurrentTool))
					{
						return false;
					}
					if (who.hasBuff("statue_of_blessings_5"))
					{
						critChance += 0.1f;
					}
					if (who.professions.Contains(25))
					{
						critChance += critChance * 0.5f;
					}
					int num2;
					if (maxDamage >= 0)
					{
						num2 = Game1.random.Next(minDamage, maxDamage + 1);
						if (who != null && Game1.random.NextDouble() < (double)(critChance + (float)who.LuckLevel * (critChance / 40f)))
						{
							flag3 = true;
							playSound("crit");
							if (who.hasTrinketWithID("IridiumSpur"))
							{
								BuffEffects buffEffects = new BuffEffects();
								buffEffects.Speed.Value = 1f;
								who.applyBuff(new Buff("iridiumspur", null, Game1.content.LoadString("Strings\\1_6_Strings:IridiumSpur_Name"), who.getFirstTrinketWithID("IridiumSpur").GetEffect().GeneralStat * 1000, Game1.objectSpriteSheet_2, 76, buffEffects, false));
							}
						}
						num2 = (flag3 ? ((int)((float)num2 * critMultiplier)) : num2);
						num2 = Math.Max(1, num2 + ((who != null) ? (who.Attack * 3) : 0));
						if (who != null && who.professions.Contains(24))
						{
							num2 = (int)Math.Ceiling((float)num2 * 1.1f);
						}
						if (who != null && who.professions.Contains(26))
						{
							num2 = (int)Math.Ceiling((float)num2 * 1.15f);
						}
						if (who != null && flag3 && who.professions.Contains(29))
						{
							num2 = (int)((float)num2 * 2f);
						}
						if (who != null)
						{
							foreach (BaseEnchantment enchantment in who.enchantments)
							{
								enchantment.OnCalculateDamage(monster, this, who, isBomb, ref num2);
							}
						}
						num2 = monster.takeDamage(num2, (int)trajectory.X, (int)trajectory.Y, isBomb, (double)addedPrecision / 10.0, who);
						if (flag2)
						{
							if (monster.stunTime.Value < 50)
							{
								monster.stunTime.Value = 50;
							}
						}
						else if (monster.stunTime.Value < 50)
						{
							monster.stunTime.Value = 0;
						}
						if (num2 == -1)
						{
							string message = Game1.content.LoadString("Strings\\StringsFromCSFiles:Attack_Miss");
							debris.Add(new Debris(message, 1, new Vector2(boundingBox.Center.X, boundingBox.Center.Y), Color.LightGray, 1f, 0f));
						}
						else
						{
							removeDamageDebris(monster);
							debris.Add(new Debris(num2, new Vector2(boundingBox.Center.X + 16, boundingBox.Center.Y), flag3 ? Color.Yellow : new Color(255, 130, 0), flag3 ? (1f + (float)num2 / 300f) : 1f, monster));
							if (who != null)
							{
								foreach (BaseEnchantment enchantment2 in who.enchantments)
								{
									enchantment2.OnDealtDamage(monster, this, who, isBomb, num2);
								}
							}
						}
						if (triggerMonsterInvincibleTimer)
						{
							monster.setInvincibleCountdown(450 / (flag ? 3 : 2));
						}
						if (who != null)
						{
							foreach (Trinket trinketItem in who.trinketItems)
							{
								trinketItem?.OnDamageMonster(who, monster, num2, isBomb, flag3);
							}
						}
					}
					else
					{
						num2 = -2;
						monster.setTrajectory(trajectory);
						if (monster.Slipperiness > 10)
						{
							monster.xVelocity /= 2f;
							monster.yVelocity /= 2f;
						}
					}
					if (who?.CurrentTool?.QualifiedItemId == "(W)4")
					{
						Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(362, Game1.random.Next(50, 120), 6, 1, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - 32), flicker: false, flipped: false));
					}
					if (monster.Health <= 0)
					{
						onMonsterKilled(who, monster, boundingBox, isBomb);
					}
					else if (num2 > 0)
					{
						monster.shedChunks(Game1.random.Next(1, 3));
						if (flag3)
						{
							Vector2 standingPosition = monster.getStandingPosition();
							Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(362, Game1.random.Next(15, 50), 6, 1, standingPosition - new Vector2(32f, 32f), flicker: false, Game1.random.NextBool())
							{
								scale = 0.75f,
								alpha = (flag3 ? 0.75f : 0.5f)
							});
							Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(362, Game1.random.Next(15, 50), 6, 1, standingPosition - new Vector2(32 + Game1.random.Next(-21, 21) + 32, 32 + Game1.random.Next(-21, 21)), flicker: false, Game1.random.NextBool())
							{
								scale = 0.5f,
								delayBeforeAnimationStart = 50,
								alpha = (flag3 ? 0.75f : 0.5f)
							});
							Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(362, Game1.random.Next(15, 50), 6, 1, standingPosition - new Vector2(32 + Game1.random.Next(-21, 21) - 32, 32 + Game1.random.Next(-21, 21)), flicker: false, Game1.random.NextBool())
							{
								scale = 0.5f,
								delayBeforeAnimationStart = 100,
								alpha = (flag3 ? 0.75f : 0.5f)
							});
							Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(362, Game1.random.Next(15, 50), 6, 1, standingPosition - new Vector2(32 + Game1.random.Next(-21, 21) + 32, 32 + Game1.random.Next(-21, 21)), flicker: false, Game1.random.NextBool())
							{
								scale = 0.5f,
								delayBeforeAnimationStart = 150,
								alpha = (flag3 ? 0.75f : 0.5f)
							});
							Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(362, Game1.random.Next(15, 50), 6, 1, standingPosition - new Vector2(32 + Game1.random.Next(-21, 21) - 32, 32 + Game1.random.Next(-21, 21)), flicker: false, Game1.random.NextBool())
							{
								scale = 0.5f,
								delayBeforeAnimationStart = 200,
								alpha = (flag3 ? 0.75f : 0.5f)
							});
						}
					}
				}
			}
		}
		return result;
	}

	/// <summary>Handle a monster reaching zero health after being hit by the player.</summary>
	/// <param name="who">The player who damaged the monster.</param>
	/// <param name="monster">The monster whose health reached zero.</param>
	/// <param name="monsterBox">The monster's pixel hitbox.</param>
	/// <param name="killedByBomb">Whether the monster was killed by a bomb placed by the player.</param>
	private void onMonsterKilled(Farmer who, Monster monster, Microsoft.Xna.Framework.Rectangle monsterBox, bool killedByBomb)
	{
		bool isHutchSlime = false;
		bool flag = false;
		if (monster is GreenSlime greenSlime)
		{
			isHutchSlime = this is SlimeHutch;
			flag = !greenSlime.firstGeneration.Value;
		}
		who.NotifyQuests((Quest quest) => quest.OnMonsterSlain(this, monster, killedByBomb, isHutchSlime));
		if (!isHutchSlime && Game1.player.team.specialOrders != null)
		{
			foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
			{
				specialOrder.onMonsterSlain?.Invoke(Game1.player, monster);
			}
		}
		if (who != null)
		{
			foreach (BaseEnchantment enchantment in who.enchantments)
			{
				enchantment.OnMonsterSlay(monster, this, who, killedByBomb);
			}
		}
		who?.leftRing.Value?.onMonsterSlay(monster, this, who);
		who?.rightRing.Value?.onMonsterSlay(monster, this, who);
		if (who != null && !isHutchSlime && !flag)
		{
			if (who.IsLocalPlayer)
			{
				Game1.stats.monsterKilled(monster.Name);
			}
			else if (Game1.IsMasterGame)
			{
				who.queueMessage(25, Game1.player, monster.Name);
			}
		}
		if (monster.isHardModeMonster.Value)
		{
			Game1.stats.Increment("hardModeMonstersKilled");
		}
		Game1.stats.MonstersKilled++;
		monsterDrop(monster, monsterBox.Center.X, monsterBox.Center.Y, who);
		if (!isHutchSlime)
		{
			who?.gainExperience(4, isFarm.Value ? Math.Max(1, monster.ExperienceGained / 3) : monster.ExperienceGained);
		}
		if (monster.ShouldMonsterBeRemoved())
		{
			characters.Remove(monster);
		}
		removeTemporarySpritesWithID((int)(monster.position.X * 777f + monster.position.Y * 77777f));
		if (who?.CurrentTool is MeleeWeapon meleeWeapon && (meleeWeapon.QualifiedItemId == "(W)65" || (meleeWeapon.appearance.Value != null && meleeWeapon.appearance.Value.Equals("(W)65"))))
		{
			Utility.addRainbowStarExplosion(this, new Vector2(monsterBox.Center.X - 32, monsterBox.Center.Y - 32), Game1.random.Next(6, 9));
		}
	}

	public void growWeedGrass(int iterations)
	{
		for (int i = 0; i < iterations; i++)
		{
			KeyValuePair<Vector2, TerrainFeature>[] array = terrainFeatures.Pairs.ToArray();
			for (int j = 0; j < array.Length; j++)
			{
				KeyValuePair<Vector2, TerrainFeature> keyValuePair = array[j];
				if (!(keyValuePair.Value is Grass grass) || !(Game1.random.NextDouble() < 0.65))
				{
					continue;
				}
				if (grass.numberOfWeeds.Value < 4)
				{
					grass.numberOfWeeds.Value = Math.Max(0, Math.Min(4, grass.numberOfWeeds.Value + Game1.random.Next(3)));
				}
				else
				{
					if (grass.numberOfWeeds.Value < 4)
					{
						continue;
					}
					int x = (int)keyValuePair.Key.X;
					int y = (int)keyValuePair.Key.Y;
					Vector2[] adjacentTileLocationsArray = Utility.getAdjacentTileLocationsArray(keyValuePair.Key);
					for (int k = 0; k < adjacentTileLocationsArray.Length; k++)
					{
						Vector2 vector = adjacentTileLocationsArray[k];
						if (isTileOnMap(x, y) && !IsTileBlockedBy(vector) && doesTileHaveProperty((int)vector.X, (int)vector.Y, "Diggable", "Back") != null && !IsNoSpawnTile(vector) && Game1.random.NextDouble() < 0.25)
						{
							terrainFeatures.Add(vector, new Grass(grass.grassType.Value, Game1.random.Next(1, 3)));
						}
					}
				}
			}
		}
	}

	public bool tryPlaceObject(Vector2 tile, Object o)
	{
		if (CanItemBePlacedHere(tile))
		{
			o.initializeLightSource(tile);
			objects.Add(tile, o);
			return true;
		}
		return false;
	}

	public void removeDamageDebris(Monster monster)
	{
		debris.RemoveWhere((Debris d) => d.toHover != null && d.toHover.Equals(monster) && !d.nonSpriteChunkColor.Equals(Color.Yellow) && d.timeSinceDoneBouncing > 900f);
	}

	public void spawnWeeds(bool weedsOnly)
	{
		LocationData data = GetData();
		int num = Game1.random.Next(data?.MinDailyWeeds ?? 1, (data?.MaxDailyWeeds ?? 5) + 1);
		if (Game1.dayOfMonth == 1 && Game1.IsSpring)
		{
			num *= data?.FirstDayWeedMultiplier ?? 15;
		}
		for (int i = 0; i < num; i++)
		{
			int num2 = 0;
			while (num2 < 3)
			{
				int num3 = Game1.random.Next(map.DisplayWidth / 64);
				int num4 = Game1.random.Next(map.DisplayHeight / 64);
				Vector2 vector = new Vector2(num3, num4);
				objects.TryGetValue(vector, out var value);
				int num5 = -1;
				int num6 = -1;
				if (Game1.random.NextDouble() < 0.15 + (weedsOnly ? 0.05 : 0.0))
				{
					num5 = 1;
				}
				else if (!weedsOnly)
				{
					if (Game1.random.NextDouble() < 0.35)
					{
						num6 = 1;
					}
					else if (!isFarm.Value && Game1.random.NextDouble() < 0.35)
					{
						num6 = 2;
					}
				}
				if (num6 != -1)
				{
					if (this is Farm && Game1.random.NextDouble() < 0.25)
					{
						return;
					}
				}
				else if (value == null && doesTileHaveProperty(num3, num4, "Diggable", "Back") != null && isTileLocationOpen(new Location(num3, num4)) && !IsTileOccupiedBy(vector) && !isWaterTile(num3, num4))
				{
					if (IsNoSpawnTile(vector, "Grass"))
					{
						continue;
					}
					if (num5 != -1 && GetSeason() != Season.Winter && name.Value == "Farm")
					{
						if (Game1.GetFarmTypeID() == "MeadowlandsFarm" && Game1.random.NextDouble() < 0.1)
						{
							num5 = 7;
						}
						int numberOfWeeds = Game1.random.Next(1, 3);
						terrainFeatures.Add(vector, new Grass(num5, numberOfWeeds));
					}
				}
				num2++;
			}
		}
	}

	public virtual void OnMiniJukeboxAdded()
	{
		miniJukeboxCount.Value += 1;
		UpdateMiniJukebox();
	}

	public virtual void OnMiniJukeboxRemoved()
	{
		miniJukeboxCount.Value -= 1;
		UpdateMiniJukebox();
	}

	public virtual void UpdateMiniJukebox()
	{
		if (miniJukeboxCount.Value <= 0)
		{
			miniJukeboxCount.Set(0);
			miniJukeboxTrack.Set("");
		}
	}

	public virtual bool IsMiniJukeboxPlaying()
	{
		if (miniJukeboxCount.Value > 0 && miniJukeboxTrack.Value != "" && (!IsOutdoors || !IsRainingHere()))
		{
			return !Game1.isGreenRain;
		}
		return false;
	}

	/// <summary>Update the location state when setting up the new day, before the game saves overnight.</summary>
	/// <param name="dayOfMonth">The current day of month.</param>
	/// <remarks>See also <see cref="M:StardewValley.GameLocation.OnDayStarted" />, which happens after saving when the day has started.</remarks>
	public virtual void DayUpdate(int dayOfMonth)
	{
		isMusicTownMusic = null;
		netAudio.StopPlaying("fuse");
		SelectRandomMiniJukeboxTrack();
		critters?.Clear();
		characters.RemoveWhere((NPC npc) => npc is JunimoHarvester || (npc is Monster monster && monster.wildernessFarmMonster));
		FarmAnimal[] array = animals.Values.ToArray();
		for (int num = 0; num < array.Length; num++)
		{
			array[num].dayUpdate(this);
		}
		for (int num2 = this.debris.Count - 1; num2 >= 0; num2--)
		{
			Debris debris = this.debris[num2];
			if (debris.isEssentialItem() && Game1.IsMasterGame)
			{
				if (debris.item?.QualifiedItemId == "(O)73")
				{
					debris.collect(Game1.player);
				}
				else
				{
					Item item = debris.item;
					debris.item = null;
					Game1.player.team.returnedDonations.Add(item);
					Game1.player.team.newLostAndFoundItems.Value = true;
				}
				this.debris.RemoveAt(num2);
			}
		}
		updateMap();
		temporarySprites.Clear();
		KeyValuePair<Vector2, TerrainFeature>[] array2 = terrainFeatures.Pairs.ToArray();
		KeyValuePair<Vector2, TerrainFeature>[] array3 = array2;
		for (int num = 0; num < array3.Length; num++)
		{
			KeyValuePair<Vector2, TerrainFeature> keyValuePair = array3[num];
			if (!isTileOnMap(keyValuePair.Key))
			{
				terrainFeatures.Remove(keyValuePair.Key);
			}
			else
			{
				keyValuePair.Value.dayUpdate();
			}
		}
		array3 = array2;
		foreach (KeyValuePair<Vector2, TerrainFeature> keyValuePair2 in array3)
		{
			if (keyValuePair2.Value is HoeDirt hoeDirt)
			{
				hoeDirt.updateNeighbors();
			}
		}
		if (largeTerrainFeatures != null)
		{
			LargeTerrainFeature[] array4 = largeTerrainFeatures.ToArray();
			for (int num = 0; num < array4.Length; num++)
			{
				array4[num].dayUpdate();
			}
		}
		objects.Lock();
		foreach (KeyValuePair<Vector2, Object> pair in objects.Pairs)
		{
			pair.Value.DayUpdate();
			if (pair.Value.destroyOvernight)
			{
				pair.Value.performRemoveAction();
				objects.Remove(pair.Key);
			}
		}
		objects.Unlock();
		RespawnStumpsFromMapProperty();
		if (!(this is FarmHouse))
		{
			this.debris.RemoveWhere((Debris d) => d.item == null && d.itemId.Value == null);
		}
		if (map != null && (isOutdoors.Value || map.Properties.ContainsKey("ForceSpawnForageables")) && !map.Properties.ContainsKey("skipWeedGrowth"))
		{
			if (Game1.dayOfMonth % 7 == 0 && !(this is Farm))
			{
				Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 0, 0);
				if (this is IslandWest)
				{
					rectangle = new Microsoft.Xna.Framework.Rectangle(31, 3, 77, 70);
				}
				KeyValuePair<Vector2, Object>[] array5 = objects.Pairs.ToArray();
				for (int num = 0; num < array5.Length; num++)
				{
					KeyValuePair<Vector2, Object> keyValuePair3 = array5[num];
					if (keyValuePair3.Value.isSpawnedObject.Value && keyValuePair3.Value.SpecialVariable != 724519 && !rectangle.Contains(Utility.Vector2ToPoint(keyValuePair3.Key)))
					{
						objects.Remove(keyValuePair3.Key);
					}
				}
				numberOfSpawnedObjectsOnMap = 0;
				spawnObjects();
				spawnObjects();
			}
			spawnObjects();
			if (Game1.dayOfMonth == 1)
			{
				spawnObjects();
			}
			if (Game1.stats.DaysPlayed < 4)
			{
				spawnObjects();
			}
			Layer layer = map.GetLayer("Paths");
			if (layer != null && !(this is Farm))
			{
				for (int num3 = 0; num3 < map.Layers[0].LayerWidth; num3++)
				{
					for (int num4 = 0; num4 < map.Layers[0].LayerHeight; num4++)
					{
						if (!TryGetTreeIdForTile(layer.Tiles[num3, num4], out var treeId, out var _, out var growthStageOnRegrow, out var isFruitTree) || !Game1.random.NextBool())
						{
							continue;
						}
						Vector2 vector = new Vector2(num3, num4);
						if (GetFurnitureAt(vector) == null && !terrainFeatures.ContainsKey(vector) && !objects.ContainsKey(vector) && getBuildingAt(vector) == null)
						{
							if (isFruitTree)
							{
								terrainFeatures.Add(vector, new FruitTree(treeId, growthStageOnRegrow ?? 2));
							}
							else
							{
								terrainFeatures.Add(vector, new Tree(treeId, growthStageOnRegrow ?? 2));
							}
						}
					}
				}
			}
		}
		terrainFeatures.RemoveWhere((KeyValuePair<Vector2, TerrainFeature> pair) => pair.Value is HoeDirt hoeDirt2 && (hoeDirt2.crop == null || hoeDirt2.crop.forageCrop.Value) && (!objects.TryGetValue(pair.Key, out var value) || value == null || !value.IsSpawnedObject || !value.isForage()) && Game1.random.NextBool(GetDirtDecayChance(pair.Key)));
		lightLevel.Value = 0f;
		foreach (Furniture item2 in furniture)
		{
			item2.minutesElapsed(Utility.CalculateMinutesUntilMorning(Game1.timeOfDay));
			item2.DayUpdate();
		}
		addLightGlows();
		if (!(this is Farm))
		{
			HandleGrassGrowth(dayOfMonth);
		}
		foreach (Building building in buildings)
		{
			building.dayUpdate(dayOfMonth);
		}
		foreach (string item3 in new List<string>(Game1.netWorldState.Value.Builders.Keys))
		{
			BuilderData builderData = Game1.netWorldState.Value.Builders[item3];
			if (builderData.buildingLocation.Value == NameOrUniqueName)
			{
				Building buildingAt = getBuildingAt(Utility.PointToVector2(builderData.buildingTile.Value));
				if (buildingAt == null || (buildingAt.daysUntilUpgrade.Value == 0 && buildingAt.daysOfConstructionLeft.Value == 0))
				{
					Game1.netWorldState.Value.Builders.Remove(item3);
				}
				else
				{
					Game1.netWorldState.Value.MarkUnderConstruction(item3, buildingAt);
				}
			}
		}
		if (dayOfMonth == 9 && Name.Equals("Backwoods"))
		{
			if (terrainFeatures.GetValueOrDefault(new Vector2(18f, 18f)) is HoeDirt)
			{
				terrainFeatures.Remove(new Vector2(18f, 18f));
			}
			tryPlaceObject(new Vector2(18f, 18f), ItemRegistry.Create<Object>("(O)SeedSpot"));
		}
		fishSplashPointTime = 0;
		fishFrenzyFish.Value = "";
		fishSplashPoint.Value = Point.Zero;
		orePanPoint.Value = Point.Zero;
	}

	/// <summary>Get the probability that a hoed dirt tile decays overnight, as a value between 0 (never) and 1 (always).</summary>
	/// <param name="tile">The dirt tile position.</param>
	public virtual double GetDirtDecayChance(Vector2 tile)
	{
		if (TryGetMapPropertyAs("DirtDecayChance", out double parsed, required: false))
		{
			return parsed;
		}
		if (IsGreenhouse)
		{
			return 0.0;
		}
		if (this is Farm || this is IslandWest || isFarm.Value)
		{
			return 0.1;
		}
		return 1.0;
	}

	/// <summary>If the location's map has the <c>Stumps</c> map property, respawn any missing stumps. This will destroy any objects placed on the same tile.</summary>
	public void RespawnStumpsFromMapProperty()
	{
		string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces("Stumps");
		for (int i = 0; i < mapPropertySplitBySpaces.Length; i += 3)
		{
			if (!ArgUtility.TryGetVector2(mapPropertySplitBySpaces, i, out var value, out var error, integerOnly: false, "Vector2 tile"))
			{
				LogMapPropertyError("Stumps", mapPropertySplitBySpaces, error);
				continue;
			}
			bool flag = false;
			foreach (ResourceClump resourceClump in resourceClumps)
			{
				if (resourceClump.Tile == value)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				resourceClumps.Add(new ResourceClump(600, 2, 2, value));
				removeObject(value, showDestroyedObject: false);
				removeObject(value + new Vector2(1f, 0f), showDestroyedObject: false);
				removeObject(value + new Vector2(1f, 1f), showDestroyedObject: false);
				removeObject(value + new Vector2(0f, 1f), showDestroyedObject: false);
			}
		}
	}

	public void addLightGlows()
	{
		int num = Game1.getTrulyDarkTime(this) - 100;
		if (isOutdoors.Value || (Game1.timeOfDay >= num && !Game1.newDay))
		{
			return;
		}
		lightGlows.Clear();
		string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces("DayTiles");
		for (int i = 0; i < mapPropertySplitBySpaces.Length; i += 4)
		{
			if (!ArgUtility.TryGet(mapPropertySplitBySpaces, i, out var value, out var error, allowBlank: true, "string layerId") || !ArgUtility.TryGetVector2(mapPropertySplitBySpaces, i + 1, out var value2, out error, integerOnly: false, "Vector2 position") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces, i + 3, out var value3, out error, "int tileIndex"))
			{
				LogMapPropertyError("DayTiles", mapPropertySplitBySpaces, error);
				continue;
			}
			Tile tile = map.RequireLayer(value).Tiles[(int)value2.X, (int)value2.Y];
			if (tile != null)
			{
				tile.TileIndex = value3;
				switch (value3)
				{
				case 257:
					lightGlows.Add(value2 * 64f + new Vector2(32f, -4f));
					break;
				case 256:
					lightGlows.Add(value2 * 64f + new Vector2(32f, 64f));
					break;
				case 405:
					lightGlows.Add(value2 * 64f + new Vector2(32f, 32f));
					lightGlows.Add(value2 * 64f + new Vector2(96f, 32f));
					break;
				case 469:
					lightGlows.Add(value2 * 64f + new Vector2(32f, 36f));
					break;
				case 1224:
					lightGlows.Add(value2 * 64f + new Vector2(32f, 32f));
					break;
				}
			}
		}
	}

	public NPC isCharacterAtTile(Vector2 tileLocation)
	{
		NPC result = null;
		tileLocation.X = (int)tileLocation.X;
		tileLocation.Y = (int)tileLocation.Y;
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle((int)tileLocation.X * 64, (int)tileLocation.Y * 64, 64, 64);
		if (currentEvent == null)
		{
			foreach (NPC character in characters)
			{
				if (character.GetBoundingBox().Intersects(value))
				{
					return character;
				}
			}
		}
		else
		{
			foreach (NPC actor in currentEvent.actors)
			{
				if (actor.GetBoundingBox().Intersects(value))
				{
					return actor;
				}
			}
		}
		return result;
	}

	public void ResetCharacterDialogues()
	{
		for (int num = characters.Count - 1; num >= 0; num--)
		{
			characters[num].resetCurrentDialogue();
		}
	}

	/// <summary>Get the value of a map property, if it's defined.</summary>
	/// <param name="propertyName">The property name to check.</param>
	/// <returns>Returns the map property value, or null if it's not set.</returns>
	public string getMapProperty(string propertyName)
	{
		if (!TryGetMapProperty(propertyName, out var propertyValue))
		{
			return null;
		}
		return propertyValue;
	}

	/// <summary>Get the value of a map property, if it's defined.</summary>
	/// <param name="propertyName">The property name to check.</param>
	/// <param name="propertyValue">The map property value, if it's set.</param>
	/// <returns>Returns whether the map property is set.</returns>
	public bool TryGetMapProperty(string propertyName, out string propertyValue)
	{
		Map map = Map;
		if (map == null)
		{
			Game1.log.Warn($"Can't read map property '{propertyName}' for location '{NameOrUniqueName}' because the map is null.");
			propertyValue = null;
			return false;
		}
		if (map.Properties.TryGetValue(propertyName, out propertyValue))
		{
			return propertyValue != null;
		}
		return false;
	}

	/// <summary>Get the space-delimited values defined by a map property.</summary>
	/// <param name="propertyName">The property name to read.</param>
	/// <returns>Returns the map property value, or an empty array if it's empty or unset.</returns>
	public string[] GetMapPropertySplitBySpaces(string propertyName)
	{
		if (!TryGetMapProperty(propertyName, out var propertyValue) || propertyValue == null)
		{
			return LegacyShims.EmptyArray<string>();
		}
		return ArgUtility.SplitBySpace(propertyValue);
	}

	/// <summary>Get a map property which defines a boolean value.</summary>
	/// <param name="key">The property name to read.</param>
	/// <param name="parsed">The parsed boolean value, if the map property was present and valid.</param>
	/// <param name="required">Whether to log an error if the map property isn't defined.</param>
	public bool TryGetMapPropertyAs(string key, out bool parsed, bool required = false)
	{
		if (!TryGetMapProperty(key, out var propertyValue))
		{
			if (required)
			{
				LogMapPropertyError(key, "", "required map property isn't defined");
			}
			parsed = false;
			return false;
		}
		switch (propertyValue)
		{
		case "T":
		case "t":
			parsed = true;
			return true;
		case "F":
		case "f":
			parsed = false;
			return true;
		default:
			if (bool.TryParse(propertyValue, out parsed))
			{
				return true;
			}
			LogMapPropertyError(key, propertyValue, "not a valid boolean value");
			return false;
		}
	}

	/// <summary>Get a map property which defines a space-delimited <see cref="T:System.Double" /> value.</summary>
	/// <param name="key">The property name to read.</param>
	/// <param name="parsed">The parsed value, if the map property was present and valid.</param>
	/// <param name="required">Whether to log an error if the map property isn't defined.</param>
	public bool TryGetMapPropertyAs(string key, out double parsed, bool required = false)
	{
		if (!TryGetMapProperty(key, out var propertyValue))
		{
			if (required)
			{
				LogMapPropertyError(key, "", "required map property isn't defined");
			}
			parsed = 0.0;
			return false;
		}
		if (!double.TryParse(propertyValue, out parsed))
		{
			LogMapPropertyError(key, propertyValue, "value '" + propertyValue + "' can't be parsed as a decimal value");
			return false;
		}
		return true;
	}

	/// <summary>Get a map property which defines a space-delimited <see cref="T:Microsoft.Xna.Framework.Point" /> position.</summary>
	/// <param name="key">The property name to read.</param>
	/// <param name="parsed">The parsed position value, if the map property was present and valid.</param>
	/// <param name="required">Whether to log an error if the map property isn't defined.</param>
	public bool TryGetMapPropertyAs(string key, out Point parsed, bool required = false)
	{
		string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces(key);
		if (mapPropertySplitBySpaces.Length == 0)
		{
			if (required)
			{
				LogMapPropertyError(key, "", "required map property isn't defined");
			}
			parsed = Point.Zero;
			return false;
		}
		if (!ArgUtility.TryGetPoint(mapPropertySplitBySpaces, 0, out parsed, out var error, "parsed"))
		{
			LogMapPropertyError(key, mapPropertySplitBySpaces, error);
			parsed = Point.Zero;
			return false;
		}
		return true;
	}

	/// <summary>Get a map property which defines a space-delimited <see cref="T:Microsoft.Xna.Framework.Vector2" /> position.</summary>
	/// <param name="key">The property name to read.</param>
	/// <param name="parsed">The parsed position value, if the map property was present and valid.</param>
	/// <param name="required">Whether to log an error if the map property isn't defined.</param>
	public bool TryGetMapPropertyAs(string key, out Vector2 parsed, bool required = false)
	{
		string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces(key);
		if (mapPropertySplitBySpaces.Length == 0)
		{
			if (required)
			{
				LogMapPropertyError(key, "", "required map property isn't defined");
			}
			parsed = Vector2.Zero;
			return false;
		}
		if (!ArgUtility.TryGetVector2(mapPropertySplitBySpaces, 0, out parsed, out var error, integerOnly: false, "parsed"))
		{
			LogMapPropertyError(key, mapPropertySplitBySpaces, error);
			parsed = Vector2.Zero;
			return false;
		}
		return true;
	}

	/// <summary>Get a map property which defines a space-delimited position and size.</summary>
	/// <param name="key">The property name to read.</param>
	/// <param name="parsed">The parsed position value, if the map property was present and valid.</param>
	/// <param name="required">Whether to log an error if the map property isn't defined.</param>
	public bool TryGetMapPropertyAs(string key, out Microsoft.Xna.Framework.Rectangle parsed, bool required = false)
	{
		string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces(key);
		if (mapPropertySplitBySpaces.Length == 0)
		{
			if (required)
			{
				LogMapPropertyError(key, "", "required map property isn't defined");
			}
			parsed = Microsoft.Xna.Framework.Rectangle.Empty;
			return false;
		}
		if (!ArgUtility.TryGetRectangle(mapPropertySplitBySpaces, 0, out parsed, out var error, "parsed"))
		{
			LogMapPropertyError(key, mapPropertySplitBySpaces, error);
			parsed = Microsoft.Xna.Framework.Rectangle.Empty;
			return false;
		}
		return true;
	}

	/// <summary>Get whether a map property is defined and has a non-empty value.</summary>
	/// <param name="propertyName">The property name to check.</param>
	public bool HasMapPropertyWithValue(string propertyName)
	{
		if (map != null && Map.Properties.TryGetValue(propertyName, out var value))
		{
			if (value == null)
			{
				return false;
			}
			return value.ToString().Length > 0;
		}
		return false;
	}

	public virtual void tryToAddCritters(bool onlyIfOnScreen = false)
	{
		if (Game1.CurrentEvent != null)
		{
			return;
		}
		double num = map.Layers[0].LayerWidth * map.Layers[0].LayerHeight;
		double num2 = Math.Max(0.15, Math.Min(0.5, num / 15000.0));
		double chance = num2;
		double chance2 = num2;
		double chance3 = num2 / 2.0;
		double chance4 = num2 / 2.0;
		double chance5 = num2 / 8.0;
		double num3 = num2 * 2.0;
		if (IsRainingHere())
		{
			return;
		}
		addClouds(num3 / (double)(onlyIfOnScreen ? 2f : 1f), onlyIfOnScreen);
		if (!(this is Beach) && critters != null && critters.Count <= (IsSummerHere() ? 20 : 10))
		{
			addBirdies(chance, onlyIfOnScreen);
			addButterflies(chance2, onlyIfOnScreen);
			addBunnies(chance3, onlyIfOnScreen);
			addSquirrels(chance4, onlyIfOnScreen);
			addWoodpecker(chance5, onlyIfOnScreen);
			if (Game1.isDarkOut(this) && Game1.random.NextDouble() < 0.01)
			{
				addOwl();
			}
			if (Game1.isDarkOut(this))
			{
				addOpossums(num2 / 10.0, onlyIfOnScreen);
			}
		}
	}

	public void addClouds(double chance, bool onlyIfOnScreen = false)
	{
		if (!IsSummerHere() || IsRainingHere() || Game1.weatherIcon == 4 || Game1.timeOfDay >= Game1.getStartingToGetDarkTime(this) - 100)
		{
			return;
		}
		while (Game1.random.NextDouble() < Math.Min(0.9, chance))
		{
			Vector2 vector = getRandomTile();
			if (onlyIfOnScreen)
			{
				vector = (Game1.random.NextBool() ? new Vector2(map.Layers[0].LayerWidth, Game1.random.Next(map.Layers[0].LayerHeight)) : new Vector2(Game1.random.Next(map.Layers[0].LayerWidth), map.Layers[0].LayerHeight));
			}
			if (!onlyIfOnScreen && Utility.isOnScreen(vector * 64f, 1280))
			{
				continue;
			}
			Cloud cloud = new Cloud(vector);
			bool flag = true;
			if (critters != null)
			{
				foreach (Critter critter in critters)
				{
					if (critter is Cloud && critter.getBoundingBox(0, 0).Intersects(cloud.getBoundingBox(0, 0)))
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				addCritter(cloud);
			}
		}
	}

	public void addOwl()
	{
		critters.Add(new Owl(new Vector2(Game1.random.Next(64, map.Layers[0].LayerWidth * 64 - 64), -128f)));
	}

	public void setFireplace(bool on, int tileLocationX, int tileLocationY, bool playSound = true, int xOffset = 0, int yOffset = 0)
	{
		int id = 944468 + tileLocationX * 1000 + tileLocationY;
		string text = $"{NameOrUniqueName}_Fireplace_{tileLocationX}_{tileLocationY}";
		if (on)
		{
			if (getTemporarySpriteByID(id) == null)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), new Vector2(tileLocationX, tileLocationY) * 64f + new Vector2(32f, -32f) + new Vector2(xOffset, yOffset), flipped: false, 0f, Color.White)
				{
					interval = 50f,
					totalNumberOfLoops = 99999,
					animationLength = 4,
					lightId = text + "_1",
					id = id,
					lightRadius = 2f,
					scale = 4f,
					layerDepth = ((float)tileLocationY + 1.1f) * 64f / 10000f
				});
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), new Vector2(tileLocationX + 1, tileLocationY) * 64f + new Vector2(-16f, -32f) + new Vector2(xOffset, yOffset), flipped: false, 0f, Color.White)
				{
					delayBeforeAnimationStart = 10,
					interval = 50f,
					totalNumberOfLoops = 99999,
					animationLength = 4,
					lightId = text + "_2",
					id = id,
					lightRadius = 2f,
					scale = 4f,
					layerDepth = ((float)tileLocationY + 1.1f) * 64f / 10000f
				});
				if (playSound && Game1.gameMode != 6)
				{
					localSound("fireball");
				}
				AmbientLocationSounds.addSound(new Vector2(tileLocationX, tileLocationY), 1);
			}
		}
		else
		{
			removeTemporarySpritesWithID(id);
			Game1.currentLightSources.Remove(text + "_1");
			Game1.currentLightSources.Remove(text + "_2");
			if (playSound)
			{
				localSound("fireball");
			}
			AmbientLocationSounds.removeSound(new Vector2(tileLocationX, tileLocationY));
		}
	}

	public void addWoodpecker(double chance, bool onlyIfOnScreen = false)
	{
		if (Game1.isStartingToGetDarkOut(this) || onlyIfOnScreen || this is Town || this is Desert || !(Game1.random.NextDouble() < chance) || terrainFeatures.Length <= 0)
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			if (Utility.TryGetRandom(terrainFeatures, out var key, out var value) && value is Tree tree)
			{
				WildTreeData data = tree.GetData();
				if (data != null && data.AllowWoodpeckers && tree.growthStage.Value >= 5)
				{
					critters.Add(new Woodpecker(tree, key));
					break;
				}
			}
		}
	}

	public void addSquirrels(double chance, bool onlyIfOnScreen = false)
	{
		if (Game1.isStartingToGetDarkOut(this) || onlyIfOnScreen || this is Farm || this is Town || this is Desert || !(Game1.random.NextDouble() < chance) || terrainFeatures.Length <= 0)
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			if (!Utility.TryGetRandom(terrainFeatures, out var key, out var value) || !(value is Tree tree) || tree.growthStage.Value < 5 || tree.stump.Value)
			{
				continue;
			}
			int num = Game1.random.Next(4, 7);
			bool flag = Game1.random.NextBool();
			bool flag2 = true;
			for (int j = 0; j < num; j++)
			{
				key.X += (flag ? 1 : (-1));
				if (!CanSpawnCharacterHere(key))
				{
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				critters.Add(new Squirrel(key, flag));
				break;
			}
		}
	}

	public void addBunnies(double chance, bool onlyIfOnScreen = false)
	{
		if (onlyIfOnScreen || this is Farm || this is Desert || !(Game1.random.NextDouble() < chance) || largeTerrainFeatures == null)
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			int index = Game1.random.Next(largeTerrainFeatures.Count);
			if (largeTerrainFeatures.Count <= 0 || !(largeTerrainFeatures[index] is Bush))
			{
				continue;
			}
			Vector2 tile = largeTerrainFeatures[index].Tile;
			int num = Game1.random.Next(5, 12);
			bool flag = Game1.random.NextBool();
			bool flag2 = true;
			for (int j = 0; j < num; j++)
			{
				tile.X += (flag ? 1 : (-1));
				if (!largeTerrainFeatures[index].getBoundingBox().Intersects(new Microsoft.Xna.Framework.Rectangle((int)tile.X * 64, (int)tile.Y * 64, 64, 64)) && !CanSpawnCharacterHere(tile))
				{
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				critters.Add(new Rabbit(this, tile, flag));
				break;
			}
		}
	}

	public void addOpossums(double chance, bool onlyIfOnScreen = false)
	{
		if (onlyIfOnScreen || this is Farm || this is Desert || !(Game1.random.NextDouble() < chance) || largeTerrainFeatures == null)
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			int index = Game1.random.Next(largeTerrainFeatures.Count);
			if (largeTerrainFeatures.Count <= 0 || !(largeTerrainFeatures[index] is Bush))
			{
				continue;
			}
			Vector2 vector = largeTerrainFeatures[index].Tile;
			int num = Game1.random.Next(5, 12);
			bool flag = Game1.player.Position.X > (float)((this is BusStop) ? 704 : 64);
			bool flag2 = true;
			for (int j = 0; j < num; j++)
			{
				vector.X += (flag ? 1 : (-1));
				if (!largeTerrainFeatures[index].getBoundingBox().Intersects(new Microsoft.Xna.Framework.Rectangle((int)vector.X * 64, (int)vector.Y * 64, 64, 64)) && !CanSpawnCharacterHere(vector))
				{
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				if (this is BusStop && Game1.random.NextDouble() < 0.5)
				{
					vector = new Vector2((Game1.player.Tile.X < 26f) ? 36 : 16, 23 + Game1.random.Next(2));
				}
				critters.Add(new Opossum(this, vector, flag));
				break;
			}
		}
	}

	public void instantiateCrittersList()
	{
		if (critters == null)
		{
			critters = new List<Critter>();
		}
	}

	public void addCritter(Critter c)
	{
		critters?.Add(c);
	}

	public void addButterflies(double chance, bool onlyIfOnScreen = false)
	{
		Season season = GetSeason();
		bool islandButterfly = InIslandContext();
		bool flag = season == Season.Summer && Game1.isDarkOut(this);
		if (Game1.timeOfDay >= 1500 && !flag && season != Season.Winter)
		{
			return;
		}
		if (season == Season.Spring || season == Season.Summer || (season == Season.Winter && Game1.dayOfMonth % 7 == 0 && Game1.isDarkOut(this)))
		{
			chance = Math.Min(0.8, chance * 1.5);
			while (Game1.random.NextDouble() < chance)
			{
				Vector2 randomTile = getRandomTile();
				if (onlyIfOnScreen && Utility.isOnScreen(randomTile * 64f, 64))
				{
					continue;
				}
				if (flag)
				{
					critters.Add(new Firefly(randomTile));
				}
				else
				{
					critters.Add(new Butterfly(this, randomTile, islandButterfly));
				}
				while (Game1.random.NextDouble() < 0.4)
				{
					if (flag)
					{
						critters.Add(new Firefly(randomTile + new Vector2(Game1.random.Next(-2, 3), Game1.random.Next(-2, 3))));
					}
					else
					{
						critters.Add(new Butterfly(this, randomTile + new Vector2(Game1.random.Next(-2, 3), Game1.random.Next(-2, 3)), islandButterfly));
					}
				}
			}
		}
		if (Game1.timeOfDay < 1700)
		{
			tryAddPrismaticButterfly();
		}
	}

	public void tryAddPrismaticButterfly()
	{
		if (!Game1.player.hasBuff("statue_of_blessings_6"))
		{
			return;
		}
		foreach (Critter critter in critters)
		{
			if (critter is Butterfly { isPrismatic: not false })
			{
				return;
			}
		}
		Random random = Utility.CreateDaySaveRandom(Game1.player.UniqueMultiplayerID % 10000);
		string[] array = new string[7] { "Forest", "Town", "Beach", "Mountain", "Woods", "BusStop", "Backwoods" };
		string text = array[random.Next(array.Length)];
		if (text.Equals("Beach") && Name.Equals("BeachNightMarket"))
		{
			text = "BeachNightMarket";
		}
		if (!Name.Equals(text))
		{
			return;
		}
		Vector2 randomTile = getRandomTile(random);
		for (int i = 0; i < 32; i++)
		{
			if (isTileLocationOpen(randomTile))
			{
				break;
			}
			randomTile = getRandomTile(random);
		}
		critters.Add(new Butterfly(this, randomTile, islandButterfly: false, forceSummerButterfly: false, 394, prismatic: true)
		{
			stayInbounds = true
		});
	}

	public void addBirdies(double chance, bool onlyIfOnScreen = false)
	{
		if (Game1.timeOfDay >= 1500 || this is Desert || this is Railroad || this is Farm)
		{
			return;
		}
		Season season = GetSeason();
		if (season == Season.Summer)
		{
			return;
		}
		while (Game1.random.NextDouble() < chance)
		{
			int num = Game1.random.Next(1, 4);
			bool flag = false;
			int num2 = 0;
			while (!flag && num2 < 5)
			{
				Vector2 randomTile = getRandomTile();
				if (!onlyIfOnScreen || !Utility.isOnScreen(randomTile * 64f, 64))
				{
					Microsoft.Xna.Framework.Rectangle area = new Microsoft.Xna.Framework.Rectangle((int)randomTile.X - 2, (int)randomTile.Y - 2, 5, 5);
					if (isAreaClear(area))
					{
						List<Critter> list = new List<Critter>();
						int num3 = ((season == Season.Fall) ? 45 : 25);
						if (Game1.random.NextBool() && Game1.MasterPlayer.mailReceived.Contains("Farm_Eternal"))
						{
							num3 = ((season == Season.Fall) ? 135 : 125);
						}
						if (num3 == 25 && Game1.random.NextDouble() < 0.05)
						{
							num3 = 165;
						}
						for (int i = 0; i < num; i++)
						{
							list.Add(new Birdie(-100, -100, num3));
						}
						addCrittersStartingAtTile(randomTile, list);
						flag = true;
					}
				}
				num2++;
			}
		}
	}

	public void addJumperFrog(Vector2 tileLocation)
	{
		critters?.Add(new Frog(tileLocation));
	}

	public void addFrog()
	{
		if (!IsRainingHere() || IsWinterHere())
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			Vector2 randomTile = getRandomTile();
			if (!isWaterTile((int)randomTile.X, (int)randomTile.Y) || !isWaterTile((int)randomTile.X, (int)randomTile.Y - 1) || doesTileHaveProperty((int)randomTile.X, (int)randomTile.Y, "Passable", "Buildings") != null)
			{
				continue;
			}
			int num = 10;
			bool flag = Game1.random.NextBool();
			for (int j = 0; j < num; j++)
			{
				randomTile.X += (flag ? 1 : (-1));
				if (isTileOnMap((int)randomTile.X, (int)randomTile.Y) && !isWaterTile((int)randomTile.X, (int)randomTile.Y))
				{
					critters.Add(new Frog(randomTile, waterLeaper: true, flag));
					return;
				}
			}
		}
	}

	public void checkForSpecialCharacterIconAtThisTile(Vector2 tileLocation)
	{
		currentEvent?.checkForSpecialCharacterIconAtThisTile(tileLocation);
	}

	private void addCrittersStartingAtTile(Vector2 tile, List<Critter> crittersToAdd)
	{
		if (crittersToAdd == null)
		{
			return;
		}
		int num = 0;
		HashSet<Vector2> hashSet = new HashSet<Vector2>();
		while (crittersToAdd.Count > 0 && num < 20)
		{
			if (hashSet.Contains(tile))
			{
				tile = Utility.getTranslatedVector2(tile, Game1.random.Next(4), 1f);
			}
			else
			{
				if (CanItemBePlacedHere(tile))
				{
					Critter critter = crittersToAdd.Last();
					critter.position = tile * 64f;
					critter.startingPosition = tile * 64f;
					critters.Add(critter);
					crittersToAdd.RemoveAt(crittersToAdd.Count - 1);
				}
				tile = Utility.getTranslatedVector2(tile, Game1.random.Next(4), 1f);
				hashSet.Add(tile);
			}
			num++;
		}
	}

	public bool isAreaClear(Microsoft.Xna.Framework.Rectangle area)
	{
		foreach (Vector2 vector in area.GetVectors())
		{
			if (!CanItemBePlacedHere(vector))
			{
				return false;
			}
		}
		return true;
	}

	public void performGreenRainUpdate()
	{
		if (!IsGreenRainingHere() || !IsOutdoors || !(GetData()?.CanHaveGreenRainSpawns ?? true))
		{
			return;
		}
		Layer layer = map.GetLayer("Paths");
		if (layer != null)
		{
			for (int i = 0; i < layer.LayerWidth; i++)
			{
				for (int j = 0; j < layer.LayerHeight; j++)
				{
					Tile tile = layer.Tiles[i, j];
					if (tile != null && tile.TileIndexProperties.ContainsKey("GreenRain"))
					{
						Vector2 vector = new Vector2(i, j);
						if (!IsTileOccupiedBy(vector))
						{
							terrainFeatures.Add(vector, (this is Forest) ? new Tree("12", 5, isGreenRainTemporaryTree: true) : new Tree((10 + (Game1.random.NextBool(0.1) ? 2 : Game1.random.Choose(1, 0))).ToString(), 5, isGreenRainTemporaryTree: true));
						}
					}
				}
			}
		}
		if (this is Town)
		{
			return;
		}
		string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces("Trees");
		for (int k = 0; k < mapPropertySplitBySpaces.Length; k += 3)
		{
			if (!ArgUtility.TryGetVector2(mapPropertySplitBySpaces, k, out var value, out var error, integerOnly: false, "Vector2 position") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces, k + 2, out var value2, out error, "int treeType"))
			{
				LogMapPropertyError("Trees", mapPropertySplitBySpaces, error);
				continue;
			}
			float chance = (IsFarm ? 0.5f : 1f);
			if (Game1.random.NextBool(chance) && !IsTileOccupiedBy(value))
			{
				terrainFeatures.Add(value, new Tree((value2 + 1).ToString(), 5));
			}
		}
		TerrainFeature[] array = terrainFeatures.Values.ToArray();
		for (int l = 0; l < array.Length; l++)
		{
			if (array[l] is Tree tree)
			{
				tree.onGreenRainDay();
			}
		}
		int num = map.Layers[0].LayerWidth * map.Layers[0].LayerHeight;
		spawnWeedsAndStones(num / 16, weedsOnly: true, spawnFromOldWeeds: false);
		spawnWeedsAndStones(num / 8, weedsOnly: true);
		for (int m = 0; m < num / 4; m++)
		{
			Vector2 randomTile = getRandomTile();
			if (objects.TryGetValue(randomTile, out var value3) && value3.IsWeeds() && objects.TryGetValue(randomTile + new Vector2(1f, 0f), out var value4) && value4.IsWeeds() && objects.TryGetValue(randomTile + new Vector2(1f, 1f), out var value5) && value5.IsWeeds() && objects.TryGetValue(randomTile + new Vector2(0f, 1f), out var value6) && value6.IsWeeds())
			{
				objects.Remove(randomTile);
				objects.Remove(randomTile + new Vector2(1f, 0f));
				objects.Remove(randomTile + new Vector2(1f, 1f));
				objects.Remove(randomTile + new Vector2(0f, 1f));
				resourceClumps.Add(new ResourceClump(44 + Game1.random.Choose(2, 0), 2, 2, randomTile, 4, "TileSheets\\Objects_2"));
			}
		}
	}

	public void performDayAfterGreenRainUpdate()
	{
		KeyValuePair<Vector2, Object>[] array = objects.Pairs.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<Vector2, Object> keyValuePair = array[i];
			if (keyValuePair.Value.Name.Contains("GreenRainWeeds"))
			{
				objects.Remove(keyValuePair.Key);
			}
		}
		resourceClumps.RemoveWhere((ResourceClump clump) => clump.IsGreenRainBush());
		KeyValuePair<Vector2, TerrainFeature>[] array2 = terrainFeatures.Pairs.ToArray();
		for (int i = 0; i < array2.Length; i++)
		{
			KeyValuePair<Vector2, TerrainFeature> keyValuePair2 = array2[i];
			if (!(keyValuePair2.Value is Tree tree))
			{
				continue;
			}
			if (this is Town)
			{
				if (tree.isTemporaryGreenRainTree.Value)
				{
					terrainFeatures.Remove(keyValuePair2.Key);
				}
			}
			else
			{
				tree.onGreenRainDay(undo: true);
			}
		}
	}

	public Vector2 getRandomTile(Random r = null)
	{
		if (r == null)
		{
			r = Game1.random;
		}
		return new Vector2(r.Next(Map.Layers[0].LayerWidth), r.Next(Map.Layers[0].LayerHeight));
	}

	public void setUpLocationSpecificFlair()
	{
		indoorLightingColor = new Color(100, 120, 30);
		indoorLightingNightColor = new Color(150, 150, 30);
		if (TryGetAmbientLightFromMap(out var color))
		{
			if (color == Color.White)
			{
				color = Color.Black;
			}
			indoorLightingColor = color;
			if (TryGetAmbientLightFromMap(out var color2, "AmbientNightLight"))
			{
				indoorLightingNightColor = color2;
			}
			else
			{
				indoorLightingNightColor = indoorLightingColor;
			}
		}
		if (!isOutdoors.Value && !(this is FarmHouse) && !(this is IslandFarmHouse))
		{
			Game1.ambientLight = indoorLightingColor;
		}
		Game1.screenGlow = false;
		if (!IsOutdoors && IsGreenRainingHere() && !InIslandContext() && IsRainingHere())
		{
			indoorLightingColor = new Color(123, 0, 96);
			indoorLightingNightColor = new Color(185, 40, 119);
			Game1.screenGlowOnce(new Color(0, 255, 50) * 0.5f, hold: true, 1f);
		}
		string value = name.Value;
		if (value == null)
		{
			return;
		}
		switch (value.Length)
		{
		case 9:
			switch (value[0])
			{
			case 'J':
				if (value == "JoshHouse" && Game1.isGreenRain)
				{
					temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(386, 334, 36, 28), 40f, 3, 999999, new Vector2(246.5f, 317f) * 4f, flicker: false, flipped: false, 0.136001f, 0f, Color.White, 2f, 0f, 0f, 0f));
				}
				break;
			case 'Q':
				if (value == "QiNutRoom")
				{
					Game1.ambientLight = indoorLightingColor;
				}
				break;
			case 'L':
				if (value == "LeahHouse")
				{
					NPC characterFromName10 = Game1.getCharacterFromName("Leah");
					if (IsFallHere() || IsWinterHere() || IsRainingHere())
					{
						setFireplace(on: true, 11, 4, playSound: false);
					}
					if (characterFromName10 != null && characterFromName10.currentLocation == this && !characterFromName10.isDivorcedFrom(Game1.player))
					{
						string path7 = Game1.random.Next(3) switch
						{
							0 => "Strings\\SpeechBubbles:LeahHouse_Leah_Greeting1", 
							1 => "Strings\\SpeechBubbles:LeahHouse_Leah_Greeting2", 
							_ => "Strings\\SpeechBubbles:LeahHouse_Leah_Greeting3", 
						};
						characterFromName10.faceTowardFarmerForPeriod(3000, 15, faceAway: false, Game1.player);
						characterFromName10.showTextAboveHead(Game1.content.LoadString(path7, Game1.player.Name));
					}
				}
				break;
			}
			break;
		case 6:
			switch (value[1])
			{
			case 'u':
				if (value == "Summit")
				{
					Game1.ambientLight = Color.Black;
				}
				break;
			case 'a':
				if (!(value == "Saloon"))
				{
					break;
				}
				if (Game1.timeOfDay >= 1700 || IsGreenRainingHere())
				{
					setFireplace(on: true, 22, 17, playSound: false);
				}
				if (Game1.random.NextDouble() < 0.25)
				{
					NPC characterFromName12 = Game1.getCharacterFromName("Gus");
					if (characterFromName12 != null && characterFromName12.TilePoint.Y == 18 && characterFromName12.currentLocation == this)
					{
						string text4 = Game1.random.Next(5) switch
						{
							0 => "Greeting", 
							1 => IsSummerHere() ? "Summer" : "NotSummer", 
							2 => IsSnowingHere() ? "Snowing1" : "NotSnowing1", 
							3 => IsRainingHere() ? "Raining" : "NotRaining", 
							_ => IsSnowingHere() ? "Snowing2" : "NotSnowing2", 
						};
						if (Game1.random.NextDouble() < 0.001)
						{
							text4 = "RareGreeting";
						}
						characterFromName12.showTextAboveHead(Game1.content.LoadString("Strings\\SpeechBubbles:Saloon_Gus_" + text4));
					}
				}
				if (getCharacterFromName("Gus") == null && Game1.IsVisitingIslandToday("Gus"))
				{
					temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors2,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(129, 210, 13, 16),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(129f, 210f),
						interval = 50000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(11f, 18f) * 64f + new Vector2(3f, 0f) * 4f,
						scale = 4f,
						layerDepth = 0.1281f,
						id = 777
					});
				}
				if (Game1.dayOfMonth % 7 == 0 && NetWorldState.checkAnywhereForWorldStateID("saloonSportsRoom") && Game1.timeOfDay < 1500)
				{
					Texture2D texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(368, 336, 19, 14),
						animationLength = 7,
						sourceRectStartingPos = new Vector2(368f, 336f),
						interval = 5000f,
						totalNumberOfLoops = 99999,
						position = new Vector2(34f, 3f) * 64f + new Vector2(7f, 13f) * 4f,
						scale = 4f,
						layerDepth = 0.0401f,
						id = 2400
					});
				}
				break;
			}
			break;
		case 12:
			switch (value[0])
			{
			case 'L':
				if (value == "LeoTreeHouse")
				{
					temporarySprites.Add(new EmilysParrot(new Vector2(88f, 224f))
					{
						layerDepth = 1f,
						id = 5858585
					});
					temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(71, 334, 12, 11), new Vector2(304f, 32f), flipped: false, 0f, Color.White)
					{
						layerDepth = 0.001f,
						interval = 700f,
						animationLength = 3,
						totalNumberOfLoops = 999999,
						scale = 4f
					});
					temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(47, 334, 12, 11), new Vector2(112f, -25.6f), flipped: true, 0f, Color.White)
					{
						layerDepth = 0.001f,
						interval = 300f,
						animationLength = 3,
						totalNumberOfLoops = 999999,
						scale = 4f
					});
					temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(71, 334, 12, 11), new Vector2(224f, -25.6f), flipped: false, 0f, Color.White)
					{
						layerDepth = 0.001f,
						interval = 800f,
						animationLength = 3,
						totalNumberOfLoops = 999999,
						scale = 4f
					});
				}
				break;
			case 'S':
				if (!(value == "ScienceHouse"))
				{
					break;
				}
				if (Game1.random.NextBool() && Game1.player.currentLocation != null && Game1.player.currentLocation.isOutdoors.Value)
				{
					NPC characterFromName3 = Game1.getCharacterFromName("Robin");
					if (characterFromName3 != null && characterFromName3.TilePoint.Y == 18)
					{
						string path2 = Game1.random.Next(5) switch
						{
							0 => IsRainingHere() ? "Strings\\SpeechBubbles:ScienceHouse_Robin_Raining1" : "Strings\\SpeechBubbles:ScienceHouse_Robin_NotRaining1", 
							1 => IsSnowingHere() ? "Strings\\SpeechBubbles:ScienceHouse_Robin_Snowing" : "Strings\\SpeechBubbles:ScienceHouse_Robin_NotSnowing", 
							2 => (Game1.player.getFriendshipHeartLevelForNPC("Robin") > 4) ? "Strings\\SpeechBubbles:ScienceHouse_Robin_CloseFriends" : "Strings\\SpeechBubbles:ScienceHouse_Robin_NotCloseFriends", 
							3 => IsRainingHere() ? "Strings\\SpeechBubbles:ScienceHouse_Robin_Raining2" : "Strings\\SpeechBubbles:ScienceHouse_Robin_NotRaining2", 
							_ => "Strings\\SpeechBubbles:ScienceHouse_Robin_Greeting", 
						};
						if (Game1.random.NextDouble() < 0.001)
						{
							path2 = "Strings\\SpeechBubbles:ScienceHouse_Robin_RareGreeting";
						}
						characterFromName3.showTextAboveHead(Game1.content.LoadString(path2, Game1.player.Name));
					}
				}
				if (getCharacterFromName("Robin") == null && Game1.IsVisitingIslandToday("Robin"))
				{
					temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors2,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(129, 210, 13, 16),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(129f, 210f),
						interval = 50000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(7f, 18f) * 64f + new Vector2(3f, 0f) * 4f,
						scale = 4f,
						layerDepth = 0.1281f,
						id = 777
					});
				}
				break;
			case 'E':
				if (value == "ElliottHouse")
				{
					NPC characterFromName2 = Game1.getCharacterFromName("Elliott");
					if (characterFromName2 != null && characterFromName2.currentLocation == this && !characterFromName2.isDivorcedFrom(Game1.player))
					{
						string path = Game1.random.Next(3) switch
						{
							0 => "Strings\\SpeechBubbles:ElliottHouse_Elliott_Greeting1", 
							1 => "Strings\\SpeechBubbles:ElliottHouse_Elliott_Greeting2", 
							_ => "Strings\\SpeechBubbles:ElliottHouse_Elliott_Greeting3", 
						};
						characterFromName2.faceTowardFarmerForPeriod(3000, 15, faceAway: false, Game1.player);
						characterFromName2.showTextAboveHead(Game1.content.LoadString(path, Game1.player.Name));
					}
				}
				break;
			}
			break;
		case 7:
			switch (value[0])
			{
			case 'S':
				if (!(value == "Sunroom"))
				{
					break;
				}
				indoorLightingColor = new Color(0, 0, 0);
				AmbientLocationSounds.addSound(new Vector2(3f, 4f), 0);
				if (largeTerrainFeatures.Count == 0)
				{
					Bush bush = new Bush(new Vector2(6f, 7f), 3, this, -999);
					bush.loadSprite();
					bush.health = 99f;
					largeTerrainFeatures.Add(bush);
				}
				if (!IsRainingHere())
				{
					critters = new List<Critter>();
					critters.Add(new Butterfly(this, getRandomTile()).setStayInbounds(stayInbounds: true));
					while (Game1.random.NextBool())
					{
						critters.Add(new Butterfly(this, getRandomTile()).setStayInbounds(stayInbounds: true));
					}
				}
				break;
			case 'B':
				if (!(value == "BugLand"))
				{
					break;
				}
				if (!Game1.player.hasDarkTalisman && CanItemBePlacedHere(new Vector2(31f, 5f)))
				{
					overlayObjects.Add(new Vector2(31f, 5f), new Chest(new List<Item>
					{
						new SpecialItem(6)
					}, new Vector2(31f, 5f))
					{
						Tint = Color.Gray
					});
				}
				{
					foreach (NPC character in characters)
					{
						if (!(character is Grub grub))
						{
							if (character is Fly fly)
							{
								fly.setHard();
							}
						}
						else
						{
							grub.setHard();
						}
					}
					break;
				}
			}
			break;
		case 8:
			switch (value[0])
			{
			case 'W':
				if (value == "WitchHut" && Game1.player.mailReceived.Contains("cursed_doll") && !farmers.Any())
				{
					characters.Clear();
					uint num = Game1.stats.Get("childrenTurnedToDoves");
					addCharacter(new Bat(new Vector2(7f, 6f) * 64f, -666));
					if (num > 1)
					{
						addCharacter(new Bat(new Vector2(4f, 7f) * 64f, -666));
					}
					if (num > 2)
					{
						addCharacter(new Bat(new Vector2(10f, 7f) * 64f, -666));
					}
					for (int i = 4; i <= num; i++)
					{
						addCharacter(new Bat(Utility.getRandomPositionInThisRectangle(new Microsoft.Xna.Framework.Rectangle(1, 4, 13, 4), Game1.random) * 64f + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)), -666));
					}
				}
				break;
			case 'H':
			{
				if (!(value == "Hospital"))
				{
					break;
				}
				indoorLightingColor = new Color(100, 100, 60);
				if (!Game1.random.NextBool())
				{
					break;
				}
				NPC characterFromName8 = Game1.getCharacterFromName("Maru");
				if (characterFromName8 != null && characterFromName8.currentLocation == this && !characterFromName8.isDivorcedFrom(Game1.player))
				{
					string path5 = Game1.random.Next(5) switch
					{
						0 => "Strings\\SpeechBubbles:Hospital_Maru_Greeting1", 
						1 => "Strings\\SpeechBubbles:Hospital_Maru_Greeting2", 
						2 => "Strings\\SpeechBubbles:Hospital_Maru_Greeting3", 
						3 => "Strings\\SpeechBubbles:Hospital_Maru_Greeting4", 
						_ => "Strings\\SpeechBubbles:Hospital_Maru_Greeting5", 
					};
					if (Game1.player.spouse == "Maru")
					{
						path5 = "Strings\\SpeechBubbles:Hospital_Maru_Spouse";
						characterFromName8.showTextAboveHead(Game1.content.LoadString(path5), SpriteText.color_Red);
					}
					else
					{
						characterFromName8.showTextAboveHead(Game1.content.LoadString(path5));
					}
				}
				break;
			}
			case 'J':
				if (!(value == "JojaMart"))
				{
					break;
				}
				indoorLightingColor = new Color(0, 0, 0);
				if (Game1.random.NextBool())
				{
					NPC characterFromName9 = Game1.getCharacterFromName("Morris");
					if (characterFromName9 != null && characterFromName9.currentLocation == this)
					{
						string path6 = "Strings\\SpeechBubbles:JojaMart_Morris_Greeting";
						characterFromName9.showTextAboveHead(Game1.content.LoadString(path6));
					}
				}
				break;
			case 'S':
				if (!(value == "SeedShop"))
				{
					break;
				}
				setFireplace(on: true, 25, 13, playSound: false);
				if (Game1.random.NextBool() && Game1.player.TilePoint.Y > 10)
				{
					NPC characterFromName7 = Game1.getCharacterFromName("Pierre");
					if (characterFromName7 != null && characterFromName7.TilePoint.Y == 17 && characterFromName7.currentLocation == this)
					{
						string text3 = Game1.random.Next(5) switch
						{
							0 => IsWinterHere() ? "Winter" : "NotWinter", 
							1 => IsSummerHere() ? "Summer" : "NotSummer", 
							2 => "Greeting1", 
							3 => "Greeting2", 
							_ => IsRainingHere() ? "Raining" : "NotRaining", 
						};
						if (Game1.random.NextDouble() < 0.001)
						{
							text3 = "RareGreeting";
						}
						string format = Game1.content.LoadString("Strings\\SpeechBubbles:SeedShop_Pierre_" + text3);
						characterFromName7.showTextAboveHead(string.Format(format, Game1.player.Name));
					}
				}
				if (getCharacterFromName("Pierre") == null && Game1.IsVisitingIslandToday("Pierre"))
				{
					temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors2,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(129, 210, 13, 16),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(129f, 210f),
						interval = 50000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(5f, 17f) * 64f + new Vector2(3f, 0f) * 4f,
						scale = 4f,
						layerDepth = 0.1217f,
						id = 777
					});
				}
				if (getCharacterFromName("Abigail") != null && getCharacterFromName("Abigail").TilePoint.Equals(new Point(3, 6)))
				{
					temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(167, 1714, 19, 14), 100f, 3, 999999, new Vector2(2f, 3f) * 64f + new Vector2(7f, 12f) * 4f, flicker: false, flipped: false, 0.0002f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 688
					});
				}
				break;
			}
			break;
		case 10:
			switch (value[0])
			{
			case 'H':
				if (value == "HaleyHouse" && Game1.player.eventsSeen.Contains("463391") && Game1.player.spouse != "Emily")
				{
					temporarySprites.Add(new EmilysParrot(new Vector2(912f, 160f)));
				}
				break;
			case 'A':
				if (!(value == "AnimalShop"))
				{
					break;
				}
				setFireplace(on: true, 3, 14, playSound: false);
				if (Game1.random.NextBool())
				{
					NPC characterFromName6 = Game1.getCharacterFromName("Marnie");
					if (characterFromName6 != null && characterFromName6.TilePoint.Y == 14)
					{
						string path4 = Game1.random.Next(5) switch
						{
							0 => "Strings\\SpeechBubbles:AnimalShop_Marnie_Greeting1", 
							1 => "Strings\\SpeechBubbles:AnimalShop_Marnie_Greeting2", 
							2 => (Game1.player.getFriendshipHeartLevelForNPC("Marnie") > 4) ? "Strings\\SpeechBubbles:AnimalShop_Marnie_CloseFriends" : "Strings\\SpeechBubbles:AnimalShop_Marnie_NotCloseFriends", 
							3 => IsRainingHere() ? "Strings\\SpeechBubbles:AnimalShop_Marnie_Raining" : "Strings\\SpeechBubbles:AnimalShop_Marnie_NotRaining", 
							_ => "Strings\\SpeechBubbles:AnimalShop_Marnie_Greeting3", 
						};
						if (Game1.random.NextDouble() < 0.001)
						{
							path4 = "Strings\\SpeechBubbles:AnimalShop_Marnie_RareGreeting";
						}
						characterFromName6.showTextAboveHead(Game1.content.LoadString(path4, Game1.player.Name, Game1.player.farmName));
					}
				}
				if (getCharacterFromName("Marnie") == null && Game1.IsVisitingIslandToday("Marnie"))
				{
					temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors2,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(129, 210, 13, 16),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(129f, 210f),
						interval = 50000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(13f, 14f) * 64f + new Vector2(3f, 0f) * 4f,
						scale = 4f,
						layerDepth = 0.1025f,
						id = 777
					});
				}
				if (Game1.netWorldState.Value.hasWorldStateID("m_painting0"))
				{
					temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(25, 1925, 25, 23),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(25f, 1925f),
						interval = 5000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(16f, 1f) * 64f + new Vector2(3f, 1f) * 4f,
						scale = 4f,
						layerDepth = 0.1f,
						id = 777
					});
				}
				else if (Game1.netWorldState.Value.hasWorldStateID("m_painting1"))
				{
					temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 1925, 25, 23),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(0f, 1925f),
						interval = 5000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(16f, 1f) * 64f + new Vector2(3f, 1f) * 4f,
						scale = 4f,
						layerDepth = 0.1f,
						id = 777
					});
				}
				else if (Game1.netWorldState.Value.hasWorldStateID("m_painting2"))
				{
					temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 1948, 25, 24),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(0f, 1948f),
						interval = 5000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(16f, 1f) * 64f + new Vector2(3f, 1f) * 4f,
						scale = 4f,
						layerDepth = 0.1f,
						id = 777
					});
				}
				break;
			case 'B':
				if (value == "Blacksmith")
				{
					AmbientLocationSounds.addSound(new Vector2(9f, 10f), 2);
					AmbientLocationSounds.changeSpecificVariable("Frequency", 2f, 2);
				}
				break;
			case 'S':
				if (!(value == "SandyHouse"))
				{
					break;
				}
				indoorLightingColor = new Color(0, 0, 0);
				if (Game1.random.NextBool())
				{
					NPC characterFromName4 = Game1.getCharacterFromName("Sandy");
					if (characterFromName4 != null && characterFromName4.currentLocation == this)
					{
						string path3 = Game1.random.Next(5) switch
						{
							0 => "Strings\\SpeechBubbles:SandyHouse_Sandy_Greeting1", 
							1 => "Strings\\SpeechBubbles:SandyHouse_Sandy_Greeting2", 
							2 => "Strings\\SpeechBubbles:SandyHouse_Sandy_Greeting3", 
							3 => "Strings\\SpeechBubbles:SandyHouse_Sandy_Greeting4", 
							_ => "Strings\\SpeechBubbles:SandyHouse_Sandy_Greeting5", 
						};
						characterFromName4.showTextAboveHead(Game1.content.LoadString(path3));
					}
				}
				break;
			case 'M':
				if (value == "ManorHouse")
				{
					indoorLightingColor = new Color(150, 120, 50);
					NPC characterFromName5 = Game1.getCharacterFromName("Lewis");
					if (characterFromName5 != null && characterFromName5.currentLocation == this)
					{
						string text2 = ((Game1.timeOfDay < 1200) ? "Morning" : ((Game1.timeOfDay < 1700) ? "Afternoon" : "Evening"));
						characterFromName5.faceTowardFarmerForPeriod(3000, 15, faceAway: false, Game1.player);
						characterFromName5.showTextAboveHead(Game1.content.LoadString("Strings\\SpeechBubbles:ManorHouse_Lewis_" + text2));
					}
				}
				break;
			case 'G':
				if (value == "Greenhouse" && Game1.isDarkOut(this))
				{
					Game1.ambientLight = Game1.outdoorLight;
				}
				break;
			}
			break;
		case 13:
			if (value == "LewisBasement")
			{
				if (farmers.Count == 0)
				{
					characters.Clear();
				}
				Vector2 vector = new Vector2(17f, 15f);
				overlayObjects.Remove(vector);
				Object obj = ItemRegistry.Create<Object>("(O)789");
				obj.questItem.Value = true;
				obj.TileLocation = vector;
				obj.IsSpawnedObject = true;
				overlayObjects.Add(vector, obj);
			}
			break;
		case 17:
			if (value == "AbandonedJojaMart" && !Game1.MasterPlayer.hasOrWillReceiveMail("ccMovieTheater"))
			{
				Point point = new Point(8, 8);
				Game1.currentLightSources.Add(new LightSource("AbandonedJojaMart", 4, new Vector2(point.X * 64, point.Y * 64), 1f, LightSource.LightContext.None, 0L, NameOrUniqueName));
				temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(point.X * 64, point.Y * 64), Color.White)
				{
					layerDepth = 1f,
					interval = 50f,
					motion = new Vector2(1f, 0f),
					acceleration = new Vector2(-0.005f, 0f)
				});
				temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(point.X * 64 - 12, point.Y * 64 - 12), Color.White)
				{
					scale = 0.75f,
					layerDepth = 1f,
					interval = 50f,
					motion = new Vector2(1f, 0f),
					acceleration = new Vector2(-0.005f, 0f),
					delayBeforeAnimationStart = 50
				});
				temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(point.X * 64 - 12, point.Y * 64 + 12), Color.White)
				{
					layerDepth = 1f,
					interval = 50f,
					motion = new Vector2(1f, 0f),
					acceleration = new Vector2(-0.005f, 0f),
					delayBeforeAnimationStart = 100
				});
				temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(point.X * 64, point.Y * 64), Color.White)
				{
					layerDepth = 1f,
					scale = 0.75f,
					interval = 50f,
					motion = new Vector2(1f, 0f),
					acceleration = new Vector2(-0.005f, 0f),
					delayBeforeAnimationStart = 150
				});
				if (characters.Count == 0)
				{
					characters.Add(new Junimo(new Vector2(8f, 7f) * 64f, 6));
				}
			}
			break;
		case 15:
			if (value == "CommunityCenter" && this is CommunityCenter && (Game1.isLocationAccessible("CommunityCenter") || currentEvent?.id == "191393"))
			{
				setFireplace(on: true, 31, 8, playSound: false);
				setFireplace(on: true, 32, 8, playSound: false);
				setFireplace(on: true, 33, 8, playSound: false);
			}
			break;
		case 14:
			if (!(value == "AdventureGuild"))
			{
				break;
			}
			setFireplace(on: true, 9, 11, playSound: false);
			if (Game1.random.NextBool())
			{
				NPC characterFromName11 = Game1.getCharacterFromName("Marlon");
				if (characterFromName11 != null)
				{
					string path8 = Game1.random.Next(5) switch
					{
						0 => "Strings\\SpeechBubbles:AdventureGuild_Marlon_Greeting_" + (Game1.player.IsMale ? "Male" : "Female"), 
						1 => "Strings\\SpeechBubbles:AdventureGuild_Marlon_Greeting1", 
						2 => "Strings\\SpeechBubbles:AdventureGuild_Marlon_Greeting2", 
						3 => "Strings\\SpeechBubbles:AdventureGuild_Marlon_Greeting3", 
						_ => "Strings\\SpeechBubbles:AdventureGuild_Marlon_Greeting4", 
					};
					characterFromName11.showTextAboveHead(Game1.content.LoadString(path8));
				}
			}
			break;
		case 16:
		{
			if (!(value == "ArchaeologyHouse"))
			{
				break;
			}
			setFireplace(on: true, 43, 4, playSound: false);
			if (!Game1.random.NextBool() || !Game1.player.hasOrWillReceiveMail("artifactFound"))
			{
				break;
			}
			NPC characterFromName = Game1.getCharacterFromName("Gunther");
			if (characterFromName != null && characterFromName.currentLocation == this)
			{
				string text = Game1.random.Next(5) switch
				{
					0 => "Greeting1", 
					1 => "Greeting2", 
					2 => "Greeting3", 
					3 => "Greeting4", 
					_ => "Greeting5", 
				};
				if (Game1.random.NextDouble() < 0.001)
				{
					text = "RareGreeting";
				}
				characterFromName.showTextAboveHead(Game1.content.LoadString("Strings\\SpeechBubbles:ArchaeologyHouse_Gunther_" + text));
			}
			break;
		}
		case 11:
			break;
		}
	}

	public virtual void hostSetup()
	{
		if (Game1.IsMasterGame && !farmers.Any() && !HasFarmerWatchingBroadcastEventReturningHere())
		{
			interiorDoors.ResetSharedState();
		}
	}

	public virtual void ResetForEvent(Event ev)
	{
		ev.eventPositionTileOffset = Vector2.Zero;
		if (IsOutdoors)
		{
			Game1.ambientLight = (IsRainingHere() ? new Color(255, 200, 80) : Color.White);
		}
	}

	public virtual bool HasFarmerWatchingBroadcastEventReturningHere()
	{
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.locationBeforeForcedEvent.Value != null && allFarmer.locationBeforeForcedEvent.Value == NameOrUniqueName)
			{
				return true;
			}
		}
		return false;
	}

	public void resetForPlayerEntry()
	{
		Game1.updateWeatherIcon();
		Game1.hooks.OnGameLocation_ResetForPlayerEntry(this, delegate
		{
			_madeMapModifications = false;
			if ((!farmers.Any() && !HasFarmerWatchingBroadcastEventReturningHere()) || Game1.player.sleptInTemporaryBed.Value)
			{
				resetSharedState();
			}
			resetLocalState();
			if (!_madeMapModifications)
			{
				_madeMapModifications = true;
				MakeMapModifications();
			}
		});
		Microsoft.Xna.Framework.Rectangle boundingBox = Game1.player.GetBoundingBox();
		foreach (Furniture item in furniture)
		{
			Microsoft.Xna.Framework.Rectangle boundingBox2 = item.GetBoundingBox();
			if (boundingBox2.Intersects(boundingBox) && item.IntersectsForCollision(boundingBox) && !item.isPassable())
			{
				Game1.player.TemporaryPassableTiles.Add(boundingBox2);
			}
		}
	}

	protected virtual void resetLocalState()
	{
		bool flag = Game1.newDaySync.hasInstance();
		if (TryGetMapProperty("ViewportClamp", out var propertyValue))
		{
			try
			{
				int[] array = Utility.parseStringToIntArray(propertyValue);
				Game1.viewportClampArea = new Microsoft.Xna.Framework.Rectangle(array[0] * 64, array[1] * 64, array[2] * 64, array[3] * 64);
			}
			catch (Exception)
			{
				Game1.viewportClampArea = Microsoft.Xna.Framework.Rectangle.Empty;
			}
		}
		else
		{
			Game1.viewportClampArea = Microsoft.Xna.Framework.Rectangle.Empty;
		}
		Game1.elliottPiano = 0;
		Game1.crabPotOverlayTiles.Clear();
		Utility.killAllStaticLoopingSoundCues();
		Game1.player.bridge = null;
		Game1.player.SetOnBridge(val: false);
		if (Game1.CurrentEvent == null && !Name.ContainsIgnoreCase("bath"))
		{
			Game1.player.canOnlyWalk = false;
		}
		if (!(this is Farm))
		{
			temporarySprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.clearOnAreaEntry());
		}
		if (Game1.options != null)
		{
			if (Game1.isOneOfTheseKeysDown(Game1.GetKeyboardState(), Game1.options.runButton))
			{
				Game1.player.setRunning(!Game1.options.autoRun, force: true);
			}
			else
			{
				Game1.player.setRunning(Game1.options.autoRun, force: true);
			}
		}
		Game1.player.mount?.SyncPositionToRider();
		Game1.UpdateViewPort(overrideFreeze: false, Game1.player.StandingPixel);
		Game1.previousViewportPosition = new Vector2(Game1.viewport.X, Game1.viewport.Y);
		Game1.PushUIMode();
		foreach (IClickableMenu onScreenMenu in Game1.onScreenMenus)
		{
			onScreenMenu.gameWindowSizeChanged(new Microsoft.Xna.Framework.Rectangle(Game1.uiViewport.X, Game1.uiViewport.Y, Game1.uiViewport.Width, Game1.uiViewport.Height), new Microsoft.Xna.Framework.Rectangle(Game1.uiViewport.X, Game1.uiViewport.Y, Game1.uiViewport.Width, Game1.uiViewport.Height));
		}
		Game1.PopUIMode();
		ignoreWarps = false;
		if (!flag || Game1.newDaySync.hasFinished())
		{
			if (Game1.player.rightRing.Value != null)
			{
				Game1.player.rightRing.Value.onNewLocation(Game1.player, this);
			}
			if (Game1.player.leftRing.Value != null)
			{
				Game1.player.leftRing.Value.onNewLocation(Game1.player, this);
			}
		}
		forceViewportPlayerFollow = Map.Properties.ContainsKey("ViewportFollowPlayer");
		lastTouchActionLocation = Game1.player.Tile;
		Game1.player.NotifyQuests((Quest quest) => quest.OnWarped(this));
		if (!isOutdoors.Value)
		{
			Game1.player.FarmerSprite.currentStep = "thudStep";
		}
		setUpLocationSpecificFlair();
		_updateAmbientLighting();
		if (!ignoreLights.Value)
		{
			string lightIdPrefix = NameOrUniqueName + "_MapLight_";
			Game1.currentLightSources.RemoveWhere((KeyValuePair<string, LightSource> p) => p.Key.StartsWith(lightIdPrefix));
			string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces("Light");
			for (int num = 0; num < mapPropertySplitBySpaces.Length; num += 3)
			{
				if (!ArgUtility.TryGetPoint(mapPropertySplitBySpaces, num, out var value, out var error, "Point tile") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces, num + 2, out var value2, out error, "int textureIndex"))
				{
					LogMapPropertyError("Light", mapPropertySplitBySpaces, error);
					continue;
				}
				Game1.currentLightSources.Add(new LightSource($"{lightIdPrefix}_{value.X}_{value.Y}", value2, new Vector2(value.X * 64 + 32, value.Y * 64 + 32), 1f, LightSource.LightContext.MapLight, 0L, NameOrUniqueName));
			}
			if (!Game1.isTimeToTurnOffLighting(this) && !Game1.isRaining)
			{
				string[] mapPropertySplitBySpaces2 = GetMapPropertySplitBySpaces("WindowLight");
				for (int num2 = 0; num2 < mapPropertySplitBySpaces2.Length; num2 += 3)
				{
					if (!ArgUtility.TryGetPoint(mapPropertySplitBySpaces2, num2, out var value3, out var error2, "Point tile") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces2, num2 + 2, out var value4, out error2, "int textureIndex"))
					{
						LogMapPropertyError("WindowLight", mapPropertySplitBySpaces2, error2);
						continue;
					}
					Game1.currentLightSources.Add(new LightSource($"{lightIdPrefix}_{value3.X}_{value3.Y}_Window", value4, new Vector2(value3.X * 64 + 32, value3.Y * 64 + 32), 1f, LightSource.LightContext.WindowLight, 0L, NameOrUniqueName));
				}
				foreach (Vector2 lightGlow in lightGlows)
				{
					Game1.currentLightSources.Add(new LightSource($"{lightIdPrefix}_{lightGlow.X}_{lightGlow.Y}_Glow", 6, lightGlow, 1f, LightSource.LightContext.WindowLight, 0L, NameOrUniqueName));
				}
			}
		}
		if (isOutdoors.Value || treatAsOutdoors.Value)
		{
			string[] mapPropertySplitBySpaces3 = GetMapPropertySplitBySpaces("BrookSounds");
			for (int num3 = 0; num3 < mapPropertySplitBySpaces3.Length; num3 += 3)
			{
				if (!ArgUtility.TryGetVector2(mapPropertySplitBySpaces3, num3, out var value5, out var error3, integerOnly: false, "Vector2 tile") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces3, num3 + 2, out var value6, out error3, "int soundId"))
				{
					LogMapPropertyError("BrookSounds", mapPropertySplitBySpaces3, error3);
				}
				else
				{
					AmbientLocationSounds.addSound(value5, value6);
				}
			}
			Game1.randomizeRainPositions();
			Game1.randomizeDebrisWeatherPositions(Game1.debrisWeather);
		}
		foreach (KeyValuePair<Vector2, TerrainFeature> pair in terrainFeatures.Pairs)
		{
			pair.Value.performPlayerEntryAction();
		}
		if (largeTerrainFeatures != null)
		{
			foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
			{
				largeTerrainFeature.performPlayerEntryAction();
			}
		}
		foreach (KeyValuePair<Vector2, Object> pair2 in objects.Pairs)
		{
			pair2.Value.actionOnPlayerEntry();
		}
		if (isOutdoors.Value)
		{
			((FarmerSprite)Game1.player.Sprite).currentStep = "sandyStep";
			tryToAddCritters();
		}
		interiorDoors.ResetLocalState();
		int num4 = Game1.getTrulyDarkTime(this) - 100;
		if (Game1.timeOfDay < num4 && (!IsRainingHere() || name.Equals("SandyHouse")))
		{
			string[] mapPropertySplitBySpaces4 = GetMapPropertySplitBySpaces("DayTiles");
			for (int num5 = 0; num5 < mapPropertySplitBySpaces4.Length; num5 += 4)
			{
				if (!ArgUtility.TryGet(mapPropertySplitBySpaces4, num5, out var value7, out var error4, allowBlank: true, "string layerId") || !ArgUtility.TryGetPoint(mapPropertySplitBySpaces4, num5 + 1, out var value8, out error4, "Point position") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces4, num5 + 3, out var value9, out error4, "int tileIndex"))
				{
					LogMapPropertyError("DayTiles", mapPropertySplitBySpaces4, error4);
				}
				else if (value9 != 720 || !Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
				{
					Tile tile = map.RequireLayer(value7).Tiles[value8.X, value8.Y];
					if (tile != null)
					{
						tile.TileIndex = value9;
					}
				}
			}
		}
		else if (Game1.timeOfDay >= num4 || (IsRainingHere() && !name.Equals("SandyHouse")))
		{
			switchOutNightTiles();
		}
		if (Game1.killScreen && Game1.activeClickableMenu != null && !Game1.dialogueUp)
		{
			Game1.activeClickableMenu.emergencyShutDown();
			Game1.exitActiveMenu();
		}
		if (Game1.activeClickableMenu == null && !Game1.warpingForForcedRemoteEvent && !flag)
		{
			checkForEvents();
		}
		foreach (KeyValuePair<string, LightSource> pair3 in sharedLights.Pairs)
		{
			Game1.currentLightSources[pair3.Key] = pair3.Value;
		}
		foreach (NPC character in characters)
		{
			character.behaviorOnLocalFarmerLocationEntry(this);
		}
		foreach (Furniture item in furniture)
		{
			item.actionOnPlayerEntry();
		}
		updateFishSplashAnimation();
		updateOrePanAnimation();
		showDropboxIndicator = false;
		foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
		{
			if (specialOrder.ShouldDisplayAsComplete())
			{
				continue;
			}
			foreach (OrderObjective objective in specialOrder.objectives)
			{
				if (objective is DonateObjective donateObjective && !string.IsNullOrEmpty(donateObjective.dropBoxGameLocation.Value) && donateObjective.GetDropboxLocationName() == Name)
				{
					showDropboxIndicator = true;
					dropBoxIndicatorLocation = donateObjective.dropBoxTileLocation.Value * 64f + new Vector2(7f, 0f) * 4f;
				}
			}
		}
		if (Game1.timeOfDay >= 1830)
		{
			FarmAnimal[] array2 = animals.Values.ToArray();
			for (int num6 = 0; num6 < array2.Length; num6++)
			{
				array2[num6].warpHome();
			}
		}
		foreach (Building building in buildings)
		{
			building.resetLocalState();
		}
		if (isThereABuildingUnderConstruction())
		{
			foreach (string key in Game1.netWorldState.Value.Builders.Keys)
			{
				BuilderData builderData = Game1.netWorldState.Value.Builders[key];
				if (builderData.buildingLocation.Value == NameOrUniqueName && builderData.daysUntilBuilt.Value > 0)
				{
					NPC characterFromName = Game1.getCharacterFromName(key);
					if (characterFromName != null && characterFromName.currentLocation.Equals(this))
					{
						Building buildingAt = getBuildingAt(Utility.PointToVector2(builderData.buildingTile.Value));
						if (buildingAt != null)
						{
							temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(399, 262, (buildingAt.daysOfConstructionLeft.Value == 1) ? 29 : 9, 43), new Vector2(buildingAt.tileX.Value + buildingAt.tilesWide.Value / 2, buildingAt.tileY.Value + buildingAt.tilesHigh.Value / 2) * 64f + new Vector2(-16f, -144f), flipped: false, 0f, Color.White)
							{
								id = 16846,
								scale = 4f,
								interval = 999999f,
								animationLength = 1,
								totalNumberOfLoops = 99999,
								layerDepth = (float)((buildingAt.tileY.Value + buildingAt.tilesHigh.Value / 2) * 64 + 32) / 10000f
							});
						}
					}
				}
			}
			return;
		}
		removeTemporarySpritesWithIDLocal(16846);
	}

	protected virtual void _updateAmbientLighting()
	{
		if (Game1.eventUp || (Game1.player.viewingLocation.Value != null && !Game1.player.viewingLocation.Value.Equals(Name)))
		{
			return;
		}
		if (!isOutdoors.Value || ignoreOutdoorLighting.Value)
		{
			if (Game1.isStartingToGetDarkOut(this) || lightLevel.Value > 0f)
			{
				int startTime = Game1.timeOfDay + Game1.gameTimeInterval / (Game1.realMilliSecondsPerGameMinute + ExtraMillisecondsPerInGameMinute);
				float t = 1f - Utility.Clamp((float)Utility.CalculateMinutesBetweenTimes(startTime, Game1.getTrulyDarkTime(this)) / 120f, 0f, 1f);
				Game1.ambientLight = new Color((byte)Utility.Lerp((int)indoorLightingColor.R, (int)indoorLightingNightColor.R, t), (byte)Utility.Lerp((int)indoorLightingColor.G, (int)indoorLightingNightColor.G, t), (byte)Utility.Lerp((int)indoorLightingColor.B, (int)indoorLightingNightColor.B, t));
			}
			else
			{
				Game1.ambientLight = indoorLightingColor;
			}
		}
		else
		{
			Game1.ambientLight = (IsRainingHere() ? new Color(255, 200, 80) : Color.White);
		}
	}

	private bool TryGetAmbientLightFromMap(out Color color, string propertyName = "AmbientLight")
	{
		string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces(propertyName);
		if (mapPropertySplitBySpaces.Length != 0)
		{
			if (ArgUtility.TryGetInt(mapPropertySplitBySpaces, 0, out var value, out var error, "int r") && ArgUtility.TryGetInt(mapPropertySplitBySpaces, 1, out var value2, out error, "int g") && ArgUtility.TryGetInt(mapPropertySplitBySpaces, 2, out var value3, out error, "int b"))
			{
				color = new Color(value, value2, value3);
				return true;
			}
			LogMapPropertyError(propertyName, mapPropertySplitBySpaces, error);
		}
		color = Color.White;
		return false;
	}

	public void SelectRandomMiniJukeboxTrack()
	{
		if (!(miniJukeboxTrack.Value != "random"))
		{
			Farmer player = Game1.player;
			if (this is FarmHouse { HasOwner: not false } farmHouse)
			{
				player = farmHouse.owner;
			}
			List<string> jukeboxTracks = Utility.GetJukeboxTracks(player, this);
			string value = Game1.random.ChooseFrom(jukeboxTracks);
			randomMiniJukeboxTrack.Value = value;
		}
	}

	protected virtual void resetSharedState()
	{
		SelectRandomMiniJukeboxTrack();
		for (int num = characters.Count - 1; num >= 0; num--)
		{
			characters[num].behaviorOnFarmerLocationEntry(this, Game1.player);
		}
		if (!(this is MineShaft))
		{
			switch (GetSeason())
			{
			case Season.Spring:
				waterColor.Value = new Color(120, 200, 255) * 0.5f;
				break;
			case Season.Summer:
				waterColor.Value = new Color(60, 240, 255) * 0.5f;
				break;
			case Season.Fall:
				waterColor.Value = new Color(255, 130, 200) * 0.5f;
				break;
			case Season.Winter:
				waterColor.Value = new Color(130, 80, 255) * 0.5f;
				break;
			}
		}
	}

	public LightSource getLightSource([NotNullWhen(true)] string identifier)
	{
		if (identifier == null || !sharedLights.TryGetValue(identifier, out var value))
		{
			return null;
		}
		return value;
	}

	public bool hasLightSource([NotNullWhen(true)] string identifier)
	{
		if (identifier != null)
		{
			return sharedLights.ContainsKey(identifier);
		}
		return false;
	}

	public void removeLightSource([NotNullWhen(true)] string identifier)
	{
		if (identifier != null)
		{
			sharedLights.Remove(identifier);
		}
	}

	public void repositionLightSource([NotNullWhen(true)] string identifier, Vector2 position)
	{
		if (identifier != null && sharedLights.TryGetValue(identifier, out var value))
		{
			value.position.Value = position;
		}
	}

	public virtual bool CanSpawnCharacterHere(Vector2 tileLocation)
	{
		if (isTileOnMap(tileLocation) && isTilePlaceable(tileLocation))
		{
			return !IsTileBlockedBy(tileLocation);
		}
		return false;
	}

	/// <summary>Get whether items in general can be placed on a tile.</summary>
	/// <param name="tile">The tile position within the location.</param>
	/// <param name="itemIsPassable">Whether the item being placed can be walked over by players and characters.</param>
	/// <param name="collisionMask">The collision types to look for. This should usually be kept default.</param>
	/// <param name="ignorePassables">The collision types to ignore when they don't block movement (e.g. tilled dirt).</param>
	/// <param name="useFarmerTile">When checking collisions with farmers, whether to check their tile position instead of their bounding box.</param>
	/// <param name="ignorePassablesExactly">Whether to use the exact <paramref name="ignorePassables" /> value provided, without adjusting it for <paramref name="itemIsPassable" />. This should only be true in specialized cases.</param>
	public virtual bool CanItemBePlacedHere(Vector2 tile, bool itemIsPassable = false, CollisionMask collisionMask = CollisionMask.All, CollisionMask ignorePassables = ~CollisionMask.Objects, bool useFarmerTile = false, bool ignorePassablesExactly = false)
	{
		if (!ignorePassablesExactly)
		{
			ignorePassables &= ~CollisionMask.Objects;
			if (!itemIsPassable)
			{
				ignorePassables &= ~(CollisionMask.Characters | CollisionMask.Farmers);
			}
		}
		if (!isTileOnMap(tile))
		{
			return false;
		}
		if (!isTilePlaceable(tile, itemIsPassable))
		{
			return false;
		}
		if (GetHoeDirtAtTile(tile)?.crop != null)
		{
			return false;
		}
		if (IsTileBlockedBy(tile, collisionMask, ignorePassables, useFarmerTile))
		{
			return false;
		}
		if (itemIsPassable && getBuildingAt(tile) != null && getBuildingAt(tile).GetData() != null && !getBuildingAt(tile).GetData().AllowsFlooringUnderneath)
		{
			return false;
		}
		return true;
	}

	/// <summary>Get whether a tile is either occupied by an object or is a non-passable tile.</summary>
	/// <param name="tile">The tile position within the location.</param>
	/// <param name="collisionMask">The collision types to look for. This should usually be kept default.</param>
	/// <param name="ignorePassables">The collision types to ignore when they don't block movement (e.g. tilled dirt).</param>
	/// <param name="useFarmerTile">When checking collisions with farmers, whether to check their tile position instead of their bounding box.</param>
	public virtual bool IsTileBlockedBy(Vector2 tile, CollisionMask collisionMask = CollisionMask.All, CollisionMask ignorePassables = CollisionMask.None, bool useFarmerTile = false)
	{
		if (!IsTileOccupiedBy(tile, collisionMask, ignorePassables, useFarmerTile))
		{
			return !isTilePassable(tile);
		}
		return true;
	}

	/// <summary>Get whether a tile is occupied.</summary>
	/// <param name="tile">The tile position within the location.</param>
	/// <param name="collisionMask">The collision types to look for. This should usually be kept default.</param>
	/// <param name="ignorePassables">The collision types to ignore when they don't block movement (e.g. tilled dirt).</param>
	/// <param name="useFarmerTile">When checking collisions with farmers, whether to check their tile position instead of their bounding box.</param>
	public virtual bool IsTileOccupiedBy(Vector2 tile, CollisionMask collisionMask = CollisionMask.All, CollisionMask ignorePassables = CollisionMask.None, bool useFarmerTile = false)
	{
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle((int)tile.X * 64, (int)tile.Y * 64, 64, 64);
		if (collisionMask.HasFlag(CollisionMask.Farmers) && !ignorePassables.HasFlag(CollisionMask.Farmers))
		{
			foreach (Farmer farmer in farmers)
			{
				if (useFarmerTile ? (farmer.Tile == tile) : farmer.GetBoundingBox().Intersects(value))
				{
					return true;
				}
			}
		}
		if (collisionMask.HasFlag(CollisionMask.Objects) && objects.TryGetValue(tile, out var value2) && (!ignorePassables.HasFlag(CollisionMask.Objects) || !value2.isPassable()))
		{
			return true;
		}
		if (collisionMask.HasFlag(CollisionMask.Furniture))
		{
			Furniture furnitureAt = GetFurnitureAt(tile);
			if (furnitureAt != null && (!ignorePassables.HasFlag(CollisionMask.Furniture) || !furnitureAt.isPassable()))
			{
				return true;
			}
		}
		if (collisionMask.HasFlag(CollisionMask.Characters))
		{
			foreach (NPC character in characters)
			{
				if (character != null && character.GetBoundingBox().Intersects(value) && !character.IsInvisible && (!ignorePassables.HasFlag(CollisionMask.Characters) || !character.farmerPassesThrough))
				{
					return true;
				}
			}
			if (animals.Length > 0)
			{
				foreach (FarmAnimal value4 in animals.Values)
				{
					if (value4.Tile == tile && (!ignorePassables.HasFlag(CollisionMask.Characters) || !value4.farmerPassesThrough))
					{
						return true;
					}
				}
			}
		}
		if (collisionMask.HasFlag(CollisionMask.TerrainFeatures))
		{
			foreach (ResourceClump resourceClump in resourceClumps)
			{
				if (resourceClump.occupiesTile((int)tile.X, (int)tile.Y) && (!ignorePassables.HasFlag(CollisionMask.TerrainFeatures) || !resourceClump.isPassable()))
				{
					return true;
				}
			}
			if (largeTerrainFeatures != null)
			{
				foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
				{
					if (largeTerrainFeature.getBoundingBox().Intersects(value) && (!ignorePassables.HasFlag(CollisionMask.TerrainFeatures) || !largeTerrainFeature.isPassable()))
					{
						return true;
					}
				}
			}
		}
		if ((collisionMask.HasFlag(CollisionMask.TerrainFeatures) || collisionMask.HasFlag(CollisionMask.Flooring)) && terrainFeatures.TryGetValue(tile, out var value3) && value3.getBoundingBox().Intersects(value))
		{
			CollisionMask collisionMask2 = ((value3 is Flooring) ? CollisionMask.Flooring : CollisionMask.TerrainFeatures);
			if (collisionMask.HasFlag(collisionMask2) && (!ignorePassables.HasFlag(collisionMask2) || !value3.isPassable()))
			{
				return true;
			}
		}
		if (collisionMask.HasFlag(CollisionMask.LocationSpecific) && IsLocationSpecificOccupantOnTile(tile))
		{
			return true;
		}
		if (collisionMask.HasFlag(CollisionMask.Buildings))
		{
			foreach (Building building in buildings)
			{
				if (!building.isMoving && (ignorePassables.HasFlag(CollisionMask.Buildings) ? (!building.isTilePassable(tile)) : building.occupiesTile(tile)))
				{
					return true;
				}
			}
		}
		return false;
	}

	public virtual bool IsLocationSpecificOccupantOnTile(Vector2 tileLocation)
	{
		return false;
	}

	public virtual bool IsLocationSpecificPlacementRestriction(Vector2 tileLocation)
	{
		return false;
	}

	public Farmer isTileOccupiedByFarmer(Vector2 tileLocation)
	{
		foreach (Farmer farmer in farmers)
		{
			if (farmer.Tile == tileLocation)
			{
				return farmer;
			}
		}
		return null;
	}

	/// <summary>Get any tilled dirt at a tile position, whether it's on the ground or in a garden pot.</summary>
	/// <param name="tile">The tile position to check.</param>
	/// <returns>Returns the tilled dirt found, else <c>null</c>.</returns>
	public HoeDirt GetHoeDirtAtTile(Vector2 tile)
	{
		if (objects.TryGetValue(tile, out var value) && value is IndoorPot indoorPot)
		{
			return indoorPot.hoeDirt.Value;
		}
		if (terrainFeatures.TryGetValue(tile, out var value2) && value2 is HoeDirt result)
		{
			return result;
		}
		return null;
	}

	/// <summary>Get whether a tile contains a hoe dirt, or an object that should behave like a hoe dirt, such as a Garden Pot.</summary>
	public bool isTileHoeDirt(Vector2 tile)
	{
		return GetHoeDirtAtTile(tile) != null;
	}

	/// <summary>Get whether a tile is not on the water, and is unobstructed by a tile on the Buildings layer or higher. This can be used to ensure items don't spawn behind high walls, etc.</summary>
	public bool isTileLocationOpen(Location location)
	{
		return isTileLocationOpen(new Vector2(location.X, location.Y));
	}

	/// <summary>Get whether a tile is not on the water, and is unobstructed by a tile on the Buildings layer or higher. This can be used to ensure items don't spawn behind high walls, etc.</summary>
	public bool isTileLocationOpen(Vector2 location)
	{
		if (map.RequireLayer("Buildings").Tiles[(int)location.X, (int)location.Y] == null && !isWaterTile((int)location.X, (int)location.Y) && map.RequireLayer("Front").Tiles[(int)location.X, (int)location.Y] == null)
		{
			return map.GetLayer("AlwaysFront")?.Tiles[(int)location.X, (int)location.Y] == null;
		}
		return false;
	}

	public virtual bool CanPlaceThisFurnitureHere(Furniture furniture)
	{
		if (furniture == null)
		{
			return false;
		}
		bool flag = this is DecoratableLocation || !IsOutdoors;
		if (furniture.furniture_type.Value == 15)
		{
			if (!TryGetMapPropertyAs("AllowBeds", out bool parsed, required: false))
			{
				parsed = this is FarmHouse || this is IslandFarmHouse || (flag && ParentBuilding != null);
			}
			if (!parsed)
			{
				return false;
			}
		}
		switch (furniture.placementRestriction)
		{
		case 0:
			return flag;
		case 1:
			return !flag;
		case 2:
			if (!flag)
			{
				return !flag;
			}
			return true;
		default:
			return false;
		}
	}

	/// <summary>Get whether a tile is allowed to have an object placed on it. Note that this function does not factor in the tile's current occupancy.</summary>
	public virtual bool isTilePlaceable(Vector2 v, bool itemIsPassable = false)
	{
		if (IsLocationSpecificPlacementRestriction(v))
		{
			return false;
		}
		if (!hasTileAt((int)v.X, (int)v.Y, "Back"))
		{
			return false;
		}
		if (isWaterTile((int)v.X, (int)v.Y))
		{
			return false;
		}
		string text = doesTileHaveProperty((int)v.X, (int)v.Y, "NoFurniture", "Back");
		if (text != null)
		{
			if (text == "total")
			{
				return false;
			}
			if (!itemIsPassable || !Game1.currentLocation.IsOutdoors)
			{
				return false;
			}
		}
		return true;
	}

	public void playTerrainSound(Vector2 tileLocation, Character who = null, bool showTerrainDisturbAnimation = true)
	{
		string text = "thudStep";
		if (IsOutdoors || treatAsOutdoors.Value || Name.ContainsIgnoreCase("mine"))
		{
			switch (doesTileHaveProperty((int)tileLocation.X, (int)tileLocation.Y, "Type", "Back"))
			{
			case "Dirt":
				text = "sandyStep";
				break;
			case "Stone":
				text = "stoneStep";
				break;
			case "Grass":
				text = ((GetSeason() == Season.Winter) ? "snowyStep" : "grassyStep");
				break;
			case "Wood":
				text = "woodyStep";
				break;
			case null:
				if (isWaterTile((int)tileLocation.X, (int)tileLocation.Y))
				{
					text = "waterSlosh";
				}
				break;
			}
		}
		if (terrainFeatures.TryGetValue(tileLocation, out var value) && value is Flooring)
		{
			text = ((Flooring)terrainFeatures[tileLocation]).getFootstepSound();
		}
		if (who != null && showTerrainDisturbAnimation && text == "sandyStep")
		{
			Vector2 vector = Vector2.Zero;
			if (who.shouldShadowBeOffset)
			{
				vector = who.drawOffset;
			}
			temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 64, 64, 64), 50f, 4, 1, new Vector2(who.Position.X + (float)Game1.random.Next(-8, 8), who.Position.Y + (float)Game1.random.Next(-16, 0)) + vector, flicker: false, Game1.random.NextBool(), 0.0001f, 0f, Color.White, 1f, 0.01f, 0f, (float)Game1.random.Next(-5, 6) * (float)Math.PI / 128f));
		}
		else if (who != null && showTerrainDisturbAnimation && GetSeason() == Season.Winter && text == "grassyStep")
		{
			Vector2 vector2 = Vector2.Zero;
			if (who.shouldShadowBeOffset)
			{
				vector2 = who.drawOffset;
			}
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(247, 407, 6, 6), 2000f, 1, 10000, new Vector2(who.Position.X, who.Position.Y) + vector2, flicker: false, flipped: false, 0.0001f, 0.001f, Color.White, 1f, 0.01f, 0f, 0f));
		}
		if ((who as Farmer)?.boots.Value?.ItemId == "853")
		{
			localSound("jingleBell");
		}
		if (text.Length > 0)
		{
			localSound(text);
		}
	}

	public bool checkTileIndexAction(int tileIndex)
	{
		if ((tileIndex == 1799 || (uint)(tileIndex - 1824) <= 9u) && Name.Equals("AbandonedJojaMart"))
		{
			Game1.RequireLocation<AbandonedJojaMart>("AbandonedJojaMart").checkBundle();
			return true;
		}
		return false;
	}

	public bool checkForTerrainFeaturesAndObjectsButDestroyNonPlayerItems(int x, int y)
	{
		Vector2 key = new Vector2(x, y);
		if (objects.TryGetValue(key, out var value))
		{
			if (!value.IsSpawnedObject || value is Chest || value.Type == "Crafting")
			{
				return false;
			}
			objects.Remove(key);
		}
		terrainFeatures.Remove(key);
		return true;
	}

	public virtual bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		who.ignoreItemConsumptionThisFrame = false;
		Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle(tileLocation.X * 64, tileLocation.Y * 64, 64, 64);
		if (!objects.ContainsKey(new Vector2(tileLocation.X, tileLocation.Y)) && CheckPetAnimal(rectangle, who))
		{
			return true;
		}
		foreach (Building building in buildings)
		{
			if (building.doAction(new Vector2(tileLocation.X, tileLocation.Y), who))
			{
				return true;
			}
		}
		if (who.IsSitting())
		{
			who.StopSitting();
			return true;
		}
		foreach (Farmer farmer in farmers)
		{
			if (farmer != Game1.player && farmer.GetBoundingBox().Intersects(rectangle) && farmer.checkAction(who, this))
			{
				return true;
			}
		}
		if (currentEvent != null && currentEvent.isFestival)
		{
			return currentEvent.checkAction(tileLocation, viewport, who);
		}
		foreach (NPC character in characters)
		{
			if (character != null && !character.IsMonster && (!who.isRidingHorse() || !(character is Horse)) && character.GetBoundingBox().Intersects(rectangle) && character.checkAction(who, this))
			{
				if (who.FarmerSprite.IsPlayingBasicAnimation(who.FacingDirection, carrying: false) || who.FarmerSprite.IsPlayingBasicAnimation(who.FacingDirection, carrying: true))
				{
					who.faceGeneralDirection(character.getStandingPosition(), 0, opposite: false, useTileCalculations: false);
				}
				return true;
			}
		}
		int tileIndexAt = getTileIndexAt(tileLocation, "Buildings", "untitled tile sheet");
		if (NameOrUniqueName == "SkullCave" && (tileIndexAt == 344 || tileIndexAt == 349))
		{
			if (Game1.player.team.SpecialOrderActive("QiChallenge10"))
			{
				who.doEmote(40);
				return false;
			}
			if (!Game1.player.team.completedSpecialOrders.Contains("QiChallenge10"))
			{
				who.doEmote(8);
				return false;
			}
			if (!Game1.player.team.toggleSkullShrineOvernight.Value)
			{
				if (!Game1.player.team.skullShrineActivated.Value)
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:ChallengeShrine_NotYetHard"), createYesNoResponses(), "ShrineOfSkullChallenge");
				}
				else
				{
					Game1.player.team.toggleSkullShrineOvernight.Value = true;
					Game1.showGlobalMessage(Game1.content.LoadString("Strings\\Locations:ChallengeShrine_Activated"));
					Game1.multiplayer.globalChatInfoMessage(Game1.player.team.skullShrineActivated.Value ? "HardModeSkullCaveDeactivated" : "HardModeSkullCaveActivated", who.name.Value);
					playSound(Game1.player.team.skullShrineActivated.Value ? "skeletonStep" : "serpentDie");
				}
			}
			else if (Game1.player.team.toggleSkullShrineOvernight.Value && Game1.player.team.skullShrineActivated.Value)
			{
				Game1.player.team.toggleSkullShrineOvernight.Value = false;
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\UI:PendingProposal_Canceling"));
				playSound("skeletonStep");
			}
			return true;
		}
		foreach (ResourceClump resourceClump in resourceClumps)
		{
			if (resourceClump.getBoundingBox().Intersects(rectangle) && resourceClump.performUseAction(new Vector2(tileLocation.X, tileLocation.Y)))
			{
				return true;
			}
		}
		Vector2 vector = new Vector2(tileLocation.X, tileLocation.Y);
		if (objects.TryGetValue(vector, out var value))
		{
			// Allow right-click picking up placed Mega Stones, Z-Crystals, and Pokémon items
			if (value != null && value.ItemId != null &&
			    (value.ItemId.StartsWith("Poke_Mega_") || value.ItemId.StartsWith("Poke_Z_") || value.ItemId.StartsWith("Poke_") || StardewValley.Pokemon.PokemonItemManager.IsPokeBall(value.ItemId)))
			{
				if (who.couldInventoryAcceptThisItem(value))
				{
					if (who.IsLocalPlayer)
					{
						localSound("pickUpItem");
					}
					who.animateOnce(279 + who.FacingDirection);
					who.addItemToInventoryBool(value.getOne());
					objects.Remove(vector);
					return true;
				}
			}

			bool isErrorItem = ItemRegistry.GetDataOrErrorItem(value.QualifiedItemId).IsErrorItem;
			if (value.Type != null || isErrorItem)
			{
				if (who.isRidingHorse() && !(value is Fence))
				{
					return false;
				}
				if (vector == who.Tile && !value.isPassable() && (!(value is Fence fence) || !fence.isGate.Value))
				{
					Tool tool = ItemRegistry.Create<Tool>("(T)Pickaxe");
					tool.DoFunction(Game1.currentLocation, -1, -1, 0, who);
					if (value.performToolAction(tool))
					{
						value.performRemoveAction();
						value.dropItem(this, who.GetToolLocation(), Utility.PointToVector2(who.StandingPixel));
						Game1.currentLocation.Objects.Remove(vector);
						return true;
					}
					tool = ItemRegistry.Create<Tool>("(T)Axe");
					tool.DoFunction(Game1.currentLocation, -1, -1, 0, who);
					if (objects.TryGetValue(vector, out value) && value.performToolAction(tool))
					{
						value.performRemoveAction();
						value.dropItem(this, who.GetToolLocation(), Utility.PointToVector2(who.StandingPixel));
						Game1.currentLocation.Objects.Remove(vector);
						return true;
					}
					if (!objects.TryGetValue(vector, out value))
					{
						return true;
					}
				}
				if (objects.TryGetValue(vector, out value) && (value.Type == "Crafting" || value.Type == "interactive"))
				{
					if (who.ActiveObject == null && value.checkForAction(who))
					{
						return true;
					}
					if (objects.TryGetValue(vector, out value))
					{
						if (who.CurrentItem != null)
						{
							Object value2 = value.heldObject.Value;
							value.heldObject.Value = null;
							bool flag = value.performObjectDropInAction(who.CurrentItem, probe: true, who);
							value.heldObject.Value = value2;
							bool flag2 = value.performObjectDropInAction(who.CurrentItem, probe: false, who, returnFalseIfItemConsumed: true);
							if ((flag || flag2) && who.isMoving())
							{
								Game1.haltAfterCheck = false;
							}
							if (who.ignoreItemConsumptionThisFrame)
							{
								return true;
							}
							if (flag2)
							{
								who.reduceActiveItemByOne();
								return true;
							}
							return value.checkForAction(who) || flag;
						}
						return value.checkForAction(who);
					}
				}
				else if (objects.TryGetValue(vector, out value) && (value.isSpawnedObject.Value || isErrorItem))
				{
					int value3 = value.quality.Value;
					Random random = Utility.CreateDaySaveRandom(vector.X, vector.Y * 777f);
					if (value.isForage())
					{
						value.Quality = GetHarvestSpawnedObjectQuality(who, value.isForage(), value.TileLocation, random);
					}
					if (value.questItem.Value && value.questId.Value != null && value.questId.Value != "0" && !who.hasQuest(value.questId.Value))
					{
						return false;
					}
					if (who.couldInventoryAcceptThisItem(value))
					{
						if (who.IsLocalPlayer)
						{
							localSound("pickUpItem");
							DelayedAction.playSoundAfterDelay("coin", 300);
						}
						who.animateOnce(279 + who.FacingDirection);
						if (!isFarmBuildingInterior())
						{
							if (value.isForage())
							{
								OnHarvestedForage(who, value);
							}
							if (value.ItemId.Equals("789") && Name.Equals("LewisBasement"))
							{
								Bat bat = new Bat(Vector2.Zero, -789);
								bat.focusedOnFarmers = true;
								Game1.changeMusicTrack("none");
								playSound("cursed_mannequin");
								characters.Add(bat);
							}
						}
						else
						{
							who.gainExperience(0, 5);
						}
						who.addItemToInventoryBool(value.getOne());
						Game1.stats.ItemsForaged++;
						if (who.professions.Contains(13) && random.NextDouble() < 0.2 && !value.questItem.Value && who.couldInventoryAcceptThisItem(value) && !isFarmBuildingInterior())
						{
							who.addItemToInventoryBool(value.getOne());
							who.gainExperience(2, 7);
						}
						objects.Remove(vector);
						return true;
					}
					value.Quality = value3;
				}
			}
		}
		if (who.isRidingHorse())
		{
			who.mount.checkAction(who, this);
			return true;
		}
		foreach (KeyValuePair<Vector2, TerrainFeature> pair in terrainFeatures.Pairs)
		{
			if (pair.Value.getBoundingBox().Intersects(rectangle) && pair.Value.performUseAction(pair.Key))
			{
				Game1.haltAfterCheck = false;
				return true;
			}
		}
		if (largeTerrainFeatures != null)
		{
			foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
			{
				if (largeTerrainFeature.getBoundingBox().Intersects(rectangle) && largeTerrainFeature.performUseAction(largeTerrainFeature.Tile))
				{
					Game1.haltAfterCheck = false;
					return true;
				}
			}
		}
		Tile tile = map.RequireLayer("Buildings").PickTile(new Location(tileLocation.X * 64, tileLocation.Y * 64), viewport.Size);
		string action = null;
		if (tile != null && tile.Properties.TryGetValue("Action", out var pv) && pv != null)
		{
			try { action = pv.ToString(); } catch { }
		}
		if (string.IsNullOrEmpty(action))
		{
			action = doesTileHaveProperty(tileLocation.X, tileLocation.Y, "Action", "Buildings");
		}
		if (!string.IsNullOrEmpty(action))
		{
			NPC nPC = isCharacterAtTile(vector + new Vector2(0f, 1f));
			if (currentEvent == null && nPC != null && !nPC.IsInvisible && !nPC.IsMonster && (!who.isRidingHorse() || !(nPC is Horse)))
			{
				Point standingPixel = nPC.StandingPixel;
				if (Utility.withinRadiusOfPlayer(standingPixel.X, standingPixel.Y, 1, who) && nPC.checkAction(who, this))
				{
					if (who.FarmerSprite.IsPlayingBasicAnimation(who.FacingDirection, who.IsCarrying()))
					{
						who.faceGeneralDirection(Utility.PointToVector2(standingPixel), 0, opposite: false, useTileCalculations: false);
					}
					return true;
				}
			}
			return performAction(action, who, tileLocation);
		}
		if (tile != null && checkTileIndexAction(tile.TileIndex))
		{
			return true;
		}
		foreach (MapSeat mapSeat in mapSeats)
		{
			if (mapSeat.OccupiesTile(tileLocation.X, tileLocation.Y) && !mapSeat.IsBlocked(this))
			{
				who.BeginSitting(mapSeat);
				return true;
			}
		}
		Point value5 = new Point(tileLocation.X * 64, (tileLocation.Y - 1) * 64);
		bool flag3 = Game1.didPlayerJustRightClick();
		Furniture furniture = null;
		foreach (Furniture item in this.furniture)
		{
			if (item.boundingBox.Value.Contains((int)(vector.X * 64f), (int)(vector.Y * 64f)) && item.furniture_type.Value != 12)
			{
				if (flag3)
				{
					if (who.ActiveObject != null && item.performObjectDropInAction(who.ActiveObject, probe: false, who))
					{
						return true;
					}
					return item.checkForAction(who);
				}
				return item.clicked(who);
			}
			if (item.furniture_type.Value == 6 && item.boundingBox.Value.Contains(value5))
			{
				furniture = item;
			}
		}
		if (furniture != null)
		{
			if (flag3)
			{
				if (who.ActiveObject != null && furniture.performObjectDropInAction(who.ActiveObject, probe: false, who))
				{
					return true;
				}
				return furniture.checkForAction(who);
			}
			return furniture.clicked(who);
		}
		if (Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true) && animals.Length > 0 && CheckInspectAnimal(rectangle, who))
		{
			return true;
		}
		return false;
	}

	/// <summary>Get the quality for a newly harvested spawned object.</summary>
	/// <param name="who">The player harvesting the object.</param>
	/// <param name="isForage">Whether the object is a forage item.</param>
	/// <param name="tile">The tile position.</param>
	/// <param name="random">The RNG to use if needed, or <c>null</c> to create a new one.</param>
	public int GetHarvestSpawnedObjectQuality(Farmer who, bool isForage, Vector2 tile, Random random = null)
	{
		if (who.professions.Contains(16) && isForage)
		{
			return 4;
		}
		if (isForage)
		{
			if (random == null)
			{
				random = Utility.CreateDaySaveRandom(tile.X, tile.Y * 777f);
			}
			if (random.NextBool((float)who.ForagingLevel / 30f))
			{
				return 2;
			}
			if (random.NextBool((float)who.ForagingLevel / 15f))
			{
				return 1;
			}
		}
		return 0;
	}

	/// <summary>Handle a player harvesting a spawned forage object.</summary>
	/// <param name="who">The player who harvested the forage.</param>
	/// <param name="forage">The forage object that was harvested.</param>
	public void OnHarvestedForage(Farmer who, Object forage)
	{
		if (forage.SpecialVariable == 724519)
		{
			who.gainExperience(2, 2);
			who.gainExperience(0, 3);
		}
		else
		{
			who.gainExperience(2, 7);
		}
	}

	public virtual bool CanFreePlaceFurniture()
	{
		return false;
	}

	public virtual bool LowPriorityLeftClick(int x, int y, Farmer who)
	{
		if (Game1.activeClickableMenu != null)
		{
			return false;
		}
		for (int num = this.furniture.Count - 1; num >= 0; num--)
		{
			Furniture furniture = this.furniture[num];
			if (CanFreePlaceFurniture() || furniture.IsCloseEnoughToFarmer(who))
			{
				if (!furniture.isPassable() && furniture.boundingBox.Value.Contains(x, y) && furniture.canBeRemoved(who))
				{
					furniture.AttemptRemoval(delegate(Furniture f)
					{
						Guid job = this.furniture.GuidOf(f);
						if (!furnitureToRemove.Contains(job))
						{
							furnitureToRemove.Add(job);
						}
					});
					return true;
				}
				if (furniture.boundingBox.Value.Contains(x, y) && furniture.heldObject.Value != null)
				{
					furniture.clicked(who);
					return true;
				}
				if (!furniture.isGroundFurniture() && furniture.canBeRemoved(who))
				{
					int y2 = y;
					if (this is DecoratableLocation decoratableLocation)
					{
						y2 = decoratableLocation.GetWallTopY(x / 64, y / 64);
						y2 = ((y2 != -1) ? (y2 * 64) : (y * 64));
					}
					if (furniture.boundingBox.Value.Contains(x, y2))
					{
						furniture.AttemptRemoval(delegate(Furniture f)
						{
							Guid job = this.furniture.GuidOf(f);
							if (!furnitureToRemove.Contains(job))
							{
								furnitureToRemove.Add(job);
							}
						});
						return true;
					}
				}
			}
		}
		for (int num2 = this.furniture.Count - 1; num2 >= 0; num2--)
		{
			Furniture furniture2 = this.furniture[num2];
			if ((CanFreePlaceFurniture() || furniture2.IsCloseEnoughToFarmer(who)) && furniture2.isPassable() && furniture2.boundingBox.Value.Contains(x, y) && furniture2.canBeRemoved(who))
			{
				furniture2.AttemptRemoval(delegate(Furniture f)
				{
					Guid job = this.furniture.GuidOf(f);
					if (!furnitureToRemove.Contains(job))
					{
						furnitureToRemove.Add(job);
					}
				});
				return true;
			}
		}
		Microsoft.Xna.Framework.Rectangle rect = new Microsoft.Xna.Framework.Rectangle(x * 64, y * 64, 64, 64);
		if (Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true) && CheckInspectAnimal(rect, who))
		{
			return true;
		}
		return false;
	}

	[Obsolete("These values returned by this function are no longer used by the game (except for rare, backwards compatibility related cases.) Check DecoratableLocation's wallpaper/flooring related functionality instead.")]
	public virtual List<Microsoft.Xna.Framework.Rectangle> getWalls()
	{
		return new List<Microsoft.Xna.Framework.Rectangle>();
	}

	protected virtual void removeQueuedFurniture(Guid guid)
	{
		Farmer player = Game1.player;
		if (!furniture.TryGetValue(guid, out var value) || !player.couldInventoryAcceptThisItem(value))
		{
			return;
		}
		value.performRemoveAction();
		furniture.Remove(guid);
		bool flag = false;
		for (int i = 0; i < 12; i++)
		{
			if (player.Items[i] == null)
			{
				player.Items[i] = value;
				player.CurrentToolIndex = i;
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Item item = player.addItemToInventory(value, 11);
			player.addItemToInventory(item);
			player.CurrentToolIndex = 11;
		}
		localSound("coin");
	}

	public virtual bool leftClick(int x, int y, Farmer who)
	{
		Vector2 key = new Vector2(x / 64, y / 64);
		foreach (Building building in buildings)
		{
			if (building.CanLeftClick(x, y) && building.leftClicked())
			{
				return true;
			}
		}
		if (objects.TryGetValue(key, out var value) && value.clicked(who))
		{
			objects.Remove(key);
			return true;
		}
		return false;
	}

	public virtual bool shouldShadowBeDrawnAboveBuildingsLayer(Vector2 p)
	{
		if (doesTileHaveProperty((int)p.X, (int)p.Y, "Passable", "Buildings") != null)
		{
			return true;
		}
		if (terrainFeatures.TryGetValue(p, out var value) && value is HoeDirt)
		{
			return true;
		}
		if (isWaterTile((int)p.X, (int)p.Y))
		{
			int tileIndexAt = getTileIndexAt((int)p.X, (int)p.Y, "Buildings", "Town");
			if (tileIndexAt < 1004 || tileIndexAt > 1013)
			{
				return true;
			}
		}
		foreach (Building building in buildings)
		{
			if (building.occupiesTile(p) && building.isTilePassable(p))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Get the fridge that's part of this map, if it has one.</summary>
	/// <param name="onlyUnlocked">Whether to only return the fridge if it's available to the player (e.g. they've unlocked the required house upgrade).</param>
	public virtual Chest GetFridge(bool onlyUnlocked = true)
	{
		if (!(this is FarmHouse farmHouse))
		{
			if (this is IslandFarmHouse islandFarmHouse && (!onlyUnlocked || islandFarmHouse.fridgePosition != Point.Zero))
			{
				return islandFarmHouse.fridge.Value;
			}
		}
		else if (!onlyUnlocked || farmHouse.fridgePosition != Point.Zero)
		{
			return farmHouse.fridge.Value;
		}
		return null;
	}

	/// <summary>Get the tile position of the fridge that's part of this map, if it has one and it's available to the player (e.g. they've unlocked the required house upgrade).</summary>
	public virtual Point? GetFridgePosition()
	{
		if (!(this is FarmHouse farmHouse))
		{
			if (this is IslandFarmHouse islandFarmHouse && islandFarmHouse.fridgePosition != Point.Zero)
			{
				return islandFarmHouse.fridgePosition;
			}
		}
		else if (farmHouse.fridgePosition != Point.Zero)
		{
			return farmHouse.fridgePosition;
		}
		return null;
	}

	/// <summary>Open the cooking menu, with ingredients available from any <see cref="M:StardewValley.GameLocation.GetFridge(System.Boolean)" /> or mini-fridges in the location.</summary>
	public void ActivateKitchen()
	{
		List<NetMutex> list = new List<NetMutex>();
		List<Chest> mini_fridges = new List<Chest>();
		foreach (Object value in objects.Values)
		{
			if (value != null && value.bigCraftable.Value && value is Chest chest && chest.fridge.Value)
			{
				mini_fridges.Add(chest);
				list.Add(chest.mutex);
			}
		}
		Chest fridge = GetFridge();
		if (fridge != null)
		{
			list.Add(fridge.mutex);
		}
		new MultipleMutexRequest(list, delegate(MultipleMutexRequest request)
		{
			List<IInventory> list2 = new List<IInventory>();
			if (fridge != null)
			{
				list2.Add(fridge.Items);
			}
			foreach (Chest item in mini_fridges)
			{
				list2.Add(item.Items);
			}
			Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2);
			Game1.activeClickableMenu = new CraftingPage((int)topLeftPositionForCenteringOnScreen.X, (int)topLeftPositionForCenteringOnScreen.Y, 800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, cooking: true, standaloneMenu: true, list2);
			Game1.activeClickableMenu.exitFunction = request.ReleaseLocks;
		}, delegate
		{
			Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Kitchen_InUse"));
		});
	}

	public void openDoor(Location tileLocation, bool playSound)
	{
		try
		{
			int tileIndexAt = getTileIndexAt(tileLocation.X, tileLocation.Y, "Buildings", "1");
			Point key = new Point(tileLocation.X, tileLocation.Y);
			if (!interiorDoors.ContainsKey(key))
			{
				return;
			}
			interiorDoors[key] = true;
			if (playSound)
			{
				Vector2 value = new Vector2(tileLocation.X, tileLocation.Y);
				if (tileIndexAt == 120)
				{
					this.playSound("doorOpen", value);
				}
				else
				{
					this.playSound("doorCreak", value);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public void doStarpoint(string which)
	{
		if (!(which == "3"))
		{
			if (which == "4" && Game1.player.ActiveObject != null && Game1.player.ActiveObject.QualifiedItemId == "(O)203")
			{
				Object obj = ItemRegistry.Create<Object>("(BC)162");
				if (!Game1.player.couldInventoryAcceptThisItem(obj) && Game1.player.ActiveObject.stack.Value > 1)
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
					return;
				}
				Game1.player.reduceActiveItemByOne();
				Game1.player.makeThisTheActiveObject(obj);
				localSound("croak");
				Game1.flashAlpha = 1f;
			}
		}
		else if (Game1.player.ActiveObject != null && Game1.player.ActiveObject.QualifiedItemId == "(O)307")
		{
			Object obj2 = ItemRegistry.Create<Object>("(BC)161");
			if (!Game1.player.couldInventoryAcceptThisItem(obj2) && Game1.player.ActiveObject.stack.Value > 1)
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
				return;
			}
			Game1.player.reduceActiveItemByOne();
			Game1.player.makeThisTheActiveObject(obj2);
			localSound("discoverMineral");
			Game1.flashAlpha = 1f;
		}
	}

	public virtual string FormatCompletionLine(Func<Farmer, float> check)
	{
		KeyValuePair<Farmer, float> farmCompletion = Utility.GetFarmCompletion(check);
		if (farmCompletion.Key == Game1.player)
		{
			return farmCompletion.Value.ToString();
		}
		return "(" + farmCompletion.Key.Name + ") " + farmCompletion.Value;
	}

	public virtual string FormatCompletionLine(Func<Farmer, bool> check, string true_value, string false_value)
	{
		KeyValuePair<Farmer, bool> farmCompletion = Utility.GetFarmCompletion(check);
		if (farmCompletion.Key == Game1.player)
		{
			if (!farmCompletion.Value)
			{
				return false_value;
			}
			return true_value;
		}
		return "(" + farmCompletion.Key.Name + ") " + (farmCompletion.Value ? true_value : false_value);
	}

	public virtual void ShowQiCat()
	{
		if (Game1.MasterPlayer.mailReceived.Contains("Farm_Eternal") && !Game1.MasterPlayer.mailReceived.Contains("GotPerfectionStatue"))
		{
			Game1.player.team.RequestSetMail(PlayerActionTarget.Host, "GotPerfectionStatue", MailType.Received, add: true);
			Game1.player.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create("(BC)280"));
			return;
		}
		if (!Game1.player.hasOrWillReceiveMail("FizzIntro"))
		{
			Game1.addMailForTomorrow("FizzIntro", noLetter: false, sendToEveryone: true);
		}
		Game1.playSound("qi_shop");
		int perfectionWaivers = Game1.netWorldState.Value.PerfectionWaivers;
		double num = Math.Floor(Utility.percentGameComplete() * 100f);
		if (Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ja || Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ko || Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.zh)
		{
			num += (double)perfectionWaivers;
		}
		string text = perfectionWaivers switch
		{
			0 => Game1.content.LoadString("Strings\\UI:PT_Total_Value", num), 
			1 => Game1.content.LoadString("Strings\\UI:PT_Total_ValueWithWaiver", num), 
			_ => Game1.content.LoadString("Strings\\UI:PT_Total_ValueWithWaivers", num, perfectionWaivers), 
		};
		List<string> stringBrokenIntoSectionsOfHeight = SpriteText.getStringBrokenIntoSectionsOfHeight(string.Concat(Utility.loadStringShort("UI", "PT_Title") + "^", "----------------^", Utility.loadStringShort("UI", "PT_Shipped") + ": " + FormatCompletionLine((Farmer farmer) => (float)Math.Floor(Utility.getFarmerItemsShippedPercent(farmer) * 100f)) + "%^", Utility.loadStringShort("UI", "PT_Obelisks") + ": " + Math.Min(Utility.GetObeliskTypesBuilt(), 4) + "/4^", Utility.loadStringShort("UI", "PT_GoldClock") + ": " + (Game1.IsBuildingConstructed("Gold Clock") ? Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes") : Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")) + "^", Utility.loadStringShort("UI", "PT_MonsterSlayer") + ": " + FormatCompletionLine((Farmer farmer) => farmer.hasCompletedAllMonsterSlayerQuests.Value, Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes"), Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")) + "^", Utility.loadStringShort("UI", "PT_GreatFriends") + ": " + FormatCompletionLine((Farmer farmer) => (float)Math.Floor(Utility.getMaxedFriendshipPercent(farmer) * 100f)) + "%^", Utility.loadStringShort("UI", "PT_FarmerLevel") + ": " + FormatCompletionLine((Farmer farmer) => Math.Min(farmer.Level, 25)) + "/25^", Utility.loadStringShort("UI", "PT_Stardrops") + ": " + FormatCompletionLine((Farmer farmer) => Utility.foundAllStardrops(farmer), Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes"), Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")) + "^", Utility.loadStringShort("UI", "PT_Cooking") + ": " + FormatCompletionLine((Farmer farmer) => (float)Math.Floor(Utility.getCookedRecipesPercent(farmer) * 100f)) + "%^", Utility.loadStringShort("UI", "PT_Crafting") + ": " + FormatCompletionLine((Farmer farmer) => (float)Math.Floor(Utility.getCraftedRecipesPercent(farmer) * 100f)) + "%^", Utility.loadStringShort("UI", "PT_Fish") + ": " + FormatCompletionLine((Farmer farmer) => (float)Math.Floor(Utility.getFishCaughtPercent(farmer) * 100f)) + "%^", Utility.loadStringShort("UI", "PT_GoldenWalnut") + ": " + Math.Min(Game1.netWorldState.Value.GoldenWalnutsFound, 130) + "/" + 130 + "^", "----------------^", Utility.loadStringShort("UI", "PT_Total") + ": " + text), 9999, Game1.uiViewport.Height - 100);
		for (int num2 = 0; num2 < stringBrokenIntoSectionsOfHeight.Count - 1; num2++)
		{
			stringBrokenIntoSectionsOfHeight[num2] += "...\n";
		}
		Game1.drawDialogueNoTyping(stringBrokenIntoSectionsOfHeight);
	}

	/// <summary>Search a garbage can for a player if they haven't searched it today, and give or drop the resulting item (if any).</summary>
	/// <param name="id">The unique ID for the garbage can to search.</param>
	/// <param name="tile">The tile position for the garbage can being searched.</param>
	/// <param name="who">The player performing the search.</param>
	/// <param name="playAnimations">Whether to play animations and sounds.</param>
	/// <param name="reactNpcs">Whether nearby NPCs should react to the search (e.g. friendship point impact or dialogue).</param>
	/// <param name="logError">Log an error if the search fails due to invalid data, or <c>null</c> to fail silently.</param>
	/// <returns>Returns whether the garbage can was searched successfully, regardless of whether an item was found.</returns>
	public virtual bool CheckGarbage(string id, Vector2 tile, Farmer who, bool playAnimations = true, bool reactNpcs = true, Action<string> logError = null)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			logError?.Invoke("must specify a garbage can ID");
			return false;
		}
		switch (id)
		{
		case "0":
			id = "JodiAndKent";
			break;
		case "1":
			id = "EmilyAndHaley";
			break;
		case "2":
			id = "Mayor";
			break;
		case "3":
			id = "Museum";
			break;
		case "4":
			id = "Blacksmith";
			break;
		case "5":
			id = "Saloon";
			break;
		case "6":
			id = "Evelyn";
			break;
		case "7":
			id = "JojaMart";
			break;
		}
		if (!Game1.netWorldState.Value.CheckedGarbage.Add(id))
		{
			Game1.haltAfterCheck = false;
			return true;
		}
		TryGetGarbageItem(id, who.DailyLuck, out var item, out var selected, out var garbageRandom, logError);
		if (playAnimations)
		{
			bool flag = selected?.IsDoubleMegaSuccess ?? false;
			bool flag2 = !flag && (selected?.IsMegaSuccess ?? false);
			if (flag)
			{
				playSound("explosion");
			}
			else if (flag2)
			{
				playSound("crit");
			}
			playSound("trashcan");
			int tileY = (int)tile.Y;
			int num = GetSeasonIndex() * 17;
			TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(22 + num, 0, 16, 10), tile * 64f + new Vector2(0f, -6f) * 4f, flipped: false, 0f, Color.White)
			{
				interval = (flag ? 4000 : 1000),
				motion = (flag ? new Vector2(4f, -20f) : new Vector2(0f, -8f + (flag2 ? (-7f) : ((float)(garbageRandom.Next(-1, 3) + ((garbageRandom.NextDouble() < 0.1) ? (-2) : 0)))))),
				rotationChange = (flag ? 0.4f : 0f),
				acceleration = new Vector2(0f, 0.7f),
				yStopCoordinate = tileY * 64 + -24,
				layerDepth = (flag ? 1f : ((float)((tileY + 1) * 64 + 2) / 10000f)),
				scale = 4f,
				Parent = this,
				shakeIntensity = (flag ? 0f : 1f),
				reachedStopCoordinate = delegate
				{
					removeTemporarySpritesWithID(97654);
					playSound("thudStep");
					for (int i = 0; i < 3; i++)
					{
						temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), tile * 64f + new Vector2(i * 6, -3 + garbageRandom.Next(3)) * 4f, flipped: false, 0.02f, Color.DimGray)
						{
							alpha = 0.85f,
							motion = new Vector2(-0.6f + (float)i * 0.3f, -1f),
							acceleration = new Vector2(0.002f, 0f),
							interval = 99999f,
							layerDepth = (float)((tileY + 1) * 64 + 3) / 10000f,
							scale = 3f,
							scaleChange = 0.02f,
							rotationChange = (float)garbageRandom.Next(-5, 6) * (float)Math.PI / 256f,
							delayBeforeAnimationStart = 50
						});
					}
				},
				id = 97654
			};
			TemporaryAnimatedSprite item2 = new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(22 + num, 11, 16, 16), tile * 64f + new Vector2(0f, -5f) * 4f, flipped: false, 0f, Color.White)
			{
				interval = (flag ? 999999 : 1000),
				layerDepth = (float)((tileY + 1) * 64 + 1) / 10000f,
				scale = 4f,
				id = 97654
			};
			if (flag)
			{
				temporaryAnimatedSprite.reachedStopCoordinate = temporaryAnimatedSprite.bounce;
			}
			TemporaryAnimatedSpriteList temporaryAnimatedSpriteList = new TemporaryAnimatedSpriteList { temporaryAnimatedSprite, item2 };
			for (int num2 = 0; num2 < 5; num2++)
			{
				TemporaryAnimatedSprite item3 = new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(22 + garbageRandom.Next(4) * 4, 32, 4, 4), tile * 64f + new Vector2(Game1.random.Next(13), -3 + Game1.random.Next(3)) * 4f, flipped: false, 0f, Color.White)
				{
					interval = 500f,
					motion = new Vector2(garbageRandom.Next(-2, 3), -5f),
					acceleration = new Vector2(0f, 0.4f),
					layerDepth = (float)((tileY + 1) * 64 + 3) / 10000f,
					scale = 4f,
					color = Utility.getRandomRainbowColor(garbageRandom),
					delayBeforeAnimationStart = garbageRandom.Next(100)
				};
				temporaryAnimatedSpriteList.Add(item3);
			}
			Game1.multiplayer.broadcastSprites(this, temporaryAnimatedSpriteList);
		}
		if (reactNpcs)
		{
			foreach (NPC item4 in Utility.GetNpcsWithinDistance(tile, 7, this))
			{
				if (!(item4 is Horse))
				{
					Game1.multiplayer.globalChatInfoMessage("TrashCan", who.Name, item4.GetTokenizedDisplayName());
					if (item4.Name == "Linus")
					{
						Game1.multiplayer.globalChatInfoMessage("LinusTrashCan");
					}
					CharacterData data = item4.GetData();
					int amount = data?.DumpsterDiveFriendshipEffect ?? (-25);
					int? num3 = data?.DumpsterDiveEmote;
					Dialogue dialogue = item4.TryGetDialogue("DumpsterDiveComment");
					switch (item4.Age)
					{
					case 2:
						num3 = num3 ?? 28;
						dialogue = dialogue ?? new Dialogue(item4, "Data\\ExtraDialogue:Town_DumpsterDiveComment_Child");
						break;
					case 1:
						num3 = num3 ?? 8;
						dialogue = dialogue ?? new Dialogue(item4, "Data\\ExtraDialogue:Town_DumpsterDiveComment_Teen");
						break;
					default:
						num3 = num3 ?? 12;
						dialogue = dialogue ?? new Dialogue(item4, "Data\\ExtraDialogue:Town_DumpsterDiveComment_Adult");
						break;
					}
					item4.doEmote(num3.Value);
					who.changeFriendship(amount, item4);
					item4.setNewDialogue(dialogue, add: true, clearOnMovement: true);
					Game1.drawDialogue(item4);
					break;
				}
			}
		}
		Game1.stats.Increment("trashCansChecked");
		if (selected != null)
		{
			if (selected.AddToInventoryDirectly)
			{
				who.addItemByMenuIfNecessary(item);
			}
			else
			{
				Vector2 pixelOrigin = new Vector2(tile.X + 0.5f, tile.Y - 1f) * 64f;
				if (selected.CreateMultipleDebris)
				{
					Game1.createMultipleItemDebris(item, pixelOrigin, 2, this, (int)pixelOrigin.Y + 64);
				}
				else
				{
					Game1.createItemDebris(item, pixelOrigin, 2, this, (int)pixelOrigin.Y + 64);
				}
			}
		}
		return true;
	}

	/// <summary>Try to get the item that would be produced by checking a garbage can in the location, without marking it checked or playing animations or sounds.</summary>
	/// <param name="id">The garbage can ID in <c>Data/GarbageCans</c>.</param>
	/// <param name="dailyLuck">The daily luck of the player checking the garbage can.</param>
	/// <param name="item">The item produced by the garbage can, if any.</param>
	/// <param name="selected">The data entry which produced the <paramref name="item" />, if applicable.</param>
	/// <param name="garbageRandom">The RNG used to select the item, and which would normally be used for subsequent effects like animations.</param>
	/// <param name="logError">Log an error if the search fails due to invalid data, or <c>null</c> to fail silently.</param>
	/// <returns>Returns whether an item was produced.</returns>
	public virtual bool TryGetGarbageItem(string id, double dailyLuck, out Item item, out GarbageCanItemData selected, out Random garbageRandom, Action<string> logError = null)
	{
		GarbageCanData garbageCanData = DataLoader.GarbageCans(Game1.content);
		GarbageCanEntryData valueOrDefault = garbageCanData.GarbageCans.GetValueOrDefault(id);
		float num = ((valueOrDefault != null && valueOrDefault.BaseChance > 0f) ? valueOrDefault.BaseChance : garbageCanData.DefaultBaseChance);
		num += (float)dailyLuck;
		if (Game1.player.stats.Get("Book_Trash") != 0)
		{
			num += 0.2f;
		}
		garbageRandom = Utility.CreateDaySaveRandom(777 + Game1.hash.GetDeterministicHashCode(id));
		int num2 = garbageRandom.Next(0, 100);
		for (int i = 0; i < num2; i++)
		{
			garbageRandom.NextDouble();
		}
		num2 = garbageRandom.Next(0, 100);
		for (int j = 0; j < num2; j++)
		{
			garbageRandom.NextDouble();
		}
		selected = null;
		item = null;
		bool flag = garbageRandom.NextDouble() < (double)num;
		ItemQueryContext context = new ItemQueryContext(this, Game1.player, garbageRandom, "garbage data '" + id + "'");
		List<GarbageCanItemData>[] array = new List<GarbageCanItemData>[3]
		{
			garbageCanData.BeforeAll,
			valueOrDefault?.Items,
			garbageCanData.AfterAll
		};
		foreach (List<GarbageCanItemData> list in array)
		{
			if (list == null)
			{
				continue;
			}
			foreach (GarbageCanItemData item3 in list)
			{
				if (string.IsNullOrWhiteSpace(item3.Id))
				{
					logError("ignored item entry with no Id field.");
				}
				else if ((flag || item3.IgnoreBaseChance) && GameStateQuery.CheckConditions(item3.Condition, this, null, null, null, garbageRandom))
				{
					bool error = false;
					Item item2 = ItemQueryResolver.TryResolveRandomItem(item3, context, avoidRepeat: false, null, null, null, delegate(string query, string message)
					{
						error = true;
						logError("failed parsing item query '" + query + "': " + message);
					});
					if (!error)
					{
						selected = item3;
						item = item2;
						break;
					}
				}
			}
			if (selected != null)
			{
				break;
			}
		}
		return item != null;
	}

	/// <summary>Handle an <c>Action</c> property from a <c>Buildings</c> map tile in the location when the player interacts with the tile.</summary>
	/// <param name="fullActionString">The full action string to parse, <strong>excluding</strong> the <c>Action</c> prefix.</param>
	/// <param name="who">The player performing the action.</param>
	/// <param name="tileLocation">The tile coordinate of the action to handle.</param>
	public virtual bool performAction(string fullActionString, Farmer who, Location tileLocation)
	{
		if (fullActionString == null)
		{
			return false;
		}
		string[] action = ArgUtility.SplitBySpace(fullActionString);
		return performAction(action, who, tileLocation);
	}

	/// <summary>Get whether an <c>Action</c> property from a <c>Buildings</c> map tile in the location should be ignored, so it doesn't show an action cursor and isn't triggered on click.</summary>
	/// <param name="action">The action arguments to parse, including the <c>Action</c> prefix.</param>
	/// <param name="who">The player performing the action.</param>
	/// <param name="tileLocation">The tile coordinate of the action to handle.</param>
	public virtual bool ShouldIgnoreAction(string[] action, Farmer who, Location tileLocation)
	{
		string text = ArgUtility.Get(action, 0);
		if (string.IsNullOrWhiteSpace(text))
		{
			return true;
		}
		if (!(text == "DropBox"))
		{
			if (text == "MonsterGrave")
			{
				return !who.eventsSeen.Contains("6963327");
			}
			return false;
		}
		if (Game1.player.team.specialOrders != null)
		{
			string text2 = ArgUtility.Get(action, 1);
			if (text2 != null)
			{
				foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
				{
					if (specialOrder.UsesDropBox(text2))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	/// <summary>Displays a message when a player is not good enough friends with a villager to enter their room.</summary>
	/// <param name="action">The action arguments from <see cref="M:StardewValley.GameLocation.performAction(System.String,StardewValley.Farmer,xTile.Dimensions.Location)" />.</param>
	public virtual void ShowLockedDoorMessage(string[] action)
	{
		Gender value = Gender.Female;
		string text = null;
		string[] array = new string[(action.Length == 2) ? 1 : 2];
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = action[i + 1];
			NPC characterFromName = Game1.getCharacterFromName(text2);
			if (characterFromName != null)
			{
				text = characterFromName.Name;
				value = characterFromName.Gender;
				array[i] = characterFromName.displayName;
				continue;
			}
			if (NPC.TryGetData(text2, out var data))
			{
				text = text2;
				value = data.Gender;
				array[i] = TokenParser.ParseText(data.DisplayName);
				continue;
			}
			return;
		}
		string dialogue;
		if (array.Length > 1)
		{
			dialogue = Game1.content.LoadString("Strings\\Locations:DoorUnlock_NotFriend_Couple", array[0], array[1]);
		}
		else
		{
			string sub = array[0];
			dialogue = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:DoorUnlock_NotFriend_" + text) ?? Game1.content.LoadStringReturnNullIfNotFound($"Strings\\Locations:DoorUnlock_NotFriend_{value}", sub) ?? Game1.content.LoadString("Strings\\Locations:DoorUnlock_NotFriend_Female", sub);
		}
		Game1.drawObjectDialogue(dialogue);
	}

	/// <summary>Handle an <c>Action</c> property from a <c>Buildings</c> map tile in the location when the player interacts with the tile.</summary>
	/// <param name="action">The action arguments to parse, <strong>excluding</strong> the <c>Action</c> prefix.</param>
	/// <param name="who">The player performing the action.</param>
	/// <param name="tileLocation">The tile coordinate of the action to handle.</param>
	public virtual bool performAction(string[] action, Farmer who, Location tileLocation)
	{
		if (ShouldIgnoreAction(action, who, tileLocation))
		{
			return false;
		}
		if (!ArgUtility.TryGet(action, 0, out var value, out var error, allowBlank: true, "string actionType"))
		{
			return LogError(error);
		}
		if (who.IsLocalPlayer)
		{
			if (registeredTileActions.TryGetValue(value, out var value2))
			{
				return value2(this, action, who, new Point(tileLocation.X, tileLocation.Y));
			}
			switch (value)
			{
			case "None":
				return true;
			case "BuildingGoldClock":
			{
				bool flag = !Game1.netWorldState.Value.goldenClocksTurnedOff.Value;
				who.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:GoldClock_" + (flag ? "Off" : "On")), who.currentLocation.createYesNoResponses(), "GoldClock");
				break;
			}
			case "Bobbers":
				Game1.activeClickableMenu = new ChooseFromIconsMenu("bobbers");
				break;
			case "GrandpaMasteryNote":
				Game1.activeClickableMenu = new LetterViewerMenu(Game1.content.LoadString("Strings\\1_6_Strings:GrandpaMasteryNote", Game1.player.Name, Game1.player.farmName));
				break;
			case "Bookseller":
				if (Utility.getDaysOfBooksellerThisSeason().Contains(Game1.dayOfMonth))
				{
					if (Game1.player.mailReceived.Contains("read_a_book"))
					{
						createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:books_welcome"), new Response[3]
						{
							new Response("Buy", Game1.content.LoadString("Strings\\1_6_Strings:buy_books")),
							new Response("Trade", Game1.content.LoadString("Strings\\1_6_Strings:trade_books")),
							new Response("Leave", Game1.content.LoadString("Strings\\1_6_Strings:Leave"))
						}, "Bookseller");
					}
					else
					{
						Utility.TryOpenShopMenu("Bookseller", null, playOpenSound: true);
					}
				}
				break;
			case "MasteryCave_Pedestal":
				Game1.activeClickableMenu = new MasteryTrackerMenu();
				break;
			case "MasteryCave_Farming":
				if (Game1.player.stats.Get(StatKeys.Mastery(0)) >= 0)
				{
					Game1.activeClickableMenu = new MasteryTrackerMenu(0);
				}
				break;
			case "MasteryCave_Fishing":
				if (Game1.player.stats.Get(StatKeys.Mastery(1)) >= 0)
				{
					Game1.activeClickableMenu = new MasteryTrackerMenu(1);
				}
				break;
			case "MasteryCave_Foraging":
				if (Game1.player.stats.Get(StatKeys.Mastery(2)) >= 0)
				{
					Game1.activeClickableMenu = new MasteryTrackerMenu(2);
				}
				break;
			case "MasteryCave_Combat":
				if (Game1.player.stats.Get(StatKeys.Mastery(4)) >= 0)
				{
					Game1.activeClickableMenu = new MasteryTrackerMenu(4);
				}
				break;
			case "MasteryCave_Mining":
				if (Game1.player.stats.Get(StatKeys.Mastery(3)) >= 0)
				{
					Game1.activeClickableMenu = new MasteryTrackerMenu(3);
				}
				break;
			case "MasteryRoom":
			{
				int num2 = Game1.player.farmingLevel.Value / 10 + Game1.player.fishingLevel.Value / 10 + Game1.player.foragingLevel.Value / 10 + Game1.player.miningLevel.Value / 10 + Game1.player.combatLevel.Value / 10;
				if (num2 >= 5)
				{
					Game1.playSound("doorClose");
					Game1.warpFarmer("MasteryCave", 7, 11, 0);
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:MasteryCave", num2));
				}
				break;
			}
			case "PrizeMachine":
				Game1.activeClickableMenu = new PrizeTicketMenu();
				break;
			case "SquidFestBooth":
				createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:SquidFestBooth_Intro"), new Response[3]
				{
					new Response("Rewards", Game1.content.LoadString("Strings\\1_6_Strings:GetRewards")),
					new Response("Explanation", Game1.content.LoadString("Strings\\1_6_Strings:Explanation")),
					new Response("Leave", Game1.content.LoadString("Strings\\1_6_Strings:Leave"))
				}, "SquidFestBooth");
				break;
			case "TroutDerbyBooth":
				createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:FishingDerbyBooth_Intro"), new Response[3]
				{
					new Response("Rewards", Game1.content.LoadString("Strings\\1_6_Strings:GetRewards")),
					new Response("Explanation", Game1.content.LoadString("Strings\\1_6_Strings:Explanation")),
					new Response("Leave", Game1.content.LoadString("Strings\\1_6_Strings:Leave"))
				}, "TroutDerbyBooth");
				break;
			case "FishingDerbySign":
				Game1.activeClickableMenu = new LetterViewerMenu(Game1.content.LoadString(Game1.IsSummer ? "Strings\\1_6_Strings:FishingDerbySign" : "Strings\\1_6_Strings:SquidFestSign"));
				break;
			case "SpecialWaterDroppable":
				if (!(this is MineShaft) || (this as MineShaft).mineLevel == 100)
				{
					if (who?.ActiveObject?.QualifiedItemId == "(O)103")
					{
						localSound("throwDownITem");
						who.reduceActiveItemByOne();
						TemporaryAnimatedSprite tempSprite = new TemporaryAnimatedSprite(103, 9999f, 1, 1, who.position.Value + new Vector2(0f, -128f), flicker: false, bigCraftable: false, flipped: false)
						{
							motion = new Vector2(4f, -4f),
							acceleration = new Vector2(0f, 0.3f),
							yStopCoordinate = (int)who.position.Y,
							id = 777
						};
						who.freezePause = 4000;
						tempSprite.reachedStopCoordinate = delegate
						{
							removeTemporarySpritesWithID(777);
							temporarySprites.Add(new TemporaryAnimatedSprite(28, 300f, 2, 1, tempSprite.position, flicker: false, flipped: false)
							{
								color = Color.OrangeRed
							});
							localSound("dropItemInWater");
							DelayedAction.functionAfterDelay(delegate
							{
								localSound("terraria_boneSerpent");
								temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(128, 96, 32, 32), 70f, 4, 5, tempSprite.position + new Vector2(-5f, -3f) * 4f, flicker: false, flipped: true, 0.99f, 0f, Color.White, 4f, 0f, 0f, 0f));
								temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(128, 96, 32, 32), 60f, 4, 5, tempSprite.position + new Vector2(-5f, 7f) * 4f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f));
								temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(134, 2, 21, 38), 9999f, 1, 1, tempSprite.position, flicker: false, flipped: false, 0.98f, 0f, Color.White, 4f, 0f, 0f, 0f)
								{
									xPeriodic = true,
									xPeriodicLoopTime = 500f,
									xPeriodicRange = 2f,
									motion = new Vector2(0f, -8f)
								});
								for (int i = 0; i < 13; i++)
								{
									temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(134, (i == 12) ? 54 : 41, 21, 12), 9999f, 1, 1, tempSprite.position, flicker: false, flipped: false, 0.97f - (float)i * 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
									{
										xPeriodic = true,
										xPeriodicLoopTime = 500 + Game1.random.Next(-50, 50),
										xPeriodicRange = 2f,
										motion = new Vector2(0f, -8f),
										delayBeforeAnimationStart = 220 + 80 * i
									});
								}
								TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(935, 9999f, 1, 1, tempSprite.position + new Vector2(0f, -128f), flicker: false, bigCraftable: false, flipped: false)
								{
									motion = new Vector2(-4f, -4f),
									acceleration = new Vector2(0f, 0.3f),
									yStopCoordinate = (int)(who.position.Y - 128f + 12f),
									id = 888
								};
								temporaryAnimatedSprite.reachedStopCoordinate = delegate
								{
									who.addItemByMenuIfNecessary(new Object("FarAwayStone", 1));
									who.currentLocation.removeTemporarySpritesWithID(888);
									localSound("coin");
								};
								who.currentLocation.temporarySprites.Add(temporaryAnimatedSprite);
							}, 1000);
						};
						temporarySprites.Add(tempSprite);
						return true;
					}
					if (who?.ActiveObject != null && !who.ActiveObject.questItem.Value && who.ActiveObject.QualifiedItemId != "(O)FarAwayStone" && who.ActiveObject.Edibility <= 0 && !who.ActiveObject.Name.Contains("Totem"))
					{
						ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(who?.ActiveObject.QualifiedItemId);
						if (dataOrErrorItem != null)
						{
							localSound("throwDownITem");
							int _id = Game1.random.Next();
							TemporaryAnimatedSprite tempSprite2 = new TemporaryAnimatedSprite(dataOrErrorItem.GetTextureName(), dataOrErrorItem.GetSourceRect(), 9999f, 1, 1, who.position.Value + new Vector2(0f, -128f), flicker: false, flipped: false)
							{
								motion = new Vector2(4f, -4f),
								acceleration = new Vector2(0f, 0.3f),
								yStopCoordinate = (int)who.position.Y,
								id = _id,
								scale = 4f * ((dataOrErrorItem.GetSourceRect().Height > 32) ? 0.5f : 1f)
							};
							who.reduceActiveItemByOne();
							tempSprite2.reachedStopCoordinate = delegate
							{
								removeTemporarySpritesWithID(_id);
								temporarySprites.Add(new TemporaryAnimatedSprite(28, 300f, 2, 1, tempSprite2.position, flicker: false, flipped: false)
								{
									color = Color.OrangeRed
								});
								temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), tempSprite2.position + new Vector2(2f, 0f) * 4f, flipped: false, 0f, Color.White)
								{
									interval = 50f,
									totalNumberOfLoops = 99999,
									animationLength = 4,
									scale = 4f,
									layerDepth = 0.99f,
									alphaFade = 0.02f
								});
								for (int i = 0; i < 4; i++)
								{
									temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1965, 8, 8), tempSprite2.position + new Vector2(2f, 0f) * 4f, flipped: false, 0f, Color.White)
									{
										motion = new Vector2((float)Game1.random.Next(-15, 26) / 10f, -4f),
										acceleration = new Vector2(0f, (float)Game1.random.Next(3, 7) / 30f),
										interval = 50f,
										totalNumberOfLoops = 99999,
										animationLength = 7,
										scale = 4f,
										layerDepth = 0.99f,
										alphaFade = 0.02f,
										delayBeforeAnimationStart = i * 30
									});
								}
								localSound("dropItemInWater");
								localSound("fireball");
							};
							temporarySprites.Add(tempSprite2);
						}
						return true;
					}
				}
				return false;
			case "ForestPylon":
				if (who?.ActiveObject?.QualifiedItemId == "(O)FarAwayStone")
				{
					who.reduceActiveItemByOne();
					Game1.playSound("openBox");
					Game1.player.mailReceived.Add("hasActivatedForestPylon");
					temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(0, 106, 14, 22), new Vector2(16.6f, 2.5f) * 64f, flipped: false, 0f, Color.White)
					{
						animationLength = 8,
						interval = 100f,
						totalNumberOfLoops = 9999,
						scale = 4f
					});
					Game1.player.freezePause = 3000;
					DelayedAction.functionAfterDelay(delegate
					{
						Game1.globalFadeToBlack(delegate
						{
							startEvent(new Event(Game1.content.LoadString("Strings\\1_6_Strings:ForestPylonEvent")));
						});
					}, 1000);
				}
				else if (who.mailReceived.Contains("hasActivatedForestPylon"))
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:ForestPylonActivated"));
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:ForestPylon"));
				}
				break;
			case "Garbage":
			{
				if (!ArgUtility.TryGet(action, 1, out var id, out error, allowBlank: true, "string id"))
				{
					return LogError(error);
				}
				CheckGarbage(id, new Vector2(tileLocation.X, tileLocation.Y), who, playAnimations: true, reactNpcs: true, delegate(string garbageError)
				{
					Game1.log.Warn($"Ignored invalid 'Action Garbage {id}' property: {garbageError}.");
				});
				Game1.haltAfterCheck = false;
				return true;
			}
			case "kitchen":
			case "Kitchen":
				ActivateKitchen();
				return true;
			case "Forge":
				Game1.activeClickableMenu = new ForgeMenu();
				return true;
			case "SummitBoulder":
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:SummitBoulder"));
				break;
			case "QiCat":
				ShowQiCat();
				break;
			case "QiGemShop":
				return Utility.TryOpenShopMenu("QiGemShop", null, playOpenSound: true);
			case "QiChallengeBoard":
				Game1.player.team.qiChallengeBoardMutex.RequestLock(delegate
				{
					Game1.activeClickableMenu = new SpecialOrdersBoard("Qi")
					{
						behaviorBeforeCleanup = delegate
						{
							Game1.player.team.qiChallengeBoardMutex.ReleaseLock();
						}
					};
				});
				break;
			case "SpecialOrdersPrizeTickets":
				if (Game1.player.stats.Get("specialOrderPrizeTickets") != 0)
				{
					if (Game1.player.couldInventoryAcceptThisItem(ItemRegistry.Create("(O)PrizeTicket")))
					{
						Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)PrizeTicket"));
						Game1.player.stats.Decrement("specialOrderPrizeTickets");
						Game1.playSound("coin");
					}
					else
					{
						Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
					}
				}
				break;
			case "SpecialOrders":
				Game1.player.team.ordersBoardMutex.RequestLock(delegate
				{
					Game1.activeClickableMenu = new SpecialOrdersBoard
					{
						behaviorBeforeCleanup = delegate
						{
							Game1.player.team.ordersBoardMutex.ReleaseLock();
						}
					};
				});
				break;
			case "MonsterGrave":
				Game1.multipleDialogues(Game1.content.LoadString("Strings\\Locations:Backwoods_MonsterGrave").Split('#'));
				break;
			case "ObeliskWarp":
			{
				if (!ArgUtility.TryGet(action, 1, out var value39, out error, allowBlank: true, "string targetLocation") || !ArgUtility.TryGetPoint(action, 2, out var value40, out error, "Point targetTile") || !ArgUtility.TryGetOptionalBool(action, 4, out var value41, out error, defaultValue: false, "bool forceDismount"))
				{
					return LogError(error);
				}
				Building.PerformObeliskWarp(value39, value40.X, value40.Y, value41, who);
				return true;
			}
			case "PlayEvent":
			{
				if (!ArgUtility.TryGet(action, 1, out var value15, out error, allowBlank: true, "string eventId") || !ArgUtility.TryGetOptionalBool(action, 2, out var value16, out error, defaultValue: true, "bool checkPreconditions") || !ArgUtility.TryGetOptionalBool(action, 3, out var value17, out error, defaultValue: true, "bool checkSeen") || !ArgUtility.TryGetOptionalRemainder(action, 4, out var value18))
				{
					return LogError(error);
				}
				if (Game1.PlayEvent(value15, value16, value17))
				{
					return true;
				}
				if (value18 != null)
				{
					return performAction(value18, who, tileLocation);
				}
				return false;
			}
			case "OpenShop":
			{
				if (!ArgUtility.TryGet(action, 1, out var value22, out error, allowBlank: true, "string shopId") || !ArgUtility.TryGetOptional(action, 2, out var value23, out error, null, allowBlank: true, "string direction") || !ArgUtility.TryGetOptionalInt(action, 3, out var value24, out error, -1, "int openTime") || !ArgUtility.TryGetOptionalInt(action, 4, out var value25, out error, -1, "int closeTime") || !ArgUtility.TryGetOptionalInt(action, 5, out var value26, out error, -1, "int shopAreaX") || !ArgUtility.TryGetOptionalInt(action, 6, out var value27, out error, -1, "int shopAreaY") || !ArgUtility.TryGetOptionalInt(action, 7, out var value28, out error, -1, "int shopAreaWidth") || !ArgUtility.TryGetOptionalInt(action, 8, out var value29, out error, -1, "int shopAreaHeight"))
				{
					return LogError(error);
				}
				Microsoft.Xna.Framework.Rectangle? rectangle = null;
				if (value26 != -1 || value27 != -1 || value28 != -1 || value29 != -1)
				{
					if (value26 == -1 || value27 == -1 || value28 == -1 || value29 == -1)
					{
						return LogError("when specifying any of the shop area 'x y width height' arguments (indexes 5-8), all four must be specified");
					}
					rectangle = new Microsoft.Xna.Framework.Rectangle(value26, value27, value28, value29);
				}
				switch (value23)
				{
				case "down":
					if (who.TilePoint.Y < tileLocation.Y)
					{
						return false;
					}
					break;
				case "up":
					if (who.TilePoint.Y > tileLocation.Y)
					{
						return false;
					}
					break;
				case "left":
					if (who.TilePoint.X > tileLocation.X)
					{
						return false;
					}
					break;
				case "right":
					if (who.TilePoint.X < tileLocation.X)
					{
						return false;
					}
					break;
				}
				if ((value24 >= 0 && Game1.timeOfDay < value24) || (value25 >= 0 && Game1.timeOfDay >= value25))
				{
					return false;
				}
				string shopId = value22;
				Microsoft.Xna.Framework.Rectangle? ownerArea = rectangle;
				bool forceOpen = !rectangle.HasValue;
				return Utility.TryOpenShopMenu(shopId, this, ownerArea, null, forceOpen);
			}
			case "Warp_Sunroom_Door":
				if (who.getFriendshipHeartLevelForNPC("Caroline") >= 2)
				{
					playSound("doorClose", new Vector2(tileLocation.X, tileLocation.Y));
					Game1.warpFarmer("Sunroom", 5, 13, flip: false);
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Caroline_Sunroom_Door"));
				}
				break;
			case "DogStatue":
			{
				if (canRespec(0) || canRespec(3) || canRespec(2) || canRespec(4) || canRespec(1))
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatue"), createYesNoResponses(), "dogStatue");
					break;
				}
				string text = Game1.content.LoadString("Strings\\Locations:Sewer_DogStatue");
				text = text.Substring(0, text.LastIndexOf('^'));
				Game1.drawObjectDialogue(text);
				break;
			}
			case "WizardBook":
				if (who.mailReceived.Contains("hasPickedUpMagicInk") || who.hasMagicInk)
				{
					ShowConstructOptions("Wizard");
				}
				break;
			case "EvilShrineLeft":
				if (who.getChildrenCount() == 0)
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_EvilShrineLeftInactive"));
				}
				else
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_EvilShrineLeft"), createYesNoResponses(), "evilShrineLeft");
				}
				break;
			case "EvilShrineCenter":
				if (who.isDivorced())
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_EvilShrineCenter"), createYesNoResponses(), "evilShrineCenter");
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_EvilShrineCenterInactive"));
				}
				break;
			case "EvilShrineRight":
				if (Game1.spawnMonstersAtNight)
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_EvilShrineRightDeActivate"), createYesNoResponses(), "evilShrineRightDeActivate");
				}
				else
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_EvilShrineRightActivate"), createYesNoResponses(), "evilShrineRightActivate");
				}
				break;
			case "Tailoring":
				if (who.eventsSeen.Contains("992559"))
				{
					Game1.activeClickableMenu = new TailoringMenu();
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:HaleyHouse_SewingMachine"));
				}
				break;
			case "DyePot":
				if (who.eventsSeen.Contains("992559"))
				{
					if (!DyeMenu.IsWearingDyeable())
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:DyePot_NoDyeable"));
					}
					else
					{
						Game1.activeClickableMenu = new DyeMenu();
					}
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:HaleyHouse_DyePot"));
				}
				break;
			case "MagicInk":
				if (who.mailReceived.Add("hasPickedUpMagicInk"))
				{
					who.hasMagicInk = true;
					setMapTile(4, 11, 113, "Buildings", "untitled tile sheet");
					who.addItemByMenuIfNecessaryElseHoldUp(new SpecialItem(7));
				}
				break;
			case "LeoParrot":
				(getTemporarySpriteByID(5858585) as EmilysParrot)?.doAction();
				break;
			case "EmilyRoomObject":
				if (Game1.player.eventsSeen.Contains("463391") && Game1.player.spouse != "Emily")
				{
					(getTemporarySpriteByID(5858585) as EmilysParrot)?.doAction();
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:HaleyHouse_EmilyRoomObject"));
				}
				break;
			case "Starpoint":
			{
				if (!ArgUtility.TryGet(action, 1, out var value37, out error, allowBlank: true, "string which"))
				{
					return LogError(error);
				}
				doStarpoint(value37);
				break;
			}
			case "JojaShop":
				Utility.TryOpenShopMenu("Joja", null, playOpenSound: true);
				break;
			case "ColaMachine":
				createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Saloon_ColaMachine_Question"), createYesNoResponses(), "buyJojaCola");
				break;
			case "IceCreamStand":
			{
				Microsoft.Xna.Framework.Rectangle value34 = new Microsoft.Xna.Framework.Rectangle(tileLocation.X, tileLocation.Y - 3, 1, 3);
				Utility.TryOpenShopMenu("IceCreamStand", this, value34);
				break;
			}
			case "WizardShrine":
				createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:WizardTower_WizardShrine").Replace('\n', '^'), createYesNoResponses(), "WizardShrine");
				break;
			case "HMTGF":
				if (who.ActiveObject != null && who.ActiveObject.QualifiedItemId == "(O)155")
				{
					Object obj = ItemRegistry.Create<Object>("(BC)155");
					if (!Game1.player.couldInventoryAcceptThisItem(obj) && Game1.player.ActiveObject.stack.Value > 1)
					{
						Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
						break;
					}
					Game1.player.reduceActiveItemByOne();
					Game1.player.makeThisTheActiveObject(obj);
					localSound("discoverMineral");
					Game1.flashAlpha = 1f;
				}
				break;
			case "HospitalShop":
			{
				Point tilePoint = who.TilePoint;
				Microsoft.Xna.Framework.Rectangle value13 = new Microsoft.Xna.Framework.Rectangle(tilePoint.X - 1, tilePoint.Y - 2, 2, 1);
				Utility.TryOpenShopMenu("Hospital", this, value13);
				break;
			}
			case "BuyBackpack":
			{
				Response response = new Response("Purchase", Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Response2000"));
				Response response2 = new Response("Purchase", Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Response10000"));
				Response response3 = new Response("Not", Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_ResponseNo"));
				if (Game1.player.maxItems.Value == 12)
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Question24"), new Response[2] { response, response3 }, "Backpack");
				}
				else if (Game1.player.maxItems.Value < 36)
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_Question36"), new Response[2] { response2, response3 }, "Backpack");
				}
				break;
			}
			case "BuyQiCoins":
				createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Club_Buy100Coins"), createYesNoResponses(), "BuyQiCoins");
				break;
			case "LumberPile":
				if (!who.hasOrWillReceiveMail("TH_LumberPile") && who.hasOrWillReceiveMail("TH_SandDragon"))
				{
					Game1.player.hasClubCard = true;
					Game1.player.CanMove = false;
					Game1.player.mailReceived.Add("TH_LumberPile");
					Game1.player.addItemByMenuIfNecessaryElseHoldUp(new SpecialItem(2));
					Game1.player.removeQuest("5");
				}
				break;
			case "SandDragon":
				if (who.ActiveObject?.QualifiedItemId == "(O)768" && !who.hasOrWillReceiveMail("TH_SandDragon") && who.hasOrWillReceiveMail("TH_MayorFridge"))
				{
					who.reduceActiveItemByOne();
					Game1.player.CanMove = false;
					localSound("eat");
					Game1.player.mailReceived.Add("TH_SandDragon");
					Game1.multipleDialogues(new string[2]
					{
						Game1.content.LoadString("Strings\\Locations:Desert_SandDragon_ConsumeEssence"),
						Game1.content.LoadString("Strings\\Locations:Desert_SandDragon_MrQiNote")
					});
					Game1.player.removeQuest("4");
					Game1.player.addQuest("5");
				}
				else if (who.hasOrWillReceiveMail("TH_SandDragon"))
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Desert_SandDragon_MrQiNote"));
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Desert_SandDragon_Initial"));
				}
				break;
			case "RailroadBox":
				if (who.ActiveObject?.QualifiedItemId == "(O)394" && !who.hasOrWillReceiveMail("TH_Railroad") && who.hasOrWillReceiveMail("TH_Tunnel"))
				{
					who.reduceActiveItemByOne();
					Game1.player.CanMove = false;
					localSound("Ship");
					Game1.player.mailReceived.Add("TH_Railroad");
					Game1.multipleDialogues(new string[2]
					{
						Game1.content.LoadString("Strings\\Locations:Railroad_Box_ConsumeShell"),
						Game1.content.LoadString("Strings\\Locations:Railroad_Box_MrQiNote")
					});
					Game1.player.removeQuest("2");
					Game1.player.addQuest("3");
				}
				else if (who.hasOrWillReceiveMail("TH_Railroad"))
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Railroad_Box_MrQiNote"));
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Railroad_Box_Initial"));
				}
				break;
			case "TunnelSafe":
				if (who.ActiveObject?.QualifiedItemId == "(O)787" && !who.hasOrWillReceiveMail("TH_Tunnel"))
				{
					who.reduceActiveItemByOne();
					Game1.player.CanMove = false;
					playSound("openBox");
					DelayedAction.playSoundAfterDelay("doorCreakReverse", 500);
					Game1.player.mailReceived.Add("TH_Tunnel");
					Game1.multipleDialogues(new string[2]
					{
						Game1.content.LoadString("Strings\\Locations:Tunnel_TunnelSafe_ConsumeBattery"),
						Game1.content.LoadString("Strings\\Locations:Tunnel_TunnelSafe_MrQiNote")
					});
					Game1.player.addQuest("2");
				}
				else if (who.hasOrWillReceiveMail("TH_Tunnel"))
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Tunnel_TunnelSafe_MrQiNote"));
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Tunnel_TunnelSafe_Initial"));
				}
				break;
			case "SkullDoor":
				if (who.hasSkullKey || Utility.IsPassiveFestivalDay("DesertFestival"))
				{
					if (!who.hasUnlockedSkullDoor && !Utility.IsPassiveFestivalDay("DesertFestival"))
					{
						Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:SkullCave_SkullDoor_Unlock")));
						DelayedAction.playSoundAfterDelay("openBox", 500);
						DelayedAction.playSoundAfterDelay("openBox", 700);
						Game1.addMailForTomorrow("skullCave");
						who.hasUnlockedSkullDoor = true;
						who.completeQuest("19");
					}
					else
					{
						who.completelyStopAnimatingOrDoingAction();
						playSound("doorClose");
						DelayedAction.playSoundAfterDelay("stairsdown", 500, this);
						Game1.enterMine(121);
						MineShaft.numberOfCraftedStairsUsedThisRun = 0;
					}
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:SkullCave_SkullDoor_Locked"));
				}
				break;
			case "Crib":
				foreach (NPC character in characters)
				{
					if (!(character is Child child))
					{
						continue;
					}
					switch (child.Age)
					{
					case 1:
						child.toss(who);
						return true;
					case 0:
						Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:FarmHouse_Crib_NewbornSleeping", character.displayName)));
						return true;
					case 2:
						if (child.isInCrib())
						{
							return character.checkAction(who, this);
						}
						break;
					}
				}
				return false;
			case "WarpGreenhouse":
				if (Game1.MasterPlayer.mailReceived.Contains("ccPantry"))
				{
					who.faceGeneralDirection(new Vector2(tileLocation.X, tileLocation.Y) * 64f);
					playSound("doorClose", new Vector2(tileLocation.X, tileLocation.Y));
					GameLocation locationFromName = Game1.getLocationFromName("Greenhouse");
					int tileX = 10;
					int tileY = 23;
					if (locationFromName != null)
					{
						foreach (Warp warp in locationFromName.warps)
						{
							if (warp.TargetName == "Farm")
							{
								tileX = warp.X;
								tileY = warp.Y - 1;
								break;
							}
						}
					}
					Game1.warpFarmer("Greenhouse", tileX, tileY, flip: false);
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Farm_GreenhouseRuins"));
				}
				break;
			case "Arcade_Prairie":
				showPrairieKingMenu();
				break;
			case "Arcade_Minecart":
				if (who.hasSkullKey)
				{
					Response[] answerChoices2 = new Response[3]
					{
						new Response("Progress", Game1.content.LoadString("Strings\\Locations:Saloon_Arcade_Minecart_ProgressMode")),
						new Response("Endless", Game1.content.LoadString("Strings\\Locations:Saloon_Arcade_Minecart_EndlessMode")),
						new Response("Exit", Game1.content.LoadString("Strings\\Locations:Saloon_Arcade_Minecart_Exit"))
					};
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Saloon_Arcade_Minecart_Menu"), answerChoices2, "MinecartGame");
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Saloon_Arcade_Minecart_Inactive"));
				}
				break;
			case "WarpCommunityCenter":
				if (Game1.MasterPlayer.mailReceived.Contains("ccDoorUnlock") || Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
				{
					playSound("doorClose", new Vector2(tileLocation.X, tileLocation.Y));
					Game1.warpFarmer("CommunityCenter", 32, 23, flip: false);
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8175"));
				}
				break;
			case "AdventureShop":
				adventureShop();
				break;
			case "Warp":
			{
				if (!ArgUtility.TryGetPoint(action, 1, out var value10, out error, "Point tile") || !ArgUtility.TryGet(action, 3, out var value11, out error, allowBlank: true, "string locationName"))
				{
					return LogError(error);
				}
				bool num3 = action.Length < 5;
				who.faceGeneralDirection(new Vector2(tileLocation.X, tileLocation.Y) * 64f);
				Rumble.rumble(0.15f, 200f);
				if (num3)
				{
					playSound("doorClose", new Vector2(tileLocation.X, tileLocation.Y));
				}
				Game1.warpFarmer(value11, value10.X, value10.Y, flip: false);
				break;
			}
			case "WarpWomensLocker":
			{
				if (!ArgUtility.TryGetPoint(action, 1, out var value5, out error, "Point tile") || !ArgUtility.TryGet(action, 3, out var value6, out error, allowBlank: true, "string locationName"))
				{
					return LogError(error);
				}
				bool flag2 = action.Length < 5;
				if (who.IsMale)
				{
					if (who.IsLocalPlayer)
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:WomensLocker_WrongGender"));
					}
					return true;
				}
				who.faceGeneralDirection(new Vector2(tileLocation.X, tileLocation.Y) * 64f);
				if (flag2)
				{
					playSound("doorClose", new Vector2(tileLocation.X, tileLocation.Y));
				}
				Game1.warpFarmer(value6, value5.X, value5.Y, flip: false);
				break;
			}
			case "WarpMensLocker":
			{
				if (!ArgUtility.TryGetPoint(action, 1, out var value50, out error, "Point tile") || !ArgUtility.TryGet(action, 3, out var value51, out error, allowBlank: true, "string locationName"))
				{
					return LogError(error);
				}
				bool flag3 = action.Length < 5;
				if (!who.IsMale)
				{
					if (who.IsLocalPlayer)
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:MensLocker_WrongGender"));
					}
					return true;
				}
				who.faceGeneralDirection(new Vector2(tileLocation.X, tileLocation.Y) * 64f);
				if (flag3)
				{
					playSound("doorClose", new Vector2(tileLocation.X, tileLocation.Y));
				}
				Game1.warpFarmer(value51, value50.X, value50.Y, flip: false);
				break;
			}
			case "LockedDoorWarp":
			{
				if (!ArgUtility.TryGetPoint(action, 1, out var value44, out error, "Point tile") || !ArgUtility.TryGet(action, 3, out var value45, out error, allowBlank: true, "string locationName") || !ArgUtility.TryGetInt(action, 4, out var value46, out error, "int openTime") || !ArgUtility.TryGetInt(action, 5, out var value47, out error, "int closeTime") || !ArgUtility.TryGetOptional(action, 6, out var value48, out error, null, allowBlank: true, "string npcName") || !ArgUtility.TryGetOptionalInt(action, 7, out var value49, out error, 0, "int minFriendship"))
				{
					return LogError(error);
				}
				who.faceGeneralDirection(new Vector2(tileLocation.X, tileLocation.Y) * 64f);
				lockedDoorWarp(value44, value45, value46, value47, value48, value49);
				break;
			}
			case "ConditionalDoor":
				if (action.Length > 1 && !Game1.eventUp)
				{
					if (GameStateQuery.CheckConditions(ArgUtility.UnsplitQuoteAware(action, ' ', 1)))
					{
						openDoor(tileLocation, playSound: true);
						return true;
					}
					string text7 = doesTileHaveProperty(tileLocation.X, tileLocation.Y, "LockedDoorMessage", "Buildings");
					if (text7 != null)
					{
						Game1.drawObjectDialogue(TokenParser.ParseText(Game1.content.LoadString(text7)));
					}
					else
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:LockedDoor"));
					}
				}
				break;
			case "Door":
				if (action.Length > 1 && !Game1.eventUp)
				{
					for (int num4 = 1; num4 < action.Length; num4++)
					{
						string text6 = action[num4];
						string item = "doorUnlock" + text6;
						if (who.getFriendshipHeartLevelForNPC(text6) >= 2 || Game1.player.mailReceived.Contains(item))
						{
							Rumble.rumble(0.1f, 100f);
							Game1.player.mailReceived.Add(item);
							openDoor(tileLocation, playSound: true);
							return true;
						}
						if (text6 == "Sebastian" && IsGreenRainingHere() && Game1.year == 1)
						{
							Rumble.rumble(0.1f, 100f);
							openDoor(tileLocation, playSound: true);
							return true;
						}
					}
					ShowLockedDoorMessage(action);
					break;
				}
				openDoor(tileLocation, playSound: true);
				return true;
			case "Tutorial":
				Game1.activeClickableMenu = new TutorialMenu();
				break;
			case "Message":
			case "MessageSpeech":
			{
				if (!ArgUtility.TryGet(action, 1, out var value36, out error, allowBlank: true, "string translationKey"))
				{
					return LogError(error);
				}
				string text5 = null;
				try
				{
					text5 = Game1.content.LoadStringReturnNullIfNotFound(value36);
				}
				catch (Exception)
				{
					text5 = null;
				}
				if (text5 != null)
				{
					Game1.drawDialogueNoTyping(text5);
				}
				else
				{
					Game1.drawDialogueNoTyping(Game1.content.LoadString("Strings\\StringsFromMaps:" + value36.Replace("\"", "")));
				}
				break;
			}
			case "Dialogue":
			{
				if (!ArgUtility.TryGetRemainder(action, 1, out var value35, out error, ' ', "string dialogue"))
				{
					return LogError(error);
				}
				value35 = TokenParser.ParseText(value35);
				Game1.drawDialogueNoTyping(value35);
				break;
			}
			case "NPCSpeechMessageNoRadius":
			{
				if (!ArgUtility.TryGet(action, 1, out var value30, out error, allowBlank: true, "string npcName") || !ArgUtility.TryGet(action, 2, out var value31, out error, allowBlank: true, "string translationKey"))
				{
					return LogError(error);
				}
				NPC nPC2 = Game1.getCharacterFromName(value30);
				if (nPC2 == null)
				{
					try
					{
						nPC2 = new NPC(null, Vector2.Zero, "", 0, value30, datable: false, Game1.temporaryContent.Load<Texture2D>("Portraits\\" + value30));
					}
					catch (Exception)
					{
						return LogError("couldn't find or create a matching NPC");
					}
				}
				try
				{
					nPC2.setNewDialogue("Strings\\StringsFromMaps:" + value31, add: true);
					Game1.drawDialogue(nPC2);
					return true;
				}
				catch (Exception value32)
				{
					return LogError($"unhandled exception drawing dialogue: {value32}");
				}
			}
			case "NPCMessage":
			{
				if (!ArgUtility.TryGet(action, 1, out var value20, out error, allowBlank: true, "string npcName") || !ArgUtility.TryGetRemainder(action, 2, out var value21, out error, ' ', "string rawMessage"))
				{
					return LogError(error);
				}
				string text2 = value21.Replace("\"", "");
				NPC characterFromName = Game1.getCharacterFromName(value20);
				if (characterFromName != null && characterFromName.currentLocation == who.currentLocation && Utility.tileWithinRadiusOfPlayer(characterFromName.TilePoint.X, characterFromName.TilePoint.Y, 14, who))
				{
					try
					{
						string text3 = text2.Split('/')[0];
						string text4 = text3.Substring(text3.IndexOf(':') + 1);
						characterFromName.setNewDialogue(text3, add: true);
						Game1.drawDialogue(characterFromName);
						switch (text4)
						{
						case "AnimalShop.20":
						case "JoshHouse_Alex_Trash":
						case "SamHouse_Sam_Trash":
						case "SeedShop_Abigail_Drawers":
							if (who != null)
							{
								Game1.multiplayer.globalChatInfoMessage("Caught_Snooping", who.name.Value, characterFromName.GetTokenizedDisplayName());
							}
							break;
						}
						return true;
					}
					catch (Exception)
					{
						return false;
					}
				}
				try
				{
					Game1.drawDialogueNoTyping(Game1.content.LoadString(text2.Split('/')[1]));
					return false;
				}
				catch (Exception)
				{
					return false;
				}
			}
			case "ElliottPiano":
			{
				if (!ArgUtility.TryGetInt(action, 1, out var value14, out error, "int key"))
				{
					return LogError(error);
				}
				playElliottPiano(value14);
				break;
			}
			case "DropBox":
			{
				if (!ArgUtility.TryGet(action, 1, out var value12, out error, allowBlank: true, "string box_id"))
				{
					return LogError(error);
				}
				int minimum_capacity = 0;
				foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
				{
					if (specialOrder.UsesDropBox(value12))
					{
						minimum_capacity = Math.Max(minimum_capacity, specialOrder.GetMinimumDropBoxCapacity(value12));
					}
				}
				foreach (SpecialOrder order in Game1.player.team.specialOrders)
				{
					if (!order.UsesDropBox(value12))
					{
						continue;
					}
					order.donateMutex.RequestLock(delegate
					{
						while (order.donatedItems.Count < minimum_capacity)
						{
							order.donatedItems.Add(null);
						}
						Game1.activeClickableMenu = new QuestContainerMenu(order.donatedItems, 3, order.HighlightAcceptableItems, order.GetAcceptCount, order.UpdateDonationCounts, order.ConfirmCompleteDonations);
					});
					return true;
				}
				return false;
			}
			case "playSound":
			{
				if (!ArgUtility.TryGet(action, 1, out var value19, out error, allowBlank: true, "string audioName"))
				{
					return LogError(error);
				}
				localSound(value19);
				break;
			}
			case "Letter":
			{
				if (!ArgUtility.TryGet(action, 1, out var value9, out error, allowBlank: true, "string translationKey"))
				{
					return LogError(error);
				}
				Game1.drawLetterMessage(Game1.content.LoadString("Strings\\StringsFromMaps:" + value9.Replace("\"", "")));
				break;
			}
			case "MessageOnce":
			{
				if (!ArgUtility.TryGet(action, 1, out var value7, out error, allowBlank: true, "string eventFlag") || !ArgUtility.TryGetRemainder(action, 2, out var value8, out error, ' ', "string dialogue"))
				{
					return LogError(error);
				}
				if (who.eventsSeen.Add(value7))
				{
					Game1.drawObjectDialogue(Game1.parseText(value8));
				}
				break;
			}
			case "Lamp":
				if (lightLevel.Value == 0f)
				{
					lightLevel.Value = 0.6f;
				}
				else
				{
					lightLevel.Value = 0f;
				}
				playSound("openBox");
				break;
			case "Billboard":
				Game1.activeClickableMenu = new Billboard(ArgUtility.Get(action, 1) == "3");
				break;
			case "MinecartTransport":
			{
				string networkId = ArgUtility.Get(action, 1) ?? "Default";
				string excludeDestinationId = ArgUtility.Get(action, 2);
				ShowMineCartMenu(networkId, excludeDestinationId);
				return true;
			}
			case "MineElevator":
				if (MineShaft.lowestLevelReached < 5)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:Mines_MineElevator_NotWorking")));
				}
				else
				{
					Game1.activeClickableMenu = new MineElevatorMenu();
				}
				break;
			case "Mine":
			case "NextMineLevel":
			{
				if (!ArgUtility.TryGetOptionalInt(action, 1, out var value3, out error, 1, "int mineLevel"))
				{
					return LogError(error);
				}
				playSound("stairsdown");
				Game1.enterMine(value3);
				break;
			}
			case "ExitMine":
			{
				Response[] answerChoices = new Response[3]
				{
					new Response("Leave", Game1.content.LoadString("Strings\\Locations:Mines_LeaveMine")),
					new Response("Go", Game1.content.LoadString("Strings\\Locations:Mines_GoUp")),
					new Response("Do", Game1.content.LoadString("Strings\\Locations:Mines_DoNothing"))
				};
				createQuestionDialogue(" ", answerChoices, "ExitMine");
				break;
			}
			case "GoldenScythe":
				if (!Game1.player.mailReceived.Contains("gotGoldenScythe"))
				{
					if (!Game1.player.isInventoryFull())
					{
						Game1.playSound("parry");
						Game1.player.mailReceived.Add("gotGoldenScythe");
						setMapTile(29, 4, 245, "Front", "mine");
						setMapTile(30, 4, 246, "Front", "mine");
						setMapTile(29, 5, 261, "Front", "mine");
						setMapTile(30, 5, 262, "Front", "mine");
						setMapTile(29, 6, 277, "Buildings", "mine");
						setMapTile(30, 56, 278, "Buildings", "mine");
						Game1.player.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create("(W)53"));
					}
					else
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
					}
				}
				else
				{
					Game1.changeMusicTrack("silence");
					performTouchAction("MagicWarp Mine 67 10", Game1.player.getStandingPosition());
				}
				break;
			case "Saloon":
				if (who.TilePoint.Y > tileLocation.Y)
				{
					return saloon(tileLocation);
				}
				return false;
			case "Carpenter":
				if (who.TilePoint.Y > tileLocation.Y)
				{
					return carpenters(tileLocation);
				}
				return false;
			case "AnimalShop":
				if (who.TilePoint.Y > tileLocation.Y)
				{
					return animalShop(tileLocation);
				}
				return false;
			case "Blacksmith":
				if (who.TilePoint.Y > tileLocation.Y)
				{
					return blacksmith(tileLocation);
				}
				return false;
			case "Jukebox":
				Game1.activeClickableMenu = new ChooseFromListMenu(Utility.GetJukeboxTracks(Game1.player, Game1.player.currentLocation), ChooseFromListMenu.playSongAction, isJukebox: true);
				break;
			case "Buy":
			{
				if (!ArgUtility.TryGet(action, 1, out var value43, out error, allowBlank: true, "string which"))
				{
					return LogError(error);
				}
				if (who.TilePoint.Y >= tileLocation.Y)
				{
					return HandleBuyAction(value43);
				}
				return false;
			}
			case "Craft":
				openCraftingMenu();
				break;
			case "MineSign":
			{
				if (!ArgUtility.TryGetRemainder(action, 1, out var value42, out error, ' ', "string dialogue"))
				{
					return LogError(error);
				}
				Game1.drawObjectDialogue(Game1.parseText(value42));
				break;
			}
			case "ClubSlots":
				Game1.currentMinigame = new Slots();
				break;
			case "ClubShop":
				Utility.TryOpenShopMenu("Casino", null, playOpenSound: true);
				break;
			case "ClubCards":
			case "BlackJack":
				if (ArgUtility.Get(action, 1) == "1000")
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Club_CalicoJack_HS"), new Response[2]
					{
						new Response("Play", Game1.content.LoadString("Strings\\Locations:Club_CalicoJack_Play")),
						new Response("Leave", Game1.content.LoadString("Strings\\Locations:Club_CalicoJack_Leave"))
					}, "CalicoJackHS");
				}
				else
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Club_CalicoJack"), new Response[3]
					{
						new Response("Play", Game1.content.LoadString("Strings\\Locations:Club_CalicoJack_Play")),
						new Response("Leave", Game1.content.LoadString("Strings\\Locations:Club_CalicoJack_Leave")),
						new Response("Rules", Game1.content.LoadString("Strings\\Locations:Club_CalicoJack_Rules"))
					}, "CalicoJack");
				}
				break;
			case "QiCoins":
				if (who.clubCoins > 0)
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Club_QiCoins", who.clubCoins));
				}
				else
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Club_QiCoins_BuyStarter"), createYesNoResponses(), "BuyClubCoins");
				}
				break;
			case "FarmerFile":
			case "ClubComputer":
				farmerFile();
				break;
			case "ClubSeller":
				createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Club_ClubSeller"), new Response[2]
				{
					new Response("I'll", Game1.content.LoadString("Strings\\Locations:Club_ClubSeller_Yes")),
					new Response("No", Game1.content.LoadString("Strings\\Locations:Club_ClubSeller_No"))
				}, "ClubSeller");
				break;
			case "Mailbox":
				if (this is Farm && getBuildingAt(new Vector2(tileLocation.X, tileLocation.Y))?.GetIndoors() is FarmHouse { IsOwnedByCurrentPlayer: false })
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Farm_OtherPlayerMailbox"));
				}
				else
				{
					mailbox();
				}
				break;
			case "Notes":
			{
				if (!ArgUtility.TryGetInt(action, 1, out var value38, out error, "int noteId"))
				{
					return LogError(error);
				}
				readNote(value38);
				break;
			}
			case "SpiritAltar":
				if (who.ActiveObject != null && Game1.player.team.sharedDailyLuck.Value != -0.12 && Game1.player.team.sharedDailyLuck.Value != 0.12)
				{
					if (who.ActiveObject.Price >= 60)
					{
						temporarySprites.Add(new TemporaryAnimatedSprite(352, 70f, 2, 2, new Vector2(tileLocation.X * 64, tileLocation.Y * 64), flicker: false, flipped: false));
						Game1.player.team.sharedDailyLuck.Value = 0.12;
						playSound("money");
					}
					else
					{
						temporarySprites.Add(new TemporaryAnimatedSprite(362, 50f, 6, 1, new Vector2(tileLocation.X * 64, tileLocation.Y * 64), flicker: false, flipped: false));
						Game1.player.team.sharedDailyLuck.Value = -0.12;
						playSound("thunder");
					}
					who.ActiveObject = null;
					who.showNotCarrying();
				}
				break;
			case "WizardHatch":
			{
				if (who.friendshipData.TryGetValue("Wizard", out var value33) && value33.Points >= 1000)
				{
					playSound("doorClose", new Vector2(tileLocation.X, tileLocation.Y));
					Game1.warpFarmer("WizardHouseBasement", 4, 4, flip: true);
				}
				else
				{
					NPC nPC3 = characters[0];
					nPC3.CurrentDialogue.Push(new Dialogue(nPC3, "Data\\ExtraDialogue:Wizard_Hatch"));
					Game1.drawDialogue(nPC3);
				}
				break;
			}
			case "EnterSewer":
				if (who.mailReceived.Contains("OpenedSewer"))
				{
					playSound("stairsdown", new Vector2(tileLocation.X, tileLocation.Y));
					Game1.warpFarmer("Sewer", 16, 11, 2);
				}
				else if (who.hasRustyKey)
				{
					playSound("openBox");
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:Forest_OpenedSewer")));
					who.mailReceived.Add("OpenedSewer");
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:LockedDoor"));
				}
				break;
			case "DwarfGrave":
				if (who.canUnderstandDwarves)
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Town_DwarfGrave_Translated").Replace('\n', '^'));
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8214"));
				}
				break;
			case "Yoba":
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:SeedShop_Yoba"));
				break;
			case "ElliottBook":
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ElliottHouse_ElliottBook_Blank"));
				break;
			case "Theater_Poster":
				if (Game1.MasterPlayer.hasOrWillReceiveMail("ccMovieTheater"))
				{
					MovieData movieToday = MovieTheater.GetMovieToday();
					if (movieToday != null)
					{
						Game1.multipleDialogues(new string[2]
						{
							Game1.content.LoadString("Strings\\Locations:Theater_Poster_0", TokenParser.ParseText(movieToday.Title)),
							Game1.content.LoadString("Strings\\Locations:Theater_Poster_1", TokenParser.ParseText(movieToday.Description))
						});
					}
				}
				break;
			case "Theater_PosterComingSoon":
				if (Game1.MasterPlayer.hasOrWillReceiveMail("ccMovieTheater"))
				{
					MovieData upcomingMovie = MovieTheater.GetUpcomingMovie();
					if (upcomingMovie != null)
					{
						Game1.multipleDialogues(new string[1] { Game1.content.LoadString("Strings\\Locations:Theater_Poster_Coming_Soon", TokenParser.ParseText(upcomingMovie.Title)) });
					}
				}
				break;
			case "Theater_Entrance":
			{
				if (!Game1.MasterPlayer.hasOrWillReceiveMail("ccMovieTheater"))
				{
					break;
				}
				if (Game1.player.team.movieMutex.IsLocked())
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieTheater_CurrentlyShowing")));
					break;
				}
				if (Game1.isFestival())
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieTheater_ClosedFestival")));
					break;
				}
				if (Game1.timeOfDay > 2100 || Game1.timeOfDay < 900)
				{
					string sub = Game1.getTimeOfDayString(900).Replace(" ", "");
					string sub2 = Game1.getTimeOfDayString(2100).Replace(" ", "");
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:LockedDoor_OpenRange", sub, sub2));
					break;
				}
				if (Game1.player.lastSeenMovieWeek.Value >= Game1.Date.TotalWeeks)
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Characters:MovieTheater_AlreadySeen"));
					break;
				}
				NPC nPC = null;
				foreach (MovieInvitation movieInvitation in Game1.player.team.movieInvitations)
				{
					if (movieInvitation.farmer == Game1.player && !movieInvitation.fulfilled && MovieTheater.GetFirstInvitedPlayer(movieInvitation.invitedNPC) == Game1.player)
					{
						nPC = movieInvitation.invitedNPC;
						break;
					}
				}
				if (Game1.player.Items.ContainsId("(O)809"))
				{
					string question = ((nPC != null) ? Game1.content.LoadString("Strings\\Characters:MovieTheater_WatchWithFriendPrompt", nPC.displayName) : Game1.content.LoadString("Strings\\Characters:MovieTheater_WatchAlonePrompt"));
					Game1.currentLocation.createQuestionDialogue(question, Game1.currentLocation.createYesNoResponses(), "EnterTheaterSpendTicket");
				}
				else
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieTheater_NoTicket")));
				}
				break;
			}
			case "Theater_BoxOffice":
				if (Game1.MasterPlayer.hasOrWillReceiveMail("ccMovieTheater"))
				{
					if (Game1.isFestival())
					{
						Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Characters:MovieTheater_ClosedFestival")));
					}
					else if (Game1.timeOfDay > 2100)
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Characters:MovieTheater_BoxOfficeClosed"));
					}
					else if (MovieTheater.GetMovieToday() != null)
					{
						Utility.TryOpenShopMenu("BoxOffice", null, playOpenSound: true);
					}
				}
				break;
			case "BuildingChest":
			{
				if (!ArgUtility.TryGet(action, 1, out var value4, out error, allowBlank: true, "string buildingAction"))
				{
					return LogError(error);
				}
				_ = getBuildingAt(new Vector2(tileLocation.X, tileLocation.Y))?.PerformBuildingChestAction(value4, who) ?? false;
				return true;
			}
			case "BuildingToggleAnimalDoor":
			{
				Building buildingAt = getBuildingAt(new Vector2(tileLocation.X, tileLocation.Y));
				if (buildingAt != null)
				{
					if (Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
					{
						buildingAt.ToggleAnimalDoor(who);
					}
					return true;
				}
				break;
			}
			case "BuildingSilo":
			{
				if (!who.IsLocalPlayer)
				{
					break;
				}
				Object activeObject = who.ActiveObject;
				if (activeObject?.QualifiedItemId == "(O)178")
				{
					activeObject.FixStackSize();
					int num = activeObject.Stack - tryToAddHay(activeObject.Stack);
					if (num > 0)
					{
						if (activeObject.ConsumeStack(num) == null)
						{
							who.ActiveObject = null;
						}
						Game1.playSound("Ship");
						DelayedAction.playSoundAfterDelay("grassyStep", 100);
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:AddedHay", num));
					}
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:PiecesOfHay", piecesOfHay, GetHayCapacity()));
				}
				break;
			}
			default:
				return false;
			}
			return true;
		}
		if (value == "Door")
		{
			openDoor(tileLocation, playSound: true);
		}
		return false;
		bool LogError(string errorPhrase)
		{
			LogTileActionError(action, tileLocation.X, tileLocation.Y, errorPhrase);
			return false;
		}
	}

	public void showPrairieKingMenu()
	{
		if (Game1.player.jotpkProgress.Value == null)
		{
			Game1.currentMinigame = new AbigailGame();
			return;
		}
		Response[] answerChoices = new Response[3]
		{
			new Response("Continue", Game1.content.LoadString("Strings\\Locations:Saloon_Arcade_Cowboy_Continue")),
			new Response("NewGame", Game1.content.LoadString("Strings\\Locations:Saloon_Arcade_Cowboy_NewGame")),
			new Response("Exit", Game1.content.LoadString("Strings\\Locations:Saloon_Arcade_Minecart_Exit"))
		};
		createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Saloon_Arcade_Cowboy_Menu"), answerChoices, "CowboyGame");
	}

	/// <summary>Show a minecart destination menu if the network is unlocked.</summary>
	/// <param name="networkId">The network whose destinations to show.</param>
	/// <param name="excludeDestinationId">The destination to hide from the list (usually the ID of the minecart we're using), or <c>null</c> to show all of them.</param>
	public void ShowMineCartMenu(string networkId, string excludeDestinationId)
	{
		if (Game1.player.mount != null)
		{
			return;
		}
		Dictionary<string, MinecartNetworkData> dictionary = DataLoader.Minecarts(Game1.content);
		if (networkId == null || !dictionary.TryGetValue(networkId, out var network))
		{
			Game1.log.Warn("Can't show minecart menu for unknown network ID '" + networkId + "'.");
			return;
		}
		if (!GameStateQuery.CheckConditions(network.UnlockCondition, this))
		{
			Game1.drawObjectDialogue(TokenParser.ParseText(network.LockedMessage) ?? Game1.content.LoadString("Strings\\Locations:MineCart_OutOfOrder"));
			return;
		}
		MinecartNetworkData minecartNetworkData = network;
		if (minecartNetworkData == null || !(minecartNetworkData.Destinations?.Count > 0))
		{
			Game1.log.Warn("Can't show minecart menu for network ID '" + networkId + "' with missing destination data.");
			return;
		}
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		Dictionary<string, MinecartDestinationData> destinationLookup = new Dictionary<string, MinecartDestinationData>();
		foreach (MinecartDestinationData destination2 in network.Destinations)
		{
			if (string.IsNullOrWhiteSpace(destination2?.Id) || string.IsNullOrWhiteSpace(destination2?.TargetLocation))
			{
				Game1.log.Warn($"Ignored invalid minecart destination '{destination2?.Id}' in network '{networkId}' because its ID or location isn't specified.");
			}
			else
			{
				if (destination2.Id.EqualsIgnoreCase(excludeDestinationId) || !GameStateQuery.CheckConditions(destination2.Condition, this))
				{
					continue;
				}
				if (destinationLookup.TryAdd(destination2.Id, destination2))
				{
					string text = TokenParser.ParseText(destination2.DisplayName) ?? destination2.TargetLocation;
					if (destination2.Price > 0)
					{
						text = Game1.content.LoadString("Strings\\Locations:MineCart_DestinationWithPrice", text, destination2.Price);
					}
					list.Add(new KeyValuePair<string, string>(destination2.Id, text));
				}
				else
				{
					Game1.log.Warn($"Ignored minecart destination with duplicate ID '{destination2.Id}' in network '{networkId}'.");
				}
			}
		}
		ShowPagedResponses(TokenParser.ParseText(network.ChooseDestinationMessage) ?? Game1.content.LoadString("Strings\\Locations:MineCart_ChooseDestination"), list, delegate(string destinationId)
		{
			if (destinationLookup.TryGetValue(destinationId, out var destination))
			{
				int price = destination.Price;
				if (price < 1)
				{
					MinecartWarp(destination);
				}
				else
				{
					string numberWithCommas = Utility.getNumberWithCommas(price);
					string text2 = destination.BuyTicketMessage ?? network.BuyTicketMessage;
					text2 = ((text2 != null) ? string.Format(TokenParser.ParseText(network.BuyTicketMessage), numberWithCommas) : Game1.content.LoadString("Strings\\Locations:BuyTicket", numberWithCommas));
					createQuestionDialogue(text2, createYesNoResponses(), delegate(Farmer who, string whichAnswer)
					{
						if (whichAnswer == "Yes")
						{
							if (who.Money >= price)
							{
								who.Money -= price;
								MinecartWarp(destination);
							}
							else
							{
								Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
							}
						}
					});
				}
			}
		});
	}

	/// <summary>Warp to a minecart destination.</summary>
	/// <param name="destination">The minecart destination data.</param>
	public void MinecartWarp(MinecartDestinationData destination)
	{
		GameLocation gameLocation = Game1.RequireLocation(destination.TargetLocation);
		Point targetTile = destination.TargetTile;
		if (!Utility.TryParseDirection(destination.TargetDirection, out var parsed))
		{
			parsed = 2;
		}
		Game1.player.Halt();
		Game1.player.freezePause = 700;
		Game1.warpFarmer(gameLocation.NameOrUniqueName, targetTile.X, targetTile.Y, parsed);
		if (Game1.IsPlayingTownMusic && !gameLocation.IsOutdoors)
		{
			Game1.changeMusicTrack("none");
		}
	}

	public void lockedDoorWarp(Point tile, string locationName, int openTime, int closeTime, string npcName, int minFriendship)
	{
		bool flag = Game1.player.HasTownKey;
		if (AreStoresClosedForFestival() && InValleyContext())
		{
			Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:FestivalDay_DoorLocked")));
			return;
		}
		if (locationName == "SeedShop" && Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Wed") && !Utility.HasAnyPlayerSeenEvent("191393") && !flag)
		{
			Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:SeedShop_LockedWed")));
			return;
		}
		if (locationName == "FishShop" && Game1.player.mailReceived.Contains("willyHours"))
		{
			openTime = 800;
		}
		if (flag)
		{
			if (flag && !InValleyContext())
			{
				flag = false;
			}
			if (flag && this is BeachNightMarket && locationName != "FishShop")
			{
				flag = false;
			}
		}
		Friendship value;
		bool flag2 = (flag || (Game1.timeOfDay >= openTime && Game1.timeOfDay < closeTime)) && (minFriendship <= 0 || IsWinterHere() || (Game1.player.friendshipData.TryGetValue(npcName, out value) && value.Points >= minFriendship));
		if (IsGreenRainingHere() && Game1.year == 1 && !(this is Beach) && !(this is Forest) && !locationName.Equals("AdventureGuild"))
		{
			flag2 = true;
		}
		if (flag2)
		{
			Rumble.rumble(0.15f, 200f);
			Game1.player.completelyStopAnimatingOrDoingAction();
			playSound("doorClose", Game1.player.Tile);
			Game1.warpFarmer(locationName, tile.X, tile.Y, flip: false);
		}
		else if (minFriendship <= 0)
		{
			string sub = Game1.getTimeOfDayString(openTime).Replace(" ", "");
			if (locationName == "FishShop" && Game1.player.mailReceived.Contains("willyHours"))
			{
				sub = Game1.getTimeOfDayString(800).Replace(" ", "");
			}
			string sub2 = Game1.getTimeOfDayString(closeTime).Replace(" ", "");
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:LockedDoor_OpenRange", sub, sub2));
		}
		else if (Game1.timeOfDay < openTime || Game1.timeOfDay >= closeTime)
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:LockedDoor"));
		}
		else
		{
			NPC characterFromName = Game1.getCharacterFromName(npcName);
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:LockedDoor_FriendsOnly", characterFromName.displayName));
		}
	}

	public void playElliottPiano(int key)
	{
		if (Game1.IsMultiplayer && Game1.player.UniqueMultiplayerID % 111 == 0L)
		{
			switch (key)
			{
			case 1:
			{
				int? pitch = 500;
				playSound("toyPiano", null, pitch);
				break;
			}
			case 2:
			{
				int? pitch = 1200;
				playSound("toyPiano", null, pitch);
				break;
			}
			case 3:
			{
				int? pitch = 1400;
				playSound("toyPiano", null, pitch);
				break;
			}
			case 4:
			{
				int? pitch = 2000;
				playSound("toyPiano", null, pitch);
				break;
			}
			}
			return;
		}
		switch (key)
		{
		case 1:
		{
			int? pitch = 1100;
			playSound("toyPiano", null, pitch);
			break;
		}
		case 2:
		{
			int? pitch = 1500;
			playSound("toyPiano", null, pitch);
			break;
		}
		case 3:
		{
			int? pitch = 1600;
			playSound("toyPiano", null, pitch);
			break;
		}
		case 4:
		{
			int? pitch = 1800;
			playSound("toyPiano", null, pitch);
			break;
		}
		}
		switch (Game1.elliottPiano)
		{
		case 0:
			if (key == 2)
			{
				Game1.elliottPiano++;
			}
			else
			{
				Game1.elliottPiano = 0;
			}
			break;
		case 1:
			if (key == 4)
			{
				Game1.elliottPiano++;
			}
			else
			{
				Game1.elliottPiano = 0;
			}
			break;
		case 2:
			if (key == 3)
			{
				Game1.elliottPiano++;
			}
			else
			{
				Game1.elliottPiano = 0;
			}
			break;
		case 3:
			if (key == 2)
			{
				Game1.elliottPiano++;
			}
			else
			{
				Game1.elliottPiano = 0;
			}
			break;
		case 4:
			if (key == 3)
			{
				Game1.elliottPiano++;
			}
			else
			{
				Game1.elliottPiano = 0;
			}
			break;
		case 5:
			if (key == 4)
			{
				Game1.elliottPiano++;
			}
			else
			{
				Game1.elliottPiano = 0;
			}
			break;
		case 6:
			if (key == 2)
			{
				Game1.elliottPiano++;
			}
			else
			{
				Game1.elliottPiano = 0;
			}
			break;
		case 7:
			if (key == 1)
			{
				Game1.elliottPiano = 0;
				NPC characterFromName = getCharacterFromName("Elliott");
				if (!Game1.eventUp && characterFromName != null && !characterFromName.isMoving())
				{
					characterFromName.faceTowardFarmerForPeriod(1000, 100, faceAway: false, Game1.player);
					characterFromName.doEmote(20);
				}
			}
			else
			{
				Game1.elliottPiano = 0;
			}
			break;
		}
	}

	public void readNote(int which)
	{
		if (Game1.netWorldState.Value.LostBooksFound >= which)
		{
			string message = Game1.content.LoadString("Strings\\Notes:" + which).Replace('\n', '^');
			Game1.player.mailReceived.Add("lb_" + which);
			removeTemporarySpritesWithIDLocal(which);
			Game1.drawLetterMessage(message);
		}
		else
		{
			Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Notes:Missing")));
		}
	}

	public void mailbox()
	{
		if (Game1.mailbox.Count > 0)
		{
			string text = Game1.mailbox[0];
			if (!text.Contains("passedOut") && !text.Contains("Cooking"))
			{
				Game1.player.mailReceived.Add(text);
			}
			Game1.mailbox.RemoveAt(0);
			Dictionary<string, string> dictionary = DataLoader.Mail(Game1.content);
			string text2 = dictionary.GetValueOrDefault(text, "");
			if (text.StartsWith("passedOut"))
			{
				if (text.StartsWith("passedOut "))
				{
					string[] array = ArgUtility.SplitBySpace(text);
					int num = ((array.Length > 1) ? Convert.ToInt32(array[1]) : 0);
					text2 = Dialogue.applyGenderSwitchBlocks(str: dictionary[Utility.CreateDaySaveRandom(num).Next((Game1.player.getSpouse() != null && Game1.player.getSpouse().Name.Equals("Harvey")) ? 2 : 3) switch
					{
						0 => (Game1.MasterPlayer.hasCompletedCommunityCenter() && !Game1.MasterPlayer.mailReceived.Contains("JojaMember")) ? "passedOut4" : ("passedOut1_" + ((num > 0) ? "Billed" : "NotBilled") + "_" + (Game1.player.IsMale ? "Male" : "Female")), 
						1 => "passedOut2", 
						_ => "passedOut3_" + ((num > 0) ? "Billed" : "NotBilled"), 
					}], gender: Game1.player.Gender);
					text2 = string.Format(text2, num);
				}
				else
				{
					string[] array2 = ArgUtility.SplitBySpace(text);
					if (array2.Length > 1)
					{
						int num2 = Convert.ToInt32(array2[1]);
						text2 = Dialogue.applyGenderSwitchBlocks(Game1.player.Gender, dictionary[array2[0]]);
						text2 = string.Format(text2, num2);
					}
				}
			}
			if (text2.Length > 0)
			{
				Game1.activeClickableMenu = new LetterViewerMenu(text2, text);
			}
		}
		else if (Game1.mailbox.Count == 0)
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8429"));
		}
	}

	public void farmerFile()
	{
		Game1.multipleDialogues(new string[2]
		{
			Game1.content.LoadString("Strings\\UI:FarmerFile_1", Game1.player.Name, Game1.stats.StepsTaken, Game1.stats.GiftsGiven, Game1.stats.DaysPlayed, Game1.stats.DirtHoed, Game1.stats.ItemsCrafted, Game1.stats.ItemsCooked, Game1.stats.PiecesOfTrashRecycled).Replace('\n', '^'),
			Game1.content.LoadString("Strings\\UI:FarmerFile_2", Game1.stats.MonstersKilled, Game1.stats.FishCaught, Game1.stats.TimesFished, Game1.stats.SeedsSown, Game1.stats.ItemsShipped).Replace('\n', '^')
		});
	}

	/// <summary>Get the number of crops currently planted in this location.</summary>
	public int getTotalCrops()
	{
		int num = 0;
		foreach (TerrainFeature value in terrainFeatures.Values)
		{
			if (value is HoeDirt { crop: not null } hoeDirt && !hoeDirt.crop.dead.Value)
			{
				num++;
			}
		}
		return num;
	}

	/// <summary>Get the number of crops currently planted in this location which are ready to harvest.</summary>
	public int getTotalCropsReadyForHarvest()
	{
		int num = 0;
		foreach (TerrainFeature value in terrainFeatures.Values)
		{
			if (value is HoeDirt hoeDirt && hoeDirt.readyForHarvest())
			{
				num++;
			}
		}
		return num;
	}

	/// <summary>Get the number of crops currently planted in this location which need to be watered.</summary>
	public int getTotalUnwateredCrops()
	{
		int num = 0;
		foreach (TerrainFeature value in terrainFeatures.Values)
		{
			if (value is HoeDirt { crop: not null } hoeDirt && hoeDirt.needsWatering() && !hoeDirt.isWatered())
			{
				num++;
			}
		}
		return num;
	}

	/// <summary>Get the number of crops currently planted in a greenhouse within this location.</summary>
	public int? getTotalGreenhouseCropsReadyForHarvest()
	{
		if (Game1.MasterPlayer.mailReceived.Contains("ccPantry"))
		{
			int num = 0;
			foreach (TerrainFeature value in Game1.RequireLocation("Greenhouse").terrainFeatures.Values)
			{
				if (value is HoeDirt hoeDirt && hoeDirt.readyForHarvest())
				{
					num++;
				}
			}
			return num;
		}
		return null;
	}

	/// <summary>Get the number of tiles currently tilled in this location which don't contain a crop.</summary>
	public int getTotalOpenHoeDirt()
	{
		int num = 0;
		foreach (TerrainFeature value in terrainFeatures.Values)
		{
			if (value is HoeDirt { crop: null } && !objects.ContainsKey(value.Tile))
			{
				num++;
			}
		}
		return num;
	}

	/// <summary>Get the number of forage items currently in this location.</summary>
	public int getTotalForageItems()
	{
		int num = 0;
		foreach (Object value in objects.Values)
		{
			if (value.isSpawnedObject.Value)
			{
				num++;
			}
		}
		return num;
	}

	/// <summary>Get the number of machines within this location with output ready to collect.</summary>
	public int getNumberOfMachinesReadyForHarvest()
	{
		int num = 0;
		foreach (Object value in objects.Values)
		{
			if (value.IsConsideredReadyMachineForComputer())
			{
				num++;
			}
		}
		string text = null;
		if (!(this is Farm))
		{
			if (this is IslandWest islandWest && islandWest.farmhouseRestored.Value)
			{
				text = "IslandFarmHouse";
			}
		}
		else
		{
			text = "FarmHouse";
		}
		if (text != null)
		{
			foreach (Object value2 in Game1.RequireLocation(text).objects.Values)
			{
				if (value2.IsConsideredReadyMachineForComputer())
				{
					num++;
				}
			}
		}
		foreach (Building building in buildings)
		{
			GameLocation indoors = building.GetIndoors();
			if (indoors == null)
			{
				continue;
			}
			foreach (Object value3 in indoors.objects.Values)
			{
				if (value3.IsConsideredReadyMachineForComputer())
				{
					num++;
				}
			}
		}
		return num;
	}

	public static void openCraftingMenu()
	{
		Game1.activeClickableMenu = new GameMenu(GameMenu.craftingTab);
	}

	/// <summary>Handle an <c>Action Buy</c> tile property in this location.</summary>
	/// <param name="which">The legacy shop ID. This is not necessarily the same ID used in <c>Data/ShopData</c>.</param>
	/// <remarks>This is used to apply hardcoded game logic (like showing a message when Pierre is visiting the island). Most code should use <c>Action OpenShop</c> or <see cref="M:StardewValley.Utility.TryOpenShopMenu(System.String,System.String,System.Boolean)" /> instead.</remarks>
	public virtual bool HandleBuyAction(string which)
	{
		if (which.Equals("Fish"))
		{
			int? maxOwnerY = Game1.player.TilePoint.Y - 1;
			return Utility.TryOpenShopMenu("FishShop", this, null, maxOwnerY);
		}
		if (this is SeedShop)
		{
			if (getCharacterFromName("Pierre") == null && Game1.IsVisitingIslandToday("Pierre"))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:SeedShop_MoneyBox"));
				Game1.afterDialogues = delegate
				{
					Utility.TryOpenShopMenu("SeedShop", null, playOpenSound: true);
				};
			}
			else
			{
				Utility.TryOpenShopMenu("SeedShop", this, new Microsoft.Xna.Framework.Rectangle(4, 17, 1, 1), Game1.player.TilePoint.Y - 1);
			}
			return true;
		}
		if (name.Equals("SandyHouse"))
		{
			Utility.TryOpenShopMenu("Sandy", this);
			return true;
		}
		return false;
	}

	public virtual bool isObjectAt(int x, int y)
	{
		Vector2 key = new Vector2(x / 64, y / 64);
		foreach (Furniture item in furniture)
		{
			if (item.boundingBox.Value.Contains(x, y))
			{
				return true;
			}
		}
		return objects.ContainsKey(key);
	}

	public virtual bool isObjectAtTile(int tileX, int tileY)
	{
		Vector2 key = new Vector2(tileX, tileY);
		foreach (Furniture item in furniture)
		{
			if (item.boundingBox.Value.Contains(tileX * 64, tileY * 64))
			{
				return true;
			}
		}
		return objects.ContainsKey(key);
	}

	public virtual Object getObjectAt(int x, int y, bool ignorePassables = false)
	{
		Vector2 key = new Vector2(x / 64, y / 64);
		foreach (Furniture item in furniture)
		{
			if (item.boundingBox.Value.Contains(x, y) && (!ignorePassables || !item.isPassable()))
			{
				return item;
			}
		}
		Object value = null;
		objects.TryGetValue(key, out value);
		if (ignorePassables && value != null && value.isPassable())
		{
			value = null;
		}
		return value;
	}

	public Object getObjectAtTile(int x, int y, bool ignorePassables = false)
	{
		return getObjectAt(x * 64, y * 64, ignorePassables);
	}

	public virtual bool saloon(Location tileLocation)
	{
		NPC characterFromName = getCharacterFromName("Gus");
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(9, 17, 10, 2);
		if (Utility.TryOpenShopMenu("Saloon", this, value))
		{
			characterFromName?.facePlayer(Game1.player);
			return true;
		}
		if (characterFromName == null && Game1.IsVisitingIslandToday("Gus"))
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Saloon_MoneyBox"));
			Game1.afterDialogues = delegate
			{
				Utility.TryOpenShopMenu("Saloon", null, playOpenSound: true);
			};
			return true;
		}
		return false;
	}

	private void adventureShop()
	{
		if (Game1.player.itemsLostLastDeath.Count > 0)
		{
			List<Response> list = new List<Response>
			{
				new Response("Shop", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Shop")),
				new Response("Recovery", Game1.content.LoadString("Strings\\Locations:AdventureGuild_ItemRecovery")),
				new Response("Leave", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Leave"))
			};
			createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:AdventureGuild_Greeting"), list.ToArray(), "adventureGuild");
		}
		else
		{
			Utility.TryOpenShopMenu("AdventureShop", "Marlon");
		}
	}

	public virtual bool carpenters(Location tileLocation)
	{
		foreach (NPC character in characters)
		{
			if (!character.Name.Equals("Robin"))
			{
				continue;
			}
			if (Vector2.Distance(character.Tile, new Vector2(tileLocation.X, tileLocation.Y)) > 3f)
			{
				return false;
			}
			character.faceDirection(2);
			if (Game1.player.daysUntilHouseUpgrade.Value < 0 && !Game1.IsThereABuildingUnderConstruction())
			{
				List<Response> list = new List<Response>();
				list.Add(new Response("Shop", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Shop")));
				if (Game1.IsMasterGame)
				{
					if (Game1.player.houseUpgradeLevel.Value < 3)
					{
						list.Add(new Response("Upgrade", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_UpgradeHouse")));
					}
					else if ((Game1.MasterPlayer.mailReceived.Contains("ccIsComplete") || Game1.MasterPlayer.mailReceived.Contains("JojaMember") || Game1.MasterPlayer.hasCompletedCommunityCenter()) && Game1.RequireLocation<Town>("Town").daysUntilCommunityUpgrade.Value <= 0)
					{
						if (!Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
						{
							list.Add(new Response("CommunityUpgrade", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_CommunityUpgrade")));
						}
						else if (!Game1.MasterPlayer.mailReceived.Contains("communityUpgradeShortcuts"))
						{
							list.Add(new Response("CommunityUpgrade", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_CommunityUpgrade")));
						}
					}
				}
				else if (Game1.player.houseUpgradeLevel.Value < 3)
				{
					list.Add(new Response("Upgrade", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_UpgradeCabin")));
				}
				if (Game1.player.houseUpgradeLevel.Value >= 2)
				{
					if (Game1.IsMasterGame)
					{
						list.Add(new Response("Renovate", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_RenovateHouse")));
					}
					else
					{
						list.Add(new Response("Renovate", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_RenovateCabin")));
					}
				}
				list.Add(new Response("Construct", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Construct")));
				list.Add(new Response("Leave", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Leave")));
				createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu"), list.ToArray(), "carpenter");
			}
			else
			{
				Utility.TryOpenShopMenu("Carpenter", "Robin");
			}
			return true;
		}
		if (getCharacterFromName("Robin") == null && Game1.IsVisitingIslandToday("Robin"))
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ScienceHouse_MoneyBox"));
			Game1.afterDialogues = delegate
			{
				Utility.TryOpenShopMenu("Carpenter", null, playOpenSound: true);
			};
			return true;
		}
		if (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Tue"))
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ScienceHouse_RobinAbsent").Replace('\n', '^'));
			return true;
		}
		return false;
	}

	public virtual bool blacksmith(Location tileLocation)
	{
		foreach (NPC character in characters)
		{
			if (!character.Name.Equals("Clint"))
			{
				continue;
			}
			if (character.Tile != new Vector2(tileLocation.X, tileLocation.Y - 1))
			{
				_ = character.Tile != new Vector2(tileLocation.X - 1, tileLocation.Y - 1);
			}
			character.faceDirection(2);
			if (Game1.player.toolBeingUpgraded.Value != null && Game1.player.daysLeftForToolUpgrade.Value <= 0)
			{
				if (Game1.player.freeSpotsInInventory() > 0 || Game1.player.toolBeingUpgraded.Value is GenericTool)
				{
					Tool value = Game1.player.toolBeingUpgraded.Value;
					Game1.player.toolBeingUpgraded.Value = null;
					Game1.player.hasReceivedToolUpgradeMessageYet = false;
					Game1.player.holdUpItemThenMessage(value);
					if (value is GenericTool)
					{
						value.actionWhenClaimed();
					}
					else
					{
						Game1.player.addItemToInventoryBool(value);
					}
					if (Game1.player.team.useSeparateWallets.Value && value.UpgradeLevel == 4)
					{
						Game1.multiplayer.globalChatInfoMessage("IridiumToolUpgrade", Game1.player.Name, TokenStringBuilder.ToolName(value.QualifiedItemId, value.UpgradeLevel));
					}
				}
				else
				{
					Game1.DrawDialogue(character, "Data\\ExtraDialogue:Clint_NoInventorySpace");
				}
			}
			else
			{
				bool flag = false;
				foreach (Item item in Game1.player.Items)
				{
					if (Utility.IsGeode(item))
					{
						flag = true;
						break;
					}
				}
				Response[] answerChoices = ((!flag) ? new Response[3]
				{
					new Response("Shop", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Shop")),
					new Response("Upgrade", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Upgrade")),
					new Response("Leave", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Leave"))
				} : new Response[4]
				{
					new Response("Shop", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Shop")),
					new Response("Upgrade", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Upgrade")),
					new Response("Process", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Geodes")),
					new Response("Leave", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Leave"))
				});
				createQuestionDialogue("", answerChoices, "Blacksmith");
			}
			return true;
		}
		return false;
	}

	public virtual bool animalShop(Location tileLocation)
	{
		foreach (NPC character in characters)
		{
			if (!character.Name.Equals("Marnie"))
			{
				continue;
			}
			if (character.Tile != new Vector2(tileLocation.X, tileLocation.Y - 1) && character.Tile != new Vector2(tileLocation.X - 1, tileLocation.Y - 1))
			{
				if (Game1.player.stats.Get("Book_AnimalCatalogue") != 0)
				{
					break;
				}
				return false;
			}
			character.faceDirection(2);
			List<Response> list = new List<Response>
			{
				new Response("Supplies", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Supplies")),
				new Response("Purchase", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Animals")),
				new Response("Leave", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Leave"))
			};
			if ((Utility.getAllPets().Count == 0 && Game1.year >= 2) || Game1.player.mailReceived.Contains("MarniePetAdoption") || Game1.player.mailReceived.Contains("MarniePetRejectedAdoption"))
			{
				list.Insert(2, new Response("Adopt", Game1.content.LoadString("Strings\\1_6_Strings:AdoptPets")));
			}
			createQuestionDialogue("", list.ToArray(), "Marnie");
			return true;
		}
		if (getCharacterFromName("Marnie") == null && Game1.IsVisitingIslandToday("Marnie"))
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:AnimalShop_MoneyBox"));
			Game1.afterDialogues = delegate
			{
				Utility.TryOpenShopMenu("AnimalShop", null, playOpenSound: true);
			};
			return true;
		}
		if (Game1.player.stats.Get("Book_AnimalCatalogue") != 0)
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Marnie_Counter"));
			Game1.afterDialogues = delegate
			{
				List<Response> list2 = new List<Response>
				{
					new Response("Supplies", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Supplies")),
					new Response("Purchase", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Animals")),
					new Response("Leave", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Leave"))
				};
				if ((Utility.getAllPets().Count == 0 && Game1.year >= 2) || Game1.player.mailReceived.Contains("MarniePetAdoption") || Game1.player.mailReceived.Contains("MarniePetRejectedAdoption"))
				{
					list2.Insert(2, new Response("Adopt", Game1.content.LoadString("Strings\\1_6_Strings:AdoptPets")));
				}
				createQuestionDialogue("", list2.ToArray(), "Marnie");
			};
			return true;
		}
		if (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Tue"))
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Absent").Replace('\n', '^'));
			return true;
		}
		return false;
	}

	public void removeTile(Location tileLocation, string layer)
	{
		Map.RequireLayer(layer).Tiles[tileLocation.X, tileLocation.Y] = null;
	}

	public void removeTile(int x, int y, string layer)
	{
		Map.RequireLayer(layer).Tiles[x, y] = null;
	}

	public void characterTrampleTile(Vector2 tile)
	{
		if (!(this is FarmHouse) && !(this is IslandFarmHouse) && !(this is Farm))
		{
			terrainFeatures.TryGetValue(tile, out var value);
			if (value is Tree tree && tree.growthStage.Value < 1 && tree.instantDestroy(tile))
			{
				terrainFeatures.Remove(tile);
			}
		}
	}

	public bool characterDestroyObjectWithinRectangle(Microsoft.Xna.Framework.Rectangle rect, bool showDestroyedObject)
	{
		if (this is FarmHouse || this is IslandFarmHouse)
		{
			return false;
		}
		foreach (Farmer farmer in farmers)
		{
			if (rect.Intersects(farmer.GetBoundingBox()))
			{
				return false;
			}
		}
		Vector2 vector = new Vector2(rect.X / 64, rect.Y / 64);
		objects.TryGetValue(vector, out var value);
		if (checkDestroyItem(value, vector, showDestroyedObject))
		{
			return true;
		}
		terrainFeatures.TryGetValue(vector, out var value2);
		if (checkDestroyTerrainFeature(value2, vector))
		{
			return true;
		}
		vector.X = rect.Right / 64;
		objects.TryGetValue(vector, out value);
		if (checkDestroyItem(value, vector, showDestroyedObject))
		{
			return true;
		}
		terrainFeatures.TryGetValue(vector, out value2);
		if (checkDestroyTerrainFeature(value2, vector))
		{
			return true;
		}
		vector.X = rect.X / 64;
		vector.Y = rect.Bottom / 64;
		objects.TryGetValue(vector, out value);
		if (checkDestroyItem(value, vector, showDestroyedObject))
		{
			return true;
		}
		terrainFeatures.TryGetValue(vector, out value2);
		if (checkDestroyTerrainFeature(value2, vector))
		{
			return true;
		}
		vector.X = rect.Right / 64;
		objects.TryGetValue(vector, out value);
		if (checkDestroyItem(value, vector, showDestroyedObject))
		{
			return true;
		}
		terrainFeatures.TryGetValue(vector, out value2);
		if (checkDestroyTerrainFeature(value2, vector))
		{
			return true;
		}
		for (int num = largeTerrainFeatures.Count - 1; num >= 0; num--)
		{
			LargeTerrainFeature largeTerrainFeature = largeTerrainFeatures[num];
			if (largeTerrainFeature.isDestroyedByNPCTrample && largeTerrainFeature.getBoundingBox().Intersects(rect))
			{
				largeTerrainFeature.onDestroy();
				largeTerrainFeatures.RemoveAt(num);
				return true;
			}
		}
		for (int num2 = resourceClumps.Count - 1; num2 >= 0; num2--)
		{
			ResourceClump resourceClump = resourceClumps[num2];
			if (resourceClump.IsGreenRainBush() && resourceClump.getBoundingBox().Intersects(rect) && resourceClump.destroy(null, this, resourceClump.Tile))
			{
				resourceClumps.RemoveAt(num2);
			}
		}
		return false;
	}

	private bool checkDestroyTerrainFeature(TerrainFeature tf, Vector2 tilePositionToTry)
	{
		if (tf is Tree tree && tree.instantDestroy(tilePositionToTry))
		{
			terrainFeatures.Remove(tilePositionToTry);
		}
		return false;
	}

	private bool checkDestroyItem(Object o, Vector2 tilePositionToTry, bool showDestroyedObject)
	{
		if (o != null && !o.isPassable() && !map.RequireLayer("Back").Tiles[(int)tilePositionToTry.X, (int)tilePositionToTry.Y].Properties.ContainsKey("NPCBarrier"))
		{
			if (o.IsSpawnedObject)
			{
				numberOfSpawnedObjectsOnMap--;
			}
			if (showDestroyedObject && !o.bigCraftable.Value)
			{
				TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 150f, 1, 3, new Vector2(tilePositionToTry.X * 64f, tilePositionToTry.Y * 64f), flicker: false, o.flipped.Value)
				{
					alphaFade = 0.01f
				};
				temporaryAnimatedSprite.CopyAppearanceFromItemId(o.QualifiedItemId);
				Game1.multiplayer.broadcastSprites(this, temporaryAnimatedSprite);
			}
			o.performToolAction(null);
			if (objects.ContainsKey(tilePositionToTry))
			{
				if (o is Chest chest)
				{
					if (chest.TryMoveToSafePosition())
					{
						return true;
					}
					chest.destroyAndDropContents(tilePositionToTry * 64f);
				}
				objects.Remove(tilePositionToTry);
			}
			return true;
		}
		return false;
	}

	public Object removeObject(Vector2 location, bool showDestroyedObject)
	{
		objects.TryGetValue(location, out var value);
		if (value != null && (value.CanBeGrabbed || showDestroyedObject))
		{
			if (value.IsSpawnedObject)
			{
				numberOfSpawnedObjectsOnMap--;
			}
			Object obj = objects[location];
			objects.Remove(location);
			if (showDestroyedObject)
			{
				TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 150f, 1, 3, new Vector2(location.X * 64f, location.Y * 64f), flicker: true, obj.bigCraftable.Value, obj.flipped.Value);
				temporaryAnimatedSprite.CopyAppearanceFromItemId(obj.QualifiedItemId, (!(obj.Type == "Crafting")) ? 1 : 0);
				Game1.multiplayer.broadcastSprites(this, temporaryAnimatedSprite);
			}
			if (value.IsWeeds())
			{
				Game1.stats.WeedsEliminated++;
			}
			return obj;
		}
		return null;
	}

	public void removeTileProperty(int tileX, int tileY, string layer, string key)
	{
		try
		{
			(map?.GetLayer(layer)?.Tiles[tileX, tileY])?.Properties.Remove(key);
		}
		catch (Exception)
		{
		}
	}

	public void setTileProperty(int tileX, int tileY, string layer, string key, string value)
	{
		try
		{
			Tile tile = map?.GetLayer(layer)?.Tiles[tileX, tileY];
			if (tile != null)
			{
				tile.Properties[key] = value;
			}
		}
		catch (Exception)
		{
		}
	}

	public void setObjectAt(float x, float y, Object o)
	{
		Vector2 key = new Vector2(x, y);
		objects[key] = o;
	}

	public virtual void cleanupBeforeSave()
	{
		characters.RemoveWhere((NPC npc) => npc is Junimo);
		if (name.Equals("WitchHut"))
		{
			characters.Clear();
		}
		largeTerrainFeatures.RemoveWhere((LargeTerrainFeature feature) => feature is Tent);
		foreach (Building building in buildings)
		{
			building.indoors.Value?.cleanupBeforeSave();
		}
	}

	public virtual void cleanupForVacancy()
	{
		if (Game1.IsMasterGame)
		{
			debris.RemoveWhere((Debris d) => d.isEssentialItem() && d.collect(Game1.player));
		}
	}

	public virtual void cleanupBeforePlayerExit()
	{
		debris.RemoveWhere((Debris d) => d.isEssentialItem() && d.player.Value != null && d.player.Value == Game1.player && d.collect(d.player.Value));
		Game1.currentLightSources.Clear();
		critters?.Clear();
		Game1.onScreenMenus.RemoveWhere(delegate(IClickableMenu menu)
		{
			if (menu.destroy)
			{
				(menu as IDisposable)?.Dispose();
				return true;
			}
			return false;
		});
		AmbientLocationSounds.onLocationLeave();
		Game1.player.rightRing.Value?.onLeaveLocation(Game1.player, this);
		Game1.player.leftRing.Value?.onLeaveLocation(Game1.player, this);
		if (name.Equals("AbandonedJojaMart") && farmers.Count <= 1)
		{
			characters.RemoveWhere((NPC npc) => npc is Junimo);
		}
		furnitureToRemove.Clear();
		interiorDoors.CleanUpLocalState();
		Game1.temporaryContent.Unload();
		Utility.CollectGarbage();
	}

	public static string getWeedForSeason(Random r, Season season)
	{
		return season switch
		{
			Season.Spring => r.Choose("(O)784", "(O)674", "(O)675"), 
			Season.Summer => r.Choose("(O)785", "(O)676", "(O)677"), 
			Season.Fall => r.Choose("(O)786", "(O)678", "(O)679"), 
			_ => "(O)674", 
		};
	}

	private void startSleep()
	{
		Game1.player.timeWentToBed.Value = Game1.timeOfDay;
		if (Game1.IsMultiplayer)
		{
			Game1.netReady.SetLocalReady("sleep", ready: true);
			Game1.dialogueUp = false;
			Game1.activeClickableMenu = new ReadyCheckDialog("sleep", allowCancel: true, delegate
			{
				doSleep();
			}, delegate(Farmer who)
			{
				if (Game1.activeClickableMenu is ReadyCheckDialog readyCheckDialog)
				{
					readyCheckDialog.closeDialog(who);
				}
				who.timeWentToBed.Value = 0;
			});
		}
		else
		{
			doSleep();
		}
		if (Game1.IsDedicatedHost || Game1.player.team.announcedSleepingFarmers.Contains(Game1.player))
		{
			return;
		}
		Game1.player.team.announcedSleepingFarmers.Add(Game1.player);
		if (!Game1.IsMultiplayer || (Game1.player.team.sleepAnnounceMode.Value != FarmerTeam.SleepAnnounceModes.All && (Game1.player.team.sleepAnnounceMode.Value != FarmerTeam.SleepAnnounceModes.First || Game1.player.team.announcedSleepingFarmers.Count != 1)))
		{
			return;
		}
		string text = "GoneToBed";
		if (Game1.random.NextDouble() < 0.75)
		{
			if (Game1.timeOfDay < 1800)
			{
				text += "Early";
			}
			else if (Game1.timeOfDay > 2530)
			{
				text += "Late";
			}
		}
		int num = 0;
		for (int num2 = 0; num2 < 2; num2++)
		{
			if (Game1.random.NextDouble() < 0.25)
			{
				num++;
			}
		}
		Game1.multiplayer.globalChatInfoMessage(text + num, Game1.player.displayName);
	}

	protected virtual void _CleanupPagedResponses()
	{
		_PagedResponses.Clear();
		_OnPagedResponse = null;
		_PagedResponsePrompt = null;
	}

	public virtual void ShowPagedResponses(string prompt, List<KeyValuePair<string, string>> responses, Action<string> on_response, bool auto_select_single_choice = false, bool addCancel = true, int itemsPerPage = 5)
	{
		_PagedResponses.Clear();
		_PagedResponses.AddRange(responses);
		_PagedResponsePage = 0;
		_PagedResponseAddCancel = addCancel;
		_PagedResponseItemsPerPage = itemsPerPage;
		_PagedResponsePrompt = prompt;
		_OnPagedResponse = on_response;
		if (_PagedResponses.Count == 1 && auto_select_single_choice)
		{
			on_response(_PagedResponses[0].Key);
		}
		else if (_PagedResponses.Count > 0)
		{
			_ShowPagedResponses(_PagedResponsePage);
		}
	}

	protected virtual void _ShowPagedResponses(int page = -1)
	{
		_PagedResponsePage = page;
		int pagedResponseItemsPerPage = _PagedResponseItemsPerPage;
		int num = (_PagedResponses.Count - 1) / pagedResponseItemsPerPage;
		int num2 = pagedResponseItemsPerPage;
		if (_PagedResponsePage == num - 1 && _PagedResponses.Count % pagedResponseItemsPerPage == 1)
		{
			num2++;
			num--;
		}
		List<Response> list = new List<Response>();
		for (int i = 0; i < num2; i++)
		{
			int num3 = i + _PagedResponsePage * pagedResponseItemsPerPage;
			if (num3 < _PagedResponses.Count)
			{
				KeyValuePair<string, string> keyValuePair = _PagedResponses[num3];
				list.Add(new Response(keyValuePair.Key, keyValuePair.Value));
			}
		}
		if (_PagedResponsePage < num)
		{
			list.Add(new Response("nextPage", Game1.content.LoadString("Strings\\UI:NextPage")));
		}
		if (_PagedResponsePage > 0)
		{
			list.Add(new Response("previousPage", Game1.content.LoadString("Strings\\UI:PreviousPage")));
		}
		if (_PagedResponseAddCancel)
		{
			list.Add(new Response("cancel", Game1.content.LoadString("Strings\\Locations:MineCart_Destination_Cancel")));
		}
		createQuestionDialogue(_PagedResponsePrompt, list.ToArray(), "pagedResponse");
	}

	/// <summary>Show a dialogue menu to choose where to construct buildings.</summary>
	/// <param name="builder">The name of the NPC whose building menu is being shown (the vanilla values are <see cref="F:StardewValley.Game1.builder_robin" /> and <see cref="F:StardewValley.Game1.builder_wizard" />).</param>
	/// <param name="page">The page of location names to show, if there are multiple pages.</param>
	public virtual void ShowConstructOptions(string builder, int page = -1)
	{
		if (builder != null)
		{
			_constructLocationBuilderName = builder;
		}
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		foreach (GameLocation location in Game1.locations)
		{
			if (location.IsBuildableLocation())
			{
				list.Add(new KeyValuePair<string, string>(location.NameOrUniqueName, location.DisplayName));
			}
		}
		if (!list.Any())
		{
			Farm farm = Game1.getFarm();
			list.Add(new KeyValuePair<string, string>(farm.NameOrUniqueName, farm.DisplayName));
		}
		ShowPagedResponses(Game1.content.LoadString("Strings\\Buildings:Construction_ChooseLocation"), list, delegate(string value)
		{
			GameLocation locationFromName = Game1.getLocationFromName(value);
			if (locationFromName != null)
			{
				Game1.activeClickableMenu = new CarpenterMenu(_constructLocationBuilderName, locationFromName);
			}
			else
			{
				Game1.log.Error("Can't find location '" + value + "' for construct menu.");
			}
		}, auto_select_single_choice: true);
	}

	/// <summary>Show a shop menu to select a location (if multiple have animal buildings) and purchase animals.</summary>
	/// <param name="onMenuOpened">An callback to invoke when the purchase menu is opened.</param>
	public void ShowAnimalShopMenu(Action<PurchaseAnimalsMenu> onMenuOpened = null)
	{
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		foreach (GameLocation location in Game1.locations)
		{
			if (location.buildings.Any((Building p) => p.GetIndoors() is AnimalHouse) && (!Game1.IsClient || location.CanBeRemotedlyViewed()))
			{
				list.Add(new KeyValuePair<string, string>(location.NameOrUniqueName, location.DisplayName));
			}
		}
		if (!list.Any())
		{
			Farm farm = Game1.getFarm();
			list.Add(new KeyValuePair<string, string>(farm.NameOrUniqueName, farm.DisplayName));
		}
		Game1.currentLocation.ShowPagedResponses(Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.ChooseLocation"), list, delegate(string value)
		{
			GameLocation locationFromName = Game1.getLocationFromName(value);
			if (locationFromName != null)
			{
				PurchaseAnimalsMenu purchaseAnimalsMenu = new PurchaseAnimalsMenu(Utility.getPurchaseAnimalStock(locationFromName), locationFromName);
				onMenuOpened?.Invoke(purchaseAnimalsMenu);
				Game1.activeClickableMenu = purchaseAnimalsMenu;
			}
			else
			{
				Game1.log.Error("Can't find location '" + value + "' for animal purchase menu.");
			}
		}, auto_select_single_choice: true);
	}

	private void doSleep()
	{
		if (lightLevel.Value == 0f && Game1.timeOfDay < 2000)
		{
			if (!isOutdoors.Value)
			{
				lightLevel.Value = 0.6f;
				localSound("openBox");
			}
			if (Game1.IsMasterGame)
			{
				Game1.NewDay(600f);
			}
		}
		else if (lightLevel.Value > 0f && Game1.timeOfDay >= 2000)
		{
			if (!isOutdoors.Value)
			{
				lightLevel.Value = 0f;
				localSound("openBox");
			}
			if (Game1.IsMasterGame)
			{
				Game1.NewDay(600f);
			}
		}
		else if (Game1.IsMasterGame)
		{
			Game1.NewDay(0f);
		}
		Game1.player.lastSleepLocation.Value = Game1.currentLocation.NameOrUniqueName;
		Game1.player.lastSleepPoint.Value = Game1.player.TilePoint;
		Game1.player.mostRecentBed = Game1.player.Position;
		Game1.player.doEmote(24);
		Game1.player.freezePause = 2000;
	}

	public virtual bool answerDialogueAction(string questionAndAnswer, string[] questionParams)
	{
		switch (questionAndAnswer)
		{
		case null:
			return false;
		case "GoldClock_Yes":
			Game1.netWorldState.Value.goldenClocksTurnedOff.Value = !Game1.netWorldState.Value.goldenClocksTurnedOff.Value;
			Game1.playSound("yoba");
			break;
		case "Bookseller_Buy":
			Utility.TryOpenShopMenu("Bookseller", null, playOpenSound: true);
			break;
		case "Bookseller_Trade":
			Utility.TryOpenShopMenu("BooksellerTrade", null, playOpenSound: true);
			break;
		case "SquidFestBooth_Rewards":
		{
			if (Game1.player.mailReceived.Contains("GotSquidFestReward_" + Game1.year + "_" + Game1.dayOfMonth + "_3") || Game1.player.mailReceived.Contains("GotSquidFestReward_" + Game1.year + "_" + Game1.dayOfMonth + "_3"))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:SquidFest_GotAllRewardsToday"));
				break;
			}
			List<string> list = new List<string>();
			int[] array = ((Game1.dayOfMonth != 12) ? new int[4] { 2, 5, 7, 10 } : new int[4] { 1, 3, 5, 8 });
			int num4 = (int)Game1.stats.Get(StatKeys.SquidFestScore(Game1.dayOfMonth, Game1.year));
			bool flag = false;
			bool flag2 = Game1.player.mailReceived.Contains("GotCrabbingBook");
			for (int num5 = 0; num5 < array.Length; num5++)
			{
				if (num4 < array[num5])
				{
					continue;
				}
				if (!Game1.player.mailReceived.Contains("GotSquidFestReward_" + Game1.year + "_" + Game1.dayOfMonth + "_" + num5))
				{
					list.Add(Game1.dayOfMonth + "_" + num5);
					Game1.player.mailReceived.Add("GotSquidFestReward_" + Game1.year + "_" + Game1.dayOfMonth + "_" + num5);
					flag = false;
					if (!flag2 && num5 >= 3)
					{
						Game1.player.mailReceived.Add("GotCrabbingBook");
					}
				}
				else
				{
					flag = true;
				}
			}
			if (list.Count > 0)
			{
				List<Item> list2 = new List<Item>();
				Random random = Utility.CreateDaySaveRandom(Game1.year * 2000, Game1.dayOfMonth * 10);
				using (List<string>.Enumerator enumerator = list.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						switch (enumerator.Current)
						{
						case "12_0":
							list2.Add(ItemRegistry.Create("(O)DeluxeBait", 20));
							break;
						case "12_1":
							list2.Add((random.NextDouble() < 0.5) ? ItemRegistry.Create("(O)498", 10) : ItemRegistry.Create("(O)MysteryBox", 2));
							list2.Add(ItemRegistry.Create("(O)242"));
							break;
						case "12_2":
							list2.Add(ItemRegistry.Create("(O)797"));
							list2.Add(ItemRegistry.Create("(O)395", 3));
							break;
						case "12_3":
							list2.Add(new Furniture("SquidKid_Painting", Vector2.Zero));
							if (!flag2)
							{
								list2.Add(ItemRegistry.Create("(O)Book_Crabbing"));
								break;
							}
							list2.Add(ItemRegistry.Create("(O)MysteryBox", 3));
							list2.Add(ItemRegistry.Create("(O)265"));
							break;
						case "13_0":
							list2.Add(ItemRegistry.Create("(O)694"));
							break;
						case "13_1":
							list2.Add((random.NextDouble() < 0.5) ? ItemRegistry.Create("(O)498", 15) : ItemRegistry.Create("(O)MysteryBox", 3));
							list2.Add(ItemRegistry.Create("(O)242"));
							break;
						case "13_2":
							list2.Add(ItemRegistry.Create("(O)166"));
							list2.Add(ItemRegistry.Create("(O)253", 3));
							break;
						case "13_3":
							list2.Add(new Hat("SquidHat"));
							if (!flag2)
							{
								list2.Add(ItemRegistry.Create("(O)Book_Crabbing"));
								break;
							}
							list2.Add(ItemRegistry.Create("(O)MysteryBox", 3));
							list2.Add(ItemRegistry.Create("(O)265"));
							break;
						}
					}
				}
				if (list2.Count > 0)
				{
					ItemGrabMenu itemGrabMenu = new ItemGrabMenu(list2).setEssential(essential: true, superEssential: true);
					itemGrabMenu.inventory.showGrayedOutSlots = true;
					itemGrabMenu.source = 2;
					Game1.activeClickableMenu = itemGrabMenu;
				}
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString(flag ? "Strings\\1_6_Strings:SquidFest_AlreadyGotAvailableRewards" : "Strings\\1_6_Strings:SquidFestBooth_NoRewards"));
			}
			break;
		}
		case "SquidFestBooth_Explanation":
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:SquidFestBooth_Explanation"));
			break;
		case "TroutDerbyBooth_Rewards":
			if (Game1.player.Items.CountId("TroutDerbyTag") > 0)
			{
				Item item2 = null;
				int num14 = (int)(Utility.CreateRandom(Game1.uniqueIDForThisGame).Next(10) + Game1.stats.Get("GoldenTagsTurnedIn")) % 10;
				if (Game1.stats.Get("GoldenTagsTurnedIn") == 0)
				{
					item2 = ItemRegistry.Create("(O)TentKit");
				}
				else
				{
					switch (num14)
					{
					case 0:
						item2 = ItemRegistry.Create("(H)BucketHat");
						break;
					case 1:
						item2 = ItemRegistry.Create("(O)710");
						break;
					case 2:
						item2 = ItemRegistry.Create("(O)MysteryBox", 3);
						break;
					case 3:
						item2 = ItemRegistry.Create("(O)72");
						break;
					case 4:
						item2 = ItemRegistry.Create("(F)MountedTrout_Painting");
						break;
					case 5:
						item2 = ItemRegistry.Create("(O)DeluxeBait", 20);
						break;
					case 6:
						item2 = ItemRegistry.Create("(O)253", 2);
						break;
					case 7:
						item2 = ItemRegistry.Create("(O)621");
						break;
					case 8:
						item2 = ItemRegistry.Create("(O)688", 3);
						break;
					case 9:
						item2 = ItemRegistry.Create("(O)749", 3);
						break;
					}
				}
				if (item2 != null && (Game1.player.couldInventoryAcceptThisItem(item2) || Game1.player.Items.CountId("TroutDerbyTag") == 1))
				{
					Game1.stats.Increment("GoldenTagsTurnedIn");
					Game1.player.Items.ReduceId("TroutDerbyTag", 1);
					Game1.player.holdUpItemThenMessage(item2);
					Game1.player.addItemToInventoryBool(item2);
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:FishingDerbyBooth_BagFull"));
				}
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:FishingDerbyBooth_NoTags"));
			}
			break;
		case "TroutDerbyBooth_Explanation":
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:FishingDerbyBooth_Explanation"));
			break;
		case "pagedResponse_cancel":
			_CleanupPagedResponses();
			break;
		case "pagedResponse_nextPage":
			_ShowPagedResponses(_PagedResponsePage + 1);
			break;
		case "pagedResponse_previousPage":
			_ShowPagedResponses(_PagedResponsePage - 1);
			break;
		case "Fizz_Yes":
			if (Game1.player.Money >= 500000)
			{
				Game1.player.Money -= 500000;
				Game1.netWorldState.Value.PerfectionWaivers++;
				DelayedAction.playSoundAfterDelay("qi_shop_purchase", 500);
				getCharacterFromName("Fizz")?.showTextAboveHead(Game1.content.LoadString("Strings\\1_6_Strings:Fizz_Sweet"));
				getCharacterFromName("Fizz")?.shake(500);
				if (Game1.IsMultiplayer)
				{
					Game1.Multiplayer.broadcastGlobalMessage("Strings\\1_6_Strings:Waiver_Note_Multiplayer", false, null, Game1.player.Name);
				}
				else
				{
					Game1.showGlobalMessage(string.Format(Game1.content.LoadString("Strings\\1_6_Strings:Waiver_Note", Game1.netWorldState.Value.PerfectionWaivers.ToString() ?? "")));
				}
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
			}
			break;
		case "EnterTheaterSpendTicket_Yes":
			Game1.player.Items.ReduceId("(O)809", 1);
			Rumble.rumble(0.15f, 200f);
			Game1.player.completelyStopAnimatingOrDoingAction();
			playSound("doorClose", Game1.player.Tile);
			Game1.warpFarmer("MovieTheater", 13, 15, 0);
			break;
		case "EnterTheater_Yes":
			Rumble.rumble(0.15f, 200f);
			Game1.player.completelyStopAnimatingOrDoingAction();
			playSound("doorClose", Game1.player.Tile);
			Game1.warpFarmer("MovieTheater", 13, 15, 0);
			break;
		case "dogStatue_Yes":
		{
			if (Game1.player.Money < 10000)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:BusStop_NotEnoughMoneyForTicket"));
				break;
			}
			List<Response> list3 = new List<Response>();
			if (canRespec(0))
			{
				list3.Add(new Response("farming", Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11604")));
			}
			if (canRespec(3))
			{
				list3.Add(new Response("mining", Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11605")));
			}
			if (canRespec(2))
			{
				list3.Add(new Response("foraging", Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11606")));
			}
			if (canRespec(1))
			{
				list3.Add(new Response("fishing", Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11607")));
			}
			if (canRespec(4))
			{
				list3.Add(new Response("combat", Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11608")));
			}
			list3.Add(new Response("cancel", Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueCancel")));
			createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueQuestion"), list3.ToArray(), "professionForget");
			break;
		}
		case "professionForget_farming":
		{
			if (Game1.player.newLevels.Contains(new Point(0, 5)) || Game1.player.newLevels.Contains(new Point(0, 10)))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueAlready"));
				break;
			}
			Game1.player.Money = Math.Max(0, Game1.player.Money - 10000);
			RemoveProfession(0);
			RemoveProfession(1);
			RemoveProfession(3);
			RemoveProfession(5);
			RemoveProfession(2);
			RemoveProfession(4);
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueFinished"));
			int num15 = Farmer.checkForLevelGain(0, Game1.player.experiencePoints[0]);
			if (num15 >= 5)
			{
				Game1.player.newLevels.Add(new Point(0, 5));
			}
			if (num15 >= 10)
			{
				Game1.player.newLevels.Add(new Point(0, 10));
			}
			DelayedAction.playSoundAfterDelay("dog_bark", 300);
			DelayedAction.playSoundAfterDelay("dog_bark", 900);
			break;
		}
		case "professionForget_mining":
		{
			if (Game1.player.newLevels.Contains(new Point(3, 5)) || Game1.player.newLevels.Contains(new Point(3, 10)))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueAlready"));
				break;
			}
			Game1.player.Money = Math.Max(0, Game1.player.Money - 10000);
			RemoveProfession(23);
			RemoveProfession(21);
			RemoveProfession(18);
			RemoveProfession(19);
			RemoveProfession(22);
			RemoveProfession(20);
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueFinished"));
			int num6 = Farmer.checkForLevelGain(0, Game1.player.experiencePoints[3]);
			if (num6 >= 5)
			{
				Game1.player.newLevels.Add(new Point(3, 5));
			}
			if (num6 >= 10)
			{
				Game1.player.newLevels.Add(new Point(3, 10));
			}
			DelayedAction.playSoundAfterDelay("dog_bark", 300);
			DelayedAction.playSoundAfterDelay("dog_bark", 900);
			break;
		}
		case "professionForget_foraging":
		{
			if (Game1.player.newLevels.Contains(new Point(2, 5)) || Game1.player.newLevels.Contains(new Point(2, 10)))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueAlready"));
				break;
			}
			Game1.player.Money = Math.Max(0, Game1.player.Money - 10000);
			RemoveProfession(16);
			RemoveProfession(14);
			RemoveProfession(17);
			RemoveProfession(12);
			RemoveProfession(13);
			RemoveProfession(15);
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueFinished"));
			int num16 = Farmer.checkForLevelGain(0, Game1.player.experiencePoints[2]);
			if (num16 >= 5)
			{
				Game1.player.newLevels.Add(new Point(2, 5));
			}
			if (num16 >= 10)
			{
				Game1.player.newLevels.Add(new Point(2, 10));
			}
			DelayedAction.playSoundAfterDelay("dog_bark", 300);
			DelayedAction.playSoundAfterDelay("dog_bark", 900);
			break;
		}
		case "professionForget_fishing":
		{
			if (Game1.player.newLevels.Contains(new Point(1, 5)) || Game1.player.newLevels.Contains(new Point(1, 10)))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueAlready"));
				break;
			}
			Game1.player.Money = Math.Max(0, Game1.player.Money - 10000);
			RemoveProfession(8);
			RemoveProfession(11);
			RemoveProfession(10);
			RemoveProfession(6);
			RemoveProfession(9);
			RemoveProfession(7);
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueFinished"));
			int num9 = Farmer.checkForLevelGain(0, Game1.player.experiencePoints[1]);
			if (num9 >= 5)
			{
				Game1.player.newLevels.Add(new Point(1, 5));
			}
			if (num9 >= 10)
			{
				Game1.player.newLevels.Add(new Point(1, 10));
			}
			DelayedAction.playSoundAfterDelay("dog_bark", 300);
			DelayedAction.playSoundAfterDelay("dog_bark", 900);
			break;
		}
		case "professionForget_combat":
		{
			if (Game1.player.newLevels.Contains(new Point(4, 5)) || Game1.player.newLevels.Contains(new Point(4, 10)))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueAlready"));
				break;
			}
			Game1.player.Money = Math.Max(0, Game1.player.Money - 10000);
			RemoveProfession(26);
			RemoveProfession(27);
			RemoveProfession(29);
			RemoveProfession(25);
			RemoveProfession(28);
			RemoveProfession(24);
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Sewer_DogStatueFinished"));
			int num = Farmer.checkForLevelGain(0, Game1.player.experiencePoints[4]);
			if (num >= 5)
			{
				Game1.player.newLevels.Add(new Point(4, 5));
			}
			if (num >= 10)
			{
				Game1.player.newLevels.Add(new Point(4, 10));
			}
			DelayedAction.playSoundAfterDelay("dog_bark", 300);
			DelayedAction.playSoundAfterDelay("dog_bark", 900);
			break;
		}
		case "specialCharmQuestion_Yes":
			if (Game1.player.Items.ContainsId("(O)446"))
			{
				Game1.player.holdUpItemThenMessage(new SpecialItem(3));
				Game1.player.removeFirstOfThisItemFromInventory("446");
				Game1.player.hasSpecialCharm = true;
				Game1.player.mailReceived.Add("SecretNote20_done");
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Town_specialCharmNoFoot"));
			}
			break;
		case "evilShrineLeft_Yes":
			if (Game1.player.Items.ReduceId("(O)74", 1) > 0)
			{
				Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2(156f, 388f), flipped: false, 0f, Color.White)
				{
					interval = 50f,
					totalNumberOfLoops = 99999,
					animationLength = 7,
					layerDepth = 0.038500004f,
					scale = 4f
				});
				for (int num13 = 0; num13 < 20; num13++)
				{
					Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Vector2(2f, 6f) * 64f + new Vector2(Game1.random.Next(-32, 64), Game1.random.Next(16)), flipped: false, 0.002f, Color.LightGray)
					{
						alpha = 0.75f,
						motion = new Vector2(1f, -0.5f),
						acceleration = new Vector2(-0.002f, 0f),
						interval = 99999f,
						layerDepth = 0.0384f + (float)Game1.random.Next(100) / 10000f,
						scale = 3f,
						scaleChange = 0.01f,
						rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f,
						delayBeforeAnimationStart = num13 * 25
					});
				}
				playSound("fireball");
				Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(2f, 5f) * 64f, flicker: false, flipped: true, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					motion = new Vector2(4f, -2f)
				});
				if (Game1.player.getChildrenCount() > 1)
				{
					Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(2f, 5f) * 64f, flicker: false, flipped: true, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(4f, -1.5f),
						delayBeforeAnimationStart = 50
					});
				}
				string text3 = "";
				foreach (Child child in Game1.player.getChildren())
				{
					text3 += Game1.content.LoadString("Strings\\Locations:WitchHut_Goodbye", child.getName());
				}
				Game1.showGlobalMessage(text3);
				Game1.player.getRidOfChildren();
				Game1.multiplayer.globalChatInfoMessage("EvilShrine", Game1.player.name.Value);
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_NoOffering"));
			}
			break;
		case "evilShrineCenter_Yes":
			if (Game1.player.Money >= 30000)
			{
				Game1.player.Money -= 30000;
				Game1.player.wipeExMemories();
				Game1.multiplayer.globalChatInfoMessage("EvilShrine", Game1.player.name.Value);
				Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2(468f, 328f), flipped: false, 0f, Color.White)
				{
					interval = 50f,
					totalNumberOfLoops = 99999,
					animationLength = 7,
					layerDepth = 0.038500004f,
					scale = 4f
				});
				playSound("fireball");
				DelayedAction.playSoundAfterDelay("debuffHit", 500, this);
				int num10 = 0;
				Game1.player.faceDirection(2);
				Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[2]
				{
					new FarmerSprite.AnimationFrame(94, 1500),
					new FarmerSprite.AnimationFrame(0, 1)
				});
				Game1.player.freezePause = 1500;
				Game1.player.jitterStrength = 1f;
				for (int num11 = 0; num11 < 20; num11++)
				{
					Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Vector2(7f, 5f) * 64f + new Vector2(Game1.random.Next(-32, 64), Game1.random.Next(16)), flipped: false, 0.002f, Color.SlateGray)
					{
						alpha = 0.75f,
						motion = new Vector2(0f, -0.5f),
						acceleration = new Vector2(-0.002f, 0f),
						interval = 99999f,
						layerDepth = 0.032f + (float)Game1.random.Next(100) / 10000f,
						scale = 3f,
						scaleChange = 0.01f,
						rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f,
						delayBeforeAnimationStart = num11 * 25
					});
				}
				for (int num12 = 0; num12 < 16; num12++)
				{
					foreach (Vector2 item3 in Utility.getBorderOfThisRectangle(Utility.getRectangleCenteredAt(new Vector2(7f, 5f), 2 + num12 * 2)))
					{
						if (num10 % 2 == 0)
						{
							Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(692, 1853, 4, 4), 25f, 1, 16, item3 * 64f + new Vector2(32f, 32f), flicker: false, flipped: false)
							{
								layerDepth = 1f,
								delayBeforeAnimationStart = num12 * 50,
								scale = 4f,
								scaleChange = 1f,
								color = new Color(255 - Utility.getRedToGreenLerpColor(1f / (float)(num12 + 1)).R, 255 - Utility.getRedToGreenLerpColor(1f / (float)(num12 + 1)).G, 255 - Utility.getRedToGreenLerpColor(1f / (float)(num12 + 1)).B),
								acceleration = new Vector2(-0.1f, 0f)
							});
						}
						num10++;
					}
				}
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_NoOffering"));
			}
			break;
		case "evilShrineRightActivate_Yes":
			if (Game1.player.Items.ReduceId("(O)203", 1) > 0)
			{
				Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2(780f, 388f), flipped: false, 0f, Color.White)
				{
					interval = 50f,
					totalNumberOfLoops = 99999,
					animationLength = 7,
					layerDepth = 0.038500004f,
					scale = 4f
				});
				playSound("fireball");
				DelayedAction.playSoundAfterDelay("batScreech", 500, this);
				for (int num2 = 0; num2 < 20; num2++)
				{
					Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Vector2(12f, 6f) * 64f + new Vector2(Game1.random.Next(-32, 64), Game1.random.Next(16)), flipped: false, 0.002f, Color.DarkSlateBlue)
					{
						alpha = 0.75f,
						motion = new Vector2(-0.1f, -0.5f),
						acceleration = new Vector2(-0.002f, 0f),
						interval = 99999f,
						layerDepth = 0.0384f + (float)Game1.random.Next(100) / 10000f,
						scale = 3f,
						scaleChange = 0.01f,
						rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f,
						delayBeforeAnimationStart = num2 * 60
					});
				}
				Game1.player.freezePause = 1501;
				for (int num3 = 0; num3 < 28; num3++)
				{
					Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(540, 347, 13, 13), 50f, 4, 9999, new Vector2(12f, 5f) * 64f, flicker: false, flipped: true, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						delayBeforeAnimationStart = 500 + num3 * 25,
						motion = new Vector2(Game1.random.Next(1, 5) * Game1.random.Choose(-1, 1), Game1.random.Next(1, 5) * Game1.random.Choose(-1, 1))
					});
				}
				Game1.spawnMonstersAtNight = true;
				Game1.multiplayer.globalChatInfoMessage("MonstersActivated", Game1.player.name.Value);
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_NoOffering"));
			}
			break;
		case "evilShrineRightDeActivate_Yes":
			if (Game1.player.Items.ReduceId("(O)203", 1) > 0)
			{
				Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2(780f, 388f), flipped: false, 0f, Color.White)
				{
					interval = 50f,
					totalNumberOfLoops = 99999,
					animationLength = 7,
					layerDepth = 0.038500004f,
					scale = 4f
				});
				playSound("fireball");
				for (int num17 = 0; num17 < 20; num17++)
				{
					Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Vector2(12f, 6f) * 64f + new Vector2(Game1.random.Next(-32, 64), Game1.random.Next(16)), flipped: false, 0.002f, Color.DarkSlateBlue)
					{
						alpha = 0.75f,
						motion = new Vector2(0f, -0.5f),
						acceleration = new Vector2(-0.002f, 0f),
						interval = 99999f,
						layerDepth = 0.0384f + (float)Game1.random.Next(100) / 10000f,
						scale = 3f,
						scaleChange = 0.01f,
						rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f,
						delayBeforeAnimationStart = num17 * 25
					});
				}
				Game1.spawnMonstersAtNight = false;
				Game1.multiplayer.globalChatInfoMessage("MonstersDeActivated", Game1.player.name.Value);
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:WitchHut_NoOffering"));
			}
			break;
		case "buyJojaCola_Yes":
			if (Game1.player.Money >= 75)
			{
				Game1.player.Money -= 75;
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(O)167"));
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
			}
			break;
		case "WizardShrine_Yes":
			if (Game1.player.Money >= 500)
			{
				Game1.activeClickableMenu = new CharacterCustomization(CharacterCustomization.Source.Wizard);
				Game1.player.Money -= 500;
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney2"));
			}
			break;
		case "Backpack_Purchase":
			if (Game1.player.maxItems.Value == 12 && Game1.player.Money >= 2000)
			{
				Game1.player.Money -= 2000;
				Game1.player.increaseBackpackSize(12);
				Game1.player.holdUpItemThenMessage(new SpecialItem(99, Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8708")));
				Game1.multiplayer.globalChatInfoMessage("BackpackLarge", Game1.player.Name);
			}
			else if (Game1.player.maxItems.Value < 36 && Game1.player.Money >= 10000)
			{
				Game1.player.Money -= 10000;
				Game1.player.maxItems.Value += 12;
				Game1.player.holdUpItemThenMessage(new SpecialItem(99, Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8709")));
				for (int num8 = 0; num8 < Game1.player.maxItems.Value; num8++)
				{
					if (Game1.player.Items.Count <= num8)
					{
						Game1.player.Items.Add(null);
					}
				}
				Game1.multiplayer.globalChatInfoMessage("BackpackDeluxe", Game1.player.Name);
			}
			else if (Game1.player.maxItems.Value != 36)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney2"));
			}
			break;
		case "ClubSeller_I'll":
			if (Game1.player.Money >= 1000000)
			{
				Game1.player.Money -= 1000000;
				Game1.exitActiveMenu();
				Game1.player.forceCanMove();
				Game1.player.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create("(BC)127"));
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Club_ClubSeller_NotEnoughMoney"));
			}
			break;
		case "BuyQiCoins_Yes":
			if (Game1.player.Money >= 1000)
			{
				Game1.player.Money -= 1000;
				localSound("Pickup_Coin15");
				Game1.player.clubCoins += 100;
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8715"));
			}
			break;
		case "Shaft_Jump":
			if (this is MineShaft mineShaft)
			{
				mineShaft.enterMineShaft();
			}
			break;
		case "mariner_Buy":
			if (Game1.player.Money >= 5000)
			{
				Game1.player.Money -= 5000;
				Item item = ItemRegistry.Create("(O)460");
				item.specialItem = true;
				Game1.player.addItemByMenuIfNecessary(item);
				if (Game1.activeClickableMenu == null)
				{
					Game1.player.holdUpItemThenMessage(ItemRegistry.Create("(O)460"));
				}
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
			}
			break;
		case "upgrade_Yes":
			houseUpgradeAccept();
			break;
		case "communityUpgrade_Yes":
			communityUpgradeAccept();
			break;
		case "adventureGuild_Shop":
			Game1.player.forceCanMove();
			Utility.TryOpenShopMenu("AdventureShop", "Marlon");
			break;
		case "adventureGuild_Recovery":
			Game1.player.forceCanMove();
			Utility.TryOpenShopMenu("AdventureGuildRecovery", "Marlon");
			break;
		case "carpenter_Shop":
			Game1.player.forceCanMove();
			Utility.TryOpenShopMenu("Carpenter", "Robin");
			break;
		case "carpenter_Upgrade":
			houseUpgradeOffer();
			break;
		case "carpenter_Renovate":
			Game1.player.forceCanMove();
			HouseRenovation.ShowRenovationMenu();
			break;
		case "carpenter_CommunityUpgrade":
			communityUpgradeOffer();
			break;
		case "carpenter_Construct":
			ShowConstructOptions("Robin");
			break;
		case "Eat_Yes":
			Game1.player.isEating = false;
			Game1.player.eatHeldObject();
			break;
		case "Eat_No":
			Game1.player.isEating = false;
			Game1.player.completelyStopAnimatingOrDoingAction();
			break;
		case "Marnie_Supplies":
			Utility.TryOpenShopMenu("AnimalShop", "Marnie");
			break;
		case "Marnie_Adopt":
			Utility.TryOpenShopMenu("PetAdoption", "Marnie");
			break;
		case "Marnie_Purchase":
			Game1.player.forceCanMove();
			Game1.currentLocation.ShowAnimalShopMenu();
			break;
		case "Blacksmith_Shop":
			Utility.TryOpenShopMenu("Blacksmith", "Clint");
			break;
		case "Blacksmith_Upgrade":
			if (Game1.player.daysLeftForToolUpgrade.Value > 0)
			{
				NPC characterFromName3 = getCharacterFromName("Clint");
				if (characterFromName3 != null)
				{
					Game1.DrawDialogue(characterFromName3, "Data\\ExtraDialogue:Clint_StillWorking", Game1.player.toolBeingUpgraded.Value.DisplayName);
				}
			}
			else
			{
				Utility.TryOpenShopMenu("ClintUpgrade", "Clint");
			}
			break;
		case "Blacksmith_Process":
			Game1.activeClickableMenu = new GeodeMenu();
			break;
		case "Dungeon_Go":
			Game1.enterMine(Game1.CurrentMineLevel + 1);
			break;
		case "Mine_Return":
			Game1.enterMine(Game1.player.deepestMineLevel);
			break;
		case "Mine_Enter":
			Game1.enterMine(1);
			break;
		case "Sleep_Yes":
			startSleep();
			break;
		case "SleepTent_Yes":
			Game1.player.isInBed.Value = true;
			Game1.player.sleptInTemporaryBed.Value = true;
			Game1.displayFarmer = false;
			Game1.playSound("sandyStep");
			DelayedAction.playSoundAfterDelay("sandyStep", 500);
			startSleep();
			break;
		case "Mine_Yes":
			if (Game1.CurrentMineLevel > 120)
			{
				Game1.warpFarmer("SkullCave", 3, 4, 2);
			}
			else
			{
				Game1.warpFarmer("UndergroundMine", 16, 16, flip: false);
			}
			break;
		case "Mine_No":
		{
			Response[] answerChoices = new Response[2]
			{
				new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")),
				new Response("Yes", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes"))
			};
			createQuestionDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:Mines_ResetMine")), answerChoices, "ResetMine");
			break;
		}
		case "ExitMine_Leave":
		case "ExitMine_Yes":
			if (Game1.CurrentMineLevel == 77377)
			{
				Game1.warpFarmer("Mine", 67, 10, flip: true);
			}
			else if (Game1.CurrentMineLevel > 120)
			{
				Game1.warpFarmer("SkullCave", 3, 4, 2);
			}
			else
			{
				Game1.warpFarmer("Mine", 23, 8, flip: false);
			}
			break;
		case "ExitMine_Go":
			Game1.enterMine(Game1.CurrentMineLevel - 1);
			break;
		case "MinecartGame_Endless":
			Game1.currentMinigame = new MineCart(0, 2);
			break;
		case "MinecartGame_Progress":
			Game1.currentMinigame = new MineCart(0, 3);
			break;
		case "CowboyGame_NewGame":
			Game1.player.jotpkProgress.Value = null;
			Game1.currentMinigame = new AbigailGame();
			break;
		case "CowboyGame_Continue":
			Game1.currentMinigame = new AbigailGame();
			break;
		case "ClubCard_Yes.":
		case "ClubCard_That's":
		{
			Game1.addMail("bouncerGone", noLetter: true, sendToEveryone: true);
			playSound("explosion");
			Game1.flashAlpha = 5f;
			characters.Remove(getCharacterFromName("Bouncer"));
			NPC characterFromName2 = getCharacterFromName("Sandy");
			if (characterFromName2 != null)
			{
				characterFromName2.faceDirection(1);
				characterFromName2.setNewDialogue("Data\\ExtraDialogue:Sandy_PlayerClubMember");
				characterFromName2.doEmote(16);
			}
			Game1.pauseThenMessage(500, Game1.content.LoadString("Strings\\Locations:Club_Bouncer_PlayerClubMember"));
			Game1.player.Halt();
			Game1.getCharacterFromName("Mister Qi")?.setNewDialogue("Data\\ExtraDialogue:MisterQi_PlayerClubMember");
			break;
		}
		case "CalicoJack_Rules":
			Game1.multipleDialogues(new string[2]
			{
				Game1.content.LoadString("Strings\\Locations:Club_CalicoJack_Rules1"),
				Game1.content.LoadString("Strings\\Locations:Club_CalicoJack_Rules2")
			});
			break;
		case "CalicoJackHS_Play":
			if (Game1.player.clubCoins >= 1000)
			{
				Game1.currentMinigame = new CalicoJack(-1, highStakes: true);
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Club_CalicoJackHS_NotEnoughCoins"));
			}
			break;
		case "CalicoJack_Play":
			if (Game1.player.clubCoins >= 100)
			{
				Game1.currentMinigame = new CalicoJack();
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Club_CalicoJack_NotEnoughCoins"));
			}
			break;
		case "BuyClubCoins_Yes":
			if (Game1.player.Money >= 1000)
			{
				Game1.player.Money -= 1000;
				Game1.player.clubCoins += 10;
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
			}
			break;
		case "Bouquet_Yes":
			if (Game1.player.Money >= 500)
			{
				if (Game1.player.ActiveObject == null)
				{
					Game1.player.Money -= 500;
					Object obj3 = ItemRegistry.Create<Object>("(O)458");
					obj3.CanBeSetDown = false;
					Game1.player.grabObject(obj3);
					return true;
				}
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
			}
			break;
		case "Mariner_Buy":
			if (Game1.player.Money >= 5000)
			{
				Game1.player.Money -= 5000;
				Object obj2 = ItemRegistry.Create<Object>("(O)460");
				obj2.CanBeSetDown = false;
				Game1.player.grabObject(obj2);
				return true;
			}
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
			break;
		case "ClearHouse_Yes":
		{
			Vector2 tile = Game1.player.Tile;
			Vector2[] adjacentTilesOffsets = Character.AdjacentTilesOffsets;
			foreach (Vector2 vector in adjacentTilesOffsets)
			{
				Vector2 key = tile + vector;
				objects.Remove(key);
			}
			break;
		}
		case "ExitToTitle_Yes":
			Game1.fadeScreenToBlack();
			Game1.exitToTitle = true;
			break;
		case "telephone_Carpenter_HouseCost":
		{
			NPC characterFromName = Game1.getCharacterFromName("Robin");
			string text = "Strings\\Locations:ScienceHouse_Carpenter_UpgradeHouse" + (Game1.player.houseUpgradeLevel.Value + 1);
			string text2 = Game1.content.LoadString(text, "65,000", "100");
			if (text2.Contains('.'))
			{
				text2 = text2.Substring(0, text2.LastIndexOf('.') + 1);
			}
			else if (text2.Contains('。'))
			{
				text2 = text2.Substring(0, text2.LastIndexOf('。') + 1);
			}
			Game1.DrawDialogue(new Dialogue(characterFromName, text, text2)
			{
				overridePortrait = Game1.temporaryContent.Load<Texture2D>("Portraits\\AnsweringMachine")
			});
			Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
			{
				answerDialogueAction("HangUp", LegacyShims.EmptyArray<string>());
			});
			break;
		}
		case "telephone_Carpenter_BuildingCost":
		{
			GameLocation targetLocation = Game1.getFarm();
			if (Game1.currentLocation.IsBuildableLocation())
			{
				targetLocation = Game1.currentLocation;
			}
			Game1.activeClickableMenu = new CarpenterMenu("Robin", targetLocation);
			if (Game1.activeClickableMenu is CarpenterMenu carpenterMenu)
			{
				carpenterMenu.readOnly = true;
				carpenterMenu.behaviorBeforeCleanup = (Action<IClickableMenu>)Delegate.Combine(carpenterMenu.behaviorBeforeCleanup, (Action<IClickableMenu>)delegate
				{
					answerDialogueAction("HangUp", LegacyShims.EmptyArray<string>());
				});
			}
			break;
		}
		case "telephone_Carpenter_ShopStock":
			Utility.TryOpenShopMenu("Carpenter", null, playOpenSound: true);
			if (Game1.activeClickableMenu is ShopMenu shopMenu3)
			{
				shopMenu3.readOnly = true;
				shopMenu3.behaviorBeforeCleanup = (Action<IClickableMenu>)Delegate.Combine(shopMenu3.behaviorBeforeCleanup, (Action<IClickableMenu>)delegate
				{
					answerDialogueAction("HangUp", LegacyShims.EmptyArray<string>());
				});
			}
			break;
		case "telephone_Blacksmith_UpgradeCost":
			answerDialogueAction("Blacksmith_Upgrade", LegacyShims.EmptyArray<string>());
			if (Game1.activeClickableMenu is ShopMenu shopMenu2)
			{
				shopMenu2.readOnly = true;
				shopMenu2.behaviorBeforeCleanup = (Action<IClickableMenu>)Delegate.Combine(shopMenu2.behaviorBeforeCleanup, (Action<IClickableMenu>)delegate
				{
					answerDialogueAction("HangUp", LegacyShims.EmptyArray<string>());
				});
			}
			break;
		case "telephone_SeedShop_CheckSeedStock":
			if (Game1.getLocationFromName("SeedShop") is SeedShop)
			{
				if (Utility.TryOpenShopMenu("SeedShop", null, playOpenSound: true) && Game1.activeClickableMenu is ShopMenu shopMenu)
				{
					shopMenu.readOnly = true;
					shopMenu.behaviorBeforeCleanup = (Action<IClickableMenu>)Delegate.Combine(shopMenu.behaviorBeforeCleanup, (Action<IClickableMenu>)delegate
					{
						answerDialogueAction("HangUp", LegacyShims.EmptyArray<string>());
					});
				}
			}
			else
			{
				answerDialogueAction("HangUp", LegacyShims.EmptyArray<string>());
			}
			break;
		case "telephone_AnimalShop_CheckAnimalPrices":
			Game1.currentLocation.ShowAnimalShopMenu(delegate(PurchaseAnimalsMenu menu)
			{
				menu.readOnly = true;
				menu.behaviorBeforeCleanup = (Action<IClickableMenu>)Delegate.Combine(menu.behaviorBeforeCleanup, (Action<IClickableMenu>)delegate
				{
					answerDialogueAction("HangUp", LegacyShims.EmptyArray<string>());
				});
			});
			break;
		case "ShrineOfSkullChallenge_Yes":
			Game1.player.team.toggleSkullShrineOvernight.Value = true;
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\Locations:ChallengeShrine_Activated"));
			Game1.multiplayer.globalChatInfoMessage(Game1.player.team.skullShrineActivated.Value ? "HardModeSkullCaveDeactivated" : "HardModeSkullCaveActivated", Game1.player.Name);
			playSound(Game1.player.team.skullShrineActivated.Value ? "skeletonStep" : "serpentDie");
			break;
		case "ShrineOfChallenge_Yes":
			Game1.player.team.toggleMineShrineOvernight.Value = true;
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ChallengeShrine_Activated"));
			Game1.multiplayer.globalChatInfoMessage((!Game1.player.team.mineShrineActivated.Value) ? "HardModeMinesActivated" : "HardModeMinesDeactivated", Game1.player.Name);
			DelayedAction.functionAfterDelay(delegate
			{
				if (!Game1.player.team.mineShrineActivated.Value)
				{
					Game1.playSound("fireball");
					temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2(8.75f, 5.8f) * 64f + new Vector2(32f, -32f), flipped: false, 0f, Color.White)
					{
						interval = 50f,
						totalNumberOfLoops = 99999,
						animationLength = 4,
						lightId = "ShrineOfChallenge_Activation_1",
						id = 888,
						lightRadius = 2f,
						scale = 4f,
						yPeriodic = true,
						lightcolor = new Color(100, 0, 0),
						yPeriodicLoopTime = 1000f,
						yPeriodicRange = 4f,
						layerDepth = 0.04544f
					});
					temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2(10.75f, 5.8f) * 64f + new Vector2(32f, -32f), flipped: false, 0f, Color.White)
					{
						interval = 50f,
						totalNumberOfLoops = 99999,
						animationLength = 4,
						lightId = "ShrineOfChallenge_Activation_2",
						id = 889,
						lightRadius = 2f,
						scale = 4f,
						lightcolor = new Color(100, 0, 0),
						yPeriodic = true,
						yPeriodicLoopTime = 1100f,
						yPeriodicRange = 4f,
						layerDepth = 0.04544f
					});
				}
				else
				{
					removeTemporarySpritesWithID(888);
					removeTemporarySpritesWithID(889);
					Game1.playSound("fireball");
				}
			}, 500);
			break;
		default:
			if (questionAndAnswer.StartsWith("pagedResponse"))
			{
				string obj = questionAndAnswer.Substring("pagedResponse".Length + 1);
				Action<string> onPagedResponse = _OnPagedResponse;
				_CleanupPagedResponses();
				onPagedResponse?.Invoke(obj);
			}
			break;
		}
		return true;
	}

	public void playShopPhoneNumberSounds(string whichShop)
	{
		Random random = Utility.CreateRandom(whichShop.GetHashCode());
		DelayedAction.playSoundAfterDelay("telephone_dialtone", 495, null, null, 1200);
		DelayedAction.playSoundAfterDelay("telephone_buttonPush", 1200, null, null, 1200 + random.Next(-4, 5) * 100);
		DelayedAction.playSoundAfterDelay("telephone_buttonPush", 1370, null, null, 1200 + random.Next(-4, 5) * 100);
		DelayedAction.playSoundAfterDelay("telephone_buttonPush", 1600, null, null, 1200 + random.Next(-4, 5) * 100);
		DelayedAction.playSoundAfterDelay("telephone_buttonPush", 1850, null, null, 1200 + random.Next(-4, 5) * 100);
		DelayedAction.playSoundAfterDelay("telephone_buttonPush", 2030, null, null, 1200 + random.Next(-4, 5) * 100);
		DelayedAction.playSoundAfterDelay("telephone_buttonPush", 2250, null, null, 1200 + random.Next(-4, 5) * 100);
		DelayedAction.playSoundAfterDelay("telephone_buttonPush", 2410, null, null, 1200 + random.Next(-4, 5) * 100);
		DelayedAction.playSoundAfterDelay("telephone_ringingInEar", 3150);
	}

	public virtual bool answerDialogue(Response answer)
	{
		string[] array = ((lastQuestionKey != null) ? ArgUtility.SplitBySpace(lastQuestionKey) : null);
		string text = ((array != null) ? (array[0] + "_" + answer.responseKey) : null);
		if (answer.responseKey.Equals("Move"))
		{
			Game1.player.grabObject(actionObjectForQuestionDialogue);
			removeObject(actionObjectForQuestionDialogue.TileLocation, showDestroyedObject: false);
			actionObjectForQuestionDialogue = null;
			return true;
		}
		if (afterQuestion != null)
		{
			afterQuestion(Game1.player, answer.responseKey);
			afterQuestion = null;
			Game1.objectDialoguePortraitPerson = null;
			return true;
		}
		if (text == null)
		{
			return false;
		}
		return answerDialogueAction(text, array);
	}

	public static bool AreStoresClosedForFestival()
	{
		if (Utility.isFestivalDay())
		{
			return Utility.getStartTimeOfFestival() < 1900;
		}
		return false;
	}

	public static void RemoveProfession(int profession)
	{
		if (Game1.player.professions.Remove(profession))
		{
			LevelUpMenu.removeImmediateProfessionPerk(profession);
		}
	}

	public static bool canRespec(int skill_index)
	{
		if (Game1.player.GetUnmodifiedSkillLevel(skill_index) < 5)
		{
			return false;
		}
		if (Game1.player.newLevels.Contains(new Point(skill_index, 5)) || Game1.player.newLevels.Contains(new Point(skill_index, 10)))
		{
			return false;
		}
		return true;
	}

	public void setObject(Vector2 v, Object o)
	{
		objects[v] = o;
	}

	private void houseUpgradeOffer()
	{
		switch (Game1.player.houseUpgradeLevel.Value)
		{
		case 0:
			createQuestionDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:ScienceHouse_Carpenter_UpgradeHouse1")), createYesNoResponses(), "upgrade");
			break;
		case 1:
			createQuestionDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:ScienceHouse_Carpenter_UpgradeHouse2", "65,000", "100")), createYesNoResponses(), "upgrade");
			break;
		case 2:
			createQuestionDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:ScienceHouse_Carpenter_UpgradeHouse3")), createYesNoResponses(), "upgrade");
			break;
		}
	}

	private void communityUpgradeOffer()
	{
		if (!Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
		{
			createQuestionDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:ScienceHouse_Carpenter_CommunityUpgrade1")), createYesNoResponses(), "communityUpgrade");
			Game1.player.team.RequestSetMail(PlayerActionTarget.Host, "pamHouseUpgradeAsked", MailType.Received, add: true);
		}
		else if (!Game1.MasterPlayer.mailReceived.Contains("communityUpgradeShortcuts"))
		{
			createQuestionDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:ScienceHouse_Carpenter_CommunityUpgrade2")), createYesNoResponses(), "communityUpgrade");
		}
	}

	/// <summary>Whether crab pots on a given tile can only catch ocean fish, regardless of the location's crab pot fish areas.</summary>
	/// <param name="x">The X tile position to check.</param>
	/// <param name="y">The Y tile position to check.</param>
	/// <returns>Returns true to only catch ocean fish, or false to apply the normal crab pot behavior based on <c>Data/Locations</c> or <see cref="F:StardewValley.GameLocation.DefaultCrabPotFishTypes" />.</returns>
	public virtual bool catchOceanCrabPotFishFromThisSpot(int x, int y)
	{
		return false;
	}

	private void communityUpgradeAccept()
	{
		if (!Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
		{
			if (Game1.player.Money >= 500000 && Game1.player.Items.ContainsId("(O)388", 950))
			{
				Game1.player.Money -= 500000;
				Game1.player.Items.ReduceId("(O)388", 950);
				Game1.RequireCharacter("Robin").setNewDialogue("Data\\ExtraDialogue:Robin_PamUpgrade_Accepted");
				Game1.drawDialogue(Game1.getCharacterFromName("Robin"));
				Game1.RequireLocation<Town>("Town").daysUntilCommunityUpgrade.Value = 3;
				Game1.multiplayer.globalChatInfoMessage("CommunityUpgrade", Game1.player.Name);
			}
			else if (Game1.player.Money < 500000)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney3"));
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ScienceHouse_Carpenter_NotEnoughWood", 950));
			}
		}
		else if (!Game1.MasterPlayer.mailReceived.Contains("communityUpgradeShortcuts"))
		{
			if (Game1.player.Money >= 300000)
			{
				Game1.player.Money -= 300000;
				Game1.RequireCharacter("Robin").setNewDialogue("Data\\ExtraDialogue:Robin_HouseUpgrade_Accepted", add: true);
				Game1.drawDialogue(Game1.getCharacterFromName("Robin"));
				Game1.RequireLocation<Town>("Town").daysUntilCommunityUpgrade.Value = 3;
				Game1.multiplayer.globalChatInfoMessage("CommunityUpgrade", Game1.player.Name);
			}
			else if (Game1.player.Money < 300000)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney3"));
			}
		}
	}

	private void houseUpgradeAccept()
	{
		switch (Game1.player.houseUpgradeLevel.Value)
		{
		case 0:
			if (Game1.player.Money >= 10000 && Game1.player.Items.ContainsId("(O)388", 450))
			{
				Game1.player.daysUntilHouseUpgrade.Value = 3;
				Game1.player.Money -= 10000;
				Game1.player.Items.ReduceId("(O)388", 450);
				Game1.RequireCharacter("Robin").setNewDialogue("Data\\ExtraDialogue:Robin_HouseUpgrade_Accepted", add: true);
				Game1.drawDialogue(Game1.getCharacterFromName("Robin"));
				Game1.multiplayer.globalChatInfoMessage("HouseUpgrade", Game1.player.Name, Lexicon.getTokenizedPossessivePronoun(Game1.player.IsMale));
			}
			else if (Game1.player.Money < 10000)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney3"));
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ScienceHouse_Carpenter_NotEnoughWood", 450));
			}
			break;
		case 1:
			if (Game1.player.Money >= 65000 && Game1.player.Items.ContainsId("(O)709", 100))
			{
				Game1.player.daysUntilHouseUpgrade.Value = 3;
				Game1.player.Money -= 65000;
				Game1.player.Items.ReduceId("(O)709", 100);
				Game1.RequireCharacter("Robin").setNewDialogue("Data\\ExtraDialogue:Robin_HouseUpgrade_Accepted", add: true);
				Game1.drawDialogue(Game1.getCharacterFromName("Robin"));
				Game1.multiplayer.globalChatInfoMessage("HouseUpgrade", Game1.player.Name, Lexicon.getTokenizedPossessivePronoun(Game1.player.IsMale));
			}
			else if (Game1.player.Money < 65000)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney3"));
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ScienceHouse_Carpenter_NotEnoughHardwood", 100));
			}
			break;
		case 2:
			if (Game1.player.Money >= 100000)
			{
				Game1.player.daysUntilHouseUpgrade.Value = 3;
				Game1.player.Money -= 100000;
				Game1.RequireCharacter("Robin").setNewDialogue("Data\\ExtraDialogue:Robin_HouseUpgrade_Accepted", add: true);
				Game1.drawDialogue(Game1.getCharacterFromName("Robin"));
				Game1.multiplayer.globalChatInfoMessage("HouseUpgrade", Game1.player.Name, Lexicon.getTokenizedPossessivePronoun(Game1.player.IsMale));
			}
			else if (Game1.player.Money < 100000)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney3"));
			}
			break;
		}
	}

	public void destroyObject(Vector2 tileLocation, Farmer who)
	{
		destroyObject(tileLocation, hardDestroy: false, who);
	}

	public void destroyObject(Vector2 tileLocation, bool hardDestroy, Farmer who)
	{
		if (!objects.TryGetValue(tileLocation, out var value) || value.fragility.Value == 2 || value is Chest || !(value.QualifiedItemId != "(BC)165"))
		{
			return;
		}
		bool flag = false;
		if (value.Type == "Fish" || value.Type == "Cooking" || value.Type == "Crafting")
		{
			if (!(value is BreakableContainer))
			{
				TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 150f, 1, 3, new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f), flicker: true, value.bigCraftable.Value, value.flipped.Value);
				temporaryAnimatedSprite.CopyAppearanceFromItemId(value.QualifiedItemId, value.showNextIndex.Value ? 1 : 0);
				temporaryAnimatedSprite.scale = 4f;
				Game1.multiplayer.broadcastSprites(this, temporaryAnimatedSprite);
			}
			flag = true;
		}
		else if (value.CanBeGrabbed || hardDestroy)
		{
			flag = true;
		}
		if (value.IsBreakableStone())
		{
			flag = true;
			OnStoneDestroyed(value.ItemId, (int)tileLocation.X, (int)tileLocation.Y, who);
		}
		if (flag)
		{
			objects.Remove(tileLocation);
		}
	}

	public void addOneTimeGiftBox(Item i, int x, int y, int whichGiftBox = 2)
	{
		string text = Name + "_giftbox_" + x + "_" + y;
		if (!Game1.player.mailReceived.Contains(text))
		{
			Vector2 vector = new Vector2(x, y);
			if (!(overlayObjects.GetValueOrDefault(vector) is Chest chest) || !(chest.mailToAddOnItemDump == text))
			{
				cleanUpTileForMapOverride(new Point(x, y));
			}
			if (!overlayObjects.ContainsKey(vector))
			{
				Chest value = new Chest(new List<Item> { i }, vector, giftbox: true, whichGiftBox)
				{
					mailToAddOnItemDump = text
				};
				overlayObjects.Add(vector, value);
			}
		}
	}

	/// <summary>Get the unique ID of the location context in <c>Data/LocationContexts</c> which includes this location.</summary>
	public virtual string GetLocationContextId()
	{
		if (locationContextId == null)
		{
			if (map == null)
			{
				reloadMap();
			}
			if (map != null && map.Properties.TryGetValue("LocationContext", out var value))
			{
				if (Game1.locationContextData.ContainsKey(value))
				{
					locationContextId = value;
				}
				else
				{
					Game1.log.Error($"Location {NameOrUniqueName} has invalid LocationContext map property '{value}', ignoring value.");
				}
			}
			if (locationContextId == null)
			{
				locationContextId = GetParentLocation()?.GetLocationContextId() ?? "Default";
			}
		}
		return locationContextId;
	}

	/// <summary>Get the data for the location context in <c>Data/LocationContexts</c> which includes this location.</summary>
	public virtual LocationContextData GetLocationContext()
	{
		return LocationContexts.Require(GetLocationContextId());
	}

	/// <summary>Get whether this location is in the desert context.</summary>
	public bool InDesertContext()
	{
		return GetLocationContextId() == "Desert";
	}

	/// <summary>Get whether this location is in the Ginger Island context.</summary>
	public bool InIslandContext()
	{
		return GetLocationContextId() == "Island";
	}

	/// <summary>Get whether this location is in the default valley context.</summary>
	public bool InValleyContext()
	{
		return GetLocationContextId() == "Default";
	}

	public virtual bool sinkDebris(Debris debris, Vector2 chunkTile, Vector2 chunkPosition)
	{
		if (debris.isEssentialItem())
		{
			return false;
		}
		if (debris.item != null && debris.item.HasContextTag("book_item"))
		{
			return false;
		}
		if (debris.debrisType.Value == Debris.DebrisType.OBJECT && debris.chunkType.Value == 74)
		{
			return false;
		}
		if (debris.floppingFish.Value)
		{
			foreach (Building building in buildings)
			{
				if (building.isTileFishable(chunkTile))
				{
					return false;
				}
			}
		}
		if (!debris.isSinking.Value)
		{
			debris.isSinking.Value = true;
			Debris.DebrisType value = debris.debrisType.Value;
			if (value == Debris.DebrisType.OBJECT || value == Debris.DebrisType.RESOURCE)
			{
				if (Game1.random.NextBool())
				{
					TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), 150f, 8, 0, chunkPosition + new Vector2(-8f), flicker: false, Game1.random.NextBool(), 0.001f, 0.02f, Color.White, 1f, 0.003f, 0f, 0f)
					{
						delayBeforeAnimationStart = Game1.random.Next(300),
						startSound = "quickSlosh"
					});
				}
				return false;
			}
		}
		else
		{
			bool flag = false;
			foreach (Chunk chunk in debris.Chunks)
			{
				if (chunk.sinkTimer.Value <= 0)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		if (debris.debrisType.Value == Debris.DebrisType.CHUNKS)
		{
			localSound("quickSlosh");
			TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), 150f, 8, 0, chunkPosition + new Vector2(-8f), flicker: false, Game1.random.NextBool(), 0.001f, 0.02f, Color.White, 1f, 0.003f, 0f, 0f));
			return true;
		}
		TemporarySprites.Add(new TemporaryAnimatedSprite(28, 300f, 2, 1, chunkPosition + new Vector2(-8f), flicker: false, flipped: false));
		localSound("dropItemInWater");
		return true;
	}

	public virtual bool doesTileSinkDebris(int xTile, int yTile, Debris.DebrisType type)
	{
		if (isTileBuildingFishable(xTile, yTile))
		{
			return true;
		}
		if (type == Debris.DebrisType.CHUNKS)
		{
			if (isWaterTile(xTile, yTile))
			{
				return !hasTileAt(xTile, yTile, "Buildings");
			}
			return false;
		}
		if (isWaterTile(xTile, yTile) && !isTileUpperWaterBorder(getTileIndexAt(xTile, yTile, "Buildings", "untitled tile sheet")))
		{
			return doesTileHaveProperty(xTile, yTile, "Passable", "Buildings") == null;
		}
		return false;
	}

	private bool isTileUpperWaterBorder(int index)
	{
		switch (index)
		{
		case 183:
		case 184:
		case 185:
		case 211:
		case 1182:
		case 1183:
		case 1184:
		case 1210:
			return true;
		default:
			return false;
		}
	}

	public virtual bool doesEitherTileOrTileIndexPropertyEqual(int xTile, int yTile, string propertyName, string layerName, string propertyValue)
	{
		Layer layer = map?.GetLayer(layerName);
		if (layer != null)
		{
			Tile tile = layer.PickTile(new Location(xTile * 64, yTile * 64), Game1.viewport.Size);
			if (tile != null && tile.TileIndexProperties.TryGetValue(propertyName, out var value) && value == propertyValue)
			{
				return true;
			}
			if (tile != null && layer.PickTile(new Location(xTile * 64, yTile * 64), Game1.viewport.Size).Properties.TryGetValue(propertyName, out var value2) && value2 == propertyValue)
			{
				return true;
			}
		}
		return propertyValue == null;
	}

	/// <summary>Get whether the given tile prohibits spawned items.</summary>
	/// <param name="tile">The tile position to check.</param>
	/// <param name="type">The spawn type. This can be <c>Grass</c> (weeds, stones, and other debris), <c>Tree</c> (trees), or <c>All</c> (any other type).</param>
	public virtual bool IsNoSpawnTile(Vector2 tile, string type = "All", bool ignoreTileSheetProperties = false)
	{
		int xTile = (int)tile.X;
		int yTile = (int)tile.Y;
		string text = doesTileHaveProperty(xTile, yTile, "NoSpawn", "Back", ignoreTileSheetProperties);
		switch (text)
		{
		case "Grass":
		case "Tree":
			if (type == text)
			{
				return true;
			}
			break;
		default:
		{
			if (!bool.TryParse(text, out var result) || result)
			{
				return true;
			}
			break;
		}
		case null:
			break;
		}
		return getBuildingAt(tile) != null;
	}

	public virtual string doesTileHaveProperty(int xTile, int yTile, string propertyName, string layerName, bool ignoreTileSheetProperties = false)
	{
		Vector2 tile = new Vector2(xTile, yTile);
		bool flag = false;
		foreach (Building building in buildings)
		{
			if (!building.isMoving && building.occupiesTile(tile, applyTilePropertyRadius: true))
			{
				string property_value = null;
				if (building.doesTileHaveProperty(xTile, yTile, propertyName, layerName, ref property_value))
				{
					return property_value;
				}
				flag = flag || building.occupiesTile(tile);
			}
		}
		foreach (Furniture item in furniture)
		{
			if ((float)xTile >= item.tileLocation.X - (float)item.GetAdditionalTilePropertyRadius() && (float)xTile < item.tileLocation.X + (float)item.getTilesWide() + (float)item.GetAdditionalTilePropertyRadius() && (float)yTile >= item.tileLocation.Y - (float)item.GetAdditionalTilePropertyRadius() && (float)yTile < item.tileLocation.Y + (float)item.getTilesHigh() + (float)item.GetAdditionalTilePropertyRadius())
			{
				string property_value2 = null;
				if (item.DoesTileHaveProperty(xTile, yTile, propertyName, layerName, ref property_value2))
				{
					return property_value2;
				}
			}
		}
		if (!flag && map != null)
		{
			Tile tile2 = map.GetLayer(layerName)?.Tiles[xTile, yTile];
			if (tile2 != null)
			{
				if (tile2.Properties.TryGetValue(propertyName, out var value))
				{
					return value;
				}
				if (!ignoreTileSheetProperties && tile2.TileIndexProperties.TryGetValue(propertyName, out value))
				{
					return value;
				}
			}
		}
		return null;
	}

	public virtual string doesTileHavePropertyNoNull(int xTile, int yTile, string propertyName, string layerName)
	{
		return doesTileHaveProperty(xTile, yTile, propertyName, layerName) ?? "";
	}

	/// <summary>Get the space-delimited values defined by a map property.</summary>
	/// <param name="propertyName">The property name to read.</param>
	/// <param name="layerId">The ID for the layer whose tile to check.</param>
	/// <param name="tileX">The X tile position for the map tile to check.</param>
	/// <param name="tileY">The Y tile position for the map tile to check.</param>
	/// <returns>Returns the map property value, or an empty array if it's empty or unset.</returns>
	/// <remarks>See <see cref="M:StardewValley.GameLocation.doesTileHaveProperty(System.Int32,System.Int32,System.String,System.String,System.Boolean)" /> or <see cref="M:StardewValley.GameLocation.doesTileHavePropertyNoNull(System.Int32,System.Int32,System.String,System.String)" /> to get a tile property without splitting it.</remarks>
	public string[] GetTilePropertySplitBySpaces(string propertyName, string layerId, int tileX, int tileY)
	{
		string text = doesTileHaveProperty(tileX, tileY, propertyName, layerId);
		if (text == null)
		{
			return LegacyShims.EmptyArray<string>();
		}
		return ArgUtility.SplitBySpace(text);
	}

	/// <summary>Whether a tile coordinate matches a map water tile.</summary>
	/// <param name="xTile">The X tile position.</param>
	/// <param name="yTile">The Y tile position.</param>
	public bool isWaterTile(int xTile, int yTile)
	{
		return doesTileHaveProperty(xTile, yTile, "Water", "Back") != null;
	}

	public bool isOpenWater(int xTile, int yTile)
	{
		if (!isWaterTile(xTile, yTile))
		{
			return false;
		}
		int tileIndexAt = getTileIndexAt(xTile, yTile, "Buildings", "outdoors");
		if ((uint)(tileIndexAt - 628) <= 1u || tileIndexAt == 734 || tileIndexAt == 759)
		{
			return false;
		}
		return !objects.ContainsKey(new Vector2(xTile, yTile));
	}

	public bool isCropAtTile(int tileX, int tileY)
	{
		Vector2 key = new Vector2(tileX, tileY);
		if (terrainFeatures.TryGetValue(key, out var value) && value is HoeDirt hoeDirt)
		{
			return hoeDirt.crop != null;
		}
		return false;
	}

	/// <summary>Try to add an object to the location.</summary>
	/// <param name="obj">The object to place. This must be a new instance or <see cref="M:StardewValley.Item.getOne" /> copy; passing a stack that's stored in an inventory will link their state and cause unexpected behaviors.</param>
	/// <param name="dropLocation">The pixel position at which to place the item.</param>
	/// <param name="viewport">Unused.</param>
	/// <param name="initialPlacement">Whether to place the item regardless of the <see cref="F:StardewValley.Object.canBeSetDown" /> field.</param>
	/// <param name="who">The player placing the object, if applicable.</param>
	/// <returns>Returns whether the object was added to the location.</returns>
	public virtual bool dropObject(Object obj, Vector2 dropLocation, xTile.Dimensions.Rectangle viewport, bool initialPlacement, Farmer who = null)
	{
		Vector2 vector = new Vector2((int)dropLocation.X / 64, (int)dropLocation.Y / 64);
		obj.Location = this;
		obj.TileLocation = vector;
		obj.isSpawnedObject.Value = true;
		if (!isTileOnMap(vector) || map.RequireLayer("Back").PickTile(new Location((int)dropLocation.X, (int)dropLocation.Y), Game1.viewport.Size) == null || map.RequireLayer("Back").Tiles[(int)vector.X, (int)vector.Y].TileIndexProperties.ContainsKey("Unplaceable"))
		{
			return false;
		}
		if (obj.bigCraftable.Value)
		{
			if (!isFarm.Value)
			{
				return false;
			}
			if (!obj.setOutdoors.Value && isOutdoors.Value)
			{
				return false;
			}
			if (!obj.setIndoors.Value && !isOutdoors.Value)
			{
				return false;
			}
			if (obj.performDropDownAction(who))
			{
				return false;
			}
		}
		else if (obj.Type == "Crafting" && obj.performDropDownAction(who))
		{
			obj.CanBeSetDown = false;
		}
		bool flag = isTilePassable(new Location((int)vector.X, (int)vector.Y), viewport) && CanItemBePlacedHere(vector);
		if ((obj.CanBeSetDown || initialPlacement) && flag && !isTileHoeDirt(vector))
		{
			if (!objects.TryAdd(vector, obj))
			{
				return false;
			}
		}
		else if (isWaterTile((int)vector.X, (int)vector.Y))
		{
			Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(28, 300f, 2, 1, dropLocation, flicker: false, obj.flipped.Value));
			playSound("dropItemInWater");
		}
		else
		{
			if (obj.CanBeSetDown && !flag)
			{
				return false;
			}
			if (obj.ParentSheetIndex >= 0 && obj.Type != null)
			{
				if (obj.Type == "Fish" || obj.Type == "Cooking" || obj.Type == "Crafting")
				{
					TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 150f, 1, 3, dropLocation, flicker: true, obj.flipped.Value);
					temporaryAnimatedSprite.CopyAppearanceFromItemId(obj.QualifiedItemId);
					Game1.multiplayer.broadcastSprites(this, temporaryAnimatedSprite);
				}
				else
				{
					TemporaryAnimatedSprite temporaryAnimatedSprite2 = new TemporaryAnimatedSprite(0, 150f, 1, 3, dropLocation, flicker: true, obj.flipped.Value);
					temporaryAnimatedSprite2.CopyAppearanceFromItemId(obj.QualifiedItemId, 1);
					Game1.multiplayer.broadcastSprites(this, temporaryAnimatedSprite2);
				}
			}
		}
		return true;
	}

	private void rumbleAndFade(int milliseconds)
	{
		rumbleAndFadeEvent.Fire(milliseconds);
	}

	private void performRumbleAndFade(int milliseconds)
	{
		if (Game1.currentLocation == this)
		{
			Rumble.rumbleAndFade(1f, milliseconds);
		}
	}

	/// <summary>Sends a request to damage players within the current location.</summary>
	/// <param name="area">The location pixel area where players will take damage.</param>
	/// <param name="damage">The amount of damage the player should take.</param>
	/// <param name="isBomb">Whether the damage source was a bomb.</param>
	private void damagePlayers(Microsoft.Xna.Framework.Rectangle area, int damage, bool isBomb = false)
	{
		damagePlayersEvent.Fire(new DamagePlayersEventArg
		{
			Area = area,
			Damage = damage,
			IsBomb = isBomb
		});
	}

	private void performDamagePlayers(DamagePlayersEventArg arg)
	{
		if (Game1.player.currentLocation == this && (!arg.IsBomb || !Game1.player.hasBuff("dwarfStatue_3")))
		{
			int num = arg.Damage;
			if (Game1.player.stats.Get("Book_Bombs") != 0)
			{
				num = (int)((float)num * 0.75f);
			}
			if (Game1.player.GetBoundingBox().Intersects(arg.Area) && !Game1.player.onBridge.Value)
			{
				Game1.player.takeDamage(num, overrideParry: true, null);
			}
		}
	}

	public void explode(Vector2 tileLocation, int radius, Farmer who, bool damageFarmers = true, int damage_amount = -1, bool destroyObjects = true)
	{
		int num = 0;
		updateMap();
		Vector2 vector = new Vector2(Math.Min(map.Layers[0].LayerWidth - 1, Math.Max(0f, tileLocation.X - (float)radius)), Math.Min(map.Layers[0].LayerHeight - 1, Math.Max(0f, tileLocation.Y - (float)radius)));
		bool[,] circleOutlineGrid = Game1.getCircleOutlineGrid(radius);
		Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle((int)(tileLocation.X - (float)radius) * 64, (int)(tileLocation.Y - (float)radius) * 64, (radius * 2 + 1) * 64, (radius * 2 + 1) * 64);
		if (damage_amount > 0)
		{
			damageMonster(rectangle, damage_amount, damage_amount, isBomb: true, who);
		}
		else
		{
			damageMonster(rectangle, radius * 6, radius * 8, isBomb: true, who);
		}
		TemporaryAnimatedSpriteList temporaryAnimatedSpriteList = new TemporaryAnimatedSpriteList
		{
			new TemporaryAnimatedSprite(23, 9999f, 6, 1, new Vector2(vector.X * 64f, vector.Y * 64f), flicker: false, Game1.random.NextBool())
			{
				lightId = $"{NameOrUniqueName}_{"explode"}_{tileLocation.X}_{tileLocation.Y}_{Game1.random.Next()}",
				lightRadius = radius,
				lightcolor = Color.Black,
				alphaFade = 0.03f - (float)radius * 0.003f,
				Parent = this
			}
		};
		rumbleAndFade(300 + radius * 100);
		if (damageFarmers)
		{
			int damage = ((damage_amount > 0) ? damage_amount : (radius * 3));
			damagePlayers(rectangle, damage, isBomb: true);
		}
		for (int i = 0; i < radius * 2 + 1; i++)
		{
			for (int j = 0; j < radius * 2 + 1; j++)
			{
				if (i == 0 || j == 0 || i == radius * 2 || j == radius * 2)
				{
					num = (circleOutlineGrid[i, j] ? 1 : 0);
				}
				else if (circleOutlineGrid[i, j])
				{
					num += ((j <= radius) ? 1 : (-1));
					if (num <= 0)
					{
						if (destroyObjects)
						{
							if (objects.TryGetValue(vector, out var value) && value.onExplosion(who))
							{
								destroyObject(vector, who);
							}
							if (terrainFeatures.TryGetValue(vector, out var value2) && value2.performToolAction(null, radius / 2, vector))
							{
								terrainFeatures.Remove(vector);
							}
						}
						if (Game1.random.NextDouble() < 0.45)
						{
							if (Game1.random.NextBool())
							{
								temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite(362, Game1.random.Next(30, 90), 6, 1, new Vector2(vector.X * 64f, vector.Y * 64f), flicker: false, Game1.random.NextBool())
								{
									delayBeforeAnimationStart = Game1.random.Next(700)
								});
							}
							else
							{
								temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite(5, new Vector2(vector.X * 64f, vector.Y * 64f), Color.White, 8, flipped: false, 50f)
								{
									delayBeforeAnimationStart = Game1.random.Next(200),
									scale = (float)Game1.random.Next(5, 15) / 10f
								});
							}
						}
					}
				}
				if (num >= 1)
				{
					explosionAt(vector.X, vector.Y);
					if (destroyObjects)
					{
						if (objects.TryGetValue(vector, out var value3) && value3.onExplosion(who))
						{
							destroyObject(vector, who);
						}
						if (terrainFeatures.TryGetValue(vector, out var value4) && value4.performToolAction(null, radius / 2, vector))
						{
							terrainFeatures.Remove(vector);
						}
					}
					if (Game1.random.NextDouble() < 0.45)
					{
						if (Game1.random.NextBool())
						{
							temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite(362, Game1.random.Next(30, 90), 6, 1, new Vector2(vector.X * 64f, vector.Y * 64f), flicker: false, Game1.random.NextBool())
							{
								delayBeforeAnimationStart = Game1.random.Next(700)
							});
						}
						else
						{
							temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite(5, new Vector2(vector.X * 64f, vector.Y * 64f), Color.White, 8, flipped: false, 50f)
							{
								delayBeforeAnimationStart = Game1.random.Next(200),
								scale = (float)Game1.random.Next(5, 15) / 10f
							});
						}
					}
					temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite(6, new Vector2(vector.X * 64f, vector.Y * 64f), Color.White, 8, Game1.random.NextBool(), Vector2.Distance(vector, tileLocation) * 20f));
				}
				vector.Y += 1f;
				vector.Y = Math.Min(map.Layers[0].LayerHeight - 1, Math.Max(0f, vector.Y));
			}
			vector.X += 1f;
			vector.Y = Math.Min(map.Layers[0].LayerWidth - 1, Math.Max(0f, vector.X));
			vector.Y = tileLocation.Y - (float)radius;
			vector.Y = Math.Min(map.Layers[0].LayerHeight - 1, Math.Max(0f, vector.Y));
		}
		Game1.multiplayer.broadcastSprites(this, temporaryAnimatedSpriteList);
		radius /= 2;
		circleOutlineGrid = Game1.getCircleOutlineGrid(radius);
		vector = new Vector2((int)(tileLocation.X - (float)radius), (int)(tileLocation.Y - (float)radius));
		num = 0;
		for (int k = 0; k < radius * 2 + 1; k++)
		{
			for (int l = 0; l < radius * 2 + 1; l++)
			{
				if (k == 0 || l == 0 || k == radius * 2 || l == radius * 2)
				{
					num = (circleOutlineGrid[k, l] ? 1 : 0);
				}
				else if (circleOutlineGrid[k, l])
				{
					num += ((l <= radius) ? 1 : (-1));
					if (num <= 0 && !objects.ContainsKey(vector) && Game1.random.NextDouble() < 0.9 && !isTileHoeDirt(vector) && makeHoeDirt(vector))
					{
						checkForBuriedItem((int)vector.X, (int)vector.Y, explosion: true, detectOnly: false, who);
					}
				}
				if (num >= 1 && !objects.ContainsKey(vector) && Game1.random.NextDouble() < 0.9 && !isTileHoeDirt(vector) && makeHoeDirt(vector))
				{
					checkForBuriedItem((int)vector.X, (int)vector.Y, explosion: true, detectOnly: false, who);
				}
				vector.Y += 1f;
				vector.Y = Math.Min(map.Layers[0].LayerHeight - 1, Math.Max(0f, vector.Y));
			}
			vector.X += 1f;
			vector.Y = Math.Min(map.Layers[0].LayerWidth - 1, Math.Max(0f, vector.X));
			vector.Y = tileLocation.Y - (float)radius;
			vector.Y = Math.Min(map.Layers[0].LayerHeight - 1, Math.Max(0f, vector.Y));
		}
	}

	public virtual void explosionAt(float x, float y)
	{
	}

	public void removeTemporarySpritesWithID(int id)
	{
		removeTemporarySpritesWithIDEvent.Fire(id);
	}

	public void removeTemporarySpritesWithIDLocal(int id)
	{
		temporarySprites.RemoveWhere(delegate(TemporaryAnimatedSprite sprite)
		{
			if (sprite.id == id)
			{
				if (sprite.hasLit)
				{
					Utility.removeLightSource(sprite.lightId);
				}
				return true;
			}
			return false;
		});
	}

	/// <summary>Till a tile into a <see cref="T:StardewValley.TerrainFeatures.HoeDirt" /> if it's a valid diggable position, and there isn't already a tilled dirt there.</summary>
	/// <param name="tileLocation">The tile position to till.</param>
	/// <param name="ignoreChecks">Whether to till the tile even if it's occupied or non-diggable.</param>
	/// <returns>Returns whether a <see cref="T:StardewValley.TerrainFeatures.HoeDirt" /> instance was successfully added.</returns>
	public bool makeHoeDirt(Vector2 tileLocation, bool ignoreChecks = false)
	{
		if (ignoreChecks || (doesTileHaveProperty((int)tileLocation.X, (int)tileLocation.Y, "Diggable", "Back") != null && !IsTileBlockedBy(tileLocation, ~(CollisionMask.Characters | CollisionMask.Farmers))))
		{
			MineShaft obj = this as MineShaft;
			if (obj == null || obj.getMineArea() != 77377)
			{
				return terrainFeatures.TryAdd(tileLocation, new HoeDirt((IsRainingHere() && isOutdoors.Value) ? 1 : 0, this));
			}
		}
		return false;
	}

	public int numberOfObjectsOfType(string itemId, bool bigCraftable)
	{
		int num = 0;
		string typeId = (bigCraftable ? "(BC)" : "(O)");
		foreach (Object value in Objects.Values)
		{
			if (value.HasTypeId(typeId) && value.ItemId == itemId)
			{
				num++;
			}
		}
		return num;
	}

	public virtual void timeUpdate(int timeElapsed)
	{
		if (Game1.IsMasterGame)
		{
			foreach (FarmAnimal value in animals.Values)
			{
				value.updatePerTenMinutes(Game1.timeOfDay, this);
			}
		}
		foreach (Building building in buildings)
		{
			if (building.daysOfConstructionLeft.Value > 0)
			{
				continue;
			}
			building.performTenMinuteAction(timeElapsed);
			if (building.GetIndoorsType() != IndoorsType.Instanced)
			{
				continue;
			}
			GameLocation indoors = building.GetIndoors();
			if (indoors == null)
			{
				continue;
			}
			foreach (FarmAnimal value2 in indoors.animals.Values)
			{
				value2.updatePerTenMinutes(Game1.timeOfDay, indoors);
			}
			if (timeElapsed >= 10)
			{
				indoors.performTenMinuteUpdate(Game1.timeOfDay);
				if (timeElapsed > 10)
				{
					indoors.passTimeForObjects(timeElapsed - 10);
				}
			}
		}
	}

	/// <summary>Update all object when the time of day changes.</summary>
	/// <param name="timeElapsed">The number of minutes that passed.</param>
	public void passTimeForObjects(int timeElapsed)
	{
		objects.Lock();
		foreach (KeyValuePair<Vector2, Object> pair in objects.Pairs)
		{
			if (pair.Value.minutesElapsed(timeElapsed))
			{
				Vector2 key = pair.Key;
				objects.Remove(key);
			}
		}
		objects.Unlock();
	}

	public virtual void performTenMinuteUpdate(int timeOfDay)
	{
		for (int i = 0; i < furniture.Count; i++)
		{
			furniture[i].minutesElapsed(10);
		}
		for (int j = 0; j < characters.Count; j++)
		{
			NPC nPC = characters[j];
			if (!nPC.IsInvisible)
			{
				nPC.checkSchedule(timeOfDay);
				nPC.performTenMinuteUpdate(timeOfDay, this);
			}
		}
		passTimeForObjects(10);
		if (isOutdoors.Value)
		{
			Random random = Utility.CreateDaySaveRandom(timeOfDay, Map.Layers[0].LayerWidth);
			if (Equals(Game1.currentLocation))
			{
				tryToAddCritters(onlyIfOnScreen: true);
			}
			if (Game1.IsMasterGame)
			{
				int num = Utility.CalculateMinutesBetweenTimes(fishSplashPointTime, Game1.timeOfDay);
				bool flag = fishFrenzyFish.Value != null && !fishFrenzyFish.Value.Equals("");
				if (fishSplashPoint.Value.Equals(Point.Zero) && random.NextBool() && (!(this is Farm) || Game1.whichFarm == 1))
				{
					for (int k = 0; k < 2; k++)
					{
						Point point = new Point(random.Next(0, map.RequireLayer("Back").LayerWidth), random.Next(0, map.RequireLayer("Back").LayerHeight));
						if (!isOpenWater(point.X, point.Y) || doesTileHaveProperty(point.X, point.Y, "NoFishing", "Back") != null)
						{
							continue;
						}
						int num2 = FishingRod.distanceToLand(point.X, point.Y, this);
						if (num2 <= 1 || num2 >= 5)
						{
							continue;
						}
						if (Game1.player.currentLocation.Equals(this))
						{
							playSound("waterSlosh");
						}
						if (random.NextDouble() < ((this is Beach) ? 0.008 : 0.01) && Game1.Date.TotalDays > 3 && (this is Town || this is Mountain || this is Forest || this is Beach) && Game1.timeOfDay < 2300 && (Game1.player.fishCaught.Count() > 2 || Game1.Date.TotalDays > 14) && !Utility.isFestivalDay())
						{
							Item fish = getFish(random.Next(500), "", num2, Game1.player, 0.0, Utility.PointToVector2(point));
							if (fish.Category == -4 && !fish.HasContextTag("fish_legendary"))
							{
								fishFrenzyFish.Value = fish.QualifiedItemId;
								string text = ((this is Mountain) ? "mountain" : ((this is Forest) ? "forest" : ((!(this is Town)) ? "beach" : "town")));
								string text2 = TokenStringBuilder.ItemNameFor(fish);
								string text3 = TokenStringBuilder.CapitalizeFirstLetter(TokenStringBuilder.ArticleFor(text2));
								Game1.multiplayer.broadcastGlobalMessage("Strings\\1_6_Strings:FishFrenzy_" + text, false, null, text2, text3);
							}
						}
						fishSplashPointTime = Game1.timeOfDay;
						fishSplashPoint.Value = point;
						break;
					}
				}
				else if (!fishSplashPoint.Value.Equals(Point.Zero) && random.NextDouble() < 0.1 + (double)((float)num / 1800f) && num > (flag ? 120 : 60))
				{
					fishSplashPointTime = 0;
					fishFrenzyFish.Value = "";
					fishSplashPoint.Value = Point.Zero;
				}
				performOrePanTenMinuteUpdate(random);
			}
		}
		if (Game1.dayOfMonth % 7 == 0 && Game1.timeOfDay >= 1200 && Game1.timeOfDay <= 1500 && name.Equals("Saloon") && NetWorldState.checkAnywhereForWorldStateID("saloonSportsRoom"))
		{
			if (Game1.timeOfDay == 1500)
			{
				removeTemporarySpritesWithID(2400);
			}
			else
			{
				bool flag2 = Game1.random.NextDouble() < 0.25;
				bool flag3 = Game1.random.NextDouble() < 0.25;
				List<NPC> list = new List<NPC>();
				foreach (NPC character in characters)
				{
					if (character.TilePoint.Y < 12 && character.TilePoint.X > 26 && Game1.random.NextDouble() < ((flag2 || flag3) ? 0.66 : 0.25))
					{
						list.Add(character);
					}
				}
				foreach (NPC item in list)
				{
					item.showTextAboveHead(Game1.content.LoadString("Strings\\Characters:Saloon_" + (flag2 ? "goodEvent" : (flag3 ? "badEvent" : "neutralEvent")) + "_" + Game1.random.Next(5)));
					if (flag2 && Game1.random.NextDouble() < 0.55)
					{
						item.jump();
					}
				}
			}
		}
		if (Game1.currentLocation.Equals(this) && name.Equals("BugLand") && Game1.random.NextDouble() <= 0.2)
		{
			characters.Add(new Fly(getRandomTile() * 64f, hard: true));
		}
	}

	public virtual bool performOrePanTenMinuteUpdate(Random r)
	{
		if (Game1.MasterPlayer.mailReceived.Contains("ccFishTank") && !(this is Beach) && orePanPoint.Value.Equals(Point.Zero) && r.NextBool())
		{
			for (int i = 0; i < 8; i++)
			{
				Point point = new Point(r.Next(0, Map.RequireLayer("Back").LayerWidth), r.Next(0, Map.RequireLayer("Back").LayerHeight));
				if (isOpenWater(point.X, point.Y) && FishingRod.distanceToLand(point.X, point.Y, this, landMustBeAdjacentToWalkableTile: true) <= 1 && !hasTileAt(point, "Buildings"))
				{
					if (Game1.player.currentLocation.Equals(this))
					{
						playSound("slosh");
					}
					orePanPoint.Value = point;
					return true;
				}
			}
		}
		else if (!orePanPoint.Value.Equals(Point.Zero) && r.NextDouble() < 0.1)
		{
			orePanPoint.Value = Point.Zero;
		}
		return false;
	}

	/// <summary>Get the fish types that can be caught by crab pots on a given tile.</summary>
	/// <param name="tile">The tile position containing the crab pot.</param>
	public virtual IList<string> GetCrabPotFishForTile(Vector2 tile)
	{
		if (catchOceanCrabPotFishFromThisSpot((int)tile.X, (int)tile.Y))
		{
			return OceanCrabPotFishTypes;
		}
		if (TryGetFishAreaForTile(tile, out var _, out var data))
		{
			List<string> crabPotFishTypes = data.CrabPotFishTypes;
			if (crabPotFishTypes != null && crabPotFishTypes.Count > 0)
			{
				return data.CrabPotFishTypes;
			}
		}
		return DefaultCrabPotFishTypes;
	}

	/// <summary>Get the fish area that applies to the given tile, if any.</summary>
	/// <param name="tile">The tile to check.</param>
	/// <param name="id">The fish area ID which applies, if any.</param>
	/// <param name="data">The fish area data which applies, if any.</param>
	public virtual bool TryGetFishAreaForTile(Vector2 tile, out string id, out FishAreaData data)
	{
		LocationData data2 = GetData();
		if (data2?.FishAreas != null)
		{
			string text = null;
			FishAreaData fishAreaData = null;
			foreach (KeyValuePair<string, FishAreaData> fishArea in data2.FishAreas)
			{
				FishAreaData value = fishArea.Value;
				bool? flag = value.Position?.Contains((int)tile.X, (int)tile.Y);
				if (flag.HasValue)
				{
					if (flag == true)
					{
						id = fishArea.Key;
						data = value;
						return true;
					}
				}
				else if (text == null)
				{
					text = fishArea.Key;
					fishAreaData = fishArea.Value;
				}
			}
			if (text != null)
			{
				id = text;
				data = fishAreaData;
				return true;
			}
		}
		id = null;
		data = null;
		return false;
	}

	/// <summary>Get the display name for a fishing area, if it has one.</summary>
	/// <param name="id">The fishing area ID, as returned by <see cref="M:StardewValley.GameLocation.TryGetFishAreaForTile(Microsoft.Xna.Framework.Vector2,System.String@,StardewValley.GameData.Locations.FishAreaData@)" />.</param>
	public virtual string GetFishingAreaDisplayName(string id)
	{
		LocationData data = GetData();
		if (data?.FishAreas == null || !data.FishAreas.TryGetValue(id, out var value) || value.DisplayName == null)
		{
			return null;
		}
		return TokenParser.ParseText(value.DisplayName);
	}

	/// <summary>Get a random fish that can be caught in this location.</summary>
	/// <param name="millisecondsAfterNibble">The number of milliseconds after the fish starting biting before the player reacted and pressed the tool button.</param>
	/// <param name="bait">The qualified item ID for the bait attached to the fishing rod, if any.</param>
	/// <param name="waterDepth">The tile distance from the nearest shore.</param>
	/// <param name="who">The player who's fishing.</param>
	/// <param name="baitPotency">Unused.</param>
	/// <param name="bobberTile">The tile position where the fishing rod's bobber is floating.</param>
	/// <param name="locationName">The name of the location whose fish to get, or <c>null</c> for the current location.</param>
	public virtual Item getFish(float millisecondsAfterNibble, string bait, int waterDepth, Farmer who, double baitPotency, Vector2 bobberTile, string locationName = null)
	{
		if (locationName != null && locationName != Name && (!(locationName == "UndergroundMine") || !(this is MineShaft)))
		{
			GameLocation locationFromName = Game1.getLocationFromName(locationName);
			if (locationFromName != null && locationFromName != this)
			{
				return locationFromName.getFish(millisecondsAfterNibble, bait, waterDepth, who, baitPotency, bobberTile);
			}
		}
		if (bobberTile != Vector2.Zero && who.currentLocation?.NameOrUniqueName == NameOrUniqueName)
		{
			foreach (Building building in buildings)
			{
				if (building is FishPond fishPond && fishPond.isTileFishable(bobberTile))
				{
					return fishPond.CatchFish();
				}
			}
		}
		if (fishFrenzyFish.Value != null && !fishFrenzyFish.Value.Equals("") && Vector2.Distance(bobberTile, Utility.PointToVector2(fishSplashPoint.Value)) <= 2f)
		{
			return ItemRegistry.Create(fishFrenzyFish.Value);
		}
		bool isTutorialCatch = who.fishCaught.Length == 0;
		return GetFishFromLocationData(Name, bobberTile, waterDepth, who, isTutorialCatch, isInherited: false, this) ?? ItemRegistry.Create("(O)168");
	}

	/// <summary>Get a random fish that can be caught for a given location based on its <c>Data\Locations</c> entry. This doesn't include global default fish and special cases; most code should call <see cref="M:StardewValley.GameLocation.getFish(System.Single,System.String,System.Int32,StardewValley.Farmer,System.Double,Microsoft.Xna.Framework.Vector2,System.String)" /> instead.</summary>
	/// <param name="locationName">The name of the location whose fish to get.</param>
	/// <param name="bobberTile">The tile position where the fishing rod's bobber is floating.</param>
	/// <param name="waterDepth">The tile distance from the nearest shore.</param>
	/// <param name="player">The player who's fishing.</param>
	/// <param name="isTutorialCatch">Whether this is the player's first catch, so it should be an easy fish for the tutorial.</param>
	/// <param name="isInherited">Whether we're loading fish indirectly (e.g. via the <c>LOCATION_FISH</c> item query), rather than for the actual location.</param>
	/// <param name="location">The location instance from which to get context data. If this is <c>null</c>, it'll be loaded based on the <paramref name="locationName" />; if that fails, generic context info (e.g. current location's weather) will be used instead.</param>
	/// <returns>Returns the fish to catch, or <c>null</c> if no match was found.</returns>
	public static Item GetFishFromLocationData(string locationName, Vector2 bobberTile, int waterDepth, Farmer player, bool isTutorialCatch, bool isInherited, GameLocation location = null)
	{
		return GetFishFromLocationData(locationName, bobberTile, waterDepth, player, isTutorialCatch, isInherited, location, null);
	}

	/// <summary>Get a random fish that can be caught for a given location based on its <c>Data\Locations</c> entry. This doesn't include global default fish and special cases; most code should call <see cref="M:StardewValley.GameLocation.getFish(System.Single,System.String,System.Int32,StardewValley.Farmer,System.Double,Microsoft.Xna.Framework.Vector2,System.String)" /> instead.</summary>
	/// <param name="locationName">The name of the location whose fish to get.</param>
	/// <param name="bobberTile">The tile position where the fishing rod's bobber is floating.</param>
	/// <param name="waterDepth">The tile distance from the nearest shore.</param>
	/// <param name="player">The player who's fishing.</param>
	/// <param name="isTutorialCatch">Whether this is the player's first catch, so it should be an easy fish for the tutorial.</param>
	/// <param name="isInherited">Whether we're loading fish indirectly (e.g. via the <c>LOCATION_FISH</c> item query), rather than for the actual location.</param>
	/// <param name="location">The location instance from which to get context data. If this is <c>null</c>, it'll be loaded based on the <paramref name="locationName" />; if that fails, generic context info (e.g. current location's weather) will be used instead.</param>
	/// <param name="itemQueryContext">The context for the item query which led to this call, if applicable. This is used internally to prevent circular loops.</param>
	/// <returns>Returns the fish to catch, or <c>null</c> if no match was found.</returns>
	internal static Item GetFishFromLocationData(string locationName, Vector2 bobberTile, int waterDepth, Farmer player, bool isTutorialCatch, bool isInherited, GameLocation location, ItemQueryContext itemQueryContext)
	{
		if (location == null)
		{
			location = Game1.getLocationFromName(locationName);
		}
		LocationData locationData = ((location != null) ? location.GetData() : GetData(locationName));
		Dictionary<string, string> allFishData = DataLoader.Fish(Game1.content);
		Season seasonForLocation = Game1.GetSeasonForLocation(location);
		if (location == null || !location.TryGetFishAreaForTile(bobberTile, out var id, out var _))
		{
			id = null;
		}
		bool flag = false;
		bool hasCuriosityLure = false;
		string text = null;
		bool flag2 = false;
		if (player?.CurrentTool is FishingRod { isFishing: not false } fishingRod)
		{
			flag = fishingRod.HasMagicBait();
			hasCuriosityLure = fishingRod.HasCuriosityLure();
			Object bait = fishingRod.GetBait();
			if (bait != null)
			{
				if (bait.QualifiedItemId == "(O)SpecificBait" && bait.preservedParentSheetIndex.Value != null)
				{
					text = "(O)" + bait.preservedParentSheetIndex.Value;
				}
				if (bait.QualifiedItemId != "(O)685")
				{
					flag2 = true;
				}
			}
		}
		Point tilePoint = player.TilePoint;
		if (itemQueryContext == null)
		{
			itemQueryContext = new ItemQueryContext(location, null, Game1.random, "location '" + locationName + "' > fish data");
		}
		IEnumerable<SpawnFishData> enumerable = Game1.locationData["Default"].Fish;
		if (locationData != null && locationData.Fish?.Count > 0)
		{
			enumerable = enumerable.Concat(locationData.Fish);
		}
		enumerable = from p in enumerable
			orderby p.Precedence, Game1.random.Next()
			select p;
		int num = 0;
		HashSet<string> ignoreQueryKeys = (flag ? GameStateQuery.MagicBaitIgnoreQueryKeys : null);
		Item item = null;
		for (int num2 = 0; num2 < 2; num2++)
		{
			foreach (SpawnFishData spawn in enumerable)
			{
				if ((isInherited && !spawn.CanBeInherited) || (spawn.FishAreaId != null && id != spawn.FishAreaId) || (spawn.Season.HasValue && !flag && spawn.Season != seasonForLocation))
				{
					continue;
				}
				Microsoft.Xna.Framework.Rectangle? playerPosition = spawn.PlayerPosition;
				if (playerPosition.HasValue && !playerPosition.GetValueOrDefault().Contains(tilePoint.X, tilePoint.Y))
				{
					continue;
				}
				playerPosition = spawn.BobberPosition;
				if ((playerPosition.HasValue && !playerPosition.GetValueOrDefault().Contains((int)bobberTile.X, (int)bobberTile.Y)) || player.FishingLevel < spawn.MinFishingLevel || waterDepth < spawn.MinDistanceFromShore || (spawn.MaxDistanceFromShore > -1 && waterDepth > spawn.MaxDistanceFromShore) || (spawn.RequireMagicBait && !flag))
				{
					continue;
				}
				float chance = spawn.GetChance(hasCuriosityLure, player.DailyLuck, player.LuckLevel, (float value2, IList<QuantityModifier> modifiers, QuantityModifier.QuantityModifierMode mode) => Utility.ApplyQuantityModifiers(value2, modifiers, mode, location), spawn.ItemId == text);
				if (spawn.UseFishCaughtSeededRandom)
				{
					if (!Utility.CreateRandom(Game1.uniqueIDForThisGame, player.stats.Get("PreciseFishCaught") * 859).NextBool(chance))
					{
						continue;
					}
				}
				else if (!Game1.random.NextBool(chance))
				{
					continue;
				}
				if (spawn.Condition != null && !GameStateQuery.CheckConditions(spawn.Condition, location, null, null, null, null, ignoreQueryKeys))
				{
					continue;
				}
				Item item2 = ItemQueryResolver.TryResolveRandomItem(spawn, itemQueryContext, avoidRepeat: false, null, (string query) => query.Replace("BOBBER_X", ((int)bobberTile.X).ToString()).Replace("BOBBER_Y", ((int)bobberTile.Y).ToString()).Replace("WATER_DEPTH", waterDepth.ToString()), null, delegate(string query, string error)
				{
					Game1.log.Error($"Location '{location.NameOrUniqueName}' failed parsing item query '{query}' for fish '{spawn.Id}': {error}");
				});
				if (item2 == null)
				{
					continue;
				}
				if (!string.IsNullOrWhiteSpace(spawn.SetFlagOnCatch))
				{
					item2.SetFlagOnPickup = spawn.SetFlagOnCatch;
				}
				if (spawn.IsBossFish)
				{
					item2.SetTempData("IsBossFish", value: true);
				}
				Item item3 = item2;
				if ((spawn.CatchLimit <= -1 || !player.fishCaught.TryGetValue(item3.QualifiedItemId, out var value) || value[0] < spawn.CatchLimit) && CheckGenericFishRequirements(item3, allFishData, location, player, spawn, waterDepth, flag, hasCuriosityLure, spawn.ItemId == text, isTutorialCatch))
				{
					if (text == null || !(item3.QualifiedItemId != text) || num >= 2)
					{
						return item3;
					}
					if (item == null)
					{
						item = item3;
					}
					num++;
				}
			}
			if (!flag2)
			{
				num2++;
			}
		}
		if (item != null)
		{
			return item;
		}
		if (!isTutorialCatch)
		{
			return null;
		}
		return ItemRegistry.Create("(O)145");
	}

	/// <summary>Get whether a fish can be spawned based on its requirements in Data/Fish, if applicable.</summary>
	/// <param name="fish">The fish being checked.</param>
	/// <param name="allFishData">The Data/Fish data to check.</param>
	/// <param name="location">The location for which fish are being caught.</param>
	/// <param name="player">The player catching fish.</param>
	/// <param name="spawn">The fish spawn rule for which a fish is being checked.</param>
	/// <param name="waterDepth">The current water depth for the fishing bobber.</param>
	/// <param name="usingMagicBait">Whether the player has the magic bait equipped.</param>
	/// <param name="hasCuriosityLure">Whether the player has the curiosity lure equipped.</param>
	/// <param name="usingTargetBait">Whether the player has the target bait equipped.</param>
	/// <param name="isTutorialCatch">Whether this is the player's first catch, so it should be an easy fish for the tutorial.</param>
	internal static bool CheckGenericFishRequirements(Item fish, Dictionary<string, string> allFishData, GameLocation location, Farmer player, SpawnFishData spawn, int waterDepth, bool usingMagicBait, bool hasCuriosityLure, bool usingTargetBait, bool isTutorialCatch)
	{
		if (!fish.HasTypeObject() || !allFishData.TryGetValue(fish.ItemId, out var value))
		{
			return !isTutorialCatch;
		}
		string[] array = value.Split('/');
		if (ArgUtility.Get(array, 1) == "trap")
		{
			return !isTutorialCatch;
		}
		bool flag = player?.CurrentTool?.QualifiedItemId == "(T)TrainingRod";
		if (flag)
		{
			bool? canUseTrainingRod = spawn.CanUseTrainingRod;
			if (canUseTrainingRod.HasValue)
			{
				if (canUseTrainingRod != true)
				{
					return false;
				}
			}
			else
			{
				if (!ArgUtility.TryGetInt(array, 1, out var value2, out var error, "int difficulty"))
				{
					return LogFormatError(error);
				}
				if (value2 >= 50)
				{
					return false;
				}
			}
		}
		if (isTutorialCatch)
		{
			if (!ArgUtility.TryGetOptionalBool(array, 13, out var value3, out var error2, defaultValue: false, "bool isTutorialFish"))
			{
				return LogFormatError(error2);
			}
			if (!value3)
			{
				return false;
			}
		}
		if (!spawn.IgnoreFishDataRequirements)
		{
			if (!usingMagicBait)
			{
				if (!ArgUtility.TryGet(array, 5, out var value4, out var error3, allowBlank: true, "string rawTimeSpans"))
				{
					return LogFormatError(error3);
				}
				string[] array2 = ArgUtility.SplitBySpace(value4);
				bool flag2 = false;
				for (int i = 0; i < array2.Length; i += 2)
				{
					if (!ArgUtility.TryGetInt(array2, i, out var value5, out error3, "int startTime") || !ArgUtility.TryGetInt(array2, i + 1, out var value6, out error3, "int endTime"))
					{
						return LogFormatError("invalid time spans '" + value4 + "': " + error3);
					}
					if (Game1.timeOfDay >= value5 && Game1.timeOfDay < value6)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					return false;
				}
			}
			if (!usingMagicBait)
			{
				if (!ArgUtility.TryGet(array, 7, out var value7, out var error4, allowBlank: true, "string weather"))
				{
					return LogFormatError(error4);
				}
				if (!(value7 == "rainy"))
				{
					if (value7 == "sunny" && location.IsRainingHere())
					{
						return false;
					}
				}
				else if (!location.IsRainingHere())
				{
					return false;
				}
			}
			if (!ArgUtility.TryGetInt(array, 12, out var value8, out var error5, "int minFishingLevel"))
			{
				return LogFormatError(error5);
			}
			if (player.FishingLevel < value8)
			{
				return false;
			}
			if (!ArgUtility.TryGetInt(array, 9, out var value9, out var error6, "int maxDepth") || !ArgUtility.TryGetFloat(array, 10, out var value10, out error6, "float chance") || !ArgUtility.TryGetFloat(array, 11, out var value11, out error6, "float depthMultiplier"))
			{
				return LogFormatError(error6);
			}
			float num = value11 * value10;
			value10 -= (float)Math.Max(0, value9 - waterDepth) * num;
			value10 += (float)player.FishingLevel / 50f;
			if (flag)
			{
				value10 *= 1.1f;
			}
			value10 = Math.Min(value10, 0.9f);
			if ((double)value10 < 0.25 && hasCuriosityLure)
			{
				if (spawn.CuriosityLureBuff > -1f)
				{
					value10 += spawn.CuriosityLureBuff;
				}
				else
				{
					float num2 = 0.25f;
					float num3 = 0.08f;
					value10 = (num2 - num3) / num2 * value10 + (num2 - num3) / 2f;
				}
			}
			if (usingTargetBait)
			{
				value10 *= 1.66f;
			}
			if (spawn.ApplyDailyLuck)
			{
				value10 += (float)player.DailyLuck;
			}
			List<QuantityModifier> chanceModifiers = spawn.ChanceModifiers;
			if (chanceModifiers != null && chanceModifiers.Count > 0)
			{
				value10 = Utility.ApplyQuantityModifiers(value10, spawn.ChanceModifiers, spawn.ChanceModifierMode, location);
			}
			if (!Game1.random.NextBool(value10))
			{
				return false;
			}
		}
		return true;
		bool LogFormatError(string text)
		{
			Game1.log.Warn("Skipped fish '" + fish.ItemId + "' due to invalid requirements in Data/Fish: " + text);
			return false;
		}
	}

	public virtual bool isActionableTile(int xTile, int yTile, Farmer who)
	{
		foreach (Building building in buildings)
		{
			if (building.isActionableTile(xTile, yTile, who))
			{
				return true;
			}
		}
		bool flag = false;
		string[] array = ArgUtility.SplitBySpace(doesTileHaveProperty(xTile, yTile, "Action", "Buildings"));
		if (!ShouldIgnoreAction(array, who, new Location(xTile, yTile)))
		{
			switch (array[0])
			{
			case "Dialogue":
			case "Message":
			case "MessageOnce":
			case "NPCMessage":
				flag = true;
				Game1.isInspectionAtCurrentCursorTile = true;
				break;
			case "MessageSpeech":
				flag = true;
				Game1.isSpeechAtCurrentCursorTile = true;
				break;
			default:
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			if (objects.TryGetValue(new Vector2(xTile, yTile), out var value) && value.isActionable(who))
			{
				flag = true;
			}
			if (!Game1.isFestival() && terrainFeatures.TryGetValue(new Vector2(xTile, yTile), out var value2) && value2.isActionable())
			{
				flag = true;
			}
		}
		if (flag && !Utility.tileWithinRadiusOfPlayer(xTile, yTile, 1, who))
		{
			Game1.mouseCursorTransparency = 0.5f;
		}
		return flag;
	}

	public Item tryGetRandomArtifactFromThisLocation(Farmer who, Random r, double chanceMultipler = 1.0)
	{
		LocationData data = GetData();
		ItemQueryContext context = new ItemQueryContext(this, who, r, "location '" + NameOrUniqueName + "' > artifact spots");
		IEnumerable<ArtifactSpotDropData> enumerable = Game1.locationData["Default"].ArtifactSpots;
		if (data != null && data.ArtifactSpots?.Count > 0)
		{
			enumerable = enumerable.Concat(data.ArtifactSpots);
		}
		enumerable = enumerable.OrderBy((ArtifactSpotDropData p) => p.Precedence);
		foreach (ArtifactSpotDropData drop in enumerable)
		{
			if (r.NextBool(drop.Chance * chanceMultipler) && (drop.Condition == null || GameStateQuery.CheckConditions(drop.Condition, this, who, null, null, r)))
			{
				Item item = ItemQueryResolver.TryResolveRandomItem(drop, context, avoidRepeat: false, null, null, null, delegate(string query, string error)
				{
					Game1.log.Error($"Location '{NameOrUniqueName}' failed parsing item query '{query}' for artifact spot '{drop.Id}': {error}");
				});
				if (item != null)
				{
					return item;
				}
			}
		}
		return null;
	}

	public virtual void digUpArtifactSpot(int xLocation, int yLocation, Farmer who)
	{
		Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation, Game1.netWorldState.Value.TreasureTotemsUsed * 777);
		Vector2 vector = new Vector2(xLocation * 64, yLocation * 64);
		bool flag = (who?.CurrentTool as Hoe)?.hasEnchantmentOfType<GenerousEnchantment>() ?? false;
		LocationData data = GetData();
		ItemQueryContext context = new ItemQueryContext(this, who, random, "location '" + NameOrUniqueName + "' > artifact spots");
		IEnumerable<ArtifactSpotDropData> enumerable = Game1.locationData["Default"].ArtifactSpots;
		if (data != null && data.ArtifactSpots?.Count > 0)
		{
			enumerable = enumerable.Concat(data.ArtifactSpots);
		}
		enumerable = enumerable.OrderBy((ArtifactSpotDropData p) => p.Precedence);
		if (Game1.player.mailReceived.Contains("sawQiPlane") && random.NextDouble() < 0.05 + Game1.player.team.AverageDailyLuck() / 2.0)
		{
			Game1.createMultipleItemDebris(ItemRegistry.Create("(O)MysteryBox", random.Next(1, 3)), vector, -1, this);
		}
		Utility.trySpawnRareObject(who, vector, this, 9.0, 1.0, -1, random);
		if (random.NextDouble() < 0.25)
		{
			string relicId = StardewValley.Pokemon.PokemonRelics.GetArtifactSpotRelic(this, random);
			if (!string.IsNullOrEmpty(relicId))
			{
				Item relicObj = ItemRegistry.Create("(O)" + relicId, 1, 0, allowNull: true) ?? new StardewValley.Object(relicId, 1);
				if (relicObj != null)
				{
					Game1.createItemDebris(relicObj, vector, Game1.random.Next(4), this);
				}
			}
		}
		foreach (ArtifactSpotDropData drop in enumerable)
		{
			if (!random.NextBool(drop.Chance) || (drop.Condition != null && !GameStateQuery.CheckConditions(drop.Condition, this, who, null, null, random)))
			{
				continue;
			}
			Item item = ItemQueryResolver.TryResolveRandomItem(drop, context, avoidRepeat: false, null, null, null, delegate(string query, string error)
			{
				Game1.log.Error($"Location '{NameOrUniqueName}' failed parsing item query '{query}' for artifact spot '{drop.Id}': {error}");
			});
			if (item == null)
			{
				continue;
			}
			if (drop.OneDebrisPerDrop && item.Stack > 1)
			{
				Game1.createMultipleItemDebris(item, vector, -1, this);
			}
			else
			{
				Game1.createItemDebris(item, vector, Game1.random.Next(4), this);
			}
			if (flag && drop.ApplyGenerousEnchantment && random.NextBool())
			{
				item = item.getOne();
				item = (Item)ItemQueryResolver.ApplyItemFields(item, drop, context);
				if (drop.OneDebrisPerDrop && item.Stack > 1)
				{
					Game1.createMultipleItemDebris(item, vector, -1, this);
				}
				else
				{
					Game1.createItemDebris(item, vector, -1, this);
				}
			}
			if (!drop.ContinueOnDrop)
			{
				break;
			}
		}
	}

	/// <summary>Get the underlying data from <c>Data/Locations</c> for this location, if available.</summary>
	/// <remarks>If this is a passive festival location and doesn't have its own data, this will return the data matching its <see cref="F:StardewValley.GameData.PassiveFestivalData.MapReplacements" /> field.</remarks>
	public LocationData GetData()
	{
		string text = Name;
		if (!(this is MineShaft))
		{
			if (this is Cellar && text.StartsWith("Cellar"))
			{
				text = "Cellar";
			}
		}
		else
		{
			text = "UndergroundMine";
		}
		return GetData(text);
	}

	/// <summary>Get the underlying data from <c>Data/Locations</c> for this location, if available.</summary>
	/// <param name="name">The location name to match.</param>
	/// <remarks>If this is a passive festival location and doesn't have its own data, this will return the data matching its <see cref="F:StardewValley.GameData.PassiveFestivalData.MapReplacements" /> field.</remarks>
	public static LocationData GetData(string name)
	{
		IDictionary<string, LocationData> rawData = Game1.locationData;
		if (name == "Farm")
		{
			return GetImpl("Farm_" + Game1.GetFarmTypeKey()) ?? GetImpl("Farm_Standard");
		}
		return GetImpl(name);
		LocationData GetImpl(string entryName)
		{
			if (rawData.TryGetValue(entryName, out var value))
			{
				return value;
			}
			foreach (string activePassiveFestival in Game1.netWorldState.Value.ActivePassiveFestivals)
			{
				if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data) && data.MapReplacements != null)
				{
					foreach (KeyValuePair<string, string> mapReplacement in data.MapReplacements)
					{
						if (mapReplacement.Value == entryName)
						{
							if (!rawData.TryGetValue(mapReplacement.Key, out value))
							{
								break;
							}
							return value;
						}
					}
				}
			}
			return null;
		}
	}

	/// <summary>Get whether NPCs should ignore this location when pathfinding between locations.</summary>
	public virtual bool ShouldExcludeFromNpcPathfinding()
	{
		return GetData()?.ExcludeFromNpcPathfinding ?? false;
	}

	public virtual string checkForBuriedItem(int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
	{
		Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation * 77, Game1.stats.DirtHoed);
		string text = HandleTreasureTileProperty(xLocation, yLocation, detectOnly);
		if (text != null)
		{
			return text;
		}
		bool flag = who?.CurrentTool is Hoe && who.CurrentTool.hasEnchantmentOfType<GenerousEnchantment>();
		float num = 0.5f;
		if (!isFarm.Value && isOutdoors.Value && GetSeason() == Season.Winter && random.NextDouble() < 0.08 && !explosion && !detectOnly && !(this is Desert))
		{
			Game1.createObjectDebris(random.Choose("(O)412", "(O)416"), xLocation, yLocation);
			if (flag && random.NextDouble() < (double)num)
			{
				Game1.createObjectDebris(random.Choose("(O)412", "(O)416"), xLocation, yLocation);
			}
			return "";
		}
		LocationData data = GetData();
		if (isOutdoors.Value && random.NextBool(data?.ChanceForClay ?? 0.03) && !explosion)
		{
			if (detectOnly)
			{
				map.RequireLayer("Back").Tiles[xLocation, yLocation].Properties.Add("Treasure", "Item (O)330");
				return "Item";
			}
			Game1.createObjectDebris("(O)330", xLocation, yLocation);
			if (flag && random.NextDouble() < (double)num)
			{
				Game1.createObjectDebris("(O)330", xLocation, yLocation);
			}
			return "";
		}
		return "";
	}

	private string HandleTreasureTileProperty(int xLocation, int yLocation, bool detectOnly)
	{
		string text = doesTileHaveProperty(xLocation, yLocation, "Treasure", "Back");
		if (text == null)
		{
			return null;
		}
		string[] array = ArgUtility.SplitBySpace(text);
		if (!ArgUtility.TryGet(array, 0, out var value, out var error, allowBlank: true, "string type"))
		{
			LogError(text, error);
			return null;
		}
		if (detectOnly)
		{
			return value;
		}
		switch (value)
		{
		case "Arch":
		{
			if (ArgUtility.TryGet(array, 1, out var value3, out error, allowBlank: true, "string itemId"))
			{
				Game1.createObjectDebris(value3, xLocation, yLocation);
			}
			else
			{
				LogError(text, error);
			}
			break;
		}
		case "CaveCarrot":
			Game1.createObjectDebris("(O)78", xLocation, yLocation);
			break;
		case "Coins":
			Game1.createObjectDebris("(O)330", xLocation, yLocation);
			break;
		case "Coal":
		case "Gold":
		case "Iron":
		case "Copper":
		case "Iridium":
		{
			int debrisType = value switch
			{
				"Coal" => 4, 
				"Copper" => 0, 
				"Gold" => 6, 
				"Iridium" => 10, 
				_ => 2, 
			};
			if (ArgUtility.TryGetInt(array, 1, out var value5, out error, "int itemId"))
			{
				Game1.createDebris(debrisType, xLocation, yLocation, value5);
			}
			else
			{
				LogError(text, error);
			}
			break;
		}
		case "Object":
		{
			if (ArgUtility.TryGet(array, 1, out var value4, out error, allowBlank: true, "string itemId"))
			{
				Game1.createObjectDebris(value4, xLocation, yLocation);
				if (value4 == "78" || value4 == "(O)79")
				{
					Game1.stats.CaveCarrotsFound++;
				}
			}
			else
			{
				LogError(text, error);
			}
			break;
		}
		case "Item":
		{
			if (ArgUtility.TryGet(array, 1, out var value2, out error, allowBlank: true, "string itemId"))
			{
				Item item = ItemRegistry.Create(value2);
				Game1.createItemDebris(item, new Vector2(xLocation, yLocation), -1, this);
				if (item.QualifiedItemId == "(O)78")
				{
					Game1.stats.CaveCarrotsFound++;
				}
			}
			else
			{
				LogError(text, error);
			}
			break;
		}
		default:
			value = null;
			LogError(text, "invalid treasure type '" + value + "'");
			break;
		}
		map.RequireLayer("Back").Tiles[xLocation, yLocation].Properties["Treasure"] = null;
		return value;
		void LogError(string value6, string errorPhrase)
		{
			LogTilePropertyError("Treasure", "Back", xLocation, yLocation, value6, errorPhrase);
		}
	}

	public virtual bool AllowMapModificationsInResetState()
	{
		return false;
	}

	/// <summary>Remove a tile from the location's map.</summary>
	/// <param name="tileX">The X tile position to set.</param>
	/// <param name="tileY">The Y tile position to set.</param>
	/// <param name="layer">The layer whose tile to set.</param>
	public void removeMapTile(int tileX, int tileY, string layer)
	{
		Layer layer2 = map?.RequireLayer(layer);
		if (layer2?.Tiles[tileX, tileY] != null)
		{
			layer2.Tiles[tileX, tileY] = null;
		}
	}

	/// <summary>Change the tile at a given map position to use the given tilesheet and tile index, recreating it if needed.</summary>
	/// <param name="tileX">The X tile position to set.</param>
	/// <param name="tileY">The Y tile position to set.</param>
	/// <param name="index">The tile index in the tilesheet to show.</param>
	/// <param name="layer">The layer whose tile to set.</param>
	/// <param name="tileSheetId">The tilesheet ID from which to get the <paramref name="index" />.</param>
	/// <param name="action">The <c>Action</c> tile property to set, or <c>null</c> for none. This is ignored if <paramref name="layer" /> is not <c>Buildings</c>.</param>
	/// <param name="copyProperties">If the tile is recreated, whether to copy any tile properties that were on the previous tile.</param>
	/// <returns>Returns the new or updated tile at the tile position.</returns>
	public StaticTile setMapTile(int tileX, int tileY, int index, string layer, string tileSheetId, string action = null, bool copyProperties = true)
	{
		Layer layer2 = map.RequireLayer(layer);
		Tile tile = layer2.Tiles[tileX, tileY];
		StaticTile staticTile = tile as StaticTile;
		if (staticTile != null && staticTile.TileSheet.Id == tileSheetId)
		{
			staticTile.TileIndex = index;
		}
		else
		{
			staticTile = (StaticTile)(layer2.Tiles[tileX, tileY] = new StaticTile(layer2, map.RequireTileSheet(tileSheetId), BlendMode.Alpha, index));
			if (copyProperties && tile != null)
			{
				foreach (KeyValuePair<string, PropertyValue> property in tile.Properties)
				{
					staticTile.Properties[property.Key] = property.Value;
				}
			}
		}
		if (action != null && layer == "Buildings")
		{
			staticTile.Properties["Action"] = action;
		}
		return staticTile;
	}

	/// <summary>Replace a map tile with an animated tile.</summary>
	/// <param name="tileX">The X tile position to set.</param>
	/// <param name="tileY">The Y tile position to set.</param>
	/// <param name="animationTileIndexes">The tile index in the tilesheet to show for each frame in the animation.</param>
	/// <param name="interval">The number of milliseconds for which to show each frame of the animation.</param>
	/// <param name="layer">The layer whose tile to set.</param>
	/// <param name="tileSheetId">The tilesheet ID from which to get the <paramref name="animationTileIndexes" />.</param>
	/// <param name="action">The <c>Action</c> tile property to set, or <c>null</c> for none. This is ignored if <paramref name="layer" /> is not <c>Buildings</c>.</param>
	/// <param name="copyProperties">Whether to copy any tile properties that were on the previous tile.</param>
	/// <returns>Returns the new tile at the tile position.</returns>
	public AnimatedTile setAnimatedMapTile(int tileX, int tileY, int[] animationTileIndexes, long interval, string layer, string tileSheetId, string action = null, bool copyProperties = true)
	{
		Layer layer2 = map.RequireLayer(layer);
		TileSheet tileSheet = map.RequireTileSheet(tileSheetId);
		StaticTile[] array = new StaticTile[animationTileIndexes.Length];
		for (int i = 0; i < animationTileIndexes.Length; i++)
		{
			array[i] = new StaticTile(layer2, tileSheet, BlendMode.Alpha, animationTileIndexes[i]);
		}
		AnimatedTile animatedTile = new AnimatedTile(layer2, array, interval);
		if (copyProperties)
		{
			Tile tile = layer2.Tiles[tileX, tileY];
			if (tile != null)
			{
				foreach (KeyValuePair<string, PropertyValue> property in tile.Properties)
				{
					animatedTile.Properties[property.Key] = property.Value;
				}
			}
		}
		if (action != null && layer == "Buildings")
		{
			animatedTile.Properties["Action"] = action;
		}
		layer2.Tiles[tileX, tileY] = animatedTile;
		return animatedTile;
	}

	/// <summary>Move all objects, furniture, terrain features, and large terrain features within the location.</summary>
	/// <param name="dx">The X tile offset to apply.</param>
	/// <param name="dy">The Y tile offset to apply.</param>
	/// <param name="where">If set, a filter which indicates whether something should be moved.</param>
	public virtual void shiftContents(int dx, int dy, Func<Vector2, object, bool> where = null)
	{
		Vector2 vector = new Vector2(dx, dy);
		List<KeyValuePair<Vector2, Object>> list = new List<KeyValuePair<Vector2, Object>>(objects.Pairs);
		objects.Clear();
		foreach (KeyValuePair<Vector2, Object> item in list)
		{
			if (where == null || where(item.Key, item.Value))
			{
				removeLightSource(item.Value.lightSource?.Id);
				Vector2 vector2 = item.Key + vector;
				objects.Add(vector2, item.Value);
				item.Value.initializeLightSource(vector2);
			}
			else
			{
				objects.Add(item.Key, item.Value);
			}
		}
		List<KeyValuePair<Vector2, TerrainFeature>> list2 = new List<KeyValuePair<Vector2, TerrainFeature>>(terrainFeatures.Pairs);
		terrainFeatures.Clear();
		foreach (KeyValuePair<Vector2, TerrainFeature> item2 in list2)
		{
			Vector2 key = ((where == null || where(item2.Key, item2.Value)) ? (item2.Key + vector) : item2.Key);
			terrainFeatures.Add(key, item2.Value);
		}
		foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
		{
			if (where == null || where(largeTerrainFeature.Tile, largeTerrainFeature))
			{
				largeTerrainFeature.Tile += vector;
			}
		}
		foreach (Furniture item3 in furniture)
		{
			if (where == null || where(item3.TileLocation, item3))
			{
				item3.removeLights();
				item3.TileLocation = new Vector2(item3.TileLocation.X + (float)dx, item3.TileLocation.Y + (float)dy);
				item3.updateDrawPosition();
				if (Game1.isDarkOut(this))
				{
					item3.addLights();
				}
			}
		}
	}

	public void moveFurniture(int oldX, int oldY, int newX, int newY)
	{
		Vector2 vector = new Vector2(oldX, oldY);
		foreach (Furniture item in furniture)
		{
			if (item.tileLocation.Equals(vector))
			{
				item.removeLights();
				item.TileLocation = new Vector2(newX, newY);
				if (Game1.isDarkOut(this))
				{
					item.addLights();
				}
				return;
			}
		}
		if (objects.TryGetValue(vector, out var value))
		{
			objects.Remove(vector);
			objects.Add(new Vector2(newX, newY), value);
		}
	}

	/// <summary>Get whether a tile exists at the given coordinate.</summary>
	/// <param name="x">The tile X coordinate.</param>
	/// <param name="y">The tile Y coordinate.</param>
	/// <param name="layer">The layer whose tiles to check.</param>
	/// <param name="tilesheetId">The tilesheet ID to check, or <c>null</c> for any tilesheet. If the tile doesn't use this tilesheet, it'll be ignored.</param>
	public bool hasTileAt(int x, int y, string layer, string tilesheetId = null)
	{
		return map?.HasTileAt(x, y, layer, tilesheetId) ?? false;
	}

	/// <summary>Get whether a tile exists at the given coordinate.</summary>
	/// <param name="tile">The tile coordinate.</param>
	/// <param name="layer">The layer whose tiles to check.</param>
	/// <param name="tilesheetId">The tilesheet ID to check, or <c>null</c> for any tilesheet. If the tile doesn't use this tilesheet, it'll be ignored.</param>
	public bool hasTileAt(Location tile, string layer, string tilesheetId = null)
	{
		return map?.HasTileAt(tile.X, tile.Y, layer, tilesheetId) ?? false;
	}

	/// <summary>Get whether a tile exists at the given coordinate.</summary>
	/// <param name="tile">The tile coordinate.</param>
	/// <param name="layer">The layer whose tiles to check.</param>
	/// <param name="tilesheetId">The tilesheet ID to check, or <c>null</c> for any tilesheet. If the tile doesn't use this tilesheet, it'll be ignored.</param>
	public bool hasTileAt(Point tile, string layer, string tilesheetId = null)
	{
		return map?.HasTileAt(tile.X, tile.Y, layer, tilesheetId) ?? false;
	}

	/// <summary>Get the tile index at the given map coordinate.</summary>
	/// <param name="p">The tile coordinate.</param>
	/// <param name="layer">The layer whose tiles to check.</param>
	/// <param name="tilesheetId">The tilesheet ID for which to get a tile index, or <c>null</c> for any tilesheet. If the tile doesn't use this tilesheet, it'll be ignored.</param>
	/// <returns>Returns the matching tile's index, or <c>-1</c> if no tile was found.</returns>
	public int getTileIndexAt(Location p, string layer, string tilesheetId = null)
	{
		return map?.GetTileIndexAt(p.X, p.Y, layer, tilesheetId) ?? (-1);
	}

	/// <summary>Get the tile index at the given map coordinate.</summary>
	/// <param name="p">The tile coordinate.</param>
	/// <param name="layer">The layer whose tiles to check.</param>
	/// <param name="tilesheetId">The tilesheet ID for which to get a tile index, or <c>null</c> for any tilesheet. If the tile doesn't use this tilesheet, it'll be ignored.</param>
	/// <returns>Returns the matching tile's index, or <c>-1</c> if no tile was found.</returns>
	public int getTileIndexAt(Point p, string layer, string tilesheetId = null)
	{
		return map?.GetTileIndexAt(p.X, p.Y, layer, tilesheetId) ?? (-1);
	}

	/// <summary>Get the tile index at the given layer coordinate.</summary>
	/// <param name="x">The tile X coordinate.</param>
	/// <param name="y">The tile Y coordinate.</param>
	/// <param name="layer">The layer whose tiles to check.</param>
	/// <param name="tilesheetId">The tilesheet ID for which to get a tile index, or <c>null</c> for any tilesheet. If the tile doesn't use this tilesheet, it'll be ignored.</param>
	/// <returns>Returns the matching tile's index, or <c>-1</c> if no tile was found.</returns>
	public int getTileIndexAt(int x, int y, string layer, string tilesheetId = null)
	{
		return map?.GetTileIndexAt(x, y, layer, tilesheetId) ?? (-1);
	}

	public string getTileSheetIDAt(int x, int y, string layer)
	{
		return map.GetLayer(layer)?.Tiles[x, y]?.TileSheet.Id ?? "";
	}

	/// <summary>Handle a building in this location being constructed by any player.</summary>
	/// <param name="building">The building that was constructed.</param>
	/// <param name="who">The player that constructed the building.</param>
	public virtual void OnBuildingConstructed(Building building, Farmer who)
	{
		building.performActionOnConstruction(this, who);
	}

	/// <summary>Handle a building in this location being moved by any player.</summary>
	/// <param name="building">The building that was moved.</param>
	public virtual void OnBuildingMoved(Building building)
	{
		building.performActionOnBuildingPlacement();
	}

	/// <summary>Handle a building in this location being demolished by the current player.</summary>
	/// <param name="building">The building type that was demolished.</param>
	/// <param name="id">The unique building ID.</param>
	public virtual void OnBuildingDemolished(string type, Guid id)
	{
		if (type == "Stable")
		{
			Horse mount = Game1.player.mount;
			if (mount != null && mount.HorseId == id)
			{
				Game1.player.mount.dismount(from_demolish: true);
			}
		}
	}

	/// <summary>Handle the new day starting after the player saves, loads, or connects.</summary>
	/// <remarks>See also <see cref="M:StardewValley.GameLocation.DayUpdate(System.Int32)" />, which happens while setting up the day before saving.</remarks>
	public virtual void OnDayStarted()
	{
	}

	/// <summary>Handle a breakable mine stone being destroyed.</summary>
	/// <param name="stoneId">The unqualified item ID for the stone object.</param>
	/// <param name="x">The stone's X tile position.</param>
	/// <param name="y">The stone's Y tile position.</param>
	/// <param name="who">The player who broke the stone.</param>
	/// <remarks>This is the entry point for creating item drops when breaking stone.</remarks>
	public void OnStoneDestroyed(string stoneId, int x, int y, Farmer who)
	{
		long whichPlayer = who?.UniqueMultiplayerID ?? 0;
		if (who?.currentLocation is MineShaft { mineLevel: >120 } mineShaft && !mineShaft.isSideBranch())
		{
			int num = mineShaft.mineLevel - 121;
			if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0)
			{
				float num2 = 0.01f;
				num2 += (float)num * 0.0005f;
				if (num2 > 0.5f)
				{
					num2 = 0.5f;
				}
				if (Game1.random.NextBool(num2))
				{
					Game1.createMultipleObjectDebris("CalicoEgg", x, y, Game1.random.Next(1, 4), who.UniqueMultiplayerID, this);
				}
			}
		}
		if (who != null && Game1.random.NextDouble() <= 0.02 && Game1.player.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
		{
			Game1.createMultipleObjectDebris("(O)890", x, y, 1, who.UniqueMultiplayerID, this);
		}
		if (!MineShaft.IsGeneratedLevel(this))
		{
			if (stoneId == "343" || stoneId == "450")
			{
				Random random = Utility.CreateDaySaveRandom(x * 2000, y);
				double num3 = ((who != null && who.hasBuff("dwarfStatue_4")) ? 1.25 : 1.0);
				if (random.NextDouble() < 0.035 * num3 && Game1.stats.DaysPlayed > 1)
				{
					Game1.createObjectDebris("(O)" + (535 + ((Game1.stats.DaysPlayed > 60 && random.NextDouble() < 0.2) ? 1 : ((Game1.stats.DaysPlayed > 120 && random.NextDouble() < 0.2) ? 2 : 0))), x, y, whichPlayer, this);
				}
				int num4 = ((who == null || !who.professions.Contains(21)) ? 1 : 2);
				double num5 = ((who != null && who.hasBuff("dwarfStatue_2")) ? 0.03 : 0.0);
				if (random.NextDouble() < 0.035 * (double)num4 + num5 && Game1.stats.DaysPlayed > 1)
				{
					Game1.createObjectDebris("(O)382", x, y, whichPlayer, this);
				}
				if (random.NextDouble() < 0.01 && Game1.stats.DaysPlayed > 1)
				{
					Game1.createObjectDebris("(O)390", x, y, whichPlayer, this);
				}
			}
			breakStone(stoneId, x, y, who, Utility.CreateDaySaveRandom(x * 4000, y));
		}
		else
		{
			(this as MineShaft).checkStoneForItems(stoneId, x, y, who);
		}
	}

	protected virtual bool breakStone(string stoneId, int x, int y, Farmer who, Random r)
	{
		int num = 0;
		int num2 = ((who != null && who.professions.Contains(18)) ? 1 : 0);
		if (who != null && who.hasBuff("dwarfStatue_0"))
		{
			num2++;
		}
		if (stoneId == 44.ToString())
		{
			stoneId = (r.Next(1, 8) * 2).ToString();
		}
		long num3 = who?.UniqueMultiplayerID ?? 0;
		int num4 = who?.LuckLevel ?? 0;
		double num5 = who?.DailyLuck ?? 0.0;
		int num6 = who?.MiningLevel ?? 0;
		switch (stoneId)
		{
		case "95":
			Game1.createMultipleObjectDebris("(O)909", x, y, num2 + r.Next(1, 3) + ((r.NextDouble() < (double)((float)num4 / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)num6 / 200f)) ? 1 : 0), num3, this);
			num = 18;
			break;
		case "843":
		case "844":
			Game1.createMultipleObjectDebris("(O)848", x, y, num2 + r.Next(1, 3) + ((r.NextDouble() < (double)((float)num4 / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)num6 / 200f)) ? 1 : 0), num3, this);
			num = 12;
			break;
		case "25":
			Game1.createMultipleObjectDebris("(O)719", x, y, r.Next(2, 5), num3, this);
			num = 5;
			if (this is IslandLocation && r.NextDouble() < 0.1)
			{
				Game1.player.team.RequestLimitedNutDrops("MusselStone", this, x * 64, y * 64, 5);
			}
			break;
		case "75":
			Game1.createObjectDebris("(O)535", x, y, num3, this);
			num = 8;
			break;
		case "76":
			Game1.createObjectDebris("(O)536", x, y, num3, this);
			num = 16;
			break;
		case "77":
			Game1.createObjectDebris("(O)537", x, y, num3, this);
			num = 32;
			break;
		case "816":
		case "817":
			if (r.NextDouble() < 0.1)
			{
				Game1.createObjectDebris("(O)823", x, y, num3, this);
			}
			else if (r.NextDouble() < 0.015)
			{
				Game1.createObjectDebris("(O)824", x, y, num3, this);
			}
			else if (r.NextDouble() < 0.1)
			{
				Game1.createObjectDebris("(O)" + (579 + r.Next(11)), x, y, num3, this);
			}
			Game1.createMultipleObjectDebris("(O)881", x, y, num2 + r.Next(1, 3) + ((r.NextDouble() < (double)((float)num4 / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)num6 / 100f)) ? 1 : 0), num3, this);
			num = 6;
			break;
		case "818":
			Game1.createMultipleObjectDebris("(O)330", x, y, num2 + r.Next(1, 3) + ((r.NextDouble() < (double)((float)num4 / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)num6 / 100f)) ? 1 : 0), num3, this);
			num = 6;
			break;
		case "819":
			Game1.createObjectDebris("(O)749", x, y, num3, this);
			num = 64;
			break;
		case "8":
			Game1.createMultipleObjectDebris("(O)66", x, y, (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2, num3, this);
			num = 16;
			break;
		case "10":
			Game1.createMultipleObjectDebris("(O)68", x, y, (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2, num3, this);
			num = 16;
			break;
		case "12":
			Game1.createMultipleObjectDebris("(O)60", x, y, (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2, num3, this);
			num = 80;
			break;
		case "14":
			Game1.createMultipleObjectDebris("(O)62", x, y, (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2, num3, this);
			num = 40;
			break;
		case "6":
			Game1.createMultipleObjectDebris("(O)70", x, y, (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2, num3, this);
			num = 40;
			break;
		case "4":
			Game1.createMultipleObjectDebris("(O)64", x, y, (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2, num3, this);
			num = 80;
			break;
		case "2":
			Game1.createMultipleObjectDebris("(O)72", x, y, (who == null || who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2, num3, this);
			num = 150;
			break;
		case "846":
		case "847":
		case "668":
		case "845":
		case "670":
			Game1.createMultipleObjectDebris("(O)390", x, y, num2 + r.Next(1, 3) + ((r.NextDouble() < (double)((float)num4 / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)num6 / 100f)) ? 1 : 0), num3, this);
			num = 3;
			if (r.NextDouble() < 0.08)
			{
				Game1.createMultipleObjectDebris("(O)382", x, y, 1 + num2, num3, this);
				num = 4;
			}
			break;
		case "849":
		case "751":
			Game1.createMultipleObjectDebris("(O)378", x, y, num2 + r.Next(1, 4) + ((r.NextDouble() < (double)((float)num4 / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)num6 / 100f)) ? 1 : 0), num3, this);
			num = 5;
			Game1.multiplayer.broadcastSprites(this, Utility.sparkleWithinArea(new Microsoft.Xna.Framework.Rectangle(x * 64, (y - 1) * 64, 32, 96), 3, Color.Orange * 0.5f, 175, 100));
			break;
		case "850":
		case "290":
			Game1.createMultipleObjectDebris("(O)380", x, y, num2 + r.Next(1, 4) + ((r.NextDouble() < (double)((float)num4 / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)num6 / 100f)) ? 1 : 0), num3, this);
			num = 12;
			Game1.multiplayer.broadcastSprites(this, Utility.sparkleWithinArea(new Microsoft.Xna.Framework.Rectangle(x * 64, (y - 1) * 64, 32, 96), 3, Color.White * 0.5f, 175, 100));
			break;
		case "BasicCoalNode0":
		case "BasicCoalNode1":
		case "VolcanoCoalNode0":
		case "VolcanoCoalNode1":
			Game1.createMultipleObjectDebris("(O)382", x, y, num2 + r.Next(1, 4) + ((r.NextDouble() < (double)((float)num4 / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)num6 / 100f)) ? 1 : 0), num3, this);
			num = 10;
			Game1.multiplayer.broadcastSprites(this, Utility.sparkleWithinArea(new Microsoft.Xna.Framework.Rectangle(x * 64, (y - 1) * 64, 32, 96), 3, Color.Black * 0.5f, 175, 100));
			break;
		case "764":
		case "VolcanoGoldNode":
			Game1.createMultipleObjectDebris("(O)384", x, y, num2 + r.Next(1, 4) + ((r.NextDouble() < (double)((float)num4 / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)num6 / 100f)) ? 1 : 0), num3, this);
			num = 18;
			Game1.multiplayer.broadcastSprites(this, Utility.sparkleWithinArea(new Microsoft.Xna.Framework.Rectangle(x * 64, (y - 1) * 64, 32, 96), 3, Color.Yellow * 0.5f, 175, 100));
			break;
		case "765":
			Game1.createMultipleObjectDebris("(O)386", x, y, num2 + r.Next(1, 4) + ((r.NextDouble() < (double)((float)num4 / 100f)) ? 1 : 0) + ((r.NextDouble() < (double)((float)num6 / 100f)) ? 1 : 0), num3, this);
			Game1.multiplayer.broadcastSprites(this, Utility.sparkleWithinArea(new Microsoft.Xna.Framework.Rectangle(x * 64, (y - 1) * 64, 32, 96), 6, Color.BlueViolet * 0.5f, 175, 100));
			if (r.NextDouble() < 0.035)
			{
				Game1.createMultipleObjectDebris("(O)74", x, y, 1, num3, this);
			}
			num = 50;
			break;
		case "CalicoEggStone_0":
		case "CalicoEggStone_1":
		case "CalicoEggStone_2":
			Game1.createMultipleObjectDebris("CalicoEgg", x, y, r.Next(1, 4) + (r.NextBool((float)num4 / 100f) ? 1 : 0) + (r.NextBool((float)num6 / 100f) ? 1 : 0), num3, this);
			num = 50;
			Game1.multiplayer.broadcastSprites(this, Utility.sparkleWithinArea(new Microsoft.Xna.Framework.Rectangle(x * 64, (y - 1) * 64, 32, 96), 6, new Color(255, 120, 0) * 0.5f, 175, 100));
			break;
		}
		if (who != null && who.professions.Contains(19) && r.NextBool())
		{
			int number = ((who.stats.Get(StatKeys.Mastery(3)) == 0) ? 1 : 2);
			switch (stoneId)
			{
			case "8":
				Game1.createMultipleObjectDebris("(O)66", x, y, number, who.UniqueMultiplayerID, this);
				num = 8;
				break;
			case "10":
				Game1.createMultipleObjectDebris("(O)68", x, y, number, who.UniqueMultiplayerID, this);
				num = 8;
				break;
			case "12":
				Game1.createMultipleObjectDebris("(O)60", x, y, number, who.UniqueMultiplayerID, this);
				num = 50;
				break;
			case "14":
				Game1.createMultipleObjectDebris("(O)62", x, y, number, who.UniqueMultiplayerID, this);
				num = 20;
				break;
			case "6":
				Game1.createMultipleObjectDebris("(O)70", x, y, number, who.UniqueMultiplayerID, this);
				num = 20;
				break;
			case "4":
				Game1.createMultipleObjectDebris("(O)64", x, y, number, who.UniqueMultiplayerID, this);
				num = 50;
				break;
			case "2":
				Game1.createMultipleObjectDebris("(O)72", x, y, number, who.UniqueMultiplayerID, this);
				num = 100;
				break;
			}
		}
		if (stoneId == 46.ToString())
		{
			Game1.createDebris(10, x, y, r.Next(1, 4), this);
			Game1.createDebris(6, x, y, r.Next(1, 5), this);
			if (r.NextDouble() < 0.25)
			{
				Game1.createMultipleObjectDebris("(O)74", x, y, 1, num3, this);
			}
			num = 150;
			Game1.stats.MysticStonesCrushed++;
		}
		if ((isOutdoors.Value || treatAsOutdoors.Value) && num == 0)
		{
			double num7 = num5 / 2.0 + (double)num6 * 0.005 + (double)num4 * 0.001;
			Random random = Utility.CreateDaySaveRandom(x * 1000, y);
			Game1.createDebris(14, x, y, 1, this);
			if (who != null)
			{
				who.gainExperience(3, 1);
				double num8 = 0.0;
				if (who.professions.Contains(21))
				{
					num8 += 0.05 * (1.0 + num7);
				}
				if (who.hasBuff("dwarfStatue_2"))
				{
					num8 += 0.025;
				}
				if (random.NextDouble() < num8)
				{
					Game1.createObjectDebris("(O)382", x, y, who.UniqueMultiplayerID, this);
				}
			}
			if (random.NextDouble() < 0.05 * (1.0 + num7))
			{
				Game1.createObjectDebris("(O)382", x, y, num3, this);
				Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(25, new Vector2(64 * x, 64 * y), Color.White, 8, Game1.random.NextBool(), 80f, 0, -1, -1f, 128));
				who?.gainExperience(3, 5);
			}
		}
		if (who != null && HasUnlockedAreaSecretNotes(who) && r.NextDouble() < 0.0075)
		{
			Object obj = tryToCreateUnseenSecretNote(who);
			if (obj != null)
			{
				Game1.createItemDebris(obj, new Vector2((float)x + 0.5f, (float)y + 0.75f) * 64f, Game1.player.FacingDirection, this);
			}
		}
		who?.gainExperience(3, num);
		return num > 0;
	}

	public bool isBehindBush(Vector2 Tile)
	{
		if (largeTerrainFeatures != null)
		{
			Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle((int)Tile.X * 64, (int)(Tile.Y + 1f) * 64, 64, 128);
			foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
			{
				if (largeTerrainFeature.getBoundingBox().Intersects(value))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool isBehindTree(Vector2 Tile)
	{
		if (terrainFeatures != null)
		{
			Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle((int)(Tile.X - 1f) * 64, (int)Tile.Y * 64, 192, 256);
			foreach (KeyValuePair<Vector2, TerrainFeature> pair in terrainFeatures.Pairs)
			{
				if (pair.Value is Tree && pair.Value.getBoundingBox().Intersects(value))
				{
					return true;
				}
			}
		}
		return false;
	}

	public virtual void spawnObjects()
	{
		Random random = Utility.CreateDaySaveRandom();
		LocationData data = GetData();
		if (data != null && numberOfSpawnedObjectsOnMap < data.MaxSpawnedForageAtOnce)
		{
			Season season = GetSeason();
			List<SpawnForageData> list = new List<SpawnForageData>();
			foreach (SpawnForageData item2 in GetData("Default").Forage.Concat(data.Forage))
			{
				if ((item2.Condition == null || GameStateQuery.CheckConditions(item2.Condition, this, null, null, null, random)) && (!item2.Season.HasValue || item2.Season == season))
				{
					list.Add(item2);
				}
			}
			if (list.Any())
			{
				int val = random.Next(data.MinDailyForageSpawn, data.MaxDailyForageSpawn + 1);
				val = Math.Min(val, data.MaxSpawnedForageAtOnce - numberOfSpawnedObjectsOnMap);
				ItemQueryContext context = new ItemQueryContext(this, null, random, "location '" + NameOrUniqueName + "' > forage");
				for (int i = 0; i < val; i++)
				{
					for (int j = 0; j < 11; j++)
					{
						int num = random.Next(map.DisplayWidth / 64);
						int num2 = random.Next(map.DisplayHeight / 64);
						Vector2 vector = new Vector2(num, num2);
						if (objects.ContainsKey(vector) || IsNoSpawnTile(vector) || doesTileHaveProperty(num, num2, "Spawnable", "Back") == null || doesEitherTileOrTileIndexPropertyEqual(num, num2, "Spawnable", "Back", "F") || !CanItemBePlacedHere(vector) || hasTileAt(num, num2, "AlwaysFront") || hasTileAt(num, num2, "AlwaysFront2") || hasTileAt(num, num2, "AlwaysFront3") || hasTileAt(num, num2, "Front") || isBehindBush(vector) || (!random.NextBool(0.1) && isBehindTree(vector)))
						{
							continue;
						}
						SpawnForageData forage = random.ChooseFrom(list);
						if (!random.NextBool(forage.Chance))
						{
							continue;
						}
						Item item = ItemQueryResolver.TryResolveRandomItem(forage, context, avoidRepeat: false, null, null, null, delegate(string query, string error)
						{
							Game1.log.Error($"Location '{NameOrUniqueName}' failed parsing item query '{query}' for forage '{forage.Id}': {error}");
						});
						if (item == null)
						{
							continue;
						}
						if (!(item is Object obj))
						{
							Game1.log.Warn($"Location '{Name}' ignored invalid forage data '{forage.Id}': the resulting item '{item.QualifiedItemId}' isn't an {"Object"}-type item.");
						}
						else
						{
							obj.IsSpawnedObject = true;
							if (dropObject(obj, vector * 64f, Game1.viewport, initialPlacement: true))
							{
								numberOfSpawnedObjectsOnMap++;
								break;
							}
						}
					}
				}
			}
		}
		List<Vector2> list2 = new List<Vector2>();
		foreach (KeyValuePair<Vector2, Object> pair in objects.Pairs)
		{
			if (pair.Value.QualifiedItemId == "(O)590" || pair.Value.QualifiedItemId == "(O)SeedSpot")
			{
				list2.Add(pair.Key);
			}
		}
		if (!(this is Farm) && !(this is IslandWest))
		{
			spawnWeedsAndStones();
		}
		for (int num3 = list2.Count - 1; num3 >= 0; num3--)
		{
			if ((!(this is IslandNorth) || !(list2[num3].X < 26f)) && random.NextBool(0.15))
			{
				objects.Remove(list2[num3]);
				list2.RemoveAt(num3);
			}
		}
		if (list2.Count > ((!(this is Farm)) ? 1 : 0) && (GetSeason() != Season.Winter || list2.Count > 4))
		{
			return;
		}
		double num4 = 1.0;
		while (random.NextDouble() < num4)
		{
			int num5 = random.Next(map.DisplayWidth / 64);
			int num6 = random.Next(map.DisplayHeight / 64);
			Vector2 vector2 = new Vector2(num5, num6);
			if (CanItemBePlacedHere(vector2) && !IsTileOccupiedBy(vector2) && !hasTileAt(num5, num6, "AlwaysFront") && !hasTileAt(num5, num6, "Front") && !isBehindBush(vector2) && (doesTileHaveProperty(num5, num6, "Diggable", "Back") != null || (GetSeason() == Season.Winter && doesTileHaveProperty(num5, num6, "Type", "Back") != null && doesTileHaveProperty(num5, num6, "Type", "Back").Equals("Grass"))))
			{
				if (name.Equals("Forest") && num5 >= 93 && num6 <= 22)
				{
					continue;
				}
				objects.Add(vector2, ItemRegistry.Create<Object>(random.NextBool(0.166) ? "(O)SeedSpot" : "(O)590"));
			}
			num4 *= 0.75;
			if (GetSeason() == Season.Winter)
			{
				num4 += 0.10000000149011612;
			}
		}
	}

	public void spawnWeedsAndStones(int numDebris = -1, bool weedsOnly = false, bool spawnFromOldWeeds = true)
	{
		if ((this is Farm || this is IslandWest) && Game1.IsBuildingConstructed("Gold Clock") && !Game1.netWorldState.Value.goldenClocksTurnedOff.Value)
		{
			return;
		}
		bool flag = false;
		if (this is Beach || GetSeason() == Season.Winter || this is Desert)
		{
			return;
		}
		int num = ((numDebris != -1) ? numDebris : ((Game1.random.NextDouble() < 0.95) ? ((Game1.random.NextDouble() < 0.25) ? Game1.random.Next(10, 21) : Game1.random.Next(5, 11)) : 0));
		if (IsRainingHere())
		{
			num *= 2;
		}
		if (Game1.dayOfMonth == 1)
		{
			num *= 5;
		}
		if (objects.Length <= 0 && spawnFromOldWeeds)
		{
			return;
		}
		if (!(this is Farm))
		{
			num /= 2;
		}
		bool flag2 = IsGreenRainingHere();
		for (int i = 0; i < num; i++)
		{
			Vector2 vector = (spawnFromOldWeeds ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : new Vector2(Game1.random.Next(map.Layers[0].LayerWidth), Game1.random.Next(map.Layers[0].LayerHeight)));
			if (!spawnFromOldWeeds && this is IslandWest)
			{
				vector = new Vector2(Game1.random.Next(57, 97), Game1.random.Next(44, 68));
			}
			while (spawnFromOldWeeds && vector.Equals(Vector2.Zero))
			{
				vector = new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			}
			Vector2 key = Vector2.Zero;
			Object value = null;
			if (spawnFromOldWeeds)
			{
				Utility.TryGetRandom(objects, out key, out value);
			}
			Vector2 vector2 = (spawnFromOldWeeds ? key : Vector2.Zero);
			if ((this is Mountain && vector.X + vector2.X > 100f) || this is IslandNorth)
			{
				continue;
			}
			bool num2 = this is Farm || this is IslandWest;
			int num3 = (int)(vector.X + vector2.X);
			int num4 = (int)(vector.Y + vector2.Y);
			Vector2 vector3 = vector + vector2;
			int value2 = 1;
			bool flag3 = false;
			bool flag4 = doesTileHaveProperty(num3, num4, "Diggable", "Back") != null;
			if (num2 == flag4 && !IsNoSpawnTile(vector3) && doesTileHaveProperty(num3, num4, "Type", "Back") != "Wood")
			{
				bool flag5 = false;
				if (CanItemBePlacedHere(vector3) && !terrainFeatures.ContainsKey(vector3))
				{
					flag5 = true;
				}
				else if (spawnFromOldWeeds)
				{
					if (objects.TryGetValue(vector3, out var value3))
					{
						if (flag2)
						{
							flag5 = false;
						}
						else if (!value3.IsTapper())
						{
							flag5 = true;
						}
					}
					if (!flag5 && terrainFeatures.TryGetValue(vector3, out var value4) && (value4 is HoeDirt || value4 is Flooring))
					{
						flag5 = !flag2 && getLargeTerrainFeatureAt(num3, num4) == null;
					}
				}
				if (flag5)
				{
					if (spawnFromOldWeeds)
					{
						flag3 = true;
					}
					else if (!objects.ContainsKey(vector3))
					{
						flag3 = true;
					}
				}
			}
			if (!flag3)
			{
				continue;
			}
			string text = null;
			if (this is Desert)
			{
				text = "(O)750";
			}
			else
			{
				if (Game1.random.NextBool() && !weedsOnly && (!spawnFromOldWeeds || value.IsBreakableStone() || value.IsTwig()))
				{
					text = Game1.random.Choose("(O)294", "(O)295", "(O)343", "(O)450");
				}
				else if (!spawnFromOldWeeds || value.IsWeeds())
				{
					text = getWeedForSeason(Game1.random, GetSeason());
					if (IsGreenRainingHere())
					{
						if (doesTileHavePropertyNoNull((int)(vector.X + vector2.X), (int)(vector.Y + vector2.Y), "Type", "Back") == (IsFarm ? "Dirt" : "Grass"))
						{
							int num5 = Game1.random.Next(8);
							text = "(O)GreenRainWeeds" + num5;
							if (num5 == 2 || num5 == 3 || num5 == 7)
							{
								value2 = 2;
							}
						}
						else
						{
							text = null;
						}
					}
				}
				if (this is Farm && !spawnFromOldWeeds && Game1.random.NextDouble() < 0.05 && !terrainFeatures.ContainsKey(vector3))
				{
					terrainFeatures.Add(vector3, new Tree((Game1.random.Next(3) + 1).ToString(), Game1.random.Next(3)));
					continue;
				}
			}
			if (text == null)
			{
				continue;
			}
			bool flag6 = false;
			if (objects.TryGetValue(vector + vector2, out var value5))
			{
				if (flag2 || value5 is Fence || value5 is Chest || value5.QualifiedItemId == "(O)590" || value5.QualifiedItemId == "(BC)MushroomLog")
				{
					continue;
				}
				if (value5.name.Length > 0 && value5.Category != -999)
				{
					flag6 = true;
					Game1.debugOutput = value5.Name + " was destroyed";
				}
				objects.Remove(vector + vector2);
			}
			if (terrainFeatures.TryGetValue(vector + vector2, out var value6))
			{
				try
				{
					flag6 = value6 is HoeDirt || value6 is Flooring;
				}
				catch (Exception)
				{
				}
				if (!flag6 || IsGreenRainingHere())
				{
					break;
				}
				terrainFeatures.Remove(vector + vector2);
			}
			if (flag6 && this is Farm && Game1.stats.DaysPlayed > 1 && !flag)
			{
				flag = true;
				Game1.multiplayer.broadcastGlobalMessage("Strings\\Locations:Farm_WeedsDestruction", false, null);
			}
			Object obj = ItemRegistry.Create<Object>(text);
			obj.minutesUntilReady.Value = value2;
			objects.TryAdd(vector + vector2, obj);
		}
	}

	[Obsolete("Use removeObjectsAndSpawned instead.")]
	public virtual void removeEverythingExceptCharactersFromThisTile(int x, int y)
	{
		removeObjectsAndSpawned(x, y, 1, 1);
	}

	/// <summary>Remove all objects, bushes, resource clumps, and terrain features within an area.</summary>
	/// <param name="x">The top-left X position of the area to clear.</param>
	/// <param name="y">The top-right X position of the area to clear.</param>
	/// <param name="width">The width of the area to clear.</param>
	/// <param name="height">The height of the area to clear.</param>
	public virtual void removeObjectsAndSpawned(int x, int y, int width, int height)
	{
		Microsoft.Xna.Framework.Rectangle pixelArea = new Microsoft.Xna.Framework.Rectangle(x * 64, y * 64, width * 64, height * 64);
		int num = x + width - 1;
		int num2 = y + height - 1;
		for (int i = y; i <= num2; i++)
		{
			for (int j = x; j <= num; j++)
			{
				Vector2 key = new Vector2(j, i);
				terrainFeatures.Remove(key);
				objects.Remove(key);
			}
		}
		largeTerrainFeatures.RemoveWhere((LargeTerrainFeature feature) => feature.getBoundingBox().Intersects(pixelArea));
		resourceClumps.RemoveWhere((ResourceClump clump) => clump.getBoundingBox().Intersects(pixelArea));
	}

	public virtual string getFootstepSoundReplacement(string footstep)
	{
		return footstep;
	}

	public virtual void removeEverythingFromThisTile(int x, int y)
	{
		Vector2 tile = new Vector2(x, y);
		Point pixel = Utility.Vector2ToPoint(tile * 64f + new Vector2(32f));
		resourceClumps.RemoveWhere((ResourceClump clump) => clump.Tile == tile);
		terrainFeatures.Remove(tile);
		objects.Remove(tile);
		furniture.RemoveWhere((Furniture f) => f.GetBoundingBox().Contains(pixel));
		characters.RemoveWhere((NPC npc) => npc.Tile == tile && npc is Monster);
	}

	public virtual bool TryGetLocationEvents(out string assetName, out Dictionary<string, string> events)
	{
		events = null;
		assetName = ((NameOrUniqueName == Game1.player.homeLocation.Value) ? "Data\\Events\\FarmHouse" : ("Data\\Events\\" + name.Value));
		try
		{
			if (Game1.content.DoesAssetExist<Dictionary<string, string>>(assetName))
			{
				events = Game1.content.Load<Dictionary<string, string>>(assetName);
			}
		}
		catch (Exception exception)
		{
			Game1.log.Error($"Failed loading events for location '{NameOrUniqueName}' from asset '{assetName}'.", exception);
		}
		if (events == null)
		{
			events = new Dictionary<string, string>();
		}
		if (assetName != "Data\\Events\\FarmHouse")
		{
			foreach (KeyValuePair<string, string> item in Game1.content.Load<Dictionary<string, string>>("Data\\Events\\FarmHouse"))
			{
				if (item.Key.StartsWith("558291/") || item.Key.StartsWith("558292/"))
				{
					events.TryAdd(item.Key, item.Value);
				}
			}
		}
		if (Name == "Trailer_Big")
		{
			events = new Dictionary<string, string>(events);
			Dictionary<string, string> dictionary = Game1.content.Load<Dictionary<string, string>>("Data\\Events\\Trailer");
			if (dictionary != null)
			{
				foreach (string key in dictionary.Keys)
				{
					string text = dictionary[key];
					if (!(name.Value == "Trailer_Big") || !events.ContainsKey(key))
					{
						if (key.StartsWith("36/"))
						{
							text = text.Replace("/farmer -30 30 0", "/farmer 12 19 0");
							text = text.Replace("/playSound doorClose/warp farmer 12 9", "/move farmer 0 -10 0");
						}
						else if (key.StartsWith("35/"))
						{
							text = text.Replace("/farmer -30 30 0", "/farmer 12 19 0");
							text = text.Replace("/warp farmer 12 9/playSound doorClose", "/move farmer 0 -10 0");
							text = text.Replace("/warp farmer -40 -40/playSound doorClose", "/move farmer 0 10 0/warp farmer -40 -40");
						}
						events[key] = text;
					}
				}
			}
		}
		return events.Count > 0;
	}

	public static bool IsValidLocationEvent(string key, string eventScript)
	{
		if (!key.Contains('/') && !int.TryParse(key, out var _))
		{
			return false;
		}
		string[] array = Event.ParseCommands(eventScript);
		if (array.Length < 3)
		{
			return false;
		}
		string text = array[1];
		if (text.Length == 0 || (text != "follow" && !char.IsDigit(text[0]) && text[0] != '-'))
		{
			return false;
		}
		return true;
	}

	public virtual void checkForEvents()
	{
		if (Game1.killScreen && !Game1.eventUp)
		{
			if (Game1.player.bathingClothes.Value)
			{
				Game1.player.changeOutOfSwimSuit();
			}
			if (name.Equals("Mine"))
			{
				string sub;
				string sub2;
				switch (Game1.random.Next(7))
				{
				case 0:
					sub = "Robin";
					sub2 = "Data\\ExtraDialogue:Mines_PlayerKilled_Robin";
					break;
				case 1:
					sub = "Clint";
					sub2 = "Data\\ExtraDialogue:Mines_PlayerKilled_Clint";
					break;
				case 2:
					sub = "Maru";
					sub2 = ((Game1.player.spouse == "Maru") ? "Data\\ExtraDialogue:Mines_PlayerKilled_Maru_Spouse" : "Data\\ExtraDialogue:Mines_PlayerKilled_Maru_NotSpouse");
					break;
				default:
					sub = "Linus";
					sub2 = "Data\\ExtraDialogue:Mines_PlayerKilled_Linus";
					break;
				}
				if (Game1.random.NextDouble() < 0.1 && Game1.player.spouse != null && !Game1.player.isEngaged() && Game1.player.spouse.Length > 1)
				{
					sub = Game1.player.spouse;
					sub2 = (Game1.player.IsMale ? "Data\\ExtraDialogue:Mines_PlayerKilled_Spouse_PlayerMale" : "Data\\ExtraDialogue:Mines_PlayerKilled_Spouse_PlayerFemale");
				}
				currentEvent = new Event(Game1.content.LoadString("Data\\Events\\Mine:PlayerKilled", sub, sub2, ArgUtility.EscapeQuotes(Game1.player.Name)));
			}
			else if (this is IslandLocation)
			{
				string sub3 = "Willy";
				string sub4 = "Data\\ExtraDialogue:Island_willy_rescue";
				if (Game1.player.friendshipData.ContainsKey("Leo") && Game1.random.NextBool())
				{
					sub3 = "Leo";
					sub4 = "Data\\ExtraDialogue:Island_leo_rescue";
				}
				currentEvent = new Event(Game1.content.LoadString("Data\\Events\\IslandSouth:PlayerKilled", sub3, sub4, ArgUtility.EscapeQuotes(Game1.player.Name)));
			}
			else if (name.Equals("Hospital"))
			{
				currentEvent = new Event(Game1.content.LoadString("Data\\Events\\Hospital:PlayerKilled", ArgUtility.EscapeQuotes(Game1.player.Name)));
			}
			else
			{
				try
				{
					if (TryGetLocationEvents(out var assetName, out var events) && events.TryGetValue("PlayerKilled", out var value))
					{
						currentEvent = new Event(value, assetName, "PlayerKilled");
					}
				}
				catch (Exception)
				{
				}
			}
			if (currentEvent != null)
			{
				Game1.eventUp = true;
			}
			Game1.changeMusicTrack("none", track_interruptable: true);
			Game1.killScreen = false;
			Game1.player.health = 10;
		}
		else if (!Game1.eventUp && Game1.weddingsToday.Count > 0 && (Game1.CurrentEvent == null || Game1.CurrentEvent.id != "-2") && Game1.currentLocation != null && !Game1.currentLocation.IsTemporary)
		{
			currentEvent = Game1.getAvailableWeddingEvent();
			if (currentEvent != null)
			{
				startEvent(currentEvent);
			}
		}
		else
		{
			if (Game1.eventUp || Game1.farmEvent != null)
			{
				return;
			}
			string festival = $"{Game1.currentSeason}{Game1.dayOfMonth}";
			try
			{
				if (Event.tryToLoadFestival(festival, out var ev))
				{
					currentEvent = ev;
				}
			}
			catch (Exception)
			{
			}
			if (!Game1.eventUp && currentEvent == null && Game1.farmEvent == null && !IsGreenRainingHere())
			{
				string assetName2;
				Dictionary<string, string> events2;
				try
				{
					if (!TryGetLocationEvents(out assetName2, out events2))
					{
						return;
					}
				}
				catch
				{
					return;
				}
				if (events2 != null)
				{
					foreach (string key in events2.Keys)
					{
						string text = checkEventPrecondition(key);
						if (!string.IsNullOrEmpty(text) && text != "-1" && IsValidLocationEvent(key, events2[key]))
						{
							currentEvent = new Event(events2[key], assetName2, text);
							break;
						}
					}
					if (currentEvent == null && Game1.IsMasterGame && Game1.stats.DaysPlayed >= 20 && !Game1.player.mailReceived.Contains("rejectedPet") && !Game1.player.hasPet() && Pet.TryGetData(Game1.player.whichPetType, out var data) && Name == data.AdoptionEventLocation && !string.IsNullOrWhiteSpace(data.AdoptionEventId) && !Game1.player.eventsSeen.Contains(data.AdoptionEventId))
					{
						Game1.PlayEvent(data.AdoptionEventId, checkPreconditions: false, checkSeen: false);
					}
				}
			}
			if (currentEvent != null)
			{
				startEvent(currentEvent);
			}
		}
	}

	public Event findEventById(string id, Farmer farmerActor = null)
	{
		if (id == "-2")
		{
			long? spouse = Game1.player.team.GetSpouse(farmerActor.UniqueMultiplayerID);
			if (farmerActor == null || !spouse.HasValue)
			{
				return Utility.getWeddingEvent(farmerActor);
			}
			if (Game1.otherFarmers.ContainsKey(spouse.Value))
			{
				return Utility.getWeddingEvent(farmerActor);
			}
		}
		string assetName;
		Dictionary<string, string> events;
		try
		{
			if (!TryGetLocationEvents(out assetName, out events))
			{
				return null;
			}
		}
		catch
		{
			return null;
		}
		foreach (KeyValuePair<string, string> item in events)
		{
			if (Event.SplitPreconditions(item.Key)[0] == id)
			{
				return new Event(item.Value, assetName, id, farmerActor);
			}
		}
		return null;
	}

	public virtual void startEvent(Event evt)
	{
		if (Game1.eventUp || Game1.eventOver)
		{
			return;
		}
		currentEvent = evt;
		ResetForEvent(evt);
		if (evt.exitLocation == null)
		{
			evt.exitLocation = Game1.getLocationRequest(NameOrUniqueName, isStructure.Value);
		}
		if (Game1.player.mount != null)
		{
			Horse mount = Game1.player.mount;
			mount.currentLocation = this;
			mount.dismount();
			Microsoft.Xna.Framework.Rectangle boundingBox = mount.GetBoundingBox();
			Vector2 position = mount.Position;
			if (mount.currentLocation != null && mount.currentLocation.isCollidingPosition(boundingBox, Game1.viewport, isFarmer: false, 0, glider: false, mount, pathfinding: true))
			{
				boundingBox.X -= 64;
				if (!mount.currentLocation.isCollidingPosition(boundingBox, Game1.viewport, isFarmer: false, 0, glider: false, mount, pathfinding: true))
				{
					position.X -= 64f;
					mount.Position = position;
				}
				else
				{
					boundingBox.X += 128;
					if (!mount.currentLocation.isCollidingPosition(boundingBox, Game1.viewport, isFarmer: false, 0, glider: false, mount, pathfinding: true))
					{
						position.X += 64f;
						mount.Position = position;
					}
				}
			}
		}
		foreach (NPC character in characters)
		{
			character.clearTextAboveHead();
		}
		Game1.eventUp = true;
		Game1.displayHUD = false;
		Game1.player.CanMove = false;
		Game1.player.showNotCarrying();
		critters?.Clear();
		if (currentEvent != null)
		{
			Game1.player.autoGenerateActiveDialogueEvent("eventSeen_" + currentEvent.id);
		}
	}

	public virtual void drawBackground(SpriteBatch b)
	{
	}

	public virtual void drawWater(SpriteBatch b)
	{
		currentEvent?.drawUnderWater(b);
		if (waterTiles == null)
		{
			return;
		}
		for (int i = Math.Max(0, Game1.viewport.Y / 64 - 1); i < Math.Min(map.Layers[0].LayerHeight, (Game1.viewport.Y + Game1.viewport.Height) / 64 + 2); i++)
		{
			for (int j = Math.Max(0, Game1.viewport.X / 64 - 1); j < Math.Min(map.Layers[0].LayerWidth, (Game1.viewport.X + Game1.viewport.Width) / 64 + 1); j++)
			{
				if (waterTiles.waterTiles[j, i].isWater && waterTiles.waterTiles[j, i].isVisible)
				{
					drawWaterTile(b, j, i);
				}
			}
		}
	}

	public virtual void drawWaterTile(SpriteBatch b, int x, int y)
	{
		drawWaterTile(b, x, y, waterColor.Value);
	}

	public void drawWaterTile(SpriteBatch b, int x, int y, Color color)
	{
		bool num = y == map.Layers[0].LayerHeight - 1 || !waterTiles[x, y + 1];
		bool flag = y == 0 || !waterTiles[x, y - 1];
		b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - (int)((!flag) ? waterPosition : 0f))), new Microsoft.Xna.Framework.Rectangle(waterAnimationIndex * 64, 2064 + (((x + y) % 2 != 0) ? ((!waterTileFlip) ? 128 : 0) : (waterTileFlip ? 128 : 0)) + (flag ? ((int)waterPosition) : 0), 64, 64 + (flag ? ((int)(0f - waterPosition)) : 0)), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.56f);
		if (num)
		{
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (y + 1) * 64 - (int)waterPosition)), new Microsoft.Xna.Framework.Rectangle(waterAnimationIndex * 64, 2064 + (((x + (y + 1)) % 2 != 0) ? ((!waterTileFlip) ? 128 : 0) : (waterTileFlip ? 128 : 0)), 64, 64 - (int)(64f - waterPosition) - 1), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.56f);
		}
	}

	public virtual void drawFloorDecorations(SpriteBatch b)
	{
		int num = 1;
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(Game1.viewport.X / 64 - num, Game1.viewport.Y / 64 - num, (int)Math.Ceiling((float)Game1.viewport.Width / 64f) + 2 * num, (int)Math.Ceiling((float)Game1.viewport.Height / 64f) + 3 + 2 * num);
		Microsoft.Xna.Framework.Rectangle rectangle = default(Microsoft.Xna.Framework.Rectangle);
		if (buildings.Count > 0)
		{
			foreach (Building building in buildings)
			{
				int additionalTilePropertyRadius = building.GetAdditionalTilePropertyRadius();
				Microsoft.Xna.Framework.Rectangle sourceRect = building.getSourceRect();
				rectangle.X = building.tileX.Value - additionalTilePropertyRadius;
				rectangle.Width = building.tilesWide.Value + additionalTilePropertyRadius * 2;
				int num2 = building.tileY.Value + building.tilesHigh.Value + additionalTilePropertyRadius;
				rectangle.Height = num2 - (rectangle.Y = num2 - (int)Math.Ceiling((float)sourceRect.Height * 4f / 64f) - additionalTilePropertyRadius);
				if (rectangle.Intersects(value))
				{
					building.drawBackground(b);
				}
			}
		}
		if (!Game1.isFestival() && terrainFeatures.Length > 0)
		{
			Vector2 key = default(Vector2);
			for (int i = Game1.viewport.Y / 64 - 1; i < (Game1.viewport.Y + Game1.viewport.Height) / 64 + 7; i++)
			{
				for (int j = Game1.viewport.X / 64 - 1; j < (Game1.viewport.X + Game1.viewport.Width) / 64 + 3; j++)
				{
					key.X = j;
					key.Y = i;
					if (terrainFeatures.TryGetValue(key, out var value2) && value2 is Flooring)
					{
						value2.draw(b);
					}
				}
			}
		}
		if (Game1.eventUp && !(this is Farm) && !(this is FarmHouse))
		{
			return;
		}
		Furniture.isDrawingLocationFurniture = true;
		foreach (Furniture item in furniture)
		{
			if (item.furniture_type.Value == 12)
			{
				item.draw(b, -1, -1);
			}
		}
		Furniture.isDrawingLocationFurniture = false;
	}

	public TemporaryAnimatedSprite getTemporarySpriteByID(int id)
	{
		for (int i = 0; i < temporarySprites.Count; i++)
		{
			if (temporarySprites[i].id == id)
			{
				return temporarySprites[i];
			}
		}
		return null;
	}

	protected void drawDebris(SpriteBatch b)
	{
		int num = 0;
		foreach (Debris debri in debris)
		{
			num++;
			if (debri.item != null)
			{
				Vector2 visualPosition = debri.Chunks[0].GetVisualPosition();
				if (debri.item is Object obj && obj.bigCraftable.Value)
				{
					obj.drawInMenu(b, Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, visualPosition + new Vector2(32f, 32f))), 1.6f, 1f, ((float)(debri.chunkFinalYLevel + 64 + 8) + visualPosition.X / 10000f) / 10000f, StackDrawType.Hide, Color.White, drawShadow: true);
				}
				else
				{
					debri.item.drawInMenu(b, Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, visualPosition + new Vector2(32f, 32f))), 0.8f + (float)debri.itemQuality * 0.1f, 1f, ((float)(debri.chunkFinalYLevel + 64 + 8) + visualPosition.X / 10000f) / 10000f, StackDrawType.Hide, Color.White, drawShadow: true);
				}
				continue;
			}
			switch (debri.debrisType.Value)
			{
			case Debris.DebrisType.LETTERS:
			{
				Chunk chunk = debri.Chunks[0];
				Vector2 visualPosition2 = chunk.GetVisualPosition();
				Game1.drawWithBorder(debri.debrisMessage.Value, Color.Black, debri.nonSpriteChunkColor.Value, Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, visualPosition2)), chunk.rotation, chunk.scale, (visualPosition2.Y + 64f) / 10000f);
				continue;
			}
			case Debris.DebrisType.NUMBERS:
			{
				Chunk chunk2 = debri.Chunks[0];
				Vector2 visualPosition3 = chunk2.GetVisualPosition();
				NumberSprite.draw(debri.chunkType.Value, b, Game1.GlobalToLocal(Game1.viewport, Utility.snapDrawPosition(new Vector2(visualPosition3.X, (float)debri.chunkFinalYLevel - ((float)debri.chunkFinalYLevel - visualPosition3.Y)))), debri.nonSpriteChunkColor.Value, chunk2.scale * 0.75f, 0.98f + 0.0001f * (float)num, chunk2.alpha, -1 * (int)((float)debri.chunkFinalYLevel - visualPosition3.Y) / 2);
				continue;
			}
			case Debris.DebrisType.SPRITECHUNKS:
			{
				for (int i = 0; i < debri.Chunks.Count; i++)
				{
					Chunk chunk3 = debri.Chunks[0];
					Vector2 visualPosition4 = chunk3.GetVisualPosition();
					b.Draw(debri.spriteChunkSheet, Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, visualPosition4)), new Microsoft.Xna.Framework.Rectangle(chunk3.xSpriteSheet.Value, chunk3.ySpriteSheet.Value, Math.Min(debri.sizeOfSourceRectSquares.Value, debri.spriteChunkSheet.Bounds.Width), Math.Min(debri.sizeOfSourceRectSquares.Value, debri.spriteChunkSheet.Bounds.Height)), debri.nonSpriteChunkColor.Value * chunk3.alpha, chunk3.rotation, new Vector2(debri.sizeOfSourceRectSquares.Value / 2, debri.sizeOfSourceRectSquares.Value / 2), chunk3.scale, SpriteEffects.None, ((float)(debri.chunkFinalYLevel + 16) + visualPosition4.X / 10000f) / 10000f);
				}
				continue;
			}
			}
			if (debri.itemId.Value != null)
			{
				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(debri.itemId.Value);
				Texture2D texture = dataOrErrorItem.GetTexture();
				Microsoft.Xna.Framework.Rectangle value = dataOrErrorItem.GetSourceRect();
				float scale = ((debri.debrisType.Value == Debris.DebrisType.RESOURCE || debri.floppingFish.Value) ? 4f : (4f * (0.8f + (float)debri.itemQuality * 0.1f)));

				if (StardewValley.Pokemon.PokemonItemManager.TryGetCustomTexture(debri.itemId.Value, out Texture2D customTex, out Microsoft.Xna.Framework.Rectangle customRect))
				{
					texture = customTex;
					value = customRect;
					scale = 2.5f;
				}

				for (int j = 0; j < debri.Chunks.Count; j++)
				{
					Chunk chunk4 = debri.Chunks[j];
					Vector2 visualPosition5 = chunk4.GetVisualPosition();
					if (texture != customTex)
					{
						value = ((debri.debrisType.Value == Debris.DebrisType.RESOURCE) ? dataOrErrorItem.GetSourceRect(chunk4.randomOffset) : dataOrErrorItem.GetSourceRect());
					}
					SpriteEffects effects = ((debri.floppingFish.Value && chunk4.bounces % 2 == 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
					b.Draw(texture, Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, visualPosition5)), value, Color.White, 0f, Vector2.Zero, scale, effects, ((float)(debri.chunkFinalYLevel + 32) + visualPosition5.X / 10000f) / 10000f);
					b.Draw(Game1.shadowTexture, Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, new Vector2(visualPosition5.X + 25.6f, (debri.chunksMoveTowardPlayer ? (visualPosition5.Y + 8f) : ((float)debri.chunkFinalYLevel)) + 32f + (float)(12 * debri.itemQuality)))), Game1.shadowTexture.Bounds, Color.White * 0.75f, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), Math.Min(3f, 3f - (debri.chunksMoveTowardPlayer ? 0f : (((float)debri.chunkFinalYLevel - visualPosition5.Y) / 96f))), SpriteEffects.None, (float)debri.chunkFinalYLevel / 10000f);
				}
			}
			else
			{
				for (int k = 0; k < debri.Chunks.Count; k++)
				{
					Vector2 position = Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, debri.Chunks[k].position.Value));
					Microsoft.Xna.Framework.Rectangle sourceRectForStandardTileSheet = Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, debri.chunkType.Value + debri.Chunks[k].randomOffset, 16, 16);
					float layerDepth = (debri.Chunks[k].position.Y + 128f + debri.Chunks[k].position.X / 10000f) / 10000f;
					b.Draw(Game1.debrisSpriteSheet, position, sourceRectForStandardTileSheet, debri.chunksColor.Value, 0f, Vector2.Zero, 4f * debri.scale.Value, SpriteEffects.None, layerDepth);
				}
			}
		}
	}

	public virtual bool shouldHideCharacters()
	{
		return false;
	}

	protected virtual void drawCharacters(SpriteBatch b)
	{
		if (shouldHideCharacters() || (Game1.eventUp && (Game1.CurrentEvent == null || !Game1.CurrentEvent.showWorldCharacters)))
		{
			return;
		}
		for (int i = 0; i < characters.Count; i++)
		{
			if (characters[i] != null)
			{
				characters[i].draw(b);
			}
		}
	}

	protected virtual void drawFarmers(SpriteBatch b)
	{
		if (shouldHideCharacters() || Game1.currentMinigame != null)
		{
			return;
		}
		if (currentEvent == null || currentEvent.isFestival || currentEvent.farmerActors.Count == 0)
		{
			foreach (Farmer farmer in farmers)
			{
				if (!Game1.multiplayer.isDisconnecting(farmer.UniqueMultiplayerID))
				{
					farmer.draw(b);
				}
			}
			return;
		}
		currentEvent.drawFarmers(b);
	}

	public virtual void DrawFarmerUsernames(SpriteBatch b)
	{
		if (shouldHideCharacters() || Game1.currentMinigame != null || (currentEvent != null && !currentEvent.isFestival && currentEvent.farmerActors.Count != 0))
		{
			return;
		}
		foreach (Farmer farmer in farmers)
		{
			if (!Game1.multiplayer.isDisconnecting(farmer.UniqueMultiplayerID))
			{
				farmer.DrawUsername(b);
			}
		}
	}

	public virtual void draw(SpriteBatch b)
	{
		if (animals.Length > 0)
		{
			foreach (FarmAnimal value3 in animals.Values)
			{
				value3.draw(b);
			}
		}
		if (mapSeats.Count > 0)
		{
			foreach (MapSeat mapSeat in mapSeats)
			{
				mapSeat.Draw(b);
			}
		}
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(Game1.viewport.X, Game1.viewport.Y, Game1.viewport.Width, Game1.viewport.Height);
		value.Inflate(128, 128);
		if (this is Woods && Game1.eventUp)
		{
			Event obj = currentEvent;
			if (obj == null || !obj.showGroundObjects)
			{
				goto IL_014d;
			}
		}
		if (resourceClumps.Count > 0)
		{
			foreach (ResourceClump resourceClump in resourceClumps)
			{
				if (resourceClump.getRenderBounds().Intersects(value))
				{
					resourceClump.draw(b);
				}
			}
		}
		goto IL_014d;
		IL_014d:
		_currentLocationFarmersForDisambiguating.Clear();
		foreach (Farmer farmer3 in farmers)
		{
			farmer3.drawLayerDisambiguator = 0f;
			_currentLocationFarmersForDisambiguating.Add(farmer3);
		}
		if (_currentLocationFarmersForDisambiguating.Contains(Game1.player))
		{
			_currentLocationFarmersForDisambiguating.Remove(Game1.player);
			_currentLocationFarmersForDisambiguating.Insert(0, Game1.player);
		}
		float num = 0.0001f;
		for (int i = 0; i < _currentLocationFarmersForDisambiguating.Count; i++)
		{
			for (int j = i + 1; j < _currentLocationFarmersForDisambiguating.Count; j++)
			{
				Farmer farmer = _currentLocationFarmersForDisambiguating[i];
				Farmer farmer2 = _currentLocationFarmersForDisambiguating[j];
				if (!farmer2.IsSitting() && Math.Abs(farmer.getDrawLayer() - farmer2.getDrawLayer()) < num && Math.Abs(farmer.position.X - farmer2.position.X) < 64f)
				{
					farmer2.drawLayerDisambiguator += farmer.getDrawLayer() - num - farmer2.getDrawLayer();
				}
			}
		}
		drawCharacters(b);
		drawFarmers(b);
		if (Game1.player != null && Game1.player.currentLocation == this)
		{
			StardewValley.Pokemon.PokemonFollower.Instance.Draw(b);
		}
		if (critters != null && Game1.farmEvent == null)
		{
			for (int k = 0; k < critters.Count; k++)
			{
				critters[k].draw(b);
			}
		}
		drawDebris(b);
		if ((!Game1.eventUp || (currentEvent != null && currentEvent.showGroundObjects)) && objects.Length > 0)
		{
			Vector2 key = default(Vector2);
			for (int l = Game1.viewport.Y / 64 - 1; l < (Game1.viewport.Y + Game1.viewport.Height) / 64 + 3; l++)
			{
				for (int m = Game1.viewport.X / 64 - 1; m < (Game1.viewport.X + Game1.viewport.Width) / 64 + 1; m++)
				{
					key.X = m;
					key.Y = l;
					if (objects.TryGetValue(key, out var value2))
					{
						value2.draw(b, (int)key.X, (int)key.Y);
					}
				}
			}
		}
		if (TemporarySprites.Count > 0)
		{
			foreach (TemporaryAnimatedSprite temporarySprite in TemporarySprites)
			{
				if (!temporarySprite.drawAboveAlwaysFront)
				{
					temporarySprite.draw(b);
				}
			}
		}
		interiorDoors.Draw(b);
		NetCollection<LargeTerrainFeature> netCollection = largeTerrainFeatures;
		if (netCollection != null && netCollection.Count > 0)
		{
			foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
			{
				if (largeTerrainFeature.getRenderBounds().Intersects(value))
				{
					largeTerrainFeature.draw(b);
				}
			}
		}
		if (buildings.Count > 0)
		{
			int num2 = 1;
			value = new Microsoft.Xna.Framework.Rectangle(Game1.viewport.X / 64 - num2, Game1.viewport.Y / 64 - num2, (int)Math.Ceiling((float)Game1.viewport.Width / 64f) + 2 * num2, (int)Math.Ceiling((float)Game1.viewport.Height / 64f) + 3 + 2 * num2);
			Microsoft.Xna.Framework.Rectangle rectangle = default(Microsoft.Xna.Framework.Rectangle);
			foreach (Building building in buildings)
			{
				int additionalTilePropertyRadius = building.GetAdditionalTilePropertyRadius();
				Microsoft.Xna.Framework.Rectangle sourceRect = building.getSourceRect();
				rectangle.X = building.tileX.Value - additionalTilePropertyRadius;
				rectangle.Width = building.tilesWide.Value + additionalTilePropertyRadius * 2;
				int num3 = building.tileY.Value + building.tilesHigh.Value + additionalTilePropertyRadius;
				rectangle.Height = num3 - (rectangle.Y = num3 - (int)Math.Ceiling((float)sourceRect.Height * 4f / 64f) - additionalTilePropertyRadius);
				if (rectangle.Intersects(value))
				{
					building.draw(b);
				}
			}
		}
		fishSplashAnimation?.draw(b);
		orePanAnimation?.draw(b);
		if (!Game1.eventUp || this is Farm || this is FarmHouse)
		{
			Furniture.isDrawingLocationFurniture = true;
			foreach (Furniture item in furniture)
			{
				if (item.furniture_type.Value != 12)
				{
					item.draw(b, -1, -1);
				}
			}
			Furniture.isDrawingLocationFurniture = false;
		}
		if (showDropboxIndicator && !Game1.eventUp)
		{
			float num4 = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			b.Draw(Game1.mouseCursors2, Game1.GlobalToLocal(Game1.viewport, new Vector2(dropBoxIndicatorLocation.X, dropBoxIndicatorLocation.Y + num4)), new Microsoft.Xna.Framework.Rectangle(114, 53, 6, 10), Color.White, 0f, new Vector2(1f, 4f), 4f, SpriteEffects.None, 1f);
		}
		if (lightGlows.Count > 0)
		{
			drawLightGlows(b);
		}
	}

	public virtual void drawOverlays(SpriteBatch b)
	{
	}

	public virtual void drawAboveFrontLayer(SpriteBatch b)
	{
		Vector2 key = default(Vector2);
		for (int i = Game1.viewport.Y / 64 - 1; i < (Game1.viewport.Y + Game1.viewport.Height) / 64 + 7; i++)
		{
			for (int j = Game1.viewport.X / 64 - 1; j < (Game1.viewport.X + Game1.viewport.Width) / 64 + 3; j++)
			{
				key.X = j;
				key.Y = i;
				if (terrainFeatures.TryGetValue(key, out var value) && !(value is Flooring))
				{
					value.draw(b);
				}
			}
		}
	}

	public virtual void drawLightGlows(SpriteBatch b)
	{
		foreach (Vector2 lightGlow in lightGlows)
		{
			if (!lightGlowLayerCache.ContainsKey(lightGlow))
			{
				Furniture furnitureAt = GetFurnitureAt(new Vector2((int)(lightGlow.X / 64f), (int)(lightGlow.Y / 64f) + 2));
				if (furnitureAt != null && furnitureAt.sourceRect.Height / 16 - furnitureAt.getTilesHigh() > 1)
				{
					lightGlowLayerCache.Add(lightGlow, 2.5f);
				}
				else if (this is FarmHouse { upgradeLevel: >0 } farmHouse)
				{
					Vector2 vector = new Vector2((int)(lightGlow.X / 64f), (int)(lightGlow.Y / 64f));
					Vector2 vector2 = Utility.PointToVector2(farmHouse.getKitchenStandingSpot()) - vector;
					if (vector2.Y == 3f && (vector2.X == 2f || vector2.X == 3f || vector2.X == -1f || vector2.X == -2f))
					{
						lightGlowLayerCache.Add(lightGlow, 1.5f);
					}
					else
					{
						lightGlowLayerCache.Add(lightGlow, 10f);
					}
				}
				else
				{
					lightGlowLayerCache.Add(lightGlow, 10f);
				}
			}
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, lightGlow), new Microsoft.Xna.Framework.Rectangle(21, 1695, 41, 67), Color.White, 0f, new Vector2(19f, 22f), 4f, SpriteEffects.None, (lightGlow.Y + 64f * lightGlowLayerCache[lightGlow]) / 10000f);
		}
	}

	/// <summary>Try to create a secret note or journal scrap that hasn't been seen by a player, based on the random spawn chance.</summary>
	/// <param name="who">The farmer for whom to create a secret note.</param>
	/// <returns>Returns an unseen secret note/journal scrap, or <see langworld="null" /> if there are none left or the random spawn chance fails.</returns>
	public Object tryToCreateUnseenSecretNote(Farmer who)
	{
		if (currentEvent != null && currentEvent.isFestival)
		{
			return null;
		}
		bool flag = InIslandContext();
		if (!flag && (who == null || !who.hasMagnifyingGlass))
		{
			return null;
		}
		string itemId = (flag ? "(O)842" : "(O)79");
		int totalNotes;
		int num = Utility.GetUnseenSecretNotes(who, flag, out totalNotes).Length - who.Items.CountId(itemId);
		if (num <= 0)
		{
			return null;
		}
		float num2 = (float)(num - 1) / (float)Math.Max(1, totalNotes - 1);
		float chance = LAST_SECRET_NOTE_CHANCE + (FIRST_SECRET_NOTE_CHANCE - LAST_SECRET_NOTE_CHANCE) * num2;
		if (!Game1.random.NextBool(chance))
		{
			return null;
		}
		return ItemRegistry.Create<Object>(itemId);
	}

	public virtual bool performToolAction(Tool t, int tileX, int tileY)
	{
		if (t is MeleeWeapon meleeWeapon)
		{
			foreach (FarmAnimal value2 in animals.Values)
			{
				if (value2.GetBoundingBox().Intersects(meleeWeapon.mostRecentArea))
				{
					value2.hitWithWeapon(meleeWeapon);
				}
			}
		}
		foreach (Building building in buildings)
		{
			if (building.occupiesTile(new Vector2(tileX, tileY)))
			{
				building.performToolAction(t, tileX, tileY);
			}
		}
		for (int num = resourceClumps.Count - 1; num >= 0; num--)
		{
			if (resourceClumps[num] != null && resourceClumps[num].getBoundingBox().Contains(tileX * 64, tileY * 64) && resourceClumps[num].performToolAction(t, 1, resourceClumps[num].Tile))
			{
				resourceClumps.RemoveAt(num);
				return true;
			}
		}
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(tileX * 64, tileY * 64, 64, 64);
		foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
		{
			if (largeTerrainFeature.getBoundingBox().Intersects(value))
			{
				largeTerrainFeature.performToolAction(t, 1, new Vector2(tileX, tileY));
			}
		}
		return false;
	}

	/// <summary>Update the location when the season changes.</summary>
	/// <param name="onLoad">Whether the season is being initialized as part of loading the save, instead of an actual in-game season change.</param>
	public virtual void seasonUpdate(bool onLoad = false)
	{
		Season season = GetSeason();
		terrainFeatures.RemoveWhere((KeyValuePair<Vector2, TerrainFeature> pair) => pair.Value.seasonUpdate(onLoad));
		largeTerrainFeatures?.RemoveWhere((LargeTerrainFeature feature) => feature.seasonUpdate(onLoad));
		foreach (NPC character in characters)
		{
			if (!character.IsMonster)
			{
				character.resetSeasonalDialogue();
			}
		}
		if (IsOutdoors && !onLoad)
		{
			KeyValuePair<Vector2, Object>[] array = objects.Pairs.ToArray();
			for (int num = 0; num < array.Length; num++)
			{
				KeyValuePair<Vector2, Object> keyValuePair = array[num];
				Vector2 key = keyValuePair.Key;
				Object value = keyValuePair.Value;
				if (value.IsSpawnedObject && !value.IsBreakableStone())
				{
					objects.Remove(key);
				}
				else if (value.QualifiedItemId == "(O)590" && doesTileHavePropertyNoNull((int)key.X, (int)key.Y, "Diggable", "Back") == "")
				{
					objects.Remove(key);
				}
			}
			numberOfSpawnedObjectsOnMap = 0;
		}
		switch (season)
		{
		case Season.Spring:
			waterColor.Value = new Color(120, 200, 255) * 0.5f;
			break;
		case Season.Summer:
			waterColor.Value = new Color(60, 240, 255) * 0.5f;
			break;
		case Season.Fall:
			waterColor.Value = new Color(255, 130, 200) * 0.5f;
			break;
		case Season.Winter:
			waterColor.Value = new Color(130, 80, 255) * 0.5f;
			break;
		}
		if (!onLoad && season == Season.Spring && Game1.stats.DaysPlayed > 1 && !(this is Farm))
		{
			loadWeeds();
		}
	}

	public List<FarmAnimal> getAllFarmAnimals()
	{
		List<FarmAnimal> list = animals.Values.ToList();
		foreach (Building building in buildings)
		{
			GameLocation indoors = building.GetIndoors();
			if (indoors != null)
			{
				list.AddRange(indoors.animals.Values);
			}
		}
		return list;
	}

	public virtual int GetHayCapacity()
	{
		int num = 0;
		foreach (Building building in buildings)
		{
			if (building.hayCapacity.Value > 0 && building.daysOfConstructionLeft.Value <= 0)
			{
				num += building.hayCapacity.Value;
			}
		}
		return num;
	}

	public bool CheckPetAnimal(Vector2 position, Farmer who)
	{
		foreach (FarmAnimal value in animals.Values)
		{
			if (!value.wasPet.Value && value.GetCursorPetBoundingBox().Contains((int)position.X, (int)position.Y))
			{
				value.pet(who);
				return true;
			}
		}
		return false;
	}

	public bool CheckPetAnimal(Microsoft.Xna.Framework.Rectangle rect, Farmer who)
	{
		foreach (FarmAnimal value in animals.Values)
		{
			if (!value.wasPet.Value && value.GetBoundingBox().Intersects(rect))
			{
				value.pet(who);
				return true;
			}
		}
		return false;
	}

	public bool CheckInspectAnimal(Vector2 position, Farmer who)
	{
		foreach (FarmAnimal value in animals.Values)
		{
			if (value.wasPet.Value && value.GetCursorPetBoundingBox().Contains((int)position.X, (int)position.Y))
			{
				value.pet(who);
				return true;
			}
		}
		return false;
	}

	public bool CheckInspectAnimal(Microsoft.Xna.Framework.Rectangle rect, Farmer who)
	{
		foreach (FarmAnimal value in animals.Values)
		{
			if (value.wasPet.Value && value.GetBoundingBox().Intersects(rect))
			{
				value.pet(who);
				return true;
			}
		}
		return false;
	}

	public virtual void updateSeasonalTileSheets(Map map = null)
	{
		if (map == null)
		{
			map = Map;
		}
		if (!(this is Summit) && (!IsOutdoors || Name.Equals("Desert")))
		{
			return;
		}
		map.DisposeTileSheets(Game1.mapDisplayDevice);
		foreach (TileSheet tileSheet in map.TileSheets)
		{
			string imageSource = tileSheet.ImageSource;
			try
			{
				tileSheet.ImageSource = GetSeasonalTilesheetName(tileSheet.ImageSource, GetSeasonKey());
				Game1.mapDisplayDevice.LoadTileSheet(tileSheet);
			}
			catch (Exception exception)
			{
				Game1.log.Error($"Location '{NameOrUniqueName}' failed to load seasonal asset name '{tileSheet.ImageSource}' for tilesheet ID '{tileSheet.Id}'.", exception);
				tileSheet.ImageSource = imageSource;
			}
		}
		map.LoadTileSheets(Game1.mapDisplayDevice);
	}

	public static string GetSeasonalTilesheetName(string sheet_path, string current_season)
	{
		string fileName = Path.GetFileName(sheet_path);
		if (fileName.StartsWith("spring_") || fileName.StartsWith("summer_") || fileName.StartsWith("fall_") || fileName.StartsWith("winter_"))
		{
			sheet_path = Path.Combine(Path.GetDirectoryName(sheet_path), current_season + fileName.Substring(fileName.IndexOf('_')));
		}
		return sheet_path;
	}

	public virtual string checkEventPrecondition(string precondition)
	{
		return checkEventPrecondition(precondition, check_seen: true);
	}

	public virtual string checkEventPrecondition(string precondition, bool check_seen)
	{
		string[] array = Event.SplitPreconditions(precondition);
		string text = array[0];
		if (string.IsNullOrEmpty(text) || text == "-1")
		{
			return "-1";
		}
		if (check_seen && (Game1.player.eventsSeen.Contains(text) || Game1.eventsSeenSinceLastLocationChange.Contains(text)))
		{
			return "-1";
		}
		for (int i = 1; i < array.Length; i++)
		{
			if (!string.IsNullOrEmpty(array[i]) && !Event.CheckPrecondition(this, array[0], array[i]))
			{
				return "-1";
			}
		}
		return text;
	}

	/// <summary>Get hay from any non-empty silos.</summary>
	/// <param name="currentLocation">The location in which the hay was found.</param>
	public static Object GetHayFromAnySilo(GameLocation currentLocation)
	{
		if (TryGetHayFrom(currentLocation, out var hay))
		{
			return hay;
		}
		if (currentLocation.Name != "Farm" && TryGetHayFrom(Game1.getFarm(), out hay))
		{
			return hay;
		}
		Utility.ForEachLocation((GameLocation location) => !TryGetHayFrom(location, out hay), includeInteriors: false);
		return hay;
		static bool TryGetHayFrom(GameLocation location, out Object foundHay)
		{
			if (location.piecesOfHay.Value < 1)
			{
				foundHay = null;
				return false;
			}
			foundHay = ItemRegistry.Create<Object>("(O)178");
			location.piecesOfHay.Value--;
			return true;
		}
	}

	/// <summary>Store hay in any silos that have available space.</summary>
	/// <param name="count">The number of hay items to store.</param>
	/// <param name="currentLocation">The location in which the hay was found.</param>
	/// <returns>Returns the number of hay that couldn't be stored.</returns>
	public static int StoreHayInAnySilo(int count, GameLocation currentLocation)
	{
		count = currentLocation.tryToAddHay(count);
		if (count > 0 && currentLocation.Name != "Farm")
		{
			count = Game1.getFarm().tryToAddHay(count);
			if (count <= 0)
			{
				return 0;
			}
		}
		if (count > 0)
		{
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (location.buildings.Count > 0)
				{
					count = location.tryToAddHay(count);
					return count > 0;
				}
				return true;
			}, includeInteriors: false);
		}
		if (count <= 0)
		{
			return 0;
		}
		return count;
	}

	/// <summary>Store hay in the current location's silos, if they have space available.</summary>
	/// <param name="num">The number of hay items to store.</param>
	/// <returns>Returns the number of hay that couldn't be stored.</returns>
	public int tryToAddHay(int num)
	{
		int num2 = Math.Min(GetHayCapacity() - piecesOfHay.Value, num);
		piecesOfHay.Value += num2;
		return num - num2;
	}

	public Building getBuildingAt(Vector2 tile)
	{
		foreach (Building building in buildings)
		{
			if (building.occupiesTile(tile) || !building.isTilePassable(tile))
			{
				return building;
			}
		}
		return null;
	}

	/// <summary>Get a building by its <see cref="F:StardewValley.Buildings.Building.buildingType" /> value.</summary>
	/// <param name="id">The building type key.</param>
	public Building getBuildingByType(string type)
	{
		if (type != null)
		{
			foreach (Building building in buildings)
			{
				if (string.Equals(building.buildingType.Value, type, StringComparison.Ordinal))
				{
					return building;
				}
			}
		}
		return null;
	}

	/// <summary>Get a building by its <see cref="F:StardewValley.Buildings.Building.id" /> value.</summary>
	/// <param name="id">The unique building ID.</param>
	public Building getBuildingById(Guid id)
	{
		if (id != Guid.Empty)
		{
			foreach (Building building in buildings)
			{
				if (building.id.Value == id)
				{
					return building;
				}
			}
		}
		return null;
	}

	/// <summary>Get a building by the unique name of its interior location.</summary>
	/// <param name="id">The building interior location's unique name.</param>
	public Building getBuildingByName(string name)
	{
		if (name != null)
		{
			foreach (Building building in buildings)
			{
				if (building.HasIndoorsName(name))
				{
					return building;
				}
			}
		}
		return null;
	}

	public bool destroyStructure(Vector2 tile)
	{
		Building buildingAt = getBuildingAt(tile);
		if (buildingAt != null)
		{
			return destroyStructure(buildingAt);
		}
		return false;
	}

	public bool destroyStructure(Building building)
	{
		if (buildings.Remove(building))
		{
			building.performActionOnDemolition(this);
			Game1.player.team.SendBuildingDemolishedEvent(this, building);
			return true;
		}
		return false;
	}

	public bool buildStructure(Building building, Vector2 tileLocation, Farmer who, bool skipSafetyChecks = false)
	{
		if (!skipSafetyChecks)
		{
			for (int i = 0; i < building.tilesHigh.Value; i++)
			{
				for (int j = 0; j < building.tilesWide.Value; j++)
				{
					pokeTileForConstruction(new Vector2(tileLocation.X + (float)j, tileLocation.Y + (float)i));
				}
			}
			foreach (BuildingPlacementTile additionalPlacementTile in building.GetAdditionalPlacementTiles())
			{
				foreach (Point point in additionalPlacementTile.TileArea.GetPoints())
				{
					pokeTileForConstruction(new Vector2(tileLocation.X + (float)point.X, tileLocation.Y + (float)point.Y));
				}
			}
			for (int k = 0; k < building.tilesHigh.Value; k++)
			{
				for (int l = 0; l < building.tilesWide.Value; l++)
				{
					Vector2 vector = new Vector2(tileLocation.X + (float)l, tileLocation.Y + (float)k);
					if (buildings.Contains(building) && building.occupiesTile(vector))
					{
						continue;
					}
					if (!isBuildable(vector))
					{
						return false;
					}
					foreach (Farmer farmer in farmers)
					{
						if (farmer.GetBoundingBox().Intersects(new Microsoft.Xna.Framework.Rectangle(l * 64, k * 64, 64, 64)))
						{
							return false;
						}
					}
				}
			}
			foreach (BuildingPlacementTile additionalPlacementTile2 in building.GetAdditionalPlacementTiles())
			{
				bool onlyNeedsToBePassable = additionalPlacementTile2.OnlyNeedsToBePassable;
				foreach (Point point2 in additionalPlacementTile2.TileArea.GetPoints())
				{
					int x = point2.X;
					int y = point2.Y;
					Vector2 vector2 = new Vector2(tileLocation.X + (float)x, tileLocation.Y + (float)y);
					if (buildings.Contains(building) && building.occupiesTile(vector2))
					{
						continue;
					}
					if (!isBuildable(vector2, onlyNeedsToBePassable))
					{
						return false;
					}
					if (onlyNeedsToBePassable)
					{
						continue;
					}
					foreach (Farmer farmer2 in farmers)
					{
						if (farmer2.GetBoundingBox().Intersects(new Microsoft.Xna.Framework.Rectangle(x * 64, y * 64, 64, 64)))
						{
							return false;
						}
					}
				}
			}
			if (building.humanDoor.Value != new Point(-1, -1))
			{
				Vector2 vector3 = tileLocation + new Vector2(building.humanDoor.X, building.humanDoor.Y + 1);
				if ((!buildings.Contains(building) || !building.occupiesTile(vector3)) && !isBuildable(vector3) && !isPath(vector3))
				{
					return false;
				}
			}
			string text = building.isThereAnythingtoPreventConstruction(this, tileLocation);
			if (text != null)
			{
				Game1.addHUDMessage(new HUDMessage(text, 3));
				return false;
			}
		}
		building.tileX.Value = (int)tileLocation.X;
		building.tileY.Value = (int)tileLocation.Y;
		for (int m = 0; m < building.tilesHigh.Value; m++)
		{
			for (int n = 0; n < building.tilesWide.Value; n++)
			{
				Vector2 key = new Vector2(tileLocation.X + (float)n, tileLocation.Y + (float)m);
				if (!(terrainFeatures.GetValueOrDefault(key) is Flooring) || !(building.GetData()?.AllowsFlooringUnderneath ?? false))
				{
					terrainFeatures.Remove(key);
				}
			}
		}
		if (!buildings.Contains(building))
		{
			buildings.Add(building);
			who.team.SendBuildingConstructedEvent(this, building, who);
		}
		GameLocation indoors = building.GetIndoors();
		if (indoors is AnimalHouse animalHouse)
		{
			foreach (long item in animalHouse.animalsThatLiveHere)
			{
				FarmAnimal value = Utility.getAnimal(item);
				if (value != null)
				{
					value.homeInterior = indoors;
				}
				else if (animalHouse.animals.TryGetValue(item, out value))
				{
					value.homeInterior = indoors;
				}
			}
		}
		if (indoors != null)
		{
			foreach (Warp warp in indoors.warps)
			{
				if (warp.TargetName == NameOrUniqueName)
				{
					warp.TargetX = building.humanDoor.X + building.tileX.Value;
					warp.TargetY = building.humanDoor.Y + building.tileY.Value + 1;
				}
			}
		}
		for (int num = 0; num < building.tilesHigh.Value; num++)
		{
			for (int num2 = 0; num2 < building.tilesWide.Value; num2++)
			{
				RemoveArtifactSpots(new Vector2(tileLocation.X + (float)num2, tileLocation.Y + (float)num));
			}
		}
		foreach (BuildingPlacementTile additionalPlacementTile3 in building.GetAdditionalPlacementTiles())
		{
			if (additionalPlacementTile3.OnlyNeedsToBePassable)
			{
				continue;
			}
			foreach (Point point3 in additionalPlacementTile3.TileArea.GetPoints())
			{
				RemoveArtifactSpots(new Vector2(tileLocation.X + (float)point3.X, tileLocation.Y + (float)point3.Y));
			}
		}
		return true;
		void RemoveArtifactSpots(Vector2 tile_location)
		{
			if (getObjectAtTile((int)tile_location.X, (int)tile_location.Y)?.QualifiedItemId == "(O)590")
			{
				removeObject(tile_location, showDestroyedObject: false);
			}
		}
	}

	/// <summary>Construct a building in the location.</summary>
	/// <param name="typeId">The building type ID in <c>Data/Buildings</c>.</param>
	/// <param name="data">The building data from <c>Data/Buildings</c>.</param>
	/// <param name="tileLocation">The top-left tile position of the building.</param>
	/// <param name="who">The player constructing the building.</param>
	/// <param name="magicalConstruction">Whether construction should complete instantly.</param>
	/// <param name="skipSafetyChecks">Whether to ignore safety checks (e.g. making sure the area is clear).</param>
	/// <returns>Returns whether the building was successfully placed.</returns>
	public bool buildStructure(string typeId, BuildingData data, Vector2 tileLocation, Farmer who, out Building constructed, bool magicalConstruction = false, bool skipSafetyChecks = false)
	{
		if (data == null || (!skipSafetyChecks && !IsBuildableLocation()))
		{
			constructed = null;
			return false;
		}
		int x = data.Size.X;
		int y = data.Size.Y;
		List<BuildingPlacementTile> list = data.AdditionalPlacementTiles ?? new List<BuildingPlacementTile>(0);
		if (!skipSafetyChecks)
		{
			for (int i = 0; i < y; i++)
			{
				for (int j = 0; j < x; j++)
				{
					pokeTileForConstruction(new Vector2(tileLocation.X + (float)j, tileLocation.Y + (float)i));
				}
			}
			foreach (BuildingPlacementTile item in list)
			{
				foreach (Point point in item.TileArea.GetPoints())
				{
					pokeTileForConstruction(new Vector2(tileLocation.X + (float)point.X, tileLocation.Y + (float)point.Y));
				}
			}
			for (int k = 0; k < y; k++)
			{
				for (int l = 0; l < x; l++)
				{
					Vector2 tileLocation2 = new Vector2(tileLocation.X + (float)l, tileLocation.Y + (float)k);
					if (!isBuildable(tileLocation2))
					{
						constructed = null;
						return false;
					}
					foreach (Farmer farmer in farmers)
					{
						if (farmer.GetBoundingBox().Intersects(new Microsoft.Xna.Framework.Rectangle(l * 64, k * 64, 64, 64)))
						{
							constructed = null;
							return false;
						}
					}
				}
			}
			foreach (BuildingPlacementTile item2 in list)
			{
				bool onlyNeedsToBePassable = item2.OnlyNeedsToBePassable;
				foreach (Point point2 in item2.TileArea.GetPoints())
				{
					int x2 = point2.X;
					int y2 = point2.Y;
					Vector2 tileLocation3 = new Vector2(tileLocation.X + (float)x2, tileLocation.Y + (float)y2);
					if (!isBuildable(tileLocation3, onlyNeedsToBePassable))
					{
						constructed = null;
						return false;
					}
					if (onlyNeedsToBePassable)
					{
						continue;
					}
					foreach (Farmer farmer2 in farmers)
					{
						if (farmer2.GetBoundingBox().Intersects(new Microsoft.Xna.Framework.Rectangle(x2 * 64, y2 * 64, 64, 64)))
						{
							constructed = null;
							return false;
						}
					}
				}
			}
			if (data.HumanDoor != new Point(-1, -1))
			{
				Vector2 tileLocation4 = tileLocation + new Vector2(data.HumanDoor.X, data.HumanDoor.Y + 1);
				if (!isBuildable(tileLocation4, onlyNeedsToBePassable: true) && !isPath(tileLocation4))
				{
					constructed = null;
					return false;
				}
			}
		}
		Building building = Building.CreateInstanceFromId(typeId, tileLocation);
		if (magicalConstruction)
		{
			building.magical.Value = true;
			building.daysOfConstructionLeft.Value = 0;
		}
		building.owner.Value = who.UniqueMultiplayerID;
		if (!skipSafetyChecks)
		{
			string text = building.isThereAnythingtoPreventConstruction(this, tileLocation);
			if (text != null)
			{
				Game1.addHUDMessage(new HUDMessage(text, 3));
				constructed = null;
				return false;
			}
		}
		for (int m = 0; m < building.tilesHigh.Value; m++)
		{
			for (int n = 0; n < building.tilesWide.Value; n++)
			{
				Vector2 key = new Vector2(tileLocation.X + (float)n, tileLocation.Y + (float)m);
				if (!(terrainFeatures.GetValueOrDefault(key) is Flooring) || !(building.GetData()?.AllowsFlooringUnderneath ?? false))
				{
					terrainFeatures.Remove(key);
				}
			}
		}
		buildings.Add(building);
		who.team.SendBuildingConstructedEvent(this, building, who);
		string messageKey = (magicalConstruction ? "BuildingMagicBuild" : "BuildingBuild");
		Game1.multiplayer.globalChatInfoMessage(messageKey, Game1.player.Name, "aOrAn:" + data.Name, data.Name, Game1.player.farmName.Value);
		constructed = building;
		return true;
	}

	/// <summary>Construct a building in the location.</summary>
	/// <param name="typeId">The building type ID in <c>Data/Buildings</c>.</param>
	/// <param name="tileLocation">The top-left tile position of the building.</param>
	/// <param name="who">The player constructing the building.</param>
	/// <param name="magicalConstruction">Whether construction should complete instantly.</param>
	/// <param name="skipSafetyChecks">Whether to ignore safety checks (e.g. making sure the area is clear).</param>
	/// <returns>Returns whether the building was successfully placed.</returns>
	public bool buildStructure(string typeId, Vector2 tileLocation, Farmer who, out Building constructed, bool magicalConstruction = false, bool skipSafetyChecks = false)
	{
		if (typeId == null || !Game1.buildingData.TryGetValue(typeId, out var value))
		{
			Game1.log.Error("Can't construct building '" + typeId + "', no data found matching that ID.");
			constructed = null;
			return false;
		}
		return buildStructure(typeId, value, tileLocation, who, out constructed, magicalConstruction, skipSafetyChecks);
	}

	/// <summary>Get whether the location contains any buildings of the given type.</summary>
	/// <param name="name">The building type's ID in <c>Data/Buildings</c>.</param>
	public bool isBuildingConstructed(string name)
	{
		return getNumberBuildingsConstructed(name) > 0;
	}

	/// <summary>Get whether the location has a minimum number of matching buildings.</summary>
	/// <param name="buildingType">The building type to count.</param>
	/// <param name="minCount">The minimum number needed.</param>
	public bool HasMinBuildings(string buildingType, int minCount)
	{
		return getNumberBuildingsConstructed(buildingType) >= minCount;
	}

	/// <summary>Get whether the location has a minimum number of matching buildings.</summary>
	/// <param name="match">A filter which matches buildings to count.</param>
	/// <param name="minCount">The minimum number needed.</param>
	public bool HasMinBuildings(Func<Building, bool> match, int minCount)
	{
		if (minCount <= 0)
		{
			return true;
		}
		int num = 0;
		foreach (Building building in buildings)
		{
			if (match(building))
			{
				num++;
			}
			if (num >= minCount)
			{
				return true;
			}
		}
		return false;
	}

	public int getNumberBuildingsConstructed(bool includeUnderConstruction = false)
	{
		if (includeUnderConstruction || buildings.Count == 0)
		{
			return buildings.Count;
		}
		int num = 0;
		foreach (Building building in buildings)
		{
			if (!building.isUnderConstruction())
			{
				num++;
			}
		}
		return num;
	}

	public int getNumberBuildingsConstructed(string name, bool includeUnderConstruction = false)
	{
		int num = 0;
		if (buildings.Count > 0)
		{
			foreach (Building building in buildings)
			{
				if (building.buildingType.Value == name && (includeUnderConstruction || !building.isUnderConstruction()))
				{
					num++;
				}
			}
		}
		return num;
	}

	public bool isThereABuildingUnderConstruction()
	{
		if (buildings.Count > 0)
		{
			foreach (Building building in buildings)
			{
				if (building.isUnderConstruction())
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Get all building interiors within this location which are instanced to the building (i.e. not in <see cref="P:StardewValley.Game1.locations" /> separately).</summary>
	public IEnumerable<GameLocation> GetInstancedBuildingInteriors()
	{
		List<GameLocation> interiors = null;
		ForEachInstancedInterior(delegate(GameLocation location)
		{
			if (interiors == null)
			{
				interiors = new List<GameLocation>();
			}
			interiors.Add(location);
			return true;
		});
		if (interiors == null)
		{
			return LegacyShims.EmptyArray<GameLocation>();
		}
		return interiors;
	}

	/// <summary>Perform an action for each building interior within this location which is instanced to the building (i.e. not in <see cref="P:StardewValley.Game1.locations" /> separately).</summary>
	/// <param name="action">The action to perform for each interior. This should return true (continue iterating) or false (stop).</param>
	public void ForEachInstancedInterior(Func<GameLocation, bool> action)
	{
		foreach (Building building in buildings)
		{
			if (building.GetIndoorsType() == IndoorsType.Instanced)
			{
				GameLocation indoors = building.GetIndoors();
				if (indoors != null && !action(indoors))
				{
					break;
				}
			}
		}
	}

	/// <summary>Perform an action for each tilled dirt in the location.</summary>
	/// <param name="action">The action to perform.</param>
	/// <param name="includeGardenPots">Whether to apply the action to dirt in garden pots.</param>
	public void ForEachDirt(Func<HoeDirt, bool> action, bool includeGardenPots = true)
	{
		foreach (TerrainFeature value in terrainFeatures.Values)
		{
			if (value is HoeDirt arg && !action(arg))
			{
				return;
			}
		}
		if (!includeGardenPots)
		{
			return;
		}
		using Dictionary<Vector2, Object>.ValueCollection.Enumerator enumerator2 = objects.Values.GetEnumerator();
		while (enumerator2.MoveNext() && (!(enumerator2.Current is IndoorPot indoorPot) || indoorPot.bush.Value != null || action(indoorPot.hoeDirt.Value)))
		{
		}
	}

	public bool isPath(Vector2 tileLocation)
	{
		if (terrainFeatures.TryGetValue(tileLocation, out var value) && value != null && value.isPassable())
		{
			if (objects.TryGetValue(tileLocation, out var value2) && value2 != null)
			{
				return value2.isPassable();
			}
			return true;
		}
		return false;
	}

	public bool isBuildable(Vector2 tileLocation, bool onlyNeedsToBePassable = false)
	{
		Microsoft.Xna.Framework.Rectangle buildableRectangle = GetBuildableRectangle();
		if (buildableRectangle != Microsoft.Xna.Framework.Rectangle.Empty && !buildableRectangle.Contains((int)tileLocation.X, (int)tileLocation.Y))
		{
			return false;
		}
		if (onlyNeedsToBePassable)
		{
			if (isTilePassable(tileLocation))
			{
				return !IsTileOccupiedBy(tileLocation, CollisionMask.All, CollisionMask.All);
			}
			return false;
		}
		Building buildingAt = getBuildingAt(tileLocation);
		if (buildingAt != null && !buildingAt.isMoving)
		{
			return false;
		}
		if (CanItemBePlacedHere(tileLocation, itemIsPassable: false, CollisionMask.All, ~CollisionMask.Objects, useFarmerTile: true) || getObjectAtTile((int)tileLocation.X, (int)tileLocation.Y)?.QualifiedItemId == "(O)590")
		{
			if (_looserBuildRestrictions)
			{
				return !Game1.currentLocation.doesTileHavePropertyNoNull((int)tileLocation.X, (int)tileLocation.Y, "Buildable", "Back").EqualsIgnoreCase("f");
			}
			if (Game1.currentLocation.doesTileHavePropertyNoNull((int)tileLocation.X, (int)tileLocation.Y, "Buildable", "Back").EqualsIgnoreCase("t") || Game1.currentLocation.doesTileHavePropertyNoNull((int)tileLocation.X, (int)tileLocation.Y, "Buildable", "Back").ToLower().Equals("true"))
			{
				return true;
			}
			if (Game1.currentLocation.doesTileHaveProperty((int)tileLocation.X, (int)tileLocation.Y, "Diggable", "Back") != null && !Game1.currentLocation.doesTileHavePropertyNoNull((int)tileLocation.X, (int)tileLocation.Y, "Buildable", "Back").EqualsIgnoreCase("f"))
			{
				return true;
			}
		}
		return false;
	}

	public virtual void pokeTileForConstruction(Vector2 tile)
	{
		foreach (FarmAnimal value in animals.Values)
		{
			if (value.Tile == tile)
			{
				value.Poke();
			}
		}
	}

	public virtual void updateWarps()
	{
		if (Game1.IsClient)
		{
			return;
		}
		warps.Clear();
		string[] array = new string[2] { "NPCWarp", "Warp" };
		foreach (string text in array)
		{
			if (!map.Properties.TryGetValue(text, out var value) || value == null)
			{
				continue;
			}
			bool flag = text == "NPCWarp";
			string[] array2 = ArgUtility.SplitBySpace(value);
			for (int j = 0; j < array2.Length; j += 5)
			{
				bool flag2 = array2.Length >= j + 5;
				if (!flag2 || !int.TryParse(array2[j], out var result) || !int.TryParse(array2[j + 1], out var result2) || !int.TryParse(array2[j + 3], out var result3) || !int.TryParse(array2[j + 4], out var result4))
				{
					Game1.log.Warn($"Failed parsing {(flag ? "NPC warp" : "warp")} '{string.Join(" ", array2.Skip(j))}' for location '{NameOrUniqueName}'. Warps must have five fields in the form 'fromX fromY toLocationName toX toY', but " + ((!flag2) ? "got insufficient fields." : "got a non-numeric value for one of the X/Y position fields."));
				}
				else
				{
					warps.Add(new Warp(result, result2, array2[j + 2], result3, result4, flipFarmer: false, flag));
				}
			}
		}
		if (warps.Count > 0)
		{
			ParentBuilding?.updateInteriorWarps(this);
		}
	}

	public void loadWeeds()
	{
		if (!isOutdoors.Value && !treatAsOutdoors.Value)
		{
			return;
		}
		Layer layer = map?.GetLayer("Paths");
		if (layer == null)
		{
			return;
		}
		for (int i = 0; i < map.Layers[0].LayerWidth; i++)
		{
			for (int j = 0; j < map.Layers[0].LayerHeight; j++)
			{
				int tileIndexAt = layer.GetTileIndexAt(i, j);
				if (tileIndexAt == -1)
				{
					continue;
				}
				Vector2 vector = new Vector2(i, j);
				switch (tileIndexAt)
				{
				case 13:
				case 14:
				case 15:
					if (CanLoadPathObjectHere(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(getWeedForSeason(Game1.random, GetSeason())));
					}
					break;
				case 16:
					if (CanLoadPathObjectHere(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(Game1.random.Choose("(O)343", "(O)450")));
					}
					break;
				case 17:
					if (CanLoadPathObjectHere(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(Game1.random.Choose("(O)343", "(O)450")));
					}
					break;
				case 18:
					if (CanLoadPathObjectHere(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(Game1.random.Choose("(O)294", "(O)295")));
					}
					break;
				}
			}
		}
	}

	/// <summary>Get whether a tile is unoccupied for the purposes of spawning weed/stone debris from the <c>Paths</c> layer.</summary>
	/// <param name="tile">The tile position.</param>
	public bool CanLoadPathObjectHere(Vector2 tile)
	{
		if (IsTileOccupiedBy(tile, CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.Objects | CollisionMask.TerrainFeatures))
		{
			return false;
		}
		Vector2 vector = tile * 64f;
		vector.X += 32f;
		vector.Y += 32f;
		foreach (Furniture item in furniture)
		{
			if (item.furniture_type.Value != 12 && !item.isPassable() && item.GetBoundingBox().Contains((int)vector.X, (int)vector.Y) && !item.AllowPlacementOnThisTile((int)tile.X, (int)tile.Y))
			{
				return false;
			}
		}
		return true;
	}

	public void loadObjects()
	{
		_startingCabinLocations.Clear();
		if (map == null)
		{
			return;
		}
		updateWarps();
		Layer layer = map.GetLayer("Paths");
		string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces("Trees");
		for (int i = 0; i < mapPropertySplitBySpaces.Length; i += 3)
		{
			if (!ArgUtility.TryGetVector2(mapPropertySplitBySpaces, i, out var value, out var error, integerOnly: false, "Vector2 position") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces, i + 2, out var value2, out error, "int treeType"))
			{
				LogMapPropertyError("Trees", mapPropertySplitBySpaces, error);
			}
			else
			{
				terrainFeatures.Add(value, new Tree((value2 + 1).ToString(), 5));
			}
		}
		if (layer != null && TryGetMapProperty("LoadTreesFrom", out var propertyValue))
		{
			GameLocation locationFromName = Game1.getLocationFromName(propertyValue);
			if (locationFromName != null)
			{
				foreach (KeyValuePair<Vector2, TerrainFeature> pair in locationFromName.terrainFeatures.Pairs)
				{
					if (pair.Value is Tree tree)
					{
						Point point = new Point((int)pair.Key.X, (int)pair.Key.Y);
						if (layer.HasTileAt(point.X, point.Y) && TryGetTreeIdForTile(layer.Tiles[point.X, point.Y], out var _, out var _, out var _, out var _))
						{
							terrainFeatures.Add(pair.Key, new Tree(tree.treeType.Value, tree.growthStage.Value));
						}
					}
				}
			}
		}
		if ((isOutdoors.Value || name.Equals("BathHouse_Entry") || treatAsOutdoors.Value || map.Properties.ContainsKey("forceLoadObjects")) && layer != null)
		{
			loadPathsLayerObjectsInArea(0, 0, map.Layers[0].LayerWidth, map.Layers[0].LayerHeight);
			if (!Game1.eventUp && HasMapPropertyWithValue(GetSeason().ToString() + "_Objects"))
			{
				spawnObjects();
			}
		}
		updateDoors();
	}

	public void loadPathsLayerObjectsInArea(int startingX, int startingY, int width, int height)
	{
		Layer layer = map.GetLayer("Paths");
		for (int i = startingX; i < startingX + width; i++)
		{
			for (int j = startingY; j < startingY + height; j++)
			{
				Tile tile = layer.Tiles[i, j];
				if (tile == null)
				{
					continue;
				}
				Vector2 vector = new Vector2(i, j);
				if (TryGetTreeIdForTile(tile, out var treeId, out var growthStageOnLoad, out var _, out var isFruitTree))
				{
					if (GetFurnitureAt(vector) == null && !terrainFeatures.ContainsKey(vector) && !objects.ContainsKey(vector))
					{
						if (isFruitTree)
						{
							terrainFeatures.Add(vector, new FruitTree(treeId, growthStageOnLoad ?? 4));
						}
						else
						{
							terrainFeatures.Add(vector, new Tree(treeId, growthStageOnLoad ?? 5));
						}
					}
					continue;
				}
				switch (tile.TileIndex)
				{
				case 13:
				case 14:
				case 15:
					if (!objects.ContainsKey(vector) && (!IsOutdoors || !Game1.IsWinter))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(getWeedForSeason(Game1.random, GetSeason())));
					}
					break;
				case 16:
					if (!objects.ContainsKey(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(Game1.random.Choose("(O)343", "(O)450")));
					}
					break;
				case 17:
					if (!objects.ContainsKey(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(Game1.random.Choose("(O)343", "(O)450")));
					}
					break;
				case 18:
					if (!objects.ContainsKey(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(Game1.random.Choose("(O)294", "(O)295")));
					}
					break;
				case 19:
					addResourceClumpAndRemoveUnderlyingTerrain(602, 2, 2, vector);
					break;
				case 20:
					addResourceClumpAndRemoveUnderlyingTerrain(672, 2, 2, vector);
					break;
				case 21:
					addResourceClumpAndRemoveUnderlyingTerrain(600, 2, 2, vector);
					break;
				case 22:
				case 36:
				{
					if (terrainFeatures.ContainsKey(vector))
					{
						break;
					}
					Microsoft.Xna.Framework.Rectangle value2 = new Microsoft.Xna.Framework.Rectangle((int)vector.X * 64, (int)vector.Y * 64, 64, 64);
					value2.Inflate(-1, -1);
					bool flag = false;
					foreach (ResourceClump resourceClump in resourceClumps)
					{
						if (resourceClump.getBoundingBox().Intersects(value2))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						terrainFeatures.Add(vector, new Grass((tile.TileIndex != 36) ? 1 : 7, 3));
					}
					break;
				}
				case 23:
					if (!terrainFeatures.ContainsKey(vector))
					{
						terrainFeatures.Add(vector, new Tree(Game1.random.Next(1, 4).ToString(), Game1.random.Next(2, 4)));
					}
					break;
				case 24:
					if (!terrainFeatures.ContainsKey(vector))
					{
						largeTerrainFeatures.Add(new Bush(vector, 2, this));
					}
					break;
				case 25:
					if (!terrainFeatures.ContainsKey(vector))
					{
						largeTerrainFeatures.Add(new Bush(vector, 1, this));
					}
					break;
				case 26:
					if (!terrainFeatures.ContainsKey(vector))
					{
						largeTerrainFeatures.Add(new Bush(vector, 0, this));
					}
					break;
				case 33:
					if (!terrainFeatures.ContainsKey(vector))
					{
						largeTerrainFeatures.Add(new Bush(vector, 4, this));
					}
					break;
				case 27:
					changeMapProperties("BrookSounds", vector.X + " " + vector.Y + " 0");
					break;
				case 29:
				case 30:
				{
					if (Game1.startingCabins > 0 && tile.Properties.TryGetValue("Order", out var value) && int.Parse(value) <= Game1.startingCabins && ((tile.TileIndex == 29 && !Game1.cabinsSeparate) || (tile.TileIndex == 30 && Game1.cabinsSeparate)))
					{
						_startingCabinLocations.Add(vector);
					}
					break;
				}
				}
			}
		}
	}

	/// <summary>Get the tree to spawn on a tile based on its tile index on the <c>Paths</c> layer, if any.</summary>
	/// <param name="tileIndex">The tile index on the <c>Paths</c> layer.</param>
	/// <param name="treeId">The tree ID in <c>Data/FruitTrees</c> or <c>Data/WildTrees</c> that should spawn.</param>
	/// <param name="growthStageOnLoad">The preferred tree growth stage when first populating the save, if applicable.</param>
	/// <param name="growthStageOnRegrow">The preferred tree growth stage when regrowing trees on day update, if applicable.</param>
	/// <param name="isFruitTree">Whether to spawn a fruit tree (<c>true</c>) or wild tree (<c>false</c>).</param>
	/// <returns>Returns whether a tree should spawn here.</returns>
	public bool TryGetTreeIdForTile(Tile tile, out string treeId, out int? growthStageOnLoad, out int? growthStageOnRegrow, out bool isFruitTree)
	{
		isFruitTree = false;
		growthStageOnLoad = null;
		growthStageOnRegrow = null;
		if (tile == null)
		{
			treeId = null;
			return false;
		}
		switch (tile.TileIndex)
		{
		case 9:
			treeId = (IsWinterHere() ? "4" : "1");
			return true;
		case 10:
			treeId = (IsWinterHere() ? "5" : "2");
			return true;
		case 11:
			treeId = "3";
			return true;
		case 12:
			treeId = "6";
			return true;
		case 31:
			treeId = "9";
			return true;
		case 32:
			treeId = "8";
			return true;
		case 34:
		{
			if (!tile.Properties.TryGetValue("SpawnTree", out var value))
			{
				Game1.log.Warn($"Location '{NameOrUniqueName}' ignored path tile index 34 (spawn tree) at position {tile} because the tile has no '{"SpawnTree"}' tile property.");
				break;
			}
			string[] array = ArgUtility.SplitBySpace(value);
			if (!ArgUtility.TryGet(array, 0, out var value2, out var error, allowBlank: true, "string rawType") || !ArgUtility.TryGet(array, 1, out var value3, out error, allowBlank: true, "string rawId") || !ArgUtility.TryGetOptionalInt(array, 2, out var value4, out error, -1, "int rawGrowthStageOnLoad") || !ArgUtility.TryGetOptionalInt(array, 3, out var value5, out error, -1, "int rawGrowthStageOnRegrow"))
			{
				Game1.log.Warn($"Location '{NameOrUniqueName}' ignored path tile index 34 (spawn tree) at position {tile} because the '{"SpawnTree"}' tile property is invalid: {error}.");
				break;
			}
			if (value4 > -1)
			{
				growthStageOnLoad = value4;
			}
			if (value5 > -1)
			{
				growthStageOnRegrow = value5;
			}
			if (value2.EqualsIgnoreCase("wild"))
			{
				treeId = value3;
				return true;
			}
			if (value2.EqualsIgnoreCase("fruit"))
			{
				treeId = value3;
				isFruitTree = true;
				return true;
			}
			Game1.log.Warn($"Location '{NameOrUniqueName}' ignored path tile index 34 (spawn tree) at position {tile} because the '{"SpawnTree"}' tile property has invalid type '{value2}' (expected 'fruit' or 'wild').");
			break;
		}
		}
		growthStageOnLoad = null;
		growthStageOnRegrow = null;
		treeId = null;
		return false;
	}

	public void BuildStartingCabins()
	{
		if (_startingCabinLocations.Count > 0)
		{
			List<string> list = new List<string>();
			switch (Game1.whichFarm)
			{
			case 3:
			case 4:
				list.Add("Stone Cabin");
				list.Add("Log Cabin");
				list.Add("Plank Cabin");
				list.Add("Rustic Cabin");
				list.Add("Trailer Cabin");
				list.Add("Neighbor Cabin");
				list.Add("Beach Cabin");
				break;
			case 1:
				list.Add("Beach Cabin");
				list.Add("Plank Cabin");
				list.Add("Log Cabin");
				list.Add("Neighbor Cabin");
				list.Add("Trailer Cabin");
				list.Add("Stone Cabin");
				list.Add("Rustic Cabin");
				break;
			default:
			{
				bool flag = Game1.random.NextBool();
				list.Add(flag ? "Log Cabin" : "Plank Cabin");
				list.Add("Stone Cabin");
				list.Add(flag ? "Plank Cabin" : "Log Cabin");
				list.Add("Trailer Cabin");
				list.Add("Neighbor Cabin");
				list.Add("Rustic Cabin");
				list.Add("Beach Cabin");
				break;
			}
			}
			List<Vector2> list2 = new List<Vector2>();
			for (int i = 0; i < _startingCabinLocations.Count; i++)
			{
				for (int j = 0; j < _startingCabinLocations.Count; j++)
				{
					if (doesTileHavePropertyNoNull((int)_startingCabinLocations[j].X, (int)_startingCabinLocations[j].Y, "Order", "Paths").Equals((i + 1).ToString() ?? ""))
					{
						list2.Add(_startingCabinLocations[j]);
					}
				}
			}
			for (int k = 0; k < list2.Count; k++)
			{
				removeObjectsAndSpawned((int)list2[k].X, (int)list2[k].Y, 5, 3);
				removeObjectsAndSpawned((int)list2[k].X + 2, (int)list2[k].Y + 3, 1, 1);
				Building building = new Building("Cabin", list2[k]);
				building.magical.Value = true;
				building.skinId.Value = list[k % list.Count];
				building.daysOfConstructionLeft.Value = 0;
				building.load();
				buildStructure(building, list2[k], Game1.player, skipSafetyChecks: true);
				building.removeOverlappingBushes(this);
			}
		}
		_startingCabinLocations.Clear();
	}

	public void updateDoors()
	{
		if (Game1.IsClient)
		{
			return;
		}
		doors.Clear();
		Layer layer = map.RequireLayer("Buildings");
		int i = 0;
		for (int layerHeight = layer.LayerHeight; i < layerHeight; i++)
		{
			int j = 0;
			for (int layerWidth = layer.LayerWidth; j < layerWidth; j++)
			{
				Tile tile = layer.Tiles[j, i];
				if (tile == null || !tile.Properties.TryGetValue("Action", out var value) || !value.ToString().Contains("Warp"))
				{
					continue;
				}
				string[] array = ArgUtility.SplitBySpace(value.ToString());
				string text = ArgUtility.Get(array, 0);
				switch (text)
				{
				case "WarpBoatTunnel":
					doors.Add(new Point(j, i), new NetString("BoatTunnel"));
					continue;
				case "WarpCommunityCenter":
					doors.Add(new Point(j, i), new NetString("CommunityCenter"));
					continue;
				case "Warp_Sunroom_Door":
					doors.Add(new Point(j, i), new NetString("Sunroom"));
					continue;
				case "WarpMensLocker":
				case "LockedDoorWarp":
				case "Warp":
				case "WarpWomensLocker":
					break;
				default:
					if (!text.Contains("Warp"))
					{
						continue;
					}
					Game1.log.Warn($"{NameOrUniqueName} ({j}, {i}) has unknown warp property '{value}', parsing with legacy logic.");
					break;
				}
				if (!(name.Value == "Mountain") || j != 8 || i != 20)
				{
					string text2 = ArgUtility.Get(array, 3);
					if (text2 != null)
					{
						doors.Add(new Point(j, i), new NetString(text2));
					}
				}
			}
		}
	}

	[Obsolete("Use removeObjectsAndSpawned instead.")]
	private void clearArea(int startingX, int startingY, int width, int height)
	{
		removeObjectsAndSpawned(startingX, startingY, width, height);
	}

	public bool isTerrainFeatureAt(int x, int y)
	{
		Vector2 key = new Vector2(x, y);
		if (terrainFeatures.TryGetValue(key, out var value) && !value.isPassable())
		{
			return true;
		}
		if (largeTerrainFeatures != null)
		{
			Microsoft.Xna.Framework.Rectangle value2 = new Microsoft.Xna.Framework.Rectangle(x * 64, y * 64, 64, 64);
			foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
			{
				if (largeTerrainFeature.getBoundingBox().Intersects(value2))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void loadLights()
	{
		if ((isOutdoors.Value && !Game1.isFestival() && !forceLoadPathLayerLights) || this is FarmHouse || this is IslandFarmHouse)
		{
			return;
		}
		Layer layer = map.GetLayer("Paths");
		Layer layer2 = map.RequireLayer("Front");
		Layer layer3 = map.RequireLayer("Buildings");
		for (int i = 0; i < map.Layers[0].LayerWidth; i++)
		{
			for (int j = 0; j < map.Layers[0].LayerHeight; j++)
			{
				int tileIndexAt;
				if (!isOutdoors.Value && !map.Properties.ContainsKey("IgnoreLightingTiles"))
				{
					tileIndexAt = layer2.GetTileIndexAt(i, j);
					if (tileIndexAt != -1)
					{
						adjustMapLightPropertiesForLamp(tileIndexAt, i, j, "Front");
					}
					tileIndexAt = layer3.GetTileIndexAt(i, j);
					if (tileIndexAt != -1)
					{
						adjustMapLightPropertiesForLamp(tileIndexAt, i, j, "Buildings");
					}
				}
				tileIndexAt = layer?.GetTileIndexAt(i, j) ?? (-1);
				if (tileIndexAt != -1)
				{
					adjustMapLightPropertiesForLamp(tileIndexAt, i, j, "Paths");
				}
			}
		}
	}

	public bool isFarmBuildingInterior()
	{
		return this is AnimalHouse;
	}

	/// <summary>Get whether this location is actively synced to the current player.</summary>
	/// <remarks>This is always true for the main player, and based on <see cref="M:StardewValley.Multiplayer.isActiveLocation(StardewValley.GameLocation)" /> for farmhands.</remarks>
	public bool IsActiveLocation()
	{
		if (Game1.IsMasterGame)
		{
			return true;
		}
		if (Root?.Value != null)
		{
			return Game1.multiplayer.isActiveLocation(this);
		}
		return false;
	}

	public virtual bool CanBeRemotedlyViewed()
	{
		return Game1.multiplayer.isAlwaysActiveLocation(this);
	}

	protected void adjustMapLightPropertiesForLamp(int tile, int x, int y, string layer)
	{
		string tileSheetIDAt = getTileSheetIDAt(x, y, layer);
		if (isFarmBuildingInterior())
		{
			if (tileSheetIDAt == "Coop" || tileSheetIDAt == "barn")
			{
				switch (tile)
				{
				case 24:
					changeMapProperties("DayTiles", layer + " " + x + " " + y + " " + tile);
					changeMapProperties("NightTiles", layer + " " + x + " " + y + " " + 26);
					changeMapProperties("WindowLight", x + " " + (y + 1) + " 4");
					changeMapProperties("WindowLight", x + " " + (y + 3) + " 4");
					break;
				case 25:
					changeMapProperties("DayTiles", layer + " " + x + " " + y + " " + tile);
					changeMapProperties("NightTiles", layer + " " + x + " " + y + " " + 12);
					break;
				case 46:
					changeMapProperties("DayTiles", layer + " " + x + " " + y + " " + tile);
					changeMapProperties("NightTiles", layer + " " + x + " " + y + " " + 53);
					break;
				}
			}
		}
		else if (tile == 8 && layer == "Paths")
		{
			changeMapProperties("Light", x + " " + y + " 4");
		}
		else
		{
			if (!(tileSheetIDAt == "indoor"))
			{
				return;
			}
			switch (tile)
			{
			case 1346:
				changeMapProperties("DayTiles", "Front " + x + " " + y + " " + tile);
				changeMapProperties("NightTiles", "Front " + x + " " + y + " " + 1347);
				changeMapProperties("DayTiles", "Buildings " + x + " " + (y + 1) + " " + 452);
				changeMapProperties("NightTiles", "Buildings " + x + " " + (y + 1) + " " + 453);
				changeMapProperties("Light", x + " " + y + " 4");
				break;
			case 480:
				changeMapProperties("DayTiles", layer + " " + x + " " + y + " " + tile);
				changeMapProperties("NightTiles", layer + " " + x + " " + y + " " + 809);
				changeMapProperties("Light", x + " " + y + " 4");
				break;
			case 826:
				changeMapProperties("DayTiles", layer + " " + x + " " + y + " " + tile);
				changeMapProperties("NightTiles", layer + " " + x + " " + y + " " + 827);
				changeMapProperties("Light", x + " " + y + " 4");
				break;
			case 1344:
				changeMapProperties("DayTiles", layer + " " + x + " " + y + " " + tile);
				changeMapProperties("NightTiles", layer + " " + x + " " + y + " " + 1345);
				changeMapProperties("Light", x + " " + y + " 4");
				break;
			case 256:
				changeMapProperties("DayTiles", layer + " " + x + " " + y + " " + tile);
				changeMapProperties("NightTiles", layer + " " + x + " " + y + " " + 1253);
				changeMapProperties("DayTiles", layer + " " + x + " " + (y + 1) + " " + 288);
				changeMapProperties("NightTiles", layer + " " + x + " " + (y + 1) + " " + 1285);
				changeMapProperties("WindowLight", x + " " + y + " 4");
				changeMapProperties("WindowLight", x + " " + (y + 1) + " 4");
				break;
			case 225:
				if (!name.Value.Contains("BathHouse") && !name.Value.Contains("Club") && (!name.Equals("SeedShop") || (x != 36 && x != 37)))
				{
					changeMapProperties("DayTiles", layer + " " + x + " " + y + " " + tile);
					changeMapProperties("NightTiles", layer + " " + x + " " + y + " " + 1222);
					changeMapProperties("DayTiles", layer + " " + x + " " + (y + 1) + " " + 257);
					changeMapProperties("NightTiles", layer + " " + x + " " + (y + 1) + " " + 1254);
					changeMapProperties("WindowLight", x + " " + y + " 4");
					changeMapProperties("WindowLight", x + " " + (y + 1) + " 4");
				}
				break;
			}
		}
	}

	private void changeMapProperties(string propertyName, string toAdd)
	{
		try
		{
			if (!map.Properties.TryGetValue(propertyName, out var value))
			{
				map.Properties[propertyName] = new PropertyValue(toAdd);
			}
			else if (!value.ToString().Contains(toAdd))
			{
				string value2 = value + " " + toAdd;
				map.Properties[propertyName] = new PropertyValue(value2);
			}
		}
		catch
		{
		}
	}

	/// <summary>Log an error indicating that a map property could not be parsed.</summary>
	/// <param name="name">The name of the property that failed to parse.</param>
	/// <param name="value">The property value that failed to parse.</param>
	/// <param name="error">The error phrase indicating why it failed.</param>
	public void LogMapPropertyError(string name, string value, string error)
	{
		Game1.log.Error($"Can't parse map property '{name}' with value '{value}' in location '{NameOrUniqueName}': {error}.");
	}

	/// <summary>Log an error indicating that a map property could not be parsed.</summary>
	/// <param name="name">The name of the property that failed to parse.</param>
	/// <param name="value">The property value that failed to parse.</param>
	/// <param name="error">The error phrase indicating why it failed.</param>
	/// <param name="delimiter">The character used to delimit values in the property.</param>
	public void LogMapPropertyError(string name, string[] value, string error, char delimiter = ' ')
	{
		LogMapPropertyError(name, string.Join(delimiter, value), error);
	}

	/// <summary>Log an error indicating that a tile property could not be parsed.</summary>
	/// <param name="name">The name of the property that failed to parse.</param>
	/// <param name="layerId">The layer containing the tile.</param>
	/// <param name="x">The X tile position of the tile.</param>
	/// <param name="y">The Y tile position of the tile.</param>
	/// <param name="value">The property value that failed to parse.</param>
	/// <param name="error">The error phrase indicating why it failed.</param>
	public void LogTilePropertyError(string name, string layerId, int x, int y, string value, string error)
	{
		Game1.log.Error($"Can't parse tile property '{name}' at {layerId}:{x},{y} with value '{value}' in location '{NameOrUniqueName}': {error}.");
	}

	/// <summary>Log an error indicating that a tile property could not be parsed.</summary>
	/// <param name="name">The name of the property that failed to parse.</param>
	/// <param name="layerId">The layer containing the tile.</param>
	/// <param name="x">The X tile position of the tile.</param>
	/// <param name="y">The Y tile position of the tile.</param>
	/// <param name="value">The property value that failed to parse.</param>
	/// <param name="error">The error phrase indicating why it failed.</param>
	/// <param name="delimiter">The character used to delimit values in the property.</param>
	public void LogTilePropertyError(string name, string layerId, int x, int y, string[] value, string error, char delimiter = ' ')
	{
		LogTilePropertyError(name, layerId, x, y, string.Join(delimiter, value), error);
	}

	/// <summary>Log an error indicating that a tile <c>Action</c> property could not be parsed.</summary>
	/// <param name="action">The action arguments, including the <c>Action</c> prefix.</param>
	/// <param name="x">The tile X position containing the action.</param>
	/// <param name="y">The tile Y position containing the action.</param>
	/// <param name="error">The error phrase indicating why it failed.</param>
	public void LogTileActionError(string[] action, int x, int y, string error)
	{
		LogTilePropertyError("Action", "Buildings", x, y, action, error);
	}

	/// <summary>Log an error indicating that a tile <c>TouchAction</c> property could not be parsed.</summary>
	/// <param name="action">The action arguments, including the <c>TouchAction</c> prefix.</param>
	/// <param name="tile">The tile position containing the action.</param>
	/// <param name="error">The error phrase indicating why it failed.</param>
	public void LogTileTouchActionError(string[] action, Vector2 tile, string error)
	{
		LogTilePropertyError("TouchAction", "Back", (int)tile.X, (int)tile.Y, action, error);
	}

	public override bool Equals(object obj)
	{
		if (obj is GameLocation other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(GameLocation other)
	{
		if (other != null && isStructure.Get() == other.isStructure.Get())
		{
			return string.Equals(NameOrUniqueName, other.NameOrUniqueName, StringComparison.Ordinal);
		}
		return false;
	}
}
