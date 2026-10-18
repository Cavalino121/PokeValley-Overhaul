using System;
using System.Collections.Generic;

namespace StardewValley.Tests;

/// <summary>Provides methods to compare and validate translations, used in the game's internal unit tests.</summary>
public class TranslationValidator
{
	/// <summary>Converts raw text into language-independent syntax representations, which can be compared between languages.</summary>
	private readonly SyntaxAbstractor Abstractor = new SyntaxAbstractor();

	/// <summary>Compare the base and translated variants of an asset and return a list of keys which are missing, unknown, or have a different syntax.</summary>
	/// <typeparam name="TValue">The value type in the asset data.</typeparam>
	/// <param name="baseData">The original untranslated data.</param>
	/// <param name="translatedData">The translated data.</param>
	/// <param name="getText">Get the text to compare for an entry.</param>
	/// <param name="baseAssetName">The asset name without the locale suffix, like <c>Data/Achievements</c>.</param>
	public IEnumerable<TranslationValidatorResult> Compare<TValue>(Dictionary<string, TValue> baseData, Dictionary<string, TValue> translatedData, Func<TValue, string> getText, string baseAssetName)
	{
		return Compare(baseData, translatedData, getText, (string key, string text) => Abstractor.ExtractSyntaxFor(baseAssetName, key, text));
	}

	/// <summary>Compare the base and translated variants of an asset and return a list of keys which are missing, unknown, or have a different syntax.</summary>
	/// <typeparam name="TValue">The value type in the asset data.</typeparam>
	/// <param name="baseData">The original untranslated data.</param>
	/// <param name="translatedData">The translated data.</param>
	/// <param name="getText">Get the text to compare for an entry.</param>
	/// <param name="getSyntax">Get the syntax for a data entry, given its key and value.</param>
	public IEnumerable<TranslationValidatorResult> Compare<TValue>(Dictionary<string, TValue> baseData, Dictionary<string, TValue> translatedData, Func<TValue, string> getText, Func<string, string, string> getSyntax)
	{
		foreach (KeyValuePair<string, TValue> baseDatum in baseData)
		{
			string key = baseDatum.Key;
			string text = getText(baseDatum.Value);
			if (!translatedData.TryGetValue(key, out var value))
			{
				yield return new TranslationValidatorResult(TranslationValidatorIssue.MissingKey, key, getSyntax(key, text), text, null, null, "Key not found in the translated asset.");
				continue;
			}
			string translationText = getText(value);
			TranslationValidatorResult translationValidatorResult = CompareEntry(key, text, translationText, (string arg) => getSyntax(key, arg));
			if (translationValidatorResult != null)
			{
				yield return translationValidatorResult;
			}
		}
		foreach (KeyValuePair<string, TValue> translatedDatum in translatedData)
		{
			string key2 = translatedDatum.Key;
			if (!baseData.ContainsKey(key2))
			{
				string text2 = getText(translatedDatum.Value);
				string translationSyntax = getSyntax(key2, text2);
				yield return new TranslationValidatorResult(TranslationValidatorIssue.UnknownKey, key2, null, null, translationSyntax, text2, "Unknown key in translation which isn't in the base asset.");
			}
		}
	}

	/// <summary>Compare the base and translated variants of a single entry in an asset and return a result if the entries have a different syntax.</summary>
	/// <param name="key">The key for this entry in the asset.</param>
	/// <param name="baseText">The original untranslated text.</param>
	/// <param name="translationText">The translated text.</param>
	/// <param name="getSyntax">Get the syntax for an entry, given its value.</param>
	/// <returns>Returns the validator result if an issue was found, else <c>null</c>.</returns>
	public TranslationValidatorResult CompareEntry(string key, string baseText, string translationText, Func<string, string> getSyntax)
	{
		string text = getSyntax(baseText);
		string text2 = getSyntax(translationText);
		if (text != text2)
		{
			return new TranslationValidatorResult(TranslationValidatorIssue.SyntaxMismatch, key, text, baseText, text2, translationText, $"The translation has a different syntax than the base text.\nSyntax:\n    base:  {text}\n    local: {text2}\n           {"".PadRight(GetDiffIndex(text, text2), ' ')}^\nText:\n    base:  {baseText}\n    local: {translationText}\n\n           {"".PadRight(GetDiffIndex(baseText, translationText), ' ')}^\n");
		}
		if (!ValidateGenderSwitchBlocks(baseText, out var error, out var errorBlock))
		{
			return new TranslationValidatorResult(TranslationValidatorIssue.MalformedSyntax, key, text, baseText, text2, translationText, $"Base text has invalid gender switch block: {error}.\nAffected block: {errorBlock}.");
		}
		if (!ValidateGenderSwitchBlocks(baseText, out error, out errorBlock))
		{
			return new TranslationValidatorResult(TranslationValidatorIssue.MalformedSyntax, key, text, baseText, text2, translationText, $"Translated text has invalid gender switch block: {error}.\nAffected block: {errorBlock}.");
		}
		return null;
	}

	/// <summary>Validate that all gender-switch blocks in a given text are correctly formatted.</summary>
	/// <param name="text">The text which may contain gender-switch blocks to validate.</param>
	/// <param name="error">If applicable, a human-readable phrase indicating why the gender-switch blocks are invalid.</param>
	/// <param name="errorBlock">The gender-switch block which is invalid.</param>
	public bool ValidateGenderSwitchBlocks(string text, out string error, out string errorBlock)
	{
		int startIndex = 0;
		while (true)
		{
			int num = text.IndexOf("${", startIndex, StringComparison.OrdinalIgnoreCase);
			if (num == -1)
			{
				break;
			}
			int num2 = text.IndexOf("}$", num, StringComparison.OrdinalIgnoreCase);
			if (num2 == -1)
			{
				error = "closing '}$' not found";
				errorBlock = text.Substring(num);
				return false;
			}
			errorBlock = text.Substring(num, num2 - num);
			string text2 = text.Substring(num + 2, num2 - num - 2);
			char c = (text2.Contains('^') ? '^' : '¦');
			string[] array = text2.Split(c);
			if (text2.Contains("${"))
			{
				error = "can't start a new gender-switch block inside another";
				return false;
			}
			if (array.Length < 2)
			{
				error = $"must have at least two branches delimited by {94} or {166}";
				return false;
			}
			if (array.Length > 3)
			{
				error = $"found {array.Length} branches delimited by {c}, must be two (male{c}female) or three (male{c}female{c}other)";
				return false;
			}
			string text3 = Abstractor.ExtractDialogueSyntax(array[0]);
			for (int i = 1; i < array.Length; i++)
			{
				string text4 = Abstractor.ExtractDialogueSyntax(array[1]);
				if (text3 != text4)
				{
					error = $"branches have different syntax (0: `{text3}`, {i}: `{text4}`)";
					return false;
				}
			}
			startIndex = num2 + 2;
		}
		error = null;
		errorBlock = null;
		return true;
	}

	/// <summary>Get the index at which two strings first differ.</summary>
	/// <param name="baseText">The base text being compare to.</param>
	/// <param name="translatedText">The translated text to compare with the base text.</param>
	public int GetDiffIndex(string baseText, string translatedText)
	{
		int num = Math.Min(baseText.Length, translatedText.Length);
		for (int i = 0; i < num; i++)
		{
			if (baseText[i] != translatedText[i])
			{
				return i;
			}
		}
		return num;
	}
}
