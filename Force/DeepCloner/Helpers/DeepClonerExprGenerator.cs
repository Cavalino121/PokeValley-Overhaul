using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Force.DeepCloner.Helpers;

internal static class DeepClonerExprGenerator
{
	private static readonly ConcurrentDictionary<FieldInfo, bool> _readonlyFields = new ConcurrentDictionary<FieldInfo, bool>();

	private static FieldInfo _attributesFieldInfo = typeof(FieldInfo).GetPrivateField("m_fieldAttributes");

	internal static object GenerateClonerInternal(Type realType, bool asObject)
	{
		return GenerateProcessMethod(realType, asObject && realType.IsValueType());
	}

	internal static void ForceSetField(FieldInfo field, object obj, object value)
	{
		FieldInfo privateField = field.GetType().GetPrivateField("m_fieldAttributes");
		if (privateField == null || !(privateField.GetValue(field) is FieldAttributes fieldAttributes))
		{
			return;
		}
		lock (privateField)
		{
			privateField.SetValue(field, fieldAttributes & ~FieldAttributes.InitOnly);
			field.SetValue(obj, value);
			privateField.SetValue(field, fieldAttributes | FieldAttributes.InitOnly);
		}
	}

	private static object GenerateProcessMethod(Type type, bool unboxStruct)
	{
		if (type.IsArray)
		{
			return GenerateProcessArrayMethod(type);
		}
		if (type.FullName != null && type.FullName.StartsWith("System.Tuple`"))
		{
			Type[] array = type.GenericArguments();
			if (array.Length < 10 && array.All(DeepClonerSafeTypes.CanReturnSameObject))
			{
				return GenerateProcessTupleMethod(type);
			}
		}
		Type type2 = ((unboxStruct || type.IsClass()) ? typeof(object) : type);
		List<Expression> list = new List<Expression>();
		ParameterExpression parameterExpression = Expression.Parameter(type2);
		ParameterExpression parameterExpression2 = parameterExpression;
		ParameterExpression parameterExpression3 = Expression.Variable(type);
		ParameterExpression parameterExpression4 = Expression.Parameter(typeof(DeepCloneState));
		if (!type.IsValueType())
		{
			MethodInfo privateMethod = typeof(object).GetPrivateMethod("MemberwiseClone");
			list.Add(Expression.Assign(parameterExpression3, Expression.Convert(Expression.Call(parameterExpression, privateMethod), type)));
			parameterExpression2 = Expression.Variable(type);
			list.Add(Expression.Assign(parameterExpression2, Expression.Convert(parameterExpression, type)));
			list.Add(Expression.Call(parameterExpression4, typeof(DeepCloneState).GetMethod("AddKnownRef"), parameterExpression, parameterExpression3));
		}
		else if (unboxStruct)
		{
			list.Add(Expression.Assign(parameterExpression3, Expression.Unbox(parameterExpression, type)));
			parameterExpression2 = Expression.Variable(type);
			list.Add(Expression.Assign(parameterExpression2, parameterExpression3));
		}
		else
		{
			list.Add(Expression.Assign(parameterExpression3, parameterExpression));
		}
		List<FieldInfo> list2 = new List<FieldInfo>();
		Type type3 = type;
		while (!(type3.Name == "ContextBoundObject"))
		{
			list2.AddRange(type3.GetDeclaredFields());
			type3 = type3.BaseType();
			if (!(type3 != null))
			{
				break;
			}
		}
		foreach (FieldInfo item in list2)
		{
			if (!DeepClonerSafeTypes.CanReturnSameObject(item.FieldType))
			{
				MethodInfo method = (item.FieldType.IsValueType() ? typeof(DeepClonerGenerator).GetPrivateStaticMethod("CloneStructInternal").MakeGenericMethod(item.FieldType) : typeof(DeepClonerGenerator).GetPrivateStaticMethod("CloneClassInternal"));
				MemberExpression arg = Expression.Field(parameterExpression2, item);
				Expression expression = Expression.Call(method, arg, parameterExpression4);
				if (!item.FieldType.IsValueType())
				{
					expression = Expression.Convert(expression, item.FieldType);
				}
				if (_readonlyFields.GetOrAdd(item, (FieldInfo f) => f.IsInitOnly))
				{
					MethodInfo privateStaticMethod = typeof(DeepClonerExprGenerator).GetPrivateStaticMethod("ForceSetField");
					list.Add(Expression.Call(privateStaticMethod, Expression.Constant(item), Expression.Convert(parameterExpression3, typeof(object)), Expression.Convert(expression, typeof(object))));
				}
				else
				{
					list.Add(Expression.Assign(Expression.Field(parameterExpression3, item), expression));
				}
			}
		}
		list.Add(Expression.Convert(parameterExpression3, type2));
		Type delegateType = typeof(Func<, , >).MakeGenericType(type2, typeof(DeepCloneState), type2);
		List<ParameterExpression> list3 = new List<ParameterExpression>();
		if (parameterExpression != parameterExpression2)
		{
			list3.Add(parameterExpression2);
		}
		list3.Add(parameterExpression3);
		return Expression.Lambda(delegateType, Expression.Block(list3, list), parameterExpression, parameterExpression4).Compile();
	}

	private static object GenerateProcessArrayMethod(Type type)
	{
		Type elementType = type.GetElementType();
		int arrayRank = type.GetArrayRank();
		MethodInfo method;
		if (arrayRank != 1 || type != elementType.MakeArrayType())
		{
			method = ((arrayRank != 2 || !(type == elementType.MakeArrayType())) ? typeof(DeepClonerGenerator).GetPrivateStaticMethod("CloneAbstractArrayInternal") : typeof(DeepClonerGenerator).GetPrivateStaticMethod("Clone2DimArrayInternal").MakeGenericMethod(elementType));
		}
		else
		{
			string methodName = "Clone1DimArrayClassInternal";
			if (DeepClonerSafeTypes.CanReturnSameObject(elementType))
			{
				methodName = "Clone1DimArraySafeInternal";
			}
			else if (elementType.IsValueType())
			{
				methodName = "Clone1DimArrayStructInternal";
			}
			method = typeof(DeepClonerGenerator).GetPrivateStaticMethod(methodName).MakeGenericMethod(elementType);
		}
		ParameterExpression parameterExpression = Expression.Parameter(typeof(object));
		ParameterExpression parameterExpression2 = Expression.Parameter(typeof(DeepCloneState));
		MethodCallExpression body = Expression.Call(method, Expression.Convert(parameterExpression, type), parameterExpression2);
		return Expression.Lambda(typeof(Func<, , >).MakeGenericType(typeof(object), typeof(DeepCloneState), typeof(object)), body, parameterExpression, parameterExpression2).Compile();
	}

	private static object GenerateProcessTupleMethod(Type type)
	{
		ParameterExpression parameterExpression = Expression.Parameter(typeof(object));
		ParameterExpression parameterExpression2 = Expression.Parameter(typeof(DeepCloneState));
		ParameterExpression local = Expression.Variable(type);
		BinaryExpression binaryExpression = Expression.Assign(local, Expression.Convert(parameterExpression, type));
		Type typeFromHandle = typeof(Func<object, DeepCloneState, object>);
		int tupleLength = type.GenericArguments().Length;
		BinaryExpression binaryExpression2 = Expression.Assign(local, Expression.New(type.GetPublicConstructors().First((ConstructorInfo x) => x.GetParameters().Length == tupleLength), from x in type.GetPublicProperties()
			orderby x.Name
			where x.CanRead && x.Name.StartsWith("Item") && char.IsDigit(x.Name[4])
			select Expression.Property(local, x.Name)));
		return Expression.Lambda(typeFromHandle, Expression.Block(new ParameterExpression[1] { local }, binaryExpression, binaryExpression2, Expression.Call(parameterExpression2, typeof(DeepCloneState).GetMethod("AddKnownRef"), parameterExpression, local), parameterExpression), parameterExpression, parameterExpression2).Compile();
	}
}
