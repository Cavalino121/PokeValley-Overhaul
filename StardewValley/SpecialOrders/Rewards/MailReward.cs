using System;
using System.Collections.Generic;
using Netcode;

namespace StardewValley.SpecialOrders.Rewards;

public class MailReward : OrderReward
{
	public NetBool noLetter = new NetBool(value: true);

	public NetStringList grantedMails = new NetStringList();

	public NetBool host = new NetBool(value: false);

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(noLetter, "noLetter").AddField(grantedMails, "grantedMails").AddField(host, "host");
	}

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		string value = order.Parse(data["MailReceived"]);
		grantedMails.AddRange(ArgUtility.SplitBySpace(value));
		if (data.TryGetValue("NoLetter", out var value2))
		{
			noLetter.Value = Convert.ToBoolean(order.Parse(value2));
		}
		if (data.TryGetValue("Host", out value2))
		{
			host.Value = Convert.ToBoolean(order.Parse(value2));
		}
	}

	public override void Grant()
	{
		foreach (string grantedMail in grantedMails)
		{
			if (host.Value)
			{
				if (!Game1.IsMasterGame)
				{
					continue;
				}
				if (Game1.newDaySync.hasInstance())
				{
					Game1.addMail(grantedMail, noLetter.Value, sendToEveryone: true);
					continue;
				}
				string text = grantedMail;
				if (text == "ClintReward" && Game1.player.mailReceived.Contains("ClintReward"))
				{
					Game1.player.mailReceived.Remove("ClintReward2");
					text = "ClintReward2";
				}
				Game1.addMailForTomorrow(text, noLetter.Value, sendToEveryone: true);
			}
			else if (Game1.newDaySync.hasInstance())
			{
				Game1.addMail(grantedMail, noLetter.Value, sendToEveryone: true);
			}
			else
			{
				string text2 = grantedMail;
				if (text2 == "ClintReward" && Game1.player.mailReceived.Contains("ClintReward"))
				{
					Game1.player.mailReceived.Remove("ClintReward2");
					text2 = "ClintReward2";
				}
				Game1.addMailForTomorrow(text2, noLetter.Value, sendToEveryone: true);
			}
		}
	}
}
