using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.GameData.WorldMaps;
using StardewValley.Internal;
using StardewValley.TokenizableStrings;

namespace StardewValley.WorldMaps;

/// <summary>A smaller section of the map which is linked to one or more in-game locations. The map area might be edited/swapped depending on the context, have its own tooltip(s), or have its own player marker positions.</summary>
public class MapArea
{
	/// <summary>The cached value for <see cref="M:StardewValley.WorldMaps.MapArea.GetTextures" />.</summary>
	protected MapAreaTexture[] CachedTextures;

	/// <summary>The cached value for <see cref="M:StardewValley.WorldMaps.MapArea.GetTooltips" />.</summary>
	protected MapAreaTooltip[] CachedTooltips;

	/// <summary>The cached value for <see cref="M:StardewValley.WorldMaps.MapArea.GetWorldPositions" />.</summary>
	protected MapAreaPosition[] CachedWorldPositions;

	/// <summary>The cached value for <see cref="M:StardewValley.WorldMaps.MapArea.GetScrollText" />.</summary>
	protected string CachedScrollText;

	/// <summary>The unique identifier for the area.</summary>
	public string Id { get; }

	/// <summary>The large-scale part of the world (like the Valley) which contains this area.</summary>
	public MapRegion Region { get; }

	/// <summary>The underlying data.</summary>
	public WorldMapAreaData Data { get; }

	/// <summary>Construct an instance.</summary>
	/// <param name="region">The large-scale part of the world (like the Valley) which contains this area.</param>
	/// <param name="data">The underlying data.</param>
	public MapArea(MapRegion region, WorldMapAreaData data)
	{
		Data = data;
		Id = data.Id;
		Region = region;
	}

	/// <summary>Get the textures to draw onto the map (adjusted for pixel zoom), if any.</summary>
	public MapAreaTexture[] GetTextures()
	{
		if (CachedTextures == null)
		{
			if (Data.Textures.Count > 0)
			{
				List<MapAreaTexture> list = new List<MapAreaTexture>();
				foreach (WorldMapTextureData texture in Data.Textures)
				{
					if (!GameStateQuery.CheckConditions(texture.Condition))
					{
						continue;
					}
					Texture2D texture2D = null;
					if (texture.Condition == "IS_CUSTOM_FARM_TYPE")
					{
						string text = Game1.whichModFarm?.WorldMapTexture;
						if (text == null)
						{
							continue;
						}
						texture2D = GetTexture(text);
						if (texture2D.Width <= 200)
						{
							texture.SourceRect = texture2D.Bounds;
						}
					}
					else
					{
						texture2D = GetTexture(texture.Texture);
					}
					Rectangle sourceRect = texture.SourceRect;
					if (sourceRect.IsEmpty)
					{
						sourceRect = new Rectangle(0, 0, texture2D.Width, texture2D.Height);
					}
					Rectangle rectangle = texture.MapPixelArea;
					if (rectangle.IsEmpty)
					{
						rectangle = Data.PixelArea;
					}
					list.Add(new MapAreaTexture(mapPixelArea: new Rectangle(rectangle.X * 4, rectangle.Y * 4, rectangle.Width * 4, rectangle.Height * 4), texture: texture2D, sourceRect: sourceRect));
				}
				CachedTextures = list.ToArray();
			}
			else
			{
				CachedTextures = LegacyShims.EmptyArray<MapAreaTexture>();
			}
		}
		return CachedTextures;
	}

	/// <summary>Get the tooltips to draw onto the map, if any.</summary>
	public MapAreaTooltip[] GetTooltips()
	{
		if (CachedTooltips == null)
		{
			List<WorldMapTooltipData> tooltips = Data.Tooltips;
			if (tooltips != null && tooltips.Count > 0)
			{
				List<MapAreaTooltip> list = new List<MapAreaTooltip>();
				foreach (WorldMapTooltipData tooltip in Data.Tooltips)
				{
					if (GameStateQuery.CheckConditions(tooltip.Condition))
					{
						string text = (GameStateQuery.CheckConditions(tooltip.KnownCondition) ? TokenParser.ParseText(Utility.TrimLines(tooltip.Text)) : "???");
						if (!string.IsNullOrWhiteSpace(text))
						{
							list.Add(new MapAreaTooltip(this, tooltip, text));
						}
					}
				}
				CachedTooltips = list.ToArray();
			}
			else
			{
				CachedTooltips = LegacyShims.EmptyArray<MapAreaTooltip>();
			}
		}
		return CachedTooltips;
	}

	/// <summary>Get all valid world positions in this area.</summary>
	public IEnumerable<MapAreaPosition> GetWorldPositions()
	{
		if (CachedWorldPositions == null)
		{
			List<MapAreaPosition> list = new List<MapAreaPosition>();
			foreach (WorldMapAreaPositionData worldPosition in Data.WorldPositions)
			{
				if (GameStateQuery.CheckConditions(worldPosition.Condition))
				{
					list.Add(new MapAreaPosition(this, worldPosition));
				}
			}
			CachedWorldPositions = list.ToArray();
		}
		return CachedWorldPositions;
	}

	/// <summary>Get a valid world position matching the given values, if any.</summary>
	/// <param name="locationName">The location name containing the tile.</param>
	/// <param name="contextName">The location's context name.</param>
	/// <param name="tile">The tile coordinate to match.</param>
	public MapAreaPosition GetWorldPosition(string locationName, string contextName, Point tile)
	{
		return GetWorldPosition(locationName, contextName, tile, null);
	}

	/// <summary>Get a valid world position matching the given values, if any.</summary>
	/// <param name="locationName">The location name containing the tile.</param>
	/// <param name="contextName">The location's context name.</param>
	/// <param name="tile">The tile coordinate to match.</param>
	/// <param name="log">The detailed log to update with the steps used to match the position, if set.</param>
	internal MapAreaPosition GetWorldPosition(string locationName, string contextName, Point tile, LogBuilder log)
	{
		LogBuilder log2 = log?.GetIndentedLog();
		foreach (MapAreaPosition worldPosition in GetWorldPositions())
		{
			log?.AppendLine("Checking position '" + worldPosition.Data.Id + "'...");
			if (worldPosition.Matches(locationName, contextName, tile, log2))
			{
				return worldPosition;
			}
		}
		return null;
	}

	/// <summary>Get the translated tooltip text to display when hovering the cursor over the map area.</summary>
	public virtual string GetScrollText()
	{
		if (CachedScrollText == null)
		{
			CachedScrollText = TokenParser.ParseText(Utility.TrimLines(Data.ScrollText));
		}
		return CachedScrollText;
	}

	/// <summary>Get the texture to load for an asset name.</summary>
	/// <param name="assetName">The asset name to load.</param>
	private Texture2D GetTexture(string assetName)
	{
		if (Game1.season != Season.Spring)
		{
			string assetName2 = assetName + "_" + Game1.currentSeason.ToLower();
			if (Game1.content.DoesAssetExist<Texture2D>(assetName2))
			{
				return Game1.content.Load<Texture2D>(assetName2);
			}
		}
		return Game1.content.Load<Texture2D>(assetName);
	}
}
