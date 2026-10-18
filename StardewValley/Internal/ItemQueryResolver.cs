using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewValley.Delegates;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Pets;
using StardewValley.GameData.Tools;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StardewValley.Internal;

/// <summary>Resolves item IDs like <samp>(O)128</samp> and item queries like <samp>RANDOM_ITEMS</samp> in data assets.</summary>
/// <remarks>This is an internal implementation class. Most code should use higher-level code like <see cref="M:StardewValley.Utility.TryOpenShopMenu(System.String,System.String,System.Boolean)" /> instead.</remarks>
/// <summary>Resolves item IDs like <samp>(O)128</samp> and item queries like <samp>RANDOM_ITEMS</samp> in data assets.</summary>
/// <remarks>This is an internal implementation class. Most code should use higher-level code like <see cref="M:StardewValley.Utility.TryOpenShopMenu(System.String,System.String,System.Boolean)" /> instead.</remarks>
public static class ItemQueryResolver
{
	/// <summary>The helper methods which simplify implementing custom item queries.</summary>
	public static class Helpers
	{
		/// <summary>Split an argument list into individual arguments.</summary>
		/// <param name="arguments">The arguments to split.</param>
		public static string[] SplitArguments(string arguments)
		{
			if (arguments.Length <= 0)
			{
				return LegacyShims.EmptyArray<string>();
			}
			return ArgUtility.SplitBySpace(arguments);
		}

		/// <summary>Log an error for an invalid query, and return an empty list of items.</summary>
		/// <param name="key">The query key specified in the item ID.</param>
		/// <param name="arguments">Any text specified in the item ID after the <paramref name="key" />.</param>
		/// <param name="logError">Log an error message to the console, given the item query and error message.</param>
		/// <param name="message">A human-readable message indicating why the query is invalid.</param>
		public static ItemQueryResult[] ErrorResult(string key, string arguments, Action<string, string> logError, string message)
		{
			logError?.Invoke((key + " " + arguments).Trim(), message);
			return LegacyShims.EmptyArray<ItemQueryResult>();
		}

		/// <summary>Get whether to exclude this item from shops when selecting random items to sell, including catalogues.</summary>
		/// <param name="data">The parsed item data.</param>
		public static bool ExcludeFromRandomSale(ParsedItemData data)
		{
			if (data.ExcludeFromRandomSale)
			{
				return true;
			}
			string itemTypeId = data.GetItemTypeId();
			if (!(itemTypeId == "(WP)"))
			{
				if (itemTypeId == "(FL)" && Utility.isFlooringOffLimitsForSale(data.ItemId))
				{
					return true;
				}
			}
			else if (Utility.isWallpaperOffLimitsForSale(data.ItemId))
			{
				return true;
			}
			return false;
		}
	}

	/// <summary>The resolvers for vanilla item queries. Most code should call <c>TryResolve</c> instead of using these directly.</summary>
	public static class DefaultResolvers
	{
		/// <summary>Get every item in the game, optionally filtered by type. Format: <c>ALL_ITEMS [type]</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> ALL_ITEMS(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			string onlyTypeId = null;
			bool isRandomSale = false;
			bool requirePrice = false;
			string[] array = Helpers.SplitArguments(arguments);
			int num = 0;
			if (ArgUtility.HasIndex(array, 0) && !array[0].StartsWith('@'))
			{
				onlyTypeId = array[0];
				num++;
			}
			for (int i = num; i < array.Length; i++)
			{
				string text = array[i];
				if (text.EqualsIgnoreCase("@isRandomSale"))
				{
					isRandomSale = true;
					continue;
				}
				if (text.EqualsIgnoreCase("@requirePrice"))
				{
					requirePrice = true;
					continue;
				}
				if (text.StartsWith('@'))
				{
					Helpers.ErrorResult(key, arguments, logError, $"index {i} has unknown option flag '{text}'");
					yield break;
				}
				if (onlyTypeId != null && onlyTypeId != text)
				{
					Helpers.ErrorResult(key, arguments, logError, $"index {i} must be an option flag starting with '@'");
					yield break;
				}
				onlyTypeId = text;
			}
			foreach (IItemDataDefinition itemType in ItemRegistry.ItemTypes)
			{
				string identifier = itemType.Identifier;
				if (onlyTypeId != null && identifier != onlyTypeId)
				{
					continue;
				}
				if (identifier == "(F)")
				{
					List<Furniture> list = new List<Furniture>();
					foreach (ParsedItemData allDatum in itemType.GetAllData())
					{
						if (!isRandomSale || !Helpers.ExcludeFromRandomSale(allDatum))
						{
							Furniture furniture = ItemRegistry.Create<Furniture>(allDatum.QualifiedItemId);
							if (!requirePrice || furniture.salePrice(ignoreProfitMargins: true) > 0)
							{
								list.Add(furniture);
							}
						}
					}
					list.Sort(Utility.SortAllFurnitures);
					foreach (Furniture item2 in list)
					{
						yield return new ItemQueryResult(item2);
					}
					continue;
				}
				foreach (ParsedItemData allDatum2 in itemType.GetAllData())
				{
					if (!isRandomSale || !Helpers.ExcludeFromRandomSale(allDatum2))
					{
						Item item = ItemRegistry.Create(allDatum2.QualifiedItemId);
						if (!requirePrice || item.salePrice(ignoreProfitMargins: true) > 0)
						{
							yield return new ItemQueryResult(item);
						}
					}
				}
			}
		}

		/// <summary>Get the dish of the day sold at the Saloon, if any. Format: <c>DISH_OF_THE_DAY</c> (no arguments).</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> DISH_OF_THE_DAY(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			if (Game1.dishOfTheDay == null)
			{
				return LegacyShims.EmptyArray<ItemQueryResult>();
			}
			return new ItemQueryResult[1]
			{
				new ItemQueryResult(Game1.dishOfTheDay.getOne())
				{
					OverrideShopAvailableStock = Game1.dishOfTheDay.Stack,
					SyncStacksWith = Game1.dishOfTheDay
				}
			};
		}

		/// <summary>Get a flavored item for a given type and ingredient (like Wine + Blueberry = Blueberry Wine). Format: <c>FLAVORED_ITEM &lt;type&gt; &lt;ingredient item ID&gt; [ingredient preserved ID]</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> FLAVORED_ITEM(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			int value = 0;
			bool flag = false;
			string[] array = Helpers.SplitArguments(arguments);
			if (!Utility.TryParseEnum<Object.PreserveType>(array[0], out var parsed))
			{
				return Helpers.ErrorResult(key, arguments, logError, "invalid flavored item type (must be one of " + string.Join(", ", Enum.GetNames(typeof(Object.PreserveType))) + ")");
			}
			string text = ArgUtility.Get(array, 1);
			if (parsed == Object.PreserveType.Honey && text == "-1")
			{
				flag = true;
				text = null;
			}
			else
			{
				text = ItemRegistry.QualifyItemId(text);
				if (text == null)
				{
					return Helpers.ErrorResult(key, arguments, logError, "must specify a valid flavor ingredient ID");
				}
			}
			string text2 = ArgUtility.Get(array, 2);
			if (text2 == "0")
			{
				text2 = null;
			}
			ArgUtility.TryGetOptionalInt(array, 2, out value, out var _, 0, "quality");
			ObjectDataDefinition objectTypeDefinition = ItemRegistry.GetObjectTypeDefinition();
			Object obj = null;
			if (!flag)
			{
				try
				{
					obj = ((parsed == Object.PreserveType.AgedRoe && text == "(O)812" && text2 != null) ? objectTypeDefinition.CreateFlavoredItem(Object.PreserveType.Roe, ItemRegistry.Create<Object>(text2)) : (ItemRegistry.Create(text) as Object));
				}
				catch (Exception ex)
				{
					return Helpers.ErrorResult(key, arguments, logError, ex.Message);
				}
				if (obj != null)
				{
					obj.Quality = value;
				}
			}
			Object obj2 = objectTypeDefinition.CreateFlavoredItem(parsed, obj);
			if (obj2 == null)
			{
				return Helpers.ErrorResult(key, arguments, logError, $"unsupported flavor type '{parsed}'.");
			}
			return new ItemQueryResult[1]
			{
				new ItemQueryResult(obj2)
			};
		}

		/// <summary>Get the items lost when the player collapsed in the mines, which can be recovered from Marlon's shop. Format: <c>ITEMS_LOST_ON_DEATH</c> (no arguments).</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> ITEMS_LOST_ON_DEATH(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			List<ItemQueryResult> list = new List<ItemQueryResult>();
			foreach (Item item in Game1.player.itemsLostLastDeath)
			{
				if (item != null)
				{
					item.isLostItem = true;
					list.Add(new ItemQueryResult(item)
					{
						OverrideStackSize = item.Stack,
						OverrideBasePrice = ((Game1.player.stats.Get("Book_Marlon") != 0) ? ((int)((float)Utility.getSellToStorePriceOfItem(item) * 0.5f)) : Utility.getSellToStorePriceOfItem(item))
					});
				}
			}
			return list;
		}

		/// <summary>Get items the player has recently sold to a given shop. Format: <c>ITEMS_SOLD_BY_PLAYER &lt;shop location ID&gt;</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> ITEMS_SOLD_BY_PLAYER(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			if (string.IsNullOrWhiteSpace(arguments))
			{
				Helpers.ErrorResult(key, arguments, logError, "must specify a location ID");
				yield break;
			}
			GameLocation locationFromName = Game1.getLocationFromName(arguments);
			if (locationFromName == null)
			{
				Helpers.ErrorResult(key, arguments, logError, "the specified location ID didn't match any location");
				yield break;
			}
			if (!(locationFromName is ShopLocation shopLocation))
			{
				Helpers.ErrorResult(key, arguments, logError, "the specified location ID matched a location which isn't a ShopLocation instance");
				yield break;
			}
			foreach (Item item in shopLocation.itemsFromPlayerToSell)
			{
				if (item.Stack > 0)
				{
					int value = ((item is Object obj) ? obj.sellToStorePrice(-1L) : item.salePrice());
					yield return new ItemQueryResult(item.getOne())
					{
						OverrideBasePrice = value,
						OverrideShopAvailableStock = item.Stack,
						SyncStacksWith = item
					};
				}
			}
		}

		/// <summary>Get a fish which can be caught in a location based on its <c>Data/Locations</c> entry. Format: <c>LOCATION_FISH &lt;location name&gt; &lt;bobber x&gt; &lt;bobber y&gt; &lt;water depth&gt;</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> LOCATION_FISH(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			string[] array = Helpers.SplitArguments(arguments);
			if (array.Length != 4)
			{
				return Helpers.ErrorResult(key, arguments, logError, "expected four arguments in the form <location name> <bobber x> <bobber y> <depth>");
			}
			string locationName = array[0];
			string text = array[1];
			string text2 = array[2];
			string text3 = array[3];
			if (!int.TryParse(text, out var result) || !int.TryParse(text2, out var result2))
			{
				return Helpers.ErrorResult(key, arguments, logError, $"can't parse '{text} {text2}' as numeric 'x y' values");
			}
			if (!int.TryParse(text3, out var result3))
			{
				return Helpers.ErrorResult(key, arguments, logError, "can't parse '" + text3 + "' as a numeric depth value");
			}
			Item fishFromLocationData = GameLocation.GetFishFromLocationData(locationName, new Vector2(result, result2), result3, context?.Player, isTutorialCatch: false, isInherited: true);
			if (fishFromLocationData == null)
			{
				return LegacyShims.EmptyArray<ItemQueryResult>();
			}
			return new ItemQueryResult[1]
			{
				new ItemQueryResult(fishFromLocationData)
			};
		}

		/// <summary>Get a lost book (if they haven't all been found), else the given item query (if provided), else nothing. Format: <c>LOST_BOOK_OR_ITEM [alternate item query]</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> LOST_BOOK_OR_ITEM(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			if (Game1.netWorldState.Value.LostBooksFound < 21)
			{
				return new ItemQueryResult[1]
				{
					new ItemQueryResult(ItemRegistry.Create("(O)102"))
				};
			}
			if (string.IsNullOrWhiteSpace(arguments))
			{
				return LegacyShims.EmptyArray<ItemQueryResult>();
			}
			return TryResolve(arguments, new ItemQueryContext(context, "query 'LOST_BOOK_OR_ITEM'"));
		}

		/// <summary>Get the unique items which no longer exist anywhere in the world, which are shown in the crow lost-items shop. Format: <c>LOST_UNIQUE_ITEMS</c> (no arguments).</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> LOST_UNIQUE_ITEMS(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			List<ItemQueryResult> list = new List<ItemQueryResult>();
			foreach (Item item in Woods.GetLostItemsShopInventory())
			{
				if (item != null && item.Stack > 0)
				{
					list.Add(new ItemQueryResult(item)
					{
						OverrideStackSize = item.Stack,
						SyncStacksWith = item
					});
				}
			}
			return list;
		}

		/// <summary>Get the rewards that can currently be collected from Gil in the Adventurer's Guild. Format: <c>MONSTER_SLAYER_REWARDS</c> (no arguments).</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> MONSTER_SLAYER_REWARDS(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			KeyValuePair<string, MonsterSlayerQuestData>[] monsterSlayerQuestData = (from p in DataLoader.MonsterSlayerQuests(Game1.content)
				where AdventureGuild.HasCollectedReward(context.Player, p.Key)
				select p).ToArray();
			HashSet<string> questIds = new HashSet<string>();
			KeyValuePair<string, MonsterSlayerQuestData>[] array = monsterSlayerQuestData;
			for (int num = 0; num < array.Length; num++)
			{
				KeyValuePair<string, MonsterSlayerQuestData> keyValuePair = array[num];
				string id = keyValuePair.Key;
				MonsterSlayerQuestData value = keyValuePair.Value;
				if (!questIds.Contains(id) && value.RewardItemId != null && value.RewardItemPrice != -1 && ItemContextTagManager.HasBaseTag(value.RewardItemId, "item_type_ring"))
				{
					Item item = ItemRegistry.Create(value.RewardItemId);
					yield return new ItemQueryResult(item)
					{
						OverrideBasePrice = value.RewardItemPrice,
						OverrideShopAvailableStock = int.MaxValue
					};
					questIds.Add(id);
				}
			}
			array = monsterSlayerQuestData;
			for (int num = 0; num < array.Length; num++)
			{
				KeyValuePair<string, MonsterSlayerQuestData> keyValuePair2 = array[num];
				string id = keyValuePair2.Key;
				MonsterSlayerQuestData value2 = keyValuePair2.Value;
				if (!questIds.Contains(id) && value2.RewardItemId != null && value2.RewardItemPrice != -1 && !(ItemRegistry.ResolveMetadata(value2.RewardItemId)?.GetTypeDefinition()?.Identifier != "(H)"))
				{
					Item item2 = ItemRegistry.Create(value2.RewardItemId);
					yield return new ItemQueryResult(item2)
					{
						OverrideBasePrice = value2.RewardItemPrice,
						OverrideShopAvailableStock = int.MaxValue
					};
					questIds.Add(id);
				}
			}
			array = monsterSlayerQuestData;
			for (int num = 0; num < array.Length; num++)
			{
				KeyValuePair<string, MonsterSlayerQuestData> keyValuePair3 = array[num];
				string id = keyValuePair3.Key;
				MonsterSlayerQuestData value3 = keyValuePair3.Value;
				if (!questIds.Contains(id) && value3.RewardItemId != null && value3.RewardItemPrice != -1 && !(ItemRegistry.ResolveMetadata(value3.RewardItemId)?.GetTypeDefinition()?.Identifier != "(W)"))
				{
					Item item3 = ItemRegistry.Create(value3.RewardItemId);
					yield return new ItemQueryResult(item3)
					{
						OverrideBasePrice = value3.RewardItemPrice,
						OverrideShopAvailableStock = int.MaxValue
					};
					questIds.Add(id);
				}
			}
			array = monsterSlayerQuestData;
			for (int num = 0; num < array.Length; num++)
			{
				KeyValuePair<string, MonsterSlayerQuestData> keyValuePair4 = array[num];
				string id = keyValuePair4.Key;
				MonsterSlayerQuestData value4 = keyValuePair4.Value;
				if (!questIds.Contains(id) && value4.RewardItemId != null && value4.RewardItemPrice != -1)
				{
					Item item4 = ItemRegistry.Create(value4.RewardItemId);
					yield return new ItemQueryResult(item4)
					{
						OverrideBasePrice = value4.RewardItemPrice,
						OverrideShopAvailableStock = int.MaxValue
					};
					questIds.Add(id);
				}
			}
		}

		/// <summary>Get the movie concessions to show for an invited NPC. Format <c>MOVIE_CONCESSIONS_FOR_GUEST [npcName]</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> MOVIE_CONCESSIONS_FOR_GUEST(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			string text = ArgUtility.SplitBySpaceAndGet(arguments, 0);
			List<MovieConcession> list = ((text != null) ? MovieTheater.GetConcessionsForGuest(text) : MovieTheater.GetConcessionsForGuest());
			foreach (MovieConcession item in list)
			{
				yield return new ItemQueryResult(item);
			}
		}

		/// <summary>Get the first artifact in <c>Data/Objects</c> which lists the current location as a spawn location and whose chance matches. Format <c>RANDOM_ARTIFACT_FOR_DIG_SPOT</c> (no arguments).</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> RANDOM_ARTIFACT_FOR_DIG_SPOT(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			Random random = context.Random ?? Game1.random;
			Farmer player = context.Player;
			string name = context.Location.Name;
			Hoe obj = player.CurrentTool as Hoe;
			int num = ((obj == null || !obj.hasEnchantmentOfType<ArchaeologistEnchantment>()) ? 1 : 2);
			foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
			{
				if (!(allDatum.ObjectType != "Arch"))
				{
					Dictionary<string, float> dictionary = (allDatum.RawData as ObjectData)?.ArtifactSpotChances;
					if (dictionary != null && dictionary.TryGetValue(name, out var value) && random.NextBool((float)num * value))
					{
						return new ItemQueryResult[1]
						{
							new ItemQueryResult(ItemRegistry.Create(allDatum.QualifiedItemId))
						};
					}
				}
			}
			return LegacyShims.EmptyArray<ItemQueryResult>();
		}

		/// <summary>Get a random seasonal vanilla item which can be found by searching garbage cans, breaking containers in the mines, etc. Format: <c>RANDOM_BASE_SEASON_ITEM</c> (no arguments).</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> RANDOM_BASE_SEASON_ITEM(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			GameLocation location = context.Location;
			Item item = ItemRegistry.Create(Utility.getRandomItemFromSeason(random: context.Random ?? Utility.CreateDaySaveRandom(Game1.hash.GetDeterministicHashCode(key + arguments)), season: location.GetSeason(), forQuest: false));
			return new ItemQueryResult[1]
			{
				new ItemQueryResult(item)
			};
		}

		/// <summary>Get random items for a given type, optionally within a numeric ID range. Format: <c>RANDOM_ITEMS &lt;item data definition ID&gt; [min numeric id] [max numeric id]</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> RANDOM_ITEMS(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			int minId = int.MinValue;
			int maxId = int.MaxValue;
			bool isRandomSale = false;
			bool requirePrice = false;
			string[] array = Helpers.SplitArguments(arguments);
			if (!ArgUtility.TryGet(array, 0, out var value, out var error, allowBlank: false, "typeId"))
			{
				Helpers.ErrorResult(key, arguments, logError, error);
				yield break;
			}
			int num = 1;
			if (ArgUtility.HasIndex(array, 1) && int.TryParse(array[1], out var result))
			{
				minId = result;
				num++;
				if (ArgUtility.HasIndex(array, 2) && int.TryParse(array[2], out result))
				{
					maxId = result;
					num++;
				}
			}
			for (int i = num; i < array.Length; i++)
			{
				string text = array[i];
				if (text.EqualsIgnoreCase("@isRandomSale"))
				{
					isRandomSale = true;
					continue;
				}
				if (text.EqualsIgnoreCase("@requirePrice"))
				{
					requirePrice = true;
					continue;
				}
				if (text.StartsWith('@'))
				{
					Helpers.ErrorResult(key, arguments, logError, $"index {i} has unknown flag argument '{text}'");
				}
				else if (i == 1 || i == 2)
				{
					Helpers.ErrorResult(key, arguments, logError, $"index {i} must a numeric {((i == 1) ? "min" : "max")} ID, or an option flag starting with '@'.");
				}
				else
				{
					Helpers.ErrorResult(key, arguments, logError, $"index {i} must be an option flag starting with '@'.");
				}
				yield break;
			}
			IItemDataDefinition typeDefinition = ItemRegistry.GetTypeDefinition(value);
			if (typeDefinition == null)
			{
				Helpers.ErrorResult(key, arguments, logError, "there's no item data definition with ID '" + value + "'");
				yield break;
			}
			bool hasRange = minId != int.MinValue || maxId != int.MaxValue;
			Random random = context.Random ?? Game1.random;
			foreach (ParsedItemData item2 in from p in typeDefinition.GetAllData()
				orderby random.Next()
				select p)
			{
				if ((!isRandomSale || !Helpers.ExcludeFromRandomSale(item2)) && (!hasRange || (int.TryParse(item2.ItemId, out var result2) && result2 >= minId && result2 <= maxId)))
				{
					Item item = ItemRegistry.Create(item2.QualifiedItemId);
					if (!requirePrice || item.salePrice(ignoreProfitMargins: true) > 0)
					{
						yield return new ItemQueryResult(item);
					}
				}
			}
		}

		/// <summary>Get a secret note (if the player unlocked them and hasn't found them all), else the given item query (if provided), else nothing. Format: <c>SECRET_NOTE_OR_ITEM [alternate item query]</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> SECRET_NOTE_OR_ITEM(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			GameLocation location = context.Location;
			Farmer player = context.Player;
			if (location != null && location.HasUnlockedAreaSecretNotes(player))
			{
				Object obj = location.tryToCreateUnseenSecretNote(player);
				if (obj != null)
				{
					return new ItemQueryResult[1]
					{
						new ItemQueryResult(obj)
					};
				}
			}
			if (string.IsNullOrWhiteSpace(arguments))
			{
				return LegacyShims.EmptyArray<ItemQueryResult>();
			}
			return TryResolve(arguments, new ItemQueryContext(context, "query 'SECRET_NOTE_OR_ITEM'"));
		}

		/// <summary>Get a special 'key to the town' shop item. This returns an <see cref="T:StardewValley.ISalable" /> instance which may be ignored or invalid outside shops. Format: <c>SHOP_TOWN_KEY</c> (no arguments).</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> SHOP_TOWN_KEY(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			ISalable item = new PurchaseableKeyItem(Game1.content.LoadString("Strings\\StringsFromCSFiles:KeyToTheTown"), Game1.content.LoadString("Strings\\StringsFromCSFiles:KeyToTheTown_desc"), 912, delegate(Farmer farmer)
			{
				farmer.HasTownKey = true;
			});
			return new ItemQueryResult[1]
			{
				new ItemQueryResult(item)
				{
					OverrideShopAvailableStock = 1
				}
			};
		}

		/// <summary>Get the tool upgrades listed in <c>Data/Shops</c> for the given tool ID (or all tool upgrades if <c>[tool ID]</c> is omitted). Format: <c>TOOL_UPGRADES [tool ID]</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.ResolveItemQueryDelegate" />
		public static IEnumerable<ItemQueryResult> TOOL_UPGRADES(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			string text = null;
			if (!string.IsNullOrWhiteSpace(arguments))
			{
				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(arguments);
				if (dataOrErrorItem.HasTypeId("(T)"))
				{
					return Helpers.ErrorResult(key, arguments, logError, "can't filter for ID '" + arguments + "' because that isn't a tool item ID");
				}
				text = dataOrErrorItem.ItemId;
			}
			List<ItemQueryResult> list = new List<ItemQueryResult>();
			foreach (KeyValuePair<string, ToolData> toolDatum in Game1.toolData)
			{
				string key2 = toolDatum.Key;
				ToolData value = toolDatum.Value;
				if (text == null || !(key2 != text))
				{
					ToolUpgradeData toolUpgradeData = ShopBuilder.GetToolUpgradeData(value, Game1.player);
					if (toolUpgradeData != null)
					{
						Item item = ItemRegistry.Create("(T)" + key2);
						int value2 = ((toolUpgradeData.Price > -1) ? toolUpgradeData.Price : Math.Max(0, item.salePrice()));
						list.Add(new ItemQueryResult(item)
						{
							OverrideBasePrice = value2,
							OverrideShopAvailableStock = 1,
							OverrideTradeItemId = toolUpgradeData.TradeItemId,
							OverrideTradeItemAmount = toolUpgradeData.TradeItemAmount
						});
					}
				}
			}
			return list;
		}

		public static IEnumerable<ItemQueryResult> PET_ADOPTION(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
		{
			List<ItemQueryResult> list = new List<ItemQueryResult>();
			foreach (KeyValuePair<string, PetData> petDatum in Game1.petData)
			{
				foreach (PetBreed breed in petDatum.Value.Breeds)
				{
					if (breed.CanBeAdoptedFromMarnie)
					{
						list.Add(new ItemQueryResult(new PetLicense
						{
							Name = petDatum.Key + "|" + breed.Id
						})
						{
							OverrideBasePrice = breed.AdoptionPrice
						});
					}
				}
			}
			return list;
		}
	}

	/// <summary>The item query keys that can be used instead of an item ID in list data fields like <see cref="P:StardewValley.GameData.ISpawnItemData.ItemId" /> or <see cref="P:StardewValley.GameData.ISpawnItemData.RandomItemId" /> fields, and the methods which create the items for them.</summary>
	public static Dictionary<string, ResolveItemQueryDelegate> ItemResolvers { get; }

	/// <summary>Register the default item queries, defined as <see cref="T:StardewValley.Internal.ItemQueryResolver.DefaultResolvers" /> methods.</summary>
	static ItemQueryResolver()
	{
		ItemResolvers = new Dictionary<string, ResolveItemQueryDelegate>(StringComparer.OrdinalIgnoreCase);
		MethodInfo[] methods = typeof(DefaultResolvers).GetMethods(BindingFlags.Static | BindingFlags.Public);
		foreach (MethodInfo methodInfo in methods)
		{
			ResolveItemQueryDelegate queryDelegate = (ResolveItemQueryDelegate)Delegate.CreateDelegate(typeof(ResolveItemQueryDelegate), methodInfo);
			Register(methodInfo.Name, queryDelegate);
		}
	}

	/// <summary>Register an item query resolver.</summary>
	/// <param name="queryKey">The item query key, like <c>ALL_ITEMS</c>. This should only contain alphanumeric, underscore, and dot characters. For custom queries, this should be prefixed with your mod ID like <c>Example.ModId_QueryName</c>.</param>
	/// <param name="queryDelegate">The resolver which returns the items produced by the item query.</param>
	/// <exception cref="T:System.ArgumentException">The <paramref name="queryKey" /> is null or whitespace-only.</exception>
	/// <exception cref="T:System.ArgumentNullException">The <paramref name="queryDelegate" /> is null.</exception>
	/// <exception cref="T:System.InvalidOperationException">The <paramref name="queryKey" /> is already registered.</exception>
	public static void Register(string queryKey, ResolveItemQueryDelegate queryDelegate)
	{
		if (string.IsNullOrWhiteSpace(queryKey))
		{
			throw new ArgumentException("The query key can't be null or empty.", "queryKey");
		}
		if (ItemResolvers.ContainsKey(queryKey))
		{
			throw new InvalidOperationException("The query key '" + queryKey + "' is already registered.");
		}
		ItemResolvers[queryKey.Trim()] = queryDelegate ?? throw new ArgumentNullException("queryDelegate");
	}

	/// <summary>Get the items matching an item ID or query.</summary>
	/// <param name="query">The item ID or query to match.</param>
	/// <param name="context">The contextual info for item queries, or <c>null</c> to use the global context.</param>
	/// <param name="filter">The filter to apply to the search results.</param>
	/// <param name="perItemCondition">A game state query which indicates whether an item produced from the other fields should be returned. Defaults to always true.</param>
	/// <param name="maxItems">The maximum number of item stacks to produce, or <c>null</c> to include all stacks produced by the <paramref name="query" />.</param>
	/// <param name="avoidRepeat">Whether to avoid adding duplicate items.</param>
	/// <param name="avoidItemIds">The qualified item IDs which shouldn't be returned.</param>
	/// <param name="logError">Log an error message to the console, given the item query and error message.</param>
	public static ItemQueryResult[] TryResolve(string query, ItemQueryContext context, ItemQuerySearchMode filter = ItemQuerySearchMode.All, string perItemCondition = null, int? maxItems = null, bool avoidRepeat = false, HashSet<string> avoidItemIds = null, Action<string, string> logError = null)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return Helpers.ErrorResult(query, "", logError, "must specify an item ID or query");
		}
		string key = query;
		string text = null;
		int num = query.IndexOf(' ');
		if (num > -1)
		{
			key = query.Substring(0, num);
			text = query.Substring(num + 1);
		}
		if (context == null)
		{
			context = new ItemQueryContext();
		}
		context.QueryString = query;
		if (context.ParentContext != null)
		{
			List<string> list = new List<string>();
			for (ItemQueryContext itemQueryContext = context; itemQueryContext != null; itemQueryContext = itemQueryContext.ParentContext)
			{
				bool num2 = list.Contains(itemQueryContext.QueryString);
				list.Add(itemQueryContext.QueryString);
				if (num2)
				{
					logError?.Invoke(query, "detected circular reference in item queries: " + string.Join(" -> ", list));
					return LegacyShims.EmptyArray<ItemQueryResult>();
				}
			}
		}
		IEnumerable<ItemQueryResult> enumerable;
		if (ItemResolvers.TryGetValue(key, out var value))
		{
			enumerable = value(key, text ?? string.Empty, context, avoidRepeat, avoidItemIds, logError ?? new Action<string, string>(LogNothing));
			if (enumerable is ItemQueryResult[] array && array.Length == 0)
			{
				return array;
			}
			HashSet<string> duplicates = (avoidRepeat ? new HashSet<string>() : null);
			if (!avoidRepeat)
			{
				HashSet<string> hashSet = avoidItemIds;
				if ((hashSet == null || hashSet.Count <= 0) && GameStateQuery.IsImmutablyFalse(perItemCondition))
				{
					goto IL_0174;
				}
			}
			enumerable = enumerable.Where(delegate(ItemQueryResult result)
			{
				HashSet<string> hashSet3 = avoidItemIds;
				if (hashSet3 == null || !hashSet3.Contains(result.Item.QualifiedItemId))
				{
					HashSet<string> hashSet4 = duplicates;
					if (hashSet4 == null || hashSet4.Add(result.Item.QualifiedItemId))
					{
						return GameStateQuery.CheckConditions(perItemCondition, null, null, result.Item as Item);
					}
				}
				return false;
			});
			goto IL_0174;
		}
		Item item = ItemRegistry.Create(query);
		if (item != null)
		{
			HashSet<string> hashSet2 = avoidItemIds;
			if (hashSet2 == null || !hashSet2.Contains(item.QualifiedItemId))
			{
				return new ItemQueryResult[1]
				{
					new ItemQueryResult(item)
				};
			}
		}
		return LegacyShims.EmptyArray<ItemQueryResult>();
		IL_0174:
		switch (filter)
		{
		case ItemQuerySearchMode.AllOfTypeItem:
			enumerable = enumerable.Where((ItemQueryResult result) => result.Item is Item);
			break;
		case ItemQuerySearchMode.FirstOfTypeItem:
		{
			ItemQueryResult itemQueryResult2 = enumerable.FirstOrDefault((ItemQueryResult p) => p.Item is Item);
			enumerable = ((itemQueryResult2 == null) ? LegacyShims.EmptyArray<ItemQueryResult>() : new ItemQueryResult[1] { itemQueryResult2 });
			break;
		}
		case ItemQuerySearchMode.RandomOfTypeItem:
		{
			ItemQueryResult itemQueryResult = (context.Random ?? Game1.random).ChooseFrom(enumerable.Where((ItemQueryResult p) => p.Item is Item).ToArray());
			enumerable = ((itemQueryResult == null) ? LegacyShims.EmptyArray<ItemQueryResult>() : new ItemQueryResult[1] { itemQueryResult });
			break;
		}
		}
		if (maxItems.HasValue)
		{
			enumerable = enumerable.Take(maxItems.Value);
		}
		return (enumerable as ItemQueryResult[]) ?? enumerable.ToArray();
	}

	/// <summary>Get the items matching spawn data from a content asset.</summary>
	/// <param name="data">The spawn data to match.</param>
	/// <param name="context">The contextual info for item queries, or <c>null</c> to use the global context.</param>
	/// <param name="filter">The filter to apply to the search results.</param>
	/// <param name="avoidRepeat">Whether to avoid adding duplicate items.</param>
	/// <param name="avoidItemIds">The qualified item IDs which shouldn't be returned.</param>
	/// <param name="formatItemId">Format the raw item ID before it's resolved. Note that this is applied after <paramref name="avoidRepeat" /> and <paramref name="avoidItemIds" /> are checked.</param>
	/// <param name="inputItem">The input item (e.g. machine input) for which to check queries, or <c>null</c> if not applicable.</param>
	/// <param name="logError">Log an error message to the console, given the item query and error message.</param>
	public static IList<ItemQueryResult> TryResolve(ISpawnItemData data, ItemQueryContext context, ItemQuerySearchMode filter = ItemQuerySearchMode.All, bool avoidRepeat = false, HashSet<string> avoidItemIds = null, Func<string, string> formatItemId = null, Action<string, string> logError = null, Item inputItem = null)
	{
		Random random = context?.Random ?? Game1.random;
		string selected = data.ItemId;
		List<string> randomItemId = data.RandomItemId;
		if (randomItemId != null && randomItemId.Any())
		{
			if (avoidItemIds != null)
			{
				if (!Utility.TryGetRandomExcept(data.RandomItemId, avoidItemIds, random, out selected))
				{
					return LegacyShims.EmptyArray<ItemQueryResult>();
				}
			}
			else
			{
				selected = random.ChooseFrom(data.RandomItemId);
			}
		}
		if (string.IsNullOrWhiteSpace(selected))
		{
			Game1.log.Warn(FormatLogMessage("Item spawn fields for {0} produced a null or empty item ID.", data, context));
			return LegacyShims.EmptyArray<ItemQueryResult>();
		}
		if (formatItemId != null)
		{
			selected = formatItemId(selected);
		}
		ItemQueryResult[] array = TryResolve(selected, context, filter, data.PerItemCondition, data.MaxItems, avoidRepeat, avoidItemIds, logError);
		ItemQueryResult[] array2 = array;
		foreach (ItemQueryResult obj in array2)
		{
			obj.Item = ApplyItemFields(obj.Item, data, context, inputItem);
		}
		return array;
	}

	/// <summary>Get a random item matching an item ID or query.</summary>
	/// <param name="query">The item ID or query to match.</param>
	/// <param name="context">The contextual info for item queries, or <c>null</c> to use the global context.</param>
	/// <param name="avoidRepeat">Whether to avoid adding duplicate items.</param>
	/// <param name="avoidItemIds">The qualified item IDs which shouldn't be returned.</param>
	/// <param name="logError">Log an error message to the console, given the item query and error message.</param>
	public static Item TryResolveRandomItem(string query, ItemQueryContext context, bool avoidRepeat = false, HashSet<string> avoidItemIds = null, Action<string, string> logError = null)
	{
		return TryResolve(query, context, ItemQuerySearchMode.RandomOfTypeItem, null, null, avoidRepeat, avoidItemIds, logError).FirstOrDefault()?.Item as Item;
	}

	/// <summary>Get the items matching spawn data from a content asset.</summary>
	/// <param name="data">The spawn data to match.</param>
	/// <param name="context">The contextual info for item queries, or <c>null</c> to use the global context.</param>
	/// <param name="avoidRepeat">Whether to avoid adding duplicate items.</param>
	/// <param name="avoidItemIds">The qualified item IDs which shouldn't be returned.</param>
	/// <param name="formatItemId">Format the selected item ID before it's resolved.</param>
	/// <param name="inputItem">The input item (e.g. machine input) for which to check queries, or <c>null</c> if not applicable.</param>
	/// <param name="logError">Log an error message to the console, given the item query and error message.</param>
	public static Item TryResolveRandomItem(ISpawnItemData data, ItemQueryContext context, bool avoidRepeat = false, HashSet<string> avoidItemIds = null, Func<string, string> formatItemId = null, Item inputItem = null, Action<string, string> logError = null)
	{
		return TryResolve(data, context, ItemQuerySearchMode.RandomOfTypeItem, avoidRepeat, avoidItemIds, formatItemId, logError, inputItem).FirstOrDefault()?.Item as Item;
	}

	/// <summary>Apply data fields to an item instance.</summary>
	/// <param name="item">The item to modify.</param>
	/// <param name="data">The spawn data to apply.</param>
	/// <param name="context">The contextual info for item queries, or <c>null</c> to use the global context.</param>
	/// <remarks>This is applied automatically by methods which take an <see cref="T:StardewValley.GameData.ISpawnItemData" />, so it only needs to be called directly when creating an item from an item query string directly.</remarks>
	/// <param name="inputItem">The input item (e.g. machine input) for which to check queries, or <c>null</c> if not applicable.</param>
	/// <returns>Returns the modified item. This is usually the input <paramref name="item" />, but may be a new item instance in some cases.</returns>
	public static ISalable ApplyItemFields(ISalable item, ISpawnItemData data, ItemQueryContext context, Item inputItem = null)
	{
		return ApplyItemFields(item, data.MinStack, data.MaxStack, data.ToolUpgradeLevel, data.ObjectInternalName, data.ObjectDisplayName, data.ObjectColor, data.Quality, data.IsRecipe, data.StackModifiers, data.StackModifierMode, data.QualityModifiers, data.QualityModifierMode, data.ModData, context, inputItem);
	}

	/// <summary>Apply data fields to an item instance.</summary>
	/// <param name="item">The item to modify.</param>
	/// <param name="minStackSize">The minimum stack size for the item to create, or <c>-1</c> to keep it as-is.</param>
	/// <param name="maxStackSize">The maximum stack size for the item to create, or <c>-1</c> to match <paramref name="minStackSize" />.</param>
	/// <param name="toolUpgradeLevel"><inheritdoc cref="P:StardewValley.GameData.ISpawnItemData.ToolUpgradeLevel" path="/summary" /></param>
	/// <param name="objectInternalName"><inheritdoc cref="P:StardewValley.GameData.ISpawnItemData.ObjectInternalName" path="/summary" /></param>
	/// <param name="objectDisplayName"><inheritdoc cref="P:StardewValley.GameData.ISpawnItemData.ObjectDisplayName" path="/summary" /></param>
	/// <param name="objectColor"><inheritdoc cref="P:StardewValley.GameData.ISpawnItemData.ObjectColor" path="/summary" /></param>
	/// <param name="quality"><inheritdoc cref="P:StardewValley.GameData.ISpawnItemData.Quality" path="/summary" /></param>
	/// <param name="isRecipe"><inheritdoc cref="P:StardewValley.GameData.ISpawnItemData.IsRecipe" path="/summary" /></param>
	/// <param name="stackSizeModifiers">The modifiers to apply to the item's stack size.</param>
	/// <param name="stackSizeModifierMode">How multiple <paramref name="stackSizeModifiers" /> should be combined.</param>
	/// <param name="qualityModifiers">The modifiers to apply to the item's quality.</param>
	/// <param name="qualityModifierMode">How multiple <paramref name="qualityModifiers" /> should be combined.</param>
	/// <param name="modData"><inheritdoc cref="P:StardewValley.GameData.ISpawnItemData.ModData" path="/summary" /></param>
	/// <param name="context">The contextual info for item queries, or <c>null</c> to use the global context.</param>
	/// <param name="inputItem">The input item (e.g. machine input) for which to check queries, or <c>null</c> if not applicable.</param>
	/// <returns>Returns the modified item. This is usually the input <paramref name="item" />, but may be a new item instance in some cases.</returns>
	/// <remarks>This is applied automatically by methods which take an <see cref="T:StardewValley.GameData.ISpawnItemData" />, so it only needs to be called directly when creating an item from an item query string directly.</remarks>
	public static ISalable ApplyItemFields(ISalable item, int minStackSize, int maxStackSize, int toolUpgradeLevel, string objectInternalName, string objectDisplayName, string objectColor, int quality, bool isRecipe, List<QuantityModifier> stackSizeModifiers, QuantityModifier.QuantityModifierMode stackSizeModifierMode, List<QuantityModifier> qualityModifiers, QuantityModifier.QuantityModifierMode qualityModifierMode, Dictionary<string, string> modData, ItemQueryContext context, Item inputItem = null)
	{
		if (item == null)
		{
			return null;
		}
		Ring ring = item as Ring;
		if (ring != null && isRecipe)
		{
			item = new Object(ring.ItemId, ring.Stack, isRecipe: true);
		}
		int num = 1;
		if (!isRecipe)
		{
			if (minStackSize == -1 && maxStackSize == -1)
			{
				num = item.Stack;
			}
			else if (maxStackSize > 1)
			{
				minStackSize = Math.Max(minStackSize, 1);
				maxStackSize = Math.Max(maxStackSize, minStackSize);
				num = (context?.Random ?? Game1.random).Next(minStackSize, maxStackSize + 1);
			}
			else if (minStackSize > 1)
			{
				num = minStackSize;
			}
			num = (int)Utility.ApplyQuantityModifiers(num, stackSizeModifiers, stackSizeModifierMode, context?.Location, context?.Player, item as Item, inputItem, context?.Random);
		}
		quality = ((quality >= 0) ? quality : item.Quality);
		quality = (int)Utility.ApplyQuantityModifiers(quality, qualityModifiers, qualityModifierMode, context?.Location, context?.Player, item as Item, inputItem, context?.Random);
		if (isRecipe)
		{
			item.IsRecipe = true;
		}
		if (num > -1 && num != item.Stack)
		{
			item.Stack = num;
			item.FixStackSize();
		}
		if (quality >= 0 && quality != item.Quality)
		{
			item.Quality = quality;
			item.FixQuality();
		}
		if (modData != null && modData.Count > 0)
		{
			(item as Item)?.modData.CopyFrom(modData);
		}
		if (!(item is Object obj))
		{
			if (item is Tool tool && toolUpgradeLevel > -1 && toolUpgradeLevel != tool.UpgradeLevel)
			{
				tool.UpgradeLevel = toolUpgradeLevel;
			}
		}
		else
		{
			if (!string.IsNullOrWhiteSpace(objectInternalName))
			{
				obj.Name = objectInternalName;
			}
			if (!string.IsNullOrWhiteSpace(objectDisplayName))
			{
				obj.displayNameFormat = objectDisplayName;
			}
			if (!string.IsNullOrWhiteSpace(objectColor) && item.HasTypeObject())
			{
				Color? color = Utility.StringToColor(objectColor);
				if (color.HasValue && ColoredObject.TrySetColor(obj, color.Value, out var coloredItem))
				{
					Object obj2;
					item = (obj2 = coloredItem);
				}
			}
		}
		return item;
	}

	/// <summary>Build a log message with the source context.</summary>
	/// <param name="template">The template message to log, where <c>{0}</c> is the source phrase. See remarks on <see cref="P:StardewValley.Internal.ItemQueryContext.SourcePhrase" />.</param>
	/// <param name="data">The item spawn data.</param>
	/// <param name="context">The contextual info for item queries, or <c>null</c> to use the global context.</param>
	public static string FormatLogMessage(string template, ISpawnItemData data, ItemQueryContext context)
	{
		string text = (data as GenericSpawnItemData)?.Id;
		string arg = ((context != null && context.SourcePhrase != null) ? ((text != null) ? (context.SourcePhrase + " > entry '" + text + "'") : context.SourcePhrase) : ((text == null) ? "unknown context" : ("entry '" + text + "'")));
		return string.Format(template, arg);
	}

	/// <summary>A default implementation for <c>logError</c> parameters which logs nothing.</summary>
	/// <param name="query">The item query which failed.</param>
	/// <param name="error">The error indicating why it failed.</param>
	private static void LogNothing(string query, string error)
	{
	}
}
