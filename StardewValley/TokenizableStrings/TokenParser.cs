using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Content;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.SpecialOrders;

namespace StardewValley.TokenizableStrings;

/// <summary>Parses text containing tokens like "<c>It's a nice [Season] day</c>" into the resulting display text.</summary>
public class TokenParser
{
	/// <summary>The resolvers for vanilla token strings. Most code should call <see cref="M:StardewValley.TokenizableStrings.TokenParser.ParseText(System.String,System.Random,StardewValley.TokenizableStrings.TokenParserDelegate,StardewValley.Farmer)" /> instead of using these directly.</summary>
	public static class DefaultResolvers
	{
		/// <summary>The translated display name for an achievement ID.</summary>
		/// <remarks>For example, <c>[AchievementName 5]</c> will output something like "A Complete Collection".</remarks>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool AchievementName(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGetInt(query, 1, out var value, out var error, "int achievementId"))
			{
				return LogTokenError(query, error, out replacement);
			}
			if (!Game1.achievements.TryGetValue(value, out var value2))
			{
				return LogTokenError(query, $"unknown achievement ID '{value}'", out replacement);
			}
			replacement = value2.Split('^', 2)[0];
			return true;
		}

		/// <summary>The grammatical article ('a' or 'an') for the given word when playing in English, else blank.</summary>
		/// <remarks>For example: <c>[ArticleFor apple]</c> will output <c>an</c>.</remarks>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool ArticleFor(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string word"))
			{
				return LogTokenError(query, error, out replacement);
			}
			replacement = Lexicon.getProperArticleForWord(value);
			return true;
		}

		/// <summary>Get the input text with the first letter capitalized.</summary>
		/// <remarks>For example: <c>[CapitalizeFirstLetter an apple]</c> will output <c>An apple</c>.</remarks>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool CapitalizeFirstLetter(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGetRemainder(query, 1, out var value, out var error, ' ', "string text"))
			{
				return LogTokenError(query, error, out replacement);
			}
			replacement = Utility.capitalizeFirstLetter(value);
			return true;
		}

		/// <summary>Replaces spaces in the given text with a special character that lets you pass them into other space-delimited tokens. The characters are automatically turned back into spaces when displayed.</summary>
		/// <remarks>For example: <c>[EscapedText Some arbitrary text]</c>.</remarks>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool EscapedText(string[] query, out string replacement, Random random, Farmer player)
		{
			replacement = string.Join(" ", query.Skip(1));
			replacement = EscapeSpaces(replacement);
			return true;
		}

		/// <summary>Depending on the target player's gender, show either the male text or female text. To pass text containing spaces, wrap it in <c>EscapeText</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool GenderedText(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string maleStr") || !ArgUtility.TryGet(query, 2, out var value2, out error, allowBlank: true, "string femaleStr") || !ArgUtility.TryGetOptional(query, 3, out var value3, out error, null, allowBlank: true, "string otherStr"))
			{
				return LogTokenError(query, error, out replacement);
			}
			switch (player.Gender)
			{
			case Gender.Male:
				replacement = value;
				break;
			case Gender.Female:
				replacement = value2;
				break;
			default:
				replacement = value3 ?? value2;
				break;
			}
			return true;
		}

		/// <summary>The translated display name for a qualified item ID.</summary>
		/// <remarks>For example, <c>[ItemName (O)128]</c> returns a value like "Pufferfish".</remarks>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool ItemName(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string itemId") || !ArgUtility.TryGetOptional(query, 2, out var value2, out error, null, allowBlank: true, "string fallbackItemName"))
			{
				return LogTokenError(query, error, out replacement);
			}
			replacement = ItemRegistry.GetData(value)?.DisplayName ?? value2 ?? ItemRegistry.GetErrorItemName(value);
			return true;
		}

		/// <summary>The translated display name for a qualified item ID which includes a preserved flavor.</summary>
		/// <remarks>For example, <c>[ItemNameWithFlavor Wine (O)258]</c> returns a value like "Blueberry Wine".</remarks>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool ItemNameWithFlavor(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGetEnum<Object.PreserveType>(query, 1, out var value, out var error, "Object.PreserveType preserveType") || !ArgUtility.TryGet(query, 2, out var value2, out error, allowBlank: true, "string preservedId") || !ArgUtility.TryGetOptional(query, 3, out var value3, out error, null, allowBlank: true, "string fallbackItemName"))
			{
				return LogTokenError(query, error, out replacement);
			}
			string baseItemIdForFlavoredItem = ItemRegistry.GetObjectTypeDefinition().GetBaseItemIdForFlavoredItem(value, value2);
			replacement = Object.GetObjectDisplayName(baseItemIdForFlavoredItem, value, value2, null, value3);
			return true;
		}

		/// <summary>Translation text loaded from a string key. If the translation has placeholder tokens like {0}, you can add the values after the string key. To pass arguments containing spaces, wrap them in <c>EscapeText</c>.</summary>
		/// <remarks>For example: <c>[LocalizedText Strings\NPCNames:OldMariner]</c>.</remarks>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool LocalizedText(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string key"))
			{
				return LogTokenError(query, error, out replacement);
			}
			object[] array;
			if (query.Length > 2)
			{
				array = new object[query.Length - 2];
				for (int i = 2; i < query.Length; i++)
				{
					array[i - 2] = query[i];
				}
			}
			else
			{
				array = LegacyShims.EmptyArray<object>();
			}
			try
			{
				replacement = ((array.Length != 0) ? Game1.content.LoadString(value, array) : Game1.content.LoadString(value));
				return true;
			}
			catch (ContentLoadException)
			{
				return LogTokenError(query, "the key '" + value + "' doesn't match an existing asset", out replacement);
			}
			catch (InvalidCastException)
			{
				return LogTokenError(query, "the key '" + value + "' matches an asset, but it isn't of the required type 'Dictionary<string, string>'", out replacement);
			}
		}

		/// <summary>The translated display name for a monster.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool MonsterName(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string monsterId") || !ArgUtility.TryGetOptional(query, 2, out var value2, out error, null, allowBlank: true, "string fallbackText"))
			{
				return LogTokenError(query, error, out replacement);
			}
			replacement = (DataLoader.Monsters(Game1.content).TryGetValue(value, out var value3) ? ArgUtility.Get(value3.Split('/'), 14) : null);
			replacement = replacement ?? value2 ?? value;
			return true;
		}

		/// <summary>The translated title for a movie ID.</summary>
		/// <remarks>For example, <c>[MovieTitle spring_movie_0]</c> will output something like "The Brave Little Sapling".</remarks>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool MovieName(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string movieId"))
			{
				return LogTokenError(query, error, out replacement);
			}
			if (!MovieTheater.TryGetMovieData(value, out var data))
			{
				return LogTokenError(query, "unknown movie ID '" + value + "'", out replacement);
			}
			replacement = ParseText(data.Title);
			return true;
		}

		/// <summary>Format a number with commas based on the current language.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool NumberWithSeparators(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGetInt(query, 1, out var value, out var error, "int number"))
			{
				return LogTokenError(query, error, out replacement);
			}
			replacement = Utility.getNumberWithCommas(value);
			return true;
		}

		/// <summary>A random adjective from the <c>Strings\Lexicon</c> data asset's <c>RandomPositiveAdjective_PlaceOrEvent</c> entry.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool PositiveAdjective(string[] query, out string replacement, Random random, Farmer player)
		{
			replacement = Lexicon.getRandomPositiveAdjectiveForEventOrPerson();
			return true;
		}

		/// <summary>The translated display name for a special order ID.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool SpecialOrderName(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string orderId"))
			{
				return LogTokenError(query, error, out replacement);
			}
			foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
			{
				if (specialOrder.questKey.Value == value)
				{
					replacement = specialOrder.GetName();
					return true;
				}
			}
			if (SpecialOrder.TryGetData(value, out var data))
			{
				replacement = SpecialOrder.MakeLocalizationReplacements(ParseText(data.Name));
				return true;
			}
			return LogTokenError(query, "unknown special order ID '" + value + "'", out replacement);
		}

		/// <summary>Show different text depending on whether the target player's spouse is a player (first argument) or NPC (second argument). To pass text containing spaces, wrap it in <c>EscapeText</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool SpouseFarmerText(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerSpouse") || !ArgUtility.TryGet(query, 2, out var value2, out error, allowBlank: true, "string npcSpouse"))
			{
				return LogTokenError(query, error, out replacement);
			}
			if (player.team.GetSpouse(player.UniqueMultiplayerID).HasValue)
			{
				replacement = value;
				return true;
			}
			if (player.getSpouse() != null)
			{
				replacement = value2;
				return true;
			}
			return LogTokenError(query, "the target player '" + player.Name + "' isn't married", out replacement);
		}

		/// <summary>Equivalent to <see cref="M:StardewValley.TokenizableStrings.TokenParser.DefaultResolvers.GenderedText(System.String[],System.String@,System.Random,StardewValley.Farmer)" />, but based on the gender of the player's NPC or player spouse.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool SpouseGenderedText(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string maleStr") || !ArgUtility.TryGet(query, 2, out var value2, out error, allowBlank: true, "string femaleStr") || !ArgUtility.TryGetOptional(query, 3, out var value3, out error, null, allowBlank: true, "string otherStr"))
			{
				return LogTokenError(query, error, out replacement);
			}
			Gender? gender = null;
			long? spouse = player.team.GetSpouse(player.UniqueMultiplayerID);
			gender = ((!spouse.HasValue) ? player.getSpouse()?.Gender : new Gender?(Game1.GetPlayer(spouse.Value)?.Gender ?? Gender.Male));
			if (gender.HasValue)
			{
				switch (gender)
				{
				case Gender.Male:
					replacement = value;
					break;
				case Gender.Female:
					replacement = value2;
					break;
				default:
					replacement = value3 ?? value2;
					break;
				}
				return true;
			}
			return LogTokenError(query, "the target player '" + player.Name + "' isn't married", out replacement);
		}

		/// <summary>The translated display name for a qualified tool ID.</summary>
		/// <remarks>For example, <c>[ToolName (T)IridiumAxe]</c> returns a value like "Iridium Axe".</remarks>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool ToolName(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string itemId") || !ArgUtility.TryGetOptionalInt(query, 2, out var _, out error, -1, "int upgradeLevel"))
			{
				return LogTokenError(query, error, out replacement);
			}
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(value);
			if (!dataOrErrorItem.HasTypeId("(T)"))
			{
				return LogTokenError(query, "the item ID '" + value + "' matches a non-tool item", out replacement);
			}
			replacement = dataOrErrorItem.DisplayName;
			return true;
		}

		/// <summary>The numeric day of month, like <c>5</c> on spring 5.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool DayOfMonth(string[] query, out string replacement, Random random, Farmer player)
		{
			replacement = Game1.dayOfMonth.ToString();
			return true;
		}

		/// <summary>The current season name, like <c>spring</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool Season(string[] query, out string replacement, Random random, Farmer player)
		{
			replacement = Game1.CurrentSeasonDisplayName;
			return true;
		}

		/// <summary>The translated display name for an NPC, given their internal name.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool CharacterName(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string npcName"))
			{
				return LogTokenError(query, error, out replacement);
			}
			NPC characterFromName = Game1.getCharacterFromName(value);
			if (characterFromName == null)
			{
				return LogTokenError(query, "no character found with name '" + value + "'", out replacement);
			}
			replacement = characterFromName.displayName;
			return true;
		}

		/// <summary>The farm name for the current save (without the injected "Farm" text).</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool FarmName(string[] query, out string replacement, Random random, Farmer player)
		{
			replacement = player.farmName.Value;
			return true;
		}

		/// <summary>The target player's unique internal multiplayer ID.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool FarmerUniqueId(string[] query, out string replacement, Random random, Farmer player)
		{
			replacement = player.UniqueMultiplayerID.ToString();
			return true;
		}

		/// <summary>The translated display name for a location given its ID in <c>Data/Locations</c>.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool LocationName(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string locationKey"))
			{
				return LogTokenError(query, error, out replacement);
			}
			GameLocation locationFromName = Game1.getLocationFromName(value);
			if (locationFromName == null)
			{
				return LogTokenError(query, "no location found with name '" + value + "'", out replacement);
			}
			replacement = locationFromName.DisplayName;
			return true;
		}

		/// <summary>The value of a tracked player stat.</summary>
		/// <inheritdoc cref="T:StardewValley.TokenizableStrings.TokenParserDelegate" />
		public static bool FarmerStat(string[] query, out string replacement, Random random, Farmer player)
		{
			if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string statName"))
			{
				return LogTokenError(query, error, out replacement);
			}
			replacement = player.stats.Get(value).ToString();
			return true;
		}
	}

	/// <summary>The supported tokens and their resolvers.</summary>
	private static readonly Dictionary<string, TokenParserDelegate> Parsers;

	/// <summary>The character used to escape spaces in token arguments.</summary>
	private const char EscapedSpace = '\u00a0';

	/// <summary>The character used to escape an empty argument.</summary>
	private const char EscapedEmpty = '\u200b';

	/// <summary>The character used to escape an empty argument.</summary>
	private static readonly string EscapedEmptyStr;

	/// <summary>The character used to start a token.</summary>
	internal const char StartTokenChar = '[';

	/// <summary>The character used to end a token.</summary>
	internal const char EndTokenChar = ']';

	/// <summary>The characters which, when present in a tokenizable string, indicate that the string should be wrapped in [EscapedText] when used as an argument.</summary>
	internal static readonly char[] HeuristicCharactersForEscapableStrings;

	/// <summary>Register the default game state queries, defined as <see cref="T:StardewValley.TokenizableStrings.TokenParser.DefaultResolvers" /> methods.</summary>
	static TokenParser()
	{
		Parsers = new Dictionary<string, TokenParserDelegate>(StringComparer.OrdinalIgnoreCase);
		EscapedEmptyStr = '\u200b'.ToString();
		HeuristicCharactersForEscapableStrings = new char[2] { ' ', '[' };
		MethodInfo[] methods = typeof(DefaultResolvers).GetMethods(BindingFlags.Static | BindingFlags.Public);
		foreach (MethodInfo methodInfo in methods)
		{
			TokenParserDelegate value = (TokenParserDelegate)Delegate.CreateDelegate(typeof(TokenParserDelegate), methodInfo);
			Parsers[methodInfo.Name] = value;
		}
	}

	/// <summary>Register a custom token parser.</summary>
	/// <param name="tokenKey">The token key. This should only contain alphanumeric, underscore, and dot characters. For custom queries, this should be prefixed with your mod ID like <c>Example.ModId_TokenName</c>.</param>
	/// <param name="parser">The parses which returns the text to use for a given token tag.</param>
	public static void RegisterParser(string tokenKey, TokenParserDelegate parser)
	{
		if (string.IsNullOrWhiteSpace(tokenKey))
		{
			throw new ArgumentException("The token key can't be empty.", "tokenKey");
		}
		if (parser == null)
		{
			throw new ArgumentException("The parser callback for token key '" + tokenKey + "' can't be null.", "parser");
		}
		tokenKey = tokenKey.Trim();
		if (!Parsers.TryAdd(tokenKey, parser))
		{
			throw new ArgumentException("Can't add token parser for key '" + tokenKey + "' because one is already registered for it.");
		}
	}

	/// <summary>Escape spaces within a tokenized string so it can be passed as an argument to tokens. The characters will automatically be converted back into spaces when parsed.</summary>
	/// <param name="text">The text to modify.</param>
	public static string EscapeSpaces(string text)
	{
		if (text.Length <= 0)
		{
			return EscapedEmptyStr;
		}
		return text.Replace(' ', '\u00a0');
	}

	/// <summary>Parse text containing tokens like "<c>It's a nice [Season] day</c>" into the resulting display text.</summary>
	/// <param name="text">The text to parse.</param>
	/// <param name="random">The RNG to use for randomization, or <c>null</c> to use <see cref="F:StardewValley.Game1.random" />.</param>
	/// <param name="customParser">A custom token parser which will be given an opportunity to parse each token first, if any.</param>
	/// <param name="player">The player to use for any player-related checks, or <c>null</c> to use <see cref="P:StardewValley.Game1.player" />.</param>
	/// <returns>Returns the modified text.</returns>
	public static string ParseText(string text, Random random = null, TokenParserDelegate customParser = null, Farmer player = null)
	{
		if (text == null)
		{
			return null;
		}
		int num = text.IndexOf('[');
		if (num == -1)
		{
			return text;
		}
		for (int i = num; i < text.Length; i++)
		{
			if (text[i] == '[')
			{
				i = ParseTagStartingAt(ref text, i, random ?? Game1.random, customParser, player ?? Game1.player);
			}
		}
		return UnescapeText(text.Replace("\\n", "\n"));
	}

	/// <summary>Log an error indicating that a token could not be parsed.</summary>
	/// <param name="query">The full token string split by spaces, including the token name.</param>
	/// <param name="error">The error indicating why parsing failed.</param>
	/// <param name="replacement">The replacement value to set.</param>
	/// <returns>Returns <c>false</c> for convenience.</returns>
	public static bool LogTokenError(string[] query, string error, out string replacement)
	{
		Game1.log.Error($"Failed parsing [{string.Join(" ", query)}]: {error}.");
		replacement = null;
		return false;
	}

	/// <summary>Log an error indicating that a token could not be parsed.</summary>
	/// <param name="query">The full token string split by spaces, including the token name.</param>
	/// <param name="error">The error indicating why parsing failed.</param>
	/// <param name="replacement">The replacement value to set.</param>
	/// <returns>Returns <c>false</c> for convenience.</returns>
	public static bool LogTokenError(string[] query, Exception error, out string replacement)
	{
		Game1.log.Error("Failed parsing [" + string.Join(" ", query) + "].", error);
		replacement = null;
		return false;
	}

	/// <summary>Parse a tag within a text starting at the given index.</summary>
	/// <param name="text">The full text being parsed.</param>
	/// <param name="startIndex">The index at which the token appears, including the <see cref="F:StardewValley.TokenizableStrings.TokenParser.StartTokenChar" />.</param>
	/// <param name="random">The RNG to use for randomization.</param>
	/// <param name="customParser">A custom token parser which will be given an opportunity to parse each token first, if any.</param>
	/// <param name="player">The player to use for any player-related checks.</param>
	/// <returns>Returns the index within the <paramref name="text" /> at which to resume parsing.</returns>
	private static int ParseTagStartingAt(ref string text, int startIndex, Random random, TokenParserDelegate customParser, Farmer player)
	{
		for (int i = startIndex + 1; i < text.Length; i++)
		{
			switch (text[i])
			{
			case '[':
				i = ParseTagStartingAt(ref text, i, random, customParser, player);
				break;
			case ']':
			{
				if (ParseTag(text.Substring(startIndex + 1, i - startIndex - 1), out var replacement, random, customParser, player))
				{
					text = text.Remove(startIndex, i - startIndex + 1);
					text = text.Insert(startIndex, replacement);
					return startIndex + replacement.Length - 1;
				}
				return i;
			}
			}
		}
		return text.Length - 1;
	}

	/// <summary>Parse a tag substring within a text.</summary>
	/// <param name="tag">The token tag to parse, excluding the <see cref="F:StardewValley.TokenizableStrings.TokenParser.StartTokenChar" /> and <see cref="F:StardewValley.TokenizableStrings.TokenParser.EndTokenChar" /> characters.</param>
	/// <param name="replacement">The output string with which to replace the token within the text being parsed.</param>
	/// <param name="random">The RNG to use for randomization.</param>
	/// <param name="customParser">A custom token parser which will be given an opportunity to parse each token first, if any.</param>
	/// <param name="player">The player to use for any player-related checks.</param>
	/// <returns>Returns whether the tag was successfully parsed.</returns>
	private static bool ParseTag(string tag, out string replacement, Random random, TokenParserDelegate customParser, Farmer player)
	{
		string[] array = ArgUtility.SplitBySpace(tag);
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = UnescapeText(array[i]);
		}
		if (customParser != null && customParser(array, out replacement, random, player))
		{
			return true;
		}
		if (Parsers.TryGetValue(array[0], out var value) && value(array, out replacement, random, player))
		{
			return true;
		}
		replacement = null;
		return false;
	}

	/// <summary>Reverse replacements from <see cref="M:StardewValley.TokenizableStrings.TokenParser.EscapeSpaces(System.String)" />.</summary>
	/// <param name="text">The text to unescape.</param>
	private static string UnescapeText(string text)
	{
		return text.Replace('\u00a0', ' ').Replace(EscapedEmptyStr, "");
	}
}
