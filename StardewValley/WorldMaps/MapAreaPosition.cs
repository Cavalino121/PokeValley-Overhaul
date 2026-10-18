using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.GameData.WorldMaps;
using StardewValley.Internal;
using StardewValley.TokenizableStrings;
using xTile.Dimensions;

namespace StardewValley.WorldMaps;

/// <summary>Maps in-game locations and tile positions to the parent <see cref="T:StardewValley.WorldMaps.MapArea" />.</summary>
public class MapAreaPosition
{
	/// <summary>The cached map pixel area for <see cref="M:StardewValley.WorldMaps.MapAreaPosition.GetMapPixelPosition(StardewValley.GameLocation,Microsoft.Xna.Framework.Point)" />, adjusted for zoom.</summary>
	protected Microsoft.Xna.Framework.Rectangle? CachedMapPixelArea;

	/// <summary>The cached value for <see cref="M:StardewValley.WorldMaps.MapAreaPosition.GetScrollText(Microsoft.Xna.Framework.Point)" />.</summary>
	protected string CachedScrollText;

	/// <summary>Whether this is mapped to a fixed pixel coordinate on the map.</summary>
	protected bool IsFixedMapPosition;

	/// <summary>The map region which contains this position.</summary>
	public MapRegion Region { get; }

	/// <summary>The map area which contains this position.</summary>
	public MapArea Area { get; }

	/// <summary>The underlying map position data.</summary>
	public WorldMapAreaPositionData Data { get; }

	/// <summary>Construct an instance.</summary>
	/// <param name="mapArea">The map area which contains this position.</param>
	/// <param name="data">The underlying map position data.</param>
	public MapAreaPosition(MapArea mapArea, WorldMapAreaPositionData data)
	{
		Region = mapArea.Region;
		Area = mapArea;
		Data = data;
	}

	/// <summary>Get whether this position matches the given values.</summary>
	/// <param name="locationName">The location name containing the tile.</param>
	/// <param name="contextName">The location's context name.</param>
	/// <param name="tile">The tile coordinate to match.</param>
	public bool Matches(string locationName, string contextName, Point tile)
	{
		return Matches(locationName, contextName, tile, null);
	}

	/// <summary>Get whether this position matches the given values.</summary>
	/// <param name="locationName">The location name containing the tile.</param>
	/// <param name="contextName">The location's context name.</param>
	/// <param name="tile">The tile coordinate to match.</param>
	/// <param name="log">The detailed log to update with the steps used to match the position, if set.</param>
	internal bool Matches(string locationName, string contextName, Point tile, LogBuilder log)
	{
		WorldMapAreaPositionData data = Data;
		if (data.LocationContext != null && data.LocationContext != contextName)
		{
			log?.AppendLine($"Skipped: location context '{contextName}' doesn't match required context '{data.LocationContext}'.");
			return false;
		}
		if (data.LocationName != null && data.LocationName != locationName)
		{
			log?.AppendLine($"Skipped: location '{locationName}' doesn't match required location '{data.LocationName}'.");
			return false;
		}
		List<string> locationNames = data.LocationNames;
		if (locationNames != null && locationNames.Count > 0 && !data.LocationNames.Contains(locationName))
		{
			log?.AppendLine($"Skipped: location '{locationName}' doesn't match one of the required locations '{string.Join("', '", data.LocationNames)}'.");
			return false;
		}
		if (!IsTileWithinZone(tile))
		{
			log?.AppendLine($"Skipped: tile position {tile} doesn't match required tile zone {Data.ExtendedTileArea ?? Data.TileArea}.");
			return false;
		}
		log?.AppendLine("Matched successfully.");
		return true;
	}

	/// <summary>Get the pixel area covered by this position, adjusted for pixel zoom.</summary>
	public Microsoft.Xna.Framework.Rectangle GetPixelArea()
	{
		Microsoft.Xna.Framework.Rectangle? cachedMapPixelArea = CachedMapPixelArea;
		if (!cachedMapPixelArea.HasValue)
		{
			Microsoft.Xna.Framework.Rectangle rectangle = Data.MapPixelArea;
			if (rectangle.IsEmpty)
			{
				rectangle = Area.Data.PixelArea;
			}
			Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(rectangle.X * 4, rectangle.Y * 4, rectangle.Width * 4, rectangle.Height * 4);
			CachedMapPixelArea = value;
			IsFixedMapPosition = rectangle.Width <= 1 && rectangle.Height <= 1;
		}
		return CachedMapPixelArea.Value;
	}

	/// <summary>Get the pixel position within the world map which corresponds to an in-game location's tile within the map area, adjusted for pixel zoom.</summary>
	/// <param name="location">The in-game location containing the tile.</param>
	/// <param name="tileLocation">The tile position within the location.</param>
	public Vector2 GetMapPixelPosition(GameLocation location, Point tileLocation)
	{
		Microsoft.Xna.Framework.Rectangle pixelArea = GetPixelArea();
		if (IsFixedMapPosition)
		{
			return new Vector2(pixelArea.X, pixelArea.Y);
		}
		Vector2? positionRatioIfValid = GetPositionRatioIfValid(location, tileLocation);
		if (positionRatioIfValid.HasValue)
		{
			return new Vector2(Utility.Lerp(pixelArea.Left, pixelArea.Right, positionRatioIfValid.Value.X), Utility.Lerp(pixelArea.Top, pixelArea.Bottom, positionRatioIfValid.Value.Y));
		}
		Point center = pixelArea.Center;
		return new Vector2(center.X, center.Y);
	}

	/// <summary>Get the translated display name to show when the player is in this position.</summary>
	/// <param name="playerTile">The player's tile position within the position.</param>
	public string GetScrollText(Point playerTile)
	{
		if (CachedScrollText == null)
		{
			string scrollText = Data.ScrollText;
			List<WorldMapAreaPositionScrollTextZoneData> scrollTextZones = Data.ScrollTextZones;
			if (scrollTextZones != null && scrollTextZones.Count > 0)
			{
				foreach (WorldMapAreaPositionScrollTextZoneData scrollTextZone in Data.ScrollTextZones)
				{
					if (scrollTextZone.TileArea.Contains(playerTile))
					{
						scrollText = scrollTextZone.ScrollText;
						break;
					}
				}
			}
			CachedScrollText = ((scrollText != null) ? TokenParser.ParseText(Utility.TrimLines(scrollText)) : Area.GetScrollText());
		}
		return CachedScrollText;
	}

	/// <summary>Get the player's position as a percentage along the X and Y axes.</summary>
	/// <param name="location">The in-game location containing the tile.</param>
	/// <param name="tile">The tile position within the location.</param>
	public virtual Vector2? GetPositionRatioIfValid(GameLocation location, Point tile)
	{
		if (location?.map == null || !IsTileWithinZone(tile))
		{
			return null;
		}
		Size layerSize = location.map.Layers[0].LayerSize;
		Microsoft.Xna.Framework.Rectangle rectangle = Data.TileArea;
		if (rectangle.IsEmpty || rectangle.Right > layerSize.Width || rectangle.Bottom > layerSize.Height)
		{
			rectangle = (rectangle.IsEmpty ? new Microsoft.Xna.Framework.Rectangle(0, 0, layerSize.Width, layerSize.Height) : new Microsoft.Xna.Framework.Rectangle(rectangle.X, rectangle.Y, Math.Min(rectangle.Width, layerSize.Width - rectangle.X), Math.Min(rectangle.Height, layerSize.Height - rectangle.Y)));
		}
		float num = MathHelper.Clamp(tile.X, rectangle.X, rectangle.Right - 1);
		return new Vector2(y: ((float)MathHelper.Clamp(tile.Y, rectangle.Y, rectangle.Bottom - 1) - (float)rectangle.Y) / (float)rectangle.Height, x: (num - (float)rectangle.X) / (float)rectangle.Width);
	}

	/// <summary>Get whether a tile position is within the bounds of this position data.</summary>
	/// <param name="tile">The tile position within the location.</param>
	public virtual bool IsTileWithinZone(Point tile)
	{
		Microsoft.Xna.Framework.Rectangle rectangle = Data.ExtendedTileArea ?? Data.TileArea;
		if (!rectangle.IsEmpty)
		{
			return rectangle.Contains(tile);
		}
		return true;
	}
}
