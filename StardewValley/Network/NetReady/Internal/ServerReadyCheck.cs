using System.Collections.Generic;

namespace StardewValley.Network.NetReady.Internal;

/// <summary>A cancelable ready-check for the host player.</summary>
internal sealed class ServerReadyCheck : BaseReadyCheck
{
	/// <summary>The ready states for all farmers required by this ready check.</summary>
	private readonly Dictionary<long, ReadyState> ReadyStates = new Dictionary<long, ReadyState>();

	/// <summary>Whether we're currently attempting to lock all clients.</summary>
	private bool Locking;

	/// <summary>All farmers that should be included in this check.</summary>
	private readonly HashSet<long> RequiredFarmers = new HashSet<long>();

	/// <summary>Whether all farmers (including those that recently joined) should be included in this check.</summary>
	private bool IncludesAll => RequiredFarmers.Count == 0;

	/// <inheritdoc />
	public ServerReadyCheck(string id)
		: base(id)
	{
	}

	/// <inheritdoc />
	public override void SetRequiredFarmers(List<long> farmerIds)
	{
		RequireFarmers(farmerIds);
	}

	/// <inheritdoc />
	public override bool SetLocalReady(bool ready)
	{
		if (!base.SetLocalReady(ready))
		{
			return false;
		}
		if (!IsFarmerRequired(Game1.player.UniqueMultiplayerID))
		{
			base.State = ReadyState.NotReady;
			return false;
		}
		ReadyStates[Game1.player.UniqueMultiplayerID] = base.State;
		return true;
	}

	/// <inheritdoc />
	public override void Update()
	{
		if (base.IsReady)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		bool flag = IsFarmerRequired(Game1.player.UniqueMultiplayerID);
		foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
		{
			if (IsFarmerRequired(onlineFarmer.UniqueMultiplayerID) && !Game1.multiplayer.isDisconnecting(onlineFarmer))
			{
				if (!ReadyStates.TryGetValue(onlineFarmer.UniqueMultiplayerID, out var value))
				{
					value = ReadyState.NotReady;
					ReadyStates[onlineFarmer.UniqueMultiplayerID] = value;
				}
				num2++;
				switch (value)
				{
				case ReadyState.Ready:
					num++;
					break;
				case ReadyState.Locked:
					num++;
					num3++;
					break;
				}
			}
		}
		if (num != base.NumberReady || num2 != base.NumberRequired)
		{
			if (flag && Game1.IsDedicatedHost)
			{
				SendMessage(ReadyCheckMessageType.UpdateAmounts, num - ((base.State == ReadyState.Ready) ? 1 : 0), num2 - 1);
			}
			else
			{
				SendMessage(ReadyCheckMessageType.UpdateAmounts, num, num2);
			}
			if (num == num2)
			{
				if (!Locking)
				{
					base.ActiveLockId++;
					Locking = true;
					if (flag && base.State == ReadyState.Ready)
					{
						Dictionary<long, ReadyState> readyStates = ReadyStates;
						long uniqueMultiplayerID = Game1.player.UniqueMultiplayerID;
						ReadyState value2 = (base.State = ReadyState.Locked);
						readyStates[uniqueMultiplayerID] = value2;
						num3 = 1;
					}
					SendMessage(ReadyCheckMessageType.Lock, base.ActiveLockId);
				}
			}
			else if (Locking)
			{
				Locking = false;
				if (base.State == ReadyState.Locked)
				{
					base.State = ReadyState.Ready;
				}
				foreach (long key in ReadyStates.Keys)
				{
					if (ReadyStates[key] == ReadyState.Locked && IsFarmerRequired(key))
					{
						ReadyStates[key] = ReadyState.Ready;
					}
				}
				num3 = 0;
				SendMessage(ReadyCheckMessageType.Release, base.ActiveLockId);
			}
		}
		if (Locking && num3 == num2)
		{
			base.IsReady = true;
			SendMessage(ReadyCheckMessageType.Finish);
		}
		base.NumberReady = num;
		base.NumberRequired = num2;
	}

	/// <inheritdoc />
	public override void ProcessMessage(ReadyCheckMessageType messageType, IncomingMessage message)
	{
		switch (messageType)
		{
		case ReadyCheckMessageType.Ready:
			ProcessReady(message);
			return;
		case ReadyCheckMessageType.Cancel:
			ProcessCancel(message);
			return;
		case ReadyCheckMessageType.AcceptLock:
			ProcessAcceptLock(message);
			return;
		case ReadyCheckMessageType.RejectLock:
			ProcessRejectLock(message);
			return;
		case ReadyCheckMessageType.RequireFarmers:
			ProcessRequireFarmers(message);
			return;
		}
		Game1.log.Warn($"{"ServerReadyCheck"} '{base.Id}' received invalid message type '{messageType}'.");
	}

	/// <inheritdoc />
	protected override void SendMessage(ReadyCheckMessageType messageType, params object[] data)
	{
		if (Game1.server == null)
		{
			return;
		}
		foreach (Farmer value in Game1.otherFarmers.Values)
		{
			Game1.server.sendMessage(value.UniqueMultiplayerID, CreateSyncMessage(messageType, data));
		}
	}

	/// <summary>Handle a request to mark a farmer's state as ready.</summary>
	/// <param name="message">The incoming <see cref="F:StardewValley.Network.NetReady.Internal.ReadyCheckMessageType.Ready" /> message.</param>
	private void ProcessReady(IncomingMessage message)
	{
		if (!Locking)
		{
			ReadyStates[message.FarmerID] = ReadyState.Ready;
		}
	}

	/// <summary>Handle a request to mark a farmer as non-ready.</summary>
	/// <param name="message">The incoming <see cref="F:StardewValley.Network.NetReady.Internal.ReadyCheckMessageType.Cancel" /> message.</param>
	private void ProcessCancel(IncomingMessage message)
	{
		if (!Locking)
		{
			ReadyStates[message.FarmerID] = ReadyState.NotReady;
		}
	}

	/// <summary>Handle a request to mark a farmer as locked.</summary>
	/// <param name="message">The incoming <see cref="F:StardewValley.Network.NetReady.Internal.ReadyCheckMessageType.AcceptLock" /> message.</param>
	private void ProcessAcceptLock(IncomingMessage message)
	{
		if (message.Reader.ReadInt32() == base.ActiveLockId)
		{
			ReadyStates[message.FarmerID] = ReadyState.Locked;
		}
	}

	/// <summary>Handle a request to mark a farmer as not ready to lock.</summary>
	/// <param name="message">The incoming <see cref="F:StardewValley.Network.NetReady.Internal.ReadyCheckMessageType.RejectLock" /> message.</param>
	private void ProcessRejectLock(IncomingMessage message)
	{
		if (message.Reader.ReadInt32() == base.ActiveLockId)
		{
			ReadyStates[message.FarmerID] = ReadyState.NotReady;
		}
	}

	/// <summary>Handle a request to set the required farmers for this check.</summary>
	/// <param name="message">The incoming <see cref="F:StardewValley.Network.NetReady.Internal.ReadyCheckMessageType.RequireFarmers" /> message.</param>
	private void ProcessRequireFarmers(IncomingMessage message)
	{
		int num = message.Reader.ReadInt32();
		HashSet<long> hashSet = new HashSet<long>();
		for (int i = 0; i < num; i++)
		{
			hashSet.Add(message.Reader.ReadInt64());
		}
		RequireFarmers(hashSet);
	}

	/// <summary>Update the required farmers in <see cref="F:StardewValley.Network.NetReady.Internal.ServerReadyCheck.ReadyStates" /> to be the set of <paramref name="farmerIds" />.</summary>
	/// <param name="farmerIds">The list of farmer multiplayer IDs that should be required for this check.</param>
	private void RequireFarmers(ICollection<long> farmerIds)
	{
		RequiredFarmers.Clear();
		if (farmerIds == null)
		{
			return;
		}
		foreach (long farmerId in farmerIds)
		{
			RequiredFarmers.Add(farmerId);
		}
	}

	/// <summary>Checks if a farmer is required for this ready check to pass.</summary>
	/// <param name="uid">The unique multiplayer ID of the farmer to check.</param>
	private bool IsFarmerRequired(long uid)
	{
		if (!IncludesAll)
		{
			return RequiredFarmers.Contains(uid);
		}
		return true;
	}
}
