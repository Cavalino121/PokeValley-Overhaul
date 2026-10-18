using System;
using System.Linq;

namespace Force.DeepCloner.Helpers;

internal static class DeepClonerGenerator
{
	public static T CloneObject<T>(T obj)
	{
		if (obj is ValueType)
		{
			Type type = obj.GetType();
			if (typeof(T) == type)
			{
				if (DeepClonerSafeTypes.CanReturnSameObject(type))
				{
					return obj;
				}
				return CloneStructInternal(obj, new DeepCloneState());
			}
		}
		return (T)CloneClassRoot(obj);
	}

	private static object CloneClassRoot(object obj)
	{
		if (obj == null)
		{
			return null;
		}
		Func<object, DeepCloneState, object> func = (Func<object, DeepCloneState, object>)DeepClonerCache.GetOrAddClass(obj.GetType(), (Type t) => GenerateCloner(t, asObject: true));
		if (func == null)
		{
			return obj;
		}
		return func(obj, new DeepCloneState());
	}

	internal static object CloneClassInternal(object obj, DeepCloneState state)
	{
		if (obj == null)
		{
			return null;
		}
		Func<object, DeepCloneState, object> func = (Func<object, DeepCloneState, object>)DeepClonerCache.GetOrAddClass(obj.GetType(), (Type t) => GenerateCloner(t, asObject: true));
		if (func == null)
		{
			return obj;
		}
		object knownRef = state.GetKnownRef(obj);
		if (knownRef != null)
		{
			return knownRef;
		}
		return func(obj, state);
	}

	private static T CloneStructInternal<T>(T obj, DeepCloneState state)
	{
		Func<T, DeepCloneState, T> clonerForValueType = GetClonerForValueType<T>();
		if (clonerForValueType == null)
		{
			return obj;
		}
		return clonerForValueType(obj, state);
	}

	internal static T[] Clone1DimArraySafeInternal<T>(T[] obj, DeepCloneState state)
	{
		T[] array = new T[obj.Length];
		state.AddKnownRef(obj, array);
		Array.Copy(obj, array, obj.Length);
		return array;
	}

	internal static T[] Clone1DimArrayStructInternal<T>(T[] obj, DeepCloneState state)
	{
		if (obj == null)
		{
			return null;
		}
		int num = obj.Length;
		T[] array = new T[num];
		state.AddKnownRef(obj, array);
		Func<T, DeepCloneState, T> clonerForValueType = GetClonerForValueType<T>();
		for (int i = 0; i < num; i++)
		{
			array[i] = clonerForValueType(obj[i], state);
		}
		return array;
	}

	internal static T[] Clone1DimArrayClassInternal<T>(T[] obj, DeepCloneState state)
	{
		if (obj == null)
		{
			return null;
		}
		int num = obj.Length;
		T[] array = new T[num];
		state.AddKnownRef(obj, array);
		for (int i = 0; i < num; i++)
		{
			array[i] = (T)CloneClassInternal(obj[i], state);
		}
		return array;
	}

	internal static T[,] Clone2DimArrayInternal<T>(T[,] obj, DeepCloneState state)
	{
		if (obj == null)
		{
			return null;
		}
		int length = obj.GetLength(0);
		int length2 = obj.GetLength(1);
		T[,] array = new T[length, length2];
		state.AddKnownRef(obj, array);
		if (DeepClonerSafeTypes.CanReturnSameObject(typeof(T)))
		{
			Array.Copy(obj, array, obj.Length);
			return array;
		}
		if (typeof(T).IsValueType())
		{
			Func<T, DeepCloneState, T> clonerForValueType = GetClonerForValueType<T>();
			for (int i = 0; i < length; i++)
			{
				for (int j = 0; j < length2; j++)
				{
					array[i, j] = clonerForValueType(obj[i, j], state);
				}
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				for (int l = 0; l < length2; l++)
				{
					array[k, l] = (T)CloneClassInternal(obj[k, l], state);
				}
			}
		}
		return array;
	}

	internal static Array CloneAbstractArrayInternal(Array obj, DeepCloneState state)
	{
		if (obj == null)
		{
			return null;
		}
		int rank = obj.Rank;
		int[] array = Enumerable.Range(0, rank).Select(obj.GetLowerBound).ToArray();
		int[] array2 = Enumerable.Range(0, rank).Select(obj.GetLength).ToArray();
		int[] array3 = Enumerable.Range(0, rank).Select(obj.GetLowerBound).ToArray();
		Array array4 = Array.CreateInstance(obj.GetType().GetElementType(), array2, array);
		state.AddKnownRef(obj, array4);
		while (true)
		{
			array4.SetValue(CloneClassInternal(obj.GetValue(array3), state), array3);
			int num = rank - 1;
			while (true)
			{
				array3[num]++;
				if (array3[num] < array[num] + array2[num])
				{
					break;
				}
				array3[num] = array[num];
				num--;
				if (num < 0)
				{
					return array4;
				}
			}
		}
	}

	internal static Func<T, DeepCloneState, T> GetClonerForValueType<T>()
	{
		return (Func<T, DeepCloneState, T>)DeepClonerCache.GetOrAddStructAsObject(typeof(T), (Type t) => GenerateCloner(t, asObject: false));
	}

	private static object GenerateCloner(Type t, bool asObject)
	{
		if (DeepClonerSafeTypes.CanReturnSameObject(t) && asObject && !t.IsValueType())
		{
			return null;
		}
		return DeepClonerExprGenerator.GenerateClonerInternal(t, asObject);
	}

	public static object CloneObjectTo(object objFrom, object objTo, bool isDeep)
	{
		if (objTo == null)
		{
			return null;
		}
		if (objFrom == null)
		{
			throw new ArgumentNullException("objFrom", "Cannot copy null object to another");
		}
		Type type = objFrom.GetType();
		if (!type.IsInstanceOfType(objTo))
		{
			throw new InvalidOperationException("From object should be derived from From object, but From object has type " + objFrom.GetType().FullName + " and to " + objTo.GetType().FullName);
		}
		if (objFrom is string)
		{
			throw new InvalidOperationException("It is forbidden to clone strings");
		}
		Func<object, object, DeepCloneState, object> func = (Func<object, object, DeepCloneState, object>)(isDeep ? DeepClonerCache.GetOrAddDeepClassTo(type, (Type t) => ClonerToExprGenerator.GenerateClonerInternal(t, isDeepClone: true)) : DeepClonerCache.GetOrAddShallowClassTo(type, (Type t) => ClonerToExprGenerator.GenerateClonerInternal(t, isDeepClone: false)));
		if (func == null)
		{
			return objTo;
		}
		return func(objFrom, objTo, new DeepCloneState());
	}
}
