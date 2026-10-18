using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.Objects;
using xTile.Dimensions;
using xTile.Layers;

namespace StardewValley.Locations;

public class IslandFarmHouse : DecoratableLocation
{
	[XmlElement("fridge")]
	public readonly NetRef<Chest> fridge = new NetRef<Chest>(new Chest(playerChest: true));

	public Point fridgePosition;

	public NetBool visited = new NetBool(value: false)
	{
		InterpolationEnabled = false
	};

	private Color nightLightingColor = new Color(180, 180, 0);

	private Color rainLightingColor = new Color(90, 90, 0);

	public IslandFarmHouse()
	{
		fridge.Value.Location = this;
	}

	public IslandFarmHouse(string map, string name)
		: base(map, name)
	{
		fridge.Value.Location = this;
		furniture.Add(ItemRegistry.Create<Furniture>("(F)1798").SetPlacement(12, 8));
		furniture.Add(ItemRegistry.Create<Furniture>("(F)1614").SetPlacement(3, 1));
		furniture.Add(ItemRegistry.Create<Furniture>("(F)1614").SetPlacement(8, 1));
		furniture.Add(ItemRegistry.Create<Furniture>("(F)1614").SetPlacement(20, 1));
		furniture.Add(ItemRegistry.Create<Furniture>("(F)1614").SetPlacement(25, 1));
		furniture.Add(ItemRegistry.Create<Furniture>("(F)1294").SetPlacement(1, 4));
		furniture.Add(ItemRegistry.Create<Furniture>("(F)1294").SetPlacement(10, 4));
		furniture.Add(ItemRegistry.Create<Furniture>("(F)1294").SetPlacement(18, 4));
		furniture.Add(ItemRegistry.Create<Furniture>("(F)1294").SetPlacement(28, 4));
		furniture.Add(ItemRegistry.Create<Furniture>("(F)1742").SetPlacement(20, 4));
		Furniture item = ItemRegistry.Create<Furniture>("(F)1755").SetPlacement(14, 9);
		furniture.Add(item);
		ReadWallpaperAndFloorTileData();
		SetWallpaper("88", "UpperLeft");
		SetFloor("23", "UpperLeft");
		SetWallpaper("88", "UpperRight");
		SetFloor("48", "Kitchen");
		SetWallpaper("87", "Kitchen");
		SetFloor("52", "UpperRight");
		SetWallpaper("87", "BottomRight_Left");
		SetFloor("23", "BottomRight");
		SetWallpaper("87", "BottomRight_Right");
		fridgePosition = default(Point);
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		IslandFarmHouse islandFarmHouse = (IslandFarmHouse)l;
		fridge.Value = islandFarmHouse.fridge.Value;
		visited.Value = islandFarmHouse.visited.Value;
		base.TransferDataFromSavedLocation(l);
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		fridge.Value.updateWhenCurrentLocation(time);
	}

	public override List<Microsoft.Xna.Framework.Rectangle> getWalls()
	{
		return new List<Microsoft.Xna.Framework.Rectangle>
		{
			new Microsoft.Xna.Framework.Rectangle(1, 1, 10, 3),
			new Microsoft.Xna.Framework.Rectangle(18, 1, 11, 3),
			new Microsoft.Xna.Framework.Rectangle(12, 5, 5, 2),
			new Microsoft.Xna.Framework.Rectangle(17, 9, 2, 2),
			new Microsoft.Xna.Framework.Rectangle(21, 9, 8, 2)
		};
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		if (!visited.Value)
		{
			visited.Value = true;
		}
		fridgePosition = GetFridgePositionFromMap() ?? Point.Zero;
	}

	/// <summary>Get the fridge position by scanning the map tiles for the sprite index.</summary>
	/// <remarks>This is relatively expensive. Most code should use the cached <see cref="F:StardewValley.Locations.IslandFarmHouse.fridgePosition" /> instead.</remarks>
	public Point? GetFridgePositionFromMap()
	{
		Layer layer = map.RequireLayer("Buildings");
		for (int i = 0; i < layer.LayerHeight; i++)
		{
			for (int j = 0; j < layer.LayerWidth; j++)
			{
				if (layer.GetTileIndexAt(j, i, "untitled tile sheet") == 258)
				{
					return new Point(j, i);
				}
			}
		}
		return null;
	}

	public override List<Microsoft.Xna.Framework.Rectangle> getFloors()
	{
		return new List<Microsoft.Xna.Framework.Rectangle>
		{
			new Microsoft.Xna.Framework.Rectangle(1, 3, 11, 12),
			new Microsoft.Xna.Framework.Rectangle(11, 7, 6, 9),
			new Microsoft.Xna.Framework.Rectangle(18, 3, 11, 6),
			new Microsoft.Xna.Framework.Rectangle(17, 11, 12, 6)
		};
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(fridge, "fridge").AddField(visited, "visited");
		visited.fieldChangeVisibleEvent += delegate
		{
			InitializeBeds();
		};
		fridge.fieldChangeEvent += delegate(NetRef<Chest> field, Chest oldValue, Chest newValue)
		{
			newValue.Location = this;
		};
	}

	public virtual void InitializeBeds()
	{
		if (!Game1.IsMasterGame || Game1.gameMode == 6 || !visited.Value)
		{
			return;
		}
		int num = 0;
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			_ = allFarmer;
			num++;
		}
		string itemId = "2176";
		furniture.Add(new BedFurniture(itemId, new Vector2(22f, 3f)));
		num--;
		if (num > 0)
		{
			furniture.Add(new BedFurniture(itemId, new Vector2(26f, 3f)));
			num--;
		}
		for (int i = 0; i < Math.Min(6, num); i++)
		{
			int num2 = 3;
			int num3 = 3;
			if (i % 2 == 0)
			{
				num2 += 4;
			}
			num3 += i / 2 * 4;
			furniture.Add(new BedFurniture(itemId, new Vector2(num2, num3)));
		}
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

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		if (getTileIndexAt(tileLocation, "Buildings", "untitled tile sheet") == 258)
		{
			fridge.Value.fridge.Value = true;
			fridge.Value.checkForAction(who);
			return true;
		}
		return base.checkAction(tileLocation, viewport, who);
	}
}
