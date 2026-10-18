using System;

namespace ContentManifest.Internal;

internal class CHValue : CHParsable
{
	public CHValueUnion RawValue;

	public CHValueEnum ValueType = CHValueEnum.Unknown;

	public CHValue()
	{
		RawValue.ValueNull = null;
	}

	public void Parse(CHJsonParserContext context)
	{
		if (context.ReadHead >= context.JsonText.Length)
		{
			throw new InvalidOperationException();
		}
		CHParsable cHParsable = null;
		char c = context.JsonText[context.ReadHead];
		switch (c)
		{
		case '{':
			cHParsable = (RawValue.ValueObject = new CHObject());
			ValueType = CHValueEnum.Object;
			break;
		case '[':
			cHParsable = (RawValue.ValueArray = new CHArray());
			ValueType = CHValueEnum.Array;
			break;
		case '"':
			cHParsable = (RawValue.ValueString = new CHString());
			ValueType = CHValueEnum.String;
			break;
		case 'f':
		case 't':
			cHParsable = (RawValue.ValueBoolean = new CHBoolean());
			ValueType = CHValueEnum.Boolean;
			break;
		case 'n':
			if (context.ReadHead + 3 >= context.JsonText.Length)
			{
				throw new InvalidOperationException();
			}
			if (context.JsonText[context.ReadHead + 1] != 'u' || context.JsonText[context.ReadHead + 2] != 'l' || context.JsonText[context.ReadHead + 3] != 'l')
			{
				throw new InvalidOperationException();
			}
			cHParsable = null;
			ValueType = CHValueEnum.Null;
			break;
		default:
			if (CHNumber.IsValidPrefix(c))
			{
				cHParsable = (RawValue.ValueNumber = new CHNumber());
				ValueType = CHValueEnum.Number;
				break;
			}
			throw new InvalidOperationException();
		}
		cHParsable?.Parse(context);
	}

	public object GetManagedObject()
	{
		return ValueType switch
		{
			CHValueEnum.Object => RawValue.ValueObject.Members, 
			CHValueEnum.Array => RawValue.ValueArray.Elements, 
			CHValueEnum.String => RawValue.ValueString.RawString, 
			CHValueEnum.Number => RawValue.ValueNumber.RawDouble, 
			CHValueEnum.Boolean => RawValue.ValueBoolean.RawBoolean, 
			CHValueEnum.Null => null, 
			_ => throw new InvalidOperationException(), 
		};
	}
}
