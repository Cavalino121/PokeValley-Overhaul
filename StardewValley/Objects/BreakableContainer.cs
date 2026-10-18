using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Constants;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects.Trinkets;
using StardewValley.Tools;

namespace StardewValley.Objects;

public class BreakableContainer : Object
{
	public const string barrelId = "118";

	public const string frostBarrelId = "120";

	public const string darkBarrelId = "122";

	public const string desertBarrelId = "124";

	public const string volcanoBarrelId = "174";

	public const string waterBarrelId = "262";

	[XmlElement("debris")]
	private readonly NetInt debris = new NetInt();

	private new int shakeTimer;

	[XmlElement("health")]
	private new readonly NetInt health = new NetInt();

	[XmlElement("hitSound")]
	private readonly NetString hitSound = new NetString();

	[XmlElement("breakSound")]
	private readonly NetString breakSound = new NetString();

	[XmlElement("breakDebrisSource")]
	private readonly NetRectangle breakDebrisSource = new NetRectangle();

	[XmlElement("breakDebrisSource2")]
	private readonly NetRectangle breakDebrisSource2 = new NetRectangle();

	/// <inheritdoc />
	public override string TypeDefinitionId => "(BC)";

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(debris, "debris").AddField(health, "health").AddField(hitSound, "hitSound")
			.AddField(breakSound, "breakSound")
			.AddField(breakDebrisSource, "breakDebrisSource")
			.AddField(breakDebrisSource2, "breakDebrisSource2");
	}

	public BreakableContainer()
	{
	}

	public BreakableContainer(Vector2 tile, string itemId, int health = 3, int debrisType = 12, string hitSound = "woodWhack", string breakSound = "barrelBreak")
		: base(tile, itemId)
	{
		this.health.Value = health;
		debris.Value = debrisType;
		this.hitSound.Value = hitSound;
		this.breakSound.Value = breakSound;
		breakDebrisSource.Value = new Rectangle(598, 1275, 13, 4);
		breakDebrisSource2.Value = new Rectangle(611, 1275, 10, 4);
	}

	/// <summary>Get a barrel to place in the mines or Skull Cavern.</summary>
	/// <param name="tile">The tile position at which it'll be placed.</param>
	/// <param name="mine">The mine level.</param>
	public static BreakableContainer GetBarrelForMines(Vector2 tile, MineShaft mine)
	{
		int mineArea = mine.getMineArea();
		string text = ((mine.GetAdditionalDifficulty() > 0) ? (((mineArea == 0 || mineArea == 10) && !mine.isDarkArea()) ? "262" : "118") : (mineArea switch
		{
			40 => "120", 
			80 => "122", 
			121 => "124", 
			_ => "118", 
		}));
		BreakableContainer breakableContainer = new BreakableContainer(tile, text);
		if (Game1.random.NextBool())
		{
			breakableContainer.showNextIndex.Value = true;
		}
		return breakableContainer;
	}

	/// <summary>Get a barrel to place in the Volcano Dungeon.</summary>
	/// <param name="tile">The tile position at which it'll be placed.</param>
	public static BreakableContainer GetBarrelForVolcanoDungeon(Vector2 tile)
	{
		BreakableContainer breakableContainer = new BreakableContainer(tile, "174", 4, 14, "clank", "boulderBreak");
		if (Game1.random.NextBool())
		{
			breakableContainer.showNextIndex.Value = true;
		}
		return breakableContainer;
	}

	public override bool performToolAction(Tool t)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return false;
		}
		if (t != null && t.isHeavyHitter())
		{
			health.Value--;
			if (t is MeleeWeapon meleeWeapon && meleeWeapon.type.Value == 2)
			{
				health.Value--;
			}
			if (health.Value <= 0)
			{
				if (!string.IsNullOrEmpty(breakSound.Value))
				{
					playNearbySoundAll(breakSound.Value);
				}
				releaseContents(t.getLastFarmerToUse());
				location.objects.Remove(tileLocation.Value);
				int num = Game1.random.Next(4, 12);
				Color chipColor = GetChipColor();
				for (int i = 0; i < num; i++)
				{
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", Game1.random.NextBool() ? breakDebrisSource.Value : breakDebrisSource2.Value, 999f, 1, 0, tileLocation.Value * 64f + new Vector2(32f, 32f), flicker: false, Game1.random.NextBool(), (tileLocation.Y * 64f + 32f) / 10000f, 0.01f, chipColor, 4f, 0f, (float)Game1.random.Next(-5, 6) * (float)Math.PI / 8f, (float)Game1.random.Next(-5, 6) * (float)Math.PI / 64f)
					{
						motion = new Vector2((float)Game1.random.Next(-30, 31) / 10f, Game1.random.Next(-10, -7)),
						acceleration = new Vector2(0f, 0.3f)
					});
				}
			}
			else if (!string.IsNullOrEmpty(hitSound.Value))
			{
				shakeTimer = 300;
				playNearbySoundAll(hitSound.Value);
				Color? color = ((base.ItemId == "120") ? new Color?(Color.White) : ((Color?)null));
				Game1.createRadialDebris(location, debris.Value, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(4, 7), resource: false, -1, item: false, color);
			}
		}
		return false;
	}

	public override bool onExplosion(Farmer who)
	{
		if (who == null)
		{
			who = Game1.player;
		}
		GameLocation location = Location;
		if (location == null)
		{
			return true;
		}
		releaseContents(who);
		int num = Game1.random.Next(4, 12);
		Color chipColor = GetChipColor();
		for (int i = 0; i < num; i++)
		{
			Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", Game1.random.NextBool() ? breakDebrisSource.Value : breakDebrisSource2.Value, 999f, 1, 0, tileLocation.Value * 64f + new Vector2(32f, 32f), flicker: false, Game1.random.NextBool(), (tileLocation.Y * 64f + 32f) / 10000f, 0.01f, chipColor, 4f, 0f, (float)Game1.random.Next(-5, 6) * (float)Math.PI / 8f, (float)Game1.random.Next(-5, 6) * (float)Math.PI / 64f)
			{
				motion = new Vector2((float)Game1.random.Next(-30, 31) / 10f, Game1.random.Next(-10, -7)),
				acceleration = new Vector2(0f, 0.3f)
			});
		}
		return true;
	}

	/// <summary>Get the color of cosmetic chip debris when breaking this container.</summary>
	public Color GetChipColor()
	{
		return base.ItemId switch
		{
			"120" => Color.White, 
			"122" => new Color(109, 122, 80), 
			"174" => new Color(107, 76, 83), 
			_ => new Color(130, 80, 30), 
		};
	}

	public void releaseContents(Farmer who)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return;
		}
		Random random = Utility.CreateRandom(tileLocation.X, (double)tileLocation.Y * 10000.0, Game1.stats.DaysPlayed, (location as MineShaft)?.mineLevel ?? 0);
		int num = (int)tileLocation.X;
		int num2 = (int)tileLocation.Y;
		int level = -1;
		int num3 = 0;
		if (location is MineShaft mineShaft)
		{
			level = mineShaft.mineLevel;
			if (mineShaft.isContainerPlatform(num, num2))
			{
				mineShaft.updateMineLevelData(0, -1);
			}
			num3 = mineShaft.GetAdditionalDifficulty();
		}
		if (random.NextDouble() < 0.2)
		{
			if (random.NextDouble() < 0.1)
			{
				Game1.createMultipleItemDebris(Utility.getRaccoonSeedForCurrentTimeOfYear(who, random), new Vector2(num, num2) * 64f + new Vector2(32f), -1, location);
			}
			return;
		}
		if (location is MineShaft mineShaft2)
		{
			if (mineShaft2.mineLevel > 120 && !mineShaft2.isSideBranch())
			{
				int num4 = mineShaft2.mineLevel - 121;
				if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0)
				{
					float num5 = (float)(num4 + Game1.player.team.calicoEggSkullCavernRating.Value * 2) * 0.003f;
					if (num5 > 0.33f)
					{
						num5 = 0.33f;
					}
					if (random.NextBool(num5))
					{
						Game1.createMultipleObjectDebris("CalicoEgg", num, num2, random.Next(1, 4), who.UniqueMultiplayerID, location);
					}
				}
			}
			int num6 = mineShaft2.mineLevel;
			if (mineShaft2.mineLevel == 77377)
			{
				num6 = 5000;
			}
			Trinket.TrySpawnTrinket(location, null, new Vector2(num, num2) * 64f + new Vector2(32f), 1.0 + (double)num6 * 0.001);
		}
		if (random.NextDouble() <= 0.05 && Game1.player.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
		{
			Game1.createMultipleObjectDebris("(O)890", num, num2, random.Next(1, 3), who.UniqueMultiplayerID, location);
		}
		if (Utility.tryRollMysteryBox(0.0081 + Game1.player.team.AverageDailyLuck() / 15.0, random))
		{
			Game1.createItemDebris(ItemRegistry.Create((Game1.player.stats.Get(StatKeys.Mastery(2)) != 0) ? "(O)GoldenMysteryBox" : "(O)MysteryBox"), new Vector2(num, num2) * 64f + new Vector2(32f), -1, location);
		}
		Utility.trySpawnRareObject(who, new Vector2(num, num2) * 64f, location, 1.5, 1.0, -1, random);
		if (num3 > 0)
		{
			if (!(random.NextDouble() < 0.15))
			{
				if (random.NextDouble() < 0.008)
				{
					Game1.createMultipleObjectDebris("(O)858", num, num2, 1, location);
				}
				if (random.NextDouble() < 0.01)
				{
					Game1.createItemDebris(ItemRegistry.Create("(BC)71"), new Vector2(num, num2) * 64f + new Vector2(32f), 0);
				}
				if (random.NextDouble() < 0.01)
				{
					Game1.createMultipleObjectDebris(random.Choose("(O)918", "(O)919", "(O)920"), num, num2, 1, location);
				}
				if (random.NextDouble() < 0.01)
				{
					Game1.createMultipleObjectDebris("(O)386", num, num2, random.Next(1, 4), location);
				}
				switch (random.Next(17))
				{
				case 0:
					Game1.createMultipleObjectDebris("(O)382", num, num2, random.Next(1, 3), location);
					break;
				case 1:
					Game1.createMultipleObjectDebris("(O)380", num, num2, random.Next(1, 4), location);
					break;
				case 2:
					Game1.createMultipleObjectDebris("(O)62", num, num2, 1, location);
					break;
				case 3:
					Game1.createMultipleObjectDebris("(O)390", num, num2, random.Next(2, 6), location);
					break;
				case 4:
					Game1.createMultipleObjectDebris("(O)80", num, num2, random.Next(2, 3), location);
					break;
				case 5:
					Game1.createMultipleObjectDebris((who.timesReachedMineBottom > 0) ? "(O)84" : random.Choose("(O)92", "(O)370"), num, num2, random.Choose(2, 3), location);
					break;
				case 6:
					Game1.createMultipleObjectDebris("(O)70", num, num2, 1, location);
					break;
				case 7:
					Game1.createMultipleObjectDebris("(O)390", num, num2, random.Next(2, 6), location);
					break;
				case 8:
					Game1.createMultipleObjectDebris("(O)" + random.Next(218, 245), num, num2, 1, location);
					break;
				case 9:
					Game1.createMultipleObjectDebris((Game1.whichFarm == 6) ? "(O)920" : "(O)749", num, num2, 1, location);
					break;
				case 10:
					Game1.createMultipleObjectDebris("(O)286", num, num2, 1, location);
					break;
				case 11:
					Game1.createMultipleObjectDebris("(O)378", num, num2, random.Next(1, 4), location);
					break;
				case 12:
					Game1.createMultipleObjectDebris("(O)384", num, num2, random.Next(1, 4), location);
					break;
				case 13:
					Game1.createMultipleObjectDebris("(O)287", num, num2, 1, location);
					break;
				}
			}
			return;
		}
		switch (base.ItemId)
		{
		case "118":
			if (random.NextDouble() < 0.65)
			{
				if (random.NextDouble() < 0.8)
				{
					switch (random.Next(9))
					{
					case 0:
						Game1.createMultipleObjectDebris("(O)382", num, num2, random.Next(1, 3), location);
						break;
					case 1:
						Game1.createMultipleObjectDebris("(O)378", num, num2, random.Next(1, 4), location);
						break;
					case 3:
						Game1.createMultipleObjectDebris("(O)390", num, num2, random.Next(2, 6), location);
						break;
					case 4:
						Game1.createMultipleObjectDebris("(O)388", num, num2, random.Next(2, 3), location);
						break;
					case 5:
						Game1.createMultipleObjectDebris((who.timesReachedMineBottom > 0) ? "(O)80" : random.Choose("(O)92", "(O)370"), num, num2, random.Choose(2, 3), location);
						break;
					case 6:
						Game1.createMultipleObjectDebris("(O)388", num, num2, random.Next(2, 6), location);
						break;
					case 7:
						Game1.createMultipleObjectDebris("(O)390", num, num2, random.Next(2, 6), location);
						break;
					case 8:
						Game1.createMultipleObjectDebris("(O)770", num, num2, 1, location);
						break;
					case 2:
						break;
					}
				}
				else
				{
					switch (random.Next(4))
					{
					case 0:
						Game1.createMultipleObjectDebris("(O)78", num, num2, random.Next(1, 3), location);
						break;
					case 1:
						Game1.createMultipleObjectDebris("(O)78", num, num2, random.Next(1, 3), location);
						break;
					case 2:
						Game1.createMultipleObjectDebris("(O)78", num, num2, random.Next(1, 3), location);
						break;
					case 3:
						Game1.createMultipleObjectDebris("(O)535", num, num2, random.Next(1, 3), location);
						break;
					}
				}
			}
			else if (random.NextDouble() < 0.4)
			{
				switch (random.Next(5))
				{
				case 0:
					Game1.createMultipleObjectDebris("(O)66", num, num2, 1, location);
					break;
				case 1:
					Game1.createMultipleObjectDebris("(O)68", num, num2, 1, location);
					break;
				case 2:
					Game1.createMultipleObjectDebris("(O)709", num, num2, 1, location);
					break;
				case 3:
					Game1.createMultipleObjectDebris("(O)535", num, num2, 1, location);
					break;
				case 4:
					Game1.createItemDebris(MineShaft.getSpecialItemForThisMineLevel(level, num, num2), new Vector2(num, num2) * 64f + new Vector2(32f, 32f), random.Next(4), location);
					break;
				}
			}
			break;
		case "120":
			if (random.NextDouble() < 0.65)
			{
				if (random.NextDouble() < 0.8)
				{
					switch (random.Next(9))
					{
					case 0:
						Game1.createMultipleObjectDebris("(O)382", num, num2, random.Next(1, 3), location);
						break;
					case 1:
						Game1.createMultipleObjectDebris("(O)380", num, num2, random.Next(1, 4), location);
						break;
					case 3:
						Game1.createMultipleObjectDebris("(O)378", num, num2, random.Next(2, 6), location);
						break;
					case 4:
						Game1.createMultipleObjectDebris("(O)388", num, num2, random.Next(2, 6), location);
						break;
					case 5:
						Game1.createMultipleObjectDebris((who.timesReachedMineBottom > 0) ? "(O)84" : random.Choose("(O)92", "(O)371"), num, num2, random.Choose(2, 3), location);
						break;
					case 6:
						Game1.createMultipleObjectDebris("(O)390", num, num2, random.Next(2, 4), location);
						break;
					case 7:
						Game1.createMultipleObjectDebris("(O)390", num, num2, random.Next(2, 6), location);
						break;
					case 8:
						Game1.createMultipleObjectDebris("(O)770", num, num2, 1, location);
						break;
					case 2:
						break;
					}
				}
				else
				{
					switch (random.Next(4))
					{
					case 0:
						Game1.createMultipleObjectDebris("(O)78", num, num2, random.Next(1, 3), location);
						break;
					case 1:
						Game1.createMultipleObjectDebris("(O)536", num, num2, random.Next(1, 3), location);
						break;
					case 2:
						Game1.createMultipleObjectDebris("(O)78", num, num2, random.Next(1, 3), location);
						break;
					case 3:
						Game1.createMultipleObjectDebris("(O)78", num, num2, random.Next(1, 3), location);
						break;
					}
				}
			}
			else if (random.NextDouble() < 0.4)
			{
				switch (random.Next(5))
				{
				case 0:
					Game1.createMultipleObjectDebris("(O)62", num, num2, 1, location);
					break;
				case 1:
					Game1.createMultipleObjectDebris("(O)70", num, num2, 1, location);
					break;
				case 2:
					Game1.createMultipleObjectDebris("(O)709", num, num2, random.Next(1, 4), location);
					break;
				case 3:
					Game1.createMultipleObjectDebris("(O)536", num, num2, 1, location);
					break;
				case 4:
					Game1.createItemDebris(MineShaft.getSpecialItemForThisMineLevel(level, num, num2), new Vector2(num, num2) * 64f + new Vector2(32f, 32f), random.Next(4), location);
					break;
				}
			}
			break;
		case "124":
		case "122":
			if (random.NextDouble() < 0.65)
			{
				if (random.NextDouble() < 0.8)
				{
					switch (random.Next(8))
					{
					case 0:
						Game1.createMultipleObjectDebris("(O)382", num, num2, random.Next(1, 3), location);
						break;
					case 1:
						Game1.createMultipleObjectDebris("(O)384", num, num2, random.Next(1, 4), location);
						break;
					case 3:
						Game1.createMultipleObjectDebris("(O)380", num, num2, random.Next(2, 6), location);
						break;
					case 4:
						Game1.createMultipleObjectDebris("(O)378", num, num2, random.Next(2, 6), location);
						break;
					case 5:
						Game1.createMultipleObjectDebris("(O)390", num, num2, random.Next(2, 6), location);
						break;
					case 6:
						Game1.createMultipleObjectDebris("(O)388", num, num2, random.Next(2, 6), location);
						break;
					case 7:
						Game1.createMultipleObjectDebris("(O)881", num, num2, random.Next(2, 6), location);
						break;
					case 2:
						break;
					}
				}
				else
				{
					switch (random.Next(4))
					{
					case 0:
						Game1.createMultipleObjectDebris("(O)78", num, num2, random.Next(1, 3), location);
						break;
					case 1:
						Game1.createMultipleObjectDebris("(O)537", num, num2, random.Next(1, 3), location);
						break;
					case 2:
						Game1.createMultipleObjectDebris((who.timesReachedMineBottom > 0) ? "(O)82" : "(O)78", num, num2, random.Next(1, 3), location);
						break;
					case 3:
						Game1.createMultipleObjectDebris("(O)78", num, num2, random.Next(1, 3), location);
						break;
					}
				}
			}
			else if (random.NextDouble() < 0.4)
			{
				switch (random.Next(6))
				{
				case 0:
					Game1.createMultipleObjectDebris("(O)60", num, num2, 1, location);
					break;
				case 1:
					Game1.createMultipleObjectDebris("(O)64", num, num2, 1, location);
					break;
				case 2:
					Game1.createMultipleObjectDebris("(O)709", num, num2, random.Next(1, 4), location);
					break;
				case 3:
					Game1.createMultipleObjectDebris("(O)749", num, num2, 1, location);
					break;
				case 4:
					Game1.createItemDebris(MineShaft.getSpecialItemForThisMineLevel(level, num, num2), new Vector2(num, num2) * 64f + new Vector2(32f, 32f), random.Next(4), location);
					break;
				case 5:
					Game1.createMultipleObjectDebris("(O)688", num, num2, 1, location);
					break;
				}
			}
			break;
		case "174":
			if (random.NextDouble() < 0.1)
			{
				Game1.player.team.RequestLimitedNutDrops("VolcanoBarrel", location, num * 64, num2 * 64, 5);
			}
			if (location is VolcanoDungeon volcanoDungeon && volcanoDungeon.level.Value == 5 && num == 34)
			{
				Item item = ItemRegistry.Create("(O)851");
				item.Quality = 2;
				Game1.createItemDebris(item, new Vector2(num, num2) * 64f, 1);
			}
			else if (random.NextDouble() < 0.75)
			{
				if (random.NextDouble() < 0.8)
				{
					switch (random.Next(7))
					{
					case 0:
						Game1.createMultipleObjectDebris("(O)382", num, num2, random.Next(1, 3), location);
						break;
					case 1:
						Game1.createMultipleObjectDebris("(O)384", num, num2, random.Next(1, 4), location);
						break;
					case 2:
						location.characters.Add(new DwarvishSentry(new Vector2(num, num2) * 64f));
						break;
					case 3:
						Game1.createMultipleObjectDebris("(O)380", num, num2, random.Next(2, 6), location);
						break;
					case 4:
						Game1.createMultipleObjectDebris("(O)378", num, num2, random.Next(2, 6), location);
						break;
					case 5:
						Game1.createMultipleObjectDebris("66", num, num2, 1, location);
						break;
					case 6:
						Game1.createMultipleObjectDebris("(O)709", num, num2, random.Next(2, 6), location);
						break;
					}
				}
				else
				{
					switch (random.Next(5))
					{
					case 0:
						Game1.createMultipleObjectDebris("(O)78", num, num2, random.Next(1, 3), location);
						break;
					case 1:
						Game1.createMultipleObjectDebris("(O)749", num, num2, random.Next(1, 3), location);
						break;
					case 2:
						Game1.createMultipleObjectDebris("(O)60", num, num2, 1, location);
						break;
					case 3:
						Game1.createMultipleObjectDebris("(O)64", num, num2, 1, location);
						break;
					case 4:
						Game1.createMultipleObjectDebris("(O)68", num, num2, 1, location);
						break;
					}
				}
			}
			else if (random.NextDouble() < 0.4)
			{
				switch (random.Next(9))
				{
				case 0:
					Game1.createMultipleObjectDebris("(O)72", num, num2, 1, location);
					break;
				case 1:
					Game1.createMultipleObjectDebris("(O)831", num, num2, random.Next(1, 4), location);
					break;
				case 2:
					Game1.createMultipleObjectDebris("(O)833", num, num2, random.Next(1, 3), location);
					break;
				case 3:
					Game1.createMultipleObjectDebris("(O)749", num, num2, 1, location);
					break;
				case 4:
					Game1.createMultipleObjectDebris("(O)386", num, num2, 1, location);
					break;
				case 5:
					Game1.createMultipleObjectDebris("(O)848", num, num2, 1, location);
					break;
				case 6:
					Game1.createMultipleObjectDebris("(O)856", num, num2, 1, location);
					break;
				case 7:
					Game1.createMultipleObjectDebris("(O)886", num, num2, 1, location);
					break;
				case 8:
					Game1.createMultipleObjectDebris("(O)688", num, num2, 1, location);
					break;
				}
			}
			else
			{
				location.characters.Add(new DwarvishSentry(new Vector2(num, num2) * 64f));
			}
			break;
		}
	}

	public override void updateWhenCurrentLocation(GameTime time)
	{
		if (shakeTimer > 0)
		{
			shakeTimer -= time.ElapsedGameTime.Milliseconds;
		}
	}

	public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		Vector2 vector = getScale();
		vector *= 4f;
		Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 64));
		Rectangle destinationRectangle = new Rectangle((int)(vector2.X - vector.X / 2f), (int)(vector2.Y - vector.Y / 2f), (int)(64f + vector.X), (int)(128f + vector.Y / 2f));
		if (shakeTimer > 0)
		{
			int num = shakeTimer / 100 + 1;
			destinationRectangle.X += Game1.random.Next(-num, num + 1);
			destinationRectangle.Y += Game1.random.Next(-num, num + 1);
		}
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		spriteBatch.Draw(dataOrErrorItem.GetTexture(), destinationRectangle, dataOrErrorItem.GetSourceRect(showNextIndex.Value ? 1 : 0), Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, Math.Max(0f, (float)((y + 1) * 64 - 1) / 10000f));
	}
}
