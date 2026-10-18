using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;

namespace StardewValley.Objects;

public class CrabPot : Object
{
	public const int lidFlapTimerInterval = 60;

	[XmlIgnore]
	public float yBob;

	[XmlElement("directionOffset")]
	public readonly NetVector2 directionOffset = new NetVector2();

	[XmlElement("bait")]
	public readonly NetRef<Object> bait = new NetRef<Object>();

	public int tileIndexToShow;

	[XmlIgnore]
	public bool lidFlapping;

	[XmlIgnore]
	public bool lidClosing;

	[XmlIgnore]
	public float lidFlapTimer;

	[XmlIgnore]
	public new float shakeTimer;

	[XmlIgnore]
	public Vector2 shake;

	[XmlIgnore]
	private int ignoreRemovalTimer;

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(directionOffset, "directionOffset").AddField(bait, "bait");
	}

	public CrabPot()
		: base("710", 1)
	{
		base.CanBeGrabbed = false;
		type.Value = "interactive";
		tileIndexToShow = base.ParentSheetIndex;
	}

	/// <summary>Get whether the crab pot currently needs to be filled with bait before it can catch something.</summary>
	/// <param name="player">The player checking the crab pot.</param>
	public bool NeedsBait(Farmer player)
	{
		if (bait.Value != null)
		{
			return false;
		}
		return !(Game1.GetPlayer(owner.Value) ?? player ?? Game1.player).professions.Contains(11);
	}

	public List<Vector2> getOverlayTiles()
	{
		List<Vector2> list = new List<Vector2>();
		if (Location != null)
		{
			if (directionOffset.Y < 0f)
			{
				addOverlayTilesIfNecessary((int)TileLocation.X, (int)tileLocation.Y, list);
			}
			addOverlayTilesIfNecessary((int)TileLocation.X, (int)tileLocation.Y + 1, list);
			if (directionOffset.X < 0f)
			{
				addOverlayTilesIfNecessary((int)TileLocation.X - 1, (int)tileLocation.Y + 1, list);
			}
			if (directionOffset.X > 0f)
			{
				addOverlayTilesIfNecessary((int)TileLocation.X + 1, (int)tileLocation.Y + 1, list);
			}
		}
		return list;
	}

	protected void addOverlayTilesIfNecessary(int tile_x, int tile_y, List<Vector2> tiles)
	{
		GameLocation location = Location;
		if (location != null && location == Game1.currentLocation && location.hasTileAt(tile_x, tile_y, "Buildings") && !location.isWaterTile(tile_x, tile_y + 1))
		{
			tiles.Add(new Vector2(tile_x, tile_y));
		}
	}

	/// <summary>Add any tiles that might overlap with this crab pot incorrectly to the <see cref="F:StardewValley.Game1.crabPotOverlayTiles" /> dictionary.</summary>
	public void addOverlayTiles()
	{
		GameLocation location = Location;
		if (location == null || location != Game1.currentLocation)
		{
			return;
		}
		foreach (Vector2 overlayTile in getOverlayTiles())
		{
			if (!Game1.crabPotOverlayTiles.TryGetValue(overlayTile, out var value))
			{
				value = (Game1.crabPotOverlayTiles[overlayTile] = 0);
			}
			Game1.crabPotOverlayTiles[overlayTile] = value + 1;
		}
	}

	/// <summary>Remove any tiles that might overlap with this crab pot incorrectly from the <see cref="F:StardewValley.Game1.crabPotOverlayTiles" /> dictionary.</summary>
	public void removeOverlayTiles()
	{
		if (Location == null || Location != Game1.currentLocation)
		{
			return;
		}
		foreach (Vector2 overlayTile in getOverlayTiles())
		{
			if (Game1.crabPotOverlayTiles.TryGetValue(overlayTile, out var value))
			{
				value--;
				if (value <= 0)
				{
					Game1.crabPotOverlayTiles.Remove(overlayTile);
				}
				else
				{
					Game1.crabPotOverlayTiles[overlayTile] = value;
				}
			}
		}
	}

	public static bool IsValidCrabPotLocationTile(GameLocation location, int x, int y)
	{
		if (location is Caldera || location is VolcanoDungeon || location is MineShaft)
		{
			return false;
		}
		Vector2 key = new Vector2(x, y);
		bool flag = (location.isWaterTile(x + 1, y) && location.isWaterTile(x - 1, y)) || (location.isWaterTile(x, y + 1) && location.isWaterTile(x, y - 1));
		if (location.objects.ContainsKey(key) || !flag || !location.isWaterTile((int)key.X, (int)key.Y) || location.doesTileHaveProperty((int)key.X, (int)key.Y, "Passable", "Buildings") != null)
		{
			return false;
		}
		return true;
	}

	/// <inheritdoc />
	public override void actionOnPlayerEntry()
	{
		updateOffset();
		addOverlayTiles();
		base.actionOnPlayerEntry();
	}

	public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
	{
		Vector2 vector = new Vector2(x / 64, y / 64);
		if (who != null)
		{
			owner.Value = who.UniqueMultiplayerID;
		}
		if (!IsValidCrabPotLocationTile(location, (int)vector.X, (int)vector.Y))
		{
			return false;
		}
		TileLocation = vector;
		location.objects.Add(tileLocation.Value, this);
		location.playSound("waterSlosh");
		DelayedAction.playSoundAfterDelay("slosh", 150);
		updateOffset();
		addOverlayTiles();
		return true;
	}

	public void updateOffset()
	{
		Vector2 zero = Vector2.Zero;
		if (checkLocation(tileLocation.X - 1f, tileLocation.Y))
		{
			zero += new Vector2(32f, 0f);
		}
		if (checkLocation(tileLocation.X + 1f, tileLocation.Y))
		{
			zero += new Vector2(-32f, 0f);
		}
		if (zero.X != 0f && checkLocation(tileLocation.X + (float)Math.Sign(zero.X), tileLocation.Y + 1f))
		{
			zero += new Vector2(0f, -42f);
		}
		if (checkLocation(tileLocation.X, tileLocation.Y - 1f))
		{
			zero += new Vector2(0f, 32f);
		}
		if (checkLocation(tileLocation.X, tileLocation.Y + 1f))
		{
			zero += new Vector2(0f, -42f);
		}
		directionOffset.Value = zero;
	}

	protected bool checkLocation(float tile_x, float tile_y)
	{
		GameLocation location = Location;
		if (!location.isWaterTile((int)tile_x, (int)tile_y) || location.doesTileHaveProperty((int)tile_x, (int)tile_y, "Passable", "Buildings") != null)
		{
			return true;
		}
		return false;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Object(base.ItemId, 1);
	}

	/// <inheritdoc />
	public override bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return false;
		}
		if (dropInItem is Object { Category: -21 } obj && NeedsBait(who))
		{
			if (!probe)
			{
				if (who != null)
				{
					owner.Value = who.UniqueMultiplayerID;
				}
				bait.Value = obj.getOne() as Object;
				location.playSound("Ship");
				lidFlapping = true;
				lidFlapTimer = 60f;
			}
			return true;
		}
		return false;
	}

	/// <inheritdoc />
	public override bool AttemptAutoLoad(IInventory inventory, Farmer who)
	{
		Object value = bait.Value;
		if (base.AttemptAutoLoad(inventory, who) && value != bait.Value)
		{
			inventory.ReduceId(bait.Value.QualifiedItemId, bait.Value.Stack);
			return true;
		}
		return false;
	}

	/// <inheritdoc />
	public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return false;
		}
		if (tileIndexToShow == 714)
		{
			if (justCheckingForActivity)
			{
				return true;
			}
			Object value = heldObject.Value;
			if (value != null)
			{
				int num = value.Stack;
				if (Utility.CreateDaySaveRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed * 77, tileLocation.X * 777f + tileLocation.Y).NextDouble() < 0.25 && Game1.player.stats.Get("Book_Crabbing") != 0 && who.couldInventoryAcceptThisItem(value.QualifiedItemId, num * 2, value.Quality))
				{
					num *= 2;
				}
				value.Stack = num;
				heldObject.Value = null;
				if (who.IsLocalPlayer && !who.addItemToInventoryBool(value))
				{
					heldObject.Value = value;
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
					return false;
				}
				if (DataLoader.Fish(Game1.content).TryGetValue(value.ItemId, out var value2))
				{
					string[] array = value2.Split('/');
					int minValue = ((array.Length <= 5) ? 1 : Convert.ToInt32(array[5]));
					int num2 = ((array.Length > 5) ? Convert.ToInt32(array[6]) : 10);
					who.caughtFish(value.QualifiedItemId, Game1.random.Next(minValue, num2 + 1), from_fish_pond: false, num);
				}
				who.gainExperience(1, 5);
			}
			readyForHarvest.Value = false;
			tileIndexToShow = 710;
			lidFlapping = true;
			lidFlapTimer = 60f;
			bait.Value = null;
			who.animateOnce(279 + who.FacingDirection);
			location.playSound("fishingRodBend");
			DelayedAction.playSoundAfterDelay("coin", 500);
			shake = Vector2.Zero;
			shakeTimer = 0f;
			ignoreRemovalTimer = 750;
			return true;
		}
		if (bait.Value == null && ignoreRemovalTimer <= 0)
		{
			if (justCheckingForActivity)
			{
				return true;
			}
			if (Game1.didPlayerJustClickAtAll(ignoreNonMouseHeldInput: true))
			{
				if (Game1.player.addItemToInventoryBool(getOne()))
				{
					if (who.isMoving())
					{
						Game1.haltAfterCheck = false;
					}
					Game1.playSound("coin");
					location.objects.Remove(tileLocation.Value);
					return true;
				}
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
			}
		}
		return false;
	}

	public override void performRemoveAction()
	{
		removeOverlayTiles();
		base.performRemoveAction();
	}

	public override void DayUpdate()
	{
		GameLocation location = Location;
		Farmer farmer = Game1.GetPlayer(owner.Value) ?? Game1.MasterPlayer;
		bool flag = farmer.professions.Contains(10);
		if (NeedsBait(farmer) || heldObject.Value != null)
		{
			return;
		}
		tileIndexToShow = 714;
		readyForHarvest.Value = true;
		Random random = Utility.CreateDaySaveRandom(tileLocation.X * 1000f, tileLocation.Y * 255f, directionOffset.X * 1000f + directionOffset.Y);
		List<string> list = new List<string>();
		if (!location.TryGetFishAreaForTile(tileLocation.Value, out var _, out var data))
		{
			data = null;
		}
		double num = (flag ? 0.0 : (((double?)data?.CrabPotJunkChance) ?? 0.2));
		int amount = 1;
		int num2 = 0;
		string text = null;
		switch (bait.Value?.QualifiedItemId)
		{
		case "(O)DeluxeBait":
			num2 = 1;
			num /= 2.0;
			break;
		case "(O)774":
			num /= 2.0;
			if (random.NextBool(0.25))
			{
				amount = 2;
			}
			break;
		case "(O)SpecificBait":
			if (bait.Value.preservedParentSheetIndex.Value != null && bait.Value.preserve.Value.HasValue)
			{
				text = bait.Value.preservedParentSheetIndex.Value;
				num /= 2.0;
			}
			break;
		}
		if (!random.NextBool(num))
		{
			IList<string> crabPotFishForTile = location.GetCrabPotFishForTile(tileLocation.Value);
			foreach (KeyValuePair<string, string> item in DataLoader.Fish(Game1.content))
			{
				if (!item.Value.Contains("trap"))
				{
					continue;
				}
				string[] array = item.Value.Split('/');
				string[] array2 = ArgUtility.SplitBySpace(array[4]);
				bool flag2 = false;
				string[] array3 = array2;
				foreach (string text2 in array3)
				{
					foreach (string item2 in crabPotFishForTile)
					{
						if (text2 == item2)
						{
							flag2 = true;
							break;
						}
					}
				}
				if (!flag2)
				{
					continue;
				}
				if (flag)
				{
					list.Add(item.Key);
					continue;
				}
				double num3 = Convert.ToDouble(array[2]);
				if (text != null && text == item.Key)
				{
					num3 *= (double)((num3 < 0.1) ? 4 : ((num3 < 0.2) ? 3 : 2));
				}
				if (!(random.NextDouble() < num3))
				{
					continue;
				}
				heldObject.Value = ItemRegistry.Create<Object>("(O)" + item.Key, amount, num2);
				break;
			}
		}
		if (heldObject.Value == null)
		{
			if (flag && list.Count > 0)
			{
				heldObject.Value = ItemRegistry.Create<Object>("(O)" + random.ChooseFrom(list), amount, num2);
			}
			else
			{
				heldObject.Value = ItemRegistry.Create<Object>("(O)" + random.Next(168, 173));
			}
		}
	}

	public override void updateWhenCurrentLocation(GameTime time)
	{
		if (lidFlapping)
		{
			lidFlapTimer -= time.ElapsedGameTime.Milliseconds;
			if (lidFlapTimer <= 0f)
			{
				tileIndexToShow += ((!lidClosing) ? 1 : (-1));
				if (tileIndexToShow >= 713 && !lidClosing)
				{
					lidClosing = true;
					tileIndexToShow--;
				}
				else if (tileIndexToShow <= 709 && lidClosing)
				{
					lidClosing = false;
					tileIndexToShow++;
					lidFlapping = false;
					if (bait.Value != null)
					{
						tileIndexToShow = 713;
					}
				}
				lidFlapTimer = 60f;
			}
		}
		if (readyForHarvest.Value && heldObject.Value != null)
		{
			shakeTimer -= time.ElapsedGameTime.Milliseconds;
			if (shakeTimer < 0f)
			{
				shakeTimer = Game1.random.Next(2800, 3200);
			}
		}
		if (shakeTimer > 2000f)
		{
			shake.X = Game1.random.Next(-1, 2);
		}
		else
		{
			shake.X = 0f;
		}
		if (ignoreRemovalTimer > 0)
		{
			ignoreRemovalTimer -= (int)time.ElapsedGameTime.TotalMilliseconds;
		}
	}

	public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return;
		}
		if (heldObject.Value != null)
		{
			tileIndexToShow = 714;
		}
		else if (tileIndexToShow == 0)
		{
			tileIndexToShow = base.ParentSheetIndex;
		}
		yBob = (float)(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500.0 + (double)(x * 64)) * 8.0 + 8.0);
		if (yBob <= 0.001f)
		{
			location.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 150f, 8, 0, directionOffset.Value + new Vector2(x * 64 + 4, y * 64 + 32), flicker: false, Game1.random.NextBool(), 0.001f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f));
		}
		spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, directionOffset.Value + new Vector2(x * 64, y * 64 + (int)yBob)) + shake, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, tileIndexToShow, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, ((float)(y * 64) + directionOffset.Y + (float)(x % 4)) / 10000f);
		if (location.waterTiles != null && x < location.waterTiles.waterTiles.GetLength(0) && y < location.waterTiles.waterTiles.GetLength(1) && location.waterTiles.waterTiles[x, y].isWater)
		{
			if (location.waterTiles.waterTiles[x, y].isVisible)
			{
				spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, directionOffset.Value + new Vector2(x * 64 + 4, y * 64 + 48)) + shake, new Rectangle(location.waterAnimationIndex * 64, 2112 + (((x + y) % 2 != 0) ? ((!location.waterTileFlip) ? 128 : 0) : (location.waterTileFlip ? 128 : 0)), 56, 16 + (int)yBob), location.waterColor.Value, 0f, Vector2.Zero, 1f, SpriteEffects.None, ((float)(y * 64) + directionOffset.Y + (float)(x % 4)) / 9999f);
			}
			else
			{
				Color a = new Color(135, 135, 135, 215);
				a = Utility.MultiplyColor(a, location.waterColor.Value);
				spriteBatch.Draw(Game1.staminaRect, Game1.GlobalToLocal(Game1.viewport, directionOffset.Value + new Vector2(x * 64 + 4, y * 64 + 48)) + shake, null, a, 0f, Vector2.Zero, new Vector2(56f, 16 + (int)yBob), SpriteEffects.None, ((float)(y * 64) + directionOffset.Y + (float)(x % 4)) / 9999f);
			}
		}
		if (readyForHarvest.Value && heldObject.Value != null)
		{
			float num = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, directionOffset.Value + new Vector2(x * 64 - 8, (float)(y * 64 - 96 - 16) + num)), new Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)((y + 1) * 64) / 10000f + 1E-06f + tileLocation.X / 10000f);
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
			spriteBatch.Draw(dataOrErrorItem.GetTexture(), Game1.GlobalToLocal(Game1.viewport, directionOffset.Value + new Vector2(x * 64 + 32, (float)(y * 64 - 64 - 8) + num)), dataOrErrorItem.GetSourceRect(), Color.White * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, (float)((y + 1) * 64) / 10000f + 1E-05f + tileLocation.X / 10000f);
		}
	}
}
