using System;
using System.Collections.Generic;

namespace ContentManifest.Internal;

internal class CHObject : CHParsable
{
	public readonly Dictionary<string, object> Members = new Dictionary<string, object>();

	public void Parse(CHJsonParserContext context)
	{
		if (context.JsonText[context.ReadHead] != '{')
		{
			throw new InvalidOperationException();
		}
		context.ReadHead++;
		bool flag = false;
		while (true)
		{
			context.SkipWhitespace();
			context.AssertReadHeadIsValid();
			switch (context.JsonText[context.ReadHead])
			{
			case '}':
				if (flag)
				{
					throw new InvalidOperationException();
				}
				context.ReadHead++;
				return;
			case '"':
			{
				CHString cHString = new CHString();
				cHString.Parse(context);
				context.SkipWhitespace();
				context.AssertReadHeadIsValid();
				if (context.JsonText[context.ReadHead] != ':')
				{
					throw new InvalidOperationException();
				}
				context.ReadHead++;
				CHElement cHElement = new CHElement();
				cHElement.Parse(context);
				Members[cHString.RawString] = cHElement.Value.GetManagedObject();
				flag = false;
				context.SkipWhitespace();
				context.AssertReadHeadIsValid();
				if (context.JsonText[context.ReadHead] == ',')
				{
					context.ReadHead++;
					flag = true;
				}
				break;
			}
			default:
				throw new InvalidOperationException();
			}
		}
	}
}
