using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace StardewValley.Tests;

/// <summary>Converts raw text from dialogue, event, mail, and data assets into language-independent syntax representations, which can be compared between languages to make sure they have the same sequence of commands, portraits, unlocalized metadata and delimiters, etc.</summary>
/// <remarks>
///   <para><strong>This is highly specialized.</strong> It's meant for vanilla unit tests, so it may not correctly handle non-vanilla text and may change at any time.</para>
///
///   For example, this converts a dialogue string like this:
///   <code>$c 0.5#Wow... Thanks, @!$h#Thank you! It's so pretty.</code>
///
///   Into a language-independent representation like this:
///   <code>$c 0.5#text$h#text</code>
/// </remarks>
public class SyntaxAbstractor
{
	/// <summary>The placeholder in <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractSyntaxFor(System.String,System.String,System.String)" /> for localizable text.</summary>
	public const string TextMarker = "text";

	/// <summary>The implementations which extract syntax from specific assets, indexed by exact match or prefix.</summary>
	public readonly Dictionary<string, ExtractSyntaxDelegate> SyntaxHandlers = new Dictionary<string, ExtractSyntaxDelegate>(StringComparer.OrdinalIgnoreCase)
	{
		["Characters/Dialogue/*"] = DialogueSyntaxHandler,
		["Data/EngagementDialogue"] = DialogueSyntaxHandler,
		["Data/ExtraDialogue"] = DialogueSyntaxHandler,
		["Strings/animationDescriptions"] = DialogueSyntaxHandler,
		["Strings/Buildings"] = DialogueSyntaxHandler,
		["Strings/Characters"] = DialogueSyntaxHandler,
		["Strings/Events"] = DialogueSyntaxHandler,
		["Strings/Locations"] = DialogueSyntaxHandler,
		["Strings/MovieReactions"] = DialogueSyntaxHandler,
		["Strings/Objects"] = DialogueSyntaxHandler,
		["Strings/Quests"] = DialogueSyntaxHandler,
		["Strings/schedules/*"] = DialogueSyntaxHandler,
		["Strings/SimpleNonVillagerDialogues"] = DialogueSyntaxHandler,
		["Strings/SpecialOrderStrings"] = DialogueSyntaxHandler,
		["Strings/SpeechBubbles"] = DialogueSyntaxHandler,
		["Strings/StringsFromCSFiles"] = DialogueSyntaxHandler,
		["Strings/StringsFromMaps"] = DialogueSyntaxHandler,
		["Strings/BigCraftables"] = PlainTextSyntaxHandler,
		["Strings/BundleNames"] = PlainTextSyntaxHandler,
		["Strings/EnchantmentNames"] = PlainTextSyntaxHandler,
		["Strings/FarmAnimals"] = PlainTextSyntaxHandler,
		["Strings/Furniture"] = PlainTextSyntaxHandler,
		["Strings/MovieConcessions"] = PlainTextSyntaxHandler,
		["Strings/Movies"] = PlainTextSyntaxHandler,
		["Strings/NPCNames"] = PlainTextSyntaxHandler,
		["Strings/Pants"] = PlainTextSyntaxHandler,
		["Strings/Shirts"] = PlainTextSyntaxHandler,
		["Strings/Tools"] = PlainTextSyntaxHandler,
		["Strings/TV/TipChannel"] = PlainTextSyntaxHandler,
		["Strings/UI"] = PlainTextSyntaxHandler,
		["Strings/Weapons"] = PlainTextSyntaxHandler,
		["Strings/WorldMap"] = PlainTextSyntaxHandler,
		["Data/Events/*"] = EventSyntaxHandler,
		["Data/Festivals/*"] = FestivalSyntaxHandler,
		["Data/Achievements"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractDelimitedDataSyntax(text, '^', 0, 1),
		["Data/Boots"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractDelimitedDataSyntax(text, '/', 1, 6),
		["Data/Bundles"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractDelimitedDataSyntax(text, '/', 6),
		["Data/hats"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractDelimitedDataSyntax(text, '/', 1, 5),
		["Data/Monsters"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractDelimitedDataSyntax(text, '/', 14),
		["Data/NPCGiftTastes"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => (!key.StartsWith("Universal_")) ? syntaxBuilder.ExtractDelimitedDataSyntax(text, '/', 0, 2, 4, 6, 8) : text,
		["Data/Quests"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractDelimitedDataSyntax(text, '/', new int[3] { 1, 2, 3 }, new int[1] { 9 }),
		["Data/TV/CookingChannel"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractDelimitedDataSyntax(text, '/', 1),
		["Data/mail"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractMailSyntax(text),
		["Data/Notes"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractMailSyntax(text),
		["Data/SecretNotes"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractMailSyntax(text),
		["Strings/credits"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractCreditsSyntax(text),
		["Strings/1_6_Strings"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.Extract16StringsSyntax(key, text),
		["Strings/Lexicon"] = (SyntaxAbstractor syntaxBuilder, string _, string key, string text) => syntaxBuilder.ExtractLexiconSyntax(key, text)
	};

	/// <summary>Get a handler which can extract syntactic representations for a given asset.</summary>
	/// <param name="baseAssetName">The asset name without the locale suffix, like <c>Data/Achievements</c>.</param>
	/// <remarks>Most code should use <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractSyntaxFor(System.String,System.String,System.String)" /> or a specific method like <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractDialogueSyntax(System.String)" /> instead.</remarks>
	public ExtractSyntaxDelegate GetSyntaxHandler(string baseAssetName)
	{
		if (SyntaxHandlers.TryGetValue(baseAssetName, out var value))
		{
			return value;
		}
		int num = baseAssetName.LastIndexOf('/');
		if (num != -1 && SyntaxHandlers.TryGetValue(baseAssetName.Substring(0, num) + "/*", out value))
		{
			return value;
		}
		return null;
	}

	/// <summary>Get a syntactic representation of an arbitrary asset entry, if it's a known asset.</summary>
	/// <param name="baseAssetName">The asset name without the locale suffix, like <c>Data/Achievements</c>.</param>
	/// <param name="key">The key within the asset for the text value.</param>
	/// <param name="value">The text to represent.</param>
	public string ExtractSyntaxFor(string baseAssetName, string key, string value)
	{
		if (value.Contains("${"))
		{
			value = Regex.Replace(value, "\\$\\{.+?\\}\\$", "text");
		}
		return GetSyntaxHandler(baseAssetName)?.Invoke(this, baseAssetName, key, value) ?? value;
	}

	/// <summary>Get a syntactic representation of plain text which has no special syntax.</summary>
	/// <param name="value">The text to represent.</param>
	public string ExtractPlainTextSyntax(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return "text";
		}
		return string.Empty;
	}

	/// <summary>Get a syntactic representation of a dialogue string.</summary>
	/// <param name="value">The text to represent.</param>
	/// <remarks>This handles the general syntax format. For asset-specific formats, see <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractDialogueSyntax(System.String,System.String,System.String)" /> instead.</remarks>
	public string ExtractDialogueSyntax(string value)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int index = 0;
		ExtractDialogueSyntaxImpl(value, '#', ref index, stringBuilder);
		return stringBuilder.ToString();
	}

	/// <summary>Get a syntactic representation of a dialogue string.</summary>
	/// <param name="baseAssetName">The asset name without the locale suffix, like <c>Data/Achievements</c>.</param>
	/// <param name="key">The key within the asset for the text value.</param>
	/// <param name="value">The text to represent.</param>
	/// <remarks>This supports asset-specific dialogue formats. In particular, some translations are loaded via <see cref="M:StardewValley.Game1.LoadStringByGender(StardewValley.Gender,System.String)" /> which supports a special <c>male/female</c> format based on the NPC's gender (not the player's gender).</remarks>
	public string ExtractDialogueSyntax(string baseAssetName, string key, string value)
	{
		switch (baseAssetName)
		{
		case "Data/ExtraDialogue":
			switch (key)
			{
			case "NewChild_Adoption":
			case "NewChild_FirstChild":
			case "NewChild_SecondChild1":
			case "NewChild_SecondChild2":
				return ExtractNpcGenderedDialogueSyntax(value);
			}
			break;
		case "Strings/Locations":
			if (key == "FarmHouse_SpouseAttacked3")
			{
				return "text";
			}
			break;
		case "Strings/StringsFromCSFiles":
			switch (key)
			{
			case "Pipe":
				return "text";
			case "Event.cs.1497":
			case "Event.cs.1498":
			case "Event.cs.1499":
			case "Event.cs.1500":
			case "Event.cs.1501":
			case "Event.cs.1504":
			case "NPC.cs.3957":
			case "NPC.cs.3959":
			case "NPC.cs.3962":
			case "NPC.cs.3963":
			case "NPC.cs.3965":
			case "NPC.cs.3966":
			case "NPC.cs.3968":
			case "NPC.cs.3974":
			case "NPC.cs.3975":
			case "NPC.cs.4079":
			case "NPC.cs.4080":
			case "NPC.cs.4088":
			case "NPC.cs.4089":
			case "NPC.cs.4091":
			case "NPC.cs.4113":
			case "NPC.cs.4115":
			case "NPC.cs.4141":
			case "NPC.cs.4144":
			case "NPC.cs.4146":
			case "NPC.cs.4147":
			case "NPC.cs.4149":
			case "NPC.cs.4152":
			case "NPC.cs.4153":
			case "NPC.cs.4154":
			case "NPC.cs.4274":
			case "NPC.cs.4276":
			case "NPC.cs.4277":
			case "NPC.cs.4278":
			case "NPC.cs.4279":
			case "NPC.cs.4293":
			case "NPC.cs.4422":
			case "NPC.cs.4446":
			case "NPC.cs.4447":
			case "NPC.cs.4449":
			case "NPC.cs.4452":
			case "NPC.cs.4455":
			case "NPC.cs.4462":
			case "NPC.cs.4470":
			case "NPC.cs.4474":
			case "NPC.cs.4481":
			case "NPC.cs.4488":
			case "NPC.cs.4498":
			case "NPC.cs.4500":
				return ExtractNpcGenderedDialogueSyntax(value);
			case "OptionsPage.cs.11289":
			case "OptionsPage.cs.11290":
			case "OptionsPage.cs.11291":
			case "OptionsPage.cs.11292":
			case "OptionsPage.cs.11293":
			case "OptionsPage.cs.11294":
			case "OptionsPage.cs.11295":
			case "OptionsPage.cs.11296":
			case "OptionsPage.cs.11297":
			case "OptionsPage.cs.11298":
			case "OptionsPage.cs.11299":
			case "OptionsPage.cs.11300":
				if (!string.IsNullOrWhiteSpace(value))
				{
					return "text";
				}
				break;
			}
			break;
		}
		return ExtractDialogueSyntax(value);
	}

	/// <summary>Get a syntactic representation of a dialogue string.</summary>
	/// <param name="value">The text to represent.</param>
	public string ExtractEventSyntax(string value)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int index = 0;
		ExtractEventSyntaxImpl(value, ref index, stringBuilder);
		return stringBuilder.ToString();
	}

	/// <summary>Get a syntactic representation of a festival string.</summary>
	/// <param name="baseAssetName">The asset name without the locale suffix, like <c>Data/Achievements</c>.</param>
	/// <param name="key">The key within the asset for the text value.</param>
	/// <param name="value">The text to represent.</param>
	public string ExtractFestivalSyntax(string baseAssetName, string key, string value)
	{
		switch (key)
		{
		case "mainEvent":
		case "set-up_y2":
		case "mainEvent_y2":
		case "conditions":
		case "set-up":
			return ExtractEventSyntax(value);
		case "afterEggHunt":
		case "AbbyWin":
		case "afterEggHunt_y2":
			if (baseAssetName == "Data/Festivals/spring13")
			{
				return ExtractEventSyntax(value);
			}
			break;
		case "governorReaction0":
		case "governorReaction1":
		case "governorReaction2":
		case "governorReaction3":
		case "governorReaction4":
		case "governorReaction5":
		case "governorReaction6":
			if (baseAssetName == "Data/Festivals/summer11")
			{
				return ExtractEventSyntax(value);
			}
			break;
		}
		return ExtractDialogueSyntax(value);
	}

	/// <summary>Get a syntactic representation of a <c>Strings/credits</c> entry.</summary>
	/// <param name="text">The text to represent.</param>
	/// <remarks>See parsing logic in <see cref="M:StardewValley.Menus.AboutMenu.SetUpCredits" />.</remarks>
	public string ExtractCreditsSyntax(string text)
	{
		if (text.Length == 0)
		{
			return text;
		}
		if (text.StartsWith('['))
		{
			if (text.StartsWith("[image]"))
			{
				return text;
			}
			if (text.StartsWith("[link]"))
			{
				string[] array = text.Split(' ', 3);
				array[2] = "text";
				return string.Join(" ", array);
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		int i = 0;
		bool hasText = false;
		for (; i < text.Length; i++)
		{
			if (text[i] == '[')
			{
				EndTextContext(ref hasText, stringBuilder);
				ExtractTagSyntax(text, ref i, stringBuilder);
			}
			else
			{
				hasText = true;
			}
		}
		EndTextContext(ref hasText, stringBuilder);
		return stringBuilder.ToString();
	}

	/// <summary>Get a syntactic representation of a mail string.</summary>
	/// <param name="text">The text to represent.</param>
	/// <remarks>This handles the general syntax format. For asset-specific formats, see <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractDialogueSyntax(System.String,System.String,System.String)" /> instead.</remarks>
	public string ExtractMailSyntax(string text)
	{
		text = text.Replace("%secretsanta", "text");
		StringBuilder stringBuilder = new StringBuilder();
		int i = 0;
		bool hasText = false;
		for (; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case '¦':
				EndTextContext(ref hasText, stringBuilder);
				stringBuilder.Append(c);
				break;
			case '[':
				EndTextContext(ref hasText, stringBuilder);
				ExtractTagSyntax(text, ref i, stringBuilder);
				break;
			case '%':
				if (i >= text.Length || char.IsWhiteSpace(text[i + 1]) || char.IsDigit(text[i + 1]))
				{
					hasText = true;
					break;
				}
				EndTextContext(ref hasText, stringBuilder);
				ExtractMailCommandSyntax(text, ref i, stringBuilder);
				break;
			default:
				if (!hasText)
				{
					hasText = true;
				}
				break;
			case ' ':
				break;
			}
		}
		EndTextContext(ref hasText, stringBuilder);
		return stringBuilder.ToString();
	}

	/// <summary>Get a syntactic representation of a data entry containing delimited fields.</summary>
	/// <param name="text">The dialogue entry.</param>
	/// <param name="delimiter">The delimiter between fields.</param>
	/// <param name="textFields">The field indices containing localized text, which should be replaced by <see cref="F:StardewValley.Tests.SyntaxAbstractor.TextMarker" />.</param>
	public string ExtractDelimitedDataSyntax(string text, char delimiter, params int[] textFields)
	{
		return ExtractDelimitedDataSyntax(text, delimiter, textFields, null);
	}

	/// <summary>Get a syntactic representation of a data entry containing delimited fields.</summary>
	/// <param name="text">The dialogue entry.</param>
	/// <param name="delimiter">The delimiter between fields.</param>
	/// <param name="textFields">The field indices containing localized text, which should be replaced by <see cref="F:StardewValley.Tests.SyntaxAbstractor.TextMarker" />.</param>
	/// <param name="dialogueFields">The field indices containing dialogue text.</param>
	public string ExtractDelimitedDataSyntax(string text, char delimiter, int[] textFields, int[] dialogueFields)
	{
		string[] array = text.Split(delimiter);
		int[] array2 = textFields;
		foreach (int num in array2)
		{
			if (ArgUtility.HasIndex(array, num))
			{
				array[num] = "text";
			}
		}
		if (dialogueFields != null)
		{
			array2 = dialogueFields;
			foreach (int num2 in array2)
			{
				if (ArgUtility.HasIndex(array, num2))
				{
					array[num2] = ExtractDialogueSyntax(array[num2]);
				}
			}
		}
		return string.Join(delimiter.ToString(), array);
	}

	/// <summary>Get a syntactic representation of a string from <c>Strings/1_6_Strings</c>.</summary>
	/// <param name="key">The key within the asset for the text value.</param>
	/// <param name="text">The text to represent.</param>
	public string Extract16StringsSyntax(string key, string text)
	{
		if (key.StartsWith("Renovation_"))
		{
			return ExtractDelimitedDataSyntax(text, '/', LegacyShims.EmptyArray<int>(), new int[3] { 0, 1, 2 });
		}
		if (!(key == "ForestPylonEvent"))
		{
			if (key == "StarterChicken_Names")
			{
				string[] array = text.Split('|');
				StringBuilder stringBuilder = new StringBuilder();
				bool flag = false;
				string[] array2 = array;
				foreach (string text2 in array2)
				{
					if (text2.Split(',', 3).Length == 2)
					{
						if (stringBuilder.Length == 0)
						{
							stringBuilder.Append("name,name");
						}
						else
						{
							flag = true;
						}
						continue;
					}
					if (stringBuilder.Length > 0)
					{
						stringBuilder.Append(" | ");
					}
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
					handler.AppendLiteral("<invalid pair: ");
					handler.AppendFormatted(text2.Trim());
					handler.AppendLiteral(">");
					stringBuilder2.Append(ref handler);
				}
				if (flag)
				{
					return $"{stringBuilder} | ...";
				}
				if (stringBuilder.Length > 0)
				{
					return stringBuilder.ToString();
				}
				return string.Empty;
			}
			return ExtractDialogueSyntax(text);
		}
		return ExtractEventSyntax(text);
	}

	/// <summary>Get a syntactic representation of a string from <c>Strings/Lexicon</c>.</summary>
	/// <param name="key">The key within the asset for the text value.</param>
	/// <param name="text">The text to represent.</param>
	public string ExtractLexiconSyntax(string key, string text)
	{
		string[] array = text.Split('#');
		for (int i = 0; i < array.Length; i++)
		{
			if (!string.IsNullOrWhiteSpace(array[i]))
			{
				string text2 = array[i];
				int num = text2.Length - text2.TrimStart().Length;
				int num2 = text2.Length - text2.TrimEnd().Length;
				array[i] = ((num > 0 || num2 > 0) ? ("".PadRight(num) + "text" + "".PadRight(num2)) : "text");
			}
		}
		if (key.StartsWith("Random") && array.Length > 2)
		{
			return array[0] + "#" + array[1] + "#...";
		}
		return string.Join("#", array);
	}

	/// <summary>A shortcut for calling <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractDialogueSyntax(System.String,System.String,System.String)" /> in <see cref="F:StardewValley.Tests.SyntaxAbstractor.SyntaxHandlers" />.</summary>
	/// <inheritdoc cref="T:StardewValley.Tests.ExtractSyntaxDelegate" />
	private static string DialogueSyntaxHandler(SyntaxAbstractor syntaxAbstractor, string baseAssetName, string key, string text)
	{
		return syntaxAbstractor.ExtractDialogueSyntax(baseAssetName, key, text);
	}

	/// <summary>A shortcut for calling <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractPlainTextSyntax(System.String)" /> in <see cref="F:StardewValley.Tests.SyntaxAbstractor.SyntaxHandlers" />.</summary>
	/// <inheritdoc cref="T:StardewValley.Tests.ExtractSyntaxDelegate" />
	private static string PlainTextSyntaxHandler(SyntaxAbstractor syntaxAbstractor, string baseAssetName, string key, string text)
	{
		return syntaxAbstractor.ExtractPlainTextSyntax(text);
	}

	/// <summary>A shortcut for calling <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractEventSyntax(System.String)" /> in <see cref="F:StardewValley.Tests.SyntaxAbstractor.SyntaxHandlers" />.</summary>
	/// <inheritdoc cref="T:StardewValley.Tests.ExtractSyntaxDelegate" />
	private static string EventSyntaxHandler(SyntaxAbstractor syntaxAbstractor, string baseAssetName, string key, string text)
	{
		return syntaxAbstractor.ExtractEventSyntax(text);
	}

	/// <summary>A shortcut for calling <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractFestivalSyntax(System.String,System.String,System.String)" /> in <see cref="F:StardewValley.Tests.SyntaxAbstractor.SyntaxHandlers" />.</summary>
	/// <inheritdoc cref="T:StardewValley.Tests.ExtractSyntaxDelegate" />
	private static string FestivalSyntaxHandler(SyntaxAbstractor syntaxAbstractor, string baseAssetName, string key, string text)
	{
		return syntaxAbstractor.ExtractFestivalSyntax(baseAssetName, key, text);
	}

	/// <summary>As part of <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractSyntaxFor(System.String,System.String,System.String)" />, read a syntax representation of an event script.</summary>
	/// <param name="text">The event script.</param>
	/// <param name="index">The index at which to read the next character. After this method runs, it will be set to the last character in the event script.</param>
	/// <param name="syntax">The string builder to extend with the current command's syntax.</param>
	/// <param name="maxIndex">If set, the index at which to stop reading the string.</param>
	private void ExtractEventSyntaxImpl(string text, ref int index, StringBuilder syntax, int maxIndex = -1)
	{
		string[] array = ArgUtility.SplitQuoteAware((index == 0 && maxIndex < 0) ? text : text.Substring(index, maxIndex - index + 1), '/', StringSplitOptions.TrimEntries, keepQuotesAndEscapes: true);
		bool flag = true;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!flag)
			{
				syntax.Append('/');
			}
			if (!string.IsNullOrWhiteSpace(text2))
			{
				string[] array3 = ArgUtility.SplitBySpaceQuoteAware(text2);
				string text3 = array3[0];
				syntax.Append(text3);
				int j = 1;
				if (Event.TryResolveCommandName(text3, out var actualName))
				{
					switch (actualName)
					{
					case "End":
					{
						string text4 = ArgUtility.Get(array3, 1);
						if (text4 == "dialogue" || text4 == "dialogueWarpOut")
						{
							AppendEventCommandArg(syntax, array3, 1);
							AppendEventCommandArg(syntax, array3, 2);
							AppendEventCommandDialogueArg(syntax, array3, 3);
							j = 4;
						}
						break;
					}
					case "Message":
						AppendEventCommandDialogueArg(syntax, array3, 1);
						j = 2;
						break;
					case "Question":
						AppendEventCommandArg(syntax, array3, 1);
						AppendEventCommandDialogueArg(syntax, array3, 2);
						j = 3;
						break;
					case "QuickQuestion":
					{
						string[] array5 = LegacyShims.SplitAndTrim(text2.Substring(text2.IndexOf(' ')), "(break)");
						string[] array6 = LegacyShims.SplitAndTrim(array5[0], '#');
						syntax.Append(" \"");
						AppendEventCommandDialogueArg(syntax, array6, 0, prependSpace: true, quote: false);
						for (int l = 1; l < array6.Length; l++)
						{
							syntax.Append('#');
							AppendEventCommandDialogueArg(syntax, array6, l, prependSpace: false, quote: false);
						}
						for (int m = 1; m < array5.Length; m++)
						{
							array5[m] = array5[m].Replace('\\', '/');
							syntax.Append("(break)");
							int index2 = 0;
							ExtractEventSyntaxImpl(array5[m], ref index2, syntax);
						}
						syntax.Append('"');
						j = array3.Length;
						break;
					}
					case "Speak":
						AppendEventCommandArg(syntax, array3, 1);
						AppendEventCommandDialogueArg(syntax, array3, 2);
						j = 3;
						break;
					case "SplitSpeak":
					{
						string[] array4 = ArgUtility.Get(array3, 2)?.Split('~');
						AppendEventCommandArg(syntax, array3, 1);
						if (array4 != null)
						{
							syntax.Append(" \"");
							for (int k = 0; k < array4.Length; k++)
							{
								if (k > 0)
								{
									syntax.Append('~');
								}
								AppendEventCommandDialogueArg(syntax, array4, k, prependSpace: false, quote: false);
							}
							syntax.Append('"');
						}
						j = 3;
						break;
					}
					case "SpriteText":
						AppendEventCommandArg(syntax, array3, 1);
						AppendEventCommandDialogueArg(syntax, array3, 2);
						j = 3;
						break;
					case "TextAboveHead":
						AppendEventCommandArg(syntax, array3, 1);
						AppendEventCommandDialogueArg(syntax, array3, 2);
						j = 3;
						break;
					}
				}
				for (; j < array3.Length; j++)
				{
					AppendEventCommandArg(syntax, array3, j);
				}
			}
			flag = false;
		}
		index = ((maxIndex > 0) ? maxIndex : (text.Length - 1));
	}

	/// <summary>Append an event command argument to a syntax string being built, including the preceding space.</summary>
	/// <param name="syntax">The syntax string being built.</param>
	/// <param name="args">The command arguments.</param>
	/// <param name="index">The index of the argument in <paramref name="args" /> to append.</param>
	/// <param name="prependSpace">Whether to prepend a space before the argument.</param>
	private void AppendEventCommandArg(StringBuilder syntax, string[] args, int index, bool prependSpace = true)
	{
		if (ArgUtility.HasIndex(args, index))
		{
			string text = args[index];
			bool num = text.Contains(' ');
			if (prependSpace)
			{
				syntax.Append(' ');
			}
			if (num)
			{
				syntax.Append('"');
			}
			syntax.Append(text);
			if (num)
			{
				syntax.Append('"');
			}
		}
	}

	/// <summary>Append an event command argument containing dialogue syntax to a syntax string being built, including the preceding space.</summary>
	/// <param name="syntax">The syntax string being built.</param>
	/// <param name="args">The command arguments.</param>
	/// <param name="index">The index of the argument in <paramref name="args" /> to append.</param>
	/// <param name="prependSpace">Whether to prepend a space before the argument.</param>
	/// <param name="quote">Whether to quote the dialogue string.</param>
	private void AppendEventCommandDialogueArg(StringBuilder syntax, string[] args, int index, bool prependSpace = true, bool quote = true)
	{
		if (ArgUtility.HasIndex(args, index))
		{
			string text = args[index];
			int index2 = 0;
			if (prependSpace)
			{
				syntax.Append(' ');
			}
			if (quote)
			{
				syntax.Append('"');
			}
			ExtractDialogueSyntaxImpl(text, '/', ref index2, syntax);
			if (quote)
			{
				syntax.Append('"');
			}
		}
	}

	/// <summary>As part of <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractDialogueSyntax(System.String,System.String,System.String)" />, extract the syntax for a dialogue which is NPC-gendered via <see cref="M:StardewValley.Game1.LoadStringByGender(StardewValley.Gender,System.String)" />.</summary>
	/// <param name="text">The dialogue entry.</param>
	private string ExtractNpcGenderedDialogueSyntax(string text)
	{
		if (!text.Contains('/'))
		{
			return ExtractDialogueSyntax(text);
		}
		string[] array = text.Split('/');
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ExtractDialogueSyntax(array[i]);
		}
		if (array.Length != 2 || !(array[0] == array[1]))
		{
			return string.Join("/", array);
		}
		return array[0];
	}

	/// <summary>As part of <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractSyntaxFor(System.String,System.String,System.String)" />, read a syntax representation of a dialogue entry.</summary>
	/// <param name="text">The dialogue entry.</param>
	/// <param name="commandDelimiter">Within the larger asset, the character which delimits commands. This is usually <c>#</c> for dialogue, or <c>/</c> for event data. This is used in certain specialized cases like <see cref="F:StardewValley.Dialogue.dialogueQuickResponse" />, which extends to the end of the command.</param>
	/// <param name="index">The index at which to read the next character. After this method runs, it will be set to the last character in the dialogue string.</param>
	/// <param name="syntax">The string builder to extend with the current command's syntax.</param>
	/// <param name="maxIndex">If set, the index at which to stop reading the string.</param>
	private void ExtractDialogueSyntaxImpl(string text, char commandDelimiter, ref int index, StringBuilder syntax, int maxIndex = -1)
	{
		bool hasText = false;
		bool flag = false;
		if (maxIndex < 0 || maxIndex > text.Length - 1)
		{
			maxIndex = text.Length - 1;
		}
		while (index <= maxIndex)
		{
			char c = text[index];
			switch (c)
			{
			case '#':
			case '$':
			case '|':
				if (c == '$' && flag && !hasText)
				{
					syntax.Append("text");
				}
				EndTextContext(ref hasText, syntax);
				flag = false;
				if (c == '$')
				{
					ExtractDialogueCommandSyntax(text, ref index, syntax, commandDelimiter);
				}
				else
				{
					syntax.Append(c);
				}
				break;
			case '[':
				EndTextContext(ref hasText, syntax);
				ExtractDialogueItemSpawnSyntax(text, ref index, syntax);
				flag = false;
				break;
			case ']':
				EndTextContext(ref hasText, syntax);
				syntax.Append(']');
				flag = false;
				break;
			default:
				if (c == ' ')
				{
					flag = true;
				}
				else
				{
					hasText = true;
				}
				break;
			}
			index++;
		}
		EndTextContext(ref hasText, syntax);
	}

	/// <summary>As part of <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractDialogueSyntaxImpl(System.String,System.Char,System.Int32@,System.Text.StringBuilder,System.Int32)" />, read a syntax representation of a single command from the input string.</summary>
	/// <param name="text">The dialogue or event text.</param>
	/// <param name="index">The index at which to read the next character. After this method runs, it will be set to the last character in the command.</param>
	/// <param name="syntax">The string builder to extend with the current command's syntax.</param>
	/// <param name="commandDelimiter">Within the larger asset, the character which delimits commands. This is usually <c>#</c> for dialogue, or <c>/</c> for event data. This is used in certain specialized cases like <see cref="F:StardewValley.Dialogue.dialogueQuickResponse" />, which extends to the end of the command.</param>
	private void ExtractDialogueCommandSyntax(string text, ref int index, StringBuilder syntax, char commandDelimiter)
	{
		int num = index;
		index++;
		while (index < text.Length && (char.IsLetter(text[index]) || char.IsNumber(text[index])))
		{
			index++;
		}
		string text2 = text.Substring(num, index - num);
		syntax.Append(text2);
		if (text2 != null)
		{
			int length = text2.Length;
			if (length != 2)
			{
				if (length == 6 && text2 == "$query")
				{
					goto IL_01a8;
				}
			}
			else
			{
				int num2;
				switch (text2[1])
				{
				case 'c':
					if (text2 == "$c")
					{
						goto IL_0166;
					}
					goto IL_03a0;
				case 'q':
					if (text2 == "$q")
					{
						goto IL_0166;
					}
					goto IL_03a0;
				case 'r':
					if (text2 == "$r")
					{
						goto IL_0166;
					}
					goto IL_03a0;
				case '1':
					if (text2 == "$1")
					{
						goto IL_0166;
					}
					goto IL_03a0;
				case 't':
					if (text2 == "$t")
					{
						goto IL_0166;
					}
					goto IL_03a0;
				case 'd':
					break;
				case 'p':
					goto IL_012d;
				case 'y':
					goto IL_013f;
				default:
					goto IL_03a0;
					IL_0166:
					num2 = index;
					while (index < text.Length && text[index] != '#')
					{
						index++;
					}
					syntax.Append(text.Substring(num2, index - num2).TrimEnd(' '));
					goto IL_03a0;
				}
				if (text2 == "$d")
				{
					goto IL_01a8;
				}
			}
		}
		goto IL_03a0;
		IL_012d:
		if (text2 == "$p")
		{
			goto IL_01a8;
		}
		goto IL_03a0;
		IL_013f:
		if (text2 == "$y")
		{
			int num3 = index;
			while (index < text.Length && text[index] == ' ')
			{
				index++;
			}
			if (text[index] != '\'')
			{
				index = num3;
				return;
			}
			index++;
			syntax.Append(text.Substring(num3, index - num3).TrimEnd(' '));
			int num4 = index;
			int num5 = text.IndexOf(commandDelimiter, index);
			if (num5 == -1)
			{
				num5 = text.Length;
			}
			while (true)
			{
				int num6 = text.IndexOf('\'', num4 + 1);
				if (num6 == -1 || num6 > num5)
				{
					break;
				}
				num4 = num6;
			}
			if (num4 <= index)
			{
				return;
			}
			bool flag = false;
			while (index < num4 - 1)
			{
				char c = text[index];
				if (c == '_')
				{
					if (flag)
					{
						syntax.Append("text");
						flag = false;
					}
					syntax.Append(c);
				}
				else
				{
					flag = true;
				}
				index++;
			}
			if (flag)
			{
				syntax.Append("text");
			}
			index++;
			syntax.Append(text[index]);
			index++;
		}
		goto IL_03a0;
		IL_01a8:
		int num7 = index;
		while (index < text.Length && text[index] != '#')
		{
			index++;
		}
		index++;
		syntax.Append(text.Substring(num7, index - num7).TrimEnd(' '));
		int i;
		for (i = index; i < text.Length && text[i] != '#' && text[i] != '|'; i++)
		{
		}
		ExtractDialogueSyntaxImpl(text, commandDelimiter, ref index, syntax, i - 1);
		if (index < text.Length && text[index] == '|')
		{
			syntax.Append(text[index]);
			index++;
			int j;
			for (j = index; j < text.Length && text[j] != '#' && text[j] != '|'; j++)
			{
			}
			ExtractDialogueSyntaxImpl(text, commandDelimiter, ref index, syntax, j - 1);
			goto IL_03a0;
		}
		return;
		IL_03a0:
		index--;
	}

	/// <summary>As part of <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractDialogueSyntaxImpl(System.String,System.Char,System.Int32@,System.Text.StringBuilder,System.Int32)" />, read a syntax representation of an item spawn list like <c>[128 129]</c>.</summary>
	/// <param name="text">The dialogue or event text.</param>
	/// <param name="index">The index at which to read the next character. After this method runs, it will be set to the last character in the command.</param>
	/// <param name="syntax">The string builder to extend with the current command's syntax.</param>
	private void ExtractDialogueItemSpawnSyntax(string text, ref int index, StringBuilder syntax)
	{
		int num = index;
		int num2 = index;
		num2++;
		bool flag = false;
		for (; num2 < text.Length; num2++)
		{
			char c = text[num2];
			if (c != ' ' && c != '.' && !char.IsLetter(c) && !char.IsNumber(c))
			{
				if (c == ']')
				{
					flag = true;
				}
				break;
			}
		}
		if (flag)
		{
			syntax.Append(text.Substring(num, num2 - num + 1).TrimEnd(' '));
			index = num2;
		}
		else
		{
			syntax.Append(text[index]);
			index++;
		}
	}

	/// <summary>As part of <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractMailSyntax(System.String)" />, read a syntax representation of a single <c>%</c> mail command from the input string.</summary>
	/// <param name="text">The dialogue or event text.</param>
	/// <param name="index">The index at which to read the next character. After this method runs, it will be set to the last character in the command.</param>
	/// <param name="syntax">The string builder to extend with the current command's syntax.</param>
	private void ExtractMailCommandSyntax(string text, ref int index, StringBuilder syntax)
	{
		int num = index;
		index++;
		while (index < text.Length && (char.IsLetter(text[index]) || char.IsNumber(text[index])))
		{
			index++;
		}
		string text2 = text.Substring(num, index - num);
		if (!(text2 == "%item"))
		{
			if (text2 == "%revealtaste")
			{
				index -= "%revealtaste".Length;
				ExtractRevealTasteCommandSyntax(text, ref index, syntax);
			}
			else
			{
				syntax.Append(text2);
				index--;
			}
			return;
		}
		syntax.Append(text2);
		int num2 = index;
		while (index < text.Length)
		{
			index++;
			if (index > 1 && text[index] == '%' && text[index - 1] == '%')
			{
				break;
			}
		}
		string value = ((text[index] == '%' && text[index - 1] == '%' && char.IsWhiteSpace(text[index - 2])) ? (text.Substring(num2, index - num2 - 1).TrimEnd() + "%%") : text.Substring(num2, index - num2 + 1));
		syntax.Append(value);
	}

	/// <summary>As part of <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractMailSyntax(System.String)" /> or <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractCreditsSyntax(System.String)" />, read a syntax representation of a single <c>[...]</c> tag from the input string.</summary>
	/// <param name="text">The dialogue or event text.</param>
	/// <param name="index">The index at which to read the next character. After this method runs, it will be set to the last character in the command.</param>
	/// <param name="syntax">The string builder to extend with the current command's syntax.</param>
	private void ExtractTagSyntax(string text, ref int index, StringBuilder syntax)
	{
		int num = index;
		index++;
		while (index < text.Length - 1 && text[index] != ']')
		{
			index++;
		}
		syntax.Append(text.Substring(num, index - num + 1));
	}

	/// <summary>As part of <see cref="M:StardewValley.Tests.SyntaxAbstractor.ExtractMailSyntax(System.String)" />, read a syntax representation of a single <c>%</c> mail command from the input string.</summary>
	/// <param name="text">The dialogue or event text.</param>
	/// <param name="index">The index at which to read the next character. After this method runs, it will be set to the last character in the command.</param>
	/// <param name="syntax">The string builder to extend with the current command's syntax.</param>
	/// <remarks>Derived from <see cref="M:StardewValley.Utility.ParseGiftReveals(System.String)" />.</remarks>
	private void ExtractRevealTasteCommandSyntax(string text, ref int index, StringBuilder syntax)
	{
		int num = index;
		while (index < text.Length - 1)
		{
			char c = text[index + 1];
			if (char.IsWhiteSpace(c) || c == '#' || c == '%' || c == '$' || c == '{' || c == '^' || c == '*' || c == '[')
			{
				break;
			}
			index++;
		}
		syntax.Append(text.Substring(num, index - num + 1));
	}

	/// <summary>If we're in the text portion of a dialogue/data string, output a <c>text</c> token now and end the text portion.</summary>
	/// <param name="hasText">Whether we're in a text portion of the input string. This will be set to false.</param>
	/// <param name="syntax">The syntax string being compiled.</param>
	private void EndTextContext(ref bool hasText, StringBuilder syntax)
	{
		if (hasText)
		{
			syntax.Append("text");
			hasText = false;
		}
	}
}
