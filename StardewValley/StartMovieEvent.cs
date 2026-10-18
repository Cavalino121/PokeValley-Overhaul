using System.Collections.Generic;
using System.IO;
using Netcode;

namespace StardewValley;

public class StartMovieEvent : NetEventArg
{
	public long uid;

	public List<List<Character>> playerGroups;

	public List<List<Character>> npcGroups;

	public StartMovieEvent()
	{
	}

	public StartMovieEvent(long farmer_uid, List<List<Character>> player_groups, List<List<Character>> npc_groups)
	{
		uid = farmer_uid;
		playerGroups = player_groups;
		npcGroups = npc_groups;
	}

	public void Read(BinaryReader reader)
	{
		uid = reader.ReadInt64();
		playerGroups = ReadCharacterList(reader);
		npcGroups = ReadCharacterList(reader);
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(uid);
		WriteCharacterList(writer, playerGroups);
		WriteCharacterList(writer, npcGroups);
	}

	public List<List<Character>> ReadCharacterList(BinaryReader reader)
	{
		List<List<Character>> list = new List<List<Character>>();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			List<Character> list2 = new List<Character>();
			int num2 = reader.ReadInt32();
			for (int j = 0; j < num2; j++)
			{
				Character item = ((reader.ReadInt32() == 1) ? ((Character)(Game1.GetPlayer(reader.ReadInt64(), onlyOnline: true) ?? Game1.MasterPlayer)) : ((Character)Game1.getCharacterFromName(reader.ReadString())));
				list2.Add(item);
			}
			list.Add(list2);
		}
		return list;
	}

	public void WriteCharacterList(BinaryWriter writer, List<List<Character>> group_list)
	{
		writer.Write(group_list.Count);
		foreach (List<Character> item in group_list)
		{
			writer.Write(item.Count);
			foreach (Character item2 in item)
			{
				if (item2 is Farmer farmer)
				{
					writer.Write(1);
					writer.Write(farmer.UniqueMultiplayerID);
				}
				else
				{
					writer.Write(0);
					writer.Write(item2.Name);
				}
			}
		}
	}
}
