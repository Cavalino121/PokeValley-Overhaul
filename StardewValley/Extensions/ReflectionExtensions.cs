using System;
using System.Reflection;
using Sickhead.Engine.Util;

namespace StardewValley.Extensions;

/// <summary>Provides utility extension for reflection.</summary>
public static class ReflectionExtensions
{
	/// <summary>Try to set the field or property's value from its string representation.</summary>
	/// <param name="info">The field or property to set.</param>
	/// <param name="obj">The object instance whose field or property to set.</param>
	/// <param name="rawValue">A string representation of the value to set. This will be converted to the property type if possible.</param>
	/// <param name="index">Optional index values for an indexed property. This should be null for fields or non-indexed properties.</param>
	/// <param name="error">An error indicating why the property value could not be set, if applicable.</param>
	public static bool TrySetValueFromString(this MemberInfo info, object obj, string rawValue, object[] index, out string error)
	{
		Type type;
		bool flag;
		if (!(info is FieldInfo fieldInfo))
		{
			if (!(info is PropertyInfo propertyInfo))
			{
				error = "the member is not a field or property";
				return false;
			}
			type = propertyInfo.PropertyType;
			flag = propertyInfo.CanWrite;
		}
		else
		{
			type = fieldInfo.FieldType;
			flag = !fieldInfo.IsLiteral && !fieldInfo.IsLiteral;
		}
		if (!flag)
		{
			error = "the " + ((info is FieldInfo) ? "field" : "property") + " property is read-only";
			return false;
		}
		object value;
		try
		{
			value = Convert.ChangeType(rawValue, type);
		}
		catch (FormatException)
		{
			error = $"can't convert value '{rawValue}' to the '{type.FullName}' type";
			return false;
		}
		try
		{
			info.SetValue(obj, value, index);
			error = null;
			return true;
		}
		catch (Exception ex2)
		{
			error = ex2.Message;
			return false;
		}
	}
}
