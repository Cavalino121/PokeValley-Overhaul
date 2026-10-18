using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;

namespace StardewValley;

/// <summary>Handles parsing and caching item context tags.</summary>
public static class ItemContextTagManager
{
	/// <summary>A cache of the base context tags by qualified item ID, excluding context tags added dynamically by the item instance.</summary>
	private static readonly Dictionary<string, HashSet<string>> BaseTagsCache = new Dictionary<string, HashSet<string>>();

	/// <summary>Get the base context tags for an item ID, excluding context tags added dynamically by the item instance.</summary>
	/// <param name="itemId">The qualified or unqualified item ID.</param>
	public static HashSet<string> GetBaseContextTags(string itemId)
	{
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(itemId);
		if (!BaseTagsCache.TryGetValue(dataOrErrorItem.QualifiedItemId, out var value))
		{
			IItemDataDefinition itemType = dataOrErrorItem.ItemType;
			value = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			string item = SanitizeContextTag("id_" + dataOrErrorItem.QualifiedItemId);
			value.Add(item);
			if (itemType.StandardDescriptor != null)
			{
				string item2 = SanitizeContextTag("id_" + dataOrErrorItem.ItemType.StandardDescriptor + "_" + dataOrErrorItem.ItemId);
				value.Add(item2);
			}
			switch (itemType.Identifier)
			{
			case "(BC)":
			{
				if (!(dataOrErrorItem.RawData is BigCraftableData bigCraftableData))
				{
					break;
				}
				List<string> contextTags2 = bigCraftableData.ContextTags;
				if (contextTags2 == null || contextTags2.Count <= 0)
				{
					break;
				}
				foreach (string contextTag in bigCraftableData.ContextTags)
				{
					value.Add(contextTag);
				}
				break;
			}
			case "(F)":
				if (dataOrErrorItem.RawData is string[] array2)
				{
					string value3 = ArgUtility.Get(array2, 11);
					value.AddRange(ArgUtility.SplitBySpace(value3));
				}
				break;
			case "(O)":
			{
				if (!(dataOrErrorItem.RawData is ObjectData objectData))
				{
					break;
				}
				List<string> contextTags = objectData.ContextTags;
				if (contextTags != null && contextTags.Count > 0)
				{
					foreach (string contextTag2 in objectData.ContextTags)
					{
						value.Add(contextTag2);
					}
				}
				if (!objectData.GeodeDropsDefaultItems)
				{
					List<ObjectGeodeDropData> geodeDrops = objectData.GeodeDrops;
					if (geodeDrops == null || geodeDrops.Count <= 0)
					{
						goto IL_0207;
					}
				}
				value.Add("geode");
				IL_0207:
				if (!objectData.CanBeGivenAsGift)
				{
					value.Add("not_giftable");
				}
				break;
			}
			case "(H)":
				{
					if (dataOrErrorItem.RawData is string[] array)
					{
						string value2 = ArgUtility.Get(array, 4);
						value.AddRange(ArgUtility.SplitBySpace(value2));
					}
					break;
				}
			}
			if (dataOrErrorItem.InternalName != null)
			{
				value.Add("item_" + SanitizeContextTag(dataOrErrorItem.InternalName));
			}
			if (dataOrErrorItem.ObjectType != null)
			{
				value.Add("item_type_" + SanitizeContextTag(dataOrErrorItem.ObjectType));
			}
			if (DataLoader.Machines(Game1.content).TryGetValue(dataOrErrorItem.QualifiedItemId, out var value4))
			{
				value.Add("is_machine");
				int num;
				if (!value4.HasOutput)
				{
					List<MachineOutputRule> outputRules = value4.OutputRules;
					num = ((outputRules != null && outputRules.Count > 0) ? 1 : 0);
				}
				else
				{
					num = 1;
				}
				bool flag = (byte)num != 0;
				bool flag2 = value4.HasInput;
				if (!flag2)
				{
					List<MachineOutputRule> outputRules2 = value4.OutputRules;
					if (outputRules2 != null && outputRules2.Count > 0)
					{
						foreach (MachineOutputRule outputRule in value4.OutputRules)
						{
							if (outputRule.Triggers == null)
							{
								continue;
							}
							foreach (MachineOutputTriggerRule trigger in outputRule.Triggers)
							{
								if (trigger.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
								{
									flag2 = true;
									break;
								}
							}
							if (flag2)
							{
								break;
							}
						}
					}
				}
				if (flag)
				{
					value.Add("machine_output");
				}
				if (flag2)
				{
					value.Add("machine_input");
				}
			}
			if (dataOrErrorItem.Category == -4 && DataLoader.Fish(Game1.content).TryGetValue(dataOrErrorItem.ItemId, out var value5))
			{
				string[] array3 = value5.Split('/');
				if (array3[1] == "trap")
				{
					value.Add("fish_trap_location_" + array3[4]);
				}
				else
				{
					value.Add("fish_motion_" + array3[2]);
					int num2 = Convert.ToInt32(array3[1]);
					if (num2 <= 33)
					{
						value.Add("fish_difficulty_easy");
					}
					else if (num2 <= 66)
					{
						value.Add("fish_difficulty_medium");
					}
					else if (num2 <= 100)
					{
						value.Add("fish_difficulty_hard");
					}
					else
					{
						value.Add("fish_difficulty_extremely_hard");
					}
					value.Add("fish_favor_weather_" + array3[7]);
				}
			}
			switch (dataOrErrorItem.Category)
			{
			case -26:
				value.Add("category_artisan_goods");
				break;
			case -21:
				value.Add("category_bait");
				break;
			case -9:
				value.Add("category_big_craftable");
				break;
			case -97:
				value.Add("category_boots");
				break;
			case -100:
				value.Add("category_clothing");
				break;
			case -7:
				value.Add("category_cooking");
				break;
			case -8:
				value.Add("category_crafting");
				break;
			case -5:
				value.Add("category_egg");
				break;
			case -29:
				value.Add("category_equipment");
				break;
			case -19:
				value.Add("category_fertilizer");
				break;
			case -4:
				value.Add("category_fish");
				break;
			case -80:
				value.Add("category_flowers");
				break;
			case -79:
				value.Add("category_fruits");
				break;
			case -24:
				value.Add("category_furniture");
				break;
			case -2:
				value.Add("category_gem");
				break;
			case -81:
				value.Add("category_greens");
				break;
			case -95:
				value.Add("category_hat");
				break;
			case -25:
				value.Add("category_ingredients");
				break;
			case -20:
				value.Add("category_junk");
				break;
			case -999:
				value.Add("category_litter");
				break;
			case -14:
				value.Add("category_meat");
				break;
			case -6:
				value.Add("category_milk");
				break;
			case -12:
				value.Add("category_minerals");
				break;
			case -28:
				value.Add("category_monster_loot");
				break;
			case -96:
				value.Add("category_ring");
				break;
			case -74:
				value.Add("category_seeds");
				break;
			case -23:
				value.Add("category_sell_at_fish_shop");
				break;
			case -27:
				value.Add("category_syrup");
				break;
			case -22:
				value.Add("category_tackle");
				break;
			case -99:
				value.Add("category_tool");
				break;
			case -75:
				value.Add("category_vegetable");
				break;
			case -98:
				value.Add("category_weapon");
				break;
			case -17:
				value.Add("category_sell_at_pierres");
				break;
			case -18:
				value.Add("category_sell_at_pierres_and_marnies");
				break;
			case -15:
				value.Add("category_metal_resources");
				break;
			case -16:
				value.Add("category_building_resources");
				break;
			case -101:
				value.Add("category_trinket");
				break;
			}
			BaseTagsCache[dataOrErrorItem.QualifiedItemId] = value;
		}
		return value;
	}

	/// <summary>Get whether an item has a given base context tag, excluding context tags added dynamically by the item instance.</summary>
	/// <param name="itemId">The qualified or unqualified item ID.</param>
	/// <param name="tag">The tag to match.</param>
	public static bool HasBaseTag(string itemId, string tag)
	{
		return GetBaseContextTags(itemId).Contains(tag);
	}

	/// <summary>Get whether a tag query string (containing one or more context tags) matches the given item tags.</summary>
	/// <param name="tagQueryString">The comma-delimited list of context tags. Each tag can be negated by prefixing with <c>!</c> (like <c>!wine_item</c> to check if the tags <em>don't</em> contain <c>wine_item</c>).</param>
	/// <param name="tags">The context tags for the item to check.</param>
	public static bool DoesTagQueryMatch(string tagQueryString, HashSet<string> tags)
	{
		return DoAllTagsMatch(tagQueryString?.Split(','), tags);
	}

	/// <summary>Get whether each tag matches the actual item tags.</summary>
	/// <param name="requiredTags">The tag values to match against the actual tag. Each tag can be negated by prefixing with <c>!</c> (like <c>!wine_item</c> to check if the tags <em>don't</em> contain <c>wine_item</c>).</param>
	/// <param name="actualTags">The actual tags for the item being checked.</param>
	public static bool DoAllTagsMatch(IList<string> requiredTags, HashSet<string> actualTags)
	{
		if (requiredTags == null || requiredTags.Count == 0)
		{
			return false;
		}
		foreach (string requiredTag in requiredTags)
		{
			if (!DoesTagMatch(requiredTag, actualTags))
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>Get whether any tag matches the actual item tags.</summary>
	/// <param name="requiredTags">The tag values to match against the actual tag. Each tag can be negated by prefixing with <c>!</c> (like <c>!wine_item</c> to check if the tags <em>don't</em> contain <c>wine_item</c>).</param>
	/// <param name="actualTags">The actual tags for the item being checked.</param>
	public static bool DoAnyTagsMatch(IList<string> requiredTags, HashSet<string> actualTags)
	{
		if (requiredTags != null && requiredTags.Count > 0)
		{
			foreach (string requiredTag in requiredTags)
			{
				if (requiredTag != null && requiredTag.Length > 0 && DoesTagMatch(requiredTag, actualTags))
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Get whether a single-tag query matches the given item tags.</summary>
	/// <param name="tag">The tag to match. This can be negated by prefixing with <c>!</c> (like <c>!wine_item</c> to check if the tags <em>don't</em> contain <c>wine_item</c>).</param>
	/// <param name="tags">The list of tags to search for a match to <paramref name="tag" />.</param>
	public static bool DoesTagMatch(string tag, HashSet<string> tags)
	{
		if (tag == null)
		{
			return false;
		}
		tag = tag.Trim();
		bool flag = true;
		if (tag.StartsWith('!'))
		{
			tag = tag.Substring(1).TrimStart();
			flag = false;
		}
		if (tag.Length > 0)
		{
			return tags.Contains(tag) == flag;
		}
		return false;
	}

	/// <summary>Get a tag value with invalid characters (like spaces) escaped.</summary>
	/// <param name="tag">The raw tag value to sanitize.</param>
	public static string SanitizeContextTag(string tag)
	{
		return tag.Trim().ToLower().Replace(' ', '_')
			.Replace("'", "");
	}

	/// <summary>Get the color of an item based on its <c>color_*</c> context tag, if any.</summary>
	/// <param name="item">The item whose context tags to check.</param>
	public static Color? GetColorFromTags(Item item)
	{
		foreach (string contextTag in item.GetContextTags())
		{
			if (contextTag.StartsWithIgnoreCase("color_"))
			{
				switch (contextTag.ToLowerInvariant())
				{
				case "color_black":
					return new Color(45, 45, 45);
				case "color_gray":
					return Color.Gray;
				case "color_white":
					return Color.White;
				case "color_pink":
					return new Color(255, 163, 186);
				case "color_red":
					return new Color(220, 0, 0);
				case "color_orange":
					return new Color(255, 128, 0);
				case "color_yellow":
					return new Color(255, 230, 0);
				case "color_green":
					return new Color(10, 143, 0);
				case "color_blue":
					return new Color(46, 85, 183);
				case "color_purple":
					return new Color(115, 41, 181);
				case "color_brown":
					return new Color(130, 73, 37);
				case "color_light_cyan":
					return new Color(180, 255, 255);
				case "color_cyan":
					return Color.Cyan;
				case "color_aquamarine":
					return Color.Aquamarine;
				case "color_sea_green":
					return Color.SeaGreen;
				case "color_lime":
					return Color.Lime;
				case "color_yellow_green":
					return Color.GreenYellow;
				case "color_pale_violet_red":
					return Color.PaleVioletRed;
				case "color_salmon":
					return new Color(255, 85, 95);
				case "color_jade":
					return new Color(130, 158, 93);
				case "color_sand":
					return Color.NavajoWhite;
				case "color_poppyseed":
					return new Color(82, 47, 153);
				case "color_dark_red":
					return Color.DarkRed;
				case "color_dark_orange":
					return Color.DarkOrange;
				case "color_dark_yellow":
					return Color.DarkGoldenrod;
				case "color_dark_green":
					return Color.DarkGreen;
				case "color_dark_blue":
					return Color.DarkBlue;
				case "color_dark_purple":
					return Color.DarkViolet;
				case "color_dark_pink":
					return Color.DeepPink;
				case "color_dark_cyan":
					return Color.DarkCyan;
				case "color_dark_gray":
					return Color.DarkGray;
				case "color_dark_brown":
					return Color.SaddleBrown;
				case "color_gold":
					return Color.Gold;
				case "color_copper":
					return new Color(179, 85, 0);
				case "color_iron":
					return new Color(197, 213, 224);
				case "color_iridium":
					return new Color(105, 15, 255);
				}
			}
		}
		return null;
	}

	/// <summary>Reset all cached item context tags.</summary>
	/// <remarks>This is called from <see cref="M:StardewValley.ItemRegistry.RebuildCache" /> and generally shouldn't be called directly by other code.</remarks>
	internal static void ResetCache()
	{
		BaseTagsCache.Clear();
	}
}
