using System;
using System.Collections;
using System.Collections.Generic;
using Netcode;

namespace StardewValley.Network;

public class NetFarmerCollection : INetObject<NetFields>, ICollection<Farmer>, IEnumerable<Farmer>, IEnumerable
{
	public delegate void FarmerEvent(Farmer f);

	private List<Farmer> farmers = new List<Farmer>();

	private NetLongDictionary<bool, NetBool> uids = new NetLongDictionary<bool, NetBool>();

	public NetFields NetFields { get; } = new NetFields("NetFarmerCollection");

	public int Count => farmers.Count;

	public bool IsReadOnly => false;

	public event FarmerEvent FarmerAdded;

	public event FarmerEvent FarmerRemoved;

	public NetFarmerCollection()
	{
		NetFields.SetOwner(this).AddField(uids, "uids");
		uids.OnValueAdded += delegate(long uid, bool _)
		{
			Farmer farmer = getFarmer(uid);
			if (farmer != null && !farmers.Contains(farmer))
			{
				farmers.Add(farmer);
				this.FarmerAdded?.Invoke(farmer);
			}
		};
		uids.OnValueRemoved += delegate(long uid, bool _)
		{
			Farmer farmer = getFarmer(uid);
			if (farmer != null)
			{
				farmers.Remove(farmer);
				this.FarmerRemoved?.Invoke(farmer);
			}
		};
	}

	private static bool playerIsOnline(long uid)
	{
		if (Game1.player.UniqueMultiplayerID != uid && (!(Game1.serverHost != null) || Game1.serverHost.Value.UniqueMultiplayerID != uid))
		{
			if (Game1.otherFarmers.ContainsKey(uid))
			{
				return !Game1.multiplayer.isDisconnecting(uid);
			}
			return false;
		}
		return true;
	}

	public bool RetainOnlinePlayers()
	{
		int length = uids.Length;
		if (length == 0)
		{
			return false;
		}
		uids.RemoveWhere((KeyValuePair<long, bool> pair) => !playerIsOnline(pair.Key));
		farmers.Clear();
		foreach (long key in uids.Keys)
		{
			Farmer farmer = getFarmer(key);
			if (farmer != null)
			{
				farmers.Add(farmer);
			}
		}
		return uids.Length < length;
	}

	private Farmer getFarmer(long uid)
	{
		foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
		{
			if (onlineFarmer.UniqueMultiplayerID == uid)
			{
				return onlineFarmer;
			}
		}
		return null;
	}

	public void Add(Farmer item)
	{
		farmers.Add(item);
		uids.TryAdd(item.UniqueMultiplayerID, value: true);
	}

	public void Clear()
	{
		farmers.Clear();
		uids.Clear();
	}

	public bool Contains(Farmer item)
	{
		return farmers.Contains(item);
	}

	public void CopyTo(Farmer[] array, int arrayIndex)
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
		using IEnumerator<Farmer> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			Farmer current = enumerator.Current;
			array[arrayIndex++] = current;
		}
	}

	public bool Remove(Farmer item)
	{
		uids.Remove(item.UniqueMultiplayerID);
		return farmers.Remove(item);
	}

	public IEnumerator<Farmer> GetEnumerator()
	{
		return farmers.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
