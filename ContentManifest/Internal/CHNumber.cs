using System;
using System.Globalization;
using System.Text;

namespace ContentManifest.Internal;

internal class CHNumber : CHParsable
{
	private static StringBuilder DoubleSb;

	public double RawDouble;

	public static bool IsValidPrefix(char prefixChar)
	{
		if (prefixChar != '-')
		{
			if ('0' <= prefixChar)
			{
				return prefixChar <= '9';
			}
			return false;
		}
		return true;
	}

	public void Parse(CHJsonParserContext context)
	{
		EnsureStringBuilderInitialized();
		DoubleSb.Clear();
		if (context.JsonText[context.ReadHead] == '-')
		{
			context.ReadHead++;
			DoubleSb.Append('-');
		}
		context.AssertReadHeadIsValid();
		char c = context.JsonText[context.ReadHead];
		if (c == '0')
		{
			context.ReadHead++;
			if (context.ReadHead < context.JsonText.Length)
			{
				char c2 = context.JsonText[context.ReadHead];
				if ('1' <= c2 && c2 <= '9')
				{
					throw new InvalidOperationException();
				}
			}
			DoubleSb.Append('0');
		}
		else
		{
			if ('1' > c || c > '9')
			{
				throw new InvalidOperationException();
			}
			context.ReadHead++;
			DoubleSb.Append(c);
		}
		ParseDigits(context);
		if (context.ReadHead < context.JsonText.Length && context.JsonText[context.ReadHead] == '.')
		{
			context.ReadHead++;
			context.AssertReadHeadIsValid();
			DoubleSb.Append('.');
			ParseDigits(context);
		}
		if (context.ReadHead < context.JsonText.Length)
		{
			char c3 = context.JsonText[context.ReadHead];
			if (c3 == 'e' || c3 == 'E')
			{
				context.ReadHead++;
				context.AssertReadHeadIsValid();
				DoubleSb.Append('E');
				char c4 = context.JsonText[context.ReadHead];
				if (c4 == '-' || c4 == '+')
				{
					context.ReadHead++;
					context.AssertReadHeadIsValid();
					DoubleSb.Append(c4);
				}
				ParseDigits(context);
			}
		}
		RawDouble = double.Parse(DoubleSb.ToString(), CultureInfo.InvariantCulture);
	}

	private void ParseDigits(CHJsonParserContext context)
	{
		string jsonText = context.JsonText;
		int i;
		for (i = context.ReadHead; i < jsonText.Length; i++)
		{
			char c = jsonText[i];
			if (c < '0' || c > '9')
			{
				break;
			}
			DoubleSb.Append(c);
		}
		context.ReadHead = i;
	}

	private static void EnsureStringBuilderInitialized()
	{
		string text = Convert.ToString(long.MaxValue);
		DoubleSb = new StringBuilder("-".Length + text.Length + ".".Length + text.Length + "E".Length + "+".Length + text.Length);
	}
}
