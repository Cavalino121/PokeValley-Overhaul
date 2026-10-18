using System;
using Netcode;

namespace StardewValley.Network;

public class NetCharacterRef : INetObject<NetFields>
{
	private readonly NetNPCRef npc = new NetNPCRef();

	private readonly NetFarmerRef farmer = new NetFarmerRef();

	public NetFields NetFields { get; } = new NetFields("NetCharacterRef");

	public NetCharacterRef()
	{
		NetFields.SetOwner(this).AddField(npc.NetFields, "npc.NetFields").AddField(farmer.NetFields, "farmer.NetFields");
	}

	public Character Get(GameLocation location)
	{
		NPC nPC = npc.Get(location);
		if (nPC != null)
		{
			return nPC;
		}
		return farmer.Value;
	}

	public void Set(GameLocation location, Character character)
	{
		if (!(character is NPC nPC))
		{
			if (!(character is Farmer value))
			{
				throw new ArgumentException();
			}
			npc.Clear();
			farmer.Value = value;
		}
		else
		{
			npc.Set(location, nPC);
			farmer.Value = null;
		}
	}

	public void Clear()
	{
		npc.Clear();
		farmer.Value = null;
	}
}
