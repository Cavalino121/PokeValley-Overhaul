using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.GameData.WorldMaps;
using StardewValley.Internal;

namespace StardewValley.WorldMaps;

/// <summary>Manages data related to the world map shown in the game menu.</summary>
public static class WorldMapManager
{
	/// <summary>The <see cref="F:StardewValley.Game1.ticks" /> value when cached data should be reset.</summary>
	private static int NextClearCacheTick;

	/// <summary>The maximum update ticks before any cached data should be refreshed.</summary>
	private static int MaxCacheTicks;

	/// <summary>The cached map regions.</summary>
	private static readonly List<MapRegion> Regions;

	/// <summary>Initialize before the class is first accessed.</summary>
	static WorldMapManager()
	{
		MaxCacheTicks = 3600;
		Regions = new List<MapRegion>();
		ReloadData();
	}

	/// <summary>Load the raw world map data.</summary>
	public static void ReloadData()
	{
		Regions.Clear();
		foreach (KeyValuePair<string, WorldMapRegionData> item in DataLoader.WorldMap(Game1.content))
		{
			Regions.Add(new MapRegion(item.Key, item.Value));
		}
		NextClearCacheTick = Game1.ticks + MaxCacheTicks;
	}

	/// <summary>Get all map regions in the underlying data which are currently valid.</summary>
	public static IEnumerable<MapRegion> GetMapRegions()
	{
		ReloadDataIfStale();
		return Regions;
	}

	/// <summary>Get the map position which contains a given location and tile coordinate, if any.</summary>
	/// <param name="location">The in-game location.</param>
	/// <param name="tile">The tile coordinate within the location.</param>
	public static MapAreaPositionWithContext? GetPositionData(GameLocation location, Point tile)
	{
		return GetPositionData(location, tile, null);
	}

	/// <summary>Get the map position which contains a given location and tile coordinate, if any.</summary>
	/// <param name="location">The in-game location.</param>
	/// <param name="tile">The tile coordinate within the location.</param>
	/// <param name="log">The detailed log to update with the steps used to match the position, if set.</param>
	internal static MapAreaPositionWithContext? GetPositionData(GameLocation location, Point tile, LogBuilder log)
	{
		if (location == null)
		{
			log?.AppendLine("Skipped: location is null.");
			return null;
		}
		LogBuilder log2 = log?.GetIndentedLog();
		log?.AppendLine("Searching for the player position...");
		MapAreaPosition positionDataWithoutFallback = GetPositionDataWithoutFallback(location, tile, log2);
		if (positionDataWithoutFallback != null)
		{
			log?.AppendLine("Found match: position '" + positionDataWithoutFallback.Data.Id + "'.");
			return new MapAreaPositionWithContext(positionDataWithoutFallback, location, tile);
		}
		Building parentBuilding = location.ParentBuilding;
		GameLocation gameLocation = parentBuilding?.GetParentLocation();
		if (gameLocation != null)
		{
			log?.AppendLine("");
			log?.AppendLine($"Searching for the exterior position of the '{parentBuilding.buildingType.Value}' building in {gameLocation.NameOrUniqueName}...");
			Point tile2 = new Point(parentBuilding.tileX.Value + parentBuilding.tilesWide.Value / 2, parentBuilding.tileY.Value + parentBuilding.tilesHigh.Value / 2);
			positionDataWithoutFallback = GetPositionDataWithoutFallback(gameLocation, tile2, log2);
			if (positionDataWithoutFallback != null)
			{
				log?.AppendLine("Found match: position '" + positionDataWithoutFallback.Data.Id + "'.");
				return new MapAreaPositionWithContext(positionDataWithoutFallback, gameLocation, tile2);
			}
		}
		log?.AppendLine("");
		log?.AppendLine("No match found.");
		return null;
	}

	/// <summary>Get the map position which contains a given location and tile coordinate, if any, without checking for parent buildings or locations.</summary>
	/// <param name="location">The in-game location.</param>
	/// <param name="tile">The tile coordinate within the location.</param>
	public static MapAreaPosition GetPositionDataWithoutFallback(GameLocation location, Point tile)
	{
		return GetPositionDataWithoutFallback(location, tile, null);
	}

	/// <summary>Get the map position which contains a given location and tile coordinate, if any, without checking for parent buildings or locations.</summary>
	/// <param name="location">The in-game location.</param>
	/// <param name="tile">The tile coordinate within the location.</param>
	/// <param name="log">The detailed log to update with the steps used to match the position, if set.</param>
	internal static MapAreaPosition GetPositionDataWithoutFallback(GameLocation location, Point tile, LogBuilder log)
	{
		if (location == null)
		{
			log?.AppendLine("Skipped: location is null.");
			return null;
		}
		LogBuilder log2 = log?.GetIndentedLog();
		foreach (MapRegion mapRegion in GetMapRegions())
		{
			log?.AppendLine("Checking region '" + mapRegion.Id + "'...");
			MapAreaPosition positionData = mapRegion.GetPositionData(location, tile, log2);
			if (positionData != null)
			{
				return positionData;
			}
		}
		return null;
	}

	/// <summary>Update the world map data if needed.</summary>
	private static void ReloadDataIfStale()
	{
		if (Game1.ticks >= NextClearCacheTick)
		{
			ReloadData();
		}
	}
}
