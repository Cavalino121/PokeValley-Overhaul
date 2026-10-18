using System;

namespace StardewValley.SDKs.GogGalaxy.Internal;

public class Base36
{
	private const string Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	private const ulong Base = 36uL;

	public static string Encode(ulong value)
	{
		string text = "";
		if (value == 0L)
		{
			return "0";
		}
		while (value != 0L)
		{
			int index = (int)(value % 36);
			value /= 36;
			text = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[index] + text;
		}
		return text;
	}

	public static ulong Decode(string value)
	{
		value = value.ToUpper();
		ulong num = 0uL;
		string text = value;
		foreach (char value2 in text)
		{
			num *= 36;
			int num2 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(value2);
			if (num2 == -1)
			{
				throw new FormatException(value);
			}
			num += (ulong)num2;
		}
		return num;
	}
}
