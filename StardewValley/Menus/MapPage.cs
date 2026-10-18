using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.WorldMaps;

namespace StardewValley.Menus;

/// <summary>The in-game world map view.</summary>
public class MapPage : IClickableMenu
{
	/// <summary>The world map debug lines to draw.</summary>
	[Flags]
	public enum WorldMapDebugLineType
	{
		/// <summary>Don't show debug lines on the map.</summary>
		None = 0,
		/// <summary>Highlight map areas.</summary>
		Areas = 1,
		/// <summary>Highlight map position rectangles.</summary>
		Positions = 2,
		/// <summary>Highlight tooltip rectangles.</summary>
		Tooltips = 4,
		/// <summary>Highlight all types.</summary>
		All = -1
	}

	/// <summary>The world map debug lines to draw, if any.</summary>
	public static WorldMapDebugLineType EnableDebugLines;

	/// <summary>The map position containing the current player.</summary>
	public readonly MapAreaPositionWithContext? mapPosition;

	/// <summary>The map region containing the <see cref="F:StardewValley.Menus.MapPage.mapPosition" />.</summary>
	public readonly MapRegion mapRegion;

	/// <summary>The smaller sections of the map linked to one or more in-game locations. Each map area might be edited/swapped depending on the context, have its own tooltip(s), or have its own player marker positions.</summary>
	public readonly MapArea[] mapAreas;

	/// <summary>The translated scroll text to show at the bottom of the map, if any.</summary>
	public readonly string scrollText;

	/// <summary>The default component ID in <see cref="F:StardewValley.Menus.MapPage.points" /> to which to snap the controller cursor by default.</summary>
	public readonly int defaultComponentID;

	/// <summary>The pixel area on screen containing all the map areas being drawn.</summary>
	public Rectangle mapBounds;

	/// <summary>The tooltips to render, indexed by <see cref="P:StardewValley.WorldMaps.MapAreaTooltip.NamespacedId" />.</summary>
	public readonly Dictionary<string, ClickableComponent> points = new Dictionary<string, ClickableComponent>(StringComparer.OrdinalIgnoreCase);

	/// <summary>The tooltip text being drawn.</summary>
	public string hoverText = "";

	public MapPage(int x, int y, int width, int height)
		: base(x, y, width, height)
	{
		WorldMapManager.ReloadData();
		Point normalizedPlayerTile = GetNormalizedPlayerTile(Game1.player);
		mapPosition = WorldMapManager.GetPositionData(Game1.player.currentLocation, normalizedPlayerTile) ?? WorldMapManager.GetPositionData(Game1.getFarm(), Point.Zero);
		mapRegion = mapPosition?.Data.Region ?? WorldMapManager.GetMapRegions().First();
		mapAreas = mapRegion.GetAreas();
		scrollText = mapPosition?.Data.GetScrollText(normalizedPlayerTile);
		mapBounds = mapRegion.GetMapPixelBounds();
		int num = (defaultComponentID = 1000);
		MapArea[] array = mapAreas;
		for (int i = 0; i < array.Length; i++)
		{
			MapAreaTooltip[] tooltips = array[i].GetTooltips();
			foreach (MapAreaTooltip mapAreaTooltip in tooltips)
			{
				Rectangle pixelArea = mapAreaTooltip.GetPixelArea();
				pixelArea = new Rectangle(mapBounds.X + pixelArea.X, mapBounds.Y + pixelArea.Y, pixelArea.Width, pixelArea.Height);
				num++;
				ClickableComponent value = new ClickableComponent(pixelArea, mapAreaTooltip.NamespacedId)
				{
					myID = num,
					label = mapAreaTooltip.Text
				};
				points[mapAreaTooltip.NamespacedId] = value;
				if (mapAreaTooltip.NamespacedId == "Farm/Default")
				{
					defaultComponentID = num;
				}
			}
		}
		array = mapAreas;
		for (int i = 0; i < array.Length; i++)
		{
			MapAreaTooltip[] tooltips = array[i].GetTooltips();
			foreach (MapAreaTooltip mapAreaTooltip2 in tooltips)
			{
				if (points.TryGetValue(mapAreaTooltip2.NamespacedId, out var value2))
				{
					SetNeighborId(value2, "left", mapAreaTooltip2.Data.LeftNeighbor);
					SetNeighborId(value2, "right", mapAreaTooltip2.Data.RightNeighbor);
					SetNeighborId(value2, "up", mapAreaTooltip2.Data.UpNeighbor);
					SetNeighborId(value2, "down", mapAreaTooltip2.Data.DownNeighbor);
				}
			}
		}
	}

	public override void populateClickableComponentList()
	{
		base.populateClickableComponentList();
		allClickableComponents.AddRange(points.Values);
	}

	/// <summary>Set a controller navigation ID for a tooltip component.</summary>
	/// <param name="component">The tooltip component whose neighbor ID to set.</param>
	/// <param name="direction">The direction to set.</param>
	/// <param name="neighborKeys">The tooltip neighbor keys to match. See remarks on <see cref="F:StardewValley.GameData.WorldMaps.WorldMapTooltipData.LeftNeighbor" /> for details on the format.</param>
	/// <returns>Returns whether the <paramref name="neighborKeys" /> matched an existing tooltip neighbor ID.</returns>
	public void SetNeighborId(ClickableComponent component, string direction, string neighborKeys)
	{
		if (string.IsNullOrWhiteSpace(neighborKeys))
		{
			return;
		}
		if (!TryGetNeighborId(neighborKeys, out var id, out var foundIgnore))
		{
			if (!foundIgnore)
			{
				Game1.log.Warn($"World map tooltip '{component.name}' has {direction} neighbor keys '{neighborKeys}' which don't match a tooltip namespaced ID or alias.");
			}
			return;
		}
		switch (direction)
		{
		case "left":
			component.leftNeighborID = id;
			break;
		case "right":
			component.rightNeighborID = id;
			break;
		case "up":
			component.upNeighborID = id;
			break;
		case "down":
			component.downNeighborID = id;
			break;
		default:
			Game1.log.Warn("Can't set neighbor ID for unknown direction '" + direction + "'.");
			break;
		}
	}

	/// <summary>Get the controller navigation ID for a tooltip neighbor field value.</summary>
	/// <param name="keys">The tooltip neighbor keys to match. See remarks on <see cref="F:StardewValley.GameData.WorldMaps.WorldMapTooltipData.LeftNeighbor" /> for details on the format.</param>
	/// <param name="id">The matching controller navigation ID, if found.</param>
	/// <param name="foundIgnore">Whether the neighbor IDs contains <c>ignore</c>, which indicates it should be skipped silently if none match.</param>
	/// <param name="isAlias">Whether the <paramref name="keys" /> are from an alias in <see cref="F:StardewValley.GameData.WorldMaps.WorldMapRegionData.MapNeighborIdAliases" />.</param>
	/// <returns>Returns <c>true</c> if the neighbor ID was found, else <c>false</c>.</returns>
	public bool TryGetNeighborId(string keys, out int id, out bool foundIgnore, bool isAlias = false)
	{
		foundIgnore = false;
		if (!string.IsNullOrWhiteSpace(keys))
		{
			string[] array = keys.Split(',', StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.EqualsIgnoreCase("ignore"))
				{
					foundIgnore = true;
					continue;
				}
				if (points.TryGetValue(text, out var value))
				{
					id = value.myID;
					return true;
				}
				if (!isAlias && mapRegion.Data.MapNeighborIdAliases.TryGetValue(text, out var value2))
				{
					if (TryGetNeighborId(value2, out id, out var foundIgnore2, isAlias: true))
					{
						foundIgnore |= foundIgnore2;
						return true;
					}
					foundIgnore |= foundIgnore2;
				}
			}
		}
		id = -1;
		return false;
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(defaultComponentID);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		foreach (ClickableComponent value in points.Values)
		{
			if (!value.containsPoint(x, y))
			{
				continue;
			}
			string name = value.name;
			if (!(name == "Beach/LonelyStone"))
			{
				if (name == "Forest/SewerPipe")
				{
					Game1.playSound("shadowpeep");
				}
			}
			else
			{
				Game1.playSound("stoneCrack");
			}
			return;
		}
		if (Game1.activeClickableMenu is GameMenu gameMenu)
		{
			gameMenu.changeTab(gameMenu.lastOpenedNonMapTab);
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoverText = "";
		foreach (ClickableComponent value in points.Values)
		{
			if (value.containsPoint(x, y))
			{
				hoverText = value.label;
				break;
			}
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		drawMap(b);
		drawMiniPortraits(b);
		drawScroll(b);
		drawTooltip(b);
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (Game1.options.doesInputListContain(Game1.options.mapButton, key) && readyToClose())
		{
			exitThisMenu();
		}
		base.receiveKeyPress(key);
	}

	public virtual void drawMiniPortraits(SpriteBatch b, float alpha = 1f)
	{
		Dictionary<Vector2, int> dictionary = new Dictionary<Vector2, int>();
		foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
		{
			Point normalizedPlayerTile = GetNormalizedPlayerTile(onlineFarmer);
			MapAreaPositionWithContext? mapAreaPositionWithContext = (onlineFarmer.IsLocalPlayer ? mapPosition : WorldMapManager.GetPositionData(onlineFarmer.currentLocation, normalizedPlayerTile));
			if (mapAreaPositionWithContext.HasValue && !(mapAreaPositionWithContext.Value.Data.Region.Id != mapRegion.Id))
			{
				Vector2 mapPixelPosition = mapAreaPositionWithContext.Value.GetMapPixelPosition();
				mapPixelPosition = new Vector2(mapPixelPosition.X + (float)mapBounds.X - 32f, mapPixelPosition.Y + (float)mapBounds.Y - 32f);
				dictionary.TryGetValue(mapPixelPosition, out var value);
				dictionary[mapPixelPosition] = value + 1;
				if (value > 0)
				{
					mapPixelPosition += new Vector2(48 * (value % 2), 48 * (value / 2));
				}
				onlineFarmer.FarmerRenderer.drawMiniPortrat(b, mapPixelPosition, 0.00011f, 4f, 2, onlineFarmer, alpha);
			}
		}
	}

	public virtual void drawScroll(SpriteBatch b)
	{
		if (scrollText != null)
		{
			float num = yPositionOnScreen + height + 32 + 4;
			float num2 = num + 80f;
			if (num2 > (float)Game1.uiViewport.Height)
			{
				num -= num2 - (float)Game1.uiViewport.Height;
			}
			SpriteText.drawStringWithScrollCenteredAt(b, scrollText, xPositionOnScreen + width / 2, (int)num);
		}
	}

	public virtual void drawMap(SpriteBatch b, bool drawBorders = true, float alpha = 1f)
	{
		if (drawBorders)
		{
			int y = mapBounds.Y - 96;
			Game1.drawDialogueBox(mapBounds.X - 32, y, (mapBounds.Width + 16) * 4, (mapBounds.Height + 32) * 4, speaker: false, drawOnlyBox: true);
		}
		float num = 0.86f;
		MapAreaTexture baseTexture = mapRegion.GetBaseTexture();
		if (baseTexture != null)
		{
			Rectangle offsetMapPixelArea = baseTexture.GetOffsetMapPixelArea(mapBounds.X, mapBounds.Y);
			b.Draw(baseTexture.Texture, offsetMapPixelArea, baseTexture.SourceRect, Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, num);
			num += 0.001f;
		}
		MapArea[] array = mapAreas;
		for (int i = 0; i < array.Length; i++)
		{
			MapAreaTexture[] textures = array[i].GetTextures();
			foreach (MapAreaTexture mapAreaTexture in textures)
			{
				Rectangle offsetMapPixelArea2 = mapAreaTexture.GetOffsetMapPixelArea(mapBounds.X, mapBounds.Y);
				b.Draw(mapAreaTexture.Texture, offsetMapPixelArea2, mapAreaTexture.SourceRect, Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, num);
				num += 0.001f;
			}
		}
		if (EnableDebugLines == WorldMapDebugLineType.None)
		{
			return;
		}
		array = mapAreas;
		foreach (MapArea mapArea in array)
		{
			if (EnableDebugLines.HasFlag(WorldMapDebugLineType.Tooltips))
			{
				MapAreaTooltip[] tooltips = mapArea.GetTooltips();
				for (int j = 0; j < tooltips.Length; j++)
				{
					Rectangle pixelArea = tooltips[j].GetPixelArea();
					pixelArea = new Rectangle(mapBounds.X + pixelArea.X, mapBounds.Y + pixelArea.Y, pixelArea.Width, pixelArea.Height);
					Utility.DrawSquare(b, pixelArea, 2, Color.Blue * alpha);
				}
			}
			if (EnableDebugLines.HasFlag(WorldMapDebugLineType.Areas))
			{
				Rectangle pixelArea2 = mapArea.Data.PixelArea;
				if (pixelArea2.Width > 0 || pixelArea2.Height > 0)
				{
					pixelArea2 = new Rectangle(mapBounds.X + pixelArea2.X * 4, mapBounds.Y + pixelArea2.Y * 4, pixelArea2.Width * 4, pixelArea2.Height * 4);
					Utility.DrawSquare(b, pixelArea2, 4, Color.Black * alpha);
				}
			}
			if (!EnableDebugLines.HasFlag(WorldMapDebugLineType.Positions))
			{
				continue;
			}
			foreach (MapAreaPosition worldPosition in mapArea.GetWorldPositions())
			{
				Rectangle pixelArea3 = worldPosition.GetPixelArea();
				pixelArea3 = new Rectangle(mapBounds.X + pixelArea3.X, mapBounds.Y + pixelArea3.Y, pixelArea3.Width, pixelArea3.Height);
				Utility.DrawSquare(b, pixelArea3, 2, Color.Red * alpha);
			}
		}
	}

	public virtual void drawTooltip(SpriteBatch b)
	{
		if (!string.IsNullOrEmpty(hoverText))
		{
			IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);
		}
	}

	/// <summary>Get the tile coordinate for a player, with negative values snapped to zero.</summary>
	/// <param name="player">The player instance.</param>
	public Point GetNormalizedPlayerTile(Farmer player)
	{
		Point result = player.TilePoint;
		if (result.X < 0 || result.Y < 0)
		{
			result = new Point(Math.Max(0, result.X), Math.Max(0, result.Y));
		}
		return result;
	}
}
