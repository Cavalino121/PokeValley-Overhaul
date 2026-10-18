using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Extensions;
using StardewValley.GameData.MakeoverOutfits;
using StardewValley.GameData.Shops;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.Quests;
using StardewValley.SpecialOrders;
using StardewValley.TokenizableStrings;
using xTile.Dimensions;

namespace StardewValley.Locations;

public class DesertFestival : Desert
{
	public enum RaceState
	{
		PreRace,
		StartingLine,
		Ready,
		Set,
		Go,
		AnnounceWinner,
		AnnounceWinner2,
		AnnounceWinner3,
		AnnounceWinner4,
		RaceEnd,
		RacesOver
	}

	public const int CALICO_STATUE_GHOST_INVASION = 0;

	public const int CALICO_STATUE_SERPENT_INVASION = 1;

	public const int CALICO_STATUE_SKELETON_INVASION = 2;

	public const int CALICO_STATUE_BAT_INVASION = 3;

	public const int CALICO_STATUE_ASSASSIN_BUGS = 4;

	public const int CALICO_STATUE_THIN_SHELLS = 5;

	public const int CALICO_STATUE_MEAGER_MEALS = 6;

	public const int CALICO_STATUE_MONSTER_SURGE = 7;

	public const int CALICO_STATUE_SHARP_TEETH = 8;

	public const int CALICO_STATUE_MUMMY_CURSE = 9;

	public const int CALICO_STATUE_SPEED_BOOST = 10;

	public const int CALICO_STATUE_REFRESH = 11;

	public const int CALICO_STATUE_50_EGG_TREASURE = 12;

	public const int CALICO_STATUE_NO_EFFECT = 13;

	public const int CALICO_STATUE_TOOTH_FILE = 14;

	public const int CALICO_STATUE_25_EGG_TREASURE = 15;

	public const int CALICO_STATUE_10_EGG_TREASURE = 16;

	public const int CALICO_STATUE_100_EGG_TREASURE = 17;

	public static readonly int[] CalicoStatueInvasionIds = new int[4] { 3, 0, 1, 2 };

	public const int NUM_SCHOLAR_QUESTIONS = 4;

	public const string FISHING_QUEST_ID = "98765";

	protected RandomizedPlantFurniture _cactusGuyRevealItem;

	protected float _cactusGuyRevealTimer = -1f;

	protected float _cactusShakeTimer = -1f;

	protected int _currentlyShownCactusID;

	protected NetEvent1Field<int, NetInt> _revealCactusEvent = new NetEvent1Field<int, NetInt>();

	protected NetEvent1Field<int, NetInt> _hideCactusEvent = new NetEvent1Field<int, NetInt>();

	protected MoneyDial eggMoneyDial;

	[XmlIgnore]
	public NetList<Racer, NetRef<Racer>> netRacers = new NetList<Racer, NetRef<Racer>>();

	[XmlIgnore]
	protected List<Racer> _localRacers = new List<Racer>();

	[XmlIgnore]
	protected float festivalChimneyTimer;

	[XmlIgnore]
	public List<int> finishedRacers = new List<int>();

	[XmlIgnore]
	public int racerCount = 3;

	[XmlIgnore]
	public int totalRacers = 5;

	[XmlIgnore]
	public NetEvent1Field<string, NetString> announceRaceEvent = new NetEvent1Field<string, NetString>();

	[XmlIgnore]
	public NetEnum<RaceState> currentRaceState = new NetEnum<RaceState>(RaceState.PreRace);

	[XmlIgnore]
	public NetLongDictionary<int, NetInt> sabotages = new NetLongDictionary<int, NetInt>();

	[XmlIgnore]
	public NetLongDictionary<int, NetInt> raceGuesses = new NetLongDictionary<int, NetInt>();

	[XmlIgnore]
	public NetLongDictionary<int, NetInt> nextRaceGuesses = new NetLongDictionary<int, NetInt>();

	[XmlIgnore]
	public NetLongDictionary<bool, NetBool> specialRewardsCollected = new NetLongDictionary<bool, NetBool>();

	[XmlIgnore]
	public NetLongDictionary<int, NetInt> rewardsToCollect = new NetLongDictionary<int, NetInt>();

	[XmlIgnore]
	public NetInt lastRaceWinner = new NetInt();

	[XmlIgnore]
	protected float _raceStateTimer;

	protected string _raceText;

	protected float _raceTextTimer;

	protected bool _raceTextShake;

	protected int _localSabotageText = -1;

	protected int _currentScholarQuestion = -1;

	protected int _cookIngredient = -1;

	protected int _cookSauce = -1;

	public Vector3[][] raceTrack = new Vector3[16][]
	{
		new Vector3[2]
		{
			new Vector3(41f, 39f, 0f),
			new Vector3(42f, 39f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(41f, 29f, 0f),
			new Vector3(42f, 28f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(6f, 29f, 0f),
			new Vector3(5f, 28f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(6f, 35f, 0f),
			new Vector3(5f, 36f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(10f, 35f, 2f),
			new Vector3(10f, 36f, 2f)
		},
		new Vector3[2]
		{
			new Vector3(12.5f, 35f, 0f),
			new Vector3(12.5f, 36f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(17.5f, 35f, 1f),
			new Vector3(17.5f, 36f, 1f)
		},
		new Vector3[2]
		{
			new Vector3(23.5f, 35f, 0f),
			new Vector3(23.5f, 36f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(28.5f, 35f, 1f),
			new Vector3(28.5f, 36f, 1f)
		},
		new Vector3[2]
		{
			new Vector3(31f, 35f, 0f),
			new Vector3(31f, 36f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(32f, 35f, 0f),
			new Vector3(31f, 36f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(32f, 38f, 3f),
			new Vector3(31f, 38f, 3f)
		},
		new Vector3[2]
		{
			new Vector3(32f, 43f, 0f),
			new Vector3(31f, 43f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(32f, 46f, 0f),
			new Vector3(31f, 47f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(41f, 46f, 0f),
			new Vector3(42f, 47f, 0f)
		},
		new Vector3[2]
		{
			new Vector3(41f, 39f, 0f),
			new Vector3(42f, 39f, 0f)
		}
	};

	private bool checkedMineExplanation;

	public DesertFestival()
	{
		forceLoadPathLayerLights = true;
	}

	public DesertFestival(string mapPath, string name)
		: base(mapPath, name)
	{
		forceLoadPathLayerLights = true;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(_revealCactusEvent, "_revealCactusEvent").AddField(_hideCactusEvent, "_hideCactusEvent").AddField(netRacers, "netRacers")
			.AddField(announceRaceEvent, "announceRaceEvent")
			.AddField(sabotages, "sabotages")
			.AddField(raceGuesses, "raceGuesses")
			.AddField(rewardsToCollect, "rewardsToCollect")
			.AddField(specialRewardsCollected, "specialRewardsCollected")
			.AddField(nextRaceGuesses, "nextRaceGuesses")
			.AddField(lastRaceWinner, "lastRaceWinner")
			.AddField(currentRaceState, "currentRaceState");
		_revealCactusEvent.onEvent += CactusGuyRevealCactus;
		_hideCactusEvent.onEvent += CactusGuyHideCactus;
		announceRaceEvent.onEvent += AnnounceRace;
	}

	public static void SetupMerchantSchedule(NPC character, int shop_index)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (shop_index == 0)
		{
			stringBuilder.Append("/a1130 Desert 15 40 2");
		}
		else
		{
			stringBuilder.Append("/a1140 Desert 26 40 2");
		}
		stringBuilder.Append("/2400 bed");
		stringBuilder.Remove(0, 1);
		GameLocation locationFromName = Game1.getLocationFromName(character.DefaultMap);
		if (locationFromName != null)
		{
			Game1.warpCharacter(character, locationFromName, new Vector2((int)(character.DefaultPosition.X / 64f), (int)(character.DefaultPosition.Y / 64f)));
		}
		character.islandScheduleName.Value = "festival_vendor";
		character.TryLoadSchedule("desertFestival", stringBuilder.ToString());
		character.performSpecialScheduleChanges();
	}

	public override void OnCamel()
	{
		Game1.playSound("camel");
		ShowCamelAnimation();
		Game1.player.faceDirection(0);
		Game1.haltAfterCheck = false;
	}

	public override void ShowCamelAnimation()
	{
		temporarySprites.Add(new TemporaryAnimatedSprite
		{
			texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
			sourceRect = new Microsoft.Xna.Framework.Rectangle(273, 524, 65, 49),
			sourceRectStartingPos = new Vector2(273f, 524f),
			animationLength = 1,
			totalNumberOfLoops = 1,
			interval = 300f,
			scale = 4f,
			position = new Vector2(536f, 340f) * 4f,
			layerDepth = 0.1332f,
			id = 999
		});
	}

	public override void checkForMusic(GameTime time)
	{
		Game1.changeMusicTrack(GetFestivalMusic(), track_interruptable: true);
	}

	public virtual string GetFestivalMusic()
	{
		if (Utility.IsPassiveFestivalOpen("DesertFestival"))
		{
			return "event2";
		}
		return "summer_day_ambient";
	}

	public override string GetLocationSpecificMusic()
	{
		return GetFestivalMusic();
	}

	public override void digUpArtifactSpot(int xLocation, int yLocation, Farmer who)
	{
		Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation);
		Game1.createMultipleObjectDebris("CalicoEgg", xLocation, yLocation, random.Next(3, 7), who.UniqueMultiplayerID, this);
		base.digUpArtifactSpot(xLocation, yLocation, who);
	}

	public virtual void CollectRacePrizes()
	{
		List<Item> list = new List<Item>();
		if (specialRewardsCollected.TryGetValue(Game1.player.UniqueMultiplayerID, out var value) && !value)
		{
			specialRewardsCollected[Game1.player.UniqueMultiplayerID] = true;
			list.Add(ItemRegistry.Create("CalicoEgg", 100));
		}
		for (int i = 0; i < rewardsToCollect[Game1.player.UniqueMultiplayerID]; i++)
		{
			list.Add(ItemRegistry.Create("CalicoEgg", 20));
		}
		rewardsToCollect[Game1.player.UniqueMultiplayerID] = 0;
		Game1.activeClickableMenu = new ItemGrabMenu(list, reverseGrab: false, showReceivingMenu: true, null, null, "Rewards", null, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: false, allowRightClick: false, showOrganizeButton: false, 0, null, -1, this);
	}

	public override void performTouchAction(string full_action_string, Vector2 player_standing_position)
	{
		if (Game1.eventUp)
		{
			return;
		}
		if (full_action_string.Split(' ')[0] == "DesertMakeover")
		{
			if (Game1.player.controller != null)
			{
				return;
			}
			bool flag = false;
			string failMessageKey = null;
			NPC stylist = GetStylist();
			if (!flag && stylist == null)
			{
				stylist = null;
				failMessageKey = "Strings\\1_6_Strings:MakeOver_NoStylist";
				flag = true;
			}
			if (!flag && Game1.player.activeDialogueEvents.ContainsKey("DesertMakeover"))
			{
				failMessageKey = "Strings\\1_6_Strings:MakeOver_" + stylist.Name + "_AlreadyStyled";
				flag = true;
			}
			int num = 0;
			if (Game1.player.hat.Value != null)
			{
				num++;
			}
			if (Game1.player.shirtItem.Value != null)
			{
				num++;
			}
			if (Game1.player.pantsItem.Value != null)
			{
				num++;
			}
			if (!flag && Game1.player.freeSpotsInInventory() < num)
			{
				failMessageKey = "Strings\\1_6_Strings:MakeOver_" + stylist.Name + "_InventoryFull";
				flag = true;
			}
			if (flag)
			{
				Game1.freezeControls = true;
				Game1.displayHUD = false;
				int finalFacingDirection = 2;
				if (stylist != null)
				{
					finalFacingDirection = 3;
				}
				Game1.player.controller = new PathFindController(Game1.player, this, new Point(26, 52), finalFacingDirection, delegate
				{
					Game1.freezeControls = false;
					Game1.displayHUD = true;
					if (stylist != null)
					{
						stylist.faceTowardFarmerForPeriod(1000, 2, faceAway: false, Game1.player);
						if (failMessageKey != null)
						{
							Game1.DrawDialogue(stylist, failMessageKey);
						}
					}
					else if (failMessageKey != null)
					{
						Game1.drawObjectDialogue(Game1.content.LoadString(failMessageKey));
					}
				});
			}
			else
			{
				Game1.player.activeDialogueEvents["DesertMakeover"] = 0;
				Game1.freezeControls = true;
				Game1.displayHUD = false;
				Game1.player.controller = new PathFindController(Game1.player, this, new Point(27, 50), 0);
				Game1.globalFadeToBlack(delegate
				{
					Game1.freezeControls = false;
					Game1.forceSnapOnNextViewportUpdate = true;
					Event obj = new Event(GetMakeoverEvent());
					obj.onEventFinished = (Action)Delegate.Combine(obj.onEventFinished, new Action(ReceiveMakeOver));
					startEvent(obj);
					Game1.globalFadeToClear();
				});
			}
		}
		else
		{
			base.performTouchAction(full_action_string, player_standing_position);
		}
	}

	public virtual string GetMakeoverEvent()
	{
		NPC stylist = GetStylist();
		Random random = Utility.CreateDaySaveRandom(Game1.year);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("continue/26 51/farmer 27 50 2 ");
		foreach (NPC character in characters)
		{
			if (!(character.Name == stylist.Name) && !(character.Name == "Sandy"))
			{
				stringBuilder.Append(character.Name + " " + character.Tile.X + " " + character.Tile.Y + " " + character.FacingDirection + " ");
			}
		}
		if (stylist.Name == "Emily")
		{
			stringBuilder.Append("Emily 25 52 2 Sandy 22 52 2/skippable/pause 1200/speak Emily \"");
			stringBuilder.Append(Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_1"));
			stringBuilder.Append("\"/pause 100/");
			switch (random.Next(0, 3))
			{
			case 0:
				stringBuilder.Append("animate Emily false true 200 39 39/");
				break;
			case 1:
				stringBuilder.Append("animate Emily false true 300 16 17 18 19 20 21 22 23/");
				break;
			case 2:
				stringBuilder.Append("animate Emily false true 300 31 48 49/");
				break;
			}
			stringBuilder.Append("pause 1000/faceDirection Sandy 1 true/pause 2000/textAboveHead Emily \"");
			stringBuilder.Append(Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_2"));
			stringBuilder.Append("\"/pause 3000/stopAnimation Emily 2/playSound dwop/shake Emily 100/jump Emily 4/pause 300/speak Emily \"");
			stringBuilder.Append(Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_3"));
			stringBuilder.Append("\"/pause 100/advancedMove Emily false 1 0 0 -1 0 -1 0 -1 1 100/pause 100/");
			stringBuilder.Append("advancedMove Sandy false 1 0 1 0 1 0 1 0 2 100/pause 3000/playSound openChest/pause 1000/");
			List<string> list = new List<string>
			{
				string.Format("playSound dustMeep/pause 300/playSound dustMeep/pause 300/playSound dustMeep/textAboveHead Emily \"{0}\"/", Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_Reaction1")),
				string.Format("playSound rooster/playSound dwop/shake Sandy 400/jump Sandy 4/pause 500/textAboveHead Emily \"{0}\"/", Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_Reaction2")),
				string.Format("playSound slimeHit/pause 300/playSound slimeHit/pause 600/playSound slimedead/textAboveHead Emily \"{0}\"/", Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_Reaction3")),
				string.Format("textAboveHead Emily \"{0}\"/playSound trashcanlid/pause 1000/playSound trashcan/", Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_Reaction4")),
				string.Format("textAboveHead Emily \"{0}\"/pause 1000/playSound cast/pause 500/playSound axe/pause 200/playSound ow/", Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_Reaction5")),
				string.Format("textAboveHead Emily \"{0}\"/pause 1000/playSound eat/", Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_Reaction6")),
				string.Format("textAboveHead Emily \"{0}\"/playSound scissors/pause 300/playSound scissors/pause 300/playSound scissors/", Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_Reaction7")),
				string.Format("textAboveHead Emily \"{0}\"/pause 500/playSound trashbear/pause 300/playSound trashbear/pause 300/playSound trashbear/", Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_Reaction8")),
				string.Format("textAboveHead Emily \"{0}\"/pause 1000/playSound fishingRodBend/pause 500/playSound fishingRodBend/pause 1000/playSound fishingRodBend/", Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_Reaction9"))
			};
			Utility.Shuffle(random, list);
			for (int i = 0; i < 3; i++)
			{
				stringBuilder.Append("pause 500/");
				stringBuilder.Append(list[i]);
				stringBuilder.Append("pause 1500/");
			}
			stringBuilder.Append("pause 500/playSound money/textAboveHead Emily \"");
			stringBuilder.Append(Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_4"));
			stringBuilder.Append("\"/playSound dwop/shake Sandy 400/jump Sandy 4/pause 750/advancedMove Sandy false -1 0 -1 0 -1 0 -1 0 1 100/pause 2000/advancedMove Emily false 0 1 0 1 0 1 2 100/pause 2000/speak Emily \"");
			stringBuilder.Append(Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Emily_5"));
		}
		else
		{
			stringBuilder.Append("Sandy 22 52 2/skippable/pause 2000/textAboveHead Sandy \"");
			stringBuilder.Append(Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Sandy_1"));
			stringBuilder.Append("\"/");
			stringBuilder.Append("pause 1000/playSound dwop/shake Sandy 400/jump Sandy 4/textAboveHead Sandy \"");
			stringBuilder.Append(Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Sandy_2"));
			stringBuilder.Append("\"/");
			stringBuilder.Append("pause 200/advancedMove Sandy false 1 0 1 0 1 0 1 0 4 100/");
			stringBuilder.Append("pause 2500/speak Sandy \"");
			stringBuilder.Append(Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Sandy_3"));
			stringBuilder.Append("\"/");
			stringBuilder.Append("pause 500/advancedMove Sandy false 0 -1 0 -1 0 -1/pause 3000/playSound openChest/pause 1000/");
			stringBuilder.Append(string.Format("textAboveHead Sandy \"{0}\"/pause 1000/playSound fishingRodBend/pause 500/playSound fishingRodBend/pause 1000/playSound fishingRodBend/", Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Sandy_4")));
			stringBuilder.Append("pause 1500/");
			stringBuilder.Append("pause 500/playSound money/textAboveHead Sandy \"");
			stringBuilder.Append(Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Sandy_5"));
			stringBuilder.Append("\"/pause 200/advancedMove Sandy false 0 1 0 1 0 1 2 100/pause 2000/speak Sandy \"");
			stringBuilder.Append(Game1.content.LoadString("Strings\\1_6_Strings:MakeOver_Sandy_6"));
		}
		stringBuilder.Append("\"/pause 500/end");
		return stringBuilder.ToString();
	}

	private void ReceiveMakeOver()
	{
		ReceiveMakeOver(-1);
	}

	public virtual void ReceiveMakeOver(int randomSeedOverride = -1)
	{
		Random random = ((randomSeedOverride == -1) ? Utility.CreateDaySaveRandom(Game1.year) : Utility.CreateRandom(randomSeedOverride));
		if (randomSeedOverride == -1 && random.NextDouble() < 0.75)
		{
			random = Utility.CreateDaySaveRandom(Game1.year, (int)Game1.player.uniqueMultiplayerID.Value);
		}
		List<MakeoverOutfit> list = DataLoader.MakeoverOutfits(Game1.content);
		if (list == null)
		{
			return;
		}
		List<MakeoverOutfit> list2 = new List<MakeoverOutfit>(list);
		for (int i = 0; i < list2.Count; i++)
		{
			MakeoverOutfit makeoverOutfit = list2[i];
			if (makeoverOutfit.Gender.HasValue && makeoverOutfit.Gender.Value != Game1.player.Gender)
			{
				list2.RemoveAt(i);
				i--;
				continue;
			}
			bool flag = false;
			foreach (MakeoverItem outfitPart in makeoverOutfit.OutfitParts)
			{
				if (outfitPart.MatchesGender(Game1.player.Gender))
				{
					ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(outfitPart.ItemId);
					flag = Game1.player.hat.Value?.QualifiedItemId == dataOrErrorItem.QualifiedItemId || Game1.player.shirtItem.Value?.QualifiedItemId == dataOrErrorItem.QualifiedItemId;
					if (flag)
					{
						break;
					}
				}
			}
			if (flag)
			{
				list2.RemoveAt(i);
				i--;
			}
		}
		Farmer player = Game1.player;
		foreach (Item item3 in new List<Item>
		{
			player.Equip(null, player.shirtItem),
			player.Equip(null, player.pantsItem),
			player.Equip(null, player.hat)
		})
		{
			Item item = Utility.PerformSpecialItemGrabReplacement(item3);
			if (item != null && player.addItemToInventory(item) != null)
			{
				player.team.returnedDonations.Add(item);
				player.team.newLostAndFoundItems.Value = true;
			}
		}
		MakeoverOutfit makeoverOutfit2 = random.ChooseFrom(list2);
		Random random2 = Utility.CreateDaySaveRandom();
		if (Utility.GetDayOfPassiveFestival("DesertFestival") == 2 && random2.NextDouble() < 0.03)
		{
			makeoverOutfit2 = new MakeoverOutfit
			{
				OutfitParts = new List<MakeoverItem>
				{
					new MakeoverItem
					{
						ItemId = "(H)LaurelWreathCrown"
					},
					new MakeoverItem
					{
						ItemId = "(P)3",
						Color = "247 245 205"
					},
					new MakeoverItem
					{
						ItemId = "(S)1199"
					}
				}
			};
		}
		if (makeoverOutfit2?.OutfitParts == null)
		{
			return;
		}
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		foreach (MakeoverItem outfitPart2 in makeoverOutfit2.OutfitParts)
		{
			if (!outfitPart2.MatchesGender(Game1.player.Gender))
			{
				continue;
			}
			Item item2 = ItemRegistry.Create(outfitPart2.ItemId);
			if (!(item2 is Hat newItem))
			{
				if (!(item2 is Clothing clothing))
				{
					continue;
				}
				Color? color = Utility.StringToColor(outfitPart2.Color);
				if (color.HasValue)
				{
					clothing.clothesColor.Value = color.Value;
				}
				switch (clothing.clothesType.Value)
				{
				case Clothing.ClothesType.PANTS:
					if (!flag4)
					{
						player.Equip(clothing, player.pantsItem);
						flag4 = true;
					}
					break;
				case Clothing.ClothesType.SHIRT:
					if (!flag3)
					{
						player.Equip(clothing, player.shirtItem);
						flag3 = true;
					}
					break;
				}
			}
			else if (!flag2)
			{
				player.Equip(newItem, player.hat);
				flag2 = true;
			}
		}
	}

	public virtual void AfterMakeOver()
	{
		Game1.player.canOnlyWalk = false;
		Game1.freezeControls = false;
		Game1.displayHUD = true;
		NPC stylist = GetStylist();
		if (stylist != null)
		{
			Game1.DrawDialogue(stylist, "Strings\\1_6_Strings:MakeOver_" + stylist.Name + "_Done");
			stylist.faceTowardFarmerForPeriod(1000, 2, faceAway: false, Game1.player);
		}
	}

	public NPC GetStylist()
	{
		NPC characterFromName = getCharacterFromName("Emily");
		if (characterFromName != null && characterFromName.TilePoint == new Point(25, 52))
		{
			return characterFromName;
		}
		characterFromName = getCharacterFromName("Sandy");
		if (characterFromName != null && characterFromName.TilePoint == new Point(22, 52))
		{
			NPC characterFromName2 = getCharacterFromName("Emily");
			if (characterFromName2 != null && characterFromName2.islandScheduleName.Value == "festival_vendor")
			{
				return characterFromName;
			}
		}
		return null;
	}

	public static void addCalicoStatueSpeedBuff()
	{
		BuffEffects buffEffects = new BuffEffects();
		buffEffects.Speed.Value = 1f;
		Game1.player.applyBuff(new Buff("CalicoStatueSpeed", "Calico Statue", Game1.content.LoadString("Strings\\1_6_Strings:DF_Mine_CalicoStatue"), 300000, Game1.buffsIcons, 9, buffEffects, false, Game1.content.LoadString("Strings\\1_6_Strings:DF_Mine_CalicoStatue_Name_10")));
	}

	public override bool performAction(string action, Farmer who, Location tile_location)
	{
		string text = "DesertFestival";
		DataLoader.Shops(Game1.content);
		switch (action)
		{
		case "DesertFestivalMineExplanation":
			Game1.player.mailReceived.Add("Checked_DF_Mine_Explanation");
			checkedMineExplanation = true;
			Game1.multipleDialogues(new string[3]
			{
				Game1.content.LoadString("Strings\\1_6_Strings:DF_Mine_Explanation"),
				Game1.content.LoadString("Strings\\1_6_Strings:DF_Mine_Explanation_2"),
				Game1.content.LoadString("Strings\\1_6_Strings:DF_Mine_Explanation_3")
			});
			break;
		case "DesertFishingBoard":
			if (Game1.Date != who.lastDesertFestivalFishingQuest.Value)
			{
				List<Response> list = new List<Response>
				{
					new Response("Yes", Game1.content.LoadString("Strings\\1_6_Strings:Accept")),
					new Response("No", Game1.content.LoadString("Strings\\1_6_Strings:Decline"))
				};
				createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Willy_DesertFishing" + Utility.GetDayOfPassiveFestival("DesertFestival")), list.ToArray(), "Fishing_Quest");
			}
			break;
		case "DesertVendor":
		{
			Game1.player.faceDirection(0);
			if (!Utility.IsPassiveFestivalOpen(text))
			{
				return false;
			}
			Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle(tile_location.X, tile_location.Y - 1, 1, 1);
			foreach (NPC character in characters)
			{
				if (rectangle.Contains(character.TilePoint) && Utility.TryOpenShopMenu(text + "_" + character.Name, character.Name))
				{
					return true;
				}
			}
			break;
		}
		case "DesertCactusMan":
			Game1.player.faceDirection(0);
			if (!Utility.IsPassiveFestivalOpen(text))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:CactusMan_Closed"));
			}
			else if (Game1.player.isInventoryFull())
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:CactusMan_Yes_Full"));
			}
			else if (!Game1.player.mailReceived.Contains(GetCactusMail()))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:CactusMan_Intro_" + Game1.random.Next(1, 4)));
				Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:CactusMan_Question"), createYesNoResponses(), "CactusMan");
				});
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:CactusMan_Collected"));
			}
			break;
		case "DesertEggShop":
			if (!Utility.IsPassiveFestivalOpen(text))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:EggShop_Closed"));
			}
			else
			{
				Utility.TryOpenShopMenu("DesertFestival_EggShop", "Vendor");
			}
			break;
		case "DesertRacerMan":
		{
			Game1.player.faceGeneralDirection(new Vector2((float)tile_location.X + 0.5f, (float)tile_location.Y + 0.5f) * 64f);
			int value2;
			int value4;
			if (specialRewardsCollected.TryGetValue(Game1.player.UniqueMultiplayerID, out var value) && !value)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Collect_Prize_Special"));
				Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, new Game1.afterFadeFunction(CollectRacePrizes));
			}
			else if (rewardsToCollect.TryGetValue(Game1.player.UniqueMultiplayerID, out value2) && value2 > 0)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Collect_Prize"));
				Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, new Game1.afterFadeFunction(CollectRacePrizes));
			}
			else if (!Utility.IsPassiveFestivalOpen(text) && Game1.timeOfDay < 1000)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Closed"));
			}
			else if (currentRaceState.Value >= RaceState.Go && currentRaceState.Value < RaceState.AnnounceWinner4)
			{
				if (raceGuesses.TryGetValue(Game1.player.UniqueMultiplayerID, out var value3) && currentRaceState.Value == RaceState.Go)
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Guess_Already_Made", Game1.content.LoadString("Strings\\1_6_Strings:Racer_" + value3)));
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Ongoing"));
				}
			}
			else if (!CanMakeAnotherRaceGuess())
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Ended"));
			}
			else if (nextRaceGuesses.TryGetValue(Game1.player.UniqueMultiplayerID, out value4))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Guess_Already_Made", Game1.content.LoadString("Strings\\1_6_Strings:Racer_" + value4)));
			}
			else
			{
				createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Question"), createYesNoResponses(), "Race");
			}
			return true;
		}
		case "DesertShadyGuy":
			Game1.player.faceDirection(0);
			if (!Utility.IsPassiveFestivalOpen(text) && Game1.timeOfDay < 1000)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_Closed"));
			}
			if (currentRaceState.Value >= RaceState.Go && currentRaceState.Value < RaceState.AnnounceWinner4)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_Ongoing"));
			}
			else if (!CanMakeAnotherRaceGuess())
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_Ended"));
			}
			else if (sabotages.ContainsKey(Game1.player.UniqueMultiplayerID))
			{
				ShowSabotagedRaceText();
			}
			else if (!Game1.player.mailReceived.Contains("Desert_Festival_Shady_Guy"))
			{
				Game1.player.mailReceived.Add("Desert_Festival_Shady_Guy");
				Game1.multipleDialogues(new string[3]
				{
					Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_Intro"),
					Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_Intro_2"),
					Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_Intro_3")
				});
				Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy"), createYesNoResponses(), "Shady_Guy");
				});
			}
			else
			{
				createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_2nd"), createYesNoResponses(), "Shady_Guy");
			}
			return true;
		case "DesertGil":
			if (Game1.Date == who.lastGotPrizeFromGil.Value)
			{
				if (Utility.GetDayOfPassiveFestival("DesertFestival") == 3)
				{
					Game1.DrawDialogue(Game1.RequireLocation<AdventureGuild>("AdventureGuild").Gil, "Strings\\1_6_Strings:Gil_NextYear");
				}
				else
				{
					Game1.DrawDialogue(Game1.RequireLocation<AdventureGuild>("AdventureGuild").Gil, "Strings\\1_6_Strings:Gil_ComeBack");
				}
			}
			else if (Game1.player.team.highestCalicoEggRatingToday.Value == 0)
			{
				Game1.DrawDialogue(Game1.RequireLocation<AdventureGuild>("AdventureGuild").Gil, "Strings\\1_6_Strings:Gil_NoRating");
			}
			else
			{
				createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Gil_SubmitRating", Game1.player.team.highestCalicoEggRatingToday.Value + 1), createYesNoResponses(), "Gil_EggRating");
			}
			return true;
		case "DesertMarlon":
		{
			if (!Game1.player.mailReceived.Contains("Desert_Festival_Marlon"))
			{
				Game1.player.mailReceived.Add("Desert_Festival_Marlon");
				Game1.DrawDialogue(Game1.getCharacterFromName("Marlon"), "Strings\\1_6_Strings:Marlon_Intro");
				break;
			}
			bool flag = false;
			bool flag2 = false;
			if (Game1.player.team.acceptedSpecialOrderTypes.Contains("DesertFestivalMarlon"))
			{
				flag2 = true;
				foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
				{
					if (specialOrder.orderType.Value == "DesertFestivalMarlon")
					{
						flag = true;
						if (specialOrder.questState.Value == SpecialOrderStatus.InProgress || specialOrder.questState.Value == SpecialOrderStatus.Failed)
						{
							flag2 = false;
						}
						break;
					}
				}
			}
			if (flag2)
			{
				if (Utility.GetDayOfPassiveFestival("DesertFestival") < 3)
				{
					Game1.DrawDialogue(Game1.getCharacterFromName("Marlon"), "Strings\\1_6_Strings:Marlon_Challenge_Finished");
					return true;
				}
				Game1.DrawDialogue(Game1.getCharacterFromName("Marlon"), "Strings\\1_6_Strings:Marlon_Challenge_Finished_LastDay");
				return true;
			}
			if (flag)
			{
				Game1.DrawDialogue(Game1.getCharacterFromName("Marlon"), "Strings\\1_6_Strings:Marlon_Challenge_Chosen");
			}
			else
			{
				Game1.DrawDialogue(Game1.getCharacterFromName("Marlon"), "Strings\\1_6_Strings:Marlon_" + Game1.random.Next(1, 5));
			}
			Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
			{
				Game1.activeClickableMenu = new SpecialOrdersBoard("DesertFestivalMarlon");
			});
			return true;
		}
		case "DesertScholar":
			if (!Utility.IsPassiveFestivalOpen(text))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Scholar_Closed"));
				return true;
			}
			if (Game1.player.mailReceived.Contains(GetScholarMail()))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Scholar_DoneThisYear"));
				return true;
			}
			if (_currentScholarQuestion == -2)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Scholar_Failed"));
				return true;
			}
			createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Scholar_Intro"), createYesNoResponses(), "DesertScholar");
			break;
		case "DesertFood":
			Game1.player.faceDirection(0);
			createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Cook_Intro"), createYesNoResponses(), "Cook_Intro");
			break;
		}
		return base.performAction(action, who, tile_location);
	}

	public string GetCactusMail()
	{
		return "Y" + Game1.year + "_Cactus";
	}

	public string GetScholarMail()
	{
		return "Y" + Game1.year + "_Scholar";
	}

	public virtual Response[] GetRacerResponses()
	{
		List<Response> list = new List<Response>();
		foreach (Racer netRacer in netRacers)
		{
			list.Add(new Response(netRacer.racerIndex.ToString(), Game1.content.LoadString("Strings\\1_6_Strings:Racer_" + netRacer.racerIndex.Value)));
		}
		list.Add(new Response("cancel", Game1.content.LoadString("Strings\\Locations:MineCart_Destination_Cancel")));
		return list.ToArray();
	}

	public virtual void ShowSabotagedRaceText()
	{
		if (sabotages.TryGetValue(Game1.player.UniqueMultiplayerID, out var value))
		{
			if (_localSabotageText == -1)
			{
				_localSabotageText = Game1.random.Next(1, 4);
			}
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_Selected_" + _localSabotageText, Game1.content.LoadString("Strings\\1_6_Strings:Racer_" + value)));
		}
	}

	private void generateNextScholarQuestion()
	{
		Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame);
		int num = random.Next(3);
		num += Game1.year;
		num %= 3;
		string text = "Scholar_Question_" + _currentScholarQuestion + "_" + num;
		string text2 = "Scholar_Question_" + _currentScholarQuestion + "_" + num + "_Options";
		string text3 = "Scholar_Question_" + _currentScholarQuestion + "_" + num + "_Answers";
		string[] array = null;
		int num2 = 0;
		try
		{
			array = Game1.content.LoadString("Strings\\1_6_Strings:" + text2).Split(',');
			num2 = random.Next(array.Length);
		}
		catch (Exception)
		{
		}
		string[] array2 = Game1.content.LoadString("Strings\\1_6_Strings:" + text3).Split(',');
		string question = ((array != null) ? Game1.content.LoadString("Strings\\1_6_Strings:" + text, array[num2]) : Game1.content.LoadString("Strings\\1_6_Strings:" + text));
		List<Response> list = new List<Response>();
		if (_currentScholarQuestion == 2 && num == 1)
		{
			list.Add(new Response("Correct", Game1.stats.StepsTaken.ToString() ?? ""));
			list.Add(new Response("Wrong", (Game1.stats.StepsTaken * 2).ToString() ?? ""));
			list.Add(new Response("Wrong", (Game1.stats.StepsTaken / 2).ToString() ?? ""));
		}
		else
		{
			list.Add(new Response("Correct", array2[num2]));
			int num3;
			for (num3 = num2; num3 == num2; num3 = random.Next(array2.Length))
			{
			}
			list.Add(new Response("Wrong", array2[num3]));
			int num4 = num2;
			while (num4 == num2 || num4 == num3)
			{
				num4 = random.Next(array2.Length);
			}
			list.Add(new Response("Wrong", array2[num4]));
		}
		Utility.Shuffle(random, list);
		createQuestionDialogue(question, list.ToArray(), "DesertScholar_Answer_");
		_currentScholarQuestion++;
	}

	public override void customQuestCompleteBehavior(string questId)
	{
		if (questId == "98765")
		{
			switch (Utility.GetDayOfPassiveFestival("DesertFestival"))
			{
			case 1:
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("CalicoEgg", 25));
				break;
			case 2:
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("CalicoEgg", 50));
				break;
			case 3:
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("CalicoEgg", 30));
				break;
			}
		}
		base.customQuestCompleteBehavior(questId);
	}

	public override bool answerDialogueAction(string question_and_answer, string[] question_params)
	{
		switch (question_and_answer)
		{
		case null:
			return false;
		case "WarperQuestion_Yes":
			if (Game1.player.Money < 250)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:BusStop_NotEnoughMoneyForTicket"));
			}
			else
			{
				Game1.player.Money -= 250;
				Game1.player.CanMove = true;
				ItemRegistry.Create<Object>("(O)688").performUseAction(this);
				Game1.player.freezePause = 5000;
			}
			return true;
		case "Fishing_Quest_Yes":
		{
			Quest quest = null;
			quest = ((Utility.GetDayOfPassiveFestival("DesertFestival") != 3) ? ((Quest)new FishingQuest((Utility.GetDayOfPassiveFestival("DesertFestival") == 1) ? "164" : "165", (Utility.GetDayOfPassiveFestival("DesertFestival") != 1) ? 1 : 3, "Willy", Game1.content.LoadString("Strings\\1_6_Strings:Willy_Challenge"), Game1.content.LoadString("Strings\\1_6_Strings:Willy_Challenge_Description_" + Utility.GetDayOfPassiveFestival("DesertFestival")), Game1.content.LoadString("Strings\\1_6_Strings:Willy_Challenge_Return_" + Utility.GetDayOfPassiveFestival("DesertFestival")))) : ((Quest)new ItemDeliveryQuest("Willy", "GoldenBobber", Game1.content.LoadString("Strings\\1_6_Strings:Willy_Challenge"), Game1.content.LoadString("Strings\\1_6_Strings:Willy_Challenge_Description_" + Utility.GetDayOfPassiveFestival("DesertFestival")), "Strings\\1_6_Strings:Willy_GoldenBobber", Game1.content.LoadString("Strings\\1_6_Strings:Willy_Challenge_Return_" + Utility.GetDayOfPassiveFestival("DesertFestival")))));
			quest.daysLeft.Value = 1;
			quest.id.Value = "98765";
			Game1.player.questLog.Add(quest);
			Game1.player.lastDesertFestivalFishingQuest.Value = Game1.Date;
			return true;
		}
		case "Gil_EggRating_Yes":
			Game1.player.lastGotPrizeFromGil.Value = Game1.Date;
			Game1.player.freezePause = 1400;
			DelayedAction.playSoundAfterDelay("coin", 500);
			DelayedAction.functionAfterDelay(delegate
			{
				int num3 = Game1.player.team.highestCalicoEggRatingToday.Value + 1;
				int eggPrize = 0;
				Item extraPrize = null;
				if (num3 >= 1000)
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Gil_Rating_1000"));
				}
				else if (num3 >= 55)
				{
					Game1.DrawDialogue(Game1.RequireLocation<AdventureGuild>("AdventureGuild").Gil, "Strings\\1_6_Strings:Gil_Rating_50", num3);
					eggPrize = 500;
					extraPrize = new Object("279", 1);
				}
				else if (num3 >= 25)
				{
					Game1.DrawDialogue(Game1.RequireLocation<AdventureGuild>("AdventureGuild").Gil, "Strings\\1_6_Strings:Gil_Rating_25", num3);
					eggPrize = 200;
					if (!Game1.player.mailReceived.Contains("DF_Gil_Hat"))
					{
						extraPrize = new Hat("GilsHat");
						Game1.player.mailReceived.Add("DF_Gil_Hat");
					}
					else
					{
						extraPrize = new Object("253", 5);
					}
				}
				else if (num3 >= 20)
				{
					Game1.DrawDialogue(Game1.RequireLocation<AdventureGuild>("AdventureGuild").Gil, "Strings\\1_6_Strings:Gil_Rating_20to24", num3);
					eggPrize = 100;
					extraPrize = new Object("253", 5);
				}
				else if (num3 >= 15)
				{
					Game1.DrawDialogue(Game1.RequireLocation<AdventureGuild>("AdventureGuild").Gil, "Strings\\1_6_Strings:Gil_Rating_15to19", num3);
					eggPrize = 50;
					extraPrize = new Object("253", 3);
				}
				else if (num3 >= 10)
				{
					Game1.DrawDialogue(Game1.RequireLocation<AdventureGuild>("AdventureGuild").Gil, "Strings\\1_6_Strings:Gil_Rating_10to14", num3);
					eggPrize = 25;
					extraPrize = new Object("253", 1);
				}
				else if (num3 >= 5)
				{
					Game1.DrawDialogue(Game1.RequireLocation<AdventureGuild>("AdventureGuild").Gil, "Strings\\1_6_Strings:Gil_Rating_5to9", num3);
					eggPrize = 10;
					extraPrize = new Object("395", 1);
				}
				else
				{
					Game1.DrawDialogue(Game1.RequireLocation<AdventureGuild>("AdventureGuild").Gil, "Strings\\1_6_Strings:Gil_Rating_1to4", num3);
					eggPrize = 1;
					extraPrize = new Object("243", 1);
				}
				Game1.afterDialogues = delegate
				{
					Game1.player.addItemByMenuIfNecessaryElseHoldUp(new Object("CalicoEgg", eggPrize));
					if (extraPrize != null)
					{
						Game1.afterDialogues = delegate
						{
							Game1.player.addItemByMenuIfNecessary(extraPrize);
						};
					}
				};
			}, 1000);
			break;
		case "Race_Yes":
			createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Guess"), GetRacerResponses(), "Race_Guess_");
			return true;
		case "Shady_Guy_Yes":
			if (Game1.player.Items.CountId("CalicoEgg") >= 1)
			{
				Game1.player.Items.ReduceId("CalicoEgg", 1);
				createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_Question"), GetRacerResponses(), "Shady_Guy_Sabotage_");
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_NoEgg"));
			}
			break;
		case "CactusMan_Yes":
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:CactusMan_Yes_Intro"));
			Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
			{
				if (Game1.player.isInventoryFull())
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:CactusMan_Yes_Full"));
				}
				else
				{
					int seed = Utility.CreateRandomSeed(Game1.player.UniqueMultiplayerID, Game1.year);
					Game1.player.freezePause = 4000;
					DelayedAction.functionAfterDelay(delegate
					{
						_revealCactusEvent.Fire(seed);
					}, 1000);
					DelayedAction.functionAfterDelay(delegate
					{
						Random random = Utility.CreateRandom(seed);
						random.Next();
						random.Next();
						random.Next();
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:CactusMan_Yes_" + random.Next(1, 6)));
						Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
						{
							RandomizedPlantFurniture item = new RandomizedPlantFurniture("FreeCactus", Vector2.Zero, seed);
							if (Game1.player.addItemToInventoryBool(item))
							{
								Game1.playSound("coin");
								Game1.player.mailReceived.Add(GetCactusMail());
							}
							_hideCactusEvent.Fire(seed);
							Game1.player.freezePause = 100;
						});
					}, 3000);
				}
			});
			return true;
		case "CactusMan_No":
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:CactusMan_No"));
			return true;
		}
		if (question_and_answer.StartsWith("Race_Guess_"))
		{
			string s = question_and_answer.Substring("Race_Guess_".Length + 1);
			int result = -1;
			if (int.TryParse(s, out result))
			{
				if (currentRaceState.Value >= RaceState.Go && currentRaceState.Value < RaceState.AnnounceWinner4)
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Late_Guess"));
					return true;
				}
				string text = "Strings\\1_6_Strings:Racer_" + result;
				string sub = Game1.content.LoadString(text);
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Race_Guess_Made", sub));
				Game1.multiplayer.globalChatInfoMessage("GuessRacer_" + Game1.random.Next(1, 11), Game1.player.Name, TokenStringBuilder.LocalizedText(text));
				nextRaceGuesses[Game1.player.UniqueMultiplayerID] = result;
			}
			return true;
		}
		if (question_and_answer.StartsWith("Shady_Guy_Sabotage_"))
		{
			string s2 = question_and_answer.Substring("Shady_Guy_Sabotage_".Length + 1);
			int result2 = -1;
			if (int.TryParse(s2, out result2))
			{
				if (currentRaceState.Value >= RaceState.Go && currentRaceState.Value < RaceState.AnnounceWinner4)
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Shady_Guy_Late"));
					return true;
				}
				if (!sabotages.Any() && Game1.random.NextDouble() < 0.25)
				{
					Game1.multiplayer.globalChatInfoMessage("RaceSabotage_" + Game1.random.Next(1, 6));
				}
				sabotages[Game1.player.UniqueMultiplayerID] = result2;
				_localSabotageText = -1;
				ShowSabotagedRaceText();
			}
			return true;
		}
		if (question_and_answer.StartsWith("DesertScholar"))
		{
			if (question_and_answer == "DesertScholar_Yes")
			{
				_currentScholarQuestion++;
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Scholar_Intro2"));
				Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
				{
					generateNextScholarQuestion();
				});
			}
			else if (question_and_answer.StartsWith("DesertScholar_Answer_"))
			{
				if (question_and_answer == "DesertScholar_Answer__Wrong")
				{
					Game1.playSound("cancel");
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Scholar_Wrong"));
					_currentScholarQuestion = -2;
				}
				else if (question_and_answer == "DesertScholar_Answer__Correct")
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Scholar_Correct"));
					Game1.playSound("give_gift");
					if (_currentScholarQuestion == 4)
					{
						Game1.player.mailReceived.Add(GetScholarMail());
						Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Scholar_Win"));
							Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
							{
								Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("CalicoEgg", 50));
								Game1.playSound("coin");
							});
						});
					}
					else
					{
						Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
						{
							generateNextScholarQuestion();
						});
					}
				}
			}
		}
		if (question_and_answer.StartsWith("Cook"))
		{
			if (question_and_answer.EndsWith("No"))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Cook_Intro_No"));
			}
			else if (question_and_answer.StartsWith("Cook_ChoseSauce"))
			{
				Game1.playSound("smallSelect");
				_cookSauce = Convert.ToInt32(question_and_answer[question_and_answer.Length - 1].ToString() ?? "");
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Cook_ChoseSauce", Game1.content.LoadString("Strings\\1_6_Strings:Cook_Sauce" + _cookSauce)));
				Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
				{
					temporarySprites.Add(new TemporaryAnimatedSprite("Maps\\desert_festival_tilesheet", new Microsoft.Xna.Framework.Rectangle(320, 280, 29, 24), new Vector2(480f, 1372f), flipped: false, 0f, Color.White)
					{
						id = 1001,
						animationLength = 2,
						interval = 200f,
						totalNumberOfLoops = 9999,
						scale = 4f,
						layerDepth = 0.1343f
					});
					temporarySprites.Add(new TemporaryAnimatedSprite("Maps\\desert_festival_tilesheet", new Microsoft.Xna.Framework.Rectangle(378, 280, 29, 24), new Vector2(480f, 1372f), flipped: false, 0f, Color.White)
					{
						id = 1002,
						animationLength = 4,
						interval = 100f,
						totalNumberOfLoops = 4,
						delayBeforeAnimationStart = 400,
						scale = 4f,
						layerDepth = 0.1344f
					});
					DelayedAction.playSoundAfterDelay("hammer", 800, this);
					DelayedAction.playSoundAfterDelay("hammer", 1200, this);
					DelayedAction.playSoundAfterDelay("hammer", 1600, this);
					DelayedAction.playSoundAfterDelay("hammer", 2000, this);
					DelayedAction.playSoundAfterDelay("furnace", 2500, this);
					for (int i = 0; i < 12; i++)
					{
						temporarySprites.Add(new TemporaryAnimatedSprite(30, new Vector2(460.8f + (float)Game1.random.Next(-10, 10), 1388 + Game1.random.Next(-10, 10)), Color.White, 4, flipped: false, 100f, 2)
						{
							delayBeforeAnimationStart = 2700 + i * 80,
							motion = new Vector2(-1f + (float)Game1.random.Next(-5, 5) / 10f, -1f + (float)Game1.random.Next(-5, 5) / 10f),
							drawAboveAlwaysFront = true
						});
						temporarySprites.Add(new TemporaryAnimatedSprite(30, new Vector2(544f + (float)Game1.random.Next(-10, 10), 1388 + Game1.random.Next(-10, 10)), Color.White, 4, flipped: false, 100f, 2)
						{
							delayBeforeAnimationStart = 2700 + i * 80,
							motion = new Vector2(1f + (float)Game1.random.Next(-5, 5) / 10f, -1f + (float)Game1.random.Next(-5, 5) / 10f),
							drawAboveAlwaysFront = true
						});
						if (i % 2 == 0)
						{
							temporarySprites.Add(new TemporaryAnimatedSprite("Tilesheets\\Animations", new Microsoft.Xna.Framework.Rectangle(0, 2944, 64, 64), new Vector2(505.6f + (float)Game1.random.Next(-16, 16), 1344f), Game1.random.NextDouble() < 0.5, 0f, Color.Gray)
							{
								delayBeforeAnimationStart = 2700 + i * 80,
								motion = new Vector2(0f, -0.25f),
								animationLength = 8,
								interval = 70f,
								drawAboveAlwaysFront = true
							});
						}
					}
					Game1.player.freezePause = 4805;
					DelayedAction.functionAfterDelay(delegate
					{
						removeTemporarySpritesWithID(1001);
						removeTemporarySpritesWithID(1002);
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Cook_Done", Game1.content.LoadString("Strings\\1_6_Strings:Cook_DishNames_" + _cookIngredient + "_" + _cookSauce)));
						Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
						{
							Object food = new Object();
							food.edibility.Value = Game1.player.maxHealth;
							string text2 = "Strings\\1_6_Strings:Cook_DishNames_" + _cookIngredient + "_" + _cookSauce;
							food.name = Game1.content.LoadString(text2);
							food.displayNameFormat = "[LocalizedText " + text2 + "]";
							BuffEffects effects = new BuffEffects();
							switch (_cookIngredient)
							{
							case 0:
								effects.Defense.Value = 3f;
								break;
							case 1:
								effects.MiningLevel.Value = 3f;
								break;
							case 2:
								effects.LuckLevel.Value = 3f;
								break;
							case 3:
								effects.Attack.Value = 3f;
								break;
							case 4:
								effects.FishingLevel.Value = 3f;
								break;
							}
							switch (_cookSauce)
							{
							case 0:
								effects.Defense.Value = 1f;
								break;
							case 1:
								effects.MiningLevel.Value = 1f;
								break;
							case 2:
								effects.LuckLevel.Value = 1f;
								break;
							case 3:
								effects.Attack.Value = 1f;
								break;
							case 4:
								effects.Speed.Value = 1f;
								break;
							}
							food.customBuff = () => new Buff("DesertFestival", food.Name, food.Name, 600 * Game1.realMilliSecondsPerGameMinute, null, -1, effects);
							int sourceIndex = _cookIngredient * 4 + _cookSauce + ((_cookSauce > _cookIngredient) ? (-1) : 0);
							Game1.player.tempFoodItemTextureName.Value = "TileSheets\\Objects_2";
							Game1.player.tempFoodItemSourceRect.Value = Utility.getSourceRectWithinRectangularRegion(0, 32, 128, sourceIndex, 16, 16);
							Game1.player.faceDirection(2);
							Game1.player.eatObject(food);
						});
					}, 4800);
				});
			}
			else if (question_and_answer.StartsWith("Cook_PickedIngredient"))
			{
				Game1.playSound("smallSelect");
				_cookIngredient = Convert.ToInt32(question_and_answer[question_and_answer.Length - 1].ToString() ?? "");
				List<Response> list = new List<Response>();
				for (int num = 0; num < 5; num++)
				{
					if (num != _cookIngredient || _cookIngredient == 4)
					{
						list.Add(new Response(num.ToString() ?? "", Game1.content.LoadString("Strings\\1_6_Strings:Cook_Sauce" + num)));
					}
				}
				createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Cook_ChoseIngredient", Game1.content.LoadString("Strings\\1_6_Strings:Cook_Ingredient" + _cookIngredient)), list.ToArray(), "Cook_ChoseSauce");
			}
			else if (!(question_and_answer == "Cook_Intro_Yes"))
			{
				if (question_and_answer == "Cook_Intro2_Yes")
				{
					Game1.playSound("smallSelect");
					Response[] array = new Response[5];
					for (int num2 = 0; num2 < 5; num2++)
					{
						array[num2] = new Response(num2.ToString() ?? "", Game1.content.LoadString("Strings\\1_6_Strings:Cook_Ingredient" + num2));
					}
					createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Cook_Intro_Yes3"), array, "Cook_PickedIngredient");
				}
			}
			else
			{
				Game1.playSound("smallSelect");
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Cook_Intro_Yes"));
				Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate
				{
					Game1.playSound("smallSelect");
					createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:Cook_Intro_Yes2"), createYesNoResponses(), "Cook_Intro2");
				});
			}
		}
		return base.answerDialogueAction(question_and_answer, question_params);
	}

	public void CactusGuyHideCactus(int seed)
	{
		if (_currentlyShownCactusID == seed)
		{
			_cactusGuyRevealItem = null;
			_cactusGuyRevealTimer = -1f;
			_cactusShakeTimer = -1f;
			_currentlyShownCactusID = -1;
		}
	}

	public void CactusGuyRevealCactus(int seed)
	{
		RandomizedPlantFurniture randomizedPlantFurniture = new RandomizedPlantFurniture("FreeCactus", Vector2.Zero, seed);
		_currentlyShownCactusID = seed;
		_cactusGuyRevealItem = randomizedPlantFurniture.getOne() as RandomizedPlantFurniture;
		_cactusGuyRevealTimer = 0f;
		_cactusShakeTimer = -1f;
		Random random = Utility.CreateRandom(seed);
		random.Next();
		random.Next();
		List<string> options = new List<string> { "pig", "Duck", "dog_bark", "cat", "camel" };
		Game1.playSound("throwDownITem");
		DelayedAction.playSoundAfterDelay("thudStep", 500);
		DelayedAction.playSoundAfterDelay("thudStep", 750);
		DelayedAction.playSoundAfterDelay(random.ChooseFrom(options), 1000);
		DelayedAction.functionAfterDelay(delegate
		{
			_cactusShakeTimer = 0.25f;
		}, 1000);
	}

	public bool CanMakeAnotherRaceGuess()
	{
		if (Game1.timeOfDay >= 2200 && currentRaceState.Value >= RaceState.Go)
		{
			return false;
		}
		return true;
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		if (_cactusShakeTimer > 0f)
		{
			_cactusShakeTimer -= (float)time.ElapsedGameTime.TotalSeconds;
			if (_cactusShakeTimer <= 0f)
			{
				_cactusShakeTimer = -1f;
			}
		}
		if (_raceTextTimer > 0f)
		{
			_raceTextTimer -= (float)time.ElapsedGameTime.TotalSeconds;
			if (_raceTextTimer < 0f)
			{
				_raceTextTimer = 0f;
			}
		}
		if (_cactusGuyRevealTimer >= 0f && _cactusGuyRevealTimer < 1f)
		{
			_cactusGuyRevealTimer += (float)time.ElapsedGameTime.TotalSeconds / 0.75f;
			if (_cactusGuyRevealTimer >= 1f)
			{
				_cactusGuyRevealTimer = 1f;
			}
		}
		_revealCactusEvent.Poll();
		_hideCactusEvent.Poll();
		announceRaceEvent.Poll();
		if (Game1.shouldTimePass())
		{
			if (Game1.IsMasterGame)
			{
				if (_raceStateTimer >= 0f)
				{
					_raceStateTimer -= (float)time.ElapsedGameTime.TotalSeconds;
					if (_raceStateTimer <= 0f)
					{
						_raceStateTimer = 0f;
						switch (currentRaceState.Value)
						{
						case RaceState.StartingLine:
							announceRaceEvent.Fire("Race_Ready");
							_raceStateTimer = 3f;
							currentRaceState.Value = RaceState.Ready;
							break;
						case RaceState.Ready:
							currentRaceState.Value = RaceState.Set;
							announceRaceEvent.Fire("Race_Set");
							_raceStateTimer = 3f;
							break;
						case RaceState.Set:
							currentRaceState.Value = RaceState.Go;
							announceRaceEvent.Fire("Race_Go");
							raceGuesses.Clear();
							foreach (KeyValuePair<long, int> pair in nextRaceGuesses.Pairs)
							{
								raceGuesses[pair.Key] = pair.Value;
							}
							nextRaceGuesses.Clear();
							foreach (Racer netRacer in netRacers)
							{
								netRacer.sabotages.Value = 0;
								foreach (int value in sabotages.Values)
								{
									if (value == netRacer.racerIndex.Value)
									{
										netRacer.sabotages.Value++;
									}
								}
								netRacer.ResetMoveSpeed();
							}
							sabotages.Clear();
							_raceStateTimer = 3f;
							break;
						case RaceState.AnnounceWinner:
						case RaceState.AnnounceWinner2:
						case RaceState.AnnounceWinner3:
						case RaceState.AnnounceWinner4:
							_raceStateTimer = 2f;
							switch (currentRaceState.Value)
							{
							case RaceState.AnnounceWinner:
								announceRaceEvent.Fire("Race_Comment_" + Game1.random.Next(1, 5));
								_raceStateTimer = 4f;
								break;
							case RaceState.AnnounceWinner2:
								announceRaceEvent.Fire("Race_Winner");
								_raceStateTimer = 2f;
								break;
							case RaceState.AnnounceWinner3:
								announceRaceEvent.Fire("Racer_" + lastRaceWinner.Value);
								_raceStateTimer = 4f;
								break;
							case RaceState.AnnounceWinner4:
								announceRaceEvent.Fire("RESULT");
								_raceStateTimer = 2f;
								finishedRacers.Clear();
								break;
							}
							currentRaceState.Value++;
							break;
						case RaceState.RaceEnd:
							if (!CanMakeAnotherRaceGuess())
							{
								if (Utility.GetDayOfPassiveFestival("DesertFestival") < 3)
								{
									announceRaceEvent.Fire("Race_Close");
								}
								else
								{
									announceRaceEvent.Fire("Race_Close_LastDay");
								}
								currentRaceState.Value = RaceState.RacesOver;
							}
							else
							{
								currentRaceState.Value = RaceState.PreRace;
							}
							break;
						}
					}
				}
				if (currentRaceState.Value == RaceState.Go)
				{
					if (finishedRacers.Count >= racerCount)
					{
						currentRaceState.Value = RaceState.AnnounceWinner;
						_raceStateTimer = 2f;
					}
					else
					{
						foreach (Racer netRacer2 in netRacers)
						{
							netRacer2.UpdateRaceProgress(this);
						}
					}
				}
			}
			foreach (Racer netRacer3 in netRacers)
			{
				netRacer3.Update(this);
			}
		}
		festivalChimneyTimer -= time.ElapsedGameTime.Milliseconds;
		if (festivalChimneyTimer <= 0f)
		{
			AddSmokePuff(new Vector2(7.25f, 16.25f) * 64f);
			AddSmokePuff(new Vector2(28.25f, 6f) * 64f);
			festivalChimneyTimer = 500f;
		}
		if (Game1.isStartingToGetDarkOut(this) && Game1.outdoorLight.R > 160)
		{
			Game1.outdoorLight.R = 160;
			Game1.outdoorLight.G = 160;
			Game1.outdoorLight.B = 0;
		}
		base.UpdateWhenCurrentLocation(time);
	}

	public void OnRaceWon(int winner)
	{
		lastRaceWinner.Value = winner;
		if (raceGuesses.FieldDict.Count <= 0)
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<long, int> pair in raceGuesses.Pairs)
		{
			if (pair.Value != winner)
			{
				continue;
			}
			if (winner == 3 && !specialRewardsCollected.ContainsKey(pair.Key))
			{
				specialRewardsCollected[pair.Key] = false;
				continue;
			}
			if (!rewardsToCollect.ContainsKey(pair.Key))
			{
				rewardsToCollect[pair.Key] = 0;
			}
			rewardsToCollect[pair.Key]++;
			Farmer player = Game1.GetPlayer(pair.Key);
			if (player != null)
			{
				list.Add(player.Name);
			}
		}
		string text = TokenStringBuilder.LocalizedText("Strings\\1_6_Strings:Racer_" + winner);
		switch (list.Count)
		{
		case 0:
			Game1.multiplayer.globalChatInfoMessage("RaceWinners_Zero", text);
			return;
		case 1:
			Game1.multiplayer.globalChatInfoMessage("RaceWinners_One", text, list[0]);
			return;
		case 2:
			Game1.multiplayer.globalChatInfoMessage("RaceWinners_Two", text, list[0], list[1]);
			return;
		}
		Game1.multiplayer.globalChatInfoMessage("RaceWinners_Many", text);
		for (int i = 0; i < list.Count; i++)
		{
			if (i < list.Count - 1)
			{
				Game1.multiplayer.globalChatInfoMessage("RaceWinners_List", list[i]);
			}
			else
			{
				Game1.multiplayer.globalChatInfoMessage("RaceWinners_Final", list[i]);
			}
		}
	}

	public void AddSmokePuff(Vector2 v)
	{
		temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), v, flipped: false, 0.002f, Color.Gray)
		{
			alpha = 0.75f,
			motion = new Vector2(0f, -0.5f),
			acceleration = new Vector2(0.002f, 0f),
			interval = 99999f,
			layerDepth = 1f,
			scale = 2f,
			scaleChange = 0.02f,
			drawAboveAlwaysFront = true,
			rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f
		});
	}

	public static void CleanupFestival()
	{
		Game1.player.team.itemsToRemoveOvernight.Add("CalicoEgg");
		SpecialOrder.RemoveAllSpecialOrders("DesertFestivalMarlon");
	}

	public override void draw(SpriteBatch spriteBatch)
	{
		if (_cactusGuyRevealTimer > 0f && _cactusGuyRevealItem != null)
		{
			Vector2 vector = new Vector2(29f, 66.5f) * 64f;
			Vector2 vector2 = new Vector2(27.5f, 66.5f) * 64f;
			float num = 0f;
			float num2 = 0.6f;
			num = ((!(_cactusGuyRevealTimer < num2)) ? ((float)Math.Sin((double)((_cactusGuyRevealTimer - num2) / (1f - num2)) * Math.PI) * 8f * 4f) : ((float)Math.Sin((double)(_cactusGuyRevealTimer / num2) * Math.PI) * 16f * 4f));
			Vector2 vector3 = new Vector2(Utility.Lerp(vector.X, vector2.X, _cactusGuyRevealTimer), Utility.Lerp(vector.Y, vector2.Y, _cactusGuyRevealTimer));
			float y = vector3.Y;
			if (_cactusShakeTimer > 0f)
			{
				vector3.X += Game1.random.Next(-1, 2);
				vector3.Y += Game1.random.Next(-1, 2);
			}
			_cactusGuyRevealItem.DrawFurniture(spriteBatch, Game1.GlobalToLocal(Game1.viewport, vector3 + new Vector2(0f, 0f - num)), 1f, new Vector2(8f, 16f), 4f, y / 10000f);
			spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, vector3), null, Color.White * 0.75f, 0f, new Vector2(Game1.shadowTexture.Width / 2, Game1.shadowTexture.Height / 2), new Vector2(4f, 4f), SpriteEffects.None, y / 10000f - 1E-07f);
		}
		foreach (Racer localRacer in _localRacers)
		{
			if (!localRacer.drawAboveMap.Value)
			{
				localRacer.Draw(spriteBatch);
			}
		}
		if (Game1.Date != Game1.player.lastDesertFestivalFishingQuest.Value)
		{
			float num3 = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(984f, 842f + num3)), new Microsoft.Xna.Framework.Rectangle(395, 497, 3, 8), Color.White, 0f, new Vector2(1f, 4f), 4f + Math.Max(0f, 0.25f - num3 / 16f), SpriteEffects.None, 1f);
		}
		if (!checkedMineExplanation)
		{
			float num4 = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(609.6f, 320f + num4)), new Microsoft.Xna.Framework.Rectangle(395, 497, 3, 8), Color.White, 0f, new Vector2(1f, 4f), 4f + Math.Max(0f, 0.25f - num4 / 16f), SpriteEffects.None, 1f);
		}
		if (Game1.timeOfDay < 1000)
		{
			spriteBatch.Draw(Game1.mouseCursors_1_6, Game1.GlobalToLocal(new Vector2(45f, 14f) * 64f + new Vector2(7f, 9f) * 4f), new Microsoft.Xna.Framework.Rectangle(239, 317, 16, 17), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.096f);
		}
		base.draw(spriteBatch);
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		switch (getTileIndexAt(tileLocation, "Buildings", "desert-festival"))
		{
		case 796:
		case 797:
			Utility.TryOpenShopMenu("Traveler", this);
			return true;
		case 792:
		case 793:
			playSound("pig");
			return true;
		case 1073:
			createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:BeachNightMarket_WarperQuestion"), createYesNoResponses(), "WarperQuestion");
			return true;
		default:
			return base.checkAction(tileLocation, viewport, who);
		}
	}

	public override void drawOverlays(SpriteBatch b)
	{
		SpecialCurrencyDisplay.Draw(b, new Vector2(16f, 0f), eggMoneyDial, Game1.player.Items.CountId("CalicoEgg"), Game1.mouseCursors_1_6, new Microsoft.Xna.Framework.Rectangle(0, 21, 0, 0));
		base.drawOverlays(b);
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch sb)
	{
		base.drawAboveAlwaysFrontLayer(sb);
		_localRacers.Sort((Racer a, Racer b) => a.position.Y.CompareTo(b.position.Y));
		foreach (Racer localRacer in _localRacers)
		{
			if (localRacer.drawAboveMap.Value)
			{
				localRacer.Draw(sb);
			}
		}
		if (_raceTextTimer > 0f && _raceText != null)
		{
			Vector2 vector = Game1.GlobalToLocal(new Vector2(44.5f, 39.5f) * 64f);
			if (_raceTextShake)
			{
				vector += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			}
			float alpha = Utility.Clamp(_raceTextTimer / 0.25f, 0f, 1f);
			SpriteText.drawStringWithScrollCenteredAt(sb, _raceText, (int)vector.X, (int)vector.Y - 192, "", alpha, null, 1, vector.Y / 10000f + 0.001f);
		}
	}

	public Vector3 GetTrackPosition(int track_index, float horizontal_position)
	{
		Vector2 vector = new Vector2(raceTrack[track_index][0].X + 0.5f, raceTrack[track_index][0].Y + 0.5f);
		Vector2 vector2 = new Vector2(raceTrack[track_index][1].X + 0.5f, raceTrack[track_index][1].Y + 0.5f);
		_ = vector == vector2;
		Vector2 vector3 = vector2 - vector;
		vector3.Normalize();
		vector *= 64f;
		vector2 *= 64f;
		vector -= vector3 * 64f / 4f;
		vector2 += vector3 * 64f / 4f;
		return new Vector3(Utility.Lerp(vector.X, vector2.X, horizontal_position), Utility.Lerp(vector.Y, vector2.Y, horizontal_position), raceTrack[track_index][0].Z);
	}

	public override void performTenMinuteUpdate(int timeOfDay)
	{
		string festivalId = "DesertFestival";
		base.performTenMinuteUpdate(timeOfDay);
		if (Game1.IsMasterGame && Utility.IsPassiveFestivalOpen(festivalId) && timeOfDay % 200 == 0 && timeOfDay < 2400 && currentRaceState.Value == RaceState.PreRace)
		{
			announceRaceEvent.Fire("Race_Begin");
			currentRaceState.Value = RaceState.StartingLine;
			if (nextRaceGuesses.FieldDict.Count > 0)
			{
				Game1.multiplayer.globalChatInfoMessage("RaceStarting");
			}
			_raceStateTimer = 5f;
		}
	}

	public virtual void AnnounceRace(string text)
	{
		_raceTextShake = false;
		_raceTextTimer = 2f;
		if (text == "Race_Go" || text == "Race_Finish" || text.StartsWith("Racer_"))
		{
			_raceTextShake = true;
		}
		if (text.StartsWith("Race_Close"))
		{
			_raceTextTimer = 4f;
		}
		if (text == "RESULT")
		{
			_raceTextTimer = 4f;
			if (raceGuesses.TryGetValue(Game1.player.UniqueMultiplayerID, out var value))
			{
				if (lastRaceWinner.Value == value)
				{
					_raceText = Game1.content.LoadString("Strings\\1_6_Strings:Race_Win");
				}
				else
				{
					_raceText = Game1.content.LoadString("Strings\\1_6_Strings:Race_Lose");
				}
			}
		}
		else
		{
			_raceText = Game1.content.LoadString("Strings\\1_6_Strings:" + text);
			if (text.StartsWith("Racer_"))
			{
				_raceText += "!";
			}
		}
	}

	public override void DayUpdate(int dayOfMonth)
	{
		base.DayUpdate(dayOfMonth);
		Game1.player.team.calicoEggSkullCavernRating.Value = 0;
		Game1.player.team.highestCalicoEggRatingToday.Value = 0;
		Game1.player.team.calicoStatueEffects.Clear();
		MineShaft.totalCalicoStatuesActivatedToday = 0;
		finishedRacers.Clear();
		lastRaceWinner.Value = -1;
		rewardsToCollect.Clear();
		specialRewardsCollected.Clear();
		raceGuesses.Clear();
		nextRaceGuesses.Clear();
		sabotages.Clear();
		currentRaceState.Value = RaceState.PreRace;
		_raceStateTimer = 0f;
		_currentScholarQuestion = -1;
	}

	public override void cleanupBeforePlayerExit()
	{
		_localRacers.Clear();
		_cactusGuyRevealTimer = -1f;
		_cactusGuyRevealItem = null;
		base.cleanupBeforePlayerExit();
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		if (Game1.player.mailReceived.Contains("Checked_DF_Mine_Explanation"))
		{
			checkedMineExplanation = true;
		}
		_localRacers.Clear();
		_localRacers.AddRange(netRacers);
		if (critters == null)
		{
			critters = new List<Critter>();
		}
		for (int i = 0; i < 8; i++)
		{
			critters.Add(new Butterfly(this, getRandomTile(), islandButterfly: false, forceSummerButterfly: true));
		}
		eggMoneyDial = new MoneyDial(4, playSound: false);
		eggMoneyDial.currentValue = Game1.player.Items.CountId("CalicoEgg");
	}

	public static void SetupFestivalDay()
	{
		string festival_id = "DesertFestival";
		int day_number = Utility.GetDayOfPassiveFestival(festival_id);
		Dictionary<string, ShopData> store_data_sheet = DataLoader.Shops(Game1.content);
		List<NPC> allVillagers = Utility.getAllVillagers();
		allVillagers.RemoveAll((NPC character) => !store_data_sheet.ContainsKey(festival_id + "_" + character.Name) || (character.Name == "Leo" && !Game1.MasterPlayer.mailReceived.Contains("leoMoved")) || character.getMasterScheduleRawData().ContainsKey(festival_id + "_" + day_number));
		Random random = Utility.CreateDaySaveRandom();
		for (int num = 0; num < day_number - 1; num++)
		{
			for (int num2 = 0; num2 < 2; num2++)
			{
				NPC item = random.ChooseFrom(allVillagers);
				allVillagers.Remove(item);
				if (allVillagers.Count == 0)
				{
					break;
				}
			}
		}
		if (allVillagers.Count > 0)
		{
			NPC nPC = random.ChooseFrom(allVillagers);
			allVillagers.Remove(nPC);
			SetupMerchantSchedule(nPC, 0);
		}
		if (allVillagers.Count > 0)
		{
			NPC nPC2 = random.ChooseFrom(allVillagers);
			allVillagers.Remove(nPC2);
			SetupMerchantSchedule(nPC2, 1);
		}
		if (Game1.getLocationFromName("DesertFestival") is DesertFestival desertFestival)
		{
			desertFestival.netRacers.Clear();
			List<int> list = new List<int>();
			for (int num3 = 0; num3 < desertFestival.totalRacers; num3++)
			{
				list.Add(num3);
			}
			for (int num4 = 0; num4 < desertFestival.racerCount; num4++)
			{
				int num5 = random.ChooseFrom(list);
				list.Remove(num5);
				Racer racer = new Racer(num5);
				racer.position.Value = new Vector2(44.5f, 37.5f - (float)num4) * 64f;
				racer.segmentStart = racer.position.Value;
				racer.segmentEnd = racer.position.Value;
				desertFestival.netRacers.Add(racer);
			}
		}
		SpecialOrder.UpdateAvailableSpecialOrders("DesertFestivalMarlon", forceRefresh: true);
	}
}
