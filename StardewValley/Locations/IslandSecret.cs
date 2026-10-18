using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Monsters;
using xTile.Dimensions;

namespace StardewValley.Locations;

public class IslandSecret : IslandLocation
{
	[XmlIgnore]
	public List<SuspensionBridge> suspensionBridges = new List<SuspensionBridge>();

	[XmlElement("addedSlimesToday")]
	private readonly NetBool addedSlimesToday = new NetBool();

	public IslandSecret()
	{
	}

	public IslandSecret(string map, string name)
		: base(map, name)
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(addedSlimesToday, "addedSlimesToday");
	}

	protected override void resetSharedState()
	{
		base.resetSharedState();
		if (addedSlimesToday.Value)
		{
			return;
		}
		addedSlimesToday.Value = true;
		Random random = Utility.CreateRandom(Game1.stats.DaysPlayed, Game1.uniqueIDForThisGame, 12.0);
		Microsoft.Xna.Framework.Rectangle r = new Microsoft.Xna.Framework.Rectangle(13, 15, 7, 6);
		for (int num = 5; num > 0; num--)
		{
			Vector2 randomPositionInThisRectangle = Utility.getRandomPositionInThisRectangle(r, random);
			if (CanItemBePlacedHere(randomPositionInThisRectangle))
			{
				GreenSlime item = new GreenSlime(randomPositionInThisRectangle * 64f, 9999899);
				characters.Add(item);
			}
		}
		if (random.NextBool() && CanItemBePlacedHere(new Vector2(17f, 18f)))
		{
			objects.Add(new Vector2(17f, 18f), ItemRegistry.Create<Object>("(BC)56"));
		}
		GreenSlime greenSlime = new GreenSlime(new Vector2(42f, 34f) * 64f);
		greenSlime.makeTigerSlime();
		characters.Add(greenSlime);
		greenSlime = new GreenSlime(new Vector2(38f, 33f) * 64f);
		greenSlime.makeTigerSlime();
		characters.Add(greenSlime);
	}

	public override string checkForBuriedItem(int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
	{
		if (xLocation == 82 && yLocation == 83 && who.secretNotesSeen.Contains(1002))
		{
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("Island_Secret_BuriedTreasureNut"))
			{
				Game1.createItemDebris(ItemRegistry.Create("(O)73"), new Vector2(xLocation, yLocation) * 64f, 1);
				Game1.addMailForTomorrow("Island_Secret_BuriedTreasureNut", noLetter: true, sendToEveryone: true);
			}
			if (!Game1.player.hasOrWillReceiveMail("Island_Secret_BuriedTreasure"))
			{
				Game1.createItemDebris(ItemRegistry.Create("(O)166"), new Vector2(xLocation, yLocation) * 64f, 1);
				Game1.addMailForTomorrow("Island_Secret_BuriedTreasure", noLetter: true);
			}
		}
		return base.checkForBuriedItem(xLocation, yLocation, explosion, detectOnly, who);
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		suspensionBridges.Clear();
		suspensionBridges.Add(new SuspensionBridge(46, 44));
		suspensionBridges.Add(new SuspensionBridge(47, 34));
		NPC characterFromName = getCharacterFromName("Birdie");
		if (characterFromName != null)
		{
			if (characterFromName.Sprite.SourceRect.Width < 32)
			{
				characterFromName.extendSourceRect(16, 0);
			}
			characterFromName.Sprite.SpriteWidth = 32;
			characterFromName.Sprite.ignoreSourceRectUpdates = false;
			characterFromName.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
			{
				new FarmerSprite.AnimationFrame(8, 1000, 0, secondaryArm: false, flip: false),
				new FarmerSprite.AnimationFrame(9, 1000, 0, secondaryArm: false, flip: false)
			});
			characterFromName.Sprite.loop = true;
			characterFromName.HideShadow = true;
			characterFromName.IsInvisible = IsRainingHere();
		}
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		NPC characterFromName = getCharacterFromName("Birdie");
		if (characterFromName != null && !characterFromName.IsInvisible && characterFromName.Tile == new Vector2(tileLocation.X, tileLocation.Y))
		{
			if (who.mailReceived.Add("birdieQuestBegun"))
			{
				Game1.globalFadeToBlack(delegate
				{
					startEvent(new Event(Game1.content.LoadString("Strings\\Locations:IslandSecret_Event_BirdieIntro")));
				});
			}
			else if (!who.mailReceived.Contains("birdieQuestFinished") && who.ActiveObject?.QualifiedItemId == "(O)870")
			{
				Game1.globalFadeToBlack(delegate
				{
					startEvent(new Event(Game1.content.LoadString("Strings\\Locations:IslandSecret_Event_BirdieFinished")));
					who.ActiveObject = null;
				});
				who.mailReceived.Add("birdieQuestFinished");
			}
		}
		return base.checkAction(tileLocation, viewport, who);
	}

	public override void DayUpdate(int dayOfMonth)
	{
		characters.RemoveWhere((NPC npc) => npc is Monster);
		addedSlimesToday.Value = false;
		base.DayUpdate(dayOfMonth);
	}

	/// <inheritdoc />
	public override bool performAction(string[] action, Farmer who, Location tileLocation)
	{
		if (ArgUtility.Get(action, 0) == "BananaShrine")
		{
			if (who.CurrentItem?.QualifiedItemId == "(O)91" && getTemporarySpriteByID(777) == null)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("Maps\\springobjects", new Microsoft.Xna.Framework.Rectangle(304, 48, 16, 16), new Vector2(tileLocation.X, tileLocation.Y - 1) * 64f, flipped: false, 0f, Color.White)
				{
					id = 888,
					scale = 4f,
					layerDepth = ((float)tileLocation.Y + 1.2f) * 64f / 10000f
				});
				temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(32, 352, 32, 32), 400f, 2, 999, new Vector2(15.5f, 20f) * 64f, flicker: false, flipped: false, 0.128f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					id = 777,
					yStopCoordinate = 1561,
					motion = new Vector2(0f, 2f),
					reachedStopCoordinate = gorillaReachedShrine,
					delayBeforeAnimationStart = 1000
				});
				playSound("coin");
				DelayedAction.playSoundAfterDelay("grassyStep", 1400);
				DelayedAction.playSoundAfterDelay("grassyStep", 1800);
				DelayedAction.playSoundAfterDelay("grassyStep", 2200);
				DelayedAction.playSoundAfterDelay("grassyStep", 2600);
				DelayedAction.playSoundAfterDelay("grassyStep", 3000);
				who.reduceActiveItemByOne();
				Game1.changeMusicTrack("none");
				DelayedAction.playSoundAfterDelay("gorilla_intro", 2000);
			}
			return true;
		}
		return base.performAction(action, who, tileLocation);
	}

	private void gorillaReachedShrine(int extra)
	{
		TemporaryAnimatedSprite temporarySpriteByID = getTemporarySpriteByID(777);
		temporarySpriteByID.sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 352, 32, 32);
		temporarySpriteByID.sourceRectStartingPos = Utility.PointToVector2(temporarySpriteByID.sourceRect.Location);
		temporarySpriteByID.currentNumberOfLoops = 0;
		temporarySpriteByID.totalNumberOfLoops = 1;
		temporarySpriteByID.interval = 1000f;
		temporarySpriteByID.timer = 0f;
		temporarySpriteByID.motion = Vector2.Zero;
		temporarySpriteByID.animationLength = 1;
		temporarySpriteByID.endFunction = gorillaGrabBanana;
	}

	private void gorillaGrabBanana(int extra)
	{
		TemporaryAnimatedSprite temporarySpriteByID = getTemporarySpriteByID(777);
		removeTemporarySpritesWithID(888);
		playSound("slimeHit");
		temporarySpriteByID.sourceRect = new Microsoft.Xna.Framework.Rectangle(96, 352, 32, 32);
		temporarySpriteByID.sourceRectStartingPos = Utility.PointToVector2(temporarySpriteByID.sourceRect.Location);
		temporarySpriteByID.currentNumberOfLoops = 0;
		temporarySpriteByID.totalNumberOfLoops = 1;
		temporarySpriteByID.interval = 1000f;
		temporarySpriteByID.timer = 0f;
		temporarySpriteByID.animationLength = 1;
		temporarySpriteByID.endFunction = gorillaEatBanana;
		temporarySprites.Add(temporarySpriteByID);
	}

	private void gorillaEatBanana(int extra)
	{
		TemporaryAnimatedSprite temporarySpriteByID = getTemporarySpriteByID(777);
		temporarySpriteByID.sourceRect = new Microsoft.Xna.Framework.Rectangle(128, 352, 32, 32);
		temporarySpriteByID.sourceRectStartingPos = Utility.PointToVector2(temporarySpriteByID.sourceRect.Location);
		temporarySpriteByID.currentNumberOfLoops = 0;
		temporarySpriteByID.totalNumberOfLoops = 5;
		temporarySpriteByID.interval = 300f;
		temporarySpriteByID.timer = 0f;
		temporarySpriteByID.animationLength = 2;
		temporarySpriteByID.endFunction = gorillaAfterEat;
		playSound("eat");
		DelayedAction.playSoundAfterDelay("eat", 600);
		DelayedAction.playSoundAfterDelay("eat", 1200);
		DelayedAction.playSoundAfterDelay("eat", 1800);
		DelayedAction.playSoundAfterDelay("eat", 2400);
		temporarySprites.Add(temporarySpriteByID);
	}

	private void gorillaAfterEat(int extra)
	{
		TemporaryAnimatedSprite temporarySpriteByID = getTemporarySpriteByID(777);
		temporarySpriteByID.sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 352, 32, 32);
		temporarySpriteByID.sourceRectStartingPos = Utility.PointToVector2(temporarySpriteByID.sourceRect.Location);
		temporarySpriteByID.currentNumberOfLoops = 0;
		temporarySpriteByID.totalNumberOfLoops = 1;
		temporarySpriteByID.interval = 1000f;
		temporarySpriteByID.timer = 0f;
		temporarySpriteByID.motion = Vector2.Zero;
		temporarySpriteByID.animationLength = 1;
		temporarySpriteByID.endFunction = gorillaSpawnNut;
		temporarySpriteByID.shakeIntensity = 1f;
		temporarySpriteByID.shakeIntensityChange = -0.01f;
		temporarySprites.Add(temporarySpriteByID);
	}

	private void gorillaSpawnNut(int extra)
	{
		TemporaryAnimatedSprite temporarySpriteByID = getTemporarySpriteByID(777);
		temporarySpriteByID.sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 352, 32, 32);
		temporarySpriteByID.sourceRectStartingPos = Utility.PointToVector2(temporarySpriteByID.sourceRect.Location);
		temporarySpriteByID.currentNumberOfLoops = 0;
		temporarySpriteByID.totalNumberOfLoops = 1;
		temporarySpriteByID.interval = 1000f;
		temporarySpriteByID.shakeIntensity = 2f;
		temporarySpriteByID.shakeIntensityChange = -0.01f;
		playSound("grunt");
		Game1.createItemDebris(ItemRegistry.Create("(O)73"), new Vector2(16.5f, 25f) * 64f, 0, this, 1280);
		temporarySpriteByID.timer = 0f;
		temporarySpriteByID.motion = Vector2.Zero;
		temporarySpriteByID.animationLength = 1;
		temporarySpriteByID.endFunction = gorillaReturn;
		temporarySprites.Add(temporarySpriteByID);
	}

	private void gorillaReturn(int extra)
	{
		TemporaryAnimatedSprite temporarySpriteByID = getTemporarySpriteByID(777);
		temporarySpriteByID.sourceRect = new Microsoft.Xna.Framework.Rectangle(32, 352, 32, 32);
		temporarySpriteByID.sourceRectStartingPos = Utility.PointToVector2(temporarySpriteByID.sourceRect.Location);
		temporarySpriteByID.currentNumberOfLoops = 0;
		temporarySpriteByID.totalNumberOfLoops = 6;
		temporarySpriteByID.interval = 200f;
		temporarySpriteByID.timer = 0f;
		temporarySpriteByID.motion = new Vector2(0f, -3f);
		temporarySpriteByID.animationLength = 2;
		temporarySpriteByID.yStopCoordinate = 1280;
		temporarySpriteByID.reachedStopCoordinate = delegate
		{
			removeTemporarySpritesWithID(777);
		};
		temporarySprites.Add(temporarySpriteByID);
		DelayedAction.functionAfterDelay(delegate
		{
			Game1.playMorningSong();
		}, 3000);
	}

	public override void SetBuriedNutLocations()
	{
		buriedNutPoints.Add(new Point(23, 47));
		buriedNutPoints.Add(new Point(61, 21));
		base.SetBuriedNutLocations();
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		foreach (SuspensionBridge suspensionBridge in suspensionBridges)
		{
			suspensionBridge.Update(time);
		}
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		foreach (SuspensionBridge suspensionBridge in suspensionBridges)
		{
			suspensionBridge.Draw(b);
		}
	}

	public override bool IsLocationSpecificPlacementRestriction(Vector2 tileLocation)
	{
		foreach (SuspensionBridge suspensionBridge in suspensionBridges)
		{
			if (suspensionBridge.CheckPlacementPrevention(tileLocation))
			{
				return true;
			}
		}
		return base.IsLocationSpecificPlacementRestriction(tileLocation);
	}
}
