using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Netcode;

public class NetArray<T, TField> : AbstractNetSerializable, IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IEquatable<NetArray<T, TField>> where TField : NetField<T, TField>, new()
{
	public delegate void FieldCreateEvent(int index, TField field);

	private int appendPosition;

	private readonly List<TField> elements = new List<TField>();

	public List<TField> Fields => elements;

	public T this[int index]
	{
		get
		{
			return elements[index].Get();
		}
		set
		{
			elements[index].Set(value);
		}
	}

	public int Count => elements.Count;

	public int Length => elements.Count;

	public bool IsReadOnly => false;

	public bool IsFixedSize => base.Parent != null;

	public event FieldCreateEvent OnFieldCreate;

	public NetArray()
	{
	}

	public NetArray(IEnumerable<T> values)
		: this()
	{
		int num = 0;
		foreach (T value in values)
		{
			TField val = createField(num++);
			val.Set(value);
			elements.Add(val);
		}
	}

	public NetArray(int size)
		: this()
	{
		for (int i = 0; i < size; i++)
		{
			elements.Add(createField(i));
		}
	}

	private TField createField(int index)
	{
		TField val = new TField().Interpolated(interpolate: false, wait: false);
		this.OnFieldCreate?.Invoke(index, val);
		return val;
	}

	public void Add(T item)
	{
		if (IsFixedSize)
		{
			throw new InvalidOperationException();
		}
		while (appendPosition >= elements.Count)
		{
			elements.Add(createField(elements.Count));
		}
		elements[appendPosition].Set(item);
		appendPosition++;
	}

	public void Clear()
	{
		if (IsFixedSize)
		{
			throw new InvalidOperationException();
		}
		elements.Clear();
	}

	public bool Contains(T item)
	{
		foreach (TField element in elements)
		{
			if (object.Equals(element.Get(), item))
			{
				return true;
			}
		}
		return false;
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException();
		}
		if (arrayIndex < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (Count - arrayIndex > array.Length)
		{
			throw new ArgumentException();
		}
		using IEnumerator<T> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			T current = enumerator.Current;
			array[arrayIndex++] = current;
		}
	}

	private void ensureCapacity(int size)
	{
		if (IsFixedSize && size != Count)
		{
			throw new InvalidOperationException();
		}
		while (Count < size)
		{
			elements.Add(createField(Count));
		}
	}

	public void SetCount(int size)
	{
		ensureCapacity(size);
	}

	public void Set(IList<T> values)
	{
		ensureCapacity(values.Count);
		for (int i = 0; i < Count; i++)
		{
			this[i] = values[i];
		}
	}

	public bool Equals(NetArray<T, TField> other)
	{
		return object.Equals(elements, other.elements);
	}

	public override bool Equals(object obj)
	{
		if (obj is NetArray<T, TField> other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return elements.GetHashCode() ^ 0x300A5A8D;
	}

	public IEnumerator<T> GetEnumerator()
	{
		foreach (TField element in elements)
		{
			yield return element.Get();
		}
	}

	public int IndexOf(T item)
	{
		for (int i = 0; i < Count; i++)
		{
			if (object.Equals(elements[i].Get(), item))
			{
				return i;
			}
		}
		return -1;
	}

	public void Insert(int index, T item)
	{
		if (IsFixedSize)
		{
			throw new InvalidOperationException();
		}
		TField val = createField(index);
		val.Set(item);
		elements.Insert(index, val);
	}

	public bool Remove(T item)
	{
		int num = IndexOf(item);
		if (num != -1)
		{
			RemoveAt(num);
			return true;
		}
		return false;
	}

	public void RemoveAt(int index)
	{
		if (IsFixedSize)
		{
			throw new InvalidOperationException();
		}
		elements.RemoveAt(index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public override void Read(BinaryReader reader, NetVersion version)
	{
		BitArray bitArray = reader.ReadBitArray();
		for (int i = 0; i < elements.Count; i++)
		{
			if (bitArray[i])
			{
				elements[i].Read(reader, version);
			}
		}
	}

	public override void Write(BinaryWriter writer)
	{
		BitArray bitArray = new BitArray(elements.Count);
		for (int i = 0; i < elements.Count; i++)
		{
			bitArray[i] = elements[i].Dirty;
		}
		writer.WriteBitArray(bitArray);
		for (int j = 0; j < elements.Count; j++)
		{
			if (bitArray[j])
			{
				elements[j].Write(writer);
			}
		}
	}

	public override void ReadFull(BinaryReader reader, NetVersion version)
	{
		int num = reader.ReadInt32();
		elements.Clear();
		for (int i = 0; i < num; i++)
		{
			TField val = createField(elements.Count);
			val.ReadFull(reader, version);
			if (base.Parent != null)
			{
				val.Parent = this;
			}
			elements.Add(val);
		}
	}

	public override void WriteFull(BinaryWriter writer)
	{
		writer.Write(Count);
		foreach (TField element in elements)
		{
			element.WriteFull(writer);
		}
	}

	protected override void ForEachChild(Action<INetSerializable> childAction)
	{
		foreach (TField element in elements)
		{
			childAction(element);
		}
	}

	public override string ToString()
	{
		return string.Join(",", this);
	}
}
