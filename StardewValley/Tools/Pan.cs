using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Constants;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Objects;

namespace StardewValley.Tools;

public class Pan : Tool
{
	[XmlIgnore]
	private readonly NetEvent0 finishEvent = new NetEvent0();

	public Pan()
		: base("Copper Pan", 1, 12, 12, stackable: false)
	{
	}

	public Pan(int upgradeLevel)
		: base("Copper Pan", upgradeLevel, 12, 12, stackable: false)
	{
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		if (upgradeLevel.Value == -1)
		{
			base.UpgradeLevel = 1;
		}
		return new Pan(base.UpgradeLevel);
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(finishEvent, "finishEvent");
		finishEvent.onEvent += doFinish;
	}

	public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
	{
		if (upgradeLevel.Value <= 0)
		{
			base.UpgradeLevel = 1;
		}
		base.CurrentParentTileIndex = 12;
		base.IndexOfMenuItemView = 12;
		int num = 4;
		if (hasEnchantmentOfType<ReachingToolEnchantment>())
		{
			num++;
		}
		bool flag = false;
		Rectangle value = new Rectangle(location.orePanPoint.X * 64 - (int)(64f * ((float)num / 2f)), location.orePanPoint.Y * 64 - (int)(64f * ((float)num / 2f)), 64 * num, 64 * num);
		Point standingPixel = who.StandingPixel;
		if (value.Contains(x, y) && Utility.distance(standingPixel.X, value.Center.X, standingPixel.Y, value.Center.Y) <= (float)(num * 64))
		{
			flag = true;
		}
		who.lastClick = Vector2.Zero;
		x = (int)who.GetToolLocation().X;
		y = (int)who.GetToolLocation().Y;
		who.lastClick = new Vector2(x, y);
		if (location.orePanPoint != null && !location.orePanPoint.Equals(Point.Zero))
		{
			Rectangle boundingBox = who.GetBoundingBox();
			if (flag || boundingBox.Intersects(value))
			{
				who.faceDirection(2);
				who.FarmerSprite.animateOnce(303, 50f, 4);
				return true;
			}
		}
		who.forceCanMove();
		return true;
	}

	public static void playSlosh(Farmer who)
	{
		who.playNearbySoundLocal("slosh");
	}

	public override void tickUpdate(GameTime time, Farmer who)
	{
		lastUser = who;
		base.tickUpdate(time, who);
		finishEvent.Poll();
	}

	public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
	{
		base.DoFunction(location, x, y, power, who);
		Vector2 toolLocation = who.GetToolLocation();
		x = (int)toolLocation.X;
		y = (int)toolLocation.Y;
		base.CurrentParentTileIndex = 12;
		base.IndexOfMenuItemView = 12;
		location.localSound("coin", toolLocation / 64f);
		who.addItemsByMenuIfNecessary(getPanItems(location, who));
		location.orePanPoint.Value = Point.Zero;
		for (int i = 0; i < upgradeLevel.Value - 1; i++)
		{
			if (location.performOrePanTenMinuteUpdate(Game1.random))
			{
				break;
			}
			if (Game1.random.NextDouble() < 0.5 && location.performOrePanTenMinuteUpdate(Game1.random) && !(location is IslandNorth))
			{
				break;
			}
		}
		finish();
	}

	private void finish()
	{
		finishEvent.Fire();
	}

	private void doFinish()
	{
		lastUser.CanMove = true;
		lastUser.UsingTool = false;
		lastUser.canReleaseTool = true;
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		base.IndexOfMenuItemView = 12;
		base.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
	}

	public List<Item> getPanItems(GameLocation location, Farmer who)
	{
		List<Item> list = new List<Item>();
		string text = "378";
		string text2 = null;
		who.stats.Increment("TimesPanned", 1);
		Random random = Utility.CreateRandom(location.orePanPoint.X, (double)location.orePanPoint.Y * 1000.0, Game1.stats.DaysPlayed, who.stats.Get("TimesPanned") * 77);
		double num = random.NextDouble() - (double)who.luckLevel.Value * 0.001 - who.DailyLuck;
		num -= (double)(upgradeLevel.Value - 1) * 0.05;
		if (num < 0.01)
		{
			text = "386";
		}
		else if (num < 0.241)
		{
			text = "384";
		}
		else if (num < 0.6)
		{
			text = "380";
		}
		if (text != "386" && random.NextDouble() < 0.1 + (hasEnchantmentOfType<ArchaeologistEnchantment>() ? 0.1 : 0.0))
		{
			text = "881";
		}
		int num2 = random.Next(2, 7) + 1 + (int)((random.NextDouble() + 0.1 + (double)((float)who.luckLevel.Value / 10f) + who.DailyLuck) * 2.0);
		int num3 = random.Next(5) + 1 + (int)((random.NextDouble() + 0.1 + (double)((float)who.luckLevel.Value / 10f)) * 2.0);
		num2 += upgradeLevel.Value - 1;
		num = random.NextDouble() - who.DailyLuck;
		int num4 = upgradeLevel.Value;
		bool flag = false;
		double num5 = (double)(upgradeLevel.Value - 1) * 0.04;
		if (enchantments.Count > 0)
		{
			num5 *= 1.25;
		}
		if (hasEnchantmentOfType<GenerousEnchantment>())
		{
			num4 += 2;
		}
		while (random.NextDouble() - who.DailyLuck < 0.4 + (double)who.LuckLevel * 0.04 + num5 && num4 > 0)
		{
			num = random.NextDouble() - who.DailyLuck;
			num -= (double)(upgradeLevel.Value - 1) * 0.005;
			text2 = "382";
			if (num < 0.02 + (double)who.LuckLevel * 0.002 && random.NextDouble() < 0.75)
			{
				text2 = "72";
				num3 = 1;
			}
			else if (num < 0.1 && random.NextDouble() < 0.75)
			{
				text2 = (60 + random.Next(5) * 2).ToString();
				num3 = 1;
			}
			else if (num < 0.36)
			{
				text2 = "749";
				num3 = Math.Max(1, num3 / 2);
			}
			else if (num < 0.5)
			{
				text2 = random.Choose("82", "84", "86");
				num3 = 1;
			}
			if (num < (double)who.LuckLevel * 0.002 && !flag && random.NextDouble() < 0.33)
			{
				list.Add(new Ring("859"));
				flag = true;
			}
			if (num < 0.01 && random.NextDouble() < 0.5)
			{
				list.Add(Utility.getRandomCosmeticItem(random));
			}
			if (random.NextDouble() < 0.1 && hasEnchantmentOfType<FisherEnchantment>())
			{
				Item fish = location.getFish(1f, null, random.Next(1, 6), who, 0.0, who.Tile);
				if (fish != null && fish.Category == -4)
				{
					list.Add(fish);
				}
			}
			if (random.NextDouble() < 0.02 + (hasEnchantmentOfType<ArchaeologistEnchantment>() ? 0.05 : 0.0))
			{
				Item item = location.tryGetRandomArtifactFromThisLocation(who, random);
				if (item != null)
				{
					list.Add(item);
				}
			}
			if (Utility.tryRollMysteryBox(0.05, random))
			{
				list.Add(ItemRegistry.Create((Game1.player.stats.Get(StatKeys.Mastery(2)) != 0) ? "(O)GoldenMysteryBox" : "(O)MysteryBox"));
			}
			if (text2 != null)
			{
				list.Add(new Object(text2, num3));
			}
			num4--;
		}
		int num6 = 0;
		while (random.NextDouble() < 0.05 + (hasEnchantmentOfType<ArchaeologistEnchantment>() ? 0.15 : 0.0))
		{
			num6++;
		}
		if (num6 > 0)
		{
			list.Add(ItemRegistry.Create("(O)275", num6));
		}
		list.Add(new Object(text, num2));
		if (!(location is IslandNorth islandNorth))
		{
			if (location is IslandLocation && random.NextDouble() < 0.2)
			{
				list.Add(ItemRegistry.Create("(O)831", random.Next(2, 6)));
			}
		}
		else if (islandNorth.bridgeFixed.Value && random.NextDouble() < 0.2)
		{
			list.Add(ItemRegistry.Create("(O)822"));
		}
		if (who != null)
		{
			who.gainExperience(3, num2 + num3);
			who.gainExperience(2, list.Count * 7);
		}
		return list;
	}
}
