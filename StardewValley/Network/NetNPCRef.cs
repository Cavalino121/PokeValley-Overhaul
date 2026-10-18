using System;
using Netcode;

namespace StardewValley.Network;

public class NetNPCRef : INetObject<NetFields>
{
	private readonly NetGuid guid = new NetGuid();

	public NetFields NetFields { get; } = new NetFields("NetNPCRef");

	public NetNPCRef()
	{
		NetFields.SetOwner(this).AddField(guid, "guid");
	}

	public NPC Get(GameLocation location)
	{
		if (!(guid.Value != Guid.Empty) || location == null || !location.characters.TryGetValue(guid.Value, out var value))
		{
			return null;
		}
		return value;
	}

	public void Set(GameLocation location, NPC npc)
	{
		if (npc == null)
		{
			this.guid.Value = Guid.Empty;
			return;
		}
		Guid guid = location.characters.GuidOf(npc);
		if (guid == Guid.Empty)
		{
			throw new ArgumentException();
		}
		this.guid.Value = guid;
	}

	public void Clear()
	{
		guid.Value = Guid.Empty;
	}
}
