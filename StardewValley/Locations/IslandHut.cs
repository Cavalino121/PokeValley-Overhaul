using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Tools;
using xTile.Dimensions;

namespace StardewValley.Locations;

public class IslandHut : IslandLocation
{
	public NetBool treeNutObtained = new NetBool();

	[XmlIgnore]
	public NetEvent0 hitTreeEvent = new NetEvent0();

	[XmlIgnore]
	public NetEvent0 parrotBoyEvent = new NetEvent0();

	[XmlIgnore]
	public bool treeHitLocal;

	[XmlElement("firstParrotDone")]
	public readonly NetBool firstParrotDone = new NetBool();

	[XmlIgnore]
	public List<string> hintDialogues = new List<string>();

	[XmlElement("hintForToday")]
	public NetString hintForToday = new NetString(null);

	[XmlIgnore]
	public float hintShowTime = -1f;

	[XmlIgnore]
	public float hintShakeTime = -1f;

	public override void draw(SpriteBatch b)
	{
		if (treeHitLocal)
		{
			b.Draw(Game1.mouseCursors2, Game1.GlobalToLocal(Game1.viewport, new Vector2(10f, 7f) * 64f), new Microsoft.Xna.Framework.Rectangle(16, 192, 16, 32), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
		}
		base.draw(b);
	}

	/// <inheritdoc />
	public override bool performAction(string[] action, Farmer who, Location tileLocation)
	{
		if (ArgUtility.Get(action, 0) == "Parrot")
		{
			ShowNutHint();
			return true;
		}
		return base.performAction(action, who, tileLocation);
	}

	public virtual int ShowNutHint()
	{
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
		int running_total = 0;
		int running_total2 = 0;
		if (MissingTheseNuts(ref running_total2, "Bush_IslandNorth_13_33", "Bush_IslandNorth_5_30"))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_VolcanoLava", 0));
		}
		bool flag = Game1.MasterPlayer.hasOrWillReceiveMail("Island_UpgradeBridge");
		int running_total3 = 0;
		if (MissingTheseNuts(ref running_total3, "Buried_IslandNorth_19_39") && flag)
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_BuriedArch", 0));
		}
		MissingTheseNuts(ref running_total2, "Bush_IslandNorth_4_42");
		MissingTheseNuts(ref running_total2, "Bush_IslandNorth_45_38", "Bush_IslandNorth_47_40");
		bool flag2 = false;
		if (MissingTheseNuts(ref running_total, "IslandLeftPlantRestored", "IslandRightPlantRestored", "IslandBatRestored", "IslandFrogRestored"))
		{
			flag2 = true;
		}
		if (MissingTheseNuts(ref running_total, "IslandCenterSkeletonRestored"))
		{
			running_total += 5;
			flag2 = true;
		}
		if (MissingTheseNuts(ref running_total, "IslandSnakeRestored"))
		{
			running_total += 2;
			flag2 = true;
		}
		if (flag2 && Utility.doesAnyFarmerHaveOrWillReceiveMail("islandNorthCaveOpened"))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_Arch", 0));
		}
		if (MissingTheseNuts(ref running_total3, "Buried_IslandNorth_19_13", "Buried_IslandNorth_57_79", "Buried_IslandNorth_54_21", "Buried_IslandNorth_42_77", "Buried_IslandNorth_62_54", "Buried_IslandNorth_26_81"))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_NorthBuried", running_total3));
		}
		MissingTheseNuts(ref running_total2, "Bush_IslandNorth_20_26", "Bush_IslandNorth_9_84");
		MissingTheseNuts(ref running_total2, "Bush_IslandNorth_56_27");
		MissingTheseNuts(ref running_total2, "Bush_IslandSouth_31_5");
		running_total2 += running_total3;
		if (running_total2 > 0)
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_NorthHidden", running_total2));
		}
		running_total += running_total2;
		if (MissingTheseNuts(ref running_total, "TreeNut"))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_HutTree", 0));
		}
		bool flag3 = Game1.MasterPlayer.hasOrWillReceiveMail("Island_Turtle");
		int running_total4 = 0;
		if (MissingTheseNuts(ref running_total4, "IslandWestCavePuzzle"))
		{
			running_total4 += 2;
		}
		MissingTheseNuts(ref running_total4, "SandDuggy");
		if (MissingLimitedNutDrops(ref running_total4, "TigerSlimeNut") && flag3)
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_TigerSlime", 0));
		}
		int running_total5 = 0;
		if (MissingTheseNuts(ref running_total5, "Buried_IslandWest_21_81", "Buried_IslandWest_62_76", "Buried_IslandWest_39_24", "Buried_IslandWest_88_14", "Buried_IslandWest_43_74", "Buried_IslandWest_30_75"))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_WestBuried", running_total5));
		}
		running_total4 += running_total5;
		int running_total6 = 0;
		if (MissingLimitedNutDrops(ref running_total6, "MusselStone", 5) && flag3)
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_MusselStone", running_total6));
		}
		running_total += running_total6;
		bool flag4 = Game1.MasterPlayer.hasOrWillReceiveMail("Island_UpgradeHouse");
		int running_total7 = 0;
		if (MissingLimitedNutDrops(ref running_total7, "IslandFarming", 5) && flag4)
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_IslandFarming", running_total7));
		}
		MissingTheseNuts(ref running_total4, "Bush_IslandWest_104_3", "Bush_IslandWest_31_24", "Bush_IslandWest_38_56", "Bush_IslandWest_75_29", "Bush_IslandWest_64_30");
		MissingTheseNuts(ref running_total4, "Bush_IslandWest_54_18", "Bush_IslandWest_25_30", "Bush_IslandWest_15_3");
		running_total += running_total7;
		running_total += running_total4;
		if (running_total4 > 0 && flag3)
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_WestHidden", running_total4));
		}
		int running_total8 = 0;
		if (MissingLimitedNutDrops(ref running_total8, "IslandFishing", 5))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_IslandFishing", running_total8));
		}
		running_total += running_total8;
		int running_total9 = 0;
		MissingLimitedNutDrops(ref running_total9, "VolcanoNormalChest");
		MissingLimitedNutDrops(ref running_total9, "VolcanoRareChest");
		if (running_total9 > 0)
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_VolcanoTreasure", running_total9));
		}
		running_total += running_total9;
		int running_total10 = 0;
		if (MissingLimitedNutDrops(ref running_total10, "VolcanoBarrel", 5))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_VolcanoBarrel", running_total10));
		}
		running_total += running_total10;
		int running_total11 = 0;
		if (MissingLimitedNutDrops(ref running_total11, "VolcanoMining", 5))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_VolcanoMining", running_total11));
		}
		running_total += running_total11;
		int running_total12 = 0;
		if (MissingLimitedNutDrops(ref running_total12, "VolcanoMonsterDrop", 5))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_VolcanoMonsters", running_total12));
		}
		running_total += running_total12;
		int running_total13 = 0;
		MissingLimitedNutDrops(ref running_total13, "Island_N_BuriedTreasureNut");
		MissingLimitedNutDrops(ref running_total13, "Island_W_BuriedTreasureNut");
		MissingLimitedNutDrops(ref running_total13, "Island_W_BuriedTreasureNut2");
		if (MissingTheseNuts(ref running_total13, "Mermaid"))
		{
			running_total13 += 4;
		}
		MissingTheseNuts(ref running_total13, "TreeNutShot");
		if (running_total13 > 0 && Utility.HasAnyPlayerSeenSecretNote(GameLocation.JOURNAL_INDEX + 1))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_Journal", running_total13));
		}
		running_total += running_total13;
		bool flag5 = Game1.MasterPlayer.hasOrWillReceiveMail("Island_Resort");
		int running_total14 = 0;
		if (MissingTheseNuts(ref running_total14, "Buried_IslandSouthEastCave_36_26", "Buried_IslandSouthEast_25_17") && flag5)
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_SouthEastBuried", running_total14));
		}
		running_total += running_total14;
		if (MissingTheseNuts(ref running_total, "StardropPool") && flag5)
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_StardropPool", 0));
		}
		if (MissingTheseNuts(ref running_total, "Bush_Caldera_28_36", "Bush_Caldera_9_34"))
		{
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_Caldera", 0));
		}
		MissingTheseNuts(ref running_total, "Bush_CaptainRoom_2_4");
		if (MissingTheseNuts(ref running_total, "BananaShrine"))
		{
			running_total += 2;
		}
		MissingTheseNuts(ref running_total, "Bush_IslandEast_17_37");
		MissingLimitedNutDrops(ref running_total, "Darts", 3);
		int running_total15 = 0;
		if (MissingTheseNuts(ref running_total15, "IslandGourmand1", "IslandGourmand2", "IslandGourmand3"))
		{
			if (Utility.doesAnyFarmerHaveOrWillReceiveMail("talkedToGourmand"))
			{
				list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_Gourmand", 0));
			}
			running_total15 *= 5;
		}
		running_total += running_total15;
		if (MissingTheseNuts(ref running_total, "IslandShrinePuzzle"))
		{
			running_total += 4;
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_IslandShrine", 0));
		}
		MissingTheseNuts(ref running_total, "Bush_IslandShrine_23_34");
		if (!Game1.netWorldState.Value.GoldenCoconutCracked)
		{
			running_total++;
			list.Add(new KeyValuePair<string, int>("Strings\\Locations:NutHint_GoldenCoconut", 0));
		}
		if (!Game1.MasterPlayer.hasOrWillReceiveMail("gotBirdieReward"))
		{
			running_total += 5;
		}
		KeyValuePair<string, int>? keyValuePair = null;
		if (hintForToday.Value == null)
		{
			Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)Game1.Date.TotalDays * 642.0);
			if (list.Count > 0)
			{
				keyValuePair = list[random.Next(list.Count)];
				hintForToday.Value = keyValuePair.Value.Key;
			}
		}
		else
		{
			foreach (KeyValuePair<string, int> item in list)
			{
				if (item.Key == hintForToday.Value)
				{
					keyValuePair = item;
					break;
				}
			}
		}
		hintShowTime = 1.5f;
		hintShakeTime = 0.5f;
		hintDialogues.Clear();
		Squawk();
		if (keyValuePair.HasValue)
		{
			hintDialogues.Add(Game1.content.LoadString("Strings\\Locations:NutHint_Squawk"));
			hintDialogues.Add(Game1.content.LoadString(keyValuePair.Value.Key, keyValuePair.Value.Value));
			hintDialogues.Add(Game1.content.LoadString("Strings\\Locations:NutHint_Squawk"));
		}
		else
		{
			hintDialogues.Add(Game1.content.LoadString("Strings\\Locations:NutHint_Squawk"));
		}
		return running_total;
	}

	public virtual void Squawk()
	{
		if (parrotUpgradePerches.Count > 0)
		{
			parrotUpgradePerches[0].ShowInsufficientNuts();
		}
	}

	protected virtual bool MissingLimitedNutDrops(ref int running_total, string key, int count = 1)
	{
		count -= Math.Max(Game1.player.team.GetDroppedLimitedNutCount(key), 0);
		running_total += count;
		return count > 0;
	}

	protected virtual bool MissingTheseNuts(ref int running_total, params string[] keys)
	{
		int num = 0;
		foreach (string item in keys)
		{
			if (!Game1.player.team.collectedNutTracker.Contains(item))
			{
				num++;
			}
		}
		running_total += num;
		return num > 0;
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		hitTreeEvent.Poll();
		parrotBoyEvent.Poll();
		if (hintDialogues.Count <= 0)
		{
			return;
		}
		hintShowTime -= (float)time.ElapsedGameTime.TotalSeconds;
		hintShakeTime -= (float)time.ElapsedGameTime.TotalSeconds;
		if (!(hintShowTime <= 0f))
		{
			return;
		}
		hintDialogues.RemoveAt(0);
		if (hintDialogues.Count > 0)
		{
			if (hintDialogues.Count == 2)
			{
				hintShowTime = 3f;
			}
			else
			{
				hintShowTime = 1.5f;
			}
			hintShakeTime = 0.5f;
			Squawk();
		}
		else
		{
			hintShowTime = -1f;
		}
	}

	public IslandHut()
	{
	}

	public IslandHut(string map, string name)
		: base(map, name)
	{
		parrotUpgradePerches.Add(new ParrotUpgradePerch(this, new Point(7, 6), new Microsoft.Xna.Framework.Rectangle(-1000, -1000, 1, 1), 1, delegate
		{
			Game1.addMailForTomorrow("Island_FirstParrot", noLetter: true, sendToEveryone: true);
			firstParrotDone.Value = true;
			parrotBoyEvent.Fire();
		}, () => firstParrotDone.Value, "Hut"));
	}

	public override bool performToolAction(Tool t, int tileX, int tileY)
	{
		if (tileX == 10 && tileY == 8 && (t is Pickaxe || t is Axe) && !treeHitLocal)
		{
			hitTreeEvent.Fire();
		}
		return base.performToolAction(t, tileX, tileY);
	}

	public override void DayUpdate(int dayOfMonth)
	{
		base.DayUpdate(dayOfMonth);
		hintForToday.Value = null;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(treeNutObtained, "treeNutObtained").AddField(hitTreeEvent.NetFields, "hitTreeEvent.NetFields").AddField(firstParrotDone, "firstParrotDone")
			.AddField(parrotBoyEvent.NetFields, "parrotBoyEvent.NetFields")
			.AddField(hintForToday, "hintForToday");
		hitTreeEvent.onEvent += SpitTreeNut;
		parrotBoyEvent.onEvent += ParrotBoyEvent_onEvent;
	}

	private void ParrotBoyEvent_onEvent()
	{
		if (Game1.player.currentLocation.Equals(this) && !Game1.IsFading())
		{
			Game1.addMailForTomorrow("sawParrotBoyIntro", noLetter: true);
			Game1.globalFadeToBlack(delegate
			{
				startEvent(new Event(Game1.content.LoadString("Strings\\Locations:IslandHut_Event_ParrotBoyIntro")));
			});
		}
		else if (Game1.locationRequest?.Location?.NameOrUniqueName == base.NameOrUniqueName && !Game1.warpingForForcedRemoteEvent)
		{
			Game1.addMailForTomorrow("sawParrotBoyIntro", noLetter: true);
			startEvent(new Event(Game1.content.LoadString("Strings\\Locations:IslandHut_Event_ParrotBoyIntro")));
		}
	}

	public virtual void SpitTreeNut()
	{
		if (treeHitLocal)
		{
			return;
		}
		treeHitLocal = true;
		if (Game1.currentLocation == this)
		{
			Game1.playSound("boulderBreak");
			DelayedAction.playSoundAfterDelay("croak", 300);
			DelayedAction.playSoundAfterDelay("slimeHit", 1250);
			DelayedAction.playSoundAfterDelay("coin", 1250);
		}
		TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(5, new Vector2(10f, 5f) * 64f, Color.White);
		temporaryAnimatedSprite.motion = new Vector2(0f, -1.5f);
		temporaryAnimatedSprite.interval = 25f;
		temporaryAnimatedSprite.delayBeforeAnimationStart = 1250;
		temporarySprites.Add(temporaryAnimatedSprite);
		temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(32, 192, 16, 32), 1250f, 1, 1, new Vector2(10f, 7f) * 64f, flicker: false, flipped: false, 0.0001f, 0f, Color.White, 4f, 0f, 0f, 0f);
		temporaryAnimatedSprite.shakeIntensity = 1f;
		temporarySprites.Add(temporaryAnimatedSprite);
		temporaryAnimatedSprite = new TemporaryAnimatedSprite(46, new Vector2(10f, 5f) * 64f, Color.White);
		temporaryAnimatedSprite.motion = new Vector2(0f, -3f);
		temporaryAnimatedSprite.interval = 25f;
		temporaryAnimatedSprite.delayBeforeAnimationStart = 1250;
		temporarySprites.Add(temporaryAnimatedSprite);
		for (int i = 0; i < 5; i++)
		{
			temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(352, 1200, 16, 16), 50f, 11, 3, new Vector2(10f, 5f) * 64f, flicker: false, flipped: false, 0.1f, 0.01f, Color.White, 4f, 0f, 0f, 0f);
			temporaryAnimatedSprite.motion.X = Utility.RandomFloat(-3f, 3f);
			temporaryAnimatedSprite.motion.Y = Utility.RandomFloat(-1f, -3f);
			temporaryAnimatedSprite.acceleration.Y = 0.05f;
			temporaryAnimatedSprite.delayBeforeAnimationStart = 1250;
			temporarySprites.Add(temporaryAnimatedSprite);
		}
		if (Game1.IsMasterGame && !treeNutObtained.Value)
		{
			Game1.player.team.MarkCollectedNut("TreeNut");
			DelayedAction.functionAfterDelay(delegate
			{
				Game1.createItemDebris(ItemRegistry.Create("(O)73"), new Vector2(10.5f, 7f) * 64f, 0, this, 0);
			}, 1250);
			treeNutObtained.Value = true;
		}
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		if (l is IslandHut islandHut)
		{
			treeNutObtained.Value = islandHut.treeNutObtained.Value;
			firstParrotDone.Value = islandHut.firstParrotDone.Value;
			hintForToday.Value = islandHut.hintForToday.Value;
		}
		base.TransferDataFromSavedLocation(l);
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		if (hintDialogues.Count > 0)
		{
			Vector2 vector = Game1.GlobalToLocal(Game1.viewport, new Vector2(7.25f, 3f) * 64f);
			if (hintShakeTime > 0f)
			{
				vector.X += Utility.RandomFloat(-1f, 1f);
				vector.Y += Utility.RandomFloat(-1f, 1f);
			}
			SpriteText.drawStringWithScrollCenteredAt(b, hintDialogues[0], (int)vector.X, (int)vector.Y, "", Math.Min(1f, hintShowTime * 2f), null, 1, 1f);
		}
		base.drawAboveAlwaysFrontLayer(b);
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		hintDialogues.Clear();
		hintShowTime = -1f;
		treeHitLocal = treeNutObtained.Value;
		if (Game1.netWorldState.Value.GoldenWalnutsFound < 10)
		{
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\shadow", new Microsoft.Xna.Framework.Rectangle(0, 0, 12, 7), new Vector2(5.15f, 2.25f) * 64f, flipped: false, 0f, Color.White)
			{
				id = 777,
				scale = 4f,
				totalNumberOfLoops = 99999,
				interval = 9999f,
				animationLength = 1,
				layerDepth = 0.95f,
				drawAboveAlwaysFront = true
			});
			temporarySprites.Add(new TemporaryAnimatedSprite("Characters\\ParrotBoy", new Microsoft.Xna.Framework.Rectangle(32, 128, 16, 32), new Vector2(5f, 0.5f) * 64f, flipped: false, 0f, Color.White)
			{
				id = 777,
				scale = 4f,
				totalNumberOfLoops = 99999,
				interval = 9999f,
				animationLength = 1,
				layerDepth = 1f,
				drawAboveAlwaysFront = true
			});
		}
		if (firstParrotDone.Value && !Game1.MasterPlayer.hasOrWillReceiveMail("addedParrotBoy") && !Game1.player.hasOrWillReceiveMail("sawParrotBoyIntro"))
		{
			ParrotBoyEvent_onEvent();
		}
	}
}
