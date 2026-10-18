using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData.Crops;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;

namespace StardewValley;

public class Stats
{
	/// <summary>The number of each monster type killed, prefixed by the monster's internal name.</summary>
	public StatsDictionary<int> specificMonstersKilled = new StatsDictionary<int>();

	/// <summary>The numeric metrics tracked by the game.</summary>
	/// <remarks>Most code should use methods like <see cref="M:StardewValley.Stats.Get(System.String)" /> or <see cref="M:StardewValley.Stats.Set(System.String,System.UInt32)" /> instead of calling this directly.</remarks>
	public StatsDictionary<uint> Values = new StatsDictionary<uint>();

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.Stats.Values" /> instead.</summary>
	[XmlElement("stat_dictionary")]
	public SerializableDictionary<string, uint> obsolete_stat_dictionary;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.AverageBedtime" /> instead.</summary>
	[XmlElement("averageBedtime")]
	public uint? obsolete_averageBedtime;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.BeveragesMade" /> instead.</summary>
	[XmlElement("beveragesMade")]
	public uint? obsolete_beveragesMade;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.CaveCarrotsFound" /> instead.</summary>
	[XmlElement("caveCarrotsFound")]
	public uint? obsolete_caveCarrotsFound;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.CheeseMade" /> instead.</summary>
	[XmlElement("cheeseMade")]
	public uint? obsolete_cheeseMade;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.ChickenEggsLayed" /> instead.</summary>
	[XmlElement("chickenEggsLayed")]
	public uint? obsolete_chickenEggsLayed;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.CopperFound" /> instead.</summary>
	[XmlElement("copperFound")]
	public uint? obsolete_copperFound;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.CowMilkProduced" /> instead.</summary>
	[XmlElement("cowMilkProduced")]
	public uint? obsolete_cowMilkProduced;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.CropsShipped" /> instead.</summary>
	[XmlElement("cropsShipped")]
	public uint? obsolete_cropsShipped;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.DaysPlayed" /> instead.</summary>
	[XmlElement("daysPlayed")]
	public uint? obsolete_daysPlayed;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.DiamondsFound" /> instead.</summary>
	[XmlElement("diamondsFound")]
	public uint? obsolete_diamondsFound;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.DirtHoed" /> instead.</summary>
	[XmlElement("dirtHoed")]
	public uint? obsolete_dirtHoed;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.DuckEggsLayed" /> instead.</summary>
	[XmlElement("duckEggsLayed")]
	public uint? obsolete_duckEggsLayed;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.FishCaught" /> instead.</summary>
	[XmlElement("fishCaught")]
	public uint? obsolete_fishCaught;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.GeodesCracked" /> instead.</summary>
	[XmlElement("geodesCracked")]
	public uint? obsolete_geodesCracked;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.GiftsGiven" /> instead.</summary>
	[XmlElement("giftsGiven")]
	public uint? obsolete_giftsGiven;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.GoatCheeseMade" /> instead.</summary>
	[XmlElement("goatCheeseMade")]
	public uint? obsolete_goatCheeseMade;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.GoatMilkProduced" /> instead.</summary>
	[XmlElement("goatMilkProduced")]
	public uint? obsolete_goatMilkProduced;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.GoldFound" /> instead.</summary>
	[XmlElement("goldFound")]
	public uint? obsolete_goldFound;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.GoodFriends" /> instead.</summary>
	[XmlElement("goodFriends")]
	public uint? obsolete_goodFriends;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.IndividualMoneyEarned" /> instead.</summary>
	[XmlElement("individualMoneyEarned")]
	public uint? obsolete_individualMoneyEarned;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.IridiumFound" /> instead.</summary>
	[XmlElement("iridiumFound")]
	public uint? obsolete_iridiumFound;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.IronFound" /> instead.</summary>
	[XmlElement("ironFound")]
	public uint? obsolete_ironFound;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.ItemsCooked" /> instead.</summary>
	[XmlElement("itemsCooked")]
	public uint? obsolete_itemsCooked;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.ItemsCrafted" /> instead.</summary>
	[XmlElement("itemsCrafted")]
	public uint? obsolete_itemsCrafted;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.ItemsForaged" /> instead.</summary>
	[XmlElement("itemsForaged")]
	public uint? obsolete_itemsForaged;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.ItemsShipped" /> instead.</summary>
	[XmlElement("itemsShipped")]
	public uint? obsolete_itemsShipped;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.MonstersKilled" /> instead.</summary>
	[XmlElement("monstersKilled")]
	public uint? obsolete_monstersKilled;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.MysticStonesCrushed" /> instead.</summary>
	[XmlElement("mysticStonesCrushed")]
	public uint? obsolete_mysticStonesCrushed;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.NotesFound" /> instead.</summary>
	[XmlElement("notesFound")]
	public uint? obsolete_notesFound;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.OtherPreciousGemsFound" /> instead.</summary>
	[XmlElement("otherPreciousGemsFound")]
	public uint? obsolete_otherPreciousGemsFound;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.PiecesOfTrashRecycled" /> instead.</summary>
	[XmlElement("piecesOfTrashRecycled")]
	public uint? obsolete_piecesOfTrashRecycled;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.PreservesMade" /> instead.</summary>
	[XmlElement("preservesMade")]
	public uint? obsolete_preservesMade;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.PrismaticShardsFound" /> instead.</summary>
	[XmlElement("prismaticShardsFound")]
	public uint? obsolete_prismaticShardsFound;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.QuestsCompleted" /> instead.</summary>
	[XmlElement("questsCompleted")]
	public uint? obsolete_questsCompleted;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.RabbitWoolProduced" /> instead.</summary>
	[XmlElement("rabbitWoolProduced")]
	public uint? obsolete_rabbitWoolProduced;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.RocksCrushed" /> instead.</summary>
	[XmlElement("rocksCrushed")]
	public uint? obsolete_rocksCrushed;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.SheepWoolProduced" /> instead.</summary>
	[XmlElement("sheepWoolProduced")]
	public uint? obsolete_sheepWoolProduced;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.SlimesKilled" /> instead.</summary>
	[XmlElement("slimesKilled")]
	public uint? obsolete_slimesKilled;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.StepsTaken" /> instead.</summary>
	[XmlElement("stepsTaken")]
	public uint? obsolete_stepsTaken;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.StoneGathered" /> instead.</summary>
	[XmlElement("stoneGathered")]
	public uint? obsolete_stoneGathered;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.StumpsChopped" /> instead.</summary>
	[XmlElement("stumpsChopped")]
	public uint? obsolete_stumpsChopped;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.TimesFished" /> instead.</summary>
	[XmlElement("timesFished")]
	public uint? obsolete_timesFished;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.TimesUnconscious" /> instead.</summary>
	[XmlElement("timesUnconscious")]
	public uint? obsolete_timesUnconscious;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.Constants.StatKeys.TotalMoneyGifted" /> instead.</summary>
	[XmlElement("totalMoneyGifted")]
	public uint? obsolete_totalMoneyGifted;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.TrufflesFound" /> instead.</summary>
	[XmlElement("trufflesFound")]
	public uint? obsolete_trufflesFound;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.WeedsEliminated" /> instead.</summary>
	[XmlElement("weedsEliminated")]
	public uint? obsolete_weedsEliminated;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="P:StardewValley.Stats.SeedsSown" /> instead.</summary>
	[XmlElement("seedsSown")]
	public uint? obsolete_seedsSown;

	/// <summary>Whether platform achievements can be unlocked retroactively overnight or when loading the save.</summary>
	/// <remarks>Certification requirements on some platforms prohibit us from unlocking trophies without the player doing something. On those platforms, we instead unlock missed achievements when the player performs a relevant action.</remarks>
	public static bool AllowRetroactiveAchievements => Program.sdk.RetroactiveAchievementsAllowed();

	[XmlIgnore]
	public uint AverageBedtime
	{
		get
		{
			return Get("averageBedtime");
		}
		set
		{
			uint num = Get("averageBedtime");
			uint num2 = Get("daysPlayed");
			Set("averageBedtime", (num * (num2 - 1) + value) / Math.Max(1u, num2));
		}
	}

	[XmlIgnore]
	public uint DaysPlayed
	{
		get
		{
			return Get("daysPlayed");
		}
		set
		{
			Set("daysPlayed", value);
		}
	}

	[XmlIgnore]
	public uint IndividualMoneyEarned
	{
		get
		{
			return Get("individualMoneyEarned");
		}
		set
		{
			uint num = Get("individualMoneyEarned");
			Set("individualMoneyEarned", value);
			if (num < 1000000 && value >= 1000000)
			{
				Game1.multiplayer.globalChatInfoMessage("SoloEarned1mil_" + (Game1.player.IsMale ? "Male" : "Female"), Game1.player.Name);
			}
			else if (num < 100000 && value >= 100000)
			{
				Game1.multiplayer.globalChatInfoMessage("SoloEarned100k_" + (Game1.player.IsMale ? "Male" : "Female"), Game1.player.Name);
			}
			else if (num < 10000 && value >= 10000)
			{
				Game1.multiplayer.globalChatInfoMessage("SoloEarned10k_" + (Game1.player.IsMale ? "Male" : "Female"), Game1.player.Name);
			}
			else if (num < 1000 && value >= 1000)
			{
				Game1.multiplayer.globalChatInfoMessage("SoloEarned1k_" + (Game1.player.IsMale ? "Male" : "Female"), Game1.player.Name);
			}
		}
	}

	[XmlIgnore]
	public uint ItemsCooked
	{
		get
		{
			return Get("itemsCooked");
		}
		set
		{
			Set("itemsCooked", value);
		}
	}

	[XmlIgnore]
	public uint ItemsCrafted
	{
		get
		{
			return Get("itemsCrafted");
		}
		set
		{
			Set("itemsCrafted", value);
			checkForCraftingAchievements();
		}
	}

	[XmlIgnore]
	public uint ItemsForaged
	{
		get
		{
			return Get("itemsForaged");
		}
		set
		{
			Set("itemsForaged", value);
		}
	}

	[XmlIgnore]
	public uint ItemsShipped
	{
		get
		{
			return Get("itemsShipped");
		}
		set
		{
			Set("itemsShipped", value);
		}
	}

	[XmlIgnore]
	public uint NotesFound
	{
		get
		{
			return Get("notesFound");
		}
		set
		{
			Set("notesFound", value);
		}
	}

	[XmlIgnore]
	public uint StepsTaken
	{
		get
		{
			return Get("stepsTaken");
		}
		set
		{
			Set("stepsTaken", value);
		}
	}

	[XmlIgnore]
	public uint StumpsChopped
	{
		get
		{
			return Get("stumpsChopped");
		}
		set
		{
			Set("stumpsChopped", value);
		}
	}

	[XmlIgnore]
	public uint TimesUnconscious
	{
		get
		{
			return Get("timesUnconscious");
		}
		set
		{
			Set("timesUnconscious", value);
		}
	}

	[XmlIgnore]
	public uint BeveragesMade
	{
		get
		{
			return Get("beveragesMade");
		}
		set
		{
			Set("beveragesMade", value);
		}
	}

	[XmlIgnore]
	public uint CheeseMade
	{
		get
		{
			return Get("cheeseMade");
		}
		set
		{
			Set("cheeseMade", value);
		}
	}

	[XmlIgnore]
	public uint ChickenEggsLayed
	{
		get
		{
			return Get("chickenEggsLayed");
		}
		set
		{
			Set("chickenEggsLayed", value);
		}
	}

	[XmlIgnore]
	public uint CowMilkProduced
	{
		get
		{
			return Get("cowMilkProduced");
		}
		set
		{
			Set("cowMilkProduced", value);
		}
	}

	[XmlIgnore]
	public uint CropsShipped
	{
		get
		{
			return Get("cropsShipped");
		}
		set
		{
			Set("cropsShipped", value);
		}
	}

	[XmlIgnore]
	public uint DirtHoed
	{
		get
		{
			return Get("dirtHoed");
		}
		set
		{
			Set("dirtHoed", value);
		}
	}

	[XmlIgnore]
	public uint DuckEggsLayed
	{
		get
		{
			return Get("duckEggsLayed");
		}
		set
		{
			Set("duckEggsLayed", value);
		}
	}

	[XmlIgnore]
	public uint GoatCheeseMade
	{
		get
		{
			return Get("goatCheeseMade");
		}
		set
		{
			Set("goatCheeseMade", value);
		}
	}

	[XmlIgnore]
	public uint GoatMilkProduced
	{
		get
		{
			return Get("goatMilkProduced");
		}
		set
		{
			Set("goatMilkProduced", value);
		}
	}

	[XmlIgnore]
	public uint PiecesOfTrashRecycled
	{
		get
		{
			return Get("piecesOfTrashRecycled");
		}
		set
		{
			Set("piecesOfTrashRecycled", value);
		}
	}

	[XmlIgnore]
	public uint PreservesMade
	{
		get
		{
			return Get("preservesMade");
		}
		set
		{
			Set("preservesMade", value);
		}
	}

	[XmlIgnore]
	public uint RabbitWoolProduced
	{
		get
		{
			return Get("rabbitWoolProduced");
		}
		set
		{
			Set("rabbitWoolProduced", value);
		}
	}

	[XmlIgnore]
	public uint SeedsSown
	{
		get
		{
			return Get("seedsSown");
		}
		set
		{
			Set("seedsSown", value);
		}
	}

	[XmlIgnore]
	public uint SheepWoolProduced
	{
		get
		{
			return Get("sheepWoolProduced");
		}
		set
		{
			Set("sheepWoolProduced", value);
		}
	}

	[XmlIgnore]
	public uint TrufflesFound
	{
		get
		{
			return Get("trufflesFound");
		}
		set
		{
			Set("trufflesFound", value);
		}
	}

	[XmlIgnore]
	public uint WeedsEliminated
	{
		get
		{
			return Get("weedsEliminated");
		}
		set
		{
			Set("weedsEliminated", value);
		}
	}

	[XmlIgnore]
	public uint MonstersKilled
	{
		get
		{
			return Get("monstersKilled");
		}
		set
		{
			Set("monstersKilled", value);
		}
	}

	[XmlIgnore]
	public uint SlimesKilled
	{
		get
		{
			return Get("slimesKilled");
		}
		set
		{
			Set("slimesKilled", value);
		}
	}

	[XmlIgnore]
	public uint FishCaught
	{
		get
		{
			return Get("fishCaught");
		}
		set
		{
			Set("fishCaught", value);
		}
	}

	[XmlIgnore]
	public uint TimesFished
	{
		get
		{
			return Get("timesFished");
		}
		set
		{
			Set("timesFished", value);
		}
	}

	[XmlIgnore]
	public uint CaveCarrotsFound
	{
		get
		{
			return Get("caveCarrotsFound");
		}
		set
		{
			Set("caveCarrotsFound", value);
		}
	}

	[XmlIgnore]
	public uint CopperFound
	{
		get
		{
			return Get("copperFound");
		}
		set
		{
			Set("copperFound", value);
		}
	}

	[XmlIgnore]
	public uint DiamondsFound
	{
		get
		{
			return Get("diamondsFound");
		}
		set
		{
			Set("diamondsFound", value);
		}
	}

	[XmlIgnore]
	public uint GeodesCracked
	{
		get
		{
			return Get("geodesCracked");
		}
		set
		{
			Set("geodesCracked", value);
		}
	}

	[XmlIgnore]
	public uint GoldFound
	{
		get
		{
			return Get("goldFound");
		}
		set
		{
			Set("goldFound", value);
		}
	}

	[XmlIgnore]
	public uint IridiumFound
	{
		get
		{
			return Get("iridiumFound");
		}
		set
		{
			Set("iridiumFound", value);
		}
	}

	[XmlIgnore]
	public uint IronFound
	{
		get
		{
			return Get("ironFound");
		}
		set
		{
			Set("ironFound", value);
		}
	}

	[XmlIgnore]
	public uint MysticStonesCrushed
	{
		get
		{
			return Get("mysticStonesCrushed");
		}
		set
		{
			Set("mysticStonesCrushed", value);
		}
	}

	[XmlIgnore]
	public uint OtherPreciousGemsFound
	{
		get
		{
			return Get("otherPreciousGemsFound");
		}
		set
		{
			Set("otherPreciousGemsFound", value);
		}
	}

	[XmlIgnore]
	public uint PrismaticShardsFound
	{
		get
		{
			return Get("prismaticShardsFound");
		}
		set
		{
			Set("prismaticShardsFound", value);
		}
	}

	[XmlIgnore]
	public uint RocksCrushed
	{
		get
		{
			return Get("rocksCrushed");
		}
		set
		{
			Set("rocksCrushed", value);
		}
	}

	[XmlIgnore]
	public uint StoneGathered
	{
		get
		{
			return Get("stoneGathered");
		}
		set
		{
			Set("stoneGathered", value);
		}
	}

	[XmlIgnore]
	public uint GiftsGiven
	{
		get
		{
			return Get("giftsGiven");
		}
		set
		{
			Set("giftsGiven", value);
		}
	}

	[XmlIgnore]
	public uint GoodFriends
	{
		get
		{
			return Get("goodFriends");
		}
		set
		{
			Set("goodFriends", value);
		}
	}

	[XmlIgnore]
	public uint QuestsCompleted
	{
		get
		{
			return Get("questsCompleted");
		}
		set
		{
			Set("questsCompleted", value);
			checkForQuestAchievements();
		}
	}

	/// <summary>Get the value of a tracked stat.</summary>
	/// <param name="key">The unique stat key, usually matching a <see cref="T:StardewValley.Constants.StatKeys" /> field.</param>
	public uint Get(string key)
	{
		if (!Values.TryGetValue(key, out var value))
		{
			return 0u;
		}
		return value;
	}

	/// <summary>Set the value of a tracked stat.</summary>
	/// <param name="key">The unique stat key, usually matching a <see cref="T:StardewValley.Constants.StatKeys" /> field.</param>
	/// <param name="value">The new value to set.</param>
	public void Set(string key, uint value)
	{
		if (value != 0)
		{
			Values[key] = value;
		}
		else
		{
			Values.Remove(key);
		}
	}

	/// <summary>Set the value of a tracked stat.</summary>
	/// <param name="key">The unique stat key, usually matching a <see cref="T:StardewValley.Constants.StatKeys" /> field.</param>
	/// <param name="value">The new value to set.</param>
	/// <remarks>The minimum stat value is zero. Setting a negative value is equivalent to setting zero.</remarks>
	public void Set(string key, int value)
	{
		if (value <= 0)
		{
			Set(key, 0u);
		}
		else
		{
			Set(key, (uint)value);
		}
	}

	/// <summary>Decrease the value of a tracked stat.</summary>
	/// <param name="key">The unique stat key, usually matching a <see cref="T:StardewValley.Constants.StatKeys" /> field.</param>
	/// <param name="amount">The amount by which to decrease the stat.</param>
	/// <remarks>The minimum stat value is zero. Decrementing past zero is equivalent to setting zero.</remarks>
	public uint Decrement(string key, uint amount = 1u)
	{
		uint num = Get(key);
		num = ((amount < num) ? (num - amount) : 0u);
		Set(key, num);
		return num;
	}

	/// <summary>Increase the value of a tracked stat.</summary>
	/// <param name="key">The unique stat key, usually matching a <see cref="T:StardewValley.Constants.StatKeys" /> field.</param>
	/// <param name="amount">The amount by which to increase the stat.</param>
	/// <returns>Returns the new stat value.</returns>
	public uint Increment(string key, uint amount = 1u)
	{
		uint num = Get(key) + amount;
		Set(key, num);
		return num;
	}

	/// <summary>Increase the value of a tracked stat.</summary>
	/// <param name="key">The unique stat key, usually matching a <see cref="T:StardewValley.Constants.StatKeys" /> field.</param>
	/// <param name="amount">The amount by which to increase the stat. If this is set to a negative value, the stat will be decremented instead (up to a minimum of zero).</param>
	/// <returns>Returns the new stat value.</returns>
	public uint Increment(string key, int amount)
	{
		if (amount >= 0)
		{
			return Increment(key, (uint)amount);
		}
		return Decrement(key, (uint)(-amount));
	}

	/// <summary>Update the stats when a monster is killed.</summary>
	/// <param name="name">The monster's internal name.</param>
	public void monsterKilled(string name)
	{
		if (AdventureGuild.willThisKillCompleteAMonsterSlayerQuest(name))
		{
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Stats.cs.5129"));
			Game1.multiplayer.globalChatInfoMessage("MonsterSlayer" + Game1.random.Next(4), Game1.player.Name, TokenStringBuilder.MonsterName(name));
		}
		specificMonstersKilled[name] = getMonstersKilled(name) + 1;
		checkForMonsterSlayerAchievement(isDirectUnlock: true);
	}

	/// <summary>Get the number of a given monster type that the player has killed.</summary>
	/// <param name="name">The monster's internal name.</param>
	public int getMonstersKilled(string name)
	{
		return specificMonstersKilled.GetValueOrDefault(name);
	}

	public void onMoneyGifted(uint amount)
	{
		uint num = Get("totalMoneyGifted");
		uint num2 = Increment("totalMoneyGifted", amount);
		if (num <= 1000000 && num2 > 1000000)
		{
			Game1.multiplayer.globalChatInfoMessage("Gifted1mil", Game1.player.Name);
		}
		else if (num <= 100000 && num2 > 100000)
		{
			Game1.multiplayer.globalChatInfoMessage("Gifted100k", Game1.player.Name);
		}
		else if (num <= 10000 && num2 > 10000)
		{
			Game1.multiplayer.globalChatInfoMessage("Gifted10k", Game1.player.Name);
		}
		else if (num <= 1000 && num2 > 1000)
		{
			Game1.multiplayer.globalChatInfoMessage("Gifted1k", Game1.player.Name);
		}
	}

	public void takeStep()
	{
		switch (Increment("stepsTaken"))
		{
		case 10000u:
			Game1.multiplayer.globalChatInfoMessage("Walked10k", Game1.player.Name);
			break;
		case 100000u:
			Game1.multiplayer.globalChatInfoMessage("Walked100k", Game1.player.Name);
			break;
		case 1000000u:
			Game1.multiplayer.globalChatInfoMessage("Walked1m", Game1.player.Name);
			break;
		case 10000000u:
			Game1.multiplayer.globalChatInfoMessage("Walked10m", Game1.player.Name);
			break;
		}
	}

	/// <summary>Unlock the 'Well Read' achievement if its criteria has been met.</summary>
	public void checkForBooksReadAchievement()
	{
		if (Game1.player.stats.Get("Book_Trash") != 0 && Game1.player.stats.Get("Book_Crabbing") != 0 && Game1.player.stats.Get("Book_Bombs") != 0 && Game1.player.stats.Get("Book_Roe") != 0 && Game1.player.stats.Get("Book_WildSeeds") != 0 && Game1.player.stats.Get("Book_Woodcutting") != 0 && Game1.player.stats.Get("Book_Defense") != 0 && Game1.player.stats.Get("Book_Friendship") != 0 && Game1.player.stats.Get("Book_Void") != 0 && Game1.player.stats.Get("Book_Speed") != 0 && Game1.player.stats.Get("Book_Marlon") != 0 && Game1.player.stats.Get("Book_PriceCatalogue") != 0 && Game1.player.stats.Get("Book_Diamonds") != 0 && Game1.player.stats.Get("Book_Mystery") != 0 && Game1.player.stats.Get("Book_AnimalCatalogue") != 0 && Game1.player.stats.Get("Book_Speed2") != 0 && Game1.player.stats.Get("Book_Artifact") != 0 && Game1.player.stats.Get("Book_Horse") != 0 && Game1.player.stats.Get("Book_Grass") != 0)
		{
			Game1.getAchievement(35);
		}
	}

	/// <summary>Unlock the cooking-related achievements if their criteria have been met.</summary>
	public void checkForCookingAchievements()
	{
		Dictionary<string, string> cookingRecipes = CraftingRecipe.cookingRecipes;
		int num = 0;
		int num2 = 0;
		foreach (KeyValuePair<string, string> item in cookingRecipes)
		{
			if (Game1.player.cookingRecipes.ContainsKey(item.Key))
			{
				string key = ArgUtility.SplitBySpaceAndGet(item.Value.Split('/')[2], 0);
				if (Game1.player.recipesCooked.TryGetValue(key, out var value))
				{
					num2 += value;
					num++;
				}
			}
		}
		Set("itemsCooked", num2);
		if (num >= cookingRecipes.Count)
		{
			Game1.getAchievement(17);
		}
		if (num >= 25)
		{
			Game1.getAchievement(16);
		}
		if (num >= 10)
		{
			Game1.getAchievement(15);
		}
	}

	/// <summary>Unlock the crafting-related achievements if their criteria have been met.</summary>
	public void checkForCraftingAchievements()
	{
		Dictionary<string, string> craftingRecipes = CraftingRecipe.craftingRecipes;
		int num = 0;
		int num2 = 0;
		foreach (string key in craftingRecipes.Keys)
		{
			if (!(key == "Wedding Ring") && Game1.player.craftingRecipes.TryGetValue(key, out var value))
			{
				num2 += value;
				if (Game1.player.craftingRecipes[key] > 0)
				{
					num++;
				}
			}
		}
		Set("itemsCrafted", num2);
		if (num >= craftingRecipes.Count - 1)
		{
			Game1.getAchievement(22);
		}
		if (num >= 30)
		{
			Game1.getAchievement(21);
		}
		if (num >= 15)
		{
			Game1.getAchievement(20);
		}
	}

	/// <summary>Unlock the shipping-related achievements if their criteria have been met.</summary>
	public void checkForShippingAchievements()
	{
		bool flag = true;
		bool flag2 = false;
		foreach (CropData value in Game1.cropData.Values)
		{
			if (value.CountForPolyculture)
			{
				flag = flag && DidFarmerShip(value.HarvestItemId, 15);
			}
			if (value.CountForMonoculture)
			{
				flag2 = flag2 || DidFarmerShip(value.HarvestItemId, 300);
			}
		}
		if (flag)
		{
			Game1.getAchievement(31);
		}
		if (flag2)
		{
			Game1.getAchievement(32);
		}
		if (Utility.hasFarmerShippedAllItems())
		{
			Game1.getAchievement(34);
		}
		static bool DidFarmerShip(string itemId, int number)
		{
			return Game1.player.basicShipped.GetValueOrDefault(itemId, 0) >= number;
		}
	}

	/// <summary>Unlock the fishing-related achievements if their criteria have been met.</summary>
	public void checkForFishingAchievements()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
		{
			if (allDatum.ObjectType == "Fish" && !(allDatum.RawData is ObjectData { ExcludeFromFishingCollection: not false }))
			{
				num3++;
				if (Game1.player.fishCaught.TryGetValue(allDatum.QualifiedItemId, out var value))
				{
					num += value[0];
					num2++;
				}
			}
		}
		Set("fishCaught", num);
		if (num >= 100)
		{
			Game1.getAchievement(27);
		}
		if (num2 >= num3)
		{
			Game1.getAchievement(26);
			if (!Game1.player.hasOrWillReceiveMail("CF_Fish"))
			{
				Game1.addMailForTomorrow("CF_Fish");
			}
		}
		if (num2 >= 24)
		{
			Game1.getAchievement(25);
		}
		if (num2 >= 10)
		{
			Game1.getAchievement(24);
		}
	}

	/// <summary>Unlock the artifact donation-related achievements if their criteria have been met.</summary>
	public void checkForArchaeologyAchievements()
	{
		int length = Game1.netWorldState.Value.MuseumPieces.Length;
		if (length >= LibraryMuseum.totalArtifacts)
		{
			Game1.getAchievement(5);
		}
		if (length >= 40)
		{
			Game1.getAchievement(28);
		}
	}

	/// <summary>Unlock achievements related to the items held by the player.</summary>
	public void checkForHeldItemAchievements()
	{
		if (Game1.player.Items.ContainsId("(W)62") || Game1.player.Items.ContainsId("(W)63") || Game1.player.Items.ContainsId("(W)64"))
		{
			Game1.getAchievement(42);
		}
	}

	/// <summary>Unlock the money-related achievements if their criteria have been met.</summary>
	public void checkForMoneyAchievements()
	{
		if (Game1.player.totalMoneyEarned >= 10000000)
		{
			Game1.getAchievement(4);
		}
		if (Game1.player.totalMoneyEarned >= 1000000)
		{
			Game1.getAchievement(3);
		}
		if (Game1.player.totalMoneyEarned >= 250000)
		{
			Game1.getAchievement(2);
		}
		if (Game1.player.totalMoneyEarned >= 50000)
		{
			Game1.getAchievement(1);
		}
		if (Game1.player.totalMoneyEarned >= 15000)
		{
			Game1.getAchievement(0);
		}
	}

	/// <summary>Unlock the farmhouse upgrade-related achievements if their criteria have been met.</summary>
	public void checkForBuildingUpgradeAchievements()
	{
		if (Game1.player.HouseUpgradeLevel >= 2)
		{
			Game1.getAchievement(19);
		}
		if (Game1.player.HouseUpgradeLevel >= 1)
		{
			Game1.getAchievement(18);
		}
	}

	/// <summary>Unlock the quest-related achievements if their criteria have been met.</summary>
	public void checkForQuestAchievements()
	{
		if (QuestsCompleted >= 40)
		{
			Game1.getAchievement(30);
			Game1.addMailForTomorrow("quest35");
		}
		if (QuestsCompleted >= 10)
		{
			Game1.getAchievement(29);
			Game1.addMailForTomorrow("quest10");
		}
	}

	/// <summary>Unlock the friendship-related achievements if their criteria have been met.</summary>
	public void checkForFriendshipAchievements()
	{
		uint num = 0u;
		uint num2 = 0u;
		uint num3 = 0u;
		foreach (Friendship value3 in Game1.player.friendshipData.Values)
		{
			if (value3.Points >= 2500)
			{
				num3++;
			}
			if (value3.Points >= 2000)
			{
				num2++;
			}
			if (value3.Points >= 1250)
			{
				num++;
			}
		}
		GoodFriends = num2;
		if (num >= 20)
		{
			Game1.getAchievement(13);
		}
		if (num >= 10)
		{
			Game1.getAchievement(12);
		}
		if (num >= 4)
		{
			Game1.getAchievement(11);
		}
		if (num >= 1)
		{
			Game1.getAchievement(6);
		}
		if (num3 >= 8)
		{
			Game1.getAchievement(9);
		}
		if (num3 >= 1)
		{
			Game1.getAchievement(7);
		}
		foreach (KeyValuePair<string, string> cookingRecipe in CraftingRecipe.cookingRecipes)
		{
			string key = cookingRecipe.Key;
			string[] array = ArgUtility.SplitBySpace(ArgUtility.Get(cookingRecipe.Value.Split('/'), 3));
			if (!(ArgUtility.Get(array, 0) != "f"))
			{
				string text = ArgUtility.Get(array, 1);
				int num4 = ArgUtility.GetInt(array, 2);
				if (text != null && Game1.player.friendshipData.TryGetValue(text, out var value) && value.Points >= num4 * 250 && !Game1.player.cookingRecipes.ContainsKey(key) && !Game1.player.hasOrWillReceiveMail(text + "Cooking"))
				{
					Game1.addMailForTomorrow(text + "Cooking");
				}
			}
		}
		foreach (KeyValuePair<string, string> craftingRecipe in CraftingRecipe.craftingRecipes)
		{
			string key2 = craftingRecipe.Key;
			string[] array2 = ArgUtility.SplitBySpace(ArgUtility.Get(craftingRecipe.Value.Split('/'), 4));
			if (!(ArgUtility.Get(array2, 0) != "f"))
			{
				string text2 = ArgUtility.Get(array2, 1);
				int num5 = ArgUtility.GetInt(array2, 2);
				if (text2 != null && Game1.player.friendshipData.TryGetValue(text2, out var value2) && value2.Points >= num5 * 250 && !Game1.player.craftingRecipes.ContainsKey(key2) && !Game1.player.hasOrWillReceiveMail(text2 + "Crafting"))
				{
					Game1.addMailForTomorrow(text2 + "Crafting");
				}
			}
		}
	}

	/// <summary>Unlock the achievements for completing the community center or Joja path if their criteria have been met.</summary>
	/// <param name="isDirectUnlock">Whether we're unlocking the achievements at the point where they normally trigger (i.e. not retroactively).</param>
	public void checkForCommunityCenterOrJojaAchievements(bool isDirectUnlock)
	{
		if (CanUnlockPlatformAchievements(isDirectUnlock))
		{
			if (Game1.player.eventsSeen.Contains("191393"))
			{
				Game1.getSteamAchievement("Achievement_LocalLegend");
			}
			if (Game1.player.eventsSeen.Contains("502261"))
			{
				Game1.getSteamAchievement("Achievement_Joja");
			}
		}
	}

	/// <summary>Unlock the mini-game-related achievements if their criteria have been met.</summary>
	/// <param name="isDirectUnlock">Whether we're unlocking the achievements at the point where they normally trigger (i.e. not retroactively).</param>
	public void checkForMiniGameAchievements(bool isDirectUnlock)
	{
		if (CanUnlockPlatformAchievements(isDirectUnlock))
		{
			if (Game1.player.stats.Get("completedPrairieKing") != 0)
			{
				Game1.getSteamAchievement("Achievement_PrairieKing");
			}
			if (Game1.player.stats.Get("completedPrairieKingWithoutDying") != 0)
			{
				Game1.getSteamAchievement("Achievement_FectorsChallenge");
			}
		}
	}

	/// <summary>Unlock the 'Full House' achievement if the player is married with two children.</summary>
	/// <param name="isDirectUnlock">Whether we're unlocking the achievements at the point where they normally trigger (i.e. not retroactively).</param>
	public void checkForFullHouseAchievement(bool isDirectUnlock)
	{
		if (CanUnlockPlatformAchievements(isDirectUnlock) && Game1.player.isMarriedOrRoommates() && Game1.player.getChildrenCount() >= 2)
		{
			Game1.getSteamAchievement("Achievement_FullHouse");
		}
	}

	/// <summary>Unlock the 'The Bottom' achievement if the player has reached the bottom of the mines.</summary>
	/// <param name="isDirectUnlock">Whether we're unlocking the achievements at the point where they normally trigger (i.e. not retroactively).</param>
	/// <param name="assumeDeepestLevel">Unlock the achievement regardless of the <see cref="P:StardewValley.Farmer.deepestMineLevel" />.</param>
	public void checkForMineAchievement(bool isDirectUnlock, bool assumeDeepestLevel = false)
	{
		if (CanUnlockPlatformAchievements(isDirectUnlock) && (assumeDeepestLevel || Game1.player.deepestMineLevel >= 120))
		{
			Game1.getSteamAchievement("Achievement_TheBottom");
		}
	}

	/// <summary>Unlock the 'Protector of the Valley' achievement if the player has completed all monster slayer goals.</summary>
	/// <param name="isDirectUnlock">Whether we're unlocking the achievements at the point where they normally trigger (i.e. not retroactively).</param>
	public void checkForMonsterSlayerAchievement(bool isDirectUnlock)
	{
		if (CanUnlockPlatformAchievements(isDirectUnlock) && AdventureGuild.areAllMonsterSlayerQuestsComplete())
		{
			Game1.player.hasCompletedAllMonsterSlayerQuests.Value = true;
			Game1.getSteamAchievement("Achievement_KeeperOfTheMysticRings");
		}
	}

	/// <summary>Unlock the skill-related achievements if their criteria have been met.</summary>
	/// <param name="isDirectUnlock">Whether we're unlocking the achievements at the point where they normally trigger (i.e. not retroactively).</param>
	public void checkForSkillAchievements(bool isDirectUnlock)
	{
		if (!CanUnlockPlatformAchievements(isDirectUnlock))
		{
			return;
		}
		NetInt[] obj = new NetInt[5]
		{
			Game1.player.farmingLevel,
			Game1.player.miningLevel,
			Game1.player.fishingLevel,
			Game1.player.foragingLevel,
			Game1.player.combatLevel
		};
		bool flag = false;
		bool flag2 = true;
		NetInt[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Value >= 10)
			{
				flag = true;
			}
			else
			{
				flag2 = false;
			}
		}
		if (flag)
		{
			Game1.getSteamAchievement("Achievement_SingularTalent");
			if (flag2)
			{
				Game1.getSteamAchievement("Achievement_MasterOfTheFiveWays");
			}
		}
	}

	/// <summary>Unlock the 'Mystery Of The Stardrops' achievement if the player has found all stardrops.</summary>
	/// <param name="isDirectUnlock">Whether we're unlocking the achievements at the point where they normally trigger (i.e. not retroactively).</param>
	public void checkForStardropAchievement(bool isDirectUnlock)
	{
		if (CanUnlockPlatformAchievements(isDirectUnlock) && Utility.foundAllStardrops())
		{
			Game1.getSteamAchievement("Achievement_Stardrop");
		}
	}

	public bool isSharedAchievement(int which)
	{
		if ((uint)which <= 5u || which == 28)
		{
			return true;
		}
		return false;
	}

	/// <summary>Unlock all achievements whose criteria have been met.</summary>
	public void checkForAchievements()
	{
		checkForBooksReadAchievement();
		checkForCookingAchievements();
		checkForCraftingAchievements();
		checkForShippingAchievements();
		checkForFishingAchievements();
		checkForArchaeologyAchievements();
		checkForHeldItemAchievements();
		checkForMoneyAchievements();
		checkForBuildingUpgradeAchievements();
		checkForQuestAchievements();
		checkForFriendshipAchievements();
		checkForCommunityCenterOrJojaAchievements(isDirectUnlock: false);
		checkForMiniGameAchievements(isDirectUnlock: false);
		checkForFullHouseAchievement(isDirectUnlock: false);
		checkForMineAchievement(isDirectUnlock: false);
		checkForMonsterSlayerAchievement(isDirectUnlock: false);
		checkForSkillAchievements(isDirectUnlock: false);
		checkForStardropAchievement(isDirectUnlock: false);
	}

	/// <summary>Get whether platform achievements can be unlocked now based on platform restrictions.</summary>
	/// <param name="isDirectUnlock">Whether we're unlocking the achievements at the point where they normally trigger (i.e. not retroactively).</param>
	/// <remarks>See remarks on <see cref="P:StardewValley.Stats.AllowRetroactiveAchievements" />.</remarks>
	public bool CanUnlockPlatformAchievements(bool isDirectUnlock)
	{
		return AllowRetroactiveAchievements || isDirectUnlock;
	}
}
