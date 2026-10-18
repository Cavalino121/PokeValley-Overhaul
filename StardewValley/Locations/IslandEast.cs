using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using xTile.Dimensions;

namespace StardewValley.Locations;

public class IslandEast : IslandForestLocation
{
	protected PerchingBirds _parrots;

	protected Texture2D _parrotTextures;

	protected NetEvent0 bananaShrineEvent = new NetEvent0();

	public NetBool bananaShrineComplete = new NetBool();

	public NetBool bananaShrineNutAwarded = new NetBool();

	public IslandEast()
	{
	}

	public IslandEast(string map, string name)
		: base(map, name)
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(bananaShrineEvent.NetFields, "bananaShrineEvent.NetFields").AddField(bananaShrineComplete, "bananaShrineComplete").AddField(bananaShrineNutAwarded, "bananaShrineNutAwarded");
		bananaShrineEvent.onEvent += OnBananaShrine;
	}

	public virtual void AddTorchLights()
	{
		removeTemporarySpritesWithIDLocal(6666);
		int num = 1280;
		int num2 = 704;
		temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1965, 8, 8), new Vector2(num + 24, num2 + 48), flipped: false, 0f, Color.White)
		{
			interval = 50f,
			totalNumberOfLoops = 99999,
			animationLength = 7,
			lightId = "IslandEast_TorchLight_1",
			id = 6666,
			lightRadius = 1f,
			scale = 3f,
			layerDepth = (float)(num2 + 48) / 10000f + 0.0001f,
			delayBeforeAnimationStart = 0
		});
		temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1984, 12, 12), new Vector2(num + 16, num2 + 28), flipped: false, 0f, Color.White)
		{
			interval = 50f,
			totalNumberOfLoops = 99999,
			animationLength = 4,
			lightId = "IslandEast_TorchLight_2",
			id = 6666,
			lightRadius = 1f,
			scale = 3f,
			layerDepth = (float)(num2 + 28) / 10000f + 0.0001f,
			delayBeforeAnimationStart = 0
		});
		num = 1472;
		num2 = 704;
		temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1965, 8, 8), new Vector2(num + 24, num2 + 48), flipped: false, 0f, Color.White)
		{
			interval = 50f,
			totalNumberOfLoops = 99999,
			animationLength = 7,
			lightId = "IslandEast_TorchLight_3",
			id = 6666,
			lightRadius = 1f,
			scale = 3f,
			layerDepth = (float)(num2 + 48) / 10000f + 0.0001f,
			delayBeforeAnimationStart = 0
		});
		temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1984, 12, 12), new Vector2(num + 16, num2 + 28), flipped: false, 0f, Color.White)
		{
			interval = 50f,
			totalNumberOfLoops = 99999,
			animationLength = 4,
			lightId = "IslandEast_TorchLight_4",
			id = 6666,
			lightRadius = 1f,
			scale = 3f,
			layerDepth = (float)(num2 + 28) / 10000f + 0.0001f,
			delayBeforeAnimationStart = 0
		});
	}

	protected override void resetLocalState()
	{
		_parrotTextures = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\parrots");
		base.resetLocalState();
		for (int i = 0; i < 5; i++)
		{
			Vector2 randomPositionInThisRectangle = Utility.getRandomPositionInThisRectangle(new Microsoft.Xna.Framework.Rectangle(14, 3, 16, 12), Game1.random);
			critters.Add(new Firefly(randomPositionInThisRectangle));
		}
		AddTorchLights();
		if (bananaShrineComplete.Value)
		{
			AddGorillaShrineTorches(0);
		}
		_parrots = new PerchingBirds(_parrotTextures, 3, 24, 24, new Vector2(12f, 19f), new Point[9]
		{
			new Point(18, 8),
			new Point(17, 9),
			new Point(20, 7),
			new Point(21, 8),
			new Point(22, 7),
			new Point(23, 8),
			new Point(18, 12),
			new Point(25, 11),
			new Point(27, 8)
		}, new Point[0]);
		_parrots.peckDuration = 0;
		for (int j = 0; j < 5; j++)
		{
			_parrots.AddBird(Game1.random.Next(0, 4));
		}
		if (bananaShrineComplete.Value && Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, 1111.0).NextDouble() < 0.1)
		{
			temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(32, 352, 32, 32), 500f, 2, 999, new Vector2(15.5f, 19f) * 64f, flicker: false, flipped: false, 0.1216f, 0f, Color.White, 4f, 0f, 0f, 0f)
			{
				id = 888,
				yStopCoordinate = 1497,
				motion = new Vector2(0f, 1f),
				reachedStopCoordinate = gorillaReachedShrineCosmetic,
				delayBeforeAnimationStart = 1000
			});
		}
		addOneTimeGiftBox(ItemRegistry.Create("(O)TentKit", 3), 30, 40, 4);
	}

	public override void cleanupBeforePlayerExit()
	{
		_parrots = null;
		_parrotTextures = null;
		base.cleanupBeforePlayerExit();
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		bananaShrineEvent.Poll();
		_parrots?.Update(time);
		if (bananaShrineComplete.Value && Game1.random.NextDouble() < 0.005)
		{
			TemporaryAnimatedSprite temporarySpriteByID = getTemporarySpriteByID(888);
			if (temporarySpriteByID != null && temporarySpriteByID.motion.Equals(Vector2.Zero))
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(128, 352, 32, 32), 200 + ((Game1.random.NextDouble() < 0.1) ? Game1.random.Next(1000, 3000) : 0), 1, 1, temporarySpriteByID.position, flicker: false, flipped: false, 0.12224f, 0f, Color.White, 4f, 0f, 0f, 0f));
			}
		}
	}

	public virtual void SpawnBananaNutReward()
	{
		if (!bananaShrineNutAwarded.Value && Game1.IsMasterGame)
		{
			Game1.player.team.MarkCollectedNut("BananaShrine");
			bananaShrineNutAwarded.Value = true;
			for (int i = 0; i < 3; i++)
			{
				Game1.createItemDebris(ItemRegistry.Create("(O)73"), new Vector2(16.5f, 25f) * 64f, 0, this, 1280);
			}
		}
	}

	public override void DayUpdate(int dayOfMonth)
	{
		if (Game1.IsMasterGame && bananaShrineComplete.Value && !bananaShrineNutAwarded.Value)
		{
			SpawnBananaNutReward();
		}
		base.DayUpdate(dayOfMonth);
		Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle(27, 27, 3, 3);
		for (int i = 0; i < 8; i++)
		{
			Vector2 randomTile = getRandomTile();
			if (randomTile.Y < 24f)
			{
				randomTile.Y += 24f;
			}
			if (randomTile.X > 4f && !hasTileAt((int)randomTile.X, (int)randomTile.Y, "AlwaysFront") && CanItemBePlacedHere(randomTile, itemIsPassable: false, CollisionMask.All, CollisionMask.None) && doesTileHavePropertyNoNull((int)randomTile.X, (int)randomTile.Y, "Type", "Back") == "Grass" && !IsNoSpawnTile(randomTile) && doesTileHavePropertyNoNull((int)randomTile.X + 1, (int)randomTile.Y, "Type", "Back") != "Stone" && doesTileHavePropertyNoNull((int)randomTile.X - 1, (int)randomTile.Y, "Type", "Back") != "Stone" && doesTileHavePropertyNoNull((int)randomTile.X, (int)randomTile.Y + 1, "Type", "Back") != "Stone" && doesTileHavePropertyNoNull((int)randomTile.X, (int)randomTile.Y - 1, "Type", "Back") != "Stone" && !rectangle.Contains((int)randomTile.X, (int)randomTile.Y))
			{
				if (Game1.random.NextDouble() < 0.04)
				{
					Object obj = ItemRegistry.Create<Object>("(O)259");
					obj.isSpawnedObject.Value = true;
					objects.Add(randomTile, obj);
				}
				else
				{
					objects.Add(randomTile, ItemRegistry.Create<Object>("(O)" + (882 + Game1.random.Next(3))));
				}
			}
		}
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		_parrots?.Draw(b);
		base.drawAboveAlwaysFrontLayer(b);
	}

	public virtual void AddGorillaShrineTorches(int delay)
	{
		if (getTemporarySpriteByID(12038) == null)
		{
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), new Vector2(15f, 24f) * 64f + new Vector2(8f, -16f), flipped: false, 0f, Color.White)
			{
				interval = 50f,
				totalNumberOfLoops = 99999,
				animationLength = 4,
				lightId = "IslandEast_GorillaTorch_1",
				lightRadius = 2f,
				delayBeforeAnimationStart = delay,
				scale = 4f,
				layerDepth = 0.16704f,
				id = 12038
			});
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), new Vector2(17f, 24f) * 64f + new Vector2(8f, -16f), flipped: false, 0f, Color.White)
			{
				interval = 50f,
				totalNumberOfLoops = 99999,
				animationLength = 4,
				lightId = "IslandEast_GorillaTorch_2",
				lightRadius = 2f,
				delayBeforeAnimationStart = delay,
				scale = 4f,
				layerDepth = 0.16704f,
				id = 12097
			});
		}
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		base.TransferDataFromSavedLocation(l);
		if (l is IslandEast islandEast)
		{
			bananaShrineComplete.Value = islandEast.bananaShrineComplete.Value;
			bananaShrineNutAwarded.Value = islandEast.bananaShrineNutAwarded.Value;
		}
	}

	public virtual void OnBananaShrine()
	{
		Location location = new Location(16, 26);
		temporarySprites.Add(new TemporaryAnimatedSprite("Maps\\springobjects", new Microsoft.Xna.Framework.Rectangle(304, 48, 16, 16), new Vector2(16f, location.Y - 1) * 64f, flipped: false, 0f, Color.White)
		{
			id = 88976,
			scale = 4f,
			layerDepth = ((float)location.Y + 1.2f) * 64f / 10000f,
			dontClearOnAreaEntry = true
		});
		temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(32, 352, 32, 32), 400f, 2, 999, new Vector2(15.5f, 19f) * 64f, flicker: false, flipped: false, 0.1216f, 0f, Color.White, 4f, 0f, 0f, 0f)
		{
			id = 777,
			yStopCoordinate = 1497,
			motion = new Vector2(0f, 2f),
			reachedStopCoordinate = gorillaReachedShrine,
			delayBeforeAnimationStart = 1000,
			dontClearOnAreaEntry = true
		});
		if (Game1.currentLocation == this)
		{
			Game1.playSound("coin");
			DelayedAction.playSoundAfterDelay("fireball", 800);
		}
		AddGorillaShrineTorches(800);
		if (Game1.currentLocation == this)
		{
			DelayedAction.playSoundAfterDelay("grassyStep", 1400);
			DelayedAction.playSoundAfterDelay("grassyStep", 1800);
			DelayedAction.playSoundAfterDelay("grassyStep", 2200);
			DelayedAction.playSoundAfterDelay("grassyStep", 2600);
			DelayedAction.playSoundAfterDelay("grassyStep", 3000);
			Game1.changeMusicTrack("none");
			DelayedAction.playSoundAfterDelay("gorilla_intro", 2000);
		}
	}

	/// <inheritdoc />
	public override bool performAction(string[] action, Farmer who, Location tileLocation)
	{
		if (ArgUtility.Get(action, 0) == "BananaShrine")
		{
			if (who.CurrentItem?.QualifiedItemId == "(O)91" && getTemporarySpriteByID(777) == null && !bananaShrineComplete.Value)
			{
				bananaShrineComplete.Value = true;
				who.reduceActiveItemByOne();
				bananaShrineEvent.Fire();
				return true;
			}
			if (getTemporarySpriteByID(777) == null && !bananaShrineComplete.Value)
			{
				who.doEmote(8);
			}
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

	private void gorillaReachedShrineCosmetic(int extra)
	{
		TemporaryAnimatedSprite temporarySpriteByID = getTemporarySpriteByID(888);
		temporarySpriteByID.sourceRect = new Microsoft.Xna.Framework.Rectangle(192, 352, 32, 32);
		temporarySpriteByID.sourceRectStartingPos = Utility.PointToVector2(temporarySpriteByID.sourceRect.Location);
		temporarySpriteByID.currentNumberOfLoops = 0;
		temporarySpriteByID.totalNumberOfLoops = 999999;
		temporarySpriteByID.interval = 8000f;
		temporarySpriteByID.timer = 0f;
		temporarySpriteByID.motion = Vector2.Zero;
		temporarySpriteByID.animationLength = 1;
	}

	private void gorillaGrabBanana(int extra)
	{
		TemporaryAnimatedSprite temporarySpriteByID = getTemporarySpriteByID(777);
		DelayedAction.functionAfterDelay(delegate
		{
			removeTemporarySpritesWithID(88976);
		}, 50);
		if (Game1.currentLocation == this)
		{
			Game1.playSound("slimeHit");
		}
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
		if (Game1.currentLocation == this)
		{
			Game1.playSound("eat");
			DelayedAction.playSoundAfterDelay("eat", 600);
			DelayedAction.playSoundAfterDelay("eat", 1200);
			DelayedAction.playSoundAfterDelay("eat", 1800);
			DelayedAction.playSoundAfterDelay("eat", 2400);
		}
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
		if (Game1.currentLocation == this)
		{
			Game1.playSound("grunt");
		}
		if (Game1.IsMasterGame)
		{
			SpawnBananaNutReward();
		}
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
		if (Game1.currentLocation == this)
		{
			DelayedAction.functionAfterDelay(delegate
			{
				Game1.playMorningSong();
			}, 3000);
		}
	}
}
