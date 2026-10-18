using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Characters;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley.Locations;

public class FarmHouse : DecoratableLocation
{
	[XmlElement("fridge")]
	public readonly NetRef<Chest> fridge = new NetRef<Chest>(new Chest(playerChest: true));

	[XmlIgnore]
	public readonly NetInt synchronizedDisplayedLevel = new NetInt(-1);

	/// <summary>The tile position containing the map fridge, or <see cref="P:Microsoft.Xna.Framework.Point.Zero" /> if not found.</summary>
	public Point fridgePosition = Point.Zero;

	[XmlIgnore]
	public Point spouseRoomSpot = Point.Zero;

	private string lastSpouseRoom;

	[XmlIgnore]
	private LocalizedContentManager mapLoader;

	public List<Warp> cellarWarps;

	[XmlElement("cribStyle")]
	public readonly NetInt cribStyle = new NetInt(1)
	{
		InterpolationEnabled = false
	};

	[XmlIgnore]
	public int previousUpgradeLevel = -1;

	private int currentlyDisplayedUpgradeLevel;

	private bool displayingSpouseRoom;

	private Color nightLightingColor = new Color(180, 180, 0);

	private Color rainLightingColor = new Color(90, 90, 0);

	/// <summary>The player who owns this home.</summary>
	[XmlIgnore]
	public virtual Farmer owner => Game1.MasterPlayer;

	/// <summary>Whether the home has an assigned player, regardless of whether they've finished creating their character..</summary>
	/// <remarks>See also <see cref="P:StardewValley.Locations.FarmHouse.IsOwnerActivated" />.</remarks>
	[XmlIgnore]
	[MemberNotNullWhen(true, "owner")]
	public virtual bool HasOwner
	{
		[MemberNotNullWhen(true, "owner")]
		get
		{
			return owner != null;
		}
	}

	/// <summary>The unique ID of the player who owns this home, if any.</summary>
	public virtual long OwnerId => owner?.UniqueMultiplayerID ?? 0;

	/// <summary>Whether the home has an assigned player and they've finished creating their character.</summary>
	/// <remarks>See also <see cref="P:StardewValley.Locations.FarmHouse.HasOwner" />.</remarks>
	[MemberNotNullWhen(true, "owner")]
	public bool IsOwnerActivated
	{
		[MemberNotNullWhen(true, "owner")]
		get
		{
			return owner?.isActive() ?? false;
		}
	}

	/// <summary>Whether the home is owned by the current player.</summary>
	[MemberNotNullWhen(true, "owner")]
	public bool IsOwnedByCurrentPlayer
	{
		[MemberNotNullWhen(true, "owner")]
		get
		{
			return owner?.UniqueMultiplayerID == Game1.player.UniqueMultiplayerID;
		}
	}

	[XmlIgnore]
	public virtual int upgradeLevel
	{
		get
		{
			return owner?.HouseUpgradeLevel ?? 0;
		}
		set
		{
			if (HasOwner)
			{
				owner.houseUpgradeLevel.Value = value;
			}
		}
	}

	public FarmHouse()
	{
		fridge.Value.Location = this;
	}

	public FarmHouse(string m, string name)
		: base(m, name)
	{
		fridge.Value.Location = this;
		ReadWallpaperAndFloorTileData();
		Farm farm = Game1.getFarm();
		AddStarterGiftBox(farm);
		AddStarterFurniture(farm);
		SetStarterFlooring(farm);
		SetStarterWallpaper(farm);
	}

	/// <summary>Place the starter gift box when the farmhouse is first created.</summary>
	/// <param name="farm">The farm instance to which a farmhouse is being added.</param>
	private void AddStarterGiftBox(Farm farm)
	{
		Chest chest = new Chest(null, Vector2.Zero, giftbox: true, 0, giftboxIsStarterGift: true);
		string[] mapPropertySplitBySpaces = farm.GetMapPropertySplitBySpaces("FarmHouseStarterGift");
		for (int i = 0; i < mapPropertySplitBySpaces.Length; i += 2)
		{
			if (!ArgUtility.TryGet(mapPropertySplitBySpaces, i, out var value, out var error, allowBlank: false, "string giftId") || !ArgUtility.TryGetOptionalInt(mapPropertySplitBySpaces, i + 1, out var value2, out error, 0, "int count"))
			{
				farm.LogMapPropertyError("FarmHouseStarterGift", mapPropertySplitBySpaces, error);
			}
			else
			{
				chest.Items.Add(ItemRegistry.Create(value, value2));
			}
		}
		if (!chest.Items.Any())
		{
			Item item = ItemRegistry.Create("(O)472", 15);
			chest.Items.Add(item);
		}
		if (!farm.TryGetMapPropertyAs("FarmHouseStarterSeedsPosition", out Vector2 parsed, required: false))
		{
			switch (Game1.whichFarm)
			{
			case 1:
			case 2:
			case 4:
				parsed = new Vector2(4f, 7f);
				break;
			case 3:
				parsed = new Vector2(2f, 9f);
				break;
			case 6:
				parsed = new Vector2(8f, 6f);
				break;
			default:
				parsed = new Vector2(3f, 7f);
				break;
			}
		}
		objects.Add(parsed, chest);
	}

	/// <summary>Place the starter furniture when the farmhouse is first created.</summary>
	/// <param name="farm">The farm instance to which a farmhouse is being added.</param>
	private void AddStarterFurniture(Farm farm)
	{
		base.furniture.Add(new BedFurniture(BedFurniture.DEFAULT_BED_INDEX, new Vector2(9f, 8f)));
		string[] mapPropertySplitBySpaces = farm.GetMapPropertySplitBySpaces("FarmHouseFurniture");
		if (mapPropertySplitBySpaces.Any())
		{
			for (int i = 0; i < mapPropertySplitBySpaces.Length; i += 4)
			{
				if (!ArgUtility.TryGetInt(mapPropertySplitBySpaces, i, out var value, out var error, "int index") || !ArgUtility.TryGetVector2(mapPropertySplitBySpaces, i + 1, out var value2, out error, integerOnly: false, "Vector2 tile") || !ArgUtility.TryGetInt(mapPropertySplitBySpaces, i + 3, out var value3, out error, "int rotations"))
				{
					farm.LogMapPropertyError("FarmHouseFurniture", mapPropertySplitBySpaces, error);
					continue;
				}
				Furniture furniture = ItemRegistry.Create<Furniture>("(F)" + value);
				furniture.InitializeAtTile(value2);
				furniture.isOn.Value = true;
				for (int j = 0; j < value3; j++)
				{
					furniture.rotate();
				}
				Furniture furnitureAt = GetFurnitureAt(value2);
				if (furnitureAt != null)
				{
					furnitureAt.heldObject.Value = furniture;
				}
				else
				{
					base.furniture.Add(furniture);
				}
			}
			return;
		}
		switch (Game1.whichFarm)
		{
		case 0:
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1120").SetPlacement(5, 4).SetHeldObject(ItemRegistry.Create<Furniture>("(F)1364")));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1376").SetPlacement(1, 10));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)0").SetPlacement(4, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1466").SetPlacement(1, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1614").SetPlacement(3, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1618").SetPlacement(6, 8));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1602").SetPlacement(5, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1792").SetPlacement(getFireplacePoint()));
			break;
		case 1:
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1122").SetPlacement(1, 6).SetHeldObject(ItemRegistry.Create<Furniture>("(F)1367")));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)3").SetPlacement(1, 5));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1680").SetPlacement(5, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1673").SetPlacement(1, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1673").SetPlacement(3, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1676").SetPlacement(5, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1737").SetPlacement(6, 8));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1742").SetPlacement(5, 5));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1675").SetPlacement(10, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1792").SetPlacement(getFireplacePoint()));
			objects.Add(new Vector2(4f, 4f), ItemRegistry.Create<Object>("(BC)FishSmoker"));
			break;
		case 2:
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1134").SetPlacement(1, 7).SetHeldObject(ItemRegistry.Create<Furniture>("(F)1748")));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)3").SetPlacement(1, 6));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1680").SetPlacement(6, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1296").SetPlacement(1, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1682").SetPlacement(3, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1777").SetPlacement(6, 5));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1745").SetPlacement(6, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1747").SetPlacement(5, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1296").SetPlacement(10, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1792").SetPlacement(getFireplacePoint()));
			break;
		case 3:
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1218").SetPlacement(1, 6).SetHeldObject(ItemRegistry.Create<Furniture>("(F)1368")));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1755").SetPlacement(1, 5));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1755").SetPlacement(3, 6, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1680").SetPlacement(5, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1751").SetPlacement(5, 10));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1749").SetPlacement(3, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1753").SetPlacement(5, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1742").SetPlacement(5, 5));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1794").SetPlacement(getFireplacePoint()));
			break;
		case 4:
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1680").SetPlacement(1, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1628").SetPlacement(1, 5));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1393").SetPlacement(3, 4).SetHeldObject(ItemRegistry.Create<Furniture>("(F)1369")));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1678").SetPlacement(10, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1812").SetPlacement(3, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1630").SetPlacement(1, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1811").SetPlacement(6, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1389").SetPlacement(10, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1758").SetPlacement(1, 10));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1794").SetPlacement(getFireplacePoint()));
			break;
		case 5:
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1466").SetPlacement(1, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1614").SetPlacement(3, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1614").SetPlacement(6, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1601").SetPlacement(10, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)202").SetPlacement(3, 4, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1124").SetPlacement(4, 4, 1).SetHeldObject(ItemRegistry.Create<Furniture>("(F)1379")));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)202").SetPlacement(6, 4, 3));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1378").SetPlacement(10, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1377").SetPlacement(1, 9));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1445").SetPlacement(1, 10));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1618").SetPlacement(2, 9));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1792").SetPlacement(getFireplacePoint()));
			break;
		case 6:
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1680").SetPlacement(4, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1614").SetPlacement(7, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1294").SetPlacement(3, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1283").SetPlacement(1, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1614").SetPlacement(8, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)202").SetPlacement(7, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1294").SetPlacement(10, 4));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)6").SetPlacement(2, 6, 1));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)6").SetPlacement(5, 7, 3));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1124").SetPlacement(3, 6).SetHeldObject(ItemRegistry.Create<Furniture>("(F)1362")));
			base.furniture.Add(ItemRegistry.Create<Furniture>("(F)1228").SetPlacement(2, 9));
			break;
		}
	}

	/// <summary>Gets the initial flooring type for a farmhouse being created, if any.</summary>
	/// <param name="farm">The farm instance to which a farmhouse is being added.</param>
	public static string GetStarterFlooring(Farm farm)
	{
		string text = farm?.getMapProperty("FarmHouseFlooring");
		if (text != null)
		{
			return text;
		}
		return Game1.whichFarm switch
		{
			1 => "1", 
			2 => "34", 
			3 => "18", 
			4 => "4", 
			5 => "5", 
			6 => "35", 
			_ => null, 
		};
	}

	/// <summary>Gets the initial wallpaper type for a farmhouse being created, if any.</summary>
	/// <param name="farm">The farm instance to which a farmhouse is being added.</param>
	public static string GetStarterWallpaper(Farm farm)
	{
		string text = farm?.getMapProperty("FarmHouseWallpaper");
		if (text != null)
		{
			return text;
		}
		return Game1.whichFarm switch
		{
			1 => "11", 
			2 => "92", 
			3 => "12", 
			4 => "95", 
			5 => "65", 
			6 => "106", 
			_ => null, 
		};
	}

	/// <summary>Set the initial flooring when the farmhouse is first created, if any.</summary>
	/// <param name="farm">The farm instance to which a farmhouse is being added.</param>
	/// <param name="styleToOverride">Unused.</param>
	private void SetStarterFlooring(Farm farm, string styleToOverride = null)
	{
		string starterFlooring = GetStarterFlooring(farm);
		if (starterFlooring != null)
		{
			SetFloor(starterFlooring, null);
		}
	}

	/// <summary>Set the initial wallpaper when the farmhouse is first created, if any.</summary>
	/// <param name="farm">The farm instance to which a farmhouse is being added.</param>
	/// <param name="styleToOverride">Unused.</param>
	private void SetStarterWallpaper(Farm farm, string styleToOverride = null)
	{
		string starterWallpaper = GetStarterWallpaper(farm);
		if (starterWallpaper != null)
		{
			SetWallpaper(starterWallpaper, null);
		}
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(fridge, "fridge").AddField(cribStyle, "cribStyle").AddField(synchronizedDisplayedLevel, "synchronizedDisplayedLevel");
		cribStyle.fieldChangeVisibleEvent += delegate
		{
			if (map != null)
			{
				if (_appliedMapOverrides != null && _appliedMapOverrides.Contains("crib"))
				{
					_appliedMapOverrides.Remove("crib");
				}
				UpdateChildRoom();
				ReadWallpaperAndFloorTileData();
				setWallpapers();
				setFloors();
			}
		};
		fridge.fieldChangeEvent += delegate(NetRef<Chest> field, Chest oldValue, Chest newValue)
		{
			newValue.Location = this;
		};
	}

	public List<Child> getChildren()
	{
		return characters.OfType<Child>().ToList();
	}

	public int getChildrenCount()
	{
		int num = 0;
		foreach (NPC character in characters)
		{
			if (character is Child)
			{
				num++;
			}
		}
		return num;
	}

	public override bool isCollidingPosition(Microsoft.Xna.Framework.Rectangle position, xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character, bool pathfinding, bool projectile = false, bool ignoreCharacterRequirement = false, bool skipCollisionEffects = false)
	{
		return base.isCollidingPosition(position, viewport, isFarmer, damagesFarmer, glider, character, pathfinding);
	}

	public override void performTenMinuteUpdate(int timeOfDay)
	{
		base.performTenMinuteUpdate(timeOfDay);
		foreach (NPC character in characters)
		{
			if (character.isMarried())
			{
				if (character.getSpouse() == Game1.player)
				{
					character.checkForMarriageDialogue(timeOfDay, this);
				}
				if (Game1.IsMasterGame && Game1.timeOfDay >= 2200 && Game1.IsMasterGame && character.TilePoint != getSpouseBedSpot(character.Name) && (timeOfDay == 2200 || (character.controller == null && timeOfDay % 100 % 30 == 0)))
				{
					Point spouseBedSpot = getSpouseBedSpot(character.Name);
					character.controller = null;
					PathFindController.endBehavior endBehaviorFunction = null;
					bool flag = GetSpouseBed() != null;
					if (flag)
					{
						endBehaviorFunction = spouseSleepEndFunction;
					}
					character.controller = new PathFindController(character, this, spouseBedSpot, 0, endBehaviorFunction);
					if (character.controller.pathToEndPoint == null || !isTileOnMap(character.controller.pathToEndPoint.Last()))
					{
						character.controller = null;
					}
					else if (flag)
					{
						foreach (Furniture item in furniture)
						{
							if (item is BedFurniture bedFurniture && bedFurniture.GetBoundingBox().Intersects(new Microsoft.Xna.Framework.Rectangle(spouseBedSpot.X * 64, spouseBedSpot.Y * 64, 64, 64)))
							{
								bedFurniture.ReserveForNPC();
								break;
							}
						}
					}
				}
			}
			if (character is Child child)
			{
				child.tenMinuteUpdate();
			}
		}
	}

	public static void spouseSleepEndFunction(Character c, GameLocation location)
	{
		if (!(c is NPC nPC))
		{
			return;
		}
		if (DataLoader.AnimationDescriptions(Game1.content).ContainsKey(nPC.name.Value.ToLower() + "_sleep"))
		{
			nPC.playSleepingAnimation();
		}
		Microsoft.Xna.Framework.Rectangle boundingBox = nPC.GetBoundingBox();
		foreach (Furniture item in location.furniture)
		{
			if (item is BedFurniture bedFurniture && bedFurniture.GetBoundingBox().Intersects(boundingBox))
			{
				bedFurniture.ReserveForNPC();
				break;
			}
		}
		if (Game1.random.NextDouble() < 0.1)
		{
			if (Game1.random.NextDouble() < 0.8)
			{
				nPC.showTextAboveHead(Game1.content.LoadString("Strings\\1_6_Strings:Spouse_Goodnight0", nPC.getTermOfSpousalEndearment(Game1.random.NextDouble() < 0.1)));
			}
			else
			{
				nPC.showTextAboveHead(Game1.content.LoadString("Strings\\1_6_Strings:Spouse_Goodnight1"));
			}
		}
	}

	public virtual Point getFrontDoorSpot()
	{
		foreach (Warp warp in warps)
		{
			if (warp.TargetName == "Farm")
			{
				if (this is Cabin)
				{
					return new Point(warp.TargetX, warp.TargetY);
				}
				if (warp.TargetX == 64 && warp.TargetY == 15)
				{
					return Game1.getFarm().GetMainFarmHouseEntry();
				}
				return new Point(warp.TargetX, warp.TargetY);
			}
		}
		return Game1.getFarm().GetMainFarmHouseEntry();
	}

	public virtual Point getPorchStandingSpot()
	{
		Point mainFarmHouseEntry = Game1.getFarm().GetMainFarmHouseEntry();
		mainFarmHouseEntry.X += 2;
		return mainFarmHouseEntry;
	}

	public Point getKitchenStandingSpot()
	{
		if (TryGetMapPropertyAs("KitchenStandingLocation", out Point parsed, required: false))
		{
			return parsed;
		}
		switch (upgradeLevel)
		{
		case 1:
			return new Point(4, 5);
		case 2:
		case 3:
			return new Point(22, 24);
		default:
			return new Point(-1000, -1000);
		}
	}

	public virtual BedFurniture GetSpouseBed()
	{
		if (HasOwner)
		{
			if (owner.getSpouse()?.Name == "Krobus")
			{
				return null;
			}
			if (owner.hasCurrentOrPendingRoommate() && GetBed(BedFurniture.BedType.Single) != null)
			{
				return GetBed(BedFurniture.BedType.Single);
			}
		}
		return GetBed(BedFurniture.BedType.Double);
	}

	public Point getSpouseBedSpot(string spouseName)
	{
		if (spouseName == "Krobus")
		{
			NPC characterFromName = Game1.getCharacterFromName(name.Value);
			if (characterFromName != null && characterFromName.isRoommate())
			{
				goto IL_0035;
			}
		}
		if (GetSpouseBed() != null)
		{
			BedFurniture spouseBed = GetSpouseBed();
			Point bedSpot = GetSpouseBed().GetBedSpot();
			if (spouseBed.bedType == BedFurniture.BedType.Double)
			{
				bedSpot.X++;
			}
			return bedSpot;
		}
		goto IL_0035;
		IL_0035:
		return GetSpouseRoomSpot();
	}

	public Point GetSpouseRoomSpot()
	{
		if (upgradeLevel == 0)
		{
			return new Point(-1000, -1000);
		}
		return spouseRoomSpot;
	}

	public BedFurniture GetBed(BedFurniture.BedType bed_type = BedFurniture.BedType.Any, int index = 0)
	{
		foreach (Furniture item in furniture)
		{
			if (item is BedFurniture bedFurniture && (bed_type == BedFurniture.BedType.Any || bedFurniture.bedType == bed_type))
			{
				if (index == 0)
				{
					return bedFurniture;
				}
				index--;
			}
		}
		return null;
	}

	public Point GetPlayerBedSpot()
	{
		return GetPlayerBed()?.GetBedSpot() ?? getEntryLocation();
	}

	public BedFurniture GetPlayerBed()
	{
		if (upgradeLevel == 0)
		{
			return GetBed(BedFurniture.BedType.Single);
		}
		return GetBed(BedFurniture.BedType.Double);
	}

	public Point getBedSpot(BedFurniture.BedType bed_type = BedFurniture.BedType.Any)
	{
		return GetBed(bed_type)?.GetBedSpot() ?? new Point(-1000, -1000);
	}

	public Point getEntryLocation()
	{
		if (TryGetMapPropertyAs("EntryLocation", out Point parsed, required: false))
		{
			return parsed;
		}
		switch (upgradeLevel)
		{
		case 0:
			return new Point(3, 11);
		case 1:
			return new Point(9, 11);
		case 2:
		case 3:
			return new Point(27, 30);
		default:
			return new Point(-1000, -1000);
		}
	}

	public BedFurniture GetChildBed(int index)
	{
		return GetBed(BedFurniture.BedType.Child, index);
	}

	public Point GetChildBedSpot(int index)
	{
		return GetChildBed(index)?.GetBedSpot() ?? Point.Zero;
	}

	public override bool isTilePlaceable(Vector2 v, bool itemIsPassable = false)
	{
		if (isTileOnMap(v) && getTileIndexAt((int)v.X, (int)v.Y, "Back", "indoor") == 0)
		{
			return false;
		}
		return base.isTilePlaceable(v, itemIsPassable);
	}

	public Point getRandomOpenPointInHouse(Random r, int buffer = 0, int tries = 30)
	{
		for (int i = 0; i < tries; i++)
		{
			Point result = new Point(r.Next(map.Layers[0].LayerWidth), r.Next(map.Layers[0].LayerHeight));
			Microsoft.Xna.Framework.Rectangle rect = new Microsoft.Xna.Framework.Rectangle(result.X - buffer, result.Y - buffer, 1 + buffer * 2, 1 + buffer * 2);
			bool flag = false;
			foreach (Point point in rect.GetPoints())
			{
				int x = point.X;
				int y = point.Y;
				flag = !hasTileAt(x, y, "Back") || !CanItemBePlacedHere(new Vector2(x, y)) || isTileOnWall(x, y);
				if (getTileIndexAt(x, y, "Back", "indoor") == 0)
				{
					flag = true;
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				return result;
			}
		}
		return Point.Zero;
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		if (getTileIndexAt(tileLocation, "Buildings", "untitled tile sheet") == 173)
		{
			fridge.Value.fridge.Value = true;
			fridge.Value.checkForAction(who);
			return true;
		}
		if (getTileIndexAt(tileLocation, "Buildings", "indoor") == 2173)
		{
			if (Game1.player.eventsSeen.Contains("463391") && Game1.player.spouse == "Emily" && getTemporarySpriteByID(5858585) is EmilysParrot emilysParrot)
			{
				emilysParrot.doAction();
			}
			return true;
		}
		return base.checkAction(tileLocation, viewport, who);
	}

	public override void updateEvenIfFarmerIsntHere(GameTime time, bool ignoreWasUpdatedFlush = false)
	{
		base.updateEvenIfFarmerIsntHere(time, ignoreWasUpdatedFlush);
		if (!HasOwner || !Game1.IsMasterGame)
		{
			return;
		}
		foreach (NPC character in characters)
		{
			if (character.getSpouse()?.UniqueMultiplayerID != OwnerId || Game1.timeOfDay >= 1500 || !(Game1.random.NextDouble() < 0.0006) || character.controller != null || character.Schedule != null || !(character.TilePoint != getSpouseBedSpot(Game1.player.spouse)) || base.furniture.Count <= 0)
			{
				continue;
			}
			Furniture furniture = base.furniture[Game1.random.Next(base.furniture.Count)];
			Microsoft.Xna.Framework.Rectangle value = furniture.boundingBox.Value;
			Vector2 tile = new Vector2(value.X / 64, value.Y / 64);
			if (furniture.furniture_type.Value == 15 || furniture.furniture_type.Value == 12)
			{
				continue;
			}
			int i = 0;
			int finalFacingDirection = -3;
			for (; i < 3; i++)
			{
				int num = Game1.random.Next(-1, 2);
				int num2 = Game1.random.Next(-1, 2);
				tile.X += num;
				if (num == 0)
				{
					tile.Y += num2;
				}
				switch (num)
				{
				case -1:
					finalFacingDirection = 1;
					break;
				case 1:
					finalFacingDirection = 3;
					break;
				default:
					switch (num2)
					{
					case -1:
						finalFacingDirection = 2;
						break;
					case 1:
						finalFacingDirection = 0;
						break;
					}
					break;
				}
				if (CanItemBePlacedHere(tile))
				{
					break;
				}
			}
			if (i < 3)
			{
				character.controller = new PathFindController(character, this, new Point((int)tile.X, (int)tile.Y), finalFacingDirection, clearMarriageDialogues: false);
			}
		}
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		if (wasUpdated)
		{
			return;
		}
		base.UpdateWhenCurrentLocation(time);
		fridge.Value.updateWhenCurrentLocation(time);
		if (!Game1.player.isMarriedOrRoommates() || Game1.player.spouse == null)
		{
			return;
		}
		NPC characterFromName = getCharacterFromName(Game1.player.spouse);
		if (characterFromName == null || characterFromName.isEmoting)
		{
			return;
		}
		Vector2 tile = characterFromName.Tile;
		Vector2[] adjacentTilesOffsets = Character.AdjacentTilesOffsets;
		foreach (Vector2 vector in adjacentTilesOffsets)
		{
			Vector2 vector2 = tile + vector;
			if (isCharacterAtTile(vector2) is Monster monster)
			{
				Microsoft.Xna.Framework.Rectangle boundingBox = monster.GetBoundingBox();
				Point center = boundingBox.Center;
				characterFromName.faceGeneralDirection(vector2 * new Vector2(64f, 64f));
				Game1.showSwordswipeAnimation(characterFromName.FacingDirection, characterFromName.Position, 60f, flip: false);
				localSound("swordswipe");
				characterFromName.shake(500);
				characterFromName.showTextAboveHead(Game1.content.LoadString("Strings\\Locations:FarmHouse_SpouseAttacked" + (Game1.random.Next(12) + 1)));
				monster.takeDamage(50, (int)Utility.getAwayFromPositionTrajectory(boundingBox, characterFromName.Position).X, (int)Utility.getAwayFromPositionTrajectory(boundingBox, characterFromName.Position).Y, isBomb: false, 1.0, Game1.player);
				if (monster.Health <= 0)
				{
					debris.Add(new Debris(monster.Sprite.textureName.Value, Game1.random.Next(6, 16), Utility.PointToVector2(center)));
					monsterDrop(monster, center.X, center.Y, owner);
					characters.Remove(monster);
					Game1.stats.MonstersKilled++;
					Game1.player.changeFriendship(-10, characterFromName);
				}
				else
				{
					monster.shedChunks(4);
				}
				characterFromName.CurrentDialogue.Clear();
				characterFromName.CurrentDialogue.Push(characterFromName.TryGetDialogue("Spouse_MonstersInHouse") ?? new Dialogue(characterFromName, "Data\\ExtraDialogue:Spouse_MonstersInHouse"));
			}
		}
	}

	public Point getFireplacePoint()
	{
		switch (upgradeLevel)
		{
		case 0:
			return new Point(8, 4);
		case 1:
			return new Point(26, 4);
		case 2:
		case 3:
			return new Point(17, 23);
		default:
			return new Point(-50, -50);
		}
	}

	/// <summary>Get whether the player who owns this home is married to or roommates with an NPC.</summary>
	public bool HasNpcSpouseOrRoommate()
	{
		if (owner?.spouse != null)
		{
			return owner.isMarriedOrRoommates();
		}
		return false;
	}

	/// <summary>Get whether the player who owns this home is married to or roommates with the given NPC.</summary>
	/// <param name="spouseName">The NPC name.</param>
	public bool HasNpcSpouseOrRoommate(string spouseName)
	{
		if (spouseName != null && owner?.spouse == spouseName)
		{
			return owner.isMarriedOrRoommates();
		}
		return false;
	}

	public virtual void showSpouseRoom()
	{
		bool flag = HasNpcSpouseOrRoommate();
		bool num = displayingSpouseRoom;
		displayingSpouseRoom = flag;
		updateMap();
		if (num && !displayingSpouseRoom)
		{
			Point spouseRoomCorner = GetSpouseRoomCorner();
			Microsoft.Xna.Framework.Rectangle rectangle = CharacterSpouseRoomData.DefaultMapSourceRect;
			if (NPC.TryGetData(owner.spouse, out var data))
			{
				rectangle = data.SpouseRoom?.MapSourceRect ?? rectangle;
			}
			Microsoft.Xna.Framework.Rectangle rectangle2 = new Microsoft.Xna.Framework.Rectangle(spouseRoomCorner.X, spouseRoomCorner.Y, rectangle.Width, rectangle.Height);
			rectangle2.X--;
			List<Item> list = new List<Item>();
			Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(rectangle2.X * 64, rectangle2.Y * 64, rectangle2.Width * 64, rectangle2.Height * 64);
			foreach (Furniture item in new List<Furniture>(furniture))
			{
				if (item.GetBoundingBox().Intersects(value))
				{
					if (item is StorageFurniture storageFurniture)
					{
						list.AddRange(storageFurniture.heldItems);
						storageFurniture.heldItems.Clear();
					}
					if (item.heldObject.Value != null)
					{
						list.Add(item.heldObject.Value);
						item.heldObject.Value = null;
					}
					list.Add(item);
					furniture.Remove(item);
				}
			}
			for (int i = rectangle2.X; i <= rectangle2.Right; i++)
			{
				for (int j = rectangle2.Y; j <= rectangle2.Bottom; j++)
				{
					Object obj = getObjectAtTile(i, j);
					if (obj == null || obj is Furniture)
					{
						continue;
					}
					obj.performRemoveAction();
					if (!(obj is Fence fence))
					{
						if (!(obj is IndoorPot indoorPot))
						{
							if (obj is Chest chest)
							{
								list.AddRange(chest.Items);
								chest.Items.Clear();
							}
						}
						else if (indoorPot.hoeDirt.Value?.crop != null)
						{
							indoorPot.hoeDirt.Value.destroyCrop(showAnimation: false);
						}
					}
					else
					{
						obj = new Object(fence.ItemId, 1);
					}
					obj.heldObject.Value = null;
					obj.minutesUntilReady.Value = -1;
					obj.readyForHarvest.Value = false;
					list.Add(obj);
					objects.Remove(new Vector2(i, j));
				}
			}
			if (upgradeLevel >= 2)
			{
				Utility.createOverflowChest(this, new Vector2(39f, 32f), list);
			}
			else
			{
				Utility.createOverflowChest(this, new Vector2(21f, 10f), list);
			}
		}
		loadObjects();
		if (upgradeLevel == 3)
		{
			AddCellarTiles();
			createCellarWarps();
			Game1.player.craftingRecipes.TryAdd("Cask", 0);
		}
		if (flag)
		{
			loadSpouseRoom();
		}
		lastSpouseRoom = owner?.spouse;
	}

	public virtual void AddCellarTiles()
	{
		if (_appliedMapOverrides.Contains("cellar"))
		{
			_appliedMapOverrides.Remove("cellar");
		}
		ApplyMapOverride("FarmHouse_Cellar", "cellar");
	}

	/// <summary>Get the cellar location linked to this cabin, or <c>null</c> if there is none.</summary>
	public Cellar GetCellar()
	{
		string cellarName = GetCellarName();
		if (cellarName == null)
		{
			return null;
		}
		return Game1.RequireLocation<Cellar>(cellarName);
	}

	/// <summary>Get the name of the cellar location linked to this cabin, or <c>null</c> if there is none.</summary>
	public string GetCellarName()
	{
		int num = -1;
		if (HasOwner)
		{
			foreach (int key in Game1.player.team.cellarAssignments.Keys)
			{
				if (Game1.player.team.cellarAssignments[key] == OwnerId)
				{
					num = key;
				}
			}
		}
		switch (num)
		{
		case 0:
		case 1:
			return "Cellar";
		case -1:
			return null;
		default:
			return "Cellar" + num;
		}
	}

	protected override void resetSharedState()
	{
		base.resetSharedState();
		if (HasOwner)
		{
			if (Game1.timeOfDay >= 2200 && owner.spouse != null && getCharacterFromName(owner.spouse) != null && !owner.isEngaged())
			{
				Game1.player.team.requestSpouseSleepEvent.Fire(owner.UniqueMultiplayerID);
			}
			if (Game1.timeOfDay >= 2000 && IsOwnedByCurrentPlayer && Game1.getFarm().farmers.Count <= 1)
			{
				Game1.player.team.requestPetWarpHomeEvent.Fire(owner.UniqueMultiplayerID);
			}
		}
		if (!Game1.IsMasterGame)
		{
			return;
		}
		Farm farm = Game1.getFarm();
		for (int num = characters.Count - 1; num >= 0; num--)
		{
			if (characters[num] is Pet { TilePoint: var tilePoint } pet)
			{
				Microsoft.Xna.Framework.Rectangle boundingBox = pet.GetBoundingBox();
				if (!isTileOnMap(tilePoint.X, tilePoint.Y) || hasTileAt(boundingBox.Left / 64, tilePoint.Y, "Buildings") || hasTileAt(boundingBox.Right / 64, tilePoint.Y, "Buildings"))
				{
					pet.WarpToPetBowl();
					break;
				}
			}
		}
		for (int num2 = characters.Count - 1; num2 >= 0; num2--)
		{
			for (int num3 = num2 - 1; num3 >= 0; num3--)
			{
				if (num2 < characters.Count && num3 < characters.Count && (characters[num3].Equals(characters[num2]) || (characters[num3].Name.Equals(characters[num2].Name) && characters[num3].IsVillager && characters[num2].IsVillager)) && num3 != num2)
				{
					characters.RemoveAt(num3);
				}
			}
			for (int num4 = farm.characters.Count - 1; num4 >= 0; num4--)
			{
				if (num2 < characters.Count && num4 < characters.Count && farm.characters[num4].Equals(characters[num2]))
				{
					farm.characters.RemoveAt(num4);
				}
			}
		}
	}

	public void UpdateForRenovation()
	{
		updateFarmLayout();
		setWallpapers();
		setFloors();
	}

	public void updateFarmLayout()
	{
		if (currentlyDisplayedUpgradeLevel != upgradeLevel)
		{
			setMapForUpgradeLevel(upgradeLevel);
		}
		_ApplyRenovations();
		if (displayingSpouseRoom != HasNpcSpouseOrRoommate() || lastSpouseRoom != owner?.spouse)
		{
			showSpouseRoom();
		}
		UpdateChildRoom();
		ReadWallpaperAndFloorTileData();
	}

	protected virtual void _ApplyRenovations()
	{
		bool hasOwner = HasOwner;
		if (upgradeLevel >= 2)
		{
			if (_appliedMapOverrides.Contains("bedroom_open"))
			{
				_appliedMapOverrides.Remove("bedroom_open");
			}
			if (hasOwner && owner.mailReceived.Contains("renovation_bedroom_open"))
			{
				ApplyMapOverride("FarmHouse_Bedroom_Open", "bedroom_open");
			}
			else
			{
				ApplyMapOverride("FarmHouse_Bedroom_Normal", "bedroom_open");
			}
			if (_appliedMapOverrides.Contains("southernroom_open"))
			{
				_appliedMapOverrides.Remove("southernroom_open");
			}
			if (hasOwner && owner.mailReceived.Contains("renovation_southern_open"))
			{
				ApplyMapOverride("FarmHouse_SouthernRoom_Add", "southernroom_open");
			}
			else
			{
				ApplyMapOverride("FarmHouse_SouthernRoom_Remove", "southernroom_open");
			}
			if (_appliedMapOverrides.Contains("cornerroom_open"))
			{
				_appliedMapOverrides.Remove("cornerroom_open");
			}
			if (hasOwner && owner.mailReceived.Contains("renovation_corner_open"))
			{
				ApplyMapOverride("FarmHouse_CornerRoom_Add", "cornerroom_open");
				if (displayingSpouseRoom)
				{
					setMapTile(49, 19, 229, "Front", "untitled tile sheet");
				}
			}
			else
			{
				ApplyMapOverride("FarmHouse_CornerRoom_Remove", "cornerroom_open");
				if (displayingSpouseRoom)
				{
					setMapTile(49, 19, 87, "Front", "untitled tile sheet");
				}
			}
			if (_appliedMapOverrides.Contains("diningroom_open"))
			{
				_appliedMapOverrides.Remove("diningroom_open");
			}
			if (hasOwner && owner.mailReceived.Contains("renovation_dining_open"))
			{
				ApplyMapOverride("FarmHouse_DiningRoom_Add", "diningroom_open");
			}
			else
			{
				ApplyMapOverride("FarmHouse_DiningRoom_Remove", "diningroom_open");
			}
			if (_appliedMapOverrides.Contains("cubby_open"))
			{
				_appliedMapOverrides.Remove("cubby_open");
			}
			if (hasOwner && owner.mailReceived.Contains("renovation_cubby_open"))
			{
				ApplyMapOverride("FarmHouse_Cubby_Add", "cubby_open");
			}
			else
			{
				ApplyMapOverride("FarmHouse_Cubby_Remove", "cubby_open");
			}
			if (_appliedMapOverrides.Contains("farupperroom_open"))
			{
				_appliedMapOverrides.Remove("farupperroom_open");
			}
			if (hasOwner && owner.mailReceived.Contains("renovation_farupperroom_open"))
			{
				ApplyMapOverride("FarmHouse_FarUpperRoom_Add", "farupperroom_open");
			}
			else
			{
				ApplyMapOverride("FarmHouse_FarUpperRoom_Remove", "farupperroom_open");
			}
			if (_appliedMapOverrides.Contains("extendedcorner_open"))
			{
				_appliedMapOverrides.Remove("extendedcorner_open");
			}
			if (hasOwner && owner.mailReceived.Contains("renovation_extendedcorner_open"))
			{
				ApplyMapOverride("FarmHouse_ExtendedCornerRoom_Add", "extendedcorner_open");
			}
			else if (hasOwner && owner.mailReceived.Contains("renovation_corner_open"))
			{
				ApplyMapOverride("FarmHouse_ExtendedCornerRoom_Remove", "extendedcorner_open");
			}
			if (_appliedMapOverrides.Contains("diningroomwall_open"))
			{
				_appliedMapOverrides.Remove("diningroomwall_open");
			}
			if (hasOwner && owner.mailReceived.Contains("renovation_diningroomwall_open"))
			{
				ApplyMapOverride("FarmHouse_DiningRoomWall_Add", "diningroomwall_open");
			}
			else if (hasOwner && owner.mailReceived.Contains("renovation_dining_open"))
			{
				ApplyMapOverride("FarmHouse_DiningRoomWall_Remove", "diningroomwall_open");
			}
		}
		if (!TryGetMapProperty("AdditionalRenovations", out var propertyValue))
		{
			return;
		}
		string[] array = propertyValue.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = ArgUtility.SplitBySpace(array[i]);
			if (array2.Length < 4)
			{
				continue;
			}
			string text = array2[0];
			string item = array2[1];
			string map_name = array2[2];
			string map_name2 = array2[3];
			Microsoft.Xna.Framework.Rectangle? destination_rect = null;
			if (array2.Length >= 8)
			{
				try
				{
					destination_rect = new Microsoft.Xna.Framework.Rectangle
					{
						X = int.Parse(array2[4]),
						Y = int.Parse(array2[5]),
						Width = int.Parse(array2[6]),
						Height = int.Parse(array2[7])
					};
				}
				catch (Exception)
				{
					destination_rect = null;
				}
			}
			if (_appliedMapOverrides.Contains(text))
			{
				_appliedMapOverrides.Remove(text);
			}
			if (hasOwner && owner.mailReceived.Contains(item))
			{
				ApplyMapOverride(map_name, text, null, destination_rect);
			}
			else
			{
				ApplyMapOverride(map_name2, text, null, destination_rect);
			}
		}
	}

	public override void MakeMapModifications(bool force = false)
	{
		base.MakeMapModifications(force);
		updateFarmLayout();
		setWallpapers();
		setFloors();
		if (HasNpcSpouseOrRoommate("Sebastian") && Game1.netWorldState.Value.hasWorldStateID("sebastianFrog"))
		{
			Point spouseRoomCorner = GetSpouseRoomCorner();
			spouseRoomCorner.X++;
			spouseRoomCorner.Y += 6;
			Vector2 vector = Utility.PointToVector2(spouseRoomCorner);
			removeTile((int)vector.X, (int)vector.Y - 1, "Front");
			removeTile((int)vector.X + 1, (int)vector.Y - 1, "Front");
			removeTile((int)vector.X + 2, (int)vector.Y - 1, "Front");
		}
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		if (HasNpcSpouseOrRoommate("Emily") && Game1.player.eventsSeen.Contains("463391"))
		{
			Vector2 location = new Vector2(2064f, 160f);
			int num = upgradeLevel;
			if ((uint)(num - 2) <= 1u)
			{
				location = new Vector2(3408f, 1376f);
			}
			temporarySprites.Add(new EmilysParrot(location));
		}
		if (Game1.player.currentLocation == null || (!Game1.player.currentLocation.Equals(this) && !Game1.player.currentLocation.name.Value.StartsWith("Cellar")))
		{
			Game1.player.Position = Utility.PointToVector2(getEntryLocation()) * 64f;
			Game1.xLocationAfterWarp = Game1.player.TilePoint.X;
			Game1.yLocationAfterWarp = Game1.player.TilePoint.Y;
			Game1.player.currentLocation = this;
		}
		foreach (NPC character in characters)
		{
			if (character is Child child)
			{
				child.resetForPlayerEntry(this);
			}
			if (Game1.IsMasterGame && Game1.timeOfDay >= 2000 && !(character is Pet))
			{
				character.controller = null;
				character.Halt();
			}
		}
		if (IsOwnedByCurrentPlayer && Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).HasValue && Game1.player.team.IsMarried(Game1.player.UniqueMultiplayerID) && !Game1.player.mailReceived.Contains("CF_Spouse"))
		{
			Vector2 vector = Utility.PointToVector2(getEntryLocation()) + new Vector2(0f, -1f);
			Chest value = new Chest(new List<Item> { ItemRegistry.Create("(O)434") }, vector, giftbox: true, 1);
			overlayObjects[vector] = value;
		}
		if (IsOwnedByCurrentPlayer && !Game1.player.activeDialogueEvents.ContainsKey("pennyRedecorating"))
		{
			int num2 = -1;
			if (Game1.player.mailReceived.Contains("pennyQuilt0"))
			{
				num2 = 0;
			}
			else if (Game1.player.mailReceived.Contains("pennyQuilt1"))
			{
				num2 = 1;
			}
			else if (Game1.player.mailReceived.Contains("pennyQuilt2"))
			{
				num2 = 2;
			}
			if (num2 != -1 && !Game1.player.mailReceived.Contains("pennyRefurbished"))
			{
				List<Object> list = new List<Object>();
				foreach (Furniture item2 in furniture)
				{
					if (item2 is BedFurniture { bedType: BedFurniture.BedType.Double } bedFurniture)
					{
						string text = null;
						if (owner.mailReceived.Contains("pennyQuilt0"))
						{
							text = "2058";
						}
						if (owner.mailReceived.Contains("pennyQuilt1"))
						{
							text = "2064";
						}
						if (owner.mailReceived.Contains("pennyQuilt2"))
						{
							text = "2070";
						}
						if (text != null)
						{
							Vector2 tileLocation = bedFurniture.TileLocation;
							bedFurniture.performRemoveAction();
							list.Add(bedFurniture);
							Guid guid = furniture.GuidOf(bedFurniture);
							furniture.Remove(guid);
							furniture.Add(new BedFurniture(text, new Vector2(tileLocation.X, tileLocation.Y)));
						}
						break;
					}
				}
				Game1.player.mailReceived.Add("pennyRefurbished");
				Microsoft.Xna.Framework.Rectangle rectangle = ((upgradeLevel >= 2) ? new Microsoft.Xna.Framework.Rectangle(38, 20, 11, 13) : new Microsoft.Xna.Framework.Rectangle(20, 1, 8, 10));
				for (int i = rectangle.X; i <= rectangle.Right; i++)
				{
					for (int j = rectangle.Y; j <= rectangle.Bottom; j++)
					{
						if (getObjectAtTile(i, j) == null)
						{
							continue;
						}
						Object obj = getObjectAtTile(i, j);
						if (obj != null && !(obj is Chest) && !(obj is StorageFurniture) && !(obj is IndoorPot) && !(obj is BedFurniture))
						{
							if (obj.heldObject.Value != null && ((obj as Furniture)?.IsTable() ?? false))
							{
								Object value2 = obj.heldObject.Value;
								obj.heldObject.Value = null;
								list.Add(value2);
							}
							obj.performRemoveAction();
							if (obj is Fence fence)
							{
								obj = new Object(fence.ItemId, 1);
							}
							list.Add(obj);
							objects.Remove(new Vector2(i, j));
							if (obj is Furniture item)
							{
								furniture.Remove(item);
							}
						}
					}
				}
				decoratePennyRoom(num2, list);
			}
		}
		if (!HasNpcSpouseOrRoommate("Sebastian") || !Game1.netWorldState.Value.hasWorldStateID("sebastianFrog"))
		{
			return;
		}
		Point spouseRoomCorner = GetSpouseRoomCorner();
		spouseRoomCorner.X++;
		spouseRoomCorner.Y += 6;
		Vector2 vector2 = Utility.PointToVector2(spouseRoomCorner);
		temporarySprites.Add(new TemporaryAnimatedSprite
		{
			texture = Game1.mouseCursors,
			sourceRect = new Microsoft.Xna.Framework.Rectangle(641, 1534, 48, 37),
			animationLength = 1,
			sourceRectStartingPos = new Vector2(641f, 1534f),
			interval = 5000f,
			totalNumberOfLoops = 9999,
			position = vector2 * 64f + new Vector2(0f, -5f) * 4f,
			scale = 4f,
			layerDepth = (vector2.Y + 2f + 0.1f) * 64f / 10000f
		});
		if (Game1.random.NextDouble() < 0.85)
		{
			Texture2D texture = Game1.temporaryContent.Load<Texture2D>("TileSheets\\critters");
			base.TemporarySprites.Add(new SebsFrogs
			{
				texture = texture,
				sourceRect = new Microsoft.Xna.Framework.Rectangle(64, 224, 16, 16),
				animationLength = 1,
				sourceRectStartingPos = new Vector2(64f, 224f),
				interval = 100f,
				totalNumberOfLoops = 9999,
				position = vector2 * 64f + new Vector2(Game1.random.Choose(22, 25), Game1.random.Choose(2, 1)) * 4f,
				scale = 4f,
				flipped = Game1.random.NextBool(),
				layerDepth = (vector2.Y + 2f + 0.11f) * 64f / 10000f,
				Parent = this
			});
		}
		if (!Game1.player.activeDialogueEvents.ContainsKey("sebastianFrog2") && Game1.random.NextBool())
		{
			Texture2D texture2 = Game1.temporaryContent.Load<Texture2D>("TileSheets\\critters");
			base.TemporarySprites.Add(new SebsFrogs
			{
				texture = texture2,
				sourceRect = new Microsoft.Xna.Framework.Rectangle(64, 240, 16, 16),
				animationLength = 1,
				sourceRectStartingPos = new Vector2(64f, 240f),
				interval = 150f,
				totalNumberOfLoops = 9999,
				position = vector2 * 64f + new Vector2(8f, 3f) * 4f,
				scale = 4f,
				layerDepth = (vector2.Y + 2f + 0.11f) * 64f / 10000f,
				flipped = Game1.random.NextBool(),
				pingPong = false,
				Parent = this
			});
			if (Game1.random.NextDouble() < 0.1 && Game1.timeOfDay > 610)
			{
				DelayedAction.playSoundAfterDelay("croak", 1000);
			}
		}
	}

	private void addFurnitureIfSpaceIsFreePenny(List<Object> objectsToStoreInChests, Furniture f, Furniture heldObject = null)
	{
		bool flag = false;
		foreach (Furniture item in furniture)
		{
			if (f.GetBoundingBox().Intersects(item.GetBoundingBox()))
			{
				flag = true;
				break;
			}
		}
		if (objects.ContainsKey(f.TileLocation))
		{
			flag = true;
		}
		if (!flag)
		{
			furniture.Add(f);
			if (heldObject != null)
			{
				f.heldObject.Value = heldObject;
			}
		}
		else
		{
			objectsToStoreInChests.Add(f);
			if (heldObject != null)
			{
				objectsToStoreInChests.Add(heldObject);
			}
		}
	}

	private void decoratePennyRoom(int whichStyle, List<Object> objectsToStoreInChests)
	{
		List<Chest> list = new List<Chest>();
		List<Vector2> list2 = new List<Vector2>();
		Color value = default(Color);
		switch (whichStyle)
		{
		case 0:
			if (upgradeLevel == 1)
			{
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1916").SetPlacement(20, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1914").SetPlacement(21, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1915").SetPlacement(22, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1914").SetPlacement(23, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1916").SetPlacement(24, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1682").SetPlacement(26, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1747").SetPlacement(25, 4));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1395").SetPlacement(26, 4), ItemRegistry.Create<Furniture>("(F)1363"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1443").SetPlacement(27, 4));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1664").SetPlacement(27, 5, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1978").SetPlacement(21, 6));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1124").SetPlacement(26, 9), ItemRegistry.Create<Furniture>("(F)1368"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)6").SetPlacement(25, 10, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1296").SetPlacement(28, 10));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1747").SetPlacement(24, 10));
				SetWallpaper("107", "Bedroom");
				SetFloor("2", "Bedroom");
				value = new Color(85, 85, 255);
				list2.Add(new Vector2(21f, 10f));
				list2.Add(new Vector2(22f, 10f));
			}
			else
			{
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1916").SetPlacement(38, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1914").SetPlacement(39, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1604").SetPlacement(41, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1915").SetPlacement(43, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1916").SetPlacement(45, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1914").SetPlacement(47, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1916").SetPlacement(48, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1443").SetPlacement(38, 23));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1747").SetPlacement(39, 23));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1395").SetPlacement(40, 23), ItemRegistry.Create<Furniture>("(F)1363"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)714").SetPlacement(46, 23));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1443").SetPlacement(48, 23));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1978").SetPlacement(42, 25));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1664").SetPlacement(47, 25, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1664").SetPlacement(38, 27, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1124").SetPlacement(46, 31), ItemRegistry.Create<Furniture>("(F)1368"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)416").SetPlacement(40, 32, 2));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1296").SetPlacement(38, 32));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)6").SetPlacement(45, 32, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1296").SetPlacement(48, 32));
				SetWallpaper("107", "Bedroom");
				SetFloor("2", "Bedroom");
				value = new Color(85, 85, 255);
				list2.Add(new Vector2(38f, 24f));
				list2.Add(new Vector2(39f, 24f));
			}
			break;
		case 1:
			if (upgradeLevel == 1)
			{
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1678").SetPlacement(20, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1814").SetPlacement(21, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1814").SetPlacement(22, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1814").SetPlacement(23, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1907").SetPlacement(24, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1400").SetPlacement(25, 4), ItemRegistry.Create<Furniture>("(F)1365"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1866").SetPlacement(26, 4));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1909").SetPlacement(27, 6, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1451").SetPlacement(21, 6));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1138").SetPlacement(27, 9), ItemRegistry.Create<Furniture>("(F)1378"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)12").SetPlacement(26, 10, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1758").SetPlacement(24, 10));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1618").SetPlacement(21, 9));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1390").SetPlacement(22, 10));
				SetWallpaper("84", "Bedroom");
				SetFloor("35", "Bedroom");
				value = new Color(255, 85, 85);
				list2.Add(new Vector2(21f, 10f));
				list2.Add(new Vector2(23f, 10f));
			}
			else
			{
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1678").SetPlacement(39, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1907").SetPlacement(40, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1814").SetPlacement(42, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1814").SetPlacement(43, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1814").SetPlacement(44, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1907").SetPlacement(45, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1916").SetPlacement(48, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1758").SetPlacement(38, 23));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1400").SetPlacement(40, 23), ItemRegistry.Create<Furniture>("(F)1365"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1390").SetPlacement(46, 23));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1866").SetPlacement(47, 23));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1387").SetPlacement(38, 24));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1909").SetPlacement(47, 24, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)719").SetPlacement(38, 25, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1451").SetPlacement(42, 25));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1909").SetPlacement(38, 27, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1389").SetPlacement(47, 29));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1377").SetPlacement(48, 29));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1758").SetPlacement(41, 30));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)424").SetPlacement(42, 30, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1618").SetPlacement(44, 30));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)536").SetPlacement(47, 30, 3));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1138").SetPlacement(38, 31), ItemRegistry.Create<Furniture>("(F)1378"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1383").SetPlacement(41, 31));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1449").SetPlacement(48, 32));
				SetWallpaper("84", "Bedroom");
				SetFloor("35", "Bedroom");
				value = new Color(255, 85, 85);
				list2.Add(new Vector2(39f, 23f));
				list2.Add(new Vector2(43f, 25f));
			}
			break;
		case 2:
			if (upgradeLevel == 1)
			{
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1673").SetPlacement(20, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1547").SetPlacement(21, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1675").SetPlacement(24, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1900").SetPlacement(25, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1393").SetPlacement(25, 4), ItemRegistry.Create<Furniture>("(F)1367"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1798").SetPlacement(26, 4));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1902").SetPlacement(25, 5));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1751").SetPlacement(22, 6));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1122").SetPlacement(26, 9), ItemRegistry.Create<Furniture>("(F)1378"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)197").SetPlacement(28, 9, 3));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)3").SetPlacement(25, 10, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1294").SetPlacement(20, 10));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1294").SetPlacement(24, 10));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1964").SetPlacement(21, 8));
				SetWallpaper("95", "Bedroom");
				SetFloor("1", "Bedroom");
				value = new Color(85, 85, 85);
				list2.Add(new Vector2(22f, 10f));
				list2.Add(new Vector2(23f, 10f));
			}
			else
			{
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1673").SetPlacement(38, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1675").SetPlacement(40, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1547").SetPlacement(42, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1900").SetPlacement(45, 20));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1751").SetPlacement(38, 23));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1393").SetPlacement(40, 23), ItemRegistry.Create<Furniture>("(F)1367"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1798").SetPlacement(47, 23));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1902").SetPlacement(46, 24));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1964").SetPlacement(42, 25));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1294").SetPlacement(38, 26));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)3").SetPlacement(46, 29));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1294").SetPlacement(38, 30));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)1122").SetPlacement(46, 30), ItemRegistry.Create<Furniture>("(F)1369"));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)197").SetPlacement(48, 30, 3));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)709").SetPlacement(38, 31, 1));
				addFurnitureIfSpaceIsFreePenny(objectsToStoreInChests, ItemRegistry.Create<Furniture>("(F)3").SetPlacement(47, 32, 2));
				SetWallpaper("95", "Bedroom");
				SetFloor("1", "Bedroom");
				value = new Color(85, 85, 85);
				list2.Add(new Vector2(39f, 23f));
				list2.Add(new Vector2(46f, 23f));
			}
			break;
		}
		if (objectsToStoreInChests != null)
		{
			foreach (Object objectsToStoreInChest in objectsToStoreInChests)
			{
				if (list.Count == 0)
				{
					list.Add(new Chest(playerChest: true));
				}
				bool flag = false;
				foreach (Chest item in list)
				{
					if (item.addItem(objectsToStoreInChest) == null)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					Chest chest = new Chest(playerChest: true);
					list.Add(chest);
					chest.addItem(objectsToStoreInChest);
				}
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			Chest chest2 = list[i];
			chest2.playerChoiceColor.Value = value;
			Vector2 tileLocation = list2[Math.Min(i, list2.Count - 1)];
			PlaceInNearbySpace(tileLocation, chest2);
		}
	}

	public void PlaceInNearbySpace(Vector2 tileLocation, Object o)
	{
		if (o == null || tileLocation.Equals(Vector2.Zero))
		{
			return;
		}
		int i = 0;
		Queue<Vector2> queue = new Queue<Vector2>();
		HashSet<Vector2> hashSet = new HashSet<Vector2>();
		queue.Enqueue(tileLocation);
		Vector2 vector = Vector2.Zero;
		for (; i < 100; i++)
		{
			vector = queue.Dequeue();
			if (CanItemBePlacedHere(vector))
			{
				break;
			}
			hashSet.Add(vector);
			foreach (Vector2 adjacentTileLocation in Utility.getAdjacentTileLocations(vector))
			{
				if (!hashSet.Contains(adjacentTileLocation))
				{
					queue.Enqueue(adjacentTileLocation);
				}
			}
		}
		if (!vector.Equals(Vector2.Zero) && CanItemBePlacedHere(vector))
		{
			o.TileLocation = vector;
			objects.Add(vector, o);
		}
	}

	public virtual void RefreshFloorObjectNeighbors()
	{
		foreach (Vector2 key in terrainFeatures.Keys)
		{
			if (terrainFeatures[key] is Flooring flooring)
			{
				flooring.OnAdded(this, key);
			}
		}
	}

	public void moveObjectsForHouseUpgrade(int whichUpgrade)
	{
		previousUpgradeLevel = upgradeLevel;
		overlayObjects.Clear();
		switch (whichUpgrade)
		{
		case 0:
			if (upgradeLevel == 1)
			{
				shiftContents(-6, 0);
			}
			break;
		case 1:
			switch (upgradeLevel)
			{
			case 0:
				shiftContents(6, 0);
				break;
			case 2:
				shiftContents(-3, 0);
				break;
			}
			break;
		case 2:
		case 3:
			switch (upgradeLevel)
			{
			case 1:
				shiftContents(18, 19);
				foreach (Furniture item in furniture)
				{
					if (item.tileLocation.X >= 25f && item.tileLocation.X <= 28f && item.tileLocation.Y >= 20f && item.tileLocation.Y <= 21f)
					{
						item.TileLocation = new Vector2(item.tileLocation.X - 3f, item.tileLocation.Y - 9f);
					}
				}
				moveFurniture(42, 23, 16, 14);
				moveFurniture(43, 23, 17, 14);
				moveFurniture(44, 23, 18, 14);
				moveFurniture(43, 24, 22, 14);
				moveFurniture(44, 24, 23, 14);
				moveFurniture(42, 24, 19, 14);
				moveFurniture(43, 25, 20, 14);
				moveFurniture(44, 26, 21, 14);
				break;
			case 0:
				shiftContents(24, 19);
				break;
			}
			break;
		}
	}

	protected override LocalizedContentManager getMapLoader()
	{
		if (mapLoader == null)
		{
			mapLoader = Game1.game1.xTileContent.CreateTemporary();
		}
		return mapLoader;
	}

	protected override void _updateAmbientLighting()
	{
		if (Game1.isStartingToGetDarkOut(this) || lightLevel.Value > 0f)
		{
			int startTime = Game1.timeOfDay + Game1.gameTimeInterval / (Game1.realMilliSecondsPerGameMinute + base.ExtraMillisecondsPerInGameMinute);
			float t = 1f - Utility.Clamp((float)Utility.CalculateMinutesBetweenTimes(startTime, Game1.getTrulyDarkTime(this)) / 120f, 0f, 1f);
			Game1.ambientLight = new Color((byte)Utility.Lerp(Game1.isRaining ? rainLightingColor.R : 0, (int)nightLightingColor.R, t), (byte)Utility.Lerp(Game1.isRaining ? rainLightingColor.G : 0, (int)nightLightingColor.G, t), (byte)Utility.Lerp(0f, (int)nightLightingColor.B, t));
		}
		else
		{
			Game1.ambientLight = (Game1.isRaining ? rainLightingColor : Color.White);
		}
	}

	public override void drawAboveFrontLayer(SpriteBatch b)
	{
		base.drawAboveFrontLayer(b);
		if (fridge.Value.mutex.IsLocked())
		{
			b.Draw(Game1.mouseCursors2, Game1.GlobalToLocal(Game1.viewport, new Vector2(fridgePosition.X, fridgePosition.Y - 1) * 64f), new Microsoft.Xna.Framework.Rectangle(0, 192, 16, 32), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)((fridgePosition.Y + 1) * 64 + 1) / 10000f);
		}
	}

	public override void updateMap()
	{
		bool flag = HasNpcSpouseOrRoommate();
		mapPath.Value = "Maps\\FarmHouse" + ((upgradeLevel == 0) ? "" : ((upgradeLevel == 3) ? "2" : (upgradeLevel.ToString() ?? ""))) + (flag ? "_marriage" : "");
		base.updateMap();
	}

	public virtual void setMapForUpgradeLevel(int level)
	{
		upgradeLevel = level;
		int value = synchronizedDisplayedLevel.Value;
		currentlyDisplayedUpgradeLevel = level;
		synchronizedDisplayedLevel.Value = level;
		bool flag = HasNpcSpouseOrRoommate();
		if (displayingSpouseRoom && !flag)
		{
			displayingSpouseRoom = false;
		}
		updateMap();
		RefreshFloorObjectNeighbors();
		if (flag)
		{
			showSpouseRoom();
		}
		loadObjects();
		if (level == 3)
		{
			AddCellarTiles();
			createCellarWarps();
			Game1.player.craftingRecipes.TryAdd("Cask", 0);
		}
		bool flag2 = previousUpgradeLevel == 0 && upgradeLevel >= 0;
		if (previousUpgradeLevel >= 0)
		{
			if (previousUpgradeLevel < 2 && upgradeLevel >= 2)
			{
				for (int i = 0; i < map.Layers[0].LayerWidth; i++)
				{
					for (int j = 0; j < map.Layers[0].LayerHeight; j++)
					{
						if (doesTileHaveProperty(i, j, "DefaultChildBedPosition", "Back") != null)
						{
							string cHILD_BED_INDEX = BedFurniture.CHILD_BED_INDEX;
							base.furniture.Add(new BedFurniture(cHILD_BED_INDEX, new Vector2(i, j)));
							break;
						}
					}
				}
			}
			Furniture furniture = null;
			if (previousUpgradeLevel == 0)
			{
				foreach (Furniture item in base.furniture)
				{
					if (item is BedFurniture { bedType: BedFurniture.BedType.Single } bedFurniture)
					{
						furniture = bedFurniture;
						break;
					}
				}
			}
			else
			{
				foreach (Furniture item2 in base.furniture)
				{
					if (item2 is BedFurniture { bedType: BedFurniture.BedType.Double } bedFurniture2)
					{
						furniture = bedFurniture2;
						break;
					}
				}
			}
			if (upgradeLevel != 3 || flag2)
			{
				for (int k = 0; k < map.Layers[0].LayerWidth; k++)
				{
					for (int l = 0; l < map.Layers[0].LayerHeight; l++)
					{
						if (doesTileHaveProperty(k, l, "DefaultBedPosition", "Back") == null)
						{
							continue;
						}
						string bedId = BedFurniture.DEFAULT_BED_INDEX;
						if (previousUpgradeLevel != 1 || furniture == null || (furniture.tileLocation.X == 39f && furniture.tileLocation.Y == 22f))
						{
							if (furniture != null)
							{
								bedId = furniture.ItemId;
							}
							if (previousUpgradeLevel == 0 && furniture != null)
							{
								furniture.performRemoveAction();
								Guid guid = base.furniture.GuidOf(furniture);
								base.furniture.Remove(guid);
								bedId = Utility.GetDoubleWideVersionOfBed(bedId);
								base.furniture.Add(new BedFurniture(bedId, new Vector2(k, l)));
							}
							else if (furniture != null)
							{
								furniture.performRemoveAction();
								Guid guid2 = base.furniture.GuidOf(furniture);
								base.furniture.Remove(guid2);
								base.furniture.Add(new BedFurniture(furniture.ItemId, new Vector2(k, l)));
							}
						}
						break;
					}
				}
			}
			previousUpgradeLevel = -1;
		}
		if (value != level)
		{
			lightGlows.Clear();
		}
		fridgePosition = GetFridgePositionFromMap() ?? Point.Zero;
	}

	/// <summary>Get the fridge position by scanning the map tiles for the sprite index.</summary>
	/// <remarks>This is relatively expensive. Most code should use the cached <see cref="F:StardewValley.Locations.FarmHouse.fridgePosition" /> instead.</remarks>
	public Point? GetFridgePositionFromMap()
	{
		Layer layer = map.RequireLayer("Buildings");
		for (int i = 0; i < layer.LayerHeight; i++)
		{
			for (int j = 0; j < layer.LayerWidth; j++)
			{
				if (layer.GetTileIndexAt(j, i, "untitled tile sheet") == 173)
				{
					return new Point(j, i);
				}
			}
		}
		return null;
	}

	public void createCellarWarps()
	{
		updateCellarWarps();
	}

	public void updateCellarWarps()
	{
		Layer layer = map.RequireLayer("Back");
		string cellarName = GetCellarName();
		if (cellarName == null)
		{
			return;
		}
		for (int i = 0; i < layer.LayerWidth; i++)
		{
			for (int j = 0; j < layer.LayerHeight; j++)
			{
				string[] tilePropertySplitBySpaces = GetTilePropertySplitBySpaces("TouchAction", "Back", i, j);
				if (ArgUtility.Get(tilePropertySplitBySpaces, 0) == "Warp" && ArgUtility.Get(tilePropertySplitBySpaces, 1, "").StartsWith("Cellar"))
				{
					tilePropertySplitBySpaces[1] = cellarName;
					setTileProperty(i, j, "Back", "TouchAction", string.Join(" ", tilePropertySplitBySpaces));
				}
			}
		}
		if (cellarWarps == null)
		{
			return;
		}
		foreach (Warp cellarWarp in cellarWarps)
		{
			if (!warps.Contains(cellarWarp))
			{
				warps.Add(cellarWarp);
			}
			cellarWarp.TargetName = cellarName;
		}
	}

	public virtual Point GetSpouseRoomCorner()
	{
		if (TryGetMapPropertyAs("SpouseRoomPosition", out Point parsed, required: false))
		{
			return parsed;
		}
		if (upgradeLevel != 1)
		{
			return new Point(50, 20);
		}
		return new Point(29, 1);
	}

	public virtual void loadSpouseRoom()
	{
		string obj = ((owner?.spouse != null && owner.isMarriedOrRoommates()) ? owner.spouse : null);
		CharacterData data;
		CharacterSpouseRoomData characterSpouseRoomData = ((!NPC.TryGetData(obj, out data)) ? null : data?.SpouseRoom);
		spouseRoomSpot = GetSpouseRoomCorner();
		spouseRoomSpot.X += 3;
		spouseRoomSpot.Y += 4;
		if (obj == null)
		{
			return;
		}
		string text = characterSpouseRoomData?.MapAsset ?? "spouseRooms";
		Microsoft.Xna.Framework.Rectangle rectangle = characterSpouseRoomData?.MapSourceRect ?? CharacterSpouseRoomData.DefaultMapSourceRect;
		Point spouseRoomCorner = GetSpouseRoomCorner();
		Microsoft.Xna.Framework.Rectangle rectangle2 = new Microsoft.Xna.Framework.Rectangle(spouseRoomCorner.X, spouseRoomCorner.Y, rectangle.Width, rectangle.Height);
		Map map = Game1.game1.xTileContent.Load<Map>("Maps\\" + text);
		Point location = rectangle.Location;
		base.map.Properties.Remove("Light");
		base.map.Properties.Remove("DayTiles");
		base.map.Properties.Remove("NightTiles");
		List<KeyValuePair<Point, Tile>> list = new List<KeyValuePair<Point, Tile>>();
		Layer layer = base.map.RequireLayer("Front");
		for (int i = rectangle2.Left; i < rectangle2.Right; i++)
		{
			Point key = new Point(i, rectangle2.Bottom - 1);
			Tile tile = layer.Tiles[key.X, key.Y];
			if (tile != null)
			{
				list.Add(new KeyValuePair<Point, Tile>(key, tile));
			}
		}
		if (_appliedMapOverrides.Contains("spouse_room"))
		{
			_appliedMapOverrides.Remove("spouse_room");
		}
		ApplyMapOverride(text, "spouse_room", new Microsoft.Xna.Framework.Rectangle(location.X, location.Y, rectangle2.Width, rectangle2.Height), rectangle2);
		Layer layer2 = map.RequireLayer("Buildings");
		Layer layer3 = map.RequireLayer("Front");
		for (int j = 0; j < rectangle2.Width; j++)
		{
			for (int k = 0; k < rectangle2.Height; k++)
			{
				int tileIndexAt = layer2.GetTileIndexAt(location.X + j, location.Y + k);
				if (tileIndexAt != -1)
				{
					adjustMapLightPropertiesForLamp(tileIndexAt, rectangle2.X + j, rectangle2.Y + k, "Buildings");
				}
				if (k < rectangle2.Height - 1)
				{
					tileIndexAt = layer3.GetTileIndexAt(location.X + j, location.Y + k);
					if (tileIndexAt != -1)
					{
						adjustMapLightPropertiesForLamp(tileIndexAt, rectangle2.X + j, rectangle2.Y + k, "Front");
					}
				}
			}
		}
		foreach (Point point2 in rectangle2.GetPoints())
		{
			if (getTileIndexAt(point2, "Paths") == 7)
			{
				spouseRoomSpot = point2;
				break;
			}
		}
		Point point = GetSpouseRoomSpot();
		setTileProperty(point.X, point.Y, "Back", "NoFurniture", "T");
		foreach (KeyValuePair<Point, Tile> item in list)
		{
			layer.Tiles[item.Key.X, item.Key.Y] = item.Value;
		}
	}

	public virtual Microsoft.Xna.Framework.Rectangle? GetCribBounds()
	{
		if (upgradeLevel < 2)
		{
			return null;
		}
		return new Microsoft.Xna.Framework.Rectangle(30, 12, 3, 4);
	}

	public virtual void UpdateChildRoom()
	{
		Microsoft.Xna.Framework.Rectangle? cribBounds = GetCribBounds();
		if (cribBounds.HasValue)
		{
			if (_appliedMapOverrides.Contains("crib"))
			{
				_appliedMapOverrides.Remove("crib");
			}
			ApplyMapOverride("FarmHouse_Crib_" + cribStyle.Value, "crib", null, cribBounds);
		}
	}

	public void playerDivorced()
	{
		displayingSpouseRoom = false;
	}

	public virtual List<Microsoft.Xna.Framework.Rectangle> getForbiddenPetWarpTiles()
	{
		List<Microsoft.Xna.Framework.Rectangle> list = new List<Microsoft.Xna.Framework.Rectangle>();
		switch (upgradeLevel)
		{
		case 0:
			list.Add(new Microsoft.Xna.Framework.Rectangle(2, 8, 3, 4));
			break;
		case 1:
			list.Add(new Microsoft.Xna.Framework.Rectangle(8, 8, 3, 4));
			list.Add(new Microsoft.Xna.Framework.Rectangle(17, 8, 4, 3));
			break;
		case 2:
		case 3:
			list.Add(new Microsoft.Xna.Framework.Rectangle(26, 27, 3, 4));
			list.Add(new Microsoft.Xna.Framework.Rectangle(35, 27, 4, 3));
			list.Add(new Microsoft.Xna.Framework.Rectangle(27, 15, 4, 3));
			list.Add(new Microsoft.Xna.Framework.Rectangle(26, 17, 2, 6));
			break;
		}
		return list;
	}

	public bool canPetWarpHere(Vector2 tile_position)
	{
		foreach (Microsoft.Xna.Framework.Rectangle forbiddenPetWarpTile in getForbiddenPetWarpTiles())
		{
			if (forbiddenPetWarpTile.Contains((int)tile_position.X, (int)tile_position.Y))
			{
				return false;
			}
		}
		return true;
	}

	public override List<Microsoft.Xna.Framework.Rectangle> getWalls()
	{
		List<Microsoft.Xna.Framework.Rectangle> list = new List<Microsoft.Xna.Framework.Rectangle>();
		switch (upgradeLevel)
		{
		case 0:
			list.Add(new Microsoft.Xna.Framework.Rectangle(1, 1, 10, 3));
			break;
		case 1:
			list.Add(new Microsoft.Xna.Framework.Rectangle(1, 1, 17, 3));
			list.Add(new Microsoft.Xna.Framework.Rectangle(18, 6, 2, 2));
			list.Add(new Microsoft.Xna.Framework.Rectangle(20, 1, 9, 3));
			break;
		case 2:
		case 3:
		{
			bool hasOwner = HasOwner;
			list.Add(new Microsoft.Xna.Framework.Rectangle(1, 1, 12, 3));
			list.Add(new Microsoft.Xna.Framework.Rectangle(15, 1, 13, 3));
			list.Add(new Microsoft.Xna.Framework.Rectangle(13, 3, 2, 2));
			list.Add(new Microsoft.Xna.Framework.Rectangle(1, 10, 10, 3));
			list.Add(new Microsoft.Xna.Framework.Rectangle(13, 10, 8, 3));
			int num = ((hasOwner && owner.hasOrWillReceiveMail("renovation_corner_open")) ? (-3) : 0);
			if (hasOwner && owner.hasOrWillReceiveMail("renovation_bedroom_open"))
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(21, 15, 0, 2));
				list.Add(new Microsoft.Xna.Framework.Rectangle(21, 10, 13 + num, 3));
			}
			else
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(21, 15, 2, 2));
				list.Add(new Microsoft.Xna.Framework.Rectangle(23, 10, 11 + num, 3));
			}
			if (hasOwner && owner.hasOrWillReceiveMail("renovation_southern_open"))
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(23, 24, 3, 3));
				list.Add(new Microsoft.Xna.Framework.Rectangle(31, 24, 3, 3));
			}
			else
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(0, 0, 0, 0));
				list.Add(new Microsoft.Xna.Framework.Rectangle(0, 0, 0, 0));
			}
			if (hasOwner && owner.hasOrWillReceiveMail("renovation_corner_open"))
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(30, 1, 9, 3));
				list.Add(new Microsoft.Xna.Framework.Rectangle(28, 3, 2, 2));
			}
			else
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(0, 0, 0, 0));
				list.Add(new Microsoft.Xna.Framework.Rectangle(0, 0, 0, 0));
			}
			foreach (Microsoft.Xna.Framework.Rectangle item in list)
			{
				item.Offset(15, 10);
			}
			break;
		}
		}
		return list;
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		if (l is FarmHouse farmHouse)
		{
			cribStyle.Value = farmHouse.cribStyle.Value;
		}
		base.TransferDataFromSavedLocation(l);
	}

	public override List<Microsoft.Xna.Framework.Rectangle> getFloors()
	{
		List<Microsoft.Xna.Framework.Rectangle> list = new List<Microsoft.Xna.Framework.Rectangle>();
		switch (upgradeLevel)
		{
		case 0:
			list.Add(new Microsoft.Xna.Framework.Rectangle(1, 3, 10, 9));
			break;
		case 1:
			list.Add(new Microsoft.Xna.Framework.Rectangle(1, 3, 6, 9));
			list.Add(new Microsoft.Xna.Framework.Rectangle(7, 3, 11, 9));
			list.Add(new Microsoft.Xna.Framework.Rectangle(18, 8, 2, 2));
			list.Add(new Microsoft.Xna.Framework.Rectangle(20, 3, 9, 8));
			break;
		case 2:
		case 3:
		{
			bool hasOwner = HasOwner;
			list.Add(new Microsoft.Xna.Framework.Rectangle(1, 3, 12, 6));
			list.Add(new Microsoft.Xna.Framework.Rectangle(15, 3, 13, 6));
			list.Add(new Microsoft.Xna.Framework.Rectangle(13, 5, 2, 2));
			list.Add(new Microsoft.Xna.Framework.Rectangle(0, 12, 10, 11));
			list.Add(new Microsoft.Xna.Framework.Rectangle(10, 12, 11, 9));
			if (hasOwner && owner.mailReceived.Contains("renovation_bedroom_open"))
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(21, 17, 0, 2));
				list.Add(new Microsoft.Xna.Framework.Rectangle(21, 12, 14, 11));
			}
			else
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(21, 17, 2, 2));
				list.Add(new Microsoft.Xna.Framework.Rectangle(23, 12, 12, 11));
			}
			if (hasOwner && owner.hasOrWillReceiveMail("renovation_southern_open"))
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(23, 26, 11, 8));
			}
			else
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(0, 0, 0, 0));
			}
			if (hasOwner && owner.hasOrWillReceiveMail("renovation_corner_open"))
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(28, 5, 2, 3));
				list.Add(new Microsoft.Xna.Framework.Rectangle(30, 3, 9, 6));
			}
			else
			{
				list.Add(new Microsoft.Xna.Framework.Rectangle(0, 0, 0, 0));
				list.Add(new Microsoft.Xna.Framework.Rectangle(0, 0, 0, 0));
			}
			foreach (Microsoft.Xna.Framework.Rectangle item in list)
			{
				item.Offset(15, 10);
			}
			break;
		}
		}
		return list;
	}

	public virtual bool CanModifyCrib()
	{
		if (!HasOwner)
		{
			return false;
		}
		if (owner.isMarriedOrRoommates() && owner.GetSpouseFriendship().DaysUntilBirthing != -1)
		{
			return false;
		}
		foreach (Child child in owner.getChildren())
		{
			if (child.Age < 3)
			{
				return false;
			}
		}
		return true;
	}
}
