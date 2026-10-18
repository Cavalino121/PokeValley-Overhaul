using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.FarmAnimals;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using xTile.Dimensions;

namespace StardewValley;

public class FarmAnimal : Character
{
	public const byte eatGrassBehavior = 0;

	public const short newHome = 0;

	public const short happy = 1;

	public const short neutral = 2;

	public const short unhappy = 3;

	public const short hungry = 4;

	public const short disturbedByDog = 5;

	public const short leftOutAtNight = 6;

	public const double chancePerUpdateToChangeDirection = 0.007;

	public const byte fullnessValueOfGrass = 60;

	public const int noWarpTimerTime = 3000;

	public new const double chanceForSound = 0.002;

	public const double chanceToGoOutside = 0.002;

	public const int uniqueDownFrame = 16;

	public const int uniqueRightFrame = 18;

	public const int uniqueUpFrame = 20;

	public const int uniqueLeftFrame = 22;

	public const int pushAccumulatorTimeTillPush = 60;

	public const int timePerUniqueFrame = 500;

	/// <summary>The texture name to load if the animal's actual sprite can't be loaded.</summary>
	public const string ErrorTextureName = "Animals\\Error";

	/// <summary>The pixel size of sprites in the <see cref="F:StardewValley.FarmAnimal.ErrorTextureName" />.</summary>
	public const int ErrorSpriteSize = 16;

	public NetBool isSwimming = new NetBool();

	[XmlIgnore]
	public Vector2 hopOffset = new Vector2(0f, 0f);

	[XmlElement("currentProduce")]
	public readonly NetString currentProduce = new NetString();

	[XmlElement("friendshipTowardFarmer")]
	public readonly NetInt friendshipTowardFarmer = new NetInt();

	[XmlElement("skinID")]
	public readonly NetString skinID = new NetString();

	[XmlIgnore]
	public int pushAccumulator;

	[XmlIgnore]
	public int uniqueFrameAccumulator = -1;

	[XmlElement("age")]
	public readonly NetInt age = new NetInt();

	[XmlElement("daysOwned")]
	public readonly NetInt daysOwned = new NetInt(-1);

	[XmlElement("health")]
	public readonly NetInt health = new NetInt();

	[XmlElement("produceQuality")]
	public readonly NetInt produceQuality = new NetInt();

	[XmlElement("daysSinceLastLay")]
	public readonly NetInt daysSinceLastLay = new NetInt();

	[XmlElement("happiness")]
	public readonly NetInt happiness = new NetInt();

	[XmlElement("fullness")]
	public readonly NetInt fullness = new NetInt();

	[XmlElement("wasAutoPet")]
	public readonly NetBool wasAutoPet = new NetBool();

	[XmlElement("wasPet")]
	public readonly NetBool wasPet = new NetBool();

	[XmlElement("allowReproduction")]
	public readonly NetBool allowReproduction = new NetBool(value: true);

	[XmlElement("type")]
	public readonly NetString type = new NetString();

	[XmlElement("buildingTypeILiveIn")]
	public readonly NetString buildingTypeILiveIn = new NetString();

	[XmlElement("myID")]
	public readonly NetLong myID = new NetLong();

	[XmlElement("ownerID")]
	public readonly NetLong ownerID = new NetLong();

	[XmlElement("parentId")]
	public readonly NetLong parentId = new NetLong(-1L);

	[XmlIgnore]
	private readonly NetLocationRef netHomeInterior = new NetLocationRef();

	[XmlElement("hasEatenAnimalCracker")]
	public readonly NetBool hasEatenAnimalCracker = new NetBool();

	[XmlIgnore]
	public int noWarpTimer;

	[XmlIgnore]
	public int hitGlowTimer;

	[XmlIgnore]
	public int pauseTimer;

	[XmlElement("moodMessage")]
	public readonly NetInt moodMessage = new NetInt();

	[XmlElement("isEating")]
	public readonly NetBool isEating = new NetBool();

	[XmlIgnore]
	private readonly NetEvent1Field<int, NetInt> doFarmerPushEvent = new NetEvent1Field<int, NetInt>();

	[XmlIgnore]
	private readonly NetEvent0 doBuildingPokeEvent = new NetEvent0();

	[XmlIgnore]
	private readonly NetEvent0 doDiveEvent = new NetEvent0();

	private string _displayHouse;

	private string _displayType;

	public static int NumPathfindingThisTick = 0;

	public static int MaxPathfindingPerTick = 1;

	[XmlIgnore]
	public int nextRipple;

	[XmlIgnore]
	public int nextFollowDirectionChange;

	protected FarmAnimal _followTarget;

	protected Point? _followTargetPosition;

	protected float _nextFollowTargetScan = 1f;

	[XmlIgnore]
	public int bobOffset;

	[XmlIgnore]
	protected Vector2 _swimmingVelocity = Vector2.Zero;

	[XmlIgnore]
	public static HashSet<Grass> reservedGrass = new HashSet<Grass>();

	[XmlIgnore]
	public Grass foundGrass;

	/// <summary>The building within which the animal is normally housed, if any.</summary>
	[XmlIgnore]
	public Building home
	{
		get
		{
			return netHomeInterior.Value?.ParentBuilding;
		}
		set
		{
			netHomeInterior.Value = value?.GetIndoors();
		}
	}

	[XmlIgnore]
	public GameLocation homeInterior
	{
		get
		{
			return netHomeInterior.Value;
		}
		set
		{
			netHomeInterior.Value = value;
		}
	}

	[XmlIgnore]
	public string displayHouse
	{
		get
		{
			if (_displayHouse == null)
			{
				FarmAnimalData animalData = GetAnimalData();
				if (animalData != null)
				{
					_displayHouse = (Game1.buildingData.TryGetValue(animalData.House, out var value) ? TokenParser.ParseText(value.Name) : animalData.House);
				}
				else
				{
					_displayHouse = buildingTypeILiveIn.Value;
				}
			}
			return _displayHouse;
		}
		set
		{
			_displayHouse = value;
		}
	}

	[XmlIgnore]
	public string displayType
	{
		get
		{
			if (_displayType == null)
			{
				_displayType = TokenParser.ParseText(GetAnimalData()?.DisplayName);
			}
			return _displayType;
		}
		set
		{
			_displayType = value;
		}
	}

	public override string displayName
	{
		get
		{
			return base.Name;
		}
		set
		{
		}
	}

	/// <summary>Get whether the farm animal is currently inside their home building.</summary>
	[MemberNotNullWhen(true, "home")]
	public bool IsHome
	{
		[MemberNotNullWhen(true, "home")]
		get
		{
			return homeInterior?.animals.ContainsKey(myID.Value) ?? false;
		}
	}

	public FarmAnimal()
	{
	}

	protected override void initNetFields()
	{
		bobOffset = Game1.random.Next(0, 1000);
		base.initNetFields();
		base.NetFields.AddField(currentProduce, "currentProduce").AddField(friendshipTowardFarmer, "friendshipTowardFarmer").AddField(age, "age")
			.AddField(health, "health")
			.AddField(produceQuality, "produceQuality")
			.AddField(daysSinceLastLay, "daysSinceLastLay")
			.AddField(happiness, "happiness")
			.AddField(fullness, "fullness")
			.AddField(wasPet, "wasPet")
			.AddField(wasAutoPet, "wasAutoPet")
			.AddField(allowReproduction, "allowReproduction")
			.AddField(type, "type")
			.AddField(buildingTypeILiveIn, "buildingTypeILiveIn")
			.AddField(myID, "myID")
			.AddField(ownerID, "ownerID")
			.AddField(parentId, "parentId")
			.AddField(netHomeInterior.NetFields, "netHomeInterior.NetFields")
			.AddField(moodMessage, "moodMessage")
			.AddField(isEating, "isEating")
			.AddField(doFarmerPushEvent, "doFarmerPushEvent")
			.AddField(doBuildingPokeEvent, "doBuildingPokeEvent")
			.AddField(isSwimming, "isSwimming")
			.AddField(doDiveEvent.NetFields, "doDiveEvent.NetFields")
			.AddField(daysOwned, "daysOwned")
			.AddField(skinID, "skinID")
			.AddField(hasEatenAnimalCracker, "hasEatenAnimalCracker");
		position.Field.AxisAlignedMovement = true;
		doFarmerPushEvent.onEvent += doFarmerPush;
		doBuildingPokeEvent.onEvent += doBuildingPoke;
		doDiveEvent.onEvent += doDive;
		skinID.fieldChangeVisibleEvent += delegate
		{
			if (Game1.gameMode != 6)
			{
				ReloadTextureIfNeeded();
			}
		};
		isSwimming.fieldChangeVisibleEvent += delegate
		{
			if (isSwimming.Value)
			{
				position.Field.AxisAlignedMovement = false;
			}
			else
			{
				position.Field.AxisAlignedMovement = true;
			}
		};
		name.FilterStringEvent += Utility.FilterDirtyWords;
	}

	public FarmAnimal(string type, long id, long ownerID)
		: base(null, new Vector2(64 * Game1.random.Next(2, 9), 64 * Game1.random.Next(4, 8)), 2, type)
	{
		this.ownerID.Value = ownerID;
		health.Value = 3;
		myID.Value = id;
		if (type == "Dairy Cow")
		{
			type = "Brown Cow";
		}
		this.type.Value = type;
		base.Name = Dialogue.randomName();
		displayName = name.Value;
		happiness.Value = 255;
		fullness.Value = 255;
		_nextFollowTargetScan = Utility.RandomFloat(1f, 3f);
		ReloadTextureIfNeeded(forceReload: true);
		FarmAnimalData animalData = GetAnimalData();
		if (animalData == null)
		{
			Game1.log.Warn("Constructed farm animal type '" + type + "' which has no entry in Data/FarmAnimals.");
		}
		buildingTypeILiveIn.Value = animalData?.House;
		if (animalData?.Skins == null)
		{
			return;
		}
		Random random = Utility.CreateRandom(id);
		float num = 1f;
		foreach (FarmAnimalSkin skin in animalData.Skins)
		{
			num += skin.Weight;
		}
		num = Utility.RandomFloat(0f, num, random);
		foreach (FarmAnimalSkin skin2 in animalData.Skins)
		{
			num -= skin2.Weight;
			if (num <= 0f)
			{
				skinID.Value = skin2.Id;
				break;
			}
		}
	}

	/// <summary>Get the animal's <see cref="P:StardewValley.Character.Sprite" /> value, loading it if needed.</summary>
	public AnimatedSprite GetOrLoadTexture()
	{
		AnimatedSprite animatedSprite = Sprite;
		if (animatedSprite == null)
		{
			ReloadTextureIfNeeded();
			animatedSprite = Sprite;
		}
		return animatedSprite;
	}

	/// <summary>Reload the texture if the asset name should change based on the current animal state and data.</summary>
	/// <param name="forceReload">Whether to reload the texture even if the texture path hasn't changed.</param>
	public void ReloadTextureIfNeeded(bool forceReload = false)
	{
		if (Sprite == null || forceReload)
		{
			FarmAnimalData animalData = GetAnimalData();
			string text;
			int spriteWidth;
			int spriteHeight;
			if (animalData != null)
			{
				text = GetTexturePath(animalData);
				spriteWidth = animalData.SpriteWidth;
				spriteHeight = animalData.SpriteHeight;
			}
			else
			{
				text = "Animals\\Error";
				spriteWidth = 16;
				spriteHeight = 16;
			}
			if (!Game1.content.DoesAssetExist<Texture2D>(text))
			{
				Game1.log.Warn($"Farm animal '{type.Value}' failed to load texture path '{text}': asset doesn't exist. Defaulting to error texture.");
				text = "Animals\\Error";
				spriteWidth = 16;
				spriteHeight = 16;
			}
			Sprite = new AnimatedSprite(text, 0, spriteWidth, spriteHeight)
			{
				textureUsesFlippedRightForLeft = (animalData?.UseFlippedRightForLeft ?? false)
			};
			ValidateSpritesheetSize();
		}
		else
		{
			string texturePath = GetTexturePath();
			if (Sprite.textureName.Value != texturePath)
			{
				Sprite.LoadTexture(texturePath);
			}
		}
	}

	public string GetTexturePath()
	{
		return GetTexturePath(GetAnimalData());
	}

	public virtual string GetTexturePath(FarmAnimalData data)
	{
		string result = "Animals\\" + type.Value;
		if (data != null)
		{
			FarmAnimalSkin farmAnimalSkin = null;
			if (skinID.Value != null && data.Skins != null)
			{
				foreach (FarmAnimalSkin skin in data.Skins)
				{
					if (skinID.Value == skin.Id)
					{
						farmAnimalSkin = skin;
						break;
					}
				}
			}
			if (farmAnimalSkin != null && farmAnimalSkin.Texture != null)
			{
				result = farmAnimalSkin.Texture;
			}
			else if (data.Texture != null)
			{
				result = data.Texture;
			}
			if (currentProduce.Value == null)
			{
				if (farmAnimalSkin != null && farmAnimalSkin.HarvestedTexture != null)
				{
					result = farmAnimalSkin.HarvestedTexture;
				}
				else if (data.HarvestedTexture != null)
				{
					result = data.HarvestedTexture;
				}
			}
			if (isBaby())
			{
				if (farmAnimalSkin != null && farmAnimalSkin.BabyTexture != null)
				{
					result = farmAnimalSkin.BabyTexture;
				}
				else if (data.BabyTexture != null)
				{
					result = data.BabyTexture;
				}
			}
		}
		return result;
	}

	public static FarmAnimalData GetAnimalDataFromEgg(Item eggItem, GameLocation location)
	{
		if (!TryGetAnimalDataFromEgg(eggItem, location, out var _, out var data))
		{
			return null;
		}
		return data;
	}

	public static bool TryGetAnimalDataFromEgg(Item eggItem, GameLocation location, out string id, out FarmAnimalData data)
	{
		if (!eggItem.HasTypeObject())
		{
			id = null;
			data = null;
			return false;
		}
		List<string> list = location?.ParentBuilding?.GetData()?.ValidOccupantTypes;
		foreach (KeyValuePair<string, FarmAnimalData> farmAnimalDatum in Game1.farmAnimalData)
		{
			FarmAnimalData value = farmAnimalDatum.Value;
			if (value.EggItemIds != null && value.EggItemIds.Count != 0 && (list == null || list.Contains(value.House)) && value.EggItemIds.Contains(eggItem.ItemId))
			{
				id = farmAnimalDatum.Key;
				data = value;
				return true;
			}
		}
		id = null;
		data = null;
		return false;
	}

	public virtual FarmAnimalData GetAnimalData()
	{
		if (!Game1.farmAnimalData.TryGetValue(type.Value, out var value))
		{
			return null;
		}
		return value;
	}

	/// <summary>Get the translated display name for a farm animal from its data, if any.</summary>
	/// <param name="id">The animal type ID in <c>Data/FarmAnimals</c>.</param>
	/// <param name="forShop">Whether to get the shop name, if applicable.</param>
	public static string GetDisplayName(string id, bool forShop = false)
	{
		if (!Game1.farmAnimalData.TryGetValue(id, out var value))
		{
			return null;
		}
		return TokenParser.ParseText(forShop ? (value.ShopDisplayName ?? value.DisplayName) : value.DisplayName);
	}

	/// <summary>Get the translated shop description for a farm animal from its data, if any.</summary>
	/// <param name="id">The animal type ID in <c>Data/FarmAnimals</c>.</param>
	public static string GetShopDescription(string id)
	{
		if (!Game1.farmAnimalData.TryGetValue(id, out var value))
		{
			return null;
		}
		return TokenParser.ParseText(value.ShopDescription);
	}

	public string shortDisplayType()
	{
		switch (LocalizedContentManager.CurrentLanguageCode)
		{
		case LocalizedContentManager.LanguageCode.en:
			return ArgUtility.SplitBySpace(displayType).Last();
		case LocalizedContentManager.LanguageCode.ja:
			if (!displayType.Contains("トリ"))
			{
				if (!displayType.Contains("ウシ"))
				{
					if (!displayType.Contains("ブタ"))
					{
						return displayType;
					}
					return "ブタ";
				}
				return "ウシ";
			}
			return "トリ";
		case LocalizedContentManager.LanguageCode.ru:
			if (!displayType.ContainsIgnoreCase("курица"))
			{
				if (!displayType.ContainsIgnoreCase("корова"))
				{
					return displayType;
				}
				return "Корова";
			}
			return "Курица";
		case LocalizedContentManager.LanguageCode.zh:
			if (!displayType.Contains('鸡'))
			{
				if (!displayType.Contains('牛'))
				{
					if (!displayType.Contains('猪'))
					{
						return displayType;
					}
					return "猪";
				}
				return "牛";
			}
			return "鸡";
		case LocalizedContentManager.LanguageCode.pt:
		case LocalizedContentManager.LanguageCode.es:
			return ArgUtility.SplitBySpaceAndGet(displayType, 0);
		case LocalizedContentManager.LanguageCode.de:
			return ArgUtility.SplitBySpace(displayType).Last().Split('-')
				.Last();
		default:
			return displayType;
		}
	}

	public Microsoft.Xna.Framework.Rectangle GetHarvestBoundingBox()
	{
		Vector2 vector = base.Position;
		return new Microsoft.Xna.Framework.Rectangle((int)(vector.X + (float)(Sprite.getWidth() * 4 / 2) - 32f + 4f), (int)(vector.Y + (float)(Sprite.getHeight() * 4) - 64f - 24f), 56, 72);
	}

	public Microsoft.Xna.Framework.Rectangle GetCursorPetBoundingBox()
	{
		Vector2 vector = base.Position;
		FarmAnimalData animalData = GetAnimalData();
		if (animalData != null)
		{
			int num;
			int num2;
			if (isBaby())
			{
				if (FacingDirection == 0 || FacingDirection == 2 || Sprite.currentFrame >= 12)
				{
					num = (int)(animalData.BabyUpDownPetHitboxTileSize.X * 64f);
					num2 = (int)(animalData.BabyUpDownPetHitboxTileSize.Y * 64f);
				}
				else
				{
					num = (int)(animalData.BabyLeftRightPetHitboxTileSize.X * 64f);
					num2 = (int)(animalData.BabyLeftRightPetHitboxTileSize.Y * 64f);
				}
			}
			else if (FacingDirection == 0 || FacingDirection == 2 || Sprite.currentFrame >= 12)
			{
				num = (int)(animalData.UpDownPetHitboxTileSize.X * 64f);
				num2 = (int)(animalData.UpDownPetHitboxTileSize.Y * 64f);
			}
			else
			{
				num = (int)(animalData.LeftRightPetHitboxTileSize.X * 64f);
				num2 = (int)(animalData.LeftRightPetHitboxTileSize.Y * 64f);
			}
			return new Microsoft.Xna.Framework.Rectangle((int)(base.Position.X + (float)(Sprite.getWidth() * 4 / 2) - (float)(num / 2)), (int)(base.Position.Y - 24f + (float)(Sprite.getHeight() * 4) - (float)num2), num, num2);
		}
		return new Microsoft.Xna.Framework.Rectangle((int)(vector.X + (float)(Sprite.getWidth() * 4 / 2) - 32f + 4f), (int)(vector.Y + (float)(Sprite.getHeight() * 4) - 64f - 24f), 56, 72);
	}

	public override Microsoft.Xna.Framework.Rectangle GetBoundingBox()
	{
		Vector2 vector = base.Position;
		return new Microsoft.Xna.Framework.Rectangle((int)(vector.X + (float)(Sprite.getWidth() * 4 / 2) - 32f + 8f), (int)(vector.Y + (float)(Sprite.getHeight() * 4) - 64f + 8f), 48, 48);
	}

	public void reload(GameLocation homeInterior)
	{
		this.homeInterior = homeInterior;
		ReloadTextureIfNeeded();
	}

	public void reload(Building home)
	{
		reload(home?.GetIndoors());
	}

	public int GetDaysOwned()
	{
		if (daysOwned.Value < 0)
		{
			daysOwned.Value = age.Value;
		}
		return daysOwned.Value;
	}

	public void pet(Farmer who, bool is_auto_pet = false)
	{
		if (!is_auto_pet)
		{
			if (who.FarmerSprite.PauseForSingleAnimation)
			{
				return;
			}
			who.Halt();
			who.faceGeneralDirection(base.Position, 0, opposite: false, useTileCalculations: false);
			if (Game1.timeOfDay >= 1900 && !isMoving())
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\FarmAnimals:TryingToSleep", displayName));
				return;
			}
			Halt();
			Sprite.StopAnimation();
			uniqueFrameAccumulator = -1;
			switch (Game1.player.FacingDirection)
			{
			case 0:
				Sprite.currentFrame = 0;
				break;
			case 1:
				Sprite.currentFrame = 12;
				break;
			case 2:
				Sprite.currentFrame = 8;
				break;
			case 3:
				Sprite.currentFrame = 4;
				break;
			}
			if (!hasEatenAnimalCracker.Value && who.ActiveObject?.QualifiedItemId == "(O)GoldenAnimalCracker")
			{
				if ((!(GetAnimalData()?.CanEatGoldenCrackers)) ?? false)
				{
					Game1.playSound("cancel");
					doEmote(8);
					return;
				}
				hasEatenAnimalCracker.Value = true;
				Game1.playSound("give_gift");
				doEmote(56);
				Game1.player.reduceActiveItemByOne();
				return;
			}
		}
		else if (wasAutoPet.Value)
		{
			return;
		}
		if (!wasPet.Value)
		{
			if (!is_auto_pet)
			{
				wasPet.Value = true;
			}
			int num = 7;
			if (wasAutoPet.Value)
			{
				friendshipTowardFarmer.Value = Math.Min(1000, friendshipTowardFarmer.Value + num);
			}
			else if (is_auto_pet)
			{
				friendshipTowardFarmer.Value = Math.Min(1000, friendshipTowardFarmer.Value + (15 - num));
			}
			else
			{
				friendshipTowardFarmer.Value = Math.Min(1000, friendshipTowardFarmer.Value + 15);
			}
			if (is_auto_pet)
			{
				wasAutoPet.Value = true;
			}
			FarmAnimalData animalData = GetAnimalData();
			int num2 = animalData?.HappinessDrain ?? 0;
			if (!is_auto_pet)
			{
				if (animalData != null && animalData.ProfessionForHappinessBoost >= 0 && who.professions.Contains(animalData.ProfessionForHappinessBoost))
				{
					friendshipTowardFarmer.Value = Math.Min(1000, friendshipTowardFarmer.Value + 15);
					happiness.Value = (byte)Math.Min(255, happiness.Value + Math.Max(5, 30 + num2));
				}
				int num3 = 20;
				if (wasAutoPet.Value)
				{
					num3 = 32;
				}
				doEmote((moodMessage.Value == 4) ? 12 : num3);
			}
			happiness.Value = (byte)Math.Min(255, happiness.Value + Math.Max(5, 30 + num2));
			if (!is_auto_pet)
			{
				makeSound();
				who.gainExperience(0, 5);
			}
		}
		else if (!is_auto_pet && who.ActiveObject?.QualifiedItemId != "(O)178")
		{
			Game1.activeClickableMenu = new AnimalQueryMenu(this);
		}
	}

	public void farmerPushing()
	{
		pushAccumulator++;
		if (pushAccumulator > 60)
		{
			doFarmerPushEvent.Fire(Game1.player.FacingDirection);
			Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
			boundingBox = Utility.ExpandRectangle(boundingBox, Utility.GetOppositeFacingDirection(Game1.player.FacingDirection), 6);
			Game1.player.TemporaryPassableTiles.Add(boundingBox);
			pushAccumulator = 0;
		}
	}

	public virtual void doDive()
	{
		yJumpVelocity = 8f;
		yJumpOffset = 1;
	}

	public void doFarmerPush(int direction)
	{
		if (Game1.IsMasterGame)
		{
			switch (direction)
			{
			case 0:
				Halt();
				break;
			case 1:
				Halt();
				break;
			case 2:
				Halt();
				break;
			case 3:
				Halt();
				break;
			}
		}
	}

	public void Poke()
	{
		doBuildingPokeEvent.Fire();
	}

	public void doBuildingPoke()
	{
		if (Game1.IsMasterGame)
		{
			FacingDirection = Game1.random.Next(4);
			setMovingInFacingDirection();
		}
	}

	public void setRandomPosition(GameLocation location)
	{
		StopAllActions();
		if (!location.TryGetMapPropertyAs("ProduceArea", out Microsoft.Xna.Framework.Rectangle parsed, required: true))
		{
			return;
		}
		base.Position = new Vector2(Game1.random.Next(parsed.X, parsed.Right) * 64, Game1.random.Next(parsed.Y, parsed.Bottom) * 64);
		int num = 0;
		while (base.Position.Equals(Vector2.Zero) || location.Objects.ContainsKey(base.Position) || location.isCollidingPosition(GetBoundingBox(), Game1.viewport, isFarmer: false, 0, glider: false, this))
		{
			base.Position = new Vector2(Game1.random.Next(parsed.X, parsed.Right), Game1.random.Next(parsed.Y, parsed.Bottom)) * 64f;
			num++;
			if (num > 64)
			{
				break;
			}
		}
		SleepIfNecessary();
	}

	public virtual void StopAllActions()
	{
		foundGrass = null;
		controller = null;
		isSwimming.Value = false;
		hopOffset = Vector2.Zero;
		_followTarget = null;
		_followTargetPosition = null;
		Halt();
		Sprite.StopAnimation();
		Sprite.UpdateSourceRect();
	}

	public virtual void HandleStatsOnProduceCollected(Item item, uint amount = 1u)
	{
		HandleStats(GetAnimalData()?.StatToIncrementOnProduce, item, amount);
	}

	public virtual void HandleStats(List<StatIncrement> stats, Item item, uint amount = 1u)
	{
		if (stats == null)
		{
			return;
		}
		foreach (StatIncrement stat in stats)
		{
			if (stat.RequiredItemId == null || ItemRegistry.HasItemId(item, stat.RequiredItemId))
			{
				List<string> requiredTags = stat.RequiredTags;
				if (requiredTags == null || requiredTags.Count <= 0 || ItemContextTagManager.DoAllTagsMatch(stat.RequiredTags, item.GetContextTags()))
				{
					Game1.stats.Increment(stat.StatName, amount);
				}
			}
		}
	}

	public string GetProduceID(Random r, bool deluxe = false)
	{
		FarmAnimalData animalData = GetAnimalData();
		if (animalData == null)
		{
			return null;
		}
		List<FarmAnimalProduce> list = new List<FarmAnimalProduce>();
		if (deluxe)
		{
			if (animalData.DeluxeProduceItemIds != null)
			{
				list.AddRange(animalData.DeluxeProduceItemIds);
			}
		}
		else if (animalData.ProduceItemIds != null)
		{
			list.AddRange(animalData.ProduceItemIds);
		}
		list.RemoveAll((FarmAnimalProduce produce) => (produce.MinimumFriendship > 0 && friendshipTowardFarmer.Value < produce.MinimumFriendship) || (produce.Condition != null && !GameStateQuery.CheckConditions(produce.Condition, base.currentLocation, null, null, null, r)));
		return r.ChooseFrom(list)?.ItemId;
	}

	/// <summary>Update the animal state when setting up the new day, before the game saves overnight.</summary>
	/// <param name="environment">The location containing the animal.</param>
	/// <remarks>See also <see cref="M:StardewValley.FarmAnimal.OnDayStarted" />, which happens after saving when the day has started.</remarks>
	public void dayUpdate(GameLocation environment)
	{
		if (daysOwned.Value < 0)
		{
			daysOwned.Value = age.Value;
		}
		FarmAnimalData animalData = GetAnimalData();
		int num = GetAnimalData()?.HappinessDrain ?? 0;
		int num2 = ((animalData != null && animalData.FriendshipForFasterProduce >= 0 && friendshipTowardFarmer.Value >= animalData.FriendshipForFasterProduce) ? 1 : 0);
		StopAllActions();
		health.Value = 3;
		bool flag = false;
		GameLocation gameLocation = homeInterior;
		if (gameLocation != null && !IsHome)
		{
			if (home.animalDoorOpen.Value)
			{
				environment.animals.Remove(myID.Value);
				gameLocation.animals.TryAdd(myID.Value, this);
				if (Game1.timeOfDay > 1800 && controller == null)
				{
					happiness.Value /= 2;
				}
				setRandomPosition(gameLocation);
				return;
			}
			moodMessage.Value = 6;
			flag = true;
			happiness.Value /= 2;
		}
		else if (gameLocation != null && IsHome && !home.animalDoorOpen.Value)
		{
			happiness.Value = (byte)Math.Min(255, happiness.Value + num * 2);
		}
		daysSinceLastLay.Value++;
		if (!wasPet.Value && !wasAutoPet.Value)
		{
			friendshipTowardFarmer.Value = Math.Max(0, friendshipTowardFarmer.Value - (10 - friendshipTowardFarmer.Value / 200));
			happiness.Value = (byte)Math.Max(0, happiness.Value - 50);
		}
		wasPet.Value = false;
		wasAutoPet.Value = false;
		daysOwned.Value++;
		if (fullness.Value < 200 && environment is AnimalHouse)
		{
			KeyValuePair<Vector2, Object>[] array = environment.objects.Pairs.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				KeyValuePair<Vector2, Object> keyValuePair = array[i];
				if (keyValuePair.Value.QualifiedItemId == "(O)178")
				{
					environment.objects.Remove(keyValuePair.Key);
					fullness.Value = 255;
					break;
				}
			}
		}
		Random random = Utility.CreateRandom((double)myID.Value / 2.0, Game1.stats.DaysPlayed);
		if (fullness.Value > 200 || random.NextDouble() < (double)(fullness.Value - 30) / 170.0)
		{
			if (age.Value == ((animalData != null) ? new int?(animalData.DaysToMature - 1) : ((int?)null)))
			{
				growFully(random);
			}
			else
			{
				age.Value++;
			}
			happiness.Value = (byte)Math.Min(255, happiness.Value + num * 2);
		}
		if (fullness.Value < 200)
		{
			happiness.Value = (byte)Math.Max(0, happiness.Value - 100);
			friendshipTowardFarmer.Value = Math.Max(0, friendshipTowardFarmer.Value - 20);
		}
		Farmer farmer = Game1.GetPlayer(ownerID.Value) ?? Game1.MasterPlayer;
		if (animalData != null && animalData.ProfessionForFasterProduce >= 0 && farmer.professions.Contains(animalData.ProfessionForFasterProduce))
		{
			num2++;
		}
		bool flag2 = daysSinceLastLay.Value >= ((animalData != null) ? new int?(animalData.DaysToProduce - num2) : ((int?)null)) && random.NextDouble() < (double)fullness.Value / 200.0 && random.NextDouble() < (double)happiness.Value / 70.0;
		string text;
		if (!flag2 || isBaby())
		{
			text = null;
		}
		else
		{
			text = GetProduceID(random);
			if (random.NextDouble() < (double)happiness.Value / 150.0)
			{
				float num3 = ((happiness.Value > 200) ? ((float)happiness.Value * 1.5f) : ((float)((happiness.Value <= 100) ? (happiness.Value - 100) : 0)));
				string produceID = GetProduceID(random, deluxe: true);
				if (animalData != null && animalData.DeluxeProduceCareDivisor >= 0f && produceID != null && friendshipTowardFarmer.Value >= animalData.DeluxeProduceMinimumFriendship && random.NextDouble() < (double)(((float)friendshipTowardFarmer.Value + num3) / animalData.DeluxeProduceCareDivisor) + Game1.player.team.AverageDailyLuck() * (double)animalData.DeluxeProduceLuckMultiplier)
				{
					text = produceID;
				}
				daysSinceLastLay.Value = 0;
				double num4 = (float)friendshipTowardFarmer.Value / 1000f - (1f - (float)happiness.Value / 225f);
				if (animalData != null && animalData.ProfessionForQualityBoost >= 0 && farmer.professions.Contains(animalData.ProfessionForQualityBoost))
				{
					num4 += 0.33;
				}
				if (num4 >= 0.95 && random.NextDouble() < num4 / 2.0)
				{
					produceQuality.Value = 4;
				}
				else if (random.NextDouble() < num4 / 2.0)
				{
					produceQuality.Value = 2;
				}
				else if (random.NextDouble() < num4)
				{
					produceQuality.Value = 1;
				}
				else
				{
					produceQuality.Value = 0;
				}
			}
		}
		if ((animalData == null || animalData.HarvestType != FarmAnimalHarvestType.DropOvernight) && flag2)
		{
			currentProduce.Value = text;
			text = null;
		}
		if (text != null && home != null)
		{
			bool flag3 = true;
			Object obj = ItemRegistry.Create<Object>("(O)" + text);
			obj.CanBeSetDown = false;
			obj.Quality = produceQuality.Value;
			if (hasEatenAnimalCracker.Value)
			{
				obj.Stack = 2;
			}
			HandleStats(animalData?.StatToIncrementOnProduce, obj, (uint)obj.Stack);
			foreach (Object value in gameLocation.objects.Values)
			{
				if (value.QualifiedItemId == "(BC)165" && value.heldObject.Value is Chest chest && chest.addItem(obj) == null)
				{
					value.showNextIndex.Value = true;
					flag3 = false;
					break;
				}
			}
			if (flag3)
			{
				obj.Stack = 1;
				Utility.spawnObjectAround(base.Tile, obj, environment);
				if (hasEatenAnimalCracker.Value)
				{
					Object o = (Object)obj.getOne();
					Utility.spawnObjectAround(base.Tile, o, environment);
				}
			}
		}
		if (!flag)
		{
			if (fullness.Value < 30)
			{
				moodMessage.Value = 4;
			}
			else if (happiness.Value < 30)
			{
				moodMessage.Value = 3;
			}
			else if (happiness.Value < 200)
			{
				moodMessage.Value = 2;
			}
			else
			{
				moodMessage.Value = 1;
			}
		}
		fullness.Value = 0;
		if (Utility.isFestivalDay())
		{
			fullness.Value = 250;
		}
		reload(homeInterior);
	}

	/// <summary>Handle the new day starting after the player saves, loads, or connects.</summary>
	/// <remarks>See also <see cref="M:StardewValley.FarmAnimal.dayUpdate(StardewValley.GameLocation)" />, which happens while setting up the day before saving.</remarks>
	public void OnDayStarted()
	{
		FarmAnimalData animalData = GetAnimalData();
		if (animalData != null && animalData.GrassEatAmount < 1)
		{
			fullness.Value = 255;
		}
	}

	public int getSellPrice()
	{
		int num = GetAnimalData()?.SellPrice ?? 0;
		double num2 = (double)friendshipTowardFarmer.Value / 1000.0 + 0.3;
		return (int)((double)num * num2);
	}

	public bool isMale()
	{
		return GetAnimalData()?.Gender switch
		{
			FarmAnimalGender.Female => false, 
			FarmAnimalGender.Male => true, 
			_ => myID.Value % 2 == 0, 
		};
	}

	public string getMoodMessage()
	{
		string text = (isMale() ? "Male" : "Female");
		switch (moodMessage.Value)
		{
		case 0:
			if (parentId.Value != -1)
			{
				return Game1.content.LoadString("Strings\\FarmAnimals:MoodMessage_NewHome_Baby_" + text, displayName);
			}
			return Game1.content.LoadString("Strings\\FarmAnimals:MoodMessage_NewHome_Adult_" + text + "_" + (Game1.dayOfMonth % 2 + 1), displayName);
		case 6:
			return Game1.content.LoadString("Strings\\FarmAnimals:MoodMessage_LeftOutsideAtNight_" + text, displayName);
		case 5:
			return Game1.content.LoadString("Strings\\FarmAnimals:MoodMessage_DisturbedByDog_" + text, displayName);
		case 4:
			return Game1.content.LoadString("Strings\\FarmAnimals:MoodMessage_" + (((Game1.dayOfMonth + myID.Value) % 2 == 0L) ? "Hungry1" : "Hungry2"), displayName);
		default:
			if (happiness.Value < 30)
			{
				moodMessage.Value = 3;
			}
			else if (happiness.Value < 200)
			{
				moodMessage.Value = 2;
			}
			else
			{
				moodMessage.Value = 1;
			}
			return moodMessage.Value switch
			{
				3 => Game1.content.LoadString("Strings\\FarmAnimals:MoodMessage_Sad", displayName), 
				2 => Game1.content.LoadString("Strings\\FarmAnimals:MoodMessage_Fine", displayName), 
				1 => Game1.content.LoadString("Strings\\FarmAnimals:MoodMessage_Happy", displayName), 
				_ => "", 
			};
		}
	}

	/// <summary>Get whether this farm animal is fully grown.</summary>
	/// <remarks>See also <see cref="M:StardewValley.FarmAnimal.isBaby" />.</remarks>
	public bool isAdult()
	{
		int? num = GetAnimalData()?.DaysToMature;
		if (num.HasValue)
		{
			return age.Value >= num;
		}
		return true;
	}

	/// <summary>Get whether this farm animal is a baby.</summary>
	/// <remarks>See also <see cref="M:StardewValley.FarmAnimal.isAdult" />.</remarks>
	public bool isBaby()
	{
		return age.Value < GetAnimalData()?.DaysToMature;
	}

	/// <summary>Get whether this farm animal's produce can be collected using a given tool.</summary>
	/// <param name="tool">The tool to check.</param>
	public bool CanGetProduceWithTool(Tool tool)
	{
		if (tool != null && tool.Name != null)
		{
			return GetAnimalData().HarvestTool == tool.Name;
		}
		return false;
	}

	/// <summary>Get the way in which the animal's produce is output.</summary>
	public FarmAnimalHarvestType? GetHarvestType()
	{
		return GetAnimalData()?.HarvestType;
	}

	/// <summary>Get whether this farm animal can live in a building.</summary>
	/// <param name="building">The building to check.</param>
	/// <remarks>This doesn't check whether there's room for it in the building; see <see cref="M:StardewValley.AnimalHouse.isFull" /> on <see cref="M:StardewValley.Buildings.Building.GetIndoors" /> for that.</remarks>
	public bool CanLiveIn(Building building)
	{
		BuildingData buildingData = building?.GetData();
		if (buildingData?.ValidOccupantTypes != null && buildingData.ValidOccupantTypes.Contains(buildingTypeILiveIn.Value) && !building.isUnderConstruction())
		{
			return building.GetIndoors() is AnimalHouse;
		}
		return false;
	}

	public void warpHome()
	{
		GameLocation gameLocation = homeInterior;
		if (gameLocation != null && gameLocation != base.currentLocation)
		{
			if (gameLocation.animals.TryAdd(myID.Value, this))
			{
				setRandomPosition(gameLocation);
				home.currentOccupants.Value++;
			}
			base.currentLocation?.animals.Remove(myID.Value);
			controller = null;
			isSwimming.Value = false;
			hopOffset = Vector2.Zero;
			_followTarget = null;
			_followTargetPosition = null;
		}
	}

	/// <summary>If the animal is a baby, instantly age it to adult.</summary>
	/// <param name="random">The RNG with which to select its produce, if applicable.</param>
	public void growFully(Random random = null)
	{
		FarmAnimalData animalData = GetAnimalData();
		if (age.Value <= animalData?.DaysToMature)
		{
			age.Value = animalData.DaysToMature;
			if (animalData.ProduceOnMature)
			{
				currentProduce.Value = GetProduceID(random ?? Game1.random);
			}
			daysSinceLastLay.Value = 99;
			ReloadTextureIfNeeded();
		}
	}

	public override void draw(SpriteBatch b)
	{
		Vector2 vector = new Vector2(0f, yJumpOffset);
		Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
		FarmAnimalData animalData = GetAnimalData();
		bool flag = IsActuallySwimming();
		bool flag2 = isBaby();
		FarmAnimalShadowData farmAnimalShadowData = animalData?.GetShadow(flag2, flag);
		if (farmAnimalShadowData == null || farmAnimalShadowData.Visible)
		{
			int valueOrDefault = (farmAnimalShadowData?.Offset?.X).GetValueOrDefault();
			int valueOrDefault2 = (farmAnimalShadowData?.Offset?.Y).GetValueOrDefault();
			if (flag)
			{
				float num = farmAnimalShadowData?.Scale ?? (flag2 ? 2.5f : 3.5f);
				Vector2 globalPosition = new Vector2(base.Position.X + (float)valueOrDefault, base.Position.Y - 24f + (float)valueOrDefault2);
				Sprite.drawShadow(b, Game1.GlobalToLocal(Game1.viewport, globalPosition), num, 0.5f);
				int num2 = (int)((Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 4.0 + (double)bobOffset) + 0.5) * 3.0);
				vector.Y += num2;
			}
			else
			{
				float num3 = farmAnimalShadowData?.Scale ?? (flag2 ? 3f : 4f);
				Vector2 globalPosition2 = new Vector2(base.Position.X + (float)valueOrDefault, base.Position.Y - 24f + (float)valueOrDefault2);
				Sprite.drawShadow(b, Game1.GlobalToLocal(Game1.viewport, globalPosition2), num3);
			}
		}
		vector.Y += yJumpOffset;
		float layerDepth = ((float)(boundingBox.Center.Y + 4) + base.Position.X / 20000f) / 10000f;
		Sprite.draw(b, Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, base.Position - new Vector2(0f, 24f) + vector)), layerDepth, 0, 0, (hitGlowTimer > 0) ? Color.Red : Color.White, FacingDirection == 3, 4f);
		if (isEmoting)
		{
			int num4 = Sprite.SpriteWidth / 2 * 4 - 32 + (animalData?.EmoteOffset.X ?? 0);
			int num5 = -64 + (animalData?.EmoteOffset.Y ?? 0);
			Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(base.Position.X + vector.X + (float)num4, base.Position.Y + vector.Y + (float)num5));
			b.Draw(Game1.emoteSpriteSheet, vector2, new Microsoft.Xna.Framework.Rectangle(base.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, base.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)boundingBox.Bottom / 10000f);
		}
	}

	public virtual void updateWhenNotCurrentLocation(Building currentBuilding, GameTime time, GameLocation environment)
	{
		doFarmerPushEvent.Poll();
		doBuildingPokeEvent.Poll();
		doDiveEvent.Poll();
		AnimatedSprite orLoadTexture = GetOrLoadTexture();
		if (!Game1.shouldTimePass())
		{
			return;
		}
		update(time, environment, myID.Value, move: false);
		if (!Game1.IsMasterGame)
		{
			return;
		}
		if (hopOffset != Vector2.Zero)
		{
			HandleHop();
			return;
		}
		if (currentBuilding != null && Game1.random.NextBool(0.002) && currentBuilding.animalDoorOpen.Value && Game1.timeOfDay < 1630 && !environment.IsRainingHere() && !environment.IsWinterHere() && !environment.farmers.Any())
		{
			GameLocation parentLocation = currentBuilding.GetParentLocation();
			Microsoft.Xna.Framework.Rectangle rectForAnimalDoor = currentBuilding.getRectForAnimalDoor();
			rectForAnimalDoor.Inflate(-2, -2);
			if (parentLocation.isCollidingPosition(rectForAnimalDoor, Game1.viewport, isFarmer: false, 0, glider: false, this, pathfinding: false) || parentLocation.isCollidingPosition(new Microsoft.Xna.Framework.Rectangle(rectForAnimalDoor.X, rectForAnimalDoor.Y + 64, rectForAnimalDoor.Width, rectForAnimalDoor.Height), Game1.viewport, isFarmer: false, 0, glider: false, this, pathfinding: false))
			{
				return;
			}
			parentLocation.animals.Remove(myID.Value);
			currentBuilding.GetIndoors().animals.Remove(myID.Value);
			parentLocation.animals.TryAdd(myID.Value, this);
			faceDirection(2);
			SetMovingDown(b: true);
			base.Position = new Vector2(rectForAnimalDoor.X, rectForAnimalDoor.Y - (orLoadTexture.getHeight() * 4 - GetBoundingBox().Height) + 32);
			if (NumPathfindingThisTick < MaxPathfindingPerTick)
			{
				NumPathfindingThisTick++;
				controller = new PathFindController(this, parentLocation, grassEndPointFunction, Game1.random.Next(4), behaviorAfterFindingGrassPatch, 200, Point.Zero);
			}
			if (controller?.pathToEndPoint == null || controller.pathToEndPoint.Count < 3)
			{
				SetMovingDown(b: true);
				controller = null;
			}
			else
			{
				faceDirection(2);
				base.Position = new Vector2(controller.pathToEndPoint.Peek().X * 64, controller.pathToEndPoint.Peek().Y * 64 - (orLoadTexture.getHeight() * 4 - GetBoundingBox().Height) + 16);
				if (orLoadTexture.SpriteWidth * 4 > 64)
				{
					position.X -= 32f;
				}
			}
			noWarpTimer = 3000;
			currentBuilding.currentOccupants.Value--;
			if (Utility.isOnScreen(base.TilePoint, 192, parentLocation))
			{
				parentLocation.localSound("sandyStep");
			}
			environment.isTileOccupiedByFarmer(base.Tile)?.TemporaryPassableTiles.Add(GetBoundingBox());
		}
		UpdateRandomMovements();
		behaviors(time, environment);
	}

	public static void behaviorAfterFindingGrassPatch(Character c, GameLocation environment)
	{
		if (environment.terrainFeatures.TryGetValue(c.Tile, out var value) && value is Grass item)
		{
			reservedGrass.Remove(item);
		}
		if (((FarmAnimal)c).fullness.Value < 255)
		{
			((FarmAnimal)c).eatGrass(environment);
		}
	}

	public static bool grassEndPointFunction(PathNode currentPoint, Point endPoint, GameLocation location, Character c)
	{
		Vector2 key = new Vector2(currentPoint.x, currentPoint.y);
		if (location.terrainFeatures.TryGetValue(key, out var value) && value is Grass item)
		{
			if (reservedGrass.Contains(value))
			{
				return false;
			}
			reservedGrass.Add(item);
			if (c is FarmAnimal farmAnimal)
			{
				farmAnimal.foundGrass = item;
			}
			return true;
		}
		return false;
	}

	public virtual void updatePerTenMinutes(int timeOfDay, GameLocation environment)
	{
		if (timeOfDay >= 1800)
		{
			int num = GetAnimalData()?.HappinessDrain ?? 0;
			int num2 = 0;
			if (environment.IsOutdoors)
			{
				num2 = ((timeOfDay > 1900 || environment.IsRainingHere() || environment.IsWinterHere()) ? (-num) : num);
			}
			else if (happiness.Value > 150 && environment.IsWinterHere())
			{
				num2 = ((environment.numberOfObjectsWithName("Heater") > 0) ? num : (-num));
			}
			if (num2 != 0)
			{
				happiness.Value = (byte)MathHelper.Clamp(happiness.Value + num2, 0, 255);
			}
		}
		environment.isTileOccupiedByFarmer(base.Tile)?.TemporaryPassableTiles.Add(GetBoundingBox());
	}

	public void eatGrass(GameLocation environment)
	{
		if (environment.terrainFeatures.TryGetValue(base.Tile, out var value) && value is Grass item)
		{
			reservedGrass.Remove(item);
			if (foundGrass != null)
			{
				reservedGrass.Remove(foundGrass);
			}
			foundGrass = null;
			Eat(environment);
		}
	}

	public virtual void Eat(GameLocation location)
	{
		Vector2 tile = base.Tile;
		isEating.Value = true;
		int num = 1;
		if (location.terrainFeatures.TryGetValue(tile, out var value) && value is Grass grass)
		{
			num = grass.grassType.Value;
			int number = GetAnimalData()?.GrassEatAmount ?? 2;
			if (grass.reduceBy(number, location.Equals(Game1.currentLocation)))
			{
				location.terrainFeatures.Remove(tile);
			}
		}
		Sprite.loop = false;
		fullness.Value = 255;
		if (moodMessage.Value != 5 && moodMessage.Value != 6 && !location.IsRainingHere())
		{
			happiness.Value = 255;
			friendshipTowardFarmer.Value = Math.Min(1000, friendshipTowardFarmer.Value + ((num == 7) ? 16 : 8));
		}
	}

	public virtual bool behaviors(GameTime time, GameLocation location)
	{
		if (!Game1.IsMasterGame)
		{
			return false;
		}
		Building building = home;
		if (building == null)
		{
			return false;
		}
		if (isBaby() && CanFollowAdult())
		{
			_nextFollowTargetScan -= (float)time.ElapsedGameTime.TotalSeconds;
			if (_nextFollowTargetScan < 0f)
			{
				_nextFollowTargetScan = Utility.RandomFloat(1f, 3f);
				if (controller != null || !location.IsOutdoors)
				{
					_followTarget = null;
					_followTargetPosition = null;
				}
				else
				{
					if (_followTarget != null)
					{
						if (!GetFollowRange(_followTarget).Contains(_followTargetPosition.Value))
						{
							GetNewFollowPosition();
						}
						return false;
					}
					if (location.IsOutdoors)
					{
						foreach (FarmAnimal value in location.animals.Values)
						{
							if (!value.isBaby() && value.type.Value == type.Value && GetFollowRange(value, 4).Contains(base.StandingPixel))
							{
								_followTarget = value;
								GetNewFollowPosition();
								return false;
							}
						}
					}
				}
			}
		}
		if (isEating.Value)
		{
			if (building != null && building.getRectForAnimalDoor().Intersects(GetBoundingBox()))
			{
				behaviorAfterFindingGrassPatch(this, location);
				isEating.Value = false;
				Halt();
				return false;
			}
			FarmAnimalData animalData = GetAnimalData();
			int num = 16;
			if (!Sprite.textureUsesFlippedRightForLeft)
			{
				num += 4;
			}
			if (animalData?.UseDoubleUniqueAnimationFrames ?? false)
			{
				num += 4;
			}
			if (Sprite.Animate(time, num, 4, 100f))
			{
				isEating.Value = false;
				Sprite.loop = true;
				Sprite.currentFrame = 0;
				faceDirection(2);
			}
			return true;
		}
		if (controller != null)
		{
			return true;
		}
		if (!isSwimming.Value && location.IsOutdoors && fullness.Value < 195 && Game1.random.NextDouble() < 0.002 && NumPathfindingThisTick < MaxPathfindingPerTick)
		{
			NumPathfindingThisTick++;
			controller = new PathFindController(this, location, grassEndPointFunction, -1, behaviorAfterFindingGrassPatch, 200, Point.Zero);
			_followTarget = null;
			_followTargetPosition = null;
		}
		if (Game1.timeOfDay >= 1700 && location.IsOutdoors && controller == null && Game1.random.NextDouble() < 0.002 && building.animalDoorOpen.Value)
		{
			if (!location.farmers.Any())
			{
				GameLocation indoors = building.GetIndoors();
				location.animals.Remove(myID.Value);
				indoors.animals.TryAdd(myID.Value, this);
				setRandomPosition(indoors);
				faceDirection(Game1.random.Next(4));
				controller = null;
				return true;
			}
			if (NumPathfindingThisTick < MaxPathfindingPerTick)
			{
				NumPathfindingThisTick++;
				controller = new PathFindController(this, location, PathFindController.isAtEndPoint, 0, null, 200, new Point(building.tileX.Value + building.animalDoor.X, building.tileY.Value + building.animalDoor.Y));
				_followTarget = null;
				_followTargetPosition = null;
			}
		}
		if (location.IsOutdoors && !location.IsRainingHere() && !location.IsWinterHere() && currentProduce.Value != null && isAdult() && GetHarvestType() == FarmAnimalHarvestType.DigUp && Game1.random.NextDouble() < 0.0002)
		{
			Object produce = ItemRegistry.Create<Object>(currentProduce.Value);
			Microsoft.Xna.Framework.Rectangle r = GetBoundingBox();
			for (int i = 0; i < 4; i++)
			{
				Vector2 cornersOfThisRectangle = Utility.getCornersOfThisRectangle(ref r, i);
				Vector2 key = new Vector2((int)(cornersOfThisRectangle.X / 64f), (int)(cornersOfThisRectangle.Y / 64f));
				if (location.terrainFeatures.ContainsKey(key) || location.objects.ContainsKey(key))
				{
					return false;
				}
			}
			if (Game1.player.currentLocation.Equals(location))
			{
				DelayedAction.playSoundAfterDelay("dirtyHit", 450);
				DelayedAction.playSoundAfterDelay("dirtyHit", 900);
				DelayedAction.playSoundAfterDelay("dirtyHit", 1350);
			}
			if (location.Equals(Game1.currentLocation))
			{
				switch (FacingDirection)
				{
				case 2:
					Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
					{
						new FarmerSprite.AnimationFrame(1, 250),
						new FarmerSprite.AnimationFrame(3, 250),
						new FarmerSprite.AnimationFrame(1, 250),
						new FarmerSprite.AnimationFrame(3, 250),
						new FarmerSprite.AnimationFrame(1, 250),
						new FarmerSprite.AnimationFrame(3, 250, secondaryArm: false, flip: false, delegate
						{
							DigUpProduce(location, produce);
						})
					});
					break;
				case 1:
					Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
					{
						new FarmerSprite.AnimationFrame(5, 250),
						new FarmerSprite.AnimationFrame(7, 250),
						new FarmerSprite.AnimationFrame(5, 250),
						new FarmerSprite.AnimationFrame(7, 250),
						new FarmerSprite.AnimationFrame(5, 250),
						new FarmerSprite.AnimationFrame(7, 250, secondaryArm: false, flip: false, delegate
						{
							DigUpProduce(location, produce);
						})
					});
					break;
				case 0:
					Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
					{
						new FarmerSprite.AnimationFrame(9, 250),
						new FarmerSprite.AnimationFrame(11, 250),
						new FarmerSprite.AnimationFrame(9, 250),
						new FarmerSprite.AnimationFrame(11, 250),
						new FarmerSprite.AnimationFrame(9, 250),
						new FarmerSprite.AnimationFrame(11, 250, secondaryArm: false, flip: false, delegate
						{
							DigUpProduce(location, produce);
						})
					});
					break;
				case 3:
					Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
					{
						new FarmerSprite.AnimationFrame(5, 250, secondaryArm: false, flip: true),
						new FarmerSprite.AnimationFrame(7, 250, secondaryArm: false, flip: true),
						new FarmerSprite.AnimationFrame(5, 250, secondaryArm: false, flip: true),
						new FarmerSprite.AnimationFrame(7, 250, secondaryArm: false, flip: true),
						new FarmerSprite.AnimationFrame(5, 250, secondaryArm: false, flip: true),
						new FarmerSprite.AnimationFrame(7, 250, secondaryArm: false, flip: true, delegate
						{
							DigUpProduce(location, produce);
						})
					});
					break;
				}
				Sprite.loop = false;
			}
			else
			{
				DigUpProduce(location, produce);
			}
		}
		return false;
	}

	public virtual void DigUpProduce(GameLocation location, Object produce)
	{
		Random random = Utility.CreateRandom((double)myID.Value / 2.0, Game1.stats.DaysPlayed, Game1.timeOfDay);
		bool flag = false;
		if (produce.QualifiedItemId == "(O)430" && random.NextDouble() < 0.002)
		{
			RockCrab rockCrab = new RockCrab(base.Tile, "Truffle Crab");
			Vector2 vector = Utility.recursiveFindOpenTileForCharacter(rockCrab, location, base.Tile, 50, allowOffMap: false);
			if (vector != Vector2.Zero)
			{
				rockCrab.setTileLocation(vector);
				location.addCharacter(rockCrab);
				flag = true;
			}
		}
		if (!flag && Utility.spawnObjectAround(Utility.getTranslatedVector2(base.Tile, FacingDirection, 1f), produce, base.currentLocation) && produce.QualifiedItemId == "(O)430")
		{
			Game1.stats.TrufflesFound++;
		}
		if (!random.NextBool((double)friendshipTowardFarmer.Value / 1500.0))
		{
			currentProduce.Value = null;
		}
	}

	public static Microsoft.Xna.Framework.Rectangle GetFollowRange(FarmAnimal animal, int distance = 2)
	{
		Point standingPixel = animal.StandingPixel;
		return new Microsoft.Xna.Framework.Rectangle(standingPixel.X - distance * 64, standingPixel.Y - distance * 64, distance * 64 * 2, 64 * distance * 2);
	}

	public virtual void GetNewFollowPosition()
	{
		if (_followTarget == null)
		{
			_followTargetPosition = null;
		}
		else if (_followTarget.isMoving() && _followTarget.IsActuallySwimming())
		{
			_followTargetPosition = Utility.Vector2ToPoint(Utility.getRandomPositionInThisRectangle(GetFollowRange(_followTarget, 1), Game1.random));
		}
		else
		{
			_followTargetPosition = Utility.Vector2ToPoint(Utility.getRandomPositionInThisRectangle(GetFollowRange(_followTarget), Game1.random));
		}
	}

	public void hitWithWeapon(MeleeWeapon t)
	{
	}

	public void makeSound()
	{
		if (base.currentLocation == Game1.currentLocation && !Game1.options.muteAnimalSounds)
		{
			string soundId = GetSoundId();
			if (soundId != null)
			{
				Game1.playSound(soundId, 1200 + Game1.random.Next(-200, 201));
			}
		}
	}

	/// <summary>Get the sound ID produced by the animal (e.g. when pet).</summary>
	public string GetSoundId()
	{
		FarmAnimalData animalData = GetAnimalData();
		if (!isBaby() || animalData == null || animalData.BabySound == null)
		{
			return animalData?.Sound;
		}
		return animalData.BabySound;
	}

	public virtual bool CanHavePregnancy()
	{
		return GetAnimalData()?.CanGetPregnant ?? false;
	}

	public virtual bool SleepIfNecessary()
	{
		if (Game1.timeOfDay >= 2000)
		{
			isSwimming.Value = false;
			hopOffset = Vector2.Zero;
			_followTarget = null;
			_followTargetPosition = null;
			if (isMoving())
			{
				Halt();
			}
			FarmAnimalData animalData = GetAnimalData();
			Sprite.currentFrame = animalData?.SleepFrame ?? 12;
			FacingDirection = 2;
			Sprite.UpdateSourceRect();
			return true;
		}
		return false;
	}

	public override bool isMoving()
	{
		if (_swimmingVelocity != Vector2.Zero)
		{
			return true;
		}
		if (!IsActuallySwimming() && uniqueFrameAccumulator != -1)
		{
			return false;
		}
		return base.isMoving();
	}

	public virtual bool updateWhenCurrentLocation(GameTime time, GameLocation location)
	{
		if (!Game1.shouldTimePass())
		{
			return false;
		}
		if (health.Value <= 0)
		{
			return true;
		}
		AnimatedSprite orLoadTexture = GetOrLoadTexture();
		doBuildingPokeEvent.Poll();
		doDiveEvent.Poll();
		if (IsActuallySwimming())
		{
			int num = 1;
			if (isMoving())
			{
				num = 4;
			}
			nextRipple -= (int)time.ElapsedGameTime.TotalMilliseconds * num;
			if (nextRipple <= 0)
			{
				nextRipple = 2000;
				float num2 = 1f;
				if (isBaby())
				{
					num2 = 0.65f;
				}
				Point standingPixel = base.StandingPixel;
				float num3 = base.Position.X - (float)standingPixel.X;
				TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), isMoving() ? 75f : 150f, 8, 0, new Vector2((float)standingPixel.X + num3 * num2, (float)standingPixel.Y - 32f * num2), flicker: false, Game1.random.NextBool(), 0.01f, 0.01f, Color.White * 0.75f, num2, 0f, 0f, 0f);
				Vector2 vector = Utility.PointToVector2(Utility.getTranslatedPoint(default(Point), FacingDirection, -1));
				temporaryAnimatedSprite.motion = vector * 0.25f;
				location.TemporarySprites.Add(temporaryAnimatedSprite);
			}
		}
		if (hitGlowTimer > 0)
		{
			hitGlowTimer -= time.ElapsedGameTime.Milliseconds;
		}
		if (orLoadTexture.CurrentAnimation != null)
		{
			if (orLoadTexture.animateOnce(time))
			{
				orLoadTexture.CurrentAnimation = null;
			}
			return false;
		}
		update(time, location, myID.Value, move: false);
		if (hopOffset != Vector2.Zero)
		{
			orLoadTexture.UpdateSourceRect();
			HandleHop();
			return false;
		}
		if (Game1.IsMasterGame && behaviors(time, location))
		{
			return false;
		}
		if (orLoadTexture.CurrentAnimation != null)
		{
			return false;
		}
		PathFindController pathFindController = controller;
		if (pathFindController != null && pathFindController.timerSinceLastCheckPoint > 10000)
		{
			controller = null;
			Halt();
		}
		if (Game1.IsMasterGame)
		{
			if (!IsHome && noWarpTimer <= 0)
			{
				GameLocation gameLocation = homeInterior;
				if (gameLocation != null)
				{
					Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
					if (home.getRectForAnimalDoor().Contains(boundingBox.Center.X, boundingBox.Top))
					{
						if (Utility.isOnScreen(base.TilePoint, 192, location))
						{
							location.localSound("dwoop");
						}
						location.animals.Remove(myID.Value);
						gameLocation.animals[myID.Value] = this;
						setRandomPosition(gameLocation);
						faceDirection(Game1.random.Next(4));
						controller = null;
						return true;
					}
				}
			}
			noWarpTimer = Math.Max(0, noWarpTimer - time.ElapsedGameTime.Milliseconds);
		}
		if (pauseTimer > 0)
		{
			pauseTimer -= time.ElapsedGameTime.Milliseconds;
		}
		if (SleepIfNecessary())
		{
			if (!isEmoting && Game1.random.NextDouble() < 0.002)
			{
				doEmote(24);
			}
		}
		else if (pauseTimer <= 0 && Game1.random.NextDouble() < 0.001 && isAdult() && Game1.gameMode == 3 && Utility.isOnScreen(base.Position, 192))
		{
			makeSound();
		}
		if (Game1.IsMasterGame)
		{
			UpdateRandomMovements();
			if (uniqueFrameAccumulator != -1 && _followTarget != null && !GetFollowRange(_followTarget, 1).Contains(base.StandingPixel))
			{
				uniqueFrameAccumulator = -1;
			}
			if (uniqueFrameAccumulator != -1)
			{
				uniqueFrameAccumulator += time.ElapsedGameTime.Milliseconds;
				if (uniqueFrameAccumulator > 500)
				{
					if (GetAnimalData()?.UseDoubleUniqueAnimationFrames ?? false)
					{
						orLoadTexture.currentFrame = orLoadTexture.currentFrame + 1 - orLoadTexture.currentFrame % 2 * 2;
					}
					else if (orLoadTexture.currentFrame > 12)
					{
						orLoadTexture.currentFrame = (orLoadTexture.currentFrame - 13) * 4;
					}
					else
					{
						switch (FacingDirection)
						{
						case 0:
							orLoadTexture.currentFrame = 15;
							break;
						case 1:
							orLoadTexture.currentFrame = 14;
							break;
						case 2:
							orLoadTexture.currentFrame = 13;
							break;
						case 3:
							orLoadTexture.currentFrame = 14;
							break;
						}
					}
					uniqueFrameAccumulator = 0;
					if (Game1.random.NextDouble() < 0.4)
					{
						uniqueFrameAccumulator = -1;
					}
				}
				if (IsActuallySwimming())
				{
					MovePosition(time, Game1.viewport, location);
				}
			}
			else
			{
				MovePosition(time, Game1.viewport, location);
			}
		}
		if (IsActuallySwimming())
		{
			FarmAnimalData animalData = GetAnimalData();
			orLoadTexture.UpdateSourceRect();
			Microsoft.Xna.Framework.Rectangle sourceRect = orLoadTexture.SourceRect;
			sourceRect.Offset(animalData?.SwimOffset ?? new Point(0, 112));
			orLoadTexture.SourceRect = sourceRect;
		}
		return false;
	}

	public virtual void UpdateRandomMovements()
	{
		if (!Game1.IsMasterGame || Game1.timeOfDay >= 2000 || pauseTimer > 0)
		{
			return;
		}
		if (fullness.Value < 255 && IsActuallySwimming() && Game1.random.NextDouble() < 0.002 && !isEating.Value)
		{
			Eat(base.currentLocation);
		}
		if (Game1.random.NextDouble() < 0.007 && uniqueFrameAccumulator == -1)
		{
			int num = Game1.random.Next(5);
			if (num != (FacingDirection + 2) % 4 || IsActuallySwimming())
			{
				if (num < 4)
				{
					int direction = FacingDirection;
					faceDirection(num);
					if (!base.currentLocation.isOutdoors.Value && base.currentLocation.isCollidingPosition(nextPosition(num), Game1.viewport, this))
					{
						faceDirection(direction);
						return;
					}
				}
				switch (num)
				{
				case 0:
					SetMovingUp(b: true);
					break;
				case 1:
					SetMovingRight(b: true);
					break;
				case 2:
					SetMovingDown(b: true);
					break;
				case 3:
					SetMovingLeft(b: true);
					break;
				default:
					Halt();
					Sprite.StopAnimation();
					break;
				}
			}
			else if (noWarpTimer <= 0)
			{
				Halt();
				Sprite.StopAnimation();
			}
		}
		if (!isMoving() || !(Game1.random.NextDouble() < 0.014) || uniqueFrameAccumulator != -1)
		{
			return;
		}
		Halt();
		Sprite.StopAnimation();
		if (Game1.random.NextDouble() < 0.75)
		{
			FarmAnimalData animalData = GetAnimalData();
			uniqueFrameAccumulator = 0;
			if (animalData?.UseDoubleUniqueAnimationFrames ?? false)
			{
				switch (FacingDirection)
				{
				case 0:
					Sprite.currentFrame = 20;
					break;
				case 1:
					Sprite.currentFrame = 18;
					break;
				case 2:
					Sprite.currentFrame = 16;
					break;
				case 3:
					Sprite.currentFrame = 22;
					break;
				}
			}
			else
			{
				switch (FacingDirection)
				{
				case 0:
					Sprite.currentFrame = 15;
					break;
				case 1:
					Sprite.currentFrame = 14;
					break;
				case 2:
					Sprite.currentFrame = 13;
					break;
				case 3:
					Sprite.currentFrame = ((animalData?.UseFlippedRightForLeft ?? false) ? 14 : 12);
					break;
				}
			}
			uniqueFrameAccumulator = 0;
		}
		Sprite.UpdateSourceRect();
	}

	public virtual bool CanSwim()
	{
		return GetAnimalData()?.CanSwim ?? false;
	}

	public virtual bool CanFollowAdult()
	{
		if (isBaby())
		{
			return GetAnimalData()?.BabiesFollowAdults ?? false;
		}
		return false;
	}

	public override bool shouldCollideWithBuildingLayer(GameLocation location)
	{
		return true;
	}

	public virtual void HandleHop()
	{
		int num = 4;
		if (hopOffset != Vector2.Zero)
		{
			if (hopOffset.X != 0f)
			{
				int num2 = (int)Math.Min(num, Math.Abs(hopOffset.X));
				base.Position += new Vector2(num2 * Math.Sign(hopOffset.X), 0f);
				hopOffset.X = Utility.MoveTowards(hopOffset.X, 0f, num2);
			}
			if (hopOffset.Y != 0f)
			{
				int num3 = (int)Math.Min(num, Math.Abs(hopOffset.Y));
				base.Position += new Vector2(0f, num3 * Math.Sign(hopOffset.Y));
				hopOffset.Y = Utility.MoveTowards(hopOffset.Y, 0f, num3);
			}
			if (hopOffset == Vector2.Zero && isSwimming.Value)
			{
				Splash();
				_swimmingVelocity = Utility.getTranslatedVector2(Vector2.Zero, FacingDirection, base.speed);
				base.Position = new Vector2((int)Math.Round(base.Position.X), (int)Math.Round(base.Position.Y));
			}
		}
	}

	public override void MovePosition(GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation currentLocation)
	{
		if (pauseTimer > 0 || Game1.IsClient)
		{
			return;
		}
		Location location = nextPositionTile();
		if (!currentLocation.isTileOnMap(new Vector2(location.X, location.Y)))
		{
			FacingDirection = Utility.GetOppositeFacingDirection(FacingDirection);
			moveUp = facingDirection.Value == 0;
			moveLeft = facingDirection.Value == 3;
			moveDown = facingDirection.Value == 2;
			moveRight = facingDirection.Value == 1;
			_followTarget = null;
			_followTargetPosition = null;
			_swimmingVelocity = Vector2.Zero;
			return;
		}
		if (_followTarget != null && (_followTarget.currentLocation != currentLocation || _followTarget.health.Value <= 0))
		{
			_followTarget = null;
			_followTargetPosition = null;
		}
		if (_followTargetPosition.HasValue)
		{
			Point standingPixel = base.StandingPixel;
			Point value = _followTargetPosition.Value;
			Point point = new Point(standingPixel.X - value.X, standingPixel.Y - value.Y);
			if (Math.Abs(point.X) <= 64 || Math.Abs(point.Y) <= 64)
			{
				moveDown = false;
				moveUp = false;
				moveLeft = false;
				moveRight = false;
				GetNewFollowPosition();
			}
			else if (nextFollowDirectionChange >= 0)
			{
				nextFollowDirectionChange -= (int)time.ElapsedGameTime.TotalMilliseconds;
			}
			else
			{
				if (IsActuallySwimming())
				{
					nextFollowDirectionChange = 100;
				}
				else
				{
					nextFollowDirectionChange = 500;
				}
				moveDown = false;
				moveUp = false;
				moveLeft = false;
				moveRight = false;
				if (Math.Abs(standingPixel.X - _followTargetPosition.Value.X) < Math.Abs(standingPixel.Y - _followTargetPosition.Value.Y))
				{
					if (standingPixel.Y > _followTargetPosition.Value.Y)
					{
						moveUp = true;
					}
					else if (standingPixel.Y < _followTargetPosition.Value.Y)
					{
						moveDown = true;
					}
				}
				else if (standingPixel.X < _followTargetPosition.Value.X)
				{
					moveRight = true;
				}
				else if (standingPixel.X > _followTargetPosition.Value.X)
				{
					moveLeft = true;
				}
			}
		}
		if (IsActuallySwimming())
		{
			Vector2 vector = default(Vector2);
			if (!isEating.Value)
			{
				if (moveUp)
				{
					vector.Y = -base.speed;
				}
				else if (moveDown)
				{
					vector.Y = base.speed;
				}
				if (moveLeft)
				{
					vector.X = -base.speed;
				}
				else if (moveRight)
				{
					vector.X = base.speed;
				}
			}
			_swimmingVelocity = new Vector2(Utility.MoveTowards(_swimmingVelocity.X, vector.X, 0.025f), Utility.MoveTowards(_swimmingVelocity.Y, vector.Y, 0.025f));
			Vector2 vector2 = base.Position;
			base.Position += _swimmingVelocity;
			Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
			base.Position = vector2;
			int num = -1;
			if (!currentLocation.isCollidingPosition(boundingBox, Game1.viewport, isFarmer: false, 0, glider: false, this, pathfinding: false))
			{
				base.Position += _swimmingVelocity;
				if (Math.Abs(_swimmingVelocity.X) > Math.Abs(_swimmingVelocity.Y))
				{
					if (_swimmingVelocity.X < 0f)
					{
						num = 3;
					}
					else if (_swimmingVelocity.X > 0f)
					{
						num = 1;
					}
				}
				else if (_swimmingVelocity.Y < 0f)
				{
					num = 0;
				}
				else if (_swimmingVelocity.Y > 0f)
				{
					num = 2;
				}
				switch (num)
				{
				case 0:
					Sprite.AnimateUp(time);
					faceDirection(0);
					break;
				case 3:
					Sprite.AnimateRight(time);
					FacingDirection = 3;
					break;
				case 1:
					Sprite.AnimateRight(time);
					faceDirection(1);
					break;
				case 2:
					Sprite.AnimateDown(time);
					faceDirection(2);
					break;
				}
			}
			else if (!HandleCollision(boundingBox))
			{
				Halt();
				Sprite.StopAnimation();
				_swimmingVelocity *= -1f;
			}
		}
		else if (moveUp)
		{
			if (!currentLocation.isCollidingPosition(nextPosition(0), Game1.viewport, isFarmer: false, 0, glider: false, this, pathfinding: false))
			{
				position.Y -= base.speed;
				Sprite.AnimateUp(time);
			}
			else if (!HandleCollision(nextPosition(0)))
			{
				Halt();
				Sprite.StopAnimation();
				if (Game1.random.NextDouble() < 0.6 || IsActuallySwimming())
				{
					SetMovingDown(b: true);
				}
			}
			faceDirection(0);
		}
		else if (moveRight)
		{
			if (!currentLocation.isCollidingPosition(nextPosition(1), Game1.viewport, isFarmer: false, 0, glider: false, this))
			{
				position.X += base.speed;
				Sprite.AnimateRight(time);
			}
			else if (!HandleCollision(nextPosition(1)))
			{
				Halt();
				Sprite.StopAnimation();
				if (Game1.random.NextDouble() < 0.6 || IsActuallySwimming())
				{
					SetMovingLeft(b: true);
				}
			}
			faceDirection(1);
		}
		else if (moveDown)
		{
			if (!currentLocation.isCollidingPosition(nextPosition(2), Game1.viewport, isFarmer: false, 0, glider: false, this))
			{
				position.Y += base.speed;
				Sprite.AnimateDown(time);
			}
			else if (!HandleCollision(nextPosition(2)))
			{
				Halt();
				Sprite.StopAnimation();
				if (Game1.random.NextDouble() < 0.6 || IsActuallySwimming())
				{
					SetMovingUp(b: true);
				}
			}
			faceDirection(2);
		}
		else
		{
			if (!moveLeft)
			{
				return;
			}
			if (!currentLocation.isCollidingPosition(nextPosition(3), Game1.viewport, isFarmer: false, 0, glider: false, this))
			{
				position.X -= base.speed;
				Sprite.AnimateRight(time);
			}
			else if (!HandleCollision(nextPosition(3)))
			{
				Halt();
				Sprite.StopAnimation();
				if (Game1.random.NextDouble() < 0.6 || IsActuallySwimming())
				{
					SetMovingRight(b: true);
				}
			}
			FacingDirection = 3;
		}
	}

	public virtual bool HandleCollision(Microsoft.Xna.Framework.Rectangle next_position)
	{
		if (_followTarget != null)
		{
			_followTarget = null;
			_followTargetPosition = null;
		}
		if (base.currentLocation.IsOutdoors && CanSwim() && (isSwimming.Value || controller == null) && wasPet.Value && hopOffset == Vector2.Zero)
		{
			base.Position = new Vector2((int)Math.Round(base.Position.X), (int)Math.Round(base.Position.Y));
			Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
			Vector2 translatedVector = Utility.getTranslatedVector2(Vector2.Zero, FacingDirection, 1f);
			if (translatedVector != Vector2.Zero)
			{
				Point tilePoint = base.TilePoint;
				tilePoint.X += (int)translatedVector.X;
				tilePoint.Y += (int)translatedVector.Y;
				translatedVector *= 128f;
				Microsoft.Xna.Framework.Rectangle rectangle = boundingBox;
				rectangle.Offset(Utility.Vector2ToPoint(translatedVector));
				Point point = new Point(rectangle.X / 64, rectangle.Y / 64);
				if (base.currentLocation.isWaterTile(tilePoint.X, tilePoint.Y) && base.currentLocation.doesTileHaveProperty(tilePoint.X, tilePoint.Y, "Passable", "Buildings") == null && !base.currentLocation.isCollidingPosition(rectangle, Game1.viewport, isFarmer: false, 0, glider: false, this) && base.currentLocation.isOpenWater(point.X, point.Y) != isSwimming.Value)
				{
					isSwimming.Value = !isSwimming.Value;
					if (!isSwimming.Value)
					{
						Splash();
					}
					hopOffset = translatedVector;
					pauseTimer = 0;
					doDiveEvent.Fire();
				}
				return true;
			}
		}
		return false;
	}

	public virtual bool IsActuallySwimming()
	{
		if (isSwimming.Value)
		{
			return hopOffset == Vector2.Zero;
		}
		return false;
	}

	public virtual void Splash()
	{
		if (Utility.isOnScreen(base.TilePoint, 192, base.currentLocation))
		{
			base.currentLocation.playSound("dropItemInWater");
		}
		Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(28, 100f, 2, 1, getStandingPosition() + new Vector2(-0.5f, -0.5f) * 64f, flicker: false, flipped: false)
		{
			delayBeforeAnimationStart = 0,
			layerDepth = (float)base.StandingPixel.Y / 10000f
		});
	}

	public override void animateInFacingDirection(GameTime time)
	{
		if (FacingDirection == 3)
		{
			Sprite.AnimateRight(time);
		}
		else
		{
			base.animateInFacingDirection(time);
		}
	}

	/// <summary>Log warnings if the farm animal's sprite is incorrectly sized, which would otherwise lead to hard-to-diagnose issues like animals freezing.</summary>
	private void ValidateSpritesheetSize()
	{
		int num = 5 + ((!Sprite.textureUsesFlippedRightForLeft) ? 1 : 0) + ((GetAnimalData()?.UseDoubleUniqueAnimationFrames ?? false) ? 1 : 0);
		if (Sprite.Texture.Height < num * Sprite.SpriteHeight)
		{
			Game1.log.Warn($"Farm animal '{type.Value}' has sprite height {Sprite.Texture.Height}px, but expected at least {num * Sprite.SpriteHeight}px based on its data. This may cause issues like frozen animations.");
		}
		if (Sprite.Texture.Width != 4 * Sprite.SpriteWidth)
		{
			Game1.log.Warn($"Farm animal '{type.Value}' has sprite width {Sprite.Texture.Width}px, but it should be exactly {4 * Sprite.SpriteWidth}px.");
		}
	}
}
