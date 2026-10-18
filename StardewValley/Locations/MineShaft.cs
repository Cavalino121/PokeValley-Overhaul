using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Constants;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.Pathfinding;
using StardewValley.SpecialOrders;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley.Locations;

public class MineShaft : GameLocation
{
	public const int mineFrostLevel = 40;

	public const int mineLavaLevel = 80;

	public const int upperArea = 0;

	public const int jungleArea = 10;

	public const int frostArea = 40;

	public const int lavaArea = 80;

	public const int desertArea = 121;

	public const int bottomOfMineLevel = 120;

	public const int quarryMineShaft = 77377;

	public const int numberOfLevelsPerArea = 40;

	public const int mineFeature_barrels = 0;

	public const int mineFeature_chests = 1;

	public const int mineFeature_coalCart = 2;

	public const int mineFeature_elevator = 3;

	public const double chanceForColoredGemstone = 0.008;

	public const double chanceForDiamond = 0.0005;

	public const double chanceForPrismaticShard = 0.0005;

	public const int monsterLimit = 30;

	/// <summary>The main tile sheet ID for mine tiles.</summary>
	public const string MineTileSheetId = "mine";

	public static SerializableDictionary<int, MineInfo> permanentMineChanges = new SerializableDictionary<int, MineInfo>();

	public static int numberOfCraftedStairsUsedThisRun;

	public Random mineRandom = new Random();

	private LocalizedContentManager mineLoader = Game1.content.CreateTemporary();

	private int timeUntilElevatorLightUp;

	[XmlIgnore]
	public int loadedMapNumber;

	public int fogTime;

	public NetBool isFogUp = new NetBool();

	public static int timeSinceLastMusic = 200000;

	public bool ladderHasSpawned;

	public bool ghostAdded;

	public bool loadedDarkArea;

	public bool isFallingDownShaft;

	public Vector2 fogPos;

	private readonly NetBool elevatorShouldDing = new NetBool();

	public readonly NetString mapImageSource = new NetString();

	private readonly NetInt netMineLevel = new NetInt();

	private readonly NetIntDelta netStonesLeftOnThisLevel = new NetIntDelta();

	private readonly NetVector2 netTileBeneathLadder = new NetVector2();

	private readonly NetVector2 netTileBeneathElevator = new NetVector2();

	public readonly NetPoint calicoStatueSpot = new NetPoint();

	public readonly NetPoint recentlyActivatedCalicoStatue = new NetPoint();

	private readonly NetPoint netElevatorLightSpot = new NetPoint();

	private readonly NetBool netIsSlimeArea = new NetBool();

	private readonly NetBool netIsMonsterArea = new NetBool();

	private readonly NetBool netIsTreasureRoom = new NetBool();

	private readonly NetBool netIsDinoArea = new NetBool();

	private readonly NetBool netIsQuarryArea = new NetBool();

	private readonly NetBool netAmbientFog = new NetBool();

	private readonly NetColor netLighting = new NetColor(Color.White);

	private readonly NetColor netFogColor = new NetColor();

	private readonly NetVector2Dictionary<bool, NetBool> createLadderAtEvent = new NetVector2Dictionary<bool, NetBool>();

	private readonly NetPointDictionary<bool, NetBool> createLadderDownEvent = new NetPointDictionary<bool, NetBool>();

	private float fogAlpha;

	[XmlIgnore]
	public static ICue bugLevelLoop;

	public readonly NetBool rainbowLights = new NetBool(value: false);

	public readonly NetBool isLightingDark = new NetBool(value: false);

	/// <summary>The layout in <c>Maps/Mines</c> to use, or <c>null</c> to choose a random one based on the level.</summary>
	private readonly int? forceLayout;

	private LocalizedContentManager mapContent;

	/// <summary>The mine levels which are currently loaded and ready.</summary>
	/// <remarks>When removing a location from this list, code should call <see cref="M:StardewValley.Locations.MineShaft.OnRemoved" /> since it won't be called automatically.</remarks>
	public static List<MineShaft> activeMines = new List<MineShaft>();

	public static HashSet<int> mushroomLevelsGeneratedToday = new HashSet<int>();

	public static int totalCalicoStatuesActivatedToday;

	private int recentCalicoStatueEffect;

	private bool forceFirstTime;

	private static int deepestLevelOnCurrentDesertFestivalRun;

	private int lastLevelsDownFallen;

	private Microsoft.Xna.Framework.Rectangle fogSource = new Microsoft.Xna.Framework.Rectangle(640, 0, 64, 64);

	private List<Vector2> brownSpots = new List<Vector2>();

	private int lifespan;

	private bool hasAddedDesertFestivalStatue;

	public float calicoEggIconTimerShake;

	public static int lowestLevelReached
	{
		get
		{
			if (Game1.netWorldState.Value.LowestMineLevelForOrder >= 0)
			{
				if (Game1.netWorldState.Value.LowestMineLevelForOrder == 120)
				{
					return Math.Max(Game1.netWorldState.Value.LowestMineLevelForOrder, Game1.netWorldState.Value.LowestMineLevelForOrder);
				}
				return Game1.netWorldState.Value.LowestMineLevelForOrder;
			}
			return Game1.netWorldState.Value.LowestMineLevel;
		}
		set
		{
			if (Game1.netWorldState.Value.LowestMineLevelForOrder >= 0 && value <= 120)
			{
				Game1.netWorldState.Value.LowestMineLevelForOrder = value;
			}
			else if (Game1.player.hasSkullKey || value <= 120)
			{
				Game1.netWorldState.Value.LowestMineLevel = value;
			}
		}
	}

	public int mineLevel
	{
		get
		{
			return netMineLevel.Value;
		}
		set
		{
			netMineLevel.Value = value;
		}
	}

	public int stonesLeftOnThisLevel
	{
		get
		{
			return netStonesLeftOnThisLevel.Value;
		}
		set
		{
			netStonesLeftOnThisLevel.Value = value;
		}
	}

	public Vector2 tileBeneathLadder
	{
		get
		{
			return netTileBeneathLadder.Value;
		}
		set
		{
			netTileBeneathLadder.Value = value;
		}
	}

	public Vector2 tileBeneathElevator
	{
		get
		{
			return netTileBeneathElevator.Value;
		}
		set
		{
			netTileBeneathElevator.Value = value;
		}
	}

	public Point ElevatorLightSpot
	{
		get
		{
			return netElevatorLightSpot.Value;
		}
		set
		{
			netElevatorLightSpot.Value = value;
		}
	}

	public bool isSlimeArea
	{
		get
		{
			return netIsSlimeArea.Value;
		}
		set
		{
			netIsSlimeArea.Value = value;
		}
	}

	public bool isDinoArea
	{
		get
		{
			return netIsDinoArea.Value;
		}
		set
		{
			netIsDinoArea.Value = value;
		}
	}

	public bool isMonsterArea
	{
		get
		{
			return netIsMonsterArea.Value;
		}
		set
		{
			netIsMonsterArea.Value = value;
		}
	}

	public bool isQuarryArea
	{
		get
		{
			return netIsQuarryArea.Value;
		}
		set
		{
			netIsQuarryArea.Value = value;
		}
	}

	public bool ambientFog
	{
		get
		{
			return netAmbientFog.Value;
		}
		set
		{
			netAmbientFog.Value = value;
		}
	}

	public Color lighting
	{
		get
		{
			return netLighting.Value;
		}
		set
		{
			netLighting.Value = value;
		}
	}

	public Color fogColor
	{
		get
		{
			return netFogColor.Value;
		}
		set
		{
			netFogColor.Value = value;
		}
	}

	public int EnemyCount => characters.Count((NPC p) => p is Monster);

	public MineShaft()
		: this(0)
	{
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="level">The mine level.</param>
	/// <param name="forceLayout">The layout in <c>Maps/Mines</c> to use, or <c>null</c> to choose a random one based on the level.</param>
	public MineShaft(int level, int? forceLayout = null)
	{
		mineLevel = level;
		name.Value = GetLevelName(level);
		mapContent = Game1.game1.xTileContent.CreateTemporary();
		this.forceLayout = forceLayout;
		if (!Game1.IsMultiplayer && getMineArea() == 121)
		{
			base.ExtraMillisecondsPerInGameMinute = 200;
		}
	}

	public override string GetLocationContextId()
	{
		if (locationContextId == null)
		{
			locationContextId = ((mineLevel >= 121) ? "Desert" : "Default");
		}
		return base.GetLocationContextId();
	}

	public override bool CanPlaceThisFurnitureHere(Furniture furniture)
	{
		return false;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(netMineLevel, "netMineLevel").AddField(netStonesLeftOnThisLevel, "netStonesLeftOnThisLevel").AddField(netTileBeneathLadder, "netTileBeneathLadder")
			.AddField(netTileBeneathElevator, "netTileBeneathElevator")
			.AddField(netElevatorLightSpot, "netElevatorLightSpot")
			.AddField(netIsSlimeArea, "netIsSlimeArea")
			.AddField(netIsMonsterArea, "netIsMonsterArea")
			.AddField(netIsTreasureRoom, "netIsTreasureRoom")
			.AddField(netIsDinoArea, "netIsDinoArea")
			.AddField(netIsQuarryArea, "netIsQuarryArea")
			.AddField(netAmbientFog, "netAmbientFog")
			.AddField(netLighting, "netLighting")
			.AddField(netFogColor, "netFogColor")
			.AddField(createLadderAtEvent, "createLadderAtEvent")
			.AddField(createLadderDownEvent, "createLadderDownEvent")
			.AddField(mapImageSource, "mapImageSource")
			.AddField(rainbowLights, "rainbowLights")
			.AddField(isLightingDark, "isLightingDark")
			.AddField(elevatorShouldDing, "elevatorShouldDing")
			.AddField(isFogUp, "isFogUp")
			.AddField(calicoStatueSpot, "calicoStatueSpot")
			.AddField(recentlyActivatedCalicoStatue, "recentlyActivatedCalicoStatue");
		isFogUp.fieldChangeEvent += delegate(NetBool field, bool oldValue, bool newValue)
		{
			if (!oldValue && newValue)
			{
				if (Game1.currentLocation == this)
				{
					Game1.changeMusicTrack("none");
				}
				if (Game1.IsClient)
				{
					fogTime = 35000;
				}
			}
			else if (!newValue)
			{
				fogTime = 0;
			}
		};
		createLadderAtEvent.OnValueAdded += delegate(Vector2 v, bool b)
		{
			doCreateLadderAt(v);
		};
		createLadderDownEvent.OnValueAdded += doCreateLadderDown;
		mapImageSource.fieldChangeEvent += delegate(NetString field, string oldValue, string newValue)
		{
			if (newValue != null && newValue != oldValue)
			{
				base.Map.RequireTileSheet(0, "mine").ImageSource = newValue;
				base.Map.LoadTileSheets(Game1.mapDisplayDevice);
			}
		};
		recentlyActivatedCalicoStatue.fieldChangeEvent += calicoStatueActivated;
	}

	public void calicoStatueActivated(NetPoint field, Point oldVector, Point newVector)
	{
		if (newVector == Point.Zero)
		{
			return;
		}
		if (Game1.currentLocation != null && Game1.currentLocation.Equals(this))
		{
			Game1.playSound("openBox");
			temporarySprites.AddRange(Utility.sparkleWithinArea(new Microsoft.Xna.Framework.Rectangle((newVector.X - 1) * 64, (newVector.Y - 3) * 64, 192, 192), 20, Color.White, 50, 500));
			calicoEggIconTimerShake = 1500f;
			setMapTile(newVector.X, newVector.Y, 285, "Buildings", "mine");
			setMapTile(newVector.X, newVector.Y - 1, 269, "Front", "mine");
			setMapTile(newVector.X, newVector.Y - 2, 253, "Front", "mine");
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(20, 0, 19, 21), new Vector2(newVector.X * 64 - 4, ((float)newVector.Y - 2.5f) * 64f), flipped: false, 0f, Color.White)
			{
				motion = new Vector2(0f, -1f),
				yStopCoordinate = (int)(((float)newVector.Y - 3.25f) * 64f),
				scale = 4f,
				animationLength = 1,
				delayBeforeAnimationStart = 1500,
				totalNumberOfLoops = 10,
				interval = 300f,
				drawAboveAlwaysFront = true
			});
		}
		if (!Game1.IsMasterGame)
		{
			return;
		}
		Game1.player.team.calicoEggSkullCavernRating.Value++;
		totalCalicoStatuesActivatedToday++;
		Random random = Utility.CreateDaySaveRandom(totalCalicoStatuesActivatedToday);
		if (random.NextBool(0.51 + Game1.player.team.AverageDailyLuck(this)))
		{
			if (!tryToAddCalicoStatueEffect(random, 0.15, 10) && !tryToAddCalicoStatueEffect(random, 0.01, 17, effectCanStack: true) && !tryToAddCalicoStatueEffect(random, 0.05, 12, effectCanStack: true) && !tryToAddCalicoStatueEffect(random, 0.1, 15, effectCanStack: true) && !tryToAddCalicoStatueEffect(random, 0.2, 16, effectCanStack: true) && !tryToAddCalicoStatueEffect(random, 0.1, 14, effectCanStack: true) && !tryToAddCalicoStatueEffect(random, 0.5, 11, effectCanStack: true))
			{
				Game1.player.team.AddCalicoStatueEffect(13);
				signalCalicoStatueActivation(13);
			}
			return;
		}
		if (random.NextBool(0.2))
		{
			for (int i = 0; i < 30; i++)
			{
				int num = random.Next(4);
				if (!Game1.player.team.calicoStatueEffects.ContainsKey(num))
				{
					Game1.player.team.AddCalicoStatueEffect(num);
					signalCalicoStatueActivation(num);
					return;
				}
			}
		}
		if (!tryToAddCalicoStatueEffect(random, 0.1, 4) && !tryToAddCalicoStatueEffect(random, 0.1, 9) && !tryToAddCalicoStatueEffect(random, 0.1, 5) && !tryToAddCalicoStatueEffect(random, 0.1, 6) && !tryToAddCalicoStatueEffect(random, 0.2, 7, effectCanStack: true) && !tryToAddCalicoStatueEffect(random, 0.2, 8, effectCanStack: true))
		{
			Game1.player.team.AddCalicoStatueEffect(13);
			signalCalicoStatueActivation(13);
		}
	}

	private void signalCalicoStatueActivation(int whichEffect)
	{
		recentCalicoStatueEffect = whichEffect;
		if (Game1.IsMultiplayer)
		{
			Game1.multiplayer.globalChatInfoMessage("CalicoStatue_Activated", TokenStringBuilder.LocalizedText("Strings\\1_6_Strings:DF_Mine_CalicoStatue_Description_" + whichEffect));
		}
	}

	private bool tryToAddCalicoStatueEffect(Random r, double chance, int which, bool effectCanStack = false)
	{
		if (r.NextBool(chance) && (effectCanStack || !Game1.player.team.calicoStatueEffects.ContainsKey(which)))
		{
			Game1.player.team.AddCalicoStatueEffect(which);
			signalCalicoStatueActivation(which);
			return true;
		}
		return false;
	}

	public override bool AllowMapModificationsInResetState()
	{
		return true;
	}

	protected override LocalizedContentManager getMapLoader()
	{
		return mapContent;
	}

	private void setElevatorLit()
	{
		if (ElevatorLightSpot.X != -1 && ElevatorLightSpot.Y != -1)
		{
			setMapTile(ElevatorLightSpot.X, ElevatorLightSpot.Y, 48, "Buildings", "mine");
			Game1.currentLightSources.Add(new LightSource($"Mine_{mineLevel}_Elevator", 4, new Vector2(ElevatorLightSpot.X, ElevatorLightSpot.Y) * 64f, 2f, Color.Black, LightSource.LightContext.None, 0L, base.NameOrUniqueName));
			elevatorShouldDing.Value = false;
		}
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		bool num = Game1.currentLocation == this;
		if ((Game1.isMusicContextActiveButNotPlaying() || Game1.getMusicTrackName().Contains("Ambient")) && Game1.random.NextDouble() < 0.00195)
		{
			localSound("cavedrip");
		}
		if (timeUntilElevatorLightUp > 0)
		{
			timeUntilElevatorLightUp -= time.ElapsedGameTime.Milliseconds;
			if (timeUntilElevatorLightUp <= 0)
			{
				int? pitch = 0;
				localSound("crystal", null, pitch);
				setElevatorLit();
			}
		}
		if (calicoEggIconTimerShake > 0f)
		{
			calicoEggIconTimerShake -= (float)time.ElapsedGameTime.TotalMilliseconds;
		}
		if (num)
		{
			if (isFogUp.Value && Game1.shouldTimePass())
			{
				if (bugLevelLoop == null || bugLevelLoop.IsStopped)
				{
					Game1.playSound("bugLevelLoop", out bugLevelLoop);
				}
				if (fogAlpha < 1f)
				{
					if (Game1.shouldTimePass())
					{
						fogAlpha += 0.01f;
					}
					if (bugLevelLoop != null)
					{
						bugLevelLoop.SetVariable("Volume", fogAlpha * 100f);
						bugLevelLoop.SetVariable("Frequency", fogAlpha * 25f);
					}
				}
				else if (bugLevelLoop != null)
				{
					float num2 = (float)Math.Max(0.0, Math.Min(100.0, Math.Sin((double)((float)fogTime / 10000f) % (Math.PI * 200.0))));
					bugLevelLoop.SetVariable("Frequency", Math.Max(0f, Math.Min(100f, fogAlpha * 25f + num2 * 10f)));
				}
			}
			else if (fogAlpha > 0f)
			{
				if (Game1.shouldTimePass())
				{
					fogAlpha -= 0.01f;
				}
				if (bugLevelLoop != null)
				{
					bugLevelLoop.SetVariable("Volume", fogAlpha * 100f);
					bugLevelLoop.SetVariable("Frequency", Math.Max(0f, bugLevelLoop.GetVariable("Frequency") - 0.01f));
					if (fogAlpha <= 0f)
					{
						bugLevelLoop.Stop(AudioStopOptions.Immediate);
						bugLevelLoop = null;
					}
				}
			}
			if (fogAlpha > 0f || ambientFog)
			{
				fogPos = Game1.updateFloatingObjectPositionForMovement(current: new Vector2(Game1.viewport.X, Game1.viewport.Y), w: fogPos, previous: Game1.previousViewportPosition, speed: -1f);
				fogPos.X = (fogPos.X + 0.5f) % 256f;
				fogPos.Y = (fogPos.Y + 0.5f) % 256f;
			}
		}
		base.UpdateWhenCurrentLocation(time);
	}

	public override void cleanupBeforePlayerExit()
	{
		base.cleanupBeforePlayerExit();
		if (bugLevelLoop != null)
		{
			bugLevelLoop.Stop(AudioStopOptions.Immediate);
			bugLevelLoop = null;
		}
		if (!Game1.IsMultiplayer && mineLevel == 20)
		{
			Game1.changeMusicTrack("none");
		}
	}

	public Vector2 mineEntrancePosition(Farmer who)
	{
		if (!who.ridingMineElevator || tileBeneathElevator.Equals(Vector2.Zero))
		{
			return tileBeneathLadder;
		}
		return tileBeneathElevator;
	}

	private void generateContents()
	{
		ladderHasSpawned = false;
		loadLevel(mineLevel);
		chooseLevelType();
		findLadder();
		populateLevel();
	}

	public void chooseLevelType()
	{
		fogTime = 0;
		if (bugLevelLoop != null)
		{
			bugLevelLoop.Stop(AudioStopOptions.Immediate);
			bugLevelLoop = null;
		}
		ambientFog = false;
		rainbowLights.Value = false;
		isLightingDark.Value = false;
		Random random = Utility.CreateDaySaveRandom(Game1.stats.DaysPlayed, mineLevel, 4 * mineLevel);
		lighting = new Color(80, 80, 40);
		if (getMineArea() == 80)
		{
			lighting = new Color(100, 100, 50);
		}
		if (GetAdditionalDifficulty() > 0)
		{
			if (getMineArea() == 40)
			{
				lighting = new Color(230, 200, 90);
				ambientFog = true;
				fogColor = new Color(0, 80, 255) * 0.55f;
				if (mineLevel < 50)
				{
					lighting = new Color(100, 80, 40);
					ambientFog = false;
				}
			}
		}
		else if (random.NextDouble() < 0.3 && mineLevel > 2)
		{
			isLightingDark.Value = true;
			lighting = new Color(120, 120, 40);
			if (random.NextDouble() < 0.3)
			{
				lighting = new Color(150, 150, 60);
			}
		}
		if (random.NextDouble() < 0.15 && mineLevel > 5 && mineLevel != 120)
		{
			isLightingDark.Value = true;
			switch (getMineArea())
			{
			case 0:
			case 10:
				lighting = new Color(110, 110, 70);
				break;
			case 40:
				lighting = Color.Black;
				if (GetAdditionalDifficulty() > 0)
				{
					lighting = new Color(237, 212, 185);
				}
				break;
			case 80:
				lighting = new Color(90, 130, 70);
				break;
			}
		}
		if (random.NextDouble() < 0.035 && getMineArea() == 80 && mineLevel % 5 != 0 && !mushroomLevelsGeneratedToday.Contains(mineLevel))
		{
			rainbowLights.Value = true;
			mushroomLevelsGeneratedToday.Add(mineLevel);
		}
		if (isDarkArea() && mineLevel < 120)
		{
			isLightingDark.Value = true;
			lighting = ((getMineArea() == 80) ? new Color(70, 100, 100) : new Color(150, 150, 120));
			if (getMineArea() == 0)
			{
				ambientFog = true;
				fogColor = Color.Black;
			}
		}
		if (mineLevel == 100)
		{
			lighting = new Color(140, 140, 80);
		}
		if (getMineArea() == 121)
		{
			lighting = new Color(110, 110, 40);
			if (random.NextDouble() < 0.05)
			{
				lighting = (random.NextBool() ? new Color(30, 30, 0) : new Color(150, 150, 50));
			}
		}
		if (getMineArea() == 77377)
		{
			isLightingDark.Value = false;
			rainbowLights.Value = false;
			ambientFog = true;
			fogColor = Color.White * 0.4f;
			lighting = new Color(80, 80, 30);
		}
	}

	public static void yearUpdate()
	{
		permanentMineChanges.RemoveWhere((KeyValuePair<int, MineInfo> p) => p.Key > 120 || p.Key % 5 != 0);
		if (permanentMineChanges.TryGetValue(5, out var value))
		{
			value.platformContainersLeft = 6;
		}
		if (permanentMineChanges.TryGetValue(45, out value))
		{
			value.platformContainersLeft = 6;
		}
		if (permanentMineChanges.TryGetValue(85, out value))
		{
			value.platformContainersLeft = 6;
		}
	}

	private bool canAdd(int typeOfFeature, int numberSoFar)
	{
		if (permanentMineChanges.TryGetValue(mineLevel, out var value))
		{
			switch (typeOfFeature)
			{
			case 0:
				return value.platformContainersLeft > numberSoFar;
			case 1:
				return value.chestsLeft > numberSoFar;
			case 2:
				return value.coalCartsLeft > numberSoFar;
			case 3:
				return value.elevator == 0;
			}
		}
		return true;
	}

	public void updateMineLevelData(int feature, int amount = 1)
	{
		if (!permanentMineChanges.TryGetValue(mineLevel, out var value))
		{
			value = (permanentMineChanges[mineLevel] = new MineInfo());
			if (mineLevel == 5 || mineLevel == 45 || mineLevel == 85)
			{
				forceFirstTime = true;
			}
		}
		switch (feature)
		{
		case 0:
			value.platformContainersLeft += amount;
			break;
		case 1:
			value.chestsLeft += amount;
			break;
		case 2:
			value.coalCartsLeft += amount;
			break;
		case 3:
			value.elevator += amount;
			break;
		}
	}

	public void chestConsumed()
	{
		Game1.player.chestConsumedMineLevels[mineLevel] = true;
	}

	public bool isLevelSlimeArea()
	{
		return isSlimeArea;
	}

	public void checkForMapAlterations(int x, int y)
	{
		if (getTileIndexAt(x, y, "Buildings", "mine") == 194 && !canAdd(2, 0))
		{
			setMapTile(x, y, 195, "Buildings", "mine");
			setMapTile(x, y - 1, 179, "Front", "mine");
		}
	}

	public void findLadder()
	{
		int num = 0;
		tileBeneathElevator = Vector2.Zero;
		bool flag = mineLevel % 20 == 0;
		lightGlows.Clear();
		Layer layer = map.RequireLayer("Buildings");
		for (int i = 0; i < layer.LayerHeight; i++)
		{
			for (int j = 0; j < layer.LayerWidth; j++)
			{
				int tileIndexAt = layer.GetTileIndexAt(j, i, "mine");
				if (tileIndexAt != -1)
				{
					switch (tileIndexAt)
					{
					case 115:
					{
						string text = $"Mines_{mineLevel}_{j}_{i}";
						tileBeneathLadder = new Vector2(j, i + 1);
						sharedLights.AddLight(new LightSource(text + "_1", 4, new Vector2(j, i - 2) * 64f + new Vector2(32f, 0f), 0.25f, new Color(0, 20, 50), LightSource.LightContext.None, 0L, base.NameOrUniqueName));
						sharedLights.AddLight(new LightSource(text + "_2", 4, new Vector2(j, i - 1) * 64f + new Vector2(32f, 0f), 0.5f, new Color(0, 20, 50), LightSource.LightContext.None, 0L, base.NameOrUniqueName));
						sharedLights.AddLight(new LightSource(text + "_3", 4, new Vector2(j, i) * 64f + new Vector2(32f, 0f), 0.75f, new Color(0, 20, 50), LightSource.LightContext.None, 0L, base.NameOrUniqueName));
						sharedLights.AddLight(new LightSource(text + "_4", 4, new Vector2(j, i + 1) * 64f + new Vector2(32f, 0f), 1f, new Color(0, 20, 50), LightSource.LightContext.None, 0L, base.NameOrUniqueName));
						num++;
						break;
					}
					case 112:
						tileBeneathElevator = new Vector2(j, i + 1);
						num++;
						break;
					}
					if (lighting.Equals(Color.White) && num == 2 && !flag)
					{
						return;
					}
					if (!lighting.Equals(Color.White))
					{
						switch (tileIndexAt)
						{
						case 48:
						case 65:
						case 66:
						case 81:
						case 82:
						case 97:
						case 113:
							sharedLights.AddLight(new LightSource($"Mines_{mineLevel}_{j}_{i}_5", 4, new Vector2(j, i) * 64f, 2.5f, new Color(0, 50, 100), LightSource.LightContext.None, 0L, base.NameOrUniqueName));
							switch (tileIndexAt)
							{
							case 66:
								lightGlows.Add(new Vector2(j, i) * 64f + new Vector2(0f, 64f));
								break;
							case 97:
							case 113:
								lightGlows.Add(new Vector2(j, i) * 64f + new Vector2(32f, 32f));
								break;
							}
							break;
						}
					}
				}
				if (Game1.IsMasterGame && isWaterTile(j, i) && getMineArea() == 80 && Game1.random.NextDouble() < 0.1)
				{
					sharedLights.AddLight(new LightSource($"Mines_{mineLevel}_{j}_{i}_Lava", 4, new Vector2(j, i) * 64f, 2f, new Color(0, 220, 220), LightSource.LightContext.None, 0L, base.NameOrUniqueName));
				}
			}
		}
		if (isFallingDownShaft)
		{
			Vector2 v = default(Vector2);
			while (!isTileClearForMineObjects(v))
			{
				v.X = Game1.random.Next(1, map.Layers[0].LayerWidth);
				v.Y = Game1.random.Next(1, map.Layers[0].LayerHeight);
			}
			tileBeneathLadder = v;
			Game1.player.showFrame(5);
		}
		isFallingDownShaft = false;
	}

	public override void performTenMinuteUpdate(int timeOfDay)
	{
		base.performTenMinuteUpdate(timeOfDay);
		if (mustKillAllMonstersToAdvance() && EnemyCount == 0)
		{
			Vector2 p = new Vector2((int)tileBeneathLadder.X, (int)tileBeneathLadder.Y);
			if (!hasTileAt((int)p.X, (int)p.Y, "Buildings"))
			{
				createLadderAt(p, "newArtifact");
				if (mustKillAllMonstersToAdvance() && Game1.player.currentLocation == this)
				{
					Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:MineShaft.cs.9484"));
				}
			}
		}
		if (isFogUp.Value || map == null || mineLevel % 5 == 0 || !(Game1.random.NextDouble() < 0.1) || AnyOnlineFarmerHasBuff("23"))
		{
			return;
		}
		if (mineLevel > 10 && !mustKillAllMonstersToAdvance() && Game1.random.NextDouble() < 0.11 && getMineArea() != 77377)
		{
			isFogUp.Value = true;
			fogTime = 35000 + Game1.random.Next(-5, 6) * 1000;
			switch (getMineArea())
			{
			case 121:
				fogColor = Color.BlueViolet * 1f;
				break;
			case 0:
			case 10:
				if (GetAdditionalDifficulty() > 0)
				{
					fogColor = (isDarkArea() ? new Color(255, 150, 0) : (Color.Cyan * 0.75f));
				}
				else
				{
					fogColor = (isDarkArea() ? Color.Khaki : (Color.Green * 0.75f));
				}
				break;
			case 40:
				fogColor = Color.Blue * 0.75f;
				break;
			case 80:
				fogColor = Color.Red * 0.5f;
				break;
			}
		}
		else
		{
			spawnFlyingMonsterOffScreen();
		}
	}

	public void spawnFlyingMonsterOffScreen()
	{
		Vector2 zero = Vector2.Zero;
		switch (Game1.random.Next(4))
		{
		case 0:
			zero.X = Game1.random.Next(map.Layers[0].LayerWidth);
			break;
		case 3:
			zero.Y = Game1.random.Next(map.Layers[0].LayerHeight);
			break;
		case 1:
			zero.X = map.Layers[0].LayerWidth - 1;
			zero.Y = Game1.random.Next(map.Layers[0].LayerHeight);
			break;
		case 2:
			zero.Y = map.Layers[0].LayerHeight - 1;
			zero.X = Game1.random.Next(map.Layers[0].LayerWidth);
			break;
		}
		if (Utility.isOnScreen(zero * 64f, 64))
		{
			zero.X -= Game1.viewport.Width / 64;
		}
		switch (getMineArea())
		{
		case 0:
			if (mineLevel > 10 && isDarkArea())
			{
				characters.Add(BuffMonsterIfNecessary(new Bat(zero * 64f, mineLevel)
				{
					focusedOnFarmers = true
				}));
				playSound("batScreech");
			}
			break;
		case 10:
			if (GetAdditionalDifficulty() > 0)
			{
				characters.Add(BuffMonsterIfNecessary(new BlueSquid(zero * 64f)
				{
					focusedOnFarmers = true
				}));
			}
			else
			{
				characters.Add(BuffMonsterIfNecessary(new Fly(zero * 64f)
				{
					focusedOnFarmers = true
				}));
			}
			break;
		case 40:
			characters.Add(BuffMonsterIfNecessary(new Bat(zero * 64f, mineLevel)
			{
				focusedOnFarmers = true
			}));
			playSound("batScreech");
			break;
		case 80:
			characters.Add(BuffMonsterIfNecessary(new Bat(zero * 64f, mineLevel)
			{
				focusedOnFarmers = true
			}));
			playSound("batScreech");
			break;
		case 121:
			if (mineLevel < 171 || Game1.random.NextBool())
			{
				characters.Add(BuffMonsterIfNecessary((GetAdditionalDifficulty() > 0) ? new Serpent(zero * 64f, "Royal Serpent")
				{
					focusedOnFarmers = true
				} : new Serpent(zero * 64f)
				{
					focusedOnFarmers = true
				}));
				playSound("serpentDie");
			}
			else
			{
				characters.Add(BuffMonsterIfNecessary(new Bat(zero * 64f, mineLevel)
				{
					focusedOnFarmers = true
				}));
				playSound("batScreech");
			}
			break;
		case 77377:
			characters.Add(new Bat(zero * 64f, 77377)
			{
				focusedOnFarmers = true
			});
			playSound("rockGolemHit");
			break;
		}
	}

	public override void drawLightGlows(SpriteBatch b)
	{
		Color color;
		switch (getMineArea())
		{
		case 0:
			color = (isDarkArea() ? (Color.PaleGoldenrod * 0.5f) : (Color.PaleGoldenrod * 0.33f));
			break;
		case 80:
			color = (isDarkArea() ? (Color.Pink * 0.4f) : (Color.Red * 0.33f));
			break;
		case 40:
			color = Color.White * 0.65f;
			if (GetAdditionalDifficulty() > 0)
			{
				color = ((mineLevel % 40 >= 30) ? (new Color(220, 240, 255) * 0.8f) : (new Color(230, 225, 100) * 0.8f));
			}
			break;
		case 121:
			color = Color.White * 0.8f;
			if (isDinoArea)
			{
				color = Color.Orange * 0.5f;
			}
			break;
		default:
			color = Color.PaleGoldenrod * 0.33f;
			break;
		}
		foreach (Vector2 lightGlow in lightGlows)
		{
			if (rainbowLights.Value)
			{
				switch ((int)(lightGlow.X / 64f + lightGlow.Y / 64f) % 4)
				{
				case 0:
					color = Color.Red * 0.5f;
					break;
				case 1:
					color = Color.Yellow * 0.5f;
					break;
				case 2:
					color = Color.Cyan * 0.33f;
					break;
				case 3:
					color = Color.Lime * 0.45f;
					break;
				}
			}
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, lightGlow), new Microsoft.Xna.Framework.Rectangle(88, 1779, 30, 30), color, 0f, new Vector2(15f, 15f), 8f + (float)(96.0 * Math.Sin((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(lightGlow.X * 777f) + (double)(lightGlow.Y * 9746f)) % 3140.0 / 1000.0) / 50.0), SpriteEffects.None, 1f);
		}
	}

	public Monster BuffMonsterIfNecessary(Monster monster)
	{
		if (monster != null && monster.GetBaseDifficultyLevel() < GetAdditionalDifficulty())
		{
			monster.BuffForAdditionalDifficulty(GetAdditionalDifficulty() - monster.GetBaseDifficultyLevel());
			if (monster is GreenSlime greenSlime)
			{
				if (mineLevel < 40)
				{
					greenSlime.color.Value = new Color(Game1.random.Next(40, 70), Game1.random.Next(100, 190), 255);
				}
				else if (mineLevel < 80)
				{
					greenSlime.color.Value = new Color(0, 180, 120);
				}
				else if (mineLevel < 120)
				{
					greenSlime.color.Value = new Color(Game1.random.Next(180, 250), 20, 120);
				}
				else
				{
					greenSlime.color.Value = new Color(Game1.random.Next(120, 180), 20, 255);
				}
			}
			setMonsterTextureToDangerousVersion(monster);
		}
		return monster;
	}

	private void setMonsterTextureToDangerousVersion(Monster monster)
	{
		string text = monster.Sprite.textureName.Value + "_dangerous";
		if (!Game1.content.DoesAssetExist<Texture2D>(text))
		{
			return;
		}
		try
		{
			monster.Sprite.LoadTexture(text);
		}
		catch (Exception exception)
		{
			Game1.log.Error($"Failed loading '{text}' texture for dangerous {monster.Name}.", exception);
		}
	}

	public override Item getFish(float millisecondsAfterNibble, string bait, int waterDepth, Farmer who, double baitPotency, Vector2 bobberTile, string locationName = null)
	{
		if (!(who?.CurrentTool is FishingRod fishingRod) || !fishingRod.QualifiedItemId.Contains("TrainingRod"))
		{
			string text = null;
			double num = 1.0;
			num += 0.4 * (double)who.FishingLevel;
			num += (double)waterDepth * 0.1;
			string text2 = "";
			if (who?.CurrentTool is FishingRod fishingRod2)
			{
				if (fishingRod2.HasCuriosityLure())
				{
					num += 5.0;
				}
				text2 = fishingRod2.GetBait()?.Name ?? "";
			}
			switch (getMineArea())
			{
			case 0:
			case 10:
				num += (double)(text2.Contains("Stonefish") ? 10 : 0);
				if (Game1.random.NextDouble() < 0.02 + 0.01 * num)
				{
					text = "(O)158";
				}
				break;
			case 40:
				num += (double)(text2.Contains("Ice Pip") ? 10 : 0);
				if (Game1.random.NextDouble() < 0.015 + 0.009 * num)
				{
					text = "(O)161";
				}
				break;
			case 80:
				num += (double)(text2.Contains("Lava Eel") ? 10 : 0);
				if (Game1.random.NextDouble() < 0.01 + 0.008 * num)
				{
					text = "(O)162";
				}
				break;
			}
			int quality = 0;
			if (Game1.random.NextDouble() < (double)((float)who.FishingLevel / 10f))
			{
				quality = 1;
			}
			if (Game1.random.NextDouble() < (double)((float)who.FishingLevel / 50f + (float)who.LuckLevel / 100f))
			{
				quality = 2;
			}
			if (text != null)
			{
				return ItemRegistry.Create(text, 1, quality);
			}
			if (getMineArea() == 80)
			{
				if (Game1.random.NextDouble() < 0.05 + (double)who.LuckLevel * 0.05)
				{
					return ItemRegistry.Create("(O)CaveJelly");
				}
				return ItemRegistry.Create("(O)" + Game1.random.Next(167, 173));
			}
			return base.getFish(millisecondsAfterNibble, bait, waterDepth, who, baitPotency, bobberTile, "UndergroundMine");
		}
		return ItemRegistry.Create("(O)" + Game1.random.Next(167, 173));
	}

	private void adjustLevelChances(ref double stoneChance, ref double monsterChance, ref double itemChance, ref double gemStoneChance)
	{
		if (mineLevel == 1)
		{
			monsterChance = 0.0;
			itemChance = 0.0;
			gemStoneChance = 0.0;
		}
		else if (mineLevel % 5 == 0 && getMineArea() != 121)
		{
			itemChance = 0.0;
			gemStoneChance = 0.0;
			if (mineLevel % 10 == 0)
			{
				monsterChance = 0.0;
			}
		}
		if (mustKillAllMonstersToAdvance())
		{
			monsterChance = 0.025;
			itemChance = 0.001;
			stoneChance = 0.0;
			gemStoneChance = 0.0;
			if (isDinoArea)
			{
				itemChance *= 4.0;
			}
		}
		monsterChance += 0.02 * (double)GetAdditionalDifficulty();
		bool num = AnyOnlineFarmerHasBuff("23");
		bool flag = AnyOnlineFarmerHasBuff("24");
		if (num && getMineArea() != 121)
		{
			if (!flag)
			{
				monsterChance = 0.0;
			}
		}
		else if (flag)
		{
			monsterChance *= 2.0;
		}
		gemStoneChance /= 2.0;
		if (isQuarryArea || getMineArea() == 77377)
		{
			gemStoneChance = 0.001;
			itemChance = 0.0001;
			stoneChance *= 2.0;
			monsterChance = 0.02;
		}
		if (GetAdditionalDifficulty() > 0 && getMineArea() == 40)
		{
			monsterChance *= 0.6600000262260437;
		}
		if (Utility.GetDayOfPassiveFestival("DesertFestival") <= 0 || getMineArea() != 121)
		{
			return;
		}
		double num2 = 1.0;
		int[] calicoStatueInvasionIds = DesertFestival.CalicoStatueInvasionIds;
		foreach (int key in calicoStatueInvasionIds)
		{
			if (Game1.player.team.calicoStatueEffects.TryGetValue(key, out var value))
			{
				monsterChance += (double)value * 0.01;
			}
		}
		if (Game1.player.team.calicoStatueEffects.TryGetValue(7, out var value2))
		{
			num2 += (double)value2 * 0.2;
		}
		monsterChance *= num2;
	}

	public bool AnyOnlineFarmerHasBuff(string which_buff)
	{
		if (which_buff == "23" && GetAdditionalDifficulty() > 0)
		{
			return false;
		}
		foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
		{
			if (onlineFarmer.hasBuff(which_buff))
			{
				return true;
			}
		}
		return false;
	}

	private void populateLevel()
	{
		objects.Clear();
		terrainFeatures.Clear();
		resourceClumps.Clear();
		debris.Clear();
		characters.Clear();
		ghostAdded = false;
		stonesLeftOnThisLevel = 0;
		if (mineLevel == 77377)
		{
			resourceClumps.Add(new ResourceClump(148, 2, 2, new Vector2(47f, 37f), null, "TileSheets\\Objects_2"));
			resourceClumps.Add(new ResourceClump(148, 2, 2, new Vector2(36f, 12f), null, "TileSheets\\Objects_2"));
		}
		double stoneChance = (double)mineRandom.Next(10, 30) / 100.0;
		double monsterChance = 0.002 + (double)mineRandom.Next(200) / 10000.0;
		double itemChance = 0.0025;
		double gemStoneChance = 0.003;
		adjustLevelChances(ref stoneChance, ref monsterChance, ref itemChance, ref gemStoneChance);
		int num = 0;
		bool flag = !permanentMineChanges.ContainsKey(mineLevel) || forceFirstTime;
		float num2 = 0f;
		if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && mineLevel > 131)
		{
			num2 += 1f - 130f / (float)mineLevel;
		}
		if (mineLevel > 1 && (mineLevel % 5 != 0 || mineLevel >= 121) && (mineRandom.NextBool() || isDinoArea))
		{
			Layer layer = map.RequireLayer("Back");
			int num3 = mineRandom.Next(5) + (int)(Game1.player.team.AverageDailyLuck(this) * 20.0);
			if (isDinoArea)
			{
				num3 += map.Layers[0].LayerWidth * map.Layers[0].LayerHeight / 40;
			}
			for (int i = 0; i < num3; i++)
			{
				Point value;
				Point point;
				if (mineRandom.NextDouble() < 0.33 + (double)(num2 / 2f))
				{
					value = new Point(mineRandom.Next(layer.LayerWidth), 0);
					point = new Point(0, 1);
				}
				else if (mineRandom.NextBool())
				{
					value = new Point(0, mineRandom.Next(layer.LayerHeight));
					point = new Point(1, 0);
				}
				else
				{
					value = new Point(layer.LayerWidth - 1, mineRandom.Next(layer.LayerHeight));
					point = new Point(-1, 0);
				}
				while (isTileOnMap(value.X, value.Y))
				{
					value.X += point.X;
					value.Y += point.Y;
					if (!isTileClearForMineObjects(value.X, value.Y))
					{
						continue;
					}
					Vector2 vector = new Vector2(value.X, value.Y);
					if (isDinoArea)
					{
						terrainFeatures.Add(vector, new CosmeticPlant(mineRandom.Next(3)));
					}
					else if (!mustKillAllMonstersToAdvance())
					{
						if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && getMineArea() == 121 && !hasAddedDesertFestivalStatue && hasTileAt((int)vector.X, (int)vector.Y - 1, "Buildings"))
						{
							calicoStatueSpot.Value = value;
							hasAddedDesertFestivalStatue = true;
						}
						else
						{
							objects.Add(vector, BreakableContainer.GetBarrelForMines(vector, this));
						}
					}
					break;
				}
			}
		}
		bool flag2 = false;
		if (mineLevel % 10 != 0 || (getMineArea() == 121 && !isForcedChestLevel(mineLevel) && !netIsTreasureRoom.Value))
		{
			Layer layer2 = map.RequireLayer("Back");
			for (int j = 0; j < layer2.LayerWidth; j++)
			{
				for (int k = 0; k < layer2.LayerHeight; k++)
				{
					checkForMapAlterations(j, k);
					if (isTileClearForMineObjects(j, k))
					{
						if (mineRandom.NextDouble() <= stoneChance)
						{
							Vector2 vector2 = new Vector2(j, k);
							if (base.Objects.ContainsKey(vector2))
							{
								continue;
							}
							if (getMineArea() == 40 && mineRandom.NextDouble() < 0.15)
							{
								int num4 = mineRandom.Next(319, 322);
								if (GetAdditionalDifficulty() > 0 && mineLevel % 40 < 30)
								{
									num4 = mineRandom.Next(313, 316);
								}
								base.Objects.Add(vector2, new Object(num4.ToString(), 1)
								{
									Fragility = 2,
									CanBeGrabbed = true
								});
								continue;
							}
							if (rainbowLights.Value && mineRandom.NextDouble() < 0.55)
							{
								if (mineRandom.NextDouble() < 0.25)
								{
									string itemId = ((mineRandom.Next(5) != 0) ? "(O)420" : "(O)422");
									Object obj = ItemRegistry.Create<Object>(itemId);
									obj.IsSpawnedObject = true;
									base.Objects.Add(vector2, obj);
								}
								continue;
							}
							Object obj2 = createLitterObject(0.001, 5E-05, gemStoneChance, vector2);
							if (obj2 != null)
							{
								base.Objects.Add(vector2, obj2);
								if (obj2.IsBreakableStone())
								{
									stonesLeftOnThisLevel++;
								}
							}
						}
						else if (mineRandom.NextDouble() <= monsterChance && getDistanceFromStart(j, k) > 5f)
						{
							Monster monster = null;
							if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && getMineArea() == 121)
							{
								int[] calicoStatueInvasionIds = DesertFestival.CalicoStatueInvasionIds;
								foreach (int num5 in calicoStatueInvasionIds)
								{
									if (!Game1.player.team.calicoStatueEffects.TryGetValue(num5, out var value2))
									{
										continue;
									}
									for (int m = 0; m < value2; m++)
									{
										if (mineRandom.NextBool(0.15))
										{
											Vector2 position = new Vector2(j, k) * 64f;
											switch (num5)
											{
											case 3:
												monster = new Bat(position, mineLevel);
												break;
											case 0:
												monster = new Ghost(position, "Carbon Ghost");
												break;
											case 1:
												monster = new Serpent(position);
												break;
											case 2:
												monster = ((!(mineRandom.NextDouble() < 0.33)) ? ((Monster)new Skeleton(position, mineRandom.NextBool())) : ((Monster)new Bat(position, 77377)));
												monster.BuffForAdditionalDifficulty(1);
												break;
											}
											break;
										}
									}
								}
							}
							if (monster == null)
							{
								monster = BuffMonsterIfNecessary(getMonsterForThisLevel(mineLevel, j, k));
							}
							if (!(monster is GreenSlime greenSlime))
							{
								if (!(monster is Leaper))
								{
									if (!(monster is Grub))
									{
										if (monster is DustSpirit)
										{
											if (mineRandom.NextDouble() < 0.6)
											{
												tryToAddMonster(BuffMonsterIfNecessary(new DustSpirit(Vector2.Zero)), j - 1, k);
											}
											if (mineRandom.NextDouble() < 0.6)
											{
												tryToAddMonster(BuffMonsterIfNecessary(new DustSpirit(Vector2.Zero)), j + 1, k);
											}
											if (mineRandom.NextDouble() < 0.6)
											{
												tryToAddMonster(BuffMonsterIfNecessary(new DustSpirit(Vector2.Zero)), j, k - 1);
											}
											if (mineRandom.NextDouble() < 0.6)
											{
												tryToAddMonster(BuffMonsterIfNecessary(new DustSpirit(Vector2.Zero)), j, k + 1);
											}
										}
									}
									else
									{
										if (mineRandom.NextDouble() < 0.4)
										{
											tryToAddMonster(BuffMonsterIfNecessary(new Grub(Vector2.Zero)), j - 1, k);
										}
										if (mineRandom.NextDouble() < 0.4)
										{
											tryToAddMonster(BuffMonsterIfNecessary(new Grub(Vector2.Zero)), j + 1, k);
										}
										if (mineRandom.NextDouble() < 0.4)
										{
											tryToAddMonster(BuffMonsterIfNecessary(new Grub(Vector2.Zero)), j, k - 1);
										}
										if (mineRandom.NextDouble() < 0.4)
										{
											tryToAddMonster(BuffMonsterIfNecessary(new Grub(Vector2.Zero)), j, k + 1);
										}
									}
								}
								else
								{
									float num6 = (float)(GetAdditionalDifficulty() + 1) * 0.3f;
									if (mineRandom.NextDouble() < (double)num6)
									{
										tryToAddMonster(BuffMonsterIfNecessary(new Leaper(Vector2.Zero)), j - 1, k);
									}
									if (mineRandom.NextDouble() < (double)num6)
									{
										tryToAddMonster(BuffMonsterIfNecessary(new Leaper(Vector2.Zero)), j + 1, k);
									}
									if (mineRandom.NextDouble() < (double)num6)
									{
										tryToAddMonster(BuffMonsterIfNecessary(new Leaper(Vector2.Zero)), j, k - 1);
									}
									if (mineRandom.NextDouble() < (double)num6)
									{
										tryToAddMonster(BuffMonsterIfNecessary(new Leaper(Vector2.Zero)), j, k + 1);
									}
								}
							}
							else
							{
								if (!flag2 && Game1.random.NextDouble() <= Math.Max(0.01, 0.012 + Game1.player.team.AverageDailyLuck(this) / 10.0) && Game1.player.team.SpecialOrderActive("Wizard2"))
								{
									greenSlime.makePrismatic();
									flag2 = true;
								}
								if (GetAdditionalDifficulty() > 0 && mineRandom.NextDouble() < (double)Math.Min((float)GetAdditionalDifficulty() * 0.1f, 0.5f))
								{
									if (mineRandom.NextDouble() < 0.009999999776482582)
									{
										greenSlime.stackedSlimes.Value = 4;
									}
									else
									{
										greenSlime.stackedSlimes.Value = 2;
									}
								}
							}
							if (mineRandom.NextDouble() < 0.00175)
							{
								monster.hasSpecialItem.Value = true;
							}
							if (monster.GetBoundingBox().Width <= 64 || isTileClearForMineObjects(j + 1, k))
							{
								characters.Add(monster);
							}
						}
						else if (mineRandom.NextDouble() <= itemChance)
						{
							Vector2 vector3 = new Vector2(j, k);
							base.Objects.Add(vector3, getRandomItemForThisLevel(mineLevel, vector3));
						}
						else if (mineRandom.NextDouble() <= 0.005 && !isDarkArea() && !mustKillAllMonstersToAdvance() && (GetAdditionalDifficulty() <= 0 || (getMineArea() == 40 && mineLevel % 40 < 30)))
						{
							if (!isTileClearForMineObjects(j + 1, k) || !isTileClearForMineObjects(j, k + 1) || !isTileClearForMineObjects(j + 1, k + 1))
							{
								continue;
							}
							Vector2 tile = new Vector2(j, k);
							int parentSheetIndex = mineRandom.Choose(752, 754);
							if (getMineArea() == 40)
							{
								if (GetAdditionalDifficulty() > 0)
								{
									parentSheetIndex = 600;
									if (mineRandom.NextDouble() < 0.1)
									{
										parentSheetIndex = 602;
									}
								}
								else
								{
									parentSheetIndex = mineRandom.Choose(756, 758);
								}
							}
							resourceClumps.Add(new ResourceClump(parentSheetIndex, 2, 2, tile));
						}
						else if (GetAdditionalDifficulty() > 0)
						{
							if (getMineArea() == 40 && mineLevel % 40 < 30 && mineRandom.NextDouble() < 0.01 && hasTileAt(j, k - 1, "Buildings"))
							{
								terrainFeatures.Add(new Vector2(j, k), new Tree("8", 5));
							}
							else if (getMineArea() == 40 && mineLevel % 40 < 30 && mineRandom.NextDouble() < 0.1 && (hasTileAt(j, k - 1, "Buildings") || hasTileAt(j - 1, k, "Buildings") || hasTileAt(j, k + 1, "Buildings") || hasTileAt(j + 1, k, "Buildings") || terrainFeatures.ContainsKey(new Vector2(j - 1, k)) || terrainFeatures.ContainsKey(new Vector2(j + 1, k)) || terrainFeatures.ContainsKey(new Vector2(j, k - 1)) || terrainFeatures.ContainsKey(new Vector2(j, k + 1))))
							{
								terrainFeatures.Add(new Vector2(j, k), new Grass((mineLevel >= 50) ? 6 : 5, (mineLevel >= 50) ? 1 : mineRandom.Next(1, 5)));
							}
							else if (getMineArea() == 80 && !isDarkArea() && mineRandom.NextDouble() < 0.1 && (hasTileAt(j, k - 1, "Buildings") || hasTileAt(j - 1, k, "Buildings") || hasTileAt(j, k + 1, "Buildings") || hasTileAt(j + 1, k, "Buildings") || terrainFeatures.ContainsKey(new Vector2(j - 1, k)) || terrainFeatures.ContainsKey(new Vector2(j + 1, k)) || terrainFeatures.ContainsKey(new Vector2(j, k - 1)) || terrainFeatures.ContainsKey(new Vector2(j, k + 1))))
							{
								terrainFeatures.Add(new Vector2(j, k), new Grass(4, mineRandom.Next(1, 5)));
							}
						}
					}
					else if (isContainerPlatform(j, k) && CanItemBePlacedHere(new Vector2(j, k)) && mineRandom.NextDouble() < 0.4 && (flag || canAdd(0, num)))
					{
						Vector2 vector4 = new Vector2(j, k);
						objects.Add(vector4, BreakableContainer.GetBarrelForMines(vector4, this));
						num++;
						if (flag)
						{
							updateMineLevelData(0);
						}
					}
					else
					{
						if (!(mineRandom.NextDouble() <= monsterChance) || !CanSpawnCharacterHere(new Vector2(j, k)) || !isTileOnClearAndSolidGround(j, k) || !(getDistanceFromStart(j, k) > 5f) || (AnyOnlineFarmerHasBuff("23") && getMineArea() != 121))
						{
							continue;
						}
						Monster monster2 = BuffMonsterIfNecessary(getMonsterForThisLevel(mineLevel, j, k));
						if (monster2.GetBoundingBox().Width <= 64 || isTileClearForMineObjects(j + 1, k))
						{
							if (mineRandom.NextDouble() < 0.01)
							{
								monster2.hasSpecialItem.Value = true;
							}
							characters.Add(monster2);
						}
					}
				}
			}
			if (stonesLeftOnThisLevel > 35)
			{
				int num7 = stonesLeftOnThisLevel / 35;
				for (int n = 0; n < num7; n++)
				{
					if (!Utility.TryGetRandom(objects, out var key, out var value3) || !value3.IsBreakableStone())
					{
						continue;
					}
					int num8 = mineRandom.Next(3, 8);
					bool flag3 = mineRandom.NextDouble() < 0.1;
					for (int num9 = (int)key.X - num8 / 2; (float)num9 < key.X + (float)(num8 / 2); num9++)
					{
						for (int num10 = (int)key.Y - num8 / 2; (float)num10 < key.Y + (float)(num8 / 2); num10++)
						{
							Vector2 key2 = new Vector2(num9, num10);
							if (!objects.TryGetValue(key2, out var value4) || !value4.IsBreakableStone())
							{
								continue;
							}
							objects.Remove(key2);
							stonesLeftOnThisLevel--;
							if (getDistanceFromStart(num9, num10) > 5f && flag3 && mineRandom.NextDouble() < 0.12)
							{
								Monster monster3 = BuffMonsterIfNecessary(getMonsterForThisLevel(mineLevel, num9, num10));
								if (monster3.GetBoundingBox().Width <= 64 || isTileClearForMineObjects(num9 + 1, num10))
								{
									characters.Add(monster3);
								}
							}
						}
					}
				}
			}
			tryToAddAreaUniques();
			if (mineRandom.NextDouble() < 0.95 && !mustKillAllMonstersToAdvance() && mineLevel > 1 && mineLevel % 5 != 0 && shouldCreateLadderOnThisLevel())
			{
				Vector2 v = new Vector2(mineRandom.Next(layer2.LayerWidth), mineRandom.Next(layer2.LayerHeight));
				if (isTileClearForMineObjects(v))
				{
					createLadderDown((int)v.X, (int)v.Y);
				}
			}
			if (mustKillAllMonstersToAdvance() && EnemyCount <= 1)
			{
				characters.Add(new Bat(tileBeneathLadder * 64f + new Vector2(256f, 256f)));
			}
		}
		if ((!mustKillAllMonstersToAdvance() || isDinoArea) && mineLevel % 5 != 0 && mineLevel > 2 && !isForcedChestLevel(mineLevel) && !netIsTreasureRoom.Value)
		{
			tryToAddOreClumps();
			if (isLightingDark.Value)
			{
				tryToAddOldMinerPath();
			}
		}
	}

	public void placeAppropriateOreAt(Vector2 tile)
	{
		if (CanItemBePlacedHere(tile, itemIsPassable: false, CollisionMask.All, CollisionMask.None))
		{
			objects.Add(tile, getAppropriateOre(tile));
		}
	}

	public Object getAppropriateOre(Vector2 tile)
	{
		Object result = new Object("751", 1)
		{
			MinutesUntilReady = 3
		};
		switch (getMineArea())
		{
		case 0:
		case 10:
			if (GetAdditionalDifficulty() > 0)
			{
				result = new Object("849", 1)
				{
					MinutesUntilReady = 6
				};
			}
			break;
		case 40:
			if (GetAdditionalDifficulty() > 0)
			{
				result = new ColoredObject("290", 1, new Color(150, 225, 160))
				{
					MinutesUntilReady = 6,
					TileLocation = tile,
					Flipped = mineRandom.NextBool()
				};
			}
			else if (mineRandom.NextDouble() < 0.8)
			{
				result = new Object("290", 1)
				{
					MinutesUntilReady = 4
				};
			}
			break;
		case 80:
			if (mineRandom.NextDouble() < 0.8)
			{
				result = new Object("764", 1)
				{
					MinutesUntilReady = 8
				};
			}
			break;
		case 121:
			if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && mineRandom.NextBool(0.25 + (double)((float)(Game1.player.team.calicoEggSkullCavernRating.Value * 5) / 100f)))
			{
				result = new Object("CalicoEggStone_" + mineRandom.Next(3), 1)
				{
					MinutesUntilReady = 8
				};
				break;
			}
			result = new Object("764", 1)
			{
				MinutesUntilReady = 8
			};
			if (mineRandom.NextDouble() < 0.02)
			{
				result = new Object("765", 1)
				{
					MinutesUntilReady = 16
				};
			}
			break;
		}
		if (mineRandom.NextDouble() < 0.25 && getMineArea() != 40 && GetAdditionalDifficulty() <= 0)
		{
			result = new Object(mineRandom.Choose("668", "670"), 1)
			{
				MinutesUntilReady = 2
			};
		}
		return result;
	}

	public void tryToAddOreClumps()
	{
		if (!(mineRandom.NextDouble() < 0.55 + Game1.player.team.AverageDailyLuck(this)))
		{
			return;
		}
		Vector2 randomTile = getRandomTile();
		for (int i = 0; i < 1 || mineRandom.NextDouble() < 0.25 + Game1.player.team.AverageDailyLuck(this); i++)
		{
			if (CanItemBePlacedHere(randomTile, itemIsPassable: false, CollisionMask.All, CollisionMask.None) && isTileOnClearAndSolidGround(randomTile) && doesTileHaveProperty((int)randomTile.X, (int)randomTile.Y, "Diggable", "Back") == null)
			{
				Object obj = getAppropriateOre(randomTile);
				if (obj.QualifiedItemId == "(O)670")
				{
					obj = new Object("668", 1);
				}
				bool flag = obj.QualifiedItemId == "(O)668";
				if (obj.QualifiedItemId.Contains("CalicoEgg"))
				{
					Utility.recursiveObjectPlacement(obj, (int)randomTile.X, (int)randomTile.Y, 0.949999988079071, 0.30000001192092896, this, "Dirt", 0, 0.05000000074505806, 1, new List<string> { "CalicoEggStone_0", "CalicoEggStone_1", "CalicoEggStone_2" });
				}
				else
				{
					Utility.recursiveObjectPlacement(obj, (int)randomTile.X, (int)randomTile.Y, 0.949999988079071, 0.30000001192092896, this, "Dirt", flag ? 1 : 0, 0.05000000074505806, (!flag) ? 1 : 2);
				}
			}
			randomTile = getRandomTile();
		}
	}

	public void tryToAddOldMinerPath()
	{
		Vector2 randomTile = getRandomTile();
		int num = 0;
		while (!isTileOnClearAndSolidGround(randomTile) && num < 8)
		{
			randomTile = getRandomTile();
			num++;
		}
		if (!isTileOnClearAndSolidGround(randomTile))
		{
			return;
		}
		Stack<Point> stack = PathFindController.findPath(Utility.Vector2ToPoint(tileBeneathLadder), Utility.Vector2ToPoint(randomTile), PathFindController.isAtEndPoint, this, Game1.player, 500);
		if (stack == null)
		{
			return;
		}
		while (stack.Count > 0)
		{
			Point point = stack.Pop();
			removeObjectsAndSpawned(point.X, point.Y, 1, 1);
			if (stack.Count <= 0 || !(mineRandom.NextDouble() < 0.2))
			{
				continue;
			}
			Vector2 vector = ((stack.Peek().X == point.X) ? new Vector2(point.X + mineRandom.Choose(-1, 1), point.Y) : new Vector2(point.X, point.Y + mineRandom.Choose(-1, 1)));
			if (!vector.Equals(Vector2.Zero) && CanItemBePlacedHere(vector) && isTileOnClearAndSolidGround(vector))
			{
				if (mineRandom.NextBool())
				{
					new Torch().placementAction(this, (int)vector.X * 64, (int)vector.Y * 64, null);
				}
				else
				{
					placeAppropriateOreAt(vector);
				}
			}
		}
	}

	public void tryToAddAreaUniques()
	{
		if ((getMineArea() != 10 && getMineArea() != 80 && (getMineArea() != 40 || !(mineRandom.NextDouble() < 0.1))) || isDarkArea() || mustKillAllMonstersToAdvance())
		{
			return;
		}
		int num = mineRandom.Next(7, 24);
		int num2 = ((getMineArea() == 80) ? 316 : ((getMineArea() == 40) ? 319 : 313));
		Color color = Color.White;
		int objectIndexAddRange = 2;
		if (GetAdditionalDifficulty() > 0)
		{
			if (getMineArea() == 10)
			{
				num2 = 674;
				color = new Color(30, 120, 255);
			}
			else if (getMineArea() == 40)
			{
				if (mineLevel % 40 >= 30)
				{
					num2 = 319;
				}
				else
				{
					num2 = 882;
					color = new Color(100, 180, 220);
				}
			}
			else if (getMineArea() == 80)
			{
				return;
			}
		}
		Layer layer = map.RequireLayer("Back");
		for (int i = 0; i < num; i++)
		{
			Vector2 tileLocation = new Vector2(mineRandom.Next(layer.LayerWidth), mineRandom.Next(layer.LayerHeight));
			if (color.Equals(Color.White))
			{
				Utility.recursiveObjectPlacement(new Object(num2.ToString(), 1)
				{
					Fragility = 2,
					CanBeGrabbed = true
				}, (int)tileLocation.X, (int)tileLocation.Y, 1.0, (float)mineRandom.Next(10, 40) / 100f, this, "Dirt", objectIndexAddRange, 0.29);
			}
			else
			{
				Utility.recursiveObjectPlacement(new ColoredObject(num2.ToString(), 1, color)
				{
					Fragility = 2,
					CanBeGrabbed = true,
					CanBeSetDown = true,
					TileLocation = tileLocation
				}, (int)tileLocation.X, (int)tileLocation.Y, 1.0, (float)mineRandom.Next(10, 40) / 100f, this, "Dirt", objectIndexAddRange, 0.29);
			}
		}
	}

	public bool tryToAddMonster(Monster m, int tileX, int tileY)
	{
		if (isTileClearForMineObjects(tileX, tileY) && !IsTileOccupiedBy(new Vector2(tileX, tileY)))
		{
			m.setTilePosition(tileX, tileY);
			characters.Add(m);
			return true;
		}
		return false;
	}

	public bool isContainerPlatform(int x, int y)
	{
		return getTileIndexAt(x, y, "Back", "mine") == 257;
	}

	public bool mustKillAllMonstersToAdvance()
	{
		if (!isSlimeArea && !isMonsterArea)
		{
			return isDinoArea;
		}
		return true;
	}

	public void createLadderAt(Vector2 p, string sound = "hoeHit")
	{
		if (shouldCreateLadderOnThisLevel())
		{
			playSound(sound);
			createLadderAtEvent[p] = true;
		}
	}

	public bool shouldCreateLadderOnThisLevel()
	{
		if (mineLevel != 77377)
		{
			return mineLevel != 120;
		}
		return false;
	}

	private void doCreateLadderAt(Vector2 p)
	{
		string startSound = ((Game1.currentLocation == this) ? "sandyStep" : null);
		updateMap();
		setMapTile((int)p.X, (int)p.Y, 173, "Buildings", "mine");
		temporarySprites.Add(new TemporaryAnimatedSprite(5, p * 64f, Color.White * 0.5f)
		{
			interval = 80f
		});
		temporarySprites.Add(new TemporaryAnimatedSprite(5, p * 64f - new Vector2(16f, 16f), Color.White * 0.5f)
		{
			delayBeforeAnimationStart = 150,
			interval = 80f,
			scale = 0.75f,
			startSound = startSound
		});
		temporarySprites.Add(new TemporaryAnimatedSprite(5, p * 64f + new Vector2(32f, 16f), Color.White * 0.5f)
		{
			delayBeforeAnimationStart = 300,
			interval = 80f,
			scale = 0.75f,
			startSound = startSound
		});
		temporarySprites.Add(new TemporaryAnimatedSprite(5, p * 64f - new Vector2(32f, -16f), Color.White * 0.5f)
		{
			delayBeforeAnimationStart = 450,
			interval = 80f,
			scale = 0.75f,
			startSound = startSound
		});
		temporarySprites.Add(new TemporaryAnimatedSprite(5, p * 64f - new Vector2(-16f, 16f), Color.White * 0.5f)
		{
			delayBeforeAnimationStart = 600,
			interval = 80f,
			scale = 0.75f,
			startSound = startSound
		});
		if (Game1.player.currentLocation == this)
		{
			Game1.player.TemporaryPassableTiles.Add(new Microsoft.Xna.Framework.Rectangle((int)p.X * 64, (int)p.Y * 64, 64, 64));
		}
	}

	public bool recursiveTryToCreateLadderDown(Vector2 centerTile, string sound = "hoeHit", int maxIterations = 16)
	{
		int i = 0;
		Queue<Vector2> queue = new Queue<Vector2>();
		queue.Enqueue(centerTile);
		List<Vector2> list = new List<Vector2>();
		for (; i < maxIterations; i++)
		{
			if (queue.Count <= 0)
			{
				break;
			}
			Vector2 vector = queue.Dequeue();
			list.Add(vector);
			if (!IsTileOccupiedBy(vector) && isTileOnClearAndSolidGround(vector) && doesTileHaveProperty((int)vector.X, (int)vector.Y, "Type", "Back") != null && doesTileHaveProperty((int)vector.X, (int)vector.Y, "Type", "Back").Equals("Stone"))
			{
				createLadderAt(vector);
				return true;
			}
			Vector2[] directionsTileVectors = Utility.DirectionsTileVectors;
			foreach (Vector2 vector2 in directionsTileVectors)
			{
				if (!list.Contains(vector + vector2))
				{
					queue.Enqueue(vector + vector2);
				}
			}
		}
		return false;
	}

	public override void monsterDrop(Monster monster, int x, int y, Farmer who)
	{
		if (monster.hasSpecialItem.Value)
		{
			Game1.createItemDebris(getSpecialItemForThisMineLevel(mineLevel, x / 64, y / 64), monster.Position, Game1.random.Next(4), monster.currentLocation);
		}
		else if (mineLevel > 121 && who != null && who.getFriendshipHeartLevelForNPC("Krobus") >= 10 && who.houseUpgradeLevel.Value >= 1 && !who.isMarriedOrRoommates() && !who.isEngaged() && Game1.random.NextDouble() < 0.001)
		{
			Game1.createItemDebris(ItemRegistry.Create("(O)808"), monster.Position, Game1.random.Next(4), monster.currentLocation);
		}
		else
		{
			base.monsterDrop(monster, x, y, who);
		}
		double num = ((who != null && who.hasBuff("dwarfStatue_1")) ? 0.07 : 0.0);
		if ((mustKillAllMonstersToAdvance() || !(Game1.random.NextDouble() < 0.15 + num)) && (!mustKillAllMonstersToAdvance() || EnemyCount > 1))
		{
			return;
		}
		Vector2 vector = new Vector2(x, y) / 64f;
		vector.X = (int)vector.X;
		vector.Y = (int)vector.Y;
		monster.IsInvisible = true;
		if (!IsTileOccupiedBy(vector) && isTileOnClearAndSolidGround(vector) && doesTileHaveProperty((int)vector.X, (int)vector.Y, "Type", "Back") != null && doesTileHaveProperty((int)vector.X, (int)vector.Y, "Type", "Back").Equals("Stone"))
		{
			createLadderAt(vector);
		}
		else if (mustKillAllMonstersToAdvance() && EnemyCount <= 1)
		{
			vector = new Vector2((int)tileBeneathLadder.X, (int)tileBeneathLadder.Y);
			createLadderAt(vector, "newArtifact");
			if (mustKillAllMonstersToAdvance() && who.IsLocalPlayer && who.currentLocation == this)
			{
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:MineShaft.cs.9484"));
			}
		}
	}

	public Item GetReplacementChestItem(int floor)
	{
		List<Item> list = null;
		if (Game1.netWorldState.Value.ShuffleMineChests == Game1.MineChestType.Remixed)
		{
			list = new List<Item>();
			switch (floor)
			{
			case 10:
				list.Add(ItemRegistry.Create("(B)506"));
				list.Add(ItemRegistry.Create("(B)507"));
				list.Add(ItemRegistry.Create("(W)12"));
				list.Add(ItemRegistry.Create("(W)17"));
				list.Add(ItemRegistry.Create("(W)22"));
				list.Add(ItemRegistry.Create("(W)31"));
				break;
			case 20:
				list.Add(ItemRegistry.Create("(W)11"));
				list.Add(ItemRegistry.Create("(W)24"));
				list.Add(ItemRegistry.Create("(W)20"));
				list.Add(new Ring("517"));
				list.Add(new Ring("519"));
				break;
			case 50:
				list.Add(ItemRegistry.Create("(B)509"));
				list.Add(ItemRegistry.Create("(B)510"));
				list.Add(ItemRegistry.Create("(B)508"));
				list.Add(ItemRegistry.Create("(W)1"));
				list.Add(ItemRegistry.Create("(W)43"));
				break;
			case 60:
				list.Add(ItemRegistry.Create("(W)21"));
				list.Add(ItemRegistry.Create("(W)44"));
				list.Add(ItemRegistry.Create("(W)6"));
				list.Add(ItemRegistry.Create("(W)18"));
				list.Add(ItemRegistry.Create("(W)27"));
				break;
			case 80:
				list.Add(ItemRegistry.Create("(B)512"));
				list.Add(ItemRegistry.Create("(B)511"));
				list.Add(ItemRegistry.Create("(W)10"));
				list.Add(ItemRegistry.Create("(W)7"));
				list.Add(ItemRegistry.Create("(W)46"));
				list.Add(ItemRegistry.Create("(W)19"));
				break;
			case 90:
				list.Add(ItemRegistry.Create("(W)8"));
				list.Add(ItemRegistry.Create("(W)52"));
				list.Add(ItemRegistry.Create("(W)45"));
				list.Add(ItemRegistry.Create("(W)5"));
				list.Add(ItemRegistry.Create("(W)60"));
				break;
			case 110:
				list.Add(ItemRegistry.Create("(B)514"));
				list.Add(ItemRegistry.Create("(B)878"));
				list.Add(ItemRegistry.Create("(W)50"));
				list.Add(ItemRegistry.Create("(W)28"));
				break;
			}
		}
		if (list != null && list.Count > 0)
		{
			return Utility.CreateRandom((double)Game1.uniqueIDForThisGame * 512.0, floor).ChooseFrom(list);
		}
		return null;
	}

	private void addLevelChests()
	{
		List<Item> list = new List<Item>();
		Vector2 vector = new Vector2(9f, 9f);
		Color tint = Color.White;
		if (mineLevel < 121 && mineLevel % 20 == 0 && mineLevel % 40 != 0)
		{
			vector.Y += 4f;
		}
		Item replacementChestItem = GetReplacementChestItem(mineLevel);
		bool flag = false;
		if (replacementChestItem != null)
		{
			list.Add(replacementChestItem);
		}
		else
		{
			switch (mineLevel)
			{
			case 5:
				Game1.player.completeQuest("14");
				if (!Game1.player.hasOrWillReceiveMail("guildQuest"))
				{
					Game1.addMailForTomorrow("guildQuest");
				}
				break;
			case 10:
				list.Add(ItemRegistry.Create("(B)506"));
				break;
			case 20:
				list.Add(ItemRegistry.Create("(W)11"));
				break;
			case 40:
				Game1.player.completeQuest("17");
				list.Add(ItemRegistry.Create("(W)32"));
				break;
			case 50:
				list.Add(ItemRegistry.Create("(B)509"));
				break;
			case 60:
				list.Add(ItemRegistry.Create("(W)21"));
				break;
			case 70:
				list.Add(ItemRegistry.Create("(W)33"));
				break;
			case 80:
				list.Add(ItemRegistry.Create("(B)512"));
				break;
			case 90:
				list.Add(ItemRegistry.Create("(W)8"));
				break;
			case 100:
				list.Add(new Object("434", 1));
				break;
			case 110:
				list.Add(ItemRegistry.Create("(B)514"));
				break;
			case 120:
				Game1.player.completeQuest("18");
				Game1.player.stats.checkForMineAchievement(isDirectUnlock: true, assumeDeepestLevel: true);
				if (!Game1.player.hasSkullKey)
				{
					Game1.player.chestConsumedMineLevels.Remove(120);
					list.Add(new SpecialItem(4));
					tint = Color.Pink;
				}
				break;
			case 220:
				if (Game1.player.secretNotesSeen.Contains(10) && !Game1.player.mailReceived.Contains("qiCave"))
				{
					Game1.eventUp = true;
					Game1.displayHUD = false;
					Game1.player.CanMove = false;
					Game1.player.showNotCarrying();
					currentEvent = new Event(Game1.content.LoadString((numberOfCraftedStairsUsedThisRun <= 10) ? "Data\\ExtraDialogue:SkullCavern_100_event_honorable" : "Data\\ExtraDialogue:SkullCavern_100_event"));
					currentEvent.exitLocation = new LocationRequest(base.Name, isStructure: false, this);
					Game1.player.chestConsumedMineLevels[mineLevel] = true;
				}
				else
				{
					flag = true;
				}
				break;
			case 320:
			case 420:
				flag = true;
				break;
			}
		}
		if (netIsTreasureRoom.Value || flag)
		{
			list.Add(getTreasureRoomItem());
		}
		if (mineLevel == 320)
		{
			vector.X += 1f;
		}
		if (list.Count > 0 && !Game1.player.chestConsumedMineLevels.ContainsKey(mineLevel))
		{
			overlayObjects[vector] = new Chest(list, vector)
			{
				Tint = tint
			};
			if (getMineArea() == 121 && flag)
			{
				(overlayObjects[vector] as Chest).SetBigCraftableSpriteIndex(344);
			}
		}
		if (mineLevel == 320 || mineLevel == 420)
		{
			overlayObjects[vector + new Vector2(-2f, 0f)] = new Chest(new List<Item> { getTreasureRoomItem() }, vector + new Vector2(-2f, 0f))
			{
				Tint = new Color(255, 210, 200)
			};
			(overlayObjects[vector + new Vector2(-2f, 0f)] as Chest).SetBigCraftableSpriteIndex(344);
		}
		if (mineLevel == 420)
		{
			overlayObjects[vector + new Vector2(2f, 0f)] = new Chest(new List<Item> { getTreasureRoomItem() }, vector + new Vector2(2f, 0f))
			{
				Tint = new Color(216, 255, 240)
			};
			(overlayObjects[vector + new Vector2(2f, 0f)] as Chest).SetBigCraftableSpriteIndex(344);
		}
	}

	private bool isForcedChestLevel(int level)
	{
		if (level != 220 && level != 320)
		{
			return level == 420;
		}
		return true;
	}

	public static Item getTreasureRoomItem()
	{
		if (Game1.player.stats.Get(StatKeys.Mastery(0)) != 0 && Game1.random.NextDouble() < 0.02)
		{
			return ItemRegistry.Create("(O)GoldenAnimalCracker");
		}
		if (Trinket.CanSpawnTrinket(Game1.player) && Game1.random.NextDouble() < 0.045)
		{
			return Trinket.GetRandomTrinket();
		}
		switch (Game1.random.Next(26))
		{
		case 0:
			return ItemRegistry.Create("(O)288", 5);
		case 1:
			return ItemRegistry.Create("(O)287", 10);
		case 2:
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("volcanoShortcutUnlocked") || !(Game1.random.NextDouble() < 0.66))
			{
				return ItemRegistry.Create("(O)275", 5);
			}
			return ItemRegistry.Create("(O)848", 5 + Game1.random.Next(1, 4) * 5);
		case 3:
			return ItemRegistry.Create("(O)773", Game1.random.Next(2, 5));
		case 4:
			return ItemRegistry.Create("(O)749", 5 + ((Game1.random.NextDouble() < 0.25) ? 5 : 0));
		case 5:
			return ItemRegistry.Create("(O)688", 5);
		case 6:
			return ItemRegistry.Create("(O)681", Game1.random.Next(1, 4));
		case 7:
			return ItemRegistry.Create("(O)" + Game1.random.Next(628, 634));
		case 8:
			return ItemRegistry.Create("(O)645", Game1.random.Next(1, 3));
		case 9:
			return ItemRegistry.Create("(O)621", 4);
		case 10:
			if (!(Game1.random.NextDouble() < 0.33))
			{
				return ItemRegistry.Create("(O)" + Game1.random.Next(472, 499), Game1.random.Next(1, 5) * 5);
			}
			return ItemRegistry.Create("(O)802", 15);
		case 11:
			return ItemRegistry.Create("(O)286", 15);
		case 12:
			if (!(Game1.random.NextDouble() < 0.5))
			{
				return ItemRegistry.Create("(O)437");
			}
			return ItemRegistry.Create("(O)265");
		case 13:
			return ItemRegistry.Create("(O)439");
		case 14:
			if (!(Game1.random.NextDouble() < 0.33))
			{
				return ItemRegistry.Create("(O)349", Game1.random.Next(2, 5));
			}
			return ItemRegistry.Create("(O)" + ((Game1.random.NextDouble() < 0.5) ? 226 : 732), 5);
		case 15:
			return ItemRegistry.Create("(O)337", Game1.random.Next(2, 4));
		case 16:
			if (!(Game1.random.NextDouble() < 0.33))
			{
				return ItemRegistry.Create("(O)" + Game1.random.Next(235, 245), 5);
			}
			return ItemRegistry.Create("(O)" + ((Game1.random.NextDouble() < 0.5) ? 226 : 732), 5);
		case 17:
			return ItemRegistry.Create("(O)74");
		case 18:
			return ItemRegistry.Create("(BC)21");
		case 19:
			return ItemRegistry.Create("(BC)25");
		case 20:
			return ItemRegistry.Create("(BC)165");
		case 21:
			return ItemRegistry.Create(Game1.random.NextBool() ? "(H)38" : "(H)37");
		case 22:
			if (Game1.player.mailReceived.Contains("sawQiPlane"))
			{
				return ItemRegistry.Create((Game1.player.stats.Get(StatKeys.Mastery(2)) != 0) ? "(O)GoldenMysteryBox" : "(O)MysteryBox", 5);
			}
			return ItemRegistry.Create("(O)749", 5 + ((Game1.random.NextDouble() < 0.25) ? 5 : 0));
		case 23:
			return ItemRegistry.Create("(H)65");
		case 24:
			return ItemRegistry.Create("(BC)272");
		case 25:
			return ItemRegistry.Create("(H)83");
		default:
			return ItemRegistry.Create("(O)288", 5);
		}
	}

	public static Item getSpecialItemForThisMineLevel(int level, int x, int y)
	{
		Random random = Utility.CreateRandom(level, Game1.stats.DaysPlayed, x, (double)y * 9999.0);
		if (Game1.mine == null)
		{
			return ItemRegistry.Create("(O)388");
		}
		if (Game1.mine.GetAdditionalDifficulty() > 0)
		{
			if (random.NextDouble() < 0.02)
			{
				return ItemRegistry.Create("(BC)272");
			}
			switch (random.Next(7))
			{
			case 0:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)61"), random);
			case 1:
				return ItemRegistry.Create("(O)910");
			case 2:
				return ItemRegistry.Create("(O)913");
			case 3:
				return ItemRegistry.Create("(O)915");
			case 4:
				return new Ring("527");
			case 5:
				return ItemRegistry.Create("(O)858");
			case 6:
			{
				Item treasureRoomItem = getTreasureRoomItem();
				treasureRoomItem.Stack = 1;
				return treasureRoomItem;
			}
			}
		}
		if (level < 20)
		{
			switch (random.Next(6))
			{
			case 0:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)16"), random);
			case 1:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)24"), random);
			case 2:
				return ItemRegistry.Create("(B)504");
			case 3:
				return ItemRegistry.Create("(B)505");
			case 4:
				return new Ring("516");
			case 5:
				return new Ring("518");
			}
		}
		else if (level < 40)
		{
			switch (random.Next(7))
			{
			case 0:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)22"), random);
			case 1:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)24"), random);
			case 2:
				return ItemRegistry.Create("(B)504");
			case 3:
				return ItemRegistry.Create("(B)505");
			case 4:
				return new Ring("516");
			case 5:
				return new Ring("518");
			case 6:
				return ItemRegistry.Create("(W)15");
			}
		}
		else if (level < 60)
		{
			switch (random.Next(7))
			{
			case 0:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)6"), random);
			case 1:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)26"), random);
			case 2:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)15"), random);
			case 3:
				return ItemRegistry.Create("(B)510");
			case 4:
				return new Ring("517");
			case 5:
				return new Ring("519");
			case 6:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)27"), random);
			}
		}
		else if (level < 80)
		{
			switch (random.Next(7))
			{
			case 0:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)26"), random);
			case 1:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)27"), random);
			case 2:
				return ItemRegistry.Create("(B)508");
			case 3:
				return ItemRegistry.Create("(B)510");
			case 4:
				return new Ring("517");
			case 5:
				return new Ring("519");
			case 6:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)19"), random);
			}
		}
		else if (level < 100)
		{
			switch (random.Next(8))
			{
			case 0:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)48"), random);
			case 1:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)48"), random);
			case 2:
				return ItemRegistry.Create("(B)511");
			case 3:
				return ItemRegistry.Create("(B)513");
			case 4:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)18"), random);
			case 5:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)28"), random);
			case 6:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)52"), random);
			case 7:
			{
				MeleeWeapon obj = (MeleeWeapon)MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)3"), random);
				obj.AddEnchantment(new CrusaderEnchantment());
				return obj;
			}
			}
		}
		else if (level < 120)
		{
			switch (random.Next(8))
			{
			case 0:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)19"), random);
			case 1:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)50"), random);
			case 2:
				return ItemRegistry.Create("(B)511");
			case 3:
				return ItemRegistry.Create("(B)513");
			case 4:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)18"), random);
			case 5:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)46"), random);
			case 6:
				return new Ring("887");
			case 7:
			{
				MeleeWeapon obj2 = (MeleeWeapon)MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)3"), random);
				obj2.AddEnchantment(new CrusaderEnchantment());
				return obj2;
			}
			}
		}
		else
		{
			switch (random.Next(12))
			{
			case 0:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)45"), random);
			case 1:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)50"), random);
			case 2:
				return ItemRegistry.Create("(B)511");
			case 3:
				return ItemRegistry.Create("(B)513");
			case 4:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)18"), random);
			case 5:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)28"), random);
			case 6:
				return MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)52"), random);
			case 7:
				return ItemRegistry.Create("(O)787");
			case 8:
				return ItemRegistry.Create("(B)878");
			case 9:
				return ItemRegistry.Create("(O)856");
			case 10:
				return new Ring("859");
			case 11:
				return new Ring("887");
			}
		}
		return new Object("78", 1);
	}

	public override bool IsLocationSpecificOccupantOnTile(Vector2 tileLocation)
	{
		if (tileBeneathLadder.Equals(tileLocation))
		{
			return true;
		}
		if (tileBeneathElevator != Vector2.Zero && tileBeneathElevator.Equals(tileLocation))
		{
			return true;
		}
		return base.IsLocationSpecificOccupantOnTile(tileLocation);
	}

	public bool isDarkArea()
	{
		if (loadedDarkArea || mineLevel % 40 > 30)
		{
			return getMineArea() != 40;
		}
		return false;
	}

	public bool isTileClearForMineObjects(Vector2 v)
	{
		if (tileBeneathLadder.Equals(v) || tileBeneathElevator.Equals(v))
		{
			return false;
		}
		if (!CanItemBePlacedHere(v, itemIsPassable: false, CollisionMask.All, CollisionMask.None))
		{
			return false;
		}
		if (IsTileOccupiedBy(v, CollisionMask.Characters))
		{
			return false;
		}
		if (IsTileOccupiedBy(v, CollisionMask.Flooring | CollisionMask.TerrainFeatures))
		{
			return false;
		}
		string text = doesTileHaveProperty((int)v.X, (int)v.Y, "Type", "Back");
		if (text == null || !text.Equals("Stone"))
		{
			return false;
		}
		if (!isTileOnClearAndSolidGround(v))
		{
			return false;
		}
		if (objects.ContainsKey(v))
		{
			return false;
		}
		if (Utility.PointToVector2(calicoStatueSpot.Value).Equals(v))
		{
			return false;
		}
		return true;
	}

	public override string getFootstepSoundReplacement(string footstep)
	{
		if (GetAdditionalDifficulty() > 0 && getMineArea() == 40 && mineLevel % 40 < 30 && footstep == "stoneStep")
		{
			return "grassyStep";
		}
		return base.getFootstepSoundReplacement(footstep);
	}

	public bool isTileOnClearAndSolidGround(Vector2 v)
	{
		if (hasTileAt((int)v.X, (int)v.Y, "Back") && !hasTileAt((int)v.X, (int)v.Y, "Front") && !hasTileAt((int)v.X, (int)v.Y, "Buildings"))
		{
			return getTileIndexAt((int)v.X, (int)v.Y, "Back", "mine") != 77;
		}
		return false;
	}

	public bool isTileOnClearAndSolidGround(int x, int y)
	{
		if (hasTileAt(x, y, "Back") && !hasTileAt(x, y, "Front"))
		{
			return getTileIndexAt(x, y, "Back", "mine") != 77;
		}
		return false;
	}

	public bool isTileClearForMineObjects(int x, int y)
	{
		return isTileClearForMineObjects(new Vector2(x, y));
	}

	public void loadLevel(int level)
	{
		forceFirstTime = false;
		hasAddedDesertFestivalStatue = false;
		isMonsterArea = false;
		isSlimeArea = false;
		loadedDarkArea = false;
		isQuarryArea = false;
		isDinoArea = false;
		mineLoader.Unload();
		mineLoader.Dispose();
		mineLoader = Game1.content.CreateTemporary();
		if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && Game1.IsMasterGame && level > deepestLevelOnCurrentDesertFestivalRun && getMineArea() == 121)
		{
			if (level % 5 == 0)
			{
				Game1.player.team.calicoEggSkullCavernRating.Value++;
			}
			deepestLevelOnCurrentDesertFestivalRun = level;
		}
		bool flag = false;
		int num = -1;
		if (forceLayout.HasValue)
		{
			num = forceLayout.Value;
			string text = "Maps\\Mines\\" + num;
			if (!mapContent.DoesAssetExist<Map>(text))
			{
				Game1.log.Warn($"Can't force mine layout to {num} because there's no '{text}' asset, falling back to default logic.");
				num = -1;
			}
		}
		if (num < 0)
		{
			num = ((level % 40 % 20 == 0 && level % 40 != 0) ? 20 : ((level % 10 == 0) ? 10 : level));
			num %= 40;
			if (level == 120)
			{
				num = 120;
			}
			if (getMineArea(level) == 121)
			{
				MineShaft mineShaft = null;
				foreach (MineShaft activeMine in activeMines)
				{
					if (activeMine != null && activeMine.mineLevel > 120 && activeMine.mineLevel < level && (mineShaft == null || activeMine.mineLevel > mineShaft.mineLevel))
					{
						mineShaft = activeMine;
					}
				}
				for (num = mineRandom.Next(40); num == mineShaft?.loadedMapNumber; num = mineRandom.Next(40))
				{
				}
				while (num % 5 == 0)
				{
					num = mineRandom.Next(40);
				}
				if (isForcedChestLevel(level))
				{
					num = 10;
				}
				else if (level >= 130)
				{
					double num2 = 0.01;
					num2 += Game1.player.team.AverageDailyLuck(this) / 10.0 + Game1.player.team.AverageLuckLevel(this) / 100.0;
					if (Game1.random.NextDouble() < num2)
					{
						netIsTreasureRoom.Value = true;
						num = 10;
					}
				}
			}
			else if (getMineArea() == 77377 && mineLevel == 77377)
			{
				num = 77377;
			}
			if (lowestLevelReached >= 120 && num != 10 && num % 5 != 0 && mineLevel > 1 && mineLevel != 77377)
			{
				Random random = Utility.CreateDaySaveRandom(1293857 + mineLevel * 400);
				double num3 = 0.06;
				if (mineLevel > 120)
				{
					num3 += Math.Min(0.06, (double)mineLevel / 10000.0);
				}
				if (random.NextDouble() < num3)
				{
					int[] source = new int[4] { 40, 47, 50, 51 };
					num = random.Next(40, 61);
					if (source.Contains(num) && random.NextDouble() < 0.75)
					{
						num = random.Next(40, 61);
					}
					if (num == 53 && getMineArea() != 121)
					{
						num = random.Next(52, 61);
					}
					if (num == 40 && getMineArea() != 0 && getMineArea() != 80)
					{
						num = random.Next(52, 61);
					}
					if (source.Contains(num))
					{
						flag = true;
					}
				}
			}
		}
		mapPath.Value = "Maps\\Mines\\" + num;
		loadedMapNumber = num;
		updateMap();
		Random random2 = Utility.CreateDaySaveRandom(level * 100);
		if ((!AnyOnlineFarmerHasBuff("23") || getMineArea() == 121) && random2.NextDouble() < 0.044 && num % 5 != 0 && num % 40 > 5 && num % 40 < 30 && num % 40 != 19 && !flag)
		{
			if (random2.NextBool())
			{
				isMonsterArea = true;
			}
			else
			{
				isSlimeArea = true;
			}
			if (getMineArea() == 121 && mineLevel > 126 && random2.NextBool())
			{
				isDinoArea = true;
				isSlimeArea = false;
				isMonsterArea = false;
			}
		}
		else if (mineLevel < 121 && random2.NextDouble() < 0.044 && Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccCraftsRoom") && Game1.MasterPlayer.hasOrWillReceiveMail("VisitedQuarryMine") && num % 40 > 1 && num % 5 != 0)
		{
			isQuarryArea = true;
			if (random2.NextDouble() < 0.25 && !flag)
			{
				isMonsterArea = true;
			}
		}
		if (isQuarryArea || getMineArea(level) == 77377)
		{
			mapImageSource.Value = "Maps\\Mines\\mine_quarryshaft";
			int num4 = map.Layers[0].LayerWidth * map.Layers[0].LayerHeight / 100;
			isQuarryArea = true;
			isSlimeArea = false;
			isMonsterArea = false;
			isDinoArea = false;
			for (int i = 0; i < num4; i++)
			{
				brownSpots.Add(new Vector2(mineRandom.Next(0, map.Layers[0].LayerWidth), mineRandom.Next(0, map.Layers[0].LayerHeight)));
			}
		}
		else if (isDinoArea)
		{
			mapImageSource.Value = "Maps\\Mines\\mine_dino";
		}
		else if (isSlimeArea)
		{
			mapImageSource.Value = "Maps\\Mines\\mine_slime";
		}
		else if (getMineArea() == 0 || getMineArea() == 10 || (getMineArea(level) != 0 && getMineArea(level) != 10))
		{
			if (getMineArea(level) == 40)
			{
				mapImageSource.Value = "Maps\\Mines\\mine_frost";
				if (level >= 70)
				{
					mapImageSource.Value += "_dark";
					loadedDarkArea = true;
				}
			}
			else if (getMineArea(level) == 80)
			{
				mapImageSource.Value = "Maps\\Mines\\mine_lava";
				if (level >= 110 && level != 120)
				{
					mapImageSource.Value += "_dark";
					loadedDarkArea = true;
				}
			}
			else if (getMineArea(level) == 121)
			{
				mapImageSource.Value = "Maps\\Mines\\mine_desert";
				if (num % 40 >= 30)
				{
					mapImageSource.Value += "_dark";
					loadedDarkArea = true;
				}
			}
		}
		if (num == 45)
		{
			loadedDarkArea = true;
			if (mapImageSource.Value == null)
			{
				mapImageSource.Value = "Maps\\Mines\\mine_dark";
			}
			else if (!mapImageSource.Value.EndsWith("dark"))
			{
				mapImageSource.Value += "_dark";
			}
		}
		if (GetAdditionalDifficulty() > 0)
		{
			string text2 = "Maps\\Mines\\mine";
			if (mapImageSource.Value != null)
			{
				text2 = mapImageSource.Value;
			}
			if (text2.EndsWith("_dark"))
			{
				text2 = text2.Remove(text2.Length - "_dark".Length);
			}
			string text3 = text2;
			if (level % 40 >= 30)
			{
				loadedDarkArea = true;
			}
			if (loadedDarkArea)
			{
				text2 += "_dark";
			}
			text2 += "_dangerous";
			try
			{
				mapImageSource.Value = text2;
				Game1.temporaryContent.Load<Texture2D>(mapImageSource.Value);
			}
			catch (ContentLoadException)
			{
				text2 = text3 + "_dangerous";
				try
				{
					mapImageSource.Value = text2;
					Game1.temporaryContent.Load<Texture2D>(mapImageSource.Value);
				}
				catch (ContentLoadException)
				{
					text2 = text3;
					if (loadedDarkArea)
					{
						text2 += "_dark";
					}
					try
					{
						mapImageSource.Value = text2;
						Game1.temporaryContent.Load<Texture2D>(mapImageSource.Value);
						goto end_IL_08d2;
					}
					catch (ContentLoadException)
					{
						mapImageSource.Value = text3;
						goto end_IL_08d2;
					}
					end_IL_08d2:;
				}
			}
		}
		ApplyDiggableTileFixes();
		if (!isSideBranch())
		{
			lowestLevelReached = Math.Max(lowestLevelReached, level);
			if (mineLevel % 5 == 0 && getMineArea() != 121)
			{
				prepareElevator();
			}
		}
	}

	private void addBlueFlamesToChallengeShrine()
	{
		temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2(8.75f, 5.8f) * 64f + new Vector2(32f, -32f), flipped: false, 0f, Color.White)
		{
			interval = 50f,
			totalNumberOfLoops = 99999,
			animationLength = 4,
			lightId = $"Mines_{mineLevel}_ChallengeShrineFlames_1",
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
			lightId = $"Mines_{mineLevel}_ChallengeShrineFlames_2",
			id = 889,
			lightRadius = 2f,
			scale = 4f,
			lightcolor = new Color(100, 0, 0),
			yPeriodic = true,
			yPeriodicLoopTime = 1100f,
			yPeriodicRange = 4f,
			layerDepth = 0.04544f
		});
		Game1.playSound("fireball");
	}

	public static void CheckForQiChallengeCompletion()
	{
		if (Game1.player.deepestMineLevel >= 145 && Game1.player.hasQuest("20") && !Game1.player.hasOrWillReceiveMail("QiChallengeComplete"))
		{
			Game1.player.completeQuest("20");
			Game1.addMailForTomorrow("QiChallengeComplete");
		}
	}

	private void prepareElevator()
	{
		Point point = (ElevatorLightSpot = Utility.findTile(this, 80, "Buildings", "mine"));
		if (point.X >= 0)
		{
			if (canAdd(3, 0))
			{
				elevatorShouldDing.Value = true;
				updateMineLevelData(3);
			}
			else
			{
				setMapTile(point.X, point.Y, 48, "Buildings", "mine");
			}
		}
	}

	public void enterMineShaft()
	{
		DelayedAction.playSoundAfterDelay("fallDown", 800, this);
		DelayedAction.playSoundAfterDelay("clubSmash", 1800);
		Random random = Utility.CreateRandom(mineLevel, Game1.uniqueIDForThisGame, Game1.Date.TotalDays);
		int num = random.Next(3, 9);
		if (random.NextDouble() < 0.1)
		{
			num = num * 2 - 1;
		}
		if (mineLevel < 220 && mineLevel + num > 220)
		{
			num = 220 - mineLevel;
		}
		lastLevelsDownFallen = num;
		Game1.player.health = Math.Max(1, Game1.player.health - num * 3);
		isFallingDownShaft = true;
		Game1.globalFadeToBlack(afterFall, 0.045f);
		Game1.player.CanMove = false;
		Game1.player.jump();
		Game1.player.temporarilyInvincible = true;
		Game1.player.temporaryInvincibilityTimer = 0;
		Game1.player.flashDuringThisTemporaryInvincibility = false;
		Game1.player.currentTemporaryInvincibilityDuration = 700;
		if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && Game1.IsMasterGame && lastLevelsDownFallen + mineLevel > deepestLevelOnCurrentDesertFestivalRun && isFallingDownShaft && (lastLevelsDownFallen + mineLevel) / 5 > mineLevel / 5)
		{
			Game1.player.team.calicoEggSkullCavernRating.Value += (lastLevelsDownFallen + mineLevel) / 5 - mineLevel / 5;
		}
	}

	private void afterFall()
	{
		Game1.drawObjectDialogue(Game1.content.LoadString((lastLevelsDownFallen > 7) ? "Strings\\Locations:Mines_FallenFar" : "Strings\\Locations:Mines_Fallen", lastLevelsDownFallen));
		Game1.messagePause = true;
		Game1.enterMine(mineLevel + lastLevelsDownFallen);
		Game1.fadeToBlackAlpha = 1f;
		Game1.player.faceDirection(2);
		Game1.player.showFrame(5);
	}

	/// <inheritdoc />
	public override bool ShouldExcludeFromNpcPathfinding()
	{
		return true;
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		if (who.IsLocalPlayer)
		{
			switch (getTileIndexAt(tileLocation, "Buildings", "mine"))
			{
			case 284:
				if (mineLevel > 120 && mineLevel != 77377)
				{
					recentlyActivatedCalicoStatue.Value = new Point(tileLocation.X, tileLocation.Y);
					return true;
				}
				break;
			case 112:
				if (mineLevel <= 120)
				{
					Game1.activeClickableMenu = new MineElevatorMenu();
					return true;
				}
				break;
			case 115:
			{
				Response[] answerChoices = new Response[2]
				{
					new Response("Leave", Game1.content.LoadString("Strings\\Locations:Mines_LeaveMine")).SetHotKey(Keys.Y),
					new Response("Do", Game1.content.LoadString("Strings\\Locations:Mines_DoNothing")).SetHotKey(Keys.Escape)
				};
				createQuestionDialogue(" ", answerChoices, "ExitMine");
				return true;
			}
			case 173:
				Game1.enterMine(mineLevel + 1);
				playSound("stairsdown");
				return true;
			case 174:
			{
				Response[] answerChoices2 = new Response[2]
				{
					new Response("Jump", Game1.content.LoadString("Strings\\Locations:Mines_ShaftJumpIn")).SetHotKey(Keys.Y),
					new Response("Do", Game1.content.LoadString("Strings\\Locations:Mines_DoNothing")).SetHotKey(Keys.Escape)
				};
				createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Mines_Shaft"), answerChoices2, "Shaft");
				return true;
			}
			case 194:
				playSound("openBox");
				playSound("Ship");
				map.RequireLayer("Buildings").Tiles[tileLocation].TileIndex++;
				map.RequireLayer("Front").Tiles[tileLocation.X, tileLocation.Y - 1].TileIndex++;
				Game1.createRadialDebris(this, 382, tileLocation.X, tileLocation.Y, 6, resource: false, -1, item: true);
				updateMineLevelData(2, -1);
				return true;
			case 315:
			case 316:
			case 317:
				if (Game1.player.team.SpecialOrderRuleActive("MINE_HARD") || Game1.player.team.specialRulesRemovedToday.Contains("MINE_HARD"))
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ChallengeShrine_OnQiChallenge"));
				}
				else if (Game1.player.team.toggleMineShrineOvernight.Value)
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ChallengeShrine_AlreadyActive"));
				}
				else
				{
					createQuestionDialogue(Game1.player.team.mineShrineActivated.Value ? Game1.content.LoadString("Strings\\Locations:ChallengeShrine_AlreadyHard") : Game1.content.LoadString("Strings\\Locations:ChallengeShrine_NotYetHard"), createYesNoResponses(), "ShrineOfChallenge");
				}
				break;
			}
		}
		return base.checkAction(tileLocation, viewport, who);
	}

	public override string checkForBuriedItem(int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
	{
		if (isQuarryArea)
		{
			return "";
		}
		if (Game1.random.NextDouble() < 0.15)
		{
			string id = "(O)330";
			if (Game1.random.NextDouble() < 0.07)
			{
				if (Game1.random.NextDouble() < 0.75)
				{
					switch (Game1.random.Next(5))
					{
					case 0:
						id = "(O)96";
						break;
					case 1:
						id = ((!who.hasOrWillReceiveMail("lostBookFound")) ? "(O)770" : ((Game1.netWorldState.Value.LostBooksFound < 21) ? "(O)102" : "(O)770"));
						break;
					case 2:
						id = "(O)110";
						break;
					case 3:
						id = "(O)112";
						break;
					case 4:
						id = "(O)585";
						break;
					}
				}
				else if (Game1.random.NextDouble() < 0.75)
				{
					switch (getMineArea())
					{
					case 0:
					case 10:
						id = Game1.random.Choose("(O)121", "(O)97");
						break;
					case 40:
						id = Game1.random.Choose("(O)122", "(O)336");
						break;
					case 80:
						id = "(O)99";
						break;
					}
				}
				else
				{
					id = Game1.random.Choose("(O)126", "(O)127");
				}
			}
			else if (Game1.random.NextDouble() < 0.19)
			{
				id = (Game1.random.NextBool() ? "(O)390" : getOreIdForLevel(mineLevel, Game1.random));
			}
			else if (Game1.random.NextDouble() < 0.45)
			{
				id = "(O)330";
			}
			else if (Game1.random.NextDouble() < 0.12)
			{
				if (Game1.random.NextDouble() < 0.25)
				{
					id = "(O)749";
				}
				else
				{
					switch (getMineArea())
					{
					case 0:
					case 10:
						id = "(O)535";
						break;
					case 40:
						id = "(O)536";
						break;
					case 80:
						id = "(O)537";
						break;
					}
				}
			}
			else
			{
				id = "(O)78";
			}
			Game1.createObjectDebris(id, xLocation, yLocation, who.UniqueMultiplayerID, this);
			bool num = who?.CurrentTool is Hoe && who.CurrentTool.hasEnchantmentOfType<GenerousEnchantment>();
			float num2 = 0.25f;
			if (num && Game1.random.NextDouble() < (double)num2)
			{
				Game1.createObjectDebris(id, xLocation, yLocation, who.UniqueMultiplayerID, this);
			}
			return "";
		}
		return "";
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		base.drawAboveAlwaysFrontLayer(b);
		b.End();
		b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
		foreach (NPC character in characters)
		{
			if (character is Monster monster)
			{
				monster.drawAboveAllLayers(b);
			}
		}
		b.End();
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		if (fogAlpha > 0f || ambientFog)
		{
			Vector2 position = default(Vector2);
			for (float num = -256 + (int)(fogPos.X % 256f); num < (float)Game1.graphics.GraphicsDevice.Viewport.Width; num += 256f)
			{
				for (float num2 = -256 + (int)(fogPos.Y % 256f); num2 < (float)Game1.graphics.GraphicsDevice.Viewport.Height; num2 += 256f)
				{
					position.X = (int)num;
					position.Y = (int)num2;
					b.Draw(Game1.mouseCursors, position, fogSource, (fogAlpha > 0f) ? (fogColor * fogAlpha) : fogColor, 0f, Vector2.Zero, 4.001f, SpriteEffects.None, 1f);
				}
			}
		}
		if (Game1.game1.takingMapScreenshot || isSideBranch())
		{
			return;
		}
		Color value = ((getMineArea() == 0 || (isDarkArea() && getMineArea() != 121)) ? SpriteText.color_White : ((getMineArea() == 10) ? SpriteText.color_Green : ((getMineArea() == 40) ? SpriteText.color_Cyan : ((getMineArea() == 80) ? SpriteText.color_Red : SpriteText.color_Purple))));
		string s = (mineLevel + ((getMineArea() == 121) ? (-120) : 0)).ToString() ?? "";
		Microsoft.Xna.Framework.Rectangle titleSafeArea = Game1.game1.GraphicsDevice.Viewport.GetTitleSafeArea();
		int heightOfString = SpriteText.getHeightOfString(s);
		SpriteText.drawString(b, s, titleSafeArea.Left + 16, titleSafeArea.Top + 16, 999999, -1, heightOfString, 1f, 1f, junimoText: false, 2, "", value);
		int widthOfString = SpriteText.getWidthOfString(s);
		if (mustKillAllMonstersToAdvance())
		{
			b.Draw(Game1.mouseCursors, new Vector2(titleSafeArea.Left + 16 + widthOfString + 16, titleSafeArea.Top + 16) + new Vector2(4f, 6f) * 4f, new Microsoft.Xna.Framework.Rectangle(192, 324, 7, 10), Color.White, 0f, new Vector2(3f, 5f), 4f + Game1.dialogueButtonScale / 25f, SpriteEffects.None, 1f);
		}
		if (Utility.GetDayOfPassiveFestival("DesertFestival") <= 0)
		{
			return;
		}
		int num3 = 0;
		foreach (IClickableMenu onScreenMenu in Game1.onScreenMenus)
		{
			if (onScreenMenu is BuffsDisplay buffsDisplay)
			{
				num3 = buffsDisplay.getNumBuffs();
			}
		}
		Vector2 vector = new Vector2((float)Game1.graphics.GraphicsDevice.Viewport.Width - 300f * ((float)Game1.graphics.GraphicsDevice.Viewport.Width / (float)Game1.uiViewport.Width) - 100f, titleSafeArea.Top + 64 + 16 + (num3 - 1) / 5 * 16 * 4) + new Vector2(4f, 6f) * 4f;
		if (calicoEggIconTimerShake > 0f)
		{
			vector += new Vector2(Game1.random.Next(-4, 5), Game1.random.Next(-4, 5));
			b.DrawString(Game1.dialogueFont, "+1", vector + new Vector2(vector.X - 32f, vector.Y + 32f), Color.White);
		}
		b.Draw(Game1.mouseCursors_1_6, vector, new Microsoft.Xna.Framework.Rectangle(0, 0, 19, 21), Color.White, 0f, new Vector2(3f, 5f), 4f, SpriteEffects.None, 1f);
		SpriteText.drawString(b, (Game1.player.team.calicoEggSkullCavernRating.Value + 1).ToString() ?? "", (int)vector.X + 28 - SpriteText.getWidthOfString((Game1.player.team.calicoEggSkullCavernRating.Value + 1).ToString() ?? "") / 2, (int)vector.Y + 4);
	}

	/// <inheritdoc />
	public override void checkForMusic(GameTime time)
	{
		if (Game1.player.freezePause <= 0 && !isFogUp.Value && mineLevel != 120)
		{
			string text = null;
			switch (getMineArea())
			{
			case 0:
			case 10:
			case 121:
			case 77377:
				text = "Upper_Ambient";
				break;
			case 40:
				text = "Frost_Ambient";
				break;
			case 80:
				text = "Lava_Ambient";
				break;
			}
			if (GetAdditionalDifficulty() > 0 && getMineArea() == 40 && mineLevel < 70)
			{
				text = "jungle_ambience";
			}
			if (Game1.getMusicTrackName() == "none" || Game1.isMusicContextActiveButNotPlaying() || (Game1.getMusicTrackName().EndsWith("_Ambient") && Game1.getMusicTrackName() != text))
			{
				Game1.changeMusicTrack(text);
			}
			timeSinceLastMusic = Math.Min(335000, timeSinceLastMusic + time.ElapsedGameTime.Milliseconds);
		}
	}

	public string getMineSong()
	{
		if (mineLevel < 40)
		{
			return "EarthMine";
		}
		if (mineLevel < 80)
		{
			return "FrostMine";
		}
		if (getMineArea() == 121)
		{
			if (Game1.random.NextDouble() < 0.75)
			{
				return "LavaMine";
			}
			return "EarthMine";
		}
		return "LavaMine";
	}

	public int GetAdditionalDifficulty()
	{
		if (mineLevel == 77377)
		{
			return 0;
		}
		if (mineLevel > 120)
		{
			return Game1.netWorldState.Value.SkullCavesDifficulty;
		}
		return Game1.netWorldState.Value.MinesDifficulty;
	}

	public bool isPlayingSongFromDifferentArea()
	{
		if (Game1.getMusicTrackName() != getMineSong())
		{
			return Game1.getMusicTrackName().EndsWith("Mine");
		}
		return false;
	}

	public void playMineSong()
	{
		string mineSong = getMineSong();
		if ((Game1.getMusicTrackName() == "none" || Game1.isMusicContextActiveButNotPlaying() || Game1.getMusicTrackName().Contains("Ambient")) && !isDarkArea() && mineLevel != 77377)
		{
			Game1.changeMusicTrack(mineSong);
			timeSinceLastMusic = 0;
		}
	}

	protected override void resetLocalState()
	{
		addLevelChests();
		base.resetLocalState();
		if (Game1.IsPlayingBackgroundMusic)
		{
			Game1.changeMusicTrack("none");
		}
		if (elevatorShouldDing.Value)
		{
			timeUntilElevatorLightUp = 1500;
		}
		else if (mineLevel % 5 == 0 && getMineArea() != 121)
		{
			setElevatorLit();
		}
		if (!isSideBranch(mineLevel))
		{
			Game1.player.deepestMineLevel = Math.Max(Game1.player.deepestMineLevel, mineLevel);
			if (Game1.player.team.specialOrders != null)
			{
				foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
				{
					specialOrder.onMineFloorReached?.Invoke(Game1.player, mineLevel);
				}
			}
			Game1.player.autoGenerateActiveDialogueEvent("mineArea_" + getMineArea());
		}
		if (mineLevel == 77377)
		{
			Game1.addMailForTomorrow("VisitedQuarryMine", noLetter: true, sendToEveryone: true);
		}
		if (getMineArea() == 121 && Game1.player.team.calicoStatueEffects.ContainsKey(10) && !Game1.player.hasBuff("CalicoStatueSpeed"))
		{
			DesertFestival.addCalicoStatueSpeedBuff();
		}
		CheckForQiChallengeCompletion();
		if (mineLevel == 120)
		{
			Farmer player = Game1.player;
			int timesReachedMineBottom = player.timesReachedMineBottom + 1;
			player.timesReachedMineBottom = timesReachedMineBottom;
		}
		Vector2 vector = mineEntrancePosition(Game1.player);
		Game1.xLocationAfterWarp = (int)vector.X;
		Game1.yLocationAfterWarp = (int)vector.Y;
		if (Game1.IsClient)
		{
			Game1.player.Position = new Vector2(Game1.xLocationAfterWarp * 64, Game1.yLocationAfterWarp * 64 - (Game1.player.Sprite.getHeight() - 32) + 16);
		}
		forceViewportPlayerFollow = true;
		switch (mineLevel)
		{
		case 20:
			if (!Game1.IsMultiplayer && IsRainingHere() && Game1.player.eventsSeen.Contains("901756"))
			{
				characters.Clear();
				NPC nPC = new NPC(new AnimatedSprite("Characters\\Abigail", 0, 16, 32), new Vector2(896f, 644f), "SeedShop", 3, "AbigailMine", datable: true, Game1.content.Load<Texture2D>("Portraits\\Abigail"))
				{
					displayName = NPC.GetDisplayName("Abigail")
				};
				Random random = Utility.CreateRandom(Game1.stats.DaysPlayed);
				if (Game1.player.mailReceived.Add("AbigailInMineFirst"))
				{
					nPC.setNewDialogue("Strings\\Characters:AbigailInMineFirst");
					nPC.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
					{
						new FarmerSprite.AnimationFrame(0, 300),
						new FarmerSprite.AnimationFrame(1, 300),
						new FarmerSprite.AnimationFrame(2, 300),
						new FarmerSprite.AnimationFrame(3, 300)
					});
				}
				else if (random.NextDouble() < 0.15)
				{
					nPC.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
					{
						new FarmerSprite.AnimationFrame(16, 500),
						new FarmerSprite.AnimationFrame(17, 500),
						new FarmerSprite.AnimationFrame(18, 500),
						new FarmerSprite.AnimationFrame(19, 500)
					});
					nPC.setNewDialogue("Strings\\Characters:AbigailInMineFlute");
					Game1.changeMusicTrack("AbigailFlute");
				}
				else
				{
					nPC.setNewDialogue("Strings\\Characters:AbigailInMine" + random.Next(5));
					nPC.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
					{
						new FarmerSprite.AnimationFrame(0, 300),
						new FarmerSprite.AnimationFrame(1, 300),
						new FarmerSprite.AnimationFrame(2, 300),
						new FarmerSprite.AnimationFrame(3, 300)
					});
				}
				characters.Add(nPC);
			}
			break;
		case 120:
			if (GetAdditionalDifficulty() > 0 && !Game1.player.hasOrWillReceiveMail("reachedBottomOfHardMines"))
			{
				Game1.addMailForTomorrow("reachedBottomOfHardMines", noLetter: true, sendToEveryone: true);
			}
			if (GetAdditionalDifficulty() > 0)
			{
				Game1.getAchievement(41);
			}
			if (Game1.player.hasOrWillReceiveMail("reachedBottomOfHardMines"))
			{
				setMapTile(9, 6, 315, "Buildings", "mine", "None");
				setMapTile(10, 6, 316, "Buildings", "mine", "None");
				setMapTile(11, 6, 317, "Buildings", "mine", "None");
				setMapTile(9, 5, 299, "Front", "mine");
				setMapTile(10, 5, 300, "Front", "mine");
				setMapTile(11, 5, 301, "Front", "mine");
				if ((Game1.player.team.mineShrineActivated.Value && !Game1.player.team.toggleMineShrineOvernight.Value) || (!Game1.player.team.mineShrineActivated.Value && Game1.player.team.toggleMineShrineOvernight.Value))
				{
					DelayedAction.functionAfterDelay(addBlueFlamesToChallengeShrine, 1000);
				}
			}
			break;
		}
		ApplyDiggableTileFixes();
		if (isMonsterArea || isSlimeArea)
		{
			Random random2 = Utility.CreateRandom(Game1.stats.DaysPlayed);
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\Locations:Mines_" + random2.Choose("Infested", "Overrun")));
		}
		bool num = mineLevel % 20 == 0;
		bool flag = false;
		if (num)
		{
			waterTiles = new WaterTiles(map.Layers[0].LayerWidth, map.Layers[0].LayerHeight);
			waterColor.Value = ((getMineArea() == 80) ? (Color.Red * 0.8f) : (new Color(50, 100, 200) * 0.5f));
			for (int i = 0; i < map.RequireLayer("Buildings").LayerHeight; i++)
			{
				for (int j = 0; j < map.RequireLayer("Buildings").LayerWidth; j++)
				{
					string text = doesTileHaveProperty(j, i, "Water", "Back");
					if (text != null)
					{
						flag = true;
						if (text == "I")
						{
							waterTiles.waterTiles[j, i] = new WaterTiles.WaterTileData(is_water: true, is_visible: false);
						}
						else
						{
							waterTiles[j, i] = true;
						}
						if (getMineArea() == 80 && Game1.random.NextDouble() < 0.1)
						{
							sharedLights.AddLight(new LightSource($"Mines_{mineLevel}_{j}_{i}_Lava", 4, new Vector2(j, i) * 64f, 2f, new Color(0, 220, 220), LightSource.LightContext.None, 0L, base.NameOrUniqueName));
						}
					}
				}
			}
		}
		if (!flag)
		{
			waterTiles = null;
		}
		if (getMineArea(mineLevel) != getMineArea(mineLevel - 1) || mineLevel == 120 || isPlayingSongFromDifferentArea())
		{
			Game1.changeMusicTrack("none");
		}
		if (GetAdditionalDifficulty() > 0 && mineLevel == 70)
		{
			Game1.changeMusicTrack("none");
		}
		if (mineLevel == 77377 && Game1.player.mailReceived.Contains("gotGoldenScythe"))
		{
			setMapTile(29, 4, 245, "Front", "mine");
			setMapTile(30, 4, 246, "Front", "mine");
			setMapTile(29, 5, 261, "Front", "mine");
			setMapTile(30, 5, 262, "Front", "mine");
			setMapTile(29, 6, 277, "Buildings", "mine");
			setMapTile(30, 56, 278, "Buildings", "mine");
		}
		if (calicoStatueSpot.Value != Point.Zero)
		{
			if (recentlyActivatedCalicoStatue.Value != Point.Zero)
			{
				setMapTile(calicoStatueSpot.X, calicoStatueSpot.Y, 285, "Buildings", "mine");
				setMapTile(calicoStatueSpot.X, calicoStatueSpot.Y - 1, 269, "Front", "mine");
				setMapTile(calicoStatueSpot.X, calicoStatueSpot.Y - 2, 253, "Front", "mine");
			}
			else
			{
				setMapTile(calicoStatueSpot.X, calicoStatueSpot.Y, 284, "Buildings", "mine");
				setMapTile(calicoStatueSpot.X, calicoStatueSpot.Y - 1, 268, "Front", "mine");
				setMapTile(calicoStatueSpot.X, calicoStatueSpot.Y - 2, 252, "Front", "mine");
			}
		}
		if (mineLevel > 1 && (mineLevel == 2 || (mineLevel % 5 != 0 && timeSinceLastMusic > 150000 && Game1.random.NextBool())))
		{
			playMineSong();
		}
	}

	public virtual void ApplyDiggableTileFixes()
	{
		if (map != null && (GetAdditionalDifficulty() <= 0 || getMineArea() == 40 || !isDarkArea()))
		{
			TileSheet tileSheet = map.RequireTileSheet(0, "mine");
			tileSheet.TileIndexProperties[165].TryAdd("Diggable", "true");
			tileSheet.TileIndexProperties[181].TryAdd("Diggable", "true");
			tileSheet.TileIndexProperties[183].TryAdd("Diggable", "true");
		}
	}

	public void createLadderDown(int x, int y, bool forceShaft = false)
	{
		createLadderDownEvent[new Point(x, y)] = forceShaft || (getMineArea() == 121 && !mustKillAllMonstersToAdvance() && mineRandom.NextDouble() < 0.2);
	}

	private void doCreateLadderDown(Point point, bool shaft)
	{
		updateMap();
		int x = point.X;
		int y = point.Y;
		Layer layer = map.RequireLayer("Buildings");
		TileSheet tileSheet = map.RequireTileSheet(0, "mine");
		if (shaft)
		{
			layer.Tiles[x, y] = new StaticTile(layer, tileSheet, BlendMode.Alpha, 174);
		}
		else
		{
			ladderHasSpawned = true;
			layer.Tiles[x, y] = new StaticTile(layer, tileSheet, BlendMode.Alpha, 173);
		}
		if (Game1.player.currentLocation == this)
		{
			Game1.player.TemporaryPassableTiles.Add(new Microsoft.Xna.Framework.Rectangle(x * 64, y * 64, 64, 64));
		}
	}

	public void checkStoneForItems(string stoneId, int x, int y, Farmer who)
	{
		long whichPlayer = who?.UniqueMultiplayerID ?? 0;
		int num = who?.LuckLevel ?? 0;
		double num2 = who?.DailyLuck ?? 0.0;
		int num3 = who?.MiningLevel ?? 0;
		double num4 = num2 / 2.0 + (double)num3 * 0.005 + (double)num * 0.001;
		Random random = Utility.CreateDaySaveRandom(x * 1000, y, mineLevel);
		random.NextDouble();
		double num5 = ((stoneId == 40.ToString() || stoneId == 42.ToString()) ? 1.2 : 0.8);
		stonesLeftOnThisLevel--;
		double num6 = 0.02 + 1.0 / (double)Math.Max(1, stonesLeftOnThisLevel) + (double)num / 100.0 + Game1.player.DailyLuck / 5.0;
		if (EnemyCount == 0)
		{
			num6 += 0.04;
		}
		if (who != null && who.hasBuff("dwarfStatue_1"))
		{
			num6 *= 1.25;
		}
		if (!ladderHasSpawned && !mustKillAllMonstersToAdvance() && (stonesLeftOnThisLevel == 0 || random.NextDouble() < num6) && shouldCreateLadderOnThisLevel())
		{
			createLadderDown(x, y);
		}
		if (breakStone(stoneId, x, y, who, random))
		{
			return;
		}
		if (stoneId == 44.ToString())
		{
			int num7 = random.Next(59, 70);
			num7 += num7 % 2;
			bool flag = false;
			foreach (Farmer allFarmer in Game1.getAllFarmers())
			{
				if (allFarmer.timesReachedMineBottom > 0)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				if (mineLevel < 40 && num7 != 66 && num7 != 68)
				{
					num7 = random.Choose(66, 68);
				}
				else if (mineLevel < 80 && (num7 == 64 || num7 == 60))
				{
					num7 = random.Choose(66, 70, 68, 62);
				}
			}
			Game1.createObjectDebris("(O)" + num7, x, y, whichPlayer, this);
			Game1.stats.OtherPreciousGemsFound++;
			return;
		}
		int num8 = ((who == null || !who.professions.Contains(22)) ? 1 : 2);
		double num9 = ((who != null && who.hasBuff("dwarfStatue_4")) ? 1.25 : 1.0);
		if (random.NextDouble() < 0.022 * (1.0 + num4) * (double)num8 * num9)
		{
			string id = "(O)" + (535 + ((getMineArea() == 40) ? 1 : ((getMineArea() == 80) ? 2 : 0)));
			if (getMineArea() == 121)
			{
				id = "(O)749";
			}
			if (who != null && who.professions.Contains(19) && random.NextBool())
			{
				Game1.createObjectDebris(id, x, y, whichPlayer, this);
			}
			Game1.createObjectDebris(id, x, y, whichPlayer, this);
			who?.gainExperience(5, 20 * getMineArea());
		}
		if (mineLevel > 20 && random.NextDouble() < 0.005 * (1.0 + num4) * (double)num8 * num9)
		{
			if (who != null && who.professions.Contains(19) && random.NextBool())
			{
				Game1.createObjectDebris("(O)749", x, y, whichPlayer, this);
			}
			Game1.createObjectDebris("(O)749", x, y, whichPlayer, this);
			who?.gainExperience(5, 40 * getMineArea());
		}
		if (random.NextDouble() < 0.05 * (1.0 + num4) * num5)
		{
			int num10 = ((who == null || !who.professions.Contains(21)) ? 1 : 2);
			double num11 = ((who != null && who.hasBuff("dwarfStatue_2")) ? 0.1 : 0.0);
			if (random.NextDouble() < 0.25 * (double)num10 + num11)
			{
				Game1.createObjectDebris("(O)382", x, y, whichPlayer, this);
				Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(25, new Vector2(64 * x, 64 * y), Color.White, 8, Game1.random.NextBool(), 80f, 0, -1, -1f, 128));
			}
			Game1.createObjectDebris(getOreIdForLevel(mineLevel, random), x, y, whichPlayer, this);
			who?.gainExperience(3, 5);
		}
		else if (random.NextBool())
		{
			Game1.createDebris(14, x, y, 1, this);
		}
	}

	public string getOreIdForLevel(int mineLevel, Random r)
	{
		if (getMineArea(mineLevel) == 77377)
		{
			return "(O)380";
		}
		if (mineLevel < 40)
		{
			if (mineLevel >= 20 && r.NextDouble() < 0.1)
			{
				return "(O)380";
			}
			return "(O)378";
		}
		if (mineLevel < 80)
		{
			if (mineLevel >= 60 && r.NextDouble() < 0.1)
			{
				return "(O)384";
			}
			if (!(r.NextDouble() < 0.75))
			{
				return "(O)378";
			}
			return "(O)380";
		}
		if (mineLevel < 120)
		{
			if (!(r.NextDouble() < 0.75))
			{
				if (!(r.NextDouble() < 0.75))
				{
					return "(O)378";
				}
				return "(O)380";
			}
			return "(O)384";
		}
		if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && r.NextDouble() < 0.13 + (double)((float)(Game1.player.team.calicoEggSkullCavernRating.Value * 5) / 1000f))
		{
			return "CalicoEgg";
		}
		if (r.NextDouble() < 0.01 + (double)((float)(mineLevel - 120) / 2000f))
		{
			return "(O)386";
		}
		if (!(r.NextDouble() < 0.75))
		{
			if (!(r.NextDouble() < 0.75))
			{
				return "(O)378";
			}
			return "(O)380";
		}
		return "(O)384";
	}

	public bool shouldUseSnowTextureHoeDirt()
	{
		if (isSlimeArea)
		{
			return false;
		}
		if (GetAdditionalDifficulty() > 0 && (mineLevel < 40 || (mineLevel >= 70 && mineLevel < 80)))
		{
			return true;
		}
		if (GetAdditionalDifficulty() <= 0 && getMineArea() == 40)
		{
			return true;
		}
		return false;
	}

	public int getMineArea(int level = -1)
	{
		if (level == -1)
		{
			level = mineLevel;
		}
		if (!isQuarryArea)
		{
			switch (level)
			{
			case 77377:
				break;
			case 80:
			case 81:
			case 82:
			case 83:
			case 84:
			case 85:
			case 86:
			case 87:
			case 88:
			case 89:
			case 90:
			case 91:
			case 92:
			case 93:
			case 94:
			case 95:
			case 96:
			case 97:
			case 98:
			case 99:
			case 100:
			case 101:
			case 102:
			case 103:
			case 104:
			case 105:
			case 106:
			case 107:
			case 108:
			case 109:
			case 110:
			case 111:
			case 112:
			case 113:
			case 114:
			case 115:
			case 116:
			case 117:
			case 118:
			case 119:
			case 120:
				return 80;
			default:
				if (level > 120)
				{
					return 121;
				}
				if (level >= 40)
				{
					return 40;
				}
				if (level > 10 && mineLevel < 30)
				{
					return 10;
				}
				return 0;
			}
		}
		return 77377;
	}

	public bool isSideBranch(int level = -1)
	{
		if (level == -1)
		{
			level = mineLevel;
		}
		return level == 77377;
	}

	public byte getWallAt(int x, int y)
	{
		return byte.MaxValue;
	}

	public Color getLightingColor(GameTime time)
	{
		return lighting;
	}

	public Object getRandomItemForThisLevel(int level, Vector2 tile)
	{
		string itemId = "80";
		if (mineRandom.NextDouble() < 0.05 && level > 80)
		{
			itemId = "422";
		}
		else if (mineRandom.NextDouble() < 0.1 && level > 20 && getMineArea() != 40)
		{
			itemId = "420";
		}
		else if (mineRandom.NextDouble() < 0.25 || GetAdditionalDifficulty() > 0)
		{
			switch (getMineArea())
			{
			case 0:
			case 10:
				if (GetAdditionalDifficulty() > 0 && !isDarkArea())
				{
					switch (mineRandom.Next(6))
					{
					case 0:
					case 6:
						itemId = "152";
						break;
					case 1:
						itemId = "393";
						break;
					case 2:
						itemId = "397";
						break;
					case 3:
						itemId = "372";
						break;
					case 4:
						itemId = "392";
						break;
					}
					if (mineRandom.NextDouble() < 0.005)
					{
						itemId = "797";
					}
					else if (mineRandom.NextDouble() < 0.08)
					{
						itemId = "394";
					}
				}
				else
				{
					itemId = "86";
				}
				break;
			case 40:
				if (GetAdditionalDifficulty() > 0 && mineLevel % 40 < 30)
				{
					switch (mineRandom.Next(4))
					{
					case 0:
					case 3:
						itemId = "259";
						break;
					case 1:
						itemId = "404";
						break;
					case 2:
						itemId = "420";
						break;
					}
					if (mineRandom.NextDouble() < 0.08)
					{
						itemId = "422";
					}
				}
				else
				{
					itemId = "84";
				}
				break;
			case 80:
				itemId = "82";
				break;
			case 121:
				itemId = ((mineRandom.NextDouble() < 0.3) ? "86" : ((mineRandom.NextDouble() < 0.3) ? "84" : "82"));
				break;
			}
		}
		else
		{
			itemId = "80";
		}
		if (isDinoArea)
		{
			itemId = "259";
			if (mineRandom.NextDouble() < 0.06)
			{
				itemId = "107";
			}
		}
		return new Object(itemId, 1)
		{
			IsSpawnedObject = true
		};
	}

	public bool shouldShowDarkHoeDirt()
	{
		if (getMineArea() == 121 && !isDinoArea)
		{
			return false;
		}
		return true;
	}

	public string getRandomGemRichStoneForThisLevel(int level)
	{
		int num = mineRandom.Next(59, 70);
		num += num % 2;
		if (Game1.player.timesReachedMineBottom == 0)
		{
			if (level < 40 && num != 66 && num != 68)
			{
				num = mineRandom.Choose(66, 68);
			}
			else if (level < 80 && (num == 64 || num == 60))
			{
				num = mineRandom.Choose(66, 70, 68, 62);
			}
		}
		return num switch
		{
			66 => "8", 
			68 => "10", 
			60 => "12", 
			70 => "6", 
			64 => "4", 
			62 => "14", 
			_ => 40.ToString(), 
		};
	}

	public float getDistanceFromStart(int xTile, int yTile)
	{
		float num = Utility.distance(xTile, tileBeneathLadder.X, yTile, tileBeneathLadder.Y);
		if (tileBeneathElevator != Vector2.Zero)
		{
			num = Math.Min(num, Utility.distance(xTile, tileBeneathElevator.X, yTile, tileBeneathElevator.Y));
		}
		return num;
	}

	public Monster getMonsterForThisLevel(int level, int xTile, int yTile)
	{
		Vector2 vector = new Vector2(xTile, yTile) * 64f;
		float distanceFromStart = getDistanceFromStart(xTile, yTile);
		if (isSlimeArea)
		{
			if (GetAdditionalDifficulty() <= 0)
			{
				if (mineRandom.NextDouble() < 0.2)
				{
					return new BigSlime(vector, getMineArea());
				}
				return new GreenSlime(vector, mineLevel);
			}
			if (mineLevel < 20)
			{
				return new GreenSlime(vector, mineLevel);
			}
			if (mineLevel < 30)
			{
				return new BlueSquid(vector);
			}
			if (mineLevel < 40)
			{
				return new RockGolem(vector, this);
			}
			if (mineLevel < 50)
			{
				if (mineRandom.NextDouble() < 0.15 && distanceFromStart >= 10f)
				{
					return new Fly(vector);
				}
				return new Grub(vector);
			}
			if (mineLevel < 70)
			{
				return new Leaper(vector);
			}
		}
		else if (isDinoArea)
		{
			if (mineRandom.NextDouble() < 0.1)
			{
				return new Bat(vector, 999);
			}
			if (mineRandom.NextDouble() < 0.1)
			{
				return new Fly(vector, hard: true);
			}
			return new DinoMonster(vector);
		}
		if (getMineArea() == 0 || getMineArea() == 10)
		{
			if (mineRandom.NextDouble() < 0.25 && !mustKillAllMonstersToAdvance())
			{
				return new Bug(vector, mineRandom.Next(4), this);
			}
			if (level < 15)
			{
				if (doesTileHaveProperty(xTile, yTile, "Diggable", "Back") != null)
				{
					return new Duggy(vector);
				}
				if (mineRandom.NextDouble() < 0.15)
				{
					return new RockCrab(vector);
				}
				return new GreenSlime(vector, level);
			}
			if (level <= 30)
			{
				if (doesTileHaveProperty(xTile, yTile, "Diggable", "Back") != null)
				{
					return new Duggy(vector);
				}
				if (mineRandom.NextDouble() < 0.15)
				{
					return new RockCrab(vector);
				}
				if (mineRandom.NextDouble() < 0.05 && distanceFromStart > 10f && GetAdditionalDifficulty() <= 0)
				{
					return new Fly(vector);
				}
				if (mineRandom.NextDouble() < 0.45)
				{
					return new GreenSlime(vector, level);
				}
				if (GetAdditionalDifficulty() <= 0)
				{
					return new Grub(vector);
				}
				if (distanceFromStart > 9f)
				{
					return new BlueSquid(vector);
				}
				if (mineRandom.NextDouble() < 0.01)
				{
					return new RockGolem(vector, this);
				}
				return new GreenSlime(vector, level);
			}
			if (level <= 40)
			{
				if (mineRandom.NextDouble() < 0.1 && distanceFromStart > 10f)
				{
					return new Bat(vector, level);
				}
				if (GetAdditionalDifficulty() > 0 && mineRandom.NextDouble() < 0.1)
				{
					return new Ghost(vector, "Carbon Ghost");
				}
				return new RockGolem(vector, this);
			}
		}
		else if (getMineArea() == 40)
		{
			if (mineLevel >= 70 && (mineRandom.NextDouble() < 0.75 || GetAdditionalDifficulty() > 0))
			{
				if (mineRandom.NextDouble() < 0.75 || GetAdditionalDifficulty() <= 0)
				{
					return new Skeleton(vector, GetAdditionalDifficulty() > 0 && mineRandom.NextBool());
				}
				return new Bat(vector, 77377);
			}
			if (mineRandom.NextDouble() < 0.3)
			{
				return new DustSpirit(vector, mineRandom.NextDouble() < 0.8);
			}
			if (mineRandom.NextDouble() < 0.3 && distanceFromStart > 10f)
			{
				return new Bat(vector, mineLevel);
			}
			if (!ghostAdded && mineLevel > 50 && mineRandom.NextDouble() < 0.3 && distanceFromStart > 10f)
			{
				ghostAdded = true;
				if (GetAdditionalDifficulty() > 0)
				{
					return new Ghost(vector, "Putrid Ghost");
				}
				return new Ghost(vector);
			}
			if (GetAdditionalDifficulty() > 0)
			{
				if (mineRandom.NextDouble() < 0.01)
				{
					RockCrab rockCrab = new RockCrab(vector);
					rockCrab.makeStickBug();
					return rockCrab;
				}
				if (mineLevel >= 50)
				{
					return new Leaper(vector);
				}
				if (mineRandom.NextDouble() < 0.7)
				{
					return new Grub(vector);
				}
				return new GreenSlime(vector, mineLevel);
			}
		}
		else if (getMineArea() == 80)
		{
			if (isDarkArea() && mineRandom.NextDouble() < 0.25)
			{
				return new Bat(vector, mineLevel);
			}
			if (mineRandom.NextDouble() < ((GetAdditionalDifficulty() > 0) ? 0.05 : 0.15))
			{
				return new GreenSlime(vector, getMineArea());
			}
			if (mineRandom.NextDouble() < 0.15)
			{
				return new MetalHead(vector, getMineArea());
			}
			if (mineRandom.NextDouble() < 0.25)
			{
				return new ShadowBrute(vector);
			}
			if (GetAdditionalDifficulty() > 0 && mineRandom.NextDouble() < 0.25)
			{
				return new Shooter(vector, "Shadow Sniper");
			}
			if (mineRandom.NextDouble() < 0.25)
			{
				return new ShadowShaman(vector);
			}
			if (mineRandom.NextDouble() < 0.25)
			{
				return new RockCrab(vector, "Lava Crab");
			}
			if (mineRandom.NextDouble() < 0.2 && distanceFromStart > 8f && mineLevel >= 90 && hasTileAt(xTile, yTile, "Back") && !hasTileAt(xTile, yTile, "Front"))
			{
				return new SquidKid(vector);
			}
		}
		else
		{
			if (getMineArea() == 121)
			{
				if (loadedDarkArea)
				{
					if (mineRandom.NextDouble() < 0.18 && distanceFromStart > 8f)
					{
						return new Ghost(vector, "Carbon Ghost");
					}
					Mummy mummy = new Mummy(vector);
					if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && getMineArea() == 121 && Game1.player.team.calicoStatueEffects.ContainsKey(9))
					{
						mummy.BuffForAdditionalDifficulty(2);
						mummy.speed *= 2;
						setMonsterTextureToDangerousVersion(mummy);
					}
					return mummy;
				}
				if (mineLevel % 20 == 0 && distanceFromStart > 10f)
				{
					return new Bat(vector, mineLevel);
				}
				if (mineLevel % 16 == 0 && !mustKillAllMonstersToAdvance())
				{
					if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && getMineArea() == 121 && Game1.player.team.calicoStatueEffects.ContainsKey(4))
					{
						return new Bug(vector, mineRandom.Next(4), "Assassin Bug");
					}
					return new Bug(vector, mineRandom.Next(4), this);
				}
				if (mineRandom.NextDouble() < 0.33 && distanceFromStart > 10f)
				{
					if (GetAdditionalDifficulty() <= 0)
					{
						return new Serpent(vector);
					}
					return new Serpent(vector, "Royal Serpent");
				}
				if (mineRandom.NextDouble() < 0.33 && distanceFromStart > 10f && mineLevel >= 171)
				{
					return new Bat(vector, mineLevel);
				}
				if (mineLevel >= 126 && distanceFromStart > 10f && mineRandom.NextDouble() < 0.04 && !mustKillAllMonstersToAdvance())
				{
					return new DinoMonster(vector);
				}
				if (mineRandom.NextDouble() < 0.33 && !mustKillAllMonstersToAdvance())
				{
					if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && getMineArea() == 121 && Game1.player.team.calicoStatueEffects.ContainsKey(4))
					{
						return new Bug(vector, mineRandom.Next(4), "Assassin Bug");
					}
					return new Bug(vector, mineRandom.Next(4), this);
				}
				if (mineRandom.NextDouble() < 0.25)
				{
					return new GreenSlime(vector, level);
				}
				if (mineLevel >= 146 && mineRandom.NextDouble() < 0.25)
				{
					return new RockCrab(vector, "Iridium Crab");
				}
				if (GetAdditionalDifficulty() > 0 && mineRandom.NextDouble() < 0.2 && distanceFromStart > 8f && hasTileAt(xTile, yTile, "Back") && !hasTileAt(xTile, yTile, "Front"))
				{
					return new SquidKid(vector);
				}
				return new BigSlime(vector, this);
			}
			if (getMineArea() == 77377)
			{
				if ((mineLevel == 77377 && yTile > 59) || (mineLevel != 77377 && mineLevel % 2 == 0))
				{
					GreenSlime greenSlime = new GreenSlime(vector, 77377);
					Vector2 value = new Vector2(xTile, yTile);
					bool flag = false;
					for (int i = 0; i < brownSpots.Count; i++)
					{
						if (Vector2.Distance(value, brownSpots[i]) < 4f)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						int num = Game1.random.Next(120, 200);
						greenSlime.color.Value = new Color(num, num / 2, num / 4);
						while (Game1.random.NextDouble() < 0.33)
						{
							greenSlime.objectsToDrop.Add("378");
						}
						greenSlime.Health = (int)((float)greenSlime.Health * 0.5f);
						greenSlime.Speed += 2;
					}
					else
					{
						int num2 = Game1.random.Next(120, 200);
						greenSlime.color.Value = new Color(num2, num2, num2);
						while (Game1.random.NextDouble() < 0.33)
						{
							greenSlime.objectsToDrop.Add("380");
						}
						greenSlime.Speed = 1;
					}
					return greenSlime;
				}
				if (yTile < 51 || mineLevel != 77377)
				{
					if (xTile >= 70)
					{
						Monster monster = new Skeleton(vector, Game1.random.NextBool());
						monster.BuffForAdditionalDifficulty(mineRandom.Next(1, 3));
						setMonsterTextureToDangerousVersion(monster);
						return monster;
					}
					return new Bat(vector, 77377);
				}
				return new Bat(vector, 77377)
				{
					focusedOnFarmers = true
				};
			}
		}
		return new GreenSlime(vector, level);
	}

	private Object createLitterObject(double chanceForPurpleStone, double chanceForMysticStone, double gemStoneChance, Vector2 tile)
	{
		Color color = Color.White;
		int minutesUntilReady = 1;
		if (GetAdditionalDifficulty() > 0 && mineLevel % 5 != 0 && mineRandom.NextDouble() < (double)GetAdditionalDifficulty() * 0.001 + (double)((float)mineLevel / 100000f) + Game1.player.team.AverageDailyLuck(this) / 13.0 + Game1.player.team.AverageLuckLevel(this) * 0.0001500000071246177)
		{
			return new Object("95", 1)
			{
				MinutesUntilReady = 25
			};
		}
		int num;
		if (getMineArea() == 0 || getMineArea() == 10)
		{
			num = mineRandom.Next(31, 42);
			if (mineLevel % 40 < 30 && num >= 33 && num < 38)
			{
				num = mineRandom.Choose(32, 38);
			}
			else if (mineLevel % 40 >= 30)
			{
				num = mineRandom.Choose(34, 36);
			}
			if (GetAdditionalDifficulty() > 0)
			{
				num = mineRandom.Next(33, 37);
				minutesUntilReady = 5;
				if (Game1.random.NextDouble() < 0.33)
				{
					num = 846;
				}
				else
				{
					color = new Color(Game1.random.Next(60, 90), Game1.random.Next(150, 200), Game1.random.Next(190, 240));
				}
				if (isDarkArea())
				{
					num = mineRandom.Next(32, 39);
					int num2 = Game1.random.Next(130, 160);
					color = new Color(num2, num2, num2);
				}
				if (mineLevel != 1 && mineLevel % 5 != 0 && mineRandom.NextDouble() < 0.029)
				{
					return new Object("849", 1)
					{
						MinutesUntilReady = 6
					};
				}
				if (color.Equals(Color.White))
				{
					return new Object(num.ToString(), 1)
					{
						MinutesUntilReady = minutesUntilReady
					};
				}
			}
			else if (mineLevel != 1 && mineLevel % 5 != 0 && mineRandom.NextDouble() < 0.029)
			{
				return new Object("751", 1)
				{
					MinutesUntilReady = 3
				};
			}
		}
		else if (getMineArea() == 40)
		{
			num = mineRandom.Next(47, 54);
			minutesUntilReady = 3;
			if (GetAdditionalDifficulty() > 0 && mineLevel % 40 < 30)
			{
				num = mineRandom.Next(39, 42);
				minutesUntilReady = 5;
				color = new Color(170, 255, 160);
				if (isDarkArea())
				{
					num = mineRandom.Next(32, 39);
					int num3 = Game1.random.Next(130, 160);
					color = new Color(num3, num3, num3);
				}
				if (mineRandom.NextDouble() < 0.15)
				{
					return new ColoredObject((294 + mineRandom.Choose(1, 0)).ToString(), 1, new Color(170, 140, 155))
					{
						MinutesUntilReady = 6,
						CanBeSetDown = true,
						Flipped = mineRandom.NextBool()
					};
				}
				if (mineLevel != 1 && mineLevel % 5 != 0 && mineRandom.NextDouble() < 0.029)
				{
					return new ColoredObject("290", 1, new Color(150, 225, 160))
					{
						MinutesUntilReady = 6,
						CanBeSetDown = true,
						Flipped = mineRandom.NextBool()
					};
				}
				if (color.Equals(Color.White))
				{
					return new Object(num.ToString(), 1)
					{
						MinutesUntilReady = minutesUntilReady
					};
				}
			}
			else if (mineLevel % 5 != 0 && mineRandom.NextDouble() < 0.029)
			{
				return new Object("290", 1)
				{
					MinutesUntilReady = 4
				};
			}
		}
		else if (getMineArea() == 80)
		{
			minutesUntilReady = 4;
			num = ((mineRandom.NextDouble() < 0.3 && !isDarkArea()) ? ((!mineRandom.NextBool()) ? 32 : 38) : ((mineRandom.NextDouble() < 0.3) ? mineRandom.Next(55, 58) : ((!mineRandom.NextBool()) ? 762 : 760)));
			if (GetAdditionalDifficulty() > 0)
			{
				num = ((!mineRandom.NextBool()) ? 32 : 38);
				minutesUntilReady = 5;
				color = new Color(Game1.random.Next(140, 190), Game1.random.Next(90, 120), Game1.random.Next(210, 255));
				if (isDarkArea())
				{
					num = mineRandom.Next(32, 39);
					int num4 = Game1.random.Next(130, 160);
					color = new Color(num4, num4, num4);
				}
				if (mineLevel != 1 && mineLevel % 5 != 0 && mineRandom.NextDouble() < 0.029)
				{
					return new Object("764", 1)
					{
						MinutesUntilReady = 7
					};
				}
				if (color.Equals(Color.White))
				{
					return new Object(num.ToString(), 1)
					{
						MinutesUntilReady = minutesUntilReady
					};
				}
			}
			else if (mineLevel % 5 != 0 && mineRandom.NextDouble() < 0.029)
			{
				return new Object("764", 1)
				{
					MinutesUntilReady = 8
				};
			}
		}
		else
		{
			if (getMineArea() == 77377)
			{
				minutesUntilReady = 5;
				bool flag = false;
				foreach (Vector2 adjacentTileLocation in Utility.getAdjacentTileLocations(tile))
				{
					if (objects.ContainsKey(adjacentTileLocation))
					{
						flag = true;
						break;
					}
				}
				if (!flag && mineRandom.NextDouble() < 0.45)
				{
					return null;
				}
				bool flag2 = false;
				for (int i = 0; i < brownSpots.Count; i++)
				{
					if (Vector2.Distance(tile, brownSpots[i]) < 4f)
					{
						flag2 = true;
						break;
					}
					if (Vector2.Distance(tile, brownSpots[i]) < 6f)
					{
						return null;
					}
				}
				if (tile.X > 50f)
				{
					num = Game1.random.Choose(668, 670);
					if (mineRandom.NextDouble() < 0.09 + Game1.player.team.AverageDailyLuck(this) / 2.0)
					{
						return new Object(Game1.random.Choose("BasicCoalNode0", "BasicCoalNode1"), 1)
						{
							MinutesUntilReady = 5
						};
					}
					if (mineRandom.NextDouble() < 0.25)
					{
						return null;
					}
				}
				else if (flag2)
				{
					num = mineRandom.Choose(32, 38);
					if (mineRandom.NextDouble() < 0.01)
					{
						return new Object("751", 1)
						{
							MinutesUntilReady = 3
						};
					}
				}
				else
				{
					num = mineRandom.Choose(34, 36);
					if (mineRandom.NextDouble() < 0.01)
					{
						return new Object("290", 1)
						{
							MinutesUntilReady = 3
						};
					}
				}
				return new Object(num.ToString(), 1)
				{
					MinutesUntilReady = minutesUntilReady
				};
			}
			minutesUntilReady = 5;
			num = (mineRandom.NextBool() ? ((!mineRandom.NextBool()) ? 32 : 38) : ((!mineRandom.NextBool()) ? 42 : 40));
			int num5 = mineLevel - 120;
			double num6 = 0.02 + (double)num5 * 0.0005;
			if (mineLevel >= 130)
			{
				num6 += 0.01 * (double)((float)(Math.Min(100, num5) - 10) / 10f);
			}
			double num7 = 0.0;
			if (mineLevel >= 130)
			{
				num7 += 0.001 * (double)((float)(num5 - 10) / 10f);
			}
			num7 = Math.Min(num7, 0.004);
			if (num5 > 100)
			{
				num7 += (double)num5 / 1000000.0;
			}
			if (!netIsTreasureRoom.Value && mineRandom.NextDouble() < num6)
			{
				double num8 = (double)Math.Min(100, num5) * (0.0003 + num7);
				double num9 = 0.01 + (double)(mineLevel - Math.Min(150, num5)) * 0.0005;
				double num10 = Math.Min(0.5, 0.1 + (double)(mineLevel - Math.Min(200, num5)) * 0.005);
				if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && mineRandom.NextBool(0.13 + (double)((float)(Game1.player.team.calicoEggSkullCavernRating.Value * 5) / 1000f)))
				{
					return new Object("CalicoEggStone_" + mineRandom.Next(3), 1)
					{
						MinutesUntilReady = 8
					};
				}
				if (mineRandom.NextDouble() < num8)
				{
					return new Object("765", 1)
					{
						MinutesUntilReady = 16
					};
				}
				if (mineRandom.NextDouble() < num9)
				{
					return new Object("764", 1)
					{
						MinutesUntilReady = 8
					};
				}
				if (mineRandom.NextDouble() < num10)
				{
					return new Object("290", 1)
					{
						MinutesUntilReady = 4
					};
				}
				return new Object("751", 1)
				{
					MinutesUntilReady = 2
				};
			}
		}
		double num11 = Game1.player.team.AverageDailyLuck(this);
		double num12 = Game1.player.team.AverageSkillLevel(3, Game1.currentLocation);
		double num13 = num11 + num12 * 0.005;
		if (mineLevel > 50 && mineRandom.NextDouble() < 0.00025 + (double)mineLevel / 120000.0 + 0.0005 * num13 / 2.0)
		{
			num = 2;
			minutesUntilReady = 10;
		}
		else if (gemStoneChance != 0.0 && mineRandom.NextDouble() < gemStoneChance + gemStoneChance * num13 + (double)mineLevel / 24000.0)
		{
			return new Object(getRandomGemRichStoneForThisLevel(mineLevel), 1)
			{
				MinutesUntilReady = 5
			};
		}
		if (mineRandom.NextDouble() < chanceForPurpleStone / 2.0 + chanceForPurpleStone * num12 * 0.008 + chanceForPurpleStone * (num11 / 2.0))
		{
			num = 44;
		}
		if (mineLevel > 100 && mineRandom.NextDouble() < chanceForMysticStone + chanceForMysticStone * num12 * 0.008 + chanceForMysticStone * (num11 / 2.0))
		{
			num = 46;
		}
		num += num % 2;
		if (mineRandom.NextDouble() < 0.1 && getMineArea() != 40)
		{
			if (!color.Equals(Color.White))
			{
				return new ColoredObject(mineRandom.Choose("668", "670"), 1, color)
				{
					MinutesUntilReady = 2,
					Flipped = mineRandom.NextBool()
				};
			}
			return new Object(mineRandom.Choose("668", "670"), 1)
			{
				MinutesUntilReady = 2,
				Flipped = mineRandom.NextBool()
			};
		}
		if (!color.Equals(Color.White))
		{
			return new ColoredObject(num.ToString(), 1, color)
			{
				MinutesUntilReady = minutesUntilReady,
				Flipped = mineRandom.NextBool()
			};
		}
		return new Object(num.ToString(), 1)
		{
			MinutesUntilReady = minutesUntilReady
		};
	}

	public static void OnLeftMines()
	{
		if (!Game1.IsClient && !Game1.IsMultiplayer)
		{
			clearInactiveMines(keepUntickedLevels: false);
		}
		Game1.player.buffs.Remove("CalicoStatueSpeed");
	}

	public static void clearActiveMines()
	{
		activeMines.RemoveAll(delegate(MineShaft mine)
		{
			mine.OnRemoved();
			return true;
		});
	}

	private static void clearInactiveMines(bool keepUntickedLevels = true)
	{
		int maxMineLevel = -1;
		int maxSkullLevel = -1;
		string[] disconnectLevels = (from fh in Game1.getAllFarmhands()
			select (fh.disconnectDay.Value != Game1.MasterPlayer.stats.DaysPlayed) ? null : fh.disconnectLocation.Value).ToArray();
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.locationBeforeForcedEvent.Value == null || !IsGeneratedLevel(allFarmer.locationBeforeForcedEvent.Value, out var level))
			{
				continue;
			}
			if (level > 120)
			{
				if (level < 77377)
				{
					maxSkullLevel = Math.Max(maxSkullLevel, level);
				}
			}
			else
			{
				maxMineLevel = Math.Max(maxMineLevel, level);
			}
		}
		foreach (MineShaft activeMine in activeMines)
		{
			if (!activeMine.farmers.Any() && !disconnectLevels.Contains(activeMine.NameOrUniqueName))
			{
				continue;
			}
			if (activeMine.mineLevel > 120)
			{
				if (activeMine.mineLevel < 77377)
				{
					maxSkullLevel = Math.Max(maxSkullLevel, activeMine.mineLevel);
				}
			}
			else
			{
				maxMineLevel = Math.Max(maxMineLevel, activeMine.mineLevel);
			}
		}
		activeMines.RemoveAll(delegate(MineShaft mine)
		{
			if (mine.mineLevel == 77377)
			{
				return false;
			}
			if (disconnectLevels.Contains(mine.NameOrUniqueName))
			{
				return false;
			}
			if (mine.mineLevel > 120)
			{
				if (mine.mineLevel <= maxSkullLevel)
				{
					return false;
				}
			}
			else if (mine.mineLevel <= maxMineLevel)
			{
				return false;
			}
			if (mine.lifespan == 0 && keepUntickedLevels)
			{
				return false;
			}
			if (Game1.IsServer && Game1.locationRequest?.Location is MineShaft mineShaft && mine.NameOrUniqueName == mineShaft.NameOrUniqueName)
			{
				return false;
			}
			mine.OnRemoved();
			return true;
		});
		if (activeMines.Count == 0)
		{
			Game1.player.team.calicoEggSkullCavernRating.Value = 0;
			Game1.player.team.calicoStatueEffects.Clear();
			deepestLevelOnCurrentDesertFestivalRun = 0;
		}
	}

	public static void UpdateMines10Minutes(int timeOfDay)
	{
		clearInactiveMines();
		if (Game1.IsClient)
		{
			return;
		}
		foreach (MineShaft activeMine in activeMines)
		{
			if (activeMine.farmers.Any())
			{
				activeMine.performTenMinuteUpdate(timeOfDay);
			}
			activeMine.lifespan++;
		}
	}

	protected override void updateCharacters(GameTime time)
	{
		if (farmers.Any())
		{
			base.updateCharacters(time);
		}
	}

	public override void updateEvenIfFarmerIsntHere(GameTime time, bool ignoreWasUpdatedFlush = false)
	{
		base.updateEvenIfFarmerIsntHere(time, ignoreWasUpdatedFlush);
		if (!Game1.shouldTimePass() || !isFogUp.Value)
		{
			return;
		}
		int num = fogTime;
		fogTime -= (int)time.ElapsedGameTime.TotalMilliseconds;
		if (!Game1.IsMasterGame)
		{
			return;
		}
		if (fogTime > 5000 && num % 4000 < fogTime % 4000)
		{
			spawnFlyingMonsterOffScreen();
		}
		if (fogTime <= 0)
		{
			isFogUp.Value = false;
			if (isDarkArea())
			{
				netFogColor.Value = Color.Black;
			}
			else if (GetAdditionalDifficulty() > 0 && getMineArea() == 40 && !isDarkArea())
			{
				netFogColor.Value = default(Color);
			}
		}
	}

	public static void UpdateMines(GameTime time)
	{
		foreach (MineShaft activeMine in activeMines)
		{
			if (activeMine.farmers.Any())
			{
				activeMine.UpdateWhenCurrentLocation(time);
			}
			activeMine.updateEvenIfFarmerIsntHere(time);
		}
	}

	/// <inheritdoc />
	public override void OnRemoved()
	{
		base.OnRemoved();
		mapContent.Dispose();
	}

	/// <summary>Get the location name for a generated mine level.</summary>
	/// <param name="level">The mine level.</param>
	/// <param name="forceLayout">The layout in <c>Maps/Mines</c> to use, or <c>null</c> to choose a random one based on the level.</param>
	public static string GetLevelName(int level, int? forceLayout = null)
	{
		if (forceLayout.HasValue)
		{
			return $"UndergroundMine{level}:{forceLayout}";
		}
		return $"UndergroundMine{level}";
	}

	/// <summary>Get whether a location is a generated mine level.</summary>
	/// <param name="location">The location to check.</param>
	public static bool IsGeneratedLevel(GameLocation location)
	{
		int level;
		int? num;
		return IsGeneratedLevel(location, out level, out num);
	}

	/// <summary>Get whether a location is a generated mine level.</summary>
	/// <param name="location">The location to check.</param>
	/// <param name="level">The parsed mine level, if applicable.</param>
	public static bool IsGeneratedLevel(GameLocation location, out int level)
	{
		int? num;
		return IsGeneratedLevel(location, out level, out num);
	}

	/// <summary>Get whether a location is a generated mine level.</summary>
	/// <param name="location">The location to check.</param>
	/// <param name="level">The parsed mine level, if applicable.</param>
	/// <param name="forceLayout">The layout in <c>Maps/Mines</c> to use, or <c>null</c> to choose a random one based on the level.</param>
	public static bool IsGeneratedLevel(GameLocation location, out int level, out int? forceLayout)
	{
		if (location is MineShaft mineShaft)
		{
			level = mineShaft.mineLevel;
			forceLayout = mineShaft.forceLayout;
			return true;
		}
		level = 0;
		forceLayout = null;
		return false;
	}

	/// <summary>Get whether a location name is a generated mine level.</summary>
	/// <param name="locationName">The location name to check.</param>
	public static bool IsGeneratedLevel(string locationName)
	{
		int level;
		int? num;
		return IsGeneratedLevel(locationName, out level, out num);
	}

	/// <summary>Get whether a location name is a generated mine level.</summary>
	/// <param name="locationName">The location name to check.</param>
	/// <param name="level">The parsed mine level, if applicable.</param>
	public static bool IsGeneratedLevel(string locationName, out int level)
	{
		int? num;
		return IsGeneratedLevel(locationName, out level, out num);
	}

	/// <summary>Get whether a location name is a generated mine level.</summary>
	/// <param name="locationName">The location name to check.</param>
	/// <param name="level">The parsed mine level, if applicable.</param>
	/// <param name="forceLayout">The layout in <c>Maps/Mines</c> to use, or <c>null</c> to choose a random one based on the level.</param>
	public static bool IsGeneratedLevel(string locationName, out int level, out int? forceLayout)
	{
		if (locationName == null || !locationName.StartsWithIgnoreCase("UndergroundMine"))
		{
			level = 0;
			forceLayout = null;
			return false;
		}
		string text = locationName.Substring("UndergroundMine".Length);
		int num = text.IndexOf(':');
		if (num > 0)
		{
			if (int.TryParse(text.Substring(0, num), out level) && int.TryParse(text.Substring(num + 1), out var result))
			{
				forceLayout = result;
				return true;
			}
			level = 0;
			forceLayout = null;
			return false;
		}
		forceLayout = null;
		return int.TryParse(text, out level);
	}

	public static MineShaft GetMine(string name)
	{
		if (!IsGeneratedLevel(name, out var level, out var num))
		{
			Game1.log.Warn("Failed parsing mine level from location name '" + name + "', defaulting to level 0.");
			level = 0;
		}
		if (num.HasValue)
		{
			name = GetLevelName(level);
		}
		foreach (MineShaft activeMine in activeMines)
		{
			if (activeMine.Name == name)
			{
				if (num.HasValue && activeMine.loadedMapNumber != num)
				{
					Game1.log.Warn($"Can't set mine level {level} to layout {num} because it's already active with layout {activeMine.loadedMapNumber}.");
				}
				return activeMine;
			}
		}
		MineShaft mineShaft = new MineShaft(level, num);
		activeMines.Add(mineShaft);
		mineShaft.generateContents();
		return mineShaft;
	}

	public static void ForEach(Action<MineShaft> action)
	{
		foreach (MineShaft activeMine in activeMines)
		{
			action(activeMine);
		}
	}
}
