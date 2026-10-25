using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewValley.Pokemon;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Constants;
using StardewValley.Delegates;
using StardewValley.Events;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Characters;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.GameData.Weddings;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Network.NetEvents;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.Quests;
using StardewValley.SpecialOrders;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using xTile.Dimensions;
using xTile.Layers;

namespace StardewValley;

/// <summary>Provides general utility methods for the game code.</summary>
/// <remarks>See also <see cref="T:StardewValley.ItemRegistry" /> for working with item IDs.</remarks>
public class Utility
{
	/// <summary>Indicates the reasons a horse can't be summoned by a player.</summary>
	[Flags]
	public enum HorseWarpRestrictions
	{
		/// <summary>No reasons apply.</summary>
		None = 0,
		/// <summary>The player doesn't own a horse.</summary>
		NoOwnedHorse = 1,
		/// <summary>The player is indoors (horses can't be summoned to an indoors location).</summary>
		Indoors = 2,
		/// <summary>There's no room near the player to place the horse.</summary>
		NoRoom = 4,
		/// <summary>The player's horse is currently in use by another player.</summary>
		InUse = 8
	}

	public static Color[] PRISMATIC_COLORS = new Color[6]
	{
		Color.Red,
		new Color(255, 120, 0),
		new Color(255, 217, 0),
		Color.Lime,
		Color.Cyan,
		Color.Violet
	};

	public static Item recentlyDiscoveredMissingBasicShippedItem;

	public static readonly Vector2[] DirectionsTileVectors = new Vector2[4]
	{
		new Vector2(0f, -1f),
		new Vector2(1f, 0f),
		new Vector2(0f, 1f),
		new Vector2(-1f, 0f)
	};

	public static readonly Vector2[] DirectionsTileVectorsWithDiagonals = new Vector2[8]
	{
		new Vector2(0f, -1f),
		new Vector2(1f, -1f),
		new Vector2(1f, 0f),
		new Vector2(1f, 1f),
		new Vector2(0f, 1f),
		new Vector2(-1f, 1f),
		new Vector2(-1f, 0f),
		new Vector2(-1f, -1f)
	};

	public static readonly RasterizerState ScissorEnabled = new RasterizerState
	{
		ScissorTestEnable = true
	};

	public static Microsoft.Xna.Framework.Rectangle controllerMapSourceRect(Microsoft.Xna.Framework.Rectangle xboxSourceRect)
	{
		return xboxSourceRect;
	}

	public static List<Vector2> removeDuplicates(List<Vector2> list)
	{
		for (int i = 0; i < list.Count; i++)
		{
			for (int num = list.Count - 1; num >= 0; num--)
			{
				if (num != i && list[i].Equals(list[num]))
				{
					list.RemoveAt(num);
				}
			}
		}
		return list;
	}

	/// <summary>Get the reasons a horse can't be summoned to the player currently, if any.</summary>
	/// <param name="who">The player requesting a horse.</param>
	public static HorseWarpRestrictions GetHorseWarpRestrictionsForFarmer(Farmer who)
	{
		HorseWarpRestrictions horseWarpRestrictions = HorseWarpRestrictions.None;
		if (who.horseName.Value == null)
		{
			horseWarpRestrictions |= HorseWarpRestrictions.NoOwnedHorse;
		}
		GameLocation currentLocation = who.currentLocation;
		if (!currentLocation.IsOutdoors)
		{
			horseWarpRestrictions |= HorseWarpRestrictions.Indoors;
		}
		Point tilePoint = who.TilePoint;
		Microsoft.Xna.Framework.Rectangle position = new Microsoft.Xna.Framework.Rectangle(tilePoint.X * 64, tilePoint.Y * 64, 128, 64);
		if (currentLocation.isCollidingPosition(position, Game1.viewport, isFarmer: true, 0, glider: false, who))
		{
			horseWarpRestrictions |= HorseWarpRestrictions.NoRoom;
		}
		foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
		{
			if (onlineFarmer.mount != null && onlineFarmer.mount.getOwner() == who)
			{
				horseWarpRestrictions |= HorseWarpRestrictions.InUse;
				break;
			}
		}
		return horseWarpRestrictions;
	}

	/// <summary>Get the error message to show for a warp issue returned by <see cref="M:StardewValley.Utility.GetHorseWarpRestrictionsForFarmer(StardewValley.Farmer)" />.</summary>
	/// <param name="issue">The current issues preventing a warp, if any.</param>
	/// <returns>Returns the error message to display, or <c>null</c> if none apply.</returns>
	public static string GetHorseWarpErrorMessage(HorseWarpRestrictions issue)
	{
		if (issue.HasFlag(HorseWarpRestrictions.NoOwnedHorse))
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:HorseFlute_NoHorse");
		}
		if (issue.HasFlag(HorseWarpRestrictions.Indoors))
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:HorseFlute_InvalidLocation");
		}
		if (issue.HasFlag(HorseWarpRestrictions.NoRoom))
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:HorseFlute_NoClearance");
		}
		if (issue.HasFlag(HorseWarpRestrictions.InUse))
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:HorseFlute_InUse");
		}
		return null;
	}

	public static Microsoft.Xna.Framework.Rectangle ConstrainScissorRectToScreen(Microsoft.Xna.Framework.Rectangle scissor_rect)
	{
		if (scissor_rect.Top < 0)
		{
			int num = -scissor_rect.Top;
			scissor_rect.Height -= num;
			scissor_rect.Y += num;
		}
		if (scissor_rect.Bottom > Game1.viewport.Height)
		{
			int num2 = scissor_rect.Bottom - Game1.viewport.Height;
			scissor_rect.Height -= num2;
		}
		if (scissor_rect.Left < 0)
		{
			int num3 = -scissor_rect.Left;
			scissor_rect.Width -= num3;
			scissor_rect.X += num3;
		}
		if (scissor_rect.Right > Game1.viewport.Width)
		{
			int num4 = scissor_rect.Right - Game1.viewport.Width;
			scissor_rect.Width -= num4;
		}
		return scissor_rect;
	}

	public static double getRandomDouble(double min, double max, Random random = null)
	{
		if (random == null)
		{
			random = Game1.random;
		}
		double num = max - min;
		return random.NextDouble() * num + min;
	}

	public static Vector2 getRandom360degreeVector(float speed)
	{
		Vector2 position = new Vector2(0f, -1f);
		position = Vector2.Transform(position, Matrix.CreateRotationZ((float)getRandomDouble(0.0, Math.PI * 2.0)));
		position.Normalize();
		return position * speed;
	}

	public static Point Vector2ToPoint(Vector2 v)
	{
		return new Point((int)v.X, (int)v.Y);
	}

	public static Item getRaccoonSeedForCurrentTimeOfYear(Farmer who, Random r, int stackOverride = -1)
	{
		int num = r.Next(2, 4);
		while (r.NextDouble() < 0.1 + who.team.AverageDailyLuck())
		{
			num++;
		}
		Item item = null;
		Season season = Game1.season;
		if (Game1.dayOfMonth > ((season == Season.Spring) ? 23 : 20))
		{
			season = (Season)((int)(season + 1) % 4);
		}
		switch (season)
		{
		case Season.Spring:
			item = ItemRegistry.Create("(O)CarrotSeeds");
			break;
		case Season.Summer:
			item = ItemRegistry.Create("(O)SummerSquashSeeds");
			break;
		case Season.Fall:
			item = ItemRegistry.Create("(O)BroccoliSeeds");
			break;
		case Season.Winter:
			item = ItemRegistry.Create("(O)PowdermelonSeeds");
			break;
		}
		item.Stack = ((stackOverride == -1) ? num : stackOverride);
		return item;
	}

	public static Vector2 PointToVector2(Point p)
	{
		return new Vector2(p.X, p.Y);
	}

	public static int getStartTimeOfFestival()
	{
		if (Game1.weatherIcon == 1)
		{
			return Convert.ToInt32(ArgUtility.SplitBySpaceAndGet(Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + Game1.dayOfMonth)["conditions"].Split('/')[1], 0));
		}
		return -1;
	}

	public static bool doesMasterPlayerHaveMailReceivedButNotMailForTomorrow(string mailID)
	{
		if (Game1.MasterPlayer.mailReceived.Contains(mailID) || Game1.MasterPlayer.mailReceived.Contains(mailID + "%&NL&%"))
		{
			if (!Game1.MasterPlayer.mailForTomorrow.Contains(mailID))
			{
				return !Game1.MasterPlayer.mailForTomorrow.Contains(mailID + "%&NL&%");
			}
			return false;
		}
		return false;
	}

	/// <summary>Get whether there's a festival scheduled for today in any location.</summary>
	/// <remarks>This doesn't match passive festivals like the Night Market; see <see cref="M:StardewValley.Utility.IsPassiveFestivalDay" /> for those.</remarks>
	public static bool isFestivalDay()
	{
		return isFestivalDay(Game1.dayOfMonth, Game1.season, null);
	}

	/// <summary>Get whether there's a festival scheduled for today in the given location context.</summary>
	/// <param name="locationContext">The location context to check, usually matching a constant like <see cref="F:StardewValley.LocationContexts.DefaultId" />, or <c>null</c> for any context.</param>
	/// <inheritdoc cref="M:StardewValley.Utility.isFestivalDay" path="/remarks" />
	public static bool isFestivalDay(string locationContext)
	{
		return isFestivalDay(Game1.dayOfMonth, Game1.season, locationContext);
	}

	/// <summary>Get whether there's a festival scheduled on the given day in any location. This doesn't match passive festivals like the Night Market.</summary>
	/// <param name="day">The day of month to check.</param>
	/// <param name="season">The season key to check.</param>
	/// <inheritdoc cref="M:StardewValley.Utility.isFestivalDay" path="/remarks" />
	public static bool isFestivalDay(int day, Season season)
	{
		return isFestivalDay(day, season, null);
	}

	/// <summary>Get whether there's a festival scheduled on the given day and in the given location context. This doesn't match passive festivals like the Night Market.</summary>
	/// <param name="day">The day of month to check.</param>
	/// <param name="season">The season key to check.</param>
	/// <param name="locationContext">The location context to check, usually matching a constant like <see cref="F:StardewValley.LocationContexts.DefaultId" />, or <c>null</c> for any context.</param>
	/// <inheritdoc cref="M:StardewValley.Utility.isFestivalDay" path="/remarks" />
	public static bool isFestivalDay(int day, Season season, string locationContext)
	{
		string text = $"{getSeasonKey(season)}{day}";
		if (!DataLoader.Festivals_FestivalDates(Game1.temporaryContent).ContainsKey(text))
		{
			return false;
		}
		if (locationContext != null)
		{
			if (!Event.tryToLoadFestivalData(text, out var _, out var _, out var locationName, out var _, out var _))
			{
				return false;
			}
			GameLocation locationFromName = Game1.getLocationFromName(locationName);
			if (locationFromName == null)
			{
				return false;
			}
			if (locationFromName.GetLocationContextId() != locationContext)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>Perform an action for each location in the game.</summary>
	/// <param name="action">The action to perform for each location. This should return true (continue iterating) or false (stop).</param>
	/// <param name="includeInteriors">Whether to include instanced building interiors that aren't in <see cref="P:StardewValley.Game1.locations" /> directly.</param>
	/// <param name="includeGenerated">Whether to include temporary generated locations like mine or volcano dungeon levels.</param>
	public static void ForEachLocation(Func<GameLocation, bool> action, bool includeInteriors = true, bool includeGenerated = false)
	{
		GameLocation currentLocation = Game1.currentLocation;
		string text = currentLocation?.NameOrUniqueName;
		foreach (GameLocation location in Game1.locations)
		{
			GameLocation gameLocation = ((location.NameOrUniqueName == text && currentLocation != null) ? currentLocation : location);
			if (!action(gameLocation))
			{
				return;
			}
			if (!includeInteriors)
			{
				continue;
			}
			bool shouldContinue = true;
			gameLocation.ForEachInstancedInterior(delegate(GameLocation interior)
			{
				if (action(interior))
				{
					return true;
				}
				shouldContinue = false;
				return false;
			});
			if (!shouldContinue)
			{
				return;
			}
		}
		if (!includeGenerated)
		{
			return;
		}
		foreach (MineShaft activeMine in MineShaft.activeMines)
		{
			GameLocation arg = ((activeMine.NameOrUniqueName == text && currentLocation != null) ? currentLocation : activeMine);
			if (!action(arg))
			{
				return;
			}
		}
		foreach (VolcanoDungeon activeLevel in VolcanoDungeon.activeLevels)
		{
			GameLocation arg2 = ((activeLevel.NameOrUniqueName == text && currentLocation != null) ? currentLocation : activeLevel);
			if (!action(arg2))
			{
				break;
			}
		}
	}

	/// <summary>Perform an action for each building in the game.</summary>
	/// <param name="action">The action to perform for each building. This should return true (continue iterating) or false (stop).</param>
	/// <param name="ignoreUnderConstruction">Whether to ignore buildings which haven't been fully constructed yet.</param>
	public static void ForEachBuilding(Func<Building, bool> action, bool ignoreUnderConstruction = true)
	{
		ForEachLocation(delegate(GameLocation location)
		{
			foreach (Building building in location.buildings)
			{
				if ((!ignoreUnderConstruction || !building.isUnderConstruction()) && !action(building))
				{
					return false;
				}
			}
			return true;
		}, includeInteriors: false);
	}

	public static List<Pet> getAllPets()
	{
		List<Pet> list = new List<Pet>();
		foreach (NPC character in Game1.getFarm().characters)
		{
			if (character is Pet item)
			{
				list.Add(item);
			}
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			foreach (NPC character2 in getHomeOfFarmer(allFarmer).characters)
			{
				if (character2 is Pet item2)
				{
					list.Add(item2);
				}
			}
		}
		return list;
	}

	/// <summary>Perform an action for each non-playable character in the game (including villagers, horses, pets, monsters, player children, etc).</summary>
	/// <param name="action">The action to perform for each character. This should return true (continue iterating) or false (stop).</param>
	/// <param name="includeEventActors">Whether to match temporary event actors.</param>
	/// <remarks>See also <see cref="M:StardewValley.Utility.ForEachVillager(System.Func{StardewValley.NPC,System.Boolean},System.Boolean)" />.</remarks>
	public static void ForEachCharacter(Func<NPC, bool> action, bool includeEventActors = false)
	{
		ForEachLocation(delegate(GameLocation location)
		{
			foreach (NPC character in location.characters)
			{
				if ((includeEventActors || !character.EventActor) && !action(character))
				{
					return false;
				}
			}
			return true;
		}, includeInteriors: true, includeGenerated: true);
	}

	/// <summary>Perform an action for each villager NPC in the game.</summary>
	/// <param name="action">The action to perform for each character. This should return true (continue iterating) or false (stop).</param>
	/// <param name="includeEventActors">Whether to match temporary event actors.</param>
	/// <remarks>See also <see cref="M:StardewValley.Utility.ForEachCharacter(System.Func{StardewValley.NPC,System.Boolean},System.Boolean)" />.</remarks>
	public static void ForEachVillager(Func<NPC, bool> action, bool includeEventActors = false)
	{
		ForEachLocation(delegate(GameLocation location)
		{
			foreach (NPC character in location.characters)
			{
				if ((includeEventActors || !character.EventActor) && character.IsVillager && !action(character))
				{
					return false;
				}
			}
			return true;
		});
	}

	/// <summary>Perform an action for each building in the game.</summary>
	/// <typeparam name="TBuilding">The expected building type.</typeparam>
	/// <param name="action">The action to perform for each building. This should return true (continue iterating) or false (stop).</param>
	/// <param name="ignoreUnderConstruction">Whether to ignore buildings which haven't been fully constructed yet.</param>
	public static void ForEachBuilding<TBuilding>(Func<TBuilding, bool> action, bool ignoreUnderConstruction = true) where TBuilding : Building
	{
		ForEachLocation(delegate(GameLocation location)
		{
			foreach (Building building in location.buildings)
			{
				if (building is TBuilding val && (!ignoreUnderConstruction || !val.isUnderConstruction()) && !action(val))
				{
					return false;
				}
			}
			return true;
		}, includeInteriors: false);
	}

	/// <summary>Perform an action for each planted crop in the game.</summary>
	/// <param name="action">The action to perform for each crop. This should return true (continue iterating) or false (stop).</param>
	public static void ForEachCrop(Func<Crop, bool> action)
	{
		ForEachLocation(delegate(GameLocation location)
		{
			foreach (TerrainFeature value in location.terrainFeatures.Values)
			{
				Crop crop = (value as HoeDirt)?.crop;
				if (crop != null && !action(crop))
				{
					return false;
				}
			}
			foreach (Object value2 in location.objects.Values)
			{
				Crop crop2 = (value2 as IndoorPot)?.hoeDirt.Value?.crop;
				if (crop2 != null && !action(crop2))
				{
					return false;
				}
			}
			return true;
		});
	}

	/// <summary>Perform an action for each item in the game world, including items within items (e.g. in a chest or on a table), hats placed on children, items in player inventories, etc.</summary>
	/// <param name="action">The action to perform for each item. This should return true (continue iterating) or false (stop).</param>
	/// <returns>Returns whether to continue iterating if needed (i.e. returns false if the last <paramref name="action" /> call did).</returns>
	/// <remarks>See also <see cref="M:StardewValley.Utility.ForEachItemContext(StardewValley.Delegates.ForEachItemDelegate)" /> for more advanced scenarios like replacing items.</remarks>
	public static bool ForEachItem(Func<Item, bool> action)
	{
		return ForEachItemHelper.ForEachItemInWorld(Handle);
		bool Handle(in ForEachItemContext context)
		{
			return action(context.Item);
		}
	}

	/// <summary>Perform an action for each item in the game world, including items within items (e.g. in a chest or on a table), hats placed on children, items in player inventories, etc.</summary>
	/// <param name="handler">The action to perform for each item.</param>
	/// <returns>Returns whether to continue iterating if needed (i.e. returns false if the last <paramref name="handler" /> call did).</returns>
	/// <remarks>See also <see cref="M:StardewValley.Utility.ForEachItem(System.Func{StardewValley.Item,System.Boolean})" /> if you only need to iterate items.</remarks>
	public static bool ForEachItemContext(ForEachItemDelegate handler)
	{
		return ForEachItemHelper.ForEachItemInWorld(handler);
	}

	/// <summary>Perform an action for each item within a location, including items within items (e.g. in a chest or on a table), hats placed on children, items in player inventories, etc.</summary>
	/// <param name="location">The location whose items to iterate.</param>
	/// <param name="action">The action to perform for each item. This should return true (continue iterating) or false (stop).</param>
	/// <returns>Returns whether to continue iterating if needed (i.e. returns false if the last <paramref name="action" /> call did).</returns>
	/// <remarks>See also <see cref="M:StardewValley.Utility.ForEachItemContextIn(StardewValley.GameLocation,StardewValley.Delegates.ForEachItemDelegate)" /> for more advanced scenarios like replacing items.</remarks>
	public static bool ForEachItemIn(GameLocation location, Func<Item, bool> action)
	{
		return ForEachItemHelper.ForEachItemInLocation(location, Handle);
		bool Handle(in ForEachItemContext context)
		{
			return action(context.Item);
		}
	}

	/// <summary>Perform an action for each item within a location, including items within items (e.g. in a chest or on a table), hats placed on children, items in player inventories, etc.</summary>
	/// <param name="location">The location whose items to iterate.</param>
	/// <param name="handler">The action to perform for each item.</param>
	/// <returns>Returns whether to continue iterating if needed (i.e. returns false if the last <paramref name="handler" /> call did).</returns>
	/// <remarks>See also <see cref="M:StardewValley.Utility.ForEachItemIn(StardewValley.GameLocation,System.Func{StardewValley.Item,System.Boolean})" /> if you only need to iterate items.</remarks>
	public static bool ForEachItemContextIn(GameLocation location, ForEachItemDelegate handler)
	{
		return ForEachItemHelper.ForEachItemInLocation(location, handler);
	}

	public static int getNumObjectsOfIndexWithinRectangle(Microsoft.Xna.Framework.Rectangle r, string[] indexes, GameLocation location)
	{
		int num = 0;
		Vector2 zero = Vector2.Zero;
		for (int i = r.Y; i < r.Bottom + 1; i++)
		{
			zero.Y = i;
			for (int j = r.X; j < r.Right + 1; j++)
			{
				zero.X = j;
				if (!location.objects.TryGetValue(zero, out var value))
				{
					continue;
				}
				foreach (string text in indexes)
				{
					if (text == null || ItemRegistry.HasItemId(value, text))
					{
						num++;
						break;
					}
				}
			}
		}
		return num;
	}

	/// <summary>Try to parse a string as a valid enum value.</summary>
	/// <typeparam name="TEnum">The enum type.</typeparam>
	/// <param name="value">The raw value to parse. This is not case-sensitive.</param>
	/// <param name="parsed">The parsed enum value, if valid.</param>
	/// <returns>Returns whether the value was successfully parsed as an enum.</returns>
	public static bool TryParseEnum<TEnum>(string value, out TEnum parsed) where TEnum : struct
	{
		if (Enum.TryParse<TEnum>(value, ignoreCase: true, out parsed))
		{
			if (typeof(TEnum).IsEnumDefined(parsed))
			{
				return true;
			}
			if (typeof(TEnum).GetCustomAttribute<FlagsAttribute>() != null && !long.TryParse(parsed.ToString(), out var _))
			{
				return true;
			}
		}
		parsed = default(TEnum);
		return false;
	}

	/// <summary>Get an enum value if it's valid, else get a default value.</summary>
	/// <typeparam name="TEnum">The enum type.</typeparam>
	/// <param name="value">The unvalidated enum value.</param>
	/// <param name="defaultValue">The value to return if invalid.</param>
	/// <returns>Returns <paramref name="value" /> if it matches one of the enum constants, else <paramref name="defaultValue" />.</returns>
	public static TEnum GetEnumOrDefault<TEnum>(TEnum value, TEnum defaultValue) where TEnum : struct
	{
		if (!typeof(TEnum).IsEnumDefined(value))
		{
			return defaultValue;
		}
		return value;
	}

	/// <summary>Trim whitespace at the start and end of each line in the given text.</summary>
	/// <param name="text">The text whose lines to trim.</param>
	public static string TrimLines(string text)
	{
		text = text?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		string[] array = LegacyShims.SplitAndTrim(text, '\n');
		if (array.Length <= 1)
		{
			return text;
		}
		return string.Join("\n", array);
	}

	public static bool IsLegacyIdAbove(string itemId, int lowerBound)
	{
		if (int.TryParse(itemId, out var result))
		{
			return result > lowerBound;
		}
		return false;
	}

	public static bool IsLegacyIdBetween(string itemId, int lowerBound, int upperBound)
	{
		if (int.TryParse(itemId, out var result) && result >= lowerBound)
		{
			return result <= upperBound;
		}
		return false;
	}

	/// <summary>Find the best match for a search term based on fuzzy compare rules.</summary>
	/// <param name="query">The fuzzy search query to match.</param>
	/// <param name="terms">The terms from which to choose a match.</param>
	/// <returns>Returns the best match for the query, or <c>null</c> if no match was found.</returns>
	public static string fuzzySearch(string query, ICollection<string> terms)
	{
		int? num = null;
		string result = null;
		foreach (string term in terms)
		{
			int? num2 = fuzzyCompare(query, term);
			if (num2.HasValue && (!num.HasValue || num2 < num))
			{
				num = num2;
				result = term;
			}
		}
		return result;
	}

	/// <summary>Find all matches for a search term based on fuzzy compare rules.</summary>
	/// <param name="query">The fuzzy search query to match.</param>
	/// <param name="terms">The terms from which to choose a match.</param>
	/// <param name="sortByScore">Whether to sort the matching terms by score, in addition to alphabetically.</param>
	/// <returns>Returns all matches for the query, ordered by fuzzy match score and then by name.</returns>
	public static IEnumerable<string> fuzzySearchAll(string query, ICollection<string> terms, bool sortByScore = true)
	{
		if (!sortByScore)
		{
			return from term in terms
				where fuzzyCompare(query, term).HasValue
				orderby term.ToLowerInvariant()
				select term;
		}
		return from term in terms
			let score = fuzzyCompare(query, term)
			where score.HasValue
			orderby score.Value, term.ToLowerInvariant()
			select term;
	}

	/// <summary>Get whether a term is a fuzzy match for a search query.</summary>
	/// <param name="query">The fuzzy search query to match.</param>
	/// <param name="term">The actual value to compare against the query.</param>
	/// <returns>Returns the numeric match priority (where lower values are a better match), or <c>null</c> if the term doesn't match the query.</returns>
	public static int? fuzzyCompare(string query, string term)
	{
		if (query.Trim() == term.Trim())
		{
			return 0;
		}
		string text = FormatForFuzzySearch(query);
		string text2 = FormatForFuzzySearch(term);
		if (text == text2)
		{
			return 1;
		}
		if (text2.StartsWith(text))
		{
			return 2;
		}
		if (text2.Contains(text))
		{
			return 3;
		}
		return null;
		static string FormatForFuzzySearch(string value)
		{
			string text3 = value.Trim().ToLowerInvariant().Replace(" ", "");
			string text4 = text3.Replace("(", "").Replace(")", "").Replace("'", "")
				.Replace(".", "")
				.Replace("!", "")
				.Replace("?", "")
				.Replace("-", "");
			if (text4.Length != 0)
			{
				return text4;
			}
			return text3;
		}
	}

	public static Item fuzzyItemSearch(string query, int stack_count = 1, bool useLocalizedNames = false)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (IItemDataDefinition itemType in ItemRegistry.ItemTypes)
		{
			foreach (string allId in itemType.GetAllIds())
			{
				ParsedItemData data = itemType.GetData(allId);
				string key = (useLocalizedNames ? data.DisplayName : data.InternalName);
				if (!dictionary.ContainsKey(key))
				{
					dictionary[key] = itemType.Identifier + allId;
				}
			}
		}
		ParsedItemData data2 = ItemRegistry.GetData("(O)390");
		if (data2 != null)
		{
			string key2 = (useLocalizedNames ? data2.DisplayName : data2.InternalName);
			dictionary[key2] = "(O)390";
		}
		string text = fuzzySearch(query, dictionary.Keys);
		if (text != null)
		{
			return ItemRegistry.Create(dictionary[text], stack_count);
		}
		return null;
	}

	public static GameLocation fuzzyLocationSearch(string query)
	{
		Dictionary<string, GameLocation> name_bank = new Dictionary<string, GameLocation>();
		ForEachLocation(delegate(GameLocation location)
		{
			name_bank[location.NameOrUniqueName] = location;
			return true;
		});
		string text = fuzzySearch(query, name_bank.Keys);
		if (text == null)
		{
			return null;
		}
		return name_bank[text];
	}

	public static string AOrAn(string text)
	{
		if (text != null && text.Length > 0)
		{
			char c = text.ToLowerInvariant()[0];
			if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u')
			{
				if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.hu)
				{
					return "az";
				}
				return "an";
			}
		}
		return "a";
	}

	/// <summary>Get the default tile position where the player should be placed when they arrive in a location, if arriving from a warp that didn't specify a tile position.</summary>
	/// <param name="locationName">The <see cref="P:StardewValley.GameLocation.Name" /> value for the target location.</param>
	/// <param name="x">The default X tile position.</param>
	/// <param name="y">The default Y tile position.</param>
	public static void getDefaultWarpLocation(string locationName, ref int x, ref int y)
	{
		GameLocation locationFromName = Game1.getLocationFromName(locationName);
		if (locationFromName != null && locationFromName.TryGetMapPropertyAs("DefaultWarpLocation", out Point parsed, required: false))
		{
			x = parsed.X;
			y = parsed.Y;
			return;
		}
		if (locationFromName is Farm farm)
		{
			Point mainFarmHouseEntry = farm.GetMainFarmHouseEntry();
			if (mainFarmHouseEntry != Point.Zero)
			{
				x = mainFarmHouseEntry.X;
				y = mainFarmHouseEntry.Y;
			}
		}
		Point? point = GameLocation.GetData(locationName)?.DefaultArrivalTile;
		if (point.HasValue)
		{
			x = point.Value.X;
			y = point.Value.Y;
			return;
		}
		if (locationName != null)
		{
			int length = locationName.Length;
			if (length != 4)
			{
				if (length != 5)
				{
					if (length == 10 && locationName == "SlimeHutch")
					{
						x = 8;
						y = 18;
						return;
					}
				}
				else
				{
					char c = locationName[0];
					if (c != 'B')
					{
						if (c == 'C' && (locationName == "Coop2" || locationName == "Coop3"))
						{
							goto IL_018b;
						}
					}
					else if (locationName == "Barn2" || locationName == "Barn3")
					{
						goto IL_0182;
					}
				}
			}
			else
			{
				switch (locationName[0])
				{
				case 'B':
					break;
				case 'C':
					goto IL_011d;
				case 'F':
					if (!(locationName == "Farm"))
					{
						goto IL_01a3;
					}
					x = 64;
					y = 15;
					return;
				default:
					goto IL_01a3;
				}
				if (locationName == "Barn")
				{
					goto IL_0182;
				}
			}
		}
		goto IL_01a3;
		IL_011d:
		if (locationName == "Coop")
		{
			goto IL_018b;
		}
		goto IL_01a3;
		IL_018b:
		x = 2;
		y = 8;
		return;
		IL_01a3:
		if (locationFromName != null && locationFromName.TryGetMapProperty("Warp", out var propertyValue))
		{
			string[] array = propertyValue.Split(' ');
			Vector2 vector = recursiveFindOpenTileForCharacter(tileLocation: new Vector2(Convert.ToInt32(array[0]), Convert.ToInt32(array[1])), c: Game1.player, l: Game1.getLocationFromName(locationName), maxIterations: 10, allowOffMap: false);
			x = (int)vector.X;
			y = (int)vector.Y;
		}
		return;
		IL_0182:
		x = 11;
		y = 13;
	}

	public static FarmAnimal fuzzyAnimalSearch(string query)
	{
		List<FarmAnimal> animals = new List<FarmAnimal>();
		ForEachLocation(delegate(GameLocation location)
		{
			animals.AddRange(location.Animals.Values);
			return true;
		});
		Dictionary<string, FarmAnimal> dictionary = new Dictionary<string, FarmAnimal>();
		foreach (FarmAnimal item in animals)
		{
			dictionary[item.Name] = item;
		}
		string text = fuzzySearch(query, dictionary.Keys);
		if (text == null)
		{
			return null;
		}
		return dictionary[text];
	}

	public static NPC fuzzyCharacterSearch(string query, bool must_be_villager = true)
	{
		Dictionary<string, NPC> name_bank = new Dictionary<string, NPC>();
		ForEachCharacter(delegate(NPC character)
		{
			if (!must_be_villager || character.IsVillager)
			{
				name_bank[character.Name] = character;
			}
			return true;
		});
		string text = fuzzySearch(query, name_bank.Keys);
		if (text == null)
		{
			return null;
		}
		return name_bank[text];
	}

	public static Color GetPrismaticColor(int offset = 0, float speedMultiplier = 1f)
	{
		float num = 1500f;
		int num2 = ((int)((float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds * speedMultiplier / num) + offset) % PRISMATIC_COLORS.Length;
		int num3 = (num2 + 1) % PRISMATIC_COLORS.Length;
		float t = (float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds * speedMultiplier / num % 1f;
		return new Color
		{
			R = (byte)(Lerp((float)(int)PRISMATIC_COLORS[num2].R / 255f, (float)(int)PRISMATIC_COLORS[num3].R / 255f, t) * 255f),
			G = (byte)(Lerp((float)(int)PRISMATIC_COLORS[num2].G / 255f, (float)(int)PRISMATIC_COLORS[num3].G / 255f, t) * 255f),
			B = (byte)(Lerp((float)(int)PRISMATIC_COLORS[num2].B / 255f, (float)(int)PRISMATIC_COLORS[num3].B / 255f, t) * 255f),
			A = (byte)(Lerp((float)(int)PRISMATIC_COLORS[num2].A / 255f, (float)(int)PRISMATIC_COLORS[num3].A / 255f, t) * 255f)
		};
	}

	public static Color Get2PhaseColor(Color color1, Color color2, int offset = 0, float speedMultiplier = 1f, float timeOffset = 0f)
	{
		float num = 1500f;
		int num2 = ((int)((float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)timeOffset) * speedMultiplier / num) + offset) % 2;
		float t = (float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)timeOffset) * speedMultiplier / num % 1f;
		Color result = default(Color);
		Color color3 = ((num2 == 0) ? color1 : color2);
		Color color4 = ((num2 == 0) ? color2 : color1);
		result.R = (byte)(Lerp((float)(int)color3.R / 255f, (float)(int)color4.R / 255f, t) * 255f);
		result.G = (byte)(Lerp((float)(int)color3.G / 255f, (float)(int)color4.G / 255f, t) * 255f);
		result.B = (byte)(Lerp((float)(int)color3.B / 255f, (float)(int)color4.B / 255f, t) * 255f);
		result.A = (byte)(Lerp((float)(int)color3.A / 255f, (float)(int)color4.A / 255f, t) * 255f);
		return result;
	}

	public static bool IsNormalObjectAtParentSheetIndex(Item item, string itemId)
	{
		if (item.HasTypeObject() && item.GetType() == typeof(Object))
		{
			return item.ItemId == itemId;
		}
		return false;
	}

	public static Microsoft.Xna.Framework.Rectangle getSafeArea()
	{
		Microsoft.Xna.Framework.Rectangle titleSafeArea = Game1.game1.GraphicsDevice.Viewport.GetTitleSafeArea();
		if (Game1.game1.GraphicsDevice.GetRenderTargets().Length == 0)
		{
			float num = 1f / Game1.options.zoomLevel;
			if (Game1.uiMode)
			{
				num = 1f / Game1.options.uiScale;
			}
			titleSafeArea.X = (int)((float)titleSafeArea.X * num);
			titleSafeArea.Y = (int)((float)titleSafeArea.Y * num);
			titleSafeArea.Width = (int)((float)titleSafeArea.Width * num);
			titleSafeArea.Height = (int)((float)titleSafeArea.Height * num);
		}
		return titleSafeArea;
	}

	/// <summary>
	/// Return the adjusted renderPos such that bounds implied by renderSize
	/// is within the TitleSafeArea.
	///
	/// If it already is, renderPos is returned unmodified.
	/// </summary>
	public static Vector2 makeSafe(Vector2 renderPos, Vector2 renderSize)
	{
		int x = (int)renderPos.X;
		int y = (int)renderPos.Y;
		int width = (int)renderSize.X;
		int height = (int)renderSize.Y;
		makeSafe(ref x, ref y, width, height);
		return new Vector2(x, y);
	}

	public static void makeSafe(ref Vector2 position, int width, int height)
	{
		int x = (int)position.X;
		int y = (int)position.Y;
		makeSafe(ref x, ref y, width, height);
		position.X = x;
		position.Y = y;
	}

	public static void makeSafe(ref Microsoft.Xna.Framework.Rectangle bounds)
	{
		makeSafe(ref bounds.X, ref bounds.Y, bounds.Width, bounds.Height);
	}

	public static void makeSafe(ref int x, ref int y, int width, int height)
	{
		Microsoft.Xna.Framework.Rectangle safeArea = getSafeArea();
		if (x < safeArea.Left)
		{
			x = safeArea.Left;
		}
		if (y < safeArea.Top)
		{
			y = safeArea.Top;
		}
		if (x + width > safeArea.Right)
		{
			x = safeArea.Right - width;
		}
		if (y + height > safeArea.Bottom)
		{
			y = safeArea.Bottom - height;
		}
	}

	public static int makeSafeMarginY(int marginy)
	{
		Viewport viewport = Game1.game1.GraphicsDevice.Viewport;
		Microsoft.Xna.Framework.Rectangle safeArea = getSafeArea();
		int num = safeArea.Top - viewport.Bounds.Top;
		if (num > marginy)
		{
			marginy = num;
		}
		num = viewport.Bounds.Bottom - safeArea.Bottom;
		if (num > marginy)
		{
			marginy = num;
		}
		return marginy;
	}

	public static int CompareGameVersions(string version, string other_version, bool ignore_platform_specific = false)
	{
		string[] array = version.Split('.');
		string[] array2 = other_version.Split('.');
		for (int i = 0; i < Math.Max(array.Length, array2.Length); i++)
		{
			float result = 0f;
			float result2 = 0f;
			if (i < array.Length)
			{
				float.TryParse(array[i], out result);
			}
			if (i < array2.Length)
			{
				float.TryParse(array2[i], out result2);
			}
			if (result != result2 || (i == 2 && ignore_platform_specific))
			{
				return result.CompareTo(result2);
			}
		}
		return 0;
	}

	public static float getFarmerItemsShippedPercent(Farmer who = null)
	{
		if (who == null)
		{
			who = Game1.player;
		}
		recentlyDiscoveredMissingBasicShippedItem = null;
		int num = 0;
		int num2 = 0;
		foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
		{
			int category = allDatum.Category;
			if (category != -7 && category != -2 && Object.isPotentialBasicShipped(allDatum.ItemId, allDatum.Category, allDatum.ObjectType))
			{
				num2++;
				if (who.basicShipped.ContainsKey(allDatum.ItemId))
				{
					num++;
				}
				else if (recentlyDiscoveredMissingBasicShippedItem == null)
				{
					recentlyDiscoveredMissingBasicShippedItem = ItemRegistry.Create(allDatum.QualifiedItemId);
				}
			}
		}
		return (float)num / (float)num2;
	}

	public static bool hasFarmerShippedAllItems()
	{
		return getFarmerItemsShippedPercent() >= 1f;
	}

	public static NPC getTodaysBirthdayNPC()
	{
		NPC match = null;
		ForEachVillager(delegate(NPC n)
		{
			if (n.isBirthday())
			{
				match = n;
			}
			return match == null;
		});
		return match;
	}

	/// <summary>Create a <see cref="T:System.Random" /> instance using the save ID and days played as a seed.</summary>
	/// <param name="seedA">The first extra value to add to the RNG seed, if any.</param>
	/// <param name="seedB">The second extra value to add to the RNG seed, if any.</param>
	/// <param name="seedC">The third extra value to add to the RNG seed, if any.</param>
	public static Random CreateDaySaveRandom(double seedA = 0.0, double seedB = 0.0, double seedC = 0.0)
	{
		return CreateRandom(Game1.stats.DaysPlayed, Game1.uniqueIDForThisGame / 2, seedA, seedB, seedC);
	}

	/// <summary>Get an RNG seeded with the same value when called within the specified period.</summary>
	/// <param name="interval">The time interval within which the random seed should be consistent.</param>
	/// <param name="key">A key which identifies the random instance being created, if any. Instances with a different key will have a different seed.</param>
	/// <param name="random">The created RNG, if valid.</param>
	/// <param name="error">An error indicating why the RNG could not be created, if applicable.</param>
	/// <returns>Returns whether the interval is valid and the RNG was created.</returns>
	public static bool TryCreateIntervalRandom(string interval, string key, out Random random, out string error)
	{
		int num = ((key != null) ? Game1.hash.GetDeterministicHashCode(key) : 0);
		error = null;
		double seedC;
		switch (interval.ToLower())
		{
		case "tick":
			seedC = Game1.ticks;
			break;
		case "day":
			seedC = Game1.stats.DaysPlayed;
			break;
		case "season":
			seedC = Game1.hash.GetDeterministicHashCode(Game1.currentSeason + Game1.year);
			break;
		case "year":
			seedC = Game1.hash.GetDeterministicHashCode("year" + Game1.year);
			break;
		default:
			error = "invalid interval '" + interval + "'; expected one of 'tick', 'day', 'season', or 'year'";
			random = null;
			return false;
		}
		random = CreateRandom(num, Game1.uniqueIDForThisGame, seedC);
		return true;
	}

	/// <summary>Create a <see cref="T:System.Random" /> instance which safely combines the given seed values.</summary>
	/// <param name="seedA">The first seed value to combine.</param>
	/// <param name="seedB">The second seed value to combine.</param>
	/// <param name="seedC">The third seed value to combine.</param>
	/// <param name="seedD">The fourth seed value to combine.</param>
	/// <param name="seedE">The fifth seed value to combine.</param>
	public static Random CreateRandom(double seedA, double seedB = 0.0, double seedC = 0.0, double seedD = 0.0, double seedE = 0.0)
	{
		return new Random(CreateRandomSeed(seedA, seedB, seedC, seedD, seedE));
	}

	/// <summary>Safely combine seed values for use as a <see cref="T:System.Random" /> seed.</summary>
	/// <param name="seedA">The first seed value to combine.</param>
	/// <param name="seedB">The second seed value to combine.</param>
	/// <param name="seedC">The third seed value to combine.</param>
	/// <param name="seedD">The fourth seed value to combine.</param>
	/// <param name="seedE">The fifth seed value to combine.</param>
	public static int CreateRandomSeed(double seedA, double seedB, double seedC = 0.0, double seedD = 0.0, double seedE = 0.0)
	{
		if (Game1.UseLegacyRandom)
		{
			return (int)((seedA % 2147483647.0 + seedB % 2147483647.0 + seedC % 2147483647.0 + seedD % 2147483647.0 + seedE % 2147483647.0) % 2147483647.0);
		}
		return Game1.hash.GetDeterministicHashCode((int)(seedA % 2147483647.0), (int)(seedB % 2147483647.0), (int)(seedC % 2147483647.0), (int)(seedD % 2147483647.0), (int)(seedE % 2147483647.0));
	}

	/// <summary>Get a random entry from a dictionary.</summary>
	/// <typeparam name="TKey">The dictionary key type.</typeparam>
	/// <typeparam name="TValue">The dictionary value type.</typeparam>
	/// <param name="dictionary">The list whose entries to get.</param>
	/// <param name="key">The random entry's key, if found.</param>
	/// <param name="value">The random entry's value, if found.</param>
	/// <param name="random">The RNG to use, or <c>null</c> for <see cref="F:StardewValley.Game1.random" />.</param>
	/// <returns>Returns whether an entry was found.</returns>
	public static bool TryGetRandom<TKey, TValue>(IDictionary<TKey, TValue> dictionary, out TKey key, out TValue value, Random random = null)
	{
		if (dictionary == null || dictionary.Count == 0)
		{
			key = default(TKey);
			value = default(TValue);
			return false;
		}
		if (random == null)
		{
			random = Game1.random;
		}
		KeyValuePair<TKey, TValue> keyValuePair = dictionary.ElementAt(random.Next(dictionary.Count));
		key = keyValuePair.Key;
		value = keyValuePair.Value;
		return true;
	}

	/// <inheritdoc cref="M:StardewValley.Utility.TryGetRandom``2(System.Collections.Generic.IDictionary{``0,``1},``0@,``1@,System.Random)" />
	public static bool TryGetRandom<TKey, TValue, TField, TSerialDict, TSelf>(NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> dictionary, out TKey key, out TValue value, Random random = null) where TField : class, INetObject<INetSerializable>, new() where TSerialDict : IDictionary<TKey, TValue>, new() where TSelf : NetDictionary<TKey, TValue, TField, TSerialDict, TSelf>
	{
		if (dictionary == null || dictionary.Length == 0)
		{
			key = default(TKey);
			value = default(TValue);
			return false;
		}
		if (random == null)
		{
			random = Game1.random;
		}
		KeyValuePair<TKey, TValue> keyValuePair = dictionary.Pairs.ElementAt(random.Next(dictionary.Length));
		key = keyValuePair.Key;
		value = keyValuePair.Value;
		return true;
	}

	/// <inheritdoc cref="M:StardewValley.Utility.TryGetRandom``2(System.Collections.Generic.IDictionary{``0,``1},``0@,``1@,System.Random)" />
	public static bool TryGetRandom(OverlaidDictionary dictionary, out Vector2 key, out Object value, Random random = null)
	{
		if (dictionary == null || dictionary.Length == 0)
		{
			key = Vector2.Zero;
			value = null;
			return false;
		}
		if (random == null)
		{
			random = Game1.random;
		}
		KeyValuePair<Vector2, Object> keyValuePair = dictionary.Pairs.ElementAt(random.Next(dictionary.Length));
		key = keyValuePair.Key;
		value = keyValuePair.Value;
		return true;
	}

	/// <summary>Get a random entry from a list, ignoring specific values.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <param name="list">The values to choose from.</param>
	/// <param name="except">The values to ignore in the <paramref name="list" />.</param>
	/// <param name="random">The random number generator to use.</param>
	/// <param name="selected">The selected value.</param>
	/// <returns>Returns whether a value was selected.</returns>
	public static bool TryGetRandomExcept<T>(IList<T> list, ISet<T> except, Random random, out T selected)
	{
		if (list == null || list.Count == 0)
		{
			selected = default(T);
			return false;
		}
		if (except == null || except.Count == 0)
		{
			selected = random.ChooseFrom(list);
			return true;
		}
		T[] options = list.Except(except).ToArray();
		selected = random.ChooseFrom(options);
		return true;
	}

	public static string getRandomSingleTileFurniture(Random r)
	{
		return r.Next(3) switch
		{
			0 => "(F)" + r.Next(10) * 3, 
			1 => "(F)" + r.Next(1376, 1391), 
			_ => "(F)" + (r.Next(6) * 2 + 1391), 
		};
	}

	public static void improveFriendshipWithEveryoneInRegion(Farmer who, int amount, string region)
	{
		ForEachLocation(delegate(GameLocation l)
		{
			foreach (NPC character in l.characters)
			{
				if (character.GetData()?.HomeRegion == region && who.friendshipData.ContainsKey(character.Name))
				{
					who.changeFriendship(amount, character);
				}
			}
			return true;
		});
	}

	/// <summary>Get a random Winter Star gift which an NPC can give to players.</summary>
	/// <param name="who">The NPC giving the gift.</param>
	public static Item getGiftFromNPC(NPC who)
	{
		Random random = CreateRandom(Game1.uniqueIDForThisGame / 2, Game1.year, Game1.dayOfMonth, Game1.seasonIndex, who.TilePoint.X);
		List<Item> list = new List<Item>();
		CharacterData data = who.GetData();
		List<GenericSpawnItemDataWithCondition> winterStarGifts = data.WinterStarGifts;
		if (winterStarGifts != null && winterStarGifts.Count > 0)
		{
			ItemQueryContext context = new ItemQueryContext(Game1.currentLocation, Game1.player, random, "character '" + who.Name + "' > winter star gifts");
			foreach (GenericSpawnItemDataWithCondition entry in data.WinterStarGifts)
			{
				if (GameStateQuery.CheckConditions(entry.Condition, null, null, null, null, random))
				{
					Item item = ItemQueryResolver.TryResolveRandomItem(entry, context, avoidRepeat: false, null, null, null, delegate(string query, string error)
					{
						Game1.log.Error($"{who.Name} failed parsing item query '{query}' for winter star gift entry '{entry.Id}': {error}");
					});
					if (item != null)
					{
						list.Add(item);
					}
				}
			}
		}
		if (list.Count == 0)
		{
			if (who.Age == 2)
			{
				list.AddRange(new Item[4]
				{
					ItemRegistry.Create("(O)330"),
					ItemRegistry.Create("(O)103"),
					ItemRegistry.Create("(O)394"),
					ItemRegistry.Create("(O)" + random.Next(535, 538))
				});
			}
			else
			{
				list.AddRange(new Item[14]
				{
					ItemRegistry.Create("(O)608"),
					ItemRegistry.Create("(O)651"),
					ItemRegistry.Create("(O)611"),
					ItemRegistry.Create("(O)517"),
					ItemRegistry.Create("(O)466", 10),
					ItemRegistry.Create("(O)422"),
					ItemRegistry.Create("(O)392"),
					ItemRegistry.Create("(O)348"),
					ItemRegistry.Create("(O)346"),
					ItemRegistry.Create("(O)341"),
					ItemRegistry.Create("(O)221"),
					ItemRegistry.Create("(O)64"),
					ItemRegistry.Create("(O)60"),
					ItemRegistry.Create("(O)70")
				});
			}
		}
		return random.ChooseFrom(list);
	}

	public static NPC getTopRomanticInterest(Farmer who)
	{
		NPC topSpot = null;
		int highestFriendPoints = -1;
		ForEachVillager(delegate(NPC n)
		{
			if (who.friendshipData.ContainsKey(n.Name) && n.datable.Value && who.getFriendshipLevelForNPC(n.Name) > highestFriendPoints)
			{
				topSpot = n;
				highestFriendPoints = who.getFriendshipLevelForNPC(n.Name);
			}
			return true;
		});
		return topSpot;
	}

	public static Color getRandomRainbowColor(Random r = null)
	{
		return (r?.Next(8) ?? Game1.random.Next(8)) switch
		{
			0 => Color.Red, 
			1 => Color.Orange, 
			2 => Color.Yellow, 
			3 => Color.Lime, 
			4 => Color.Cyan, 
			5 => new Color(0, 100, 255), 
			6 => new Color(152, 96, 255), 
			7 => new Color(255, 100, 255), 
			_ => Color.White, 
		};
	}

	public static NPC getTopNonRomanticInterest(Farmer who)
	{
		NPC topSpot = null;
		int highestFriendPoints = -1;
		ForEachVillager(delegate(NPC n)
		{
			if (who.friendshipData.ContainsKey(n.Name) && !n.datable.Value && who.getFriendshipLevelForNPC(n.Name) > highestFriendPoints)
			{
				topSpot = n;
				highestFriendPoints = who.getFriendshipLevelForNPC(n.Name);
			}
			return true;
		});
		return topSpot;
	}

	/// <summary>Get which of a player's skills has the highest number of experience points.</summary>
	/// <param name="who">The player whose skills to check.</param>
	public static int getHighestSkill(Farmer who)
	{
		int num = 0;
		int result = 0;
		for (int i = 0; i < who.experiencePoints.Length; i++)
		{
			int num2 = who.experiencePoints[i];
			if (who.experiencePoints[i] > num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	public static int getNumberOfFriendsWithinThisRange(Farmer who, int minFriendshipPoints, int maxFriendshipPoints, bool romanceOnly = false)
	{
		int number = 0;
		ForEachVillager(delegate(NPC n)
		{
			int? num = who.tryGetFriendshipLevelForNPC(n.Name);
			if (num >= minFriendshipPoints && num.Value <= maxFriendshipPoints && (!romanceOnly || n.datable.Value))
			{
				number++;
			}
			return true;
		});
		return number;
	}

	public static bool highlightLuauSoupItems(Item i)
	{
		if (i is Object obj)
		{
			if ((obj.edibility.Value == -300 || obj.Category == -7) && !(obj.QualifiedItemId == "(O)789"))
			{
				return obj.QualifiedItemId == "(O)71";
			}
			return true;
		}
		return false;
	}

	public static bool highlightSmallObjects(Item i)
	{
		if (i is Object obj)
		{
			return !obj.bigCraftable.Value;
		}
		return false;
	}

	public static bool highlightSantaObjects(Item i)
	{
		if (!i.canBeTrashed() || !i.canBeGivenAsGift())
		{
			return false;
		}
		return highlightSmallObjects(i);
	}

	public static bool highlightShippableObjects(Item i)
	{
		return i?.canBeShipped() ?? false;
	}

	public static int getFarmerNumberFromFarmer(Farmer who)
	{
		if (who != null)
		{
			if (who.IsMainPlayer)
			{
				return 1;
			}
			int num = 2;
			foreach (Farmer item in from f in Game1.otherFarmers.Values
				orderby f.UniqueMultiplayerID
				where !f.IsMainPlayer
				select f)
			{
				if (item.UniqueMultiplayerID == who.UniqueMultiplayerID)
				{
					return num;
				}
				num++;
			}
		}
		return -1;
	}

	public static Farmer getFarmerFromFarmerNumber(int number)
	{
		if (number <= 1)
		{
			return Game1.MasterPlayer;
		}
		int num = 2;
		foreach (Farmer item in from f in Game1.otherFarmers.Values
			orderby f.UniqueMultiplayerID
			where !f.IsMainPlayer
			select f)
		{
			if (num == number)
			{
				return item;
			}
			num++;
		}
		return null;
	}

	public static string getLoveInterest(string who)
	{
		return who switch
		{
			"Haley" => "Alex", 
			"Sam" => "Penny", 
			"Alex" => "Haley", 
			"Penny" => "Sam", 
			"Leah" => "Elliott", 
			"Harvey" => "Maru", 
			"Maru" => "Harvey", 
			"Elliott" => "Leah", 
			"Abigail" => "Sebastian", 
			"Sebastian" => "Abigail", 
			"Emily" => "Shane", 
			"Shane" => "Emily", 
			_ => "", 
		};
	}

	public static string ParseGiftReveals(string str)
	{
		string text = str;
		try
		{
			while (true)
			{
				int num = str.IndexOf("%revealtaste");
				if (num < 0)
				{
					break;
				}
				int num2 = num + "%revealtaste".Length;
				for (int i = num2; i < str.Length; i++)
				{
					char c = str[i];
					if (char.IsWhiteSpace(c) || c == '#' || c == '%' || c == '$' || c == '{' || c == '^' || c == '*')
					{
						break;
					}
					num2 = i;
				}
				string text2 = str.Substring(num, num2 - num + 1);
				string[] array = text2.Split(':');
				if (array.Length == 3 && array[0] == "%revealtaste")
				{
					string text3 = array[1].Trim();
					NPC characterFromName = Game1.getCharacterFromName(text3);
					ItemMetadata metadata = ItemRegistry.GetMetadata(array[2].Trim());
					if (metadata == null)
					{
						Game1.log.Warn($"Failed to parse gift taste reveal '{text2}' in dialogue '{str}'. There is no item with that ID.");
					}
					else
					{
						Game1.player.revealGiftTaste(characterFromName?.Name ?? text3, metadata.LocalItemId);
					}
					str = str.Remove(num, text2.Length);
					continue;
				}
				int num3 = num + "%revealtaste".Length;
				int j = num + 1;
				if (j >= str.Length)
				{
					j = str.Length - 1;
				}
				for (; j < str.Length && (str[j] < '0' || str[j] > '9'); j++)
				{
				}
				string text4 = str.Substring(num3, j - num3);
				num3 = j;
				for (; j < str.Length && str[j] >= '0' && str[j] <= '9'; j++)
				{
				}
				string itemId = str.Substring(num3, j - num3);
				str = str.Remove(num, j - num);
				NPC characterFromName2 = Game1.getCharacterFromName(text4);
				Game1.player.revealGiftTaste(characterFromName2?.Name ?? text4, itemId);
			}
		}
		catch (Exception exception)
		{
			Game1.log.Error("Error parsing gift taste reveals in string '" + text + "'.", exception);
		}
		return str;
	}

	public static void Shuffle<T>(Random rng, List<T> list)
	{
		int count = list.Count;
		while (count > 1)
		{
			int index = rng.Next(count--);
			T value = list[count];
			list[count] = list[index];
			list[index] = value;
		}
	}

	public static void Shuffle<T>(Random rng, T[] array)
	{
		int num = array.Length;
		while (num > 1)
		{
			int num2 = rng.Next(num--);
			T val = array[num];
			array[num] = array[num2];
			array[num2] = val;
		}
	}

	/// <summary>Get the unique key for a season (one of <c>spring</c>, <c>summer</c>, <c>fall</c>, or <c>winter</c>).</summary>
	/// <param name="season">The season value.</param>
	public static string getSeasonKey(Season season)
	{
		return season switch
		{
			Season.Spring => "spring", 
			Season.Summer => "summer", 
			Season.Fall => "fall", 
			Season.Winter => "winter", 
			_ => season.ToString().ToLower(), 
		};
	}

	public static int getSeasonNumber(string whichSeason)
	{
		if (TryParseEnum<Season>(whichSeason, out var parsed))
		{
			return (int)parsed;
		}
		if (whichSeason.EqualsIgnoreCase("autumn"))
		{
			return 2;
		}
		return -1;
	}

	/// <summary>
	/// uses Game1.random so this will not be the same each time it's called in the same context.
	/// </summary>
	/// <param name="startTile"></param>
	/// <param name="number"></param>
	/// <returns></returns>
	public static List<Vector2> getPositionsInClusterAroundThisTile(Vector2 startTile, int number)
	{
		Queue<Vector2> queue = new Queue<Vector2>();
		List<Vector2> list = new List<Vector2>();
		Vector2 item = startTile;
		queue.Enqueue(item);
		while (list.Count < number)
		{
			item = queue.Dequeue();
			list.Add(item);
			if (!list.Contains(new Vector2(item.X + 1f, item.Y)))
			{
				queue.Enqueue(new Vector2(item.X + 1f, item.Y));
			}
			if (!list.Contains(new Vector2(item.X - 1f, item.Y)))
			{
				queue.Enqueue(new Vector2(item.X - 1f, item.Y));
			}
			if (!list.Contains(new Vector2(item.X, item.Y + 1f)))
			{
				queue.Enqueue(new Vector2(item.X, item.Y + 1f));
			}
			if (!list.Contains(new Vector2(item.X, item.Y - 1f)))
			{
				queue.Enqueue(new Vector2(item.X, item.Y - 1f));
			}
		}
		return list;
	}

	public static bool doesPointHaveLineOfSightInMine(GameLocation mine, Vector2 start, Vector2 end, int visionDistance)
	{
		if (Vector2.Distance(start, end) > (float)visionDistance)
		{
			return false;
		}
		foreach (Point item in GetPointsOnLine((int)start.X, (int)start.Y, (int)end.X, (int)end.Y))
		{
			if (mine.hasTileAt(item, "Buildings"))
			{
				return false;
			}
		}
		return true;
	}

	public static void addSprinklesToLocation(GameLocation l, int sourceXTile, int sourceYTile, int tilesWide, int tilesHigh, int totalSprinkleDuration, int millisecondsBetweenSprinkles, Color sprinkleColor, string sound = null, bool motionTowardCenter = false)
	{
		Microsoft.Xna.Framework.Rectangle r = new Microsoft.Xna.Framework.Rectangle(sourceXTile - tilesWide / 2, sourceYTile - tilesHigh / 2, tilesWide, tilesHigh);
		Random random = Game1.random;
		int num = totalSprinkleDuration / millisecondsBetweenSprinkles;
		for (int i = 0; i < num; i++)
		{
			Vector2 vector = getRandomPositionInThisRectangle(r, random) * 64f;
			l.temporarySprites.Add(new TemporaryAnimatedSprite(random.Next(10, 12), vector, sprinkleColor, 8, flipped: false, 50f)
			{
				layerDepth = 1f,
				delayBeforeAnimationStart = millisecondsBetweenSprinkles * i,
				interval = 100f,
				startSound = sound,
				motion = (motionTowardCenter ? getVelocityTowardPoint(vector, new Vector2(sourceXTile, sourceYTile) * 64f, Vector2.Distance(new Vector2(sourceXTile, sourceYTile) * 64f, vector) / 64f) : Vector2.Zero),
				xStopCoordinate = sourceXTile,
				yStopCoordinate = sourceYTile
			});
		}
	}

	public static void addRainbowStarExplosion(GameLocation l, Vector2 origin, int numStars)
	{
		List<TemporaryAnimatedSprite> list = new List<TemporaryAnimatedSprite>();
		float num = (float)Math.PI * 2f / (float)Math.Max(1, numStars - 1);
		Vector2 vector = new Vector2(0f, -4f);
		double num2 = Game1.random.NextDouble() * Math.PI * 2.0;
		for (int i = 0; i < numStars; i++)
		{
			list.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 640, 64, 64), origin + vector, flipped: false, 0.03f, GetPrismaticColor(Game1.random.Next(99999)))
			{
				motion = getVectorDirection(origin, origin + vector, normalize: true) * 0.06f * 150f,
				acceleration = -getVectorDirection(origin, origin + vector, normalize: true) * 0.06f * 6f,
				totalNumberOfLoops = 1,
				animationLength = 8,
				interval = 50f,
				drawAboveAlwaysFront = true,
				rotation = -(float)Math.PI / 2f - num * (float)i
			});
			vector.X = 4f * (float)Math.Sin((double)(num * (float)(i + 1)) + num2);
			vector.Y = 4f * (float)Math.Cos((double)(num * (float)(i + 1)) + num2);
		}
		list.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 320, 64, 64), origin + vector, flipped: false, 0.03f, Color.White)
		{
			totalNumberOfLoops = 1,
			animationLength = 8,
			interval = 60f,
			drawAboveAlwaysFront = true
		});
		l.temporarySprites.AddRange(list);
	}

	public static Vector2 getVectorDirection(Vector2 start, Vector2 finish, bool normalize = false)
	{
		Vector2 result = new Vector2(finish.X - start.X, finish.Y - start.Y);
		if (normalize)
		{
			result.Normalize();
		}
		return result;
	}

	public static TemporaryAnimatedSpriteList getStarsAndSpirals(GameLocation l, int sourceXTile, int sourceYTile, int tilesWide, int tilesHigh, int totalSprinkleDuration, int millisecondsBetweenSprinkles, Color sprinkleColor, string sound = null, bool motionTowardCenter = false)
	{
		Microsoft.Xna.Framework.Rectangle r = new Microsoft.Xna.Framework.Rectangle(sourceXTile - tilesWide / 2, sourceYTile - tilesHigh / 2, tilesWide, tilesHigh);
		Random random = CreateRandom(sourceXTile * 7, sourceYTile * 77, Game1.currentGameTime.TotalGameTime.TotalSeconds);
		int num = totalSprinkleDuration / millisecondsBetweenSprinkles;
		TemporaryAnimatedSpriteList temporaryAnimatedSpriteList = new TemporaryAnimatedSpriteList();
		for (int i = 0; i < num; i++)
		{
			Vector2 position = getRandomPositionInThisRectangle(r, random) * 64f;
			temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", random.NextBool() ? new Microsoft.Xna.Framework.Rectangle(359, 1437, 14, 14) : new Microsoft.Xna.Framework.Rectangle(377, 1438, 9, 9), position, flipped: false, 0.01f, sprinkleColor)
			{
				xPeriodic = true,
				xPeriodicLoopTime = random.Next(2000, 3000),
				xPeriodicRange = random.Next(-64, 64),
				motion = new Vector2(0f, -2f),
				rotationChange = (float)Math.PI / (float)random.Next(4, 64),
				delayBeforeAnimationStart = millisecondsBetweenSprinkles * i,
				layerDepth = 1f,
				scaleChange = 0.04f,
				scaleChangeChange = -0.0008f,
				scale = 4f
			});
		}
		return temporaryAnimatedSpriteList;
	}

	public static void addStarsAndSpirals(GameLocation l, int sourceXTile, int sourceYTile, int tilesWide, int tilesHigh, int totalSprinkleDuration, int millisecondsBetweenSprinkles, Color sprinkleColor, string sound = null, bool motionTowardCenter = false)
	{
		l.temporarySprites.AddRange(getStarsAndSpirals(l, sourceXTile, sourceYTile, tilesWide, tilesHigh, totalSprinkleDuration, millisecondsBetweenSprinkles, sprinkleColor, sound, motionTowardCenter));
	}

	public static Vector2 snapDrawPosition(Vector2 draw_position)
	{
		return new Vector2((int)draw_position.X, (int)draw_position.Y);
	}

	public static Vector2 clampToTile(Vector2 nonTileLocation)
	{
		nonTileLocation.X -= nonTileLocation.X % 64f;
		nonTileLocation.Y -= nonTileLocation.Y % 64f;
		return nonTileLocation;
	}

	public static float distance(float x1, float x2, float y1, float y2)
	{
		return (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
	}

	public static bool couldSeePlayerInPeripheralVision(Farmer player, Character c)
	{
		Point standingPixel = player.StandingPixel;
		Point standingPixel2 = c.StandingPixel;
		switch (c.FacingDirection)
		{
		case 0:
			if (standingPixel.Y < standingPixel2.Y + 32)
			{
				return true;
			}
			break;
		case 1:
			if (standingPixel.X > standingPixel2.X - 32)
			{
				return true;
			}
			break;
		case 2:
			if (standingPixel.Y > standingPixel2.Y - 32)
			{
				return true;
			}
			break;
		case 3:
			if (standingPixel.X < standingPixel2.X + 32)
			{
				return true;
			}
			break;
		}
		return false;
	}

	public static IEnumerable<Point> GetPointsOnLine(int x0, int y0, int x1, int y1)
	{
		return GetPointsOnLine(x0, y0, x1, y1, ignoreSwap: false);
	}

	public static List<Vector2> getBorderOfThisRectangle(Microsoft.Xna.Framework.Rectangle r)
	{
		List<Vector2> list = new List<Vector2>();
		for (int i = r.X; i < r.Right; i++)
		{
			list.Add(new Vector2(i, r.Y));
		}
		for (int j = r.Y + 1; j < r.Bottom; j++)
		{
			list.Add(new Vector2(r.Right - 1, j));
		}
		for (int num = r.Right - 2; num >= r.X; num--)
		{
			list.Add(new Vector2(num, r.Bottom - 1));
		}
		for (int num2 = r.Bottom - 2; num2 >= r.Y + 1; num2--)
		{
			list.Add(new Vector2(r.X, num2));
		}
		return list;
	}

	/// <summary>Get the closest valid monster within range of a pixel position, if any.</summary>
	/// <param name="location">The location whose monsters to search.</param>
	/// <param name="originPoint">The pixel position from which to find a nearby monster.</param>
	/// <param name="range">The maximum pixel distance from the <paramref name="originPoint" /> within which to match monsters.</param>
	/// <param name="ignoreUntargetables">Whether to ignore monsters which can't normally be targeted by the player.</param>
	/// <param name="match">If set, a callback which returns whether a matched monster is valid.</param>
	public static Monster findClosestMonsterWithinRange(GameLocation location, Vector2 originPoint, int range, bool ignoreUntargetables = false, Func<Monster, bool> match = null)
	{
		Monster result = null;
		float num = range + 1;
		foreach (NPC character in location.characters)
		{
			if (character is Monster monster && (!ignoreUntargetables || !(character is Spiker)) && (match == null || match(monster)))
			{
				float num2 = Vector2.Distance(originPoint, character.getStandingPosition());
				if (num2 <= (float)range && num2 < num && !monster.IsInvisible)
				{
					result = monster;
					num = num2;
				}
			}
		}
		return result;
	}

	public static Microsoft.Xna.Framework.Rectangle getTranslatedRectangle(Microsoft.Xna.Framework.Rectangle r, int xTranslate, int yTranslate = 0)
	{
		return translateRect(r, xTranslate, yTranslate);
	}

	public static Microsoft.Xna.Framework.Rectangle translateRect(Microsoft.Xna.Framework.Rectangle r, int xTranslate, int yTranslate = 0)
	{
		r.X += xTranslate;
		r.Y += yTranslate;
		return r;
	}

	public static Point getTranslatedPoint(Point p, int direction, int movementAmount)
	{
		return direction switch
		{
			0 => new Point(p.X, p.Y - movementAmount), 
			2 => new Point(p.X, p.Y + movementAmount), 
			1 => new Point(p.X + movementAmount, p.Y), 
			3 => new Point(p.X - movementAmount, p.Y), 
			_ => p, 
		};
	}

	public static Vector2 getTranslatedVector2(Vector2 p, int direction, float movementAmount)
	{
		return direction switch
		{
			0 => new Vector2(p.X, p.Y - movementAmount), 
			2 => new Vector2(p.X, p.Y + movementAmount), 
			1 => new Vector2(p.X + movementAmount, p.Y), 
			3 => new Vector2(p.X - movementAmount, p.Y), 
			_ => p, 
		};
	}

	public static IEnumerable<Point> GetPointsOnLine(int x0, int y0, int x1, int y1, bool ignoreSwap)
	{
		bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
		if (steep)
		{
			int num = x0;
			x0 = y0;
			y0 = num;
			num = x1;
			x1 = y1;
			y1 = num;
		}
		if (!ignoreSwap && x0 > x1)
		{
			int num2 = x0;
			x0 = x1;
			x1 = num2;
			num2 = y0;
			y0 = y1;
			y1 = num2;
		}
		int dx = x1 - x0;
		int dy = Math.Abs(y1 - y0);
		int error = dx / 2;
		int ystep = ((y0 < y1) ? 1 : (-1));
		int y2 = y0;
		for (int i = x0; i <= x1; i++)
		{
			yield return new Point(steep ? y2 : i, steep ? i : y2);
			error -= dy;
			if (error < 0)
			{
				y2 += ystep;
				error += dx;
			}
		}
	}

	public static Vector2 getRandomAdjacentOpenTile(Vector2 tile, GameLocation location)
	{
		List<Vector2> adjacentTileLocations = getAdjacentTileLocations(tile);
		int i = 0;
		int num = Game1.random.Next(adjacentTileLocations.Count);
		Vector2 vector = adjacentTileLocations[num];
		for (; i < 4; i++)
		{
			if (!location.IsTileBlockedBy(vector))
			{
				break;
			}
			num = (num + 1) % adjacentTileLocations.Count;
			vector = adjacentTileLocations[num];
		}
		if (i >= 4)
		{
			return Vector2.Zero;
		}
		return vector;
	}

	public static void CollectSingleItemOrShowChestMenu(Chest chest, object context = null)
	{
		int num = 0;
		Item item = null;
		IInventory items = chest.Items;
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] != null)
			{
				num++;
				if (num == 1)
				{
					item = items[i];
				}
				if (num == 2)
				{
					item = null;
					break;
				}
			}
		}
		if (num == 0)
		{
			return;
		}
		if (item != null)
		{
			int stack = item.Stack;
			if (Game1.player.addItemToInventory(item) == null)
			{
				Game1.playSound("coin");
				items.Remove(item);
				chest.clearNulls();
				return;
			}
			if (item.Stack != stack)
			{
				Game1.playSound("coin");
			}
		}
		Game1.activeClickableMenu = new ItemGrabMenu(items, reverseGrab: false, showReceivingMenu: true, InventoryMenu.highlightAllItems, chest.grabItemFromInventory, null, chest.grabItemFromChest, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: true, 1, null, -1, context);
	}

	/// <summary>Add the item to the player's inventory if there's room, and drop any remainder at their feet.</summary>
	/// <param name="item">The item to collect or drop.</param>
	/// <param name="direction">The direction in which to drop the item relative to the player, or <c>-1</c> to use their facing direction.</param>
	/// <returns>Returns <c>true</c> if the item item was fully added to their inventory, or <c>false</c> if any were dropped.</returns>
	public static bool CollectOrDrop(Item item, int direction)
	{
		if (item != null)
		{
			item = Game1.player.addItemToInventory(item);
			if (item != null)
			{
				if (direction != -1)
				{
					Game1.createItemDebris(item, Game1.player.getStandingPosition(), direction);
				}
				else
				{
					Game1.createItemDebris(item, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
				}
				return false;
			}
			return true;
		}
		return true;
	}

	public static bool CollectOrDrop(Item item)
	{
		return CollectOrDrop(item, -1);
	}

	public static List<string> getExes(Farmer farmer)
	{
		List<string> list = new List<string>();
		foreach (string key in farmer.friendshipData.Keys)
		{
			if (farmer.friendshipData[key].IsDivorced())
			{
				list.Add(key);
			}
		}
		return list;
	}

	public static void fixAllAnimals()
	{
		if (!Game1.IsMasterGame)
		{
			return;
		}
		List<GameLocation> animalLocations = new List<GameLocation>();
		HashSet<long> uniqueAnimals = new HashSet<long>();
		List<long> animalsToRemove = new List<long>();
		ForEachLocation(delegate(GameLocation f)
		{
			if (f.animals.Length == 0 && f.buildings.Count == 0)
			{
				return true;
			}
			animalLocations.Clear();
			animalLocations.Add(f);
			foreach (Building building in f.buildings)
			{
				GameLocation indoors = building.GetIndoors();
				if (indoors != null && indoors.animals.Length > 0)
				{
					animalLocations.Add(indoors);
				}
			}
			bool flag = false;
			bool flag2 = false;
			foreach (GameLocation item in animalLocations)
			{
				AnimalHouse animalHouse = item as AnimalHouse;
				animalsToRemove.Clear();
				foreach (KeyValuePair<long, NetRef<FarmAnimal>> item2 in item.animals.FieldDict)
				{
					if (item2.Value?.Value == null)
					{
						animalsToRemove.Add(item2.Key);
					}
					else
					{
						if (item2.Value.Value.home == null)
						{
							flag = true;
						}
						if (!uniqueAnimals.Add(item2.Value.Value.myID.Value))
						{
							animalsToRemove.Add(item2.Key);
						}
					}
				}
				flag2 = flag2 || animalsToRemove.Count > 0;
				foreach (long item3 in animalsToRemove)
				{
					long animalId = item.animals[item3].myID.Value;
					item.animals.Remove(item3);
					animalHouse?.animalsThatLiveHere.RemoveWhere((long id) => id == animalId);
				}
			}
			foreach (Building building2 in f.buildings)
			{
				if (building2.GetIndoors() is AnimalHouse animalHouse2)
				{
					foreach (long item4 in animalHouse2.animalsThatLiveHere)
					{
						FarmAnimal animal = getAnimal(item4);
						if (animal != null)
						{
							if (animal.home == null)
							{
								flag = true;
							}
							animal.homeInterior = animalHouse2;
						}
					}
				}
			}
			if (!flag && !flag2)
			{
				return true;
			}
			List<FarmAnimal> allFarmAnimals = f.getAllFarmAnimals();
			allFarmAnimals.RemoveAll((FarmAnimal farmAnimal) => farmAnimal.home != null);
			foreach (FarmAnimal a in allFarmAnimals)
			{
				foreach (Building building3 in f.buildings)
				{
					building3.GetIndoors()?.animals.RemoveWhere((KeyValuePair<long, FarmAnimal> pair) => pair.Value.Equals(a));
				}
				f.animals.RemoveWhere((KeyValuePair<long, FarmAnimal> pair) => pair.Value.Equals(a));
			}
			foreach (Building b in f.buildings)
			{
				if (b.GetIndoors() is AnimalHouse animalHouse3)
				{
					animalHouse3.animalsThatLiveHere.RemoveWhere((long id) => getAnimal(id)?.home != b);
				}
			}
			foreach (FarmAnimal item5 in allFarmAnimals)
			{
				foreach (Building building4 in f.buildings)
				{
					if (item5.CanLiveIn(building4) && building4.GetIndoors() is AnimalHouse animalHouse4 && !animalHouse4.isFull())
					{
						animalHouse4.adoptAnimal(item5);
						break;
					}
				}
			}
			foreach (FarmAnimal item6 in allFarmAnimals)
			{
				if (item6.home == null)
				{
					item6.Position = recursiveFindOpenTileForCharacter(item6, f, new Vector2(40f, 40f), 200) * 64f;
					f.animals.TryAdd(item6.myID.Value, item6);
				}
			}
			return true;
		}, includeInteriors: false);
	}

	/// <summary>Create a generated event to marry a player's current NPC or player spouse.</summary>
	/// <param name="farmer">The player getting married.</param>
	public static Event getWeddingEvent(Farmer farmer)
	{
		Farmer farmer2 = null;
		long? spouse = farmer.team.GetSpouse(farmer.UniqueMultiplayerID);
		if (spouse.HasValue)
		{
			farmer2 = Game1.GetPlayer(spouse.Value);
		}
		string spouseActor = ((farmer2 != null) ? ("farmer" + getFarmerNumberFromFarmer(farmer2)) : farmer.spouse);
		WeddingData weddingData = DataLoader.Weddings(Game1.content);
		List<WeddingAttendeeData> contextualAttendees = new List<WeddingAttendeeData>();
		if (weddingData.Attendees != null)
		{
			List<string> exes = getExes(farmer);
			foreach (WeddingAttendeeData value2 in weddingData.Attendees.Values)
			{
				if (!exes.Contains(value2.Id) && !(value2.Id == farmer.spouse) && GameStateQuery.CheckConditions(value2.Condition, null, farmer) && (value2.IgnoreUnlockConditions || !NPC.TryGetData(value2.Id, out var data) || GameStateQuery.CheckConditions(data.UnlockConditions, null, farmer)))
				{
					contextualAttendees.Add(value2);
				}
			}
		}
		if (!weddingData.EventScript.TryGetValue(spouse?.ToString() ?? farmer.spouse, out var value) && !weddingData.EventScript.TryGetValue("default", out value))
		{
			throw new InvalidOperationException("The Data/Weddings asset has no wedding script with the 'default' script key.");
		}
		value = TokenParser.ParseText(value, null, ParseWeddingToken, farmer);
		return new Event(value, null, "-2", farmer);
		bool ParseWeddingToken(string[] query, out string replacement, Random random, Farmer player)
		{
			switch (ArgUtility.Get(query, 0)?.ToLower())
			{
			case "spouseactor":
				replacement = spouseActor;
				return true;
			case "setupcontextualweddingattendees":
			{
				StringBuilder stringBuilder2 = new StringBuilder();
				foreach (WeddingAttendeeData item in contextualAttendees)
				{
					stringBuilder2.Append(" ");
					stringBuilder2.Append(item.Setup);
				}
				replacement = stringBuilder2.ToString();
				return true;
			}
			case "contextualweddingcelebrations":
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (WeddingAttendeeData item2 in contextualAttendees)
				{
					if (item2.Celebration != null)
					{
						stringBuilder.Append(item2.Celebration);
						stringBuilder.Append("/");
					}
				}
				replacement = stringBuilder.ToString();
				return true;
			}
			default:
				replacement = null;
				return false;
			}
		}
	}

	/// <summary>Draw a box to the screen.</summary>
	/// <param name="b">The sprite batch being drawn.</param>
	/// <param name="pixelArea">The pixel area of the box to draw.</param>
	/// <param name="borderWidth">The width of the border to draw.</param>
	/// <param name="borderColor">The color of the border to draw, or <c>null</c> for black.</param>
	/// <param name="backgroundColor">The background color to draw, or <c>null</c> for none.</param>
	public static void DrawSquare(SpriteBatch b, Microsoft.Xna.Framework.Rectangle pixelArea, int borderWidth, Color? borderColor = null, Color? backgroundColor = null)
	{
		if (backgroundColor.HasValue)
		{
			b.Draw(Game1.staminaRect, pixelArea, backgroundColor.Value);
		}
		if (borderWidth > 0)
		{
			Color color = borderColor ?? Color.Black;
			b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle(pixelArea.X, pixelArea.Y, pixelArea.Width, borderWidth), color);
			b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle(pixelArea.X, pixelArea.Y + pixelArea.Height - borderWidth, pixelArea.Width, borderWidth), color);
			b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle(pixelArea.X, pixelArea.Y, borderWidth, pixelArea.Height), color);
			b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle(pixelArea.X + pixelArea.Width - borderWidth, pixelArea.Y, borderWidth, pixelArea.Height), color);
		}
	}

	/// <summary>Draw a missing-texture image to the screen.</summary>
	/// <param name="spriteBatch">The sprite batch being drawn.</param>
	/// <param name="screenArea">The pixel area within the <see cref="F:StardewValley.Game1.viewport" /> to cover with the error texture.</param>
	/// <param name="layerDepth">The layer depth at which to draw the error texture in the <paramref name="spriteBatch" />.</param>
	public static void DrawErrorTexture(SpriteBatch spriteBatch, Microsoft.Xna.Framework.Rectangle screenArea, float layerDepth)
	{
		spriteBatch.Draw(Game1.mouseCursors, screenArea, new Microsoft.Xna.Framework.Rectangle(320, 496, 16, 16), Color.White, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
	}

	public static void drawTinyDigits(int toDraw, SpriteBatch b, Vector2 position, float scale, float layerDepth, Color c)
	{
		int num = 0;
		int num2 = toDraw;
		int num3 = 0;
		do
		{
			num3++;
		}
		while ((toDraw /= 10) >= 1);
		int num4 = (int)Math.Pow(10.0, num3 - 1);
		bool flag = false;
		for (int i = 0; i < num3; i++)
		{
			int num5 = num2 / num4 % 10;
			if (num5 > 0 || i == num3 - 1)
			{
				flag = true;
			}
			if (flag)
			{
				b.Draw(Game1.mouseCursors, position + new Vector2(num, 0f), new Microsoft.Xna.Framework.Rectangle(368 + num5 * 5, 56, 5, 7), c, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
			}
			num += (int)(5f * scale) - 1;
			num4 /= 10;
		}
	}

	public static int getWidthOfTinyDigitString(int toDraw, float scale)
	{
		int num = 0;
		do
		{
			num++;
		}
		while ((toDraw /= 10) >= 1);
		return (int)((float)(num * 5) * scale);
	}

	public static bool isMale(string who)
	{
		if (NPC.TryGetData(who, out var data))
		{
			return data.Gender == Gender.Male;
		}
		return true;
	}

	public static int GetMaximumHeartsForCharacter(Character character)
	{
		if (character == null)
		{
			return 0;
		}
		int result = 10;
		if (character is NPC nPC && nPC.datable.Value)
		{
			result = 8;
		}
		if (Game1.player.friendshipData.TryGetValue(character.Name, out var value))
		{
			if (value.IsMarried())
			{
				result = 14;
			}
			else if (value.IsDating())
			{
				result = 10;
			}
		}
		return result;
	}

	/// <summary>Get whether an item exists anywhere in the world.</summary>
	/// <param name="itemId">The qualified or unqualified item ID.</param>
	public static bool doesItemExistAnywhere(string itemId)
	{
		itemId = ItemRegistry.QualifyItemId(itemId);
		if (itemId == null)
		{
			return false;
		}
		bool itemFound = false;
		ForEachItem(delegate(Item item)
		{
			if (item.QualifiedItemId == itemId)
			{
				itemFound = true;
			}
			return !itemFound;
		});
		return itemFound;
	}

	internal static void CollectGarbage(string filePath = "", int lineNumber = 0)
	{
		GC.Collect(0, GCCollectionMode.Forced);
	}

	public static List<string> possibleCropsAtThisTime(Season season, bool firstWeek)
	{
		List<string> list = null;
		List<string> list2 = null;
		switch (season)
		{
		case Season.Spring:
			list = new List<string> { "24", "192" };
			if (Game1.year > 1)
			{
				list.Add("250");
			}
			if (doesAnyFarmerHaveMail("ccVault"))
			{
				list.Add("248");
			}
			list2 = new List<string> { "190", "188" };
			if (doesAnyFarmerHaveMail("ccVault"))
			{
				list2.Add("252");
			}
			list2.AddRange(list);
			break;
		case Season.Summer:
			list = new List<string> { "264", "262", "260" };
			list2 = new List<string> { "254", "256" };
			if (Game1.year > 1)
			{
				list.Add("266");
			}
			if (doesAnyFarmerHaveMail("ccVault"))
			{
				list2.AddRange(new string[2] { "258", "268" });
			}
			list2.AddRange(list);
			break;
		case Season.Fall:
			list = new List<string> { "272", "278" };
			list2 = new List<string> { "270", "276", "280" };
			if (Game1.year > 1)
			{
				list2.Add("274");
			}
			if (doesAnyFarmerHaveMail("ccVault"))
			{
				list.Add("284");
				list2.Add("282");
			}
			list2.AddRange(list);
			break;
		}
		if (!firstWeek)
		{
			return list2;
		}
		return list;
	}

	public static float RandomFloat(float min, float max, Random random = null)
	{
		if (random == null)
		{
			random = Game1.random;
		}
		return Lerp(min, max, (float)random.NextDouble());
	}

	public static float Clamp(float value, float min, float max)
	{
		if (max < min)
		{
			float num = min;
			min = max;
			max = num;
		}
		if (value < min)
		{
			value = min;
		}
		if (value > max)
		{
			value = max;
		}
		return value;
	}

	public static Color MakeCompletelyOpaque(Color color)
	{
		if (color.A >= byte.MaxValue)
		{
			return color;
		}
		color.A = byte.MaxValue;
		return color;
	}

	public static int Clamp(int value, int min, int max)
	{
		if (max < min)
		{
			int num = min;
			min = max;
			max = num;
		}
		if (value < min)
		{
			value = min;
		}
		if (value > max)
		{
			value = max;
		}
		return value;
	}

	public static float Lerp(float a, float b, float t)
	{
		return a + t * (b - a);
	}

	public static float MoveTowards(float from, float to, float delta)
	{
		if (Math.Abs(to - from) <= delta)
		{
			return to;
		}
		return from + (float)Math.Sign(to - from) * delta;
	}

	public static Color MultiplyColor(Color a, Color b)
	{
		return new Color((float)(int)a.R / 255f * ((float)(int)b.R / 255f), (float)(int)a.G / 255f * ((float)(int)b.G / 255f), (float)(int)a.B / 255f * ((float)(int)b.B / 255f), (float)(int)a.A / 255f * ((float)(int)b.A / 255f));
	}

	/// <summary>Get the number of minutes until 6am tomorrow.</summary>
	/// <param name="currentTime">The starting time of day, in 26-hour format.</param>
	public static int CalculateMinutesUntilMorning(int currentTime)
	{
		return CalculateMinutesUntilMorning(currentTime, 1);
	}

	/// <summary>Get the number of minutes until 6am on a given day.</summary>
	/// <param name="currentTime">The starting time of day, in 26-hour format.</param>
	/// <param name="daysElapsed">The day offset (e.g. 1 for tomorrow).</param>
	public static int CalculateMinutesUntilMorning(int currentTime, int daysElapsed)
	{
		if (daysElapsed < 1)
		{
			return 0;
		}
		return ConvertTimeToMinutes(2600) - ConvertTimeToMinutes(currentTime) + 400 + (daysElapsed - 1) * 1600;
	}

	/// <summary>Get the number of minutes between two times.</summary>
	/// <param name="startTime">The starting time of day, in 26-hour format.</param>
	/// <param name="endTime">The ending time of day, in 26-hour format.</param>
	public static int CalculateMinutesBetweenTimes(int startTime, int endTime)
	{
		return ConvertTimeToMinutes(endTime) - ConvertTimeToMinutes(startTime);
	}

	/// <summary>Apply a minute offset to a time of day.</summary>
	/// <param name="timestamp">The initial time of day, in 26-hour format.</param>
	/// <param name="minutes_to_add">The number of minutes to add to the time.</param>
	public static int ModifyTime(int timestamp, int minutes_to_add)
	{
		timestamp = ConvertTimeToMinutes(timestamp);
		timestamp += minutes_to_add;
		return ConvertMinutesToTime(timestamp);
	}

	/// <summary>Get the time of day given the number of minutes since midnight.</summary>
	/// <param name="minutes">The number of minutes since midnight.</param>
	public static int ConvertMinutesToTime(int minutes)
	{
		return minutes / 60 * 100 + minutes % 60;
	}

	/// <summary>Get the number of minutes since midnight for a time.</summary>
	/// <param name="time_stamp">The time of day, in 26-hour format.</param>
	public static int ConvertTimeToMinutes(int time_stamp)
	{
		return time_stamp / 100 * 60 + time_stamp % 100;
	}

	public static int getSellToStorePriceOfItem(Item i, bool countStack = true)
	{
		if (i != null)
		{
			return i.sellToStorePrice(-1L) * ((!countStack) ? 1 : i.Stack);
		}
		return 0;
	}

	/// <summary>Get a list of secret notes or journal scraps that have not been seen.</summary>
	/// <param name="who">The farmer to check for unseen secret notes or journal scraps.</param>
	/// <param name="journal">Whether to get journal scraps (true) or secret notes (false).</param>
	/// <param name="totalNotes">The total number of secret notes or journal scraps (depending on <paramref name="journal" />), including seen ones.</param>
	public static int[] GetUnseenSecretNotes(Farmer who, bool journal, out int totalNotes)
	{
		Func<int, bool> predicate = ((!journal) ? ((Func<int, bool>)((int id) => id < GameLocation.JOURNAL_INDEX)) : ((Func<int, bool>)((int id) => id >= GameLocation.JOURNAL_INDEX)));
		int[] array = DataLoader.SecretNotes(Game1.content).Keys.Where(predicate).ToArray();
		totalNotes = array.Length;
		return array.Except(who.secretNotesSeen.Where(predicate)).ToArray();
	}

	public static bool HasAnyPlayerSeenSecretNote(int note_number)
	{
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.secretNotesSeen.Contains(note_number))
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasAnyPlayerSeenEvent(string eventId)
	{
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.eventsSeen.Contains(eventId))
			{
				return true;
			}
		}
		return false;
	}

	public static bool HaveAllPlayersSeenEvent(string eventId)
	{
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (!allFarmer.eventsSeen.Contains(eventId))
			{
				return false;
			}
		}
		return true;
	}

	public static List<string> GetAllPlayerUnlockedCookingRecipes()
	{
		List<string> list = new List<string>();
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			foreach (string key in allFarmer.cookingRecipes.Keys)
			{
				if (!list.Contains(key))
				{
					list.Add(key);
				}
			}
		}
		return list;
	}

	public static List<string> GetAllPlayerUnlockedCraftingRecipes()
	{
		List<string> list = new List<string>();
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			foreach (string key in allFarmer.craftingRecipes.Keys)
			{
				if (!list.Contains(key))
				{
					list.Add(key);
				}
			}
		}
		return list;
	}

	public static int GetAllPlayerFriendshipLevel(NPC npc)
	{
		int num = -1;
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.friendshipData.TryGetValue(npc.Name, out var value) && value.Points > num)
			{
				num = value.Points;
			}
		}
		return num;
	}

	public static int GetAllPlayerReachedBottomOfMines()
	{
		int num = 0;
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.timesReachedMineBottom > num)
			{
				num = allFarmer.timesReachedMineBottom;
			}
		}
		return num;
	}

	public static int GetAllPlayerDeepestMineLevel()
	{
		int num = 0;
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.deepestMineLevel > num)
			{
				num = allFarmer.deepestMineLevel;
			}
		}
		return num;
	}

	public static string LegacyWeatherToWeather(int legacyWeather)
	{
		return legacyWeather switch
		{
			2 => "Wind", 
			4 => "Festival", 
			3 => "Storm", 
			1 => "Rain", 
			5 => "Snow", 
			6 => "Wedding", 
			_ => "Sun", 
		};
	}

	public static string getRandomBasicSeasonalForageItem(Season season, int randomSeedAddition = -1)
	{
		Random random = CreateRandom(Game1.uniqueIDForThisGame, randomSeedAddition);
		string[] options = LegacyShims.EmptyArray<string>();
		switch (season)
		{
		case Season.Spring:
			options = new string[4] { "16", "18", "20", "22" };
			break;
		case Season.Summer:
			options = new string[3] { "396", "398", "402" };
			break;
		case Season.Fall:
			options = new string[4] { "404", "406", "408", "410" };
			break;
		case Season.Winter:
			options = new string[4] { "412", "414", "416", "418" };
			break;
		}
		return random.ChooseFrom(options) ?? "0";
	}

	public static string getRandomPureSeasonalItem(Season season, int randomSeedAddition)
	{
		Random random = CreateRandom(Game1.uniqueIDForThisGame, randomSeedAddition);
		string[] options = LegacyShims.EmptyArray<string>();
		switch (season)
		{
		case Season.Spring:
			options = new string[15]
			{
				"16", "18", "20", "22", "129", "131", "132", "136", "137", "142",
				"143", "145", "147", "148", "152"
			};
			break;
		case Season.Summer:
			options = new string[16]
			{
				"128", "130", "131", "132", "136", "138", "142", "144", "145", "146",
				"149", "150", "155", "396", "398", "402"
			};
			break;
		case Season.Fall:
			options = new string[17]
			{
				"404", "406", "408", "410", "129", "131", "132", "136", "137", "139",
				"140", "142", "143", "148", "150", "154", "155"
			};
			break;
		case Season.Winter:
			options = new string[17]
			{
				"412", "414", "416", "418", "130", "131", "132", "136", "140", "141",
				"143", "144", "146", "147", "150", "151", "154"
			};
			break;
		}
		return random.ChooseFrom(options) ?? "0";
	}

	public static Item CreateFlavoredItem(string baseID, string preservesID, int quality = 0, int stack = 1)
	{
		ItemQueryContext context = new ItemQueryContext(Game1.currentLocation, Game1.player, Game1.random, "FLAVORED_ITEM query");
		if (ItemQueryResolver.TryResolve("FLAVORED_ITEM " + baseID + " " + preservesID, context).FirstOrDefault()?.Item is Item item)
		{
			item.Quality = quality;
			item.Stack = stack;
			return item;
		}
		return null;
	}

	public static string getRandomItemFromSeason(Season season, bool forQuest, Random random)
	{
		List<string> list = new List<string> { "68", "66", "78", "80", "86", "152", "167", "153", "420" };
		List<string> list2 = new List<string>(Game1.player.craftingRecipes.Keys);
		List<string> list3 = new List<string>(Game1.player.cookingRecipes.Keys);
		if (forQuest)
		{
			list2 = GetAllPlayerUnlockedCraftingRecipes();
			list3 = GetAllPlayerUnlockedCookingRecipes();
		}
		if ((forQuest && (MineShaft.lowestLevelReached > 40 || GetAllPlayerReachedBottomOfMines() >= 1)) || (!forQuest && (Game1.player.deepestMineLevel > 40 || Game1.player.timesReachedMineBottom >= 1)))
		{
			list.AddRange(new string[5] { "62", "70", "72", "84", "422" });
		}
		if ((forQuest && (MineShaft.lowestLevelReached > 80 || GetAllPlayerReachedBottomOfMines() >= 1)) || (!forQuest && (Game1.player.deepestMineLevel > 80 || Game1.player.timesReachedMineBottom >= 1)))
		{
			list.AddRange(new string[3] { "64", "60", "82" });
		}
		if (doesAnyFarmerHaveMail("ccVault"))
		{
			list.AddRange(new string[4] { "88", "90", "164", "165" });
		}
		if (list2.Contains("Furnace"))
		{
			list.AddRange(new string[4] { "334", "335", "336", "338" });
		}
		if (list2.Contains("Quartz Globe"))
		{
			list.Add("339");
		}
		switch (season)
		{
		case Season.Spring:
			list.AddRange(new string[17]
			{
				"16", "18", "20", "22", "129", "131", "132", "136", "137", "142",
				"143", "145", "147", "148", "152", "167", "267"
			});
			break;
		case Season.Summer:
			list.AddRange(new string[16]
			{
				"128", "130", "132", "136", "138", "142", "144", "145", "146", "149",
				"150", "155", "396", "398", "402", "267"
			});
			break;
		case Season.Fall:
			list.AddRange(new string[18]
			{
				"404", "406", "408", "410", "129", "131", "132", "136", "137", "139",
				"140", "142", "143", "148", "150", "154", "155", "269"
			});
			break;
		case Season.Winter:
			list.AddRange(new string[17]
			{
				"412", "414", "416", "418", "130", "131", "132", "136", "140", "141",
				"144", "146", "147", "150", "151", "154", "269"
			});
			break;
		}
		if (forQuest)
		{
			foreach (string item in list3)
			{
				if (random.NextDouble() < 0.4)
				{
					continue;
				}
				List<string> list4 = possibleCropsAtThisTime(Game1.season, Game1.dayOfMonth <= 7);
				if (!CraftingRecipe.cookingRecipes.TryGetValue(item, out var value))
				{
					continue;
				}
				string[] array = value.Split('/');
				string[] array2 = ArgUtility.SplitBySpace(ArgUtility.Get(array, 0));
				bool flag = true;
				for (int i = 0; i < array2.Length; i++)
				{
					if (!list.Contains(array2[i]) && !isCategoryIngredientAvailable(array2[i]) && (list4 == null || !list4.Contains(array2[i])))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					string text = ArgUtility.Get(array, 2);
					if (text != null)
					{
						list.Add(text);
					}
				}
			}
		}
		return random.ChooseFrom(list);
	}

	public static string getRandomItemFromSeason(Season season, int randomSeedAddition, bool forQuest, bool changeDaily = true)
	{
		Random random = CreateRandom(Game1.uniqueIDForThisGame, changeDaily ? Game1.stats.DaysPlayed : 0u, randomSeedAddition);
		return getRandomItemFromSeason(season, forQuest, random);
	}

	private static bool isCategoryIngredientAvailable(string category)
	{
		if (category != null && category.StartsWith('-'))
		{
			if (category == "-5" || category == "-6")
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public static void farmerHeardSong(string trackName)
	{
		if (string.IsNullOrWhiteSpace(trackName))
		{
			return;
		}
		HashSet<string> songsHeard = Game1.player.songsHeard;
		switch (trackName)
		{
		case "EarthMine":
			songsHeard.Add("Crystal Bells");
			songsHeard.Add("Cavern");
			songsHeard.Add("Secret Gnomes");
			break;
		case "FrostMine":
			songsHeard.Add("Cloth");
			songsHeard.Add("Icicles");
			songsHeard.Add("XOR");
			break;
		case "LavaMine":
			songsHeard.Add("Of Dwarves");
			songsHeard.Add("Near The Planet Core");
			songsHeard.Add("Overcast");
			songsHeard.Add("tribal");
			break;
		case "VolcanoMines":
			songsHeard.Add("VolcanoMines1");
			songsHeard.Add("VolcanoMines2");
			break;
		default:
			if (trackName != "none" && trackName != "rain" && trackName != "silence")
			{
				songsHeard.Add(trackName);
			}
			break;
		}
	}

	public static float getMaxedFriendshipPercent(Farmer who = null)
	{
		if (who == null)
		{
			who = Game1.player;
		}
		int num = 0;
		int num2 = 0;
		foreach (KeyValuePair<string, CharacterData> characterDatum in Game1.characterData)
		{
			string key = characterDatum.Key;
			CharacterData value = characterDatum.Value;
			if (!value.PerfectionScore || GameStateQuery.IsImmutablyFalse(value.CanSocialize))
			{
				continue;
			}
			num2++;
			if (who.friendshipData.TryGetValue(key, out var value2))
			{
				int num3 = (value.CanBeRomanced ? 8 : 10) * 250;
				if (value2 != null && value2.Points >= num3)
				{
					num++;
				}
			}
		}
		return (float)num / ((float)num2 * 1f);
	}

	public static float getCookedRecipesPercent(Farmer who = null)
	{
		if (who == null)
		{
			who = Game1.player;
		}
		Dictionary<string, string> cookingRecipes = CraftingRecipe.cookingRecipes;
		float num = 0f;
		foreach (KeyValuePair<string, string> item in cookingRecipes)
		{
			string key = item.Key;
			if (who.cookingRecipes.ContainsKey(key))
			{
				string key2 = ArgUtility.SplitBySpaceAndGet(ArgUtility.Get(item.Value.Split('/'), 2), 0);
				if (who.recipesCooked.ContainsKey(key2))
				{
					num += 1f;
				}
			}
		}
		return num / (float)cookingRecipes.Count;
	}

	public static float getCraftedRecipesPercent(Farmer who = null)
	{
		if (who == null)
		{
			who = Game1.player;
		}
		Dictionary<string, string> craftingRecipes = CraftingRecipe.craftingRecipes;
		float num = 0f;
		foreach (string key in craftingRecipes.Keys)
		{
			if (!(key == "Wedding Ring") && who.craftingRecipes.TryGetValue(key, out var value) && value > 0)
			{
				num += 1f;
			}
		}
		return num / ((float)craftingRecipes.Count - 1f);
	}

	public static float getFishCaughtPercent(Farmer who = null)
	{
		if (who == null)
		{
			who = Game1.player;
		}
		float num = 0f;
		float num2 = 0f;
		foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
		{
			if (allDatum.ObjectType == "Fish" && !(allDatum.RawData is ObjectData { ExcludeFromFishingCollection: not false }))
			{
				num2 += 1f;
				if (who.fishCaught.ContainsKey(allDatum.QualifiedItemId))
				{
					num += 1f;
				}
			}
		}
		return num / num2;
	}

	public static KeyValuePair<Farmer, bool> GetFarmCompletion(Func<Farmer, bool> check)
	{
		if (check(Game1.player))
		{
			return new KeyValuePair<Farmer, bool>(Game1.player, value: true);
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer != Game1.player && allFarmer.isCustomized.Value && check(allFarmer))
			{
				return new KeyValuePair<Farmer, bool>(allFarmer, value: true);
			}
		}
		return new KeyValuePair<Farmer, bool>(Game1.player, value: false);
	}

	public static KeyValuePair<Farmer, float> GetFarmCompletion(Func<Farmer, float> check)
	{
		Farmer key = Game1.player;
		float num = check(Game1.player);
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer != Game1.player && allFarmer.isCustomized.Value)
			{
				float num2 = check(allFarmer);
				if (num2 > num)
				{
					key = allFarmer;
					num = num2;
				}
			}
		}
		return new KeyValuePair<Farmer, float>(key, num);
	}

	/// <summary>Get the overall perfection score for this save, accounting for all players.</summary>
	/// <remarks>See also <see cref="M:StardewValley.Game1.UpdateFarmPerfection" /> for the overnight changes if perfection was reached.</remarks>
	/// <returns>Returns a number between 0 (no perfection requirements met) and 1 (all requirements met).</returns>
	public static float percentGameComplete()
	{
		float num = 0f;
		float num2 = 0f + GetFarmCompletion((Farmer farmer) => getFarmerItemsShippedPercent(farmer)).Value * 15f;
		num += 15f;
		float num3 = num2 + Math.Min(GetObeliskTypesBuilt(), 4f);
		num += 4f;
		float num4 = num3 + (float)(Game1.IsBuildingConstructed("Gold Clock") ? 10 : 0);
		num += 10f;
		float num5 = num4 + (float)(GetFarmCompletion((Farmer farmer) => farmer.hasCompletedAllMonsterSlayerQuests.Value).Value ? 10 : 0);
		num += 10f;
		float value = GetFarmCompletion((Farmer farmer) => getMaxedFriendshipPercent(farmer)).Value;
		float num6 = num5 + value * 11f;
		num += 11f;
		float value2 = GetFarmCompletion((Farmer farmer) => Math.Min(farmer.Level, 25f) / 25f).Value;
		float num7 = num6 + value2 * 5f;
		num += 5f;
		float num8 = num7 + (float)(GetFarmCompletion((Farmer farmer) => foundAllStardrops(farmer)).Value ? 10 : 0);
		num += 10f;
		float num9 = num8 + GetFarmCompletion((Farmer farmer) => getCookedRecipesPercent(farmer)).Value * 10f;
		num += 10f;
		float num10 = num9 + GetFarmCompletion((Farmer farmer) => getCraftedRecipesPercent(farmer)).Value * 10f;
		num += 10f;
		float num11 = num10 + GetFarmCompletion((Farmer farmer) => getFishCaughtPercent(farmer)).Value * 10f;
		num += 10f;
		float num12 = 130f;
		float num13 = Math.Min(Game1.netWorldState.Value.GoldenWalnutsFound, num12);
		float num14 = num11 + num13 / num12 * 5f;
		num += 5f;
		return num14 / num;
	}

	/// <summary>Get the number of unique obelisk building types constructed anywhere in the world.</summary>
	public static int GetObeliskTypesBuilt()
	{
		return (Game1.IsBuildingConstructed("Water Obelisk") ? 1 : 0) + (Game1.IsBuildingConstructed("Earth Obelisk") ? 1 : 0) + (Game1.IsBuildingConstructed("Desert Obelisk") ? 1 : 0) + (Game1.IsBuildingConstructed("Island Obelisk") ? 1 : 0);
	}

	private static int itemsShippedPercent()
	{
		return (int)((float)Game1.player.basicShipped.Length / 92f * 5f);
	}

	public static int getTrashReclamationPrice(Item i, Farmer f)
	{
		float num = 0.15f * (float)f.trashCanLevel;
		if (i.canBeTrashed())
		{
			if (i is Wallpaper || i is Furniture)
			{
				return -1;
			}
			if ((i is Object obj && !obj.bigCraftable.Value) || i is MeleeWeapon || i is Ring || i is Boots)
			{
				return (int)((float)i.Stack * ((float)i.sellToStorePrice(-1L) * num));
			}
		}
		return -1;
	}

	/// <summary>Get the help-wanted quest to show on Pierre's bulletin board today, if any.</summary>
	public static Quest getQuestOfTheDay()
	{
		if (Game1.stats.DaysPlayed <= 1)
		{
			return null;
		}
		double num = CreateDaySaveRandom(100.0, Game1.stats.DaysPlayed * 777).NextDouble();
		if (num < 0.08)
		{
			return new ResourceCollectionQuest();
		}
		if (num < 0.2 && MineShaft.lowestLevelReached > 0 && Game1.stats.DaysPlayed > 5)
		{
			return new SlayMonsterQuest
			{
				ignoreFarmMonsters = { true }
			};
		}
		if (num < 0.5)
		{
			return null;
		}
		if (num < 0.6)
		{
			return new FishingQuest();
		}
		if (num < 0.66 && Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Mon"))
		{
			bool flag = false;
			foreach (Farmer allFarmer in Game1.getAllFarmers())
			{
				foreach (Quest item in allFarmer.questLog)
				{
					if (item is SocializeQuest)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				return new SocializeQuest();
			}
			return new ItemDeliveryQuest();
		}
		return new ItemDeliveryQuest();
	}

	/// <summary>Get a MonoGame color from a string representation.</summary>
	/// <param name="rawColor">The raw color value to parse. This can be a <see cref="T:Microsoft.Xna.Framework.Color" /> property name (like <c>SkyBlue</c>), RGB or RGBA hex code (like <c>#AABBCC</c> or <c>#AABBCCDD</c>), or 8-bit RGB or RGBA code (like <c>34 139 34</c> or <c>34 139 34 255</c>).</param>
	/// <returns>Returns the matching color (if any), else <c>null</c>.</returns>
	public static Color? StringToColor(string rawColor)
	{
		rawColor = rawColor?.Trim();
		if (string.IsNullOrEmpty(rawColor))
		{
			return null;
		}
		if (rawColor.StartsWith('#'))
		{
			byte result = byte.MaxValue;
			if ((rawColor.Length == 7 || rawColor.Length == 9) && byte.TryParse(rawColor.Substring(1, 2), NumberStyles.HexNumber, null, out var result2) && byte.TryParse(rawColor.Substring(3, 2), NumberStyles.HexNumber, null, out var result3) && byte.TryParse(rawColor.Substring(5, 2), NumberStyles.HexNumber, null, out var result4) && (rawColor.Length == 7 || byte.TryParse(rawColor.Substring(7, 2), NumberStyles.HexNumber, null, out result)))
			{
				return new Color(result2, result3, result4, result);
			}
		}
		else if (rawColor.Contains(' '))
		{
			string[] array = ArgUtility.SplitBySpace(rawColor);
			if ((array.Length == 3 || array.Length == 4) && ArgUtility.TryGetInt(array, 0, out var value, out var error, "int red") && ArgUtility.TryGetInt(array, 1, out var value2, out error, "int green") && ArgUtility.TryGetInt(array, 2, out var value3, out error, "int blue") && ArgUtility.TryGetOptionalInt(array, 3, out var value4, out error, 255, "int alpha"))
			{
				return new Color(value, value2, value3, value4);
			}
		}
		else
		{
			PropertyInfo property = typeof(Color).GetProperty(rawColor, BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				return (Color)property.GetValue(null, null);
			}
		}
		Game1.log.Warn("Can't parse '" + rawColor + "' as a color because it's not a hexadecimal code, RGB code, or color name.");
		return null;
	}

	public static Color getOppositeColor(Color color)
	{
		return new Color(255 - color.R, 255 - color.G, 255 - color.B);
	}

	public static void drawLightningBolt(Vector2 strikePosition, GameLocation l)
	{
		Microsoft.Xna.Framework.Rectangle sourceRect = new Microsoft.Xna.Framework.Rectangle(644, 1078, 37, 57);
		Vector2 position = strikePosition + new Vector2(-sourceRect.Width * 4 / 2, -sourceRect.Height * 4);
		while (position.Y > (float)(-sourceRect.Height * 4))
		{
			l.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", sourceRect, 9999f, 1, 999, position, flicker: false, Game1.random.NextBool(), (strikePosition.Y + 32f) / 10000f + 0.001f, 0.025f, Color.White, 4f, 0f, 0f, 0f)
			{
				lightId = $"{l.NameOrUniqueName}_LightningBolt_{strikePosition.X}_{strikePosition.Y}_{Game1.random.Next()}",
				lightRadius = 2f,
				delayBeforeAnimationStart = 200,
				lightcolor = Color.Black
			});
			position.Y -= sourceRect.Height * 4;
		}
	}

	/// <summary>Get a translated display text for a calendar date.</summary>
	/// <param name="day">The calendar day of month.</param>
	/// <param name="season">The calendar season.</param>
	/// <param name="year">The calendar year.</param>
	public static string getDateStringFor(int day, int season, int year)
	{
		if (day <= 0)
		{
			day += 28;
			season--;
			if (season < 0)
			{
				season = 3;
				year--;
			}
		}
		else if (day > 28)
		{
			day -= 28;
			season++;
			if (season > 3)
			{
				season = 0;
				year++;
			}
		}
		if (year == 0)
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5677");
		}
		return Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5678", day, (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.es) ? getSeasonNameFromNumber(season).ToLower() : getSeasonNameFromNumber(season), year);
	}

	public static string getDateString(int offset = 0)
	{
		int dayOfMonth = Game1.dayOfMonth;
		int seasonIndex = Game1.seasonIndex;
		int year = Game1.year;
		return getDateStringFor(dayOfMonth + offset, seasonIndex, year);
	}

	public static string getYesterdaysDate()
	{
		return getDateString(-1);
	}

	public static string getSeasonNameFromNumber(int number)
	{
		return number switch
		{
			0 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5680"), 
			1 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5681"), 
			2 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5682"), 
			3 => Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5683"), 
			_ => "", 
		};
	}

	public static string getNumberEnding(int number)
	{
		if (number % 100 > 10 && number % 100 < 20)
		{
			return "th";
		}
		switch (number % 10)
		{
		case 0:
		case 4:
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
			return "th";
		case 1:
			return "st";
		case 2:
			return "nd";
		case 3:
			return "rd";
		default:
			return "";
		}
	}

	public static void killAllStaticLoopingSoundCues()
	{
		Intro.roadNoise?.Stop(AudioStopOptions.Immediate);
		Fly.buzz?.Stop(AudioStopOptions.Immediate);
		Railroad.trainLoop?.Stop(AudioStopOptions.Immediate);
		BobberBar.reelSound?.Stop(AudioStopOptions.Immediate);
		BobberBar.unReelSound?.Stop(AudioStopOptions.Immediate);
		FishingRod.reelSound?.Stop(AudioStopOptions.Immediate);
		Game1.loopingLocationCues.StopAll();
	}

	public static void consolidateStacks(IList<Item> objects)
	{
		for (int i = 0; i < objects.Count; i++)
		{
			if (!(objects[i] is Object obj))
			{
				continue;
			}
			for (int j = i + 1; j < objects.Count; j++)
			{
				if (objects[j] != null && obj.canStackWith(objects[j]))
				{
					int amount = obj.Stack - objects[j].addToStack(obj);
					if (obj.ConsumeStack(amount) == null)
					{
						objects[i] = null;
						break;
					}
				}
			}
		}
	}

	public static void performLightningUpdate(int time_of_day)
	{
		Random random = CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, time_of_day);
		if (random.NextDouble() < 0.125 + Game1.player.team.AverageDailyLuck() + Game1.player.team.AverageLuckLevel() / 100.0)
		{
			Farm.LightningStrikeEvent lightningStrikeEvent = new Farm.LightningStrikeEvent();
			lightningStrikeEvent.bigFlash = true;
			Farm farm = Game1.getFarm();
			List<Vector2> list = new List<Vector2>();
			foreach (KeyValuePair<Vector2, Object> pair in farm.objects.Pairs)
			{
				if (pair.Value.QualifiedItemId == "(BC)9")
				{
					list.Add(pair.Key);
				}
			}
			if (list.Count > 0)
			{
				for (int i = 0; i < 2; i++)
				{
					Vector2 vector = random.ChooseFrom(list);
					if (farm.objects[vector].heldObject.Value == null)
					{
						farm.objects[vector].heldObject.Value = ItemRegistry.Create<Object>("(O)787");
						farm.objects[vector].minutesUntilReady.Value = CalculateMinutesUntilMorning(Game1.timeOfDay);
						farm.objects[vector].shakeTimer = 1000;
						lightningStrikeEvent.createBolt = true;
						lightningStrikeEvent.boltPosition = vector * 64f + new Vector2(32f, 0f);
						farm.lightningStrikeEvent.Fire(lightningStrikeEvent);
						return;
					}
				}
			}
			if (random.NextDouble() < 0.25 - Game1.player.team.AverageDailyLuck() - Game1.player.team.AverageLuckLevel() / 100.0)
			{
				try
				{
					if (TryGetRandom(farm.terrainFeatures, out var key, out var value))
					{
						if (value is FruitTree fruitTree)
						{
							fruitTree.struckByLightningCountdown.Value = 4;
							fruitTree.shake(key, doEvenIfStillShaking: true);
							lightningStrikeEvent.createBolt = true;
							lightningStrikeEvent.boltPosition = key * 64f + new Vector2(32f, -128f);
						}
						else
						{
							Crop crop = (value as HoeDirt)?.crop;
							bool num = crop != null && !crop.dead.Value;
							if (value.performToolAction(null, 50, key))
							{
								lightningStrikeEvent.destroyedTerrainFeature = true;
								lightningStrikeEvent.createBolt = true;
								farm.terrainFeatures.Remove(key);
								lightningStrikeEvent.boltPosition = key * 64f + new Vector2(32f, -128f);
							}
							if (num && crop.dead.Value)
							{
								lightningStrikeEvent.createBolt = true;
								lightningStrikeEvent.boltPosition = key * 64f + new Vector2(32f, 0f);
							}
						}
					}
				}
				catch (Exception)
				{
				}
			}
			farm.lightningStrikeEvent.Fire(lightningStrikeEvent);
		}
		else if (random.NextDouble() < 0.1)
		{
			Farm.LightningStrikeEvent lightningStrikeEvent2 = new Farm.LightningStrikeEvent();
			lightningStrikeEvent2.smallFlash = true;
			Farm farm = Game1.getFarm();
			farm.lightningStrikeEvent.Fire(lightningStrikeEvent2);
		}
	}

	/// <summary>Apply overnight lightning strikes after the player goes to sleep.</summary>
	/// <param name="timeWentToSleep">The time of day when the player went to sleep, in 26-hour format.</param>
	public static void overnightLightning(int timeWentToSleep)
	{
		if (Game1.IsMasterGame)
		{
			int num = (2300 - timeWentToSleep) / 100;
			for (int i = 1; i <= num; i++)
			{
				performLightningUpdate(timeWentToSleep + i * 100);
			}
		}
	}

	public static List<Vector2> getAdjacentTileLocations(Vector2 tileLocation)
	{
		return new List<Vector2>
		{
			new Vector2(-1f, 0f) + tileLocation,
			new Vector2(1f, 0f) + tileLocation,
			new Vector2(0f, 1f) + tileLocation,
			new Vector2(0f, -1f) + tileLocation
		};
	}

	public static Vector2[] getAdjacentTileLocationsArray(Vector2 tileLocation)
	{
		return new Vector2[4]
		{
			new Vector2(-1f, 0f) + tileLocation,
			new Vector2(1f, 0f) + tileLocation,
			new Vector2(0f, 1f) + tileLocation,
			new Vector2(0f, -1f) + tileLocation
		};
	}

	public static Vector2[] getSurroundingTileLocationsArray(Vector2 tileLocation)
	{
		return new Vector2[8]
		{
			new Vector2(-1f, 0f) + tileLocation,
			new Vector2(1f, 0f) + tileLocation,
			new Vector2(0f, 1f) + tileLocation,
			new Vector2(0f, -1f) + tileLocation,
			new Vector2(-1f, -1f) + tileLocation,
			new Vector2(1f, -1f) + tileLocation,
			new Vector2(1f, 1f) + tileLocation,
			new Vector2(-1f, 1f) + tileLocation
		};
	}

	public static Crop findCloseFlower(GameLocation location, Vector2 startTileLocation, int range = -1, Func<Crop, bool> additional_check = null)
	{
		Queue<Vector2> queue = new Queue<Vector2>();
		HashSet<Vector2> hashSet = new HashSet<Vector2>();
		queue.Enqueue(startTileLocation);
		for (int i = 0; range >= 0 || (range < 0 && i <= 150); i++)
		{
			if (queue.Count <= 0)
			{
				break;
			}
			Vector2 vector = queue.Dequeue();
			HoeDirt hoeDirtAtTile = location.GetHoeDirtAtTile(vector);
			if (hoeDirtAtTile?.crop != null)
			{
				ParsedItemData data = ItemRegistry.GetData(hoeDirtAtTile.crop.indexOfHarvest.Value);
				if (data != null && data.Category == -80 && hoeDirtAtTile.crop.currentPhase.Value >= hoeDirtAtTile.crop.phaseDays.Count - 1 && !hoeDirtAtTile.crop.dead.Value && (additional_check == null || additional_check(hoeDirtAtTile.crop)))
				{
					return hoeDirtAtTile.crop;
				}
			}
			foreach (Vector2 adjacentTileLocation in getAdjacentTileLocations(vector))
			{
				if (!hashSet.Contains(adjacentTileLocation) && (range < 0 || Math.Abs(adjacentTileLocation.X - startTileLocation.X) + Math.Abs(adjacentTileLocation.Y - startTileLocation.Y) <= (float)range))
				{
					queue.Enqueue(adjacentTileLocation);
				}
			}
			hashSet.Add(vector);
		}
		return null;
	}

	public static void recursiveFenceBuild(Vector2 position, int direction, GameLocation location, Random r)
	{
		if (!(r.NextDouble() < 0.04) && !location.objects.ContainsKey(position) && location.isTileLocationOpen(new Location((int)position.X, (int)position.Y)))
		{
			location.objects.Add(position, new Fence(position, "322", isGate: false));
			int num = direction;
			if (r.NextDouble() < 0.16)
			{
				num = r.Next(4);
			}
			if (num == (direction + 2) % 4)
			{
				num = (num + 1) % 4;
			}
			switch (direction)
			{
			case 0:
				recursiveFenceBuild(position + new Vector2(0f, -1f), num, location, r);
				break;
			case 1:
				recursiveFenceBuild(position + new Vector2(1f, 0f), num, location, r);
				break;
			case 3:
				recursiveFenceBuild(position + new Vector2(-1f, 0f), num, location, r);
				break;
			case 2:
				recursiveFenceBuild(position + new Vector2(0f, 1f), num, location, r);
				break;
			}
		}
	}

	public static bool addAnimalToFarm(FarmAnimal animal)
	{
		if (animal?.Sprite == null)
		{
			return false;
		}
		foreach (Building building in Game1.currentLocation.buildings)
		{
			if (animal.CanLiveIn(building) && building.GetIndoors() is AnimalHouse animalHouse && !animalHouse.isFull())
			{
				animalHouse.adoptAnimal(animal);
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// "Standard" description is as follows:
	/// (Item type [Object (O), BigObject (BO), Weapon (W), Ring (R), Hat (H), Boot (B), Blueprint (BL), Big Object Blueprint(BBL)], follwed by item index, then stack amount)
	/// </summary>
	/// <returns>the described Item object</returns>
	[Obsolete("This is only intended for backwards compatibility with older data. Most code should use ItemRegistry instead.")]
	public static Item getItemFromStandardTextDescription(string description, Farmer who, char delimiter = ' ')
	{
		string[] array = description.Split(delimiter);
		string type = array[0];
		string itemId = array[1];
		int stock = Convert.ToInt32(array[2]);
		return getItemFromStandardTextDescription(type, itemId, stock, who);
	}

	/// <summary>
	/// "Standard" description is as follows:
	/// (Item type [Object (O), BigObject (BO), Weapon (W), Ring (R), Hat (H), Boot (B), Blueprint (BL), Big Object Blueprint(BBL)], follwed by item index, then stack amount)
	/// </summary>
	/// <returns>the described Item object</returns>
	[Obsolete("This is only intended for backwards compatibility with older data. Most code should use ItemRegistry instead.")]
	public static Item getItemFromStandardTextDescription(string type, string itemId, int stock, Farmer who)
	{
		Item item = null;
		if (type != null)
		{
			switch (type.Length)
			{
			case 9:
			{
				char c = type[1];
				if (c != 'i')
				{
					if (c != 'l')
					{
						if (c != 'u' || !(type == "Furniture"))
						{
							break;
						}
						goto IL_0252;
					}
					if (!(type == "Blueprint"))
					{
						break;
					}
					goto IL_02cf;
				}
				if (!(type == "BigObject"))
				{
					break;
				}
				goto IL_0284;
			}
			case 1:
				switch (type[0])
				{
				case 'F':
					break;
				case 'O':
				case 'R':
					goto IL_026b;
				case 'B':
					goto IL_029d;
				case 'W':
					goto IL_02b6;
				case 'H':
					goto IL_02ec;
				case 'C':
				{
					item = (int.TryParse(itemId, out var result) ? ItemRegistry.Create(((result >= 1000) ? "(S)" : "(P)") + itemId) : ItemRegistry.Create(itemId));
					goto end_IL_0012;
				}
				default:
					goto end_IL_0012;
				}
				goto IL_0252;
			case 6:
			{
				char c = type[0];
				if (c != 'O')
				{
					if (c != 'W' || !(type == "Weapon"))
					{
						break;
					}
					goto IL_02b6;
				}
				if (!(type == "Object"))
				{
					break;
				}
				goto IL_026b;
			}
			case 4:
			{
				char c = type[0];
				if (c != 'B')
				{
					if (c != 'R' || !(type == "Ring"))
					{
						break;
					}
					goto IL_026b;
				}
				if (!(type == "Boot"))
				{
					break;
				}
				goto IL_029d;
			}
			case 2:
			{
				char c = type[1];
				if (c != 'L')
				{
					if (c != 'O' || !(type == "BO"))
					{
						break;
					}
					goto IL_0284;
				}
				if (!(type == "BL"))
				{
					break;
				}
				goto IL_02cf;
			}
			case 3:
			{
				char c = type[2];
				if (c != 'L')
				{
					if (c != 'l')
					{
						if (c != 't' || !(type == "Hat"))
						{
							break;
						}
						goto IL_02ec;
					}
					if (!(type == "BBl"))
					{
						break;
					}
				}
				else if (!(type == "BBL"))
				{
					break;
				}
				goto IL_0302;
			}
			case 12:
				{
					if (!(type == "BigBlueprint"))
					{
						break;
					}
					goto IL_0302;
				}
				IL_026b:
				item = ItemRegistry.Create("(O)" + itemId);
				break;
				IL_0252:
				item = ItemRegistry.Create("(F)" + itemId);
				break;
				IL_02cf:
				item = ItemRegistry.Create("(O)" + itemId);
				item.IsRecipe = true;
				break;
				IL_0284:
				item = ItemRegistry.Create("(BC)" + itemId);
				break;
				IL_0302:
				item = ItemRegistry.Create("(BC)" + itemId);
				item.IsRecipe = true;
				break;
				IL_02b6:
				item = ItemRegistry.Create("(W)" + itemId);
				break;
				IL_02ec:
				item = ItemRegistry.Create("(H)" + itemId);
				break;
				IL_029d:
				item = ItemRegistry.Create("(B)" + itemId);
				break;
				end_IL_0012:
				break;
			}
		}
		item.Stack = stock;
		if (who != null && item.IsRecipe && who.knowsRecipe(item.Name))
		{
			return null;
		}
		return item;
	}

	[Obsolete("This is only intended for backwards compatibility with older data. Most code should use ItemRegistry instead.")]
	public static string getStandardDescriptionFromItem(Item item, int stack, char delimiter = ' ')
	{
		return getStandardDescriptionFromItem(item.TypeDefinitionId, item.ItemId, item.isRecipe.Value, item is Ring, stack, delimiter);
	}

	[Obsolete("This is only intended for backwards compatibility with older data. Most code should use ItemRegistry instead.")]
	public static string getStandardDescriptionFromItem(string typeDefinitionId, string itemId, bool isRecipe, bool isRing, int stack, char delimiter = ' ')
	{
		if (typeDefinitionId == null)
		{
			goto IL_014f;
		}
		int length = typeDefinitionId.Length;
		string text;
		if (length != 3)
		{
			if (length != 4 || !(typeDefinitionId == "(BC)"))
			{
				goto IL_014f;
			}
			text = (isRecipe ? "BBL" : "BO");
		}
		else
		{
			switch (typeDefinitionId[1])
			{
			case 'F':
				break;
			case 'O':
				goto IL_007a;
			case 'B':
				goto IL_008f;
			case 'W':
				goto IL_00a4;
			case 'H':
				goto IL_00b9;
			case 'S':
				if (typeDefinitionId == "(S)")
				{
					goto IL_0147;
				}
				goto IL_014f;
			case 'P':
				if (typeDefinitionId == "(P)")
				{
					goto IL_0147;
				}
				goto IL_014f;
			default:
				goto IL_014f;
				IL_0147:
				text = "C";
				goto IL_0155;
			}
			if (!(typeDefinitionId == "(F)"))
			{
				goto IL_014f;
			}
			text = "F";
		}
		goto IL_0155;
		IL_014f:
		text = "";
		goto IL_0155;
		IL_0155:
		return text + delimiter + itemId + delimiter + stack;
		IL_00b9:
		if (!(typeDefinitionId == "(H)"))
		{
			goto IL_014f;
		}
		text = "H";
		goto IL_0155;
		IL_008f:
		if (!(typeDefinitionId == "(B)"))
		{
			goto IL_014f;
		}
		text = "B";
		goto IL_0155;
		IL_007a:
		if (!(typeDefinitionId == "(O)"))
		{
			goto IL_014f;
		}
		text = ((!isRing) ? (isRecipe ? "BL" : "O") : "R");
		goto IL_0155;
		IL_00a4:
		if (!(typeDefinitionId == "(W)"))
		{
			goto IL_014f;
		}
		text = "W";
		goto IL_0155;
	}

	public static TemporaryAnimatedSpriteList sparkleWithinArea(Microsoft.Xna.Framework.Rectangle bounds, int numberOfSparkles, Color sparkleColor, int delayBetweenSparkles = 100, int delayBeforeStarting = 0, string sparkleSound = "")
	{
		return getTemporarySpritesWithinArea(new int[2] { 10, 11 }, bounds, numberOfSparkles, sparkleColor, delayBetweenSparkles, delayBeforeStarting, sparkleSound);
	}

	public static TemporaryAnimatedSpriteList getTemporarySpritesWithinArea(int[] temporarySpriteRowNumbers, Microsoft.Xna.Framework.Rectangle bounds, int numberOfsprites, Color color, int delayBetweenSprites = 100, int delayBeforeStarting = 0, string sound = "")
	{
		TemporaryAnimatedSpriteList temporaryAnimatedSpriteList = new TemporaryAnimatedSpriteList();
		for (int i = 0; i < numberOfsprites; i++)
		{
			temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite(Game1.random.Choose(temporarySpriteRowNumbers), new Vector2(Game1.random.Next(bounds.X, bounds.Right), Game1.random.Next(bounds.Y, bounds.Bottom)), color)
			{
				delayBeforeAnimationStart = delayBeforeStarting + delayBetweenSprites * i,
				startSound = ((sound.Length > 0) ? sound : null)
			});
		}
		return temporaryAnimatedSpriteList;
	}

	public static Vector2 getAwayFromPlayerTrajectory(Microsoft.Xna.Framework.Rectangle monsterBox, Farmer who)
	{
		Point center = monsterBox.Center;
		Point standingPixel = who.StandingPixel;
		Vector2 result = new Vector2(-(standingPixel.X - center.X), standingPixel.Y - center.Y);
		if (result.Length() <= 0f)
		{
			switch (who.FacingDirection)
			{
			case 3:
				result = new Vector2(-1f, 0f);
				break;
			case 1:
				result = new Vector2(1f, 0f);
				break;
			case 0:
				result = new Vector2(0f, 1f);
				break;
			case 2:
				result = new Vector2(0f, -1f);
				break;
			}
		}
		result.Normalize();
		result.X *= 50 + Game1.random.Next(-20, 20);
		result.Y *= 50 + Game1.random.Next(-20, 20);
		return result;
	}

	/// <summary>Get the cue names that can be played from a jukebox for the current player.</summary>
	/// <param name="player">The player for whom to get music.</param>
	/// <param name="location">The location for whom to get music.</param>
	/// <remarks>See also <see cref="M:StardewValley.Utility.getSongTitleFromCueName(System.String)" />.</remarks>
	public static List<string> GetJukeboxTracks(Farmer player, GameLocation location)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, JukeboxTrackData> jukeboxTrackDatum in Game1.jukeboxTrackData)
		{
			List<string> alternativeTrackIds = jukeboxTrackDatum.Value.AlternativeTrackIds;
			if (alternativeTrackIds == null || alternativeTrackIds.Count <= 0)
			{
				continue;
			}
			foreach (string alternativeTrackId in jukeboxTrackDatum.Value.AlternativeTrackIds)
			{
				if (alternativeTrackId != null)
				{
					dictionary[alternativeTrackId] = jukeboxTrackDatum.Key;
				}
			}
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (KeyValuePair<string, JukeboxTrackData> jukeboxTrackDatum2 in Game1.jukeboxTrackData)
		{
			if (jukeboxTrackDatum2.Value.Available ?? false)
			{
				list.Add(jukeboxTrackDatum2.Key);
				hashSet.Add(jukeboxTrackDatum2.Key);
			}
		}
		foreach (string item in player.songsHeard)
		{
			string text = dictionary.GetValueOrDefault(item) ?? item;
			if (IsValidTrackName(text) && hashSet.Add(text) && (!Game1.jukeboxTrackData.TryGetValue(text, out var value) || !((!value.Available) ?? false)))
			{
				list.Add(text);
			}
		}
		return list;
	}

	/// <summary>Get whether an audio cue name is valid for the jukebox, regardless of whether it's disabled in <see cref="F:StardewValley.Game1.jukeboxTrackData" />.</summary>
	/// <param name="name">The audio cue name to check.</param>
	/// <remarks>This only checks whether the cue *could* be played by the jukebox. To check whether it's actually available, see <see cref="M:StardewValley.Utility.GetJukeboxTracks(StardewValley.Farmer,StardewValley.GameLocation)" />.</remarks>
	public static bool IsValidTrackName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}
		string text = name.ToLower();
		if (text.Contains("ambience") || text.Contains("ambient") || text.Contains("bigdrums") || text.Contains("clubloop"))
		{
			return false;
		}
		if (!Game1.soundBank.Exists(name))
		{
			return false;
		}
		return true;
	}

	/// <summary>Get the jukebox display name for a cue name.</summary>
	/// <param name="cueName">The cue name being played.</param>
	/// <remarks>See also <see cref="M:StardewValley.Utility.GetJukeboxTracks(StardewValley.Farmer,StardewValley.GameLocation)" />.</remarks>
	public static string getSongTitleFromCueName(string cueName)
	{
		if (!string.IsNullOrWhiteSpace(cueName))
		{
			string text = cueName.ToLowerInvariant();
			if (text == "turn_off")
			{
				return Game1.content.LoadString("Strings\\UI:Mini_JukeBox_Off");
			}
			if (text == "random")
			{
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:JukeboxRandomTrack");
			}
			if (Game1.jukeboxTrackData.TryGetValue(cueName, out var value))
			{
				return TokenParser.ParseText(value.Name) ?? cueName;
			}
			foreach (JukeboxTrackData value2 in Game1.jukeboxTrackData.Values)
			{
				if (value2.AlternativeTrackIds?.Contains<string>(cueName, StringComparer.OrdinalIgnoreCase) ?? false)
				{
					return TokenParser.ParseText(value2.Name) ?? cueName;
				}
			}
		}
		return cueName;
	}

	public static bool isOffScreenEndFunction(PathNode currentNode, Point endPoint, GameLocation location, Character c)
	{
		if (!isOnScreen(new Vector2(currentNode.x * 64, currentNode.y * 64), 32))
		{
			return true;
		}
		return false;
	}

	public static Vector2 getAwayFromPositionTrajectory(Microsoft.Xna.Framework.Rectangle monsterBox, Vector2 position)
	{
		float num = 0f - (position.X - (float)monsterBox.Center.X);
		float num2 = position.Y - (float)monsterBox.Center.Y;
		float num3 = Math.Abs(num) + Math.Abs(num2);
		if (num3 < 1f)
		{
			num3 = 5f;
		}
		float x = num / num3 * 20f;
		num2 = num2 / num3 * 20f;
		return new Vector2(x, num2);
	}

	public static bool tileWithinRadiusOfPlayer(int xTile, int yTile, int tileRadius, Farmer f)
	{
		Point point = new Point(xTile, yTile);
		Vector2 tile = f.Tile;
		if (Math.Abs((float)point.X - tile.X) <= (float)tileRadius)
		{
			return Math.Abs((float)point.Y - tile.Y) <= (float)tileRadius;
		}
		return false;
	}

	public static bool withinRadiusOfPlayer(int x, int y, int tileRadius, Farmer f)
	{
		Point point = new Point(x / 64, y / 64);
		Vector2 tile = f.Tile;
		if (Math.Abs((float)point.X - tile.X) <= (float)tileRadius)
		{
			return Math.Abs((float)point.Y - tile.Y) <= (float)tileRadius;
		}
		return false;
	}

	public static bool isThereAnObjectHereWhichAcceptsThisItem(GameLocation location, Item item, int x, int y)
	{
		if (item is Tool)
		{
			return false;
		}
		Vector2 vector = new Vector2(x / 64, y / 64);
		foreach (Building building in location.buildings)
		{
			if (building.occupiesTile(vector) && building.performActiveObjectDropInAction(Game1.player, probe: true))
			{
				return true;
			}
		}
		if (location.Objects.TryGetValue(vector, out var value) && value.heldObject.Value == null && value.performObjectDropInAction((Object)item, probe: true, Game1.player))
		{
			return true;
		}
		return false;
	}

	public static FarmAnimal getAnimal(long id)
	{
		FarmAnimal match = null;
		ForEachLocation(delegate(GameLocation location)
		{
			if (location.animals.TryGetValue(id, out var value))
			{
				match = value;
				return false;
			}
			return true;
		});
		return match;
	}

	public static bool isWallpaperOffLimitsForSale(string index)
	{
		if (index.StartsWith("MoreWalls"))
		{
			return true;
		}
		return false;
	}

	public static bool isFlooringOffLimitsForSale(string index)
	{
		return false;
	}

	/// <summary>Open a menu to buy items from a shop, if it exists, using the specified NPC regardless of whether they're present.</summary>
	/// <param name="shopId">The shop ID matching the entry in <c>Data/Shops</c>.</param>
	/// <param name="ownerName">The internal name of the NPC running the shop, or <c>null</c> to open the shop with no NPC portrait/dialogue.</param>
	/// <param name="playOpenSound">Whether to play the open-menu sound.</param>
	/// <returns>Returns whether the shop menu was opened.</returns>
	public static bool TryOpenShopMenu(string shopId, string ownerName, bool playOpenSound = true)
	{
		if (!DataLoader.Shops(Game1.content).TryGetValue(shopId, out var value))
		{
			return false;
		}
		if (!TryParseEnum<ShopOwnerType>(ownerName, out var ownerType))
		{
			ownerType = ShopOwnerType.NamedNpc;
		}
		ShopOwnerData[] source = ShopBuilder.GetCurrentOwners(value).ToArray();
		NPC owner;
		ShopOwnerData ownerData;
		switch (ownerType)
		{
		case ShopOwnerType.Any:
			owner = null;
			ownerData = source.FirstOrDefault((ShopOwnerData p) => p.Type == ownerType) ?? source.FirstOrDefault((ShopOwnerData p) => p.Type != ShopOwnerType.None);
			break;
		case ShopOwnerType.AnyOrNone:
			owner = null;
			ownerData = source.FirstOrDefault((ShopOwnerData p) => p.Type == ownerType) ?? source.FirstOrDefault();
			break;
		case ShopOwnerType.None:
			owner = null;
			ownerData = source.FirstOrDefault((ShopOwnerData p) => p.Type == ownerType) ?? source.FirstOrDefault((ShopOwnerData p) => p.Type == ShopOwnerType.AnyOrNone);
			break;
		default:
			if (ownerName == null)
			{
				owner = null;
				ownerData = source.FirstOrDefault((ShopOwnerData p) => p.Type == ShopOwnerType.AnyOrNone || p.Type == ShopOwnerType.None);
				break;
			}
			owner = Game1.getCharacterFromName(ownerName);
			ownerData = (from p in source
				orderby p.Type == ShopOwnerType.NamedNpc descending, p.Type != ShopOwnerType.None descending
				select p).FirstOrDefault((ShopOwnerData p) => p.IsValid(ownerName));
			break;
		}
		Game1.activeClickableMenu = new ShopMenu(shopId, value, ownerData, owner, null, null, playOpenSound);
		return true;
	}

	/// <summary>Open a menu to buy items from a shop, if it exists and an NPC who can run it is within the specified range.</summary>
	/// <param name="shopId">The shop ID matching the entry in <c>Data/Shops</c>.</param>
	/// <param name="location">The location in which to open the shop menu.</param>
	/// <param name="ownerArea">The tile area to search for an NPC who can run the shop (or <c>null</c> to search the entire location). If no NPC within the area matches the shop's <see cref="F:StardewValley.GameData.Shops.ShopData.Owners" />, the shop won't be opened (unless <paramref name="forceOpen" /> is <c>true</c>).</param>
	/// <param name="maxOwnerY">The maximum Y tile position for an owner NPC, or <c>null</c> for no maximum. This is used for shops that only work if the NPC is behind the counter.</param>
	/// <param name="forceOpen">Whether to open the menu regardless of whether an owner NPC was found.</param>
	/// <param name="playOpenSound">Whether to play the open-menu sound.</param>
	/// <param name="showClosedMessage">Custom logic to handle the closed message if it shouldn't be shown directly.</param>
	/// <returns>Returns whether the shop menu was opened.</returns>
	public static bool TryOpenShopMenu(string shopId, GameLocation location, Microsoft.Xna.Framework.Rectangle? ownerArea = null, int? maxOwnerY = null, bool forceOpen = false, bool playOpenSound = true, Action<string> showClosedMessage = null)
	{
		if (!DataLoader.Shops(Game1.content).TryGetValue(shopId, out var value))
		{
			return false;
		}
		IList<NPC> list = location.currentEvent?.actors;
		if (list == null)
		{
			list = location.characters;
		}
		NPC owner = null;
		ShopOwnerData shopOwnerData = null;
		ShopOwnerData[] array = ShopBuilder.GetCurrentOwners(value).ToArray();
		ShopOwnerData[] array2 = array;
		foreach (ShopOwnerData shopOwnerData2 in array2)
		{
			if (forceOpen && shopOwnerData2.ClosedMessage != null)
			{
				continue;
			}
			foreach (NPC item in list)
			{
				if (shopOwnerData2.IsValid(item.Name))
				{
					Point tilePoint = item.TilePoint;
					if ((!ownerArea.HasValue || ownerArea.Value.Contains(tilePoint)) && (!maxOwnerY.HasValue || tilePoint.Y <= maxOwnerY))
					{
						owner = item;
						shopOwnerData = shopOwnerData2;
						break;
					}
				}
			}
			if (shopOwnerData != null)
			{
				break;
			}
		}
		if (shopOwnerData == null)
		{
			shopOwnerData = array.FirstOrDefault((ShopOwnerData p) => (p.Type == ShopOwnerType.AnyOrNone || p.Type == ShopOwnerType.None) && (!forceOpen || p.ClosedMessage == null));
		}
		if (forceOpen && shopOwnerData == null)
		{
			array2 = array;
			foreach (ShopOwnerData shopOwnerData3 in array2)
			{
				if (shopOwnerData3.Type == ShopOwnerType.Any)
				{
					shopOwnerData = shopOwnerData3;
					owner = list.FirstOrDefault((NPC p) => p.IsVillager);
					if (owner == null)
					{
						ForEachVillager(delegate(NPC npc)
						{
							owner = npc;
							return false;
						});
					}
				}
				else
				{
					owner = Game1.getCharacterFromName(shopOwnerData3.Name);
					if (owner != null)
					{
						shopOwnerData = shopOwnerData3;
					}
				}
				if (shopOwnerData != null)
				{
					break;
				}
			}
		}
		if (shopOwnerData != null && shopOwnerData.ClosedMessage != null)
		{
			string text = TokenParser.ParseText(shopOwnerData.ClosedMessage);
			if (showClosedMessage != null)
			{
				showClosedMessage(text);
			}
			else
			{
				Game1.drawObjectDialogue(text);
			}
			return false;
		}
		if (shopOwnerData != null || forceOpen)
		{
			Game1.activeClickableMenu = new ShopMenu(shopId, value, shopOwnerData, owner);
			return true;
		}
		return false;
	}

	/// <summary>Apply a set of modifiers to a value.</summary>
	/// <param name="value">The base value to which to apply modifiers.</param>
	/// <param name="modifiers">The modifiers to apply.</param>
	/// <param name="mode">How multiple quantity modifiers should be combined.</param>
	/// <param name="location">The location for which to check queries, or <c>null</c> for the current location.</param>
	/// <param name="player">The player for which to check queries, or <c>null</c> for the current player.</param>
	/// <param name="targetItem">The target item (e.g. machine output or tree fruit) for which to check queries, or <c>null</c> if not applicable.</param>
	/// <param name="inputItem">The input item (e.g. machine input) for which to check queries, or <c>null</c> if not applicable.</param>
	/// <param name="random">The random number generator to use, or <c>null</c> for <see cref="F:StardewValley.Game1.random" />.</param>
	public static float ApplyQuantityModifiers(float value, IList<QuantityModifier> modifiers, QuantityModifier.QuantityModifierMode mode = QuantityModifier.QuantityModifierMode.Stack, GameLocation location = null, Farmer player = null, Item targetItem = null, Item inputItem = null, Random random = null)
	{
		if (modifiers == null || !modifiers.Any())
		{
			return value;
		}
		if (random == null)
		{
			random = Game1.random;
		}
		float? num = null;
		foreach (QuantityModifier modifier in modifiers)
		{
			float amount = modifier.Amount;
			List<float> randomAmount = modifier.RandomAmount;
			if (randomAmount != null && randomAmount.Any())
			{
				amount = random.ChooseFrom(modifier.RandomAmount);
			}
			if (!GameStateQuery.CheckConditions(modifier.Condition, location, player, targetItem, inputItem, random))
			{
				continue;
			}
			switch (mode)
			{
			case QuantityModifier.QuantityModifierMode.Minimum:
			{
				float num3 = QuantityModifier.Apply(value, modifier.Modification, amount);
				if (!num.HasValue || num3 < num)
				{
					num = num3;
				}
				break;
			}
			case QuantityModifier.QuantityModifierMode.Maximum:
			{
				float num2 = QuantityModifier.Apply(value, modifier.Modification, amount);
				if (!num.HasValue || num2 > num)
				{
					num = num2;
				}
				break;
			}
			default:
				num = QuantityModifier.Apply(num ?? value, modifier.Modification, amount);
				break;
			}
		}
		return num ?? value;
	}

	public static bool IsForbiddenDishOfTheDay(string id)
	{
		switch (id)
		{
		case "346":
		case "196":
		case "216":
		case "224":
		case "206":
		case "395":
			return true;
		default:
			return !ItemRegistry.Exists(id);
		}
	}

	public static bool removeLightSource([NotNullWhen(true)] string identifier)
	{
		if (identifier != null)
		{
			return Game1.currentLightSources.Remove(identifier);
		}
		return false;
	}

	public static Horse findHorseForPlayer(long uid)
	{
		Horse match = null;
		ForEachLocation(delegate(GameLocation location)
		{
			foreach (NPC character in location.characters)
			{
				if (character is Horse horse && horse.ownerId.Value == uid)
				{
					match = horse;
					return false;
				}
			}
			return true;
		}, includeInteriors: true, includeGenerated: true);
		return match;
	}

	public static Horse findHorse(Guid horseId)
	{
		Horse match = null;
		ForEachLocation(delegate(GameLocation location)
		{
			foreach (NPC character in location.characters)
			{
				if (character is Horse horse && horse.HorseId == horseId)
				{
					match = horse;
					return false;
				}
			}
			return true;
		}, includeInteriors: true, includeGenerated: true);
		return match;
	}

	public static void addDirtPuffs(GameLocation location, int tileX, int tileY, int tilesWide, int tilesHigh, int number = 5)
	{
		for (int i = tileX; i < tileX + tilesWide; i++)
		{
			for (int j = tileY; j < tileY + tilesHigh; j++)
			{
				for (int k = 0; k < number; k++)
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite(Game1.random.Choose(46, 12), new Vector2(i, j) * 64f + new Vector2(Game1.random.Next(-16, 32), Game1.random.Next(-16, 32)), Color.White, 10, Game1.random.NextBool())
					{
						delayBeforeAnimationStart = Math.Max(0, Game1.random.Next(-200, 400)),
						motion = new Vector2(0f, -1f),
						interval = Game1.random.Next(50, 80)
					});
				}
				location.temporarySprites.Add(new TemporaryAnimatedSprite(14, new Vector2(i, j) * 64f + new Vector2(Game1.random.Next(-16, 32), Game1.random.Next(-16, 32)), Color.White, 10, Game1.random.NextBool()));
			}
		}
	}

	public static void addSmokePuff(GameLocation l, Vector2 v, int delay = 0, float baseScale = 2f, float scaleChange = 0.02f, float alpha = 0.75f, float alphaFade = 0.002f)
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), v, flipped: false, alphaFade, Color.Gray);
		temporaryAnimatedSprite.alpha = alpha;
		temporaryAnimatedSprite.motion = new Vector2(0f, -0.5f);
		temporaryAnimatedSprite.acceleration = new Vector2(0.002f, 0f);
		temporaryAnimatedSprite.interval = 99999f;
		temporaryAnimatedSprite.layerDepth = 1f;
		temporaryAnimatedSprite.scale = baseScale;
		temporaryAnimatedSprite.scaleChange = scaleChange;
		temporaryAnimatedSprite.rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f;
		temporaryAnimatedSprite.delayBeforeAnimationStart = delay;
		l.temporarySprites.Add(temporaryAnimatedSprite);
	}

	public static LightSource getLightSource([NotNullWhen(true)] string identifier)
	{
		if (identifier == null || !Game1.currentLightSources.TryGetValue(identifier, out var value))
		{
			return null;
		}
		return value;
	}

	public static int SortAllFurnitures(Furniture a, Furniture b)
	{
		string qualifiedItemId = a.QualifiedItemId;
		string qualifiedItemId2 = b.QualifiedItemId;
		if (qualifiedItemId != qualifiedItemId2)
		{
			if (qualifiedItemId == "(F)1226" || qualifiedItemId == "(F)1308")
			{
				return -1;
			}
			if (qualifiedItemId2 == "(F)1226" || qualifiedItemId2 == "(F)1308")
			{
				return 1;
			}
		}
		if (a.furniture_type.Value != b.furniture_type.Value)
		{
			return a.furniture_type.Value.CompareTo(b.furniture_type.Value);
		}
		if (a.furniture_type.Value == 12 && b.furniture_type.Value == 12)
		{
			bool num = a.Name.StartsWith("Floor Divider ");
			bool flag = b.Name.StartsWith("Floor Divider ");
			if (num != flag)
			{
				if (flag)
				{
					return -1;
				}
				return 1;
			}
		}
		return a.ItemId.CompareTo(b.ItemId);
	}

	public static bool doesAnyFarmerHaveOrWillReceiveMail(string id)
	{
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.hasOrWillReceiveMail(id))
			{
				return true;
			}
		}
		return false;
	}

	public static string loadStringShort(string fileWithinStringsFolder, string key)
	{
		return Game1.content.LoadString("Strings\\" + fileWithinStringsFolder + ":" + key);
	}

	public static bool doesAnyFarmerHaveMail(string id)
	{
		if (Game1.player.mailReceived.Contains(id))
		{
			return true;
		}
		foreach (Farmer value in Game1.otherFarmers.Values)
		{
			if (value.mailReceived.Contains(id))
			{
				return true;
			}
		}
		return false;
	}

	public static FarmEvent pickFarmEvent()
	{
		return Game1.hooks.OnUtility_PickFarmEvent(delegate
		{
			Random random = CreateDaySaveRandom();
			for (int i = 0; i < 10; i++)
			{
				random.NextDouble();
			}
			if (Game1.weddingToday)
			{
				return (FarmEvent)null;
			}
			foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
			{
				Friendship spouseFriendship = onlineFarmer.GetSpouseFriendship();
				if (spouseFriendship != null && spouseFriendship.IsMarried() && spouseFriendship.WeddingDate == Game1.Date)
				{
					return (FarmEvent)null;
				}
			}
			if (Game1.stats.DaysPlayed == 31)
			{
				return new SoundInTheNightEvent(4);
			}
			if (Game1.MasterPlayer.mailForTomorrow.Contains("leoMoved%&NL&%") || Game1.MasterPlayer.mailForTomorrow.Contains("leoMoved"))
			{
				return new WorldChangeEvent(14);
			}
			if (Game1.player.mailForTomorrow.Contains("jojaPantry%&NL&%") || Game1.player.mailForTomorrow.Contains("jojaPantry"))
			{
				return new WorldChangeEvent(0);
			}
			if (Game1.player.mailForTomorrow.Contains("ccPantry%&NL&%") || Game1.player.mailForTomorrow.Contains("ccPantry"))
			{
				return new WorldChangeEvent(1);
			}
			if (Game1.player.mailForTomorrow.Contains("jojaVault%&NL&%") || Game1.player.mailForTomorrow.Contains("jojaVault"))
			{
				return new WorldChangeEvent(6);
			}
			if (Game1.player.mailForTomorrow.Contains("ccVault%&NL&%") || Game1.player.mailForTomorrow.Contains("ccVault"))
			{
				return new WorldChangeEvent(7);
			}
			if (Game1.player.mailForTomorrow.Contains("jojaBoilerRoom%&NL&%") || Game1.player.mailForTomorrow.Contains("jojaBoilerRoom"))
			{
				return new WorldChangeEvent(2);
			}
			if (Game1.player.mailForTomorrow.Contains("ccBoilerRoom%&NL&%") || Game1.player.mailForTomorrow.Contains("ccBoilerRoom"))
			{
				return new WorldChangeEvent(3);
			}
			if (Game1.player.mailForTomorrow.Contains("jojaCraftsRoom%&NL&%") || Game1.player.mailForTomorrow.Contains("jojaCraftsRoom"))
			{
				return new WorldChangeEvent(4);
			}
			if (Game1.player.mailForTomorrow.Contains("ccCraftsRoom%&NL&%") || Game1.player.mailForTomorrow.Contains("ccCraftsRoom"))
			{
				return new WorldChangeEvent(5);
			}
			if (Game1.player.mailForTomorrow.Contains("jojaFishTank%&NL&%") || Game1.player.mailForTomorrow.Contains("jojaFishTank"))
			{
				return new WorldChangeEvent(8);
			}
			if (Game1.player.mailForTomorrow.Contains("ccFishTank%&NL&%") || Game1.player.mailForTomorrow.Contains("ccFishTank"))
			{
				return new WorldChangeEvent(9);
			}
			if (Game1.player.mailForTomorrow.Contains("ccMovieTheaterJoja%&NL&%") || Game1.player.mailForTomorrow.Contains("jojaMovieTheater"))
			{
				return new WorldChangeEvent(10);
			}
			if (Game1.player.mailForTomorrow.Contains("ccMovieTheater%&NL&%") || Game1.player.mailForTomorrow.Contains("ccMovieTheater"))
			{
				return new WorldChangeEvent(11);
			}
			if (Game1.MasterPlayer.eventsSeen.Contains("191393") && (Game1.isRaining || Game1.isLightning) && !Game1.MasterPlayer.mailReceived.Contains("abandonedJojaMartAccessible") && !Game1.MasterPlayer.mailReceived.Contains("ccMovieTheater"))
			{
				return new WorldChangeEvent(12);
			}
			if (Game1.MasterPlayer.hasOrWillReceiveMail("willyBoatTicketMachine") && Game1.MasterPlayer.hasOrWillReceiveMail("willyBoatHull") && Game1.MasterPlayer.hasOrWillReceiveMail("willyBoatAnchor") && !Game1.MasterPlayer.hasOrWillReceiveMail("willyBoatFixed"))
			{
				return new WorldChangeEvent(13);
			}
			if (Game1.MasterPlayer.hasOrWillReceiveMail("activateGoldenParrotsTonight") && !Game1.netWorldState.Value.ActivatedGoldenParrot)
			{
				return new WorldChangeEvent(15);
			}
			if (Game1.player.mailReceived.Contains("ccPantry") && random.NextDouble() < 0.1 && !Game1.MasterPlayer.mailReceived.Contains("raccoonTreeFallen"))
			{
				return new SoundInTheNightEvent(5);
			}
			if (!Game1.player.mailReceived.Contains("sawQiPlane"))
			{
				foreach (Farmer onlineFarmer2 in Game1.getOnlineFarmers())
				{
					if (onlineFarmer2.mailReceived.Contains("gotFirstBillboardPrizeTicket") || Game1.stats.DaysPlayed > 50)
					{
						return new QiPlaneEvent();
					}
				}
			}
			double num = (Game1.getFarm().hasMatureFairyRoseTonight ? 0.007 : 0.0);
			Game1.getFarm().hasMatureFairyRoseTonight = false;
			if (random.NextDouble() < 0.01 + num && !Game1.IsWinter && Game1.dayOfMonth != 1)
			{
				return new FairyEvent();
			}
			if (random.NextDouble() < 0.01 && Game1.stats.DaysPlayed > 20)
			{
				return new WitchEvent();
			}
			if (random.NextDouble() < 0.01 && Game1.stats.DaysPlayed > 5)
			{
				return new SoundInTheNightEvent(1);
			}
			if (random.NextDouble() < 0.005)
			{
				return new SoundInTheNightEvent(3);
			}
			if (random.NextDouble() < 0.008 && Game1.year > 1 && !Game1.MasterPlayer.mailReceived.Contains("Got_Capsule"))
			{
				Game1.player.team.RequestSetMail(PlayerActionTarget.Host, "Got_Capsule", MailType.Received, add: true);
				return new SoundInTheNightEvent(0);
			}
			return (FarmEvent)null;
		});
	}

	public static bool hasFinishedJojaRoute()
	{
		bool flag = false;
		if (Game1.MasterPlayer.mailReceived.Contains("jojaVault"))
		{
			flag = true;
		}
		else if (!Game1.MasterPlayer.mailReceived.Contains("ccVault"))
		{
			return false;
		}
		if (Game1.MasterPlayer.mailReceived.Contains("jojaPantry"))
		{
			flag = true;
		}
		else if (!Game1.MasterPlayer.mailReceived.Contains("ccPantry"))
		{
			return false;
		}
		if (Game1.MasterPlayer.mailReceived.Contains("jojaBoilerRoom"))
		{
			flag = true;
		}
		else if (!Game1.MasterPlayer.mailReceived.Contains("ccBoilerRoom"))
		{
			return false;
		}
		if (Game1.MasterPlayer.mailReceived.Contains("jojaCraftsRoom"))
		{
			flag = true;
		}
		else if (!Game1.MasterPlayer.mailReceived.Contains("ccCraftsRoom"))
		{
			return false;
		}
		if (Game1.MasterPlayer.mailReceived.Contains("jojaFishTank"))
		{
			flag = true;
		}
		else if (!Game1.MasterPlayer.mailReceived.Contains("ccFishTank"))
		{
			return false;
		}
		if (flag || Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
		{
			return true;
		}
		return false;
	}

	public static FarmEvent pickPersonalFarmEvent()
	{
		Random random = CreateRandom(Game1.stats.DaysPlayed, Game1.uniqueIDForThisGame / 2, 470124797.0, Game1.player.UniqueMultiplayerID);
		if (Game1.weddingToday)
		{
			return null;
		}
		NPC spouse = Game1.player.getSpouse();
		bool flag = Game1.player.isMarriedOrRoommates();
		if (flag && Game1.player.GetSpouseFriendship().DaysUntilBirthing <= 0 && Game1.player.GetSpouseFriendship().NextBirthingDate != null)
		{
			if (spouse != null)
			{
				return new BirthingEvent();
			}
			long value = Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).Value;
			if (Game1.otherFarmers.ContainsKey(value))
			{
				return new PlayerCoupleBirthingEvent();
			}
		}
		else
		{
			if (flag)
			{
				bool? flag2 = spouse?.canGetPregnant();
				if (flag2.HasValue && flag2 == true && Game1.player.currentLocation == Game1.getLocationFromName(Game1.player.homeLocation.Value) && random.NextDouble() < 0.05 && GameStateQuery.CheckConditions(spouse.GetData()?.SpouseWantsChildren))
				{
					return new QuestionEvent(1);
				}
			}
			if (flag && Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).HasValue && Game1.player.GetSpouseFriendship().NextBirthingDate == null && random.NextDouble() < 0.05)
			{
				long value2 = Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).Value;
				if (Game1.otherFarmers.TryGetValue(value2, out var value3))
				{
					Farmer farmer = value3;
					if (farmer.currentLocation == Game1.player.currentLocation && (farmer.currentLocation == Game1.getLocationFromName(farmer.homeLocation.Value) || farmer.currentLocation == Game1.getLocationFromName(Game1.player.homeLocation.Value)) && playersCanGetPregnantHere(farmer.currentLocation as FarmHouse))
					{
						return new QuestionEvent(3);
					}
				}
			}
		}
		if (random.NextBool())
		{
			return new QuestionEvent(2);
		}
		return new SoundInTheNightEvent(2);
	}

	public static bool playersCanGetPregnantHere(FarmHouse farmHouse)
	{
		List<Child> children = farmHouse.getChildren();
		if (farmHouse.cribStyle.Value <= 0)
		{
			return false;
		}
		if (farmHouse.getChildrenCount() < 2 && farmHouse.upgradeLevel >= 2 && children.Count < 2)
		{
			if (children.Count != 0)
			{
				return children[0].Age > 2;
			}
			return true;
		}
		return false;
	}

	public static string capitalizeFirstLetter(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		return s[0].ToString().ToUpper() + ((s.Length > 1) ? s.Substring(1) : "");
	}

	public static void repositionLightSource([NotNullWhen(true)] string identifier, Vector2 position)
	{
		if (identifier != null && Game1.currentLightSources.TryGetValue(identifier, out var value))
		{
			value.position.Value = position;
		}
	}

	public static bool areThereAnyOtherAnimalsWithThisName(string name)
	{
		bool found = false;
		if (name != null)
		{
			ForEachLocation(delegate(GameLocation location)
			{
				foreach (FarmAnimal value in location.animals.Values)
				{
					if (value.displayName == name)
					{
						found = true;
						return false;
					}
				}
				return true;
			});
		}
		return found;
	}

	public static string getNumberWithCommas(int number)
	{
		StringBuilder stringBuilder = new StringBuilder(number.ToString() ?? "");
		string value;
		switch (LocalizedContentManager.CurrentLanguageCode)
		{
		case LocalizedContentManager.LanguageCode.pt:
		case LocalizedContentManager.LanguageCode.es:
		case LocalizedContentManager.LanguageCode.de:
		case LocalizedContentManager.LanguageCode.hu:
			value = ".";
			break;
		case LocalizedContentManager.LanguageCode.ru:
			value = " ";
			break;
		case LocalizedContentManager.LanguageCode.mod:
			value = LocalizedContentManager.CurrentModLanguage?.NumberComma ?? ",";
			break;
		default:
			value = ",";
			break;
		}
		for (int num = stringBuilder.Length - 4; num >= 0; num -= 3)
		{
			stringBuilder.Insert(num + 1, value);
		}
		return stringBuilder.ToString();
	}

	protected static bool _HasBuildingOrUpgrade(GameLocation location, string buildingId)
	{
		if (location.getNumberBuildingsConstructed(buildingId) > 0)
		{
			return true;
		}
		foreach (KeyValuePair<string, BuildingData> buildingDatum in Game1.buildingData)
		{
			string key = buildingDatum.Key;
			BuildingData value = buildingDatum.Value;
			if (!(key == buildingId) && value.BuildingToUpgrade == buildingId && _HasBuildingOrUpgrade(location, key))
			{
				return true;
			}
		}
		return false;
	}

	public static List<int> getDaysOfBooksellerThisSeason()
	{
		Random random = CreateRandom(Game1.year * 11, Game1.uniqueIDForThisGame, Game1.seasonIndex);
		int[] array = null;
		List<int> list = new List<int>();
		switch (Game1.season)
		{
		case Season.Spring:
			array = new int[5] { 11, 12, 21, 22, 25 };
			break;
		case Season.Summer:
			array = new int[5] { 9, 12, 18, 25, 27 };
			break;
		case Season.Fall:
			array = new int[8] { 4, 7, 8, 9, 12, 19, 22, 25 };
			break;
		case Season.Winter:
			array = new int[6] { 5, 11, 12, 19, 22, 24 };
			break;
		}
		int num = random.Next(array.Length);
		list.Add(array[num]);
		list.Add(array[(num + array.Length / 2) % array.Length]);
		return list;
	}

	/// <summary>Get whether there's green rain scheduled for today.</summary>
	public static bool isGreenRainDay()
	{
		return isGreenRainDay(Game1.dayOfMonth, Game1.season);
	}

	/// <summary>Get whether there's green rain scheduled on the given day.</summary>
	/// <param name="day">The day of month to check.</param>
	/// <param name="season">The season key to check.</param>
	public static bool isGreenRainDay(int day, Season season)
	{
		if (season == Season.Summer)
		{
			Random random = CreateRandom(Game1.year * 777, Game1.uniqueIDForThisGame);
			int[] options = new int[8] { 5, 6, 7, 14, 15, 16, 18, 23 };
			return day == random.ChooseFrom(options);
		}
		return false;
	}

	public static List<Object> getPurchaseAnimalStock(GameLocation location)
	{
		List<Object> list = new List<Object>();
		foreach (KeyValuePair<string, FarmAnimalData> farmAnimalDatum in Game1.farmAnimalData)
		{
			FarmAnimalData value = farmAnimalDatum.Value;
			if (value.PurchasePrice >= 0 && GameStateQuery.CheckConditions(value.UnlockCondition))
			{
				Object obj = new Object("100", 1, isRecipe: false, value.PurchasePrice)
				{
					Name = farmAnimalDatum.Key,
					Type = null
				};
				if (value.RequiredBuilding != null && !_HasBuildingOrUpgrade(location, value.RequiredBuilding))
				{
					obj.Type = ((value.ShopMissingBuildingDescription == null) ? "" : TokenParser.ParseText(value.ShopMissingBuildingDescription));
				}
				obj.displayNameFormat = value.ShopDisplayName;
				list.Add(obj);
			}
		}
		return list;
	}

	public static string SanitizeName(string name)
	{
		return Regex.Replace(name, "[^a-zA-Z0-9]", string.Empty);
	}

	public static void FixChildNameCollisions()
	{
		List<NPC> allCharacters = getAllCharacters();
		foreach (NPC item in allCharacters)
		{
			if (!(item is Child))
			{
				continue;
			}
			string name = item.Name;
			string text = item.Name;
			bool flag;
			do
			{
				flag = false;
				if (Game1.characterData.ContainsKey(text))
				{
					text += " ";
					flag = true;
					continue;
				}
				foreach (NPC item2 in allCharacters)
				{
					if (item2 != item && item2.name.Equals(text))
					{
						text += " ";
						flag = true;
					}
				}
			}
			while (flag);
			if (!(text != item.Name))
			{
				continue;
			}
			item.Name = text;
			item.displayName = null;
			foreach (Farmer allFarmer in Game1.getAllFarmers())
			{
				if (allFarmer.friendshipData != null && allFarmer.friendshipData.TryGetValue(name, out var value))
				{
					allFarmer.friendshipData[text] = value;
					allFarmer.friendshipData.Remove(name);
				}
			}
		}
	}

	public static Vector2 getCornersOfThisRectangle(ref Microsoft.Xna.Framework.Rectangle r, int corner)
	{
		return corner switch
		{
			1 => new Vector2(r.Right - 1, r.Y), 
			2 => new Vector2(r.Right - 1, r.Bottom - 1), 
			3 => new Vector2(r.X, r.Bottom - 1), 
			_ => new Vector2(r.X, r.Y), 
		};
	}

	public static Vector2 GetCurvePoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
	{
		float num = 3f * (p1.X - p0.X);
		float num2 = 3f * (p1.Y - p0.Y);
		float num3 = 3f * (p2.X - p1.X) - num;
		float num4 = 3f * (p2.Y - p1.Y) - num2;
		float num5 = p3.X - p0.X - num - num3;
		float num6 = p3.Y - p0.Y - num2 - num4;
		float num7 = t * t * t;
		float num8 = t * t;
		float x = num5 * num7 + num3 * num8 + num * t + p0.X;
		float y = num6 * num7 + num4 * num8 + num2 * t + p0.Y;
		return new Vector2(x, y);
	}

	public static GameLocation getGameLocationOfCharacter(NPC n)
	{
		return n.currentLocation;
	}

	public static int[] parseStringToIntArray(string s, char delimiter = ' ')
	{
		string[] array = s.Split(delimiter);
		int[] array2 = new int[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = Convert.ToInt32(array[i]);
		}
		return array2;
	}

	public static void drawLineWithScreenCoordinates(int x1, int y1, int x2, int y2, SpriteBatch b, Color color1, float layerDepth = 1f, int thickness = 1)
	{
		Vector2 vector = new Vector2(x2, y2);
		Vector2 vector2 = new Vector2(x1, y1);
		Vector2 vector3 = vector - vector2;
		float rotation = (float)Math.Atan2(vector3.Y, vector3.X);
		b.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((int)vector2.X, (int)vector2.Y, (int)vector3.Length(), thickness), null, color1, rotation, new Vector2(0f, 0f), SpriteEffects.None, layerDepth);
		b.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((int)vector2.X, (int)vector2.Y + 1, (int)vector3.Length(), thickness), null, color1, rotation, new Vector2(0f, 0f), SpriteEffects.None, layerDepth);
	}

	public static Farmer isThereAFarmerWithinDistance(Vector2 tileLocation, int tilesAway, GameLocation location)
	{
		return GetPlayersWithinDistance(tileLocation, tilesAway, location).FirstOrDefault();
	}

	public static Character isThereAFarmerOrCharacterWithinDistance(Vector2 tileLocation, int tilesAway, GameLocation environment)
	{
		Character character = GetNpcsWithinDistance(tileLocation, tilesAway, environment).FirstOrDefault();
		if (character == null)
		{
			character = GetPlayersWithinDistance(tileLocation, tilesAway, environment).FirstOrDefault();
		}
		return character;
	}

	/// <summary>Get all NPCs within a given distance of a tile.</summary>
	/// <param name="centerTile">The tile location around which to find NPCs.</param>
	/// <param name="tilesAway">The maximum tile distance (including diagonal) within which to match NPCs.</param>
	/// <param name="location">The location to search.</param>
	public static IEnumerable<NPC> GetNpcsWithinDistance(Vector2 centerTile, int tilesAway, GameLocation location)
	{
		foreach (NPC character in location.characters)
		{
			if (Vector2.Distance(character.Tile, centerTile) <= (float)tilesAway)
			{
				yield return character;
			}
		}
	}

	/// <summary>Get all players within a given distance of a tile.</summary>
	/// <param name="centerTile">The tile location around which to find NPCs.</param>
	/// <param name="tilesAway">The maximum tile distance (including diagonal) within which to match NPCs.</param>
	/// <param name="location">The location to search.</param>
	public static IEnumerable<Farmer> GetPlayersWithinDistance(Vector2 centerTile, int tilesAway, GameLocation location)
	{
		foreach (Farmer farmer in location.farmers)
		{
			if (Vector2.Distance(farmer.Tile, centerTile) <= (float)tilesAway)
			{
				yield return farmer;
			}
		}
	}

	public static Color getRedToGreenLerpColor(float power)
	{
		return new Color((int)((power <= 0.5f) ? 255f : ((1f - power) * 2f * 255f)), (int)Math.Min(255f, power * 2f * 255f), 0);
	}

	public static FarmHouse getHomeOfFarmer(Farmer who)
	{
		return Game1.RequireLocation<FarmHouse>(who.homeLocation.Value);
	}

	public static Vector2 getRandomPositionOnScreen()
	{
		return new Vector2(Game1.random.Next(Game1.viewport.Width), Game1.random.Next(Game1.viewport.Height));
	}

	public static Vector2 getRandomPositionOnScreenNotOnMap()
	{
		Vector2 vector = Vector2.Zero;
		int i;
		for (i = 0; i < 30; i++)
		{
			if (!vector.Equals(Vector2.Zero) && !Game1.currentLocation.isTileOnMap((vector + new Vector2(Game1.viewport.X, Game1.viewport.Y)) / 64f))
			{
				break;
			}
			vector = getRandomPositionOnScreen();
		}
		if (i >= 30)
		{
			return new Vector2(-1000f, -1000f);
		}
		return vector;
	}

	public static Microsoft.Xna.Framework.Rectangle getRectangleCenteredAt(Vector2 v, int size)
	{
		return new Microsoft.Xna.Framework.Rectangle((int)v.X - size / 2, (int)v.Y - size / 2, size, size);
	}

	public static bool checkForCharacterInteractionAtTile(Vector2 tileLocation, Farmer who)
	{
		NPC nPC = Game1.currentLocation.isCharacterAtTile(tileLocation);
		if (nPC != null && !nPC.IsMonster && !nPC.IsInvisible)
		{
			if (nPC.SimpleNonVillagerNPC && nPC.nonVillagerNPCTimesTalked != -1)
			{
				Game1.mouseCursor = Game1.cursor_talk;
			}
			else if (Game1.currentLocation is MovieTheater)
			{
				Game1.mouseCursor = Game1.cursor_talk;
			}
			else if (nPC.Name == "Pierre" && who.ActiveObject?.QualifiedItemId == "(O)897" && nPC.tryToReceiveActiveObject(who, probe: true))
			{
				Game1.mouseCursor = Game1.cursor_gift;
			}
			else
			{
				bool? flag = who.ActiveItem?.canBeGivenAsGift();
				if (flag.HasValue && flag == true && nPC.CanReceiveGifts() && !who.isRidingHorse() && who.friendshipData.ContainsKey(nPC.Name) && !Game1.eventUp)
				{
					Game1.mouseCursor = (nPC.tryToReceiveActiveObject(who, probe: true) ? Game1.cursor_gift : Game1.cursor_default);
				}
				else if (nPC.canTalk())
				{
					if (nPC.CurrentDialogue == null || nPC.CurrentDialogue.Count <= 0)
					{
						if (Game1.player.spouse != null && nPC.Name != null && nPC.Name == Game1.player.spouse && nPC.shouldSayMarriageDialogue.Value)
						{
							NetList<MarriageDialogueReference, NetRef<MarriageDialogueReference>> currentMarriageDialogue = nPC.currentMarriageDialogue;
							if (currentMarriageDialogue != null && currentMarriageDialogue.Count > 0)
							{
								goto IL_01fb;
							}
						}
						if (!nPC.hasTemporaryMessageAvailable() && (!who.hasClubCard || !nPC.Name.Equals("Bouncer") || !who.IsLocalPlayer) && (!nPC.Name.Equals("Henchman") || !nPC.currentLocation.Name.Equals("WitchSwamp") || who.hasOrWillReceiveMail("henchmanGone")))
						{
							goto IL_020d;
						}
					}
					goto IL_01fb;
				}
			}
			goto IL_020d;
		}
		return false;
		IL_020d:
		if (Game1.eventUp && Game1.CurrentEvent != null && !Game1.CurrentEvent.playerControlSequence)
		{
			Game1.mouseCursor = Game1.cursor_default;
		}
		Game1.currentLocation.checkForSpecialCharacterIconAtThisTile(tileLocation);
		if (Game1.mouseCursor == Game1.cursor_gift || Game1.mouseCursor == Game1.cursor_talk)
		{
			if (tileWithinRadiusOfPlayer((int)tileLocation.X, (int)tileLocation.Y, 1, who))
			{
				Game1.mouseCursorTransparency = 1f;
			}
			else
			{
				Game1.mouseCursorTransparency = 0.5f;
			}
		}
		return true;
		IL_01fb:
		if (!nPC.isOnSilentTemporaryMessage())
		{
			Game1.mouseCursor = Game1.cursor_talk;
		}
		goto IL_020d;
	}

	public static bool canGrabSomethingFromHere(int x, int y, Farmer who)
	{
		if (Game1.currentLocation == null)
		{
			return false;
		}
		Vector2 vector = new Vector2(x / 64, y / 64);
		if (Game1.currentLocation.isObjectAt(x, y))
		{
			Game1.currentLocation.getObjectAt(x, y).hoverAction();
		}
		if (checkForCharacterInteractionAtTile(vector, who))
		{
			return false;
		}
		if (checkForCharacterInteractionAtTile(vector + new Vector2(0f, 1f), who))
		{
			return false;
		}
		if (who.IsLocalPlayer)
		{
			if (who.onBridge.Value)
			{
				return false;
			}
			if (Game1.currentLocation != null)
			{
				foreach (Furniture item in Game1.currentLocation.furniture)
				{
					if (item.GetBoundingBox().Contains(Vector2ToPoint(vector * 64f)) && item.IsTable() && item.heldObject.Value != null)
					{
						return true;
					}
				}
			}
			TerrainFeature value2;
			if (Game1.currentLocation.Objects.TryGetValue(vector, out var value))
			{
				if (value.readyForHarvest.Value || value.isSpawnedObject.Value || (value is IndoorPot indoorPot && indoorPot.hoeDirt.Value.readyForHarvest()))
				{
					Game1.mouseCursor = Game1.cursor_harvest;
					if (!withinRadiusOfPlayer(x, y, 1, who))
					{
						Game1.mouseCursorTransparency = 0.5f;
						return false;
					}
					return true;
				}
			}
			else if (Game1.currentLocation.terrainFeatures.TryGetValue(vector, out value2) && value2 is HoeDirt hoeDirt && hoeDirt.readyForHarvest())
			{
				Game1.mouseCursor = Game1.cursor_harvest;
				if (!withinRadiusOfPlayer(x, y, 1, who))
				{
					Game1.mouseCursorTransparency = 0.5f;
					return false;
				}
				return true;
			}
		}
		return false;
	}

	public static int getStringCountInList(List<string> strings, string whichStringToCheck)
	{
		int num = 0;
		if (strings != null)
		{
			foreach (string @string in strings)
			{
				if (@string == whichStringToCheck)
				{
					num++;
				}
			}
		}
		return num;
	}

	public static Microsoft.Xna.Framework.Rectangle getSourceRectWithinRectangularRegion(int regionX, int regionY, int regionWidth, int sourceIndex, int sourceWidth, int sourceHeight)
	{
		int num = regionWidth / sourceWidth;
		return new Microsoft.Xna.Framework.Rectangle(regionX + sourceIndex % num * sourceWidth, regionY + sourceIndex / num * sourceHeight, sourceWidth, sourceHeight);
	}

	public static void drawWithShadow(SpriteBatch b, Texture2D texture, Vector2 position, Microsoft.Xna.Framework.Rectangle sourceRect, Color color, float rotation, Vector2 origin, float scale = -1f, bool flipped = false, float layerDepth = -1f, int horizontalShadowOffset = -1, int verticalShadowOffset = -1, float shadowIntensity = 0.35f)
	{
		if (scale == -1f)
		{
			scale = 4f;
		}
		if (layerDepth == -1f)
		{
			layerDepth = position.Y / 10000f;
		}
		if (horizontalShadowOffset == -1)
		{
			horizontalShadowOffset = -4;
		}
		if (verticalShadowOffset == -1)
		{
			verticalShadowOffset = 4;
		}
		b.Draw(texture, position + new Vector2(horizontalShadowOffset, verticalShadowOffset), sourceRect, Color.Black * shadowIntensity * ((float)(int)color.A / 255f), rotation, origin, scale, flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth - 0.0001f);
		b.Draw(texture, position, sourceRect, color, rotation, origin, scale, flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
	}

	public static void drawTextWithShadow(SpriteBatch b, StringBuilder text, SpriteFont font, Vector2 position, Color color, float scale = 1f, float layerDepth = -1f, int horizontalShadowOffset = -1, int verticalShadowOffset = -1, float shadowIntensity = 1f, int numShadows = 3)
	{
		if (layerDepth == -1f)
		{
			layerDepth = position.Y / 10000f;
		}
		bool flag = Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ru || Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.de;
		if (horizontalShadowOffset == -1)
		{
			horizontalShadowOffset = ((font.Equals(Game1.smallFont) || flag) ? (-2) : (-3));
		}
		if (verticalShadowOffset == -1)
		{
			verticalShadowOffset = ((font.Equals(Game1.smallFont) || flag) ? 2 : 3);
		}
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		b.DrawString(font, text, position + new Vector2(horizontalShadowOffset, verticalShadowOffset), Game1.textShadowDarkerColor * shadowIntensity, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth - 0.0001f);
		switch (numShadows)
		{
		case 2:
			b.DrawString(font, text, position + new Vector2(horizontalShadowOffset, 0f), Game1.textShadowDarkerColor * shadowIntensity, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth - 0.0002f);
			break;
		case 3:
			b.DrawString(font, text, position + new Vector2(0f, verticalShadowOffset), Game1.textShadowDarkerColor * shadowIntensity, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth - 0.0003f);
			break;
		}
		b.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
	}

	public static void drawTextWithShadow(SpriteBatch b, string text, SpriteFont font, Vector2 position, Color color, float scale = 1f, float layerDepth = -1f, int horizontalShadowOffset = -1, int verticalShadowOffset = -1, float shadowIntensity = 1f, int numShadows = 3)
	{
		if (layerDepth == -1f)
		{
			layerDepth = position.Y / 10000f;
		}
		bool flag = Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ru || Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.de || Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ko;
		if (horizontalShadowOffset == -1)
		{
			horizontalShadowOffset = ((font.Equals(Game1.smallFont) || flag) ? (-2) : (-3));
		}
		if (verticalShadowOffset == -1)
		{
			verticalShadowOffset = ((font.Equals(Game1.smallFont) || flag) ? 2 : 3);
		}
		if (text == null)
		{
			text = "";
		}
		b.DrawString(font, text, position + new Vector2(horizontalShadowOffset, verticalShadowOffset), Game1.textShadowDarkerColor * shadowIntensity, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth - 0.0001f);
		switch (numShadows)
		{
		case 2:
			b.DrawString(font, text, position + new Vector2(horizontalShadowOffset, 0f), Game1.textShadowDarkerColor * shadowIntensity, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth - 0.0002f);
			break;
		case 3:
			b.DrawString(font, text, position + new Vector2(0f, verticalShadowOffset), Game1.textShadowDarkerColor * shadowIntensity, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth - 0.0003f);
			break;
		}
		b.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
	}

	public static void drawTextWithColoredShadow(SpriteBatch b, string text, SpriteFont font, Vector2 position, Color color, Color shadowColor, float scale = 1f, float layerDepth = -1f, int horizontalShadowOffset = -1, int verticalShadowOffset = -1, int numShadows = 3)
	{
		if (layerDepth == -1f)
		{
			layerDepth = position.Y / 10000f;
		}
		bool flag = Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ru || Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.de;
		if (horizontalShadowOffset == -1)
		{
			horizontalShadowOffset = ((font.Equals(Game1.smallFont) || flag) ? (-2) : (-3));
		}
		if (verticalShadowOffset == -1)
		{
			verticalShadowOffset = ((font.Equals(Game1.smallFont) || flag) ? 2 : 3);
		}
		if (text == null)
		{
			text = "";
		}
		b.DrawString(font, text, position + new Vector2(horizontalShadowOffset, verticalShadowOffset), shadowColor, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth - 0.0001f);
		switch (numShadows)
		{
		case 2:
			b.DrawString(font, text, position + new Vector2(horizontalShadowOffset, 0f), shadowColor, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth - 0.0002f);
			break;
		case 3:
			b.DrawString(font, text, position + new Vector2(0f, verticalShadowOffset), shadowColor, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth - 0.0003f);
			break;
		}
		b.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
	}

	public static void drawBoldText(SpriteBatch b, string text, SpriteFont font, Vector2 position, Color color, float scale = 1f, float layerDepth = -1f, int boldnessOffset = 1)
	{
		if (layerDepth == -1f)
		{
			layerDepth = position.Y / 10000f;
		}
		b.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
		b.DrawString(font, text, position + new Vector2(boldnessOffset, 0f), color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
		b.DrawString(font, text, position + new Vector2(boldnessOffset, boldnessOffset), color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
		b.DrawString(font, text, position + new Vector2(0f, boldnessOffset), color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
	}

	protected static bool _HasNonMousePlacementLeeway(int x, int y, Item item, Farmer f)
	{
		if (!Game1.isCheckingNonMousePlacement)
		{
			return false;
		}
		Point tilePoint = f.TilePoint;
		if (!withinRadiusOfPlayer(x, y, 2, f))
		{
			return false;
		}
		if (item.Category == -74)
		{
			return true;
		}
		foreach (Point item2 in GetPointsOnLine(tilePoint.X, tilePoint.Y, x / 64, y / 64))
		{
			if (!(item2 == tilePoint) && !item.canBePlacedHere(f.currentLocation, new Vector2(item2.X, item2.Y), ~(CollisionMask.Characters | CollisionMask.Farmers)))
			{
				return false;
			}
		}
		return true;
	}

	public static bool isPlacementForbiddenHere(GameLocation location)
	{
		if (location == null)
		{
			return true;
		}
		return isPlacementForbiddenHere(location.name.Value);
	}

	public static bool TryGetPassiveFestivalData(string festivalId, out PassiveFestivalData data)
	{
		if (festivalId == null)
		{
			data = null;
			return false;
		}
		return DataLoader.PassiveFestivals(Game1.content).TryGetValue(festivalId, out data);
	}

	/// <summary>Get the passive festival which is active on a given date.</summary>
	/// <param name="dayOfMonth">The day of month to check.</param>
	/// <param name="season">The season to check.</param>
	/// <param name="locationContextId">The location context to check, or <c>null</c> for any location context.</param>
	/// <param name="id">The passive festival ID, if found.</param>
	/// <param name="data">The passive festival data, if found.</param>
	/// <param name="ignoreConditionsCheck">Whether to ignore the custom passive festival conditions, if any.</param>
	public static bool TryGetPassiveFestivalDataForDay(int dayOfMonth, Season season, string locationContextId, out string id, out PassiveFestivalData data, bool ignoreConditionsCheck = false)
	{
		bool flag = true;
		ICollection<string> collection;
		if (dayOfMonth == Game1.dayOfMonth && season == Game1.season)
		{
			collection = Game1.netWorldState.Value.ActivePassiveFestivals;
			flag = false;
		}
		else
		{
			collection = DataLoader.PassiveFestivals(Game1.content).Keys;
		}
		foreach (string item in collection)
		{
			id = item;
			if (!TryGetPassiveFestivalData(id, out data) || (flag && (dayOfMonth < data.StartDay || dayOfMonth > data.EndDay || season != data.Season || (!ignoreConditionsCheck && !GameStateQuery.CheckConditions(data.Condition)))))
			{
				continue;
			}
			if (locationContextId != null)
			{
				if (data.MapReplacements == null)
				{
					continue;
				}
				foreach (string key in data.MapReplacements.Keys)
				{
					if (Game1.getLocationFromName(key)?.GetLocationContextId() == locationContextId)
					{
						return true;
					}
				}
				continue;
			}
			return true;
		}
		id = null;
		data = null;
		return false;
	}

	/// <summary>Get whether there's a passive festival scheduled for today.</summary>
	/// <remarks>This doesn't match active festivals like the Flower Dance; see <see cref="M:StardewValley.Utility.isFestivalDay" /> for those.</remarks>
	public static bool IsPassiveFestivalDay()
	{
		string id;
		PassiveFestivalData data;
		return TryGetPassiveFestivalDataForDay(Game1.dayOfMonth, Game1.season, null, out id, out data);
	}

	/// <summary>Get whether there's a passive festival scheduled for the given day.</summary>
	/// <param name="day">The day of month to check.</param>
	/// <param name="season">The season to check.</param>
	/// <param name="locationContextId">The location context to check, or <c>null</c> for any location context.</param>
	/// <remarks>This doesn't match active festivals like the Flower Dance; see <see cref="M:StardewValley.Utility.isFestivalDay(System.Int32,StardewValley.Season)" /> for those.</remarks>
	public static bool IsPassiveFestivalDay(int dayOfMonth, Season season, string locationContextId)
	{
		string id;
		PassiveFestivalData data;
		return TryGetPassiveFestivalDataForDay(dayOfMonth, season, locationContextId, out id, out data);
	}

	/// <summary>Get whether a given passive festival is scheduled for today.</summary>
	/// <param name="festivalId">The passive festival ID.</param>
	/// <remarks>This doesn't match active festivals like the Flower Dance; see <see cref="M:StardewValley.Utility.isFestivalDay" /> for those.</remarks>
	public static bool IsPassiveFestivalDay(string festivalId)
	{
		return Game1.netWorldState.Value.ActivePassiveFestivals.Contains(festivalId);
	}

	public static bool IsPassiveFestivalOpen(string festivalId)
	{
		if (IsPassiveFestivalDay(festivalId) && TryGetPassiveFestivalData(festivalId, out var data))
		{
			return Game1.timeOfDay >= data.StartTime;
		}
		return false;
	}

	public static int GetDayOfPassiveFestival(string festivalId)
	{
		if (!IsPassiveFestivalDay(festivalId) || !TryGetPassiveFestivalData(festivalId, out var data))
		{
			return -1;
		}
		return Game1.dayOfMonth - data.StartDay + 1;
	}

	public static bool isPlacementForbiddenHere(string location_name)
	{
		if (location_name == "AbandonedJojaMart")
		{
			return true;
		}
		foreach (string activePassiveFestival in Game1.netWorldState.Value.ActivePassiveFestivals)
		{
			if (!TryGetPassiveFestivalData(activePassiveFestival, out var data) || data.MapReplacements == null)
			{
				continue;
			}
			foreach (string value in data.MapReplacements.Values)
			{
				if (location_name == value)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void transferPlacedObjectsFromOneLocationToAnother(GameLocation source, GameLocation destination, Vector2? overflow_chest_position = null, GameLocation overflow_chest_location = null)
	{
		if (source == null)
		{
			return;
		}
		List<Item> list = new List<Item>();
		foreach (Vector2 item in new List<Vector2>(source.objects.Keys))
		{
			if (source.objects[item] == null)
			{
				continue;
			}
			Object obj = source.objects[item];
			bool num = destination != null && !destination.objects.ContainsKey(item) && destination.CanItemBePlacedHere(item);
			source.objects.Remove(item);
			if (num && destination != null)
			{
				destination.objects[item] = obj;
				continue;
			}
			list.Add(obj);
			if (!(obj is Chest chest))
			{
				continue;
			}
			List<Item> list2 = new List<Item>(chest.Items);
			chest.Items.Clear();
			foreach (Item item2 in list2)
			{
				if (item2 != null)
				{
					list.Add(item2);
				}
			}
		}
		if (overflow_chest_position.HasValue)
		{
			if (overflow_chest_location != null)
			{
				createOverflowChest(overflow_chest_location, overflow_chest_position.Value, list);
			}
			else if (destination != null)
			{
				createOverflowChest(destination, overflow_chest_position.Value, list);
			}
		}
	}

	public static void createOverflowChest(GameLocation destination, Vector2 overflow_chest_location, List<Item> overflow_items)
	{
		List<Chest> list = new List<Chest>();
		foreach (Item overflow_item in overflow_items)
		{
			if (list.Count == 0)
			{
				list.Add(new Chest(playerChest: true));
			}
			bool flag = false;
			foreach (Chest item in list)
			{
				if (item.addItem(overflow_item) == null)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Chest chest = new Chest(playerChest: true);
				chest.addItem(overflow_item);
				list.Add(chest);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			Chest o = list[i];
			_placeOverflowChestInNearbySpace(destination, overflow_chest_location, o);
		}
	}

	protected static void _placeOverflowChestInNearbySpace(GameLocation location, Vector2 tileLocation, Object o)
	{
		if (o == null || tileLocation.Equals(Vector2.Zero))
		{
			return;
		}
		int i = 0;
		Queue<Vector2> queue = new Queue<Vector2>();
		HashSet<Vector2> hashSet = new HashSet<Vector2>();
		queue.Enqueue(tileLocation);
		Vector2 vector = Vector2.Zero;
		for (; i < 100; i++)
		{
			vector = queue.Dequeue();
			if (location.CanItemBePlacedHere(vector))
			{
				break;
			}
			hashSet.Add(vector);
			foreach (Vector2 adjacentTileLocation in getAdjacentTileLocations(vector))
			{
				if (!hashSet.Contains(adjacentTileLocation))
				{
					queue.Enqueue(adjacentTileLocation);
				}
			}
		}
		if (!vector.Equals(Vector2.Zero) && location.CanItemBePlacedHere(vector))
		{
			o.TileLocation = vector;
			location.objects.Add(vector, o);
		}
	}

	public static bool isWithinTileWithLeeway(int x, int y, Item item, Farmer f)
	{
		if (!withinRadiusOfPlayer(x, y, 1, f))
		{
			return _HasNonMousePlacementLeeway(x, y, item, f);
		}
		return true;
	}

	public static bool playerCanPlaceItemHere(GameLocation location, Item item, int x, int y, Farmer f, bool show_error = false)
	{
		bool isSummonRelic = item != null && StardewValley.Pokemon.PokemonEncounterManager.IsSummonRelic(item.ItemId);
		if (!isSummonRelic && isPlacementForbiddenHere(location))
		{
			return false;
		}
		if (item == null || item is Tool || Game1.eventUp || (!isSummonRelic && (f.bathingClothes.Value || f.onBridge.Value)))
		{
			return false;
		}
		if (isWithinTileWithLeeway(x, y, item, f) || (item is Wallpaper && location is DecoratableLocation) || (item is Furniture furniture && location.CanPlaceThisFurnitureHere(furniture)))
		{
			if (item is Furniture furniture2 && !location.CanFreePlaceFurniture() && !furniture2.IsCloseEnoughToFarmer(f, x / 64, y / 64))
			{
				return false;
			}
			Vector2 tile = new Vector2(x / 64, y / 64);
			if (item.canBePlacedHere(location, tile, CollisionMask.All, show_error))
			{
				return item.isPlaceable();
			}
		}
		return false;
	}

	public static string GetDoubleWideVersionOfBed(string bedId)
	{
		if (int.TryParse(bedId, out var result))
		{
			return (result + 4).ToString();
		}
		if (bedId == "BluePinstripeBed")
		{
			return "BluePinstripeDoubleBed";
		}
		return BedFurniture.DOUBLE_BED_INDEX;
	}

	public static int getDirectionFromChange(Vector2 current, Vector2 previous)
	{
		if (current.X > previous.X)
		{
			return 1;
		}
		if (current.X < previous.X)
		{
			return 3;
		}
		if (current.Y > previous.Y)
		{
			return 2;
		}
		if (current.Y < previous.Y)
		{
			return 0;
		}
		return -1;
	}

	public static bool doesRectangleIntersectTile(Microsoft.Xna.Framework.Rectangle r, int tileX, int tileY)
	{
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(tileX * 64, tileY * 64, 64, 64);
		return r.Intersects(value);
	}

	public static bool IsHospitalVisitDay(string character_name)
	{
		try
		{
			Dictionary<string, string> dictionary = Game1.content.Load<Dictionary<string, string>>("Characters\\schedules\\" + character_name);
			string key = Game1.currentSeason + "_" + Game1.dayOfMonth;
			if (dictionary.TryGetValue(key, out var value) && value.Contains("Hospital"))
			{
				return true;
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	/// <summary>Get all characters of any type (including villagers, horses, pets, monsters, player children, etc).</summary>
	/// <remarks>This creates a new list each time it's called, which is inefficient for hot paths. Consider using <see cref="M:StardewValley.Utility.ForEachCharacter(System.Func{StardewValley.NPC,System.Boolean},System.Boolean)" /> if you don't need an actual list (e.g. you just need to iterate them once).</remarks>
	public static List<NPC> getAllCharacters()
	{
		List<NPC> list = new List<NPC>();
		ForEachCharacter(delegate(NPC npc)
		{
			list.Add(npc);
			return true;
		});
		return list;
	}

	/// <summary>Get all villager NPCs (excluding horses, pets, monsters, player children, etc).</summary>
	/// <remarks>This creates a new list each time it's called, which is inefficient for hot paths. Consider using <see cref="M:StardewValley.Utility.ForEachVillager(System.Func{StardewValley.NPC,System.Boolean},System.Boolean)" /> if you don't need an actual list (e.g. you just need to iterate them once).</remarks>
	public static List<NPC> getAllVillagers()
	{
		List<NPC> list = new List<NPC>();
		ForEachVillager(delegate(NPC npc)
		{
			list.Add(npc);
			return true;
		});
		return list;
	}

	/// <summary>Apply special conversion rules when equipping an item. For example, this is used to convert a Copper Pan tool into a hat.</summary>
	/// <param name="placedItem">The item being equipped.</param>
	public static Item PerformSpecialItemPlaceReplacement(Item placedItem)
	{
		Item item;
		switch (placedItem?.QualifiedItemId)
		{
		case "(T)Pan":
			item = ItemRegistry.Create("(H)71");
			break;
		case "(T)SteelPan":
			item = ItemRegistry.Create("(H)SteelPanHat");
			break;
		case "(T)GoldPan":
			item = ItemRegistry.Create("(H)GoldPanHat");
			break;
		case "(T)IridiumPan":
			item = ItemRegistry.Create("(H)IridiumPanHat");
			break;
		case "(O)71":
			item = ItemRegistry.Create("(P)15");
			break;
		default:
			return placedItem;
		}
		item.modData.CopyFrom(placedItem.modData);
		if (item is Hat hat && placedItem is Tool tool)
		{
			hat.enchantments.AddRange(tool.enchantments);
			hat.previousEnchantments.AddRange(tool.previousEnchantments);
		}
		return item;
	}

	/// <summary>Apply special conversion rules when un-equipping an item. For example, this is used to convert a Copper Pan hat back into a tool.</summary>
	/// <param name="placedItem">The item being equipped.</param>
	public static Item PerformSpecialItemGrabReplacement(Item heldItem)
	{
		Item item;
		switch (heldItem?.QualifiedItemId)
		{
		case "(P)15":
		{
			Object obj = ItemRegistry.Create<Object>("(O)71");
			obj.questItem.Value = true;
			obj.questId.Value = "102";
			item = obj;
			break;
		}
		case "(H)71":
			item = ItemRegistry.Create("(T)Pan");
			break;
		case "(H)SteelPanHat":
			item = ItemRegistry.Create("(T)SteelPan");
			break;
		case "(H)GoldPanHat":
			item = ItemRegistry.Create("(T)GoldPan");
			break;
		case "(H)IridiumPanHat":
			item = ItemRegistry.Create("(T)IridiumPan");
			break;
		default:
			return heldItem;
		}
		item.modData.CopyFrom(heldItem.modData);
		if (item is Pan pan && heldItem is Hat hat)
		{
			pan.enchantments.AddRange(hat.enchantments);
			pan.previousEnchantments.AddRange(hat.previousEnchantments);
		}
		return item;
	}

	/// <summary>Perform an action for every item stored in chests or storage furniture, or placed on furniture.</summary>
	/// <param name="action">The action to perform.</param>
	/// <remarks>See also <see cref="M:StardewValley.Utility.ForEachItem(System.Func{StardewValley.Item,System.Boolean})" /> to iterate all items, regardless of where they are.</remarks>
	public static void iterateChestsAndStorage(Action<Item> action)
	{
		ForEachLocation(delegate(GameLocation l)
		{
			Chest fridge = l.GetFridge(onlyUnlocked: false);
			fridge?.ForEachItem(Handle, null);
			foreach (Object value in l.objects.Values)
			{
				if (value != fridge)
				{
					if (value is Chest)
					{
						value.ForEachItem(Handle, null);
					}
					else if (value.heldObject.Value is Chest chest)
					{
						chest.ForEachItem(Handle, null);
					}
				}
			}
			foreach (Furniture item in l.furniture)
			{
				item.ForEachItem(Handle, null);
			}
			foreach (Building building in l.buildings)
			{
				foreach (Chest buildingChest in building.buildingChests)
				{
					buildingChest.ForEachItem(Handle, null);
				}
			}
			return true;
		});
		foreach (Item returnedDonation in Game1.player.team.returnedDonations)
		{
			if (returnedDonation != null)
			{
				action(returnedDonation);
			}
		}
		foreach (Inventory value2 in Game1.player.team.globalInventories.Values)
		{
			foreach (Item item2 in (IEnumerable<Item>)value2)
			{
				if (item2 != null)
				{
					action(item2);
				}
			}
		}
		foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
		{
			foreach (Item donatedItem in specialOrder.donatedItems)
			{
				if (donatedItem != null)
				{
					action(donatedItem);
				}
			}
		}
		bool Handle(in ForEachItemContext context)
		{
			action(context.Item);
			return true;
		}
	}

	public static Item removeItemFromInventory(int whichItemIndex, IList<Item> items)
	{
		if (whichItemIndex >= 0 && whichItemIndex < items.Count && items[whichItemIndex] != null)
		{
			Item item = items[whichItemIndex];
			if (whichItemIndex == Game1.player.CurrentToolIndex && items.Equals(Game1.player.Items))
			{
				item?.actionWhenStopBeingHeld(Game1.player);
			}
			items[whichItemIndex] = null;
			return item;
		}
		return null;
	}

	/// <summary>Get a random available NPC listed in <c>Data/Characters</c> whose <see cref="F:StardewValley.GameData.Characters.CharacterData.HomeRegion" /> is <see cref="F:StardewValley.NPC.region_town" />.</summary>
	/// <param name="random">The RNG with which to choose an NPC.</param>
	/// <remarks>See also <see cref="M:StardewValley.Utility.getRandomNpcFromHomeRegion(System.String,System.Random)" />.</remarks>
	public static NPC getRandomTownNPC(Random random = null)
	{
		return getRandomNpcFromHomeRegion("Town", random);
	}

	/// <summary>Get a random available NPC listed in <c>Data/Characters</c> with a given <see cref="F:StardewValley.GameData.Characters.CharacterData.HomeRegion" />.</summary>
	/// <param name="region">The region to match.</param>
	/// <param name="random">The RNG with which to choose an NPC.</param>
	public static NPC getRandomNpcFromHomeRegion(string region, Random random = null)
	{
		return GetRandomNpc((string name, CharacterData data) => data.HomeRegion == region, random);
	}

	/// <summary>Get a random available NPC listed in <c>Data/Characters</c> which can give or receive gifts at the Feast of the Winter Star.</summary>
	/// <param name="ignoreNpc">Whether to exclude an NPC from the selection.</param>
	public static NPC GetRandomWinterStarParticipant(Func<string, bool> ignoreNpc = null)
	{
		return GetRandomNpc(delegate(string name, CharacterData data)
		{
			Func<string, bool> func = ignoreNpc;
			return (func == null || !func(name)) && ((data.WinterStarParticipant == null) ? (data.HomeRegion == "Town") : GameStateQuery.CheckConditions(data.WinterStarParticipant));
		}, CreateRandom(Game1.uniqueIDForThisGame / 2, Game1.year, Game1.player.UniqueMultiplayerID));
	}

	/// <summary>Get a random available NPC listed in <c>Data/Characters</c>.</summary>
	/// <param name="match">A predicate matching the NPCs to include, or <c>null</c> to allow any valid match.</param>
	/// <param name="random">The RNG with which to choose an NPC.</param>
	/// <param name="mustBeSocial">Whether to only include NPCs whose <see cref="P:StardewValley.NPC.CanSocialize" /> property is true.</param>
	public static NPC GetRandomNpc(Func<string, CharacterData, bool> match = null, Random random = null, bool mustBeSocial = true)
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, CharacterData> characterDatum in Game1.characterData)
		{
			if (match == null || match(characterDatum.Key, characterDatum.Value))
			{
				list.Add(characterDatum.Key);
			}
		}
		random = random ?? Game1.random;
		while (list.Count > 0)
		{
			int index = random.Next(list.Count);
			NPC characterFromName = Game1.getCharacterFromName(list[index]);
			if (characterFromName != null && (!mustBeSocial || characterFromName.CanSocialize))
			{
				return characterFromName;
			}
			list.RemoveAt(index);
		}
		return null;
	}

	public static bool foundAllStardrops(Farmer who = null)
	{
		if (who == null)
		{
			who = Game1.player;
		}
		if (who.mailReceived.Contains("gotMaxStamina"))
		{
			return true;
		}
		if (who.hasOrWillReceiveMail("CF_Fair") && who.hasOrWillReceiveMail("CF_Fish") && (who.hasOrWillReceiveMail("CF_Mines") || who.chestConsumedMineLevels.GetValueOrDefault(100, defaultValue: false)) && who.hasOrWillReceiveMail("CF_Sewer") && who.hasOrWillReceiveMail("museumComplete") && who.hasOrWillReceiveMail("CF_Spouse"))
		{
			return who.hasOrWillReceiveMail("CF_Statue");
		}
		return false;
	}

	public static int numStardropsFound(Farmer who = null)
	{
		if (who == null)
		{
			who = Game1.player;
		}
		int num = 0;
		if (who.hasOrWillReceiveMail("CF_Fair"))
		{
			num++;
		}
		if (who.hasOrWillReceiveMail("CF_Fish"))
		{
			num++;
		}
		if (who.hasOrWillReceiveMail("CF_Mines") || who.chestConsumedMineLevels.GetValueOrDefault(100, defaultValue: false))
		{
			num++;
		}
		if (who.hasOrWillReceiveMail("CF_Sewer"))
		{
			num++;
		}
		if (who.hasOrWillReceiveMail("museumComplete"))
		{
			num++;
		}
		if (who.hasOrWillReceiveMail("CF_Spouse"))
		{
			num++;
		}
		if (who.hasOrWillReceiveMail("CF_Statue"))
		{
			num++;
		}
		return num;
	}

	/// <summary>
	/// Can range from 0 to 21.
	///
	///    if (points &gt;= 12) 4
	///     if (points &gt;= 8) 3
	///   if (points &gt;= 4)  2
	///    else 1
	/// those are the number of candles that will be light on grandpa's shrine.
	/// </summary>
	/// <returns></returns>
	public static int getGrandpaScore()
	{
		int num = 0;
		if (Game1.player.totalMoneyEarned >= 50000)
		{
			num++;
		}
		if (Game1.player.totalMoneyEarned >= 100000)
		{
			num++;
		}
		if (Game1.player.totalMoneyEarned >= 200000)
		{
			num++;
		}
		if (Game1.player.totalMoneyEarned >= 300000)
		{
			num++;
		}
		if (Game1.player.totalMoneyEarned >= 500000)
		{
			num++;
		}
		if (Game1.player.totalMoneyEarned >= 1000000)
		{
			num += 2;
		}
		if (Game1.player.achievements.Contains(5))
		{
			num++;
		}
		if (Game1.player.hasSkullKey)
		{
			num++;
		}
		bool num2 = Game1.isLocationAccessible("CommunityCenter");
		if (num2 || Game1.player.hasCompletedCommunityCenter())
		{
			num++;
		}
		if (num2)
		{
			num += 2;
		}
		if (Game1.player.isMarriedOrRoommates() && getHomeOfFarmer(Game1.player).upgradeLevel >= 2)
		{
			num++;
		}
		if (Game1.player.hasRustyKey)
		{
			num++;
		}
		if (Game1.player.achievements.Contains(26))
		{
			num++;
		}
		if (Game1.player.achievements.Contains(34))
		{
			num++;
		}
		int numberOfFriendsWithinThisRange = getNumberOfFriendsWithinThisRange(Game1.player, 1975, 999999);
		if (numberOfFriendsWithinThisRange >= 5)
		{
			num++;
		}
		if (numberOfFriendsWithinThisRange >= 10)
		{
			num++;
		}
		int level = Game1.player.Level;
		if (level >= 15)
		{
			num++;
		}
		if (level >= 25)
		{
			num++;
		}
		if (Game1.player.mailReceived.Contains("petLoveMessage"))
		{
			num++;
		}
		return num;
	}

	public static int getGrandpaCandlesFromScore(int score)
	{
		if (score >= 12)
		{
			return 4;
		}
		if (score >= 8)
		{
			return 3;
		}
		if (score >= 4)
		{
			return 2;
		}
		return 1;
	}

	public static bool canItemBeAddedToThisInventoryList(Item i, IList<Item> list, int listMaxSpace = -1)
	{
		if (listMaxSpace != -1 && list.Count < listMaxSpace)
		{
			return true;
		}
		int num = i.Stack;
		foreach (Item item in list)
		{
			if (item == null)
			{
				return true;
			}
			if (item.canStackWith(i) && item.getRemainingStackSpace() > 0)
			{
				num -= item.getRemainingStackSpace();
				if (num <= 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Parse a raw direction string into a number matching one of the constants like <see cref="F:StardewValley.Game1.up" />.</summary>
	/// <param name="direction">The raw direction value. This can be a case-insensitive name (<c>up</c>, <c>down</c>, <c>left</c>, or <c>right</c>) or a numeric value matching a contant like <see cref="F:StardewValley.Game1.up" />.</param>
	/// <param name="parsed">The parsed value matching a constant like <see cref="F:StardewValley.Game1.up" />, or <c>-1</c> if not valid.</param>
	/// <returns>Returns whether the value was successfully parsed.</returns>
	public static bool TryParseDirection(string direction, out int parsed)
	{
		if (string.IsNullOrWhiteSpace(direction))
		{
			parsed = -1;
			return false;
		}
		if (direction.EqualsIgnoreCase("up"))
		{
			parsed = 0;
			return true;
		}
		if (direction.EqualsIgnoreCase("down"))
		{
			parsed = 2;
			return true;
		}
		if (direction.EqualsIgnoreCase("left"))
		{
			parsed = 3;
			return true;
		}
		if (direction.EqualsIgnoreCase("right"))
		{
			parsed = 1;
			return true;
		}
		if (int.TryParse(direction, out parsed))
		{
			int num = parsed;
			if ((uint)num <= 3u)
			{
				return true;
			}
		}
		parsed = -1;
		return false;
	}

	public static int GetNumberOfItemThatCanBeAddedToThisInventoryList(Item item, IList<Item> list, int listMaxItems)
	{
		int num = 0;
		foreach (Item item2 in list)
		{
			if (item2 == null)
			{
				num += item.maximumStackSize();
			}
			else if (item2 != null && item2.canStackWith(item) && item2.getRemainingStackSpace() > 0)
			{
				num += item2.getRemainingStackSpace();
			}
		}
		for (int i = 0; i < listMaxItems - list.Count; i++)
		{
			num += item.maximumStackSize();
		}
		return num;
	}

	/// <summary>Add an item to an inventory list if there's room for it.</summary>
	/// <param name="i">The item to add.</param>
	/// <param name="list">The inventory list to add it to.</param>
	/// <param name="listMaxSpace">The maximum number of item slots allowed in the <paramref name="list" />, or <c>-1</c> for no limit.</param>
	/// <returns>If the item was fully added to the inventory, returns <c>null</c>. Else returns the input item with its stack reduced to the amount that couldn't be added.</returns>
	public static Item addItemToThisInventoryList(Item i, IList<Item> list, int listMaxSpace = -1)
	{
		i.FixStackSize();
		foreach (Item item in list)
		{
			if (item != null && item.canStackWith(i) && item.getRemainingStackSpace() > 0)
			{
				int amount = i.Stack - item.addToStack(i);
				if (i.ConsumeStack(amount) == null)
				{
					return null;
				}
			}
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (list[num] == null)
			{
				if (i.Stack <= i.maximumStackSize())
				{
					list[num] = i;
					return null;
				}
				list[num] = i.getOne();
				list[num].Stack = i.maximumStackSize();
				if (i is Object obj)
				{
					obj.stack.Value -= i.maximumStackSize();
				}
				else
				{
					i.Stack -= i.maximumStackSize();
				}
			}
		}
		while (listMaxSpace != -1 && list.Count < listMaxSpace)
		{
			if (i.Stack > i.maximumStackSize())
			{
				Item one = i.getOne();
				one.Stack = i.maximumStackSize();
				if (i is Object obj2)
				{
					obj2.stack.Value -= i.maximumStackSize();
				}
				else
				{
					i.Stack -= i.maximumStackSize();
				}
				list.Add(one);
				continue;
			}
			list.Add(i);
			return null;
		}
		return i;
	}

	/// <summary>Add an item to an inventory list at a specific index position. If there's already an item at that position, the stacks are merged (if possible) else they're swapped.</summary>
	/// <param name="item">The item to add.</param>
	/// <param name="position">The index position within the list at which to add the item.</param>
	/// <param name="items">The inventory list to add it to.</param>
	/// <param name="onAddFunction">The callback to invoke when an item is added to the inventory.</param>
	/// <returns>If the item was fully added to the inventory, returns <c>null</c>. If it replaced an item stack previously at that position, returns the replaced item stack. Else returns the input item with its stack reduced to the amount that couldn't be added.</returns>
	public static Item addItemToInventory(Item item, int position, IList<Item> items, ItemGrabMenu.behaviorOnItemSelect onAddFunction = null)
	{
		bool flag = items.Equals(Game1.player.Items);
		if (flag)
		{
			Game1.player.GetItemReceiveBehavior(item, out var needsInventorySpace, out var _);
			if (!needsInventorySpace)
			{
				Game1.player.OnItemReceived(item, item.Stack, null);
				return null;
			}
		}
		if (position >= 0 && position < items.Count)
		{
			if (items[position] == null)
			{
				items[position] = item;
				if (flag)
				{
					Game1.player.OnItemReceived(item, item.Stack, null);
				}
				onAddFunction?.Invoke(item, null);
				return null;
			}
			if (item.canStackWith(items[position]))
			{
				int stack = item.Stack;
				int num = items[position].addToStack(item);
				if (flag)
				{
					Game1.player.OnItemReceived(item, stack - num, items[position]);
				}
				if (num <= 0)
				{
					return null;
				}
				item.Stack = num;
				onAddFunction?.Invoke(item, null);
				return item;
			}
			Item item2 = items[position];
			if (position == Game1.player.CurrentToolIndex && items.Equals(Game1.player.Items) && item2 != null)
			{
				item2.actionWhenStopBeingHeld(Game1.player);
				item.actionWhenBeingHeld(Game1.player);
			}
			items[position] = item;
			if (flag)
			{
				Game1.player.OnItemReceived(item, item.Stack, null);
			}
			onAddFunction?.Invoke(item, null);
			return item2;
		}
		return item;
	}

	/// <summary>
	/// called on monster kill, breakable container open, tree chop, tree shake w/ seed, diggable spots. ChanceModifier is adjusted per each source to account for the frequency of source hits
	/// </summary>
	public static bool trySpawnRareObject(Farmer who, Vector2 position, GameLocation location, double chanceModifier = 1.0, double dailyLuckWeight = 1.0, int groundLevel = -1, Random random = null)
	{
		if (random == null)
		{
			random = Game1.random;
		}
		double num = 1.0;
		if (who != null)
		{
			num = 1.0 + who.team.AverageDailyLuck() * dailyLuckWeight;
		}
		if (who != null && who.stats.Get(StatKeys.Mastery(0)) != 0 && random.NextDouble() < 0.001 * chanceModifier * num)
		{
			Game1.createItemDebris(ItemRegistry.Create("(O)GoldenAnimalCracker"), position, -1, location, groundLevel);
		}
		if (Game1.stats.DaysPlayed > 2 && random.NextDouble() < 0.002 * chanceModifier)
		{
			Game1.createItemDebris(getRandomCosmeticItem(random), position, -1, location, groundLevel);
		}
		if (Game1.stats.DaysPlayed > 2 && random.NextDouble() < 0.0006 * chanceModifier)
		{
			Game1.createItemDebris(ItemRegistry.Create("(O)SkillBook_" + random.Next(5)), position, -1, location, groundLevel);
		}
		return false;
	}

	public static bool spawnObjectAround(Vector2 tileLocation, Object o, GameLocation l, bool playSound = true, Action<Object> modifyObject = null)
	{
		if (o == null || l == null || tileLocation.Equals(Vector2.Zero))
		{
			return false;
		}
		int i = 0;
		Queue<Vector2> queue = new Queue<Vector2>();
		HashSet<Vector2> hashSet = new HashSet<Vector2>();
		queue.Enqueue(tileLocation);
		Vector2 vector = Vector2.Zero;
		for (; i < 100; i++)
		{
			vector = queue.Dequeue();
			if (l.CanItemBePlacedHere(vector))
			{
				break;
			}
			hashSet.Add(vector);
			Vector2[] array = (from a in getAdjacentTileLocations(vector)
				orderby Guid.NewGuid()
				select a).ToArray();
			foreach (Vector2 item in array)
			{
				if (!hashSet.Contains(item))
				{
					queue.Enqueue(item);
				}
			}
		}
		o.isSpawnedObject.Value = true;
		o.canBeGrabbed.Value = true;
		o.TileLocation = vector;
		modifyObject?.Invoke(o);
		if (!vector.Equals(Vector2.Zero) && l.CanItemBePlacedHere(vector))
		{
			l.objects.Add(vector, o);
			if (playSound)
			{
				l.playSound("coin");
			}
			if (l.Equals(Game1.currentLocation))
			{
				l.temporarySprites.Add(new TemporaryAnimatedSprite(5, vector * 64f, Color.White));
			}
			return true;
		}
		return false;
	}

	public static bool IsGeode(Item item, bool disallow_special_geodes = false)
	{
		if (item.HasTypeObject() && (!disallow_special_geodes || !item.HasContextTag("geode_crusher_ignored")))
		{
			if (!item.QualifiedItemId.Contains("MysteryBox"))
			{
				if (Game1.objectData.TryGetValue(item.ItemId, out var value))
				{
					if (!value.GeodeDropsDefaultItems)
					{
						List<ObjectGeodeDropData> geodeDrops = value.GeodeDrops;
						if (geodeDrops == null)
						{
							return false;
						}
						return geodeDrops.Count > 0;
					}
					return true;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static Item getRandomCosmeticItem(Random r)
	{
		if (r.NextDouble() < 0.2)
		{
			if (r.NextDouble() < 0.05)
			{
				return ItemRegistry.Create("(F)1369");
			}
			Item item = null;
			switch (r.Next(3))
			{
			case 0:
				item = ItemRegistry.Create(getRandomSingleTileFurniture(r));
				break;
			case 1:
				item = ItemRegistry.Create("(F)" + r.Next(1362, 1370));
				break;
			case 2:
				item = ItemRegistry.Create("(F)" + r.Next(1376, 1391));
				break;
			}
			if (item == null || item.Name.Contains("Error"))
			{
				item = ItemRegistry.Create("(F)1369");
			}
			return item;
		}
		if (r.NextDouble() < 0.25)
		{
			List<string> list = new List<string>
			{
				"(H)45", "(H)46", "(H)47", "(H)49", "(H)52", "(H)53", "(H)54", "(H)55", "(H)57", "(H)58",
				"(H)59", "(H)62", "(H)63", "(H)68", "(H)69", "(H)70", "(H)84", "(H)85", "(H)87", "(H)88",
				"(H)89", "(H)90"
			};
			return ItemRegistry.Create(list[r.Next(list.Count)]);
		}
		return ItemRegistry.Create("(S)" + getRandomIntWithExceptions(r, 1112, 1291, new List<int>
		{
			1038, 1041, 1129, 1130, 1132, 1133, 1136, 1152, 1176, 1177,
			1201, 1202, 1127
		}));
	}

	public static int getRandomIntWithExceptions(Random r, int minValue, int maxValueExclusive, List<int> exceptions)
	{
		if (r == null)
		{
			r = Game1.random;
		}
		int num = r.Next(minValue, maxValueExclusive);
		while (exceptions != null && exceptions.Contains(num))
		{
			num = r.Next(minValue, maxValueExclusive);
		}
		return num;
	}

	public static bool tryRollMysteryBox(double baseChance, Random r = null)
	{
		if (!Game1.MasterPlayer.mailReceived.Contains("sawQiPlane"))
		{
			return false;
		}
		if (r == null)
		{
			r = Game1.random;
		}
		baseChance = ((Game1.player.stats.Get("Book_Mystery") == 0) ? (baseChance * 0.66) : (baseChance * 0.88));
		return r.NextDouble() < baseChance;
	}

	public static Item getTreasureFromGeode(Item geode)
	{
		if (!IsGeode(geode))
		{
			return null;
		}
		try
		{
			string qualifiedItemId = geode.QualifiedItemId;
			Random random = CreateRandom(qualifiedItemId.Contains("MysteryBox") ? Game1.stats.Get("MysteryBoxesOpened") : Game1.stats.GeodesCracked, Game1.uniqueIDForThisGame / 2, (int)Game1.player.uniqueMultiplayerID.Value / 2);
			int num = random.Next(1, 10);
			for (int i = 0; i < num; i++)
			{
				random.NextDouble();
			}
			num = random.Next(1, 10);
			for (int j = 0; j < num; j++)
			{
				random.NextDouble();
			}

			Item pokeRelic = PokemonRelics.TryGetGeodeRelic(geode, random);
			if (pokeRelic != null)
			{
				return pokeRelic;
			}
			if (qualifiedItemId.Contains("MysteryBox"))
			{
				if (Game1.stats.Get("MysteryBoxesOpened") > 10 || qualifiedItemId == "(O)GoldenMysteryBox")
				{
					double num2 = ((!(qualifiedItemId == "(O)GoldenMysteryBox")) ? 1 : 2);
					if (qualifiedItemId == "(O)GoldenMysteryBox")
					{
						if (Game1.player.stats.Get(StatKeys.Mastery(0)) != 0 && random.NextBool(0.005))
						{
							return ItemRegistry.Create("(O)GoldenAnimalCracker");
						}
						if (random.NextBool(0.005))
						{
							return ItemRegistry.Create("(BC)272");
						}
					}
					if (random.NextBool(0.002 * num2))
					{
						return ItemRegistry.Create("(O)279");
					}
					if (random.NextBool(0.004 * num2))
					{
						return ItemRegistry.Create("(O)74");
					}
					if (random.NextBool(0.008 * num2))
					{
						return ItemRegistry.Create("(O)166");
					}
					if (random.NextBool(0.01 * num2 + (Game1.player.mailReceived.Contains("GotMysteryBook") ? 0.0 : (0.0004 * (double)Game1.stats.Get("MysteryBoxesOpened")))))
					{
						if (!Game1.player.mailReceived.Contains("GotMysteryBook"))
						{
							Game1.player.mailReceived.Add("GotMysteryBook");
							return ItemRegistry.Create("(O)Book_Mystery");
						}
						return ItemRegistry.Create(random.Choose("(O)PurpleBook", "(O)Book_Mystery"));
					}
					if (random.NextBool(0.01 * num2))
					{
						return ItemRegistry.Create(random.Choose("(O)797", "(O)373"));
					}
					if (random.NextBool(0.01 * num2))
					{
						return ItemRegistry.Create("(H)MysteryHat");
					}
					if (random.NextBool(0.01 * num2))
					{
						return ItemRegistry.Create("(S)MysteryShirt");
					}
					if (random.NextBool(0.01 * num2))
					{
						return ItemRegistry.Create("(WP)MoreWalls:11");
					}
					if (random.NextBool(0.1) || qualifiedItemId == "(O)GoldenMysteryBox")
					{
						switch (random.Next(15))
						{
						case 0:
							return ItemRegistry.Create("(O)288", 5);
						case 1:
							return ItemRegistry.Create("(O)253", 3);
						case 2:
							if (Game1.player.GetUnmodifiedSkillLevel(1) >= 6 && random.NextBool())
							{
								return ItemRegistry.Create(random.Choose("(O)687", "(O)695"));
							}
							return ItemRegistry.Create("(O)242", 2);
						case 3:
							return ItemRegistry.Create("(O)204", 2);
						case 4:
							return ItemRegistry.Create("(O)369", 20);
						case 5:
							return ItemRegistry.Create("(O)466", 20);
						case 6:
							return ItemRegistry.Create("(O)773", 2);
						case 7:
							return ItemRegistry.Create("(O)688", 3);
						case 8:
							return ItemRegistry.Create("(O)" + random.Next(628, 634));
						case 9:
							return ItemRegistry.Create("(O)" + Crop.getRandomLowGradeCropForThisSeason(Game1.season), 20);
						case 10:
							if (random.NextBool())
							{
								return ItemRegistry.Create("(W)60");
							}
							return ItemRegistry.Create(random.Choose("(O)533", "(O)534"));
						case 11:
							return ItemRegistry.Create("(O)621");
						case 12:
							return ItemRegistry.Create("(O)MysteryBox", random.Next(3, 5));
						case 13:
							return ItemRegistry.Create("(O)SkillBook_" + random.Next(5));
						case 14:
							return getRaccoonSeedForCurrentTimeOfYear(Game1.player, random, 8);
						}
					}
				}
				switch (random.Next(14))
				{
				case 0:
					return ItemRegistry.Create("(O)395", 3);
				case 1:
					return ItemRegistry.Create("(O)287", 5);
				case 2:
					return ItemRegistry.Create("(O)" + Crop.getRandomLowGradeCropForThisSeason(Game1.season), 8);
				case 3:
					return ItemRegistry.Create("(O)" + random.Next(727, 734));
				case 4:
					return ItemRegistry.Create("(O)" + getRandomIntWithExceptions(random, 194, 240, new List<int> { 217 }));
				case 5:
					return ItemRegistry.Create("(O)709", 10);
				case 6:
					return ItemRegistry.Create("(O)369", 10);
				case 7:
					return ItemRegistry.Create("(O)466", 10);
				case 8:
					return ItemRegistry.Create("(O)688");
				case 9:
					return ItemRegistry.Create("(O)689");
				case 10:
					return ItemRegistry.Create("(O)770", 10);
				case 11:
					return ItemRegistry.Create("(O)MixedFlowerSeeds", 10);
				case 12:
					if (random.NextBool(0.4))
					{
						return random.Next(4) switch
						{
							0 => ItemRegistry.Create<Ring>("(O)525"), 
							1 => ItemRegistry.Create<Ring>("(O)529"), 
							2 => ItemRegistry.Create<Ring>("(O)888"), 
							_ => ItemRegistry.Create<Ring>("(O)" + random.Next(531, 533)), 
						};
					}
					return ItemRegistry.Create("(O)MysteryBox", 2);
				case 13:
					return ItemRegistry.Create("(O)690");
				default:
					return ItemRegistry.Create("(O)382");
				}
			}
			if (random.NextBool(0.1) && Game1.player.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
			{
				return ItemRegistry.Create("(O)890", (!random.NextBool(0.25)) ? 1 : 5);
			}
			if (random.NextBool(0.20))
			{
				string pokeHeld = StardewValley.Pokemon.PokemonHeldItemManager.GetRandomGeodeDrop(geode.ItemId, random);
				if (!string.IsNullOrEmpty(pokeHeld))
				{
					Item pokeItem = ItemRegistry.Create("(O)" + pokeHeld, 1, 0, allowNull: true);
					if (pokeItem != null)
					{
						return pokeItem;
					}
				}
			}
			if (Game1.objectData.TryGetValue(geode.ItemId, out var value))
			{
				List<ObjectGeodeDropData> geodeDrops = value.GeodeDrops;
				if (geodeDrops != null && geodeDrops.Count > 0 && (!value.GeodeDropsDefaultItems || random.NextBool()))
				{
					foreach (ObjectGeodeDropData drop in value.GeodeDrops.OrderBy((ObjectGeodeDropData p) => p.Precedence))
					{
						if (!random.NextBool(drop.Chance) || (drop.Condition != null && !GameStateQuery.CheckConditions(drop.Condition, null, null, null, null, random)))
						{
							continue;
						}
						Item item = ItemQueryResolver.TryResolveRandomItem(drop, new ItemQueryContext(null, null, random, $"object '{geode.ItemId}' > geode drop '{drop.Id}'"), avoidRepeat: false, null, null, null, delegate(string query, string error)
						{
							Game1.log.Error($"Geode item '{geode.QualifiedItemId}' failed parsing item query '{query}' for {"GeodeDrops"} entry '{drop.Id}': {error}");
						});
						if (item != null)
						{
							if (drop.SetFlagOnPickup != null)
							{
								item.SetFlagOnPickup = drop.SetFlagOnPickup;
							}
							return item;
						}
					}
				}
			}
			int num3 = random.Next(3) * 2 + 1;
			if (random.NextBool(0.1))
			{
				num3 = 10;
			}
			if (random.NextBool(0.01))
			{
				num3 = 20;
			}
			if (random.NextBool())
			{
				switch (random.Next(4))
				{
				case 0:
				case 1:
					return ItemRegistry.Create("(O)390", num3);
				case 2:
					return ItemRegistry.Create("(O)330");
				default:
					return qualifiedItemId switch
					{
						"(O)749" => ItemRegistry.Create("(O)" + (82 + random.Next(3) * 2)), 
						"(O)535" => ItemRegistry.Create("(O)86"), 
						"(O)536" => ItemRegistry.Create("(O)84"), 
						_ => ItemRegistry.Create("(O)82"), 
					};
				}
			}
			if (!(qualifiedItemId == "(O)535"))
			{
				if (qualifiedItemId == "(O)536")
				{
					return random.Next(4) switch
					{
						0 => ItemRegistry.Create("(O)378", num3), 
						1 => ItemRegistry.Create("(O)380", num3), 
						2 => ItemRegistry.Create("(O)382", num3), 
						_ => ItemRegistry.Create((Game1.player.deepestMineLevel > 75) ? "(O)384" : "(O)380", num3), 
					};
				}
				return random.Next(5) switch
				{
					0 => ItemRegistry.Create("(O)378", num3), 
					1 => ItemRegistry.Create("(O)380", num3), 
					2 => ItemRegistry.Create("(O)382", num3), 
					3 => ItemRegistry.Create("(O)384", num3), 
					_ => ItemRegistry.Create("(O)386", num3 / 2 + 1), 
				};
			}
			return random.Next(3) switch
			{
				0 => ItemRegistry.Create("(O)378", num3), 
				1 => ItemRegistry.Create((Game1.player.deepestMineLevel > 25) ? "(O)380" : "(O)378", num3), 
				_ => ItemRegistry.Create("(O)382", num3), 
			};
		}
		catch (Exception exception)
		{
			Game1.log.Error("Geode '" + geode?.QualifiedItemId + "' failed creating treasure.", exception);
		}
		return ItemRegistry.Create("(O)390");
	}

	public static Vector2 snapToInt(Vector2 v)
	{
		v.X = (int)v.X;
		v.Y = (int)v.Y;
		return v;
	}

	public static Vector2 GetNearbyValidPlacementPosition(Farmer who, GameLocation location, Item item, int x, int y)
	{
		if (!Game1.isCheckingNonMousePlacement)
		{
			return new Vector2(x, y);
		}
		int num = 1;
		int num2 = 1;
		Point point = default(Point);
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(0, 0, num * 64, num2 * 64);
		if (item is Furniture furniture)
		{
			num = furniture.getTilesWide();
			num2 = furniture.getTilesHigh();
			value.Width = furniture.boundingBox.Value.Width;
			value.Height = furniture.boundingBox.Value.Height;
		}
		switch (who.FacingDirection)
		{
		case 0:
			point.X = 0;
			point.Y = -1;
			y -= (num2 - 1) * 64;
			break;
		case 2:
			point.X = 0;
			point.Y = 1;
			break;
		case 3:
			point.X = -1;
			point.Y = 0;
			x -= (num - 1) * 64;
			break;
		case 1:
			point.X = 1;
			point.Y = 0;
			break;
		}
		int num3 = 2;
		if (item is Object obj && obj.isPassable() && (obj.Category == -74 || obj.isSapling() || obj.Category == -19))
		{
			x = (int)who.GetToolLocation().X / 64 * 64;
			y = (int)who.GetToolLocation().Y / 64 * 64;
			point.X = who.TilePoint.X - x / 64;
			point.Y = who.TilePoint.Y - y / 64;
			int num4 = (int)Math.Sqrt(Math.Pow(point.X, 2.0) + Math.Pow(point.Y, 2.0));
			if (num4 > 0)
			{
				point.X /= num4;
				point.Y /= num4;
			}
			num3 = num4 + 1;
		}
		bool flag = (item as Object)?.isPassable() ?? false;
		x = x / 64 * 64;
		y = y / 64 * 64;
		Microsoft.Xna.Framework.Rectangle boundingBox = who.GetBoundingBox();
		for (int i = 0; i < num3; i++)
		{
			int num5 = x + point.X * i * 64;
			int num6 = y + point.Y * i * 64;
			value.X = num5;
			value.Y = num6;
			if ((!boundingBox.Intersects(value) && !flag) || playerCanPlaceItemHere(location, item, num5, num6, who))
			{
				return new Vector2(num5, num6);
			}
		}
		return new Vector2(x, y);
	}

	public static bool tryToPlaceItem(GameLocation location, Object item, int x, int y)
	{
		if (item == null)
		{
			return false;
		}
		Vector2 key = new Vector2(x / 64, y / 64);
		if (playerCanPlaceItemHere(location, item, x, y, Game1.player))
		{
			if (item is Furniture)
			{
				Game1.player.ActiveObject = null;
			}
			if (item.placementAction(location, x, y, Game1.player))
			{
				Game1.player.reduceActiveItemByOne();
			}
			else if (item is Furniture activeObject)
			{
				Game1.player.ActiveObject = activeObject;
			}
			else if (item is Wallpaper)
			{
				return false;
			}
			return true;
		}
		if (isPlacementForbiddenHere(location) && item != null && item.isPlaceable())
		{
			if (Game1.didPlayerJustClickAtAll(ignoreNonMouseHeldInput: true))
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
			}
		}
		else if (item is Furniture furniture && Game1.didPlayerJustLeftClick(ignoreNonMouseHeldInput: true))
		{
			switch (furniture.GetAdditionalFurniturePlacementStatus(location, x, y, Game1.player))
			{
			case 1:
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture.cs.12629"));
				break;
			case 2:
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture.cs.12632"));
				break;
			case 3:
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture.cs.12633"));
				break;
			case 4:
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture.cs.12632"));
				break;
			}
		}
		if (item.Category == -19 && location.terrainFeatures.TryGetValue(key, out var value) && value is HoeDirt hoeDirt)
		{
			switch (hoeDirt.CheckApplyFertilizerRules(item.QualifiedItemId))
			{
			case HoeDirtFertilizerApplyStatus.HasThisFertilizer:
				return false;
			case HoeDirtFertilizerApplyStatus.HasAnotherFertilizer:
				if (Game1.didPlayerJustClickAtAll(ignoreNonMouseHeldInput: true))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13916-2"));
				}
				return false;
			case HoeDirtFertilizerApplyStatus.CropAlreadySprouted:
				if (Game1.didPlayerJustClickAtAll(ignoreNonMouseHeldInput: true))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13916"));
				}
				return false;
			}
		}
		playerCanPlaceItemHere(location, item, x, y, Game1.player, show_error: true);
		return false;
	}

	public static bool pointInRectangles(List<Microsoft.Xna.Framework.Rectangle> rectangles, int x, int y)
	{
		foreach (Microsoft.Xna.Framework.Rectangle rectangle in rectangles)
		{
			if (rectangle.Contains(x, y))
			{
				return true;
			}
		}
		return false;
	}

	public static Keys mapGamePadButtonToKey(Buttons b)
	{
		return b switch
		{
			Buttons.A => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.actionButton), 
			Buttons.X => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.useToolButton), 
			Buttons.B => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.menuButton), 
			Buttons.Back => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.journalButton), 
			Buttons.Start => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.menuButton), 
			Buttons.Y => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.menuButton), 
			Buttons.DPadUp => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveUpButton), 
			Buttons.DPadRight => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveRightButton), 
			Buttons.DPadDown => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveDownButton), 
			Buttons.DPadLeft => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveLeftButton), 
			Buttons.LeftThumbstickUp => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveUpButton), 
			Buttons.LeftThumbstickRight => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveRightButton), 
			Buttons.LeftThumbstickDown => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveDownButton), 
			Buttons.LeftThumbstickLeft => Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveLeftButton), 
			_ => Keys.None, 
		};
	}

	public static ButtonCollection getPressedButtons(GamePadState padState, GamePadState oldPadState)
	{
		return new ButtonCollection(ref padState, ref oldPadState);
	}

	public static bool thumbstickIsInDirection(int direction, GamePadState padState)
	{
		if (Game1.currentMinigame != null)
		{
			return true;
		}
		return direction switch
		{
			0 => Math.Abs(padState.ThumbSticks.Left.X) < padState.ThumbSticks.Left.Y, 
			1 => padState.ThumbSticks.Left.X > Math.Abs(padState.ThumbSticks.Left.Y), 
			2 => Math.Abs(padState.ThumbSticks.Left.X) < Math.Abs(padState.ThumbSticks.Left.Y), 
			3 => Math.Abs(padState.ThumbSticks.Left.X) > Math.Abs(padState.ThumbSticks.Left.Y), 
			_ => false, 
		};
	}

	public static ButtonCollection getHeldButtons(GamePadState padState)
	{
		return new ButtonCollection(ref padState);
	}

	/// <summary>
	/// return true if music becomes muted
	/// </summary>
	/// <returns></returns>
	public static bool toggleMuteMusic()
	{
		if (Game1.options.musicVolumeLevel != 0f)
		{
			disableMusic();
			return true;
		}
		enableMusic();
		return false;
	}

	public static void enableMusic()
	{
		Game1.options.musicVolumeLevel = 0.75f;
		Game1.musicCategory.SetVolume(0.75f);
		Game1.musicPlayerVolume = 0.75f;
		Game1.options.ambientVolumeLevel = 0.75f;
		Game1.ambientCategory.SetVolume(0.75f);
		Game1.ambientPlayerVolume = 0.75f;
	}

	public static void disableMusic()
	{
		Game1.options.musicVolumeLevel = 0f;
		Game1.musicCategory.SetVolume(0f);
		Game1.options.ambientVolumeLevel = 0f;
		Game1.ambientCategory.SetVolume(0f);
		Game1.ambientPlayerVolume = 0f;
		Game1.musicPlayerVolume = 0f;
	}

	public static Vector2 getVelocityTowardPlayer(Point startingPoint, float speed, Farmer f)
	{
		Microsoft.Xna.Framework.Rectangle boundingBox = f.GetBoundingBox();
		return getVelocityTowardPoint(startingPoint, new Vector2(boundingBox.X, boundingBox.Y), speed);
	}

	/// <summary>Get a timestamp with hours and minutes from a milliseconds count, like <c>27:46</c> for 100,000,000 milliseconds.</summary>
	/// <param name="milliseconds">The number of milliseconds.</param>
	public static string getHoursMinutesStringFromMilliseconds(ulong milliseconds)
	{
		return milliseconds / 3600000 + ":" + ((milliseconds % 3600000 / 60000 < 10) ? "0" : "") + milliseconds % 3600000 / 60000;
	}

	/// <summary>Get a timestamp with minutes and seconds from a milliseconds count, like <c>1:40</c> for 100,000 milliseconds.</summary>
	/// <param name="milliseconds">The number of milliseconds.</param>
	public static string getMinutesSecondsStringFromMilliseconds(int milliseconds)
	{
		return milliseconds / 60000 + ":" + ((milliseconds % 60000 / 1000 < 10) ? "0" : "") + milliseconds % 60000 / 1000;
	}

	public static Vector2 getVelocityTowardPoint(Vector2 startingPoint, Vector2 endingPoint, float speed)
	{
		double num = endingPoint.X - startingPoint.X;
		double num2 = endingPoint.Y - startingPoint.Y;
		if (Math.Abs(num) < 0.1 && Math.Abs(num2) < 0.1)
		{
			return new Vector2(0f, 0f);
		}
		double num3 = Math.Sqrt(Math.Pow(num, 2.0) + Math.Pow(num2, 2.0));
		num /= num3;
		num2 /= num3;
		return new Vector2((float)(num * (double)speed), (float)(num2 * (double)speed));
	}

	public static Vector2 getVelocityTowardPoint(Point startingPoint, Vector2 endingPoint, float speed)
	{
		return getVelocityTowardPoint(new Vector2(startingPoint.X, startingPoint.Y), endingPoint, speed);
	}

	public static Vector2 getRandomPositionInThisRectangle(Microsoft.Xna.Framework.Rectangle r, Random random)
	{
		return new Vector2(random.Next(r.X, r.X + r.Width), random.Next(r.Y, r.Y + r.Height));
	}

	public static Vector2 getTopLeftPositionForCenteringOnScreen(xTile.Dimensions.Rectangle viewport, int width, int height, int xOffset = 0, int yOffset = 0)
	{
		return new Vector2(viewport.Width / 2 - width / 2 + xOffset, viewport.Height / 2 - height / 2 + yOffset);
	}

	public static Vector2 getTopLeftPositionForCenteringOnScreen(int width, int height, int xOffset = 0, int yOffset = 0)
	{
		return getTopLeftPositionForCenteringOnScreen(Game1.uiViewport, width, height, xOffset, yOffset);
	}

	public static void recursiveFindPositionForCharacter(NPC c, GameLocation l, Vector2 tileLocation, int maxIterations)
	{
		int i = 0;
		Queue<Vector2> queue = new Queue<Vector2>();
		queue.Enqueue(tileLocation);
		List<Vector2> list = new List<Vector2>();
		Microsoft.Xna.Framework.Rectangle boundingBox = c.GetBoundingBox();
		for (; i < maxIterations; i++)
		{
			if (queue.Count <= 0)
			{
				break;
			}
			Vector2 vector = queue.Dequeue();
			list.Add(vector);
			c.Position = new Vector2(vector.X * 64f + 32f - (float)(boundingBox.Width / 2), vector.Y * 64f - (float)boundingBox.Height);
			if (!l.isCollidingPosition(c.GetBoundingBox(), Game1.viewport, isFarmer: false, 0, glider: false, c, pathfinding: true))
			{
				if (!l.characters.Contains(c))
				{
					l.characters.Add(c);
					c.currentLocation = l;
				}
				break;
			}
			Vector2[] directionsTileVectors = DirectionsTileVectors;
			foreach (Vector2 vector2 in directionsTileVectors)
			{
				if (!list.Contains(vector + vector2))
				{
					queue.Enqueue(vector + vector2);
				}
			}
		}
	}

	public static Pet findPet(Guid guid)
	{
		foreach (NPC character in Game1.getFarm().characters)
		{
			if (character is Pet pet && pet.petId.Value.Equals(guid))
			{
				return pet;
			}
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			foreach (NPC character2 in getHomeOfFarmer(allFarmer).characters)
			{
				if (character2 is Pet pet2 && pet2.petId.Value.Equals(guid))
				{
					return pet2;
				}
			}
		}
		return null;
	}

	public static Vector2 recursiveFindOpenTileForCharacter(Character c, GameLocation l, Vector2 tileLocation, int maxIterations, bool allowOffMap = true)
	{
		int i = 0;
		Queue<Vector2> queue = new Queue<Vector2>();
		queue.Enqueue(tileLocation);
		List<Vector2> list = new List<Vector2>();
		Vector2 position = c.Position;
		int width = c.GetBoundingBox().Width;
		for (; i < maxIterations; i++)
		{
			if (queue.Count <= 0)
			{
				break;
			}
			Vector2 vector = queue.Dequeue();
			list.Add(vector);
			c.Position = new Vector2(vector.X * 64f + 32f - (float)(width / 2), vector.Y * 64f + 4f);
			Microsoft.Xna.Framework.Rectangle boundingBox = c.GetBoundingBox();
			c.Position = position;
			if (!l.isCollidingPosition(boundingBox, Game1.viewport, c is Farmer, 0, glider: false, c, pathfinding: false, projectile: false, ignoreCharacterRequirement: false, skipCollisionEffects: true) && (allowOffMap || l.isTileOnMap(vector)))
			{
				return vector;
			}
			Vector2[] directionsTileVectors = DirectionsTileVectors;
			for (int j = 0; j < directionsTileVectors.Length; j++)
			{
				Vector2 vector2 = directionsTileVectors[j];
				if (!list.Contains(vector + vector2) && l.isTilePlaceable(vector + vector2) && (!(l is DecoratableLocation) || !(l as DecoratableLocation).isTileOnWall((int)(vector2.X + vector.X), (int)(vector2.Y + vector.Y))))
				{
					queue.Enqueue(vector + vector2);
				}
			}
		}
		return Vector2.Zero;
	}

	public static List<Vector2> recursiveFindOpenTiles(GameLocation l, Vector2 tileLocation, int maxOpenTilesToFind = 24, int maxIterations = 50)
	{
		int i = 0;
		Queue<Vector2> queue = new Queue<Vector2>();
		queue.Enqueue(tileLocation);
		List<Vector2> list = new List<Vector2>();
		List<Vector2> list2 = new List<Vector2>();
		for (; i < maxIterations; i++)
		{
			if (queue.Count <= 0)
			{
				break;
			}
			if (list2.Count >= maxOpenTilesToFind)
			{
				break;
			}
			Vector2 vector = queue.Dequeue();
			list.Add(vector);
			if (l.CanItemBePlacedHere(vector))
			{
				list2.Add(vector);
			}
			Vector2[] directionsTileVectors = DirectionsTileVectors;
			foreach (Vector2 vector2 in directionsTileVectors)
			{
				if (!list.Contains(vector + vector2))
				{
					queue.Enqueue(vector + vector2);
				}
			}
		}
		return list2;
	}

	public static void spreadAnimalsAround(Building b, GameLocation environment)
	{
		try
		{
			GameLocation indoors = b.GetIndoors();
			if (indoors != null)
			{
				spreadAnimalsAround(b, environment, indoors.animals.Values);
			}
		}
		catch (Exception)
		{
		}
	}

	public static void spreadAnimalsAround(Building b, GameLocation environment, IEnumerable<FarmAnimal> animalsList)
	{
		if (!b.HasIndoors())
		{
			return;
		}
		Queue<FarmAnimal> queue = new Queue<FarmAnimal>(animalsList);
		int num = 0;
		Queue<Vector2> queue2 = new Queue<Vector2>();
		queue2.Enqueue(new Vector2(b.tileX.Value + b.animalDoor.X, b.tileY.Value + b.animalDoor.Y + 1));
		while (queue.Count > 0 && num < 40 && queue2.Count > 0)
		{
			Vector2 vector = queue2.Dequeue();
			FarmAnimal farmAnimal = queue.Peek();
			Microsoft.Xna.Framework.Rectangle boundingBox = farmAnimal.GetBoundingBox();
			farmAnimal.Position = new Vector2(vector.X * 64f + 32f - (float)(boundingBox.Width / 2), vector.Y * 64f - 32f - (float)(boundingBox.Height / 2));
			if (!environment.isCollidingPosition(farmAnimal.GetBoundingBox(), Game1.viewport, isFarmer: false, 0, glider: false, farmAnimal, pathfinding: true))
			{
				environment.animals.Add(farmAnimal.myID.Value, farmAnimal);
				queue.Dequeue();
			}
			if (queue.Count > 0)
			{
				farmAnimal = queue.Peek();
				boundingBox = farmAnimal.GetBoundingBox();
				Vector2[] directionsTileVectors = DirectionsTileVectors;
				for (int i = 0; i < directionsTileVectors.Length; i++)
				{
					Vector2 vector2 = directionsTileVectors[i];
					farmAnimal.Position = new Vector2((vector.X + vector2.X) * 64f + 32f - (float)(boundingBox.Width / 2), (vector.Y + vector2.Y) * 64f - 32f - (float)(boundingBox.Height / 2));
					if (!environment.isCollidingPosition(farmAnimal.GetBoundingBox(), Game1.viewport, isFarmer: false, 0, glider: false, farmAnimal, pathfinding: true))
					{
						queue2.Enqueue(vector + vector2);
					}
				}
			}
			num++;
		}
	}

	/// <summary>Get the tile position which contains a tile index.</summary>
	/// <param name="location">The location whose map to search.</param>
	/// <param name="tileIndex">The tile index to find.</param>
	/// <param name="layerId">The layer whose tiles to check.</param>
	/// <param name="tilesheet">The tilesheet ID containing the <paramref name="tileIndex" />, or <c>null</c> for any tilesheet. If a tile doesn't use this tilesheet, it'll be ignored.</param>
	/// <returns>Returns the first match found, or (-1, -1) if none was found.</returns>
	public static Point findTile(GameLocation location, int tileIndex, string layerId, string tilesheet = null)
	{
		Layer layer = location.map.RequireLayer(layerId);
		for (int i = 0; i < layer.LayerHeight; i++)
		{
			for (int j = 0; j < layer.LayerWidth; j++)
			{
				if (location.getTileIndexAt(j, i, layerId, tilesheet) == tileIndex)
				{
					return new Point(j, i);
				}
			}
		}
		return new Point(-1, -1);
	}

	public static bool[] horizontalOrVerticalCollisionDirections(Microsoft.Xna.Framework.Rectangle boundingBox, Character c, bool projectile = false)
	{
		bool[] array = new bool[2];
		Microsoft.Xna.Framework.Rectangle position = new Microsoft.Xna.Framework.Rectangle(boundingBox.X, boundingBox.Y, boundingBox.Width, boundingBox.Height);
		position.Width = 1;
		position.X = boundingBox.Center.X;
		if (c != null)
		{
			if (Game1.currentLocation.isCollidingPosition(position, Game1.viewport, isFarmer: false, -1, projectile, c, pathfinding: false, projectile))
			{
				array[1] = true;
			}
		}
		else if (Game1.currentLocation.isCollidingPosition(position, Game1.viewport, isFarmer: false, -1, projectile, c, pathfinding: false, projectile))
		{
			array[1] = true;
		}
		position.Width = boundingBox.Width;
		position.X = boundingBox.X;
		position.Height = 1;
		position.Y = boundingBox.Center.Y;
		if (c != null)
		{
			if (Game1.currentLocation.isCollidingPosition(position, Game1.viewport, isFarmer: false, -1, projectile, c, pathfinding: false, projectile))
			{
				array[0] = true;
			}
		}
		else if (Game1.currentLocation.isCollidingPosition(position, Game1.viewport, isFarmer: false, -1, projectile, c, pathfinding: false, projectile))
		{
			array[0] = true;
		}
		return array;
	}

	public static Color getBlendedColor(Color c1, Color c2)
	{
		return new Color(Game1.random.NextBool() ? Math.Max(c1.R, c2.R) : ((c1.R + c2.R) / 2), Game1.random.NextBool() ? Math.Max(c1.G, c2.G) : ((c1.G + c2.G) / 2), Game1.random.NextBool() ? Math.Max(c1.B, c2.B) : ((c1.B + c2.B) / 2));
	}

	public static Character checkForCharacterWithinArea(Type kindOfCharacter, Vector2 positionToAvoid, GameLocation location, Microsoft.Xna.Framework.Rectangle area)
	{
		foreach (NPC character in location.characters)
		{
			if (character.GetType().Equals(kindOfCharacter) && character.GetBoundingBox().Intersects(area) && !character.Position.Equals(positionToAvoid))
			{
				return character;
			}
		}
		return null;
	}

	public static int getNumberOfCharactersInRadius(GameLocation l, Point position, int tileRadius)
	{
		Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle(position.X - tileRadius * 64, position.Y - tileRadius * 64, (tileRadius * 2 + 1) * 64, (tileRadius * 2 + 1) * 64);
		int num = 0;
		foreach (NPC character in l.characters)
		{
			if (rectangle.Contains(Vector2ToPoint(character.Position)))
			{
				num++;
			}
		}
		return num;
	}

	public static List<Vector2> getListOfTileLocationsForBordersOfNonTileRectangle(Microsoft.Xna.Framework.Rectangle rectangle)
	{
		return new List<Vector2>
		{
			new Vector2(rectangle.Left / 64, rectangle.Top / 64),
			new Vector2(rectangle.Right / 64, rectangle.Top / 64),
			new Vector2(rectangle.Left / 64, rectangle.Bottom / 64),
			new Vector2(rectangle.Right / 64, rectangle.Bottom / 64),
			new Vector2(rectangle.Left / 64, rectangle.Center.Y / 64),
			new Vector2(rectangle.Right / 64, rectangle.Center.Y / 64),
			new Vector2(rectangle.Center.X / 64, rectangle.Bottom / 64),
			new Vector2(rectangle.Center.X / 64, rectangle.Top / 64),
			new Vector2(rectangle.Center.X / 64, rectangle.Center.Y / 64)
		};
	}

	public static void makeTemporarySpriteJuicier(TemporaryAnimatedSprite t, GameLocation l, int numAddOns = 4, int xRange = 64, int yRange = 64)
	{
		t.position.Y -= 8f;
		l.temporarySprites.Add(t);
		for (int i = 0; i < numAddOns; i++)
		{
			TemporaryAnimatedSprite clone = t.getClone();
			clone.delayBeforeAnimationStart = i * 100;
			clone.position += new Vector2(Game1.random.Next(-xRange / 2, xRange / 2 + 1), Game1.random.Next(-yRange / 2, yRange / 2 + 1));
			clone.layerDepth += 1E-06f;
			l.temporarySprites.Add(clone);
		}
	}

	public static void recursiveObjectPlacement(Object o, int tileX, int tileY, double growthRate, double decay, GameLocation location, string terrainToExclude = "", int objectIndexAddRange = 0, double failChance = 0.0, int objectIndeAddRangeMultiplier = 1, List<string> itemIDVariations = null)
	{
		if (o == null)
		{
			return;
		}
		if (!int.TryParse(o.ItemId, out var result))
		{
			result = -1;
		}
		if (!location.isTileLocationOpen(new Location(tileX, tileY)) || location.IsTileOccupiedBy(new Vector2(tileX, tileY)) || !location.hasTileAt(tileX, tileY, "Back") || (!terrainToExclude.Equals("") && (location.doesTileHaveProperty(tileX, tileY, "Type", "Back") == null || location.doesTileHaveProperty(tileX, tileY, "Type", "Back").Equals(terrainToExclude))))
		{
			return;
		}
		Vector2 vector = new Vector2(tileX, tileY);
		if (!Game1.random.NextBool(failChance * 2.0))
		{
			string itemId = o.ItemId;
			if (result >= 0)
			{
				itemId = (result + Game1.random.Next(objectIndexAddRange + 1) * objectIndeAddRangeMultiplier).ToString();
			}
			if (o is ColoredObject coloredObject)
			{
				location.objects.Add(vector, new ColoredObject(itemId, 1, coloredObject.color.Value)
				{
					Fragility = o.fragility.Value,
					MinutesUntilReady = o.MinutesUntilReady,
					Name = o.name,
					CanBeSetDown = o.CanBeSetDown,
					CanBeGrabbed = o.CanBeGrabbed,
					IsSpawnedObject = o.IsSpawnedObject,
					TileLocation = vector,
					ColorSameIndexAsParentSheetIndex = coloredObject.ColorSameIndexAsParentSheetIndex
				});
			}
			else
			{
				location.objects.Add(vector, new Object(itemId, 1)
				{
					Fragility = o.fragility.Value,
					MinutesUntilReady = o.MinutesUntilReady,
					CanBeSetDown = o.canBeSetDown.Value,
					CanBeGrabbed = o.canBeGrabbed.Value,
					IsSpawnedObject = o.isSpawnedObject.Value
				});
			}
		}
		growthRate -= decay;
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveObjectPlacement(o, tileX + 1, tileY, growthRate, decay, location, terrainToExclude, objectIndexAddRange, failChance, objectIndeAddRangeMultiplier, itemIDVariations);
		}
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveObjectPlacement(o, tileX - 1, tileY, growthRate, decay, location, terrainToExclude, objectIndexAddRange, failChance, objectIndeAddRangeMultiplier, itemIDVariations);
		}
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveObjectPlacement(o, tileX, tileY + 1, growthRate, decay, location, terrainToExclude, objectIndexAddRange, failChance, objectIndeAddRangeMultiplier, itemIDVariations);
		}
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveObjectPlacement(o, tileX, tileY - 1, growthRate, decay, location, terrainToExclude, objectIndexAddRange, failChance, objectIndeAddRangeMultiplier, itemIDVariations);
		}
	}

	public static void recursiveFarmGrassPlacement(int tileX, int tileY, double growthRate, double decay, GameLocation farm)
	{
		if (farm.isTileLocationOpen(new Location(tileX, tileY)) && !farm.IsTileOccupiedBy(new Vector2(tileX, tileY)) && farm.doesTileHaveProperty(tileX, tileY, "Diggable", "Back") != null)
		{
			Vector2 key = new Vector2(tileX, tileY);
			if (Game1.random.NextDouble() < 0.05)
			{
				farm.objects.Add(new Vector2(tileX, tileY), ItemRegistry.Create<Object>(Game1.random.Choose("(O)674", "(O)675")));
			}
			else
			{
				farm.terrainFeatures.Add(key, new Grass(1, 4 - (int)((1.0 - growthRate) * 4.0)));
			}
			growthRate -= decay;
			if (Game1.random.NextDouble() < growthRate)
			{
				recursiveFarmGrassPlacement(tileX + 1, tileY, growthRate, decay, farm);
			}
			if (Game1.random.NextDouble() < growthRate)
			{
				recursiveFarmGrassPlacement(tileX - 1, tileY, growthRate, decay, farm);
			}
			if (Game1.random.NextDouble() < growthRate)
			{
				recursiveFarmGrassPlacement(tileX, tileY + 1, growthRate, decay, farm);
			}
			if (Game1.random.NextDouble() < growthRate)
			{
				recursiveFarmGrassPlacement(tileX, tileY - 1, growthRate, decay, farm);
			}
		}
	}

	public static void recursiveTreePlacement(int tileX, int tileY, double growthRate, int growthStage, double skipChance, GameLocation l, Microsoft.Xna.Framework.Rectangle clearPatch, bool sparse)
	{
		if (clearPatch.Contains(tileX, tileY))
		{
			return;
		}
		Vector2 vector = new Vector2(tileX, tileY);
		if (l.doesTileHaveProperty((int)vector.X, (int)vector.Y, "Diggable", "Back") == null || l.IsNoSpawnTile(vector) || !l.isTileLocationOpen(new Location((int)vector.X, (int)vector.Y)) || l.IsTileOccupiedBy(vector) || (sparse && (l.IsTileOccupiedBy(new Vector2(tileX, tileY + -1)) || l.IsTileOccupiedBy(new Vector2(tileX, tileY + 1)) || l.IsTileOccupiedBy(new Vector2(tileX + 1, tileY)) || l.IsTileOccupiedBy(new Vector2(tileX + -1, tileY)) || l.IsTileOccupiedBy(new Vector2(tileX + 1, tileY + 1)))))
		{
			return;
		}
		if (!Game1.random.NextBool(skipChance))
		{
			if (sparse && vector.X < 70f && (vector.X < 48f || vector.Y > 26f) && Game1.random.NextDouble() < 0.07)
			{
				(l as Farm).resourceClumps.Add(new ResourceClump(Game1.random.Choose(672, 600, 602), 2, 2, vector));
			}
			else
			{
				l.terrainFeatures.Add(vector, new Tree(Game1.random.Next(1, 4).ToString(), (growthStage < 5) ? Game1.random.Next(5) : 5));
			}
			growthRate -= 0.05;
		}
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveTreePlacement(tileX + Game1.random.Next(1, 3), tileY, growthRate, growthStage, skipChance, l, clearPatch, sparse);
		}
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveTreePlacement(tileX - Game1.random.Next(1, 3), tileY, growthRate, growthStage, skipChance, l, clearPatch, sparse);
		}
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveTreePlacement(tileX, tileY + Game1.random.Next(1, 3), growthRate, growthStage, skipChance, l, clearPatch, sparse);
		}
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveTreePlacement(tileX, tileY - Game1.random.Next(1, 3), growthRate, growthStage, skipChance, l, clearPatch, sparse);
		}
	}

	public static void recursiveRemoveTerrainFeatures(int tileX, int tileY, double growthRate, double decay, GameLocation l)
	{
		Vector2 key = new Vector2(tileX, tileY);
		l.terrainFeatures.Remove(key);
		growthRate -= decay;
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveRemoveTerrainFeatures(tileX + 1, tileY, growthRate, decay, l);
		}
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveRemoveTerrainFeatures(tileX - 1, tileY, growthRate, decay, l);
		}
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveRemoveTerrainFeatures(tileX, tileY + 1, growthRate, decay, l);
		}
		if (Game1.random.NextDouble() < growthRate)
		{
			recursiveRemoveTerrainFeatures(tileX, tileY - 1, growthRate, decay, l);
		}
	}

	public static IEnumerator<int> generateNewFarm(bool skipFarmGeneration)
	{
		return generateNewFarm(skipFarmGeneration, loadForNewGame: true);
	}

	public static IEnumerator<int> generateNewFarm(bool skipFarmGeneration, bool loadForNewGame)
	{
		Game1.fadeToBlack = false;
		Game1.fadeToBlackAlpha = 1f;
		Game1.debrisWeather.Clear();
		Game1.viewport.X = -9999;
		Game1.changeMusicTrack("none");
		if (loadForNewGame)
		{
			Game1.game1.loadForNewGame();
		}
		Game1.currentLocation = Game1.RequireLocation("Farmhouse");
		Game1.currentLocation.currentEvent = new Event("none/-600 -600/farmer 4 8 2/warp farmer 4 8/end beginGame");
		Game1.gameMode = 2;
		yield return 100;
	}

	/// <summary>Get the pixel distance between a position in the world and the player's screen viewport, where 0 is within the viewport.</summary>
	/// <param name="pixelPosition">The pixel position.</param>
	public static float distanceFromScreen(Vector2 pixelPosition)
	{
		float num = pixelPosition.X - (float)Game1.viewport.X;
		float num2 = pixelPosition.Y - (float)Game1.viewport.Y;
		float x = MathHelper.Clamp(num, 0f, Game1.viewport.Width - 1);
		float y = MathHelper.Clamp(num2, 0f, Game1.viewport.Height - 1);
		return distance(x, num, y, num2);
	}

	/// <summary>Get whether a pixel position is within the current player's screen viewport.</summary>
	/// <param name="positionNonTile">The pixel position.</param>
	/// <param name="acceptableDistanceFromScreen">The maximum pixel distance outside the screen viewport to allow.</param>
	public static bool isOnScreen(Vector2 positionNonTile, int acceptableDistanceFromScreen)
	{
		positionNonTile.X -= Game1.viewport.X;
		positionNonTile.Y -= Game1.viewport.Y;
		if (positionNonTile.X > (float)(-acceptableDistanceFromScreen) && positionNonTile.X < (float)(Game1.viewport.Width + acceptableDistanceFromScreen) && positionNonTile.Y > (float)(-acceptableDistanceFromScreen))
		{
			return positionNonTile.Y < (float)(Game1.viewport.Height + acceptableDistanceFromScreen);
		}
		return false;
	}

	/// <summary>Get whether a tile position is within the current player's screen viewport.</summary>
	/// <param name="positionTile">The tile position.</param>
	/// <param name="acceptableDistanceFromScreenNonTile">The maximum tile distance outside the screen viewport to allow.</param>
	/// <param name="location">The location whose position to check.</param>
	public static bool isOnScreen(Point positionTile, int acceptableDistanceFromScreenNonTile, GameLocation location = null)
	{
		if (location != null && !location.Equals(Game1.currentLocation))
		{
			return false;
		}
		if (positionTile.X * 64 > Game1.viewport.X - acceptableDistanceFromScreenNonTile && positionTile.X * 64 < Game1.viewport.X + Game1.viewport.Width + acceptableDistanceFromScreenNonTile && positionTile.Y * 64 > Game1.viewport.Y - acceptableDistanceFromScreenNonTile)
		{
			return positionTile.Y * 64 < Game1.viewport.Y + Game1.viewport.Height + acceptableDistanceFromScreenNonTile;
		}
		return false;
	}

	public static void clearObjectsInArea(Microsoft.Xna.Framework.Rectangle r, GameLocation l)
	{
		for (int i = r.Left; i < r.Right; i += 64)
		{
			for (int j = r.Top; j < r.Bottom; j += 64)
			{
				l.removeEverythingFromThisTile(i / 64, j / 64);
			}
		}
	}

	public static void trashItem(Item item)
	{
		if (item is Object && Game1.player.specialItems.Contains(item.ItemId))
		{
			Game1.player.specialItems.Remove(item.ItemId);
		}
		if (getTrashReclamationPrice(item, Game1.player) > 0)
		{
			Game1.player.Money += getTrashReclamationPrice(item, Game1.player);
		}
		Game1.playSound("trashcan");
	}

	public static FarmAnimal GetBestHarvestableFarmAnimal(IEnumerable<FarmAnimal> animals, Tool tool, Microsoft.Xna.Framework.Rectangle toolRect)
	{
		FarmAnimal result = null;
		foreach (FarmAnimal animal in animals)
		{
			if (animal.GetHarvestBoundingBox().Intersects(toolRect))
			{
				if (animal.CanGetProduceWithTool(tool) && animal.currentProduce.Value != null && animal.isAdult())
				{
					return animal;
				}
				result = animal;
			}
		}
		return result;
	}

	public static long RandomLong(Random r = null)
	{
		if (r == null)
		{
			r = Game1.random;
		}
		byte[] array = new byte[8];
		r.NextBytes(array);
		return BitConverter.ToInt64(array, 0);
	}

	public static ulong NewUniqueIdForThisGame()
	{
		DateTime dateTime = new DateTime(2012, 6, 22);
		return (ulong)(long)(DateTime.UtcNow - dateTime).TotalSeconds;
	}

	/// <summary>Apply platform-specific bad word filtering to a given text.</summary>
	/// <param name="words">The text to filter.</param>
	/// <returns>Returns the text with any bad words removed or censored.</returns>
	public static string FilterDirtyWords(string words)
	{
		return Program.sdk.FilterDirtyWords(words);
	}

	/// <summary>Apply platform-specific bad word filtering to a given text, but only if the current platform requires strict filtering of all text inputs (including text only shown locally).</summary>
	/// <param name="words">The text to filter.</param>
	/// <returns>Returns the text with any bad words removed or censored if applicable.</returns>
	public static string FilterDirtyWordsIfStrictPlatform(string words)
	{
		return words;
	}

	/// <summary>
	/// This is used to filter out special characters from user entered 
	/// names to avoid crashes and other bugs in Dialogue.cs parsing.
	///
	/// The characters are replaced with spaces.
	/// </summary>
	public static string FilterUserName(string name)
	{
		return name;
	}

	public static bool IsHorizontalDirection(int direction)
	{
		if (direction != 3)
		{
			return direction == 1;
		}
		return true;
	}

	public static bool IsVerticalDirection(int direction)
	{
		if (direction != 0)
		{
			return direction == 2;
		}
		return true;
	}

	public static Microsoft.Xna.Framework.Rectangle ExpandRectangle(Microsoft.Xna.Framework.Rectangle rect, int facingDirection, int pixels)
	{
		switch (facingDirection)
		{
		case 0:
			rect.Height += pixels;
			rect.Y -= pixels;
			break;
		case 1:
			rect.Width += pixels;
			break;
		case 2:
			rect.Height += pixels;
			break;
		case 3:
			rect.Width += pixels;
			rect.X -= pixels;
			break;
		}
		return rect;
	}

	public static int GetOppositeFacingDirection(int facingDirection)
	{
		return facingDirection switch
		{
			0 => 2, 
			1 => 3, 
			2 => 0, 
			3 => 1, 
			_ => 0, 
		};
	}

	/// <summary>Convert an RGB value into an HLS value.</summary>
	/// <param name="r">The RGB red channel value.</param>
	/// <param name="g">The RGB green channel value.</param>
	/// <param name="b">The RGB blue channel value.</param>
	/// <param name="h">The equivalent hue value. This is a value between 0 and 360, indicating the angle on the HSL color wheel.</param>
	/// <param name="s">The equivalent saturation value, which indicates the amount of color added. This is a value between 0 (pure gray) and 1 (pure color).</param>
	/// <param name="l">The equivalent lightness value, which indicates how much light is in the color. This is a value between 0 (black) and 1 (white).</param>
	/// <remarks>Adapted from <a href="http://csharphelper.com/howtos/howto_rgb_to_hls.html">code by Rod Stephens</a>.</remarks>
	public static void RGBtoHSL(int r, int g, int b, out double h, out double s, out double l)
	{
		double num = (double)r / 255.0;
		double num2 = (double)g / 255.0;
		double num3 = (double)b / 255.0;
		double num4 = num;
		if (num4 < num2)
		{
			num4 = num2;
		}
		if (num4 < num3)
		{
			num4 = num3;
		}
		double num5 = num;
		if (num5 > num2)
		{
			num5 = num2;
		}
		if (num5 > num3)
		{
			num5 = num3;
		}
		double num6 = num4 - num5;
		l = (num4 + num5) / 2.0;
		if (Math.Abs(num6) < 1E-05)
		{
			s = 0.0;
			h = 0.0;
			return;
		}
		if (l <= 0.5)
		{
			s = num6 / (num4 + num5);
		}
		else
		{
			s = num6 / (2.0 - num4 - num5);
		}
		double num7 = (num4 - num) / num6;
		double num8 = (num4 - num2) / num6;
		double num9 = (num4 - num3) / num6;
		if (num == num4)
		{
			h = num9 - num8;
		}
		else if (num2 == num4)
		{
			h = 2.0 + num7 - num9;
		}
		else
		{
			h = 4.0 + num8 - num7;
		}
		h *= 60.0;
		if (h < 0.0)
		{
			h += 360.0;
		}
	}

	/// <summary>Convert an HSL value into an RGB value.</summary>
	/// <param name="h">The hue value.</param>
	/// <param name="s">The saturation value.</param>
	/// <param name="l">The lightness value.</param>
	/// <param name="r">The equivalent RGB red channel value.</param>
	/// <param name="g">The equivalent RGB green channel value.</param>
	/// <param name="b">The equivalent RGB blue channel value.</param>
	/// <remarks>Adapted from <a href="http://csharphelper.com/howtos/howto_rgb_to_hls.html">code by Rod Stephens</a>.</remarks>
	public static void HSLtoRGB(double h, double s, double l, out int r, out int g, out int b)
	{
		double num = ((!(l <= 0.5)) ? (l + s - l * s) : (l * (1.0 + s)));
		double q = 2.0 * l - num;
		double num2;
		double num3;
		double num4;
		if (s == 0.0)
		{
			num2 = l;
			num3 = l;
			num4 = l;
		}
		else
		{
			num2 = QQHtoRGB(q, num, h + 120.0);
			num3 = QQHtoRGB(q, num, h);
			num4 = QQHtoRGB(q, num, h - 120.0);
		}
		r = (int)(num2 * 255.0);
		g = (int)(num3 * 255.0);
		b = (int)(num4 * 255.0);
	}

	private static double QQHtoRGB(double q1, double q2, double hue)
	{
		if (hue > 360.0)
		{
			hue -= 360.0;
		}
		else if (hue < 0.0)
		{
			hue += 360.0;
		}
		if (hue < 60.0)
		{
			return q1 + (q2 - q1) * hue / 60.0;
		}
		if (hue < 180.0)
		{
			return q2;
		}
		if (hue < 240.0)
		{
			return q1 + (q2 - q1) * (240.0 - hue) / 60.0;
		}
		return q1;
	}

	public static float ModifyCoordinateFromUIScale(float coordinate)
	{
		return coordinate * Game1.options.uiScale / Game1.options.zoomLevel;
	}

	public static Vector2 ModifyCoordinatesFromUIScale(Vector2 coordinates)
	{
		return coordinates * Game1.options.uiScale / Game1.options.zoomLevel;
	}

	public static float ModifyCoordinateForUIScale(float coordinate)
	{
		return coordinate / Game1.options.uiScale * Game1.options.zoomLevel;
	}

	public static Vector2 ModifyCoordinatesForUIScale(Vector2 coordinates)
	{
		return coordinates / Game1.options.uiScale * Game1.options.zoomLevel;
	}

	public static bool ShouldIgnoreValueChangeCallback()
	{
		if (Game1.gameMode != 3)
		{
			return true;
		}
		if (Game1.client != null && !Game1.client.readyToPlay)
		{
			return true;
		}
		if (Game1.client != null && Game1.locationRequest != null)
		{
			return true;
		}
		return false;
	}

	/// <summary>Constrain an index to a range by wrapping out-of-bounds values to the other side (e.g. last index + 1 is the first index).</summary>
	/// <param name="index">The index to constrain.</param>
	/// <param name="count">The number of values in the range.</param>
	public static int WrapIndex(int index, int count)
	{
		return (index + count) % count;
	}
}
