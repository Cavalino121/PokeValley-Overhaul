using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;
using Netcode.Validation;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.GameData.SpecialOrders;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Quests;
using StardewValley.SpecialOrders.Objectives;
using StardewValley.SpecialOrders.Rewards;
using StardewValley.TokenizableStrings;

namespace StardewValley.SpecialOrders;

[XmlInclude(typeof(OrderObjective))]
[XmlInclude(typeof(OrderReward))]
[NotImplicitNetField]
public class SpecialOrder : INetObject<NetFields>, IQuest
{
	[XmlIgnore]
	public Action<Farmer, Item, int> onItemShipped;

	[XmlIgnore]
	public Action<Farmer, Monster> onMonsterSlain;

	[XmlIgnore]
	public Action<Farmer, Item> onFishCaught;

	[XmlIgnore]
	public Action<Farmer, NPC, Item> onGiftGiven;

	[XmlIgnore]
	public Func<Farmer, NPC, Item, bool, int> onItemDelivered;

	[XmlIgnore]
	public Action<Farmer, Item> onItemCollected;

	[XmlIgnore]
	public Action<Farmer, int> onMineFloorReached;

	[XmlIgnore]
	public Action<Farmer, int> onJKScoreAchieved;

	[XmlIgnore]
	protected bool _objectiveRegistrationDirty;

	[XmlElement("preSelectedItems")]
	public NetStringDictionary<string, NetString> preSelectedItems = new NetStringDictionary<string, NetString>();

	[XmlElement("selectedRandomElements")]
	public NetStringDictionary<int, NetInt> selectedRandomElements = new NetStringDictionary<int, NetInt>();

	[XmlElement("objectives")]
	public NetList<OrderObjective, NetRef<OrderObjective>> objectives = new NetList<OrderObjective, NetRef<OrderObjective>>();

	[XmlElement("generationSeed")]
	public NetInt generationSeed = new NetInt();

	[XmlElement("seenParticipantsIDs")]
	public NetLongDictionary<bool, NetBool> seenParticipants = new NetLongDictionary<bool, NetBool>();

	[XmlElement("participantsIDs")]
	public NetLongDictionary<bool, NetBool> participants = new NetLongDictionary<bool, NetBool>();

	[XmlElement("unclaimedRewardsIDs")]
	public NetLongDictionary<bool, NetBool> unclaimedRewards = new NetLongDictionary<bool, NetBool>();

	[XmlElement("donatedItems")]
	public readonly NetCollection<Item> donatedItems = new NetCollection<Item>();

	[XmlElement("appliedSpecialRules")]
	public bool appliedSpecialRules;

	[XmlIgnore]
	public readonly NetMutex donateMutex = new NetMutex();

	[XmlIgnore]
	protected int _isIslandOrder = -1;

	[XmlElement("rewards")]
	public NetList<OrderReward, NetRef<OrderReward>> rewards = new NetList<OrderReward, NetRef<OrderReward>>();

	[XmlIgnore]
	protected int _moneyReward = -1;

	[XmlElement("questKey")]
	public NetString questKey = new NetString();

	[XmlElement("questName")]
	public NetString questName = new NetString("Strings\\SpecialOrders:PlaceholderName");

	[XmlElement("questDescription")]
	public NetString questDescription = new NetString("Strings\\SpecialOrders:PlaceholderDescription");

	[XmlElement("requester")]
	public NetString requester = new NetString();

	[XmlElement("orderType")]
	public NetString orderType = new NetString("");

	[XmlElement("specialRule")]
	public NetString specialRule = new NetString("");

	[XmlElement("readyForRemoval")]
	public NetBool readyForRemoval = new NetBool(value: false);

	[XmlElement("itemToRemoveOnEnd")]
	public NetString itemToRemoveOnEnd = new NetString();

	[XmlElement("mailToRemoveOnEnd")]
	public NetString mailToRemoveOnEnd = new NetString();

	[XmlIgnore]
	protected string _localizedName;

	[XmlIgnore]
	protected string _localizedDescription;

	[XmlElement("dueDate")]
	public NetInt dueDate = new NetInt();

	[XmlElement("duration")]
	public NetEnum<QuestDuration> questDuration = new NetEnum<QuestDuration>();

	[XmlIgnore]
	protected List<OrderObjective> _registeredObjectives = new List<OrderObjective>();

	[XmlIgnore]
	protected Dictionary<Item, bool> _highlightLookup;

	[XmlIgnore]
	protected SpecialOrderData _orderData;

	[XmlElement("questState")]
	public NetEnum<SpecialOrderStatus> questState = new NetEnum<SpecialOrderStatus>(SpecialOrderStatus.InProgress);

	[XmlIgnore]
	public NetFields NetFields { get; } = new NetFields("SpecialOrder");

	public SpecialOrder()
	{
		InitializeNetFields();
	}

	public virtual void SetDuration(QuestDuration duration)
	{
		questDuration.Value = duration;
		WorldDate worldDate = new WorldDate();
		switch (duration)
		{
		case QuestDuration.Week:
			worldDate = new WorldDate(Game1.year, Game1.season, (Game1.dayOfMonth - 1) / 7 * 7);
			worldDate.TotalDays++;
			worldDate.TotalDays += 7;
			break;
		case QuestDuration.TwoWeeks:
			worldDate = new WorldDate(Game1.year, Game1.season, (Game1.dayOfMonth - 1) / 7 * 7);
			worldDate.TotalDays++;
			worldDate.TotalDays += 14;
			break;
		case QuestDuration.Month:
			worldDate = new WorldDate(Game1.year, Game1.season, 0);
			worldDate.TotalDays++;
			worldDate.TotalDays += 28;
			break;
		case QuestDuration.OneDay:
			worldDate = new WorldDate(Game1.year, Game1.currentSeason, Game1.dayOfMonth);
			worldDate.TotalDays++;
			break;
		case QuestDuration.TwoDays:
			worldDate = WorldDate.Now();
			worldDate.TotalDays += 2;
			break;
		case QuestDuration.ThreeDays:
			worldDate = WorldDate.Now();
			worldDate.TotalDays += 3;
			break;
		}
		dueDate.Value = worldDate.TotalDays;
	}

	public virtual void OnFail()
	{
		foreach (OrderObjective objective in objectives)
		{
			objective.OnFail();
		}
		for (int i = 0; i < donatedItems.Count; i++)
		{
			Item item = donatedItems[i];
			donatedItems[i] = null;
			if (item != null)
			{
				Game1.player.team.returnedDonations.Add(item);
				Game1.player.team.newLostAndFoundItems.Value = true;
			}
		}
		if (Game1.IsMasterGame)
		{
			HostHandleQuestEnd();
		}
		questState.Value = SpecialOrderStatus.Failed;
		_RemoveSpecialRuleIfNecessary();
	}

	public virtual int GetCompleteObjectivesCount()
	{
		int num = 0;
		foreach (OrderObjective objective in objectives)
		{
			if (objective.IsComplete())
			{
				num++;
			}
		}
		return num;
	}

	public virtual void ConfirmCompleteDonations()
	{
		foreach (OrderObjective objective in objectives)
		{
			if (objective is DonateObjective donateObjective)
			{
				donateObjective.Confirm();
			}
		}
	}

	public virtual void UpdateDonationCounts()
	{
		_highlightLookup = null;
		int num = 0;
		int num2 = 0;
		foreach (OrderObjective objective in objectives)
		{
			if (!(objective is DonateObjective donateObjective))
			{
				continue;
			}
			int num3 = 0;
			if (donateObjective.GetCount() >= donateObjective.GetMaxCount())
			{
				num++;
			}
			foreach (Item donatedItem in donatedItems)
			{
				if (donateObjective.IsValidItem(donatedItem))
				{
					num3 += donatedItem.Stack;
				}
			}
			donateObjective.SetCount(num3);
			if (donateObjective.GetCount() >= donateObjective.GetMaxCount())
			{
				num2++;
			}
		}
		if (num2 > num)
		{
			Game1.playSound("newArtifact");
		}
	}

	public bool HighlightAcceptableItems(Item item)
	{
		if (_highlightLookup != null && _highlightLookup.TryGetValue(item, out var value))
		{
			return value;
		}
		if (_highlightLookup == null)
		{
			_highlightLookup = new Dictionary<Item, bool>();
		}
		foreach (OrderObjective objective in objectives)
		{
			if (objective is DonateObjective donateObjective && donateObjective.GetAcceptCount(item, 1) > 0)
			{
				_highlightLookup[item] = true;
				return true;
			}
		}
		_highlightLookup[item] = false;
		return false;
	}

	public virtual int GetAcceptCount(Item item)
	{
		int num = 0;
		int num2 = item.Stack;
		foreach (OrderObjective objective in objectives)
		{
			if (objective is DonateObjective donateObjective)
			{
				int acceptCount = donateObjective.GetAcceptCount(item, num2);
				num2 -= acceptCount;
				num += acceptCount;
			}
		}
		return num;
	}

	public static bool CheckTags(string tag_list)
	{
		if (tag_list == null)
		{
			return true;
		}
		string[] array = tag_list.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].Trim();
		}
		string[] array2 = array;
		for (int j = 0; j < array2.Length; j++)
		{
			string text = array2[j];
			if (text.Length != 0)
			{
				bool flag = true;
				if (text.StartsWith('!'))
				{
					flag = false;
					text = text.Substring(1);
				}
				if (CheckTag(text) != flag)
				{
					return false;
				}
			}
		}
		return true;
	}

	public static bool CheckTag(string tag)
	{
		if (tag == "NOT_IMPLEMENTED")
		{
			return false;
		}
		if (tag.StartsWith("dropbox_"))
		{
			string box_id = tag.Substring("dropbox_".Length);
			foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
			{
				if (specialOrder.UsesDropBox(box_id))
				{
					return true;
				}
			}
		}
		if (tag.StartsWith("rule_"))
		{
			string special_rule = tag.Substring("rule_".Length);
			if (Game1.player.team.SpecialOrderRuleActive(special_rule))
			{
				return true;
			}
		}
		if (tag.StartsWith("completed_"))
		{
			string item = tag.Substring("completed_".Length);
			if (Game1.player.team.completedSpecialOrders.Contains(item))
			{
				return true;
			}
		}
		if (tag.StartsWith("season_"))
		{
			string text = tag.Substring("season_".Length);
			if (Game1.currentSeason == text)
			{
				return true;
			}
		}
		else if (tag.StartsWith("mail_"))
		{
			string id = tag.Substring("mail_".Length);
			if (Game1.MasterPlayer.hasOrWillReceiveMail(id))
			{
				return true;
			}
		}
		else if (tag.StartsWith("event_"))
		{
			string item2 = tag.Substring("event_".Length);
			if (Game1.MasterPlayer.eventsSeen.Contains(item2))
			{
				return true;
			}
		}
		else
		{
			if (tag == "island")
			{
				if (Utility.doesAnyFarmerHaveOrWillReceiveMail("seenBoatJourney"))
				{
					return true;
				}
				return false;
			}
			if (tag.StartsWith("knows_"))
			{
				string key = tag.Substring("knows_".Length);
				foreach (Farmer allFarmer in Game1.getAllFarmers())
				{
					if (allFarmer.friendshipData.ContainsKey(key))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public bool IsIslandOrder()
	{
		if (_isIslandOrder == -1 && DataLoader.SpecialOrders(Game1.content).TryGetValue(questKey.Value, out var value))
		{
			string requiredTags = value.RequiredTags;
			_isIslandOrder = ((requiredTags != null && requiredTags.Contains("island")) ? (_isIslandOrder = 1) : (_isIslandOrder = 0));
		}
		return _isIslandOrder == 1;
	}

	public static bool IsSpecialOrdersBoardUnlocked()
	{
		return Game1.stats.DaysPlayed >= 58;
	}

	public static void RemoveAllSpecialOrders(string orderType)
	{
		Game1.player.team.availableSpecialOrders.RemoveWhere((SpecialOrder order) => order.orderType.Value == orderType);
		Game1.player.team.acceptedSpecialOrderTypes.Remove(orderType);
	}

	public static void UpdateAvailableSpecialOrders(string orderType, bool forceRefresh)
	{
		foreach (SpecialOrder availableSpecialOrder in Game1.player.team.availableSpecialOrders)
		{
			if ((availableSpecialOrder.questDuration.Value == QuestDuration.TwoDays || availableSpecialOrder.questDuration.Value == QuestDuration.ThreeDays) && !Game1.player.team.acceptedSpecialOrderTypes.Contains(availableSpecialOrder.orderType.Value))
			{
				availableSpecialOrder.SetDuration(availableSpecialOrder.questDuration.Value);
			}
		}
		if (!forceRefresh)
		{
			foreach (SpecialOrder availableSpecialOrder2 in Game1.player.team.availableSpecialOrders)
			{
				if (availableSpecialOrder2.orderType.Value == orderType)
				{
					return;
				}
			}
		}
		RemoveAllSpecialOrders(orderType);
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, SpecialOrderData> item in DataLoader.SpecialOrders(Game1.content))
		{
			if (item.Value.OrderType == orderType && CanStartOrderNow(item.Key, item.Value))
			{
				list.Add(item.Key);
			}
		}
		List<string> list2 = new List<string>(list);
		if (orderType == "")
		{
			list.RemoveAll((string id) => Game1.player.team.completedSpecialOrders.Contains(id));
		}
		Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)Game1.stats.DaysPlayed * 1.3);
		for (int num = 0; num < 2; num++)
		{
			if (list.Count == 0)
			{
				if (list2.Count == 0)
				{
					break;
				}
				list = new List<string>(list2);
			}
			string text = random.ChooseFrom(list);
			Game1.player.team.availableSpecialOrders.Add(GetSpecialOrder(text, random.Next()));
			list.Remove(text);
			list2.Remove(text);
		}
	}

	/// <summary>Get whether a special order is eligible to be started now by the player.</summary>
	/// <param name="orderId">The order ID in <c>Data/SpecialOrders</c>.</param>
	/// <param name="order">The special order data.</param>
	public static bool CanStartOrderNow(string orderId, SpecialOrderData order)
	{
		if (!order.Repeatable && Game1.MasterPlayer.team.completedSpecialOrders.Contains(orderId))
		{
			return false;
		}
		if (Game1.dayOfMonth >= 16 && order.Duration == QuestDuration.Month)
		{
			return false;
		}
		if (!CheckTags(order.RequiredTags))
		{
			return false;
		}
		if (!GameStateQuery.CheckConditions(order.Condition))
		{
			return false;
		}
		foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
		{
			if (specialOrder.questKey.Value == orderId)
			{
				return false;
			}
		}
		return true;
	}

	public static SpecialOrder GetSpecialOrder(string key, int? generation_seed)
	{
		try
		{
			if (!generation_seed.HasValue)
			{
				generation_seed = Game1.random.Next();
			}
			if (DataLoader.SpecialOrders(Game1.content).TryGetValue(key, out var value))
			{
				Random random = Utility.CreateRandom(generation_seed.Value);
				SpecialOrder specialOrder = new SpecialOrder();
				specialOrder.generationSeed.Value = generation_seed.Value;
				specialOrder._orderData = value;
				specialOrder.questKey.Value = key;
				specialOrder.questName.Value = value.Name;
				specialOrder.requester.Value = value.Requester;
				specialOrder.orderType.Value = value.OrderType.Trim();
				specialOrder.specialRule.Value = value.SpecialRule.Trim();
				if (value.ItemToRemoveOnEnd != null)
				{
					specialOrder.itemToRemoveOnEnd.Value = value.ItemToRemoveOnEnd;
				}
				if (value.MailToRemoveOnEnd != null)
				{
					specialOrder.mailToRemoveOnEnd.Value = value.MailToRemoveOnEnd;
				}
				specialOrder.selectedRandomElements.Clear();
				if (value.RandomizedElements != null)
				{
					foreach (RandomizedElement randomizedElement in value.RandomizedElements)
					{
						List<int> list = new List<int>();
						for (int i = 0; i < randomizedElement.Values.Count; i++)
						{
							if (CheckTags(randomizedElement.Values[i].RequiredTags))
							{
								list.Add(i);
							}
						}
						int num = random.ChooseFrom(list);
						specialOrder.selectedRandomElements[randomizedElement.Name] = num;
						string value2 = randomizedElement.Values[num].Value;
						if (!value2.StartsWith("PICK_ITEM"))
						{
							continue;
						}
						value2 = value2.Substring("PICK_ITEM".Length);
						string[] array = value2.Split(',');
						List<string> list2 = new List<string>();
						string[] array2 = array;
						for (int j = 0; j < array2.Length; j++)
						{
							string text = array2[j].Trim();
							if (text.Length != 0)
							{
								ParsedItemData data = ItemRegistry.GetData(text);
								if (data != null)
								{
									list2.Add(data.QualifiedItemId);
									continue;
								}
								Item item = Utility.fuzzyItemSearch(text);
								list2.Add(item.QualifiedItemId);
							}
						}
						specialOrder.preSelectedItems[randomizedElement.Name] = random.ChooseFrom(list2);
					}
				}
				specialOrder.SetDuration(value.Duration);
				specialOrder.questDescription.Value = value.Text;
				string text2 = typeof(OrderObjective).Namespace;
				string text3 = typeof(OrderReward).Namespace;
				foreach (SpecialOrderObjectiveData objective in value.Objectives)
				{
					Type type = Type.GetType(text2 + "." + objective.Type.Trim() + "Objective");
					if (!(type == null) && type.IsSubclassOf(typeof(OrderObjective)))
					{
						OrderObjective orderObjective = (OrderObjective)Activator.CreateInstance(type);
						if (orderObjective != null)
						{
							orderObjective.description.Value = objective.Text;
							orderObjective.maxCount.Value = int.Parse(specialOrder.Parse(objective.RequiredCount));
							orderObjective.Load(specialOrder, objective.Data);
							specialOrder.objectives.Add(orderObjective);
						}
					}
				}
				foreach (SpecialOrderRewardData reward in value.Rewards)
				{
					Type type2 = Type.GetType(text3 + "." + reward.Type.Trim() + "Reward");
					if (!(type2 == null) && type2.IsSubclassOf(typeof(OrderReward)))
					{
						OrderReward orderReward = (OrderReward)Activator.CreateInstance(type2);
						if (orderReward != null)
						{
							orderReward.Load(specialOrder, reward.Data);
							specialOrder.rewards.Add(orderReward);
						}
					}
				}
				return specialOrder;
			}
		}
		catch (Exception exception)
		{
			Game1.log.Error("Failed loading special order '" + key + "'.", exception);
		}
		return null;
	}

	public static string MakeLocalizationReplacements(string data)
	{
		data = data.Trim();
		int num;
		do
		{
			num = data.LastIndexOf('[');
			if (num >= 0)
			{
				int num2 = data.IndexOf(']', num);
				if (num2 == -1)
				{
					return data;
				}
				string text = data.Substring(num + 1, num2 - num - 1);
				string value = Game1.content.LoadString("Strings\\SpecialOrderStrings:" + text);
				data = data.Remove(num, num2 - num + 1);
				data = data.Insert(num, value);
			}
		}
		while (num >= 0);
		return data;
	}

	public virtual string Parse(string data)
	{
		data = data.Trim();
		GetData();
		data = MakeLocalizationReplacements(data);
		int num;
		do
		{
			num = data.LastIndexOf('{');
			if (num < 0)
			{
				continue;
			}
			int num2 = data.IndexOf('}', num);
			if (num2 == -1)
			{
				return data;
			}
			string text = data.Substring(num + 1, num2 - num - 1);
			string text2 = text;
			string text3 = text;
			string text4 = null;
			if (text.Contains(':'))
			{
				string[] array = text.Split(':');
				text3 = array[0];
				if (array.Length > 1)
				{
					text4 = array[1];
				}
			}
			if (_orderData.RandomizedElements != null)
			{
				int value2;
				if (preSelectedItems.TryGetValue(text3, out var value))
				{
					Item item = ItemRegistry.Create(value);
					switch (text4)
					{
					case "Text":
						text2 = item.DisplayName;
						break;
					case "TextPlural":
						text2 = Lexicon.makePlural(item.DisplayName);
						break;
					case "TextPluralCapitalized":
						text2 = Utility.capitalizeFirstLetter(Lexicon.makePlural(item.DisplayName));
						break;
					case "Tags":
					{
						string text5 = "id_" + Utility.getStandardDescriptionFromItem(item, 0, '_');
						text5 = text5.Substring(0, text5.Length - 2).ToLower();
						text2 = text5;
						break;
					}
					case "Price":
						text2 = ((item is Object obj) ? (obj.sellToStorePrice(-1L).ToString() ?? "") : "1");
						break;
					}
				}
				else if (selectedRandomElements.TryGetValue(text3, out value2))
				{
					foreach (RandomizedElement randomizedElement in _orderData.RandomizedElements)
					{
						if (randomizedElement.Name == text3)
						{
							text2 = MakeLocalizationReplacements(randomizedElement.Values[value2].Value);
							break;
						}
					}
				}
			}
			if (text4 != null)
			{
				string[] array2 = text2.Split('|');
				for (int i = 0; i < array2.Length; i += 2)
				{
					if (i + 1 <= array2.Length && array2[i] == text4)
					{
						text2 = array2[i + 1];
						break;
					}
				}
			}
			data = data.Remove(num, num2 - num + 1);
			data = data.Insert(num, text2);
		}
		while (num >= 0);
		return data;
	}

	/// <summary>Get the special order's data from <c>Data/SpecialOrders</c>, if found.</summary>
	public virtual SpecialOrderData GetData()
	{
		if (_orderData == null)
		{
			TryGetData(questKey.Value, out _orderData);
		}
		return _orderData;
	}

	/// <summary>Try to get a special order's data from <c>Data/SpecialOrders</c>.</summary>
	/// <param name="id">The special order ID (i.e. the key in <c>Data/SpecialOrders</c>).</param>
	/// <param name="data">The special order data, if found.</param>
	/// <returns>Returns whether the special order data was found.</returns>
	public static bool TryGetData(string id, out SpecialOrderData data)
	{
		if (id == null)
		{
			data = null;
			return false;
		}
		return DataLoader.SpecialOrders(Game1.content).TryGetValue(id, out data);
	}

	public virtual void InitializeNetFields()
	{
		NetFields.SetOwner(this).AddField(questName, "questName").AddField(questDescription, "questDescription")
			.AddField(dueDate, "dueDate")
			.AddField(objectives, "objectives")
			.AddField(rewards, "rewards")
			.AddField(questState, "questState")
			.AddField(donatedItems, "donatedItems")
			.AddField(questKey, "questKey")
			.AddField(requester, "requester")
			.AddField(generationSeed, "generationSeed")
			.AddField(selectedRandomElements, "selectedRandomElements")
			.AddField(preSelectedItems, "preSelectedItems")
			.AddField(orderType, "orderType")
			.AddField(specialRule, "specialRule")
			.AddField(participants, "participants")
			.AddField(seenParticipants, "seenParticipants")
			.AddField(unclaimedRewards, "unclaimedRewards")
			.AddField(donateMutex.NetFields, "donateMutex.NetFields")
			.AddField(itemToRemoveOnEnd, "itemToRemoveOnEnd")
			.AddField(mailToRemoveOnEnd, "mailToRemoveOnEnd")
			.AddField(questDuration, "questDuration")
			.AddField(readyForRemoval, "readyForRemoval");
		objectives.OnArrayReplaced += delegate
		{
			_objectiveRegistrationDirty = true;
		};
		objectives.OnElementChanged += delegate
		{
			_objectiveRegistrationDirty = true;
		};
	}

	protected virtual void _UpdateObjectiveRegistration()
	{
		for (int i = 0; i < _registeredObjectives.Count; i++)
		{
			OrderObjective orderObjective = _registeredObjectives[i];
			if (!objectives.Contains(orderObjective))
			{
				orderObjective.Unregister();
			}
		}
		foreach (OrderObjective objective in objectives)
		{
			if (!_registeredObjectives.Contains(objective))
			{
				objective.Register(this);
				_registeredObjectives.Add(objective);
			}
		}
	}

	public bool UsesDropBox(string box_id)
	{
		if (questState.Value != SpecialOrderStatus.InProgress)
		{
			return false;
		}
		foreach (OrderObjective objective in objectives)
		{
			if (objective is DonateObjective donateObjective && donateObjective.dropBox.Value == box_id)
			{
				return true;
			}
		}
		return false;
	}

	public int GetMinimumDropBoxCapacity(string box_id)
	{
		int num = 9;
		foreach (OrderObjective objective in objectives)
		{
			if (objective is DonateObjective donateObjective && donateObjective.dropBox.Value == box_id && donateObjective.minimumCapacity.Value > 0)
			{
				num = Math.Max(num, donateObjective.minimumCapacity.Value);
			}
		}
		return num;
	}

	public virtual void Update()
	{
		_AddSpecialRulesIfNecessary();
		if (_objectiveRegistrationDirty)
		{
			_objectiveRegistrationDirty = false;
			_UpdateObjectiveRegistration();
		}
		if (!readyForRemoval.Value)
		{
			switch (questState.Value)
			{
			case SpecialOrderStatus.InProgress:
				participants.TryAdd(Game1.player.UniqueMultiplayerID, value: true);
				break;
			case SpecialOrderStatus.Complete:
				if (unclaimedRewards.Remove(Game1.player.UniqueMultiplayerID))
				{
					Game1.stats.QuestsCompleted++;
					Game1.playSound("questcomplete");
					Game1.dayTimeMoneyBox.questsDirty = true;
					if (orderType.Value == "" && !questKey.Value.Contains("QiChallenge") && !questKey.Value.Contains("DesertFestival"))
					{
						Game1.player.stats.Increment("specialOrderPrizeTickets");
					}
					foreach (OrderReward reward in rewards)
					{
						reward.Grant();
					}
				}
				if (participants.ContainsKey(Game1.player.UniqueMultiplayerID) && GetMoneyReward() <= 0)
				{
					RemoveFromParticipants();
				}
				break;
			}
		}
		donateMutex.Update(Game1.getOnlineFarmers());
		if (donateMutex.IsLockHeld() && Game1.activeClickableMenu == null)
		{
			donateMutex.ReleaseLock();
		}
		if (Game1.activeClickableMenu == null)
		{
			_highlightLookup = null;
		}
		if (Game1.IsMasterGame && questState.Value != SpecialOrderStatus.InProgress)
		{
			MarkForRemovalIfEmpty();
			if (readyForRemoval.Value)
			{
				_RemoveSpecialRuleIfNecessary();
				Game1.player.team.specialOrders.Remove(this);
			}
		}
	}

	public virtual void RemoveFromParticipants()
	{
		participants.Remove(Game1.player.UniqueMultiplayerID);
		MarkForRemovalIfEmpty();
	}

	public virtual void MarkForRemovalIfEmpty()
	{
		if (participants.Length == 0)
		{
			readyForRemoval.Value = true;
		}
	}

	public virtual void HostHandleQuestEnd()
	{
		if (Game1.IsMasterGame)
		{
			if (itemToRemoveOnEnd.Value != null && !Game1.player.team.itemsToRemoveOvernight.Contains(itemToRemoveOnEnd.Value))
			{
				Game1.player.team.itemsToRemoveOvernight.Add(itemToRemoveOnEnd.Value);
			}
			if (mailToRemoveOnEnd.Value != null && !Game1.player.team.mailToRemoveOvernight.Contains(mailToRemoveOnEnd.Value))
			{
				Game1.player.team.mailToRemoveOvernight.Add(mailToRemoveOnEnd.Value);
			}
		}
	}

	protected void _AddSpecialRulesIfNecessary()
	{
		if (!Game1.IsMasterGame || appliedSpecialRules || questState.Value != SpecialOrderStatus.InProgress)
		{
			return;
		}
		appliedSpecialRules = true;
		string[] array = specialRule.Value.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (!Game1.player.team.SpecialOrderRuleActive(text, this))
			{
				AddSpecialRule(text);
				if (Game1.player.team.specialRulesRemovedToday.Contains(text))
				{
					Game1.player.team.specialRulesRemovedToday.Remove(text);
				}
			}
		}
	}

	protected void _RemoveSpecialRuleIfNecessary()
	{
		if (!Game1.IsMasterGame || !appliedSpecialRules)
		{
			return;
		}
		appliedSpecialRules = false;
		string[] array = specialRule.Value.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (!Game1.player.team.SpecialOrderRuleActive(text, this))
			{
				RemoveSpecialRule(text);
				if (!Game1.player.team.specialRulesRemovedToday.Contains(text))
				{
					Game1.player.team.specialRulesRemovedToday.Add(text);
				}
			}
		}
	}

	public virtual void AddSpecialRule(string rule)
	{
		if (!(rule == "MINE_HARD"))
		{
			if (rule == "SC_HARD")
			{
				Game1.netWorldState.Value.SkullCavesDifficulty++;
				Game1.player.team.kickOutOfMinesEvent.Fire(121);
			}
		}
		else
		{
			Game1.netWorldState.Value.MinesDifficulty++;
			Game1.player.team.kickOutOfMinesEvent.Fire(120);
			Game1.netWorldState.Value.LowestMineLevelForOrder = 0;
		}
	}

	public static void RemoveSpecialRuleAtEndOfDay(string rule)
	{
		switch (rule)
		{
		case "MINE_HARD":
			if (Game1.netWorldState.Value.MinesDifficulty > 0)
			{
				Game1.netWorldState.Value.MinesDifficulty--;
			}
			Game1.netWorldState.Value.LowestMineLevelForOrder = -1;
			break;
		case "SC_HARD":
			if (Game1.netWorldState.Value.SkullCavesDifficulty > 0)
			{
				Game1.netWorldState.Value.SkullCavesDifficulty--;
			}
			break;
		case "QI_COOKING":
			Utility.ForEachItem(delegate(Item item)
			{
				if (item is Object obj && obj.orderData.Value == "QI_COOKING")
				{
					obj.orderData.Value = null;
					obj.MarkContextTagsDirty();
				}
				return true;
			});
			break;
		}
	}

	public virtual void RemoveSpecialRule(string rule)
	{
		if (rule == "QI_BEANS")
		{
			Game1.player.team.itemsToRemoveOvernight.Add("890");
			Game1.player.team.itemsToRemoveOvernight.Add("889");
		}
	}

	public virtual bool HasMoneyReward()
	{
		if (questState.Value == SpecialOrderStatus.Complete && GetMoneyReward() > 0)
		{
			return participants.ContainsKey(Game1.player.UniqueMultiplayerID);
		}
		return false;
	}

	public virtual void Fail()
	{
	}

	public virtual void AddObjective(OrderObjective objective)
	{
		objectives.Add(objective);
	}

	public void CheckCompletion()
	{
		if (questState.Value != SpecialOrderStatus.InProgress)
		{
			return;
		}
		foreach (OrderObjective objective in objectives)
		{
			if (objective.failOnCompletion.Value && objective.IsComplete())
			{
				OnFail();
				return;
			}
		}
		foreach (OrderObjective objective2 in objectives)
		{
			if (!objective2.failOnCompletion.Value && !objective2.IsComplete())
			{
				return;
			}
		}
		if (!Game1.IsMasterGame)
		{
			return;
		}
		foreach (long key in participants.Keys)
		{
			unclaimedRewards.TryAdd(key, value: true);
		}
		Game1.multiplayer.globalChatInfoMessage("CompletedSpecialOrder", TokenStringBuilder.SpecialOrderName(questKey.Value));
		HostHandleQuestEnd();
		Game1.player.team.completedSpecialOrders.Add(questKey.Value);
		questState.Value = SpecialOrderStatus.Complete;
		_RemoveSpecialRuleIfNecessary();
	}

	public override string ToString()
	{
		string text = "";
		foreach (OrderObjective objective in objectives)
		{
			text += objective.description.Value;
			if (objective.GetMaxCount() > 1)
			{
				text = text + " (" + objective.GetCount() + "/" + objective.GetMaxCount() + ")";
			}
			text += "\n";
		}
		return text.Trim();
	}

	public string GetName()
	{
		if (_localizedName == null)
		{
			_localizedName = MakeLocalizationReplacements(questName.Value);
		}
		return _localizedName;
	}

	public string GetDescription()
	{
		if (_localizedDescription == null)
		{
			_localizedDescription = Parse(questDescription.Value).Trim();
		}
		return _localizedDescription;
	}

	public List<string> GetObjectiveDescriptions()
	{
		List<string> list = new List<string>();
		foreach (OrderObjective objective in objectives)
		{
			list.Add(Parse(objective.GetDescription()));
		}
		return list;
	}

	public bool CanBeCancelled()
	{
		return false;
	}

	public void MarkAsViewed()
	{
		seenParticipants.TryAdd(Game1.player.UniqueMultiplayerID, value: true);
	}

	public bool IsHidden()
	{
		return !participants.ContainsKey(Game1.player.UniqueMultiplayerID);
	}

	public bool ShouldDisplayAsNew()
	{
		return !seenParticipants.ContainsKey(Game1.player.UniqueMultiplayerID);
	}

	public bool HasReward()
	{
		return HasMoneyReward();
	}

	public int GetMoneyReward()
	{
		if (_moneyReward == -1)
		{
			_moneyReward = 0;
			foreach (OrderReward reward in rewards)
			{
				if (reward is MoneyReward moneyReward)
				{
					_moneyReward += moneyReward.GetRewardMoneyAmount();
				}
			}
		}
		return _moneyReward;
	}

	public bool ShouldDisplayAsComplete()
	{
		return questState.Value != SpecialOrderStatus.InProgress;
	}

	public bool IsTimedQuest()
	{
		return true;
	}

	public int GetDaysLeft()
	{
		if (questState.Value != SpecialOrderStatus.InProgress)
		{
			return 0;
		}
		return dueDate.Value - Game1.Date.TotalDays;
	}

	public void OnMoneyRewardClaimed()
	{
		participants.Remove(Game1.player.UniqueMultiplayerID);
		MarkForRemovalIfEmpty();
	}

	public bool OnLeaveQuestPage()
	{
		if (!participants.ContainsKey(Game1.player.UniqueMultiplayerID))
		{
			MarkForRemovalIfEmpty();
			return true;
		}
		return false;
	}
}
