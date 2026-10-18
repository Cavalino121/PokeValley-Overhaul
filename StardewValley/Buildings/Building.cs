using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Netcode.Validation;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Buildings;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Mods;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Util;
using xTile.Dimensions;

namespace StardewValley.Buildings;

[XmlInclude(typeof(Barn))]
[XmlInclude(typeof(Coop))]
[XmlInclude(typeof(FishPond))]
[XmlInclude(typeof(GreenhouseBuilding))]
[XmlInclude(typeof(JunimoHut))]
[XmlInclude(typeof(Mill))]
[XmlInclude(typeof(PetBowl))]
[XmlInclude(typeof(ShippingBin))]
[XmlInclude(typeof(Stable))]
[NotImplicitNetField]
public class Building : INetObject<NetFields>, IHaveModData
{
	/// <summary>A unique identifier for this specific building instance.</summary>
	[XmlElement("id")]
	public readonly NetGuid id = new NetGuid();

	[XmlIgnore]
	public Lazy<Texture2D> texture;

	[XmlIgnore]
	public Texture2D paintedTexture;

	public NetString skinId = new NetString();

	/// <summary>The indoor location created for this building, if any.</summary>
	/// <remarks>This is mutually exclusive with <see cref="F:StardewValley.Buildings.Building.nonInstancedIndoorsName" />. Most code should use <see cref="M:StardewValley.Buildings.Building.GetIndoors" /> instead, which handles both.</remarks>
	[XmlElement("indoors")]
	public readonly NetRef<GameLocation> indoors = new NetRef<GameLocation>();

	/// <summary>The unique ID of the separate location treated as the building interior (like <c>FarmHouse</c> for the farmhouse), if any.</summary>
	/// <remarks>This is mutually exclusive with <see cref="F:StardewValley.Buildings.Building.indoors" />. Most code should use <see cref="M:StardewValley.Buildings.Building.GetIndoors" /> instead, which handles both.</remarks>
	public readonly NetString nonInstancedIndoorsName = new NetString();

	[XmlElement("tileX")]
	public readonly NetInt tileX = new NetInt();

	[XmlElement("tileY")]
	public readonly NetInt tileY = new NetInt();

	[XmlElement("tilesWide")]
	public readonly NetInt tilesWide = new NetInt();

	[XmlElement("tilesHigh")]
	public readonly NetInt tilesHigh = new NetInt();

	[XmlElement("maxOccupants")]
	public readonly NetInt maxOccupants = new NetInt();

	[XmlElement("currentOccupants")]
	public readonly NetInt currentOccupants = new NetInt();

	[XmlElement("daysOfConstructionLeft")]
	public readonly NetInt daysOfConstructionLeft = new NetInt();

	[XmlElement("daysUntilUpgrade")]
	public readonly NetInt daysUntilUpgrade = new NetInt();

	[XmlElement("upgradeName")]
	public readonly NetString upgradeName = new NetString();

	[XmlElement("buildingType")]
	public readonly NetString buildingType = new NetString();

	[XmlElement("buildingPaintColor")]
	public NetRef<BuildingPaintColor> netBuildingPaintColor = new NetRef<BuildingPaintColor>();

	[XmlElement("hayCapacity")]
	public NetInt hayCapacity = new NetInt();

	public NetList<Chest, NetRef<Chest>> buildingChests = new NetList<Chest, NetRef<Chest>>();

	/// <summary>The unique name of the location which contains this building.</summary>
	[XmlIgnore]
	public NetString parentLocationName = new NetString();

	[XmlIgnore]
	public bool hasLoaded;

	[XmlIgnore]
	protected Dictionary<string, string> buildingMetadata = new Dictionary<string, string>();

	protected int lastHouseUpgradeLevel = -1;

	protected bool? hasChimney;

	protected Vector2 chimneyPosition = Vector2.Zero;

	protected int chimneyTimer = 500;

	[XmlElement("humanDoor")]
	public readonly NetPoint humanDoor = new NetPoint();

	[XmlElement("animalDoor")]
	public readonly NetPoint animalDoor = new NetPoint();

	/// <summary>A temporary color applied to the building sprite when it's highlighted in a menu.</summary>
	[XmlIgnore]
	public Color color = Color.White;

	[XmlElement("animalDoorOpen")]
	public readonly NetBool animalDoorOpen = new NetBool();

	[XmlElement("animalDoorOpenAmount")]
	public readonly NetFloat animalDoorOpenAmount = new NetFloat
	{
		InterpolationWait = false
	};

	[XmlElement("magical")]
	public readonly NetBool magical = new NetBool();

	/// <summary>Whether this building should fade into semi-transparency when the local player is behind it.</summary>
	[XmlElement("fadeWhenPlayerIsBehind")]
	public readonly NetBool fadeWhenPlayerIsBehind = new NetBool(value: true);

	[XmlElement("owner")]
	public readonly NetLong owner = new NetLong();

	[XmlElement("newConstructionTimer")]
	protected readonly NetInt newConstructionTimer = new NetInt();

	/// <summary>The building's opacity for the local player as a value between 0 (transparent) and 1 (opaque), accounting for <see cref="F:StardewValley.Buildings.Building.fadeWhenPlayerIsBehind" />.</summary>
	[XmlIgnore]
	public float alpha = 1f;

	[XmlIgnore]
	protected bool _isMoving;

	public static Microsoft.Xna.Framework.Rectangle leftShadow = new Microsoft.Xna.Framework.Rectangle(656, 394, 16, 16);

	public static Microsoft.Xna.Framework.Rectangle middleShadow = new Microsoft.Xna.Framework.Rectangle(672, 394, 16, 16);

	public static Microsoft.Xna.Framework.Rectangle rightShadow = new Microsoft.Xna.Framework.Rectangle(688, 394, 16, 16);

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

	/// <summary>Get whether this is a farmhand cabin.</summary>
	/// <remarks>To check whether a farmhand has claimed it, use <see cref="M:StardewValley.Buildings.Building.GetIndoors" /> to get the <see cref="T:StardewValley.Locations.Cabin" /> or <see cref="T:StardewValley.Locations.FarmHouse" /> instance and call methods like <see cref="P:StardewValley.Locations.FarmHouse.HasOwner" />.</remarks>
	public bool isCabin => buildingType.Value == "Cabin";

	public bool isMoving
	{
		get
		{
			return _isMoving;
		}
		set
		{
			if (_isMoving != value)
			{
				_isMoving = value;
				if (_isMoving)
				{
					OnStartMove();
				}
				if (!_isMoving)
				{
					OnEndMove();
				}
			}
		}
	}

	public NetFields NetFields { get; } = new NetFields("Building");

	/// <summary>Construct an instance.</summary>
	public Building()
	{
		id.Value = Guid.NewGuid();
		resetTexture();
		initNetFields();
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="type">The building type ID in <see cref="F:StardewValley.Game1.buildingData" />.</param>
	/// <param name="tile">The top-left tile position of the building.</param>
	public Building(string type, Vector2 tile)
		: this()
	{
		tileX.Value = (int)tile.X;
		tileY.Value = (int)tile.Y;
		buildingType.Value = type;
		BuildingData buildingData = ReloadBuildingData();
		daysOfConstructionLeft.Value = buildingData?.BuildDays ?? 0;
	}

	/// <summary>Get whether the building has any skins that can be applied to it currently.</summary>
	/// <param name="ignoreSeparateConstructionEntries">Whether to ignore skins with <see cref="F:StardewValley.GameData.Buildings.BuildingSkin.ShowAsSeparateConstructionEntry" /> set to true.</param>
	public virtual bool CanBeReskinned(bool ignoreSeparateConstructionEntries = false)
	{
		BuildingData data = GetData();
		if (skinId.Value != null)
		{
			return true;
		}
		if (data?.Skins != null)
		{
			foreach (BuildingSkin skin in data.Skins)
			{
				if (!(skin.Id == skinId.Value) && (!ignoreSeparateConstructionEntries || !skin.ShowAsSeparateConstructionEntry) && GameStateQuery.CheckConditions(skin.Condition, GetParentLocation()))
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Get whether animals within this building can get pregnant and produce offspring.</summary>
	public bool AllowsAnimalPregnancy()
	{
		return GetData()?.AllowAnimalPregnancy ?? false;
	}

	/// <summary>Get whether players can repaint this building.</summary>
	public virtual bool CanBePainted()
	{
		if (this is GreenhouseBuilding && !Game1.getFarm().greenhouseUnlocked.Value)
		{
			return false;
		}
		if ((isCabin || HasIndoorsName("Farmhouse")) && GetIndoors() is FarmHouse { upgradeLevel: <2 })
		{
			return false;
		}
		return GetPaintDataKey() != null;
	}

	/// <summary>Get the building's current skin, if applicable.</summary>
	public BuildingSkin GetSkin()
	{
		return GetSkin(skinId.Value, GetData());
	}

	/// <summary>Get a building skin from data, if it exists.</summary>
	/// <param name="skinId">The building skin ID to find.</param>
	/// <param name="data">The building data to search.</param>
	/// <returns>Returns the matching building skin if found, else <c>null</c>.</returns>
	public static BuildingSkin GetSkin(string skinId, BuildingData data)
	{
		if (skinId != null && data?.Skins != null)
		{
			foreach (BuildingSkin skin in data.Skins)
			{
				if (skin.Id == skinId)
				{
					return skin;
				}
			}
		}
		return null;
	}

	/// <summary>Get the key in <c>Data/PaintData</c> for the building, if it has any.</summary>
	public virtual string GetPaintDataKey()
	{
		Dictionary<string, string> paintData = DataLoader.PaintData(Game1.content);
		return GetPaintDataKey(paintData);
	}

	/// <summary>Get the key in <c>Data/PaintData</c> for the building, if it has any.</summary>
	/// <param name="paintData">The loaded <c>Data/PaintData</c> asset.</param>
	public virtual string GetPaintDataKey(Dictionary<string, string> paintData)
	{
		if (skinId.Value != null && paintData.ContainsKey(skinId.Value))
		{
			return skinId.Value;
		}
		string value = buildingType.Value;
		string text = ((value == "Farmhouse") ? "House" : ((!(value == "Cabin")) ? buildingType.Value : "Stone Cabin"));
		if (!paintData.ContainsKey(text))
		{
			return null;
		}
		return text;
	}

	public string GetMetadata(string key)
	{
		if (buildingMetadata == null)
		{
			buildingMetadata = new Dictionary<string, string>();
			BuildingData data = GetData();
			if (data != null)
			{
				foreach (KeyValuePair<string, string> metadatum in data.Metadata)
				{
					buildingMetadata[metadatum.Key] = metadatum.Value;
				}
				BuildingSkin skin = GetSkin(skinId.Value, data);
				if (skin != null)
				{
					foreach (KeyValuePair<string, string> metadatum2 in skin.Metadata)
					{
						buildingMetadata[metadatum2.Key] = metadatum2.Value;
					}
				}
			}
		}
		if (!buildingMetadata.TryGetValue(key, out key))
		{
			return null;
		}
		return key;
	}

	/// <summary>Get the location which contains this building.</summary>
	public GameLocation GetParentLocation()
	{
		return Game1.getLocationFromName(parentLocationName.Value);
	}

	/// <summary>Get whether the building is in <see cref="P:StardewValley.Game1.currentLocation" />.</summary>
	public bool IsInCurrentLocation()
	{
		if (Game1.currentLocation != null)
		{
			return Game1.currentLocation.NameOrUniqueName == parentLocationName.Value;
		}
		return false;
	}

	public virtual bool hasCarpenterPermissions()
	{
		if (Game1.IsMasterGame)
		{
			return true;
		}
		if (owner.Value == Game1.player.UniqueMultiplayerID)
		{
			return true;
		}
		if (GetIndoors() is FarmHouse { IsOwnedByCurrentPlayer: not false })
		{
			return true;
		}
		return false;
	}

	protected virtual void initNetFields()
	{
		NetFields.SetOwner(this).AddField(id, "id").AddField(indoors, "indoors")
			.AddField(nonInstancedIndoorsName, "nonInstancedIndoorsName")
			.AddField(tileX, "tileX")
			.AddField(tileY, "tileY")
			.AddField(tilesWide, "tilesWide")
			.AddField(tilesHigh, "tilesHigh")
			.AddField(maxOccupants, "maxOccupants")
			.AddField(currentOccupants, "currentOccupants")
			.AddField(daysOfConstructionLeft, "daysOfConstructionLeft")
			.AddField(daysUntilUpgrade, "daysUntilUpgrade")
			.AddField(buildingType, "buildingType")
			.AddField(humanDoor, "humanDoor")
			.AddField(animalDoor, "animalDoor")
			.AddField(magical, "magical")
			.AddField(fadeWhenPlayerIsBehind, "fadeWhenPlayerIsBehind")
			.AddField(animalDoorOpen, "animalDoorOpen")
			.AddField(owner, "owner")
			.AddField(newConstructionTimer, "newConstructionTimer")
			.AddField(netBuildingPaintColor, "netBuildingPaintColor")
			.AddField(buildingChests, "buildingChests")
			.AddField(animalDoorOpenAmount, "animalDoorOpenAmount")
			.AddField(hayCapacity, "hayCapacity")
			.AddField(parentLocationName, "parentLocationName")
			.AddField(upgradeName, "upgradeName")
			.AddField(skinId, "skinId")
			.AddField(modData, "modData");
		buildingType.fieldChangeVisibleEvent += delegate(NetString a, string b, string c)
		{
			hasChimney = null;
			bool forUpgrade = b != null && b != c;
			ReloadBuildingData(forUpgrade);
		};
		skinId.fieldChangeVisibleEvent += delegate
		{
			hasChimney = null;
			buildingMetadata = null;
			resetTexture();
		};
		buildingType.fieldChangeVisibleEvent += delegate
		{
			hasChimney = null;
			buildingMetadata = null;
			resetTexture();
		};
		indoors.fieldChangeVisibleEvent += delegate
		{
			UpdateIndoorParent();
		};
		parentLocationName.fieldChangeVisibleEvent += delegate
		{
			UpdateIndoorParent();
		};
		if (netBuildingPaintColor.Value == null)
		{
			netBuildingPaintColor.Value = new BuildingPaintColor();
		}
	}

	public virtual void UpdateIndoorParent()
	{
		GameLocation gameLocation = GetIndoors();
		if (gameLocation != null)
		{
			gameLocation.ParentBuilding = this;
			gameLocation.parentLocationName.Value = parentLocationName.Value;
		}
	}

	/// <summary>Get the building's data from <see cref="F:StardewValley.Game1.buildingData" />, if found.</summary>
	public virtual BuildingData GetData()
	{
		if (!TryGetData(buildingType.Value, out var data))
		{
			return null;
		}
		return data;
	}

	/// <summary>Try to get a building's data from <see cref="F:StardewValley.Game1.buildingData" />.</summary>
	/// <param name="buildingType">The building type (i.e. the key in <see cref="F:StardewValley.Game1.buildingData" />).</param>
	/// <param name="data">The building data, if found.</param>
	/// <returns>Returns whether the building data was found.</returns>
	public static bool TryGetData(string buildingType, out BuildingData data)
	{
		if (buildingType == null)
		{
			data = null;
			return false;
		}
		return Game1.buildingData.TryGetValue(buildingType, out data);
	}

	/// <summary>Reload the building's data from <see cref="F:StardewValley.Game1.buildingData" /> and reapply it to the building's fields.</summary>
	/// <param name="forUpgrade">Whether the building is being upgraded.</param>
	/// <param name="forConstruction">Whether the building is being constructed.</param>
	/// <returns>Returns the loaded building data, if any.</returns>
	/// <remarks>See also <see cref="M:StardewValley.Buildings.Building.LoadFromBuildingData(StardewValley.GameData.Buildings.BuildingData,System.Boolean,System.Boolean)" />.</remarks>
	public virtual BuildingData ReloadBuildingData(bool forUpgrade = false, bool forConstruction = false)
	{
		BuildingData data = GetData();
		if (data != null)
		{
			LoadFromBuildingData(data, forUpgrade, forConstruction);
		}
		return data;
	}

	/// <summary>Reapply the loaded data to the building's fields.</summary>
	/// <param name="data">The building data to load.</param>
	/// <param name="forUpgrade">Whether the building is being upgraded.</param>
	/// <param name="forConstruction">Whether the building is being constructed.</param>
	/// <remarks>This doesn't reload the underlying data; see <see cref="M:StardewValley.Buildings.Building.ReloadBuildingData(System.Boolean,System.Boolean)" /> if you need to do that.</remarks>
	public virtual void LoadFromBuildingData(BuildingData data, bool forUpgrade = false, bool forConstruction = false)
	{
		if (data == null)
		{
			return;
		}
		tilesWide.Value = data.Size.X;
		tilesHigh.Value = data.Size.Y;
		humanDoor.X = data.HumanDoor.X;
		humanDoor.Y = data.HumanDoor.Y;
		animalDoor.Value = data.AnimalDoor.Location;
		if (data.MaxOccupants >= 0)
		{
			maxOccupants.Value = data.MaxOccupants;
		}
		hayCapacity.Value = data.HayCapacity;
		magical.Value = data.Builder == "Wizard";
		fadeWhenPlayerIsBehind.Value = data.FadeWhenBehind;
		foreach (KeyValuePair<string, string> modDatum in data.ModData)
		{
			modData[modDatum.Key] = modDatum.Value;
		}
		GetIndoors()?.InvalidateCachedMultiplayerMap(Game1.multiplayer.cachedMultiplayerMaps);
		if (!Game1.IsMasterGame)
		{
			return;
		}
		if (hasLoaded || forConstruction)
		{
			if (nonInstancedIndoorsName.Value == null)
			{
				string indoorMap = data.IndoorMap;
				string text = typeof(GameLocation).ToString();
				if (data.IndoorMapType != null)
				{
					text = data.IndoorMapType;
				}
				if (indoorMap != null)
				{
					indoorMap = "Maps\\" + indoorMap;
					if (indoors.Value == null)
					{
						indoors.Value = createIndoors(data, data.IndoorMap);
						InitializeIndoor(data, forConstruction, forUpgrade);
					}
					else if (indoors.Value.mapPath.Value == indoorMap)
					{
						if (forUpgrade)
						{
							InitializeIndoor(data, forConstruction, forUpgrade: true);
						}
					}
					else
					{
						if (indoors.Value.GetType().ToString() != text)
						{
							load();
						}
						else
						{
							indoors.Value.mapPath.Value = indoorMap;
							indoors.Value.updateMap();
						}
						updateInteriorWarps(indoors.Value);
						InitializeIndoor(data, forConstruction, forUpgrade);
					}
				}
			}
			else
			{
				updateInteriorWarps();
			}
		}
		if (!(hasLoaded || forConstruction))
		{
			return;
		}
		HashSet<string> validChests = new HashSet<string>();
		if (data.Chests != null)
		{
			foreach (BuildingChest chest in data.Chests)
			{
				validChests.Add(chest.Id);
			}
		}
		buildingChests.RemoveWhere((Chest chest) => !validChests.Contains(chest.Name));
		if (data.Chests == null)
		{
			return;
		}
		foreach (BuildingChest chest2 in data.Chests)
		{
			if (GetBuildingChest(chest2.Id) == null)
			{
				Chest item = new Chest(playerChest: true)
				{
					Name = chest2.Id
				};
				buildingChests.Add(item);
			}
		}
	}

	/// <summary>Create a building instance from its type ID.</summary>
	/// <param name="typeId">The building type ID in <c>Data/Buildings</c>.</param>
	/// <param name="tile">The top-left tile position of the building.</param>
	public static Building CreateInstanceFromId(string typeId, Vector2 tile)
	{
		if (typeId != null && Game1.buildingData.TryGetValue(typeId, out var value))
		{
			Type type = ((value.BuildingType != null) ? Type.GetType(value.BuildingType) : null);
			if (type != null && type != typeof(Building))
			{
				try
				{
					return (Building)Activator.CreateInstance(type, typeId, tile);
				}
				catch (MissingMethodException)
				{
					try
					{
						Building obj = (Building)Activator.CreateInstance(type, tile);
						obj.buildingType.Value = typeId;
						return obj;
					}
					catch (Exception exception)
					{
						Game1.log.Error("Error trying to instantiate building for type '" + typeId + "'", exception);
					}
				}
			}
		}
		return new Building(typeId, tile);
	}

	public virtual void InitializeIndoor(BuildingData data, bool forConstruction, bool forUpgrade)
	{
		if (data == null)
		{
			return;
		}
		GameLocation gameLocation = GetIndoors();
		if (gameLocation == null)
		{
			return;
		}
		if (gameLocation is AnimalHouse animalHouse && data.MaxOccupants > 0)
		{
			animalHouse.animalLimit.Value = data.MaxOccupants;
		}
		if (forUpgrade && data.IndoorItemMoves != null)
		{
			foreach (IndoorItemMove indoorItemMove in data.IndoorItemMoves)
			{
				for (int i = 0; i < indoorItemMove.Size.X; i++)
				{
					for (int j = 0; j < indoorItemMove.Size.Y; j++)
					{
						gameLocation.moveContents(indoorItemMove.Source.X + i, indoorItemMove.Source.Y + j, indoorItemMove.Destination.X + i, indoorItemMove.Destination.Y + j, indoorItemMove.UnlessItemId);
					}
				}
			}
		}
		if (!(forConstruction || forUpgrade) || data.IndoorItems == null)
		{
			return;
		}
		foreach (IndoorItemAdd indoorItem in data.IndoorItems)
		{
			Vector2 vector = Utility.PointToVector2(indoorItem.Tile);
			Object obj = ItemRegistry.Create(indoorItem.ItemId) as Object;
			Furniture furniture = obj as Furniture;
			if (obj == null)
			{
				continue;
			}
			if (indoorItem.ClearTile)
			{
				if (furniture != null)
				{
					int k = 0;
					for (int num = furniture.getTilesHigh(); k < num; k++)
					{
						int l = 0;
						for (int num2 = furniture.getTilesWide(); l < num2; l++)
						{
							gameLocation.cleanUpTileForMapOverride(new Point((int)vector.X + l, (int)vector.Y + k), indoorItem.ItemId);
						}
					}
				}
				else
				{
					gameLocation.cleanUpTileForMapOverride(Utility.Vector2ToPoint(vector), indoorItem.ItemId);
				}
			}
			if (!gameLocation.IsTileBlockedBy(vector, CollisionMask.Furniture | CollisionMask.Objects))
			{
				if (indoorItem.Indestructible)
				{
					obj.fragility.Value = 2;
				}
				obj.TileLocation = vector;
				if (furniture != null)
				{
					gameLocation.furniture.Add(furniture);
				}
				else
				{
					gameLocation.objects.Add(vector, obj);
				}
			}
		}
	}

	public BuildingItemConversion GetItemConversionForItem(Item item, Chest chest)
	{
		if (item == null || chest == null)
		{
			return null;
		}
		BuildingData data = GetData();
		if (data?.ItemConversions != null)
		{
			foreach (BuildingItemConversion itemConversion in data.ItemConversions)
			{
				if (!(itemConversion.SourceChest == chest.Name))
				{
					continue;
				}
				bool flag = false;
				foreach (string requiredTag in itemConversion.RequiredTags)
				{
					if (!item.HasContextTag(requiredTag))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return itemConversion;
				}
			}
		}
		return null;
	}

	public bool IsValidObjectForChest(Item item, Chest chest)
	{
		return GetItemConversionForItem(item, chest) != null;
	}

	public bool PerformBuildingChestAction(string name, Farmer who)
	{
		Chest chest = GetBuildingChest(name);
		if (chest == null)
		{
			return false;
		}
		BuildingChest buildingChestData = GetBuildingChestData(name);
		if (buildingChestData == null)
		{
			return false;
		}
		switch (buildingChestData.Type)
		{
		case BuildingChestType.Chest:
			((MenuWithInventory)(Game1.activeClickableMenu = new ItemGrabMenu(chest.Items, reverseGrab: false, showReceivingMenu: true, (Item item) => IsValidObjectForChest(item, chest), chest.grabItemFromInventory, null, chest.grabItemFromChest, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: true, 1, null, -1, this))).inventory.moveItemSound = buildingChestData.Sound;
			return true;
		case BuildingChestType.Load:
			if (who?.ActiveObject != null)
			{
				if (!IsValidObjectForChest(who.ActiveObject, chest))
				{
					if (buildingChestData.InvalidItemMessage != null && (buildingChestData.InvalidItemMessageCondition == null || GameStateQuery.CheckConditions(buildingChestData.InvalidItemMessageCondition, GetParentLocation(), who, who.ActiveObject, who.ActiveObject)))
					{
						Game1.showRedMessage(TokenParser.ParseText(buildingChestData.InvalidItemMessage));
					}
					return false;
				}
				BuildingItemConversion itemConversionForItem = GetItemConversionForItem(who.ActiveObject, chest);
				Utility.consolidateStacks(chest.Items);
				chest.clearNulls();
				int numberOfItemThatCanBeAddedToThisInventoryList = Utility.GetNumberOfItemThatCanBeAddedToThisInventoryList(who.ActiveObject, chest.Items, 36);
				if (who.ActiveObject.Stack > itemConversionForItem.RequiredCount && numberOfItemThatCanBeAddedToThisInventoryList < itemConversionForItem.RequiredCount)
				{
					Game1.showRedMessage(TokenParser.ParseText(buildingChestData.ChestFullMessage));
					return false;
				}
				int num = Math.Min(numberOfItemThatCanBeAddedToThisInventoryList, who.ActiveObject.Stack) / itemConversionForItem.RequiredCount * itemConversionForItem.RequiredCount;
				if (num == 0)
				{
					if (buildingChestData.InvalidCountMessage != null)
					{
						Game1.showRedMessage(TokenParser.ParseText(buildingChestData.InvalidCountMessage));
					}
					return false;
				}
				Item one = who.ActiveObject.getOne();
				if (who.ActiveObject.ConsumeStack(num) == null)
				{
					who.ActiveObject = null;
				}
				one.Stack = num;
				Utility.addItemToThisInventoryList(one, chest.Items, 36);
				if (buildingChestData.Sound != null)
				{
					Game1.playSound(buildingChestData.Sound);
				}
			}
			return true;
		case BuildingChestType.Collect:
			Utility.CollectSingleItemOrShowChestMenu(chest);
			return true;
		default:
			return false;
		}
	}

	public BuildingChest GetBuildingChestData(string name)
	{
		return GetBuildingChestData(GetData(), name);
	}

	public static BuildingChest GetBuildingChestData(BuildingData data, string name)
	{
		if (data == null)
		{
			return null;
		}
		foreach (BuildingChest chest in data.Chests)
		{
			if (chest.Id == name)
			{
				return chest;
			}
		}
		return null;
	}

	public Chest GetBuildingChest(string name)
	{
		foreach (Chest buildingChest in buildingChests)
		{
			if (buildingChest.Name == name)
			{
				return buildingChest;
			}
		}
		return null;
	}

	public virtual string textureName()
	{
		BuildingData data = GetData();
		return GetSkin(skinId.Value, data)?.Texture ?? data?.Texture ?? ("Buildings\\" + buildingType.Value);
	}

	public virtual void resetTexture()
	{
		texture = new Lazy<Texture2D>(delegate
		{
			if (paintedTexture != null)
			{
				paintedTexture.Dispose();
				paintedTexture = null;
			}
			string text = textureName();
			Texture2D texture2D;
			try
			{
				texture2D = Game1.content.Load<Texture2D>(text);
			}
			catch
			{
				return Game1.content.Load<Texture2D>("Buildings\\Error");
			}
			paintedTexture = BuildingPainter.Apply(texture2D, text + "_PaintMask", netBuildingPaintColor.Value);
			if (paintedTexture != null)
			{
				texture2D = paintedTexture;
			}
			return texture2D;
		});
	}

	public int getTileSheetIndexForStructurePlacementTile(int x, int y)
	{
		if (x == humanDoor.X && y == humanDoor.Y)
		{
			return 2;
		}
		if (x == animalDoor.X && y == animalDoor.Y)
		{
			return 4;
		}
		return 0;
	}

	public virtual void performTenMinuteAction(int timeElapsed)
	{
	}

	public virtual void resetLocalState()
	{
		alpha = 1f;
		color = Color.White;
		isMoving = false;
	}

	public virtual bool CanLeftClick(int x, int y)
	{
		Microsoft.Xna.Framework.Rectangle boundingBox = new Microsoft.Xna.Framework.Rectangle(x, y, 1, 1);
		return intersects(boundingBox);
	}

	public virtual bool leftClicked()
	{
		return false;
	}

	public virtual void ToggleAnimalDoor(Farmer who)
	{
		BuildingData data = GetData();
		string text = ((!animalDoorOpen.Value) ? data?.AnimalDoorCloseSound : data?.AnimalDoorOpenSound);
		if (text != null)
		{
			who.currentLocation.playSound(text);
		}
		animalDoorOpen.Value = !animalDoorOpen.Value;
	}

	public virtual bool OnUseHumanDoor(Farmer who)
	{
		return true;
	}

	public virtual bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (who.isRidingHorse())
		{
			return false;
		}
		if (who.IsLocalPlayer && occupiesTile(tileLocation) && daysOfConstructionLeft.Value > 0)
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:UnderConstruction"));
		}
		else
		{
			if (who.ActiveObject != null && who.ActiveObject.IsFloorPathItem() && who.currentLocation != null && !who.currentLocation.terrainFeatures.ContainsKey(tileLocation))
			{
				return false;
			}
			GameLocation gameLocation = GetIndoors();
			if (who.IsLocalPlayer && tileLocation.X == (float)(humanDoor.X + tileX.Value) && tileLocation.Y == (float)(humanDoor.Y + tileY.Value) && gameLocation != null)
			{
				if (who.mount != null)
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\Buildings:DismountBeforeEntering"));
					return false;
				}
				if (who.team.demolishLock.IsLocked())
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\Buildings:CantEnter"));
					return false;
				}
				if (OnUseHumanDoor(who))
				{
					who.currentLocation.playSound("doorClose", tileLocation);
					bool isStructure = indoors.Value != null;
					Game1.warpFarmer(gameLocation.NameOrUniqueName, gameLocation.warps[0].X, gameLocation.warps[0].Y - 1, Game1.player.FacingDirection, isStructure);
				}
				return true;
			}
			BuildingData data = GetData();
			if (data != null)
			{
				Microsoft.Xna.Framework.Rectangle rectForAnimalDoor = getRectForAnimalDoor(data);
				rectForAnimalDoor.Width /= 64;
				rectForAnimalDoor.Height /= 64;
				rectForAnimalDoor.X /= 64;
				rectForAnimalDoor.Y /= 64;
				if (daysOfConstructionLeft.Value <= 0 && rectForAnimalDoor != Microsoft.Xna.Framework.Rectangle.Empty && rectForAnimalDoor.Contains(Utility.Vector2ToPoint(tileLocation)) && Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
				{
					ToggleAnimalDoor(who);
					return true;
				}
				if (who.IsLocalPlayer && occupiesTile(tileLocation, applyTilePropertyRadius: true) && !isTilePassable(tileLocation))
				{
					string actionAtTile = data.GetActionAtTile((int)tileLocation.X - tileX.Value, (int)tileLocation.Y - tileY.Value);
					if (actionAtTile != null)
					{
						actionAtTile = TokenParser.ParseText(actionAtTile);
						if (who.currentLocation.performAction(actionAtTile, who, new Location((int)tileLocation.X, (int)tileLocation.Y)))
						{
							return true;
						}
					}
				}
			}
			else if (who.IsLocalPlayer)
			{
				if (!isTilePassable(tileLocation) && TryPerformObeliskWarp(buildingType.Value, who))
				{
					return true;
				}
				if (who.ActiveObject != null && !isTilePassable(tileLocation))
				{
					return performActiveObjectDropInAction(who, probe: false);
				}
			}
		}
		return false;
	}

	public static bool TryPerformObeliskWarp(string buildingType, Farmer who)
	{
		switch (buildingType)
		{
		case "Desert Obelisk":
			PerformObeliskWarp("Desert", 35, 43, force_dismount: true, who);
			return true;
		case "Water Obelisk":
			PerformObeliskWarp("Beach", 20, 4, force_dismount: false, who);
			return true;
		case "Earth Obelisk":
			PerformObeliskWarp("Mountain", 31, 20, force_dismount: false, who);
			return true;
		case "Island Obelisk":
			PerformObeliskWarp("IslandSouth", 11, 11, force_dismount: false, who);
			return true;
		default:
			return false;
		}
	}

	public static void PerformObeliskWarp(string destination, int warp_x, int warp_y, bool force_dismount, Farmer who)
	{
		if (force_dismount && who.isRidingHorse() && who.mount != null)
		{
			who.mount.checkAction(who, who.currentLocation);
			return;
		}
		for (int i = 0; i < 12; i++)
		{
			who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(354, Game1.random.Next(25, 75), 6, 1, new Vector2(Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192), Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)), flicker: false, Game1.random.NextBool()));
		}
		who.currentLocation.playSound("wand");
		Game1.displayFarmer = false;
		Game1.player.temporarilyInvincible = true;
		Game1.player.temporaryInvincibilityTimer = -2000;
		Game1.player.freezePause = 1000;
		Game1.flashAlpha = 1f;
		Microsoft.Xna.Framework.Rectangle boundingBox = who.GetBoundingBox();
		DelayedAction.fadeAfterDelay(delegate
		{
			obeliskWarpForReal(destination, warp_x, warp_y, who);
		}, 1000);
		new Microsoft.Xna.Framework.Rectangle(boundingBox.X, boundingBox.Y, 64, 64).Inflate(192, 192);
		int num = 0;
		Point tilePoint = who.TilePoint;
		for (int num2 = tilePoint.X + 8; num2 >= tilePoint.X - 8; num2--)
		{
			who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(num2, tilePoint.Y) * 64f, Color.White, 8, flipped: false, 50f)
			{
				layerDepth = 1f,
				delayBeforeAnimationStart = num * 25,
				motion = new Vector2(-0.25f, 0f)
			});
			num++;
		}
	}

	private static void obeliskWarpForReal(string destination, int warp_x, int warp_y, Farmer who)
	{
		Game1.warpFarmer(destination, warp_x, warp_y, flip: false);
		Game1.fadeToBlackAlpha = 0.99f;
		Game1.screenGlow = false;
		Game1.player.temporarilyInvincible = false;
		Game1.player.temporaryInvincibilityTimer = 0;
		Game1.displayFarmer = true;
	}

	public virtual bool isActionableTile(int xTile, int yTile, Farmer who)
	{
		BuildingData data = GetData();
		if (data != null)
		{
			Vector2 tile = new Vector2(xTile, yTile);
			if (occupiesTile(tile, applyTilePropertyRadius: true) && !isTilePassable(tile) && data.GetActionAtTile(xTile - tileX.Value, yTile - tileY.Value) != null)
			{
				return true;
			}
		}
		if (humanDoor.X >= 0 && xTile == tileX.Value + humanDoor.X && yTile == tileY.Value + humanDoor.Y)
		{
			return true;
		}
		Microsoft.Xna.Framework.Rectangle rectForAnimalDoor = getRectForAnimalDoor(data);
		rectForAnimalDoor.Width /= 64;
		rectForAnimalDoor.Height /= 64;
		rectForAnimalDoor.X /= 64;
		rectForAnimalDoor.Y /= 64;
		if (rectForAnimalDoor != Microsoft.Xna.Framework.Rectangle.Empty)
		{
			return rectForAnimalDoor.Contains(new Point(xTile, yTile));
		}
		return false;
	}

	/// <summary>Handle the building being moved within its location by any player.</summary>
	public virtual void performActionOnBuildingPlacement()
	{
		GameLocation parentLocation = GetParentLocation();
		if (parentLocation == null)
		{
			return;
		}
		for (int i = 0; i < tilesHigh.Value; i++)
		{
			for (int j = 0; j < tilesWide.Value; j++)
			{
				Vector2 key = new Vector2(tileX.Value + j, tileY.Value + i);
				if (!(parentLocation.terrainFeatures.GetValueOrDefault(key) is Flooring) || !(GetData()?.AllowsFlooringUnderneath ?? false))
				{
					parentLocation.terrainFeatures.Remove(key);
				}
			}
		}
		foreach (BuildingPlacementTile additionalPlacementTile in GetAdditionalPlacementTiles())
		{
			bool onlyNeedsToBePassable = additionalPlacementTile.OnlyNeedsToBePassable;
			foreach (Point point in additionalPlacementTile.TileArea.GetPoints())
			{
				Vector2 key2 = new Vector2(tileX.Value + point.X, tileY.Value + point.Y);
				if ((!onlyNeedsToBePassable || (parentLocation.terrainFeatures.TryGetValue(key2, out var value) && !value.isPassable())) && (!(parentLocation.terrainFeatures.GetValueOrDefault(key2) is Flooring) || !(GetData()?.AllowsFlooringUnderneath ?? false)))
				{
					parentLocation.terrainFeatures.Remove(key2);
				}
			}
		}
	}

	/// <summary>Handle the building being constructed.</summary>
	/// <param name="location">The location containing the building.</param>
	/// <param name="who">The player that constructed the building.</param>
	public virtual void performActionOnConstruction(GameLocation location, Farmer who)
	{
		BuildingData data = GetData();
		LoadFromBuildingData(data, forUpgrade: false, forConstruction: true);
		Vector2 value = new Vector2((float)tileX.Value + (float)tilesWide.Value * 0.5f, (float)tileY.Value + (float)tilesHigh.Value * 0.5f);
		location.localSound("axchop", value);
		newConstructionTimer.Value = ((magical.Value || daysOfConstructionLeft.Value <= 0) ? 2000 : 1000);
		if (data?.AddMailOnBuild != null)
		{
			foreach (string item in data.AddMailOnBuild)
			{
				Game1.addMail(item, noLetter: false, sendToEveryone: true);
			}
		}
		if (!magical.Value)
		{
			location.localSound("axchop", value);
			for (int i = tileX.Value; i < tileX.Value + tilesWide.Value; i++)
			{
				for (int j = tileY.Value; j < tileY.Value + tilesHigh.Value; j++)
				{
					for (int k = 0; k < 5; k++)
					{
						location.temporarySprites.Add(new TemporaryAnimatedSprite(Game1.random.Choose(46, 12), new Vector2(i, j) * 64f + new Vector2(Game1.random.Next(-16, 32), Game1.random.Next(-16, 32)), Color.White, 10, Game1.random.NextBool())
						{
							delayBeforeAnimationStart = Math.Max(0, Game1.random.Next(-200, 400)),
							motion = new Vector2(0f, -1f),
							interval = Game1.random.Next(50, 80)
						});
					}
					location.temporarySprites.Add(new TemporaryAnimatedSprite(14, new Vector2(i, j) * 64f + new Vector2(Game1.random.Next(-16, 32), Game1.random.Next(-16, 32)), Color.White, 10, Game1.random.NextBool()));
				}
			}
			for (int l = 0; l < 8; l++)
			{
				DelayedAction.playSoundAfterDelay("dirtyHit", 250 + l * 150, location, value, -1, local: true);
			}
		}
		else
		{
			for (int m = 0; m < 8; m++)
			{
				DelayedAction.playSoundAfterDelay("dirtyHit", 100 + m * 210, location, value, -1, local: true);
			}
			if (Game1.player == who)
			{
				Game1.flashAlpha = 2f;
			}
			location.localSound("wand", value);
			Microsoft.Xna.Framework.Rectangle sourceRect = getSourceRect();
			Microsoft.Xna.Framework.Rectangle rectangle = getSourceRectForMenu() ?? sourceRect;
			int n = 0;
			for (int num = sourceRect.Height / 16 * 2; n <= num; n++)
			{
				int num2 = 0;
				for (int num3 = rectangle.Width / 16 * 2; num2 < num3; num2++)
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(666, 1851, 8, 8), 40f, 4, 2, new Vector2(tileX.Value, tileY.Value) * 64f + new Vector2(num2 * 64 / 2, n * 64 / 2 - sourceRect.Height * 4 + tilesHigh.Value * 64) + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)), flicker: false, flipped: false)
					{
						layerDepth = (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + (float)num2 / 10000f,
						pingPong = true,
						delayBeforeAnimationStart = (sourceRect.Height / 16 * 2 - n) * 100,
						scale = 4f,
						alphaFade = 0.01f,
						color = Color.AliceBlue
					});
					location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(666, 1851, 8, 8), 40f, 4, 2, new Vector2(tileX.Value, tileY.Value) * 64f + new Vector2(num2 * 64 / 2, n * 64 / 2 - sourceRect.Height * 4 + tilesHigh.Value * 64) + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)), flicker: false, flipped: false)
					{
						layerDepth = (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + (float)num2 / 10000f + 0.0001f,
						pingPong = true,
						delayBeforeAnimationStart = (sourceRect.Height / 16 * 2 - n) * 100,
						scale = 4f,
						alphaFade = 0.01f,
						color = Color.AliceBlue
					});
				}
			}
		}
		if (GetIndoors() is Cabin { HasOwner: false } cabin)
		{
			cabin.CreateFarmhand();
			if (Game1.IsMasterGame)
			{
				hasLoaded = true;
			}
		}
	}

	/// <summary>Handle the building being demolished.</summary>
	/// <param name="location">The location which previously contained the building.</param>
	public virtual void performActionOnDemolition(GameLocation location)
	{
		if (GetIndoors() is Cabin cabin)
		{
			cabin.DeleteFarmhand();
		}
		if (indoors.Value != null)
		{
			Game1.multiplayer.broadcastRemoveLocationFromLookup(indoors.Value);
			indoors.Value.OnRemoved();
			indoors.Value = null;
		}
	}

	/// <summary>Perform an action for each item within the building instance, excluding those in the interior location.</summary>
	/// <param name="action">The action to perform for each item.  This should return true (continue iterating) or false (stop).</param>
	/// <returns>Returns whether to continue iterating.</returns>
	/// <remarks>For items in the interior location, use <see cref="M:StardewValley.Utility.ForEachItemIn(StardewValley.GameLocation,System.Func{StardewValley.Item,System.Boolean})" /> instead. See also <see cref="M:StardewValley.Buildings.Building.ForEachItemContextExcludingInterior(StardewValley.Delegates.ForEachItemDelegate,StardewValley.Delegates.GetForEachItemPathDelegate)" /> for more advanced scenarios like replacing items.</remarks>
	public virtual bool ForEachItemExcludingInterior(Func<Item, bool> action)
	{
		return ForEachItemContextExcludingInterior(Handle, GetParentPath);
		IList<object> GetParentPath()
		{
			return new List<object> { GetParentLocation() };
		}
		bool Handle(in ForEachItemContext context)
		{
			return action(context.Item);
		}
	}

	/// <summary>Perform an action for each item within the building instance, excluding those in the interior location.</summary>
	/// <param name="handler">The action to perform for each item.</param>
	/// <param name="getParentPath">The contextual path leading to this building (excluding the building itself), or <c>null</c> to treat this as the root.</param>
	/// <returns>Returns whether to continue iterating.</returns>
	/// <remarks>For items in the interior location, use <see cref="M:StardewValley.Utility.ForEachItemIn(StardewValley.GameLocation,System.Func{StardewValley.Item,System.Boolean})" /> instead. See also <see cref="M:StardewValley.Buildings.Building.ForEachItemExcludingInterior(System.Func{StardewValley.Item,System.Boolean})" /> if you only need to iterate items.</remarks>
	public virtual bool ForEachItemContextExcludingInterior(ForEachItemDelegate handler, GetForEachItemPathDelegate getParentPath)
	{
		foreach (Chest buildingChest in buildingChests)
		{
			Chest chest = buildingChest;
			if (!chest.ForEachItem(handler, GetPath))
			{
				return false;
			}
			IList<object> GetPath()
			{
				return ForEachItemHelper.CombinePath(getParentPath, this, buildingChests, chest);
			}
		}
		return true;
	}

	public virtual void BeforeDemolish()
	{
		List<Item> quest_items = new List<Item>();
		ForEachItemExcludingInterior(delegate(Item item)
		{
			CollectQuestItem(item);
			return true;
		});
		if (indoors.Value != null)
		{
			Utility.ForEachItemIn(indoors.Value, delegate(Item item)
			{
				CollectQuestItem(item);
				return true;
			});
			if (indoors.Value is Cabin cabin)
			{
				Cellar cellar = cabin.GetCellar();
				if (cellar != null)
				{
					Utility.ForEachItemIn(cellar, delegate(Item item)
					{
						CollectQuestItem(item);
						return true;
					});
				}
			}
		}
		if (quest_items.Count > 0)
		{
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:NewLostAndFoundItems"));
			for (int num = 0; num < quest_items.Count; num++)
			{
				Game1.player.team.returnedDonations.Add(quest_items[num]);
			}
		}
		void CollectQuestItem(Item item)
		{
			if (item is Object obj && obj.questItem.Value)
			{
				Item one = obj.getOne();
				one.Stack = obj.Stack;
				quest_items.Add(one);
			}
		}
	}

	public virtual void performActionOnUpgrade(GameLocation location)
	{
		if (location is Farm farm)
		{
			farm.UnsetFarmhouseValues();
		}
	}

	public virtual string isThereAnythingtoPreventConstruction(GameLocation location, Vector2 tile_location)
	{
		return null;
	}

	public virtual bool performActiveObjectDropInAction(Farmer who, bool probe)
	{
		return false;
	}

	public virtual void performToolAction(Tool t, int tileX, int tileY)
	{
	}

	public virtual void updateWhenFarmNotCurrentLocation(GameTime time)
	{
		if (indoors.Value != null && Game1.currentLocation != indoors.Value)
		{
			indoors.Value.netAudio.Update();
		}
		netBuildingPaintColor.Value?.Poll(resetTexture);
		if (newConstructionTimer.Value > 0)
		{
			newConstructionTimer.Value -= time.ElapsedGameTime.Milliseconds;
			if (newConstructionTimer.Value <= 0 && magical.Value)
			{
				daysOfConstructionLeft.Value = 0;
			}
		}
		if (!Game1.IsMasterGame)
		{
			return;
		}
		BuildingData data = GetData();
		if (data == null)
		{
			return;
		}
		if (animalDoorOpen.Value)
		{
			if (animalDoorOpenAmount.Value < 1f)
			{
				animalDoorOpenAmount.Value = ((data.AnimalDoorOpenDuration > 0f) ? Utility.MoveTowards(animalDoorOpenAmount.Value, 1f, (float)time.ElapsedGameTime.TotalSeconds / data.AnimalDoorOpenDuration) : 1f);
			}
		}
		else if (animalDoorOpenAmount.Value > 0f)
		{
			animalDoorOpenAmount.Value = ((data.AnimalDoorCloseDuration > 0f) ? Utility.MoveTowards(animalDoorOpenAmount.Value, 0f, (float)time.ElapsedGameTime.TotalSeconds / data.AnimalDoorCloseDuration) : 0f);
		}
	}

	public virtual void Update(GameTime time)
	{
		if (!hasLoaded && Game1.IsMasterGame && Game1.hasLoadedGame)
		{
			ReloadBuildingData(forUpgrade: false, forConstruction: true);
			load();
		}
		UpdateTransparency();
		if (isUnderConstruction())
		{
			return;
		}
		if (!hasChimney.HasValue)
		{
			string metadata = GetMetadata("ChimneyPosition");
			if (metadata != null)
			{
				hasChimney = true;
				string[] array = ArgUtility.SplitBySpace(metadata);
				chimneyPosition.X = int.Parse(array[0]);
				chimneyPosition.Y = int.Parse(array[1]);
			}
			else
			{
				hasChimney = false;
			}
		}
		GameLocation gameLocation = GetIndoors();
		if (gameLocation is FarmHouse { upgradeLevel: var upgradeLevel } && lastHouseUpgradeLevel != upgradeLevel)
		{
			lastHouseUpgradeLevel = upgradeLevel;
			string text = null;
			for (int i = 1; i <= lastHouseUpgradeLevel; i++)
			{
				string metadata2 = GetMetadata("ChimneyPosition" + (i + 1));
				if (metadata2 != null)
				{
					text = metadata2;
				}
			}
			if (text != null)
			{
				hasChimney = true;
				string[] array2 = ArgUtility.SplitBySpace(text);
				chimneyPosition.X = int.Parse(array2[0]);
				chimneyPosition.Y = int.Parse(array2[1]);
			}
		}
		if (hasChimney != true || gameLocation == null)
		{
			return;
		}
		chimneyTimer -= time.ElapsedGameTime.Milliseconds;
		if (chimneyTimer <= 0)
		{
			if (gameLocation.hasActiveFireplace())
			{
				GameLocation parentLocation = GetParentLocation();
				Microsoft.Xna.Framework.Rectangle sourceRect = getSourceRect();
				Vector2 vector = new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64 - sourceRect.Height * 4);
				BuildingData data = GetData();
				Vector2 vector2 = ((data != null) ? (data.DrawOffset * 4f) : Vector2.Zero);
				TemporaryAnimatedSprite temporaryAnimatedSprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Vector2(vector.X + vector2.X, vector.Y + vector2.Y) + chimneyPosition * 4f + new Vector2(-8f, -12f), flipped: false, 0.002f, Color.Gray);
				temporaryAnimatedSprite.alpha = 0.75f;
				temporaryAnimatedSprite.motion = new Vector2(0f, -0.5f);
				temporaryAnimatedSprite.acceleration = new Vector2(0.002f, 0f);
				temporaryAnimatedSprite.interval = 99999f;
				temporaryAnimatedSprite.layerDepth = 1f;
				temporaryAnimatedSprite.scale = 2f;
				temporaryAnimatedSprite.scaleChange = 0.02f;
				temporaryAnimatedSprite.rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f;
				parentLocation.temporarySprites.Add(temporaryAnimatedSprite);
			}
			chimneyTimer = 500;
		}
	}

	/// <summary>Update the building transparency on tick for the local player's position.</summary>
	public virtual void UpdateTransparency()
	{
		if (fadeWhenPlayerIsBehind.Value)
		{
			Microsoft.Xna.Framework.Rectangle rectangle = getSourceRectForMenu() ?? getSourceRect();
			Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(tileX.Value * 64, (tileY.Value + (-(rectangle.Height / 16) + tilesHigh.Value)) * 64, tilesWide.Value * 64, (rectangle.Height / 16 - tilesHigh.Value) * 64 + 32);
			if (Game1.player.GetBoundingBox().Intersects(value))
			{
				if (alpha > 0.4f)
				{
					alpha = Math.Max(0.4f, alpha - 0.04f);
				}
				return;
			}
		}
		if (alpha < 1f)
		{
			alpha = Math.Min(1f, alpha + 0.05f);
		}
	}

	public virtual void showUpgradeAnimation(GameLocation location)
	{
		color = Color.White;
		location.temporarySprites.Add(new TemporaryAnimatedSprite(46, getUpgradeSignLocation() + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-16, 16)), Color.Beige, 10, Game1.random.NextBool(), 75f)
		{
			motion = new Vector2(0f, -0.5f),
			acceleration = new Vector2(-0.02f, 0.01f),
			delayBeforeAnimationStart = Game1.random.Next(100),
			layerDepth = 0.89f
		});
		location.temporarySprites.Add(new TemporaryAnimatedSprite(46, getUpgradeSignLocation() + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-16, 16)), Color.Beige, 10, Game1.random.NextBool(), 75f)
		{
			motion = new Vector2(0f, -0.5f),
			acceleration = new Vector2(-0.02f, 0.01f),
			delayBeforeAnimationStart = Game1.random.Next(40),
			layerDepth = 0.89f
		});
	}

	public virtual Vector2 getUpgradeSignLocation()
	{
		BuildingData data = GetData();
		Vector2 vector = data?.UpgradeSignTile ?? new Vector2(0.5f, 0f);
		float num = data?.UpgradeSignHeight ?? 8f;
		return new Vector2(((float)tileX.Value + vector.X) * 64f, ((float)tileY.Value + vector.Y) * 64f - num * 4f);
	}

	public virtual void showDestroyedAnimation(GameLocation location)
	{
		for (int i = tileX.Value; i < tileX.Value + tilesWide.Value; i++)
		{
			for (int j = tileY.Value; j < tileY.Value + tilesHigh.Value; j++)
			{
				location.temporarySprites.Add(new TemporaryAnimatedSprite(362, Game1.random.Next(30, 90), 6, 1, new Vector2(i * 64, j * 64) + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-16, 16)), flicker: false, Game1.random.NextBool())
				{
					delayBeforeAnimationStart = Game1.random.Next(300)
				});
				location.temporarySprites.Add(new TemporaryAnimatedSprite(362, Game1.random.Next(30, 90), 6, 1, new Vector2(i * 64, j * 64) + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-16, 16)), flicker: false, Game1.random.NextBool())
				{
					delayBeforeAnimationStart = 250 + Game1.random.Next(300)
				});
				location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), new Vector2(i, j) * 64f + new Vector2(32f, -32f) + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-16, 16)), flipped: false, 0f, Color.White)
				{
					interval = 30f,
					totalNumberOfLoops = 99999,
					animationLength = 4,
					scale = 4f,
					alphaFade = 0.01f
				});
			}
		}
	}

	/// <summary>Instantly finish constructing or upgrading the building, if applicable.</summary>
	public void FinishConstruction(bool onGameStart = false)
	{
		bool flag = false;
		if (daysOfConstructionLeft.Value > 0)
		{
			Game1.player.team.constructedBuildings.Add(buildingType.Value);
			if (buildingType.Value == "Slime Hutch")
			{
				Game1.player.mailReceived.Add("slimeHutchBuilt");
			}
			daysOfConstructionLeft.Value = 0;
			flag = true;
		}
		if (daysUntilUpgrade.Value > 0)
		{
			string text = upgradeName.Value ?? "Well";
			Game1.player.team.constructedBuildings.Add(text);
			buildingType.Value = text;
			ReloadBuildingData(forUpgrade: true);
			daysUntilUpgrade.Value = 0;
			OnUpgraded();
			flag = true;
		}
		if (flag)
		{
			Game1.netWorldState.Value.UpdateUnderConstruction();
			resetTexture();
		}
		if (onGameStart)
		{
			return;
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			allFarmer.autoGenerateActiveDialogueEvent("structureBuilt_" + buildingType.Value);
		}
	}

	public virtual void dayUpdate(int dayOfMonth)
	{
		if (daysOfConstructionLeft.Value > 0 && !Utility.isFestivalDay(dayOfMonth, Game1.season) && (!Game1.isGreenRain || Game1.year > 1))
		{
			if (daysOfConstructionLeft.Value == 1)
			{
				FinishConstruction();
			}
			else
			{
				daysOfConstructionLeft.Value--;
			}
			return;
		}
		if (daysUntilUpgrade.Value > 0 && !Utility.isFestivalDay(dayOfMonth, Game1.season) && (!Game1.isGreenRain || Game1.year > 1))
		{
			if (daysUntilUpgrade.Value == 1)
			{
				FinishConstruction();
			}
			else
			{
				daysUntilUpgrade.Value--;
			}
		}
		GameLocation gameLocation = GetIndoors();
		if (gameLocation is AnimalHouse animalHouse)
		{
			currentOccupants.Value = animalHouse.animals.Length;
		}
		if (GetIndoorsType() == IndoorsType.Instanced)
		{
			gameLocation?.DayUpdate(dayOfMonth);
		}
		BuildingData data = GetData();
		if (data == null || !(data.ItemConversions?.Count > 0))
		{
			return;
		}
		ItemQueryContext itemQueryContext = new ItemQueryContext(GetParentLocation(), null, null, "building '" + buildingType.Value + "' > item conversion rules");
		foreach (BuildingItemConversion itemConversion in data.ItemConversions)
		{
			CheckItemConversionRule(itemConversion, itemQueryContext);
		}
	}

	public virtual void CheckItemConversionRule(BuildingItemConversion conversion, ItemQueryContext itemQueryContext)
	{
		int num = 0;
		int num2 = 0;
		Chest buildingChest = GetBuildingChest(conversion.SourceChest);
		Chest buildingChest2 = GetBuildingChest(conversion.DestinationChest);
		if (buildingChest == null)
		{
			return;
		}
		foreach (Item item4 in buildingChest.Items)
		{
			if (item4 == null)
			{
				continue;
			}
			bool flag = false;
			foreach (string requiredTag in conversion.RequiredTags)
			{
				if (!item4.HasContextTag(requiredTag))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				continue;
			}
			num2 += item4.Stack;
			if (num2 >= conversion.RequiredCount)
			{
				int num3 = num2 / conversion.RequiredCount;
				if (conversion.MaxDailyConversions >= 0)
				{
					num3 = Math.Min(num3, conversion.MaxDailyConversions - num);
				}
				num += num3;
				num2 -= num3 * conversion.RequiredCount;
			}
			if (conversion.MaxDailyConversions >= 0 && num >= conversion.MaxDailyConversions)
			{
				break;
			}
		}
		if (num == 0)
		{
			return;
		}
		int num4 = 0;
		for (int i = 0; i < num; i++)
		{
			bool flag2 = false;
			for (int j = 0; j < conversion.ProducedItems.Count; j++)
			{
				GenericSpawnItemDataWithCondition genericSpawnItemDataWithCondition = conversion.ProducedItems[j];
				if (GameStateQuery.CheckConditions(genericSpawnItemDataWithCondition.Condition, GetParentLocation()))
				{
					Item item = ItemQueryResolver.TryResolveRandomItem(genericSpawnItemDataWithCondition, itemQueryContext);
					int stack = item.Stack;
					Item item2 = buildingChest2.addItem(item);
					if (item2 == null || item2.Stack != stack)
					{
						flag2 = true;
					}
				}
			}
			if (flag2)
			{
				num4++;
			}
		}
		if (num4 <= 0)
		{
			return;
		}
		int num5 = num4 * conversion.RequiredCount;
		for (int k = 0; k < buildingChest.Items.Count; k++)
		{
			Item item3 = buildingChest.Items[k];
			if (item3 == null)
			{
				continue;
			}
			bool flag3 = false;
			foreach (string requiredTag2 in conversion.RequiredTags)
			{
				if (!item3.HasContextTag(requiredTag2))
				{
					flag3 = true;
					break;
				}
			}
			if (!flag3)
			{
				int num6 = Math.Min(num5, item3.Stack);
				buildingChest.Items[k] = item3.ConsumeStack(num6);
				num5 -= num6;
				if (num5 <= 0)
				{
					break;
				}
			}
		}
	}

	public virtual void OnUpgraded()
	{
		GetIndoors()?.OnParentBuildingUpgraded(this);
		BuildingData data = GetData();
		if (data?.AddMailOnBuild == null)
		{
			return;
		}
		foreach (string item in data.AddMailOnBuild)
		{
			Game1.addMail(item, noLetter: false, sendToEveryone: true);
		}
	}

	public virtual Microsoft.Xna.Framework.Rectangle getSourceRect()
	{
		BuildingData data = GetData();
		if (data != null)
		{
			Microsoft.Xna.Framework.Rectangle sourceRect = data.SourceRect;
			if (sourceRect == Microsoft.Xna.Framework.Rectangle.Empty)
			{
				return texture.Value.Bounds;
			}
			GameLocation gameLocation = GetIndoors();
			if (gameLocation is FarmHouse farmHouse)
			{
				if (gameLocation is Cabin)
				{
					sourceRect.X += sourceRect.Width * Math.Min(farmHouse.upgradeLevel, 2);
				}
				else
				{
					sourceRect.Y += sourceRect.Height * Math.Min(farmHouse.upgradeLevel, 2);
				}
			}
			sourceRect = ApplySourceRectOffsets(sourceRect);
			if (buildingType.Value == "Greenhouse" && GetParentLocation() is Farm farm && !farm.greenhouseUnlocked.Value)
			{
				sourceRect.Y -= sourceRect.Height;
			}
			return sourceRect;
		}
		if (isCabin)
		{
			return new Microsoft.Xna.Framework.Rectangle(((GetIndoors() is Cabin cabin) ? Math.Min(cabin.upgradeLevel, 2) : 0) * 80, 0, 80, 112);
		}
		return texture.Value.Bounds;
	}

	public virtual Microsoft.Xna.Framework.Rectangle ApplySourceRectOffsets(Microsoft.Xna.Framework.Rectangle source)
	{
		BuildingData data = GetData();
		if (data != null && data.SeasonOffset != Point.Zero)
		{
			int seasonIndexForLocation = Game1.GetSeasonIndexForLocation(GetParentLocation());
			source.X += data.SeasonOffset.X * seasonIndexForLocation;
			source.Y += data.SeasonOffset.Y * seasonIndexForLocation;
		}
		return source;
	}

	public virtual Microsoft.Xna.Framework.Rectangle? getSourceRectForMenu()
	{
		return null;
	}

	public virtual void updateInteriorWarps(GameLocation interior = null)
	{
		interior = interior ?? GetIndoors();
		if (interior == null)
		{
			return;
		}
		GameLocation parentLocation = GetParentLocation();
		foreach (Warp warp in interior.warps)
		{
			if (warp.TargetName == "Farm" || (parentLocation != null && warp.TargetName == parentLocation.NameOrUniqueName))
			{
				warp.TargetName = parentLocation?.NameOrUniqueName ?? warp.TargetName;
				warp.TargetX = humanDoor.X + tileX.Value;
				warp.TargetY = humanDoor.Y + tileY.Value + 1;
			}
		}
	}

	/// <summary>Get whether the building has an interior location.</summary>
	public bool HasIndoors()
	{
		if (indoors.Value == null)
		{
			return nonInstancedIndoorsName.Value != null;
		}
		return true;
	}

	/// <summary>Get whether the building has an interior location with the given unique name.</summary>
	/// <param name="name">The name to check.</param>
	public bool HasIndoorsName(string name)
	{
		if (name != null)
		{
			return GetIndoorsName().EqualsIgnoreCase(name);
		}
		return false;
	}

	/// <summary>Get the unique name of the location within this building, if it's linked to an instanced or non-instanced interior.</summary>
	public string GetIndoorsName()
	{
		return indoors.Value?.NameOrUniqueName ?? nonInstancedIndoorsName.Value;
	}

	/// <summary>Get the type of indoors location this building has.</summary>
	public IndoorsType GetIndoorsType()
	{
		if (indoors.Value != null)
		{
			return IndoorsType.Instanced;
		}
		if (nonInstancedIndoorsName.Value != null)
		{
			return IndoorsType.Global;
		}
		return IndoorsType.None;
	}

	/// <summary>Get the location within this building, if it's linked to an instanced or non-instanced interior.</summary>
	public GameLocation GetIndoors()
	{
		if (indoors.Value != null)
		{
			return indoors.Value;
		}
		if (nonInstancedIndoorsName.Value != null)
		{
			return Game1.getLocationFromName(nonInstancedIndoorsName.Value);
		}
		return null;
	}

	protected virtual GameLocation createIndoors(BuildingData data, string nameOfIndoorsWithoutUnique)
	{
		GameLocation gameLocation = null;
		if (data != null && !string.IsNullOrEmpty(data.IndoorMap))
		{
			Type type = typeof(GameLocation);
			if (data.IndoorMapType != null)
			{
				Exception ex = null;
				try
				{
					type = Type.GetType(data.IndoorMapType);
				}
				catch (Exception ex2)
				{
					ex = ex2;
				}
				if ((object)type == null || ex != null)
				{
					Game1.log.Error($"Error constructing interior type '{data.IndoorMapType}' for building '{buildingType.Value}'" + ((ex != null) ? "." : ": that type doesn't exist."));
					type = typeof(GameLocation);
				}
			}
			string text = "Maps\\" + data.IndoorMap;
			try
			{
				gameLocation = (GameLocation)Activator.CreateInstance(type, text, buildingType.Value);
			}
			catch (Exception)
			{
				try
				{
					gameLocation = (GameLocation)Activator.CreateInstance(type, text);
				}
				catch (Exception exception)
				{
					Game1.log.Error($"Error trying to instantiate indoors for '{buildingType}'", exception);
					gameLocation = new GameLocation("Maps\\" + nameOfIndoorsWithoutUnique, buildingType.Value);
				}
			}
		}
		if (gameLocation != null)
		{
			gameLocation.uniqueName.Value = nameOfIndoorsWithoutUnique + GuidHelper.NewGuid();
			gameLocation.IsFarm = true;
			gameLocation.isStructure.Value = true;
			gameLocation.ParentBuilding = this;
			updateInteriorWarps(gameLocation);
		}
		return gameLocation;
	}

	public virtual Point getPointForHumanDoor()
	{
		return new Point(tileX.Value + humanDoor.Value.X, tileY.Value + humanDoor.Value.Y);
	}

	public virtual Microsoft.Xna.Framework.Rectangle getRectForHumanDoor()
	{
		return new Microsoft.Xna.Framework.Rectangle(getPointForHumanDoor().X * 64, getPointForHumanDoor().Y * 64, 64, 64);
	}

	public Microsoft.Xna.Framework.Rectangle getRectForAnimalDoor()
	{
		return getRectForAnimalDoor(GetData());
	}

	public virtual Microsoft.Xna.Framework.Rectangle getRectForAnimalDoor(BuildingData data)
	{
		if (data != null)
		{
			Microsoft.Xna.Framework.Rectangle rectangle = data.AnimalDoor;
			return new Microsoft.Xna.Framework.Rectangle((rectangle.X + tileX.Value) * 64, (rectangle.Y + tileY.Value) * 64, rectangle.Width * 64, rectangle.Height * 64);
		}
		return new Microsoft.Xna.Framework.Rectangle((animalDoor.X + tileX.Value) * 64, (tileY.Value + animalDoor.Y) * 64, 64, 64);
	}

	public virtual void load()
	{
		if (!Game1.IsMasterGame)
		{
			return;
		}
		BuildingData data = GetData();
		if (!hasLoaded)
		{
			hasLoaded = true;
			if (data != null)
			{
				if (data.NonInstancedIndoorLocation == null && nonInstancedIndoorsName.Value != null)
				{
					GameLocation gameLocation = GetIndoors();
					if (gameLocation != null)
					{
						gameLocation.parentLocationName.Value = null;
					}
					nonInstancedIndoorsName.Value = null;
				}
				else if (data.NonInstancedIndoorLocation != null)
				{
					bool nonInstancedLocationAlreadyUsed = false;
					Utility.ForEachBuilding(delegate(Building building)
					{
						if (building.HasIndoorsName(data.NonInstancedIndoorLocation))
						{
							nonInstancedLocationAlreadyUsed = true;
							return false;
						}
						return true;
					});
					if (!nonInstancedLocationAlreadyUsed)
					{
						nonInstancedIndoorsName.Value = Game1.RequireLocation(data.NonInstancedIndoorLocation).NameOrUniqueName;
					}
				}
			}
			LoadFromBuildingData(data);
		}
		if (nonInstancedIndoorsName.Value != null)
		{
			UpdateIndoorParent();
		}
		else
		{
			string text = data?.IndoorMap ?? indoors.Value?.Name;
			GameLocation gameLocation2 = createIndoors(data, text);
			if (gameLocation2 != null && indoors.Value != null)
			{
				gameLocation2.characters.Set(indoors.Value.characters);
				gameLocation2.netObjects.MoveFrom(indoors.Value.netObjects);
				gameLocation2.terrainFeatures.MoveFrom(indoors.Value.terrainFeatures);
				gameLocation2.IsFarm = true;
				gameLocation2.IsOutdoors = false;
				gameLocation2.isStructure.Value = true;
				gameLocation2.miniJukeboxCount.Set(indoors.Value.miniJukeboxCount.Value);
				gameLocation2.miniJukeboxTrack.Set(indoors.Value.miniJukeboxTrack.Value);
				gameLocation2.uniqueName.Value = indoors.Value.uniqueName.Value ?? (text + (tileX.Value * 2000 + tileY.Value));
				gameLocation2.numberOfSpawnedObjectsOnMap = indoors.Value.numberOfSpawnedObjectsOnMap;
				gameLocation2.animals.MoveFrom(indoors.Value.animals);
				if (indoors.Value is AnimalHouse animalHouse && gameLocation2 is AnimalHouse animalHouse2)
				{
					animalHouse2.animalsThatLiveHere.Set(animalHouse.animalsThatLiveHere);
				}
				foreach (KeyValuePair<long, FarmAnimal> pair in gameLocation2.animals.Pairs)
				{
					pair.Value.reload(gameLocation2);
				}
				gameLocation2.furniture.Set(indoors.Value.furniture);
				foreach (Furniture item in gameLocation2.furniture)
				{
					item.updateDrawPosition();
				}
				if (indoors.Value is Cabin cabin && gameLocation2 is Cabin cabin2)
				{
					cabin2.fridge.Value = cabin.fridge.Value;
					cabin2.farmhandReference.Value = cabin.farmhandReference.Value;
				}
				gameLocation2.TransferDataFromSavedLocation(indoors.Value);
				indoors.Value = gameLocation2;
			}
			updateInteriorWarps();
			if (indoors.Value != null)
			{
				for (int num = indoors.Value.characters.Count - 1; num >= 0; num--)
				{
					SaveGame.initializeCharacter(indoors.Value.characters[num], indoors.Value);
				}
				foreach (TerrainFeature value in indoors.Value.terrainFeatures.Values)
				{
					value.loadSprite();
				}
				foreach (KeyValuePair<Vector2, Object> pair2 in indoors.Value.objects.Pairs)
				{
					pair2.Value.initializeLightSource(pair2.Key);
					pair2.Value.reloadSprite();
				}
			}
		}
		if (data != null)
		{
			humanDoor.X = data.HumanDoor.X;
			humanDoor.Y = data.HumanDoor.Y;
		}
	}

	/// <summary>Get the extra tiles to treat as part of the building when placing it through a construction menu, if any. For example, the farmhouse uses this to make sure the stairs are clear.</summary>
	public IEnumerable<BuildingPlacementTile> GetAdditionalPlacementTiles()
	{
		IEnumerable<BuildingPlacementTile> enumerable = GetData()?.AdditionalPlacementTiles;
		return enumerable ?? LegacyShims.EmptyArray<BuildingPlacementTile>();
	}

	public bool isUnderConstruction(bool ignoreUpgrades = true)
	{
		if (!ignoreUpgrades && daysUntilUpgrade.Value > 0)
		{
			return true;
		}
		return daysOfConstructionLeft.Value > 0;
	}

	/// <summary>Get whether the building's bounds covers a given tile coordinate.</summary>
	/// <param name="tile">The tile position to check.</param>
	/// <param name="applyTilePropertyRadius">Whether to check the extra tiles around the building itself for which it may add tile properties.</param>
	public bool occupiesTile(Vector2 tile, bool applyTilePropertyRadius = false)
	{
		return occupiesTile((int)tile.X, (int)tile.Y, applyTilePropertyRadius);
	}

	/// <summary>Get whether the building's bounds covers a given tile coordinate.</summary>
	/// <param name="x">The X tile position to check.</param>
	/// <param name="y">The Y tile position to check</param>
	/// <param name="applyTilePropertyRadius">Whether to check the extra tiles around the building itself for which it may add tile properties.</param>
	public virtual bool occupiesTile(int x, int y, bool applyTilePropertyRadius = false)
	{
		int num = (applyTilePropertyRadius ? GetAdditionalTilePropertyRadius() : 0);
		int value = tileX.Value;
		int value2 = tileY.Value;
		int value3 = tilesWide.Value;
		int value4 = tilesHigh.Value;
		if (x >= value - num && x < value + value3 + num && y >= value2 - num)
		{
			return y < value2 + value4 + num;
		}
		return false;
	}

	public virtual bool isTilePassable(Vector2 tile)
	{
		bool flag = occupiesTile(tile);
		if (flag && isUnderConstruction())
		{
			return false;
		}
		BuildingData data = GetData();
		if (data != null && occupiesTile(tile, applyTilePropertyRadius: true))
		{
			return data.IsTilePassable((int)tile.X - tileX.Value, (int)tile.Y - tileY.Value);
		}
		return !flag;
	}

	public virtual bool isTileOccupiedForPlacement(Vector2 tile, Object to_place)
	{
		if (!isTilePassable(tile))
		{
			return true;
		}
		return false;
	}

	/// <summary>If this building is fishable, get the color of the water at the given tile position.</summary>
	/// <param name="tile">The tile position.</param>
	/// <returns>Returns the water color to use, or <c>null</c> to use the location's default water color.</returns>
	public virtual Color? GetWaterColor(Vector2 tile)
	{
		return null;
	}

	public virtual bool isTileFishable(Vector2 tile)
	{
		return false;
	}

	/// <summary>Whether watering cans can be refilled from any tile covered by this building.</summary>
	/// <remarks>If this is false, watering cans may still be refillable based on tile data (e.g. the <c>WaterSource</c> back tile property).</remarks>
	public virtual bool CanRefillWateringCan()
	{
		return false;
	}

	/// <summary>Create a pixel rectangle for the building's ground footprint within its location.</summary>
	public Microsoft.Xna.Framework.Rectangle GetBoundingBox()
	{
		return new Microsoft.Xna.Framework.Rectangle(tileX.Value * 64, tileY.Value * 64, tilesWide.Value * 64, tilesHigh.Value * 64);
	}

	public virtual bool intersects(Microsoft.Xna.Framework.Rectangle boundingBox)
	{
		Microsoft.Xna.Framework.Rectangle boundingBox2 = GetBoundingBox();
		int additionalTilePropertyRadius = GetAdditionalTilePropertyRadius();
		if (additionalTilePropertyRadius > 0)
		{
			boundingBox2.Inflate(additionalTilePropertyRadius * 64, additionalTilePropertyRadius * 64);
		}
		if (boundingBox2.Intersects(boundingBox))
		{
			int i = boundingBox.Top / 64;
			for (int num = boundingBox.Bottom / 64; i <= num; i++)
			{
				int j = boundingBox.Left / 64;
				for (int num2 = boundingBox.Right / 64; j <= num2; j++)
				{
					if (!isTilePassable(new Vector2(j, i)))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public virtual void drawInMenu(SpriteBatch b, int x, int y)
	{
		BuildingData data = GetData();
		if (data != null)
		{
			x += (int)(data.DrawOffset.X * 4f);
			y += (int)(data.DrawOffset.Y * 4f);
		}
		float num = tilesHigh.Value * 64;
		float num2 = num;
		if (data != null)
		{
			num2 -= data.SortTileOffset * 64f;
		}
		num2 /= 10000f;
		if (ShouldDrawShadow(data))
		{
			drawShadow(b, x, y);
		}
		Microsoft.Xna.Framework.Rectangle sourceRect = getSourceRect();
		b.Draw(texture.Value, new Vector2(x, y), sourceRect, color, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, num2);
		if (data?.DrawLayers == null)
		{
			return;
		}
		foreach (BuildingDrawLayer drawLayer in data.DrawLayers)
		{
			if (drawLayer.OnlyDrawIfChestHasContents == null)
			{
				num2 = num - drawLayer.SortTileOffset * 64f;
				num2 += 1f;
				if (drawLayer.DrawInBackground)
				{
					num2 = 0f;
				}
				num2 /= 10000f;
				Microsoft.Xna.Framework.Rectangle sourceRect2 = drawLayer.GetSourceRect((int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds);
				sourceRect2 = ApplySourceRectOffsets(sourceRect2);
				Texture2D texture2D = texture.Value;
				if (drawLayer.Texture != null)
				{
					texture2D = Game1.content.Load<Texture2D>(drawLayer.Texture);
				}
				b.Draw(texture2D, new Vector2(x, y) + drawLayer.DrawPosition * 4f, sourceRect2, Color.White, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, num2);
			}
		}
	}

	public virtual void drawBackground(SpriteBatch b)
	{
		if (isMoving || daysOfConstructionLeft.Value > 0 || newConstructionTimer.Value > 0)
		{
			return;
		}
		BuildingData data = GetData();
		if (data?.DrawLayers == null)
		{
			return;
		}
		Vector2 vector = new Vector2(0f, getSourceRect().Height);
		Vector2 vector2 = new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64);
		foreach (BuildingDrawLayer drawLayer in data.DrawLayers)
		{
			if (!drawLayer.DrawInBackground)
			{
				continue;
			}
			if (drawLayer.OnlyDrawIfChestHasContents != null)
			{
				Chest buildingChest = GetBuildingChest(drawLayer.OnlyDrawIfChestHasContents);
				if (buildingChest == null || buildingChest.isEmpty())
				{
					continue;
				}
			}
			Microsoft.Xna.Framework.Rectangle sourceRect = drawLayer.GetSourceRect((int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds);
			sourceRect = ApplySourceRectOffsets(sourceRect);
			Vector2 vector3 = Vector2.Zero;
			if (drawLayer.AnimalDoorOffset != Point.Zero)
			{
				vector3 = new Vector2((float)drawLayer.AnimalDoorOffset.X * animalDoorOpenAmount.Value, (float)drawLayer.AnimalDoorOffset.Y * animalDoorOpenAmount.Value);
			}
			Texture2D texture2D = texture.Value;
			if (drawLayer.Texture != null)
			{
				texture2D = Game1.content.Load<Texture2D>(drawLayer.Texture);
			}
			b.Draw(texture2D, Game1.GlobalToLocal(Game1.viewport, vector2 + (vector3 - vector + drawLayer.DrawPosition) * 4f), sourceRect, color * alpha, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, 0f);
		}
	}

	public virtual void draw(SpriteBatch b)
	{
		if (isMoving)
		{
			return;
		}
		if (daysOfConstructionLeft.Value > 0 || newConstructionTimer.Value > 0)
		{
			drawInConstruction(b);
			return;
		}
		BuildingData data = GetData();
		if (ShouldDrawShadow(data))
		{
			drawShadow(b);
		}
		float num = (tileY.Value + tilesHigh.Value) * 64;
		float num2 = num;
		if (data != null)
		{
			num2 -= data.SortTileOffset * 64f;
		}
		num2 /= 10000f;
		Vector2 vector = new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64);
		Vector2 vector2 = Vector2.Zero;
		if (data != null)
		{
			vector2 = data.DrawOffset * 4f;
		}
		Microsoft.Xna.Framework.Rectangle sourceRect = getSourceRect();
		Vector2 vector3 = new Vector2(0f, sourceRect.Height);
		b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, vector + vector2), sourceRect, color * alpha, 0f, vector3, 4f, SpriteEffects.None, num2);
		if (magical.Value && buildingType.Value.Equals("Gold Clock"))
		{
			if (Game1.netWorldState.Value.goldenClocksTurnedOff.Value)
			{
				b.Draw(Game1.mouseCursors_1_6, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 68, tileY.Value * 64 - 56)), new Microsoft.Xna.Framework.Rectangle(498, 368, 13, 9), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.0001f);
			}
			else
			{
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 92, tileY.Value * 64 - 40)), Town.hourHandSource, Color.White * alpha, (float)(Math.PI * 2.0 * (double)((float)(Game1.timeOfDay % 1200) / 1200f) + (double)((float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes / 23f)), new Vector2(2.5f, 8f), 3f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.0001f);
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 92, tileY.Value * 64 - 40)), Town.minuteHandSource, Color.White * alpha, (float)(Math.PI * 2.0 * (double)((float)(Game1.timeOfDay % 1000 % 100 % 60) / 60f) + (double)((float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 1.02f)), new Vector2(2.5f, 12f), 3f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.00011f);
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 92, tileY.Value * 64 - 40)), Town.clockNub, Color.White * alpha, 0f, new Vector2(2f, 2f), 4f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.00012f);
			}
		}
		if (data != null)
		{
			foreach (Chest buildingChest2 in buildingChests)
			{
				BuildingChest buildingChestData = GetBuildingChestData(data, buildingChest2.Name);
				if (buildingChestData.DisplayTile.X != -1f && buildingChestData.DisplayTile.Y != -1f && buildingChest2.Items.Count > 0 && buildingChest2.Items[0] != null)
				{
					num2 = ((float)tileY.Value + buildingChestData.DisplayTile.Y + 1f) * 64f;
					num2 += 1f;
					float num3 = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2) - buildingChestData.DisplayHeight * 64f;
					float num4 = ((float)tileX.Value + buildingChestData.DisplayTile.X) * 64f;
					float num5 = ((float)tileY.Value + buildingChestData.DisplayTile.Y - 1f) * 64f;
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(num4, num5 + num3)), new Microsoft.Xna.Framework.Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, num2 / 10000f);
					ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(buildingChest2.Items[0].QualifiedItemId);
					b.Draw(dataOrErrorItem.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(num4 + 32f + 4f, num5 + 32f + num3)), dataOrErrorItem.GetSourceRect(), Color.White * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, (num2 + 1f) / 10000f);
				}
			}
			if (data.DrawLayers != null)
			{
				foreach (BuildingDrawLayer drawLayer in data.DrawLayers)
				{
					if (drawLayer.DrawInBackground)
					{
						continue;
					}
					if (drawLayer.OnlyDrawIfChestHasContents != null)
					{
						Chest buildingChest = GetBuildingChest(drawLayer.OnlyDrawIfChestHasContents);
						if (buildingChest == null || buildingChest.isEmpty())
						{
							continue;
						}
					}
					num2 = num - drawLayer.SortTileOffset * 64f;
					num2 += 1f;
					num2 /= 10000f;
					Microsoft.Xna.Framework.Rectangle sourceRect2 = drawLayer.GetSourceRect((int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds);
					sourceRect2 = ApplySourceRectOffsets(sourceRect2);
					vector2 = Vector2.Zero;
					if (drawLayer.AnimalDoorOffset != Point.Zero)
					{
						vector2 = new Vector2((float)drawLayer.AnimalDoorOffset.X * animalDoorOpenAmount.Value, (float)drawLayer.AnimalDoorOffset.Y * animalDoorOpenAmount.Value);
					}
					Texture2D texture2D = texture.Value;
					if (drawLayer.Texture != null)
					{
						texture2D = Game1.content.Load<Texture2D>(drawLayer.Texture);
					}
					b.Draw(texture2D, Game1.GlobalToLocal(Game1.viewport, vector + (vector2 - vector3 + drawLayer.DrawPosition) * 4f), sourceRect2, color * alpha, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, num2);
				}
			}
		}
		if (daysUntilUpgrade.Value <= 0)
		{
			return;
		}
		if (data != null)
		{
			if (data.UpgradeSignTile.X >= 0f)
			{
				num2 = ((float)tileY.Value + data.UpgradeSignTile.Y + 1f) * 64f;
				num2 += 2f;
				num2 /= 10000f;
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, getUpgradeSignLocation()), new Microsoft.Xna.Framework.Rectangle(367, 309, 16, 15), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, num2);
			}
		}
		else if (GetIndoors() is Shed)
		{
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, getUpgradeSignLocation()), new Microsoft.Xna.Framework.Rectangle(367, 309, 16, 15), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.0001f);
		}
	}

	public bool ShouldDrawShadow(BuildingData data)
	{
		return data?.DrawShadow ?? true;
	}

	public virtual void drawShadow(SpriteBatch b, int localX = -1, int localY = -1)
	{
		Microsoft.Xna.Framework.Rectangle rectangle = getSourceRectForMenu() ?? getSourceRect();
		Vector2 vector = ((localX == -1) ? Game1.GlobalToLocal(new Vector2(tileX.Value * 64, (tileY.Value + tilesHigh.Value) * 64)) : new Vector2(localX, localY + rectangle.Height * 4));
		b.Draw(Game1.mouseCursors, vector, leftShadow, Color.White * ((localX == -1) ? alpha : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
		for (int i = 1; i < tilesWide.Value - 1; i++)
		{
			b.Draw(Game1.mouseCursors, vector + new Vector2(i * 64, 0f), middleShadow, Color.White * ((localX == -1) ? alpha : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
		}
		b.Draw(Game1.mouseCursors, vector + new Vector2((tilesWide.Value - 1) * 64, 0f), rightShadow, Color.White * ((localX == -1) ? alpha : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
	}

	public virtual void OnStartMove()
	{
	}

	public virtual void OnEndMove()
	{
		Game1.player.team.SendBuildingMovedEvent(GetParentLocation(), this);
	}

	public Point getPorchStandingSpot()
	{
		if (isCabin)
		{
			return new Point(tileX.Value + 1, tileY.Value + tilesHigh.Value - 1);
		}
		return new Point(0, 0);
	}

	public virtual bool doesTileHaveProperty(int tile_x, int tile_y, string property_name, string layer_name, ref string property_value)
	{
		BuildingData data = GetData();
		if (data != null && daysOfConstructionLeft.Value <= 0 && data.HasPropertyAtTile(tile_x - tileX.Value, tile_y - tileY.Value, property_name, layer_name, ref property_value))
		{
			return true;
		}
		if (property_name == "NoSpawn" && layer_name == "Back" && occupiesTile(tile_x, tile_y))
		{
			property_value = "All";
			return true;
		}
		return false;
	}

	public Point getMailboxPosition()
	{
		if (isCabin)
		{
			return new Point(tileX.Value + tilesWide.Value - 1, tileY.Value + tilesHigh.Value - 1);
		}
		return new Point(68, 16);
	}

	/// <summary>Get the number of extra tiles around the building for which it may add tile properties, but without hiding tile properties from the underlying ground that aren't overwritten by the building data.</summary>
	public virtual int GetAdditionalTilePropertyRadius()
	{
		return GetData()?.AdditionalTilePropertyRadius ?? 0;
	}

	public void removeOverlappingBushes(GameLocation location)
	{
		for (int i = tileX.Value; i < tileX.Value + tilesWide.Value; i++)
		{
			for (int j = tileY.Value; j < tileY.Value + tilesHigh.Value; j++)
			{
				if (location.isTerrainFeatureAt(i, j))
				{
					LargeTerrainFeature largeTerrainFeatureAt = location.getLargeTerrainFeatureAt(i, j);
					if (largeTerrainFeatureAt is Bush)
					{
						location.largeTerrainFeatures.Remove(largeTerrainFeatureAt);
					}
				}
			}
		}
	}

	public virtual void drawInConstruction(SpriteBatch b)
	{
		int num = Math.Min(16, Math.Max(0, (int)(16f - (float)newConstructionTimer.Value / 1000f * 16f)));
		float num2 = (float)(2000 - newConstructionTimer.Value) / 2000f;
		if (magical.Value || daysOfConstructionLeft.Value <= 0)
		{
			BuildingData data = GetData();
			if (ShouldDrawShadow(data))
			{
				drawShadow(b);
			}
			Microsoft.Xna.Framework.Rectangle sourceRect = getSourceRect();
			Microsoft.Xna.Framework.Rectangle rectangle = getSourceRectForMenu() ?? sourceRect;
			int num3 = (int)((float)(sourceRect.Height * 4) * (1f - num2));
			float num4 = (tileY.Value + tilesHigh.Value) * 64;
			float num5 = num4;
			if (data != null)
			{
				num5 -= data.SortTileOffset * 64f;
			}
			num5 /= 10000f;
			Vector2 vector = new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64);
			Vector2 vector2 = Vector2.Zero;
			if (data != null)
			{
				vector2 = data.DrawOffset * 4f;
			}
			Vector2 vector3 = new Vector2(0f, num3 + 4 - num3 % 4);
			Vector2 vector4 = new Vector2(0f, sourceRect.Height);
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, vector + vector3 + vector2), new Microsoft.Xna.Framework.Rectangle(sourceRect.Left, sourceRect.Bottom - (int)(num2 * (float)sourceRect.Height), rectangle.Width, (int)((float)sourceRect.Height * num2)), color * alpha, 0f, new Vector2(0f, sourceRect.Height), 4f, SpriteEffects.None, num5);
			if (data?.DrawLayers != null)
			{
				foreach (BuildingDrawLayer drawLayer in data.DrawLayers)
				{
					if (drawLayer.OnlyDrawIfChestHasContents != null)
					{
						continue;
					}
					num5 = num4 - drawLayer.SortTileOffset * 64f;
					num5 += 1f;
					num5 /= 10000f;
					Microsoft.Xna.Framework.Rectangle sourceRect2 = drawLayer.GetSourceRect((int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds);
					sourceRect2 = ApplySourceRectOffsets(sourceRect2);
					float num6 = (float)(num3 / 4) - drawLayer.DrawPosition.Y;
					vector2 = Vector2.Zero;
					if (!(num6 > (float)sourceRect2.Height))
					{
						if (num6 > 0f)
						{
							vector2.Y += num6;
							sourceRect2.Y += (int)num6;
							sourceRect2.Height -= (int)num6;
						}
						Texture2D texture2D = texture.Value;
						if (drawLayer.Texture != null)
						{
							texture2D = Game1.content.Load<Texture2D>(drawLayer.Texture);
						}
						b.Draw(texture2D, Game1.GlobalToLocal(Game1.viewport, vector + (vector2 - vector4 + drawLayer.DrawPosition) * 4f), sourceRect2, color * alpha, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, num5);
					}
				}
			}
			if (magical.Value)
			{
				for (int i = 0; i < tilesWide.Value * 4; i++)
				{
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + i * 16, (float)(tileY.Value * 64 - sourceRect.Height * 4 + tilesHigh.Value * 64) + (float)(sourceRect.Height * 4) * (1f - num2))) + new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2) - ((i % 2 == 0) ? 32 : 8)), new Microsoft.Xna.Framework.Rectangle(536 + (newConstructionTimer.Value + i * 4) % 56 / 8 * 8, 1945, 8, 8), (i % 2 == 1) ? (Color.Pink * alpha) : (Color.LightPink * alpha), 0f, new Vector2(0f, 0f), 4f + (float)Game1.random.Next(100) / 100f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.0001f);
					if (i % 2 == 0)
					{
						b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + i * 16, (float)(tileY.Value * 64 - sourceRect.Height * 4 + tilesHigh.Value * 64) + (float)(sourceRect.Height * 4) * (1f - num2))) + new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2) + ((i % 2 == 0) ? 32 : 8)), new Microsoft.Xna.Framework.Rectangle(536 + (newConstructionTimer.Value + i * 4) % 56 / 8 * 8, 1945, 8, 8), Color.White * alpha, 0f, new Vector2(0f, 0f), 4f + (float)Game1.random.Next(100) / 100f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.0001f);
					}
				}
				return;
			}
			for (int j = 0; j < tilesWide.Value * 4; j++)
			{
				b.Draw(Game1.animations, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 - 16 + j * 16, (float)(tileY.Value * 64 - sourceRect.Height * 4 + tilesHigh.Value * 64) + (float)(sourceRect.Height * 4) * (1f - num2))) + new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2) - ((j % 2 == 0) ? 32 : 8)), new Microsoft.Xna.Framework.Rectangle((newConstructionTimer.Value + j * 20) % 304 / 38 * 64, 768, 64, 64), Color.White * alpha * ((float)newConstructionTimer.Value / 500f), 0f, new Vector2(0f, 0f), 1f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.0001f);
				if (j % 2 == 0)
				{
					b.Draw(Game1.animations, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 - 16 + j * 16, (float)(tileY.Value * 64 - sourceRect.Height * 4 + tilesHigh.Value * 64) + (float)(sourceRect.Height * 4) * (1f - num2))) + new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2) - ((j % 2 == 0) ? 32 : 8)), new Microsoft.Xna.Framework.Rectangle((newConstructionTimer.Value + j * 20) % 400 / 50 * 64, 2944, 64, 64), Color.White * alpha * ((float)newConstructionTimer.Value / 500f), 0f, new Vector2(0f, 0f), 1f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value) * 64) / 10000f + 0.0001f);
				}
			}
			return;
		}
		bool flag = daysOfConstructionLeft.Value == 1;
		for (int k = tileX.Value; k < tileX.Value + tilesWide.Value; k++)
		{
			for (int l = tileY.Value; l < tileY.Value + tilesHigh.Value; l++)
			{
				if (k == tileX.Value + tilesWide.Value / 2 && l == tileY.Value + tilesHigh.Value - 1)
				{
					if (flag)
					{
						b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4 + 16 - 4), new Microsoft.Xna.Framework.Rectangle(367, 277, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
					}
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4) + ((newConstructionTimer.Value > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Microsoft.Xna.Framework.Rectangle(367, 309, 16, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(l * 64 + 64 - 1) / 10000f);
				}
				else if (k == tileX.Value && l == tileY.Value)
				{
					if (flag)
					{
						b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4 + 16), new Microsoft.Xna.Framework.Rectangle(351, 261, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
					}
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4) + ((newConstructionTimer.Value > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Microsoft.Xna.Framework.Rectangle(351, 293, 16, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(l * 64 + 64 - 1) / 10000f);
				}
				else if (k == tileX.Value + tilesWide.Value - 1 && l == tileY.Value)
				{
					if (flag)
					{
						b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4 + 16), new Microsoft.Xna.Framework.Rectangle(383, 261, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
					}
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4) + ((newConstructionTimer.Value > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Microsoft.Xna.Framework.Rectangle(383, 293, 16, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(l * 64 + 64 - 1) / 10000f);
				}
				else if (k == tileX.Value + tilesWide.Value - 1 && l == tileY.Value + tilesHigh.Value - 1)
				{
					if (flag)
					{
						b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4 + 16), new Microsoft.Xna.Framework.Rectangle(383, 277, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
					}
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4) + ((newConstructionTimer.Value > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Microsoft.Xna.Framework.Rectangle(383, 325, 16, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(l * 64) / 10000f);
				}
				else if (k == tileX.Value && l == tileY.Value + tilesHigh.Value - 1)
				{
					if (flag)
					{
						b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4 + 16), new Microsoft.Xna.Framework.Rectangle(351, 277, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
					}
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4) + ((newConstructionTimer.Value > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Microsoft.Xna.Framework.Rectangle(351, 325, 16, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(l * 64) / 10000f);
				}
				else if (k == tileX.Value + tilesWide.Value - 1)
				{
					if (flag)
					{
						b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4 + 16), new Microsoft.Xna.Framework.Rectangle(383, 261, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
					}
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4) + ((newConstructionTimer.Value > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Microsoft.Xna.Framework.Rectangle(383, 309, 16, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(l * 64) / 10000f);
				}
				else if (l == tileY.Value + tilesHigh.Value - 1)
				{
					if (flag)
					{
						b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4 + 16), new Microsoft.Xna.Framework.Rectangle(367, 277, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
					}
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4) + ((newConstructionTimer.Value > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Microsoft.Xna.Framework.Rectangle(367, 325, 16, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(l * 64) / 10000f);
				}
				else if (k == tileX.Value)
				{
					if (flag)
					{
						b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4 + 16), new Microsoft.Xna.Framework.Rectangle(351, 261, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
					}
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4) + ((newConstructionTimer.Value > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Microsoft.Xna.Framework.Rectangle(351, 309, 16, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(l * 64) / 10000f);
				}
				else if (l == tileY.Value)
				{
					if (flag)
					{
						b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4 + 16), new Microsoft.Xna.Framework.Rectangle(367, 261, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
					}
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4) + ((newConstructionTimer.Value > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Microsoft.Xna.Framework.Rectangle(367, 293, 16, num), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(l * 64 + 64 - 1) / 10000f);
				}
				else if (flag)
				{
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(k, l) * 64f) + new Vector2(0f, 64 - num * 4 + 16), new Microsoft.Xna.Framework.Rectangle(367, 261, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
				}
			}
		}
	}
}
