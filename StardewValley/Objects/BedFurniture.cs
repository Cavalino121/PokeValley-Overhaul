using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Network;

namespace StardewValley.Objects;

public class BedFurniture : Furniture
{
	public enum BedType
	{
		Any = -1,
		Single,
		Double,
		Child
	}

	public static string DEFAULT_BED_INDEX = "2048";

	public static string DOUBLE_BED_INDEX = "2052";

	public static string CHILD_BED_INDEX = "2076";

	[XmlIgnore]
	public int bedTileOffset;

	[XmlIgnore]
	protected bool _alreadyAttempingRemoval;

	[XmlIgnore]
	public static bool ignoreContextualBedSpotOffset = false;

	[XmlIgnore]
	protected NetEnum<BedType> _bedType = new NetEnum<BedType>(BedType.Any);

	[XmlIgnore]
	public NetMutex mutex = new NetMutex();

	[XmlElement("bedType")]
	public BedType bedType
	{
		get
		{
			if (_bedType.Value == BedType.Any)
			{
				BedType value = BedType.Single;
				string[] data = getData();
				if (data != null && data.Length > 1)
				{
					string[] array = ArgUtility.SplitBySpace(data[1]);
					if (array.Length > 1)
					{
						string text = array[1];
						if (!(text == "double"))
						{
							if (text == "child")
							{
								value = BedType.Child;
							}
						}
						else
						{
							value = BedType.Double;
						}
					}
				}
				_bedType.Value = value;
			}
			return _bedType.Value;
		}
		set
		{
			_bedType.Value = value;
		}
	}

	public BedFurniture()
	{
	}

	public BedFurniture(string itemId, Vector2 tile, int initialRotations)
		: base(itemId, tile, initialRotations)
	{
	}

	public BedFurniture(string itemId, Vector2 tile)
		: base(itemId, tile)
	{
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(_bedType, "_bedType").AddField(mutex.NetFields, "mutex.NetFields");
	}

	public virtual bool IsBeingSleptIn()
	{
		GameLocation location = Location;
		if (location == null)
		{
			return false;
		}
		if (mutex.IsLocked())
		{
			return true;
		}
		Rectangle value = GetBoundingBox();
		foreach (Farmer farmer in location.farmers)
		{
			if (farmer.GetBoundingBox().Intersects(value))
			{
				return true;
			}
		}
		return false;
	}

	public override void DayUpdate()
	{
		base.DayUpdate();
		mutex.ReleaseLock();
	}

	public virtual void ReserveForNPC()
	{
		mutex.RequestLock();
	}

	public override void AttemptRemoval(Action<Furniture> removal_action)
	{
		if (_alreadyAttempingRemoval)
		{
			_alreadyAttempingRemoval = false;
			return;
		}
		_alreadyAttempingRemoval = true;
		mutex.RequestLock(delegate
		{
			_alreadyAttempingRemoval = false;
			if (removal_action != null)
			{
				removal_action(this);
				mutex.ReleaseLock();
			}
		}, delegate
		{
			_alreadyAttempingRemoval = false;
		});
	}

	public static BedFurniture GetBedAtTile(GameLocation location, int x, int y)
	{
		if (location == null)
		{
			return null;
		}
		foreach (Furniture item in location.furniture)
		{
			if (Utility.doesRectangleIntersectTile(item.GetBoundingBox(), x, y) && item is BedFurniture result)
			{
				return result;
			}
		}
		return null;
	}

	public static void ApplyWakeUpPosition(Farmer who)
	{
		string value = who.lastSleepLocation.Value;
		GameLocation gameLocation = ((value != null && Game1.isLocationAccessible(value)) ? Game1.getLocationFromName(value) : null);
		GameLocation locationFromName = Game1.getLocationFromName(who.disconnectLocation.Value);
		if (locationFromName != null && who.disconnectDay.Value == Game1.MasterPlayer.stats.DaysPlayed && !Game1.newDaySync.hasInstance())
		{
			who.currentLocation = locationFromName;
			who.Position = who.disconnectPosition.Value;
		}
		else if (gameLocation?.CanWakeUpHere(who) ?? false)
		{
			who.Position = Utility.PointToVector2(who.lastSleepPoint.Value) * 64f;
			who.currentLocation = gameLocation;
			ShiftPositionForBed(who);
		}
		else
		{
			if (gameLocation != null)
			{
				Game1.log.Verbose("Can't wake up in last sleep location '" + gameLocation.NameOrUniqueName + "' because it has no bed and doesn't have the 'AllowWakeUpWithoutBed: true' map property set.");
			}
			else if (value != null)
			{
				Game1.log.Verbose("Can't wake up in last sleep location '" + value + "' because no such location was found.");
			}
			FarmHouse farmHouse = (FarmHouse)(who.currentLocation = Game1.RequireLocation<FarmHouse>(who.homeLocation.Value));
			who.Position = Utility.PointToVector2(farmHouse.GetPlayerBedSpot()) * 64f;
			ShiftPositionForBed(who);
		}
		if (who == Game1.player)
		{
			Game1.currentLocation = who.currentLocation;
		}
	}

	public static void ShiftPositionForBed(Farmer who)
	{
		GameLocation currentLocation = who.currentLocation;
		BedFurniture bedAtTile = GetBedAtTile(currentLocation, (int)(who.position.X / 64f), (int)(who.position.Y / 64f));
		if (bedAtTile != null)
		{
			who.Position = Utility.PointToVector2(bedAtTile.GetBedSpot()) * 64f;
			if (bedAtTile.bedType != BedType.Double)
			{
				if (currentLocation.map == null)
				{
					currentLocation.reloadMap();
				}
				if (!currentLocation.CanItemBePlacedHere(new Vector2(bedAtTile.TileLocation.X - 1f, bedAtTile.TileLocation.Y + 1f)))
				{
					who.faceDirection(3);
				}
				else
				{
					who.position.X -= 64f;
					who.faceDirection(1);
				}
			}
			else
			{
				bool flag = false;
				if (currentLocation is FarmHouse { HasOwner: not false } farmHouse)
				{
					if (farmHouse.owner.team.GetSpouse(farmHouse.owner.UniqueMultiplayerID) == who.UniqueMultiplayerID)
					{
						flag = true;
					}
					else if (farmHouse.owner != who && !farmHouse.owner.isMarriedOrRoommates())
					{
						flag = true;
					}
				}
				if (flag)
				{
					who.position.X += 64f;
					who.faceDirection(3);
				}
				else
				{
					who.position.X -= 64f;
					who.faceDirection(1);
				}
			}
		}
		who.position.Y += 32f;
		(who.NetFields.Root as NetRoot<Farmer>)?.CancelInterpolation();
	}

	public virtual bool CanModifyBed(Farmer who)
	{
		if (who == null)
		{
			return false;
		}
		GameLocation currentLocation = who.currentLocation;
		if (currentLocation == null)
		{
			return false;
		}
		if (currentLocation is FarmHouse farmHouse && farmHouse.owner != who && farmHouse.owner.team.GetSpouse(farmHouse.owner.UniqueMultiplayerID) != who.UniqueMultiplayerID)
		{
			return false;
		}
		return true;
	}

	public override int GetAdditionalFurniturePlacementStatus(GameLocation location, int x, int y, Farmer who = null)
	{
		if (bedType == BedType.Double)
		{
			if (!IsBedsideClear(-1))
			{
				return -1;
			}
		}
		else if (!IsBedsideClear(-1) && !IsBedsideClear(getTilesWide()))
		{
			return -1;
		}
		return base.GetAdditionalFurniturePlacementStatus(location, x, y, who);
		bool IsBedsideClear(int offsetX)
		{
			Vector2 tile = new Vector2(x / 64 + offsetX, y / 64 + 1);
			return location.CanItemBePlacedHere(tile, itemIsPassable: false, CollisionMask.All, ~CollisionMask.Objects, useFarmerTile: false, ignorePassablesExactly: true);
		}
	}

	/// <inheritdoc />
	public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
	{
		_alreadyAttempingRemoval = false;
		Location = location;
		if (!CanModifyBed(who))
		{
			Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Bed_CantMoveOthersBeds"));
			return false;
		}
		if (location is FarmHouse farmHouse && ((bedType == BedType.Child && farmHouse.upgradeLevel < 2) || (bedType == BedType.Double && farmHouse.upgradeLevel < 1)))
		{
			Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Bed_NeedsUpgrade"));
			return false;
		}
		return base.placementAction(location, x, y, who);
	}

	public override void performRemoveAction()
	{
		_alreadyAttempingRemoval = false;
		base.performRemoveAction();
	}

	public override void hoverAction()
	{
		if (!Game1.player.GetBoundingBox().Intersects(GetBoundingBox()))
		{
			base.hoverAction();
		}
	}

	public override bool canBeRemoved(Farmer who)
	{
		if (Location == null)
		{
			return false;
		}
		if (!CanModifyBed(who))
		{
			if (!Game1.player.GetBoundingBox().Intersects(GetBoundingBox()))
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Bed_CantMoveOthersBeds"));
			}
			return false;
		}
		if (IsBeingSleptIn())
		{
			if (!Game1.player.GetBoundingBox().Intersects(GetBoundingBox()))
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Bed_InUse"));
			}
			return false;
		}
		return true;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new BedFurniture(base.ItemId, tileLocation.Value);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is BedFurniture bedFurniture)
		{
			bedType = bedFurniture.bedType;
		}
	}

	public virtual Point GetBedSpot()
	{
		return new Point((int)tileLocation.X + 1, (int)tileLocation.Y + 1);
	}

	/// <inheritdoc />
	public override void actionOnPlayerEntryOrPlacement(GameLocation environment, bool dropDown)
	{
		base.actionOnPlayerEntryOrPlacement(environment, dropDown);
		UpdateBedTile(check_bounds: false);
	}

	public virtual void UpdateBedTile(bool check_bounds)
	{
		Rectangle rectangle = GetBoundingBox();
		if (bedType == BedType.Double)
		{
			bedTileOffset = 1;
		}
		else if (!check_bounds || !rectangle.Intersects(Game1.player.GetBoundingBox()))
		{
			if (Game1.player.Position.X > (float)rectangle.Center.X)
			{
				bedTileOffset = 0;
			}
			else
			{
				bedTileOffset = 1;
			}
		}
	}

	public override void updateWhenCurrentLocation(GameTime time)
	{
		if (Location != null)
		{
			mutex.Update(Game1.getOnlineFarmers());
			UpdateBedTile(check_bounds: true);
		}
		base.updateWhenCurrentLocation(time);
	}

	public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		if (!isTemporarilyInvisible)
		{
			if (Furniture.isDrawingLocationFurniture)
			{
				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
				Texture2D texture = dataOrErrorItem.GetTexture();
				Rectangle value = dataOrErrorItem.GetSourceRect();
				spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, drawPosition.Value + ((shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero)), value, Color.White * alpha, 0f, Vector2.Zero, 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(boundingBox.Value.Top + 1) / 10000f);
				value.X += value.Width;
				spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, drawPosition.Value + ((shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero)), value, Color.White * alpha, 0f, Vector2.Zero, 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(boundingBox.Value.Bottom - 1) / 10000f);
			}
			else
			{
				base.draw(spriteBatch, x, y, alpha);
			}
		}
	}

	public override bool AllowPlacementOnThisTile(int x, int y)
	{
		if (bedType == BedType.Child && (float)y == TileLocation.Y + 1f)
		{
			return true;
		}
		return base.AllowPlacementOnThisTile(x, y);
	}

	public override bool IntersectsForCollision(Rectangle rect)
	{
		Rectangle rectangle = GetBoundingBox();
		Rectangle rectangle2 = rectangle;
		rectangle2.Height = 64;
		if (rectangle2.Intersects(rect))
		{
			return true;
		}
		rectangle2 = rectangle;
		rectangle2.Y += 128;
		rectangle2.Height -= 128;
		if (rectangle2.Intersects(rect))
		{
			return true;
		}
		return false;
	}

	public override int GetAdditionalTilePropertyRadius()
	{
		return 1;
	}

	/// <summary>Get whether a given tile position contains a bed that can be slept in (e.g. bed furniture or map bed).</summary>
	/// <param name="location">The location to check.</param>
	/// <param name="x">The tile X position to check.</param>
	/// <param name="y">The tile Y position to check.</param>
	public static bool IsBedHere(GameLocation location, int x, int y)
	{
		if (location == null)
		{
			return false;
		}
		ignoreContextualBedSpotOffset = true;
		if (location.doesTileHaveProperty(x, y, "Bed", "Back") != null)
		{
			ignoreContextualBedSpotOffset = false;
			return true;
		}
		ignoreContextualBedSpotOffset = false;
		return false;
	}

	public override bool DoesTileHaveProperty(int tile_x, int tile_y, string property_name, string layer_name, ref string property_value)
	{
		if (bedType == BedType.Double && (float)tile_x == tileLocation.X - 1f && (float)tile_y == tileLocation.Y + 1f && layer_name == "Back" && property_name == "NoFurniture")
		{
			property_value = "T";
			return true;
		}
		if ((float)tile_x >= tileLocation.X && (float)tile_x < tileLocation.X + (float)getTilesWide() && (float)tile_y == tileLocation.Y + 1f && layer_name == "Back")
		{
			if (property_name == "Bed")
			{
				property_value = "T";
				return true;
			}
			if (bedType != BedType.Child)
			{
				int num = (int)tileLocation.X + bedTileOffset;
				if (ignoreContextualBedSpotOffset)
				{
					num = (int)tileLocation.X + 1;
				}
				if (tile_x == num && property_name == "TouchAction")
				{
					property_value = "Sleep";
					return true;
				}
			}
		}
		return false;
	}
}
