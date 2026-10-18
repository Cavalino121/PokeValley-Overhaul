using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile;
using xTile.Dimensions;

namespace StardewValley.Locations;

public class IslandLocation : GameLocation
{
	public const int TOTAL_WALNUTS = 130;

	[XmlIgnore]
	public List<ParrotPlatform> parrotPlatforms = new List<ParrotPlatform>();

	[XmlIgnore]
	public NetList<ParrotUpgradePerch, NetRef<ParrotUpgradePerch>> parrotUpgradePerches = new NetList<ParrotUpgradePerch, NetRef<ParrotUpgradePerch>>();

	[XmlIgnore]
	public NetList<Point, NetPoint> buriedNutPoints = new NetList<Point, NetPoint>();

	[XmlElement("locationGemBird")]
	public NetRef<IslandGemBird> locationGemBird = new NetRef<IslandGemBird>();

	[XmlIgnore]
	protected Texture2D _dayParallaxTexture;

	[XmlIgnore]
	protected Texture2D _nightParallaxTexture;

	[XmlIgnore]
	protected TemporaryAnimatedSpriteList underwaterSprites = new TemporaryAnimatedSpriteList();

	public IslandLocation()
	{
	}

	public void ApplyUnsafeMapOverride(string override_map, Microsoft.Xna.Framework.Rectangle? source_rect, Microsoft.Xna.Framework.Rectangle dest_rect)
	{
		ApplyMapOverride(override_map, source_rect, dest_rect);
		Microsoft.Xna.Framework.Rectangle rect = new Microsoft.Xna.Framework.Rectangle(dest_rect.X * 64, dest_rect.Y * 64, dest_rect.Width * 64, dest_rect.Height * 64);
		if (this == Game1.player.currentLocation)
		{
			Microsoft.Xna.Framework.Rectangle boundingBox = Game1.player.GetBoundingBox();
			if (rect.Intersects(boundingBox) && isCollidingPosition(boundingBox, Game1.viewport, isFarmer: true, 0, glider: false, Game1.player))
			{
				Game1.player.TemporaryPassableTiles.Add(rect);
			}
		}
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(parrotUpgradePerches, "parrotUpgradePerches").AddField(buriedNutPoints, "buriedNutPoints").AddField(locationGemBird, "locationGemBird");
	}

	public override string doesTileHaveProperty(int xTile, int yTile, string propertyName, string layerName, bool ignoreTileSheetProperties = false)
	{
		if (layerName == "Back" && propertyName == "Diggable" && IsBuriedNutLocation(new Point(xTile, yTile)))
		{
			return "T";
		}
		return base.doesTileHaveProperty(xTile, yTile, propertyName, layerName, ignoreTileSheetProperties);
	}

	public virtual void SetBuriedNutLocations()
	{
	}

	public virtual List<Vector2> GetAdditionalWalnutBushes()
	{
		return null;
	}

	public IslandLocation(string map, string name)
		: base(map, name)
	{
		SetBuriedNutLocations();
		foreach (LargeTerrainFeature largeTerrainFeature in largeTerrainFeatures)
		{
			if (largeTerrainFeature is Bush bush)
			{
				bush.setUpSourceRect();
			}
		}
	}

	/// <inheritdoc />
	public override bool SeedsIgnoreSeasonsHere()
	{
		return true;
	}

	/// <inheritdoc />
	public override bool catchOceanCrabPotFishFromThisSpot(int x, int y)
	{
		string id;
		FishAreaData data;
		return !TryGetFishAreaForTile(new Vector2(x, y), out id, out data);
	}

	public override bool answerDialogue(Response answer)
	{
		foreach (ParrotPlatform parrotPlatform in parrotPlatforms)
		{
			if (parrotPlatform.AnswerQuestion(answer))
			{
				return true;
			}
		}
		foreach (ParrotUpgradePerch parrotUpgradePerch in parrotUpgradePerches)
		{
			if (parrotUpgradePerch.AnswerQuestion(answer))
			{
				return true;
			}
		}
		return base.answerDialogue(answer);
	}

	public override void cleanupBeforePlayerExit()
	{
		foreach (ParrotPlatform parrotPlatform in parrotPlatforms)
		{
			parrotPlatform.Cleanup();
		}
		foreach (ParrotUpgradePerch parrotUpgradePerch in parrotUpgradePerches)
		{
			parrotUpgradePerch.Cleanup();
		}
		_dayParallaxTexture = null;
		_nightParallaxTexture = null;
		underwaterSprites.Clear();
		base.cleanupBeforePlayerExit();
	}

	public override bool isCollidingPosition(Microsoft.Xna.Framework.Rectangle position, xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character, bool pathfinding, bool projectile = false, bool ignoreCharacterRequirement = false, bool skipCollisionEffects = false)
	{
		foreach (ParrotPlatform parrotPlatform in parrotPlatforms)
		{
			if (parrotPlatform.CheckCollisions(position))
			{
				return true;
			}
		}
		return base.isCollidingPosition(position, viewport, isFarmer, damagesFarmer, glider, character, pathfinding, projectile, ignoreCharacterRequirement);
	}

	protected void addMoonlightJellies(int numTries, Random r, Microsoft.Xna.Framework.Rectangle exclusionRect)
	{
		for (int i = 0; i < numTries; i++)
		{
			Point value = new Point(r.Next(base.Map.Layers[0].LayerWidth), r.Next(base.Map.Layers[0].LayerHeight));
			if (!isOpenWater(value.X, value.Y) || exclusionRect.Contains(value) || FishingRod.distanceToLand(value.X, value.Y, this) < 2)
			{
				continue;
			}
			bool flag = false;
			foreach (TemporaryAnimatedSprite underwaterSprite in underwaterSprites)
			{
				Point point = new Point((int)underwaterSprite.position.X / 64, (int)underwaterSprite.position.Y / 64);
				if (Utility.distance(value.X, point.X, value.Y, point.Y) <= 2f)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				underwaterSprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle((r.NextDouble() < 0.2) ? 304 : 256, (r.NextDouble() < 0.01) ? 32 : 16, 16, 16), 250f, 3, 9999, new Vector2(value.X, value.Y) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White * 0.66f, 4f, 0f, 0f, 0f)
				{
					yPeriodic = (Game1.random.NextDouble() < 0.76),
					yPeriodicRange = 12f,
					yPeriodicLoopTime = Game1.random.Next(5500, 8000),
					xPeriodic = (Game1.random.NextDouble() < 0.76),
					xPeriodicLoopTime = Game1.random.Next(5500, 8000),
					xPeriodicRange = 16f,
					lightId = $"{base.NameOrUniqueName}_MoonlightJelly_{value.X}_{value.Y}",
					lightcolor = Color.Black,
					lightRadius = 1f,
					pingPong = true
				});
			}
		}
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		if (Game1.currentLocation == this)
		{
			foreach (ParrotPlatform parrotPlatform in parrotPlatforms)
			{
				parrotPlatform.Update(time);
			}
		}
		foreach (ParrotUpgradePerch parrotUpgradePerch in parrotUpgradePerches)
		{
			parrotUpgradePerch.Update(time);
		}
		underwaterSprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.update(time));
		base.UpdateWhenCurrentLocation(time);
	}

	public override void tryToAddCritters(bool onlyIfOnScreen = false)
	{
		if (Game1.random.NextDouble() < 0.20000000298023224 && !IsRainingHere() && !Game1.isDarkOut(this))
		{
			Vector2 vector = ((!(Game1.random.NextDouble() < 0.75)) ? new Vector2(Game1.viewport.X + Game1.viewport.Width + 64, Utility.RandomFloat(0f, Game1.viewport.Height)) : new Vector2((float)Game1.viewport.X + Utility.RandomFloat(0f, Game1.viewport.Width), Game1.viewport.Y - 64));
			int num = 1;
			if (Game1.random.NextBool())
			{
				num++;
			}
			if (Game1.random.NextBool())
			{
				num++;
			}
			for (int i = 0; i < num; i++)
			{
				addCritter(new OverheadParrot(vector + new Vector2(i * 64, -i * 64)));
			}
		}
		if (!IsRainingHere())
		{
			double num2 = map.Layers[0].LayerWidth * map.Layers[0].LayerHeight;
			double chance = Math.Max(0.1, Math.Min(0.25, num2 / 15000.0));
			addButterflies(chance, onlyIfOnScreen);
		}
	}

	public override void DayUpdate(int dayOfMonth)
	{
		base.DayUpdate(dayOfMonth);
		locationGemBird.Value = null;
	}

	public override void updateEvenIfFarmerIsntHere(GameTime time, bool ignoreWasUpdatedFlush = false)
	{
		base.updateEvenIfFarmerIsntHere(time, ignoreWasUpdatedFlush);
		foreach (ParrotUpgradePerch parrotUpgradePerch in parrotUpgradePerches)
		{
			parrotUpgradePerch.UpdateEvenIfFarmerIsntHere(time);
		}
		if (locationGemBird.Value != null && locationGemBird.Value.Update(time, this) && Game1.IsMasterGame)
		{
			locationGemBird.Value = null;
		}
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		base.TransferDataFromSavedLocation(l);
		foreach (ParrotUpgradePerch parrotUpgradePerch in parrotUpgradePerches)
		{
			parrotUpgradePerch.UpdateCompletionStatus();
		}
		if (l is IslandLocation islandLocation)
		{
			locationGemBird.Value = islandLocation.locationGemBird.Value;
		}
	}

	public void AddAdditionalWalnutBushes()
	{
		List<Vector2> additionalWalnutBushes = GetAdditionalWalnutBushes();
		if (additionalWalnutBushes == null)
		{
			return;
		}
		foreach (Vector2 item in additionalWalnutBushes)
		{
			if (!(getLargeTerrainFeatureAt((int)item.X, (int)item.Y) is Bush bush) || bush.size.Value != 4)
			{
				largeTerrainFeatures.Add(new Bush(new Vector2((int)item.X, (int)item.Y), 4, this));
			}
		}
	}

	public override bool isActionableTile(int xTile, int yTile, Farmer who)
	{
		foreach (ParrotUpgradePerch parrotUpgradePerch in parrotUpgradePerches)
		{
			if (parrotUpgradePerch.IsAtTile(xTile, yTile) && parrotUpgradePerch.IsAvailable(use_cached_value: true) && parrotUpgradePerch.parrotPresent)
			{
				return true;
			}
		}
		return base.isActionableTile(xTile, yTile, who);
	}

	public override string checkForBuriedItem(int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
	{
		if (IsBuriedNutLocation(new Point(xLocation, yLocation)))
		{
			Game1.player.team.MarkCollectedNut("Buried_" + base.Name + "_" + xLocation + "_" + yLocation);
			Game1.multiplayer.broadcastNutDig(this, new Point(xLocation, yLocation));
			return "";
		}
		return base.checkForBuriedItem(xLocation, yLocation, explosion, detectOnly, who);
	}

	public override void digUpArtifactSpot(int xLocation, int yLocation, Farmer who)
	{
		Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation);
		string text = null;
		int num = 1;
		if (Game1.netWorldState.Value.GoldenCoconutCracked && random.NextDouble() < 0.1)
		{
			text = "(O)791";
		}
		else if (random.NextDouble() < 0.33)
		{
			text = "(O)831";
			num = random.Next(2, 5);
		}
		else if (random.NextDouble() < 0.15)
		{
			text = "(O)275";
			num = random.Next(1, 3);
		}
		if (text != null)
		{
			for (int i = 0; i < num; i++)
			{
				Game1.createItemDebris(ItemRegistry.Create(text), new Vector2(xLocation, yLocation) * 64f, -1, this);
			}
		}
		base.digUpArtifactSpot(xLocation, yLocation, who);
	}

	public virtual bool IsBuriedNutLocation(Point point)
	{
		foreach (Point buriedNutPoint in buriedNutPoints)
		{
			if (buriedNutPoint == point)
			{
				return true;
			}
		}
		return false;
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		foreach (ParrotUpgradePerch parrotUpgradePerch in parrotUpgradePerches)
		{
			if (parrotUpgradePerch.CheckAction(tileLocation, who))
			{
				return true;
			}
		}
		return base.checkAction(tileLocation, viewport, who);
	}

	public override Item getFish(float millisecondsAfterNibble, string bait, int waterDepth, Farmer who, double baitPotency, Vector2 bobberTile, string locationName = null)
	{
		if (Utility.CreateRandom(Game1.stats.DaysPlayed, Game1.stats.TimesFished, Game1.uniqueIDForThisGame).NextDouble() < 0.15)
		{
			if (!Game1.player.team.limitedNutDrops.TryGetValue("IslandFishing", out var value))
			{
				value = 0;
			}
			if (value < 5)
			{
				if (!Game1.IsMultiplayer)
				{
					Game1.player.team.limitedNutDrops["IslandFishing"] = value + 1;
					return ItemRegistry.Create("(O)73");
				}
				Game1.player.team.RequestLimitedNutDrops("IslandFishing", this, (int)bobberTile.X * 64, (int)bobberTile.Y * 64, 5);
				return null;
			}
		}
		return base.getFish(millisecondsAfterNibble, bait, waterDepth, who, baitPotency, bobberTile, locationName);
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		foreach (ParrotPlatform parrotPlatform in parrotPlatforms)
		{
			parrotPlatform.Draw(b);
		}
		foreach (ParrotUpgradePerch parrotUpgradePerch in parrotUpgradePerches)
		{
			parrotUpgradePerch.Draw(b);
		}
		locationGemBird.Value?.Draw(b);
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		base.drawAboveAlwaysFrontLayer(b);
		foreach (ParrotUpgradePerch parrotUpgradePerch in parrotUpgradePerches)
		{
			parrotUpgradePerch.DrawAboveAlwaysFrontLayer(b);
		}
	}

	public override bool IsLocationSpecificOccupantOnTile(Vector2 tileLocation)
	{
		foreach (ParrotPlatform parrotPlatform in parrotPlatforms)
		{
			if (parrotPlatform.OccupiesTile(tileLocation))
			{
				return true;
			}
		}
		return base.IsLocationSpecificOccupantOnTile(tileLocation);
	}

	protected override void resetLocalState()
	{
		parrotPlatforms.Clear();
		parrotPlatforms = ParrotPlatform.CreateParrotPlatformsForArea(this);
		foreach (ParrotUpgradePerch parrotUpgradePerch in parrotUpgradePerches)
		{
			parrotUpgradePerch.ResetForPlayerEntry();
		}
		base.resetLocalState();
	}

	/// <inheritdoc />
	public override void seasonUpdate(bool onLoad = false)
	{
	}

	public override void updateSeasonalTileSheets(Map map = null)
	{
	}

	public override void drawWater(SpriteBatch b)
	{
		foreach (TemporaryAnimatedSprite underwaterSprite in underwaterSprites)
		{
			underwaterSprite.draw(b);
		}
		base.drawWater(b);
	}

	public virtual void DrawParallaxHorizon(SpriteBatch b, bool horizontal_parallax = true)
	{
		float num = 4f;
		if (_dayParallaxTexture == null || _dayParallaxTexture.IsDisposed)
		{
			_dayParallaxTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\Cloudy_Ocean_BG");
		}
		if (_nightParallaxTexture == null || _nightParallaxTexture.IsDisposed)
		{
			_nightParallaxTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\Cloudy_Ocean_BG_Night");
		}
		float num2 = (float)_dayParallaxTexture.Width * num - (float)map.DisplayWidth;
		float t = 0f;
		int num3 = -640;
		int y = (int)((float)Game1.viewport.Y * 0.2f + (float)num3);
		if (horizontal_parallax)
		{
			if (map.DisplayWidth - Game1.viewport.Width < 0)
			{
				t = 0.5f;
			}
			else if (map.DisplayWidth - Game1.viewport.Width > 0)
			{
				t = (float)Game1.viewport.X / (float)(map.DisplayWidth - Game1.viewport.Width);
			}
		}
		else
		{
			t = 0.5f;
		}
		if (Game1.game1.takingMapScreenshot)
		{
			y = num3;
			t = 0.5f;
		}
		float num4 = 0.25f;
		t = Utility.Lerp(0.5f + num4, 0.5f - num4, t);
		float value = (float)Utility.ConvertTimeToMinutes(Game1.timeOfDay + (int)((float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameMinute % 10f) - Game1.getStartingToGetDarkTime(this)) / (float)Utility.ConvertTimeToMinutes(Game1.getTrulyDarkTime(this) - Game1.getStartingToGetDarkTime(this));
		value = Utility.Clamp(value, 0f, 1f);
		b.Draw(Game1.staminaRect, Game1.GlobalToLocal(Game1.viewport, new Microsoft.Xna.Framework.Rectangle(0, 0, map.DisplayWidth, map.DisplayHeight)), new Color(1, 122, 217, 255));
		b.Draw(Game1.staminaRect, Game1.GlobalToLocal(Game1.viewport, new Microsoft.Xna.Framework.Rectangle(0, 0, map.DisplayWidth, map.DisplayHeight)), new Color(0, 7, 63, 255) * value);
		Microsoft.Xna.Framework.Rectangle globalPosition = new Microsoft.Xna.Framework.Rectangle((int)((0f - num2) * t), y, (int)((float)_dayParallaxTexture.Width * num), (int)((float)_dayParallaxTexture.Height * num));
		Microsoft.Xna.Framework.Rectangle value2 = new Microsoft.Xna.Framework.Rectangle(0, 0, _dayParallaxTexture.Width, _dayParallaxTexture.Height);
		int num5 = 0;
		if (globalPosition.X < num5)
		{
			int num6 = num5 - globalPosition.X;
			globalPosition.X += num6;
			globalPosition.Width -= num6;
			value2.X += (int)((float)num6 / num);
			value2.Width -= (int)((float)num6 / num);
		}
		int displayWidth = map.DisplayWidth;
		if (globalPosition.X + globalPosition.Width > displayWidth)
		{
			int num7 = globalPosition.X + globalPosition.Width - displayWidth;
			globalPosition.Width -= num7;
			value2.Width -= (int)((float)num7 / num);
		}
		if (value2.Width > 0 && globalPosition.Width > 0)
		{
			b.Draw(_dayParallaxTexture, Game1.GlobalToLocal(Game1.viewport, globalPosition), value2, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
			b.Draw(_nightParallaxTexture, Game1.GlobalToLocal(Game1.viewport, globalPosition), value2, Color.White * value, 0f, Vector2.Zero, SpriteEffects.None, 0f);
		}
	}

	/// <summary>Get whether the moonlight jellies are out right now.</summary>
	public bool AreMoonlightJelliesOut()
	{
		if (Game1.IsWinter)
		{
			if (base.IsOutdoors)
			{
				if (!IsRainingHere())
				{
					return Game1.isDarkOut(this);
				}
				return false;
			}
			return true;
		}
		return false;
	}
}
