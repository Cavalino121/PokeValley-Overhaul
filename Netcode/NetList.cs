using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Netcode;

public class NetList<T, TField> : AbstractNetSerializable, IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IEquatable<NetList<T, TField>> where TField : NetField<T, TField>, new()
{
	public delegate void ElementChangedEvent(NetList<T, TField> list, int index, T oldValue, T newValue);

	public delegate void ArrayReplacedEvent(NetList<T, TField> list, IList<T> before, IList<T> after);

	public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
	{
		private readonly NetList<T, TField> _list;

		private int _index;

		private T _current;

		private bool _done;

		public T Current => _current;

		object IEnumerator.Current
		{
			get
			{
				if (_done)
				{
					throw new InvalidOperationException();
				}
				return _current;
			}
		}

		public Enumerator(NetList<T, TField> list)
		{
			_list = list;
			_index = 0;
			_current = default(T);
			_done = false;
		}

		public bool MoveNext()
		{
			int value = _list.count.Value;
			if (_index < value)
			{
				_current = _list.array.Value[_index];
				_index++;
				return true;
			}
			_done = true;
			_current = default(T);
			return false;
		}

		public void Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			_index = 0;
			_current = default(T);
			_done = false;
		}
	}

	private const int initialSize = 10;

	private const double resizeFactor = 1.5;

	protected readonly NetInt count = new NetInt(0).Interpolated(interpolate: false, wait: false);

	protected readonly NetRef<NetArray<T, TField>> array = new NetRef<NetArray<T, TField>>(new NetArray<T, TField>(10)).Interpolated(interpolate: false, wait: false);

	public virtual T this[int index]
	{
		get
		{
			if (index >= Count || index < 0)
			{
				throw new ArgumentOutOfRangeException();
			}
			return array.Value[index];
		}
		set
		{
			if (index >= Count || index < 0)
			{
				throw new ArgumentOutOfRangeException();
			}
			array.Value[index] = value;
		}
	}

	public int Count => count.Value;

	public int Capacity => array.Value.Count;

	public bool IsReadOnly => false;

	public event ElementChangedEvent OnElementChanged;

	public event ArrayReplacedEvent OnArrayReplaced;

	public NetList()
	{
		hookArray(array.Value);
		array.fieldChangeVisibleEvent += delegate(NetRef<NetArray<T, TField>> arrayRef, NetArray<T, TField> oldArray, NetArray<T, TField> newArray)
		{
			if (newArray != null)
			{
				hookArray(newArray);
			}
			this.OnArrayReplaced?.Invoke(this, oldArray, newArray);
		};
	}

	public NetList(IEnumerable<T> values)
		: this()
	{
		foreach (T value in values)
		{
			Add(value);
		}
	}

	public NetList(int capacity)
		: this()
	{
		Resize(capacity);
	}

	private void hookField(int index, TField field)
	{
		if (!(field == null))
		{
			field.fieldChangeVisibleEvent += delegate(TField f, T oldValue, T newValue)
			{
				this.OnElementChanged?.Invoke(this, index, oldValue, newValue);
			};
		}
	}

	private void hookArray(NetArray<T, TField> array)
	{
		for (int i = 0; i < array.Count; i++)
		{
			hookField(i, array.Fields[i]);
		}
		array.OnFieldCreate += hookField;
	}

	private void Resize(int capacity)
	{
		count.Set(Math.Min(capacity, count.Value));
		NetArray<T, TField> value = array.Value;
		NetArray<T, TField> value2 = new NetArray<T, TField>(capacity);
		array.Value = value2;
		for (int i = 0; i < capacity && i < Count; i++)
		{
			T value3 = value[i];
			value[i] = default(T);
			array.Value[i] = value3;
		}
	}

	private void EnsureCapacity(int neededCapacity)
	{
		if (neededCapacity > Capacity)
		{
			int num = (int)((double)Capacity * 1.5);
			while (neededCapacity > num)
			{
				num = (int)((double)num * 1.5);
			}
			Resize(num);
		}
	}

	public virtual void Add(T item)
	{
		EnsureCapacity(Count + 1);
		array.Value[Count] = item;
		count.Set(count.Value + 1);
	}

	public virtual void Clear()
	{
		count.Set(0);
		Resize(10);
		fillNull();
	}

	private void fillNull()
	{
		for (int i = 0; i < Capacity; i++)
		{
			array.Value[i] = default(T);
		}
	}

	public virtual void CopyFrom(IList<T> list)
	{
		if (list != this)
		{
			EnsureCapacity(list.Count);
			fillNull();
			count.Set(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				array.Value[i] = list[i];
			}
		}
	}

	public void Set(IList<T> list)
	{
		CopyFrom(list);
	}

	public void MoveFrom(NetList<T, TField> list)
	{
		List<T> list2 = new List<T>(list);
		list.Clear();
		Set(list2);
	}

	public bool Any()
	{
		return count.Value > 0;
	}

	public virtual bool Contains(T item)
	{
		using (Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (object.Equals(enumerator.Current, item))
				{
					return true;
				}
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
		using Enumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			T current = enumerator.Current;
			array[arrayIndex++] = current;
		}
	}

	public List<T> GetRange(int index, int count)
	{
		List<T> list = new List<T>();
		for (int i = index; i < index + count; i++)
		{
			list.Add(this[i]);
		}
		return list;
	}

	public void AddRange(IEnumerable<T> collection)
	{
		foreach (T item in collection)
		{
			Add(item);
		}
	}

	public void RemoveRange(int index, int count)
	{
		for (int i = 0; i < count; i++)
		{
			RemoveAt(index);
		}
	}

	public bool Equals(NetList<T, TField> other)
	{
		return object.Equals(array, other.array);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(this);
	}

	public virtual int IndexOf(T item)
	{
		for (int i = 0; i < Count; i++)
		{
			if (object.Equals(array.Value[i], item))
			{
				return i;
			}
		}
		return -1;
	}

	public virtual void Insert(int index, T item)
	{
		if (index > Count || index < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		EnsureCapacity(Count + 1);
		count.Set(count.Value + 1);
		for (int num = Count - 1; num > index; num--)
		{
			T value = array.Value[num - 1];
			array.Value[num - 1] = default(T);
			array.Value[num] = value;
		}
		array.Value[index] = item;
	}

	public override void Read(BinaryReader reader, NetVersion version)
	{
		count.Read(reader, version);
		array.Read(reader, version);
	}

	public override void ReadFull(BinaryReader reader, NetVersion version)
	{
		count.ReadFull(reader, version);
		array.ReadFull(reader, version);
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

	public virtual void RemoveAt(int index)
	{
		if (index < 0 || index >= Count)
		{
			throw new ArgumentOutOfRangeException();
		}
		count.Set(count.Value - 1);
		for (int i = index; i < Count; i++)
		{
			T value = array.Value[i + 1];
			array.Value[i + 1] = default(T);
			array.Value[i] = value;
		}
		array.Value[Count] = default(T);
	}

	/// <summary>Remove all elements that match a condition.</summary>
	/// <param name="match">The predicate matching values to remove.</param>
	/// <returns>Returns the number of values removed from the set.</returns>
	public int RemoveWhere(Func<T, bool> match)
	{
		int num = 0;
		for (int num2 = Count - 1; num2 >= 0; num2--)
		{
			if (match(this[num2]))
			{
				RemoveAt(num2);
				num++;
			}
		}
		return num;
	}

	/// <summary>Remove all entries which don't match the filter.</summary>
	/// <param name="f">Get whether to keep the given item.</param>
	[Obsolete("Use RemoveWhere instead.")]
	public void Filter(Func<T, bool> f)
	{
		RemoveWhere((T pair) => !f(pair));
	}

	public override void Write(BinaryWriter writer)
	{
		count.Write(writer);
		array.Write(writer);
	}

	public override void WriteFull(BinaryWriter writer)
	{
		count.WriteFull(writer);
		array.WriteFull(writer);
	}

	protected override void ForEachChild(Action<INetSerializable> childAction)
	{
		childAction(count);
		childAction(array);
	}

	public override string ToString()
	{
		return string.Join(",", this);
	}
}
