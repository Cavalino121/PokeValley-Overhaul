using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Netcode;

public class NetObjectShrinkList<T> : AbstractNetSerializable, IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IEquatable<NetObjectShrinkList<T>> where T : class, INetObject<INetSerializable>
{
	public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
	{
		private readonly NetArray<T, NetRef<T>> _array;

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

		public Enumerator(NetArray<T, NetRef<T>> array)
		{
			_array = array;
			_index = 0;
			_current = null;
			_done = false;
		}

		public bool MoveNext()
		{
			while (_index < _array.Count)
			{
				T val = _array[_index];
				_index++;
				if (val != null)
				{
					_current = val;
					return true;
				}
			}
			_done = true;
			_current = null;
			return false;
		}

		public void Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			_index = 0;
			_current = null;
			_done = false;
		}
	}

	private NetArray<T, NetRef<T>> array = new NetArray<T, NetRef<T>>();

	public T this[int index]
	{
		get
		{
			int num = 0;
			for (int i = 0; i < array.Count; i++)
			{
				T val = array[i];
				if (val != null)
				{
					if (index == num)
					{
						return val;
					}
					num++;
				}
			}
			throw new ArgumentOutOfRangeException("index");
		}
		set
		{
			int num = 0;
			for (int i = 0; i < array.Count; i++)
			{
				if (array[i] != null)
				{
					if (index == num)
					{
						array[i] = value;
						return;
					}
					num++;
				}
			}
			throw new ArgumentOutOfRangeException("index");
		}
	}

	public int Count
	{
		get
		{
			int num = 0;
			for (int i = 0; i < array.Count; i++)
			{
				if (array[i] != null)
				{
					num++;
				}
			}
			return num;
		}
	}

	public bool IsReadOnly => false;

	public NetObjectShrinkList()
	{
	}

	public NetObjectShrinkList(IEnumerable<T> values)
		: this()
	{
		foreach (T value in values)
		{
			array.Add(value);
		}
	}

	public void Add(T item)
	{
		array.Add(item);
	}

	public void Clear()
	{
		for (int i = 0; i < array.Count; i++)
		{
			array[i] = null;
		}
	}

	public void CopyFrom(IList<T> list)
	{
		if (list == this)
		{
			return;
		}
		if (list.Count > array.Count)
		{
			throw new InvalidOperationException();
		}
		for (int i = 0; i < array.Count; i++)
		{
			if (i < list.Count)
			{
				array[i] = list[i];
			}
			else
			{
				array[i] = null;
			}
		}
	}

	public void Set(IList<T> list)
	{
		CopyFrom(list);
	}

	public void MoveFrom(IList<T> list)
	{
		List<T> list2 = new List<T>(list);
		list.Clear();
		Set(list2);
	}

	public bool Contains(T item)
	{
		using (Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current == item)
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

	public bool Equals(NetObjectShrinkList<T> other)
	{
		if (Count != other.Count)
		{
			return false;
		}
		for (int i = 0; i < Count; i++)
		{
			if (this[i] != other[i])
			{
				return false;
			}
		}
		return true;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(array);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(array);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(array);
	}

	public int IndexOf(T item)
	{
		int num = 0;
		for (int i = 0; i < array.Count; i++)
		{
			T val = array[i];
			if (val != null)
			{
				if (val == item)
				{
					return num;
				}
				num++;
			}
		}
		return -1;
	}

	public void Insert(int index, T item)
	{
		int num = 0;
		for (int i = 0; i < array.Count; i++)
		{
			if (array[i] != null)
			{
				if (num == index)
				{
					array.Insert(i, item);
					return;
				}
				num++;
			}
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public override void Read(BinaryReader reader, NetVersion version)
	{
		array.Read(reader, version);
	}

	public override void ReadFull(BinaryReader reader, NetVersion version)
	{
		array.ReadFull(reader, version);
	}

	public bool Remove(T item)
	{
		for (int i = 0; i < array.Count; i++)
		{
			if (array[i] == item)
			{
				array[i] = null;
				return true;
			}
		}
		return false;
	}

	public void RemoveAt(int index)
	{
		int num = 0;
		for (int i = 0; i < array.Count; i++)
		{
			if (array[i] != null)
			{
				if (num == index)
				{
					array[i] = null;
					break;
				}
				num++;
			}
		}
	}

	public override void Write(BinaryWriter writer)
	{
		array.Write(writer);
	}

	public override void WriteFull(BinaryWriter writer)
	{
		array.WriteFull(writer);
	}

	protected override void ForEachChild(Action<INetSerializable> childAction)
	{
		childAction(array);
	}

	public override string ToString()
	{
		return string.Join(",", this);
	}
}
