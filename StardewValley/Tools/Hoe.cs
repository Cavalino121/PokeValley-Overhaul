using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using StardewValley.Locations;
using xTile.Dimensions;

namespace StardewValley.Tools;

public class Hoe : Tool
{
	public Hoe()
		: base("Hoe", 0, 21, 47, stackable: false)
	{
	}

	/// <inheritdoc />
	protected override void MigrateLegacyItemId()
	{
		switch (base.UpgradeLevel)
		{
		case 0:
			base.ItemId = "Hoe";
			break;
		case 1:
			base.ItemId = "CopperHoe";
			break;
		case 2:
			base.ItemId = "SteelHoe";
			break;
		case 3:
			base.ItemId = "GoldHoe";
			break;
		case 4:
			base.ItemId = "IridiumHoe";
			break;
		default:
			base.ItemId = "Hoe";
			break;
		}
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Hoe();
	}

	public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
	{
		Vector2 vector = new Vector2(x / 64, y / 64);
		base.DoFunction(location, x, y, power, who);
		if (MineShaft.IsGeneratedLevel(location))
		{
			power = 1;
		}
		if (!isEfficient.Value)
		{
			who.Stamina -= (float)(2 * power) - (float)who.FarmingLevel * 0.1f;
		}
		power = who.toolPower.Value;
		who.stopJittering();
		if (PlayUseSounds)
		{
			location.playSound("woodyHit", vector);
		}
		List<Vector2> list = tilesAffected(vector, power, who);
		foreach (Vector2 item in list)
		{
			if (location.terrainFeatures.TryGetValue(item, out var value))
			{
				if (value.performToolAction(this, 0, item))
				{
					location.terrainFeatures.Remove(item);
				}
				continue;
			}
			if (location.objects.TryGetValue(item, out var value2) && value2.performToolAction(this))
			{
				if (value2.Type == "Crafting" && value2.fragility.Value != 2)
				{
					location.debris.Add(new Debris(value2.QualifiedItemId, who.GetToolLocation(), Utility.PointToVector2(who.StandingPixel)));
				}
				value2.performRemoveAction();
				location.Objects.Remove(item);
			}
			if (location.doesTileHaveProperty((int)item.X, (int)item.Y, "Diggable", "Back") == null)
			{
				continue;
			}
			if (location is MineShaft && !location.IsTileOccupiedBy(item, CollisionMask.All, CollisionMask.None, useFarmerTile: true))
			{
				if (location.makeHoeDirt(item))
				{
					if (PlayUseSounds)
					{
						location.playSound("hoeHit", item);
					}
					location.checkForBuriedItem((int)item.X, (int)item.Y, explosion: false, detectOnly: false, who);
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(12, new Vector2(vector.X * 64f, vector.Y * 64f), Color.White, 8, Game1.random.NextBool(), 50f));
					if (list.Count > 2)
					{
						Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(6, new Vector2(item.X * 64f, item.Y * 64f), Color.White, 8, Game1.random.NextBool(), Vector2.Distance(vector, item) * 30f));
					}
				}
			}
			else if (location.isTilePassable(new Location((int)item.X, (int)item.Y), Game1.viewport) && location.makeHoeDirt(item))
			{
				if (PlayUseSounds)
				{
					location.playSound("hoeHit", item);
				}
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(12, new Vector2(item.X * 64f, item.Y * 64f), Color.White, 8, Game1.random.NextBool(), 50f));
				if (list.Count > 2)
				{
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(6, new Vector2(item.X * 64f, item.Y * 64f), Color.White, 8, Game1.random.NextBool(), Vector2.Distance(vector, item) * 30f));
				}
				location.checkForBuriedItem((int)item.X, (int)item.Y, explosion: false, detectOnly: false, who);
			}
			Game1.stats.DirtHoed++;
		}
	}
}
