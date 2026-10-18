using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace StardewValley;

/// <summary>A utility for working with space-delimited or split argument lists.</summary>
public static class ArgUtility
{
	/// <summary>Split space-separated arguments in a string, ignoring extra spaces.</summary>
	/// <param name="value">The value to split.</param>
	/// <returns>Returns an array of the space-delimited arguments, or an empty array if the <paramref name="value" /> was null, empty, or only contains spaces.</returns>
	/// <remarks>For example, this text: <code>A  B C</code> would be split into three values (<c>A</c>, <c>B</c>, and <c>C</c>). See also <see cref="M:StardewValley.ArgUtility.SplitBySpaceQuoteAware(System.String)" />.</remarks>
	public static string[] SplitBySpace(string value)
	{
		return value?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? LegacyShims.EmptyArray<string>();
	}

	/// <inheritdoc cref="M:StardewValley.ArgUtility.SplitBySpace(System.String)" />
	/// <param name="value">The value to split.</param>
	/// <param name="limit">The number of arguments to return. Any remaining arguments by appended to the final argument.</param>
	public static string[] SplitBySpace(string value, int limit)
	{
		return value?.Split(' ', limit, StringSplitOptions.RemoveEmptyEntries) ?? LegacyShims.EmptyArray<string>();
	}

	/// <summary>Split space-separated arguments in a string (ignoring extra spaces), and get a specific argument.</summary>
	/// <param name="value">The value to split.</param>
	/// <param name="index">The index of the value to get.</param>
	/// <param name="defaultValue">The value to return if the <paramref name="index" /> is out of range for the array.</param>
	/// <returns>Returns the value at the given index if the array was non-null and the index is in range, else the <paramref name="defaultValue" />.</returns>
	public static string SplitBySpaceAndGet(string value, int index, string defaultValue = null)
	{
		if (value == null)
		{
			return defaultValue;
		}
		return Get(value.Split(' ', index + 2, StringSplitOptions.RemoveEmptyEntries), index, defaultValue);
	}

	/// <summary>Split a list of space-separated arguments (ignoring extra spaces), with support for using quotes to protect spaces within an argument.</summary>
	/// <param name="input">The value to split.</param>
	/// <remarks>See remarks on <see cref="M:StardewValley.ArgUtility.SplitBySpaceQuoteAware(System.String)" /> for the quote format details.</remarks>
	public static string[] SplitBySpaceQuoteAware(string input)
	{
		return SplitQuoteAware(input, ' ', StringSplitOptions.RemoveEmptyEntries);
	}

	/// <summary>Split a list of arguments using the given delimiter, with support for using quotes to protect delimiters within an argument.</summary>
	/// <param name="input">The value to split.</param>
	/// <param name="delimiter">The character on which to split the value. This shouldn't be a quote (<c>"</c>) or backslash (<c>\</c>).</param>
	/// <param name="splitOptions">The string split options to apply for the delimiter split.</param>
	/// <param name="keepQuotesAndEscapes">Whether to keep quotes and escape characters in the string. For example, the value <c>Some \"test\" "here"</c> would become <c>Some "test" here</c> if this disabled, or kept as-is (aside from splitting) if it's enabled. This impacts performance and should usually be <c>false</c> unless you need to split each value further while respecting quotes.</param>
	/// <remarks>
	///   <para>A quote in the text causes any delimiter to be ignored until the next quote. The quotes are removed from the string. For example, this comma-delimited input: <code>"some,text",here</code> will produce two values: <c>some,text</c> and <c>here</c>.</para>
	///
	///   <para>A quote character can be escaped by preceding it with a backslash (like <c>\"</c>). Escaped quotes have no effect on delimiters, and aren't removed from the string. For example, this comma-delimited input: <code>some,\"text,here</code> will produce three values: <c>some</c>, <c>"text</c>, and <c>here</c>. Remember that backslashes need to be escaped in C# or JSON strings (e.g. <c>"\\"</c> produces a single backslash).</para>
	///
	///   <para>See also <see cref="M:StardewValley.ArgUtility.SplitBySpaceQuoteAware(System.String)" /> which simplifies usage for the most common case used by the game.</para>
	///
	///   <para>When an input *doesn't* contain quotes, this is optimized to be almost as fast as just calling <see cref="M:System.String.Split(System.Char,System.StringSplitOptions)" /> directly.</para>
	/// </remarks>
	public static string[] SplitQuoteAware(string input, char delimiter, StringSplitOptions splitOptions = StringSplitOptions.None, bool keepQuotesAndEscapes = false)
	{
		if (string.IsNullOrEmpty(input))
		{
			return LegacyShims.EmptyArray<string>();
		}
		if (!input.Contains('"'))
		{
			return input.Split(delimiter, splitOptions);
		}
		bool flag = false;
		if (splitOptions.HasFlag(StringSplitOptions.TrimEntries))
		{
			flag = true;
			splitOptions &= ~StringSplitOptions.TrimEntries;
		}
		bool flag2 = splitOptions.HasFlag(StringSplitOptions.RemoveEmptyEntries);
		string[] array = input.Split('"');
		List<string> list = new List<string>(array.Length * 4);
		bool flag3 = true;
		bool flag4 = true;
		string text = null;
		int i = 0;
		for (int num = array.Length - 1; i <= num; i++)
		{
			flag3 = !flag3;
			string text2 = array[i];
			bool flag5 = false;
			bool flag6 = false;
			bool flag7 = text2.EndsWith(delimiter);
			if (keepQuotesAndEscapes && i != 0)
			{
				text2 = "\"" + text2;
			}
			if (!flag4)
			{
				if (text.EndsWith('\\'))
				{
					text2 = (keepQuotesAndEscapes ? (text + text2) : (text.Substring(0, text.Length - 1) + "\"" + text2));
					flag3 = !flag3;
					flag5 = true;
				}
				else if (flag3 || !text2.StartsWith(delimiter))
				{
					flag6 = true;
				}
				else
				{
					text2 = text2.Substring(1);
				}
			}
			if (list.Count == 0)
			{
				flag5 = false;
				flag6 = false;
			}
			if (flag3)
			{
				flag7 = false;
				if (flag5)
				{
					list[list.Count - 1] = text2;
				}
				else if (flag6)
				{
					list[list.Count - 1] += text2;
					text2 = list[list.Count - 1];
				}
				else
				{
					list.Add(text2);
				}
				text = text2;
				flag4 = false;
				continue;
			}
			if (flag7 && !flag2 && i != num && text2.Length > 0)
			{
				text2 = text2.Substring(0, text2.Length - 1);
			}
			string[] array2 = text2.Split(delimiter, splitOptions);
			int num2 = array2.Length;
			if (num2 != 0)
			{
				if (num2 == 1 && flag7 && array2[0] == string.Empty)
				{
					text = string.Empty;
				}
				else
				{
					if (flag5)
					{
						list.RemoveAt(list.Count - 1);
						list.AddRange(array2);
					}
					else if (flag6)
					{
						list[list.Count - 1] += array2[0];
						if (array2.Length > 1)
						{
							list.AddRange(new ArraySegment<string>(array2, 1, array2.Length - 1));
						}
					}
					else
					{
						list.AddRange(array2);
					}
					text = array2[^1];
				}
			}
			else
			{
				text = string.Empty;
			}
			flag4 = flag7;
		}
		if (flag)
		{
			for (int num3 = list.Count - 1; num3 >= 0; num3--)
			{
				list[num3] = list[num3].Trim();
				if (flag2 && list[num3].Length == 0)
				{
					list.RemoveAt(num3);
				}
			}
		}
		return list.ToArray();
	}

	/// <summary>Reverse a <see cref="M:StardewValley.ArgUtility.SplitQuoteAware(System.String,System.Char,System.StringSplitOptions,System.Boolean)" /> operation, producing a string which can safely be re-split.</summary>
	/// <param name="input">The values to split.</param>
	/// <param name="delimiter">The character on which to split the value. This shouldn't be a quote (<c>"</c>) or backslash (<c>\</c>).</param>
	/// <param name="startAt">The minimum index in <paramref name="input" /> to include in the resulting string.</param>
	/// <param name="count">The maximum number of arguments from <paramref name="input" /> to include in the resulting string.</param>
	public static string UnsplitQuoteAware(string[] input, char delimiter, int startAt = 0, int count = int.MaxValue)
	{
		if (startAt < 0)
		{
			throw new ArgumentException("Can't start unsplitting before the bounds of the array.", "startAt");
		}
		if (input == null || count == 0 || startAt >= input.Length)
		{
			return string.Empty;
		}
		count = Math.Min(count, input.Length - startAt);
		string[] array = new string[count];
		int i = startAt;
		for (int num = startAt + count - 1; i <= num; i++)
		{
			string text = input[i];
			if (text.Contains('"'))
			{
				text = EscapeQuotes(text);
			}
			if (text.Contains(delimiter))
			{
				text = "\"" + text + "\"";
			}
			array[i - startAt] = text;
		}
		return string.Join(delimiter, array);
	}

	/// <summary>Escape quotes in a string so they're ignored by methods like <see cref="M:StardewValley.ArgUtility.SplitQuoteAware(System.String,System.Char,System.StringSplitOptions,System.Boolean)" />.</summary>
	/// <param name="input">The input string to escape.</param>
	/// <remarks>This isn't idempotent (e.g. calling it twice will result in double-escaped quotes).</remarks>
	public static string EscapeQuotes(string input)
	{
		return input.Replace("\"", "\\\"");
	}

	/// <summary>Get whether an index is within the bounds of the array, regardless of what value is at that position.</summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="array">The array of arguments to check.</param>
	/// <param name="index">The index to check within the <paramref name="array" />.</param>
	public static bool HasIndex<T>(T[] array, int index)
	{
		if (index >= 0)
		{
			if (array == null)
			{
				return false;
			}
			return array.Length > index;
		}
		return false;
	}

	/// <summary>Get a subset of the given array.</summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="array">The array of arguments to get a subset of.</param>
	/// <param name="startAt">The index at which to start copying values.</param>
	/// <param name="length">The number of values to copy.</param>
	/// <exception cref="T:System.ArgumentException">The <paramref name="startAt" /> is before the start of the array.</exception>
	public static T[] GetSubsetOf<T>(T[] array, int startAt, int length = -1)
	{
		if (startAt < 0)
		{
			throw new ArgumentException("Can't start copying before the bounds of the array.", "startAt");
		}
		if (array == null || length == 0 || startAt > array.Length - 1)
		{
			return LegacyShims.EmptyArray<T>();
		}
		if (startAt == 0 && (length == -1 || length == array.Length))
		{
			return array.ToArray();
		}
		if (length < 0)
		{
			length = array.Length - startAt;
		}
		T[] array2 = new T[length];
		Array.Copy(array, startAt, array2, 0, length);
		return array2;
	}

	/// <summary>Get a string argument by its array index.</summary>
	/// <param name="array">The array of arguments to read.</param>
	/// <param name="index">The index to get within the <paramref name="array" />.</param>
	/// <param name="defaultValue">The value to return if the index is out of bounds or invalid.</param>
	/// <param name="allowBlank">Whether to return the argument even if it's null or whitespace. If false, the <paramref name="defaultValue" /> will be returned in that case.</param>
	/// <returns>Returns the selected argument (if the <paramref name="index" /> is found and valid), else <paramref name="defaultValue" />.</returns>
	public static string Get(string[] array, int index, string defaultValue = null, bool allowBlank = true)
	{
		if (index >= 0 && index < array?.Length)
		{
			string text = array[index];
			if (allowBlank || !string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return defaultValue;
	}

	/// <summary>Get a string argument by its array index, if it's found and valid.</summary>
	/// <param name="array">The array of arguments to read.</param>
	/// <param name="index">The index to get within the <paramref name="array" />.</param>
	/// <param name="value">The argument value, if found and valid.</param>
	/// <param name="error">An error phrase indicating why getting the argument failed (like 'required index X not found'), if applicable.</param>
	/// <param name="allowBlank">Whether to match the argument even if it's null or whitespace. If false, it will be treated as invalid in that case.</param>
	/// <param name="name">A readable name for the argument, used to format the <paramref name="error" />. This can be omitted to auto-generate it from the expression passed to <paramref name="value" />.</param>
	/// <returns>Returns whether the argument was successfully found and is valid.</returns>
	public static bool TryGet(string[] array, int index, out string value, out string error, bool allowBlank = true, [CallerArgumentExpression("value")] string name = null)
	{
		if (array == null)
		{
			value = null;
			error = "argument list is null";
			return false;
		}
		if (index < 0 || index >= array.Length)
		{
			value = null;
			error = GetMissingRequiredIndexError(array, index, name);
			return false;
		}
		value = array[index];
		if (!allowBlank && string.IsNullOrWhiteSpace(value))
		{
			value = null;
			error = "required " + GetFieldLabel(index, name) + " has a blank value";
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get a string argument by its array index, or a default value if the argument isn't found.</summary>
	/// <param name="array">The array of arguments to read.</param>
	/// <param name="index">The index to get within the <paramref name="array" />.</param>
	/// <param name="value">The argument value, if found and valid.</param>
	/// <param name="error">An error phrase indicating why getting the argument failed (like 'required index X not found'), if applicable.</param>
	/// <param name="defaultValue">The value to return if the index is out of bounds or invalid.</param>
	/// <param name="allowBlank">Whether to match the argument even if it's null or whitespace. If false, it will be treated as missing in that case.</param>
	/// <param name="name">A readable name for the argument being parsed, used to format the <paramref name="error" />. This can be omitted to auto-generate it from the expression passed to <paramref name="value" />.</param>
	/// <returns>Returns true if either (a) the argument was found and valid, or (b) the argument was not found so the default value was used. Returns false if the argument was found but isn't in a valid format.</returns>
	public static bool TryGetOptional(string[] array, int index, out string value, out string error, string defaultValue = null, bool allowBlank = true, [CallerArgumentExpression("value")] string name = null)
	{
		if (array == null || index < 0 || index >= array.Length || (!allowBlank && array[index] == string.Empty))
		{
			value = defaultValue;
			error = null;
			return true;
		}
		value = array[index];
		if (!allowBlank && string.IsNullOrWhiteSpace(value))
		{
			value = defaultValue;
			error = "optional " + GetFieldLabel(index, name) + " can't have a blank value";
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get an boolean argument by its array index.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.Get(System.String[],System.Int32,System.String,System.Boolean)" />
	public static bool GetBool(string[] array, int index, bool defaultValue = false)
	{
		if (!bool.TryParse(Get(array, index), out var result))
		{
			return defaultValue;
		}
		return result;
	}

	/// <summary>Get a boolean argument by its array index, if it's found and valid.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGet(System.String[],System.Int32,System.String@,System.String@,System.Boolean,System.String)" />
	public static bool TryGetBool(string[] array, int index, out bool value, out string error, [CallerArgumentExpression("value")] string name = null)
	{
		if (!TryGet(array, index, out var value2, out error, allowBlank: false, name))
		{
			value = false;
			return false;
		}
		if (!bool.TryParse(value2, out value))
		{
			value = false;
			error = GetValueParseError(array, index, name, required: true, "a boolean (should be 'true' or 'false')");
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get a boolean argument by its array index, or a default value if the argument isn't found.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGetOptional(System.String[],System.Int32,System.String@,System.String@,System.String,System.Boolean,System.String)" />
	public static bool TryGetOptionalBool(string[] array, int index, out bool value, out string error, bool defaultValue = false, [CallerArgumentExpression("value")] string name = null)
	{
		if (array == null || index < 0 || index >= array.Length || array[index] == string.Empty)
		{
			error = null;
			value = defaultValue;
			return true;
		}
		if (!bool.TryParse(array[index], out value))
		{
			error = GetValueParseError(array, index, name, required: false, "a boolean");
			value = defaultValue;
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get a direction argument by its array index.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.Get(System.String[],System.Int32,System.String,System.Boolean)" />
	public static int GetDirection(string[] array, int index, int defaultValue = 0)
	{
		if (!Utility.TryParseDirection(Get(array, index), out var parsed))
		{
			return defaultValue;
		}
		return parsed;
	}

	/// <summary>Get a direction argument by its array index, if it's found and valid.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGet(System.String[],System.Int32,System.String@,System.String@,System.Boolean,System.String)" />
	public static bool TryGetDirection(string[] array, int index, out int value, out string error, [CallerArgumentExpression("value")] string name = null)
	{
		if (!TryGet(array, index, out var value2, out error, allowBlank: false, name))
		{
			value = 0;
			return false;
		}
		if (!Utility.TryParseDirection(value2, out value))
		{
			value = 0;
			error = GetValueParseError(array, index, name, required: true, "a direction (should be 'up', 'down', 'left', or 'right')");
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get a direction argument by its array index, or a default value if the argument isn't found.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGetOptional(System.String[],System.Int32,System.String@,System.String@,System.String,System.Boolean,System.String)" />
	public static bool TryGetOptionalDirection(string[] array, int index, out int value, out string error, int defaultValue = 0, [CallerArgumentExpression("value")] string name = null)
	{
		if (array == null || index < 0 || index >= array.Length || array[index] == string.Empty)
		{
			error = null;
			value = defaultValue;
			return true;
		}
		if (!Utility.TryParseDirection(array[index], out value))
		{
			error = GetValueParseError(array, index, name, required: true, "a direction (should be one of 'up', 'down', 'left', or 'right')");
			value = defaultValue;
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get an enum argument by its array index.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.Get(System.String[],System.Int32,System.String,System.Boolean)" />
	public static TEnum GetEnum<TEnum>(string[] array, int index, TEnum defaultValue = default(TEnum)) where TEnum : struct
	{
		if (!Utility.TryParseEnum<TEnum>(Get(array, index), out var parsed))
		{
			return defaultValue;
		}
		return parsed;
	}

	/// <summary>Get an enum argument by its array index, if it's found and valid.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGet(System.String[],System.Int32,System.String@,System.String@,System.Boolean,System.String)" />
	public static bool TryGetEnum<TEnum>(string[] array, int index, out TEnum value, out string error, [CallerArgumentExpression("value")] string name = null) where TEnum : struct
	{
		if (!TryGet(array, index, out var value2, out error, allowBlank: false, name))
		{
			value = default(TEnum);
			return false;
		}
		if (!Utility.TryParseEnum<TEnum>(value2, out value))
		{
			Type typeFromHandle = typeof(TEnum);
			value = default(TEnum);
			error = GetValueParseError(array, index, name, required: true, $"an enum of type '{typeFromHandle.FullName ?? typeFromHandle.Name}' (should be one of {string.Join(", ", Enum.GetNames(typeof(TEnum)))})");
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get an enum argument by its array index, or a default value if the argument isn't found.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGetOptional(System.String[],System.Int32,System.String@,System.String@,System.String,System.Boolean,System.String)" />
	public static bool TryGetOptionalEnum<TEnum>(string[] array, int index, out TEnum value, out string error, TEnum defaultValue = default(TEnum), [CallerArgumentExpression("value")] string name = null) where TEnum : struct
	{
		if (array == null || index < 0 || index >= array.Length || array[index] == string.Empty)
		{
			error = null;
			value = defaultValue;
			return true;
		}
		if (!Utility.TryParseEnum<TEnum>(array[index], out value))
		{
			Type typeFromHandle = typeof(TEnum);
			error = GetValueParseError(array, index, name, required: false, "an enum of type '" + (typeFromHandle.FullName ?? typeFromHandle.Name) + "'");
			value = defaultValue;
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get a float argument by its array index.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.Get(System.String[],System.Int32,System.String,System.Boolean)" />
	public static float GetFloat(string[] array, int index, float defaultValue = 0f)
	{
		if (!float.TryParse(Get(array, index), out var result))
		{
			return defaultValue;
		}
		return result;
	}

	/// <summary>Get a float argument by its array index, if it's found and valid.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGet(System.String[],System.Int32,System.String@,System.String@,System.Boolean,System.String)" />
	public static bool TryGetFloat(string[] array, int index, out float value, out string error, [CallerArgumentExpression("value")] string name = null)
	{
		if (!TryGet(array, index, out var value2, out error, allowBlank: false, name))
		{
			value = 0f;
			return false;
		}
		if (!float.TryParse(value2, out value))
		{
			value = 0f;
			error = GetValueParseError(array, index, name, required: true, "a number");
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get a float argument by its array index, or a default value if the argument isn't found.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGetOptional(System.String[],System.Int32,System.String@,System.String@,System.String,System.Boolean,System.String)" />
	public static bool TryGetOptionalFloat(string[] array, int index, out float value, out string error, float defaultValue = 0f, [CallerArgumentExpression("value")] string name = null)
	{
		if (array == null || index < 0 || index >= array.Length || array[index] == string.Empty)
		{
			error = null;
			value = defaultValue;
			return true;
		}
		if (!float.TryParse(array[index], out value))
		{
			error = GetValueParseError(array, index, name, required: false, "a float");
			value = defaultValue;
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get an integer argument by its array index.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.Get(System.String[],System.Int32,System.String,System.Boolean)" />
	public static int GetInt(string[] array, int index, int defaultValue = 0)
	{
		if (!int.TryParse(Get(array, index), out var result))
		{
			return defaultValue;
		}
		return result;
	}

	/// <summary>Get an integer argument by its array index, if it's found and valid.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGet(System.String[],System.Int32,System.String@,System.String@,System.Boolean,System.String)" />
	public static bool TryGetInt(string[] array, int index, out int value, out string error, [CallerArgumentExpression("value")] string name = null)
	{
		if (!TryGet(array, index, out var value2, out error, allowBlank: false, name))
		{
			value = 0;
			return false;
		}
		if (!int.TryParse(value2, out value))
		{
			value = 0;
			error = GetValueParseError(array, index, name, required: true, "an integer");
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get an int argument by its array index, or a default value if the argument isn't found.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGetOptional(System.String[],System.Int32,System.String@,System.String@,System.String,System.Boolean,System.String)" />
	public static bool TryGetOptionalInt(string[] array, int index, out int value, out string error, int defaultValue = 0, [CallerArgumentExpression("value")] string name = null)
	{
		if (array == null || index < 0 || index >= array.Length || array[index] == string.Empty)
		{
			error = null;
			value = defaultValue;
			return true;
		}
		if (!int.TryParse(array[index], out value))
		{
			error = GetValueParseError(array, index, name, required: false, "an integer");
			value = defaultValue;
			return false;
		}
		error = null;
		return true;
	}

	/// <summary>Get a point argument by its array index, if it's found and valid. This reads two consecutive values starting from <paramref name="index" /> for the X and Y values.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGet(System.String[],System.Int32,System.String@,System.String@,System.Boolean,System.String)" />
	public static bool TryGetPoint(string[] array, int index, out Point value, out string error, [CallerArgumentExpression("value")] string name = null)
	{
		if (!TryGetInt(array, index, out var value2, out error, (name != null) ? (name + " > x") : null) || !TryGetInt(array, index + 1, out var value3, out error, (name != null) ? (name + " > y") : null))
		{
			value = Point.Zero;
			return false;
		}
		error = null;
		value = new Point(value2, value3);
		return true;
	}

	/// <summary>Get a rectangle argument by its array index, if it's found and valid. This reads four consecutive values starting from <paramref name="index" /> for the X, Y, width, and height values.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.TryGet(System.String[],System.Int32,System.String@,System.String@,System.Boolean,System.String)" />
	public static bool TryGetRectangle(string[] array, int index, out Rectangle value, out string error, [CallerArgumentExpression("value")] string name = null)
	{
		if (!TryGetInt(array, index, out var value2, out error, (name != null) ? (name + " > x") : null) || !TryGetInt(array, index + 1, out var value3, out error, (name != null) ? (name + " > y") : null) || !TryGetInt(array, index + 2, out var value4, out error, (name != null) ? (name + " > width") : null) || !TryGetInt(array, index + 3, out var value5, out error, (name != null) ? (name + " > height") : null))
		{
			value = Rectangle.Empty;
			return false;
		}
		error = null;
		value = new Rectangle(value2, value3, value4, value5);
		return true;
	}

	/// <summary>Get a vector argument by its array index, if it's found and valid. This reads two consecutive values starting from <paramref name="index" /> for the X and Y values.</summary>
	/// <param name="array">The array of arguments to read.</param>
	/// <param name="index">The index to get within the <paramref name="array" />.</param>
	/// <param name="value">The argument value, if found and valid.</param>
	/// <param name="error">An error phrase indicating why getting the argument failed (like 'required index X not found'), if applicable.</param>
	/// <param name="integerOnly">Whether the X and Y values must be integers.</param>
	/// <param name="name">A readable name for the argument being parsed, used to format the <paramref name="error" />. This can be omitted to auto-generate it from the expression passed to <paramref name="value" />.</param>
	/// <returns>Returns whether the argument was successfully found and is valid.</returns>
	public static bool TryGetVector2(string[] array, int index, out Vector2 value, out string error, bool integerOnly = false, [CallerArgumentExpression("value")] string name = null)
	{
		string name2 = ((name != null) ? (name + " > x") : null);
		string name3 = ((name != null) ? (name + " > y") : null);
		float value4;
		float value5;
		if (integerOnly)
		{
			if (TryGetInt(array, index, out var value2, out error, name2) && TryGetInt(array, index + 1, out var value3, out error, name3))
			{
				value = new Vector2(value2, value3);
				return true;
			}
		}
		else if (TryGetFloat(array, index, out value4, out error, name2) && TryGetFloat(array, index + 1, out value5, out error, name3))
		{
			value = new Vector2(value4, value5);
			return true;
		}
		value = Vector2.Zero;
		return false;
	}

	/// <summary>Get all arguments from the given index as a concatenated string.</summary>
	/// <inheritdoc cref="M:StardewValley.ArgUtility.Get(System.String[],System.Int32,System.String,System.Boolean)" />
	public static string GetRemainder(string[] array, int index, string defaultValue = null, char delimiter = ' ')
	{
		if (array == null || index < 0 || index >= array.Length)
		{
			return defaultValue;
		}
		if (array.Length - index == 1)
		{
			return array[index];
		}
		return string.Join(delimiter, array[index..]);
	}

	/// <summary>Get all arguments starting from the given index as a concatenated string, if the index is found.</summary>
	/// <param name="array">The array of arguments to read.</param>
	/// <param name="index">The index of the first argument to include within the <paramref name="array" />.</param>
	/// <param name="value">The concatenated argument values, if found and valid.</param>
	/// <param name="error">An error phrase indicating why getting the argument failed (like 'required index X not found'), if applicable.</param>
	/// <param name="delimiter">The delimiter with which to concatenate values.</param>
	/// <param name="name">A readable name for the argument being parsed, used to format the <paramref name="error" />. This can be omitted to auto-generate it from the expression passed to <paramref name="value" />.</param>
	/// <returns>Returns whether at least one argument was successfully found.</returns>
	public static bool TryGetRemainder(string[] array, int index, out string value, out string error, char delimiter = ' ', [CallerArgumentExpression("value")] string name = null)
	{
		if (array == null)
		{
			value = null;
			error = "argument list is null";
			return false;
		}
		if (index < 0 || index >= array.Length)
		{
			value = null;
			error = GetMissingRequiredIndexError(array, index, name);
			return false;
		}
		if (array.Length - index == 1)
		{
			value = array[index];
		}
		else
		{
			value = string.Join(delimiter, array[index..]);
		}
		error = null;
		return true;
	}

	/// <summary>Get all arguments starting from the given index as a concatenated string, or a default value if the index isn't in the array.</summary>
	/// <param name="array">The array of arguments to read.</param>
	/// <param name="index">The index of the first argument to include within the <paramref name="array" />.</param>
	/// <param name="value">The concatenated argument values, if found and valid.</param>
	/// <param name="defaultValue">The value to return if the index is out of bounds or invalid.</param>
	/// <param name="delimiter">The delimiter with which to concatenate values.</param>
	/// <returns>Returns true.</returns>
	public static bool TryGetOptionalRemainder(string[] array, int index, out string value, string defaultValue = null, char delimiter = ' ')
	{
		if (array == null || index < 0 || index >= array.Length)
		{
			value = defaultValue;
			return true;
		}
		if (array.Length - index == 1)
		{
			value = array[index];
		}
		else
		{
			value = string.Join(delimiter, array[index..]);
		}
		return true;
	}

	/// <summary>Get an error message indicating that an array doesn't contain a required index.</summary>
	/// <param name="array">The array being indexed.</param>
	/// <param name="index">The index in the array being searched for.</param>
	/// <param name="name">The readable name for the argument being parsed, if available.</param>
	internal static string GetMissingRequiredIndexError(string[] array, int index, string name)
	{
		return array.Length switch
		{
			0 => "required " + GetFieldLabel(index, name) + " not found (list is empty)", 
			1 => "required " + GetFieldLabel(index, name) + " not found (list has a single value at index 0)", 
			_ => $"required {GetFieldLabel(index, name)} not found (list has indexes 0 through {array.Length - 1})", 
		};
	}

	/// <summary>Get an error message indicating that an array index contains a value that can't be parsed.</summary>
	/// <param name="array">The array being indexed.</param>
	/// <param name="index">The index in the array being parsed.</param>
	/// <param name="required">Whether the argument is required.</param>
	/// <param name="typeSummary">A brief summary of the type being parsed, like "a boolean (one of 'true' or 'false')".</param>
	/// <param name="name">The readable name for the argument being parsed, if available.</param>
	internal static string GetValueParseError(string[] array, int index, string name, bool required, string typeSummary)
	{
		return $"{(required ? "required" : "optional")} {GetFieldLabel(index, name)} has value '{array[index]}', which can't be parsed as {typeSummary}";
	}

	/// <summary>Get the field label to show in errors.</summary>
	/// <param name="index">The field index.</param>
	/// <param name="name">The readable name for the argument being parsed, if available.</param>
	private static string GetFieldLabel(int index, string name)
	{
		if (name != null)
		{
			return $"index {index} ({name})";
		}
		return $"index {index}";
	}
}
