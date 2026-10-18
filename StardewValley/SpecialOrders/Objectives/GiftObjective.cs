using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;

namespace StardewValley.SpecialOrders.Objectives;

public class GiftObjective : OrderObjective
{
	public enum LikeLevels
	{
		None,
		Hated,
		Disliked,
		Neutral,
		Liked,
		Loved
	}

	[XmlElement("acceptableContextTagSets")]
	public NetStringList acceptableContextTagSets = new NetStringList();

	[XmlElement("minimumLikeLevel")]
	public NetEnum<LikeLevels> minimumLikeLevel = new NetEnum<LikeLevels>(LikeLevels.None);

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		if (data.TryGetValue("AcceptedContextTags", out var value))
		{
			acceptableContextTagSets.Add(order.Parse(value));
		}
		if (data.TryGetValue("MinimumLikeLevel", out value))
		{
			minimumLikeLevel.Value = (LikeLevels)Enum.Parse(typeof(LikeLevels), value);
		}
	}

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(acceptableContextTagSets, "acceptableContextTagSets").AddField(minimumLikeLevel, "minimumLikeLevel");
	}

	protected override void _Register()
	{
		base._Register();
		SpecialOrder order = _order;
		order.onGiftGiven = (Action<Farmer, NPC, Item>)Delegate.Combine(order.onGiftGiven, new Action<Farmer, NPC, Item>(OnGiftGiven));
	}

	protected override void _Unregister()
	{
		base._Unregister();
		SpecialOrder order = _order;
		order.onGiftGiven = (Action<Farmer, NPC, Item>)Delegate.Remove(order.onGiftGiven, new Action<Farmer, NPC, Item>(OnGiftGiven));
	}

	public virtual void OnGiftGiven(Farmer farmer, NPC npc, Item item)
	{
		bool flag = true;
		foreach (string acceptableContextTagSet in acceptableContextTagSets)
		{
			flag = false;
			bool flag2 = false;
			string[] array = acceptableContextTagSet.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				if (!ItemContextTagManager.DoAnyTagsMatch(array[i].Split('/'), item.GetContextTags()))
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		if (minimumLikeLevel.Value > LikeLevels.None)
		{
			int giftTasteForThisItem = npc.getGiftTasteForThisItem(item);
			LikeLevels likeLevels = LikeLevels.None;
			switch (giftTasteForThisItem)
			{
			case 6:
				likeLevels = LikeLevels.Hated;
				break;
			case 4:
				likeLevels = LikeLevels.Disliked;
				break;
			case 8:
				likeLevels = LikeLevels.Neutral;
				break;
			case 2:
				likeLevels = LikeLevels.Liked;
				break;
			case 0:
				likeLevels = LikeLevels.Loved;
				break;
			}
			if (likeLevels < minimumLikeLevel.Value)
			{
				return;
			}
		}
		IncrementCount(1);
	}
}
