using System;
using System.Text;

namespace ContentManifest.Internal;

internal class CHString : CHParsable
{
	public string RawString = "";

	public void Parse(CHJsonParserContext context)
	{
		if (context.JsonText[context.ReadHead] != '"')
		{
			throw new InvalidOperationException();
		}
		context.ReadHead++;
		int i = context.ReadHead;
		string jsonText = context.JsonText;
		StringBuilder stringBuilder = new StringBuilder();
		for (; i < jsonText.Length; i++)
		{
			char c = jsonText[i];
			switch (c)
			{
			case '"':
				RawString = stringBuilder.ToString();
				context.ReadHead = i + 1;
				return;
			case '\\':
			{
				i++;
				if (i >= jsonText.Length)
				{
					throw new InvalidOperationException();
				}
				char c2 = jsonText[i];
				switch (c2)
				{
				case '"':
				case '/':
				case '\\':
					stringBuilder.Append(c2);
					break;
				case 'b':
					stringBuilder.Append('\b');
					break;
				case 'f':
					stringBuilder.Append('\f');
					break;
				case 'r':
					stringBuilder.Append('\r');
					break;
				case 'n':
					stringBuilder.Append('\n');
					break;
				case 't':
					stringBuilder.Append('\t');
					break;
				case 'u':
				{
					if (i + 4 >= jsonText.Length)
					{
						throw new InvalidOperationException();
					}
					string text = char.ConvertFromUtf32(0 | ((ParseHexChar(jsonText[i + 1]) & 0xF) << 12) | ((ParseHexChar(jsonText[i + 2]) & 0xF) << 8) | ((ParseHexChar(jsonText[i + 3]) & 0xF) << 4) | (ParseHexChar(jsonText[i + 4]) & 0xF));
					if (text.Length != 1)
					{
						throw new InvalidOperationException();
					}
					stringBuilder.Append(text[0]);
					i += 4;
					break;
				}
				}
				break;
			}
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		throw new InvalidOperationException();
	}

	private int ParseHexChar(char hexChar)
	{
		if ('0' <= hexChar && hexChar < '9')
		{
			return hexChar - 48;
		}
		if ('a' <= hexChar && hexChar <= 'z')
		{
			return hexChar - 97 + 10;
		}
		if ('A' <= hexChar && hexChar <= 'Z')
		{
			return hexChar - 65 + 10;
		}
		throw new InvalidOperationException();
	}
}
