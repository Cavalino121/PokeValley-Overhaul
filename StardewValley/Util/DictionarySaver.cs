using System;
using System.Collections.Generic;

namespace StardewValley.Util;

public static class DictionarySaver<TKey, TValue>
{
	/// <summary>Create an array of <see cref="T:StardewValley.Util.SaveablePair`2" /> from a dictionary.</summary>
	/// <param name="data">The data to copy.</param>
	public static SaveablePair<TKey, TValue>[] ArrayFrom(IDictionary<TKey, TValue> data)
	{
		SaveablePair<TKey, TValue>[] array = new SaveablePair<TKey, TValue>[data?.Count ?? 0];
		int num = 0;
		if (data != null)
		{
			foreach (KeyValuePair<TKey, TValue> datum in data)
			{
				array[num++] = new SaveablePair<TKey, TValue>(datum.Key, datum.Value);
			}
		}
		return array;
	}

	/// <summary>Create an array of <see cref="T:StardewValley.Util.SaveablePairExtensions" /> from a dictionary with a different value type.</summary>
	/// <typeparam name="TSourceValue">The value type in the source data to copy.</typeparam>
	/// <param name="data">The data to copy.</param>
	/// <param name="getValue">Get the value to use for an entry in the original data.</param>
	public static SaveablePair<TKey, TValue>[] ArrayFrom<TSourceValue>(IDictionary<TKey, TSourceValue> data, Func<TSourceValue, TValue> getValue)
	{
		SaveablePair<TKey, TValue>[] array = new SaveablePair<TKey, TValue>[data?.Count ?? 0];
		int num = 0;
		if (data != null)
		{
			foreach (KeyValuePair<TKey, TSourceValue> datum in data)
			{
				array[num++] = new SaveablePair<TKey, TValue>(datum.Key, getValue(datum.Value));
			}
		}
		return array;
	}

	/// <summary>Create an array of <see cref="T:StardewValley.Util.SaveablePair`2" /> from a dictionary with different key and value types.</summary>
	/// <typeparam name="TSourceKey">The key type in the source data to copy.</typeparam>
	/// <typeparam name="TSourceValue">The value type in the source data to copy.</typeparam>
	/// <param name="data">The data to copy.</param>
	/// <param name="getKey">Get the key to use for an entry in the original data.</param>
	/// <param name="getValue">Get the value to use for an entry in the original data.</param>
	public static SaveablePair<TKey, TValue>[] ArrayFrom<TSourceKey, TSourceValue>(IDictionary<TSourceKey, TSourceValue> data, Func<TSourceKey, TKey> getKey, Func<TSourceValue, TValue> getValue)
	{
		SaveablePair<TKey, TValue>[] array = new SaveablePair<TKey, TValue>[data?.Count ?? 0];
		int num = 0;
		if (data != null)
		{
			foreach (KeyValuePair<TSourceKey, TSourceValue> datum in data)
			{
				array[num++] = new SaveablePair<TKey, TValue>(getKey(datum.Key), getValue(datum.Value));
			}
		}
		return array;
	}
}
