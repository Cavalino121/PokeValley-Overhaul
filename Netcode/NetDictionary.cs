using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Netcode;

public abstract class NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> : AbstractNetSerializable, IEquatable<TSelf>, IEnumerable<TSerialDict>, IEnumerable where TField : class, INetObject<INetSerializable>, new() where TSerialDict : IDictionary<TKey, TValue>, new() where TSelf : NetDictionary<TKey, TValue, TField, TSerialDict, TSelf>
{
	public class IncomingChange
	{
		public uint Tick;

		public bool Removal;

		public TKey Key;

		public TField Field;

		public NetVersion Reassigned;

		public IncomingChange(uint tick, bool removal, TKey key, TField field, NetVersion reassigned)
		{
			Tick = tick;
			Removal = removal;
			Key = key;
			Field = field;
			Reassigned = reassigned;
		}
	}

	public class OutgoingChange
	{
		public bool Removal;

		public TKey Key;

		public TField Field;

		public NetVersion Reassigned;

		public OutgoingChange(bool removal, TKey key, TField field, NetVersion reassigned)
		{
			Removal = removal;
			Key = key;
			Field = field;
			Reassigned = reassigned;
		}
	}

	public delegate void ContentsChangeEvent(TKey key, TValue value);

	public delegate void ConflictResolveEvent(TKey key, TField rejected, TField accepted);

	public delegate void ContentsUpdateEvent(TKey key, TValue old_target_value, TValue new_target_value);

	private delegate void ReadFunc(BinaryReader reader, NetVersion version);

	private delegate void WriteFunc<T>(BinaryWriter writer, T value);

	public struct PairsCollection : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
	{
		public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator, IDisposable
		{
			private readonly NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> _net;

			private Dictionary<TKey, TField>.Enumerator _enumerator;

			private KeyValuePair<TKey, TValue> _current;

			private bool _done;

			public KeyValuePair<TKey, TValue> Current => _current;

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

			public Enumerator(NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> net)
			{
				_net = net;
				_enumerator = _net.dict.GetEnumerator();
				_current = default(KeyValuePair<TKey, TValue>);
				_done = false;
			}

			public bool MoveNext()
			{
				if (_enumerator.MoveNext())
				{
					KeyValuePair<TKey, TField> current = _enumerator.Current;
					_current = new KeyValuePair<TKey, TValue>(current.Key, _net.getFieldValue(current.Value));
					return true;
				}
				_done = true;
				_current = default(KeyValuePair<TKey, TValue>);
				return false;
			}

			public void Dispose()
			{
			}

			void IEnumerator.Reset()
			{
				_enumerator = _net.dict.GetEnumerator();
				_current = default(KeyValuePair<TKey, TValue>);
				_done = false;
			}
		}

		private NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> _net;

		public PairsCollection(NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> net)
		{
			_net = net;
		}

		public int Count()
		{
			return _net.dict.Count;
		}

		public KeyValuePair<TKey, TValue> ElementAt(int index)
		{
			int num = 0;
			using (Enumerator enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<TKey, TValue> current = enumerator.Current;
					if (num == index)
					{
						return current;
					}
					num++;
				}
			}
			throw new ArgumentOutOfRangeException();
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(_net);
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return new Enumerator(_net);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(_net);
		}
	}

	public struct ValuesCollection : IEnumerable<TValue>, IEnumerable
	{
		public struct Enumerator : IEnumerator<TValue>, IEnumerator, IDisposable
		{
			private readonly NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> _net;

			private Dictionary<TKey, TField>.Enumerator _enumerator;

			private TValue _current;

			private bool _done;

			public TValue Current => _current;

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

			public Enumerator(NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> net)
			{
				_net = net;
				_enumerator = _net.dict.GetEnumerator();
				_current = default(TValue);
				_done = false;
			}

			public bool MoveNext()
			{
				if (_enumerator.MoveNext())
				{
					KeyValuePair<TKey, TField> current = _enumerator.Current;
					_current = _net.getFieldValue(current.Value);
					return true;
				}
				_done = true;
				_current = default(TValue);
				return false;
			}

			public void Dispose()
			{
			}

			void IEnumerator.Reset()
			{
				_enumerator = _net.dict.GetEnumerator();
				_current = default(TValue);
				_done = false;
			}
		}

		private NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> _net;

		public ValuesCollection(NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> net)
		{
			_net = net;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(_net);
		}

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			return new Enumerator(_net);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(_net);
		}
	}

	public struct KeysCollection : IEnumerable<TKey>, IEnumerable
	{
		public struct Enumerator : IEnumerator<TKey>, IEnumerator, IDisposable
		{
			private readonly Dictionary<TKey, TField> _dict;

			private Dictionary<TKey, TField>.Enumerator _enumerator;

			private TKey _current;

			private bool _done;

			public TKey Current => _current;

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

			public Enumerator(Dictionary<TKey, TField> dict)
			{
				_dict = dict;
				_enumerator = _dict.GetEnumerator();
				_current = default(TKey);
				_done = false;
			}

			public bool MoveNext()
			{
				if (_enumerator.MoveNext())
				{
					_current = _enumerator.Current.Key;
					return true;
				}
				_done = true;
				_current = default(TKey);
				return false;
			}

			public void Dispose()
			{
			}

			void IEnumerator.Reset()
			{
				_enumerator = _dict.GetEnumerator();
				_current = default(TKey);
				_done = false;
			}
		}

		private Dictionary<TKey, TField> _dict;

		public KeysCollection(Dictionary<TKey, TField> dict)
		{
			_dict = dict;
		}

		public bool Any()
		{
			return _dict.Count > 0;
		}

		public TKey First()
		{
			using (Dictionary<TKey, TField>.Enumerator enumerator = _dict.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current.Key;
				}
			}
			return default(TKey);
		}

		public bool Contains(TKey key)
		{
			return _dict.ContainsKey(key);
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(_dict);
		}

		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			return new Enumerator(_dict);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(_dict);
		}
	}

	public bool InterpolationWait = true;

	private Dictionary<TKey, TField> dict = new Dictionary<TKey, TField>();

	private Dictionary<TKey, NetVersion> dictReassigns = new Dictionary<TKey, NetVersion>();

	private List<OutgoingChange> outgoingChanges = new List<OutgoingChange>();

	private List<IncomingChange> incomingChanges = new List<IncomingChange>();

	/// <summary>The number of key/value pairs in the dictionary.</summary>
	public int Length => dict.Count;

	public bool IsReadOnly => false;

	public TValue this[TKey key]
	{
		get
		{
			return getFieldValue(dict[key]);
		}
		set
		{
			if (!dict.TryGetValue(key, out var value2))
			{
				value2 = (dict[key] = new TField());
				dictReassigns[key] = GetLocalVersion();
				setFieldValue(value2, key, value);
				added(key, value2, dictReassigns[key]);
			}
			else
			{
				setFieldValue(value2, key, value);
				addedEvent(key, value2);
			}
		}
	}

	public KeysCollection Keys => new KeysCollection(dict);

	public ValuesCollection Values => new ValuesCollection(this);

	public PairsCollection Pairs => new PairsCollection(this);

	public Dictionary<TKey, TField> FieldDict => dict;

	public event ContentsChangeEvent OnValueAdded;

	public event ContentsChangeEvent OnValueRemoved;

	public event ContentsUpdateEvent OnValueTargetUpdated;

	public event ConflictResolveEvent OnConflictResolve;

	public bool Any()
	{
		return dict.Count > 0;
	}

	public NetDictionary()
	{
	}

	public NetDictionary(IEnumerable<KeyValuePair<TKey, TValue>> dict)
		: this()
	{
		CopyFrom(dict);
	}

	protected override bool tickImpl()
	{
		List<IncomingChange> list = null;
		foreach (IncomingChange incomingChange in incomingChanges)
		{
			if (base.Root == null || GetLocalTick() >= incomingChange.Tick)
			{
				if (list == null)
				{
					list = new List<IncomingChange>();
				}
				list.Add(incomingChange);
				continue;
			}
			break;
		}
		if (list != null && list.Count > 0)
		{
			foreach (IncomingChange item in list)
			{
				incomingChanges.Remove(item);
			}
			foreach (IncomingChange item2 in list)
			{
				if (item2.Removal)
				{
					performIncomingRemove(item2);
				}
				else
				{
					performIncomingAdd(item2);
				}
			}
		}
		return incomingChanges.Count > 0;
	}

	protected abstract void setFieldValue(TField field, TKey key, TValue value);

	protected abstract TValue getFieldValue(TField field);

	protected abstract TValue getFieldTargetValue(TField field);

	protected TField createField(TKey key, TValue value)
	{
		TField val = new TField();
		setFieldValue(val, key, value);
		return val;
	}

	public void CopyFrom(IEnumerable<KeyValuePair<TKey, TValue>> dict)
	{
		foreach (KeyValuePair<TKey, TValue> item in dict)
		{
			this[item.Key] = item.Value;
		}
	}

	public void Set(IEnumerable<KeyValuePair<TKey, TValue>> dict)
	{
		Clear();
		CopyFrom(dict);
	}

	public void MoveFrom(TSelf dict)
	{
		List<KeyValuePair<TKey, TValue>> list = new List<KeyValuePair<TKey, TValue>>(dict.Pairs);
		dict.Clear();
		Set(list);
	}

	public void SetEqualityComparer(IEqualityComparer<TKey> comparer)
	{
		dict = new Dictionary<TKey, TField>(dict, comparer);
		dictReassigns = new Dictionary<TKey, NetVersion>(dictReassigns, comparer);
	}

	private void setFieldParent(TField arg)
	{
		if (base.Parent != null)
		{
			arg.NetFields.Parent = this;
		}
	}

	private void added(TKey key, TField field, NetVersion reassign)
	{
		outgoingChanges.Add(new OutgoingChange(removal: false, key, field, reassign));
		setFieldParent(field);
		MarkDirty();
		addedEvent(key, field);
		foreach (IncomingChange incomingChange in incomingChanges)
		{
			if (!incomingChange.Removal && object.Equals(incomingChange.Key, key))
			{
				clearFieldParent(incomingChange.Field);
				if (this.OnConflictResolve != null)
				{
					this.OnConflictResolve(key, incomingChange.Field, field);
				}
			}
		}
		incomingChanges.RemoveAll((IncomingChange change) => object.Equals(key, change.Key));
	}

	private void addedEvent(TKey key, TField field)
	{
		if (this.OnValueAdded != null)
		{
			this.OnValueAdded(key, getFieldValue(field));
		}
	}

	private void updatedEvent(TKey key, TValue old_target_value, TValue new_target_value)
	{
		if (this.OnValueTargetUpdated != null)
		{
			this.OnValueTargetUpdated(key, old_target_value, new_target_value);
		}
	}

	private void clearFieldParent(TField arg)
	{
		if (arg.NetFields.Parent == this)
		{
			arg.NetFields.Parent = null;
		}
	}

	private void removed(TKey key, TField field, NetVersion reassign)
	{
		outgoingChanges.Add(new OutgoingChange(removal: true, key, field, reassign));
		clearFieldParent(field);
		MarkDirty();
		removedEvent(key, field);
	}

	private void removedEvent(TKey key, TField field)
	{
		if (this.OnValueRemoved != null)
		{
			this.OnValueRemoved(key, getFieldValue(field));
		}
	}

	/// <summary>Add an entry to the dictionary.</summary>
	/// <param name="key">The key of the element to add.</param>
	/// <param name="value">The value of the element to add.</param>
	/// <exception cref="T:System.ArgumentException">The key is already present in the dictionary.</exception>
	public void Add(TKey key, TValue value)
	{
		TField field = createField(key, value);
		Add(key, field);
	}

	/// <summary>Add an entry to the dictionary.</summary>
	/// <param name="key">The key of the element to add.</param>
	/// <param name="field">The net field to add.</param>
	/// <exception cref="T:System.ArgumentException">The key is already present in the dictionary.</exception>
	public void Add(TKey key, TField field)
	{
		dict.Add(key, field);
		dictReassigns.Add(key, GetLocalVersion());
		added(key, field, dictReassigns[key]);
	}

	/// <summary>Add an entry to the dictionary if the key isn't already present.</summary>
	/// <param name="key">The key of the element to add.</param>
	/// <param name="value">The value of the element to add.</param>
	/// <returns>Returns whether the value was successfully added.</returns>
	public bool TryAdd(TKey key, TValue value)
	{
		if (dict.ContainsKey(key))
		{
			return false;
		}
		TField field = createField(key, value);
		Add(key, field);
		return true;
	}

	public void Clear()
	{
		KeysCollection keys = Keys;
		while (keys.Any())
		{
			Remove(keys.First());
		}
		outgoingChanges.RemoveAll((OutgoingChange ch) => !ch.Removal);
	}

	public bool ContainsKey(TKey key)
	{
		return dict.ContainsKey(key);
	}

	public int Count()
	{
		return dict.Count;
	}

	public bool Remove(TKey key)
	{
		if (dict.TryGetValue(key, out var value))
		{
			NetVersion reassign = dictReassigns[key];
			dict.Remove(key);
			dictReassigns.Remove(key);
			removed(key, value, reassign);
			return true;
		}
		return false;
	}

	/// <summary>Remove all elements that match a condition.</summary>
	/// <param name="match">The predicate matching values to remove.</param>
	public int RemoveWhere(Func<KeyValuePair<TKey, TValue>, bool> match)
	{
		if (dict.Count == 0)
		{
			return 0;
		}
		int num = 0;
		foreach (KeyValuePair<TKey, TValue> pair in Pairs)
		{
			if (match(pair))
			{
				Remove(pair.Key);
				num++;
			}
		}
		return num;
	}

	[Obsolete("Use RemoveWhere instead.")]
	public void Filter(Func<KeyValuePair<TKey, TValue>, bool> f)
	{
		RemoveWhere((KeyValuePair<TKey, TValue> pair) => !f(pair));
	}

	/// <summary>Try to get the value associated with a specified key.</summary>
	/// <param name="key">The key of the element to find.</param>
	/// <param name="value">The value that was found, or the default value if none was found.</param>
	/// <returns>Returns whether a value was found.</returns>
	public bool TryGetValue(TKey key, out TValue value)
	{
		if (dict.TryGetValue(key, out var value2))
		{
			value = getFieldValue(value2);
			return true;
		}
		value = default(TValue);
		return false;
	}

	/// <summary>Get the value associated with a specified key, or the default value if none was found.</summary>
	/// <param name="key">The key of the element to find.</param>
	/// <param name="defaultValue">The value to return if the element isn't found.</param>
	public TValue GetValueOrDefault(TKey key, TValue defaultValue = default(TValue))
	{
		if (!dict.TryGetValue(key, out var value))
		{
			return defaultValue;
		}
		return getFieldValue(value);
	}

	public bool Equals(TSelf other)
	{
		return object.Equals(dict, other.dict);
	}

	protected override void CleanImpl()
	{
		base.CleanImpl();
		outgoingChanges.Clear();
	}

	protected abstract TKey ReadKey(BinaryReader reader);

	protected abstract void WriteKey(BinaryWriter writer, TKey key);

	private void readMultiple(ReadFunc readFunc, BinaryReader reader, NetVersion version)
	{
		uint num = reader.Read7BitEncoded();
		for (uint num2 = 0u; num2 < num; num2++)
		{
			readFunc(reader, version);
		}
	}

	private void writeMultiple<T>(WriteFunc<T> writeFunc, BinaryWriter writer, IEnumerable<T> values)
	{
		writer.Write7BitEncoded((uint)values.Count());
		foreach (T value in values)
		{
			writeFunc(writer, value);
		}
	}

	protected virtual TField ReadFieldFull(BinaryReader reader, NetVersion version)
	{
		TField val = new TField();
		val.NetFields.ReadFull(reader, version);
		return val;
	}

	protected virtual void WriteFieldFull(BinaryWriter writer, TField field)
	{
		field.NetFields.WriteFull(writer);
	}

	private void readAddition(BinaryReader reader, NetVersion version)
	{
		TKey key = ReadKey(reader);
		NetVersion fieldReassign = default(NetVersion);
		fieldReassign.Read(reader);
		TField val = ReadFieldFull(reader, version);
		setFieldParent(val);
		queueIncomingChange(removal: false, key, val, fieldReassign);
	}

	protected virtual bool resolveConflict(TKey key, TField currentField, NetVersion currentReassign, TField incomingField, NetVersion incomingReassign)
	{
		if (incomingReassign.IsPriorityOver(currentReassign))
		{
			clearFieldParent(currentField);
			if (this.OnConflictResolve != null)
			{
				this.OnConflictResolve(key, currentField, incomingField);
			}
			return true;
		}
		clearFieldParent(incomingField);
		if (this.OnConflictResolve != null)
		{
			this.OnConflictResolve(key, incomingField, currentField);
		}
		return false;
	}

	private KeyValuePair<NetVersion, TField>? findConflict(TKey key)
	{
		foreach (IncomingChange item in incomingChanges.AsEnumerable().Reverse())
		{
			if (object.Equals(item.Key, key))
			{
				if (item.Removal)
				{
					return null;
				}
				return new KeyValuePair<NetVersion, TField>(item.Reassigned, item.Field);
			}
		}
		if (dict.TryGetValue(key, out var value))
		{
			return new KeyValuePair<NetVersion, TField>(dictReassigns[key], value);
		}
		return null;
	}

	private void queueIncomingChange(bool removal, TKey key, TField field, NetVersion fieldReassign)
	{
		if (!removal)
		{
			KeyValuePair<NetVersion, TField>? keyValuePair = findConflict(key);
			if (keyValuePair.HasValue && !resolveConflict(key, keyValuePair.Value.Value, keyValuePair.Value.Key, field, fieldReassign))
			{
				return;
			}
		}
		uint tick = GetLocalTick() + (uint)((InterpolationWait && base.Root != null) ? base.Root.Clock.InterpolationTicks : 0);
		incomingChanges.Add(new IncomingChange(tick, removal, key, field, fieldReassign));
		base.NeedsTick = true;
	}

	private void performIncomingAdd(IncomingChange add)
	{
		dict[add.Key] = add.Field;
		dictReassigns[add.Key] = add.Reassigned;
		addedEvent(add.Key, add.Field);
	}

	private void readRemoval(BinaryReader reader, NetVersion version)
	{
		TKey key = ReadKey(reader);
		NetVersion fieldReassign = default(NetVersion);
		fieldReassign.Read(reader);
		queueIncomingChange(removal: true, key, null, fieldReassign);
	}

	private void readDictChange(BinaryReader reader, NetVersion version)
	{
		if (reader.ReadByte() != 0)
		{
			readRemoval(reader, version);
		}
		else
		{
			readAddition(reader, version);
		}
	}

	private void performIncomingRemove(IncomingChange remove)
	{
		if (dict.TryGetValue(remove.Key, out var value))
		{
			clearFieldParent(value);
			dict.Remove(remove.Key);
			dictReassigns.Remove(remove.Key);
			removedEvent(remove.Key, value);
		}
	}

	private void readUpdate(BinaryReader reader, NetVersion version)
	{
		TKey key = ReadKey(reader);
		NetVersion reassign = default(NetVersion);
		reassign.Read(reader);
		reader.ReadSkippable(delegate
		{
			int num = incomingChanges.FindLastIndex((IncomingChange ch) => !ch.Removal && object.Equals(ch.Key, key) && reassign.Equals(ch.Reassigned));
			TField value;
			if (num != -1)
			{
				TField field = incomingChanges[num].Field;
				if (this.OnValueTargetUpdated != null)
				{
					TValue fieldTargetValue = getFieldTargetValue(field);
					field.NetFields.Read(reader, version);
					updatedEvent(key, fieldTargetValue, getFieldTargetValue(field));
				}
				else
				{
					field.NetFields.Read(reader, version);
				}
			}
			else if (dict.TryGetValue(key, out value) && dictReassigns[key].Equals(reassign))
			{
				if (this.OnValueTargetUpdated != null)
				{
					TValue fieldTargetValue2 = getFieldTargetValue(value);
					value.NetFields.Read(reader, version);
					updatedEvent(key, fieldTargetValue2, getFieldTargetValue(value));
				}
				else
				{
					value.NetFields.Read(reader, version);
				}
			}
		});
	}

	public override void Read(BinaryReader reader, NetVersion version)
	{
		readMultiple(readDictChange, reader, version);
		readMultiple(readUpdate, reader, version);
	}

	public override void ReadFull(BinaryReader reader, NetVersion version)
	{
		dict.Clear();
		dictReassigns.Clear();
		outgoingChanges.Clear();
		incomingChanges.Clear();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			TKey key = ReadKey(reader);
			NetVersion value = default(NetVersion);
			value.Read(reader);
			TField val = ReadFieldFull(reader, version);
			dict.Add(key, val);
			dictReassigns.Add(key, value);
			setFieldParent(val);
			addedEvent(key, val);
		}
	}

	private void writeAddition(BinaryWriter writer, OutgoingChange update)
	{
		WriteKey(writer, update.Key);
		update.Reassigned.Write(writer);
		WriteFieldFull(writer, update.Field);
	}

	private void writeRemoval(BinaryWriter writer, OutgoingChange update)
	{
		WriteKey(writer, update.Key);
		update.Reassigned.Write(writer);
	}

	private void writeDictChange(BinaryWriter writer, OutgoingChange ch)
	{
		if (ch.Removal)
		{
			writer.Write((byte)1);
			writeRemoval(writer, ch);
		}
		else
		{
			writer.Write((byte)0);
			writeAddition(writer, ch);
		}
	}

	private void writeUpdate(BinaryWriter writer, OutgoingChange update)
	{
		WriteKey(writer, update.Key);
		update.Reassigned.Write(writer);
		writer.WriteSkippable(delegate
		{
			update.Field.NetFields.Write(writer);
		});
	}

	private IEnumerable<OutgoingChange> updates()
	{
		foreach (KeyValuePair<TKey, TField> item in dict)
		{
			if (item.Value.NetFields.Dirty)
			{
				yield return new OutgoingChange(removal: false, item.Key, item.Value, dictReassigns[item.Key]);
			}
		}
		foreach (OutgoingChange item2 in outgoingChanges.Where((OutgoingChange ch) => ch.Removal))
		{
			if (item2.Field.NetFields.Dirty)
			{
				yield return item2;
			}
		}
	}

	public override void Write(BinaryWriter writer)
	{
		writeMultiple(writeDictChange, writer, outgoingChanges);
		writeMultiple(writeUpdate, writer, updates());
	}

	public override void WriteFull(BinaryWriter writer)
	{
		writer.Write(Length);
		foreach (TKey key in dict.Keys)
		{
			WriteKey(writer, key);
			dictReassigns[key].Write(writer);
			WriteFieldFull(writer, dict[key]);
		}
	}

	public IEnumerator<TSerialDict> GetEnumerator()
	{
		TSerialDict item = new TSerialDict();
		foreach (KeyValuePair<TKey, TField> item2 in dict)
		{
			item.Add(item2.Key, getFieldValue(item2.Value));
		}
		return new List<TSerialDict> { item }.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	protected override void ForEachChild(Action<INetSerializable> childAction)
	{
		foreach (IncomingChange incomingChange in incomingChanges)
		{
			if (incomingChange.Field != null)
			{
				childAction(incomingChange.Field.NetFields);
			}
		}
		foreach (TField value in dict.Values)
		{
			childAction(value.NetFields);
		}
	}

	public void Add(TSerialDict dict)
	{
		Set(dict);
	}

	protected override void ValidateChildren()
	{
		if ((base.Parent != null || base.Root == this) && !base.NeedsTick)
		{
			ForEachChild(ValidateChild);
		}
	}
}
