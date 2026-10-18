using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.ItemTypeDefinitions;

namespace StardewValley;

public class TemporaryAnimatedSprite
{
	public delegate void endBehavior(int extraInfo);

	public const int FireworkType_Heart = 0;

	public const int FireworkType_Star = 1;

	public const int FireworkType_Junimo = 2;

	public static float[] FireworksLifetimeMultiplier = new float[3] { 1f, 1f, 1.3f };

	public static Color[] FireworksColors = new Color[3]
	{
		new Color(252, 56, 37),
		new Color(144, 51, 237),
		new Color(92, 237, 213)
	};

	public static Vector2[][] FireworksLights = new Vector2[3][]
	{
		new Vector2[1]
		{
			new Vector2(0f, 0f)
		},
		new Vector2[1]
		{
			new Vector2(0f, 0f)
		},
		new Vector2[2]
		{
			new Vector2(-2.5f, 0f),
			new Vector2(2.5f, 0f)
		}
	};

	public static Vector2[][] FireworksPoints = new Vector2[3][]
	{
		new Vector2[14]
		{
			new Vector2(0f, -3f),
			new Vector2(2f, -5f),
			new Vector2(4f, -5f),
			new Vector2(6f, -3f),
			new Vector2(6f, -1f),
			new Vector2(4f, 1f),
			new Vector2(2f, 3f),
			new Vector2(0f, 5f),
			new Vector2(-2f, 3f),
			new Vector2(-4f, 1f),
			new Vector2(-6f, -1f),
			new Vector2(-6f, -3f),
			new Vector2(-4f, -5f),
			new Vector2(-2f, -5f)
		},
		new Vector2[20]
		{
			new Vector2(0f, -6f),
			new Vector2(1f, -4f),
			new Vector2(2f, -2f),
			new Vector2(4f, -2f),
			new Vector2(6f, -2f),
			new Vector2(4f, 0f),
			new Vector2(2f, 1f),
			new Vector2(3f, 3f),
			new Vector2(4f, 5f),
			new Vector2(2f, 4f),
			new Vector2(0f, 3f),
			new Vector2(-2f, 4f),
			new Vector2(-4f, 5f),
			new Vector2(-3f, 3f),
			new Vector2(-2f, 1f),
			new Vector2(-4f, 0f),
			new Vector2(-6f, -2f),
			new Vector2(-4f, -2f),
			new Vector2(-2f, -2f),
			new Vector2(-1f, -4f)
		},
		new Vector2[31]
		{
			new Vector2(-1f, -8f),
			new Vector2(0f, -6f),
			new Vector2(0f, -4f),
			new Vector2(2f, -4f),
			new Vector2(4f, -4f),
			new Vector2(6f, -2f),
			new Vector2(8f, -1f),
			new Vector2(9f, -3f),
			new Vector2(8f, -5f),
			new Vector2(6f, 0f),
			new Vector2(6f, 2f),
			new Vector2(3f, 2f),
			new Vector2(3f, 1f),
			new Vector2(5f, 4f),
			new Vector2(3f, 5f),
			new Vector2(3f, 7f),
			new Vector2(1f, 5f),
			new Vector2(-1f, 5f),
			new Vector2(-3f, 7f),
			new Vector2(-3f, 5f),
			new Vector2(-5f, 4f),
			new Vector2(-3f, 2f),
			new Vector2(-3f, 1f),
			new Vector2(-6f, 2f),
			new Vector2(-6f, 0f),
			new Vector2(-8f, -5f),
			new Vector2(-9f, -3f),
			new Vector2(-8f, -1f),
			new Vector2(-6f, -2f),
			new Vector2(-4f, -4f),
			new Vector2(-2f, -4f)
		}
	};

	public float timer;

	public float interval = 200f;

	public int currentParentTileIndex;

	public int oldCurrentParentTileIndex;

	public int initialParentTileIndex;

	public int totalNumberOfLoops;

	public int currentNumberOfLoops;

	public int xStopCoordinate = -1;

	public int yStopCoordinate = -1;

	public int animationLength;

	public int bombRadius;

	public int pingPongMotion = 1;

	public int bombDamage = -1;

	public int fireworkType = -1;

	public bool flicker;

	public bool timeBasedMotion;

	public bool overrideLocationDestroy;

	public bool pingPong;

	public bool holdLastFrame;

	public bool pulse;

	public int extraInfoForEndBehavior;

	/// <summary>The unique ID of the light source to add when rendering this sprite, or <c>null</c> for no light source.</summary>
	public string lightId;

	public int id;

	public bool bigCraftable;

	public bool swordswipe;

	public bool flash;

	public bool flipped;

	public bool verticalFlipped;

	public bool local;

	public bool hasLit;

	public bool xPeriodic;

	public bool yPeriodic;

	public bool destroyable = true;

	public bool paused;

	public bool stopAcceleratingWhenVelocityIsZero;

	public bool positionFollowsAttachedCharacter;

	public bool usePreciseTiming;

	public float rotation;

	public float alpha = 1f;

	public float alphaFade;

	public float layerDepth = -1f;

	public float scale = 1f;

	public float scaleChange;

	public float scaleChangeChange;

	public float rotationChange;

	public float lightRadius;

	public float xPeriodicRange;

	public float yPeriodicRange;

	public float xPeriodicLoopTime;

	public float yPeriodicLoopTime;

	public float shakeIntensityChange;

	public float shakeIntensity;

	public float pulseTime;

	public float pulseAmount = 1.1f;

	public float alphaFadeFade;

	public int lightFade = -1;

	public float afterAccelStopMotionX;

	public float afterAccelStopMotionY;

	public float layerDepthOffset;

	public Vector2 position;

	public Vector2 sourceRectStartingPos;

	protected GameLocation parent;

	public string textureName;

	public Texture2D texture;

	public Rectangle sourceRect;

	public Color color = Color.White;

	public Color lightcolor = Color.White;

	public Farmer owner;

	public Vector2 motion = Vector2.Zero;

	public Vector2 acceleration = Vector2.Zero;

	public Vector2 accelerationChange = Vector2.Zero;

	public Vector2 initialPosition;

	public Vector2 vectorScale;

	public int delayBeforeAnimationStart;

	public int ticksBeforeAnimationStart;

	public string startSound;

	public string endSound;

	public string text;

	public endBehavior endFunction;

	public endBehavior reachedStopCoordinate;

	public Action<TemporaryAnimatedSprite> reachedStopCoordinateSprite;

	public TemporaryAnimatedSprite parentSprite;

	public Character attachedCharacter;

	private float pulseTimer;

	private float originalScale;

	public bool drawAboveAlwaysFront;

	public bool dontClearOnAreaEntry;

	private Stopwatch stopWatch;

	private long previousStopwatchTime;

	protected bool _pooled;

	public static List<TemporaryAnimatedSprite> _pool;

	private float totalTimer;

	public bool Pooled => _pooled;

	public Vector2 Position
	{
		get
		{
			return position;
		}
		set
		{
			position = value;
		}
	}

	public Texture2D Texture => texture;

	public GameLocation Parent
	{
		get
		{
			return parent;
		}
		set
		{
			parent = value;
		}
	}

	public static float GetFireworkLifetimeMultiplier(int id)
	{
		return FireworksLifetimeMultiplier[id];
	}

	public static Color GetFireworkColor(int id)
	{
		return FireworksColors[id];
	}

	public static Vector2[] GetFireworkLights(int id)
	{
		return FireworksLights[id];
	}

	public static Vector2[] GetFireworkPoints(int id)
	{
		return FireworksPoints[id];
	}

	public TemporaryAnimatedSprite getClone()
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = GetTemporaryAnimatedSprite();
		temporaryAnimatedSprite.texture = texture;
		temporaryAnimatedSprite.interval = interval;
		temporaryAnimatedSprite.currentParentTileIndex = currentParentTileIndex;
		temporaryAnimatedSprite.oldCurrentParentTileIndex = oldCurrentParentTileIndex;
		temporaryAnimatedSprite.initialParentTileIndex = initialParentTileIndex;
		temporaryAnimatedSprite.totalNumberOfLoops = totalNumberOfLoops;
		temporaryAnimatedSprite.currentNumberOfLoops = currentNumberOfLoops;
		temporaryAnimatedSprite.xStopCoordinate = xStopCoordinate;
		temporaryAnimatedSprite.yStopCoordinate = yStopCoordinate;
		temporaryAnimatedSprite.animationLength = animationLength;
		temporaryAnimatedSprite.bombRadius = bombRadius;
		temporaryAnimatedSprite.bombDamage = bombDamage;
		temporaryAnimatedSprite.pingPongMotion = pingPongMotion;
		temporaryAnimatedSprite.fireworkType = fireworkType;
		temporaryAnimatedSprite.flicker = flicker;
		temporaryAnimatedSprite.timeBasedMotion = timeBasedMotion;
		temporaryAnimatedSprite.overrideLocationDestroy = overrideLocationDestroy;
		temporaryAnimatedSprite.pingPong = pingPong;
		temporaryAnimatedSprite.holdLastFrame = holdLastFrame;
		temporaryAnimatedSprite.extraInfoForEndBehavior = extraInfoForEndBehavior;
		temporaryAnimatedSprite.lightId = lightId;
		temporaryAnimatedSprite.acceleration = acceleration;
		temporaryAnimatedSprite.accelerationChange = accelerationChange;
		temporaryAnimatedSprite.alpha = alpha;
		temporaryAnimatedSprite.alphaFade = alphaFade;
		temporaryAnimatedSprite.attachedCharacter = attachedCharacter;
		temporaryAnimatedSprite.bigCraftable = bigCraftable;
		temporaryAnimatedSprite.color = color;
		temporaryAnimatedSprite.delayBeforeAnimationStart = delayBeforeAnimationStart;
		temporaryAnimatedSprite.ticksBeforeAnimationStart = ticksBeforeAnimationStart;
		temporaryAnimatedSprite.destroyable = destroyable;
		temporaryAnimatedSprite.endFunction = endFunction;
		temporaryAnimatedSprite.endSound = endSound;
		temporaryAnimatedSprite.flash = flash;
		temporaryAnimatedSprite.flipped = flipped;
		temporaryAnimatedSprite.hasLit = hasLit;
		temporaryAnimatedSprite.id = id;
		temporaryAnimatedSprite.initialPosition = initialPosition;
		temporaryAnimatedSprite.lightFade = lightFade;
		temporaryAnimatedSprite.local = local;
		temporaryAnimatedSprite.motion = motion;
		temporaryAnimatedSprite.owner = owner;
		temporaryAnimatedSprite.parent = parent;
		temporaryAnimatedSprite.parentSprite = parentSprite;
		temporaryAnimatedSprite.position = position;
		temporaryAnimatedSprite.rotation = rotation;
		temporaryAnimatedSprite.rotationChange = rotationChange;
		temporaryAnimatedSprite.scale = scale;
		temporaryAnimatedSprite.scaleChange = scaleChange;
		temporaryAnimatedSprite.scaleChangeChange = scaleChangeChange;
		temporaryAnimatedSprite.shakeIntensity = shakeIntensity;
		temporaryAnimatedSprite.shakeIntensityChange = shakeIntensityChange;
		temporaryAnimatedSprite.sourceRect = sourceRect;
		temporaryAnimatedSprite.sourceRectStartingPos = sourceRectStartingPos;
		temporaryAnimatedSprite.startSound = startSound;
		temporaryAnimatedSprite.timeBasedMotion = timeBasedMotion;
		temporaryAnimatedSprite.verticalFlipped = verticalFlipped;
		temporaryAnimatedSprite.xPeriodic = xPeriodic;
		temporaryAnimatedSprite.xPeriodicLoopTime = xPeriodicLoopTime;
		temporaryAnimatedSprite.xPeriodicRange = xPeriodicRange;
		temporaryAnimatedSprite.yPeriodic = yPeriodic;
		temporaryAnimatedSprite.yPeriodicLoopTime = yPeriodicLoopTime;
		temporaryAnimatedSprite.yPeriodicRange = yPeriodicRange;
		temporaryAnimatedSprite.yStopCoordinate = yStopCoordinate;
		temporaryAnimatedSprite.totalNumberOfLoops = totalNumberOfLoops;
		temporaryAnimatedSprite.stopAcceleratingWhenVelocityIsZero = stopAcceleratingWhenVelocityIsZero;
		temporaryAnimatedSprite.afterAccelStopMotionX = afterAccelStopMotionX;
		temporaryAnimatedSprite.afterAccelStopMotionY = afterAccelStopMotionY;
		temporaryAnimatedSprite.layerDepthOffset = layerDepthOffset;
		temporaryAnimatedSprite.positionFollowsAttachedCharacter = positionFollowsAttachedCharacter;
		temporaryAnimatedSprite.dontClearOnAreaEntry = dontClearOnAreaEntry;
		return temporaryAnimatedSprite;
	}

	public virtual void Pool()
	{
		timer = 0f;
		interval = 200f;
		currentParentTileIndex = 0;
		oldCurrentParentTileIndex = 0;
		initialParentTileIndex = 0;
		totalNumberOfLoops = 0;
		currentNumberOfLoops = 0;
		xStopCoordinate = -1;
		yStopCoordinate = -1;
		animationLength = 0;
		bombRadius = 0;
		pingPongMotion = 1;
		bombDamage = -1;
		fireworkType = -1;
		flicker = false;
		timeBasedMotion = false;
		overrideLocationDestroy = false;
		pingPong = false;
		holdLastFrame = false;
		pulse = false;
		extraInfoForEndBehavior = 0;
		lightId = null;
		bigCraftable = false;
		swordswipe = false;
		flash = false;
		flipped = false;
		verticalFlipped = false;
		local = false;
		hasLit = false;
		xPeriodic = false;
		yPeriodic = false;
		destroyable = true;
		paused = false;
		stopAcceleratingWhenVelocityIsZero = false;
		positionFollowsAttachedCharacter = false;
		rotation = 0f;
		alpha = 1f;
		alphaFade = 0f;
		layerDepth = -1f;
		scale = 1f;
		scaleChange = 0f;
		scaleChangeChange = 0f;
		rotationChange = 0f;
		id = 0;
		lightRadius = 0f;
		xPeriodicRange = 0f;
		yPeriodicRange = 0f;
		xPeriodicLoopTime = 0f;
		yPeriodicLoopTime = 0f;
		shakeIntensityChange = 0f;
		shakeIntensity = 0f;
		pulseTime = 0f;
		pulseAmount = 1.1f;
		alphaFadeFade = 0f;
		lightFade = -1;
		layerDepthOffset = 0f;
		afterAccelStopMotionX = 0f;
		afterAccelStopMotionY = 0f;
		position = Vector2.Zero;
		sourceRectStartingPos = Vector2.Zero;
		parent = null;
		textureName = null;
		texture = null;
		sourceRect = Rectangle.Empty;
		color = Color.White;
		lightcolor = Color.White;
		owner = null;
		motion = Vector2.Zero;
		acceleration = Vector2.Zero;
		accelerationChange = Vector2.Zero;
		initialPosition = Vector2.Zero;
		delayBeforeAnimationStart = 0;
		ticksBeforeAnimationStart = 0;
		startSound = null;
		endSound = null;
		text = null;
		endFunction = null;
		reachedStopCoordinate = null;
		reachedStopCoordinateSprite = null;
		parentSprite = null;
		attachedCharacter = null;
		pulseTimer = 0f;
		originalScale = 0f;
		drawAboveAlwaysFront = false;
		dontClearOnAreaEntry = false;
		_pool.Add(this);
	}

	public static TemporaryAnimatedSprite GetTemporaryAnimatedSprite()
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = null;
		if (_pool == null)
		{
			_pool = new List<TemporaryAnimatedSprite>();
			for (int i = 0; i < 256; i++)
			{
				TemporaryAnimatedSprite item = new TemporaryAnimatedSprite
				{
					_pooled = true
				};
				_pool.Add(item);
			}
		}
		if (_pool.Count > 0)
		{
			temporaryAnimatedSprite = _pool[_pool.Count - 1];
			_pool.RemoveAt(_pool.Count - 1);
		}
		if (temporaryAnimatedSprite == null)
		{
			temporaryAnimatedSprite = new TemporaryAnimatedSprite();
		}
		return temporaryAnimatedSprite;
	}

	public TemporaryAnimatedSprite()
	{
	}

	public static TemporaryAnimatedSprite GetTemporaryAnimatedSprite(int initialParentTileIndex, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = GetTemporaryAnimatedSprite();
		if (temporaryAnimatedSprite.initialParentTileIndex == -1)
		{
			temporaryAnimatedSprite.swordswipe = true;
			temporaryAnimatedSprite.currentParentTileIndex = 0;
		}
		else
		{
			temporaryAnimatedSprite.currentParentTileIndex = initialParentTileIndex;
		}
		temporaryAnimatedSprite.initialParentTileIndex = initialParentTileIndex;
		temporaryAnimatedSprite.interval = animationInterval;
		temporaryAnimatedSprite.totalNumberOfLoops = numberOfLoops;
		temporaryAnimatedSprite.position = position;
		temporaryAnimatedSprite.animationLength = animationLength;
		temporaryAnimatedSprite.flicker = flicker;
		temporaryAnimatedSprite.flipped = flipped;
		return temporaryAnimatedSprite;
	}

	public TemporaryAnimatedSprite(int initialParentTileIndex, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped)
	{
		if (initialParentTileIndex == -1)
		{
			swordswipe = true;
			currentParentTileIndex = 0;
		}
		else
		{
			currentParentTileIndex = initialParentTileIndex;
		}
		this.initialParentTileIndex = initialParentTileIndex;
		interval = animationInterval;
		totalNumberOfLoops = numberOfLoops;
		this.position = position;
		this.animationLength = animationLength;
		this.flicker = flicker;
		this.flipped = flipped;
	}

	public static TemporaryAnimatedSprite GetTemporaryAnimatedSprite(int rowInAnimationTexture, Vector2 position, Color color, int animationLength = 8, bool flipped = false, float animationInterval = 100f, int numberOfLoops = 0, int sourceRectWidth = -1, float layerDepth = -1f, int sourceRectHeight = -1, int delay = 0)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = GetTemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, rowInAnimationTexture * 64, sourceRectWidth, sourceRectHeight), animationInterval, animationLength, numberOfLoops, position, flicker: false, flipped, layerDepth, 0f, color, 1f, 0f, 0f, 0f);
		if (sourceRectWidth == -1)
		{
			sourceRectWidth = 64;
			temporaryAnimatedSprite.sourceRect.Width = 64;
		}
		if (sourceRectHeight == -1)
		{
			sourceRectHeight = 64;
			temporaryAnimatedSprite.sourceRect.Height = 64;
		}
		if (temporaryAnimatedSprite.layerDepth == -1f)
		{
			temporaryAnimatedSprite.layerDepth = (temporaryAnimatedSprite.position.Y + 32f) / 10000f;
		}
		temporaryAnimatedSprite.delayBeforeAnimationStart = delay;
		return temporaryAnimatedSprite;
	}

	public TemporaryAnimatedSprite(int rowInAnimationTexture, Vector2 position, Color color, int animationLength = 8, bool flipped = false, float animationInterval = 100f, int numberOfLoops = 0, int sourceRectWidth = -1, float layerDepth = -1f, int sourceRectHeight = -1, int delay = 0)
		: this("TileSheets\\animations", new Rectangle(0, rowInAnimationTexture * 64, sourceRectWidth, sourceRectHeight), animationInterval, animationLength, numberOfLoops, position, flicker: false, flipped, layerDepth, 0f, color, 1f, 0f, 0f, 0f)
	{
		if (sourceRectWidth == -1)
		{
			sourceRectWidth = 64;
			sourceRect.Width = 64;
		}
		if (sourceRectHeight == -1)
		{
			sourceRectHeight = 64;
			sourceRect.Height = 64;
		}
		if (layerDepth == -1f)
		{
			layerDepth = (position.Y + 32f) / 10000f;
		}
		delayBeforeAnimationStart = delay;
	}

	public static TemporaryAnimatedSprite GetTemporaryAnimatedSprite(int initialParentTileIndex, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped, bool verticalFlipped, float rotation)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = GetTemporaryAnimatedSprite(initialParentTileIndex, animationInterval, animationLength, numberOfLoops, position, flicker, flipped);
		temporaryAnimatedSprite.rotation = rotation;
		temporaryAnimatedSprite.verticalFlipped = verticalFlipped;
		return temporaryAnimatedSprite;
	}

	public TemporaryAnimatedSprite(int initialParentTileIndex, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped, bool verticalFlipped, float rotation)
		: this(initialParentTileIndex, animationInterval, animationLength, numberOfLoops, position, flicker, flipped)
	{
		this.rotation = rotation;
		this.verticalFlipped = verticalFlipped;
	}

	public static TemporaryAnimatedSprite GetTemporaryAnimatedSprite(int initialParentTileIndex, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool bigCraftable, bool flipped)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = GetTemporaryAnimatedSprite(initialParentTileIndex, animationInterval, animationLength, numberOfLoops, position, flicker, flipped);
		temporaryAnimatedSprite.bigCraftable = bigCraftable;
		if (temporaryAnimatedSprite.bigCraftable)
		{
			temporaryAnimatedSprite.position.Y -= 64f;
		}
		return temporaryAnimatedSprite;
	}

	public TemporaryAnimatedSprite(int initialParentTileIndex, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool bigCraftable, bool flipped)
		: this(initialParentTileIndex, animationInterval, animationLength, numberOfLoops, position, flicker, flipped)
	{
		this.bigCraftable = bigCraftable;
		if (bigCraftable)
		{
			this.position.Y -= 64f;
		}
	}

	public TemporaryAnimatedSprite GetTemporaryAnimatedSprite(string textureName, Rectangle sourceRect, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = GetTemporaryAnimatedSprite(0, animationInterval, animationLength, numberOfLoops, position, flicker, flipped);
		temporaryAnimatedSprite.textureName = textureName;
		temporaryAnimatedSprite.loadTexture();
		temporaryAnimatedSprite.sourceRect = sourceRect;
		temporaryAnimatedSprite.sourceRectStartingPos = new Vector2(sourceRect.X, sourceRect.Y);
		temporaryAnimatedSprite.initialPosition = position;
		return temporaryAnimatedSprite;
	}

	public TemporaryAnimatedSprite(string textureName, Rectangle sourceRect, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped)
		: this(0, animationInterval, animationLength, numberOfLoops, position, flicker, flipped)
	{
		this.textureName = textureName;
		loadTexture();
		this.sourceRect = sourceRect;
		sourceRectStartingPos = new Vector2(sourceRect.X, sourceRect.Y);
		initialPosition = position;
	}

	public static TemporaryAnimatedSprite GetTemporaryAnimatedSprite(string textureName, Rectangle sourceRect, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped, float layerDepth, float alphaFade, Color color, float scale, float scaleChange, float rotation, float rotationChange, bool local = false)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = GetTemporaryAnimatedSprite(0, animationInterval, animationLength, numberOfLoops, position, flicker, flipped);
		temporaryAnimatedSprite.textureName = textureName;
		temporaryAnimatedSprite.loadTexture();
		temporaryAnimatedSprite.sourceRect = sourceRect;
		temporaryAnimatedSprite.sourceRectStartingPos = new Vector2(sourceRect.X, sourceRect.Y);
		temporaryAnimatedSprite.layerDepth = layerDepth;
		temporaryAnimatedSprite.alphaFade = Math.Max(0f, alphaFade);
		temporaryAnimatedSprite.color = color;
		temporaryAnimatedSprite.scale = scale;
		temporaryAnimatedSprite.scaleChange = scaleChange;
		temporaryAnimatedSprite.rotation = rotation;
		temporaryAnimatedSprite.rotationChange = rotationChange;
		temporaryAnimatedSprite.local = local;
		temporaryAnimatedSprite.initialPosition = position;
		return temporaryAnimatedSprite;
	}

	public TemporaryAnimatedSprite(string textureName, Rectangle sourceRect, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped, float layerDepth, float alphaFade, Color color, float scale, float scaleChange, float rotation, float rotationChange, bool local = false)
		: this(0, animationInterval, animationLength, numberOfLoops, position, flicker, flipped)
	{
		this.textureName = textureName;
		loadTexture();
		this.sourceRect = sourceRect;
		sourceRectStartingPos = new Vector2(sourceRect.X, sourceRect.Y);
		this.layerDepth = layerDepth;
		this.alphaFade = Math.Max(0f, alphaFade);
		this.color = color;
		this.scale = scale;
		this.scaleChange = scaleChange;
		this.rotation = rotation;
		this.rotationChange = rotationChange;
		this.local = local;
		initialPosition = position;
	}

	public virtual void CopyAppearanceFromItemId(string itemId, int offset = 0)
	{
		scale = 4f * scale;
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(itemId);
		textureName = dataOrErrorItem.TextureName;
		loadTexture();
		sourceRect = dataOrErrorItem.GetSourceRect(offset);
		sourceRectStartingPos = Utility.PointToVector2(sourceRect.Location);
		currentParentTileIndex = 0;
		initialParentTileIndex = 0;
	}

	public static TemporaryAnimatedSprite GetTemporaryAnimatedSprite(string textureName, Rectangle sourceRect, Vector2 position, bool flipped, float alphaFade, Color color)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = GetTemporaryAnimatedSprite(0, 999999f, 1, 0, position, flicker: false, flipped);
		temporaryAnimatedSprite.textureName = textureName;
		temporaryAnimatedSprite.loadTexture();
		temporaryAnimatedSprite.sourceRect = sourceRect;
		temporaryAnimatedSprite.sourceRectStartingPos = new Vector2(sourceRect.X, sourceRect.Y);
		temporaryAnimatedSprite.initialPosition = position;
		temporaryAnimatedSprite.alphaFade = Math.Max(0f, alphaFade);
		temporaryAnimatedSprite.color = color;
		return temporaryAnimatedSprite;
	}

	public TemporaryAnimatedSprite(string textureName, Rectangle sourceRect, Vector2 position, bool flipped, float alphaFade, Color color)
		: this(0, 999999f, 1, 0, position, flicker: false, flipped)
	{
		this.textureName = textureName;
		loadTexture();
		this.sourceRect = sourceRect;
		sourceRectStartingPos = new Vector2(sourceRect.X, sourceRect.Y);
		initialPosition = position;
		this.alphaFade = Math.Max(0f, alphaFade);
		this.color = color;
	}

	public static TemporaryAnimatedSprite GetTemporaryAnimatedSprite(int initialParentTileIndex, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped, GameLocation parent, Farmer owner)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = GetTemporaryAnimatedSprite(initialParentTileIndex, animationInterval, animationLength, numberOfLoops, position, flicker, flipped);
		temporaryAnimatedSprite.position.X = (int)temporaryAnimatedSprite.position.X;
		temporaryAnimatedSprite.position.Y = (int)temporaryAnimatedSprite.position.Y;
		temporaryAnimatedSprite.parent = parent;
		switch (temporaryAnimatedSprite.initialParentTileIndex)
		{
		case 286:
			temporaryAnimatedSprite.bombRadius = 3;
			break;
		case 287:
			temporaryAnimatedSprite.bombRadius = 5;
			break;
		case 288:
			temporaryAnimatedSprite.bombRadius = 7;
			break;
		}
		temporaryAnimatedSprite.owner = owner;
		return temporaryAnimatedSprite;
	}

	/// <summary>Construct an instance for a bomb.</summary>
	public TemporaryAnimatedSprite(int initialParentTileIndex, float animationInterval, int animationLength, int numberOfLoops, Vector2 position, bool flicker, bool flipped, GameLocation parent, Farmer owner)
		: this(initialParentTileIndex, animationInterval, animationLength, numberOfLoops, position, flicker, flipped)
	{
		this.position.X = (int)this.position.X;
		this.position.Y = (int)this.position.Y;
		this.parent = parent;
		switch (initialParentTileIndex)
		{
		case 286:
			bombRadius = 3;
			break;
		case 287:
			bombRadius = 5;
			break;
		case 288:
			bombRadius = 7;
			break;
		}
		this.owner = owner;
	}

	private void loadTexture()
	{
		string text = textureName;
		if (text != null)
		{
			if (text == "")
			{
				texture = Game1.staminaRect;
			}
			else
			{
				texture = Game1.content.Load<Texture2D>(textureName);
			}
		}
		else
		{
			texture = null;
		}
	}

	public void Read(BinaryReader reader, GameLocation location)
	{
		timer = 0f;
		BitArray bitArray = reader.ReadBitArray();
		int num = 0;
		if (bitArray[num++])
		{
			interval = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			currentParentTileIndex = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			oldCurrentParentTileIndex = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			initialParentTileIndex = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			totalNumberOfLoops = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			currentNumberOfLoops = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			xStopCoordinate = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			yStopCoordinate = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			animationLength = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			bombRadius = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			bombDamage = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			pingPongMotion = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			fireworkType = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			flicker = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			timeBasedMotion = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			overrideLocationDestroy = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			pingPong = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			holdLastFrame = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			pulse = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			extraInfoForEndBehavior = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			lightId = reader.ReadString();
		}
		if (bitArray[num++])
		{
			bigCraftable = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			swordswipe = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			flash = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			flipped = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			verticalFlipped = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			local = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			lightFade = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			hasLit = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			xPeriodic = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			yPeriodic = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			destroyable = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			paused = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			rotation = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			alpha = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			alphaFade = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			layerDepth = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			scale = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			scaleChange = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			scaleChangeChange = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			rotationChange = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			id = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			lightRadius = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			xPeriodicRange = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			yPeriodicRange = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			xPeriodicLoopTime = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			yPeriodicLoopTime = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			shakeIntensityChange = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			shakeIntensity = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			pulseTime = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			pulseAmount = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			position = reader.ReadVector2();
		}
		if (bitArray[num++])
		{
			sourceRectStartingPos = reader.ReadVector2();
		}
		if (bitArray[num++])
		{
			sourceRect = reader.ReadRectangle();
		}
		if (bitArray[num++])
		{
			color = reader.ReadColor();
		}
		if (bitArray[num++])
		{
			lightcolor = reader.ReadColor();
		}
		if (bitArray[num++])
		{
			motion = reader.ReadVector2();
		}
		if (bitArray[num++])
		{
			acceleration = reader.ReadVector2();
		}
		if (bitArray[num++])
		{
			accelerationChange = reader.ReadVector2();
		}
		if (bitArray[num++])
		{
			initialPosition = reader.ReadVector2();
		}
		if (bitArray[num++])
		{
			delayBeforeAnimationStart = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			ticksBeforeAnimationStart = reader.ReadInt32();
		}
		if (bitArray[num++])
		{
			startSound = reader.ReadString();
		}
		if (bitArray[num++])
		{
			endSound = reader.ReadString();
		}
		if (bitArray[num++])
		{
			text = reader.ReadString();
		}
		if (bitArray[num++])
		{
			textureName = reader.ReadString();
		}
		if (bitArray[num++])
		{
			owner = Game1.GetPlayer(reader.ReadInt64()) ?? Game1.MasterPlayer;
		}
		if (bitArray[num++])
		{
			stopAcceleratingWhenVelocityIsZero = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			layerDepthOffset = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			afterAccelStopMotionX = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			afterAccelStopMotionY = reader.ReadSingle();
		}
		if (bitArray[num++])
		{
			positionFollowsAttachedCharacter = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			dontClearOnAreaEntry = reader.ReadBoolean();
		}
		if (bitArray[num++])
		{
			drawAboveAlwaysFront = reader.ReadBoolean();
		}
		parent = location;
		loadTexture();
		switch (reader.ReadByte())
		{
		case 1:
			attachedCharacter = Game1.GetPlayer(reader.ReadInt64()) ?? Game1.MasterPlayer;
			break;
		case 2:
		{
			Guid guid = reader.ReadGuid();
			if (!location.characters.ContainsGuid(guid))
			{
				Game1.log.Warn($"Failed to find character with GUID {guid} for TemporaryAniamtedSprite.attachedCharacter");
			}
			else
			{
				attachedCharacter = location.characters[guid];
			}
			break;
		}
		}
	}

	private void checkDirty<T>(BitArray dirtyBits, ref int i, T value, T defaultValue = default(T))
	{
		dirtyBits[i++] = !object.Equals(value, defaultValue);
	}

	public void Write(BinaryWriter writer, GameLocation location)
	{
		if (GetType() != typeof(TemporaryAnimatedSprite))
		{
			throw new InvalidOperationException("TemporaryAnimatedSprite.Write is not implemented for other types");
		}
		BitArray bitArray = new BitArray(80);
		int i = 0;
		checkDirty(bitArray, ref i, interval, 200f);
		checkDirty(bitArray, ref i, currentParentTileIndex, 0);
		checkDirty(bitArray, ref i, oldCurrentParentTileIndex, 0);
		checkDirty(bitArray, ref i, initialParentTileIndex, 0);
		checkDirty(bitArray, ref i, totalNumberOfLoops, 0);
		checkDirty(bitArray, ref i, currentNumberOfLoops, 0);
		checkDirty(bitArray, ref i, xStopCoordinate, -1);
		checkDirty(bitArray, ref i, yStopCoordinate, -1);
		checkDirty(bitArray, ref i, animationLength, 0);
		checkDirty(bitArray, ref i, bombRadius, 0);
		checkDirty(bitArray, ref i, bombDamage, 0);
		checkDirty(bitArray, ref i, pingPongMotion, -1);
		checkDirty(bitArray, ref i, fireworkType, -1);
		checkDirty(bitArray, ref i, flicker, defaultValue: false);
		checkDirty(bitArray, ref i, timeBasedMotion, defaultValue: false);
		checkDirty(bitArray, ref i, overrideLocationDestroy, defaultValue: false);
		checkDirty(bitArray, ref i, pingPong, defaultValue: false);
		checkDirty(bitArray, ref i, holdLastFrame, defaultValue: false);
		checkDirty(bitArray, ref i, pulse, defaultValue: false);
		checkDirty(bitArray, ref i, extraInfoForEndBehavior, 0);
		checkDirty(bitArray, ref i, lightId);
		checkDirty(bitArray, ref i, bigCraftable, defaultValue: false);
		checkDirty(bitArray, ref i, swordswipe, defaultValue: false);
		checkDirty(bitArray, ref i, flash, defaultValue: false);
		checkDirty(bitArray, ref i, flipped, defaultValue: false);
		checkDirty(bitArray, ref i, verticalFlipped, defaultValue: false);
		checkDirty(bitArray, ref i, local, defaultValue: false);
		checkDirty(bitArray, ref i, lightFade, 0);
		checkDirty(bitArray, ref i, hasLit, defaultValue: false);
		checkDirty(bitArray, ref i, xPeriodic, defaultValue: false);
		checkDirty(bitArray, ref i, yPeriodic, defaultValue: false);
		checkDirty(bitArray, ref i, destroyable, defaultValue: true);
		checkDirty(bitArray, ref i, paused, defaultValue: false);
		checkDirty(bitArray, ref i, rotation, 0f);
		checkDirty(bitArray, ref i, alpha, 1f);
		checkDirty(bitArray, ref i, alphaFade, 0f);
		checkDirty(bitArray, ref i, layerDepth, -1f);
		checkDirty(bitArray, ref i, scale, 1f);
		checkDirty(bitArray, ref i, scaleChange, 0f);
		checkDirty(bitArray, ref i, scaleChangeChange, 0f);
		checkDirty(bitArray, ref i, rotationChange, 0f);
		checkDirty(bitArray, ref i, id, 0);
		checkDirty(bitArray, ref i, lightRadius, 0f);
		checkDirty(bitArray, ref i, xPeriodicRange, 0f);
		checkDirty(bitArray, ref i, yPeriodicRange, 0f);
		checkDirty(bitArray, ref i, xPeriodicLoopTime, 0f);
		checkDirty(bitArray, ref i, yPeriodicLoopTime, 0f);
		checkDirty(bitArray, ref i, shakeIntensityChange, 0f);
		checkDirty(bitArray, ref i, shakeIntensity, 0f);
		checkDirty(bitArray, ref i, pulseTime, 0f);
		checkDirty(bitArray, ref i, pulseAmount, 1.1f);
		checkDirty(bitArray, ref i, position);
		checkDirty(bitArray, ref i, sourceRectStartingPos);
		checkDirty(bitArray, ref i, sourceRect);
		checkDirty(bitArray, ref i, color, Color.White);
		checkDirty(bitArray, ref i, lightcolor, Color.White);
		checkDirty(bitArray, ref i, motion, Vector2.Zero);
		checkDirty(bitArray, ref i, acceleration, Vector2.Zero);
		checkDirty(bitArray, ref i, accelerationChange, Vector2.Zero);
		checkDirty(bitArray, ref i, initialPosition);
		checkDirty(bitArray, ref i, delayBeforeAnimationStart, 0);
		checkDirty(bitArray, ref i, ticksBeforeAnimationStart, 0);
		checkDirty(bitArray, ref i, startSound);
		checkDirty(bitArray, ref i, endSound);
		checkDirty(bitArray, ref i, text);
		checkDirty(bitArray, ref i, texture);
		checkDirty(bitArray, ref i, owner);
		checkDirty(bitArray, ref i, stopAcceleratingWhenVelocityIsZero, defaultValue: false);
		checkDirty(bitArray, ref i, layerDepthOffset, 0f);
		checkDirty(bitArray, ref i, afterAccelStopMotionX, 0f);
		checkDirty(bitArray, ref i, afterAccelStopMotionY, 0f);
		checkDirty(bitArray, ref i, positionFollowsAttachedCharacter, defaultValue: false);
		checkDirty(bitArray, ref i, dontClearOnAreaEntry, defaultValue: false);
		checkDirty(bitArray, ref i, drawAboveAlwaysFront, defaultValue: false);
		writer.WriteBitArray(bitArray);
		i = 0;
		if (bitArray[i++])
		{
			writer.Write(interval);
		}
		if (bitArray[i++])
		{
			writer.Write(currentParentTileIndex);
		}
		if (bitArray[i++])
		{
			writer.Write(oldCurrentParentTileIndex);
		}
		if (bitArray[i++])
		{
			writer.Write(initialParentTileIndex);
		}
		if (bitArray[i++])
		{
			writer.Write(totalNumberOfLoops);
		}
		if (bitArray[i++])
		{
			writer.Write(currentNumberOfLoops);
		}
		if (bitArray[i++])
		{
			writer.Write(xStopCoordinate);
		}
		if (bitArray[i++])
		{
			writer.Write(yStopCoordinate);
		}
		if (bitArray[i++])
		{
			writer.Write(animationLength);
		}
		if (bitArray[i++])
		{
			writer.Write(bombRadius);
		}
		if (bitArray[i++])
		{
			writer.Write(bombDamage);
		}
		if (bitArray[i++])
		{
			writer.Write(pingPongMotion);
		}
		if (bitArray[i++])
		{
			writer.Write(fireworkType);
		}
		if (bitArray[i++])
		{
			writer.Write(flicker);
		}
		if (bitArray[i++])
		{
			writer.Write(timeBasedMotion);
		}
		if (bitArray[i++])
		{
			writer.Write(overrideLocationDestroy);
		}
		if (bitArray[i++])
		{
			writer.Write(pingPong);
		}
		if (bitArray[i++])
		{
			writer.Write(holdLastFrame);
		}
		if (bitArray[i++])
		{
			writer.Write(pulse);
		}
		if (bitArray[i++])
		{
			writer.Write(extraInfoForEndBehavior);
		}
		if (bitArray[i++])
		{
			writer.Write(lightId);
		}
		if (bitArray[i++])
		{
			writer.Write(bigCraftable);
		}
		if (bitArray[i++])
		{
			writer.Write(swordswipe);
		}
		if (bitArray[i++])
		{
			writer.Write(flash);
		}
		if (bitArray[i++])
		{
			writer.Write(flipped);
		}
		if (bitArray[i++])
		{
			writer.Write(verticalFlipped);
		}
		if (bitArray[i++])
		{
			writer.Write(local);
		}
		if (bitArray[i++])
		{
			writer.Write(lightFade);
		}
		if (bitArray[i++])
		{
			writer.Write(hasLit);
		}
		if (bitArray[i++])
		{
			writer.Write(xPeriodic);
		}
		if (bitArray[i++])
		{
			writer.Write(yPeriodic);
		}
		if (bitArray[i++])
		{
			writer.Write(destroyable);
		}
		if (bitArray[i++])
		{
			writer.Write(paused);
		}
		if (bitArray[i++])
		{
			writer.Write(rotation);
		}
		if (bitArray[i++])
		{
			writer.Write(alpha);
		}
		if (bitArray[i++])
		{
			writer.Write(alphaFade);
		}
		if (bitArray[i++])
		{
			writer.Write(layerDepth);
		}
		if (bitArray[i++])
		{
			writer.Write(scale);
		}
		if (bitArray[i++])
		{
			writer.Write(scaleChange);
		}
		if (bitArray[i++])
		{
			writer.Write(scaleChangeChange);
		}
		if (bitArray[i++])
		{
			writer.Write(rotationChange);
		}
		if (bitArray[i++])
		{
			writer.Write(id);
		}
		if (bitArray[i++])
		{
			writer.Write(lightRadius);
		}
		if (bitArray[i++])
		{
			writer.Write(xPeriodicRange);
		}
		if (bitArray[i++])
		{
			writer.Write(yPeriodicRange);
		}
		if (bitArray[i++])
		{
			writer.Write(xPeriodicLoopTime);
		}
		if (bitArray[i++])
		{
			writer.Write(yPeriodicLoopTime);
		}
		if (bitArray[i++])
		{
			writer.Write(shakeIntensityChange);
		}
		if (bitArray[i++])
		{
			writer.Write(shakeIntensity);
		}
		if (bitArray[i++])
		{
			writer.Write(pulseTime);
		}
		if (bitArray[i++])
		{
			writer.Write(pulseAmount);
		}
		if (bitArray[i++])
		{
			writer.WriteVector2(position);
		}
		if (bitArray[i++])
		{
			writer.WriteVector2(sourceRectStartingPos);
		}
		if (bitArray[i++])
		{
			writer.WriteRectangle(sourceRect);
		}
		if (bitArray[i++])
		{
			writer.WriteColor(color);
		}
		if (bitArray[i++])
		{
			writer.WriteColor(lightcolor);
		}
		if (bitArray[i++])
		{
			writer.WriteVector2(motion);
		}
		if (bitArray[i++])
		{
			writer.WriteVector2(acceleration);
		}
		if (bitArray[i++])
		{
			writer.WriteVector2(accelerationChange);
		}
		if (bitArray[i++])
		{
			writer.WriteVector2(initialPosition);
		}
		if (bitArray[i++])
		{
			writer.Write(delayBeforeAnimationStart);
		}
		if (bitArray[i++])
		{
			writer.Write(ticksBeforeAnimationStart);
		}
		if (bitArray[i++])
		{
			writer.Write(startSound);
		}
		if (bitArray[i++])
		{
			writer.Write(endSound);
		}
		if (bitArray[i++])
		{
			writer.Write(text);
		}
		if (bitArray[i++])
		{
			writer.Write(textureName);
		}
		if (bitArray[i++])
		{
			writer.Write(owner.uniqueMultiplayerID.Value);
		}
		if (bitArray[i++])
		{
			writer.Write(stopAcceleratingWhenVelocityIsZero);
		}
		if (bitArray[i++])
		{
			writer.Write(layerDepthOffset);
		}
		if (bitArray[i++])
		{
			writer.Write(afterAccelStopMotionX);
		}
		if (bitArray[i++])
		{
			writer.Write(afterAccelStopMotionY);
		}
		if (bitArray[i++])
		{
			writer.Write(positionFollowsAttachedCharacter);
		}
		if (bitArray[i++])
		{
			writer.Write(dontClearOnAreaEntry);
		}
		if (bitArray[i++])
		{
			writer.Write(drawAboveAlwaysFront);
		}
		Character character = attachedCharacter;
		if (character != null)
		{
			if (!(character is Farmer farmer))
			{
				if (!(character is NPC item))
				{
					throw new ArgumentException();
				}
				writer.Write((byte)2);
				writer.WriteGuid(location.characters.GuidOf(item));
			}
			else
			{
				writer.Write((byte)1);
				writer.Write(farmer.UniqueMultiplayerID);
			}
		}
		else
		{
			writer.Write((byte)0);
		}
	}

	public virtual void draw(SpriteBatch spriteBatch, bool localPosition = false, int xOffset = 0, int yOffset = 0, float extraAlpha = 1f)
	{
		if (local)
		{
			localPosition = true;
		}
		if (currentParentTileIndex < 0 || delayBeforeAnimationStart > 0 || ticksBeforeAnimationStart > 0)
		{
			return;
		}
		if (text != null)
		{
			if (extraInfoForEndBehavior == -777)
			{
				Vector2 vector = Game1.GlobalToLocal(position);
				SpriteText.drawString(spriteBatch, text, (int)vector.X, (int)vector.Y, 999999, -1, 999999, alpha, layerDepth, junimoText: false, -1, "", color.Equals(Color.White) ? SpriteText.color_White : SpriteText.color_Black);
			}
			else
			{
				spriteBatch.DrawString(Game1.dialogueFont, text, localPosition ? Position : Game1.GlobalToLocal(Game1.viewport, Position), color * alpha * extraAlpha, rotation, Vector2.Zero, scale, SpriteEffects.None, layerDepth + layerDepthOffset);
			}
		}
		else if (Texture != null)
		{
			if (positionFollowsAttachedCharacter && attachedCharacter != null)
			{
				spriteBatch.Draw(Texture, (localPosition ? Position : Game1.GlobalToLocal(Game1.viewport, attachedCharacter.Position + new Vector2((int)Position.X + xOffset, (int)Position.Y + yOffset))) + new Vector2(sourceRect.Width / 2, sourceRect.Height / 2) * scale + new Vector2((shakeIntensity > 0f) ? Game1.random.Next(-(int)shakeIntensity, (int)shakeIntensity + 1) : 0, (shakeIntensity > 0f) ? Game1.random.Next(-(int)shakeIntensity, (int)shakeIntensity + 1) : 0), sourceRect, color * alpha * extraAlpha, rotation, new Vector2(sourceRect.Width / 2, sourceRect.Height / 2), scale, flipped ? SpriteEffects.FlipHorizontally : (verticalFlipped ? SpriteEffects.FlipVertically : SpriteEffects.None), ((layerDepth >= 0f) ? layerDepth : ((Position.Y + (float)sourceRect.Height) / 10000f)) + layerDepthOffset);
			}
			else if (!vectorScale.Equals(Vector2.Zero))
			{
				spriteBatch.Draw(Texture, (localPosition ? Position : Game1.GlobalToLocal(Game1.viewport, new Vector2((int)Position.X + xOffset, (int)Position.Y + yOffset))) + new Vector2((shakeIntensity > 0f) ? Game1.random.Next(-(int)shakeIntensity, (int)shakeIntensity + 1) : 0, (shakeIntensity > 0f) ? Game1.random.Next(-(int)shakeIntensity, (int)shakeIntensity + 1) : 0), sourceRect, color * alpha * extraAlpha, rotation, new Vector2(sourceRect.Width / 2, sourceRect.Height / 2), vectorScale, flipped ? SpriteEffects.FlipHorizontally : (verticalFlipped ? SpriteEffects.FlipVertically : SpriteEffects.None), ((layerDepth >= 0f) ? layerDepth : ((Position.Y + (float)sourceRect.Height) / 10000f)) + layerDepthOffset);
			}
			else
			{
				spriteBatch.Draw(Texture, (localPosition ? Position : Game1.GlobalToLocal(Game1.viewport, new Vector2((int)Position.X + xOffset, (int)Position.Y + yOffset))) + new Vector2(sourceRect.Width / 2, sourceRect.Height / 2) * scale + new Vector2((shakeIntensity > 0f) ? Game1.random.Next(-(int)shakeIntensity, (int)shakeIntensity + 1) : 0, (shakeIntensity > 0f) ? Game1.random.Next(-(int)shakeIntensity, (int)shakeIntensity + 1) : 0), sourceRect, color * alpha * extraAlpha, rotation, new Vector2(sourceRect.Width / 2, sourceRect.Height / 2), scale, flipped ? SpriteEffects.FlipHorizontally : (verticalFlipped ? SpriteEffects.FlipVertically : SpriteEffects.None), ((layerDepth >= 0f) ? layerDepth : ((Position.Y + (float)sourceRect.Height) / 10000f)) + layerDepthOffset);
			}
		}
		else if (bigCraftable)
		{
			spriteBatch.Draw(Game1.bigCraftableSpriteSheet, localPosition ? Position : (Game1.GlobalToLocal(Game1.viewport, new Vector2((int)Position.X + xOffset, (int)Position.Y + yOffset)) + new Vector2(sourceRect.Width / 2, sourceRect.Height / 2)), Object.getSourceRectForBigCraftable(currentParentTileIndex), Color.White * extraAlpha, 0f, new Vector2(sourceRect.Width / 2, sourceRect.Height / 2), scale, SpriteEffects.None, (Position.Y + 32f) / 10000f + layerDepthOffset);
		}
		else
		{
			if (swordswipe)
			{
				return;
			}
			if (attachedCharacter != null)
			{
				if (local)
				{
					attachedCharacter.Position = new Vector2((float)Game1.viewport.X + Position.X, (float)Game1.viewport.Y + Position.Y);
				}
				attachedCharacter.draw(spriteBatch);
			}
			else
			{
				spriteBatch.Draw(Game1.objectSpriteSheet, localPosition ? Position : (Game1.GlobalToLocal(Game1.viewport, new Vector2((int)Position.X + xOffset, (int)Position.Y + yOffset)) + new Vector2(8f, 8f) * 4f + new Vector2((shakeIntensity > 0f) ? Game1.random.Next(-(int)shakeIntensity, (int)shakeIntensity + 1) : 0, (shakeIntensity > 0f) ? Game1.random.Next(-(int)shakeIntensity, (int)shakeIntensity + 1) : 0)), GameLocation.getSourceRectForObject(currentParentTileIndex), (flash ? (Color.LightBlue * 0.85f) : color) * alpha * extraAlpha, rotation, new Vector2(8f, 8f), 4f * scale, flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, ((layerDepth >= 0f) ? layerDepth : ((Position.Y + 32f) / 10000f)) + layerDepthOffset);
			}
		}
	}

	public void bounce(int extraInfo)
	{
		if ((float)extraInfo > 1f)
		{
			motion.Y = (float)(-extraInfo) / 2f;
			motion.X /= 2f;
			rotationChange = motion.Y / 50f;
			acceleration.Y = 0.7f;
			yStopCoordinate = (int)initialPosition.Y;
			parent?.playSound("thudStep");
		}
		else
		{
			if (extraInfoForEndBehavior != -777)
			{
				alphaFade = 0.01f;
			}
			motion.X = 0f;
		}
	}

	public void unload()
	{
		PlaySound(endSound);
		endFunction?.Invoke(extraInfoForEndBehavior);
		if (hasLit)
		{
			Utility.removeLightSource(lightId);
		}
	}

	public void reset()
	{
		sourceRect.X = (int)sourceRectStartingPos.X;
		sourceRect.Y = (int)sourceRectStartingPos.Y;
		currentParentTileIndex = 0;
		oldCurrentParentTileIndex = 0;
		timer = 0f;
		totalTimer = 0f;
		currentNumberOfLoops = 0;
		pingPongMotion = 1;
	}

	public void resetEnd()
	{
		reset();
		currentParentTileIndex = initialParentTileIndex + animationLength - 1;
	}

	public virtual bool update(GameTime time)
	{
		if (paused)
		{
			return false;
		}
		int num = (int)time.ElapsedGameTime.TotalMilliseconds;
		if (usePreciseTiming)
		{
			if (stopWatch == null)
			{
				stopWatch = new Stopwatch();
				stopWatch.Start();
			}
			num = (int)(stopWatch.ElapsedMilliseconds - previousStopwatchTime);
			previousStopwatchTime = stopWatch.ElapsedMilliseconds;
		}
		if (bombRadius > 0 && !Game1.shouldTimePass())
		{
			return false;
		}
		if (ticksBeforeAnimationStart > 0)
		{
			ticksBeforeAnimationStart--;
			return false;
		}
		if (delayBeforeAnimationStart > 0)
		{
			delayBeforeAnimationStart -= num;
			if (delayBeforeAnimationStart <= 0)
			{
				PlaySound(startSound);
				timer = -delayBeforeAnimationStart;
			}
			if (delayBeforeAnimationStart <= 0 && parentSprite != null)
			{
				position = parentSprite.position + position;
			}
			return false;
		}
		if (float.IsNaN(motion.X))
		{
			motion.X = 0f;
		}
		if (float.IsNaN(motion.Y))
		{
			motion.Y = 0f;
		}
		timer += num;
		totalTimer += num;
		alpha -= alphaFade * (float)((!timeBasedMotion) ? 1 : num);
		alphaFade -= alphaFadeFade * (float)((!timeBasedMotion) ? 1 : num);
		if (alphaFade > 0f && lightId != null && alpha < 1f && alpha >= 0f)
		{
			LightSource lightSource = Utility.getLightSource(lightId);
			if (lightSource != null)
			{
				lightSource.color.A = (byte)(255f * alpha);
			}
		}
		shakeIntensity += shakeIntensityChange * (float)num;
		scale += scaleChange * (float)((!timeBasedMotion) ? 1 : num);
		scaleChange += scaleChangeChange * (float)((!timeBasedMotion) ? 1 : num);
		rotation += rotationChange;
		if (xPeriodic)
		{
			position.X = initialPosition.X + xPeriodicRange * (float)Math.Sin(Math.PI * 2.0 / (double)xPeriodicLoopTime * (double)totalTimer);
		}
		else
		{
			position.X += motion.X * (float)((!timeBasedMotion) ? 1 : num);
		}
		if (yPeriodic)
		{
			position.Y = initialPosition.Y + yPeriodicRange * (float)Math.Sin(Math.PI * 2.0 / (double)yPeriodicLoopTime * (double)(totalTimer + yPeriodicLoopTime / 2f));
		}
		else
		{
			position.Y += motion.Y * (float)((!timeBasedMotion) ? 1 : num);
		}
		if (attachedCharacter != null && !positionFollowsAttachedCharacter)
		{
			if (xPeriodic)
			{
				attachedCharacter.position.X = initialPosition.X + xPeriodicRange * (float)Math.Sin(Math.PI * 2.0 / (double)xPeriodicLoopTime * (double)totalTimer);
			}
			else
			{
				attachedCharacter.position.X += motion.X * (float)((!timeBasedMotion) ? 1 : num);
			}
			if (yPeriodic)
			{
				attachedCharacter.position.Y = initialPosition.Y + yPeriodicRange * (float)Math.Sin(Math.PI * 2.0 / (double)yPeriodicLoopTime * (double)totalTimer);
			}
			else
			{
				attachedCharacter.position.Y += motion.Y * (float)((!timeBasedMotion) ? 1 : num);
			}
		}
		int num2 = Math.Sign(motion.X);
		motion.X += acceleration.X * (float)((!timeBasedMotion) ? 1 : num);
		if (stopAcceleratingWhenVelocityIsZero && Math.Sign(motion.X) != num2)
		{
			motion.X = afterAccelStopMotionX;
			acceleration.X = 0f;
			accelerationChange.X = 0f;
		}
		num2 = Math.Sign(motion.Y);
		motion.Y += acceleration.Y * (float)((!timeBasedMotion) ? 1 : num);
		if (stopAcceleratingWhenVelocityIsZero && Math.Sign(motion.Y) != num2)
		{
			motion.Y = afterAccelStopMotionY;
			acceleration.Y = 0f;
			accelerationChange.Y = 0f;
		}
		acceleration.X += accelerationChange.X;
		acceleration.Y += accelerationChange.Y;
		if (xStopCoordinate != -1 || yStopCoordinate != -1)
		{
			int extraInfo = (int)motion.Y;
			if (xStopCoordinate != -1 && Math.Abs(position.X - (float)xStopCoordinate) <= Math.Abs(motion.X))
			{
				motion.X = 0f;
				acceleration.X = 0f;
				xStopCoordinate = -1;
			}
			if (yStopCoordinate != -1 && Math.Abs(position.Y - (float)yStopCoordinate) <= Math.Abs(motion.Y))
			{
				motion.Y = 0f;
				acceleration.Y = 0f;
				yStopCoordinate = -1;
			}
			if (xStopCoordinate == -1 && yStopCoordinate == -1)
			{
				rotationChange = 0f;
				reachedStopCoordinate?.Invoke(extraInfo);
				reachedStopCoordinateSprite?.Invoke(this);
			}
		}
		if (!pingPong)
		{
			pingPongMotion = 1;
		}
		if (pulse)
		{
			pulseTimer -= num;
			if (originalScale == 0f)
			{
				originalScale = scale;
			}
			if (pulseTimer <= 0f)
			{
				pulseTimer = pulseTime;
				scale = originalScale * pulseAmount;
			}
			if (scale > originalScale)
			{
				scale -= pulseAmount / 100f * (float)num;
			}
		}
		if (lightId != null)
		{
			if (!hasLit)
			{
				hasLit = true;
				if (parent == null || Game1.currentLocation == parent)
				{
					Game1.currentLightSources.Add(new LightSource(lightId, 4, position + new Vector2(32f, 32f), lightRadius, lightcolor.Equals(Color.White) ? new Color(0, 65, 128) : lightcolor, LightSource.LightContext.None, 0L)
					{
						fadeOut = { lightFade }
					});
				}
			}
			else
			{
				Utility.repositionLightSource(lightId, position + new Vector2(32f, 32f));
			}
		}
		if (alpha <= 0f || (position.X < -2000f && !overrideLocationDestroy) || scale <= 0f)
		{
			unload();
			return destroyable;
		}
		if (timer > interval)
		{
			currentParentTileIndex += pingPongMotion;
			sourceRect.X += sourceRect.Width * pingPongMotion;
			if (Texture != null)
			{
				if (!pingPong && sourceRect.X >= Texture.Width)
				{
					sourceRect.Y += sourceRect.Height;
				}
				if (!pingPong)
				{
					sourceRect.X %= Texture.Width;
				}
				if (pingPong)
				{
					if ((float)sourceRect.X + ((float)sourceRect.Y - sourceRectStartingPos.Y) / (float)sourceRect.Height * (float)Texture.Width >= sourceRectStartingPos.X + (float)(sourceRect.Width * animationLength))
					{
						pingPongMotion = -1;
						sourceRect.X -= sourceRect.Width * 2;
						currentParentTileIndex--;
						if (sourceRect.X < 0)
						{
							sourceRect.X = Texture.Width + sourceRect.X;
						}
					}
					else if ((float)sourceRect.X < sourceRectStartingPos.X && (float)sourceRect.Y == sourceRectStartingPos.Y)
					{
						pingPongMotion = 1;
						sourceRect.X = (int)sourceRectStartingPos.X + sourceRect.Width;
						currentParentTileIndex++;
						currentNumberOfLoops++;
						if (endFunction != null)
						{
							endFunction(extraInfoForEndBehavior);
							endFunction = null;
						}
						if (currentNumberOfLoops >= totalNumberOfLoops)
						{
							unload();
							return destroyable;
						}
					}
				}
				else if (totalNumberOfLoops >= 1 && (float)sourceRect.X + ((float)sourceRect.Y - sourceRectStartingPos.Y) / (float)sourceRect.Height * (float)Texture.Width >= sourceRectStartingPos.X + (float)(sourceRect.Width * animationLength))
				{
					sourceRect.X = (int)sourceRectStartingPos.X;
					sourceRect.Y = (int)sourceRectStartingPos.Y;
				}
			}
			timer -= interval;
			if (flicker)
			{
				if (currentParentTileIndex < 0 || flash)
				{
					currentParentTileIndex = oldCurrentParentTileIndex;
					flash = false;
				}
				else
				{
					oldCurrentParentTileIndex = currentParentTileIndex;
					if (bombRadius > 0)
					{
						flash = true;
					}
					else
					{
						currentParentTileIndex = -100;
					}
				}
			}
			if (currentParentTileIndex - initialParentTileIndex >= animationLength)
			{
				currentNumberOfLoops++;
				if (holdLastFrame)
				{
					currentParentTileIndex = initialParentTileIndex + animationLength - 1;
					if (texture != null)
					{
						setSourceRectToCurrentTileIndex();
					}
					if (endFunction != null)
					{
						endFunction(extraInfoForEndBehavior);
						endFunction = null;
					}
					return false;
				}
				currentParentTileIndex = initialParentTileIndex;
				if (currentNumberOfLoops >= totalNumberOfLoops)
				{
					if (bombRadius > 0)
					{
						if (Game1.currentLocation == parent)
						{
							Game1.flashAlpha = 1f;
						}
						if (Game1.IsMasterGame)
						{
							parent.netAudio.StopPlaying("fuse");
							parent.playSound("explosion");
							parent.explode(new Vector2((int)(position.X / 64f), (int)(position.Y / 64f)), bombRadius, owner, damageFarmers: true, bombDamage);
						}
					}
					if (fireworkType >= 0)
					{
						float fireworkLifetimeMultiplier = GetFireworkLifetimeMultiplier(fireworkType);
						Color fireworkColor = GetFireworkColor(fireworkType);
						if (Game1.currentLocation == parent)
						{
							Game1.screenGlowOnce(fireworkColor * 0.8f, hold: false);
						}
						if (Game1.IsMasterGame)
						{
							float num3 = 0.3f;
							float num4 = id;
							Vector2[] fireworkLights = GetFireworkLights(fireworkType);
							Vector2[] fireworkPoints = GetFireworkPoints(fireworkType);
							List<TemporaryAnimatedSprite> list = new List<TemporaryAnimatedSprite>();
							Vector2[] array = fireworkLights;
							for (int i = 0; i < array.Length; i++)
							{
								Vector2 vector = array[i];
								list.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Rectangle(0, 0, 1, 1), 1800f * fireworkLifetimeMultiplier, 1, 0, position, flicker: false, flipped: false, -1f, 0f, Color.Transparent, 1f, 0f, 0f, 0f)
								{
									motion = vector,
									acceleration = vector * num3,
									accelerationChange = -vector / num4,
									stopAcceleratingWhenVelocityIsZero = true,
									afterAccelStopMotionX = (float)Math.Sign(vector.X) * 0.1f,
									afterAccelStopMotionY = 0.33f,
									layerDepthOffset = 320f,
									lightId = $"Firework_{id}_{vector.X}_{vector.Y}",
									lightRadius = 1.3f,
									drawAboveAlwaysFront = true,
									lightFade = 2
								});
							}
							array = fireworkPoints;
							for (int i = 0; i < array.Length; i++)
							{
								Vector2 vector2 = array[i];
								list.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Rectangle(304, 364 + fireworkType * 11, 11, 11), 75f * fireworkLifetimeMultiplier + (float)Game1.random.Next(-20, 21), 12, 1, position, flicker: false, flipped: false, -1f, 0f, Color.White, 4f, 0f, (float)(Game1.random.NextDouble() * Math.PI) * 0.5f, 0f)
								{
									motion = vector2,
									acceleration = vector2 * num3,
									accelerationChange = -vector2 / num4,
									stopAcceleratingWhenVelocityIsZero = true,
									afterAccelStopMotionX = (float)Math.Sign(vector2.X) * 0.1f,
									afterAccelStopMotionY = 0.33f,
									alpha = 1f,
									alphaFade = 0.01f,
									alphaFadeFade = 0.00025f,
									drawAboveAlwaysFront = true
								});
								int num5 = ((Game1.random.Next(3) != 0) ? 1 : 0);
								list.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 64 * (10 + num5), 64, 64), 100f * fireworkLifetimeMultiplier, (num5 == 0) ? 9 : 6, 2, position, flicker: false, flipped: false, -1f, 0f, Utility.getBlendedColor(fireworkColor, Color.White), 1f, 0f, (float)(Game1.random.NextDouble() * Math.PI) * 0.5f, 0f)
								{
									motion = vector2 * 0.75f,
									acceleration = vector2 * num3,
									accelerationChange = -vector2 / num4,
									stopAcceleratingWhenVelocityIsZero = true,
									afterAccelStopMotionX = (float)Math.Sign(vector2.X) * 0.1f,
									afterAccelStopMotionY = 0.33f,
									drawAboveAlwaysFront = true,
									alpha = 0.5f,
									delayBeforeAnimationStart = Game1.random.Next(50, 100)
								});
							}
							if (id == 30)
							{
								for (int j = 0; j < 8; j++)
								{
									Vector2 vector3 = fireworkPoints[Game1.random.Next(fireworkPoints.Length)];
									list.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Rectangle(304, 397, 11, 11), 75f * fireworkLifetimeMultiplier, 12, 5, position, flicker: false, flipped: false, -1f, 0f, Utility.getBlendedColor(Color.White, Utility.getRandomRainbowColor()), 4f, 0f, 0f, 0f)
									{
										motion = vector3 * 1.1f,
										alpha = 1f,
										alphaFade = 0.01f,
										acceleration = vector3 * num3,
										accelerationChange = -vector3 / ((float)id * 1.25f),
										stopAcceleratingWhenVelocityIsZero = true,
										afterAccelStopMotionX = (float)Math.Sign(vector3.X) * 0.1f,
										afterAccelStopMotionY = 0.33f,
										drawAboveAlwaysFront = true,
										lightId = $"Firework_{id}_{j}",
										lightRadius = 0.33f,
										lightFade = 3
									});
								}
							}
							Game1.multiplayer.broadcastSprites(parent, list.ToArray());
							parent.netAudio.StopPlaying("fuse");
						}
					}
					unload();
					return destroyable;
				}
				if (bombRadius > 0 && currentNumberOfLoops == totalNumberOfLoops - 5)
				{
					interval -= interval / 3f;
				}
			}
		}
		return false;
	}

	public bool clearOnAreaEntry()
	{
		if (dontClearOnAreaEntry)
		{
			return false;
		}
		if (bombRadius > 0)
		{
			return false;
		}
		return true;
	}

	private void setSourceRectToCurrentTileIndex()
	{
		sourceRect.X = (int)(sourceRectStartingPos.X + (float)(currentParentTileIndex * sourceRect.Width)) % texture.Width;
		if (sourceRect.X < 0)
		{
			sourceRect.X = 0;
		}
		sourceRect.Y = (int)sourceRectStartingPos.Y;
	}

	/// <summary>Play a sound locally, preferring the parent location if possible.</summary>
	/// <param name="sound">The sound to play.</param>
	private void PlaySound(string sound)
	{
		if (sound != null)
		{
			if (parent == null)
			{
				Game1.playSound(sound);
			}
			else
			{
				parent.localSound(sound);
			}
		}
	}

	public static TemporaryAnimatedSprite CreateFromData(TemporaryAnimatedSpriteDefinition temporarySprite, float x, float y, float sortLayer)
	{
		return new TemporaryAnimatedSprite(temporarySprite.Texture, temporarySprite.SourceRect, temporarySprite.Interval, temporarySprite.Frames, temporarySprite.Loops, new Vector2(x, y) * 64f + temporarySprite.PositionOffset * 4f, temporarySprite.Flicker, temporarySprite.Flip, sortLayer + temporarySprite.SortOffset, temporarySprite.AlphaFade, Utility.StringToColor(temporarySprite.Color) ?? Color.White, temporarySprite.Scale * 4f, temporarySprite.ScaleChange, temporarySprite.Rotation, temporarySprite.RotationChange);
	}
}
