using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Netcode.Validation;

namespace Netcode;

public class NetFields : AbstractNetSerializable
{
	/// <summary>Whether to run detailed validation checks to detect possible bugs with net fields (e.g. fields which aren't added to the owner's <see cref="T:Netcode.NetFields" /> collection).</summary>
	/// <remarks>These validation checks are expensive and should normally be disabled.</remarks>
	public static bool ShouldValidateNetFields;

	/// <summary>The net fields within the collection to synchronize between players.</summary>
	private readonly List<INetSerializable> fields = new List<INetSerializable>();

	/// <summary>A name for this net field collection, used for troubleshooting network sync.</summary>
	public new string Name { get; }

	/// <summary>The object instance which owns this collection.</summary>
	/// <remarks>This is the instance which has the <see cref="T:Netcode.NetFields" /> property; see also <see cref="P:Netcode.AbstractNetSerializable.Parent" /> for the net field it's synced through (if any). For example, <see cref="P:StardewValley.Character.NetFields" />'s owner is a <see cref="T:StardewValley.Character" /> and its parent is another <see cref="T:Netcode.NetFields" />.</remarks>
	public INetObject<NetFields> Owner { get; private set; }

	/// <summary>Construct an instance.</summary>
	/// <param name="name">A name for this net field collection, used for troubleshooting network sync.</param>
	public NetFields(string name)
	{
		Name = name;
	}

	/// <summary>Set the object instance which owns this collection, used to enable validation and simplify troubleshooting.</summary>
	/// <param name="owner">The instance which owns this net field collection.</param>
	public NetFields SetOwner(INetObject<NetFields> owner)
	{
		Owner = owner;
		return this;
	}

	/// <summary>Get a suggested name for an instance's net field collection, for cases where it's useful to show the name of the subtype.</summary>
	/// <typeparam name="TBaseType">The base type which defines the net field collection.</typeparam>
	/// <param name="instance">The instance which inherits the net field collection.</param>
	public static string GetNameForInstance<TBaseType>(TBaseType instance)
	{
		Type typeFromHandle = typeof(TBaseType);
		Type type = instance.GetType();
		if (!(typeFromHandle == type))
		{
			return typeFromHandle.Name + " (" + type.Name + ")";
		}
		return typeFromHandle.Name;
	}

	/// <summary>Get the fields that are in the collection.</summary>
	public IEnumerable<INetSerializable> GetFields()
	{
		return fields;
	}

	public void CancelInterpolation()
	{
		foreach (INetSerializable field in fields)
		{
			if (field is InterpolationCancellable interpolationCancellable)
			{
				interpolationCancellable.CancelInterpolation();
			}
		}
	}

	/// <summary>Add a net field to this collection.</summary>
	/// <param name="field">The field to sync as part of this collection.</param>
	/// <param name="name">A readable name for the field within the collection, used for troubleshooting network sync. This should usually be omitted, so it's auto-generated from the expression passed to <paramref name="field" />.</param>
	/// <exception cref="T:System.InvalidOperationException">The field is already part of another collection, or this collection has already been fully initialized.</exception>
	/// <remarks><see cref="M:Netcode.NetFields.SetOwner(Netcode.INetObject{Netcode.NetFields})" /> should be called before any fields are added to enable readable error logs.</remarks>
	public NetFields AddField(INetSerializable field, [CallerArgumentExpression("field")] string name = null)
	{
		name = name ?? field.GetType().FullName;
		if (Owner == null)
		{
			NetHelper.LogWarning($"Field '{name}' was added to the '{Name}' net fields before {"SetOwner"} was called.");
		}
		if (field.Parent != null)
		{
			throw new InvalidOperationException($"Can't add field '{name}' to the '{Name}' net fields because it's already part of the {field.Parent.Name} tree.");
		}
		if (base.Parent != null)
		{
			throw new InvalidOperationException($"Can't add field '{name}' to the '{Name}' net fields, because they've already been added to a tree.");
		}
		if (ShouldValidateNetFields)
		{
			foreach (INetSerializable field2 in fields)
			{
				if (field == field2)
				{
					NetHelper.LogWarning($"Field '{name}' was added to the '{Name}' net fields multiple times.");
					break;
				}
			}
		}
		field.Name = Name + ": " + name;
		fields.Add(field);
		return this;
	}

	protected override void SetParent(INetSerializable parent)
	{
		base.SetParent(parent);
		ValidateNetFields();
	}

	/// <summary>Detect and log warnings for common issues like net fields not added to the collection.</summary>
	protected void ValidateNetFields()
	{
		if (Owner == null)
		{
			NetHelper.LogWarning($"{"NetFields"} collection '{Name}' was initialized without calling {"SetOwner"}, so it can't be validated.");
		}
		else if (this != Owner.NetFields)
		{
			NetHelper.LogWarning($"{"NetFields"} collection '{Name}' has its own owner set to an {Owner?.GetType().FullName} instance whose {"NetFields"} field doesn't reference this collection.");
		}
		else if (ShouldValidateNetFields)
		{
			NetFieldValidator.ValidateNetFields(Owner, NetHelper.LogWarning);
		}
	}

	public override void Read(BinaryReader reader, NetVersion version)
	{
		BitArray bitArray = reader.ReadBitArray();
		if (fields.Count != bitArray.Length)
		{
			throw new InvalidOperationException();
		}
		for (int i = 0; i < fields.Count; i++)
		{
			if (bitArray[i])
			{
				INetSerializable netSerializable = fields[i];
				try
				{
					netSerializable.Read(reader, version);
				}
				catch (Exception innerException)
				{
					throw new InvalidOperationException($"Failed reading {Name} field '{netSerializable.Name}'", innerException);
				}
			}
		}
	}

	public override void Write(BinaryWriter writer)
	{
		BitArray bitArray = new BitArray(fields.Count);
		for (int i = 0; i < fields.Count; i++)
		{
			bitArray[i] = fields[i].Dirty;
		}
		writer.WriteBitArray(bitArray);
		for (int j = 0; j < fields.Count; j++)
		{
			if (bitArray[j])
			{
				INetSerializable netSerializable = fields[j];
				writer.Push(Convert.ToString(j));
				try
				{
					netSerializable.Write(writer);
				}
				catch (Exception innerException)
				{
					throw new InvalidOperationException($"Failed writing {Name} field '{netSerializable.Name}'", innerException);
				}
				writer.Pop();
			}
		}
	}

	public override void ReadFull(BinaryReader reader, NetVersion version)
	{
		foreach (INetSerializable field in fields)
		{
			try
			{
				field.ReadFull(reader, version);
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException($"Failed reading {Name} field '{field.Name}'", innerException);
			}
		}
	}

	public override void WriteFull(BinaryWriter writer)
	{
		for (int i = 0; i < fields.Count; i++)
		{
			INetSerializable netSerializable = fields[i];
			writer.Push(Convert.ToString(i));
			try
			{
				netSerializable.WriteFull(writer);
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException($"Failed writing {Name} field '{netSerializable.Name}'", innerException);
			}
			writer.Pop();
		}
	}

	public virtual void CopyFrom(NetFields source)
	{
		try
		{
			using MemoryStream memoryStream = new MemoryStream();
			using BinaryWriter writer = new BinaryWriter(memoryStream);
			using BinaryReader reader = new BinaryReader(memoryStream);
			source.WriteFull(writer);
			memoryStream.Seek(0L, SeekOrigin.Begin);
			if (base.Root == null)
			{
				ReadFull(reader, new NetClock().netVersion);
			}
			else
			{
				ReadFull(reader, base.Root.Clock.netVersion);
			}
			MarkClean();
		}
		catch (Exception innerException)
		{
			throw new InvalidOperationException($"Failed copying {Name} fields from '{source.Name}'", innerException);
		}
	}

	protected override void ForEachChild(Action<INetSerializable> childAction)
	{
		foreach (INetSerializable field in fields)
		{
			childAction(field);
		}
	}
}
