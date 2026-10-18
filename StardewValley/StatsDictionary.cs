using System;
using StardewValley.Extensions;

namespace StardewValley;

/// <summary>An implementation of <see cref="T:StardewValley.SerializableDictionary`2" /> specialized for storing <see cref="T:StardewValley.Stats" /> values.</summary>
/// <typeparam name="TValue">The numeric stat value type. This must be <see cref="T:System.Int32" />, <see cref="T:System.Int64" />, or <see cref="T:System.UInt32" />.</typeparam>
public class StatsDictionary<TValue> : SerializableDictionaryWithCaseInsensitiveKeys<TValue>
{
	/// <inheritdoc />
	protected override void AddDuringDeserialization(string key, TValue value)
	{
		if (!TryGetValue(key, out var value2))
		{
			base.AddDuringDeserialization(key, value);
			return;
		}
		long num = Convert.ToInt64(value);
		long num2 = Convert.ToInt64(value2);
		if (key.EqualsIgnoreCase("averageBedtime"))
		{
			if (num2 == 0L)
			{
				base[key] = value;
			}
		}
		else
		{
			base[key] = (TValue)Convert.ChangeType(num2 + num, typeof(TValue));
		}
	}
}
