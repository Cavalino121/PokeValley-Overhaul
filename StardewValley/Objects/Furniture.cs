using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.TerrainFeatures;

namespace StardewValley.Objects;

[XmlInclude(typeof(BedFurniture))]
[XmlInclude(typeof(RandomizedPlantFurniture))]
[XmlInclude(typeof(StorageFurniture))]
[XmlInclude(typeof(TV))]
public class Furniture : Object, ISittable
{
	public const int chair = 0;

	public const int bench = 1;

	public const int couch = 2;

	public const int armchair = 3;

	public const int dresser = 4;

	public const int longTable = 5;

	public const int painting = 6;

	public const int lamp = 7;

	public const int decor = 8;

	public const int other = 9;

	public const int bookcase = 10;

	public const int table = 11;

	public const int rug = 12;

	public const int window = 13;

	public const int fireplace = 14;

	public const int bed = 15;

	public const int torch = 16;

	public const int sconce = 17;

	public const string furnitureTextureName = "TileSheets\\furniture";

	[XmlElement("furniture_type")]
	public readonly NetInt furniture_type = new NetInt();

	[XmlElement("rotations")]
	public readonly NetInt rotations = new NetInt();

	[XmlElement("currentRotation")]
	public readonly NetInt currentRotation = new NetInt();

	[XmlElement("sourceIndexOffset")]
	private readonly NetInt sourceIndexOffset = new NetInt();

	[XmlElement("drawPosition")]
	protected readonly NetVector2 drawPosition = new NetVector2();

	[XmlElement("sourceRect")]
	public readonly NetRectangle sourceRect = new NetRectangle();

	[XmlElement("defaultSourceRect")]
	public readonly NetRectangle defaultSourceRect = new NetRectangle();

	[XmlElement("defaultBoundingBox")]
	public readonly NetRectangle defaultBoundingBox = new NetRectangle();

	[XmlElement("drawHeldObjectLow")]
	public readonly NetBool drawHeldObjectLow = new NetBool();

	[XmlIgnore]
	public NetLongDictionary<int, NetInt> sittingFarmers = new NetLongDictionary<int, NetInt>();

	[XmlIgnore]
	public Vector2? lightGlowPosition;

	/// <summary>Whether this furniture can be removed if other checks pass.</summary>
	/// <remarks>This value only applies for the current instance, it's not synced in multiplayer or written to the save file.</remarks>
	[XmlIgnore]
	public bool AllowLocalRemoval = true;

	public static bool isDrawingLocationFurniture;

	protected static Dictionary<string, string> _frontTextureName;

	[XmlIgnore]
	private int _placementRestriction = -1;

	[XmlIgnore]
	private string _description;

	[XmlIgnore]
	public int placementRestriction
	{
		get
		{
			if (_placementRestriction < 0)
			{
				bool flag = true;
				string[] data = getData();
				if (data != null && data.Length > 6 && int.TryParse(data[6], out _placementRestriction) && _placementRestriction >= 0)
				{
					flag = false;
				}
				if (flag)
				{
					if (base.name.Contains("TV"))
					{
						_placementRestriction = 0;
					}
					else if (IsTable() || furniture_type.Value == 1 || furniture_type.Value == 0 || furniture_type.Value == 8 || furniture_type.Value == 16)
					{
						_placementRestriction = 2;
					}
					else
					{
						_placementRestriction = 0;
					}
				}
			}
			return _placementRestriction;
		}
	}

	[XmlIgnore]
	public string description
	{
		get
		{
			if (_description == null)
			{
				_description = loadDescription();
			}
			return _description;
		}
	}

	/// <inheritdoc />
	public override string TypeDefinitionId { get; } = "(F)";

	/// <inheritdoc />
	public override string Name => base.name;

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(furniture_type, "furniture_type").AddField(rotations, "rotations").AddField(currentRotation, "currentRotation")
			.AddField(sourceIndexOffset, "sourceIndexOffset")
			.AddField(drawPosition, "drawPosition")
			.AddField(sourceRect, "sourceRect")
			.AddField(defaultSourceRect, "defaultSourceRect")
			.AddField(defaultBoundingBox, "defaultBoundingBox")
			.AddField(drawHeldObjectLow, "drawHeldObjectLow")
			.AddField(sittingFarmers, "sittingFarmers");
	}

	public Furniture()
	{
		updateDrawPosition();
		isOn.Value = false;
	}

	public Furniture(string itemId, Vector2 tile, int initialRotations)
		: this(itemId, tile)
	{
		for (int i = 0; i < initialRotations; i++)
		{
			rotate();
		}
		isOn.Value = false;
	}

	public virtual void OnAdded(GameLocation loc, Vector2 tilePos)
	{
		if (IntersectsForCollision(Game1.player.GetBoundingBox()))
		{
			Game1.player.TemporaryPassableTiles.Add(GetBoundingBoxAt((int)tilePos.X, (int)tilePos.Y));
		}
		if (furniture_type.Value == 13)
		{
			if (loc != null && loc.IsRainingHere())
			{
				sourceRect.Value = defaultSourceRect.Value;
				sourceIndexOffset.Value = 1;
			}
			else
			{
				sourceRect.Value = defaultSourceRect.Value;
				sourceIndexOffset.Value = 0;
				AddLightGlow();
			}
		}
		minutesElapsed(1);
	}

	public void OnRemoved(GameLocation loc, Vector2 tilePos)
	{
		RemoveLightGlow();
	}

	public override bool IsHeldOverHead()
	{
		return false;
	}

	/// <summary>Whether this is a table, which can have items placed on it.</summary>
	public virtual bool IsTable()
	{
		int value = furniture_type.Value;
		if (value != 11)
		{
			return value == 5;
		}
		return true;
	}

	public static Rectangle GetDefaultSourceRect(string itemId, Texture2D texture = null)
	{
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(F)" + itemId);
		string[] data = getData(itemId);
		if (data == null)
		{
			return dataOrErrorItem.GetSourceRect();
		}
		if (data[2].Equals("-1"))
		{
			return getDefaultSourceRectForType(dataOrErrorItem, getTypeNumberFromName(data[1]), texture);
		}
		string[] array = ArgUtility.SplitBySpace(data[2]);
		int spriteWidth = Convert.ToInt32(array[0]);
		int spriteHeight = Convert.ToInt32(array[1]);
		return getDefaultSourceRect(dataOrErrorItem, spriteWidth, spriteHeight, texture);
	}

	/// <summary>Set the furniture's position and rotation, and update all related data.</summary>
	/// <param name="x">The tile X position.</param>
	/// <param name="y">The tile X position.</param>
	/// <param name="rotations">The number of times to rotate the furniture, starting from its current rotation.</param>
	/// <returns>Returns the furniture instance for chaining.</returns>
	public Furniture SetPlacement(int x, int y, int rotations = 0)
	{
		return SetPlacement(new Vector2(x, y), rotations);
	}

	/// <summary>Set the furniture's position and rotation, and update all related data.</summary>
	/// <param name="tile">The tile position.</param>
	/// <param name="rotations">The number of times to rotate the furniture, starting from its current rotation.</param>
	/// <returns>Returns the furniture instance for chaining.</returns>
	public Furniture SetPlacement(Point tile, int rotations = 0)
	{
		return SetPlacement(Utility.PointToVector2(tile), rotations);
	}

	/// <summary>Set the furniture's position and rotation, and update all related data.</summary>
	/// <param name="tile">The tile position.</param>
	/// <param name="rotations">The number of times to rotate the furniture, starting from its current rotation.</param>
	/// <returns>Returns the furniture instance for chaining.</returns>
	public Furniture SetPlacement(Vector2 tile, int rotations = 0)
	{
		InitializeAtTile(tile);
		for (int i = 0; i < rotations; i++)
		{
			rotate();
		}
		return this;
	}

	/// <summary>Set the held object.</summary>
	/// <param name="obj">The object to hold.</param>
	/// <returns>Returns the furniture instance for chaining.</returns>
	public Furniture SetHeldObject(Object obj)
	{
		heldObject.Value = obj;
		if (obj != null)
		{
			if (obj is Furniture furniture)
			{
				furniture.InitializeAtTile(TileLocation);
			}
			else
			{
				obj.TileLocation = TileLocation;
			}
		}
		return this;
	}

	/// <summary>Set the furniture's tile position and update all position-related data.</summary>
	/// <param name="tile">The tile position.</param>
	public void InitializeAtTile(Vector2 tile)
	{
		Texture2D texture = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).GetTexture();
		string[] data = getData();
		if (data != null)
		{
			furniture_type.Value = getTypeNumberFromName(data[1]);
			defaultSourceRect.Value = new Rectangle(base.ParentSheetIndex * 16 % texture.Width, base.ParentSheetIndex * 16 / texture.Width * 16, 1, 1);
			drawHeldObjectLow.Value = Name.ContainsIgnoreCase("tea");
			sourceRect.Value = GetDefaultSourceRect(base.ItemId);
			defaultSourceRect.Value = sourceRect.Value;
			rotations.Value = Convert.ToInt32(data[4]);
			price.Value = Convert.ToInt32(data[5]);
		}
		else
		{
			defaultSourceRect.Value = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).GetSourceRect();
		}
		if (tile != TileLocation)
		{
			TileLocation = tile;
		}
		else
		{
			RecalculateBoundingBox(data);
		}
	}

	public Furniture(string itemId, Vector2 tile)
	{
		isOn.Value = false;
		base.ItemId = itemId;
		ResetParentSheetIndex();
		base.name = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).InternalName;
		InitializeAtTile(tile);
	}

	/// <inheritdoc />
	public override void RecalculateBoundingBox()
	{
		RecalculateBoundingBox(getData());
	}

	/// <summary>Recalculate the item's bounding box based on its current position.</summary>
	/// <param name="data">The furniture data to apply.</param>
	private void RecalculateBoundingBox(string[] data)
	{
		string text = ArgUtility.Get(data, 3);
		Rectangle value;
		if (text != null)
		{
			if (text == "-1")
			{
				value = getDefaultBoundingBoxForType(furniture_type.Value);
			}
			else
			{
				string[] array = ArgUtility.SplitBySpace(data[3]);
				value = new Rectangle((int)tileLocation.X * 64, (int)tileLocation.Y * 64, Convert.ToInt32(array[0]) * 64, Convert.ToInt32(array[1]) * 64);
			}
		}
		else
		{
			value = new Rectangle((int)tileLocation.X * 64, (int)tileLocation.Y * 64, 64, 64);
		}
		defaultBoundingBox.Value = value;
		boundingBox.Value = value;
		updateRotation();
	}

	protected string[] getData()
	{
		return getData(base.ItemId);
	}

	protected static string[] getData(string itemId)
	{
		if (!DataLoader.Furniture(Game1.content).TryGetValue(itemId, out var value))
		{
			return null;
		}
		return value.Split('/');
	}

	/// <inheritdoc />
	protected override string loadDisplayName()
	{
		return ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).DisplayName;
	}

	protected virtual string loadDescription()
	{
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		if (dataOrErrorItem.IsErrorItem)
		{
			return dataOrErrorItem.Description;
		}
		return base.QualifiedItemId switch
		{
			"(F)TrashCatalogue" => Game1.content.LoadString("Strings\\1_6_Strings:TrashCatalogueDescription"), 
			"(F)RetroCatalogue" => Game1.content.LoadString("Strings\\1_6_Strings:RetroCatalogueDescription"), 
			"(F)JunimoCatalogue" => Game1.content.LoadString("Strings\\1_6_Strings:JunimoCatalogueDescription"), 
			"(F)WizardCatalogue" => Game1.content.LoadString("Strings\\1_6_Strings:WizardCatalogueDescription"), 
			"(F)JojaCatalogue" => Game1.content.LoadString("Strings\\1_6_Strings:JojaCatalogueDescription"), 
			"(F)1308" => Game1.parseText(Game1.content.LoadString("Strings\\Objects:CatalogueDescription"), Game1.smallFont, 320), 
			"(F)1226" => Game1.parseText(Game1.content.LoadString("Strings\\Objects:FurnitureCatalogueDescription"), Game1.smallFont, 320), 
			_ => placementRestriction switch
			{
				0 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture_NotOutdoors"), 
				1 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture_Outdoors_Description"), 
				2 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture_Decoration_Description"), 
				_ => Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture.cs.12623"), 
			}, 
		};
	}

	public override string getDescription()
	{
		return Game1.parseText(description, Game1.smallFont, getDescriptionWidth());
	}

	/// <inheritdoc />
	public override Color getCategoryColor()
	{
		return new Color(100, 25, 190);
	}

	/// <inheritdoc />
	public override bool performDropDownAction(Farmer who)
	{
		actionOnPlayerEntryOrPlacement(Location, dropDown: true);
		return false;
	}

	public override void hoverAction()
	{
		base.hoverAction();
		if (!Game1.player.isInventoryFull())
		{
			Game1.mouseCursor = Game1.cursor_grab;
		}
	}

	/// <inheritdoc />
	public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return false;
		}
		if (justCheckingForActivity)
		{
			return true;
		}
		switch (base.QualifiedItemId)
		{
		case "(F)Cauldron":
			base.IsOn = !base.IsOn;
			base.SpecialVariable = (base.IsOn ? 388859 : 0);
			if (base.IsOn)
			{
				location.playSound("fireball");
				location.playSound("bubbles");
				for (int i = 0; i < 13; i++)
				{
					addCauldronBubbles(-0.5f - (float)i * 0.2f);
				}
			}
			break;
		case "(F)1402":
			Game1.activeClickableMenu = new Billboard();
			return true;
		case "(F)RetroCatalogue":
			Utility.TryOpenShopMenu("RetroFurnitureCatalogue", location);
			break;
		case "(F)TrashCatalogue":
			Utility.TryOpenShopMenu("TrashFurnitureCatalogue", location);
			break;
		case "(F)JunimoCatalogue":
			Utility.TryOpenShopMenu("JunimoFurnitureCatalogue", location);
			break;
		case "(F)WizardCatalogue":
			if (!Game1.player.mailReceived.Contains("WizardCatalogue"))
			{
				Game1.player.mailReceived.Add("WizardCatalogue");
				Game1.activeClickableMenu = new LetterViewerMenu(Game1.content.LoadString("Strings\\1_6_Strings:WizardCatalogueLetter"))
				{
					whichBG = 2
				};
			}
			else
			{
				Utility.TryOpenShopMenu("WizardFurnitureCatalogue", location);
			}
			return true;
		case "(F)JojaCatalogue":
			if (!Game1.player.mailReceived.Contains("JojaThriveTerms"))
			{
				Game1.player.mailReceived.Add("JojaThriveTerms");
				Game1.activeClickableMenu = new LetterViewerMenu(Game1.content.LoadString("Strings\\1_6_Strings:JojaCatalogueDescriptionTerms"))
				{
					whichBG = 4
				};
			}
			else
			{
				Utility.TryOpenShopMenu("JojaFurnitureCatalogue", location);
			}
			return true;
		case "(F)1308":
			Utility.TryOpenShopMenu("Catalogue", location);
			return true;
		case "(F)1226":
			Utility.TryOpenShopMenu("Furniture Catalogue", location);
			return true;
		case "(F)1309":
			Game1.playSound("openBox");
			shakeTimer = 500;
			if (Game1.getMusicTrackName().Equals("sam_acoustic1"))
			{
				Game1.changeMusicTrack("none", track_interruptable: true);
			}
			else
			{
				Game1.changeMusicTrack("sam_acoustic1");
			}
			return true;
		}
		if (furniture_type.Value == 14 || furniture_type.Value == 16)
		{
			isOn.Value = !isOn.Value;
			initializeLightSource(tileLocation.Value);
			setFireplace(playSound: true, broadcast: true);
			return true;
		}
		if (GetSeatCapacity() > 0)
		{
			who.BeginSitting(this);
			return true;
		}
		return clicked(who);
	}

	public virtual void setFireplace(bool playSound = true, bool broadcast = false)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return;
		}
		if (isOn.Value)
		{
			if (base.lightSource == null)
			{
				initializeLightSource(tileLocation.Value);
			}
			if (base.lightSource != null && isOn.Value && !location.hasLightSource(base.lightSource.Id))
			{
				location.sharedLights.AddLight(base.lightSource.Clone());
			}
			if (playSound)
			{
				location.localSound("fireball");
			}
			AmbientLocationSounds.addSound(new Vector2(tileLocation.X, tileLocation.Y), 1);
		}
		else
		{
			if (playSound)
			{
				location.localSound("fireball");
			}
			base.performRemoveAction();
			AmbientLocationSounds.removeSound(new Vector2(tileLocation.X, tileLocation.Y));
		}
	}

	public virtual void AttemptRemoval(Action<Furniture> removal_action)
	{
		removal_action?.Invoke(this);
	}

	public virtual bool canBeRemoved(Farmer who)
	{
		if (!AllowLocalRemoval)
		{
			return false;
		}
		GameLocation location = Location;
		if (location == null)
		{
			return false;
		}
		if (HasSittingFarmers())
		{
			return false;
		}
		if (heldObject.Value != null)
		{
			return false;
		}
		Rectangle rectangle = GetBoundingBox();
		if (isPassable())
		{
			for (int i = rectangle.Left / 64; i < rectangle.Right / 64; i++)
			{
				for (int j = rectangle.Top / 64; j < rectangle.Bottom / 64; j++)
				{
					Furniture furnitureAt = location.GetFurnitureAt(new Vector2(i, j));
					if (furnitureAt != null && furnitureAt != this)
					{
						return false;
					}
					if (location.objects.ContainsKey(new Vector2(i, j)))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public override bool clicked(Farmer who)
	{
		Game1.haltAfterCheck = false;
		if (furniture_type.Value == 11 && who.ActiveObject != null && heldObject.Value == null)
		{
			return false;
		}
		if (heldObject.Value != null)
		{
			Object value = heldObject.Value;
			heldObject.Value = null;
			if (who.addItemToInventoryBool(value))
			{
				value.performRemoveAction();
				Game1.playSound("coin");
				return true;
			}
			heldObject.Value = value;
		}
		return false;
	}

	public virtual int GetSeatCapacity()
	{
		if (base.QualifiedItemId.Equals("(F)UprightPiano") || base.QualifiedItemId.Equals("(F)DarkPiano"))
		{
			return 1;
		}
		return furniture_type.Value switch
		{
			0 => 1, 
			1 => 2, 
			2 => defaultBoundingBox.Width / 64 - 1, 
			3 => 1, 
			_ => 0, 
		};
	}

	public virtual bool IsSeatHere(GameLocation location)
	{
		return location.furniture.Contains(this);
	}

	public virtual bool IsSittingHere(Farmer who)
	{
		return sittingFarmers.ContainsKey(who.UniqueMultiplayerID);
	}

	public virtual Vector2? GetSittingPosition(Farmer who, bool ignore_offsets = false)
	{
		if (sittingFarmers.TryGetValue(who.UniqueMultiplayerID, out var value))
		{
			return GetSeatPositions(ignore_offsets)[value];
		}
		return null;
	}

	public virtual bool HasSittingFarmers()
	{
		return sittingFarmers.Length > 0;
	}

	public virtual void RemoveSittingFarmer(Farmer farmer)
	{
		sittingFarmers.Remove(farmer.UniqueMultiplayerID);
	}

	public virtual int GetSittingFarmerCount()
	{
		return sittingFarmers.Length;
	}

	public virtual Rectangle GetSeatBounds()
	{
		Rectangle rectangle = GetBoundingBox();
		return new Rectangle(rectangle.X / 64, rectangle.Y / 64, rectangle.Width / 64, rectangle.Height / 64);
	}

	public virtual int GetSittingDirection()
	{
		if (Name.Contains("Stool"))
		{
			return Game1.player.FacingDirection;
		}
		if (base.QualifiedItemId.Equals("(F)UprightPiano") || base.QualifiedItemId.Equals("(F)DarkPiano"))
		{
			return 0;
		}
		return currentRotation.Value switch
		{
			0 => 2, 
			1 => 1, 
			2 => 0, 
			3 => 3, 
			_ => 2, 
		};
	}

	public virtual Vector2? AddSittingFarmer(Farmer who)
	{
		List<Vector2> seatPositions = GetSeatPositions();
		int value = -1;
		Vector2? result = null;
		float num = 96f;
		Vector2 standingPosition = who.getStandingPosition();
		for (int i = 0; i < seatPositions.Count; i++)
		{
			if (!sittingFarmers.Values.Contains(i))
			{
				float num2 = ((seatPositions[i] + new Vector2(0.5f, 0.5f)) * 64f - standingPosition).Length();
				if (num2 < num)
				{
					num = num2;
					result = seatPositions[i];
					value = i;
				}
			}
		}
		if (result.HasValue)
		{
			sittingFarmers[who.UniqueMultiplayerID] = value;
		}
		return result;
	}

	public virtual List<Vector2> GetSeatPositions(bool ignore_offsets = false)
	{
		List<Vector2> list = new List<Vector2>();
		if (base.QualifiedItemId.Equals("(F)UprightPiano") || base.QualifiedItemId.Equals("(F)DarkPiano"))
		{
			list.Add(TileLocation + new Vector2(1.5f, 0f));
		}
		switch (furniture_type.Value)
		{
		case 0:
			list.Add(TileLocation);
			break;
		case 1:
		{
			for (int l = 0; l < getTilesWide(); l++)
			{
				for (int m = 0; m < getTilesHigh(); m++)
				{
					list.Add(TileLocation + new Vector2(l, m));
				}
			}
			break;
		}
		case 2:
		{
			int num = defaultBoundingBox.Width / 64 - 1;
			switch (currentRotation.Value)
			{
			case 0:
			case 2:
			{
				list.Add(TileLocation + new Vector2(0.5f, 0f));
				for (int j = 1; j < num - 1; j++)
				{
					list.Add(TileLocation + new Vector2((float)j + 0.5f, 0f));
				}
				list.Add(TileLocation + new Vector2((float)(num - 1) + 0.5f, 0f));
				break;
			}
			case 1:
			{
				for (int k = 0; k < num; k++)
				{
					list.Add(TileLocation + new Vector2(1f, k));
				}
				break;
			}
			default:
			{
				for (int i = 0; i < num; i++)
				{
					list.Add(TileLocation + new Vector2(0f, i));
				}
				break;
			}
			}
			break;
		}
		case 3:
			if (currentRotation.Value == 0 || currentRotation.Value == 2)
			{
				list.Add(TileLocation + new Vector2(0.5f, 0f));
			}
			else if (currentRotation.Value == 1)
			{
				list.Add(TileLocation + new Vector2(1f, 0f));
			}
			else
			{
				list.Add(TileLocation + new Vector2(0f, 0f));
			}
			break;
		}
		return list;
	}

	public bool timeToTurnOnLights()
	{
		if (Location != null)
		{
			if (!Location.IsRainingHere())
			{
				return Game1.timeOfDay >= Game1.getTrulyDarkTime(Location) - 100;
			}
			return true;
		}
		return false;
	}

	public override void DayUpdate()
	{
		base.DayUpdate();
		sittingFarmers.Clear();
		if (Location.IsRainingHere())
		{
			addLights();
		}
		else if (!timeToTurnOnLights() || Game1.newDay)
		{
			removeLights();
		}
		else
		{
			addLights();
		}
		RemoveLightGlow();
		if (Game1.IsMasterGame && Game1.season == Season.Winter && Game1.dayOfMonth == 25 && (furniture_type.Value == 11 || furniture_type.Value == 5) && heldObject.Value != null)
		{
			if (heldObject.Value.QualifiedItemId == "(O)223" && !Game1.player.mailReceived.Contains("CookiePresent_year" + Game1.year))
			{
				heldObject.Value = ItemRegistry.Create<Object>("(O)MysteryBox");
				Game1.player.mailReceived.Add("CookiePresent_year" + Game1.year);
			}
			else if (heldObject.Value.Category == -6 && !Game1.player.mailReceived.Contains("MilkPresent_year" + Game1.year))
			{
				heldObject.Value = ItemRegistry.Create<Object>("(O)MysteryBox");
				Game1.player.mailReceived.Add("MilkPresent_year" + Game1.year);
			}
		}
	}

	public virtual void AddLightGlow()
	{
		GameLocation location = Location;
		if (location != null && !lightGlowPosition.HasValue)
		{
			Vector2 vector = new Vector2(boundingBox.X + 32, boundingBox.Y + 64);
			if (!location.lightGlows.Contains(vector))
			{
				lightGlowPosition = vector;
				location.lightGlows.Add(vector);
			}
		}
	}

	public virtual void RemoveLightGlow()
	{
		GameLocation location = Location;
		if (location != null)
		{
			if (lightGlowPosition.HasValue && location.lightGlows.Contains(lightGlowPosition.Value))
			{
				location.lightGlows.Remove(lightGlowPosition.Value);
			}
			location.lightGlowLayerCache.Clear();
			lightGlowPosition = null;
		}
	}

	/// <inheritdoc />
	public override void actionOnPlayerEntry()
	{
		base.actionOnPlayerEntry();
		actionOnPlayerEntryOrPlacement(Location, dropDown: false);
		if (Location == null || !base.QualifiedItemId.Equals("(F)BirdHouse") || !Location.isOutdoors.Value || Game1.isRaining || Game1.timeOfDay >= Game1.getStartingToGetDarkTime(Location))
		{
			return;
		}
		Random random = Utility.CreateDaySaveRandom(TileLocation.X * 74797f, TileLocation.Y * 77f, Game1.timeOfDay * 99);
		int num = (int)Game1.stats.Get("childrenTurnedToDoves");
		if (random.NextDouble() < 0.06)
		{
			Location.instantiateCrittersList();
			int num2 = ((Game1.season == Season.Fall) ? 45 : 25);
			int num3 = 0;
			if (Game1.random.NextBool() && Game1.MasterPlayer.mailReceived.Contains("Farm_Eternal"))
			{
				num2 = ((Game1.season == Season.Fall) ? 135 : 125);
			}
			if (num2 == 25 && Game1.random.NextDouble() < 0.05)
			{
				num2 = 165;
			}
			if (random.NextDouble() < (double)num * 0.08)
			{
				num2 = 175;
				num3 = 12;
			}
			Location.critters.Add(new Birdie(TileLocation * 64f + new Vector2(32f, 64 + Game1.random.Next(3) * 4 + num3), -160f, num2, stationary: true));
		}
	}

	/// <summary>Handle the player entering the location containing the object, or the furniture being placed.</summary>
	/// <param name="environment">The location containing the object.</param>
	/// <param name="dropDown">Whether the item was just placed (instead of the player entering the location with it already placed).</param>
	public virtual void actionOnPlayerEntryOrPlacement(GameLocation environment, bool dropDown)
	{
		if (Location == null)
		{
			Location = environment;
		}
		RemoveLightGlow();
		removeLights();
		if (furniture_type.Value == 14 || furniture_type.Value == 16)
		{
			setFireplace(playSound: false);
		}
		if (timeToTurnOnLights())
		{
			addLights();
			if (heldObject.Value is Furniture furniture)
			{
				furniture.addLights();
			}
		}
		if (base.QualifiedItemId == "(F)1971" && !dropDown)
		{
			environment.instantiateCrittersList();
			environment.addCritter(new Butterfly(environment, environment.getRandomTile()).setStayInbounds(stayInbounds: true));
			while (Game1.random.NextBool())
			{
				environment.addCritter(new Butterfly(environment, environment.getRandomTile()).setStayInbounds(stayInbounds: true));
			}
		}
	}

	/// <inheritdoc />
	public override bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return false;
		}
		if (!(dropInItem is Object obj))
		{
			return false;
		}
		if (IsTable() && heldObject.Value == null && !obj.bigCraftable.Value && !(obj is Wallpaper) && (!(obj is Furniture furniture) || (furniture.getTilesWide() == 1 && furniture.getTilesHigh() == 1)))
		{
			if (!probe)
			{
				heldObject.Value = (Object)obj.getOne();
				heldObject.Value.Location = Location;
				heldObject.Value.TileLocation = tileLocation.Value;
				heldObject.Value.boundingBox.X = boundingBox.X;
				heldObject.Value.boundingBox.Y = boundingBox.Y;
				heldObject.Value.performDropDownAction(who);
				location.playSound("woodyStep");
				if (who != null)
				{
					who.reduceActiveItemByOne();
					if (returnFalseIfItemConsumed)
					{
						return false;
					}
				}
			}
			return true;
		}
		return false;
	}

	/// <summary>Auto-generate a default light source ID for this furniture when placed.</summary>
	protected virtual string GenerateLightSourceId()
	{
		return base.GenerateLightSourceId(tileLocation.Value);
	}

	private bool isLampStyleLightSource()
	{
		if (furniture_type.Value != 7 && furniture_type.Value != 17)
		{
			return base.QualifiedItemId == "(F)1369";
		}
		return true;
	}

	public virtual void addLights()
	{
		GameLocation location = Location;
		if (location == null)
		{
			return;
		}
		if (heldObject.Value is Furniture furniture)
		{
			heldObject.Value.Location = Location;
			furniture.addLights();
		}
		if (isLampStyleLightSource())
		{
			sourceRect.Value = defaultSourceRect.Value;
			sourceIndexOffset.Value = 1;
			if (base.lightSource == null)
			{
				base.lightSource = new LightSource(GenerateLightSourceId(), 4, new Vector2(boundingBox.X + 32, boundingBox.Y + ((furniture_type.Value == 7) ? (-64) : 64)), (furniture_type.Value == 7) ? 2f : 1f, (base.QualifiedItemId == "(F)1369") ? (Color.RoyalBlue * 0.7f) : Color.Black, LightSource.LightContext.None, 0L, location.NameOrUniqueName);
				location.sharedLights.AddLight(base.lightSource.Clone());
			}
		}
		else if (base.QualifiedItemId == "(F)1440")
		{
			base.lightSource = new LightSource(GenerateLightSourceId(), 4, new Vector2(boundingBox.X + 96, (float)boundingBox.Y - 32f), 1.5f, Color.Black, LightSource.LightContext.None, 0L, location.NameOrUniqueName);
			location.sharedLights.AddLight(base.lightSource.Clone());
		}
		else if (furniture_type.Value == 13)
		{
			sourceRect.Value = defaultSourceRect.Value;
			sourceIndexOffset.Value = 1;
			RemoveLightGlow();
		}
		else if (this is FishTankFurniture && base.lightSource == null)
		{
			string value = GenerateLightSourceId();
			Vector2 position = new Vector2(tileLocation.X * 64f + 32f + 2f, tileLocation.Y * 64f + 12f);
			for (int i = 0; i < getTilesWide(); i++)
			{
				base.lightSource = new LightSource($"{value}_tile{i}", 8, position, 2f, Color.Black, LightSource.LightContext.None, 0L, location.NameOrUniqueName);
				location.sharedLights.AddLight(base.lightSource.Clone());
				position.X += 64f;
			}
		}
	}

	public virtual void removeLights()
	{
		GameLocation location = Location;
		if (heldObject.Value is Furniture furniture)
		{
			furniture.removeLights();
		}
		if (isLampStyleLightSource() || base.QualifiedItemId == "(F)1440")
		{
			sourceRect.Value = defaultSourceRect.Value;
			sourceIndexOffset.Value = 0;
			location?.removeLightSource(GenerateLightSourceId());
			base.lightSource = null;
		}
		else if (furniture_type.Value == 13)
		{
			if (location != null && location.IsRainingHere())
			{
				sourceRect.Value = defaultSourceRect.Value;
				sourceIndexOffset.Value = 1;
			}
			else
			{
				sourceRect.Value = defaultSourceRect.Value;
				sourceIndexOffset.Value = 0;
				AddLightGlow();
			}
		}
		else if (this is FishTankFurniture)
		{
			string value = GenerateLightSourceId();
			for (int i = 0; i < getTilesWide(); i++)
			{
				location?.removeLightSource($"{value}_tile{i}");
			}
			base.lightSource = null;
		}
	}

	/// <inheritdoc />
	public override bool minutesElapsed(int minutes)
	{
		if (Location == null)
		{
			return false;
		}
		if (timeToTurnOnLights())
		{
			addLights();
		}
		else
		{
			removeLights();
		}
		return false;
	}

	public override void performRemoveAction()
	{
		removeLights();
		if (Location != null)
		{
			if (furniture_type.Value == 14 || furniture_type.Value == 16)
			{
				isOn.Value = false;
				setFireplace(playSound: false);
			}
			RemoveLightGlow();
			base.performRemoveAction();
			if (furniture_type.Value == 14 || furniture_type.Value == 16)
			{
				base.lightSource = null;
			}
			if (base.QualifiedItemId == "(F)1309" && Game1.getMusicTrackName().Equals("sam_acoustic1"))
			{
				Game1.changeMusicTrack("none", track_interruptable: true);
			}
			sittingFarmers.Clear();
		}
	}

	public virtual void rotate()
	{
		if (rotations.Value >= 2)
		{
			int num = ((rotations.Value == 4) ? 1 : 2);
			currentRotation.Value += num;
			currentRotation.Value %= 4;
			updateRotation();
		}
	}

	public virtual void updateRotation()
	{
		flipped.Value = false;
		if (currentRotation.Value > 0)
		{
			Point point = furniture_type.Value switch
			{
				2 => new Point(-1, 1), 
				5 => new Point(-1, 0), 
				3 => new Point(-1, 1), 
				_ => Point.Zero, 
			};
			bool flag = (IsTable() || furniture_type.Value == 12 || base.QualifiedItemId == "(F)724" || base.QualifiedItemId == "(F)727") && !base.name.Contains("End Table") && !base.name.Contains("EndTable");
			bool flag2 = defaultBoundingBox.Width != defaultBoundingBox.Height;
			if (flag && currentRotation.Value == 2)
			{
				currentRotation.Value = 1;
			}
			if (flag2)
			{
				int height = boundingBox.Height;
				switch (currentRotation.Value)
				{
				case 0:
				case 2:
					boundingBox.Height = defaultBoundingBox.Height;
					boundingBox.Width = defaultBoundingBox.Width;
					break;
				case 1:
				case 3:
					boundingBox.Height = boundingBox.Width + point.X * 64;
					boundingBox.Width = height + point.Y * 64;
					break;
				}
			}
			Point point2 = ((furniture_type.Value == 12) ? new Point(1, -1) : Point.Zero);
			if (flag2)
			{
				switch (currentRotation.Value)
				{
				case 0:
					sourceRect.Value = defaultSourceRect.Value;
					break;
				case 1:
					sourceRect.Value = new Rectangle(defaultSourceRect.X + defaultSourceRect.Width, defaultSourceRect.Y, defaultSourceRect.Height - 16 + point.Y * 16 + point2.X * 16, defaultSourceRect.Width + 16 + point.X * 16 + point2.Y * 16);
					break;
				case 2:
					sourceRect.Value = new Rectangle(defaultSourceRect.X + defaultSourceRect.Width + defaultSourceRect.Height - 16 + point.Y * 16 + point2.X * 16, defaultSourceRect.Y, defaultSourceRect.Width, defaultSourceRect.Height);
					break;
				case 3:
					sourceRect.Value = new Rectangle(defaultSourceRect.X + defaultSourceRect.Width, defaultSourceRect.Y, defaultSourceRect.Height - 16 + point.Y * 16 + point2.X * 16, defaultSourceRect.Width + 16 + point.X * 16 + point2.Y * 16);
					flipped.Value = true;
					break;
				}
			}
			else
			{
				flipped.Value = currentRotation.Value == 3;
				if (rotations.Value == 2)
				{
					sourceRect.Value = new Rectangle(defaultSourceRect.X + ((currentRotation.Value == 2) ? 1 : 0) * defaultSourceRect.Width, defaultSourceRect.Y, defaultSourceRect.Width, defaultSourceRect.Height);
				}
				else
				{
					sourceRect.Value = new Rectangle(defaultSourceRect.X + ((currentRotation.Value == 3) ? 1 : currentRotation.Value) * defaultSourceRect.Width, defaultSourceRect.Y, defaultSourceRect.Width, defaultSourceRect.Height);
				}
			}
			if (flag && currentRotation.Value == 1)
			{
				currentRotation.Value = 2;
			}
		}
		else
		{
			sourceRect.Value = defaultSourceRect.Value;
			boundingBox.Value = defaultBoundingBox.Value;
		}
		updateDrawPosition();
	}

	public virtual bool isGroundFurniture()
	{
		if (furniture_type.Value != 13 && furniture_type.Value != 6 && furniture_type.Value != 17)
		{
			return furniture_type.Value != 13;
		}
		return false;
	}

	/// <inheritdoc />
	public override bool canBeGivenAsGift()
	{
		return false;
	}

	public static Furniture GetFurnitureInstance(string itemId, Vector2? position = null)
	{
		if (!position.HasValue)
		{
			position = Vector2.Zero;
		}
		switch (itemId)
		{
		case "1466":
		case "1468":
		case "1680":
		case "2326":
		case "RetroTV":
			return new TV(itemId, position.Value);
		default:
		{
			string text = ArgUtility.Get(getData(itemId), 1);
			switch (text)
			{
			case "fishtank":
				return new FishTankFurniture(itemId, position.Value);
			case "dresser":
				return new StorageFurniture(itemId, position.Value);
			case "randomized_plant":
				return new RandomizedPlantFurniture(itemId, position.Value);
			default:
				if (text?.StartsWith("bed") ?? false)
				{
					return new BedFurniture(itemId, position.Value);
				}
				return new Furniture(itemId, position.Value);
			}
		}
		}
	}

	public virtual bool IsCloseEnoughToFarmer(Farmer f, int? override_tile_x = null, int? override_tile_y = null)
	{
		Rectangle rectangle = new Rectangle((int)tileLocation.X * 64, (int)tileLocation.Y * 64, getTilesWide() * 64, getTilesHigh() * 64);
		if (override_tile_x.HasValue)
		{
			rectangle.X = override_tile_x.Value * 64;
		}
		if (override_tile_y.HasValue)
		{
			rectangle.Y = override_tile_y.Value * 64;
		}
		rectangle.Inflate(96, 96);
		return rectangle.Contains(Game1.player.StandingPixel);
	}

	public virtual int GetModifiedWallTilePosition(GameLocation l, int tile_x, int tile_y)
	{
		if (isGroundFurniture())
		{
			return tile_y;
		}
		if (l != null)
		{
			if (l is DecoratableLocation decoratableLocation)
			{
				int wallTopY = decoratableLocation.GetWallTopY(tile_x, tile_y);
				if (wallTopY != -1)
				{
					return wallTopY;
				}
			}
			return tile_y;
		}
		return tile_y;
	}

	public override bool canBePlacedHere(GameLocation l, Vector2 tile, CollisionMask collisionMask = CollisionMask.All, bool showError = false)
	{
		if (!l.CanPlaceThisFurnitureHere(this))
		{
			return false;
		}
		if (!isGroundFurniture())
		{
			tile.Y = GetModifiedWallTilePosition(l, (int)tile.X, (int)tile.Y);
		}
		CollisionMask collisionMask2 = CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.TerrainFeatures;
		bool flag = isPassable();
		if (flag)
		{
			collisionMask2 |= CollisionMask.Characters | CollisionMask.Farmers;
		}
		collisionMask &= ~(CollisionMask.Furniture | CollisionMask.Objects);
		int tilesWide = getTilesWide();
		int tilesHigh = getTilesHigh();
		for (int i = 0; i < tilesWide; i++)
		{
			for (int j = 0; j < tilesHigh; j++)
			{
				Vector2 vector = new Vector2(tile.X + (float)i, tile.Y + (float)j);
				Vector2 vector2 = new Vector2(vector.X + 0.5f, vector.Y + 0.5f) * 64f;
				if (!l.isTilePlaceable(vector, flag))
				{
					return false;
				}
				foreach (Furniture item in l.furniture)
				{
					if (item.furniture_type.Value == 11 && item.GetBoundingBox().Contains((int)vector2.X, (int)vector2.Y) && item.heldObject.Value == null && tilesWide == 1 && tilesHigh == 1)
					{
						return true;
					}
					if ((item.furniture_type.Value != 12 || furniture_type.Value == 12) && item.GetBoundingBox().Contains((int)vector2.X, (int)vector2.Y) && !item.AllowPlacementOnThisTile((int)tile.X + i, (int)tile.Y + j))
					{
						return false;
					}
				}
				if (l.objects.TryGetValue(vector, out var value) && (!value.isPassable() || !isPassable()))
				{
					return false;
				}
				if (!isGroundFurniture())
				{
					if (l.IsTileOccupiedBy(vector, collisionMask, collisionMask2))
					{
						return false;
					}
					continue;
				}
				if (furniture_type.Value == 15 && j == 0)
				{
					if (l.IsTileOccupiedBy(vector, collisionMask, collisionMask2))
					{
						return false;
					}
					continue;
				}
				if (l.IsTileBlockedBy(vector, collisionMask, collisionMask2))
				{
					return false;
				}
				if (l.terrainFeatures.GetValueOrDefault(vector) is HoeDirt { crop: not null })
				{
					return false;
				}
			}
		}
		if (GetAdditionalFurniturePlacementStatus(l, (int)tile.X * 64, (int)tile.Y * 64) != 0)
		{
			return false;
		}
		return true;
	}

	public virtual void updateDrawPosition()
	{
		drawPosition.Value = new Vector2(boundingBox.X, boundingBox.Y - (sourceRect.Height * 4 - boundingBox.Height));
	}

	public virtual int getTilesWide()
	{
		return boundingBox.Width / 64;
	}

	public virtual int getTilesHigh()
	{
		return boundingBox.Height / 64;
	}

	/// <inheritdoc />
	public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
	{
		if (!isGroundFurniture())
		{
			y = GetModifiedWallTilePosition(location, x / 64, y / 64) * 64;
		}
		if (GetAdditionalFurniturePlacementStatus(location, x, y, who) != 0)
		{
			return false;
		}
		Vector2 vector = new Vector2(x / 64, y / 64);
		if (TileLocation != vector)
		{
			TileLocation = vector;
		}
		else
		{
			RecalculateBoundingBox();
		}
		foreach (Furniture item in location.furniture)
		{
			if (item.furniture_type.Value == 11 && item.heldObject.Value == null && item.GetBoundingBox().Intersects(boundingBox.Value))
			{
				item.performObjectDropInAction(this, probe: false, who ?? Game1.player);
				return true;
			}
		}
		return base.placementAction(location, x, y, who);
	}

	/// <summary>Get the reason the furniture can't be placed at a given position, if applicable.</summary>
	/// <param name="location">The location in which the furniture is being placed.</param>
	/// <param name="x">The X pixel position at which the furniture is being placed.</param>
	/// <param name="y">The Y pixel position at which the furniture is being placed.</param>
	/// <param name="who">The player placing the furniture, if applicable.</param>
	/// <returns>
	///   Returns one of these values:
	///   <list type="bullet">
	///     <item><description>0: valid placement.</description></item>
	///     <item><description>1: the object is a wall placed object but isn't being placed on a wall.</description></item>
	///     <item><description>2: the object can't be placed here due to the tile being marked as not furnishable.</description></item>
	///     <item><description>3: the object isn't a wall placed object, but is trying to be placed on a wall.</description></item>
	///     <item><description>4: the current location isn't decorable.</description></item>
	///     <item><description>-1: general fail condition.</description></item>
	///   </list>
	/// </returns>
	public virtual int GetAdditionalFurniturePlacementStatus(GameLocation location, int x, int y, Farmer who = null)
	{
		if (location.CanPlaceThisFurnitureHere(this))
		{
			Point point = new Point(x / 64, y / 64);
			tileLocation.Value = new Vector2(point.X, point.Y);
			bool flag = false;
			if (furniture_type.Value == 6 || furniture_type.Value == 17 || furniture_type.Value == 13 || base.QualifiedItemId == "(F)1293")
			{
				int num = ((base.QualifiedItemId == "(F)1293") ? 3 : 0);
				bool flag2 = false;
				if (location is DecoratableLocation decoratableLocation)
				{
					if ((furniture_type.Value == 6 || furniture_type.Value == 17 || furniture_type.Value == 13 || num != 0) && decoratableLocation.isTileOnWall(point.X, point.Y - num) && decoratableLocation.GetWallTopY(point.X, point.Y - num) + num == point.Y)
					{
						flag2 = true;
					}
					else if (!isGroundFurniture() && decoratableLocation.isTileOnWall(point.X, point.Y - 1) && decoratableLocation.GetWallTopY(point.X, point.Y) + 1 == point.Y)
					{
						flag2 = true;
					}
				}
				if (!flag2)
				{
					return 1;
				}
				flag = true;
			}
			int num2 = getTilesHigh();
			if (furniture_type.Value == 6 && num2 > 2)
			{
				num2 = 2;
			}
			for (int i = point.X; i < point.X + getTilesWide(); i++)
			{
				for (int j = point.Y; j < point.Y + num2; j++)
				{
					if (location.doesTileHaveProperty(i, j, "NoFurniture", "Back") != null)
					{
						return 2;
					}
					if (!flag && location is DecoratableLocation decoratableLocation2 && decoratableLocation2.isTileOnWall(i, j))
					{
						if (!(this is BedFurniture) || j != point.Y)
						{
							return 3;
						}
						continue;
					}
					int tileIndexAt = location.getTileIndexAt(i, j, "Buildings");
					if (tileIndexAt != -1 && (!(location is IslandFarmHouse) || tileIndexAt < 192 || tileIndexAt > 194 || !(location.getTileSheetIDAt(i, j, "Buildings") == "untitled tile sheet")))
					{
						return -1;
					}
				}
			}
			return 0;
		}
		return 4;
	}

	public override bool isPassable()
	{
		if (furniture_type.Value == 12)
		{
			return true;
		}
		return base.isPassable();
	}

	public override bool isPlaceable()
	{
		return true;
	}

	public virtual bool AllowPlacementOnThisTile(int tile_x, int tile_y)
	{
		return false;
	}

	/// <inheritdoc />
	public override Rectangle GetBoundingBoxAt(int x, int y)
	{
		if (isTemporarilyInvisible)
		{
			return Rectangle.Empty;
		}
		return boundingBox.Value;
	}

	protected static Rectangle getDefaultSourceRectForType(ParsedItemData itemData, int type, Texture2D texture = null)
	{
		int spriteWidth;
		int spriteHeight;
		switch (type)
		{
		case 0:
			spriteWidth = 1;
			spriteHeight = 2;
			break;
		case 1:
			spriteWidth = 2;
			spriteHeight = 2;
			break;
		case 2:
			spriteWidth = 3;
			spriteHeight = 2;
			break;
		case 3:
			spriteWidth = 2;
			spriteHeight = 2;
			break;
		case 4:
			spriteWidth = 2;
			spriteHeight = 2;
			break;
		case 5:
			spriteWidth = 5;
			spriteHeight = 3;
			break;
		case 6:
			spriteWidth = 2;
			spriteHeight = 2;
			break;
		case 17:
			spriteWidth = 1;
			spriteHeight = 2;
			break;
		case 7:
			spriteWidth = 1;
			spriteHeight = 3;
			break;
		case 8:
			spriteWidth = 1;
			spriteHeight = 2;
			break;
		case 10:
			spriteWidth = 2;
			spriteHeight = 3;
			break;
		case 11:
			spriteWidth = 2;
			spriteHeight = 3;
			break;
		case 12:
			spriteWidth = 3;
			spriteHeight = 2;
			break;
		case 13:
			spriteWidth = 1;
			spriteHeight = 2;
			break;
		case 14:
			spriteWidth = 2;
			spriteHeight = 5;
			break;
		case 16:
			spriteWidth = 1;
			spriteHeight = 2;
			break;
		default:
			spriteWidth = 1;
			spriteHeight = 2;
			break;
		}
		return getDefaultSourceRect(itemData, spriteWidth, spriteHeight, texture);
	}

	protected static Rectangle getDefaultSourceRect(ParsedItemData itemData, int spriteWidth, int spriteHeight, Texture2D texture = null)
	{
		texture = texture ?? itemData.GetTexture();
		return new Rectangle(itemData.SpriteIndex * 16 % texture.Width, itemData.SpriteIndex * 16 / texture.Width * 16, spriteWidth * 16, spriteHeight * 16);
	}

	protected virtual Rectangle getDefaultBoundingBoxForType(int type)
	{
		int num;
		int num2;
		switch (type)
		{
		case 0:
			num = 1;
			num2 = 1;
			break;
		case 1:
			num = 2;
			num2 = 1;
			break;
		case 2:
			num = 3;
			num2 = 1;
			break;
		case 3:
			num = 2;
			num2 = 1;
			break;
		case 4:
			num = 2;
			num2 = 1;
			break;
		case 5:
			num = 5;
			num2 = 2;
			break;
		case 6:
			num = 2;
			num2 = 2;
			break;
		case 17:
			num = 1;
			num2 = 2;
			break;
		case 7:
			num = 1;
			num2 = 1;
			break;
		case 8:
			num = 1;
			num2 = 1;
			break;
		case 10:
			num = 2;
			num2 = 1;
			break;
		case 11:
			num = 2;
			num2 = 2;
			break;
		case 12:
			num = 3;
			num2 = 2;
			break;
		case 13:
			num = 1;
			num2 = 2;
			break;
		case 14:
			num = 2;
			num2 = 1;
			break;
		case 16:
			num = 1;
			num2 = 1;
			break;
		default:
			num = 1;
			num2 = 1;
			break;
		}
		return new Rectangle((int)tileLocation.X * 64, (int)tileLocation.Y * 64, num * 64, num2 * 64);
	}

	public static int getTypeNumberFromName(string typeName)
	{
		if (typeName.StartsWithIgnoreCase("bed"))
		{
			return 15;
		}
		return typeName.ToLower() switch
		{
			"chair" => 0, 
			"bench" => 1, 
			"couch" => 2, 
			"armchair" => 3, 
			"dresser" => 4, 
			"long table" => 5, 
			"painting" => 6, 
			"lamp" => 7, 
			"decor" => 8, 
			"bookcase" => 10, 
			"table" => 11, 
			"rug" => 12, 
			"window" => 13, 
			"fireplace" => 14, 
			"torch" => 16, 
			"sconce" => 17, 
			_ => 9, 
		};
	}

	/// <inheritdoc />
	public override int salePrice(bool ignoreProfitMargins = false)
	{
		return price.Value;
	}

	public override int maximumStackSize()
	{
		return 1;
	}

	protected virtual float getScaleSize()
	{
		int num = defaultSourceRect.Width / 16;
		int num2 = defaultSourceRect.Height / 16;
		if (num >= 7)
		{
			return 0.5f;
		}
		if (num >= 6)
		{
			return 0.66f;
		}
		if (num >= 5)
		{
			return 0.75f;
		}
		if (num2 >= 5)
		{
			return 0.8f;
		}
		if (num2 >= 3)
		{
			return 1f;
		}
		if (num <= 2)
		{
			return 2f;
		}
		if (num <= 4)
		{
			return 1f;
		}
		return 0.1f;
	}

	public override void updateWhenCurrentLocation(GameTime time)
	{
		if (Location == null)
		{
			return;
		}
		if (Game1.IsMasterGame && sittingFarmers.Length > 0)
		{
			List<long> list = null;
			foreach (long key in sittingFarmers.Keys)
			{
				if (!Game1.player.team.playerIsOnline(key))
				{
					if (list == null)
					{
						list = new List<long>();
					}
					list.Add(key);
				}
			}
			if (list != null)
			{
				foreach (long item in list)
				{
					sittingFarmers.Remove(item);
				}
			}
		}
		if (shakeTimer > 0)
		{
			shakeTimer -= time.ElapsedGameTime.Milliseconds;
		}
		if (base.IsOn && base.SpecialVariable == 388859)
		{
			lastNoteBlockSoundTime += (int)time.ElapsedGameTime.TotalMilliseconds;
			if (lastNoteBlockSoundTime > 500)
			{
				lastNoteBlockSoundTime = 0;
				addCauldronBubbles();
			}
		}
	}

	private void addCauldronBubbles(float speed = -0.5f)
	{
		Location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(372, 1956, 10, 10), TileLocation * 64f + new Vector2(41.6f, -21f) + new Vector2(Game1.random.Next(-12, 21), Game1.random.Next(16)), flipped: false, 0.002f, Color.Lime)
		{
			alphaFade = 0.001f - speed / 300f,
			alpha = 0.75f,
			motion = new Vector2(0f, speed),
			acceleration = new Vector2(0f, 0f),
			interval = 99999f,
			layerDepth = (float)(boundingBox.Bottom - 3 - Game1.random.Next(5)) / 10000f,
			scale = 3f,
			scaleChange = 0.01f,
			rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f
		});
	}

	public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
	{
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		Rectangle rectangle = dataOrErrorItem.GetSourceRect();
		spriteBatch.Draw(dataOrErrorItem.GetTexture(), location + new Vector2(32f, 32f), dataOrErrorItem.GetSourceRect(), color * transparency, 0f, new Vector2(rectangle.Width / 2, rectangle.Height / 2), 1f * getScaleSize() * scaleSize, SpriteEffects.None, layerDepth);
		DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
	}

	public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		if (isTemporarilyInvisible)
		{
			return;
		}
		Rectangle value = sourceRect.Value;
		value.X += value.Width * sourceIndexOffset.Value;
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		Texture2D texture = dataOrErrorItem.GetTexture();
		string textureName = dataOrErrorItem.TextureName;
		if (dataOrErrorItem.IsErrorItem)
		{
			value = dataOrErrorItem.GetSourceRect();
		}
		if (_frontTextureName == null)
		{
			_frontTextureName = new Dictionary<string, string>();
		}
		if (isDrawingLocationFurniture)
		{
			if (!_frontTextureName.TryGetValue(textureName, out var value2))
			{
				value2 = textureName + "Front";
				_frontTextureName[textureName] = value2;
			}
			Texture2D texture2D = null;
			if (HasSittingFarmers() || base.SpecialVariable == 388859)
			{
				try
				{
					texture2D = Game1.content.Load<Texture2D>(value2);
				}
				catch
				{
					texture2D = null;
				}
			}
			Vector2 position = Game1.GlobalToLocal(Game1.viewport, drawPosition.Value + ((shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero));
			SpriteEffects effects = (flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
			Color color = Color.White * alpha;
			if (HasSittingFarmers())
			{
				spriteBatch.Draw(texture, position, value, color, 0f, Vector2.Zero, 4f, effects, (float)(boundingBox.Value.Top + 16) / 10000f);
				if (texture2D != null && value.Right <= texture2D.Width && value.Bottom <= texture2D.Height)
				{
					spriteBatch.Draw(texture2D, position, value, color, 0f, Vector2.Zero, 4f, effects, (float)(boundingBox.Value.Bottom - 8) / 10000f);
				}
			}
			else
			{
				spriteBatch.Draw(texture, position, value, color, 0f, Vector2.Zero, 4f, effects, (furniture_type.Value == 12) ? (2E-09f + tileLocation.Y / 100000f) : ((float)(boundingBox.Value.Bottom - ((furniture_type.Value == 6 || furniture_type.Value == 17 || furniture_type.Value == 13) ? 48 : 8)) / 10000f));
				if (base.SpecialVariable == 388859 && texture2D != null && value.Right <= texture2D.Width && value.Bottom <= texture2D.Height)
				{
					spriteBatch.Draw(texture2D, position, value, color, 0f, Vector2.Zero, 4f, effects, (float)(boundingBox.Value.Bottom - 2) / 10000f);
				}
			}
		}
		else
		{
			spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 - (value.Height * 4 - boundingBox.Height) + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))), value, Color.White * alpha, 0f, Vector2.Zero, 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (furniture_type.Value == 12) ? (2E-09f + tileLocation.Y / 100000f) : ((float)(boundingBox.Value.Bottom - ((furniture_type.Value == 6 || furniture_type.Value == 17 || furniture_type.Value == 13) ? 48 : 8)) / 10000f));
		}
		if (heldObject.Value != null)
		{
			if (heldObject.Value is Furniture furniture)
			{
				furniture.drawAtNonTileSpot(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - furniture.sourceRect.Height * 4 - (drawHeldObjectLow.Value ? (-16) : 16))), (float)(boundingBox.Bottom - 7) / 10000f, alpha);
			}
			else
			{
				ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
				spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - (drawHeldObjectLow.Value ? 32 : 85))) + new Vector2(32f, 53f), Game1.shadowTexture.Bounds, Color.White * alpha, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, (float)boundingBox.Bottom / 10000f);
				if (heldObject.Value is ColoredObject)
				{
					heldObject.Value.drawInMenu(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - (drawHeldObjectLow.Value ? 32 : 85))), 1f, 1f, (float)(boundingBox.Bottom + 1) / 10000f, StackDrawType.Hide, Color.White, drawShadow: false);
				}
				else
				{
					spriteBatch.Draw(dataOrErrorItem2.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - (drawHeldObjectLow.Value ? 32 : 85))), dataOrErrorItem2.GetSourceRect(), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBox.Bottom + 1) / 10000f);
				}
			}
		}
		if (isOn.Value && furniture_type.Value == 14)
		{
			Rectangle boundingBoxAt = GetBoundingBoxAt(x, y);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 12, boundingBox.Center.Y - 64)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBoxAt.Bottom - 2) / 10000f);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32 - 4, boundingBox.Center.Y - 64)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 2047) + (double)(y * 98)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBoxAt.Bottom - 1) / 10000f);
		}
		else if (isOn.Value && furniture_type.Value == 16)
		{
			Rectangle boundingBoxAt2 = GetBoundingBoxAt(x, y);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 20, (float)boundingBox.Center.Y - 105.6f)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBoxAt2.Bottom - 2) / 10000f);
		}
		if (Game1.debugMode)
		{
			spriteBatch.DrawString(Game1.smallFont, base.QualifiedItemId, Game1.GlobalToLocal(Game1.viewport, drawPosition.Value), Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
		}
	}

	public virtual void drawAtNonTileSpot(SpriteBatch spriteBatch, Vector2 location, float layerDepth, float alpha = 1f)
	{
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		Rectangle value = sourceRect.Value;
		value.X += value.Width * sourceIndexOffset.Value;
		if (dataOrErrorItem.IsErrorItem)
		{
			value = dataOrErrorItem.GetSourceRect();
		}
		spriteBatch.Draw(dataOrErrorItem.GetTexture(), location, value, Color.White * alpha, 0f, Vector2.Zero, 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
	}

	public virtual int GetAdditionalTilePropertyRadius()
	{
		return 0;
	}

	public virtual bool DoesTileHaveProperty(int tile_x, int tile_y, string property_name, string layer_name, ref string property_value)
	{
		return false;
	}

	public virtual bool IntersectsForCollision(Rectangle rect)
	{
		return GetBoundingBox().Intersects(rect);
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Furniture(base.ItemId, tileLocation.Value);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is Furniture furniture)
		{
			drawPosition.Value = furniture.drawPosition.Value;
			defaultBoundingBox.Value = furniture.defaultBoundingBox.Value;
			boundingBox.Value = furniture.boundingBox.Value;
			isOn.Value = false;
			rotations.Value = furniture.rotations.Value;
			currentRotation.Value = furniture.currentRotation.Value - ((rotations.Value == 4) ? 1 : 2);
			rotate();
		}
	}
}
