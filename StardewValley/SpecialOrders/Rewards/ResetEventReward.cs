using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;

namespace StardewValley.SpecialOrders.Rewards;

public class ResetEventReward : OrderReward
{
	[XmlArrayItem("int")]
	public NetStringList resetEvents = new NetStringList();

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(resetEvents, "resetEvents");
	}

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		string value = order.Parse(data["ResetEvents"]);
		resetEvents.AddRange(ArgUtility.SplitBySpace(value));
	}

	public override void Grant()
	{
		foreach (string resetEvent in resetEvents)
		{
			Game1.player.eventsSeen.Remove(resetEvent);
		}
	}
}
