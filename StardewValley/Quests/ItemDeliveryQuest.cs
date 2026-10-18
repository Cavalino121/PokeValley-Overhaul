using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData.Characters;
using StardewValley.Network;

namespace StardewValley.Quests;

public class ItemDeliveryQuest : Quest
{
	/// <summary>The translated NPC dialogue shown when the quest is completed.</summary>
	public string targetMessage;

	/// <summary>The internal name for the NPC who gave the quest.</summary>
	[XmlElement("target")]
	public readonly NetString target = new NetString();

	/// <summary>The qualified item ID that must be delivered.</summary>
	[XmlElement("item")]
	public readonly NetString ItemId = new NetString();

	/// <summary>The number of items that must be delivered.</summary>
	[XmlElement("number")]
	public readonly NetInt number = new NetInt(1);

	/// <summary>The translatable text segments for the quest description shown in the quest log.</summary>
	public readonly NetDescriptionElementList parts = new NetDescriptionElementList();

	/// <summary>The translatable text segments for the <see cref="F:StardewValley.Quests.ItemDeliveryQuest.targetMessage" />.</summary>
	public readonly NetDescriptionElementList dialogueparts = new NetDescriptionElementList();

	/// <summary>The translatable text segments for the objective shown in the quest log (like "0/5 caught").</summary>
	[XmlElement("objective")]
	public readonly NetDescriptionElementRef objective = new NetDescriptionElementRef();

	/// <summary>Construct an instance.</summary>
	public ItemDeliveryQuest()
	{
		questType.Value = 3;
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="target">The internal name for the NPC who gave the quest.</param>
	/// <param name="itemId">The qualified or unqualified item ID that must be delivered.</param>
	public ItemDeliveryQuest(string target, string itemId)
		: this()
	{
		this.target.Value = target;
		ItemId.Value = ItemRegistry.QualifyItemId(itemId) ?? itemId;
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="target">The internal name for the NPC who gave the quest.</param>
	/// <param name="itemId">The qualified or unqualified item ID that must be delivered.</param>
	/// <param name="objective">The translatable text segments for the objective shown in the quest log (like "0/5 caught").</param>
	/// <param name="returnDialogue">The translated NPC dialogue shown when the quest is completed.</param>
	public ItemDeliveryQuest(string target, string itemId, string questTitle, string questDescription, string objective, string returnDialogue)
		: this(target, itemId)
	{
		base.questDescription = questDescription;
		base.questTitle = questTitle;
		_loadedTitle = true;
		targetMessage = returnDialogue;
		this.objective = new NetDescriptionElementRef(new DescriptionElement(objective));
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(target, "target").AddField(ItemId, "ItemId").AddField(number, "number")
			.AddField(parts, "parts")
			.AddField(dialogueparts, "dialogueparts")
			.AddField(objective, "objective");
	}

	public List<NPC> GetValidTargetList()
	{
		Farmer[] source = Game1.getAllFarmers().ToArray();
		HashSet<string> hashSet = new HashSet<string>(source.SelectMany((Farmer player) => player.friendshipData.Keys));
		HashSet<string> hashSet2 = new HashSet<string>(source.Select((Farmer p) => p.spouse));
		List<NPC> list = new List<NPC>();
		foreach (KeyValuePair<string, CharacterData> characterDatum in Game1.characterData)
		{
			CharacterData value = characterDatum.Value;
			if (GameStateQuery.CheckConditions(value.CanSocialize) && ((value.ItemDeliveryQuests != null) ? GameStateQuery.CheckConditions(value.ItemDeliveryQuests) : (value.HomeRegion == "Town")) && hashSet.Contains(characterDatum.Key) && !hashSet2.Contains(characterDatum.Key) && characterDatum.Value.Age != NpcAge.Child)
			{
				NPC characterFromName = Game1.getCharacterFromName(characterDatum.Key);
				if (characterFromName != null && !characterFromName.IsInvisible)
				{
					list.Add(characterFromName);
				}
			}
		}
		return list;
	}

	public void loadQuestInfo()
	{
		if (target.Value != null)
		{
			return;
		}
		Random random = CreateInitializationRandom();
		List<NPC> validTargetList = GetValidTargetList();
		NetStringDictionary<Friendship, NetRef<Friendship>> friendshipData = Game1.player.friendshipData;
		if (friendshipData == null || friendshipData.Length <= 0 || validTargetList.Count <= 0)
		{
			return;
		}
		NPC nPC = validTargetList[random.Next(validTargetList.Count)];
		if (nPC == null)
		{
			return;
		}
		target.Value = nPC.name.Value;
		if (target.Value.Equals("Wizard") && !Game1.player.mailReceived.Contains("wizardJunimoNote") && !Game1.player.mailReceived.Contains("JojaMember"))
		{
			target.Value = "Demetrius";
			nPC = Game1.getCharacterFromName(target.Value);
		}
		base.questTitle = Game1.content.LoadString("Strings\\1_6_Strings:ItemDeliveryQuestTitle", NPC.GetDisplayName(target.Value));
		Item item;
		if (Game1.season != Season.Winter && random.NextDouble() < 0.15)
		{
			ItemId.Value = random.ChooseFrom(Utility.possibleCropsAtThisTime(Game1.season, Game1.dayOfMonth <= 7));
			ItemId.Value = ItemRegistry.QualifyItemId(ItemId.Value) ?? ItemId.Value;
			item = ItemRegistry.Create(ItemId.Value);
			if (dailyQuest.Value || moneyReward.Value == 0)
			{
				moneyReward.Value = GetGoldRewardPerItem(item);
			}
			switch (target.Value)
			{
			case "Demetrius":
				parts.Clear();
				parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13311", "13314"), item));
				break;
			case "Marnie":
				parts.Clear();
				parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13317", "13320"), item));
				break;
			case "Sebastian":
				parts.Clear();
				parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13324", "13327"), item));
				break;
			default:
				parts.Clear();
				parts.Add("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13299", "13300", "13301"));
				parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13302", "13303", "13304"), item));
				parts.Add(random.Choose("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13306", "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13307", "", "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13308"));
				parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC));
				break;
			}
		}
		else
		{
			string randomItemFromSeason = Utility.getRandomItemFromSeason(Game1.season, 1000, forQuest: true);
			if (!(randomItemFromSeason == "-5"))
			{
				if (randomItemFromSeason == "-6")
				{
					ItemId.Value = "(O)184";
				}
				else
				{
					ItemId.Value = ItemRegistry.QualifyItemId(randomItemFromSeason) ?? randomItemFromSeason;
				}
			}
			else
			{
				ItemId.Value = "(O)176";
			}
			item = ItemRegistry.Create(ItemId.Value);
			if (dailyQuest.Value || moneyReward.Value == 0)
			{
				moneyReward.Value = GetGoldRewardPerItem(item);
			}
			DescriptionElement[] array = null;
			DescriptionElement[] array2 = null;
			DescriptionElement[] array3 = null;
			if ((item as Object)?.Type == "Cooking" && target.Value != "Wizard")
			{
				if (random.NextDouble() < 0.33)
				{
					DescriptionElement[] options = new DescriptionElement[12]
					{
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13336"),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13337"),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13338"),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13339"),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13340"),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13341"),
						(!(Game1.samBandName == Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2156"))) ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13347", new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.2156")) : ((Game1.elliottBookName != Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2157")) ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13342", new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.2157")) : new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13346")),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13349"),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13350"),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13351"),
						(Game1.season == Season.Winter) ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13353") : ((Game1.season == Season.Summer) ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13355") : new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13356")),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13357")
					};
					parts.Clear();
					parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13333", "13334"), item, random.ChooseFrom(options)));
					parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC));
				}
				else
				{
					DescriptionElement descriptionElement = (Game1.dayOfMonth % 7) switch
					{
						0 => new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.3042"), 
						1 => new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.3043"), 
						2 => new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.3044"), 
						3 => new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.3045"), 
						4 => new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.3046"), 
						5 => new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.3047"), 
						_ => new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.3048"), 
					};
					array = new DescriptionElement[5]
					{
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13360", item),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13364", item),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13367", item),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13370", item),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13373", descriptionElement, item, nPC)
					};
					array2 = new DescriptionElement[5]
					{
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
						new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
						new DescriptionElement("")
					};
					array3 = new DescriptionElement[5]
					{
						new DescriptionElement(""),
						new DescriptionElement(""),
						new DescriptionElement(""),
						new DescriptionElement(""),
						new DescriptionElement("")
					};
				}
				parts.Clear();
				int num = random.Next(array.Length);
				parts.Add(array[num]);
				parts.Add(array2[num]);
				parts.Add(array3[num]);
				if (target.Value.Equals("Sebastian"))
				{
					parts.Clear();
					parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13378", "13381"), item));
				}
			}
			else
			{
				if (random.NextBool())
				{
					Object obj = item as Object;
					if (obj != null && obj.Edibility > 0)
					{
						array = new DescriptionElement[1]
						{
							new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13383", item, new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose<string>("13385", "13386", "13387", "13388", "13389", "13390", "13391", "13392", "13393", "13394", "13395", "13396")), new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13400", item))
						};
						array2 = new DescriptionElement[2]
						{
							new DescriptionElement(random.Choose("", "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13398")),
							new DescriptionElement(random.Choose("", "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13402"))
						};
						array3 = new DescriptionElement[2]
						{
							new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
							new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC)
						};
						if (random.NextDouble() < 0.33)
						{
							DescriptionElement[] options2 = new DescriptionElement[12]
							{
								new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13336"),
								new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13337"),
								new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13338"),
								new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13339"),
								new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13340"),
								new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13341"),
								(!(Game1.samBandName == Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2156"))) ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13347", new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.2156")) : ((Game1.elliottBookName != Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2157")) ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13342", new DescriptionElement("Strings\\StringsFromCSFiles:Game1.cs.2157")) : new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13346")),
								new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13420"),
								new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13421"),
								new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13422"),
								(Game1.season == Season.Winter) ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13424") : ((Game1.season == Season.Summer) ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13426") : new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13427")),
								new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13357")
							};
							parts.Clear();
							parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13333", "13334"), item, random.ChooseFrom(options2)));
							parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC));
						}
						else
						{
							parts.Clear();
							int num2 = random.Next(array.Length);
							parts.Add(array[num2]);
							parts.Add(array2[num2]);
							parts.Add(array3[num2]);
						}
						switch (target.Value)
						{
						case "Demetrius":
							parts.Clear();
							parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13311", "13314"), item));
							break;
						case "Marnie":
							parts.Clear();
							parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13317", "13320"), item));
							break;
						case "Harvey":
							parts.Clear();
							parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13446", item, new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose<string>("13448", "13449", "13450", "13451", "13452", "13453", "13454", "13455", "13456", "13457", "13458", "13459"))));
							break;
						case "Gus":
							if (random.NextDouble() < 0.6)
							{
								parts.Clear();
								parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13462", item));
							}
							break;
						}
						goto IL_134b;
					}
				}
				if (random.NextBool())
				{
					Object obj2 = item as Object;
					if (obj2 == null || obj2.Edibility < 0)
					{
						parts.Clear();
						parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13464", item, new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose<string>("13465", "13466", "13467", "13468", "13469"))));
						parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC));
						if (target.Value.Equals("Emily"))
						{
							parts.Clear();
							parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13473", "13476"), item));
						}
						goto IL_134b;
					}
				}
				array = new DescriptionElement[9]
				{
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13480", nPC, item),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13481", item),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13485", item),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13491", "13492"), item),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13494", item),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13497", item),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13500", item, new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose<string>("13502", "13503", "13504", "13505", "13506", "13507", "13508", "13509", "13510", "13511", "13512", "13513"))),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13518", nPC, item),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13520", "13523"), item)
				};
				array2 = new DescriptionElement[9]
				{
					new DescriptionElement(""),
					new DescriptionElement(random.Choose("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13482", "", "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13483")),
					new DescriptionElement(random.Choose("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13487", "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13488", "", "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13489")),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13514", "13516")),
					new DescriptionElement(""),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC)
				};
				array3 = new DescriptionElement[9]
				{
					new DescriptionElement(""),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
					new DescriptionElement(""),
					new DescriptionElement(""),
					new DescriptionElement(""),
					new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13620", nPC),
					new DescriptionElement(""),
					new DescriptionElement("")
				};
				parts.Clear();
				int num3 = random.Next(array.Length);
				parts.Add(array[num3]);
				parts.Add(array2[num3]);
				parts.Add(array3[num3]);
			}
		}
		goto IL_134b;
		IL_134b:
		dialogueparts.Clear();
		dialogueparts.Add((random.NextBool(0.3) || target.Value == "Evelyn") ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13526") : new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13527", "13528")));
		dialogueparts.Add(random.NextBool(0.3) ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13530", item) : (random.NextBool() ? new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13532") : new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13534", "13535", "13536"))));
		dialogueparts.Add("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13538", "13539", "13540"));
		dialogueparts.Add("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13542", "13543", "13544"));
		switch (target.Value)
		{
		case "Wizard":
			parts.Clear();
			parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13546", "13548", "13551", "13553"), item));
			dialogueparts.Clear();
			dialogueparts.Add("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13555");
			break;
		case "Haley":
			parts.Clear();
			parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13557", "13560"), item));
			dialogueparts.Clear();
			dialogueparts.Add("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13566");
			break;
		case "Sam":
			parts.Clear();
			parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13568", "13571"), item));
			dialogueparts.Clear();
			dialogueparts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13577"));
			break;
		case "Maru":
		{
			bool flag2 = random.NextBool();
			parts.Clear();
			parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + (flag2 ? "13580" : "13583"), item));
			dialogueparts.Clear();
			dialogueparts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + (flag2 ? "13585" : "13587")));
			break;
		}
		case "Abigail":
		{
			bool flag = random.NextBool();
			parts.Clear();
			parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + (flag ? "13590" : "13593"), item));
			dialogueparts.Clear();
			dialogueparts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + (flag ? "13597" : "13599")));
			break;
		}
		case "Sebastian":
			dialogueparts.Clear();
			dialogueparts.Add("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13602");
			break;
		case "Elliott":
			dialogueparts.Clear();
			dialogueparts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13604", item));
			break;
		}
		DescriptionElement item2 = new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs." + random.Choose("13608", "13610", "13612"), nPC);
		parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13607", moneyReward.Value));
		parts.Add(item2);
		objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13614", nPC, item);
	}

	public override void reloadDescription()
	{
		if (_questDescription == "")
		{
			loadQuestInfo();
		}
		string text = "";
		string text2 = "";
		if (parts != null && parts.Count != 0)
		{
			foreach (DescriptionElement part in parts)
			{
				text += part.loadDescriptionElement();
			}
			base.questDescription = text;
		}
		if (dialogueparts != null && dialogueparts.Count != 0)
		{
			foreach (DescriptionElement dialoguepart in dialogueparts)
			{
				text2 += dialoguepart.loadDescriptionElement();
			}
			targetMessage = text2;
		}
		else if (HasId())
		{
			string[] rawQuestFields = Quest.GetRawQuestFields(id.Value);
			targetMessage = ArgUtility.Get(rawQuestFields, 9, targetMessage, allowBlank: false);
		}
	}

	public override void reloadObjective()
	{
		if (objective.Value != null)
		{
			base.currentObjective = objective.Value.loadDescriptionElement();
		}
	}

	/// <inheritdoc />
	public override bool OnItemOfferedToNpc(NPC npc, Item item, bool probe = false)
	{
		bool result = base.OnItemOfferedToNpc(npc, item, probe);
		if (completed.Value)
		{
			return false;
		}
		if (npc.IsVillager && npc.Name == target.Value && item.QualifiedItemId == ItemId.Value)
		{
			if (item.Stack >= number.Value)
			{
				if (!probe)
				{
					Game1.player.Items.Reduce(item, number.Value);
					reloadDescription();
					npc.CurrentDialogue.Push(new Dialogue(npc, null, targetMessage));
					Game1.drawDialogue(npc);
					if (dailyQuest.Value)
					{
						Game1.player.changeFriendship(150, npc);
					}
					else
					{
						Game1.player.changeFriendship(255, npc);
					}
					questComplete();
				}
				return true;
			}
			if (!probe)
			{
				npc.CurrentDialogue.Push(Dialogue.FromTranslation(npc, "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13615", number.Value));
				Game1.drawDialogue(npc);
			}
		}
		return result;
	}

	/// <summary>Get the gold reward for a given item.</summary>
	/// <param name="item">The item instance.</param>
	public int GetGoldRewardPerItem(Item item)
	{
		if (item is Object obj)
		{
			return obj.Price * 3;
		}
		return (int)((float)item.salePrice() * 1.5f);
	}
}
