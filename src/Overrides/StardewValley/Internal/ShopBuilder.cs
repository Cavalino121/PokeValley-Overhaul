using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Extensions;
using StardewValley.GameData.Shops;
using StardewValley.GameData.Tools;

namespace StardewValley.Internal;

/// <summary>Handles building a shop menu from data in <c>Data/Shops</c>.</summary>
/// <remarks>This is an internal implementation class. Most code should use <see cref="M:StardewValley.Utility.TryOpenShopMenu(System.String,System.String,System.Boolean)" /> instead.</remarks>
public static class ShopBuilder
{
	/// <summary>Get the inventory to sell for a shop menu.</summary>
	/// <param name="shopId">The shop ID matching the entry in <c>Data/Shops</c>.</param>
	public static Dictionary<ISalable, ItemStockInformation> GetShopStock(string shopId)
	{
		if (DataLoader.Shops(Game1.content).TryGetValue(shopId, out var value))
		{
			return GetShopStock(shopId, value);
		}
		return new Dictionary<ISalable, ItemStockInformation>();
	}

	/// <summary>Get the inventory to sell for a shop menu.</summary>
	/// <param name="shopId">The shop ID in <c>Data\Shops</c>.</param>
	/// <param name="shop">The shop data from <c>Data\Shops</c>.</param>
	public static Dictionary<ISalable, ItemStockInformation> GetShopStock(string shopId, ShopData shop)
	{
		Dictionary<ISalable, ItemStockInformation> dictionary = new Dictionary<ISalable, ItemStockInformation>();
		List<ShopItemData> items = shop.Items;
		if (items != null && items.Count > 0)
		{
			Random random = Utility.CreateDaySaveRandom();
			HashSet<string> hashSet = new HashSet<string>();
			ItemQueryContext context = new ItemQueryContext(Game1.currentLocation, Game1.player, random, "shop '" + shopId + "'");
			bool applyPierreMissingStockList = shopId == "SeedShop" && Game1.MasterPlayer.hasOrWillReceiveMail("PierreStocklist");
			HashSet<string> hashSet2 = new HashSet<string>();
			foreach (ShopItemData item2 in shop.Items)
			{
				if (!hashSet2.Add(item2.Id))
				{
					Game1.log.Warn($"Shop {shopId} has multiple items with entry ID '{item2.Id}'. This may cause unintended behavior.");
				}
				if (!CheckItemCondition(item2.Condition, applyPierreMissingStockList, out var isOutOfSeason))
				{
					continue;
				}
				IList<ItemQueryResult> list = ItemQueryResolver.TryResolve(item2, context, ItemQuerySearchMode.All, item2.AvoidRepeat, item2.AvoidRepeat ? hashSet : null, null, delegate(string query, string message)
				{
					Game1.log.Error($"Failed parsing shop item query '{query}' for the '{shopId}' shop: {message}.");
				});
				int num = 0;
				foreach (ItemQueryResult item3 in list)
				{
					ISalable item = item3.Item;
					item.Stack = item3.OverrideStackSize ?? item.Stack;
					float value = GetBasePrice(item3, shop, item2, item, isOutOfSeason, item2.UseObjectDataPrice);
					int num2 = item3.OverrideShopAvailableStock ?? item2.AvailableStock;
					LimitedStockMode limitedStockMode = item2.AvailableStockLimit;
					string text = item3.OverrideTradeItemId ?? item2.TradeItemId;
					int? num3 = ((item3.OverrideTradeItemAmount > 0) ? item3.OverrideTradeItemAmount : new int?(item2.TradeItemAmount));
					if (text == null || num3 < 0)
					{
						text = null;
						num3 = null;
					}
					if (item2.IsRecipe)
					{
						item.Stack = 1;
						limitedStockMode = LimitedStockMode.None;
						num2 = 1;
					}
					if (!item2.IgnoreShopPriceModifiers)
					{
						value = Utility.ApplyQuantityModifiers(value, shop.PriceModifiers, shop.PriceModifierMode, null, null, item as Item, null, random);
					}
					value = Utility.ApplyQuantityModifiers(value, item2.PriceModifiers, item2.PriceModifierMode, null, null, item as Item, null, random);
					if (!item2.IsRecipe)
					{
						num2 = (int)Utility.ApplyQuantityModifiers(num2, item2.AvailableStockModifiers, item2.AvailableStockModifierMode, null, null, item as Item, null, random);
					}
					if (!TrackSeenItems(hashSet, item) || !item2.AvoidRepeat)
					{
						if (num2 < 0)
						{
							num2 = int.MaxValue;
						}
						string text2 = item2.Id;
						if (++num > 1)
						{
							text2 += num;
						}
						int price = (int)value;
						int stock = num2;
						string tradeItem = text;
						int? tradeItemCount = num3;
						LimitedStockMode stockMode = limitedStockMode;
						string syncedKey = text2;
						Item syncStacksWith = item3.SyncStacksWith;
						List<string> actionsOnPurchase = item2.ActionsOnPurchase;
						dictionary.Add(item, new ItemStockInformation(price, stock, tradeItem, tradeItemCount, stockMode, syncedKey, syncStacksWith, null, actionsOnPurchase));
					}
				}
			}
		}
		Game1.player.team.synchronizedShopStock.UpdateLocalStockWithSyncedQuanitities(shopId, dictionary);

		if (shopId == "SeedShop")
		{
			StardewValley.Pokemon.PokemonItemManager.AddPierreStock(dictionary);
		}
		else if (shopId == "Joja")
		{
			StardewValley.Pokemon.PokemonItemManager.AddJojaStock(dictionary);
		}

		return dictionary;
	}

	/// <summary>Check a game state query which determines whether an item should be added to a shop menu.</summary>
	/// <param name="conditions">The conditions to check.</param>
	/// <param name="applyPierreMissingStockList">Whether to apply Pierre's Missing Stock List, which allows buying out-of-season crops.</param>
	/// <param name="isOutOfSeason">Whether this is an out-of-season item which is allowed (for a price) because the player found Pierre's Stock List.</param>
	public static bool CheckItemCondition(string conditions, bool applyPierreMissingStockList, out bool isOutOfSeason)
	{
		if (conditions == null || GameStateQuery.CheckConditions(conditions))
		{
			isOutOfSeason = false;
			return true;
		}
		if (applyPierreMissingStockList && GameStateQuery.CheckConditions(conditions, null, null, null, null, null, GameStateQuery.SeasonQueryKeys))
		{
			isOutOfSeason = true;
			return true;
		}
		isOutOfSeason = false;
		return false;
	}

	/// <summary>Get the tool upgrade data to show in the blacksmith shop for a given tool, if any.</summary>
	/// <param name="tool">The tool data to show as an upgrade, if possible.</param>
	/// <param name="player">The player viewing the shop.</param>
	public static ToolUpgradeData GetToolUpgradeData(ToolData tool, Farmer player)
	{
		if (tool == null)
		{
			return null;
		}
		IList<ToolUpgradeData> list = tool.UpgradeFrom;
		if (tool.ConventionalUpgradeFrom != null)
		{
			IList<ToolUpgradeData> list2 = new ToolUpgradeData[1]
			{
				new ToolUpgradeData
				{
					RequireToolId = tool.ConventionalUpgradeFrom,
					Price = GetToolUpgradeConventionalPrice(tool.UpgradeLevel),
					TradeItemId = GetToolUpgradeConventionalTradeItem(tool.UpgradeLevel),
					TradeItemAmount = 5
				}
			};
			IList<ToolUpgradeData> list3;
			if (list == null || list.Count <= 0)
			{
				list3 = list2;
			}
			else
			{
				IList<ToolUpgradeData> list4 = list2.Concat(list).ToList();
				list3 = list4;
			}
			list = list3;
		}
		if (list == null)
		{
			return null;
		}
		foreach (ToolUpgradeData item in list)
		{
			if ((item.Condition == null || GameStateQuery.CheckConditions(item.Condition, player.currentLocation, player)) && (item.RequireToolId == null || player.Items.ContainsId(item.RequireToolId)))
			{
				return item;
			}
		}
		return null;
	}

	/// <summary>Get the conventional price for a tool upgrade.</summary>
	/// <param name="level">The level to which the tool is being upgraded.</param>
	public static int GetToolUpgradeConventionalPrice(int level)
	{
		return level switch
		{
			1 => 2000, 
			2 => 5000, 
			3 => 10000, 
			4 => 25000, 
			_ => 2000, 
		};
	}

	/// <summary>Get the unqualified item ID for the conventional material that must be provided for a tool upgrade.</summary>
	/// <param name="level">The level to which the tool is being upgraded.</param>
	private static string GetToolUpgradeConventionalTradeItem(int level)
	{
		return level switch
		{
			1 => "334", 
			2 => "335", 
			3 => "336", 
			4 => "337", 
			_ => "334", 
		};
	}

	/// <summary>Get the owner entries for a shop whose conditions currently match.</summary>
	/// <param name="shop">The shop data to check.</param>
	public static IEnumerable<ShopOwnerData> GetCurrentOwners(ShopData shop)
	{
		return shop?.Owners?.Where((ShopOwnerData owner) => GameStateQuery.CheckConditions(owner.Condition)) ?? LegacyShims.EmptyArray<ShopOwnerData>();
	}

	/// <summary>Get the sell price for a shop item, excluding quantity modifiers.</summary>
	/// <param name="output">The shop item for which to get the base price.</param>
	/// <param name="shopData">The shop data.</param>
	/// <param name="itemData">The shop item's data.</param>
	/// <param name="item">The item instance.</param>
	/// <param name="outOfSeasonPrice">Whether to apply the out-of-season pricing for Pierre's Missing Stock List.</param>
	/// <param name="useObjectDataPrice">If <paramref name="item" /> has type <see cref="F:StardewValley.ItemRegistry.type_object" />, whether to use the raw price in <c>Data/Objects</c> instead of the calculated sell-to-player price.</param>
	public static int GetBasePrice(ItemQueryResult output, ShopData shopData, ShopItemData itemData, ISalable item, bool outOfSeasonPrice, bool useObjectDataPrice = false)
	{
		float num = output.OverrideBasePrice ?? itemData.Price;
		if (num < 0f)
		{
			num = ((itemData.TradeItemId != null) ? 0f : ((!useObjectDataPrice || !item.HasTypeObject() || !(item is Object obj)) ? ((float)item.salePrice(ignoreProfitMargins: true)) : ((float)obj.Price)));
		}
		if (itemData.ApplyProfitMargins ?? shopData.ApplyProfitMargins ?? item.appliesProfitMargins())
		{
			num *= Game1.MasterPlayer.difficultyModifier;
		}
		if (outOfSeasonPrice)
		{
			num *= 1.5f;
		}
		return (int)num;
	}

	/// <summary>Add an item to the list of items already in the shop.</summary>
	/// <param name="stockedItems">The item IDs in the shop.</param>
	/// <param name="item">The item to track.</param>
	/// <returns>Returns whether the item was already in the shop.</returns>
	public static bool TrackSeenItems(HashSet<string> stockedItems, ISalable item)
	{
		string text = item.QualifiedItemId;
		if (item is Tool { UpgradeLevel: >0 } tool)
		{
			text = text + "#" + tool.UpgradeLevel;
		}
		if (item.IsRecipe)
		{
			text += "#Recipe";
		}
		return !stockedItems.Add(text);
	}
}
