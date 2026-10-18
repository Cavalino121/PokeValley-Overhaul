using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Constants;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.SpecialOrders;
using StardewValley.TokenizableStrings;

namespace StardewValley.Tools;

public class FishingRod : Tool
{
	/// <summary>The index in <see cref="F:StardewValley.Tool.attachments" /> for equipped bait.</summary>
	public const int BaitIndex = 0;

	/// <summary>The index in <see cref="F:StardewValley.Tool.attachments" /> for equipped tackle.</summary>
	public const int TackleIndex = 1;

	public const int sizeOfLandCheckRectangle = 11;

	public static int NUM_BOBBER_STYLES = 39;

	[XmlElement("bobber")]
	public readonly NetPosition bobber = new NetPosition();

	/// <summary>The underlying field for <see cref="P:StardewValley.Tools.FishingRod.CastDirection" />.</summary>
	private readonly NetInt castDirection = new NetInt(-1);

	public static int minFishingBiteTime = 600;

	public static int maxFishingBiteTime = 30000;

	public static int maxTimeToNibble = 800;

	public static int maxTackleUses = 20;

	private int whichTackleSlotToReplace = 1;

	protected Vector2 _lastAppliedMotion = Vector2.Zero;

	protected Vector2[] _totalMotionBuffer = new Vector2[4];

	protected int _totalMotionBufferIndex;

	protected NetVector2 _totalMotion = new NetVector2(Vector2.Zero)
	{
		InterpolationEnabled = false,
		InterpolationWait = false
	};

	public static double baseChanceForTreasure = 0.15;

	[XmlIgnore]
	public int bobberBob;

	[XmlIgnore]
	public float bobberTimeAccumulator;

	[XmlIgnore]
	public float timePerBobberBob = 2000f;

	[XmlIgnore]
	public float timeUntilFishingBite = -1f;

	[XmlIgnore]
	public float fishingBiteAccumulator;

	[XmlIgnore]
	public float fishingNibbleAccumulator;

	[XmlIgnore]
	public float timeUntilFishingNibbleDone = -1f;

	[XmlIgnore]
	public float castingPower;

	[XmlIgnore]
	public float castingChosenCountdown;

	[XmlIgnore]
	public float castingTimerSpeed = 0.001f;

	[XmlIgnore]
	public bool isFishing;

	[XmlIgnore]
	public bool hit;

	[XmlIgnore]
	public bool isNibbling;

	[XmlIgnore]
	public bool favBait;

	[XmlIgnore]
	public bool isTimingCast;

	[XmlIgnore]
	public bool isCasting;

	[XmlIgnore]
	public bool castedButBobberStillInAir;

	[XmlIgnore]
	public bool gotTroutDerbyTag;

	/// <summary>The cached value for <see cref="M:StardewValley.Tools.FishingRod.GetWaterColor" />.</summary>
	protected Color? lastWaterColor;

	[XmlIgnore]
	protected bool _hasPlayerAdjustedBobber;

	[XmlIgnore]
	public bool lastCatchWasJunk;

	[XmlIgnore]
	public bool goldenTreasure;

	[XmlIgnore]
	public bool doneWithAnimation;

	[XmlIgnore]
	public bool pullingOutOfWater;

	[XmlIgnore]
	public bool isReeling;

	[XmlIgnore]
	public bool hasDoneFucntionYet;

	[XmlIgnore]
	public bool fishCaught;

	[XmlIgnore]
	public bool recordSize;

	[XmlIgnore]
	public bool treasureCaught;

	[XmlIgnore]
	public bool showingTreasure;

	[XmlIgnore]
	public bool hadBobber;

	[XmlIgnore]
	public bool bossFish;

	[XmlIgnore]
	public bool fromFishPond;

	[XmlIgnore]
	public TemporaryAnimatedSpriteList animations = new TemporaryAnimatedSpriteList();

	[XmlIgnore]
	public SparklingText sparklingText;

	[XmlIgnore]
	public int fishSize;

	[XmlIgnore]
	public int fishQuality;

	[XmlIgnore]
	public int clearWaterDistance;

	[XmlIgnore]
	public int originalFacingDirection;

	[XmlIgnore]
	public int numberOfFishCaught = 1;

	[XmlIgnore]
	public ItemMetadata whichFish;

	/// <summary>The mail flag to set for the current player when the current <see cref="F:StardewValley.Tools.FishingRod.whichFish" /> is successfully caught.</summary>
	[XmlIgnore]
	public string setFlagOnCatch;

	/// <summary>The delay (in milliseconds) before recasting if the left mouse is held down after closing the 'caught fish' display.</summary>
	[XmlIgnore]
	public int recastTimerMs;

	protected const int RECAST_DELAY_MS = 200;

	[XmlIgnore]
	private readonly NetEventBinary pullFishFromWaterEvent = new NetEventBinary();

	[XmlIgnore]
	private readonly NetEvent1Field<bool, NetBool> doneFishingEvent = new NetEvent1Field<bool, NetBool>();

	[XmlIgnore]
	private readonly NetEvent0 startCastingEvent = new NetEvent0();

	[XmlIgnore]
	private readonly NetEvent0 castingEndEnableMovementEvent = new NetEvent0();

	[XmlIgnore]
	private readonly NetEvent0 putAwayEvent = new NetEvent0();

	[XmlIgnore]
	private readonly NetEvent0 beginReelingEvent = new NetEvent0();

	public static ICue chargeSound;

	public static ICue reelSound;

	private int randomBobberStyle = -1;

	private bool usedGamePadToCast;

	/// <summary>The direction in which the fishing rod was cast.</summary>
	public int CastDirection
	{
		get
		{
			if (fishCaught)
			{
				return 2;
			}
			return castDirection.Value;
		}
		set
		{
			castDirection.Value = value;
		}
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(bobber.NetFields, "bobber.NetFields").AddField(castDirection, "castDirection").AddField(pullFishFromWaterEvent, "pullFishFromWaterEvent")
			.AddField(doneFishingEvent, "doneFishingEvent")
			.AddField(startCastingEvent, "startCastingEvent")
			.AddField(castingEndEnableMovementEvent, "castingEndEnableMovementEvent")
			.AddField(putAwayEvent, "putAwayEvent")
			.AddField(_totalMotion, "_totalMotion")
			.AddField(beginReelingEvent, "beginReelingEvent");
		pullFishFromWaterEvent.AddReaderHandler(doPullFishFromWater);
		doneFishingEvent.onEvent += doDoneFishing;
		startCastingEvent.onEvent += doStartCasting;
		castingEndEnableMovementEvent.onEvent += doCastingEndEnableMovement;
		beginReelingEvent.onEvent += beginReeling;
		putAwayEvent.onEvent += resetState;
	}

	/// <inheritdoc />
	protected override void MigrateLegacyItemId()
	{
		switch (base.UpgradeLevel)
		{
		case 0:
			base.ItemId = "BambooPole";
			break;
		case 1:
			base.ItemId = "TrainingRod";
			break;
		case 2:
			base.ItemId = "FiberglassRod";
			break;
		case 3:
			base.ItemId = "IridiumRod";
			break;
		case 4:
			base.ItemId = "AdvancedIridiumRod";
			break;
		default:
			base.ItemId = "BambooPole";
			break;
		}
	}

	public override void actionWhenStopBeingHeld(Farmer who)
	{
		putAwayEvent.Fire();
		base.actionWhenStopBeingHeld(who);
	}

	public FishingRod()
		: base("Fishing Rod", 0, 189, 8, stackable: false, 2)
	{
	}

	public override void resetState()
	{
		isNibbling = false;
		fishCaught = false;
		isFishing = false;
		isReeling = false;
		isCasting = false;
		isTimingCast = false;
		doneWithAnimation = false;
		pullingOutOfWater = false;
		fromFishPond = false;
		numberOfFishCaught = 1;
		fishingBiteAccumulator = 0f;
		showingTreasure = false;
		fishingNibbleAccumulator = 0f;
		timeUntilFishingBite = -1f;
		timeUntilFishingNibbleDone = -1f;
		bobberTimeAccumulator = 0f;
		castingChosenCountdown = 0f;
		lastWaterColor = null;
		gotTroutDerbyTag = false;
		_totalMotionBufferIndex = 0;
		for (int i = 0; i < _totalMotionBuffer.Length; i++)
		{
			_totalMotionBuffer[i] = Vector2.Zero;
		}
		if (lastUser != null && lastUser == Game1.player)
		{
			Game1.screenOverlayTempSprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.id == 987654321);
		}
		_totalMotion.Value = Vector2.Zero;
		_lastAppliedMotion = Vector2.Zero;
		pullFishFromWaterEvent.Clear();
		doneFishingEvent.Clear();
		startCastingEvent.Clear();
		castingEndEnableMovementEvent.Clear();
		beginReelingEvent.Clear();
		bobber.Set(Vector2.Zero);
		CastDirection = -1;
	}

	public FishingRod(int upgradeLevel)
		: base("Fishing Rod", upgradeLevel, 189, 8, stackable: false, (upgradeLevel == 4) ? 3 : 2)
	{
		base.IndexOfMenuItemView = 8 + upgradeLevel;
	}

	public FishingRod(int upgradeLevel, int numAttachmentSlots)
		: base("Fishing Rod", upgradeLevel, 189, 8, stackable: false, numAttachmentSlots)
	{
		base.IndexOfMenuItemView = 8 + upgradeLevel;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new FishingRod();
	}

	private int getAddedDistance(Farmer who)
	{
		if (who.FishingLevel >= 15)
		{
			return 4;
		}
		if (who.FishingLevel >= 8)
		{
			return 3;
		}
		if (who.FishingLevel >= 4)
		{
			return 2;
		}
		if (who.FishingLevel >= 1)
		{
			return 1;
		}
		return 0;
	}

	private Vector2 calculateBobberTile()
	{
		return new Vector2(bobber.X / 64f, bobber.Y / 64f);
	}

	public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
	{
		who = who ?? lastUser;
		if (fishCaught || (!who.IsLocalPlayer && (isReeling || isFishing || pullingOutOfWater)))
		{
			return;
		}
		hasDoneFucntionYet = true;
		Vector2 vector = calculateBobberTile();
		int tileX = (int)vector.X;
		int tileY = (int)vector.Y;
		base.DoFunction(location, x, y, power, who);
		if (doneWithAnimation)
		{
			who.canReleaseTool = true;
		}
		if (Game1.isAnyGamePadButtonBeingPressed())
		{
			Game1.lastCursorMotionWasMouse = false;
		}
		if (!isFishing && !castedButBobberStillInAir && !pullingOutOfWater && !isNibbling && !hit && !showingTreasure)
		{
			if (!Game1.eventUp && who.IsLocalPlayer && !hasEnchantmentOfType<EfficientToolEnchantment>())
			{
				float stamina = who.Stamina;
				who.Stamina -= 8f - (float)who.FishingLevel * 0.1f;
				who.checkForExhaustion(stamina);
			}
			if (location.canFishHere() && location.isTileFishable(tileX, tileY))
			{
				clearWaterDistance = distanceToLand((int)(bobber.X / 64f), (int)(bobber.Y / 64f), who.currentLocation);
				isFishing = true;
				location.temporarySprites.Add(new TemporaryAnimatedSprite(28, 100f, 2, 1, new Vector2(bobber.X - 32f, bobber.Y - 32f), flicker: false, flipped: false));
				if (who.IsLocalPlayer)
				{
					if (PlayUseSounds)
					{
						location.playSound("dropItemInWater", vector);
					}
					Game1.stats.TimesFished++;
				}
				timeUntilFishingBite = calculateTimeUntilFishingBite(vector, isFirstCast: true, who);
				if (location.fishSplashPoint != null)
				{
					bool flag = location.fishFrenzyFish.Value != null && !location.fishFrenzyFish.Equals("");
					Rectangle value = new Rectangle(location.fishSplashPoint.X * 64, location.fishSplashPoint.Y * 64, 64, 64);
					if (flag)
					{
						value.Inflate(32, 32);
					}
					if (new Rectangle((int)bobber.X - 32, (int)bobber.Y - 32, 64, 64).Intersects(value))
					{
						timeUntilFishingBite /= (flag ? 2 : 4);
						location.temporarySprites.Add(new TemporaryAnimatedSprite(10, bobber.Value - new Vector2(32f, 32f), Color.Cyan));
					}
				}
				who.UsingTool = true;
				who.canMove = false;
			}
			else
			{
				if (doneWithAnimation)
				{
					who.UsingTool = false;
				}
				if (doneWithAnimation)
				{
					who.canMove = true;
				}
			}
			return;
		}
		if (isCasting || pullingOutOfWater)
		{
			return;
		}
		bool flag2 = location.isTileBuildingFishable((int)vector.X, (int)vector.Y);
		who.FarmerSprite.PauseForSingleAnimation = false;
		int result = who.FacingDirection;
		switch (result)
		{
		case 0:
			who.FarmerSprite.animateBackwardsOnce(299, 35f);
			break;
		case 1:
			who.FarmerSprite.animateBackwardsOnce(300, 35f);
			break;
		case 2:
			who.FarmerSprite.animateBackwardsOnce(301, 35f);
			break;
		case 3:
			who.FarmerSprite.animateBackwardsOnce(302, 35f);
			break;
		}
		Item o;
		bool flag4;
		string qualifiedItemId;
		if (isNibbling)
		{
			Object bait = GetBait();
			double num = ((bait != null) ? ((float)bait.Price / 10f) : 0f);
			bool flag3 = false;
			if (location.fishSplashPoint != null)
			{
				Rectangle rectangle = new Rectangle(location.fishSplashPoint.X * 64, location.fishSplashPoint.Y * 64, 64, 64);
				Rectangle value2 = new Rectangle((int)bobber.X - 80, (int)bobber.Y - 80, 64, 64);
				flag3 = rectangle.Intersects(value2);
			}
			o = location.getFish(fishingNibbleAccumulator, bait?.QualifiedItemId, clearWaterDistance + (flag3 ? 1 : 0), who, num + (flag3 ? 0.4 : 0.0), vector);
			if (o == null || ItemRegistry.GetDataOrErrorItem(o.QualifiedItemId).IsErrorItem)
			{
				result = Game1.random.Next(167, 173);
				o = ItemRegistry.Create("(O)" + result);
			}
			Object obj = o as Object;
			if (obj != null && obj.scale.X == 1f)
			{
				favBait = true;
			}
			Dictionary<string, string> dictionary = DataLoader.Fish(Game1.content);
			flag4 = false;
			string value3;
			if (!o.HasTypeObject())
			{
				flag4 = true;
			}
			else if (dictionary.TryGetValue(o.ItemId, out value3))
			{
				if (!int.TryParse(value3.Split('/')[1], out result))
				{
					flag4 = true;
				}
			}
			else
			{
				flag4 = true;
			}
			lastCatchWasJunk = false;
			qualifiedItemId = o.QualifiedItemId;
			if (qualifiedItemId != null)
			{
				result = qualifiedItemId.Length;
				if (result != 5)
				{
					if (result == 6)
					{
						switch (qualifiedItemId[5])
						{
						case '2':
							break;
						case '3':
							goto IL_0670;
						case '7':
							goto IL_0697;
						case '0':
							goto IL_06cf;
						case '1':
							goto IL_06ed;
						case '4':
							goto IL_06fd;
						case '5':
							goto IL_070d;
						case '6':
							goto IL_071d;
						case '8':
							goto IL_072d;
						default:
							goto IL_0760;
						}
						switch (qualifiedItemId)
						{
						case "(O)152":
						case "(O)842":
						case "(O)822":
							break;
						default:
							goto IL_0760;
						}
						goto IL_075b;
					}
				}
				else
				{
					char c = qualifiedItemId[4];
					if (c != '3')
					{
						if (c == '9' && qualifiedItemId == "(O)79")
						{
							goto IL_075b;
						}
					}
					else if (qualifiedItemId == "(O)73")
					{
						goto IL_075b;
					}
				}
			}
			goto IL_0760;
		}
		if (flag2 && Game1.timeOfDay < 2600)
		{
			Item fish = location.getFish(-1f, null, -1, who, -1.0, vector);
			if (fish != null)
			{
				pullFishFromWater(fish.QualifiedItemId, -1, 0, 0, treasureCaught: false, wasPerfect: false, fromFishPond: true, null, isBossFish: false, 1);
				return;
			}
		}
		if (PlayUseSounds && who.IsLocalPlayer)
		{
			location.playSound("pullItemFromWater", vector);
		}
		isFishing = false;
		pullingOutOfWater = true;
		Point standingPixel = who.StandingPixel;
		if (who.FacingDirection == 1 || who.FacingDirection == 3)
		{
			float num2 = Math.Abs(bobber.X - (float)standingPixel.X);
			float num3 = 0.005f;
			float num4 = 0f - (float)Math.Sqrt(num2 * num3 / 2f);
			float num5 = 2f * (Math.Abs(num4 - 0.5f) / num3);
			num5 *= 1.2f;
			Rectangle sourceRectForStandardTileSheet = Game1.getSourceRectForStandardTileSheet(Game1.bobbersTexture, getBobberStyle(who), 16, 32);
			sourceRectForStandardTileSheet.Height = 16;
			animations.Add(new TemporaryAnimatedSprite("TileSheets\\bobbers", sourceRectForStandardTileSheet, num5, 1, 0, bobber.Value + new Vector2(-32f, -48f), flicker: false, flipped: false, (float)standingPixel.Y / 10000f, 0f, Color.White, 4f, 0f, 0f, (float)Game1.random.Next(-20, 20) / 100f)
			{
				motion = new Vector2((float)((who.FacingDirection != 3) ? 1 : (-1)) * (num4 + 0.2f), num4 - 0.8f),
				acceleration = new Vector2(0f, num3),
				endFunction = donefishingEndFunction,
				timeBasedMotion = true,
				alphaFade = 0.001f,
				flipped = (who.FacingDirection == 1 && flipCurrentBobberWhenFacingRight())
			});
		}
		else
		{
			float num6 = bobber.Y - (float)standingPixel.Y;
			float num7 = Math.Abs(num6 + 256f);
			float num8 = 0.005f;
			float num9 = (float)Math.Sqrt(2f * num8 * num7);
			float animationInterval = (float)(Math.Sqrt(2f * (num7 - num6) / num8) + (double)(num9 / num8));
			Rectangle sourceRectForStandardTileSheet2 = Game1.getSourceRectForStandardTileSheet(Game1.bobbersTexture, getBobberStyle(who), 16, 32);
			sourceRectForStandardTileSheet2.Height = 16;
			animations.Add(new TemporaryAnimatedSprite("TileSheets\\bobbers", sourceRectForStandardTileSheet2, animationInterval, 1, 0, bobber.Value + new Vector2(-32f, -48f), flicker: false, flipped: false, bobber.Y / 10000f, 0f, Color.White, 4f, 0f, 0f, (float)Game1.random.Next(-20, 20) / 100f)
			{
				motion = new Vector2(((float)who.StandingPixel.X - bobber.Value.X) / 800f, 0f - num9),
				acceleration = new Vector2(0f, num8),
				endFunction = donefishingEndFunction,
				timeBasedMotion = true,
				alphaFade = 0.001f
			});
		}
		who.UsingTool = true;
		who.canReleaseTool = false;
		return;
		IL_0789:
		bool flag5;
		if (base.ItemId == "PokemonSuperRod" || base.Name == "PokemonSuperRod")
		{
			flag5 = false;
			flag2 = false;
			flag4 = false;
			lastCatchWasJunk = false;
			if (o == null || o.Category == -20 || !o.HasTypeObject())
			{
				o = ItemRegistry.Create("(O)136");
			}
		}
		if (flag5 || flag2 || flag4)
		{
			lastCatchWasJunk = true;
			pullFishFromWater(o.QualifiedItemId, -1, 0, 0, treasureCaught: false, wasPerfect: false, flag2, o.SetFlagOnPickup, isBossFish: false, 1);
		}
		else if (!hit && who.IsLocalPlayer)
		{
			hit = true;
			Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(612, 1913, 74, 30), 1500f, 1, 0, Game1.GlobalToLocal(Game1.viewport, bobber.Value + new Vector2(-140f, -160f)), flicker: false, flipped: false, 1f, 0.005f, Color.White, 4f, 0.075f, 0f, 0f, local: true)
			{
				scaleChangeChange = -0.005f,
				motion = new Vector2(0f, -0.1f),
				endFunction = delegate
				{
					startMinigameEndFunction(o);
				},
				id = 987654321
			});
			if (PlayUseSounds)
			{
				who.playNearbySoundLocal("FishHit");
			}
		}
		return;
		IL_06ed:
		if (qualifiedItemId == "(O)821")
		{
			goto IL_075b;
		}
		goto IL_0760;
		IL_06fd:
		if (qualifiedItemId == "(O)824")
		{
			goto IL_075b;
		}
		goto IL_0760;
		IL_0670:
		if (qualifiedItemId == "(O)153" || qualifiedItemId == "(O)823")
		{
			goto IL_075b;
		}
		goto IL_0760;
		IL_070d:
		if (qualifiedItemId == "(O)825")
		{
			goto IL_075b;
		}
		goto IL_0760;
		IL_0760:
		flag5 = o.Category == -20 || o.QualifiedItemId == GameLocation.CAROLINES_NECKLACE_ITEM_QID;
		goto IL_0789;
		IL_075b:
		flag5 = true;
		goto IL_0789;
		IL_06cf:
		if (qualifiedItemId == "(O)890" || qualifiedItemId == "(O)820")
		{
			goto IL_075b;
		}
		goto IL_0760;
		IL_072d:
		if (qualifiedItemId == "(O)828")
		{
			goto IL_075b;
		}
		goto IL_0760;
		IL_071d:
		if (qualifiedItemId == "(O)826")
		{
			goto IL_075b;
		}
		goto IL_0760;
		IL_0697:
		switch (qualifiedItemId)
		{
		case "(O)157":
		case "(O)797":
		case "(O)827":
			break;
		default:
			goto IL_0760;
		}
		goto IL_075b;
	}

	public int getBobberStyle(Farmer who)
	{
		if (GetTackleQualifiedItemIDs().Contains("(O)789"))
		{
			return 39;
		}
		if (who != null)
		{
			if (randomBobberStyle == -1 && who.usingRandomizedBobber && randomBobberStyle == -1)
			{
				who.bobberStyle.Value = Math.Min(NUM_BOBBER_STYLES - 1, Game1.random.Next(Game1.player.fishCaught.Count() / 2));
				randomBobberStyle = who.bobberStyle.Value;
			}
			return who.bobberStyle.Value;
		}
		return 0;
	}

	public bool flipCurrentBobberWhenFacingRight()
	{
		switch (getBobberStyle(getLastFarmerToUse()))
		{
		case 9:
		case 19:
		case 21:
		case 23:
		case 36:
			return true;
		default:
			return false;
		}
	}

	public Color getFishingLineColor()
	{
		switch (getBobberStyle(getLastFarmerToUse()))
		{
		case 6:
		case 20:
			return new Color(255, 200, 255);
		case 7:
			return Color.Yellow;
		case 35:
		case 39:
			return new Color(180, 160, 255);
		case 9:
			return new Color(255, 255, 200);
		case 10:
			return new Color(255, 208, 169);
		case 11:
			return new Color(170, 170, 255);
		case 12:
			return Color.DimGray;
		case 14:
		case 22:
			return new Color(178, 255, 112);
		case 15:
			return new Color(250, 193, 70);
		case 16:
			return new Color(255, 170, 170);
		case 37:
		case 38:
			return new Color(200, 255, 255);
		case 17:
			return new Color(200, 220, 255);
		case 13:
			return new Color(228, 228, 172);
		case 31:
			return Color.Red * 0.5f;
		case 29:
		case 32:
			return Color.Lime * 0.66f;
		case 25:
		case 27:
			return Color.White * 0.5f;
		default:
			return Color.White;
		}
	}

	private float calculateTimeUntilFishingBite(Vector2 bobberTile, bool isFirstCast, Farmer who)
	{
		if (base.ItemId == "PokemonSuperRod" || base.Name == "PokemonSuperRod")
		{
			return Game1.random.Next(600, 1400);
		}
		if (Game1.currentLocation.isTileBuildingFishable((int)bobberTile.X, (int)bobberTile.Y) && Game1.currentLocation.getBuildingAt(bobberTile) is FishPond fishPond && fishPond.currentOccupants.Value > 0)
		{
			return FishPond.FISHING_MILLISECONDS;
		}
		List<string> tackleQualifiedItemIDs = GetTackleQualifiedItemIDs();
		string text = GetBait()?.QualifiedItemId;
		int num = 0;
		num += Utility.getStringCountInList(tackleQualifiedItemIDs, "(O)687") * 10000;
		num += Utility.getStringCountInList(tackleQualifiedItemIDs, "(O)686") * 5000;
		float num2 = Game1.random.Next(minFishingBiteTime, Math.Max(minFishingBiteTime, maxFishingBiteTime - 250 * who.FishingLevel - num));
		if (isFirstCast)
		{
			num2 *= 0.75f;
		}
		if (text != null)
		{
			num2 *= 0.5f;
			switch (text)
			{
			case "(O)774":
			case "(O)ChallengeBait":
				num2 *= 0.75f;
				break;
			case "(O)DeluxeBait":
				num2 *= 0.66f;
				break;
			}
		}
		return Math.Max(500f, num2);
	}

	public Color getColor()
	{
		return upgradeLevel.Value switch
		{
			0 => Color.Goldenrod, 
			1 => Color.OliveDrab, 
			2 => Color.White, 
			3 => Color.Violet, 
			4 => new Color(128, 143, 255), 
			_ => Color.White, 
		};
	}

	public static int distanceToLand(int tileX, int tileY, GameLocation location, bool landMustBeAdjacentToWalkableTile = false)
	{
		Rectangle r = new Rectangle(tileX - 1, tileY - 1, 3, 3);
		bool flag = false;
		int num = 1;
		while (!flag && r.Width <= 11)
		{
			foreach (Vector2 item in Utility.getBorderOfThisRectangle(r))
			{
				if (!location.isTileOnMap(item) || location.isWaterTile((int)item.X, (int)item.Y))
				{
					continue;
				}
				flag = true;
				num = r.Width / 2;
				if (!landMustBeAdjacentToWalkableTile)
				{
					break;
				}
				flag = false;
				Vector2[] surroundingTileLocationsArray = Utility.getSurroundingTileLocationsArray(item);
				foreach (Vector2 tileLocation in surroundingTileLocationsArray)
				{
					if (location.isTilePassable(tileLocation) && !location.isWaterTile((int)item.X, (int)item.Y))
					{
						flag = true;
						break;
					}
				}
				break;
			}
			r.Inflate(1, 1);
		}
		if (r.Width > 11)
		{
			num = 6;
		}
		return num - 1;
	}

	public void startMinigameEndFunction(Item fish)
	{
		fish.TryGetTempData<bool>("IsBossFish", out bossFish);
		Farmer farmer = lastUser;
		beginReelingEvent.Fire();
		isReeling = true;
		hit = false;
		switch (farmer.FacingDirection)
		{
		case 1:
			farmer.FarmerSprite.setCurrentSingleFrame(48, 32000);
			break;
		case 3:
			farmer.FarmerSprite.setCurrentSingleFrame(48, 32000, secondaryArm: false, flip: true);
			break;
		}
		float num = 1f;
		num *= (float)clearWaterDistance / 5f;
		int num2 = 1 + farmer.FishingLevel / 2;
		num *= (float)Game1.random.Next(num2, Math.Max(6, num2)) / 5f;
		if (favBait)
		{
			num *= 1.2f;
		}
		num *= 1f + (float)Game1.random.Next(-10, 11) / 100f;
		num = Math.Max(0f, Math.Min(1f, num));
		string text = GetBait()?.QualifiedItemId;
		List<string> tackleQualifiedItemIDs = GetTackleQualifiedItemIDs();
		double num3 = (double)Utility.getStringCountInList(tackleQualifiedItemIDs, "(O)693") * baseChanceForTreasure / 3.0;
		goldenTreasure = false;
		int num4;
		if (!Game1.isFestival())
		{
			NetStringIntArrayDictionary netStringIntArrayDictionary = farmer.fishCaught;
			if (netStringIntArrayDictionary != null && netStringIntArrayDictionary.Length > 1)
			{
				num4 = ((Game1.random.NextDouble() < baseChanceForTreasure + (double)farmer.LuckLevel * 0.005 + ((text == "(O)703") ? baseChanceForTreasure : 0.0) + num3 + farmer.DailyLuck / 2.0 + (farmer.professions.Contains(9) ? baseChanceForTreasure : 0.0)) ? 1 : 0);
				goto IL_01cc;
			}
		}
		num4 = 0;
		goto IL_01cc;
		IL_01cc:
		bool flag = (byte)num4 != 0;
		if (flag && Game1.player.stats.Get(StatKeys.Mastery(1)) != 0 && Game1.random.NextDouble() < 0.25 + Game1.player.team.AverageDailyLuck())
		{
			goldenTreasure = true;
		}
		Game1.activeClickableMenu = new BobberBar(fish.ItemId, num, flag, tackleQualifiedItemIDs, fish.SetFlagOnPickup, bossFish, text, goldenTreasure);
	}

	/// <summary>Get the equipped tackle, if any.</summary>
	public List<Object> GetTackle()
	{
		List<Object> list = new List<Object>();
		if (CanUseTackle())
		{
			for (int i = 1; i < attachments.Count; i++)
			{
				list.Add(attachments[i]);
			}
		}
		return list;
	}

	public List<string> GetTackleQualifiedItemIDs()
	{
		List<string> list = new List<string>();
		foreach (Object item in GetTackle())
		{
			if (item != null)
			{
				list.Add(item.QualifiedItemId);
			}
		}
		return list;
	}

	/// <summary>Get the equipped bait, if any.</summary>
	public Object GetBait()
	{
		if (!CanUseBait())
		{
			return null;
		}
		return attachments[0];
	}

	/// <summary>Whether the fishing rod has Magic Bait equipped.</summary>
	public bool HasMagicBait()
	{
		return GetBait()?.QualifiedItemId == "(O)908";
	}

	/// <summary>Whether the fishing rod has a Curiosity Lure equipped.</summary>
	public bool HasCuriosityLure()
	{
		return GetTackleQualifiedItemIDs().Contains("(O)856");
	}

	public bool inUse()
	{
		if (!isFishing && !isCasting && !isTimingCast && !isNibbling && !isReeling)
		{
			return fishCaught;
		}
		return true;
	}

	public void donefishingEndFunction(int extra)
	{
		Farmer farmer = lastUser;
		isFishing = false;
		isReeling = false;
		farmer.canReleaseTool = true;
		farmer.canMove = true;
		farmer.UsingTool = false;
		farmer.FarmerSprite.PauseForSingleAnimation = false;
		pullingOutOfWater = false;
		doneFishing(farmer);
	}

	public static void endOfAnimationBehavior(Farmer f)
	{
	}

	public override void drawAttachments(SpriteBatch b, int x, int y)
	{
		y += ((enchantments.Count > 0) ? 8 : 4);
		if (CanUseBait())
		{
			DrawAttachmentSlot(0, b, x, y);
		}
		y += 68;
		if (CanUseTackle())
		{
			for (int i = 1; i < base.AttachmentSlotsCount; i++)
			{
				DrawAttachmentSlot(i, b, x, y);
				x += 68;
			}
		}
	}

	/// <inheritdoc />
	protected override void GetAttachmentSlotSprite(int slot, out Texture2D texture, out Rectangle sourceRect)
	{
		base.GetAttachmentSlotSprite(slot, out texture, out sourceRect);
		if (slot == 0)
		{
			if (GetBait() == null)
			{
				sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 36);
			}
		}
		else if (attachments[slot] == null)
		{
			sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 37);
		}
	}

	/// <inheritdoc />
	protected override bool canThisBeAttached(Object o, int slot)
	{
		if (o.QualifiedItemId == "(O)789" && slot != 0)
		{
			return true;
		}
		if (slot != 0)
		{
			if (o.Category == -22)
			{
				return CanUseTackle();
			}
			return false;
		}
		if (o.Category == -21)
		{
			return CanUseBait();
		}
		return false;
	}

	/// <summary>Whether the fishing rod has a bait attachment slot.</summary>
	public bool CanUseBait()
	{
		return base.AttachmentSlotsCount > 0;
	}

	/// <summary>Whether the fishing rod has a tackle attachment slot.</summary>
	public bool CanUseTackle()
	{
		return base.AttachmentSlotsCount > 1;
	}

	public void playerCaughtFishEndFunction(bool isBossFish)
	{
		Farmer farmer = lastUser;
		farmer.Halt();
		farmer.armOffset = Vector2.Zero;
		castedButBobberStillInAir = false;
		fishCaught = true;
		isFishing = false;
		pullingOutOfWater = false;
		farmer.canReleaseTool = false;
		if (!farmer.IsLocalPlayer)
		{
			return;
		}

		if (base.ItemId == "PokemonSuperRod" || base.Name == "PokemonSuperRod" || Game1.random.NextDouble() < 0.70)
		{
			fishCaught = false;
			doneFishing(farmer);

			string chosen = GetWaterPokemonForLocation(farmer.currentLocation);
			var species = StardewValley.Pokemon.PokemonSpecies.Get(chosen);
			int level = Math.Clamp(Game1.random.Next(8, 22) + farmer.FishingLevel, 5, 55);
			var wildWater = new StardewValley.Pokemon.PokemonInstance(species, level);
			Game1.playSound("slosh");
			Game1.activeClickableMenu = new StardewValley.Pokemon.PokemonBattleMenu(wildWater, isWild: true);
			return;
		}
		bool flag = whichFish.QualifiedItemId.StartsWith("(O)") && !farmer.fishCaught.ContainsKey(whichFish.QualifiedItemId) && !whichFish.QualifiedItemId.Equals("(O)388") && !whichFish.QualifiedItemId.Equals("(O)390");
		if (!Game1.isFestival())
		{
			recordSize = farmer.caughtFish(whichFish.QualifiedItemId, fishSize, fromFishPond, numberOfFishCaught);
			farmer.faceDirection(2);
		}
		else
		{
			Game1.currentLocation.currentEvent.caughtFish(whichFish.QualifiedItemId, fishSize, farmer);
			fishCaught = false;
			doneFishing(farmer);
		}
		if (isBossFish)
		{
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14068"));
			Game1.multiplayer.globalChatInfoMessage("CaughtLegendaryFish", farmer.Name, TokenStringBuilder.ItemName(whichFish.QualifiedItemId));
		}
		else if (recordSize)
		{
			sparklingText = new SparklingText(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14069"), Color.LimeGreen, Color.Azure);
			if (!flag)
			{
				farmer.playNearbySoundLocal("newRecord");
			}
		}
		else
		{
			farmer.playNearbySoundLocal("fishSlap");
		}
		if (flag && farmer.fishCaught.ContainsKey(whichFish.QualifiedItemId))
		{
			sparklingText = new SparklingText(Game1.dialogueFont, Game1.content.LoadString("Strings\\1_6_Strings:FirstCatch"), new Color(200, 255, 220), Color.White);
			farmer.playNearbySoundLocal("discoverMineral");
		}
	}

	private string GetWaterPokemonForLocation(GameLocation loc)
	{
		string locName = loc?.Name ?? "";
		bool isLava = false;
		if (loc is StardewValley.Locations.MineShaft ms && ms.mineLevel >= 100) isLava = true;
		else if (locName.Contains("Volcano", StringComparison.OrdinalIgnoreCase) || locName.Contains("Caldera", StringComparison.OrdinalIgnoreCase)) isLava = true;

		bool isSewer = loc is StardewValley.Locations.Sewer || locName.Contains("Sewer", StringComparison.OrdinalIgnoreCase) || locName.Contains("BugLand", StringComparison.OrdinalIgnoreCase);
		bool isOcean = loc is StardewValley.Locations.Beach || locName.Contains("Beach", StringComparison.OrdinalIgnoreCase) || locName.Contains("IslandSouth", StringComparison.OrdinalIgnoreCase);
		bool isLake = loc is StardewValley.Locations.Mountain || locName.Contains("Mountain", StringComparison.OrdinalIgnoreCase) || locName.Contains("Woods", StringComparison.OrdinalIgnoreCase);
		bool isRiver = !isOcean && !isLake && !isSewer && !isLava;
		bool isRaining = Game1.isRaining;
		bool isNight = Game1.timeOfDay >= 1900;

		return StardewValley.Pokemon.PokemonEncounterManager.GetFishingSpecies(loc, isLava, isSewer, isOcean, isRiver, isLake, isNight, isRaining);
	}

	public void pullFishFromWater(string fishId, int fishSize, int fishQuality, int fishDifficulty, bool treasureCaught, bool wasPerfect, bool fromFishPond, string setFlagOnCatch, bool isBossFish, int numCaught)
	{
		pullFishFromWaterEvent.Fire(delegate(BinaryWriter writer)
		{
			writer.Write(fishId);
			writer.Write(fishSize);
			writer.Write(fishQuality);
			writer.Write(fishDifficulty);
			writer.Write(treasureCaught);
			writer.Write(wasPerfect);
			writer.Write(fromFishPond);
			writer.Write(setFlagOnCatch ?? string.Empty);
			writer.Write(isBossFish);
			writer.Write(numCaught);
		});
	}

	private void doPullFishFromWater(BinaryReader argReader)
	{
		Farmer farmer = lastUser;
		string text = argReader.ReadString();
		int num = argReader.ReadInt32();
		int num2 = argReader.ReadInt32();
		int num3 = argReader.ReadInt32();
		bool flag = argReader.ReadBoolean();
		bool flag2 = argReader.ReadBoolean();
		bool flag3 = argReader.ReadBoolean();
		string text2 = argReader.ReadString();
		bool isBossFish = argReader.ReadBoolean();
		int num4 = argReader.ReadInt32();
		treasureCaught = flag;
		fishSize = num;
		fishQuality = num2;
		whichFish = ItemRegistry.GetMetadata(text);
		fromFishPond = flag3;
		setFlagOnCatch = ((text2 != string.Empty) ? text2 : null);
		numberOfFishCaught = num4;
		Vector2 value = calculateBobberTile();
		bool flag4 = whichFish.TypeIdentifier == "(O)";
		if (num2 >= 2 && flag2)
		{
			fishQuality = 4;
		}
		else if (num2 >= 1 && flag2)
		{
			fishQuality = 2;
		}
		if (farmer == null)
		{
			return;
		}
		if (!Game1.isFestival() && farmer.IsLocalPlayer && !flag3 && flag4)
		{
			int num5 = Math.Max(1, (num2 + 1) * 3 + num3 / 3);
			if (flag)
			{
				num5 += (int)((float)num5 * 1.2f);
			}
			if (flag2)
			{
				num5 += (int)((float)num5 * 1.4f);
			}
			if (isBossFish)
			{
				num5 *= 5;
			}
			farmer.gainExperience(1, num5);
		}
		if (fishQuality < 0)
		{
			fishQuality = 0;
		}
		string textureName;
		Rectangle sourceRect;
		if (flag4)
		{
			ParsedItemData parsedOrErrorData = whichFish.GetParsedOrErrorData();
			textureName = parsedOrErrorData.TextureName;
			sourceRect = parsedOrErrorData.GetSourceRect();
		}
		else
		{
			textureName = "LooseSprites\\Cursors";
			sourceRect = new Rectangle(228, 408, 16, 16);
		}
		float num11;
		if (farmer.FacingDirection == 1 || farmer.FacingDirection == 3)
		{
			float num6 = Vector2.Distance(bobber.Value, farmer.Position);
			float num7 = 0.001f;
			float num8 = 128f - (farmer.Position.Y - bobber.Y + 10f);
			double a = 1.1423973285781066;
			float num9 = (float)((double)(num6 * num7) * Math.Tan(a) / Math.Sqrt((double)(2f * num6 * num7) * Math.Tan(a) - (double)(2f * num7 * num8)));
			if (float.IsNaN(num9))
			{
				num9 = 0.6f;
			}
			float num10 = (float)((double)num9 * (1.0 / Math.Tan(a)));
			num11 = num6 / num10;
			animations.Add(new TemporaryAnimatedSprite(textureName, sourceRect, num11, 1, 0, bobber.Value, flicker: false, flipped: false, bobber.Y / 10000f, 0f, Color.White, 4f, 0f, 0f, 0f)
			{
				motion = new Vector2((float)((farmer.FacingDirection != 3) ? 1 : (-1)) * (0f - num10), 0f - num9),
				acceleration = new Vector2(0f, num7),
				timeBasedMotion = true,
				endFunction = delegate
				{
					playerCaughtFishEndFunction(isBossFish);
				},
				endSound = "tinyWhip"
			});
			if (numberOfFishCaught > 1)
			{
				for (int num12 = 1; num12 < numberOfFishCaught; num12++)
				{
					num6 = Vector2.Distance(bobber.Value, farmer.Position);
					num7 = 0.0008f - (float)num12 * 0.0001f;
					num8 = 128f - (farmer.Position.Y - bobber.Y + 10f);
					a = 1.1423973285781066;
					num9 = (float)((double)(num6 * num7) * Math.Tan(a) / Math.Sqrt((double)(2f * num6 * num7) * Math.Tan(a) - (double)(2f * num7 * num8)));
					if (float.IsNaN(num9))
					{
						num9 = 0.6f;
					}
					num10 = (float)((double)num9 * (1.0 / Math.Tan(a)));
					num11 = num6 / num10;
					animations.Add(new TemporaryAnimatedSprite(textureName, sourceRect, num11, 1, 0, bobber.Value, flicker: false, flipped: false, bobber.Y / 10000f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2((float)((farmer.FacingDirection != 3) ? 1 : (-1)) * (0f - num10), 0f - num9),
						acceleration = new Vector2(0f, num7),
						timeBasedMotion = true,
						endSound = "fishSlap",
						Parent = farmer.currentLocation,
						delayBeforeAnimationStart = (num12 - 1) * 100
					});
				}
			}
		}
		else
		{
			int y = farmer.StandingPixel.Y;
			float num13 = bobber.Y - (float)(y - 64);
			float num14 = Math.Abs(num13 + 256f + 32f);
			if (farmer.FacingDirection == 0)
			{
				num14 += 96f;
			}
			float num15 = 0.003f;
			float num16 = (float)Math.Sqrt(2f * num15 * num14);
			num11 = (float)(Math.Sqrt(2f * (num14 - num13) / num15) + (double)(num16 / num15));
			float x = 0f;
			if (num11 != 0f)
			{
				x = (farmer.Position.X - bobber.X) / num11;
			}
			animations.Add(new TemporaryAnimatedSprite(textureName, sourceRect, num11, 1, 0, bobber.Value, flicker: false, flipped: false, bobber.Y / 10000f, 0f, Color.White, 4f, 0f, 0f, 0f)
			{
				motion = new Vector2(x, 0f - num16),
				acceleration = new Vector2(0f, num15),
				timeBasedMotion = true,
				endFunction = delegate
				{
					playerCaughtFishEndFunction(isBossFish);
				},
				endSound = "tinyWhip"
			});
			if (numberOfFishCaught > 1)
			{
				for (int num17 = 1; num17 < numberOfFishCaught; num17++)
				{
					num13 = bobber.Y - (float)(y - 64);
					num14 = Math.Abs(num13 + 256f + 32f);
					if (farmer.FacingDirection == 0)
					{
						num14 += 96f;
					}
					num15 = 0.004f - (float)num17 * 0.0005f;
					num16 = (float)Math.Sqrt(2f * num15 * num14);
					num11 = (float)(Math.Sqrt(2f * (num14 - num13) / num15) + (double)(num16 / num15));
					x = 0f;
					if (num11 != 0f)
					{
						x = (farmer.Position.X - bobber.X) / num11;
					}
					animations.Add(new TemporaryAnimatedSprite(textureName, sourceRect, num11, 1, 0, new Vector2(bobber.X, bobber.Y), flicker: false, flipped: false, bobber.Y / 10000f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(x, 0f - num16),
						acceleration = new Vector2(0f, num15),
						timeBasedMotion = true,
						endSound = "fishSlap",
						Parent = farmer.currentLocation,
						delayBeforeAnimationStart = (num17 - 1) * 100
					});
				}
			}
		}
		if (PlayUseSounds && farmer.IsLocalPlayer)
		{
			farmer.currentLocation.playSound("pullItemFromWater", value);
			farmer.currentLocation.playSound("dwop", value);
		}
		castedButBobberStillInAir = false;
		pullingOutOfWater = true;
		isFishing = false;
		isReeling = false;
		farmer.FarmerSprite.PauseForSingleAnimation = false;
		switch (farmer.FacingDirection)
		{
		case 0:
			farmer.FarmerSprite.animateBackwardsOnce(299, num11);
			break;
		case 1:
			farmer.FarmerSprite.animateBackwardsOnce(300, num11);
			break;
		case 2:
			farmer.FarmerSprite.animateBackwardsOnce(301, num11);
			break;
		case 3:
			farmer.FarmerSprite.animateBackwardsOnce(302, num11);
			break;
		}
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		Farmer farmer = lastUser;
		float num = 4f;
		if (!bobber.Equals(Vector2.Zero) && isFishing)
		{
			Vector2 value = bobber.Value;
			if (bobberTimeAccumulator > timePerBobberBob)
			{
				if ((!isNibbling && !isReeling) || Game1.random.NextDouble() < 0.05)
				{
					if (PlayUseSounds)
					{
						farmer.playNearbySoundLocal("waterSlosh");
					}
					farmer.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 150f, 8, 0, new Vector2(bobber.X - 32f, bobber.Y - 16f), flicker: false, Game1.random.NextBool(), 0.001f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f));
				}
				timePerBobberBob = ((bobberBob == 0) ? Game1.random.Next(1500, 3500) : Game1.random.Next(350, 750));
				bobberTimeAccumulator = 0f;
				if (isNibbling || isReeling)
				{
					timePerBobberBob = Game1.random.Next(25, 75);
					value.X += Game1.random.Next(-5, 5);
					value.Y += Game1.random.Next(-5, 5);
					if (!isReeling)
					{
						num += (float)Game1.random.Next(-20, 20) / 100f;
					}
				}
				else if (PlayUseSounds && Game1.random.NextDouble() < 0.1)
				{
					farmer.playNearbySoundLocal("bob");
				}
			}
			float layerDepth = value.Y / 10000f;
			Rectangle sourceRectForStandardTileSheet = Game1.getSourceRectForStandardTileSheet(Game1.bobbersTexture, getBobberStyle(getLastFarmerToUse()), 16, 32);
			sourceRectForStandardTileSheet.Height = 16;
			sourceRectForStandardTileSheet.Y += 16;
			b.Draw(Game1.bobbersTexture, Game1.GlobalToLocal(Game1.viewport, value), sourceRectForStandardTileSheet, Color.White, 0f, new Vector2(8f, 8f), num, (getLastFarmerToUse().FacingDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
			sourceRectForStandardTileSheet = new Rectangle(sourceRectForStandardTileSheet.X, sourceRectForStandardTileSheet.Y + 8, sourceRectForStandardTileSheet.Width, sourceRectForStandardTileSheet.Height - 8);
		}
		else if ((isTimingCast || castingChosenCountdown > 0f) && farmer.IsLocalPlayer)
		{
			int num2 = (int)((0f - Math.Abs(castingChosenCountdown / 2f - castingChosenCountdown)) / 50f);
			float num3 = ((castingChosenCountdown > 0f && castingChosenCountdown < 100f) ? (castingChosenCountdown / 100f) : 1f);
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, getLastFarmerToUse().Position + new Vector2(-48f, -160 + num2)), new Rectangle(193, 1868, 47, 12), Color.White * num3, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.885f);
			b.Draw(Game1.staminaRect, new Rectangle((int)Game1.GlobalToLocal(Game1.viewport, getLastFarmerToUse().Position).X - 32 - 4, (int)Game1.GlobalToLocal(Game1.viewport, getLastFarmerToUse().Position).Y + num2 - 128 - 32 + 12, (int)(164f * castingPower), 25), Game1.staminaRect.Bounds, Utility.getRedToGreenLerpColor(castingPower) * num3, 0f, Vector2.Zero, SpriteEffects.None, 0.887f);
		}
		for (int num4 = animations.Count - 1; num4 >= 0; num4--)
		{
			animations[num4].draw(b);
		}
		if (sparklingText != null && !fishCaught)
		{
			sparklingText.draw(b, Game1.GlobalToLocal(Game1.viewport, getLastFarmerToUse().Position + new Vector2(-24f, -192f)));
		}
		else if (sparklingText != null && fishCaught)
		{
			sparklingText.draw(b, Game1.GlobalToLocal(Game1.viewport, getLastFarmerToUse().Position + new Vector2(-64f, -352f)));
		}
		if (!bobber.Value.Equals(Vector2.Zero) && (isFishing || pullingOutOfWater || castedButBobberStillInAir) && farmer.FarmerSprite.CurrentFrame != 57 && (farmer.FacingDirection != 0 || !pullingOutOfWater || whichFish == null))
		{
			Vector2 vector = (isFishing ? bobber.Value : ((animations.Count > 0) ? (animations[0].position + new Vector2(0f, 4f * num)) : Vector2.Zero));
			if (whichFish != null)
			{
				vector += new Vector2(32f, 32f);
			}
			Vector2 vector2 = Vector2.Zero;
			if (castedButBobberStillInAir)
			{
				switch (farmer.FacingDirection)
				{
				case 2:
					vector2 = farmer.FarmerSprite.currentAnimationIndex switch
					{
						0 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(8f, farmer.armOffset.Y - 96f + 4f)), 
						1 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(22f, farmer.armOffset.Y - 96f + 4f)), 
						2 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(28f, farmer.armOffset.Y - 64f + 40f)), 
						3 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(28f, farmer.armOffset.Y - 8f)), 
						4 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(28f, farmer.armOffset.Y + 32f)), 
						5 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(28f, farmer.armOffset.Y + 32f)), 
						_ => Vector2.Zero, 
					};
					break;
				case 0:
					vector2 = farmer.FarmerSprite.currentAnimationIndex switch
					{
						0 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(22f, farmer.armOffset.Y - 96f + 4f)), 
						1 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(32f, farmer.armOffset.Y - 96f + 4f)), 
						2 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(36f, farmer.armOffset.Y - 64f + 40f)), 
						3 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(36f, farmer.armOffset.Y - 16f)), 
						4 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(36f, farmer.armOffset.Y - 32f)), 
						5 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(36f, farmer.armOffset.Y - 32f)), 
						_ => Vector2.Zero, 
					};
					break;
				case 1:
					vector2 = farmer.FarmerSprite.currentAnimationIndex switch
					{
						0 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-48f, farmer.armOffset.Y - 96f - 8f)), 
						1 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-16f, farmer.armOffset.Y - 96f - 20f)), 
						2 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(84f, farmer.armOffset.Y - 96f - 20f)), 
						3 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(112f, farmer.armOffset.Y - 32f - 20f)), 
						4 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(120f, farmer.armOffset.Y - 32f + 8f)), 
						5 => Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(120f, farmer.armOffset.Y - 32f + 8f)), 
						_ => Vector2.Zero, 
					};
					break;
				case 3:
					switch (farmer.FarmerSprite.currentAnimationIndex)
					{
					case 0:
						vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(112f, farmer.armOffset.Y - 96f - 8f));
						break;
					case 1:
						vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(80f, farmer.armOffset.Y - 96f - 20f));
						break;
					case 2:
						vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-20f, farmer.armOffset.Y - 96f - 20f));
						break;
					case 3:
						vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-48f, farmer.armOffset.Y - 32f - 20f));
						break;
					case 4:
						vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-56f, farmer.armOffset.Y - 32f + 8f));
						break;
					case 5:
						vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-56f, farmer.armOffset.Y - 32f + 8f));
						break;
					}
					break;
				default:
					vector2 = Vector2.Zero;
					break;
				}
			}
			else if (!isReeling)
			{
				vector2 = farmer.FacingDirection switch
				{
					0 => pullingOutOfWater ? Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(22f, farmer.armOffset.Y - 96f + 4f)) : Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(28f, farmer.armOffset.Y - 64f - 12f)), 
					2 => pullingOutOfWater ? Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(8f, farmer.armOffset.Y - 96f + 4f)) : Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(28f, farmer.armOffset.Y + 64f - 12f)), 
					1 => pullingOutOfWater ? Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-48f, farmer.armOffset.Y - 96f - 8f)) : Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(120f, farmer.armOffset.Y - 64f + 16f)), 
					3 => pullingOutOfWater ? Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(112f, farmer.armOffset.Y - 96f - 8f)) : Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-56f, farmer.armOffset.Y - 64f + 16f)), 
					_ => Vector2.Zero, 
				};
			}
			else if (farmer != null && farmer.IsLocalPlayer && Game1.didPlayerJustClickAtAll())
			{
				switch (farmer.FacingDirection)
				{
				case 0:
					vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(24f, farmer.armOffset.Y - 96f + 12f));
					break;
				case 3:
					vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(48f, farmer.armOffset.Y - 96f - 12f));
					break;
				case 2:
					vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(12f, farmer.armOffset.Y - 96f + 8f));
					break;
				case 1:
					vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(20f, farmer.armOffset.Y - 96f - 12f));
					break;
				}
			}
			else
			{
				switch (farmer.FacingDirection)
				{
				case 2:
					vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(12f, farmer.armOffset.Y - 96f + 4f));
					break;
				case 0:
					vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(25f, farmer.armOffset.Y - 96f + 4f));
					break;
				case 3:
					vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(36f, farmer.armOffset.Y - 96f - 8f));
					break;
				case 1:
					vector2 = Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(28f, farmer.armOffset.Y - 96f - 8f));
					break;
				}
			}
			Vector2 vector3 = Game1.GlobalToLocal(Game1.viewport, vector + new Vector2(0f, -2.5f * num + (float)((bobberBob == 1) ? 4 : 0)));
			if (isTimingCast || (isCasting && !farmer.IsLocalPlayer))
			{
				return;
			}
			if (isReeling)
			{
				Utility.drawLineWithScreenCoordinates((int)vector2.X, (int)vector2.Y, (int)vector3.X, (int)vector3.Y, b, getFishingLineColor() * 0.5f);
				return;
			}
			if (!isFishing)
			{
				vector3 += new Vector2(20f, 20f);
			}
			if (pullingOutOfWater && whichFish != null)
			{
				vector3 += new Vector2(-20f, -30f);
			}
			Vector2 p = vector2;
			Vector2 p2 = new Vector2(vector2.X + (vector3.X - vector2.X) / 3f, vector2.Y + (vector3.Y - vector2.Y) * 2f / 3f);
			Vector2 p3 = new Vector2(vector2.X + (vector3.X - vector2.X) * 2f / 3f, vector2.Y + (vector3.Y - vector2.Y) * (float)(isFishing ? 6 : 2) / 5f);
			Vector2 p4 = vector3;
			float layerDepth2 = ((vector.Y > (float)farmer.StandingPixel.Y) ? (vector.Y / 10000f) : ((float)farmer.StandingPixel.Y / 10000f)) + ((farmer.FacingDirection != 0) ? 0.005f : (-0.001f));
			for (float num5 = 0f; num5 < 1f; num5 += 0.025f)
			{
				Vector2 curvePoint = Utility.GetCurvePoint(num5, p, p2, p3, p4);
				Utility.drawLineWithScreenCoordinates((int)vector2.X, (int)vector2.Y, (int)curvePoint.X, (int)curvePoint.Y, b, getFishingLineColor() * 0.5f, layerDepth2);
				vector2 = curvePoint;
			}
		}
		else
		{
			if (!fishCaught)
			{
				return;
			}
			bool flag = whichFish.TypeIdentifier == "(O)";
			float num6 = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			int y = farmer.StandingPixel.Y;
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-120f, -288f + num6)), new Rectangle(31, 1870, 73, 49), Color.White * 0.8f, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)y / 10000f + 0.06f);
			if (flag)
			{
				ParsedItemData parsedOrErrorData = whichFish.GetParsedOrErrorData();
				Texture2D texture = parsedOrErrorData.GetTexture();
				Rectangle sourceRect = parsedOrErrorData.GetSourceRect();
				b.Draw(texture, Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-124f, -284f + num6) + new Vector2(44f, 68f)), sourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)y / 10000f + 0.0001f + 0.06f);
				if (numberOfFishCaught > 1)
				{
					Utility.drawTinyDigits(numberOfFishCaught, b, Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-120f, -284f + num6) + new Vector2(23f, 29f) * 4f), 3f, (float)y / 10000f + 0.0001f + 0.061f, Color.White);
				}
				b.Draw(texture, Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(0f, -56f)), sourceRect, Color.White, (fishSize == -1 || whichFish.QualifiedItemId == "(O)800" || whichFish.QualifiedItemId == "(O)798" || whichFish.QualifiedItemId == "(O)149" || whichFish.QualifiedItemId == "(O)151") ? 0f : ((float)Math.PI * 3f / 4f), new Vector2(8f, 8f), 3f, SpriteEffects.None, (float)y / 10000f + 0.002f + 0.06f);
				if (numberOfFishCaught > 1)
				{
					for (int i = 1; i < numberOfFishCaught; i++)
					{
						b.Draw(texture, Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-(12 * i), -56f)), sourceRect, Color.White, (fishSize == -1 || whichFish.QualifiedItemId == "(O)800" || whichFish.QualifiedItemId == "(O)798" || whichFish.QualifiedItemId == "(O)149" || whichFish.QualifiedItemId == "(O)151") ? 0f : ((i == 2) ? ((float)Math.PI) : ((float)Math.PI * 4f / 5f)), new Vector2(8f, 8f), 3f, SpriteEffects.None, (float)y / 10000f + 0.002f + 0.058f);
					}
				}
			}
			else
			{
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-124f, -284f + num6) + new Vector2(44f, 68f)), new Rectangle(228, 408, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)y / 10000f + 0.0001f + 0.06f);
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(0f, -56f)), new Rectangle(228, 408, 16, 16), Color.White, 0f, new Vector2(8f, 8f), 3f, SpriteEffects.None, (float)y / 10000f + 0.002f + 0.06f);
			}
			string text = (flag ? whichFish.GetParsedOrErrorData().DisplayName : "???");
			b.DrawString(Game1.smallFont, text, Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(26f - Game1.smallFont.MeasureString(text).X / 2f, -278f + num6)), bossFish ? new Color(126, 61, 237) : Game1.textColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, (float)y / 10000f + 0.002f + 0.06f);
			if (fishSize != -1)
			{
				b.DrawString(Game1.smallFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14082"), Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(20f, -214f + num6)), Game1.textColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, (float)y / 10000f + 0.002f + 0.06f);
				b.DrawString(Game1.smallFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14083", (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.en) ? Math.Round((double)fishSize * 2.54) : ((double)fishSize)), Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(85f - Game1.smallFont.MeasureString(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14083", (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.en) ? Math.Round((double)fishSize * 2.54) : ((double)fishSize))).X / 2f, -179f + num6)), recordSize ? (Color.Blue * Math.Min(1f, num6 / 8f + 1.5f)) : Game1.textColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, (float)y / 10000f + 0.002f + 0.06f);
			}
		}
	}

	/// <summary>Get the color of the water which the bobber is submerged in.</summary>
	public Color GetWaterColor()
	{
		if (lastWaterColor.HasValue)
		{
			return lastWaterColor.Value;
		}
		GameLocation gameLocation = lastUser?.currentLocation ?? Game1.currentLocation;
		Vector2 vector = calculateBobberTile();
		if (vector != Vector2.Zero)
		{
			foreach (Building building in gameLocation.buildings)
			{
				if (building.isTileFishable(vector))
				{
					lastWaterColor = building.GetWaterColor(vector);
					if (lastWaterColor.HasValue)
					{
						return lastWaterColor.Value;
					}
					break;
				}
			}
		}
		lastWaterColor = gameLocation.waterColor.Value;
		return lastWaterColor.Value;
	}

	public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
	{
		if (who.Stamina <= 1f && who.IsLocalPlayer)
		{
			if (!who.isEmoting)
			{
				who.doEmote(36);
			}
			who.CanMove = !Game1.eventUp;
			who.UsingTool = false;
			who.canReleaseTool = false;
			doneFishing(null);
			return true;
		}
		usedGamePadToCast = false;
		if (Game1.input.GetGamePadState().IsButtonDown(Buttons.X))
		{
			usedGamePadToCast = true;
		}
		bossFish = false;
		originalFacingDirection = who.FacingDirection;
		if (who.IsLocalPlayer || who.isFakeEventActor)
		{
			CastDirection = originalFacingDirection;
		}
		who.Halt();
		treasureCaught = false;
		showingTreasure = false;
		isFishing = false;
		hit = false;
		favBait = false;
		if (GetTackle().Count > 0)
		{
			bool flag = false;
			foreach (Object item in GetTackle())
			{
				if (item != null)
				{
					flag = true;
					break;
				}
			}
			hadBobber = flag;
		}
		isNibbling = false;
		lastUser = who;
		lastWaterColor = null;
		isTimingCast = true;
		_totalMotionBufferIndex = 0;
		for (int i = 0; i < _totalMotionBuffer.Length; i++)
		{
			_totalMotionBuffer[i] = Vector2.Zero;
		}
		_totalMotion.Value = Vector2.Zero;
		_lastAppliedMotion = Vector2.Zero;
		who.UsingTool = true;
		whichFish = null;
		recastTimerMs = 0;
		who.canMove = false;
		fishCaught = false;
		doneWithAnimation = false;
		who.canReleaseTool = false;
		hasDoneFucntionYet = false;
		isReeling = false;
		pullingOutOfWater = false;
		castingPower = 0f;
		castingChosenCountdown = 0f;
		animations.Clear();
		sparklingText = null;
		setTimingCastAnimation(who);
		return true;
	}

	public void setTimingCastAnimation(Farmer who)
	{
		if (who.CurrentTool != null)
		{
			switch (who.FacingDirection)
			{
			case 0:
				who.FarmerSprite.setCurrentFrame(295);
				who.CurrentTool.Update(0, 0, who);
				break;
			case 1:
				who.FarmerSprite.setCurrentFrame(296);
				who.CurrentTool.Update(1, 0, who);
				break;
			case 2:
				who.FarmerSprite.setCurrentFrame(297);
				who.CurrentTool.Update(2, 0, who);
				break;
			case 3:
				who.FarmerSprite.setCurrentFrame(298);
				who.CurrentTool.Update(3, 0, who);
				break;
			}
		}
	}

	public void doneFishing(Farmer who, bool consumeBaitAndTackle = false)
	{
		doneFishingEvent.Fire(consumeBaitAndTackle);
	}

	private void doDoneFishing(bool consumeBaitAndTackle)
	{
		Farmer farmer = lastUser;
		if (consumeBaitAndTackle && farmer != null && farmer.IsLocalPlayer)
		{
			float num = 1f;
			if (hasEnchantmentOfType<PreservingEnchantment>())
			{
				num = 0.5f;
			}
			Object bait = GetBait();
			if (bait != null && Game1.random.NextDouble() < (double)num && bait.ConsumeStack(1) == null)
			{
				attachments[0] = null;
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14085"));
			}
			int num2 = 1;
			foreach (Object item in GetTackle())
			{
				if (item != null && !lastCatchWasJunk && Game1.random.NextDouble() < (double)num)
				{
					if (item.QualifiedItemId == "(O)789")
					{
						break;
					}
					item.uses.Value++;
					if (item.uses.Value >= maxTackleUses)
					{
						attachments[num2] = null;
						Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14086"));
					}
				}
				num2++;
			}
		}
		if (farmer != null && farmer.IsLocalPlayer)
		{
			bobber.Set(Vector2.Zero);
		}
		isNibbling = false;
		fishCaught = false;
		isFishing = false;
		isReeling = false;
		isCasting = false;
		isTimingCast = false;
		treasureCaught = false;
		showingTreasure = false;
		doneWithAnimation = false;
		pullingOutOfWater = false;
		fromFishPond = false;
		numberOfFishCaught = 1;
		fishingBiteAccumulator = 0f;
		fishingNibbleAccumulator = 0f;
		timeUntilFishingBite = -1f;
		timeUntilFishingNibbleDone = -1f;
		bobberTimeAccumulator = 0f;
		if (chargeSound != null && chargeSound.IsPlaying && farmer.IsLocalPlayer)
		{
			chargeSound.Stop(AudioStopOptions.Immediate);
			chargeSound = null;
		}
		if (reelSound != null && reelSound.IsPlaying)
		{
			reelSound.Stop(AudioStopOptions.Immediate);
			reelSound = null;
		}
		if (farmer != null)
		{
			farmer.UsingTool = false;
			farmer.CanMove = true;
			farmer.completelyStopAnimatingOrDoingAction();
			if (farmer == Game1.player)
			{
				farmer.faceDirection(originalFacingDirection);
			}
		}
	}

	public static void doneWithCastingAnimation(Farmer who)
	{
		if (who.CurrentTool is FishingRod fishingRod)
		{
			fishingRod.doneWithAnimation = true;
			if (fishingRod.hasDoneFucntionYet)
			{
				who.canReleaseTool = true;
				who.UsingTool = false;
				who.canMove = true;
				Farmer.canMoveNow(who);
			}
		}
	}

	public void castingEndFunction(Farmer who)
	{
		lastWaterColor = null;
		castedButBobberStillInAir = false;
		if (who != null)
		{
			float stamina = who.Stamina;
			DoFunction(who.currentLocation, (int)bobber.X, (int)bobber.Y, 1, who);
			who.lastClick = Vector2.Zero;
			reelSound?.Stop(AudioStopOptions.Immediate);
			reelSound = null;
			if (who.Stamina <= 0f && stamina > 0f)
			{
				who.doEmote(36);
			}
			if (!isFishing && doneWithAnimation)
			{
				castingEndEnableMovement();
			}
		}
	}

	private void castingEndEnableMovement()
	{
		castingEndEnableMovementEvent.Fire();
	}

	private void doCastingEndEnableMovement()
	{
		Farmer.canMoveNow(lastUser);
	}

	public override void tickUpdate(GameTime time, Farmer who)
	{
		lastUser = who;
		beginReelingEvent.Poll();
		putAwayEvent.Poll();
		startCastingEvent.Poll();
		pullFishFromWaterEvent.Poll();
		doneFishingEvent.Poll();
		castingEndEnableMovementEvent.Poll();
		if (recastTimerMs > 0 && who.IsLocalPlayer && who.freezePause <= 0)
		{
			if (Game1.input.GetMouseState().LeftButton == ButtonState.Pressed || Game1.didPlayerJustClickAtAll() || Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.useToolButton))
			{
				recastTimerMs -= time.ElapsedGameTime.Milliseconds;
				if (recastTimerMs <= 0)
				{
					recastTimerMs = 0;
					if (Game1.activeClickableMenu == null)
					{
						who.BeginUsingTool();
					}
				}
			}
			else
			{
				recastTimerMs = 0;
			}
		}
		if (isFishing && !Game1.shouldTimePass() && Game1.activeClickableMenu != null && !(Game1.activeClickableMenu is BobberBar))
		{
			return;
		}
		if (who.CurrentTool != null && who.CurrentTool.Equals(this) && who.UsingTool)
		{
			who.CanMove = false;
		}
		else if (Game1.currentMinigame == null && (!(who.CurrentTool is FishingRod) || !who.UsingTool))
		{
			if (chargeSound != null && chargeSound.IsPlaying && who.IsLocalPlayer)
			{
				chargeSound.Stop(AudioStopOptions.Immediate);
				chargeSound = null;
			}
			return;
		}
		animations.RemoveWhere((TemporaryAnimatedSprite animation) => animation.update(time));
		if (sparklingText != null && sparklingText.update(time))
		{
			sparklingText = null;
		}
		if (castingChosenCountdown > 0f)
		{
			castingChosenCountdown -= time.ElapsedGameTime.Milliseconds;
			if (castingChosenCountdown <= 0f && who.CurrentTool != null)
			{
				switch (who.FacingDirection)
				{
				case 0:
					who.FarmerSprite.animateOnce(295, 1f, 1);
					who.CurrentTool.Update(0, 0, who);
					break;
				case 1:
					who.FarmerSprite.animateOnce(296, 1f, 1);
					who.CurrentTool.Update(1, 0, who);
					break;
				case 2:
					who.FarmerSprite.animateOnce(297, 1f, 1);
					who.CurrentTool.Update(2, 0, who);
					break;
				case 3:
					who.FarmerSprite.animateOnce(298, 1f, 1);
					who.CurrentTool.Update(3, 0, who);
					break;
				}
				if (who.FacingDirection == 1 || who.FacingDirection == 3)
				{
					float num = Math.Max(128f, castingPower * (float)(getAddedDistance(who) + 4) * 64f);
					num -= 8f;
					float num2 = 0.005f;
					float num3 = (float)((double)num * Math.Sqrt(num2 / (2f * (num + 96f))));
					float animationInterval = 2f * (num3 / num2) + (float)((Math.Sqrt(num3 * num3 + 2f * num2 * 96f) - (double)num3) / (double)num2);
					Point standingPixel = who.StandingPixel;
					if (who.IsLocalPlayer)
					{
						bobber.Set(new Vector2((float)standingPixel.X + (float)((who.FacingDirection != 3) ? 1 : (-1)) * num, standingPixel.Y));
					}
					Rectangle sourceRectForStandardTileSheet = Game1.getSourceRectForStandardTileSheet(Game1.bobbersTexture, getBobberStyle(who), 16, 32);
					sourceRectForStandardTileSheet.Height = 16;
					animations.Add(new TemporaryAnimatedSprite("TileSheets\\bobbers", sourceRectForStandardTileSheet, animationInterval, 1, 0, who.Position + new Vector2(0f, -96f), flicker: false, flipped: false, (float)standingPixel.Y / 10000f, 0f, Color.White, 4f, 0f, 0f, (float)Game1.random.Next(-20, 20) / 100f)
					{
						motion = new Vector2((float)((who.FacingDirection != 3) ? 1 : (-1)) * num3, 0f - num3),
						acceleration = new Vector2(0f, num2),
						endFunction = delegate
						{
							castingEndFunction(who);
						},
						timeBasedMotion = true,
						flipped = (who.FacingDirection == 1 && flipCurrentBobberWhenFacingRight())
					});
				}
				else
				{
					float num4 = 0f - Math.Max(128f, castingPower * (float)(getAddedDistance(who) + 3) * 64f);
					float num5 = Math.Abs(num4 - 64f);
					if (who.FacingDirection == 0)
					{
						num4 = 0f - num4;
						num5 += 64f;
					}
					float num6 = 0.005f;
					float num7 = (float)Math.Sqrt(2f * num6 * num5);
					float num8 = (float)(Math.Sqrt(2f * (num5 - num4) / num6) + (double)(num7 / num6));
					num8 *= 1.05f;
					if (who.FacingDirection == 0)
					{
						num8 *= 1.05f;
					}
					if (who.IsLocalPlayer)
					{
						Point standingPixel2 = who.StandingPixel;
						bobber.Set(new Vector2(standingPixel2.X, (float)standingPixel2.Y - num4));
					}
					Rectangle sourceRectForStandardTileSheet2 = Game1.getSourceRectForStandardTileSheet(Game1.bobbersTexture, getBobberStyle(who), 16, 32);
					sourceRectForStandardTileSheet2.Height = 16;
					animations.Add(new TemporaryAnimatedSprite("TileSheets\\bobbers", sourceRectForStandardTileSheet2, num8, 1, 0, who.Position + new Vector2(0f, -96f), flicker: false, flipped: false, bobber.Y / 10000f, 0f, Color.White, 4f, 0f, 0f, (float)Game1.random.Next(-20, 20) / 100f)
					{
						alphaFade = 0.0001f,
						motion = new Vector2(0f, 0f - num7),
						acceleration = new Vector2(0f, num6),
						endFunction = delegate
						{
							castingEndFunction(who);
						},
						timeBasedMotion = true
					});
				}
				_hasPlayerAdjustedBobber = false;
				castedButBobberStillInAir = true;
				isCasting = false;
				if (PlayUseSounds && who.IsLocalPlayer)
				{
					who.playNearbySoundAll("cast");
					Game1.playSound("slowReel", 1600, out reelSound);
				}
			}
		}
		else if (!isTimingCast && castingChosenCountdown <= 0f)
		{
			who.jitterStrength = 0f;
		}
		if (isTimingCast)
		{
			castingPower = Math.Max(0f, Math.Min(1f, castingPower + castingTimerSpeed * (float)time.ElapsedGameTime.Milliseconds));
			if (PlayUseSounds && who.IsLocalPlayer)
			{
				if (chargeSound == null || !chargeSound.IsPlaying)
				{
					Game1.playSound("SinWave", out chargeSound);
				}
				Game1.sounds.SetPitch(chargeSound, 2400f * castingPower);
			}
			if (castingPower == 1f || castingPower == 0f)
			{
				castingTimerSpeed = 0f - castingTimerSpeed;
			}
			who.armOffset.Y = 2f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			who.jitterStrength = Math.Max(0f, castingPower - 0.5f);
			if (who.IsLocalPlayer && ((!usedGamePadToCast && Game1.input.GetMouseState().LeftButton == ButtonState.Released) || (usedGamePadToCast && Game1.options.gamepadControls && Game1.input.GetGamePadState().IsButtonUp(Buttons.X))) && Game1.areAllOfTheseKeysUp(Game1.GetKeyboardState(), Game1.options.useToolButton))
			{
				startCasting();
			}
		}
		else if (isReeling)
		{
			if (who.IsLocalPlayer && Game1.didPlayerJustClickAtAll())
			{
				if (Game1.isAnyGamePadButtonBeingPressed())
				{
					Game1.lastCursorMotionWasMouse = false;
				}
				switch (who.FacingDirection)
				{
				case 0:
					who.FarmerSprite.setCurrentSingleFrame(76, 32000);
					break;
				case 1:
					who.FarmerSprite.setCurrentSingleFrame(72, 100);
					break;
				case 2:
					who.FarmerSprite.setCurrentSingleFrame(75, 32000);
					break;
				case 3:
					who.FarmerSprite.setCurrentSingleFrame(72, 100, secondaryArm: false, flip: true);
					break;
				}
				who.armOffset.Y = (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
				who.jitterStrength = 1f;
			}
			else
			{
				switch (who.FacingDirection)
				{
				case 0:
					who.FarmerSprite.setCurrentSingleFrame(36, 32000);
					break;
				case 1:
					who.FarmerSprite.setCurrentSingleFrame(48, 100);
					break;
				case 2:
					who.FarmerSprite.setCurrentSingleFrame(66, 32000);
					break;
				case 3:
					who.FarmerSprite.setCurrentSingleFrame(48, 100, secondaryArm: false, flip: true);
					break;
				}
				who.stopJittering();
			}
			who.armOffset = new Vector2((float)Game1.random.Next(-10, 11) / 10f, (float)Game1.random.Next(-10, 11) / 10f);
			bobberTimeAccumulator += time.ElapsedGameTime.Milliseconds;
		}
		else if (isFishing)
		{
			if (who.IsLocalPlayer)
			{
				bobber.Y += (float)(0.11999999731779099 * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0));
			}
			who.canReleaseTool = true;
			bobberTimeAccumulator += time.ElapsedGameTime.Milliseconds;
			switch (who.FacingDirection)
			{
			case 0:
				who.FarmerSprite.setCurrentFrame(44);
				break;
			case 1:
				who.FarmerSprite.setCurrentFrame(89);
				break;
			case 2:
				who.FarmerSprite.setCurrentFrame(70);
				break;
			case 3:
				who.FarmerSprite.setCurrentFrame(89, 0, 10, 1, flip: true, secondaryArm: false);
				break;
			}
			who.armOffset.Y = (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2) + (float)((who.FacingDirection == 1 || who.FacingDirection == 3) ? 1 : (-1));
			if (!who.IsLocalPlayer)
			{
				return;
			}
			if (timeUntilFishingBite != -1f)
			{
				fishingBiteAccumulator += time.ElapsedGameTime.Milliseconds;
				if (fishingBiteAccumulator > timeUntilFishingBite)
				{
					fishingBiteAccumulator = 0f;
					timeUntilFishingBite = -1f;
					isNibbling = true;
					if (hasEnchantmentOfType<AutoHookEnchantment>())
					{
						timePerBobberBob = 1f;
						timeUntilFishingNibbleDone = maxTimeToNibble;
						DoFunction(who.currentLocation, (int)bobber.X, (int)bobber.Y, 1, who);
						Rumble.rumble(0.95f, 200f);
						return;
					}
					who.PlayFishBiteChime();
					Rumble.rumble(0.75f, 250f);
					timeUntilFishingNibbleDone = maxTimeToNibble;
					Point standingPixel3 = who.StandingPixel;
					Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(395, 497, 3, 8), new Vector2(standingPixel3.X - Game1.viewport.X, standingPixel3.Y - 128 - 8 - Game1.viewport.Y), flipped: false, 0.02f, Color.White)
					{
						scale = 5f,
						scaleChange = -0.01f,
						motion = new Vector2(0f, -0.5f),
						shakeIntensityChange = -0.005f,
						shakeIntensity = 1f
					});
					timePerBobberBob = 1f;
				}
			}
			if (timeUntilFishingNibbleDone != -1f && !hit)
			{
				fishingNibbleAccumulator += time.ElapsedGameTime.Milliseconds;
				if (fishingNibbleAccumulator > timeUntilFishingNibbleDone)
				{
					fishingNibbleAccumulator = 0f;
					timeUntilFishingNibbleDone = -1f;
					isNibbling = false;
					timeUntilFishingBite = calculateTimeUntilFishingBite(calculateBobberTile(), isFirstCast: false, who);
				}
			}
		}
		else if (who.UsingTool && castedButBobberStillInAir)
		{
			Vector2 zero = Vector2.Zero;
			if ((Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveDownButton) || (Game1.options.gamepadControls && (Game1.oldPadState.IsButtonDown(Buttons.DPadDown) || Game1.input.GetGamePadState().ThumbSticks.Left.Y < 0f))) && who.FacingDirection != 2 && who.FacingDirection != 0)
			{
				zero.Y += 4f;
				_hasPlayerAdjustedBobber = true;
			}
			if ((Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveRightButton) || (Game1.options.gamepadControls && (Game1.oldPadState.IsButtonDown(Buttons.DPadRight) || Game1.input.GetGamePadState().ThumbSticks.Left.X > 0f))) && who.FacingDirection != 1 && who.FacingDirection != 3)
			{
				zero.X += 2f;
				_hasPlayerAdjustedBobber = true;
			}
			if ((Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveUpButton) || (Game1.options.gamepadControls && (Game1.oldPadState.IsButtonDown(Buttons.DPadUp) || Game1.input.GetGamePadState().ThumbSticks.Left.Y > 0f))) && who.FacingDirection != 0 && who.FacingDirection != 2)
			{
				zero.Y -= 4f;
				_hasPlayerAdjustedBobber = true;
			}
			if ((Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveLeftButton) || (Game1.options.gamepadControls && (Game1.oldPadState.IsButtonDown(Buttons.DPadLeft) || Game1.input.GetGamePadState().ThumbSticks.Left.X < 0f))) && who.FacingDirection != 3 && who.FacingDirection != 1)
			{
				zero.X -= 2f;
				_hasPlayerAdjustedBobber = true;
			}
			if (!_hasPlayerAdjustedBobber)
			{
				Vector2 vector = calculateBobberTile();
				if (!who.currentLocation.isTileFishable((int)vector.X, (int)vector.Y))
				{
					if (who.FacingDirection == 3 || who.FacingDirection == 1)
					{
						int num9 = 1;
						if (vector.Y % 1f < 0.5f)
						{
							num9 = -1;
						}
						if (who.currentLocation.isTileFishable((int)vector.X, (int)vector.Y + num9))
						{
							zero.Y += (float)num9 * 4f;
						}
						else if (who.currentLocation.isTileFishable((int)vector.X, (int)vector.Y - num9))
						{
							zero.Y -= (float)num9 * 4f;
						}
					}
					if (who.FacingDirection == 0 || who.FacingDirection == 2)
					{
						int num10 = 1;
						if (vector.X % 1f < 0.5f)
						{
							num10 = -1;
						}
						if (who.currentLocation.isTileFishable((int)vector.X + num10, (int)vector.Y))
						{
							zero.X += (float)num10 * 4f;
						}
						else if (who.currentLocation.isTileFishable((int)vector.X - num10, (int)vector.Y))
						{
							zero.X -= (float)num10 * 4f;
						}
					}
				}
			}
			if (who.IsLocalPlayer)
			{
				bobber.Set(bobber.Value + zero);
				_totalMotion.Set(_totalMotion.Value + zero);
			}
			if (animations.Count <= 0)
			{
				return;
			}
			Vector2 vector2 = Vector2.Zero;
			if (who.IsLocalPlayer)
			{
				vector2 = _totalMotion.Value;
			}
			else
			{
				_totalMotionBuffer[_totalMotionBufferIndex] = _totalMotion.Value;
				for (int num11 = 0; num11 < _totalMotionBuffer.Length; num11++)
				{
					vector2 += _totalMotionBuffer[num11];
				}
				vector2 /= (float)_totalMotionBuffer.Length;
				_totalMotionBufferIndex = (_totalMotionBufferIndex + 1) % _totalMotionBuffer.Length;
			}
			animations[0].position -= _lastAppliedMotion;
			_lastAppliedMotion = vector2;
			animations[0].position += vector2;
		}
		else if (showingTreasure)
		{
			who.FarmerSprite.setCurrentSingleFrame(0, 32000);
		}
		else if (fishCaught)
		{
			if (!Game1.isFestival())
			{
				who.faceDirection(2);
				who.FarmerSprite.setCurrentFrame(84);
			}
			if (Game1.random.NextDouble() < 0.025)
			{
				who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(653, 858, 1, 1), 9999f, 1, 1, who.Position + new Vector2(Game1.random.Next(-3, 2) * 4, -32f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.002f, 0.04f, Color.LightBlue, 5f, 0f, 0f, 0f)
				{
					acceleration = new Vector2(0f, 0.25f)
				});
			}
			if (who.IsLocalPlayer && (Game1.input.GetMouseState().LeftButton == ButtonState.Pressed || Game1.didPlayerJustClickAtAll() || Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.useToolButton)))
			{
				doneHoldingFish(who);
			}
		}
		else if (who.UsingTool && castedButBobberStillInAir && doneWithAnimation)
		{
			switch (who.FacingDirection)
			{
			case 0:
				who.FarmerSprite.setCurrentFrame(39);
				break;
			case 1:
				who.FarmerSprite.setCurrentFrame(89);
				break;
			case 2:
				who.FarmerSprite.setCurrentFrame(28);
				break;
			case 3:
				who.FarmerSprite.setCurrentFrame(89, 0, 10, 1, flip: true, secondaryArm: false);
				break;
			}
			who.armOffset.Y = (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
		}
		else if (!castedButBobberStillInAir && whichFish != null && animations.Count > 0 && animations[0].timer > 500f && !Game1.eventUp)
		{
			who.faceDirection(2);
			who.FarmerSprite.setCurrentFrame(57);
		}
	}

	public void doneHoldingFish(Farmer who, bool endOfNight = false)
	{
		if (PlayUseSounds)
		{
			who.playNearbySoundLocal("coin");
		}
		if (!fromFishPond && Game1.IsSummer && whichFish.QualifiedItemId == "(O)138" && Game1.dayOfMonth >= 20 && Game1.dayOfMonth <= 21 && Game1.random.NextDouble() < 0.33 * (double)numberOfFishCaught)
		{
			gotTroutDerbyTag = true;
		}
		if (!treasureCaught && !gotTroutDerbyTag)
		{
			recastTimerMs = 200;
			Item item = CreateFish();
			bool flag = item.HasTypeObject();
			if ((item.Category == -4 || item.HasContextTag("counts_as_fish_catch")) && !fromFishPond)
			{
				Game1.player.stats.Increment("PreciseFishCaught", Math.Max(1, numberOfFishCaught));
			}
			if (item.QualifiedItemId == "(O)79" || item.QualifiedItemId == "(O)842")
			{
				item = who.currentLocation.tryToCreateUnseenSecretNote(who);
				if (item == null)
				{
					return;
				}
			}
			bool flag2 = fromFishPond;
			who.completelyStopAnimatingOrDoingAction();
			doneFishing(who, !flag2);
			if (!Game1.isFestival() && !flag2 && flag && who.team.specialOrders != null)
			{
				foreach (SpecialOrder specialOrder in who.team.specialOrders)
				{
					specialOrder.onFishCaught?.Invoke(who, item);
				}
			}
			if (!Game1.isFestival() && !who.addItemToInventoryBool(item))
			{
				if (endOfNight)
				{
					Game1.createItemDebris(item, who.getStandingPosition(), -1, who.currentLocation);
					return;
				}
				Game1.activeClickableMenu = new ItemGrabMenu(new List<Item> { item }, this).setEssential(essential: true);
			}
			return;
		}
		fishCaught = false;
		showingTreasure = true;
		who.UsingTool = true;
		Item item2 = CreateFish();
		if ((item2.Category == -4 || item2.HasContextTag("counts_as_fish_catch")) && !fromFishPond)
		{
			Game1.player.stats.Increment("PreciseFishCaught", Math.Max(1, numberOfFishCaught));
		}
		if (who.team.specialOrders != null)
		{
			foreach (SpecialOrder specialOrder2 in who.team.specialOrders)
			{
				specialOrder2.onFishCaught?.Invoke(who, item2);
			}
		}
		bool flag3 = who.addItemToInventoryBool(item2);
		if (!endOfNight)
		{
			if (treasureCaught)
			{
				animations.Add(new TemporaryAnimatedSprite(goldenTreasure ? "LooseSprites\\Cursors_1_6" : "LooseSprites\\Cursors", goldenTreasure ? new Rectangle(256, 75, 32, 32) : new Rectangle(64, 1920, 32, 32), 500f, 1, 0, who.Position + new Vector2(-32f, -160f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					motion = new Vector2(0f, -0.128f),
					timeBasedMotion = true,
					endFunction = openChestEndFunction,
					extraInfoForEndBehavior = ((!flag3) ? item2.Stack : 0),
					alpha = 0f,
					alphaFade = -0.002f
				});
			}
			else if (gotTroutDerbyTag)
			{
				animations.Add(new TemporaryAnimatedSprite("TileSheets\\Objects_2", new Rectangle(80, 16, 16, 16), 500f, 1, 0, who.Position + new Vector2(-8f, -128f), flicker: false, flipped: false, (float)who.StandingPixel.Y / 10000f + 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					motion = new Vector2(0f, -0.128f),
					timeBasedMotion = true,
					endFunction = openChestEndFunction,
					extraInfoForEndBehavior = ((!flag3) ? item2.Stack : 0),
					alpha = 0f,
					alphaFade = -0.002f,
					id = 1074
				});
			}
		}
		else if (!flag3)
		{
			Game1.createItemDebris(item2, who.getStandingPosition(), -1, who.currentLocation);
		}
	}

	/// <summary>Create a fish instance from the raw fields like <see cref="F:StardewValley.Tools.FishingRod.whichFish" />.</summary>
	private Item CreateFish()
	{
		Item item = whichFish.CreateItemOrErrorItem(1, fishQuality);
		item.SetFlagOnPickup = setFlagOnCatch;
		if (item.HasTypeObject())
		{
			if (item.QualifiedItemId == GameLocation.CAROLINES_NECKLACE_ITEM_QID)
			{
				if (item is Object obj)
				{
					obj.questItem.Value = true;
				}
			}
			else if (numberOfFishCaught > 1 && item.QualifiedItemId != "(O)79" && item.QualifiedItemId != "(O)842")
			{
				item.Stack = numberOfFishCaught;
			}
		}
		return item;
	}

	private void startCasting()
	{
		startCastingEvent.Fire();
	}

	public void beginReeling()
	{
		isReeling = true;
	}

	private void doStartCasting()
	{
		Farmer farmer = lastUser;
		randomBobberStyle = -1;
		if (chargeSound != null && farmer.IsLocalPlayer)
		{
			chargeSound.Stop(AudioStopOptions.Immediate);
			chargeSound = null;
		}
		if (farmer.currentLocation == null)
		{
			return;
		}
		if (farmer.IsLocalPlayer)
		{
			if (PlayUseSounds)
			{
				farmer.playNearbySoundLocal("button1");
			}
			Rumble.rumble(0.5f, 150f);
		}
		farmer.UsingTool = true;
		isTimingCast = false;
		isCasting = true;
		castingChosenCountdown = 350f;
		farmer.armOffset.Y = 0f;
		if (castingPower > 0.99f && farmer.IsLocalPlayer)
		{
			Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(545, 1921, 53, 19), 800f, 1, 0, Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(0f, -192f)), flicker: false, flipped: false, 1f, 0.01f, Color.White, 2f, 0f, 0f, 0f, local: true)
			{
				motion = new Vector2(0f, -4f),
				acceleration = new Vector2(0f, 0.2f),
				delayBeforeAnimationStart = 200
			});
			if (PlayUseSounds)
			{
				DelayedAction.playSoundAfterDelay("crit", 200);
			}
		}
	}

	public void openChestEndFunction(int remainingFish)
	{
		Farmer farmer = lastUser;
		if (gotTroutDerbyTag && !treasureCaught)
		{
			farmer.playNearbySoundLocal("discoverMineral");
			animations.Add(new TemporaryAnimatedSprite("TileSheets\\Objects_2", new Rectangle(80, 16, 16, 16), 800f, 1, 0, farmer.Position + new Vector2(-8f, -196f), flicker: false, flipped: false, (float)farmer.StandingPixel.Y / 10000f + 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f)
			{
				endFunction = justGotDerbyTagEndFunction,
				extraInfoForEndBehavior = remainingFish,
				shakeIntensity = 0f
			});
			animations.AddRange(Utility.getTemporarySpritesWithinArea(new int[2] { 10, 11 }, new Rectangle((int)farmer.Position.X - 16, (int)farmer.Position.Y - 228 + 16, 32, 32), 4, Color.White));
		}
		else
		{
			farmer.playNearbySoundLocal("openChest");
			animations.Add(new TemporaryAnimatedSprite(goldenTreasure ? "LooseSprites\\Cursors_1_6" : "LooseSprites\\Cursors", goldenTreasure ? new Rectangle(256, 75, 32, 32) : new Rectangle(64, 1920, 32, 32), 200f, 4, 0, farmer.Position + new Vector2(-32f, -228f), flicker: false, flipped: false, (float)farmer.StandingPixel.Y / 10000f + 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f)
			{
				endFunction = openTreasureMenuEndFunction,
				extraInfoForEndBehavior = remainingFish
			});
		}
		sparklingText = null;
	}

	public void justGotDerbyTagEndFunction(int remainingFish)
	{
		Farmer farmer = lastUser;
		farmer.UsingTool = false;
		doneFishing(farmer, consumeBaitAndTackle: true);
		Item item = ItemRegistry.Create("(O)TroutDerbyTag");
		Item item2 = null;
		if (remainingFish == 1)
		{
			item2 = CreateFish();
		}
		if (PlayUseSounds)
		{
			Game1.playSound("coin");
		}
		gotTroutDerbyTag = false;
		if (!farmer.addItemToInventoryBool(item))
		{
			List<Item> list = new List<Item> { item };
			if (item2 != null)
			{
				list.Add(item2);
			}
			ItemGrabMenu itemGrabMenu = new ItemGrabMenu(list, this).setEssential(essential: true);
			itemGrabMenu.source = 3;
			Game1.activeClickableMenu = itemGrabMenu;
			farmer.completelyStopAnimatingOrDoingAction();
		}
		else if (item2 != null && !farmer.addItemToInventoryBool(item2))
		{
			ItemGrabMenu itemGrabMenu2 = new ItemGrabMenu(new List<Item> { item2 }, this).setEssential(essential: true);
			itemGrabMenu2.source = 3;
			Game1.activeClickableMenu = itemGrabMenu2;
			farmer.completelyStopAnimatingOrDoingAction();
		}
	}

	public override bool doesShowTileLocationMarker()
	{
		return false;
	}

	public void openTreasureMenuEndFunction(int remainingFish)
	{
		Farmer farmer = lastUser;
		farmer.gainExperience(5, 10 * (clearWaterDistance + 1));
		farmer.UsingTool = false;
		farmer.completelyStopAnimatingOrDoingAction();
		bool num = treasureCaught;
		doneFishing(farmer, consumeBaitAndTackle: true);
		List<Item> list = new List<Item>();
		if (remainingFish == 1)
		{
			list.Add(CreateFish());
		}
		float num2 = 1f;
		if (num)
		{
			Game1.player.stats.Increment("FishingTreasures", 1);
			while (Game1.random.NextDouble() <= (double)num2)
			{
				num2 *= (goldenTreasure ? 0.6f : 0.4f);
				if (Game1.IsSpring && !(farmer.currentLocation is Beach) && Game1.random.NextDouble() < 0.1)
				{
					list.Add(ItemRegistry.Create("(O)273", Game1.random.Next(2, 6) + ((Game1.random.NextDouble() < 0.25) ? 5 : 0)));
				}
				if (numberOfFishCaught > 1 && farmer.craftingRecipes.ContainsKey("Wild Bait") && Game1.random.NextBool())
				{
					list.Add(ItemRegistry.Create("(O)774", 2 + ((Game1.random.NextDouble() < 0.25) ? 2 : 0)));
				}
				if (Game1.random.NextDouble() <= 0.33 && farmer.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
				{
					list.Add(ItemRegistry.Create("(O)890", Game1.random.Next(1, 3) + ((Game1.random.NextDouble() < 0.25) ? 2 : 0)));
				}
				while (Utility.tryRollMysteryBox(0.08 + Game1.player.team.AverageDailyLuck() / 5.0))
				{
					list.Add(ItemRegistry.Create((Game1.player.stats.Get(StatKeys.Mastery(2)) != 0) ? "(O)GoldenMysteryBox" : "(O)MysteryBox"));
				}
				if (Game1.player.stats.Get(StatKeys.Mastery(0)) != 0 && Game1.random.NextDouble() < 0.05)
				{
					list.Add(ItemRegistry.Create("(O)GoldenAnimalCracker"));
				}
				if (goldenTreasure && Game1.random.NextDouble() < 0.5)
				{
					switch (Game1.random.Next(13))
					{
					case 0:
						list.Add(ItemRegistry.Create("(O)337", Game1.random.Next(1, 6)));
						break;
					case 1:
						list.Add(ItemRegistry.Create("(O)SkillBook_" + Game1.random.Next(5)));
						break;
					case 2:
						list.Add(Utility.getRaccoonSeedForCurrentTimeOfYear(Game1.player, Game1.random, 8));
						break;
					case 3:
						list.Add(ItemRegistry.Create("(O)213"));
						break;
					case 4:
						list.Add(ItemRegistry.Create("(O)872", Game1.random.Next(3, 6)));
						break;
					case 5:
						list.Add(ItemRegistry.Create("(O)687"));
						break;
					case 6:
						list.Add(ItemRegistry.Create("(O)ChallengeBait", Game1.random.Next(3, 6)));
						break;
					case 7:
						list.Add(ItemRegistry.Create("(O)703", Game1.random.Next(3, 6)));
						break;
					case 8:
						list.Add(ItemRegistry.Create("(O)StardropTea"));
						break;
					case 9:
						list.Add(ItemRegistry.Create("(O)797"));
						break;
					case 10:
						list.Add(ItemRegistry.Create("(O)733"));
						break;
					case 11:
						list.Add(ItemRegistry.Create("(O)728"));
						break;
					case 12:
						list.Add(ItemRegistry.Create("(O)SonarBobber"));
						break;
					}
					continue;
				}
				switch (Game1.random.Next(4))
				{
				case 0:
				{
					if (clearWaterDistance >= 5 && Game1.random.NextDouble() < 0.03)
					{
						list.Add(new Object("386", Game1.random.Next(1, 3)));
						break;
					}
					List<int> list2 = new List<int>();
					if (clearWaterDistance >= 4)
					{
						list2.Add(384);
					}
					if (clearWaterDistance >= 3 && (list2.Count == 0 || Game1.random.NextDouble() < 0.6))
					{
						list2.Add(380);
					}
					if (list2.Count == 0 || Game1.random.NextDouble() < 0.6)
					{
						list2.Add(378);
					}
					if (list2.Count == 0 || Game1.random.NextDouble() < 0.6)
					{
						list2.Add(388);
					}
					if (list2.Count == 0 || Game1.random.NextDouble() < 0.6)
					{
						list2.Add(390);
					}
					list2.Add(382);
					Item item5 = ItemRegistry.Create(Game1.random.ChooseFrom(list2).ToString(), Game1.random.Next(2, 7) * ((!(Game1.random.NextDouble() < 0.05 + (double)farmer.luckLevel.Value * 0.015)) ? 1 : 2));
					if (Game1.random.NextDouble() < 0.05 + (double)farmer.LuckLevel * 0.03)
					{
						item5.Stack *= 2;
					}
					list.Add(item5);
					break;
				}
				case 1:
					if (clearWaterDistance >= 4 && Game1.random.NextDouble() < 0.1 && farmer.FishingLevel >= 6)
					{
						list.Add(ItemRegistry.Create("(O)687"));
					}
					else if (Game1.random.NextDouble() < 0.25 && farmer.craftingRecipes.ContainsKey("Wild Bait"))
					{
						list.Add(ItemRegistry.Create("(O)774", 5 + ((Game1.random.NextDouble() < 0.25) ? 5 : 0)));
					}
					else if (Game1.random.NextDouble() < 0.11 && farmer.FishingLevel >= 6)
					{
						list.Add(ItemRegistry.Create("(O)SonarBobber"));
					}
					else if (farmer.FishingLevel >= 6)
					{
						list.Add(ItemRegistry.Create("(O)DeluxeBait", 5));
					}
					else
					{
						list.Add(ItemRegistry.Create("(O)685", 10));
					}
					break;
				case 2:
					if (Game1.random.NextDouble() < 0.1 && Game1.netWorldState.Value.LostBooksFound < 21 && farmer != null && farmer.hasOrWillReceiveMail("lostBookFound"))
					{
						list.Add(ItemRegistry.Create("(O)102"));
					}
					else if (farmer.archaeologyFound.Length > 0)
					{
						if (Game1.random.NextDouble() < 0.25 && farmer.FishingLevel > 1)
						{
							list.Add(ItemRegistry.Create("(O)" + Game1.random.Next(585, 588)));
						}
						else if (Game1.random.NextBool() && farmer.FishingLevel > 1)
						{
							list.Add(ItemRegistry.Create("(O)" + Game1.random.Next(103, 120)));
						}
						else
						{
							list.Add(ItemRegistry.Create("(O)535"));
						}
					}
					else
					{
						list.Add(ItemRegistry.Create("(O)382", Game1.random.Next(1, 3)));
					}
					break;
				case 3:
					switch (Game1.random.Next(3))
					{
					case 0:
					{
						Item item3 = ((clearWaterDistance >= 4) ? ItemRegistry.Create("(O)" + (537 + ((Game1.random.NextDouble() < 0.4) ? Game1.random.Next(-2, 0) : 0)), Game1.random.Next(1, 4)) : ((clearWaterDistance < 3) ? ItemRegistry.Create("(O)535", Game1.random.Next(1, 4)) : ItemRegistry.Create("(O)" + (536 + ((Game1.random.NextDouble() < 0.4) ? (-1) : 0)), Game1.random.Next(1, 4))));
						if (Game1.random.NextDouble() < 0.05 + (double)farmer.LuckLevel * 0.03)
						{
							item3.Stack *= 2;
						}
						list.Add(item3);
						break;
					}
					case 1:
					{
						if (farmer.FishingLevel < 2)
						{
							list.Add(ItemRegistry.Create("(O)382", Game1.random.Next(1, 4)));
							break;
						}
						Item item4;
						if (clearWaterDistance >= 4)
						{
							list.Add(item4 = ItemRegistry.Create("(O)" + ((Game1.random.NextDouble() < 0.3) ? 82 : Game1.random.Choose(64, 60)), Game1.random.Next(1, 3)));
						}
						else if (clearWaterDistance >= 3)
						{
							list.Add(item4 = ItemRegistry.Create("(O)" + ((Game1.random.NextDouble() < 0.3) ? 84 : Game1.random.Choose(70, 62)), Game1.random.Next(1, 3)));
						}
						else
						{
							list.Add(item4 = ItemRegistry.Create("(O)" + ((Game1.random.NextDouble() < 0.3) ? 86 : Game1.random.Choose(66, 68)), Game1.random.Next(1, 3)));
						}
						if (Game1.random.NextDouble() < 0.028 * (double)((float)clearWaterDistance / 5f))
						{
							list.Add(item4 = ItemRegistry.Create("(O)72"));
						}
						if (Game1.random.NextDouble() < 0.05)
						{
							item4.Stack *= 2;
						}
						break;
					}
					case 2:
					{
						if (farmer.FishingLevel < 2)
						{
							list.Add(new Object("770", Game1.random.Next(1, 4)));
							break;
						}
						float num3 = (1f + (float)farmer.DailyLuck) * ((float)clearWaterDistance / 5f);
						if (Game1.random.NextDouble() < 0.05 * (double)num3 && !farmer.specialItems.Contains("14"))
						{
							Item item = MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)14"), Game1.random);
							item.specialItem = true;
							list.Add(item);
						}
						if (Game1.random.NextDouble() < 0.05 * (double)num3 && !farmer.specialItems.Contains("51"))
						{
							Item item2 = MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)51"), Game1.random);
							item2.specialItem = true;
							list.Add(item2);
						}
						if (Game1.random.NextDouble() < 0.07 * (double)num3)
						{
							switch (Game1.random.Next(3))
							{
							case 0:
								list.Add(new Ring((516 + ((Game1.random.NextDouble() < (double)((float)farmer.LuckLevel / 11f)) ? 1 : 0)).ToString()));
								break;
							case 1:
								list.Add(new Ring((518 + ((Game1.random.NextDouble() < (double)((float)farmer.LuckLevel / 11f)) ? 1 : 0)).ToString()));
								break;
							case 2:
								list.Add(new Ring(Game1.random.Next(529, 535).ToString()));
								break;
							}
						}
						if (Game1.random.NextDouble() < 0.02 * (double)num3)
						{
							list.Add(ItemRegistry.Create("(O)166"));
						}
						if (farmer.FishingLevel > 5 && Game1.random.NextDouble() < 0.001 * (double)num3)
						{
							list.Add(ItemRegistry.Create("(O)74"));
						}
						if (Game1.random.NextDouble() < 0.01 * (double)num3)
						{
							list.Add(ItemRegistry.Create("(O)127"));
						}
						if (Game1.random.NextDouble() < 0.01 * (double)num3)
						{
							list.Add(ItemRegistry.Create("(O)126"));
						}
						if (Game1.random.NextDouble() < 0.01 * (double)num3)
						{
							list.Add(new Ring("527"));
						}
						if (Game1.random.NextDouble() < 0.01 * (double)num3)
						{
							list.Add(ItemRegistry.Create("(B)" + Game1.random.Next(504, 514)));
						}
						if (Game1.MasterPlayer.mailReceived.Contains("Farm_Eternal") && Game1.random.NextDouble() < 0.01 * (double)num3)
						{
							list.Add(ItemRegistry.Create("(O)928"));
						}
						if (list.Count == 1)
						{
							list.Add(ItemRegistry.Create("(O)72"));
						}
						if (Game1.player.stats.Get("FishingTreasures") > 3)
						{
							Random random = Utility.CreateRandom(Game1.player.stats.Get("FishingTreasures") * 27973, Game1.uniqueIDForThisGame);
							if (random.NextDouble() < 0.05 * (double)num3)
							{
								list.Add(ItemRegistry.Create("(O)SkillBook_" + random.Next(5)));
								num2 = 0f;
							}
						}
						break;
					}
					}
					break;
				}
			}
			if (list.Count == 0)
			{
				list.Add(ItemRegistry.Create("(O)685", Game1.random.Next(1, 4) * 5));
			}
			if (lastUser.hasQuest("98765") && Utility.GetDayOfPassiveFestival("DesertFestival") == 3 && !lastUser.Items.ContainsId("GoldenBobber", 1))
			{
				list.Clear();
				list.Add(ItemRegistry.Create("(O)GoldenBobber"));
			}
			if (Game1.random.NextDouble() < 0.25 && lastUser.stats.Get("Book_Roe") != 0)
			{
				Item item6 = CreateFish();
				ObjectDataDefinition objectTypeDefinition = ItemRegistry.GetObjectTypeDefinition();
				if (objectTypeDefinition.CanHaveRoe(item6))
				{
					Object obj = objectTypeDefinition.CreateFlavoredRoe(item6 as Object);
					obj.Stack = Game1.random.Next(1, 3);
					if (Game1.random.NextDouble() < 0.1 + lastUser.team.AverageDailyLuck())
					{
						obj.Stack++;
					}
					if (Game1.random.NextDouble() < 0.1 + lastUser.team.AverageDailyLuck())
					{
						obj.Stack *= 2;
					}
					list.Add(obj);
				}
			}
			if (Game1.player.fishingLevel.Value > 4 && Game1.player.stats.Get("FishingTreasures") > 2 && Game1.random.NextDouble() < 0.02 + ((!Game1.player.mailReceived.Contains("roeBookDropped")) ? ((double)Game1.player.stats.Get("FishingTreasures") * 0.001) : 0.001))
			{
				list.Add(ItemRegistry.Create("(O)Book_Roe"));
				Game1.player.mailReceived.Add("roeBookDropped");
			}
		}
		if (gotTroutDerbyTag)
		{
			list.Add(ItemRegistry.Create("(O)TroutDerbyTag"));
			gotTroutDerbyTag = false;
		}
		ItemGrabMenu itemGrabMenu = new ItemGrabMenu(list, this).setEssential(essential: true);
		itemGrabMenu.source = 3;
		Game1.activeClickableMenu = itemGrabMenu;
		farmer.completelyStopAnimatingOrDoingAction();
	}
}
