using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley.Locations;

public class DecoratableLocation : GameLocation
{
	/// <summary>Obsolete.</summary>
	public readonly DecorationFacade wallPaper = new DecorationFacade();

	[XmlIgnore]
	public readonly NetStringList wallpaperIDs = new NetStringList();

	public readonly NetStringDictionary<string, NetString> appliedWallpaper = new NetStringDictionary<string, NetString>
	{
		InterpolationWait = false
	};

	[XmlIgnore]
	public readonly Dictionary<string, List<Vector3>> wallpaperTiles = new Dictionary<string, List<Vector3>>();

	/// <summary>Obsolete.</summary>
	public readonly DecorationFacade floor = new DecorationFacade();

	[XmlIgnore]
	public readonly NetStringList floorIDs = new NetStringList();

	public readonly NetStringDictionary<string, NetString> appliedFloor = new NetStringDictionary<string, NetString>
	{
		InterpolationWait = false
	};

	[XmlIgnore]
	public readonly Dictionary<string, List<Vector3>> floorTiles = new Dictionary<string, List<Vector3>>();

	protected Dictionary<string, TileSheet> _wallAndFloorTileSheets = new Dictionary<string, TileSheet>();

	protected Map _wallAndFloorTileSheetMap;

	/// <summary>Whether to log troubleshooting warnings for wallpaper and flooring issues.</summary>
	public static bool LogTroubleshootingInfo;

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(appliedWallpaper, "appliedWallpaper").AddField(appliedFloor, "appliedFloor").AddField(floorIDs, "floorIDs")
			.AddField(wallpaperIDs, "wallpaperIDs");
		appliedWallpaper.OnValueAdded += delegate(string key, string value)
		{
			UpdateWallpaper(key);
		};
		appliedWallpaper.OnConflictResolve += delegate(string key, NetString rejected, NetString accepted)
		{
			UpdateWallpaper(key);
		};
		appliedWallpaper.OnValueTargetUpdated += delegate(string key, string old_value, string new_value)
		{
			if (appliedWallpaper.FieldDict.TryGetValue(key, out var value))
			{
				value.CancelInterpolation();
			}
			UpdateWallpaper(key);
		};
		appliedFloor.OnValueAdded += delegate(string key, string value)
		{
			UpdateFloor(key);
		};
		appliedFloor.OnConflictResolve += delegate(string key, NetString rejected, NetString accepted)
		{
			UpdateFloor(key);
		};
		appliedFloor.OnValueTargetUpdated += delegate(string key, string old_value, string new_value)
		{
			if (appliedFloor.FieldDict.TryGetValue(key, out var value))
			{
				value.CancelInterpolation();
			}
			UpdateFloor(key);
		};
	}

	public DecoratableLocation()
	{
	}

	public DecoratableLocation(string mapPath, string name)
		: base(mapPath, name)
	{
	}

	public override void updateLayout()
	{
		base.updateLayout();
		if (Game1.IsMasterGame)
		{
			setWallpapers();
			setFloors();
		}
	}

	public virtual void ReadWallpaperAndFloorTileData()
	{
		updateMap();
		wallpaperTiles.Clear();
		floorTiles.Clear();
		wallpaperIDs.Clear();
		floorIDs.Clear();
		string value = "0";
		string value2 = "0";
		if (this is FarmHouse { upgradeLevel: <3 })
		{
			Farm farm = Game1.getLocationFromName("Farm", isStructure: false) as Farm;
			value = FarmHouse.GetStarterWallpaper(farm) ?? "0";
			value2 = FarmHouse.GetStarterFlooring(farm) ?? "0";
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (TryGetMapProperty("WallIDs", out var propertyValue))
		{
			string[] array = propertyValue.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = ArgUtility.SplitBySpace(array[i]);
				if (array2.Length >= 1)
				{
					wallpaperIDs.Add(array2[0]);
				}
				if (array2.Length >= 2)
				{
					dictionary[array2[0]] = array2[1];
				}
			}
		}
		if (wallpaperIDs.Count == 0)
		{
			List<Microsoft.Xna.Framework.Rectangle> walls = getWalls();
			for (int j = 0; j < walls.Count; j++)
			{
				string text = "Wall_" + j;
				wallpaperIDs.Add(text);
				Microsoft.Xna.Framework.Rectangle rect = walls[j];
				if (!wallpaperTiles.ContainsKey(j.ToString()))
				{
					wallpaperTiles[text] = new List<Vector3>();
				}
				foreach (Point point in rect.GetPoints())
				{
					wallpaperTiles[text].Add(new Vector3(point.X, point.Y, point.Y - rect.Top));
				}
			}
		}
		else
		{
			for (int k = 0; k < map.Layers[0].LayerWidth; k++)
			{
				for (int l = 0; l < map.Layers[0].LayerHeight; l++)
				{
					string text2 = doesTileHaveProperty(k, l, "WallID", "Back");
					if (text2 == null)
					{
						continue;
					}
					if (!wallpaperIDs.Contains(text2))
					{
						wallpaperIDs.Add(text2);
					}
					if (appliedWallpaper.TryAdd(text2, value) && dictionary.TryGetValue(text2, out var value3))
					{
						if (appliedWallpaper.TryGetValue(value3, out var value4))
						{
							appliedWallpaper[text2] = value4;
						}
						else if (GetWallpaperSource(value3).Value >= 0)
						{
							appliedWallpaper[text2] = value3;
						}
					}
					if (!wallpaperTiles.TryGetValue(text2, out var value5))
					{
						value5 = (wallpaperTiles[text2] = new List<Vector3>());
					}
					value5.Add(new Vector3(k, l, 0f));
					if (IsFloorableOrWallpaperableTile(k, l + 1, "Back"))
					{
						value5.Add(new Vector3(k, l + 1, 1f));
					}
					if (IsFloorableOrWallpaperableTile(k, l + 2, "Buildings"))
					{
						value5.Add(new Vector3(k, l + 2, 2f));
					}
					else if (IsFloorableOrWallpaperableTile(k, l + 2, "Back") && !IsFloorableTile(k, l + 2, "Back"))
					{
						value5.Add(new Vector3(k, l + 2, 2f));
					}
				}
			}
		}
		dictionary.Clear();
		if (TryGetMapProperty("FloorIDs", out var propertyValue2))
		{
			string[] array = propertyValue2.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string[] array3 = ArgUtility.SplitBySpace(array[i]);
				if (array3.Length >= 1)
				{
					floorIDs.Add(array3[0]);
				}
				if (array3.Length >= 2)
				{
					dictionary[array3[0]] = array3[1];
				}
			}
		}
		if (floorIDs.Count == 0)
		{
			List<Microsoft.Xna.Framework.Rectangle> floors = getFloors();
			for (int m = 0; m < floors.Count; m++)
			{
				string text3 = "Floor_" + m;
				floorIDs.Add(text3);
				Microsoft.Xna.Framework.Rectangle rect2 = floors[m];
				if (!floorTiles.ContainsKey(m.ToString()))
				{
					floorTiles[text3] = new List<Vector3>();
				}
				foreach (Point point2 in rect2.GetPoints())
				{
					floorTiles[text3].Add(new Vector3(point2.X, point2.Y, 0f));
				}
			}
		}
		else
		{
			for (int n = 0; n < map.Layers[0].LayerWidth; n++)
			{
				for (int num = 0; num < map.Layers[0].LayerHeight; num++)
				{
					string text4 = doesTileHaveProperty(n, num, "FloorID", "Back");
					if (text4 == null)
					{
						continue;
					}
					if (!floorIDs.Contains(text4))
					{
						floorIDs.Add(text4);
					}
					if (appliedFloor.TryAdd(text4, value2) && dictionary.TryGetValue(text4, out var value6))
					{
						if (appliedFloor.TryGetValue(value6, out var value7))
						{
							appliedFloor[text4] = value7;
						}
						else if (GetFloorSource(value6).Value >= 0)
						{
							appliedFloor[text4] = value6;
						}
					}
					if (!floorTiles.TryGetValue(text4, out var value8))
					{
						value8 = (floorTiles[text4] = new List<Vector3>());
					}
					value8.Add(new Vector3(n, num, 0f));
				}
			}
		}
		setFloors();
		setWallpapers();
	}

	public virtual TileSheet GetWallAndFloorTilesheet(string id)
	{
		if (map != _wallAndFloorTileSheetMap)
		{
			_wallAndFloorTileSheets.Clear();
			_wallAndFloorTileSheetMap = map;
		}
		if (_wallAndFloorTileSheets.TryGetValue(id, out var value))
		{
			return value;
		}
		try
		{
			foreach (ModWallpaperOrFlooring item in DataLoader.AdditionalWallpaperFlooring(Game1.content))
			{
				if (!(item.Id != id))
				{
					Texture2D texture2D = Game1.content.Load<Texture2D>(item.Texture);
					if (texture2D.Width != 256)
					{
						Game1.log.Warn($"The tilesheet for wallpaper/floor '{item.Id}' is {texture2D.Width} pixels wide, but it must be exactly {256} pixels wide.");
					}
					TileSheet tileSheet = new TileSheet("x_WallsAndFloors_" + id, map, item.Texture, new Size(texture2D.Width / 16, texture2D.Height / 16), new Size(16, 16));
					map.AddTileSheet(tileSheet);
					map.LoadTileSheets(Game1.mapDisplayDevice);
					_wallAndFloorTileSheets[id] = tileSheet;
					return tileSheet;
				}
			}
			Game1.log.Error("The tilesheet for wallpaper/floor '" + id + "' could not be loaded: no such ID found in Data/AdditionalWallpaperFlooring.");
			_wallAndFloorTileSheets[id] = null;
			return null;
		}
		catch (Exception exception)
		{
			Game1.log.Error("The tilesheet for wallpaper/floor '" + id + "' could not be loaded.", exception);
			_wallAndFloorTileSheets[id] = null;
			return null;
		}
	}

	public virtual KeyValuePair<string, int> GetFloorSource(string pattern_id)
	{
		int result;
		if (pattern_id.Contains(':'))
		{
			string[] array = pattern_id.Split(':');
			TileSheet wallAndFloorTilesheet = GetWallAndFloorTilesheet(array[0]);
			if (int.TryParse(array[1], out result) && wallAndFloorTilesheet != null)
			{
				return new KeyValuePair<string, int>(wallAndFloorTilesheet.Id, result);
			}
		}
		if (int.TryParse(pattern_id, out result))
		{
			return new KeyValuePair<string, int>("walls_and_floors", result);
		}
		return new KeyValuePair<string, int>(null, -1);
	}

	public virtual KeyValuePair<string, int> GetWallpaperSource(string pattern_id)
	{
		int result;
		if (pattern_id.Contains(':'))
		{
			string[] array = pattern_id.Split(':');
			TileSheet wallAndFloorTilesheet = GetWallAndFloorTilesheet(array[0]);
			if (int.TryParse(array[1], out result) && wallAndFloorTilesheet != null)
			{
				return new KeyValuePair<string, int>(wallAndFloorTilesheet.Id, result);
			}
		}
		if (int.TryParse(pattern_id, out result))
		{
			return new KeyValuePair<string, int>("walls_and_floors", result);
		}
		return new KeyValuePair<string, int>(null, -1);
	}

	public virtual void UpdateFloor(string floorId)
	{
		updateMap();
		if (!appliedFloor.TryGetValue(floorId, out var value) || !floorTiles.TryGetValue(floorId, out var value2))
		{
			return;
		}
		bool flag = false;
		HashSet<string> hashSet = null;
		foreach (Vector3 item in value2)
		{
			int num = (int)item.X;
			int num2 = (int)item.Y;
			KeyValuePair<string, int> floorSource = GetFloorSource(value);
			if (floorSource.Value < 0)
			{
				if (LogTroubleshootingInfo)
				{
					hashSet = hashSet ?? new HashSet<string>();
					hashSet.Add("floor pattern '" + value + "' doesn't match any known floor set");
				}
				continue;
			}
			string key = floorSource.Key;
			int value3 = floorSource.Value;
			int sheetWidth = map.RequireTileSheet(key).SheetWidth;
			value3 = value3 * 2 + value3 / (sheetWidth / 2) * sheetWidth;
			if (key == "walls_and_floors")
			{
				value3 += GetFirstFlooringTile();
			}
			if (!IsFloorableOrWallpaperableTile(num, num2, "Back", out var reasonInvalid))
			{
				if (LogTroubleshootingInfo)
				{
					hashSet = hashSet ?? new HashSet<string>();
					hashSet.Add(reasonInvalid);
				}
			}
			else
			{
				setMapTile(num, num2, GetFlooringIndex(value3, num, num2), "Back", key);
				flag = true;
			}
		}
		if (!flag && hashSet != null && hashSet.Count > 0)
		{
			Game1.log.Warn($"Couldn't apply floors for area ID '{floorId}' ({string.Join("; ", hashSet)})");
		}
	}

	public virtual void UpdateWallpaper(string wallpaperId)
	{
		updateMap();
		if (!appliedWallpaper.TryGetValue(wallpaperId, out var value) || !wallpaperTiles.TryGetValue(wallpaperId, out var value2))
		{
			return;
		}
		bool flag = false;
		HashSet<string> hashSet = null;
		foreach (Vector3 item in value2)
		{
			int num = (int)item.X;
			int num2 = (int)item.Y;
			int num3 = (int)item.Z;
			KeyValuePair<string, int> wallpaperSource = GetWallpaperSource(value);
			if (wallpaperSource.Value < 0)
			{
				if (LogTroubleshootingInfo)
				{
					hashSet = hashSet ?? new HashSet<string>();
					hashSet.Add("wallpaper pattern '" + value + "' doesn't match any known wallpaper set");
				}
				continue;
			}
			string key = wallpaperSource.Key;
			int value3 = wallpaperSource.Value;
			TileSheet tileSheet = map.RequireTileSheet(key);
			int sheetWidth = tileSheet.SheetWidth;
			string reasonInvalid;
			string text = ((num3 == 2 && IsFloorableOrWallpaperableTile(num, num2, "Buildings", out reasonInvalid)) ? "Buildings" : "Back");
			if (!IsFloorableOrWallpaperableTile(num, num2, text, out var reasonInvalid2))
			{
				if (LogTroubleshootingInfo)
				{
					hashSet = hashSet ?? new HashSet<string>();
					hashSet.Add(reasonInvalid2);
				}
			}
			else
			{
				setMapTile(num, num2, value3 / sheetWidth * sheetWidth * 3 + value3 % sheetWidth + num3 * sheetWidth, text, tileSheet.Id);
				flag = true;
			}
		}
		if (!flag && hashSet != null && hashSet.Count > 0)
		{
			Game1.log.Warn($"Couldn't apply wallpaper for area ID '{wallpaperId}' ({string.Join("; ", hashSet)})");
		}
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		if (!wasUpdated)
		{
			base.UpdateWhenCurrentLocation(time);
		}
	}

	public override void MakeMapModifications(bool force = false)
	{
		base.MakeMapModifications(force);
		if (!(this is FarmHouse))
		{
			ReadWallpaperAndFloorTileData();
			setWallpapers();
			setFloors();
		}
		if (hasTileAt(Game1.player.TilePoint, "Buildings"))
		{
			Game1.player.position.Y += 64f;
		}
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		if (Game1.player.mailReceived.Add("button_tut_1"))
		{
			Game1.onScreenMenus.Add(new ButtonTutorialMenu(0));
		}
	}

	public override bool CanFreePlaceFurniture()
	{
		return true;
	}

	public virtual bool isTileOnWall(int x, int y)
	{
		foreach (string key in wallpaperTiles.Keys)
		{
			foreach (Vector3 item in wallpaperTiles[key])
			{
				if ((int)item.X == x && (int)item.Y == y)
				{
					return true;
				}
			}
		}
		return false;
	}

	public int GetWallTopY(int x, int y)
	{
		foreach (string key in wallpaperTiles.Keys)
		{
			foreach (Vector3 item in wallpaperTiles[key])
			{
				if ((int)item.X == x && (int)item.Y == y)
				{
					return y - (int)item.Z;
				}
			}
		}
		return -1;
	}

	public virtual void setFloors()
	{
		foreach (KeyValuePair<string, string> pair in appliedFloor.Pairs)
		{
			UpdateFloor(pair.Key);
		}
	}

	public virtual void setWallpapers()
	{
		foreach (KeyValuePair<string, string> pair in appliedWallpaper.Pairs)
		{
			UpdateWallpaper(pair.Key);
		}
	}

	public void SetFloor(string which, string which_room)
	{
		if (which_room == null)
		{
			foreach (string floorID in floorIDs)
			{
				appliedFloor[floorID] = which;
			}
			return;
		}
		appliedFloor[which_room] = which;
	}

	public void SetWallpaper(string which, string which_room)
	{
		if (which_room == null)
		{
			foreach (string wallpaperID in wallpaperIDs)
			{
				appliedWallpaper[wallpaperID] = which;
			}
			return;
		}
		appliedWallpaper[which_room] = which;
	}

	public void OverrideSpecificWallpaper(string which, string which_room, string wallpaperStyleToOverride)
	{
		if (which_room == null)
		{
			foreach (string wallpaperID in wallpaperIDs)
			{
				if (appliedWallpaper.TryGetValue(wallpaperID, out var value) && value == wallpaperStyleToOverride)
				{
					appliedWallpaper[wallpaperID] = which;
				}
			}
			return;
		}
		if (appliedWallpaper[which_room] == wallpaperStyleToOverride)
		{
			appliedWallpaper[which_room] = which;
		}
	}

	public void OverrideSpecificFlooring(string which, string which_room, string flooringStyleToOverride)
	{
		if (which_room == null)
		{
			foreach (string floorID in floorIDs)
			{
				if (appliedFloor.TryGetValue(floorID, out var value) && value == flooringStyleToOverride)
				{
					appliedFloor[floorID] = which;
				}
			}
			return;
		}
		if (appliedFloor[which_room] == flooringStyleToOverride)
		{
			appliedFloor[which_room] = which;
		}
	}

	public string GetFloorID(int x, int y)
	{
		foreach (string key in floorTiles.Keys)
		{
			foreach (Vector3 item in floorTiles[key])
			{
				if ((int)item.X == x && (int)item.Y == y)
				{
					return key;
				}
			}
		}
		return null;
	}

	public string GetWallpaperID(int x, int y)
	{
		foreach (string key in wallpaperTiles.Keys)
		{
			foreach (Vector3 item in wallpaperTiles[key])
			{
				if ((int)item.X == x && (int)item.Y == y)
				{
					return key;
				}
			}
		}
		return null;
	}

	protected bool IsFloorableTile(int x, int y, string layer_name)
	{
		int tileIndexAt = getTileIndexAt(x, y, "Buildings", "untitled tile sheet");
		if (tileIndexAt >= 197 && tileIndexAt <= 199)
		{
			return false;
		}
		return IsFloorableOrWallpaperableTile(x, y, layer_name);
	}

	public bool IsWallAndFloorTilesheet(string tilesheet_id)
	{
		if (!(tilesheet_id == "walls_and_floors") && !tilesheet_id.Contains("walls_and_floors"))
		{
			return tilesheet_id.StartsWith("x_WallsAndFloors_");
		}
		return true;
	}

	protected bool IsFloorableOrWallpaperableTile(int x, int y, string layerName)
	{
		string reasonInvalid;
		return IsFloorableOrWallpaperableTile(x, y, layerName, out reasonInvalid);
	}

	protected bool IsFloorableOrWallpaperableTile(int x, int y, string layerName, out string reasonInvalid)
	{
		Layer layer = map.GetLayer(layerName);
		if (layer == null)
		{
			reasonInvalid = "layer '" + layerName + "' not found";
			return false;
		}
		if (x < 0 || x >= layer.LayerWidth || y < 0 || y >= layer.LayerHeight)
		{
			reasonInvalid = $"tile ({x}, {y}) is out of bounds for the layer";
			return false;
		}
		Tile tile = layer.Tiles[x, y];
		if (tile == null)
		{
			reasonInvalid = $"tile ({x}, {y}) not found";
			return false;
		}
		TileSheet tileSheet = tile.TileSheet;
		if (tileSheet == null)
		{
			reasonInvalid = $"tile ({x}, {y}) has unknown tilesheet";
			return false;
		}
		if (!IsWallAndFloorTilesheet(tileSheet.Id))
		{
			reasonInvalid = "tilesheet '" + tileSheet.Id + "' isn't a wall and floor tilesheet, expected tilesheet ID containing 'walls_and_floors' or starting with 'x_WallsAndFloors_'";
			return false;
		}
		reasonInvalid = null;
		return true;
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		if (l is DecoratableLocation decoratableLocation)
		{
			if (!decoratableLocation.appliedWallpaper.Keys.Any() && !decoratableLocation.appliedFloor.Keys.Any())
			{
				ReadWallpaperAndFloorTileData();
				for (int i = 0; i < decoratableLocation.wallPaper.Count; i++)
				{
					try
					{
						string key = wallpaperIDs[i];
						string value = decoratableLocation.wallPaper[i].ToString();
						appliedWallpaper[key] = value;
					}
					catch (Exception)
					{
					}
				}
				for (int j = 0; j < decoratableLocation.floor.Count; j++)
				{
					try
					{
						string key2 = floorIDs[j];
						string value2 = decoratableLocation.floor[j].ToString();
						appliedFloor[key2] = value2;
					}
					catch (Exception)
					{
					}
				}
			}
			else
			{
				foreach (string key3 in decoratableLocation.appliedWallpaper.Keys)
				{
					appliedWallpaper[key3] = decoratableLocation.appliedWallpaper[key3];
				}
				foreach (string key4 in decoratableLocation.appliedFloor.Keys)
				{
					appliedFloor[key4] = decoratableLocation.appliedFloor[key4];
				}
			}
		}
		setWallpapers();
		setFloors();
		base.TransferDataFromSavedLocation(l);
	}

	public Furniture getRandomFurniture(Random r)
	{
		return r.ChooseFrom(furniture);
	}

	public virtual string getFloorRoomIdAt(Point p)
	{
		foreach (string key in floorTiles.Keys)
		{
			foreach (Vector3 item in floorTiles[key])
			{
				if ((int)item.X == p.X && (int)item.Y == p.Y)
				{
					return key;
				}
			}
		}
		return null;
	}

	public virtual int GetFirstFlooringTile()
	{
		return 336;
	}

	public virtual int GetFlooringIndex(int base_tile_sheet, int tile_x, int tile_y)
	{
		if (!hasTileAt(tile_x, tile_y, "Back"))
		{
			return 0;
		}
		string tileSheetIDAt = getTileSheetIDAt(tile_x, tile_y, "Back");
		TileSheet tileSheet = map.GetTileSheet(tileSheetIDAt);
		int num = 16;
		if (tileSheet != null)
		{
			num = tileSheet.SheetWidth;
		}
		int num2 = tile_x % 2;
		int num3 = tile_y % 2;
		return base_tile_sheet + num2 + num * num3;
	}

	public virtual List<Microsoft.Xna.Framework.Rectangle> getFloors()
	{
		return new List<Microsoft.Xna.Framework.Rectangle>();
	}
}
