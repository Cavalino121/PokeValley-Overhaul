using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Objects;

namespace StardewValley.SpecialOrders.Objectives;

public class DonateObjective : OrderObjective
{
	[XmlElement("dropBox")]
	public NetString dropBox = new NetString();

	[XmlElement("dropBoxGameLocation")]
	public NetString dropBoxGameLocation = new NetString();

	[XmlElement("dropBoxTileLocation")]
	public NetVector2 dropBoxTileLocation = new NetVector2();

	[XmlElement("acceptableContextTagSets")]
	public NetStringList acceptableContextTagSets = new NetStringList();

	[XmlElement("minimumCapacity")]
	public NetInt minimumCapacity = new NetInt(-1);

	[XmlElement("confirmed")]
	public NetBool confirmed = new NetBool(value: false);

	public virtual string GetDropboxLocationName()
	{
		if (dropBoxGameLocation.Value == "Trailer" && Game1.MasterPlayer.hasOrWillReceiveMail("pamHouseUpgrade"))
		{
			return "Trailer_Big";
		}
		return dropBoxGameLocation.Value;
	}

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		if (data.TryGetValue("AcceptedContextTags", out var value))
		{
			acceptableContextTagSets.Add(order.Parse(value.Trim()));
		}
		if (data.TryGetValue("DropBox", out value))
		{
			dropBox.Value = order.Parse(value.Trim());
		}
		if (data.TryGetValue("DropBoxGameLocation", out value))
		{
			dropBoxGameLocation.Value = order.Parse(value.Trim());
		}
		if (data.TryGetValue("DropBoxIndicatorLocation", out value))
		{
			string[] array = ArgUtility.SplitBySpace(order.Parse(value));
			dropBoxTileLocation.Value = new Vector2((float)Convert.ToDouble(array[0]), (float)Convert.ToDouble(array[1]));
		}
		if (data.TryGetValue("MinimumCapacity", out value))
		{
			minimumCapacity.Value = int.Parse(order.Parse(value));
		}
	}

	public int GetAcceptCount(Item item, int stack_count)
	{
		if (IsValidItem(item))
		{
			return Math.Min(GetMaxCount() - GetCount(), stack_count);
		}
		return 0;
	}

	public override void OnCompletion()
	{
		base.OnCompletion();
		if (!string.IsNullOrEmpty(dropBoxGameLocation.Value))
		{
			GameLocation locationFromName = Game1.getLocationFromName(GetDropboxLocationName());
			if (locationFromName != null)
			{
				locationFromName.showDropboxIndicator = false;
			}
		}
	}

	public override bool CanComplete()
	{
		return confirmed.Value;
	}

	public virtual void Confirm()
	{
		if (GetCount() >= GetMaxCount())
		{
			confirmed.Value = true;
		}
		else
		{
			confirmed.Value = false;
		}
	}

	public override bool CanUncomplete()
	{
		return true;
	}

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(acceptableContextTagSets, "acceptableContextTagSets").AddField(dropBox, "dropBox").AddField(dropBoxGameLocation, "dropBoxGameLocation")
			.AddField(dropBoxTileLocation, "dropBoxTileLocation")
			.AddField(minimumCapacity, "minimumCapacity")
			.AddField(confirmed, "confirmed");
		confirmed.fieldChangeVisibleEvent += OnConfirmed;
	}

	protected void OnConfirmed(NetBool field, bool oldValue, bool newValue)
	{
		if (!Utility.ShouldIgnoreValueChangeCallback())
		{
			CheckCompletion();
		}
	}

	public virtual bool IsValidItem(Item item)
	{
		if (item == null)
		{
			return false;
		}
		foreach (string acceptableContextTagSet in acceptableContextTagSets)
		{
			bool flag = false;
			string[] array = acceptableContextTagSet.Split(',');
			foreach (string text in array)
			{
				if (text.StartsWith("color") && item is ColoredObject coloredObject && coloredObject.preservedParentSheetIndex.Value != null)
				{
					if (ItemContextTagManager.DoAnyTagsMatch(text.Split('/'), ItemContextTagManager.GetBaseContextTags(coloredObject.preservedParentSheetIndex.Value)))
					{
						return true;
					}
					flag = true;
					break;
				}
				if (!ItemContextTagManager.DoAnyTagsMatch(text.Split('/'), item.GetContextTags()))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return true;
			}
		}
		return false;
	}
}
