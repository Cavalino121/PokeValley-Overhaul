using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;

namespace StardewValley.SpecialOrders.Rewards;

public class FriendshipReward : OrderReward
{
	[XmlElement("targetName")]
	public NetString targetName = new NetString();

	[XmlElement("amount")]
	public NetInt amount = new NetInt();

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(targetName, "targetName").AddField(amount, "amount");
	}

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		if (!data.TryGetValue("TargetName", out var value))
		{
			value = order.requester.Value;
		}
		value = order.Parse(value);
		targetName.Value = value;
		string valueOrDefault = data.GetValueOrDefault("Amount", "250");
		valueOrDefault = order.Parse(valueOrDefault);
		amount.Value = int.Parse(valueOrDefault);
	}

	public override void Grant()
	{
		NPC characterFromName = Game1.getCharacterFromName(targetName.Value);
		if (characterFromName != null)
		{
			Game1.player.changeFriendship(amount.Value, characterFromName);
		}
	}
}
