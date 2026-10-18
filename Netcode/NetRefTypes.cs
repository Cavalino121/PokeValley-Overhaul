using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Netcode;

internal static class NetRefTypes
{
	private static Dictionary<string, Type> types = new Dictionary<string, Type>();

	public static Type ReadType(this BinaryReader reader)
	{
		Type type = reader.ReadGenericType();
		if (type == null || !type.IsGenericTypeDefinition)
		{
			return type;
		}
		int num = type.GetGenericArguments().Length;
		Type[] array = new Type[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = reader.ReadType();
		}
		return type.MakeGenericType(array);
	}

	private static Type ReadGenericType(this BinaryReader reader)
	{
		string text = reader.ReadString();
		if (text.Length == 0)
		{
			return null;
		}
		Type type = GetType(text);
		if (type == null)
		{
			throw new InvalidOperationException();
		}
		return type;
	}

	public static void WriteType(this BinaryWriter writer, Type type)
	{
		Type type2 = type;
		if (type != null && type.IsGenericType)
		{
			type2 = type.GetGenericTypeDefinition();
		}
		writer.WriteGenericType(type2);
		if (!(type2 == null) && type2.IsGenericType)
		{
			Type[] genericArguments = type.GetGenericArguments();
			foreach (Type type3 in genericArguments)
			{
				writer.WriteType(type3);
			}
		}
	}

	private static void WriteGenericType(this BinaryWriter writer, Type type)
	{
		if (type == null)
		{
			writer.Write("");
		}
		else
		{
			writer.Write(type.FullName);
		}
	}

	public static void WriteTypeOf<T>(this BinaryWriter writer, T value)
	{
		if (value == null)
		{
			writer.WriteType(null);
		}
		else
		{
			writer.WriteType(value.GetType());
		}
	}

	private static Type GetType(string typeName)
	{
		if (types.TryGetValue(typeName, out var value))
		{
			return value;
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			value = assemblies[i].GetType(typeName);
			if (value != null)
			{
				types[typeName] = value;
				return value;
			}
		}
		return null;
	}
}
