using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Audio;
using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Characters;
using StardewValley.Constants;
using StardewValley.Delegates;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Crops;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Fences;
using StardewValley.GameData.LocationContexts;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using StardewValley.GameData.WildTrees;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Network.NetEvents;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using xTile.Dimensions;

namespace StardewValley;

[XmlInclude(typeof(BreakableContainer))]
[XmlInclude(typeof(Cask))]
[XmlInclude(typeof(Chest))]
[XmlInclude(typeof(ColoredObject))]
[XmlInclude(typeof(CrabPot))]
[XmlInclude(typeof(Fence))]
[XmlInclude(typeof(Furniture))]
[XmlInclude(typeof(IndoorPot))]
[XmlInclude(typeof(ItemPedestal))]
[XmlInclude(typeof(Mannequin))]
[XmlInclude(typeof(MiniJukebox))]
[XmlInclude(typeof(Phone))]
[XmlInclude(typeof(Sign))]
[XmlInclude(typeof(Torch))]
[XmlInclude(typeof(Trinket))]
[XmlInclude(typeof(Wallpaper))]
[XmlInclude(typeof(WoodChipper))]
[XmlInclude(typeof(Workbench))]
public class Object : Item
{
	public enum PreserveType
	{
		Wine,
		Jelly,
		Pickle,
		Juice,
		Roe,
		AgedRoe,
		Honey,
		Bait,
		DriedFruit,
		DriedMushroom,
		SmokedFish
	}

	public const int wood = 388;

	public const int stone = 390;

	public const int copper = 378;

	public const int iron = 380;

	public const int coal = 382;

	public const int gold = 384;

	public const int iridium = 386;

	public const string artifactSpotID = "590";

	public const string hayID = "178";

	public const string iridiumBarID = "337";

	public const string woodID = "388";

	public const string stoneID = "390";

	public const string copperID = "378";

	public const string ironID = "380";

	public const string coalID = "382";

	public const string goldID = "384";

	public const string iridiumID = "386";

	public const string amethystClusterID = "66";

	public const string aquamarineID = "62";

	public const string bobberID = "133";

	public const string caveCarrotID = "78";

	public const string diamondID = "72";

	public const string emeraldID = "60";

	public const string prismaticShardID = "74";

	public const string quartzID = "80";

	public const string rubyID = "64";

	public const string sapphireID = "70";

	public const string stardropID = "434";

	public const string topazID = "68";

	public const string artifactSpotQID = "(O)590";

	public const string hayQID = "(O)178";

	public const string copperBarQID = "(O)334";

	public const string ironBarQID = "(O)335";

	public const string goldBarQID = "(O)336";

	public const string iridiumBarQID = "(O)337";

	public const string woodQID = "(O)388";

	public const string stoneQID = "(O)390";

	public const string copperQID = "(O)378";

	public const string ironQID = "(O)380";

	public const string coalQID = "(O)382";

	public const string goldQID = "(O)384";

	public const string iridiumQID = "(O)386";

	public const string amethystClusterQID = "(O)66";

	public const string aquamarineQID = "(O)62";

	public const string caveCarrotQID = "(O)78";

	public const string diamondQID = "(O)72";

	public const string emeraldQID = "(O)60";

	public const string prismaticShardQID = "(O)74";

	public const string rubyQID = "(O)64";

	public const string sapphireQID = "(O)70";

	public const string stardropQID = "(O)434";

	public const string topazQID = "(O)68";

	public const int inedible = -300;

	public const int GreensCategory = -81;

	public const int GemCategory = -2;

	public const int VegetableCategory = -75;

	public const int FishCategory = -4;

	public const int EggCategory = -5;

	public const int MilkCategory = -6;

	public const int CookingCategory = -7;

	public const int CraftingCategory = -8;

	public const int BigCraftableCategory = -9;

	public const int FruitsCategory = -79;

	public const int SeedsCategory = -74;

	public const int mineralsCategory = -12;

	public const int flowersCategory = -80;

	public const int meatCategory = -14;

	public const int metalResources = -15;

	public const int buildingResources = -16;

	public const int sellAtPierres = -17;

	public const int sellAtPierresAndMarnies = -18;

	public const int fertilizerCategory = -19;

	public const int junkCategory = -20;

	public const int baitCategory = -21;

	public const int tackleCategory = -22;

	public const int sellAtFishShopCategory = -23;

	public const int furnitureCategory = -24;

	public const int ingredientsCategory = -25;

	public const int artisanGoodsCategory = -26;

	public const int syrupCategory = -27;

	public const int monsterLootCategory = -28;

	public const int equipmentCategory = -29;

	public const int clothingCategorySortValue = -94;

	public const int hatCategory = -95;

	public const int ringCategory = -96;

	public const int weaponCategory = -98;

	public const int bootsCategory = -97;

	public const int toolCategory = -99;

	public const int clothingCategory = -100;

	public const int trinketCategory = -101;

	public const int booksCategory = -102;

	public const int skillBooksCategory = -103;

	/// <summary>The category for spawned twigs, weeds, and stones which spawn randomly in a location.</summary>
	public const int litterCategory = -999;

	public const int WildHorseradishIndex = 16;

	public const int LeekIndex = 20;

	public const int DandelionIndex = 22;

	public const int HandCursorIndex = 26;

	public const int WaterAnimationIndex = 28;

	public const int LumberIndex = 30;

	public const int mineStoneGrey1Index = 32;

	public const int mineStoneBlue1Index = 34;

	public const int mineStoneBlue2Index = 36;

	public const int mineStoneGrey2Index = 38;

	public const int mineStoneBrown1Index = 40;

	public const int mineStoneBrown2Index = 42;

	public const int mineStonePurpleIndex = 44;

	public const int mineStoneMysticIndex = 46;

	public const int mineStoneSnow1 = 48;

	public const int mineStoneSnow3 = 52;

	public const int mineStoneRed1Index = 56;

	public const int mineStoneRed2Index = 58;

	public const int emeraldIndex = 60;

	public const int aquamarineIndex = 62;

	public const int rubyIndex = 64;

	public const int amethystClusterIndex = 66;

	public const int topazIndex = 68;

	public const int sapphireIndex = 70;

	public const int diamondIndex = 72;

	public const int prismaticShardIndex = 74;

	public const int stardrop = 434;

	/// <summary>The <see cref="F:StardewValley.Object.preservedParentSheetIndex" /> value for Wild Honey.</summary>
	public const string WildHoneyPreservedId = "-1";

	public const int lowQuality = 0;

	public const int medQuality = 1;

	public const int highQuality = 2;

	public const int bestQuality = 4;

	public const int fragility_Removable = 0;

	public const int fragility_Delicate = 1;

	public const int fragility_Indestructable = 2;

	public const int spriteSheetTileSize = 16;

	public const float wobbleAmountWhenWorking = 10f;

	/// <summary>The suffix added to <see cref="P:StardewValley.Object.Name" /> if this is a recipe.</summary>
	public const string RecipeNameSuffix = " Recipe";

	/// <summary>The backing field for <see cref="P:StardewValley.Object.TileLocation" />.</summary>
	/// <remarks>When changing this value, most code should use <see cref="P:StardewValley.Object.TileLocation" /> instead so the bounding box is recalculated.</remarks>
	[XmlElement("tileLocation")]
	public readonly NetVector2 tileLocation = new NetVector2();

	[XmlElement("owner")]
	public readonly NetLong owner = new NetLong();

	[XmlElement("type")]
	public readonly NetString type = new NetString();

	[XmlElement("canBeSetDown")]
	public readonly NetBool canBeSetDown = new NetBool(value: false);

	[XmlElement("canBeGrabbed")]
	public readonly NetBool canBeGrabbed = new NetBool(value: true);

	[XmlElement("isSpawnedObject")]
	public readonly NetBool isSpawnedObject = new NetBool(value: false);

	[XmlElement("questItem")]
	public readonly NetBool questItem = new NetBool(value: false);

	[XmlElement("questId")]
	public readonly NetString questId = new NetString();

	[XmlElement("isOn")]
	public readonly NetBool isOn = new NetBool(value: true);

	[XmlElement("fragility")]
	public readonly NetInt fragility = new NetInt(0);

	[XmlElement("price")]
	public readonly NetInt price = new NetInt();

	[XmlElement("edibility")]
	public readonly NetInt edibility = new NetInt(-300);

	[XmlElement("bigCraftable")]
	public readonly NetBool bigCraftable = new NetBool();

	[XmlElement("setOutdoors")]
	public readonly NetBool setOutdoors = new NetBool();

	[XmlElement("setIndoors")]
	public readonly NetBool setIndoors = new NetBool();

	[XmlElement("readyForHarvest")]
	public readonly NetBool readyForHarvest = new NetBool();

	[XmlElement("showNextIndex")]
	public readonly NetBool showNextIndex = new NetBool();

	[XmlElement("flipped")]
	public readonly NetBool flipped = new NetBool();

	[XmlElement("isLamp")]
	public readonly NetBool isLamp = new NetBool();

	[XmlElement("heldObject")]
	public readonly NetRef<Object> heldObject = new NetRef<Object>();

	/// <summary>If this is a machine, the <see cref="F:StardewValley.GameData.Machines.MachineOutputRule.Id" /> value for the last rule which set the <see cref="F:StardewValley.Object.heldObject" /> value.</summary>
	[XmlElement("lastOutputRuleId")]
	public readonly NetString lastOutputRuleId = new NetString();

	/// <summary>If this is a machine, the last input item for which output was produced.</summary>
	[XmlElement("lastInputItem")]
	public readonly NetRef<Item> lastInputItem = new NetRef<Item>();

	[XmlElement("minutesUntilReady")]
	public readonly NetIntDelta minutesUntilReady = new NetIntDelta();

	[XmlElement("boundingBox")]
	public readonly NetRectangle boundingBox = new NetRectangle();

	public Vector2 scale;

	[XmlElement("uses")]
	public readonly NetInt uses = new NetInt();

	[XmlIgnore]
	private readonly NetRef<LightSource> netLightSource = new NetRef<LightSource>();

	/// <summary>The backing field <see cref="P:StardewValley.Object.displayNameFormat" />.</summary>
	[XmlIgnore]
	public readonly NetString netDisplayNameFormat = new NetString();

	[XmlIgnore]
	public bool isTemporarilyInvisible;

	[XmlIgnore]
	protected NetBool _destroyOvernight = new NetBool(value: false);

	[XmlIgnore]
	public bool shouldShowSign;

	/// <summary>If set, a custom buff to apply when the item is consumed, in addition to any buffs normally applied by the item.</summary>
	[XmlIgnore]
	public Func<Buff> customBuff;

	/// <summary>The raw net-synced text to show on hover if <see cref="M:StardewValley.Object.IsTextSign" /> is true, including any tokens and pre-filtered text.</summary>
	/// <remarks>Most code should use <see cref="P:StardewValley.Object.SignText" /> instead.</remarks>
	[XmlElement("signText")]
	public readonly NetString signText = new NetString();

	protected MachineEffects _machineAnimation;

	protected bool _machineAnimationLoop;

	protected int _machineAnimationIndex;

	protected int _machineAnimationFrame = -1;

	protected int _machineAnimationInterval;

	[XmlElement("orderData")]
	public readonly NetString orderData = new NetString();

	/// <summary>The inventory from which items are being auto-loaded, if any.</summary>
	/// <remarks>This is set during auto-loading, and unset immediately after the auto-load succeeds or fails.</remarks>
	[XmlIgnore]
	public static IInventory autoLoadFrom;

	[XmlIgnore]
	public int shakeTimer;

	[XmlIgnore]
	public int lastNoteBlockSoundTime;

	[XmlIgnore]
	public ICue internalSound;

	[XmlElement("preserve")]
	public readonly NetNullableEnum<PreserveType> preserve = new NetNullableEnum<PreserveType>();

	[XmlElement("preservedParentSheetIndex")]
	public readonly NetString preservedParentSheetIndex = new NetString();

	/// <summary>Obsolete. This is only kept to preserve data from old save files, and isn't synchronized in multiplayer. Use <see cref="F:StardewValley.Object.preservedParentSheetIndex" /> instead.</summary>
	[XmlElement("honeyType")]
	public string obsolete_honeyType;

	/// <summary>The cached value for <see cref="P:StardewValley.Object.DisplayName" />.</summary>
	[XmlIgnore]
	public string displayName;

	protected bool _hasHeldObject;

	protected bool _hasLightSource;

	public static int CurrentParsedItemCount;

	protected int health = 10;

	[XmlIgnore]
	public bool hovering;

	public bool destroyOvernight
	{
		get
		{
			return _destroyOvernight.Value;
		}
		set
		{
			_destroyOvernight.Value = value;
		}
	}

	[XmlIgnore]
	public LightSource lightSource
	{
		get
		{
			return netLightSource.Value;
		}
		set
		{
			netLightSource.Value = value;
		}
	}

	/// <summary>The location containing this object, if it's placed in the world.</summary>
	[XmlIgnore]
	public virtual GameLocation Location { get; set; }

	/// <summary>The item's tile location in the world.</summary>
	[XmlIgnore]
	public virtual Vector2 TileLocation
	{
		get
		{
			return tileLocation.Value;
		}
		set
		{
			if (tileLocation.Value != value)
			{
				tileLocation.Value = value;
				RecalculateBoundingBox();
			}
		}
	}

	[XmlIgnore]
	public string name
	{
		get
		{
			return netName.Value;
		}
		set
		{
			netName.Value = value;
		}
	}

	/// <summary>If set, a tokenizable string for the display name to use instead of the item data.</summary>
	/// <remarks>This shouldn't contain translation text directly, since it's synced in multiplayer. Instead use tokens like <c>[LOCALIZED_TEXT key]</c>, or <c>%DISPLAY_NAME</c>/<c>%DISPLAY_NAME_LOWERCASE</c> for the display name from data and <c>%PRESERVED_DISPLAY_NAME</c>/<c>%PRESERVED_DISPLAY_NAME_LOWERCASE</c> for the display name of the preserved item (if applicable).</remarks>
	[XmlElement("displayNameFormat")]
	public string displayNameFormat
	{
		get
		{
			return netDisplayNameFormat.Value;
		}
		set
		{
			netDisplayNameFormat.Value = value;
		}
	}

	/// <inheritdoc />
	public override string TypeDefinitionId
	{
		get
		{
			if (!bigCraftable.Value)
			{
				return "(O)";
			}
			return "(BC)";
		}
	}

	/// <inheritdoc />
	[XmlIgnore]
	public override string DisplayName
	{
		get
		{
			displayName = loadDisplayName();
			if (orderData.Value == "QI_COOKING")
			{
				displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:Fresh_Prefix", displayName);
			}
			if (isRecipe.Value)
			{
				string text = displayName;
				if (CraftingRecipe.craftingRecipes.TryGetValue(displayName, out var value))
				{
					string text2 = ArgUtility.SplitBySpaceAndGet(ArgUtility.Get(value.Split('/'), 2), 1);
					if (text2 != null)
					{
						text = text + " x" + text2;
					}
				}
				return text + Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12657");
			}
			return displayName;
		}
	}

	/// <inheritdoc />
	[XmlIgnore]
	public override string Name
	{
		get
		{
			if (!isRecipe.Value)
			{
				return name;
			}
			return name + " Recipe";
		}
		set
		{
			name = value;
		}
	}

	/// <inheritdoc />
	public override string BaseName => name;

	[XmlIgnore]
	public string Type
	{
		get
		{
			return type.Value;
		}
		set
		{
			type.Value = value;
		}
	}

	[XmlIgnore]
	public bool CanBeSetDown
	{
		get
		{
			return canBeSetDown.Value;
		}
		set
		{
			canBeSetDown.Value = value;
		}
	}

	[XmlIgnore]
	public bool CanBeGrabbed
	{
		get
		{
			return canBeGrabbed.Value;
		}
		set
		{
			canBeGrabbed.Value = value;
		}
	}

	[XmlIgnore]
	public bool IsOn
	{
		get
		{
			return isOn.Value;
		}
		set
		{
			isOn.Value = value;
		}
	}

	[XmlIgnore]
	public bool IsSpawnedObject
	{
		get
		{
			return isSpawnedObject.Value;
		}
		set
		{
			isSpawnedObject.Value = value;
		}
	}

	[XmlIgnore]
	public bool Flipped
	{
		get
		{
			return flipped.Value;
		}
		set
		{
			flipped.Value = value;
		}
	}

	[XmlIgnore]
	public int Price
	{
		get
		{
			return price.Value;
		}
		set
		{
			price.Value = value;
		}
	}

	[XmlIgnore]
	public int Edibility
	{
		get
		{
			return edibility.Value;
		}
		set
		{
			edibility.Value = value;
		}
	}

	[XmlIgnore]
	public int Fragility
	{
		get
		{
			return fragility.Value;
		}
		set
		{
			fragility.Value = value;
		}
	}

	[XmlIgnore]
	public Vector2 Scale
	{
		get
		{
			return scale;
		}
		set
		{
			scale = value;
		}
	}

	[XmlIgnore]
	public int MinutesUntilReady
	{
		get
		{
			return minutesUntilReady.Value;
		}
		set
		{
			minutesUntilReady.Value = value;
		}
	}

	/// <summary>The text to show on hover if <see cref="M:StardewValley.Object.IsTextSign" /> is true, formatted for the local player.</summary>
	/// <remarks>To set the text, see the underlying <see cref="F:StardewValley.Object.signText" /> field.</remarks>
	[XmlIgnore]
	public string SignText { get; private set; }

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(tileLocation, "tileLocation").AddField(owner, "owner").AddField(type, "type")
			.AddField(canBeSetDown, "canBeSetDown")
			.AddField(canBeGrabbed, "canBeGrabbed")
			.AddField(isSpawnedObject, "isSpawnedObject")
			.AddField(questItem, "questItem")
			.AddField(questId, "questId")
			.AddField(isOn, "isOn")
			.AddField(fragility, "fragility")
			.AddField(price, "price")
			.AddField(edibility, "edibility")
			.AddField(uses, "uses")
			.AddField(bigCraftable, "bigCraftable")
			.AddField(setOutdoors, "setOutdoors")
			.AddField(setIndoors, "setIndoors")
			.AddField(readyForHarvest, "readyForHarvest")
			.AddField(showNextIndex, "showNextIndex")
			.AddField(flipped, "flipped")
			.AddField(isLamp, "isLamp")
			.AddField(heldObject, "heldObject")
			.AddField(lastInputItem, "lastInputItem")
			.AddField(lastOutputRuleId, "lastOutputRuleId")
			.AddField(minutesUntilReady, "minutesUntilReady")
			.AddField(boundingBox, "boundingBox")
			.AddField(preserve, "preserve")
			.AddField(preservedParentSheetIndex, "preservedParentSheetIndex")
			.AddField(netDisplayNameFormat, "netDisplayNameFormat")
			.AddField(netLightSource, "netLightSource")
			.AddField(orderData, "orderData")
			.AddField(_destroyOvernight, "_destroyOvernight")
			.AddField(signText, "signText");
		heldObject.fieldChangeVisibleEvent += delegate
		{
			_hasHeldObject = heldObject.Value != null;
		};
		netLightSource.fieldChangeVisibleEvent += delegate
		{
			_hasLightSource = netLightSource.Value != null;
		};
		bigCraftable.fieldChangeVisibleEvent += delegate
		{
			_qualifiedItemId = null;
			MarkContextTagsDirty();
		};
		signText.fieldChangeVisibleEvent += delegate(NetString field, string oldValue, string newValue)
		{
			newValue = TokenParser.ParseText(newValue);
			SignText = Utility.FilterDirtyWords(newValue);
		};
		preserve.fieldChangeVisibleEvent += delegate
		{
			MarkContextTagsDirty();
		};
		preservedParentSheetIndex.fieldChangeVisibleEvent += delegate
		{
			MarkContextTagsDirty();
		};
	}

	/// <summary>Construct an item with default data.</summary>
	public Object()
	{
	}

	/// <summary>Construct a <see cref="F:StardewValley.ItemRegistry.type_bigCraftable" />-type item.</summary>
	public Object(Vector2 tileLocation, string itemId, bool isRecipe = false)
		: this()
	{
		itemId = ValidateUnqualifiedItemId(itemId);
		base.isRecipe.Value = isRecipe;
		base.ItemId = itemId;
		canBeSetDown.Value = true;
		bigCraftable.Value = true;
		if (Game1.bigCraftableData.TryGetValue(itemId, out var value))
		{
			name = value.Name ?? ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).InternalName;
			price.Value = value.Price;
			type.Value = "Crafting";
			base.Category = -9;
			setOutdoors.Value = value.CanBePlacedOutdoors;
			setIndoors.Value = value.CanBePlacedIndoors;
			fragility.Value = value.Fragility;
			isLamp.Value = value.IsLamp;
		}
		ResetParentSheetIndex();
		TileLocation = tileLocation;
		initializeLightSource(this.tileLocation.Value);
	}

	/// <summary>Construct a <see cref="F:StardewValley.ItemRegistry.type_object" />-type item.</summary>
	public Object(string itemId, int initialStack, bool isRecipe = false, int price = -1, int quality = 0)
		: this()
	{
		itemId = ValidateUnqualifiedItemId(itemId);
		stack.Value = initialStack;
		base.isRecipe.Value = isRecipe;
		base.quality.Value = quality;
		base.ItemId = itemId;
		ResetParentSheetIndex();
		if (Game1.objectData.TryGetValue(itemId, out var value))
		{
			name = value.Name ?? ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).InternalName;
			this.price.Value = value.Price;
			edibility.Value = value.Edibility;
			type.Value = value.Type;
			base.Category = value.Category;
		}
		if (price != -1)
		{
			this.price.Value = price;
		}
		canBeSetDown.Value = true;
		canBeGrabbed.Value = true;
		isSpawnedObject.Value = false;
		if (Game1.random.NextBool() && Utility.IsLegacyIdAbove(itemId, 52) && !Utility.IsLegacyIdBetween(itemId, 8, 15) && !Utility.IsLegacyIdBetween(itemId, 384, 391))
		{
			flipped.Value = true;
		}
		if (base.QualifiedItemId == "(O)463" || base.QualifiedItemId == "(O)464")
		{
			scale = new Vector2(1f, 1f);
		}
		if (itemId == "449" || IsWeeds() || IsTwig())
		{
			fragility.Value = 2;
		}
		else if (name.Contains("Fence"))
		{
			scale = new Vector2(10f, 0f);
		}
		else if (IsBreakableStone())
		{
			switch (itemId)
			{
			case "8":
				minutesUntilReady.Value = 4;
				break;
			case "10":
				minutesUntilReady.Value = 8;
				break;
			case "12":
				minutesUntilReady.Value = 16;
				break;
			case "14":
				minutesUntilReady.Value = 12;
				break;
			case "25":
				minutesUntilReady.Value = 8;
				break;
			default:
				minutesUntilReady.Value = 1;
				break;
			}
		}
		if (base.Category == -22)
		{
			scale.Y = 1f;
		}
	}

	/// <summary>Change the item's ID and parent sheet index without changing any other item data.</summary>
	/// <param name="spriteIndex">The new parent sheet index and item ID to set.</param>
	[Obsolete("This is used for specialized game behavior and only supports vanilla objects. New code should place a new object instance instead.")]
	public virtual void SetIdAndSprite(int spriteIndex)
	{
		base.ParentSheetIndex = spriteIndex;
		base.ItemId = spriteIndex.ToString();
	}

	/// <summary>Recalculate the item's bounding box based on its current position.</summary>
	/// <remarks>This is only needed in cases where the position was moved manually instead of using the <see cref="P:StardewValley.Object.TileLocation" /> property.</remarks>
	public virtual void RecalculateBoundingBox()
	{
		Vector2 vector = TileLocation;
		boundingBox.Value = new Microsoft.Xna.Framework.Rectangle((int)vector.X * 64, (int)vector.Y * 64, 64, 64);
	}

	public virtual bool IsHeldOverHead()
	{
		return true;
	}

	protected override void _PopulateContextTags(HashSet<string> tags)
	{
		base._PopulateContextTags(tags);
		if (orderData.Value == "QI_COOKING")
		{
			tags.Add("quality_qi");
		}
		if (preserve != null && preserve.Value.HasValue)
		{
			switch (preserve.Value)
			{
			case PreserveType.Honey:
				tags.Add("honey_item");
				break;
			case PreserveType.Jelly:
				tags.Add("jelly_item");
				break;
			case PreserveType.Juice:
				tags.Add("juice_item");
				break;
			case PreserveType.Wine:
				tags.Add("wine_item");
				break;
			case PreserveType.Pickle:
				tags.Add("pickle_item");
				break;
			}
		}
		if (preservedParentSheetIndex.Value != null)
		{
			tags.Add("preserve_sheet_index_" + ItemContextTagManager.SanitizeContextTag(preservedParentSheetIndex.Value));
		}
	}

	/// <summary>Get the translated display name for the item, excluding metadata like "(Recipe)".</summary>
	/// <remarks>Most code should use <see cref="P:StardewValley.Object.DisplayName" /> instead, which caches the value.</remarks>
	protected virtual string loadDisplayName()
	{
		return GetObjectDisplayName(base.QualifiedItemId, preserve.Value, preservedParentSheetIndex.Value, displayNameFormat);
	}

	/// <summary>Get the display name for an object including any preserved flavor, like "Blueberry Wine".</summary>
	/// <param name="itemId">The item ID for the flavored item, like <c>(O)348</c> for the wine in "Blueberry Wine".</param>
	/// <param name="preserveType">The preserve type.</param>
	/// <param name="preservedId">The item ID for the flavor item, like <c>(O)258</c> for the blueberry in "Blueberry Wine".</param>
	/// <param name="displayNameFormat">If set, a tokenizable string for the display name to use instead of the item data. See remarks on <see cref="P:StardewValley.Object.displayNameFormat" />.</param>
	/// <param name="defaultBaseName">If set, the base name to use if the <paramref name="itemId" /> doesn't match valid item data.</param>
	/// <returns>Returns the base name (if unflavored), or the flavored item name (if flavored), or a default error item name (if invalid).</returns>
	/// <remarks>This is a low-level method; most code should use <see cref="P:StardewValley.Object.DisplayName" /> instead. This only supports <see cref="F:StardewValley.ItemRegistry.type_object" /> items, not sub-types like <see cref="T:StardewValley.Objects.Furniture" />.</remarks>
	public static string GetObjectDisplayName(string itemId, PreserveType? preserveType, string preservedId, string displayNameFormat = null, string defaultBaseName = null)
	{
		string text = ((defaultBaseName == null) ? ItemRegistry.GetDataOrErrorItem(itemId).DisplayName : (ItemRegistry.GetData(itemId)?.DisplayName ?? defaultBaseName));
		string preservedItemId = GetPreservedItemId(preserveType, preservedId);
		ParsedItemData parsedItemData = ((preservedItemId != null) ? ItemRegistry.GetDataOrErrorItem(preservedItemId) : null);
		string text2 = parsedItemData?.DisplayName;
		string text3 = text2?.ToLowerInvariant();
		if (displayNameFormat != null)
		{
			string text4 = TokenParser.ParseText(displayNameFormat);
			if (text4.Contains('%'))
			{
				text4 = text4.Replace("%DISPLAY_NAME_LOWERCASE", text).Replace("%DISPLAY_NAME", text).Replace("%PRESERVED_DISPLAY_NAME_LOWERCASE", text3)
					.Replace("%PRESERVED_DISPLAY_NAME", text2);
			}
			return text4;
		}
		switch (preserveType)
		{
		case PreserveType.Honey:
			if (preservedId == "-1")
			{
				return Game1.content.LoadString("Strings\\Objects:Honey_Wild_Name");
			}
			if (text2 == null)
			{
				return text;
			}
			return Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:Honey_Flavored_" + parsedItemData?.QualifiedItemId + "_Name", text2, text3, localeFallback: false) ?? Game1.content.LoadString("Strings\\Objects:Honey_Flavored_Name", text2, text3);
		case PreserveType.Wine:
			return Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:Wine_Flavored_" + parsedItemData?.QualifiedItemId + "_Name", text2, text3, localeFallback: false) ?? Game1.content.LoadString("Strings\\Objects:Wine_Flavored_Name", text2, text3);
		case PreserveType.Jelly:
			return Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:Jelly_Flavored_" + parsedItemData?.QualifiedItemId + "_Name", text2, text3, localeFallback: false) ?? Game1.content.LoadString("Strings\\Objects:Jelly_Flavored_Name", text2, text3);
		case PreserveType.Pickle:
			return Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:Pickles_Flavored_" + parsedItemData?.QualifiedItemId + "_Name", text2, text3, localeFallback: false) ?? Game1.content.LoadString("Strings\\Objects:Pickles_Flavored_Name", text2, text3);
		case PreserveType.Juice:
			return Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:Juice_Flavored_" + parsedItemData?.QualifiedItemId + "_Name", text2, text3, localeFallback: false) ?? Game1.content.LoadString("Strings\\Objects:Juice_Flavored_Name", text2, text3);
		case PreserveType.Roe:
			return Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:Roe_Flavored_" + parsedItemData?.QualifiedItemId + "_Name", text2, text3, localeFallback: false) ?? Game1.content.LoadString("Strings\\Objects:Roe_Flavored_Name", text2?.TrimEnd('鱼'), text3?.TrimEnd('鱼'));
		case PreserveType.AgedRoe:
			if (preservedItemId != null)
			{
				return Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:AgedRoe_Flavored_" + parsedItemData?.QualifiedItemId + "_Name", text2, text3, localeFallback: false) ?? Game1.content.LoadString("Strings\\Objects:AgedRoe_Flavored_Name", text2?.TrimEnd('鱼'), text3?.TrimEnd('鱼'));
			}
			break;
		case PreserveType.Bait:
			return Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:SpecificBait_Flavored_" + parsedItemData?.QualifiedItemId + "_Name", text2, text3, localeFallback: false) ?? Game1.content.LoadString("Strings\\Objects:SpecificBait_Flavored_Name", text2, text3);
		case PreserveType.DriedFruit:
		case PreserveType.DriedMushroom:
			return Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:DriedFruit_Flavored_" + parsedItemData?.QualifiedItemId + "_Name", text2, text3, localeFallback: false) ?? Lexicon.makePlural(Game1.content.LoadString("Strings\\Objects:DriedFruit_Flavored_Name", text2, text3));
		case PreserveType.SmokedFish:
			return Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:SmokedFish_Flavored_" + parsedItemData?.QualifiedItemId + "_Name", text2, text3, localeFallback: false) ?? Game1.content.LoadString("Strings\\Objects:SmokedFish_Flavored_Name", text2, text3);
		}
		return text;
	}

	public Vector2 getLocalPosition(xTile.Dimensions.Rectangle viewport)
	{
		return new Vector2(tileLocation.X * 64f - (float)viewport.X, tileLocation.Y * 64f - (float)viewport.Y);
	}

	public static Microsoft.Xna.Framework.Rectangle getSourceRectForBigCraftable(int index)
	{
		return getSourceRectForBigCraftable(Game1.bigCraftableSpriteSheet, index);
	}

	public static Microsoft.Xna.Framework.Rectangle getSourceRectForBigCraftable(Texture2D texture, int index)
	{
		return new Microsoft.Xna.Framework.Rectangle(index % (texture.Width / 16) * 16, index * 16 / texture.Width * 16 * 2, 16, 32);
	}

	public virtual bool performToolAction(Tool t)
	{
		GameLocation location = Location;
		if (isTemporarilyInvisible)
		{
			return false;
		}
		if (base.QualifiedItemId == "(BC)165" && heldObject.Value is Chest chest && !chest.isEmpty())
		{
			chest.clearNulls();
			if (t != null && t.isHeavyHitter() && !(t is MeleeWeapon))
			{
				playNearbySoundAll("hammer");
				shakeTimer = 100;
			}
			return false;
		}
		if (t == null)
		{
			if (location.objects.TryGetValue(tileLocation.Value, out var value) && value.Equals(this))
			{
				if (location.farmers.Count > 0)
				{
					Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(4, 10), resource: false);
				}
				location.objects.Remove(tileLocation.Value);
			}
			return false;
		}
		if (IsBreakableStone() && t is Pickaxe)
		{
			int num = t.upgradeLevel.Value + 1;
			if ((base.QualifiedItemId == "(O)12" && t.upgradeLevel.Value == 1) || ((base.QualifiedItemId == "(O)12" || base.QualifiedItemId == "(O)14") && t.upgradeLevel.Value == 0))
			{
				num = 0;
				playNearbySoundAll("crafting");
			}
			MinutesUntilReady -= num;
			if (MinutesUntilReady <= 0)
			{
				return true;
			}
			playNearbySoundAll("hammer");
			shakeTimer = 100;
			return false;
		}
		if (IsBreakableStone() && t is Pickaxe)
		{
			return false;
		}
		if (name.Equals("Boulder") && (t.upgradeLevel.Value < 4 || !(t is Pickaxe)))
		{
			if (t.isHeavyHitter())
			{
				playNearbySoundAll("hammer");
			}
			return false;
		}
		if (IsWeeds() && t.isHeavyHitter())
		{
			int num2 = 1;
			if (t is MeleeWeapon && t.isScythe() && t.QualifiedItemId != "(W)47")
			{
				num2 = 2;
			}
			if (shakeTimer <= 0)
			{
				minutesUntilReady.Value -= num2;
			}
			if (minutesUntilReady.Value <= 0)
			{
				if (!(base.QualifiedItemId == "(O)319") && !(base.QualifiedItemId == "(O)320") && !(base.QualifiedItemId == "(O)321") && t.getLastFarmerToUse() != null)
				{
					foreach (BaseEnchantment enchantment in t.getLastFarmerToUse().enchantments)
					{
						enchantment.OnCutWeed(tileLocation.Value, location, t.getLastFarmerToUse());
					}
				}
				cutWeed(t.getLastFarmerToUse());
				return true;
			}
			if (shakeTimer <= 0)
			{
				Game1.playSound("weed_cut");
				shakeTimer = 200;
				return false;
			}
		}
		else
		{
			if (IsTwig() && t is Axe)
			{
				fragility.Value = 2;
				playNearbySoundAll("axchop");
				location.debris.Add(new Debris(ItemRegistry.Create("(O)388"), tileLocation.Value * 64f));
				Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(4, 10), resource: false);
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(12, new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f), Color.White, 8, Game1.random.NextBool(), 50f));
				t.getLastFarmerToUse().gainExperience(2, 1);
				return true;
			}
			if (name.Contains("SupplyCrate") && t.isHeavyHitter())
			{
				MinutesUntilReady -= t.upgradeLevel.Value + 1;
				if (MinutesUntilReady <= 0)
				{
					fragility.Value = 2;
					playNearbySoundAll("barrelBreak");
					Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)tileLocation.X * 777.0, (double)tileLocation.Y * 7.0);
					int houseUpgradeLevel = t.getLastFarmerToUse().HouseUpgradeLevel;
					int xTile = (int)tileLocation.X;
					int yTile = (int)tileLocation.Y;
					switch (houseUpgradeLevel)
					{
					case 0:
						switch (random.Next(7))
						{
						case 0:
							Game1.createMultipleObjectDebris("(O)770", xTile, yTile, random.Next(3, 6), location);
							break;
						case 1:
							Game1.createMultipleObjectDebris("(O)371", xTile, yTile, random.Next(5, 8), location);
							break;
						case 2:
							Game1.createMultipleObjectDebris("(O)535", xTile, yTile, random.Next(2, 5), location);
							break;
						case 3:
							Game1.createMultipleObjectDebris("(O)241", xTile, yTile, random.Next(1, 3), location);
							break;
						case 4:
							Game1.createMultipleObjectDebris("(O)395", xTile, yTile, random.Next(1, 3), location);
							break;
						case 5:
							Game1.createMultipleObjectDebris("(O)286", xTile, yTile, random.Next(3, 6), location);
							break;
						default:
							Game1.createMultipleObjectDebris("(O)286", xTile, yTile, random.Next(3, 6), location);
							break;
						}
						break;
					case 1:
						switch (random.Next(10))
						{
						case 0:
							Game1.createMultipleObjectDebris("(O)770", xTile, yTile, random.Next(3, 6), location);
							break;
						case 1:
							Game1.createMultipleObjectDebris("(O)371", xTile, yTile, random.Next(5, 8), location);
							break;
						case 2:
							Game1.createMultipleObjectDebris("(O)749", xTile, yTile, random.Next(2, 5), location);
							break;
						case 3:
							Game1.createMultipleObjectDebris("(O)253", xTile, yTile, random.Next(1, 3), location);
							break;
						case 4:
							Game1.createMultipleObjectDebris("(O)237", xTile, yTile, random.Next(1, 3), location);
							break;
						case 5:
							Game1.createMultipleObjectDebris("(O)246", xTile, yTile, random.Next(4, 8), location);
							break;
						case 6:
							Game1.createMultipleObjectDebris("(O)247", xTile, yTile, random.Next(2, 5), location);
							break;
						case 7:
							Game1.createMultipleObjectDebris("(O)245", xTile, yTile, random.Next(4, 8), location);
							break;
						case 8:
							Game1.createMultipleObjectDebris("(O)287", xTile, yTile, random.Next(3, 6), location);
							break;
						default:
							Game1.createMultipleObjectDebris("MixedFlowerSeeds", xTile, yTile, random.Next(4, 6), location);
							break;
						}
						break;
					default:
						switch (random.Next(9))
						{
						case 0:
							Game1.createMultipleObjectDebris("(O)770", xTile, yTile, random.Next(3, 6), location);
							break;
						case 1:
							Game1.createMultipleObjectDebris("(O)920", xTile, yTile, random.Next(5, 8), location);
							break;
						case 2:
							Game1.createMultipleObjectDebris("(O)749", xTile, yTile, random.Next(2, 5), location);
							break;
						case 3:
							Game1.createMultipleObjectDebris("(O)253", xTile, yTile, random.Next(2, 4), location);
							break;
						case 4:
							Game1.createMultipleObjectDebris(random.Choose("(O)904", "(O)905"), xTile, yTile, random.Next(1, 3), location);
							break;
						case 5:
							Game1.createMultipleObjectDebris("(O)246", xTile, yTile, random.Next(4, 8), location);
							Game1.createMultipleObjectDebris("(O)247", xTile, yTile, random.Next(2, 5), location);
							Game1.createMultipleObjectDebris("(O)245", xTile, yTile, random.Next(4, 8), location);
							break;
						case 6:
							Game1.createMultipleObjectDebris("(O)275", xTile, yTile, 2, location);
							break;
						case 7:
							Game1.createMultipleObjectDebris("(O)288", xTile, yTile, random.Next(3, 6), location);
							break;
						default:
							Game1.createMultipleObjectDebris("MixedFlowerSeeds", xTile, yTile, random.Next(5, 6), location);
							break;
						}
						break;
					}
					Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(4, 10), resource: false);
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(12, new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f), Color.White, 8, Game1.random.NextBool(), 50f));
					return true;
				}
				shakeTimer = 200;
				playNearbySoundAll("woodWhack");
				return false;
			}
		}
		if (base.QualifiedItemId == "(O)590" || base.QualifiedItemId == "(O)SeedSpot")
		{
			if (t is Hoe)
			{
				Random random2 = Utility.CreateDaySaveRandom((0f - tileLocation.X) * 7f, tileLocation.Y * 777f, Game1.netWorldState.Value.TreasureTotemsUsed * 777);
				t.getLastFarmerToUse().stats.Increment("ArtifactSpotsDug", 1);
				if (t.getLastFarmerToUse().stats.Get("ArtifactSpotsDug") > 2 && random2.NextDouble() < 0.008 + ((!t.getLastFarmerToUse().mailReceived.Contains("DefenseBookDropped")) ? ((double)t.getLastFarmerToUse().stats.Get("ArtifactSpotsDug") * 0.002) : 0.005))
				{
					t.getLastFarmerToUse().mailReceived.Add("DefenseBookDropped");
					Vector2 pixelOrigin = TileLocation * 64f;
					Game1.createMultipleItemDebris(ItemRegistry.Create("(O)Book_Defense"), pixelOrigin, Utility.GetOppositeFacingDirection(t.getLastFarmerToUse().FacingDirection), location);
				}
				if (base.QualifiedItemId == "(O)SeedSpot")
				{
					Item raccoonSeedForCurrentTimeOfYear = Utility.getRaccoonSeedForCurrentTimeOfYear(t.getLastFarmerToUse(), random2);
					Vector2 pixelOrigin2 = TileLocation * 64f;
					Game1.createMultipleItemDebris(raccoonSeedForCurrentTimeOfYear, pixelOrigin2, Utility.GetOppositeFacingDirection(t.getLastFarmerToUse().FacingDirection), location);
				}
				else
				{
					location.digUpArtifactSpot((int)tileLocation.X, (int)tileLocation.Y, t.getLastFarmerToUse());
				}
				location.makeHoeDirt(tileLocation.Value, ignoreChecks: true);
				playNearbySoundAll("hoeHit");
				t.getLastFarmerToUse().gainExperience(2, 15);
				location.objects.Remove(tileLocation.Value);
			}
			return false;
		}
		if (bigCraftable.Value && !(t is MeleeWeapon) && t.isHeavyHitter() && ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).IsErrorItem)
		{
			playNearbySoundAll("hammer");
			performRemoveAction();
			location.objects.Remove(tileLocation.Value);
			return false;
		}
		if (fragility.Value == 2)
		{
			return false;
		}
		if (Type == "Crafting" && !(t is MeleeWeapon) && t.isHeavyHitter())
		{
			if (t is Hoe && IsSprinkler())
			{
				return false;
			}
			playNearbySoundAll("hammer");
			if (fragility.Value == 1)
			{
				Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(3, 6), resource: false);
				Game1.createRadialDebris(location, 14, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(3, 6), resource: false);
				DelayedAction.functionAfterDelay(delegate
				{
					Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(2, 5), resource: false);
					Game1.createRadialDebris(location, 14, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(2, 5), resource: false);
				}, 80);
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(12, new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f), Color.White, 8, Game1.random.NextBool(), 50f));
				performRemoveAction();
				location.objects.Remove(tileLocation.Value);
				return false;
			}
			if (IsTapper() && location.terrainFeatures.TryGetValue(tileLocation.Value, out var value2) && value2 is Tree tree)
			{
				tree.tapped.Value = false;
			}
			string qualifiedItemId = base.QualifiedItemId;
			if (!(qualifiedItemId == "(BC)254"))
			{
				if (qualifiedItemId == "(BC)21" && heldObject.Value != null)
				{
					location.debris.Add(new Debris(heldObject.Value, tileLocation.Value * 64f + new Vector2(32f, 32f)));
					heldObject.Value = null;
				}
				if (IsSprinkler() && heldObject.Value != null)
				{
					if (heldObject.Value.heldObject.Value != null)
					{
						Object value3 = heldObject.Value.heldObject.Value;
						Chest chest2 = value3 as Chest;
						if (chest2 != null)
						{
							chest2.GetMutex().RequestLock(delegate
							{
								List<Item> list = new List<Item>(chest2.Items);
								chest2.Items.Clear();
								foreach (Item item in list)
								{
									if (item != null)
									{
										location.debris.Add(new Debris(item, tileLocation.Value * 64f + new Vector2(32f, 32f)));
									}
								}
								Object value4 = heldObject.Value;
								heldObject.Value = null;
								location.debris.Add(new Debris(value4, tileLocation.Value * 64f + new Vector2(32f, 32f)));
								chest2.GetMutex().ReleaseLock();
							});
						}
						return false;
					}
					location.debris.Add(new Debris(heldObject.Value, tileLocation.Value * 64f + new Vector2(32f, 32f)));
					heldObject.Value = null;
					return false;
				}
				if (IsSprinkler() && base.SpecialVariable == 999999)
				{
					location.debris.Add(new Debris(ItemRegistry.Create("(O)93"), tileLocation.Value * 64f + new Vector2(32f, 32f)));
				}
				if (heldObject.Value != null && readyForHarvest.Value)
				{
					location.debris.Add(new Debris(heldObject.Value, tileLocation.Value * 64f + new Vector2(32f, 32f)));
				}
				if (base.QualifiedItemId == "(BC)156")
				{
					ResetParentSheetIndex();
					heldObject.Value = null;
					minutesUntilReady.Value = -1;
				}
				if (name.Contains("Seasonal"))
				{
					base.ParentSheetIndex -= base.ParentSheetIndex % 4;
				}
				return true;
			}
			if (heldObject.Value != null)
			{
				ResetParentSheetIndex();
				location.debris.Add(new Debris(heldObject.Value, tileLocation.Value * 64f + new Vector2(32f, 32f)));
				heldObject.Value = null;
			}
			return true;
		}
		return false;
	}

	public virtual void cutWeed(Farmer who)
	{
		GameLocation location = Location;
		Color color = Color.Green;
		string text = "cut";
		int rowInAnimationTexture = 50;
		fragility.Value = 2;
		string text2 = null;
		if (Game1.random.NextBool())
		{
			text2 = "771";
		}
		else if (Game1.random.NextDouble() < 0.05 + ((who.stats.Get("Book_WildSeeds") != 0) ? 0.04 : 0.0))
		{
			text2 = "770";
		}
		else if (Game1.currentSeason == "summer" && Game1.random.NextDouble() < 0.05 + ((who.stats.Get("Book_WildSeeds") != 0) ? 0.04 : 0.0))
		{
			text2 = "MixedFlowerSeeds";
		}
		if (name.Contains("GreenRainWeeds") && Game1.random.NextDouble() < 0.1)
		{
			text2 = "Moss";
		}
		switch (base.QualifiedItemId)
		{
		case "(O)678":
			color = new Color(228, 109, 159);
			break;
		case "(O)679":
			color = new Color(253, 191, 46);
			break;
		case "(O)313":
		case "(O)314":
		case "(O)315":
			color = new Color(84, 101, 27);
			break;
		case "(O)318":
		case "(O)316":
		case "(O)317":
			color = new Color(109, 49, 196);
			break;
		case "(O)319":
			color = new Color(30, 216, 255);
			text = "breakingGlass";
			rowInAnimationTexture = 47;
			playNearbySoundAll("drumkit2");
			text2 = null;
			break;
		case "(O)320":
			color = new Color(175, 143, 255);
			text = "breakingGlass";
			rowInAnimationTexture = 47;
			playNearbySoundAll("drumkit2");
			text2 = null;
			break;
		case "(O)321":
			color = new Color(73, 255, 158);
			text = "breakingGlass";
			rowInAnimationTexture = 47;
			playNearbySoundAll("drumkit2");
			text2 = null;
			break;
		case "(O)793":
		case "(O)794":
		case "(O)792":
			text2 = "770";
			break;
		case "(O)883":
		case "(O)884":
		case "(O)882":
			color = new Color(30, 97, 68);
			if (Game1.MasterPlayer.hasOrWillReceiveMail("islandNorthCaveOpened") && Game1.random.NextDouble() < 0.1 && !Game1.MasterPlayer.hasOrWillReceiveMail("gotMummifiedFrog"))
			{
				Game1.addMailForTomorrow("gotMummifiedFrog", noLetter: true, sendToEveryone: true);
				text2 = "828";
			}
			else if (Game1.random.NextDouble() < 0.01)
			{
				text2 = "828";
			}
			else if (Game1.random.NextDouble() < 0.08)
			{
				text2 = "831";
			}
			break;
		case "GreenRainWeeds0":
		case "GreenRainWeeds1":
		case "GreenRainWeeds4":
			text = "weed_cut";
			break;
		}
		if (text.Equals("breakingGlass") && Game1.random.NextDouble() < 0.0025)
		{
			text2 = "338";
		}
		playNearbySoundAll(text);
		Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(rowInAnimationTexture, tileLocation.Value * 64f, color));
		Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(rowInAnimationTexture, tileLocation.Value * 64f + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-48, 48)), color * 0.75f)
		{
			scale = 0.75f,
			flipped = true
		});
		Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(rowInAnimationTexture, tileLocation.Value * 64f + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-48, 48)), color * 0.75f)
		{
			scale = 0.75f,
			delayBeforeAnimationStart = 50
		});
		Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(rowInAnimationTexture, tileLocation.Value * 64f + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-48, 48)), color * 0.75f)
		{
			scale = 0.75f,
			flipped = true,
			delayBeforeAnimationStart = 100
		});
		if (!text.Equals("breakingGlass"))
		{
			if (Game1.random.NextDouble() < 1E-05)
			{
				location.debris.Add(new Debris(ItemRegistry.Create("(H)40"), tileLocation.Value * 64f + new Vector2(32f, 32f)));
			}
			if (Game1.random.NextDouble() <= 0.01 && Game1.player.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
			{
				location.debris.Add(new Debris(ItemRegistry.Create("(O)890"), tileLocation.Value * 64f + new Vector2(32f, 32f)));
			}
		}
		if (text2 != null)
		{
			location.debris.Add(new Debris(new Object(text2, 1), tileLocation.Value * 64f + new Vector2(32f, 32f)));
		}
		if (Game1.random.NextDouble() < 0.02)
		{
			location.addJumperFrog(tileLocation.Value);
		}
		if (location.HasUnlockedAreaSecretNotes(who) && Game1.random.NextDouble() < 0.009)
		{
			Object obj = location.tryToCreateUnseenSecretNote(who);
			if (obj != null)
			{
				Game1.createItemDebris(obj, new Vector2(tileLocation.X + 0.5f, tileLocation.Y + 0.75f) * 64f, Game1.player.FacingDirection, location);
			}
		}
	}

	public virtual bool isAnimalProduct()
	{
		if (base.Category != -18 && base.Category != -5 && base.Category != -6)
		{
			return base.QualifiedItemId == "(O)430";
		}
		return true;
	}

	public virtual bool onExplosion(Farmer who)
	{
		if (who == null)
		{
			return false;
		}
		GameLocation location = Location;
		if (IsWeeds())
		{
			fragility.Value = 0;
			cutWeed(who);
			location.removeObject(tileLocation.Value, showDestroyedObject: false);
		}
		if (IsTwig())
		{
			fragility.Value = 0;
			Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(4, 10), resource: false);
			location.debris.Add(new Debris(ItemRegistry.Create("(O)388"), tileLocation.Value * 64f));
		}
		if (IsBreakableStone())
		{
			fragility.Value = 0;
		}
		performRemoveAction();
		return true;
	}

	/// <inheritdoc />
	public override bool canBeShipped()
	{
		if (!bigCraftable.Value && type.Value != null && Type != "Quest" && canBeTrashed() && !(this is Furniture))
		{
			return !(this is Wallpaper);
		}
		return false;
	}

	public virtual void ApplySprinkler(Vector2 tile)
	{
		GameLocation location = Location;
		if (!(location.doesTileHavePropertyNoNull((int)tile.X, (int)tile.Y, "NoSprinklers", "Back") == "T") && location.terrainFeatures.TryGetValue(tile, out var value) && value is HoeDirt hoeDirt && hoeDirt.state.Value != 2)
		{
			hoeDirt.state.Value = 1;
		}
	}

	public virtual void ApplySprinklerAnimation()
	{
		GameLocation location = Location;
		int modifiedRadiusForSprinkler = GetModifiedRadiusForSprinkler();
		int num = (int)tileLocation.X;
		int num2 = (int)tileLocation.Y;
		if (modifiedRadiusForSprinkler != 0)
		{
			if (modifiedRadiusForSprinkler == 1)
			{
				location.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 1984, 192, 192), 60f, 3, 100, tileLocation.Value * 64f + new Vector2(-64f, -64f), flicker: false, flipped: false)
				{
					color = Color.White * 0.4f,
					delayBeforeAnimationStart = Game1.random.Next(1000),
					id = num * 4000 + num2
				});
			}
			else if (modifiedRadiusForSprinkler > 0)
			{
				float num3 = (float)modifiedRadiusForSprinkler / 2f;
				location.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 2176, 320, 320), 60f, 4, 100, tileLocation.Value * 64f + new Vector2(32f, 32f) + new Vector2(-160f, -160f) * num3, flicker: false, flipped: false)
				{
					color = Color.White * 0.4f,
					delayBeforeAnimationStart = Game1.random.Next(1000),
					id = num * 4000 + num2,
					scale = num3
				});
			}
		}
		else
		{
			int delayBeforeAnimationStart = Game1.random.Next(1000);
			location.temporarySprites.Add(new TemporaryAnimatedSprite(29, tileLocation.Value * 64f + new Vector2(0f, -48f), Color.White * 0.5f, 4, flipped: false, 60f, 100)
			{
				delayBeforeAnimationStart = delayBeforeAnimationStart,
				id = num * 4000 + num2
			});
			location.temporarySprites.Add(new TemporaryAnimatedSprite(29, tileLocation.Value * 64f + new Vector2(48f, 0f), Color.White * 0.5f, 4, flipped: false, 60f, 100)
			{
				rotation = (float)Math.PI / 2f,
				delayBeforeAnimationStart = delayBeforeAnimationStart,
				id = num * 4000 + num2
			});
			location.temporarySprites.Add(new TemporaryAnimatedSprite(29, tileLocation.Value * 64f + new Vector2(0f, 48f), Color.White * 0.5f, 4, flipped: false, 60f, 100)
			{
				rotation = (float)Math.PI,
				delayBeforeAnimationStart = delayBeforeAnimationStart,
				id = num * 4000 + num2
			});
			location.temporarySprites.Add(new TemporaryAnimatedSprite(29, tileLocation.Value * 64f + new Vector2(-48f, 0f), Color.White * 0.5f, 4, flipped: false, 60f, 100)
			{
				rotation = 4.712389f,
				delayBeforeAnimationStart = delayBeforeAnimationStart,
				id = num * 4000 + num2
			});
		}
	}

	public virtual List<Vector2> GetSprinklerTiles()
	{
		int modifiedRadiusForSprinkler = GetModifiedRadiusForSprinkler();
		if (modifiedRadiusForSprinkler == 0)
		{
			return Utility.getAdjacentTileLocations(tileLocation.Value);
		}
		if (modifiedRadiusForSprinkler > 0)
		{
			List<Vector2> list = new List<Vector2>();
			for (int i = (int)tileLocation.X - modifiedRadiusForSprinkler; (float)i <= tileLocation.X + (float)modifiedRadiusForSprinkler; i++)
			{
				for (int j = (int)tileLocation.Y - modifiedRadiusForSprinkler; (float)j <= tileLocation.Y + (float)modifiedRadiusForSprinkler; j++)
				{
					list.Add(new Vector2(i, j));
				}
			}
			return list;
		}
		return new List<Vector2>();
	}

	public virtual bool IsInSprinklerRangeBroadphase(Vector2 target)
	{
		int num = GetModifiedRadiusForSprinkler();
		if (num == 0)
		{
			num = 1;
		}
		if (Math.Abs(target.X - TileLocation.X) <= (float)num)
		{
			return Math.Abs(target.Y - TileLocation.Y) <= (float)num;
		}
		return false;
	}

	public virtual void DayUpdate()
	{
		GameLocation location = Location;
		health = 10;
		if (IsSprinkler() && (!location.isOutdoors.Value || !location.IsRainingHere()) && GetModifiedRadiusForSprinkler() >= 0)
		{
			location.postFarmEventOvernightActions.Add(delegate
			{
				if (!Game1.player.team.SpecialOrderRuleActive("NO_SPRINKLER"))
				{
					foreach (Vector2 sprinklerTile in GetSprinklerTiles())
					{
						ApplySprinkler(sprinklerTile);
					}
					ApplySprinklerAnimation();
				}
			});
		}
		MachineData machineData = GetMachineData();
		if (machineData != null)
		{
			if (machineData.ClearContentsOvernightCondition != null && GameStateQuery.CheckConditions(machineData.ClearContentsOvernightCondition, location, null, inputItem: lastInputItem.Value, targetItem: heldObject.Value))
			{
				ResetParentSheetIndex();
				heldObject.Value = null;
				readyForHarvest.Value = false;
				showNextIndex.Value = false;
				minutesUntilReady.Value = -1;
			}
			if (heldObject.Value == null && MachineDataUtility.TryGetMachineOutputRule(this, machineData, MachineOutputTrigger.DayUpdate, null, null, location, out var rule, out var _, out var _, out var _))
			{
				OutputMachine(machineData, rule, null, null, location, probe: false);
			}
		}
		switch (base.QualifiedItemId)
		{
		case "(BC)MushroomLog":
			if (Game1.IsRainingHere(location))
			{
				minutesUntilReady.Value -= Utility.CalculateMinutesUntilMorning(Game1.timeOfDay);
			}
			break;
		case "(BC)272":
			if (!(location is AnimalHouse animalHouse))
			{
				break;
			}
			foreach (KeyValuePair<long, FarmAnimal> pair in animalHouse.animals.Pairs)
			{
				pair.Value.pet(Game1.player, is_auto_pet: true);
			}
			break;
		case "(BC)StatueOfBlessings":
			showNextIndex.Value = false;
			break;
		case "(BC)165":
			if (!(location is AnimalHouse animalHouse2) || !(heldObject.Value is Chest chest))
			{
				break;
			}
			foreach (FarmAnimal value in animalHouse2.animals.Values)
			{
				if (value.GetHarvestType() == FarmAnimalHarvestType.HarvestWithTool && value.currentProduce.Value != null)
				{
					Object obj = ItemRegistry.Create<Object>("(O)" + value.currentProduce.Value);
					obj.CanBeSetDown = false;
					obj.Quality = value.produceQuality.Value;
					if (value.hasEatenAnimalCracker.Value)
					{
						obj.Stack = 2;
					}
					if (chest.addItem(obj) == null)
					{
						value.HandleStatsOnProduceCollected(obj, (uint)obj.Stack);
						value.currentProduce.Value = null;
						value.ReloadTextureIfNeeded();
						showNextIndex.Value = true;
					}
				}
			}
			break;
		case "(BC)156":
			if (MinutesUntilReady > 0 || heldObject.Value == null)
			{
				break;
			}
			if (location.canSlimeHatchHere())
			{
				GreenSlime greenSlime = null;
				Vector2 position = new Vector2((int)tileLocation.X, (int)tileLocation.Y + 1) * 64f;
				switch (heldObject.Value.QualifiedItemId)
				{
				case "(O)680":
					greenSlime = new GreenSlime(position, 0);
					break;
				case "(O)413":
					greenSlime = new GreenSlime(position, 40);
					break;
				case "(O)437":
					greenSlime = new GreenSlime(position, 80);
					break;
				case "(O)439":
					greenSlime = new GreenSlime(position, 121);
					break;
				case "(O)857":
					greenSlime = new GreenSlime(position, 121);
					greenSlime.makeTigerSlime();
					break;
				}
				if (greenSlime != null)
				{
					Game1.showGlobalMessage(greenSlime.cute.Value ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12689") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12691"));
					Vector2 vector = Utility.recursiveFindOpenTileForCharacter(greenSlime, location, tileLocation.Value + new Vector2(0f, 1f), 10, allowOffMap: false);
					greenSlime.setTilePosition((int)vector.X, (int)vector.Y);
					location.characters.Add(greenSlime);
					ResetParentSheetIndex();
					heldObject.Value = null;
					minutesUntilReady.Value = -1;
				}
			}
			else
			{
				minutesUntilReady.Value = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay);
				readyForHarvest.Value = false;
			}
			break;
		case "(BC)108":
		{
			ResetParentSheetIndex();
			Season season = location.GetSeason();
			if (Location.IsOutdoors && (season == Season.Winter || season == Season.Fall))
			{
				base.ParentSheetIndex = 109;
			}
			break;
		}
		case "(BC)104":
			minutesUntilReady.Value = (location.IsWinterHere() ? 9999 : (-1));
			break;
		case "(BC)164":
		{
			if (!(location is Town))
			{
				break;
			}
			if (Game1.random.NextDouble() < 0.9)
			{
				GameLocation gameLocation = Game1.RequireLocation("ManorHouse");
				if (gameLocation.CanItemBePlacedHere(new Vector2(22f, 6f)))
				{
					if (!Game1.player.hasOrWillReceiveMail("lewisStatue"))
					{
						Game1.mailbox.Add("lewisStatue");
					}
					rot();
					gameLocation.objects.Add(new Vector2(22f, 6f), ItemRegistry.Create<Object>("(BC)164"));
				}
				break;
			}
			GameLocation gameLocation2 = Game1.RequireLocation("AnimalShop");
			if (gameLocation2.CanItemBePlacedHere(new Vector2(11f, 6f)))
			{
				if (!Game1.player.hasOrWillReceiveMail("lewisStatue"))
				{
					Game1.mailbox.Add("lewisStatue");
				}
				rot();
				gameLocation2.objects.Add(new Vector2(11f, 6f), ItemRegistry.Create<Object>("(BC)164"));
			}
			break;
		}
		case "(O)747":
		case "(O)748":
			destroyOvernight = true;
			break;
		case "(O)746":
			if (location.IsWinterHere())
			{
				rot();
			}
			break;
		case "(O)784":
		case "(O)785":
			if (Game1.dayOfMonth == 1 && !location.IsSpringHere() && location.isOutdoors.Value)
			{
				base.ParentSheetIndex++;
			}
			break;
		case "(O)674":
		case "(O)675":
			if (Game1.dayOfMonth == 1 && location.IsSummerHere() && location.isOutdoors.Value)
			{
				base.ParentSheetIndex += 2;
			}
			break;
		case "(O)677":
		case "(O)676":
			if (Game1.dayOfMonth == 1 && location.IsFallHere() && location.isOutdoors.Value)
			{
				base.ParentSheetIndex += 2;
			}
			break;
		}
		if (bigCraftable.Value && name.Contains("Seasonal"))
		{
			int num = base.ParentSheetIndex - base.ParentSheetIndex % 4;
			base.ParentSheetIndex = num + location.GetSeasonIndex();
		}
	}

	public virtual void rot()
	{
		Random random = Utility.CreateRandom((double)Game1.year * 999.0, Game1.dayOfMonth, Game1.seasonIndex);
		SetIdAndSprite(random.Choose(747, 748));
		price.Value = 0;
		quality.Value = 0;
		name = "Rotten Plant";
		displayName = null;
		lightSource = null;
		bigCraftable.Value = false;
	}

	public override void actionWhenBeingHeld(Farmer who)
	{
		GameLocation currentLocation = who.currentLocation;
		if (currentLocation != null)
		{
			if (Game1.eventUp && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
			{
				currentLocation.removeLightSource(lightSource?.Id);
				base.actionWhenBeingHeld(who);
				return;
			}
			if (lightSource != null && (!bigCraftable.Value || isLamp.Value))
			{
				if (!currentLocation.hasLightSource(lightSource.Id))
				{
					currentLocation.sharedLights.AddLight(new LightSource(lightSource.Id, lightSource.textureIndex.Value, lightSource.position.Value, lightSource.radius.Value, lightSource.color.Value, LightSource.LightContext.None, who.UniqueMultiplayerID, currentLocation.NameOrUniqueName));
				}
				currentLocation.repositionLightSource(lightSource.Id, who.Position + new Vector2(32f, -64f));
			}
		}
		base.actionWhenBeingHeld(who);
	}

	public override void actionWhenStopBeingHeld(Farmer who)
	{
		who.currentLocation?.removeLightSource(lightSource?.Id);
		base.actionWhenStopBeingHeld(who);
	}

	public static void ConsumeInventoryItem(Farmer who, Item drop_in, int amount)
	{
		if (drop_in.ConsumeStack(amount) == null)
		{
			(autoLoadFrom ?? who.Items).RemoveButKeepEmptySlot(drop_in);
			autoLoadFrom?.RemoveEmptySlots();
		}
	}

	/// <summary>Try to add an item to the object (e.g. input for a machine, placed on a table, etc).</summary>
	/// <param name="dropInItem">The item to add.</param>
	/// <param name="probe">Whether to return whether the item would be accepted without making any changes.</param>
	/// <param name="who">The player adding the item.</param>
	/// <param name="returnFalseIfItemConsumed">Whether to return false if the item was accepted, but it was already deducted from the inventory.</param>
	/// <returns>Usually returns whether the item was accepted by the machine.</returns>
	public virtual bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false)
	{
		if (isTemporarilyInvisible)
		{
			return false;
		}
		if (!(dropInItem is Object obj))
		{
			return false;
		}
		GameLocation location = Location;
		if (IsSprinkler())
		{
			if (heldObject.Value == null && (obj.QualifiedItemId == "(O)915" || obj.QualifiedItemId == "(O)913"))
			{
				if (probe)
				{
					return true;
				}
				if (location is MineShaft || (location is VolcanoDungeon && obj.QualifiedItemId == "(O)913"))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
					return false;
				}
				Object obj2 = obj.getOne() as Object;
				if (obj2?.QualifiedItemId == "(O)913" && obj2.heldObject.Value == null)
				{
					Chest chest = new Chest();
					chest.SpecialChestType = Chest.SpecialChestTypes.Enricher;
					obj2.heldObject.Value = chest;
				}
				location.playSound("axe");
				heldObject.Value = obj2;
				minutesUntilReady.Value = -1;
				return true;
			}
			if (obj.QualifiedItemId == "(O)93" && base.SpecialVariable != 999999)
			{
				if (probe)
				{
					return true;
				}
				base.SpecialVariable = 999999;
				Game1.playSound("woodyStep");
				lightSource = new LightSource(GenerateLightSourceId(TileLocation), 4, new Vector2(tileLocation.X * 64f + 16f, tileLocation.Y * 64f + 16f), 1.25f, new Color(1, 1, 1) * 0.9f, LightSource.LightContext.None, 0L, location.NameOrUniqueName);
				return true;
			}
		}
		if (obj.QualifiedItemId == "(O)872" && autoLoadFrom == null && TryApplyFairyDust(probe))
		{
			return true;
		}
		if ((base.QualifiedItemId == "(BC)13" || base.Name == "Furnace") && dropInItem != null)
		{
			string dropId = dropInItem.QualifiedItemId;
			if (StardewValley.Pokemon.PokemonItemManager.IsRawApricorn(dropId))
			{
				if (heldObject.Value != null)
				{
					return false;
				}
				if (probe)
				{
					return true;
				}
				if (who.Items.ContainsId("(O)382"))
				{
					who.Items.ReduceId("(O)382", 1);
					dropInItem.Stack--;
					if (dropInItem.Stack <= 0 && who.ActiveObject == dropInItem)
					{
						who.ActiveObject = null;
					}
					string cookedId = StardewValley.Pokemon.PokemonItemManager.GetCookedApricornId(dropId);
					heldObject.Value = ItemRegistry.Create<Object>(cookedId);
					MinutesUntilReady = 20;
					location.playSound("furnace");
					initializeLightSource(tileLocation.Value);
					showNextIndex.Value = true;
					return true;
				}
				else
				{
					Game1.showRedMessage("Você precisa de 1 Carvão para acender a fornalha!");
					return false;
				}
			}
		}

		// Pokémon Berry -> Medicine Crafting in the Keg
		if ((base.QualifiedItemId == "(BC)12" || base.Name == "Keg") && dropInItem != null)
		{
			string berryId = dropInItem.QualifiedItemId;
			if (berryId.StartsWith("(O)")) berryId = berryId.Substring(3);
			if (berryId.StartsWith("Poke_")) berryId = berryId.Substring(5);

			string resultMedicine = berryId switch
			{
				"CheriBerry" => "PokemonParalyzeHeal",
				"PechaBerry" => "PokemonAntidote",
				"RawstBerry" => "PokemonBurnHeal",
				"ChestoBerry" => "PokemonAwakening",
				"AspearBerry" => "PokemonIceHeal",
				"PersimBerry" or "LumBerry" => "PokemonFullHeal",
				"OranBerry" => "PokemonPotion",
				"SitrusBerry" => "PokemonSuperPotion",
				"LeppaBerry" => "PokemonMaxPotion",
				_ => null
			};

			if (resultMedicine != null)
			{
				if (heldObject.Value != null) return false;
				if (probe) return true;

				dropInItem.Stack--;
				if (dropInItem.Stack <= 0 && who.ActiveObject == dropInItem)
				{
					who.ActiveObject = null;
				}
				heldObject.Value = ItemRegistry.Create<Object>("(O)" + resultMedicine) ?? new Object(resultMedicine, 1);
				MinutesUntilReady = 180;
				location.playSound("Ship");
				showNextIndex.Value = true;
				return true;
			}
		}

		// Pokémon Egg into Coop Egg Incubator
		if ((base.QualifiedItemId == "(BC)101" || base.QualifiedItemId == "(BC)104" || (GetMachineData() != null && GetMachineData().IsIncubator) || (base.Name != null && base.Name.Contains("Incubator"))))
		{
			if (heldObject.Value == null)
			{
				StardewValley.Pokemon.PokemonInstance eggToIncubate = null;
				bool fromItem = false;
				if (dropInItem != null && (dropInItem.ItemId == "PokemonEgg" || dropInItem.QualifiedItemId == "(O)PokemonEgg" || dropInItem.Name == "Ovo Pokémon" || dropInItem.Name == "PokemonEgg"))
				{
					eggToIncubate = new StardewValley.Pokemon.PokemonInstance(StardewValley.Pokemon.PokemonSpecies.Get("Pichu"), 1) { IsEgg = true, EggSteps = 150, Nickname = "Ovo Pokémon" };
					fromItem = true;
				}
				else if (StardewValley.Pokemon.PlayerParty.Instance?.Team != null)
				{
					eggToIncubate = StardewValley.Pokemon.PlayerParty.Instance.Team.Find(p => p != null && p.IsEgg);
				}

				if (eggToIncubate != null)
				{
					if (probe) return true;

					if (fromItem)
					{
						dropInItem.Stack--;
						if (dropInItem.Stack <= 0 && who.ActiveObject == dropInItem) who.ActiveObject = null;
					}
					else
					{
						StardewValley.Pokemon.PlayerParty.Instance.RemovePokemon(eggToIncubate);
					}

					heldObject.Value = ItemRegistry.Create<Object>("(O)107") ?? new Object("107", 1);
					StardewValley.Pokemon.PokemonBreeding.AddIncubatingEgg(TileLocation, eggToIncubate);
					MinutesUntilReady = 480;
					showNextIndex.Value = true;
					Game1.playSound("Ship");
					Game1.addHUDMessage(new HUDMessage($"🥚 Ovo de {eggToIncubate.Species?.Name ?? "Pokémon"} colocado na chocadeira!", 2));
					return true;
				}
			}
		}

		MachineData machineData = GetMachineData();
		if (machineData != null)
		{
			if (heldObject.Value != null && !machineData.AllowLoadWhenFull)
			{
				return false;
			}
			if (probe && MinutesUntilReady > 0)
			{
				return false;
			}
			if (PlaceInMachine(machineData, dropInItem, probe, who))
			{
				if (returnFalseIfItemConsumed && !probe)
				{
					return false;
				}
				return true;
			}
			return false;
		}
		if (base.QualifiedItemId == "(BC)99" && obj.QualifiedItemId == "(O)178")
		{
			GameLocation rootLocation = location.GetRootLocation();
			if (rootLocation.GetHayCapacity() <= 0)
			{
				if (autoLoadFrom == null && !probe)
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\Buildings:NeedSilo"));
				}
				return false;
			}
			if (probe)
			{
				return true;
			}
			location.playSound("Ship");
			DelayedAction.playSoundAfterDelay("grassyStep", 100);
			if (obj.Stack == 0)
			{
				obj.Stack = 1;
			}
			int value = rootLocation.piecesOfHay.Value;
			int num = rootLocation.tryToAddHay(obj.Stack);
			int value2 = rootLocation.piecesOfHay.Value;
			if (value <= 0 && value2 > 0)
			{
				showNextIndex.Value = true;
			}
			else if (value2 <= 0)
			{
				showNextIndex.Value = false;
			}
			obj.Stack = num;
			if (num <= 0)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Update the machine for the effects of fairy dust, if applicable.</summary>
	/// <param name="probe">Whether the game is only checking whether fairy dust would be accepted.</param>
	/// <returns>Returns whether the machine was updated (or if <paramref name="probe" /> is true, whether it would have been updated).</returns>
	public virtual bool TryApplyFairyDust(bool probe = false)
	{
		if (MinutesUntilReady > 0 && (GetMachineData()?.AllowFairyDust ?? false))
		{
			if (!probe)
			{
				Utility.addSprinklesToLocation(Location, (int)tileLocation.X, (int)tileLocation.Y, 1, 2, 400, 40, Color.White);
				Game1.playSound("yoba");
				MinutesUntilReady = 10;
				DelayedAction.functionAfterDelay(delegate
				{
					minutesElapsed(10);
				}, 50);
			}
			return true;
		}
		return false;
	}

	/// <summary>Get the output item to produce for a Solar Panel.</summary>
	/// <inheritdoc cref="T:StardewValley.Delegates.MachineOutputDelegate" />
	public static Item OutputSolarPanel(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
	{
		int num = machine.MinutesUntilReady;
		GameLocation location = machine.Location;
		Object obj = machine.heldObject.Value;
		if (obj == null)
		{
			obj = ItemRegistry.Create<Object>("(O)787");
			obj.CanBeSetDown = false;
			num = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay, 7);
		}
		if (num > 0 && location.IsOutdoors && !location.IsRainingHere())
		{
			num = Math.Max(0, num - 2400);
		}
		overrideMinutesUntilReady = ((num != machine.MinutesUntilReady) ? new int?(num) : ((int?)null));
		return obj;
	}

	/// <summary>Get the output item to produce for a Statue of Endless Fortune.</summary>
	/// <inheritdoc cref="T:StardewValley.Delegates.MachineOutputDelegate" />
	public static Item OutputStatueOfEndlessFortune(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
	{
		overrideMinutesUntilReady = null;
		Item item = Utility.getTodaysBirthdayNPC()?.getFavoriteItem();
		if (item != null)
		{
			return item;
		}
		string text = "80";
		switch (Game1.random.Next(4))
		{
		case 0:
			text = "72";
			break;
		case 1:
			text = "337";
			break;
		case 2:
			text = "749";
			break;
		case 3:
			text = "336";
			break;
		}
		return new Object(text, 1);
	}

	/// <summary>Get the output item to produce for a Deconstructor.</summary>
	/// <inheritdoc cref="T:StardewValley.Delegates.MachineOutputDelegate" />
	public static Item OutputDeconstructor(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
	{
		overrideMinutesUntilReady = null;
		if (!inputItem.HasTypeObject() && !inputItem.HasTypeBigCraftable())
		{
			return null;
		}
		if (!CraftingRecipe.craftingRecipes.TryGetValue(inputItem.Name, out var value))
		{
			return null;
		}
		string[] array = value.Split('/');
		if (ArgUtility.SplitBySpace(ArgUtility.Get(array, 2)).Length > 1)
		{
			return null;
		}
		if (inputItem.QualifiedItemId == "(O)710")
		{
			return ItemRegistry.Create("(O)334", 2);
		}
		Object obj = null;
		string[] array2 = ArgUtility.SplitBySpace(ArgUtility.Get(array, 0));
		for (int i = 0; i < array2.Length; i += 2)
		{
			string text = ArgUtility.Get(array2, i);
			int initialStack = ArgUtility.GetInt(array2, i + 1, 1);
			Object obj2 = new Object(text, initialStack);
			if (obj == null || obj2.sellToStorePrice(-1L) * obj2.Stack > obj.sellToStorePrice(-1L) * obj.Stack)
			{
				obj = obj2;
			}
		}
		return obj;
	}

	/// <summary>Get the output item to produce for an Anvil.</summary>
	/// <inheritdoc cref="T:StardewValley.Delegates.MachineOutputDelegate" />
	public static Item OutputAnvil(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
	{
		overrideMinutesUntilReady = null;
		if (!(inputItem is Trinket trinket))
		{
			return null;
		}
		if (!trinket.GetTrinketData().CanBeReforged)
		{
			if (!probe)
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\1_6_Strings:Anvil_wrongtrinket"));
			}
			return null;
		}
		Trinket trinket2 = (Trinket)inputItem.getOne();
		if (!trinket2.RerollStats(Game1.random.Next(9999999)))
		{
			if (!probe)
			{
				player?.doEmote(40);
			}
			return null;
		}
		if (!probe)
		{
			Game1.currentLocation.playSound("metal_tap");
			DelayedAction.playSoundAfterDelay("metal_tap", 250);
			DelayedAction.playSoundAfterDelay("metal_tap", 500);
		}
		overrideMinutesUntilReady = 10;
		return trinket2;
	}

	/// <summary>Get the output item to produce for a Geode Crusher.</summary>
	/// <inheritdoc cref="T:StardewValley.Delegates.MachineOutputDelegate" />
	public static Item OutputGeodeCrusher(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
	{
		overrideMinutesUntilReady = null;
		if (!Utility.IsGeode(inputItem, disallow_special_geodes: true))
		{
			return null;
		}
		Item treasureFromGeode = Utility.getTreasureFromGeode(inputItem);
		if (!probe)
		{
			GameLocation location = machine.Location;
			Vector2 vector = machine.tileLocation.Value * 64f;
			Utility.addSmokePuff(location, vector + new Vector2(4f, -48f), 200);
			Utility.addSmokePuff(location, vector + new Vector2(-16f, -56f), 300);
			Utility.addSmokePuff(location, vector + new Vector2(16f, -52f), 400);
			Utility.addSmokePuff(location, vector + new Vector2(32f, -56f), 200);
			Utility.addSmokePuff(location, vector + new Vector2(40f, -44f), 500);
		}
		return treasureFromGeode;
	}

	/// <summary>Get the output item to produce for an Incubator or Ostrich Incubator.</summary>
	/// <inheritdoc cref="T:StardewValley.Delegates.MachineOutputDelegate" />
	public static Item OutputIncubator(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
	{
		BuildingData buildingData = machine.Location.ParentBuilding?.GetData();
		if (buildingData == null)
		{
			overrideMinutesUntilReady = null;
			return null;
		}
		FarmAnimalData animalDataFromEgg = FarmAnimal.GetAnimalDataFromEgg(inputItem, machine.Location);
		if (animalDataFromEgg == null || !buildingData.ValidOccupantTypes.Contains(animalDataFromEgg.House))
		{
			overrideMinutesUntilReady = null;
			return null;
		}
		overrideMinutesUntilReady = ((animalDataFromEgg.IncubationTime > 0) ? animalDataFromEgg.IncubationTime : 9000);
		return inputItem.getOne();
	}

	/// <summary>Get the output item to produce for a Seed Maker.</summary>
	/// <inheritdoc cref="T:StardewValley.Delegates.MachineOutputDelegate" />
	public static Item OutputSeedMaker(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
	{
		overrideMinutesUntilReady = null;
		if (!inputItem.HasTypeObject())
		{
			return null;
		}
		string text = null;
		foreach (KeyValuePair<string, CropData> cropDatum in Game1.cropData)
		{
			if (ItemRegistry.HasItemId(inputItem, cropDatum.Value.HarvestItemId))
			{
				text = cropDatum.Key;
				break;
			}
		}
		if (text == null)
		{
			return null;
		}
		Vector2 value = machine.tileLocation.Value;
		Random random = Utility.CreateDaySaveRandom(value.X, value.Y * 77f, Game1.timeOfDay);
		if (random.NextDouble() < 0.005)
		{
			return new Object("499", 1);
		}
		if (random.NextDouble() < 0.02)
		{
			return new Object("770", random.Next(1, 5));
		}
		return new Object(text, random.Next(1, 4));
	}

	public static Item OutputMushroomLog(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
	{
		overrideMinutesUntilReady = null;
		List<Tree> list = new List<Tree>();
		for (int i = (int)machine.TileLocation.X - 3; i < (int)machine.TileLocation.X + 4; i++)
		{
			for (int j = (int)machine.TileLocation.Y - 3; j < (int)machine.TileLocation.Y + 4; j++)
			{
				Vector2 key = new Vector2(i, j);
				if (machine.Location.terrainFeatures.GetValueOrDefault(key) is Tree item)
				{
					list.Add(item);
				}
			}
		}
		int count = list.Count;
		List<string> list2 = new List<string>();
		int num = 0;
		foreach (Tree item3 in list)
		{
			if (item3.growthStage.Value >= 5)
			{
				string item2 = (Game1.random.NextBool(0.05) ? "(O)422" : (Game1.random.NextBool(0.15) ? "(O)420" : "(O)404"));
				switch (item3.treeType.Value)
				{
				case "2":
					item2 = (Game1.random.NextBool(0.1) ? "(O)422" : "(O)420");
					break;
				case "1":
					item2 = "(O)257";
					break;
				case "3":
					item2 = "(O)281";
					break;
				case "13":
					item2 = "(O)422";
					break;
				}
				list2.Add(item2);
				if (item3.hasMoss.Value)
				{
					num++;
				}
			}
		}
		for (int k = 0; k < Math.Max(1, (int)((float)list.Count * 0.75f)); k++)
		{
			list2.Add(Game1.random.NextBool(0.05) ? "(O)422" : (Game1.random.NextBool(0.15) ? "(O)420" : "(O)404"));
		}
		int amount = Math.Max(1, Math.Min(5, Game1.random.Next(1, 3) * (list.Count / 2)));
		int num2 = 0;
		float num3 = (float)num * 0.025f + (float)count * 0.025f;
		while (Game1.random.NextDouble() < (double)num3)
		{
			num2++;
			if (num2 == 3)
			{
				num2 = 4;
				break;
			}
		}
		return ItemRegistry.Create(Game1.random.ChooseFrom(list2), amount, num2);
	}

	public bool ParseItemCount(string[] query, out string replacement, Random random, Farmer player)
	{
		if (query[0] == "ItemCount")
		{
			replacement = CurrentParsedItemCount.ToString();
			return true;
		}
		replacement = null;
		return false;
	}

	/// <summary>Place an item in this machine.</summary>
	/// <param name="machineData">The machine data to apply.</param>
	/// <param name="inputItem">The item to place in the machine.</param>
	/// <param name="probe">Whether to return whether the item would be placed successfully without making any changes.</param>
	/// <param name="who">The player placing an item in the machine.</param>
	/// <param name="showMessages">Whether to show UI messages for the player.</param>
	/// <param name="playSounds">Whether to play sounds when the item is placed.</param>
	public bool PlaceInMachine(MachineData machineData, Item inputItem, bool probe, Farmer who, bool showMessages = true, bool playSounds = true)
	{
		if (machineData == null || inputItem == null)
		{
			return false;
		}
		if (heldObject.Value != null)
		{
			if (!machineData.AllowLoadWhenFull)
			{
				return false;
			}
			if (inputItem.QualifiedItemId == lastInputItem.Value?.QualifiedItemId)
			{
				return false;
			}
		}
		if (!MachineDataUtility.HasAdditionalRequirements(autoLoadFrom ?? who.Items, machineData.AdditionalConsumedItems, out var failedRequirement))
		{
			if (showMessages && failedRequirement.InvalidCountMessage != null && !probe && autoLoadFrom == null)
			{
				CurrentParsedItemCount = failedRequirement.RequiredCount;
				Game1.showRedMessage(TokenParser.ParseText(failedRequirement.InvalidCountMessage, null, ParseItemCount));
				who.ignoreItemConsumptionThisFrame = true;
			}
			return false;
		}
		GameLocation location = Location;
		if (!MachineDataUtility.TryGetMachineOutputRule(this, machineData, MachineOutputTrigger.ItemPlacedInMachine, inputItem, who, location, out var rule, out var triggerRule, out var ruleIgnoringCount, out var triggerIgnoringCount))
		{
			if (showMessages && !probe && autoLoadFrom == null)
			{
				if (ruleIgnoringCount != null)
				{
					string text = ruleIgnoringCount.InvalidCountMessage ?? machineData.InvalidCountMessage;
					if (!string.IsNullOrWhiteSpace(text))
					{
						CurrentParsedItemCount = triggerIgnoringCount.RequiredCount;
						Game1.showRedMessage(TokenParser.ParseText(text, null, ParseItemCount));
						who.ignoreItemConsumptionThisFrame = true;
					}
				}
				else if (machineData.InvalidItemMessage != null && GameStateQuery.CheckConditions(machineData.InvalidItemMessageCondition, location, who, null, who.ActiveObject))
				{
					Game1.showRedMessage(TokenParser.ParseText(machineData.InvalidItemMessage));
					who.ignoreItemConsumptionThisFrame = true;
				}
			}
			return false;
		}
		if (probe)
		{
			return true;
		}
		if (!OutputMachine(machineData, rule, inputItem, who, location, probe))
		{
			return false;
		}
		if (machineData.AdditionalConsumedItems != null)
		{
			IInventory inventory = autoLoadFrom ?? who.Items;
			foreach (MachineItemAdditionalConsumedItems additionalConsumedItem in machineData.AdditionalConsumedItems)
			{
				inventory.ReduceId(additionalConsumedItem.ItemId, additionalConsumedItem.RequiredCount);
			}
		}
		if (triggerRule.RequiredCount > 0)
		{
			ConsumeInventoryItem(who, inputItem, triggerRule.RequiredCount);
		}
		if (machineData.LoadEffects != null)
		{
			foreach (MachineEffects loadEffect in machineData.LoadEffects)
			{
				if (PlayMachineEffect(loadEffect, playSounds))
				{
					_machineAnimation = loadEffect;
					_machineAnimationLoop = false;
					_machineAnimationIndex = 0;
					_machineAnimationFrame = -1;
					_machineAnimationInterval = 0;
					break;
				}
			}
		}
		playCustomMachineLoadEffects();
		MachineDataUtility.UpdateStats(machineData.StatsToIncrementWhenLoaded, inputItem, 1);
		return true;
	}

	private void playCustomMachineLoadEffects()
	{
		if (base.ItemId == "FishSmoker")
		{
			for (int i = 0; i < 12; i++)
			{
				Location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), 9999f, 1, 1, new Vector2((float)((int)TileLocation.X * 64) + 18f, ((float)(int)TileLocation.Y - 1.15f) * 64f), flicker: false, flipped: false)
				{
					color = new Color(60, 60, 60),
					alphaFade = -0.02f,
					alpha = 0.01f,
					alphaFadeFade = -0.0003f,
					motion = new Vector2(0.25f, -0.1f),
					acceleration = new Vector2(0f, -0.01f),
					rotationChange = (float)Game1.random.Next(-10, 10) / 500f,
					scale = 1.5f,
					scaleChange = 0.024f,
					layerDepth = Math.Max(0f, ((tileLocation.Y + 1f) * 64f - 24f + (float)i) / 10000f) + tileLocation.X * 1E-05f,
					delayBeforeAnimationStart = i * 550
				});
			}
		}
	}

	/// <summary>Cause the machine to produce output, if applicable.</summary>
	/// <param name="machine">The machine data to apply.</param>
	/// <param name="outputRule">The output rule to apply, or <c>null</c> to get a matching rule from the machine data.</param>
	/// <param name="inputItem">The input item for which to produce an item, if applicable.</param>
	/// <param name="who">The player for which to start producing output, or <c>null</c> for the main player.</param>
	/// <param name="location">The location containing the machine.</param>
	/// <param name="probe">Whether to return whether the machine would produce output without making any changes.</param>
	/// <param name="heldObjectOnly">Whether to change the held object without changing any other machine fields (e.g. to implement <see cref="F:StardewValley.GameData.Machines.MachineOutputRule.RecalculateOnCollect" />).</param>
	public virtual bool OutputMachine(MachineData machine, MachineOutputRule outputRule, Item inputItem, Farmer who, GameLocation location, bool probe, bool heldObjectOnly = false)
	{
		who = who ?? Game1.MasterPlayer;
		if (machine == null || (heldObject.Value != null && !machine.AllowLoadWhenFull))
		{
			return false;
		}
		if (outputRule == null && !MachineDataUtility.TryGetMachineOutputRule(this, machine, MachineOutputTrigger.ItemPlacedInMachine, inputItem, who, location, out outputRule, out var _, out var _, out var _))
		{
			return false;
		}
		MachineItemOutput outputData = MachineDataUtility.GetOutputData(this, machine, outputRule, inputItem, who, location);
		int? overrideMinutesUntilReady;
		Item outputItem = MachineDataUtility.GetOutputItem(this, outputData, inputItem, who, heldObjectOnly || probe, out overrideMinutesUntilReady);
		if (outputItem == null)
		{
			return false;
		}
		if (probe)
		{
			return true;
		}
		outputItem.FixQuality();
		outputItem.FixStackSize();
		heldObject.Value = (Object)outputItem;
		if (!heldObjectOnly)
		{
			int num = 0;
			if (overrideMinutesUntilReady >= 0)
			{
				num = overrideMinutesUntilReady.Value;
			}
			else if (outputRule.MinutesUntilReady >= 0 || outputRule.DaysUntilReady >= 0)
			{
				num = ((outputRule.DaysUntilReady >= 0) ? Utility.CalculateMinutesUntilMorning(Game1.timeOfDay, outputRule.DaysUntilReady) : outputRule.MinutesUntilReady);
			}
			num = (int)Utility.ApplyQuantityModifiers(num, machine.ReadyTimeModifiers, machine.ReadyTimeModifierMode, location, who, heldObject.Value, inputItem);
			MinutesUntilReady = num;
			if (MinutesUntilReady == 0)
			{
				readyForHarvest.Value = true;
			}
			lastOutputRuleId.Value = outputRule.Id;
			if (inputItem != null)
			{
				lastInputItem.Value = inputItem.getOne();
				lastInputItem.Value.Stack = inputItem.Stack;
			}
			else
			{
				lastInputItem.Value = null;
			}
			if (machine.IsIncubator && location is AnimalHouse animalHouse)
			{
				animalHouse.hasShownIncubatorBuildingFullMessage = false;
			}
			ResetParentSheetIndex();
			base.ParentSheetIndex += outputData.IncrementMachineParentSheetIndex;
			if (machine.LightWhileWorking != null)
			{
				initializeLightSource(tileLocation.Value);
			}
			if (machine.ShowNextIndexWhileWorking)
			{
				showNextIndex.Value = true;
			}
			if (machine.WobbleWhileWorking)
			{
				scale.X = 5f;
			}
			minutesElapsed(0);
		}
		return true;
	}

	/// <summary>Apply a machine effect, if it's valid and its fields match.</summary>
	/// <param name="effect">The machine effect to apply.</param>
	/// <param name="playSounds">Whether to play sounds when the item is placed.</param>
	public virtual bool PlayMachineEffect(MachineEffects effect, bool playSounds = true)
	{
		return MachineDataUtility.PlayEffects(this, effect, playSounds);
	}

	public virtual void updateWhenCurrentLocation(GameTime time)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return;
		}
		if (readyForHarvest.Value && !_hasHeldObject)
		{
			readyForHarvest.Value = false;
		}
		if (_hasLightSource)
		{
			LightSource lightSource = netLightSource.Get();
			if (lightSource != null && isOn.Value && !location.hasLightSource(lightSource.Id))
			{
				location.sharedLights.AddLight(lightSource.Clone());
			}
		}
		if (_machineAnimation != null)
		{
			List<int> frames = _machineAnimation.Frames;
			if (frames != null && frames.Count > 0)
			{
				_machineAnimationInterval += (int)time.ElapsedGameTime.TotalMilliseconds;
				if (_machineAnimation.Interval > 0 && _machineAnimationInterval >= _machineAnimation.Interval)
				{
					_machineAnimationIndex += _machineAnimationInterval / _machineAnimation.Interval;
					_machineAnimationInterval %= _machineAnimation.Interval;
					if (_machineAnimationIndex >= _machineAnimation.Frames.Count)
					{
						if (_machineAnimationLoop)
						{
							_machineAnimationIndex %= _machineAnimation.Frames.Count;
						}
						else
						{
							_machineAnimation = null;
							_machineAnimationFrame = -1;
						}
					}
				}
				if (_machineAnimation != null)
				{
					_machineAnimationFrame = _machineAnimation.Frames[_machineAnimationIndex];
				}
			}
			else
			{
				_machineAnimationFrame = -1;
			}
		}
		if (_hasHeldObject)
		{
			Object obj = heldObject.Get();
			if (obj.QualifiedItemId == "(O)913" && IsSprinkler() && obj.heldObject.Value is Chest chest)
			{
				chest.mutex.Update(location);
				if (Game1.activeClickableMenu == null && chest.GetMutex().IsLockHeld())
				{
					chest.GetMutex().ReleaseLock();
				}
			}
			if (obj._hasLightSource)
			{
				this.lightSource = obj.netLightSource.Get();
				if (this.lightSource != null && !location.hasLightSource(this.lightSource.Id))
				{
					location.sharedLights.AddLight(this.lightSource.Clone());
				}
			}
			if (!readyForHarvest.Value)
			{
				if (_machineAnimation == null)
				{
					MachineData machineData = GetMachineData();
					if (machineData?.WorkingEffects != null)
					{
						foreach (MachineEffects workingEffect in machineData.WorkingEffects)
						{
							if (workingEffect != null)
							{
								string condition = workingEffect.Condition;
								GameLocation location2 = Location;
								Item value = lastInputItem.Value;
								if (GameStateQuery.CheckConditions(condition, location2, null, obj, value))
								{
									_machineAnimation = workingEffect;
									_machineAnimationLoop = true;
									_machineAnimationIndex = 0;
									_machineAnimationFrame = -1;
									MachineEffects machineAnimation = _machineAnimation;
									_machineAnimationInterval = ((machineAnimation != null && machineAnimation.Frames?.Count > 0 && _machineAnimation.Interval > 0) ? ((int)(((double)(long)(tileLocation.X * (float)(_machineAnimation.Interval / 2) + tileLocation.Y * (float)(_machineAnimation.Interval / 2 * 10)) + time.TotalGameTime.TotalMilliseconds) % (double)(_machineAnimation.Interval * _machineAnimation.Frames.Count))) : 0);
									break;
								}
							}
						}
					}
				}
			}
			else if (_machineAnimation != null && _machineAnimationLoop)
			{
				_machineAnimation = null;
			}
		}
		else if (_machineAnimation != null && _machineAnimationLoop)
		{
			_machineAnimation = null;
		}
		if (shakeTimer > 0)
		{
			shakeTimer -= time.ElapsedGameTime.Milliseconds;
			if (shakeTimer <= 0)
			{
				health = 10;
			}
		}
		switch (base.QualifiedItemId)
		{
		case "(O)590":
		case "(O)SeedSpot":
			if (Game1.random.NextDouble() < 0.01)
			{
				shakeTimer = 100;
			}
			break;
		case "(BC)56":
			ResetParentSheetIndex();
			base.ParentSheetIndex += (int)(time.TotalGameTime.TotalMilliseconds % 600.0 / 100.0);
			break;
		}
		if (!IsTextSign())
		{
			return;
		}
		if (shouldShowSign)
		{
			shouldShowSign = false;
			lastNoteBlockSoundTime += (int)time.ElapsedGameTime.TotalMilliseconds;
			if (lastNoteBlockSoundTime > 125)
			{
				lastNoteBlockSoundTime = 125;
			}
		}
		else if (lastNoteBlockSoundTime > 0)
		{
			lastNoteBlockSoundTime -= (int)time.ElapsedGameTime.TotalMilliseconds;
			if (lastNoteBlockSoundTime < 0)
			{
				lastNoteBlockSoundTime = 0;
			}
		}
	}

	/// <summary>Handle the player entering the location containing the object.</summary>
	public virtual void actionOnPlayerEntry()
	{
		isTemporarilyInvisible = false;
		health = 10;
		if (base.QualifiedItemId == "(BC)99")
		{
			showNextIndex.Value = Location.GetRootLocation().piecesOfHay.Value > 0;
		}
	}

	/// <inheritdoc />
	public override bool canBeTrashed()
	{
		if (!questItem.Value && base.canBeTrashed())
		{
			if (Game1.objectData.TryGetValue(base.ItemId, out var value))
			{
				return value.CanBeTrashed;
			}
			return true;
		}
		return false;
	}

	public virtual bool isForage()
	{
		if (base.Category != -79 && base.Category != -81 && base.Category != -80 && base.Category != -75 && base.Category != -23 && !HasContextTag("forage_item"))
		{
			return base.QualifiedItemId == "(O)430";
		}
		return true;
	}

	/// <summary>Initialize and register a light source for this instance, if applicable for its data and state.</summary>
	/// <param name="tileLocation">The object's tile position.</param>
	/// <param name="mineShaft">Whether the object is in the mines.</param>
	public virtual void initializeLightSource(Vector2 tileLocation, bool mineShaft = false)
	{
		if (name == "Error Item")
		{
			return;
		}
		Furniture furniture = this as Furniture;
		if (furniture != null && furniture.furniture_type.Value == 14 && furniture.isOn.Value)
		{
			lightSource = new LightSource(GenerateLightSourceId(tileLocation), 4, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f - 64f), 2.5f, new Color(0, 80, 160), LightSource.LightContext.None, 0L, Location?.NameOrUniqueName);
			return;
		}
		if (furniture != null && furniture.furniture_type.Value == 16 && furniture.isOn.Value)
		{
			lightSource = new LightSource(GenerateLightSourceId(tileLocation), 4, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f - 64f), 1.5f, new Color(0, 80, 160), LightSource.LightContext.None, 0L, Location?.NameOrUniqueName);
			return;
		}
		if (bigCraftable.Value)
		{
			if (this is Torch && isOn.Value)
			{
				float num = -64f;
				if (ItemContextTagManager.HasBaseTag(base.QualifiedItemId, "campfire_item"))
				{
					num = 32f;
				}
				lightSource = new LightSource(GenerateLightSourceId(tileLocation), 4, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + num), 2.5f, new Color(0, 80, 160), LightSource.LightContext.None, 0L, Location?.NameOrUniqueName);
				return;
			}
			if (isLamp.Value)
			{
				lightSource = new LightSource(GenerateLightSourceId(tileLocation), 4, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f - 64f), 3f, new Color(0, 40, 80), LightSource.LightContext.None, 0L, Location?.NameOrUniqueName);
				return;
			}
			string qualifiedItemId = base.QualifiedItemId;
			if (qualifiedItemId == "(BC)74")
			{
				lightSource = new LightSource(GenerateLightSourceId(tileLocation), 4, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f), 1.5f, Color.DarkCyan, LightSource.LightContext.None, 0L, Location?.NameOrUniqueName);
				return;
			}
			if (qualifiedItemId == "(BC)96")
			{
				lightSource = new LightSource(GenerateLightSourceId(tileLocation), 4, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f), 1f, Color.HotPink * 0.75f, LightSource.LightContext.None, 0L, Location?.NameOrUniqueName);
				return;
			}
		}
		else if (Utility.IsNormalObjectAtParentSheetIndex(this, base.ItemId) || this is Torch)
		{
			if (base.QualifiedItemId == "(O)95" || ItemContextTagManager.HasBaseTag(base.QualifiedItemId, "torch_item"))
			{
				string qualifiedItemId = base.ItemId;
				lightSource = new LightSource(color: (qualifiedItemId == "94") ? Color.Yellow : ((!(qualifiedItemId == "95")) ? (new Color(1, 1, 1) * 0.9f) : (new Color(70, 0, 150) * 0.9f)), id: GenerateLightSourceId(tileLocation), textureIndex: 4, position: new Vector2(tileLocation.X * 64f + 16f, tileLocation.Y * 64f + 16f), radius: mineShaft ? 1.5f : 1.25f, lightContext: LightSource.LightContext.None, playerID: 0L, onlyLocation: Location?.NameOrUniqueName);
				return;
			}
			if (base.QualifiedItemId == "(O)746")
			{
				lightSource = new LightSource(GenerateLightSourceId(tileLocation), 4, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 48f), 0.5f, new Color(1, 1, 1) * 0.65f, LightSource.LightContext.None, 0L, Location?.NameOrUniqueName);
				return;
			}
			if (IsSprinkler() && base.SpecialVariable == 999999)
			{
				lightSource = new LightSource(GenerateLightSourceId(tileLocation), 4, new Vector2(tileLocation.X * 64f + 16f, tileLocation.Y * 64f + 16f), 1.25f, new Color(1, 1, 1) * 0.9f, LightSource.LightContext.None, 0L, Location?.NameOrUniqueName);
			}
		}
		if (MinutesUntilReady > 0)
		{
			MachineLight machineLight = GetMachineData()?.LightWhileWorking;
			if (machineLight != null)
			{
				lightSource = new LightSource(GenerateLightSourceId(tileLocation), 4, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f), machineLight.Radius, Utility.StringToColor(machineLight.Color) ?? Color.White, LightSource.LightContext.None, 0L, Location?.NameOrUniqueName);
			}
		}
	}

	public virtual void performRemoveAction()
	{
		GameLocation location = Location;
		Vector2 vector = TileLocation;
		if (location != null)
		{
			location.removeLightSource(lightSource?.Id);
			if (IsTapper() && location.terrainFeatures != null && location.terrainFeatures.TryGetValue(vector, out var value) && value is Tree tree)
			{
				tree.tapped.Value = false;
			}
			if (IsSprinkler())
			{
				location.removeTemporarySpritesWithID((int)vector.X * 4000 + (int)vector.Y);
			}
		}
		if (base.QualifiedItemId == "(BC)126")
		{
			string text = ((quality.Value != 0) ? (quality.Value - 1).ToString() : preservedParentSheetIndex.Value);
			if (text != null)
			{
				Game1.createItemDebris(new Hat(text), vector * 64f, (Game1.player.FacingDirection + 2) % 4);
				quality.Value = 0;
				preservedParentSheetIndex.Value = null;
			}
		}
		if (name.Contains("Seasonal") && bigCraftable.Value)
		{
			ResetParentSheetIndex();
		}
	}

	public virtual void dropItem(GameLocation location, Vector2 origin, Vector2 destination)
	{
		if ((Type == "Crafting" || Type == "interactive") && fragility.Value != 2)
		{
			location.debris.Add(new Debris(base.QualifiedItemId, origin, destination));
		}
	}

	public virtual bool isPassable()
	{
		if (isTemporarilyInvisible)
		{
			return true;
		}
		if (StardewValley.Pokemon.PokemonItemManager.IsPokeBall(base.QualifiedItemId))
		{
			return false;
		}
		if (bigCraftable.Value)
		{
			return false;
		}
		string qualifiedItemId = base.QualifiedItemId;
		if (qualifiedItemId != null)
		{
			int length = qualifiedItemId.Length;
			if (length <= 6)
			{
				if (length != 5)
				{
					if (length == 6)
					{
						switch (qualifiedItemId[5])
						{
						case '6':
							break;
						case '7':
							goto IL_00a5;
						case '8':
							goto IL_00c4;
						case '3':
							goto IL_00d3;
						case '4':
							goto IL_00e2;
						case '5':
							goto IL_00f1;
						case '0':
							goto IL_0100;
						default:
							goto IL_013c;
						}
						if (qualifiedItemId == "(O)286")
						{
							goto IL_013a;
						}
					}
				}
				else if (qualifiedItemId == "(O)93")
				{
					goto IL_013a;
				}
			}
			else if (length != 11)
			{
				if (length == 19 && qualifiedItemId == "(O)BlueGrassStarter")
				{
					goto IL_013a;
				}
			}
			else if (qualifiedItemId == "(O)SeedSpot")
			{
				goto IL_013a;
			}
		}
		goto IL_013c;
		IL_00d3:
		if (qualifiedItemId == "(O)893")
		{
			goto IL_013a;
		}
		goto IL_013c;
		IL_0100:
		if (qualifiedItemId == "(O)590")
		{
			goto IL_013a;
		}
		goto IL_013c;
		IL_00a5:
		if (qualifiedItemId == "(O)287" || qualifiedItemId == "(O)297")
		{
			goto IL_013a;
		}
		goto IL_013c;
		IL_00c4:
		if (qualifiedItemId == "(O)288")
		{
			goto IL_013a;
		}
		goto IL_013c;
		IL_00e2:
		if (qualifiedItemId == "(O)894")
		{
			goto IL_013a;
		}
		goto IL_013c;
		IL_013c:
		if (IsFloorPathItem())
		{
			return true;
		}
		if (base.Category == -74 || base.Category == -19)
		{
			if (isSapling())
			{
				return false;
			}
			switch (base.QualifiedItemId)
			{
			case "(O)301":
			case "(O)302":
			case "(O)473":
				return false;
			default:
				return true;
			}
		}
		return false;
		IL_00f1:
		if (qualifiedItemId == "(O)895")
		{
			goto IL_013a;
		}
		goto IL_013c;
		IL_013a:
		return true;
	}

	public virtual void reloadSprite()
	{
		initializeLightSource(tileLocation.Value);
	}

	/// <summary>Get the pixel area containing the object.</summary>
	public Microsoft.Xna.Framework.Rectangle GetBoundingBox()
	{
		Vector2 value = tileLocation.Value;
		return GetBoundingBoxAt((int)value.X, (int)value.Y);
	}

	/// <summary>Get the pixel area containing the object, adjusted for the given tile position.</summary>
	/// <param name="x">The tile X position to use instead of the object's current position.</param>
	/// <param name="y">The tile Y position to use instead of the object's current position.</param>
	public virtual Microsoft.Xna.Framework.Rectangle GetBoundingBoxAt(int x, int y)
	{
		Microsoft.Xna.Framework.Rectangle value = boundingBox.Value;
		if ((this is Torch && !bigCraftable.Value) || base.QualifiedItemId == "(O)590")
		{
			value.X = (int)tileLocation.X * 64 + 24;
			value.Y = (int)tileLocation.Y * 64 + 24;
		}
		else
		{
			value.X = (int)tileLocation.X * 64;
			value.Y = (int)tileLocation.Y * 64;
		}
		if (boundingBox.Value != value)
		{
			boundingBox.Value = value;
		}
		return value;
	}

	/// <inheritdoc />
	public override bool canBeGivenAsGift()
	{
		if (!bigCraftable.Value && !(this is Furniture) && !(this is Wallpaper))
		{
			if (Game1.objectData.TryGetValue(base.ItemId, out var value))
			{
				return value.CanBeGivenAsGift;
			}
			return true;
		}
		return false;
	}

	/// <summary>Update the object instance before it's placed in the world.</summary>
	/// <param name="who">The player placing the item.</param>
	/// <returns>Returns <c>true</c> if the item should be destroyed, or <c>false</c> if it should be set down.</returns>
	/// <remarks>This is called on the object instance being placed, after it's already been split from the inventory stack if applicable. At this point the <see cref="P:StardewValley.Object.Location" /> and <see cref="P:StardewValley.Object.TileLocation" /> values should already be set.</remarks>
	public virtual bool performDropDownAction(Farmer who)
	{
		if (who == null)
		{
			who = Game1.GetPlayer(owner.Value) ?? Game1.player;
		}
		GameLocation location = Location;
		MachineData machineData = GetMachineData();
		if (MachineDataUtility.TryGetMachineOutputRule(this, machineData, MachineOutputTrigger.MachinePutDown, null, who, location, out var rule, out var _, out var _, out var _))
		{
			OutputMachine(machineData, rule, null, who, location, probe: false);
			return false;
		}
		string qualifiedItemId = base.QualifiedItemId;
		if (!(qualifiedItemId == "(BC)96"))
		{
			if (qualifiedItemId == "(BC)99")
			{
				showNextIndex.Value = location.GetRootLocation().piecesOfHay.Value >= 0;
			}
		}
		else
		{
			minutesUntilReady.Value = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay, 3);
		}
		return false;
	}

	private void totemWarp(Farmer who)
	{
		GameLocation currentLocation = who.currentLocation;
		for (int i = 0; i < 12; i++)
		{
			Game1.multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(354, Game1.random.Next(25, 75), 6, 1, new Vector2(Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192), Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)), flicker: false, Game1.random.NextBool()));
		}
		who.playNearbySoundAll("wand");
		Game1.displayFarmer = false;
		Game1.player.temporarilyInvincible = true;
		Game1.player.temporaryInvincibilityTimer = -2000;
		Game1.player.freezePause = 1000;
		Game1.flashAlpha = 1f;
		DelayedAction.fadeAfterDelay(totemWarpForReal, 1000);
		Microsoft.Xna.Framework.Rectangle rectangle = who.GetBoundingBox();
		new Microsoft.Xna.Framework.Rectangle(rectangle.X, rectangle.Y, 64, 64).Inflate(192, 192);
		int num = 0;
		Point tilePoint = who.TilePoint;
		for (int num2 = tilePoint.X + 8; num2 >= tilePoint.X - 8; num2--)
		{
			Game1.multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(6, new Vector2(num2, tilePoint.Y) * 64f, Color.White, 8, flipped: false, 50f)
			{
				layerDepth = 1f,
				delayBeforeAnimationStart = num * 25,
				motion = new Vector2(-0.25f, 0f)
			});
			num++;
		}
	}

	private void totemWarpForReal()
	{
		switch (base.QualifiedItemId)
		{
		case "(O)688":
		{
			if (!Game1.getFarm().TryGetMapPropertyAs("WarpTotemEntry", out Point parsed, required: false))
			{
				parsed = Game1.whichFarm switch
				{
					6 => new Point(82, 29), 
					5 => new Point(48, 39), 
					_ => new Point(48, 7), 
				};
			}
			Game1.warpFarmer("Farm", parsed.X, parsed.Y, flip: false);
			break;
		}
		case "(O)689":
			Game1.warpFarmer("Mountain", 31, 20, flip: false);
			break;
		case "(O)690":
			Game1.warpFarmer("Beach", 20, 4, flip: false);
			break;
		case "(O)261":
			Game1.warpFarmer("Desert", 35, 43, flip: false);
			break;
		case "(O)886":
			Game1.warpFarmer("IslandSouth", 11, 11, flip: false);
			break;
		}
		Game1.fadeToBlackAlpha = 0.99f;
		Game1.screenGlow = false;
		Game1.player.temporarilyInvincible = false;
		Game1.player.temporaryInvincibilityTimer = 0;
		Game1.displayFarmer = true;
	}

	public void MonsterMusk(Farmer who)
	{
		GameLocation currentLocation = who.currentLocation;
		who.FarmerSprite.PauseForSingleAnimation = false;
		who.FarmerSprite.StopAnimation();
		who.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[4]
		{
			new FarmerSprite.AnimationFrame(104, 350, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(105, 350, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(104, 350, secondaryArm: false, flip: false),
			new FarmerSprite.AnimationFrame(105, 350, secondaryArm: false, flip: false)
		});
		currentLocation.playSound("croak");
		who.applyBuff("24");
	}

	public override void ModifyItemBuffs(BuffEffects effects)
	{
		if (effects != null && base.Category == -7)
		{
			int num = 0;
			if (base.Quality != 0)
			{
				num = 1;
			}
			if (num > 0)
			{
				NetFloat[] array = new NetFloat[9] { effects.FarmingLevel, effects.FishingLevel, effects.MiningLevel, effects.LuckLevel, effects.ForagingLevel, effects.MaxStamina, effects.MagneticRadius, effects.Defense, effects.Attack };
				foreach (NetFloat netFloat in array)
				{
					if (netFloat.Value != 0f)
					{
						netFloat.Value += num;
					}
				}
			}
		}
		base.ModifyItemBuffs(effects);
	}

	private void treasureTotem(Farmer who, GameLocation gameLocation)
	{
		Game1.playSound("treasure_totem");
		Game1.netWorldState.Value.TreasureTotemsUsed++;
		Vector2 tile = who.Tile;
		int num = 4;
		for (int i = (int)tile.X - num; (float)i < tile.X + (float)num; i++)
		{
			for (int j = (int)tile.Y - num; (float)j < tile.Y + (float)num; j++)
			{
				if (Math.Round(Utility.distance(i, tile.X, j, tile.Y)) == (double)(num - 1))
				{
					Vector2 vector = new Vector2(i, j);
					if (gameLocation.CanItemBePlacedHere(vector) && !gameLocation.IsTileOccupiedBy(vector) && !gameLocation.hasTileAt(i, j, "AlwaysFront") && !gameLocation.hasTileAt(i, j, "Front") && !gameLocation.isBehindBush(vector) && (gameLocation.doesTileHaveProperty(i, j, "Diggable", "Back") != null || (gameLocation.GetSeason() == Season.Winter && gameLocation.doesTileHaveProperty(i, j, "Type", "Back") == "Grass")))
					{
						if ((name.Equals("Forest") && i >= 93 && j <= 22) || !gameLocation.IsOutdoors)
						{
							continue;
						}
						gameLocation.objects.Add(vector, ItemRegistry.Create<Object>("(O)590"));
					}
					Utility.addRainbowStarExplosion(gameLocation, new Vector2(i, j) * 64f, 1);
					Utility.addStarsAndSpirals(gameLocation, i, j, 1, 1, 100, 100, Color.White);
				}
				if (Math.Round(Utility.distance(i, tile.X, j, tile.Y)) <= (double)(num - 1))
				{
					Utility.makeTemporarySpriteJuicier(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(144, 249, 7, 7), Game1.random.Next(100, 200), 6, 1, new Vector2(i, j) * 64f + new Vector2(32 + Game1.random.Next(-16, 16), 32 + Game1.random.Next(-16, 16)), flicker: false, flipped: false, 0.001f, 0f, (Game1.random.NextDouble() < 0.5) ? new Color(255, 255, 100) : Color.White, 4f, 0f, 0f, 0f), gameLocation);
				}
			}
		}
	}

	private void rainTotem(Farmer who)
	{
		GameLocation currentLocation = who.currentLocation;
		string text = currentLocation.GetLocationContextId();
		LocationContextData locationContext = currentLocation.GetLocationContext();
		if (!locationContext.AllowRainTotem)
		{
			Game1.showRedMessageUsingLoadString("Strings\\UI:Item_CantBeUsedHere");
			return;
		}
		if (locationContext.RainTotemAffectsContext != null)
		{
			text = locationContext.RainTotemAffectsContext;
		}
		bool flag = false;
		if (text == "Default")
		{
			if (!Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.season))
			{
				Game1.netWorldState.Value.WeatherForTomorrow = (Game1.weatherForTomorrow = "Rain");
				flag = true;
			}
		}
		else
		{
			currentLocation.GetWeather().WeatherForTomorrow = "Rain";
			flag = true;
		}
		if (flag)
		{
			Game1.pauseThenMessage(2000, Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12822"));
		}
		Game1.screenGlow = false;
		currentLocation.playSound("thunder");
		who.canMove = false;
		Game1.screenGlowOnce(Color.SlateBlue, hold: false);
		Game1.player.faceDirection(2);
		Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[1]
		{
			new FarmerSprite.AnimationFrame(57, 2000, secondaryArm: false, flip: false, Farmer.canMoveNow, behaviorAtEndOfFrame: true)
		});
		for (int i = 0; i < 6; i++)
		{
			Game1.multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 1045, 52, 33), 9999f, 1, 999, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 2f, 0.01f, 0f, 0f)
			{
				motion = new Vector2((float)Game1.random.Next(-10, 11) / 10f, -2f),
				delayBeforeAnimationStart = i * 200
			});
			Game1.multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 1045, 52, 33), 9999f, 1, 999, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 1f, 0.01f, 0f, 0f)
			{
				motion = new Vector2((float)Game1.random.Next(-30, -10) / 10f, -1f),
				delayBeforeAnimationStart = 100 + i * 200
			});
			Game1.multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 1045, 52, 33), 9999f, 1, 999, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 1f, 0.01f, 0f, 0f)
			{
				motion = new Vector2((float)Game1.random.Next(10, 30) / 10f, -1f),
				delayBeforeAnimationStart = 200 + i * 200
			});
		}
		TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(0f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
		{
			motion = new Vector2(0f, -7f),
			acceleration = new Vector2(0f, 0.1f),
			scaleChange = 0.015f,
			alpha = 1f,
			alphaFade = 0.0075f,
			shakeIntensity = 1f,
			initialPosition = Game1.player.Position + new Vector2(0f, -96f),
			xPeriodic = true,
			xPeriodicLoopTime = 1000f,
			xPeriodicRange = 4f,
			layerDepth = 1f
		};
		temporaryAnimatedSprite.CopyAppearanceFromItemId(base.QualifiedItemId);
		Game1.multiplayer.broadcastSprites(currentLocation, temporaryAnimatedSprite);
		DelayedAction.playSoundAfterDelay("rainsound", 2000);
	}

	private void readBook(GameLocation location)
	{
		Game1.player.canMove = false;
		Game1.player.freezePause = 1030;
		Game1.player.faceDirection(2);
		Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[1]
		{
			new FarmerSprite.AnimationFrame(57, 1000, secondaryArm: false, flip: false, Farmer.canMoveNow, behaviorAtEndOfFrame: true)
			{
				frameEndBehavior = delegate
				{
					location.removeTemporarySpritesWithID(1987654);
					Utility.addRainbowStarExplosion(location, Game1.player.getStandingPosition() + new Vector2(-40f, -156f), 8);
				}
			}
		});
		Game1.MusicDuckTimer = 4000f;
		Game1.playSound("book_read");
		Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Book_Animation", new Microsoft.Xna.Framework.Rectangle(0, 0, 20, 20), 10f, 45, 1, Game1.player.getStandingPosition() + new Vector2(-48f, -156f), flicker: false, flipped: false, Game1.player.getDrawLayer() + 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f)
		{
			holdLastFrame = true,
			id = 1987654
		});
		Color? colorFromTags = ItemContextTagManager.GetColorFromTags(this);
		if (colorFromTags.HasValue)
		{
			Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Book_Animation", new Microsoft.Xna.Framework.Rectangle(0, 20, 20, 20), 10f, 45, 1, Game1.player.getStandingPosition() + new Vector2(-48f, -156f), flicker: false, flipped: false, Game1.player.getDrawLayer() + 0.0012f, 0f, colorFromTags.Value, 4f, 0f, 0f, 0f)
			{
				holdLastFrame = true,
				id = 1987654
			});
		}
		if (base.ItemId.StartsWith("SkillBook_"))
		{
			int count = Game1.player.newLevels.Count;
			Game1.player.gainExperience(Convert.ToInt32(base.ItemId.Last().ToString() ?? ""), 250);
			if (Game1.player.newLevels.Count == count || (Game1.player.newLevels.Count > 1 && count >= 1))
			{
				DelayedAction.functionAfterDelay(delegate
				{
					Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:SkillBookMessage", Game1.content.LoadString("Strings\\1_6_Strings:SkillName_" + base.ItemId.Last()).ToLower()));
				}, 1000);
			}
			return;
		}
		if (Game1.player.stats.Get(itemId.Value) != 0 && base.ItemId != "Book_PriceCatalogue" && base.ItemId != "Book_AnimalCatalogue")
		{
			if (!Game1.player.mailReceived.Contains("read_a_book"))
			{
				Game1.player.mailReceived.Add("read_a_book");
			}
			bool flag = false;
			foreach (string contextTag in GetContextTags())
			{
				if (contextTag.StartsWithIgnoreCase("book_xp_"))
				{
					flag = true;
					string text = contextTag.Split('_')[2];
					Game1.player.gainExperience(Farmer.getSkillNumberFromName(text), 100);
					break;
				}
			}
			if (!flag)
			{
				for (int num = 0; num < 5; num++)
				{
					Game1.player.gainExperience(num, 20);
				}
			}
			return;
		}
		string text2 = base.ItemId;
		if (!(text2 == "Book_QueenOfSauce"))
		{
			if (text2 == "PurpleBook")
			{
				Game1.player.gainExperience(0, 250);
				Game1.player.gainExperience(1, 250);
				Game1.player.gainExperience(2, 250);
				Game1.player.gainExperience(3, 250);
				Game1.player.gainExperience(4, 250);
				return;
			}
			Game1.player.stats.Increment(itemId.Value);
			DelayedAction.functionAfterDelay(delegate
			{
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:LearnedANewPower"));
			}, 1000);
			if (!Game1.player.mailReceived.Contains("read_a_book"))
			{
				Game1.player.mailReceived.Add("read_a_book");
			}
			Game1.stats.checkForBooksReadAchievement();
			return;
		}
		Dictionary<string, string> dictionary = DataLoader.Tv_CookingChannel(Game1.content);
		int num2 = 0;
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			if (Game1.player.cookingRecipes.TryAdd(item.Value.Split("/")[0], 0))
			{
				num2++;
			}
		}
		Game1.player.stats.Increment(itemId.Value);
		Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:QoS_Cookbook", num2.ToString() ?? ""));
	}

	public virtual bool performUseAction(GameLocation location)
	{
		if (!Game1.player.canMove || isTemporarilyInvisible)
		{
			return false;
		}

		if (StardewValley.Pokemon.PokemonItemUse.IsUsable(base.QualifiedItemId))
		{
			return StardewValley.Pokemon.PokemonItemUse.Use(base.QualifiedItemId);
		}

		if (base.QualifiedItemId == "(O)PokemonSuperRod")
		{
			for (int i = 0; i < Game1.player.Items.Count; i++)
			{
				if (Game1.player.Items[i] == this)
				{
					Game1.player.Items[i] = ItemRegistry.Create("(T)PokemonSuperRod");
					break;
				}
			}
			Game1.playSound("coin");
			Game1.addHUDMessage(new HUDMessage("Super Rod convertida em Vara de Pesca!", 2));
			return true;
		}

		if (base.QualifiedItemId == "(O)PokemonHealingMachine" || base.QualifiedItemId == "(O)PokemonPC" || base.QualifiedItemId == "(O)PokemonTradeTerminal")
		{
			// These are placeable crafting objects. Do NOT consume them from the player's hand on right-click!
			// Game1 will perform placementAction on the ground tile instead.
			return false;
		}

		bool flag = !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack && !Game1.player.swimming.Value && !Game1.player.bathingClothes.Value && !Game1.player.onBridge.Value;
		if (flag && (base.Category == -102 || base.Category == -103))
		{
			readBook(location);
			return true;
		}
		if (name.Contains("Totem"))
		{
			if (flag)
			{
				switch (base.QualifiedItemId)
				{
				case "(O)TreasureTotem":
					if (!location.IsOutdoors)
					{
						Game1.showRedMessageUsingLoadString("Strings\\StringsFromCSFiles:Object.cs.13053");
						return false;
					}
					treasureTotem(Game1.player, location);
					return true;
				case "(O)681":
					rainTotem(Game1.player);
					return true;
				case "(O)261":
				case "(O)688":
				case "(O)689":
				case "(O)690":
				case "(O)886":
				{
					Game1.player.jitterStrength = 1f;
					Color glowColor = ((base.QualifiedItemId == "(O)681") ? Color.SlateBlue : ((base.QualifiedItemId == "(O)688") ? Color.LimeGreen : ((base.QualifiedItemId == "(O)689") ? Color.OrangeRed : ((base.QualifiedItemId == "(O)261") ? new Color(255, 200, 0) : Color.LightBlue))));
					location.playSound("warrior");
					Game1.player.faceDirection(2);
					Game1.player.CanMove = false;
					Game1.player.temporarilyInvincible = true;
					Game1.player.temporaryInvincibilityTimer = -4000;
					Game1.changeMusicTrack("silence");
					if (base.QualifiedItemId == "(O)681")
					{
						Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[2]
						{
							new FarmerSprite.AnimationFrame(57, 2000, secondaryArm: false, flip: false),
							new FarmerSprite.AnimationFrame((short)Game1.player.FarmerSprite.CurrentFrame, 0, secondaryArm: false, flip: false, rainTotem, behaviorAtEndOfFrame: true)
						});
					}
					else
					{
						Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[2]
						{
							new FarmerSprite.AnimationFrame(57, 2000, secondaryArm: false, flip: false),
							new FarmerSprite.AnimationFrame((short)Game1.player.FarmerSprite.CurrentFrame, 0, secondaryArm: false, flip: false, totemWarp, behaviorAtEndOfFrame: true)
						});
					}
					TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(0f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
					{
						motion = new Vector2(0f, -1f),
						scaleChange = 0.01f,
						alpha = 1f,
						alphaFade = 0.0075f,
						shakeIntensity = 1f,
						initialPosition = Game1.player.Position + new Vector2(0f, -96f),
						xPeriodic = true,
						xPeriodicLoopTime = 1000f,
						xPeriodicRange = 4f,
						layerDepth = 1f
					};
					temporaryAnimatedSprite.CopyAppearanceFromItemId(base.QualifiedItemId);
					Game1.multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
					temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(-64f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
					{
						motion = new Vector2(0f, -0.5f),
						scaleChange = 0.005f,
						scale = 0.5f,
						alpha = 1f,
						alphaFade = 0.0075f,
						shakeIntensity = 1f,
						delayBeforeAnimationStart = 10,
						initialPosition = Game1.player.Position + new Vector2(-64f, -96f),
						xPeriodic = true,
						xPeriodicLoopTime = 1000f,
						xPeriodicRange = 4f,
						layerDepth = 0.9999f
					};
					temporaryAnimatedSprite.CopyAppearanceFromItemId(base.QualifiedItemId);
					Game1.multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
					temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(64f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
					{
						motion = new Vector2(0f, -0.5f),
						scaleChange = 0.005f,
						scale = 0.5f,
						alpha = 1f,
						alphaFade = 0.0075f,
						delayBeforeAnimationStart = 20,
						shakeIntensity = 1f,
						initialPosition = Game1.player.Position + new Vector2(64f, -96f),
						xPeriodic = true,
						xPeriodicLoopTime = 1000f,
						xPeriodicRange = 4f,
						layerDepth = 0.9988f
					};
					temporaryAnimatedSprite.CopyAppearanceFromItemId(base.QualifiedItemId);
					Game1.multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
					Game1.screenGlowOnce(glowColor, hold: false);
					Utility.addSprinklesToLocation(location, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, 16, 16, 1300, 20, Color.White, null, motionTowardCenter: true);
					return true;
				}
				}
			}
		}
		else if (base.QualifiedItemId == "(O)79" || base.QualifiedItemId == "(O)842")
		{
			bool flag2 = base.QualifiedItemId == "(O)842";
			int totalNotes;
			int[] unseenSecretNotes = Utility.GetUnseenSecretNotes(Game1.player, flag2, out totalNotes);
			if (unseenSecretNotes.Length == 0)
			{
				return false;
			}
			Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.player.UniqueMultiplayerID, unseenSecretNotes.Length * 777);
			int num = (flag2 ? unseenSecretNotes.Min() : random.ChooseFrom(unseenSecretNotes));
			if (!Game1.player.secretNotesSeen.Add(num))
			{
				return false;
			}
			switch (num)
			{
			case 23:
				if (!Game1.player.eventsSeen.Contains("2120303"))
				{
					Game1.player.addQuest("29");
				}
				break;
			case 10:
				if (!Game1.player.mailReceived.Contains("qiCave"))
				{
					Game1.player.addQuest("30");
				}
				break;
			}
			Game1.activeClickableMenu = new LetterViewerMenu(num);
			return true;
		}
		if (base.QualifiedItemId == "(O)911")
		{
			if (!flag)
			{
				return false;
			}
			string horseWarpErrorMessage = Utility.GetHorseWarpErrorMessage(Utility.GetHorseWarpRestrictionsForFarmer(Game1.player));
			if (horseWarpErrorMessage == null)
			{
				Horse horse = null;
				foreach (NPC character in location.characters)
				{
					if (character is Horse horse2 && horse2.getOwner() == Game1.player)
					{
						horse = horse2;
						break;
					}
				}
				if (horse == null || Math.Abs(Game1.player.TilePoint.X - horse.TilePoint.X) > 1 || Math.Abs(Game1.player.TilePoint.Y - horse.TilePoint.Y) > 1)
				{
					Game1.player.faceDirection(2);
					Game1.MusicDuckTimer = 2000f;
					Game1.playSound("horse_flute");
					Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[6]
					{
						new FarmerSprite.AnimationFrame(98, 400, secondaryArm: true, flip: false),
						new FarmerSprite.AnimationFrame(99, 200, secondaryArm: true, flip: false),
						new FarmerSprite.AnimationFrame(100, 200, secondaryArm: true, flip: false),
						new FarmerSprite.AnimationFrame(99, 200, secondaryArm: true, flip: false),
						new FarmerSprite.AnimationFrame(98, 400, secondaryArm: true, flip: false),
						new FarmerSprite.AnimationFrame(99, 200, secondaryArm: true, flip: false)
					});
					Game1.player.freezePause = 1500;
					DelayedAction.functionAfterDelay(delegate
					{
						string horseWarpErrorMessage2 = Utility.GetHorseWarpErrorMessage(Utility.GetHorseWarpRestrictionsForFarmer(Game1.player));
						if (horseWarpErrorMessage2 != null)
						{
							Game1.showRedMessage(horseWarpErrorMessage2);
						}
						else
						{
							Game1.player.team.requestHorseWarpEvent.Fire(Game1.player.UniqueMultiplayerID);
						}
					}, 1500);
				}
				stack.Value += 1;
				return true;
			}
			Game1.showRedMessage(horseWarpErrorMessage);
		}
		if (base.QualifiedItemId == "(O)879")
		{
			if (!flag)
			{
				return false;
			}
			Game1.player.faceDirection(2);
			Game1.player.freezePause = 1750;
			Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[2]
			{
				new FarmerSprite.AnimationFrame(57, 750, secondaryArm: false, flip: false),
				new FarmerSprite.AnimationFrame((short)Game1.player.FarmerSprite.CurrentFrame, 0, secondaryArm: false, flip: false, MonsterMusk, behaviorAtEndOfFrame: true)
			});
			for (int num2 = 0; num2 < 3; num2++)
			{
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(5, new Vector2(16f, -64 + 32 * num2), Color.Purple)
				{
					motion = new Vector2(Utility.RandomFloat(-1f, 1f), -0.5f),
					scaleChange = 0.005f,
					scale = 0.5f,
					alpha = 1f,
					alphaFade = 0.0075f,
					shakeIntensity = 1f,
					delayBeforeAnimationStart = 100 * num2,
					layerDepth = 0.9999f,
					positionFollowsAttachedCharacter = true,
					attachedCharacter = Game1.player
				});
			}
			location.playSound("steam");
			return true;
		}
		return false;
	}

	/// <inheritdoc />
	public override Color getCategoryColor()
	{
		if (type.Value == "Arch")
		{
			return new Color(110, 0, 90);
		}
		return base.getCategoryColor();
	}

	/// <inheritdoc />
	public override string getCategoryName()
	{
		if (this is Furniture { placementRestriction: var placementRestriction })
		{
			return placementRestriction switch
			{
				1 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture_Outdoors"), 
				2 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture_Decoration"), 
				_ => Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12847"), 
			};
		}
		if (Type == "Arch")
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12849");
		}
		return base.getCategoryName();
	}

	/// <summary>Get the translated display name for an object category, if any.</summary>
	/// <param name="category">The object category.</param>
	/// <returns>Returns the display name, or an empty string if there is none.</returns>
	public static string GetCategoryDisplayName(int category)
	{
		switch (category)
		{
		case -97:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Boots.cs.12501");
		case -100:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:category_clothes");
		case -96:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Ring.cs.1");
		case -99:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs.14307");
		case -12:
		case -2:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12850");
		case -75:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12851");
		case -4:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12852");
		case -25:
		case -7:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12853");
		case -79:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12854");
		case -74:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12855");
		case -19:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12856");
		case -21:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12857");
		case -22:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12858");
		case -24:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12859");
		case -20:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12860");
		case -27:
		case -26:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12862");
		case -8:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12863");
		case -18:
		case -14:
		case -6:
		case -5:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12864");
		case -80:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12866");
		case -28:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12867");
		case -16:
		case -15:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12868");
		case -81:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12869");
		case -102:
			return Game1.content.LoadString("Strings\\1_6_Strings:Book_Category");
		case -103:
			return Game1.content.LoadString("Strings\\1_6_Strings:skillBook_Category");
		default:
			return "";
		}
	}

	/// <summary>Get the display color for an object category.</summary>
	/// <param name="category">The object category.</param>
	public static Color GetCategoryColor(int category)
	{
		switch (category)
		{
		case -12:
		case -2:
			return new Color(110, 0, 90);
		case -75:
			return Color.Green;
		case -4:
			return Color.DarkBlue;
		case -7:
			return new Color(220, 60, 0);
		case -79:
			return Color.DeepPink;
		case -74:
			return Color.Brown;
		case -19:
			return Color.SlateGray;
		case -21:
			return Color.DarkRed;
		case -22:
			return Color.DarkCyan;
		case -24:
			return new Color(150, 80, 190);
		case -20:
			return Color.DimGray;
		case -27:
		case -26:
			return new Color(0, 155, 111);
		case -8:
			return new Color(148, 61, 40);
		case -18:
		case -14:
		case -6:
		case -5:
			return new Color(255, 0, 100);
		case -80:
			return new Color(219, 54, 211);
		case -28:
			return new Color(50, 10, 70);
		case -16:
		case -15:
			return new Color(64, 102, 114);
		case -81:
			return new Color(10, 130, 50);
		case -102:
			return new Color(85, 47, 27);
		case -103:
			return new Color(122, 93, 39);
		default:
			return Color.Black;
		}
	}

	public virtual bool isActionable(Farmer who)
	{
		if (!isTemporarilyInvisible)
		{
			return checkForAction(who, justCheckingForActivity: true);
		}
		return false;
	}

	public int getHealth()
	{
		return health;
	}

	public void setHealth(int health)
	{
		this.health = health;
	}

	protected virtual void grabItemFromAutoGrabber(Item item, Farmer who)
	{
		if (heldObject.Value is Chest chest)
		{
			if (who.couldInventoryAcceptThisItem(item))
			{
				chest.Items.Remove(item);
				chest.clearNulls();
				Game1.activeClickableMenu = new ItemGrabMenu(chest.Items, reverseGrab: false, showReceivingMenu: true, InventoryMenu.highlightAllItems, chest.grabItemFromInventory, null, grabItemFromAutoGrabber, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: true, 1, this, -1, this);
			}
			if (chest.isEmpty())
			{
				showNextIndex.Value = false;
			}
		}
	}

	public static bool HighlightFertilizers(Item i)
	{
		return i.Category == -19;
	}

	public override int healthRecoveredOnConsumption()
	{
		if (Edibility < 0)
		{
			return 0;
		}
		switch (base.QualifiedItemId)
		{
		case "(O)874":
			return (int)((float)staminaRecoveredOnConsumption() * 0.68f);
		case "(O)434":
		case "(O)349":
			return 0;
		case "(O)773":
			return 999;
		default:
			return (int)((float)staminaRecoveredOnConsumption() * 0.45f);
		}
	}

	public override int staminaRecoveredOnConsumption()
	{
		string qualifiedItemId = base.QualifiedItemId;
		if (!(qualifiedItemId == "(O)773"))
		{
			if (qualifiedItemId == "(O)434")
			{
				return 999;
			}
			return (int)Math.Ceiling((double)Edibility * 2.5) + base.Quality * Edibility;
		}
		return 0;
	}

	/// <summary>Perform an action when the user interacts with this object.</summary>
	/// <param name="who">The player interacting with the object.</param>
	/// <param name="justCheckingForActivity">Whether to check if an action would be performed, without actually doing it. Setting this to true may have inconsistent effects depending on the action.</param>
	/// <returns>Returns true if the action was performed, or false if the player should pick up the item instead.</returns>
	public virtual bool checkForAction(Farmer who, bool justCheckingForActivity = false)
	{
		if (isTemporarilyInvisible)
		{
			return true;
		}
		if (!justCheckingForActivity && who != null)
		{
			GameLocation location = Location;
			Point tilePoint = who.TilePoint;
			if (location.isObjectAtTile(tilePoint.X, tilePoint.Y - 1) && location.isObjectAtTile(tilePoint.X, tilePoint.Y + 1) && location.isObjectAtTile(tilePoint.X + 1, tilePoint.Y) && location.isObjectAtTile(tilePoint.X - 1, tilePoint.Y) && !location.getObjectAtTile(tilePoint.X, tilePoint.Y - 1).isPassable() && !location.getObjectAtTile(tilePoint.X, tilePoint.Y + 1).isPassable() && !location.getObjectAtTile(tilePoint.X - 1, tilePoint.Y).isPassable() && !location.getObjectAtTile(tilePoint.X + 1, tilePoint.Y).isPassable())
			{
				performToolAction(null);
			}
		}
		if (StardewValley.Pokemon.PokemonItemManager.IsPokeBall(base.QualifiedItemId))
		{
			if (!justCheckingForActivity && who != null)
			{
				Item ballItem = ItemRegistry.Create(base.QualifiedItemId, 1);
				if (who.addItemToInventoryBool(ballItem))
				{
					Game1.playSound("coin");
					Location.removeObject(TileLocation, showDestroyedObject: false);
					return true;
				}
				else
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
				}
			}
			return true;
		}

		if ((base.QualifiedItemId == "(BC)101" || base.QualifiedItemId == "(BC)104" || (GetMachineData() != null && GetMachineData().IsIncubator) || (base.Name != null && base.Name.Contains("Incubator"))))
		{
			if (heldObject.Value == null && !StardewValley.Pokemon.PokemonBreeding.HasIncubatingEgg(TileLocation))
			{
				var party = StardewValley.Pokemon.PlayerParty.Instance;
				var activeEgg = (party?.ActivePokemon != null && party.ActivePokemon.IsEgg)
					? party.ActivePokemon
					: party?.Team?.Find(p => p != null && p.IsEgg);

				if (activeEgg != null)
				{
					if (justCheckingForActivity) return true;
					party.RemovePokemon(activeEgg);
					party.SaveParty();

					heldObject.Value = ItemRegistry.Create<Object>("(O)107") ?? new Object("107", 1);
					StardewValley.Pokemon.PokemonBreeding.AddIncubatingEgg(TileLocation, activeEgg);
					MinutesUntilReady = 480;
					showNextIndex.Value = true;
					Game1.playSound("Ship");
					Game1.addHUDMessage(new HUDMessage($"🥚 Ovo de {activeEgg.Species?.Name ?? "Pokémon"} colocado na chocadeira!", 2));
					return true;
				}
			}
			else if (StardewValley.Pokemon.PokemonBreeding.HasIncubatingEgg(TileLocation))
			{
				if (readyForHarvest.Value || MinutesUntilReady <= 0)
				{
					if (!justCheckingForActivity)
					{
						var hatchedEgg = StardewValley.Pokemon.PokemonBreeding.GetIncubatingEgg(TileLocation);
						StardewValley.Pokemon.PokemonBreeding.RemoveIncubatingEgg(TileLocation);
						heldObject.Value = null;
						readyForHarvest.Value = false;
						showNextIndex.Value = false;
						MinutesUntilReady = 0;
						if (hatchedEgg != null)
						{
							Game1.activeClickableMenu = new StardewValley.Pokemon.PokemonHatchMenu(hatchedEgg);
						}
					}
					return true;
				}
				else
				{
					if (!justCheckingForActivity)
					{
						Game1.addHUDMessage(new HUDMessage($"🥚 O Ovo Pokémon está sendo incubado... ({MinutesUntilReady}m restantes)", 1));
					}
					return true;
				}
			}
		}

		switch (base.QualifiedItemId)
		{
		case "(O)PokemonHealingMachine":
		case "(BC)PokemonHealingMachine":
			if (!justCheckingForActivity)
			{
				StardewValley.Pokemon.PlayerParty.Instance.HealAll();
				Game1.playSound("reward");
				Game1.playSound("heal");
				Utility.addStarsAndSpirals(Location, (int)TileLocation.X, (int)TileLocation.Y, 1, 1, 80, 20, Color.Pink);
				Game1.addHUDMessage(new HUDMessage("Mesa de Recuperação: Toda a equipe Pokémon foi restaurada!", 2));
			}
			return true;
		case "(O)PokemonPC":
		case "(BC)PokemonPC":
			if (!justCheckingForActivity)
			{
				Game1.playSound("computer");
				Game1.activeClickableMenu = new StardewValley.Pokemon.PokemonStorageMenu();
			}
			return true;
		case "(O)PokemonTradeTerminal":
		case "(BC)PokemonTradeTerminal":
			if (!justCheckingForActivity)
			{
				Game1.playSound("computer");
				Game1.activeClickableMenu = new StardewValley.Pokemon.PokemonTradeMenu();
			}
			return true;
		case "(O)PotOfGold":
			if (!justCheckingForActivity)
			{
				Game1.playSound("hammer");
				Game1.playSound("moneyDial");
				Game1.createMultipleItemDebris(ItemRegistry.Create("(O)GoldCoin", Math.Min(100, 7 + Game1.year)), TileLocation * 64f + new Vector2(32f), 1);
				Game1.createMultipleItemDebris(ItemRegistry.Create("(H)LeprechuanHat"), TileLocation * 64f + new Vector2(32f), 1);
				Location.removeObject(TileLocation, showDestroyedObject: false);
				Utility.addDirtPuffs(Location, (int)TileLocation.X, (int)TileLocation.Y, 1, 1, 3);
				Utility.addStarsAndSpirals(Location, (int)TileLocation.X, (int)TileLocation.Y, 1, 1, 100, 30, Color.White);
			}
			return true;
		case "(BC)MiniForge":
			if (!justCheckingForActivity)
			{
				Game1.activeClickableMenu = new ForgeMenu();
			}
			return true;
		case "(BC)StatueOfTheDwarfKing":
			if (!justCheckingForActivity)
			{
				if (who.stats.Get(StatKeys.Mastery(3)) < 1)
				{
					Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:MasteryRequirement"));
					Game1.playSound("cancel");
				}
				else if (!who.hasBuffWithNameContainingString("dwarfStatue"))
				{
					Game1.activeClickableMenu = new ChooseFromIconsMenu("dwarfStatue");
					(Game1.activeClickableMenu as ChooseFromIconsMenu).sourceObject = this;
				}
				else
				{
					shakeTimer = 400;
					Game1.playSound("cancel");
				}
			}
			return true;
		case "(BC)StatueOfBlessings":
			return CheckForActionOnBlessedStatue(who, who.currentLocation, justCheckingForActivity);
		case "(BC)0":
		case "(BC)1":
		case "(BC)2":
		case "(BC)3":
		case "(BC)4":
		case "(BC)5":
		case "(BC)6":
		case "(BC)7":
			return CheckForActionOnHousePlant(who, justCheckingForActivity);
		case "(BC)56":
			return CheckForActionOnSlimeBall(who, justCheckingForActivity);
		case "(BC)71":
			return CheckForActionOnStaircase(who, justCheckingForActivity);
		case "(BC)94":
			return CheckForActionOnSingingStone(who, justCheckingForActivity);
		case "(BC)99":
			return CheckForActionOnFeedHopper(who, justCheckingForActivity);
		case "(BC)141":
			return CheckForActionOnPrairieKingArcadeSystem(who, justCheckingForActivity);
		case "(BC)159":
			return CheckForActionOnJunimoKartArcadeSystem(who, justCheckingForActivity);
		case "(BC)165":
			return CheckForActionOnAutoGrabber(who, justCheckingForActivity);
		case "(BC)238":
			return CheckForActionOnMiniObelisk(who, justCheckingForActivity);
		case "(BC)239":
			return CheckForActionOnFarmComputer(who, justCheckingForActivity);
		case "(BC)247":
			return CheckForActionOnSewingMachine(who, justCheckingForActivity);
		case "(O)464":
			return CheckForActionOnFluteBlock(who, justCheckingForActivity);
		case "(O)463":
			return CheckForActionOnDrumBlock(who, justCheckingForActivity);
		default:
			if (IsSprinkler() && CheckForActionOnSprinkler(who, justCheckingForActivity))
			{
				return true;
			}
			if (IsScarecrow() && CheckForActionOnScarecrow(who, justCheckingForActivity))
			{
				return true;
			}
			if (IsTextSign() && CheckForActionOnTextSign(who, justCheckingForActivity))
			{
				return true;
			}
			return CheckForActionOnMachine(who, justCheckingForActivity);
		}
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a sewing machine.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnSewingMachine(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		Game1.activeClickableMenu = new TailoringMenu();
		return true;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's an auto-grabber.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnAutoGrabber(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		if (heldObject.Value is Chest chest && !chest.isEmpty())
		{
			Game1.activeClickableMenu = new ItemGrabMenu(chest.Items, reverseGrab: false, showReceivingMenu: true, InventoryMenu.highlightAllItems, chest.grabItemFromInventory, null, grabItemFromAutoGrabber, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: true, 1, null, -1, this);
			return true;
		}
		return false;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a farm computer.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnFarmComputer(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		shakeTimer = 500;
		Location.localSound("DwarvishSentry");
		who.freezePause = 500;
		DelayedAction.functionAfterDelay(delegate
		{
			ShowFarmComputerReport(who);
		}, 500);
		return true;
	}

	/// <summary>Show a farm computer analysis for a player's current location.</summary>
	/// <param name="who">The player viewing the farm report.</param>
	protected virtual void ShowFarmComputerReport(Farmer who)
	{
		GameLocation rootLocation = (Location ?? who.currentLocation).GetRootLocation();
		Farm farm = rootLocation as Farm;
		bool num = rootLocation.IsBuildableLocation() || rootLocation.buildings.Any();
		string text = rootLocation.GetDisplayName();
		int totalCrops = rootLocation.getTotalCrops();
		int totalOpenHoeDirt = rootLocation.getTotalOpenHoeDirt();
		int totalCropsReadyForHarvest = rootLocation.getTotalCropsReadyForHarvest();
		int totalUnwateredCrops = rootLocation.getTotalUnwateredCrops();
		int? num2 = (rootLocation.HasMinBuildings("Greenhouse", 1) ? rootLocation.getTotalGreenhouseCropsReadyForHarvest() : ((int?)null));
		int totalForageItems = rootLocation.getTotalForageItems();
		int numberOfMachinesReadyForHarvest = rootLocation.getNumberOfMachinesReadyForHarvest();
		bool? flag = farm?.doesFarmCaveNeedHarvesting();
		StringBuilder stringBuilder = new StringBuilder();
		if (rootLocation is Farm)
		{
			stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_Intro_Farm", Game1.player.farmName.Value));
		}
		else if (!string.IsNullOrWhiteSpace(text))
		{
			stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_Intro_NamedLocation", text));
		}
		else
		{
			stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_Intro_Generic"));
		}
		stringBuilder.Append("^--------------^");
		if (num)
		{
			stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_PiecesHay", rootLocation.piecesOfHay, rootLocation.GetHayCapacity())).Append(" ^");
		}
		stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_TotalCrops", totalCrops)).Append("  ^").Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_CropsReadyForHarvest", totalCropsReadyForHarvest))
			.Append("  ^")
			.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_CropsUnwatered", totalUnwateredCrops))
			.Append("  ^");
		if (num2.HasValue)
		{
			stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_CropsReadyForHarvest_Greenhouse", num2)).Append("  ^");
		}
		stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_TotalOpenHoeDirt", totalOpenHoeDirt)).Append("  ^");
		if (farm == null || farm.SpawnsForage())
		{
			stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_TotalForage", totalForageItems)).Append("  ^");
		}
		stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_MachinesReady", numberOfMachinesReadyForHarvest)).Append("  ^");
		if (flag.HasValue)
		{
			stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:FarmComputer_FarmCave", flag.Value ? Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes") : Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")));
		}
		Game1.multipleDialogues(new string[1] { stringBuilder.ToString() });
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a mini-obelisk.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnMiniObelisk(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		GameLocation location = Location;
		Vector2 vector = Vector2.Zero;
		Vector2 vector2 = Vector2.Zero;
		foreach (KeyValuePair<Vector2, Object> pair in location.objects.Pairs)
		{
			if (pair.Value.bigCraftable.Value && pair.Value.QualifiedItemId == "(BC)238")
			{
				if (vector == Vector2.Zero)
				{
					vector = pair.Key;
				}
				else if (vector2 == Vector2.Zero)
				{
					vector2 = pair.Key;
					break;
				}
			}
		}
		if (vector2 == Vector2.Zero)
		{
			Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:MiniObelisk_NeedsPair"));
			return false;
		}
		Vector2 vector3 = ((Vector2.Distance(who.Tile, vector) > Vector2.Distance(who.Tile, vector2)) ? vector : vector2);
		Vector2[] array = new Vector2[4]
		{
			new Vector2(vector3.X, vector3.Y + 1f),
			new Vector2(vector3.X - 1f, vector3.Y),
			new Vector2(vector3.X + 1f, vector3.Y),
			new Vector2(vector3.X, vector3.Y - 1f)
		};
		foreach (Vector2 v in array)
		{
			if (!location.IsTileBlockedBy(v, CollisionMask.All, CollisionMask.All))
			{
				for (int j = 0; j < 12; j++)
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite(354, Game1.random.Next(25, 75), 6, 1, new Vector2(Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192), Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)), flicker: false, Game1.random.NextBool()));
				}
				location.playSound("wand");
				Game1.displayFarmer = false;
				Game1.player.freezePause = 50;
				Game1.flashAlpha = 1f;
				DelayedAction.fadeAfterDelay(delegate
				{
					who.setTileLocation(v);
					Game1.displayFarmer = true;
					Game1.globalFadeToClear();
				}, 50);
				Microsoft.Xna.Framework.Rectangle rectangle = who.GetBoundingBox();
				new Microsoft.Xna.Framework.Rectangle(rectangle.X, rectangle.Y, 64, 64).Inflate(192, 192);
				int num = 0;
				Point tilePoint = who.TilePoint;
				for (int num2 = tilePoint.X + 8; num2 >= tilePoint.X - 8; num2--)
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(num2, tilePoint.Y) * 64f, Color.White, 8, flipped: false, 50f)
					{
						layerDepth = 1f,
						delayBeforeAnimationStart = num * 25,
						motion = new Vector2(-0.25f, 0f)
					});
					num++;
				}
				return true;
			}
		}
		Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:MiniObelisk_NeedsSpace"));
		return false;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a Prairie King arcade system.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnPrairieKingArcadeSystem(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		Location.showPrairieKingMenu();
		return true;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a Junimo Kart arcade system.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnJunimoKartArcadeSystem(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		Response[] answerChoices = new Response[3]
		{
			new Response("Progress", Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12873")),
			new Response("Endless", Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12875")),
			new Response("Exit", Game1.content.LoadString("Strings\\StringsFromCSFiles:TitleMenu.cs.11738"))
		};
		Location.createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Saloon_Arcade_Minecart_Menu"), answerChoices, "MinecartGame");
		return true;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a staircase.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnStaircase(Farmer who, bool justCheckingForActivity = false)
	{
		if (Location is MineShaft mineShaft && mineShaft.shouldCreateLadderOnThisLevel())
		{
			if (justCheckingForActivity)
			{
				return true;
			}
			Game1.enterMine(Game1.CurrentMineLevel + 1);
			Game1.playSound("stairsdown");
		}
		else if (Location.Name.Equals("ManorHouse"))
		{
			if (justCheckingForActivity)
			{
				return true;
			}
			Game1.warpFarmer("LewisBasement", 4, 4, 2);
			Game1.playSound("stairsdown");
		}
		return false;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a slime ball.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnSlimeBall(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		GameLocation location = Location;
		location.objects.Remove(tileLocation.Value);
		DelayedAction.playSoundAfterDelay("slimedead", 40);
		DelayedAction.playSoundAfterDelay("slimeHit", 100);
		location.playSound("slimeHit");
		Random random = Utility.CreateRandom(Game1.stats.DaysPlayed, Game1.uniqueIDForThisGame, (double)tileLocation.X * 77.0, (double)tileLocation.Y * 777.0, 2.0);
		Game1.createMultipleObjectDebris("(O)766", (int)tileLocation.X, (int)tileLocation.Y, random.Next(10, 21), 1f + ((who.FacingDirection == 2) ? 0f : ((float)Game1.random.NextDouble())));
		Utility.makeTemporarySpriteJuicier(new TemporaryAnimatedSprite(44, tileLocation.Value * 64f, Color.Lime, 10)
		{
			interval = 70f,
			holdLastFrame = true,
			alphaFade = 0.01f
		}, location);
		Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(44, tileLocation.Value * 64f + new Vector2(-16f, 0f), Color.Lime, 10)
		{
			interval = 70f,
			delayBeforeAnimationStart = 0,
			holdLastFrame = true,
			alphaFade = 0.01f
		});
		Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(44, tileLocation.Value * 64f + new Vector2(0f, 16f), Color.Lime, 10)
		{
			interval = 70f,
			delayBeforeAnimationStart = 100,
			holdLastFrame = true,
			alphaFade = 0.01f
		});
		Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(44, tileLocation.Value * 64f + new Vector2(16f, 0f), Color.Lime, 10)
		{
			interval = 70f,
			delayBeforeAnimationStart = 200,
			holdLastFrame = true,
			alphaFade = 0.01f
		});
		while (random.NextDouble() < 0.33)
		{
			Game1.createObjectDebris("(O)557", (int)tileLocation.X, (int)tileLocation.Y, who.UniqueMultiplayerID);
		}
		return true;
	}

	protected virtual bool CheckForActionOnBlessedStatue(Farmer who, GameLocation location, bool justCheckingForActivitiy = false)
	{
		if (who.stats.Get(StatKeys.Mastery(0)) < 1 && !justCheckingForActivitiy)
		{
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:MasteryRequirement"));
			Game1.playSound("cancel");
			return true;
		}
		if (!who.hasBuffWithNameContainingString("statue_of_blessings_") && !who.hasBeenBlessedByStatueToday)
		{
			if (justCheckingForActivitiy)
			{
				return true;
			}
			who.hasBeenBlessedByStatueToday = true;
			Random random = Utility.CreateDaySaveRandom(Game1.stats.DaysPlayed * 777);
			for (int i = 0; i < 8; i++)
			{
				random.Next();
			}
			who.applyBuff("statue_of_blessings_" + random.Next((Game1.isRaining || Utility.isFestivalDay()) ? 6 : 7));
			Game1.playSound("statue_of_blessings");
			showNextIndex.Value = true;
			if (location.critters == null)
			{
				location.critters = new List<Critter>();
			}
			location.critters.Add(new Butterfly(location, TileLocation + new Vector2(1f, 0f), islandButterfly: false, forceSummerButterfly: false, 163));
			location.critters.Add(new Butterfly(location, TileLocation + new Vector2(0.33f, 0.25f), islandButterfly: false, forceSummerButterfly: false, 163));
			location.critters.Add(new Butterfly(location, TileLocation + new Vector2(1.58f, 0.25f), islandButterfly: false, forceSummerButterfly: false, 163));
			location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(221, 225, 15, 31), 9000f, 1, 1, TileLocation * 64f + new Vector2(1f, -16f) * 4f, flicker: false, flipped: false, Math.Max(0f, ((TileLocation.Y + 1f) * 64f - 20f) / 10000f) + TileLocation.X * 1E-05f, 0.02f, Color.White, 4f, 0f, 0f, 0f));
			for (int j = 0; j < 6; j++)
			{
				Utility.makeTemporarySpriteJuicier(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(144, 249, 7, 7), Game1.random.Next(100, 200), 6, 1, TileLocation * 64f + new Vector2(32 + Game1.random.Next(-64, 64), Game1.random.Next(-64, 64)), flicker: false, flipped: false, Math.Max(0f, ((TileLocation.Y + 1f) * 64f - 24f) / 10000f) + TileLocation.X * 1E-05f, 0f, (Game1.random.NextDouble() < 0.5) ? new Color(255, 180, 210) : Color.White, 4f, 0f, 0f, 0f), location);
			}
			return true;
		}
		return false;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a house plant.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnHousePlant(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		base.ParentSheetIndex++;
		int num = -1;
		int num2 = -1;
		if (name == "House Plant")
		{
			num = 8;
			num2 = 0;
		}
		if (base.ParentSheetIndex == num2 + num)
		{
			base.ParentSheetIndex -= num;
			return false;
		}
		return true;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a flute block.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnFluteBlock(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		int.TryParse(preservedParentSheetIndex.Value, out var result);
		result = result switch
		{
			2300 => 2400, 
			2400 => 0, 
			_ => (result + 100) % 2400, 
		};
		preservedParentSheetIndex.Value = result.ToString();
		shakeTimer = 200;
		string cueName = "flute";
		if (who.ActiveObject != null)
		{
			cueName = getFluteBlockSoundFromHeldObject(who.ActiveObject);
		}
		internalSound?.Stop(AudioStopOptions.Immediate);
		Game1.playSound(cueName, result, out internalSound);
		scale.Y = 1.3f;
		shakeTimer = 200;
		return true;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a drum block.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnDrumBlock(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		int.TryParse(preservedParentSheetIndex.Value, out var result);
		result = (result + 1) % 7;
		preservedParentSheetIndex.Value = result.ToString();
		shakeTimer = 200;
		internalSound?.Stop(AudioStopOptions.Immediate);
		Game1.playSound("drumkit" + result, out internalSound);
		scale.Y = 1.3f;
		shakeTimer = 200;
		return true;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a sprinkler.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected bool CheckForActionOnSprinkler(Farmer who, bool justCheckingForActivity = false)
	{
		if (heldObject.Value != null && heldObject.Value.QualifiedItemId == "(O)913")
		{
			if (justCheckingForActivity)
			{
				return true;
			}
			if (!Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
			{
				return false;
			}
			if (heldObject.Value.heldObject.Value is Chest chest)
			{
				chest.GetMutex().RequestLock(chest.ShowMenu);
				return true;
			}
		}
		return false;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a scarecrow.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected bool CheckForActionOnScarecrow(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		if (base.QualifiedItemId == "(BC)126" && who.CurrentItem is Hat hat)
		{
			shakeTimer = 100;
			if (quality.Value != 0)
			{
				Game1.createItemDebris(ItemRegistry.Create("(H)" + (quality.Value - 1)), tileLocation.Value * 64f, (who.FacingDirection + 2) % 4);
				quality.Value = 0;
			}
			if (preservedParentSheetIndex.Value != null)
			{
				Game1.createItemDebris(new Hat(preservedParentSheetIndex.Value), tileLocation.Value * 64f, (who.FacingDirection + 2) % 4);
			}
			preservedParentSheetIndex.Value = hat.ItemId;
			who.Items[who.CurrentToolIndex] = null;
			Location.playSound("dirtyHit");
			return true;
		}
		if (!Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
		{
			return false;
		}
		shakeTimer = 100;
		if (base.SpecialVariable == 0)
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12926"));
		}
		else
		{
			Game1.drawObjectDialogue((base.SpecialVariable == 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12927") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12929", base.SpecialVariable));
		}
		return true;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a singing stone.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected bool CheckForActionOnSingingStone(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		int num = Game1.random.Next(2400);
		num -= num % 100;
		Game1.playSound("crystal", num);
		shakeTimer = 100;
		return true;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a text sign.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected virtual bool CheckForActionOnTextSign(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		if (Game1.activeClickableMenu == null)
		{
			TitleTextInputMenu signMenu = new TitleTextInputMenu(Game1.content.LoadString("Strings\\UI:TextSignEntry"), null, SignText, null);
			signMenu.pasteButton.visible = false;
			signMenu.doneNaming = delegate(string text)
			{
				signText.Value = text.Trim();
				signMenu.exitThisMenu();
				showNextIndex.Value = string.IsNullOrEmpty(SignText);
			};
			signMenu.textBox.textLimit = 60;
			Game1.activeClickableMenu = signMenu;
			return true;
		}
		return false;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a feed hopper.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected bool CheckForActionOnFeedHopper(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		if (who.ActiveObject != null)
		{
			return false;
		}
		if (who.freeSpotsInInventory() > 0)
		{
			GameLocation location = Location;
			GameLocation rootLocation = location.GetRootLocation();
			int value = rootLocation.piecesOfHay.Value;
			if (value > 0)
			{
				bool flag = false;
				if (location is AnimalHouse animalHouse)
				{
					int val = Math.Min(animalHouse.animalsThatLiveHere.Count, value);
					val = Math.Max(1, val);
					int num = animalHouse.numberOfObjectsWithName("Hay");
					val = Math.Min(val, animalHouse.animalLimit.Value - num);
					if (val != 0 && Game1.player.couldInventoryAcceptThisItem("(O)178", val))
					{
						rootLocation.piecesOfHay.Value -= Math.Max(1, val);
						who.addItemToInventoryBool(ItemRegistry.Create("(O)178", val));
						Game1.playSound("shwip");
						flag = true;
					}
				}
				else if (Game1.player.couldInventoryAcceptThisItem("(O)178", 1))
				{
					rootLocation.piecesOfHay.Value--;
					who.addItemToInventoryBool(ItemRegistry.Create("(O)178"));
					Game1.playSound("shwip");
				}
				if (rootLocation.piecesOfHay.Value <= 0)
				{
					showNextIndex.Value = false;
				}
				return true;
			}
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12942"));
		}
		else
		{
			Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
		}
		return true;
	}

	/// <summary>Perform an action when the user interacts with this object, assuming it's a machine.</summary>
	/// <inheritdoc cref="M:StardewValley.Object.checkForAction(StardewValley.Farmer,System.Boolean)" />
	protected bool CheckForActionOnMachine(Farmer who, bool justCheckingForActivity = false)
	{
		GameLocation location = Location;
		if (readyForHarvest.Value)
		{
			if (justCheckingForActivity)
			{
				return true;
			}
			if (who.isMoving())
			{
				Game1.haltAfterCheck = false;
			}
			MachineData machineData = GetMachineData();
			Object value = heldObject.Value;
			if (lastOutputRuleId.Value != null)
			{
				MachineOutputRule machineOutputRule = machineData.OutputRules?.FirstOrDefault((MachineOutputRule p) => p.Id == lastOutputRuleId.Value);
				if (machineOutputRule != null && machineOutputRule.RecalculateOnCollect)
				{
					heldObject.Value = null;
					OutputMachine(machineData, machineOutputRule, lastInputItem.Value, who, location, probe: false, heldObjectOnly: true);
					if (heldObject.Value != null)
					{
						value = heldObject.Value;
					}
					else
					{
						heldObject.Value = value;
					}
				}
			}
			bool flag = false;
			if (who.IsLocalPlayer)
			{
				heldObject.Value = null;
				if (!who.addItemToInventoryBool(value))
				{
					heldObject.Value = value;
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
					return false;
				}
				Game1.playSound("coin");
				flag = true;
				MachineDataUtility.UpdateStats(machineData?.StatsToIncrementWhenHarvested, value, value.Stack);
			}
			heldObject.Value = null;
			readyForHarvest.Value = false;
			showNextIndex.Value = false;
			ResetParentSheetIndex();
			if (MachineDataUtility.TryGetMachineOutputRule(this, machineData, MachineOutputTrigger.OutputCollected, value.getOne(), who, location, out var rule, out var _, out var _, out var _))
			{
				OutputMachine(machineData, rule, lastInputItem.Value, who, location, probe: false);
			}
			if (IsTapper() && location.terrainFeatures.TryGetValue(tileLocation.Value, out var value2) && value2 is Tree tree)
			{
				tree.UpdateTapperProduct(this, value);
			}
			if (machineData != null && machineData.ExperienceGainOnHarvest != null)
			{
				string[] array = machineData.ExperienceGainOnHarvest.Split(' ');
				for (int num = 0; num < array.Length; num += 2)
				{
					int skillNumberFromName = Farmer.getSkillNumberFromName(array[num]);
					if (skillNumberFromName != -1 && ArgUtility.TryGetInt(array, num + 1, out var value3, out var _, "int amount"))
					{
						who.gainExperience(skillNumberFromName, value3);
					}
				}
			}
			if (flag)
			{
				AttemptAutoLoad(who);
			}
			return true;
		}
		MachineData machineData2 = GetMachineData();
		if (machineData2 != null && machineData2.InteractMethod != null)
		{
			if (StaticDelegateBuilder.TryCreateDelegate<MachineInteractDelegate>(machineData2.InteractMethod, out var createdDelegate, out var error2))
			{
				if (!justCheckingForActivity)
				{
					return createdDelegate(this, location, who);
				}
				return true;
			}
			Game1.log.Warn($"Machine {base.ItemId} has invalid interaction method '{machineData2.InteractMethod}': {error2}");
		}
		return false;
	}

	/// <summary>Play a sound for the current player only if they're near this object.</summary>
	/// <param name="audioName">The sound ID to play.</param>
	/// <param name="pitch">The pitch modifier to apply, or <c>null</c> to keep it as-is.</param>
	/// <param name="context">The source which triggered the sound.</param>
	public void playNearbySoundLocal(string audioName, int? pitch = null, SoundContext context = SoundContext.Default)
	{
		Game1.sounds.PlayLocal(audioName, Location, tileLocation.Value, pitch, context, out var _);
	}

	/// <summary>Play a sound for each nearby online player.</summary>
	/// <param name="audioName">The sound ID to play.</param>
	/// <param name="pitch">The pitch modifier to apply, or <c>null</c> to keep it as-is.</param>
	/// <param name="context">The source which triggered the sound.</param>
	public void playNearbySoundAll(string audioName, int? pitch = null, SoundContext context = SoundContext.Default)
	{
		Game1.sounds.PlayAll(audioName, Location, tileLocation.Value, pitch, context);
	}

	public virtual bool IsScarecrow()
	{
		if (HasContextTag("crow_scare"))
		{
			return true;
		}
		return Name.Contains("arecrow");
	}

	public virtual int GetRadiusForScarecrow()
	{
		foreach (string contextTag in GetContextTags())
		{
			if (contextTag.StartsWithIgnoreCase("crow_scare_radius_") && int.TryParse(contextTag.Substring("crow_scare_radius_".Length), out var result))
			{
				return result;
			}
		}
		if (Name.StartsWith("Deluxe"))
		{
			return 17;
		}
		return 9;
	}

	/// <summary>If there's a chest above this object, try to auto-load the held object from the chest.</summary>
	/// <param name="who">The player interacting with the machine, if applicable.</param>
	/// <returns>If a chest is found, this method will acquire a mutex lock so the auto-load may not happen during the same tick. The returned task will complete once the auto-load happens, and contain true (an item was loaded) or false (no item was loaded).</returns>
	public virtual Task<bool> AttemptAutoLoad(Farmer who)
	{
		GameLocation location = Location;
		if (location != null && location.objects.TryGetValue(new Vector2(TileLocation.X, TileLocation.Y - 1f), out var value))
		{
			Chest chest = value as Chest;
			if (chest != null && chest.specialChestType.Value == Chest.SpecialChestTypes.AutoLoader)
			{
				TaskCompletionSource<bool> taskSource = new TaskCompletionSource<bool>();
				chest.GetMutex().RequestLock(delegate
				{
					try
					{
						chest.GetMutex().ReleaseLock();
						bool result = AttemptAutoLoad(chest.Items, who);
						taskSource.SetResult(result);
					}
					catch (Exception exception)
					{
						taskSource.SetException(exception);
					}
				});
				return taskSource.Task;
			}
		}
		return Task.FromResult(result: false);
	}

	/// <summary>Try to auto-load the held object from the given inventory.</summary>
	/// <param name="inventory">The inventory from which to take items.</param>
	/// <param name="who">The player interacting with the machine, if applicable.</param>
	public virtual bool AttemptAutoLoad(IInventory inventory, Farmer who)
	{
		if (heldObject.Value != null)
		{
			return false;
		}
		autoLoadFrom = inventory;
		foreach (Item item in inventory)
		{
			if (performObjectDropInAction(item, probe: false, who))
			{
				autoLoadFrom = null;
				return true;
			}
		}
		autoLoadFrom = null;
		return false;
	}

	private string getFluteBlockSoundFromHeldObject(Object o)
	{
		switch (o.QualifiedItemId)
		{
		case "(O)797":
		case "(O)372":
			return "clam_tone";
		case "(BC)214":
			return "telephone_buttonPush";
		case "(O)66":
			return "miniharp_note";
		case "(O)430":
			return "pig";
		case "(O)577":
		case "(O)578":
		case "(O)338":
		case "(O)80":
			return "crystal";
		case "(O)444":
			return "Duck";
		case "(O)746":
		case "(O)769":
			return "toyPiano";
		case "(O)382":
			return "dustMeep";
		default:
			return "flute";
		}
	}

	public virtual void farmerAdjacentAction(Farmer who, bool diagonal = false)
	{
		if (name == "Error Item" || isTemporarilyInvisible)
		{
			return;
		}
		GameLocation location = Location;
		switch (base.QualifiedItemId)
		{
		case "(O)464":
			if ((internalSound == null || ((int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds - lastNoteBlockSoundTime >= 1000 && !internalSound.IsPlaying)) && !Game1.dialogueUp && !diagonal)
			{
				int.TryParse(preservedParentSheetIndex.Value, out var result2);
				string cueName = "flute";
				if (who.ActiveObject != null)
				{
					cueName = getFluteBlockSoundFromHeldObject(who.ActiveObject);
				}
				Game1.playSound(cueName, result2, out internalSound);
				scale.Y = 1.3f;
				shakeTimer = 200;
				lastNoteBlockSoundTime = (int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
				if (location is IslandSouthEast islandSouthEast)
				{
					islandSouthEast.OnFlutePlayed(result2);
				}
			}
			return;
		case "(O)463":
			if ((internalSound == null || (Game1.currentGameTime.TotalGameTime.TotalMilliseconds - (double)lastNoteBlockSoundTime >= 1000.0 && !internalSound.IsPlaying)) && !Game1.dialogueUp && !diagonal)
			{
				int.TryParse(preservedParentSheetIndex.Value, out var result);
				Game1.playSound("drumkit" + result, out internalSound);
				scale.Y = 1.3f;
				shakeTimer = 200;
				lastNoteBlockSoundTime = (int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
			}
			return;
		case "(BC)29":
		{
			if (diagonal)
			{
				return;
			}
			scale.X += 1f;
			if (scale.X > 30f)
			{
				base.ParentSheetIndex = ((base.ParentSheetIndex == 29) ? 30 : 29);
				scale.X = 0f;
				scale.Y += 2f;
			}
			if (!(scale.Y >= 20f) || !(Game1.random.NextDouble() < 0.0001) || location.characters.Count >= 4)
			{
				return;
			}
			Vector2 tile = Game1.player.Tile;
			Vector2[] adjacentTilesOffsets = Character.AdjacentTilesOffsets;
			foreach (Vector2 vector in adjacentTilesOffsets)
			{
				Vector2 vector2 = tile + vector;
				if (!location.IsTileOccupiedBy(vector2) && location.isTilePassable(new Location((int)vector2.X, (int)vector2.Y), Game1.viewport) && location.isCharacterAtTile(vector2) == null)
				{
					if (Game1.random.NextDouble() < 0.1)
					{
						location.characters.Add(new GreenSlime(vector2 * new Vector2(64f, 64f)));
					}
					else if (Game1.random.NextBool())
					{
						location.characters.Add(new ShadowGuy(vector2 * new Vector2(64f, 64f)));
					}
					else
					{
						location.characters.Add(new ShadowGirl(vector2 * new Vector2(64f, 64f)));
					}
					((Monster)location.characters[location.characters.Count - 1]).moveTowardPlayerThreshold.Value = 4;
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(352, 400f, 2, 1, vector2 * new Vector2(64f, 64f), flicker: false, flipped: false));
					location.playSound("shadowpeep");
					break;
				}
			}
			return;
		}
		}
		if (IsTextSign())
		{
			hovering = true;
		}
		else if (!diagonal)
		{
			Vector2 key = new Vector2(TileLocation.X, TileLocation.Y - 1f);
			if (Location.objects.TryGetValue(key, out var value) && value.IsTextSign())
			{
				value.hovering = true;
			}
		}
	}

	public virtual void addWorkingAnimation()
	{
		GameLocation location = Location;
		if (location == null || !location.farmers.Any())
		{
			return;
		}
		MachineData machineData = GetMachineData();
		if (machineData?.WorkingEffects == null)
		{
			return;
		}
		foreach (MachineEffects workingEffect in machineData.WorkingEffects)
		{
			if (PlayMachineEffect(workingEffect))
			{
				break;
			}
		}
	}

	public virtual void onReadyForHarvest()
	{
	}

	/// <summary>Update the object when the time of day changes.</summary>
	/// <param name="minutes">The number of minutes that passed. If this item is a machine, this method may be called with zero minutes immediately after the machine begins processing.</param>
	/// <returns>Returns <c>true</c> if the object should be removed, else <c>false</c>.</returns>
	public virtual bool minutesElapsed(int minutes)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return false;
		}
		if (heldObject.Value != null && base.QualifiedItemId != "(BC)165")
		{
			if (IsSprinkler())
			{
				return false;
			}
			MachineData machineData = GetMachineData();
			if (Game1.IsMasterGame && (machineData == null || ShouldTimePassForMachine()))
			{
				minutesUntilReady.Value -= minutes;
			}
			if (MinutesUntilReady <= 0 && (machineData == null || !machineData.OnlyCompleteOvernight || Game1.newDaySync.hasInstance()))
			{
				if (!readyForHarvest.Value && (!Game1.newDaySync.hasInstance() || Game1.newDaySync.hasFinished()))
				{
					location.playSound("dwop");
				}
				readyForHarvest.Value = true;
				minutesUntilReady.Value = 0;
				onReadyForHarvest();
				showNextIndex.Value = machineData?.ShowNextIndexWhenReady ?? false;
				if (lightSource != null)
				{
					location.removeLightSource(lightSource.Id);
					lightSource = null;
				}
			}
			if (machineData != null)
			{
				if (!readyForHarvest.Value && machineData.WorkingEffects != null && Game1.random.NextDouble() < (double)machineData.WorkingEffectChance)
				{
					addWorkingAnimation();
				}
			}
			else if (!readyForHarvest.Value && Game1.random.NextDouble() < 0.33)
			{
				addWorkingAnimation();
			}
		}
		else
		{
			switch (base.QualifiedItemId)
			{
			case "(BC)29":
				scale.Y = Math.Max(0f, scale.Y -= minutes / 2 + 1);
				break;
			case "(BC)96":
				MinutesUntilReady -= minutes;
				showNextIndex.Value = !showNextIndex.Value;
				if (MinutesUntilReady <= 0)
				{
					performRemoveAction();
					location.objects.Remove(tileLocation.Value);
					location.objects.Add(tileLocation.Value, ItemRegistry.Create<Object>("(BC)98"));
					Game1.player.team.RequestSetMail(PlayerActionTarget.Host, "Capsule_Broken", MailType.Received, add: true);
				}
				break;
			case "(BC)141":
				showNextIndex.Value = !showNextIndex.Value;
				break;
			case "(BC)83":
				showNextIndex.Value = false;
				location.removeLightSource(GenerateLightSourceId(tileLocation.Value));
				break;
			}
		}
		return false;
	}

	public virtual bool ShouldTimePassForMachine()
	{
		GameLocation location = Location;
		MachineData machineData = GetMachineData();
		if (location == null || machineData == null)
		{
			return false;
		}
		if (machineData.PreventTimePass != null)
		{
			using List<MachineTimeBlockers>.Enumerator enumerator = machineData.PreventTimePass.GetEnumerator();
			while (enumerator.MoveNext())
			{
				switch (enumerator.Current)
				{
				case MachineTimeBlockers.Always:
					return false;
				case MachineTimeBlockers.Spring:
					if (location.IsSpringHere())
					{
						return false;
					}
					break;
				case MachineTimeBlockers.Summer:
					if (location.IsSummerHere())
					{
						return false;
					}
					break;
				case MachineTimeBlockers.Fall:
					if (location.IsFallHere())
					{
						return false;
					}
					break;
				case MachineTimeBlockers.Winter:
					if (location.IsWinterHere())
					{
						return false;
					}
					break;
				case MachineTimeBlockers.Sun:
					if (!location.IsRainingHere())
					{
						return false;
					}
					break;
				case MachineTimeBlockers.Rain:
					if (location.IsRainingHere())
					{
						return false;
					}
					break;
				case MachineTimeBlockers.Inside:
					if (!location.IsOutdoors)
					{
						return false;
					}
					break;
				case MachineTimeBlockers.Outside:
					if (location.IsOutdoors)
					{
						return false;
					}
					break;
				}
			}
		}
		return true;
	}

	public override string checkForSpecialItemHoldUpMeessage()
	{
		if (!bigCraftable.Value && Type == "Arch")
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12993");
		}
		return base.QualifiedItemId switch
		{
			"(O)102" => Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12994"), 
			"(O)535" => Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12995"), 
			"(BC)160" => Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12996"), 
			_ => base.checkForSpecialItemHoldUpMeessage(), 
		};
	}

	public virtual bool countsForShippedCollection()
	{
		if (string.IsNullOrWhiteSpace(type.Value) || Type == "Arch" || bigCraftable.Value)
		{
			return false;
		}
		if (base.QualifiedItemId == "(O)433")
		{
			return true;
		}
		switch (base.Category)
		{
		case -999:
		case -74:
		case -29:
		case -24:
		case -22:
		case -21:
		case -20:
		case -19:
		case -14:
		case -12:
		case -8:
		case -7:
		case -2:
		case 0:
			return false;
		default:
		{
			if (Game1.objectData.TryGetValue(base.ItemId, out var value) && value.ExcludeFromShippingCollection)
			{
				return false;
			}
			return true;
		}
		}
	}

	public static bool isPotentialBasicShipped(string itemId, int category, string objectType)
	{
		if (itemId == "433")
		{
			return true;
		}
		switch (objectType)
		{
		case "Arch":
		case "Fish":
		case "Minerals":
		case "Cooking":
			return false;
		default:
			switch (category)
			{
			case -999:
			case -103:
			case -102:
			case -96:
			case -74:
			case -29:
			case -24:
			case -22:
			case -21:
			case -20:
			case -19:
			case -14:
			case -12:
			case -8:
			case -7:
			case -2:
			case 0:
				return false;
			default:
			{
				if (Game1.objectData.TryGetValue(itemId, out var value) && value.ExcludeFromShippingCollection)
				{
					return false;
				}
				return true;
			}
			}
		}
	}

	public override IEnumerable<Buff> GetFoodOrDrinkBuffs()
	{
		foreach (Buff foodOrDrinkBuff in base.GetFoodOrDrinkBuffs())
		{
			yield return foodOrDrinkBuff;
		}
		if (customBuff != null)
		{
			Buff buff = customBuff();
			if (buff != null)
			{
				yield return buff;
			}
		}
		if (edibility.Value <= -300 || !Game1.objectData.TryGetValue(base.ItemId, out var value))
		{
			yield break;
		}
		List<ObjectBuffData> buffs = value.Buffs;
		if (buffs == null || buffs.Count <= 0)
		{
			yield break;
		}
		float durationMultiplier = ((base.Quality != 0) ? 1.5f : 1f);
		foreach (Buff item in TryCreateBuffsFromData(value, Name, DisplayName, durationMultiplier, ModifyItemBuffs))
		{
			yield return item;
		}
	}

	/// <summary>Create buffs matching data from <c>Data/Objects</c>, if valid.</summary>
	/// <param name="obj">The raw data from <c>Data/Objects</c> to parse.</param>
	/// <param name="name">The buff source name (usually the <see cref="P:StardewValley.Item.Name" />).</param>
	/// <param name="displayName">The translated buff source name (usually the <see cref="P:StardewValley.Item.DisplayName" />).</param>
	/// <param name="durationMultiplier">A multiplier to apply to food or drink buff durations. This only applies to food/drink buffs defined directly in <c>Data/Objects</c>, not to buff IDs which reference <c>Data/Buffs</c>.</param>
	/// <param name="adjustEffects">Adjust the parsed attribute effects before the buff is constructed.</param>
	public static IEnumerable<Buff> TryCreateBuffsFromData(ObjectData obj, string name, string displayName, float durationMultiplier = 1f, Action<BuffEffects> adjustEffects = null)
	{
		List<ObjectBuffData> buffs = obj.Buffs;
		if (buffs == null || buffs.Count <= 0)
		{
			yield break;
		}
		foreach (ObjectBuffData buff2 in obj.Buffs)
		{
			if (buff2 == null)
			{
				continue;
			}
			string text = buff2.BuffId;
			bool num = !string.IsNullOrWhiteSpace(text);
			if (!num)
			{
				text = (obj.IsDrink ? "drink" : "food");
			}
			BuffEffects buffEffects = new BuffEffects(buff2.CustomAttributes);
			adjustEffects?.Invoke(buffEffects);
			Texture2D iconTexture = null;
			int iconSheetIndex = -1;
			if (buff2.IconTexture != null)
			{
				iconTexture = Game1.content.Load<Texture2D>(buff2.IconTexture);
				iconSheetIndex = buff2.IconSpriteIndex;
			}
			int num2 = -1;
			if (buff2.Duration == -2)
			{
				num2 = -2;
			}
			else if (buff2.Duration != 0)
			{
				num2 = (int)((float)buff2.Duration * durationMultiplier) * Game1.realMilliSecondsPerGameMinute;
			}
			bool isDebuff = buff2.IsDebuff;
			Color? color = Utility.StringToColor(buff2.GlowColor);
			if (num || ((num2 > 0 || num2 == -2) && buffEffects.HasAnyValue()))
			{
				Buff buff = new Buff(text, name, displayName, num2, iconTexture, iconSheetIndex, buffEffects, isDebuff);
				buff.customFields.TryAddMany(buff2.CustomFields);
				if (color.HasValue)
				{
					buff.glow = color.Value;
				}
				yield return buff;
			}
		}
	}

	/// <summary>Get whether the object scale should pulse currently.</summary>
	public virtual bool ShouldWobble()
	{
		if (minutesUntilReady.Value > 0 && !readyForHarvest.Value)
		{
			MachineData machineData = GetMachineData();
			if (machineData != null)
			{
				if (machineData.WobbleWhileWorking)
				{
					return heldObject.Value != null;
				}
				return false;
			}
			if (bigCraftable.Value)
			{
				switch (base.QualifiedItemId)
				{
				case "(BC)22":
				case "(BC)23":
				case "(BC)65":
				case "(BC)66":
				case "(BC)165":
					return false;
				default:
					return true;
				}
			}
		}
		return false;
	}

	public virtual Vector2 getScale()
	{
		if (base.Category == -22)
		{
			return Vector2.Zero;
		}
		if (!bigCraftable.Value)
		{
			scale.Y = Math.Max(4f, scale.Y - 0.04f);
			return scale;
		}
		if (ShouldWobble())
		{
			if (base.QualifiedItemId.Equals("(BC)17"))
			{
				scale.X = (float)((double)(scale.X + 0.04f) % (Math.PI * 2.0));
				return Vector2.Zero;
			}
			scale.X -= 0.1f;
			scale.Y += 0.1f;
			if (scale.X <= 0f)
			{
				scale.X = 10f;
			}
			if (scale.Y >= 10f)
			{
				scale.Y = 0f;
			}
			return new Vector2(Math.Abs(scale.X - 5f), Math.Abs(scale.Y - 5f));
		}
		return Vector2.Zero;
	}

	public virtual void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
	{
		if (StardewValley.Pokemon.PokemonItemManager.TryGetCustomTexture(base.QualifiedItemId, out Texture2D customTex, out Microsoft.Xna.Framework.Rectangle customRect))
		{
			float layerDepth = Math.Max(0f, (float)(f.StandingPixel.Y + 3) / 10000f);
			spriteBatch.Draw(customTex, objectPosition, customRect, Color.White, 0f, Vector2.Zero, 2.5f, SpriteEffects.None, layerDepth);
			return;
		}
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		float layerDepth2 = Math.Max(0f, (float)(f.StandingPixel.Y + 3) / 10000f);
		Texture2D texture = dataOrErrorItem.GetTexture();
		int offset = 0;
		if (this is Mannequin)
		{
			offset = 2;
		}
		spriteBatch.Draw(texture, objectPosition, dataOrErrorItem.GetSourceRect(offset, base.ParentSheetIndex), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
	}

	/// <summary>Draw a green or red placement tile for the held item, if applicable.</summary>
	/// <param name="spriteBatch">The sprite batch being drawn.</param>
	/// <param name="location">The current location.</param>
	public virtual void drawPlacementBounds(SpriteBatch spriteBatch, GameLocation location)
	{
		if (!isPlaceable() || this is Wallpaper)
		{
			return;
		}
		Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
		int num = (int)Game1.GetPlacementGrabTile().X * 64;
		int num2 = (int)Game1.GetPlacementGrabTile().Y * 64;
		if (Game1.isCheckingNonMousePlacement)
		{
			Vector2 nearbyValidPlacementPosition = Utility.GetNearbyValidPlacementPosition(Game1.player, location, this, num, num2);
			num = (int)nearbyValidPlacementPosition.X;
			num2 = (int)nearbyValidPlacementPosition.Y;
		}
		Vector2 key = new Vector2(num / 64, num2 / 64);
		if (Equals(Game1.player.ActiveObject))
		{
			TileLocation = key;
		}
		if (Utility.isThereAnObjectHereWhichAcceptsThisItem(location, this, num, num2) && (!location.objects.TryGetValue(key, out var value) || !(value is IndoorPot indoorPot) || !indoorPot.IsPlantableItem(this)))
		{
			return;
		}
		bool flag = Utility.playerCanPlaceItemHere(location, this, num, num2, Game1.player) || (Utility.isThereAnObjectHereWhichAcceptsThisItem(location, this, num, num2) && Utility.withinRadiusOfPlayer(num, num2, 1, Game1.player));
		Game1.isCheckingNonMousePlacement = false;
		int num3 = 1;
		int num4 = 1;
		if (this is Furniture furniture)
		{
			num3 = furniture.getTilesWide();
			num4 = furniture.getTilesHigh();
		}
		for (int i = 0; i < num3; i++)
		{
			for (int j = 0; j < num4; j++)
			{
				spriteBatch.Draw(Game1.mouseCursors, new Vector2((key.X + (float)i) * 64f - (float)Game1.viewport.X, (key.Y + (float)j) * 64f - (float)Game1.viewport.Y), new Microsoft.Xna.Framework.Rectangle(flag ? 194 : 210, 388, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.01f);
			}
		}
		if (bigCraftable.Value || this is Furniture || (category.Value != -74 && category.Value != -19))
		{
			draw(spriteBatch, (int)key.X, (int)key.Y, 0.5f);
		}
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
		if (drawShadow && !bigCraftable.Value && base.QualifiedItemId != "(O)590" && base.QualifiedItemId != "(O)SeedSpot")
		{
			DrawShadow(spriteBatch, location, color, layerDepth);
		}
		if (StardewValley.Pokemon.PokemonItemManager.TryGetCustomTexture(base.QualifiedItemId, out Texture2D customTex, out Microsoft.Xna.Framework.Rectangle customRect))
		{
			float maxDim = Math.Max(customRect.Width, customRect.Height);
			float customScale = (48f / Math.Max(1f, maxDim)) * scaleSize;
			Vector2 origin = new Vector2(customRect.Width / 2f, customRect.Height / 2f);
			spriteBatch.Draw(customTex, location + new Vector2(32f, 32f), customRect, color * transparency, 0f, origin, customScale, SpriteEffects.None, layerDepth);
			DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
			return;
		}
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		float num = scaleSize;
		if (bigCraftable.Value && num > 0.2f)
		{
			num /= 2f;
		}
		int offset = 0;
		if (this is Mannequin)
		{
			offset = 2;
		}
		Microsoft.Xna.Framework.Rectangle sourceRect = dataOrErrorItem.GetSourceRect(offset, base.ParentSheetIndex);
		spriteBatch.Draw(dataOrErrorItem.GetTexture(), location + new Vector2(32f, 32f), sourceRect, color * transparency, 0f, new Vector2(sourceRect.Width / 2, sourceRect.Height / 2), 4f * num, SpriteEffects.None, layerDepth);
		DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
	}

	/// <summary>Draw the shadow behind the item in menus.</summary>
	/// <param name="spriteBatch">The sprite batch being drawn to the screen.</param>
	/// <param name="position">The pixel position at which the item is being drawn.</param>
	/// <param name="color">The item color tint to apply.</param>
	/// <param name="layerDepth">The layer depth relative to other sprites in the current sprite batch.</param>
	public virtual void DrawShadow(SpriteBatch spriteBatch, Vector2 position, Color color, float layerDepth)
	{
		spriteBatch.Draw(Game1.shadowTexture, position + new Vector2(32f, 48f), Game1.shadowTexture.Bounds, color * 0.5f, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f, SpriteEffects.None, layerDepth - 0.0001f);
	}

	public override void DrawIconBar(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color)
	{
		if (base.Category == -22 && uses.Value > 0)
		{
			float num = ((float)(FishingRod.maxTackleUses - uses.Value) + 0f) / (float)FishingRod.maxTackleUses;
			spriteBatch.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((int)location.X, (int)(location.Y + 56f * scaleSize), (int)(64f * scaleSize * num), (int)(8f * scaleSize)), Utility.getRedToGreenLerpColor(num));
		}
	}

	public virtual void drawAsProp(SpriteBatch b)
	{
		if (isTemporarilyInvisible)
		{
			return;
		}
		int num = (int)tileLocation.X;
		int num2 = (int)tileLocation.Y;
		if (bigCraftable.Value)
		{
			int offset = 0;
			if (showNextIndex.Value)
			{
				offset = 1;
			}
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
			Texture2D texture = dataOrErrorItem.GetTexture();
			Vector2 vector = getScale();
			vector *= 4f;
			Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(num * 64, num2 * 64 - 64));
			Microsoft.Xna.Framework.Rectangle destinationRectangle = new Microsoft.Xna.Framework.Rectangle((int)(vector2.X - vector.X / 2f), (int)(vector2.Y - vector.Y / 2f), (int)(64f + vector.X), (int)(128f + vector.Y / 2f));
			b.Draw(texture, destinationRectangle, dataOrErrorItem.GetSourceRect(offset, base.ParentSheetIndex), Color.White, 0f, Vector2.Zero, SpriteEffects.None, Math.Max(0f, (float)((num2 + 1) * 64 - 1) / 10000f) + (IsTapper() ? 0.0015f : 0f));
			if (base.QualifiedItemId == "(BC)17" && MinutesUntilReady > 0)
			{
				b.Draw(Game1.objectSpriteSheet, getLocalPosition(Game1.viewport) + new Vector2(32f, 0f), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 435), Color.White, scale.X, new Vector2(32f, 32f), 1f, SpriteEffects.None, Math.Max(0f, (float)((num2 + 1) * 64 - 1) / 10000f + 0.0001f));
			}
		}
		else
		{
			Microsoft.Xna.Framework.Rectangle boundingBoxAt = GetBoundingBoxAt(num, num2);
			if (base.QualifiedItemId != "(O)590" && base.QualifiedItemId != "(O)742" && base.QualifiedItemId != "(O)SeedSpot")
			{
				b.Draw(Game1.shadowTexture, getLocalPosition(Game1.viewport) + new Vector2(32f, 53f), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, (float)boundingBoxAt.Bottom / 15000f);
			}
			ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
			b.Draw(dataOrErrorItem2.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(num * 64 + 32, num2 * 64 + 32)), dataOrErrorItem2.GetSourceRect(), Color.White, 0f, new Vector2(8f, 8f), (scale.Y > 1f) ? getScale().Y : 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)boundingBoxAt.Bottom / 10000f);
		}
	}

	public virtual void drawAboveFrontLayer(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
	}

	public virtual void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		if (isTemporarilyInvisible)
		{
			return;
		}
		GameLocation location = Location;
		if (hovering)
		{
			if (IsTextSign() && !string.IsNullOrEmpty(SignText))
			{
				Vector2 positionOfBottomCenter = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32, y * 64 - 64));
				SpriteText.drawSmallTextBubble(spriteBatch, SignText, positionOfBottomCenter, 256, 0.98f + TileLocation.X * 0.0001f + TileLocation.Y * 1E-06f);
			}
			hovering = false;
		}
		if (bigCraftable.Value)
		{
			Vector2 vector = getScale();
			vector *= 4f;
			Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 64));
			Microsoft.Xna.Framework.Rectangle destinationRectangle = new Microsoft.Xna.Framework.Rectangle((int)(vector2.X - vector.X / 2f) + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(vector2.Y - vector.Y / 2f) + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(64f + vector.X), (int)(128f + vector.Y / 2f));
			float num = Math.Max(0f, (float)((y + 1) * 64 - 24) / 10000f) + (float)x * 1E-05f;
			int offset = 0;
			if (showNextIndex.Value)
			{
				offset = 1;
			}
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
			if (heldObject.Value != null)
			{
				MachineData machineData = GetMachineData();
				if (machineData != null && machineData.IsIncubator)
				{
					offset = FarmAnimal.GetAnimalDataFromEgg(heldObject.Value, location)?.IncubatorParentSheetOffset ?? 1;
				}
			}
			if (_machineAnimationFrame >= 0 && _machineAnimation != null)
			{
				offset = _machineAnimationFrame;
			}
			if (this is Mannequin mannequin)
			{
				offset = mannequin.facing.Value;
			}
			if (IsTapper())
			{
				num = Math.Max(0f, (float)((y + 1) * 64 + 2) / 10000f) + (float)x / 1000000f;
			}
			if (base.QualifiedItemId == "(BC)272")
			{
				Texture2D texture = dataOrErrorItem.GetTexture();
				spriteBatch.Draw(texture, destinationRectangle, dataOrErrorItem.GetSourceRect(1, base.ParentSheetIndex), Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, num);
				spriteBatch.Draw(texture, vector2 + new Vector2(8.5f, 12f) * 4f, dataOrErrorItem.GetSourceRect(2, base.ParentSheetIndex), Color.White * alpha, (float)Game1.currentGameTime.TotalGameTime.TotalSeconds * -1.5f, new Vector2(7.5f, 15.5f), 4f, SpriteEffects.None, num + 1E-05f);
				return;
			}
			spriteBatch.Draw(dataOrErrorItem.GetTexture(), destinationRectangle, dataOrErrorItem.GetSourceRect(offset, base.ParentSheetIndex), Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, num);
			if (base.QualifiedItemId == "(BC)17" && MinutesUntilReady > 0)
			{
				spriteBatch.Draw(Game1.objectSpriteSheet, getLocalPosition(Game1.viewport) + new Vector2(32f, 0f), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 435, 16, 16), Color.White * alpha, scale.X, new Vector2(8f, 8f), 4f, SpriteEffects.None, Math.Max(0f, (float)((y + 1) * 64) / 10000f + 0.0001f + (float)x * 1E-05f));
			}
			if (isLamp.Value && Game1.isDarkOut(Location))
			{
				spriteBatch.Draw(Game1.mouseCursors, vector2 + new Vector2(-32f, -32f), new Microsoft.Xna.Framework.Rectangle(88, 1779, 32, 32), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, Math.Max(0f, (float)((y + 1) * 64 - 20) / 10000f) + (float)x / 1000000f);
			}
			if (base.QualifiedItemId == "(BC)126")
			{
				string text = ((quality.Value != 0) ? (quality.Value - 1).ToString() : preservedParentSheetIndex.Value);
				if (text != null)
				{
					ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem("(H)" + text);
					Texture2D texture2 = dataOrErrorItem2.GetTexture();
					int spriteIndex = dataOrErrorItem2.SpriteIndex;
					bool flag = ItemContextTagManager.HasBaseTag("(H)" + text, "Prismatic");
					spriteBatch.Draw(texture2, vector2 + new Vector2(-3f, -6f) * 4f, new Microsoft.Xna.Framework.Rectangle(spriteIndex * 20 % texture2.Width, spriteIndex * 20 / texture2.Width * 20 * 4, 20, 20), (flag ? Utility.GetPrismaticColor() : Color.White) * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, Math.Max(0f, (float)((y + 1) * 64 - 20) / 10000f) + (float)x * 1E-05f);
				}
			}
		}
		else if (!Game1.eventUp || (Game1.CurrentEvent != null && !Game1.CurrentEvent.isTileWalkedOn(x, y)))
		{
			Microsoft.Xna.Framework.Rectangle boundingBoxAt = GetBoundingBoxAt(x, y);
			string qualifiedItemId = base.QualifiedItemId;
			if (qualifiedItemId == "(O)590")
			{
				spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), new Microsoft.Xna.Framework.Rectangle(368 + ((Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1200.0 <= 400.0) ? ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 400.0 / 100.0) * 16) : 0), 32, 16, 16), Color.White * alpha, 0f, new Vector2(8f, 8f), (scale.Y > 1f) ? getScale().Y : 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(isPassable() ? boundingBoxAt.Top : boundingBoxAt.Bottom) / 10000f);
				return;
			}
			if (qualifiedItemId == "(O)SeedSpot")
			{
				spriteBatch.Draw(Game1.mouseCursors_1_6, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), new Microsoft.Xna.Framework.Rectangle(160 + ((Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1600.0 <= 800.0) ? ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 400.0 / 100.0) * 16) : 0), 0, 17, 16), Color.White * alpha, 0f, new Vector2(8f, 8f), (scale.Y > 1f) ? getScale().Y : 4f, (Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1600.0 <= 400.0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(isPassable() ? boundingBoxAt.Top : boundingBoxAt.Bottom) / 10000f);
				return;
			}
			if (fragility.Value != 2)
			{
				spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32, y * 64 + 51 + 4)), Game1.shadowTexture.Bounds, Color.White * alpha, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, (float)boundingBoxAt.Bottom / 15000f);
			}
			if (StardewValley.Pokemon.PokemonItemManager.IsPokeBall(base.QualifiedItemId))
			{
				Texture2D ballTex = StardewValley.Pokemon.PokemonItemManager.GetItemBallTexture();
				if (ballTex != null)
				{
					spriteBatch.Draw(ballTex, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 16), Color.White * alpha, 0f, new Vector2(8f, 8f), (scale.Y > 1f) ? getScale().Y : 3f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(isPassable() ? boundingBoxAt.Top : boundingBoxAt.Center.Y) / 10000f);
				}
				else
				{
					ParsedItemData dataOrErrorItem3 = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
					spriteBatch.Draw(dataOrErrorItem3.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), dataOrErrorItem3.GetSourceRect(), Color.White * alpha, 0f, new Vector2(8f, 8f), (scale.Y > 1f) ? getScale().Y : 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(isPassable() ? boundingBoxAt.Top : boundingBoxAt.Center.Y) / 10000f);
				}
			}
			else if (StardewValley.Pokemon.PokemonItemManager.TryGetCustomTexture(base.QualifiedItemId, out Texture2D customTex, out Microsoft.Xna.Framework.Rectangle customRect))
			{
				float drawScale = (scale.Y > 1f) ? getScale().Y : 2.5f;
				if (base.QualifiedItemId.Contains("PokemonPC"))
				{
					drawScale = 4.2f;
				}
				else if (base.QualifiedItemId.Contains("PokemonTradeTerminal"))
				{
					drawScale = 2.2f;
				}
				else if (base.QualifiedItemId.Contains("PokemonHealingMachine"))
				{
					drawScale = 3.2f;
				}
				spriteBatch.Draw(customTex, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), customRect, Color.White * alpha, 0f, new Vector2(customRect.Width / 2f, customRect.Height / 2f), drawScale, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(isPassable() ? boundingBoxAt.Top : boundingBoxAt.Center.Y) / 10000f);
			}
			else
			{
				ParsedItemData dataOrErrorItem3 = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
				spriteBatch.Draw(dataOrErrorItem3.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), dataOrErrorItem3.GetSourceRect(), Color.White * alpha, 0f, new Vector2(8f, 8f), (scale.Y > 1f) ? getScale().Y : 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(isPassable() ? boundingBoxAt.Top : boundingBoxAt.Center.Y) / 10000f);
			}
			if (IsSprinkler())
			{
				if (heldObject.Value != null)
				{
					Vector2 vector3 = Vector2.Zero;
					if (heldObject.Value.QualifiedItemId == "(O)913")
					{
						vector3 = new Vector2(0f, -20f);
					}
					ParsedItemData dataOrErrorItem4 = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
					spriteBatch.Draw(dataOrErrorItem4.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0)) + vector3), dataOrErrorItem4.GetSourceRect(1), Color.White * alpha, 0f, new Vector2(8f, 8f), (scale.Y > 1f) ? getScale().Y : 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(isPassable() ? boundingBoxAt.Top : boundingBoxAt.Bottom) / 10000f + 1E-05f);
				}
				if (base.SpecialVariable == 999999)
				{
					if (heldObject.Value != null && heldObject.Value.QualifiedItemId == "(O)913")
					{
						Torch.drawBasicTorch(spriteBatch, (float)(x * 64) - 2f, y * 64 - 32, (float)boundingBoxAt.Bottom / 10000f + 1E-06f);
					}
					else
					{
						Torch.drawBasicTorch(spriteBatch, (float)(x * 64) - 2f, y * 64 - 32 + 12, (float)(boundingBoxAt.Bottom + 2) / 10000f);
					}
				}
			}
		}
		if (!readyForHarvest.Value)
		{
			return;
		}
		float num2 = (float)((y + 1) * 64) / 10000f + tileLocation.X / 50000f;
		if (IsTapper() || base.QualifiedItemId.Equals("(BC)MushroomLog"))
		{
			num2 += 0.02f;
		}
		float num3 = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
		spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 - 8, (float)(y * 64 - 96 - 16) + num3)), new Microsoft.Xna.Framework.Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, num2 + 1E-06f);
		if (heldObject.Value == null)
		{
			return;
		}
		ParsedItemData dataOrErrorItem5 = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
		Texture2D texture3 = dataOrErrorItem5.GetTexture();
		if (heldObject.Value is ColoredObject coloredObject)
		{
			coloredObject.drawInMenu(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (float)(y * 64) - 96f - 8f + num3)), 1f, 0.75f, num2 + 1.1E-05f);
			return;
		}
		spriteBatch.Draw(texture3, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32, (float)(y * 64 - 64 - 8) + num3)), dataOrErrorItem5.GetSourceRect(), Color.White * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, num2 + 1E-05f);
		if (heldObject.Value.Stack > 1)
		{
			heldObject.Value.DrawMenuIcons(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (float)(y * 64 - 64 - 32) + num3 - 4f)), 1f, 1f, num2 + 1.2E-05f, StackDrawType.Draw, Color.White);
		}
		else if (heldObject.Value.Quality > 0)
		{
			heldObject.Value.DrawMenuIcons(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (float)(y * 64 - 64 - 32) + num3 - 4f)), 1f, 1f, num2 + 1.2E-05f, StackDrawType.HideButShowQuality, Color.White);
		}
	}

	public virtual void draw(SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha = 1f)
	{
		if (isTemporarilyInvisible)
		{
			return;
		}
		if (bigCraftable.Value)
		{
			Vector2 vector = getScale();
			vector *= 4f;
			Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile, yNonTile));
			Microsoft.Xna.Framework.Rectangle destinationRectangle = new Microsoft.Xna.Framework.Rectangle((int)(vector2.X - vector.X / 2f) + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(vector2.Y - vector.Y / 2f) + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(64f + vector.X), (int)(128f + vector.Y / 2f));
			int offset = 0;
			if (showNextIndex.Value)
			{
				offset = 1;
			}
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
			spriteBatch.Draw(dataOrErrorItem.GetTexture(), destinationRectangle, dataOrErrorItem.GetSourceRect(offset, base.ParentSheetIndex), Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
			if (base.QualifiedItemId == "(BC)17" && MinutesUntilReady > 0)
			{
				spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(vector2) + new Vector2(32f, 0f), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 435, 16, 16), Color.White * alpha, scale.X, new Vector2(8f, 8f), 4f, SpriteEffects.None, layerDepth);
			}
			if (isLamp.Value && Game1.isDarkOut(Location))
			{
				spriteBatch.Draw(Game1.mouseCursors, vector2 + new Vector2(-32f, -32f), new Microsoft.Xna.Framework.Rectangle(88, 1779, 32, 32), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth);
			}
		}
		else if (!Game1.eventUp || !Game1.CurrentEvent.isTileWalkedOn(xNonTile / 64, yNonTile / 64))
		{
			if (base.QualifiedItemId != "(O)590" && base.QualifiedItemId != "(O)SeedSpot" && fragility.Value != 2)
			{
				spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile + 32, yNonTile + 51 + 4)), Game1.shadowTexture.Bounds, Color.White * alpha, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, layerDepth - 1E-06f);
			}
			if (StardewValley.Pokemon.PokemonItemManager.IsPokeBall(base.QualifiedItemId))
			{
				Texture2D ballTex = StardewValley.Pokemon.PokemonItemManager.GetItemBallTexture();
				if (ballTex != null)
				{
					spriteBatch.Draw(ballTex, Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), yNonTile + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 16), Color.White * alpha, 0f, new Vector2(8f, 8f), (scale.Y > 1f) ? getScale().Y : 3f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
				}
				else
				{
					ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
					spriteBatch.Draw(dataOrErrorItem2.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), yNonTile + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), dataOrErrorItem2.GetSourceRect(0, base.ParentSheetIndex), Color.White * alpha, 0f, new Vector2(8f, 8f), (scale.Y > 1f) ? getScale().Y : 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
				}
			}
			else if (StardewValley.Pokemon.PokemonItemManager.TryGetCustomTexture(base.QualifiedItemId, out Texture2D customTex, out Microsoft.Xna.Framework.Rectangle customRect))
			{
				float drawScale = (scale.Y > 1f) ? getScale().Y : 2.5f;
				if (base.QualifiedItemId.Contains("PokemonPC"))
				{
					drawScale = 4.2f;
				}
				else if (base.QualifiedItemId.Contains("PokemonTradeTerminal"))
				{
					drawScale = 2.2f;
				}
				else if (base.QualifiedItemId.Contains("PokemonHealingMachine"))
				{
					drawScale = 3.2f;
				}
				spriteBatch.Draw(customTex, Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), yNonTile + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), customRect, Color.White * alpha, 0f, new Vector2(customRect.Width / 2f, customRect.Height / 2f), drawScale, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
			}
			else
			{
				ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
				spriteBatch.Draw(dataOrErrorItem2.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), yNonTile + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), dataOrErrorItem2.GetSourceRect(0, base.ParentSheetIndex), Color.White * alpha, 0f, new Vector2(8f, 8f), (scale.Y > 1f) ? getScale().Y : 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
			}
		}
	}

	public override int maximumStackSize()
	{
		switch (base.QualifiedItemId)
		{
		case "(O)79":
		case "(O)842":
		case "(O)911":
			return 1;
		default:
			if (base.Category == -22)
			{
				return 1;
			}
			return 999;
		}
	}

	public virtual void hoverAction()
	{
		hovering = true;
	}

	public virtual bool clicked(Farmer who)
	{
		return false;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		if (!bigCraftable.Value)
		{
			return new Object(base.ItemId, 1);
		}
		return new Object(tileLocation.Value, base.ItemId);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is Object obj)
		{
			Scale = obj.scale;
			IsSpawnedObject = obj.isSpawnedObject.Value;
			Price = obj.price.Value;
			Edibility = obj.edibility.Value;
			name = obj.name;
			displayNameFormat = obj.displayNameFormat;
			TileLocation = obj.TileLocation;
			uses.Value = obj.uses.Value;
			questItem.Value = obj.questItem.Value;
			questId.Value = obj.questId.Value;
			preserve.Value = obj.preserve.Value;
			preservedParentSheetIndex.Value = obj.preservedParentSheetIndex.Value;
			orderData.Value = obj.orderData.Value;
			owner.Value = obj.owner.Value;
		}
	}

	public override bool canBePlacedHere(GameLocation l, Vector2 tile, CollisionMask collisionMask = CollisionMask.All, bool showError = false)
	{
		if (base.QualifiedItemId == "(O)PokemonSuperRod" ||
		    StardewValley.Pokemon.PokemonItemUse.IsUsable(base.QualifiedItemId) ||
		    (base.QualifiedItemId != null && base.QualifiedItemId.Contains("Apricorn")))
		{
			return false;
		}
		if (base.QualifiedItemId == "(O)710")
		{
			if (CrabPot.IsValidCrabPotLocationTile(l, (int)tile.X, (int)tile.Y))
			{
				return true;
			}
			return false;
		}
		if (IsTapper() && l.terrainFeatures.GetValueOrDefault(tile) is Tree tree && !l.objects.ContainsKey(tile) && (tree.GetData()?.CanBeTapped() ?? false))
		{
			return true;
		}
		string qualifiedItemId = base.QualifiedItemId;
		if (!(qualifiedItemId == "(O)805"))
		{
			if (qualifiedItemId == "(O)419")
			{
				if (l.terrainFeatures.GetValueOrDefault(tile) is Tree tree2)
				{
					return !tree2.stopGrowingMoss.Value;
				}
				return false;
			}
		}
		else if (l.terrainFeatures.GetValueOrDefault(tile) is Tree)
		{
			return true;
		}
		if (isWildTreeSeed(base.ItemId))
		{
			if (!l.CanItemBePlacedHere(tile, itemIsPassable: true, collisionMask))
			{
				return false;
			}
			if (!canPlaceWildTreeSeed(l, tile, out var deniedMessage))
			{
				if (showError && deniedMessage != null)
				{
					Game1.showRedMessage(deniedMessage);
				}
				return false;
			}
			return true;
		}
		switch (category.Value)
		{
		case -74:
		{
			HoeDirt hoeDirtAtTile2 = l.GetHoeDirtAtTile(tile);
			Object objectAtTile = l.getObjectAtTile((int)tile.X, (int)tile.Y);
			IndoorPot indoorPot2 = objectAtTile as IndoorPot;
			if (hoeDirtAtTile2?.crop != null || (hoeDirtAtTile2 == null && l.terrainFeatures.TryGetValue(tile, out var _)))
			{
				return false;
			}
			if (IsFruitTreeSapling())
			{
				if (objectAtTile != null)
				{
					return false;
				}
				if (hoeDirtAtTile2 == null)
				{
					if (FruitTree.IsTooCloseToAnotherTree(tile, l, !IsFruitTreeSapling()))
					{
						if (showError)
						{
							Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13060"));
						}
						return false;
					}
					if (FruitTree.IsGrowthBlocked(tile, l))
					{
						if (showError)
						{
							Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:FruitTree_PlacementWarning", DisplayName));
						}
						return false;
					}
					if (!l.CanItemBePlacedHere(tile, itemIsPassable: true, collisionMask))
					{
						return false;
					}
					if (!l.CanPlantTreesHere(base.ItemId, (int)tile.X, (int)tile.Y, out var deniedMessage2))
					{
						if (showError && deniedMessage2 != null)
						{
							Game1.showRedMessage(deniedMessage2);
						}
						return false;
					}
					return true;
				}
				return false;
			}
			if (IsTeaSapling())
			{
				bool flag = indoorPot2 != null && indoorPot2.bush.Value == null && indoorPot2.hoeDirt.Value.crop == null;
				if (flag)
				{
					if (!l.IsOutdoors)
					{
						return true;
					}
				}
				else
				{
					if (objectAtTile != null || hoeDirtAtTile2 != null)
					{
						return false;
					}
					if (!l.CanItemBePlacedHere(tile, itemIsPassable: true, collisionMask))
					{
						return false;
					}
					if (l.IsGreenhouse && l.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") == null)
					{
						return false;
					}
				}
				if (!l.CheckItemPlantRules(base.QualifiedItemId, flag, l.isOutdoors.Value || l.IsGreenhouse, out var deniedMessage3))
				{
					if (showError && deniedMessage3 != null)
					{
						Game1.showRedMessage(Game1.content.LoadString(deniedMessage3));
					}
					return false;
				}
				return true;
			}
			if (IsWildTreeSapling())
			{
				if (objectAtTile != null)
				{
					return false;
				}
				if (FruitTree.IsTooCloseToAnotherTree(tile, l, fruitTreesOnly: true))
				{
					if (showError)
					{
						Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13060_Fruit"));
					}
					return false;
				}
				return l.CanItemBePlacedHere(tile, itemIsPassable: true, collisionMask);
			}
			if (this.HasTypeObject())
			{
				if (indoorPot2 != null)
				{
					if (indoorPot2.IsPlantableItem(this) && indoorPot2.bush.Value == null)
					{
						return indoorPot2.hoeDirt.Value.canPlantThisSeedHere(base.ItemId);
					}
					return false;
				}
				if (hoeDirtAtTile2 != null && l.CanItemBePlacedHere(tile, itemIsPassable: true, collisionMask) && hoeDirtAtTile2.canPlantThisSeedHere(base.ItemId))
				{
					return true;
				}
			}
			return false;
		}
		case -19:
		{
			HoeDirt hoeDirtAtTile = l.GetHoeDirtAtTile(tile);
			if (hoeDirtAtTile != null && hoeDirtAtTile.CanApplyFertilizer(base.QualifiedItemId))
			{
				if (l.getObjectAtTile((int)tile.X, (int)tile.Y) is IndoorPot indoorPot)
				{
					return indoorPot.IsPlantableItem(this);
				}
				return true;
			}
			return false;
		}
		default:
			if (l != null)
			{
				Vector2 vector = tile * 64f * 64f;
				vector.X += 32f;
				vector.Y += 32f;
				foreach (Furniture item in l.furniture)
				{
					if (item.furniture_type.Value == 11 && item.GetBoundingBox().Contains((int)vector.X, (int)vector.Y) && item.heldObject.Value == null)
					{
						return true;
					}
				}
			}
			if (IsFloorPathItem())
			{
				collisionMask &= ~CollisionMask.Buildings;
			}
			return l.CanItemBePlacedHere(tile, isPassable(), collisionMask);
		}
	}

	public override bool isPlaceable()
	{
		if (base.QualifiedItemId == "(O)PokemonSuperRod" ||
		    StardewValley.Pokemon.PokemonItemUse.IsUsable(base.QualifiedItemId) ||
		    (base.QualifiedItemId != null && base.QualifiedItemId.Contains("Apricorn")))
		{
			return false;
		}
		if (HasContextTag("placeable"))
		{
			return true;
		}
		if (HasContextTag("not_placeable"))
		{
			return false;
		}
		if (type.Value != null && (base.Category == -8 || base.Category == -9 || Type == "Crafting" || isSapling() || base.QualifiedItemId == "(O)710" || base.Category == -74 || base.Category == -19) && (edibility.Value < 0 || IsWildTreeSapling()))
		{
			return true;
		}
		return false;
	}

	public bool IsConsideredReadyMachineForComputer()
	{
		if (bigCraftable.Value && heldObject.Value != null)
		{
			if (!(heldObject.Value is Chest chest))
			{
				return minutesUntilReady.Value <= 0;
			}
			if (!chest.isEmpty())
			{
				return true;
			}
		}
		return false;
	}

	public MachineData GetMachineData()
	{
		return DataLoader.Machines(Game1.content).GetValueOrDefault(base.QualifiedItemId);
	}

	public virtual bool isSapling()
	{
		if (!IsTeaSapling() && !IsWildTreeSapling())
		{
			return IsFruitTreeSapling();
		}
		return true;
	}

	public virtual bool IsTeaSapling()
	{
		return base.QualifiedItemId == "(O)251";
	}

	public virtual bool IsFruitTreeSapling()
	{
		if (this.HasTypeObject())
		{
			return Game1.fruitTreeData.ContainsKey(base.ItemId);
		}
		return false;
	}

	public virtual bool IsWildTreeSapling()
	{
		if (this.HasTypeObject())
		{
			return isWildTreeSeed(base.ItemId);
		}
		return false;
	}

	public virtual bool IsFloorPathItem()
	{
		if (this.HasTypeObject())
		{
			return IsFloorPathItem(base.ItemId);
		}
		return false;
	}

	public static bool IsFloorPathItem(string itemId)
	{
		if (itemId != null)
		{
			return Flooring.GetFloorPathItemLookup().ContainsKey(itemId);
		}
		return false;
	}

	public virtual bool IsFenceItem()
	{
		if (this.HasTypeObject())
		{
			return Fence.GetFenceLookup().ContainsKey(base.ItemId);
		}
		return false;
	}

	public static bool isWildTreeSeed(string itemId)
	{
		if (itemId != null)
		{
			return Tree.GetWildTreeSeedLookup().ContainsKey(itemId);
		}
		return false;
	}

	private bool canPlaceWildTreeSeed(GameLocation location, Vector2 tile, out string deniedMessage)
	{
		if (location.IsNoSpawnTile(tile, "Tree", ignoreTileSheetProperties: true))
		{
			deniedMessage = null;
			return false;
		}
		if (location.IsNoSpawnTile(tile, "Tree") && !location.doesEitherTileOrTileIndexPropertyEqual((int)tile.X, (int)tile.Y, "CanPlantTrees", "Back", "T"))
		{
			deniedMessage = null;
			return false;
		}
		if (location.objects.ContainsKey(tile))
		{
			deniedMessage = null;
			return false;
		}
		if (location.terrainFeatures.TryGetValue(tile, out var value) && !(value is HoeDirt))
		{
			deniedMessage = null;
			return false;
		}
		if (!location.CanPlantTreesHere(base.ItemId, (int)tile.X, (int)tile.Y, out deniedMessage))
		{
			return false;
		}
		return location.CheckItemPlantRules(base.QualifiedItemId, isGardenPot: false, location is Farm || location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") != null || location.doesEitherTileOrTileIndexPropertyEqual((int)tile.X, (int)tile.Y, "CanPlantTrees", "Back", "T"), out deniedMessage);
	}

	public virtual bool IsSprinkler()
	{
		if (GetBaseRadiusForSprinkler() >= 0)
		{
			return true;
		}
		return false;
	}

	/// <summary>Get whether this is a stone litter item which can be broken by a pickaxe.</summary>
	public bool IsBreakableStone()
	{
		if (base.Category == -999)
		{
			return Name == "Stone";
		}
		return false;
	}

	/// <summary>Get whether this is a text sign which shows the <see cref="P:StardewValley.Object.SignText" /> text on hover.</summary>
	public virtual bool IsTextSign()
	{
		return base.ItemId == "TextSign";
	}

	/// <summary>Get whether this is a twig litter item.</summary>
	public bool IsTwig()
	{
		if (base.Category == -999)
		{
			return Name == "Twig";
		}
		return false;
	}

	public bool isDebrisOrForage()
	{
		if (!IsWeeds() && !IsBreakableStone() && !IsTwig())
		{
			return isForage();
		}
		return true;
	}

	/// <summary>Get whether this is a weed litter item.</summary>
	public bool IsWeeds()
	{
		if (base.Category == -999)
		{
			return Name.ContainsIgnoreCase("weeds");
		}
		return false;
	}

	/// <summary>Get whether this is a tree tapper item.</summary>
	public virtual bool IsTapper()
	{
		return HasContextTag("tapper_item");
	}

	/// <summary>Get whether this is an ore bar.</summary>
	public virtual bool IsBar()
	{
		if (!(base.QualifiedItemId == "(O)334") && !(base.QualifiedItemId == "(O)335") && !(base.QualifiedItemId == "(O)336") && !(base.QualifiedItemId == "(O)337"))
		{
			return base.QualifiedItemId == "(O)910";
		}
		return true;
	}

	/// <summary>Get the item ID which was preserved as part of this item (e.g. the tulip ID if this item is tulip honey).</summary>
	/// <inheritdoc cref="M:StardewValley.Object.GetPreservedItemId(System.Nullable{StardewValley.Object.PreserveType},System.String)" path="/remarks" />
	public string GetPreservedItemId()
	{
		return GetPreservedItemId(preserve.Value, preservedParentSheetIndex.Value);
	}

	/// <summary>Get the item ID which was preserved as part of an item (e.g. the tulip ID for a tulip honey item).</summary>
	/// <param name="preserveType">The preserve type.</param>
	/// <param name="preservedId">The item ID for the flavor item, like <c>(O)258</c> for the blueberry in "Blueberry Wine".</param>
	/// <remarks>This returns the <see cref="F:StardewValley.Object.preservedParentSheetIndex" />, except in special cases like <see cref="F:StardewValley.Object.WildHoneyPreservedId" /> where it doesn't match an item ID.</remarks>
	public static string GetPreservedItemId(PreserveType? preserveType, string preservedId)
	{
		if (preservedId == "-1" && preserveType == PreserveType.Honey)
		{
			preservedId = null;
		}
		return preservedId;
	}

	public virtual int GetModifiedRadiusForSprinkler()
	{
		int num = GetBaseRadiusForSprinkler();
		if (num < 0)
		{
			return -1;
		}
		if (heldObject.Value != null && heldObject.Value.QualifiedItemId == "(O)915")
		{
			num++;
		}
		return num;
	}

	public virtual int GetBaseRadiusForSprinkler()
	{
		return base.QualifiedItemId switch
		{
			"(O)599" => 0, 
			"(O)621" => 1, 
			"(O)645" => 2, 
			_ => -1, 
		};
	}

	/// <summary>Check whether the object can be added to a location, and (sometimes) add it to the location.</summary>
	/// <param name="location">The location in which to place it.</param>
	/// <param name="x">The X tile position at which to place it.</param>
	/// <param name="y">The Y tile position at which to place it.</param>
	/// <param name="who">The player placing the object, if applicable.</param>
	/// <returns>Returns whether the object should be (or was) added to the location.</returns>
	/// <remarks>For legacy reasons, the behavior of this method is inconsistent. It'll sometimes add the object to the location itself, and sometimes expect the caller to do it.</remarks>
	public virtual bool placementAction(GameLocation location, int x, int y, Farmer who = null)
	{
		Vector2 vector = new Vector2(x / 64, y / 64);
		health = 10;
		Location = location;
		TileLocation = vector;
		owner.Value = who?.UniqueMultiplayerID ?? Game1.player.UniqueMultiplayerID;
		if (!bigCraftable.Value && !(this is Furniture))
		{
			if (IsSprinkler() && location.doesTileHavePropertyNoNull((int)vector.X, (int)vector.Y, "NoSprinklers", "Back") == "T")
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:NoSprinklers"));
				return false;
			}
			if (IsWildTreeSapling())
			{
				if (!canPlaceWildTreeSeed(location, vector, out var deniedMessage))
				{
					if (deniedMessage == null)
					{
						deniedMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13021");
					}
					Game1.showRedMessage(deniedMessage);
					return false;
				}
				string text = Tree.ResolveTreeTypeFromSeed(base.QualifiedItemId);
				if (text != null)
				{
					Game1.stats.Increment("wildtreesplanted");
					location.terrainFeatures.Remove(vector);
					location.terrainFeatures.Add(vector, new Tree(text, 0));
					location.playSound("dirtyHit");
					return true;
				}
				return false;
			}
			if (IsFloorPathItem())
			{
				if (location.terrainFeatures.ContainsKey(vector))
				{
					return false;
				}
				string text2 = Flooring.GetFloorPathItemLookup()[base.ItemId];
				location.terrainFeatures.Add(vector, new Flooring(text2));
				if (Game1.floorPathData.TryGetValue(text2, out var value) && value.PlacementSound != null)
				{
					location.playSound(value.PlacementSound);
				}
				return true;
			}
			if (ItemContextTagManager.HasBaseTag(base.QualifiedItemId, "torch_item"))
			{
				if (location.objects.ContainsKey(vector))
				{
					return false;
				}
				location.removeLightSource(GenerateLightSourceId(tileLocation.Value));
				location.removeLightSource(lightSource?.Id);
				new Torch(1, base.ItemId).placementAction(location, x, y, who ?? Game1.player);
				return true;
			}
			if (IsFenceItem())
			{
				if (location.objects.ContainsKey(vector))
				{
					return false;
				}
				FenceData fenceData = Fence.GetFenceLookup()[base.ItemId];
				location.objects.Add(vector, new Fence(vector, base.ItemId, base.ItemId == "325"));
				if (fenceData.PlacementSound != null)
				{
					location.playSound(fenceData.PlacementSound);
				}
				return true;
			}
			switch (base.QualifiedItemId)
			{
			case "(O)TentKit":
			{
				if (location == null || !location.IsOutdoors)
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture_Outdoors_Message"));
					return false;
				}
				if (Utility.isFestivalDay((Game1.dayOfMonth + 1) % 28, (Game1.dayOfMonth == 28) ? ((Season)((int)(Game1.season + 1) % 4)) : Game1.season, location.GetLocationContextId()))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\1_6_Strings:FestivalTentWarning"));
					return false;
				}
				PassiveFestivalData data = null;
				string id = null;
				if (Utility.TryGetPassiveFestivalDataForDay((Game1.dayOfMonth + 1) % 28, (Game1.dayOfMonth == 28) ? ((Season)((int)(Game1.season + 1) % 4)) : Game1.season, null, out id, out data) && data != null)
				{
					if (data.MapReplacements != null)
					{
						foreach (string key in data.MapReplacements.Keys)
						{
							if (key.Equals(location.Name))
							{
								Game1.showRedMessage(Game1.content.LoadString("Strings\\1_6_Strings:FestivalTentWarning"));
								return false;
							}
						}
					}
					if (((id.Equals("TroutDerby") && location.Name.Equals("Forest")) || (id.Equals("SquidFest") && location.Name.Equals("Beach"))) && data.StartDay > Game1.dayOfMonth)
					{
						Game1.showRedMessage(Game1.content.LoadString("Strings\\1_6_Strings:FestivalTentWarning"));
						return false;
					}
				}
				if (who != null)
				{
					Microsoft.Xna.Framework.Rectangle rectangle = Microsoft.Xna.Framework.Rectangle.Empty;
					switch (Utility.getDirectionFromChange(vector, who.Tile))
					{
					case 0:
						rectangle = new Microsoft.Xna.Framework.Rectangle((int)(vector.X - 1f), (int)(vector.Y - 1f), 3, 2);
						break;
					case 1:
						rectangle = new Microsoft.Xna.Framework.Rectangle((int)vector.X, (int)(vector.Y - 1f), 3, 2);
						break;
					case 2:
						rectangle = new Microsoft.Xna.Framework.Rectangle((int)(vector.X - 1f), (int)vector.Y, 3, 2);
						break;
					case 3:
						rectangle = new Microsoft.Xna.Framework.Rectangle((int)(vector.X - 2f), (int)(vector.Y - 1f), 3, 2);
						break;
					}
					if (rectangle != Microsoft.Xna.Framework.Rectangle.Empty && location.isAreaClear(rectangle))
					{
						location.largeTerrainFeatures.Add(new Tent(new Vector2(rectangle.X + 1, rectangle.Y + 1)));
						Game1.playSound("moss_cut");
						Game1.playSound("woodyHit");
						new Microsoft.Xna.Framework.Rectangle(rectangle.X * 64, rectangle.Y * 64, 192, 128);
						Utility.addDirtPuffs(location, rectangle.X, rectangle.Y, 3, 2, 9);
						return true;
					}
					Game1.showRedMessage(Game1.content.LoadString("Strings\\1_6_Strings:Tent_Blocked"));
					return false;
				}
				break;
			}
			case "(O)926":
				if (location.objects.ContainsKey(vector) || location.terrainFeatures.ContainsKey(vector))
				{
					return false;
				}
				location.objects.Add(vector, new Torch("278", bigCraftable: true)
				{
					Fragility = 1,
					destroyOvernight = true
				});
				Utility.addSmokePuff(location, new Vector2(x, y));
				Utility.addSmokePuff(location, new Vector2(x + 16, y + 16));
				Utility.addSmokePuff(location, new Vector2(x + 32, y));
				Utility.addSmokePuff(location, new Vector2(x + 48, y + 16));
				Utility.addSmokePuff(location, new Vector2(x + 32, y + 32));
				Game1.playSound("fireball");
				return true;
			case "(O)286":
			{
				foreach (TemporaryAnimatedSprite temporarySprite in location.temporarySprites)
				{
					if (temporarySprite.position.Equals(vector * 64f))
					{
						return false;
					}
				}
				int num = Game1.random.Next();
				location.playSound("thudStep");
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(base.ParentSheetIndex, 100f, 1, 24, vector * 64f, flicker: true, flipped: false, location, who)
				{
					shakeIntensity = 0.5f,
					shakeIntensityChange = 0.002f,
					extraInfoForEndBehavior = num,
					endFunction = location.removeTemporarySpritesWithID
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f + new Vector2(5f, 3f) * 4f, flicker: true, flipped: false, (float)(y + 7) / 10000f, 0f, Color.Yellow, 4f, 0f, 0f, 0f)
				{
					id = num
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f + new Vector2(5f, 3f) * 4f, flicker: true, flipped: true, (float)(y + 7) / 10000f, 0f, Color.Orange, 4f, 0f, 0f, 0f)
				{
					delayBeforeAnimationStart = 100,
					id = num
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f + new Vector2(5f, 3f) * 4f, flicker: true, flipped: false, (float)(y + 7) / 10000f, 0f, Color.White, 3f, 0f, 0f, 0f)
				{
					delayBeforeAnimationStart = 200,
					id = num
				});
				location.netAudio.StartPlaying("fuse");
				return true;
			}
			case "(O)287":
			{
				foreach (TemporaryAnimatedSprite temporarySprite2 in location.temporarySprites)
				{
					if (temporarySprite2.position.Equals(vector * 64f))
					{
						return false;
					}
				}
				int num = Game1.random.Next();
				location.playSound("thudStep");
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(base.ParentSheetIndex, 100f, 1, 24, vector * 64f, flicker: true, flipped: false, location, who)
				{
					shakeIntensity = 0.5f,
					shakeIntensityChange = 0.002f,
					extraInfoForEndBehavior = num,
					endFunction = location.removeTemporarySpritesWithID
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f, flicker: true, flipped: false, (float)(y + 7) / 10000f, 0f, Color.Yellow, 4f, 0f, 0f, 0f)
				{
					id = num
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f, flicker: true, flipped: false, (float)(y + 7) / 10000f, 0f, Color.Orange, 4f, 0f, 0f, 0f)
				{
					delayBeforeAnimationStart = 100,
					id = num
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f, flicker: true, flipped: false, (float)(y + 7) / 10000f, 0f, Color.White, 3f, 0f, 0f, 0f)
				{
					delayBeforeAnimationStart = 200,
					id = num
				});
				location.netAudio.StartPlaying("fuse");
				return true;
			}
			case "(O)288":
			{
				foreach (TemporaryAnimatedSprite temporarySprite3 in location.temporarySprites)
				{
					if (temporarySprite3.position.Equals(vector * 64f))
					{
						return false;
					}
				}
				int num = Game1.random.Next();
				location.playSound("thudStep");
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(base.ParentSheetIndex, 100f, 1, 24, vector * 64f, flicker: true, flipped: false, location, who)
				{
					shakeIntensity = 0.5f,
					shakeIntensityChange = 0.002f,
					extraInfoForEndBehavior = num,
					endFunction = location.removeTemporarySpritesWithID
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f + new Vector2(5f, 0f) * 4f, flicker: true, flipped: false, (float)(y + 7) / 10000f, 0f, Color.Yellow, 4f, 0f, 0f, 0f)
				{
					id = num
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f + new Vector2(5f, 0f) * 4f, flicker: true, flipped: true, (float)(y + 7) / 10000f, 0f, Color.Orange, 4f, 0f, 0f, 0f)
				{
					delayBeforeAnimationStart = 100,
					id = num
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 53f, 5, 9, vector * 64f + new Vector2(5f, 0f) * 4f, flicker: true, flipped: false, (float)(y + 7) / 10000f, 0f, Color.White, 3f, 0f, 0f, 0f)
				{
					delayBeforeAnimationStart = 200,
					id = num
				});
				location.netAudio.StartPlaying("fuse");
				return true;
			}
			case "(O)893":
			case "(O)894":
			case "(O)895":
			{
				int num2 = base.ParentSheetIndex - 893;
				int x2 = 256 + num2 * 16;
				foreach (TemporaryAnimatedSprite temporarySprite4 in location.temporarySprites)
				{
					if (temporarySprite4.position.Equals(vector * 64f))
					{
						return false;
					}
				}
				int num = Game1.random.Next();
				int extraInfoForEndBehavior = Game1.random.Next();
				location.playSound("thudStep");
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(x2, 397, 16, 16), 2400f, 1, 1, vector * 64f, flicker: false, flipped: false, -1f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					shakeIntensity = 0.5f,
					shakeIntensityChange = 0.002f,
					extraInfoForEndBehavior = num,
					endFunction = location.removeTemporarySpritesWithID,
					layerDepth = (vector.Y * 64f + 64f - 16f) / 10000f
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(x2, 397, 16, 16), 800f, 1, 0, vector * 64f, flicker: false, flipped: false, -1f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					fireworkType = num2,
					delayBeforeAnimationStart = 2400,
					acceleration = new Vector2(0f, -0.36f + (float)Game1.random.Next(10) / 100f),
					drawAboveAlwaysFront = true,
					startSound = "firework",
					shakeIntensity = 0.5f,
					shakeIntensityChange = 0.002f,
					extraInfoForEndBehavior = extraInfoForEndBehavior,
					endFunction = location.removeTemporarySpritesWithID,
					id = Game1.random.Next(20, 31),
					Parent = location,
					owner = who
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 40f, 5, 5, vector * 64f + new Vector2(11f, 12f) * 4f, flicker: true, flipped: false, (float)(y + 7) / 10000f, 0f, Color.Yellow, 4f, 0f, 0f, 0f)
				{
					id = num
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 40f, 5, 5, vector * 64f + new Vector2(11f, 12f) * 4f, flicker: true, flipped: true, (float)(y + 7) / 10000f, 0f, Color.Orange, 4f, 0f, 0f, 0f)
				{
					delayBeforeAnimationStart = 100,
					id = num
				});
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 40f, 5, 5, vector * 64f + new Vector2(11f, 12f) * 4f, flicker: true, flipped: false, (float)(y + 7) / 10000f, 0f, Color.White, 3f, 0f, 0f, 0f)
				{
					delayBeforeAnimationStart = 200,
					id = num
				});
				location.netAudio.StartPlaying("fuse");
				DelayedAction.functionAfterDelay(delegate
				{
					location.netAudio.StopPlaying("fuse");
				}, 2400);
				return true;
			}
			case "(O)297":
				if (location.objects.ContainsKey(vector) || location.terrainFeatures.ContainsKey(vector))
				{
					return false;
				}
				location.terrainFeatures.Add(vector, new Grass(1, 4));
				location.playSound("dirtyHit");
				return true;
			case "(O)BlueGrassStarter":
				if (location.objects.ContainsKey(vector) || location.terrainFeatures.ContainsKey(vector))
				{
					return false;
				}
				location.terrainFeatures.Add(vector, new Grass(7, 4));
				location.playSound("dirtyHit");
				return true;
			case "(O)710":
				if (!CrabPot.IsValidCrabPotLocationTile(location, (int)vector.X, (int)vector.Y))
				{
					return false;
				}
				new CrabPot().placementAction(location, x, y, who);
				return true;
			case "(O)805":
			{
				if (location.terrainFeatures.TryGetValue(vector, out var value3) && value3 is Tree tree2)
				{
					return tree2.fertilize();
				}
				return false;
			}
			case "(O)419":
			{
				if (location.terrainFeatures.TryGetValue(vector, out var value2) && value2 is Tree tree && !tree.stopGrowingMoss.Value)
				{
					tree.hasMoss.Value = false;
					tree.stopGrowingMoss.Value = true;
					Game1.playSound("slosh");
					Game1.playSound("glug");
					Utility.makeTemporarySpriteJuicier(new TemporaryAnimatedSprite(21, tree.Tile * 64f + new Vector2(0f, -64f), new Color(165, 100, 255), 8, flipped: false, 80f, 1, -1, (tree.Tile.Y + 1.25f) * 64f / 10000f, 128), location);
					return true;
				}
				return false;
			}
			}
		}
		else
		{
			if (IsTapper())
			{
				if (location.terrainFeatures.TryGetValue(vector, out var value4) && value4 is Tree tree3 && tree3.growthStage.Value >= 5 && !tree3.stump.Value && !location.objects.ContainsKey(vector) && (!tree3.isTemporaryGreenRainTree.Value || Game1.season != Season.Summer))
				{
					WildTreeData data2 = tree3.GetData();
					if (data2 != null && data2.CanBeTapped())
					{
						Object obj = (Object)getOne();
						obj.heldObject.Value = null;
						obj.TileLocation = vector;
						location.objects.Add(vector, obj);
						tree3.tapped.Value = true;
						tree3.UpdateTapperProduct(obj);
						location.playSound("axe");
						return true;
					}
				}
				return false;
			}
			if (HasContextTag("sign_item"))
			{
				if (location.objects.ContainsKey(vector))
				{
					return false;
				}
				location.objects.Add(vector, new Sign(vector, base.ItemId));
				location.playSound("axe");
				return true;
			}
			if (HasContextTag("torch_item"))
			{
				if (location.objects.ContainsKey(vector))
				{
					return false;
				}
				Torch torch = new Torch(base.ItemId, bigCraftable: true);
				torch.shakeTimer = 25;
				torch.placementAction(location, x, y, who);
				return true;
			}
			string qualifiedItemId = base.QualifiedItemId;
			if (qualifiedItemId != null)
			{
				int length = qualifiedItemId.Length;
				if (length <= 7)
				{
					switch (length)
					{
					case 7:
						switch (qualifiedItemId[6])
						{
						case '8':
							switch (qualifiedItemId)
							{
							case "(BC)108":
							{
								Object obj3 = (Object)getOne();
								obj3.ResetParentSheetIndex();
								Season season = location.GetSeason();
								if (Location.IsOutdoors && (season == Season.Winter || season == Season.Fall))
								{
									obj3.ParentSheetIndex = 109;
								}
								location.Objects.Add(vector, obj3);
								Game1.playSound("axe");
								return true;
							}
							case "(BC)208":
								location.objects.Add(vector, new Workbench(vector));
								location.playSound("axe");
								return true;
							case "(BC)248":
								if (location.objects.ContainsKey(vector) || location is MineShaft || location is VolcanoDungeon)
								{
									Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
									return false;
								}
								location.objects.Add(vector, new Chest(playerChest: true, vector, base.ItemId)
								{
									name = name,
									shakeTimer = 50
								});
								location.playSound("axe");
								return true;
							case "(BC)238":
							{
								if (!(location is Farm))
								{
									Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:OnlyPlaceOnFarm"));
									return false;
								}
								Vector2 vector2 = Vector2.Zero;
								Vector2 vector3 = Vector2.Zero;
								foreach (KeyValuePair<Vector2, Object> pair in location.objects.Pairs)
								{
									if (pair.Value.QualifiedItemId == "(BC)238")
									{
										if (vector2.Equals(Vector2.Zero))
										{
											vector2 = pair.Key;
										}
										else if (vector3.Equals(Vector2.Zero))
										{
											vector3 = pair.Key;
											break;
										}
									}
								}
								if (!vector2.Equals(Vector2.Zero) && !vector3.Equals(Vector2.Zero))
								{
									Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:OnlyPlaceTwo"));
									return false;
								}
								break;
							}
							}
							break;
						case '2':
							if (!(qualifiedItemId == "(BC)232"))
							{
								break;
							}
							goto IL_1d66;
						case '0':
							if (!(qualifiedItemId == "(BC)130"))
							{
								break;
							}
							goto IL_1d66;
						case '3':
							if (qualifiedItemId == "(BC)163")
							{
								location.objects.Add(vector, new Cask(vector));
								location.playSound("hammer");
							}
							break;
						case '5':
						{
							if (!(qualifiedItemId == "(BC)165"))
							{
								if (!(qualifiedItemId == "(BC)275"))
								{
									break;
								}
								if (location.objects.ContainsKey(vector) || location is MineShaft || location is VolcanoDungeon)
								{
									Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
									return false;
								}
								Chest chest2 = new Chest(playerChest: true, vector, base.ItemId)
								{
									name = name,
									shakeTimer = 50
								};
								chest2.lidFrameCount.Value = 2;
								location.objects.Add(vector, chest2);
								location.playSound("axe");
								return true;
							}
							Object obj2 = ItemRegistry.Create<Object>("(BC)165");
							location.objects.Add(vector, obj2);
							obj2.heldObject.Value = new Chest();
							location.playSound("axe");
							return true;
						}
						case '9':
						{
							if (!(qualifiedItemId == "(BC)209"))
							{
								break;
							}
							MiniJukebox miniJukebox = (this as MiniJukebox) ?? new MiniJukebox(vector);
							location.objects.Add(vector, miniJukebox);
							miniJukebox.RegisterToLocation();
							location.playSound("hammer");
							return true;
						}
						case '1':
						{
							if (!(qualifiedItemId == "(BC)211"))
							{
								break;
							}
							WoodChipper woodChipper = (this as WoodChipper) ?? new WoodChipper(vector);
							woodChipper.placementAction(location, x, y);
							location.objects.Add(vector, woodChipper);
							location.playSound("hammer");
							return true;
						}
						case '4':
						{
							if (!(qualifiedItemId == "(BC)214"))
							{
								if (!(qualifiedItemId == "(BC)254") || (location is AnimalHouse animalHouse && animalHouse.name.Value.Contains("Barn")))
								{
									break;
								}
								Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:MustBePlacedInBarn"));
								return false;
							}
							Phone value5 = (this as Phone) ?? new Phone(vector);
							location.objects.Add(vector, value5);
							location.playSound("hammer");
							return true;
						}
						case '6':
							{
								if (!(qualifiedItemId == "(BC)216"))
								{
									if (!(qualifiedItemId == "(BC)256"))
									{
										break;
									}
									if (location.objects.ContainsKey(vector) || location is MineShaft || location is VolcanoDungeon)
									{
										Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
										return false;
									}
									location.objects.Add(vector, new Chest(playerChest: true, vector, base.ItemId)
									{
										name = name,
										shakeTimer = 50
									});
									location.playSound("axe");
									return true;
								}
								if (location.objects.ContainsKey(vector))
								{
									Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
									return false;
								}
								if (!location.TryGetMapPropertyAs("AllowMiniFridges", out bool parsed, required: false))
								{
									if (location is FarmHouse { upgradeLevel: <1 })
									{
										Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:MiniFridge_NoKitchen"));
										return false;
									}
									parsed = location is FarmHouse || location is IslandFarmHouse;
								}
								if (!parsed)
								{
									Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
									return false;
								}
								Chest chest = new Chest("216", vector, 217, 2)
								{
									shakeTimer = 50
								};
								chest.fridge.Value = true;
								location.objects.Add(vector, chest);
								location.playSound("hammer");
								return true;
							}
							IL_1d66:
							if (location.objects.ContainsKey(vector) || location is MineShaft || location is VolcanoDungeon)
							{
								Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
								return false;
							}
							location.objects.Add(vector, new Chest(playerChest: true, vector, base.ItemId)
							{
								name = name,
								shakeTimer = 50
							});
							location.playSound((base.QualifiedItemId == "(BC)130") ? "axe" : "hammer");
							return true;
						}
						break;
					case 6:
						switch (qualifiedItemId[4])
						{
						case '6':
							if (qualifiedItemId == "(BC)62")
							{
								location.objects.Add(vector, new IndoorPot(vector));
							}
							break;
						case '7':
							if (!(qualifiedItemId == "(BC)71"))
							{
								break;
							}
							if (location is MineShaft mineShaft)
							{
								if (mineShaft.shouldCreateLadderOnThisLevel() && mineShaft.recursiveTryToCreateLadderDown(vector))
								{
									MineShaft.numberOfCraftedStairsUsedThisRun++;
									return true;
								}
								Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
							}
							else if (location.Name.Equals("ManorHouse") && x >= 1088)
							{
								Game1.warpFarmer("LewisBasement", 4, 4, 2);
								Game1.playSound("stairsdown");
								Game1.screenGlowOnce(Color.Black, hold: true, 1f, 1f);
								return true;
							}
							return false;
						}
						break;
					}
				}
				else if (length != 12)
				{
					if (length == 17 && qualifiedItemId == "(BC)BigStoneChest")
					{
						goto IL_1e1c;
					}
				}
				else if (qualifiedItemId == "(BC)BigChest")
				{
					goto IL_1e1c;
				}
			}
		}
		if (base.Category == -19 && location.terrainFeatures.TryGetValue(vector, out var value6) && value6 is HoeDirt { crop: not null } hoeDirt && (base.QualifiedItemId == "(O)369" || base.QualifiedItemId == "(O)368") && hoeDirt.crop.currentPhase.Value != 0)
		{
			Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13916"));
			return false;
		}
		if (isSapling())
		{
			if (IsWildTreeSapling() || IsFruitTreeSapling())
			{
				if (FruitTree.IsTooCloseToAnotherTree(new Vector2(x / 64, y / 64), location))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13060"));
					return false;
				}
				if (FruitTree.IsGrowthBlocked(new Vector2(x / 64, y / 64), location))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:FruitTree_PlacementWarning", DisplayName));
					return false;
				}
			}
			if (location.terrainFeatures.TryGetValue(vector, out var value7))
			{
				if (!(value7 is HoeDirt { crop: null }))
				{
					return false;
				}
				location.terrainFeatures.Remove(vector);
			}
			string deniedMessage2 = null;
			bool flag = location.doesTileHaveProperty((int)vector.X, (int)vector.Y, "Diggable", "Back") != null;
			string text3 = location.doesTileHaveProperty((int)vector.X, (int)vector.Y, "Type", "Back");
			bool flag2 = location.doesEitherTileOrTileIndexPropertyEqual((int)vector.X, (int)vector.Y, "CanPlantTrees", "Back", "T");
			if ((location is Farm && (flag || text3 == "Grass" || text3 == "Dirt" || flag2) && (!location.IsNoSpawnTile(vector, "Tree") || flag2)) || ((flag || text3 == "Stone") && location.CanPlantTreesHere(base.ItemId, (int)vector.X, (int)vector.Y, out deniedMessage2)))
			{
				location.playSound("dirtyHit");
				DelayedAction.playSoundAfterDelay("coin", 100);
				if (IsTeaSapling())
				{
					location.terrainFeatures.Add(vector, new Bush(vector, 3, location));
					return true;
				}
				FruitTree fruitTree = new FruitTree(base.ItemId)
				{
					GreenHouseTileTree = (location.IsGreenhouse && text3 == "Stone")
				};
				fruitTree.growthRate.Value = Math.Max(1, base.Quality + 1);
				location.terrainFeatures.Add(vector, fruitTree);
				return true;
			}
			if (deniedMessage2 == null)
			{
				deniedMessage2 = Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13068");
			}
			Game1.showRedMessage(deniedMessage2);
			return false;
		}
		if (base.Category == -74 || base.Category == -19)
		{
			if (location.terrainFeatures.TryGetValue(vector, out var value8))
			{
				HoeDirt dirt = value8 as HoeDirt;
				if (dirt != null)
				{
					string text4 = Crop.ResolveSeedId(who.ActiveObject.ItemId, location);
					if (dirt.canPlantThisSeedHere(text4, who.ActiveObject.Category == -19))
					{
						if (dirt.plant(text4, who, who.ActiveObject.Category == -19) && who.IsLocalPlayer)
						{
							if (base.Category == -74)
							{
								foreach (Object value12 in location.Objects.Values)
								{
									if (!value12.IsSprinkler() || value12.heldObject.Value == null || !(value12.heldObject.Value.QualifiedItemId == "(O)913") || !value12.IsInSprinklerRangeBroadphase(vector))
									{
										continue;
									}
									if (!value12.GetSprinklerTiles().Contains(vector))
									{
										continue;
									}
									Object value9 = value12.heldObject.Value.heldObject.Value;
									Chest chest3 = value9 as Chest;
									if (chest3 == null)
									{
										continue;
									}
									IInventory items = chest3.Items;
									if (items.Count <= 0 || items[0] == null || chest3.GetMutex().IsLocked())
									{
										continue;
									}
									chest3.GetMutex().RequestLock(delegate
									{
										if (items.Count > 0 && items[0] != null)
										{
											Item item2 = items[0];
											if (item2.Category == -19 && dirt.plant(item2.ItemId, who, isFertilizer: true))
											{
												items[0] = item2.ConsumeStack(1);
											}
										}
										chest3.GetMutex().ReleaseLock();
									});
									break;
								}
							}
							Game1.haltAfterCheck = false;
							return true;
						}
						return false;
					}
					return false;
				}
			}
			return false;
		}
		if (!performDropDownAction(who))
		{
			Object obj4 = (Object)getOne();
			bool flag3 = false;
			if (obj4.GetType() == typeof(Furniture) && Furniture.GetFurnitureInstance(base.ItemId, new Vector2(x / 64, y / 64)).GetType() != obj4.GetType())
			{
				StorageFurniture storageFurniture = new StorageFurniture(base.ItemId, new Vector2(x / 64, y / 64));
				storageFurniture.currentRotation.Value = (this as Furniture).currentRotation.Value;
				storageFurniture.updateRotation();
				obj4 = storageFurniture;
				flag3 = true;
			}
			obj4.shakeTimer = 50;
			obj4.Location = location;
			obj4.TileLocation = vector;
			obj4.performDropDownAction(who);
			if (IsTextSign())
			{
				obj4.signText.Value = null;
				obj4.showNextIndex.Value = obj4.QualifiedItemId == "(BC)TextSign";
			}
			if (obj4.name.Contains("Seasonal"))
			{
				int num3 = obj4.ParentSheetIndex - obj4.ParentSheetIndex % 4;
				obj4.ParentSheetIndex = num3 + location.GetSeasonIndex();
			}
			if (!(obj4 is Furniture) && location.objects.TryGetValue(vector, out var value10))
			{
				if (value10.QualifiedItemId != base.QualifiedItemId)
				{
					Game1.createItemDebris(value10, vector * 64f, Game1.random.Next(4));
					location.objects[vector] = obj4;
				}
			}
			else if (obj4 is Furniture item)
			{
				if (flag3)
				{
					location.furniture.Add(item);
				}
				else
				{
					location.furniture.Add(this as Furniture);
				}
			}
			else
			{
				location.objects.Add(vector, obj4);
			}
			obj4.initializeLightSource(vector);
		}
		location.playSound("woodyStep");
		return true;
		IL_1e1c:
		if (location.objects.ContainsKey(vector) || location is MineShaft || location is VolcanoDungeon)
		{
			Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
			return false;
		}
		Chest value11 = new Chest(playerChest: true, vector, base.ItemId)
		{
			shakeTimer = 50
		};
		location.objects.Add(vector, value11);
		location.playSound((base.QualifiedItemId == "(BC)BigChest") ? "axe" : "hammer");
		return true;
	}

	/// <inheritdoc />
	protected override void MigrateLegacyItemId()
	{
		if (bigCraftable.Value && !Game1.bigCraftableData.ContainsKey(base.ParentSheetIndex.ToString()))
		{
			if (base.ParentSheetIndex >= 56 && base.ParentSheetIndex <= 61)
			{
				base.ItemId = "56";
				return;
			}
			if (base.ParentSheetIndex >= 101 && base.ParentSheetIndex <= 103)
			{
				SetIdAndSprite(101);
				return;
			}
			if (name.Contains("Seasonal"))
			{
				base.ItemId = (base.ParentSheetIndex - base.ParentSheetIndex % 4).ToString();
				return;
			}
			if (Game1.bigCraftableData.ContainsKey((base.ParentSheetIndex - 1).ToString()))
			{
				base.ItemId = (base.ParentSheetIndex - 1).ToString();
				return;
			}
		}
		base.MigrateLegacyItemId();
	}

	/// <inheritdoc />
	public override bool actionWhenPurchased(string shopId)
	{
		if (base.QualifiedItemId == "(O)434")
		{
			if (!Game1.isFestival())
			{
				Game1.player.mailReceived.Add("CF_Sewer");
			}
			else
			{
				Game1.player.mailReceived.Add("CF_Fair");
			}
			Game1.exitActiveMenu();
			Game1.player.eatObject(this, overrideFullness: true);
		}
		if (base.actionWhenPurchased(shopId))
		{
			return true;
		}
		return isRecipe.Value;
	}

	public virtual bool needsToBeDonated()
	{
		return LibraryMuseum.IsItemSuitableForDonation(base.QualifiedItemId);
	}

	public override string getDescription()
	{
		if (StardewValley.Pokemon.PokemonHeldItemManager.IsHoldable(base.ItemId) ||
		    StardewValley.Pokemon.PokemonHeldItemManager.IsHoldable(base.QualifiedItemId) ||
		    StardewValley.Pokemon.PokemonItemManager.IsPokemonItemId(base.ItemId) ||
		    base.ItemId.StartsWith("Poke_") ||
		    StardewValley.Pokemon.PokemonHeldItemManager.TryGet(base.ItemId, out _))
		{
			string desc = StardewValley.Pokemon.PokemonHeldItemManager.GetDescription(base.ItemId);
			if (!string.IsNullOrEmpty(desc) && desc != "Item de batalha de Pokémon." && desc != "Pokémon battle hold item.")
			{
				return Game1.parseText(desc, Game1.smallFont, getDescriptionWidth());
			}
		}

		if (base.Category == -102 && Game1.player.stats.Get(itemId.Value) != 0 && base.ItemId != "Book_PriceCatalogue" && base.ItemId != "Book_AnimalCatalogue")
		{
			foreach (string contextTag in GetContextTags())
			{
				if (contextTag.StartsWithIgnoreCase("book_xp_"))
				{
					string text = contextTag.Split('_')[2];
					return Game1.parseText(Game1.content.LoadString("Strings\\1_6_Strings:alreadyreadbook", Farmer.getSkillDisplayNameFromIndex(Farmer.getSkillNumberFromName(text)).ToLower()), Game1.smallFont, getDescriptionWidth());
				}
			}
			return Game1.parseText(Game1.content.LoadString("Strings\\1_6_Strings:alreadyreadbook_random"), Game1.smallFont, getDescriptionWidth());
		}
		if (isRecipe.Value)
		{
			if (base.Category == -7)
			{
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13073", loadDisplayName());
			}
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13074", loadDisplayName());
		}
		if (needsToBeDonated())
		{
			return Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13078"), Game1.smallFont, getDescriptionWidth());
		}
		string text2 = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).Description;
		string preservedItemId = GetPreservedItemId();
		if (preservedItemId != null)
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(preservedItemId);
			text2 = string.Format(text2, dataOrErrorItem.DisplayName, dataOrErrorItem.DisplayName.ToLower());
		}
		return Game1.parseText(text2, Game1.smallFont, getDescriptionWidth());
	}

	/// <summary>Auto-generate a default light source ID for this item when placed.</summary>
	/// <param name="equippedBy">The object's tile position.</param>
	public virtual string GenerateLightSourceId(Vector2 position)
	{
		if (Location != null)
		{
			return $"{GetType().Name}_{Location.NameOrUniqueName}_{position.X}_{position.Y}";
		}
		return $"{GetType().Name}_Held_{Game1.random.Next()}";
	}

	/// <inheritdoc />
	public override int sellToStorePrice(long specificPlayerID = -1L)
	{
		if (this is Fence)
		{
			return price.Value;
		}
		if (base.Category == -22)
		{
			return (int)((float)price.Value * (1f + (float)quality.Value * 0.25f) * (((float)(FishingRod.maxTackleUses - uses.Value) + 0f) / (float)FishingRod.maxTackleUses));
		}
		float startPrice = (int)((float)price.Value * (1f + (float)base.Quality * 0.25f));
		startPrice = getPriceAfterMultipliers(startPrice, specificPlayerID);
		if (base.QualifiedItemId == "(O)493")
		{
			startPrice /= 2f;
		}
		if (startPrice > 0f)
		{
			startPrice = Math.Max(1f, startPrice * Game1.MasterPlayer.difficultyModifier);
		}
		return (int)startPrice;
	}

	/// <inheritdoc />
	public override int salePrice(bool ignoreProfitMargins = false)
	{
		if (this is Fence)
		{
			return price.Value;
		}
		if (isRecipe.Value)
		{
			return price.Value * 10;
		}
		switch (base.QualifiedItemId)
		{
		case "(O)388":
			if (Game1.year <= 1)
			{
				return 10;
			}
			return 50;
		case "(O)390":
			if (Game1.year <= 1)
			{
				return 20;
			}
			return 100;
		case "(O)382":
			if (Game1.year <= 1)
			{
				return 120;
			}
			return 250;
		case "(O)378":
			if (Game1.year <= 1)
			{
				return 80;
			}
			return 160;
		case "(O)380":
			if (Game1.year <= 1)
			{
				return 150;
			}
			return 250;
		case "(O)384":
			if (Game1.year <= 1)
			{
				return 350;
			}
			return 750;
		default:
		{
			float num = (int)((float)(price.Value * 2) * (1f + (float)quality.Value * 0.25f));
			if (!ignoreProfitMargins && appliesProfitMargins())
			{
				num = (int)Math.Max(1f, num * Game1.MasterPlayer.difficultyModifier);
			}
			return (int)num;
		}
		}
	}

	/// <inheritdoc />
	public override bool appliesProfitMargins()
	{
		if (category.Value != -74 && !isSapling())
		{
			return base.appliesProfitMargins();
		}
		return true;
	}

	protected virtual float getPriceAfterMultipliers(float startPrice, long specificPlayerID = -1L)
	{
		string text = name.ToLower();
		bool flag = text.Contains("mayonnaise") || text.Contains("cheese") || text.Contains("cloth") || text.Contains("wool");
		float num = 1f;
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (Game1.player.useSeparateWallets)
			{
				if (specificPlayerID == -1)
				{
					if (allFarmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID || !allFarmer.isActive())
					{
						continue;
					}
				}
				else if (allFarmer.UniqueMultiplayerID != specificPlayerID)
				{
					continue;
				}
			}
			else if (!allFarmer.isActive())
			{
				continue;
			}
			float num2 = 1f;
			if (allFarmer.professions.Contains(0) && (flag || base.Category == -5 || base.Category == -6 || base.Category == -18))
			{
				num2 *= 1.2f;
			}
			if (allFarmer.professions.Contains(1) && (base.Category == -75 || base.Category == -80 || (base.Category == -79 && !isSpawnedObject.Value)))
			{
				num2 *= 1.1f;
			}
			if (allFarmer.professions.Contains(4) && base.Category == -26)
			{
				num2 *= 1.4f;
			}
			if (allFarmer.professions.Contains(6) && (base.Category == -4 || (preserve != null && preserve.Value.HasValue && preserve.Value == PreserveType.SmokedFish)))
			{
				num2 *= (allFarmer.professions.Contains(8) ? 1.5f : 1.25f);
			}
			if (allFarmer.professions.Contains(15) && base.Category == -27)
			{
				num2 *= 1.25f;
			}
			if (allFarmer.professions.Contains(20) && IsBar())
			{
				num2 *= 1.5f;
			}
			if (allFarmer.professions.Contains(23) && (base.Category == -2 || base.Category == -12))
			{
				num2 *= 1.3f;
			}
			if (allFarmer.eventsSeen.Contains("2120303") && (base.QualifiedItemId == "(O)296" || base.QualifiedItemId == "(O)410"))
			{
				num2 *= 3f;
			}
			if (allFarmer.eventsSeen.Contains("3910979") && base.QualifiedItemId == "(O)399")
			{
				num2 *= 5f;
			}
			if (allFarmer.stats.Get("Book_Artifact") != 0 && Type != null && Type.Equals("Arch"))
			{
				num2 *= 3f;
			}
			num = Math.Max(num, num2);
		}
		return startPrice * num;
	}

	/// <inheritdoc />
	public override bool ForEachItem(ForEachItemDelegate handler, GetForEachItemPathDelegate getPath)
	{
		if (base.ForEachItem(handler, getPath))
		{
			return ForEachItemHelper.ApplyToField(heldObject, handler, getPath);
		}
		return false;
	}
}
