using System;
using System.Collections.Generic;

namespace ContentManifest.Internal;

internal class CHArray : CHParsable
{
	private static readonly List<object> ElementList = new List<object>();

	public object[] Elements;

	public void Parse(CHJsonParserContext context)
	{
		if (context.JsonText[context.ReadHead] != '[')
		{
			throw new InvalidOperationException();
		}
		context.ReadHead++;
		bool flag = false;
		ElementList.Clear();
		while (true)
		{
			context.SkipWhitespace();
			context.AssertReadHeadIsValid();
			if (context.JsonText[context.ReadHead] == ']')
			{
				break;
			}
			CHElement cHElement = new CHElement();
			cHElement.Parse(context);
			ElementList.Add(cHElement.Value.GetManagedObject());
			flag = false;
			context.SkipWhitespace();
			context.AssertReadHeadIsValid();
			if (context.JsonText[context.ReadHead] == ',')
			{
				context.ReadHead++;
				flag = true;
			}
		}
		if (flag)
		{
			throw new InvalidOperationException();
		}
		Elements = ElementList.ToArray();
		context.ReadHead++;
	}
}
