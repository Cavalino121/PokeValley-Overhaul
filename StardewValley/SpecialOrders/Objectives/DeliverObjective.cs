using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;

namespace StardewValley.SpecialOrders.Objectives;

public class DeliverObjective : OrderObjective
{
	[XmlElement("acceptableContextTagSets")]
	public NetStringList acceptableContextTagSets = new NetStringList();

	[XmlElement("targetName")]
	public NetString targetName = new NetString();

	[XmlElement("message")]
	public NetString message = new NetString();

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		if (data.TryGetValue("AcceptedContextTags", out var value))
		{
			acceptableContextTagSets.Add(order.Parse(value));
		}
		if (data.TryGetValue("TargetName", out value))
		{
			targetName.Value = order.Parse(value);
		}
		else
		{
			targetName.Value = _order.requester.Value;
		}
		if (data.TryGetValue("Message", out value))
		{
			message.Value = order.Parse(value);
		}
		else
		{
			message.Value = "";
		}
	}

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(acceptableContextTagSets, "acceptableContextTagSets").AddField(targetName, "targetName").AddField(message, "message");
	}

	public override bool ShouldShowProgress()
	{
		return false;
	}

	protected override void _Register()
	{
		base._Register();
		SpecialOrder order = _order;
		order.onItemDelivered = (Func<Farmer, NPC, Item, bool, int>)Delegate.Combine(order.onItemDelivered, new Func<Farmer, NPC, Item, bool, int>(OnItemDelivered));
	}

	protected override void _Unregister()
	{
		base._Unregister();
		SpecialOrder order = _order;
		order.onItemDelivered = (Func<Farmer, NPC, Item, bool, int>)Delegate.Remove(order.onItemDelivered, new Func<Farmer, NPC, Item, bool, int>(OnItemDelivered));
	}

	public virtual int OnItemDelivered(Farmer farmer, NPC npc, Item item, bool probe)
	{
		if (IsComplete())
		{
			return 0;
		}
		if (npc.Name != targetName.Value)
		{
			return 0;
		}
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
			return 0;
		}
		int num = GetMaxCount() - GetCount();
		int num2 = Math.Min(item.Stack, num);
		if (num2 < num)
		{
			return 0;
		}
		if (!probe)
		{
			Item one = item.getOne();
			one.Stack = num2;
			_order.donatedItems.Add(one);
			item.Stack -= num2;
			IncrementCount(num2);
			if (!string.IsNullOrEmpty(message.Value))
			{
				npc.CurrentDialogue.Push(new Dialogue(npc, null, message.Value));
				Game1.drawDialogue(npc);
			}
		}
		return num2;
	}
}
