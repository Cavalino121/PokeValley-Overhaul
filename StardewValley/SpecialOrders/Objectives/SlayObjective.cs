using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;
using StardewValley.Monsters;

namespace StardewValley.SpecialOrders.Objectives;

public class SlayObjective : OrderObjective
{
	[XmlElement("targetNames")]
	public NetStringList targetNames = new NetStringList();

	/// <summary>Whether to ignore monsters killed on the farm.</summary>
	[XmlElement("ignoreFarmMonsters")]
	public NetBool ignoreFarmMonsters = new NetBool(value: true);

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(targetNames, "targetNames").AddField(ignoreFarmMonsters, "ignoreFarmMonsters");
	}

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		base.Load(order, data);
		if (data.TryGetValue("TargetName", out var value))
		{
			string[] array = order.Parse(value).Split(',');
			foreach (string text in array)
			{
				targetNames.Add(text.Trim());
			}
		}
		if (data.TryGetValue("IgnoreFarmMonsters", out var value2))
		{
			if (bool.TryParse(value2, out var result))
			{
				ignoreFarmMonsters.Value = result;
			}
			else
			{
				Game1.log.Warn("Special order slay objective can't parse IgnoreFarmMonsters value '" + value2 + "' as a boolean.");
			}
		}
	}

	protected override void _Register()
	{
		base._Register();
		SpecialOrder order = _order;
		order.onMonsterSlain = (Action<Farmer, Monster>)Delegate.Combine(order.onMonsterSlain, new Action<Farmer, Monster>(OnMonsterSlain));
	}

	protected override void _Unregister()
	{
		base._Unregister();
		SpecialOrder order = _order;
		order.onMonsterSlain = (Action<Farmer, Monster>)Delegate.Remove(order.onMonsterSlain, new Action<Farmer, Monster>(OnMonsterSlain));
	}

	public virtual void OnMonsterSlain(Farmer farmer, Monster monster)
	{
		if (ignoreFarmMonsters.Value && monster.currentLocation?.Name == "Farm")
		{
			return;
		}
		foreach (string targetName in targetNames)
		{
			if (monster.Name.Contains(targetName))
			{
				IncrementCount(1);
				break;
			}
		}
	}
}
