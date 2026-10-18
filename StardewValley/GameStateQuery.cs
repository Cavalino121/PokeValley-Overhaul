using System;
using System.Collections.Generic;
using System.Reflection;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects.Trinkets;

namespace StardewValley;

/// <summary>Resolves game state queries like <c>SEASON spring</c> in data assets.</summary>
/// <summary>Resolves game state queries like <c>SEASON spring</c> in data assets.</summary>
public class GameStateQuery
{
	/// <summary>The helper methods which simplify implementing custom game state query resolvers.</summary>
	public static class Helpers
	{
		/// <summary>Get the location matching a given name.</summary>
		/// <param name="locationName">The location to check. This can be <c>Here</c> (current location), <c>Target</c> (contextual location), or a location name.</param>
		/// <param name="contextualLocation">The location for which the query is being checked.</param>
		public static GameLocation GetLocation(string locationName, GameLocation contextualLocation)
		{
			if (locationName.EqualsIgnoreCase("Here"))
			{
				return Game1.currentLocation;
			}
			if (locationName.EqualsIgnoreCase("Target"))
			{
				return contextualLocation ?? Game1.currentLocation;
			}
			return Game1.getLocationFromName(locationName);
		}

		/// <summary>Get the location matching a given name, or throw an exception if it's not found.</summary>
		/// <param name="locationName">The location to check. This can be <c>Here</c> (current location), <c>Target</c> (contextual location), or a location name.</param>
		/// <param name="contextualLocation">The location for which the query is being checked.</param>
		public static GameLocation RequireLocation(string locationName, GameLocation contextualLocation)
		{
			return GetLocation(locationName, contextualLocation) ?? throw new KeyNotFoundException("Required location '" + locationName + "' not found.");
		}

		/// <summary>Try to get a location matching a target location argument.</summary>
		/// <param name="query">The game state query split by space, including the query key.</param>
		/// <param name="index">The argument index to read.</param>
		/// <param name="error">An error phrase indicating why getting the argument failed (like 'required index X not found'), if applicable.</param>
		/// <param name="location">The contextual location instance, which will be updated if the argument is valid.</param>
		public static bool TryGetLocationArg(string[] query, int index, ref GameLocation location, out string error)
		{
			if (!ArgUtility.TryGet(query, index, out var value, out error, allowBlank: true, "string locationTarget"))
			{
				location = null;
				return false;
			}
			GameLocation location2 = GetLocation(value, location);
			if (location2 == null)
			{
				error = "no location found matching '" + value + "'";
				return false;
			}
			location = location2;
			return true;
		}

		/// <summary>Try to get an item matching an item type argument.</summary>
		/// <param name="query">The game state query split by space, including the query key.</param>
		/// <param name="index">The argument index to read.</param>
		/// <param name="targetItem">The target item (e.g. machine output or tree fruit), or <c>null</c> if not applicable.</param>
		/// <param name="inputItem">The input item (e.g. machine input), or <c>null</c> if not applicable.</param>
		/// <param name="error">An error phrase indicating why getting the argument failed (like 'required index X not found'), if applicable.</param>
		/// <param name="item">The item instance, if valid.</param>
		public static bool TryGetItemArg(string[] query, int index, Item targetItem, Item inputItem, out Item item, out string error)
		{
			if (!ArgUtility.TryGet(query, index, out var value, out error, allowBlank: true, "string itemType"))
			{
				item = null;
				return false;
			}
			if (value.EqualsIgnoreCase("Target"))
			{
				item = targetItem;
				return true;
			}
			if (value.EqualsIgnoreCase("Input"))
			{
				item = inputItem;
				return true;
			}
			item = null;
			error = "invalid item type '" + value + "' (should be 'Input' or 'Target')";
			return false;
		}

		/// <summary>Get whether a check applies to the given player or players.</summary>
		/// <param name="contextualPlayer">The player for which the query is being checked.</param>
		/// <param name="playerKey">The players to check. This can be <c>Any</c> (at least one player matches), <c>All</c> (every player matches), <c>Current</c> (the current player), <c>Target</c> (the contextual player), <c>Host</c> (the main player), or a player ID.</param>
		/// <param name="check">The check to perform.</param>
		public static bool WithPlayer(Farmer contextualPlayer, string playerKey, Func<Farmer, bool> check)
		{
			if (playerKey.EqualsIgnoreCase("Any"))
			{
				foreach (Farmer allFarmer in Game1.getAllFarmers())
				{
					if (check(allFarmer))
					{
						return true;
					}
				}
				return false;
			}
			if (playerKey.EqualsIgnoreCase("All"))
			{
				foreach (Farmer allFarmer2 in Game1.getAllFarmers())
				{
					if (!check(allFarmer2))
					{
						return false;
					}
				}
				return true;
			}
			if (playerKey.EqualsIgnoreCase("Current"))
			{
				return check(Game1.player);
			}
			if (playerKey.EqualsIgnoreCase("Target"))
			{
				return check(contextualPlayer);
			}
			if (playerKey.EqualsIgnoreCase("Host"))
			{
				return check(Game1.MasterPlayer);
			}
			if (long.TryParse(playerKey, out var result))
			{
				return check(Game1.GetPlayer(result));
			}
			return false;
		}

		/// <summary>Get whether any query argument matches a condition.</summary>
		/// <param name="query">The game state query split by space, including the query key.</param>
		/// <param name="startAt">The index within <paramref name="query" /> to start iterating.</param>
		/// <param name="check">Check whether a query argument matches. This should return true (argument matches), false (argument doesn't match, but we can try the remaining arguments), or null (argument caused an error so we should stop iterating).</param>
		public static bool AnyArgMatches(string[] query, int startAt, Func<string, bool?> check)
		{
			for (int i = startAt; i < query.Length; i++)
			{
				bool? flag = check(query[i]);
				if (flag.HasValue)
				{
					if (flag == true)
					{
						return true;
					}
					continue;
				}
				return false;
			}
			return false;
		}

		/// <summary>Log an error indicating that a query couldn't be parsed.</summary>
		/// <param name="query">The game state query split by space, including the query key.</param>
		/// <param name="reason">The human-readable reason why the query is invalid.</param>
		/// <param name="exception">The underlying exception, if applicable.</param>
		/// <returns>Returns false.</returns>
		public static bool ErrorResult(string[] query, string reason, Exception exception = null)
		{
			Game1.log.Error($"Failed parsing condition '{string.Join(" ", query)}': {reason}.", exception);
			return false;
		}

		/// <summary>The common implementation for <c>PLAYER_*_SKILL</c> game state queries.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PlayerSkillLevelImpl(string[] query, Farmer player, Func<Farmer, int> getLevel)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetInt(query, 2, out var minLevel, out error, "int minLevel") || !ArgUtility.TryGetOptionalInt(query, 3, out var maxLevel, out error, int.MaxValue, "int maxLevel"))
			{
				return ErrorResult(query, error);
			}
			return WithPlayer(player, value, delegate(Farmer target)
			{
				int num = getLevel(target);
				return num >= minLevel && num <= maxLevel;
			});
		}

		/// <summary>The common implementation for most <c>RANDOM</c> game state queries.</summary>
		/// <param name="random">The random instance to use.</param>
		/// <param name="query">The condition arguments received by the query.</param>
		/// <param name="skipArguments">The number of arguments to skip. The next argument should be the chance value, followed by an optional <c>@addDailyLuck</c> argument.</param>
		public static bool RandomImpl(Random random, string[] query, int skipArguments)
		{
			if (!ArgUtility.TryGetFloat(query, skipArguments, out var value, out var error, "float chance"))
			{
				return ErrorResult(query, error);
			}
			bool flag = false;
			for (int i = skipArguments + 1; i < query.Length; i++)
			{
				if (query[i].EqualsIgnoreCase("@addDailyLuck"))
				{
					flag = true;
				}
			}
			if (flag)
			{
				value += (float)Game1.player.DailyLuck;
			}
			return random.NextDouble() < (double)value;
		}
	}

	/// <summary>The cached metadata for a single raw game state query.</summary>
	public readonly struct ParsedGameStateQuery
	{
		/// <summary>Whether the result should be negated.</summary>
		public readonly bool Negated;

		/// <summary>The game state query split by space, including the query key.</summary>
		public readonly string[] Query;

		/// <summary>The resolver which handles the game state query.</summary>
		public readonly GameStateQueryDelegate Resolver;

		/// <summary>An error indicating why the query is invalid, if applicable.</summary>
		public readonly string Error;

		/// <summary>Construct an instance.</summary>
		/// <param name="negated">Whether the result should be negated.</param>
		/// <param name="query">The game state query split by space, including the query key.</param>
		/// <param name="resolver">The resolver which handles the game state query.</param>
		/// <param name="error">An error indicating why parsing the query failed, if applicable.</param>
		public ParsedGameStateQuery(bool negated, string[] query, GameStateQueryDelegate resolver, string error)
		{
			Negated = negated;
			Query = query;
			Resolver = resolver;
			Error = error;
		}
	}

	/// <summary>The resolvers for vanilla game state queries. Most code should call <see cref="M:StardewValley.GameStateQuery.CheckConditions(System.String,StardewValley.Delegates.GameStateQueryContext)" /> instead of using these directly.</summary>
	public static class DefaultResolvers
	{
		/// <summary>Get whether any of the given conditions match.</summary>
		/// <remarks>The query arguments must be passed as quoted arguments. For example, <c>ANY "SEASON Winter" "SEASON Spring, DAY_OF_WEEK Friday"</c> is true if (a) it's winter or (b) it's a spring Friday.</remarks>
		/// /// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ANY(string[] query, GameStateQueryContext context)
		{
			return Helpers.AnyArgMatches(query, 1, (string value) => CheckConditions(value, context));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool DATE_RANGE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGetEnum<Season>(query, 1, out var value, out var error, "Season minSeason") || !ArgUtility.TryGetInt(query, 2, out var value2, out error, "int minDayOfMonth") || !ArgUtility.TryGetInt(query, 3, out var value3, out error, "int minYear") || !ArgUtility.TryGetOptionalEnum(query, 4, out var value4, out error, Season.Winter, "Season maxSeason") || !ArgUtility.TryGetOptionalInt(query, 5, out var value5, out error, 28, "int maxDayOfMonth") || !ArgUtility.TryGetOptionalInt(query, 6, out var value6, out error, int.MaxValue, "int maxYear"))
			{
				return Helpers.ErrorResult(query, error);
			}
			int daysPlayed = WorldDate.GetDaysPlayed(value3, value, value2);
			int num = ((value6 != int.MaxValue) ? WorldDate.GetDaysPlayed(value6, value4, value5) : int.MaxValue);
			int totalDays = Game1.Date.TotalDays;
			if (totalDays >= daysPlayed)
			{
				return totalDays <= num;
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool SEASON_DAY(string[] query, GameStateQueryContext context)
		{
			for (int i = 1; i < query.Length; i += 2)
			{
				if (!ArgUtility.TryGetEnum<Season>(query, i, out var value, out var error, "Season season") || !ArgUtility.TryGetInt(query, i + 1, out var value2, out error, "int day"))
				{
					return Helpers.ErrorResult(query, error);
				}
				if (Game1.season == value && Game1.dayOfMonth == value2)
				{
					return true;
				}
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool DAY_OF_MONTH(string[] query, GameStateQueryContext context)
		{
			return Helpers.AnyArgMatches(query, 1, delegate(string rawDay)
			{
				if (int.TryParse(rawDay, out var result))
				{
					return Game1.dayOfMonth == result;
				}
				if (rawDay.EqualsIgnoreCase("even"))
				{
					return Game1.dayOfMonth % 2 == 0;
				}
				if (rawDay.EqualsIgnoreCase("odd"))
				{
					return Game1.dayOfMonth % 2 == 1;
				}
				Helpers.ErrorResult(query, "'" + rawDay + "' isn't a valid day of month");
				return (bool?)null;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool DAY_OF_WEEK(string[] query, GameStateQueryContext context)
		{
			return Helpers.AnyArgMatches(query, 1, delegate(string rawDay)
			{
				if (!WorldDate.TryGetDayOfWeekFor(rawDay, out var dayOfWeek))
				{
					Helpers.ErrorResult(query, "'" + rawDay + "' isn't a valid day of week");
					return (bool?)null;
				}
				return (Game1.Date.DayOfWeek == dayOfWeek) ? new bool?(true) : new bool?(false);
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool DAYS_PLAYED(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGetInt(query, 1, out var value, out var error, "int minDaysPlayed") || !ArgUtility.TryGetOptionalInt(query, 2, out var value2, out error, int.MaxValue, "int maxDaysPlayed"))
			{
				return Helpers.ErrorResult(query, error);
			}
			uint daysPlayed = Game1.stats.DaysPlayed;
			if (daysPlayed >= value)
			{
				return daysPlayed <= value2;
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_GREEN_RAIN_DAY(string[] query, GameStateQueryContext context)
		{
			WorldDate worldDate = new WorldDate(Game1.Date);
			worldDate.TotalDays++;
			return Utility.isGreenRainDay(worldDate.DayOfMonth, worldDate.Season);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_FESTIVAL_DAY(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGetOptional(query, 1, out var value, out var error, "any", allowBlank: false, "string locationContextId") || !ArgUtility.TryGetOptionalInt(query, 2, out var value2, out error, 0, "int dayOffset"))
			{
				return Helpers.ErrorResult(query, error);
			}
			switch (value?.ToLower())
			{
			case null:
			case "any":
				value = null;
				break;
			case "here":
			case "target":
				value = Helpers.RequireLocation(value, context.Location).GetLocationContextId();
				break;
			}
			int num = (Game1.Date.TotalDays + value2) % 112;
			return Utility.isFestivalDay(season: (Season)(num / 28), day: num % 28 + 1, locationContext: value);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_PASSIVE_FESTIVAL_OPEN(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string festivalId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Utility.IsPassiveFestivalOpen(value);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_PASSIVE_FESTIVAL_TODAY(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string festivalId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Utility.IsPassiveFestivalDay(value);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool SEASON(string[] query, GameStateQueryContext context)
		{
			for (int i = 1; i < query.Length; i++)
			{
				if (!ArgUtility.TryGetEnum<Season>(query, i, out var value, out var error, "Season season"))
				{
					return Helpers.ErrorResult(query, error);
				}
				if (Game1.season == value)
				{
					return true;
				}
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool YEAR(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGetInt(query, 1, out var value, out var error, "int minYear") || !ArgUtility.TryGetOptionalInt(query, 2, out var value2, out error, int.MaxValue, "int maxYear"))
			{
				return Helpers.ErrorResult(query, error);
			}
			int year = Game1.year;
			if (year >= value)
			{
				return year <= value2;
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool TIME(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGetInt(query, 1, out var value, out var error, "int minTime") || !ArgUtility.TryGetOptionalInt(query, 2, out var value2, out error, int.MaxValue, "int maxTime"))
			{
				return Helpers.ErrorResult(query, error);
			}
			int timeOfDay = Game1.timeOfDay;
			if (timeOfDay >= value)
			{
				return timeOfDay <= value2;
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		[OtherNames(new string[] { "EVENT_ID" })]
		public static bool IS_EVENT(string[] query, GameStateQueryContext context)
		{
			Event @event = Game1.CurrentEvent;
			if (@event != null)
			{
				if (query.Length != 1)
				{
					return Helpers.AnyArgMatches(query, 1, (string eventId) => eventId == @event.id);
				}
				return true;
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool CAN_BUILD_CABIN(string[] query, GameStateQueryContext context)
		{
			int numberBuildingsConstructed = Game1.GetNumberBuildingsConstructed("Cabin");
			if (Game1.IsMasterGame)
			{
				return numberBuildingsConstructed < Game1.CurrentPlayerLimit - 1;
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool CAN_BUILD_FOR_CABINS(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string buildingType"))
			{
				return Helpers.ErrorResult(query, error);
			}
			int numberBuildingsConstructed = Game1.GetNumberBuildingsConstructed("Cabin");
			return Game1.GetNumberBuildingsConstructed(value) < numberBuildingsConstructed + 1;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool BUILDINGS_CONSTRUCTED(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string locationFilter") || !ArgUtility.TryGetOptional(query, 2, out var value2, out error, "All", allowBlank: true, "string buildingType") || !ArgUtility.TryGetOptionalInt(query, 3, out var value3, out error, 1, "int minCount") || !ArgUtility.TryGetOptionalInt(query, 4, out var value4, out error, int.MaxValue, "int maxCount") || !ArgUtility.TryGetOptionalBool(query, 5, out var value5, out error, defaultValue: false, "bool includeUnderConstruction"))
			{
				return Helpers.ErrorResult(query, error);
			}
			bool flag = value.EqualsIgnoreCase("All");
			bool flag2 = value2.EqualsIgnoreCase("All");
			GameLocation location = context.Location;
			if (!flag)
			{
				location = Helpers.GetLocation(value, location);
				if (location == null)
				{
					return Helpers.ErrorResult(query, "required index 2 has value '" + value + "', which doesn't match an existing location name or one of the special keys (All, Here, or Target)");
				}
			}
			int num = ((!flag) ? (flag2 ? location.getNumberBuildingsConstructed(value5) : location.getNumberBuildingsConstructed(value2, value5)) : (flag2 ? Game1.GetNumberBuildingsConstructed(value5) : Game1.GetNumberBuildingsConstructed(value2, value5)));
			if (num >= value3)
			{
				return num <= value4;
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool FARM_CAVE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var _, out var error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			string caveType;
			switch (Game1.MasterPlayer.caveChoice.Value)
			{
			case 1:
				caveType = "Bats";
				break;
			case 2:
				caveType = "Mushrooms";
				break;
			default:
				caveType = "None";
				break;
			}
			return Helpers.AnyArgMatches(query, 1, (string rawCaveType) => rawCaveType.EqualsIgnoreCase(caveType));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool FARM_NAME(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGetRemainder(query, 1, out var value, out var error, ' ', "string farmName"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return context.Player.farmName.Value.EqualsIgnoreCase(value);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool FARM_TYPE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var _, out var error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			string farmTypeId = Game1.GetFarmTypeID();
			string farmTypeKey = Game1.GetFarmTypeKey();
			return Helpers.AnyArgMatches(query, 1, (string rawFarmType) => rawFarmType.EqualsIgnoreCase(farmTypeId) || rawFarmType.EqualsIgnoreCase(farmTypeKey));
		}

		/// <summary>Get whether all the Lost Books for the library have been found.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool FOUND_ALL_LOST_BOOKS(string[] query, GameStateQueryContext context)
		{
			return Game1.netWorldState.Value.LostBooksFound >= 21;
		}

		/// <summary>Get whether an explicit target location is set.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool HAS_TARGET_LOCATION(string[] query, GameStateQueryContext context)
		{
			return context.ExplicitTargetLocation != null;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_COMMUNITY_CENTER_COMPLETE(string[] query, GameStateQueryContext context)
		{
			if (Game1.MasterPlayer.hasCompletedCommunityCenter())
			{
				return !Game1.MasterPlayer.mailReceived.Contains("JojaMember");
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_CUSTOM_FARM_TYPE(string[] query, GameStateQueryContext context)
		{
			return Game1.whichFarm == 7;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_HOST(string[] query, GameStateQueryContext context)
		{
			return Game1.IsMasterGame;
		}

		/// <summary>Get whether the <see cref="T:StardewValley.Locations.IslandNorth" /> bridge is fixed.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_ISLAND_NORTH_BRIDGE_FIXED(string[] query, GameStateQueryContext context)
		{
			return ((IslandNorth)Game1.getLocationFromName("IslandNorth"))?.bridgeFixed.Value ?? false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_JOJA_MART_COMPLETE(string[] query, GameStateQueryContext context)
		{
			return Utility.hasFinishedJojaRoute();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_MULTIPLAYER(string[] query, GameStateQueryContext context)
		{
			return Game1.IsMultiplayer;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool IS_VISITING_ISLAND(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string npcName"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Game1.IsVisitingIslandToday(value);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool LOCATION_ACCESSIBLE(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Game1.isLocationAccessible(location.NameOrUniqueName);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool LOCATION_CONTEXT(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error) || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			string contextId = location.GetLocationContextId();
			return Helpers.AnyArgMatches(query, 2, (string rawContextId) => rawContextId.EqualsIgnoreCase(contextId));
		}

		/// <summary>Get whether a location has a given value in its <see cref="F:StardewValley.GameData.Locations.LocationData.CustomFields" /> data field. This expects <c>LOCATION_HAS_CUSTOM_FIELD &lt;location&gt; &lt;field key&gt; [expected value]</c>, where omitting the expected value will just check if the field is defined.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool LOCATION_HAS_CUSTOM_FIELD(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error) || !ArgUtility.TryGet(query, 2, out var value, out error, allowBlank: false, "string fieldKey") || !ArgUtility.TryGetOptional(query, 3, out var value2, out error, null, allowBlank: true, "string value"))
			{
				return Helpers.ErrorResult(query, error);
			}
			bool flag = ArgUtility.HasIndex(query, 3);
			LocationData locationData = location?.GetData();
			if (locationData?.CustomFields != null && locationData.CustomFields.TryGetValue(value, out var value3))
			{
				if (flag)
				{
					return value3 == value2;
				}
				return true;
			}
			return false;
		}

		/// <summary>Get whether a location is indoors.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool LOCATION_IS_INDOORS(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			return (!(location?.IsOutdoors)) ?? false;
		}

		/// <summary>Get whether a location is outdoors.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool LOCATION_IS_OUTDOORS(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			return location?.IsOutdoors ?? false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool LOCATION_IS_MINES(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			return location is MineShaft;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool LOCATION_IS_SKULL_CAVE(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (location is MineShaft { mineLevel: >=121 } mineShaft)
			{
				return mineShaft.mineLevel != 77377;
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool LOCATION_NAME(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error) || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (location != null)
			{
				return Helpers.AnyArgMatches(query, 2, (string rawName) => rawName.EqualsIgnoreCase(location.Name));
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool LOCATION_UNIQUE_NAME(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error) || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (location != null)
			{
				return Helpers.AnyArgMatches(query, 2, (string rawName) => rawName.EqualsIgnoreCase(location.NameOrUniqueName));
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool LOCATION_SEASON(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			string season = Game1.GetSeasonKeyForLocation(location);
			return Helpers.AnyArgMatches(query, 2, (string rawSeason) => rawSeason.EqualsIgnoreCase(season));
		}

		/// <summary>Get whether a given number of items have been donated to the museum, optionally filtered by type. Format: <c>MUSEUM_DONATIONS &lt;min count&gt; [max count] [object type]+</c> or <c>MUSEUM_DONATIONS &lt;min count&gt; &lt;max count&gt; [object type]+</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool MUSEUM_DONATIONS(string[] query, GameStateQueryContext context)
		{
			int num = 3;
			if (!ArgUtility.TryGetInt(query, 1, out var value, out var error, "int minCount"))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (!ArgUtility.TryGetInt(query, 2, out var value2, out var _, "int maxCount"))
			{
				num = 2;
				value2 = int.MaxValue;
			}
			bool flag = query.Length > num;
			int num2 = 0;
			foreach (string value3 in Game1.netWorldState.Value.MuseumPieces.Values)
			{
				if (flag)
				{
					ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(value3);
					if (dataOrErrorItem.ObjectType == null)
					{
						continue;
					}
					for (int i = num; i < query.Length; i++)
					{
						if (dataOrErrorItem.ObjectType == query[i])
						{
							num2++;
							break;
						}
					}
				}
				else
				{
					num2++;
				}
			}
			if (num2 >= value)
			{
				return num2 <= value2;
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool WEATHER(string[] query, GameStateQueryContext context)
		{
			GameLocation location = context.Location;
			if (!Helpers.TryGetLocationArg(query, 1, ref location, out var error) || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (location != null)
			{
				string weather = location.GetWeather().Weather;
				return Helpers.AnyArgMatches(query, 2, (string rawWeather) => rawWeather.EqualsIgnoreCase(weather));
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool WORLD_STATE_FIELD(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string name") || !ArgUtility.TryGet(query, 2, out var value2, out error, allowBlank: true, "string expectedValue") || !ArgUtility.TryGetOptionalInt(query, 3, out var value3, out error, int.MaxValue, "int maxValue"))
			{
				return Helpers.ErrorResult(query, error);
			}
			PropertyInfo property = typeof(NetWorldState).GetProperty(value, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
			if ((object)property == null)
			{
				return false;
			}
			object value4 = property.GetValue(Game1.netWorldState.Value, null);
			if (value4 != null)
			{
				if (!(value4 is bool flag))
				{
					if (!(value4 is int num))
					{
						if (value4 is string str)
						{
							return str.EqualsIgnoreCase(value2);
						}
						return value4.ToString().EqualsIgnoreCase(value2);
					}
					if (int.TryParse(value2, out var result) && num >= result)
					{
						return num <= value3;
					}
					return false;
				}
				if (bool.TryParse(value2, out var result2))
				{
					return flag == result2;
				}
				return false;
			}
			return value2.EqualsIgnoreCase("null");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool WORLD_STATE_ID(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var _, out var error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.AnyArgMatches(query, 1, (string worldStateId) => NetWorldState.checkAnywhereForWorldStateID(worldStateId));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool MINE_LOWEST_LEVEL_REACHED(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGetInt(query, 1, out var value, out var error, "int minLevel") || !ArgUtility.TryGetOptionalInt(query, 2, out var value2, out error, int.MaxValue, "int maxLevel"))
			{
				return Helpers.ErrorResult(query, error);
			}
			int lowestLevelReached = MineShaft.lowestLevelReached;
			if (lowestLevelReached >= value)
			{
				return lowestLevelReached <= value2;
			}
			return false;
		}

		/// <summary>Get whether a player has the given combat level, excluding buffs.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_BASE_COMBAT_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.combatLevel.Value);
		}

		/// <summary>Get whether a player has the given farming level, excluding buffs.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_BASE_FARMING_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.farmingLevel.Value);
		}

		/// <summary>Get whether a player has the given fishing level, excluding buffs.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_BASE_FISHING_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.fishingLevel.Value);
		}

		/// <summary>Get whether a player has the given foraging level, excluding buffs.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_BASE_FORAGING_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.foragingLevel.Value);
		}

		/// <summary>Get whether a player has the given luck level, excluding buffs.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_BASE_LUCK_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.luckLevel.Value);
		}

		/// <summary>Get whether a player has the given mining level, excluding buffs.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_BASE_MINING_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.miningLevel.Value);
		}

		/// <summary>Get whether a player has the given combat level, including any buffs which increase it.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_COMBAT_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.CombatLevel);
		}

		/// <summary>Get whether a player has the given farming level, including any buffs which increase it.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_FARMING_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.FarmingLevel);
		}

		/// <summary>Get whether a player has the given fishing level, including any buffs which increase it.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_FISHING_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.FishingLevel);
		}

		/// <summary>Get whether a player has the given foraging level, including any buffs which increase it.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_FORAGING_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.ForagingLevel);
		}

		/// <summary>Get whether a player has the given luck level, including any buffs which increase it.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_LUCK_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.LuckLevel);
		}

		/// <summary>Get whether a player has the given mining level, including any buffs which increase it.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_MINING_LEVEL(string[] query, GameStateQueryContext context)
		{
			return Helpers.PlayerSkillLevelImpl(query, context.Player, (Farmer target) => target.MiningLevel);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_CURRENT_MONEY(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetInt(query, 2, out var minAmount, out error, "int minAmount") || !ArgUtility.TryGetOptionalInt(query, 3, out var maxAmount, out error, int.MaxValue, "int maxAmount"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				int money = target.Money;
				return money >= minAmount && money <= maxAmount;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_FARMHOUSE_UPGRADE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetInt(query, 2, out var minUpgradeLevel, out error, "int minUpgradeLevel") || !ArgUtility.TryGetOptionalInt(query, 3, out var maxUpgradeLevel, out error, int.MaxValue, "int maxUpgradeLevel"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				int houseUpgradeLevel = target.HouseUpgradeLevel;
				return houseUpgradeLevel >= minUpgradeLevel && houseUpgradeLevel <= maxUpgradeLevel;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_GENDER(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var value2, out error, allowBlank: true, "string genderName"))
			{
				return Helpers.ErrorResult(query, error);
			}
			bool isMale = value2.EqualsIgnoreCase("Male");
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => target.IsMale == isMale);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_ACHIEVEMENT(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetInt(query, 2, out var achievementId, out error, "int achievementId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => target.achievements.Contains(achievementId));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_ALL_ACHIEVEMENTS(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				foreach (int key in Game1.achievements.Keys)
				{
					if (!target.achievements.Contains(key))
					{
						return false;
					}
				}
				return true;
			});
		}

		/// <summary>Get whether a player has a given buff currently active.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_BUFF(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "string buffId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string id) => target.buffs.IsApplied(id)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_CAUGHT_FISH(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var value2, out error, allowBlank: true, "string fishId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			value2 = ItemRegistry.QualifyItemId(value2);
			if (value2 != null)
			{
				return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string id) => target.fishCaught.ContainsKey(id)));
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_CONVERSATION_TOPIC(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "string topic"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string id) => target.activeDialogueEvents.ContainsKey(id)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_CRAFTING_RECIPE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetRemainder(query, 2, out var recipeName, out error, ' ', "string recipeName"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => target.craftingRecipes.ContainsKey(recipeName));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_COOKING_RECIPE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetRemainder(query, 2, out var recipeName, out error, ' ', "string recipeName"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => target.cookingRecipes.ContainsKey(recipeName));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_DIALOGUE_ANSWER(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "string responseId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string id) => target.DialogueQuestionsAnswered.Contains(id)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_HEARD_SONG(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "string songId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string id) => target.songsHeard.Contains(id)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_ITEM(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var itemId, out error, allowBlank: true, "string itemId") || !ArgUtility.TryGetOptionalInt(query, 3, out var minCount, out error, 1, "int minCount") || !ArgUtility.TryGetOptionalInt(query, 4, out var maxCount, out error, int.MaxValue, "int maxCount"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				switch (itemId)
				{
				case "73":
				case "(O)73":
				{
					int goldenWalnuts = Game1.netWorldState.Value.GoldenWalnuts;
					if (goldenWalnuts >= minCount)
					{
						return goldenWalnuts <= maxCount;
					}
					return false;
				}
				case "858":
				case "(O)858":
				{
					int qiGems = target.QiGems;
					if (qiGems >= minCount)
					{
						return qiGems <= maxCount;
					}
					return false;
				}
				default:
					if (maxCount != int.MaxValue)
					{
						int num = target.Items.CountId(itemId);
						if (num >= minCount)
						{
							return num <= maxCount;
						}
						return false;
					}
					return target.Items.ContainsId(itemId, minCount);
				}
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_MAIL(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var mailId, out error, allowBlank: true, "string mailId") || !ArgUtility.TryGetOptional(query, 3, out var value2, out error, "any", allowBlank: true, "string rawType"))
			{
				return Helpers.ErrorResult(query, error);
			}
			string type = value2?.ToLower();
			switch (type)
			{
			default:
				return Helpers.ErrorResult(query, "unknown mail type '" + type + "'; expected 'Mailbox', 'Tomorrow', 'Received', or 'Any'");
			case "mailbox":
			case "tomorrow":
			case "received":
			case "any":
				return Helpers.WithPlayer(context.Player, value, (Farmer target) => type switch
				{
					"mailbox" => target.mailbox.Contains(mailId), 
					"tomorrow" => target.mailForTomorrow.Contains(mailId), 
					"received" => target.mailReceived.Contains(mailId), 
					_ => target.hasOrWillReceiveMail(mailId), 
				});
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_PROFESSION(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetInt(query, 2, out var professionId, out error, "int professionId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => target.professions.Contains(professionId));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_RUN_TRIGGER_ACTION(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "string actionId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string id) => target.triggerActionsRun.Contains(id)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_SECRET_NOTE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetInt(query, 2, out var noteId, out error, "int noteId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => target.secretNotesSeen.Contains(noteId));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_SEEN_EVENT(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "string eventId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string id) => target.eventsSeen.Contains(id)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_TOWN_KEY(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => target.HasTownKey);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_TRASH_CAN_LEVEL(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetInt(query, 2, out var minLevel, out error, "int minLevel") || !ArgUtility.TryGetOptionalInt(query, 3, out var maxLevel, out error, int.MaxValue, "int maxLevel"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				int trashCanLevel = target.trashCanLevel;
				return trashCanLevel >= minLevel && trashCanLevel <= maxLevel;
			});
		}

		/// <summary>Get whether a target player has a trinket equipped.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_TRINKET(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				foreach (Trinket trinketItem in target.trinketItems)
				{
					if (trinketItem != null)
					{
						for (int i = 2; i < query.Length; i++)
						{
							if (trinketItem.QualifiedItemId == query[i] || trinketItem.ItemId == query[i])
							{
								return true;
							}
						}
					}
				}
				return false;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_LOCATION_CONTEXT(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				string contextId = target.currentLocation?.GetLocationContextId();
				return Helpers.AnyArgMatches(query, 2, (string rawContextId) => rawContextId.EqualsIgnoreCase(contextId));
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_LOCATION_NAME(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string rawName) => rawName.EqualsIgnoreCase(target.currentLocation?.Name)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_LOCATION_UNIQUE_NAME(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string rawName) => rawName.EqualsIgnoreCase(target.currentLocation?.NameOrUniqueName)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_MOD_DATA(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var key, out error, allowBlank: true, "string key") || !ArgUtility.TryGet(query, 3, out var value2, out error, allowBlank: true, "string value"))
			{
				return Helpers.ErrorResult(query, error);
			}
			string value3;
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => target.modData.TryGetValue(key, out value3) && value3.EqualsIgnoreCase(value2));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_MONEY_EARNED(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetInt(query, 2, out var minAmount, out error, "int minAmount") || !ArgUtility.TryGetOptionalInt(query, 3, out var maxAmount, out error, int.MaxValue, "int maxAmount"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				uint totalMoneyEarned = target.totalMoneyEarned;
				return totalMoneyEarned >= minAmount && totalMoneyEarned <= maxAmount;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_SHIPPED_BASIC_ITEM(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var itemId, out error, allowBlank: true, "string itemId") || !ArgUtility.TryGetOptionalInt(query, 3, out var minShipped, out error, 1, "int minShipped") || !ArgUtility.TryGetOptionalInt(query, 4, out var maxShipped, out error, int.MaxValue, "int maxShipped"))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (ItemRegistry.IsQualifiedItemId(itemId))
			{
				ItemMetadata metadata = ItemRegistry.GetMetadata(itemId);
				if (metadata?.TypeIdentifier != "(O)")
				{
					return false;
				}
				itemId = metadata.LocalItemId;
			}
			int value2;
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => target.basicShipped.TryGetValue(itemId, out value2) && value2 >= minShipped && value2 <= maxShipped);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_SPECIAL_ORDER_ACTIVE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "string orderId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string id) => target.team.SpecialOrderActive(id)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_SPECIAL_ORDER_RULE_ACTIVE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "string ruleId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string id) => target.team.SpecialOrderRuleActive(id)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_SPECIAL_ORDER_COMPLETE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "string orderId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string id) => target.team.completedSpecialOrders.Contains(id)));
		}

		/// <summary>Check the number of monsters killed by the player. Format: <c>PLAYER_KILLED_MONSTERS &lt;player&gt; &lt;monster name&gt;+ [min] [max]</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_KILLED_MONSTERS(string[] query, GameStateQueryContext context)
		{
			List<string> monsterNames = new List<string>();
			int min = 1;
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "playerKey"))
			{
				return Helpers.ErrorResult(query, error);
			}
			int num = 2;
			while (num < query.Length)
			{
				string text = query[num];
				num++;
				if (int.TryParse(text, out var result))
				{
					min = result;
					break;
				}
				monsterNames.Add(text);
			}
			if (!ArgUtility.TryGetOptionalInt(query, num, out var max, out error, int.MaxValue, "max"))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (monsterNames.Count == 0)
			{
				return Helpers.ErrorResult(query, "must specify at least one monster name to count");
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				int num2 = 0;
				foreach (string item in monsterNames)
				{
					num2 += target.stats.getMonstersKilled(item);
				}
				return num2 >= min && num2 <= max;
			});
		}

		/// <summary>Get whether the given player has a minimum value for a <see cref="P:StardewValley.Game1.stats" /> field returned by <see cref="M:StardewValley.Stats.Get(System.String)" />.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_STAT(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var statName, out error, allowBlank: true, "string statName") || !ArgUtility.TryGetInt(query, 3, out var minValue, out error, "int minValue") || !ArgUtility.TryGetOptionalInt(query, 4, out var maxValue, out error, int.MaxValue, "int maxValue"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				uint num = target.stats.Get(statName);
				return num >= minValue && num <= maxValue;
			});
		}

		/// <summary>Get whether the given player has ever visited a location name.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_VISITED_LOCATION(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string locationName) => target.locationsVisited.Contains(locationName)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_FRIENDSHIP_POINTS(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var npcName, out error, allowBlank: true, "string npcName") || !ArgUtility.TryGetInt(query, 3, out var minPoints, out error, "int minPoints") || !ArgUtility.TryGetOptionalInt(query, 4, out var maxPoints, out error, int.MaxValue, "int maxPoints"))
			{
				return Helpers.ErrorResult(query, error);
			}
			bool isAny = npcName.EqualsIgnoreCase("Any");
			bool isAnyDateable = !isAny && npcName.EqualsIgnoreCase("AnyDateable");
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				if (isAny)
				{
					return target.hasAFriendWithFriendshipPoints(minPoints, datablesOnly: false, maxPoints);
				}
				if (isAnyDateable)
				{
					return target.hasAFriendWithFriendshipPoints(minPoints, datablesOnly: true, maxPoints);
				}
				int friendshipLevelForNPC = target.getFriendshipLevelForNPC(npcName);
				return friendshipLevelForNPC >= minPoints && friendshipLevelForNPC <= maxPoints;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_CHILDREN(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGetOptionalInt(query, 2, out var minCount, out error, 1, "int minCount") || !ArgUtility.TryGetOptionalInt(query, 3, out var maxCount, out error, int.MaxValue, "int maxCount"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				int childrenCount = target.getChildrenCount();
				return childrenCount >= minCount && childrenCount <= maxCount;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_PET(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => target.hasPet());
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HEARTS(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var npcName, out error, allowBlank: true, "string npcName") || !ArgUtility.TryGetInt(query, 3, out var minHearts, out error, "int minHearts") || !ArgUtility.TryGetOptionalInt(query, 4, out var maxHearts, out error, int.MaxValue, "int maxHearts"))
			{
				return Helpers.ErrorResult(query, error);
			}
			bool isAny = npcName.EqualsIgnoreCase("Any");
			bool isAnyDateable = !isAny && npcName.EqualsIgnoreCase("AnyDateable");
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				if (isAny)
				{
					return target.hasAFriendWithHeartLevel(minHearts, datablesOnly: false, maxHearts);
				}
				if (isAnyDateable)
				{
					return target.hasAFriendWithHeartLevel(minHearts, datablesOnly: true, maxHearts);
				}
				int friendshipHeartLevelForNPC = target.getFriendshipHeartLevelForNPC(npcName);
				return friendshipHeartLevelForNPC >= minHearts && friendshipHeartLevelForNPC <= maxHearts;
			});
		}

		/// <summary>Get whether a player has ever talked to an NPC. Format: <c>PLAYER_HAS_MET &lt;player&gt; &lt;npc&gt;</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_HAS_MET(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "string npcName"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string name) => target.friendshipData.ContainsKey(name)));
		}

		/// <summary>Get a player's relationship status with an NPC. Format: <c>PLAYER_NPC_RELATIONSHIP &lt;player&gt; &lt;npc&gt; &lt;type&gt;+</c>, where the type is any combination of 'Friendly', 'Dating', 'Engaged', 'Roommate', 'Married' or 'Divorced'.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_NPC_RELATIONSHIP(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: false, "string playerKey") || !ArgUtility.TryGet(query, 2, out var npcName, out error, allowBlank: false, "string npcName"))
			{
				return Helpers.ErrorResult(query, error);
			}
			string[] relationships = new string[query.Length - 3];
			string value2;
			for (int i = 3; i < query.Length && ArgUtility.TryGet(query, i, out value2, out error, allowBlank: false, "string type"); i++)
			{
				value2 = value2.ToLower();
				relationships[i - 3] = value2;
				switch (value2)
				{
				case "friendly":
				case "roommate":
				case "dating":
				case "engaged":
				case "married":
				case "divorced":
					continue;
				}
				return Helpers.ErrorResult(query, "unknown relationship type '" + value2 + "'; expected one of Friendly, Roommate, Dating, Engaged, Married, or Divorced");
			}
			if (relationships.Length == 0)
			{
				return Helpers.ErrorResult(query, ArgUtility.GetMissingRequiredIndexError(query, 3, "type"));
			}
			bool anyNpc = npcName.EqualsIgnoreCase("Any");
			return Helpers.WithPlayer(context.Player, value, delegate(Farmer target)
			{
				Friendship value3;
				if (anyNpc)
				{
					foreach (Friendship value4 in target.friendshipData.Values)
					{
						if (IsMatch(value4, relationships))
						{
							return true;
						}
					}
				}
				else if (target.friendshipData.TryGetValue(npcName, out value3) && IsMatch(value3, relationships))
				{
					return true;
				}
				return false;
			});
			bool IsMatch(Friendship friendship, string[] relationshipTypes)
			{
				foreach (string text in relationshipTypes)
				{
					switch (text)
					{
					case "friendly":
						if (friendship.Status == FriendshipStatus.Friendly)
						{
							return true;
						}
						break;
					case "roommate":
						if (friendship.Status == FriendshipStatus.Married && friendship.RoommateMarriage)
						{
							return true;
						}
						break;
					case "dating":
						if (friendship.Status == FriendshipStatus.Dating)
						{
							return true;
						}
						break;
					case "engaged":
						if (friendship.Status == FriendshipStatus.Engaged)
						{
							return true;
						}
						break;
					case "married":
						if (friendship.Status == FriendshipStatus.Married && !friendship.RoommateMarriage)
						{
							return true;
						}
						break;
					case "divorced":
						if (friendship.Status == FriendshipStatus.Divorced && !friendship.RoommateMarriage)
						{
							return true;
						}
						break;
					default:
						return Helpers.ErrorResult(query, "unhandled relationship type '" + text + "'");
					}
				}
				return false;
			}
		}

		/// <summary>Get a player's relationship status with another player. Format: <c>PLAYER_PLAYER_RELATIONSHIP &lt;player 1&gt; &lt;player 2&gt; &lt;type&gt;+</c>, where the type is 'Engaged' or 'Married'.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_PLAYER_RELATIONSHIP(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: false, "string playerKey") || !ArgUtility.TryGet(query, 2, out var targetPlayerKey, out error, allowBlank: false, "string targetPlayerKey") || !ArgUtility.TryGet(query, 3, out var type, out error, allowBlank: false, "string type"))
			{
				return Helpers.ErrorResult(query, error);
			}
			type = type.ToLower();
			switch (type)
			{
			default:
				return Helpers.ErrorResult(query, "unknown relationship type '" + type + "'; expected one of Friendly, Engaged, or Married");
			case "friendly":
			case "engaged":
			case "married":
				return Helpers.WithPlayer(context.Player, value, (Farmer fromPlayer) => Helpers.WithPlayer(context.Player, targetPlayerKey, delegate(Farmer toPlayer)
				{
					FriendshipStatus status = fromPlayer.team.GetFriendship(fromPlayer.UniqueMultiplayerID, toPlayer.UniqueMultiplayerID).Status;
					switch (type)
					{
					case "friendly":
						if (status != FriendshipStatus.Engaged)
						{
							return status != FriendshipStatus.Married;
						}
						return false;
					case "engaged":
						return status == FriendshipStatus.Engaged;
					case "married":
						return status == FriendshipStatus.Married;
					default:
						return Helpers.ErrorResult(query, "unhandled relationship type '" + type + "'");
					}
				}));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool PLAYER_PREFERRED_PET(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey") || !ArgUtility.TryGet(query, 2, out var _, out error, allowBlank: true, "_"))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.WithPlayer(context.Player, value, (Farmer target) => Helpers.AnyArgMatches(query, 2, (string rawPetId) => rawPetId.EqualsIgnoreCase(target.whichPetType)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool RANDOM(string[] query, GameStateQueryContext context)
		{
			return Helpers.RandomImpl(context.Random, query, 1);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool SYNCED_CHOICE(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string interval") || !ArgUtility.TryGet(query, 2, out var value2, out error, allowBlank: true, "string key") || !ArgUtility.TryGetInt(query, 3, out var value3, out error, "int min") || !ArgUtility.TryGetInt(query, 4, out var value4, out error, "int max") || !Utility.TryCreateIntervalRandom(value, value2, out var random, out error))
			{
				return Helpers.ErrorResult(query, error);
			}
			string text = random.Next(value3, value4 + 1).ToString();
			for (int i = 5; i < query.Length; i++)
			{
				if (query[i] == text)
				{
					return true;
				}
			}
			return false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool SYNCED_RANDOM(string[] query, GameStateQueryContext context)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string interval") || !ArgUtility.TryGet(query, 2, out var value2, out error, allowBlank: true, "string key") || !Utility.TryCreateIntervalRandom(value, value2, out var random, out error))
			{
				return Helpers.ErrorResult(query, error);
			}
			return Helpers.RandomImpl(random, query, 3);
		}

		/// <summary>A custom variant of <see cref="M:StardewValley.GameStateQuery.DefaultResolvers.SYNCED_RANDOM(System.String[],StardewValley.Delegates.GameStateQueryContext)" /> with a set key and which applies a chance boost for each day after summer starts.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool SYNCED_SUMMER_RAIN_RANDOM(string[] query, GameStateQueryContext context)
		{
			Random random = Utility.CreateDaySaveRandom(Game1.hash.GetDeterministicHashCode("summer_rain_chance"));
			float chance = 0.12f + (float)Game1.dayOfMonth * 0.003f;
			return random.NextBool(chance);
		}

		/// <summary>Get whether the target item has all of the given context tags.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_CONTEXT_TAG(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (item == null)
			{
				return false;
			}
			for (int i = 2; i < query.Length; i++)
			{
				if (!item.HasContextTag(query[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Get whether the target item has one of the given categories.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_CATEGORY(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (item != null)
			{
				if (query.Length == 2)
				{
					return item.Category < -1;
				}
				for (int i = 2; i < query.Length; i++)
				{
					if (!ArgUtility.TryGetInt(query, i, out var value, out error, "int category"))
					{
						return Helpers.ErrorResult(query, error);
					}
					if (item.Category == value)
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>Get whether the item has an explicit category set in <c>Data/Objects</c>, ignoring categories assigned dynamically in code (e.g. for rings). These are often (but not always) special items like Secret Note or unimplemented items like Lumber. This is somewhat specialized.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_HAS_EXPLICIT_OBJECT_CATEGORY(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			return ObjectDataDefinition.HasExplicitCategory(ItemRegistry.GetData(item?.QualifiedItemId));
		}

		/// <summary>Get whether the target item has the given qualified or unqualified item ID.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_ID(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (item != null)
			{
				return Helpers.AnyArgMatches(query, 2, (string rawItemId) => rawItemId.EqualsIgnoreCase(item.QualifiedItemId) || rawItemId.EqualsIgnoreCase(item.ItemId));
			}
			return false;
		}

		/// <summary>Get whether the target item's qualified or unqualified item ID starts with the given prefix.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_ID_PREFIX(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (item != null)
			{
				return Helpers.AnyArgMatches(query, 2, (string prefix) => item.ItemId.StartsWithIgnoreCase(prefix) || item.QualifiedItemId.StartsWithIgnoreCase(prefix));
			}
			return false;
		}

		/// <summary>Get whether the target item has a numeric item ID within the given range.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_NUMERIC_ID(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error) || !ArgUtility.TryGetInt(query, 2, out var value, out error, "int minId") || !ArgUtility.TryGetOptionalInt(query, 3, out var value2, out error, int.MaxValue, "int maxId"))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (int.TryParse(item?.ItemId, out var result) && result >= value)
			{
				return result <= value2;
			}
			return false;
		}

		/// <summary>Get whether the target item has one of the given object types.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_OBJECT_TYPE(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			Object obj = item as Object;
			if (obj != null)
			{
				return Helpers.AnyArgMatches(query, 2, (string rawObjType) => rawObjType.EqualsIgnoreCase(obj.Type));
			}
			return false;
		}

		/// <summary>Get whether the target item has a price within the given range.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_PRICE(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error) || !ArgUtility.TryGetInt(query, 2, out var value, out error, "int minPrice") || !ArgUtility.TryGetOptionalInt(query, 3, out var value2, out error, int.MaxValue, "int maxPrice"))
			{
				return Helpers.ErrorResult(query, error);
			}
			int? num = item?.salePrice();
			if (num >= value)
			{
				return num <= value2;
			}
			return false;
		}

		/// <summary>Get whether the target item has a min quality level.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_QUALITY(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error) || !ArgUtility.TryGetInt(query, 2, out var value, out error, "int minQuality") || !ArgUtility.TryGetOptionalInt(query, 3, out var value2, out error, int.MaxValue, "int maxQuality"))
			{
				return Helpers.ErrorResult(query, error);
			}
			int? num = item?.Quality;
			if (num >= value)
			{
				return num <= value2;
			}
			return false;
		}

		/// <summary>Get whether the target item has a min stack size (ignoring other stacks in the inventory).</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_STACK(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error) || !ArgUtility.TryGetInt(query, 2, out var value, out error, "int minStack") || !ArgUtility.TryGetOptionalInt(query, 3, out var value2, out error, int.MaxValue, "int maxStack"))
			{
				return Helpers.ErrorResult(query, error);
			}
			int? num = item?.Stack;
			if (num >= value)
			{
				return num <= value2;
			}
			return false;
		}

		/// <summary>Get whether the target item has the given item type.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_TYPE(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (item != null)
			{
				return Helpers.AnyArgMatches(query, 2, (string rawItemType) => rawItemType.EqualsIgnoreCase(item.TypeDefinitionId));
			}
			return false;
		}

		/// <summary>Get whether the target item has a min edibility level.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool ITEM_EDIBILITY(string[] query, GameStateQueryContext context)
		{
			if (!Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error) || !ArgUtility.TryGetOptionalInt(query, 2, out var value, out error, -299, "int minEdibility") || !ArgUtility.TryGetOptionalInt(query, 3, out var value2, out error, int.MaxValue, "int maxEdibility"))
			{
				return Helpers.ErrorResult(query, error);
			}
			if (item is Object obj && obj.Edibility >= value)
			{
				return obj.Edibility <= value2;
			}
			return false;
		}

		/// <summary>A condition that always passes.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool TRUE(string[] query, GameStateQueryContext context)
		{
			return true;
		}

		/// <summary>A condition that always fails.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.GameStateQueryDelegate" />
		public static bool FALSE(string[] query, GameStateQueryContext context)
		{
			return false;
		}
	}

	/// <summary>The supported game state queries and their resolvers.</summary>
	private static readonly Dictionary<string, GameStateQueryDelegate> QueryTypeLookup;

	/// <summary>Alternate names for game state queries (e.g. shorthand or acronyms).</summary>
	private static readonly Dictionary<string, string> Aliases;

	/// <summary>The <see cref="F:StardewValley.Game1.ticks" /> value when the cache should be reset.</summary>
	private static int NextClearCacheTick;

	/// <summary>The cache of parsed game state queries.</summary>
	private static readonly Dictionary<string, ParsedGameStateQuery[]> ParseCache;

	/// <summary>The query keys which check the season, like <c>LOCATION_SEASON</c> or <c>SEASON</c>.</summary>
	public static HashSet<string> SeasonQueryKeys;

	/// <summary>The query keys which are ignored when catching fish with the Magic Bait equipped.</summary>
	public static HashSet<string> MagicBaitIgnoreQueryKeys;

	/// <summary>Register the default game state queries, defined as <see cref="T:StardewValley.GameStateQuery.DefaultResolvers" /> methods.</summary>
	static GameStateQuery()
	{
		QueryTypeLookup = new Dictionary<string, GameStateQueryDelegate>(StringComparer.OrdinalIgnoreCase);
		Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		ParseCache = new Dictionary<string, ParsedGameStateQuery[]>();
		SeasonQueryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "LOCATION_SEASON", "SEASON" };
		MagicBaitIgnoreQueryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DAY_OF_MONTH", "DAY_OF_WEEK", "DAYS_PLAYED", "LOCATION_SEASON", "SEASON", "SEASON_DAY", "WEATHER", "TIME" };
		MethodInfo[] methods = typeof(DefaultResolvers).GetMethods(BindingFlags.Static | BindingFlags.Public);
		MethodInfo[] array = methods;
		foreach (MethodInfo methodInfo in array)
		{
			GameStateQueryDelegate queryDelegate = (GameStateQueryDelegate)Delegate.CreateDelegate(typeof(GameStateQueryDelegate), methodInfo);
			Register(methodInfo.Name, queryDelegate);
		}
		array = methods;
		foreach (MethodInfo methodInfo2 in array)
		{
			OtherNamesAttribute customAttribute = methodInfo2.GetCustomAttribute<OtherNamesAttribute>();
			if (customAttribute != null)
			{
				string[] aliases = customAttribute.Aliases;
				for (int j = 0; j < aliases.Length; j++)
				{
					RegisterAlias(aliases[j], methodInfo2.Name);
				}
			}
		}
	}

	/// <summary>Update the game state query tracking.</summary>
	internal static void Update()
	{
		if (Game1.ticks >= NextClearCacheTick)
		{
			if (ParseCache.Count > 50)
			{
				ParseCache.Clear();
			}
			NextClearCacheTick = Game1.ticks + 3600;
		}
	}

	/// <summary>Get whether a game state query exists.</summary>
	/// <param name="queryKey">The game state query key, like <c>SEASON</c>.</param>
	public static bool Exists(string queryKey)
	{
		if (queryKey == null)
		{
			return false;
		}
		if (!QueryTypeLookup.ContainsKey(queryKey))
		{
			return Aliases.ContainsKey(queryKey);
		}
		return true;
	}

	/// <summary>Register a game state query resolver.</summary>
	/// <param name="queryKey">The game state query key, like <c>SEASON</c>. This should only contain alphanumeric, underscore, and dot characters. For custom queries, this should be prefixed with your mod ID like <c>Example.ModId_QueryName</c>.</param>
	/// <param name="queryDelegate">The resolver which returns whether a given query matches in the current context.</param>
	/// <exception cref="T:System.ArgumentException">The <paramref name="queryKey" /> is null or whitespace-only.</exception>
	/// <exception cref="T:System.ArgumentNullException">The <paramref name="queryDelegate" /> is null.</exception>
	/// <exception cref="T:System.InvalidOperationException">The <paramref name="queryKey" /> is already registered.</exception>
	public static void Register(string queryKey, GameStateQueryDelegate queryDelegate)
	{
		queryKey = queryKey?.Trim();
		if (string.IsNullOrWhiteSpace(queryKey))
		{
			throw new ArgumentException("The query key can't be null or empty.", "queryKey");
		}
		if (QueryTypeLookup.ContainsKey(queryKey))
		{
			throw new InvalidOperationException("The query key '" + queryKey + "' is already registered.");
		}
		if (Aliases.TryGetValue(queryKey, out var value))
		{
			throw new InvalidOperationException($"The query key '{queryKey}' is already registered as an alias of '{value}'.");
		}
		QueryTypeLookup[queryKey] = queryDelegate ?? throw new ArgumentNullException("queryDelegate");
	}

	/// <summary>Register an alternate name for a game state query.</summary>
	/// <param name="alias">The alias to register. This should only contain alphanumeric, underscore, and dot characters. For custom queries, this should be prefixed with your mod ID like <c>Example.ModId_QueryName</c>.</param>
	/// <param name="queryKey">The game state query key to map it to, like <c>SEASON</c>. This should already be registered (e.g. via <see cref="M:StardewValley.GameStateQuery.Register(System.String,StardewValley.Delegates.GameStateQueryDelegate)" />).</param>
	/// <exception cref="T:System.ArgumentException">The <paramref name="alias" /> or <paramref name="queryKey" /> is null or whitespace-only.</exception>
	/// <exception cref="T:System.InvalidOperationException">The <paramref name="queryKey" /> is already registered.</exception>
	public static void RegisterAlias(string alias, string queryKey)
	{
		alias = alias?.Trim();
		if (string.IsNullOrWhiteSpace(alias))
		{
			throw new ArgumentException("The alias can't be null or empty.", "alias");
		}
		if (QueryTypeLookup.ContainsKey(alias))
		{
			throw new InvalidOperationException("The alias '" + alias + "' is already registered as a game state query.");
		}
		if (Aliases.TryGetValue(alias, out var value))
		{
			throw new InvalidOperationException($"The alias '{alias}' is already registered for '{value}'.");
		}
		if (string.IsNullOrWhiteSpace(queryKey))
		{
			throw new ArgumentException("The query key can't be null or empty.", "alias");
		}
		if (!QueryTypeLookup.ContainsKey(queryKey))
		{
			throw new InvalidOperationException($"The alias '{alias}' can't be registered for '{queryKey}' because there's no game state query with that name.");
		}
		Aliases[alias] = queryKey;
	}

	/// <summary>Get whether a set of game state queries matches in the current context.</summary>
	/// <param name="queryString">The game state queries to check as a comma-delimited string.</param>
	/// <param name="location">The location for which to check the query, or <c>null</c> to use the current location.</param>
	/// <param name="player">The player for which to check the query, or <c>null</c> to use the current player.</param>
	/// <param name="targetItem">The target item (e.g. machine output or tree fruit) for which to check queries, or <c>null</c> if not applicable.</param>
	/// <param name="inputItem">The input item (e.g. machine input) for which to check queries, or <c>null</c> if not applicable.</param>
	/// <param name="random">The RNG to use for randomization, or <c>null</c> to use <see cref="F:StardewValley.Game1.random" />.</param>
	/// <param name="ignoreQueryKeys">The query keys to ignore when checking conditions (like <c>LOCATION_SEASON</c>), or <c>null</c> to check all of them.</param>
	/// <returns>Returns whether the query matches.</returns>
	public static bool CheckConditions(string queryString, GameLocation location = null, Farmer player = null, Item targetItem = null, Item inputItem = null, Random random = null, HashSet<string> ignoreQueryKeys = null)
	{
		if (queryString != null && (queryString == null || queryString.Length != 0) && !(queryString == "TRUE"))
		{
			if (queryString == "FALSE")
			{
				return false;
			}
			GameStateQueryContext context = new GameStateQueryContext(location, player, targetItem, inputItem, random, ignoreQueryKeys);
			return CheckConditionsImpl(queryString, context);
		}
		return true;
	}

	/// <summary>Get whether a set of game state queries matches in the current context.</summary>
	/// <param name="queryString">The game state queries to check as a comma-delimited string.</param>
	/// <param name="context">The game state query context.</param>
	/// <returns>Returns whether the query matches.</returns>
	public static bool CheckConditions(string queryString, GameStateQueryContext context)
	{
		if (queryString != null && (queryString == null || queryString.Length != 0) && !(queryString == "TRUE"))
		{
			if (queryString == "FALSE")
			{
				return false;
			}
			return CheckConditionsImpl(queryString, context);
		}
		return true;
	}

	/// <summary>Get whether a game state query can never be true under any circumstance (e.g. <c>FALSE</c> or <c>!TRUE</c>).</summary>
	/// <param name="queryString">The game state queries to check as a comma-delimited string.</param>
	public static bool IsImmutablyFalse(string queryString)
	{
		if (queryString != null && (queryString == null || queryString.Length != 0) && !(queryString == "TRUE"))
		{
			if (queryString == "FALSE")
			{
				return true;
			}
			ParsedGameStateQuery[] array = Parse(queryString);
			for (int i = 0; i < array.Length; i++)
			{
				ParsedGameStateQuery parsedGameStateQuery = array[i];
				if (parsedGameStateQuery.Query.Length != 0)
				{
					string value = (parsedGameStateQuery.Negated ? "TRUE" : "FALSE");
					if (parsedGameStateQuery.Query[0].EqualsIgnoreCase(value))
					{
						return true;
					}
				}
			}
			return false;
		}
		return false;
	}

	/// <summary>Get whether a game state query can never be false under any circumstance (e.g. <c>TRUE</c>, <c>!FALSE</c>, or empty).</summary>
	/// <param name="queryString">The game state queries to check as a comma-delimited string.</param>
	public static bool IsImmutablyTrue(string queryString)
	{
		if (queryString != null && (queryString == null || queryString.Length != 0) && !(queryString == "TRUE"))
		{
			if (queryString == "FALSE")
			{
				return false;
			}
			ParsedGameStateQuery[] array = Parse(queryString);
			for (int i = 0; i < array.Length; i++)
			{
				ParsedGameStateQuery parsedGameStateQuery = array[i];
				if (parsedGameStateQuery.Query.Length != 0)
				{
					string value = (parsedGameStateQuery.Negated ? "FALSE" : "TRUE");
					if (!parsedGameStateQuery.Query[0].EqualsIgnoreCase(value))
					{
						return false;
					}
				}
			}
			return true;
		}
		return true;
	}

	/// <summary>Parse a raw query string into its component query data.</summary>
	/// <param name="queryString">The query string to parse.</param>
	/// <returns>Returns the parsed game state queries. This value is cached, so it should not be modified. If any part of the query string is invalid, this returns a single value containing the invalid query with the error property set.</returns>
	public static ParsedGameStateQuery[] Parse(string queryString)
	{
		if (!ParseCache.TryGetValue(queryString, out var value))
		{
			string[] array = SplitRaw(queryString);
			value = new ParsedGameStateQuery[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = ArgUtility.SplitBySpaceQuoteAware(array[i]);
				string text = array2[0];
				bool flag = text.StartsWith('!');
				if (flag)
				{
					text = (array2[0] = text.Substring(1));
				}
				if (Aliases.TryGetValue(text, out var value2))
				{
					text = value2;
					array2[0] = value2;
				}
				if (!QueryTypeLookup.TryGetValue(text, out var value3))
				{
					if (value.Length > 1)
					{
						value = new ParsedGameStateQuery[1];
					}
					value[0] = new ParsedGameStateQuery(negated: false, array2, null, "'" + text + "' isn't a known query or alias");
					break;
				}
				value[i] = new ParsedGameStateQuery(flag, array2, value3, null);
			}
			ParseCache[queryString] = value;
		}
		return value;
	}

	/// <summary>Split a query string into its top-level component queries without parsing them.</summary>
	/// <param name="queryString">The query string to split.</param>
	public static string[] SplitRaw(string queryString)
	{
		return ArgUtility.SplitQuoteAware(queryString, ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, keepQuotesAndEscapes: true);
	}

	/// <summary>Get whether a set of game state queries matches in the current context without short-circuiting immutable values like <c>TRUE</c>.</summary>
	/// <param name="queryString">The game state queries to check as a comma-delimited string.</param>
	/// <param name="context">The game state query context.</param>
	/// <returns>Returns whether the query matches.</returns>
	private static bool CheckConditionsImpl(string queryString, GameStateQueryContext context)
	{
		if (queryString == null)
		{
			return true;
		}
		ParsedGameStateQuery[] array = Parse(queryString);
		if (array.Length == 0)
		{
			return true;
		}
		if (array[0].Error != null)
		{
			return Helpers.ErrorResult(array[0].Query, array[0].Error);
		}
		ParsedGameStateQuery[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			ParsedGameStateQuery parsedGameStateQuery = array2[i];
			HashSet<string> ignoreQueryKeys = context.IgnoreQueryKeys;
			if (ignoreQueryKeys != null && ignoreQueryKeys.Contains(parsedGameStateQuery.Query[0]))
			{
				continue;
			}
			try
			{
				if (parsedGameStateQuery.Resolver(parsedGameStateQuery.Query, context) == parsedGameStateQuery.Negated)
				{
					return false;
				}
			}
			catch (Exception exception)
			{
				return Helpers.ErrorResult(parsedGameStateQuery.Query, "unhandled exception", exception);
			}
		}
		return true;
	}
}
