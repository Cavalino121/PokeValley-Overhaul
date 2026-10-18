using System.Collections.Generic;
using System.IO;
using Netcode;
using StardewValley.Network;

namespace StardewValley;

public abstract class NetSynchronizer
{
	private const byte MessageTypeVar = 0;

	private const byte MessageTypeBarrier = 1;

	private Dictionary<string, INetObject<INetSerializable>> variables = new Dictionary<string, INetObject<INetSerializable>>();

	private Dictionary<string, HashSet<long>> barriers = new Dictionary<string, HashSet<long>>();

	protected void reset()
	{
		variables.Clear();
		barriers.Clear();
	}

	private HashSet<long> barrierPlayers(string name)
	{
		if (!barriers.TryGetValue(name, out var value))
		{
			value = (barriers[name] = new HashSet<long>());
		}
		return value;
	}

	private bool barrierReady(string name)
	{
		HashSet<long> hashSet = barrierPlayers(name);
		foreach (long key in Game1.otherFarmers.Keys)
		{
			if (!hashSet.Contains(key))
			{
				return false;
			}
		}
		return true;
	}

	protected bool shouldAbort()
	{
		if (Game1.client != null)
		{
			return Game1.client.timedOut;
		}
		return false;
	}

	public void barrier(string name)
	{
		barrierPlayers(name).Add(Game1.player.UniqueMultiplayerID);
		Game1.multiplayer.UpdateLate();
		sendMessage((byte)1, name);
		while (!barrierReady(name))
		{
			processMessages();
			if (shouldAbort())
			{
				throw new AbortNetSynchronizerException();
			}
			if (LocalMultiplayer.IsLocalMultiplayer())
			{
				return;
			}
		}
		Game1.hooks.AfterNewDayBarrier(name);
	}

	public bool isBarrierReady(string name)
	{
		if (!barrierReady(name))
		{
			processMessages();
			if (shouldAbort())
			{
				throw new AbortNetSynchronizerException();
			}
			return false;
		}
		return true;
	}

	public bool isVarReady(string varName)
	{
		if (!variables.ContainsKey(varName))
		{
			processMessages();
			if (shouldAbort())
			{
				throw new AbortNetSynchronizerException();
			}
			LocalMultiplayer.IsLocalMultiplayer();
			return false;
		}
		return true;
	}

	public T waitForVar<TField, T>(string varName) where TField : NetFieldBase<T, TField>, new()
	{
		while (!variables.ContainsKey(varName))
		{
			processMessages();
			if (shouldAbort())
			{
				throw new AbortNetSynchronizerException();
			}
		}
		return (variables[varName] as TField).Value;
	}

	public void sendVar<TField, T>(string varName, T value) where TField : NetFieldBase<T, TField>, new()
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter writer = new BinaryWriter(memoryStream);
		NetRoot<TField> netRoot = new NetRoot<TField>(new TField());
		netRoot.Value.Value = value;
		netRoot.WriteFull(writer);
		variables[varName] = netRoot.Value;
		memoryStream.Seek(0L, SeekOrigin.Begin);
		sendMessage((byte)0, varName, memoryStream.ToArray());
	}

	public bool hasVar(string varName)
	{
		return variables.ContainsKey(varName);
	}

	public abstract void processMessages();

	protected abstract void sendMessage(params object[] data);

	public void receiveMessage(IncomingMessage msg)
	{
		switch (msg.Reader.ReadByte())
		{
		case 0:
		{
			string key = msg.Reader.ReadString();
			NetRoot<INetObject<INetSerializable>> netRoot = new NetRoot<INetObject<INetSerializable>>();
			netRoot.ReadFull(msg.Reader, default(NetVersion));
			variables[key] = netRoot.Value;
			break;
		}
		case 1:
		{
			string name = msg.Reader.ReadString();
			barrierPlayers(name).Add(msg.FarmerID);
			break;
		}
		}
	}
}
