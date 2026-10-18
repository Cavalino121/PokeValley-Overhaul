using System;
using System.Runtime.CompilerServices;

namespace Force.DeepCloner.Helpers;

internal class DeepCloneState
{
	private class MiniDictionary
	{
		private struct Entry
		{
			public int HashCode;

			public int Next;

			public object Key;

			public object Value;
		}

		private int[] _buckets;

		private Entry[] _entries;

		private int _count;

		private static readonly int[] _primes = new int[72]
		{
			3, 7, 11, 17, 23, 29, 37, 47, 59, 71,
			89, 107, 131, 163, 197, 239, 293, 353, 431, 521,
			631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371,
			4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023,
			25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363,
			156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
			968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559,
			5999471, 7199369
		};

		public MiniDictionary()
			: this(5)
		{
		}

		public MiniDictionary(int capacity)
		{
			if (capacity > 0)
			{
				Initialize(capacity);
			}
		}

		public object FindEntry(object key)
		{
			if (_buckets != null)
			{
				int num = RuntimeHelpers.GetHashCode(key) & 0x7FFFFFFF;
				Entry[] entries = _entries;
				for (int num2 = _buckets[num % _buckets.Length]; num2 >= 0; num2 = entries[num2].Next)
				{
					if (entries[num2].HashCode == num && entries[num2].Key == key)
					{
						return entries[num2].Value;
					}
				}
			}
			return null;
		}

		private static int GetPrime(int min)
		{
			for (int i = 0; i < _primes.Length; i++)
			{
				int num = _primes[i];
				if (num >= min)
				{
					return num;
				}
			}
			for (int j = min | 1; j < int.MaxValue; j += 2)
			{
				if (IsPrime(j) && (j - 1) % 101 != 0)
				{
					return j;
				}
			}
			return min;
		}

		private static bool IsPrime(int candidate)
		{
			if ((candidate & 1) != 0)
			{
				int num = (int)Math.Sqrt(candidate);
				for (int i = 3; i <= num; i += 2)
				{
					if (candidate % i == 0)
					{
						return false;
					}
				}
				return true;
			}
			return candidate == 2;
		}

		private static int ExpandPrime(int oldSize)
		{
			int num = 2 * oldSize;
			if ((uint)num > 2146435069u && 2146435069 > oldSize)
			{
				return 2146435069;
			}
			return GetPrime(num);
		}

		private void Initialize(int size)
		{
			_buckets = new int[size];
			for (int i = 0; i < _buckets.Length; i++)
			{
				_buckets[i] = -1;
			}
			_entries = new Entry[size];
		}

		public void Insert(object key, object value)
		{
			if (_buckets == null)
			{
				Initialize(0);
			}
			int num = RuntimeHelpers.GetHashCode(key) & 0x7FFFFFFF;
			int num2 = num % _buckets.Length;
			Entry[] entries = _entries;
			if (_count == entries.Length)
			{
				Resize();
				entries = _entries;
				num2 = num % _buckets.Length;
			}
			int count = _count;
			_count++;
			entries[count].HashCode = num;
			entries[count].Next = _buckets[num2];
			entries[count].Key = key;
			entries[count].Value = value;
			_buckets[num2] = count;
		}

		private void Resize()
		{
			Resize(ExpandPrime(_count));
		}

		private void Resize(int newSize)
		{
			int[] array = new int[newSize];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = -1;
			}
			Entry[] array2 = new Entry[newSize];
			Array.Copy(_entries, 0, array2, 0, _count);
			for (int j = 0; j < _count; j++)
			{
				if (array2[j].HashCode >= 0)
				{
					int num = array2[j].HashCode % newSize;
					array2[j].Next = array[num];
					array[num] = j;
				}
			}
			_buckets = array;
			_entries = array2;
		}
	}

	private MiniDictionary _loops;

	private readonly object[] _baseFromTo = new object[6];

	private int _idx;

	public object GetKnownRef(object from)
	{
		object[] baseFromTo = _baseFromTo;
		if (from == baseFromTo[0])
		{
			return baseFromTo[3];
		}
		if (from == baseFromTo[1])
		{
			return baseFromTo[4];
		}
		if (from == baseFromTo[2])
		{
			return baseFromTo[5];
		}
		return _loops?.FindEntry(from);
	}

	public void AddKnownRef(object from, object to)
	{
		if (_idx < 3)
		{
			_baseFromTo[_idx] = from;
			_baseFromTo[_idx + 3] = to;
			_idx++;
			return;
		}
		if (_loops == null)
		{
			_loops = new MiniDictionary();
		}
		_loops.Insert(from, to);
	}
}
