using System;
using System.Collections.Generic;
using System.Reflection;

namespace Netcode.Validation;

/// <summary>A utility which auto-detects common net field issues.</summary>
public static class NetFieldValidator
{
	/// <summary>Detect and log warnings for common issues like net fields not added to the collection.</summary>
	/// <param name="owner">The object instance whose net fields to validate.</param>
	/// <param name="onError">The method to call when an error occurs.</param>
	public static void ValidateNetFields(INetObject<NetFields> owner, Action<string> onError)
	{
		string name = owner.NetFields.Name;
		HashSet<INetSerializable> trackedFields = new HashSet<INetSerializable>(owner.NetFields.GetFields(), ReferenceEqualityComparer.Instance);
		List<NetFieldValidatorEntry> list = new List<NetFieldValidatorEntry>();
		FieldInfo[] fields = owner.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo field in fields)
		{
			if (!NetFieldValidatorEntry.TryGetNetField(owner, field, out var netField))
			{
				continue;
			}
			if (netField.IsMarkedNotNetField())
			{
				if (!IsInCollection(trackedFields, netField))
				{
					continue;
				}
				onError(GetFieldError(name, netField, "is marked [NotNetFieldAttribute] but still added to the collection"));
			}
			list.Add(netField);
		}
		foreach (NetFieldValidatorEntry item in list)
		{
			if (item.Value == null)
			{
				onError(GetFieldError(name, item, "is null"));
			}
			else if (string.IsNullOrWhiteSpace(item.Name))
			{
				onError(GetFieldError(name, item, "has no name (and likely isn't in the collection)"));
			}
			else if (!IsInCollection(trackedFields, item.Value))
			{
				onError(GetFieldError(name, item, "isn't in the collection"));
			}
		}
	}

	/// <summary>Get a human-readable error message for a field validation error.</summary>
	/// <param name="collectionName">The name of the net fields collection being validated.</param>
	/// <param name="entry">The validator entry for the net field being validated.</param>
	/// <param name="phrase">A short phrase which indicates why it failed validation, like <c>is null</c>.</param>
	private static string GetFieldError(string collectionName, NetFieldValidatorEntry entry, string phrase)
	{
		return $"The owner of {"NetFields"} collection '{collectionName}' has field '{entry.FromField.Name}' which {phrase}.";
	}

	/// <summary>Get whether the net field is in the owner's <see cref="P:Netcode.INetObject`1.NetFields" /> collection.</summary>
	/// <param name="trackedFields">The fields that are synced as part of the collection.</param>
	/// <param name="netField">The net field instance to find.</param>
	private static bool IsInCollection(HashSet<INetSerializable> trackedFields, object netField)
	{
		if (!(netField is INetSerializable item))
		{
			if (netField is INetObject<NetFields> netObject)
			{
				return trackedFields.Contains(netObject.NetFields);
			}
			return false;
		}
		return trackedFields.Contains(item);
	}
}
