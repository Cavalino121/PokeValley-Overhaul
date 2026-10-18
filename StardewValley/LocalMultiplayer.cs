using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace StardewValley;

public class LocalMultiplayer
{
	public delegate void StaticInstanceMethod(object staticVarsHolder);

	internal static List<FieldInfo> staticFields;

	internal static List<object> staticDefaults;

	public static Type StaticVarHolderType;

	private static DynamicMethod staticDefaultMethod;

	private static DynamicMethod staticSaveMethod;

	private static DynamicMethod staticLoadMethod;

	public static StaticInstanceMethod StaticSetDefault;

	public static StaticInstanceMethod StaticSave;

	public static StaticInstanceMethod StaticLoad;

	public static bool IsLocalMultiplayer(bool is_local_only = false)
	{
		if (is_local_only)
		{
			return Game1.hasLocalClientsOnly;
		}
		return GameRunner.instance.gameInstances.Count > 1;
	}

	public static void Initialize()
	{
		GetStaticFieldsAndDefaults();
		GenerateDynamicMethodsForStatics();
	}

	private static void GetStaticFieldsAndDefaults()
	{
		staticFields = new List<FieldInfo>();
		staticDefaults = new List<object>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft", "MonoGame", "mscorlib", "NetCode", "System", "xTile" };
		List<Type> list = new List<Type>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (!hashSet.Contains(assembly.GetName().Name.Split('.')[0]))
			{
				Type[] types = assembly.GetTypes();
				foreach (Type item in types)
				{
					list.Add(item);
				}
			}
		}
		foreach (Type item2 in list)
		{
			if (item2.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
			{
				continue;
			}
			bool flag = item2.GetCustomAttribute<InstanceStatics>() != null;
			FieldInfo[] fields = item2.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (!fieldInfo.IsInitOnly && fieldInfo.IsStatic && !fieldInfo.IsLiteral && (flag || fieldInfo.GetCustomAttribute<InstancedStatic>() != null) && fieldInfo.GetCustomAttribute<NonInstancedStatic>() == null)
				{
					RuntimeHelpers.RunClassConstructor(fieldInfo.DeclaringType.TypeHandle);
					staticFields.Add(fieldInfo);
					staticDefaults.Add(fieldInfo.GetValue(null));
				}
			}
		}
	}

	private static void GenerateDynamicMethodsForStatics()
	{
		TypeBuilder typeBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("StardewValley.StaticInstanceVars"), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("MainModule").DefineType("StardewValley.StaticInstanceVars", TypeAttributes.Public | TypeAttributes.AutoClass);
		foreach (FieldInfo staticField in staticFields)
		{
			typeBuilder.DefineField(staticField.DeclaringType.Name + "_" + staticField.Name, staticField.FieldType, FieldAttributes.Public);
		}
		StaticVarHolderType = typeBuilder.CreateType();
		staticDefaultMethod = new DynamicMethod("SetStaticVarsToDefault", null, new Type[1] { typeof(object) }, typeof(Game1).Module, skipVisibility: true);
		ILGenerator iLGenerator = staticDefaultMethod.GetILGenerator();
		LocalBuilder localBuilder = iLGenerator.DeclareLocal(StaticVarHolderType);
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Castclass, StaticVarHolderType);
		iLGenerator.Emit(OpCodes.Stloc_0);
		FieldInfo field = typeof(LocalMultiplayer).GetField("staticDefaults", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method = typeof(List<object>).GetMethod("get_Item");
		for (int i = 0; i < staticFields.Count; i++)
		{
			FieldInfo fieldInfo = staticFields[i];
			iLGenerator.Emit(OpCodes.Ldloc, localBuilder.LocalIndex);
			iLGenerator.Emit(OpCodes.Ldsfld, field);
			iLGenerator.Emit(OpCodes.Ldc_I4, i);
			iLGenerator.Emit(OpCodes.Callvirt, method);
			if (fieldInfo.FieldType.IsValueType)
			{
				iLGenerator.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
			}
			else
			{
				iLGenerator.Emit(OpCodes.Castclass, fieldInfo.FieldType);
			}
			iLGenerator.Emit(OpCodes.Stfld, StaticVarHolderType.GetField(fieldInfo.DeclaringType.Name + "_" + fieldInfo.Name));
		}
		iLGenerator.Emit(OpCodes.Ret);
		StaticSetDefault = (StaticInstanceMethod)staticDefaultMethod.CreateDelegate(typeof(StaticInstanceMethod));
		staticSaveMethod = new DynamicMethod("SaveStaticVars", null, new Type[1] { typeof(object) }, typeof(Game1).Module, skipVisibility: true);
		iLGenerator = staticSaveMethod.GetILGenerator();
		localBuilder = iLGenerator.DeclareLocal(StaticVarHolderType);
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Castclass, StaticVarHolderType);
		iLGenerator.Emit(OpCodes.Stloc_0);
		foreach (FieldInfo staticField2 in staticFields)
		{
			iLGenerator.Emit(OpCodes.Ldloc, localBuilder.LocalIndex);
			iLGenerator.Emit(OpCodes.Ldsfld, staticField2);
			iLGenerator.Emit(OpCodes.Stfld, StaticVarHolderType.GetField(staticField2.DeclaringType.Name + "_" + staticField2.Name));
		}
		iLGenerator.Emit(OpCodes.Ret);
		StaticSave = (StaticInstanceMethod)staticSaveMethod.CreateDelegate(typeof(StaticInstanceMethod));
		staticLoadMethod = new DynamicMethod("LoadStaticVars", null, new Type[1] { typeof(object) }, typeof(Game1).Module, skipVisibility: true);
		iLGenerator = staticLoadMethod.GetILGenerator();
		localBuilder = iLGenerator.DeclareLocal(StaticVarHolderType);
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Castclass, StaticVarHolderType);
		iLGenerator.Emit(OpCodes.Stloc_0);
		foreach (FieldInfo staticField3 in staticFields)
		{
			iLGenerator.Emit(OpCodes.Ldloc, localBuilder.LocalIndex);
			iLGenerator.Emit(OpCodes.Ldfld, StaticVarHolderType.GetField(staticField3.DeclaringType.Name + "_" + staticField3.Name));
			iLGenerator.Emit(OpCodes.Stsfld, staticField3);
		}
		iLGenerator.Emit(OpCodes.Ret);
		StaticLoad = (StaticInstanceMethod)staticLoadMethod.CreateDelegate(typeof(StaticInstanceMethod));
	}

	public static void SaveOptions()
	{
		if (Game1.player != null && Game1.player.isCustomized.Value)
		{
			Game1.splitscreenOptions[Game1.player.UniqueMultiplayerID] = Game1.options;
		}
	}
}
