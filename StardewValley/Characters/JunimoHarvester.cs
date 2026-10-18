using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;

namespace StardewValley.Characters;

public class JunimoHarvester : NPC
{
	protected float alpha = 1f;

	protected float alphaChange;

	protected Vector2 motion = Vector2.Zero;

	protected new Rectangle nextPosition;

	protected readonly NetColor color = new NetColor();

	protected bool destroy;

	protected Item lastItemHarvested;

	public int whichJunimoFromThisHut;

	protected int harvestTimer;

	public readonly NetBool isPrismatic = new NetBool(value: false);

	protected readonly NetGuid netHome = new NetGuid();

	protected readonly NetEvent1Field<int, NetInt> netAnimationEvent = new NetEvent1Field<int, NetInt>();

	public Guid HomeId
	{
		get
		{
			return netHome.Value;
		}
		set
		{
			netHome.Value = value;
		}
	}

	[XmlIgnore]
	public JunimoHut home
	{
		get
		{
			if (!base.currentLocation.buildings.TryGetValue(netHome.Value, out var value))
			{
				return null;
			}
			return value as JunimoHut;
		}
		set
		{
			netHome.Value = base.currentLocation.buildings.GuidOf(value);
		}
	}

	/// <inheritdoc />
	[XmlIgnore]
	public override bool IsVillager => false;

	public JunimoHarvester()
	{
	}

	public JunimoHarvester(GameLocation location, Vector2 position, JunimoHut hut, int whichJunimoNumberFromThisHut, Color? c)
		: base(new AnimatedSprite("Characters\\Junimo", 0, 16, 16), position, 2, "Junimo")
	{
		base.currentLocation = location;
		home = hut;
		whichJunimoFromThisHut = whichJunimoNumberFromThisHut;
		if (!c.HasValue)
		{
			pickColor();
		}
		else
		{
			color.Value = c.Value;
		}
		nextPosition = GetBoundingBox();
		base.Breather = false;
		base.speed = 3;
		forceUpdateTimer = 9999;
		collidesWithOtherCharacters.Value = true;
		ignoreMovementAnimation = true;
		farmerPassesThrough = true;
		base.Scale = 0.75f;
		base.willDestroyObjectsUnderfoot = false;
		Vector2 vector = Vector2.Zero;
		switch (whichJunimoNumberFromThisHut)
		{
		case 0:
			vector = Utility.recursiveFindOpenTileForCharacter(this, base.currentLocation, new Vector2(hut.tileX.Value + 1, hut.tileY.Value + hut.tilesHigh.Value + 1), 30);
			break;
		case 1:
			vector = Utility.recursiveFindOpenTileForCharacter(this, base.currentLocation, new Vector2(hut.tileX.Value - 1, hut.tileY.Value), 30);
			break;
		case 2:
			vector = Utility.recursiveFindOpenTileForCharacter(this, base.currentLocation, new Vector2(hut.tileX.Value + hut.tilesWide.Value, hut.tileY.Value), 30);
			break;
		}
		if (vector != Vector2.Zero)
		{
			controller = new PathFindController(this, base.currentLocation, Utility.Vector2ToPoint(vector), -1, reachFirstDestinationFromHut, 100);
		}
		if (controller?.pathToEndPoint == null && Game1.IsMasterGame)
		{
			pathfindToRandomSpotAroundHut();
			if (controller?.pathToEndPoint == null)
			{
				destroy = true;
			}
		}
		collidesWithOtherCharacters.Value = false;
	}

	protected virtual void pickColor()
	{
		JunimoHut junimoHut = home;
		if (junimoHut == null)
		{
			color.Value = Color.White;
			return;
		}
		Random random = Utility.CreateRandom(junimoHut.tileX.Value, (double)junimoHut.tileY.Value * 777.0, whichJunimoFromThisHut);
		if (random.NextBool(0.25))
		{
			if (random.NextBool(0.01))
			{
				color.Value = Color.White;
				return;
			}
			switch (random.Next(8))
			{
			case 0:
				color.Value = Color.Red;
				break;
			case 1:
				color.Value = Color.Goldenrod;
				break;
			case 2:
				color.Value = Color.Yellow;
				break;
			case 3:
				color.Value = Color.Lime;
				break;
			case 4:
				color.Value = new Color(0, 255, 180);
				break;
			case 5:
				color.Value = new Color(0, 100, 255);
				break;
			case 6:
				color.Value = Color.MediumPurple;
				break;
			default:
				color.Value = Color.Salmon;
				break;
			}
		}
		else
		{
			switch (random.Next(8))
			{
			case 0:
				color.Value = Color.LimeGreen;
				break;
			case 1:
				color.Value = Color.Orange;
				break;
			case 2:
				color.Value = Color.LightGreen;
				break;
			case 3:
				color.Value = Color.Tan;
				break;
			case 4:
				color.Value = Color.GreenYellow;
				break;
			case 5:
				color.Value = Color.LawnGreen;
				break;
			case 6:
				color.Value = Color.PaleGreen;
				break;
			default:
				color.Value = Color.Turquoise;
				break;
			}
		}
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(color, "color").AddField(netHome.NetFields, "netHome.NetFields").AddField(netAnimationEvent, "netAnimationEvent")
			.AddField(isPrismatic, "isPrismatic");
		netAnimationEvent.onEvent += doAnimationEvent;
	}

	/// <inheritdoc />
	public override void ChooseAppearance(LocalizedContentManager content = null)
	{
		if (Sprite == null)
		{
			Sprite = new AnimatedSprite(content ?? Game1.content, "Characters\\Junimo");
		}
	}

	protected virtual void doAnimationEvent(int animId)
	{
		switch (animId)
		{
		case 0:
			Sprite.CurrentAnimation = null;
			break;
		case 2:
			Sprite.currentFrame = 0;
			break;
		case 3:
			Sprite.currentFrame = 1;
			break;
		case 4:
			Sprite.currentFrame = 2;
			break;
		case 5:
			Sprite.currentFrame = 44;
			break;
		case 6:
			Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
			{
				new FarmerSprite.AnimationFrame(12, 200),
				new FarmerSprite.AnimationFrame(13, 200),
				new FarmerSprite.AnimationFrame(14, 200),
				new FarmerSprite.AnimationFrame(15, 200)
			});
			break;
		case 7:
			Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
			{
				new FarmerSprite.AnimationFrame(44, 200),
				new FarmerSprite.AnimationFrame(45, 200),
				new FarmerSprite.AnimationFrame(46, 200),
				new FarmerSprite.AnimationFrame(47, 200)
			});
			break;
		case 8:
			Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
			{
				new FarmerSprite.AnimationFrame(28, 100),
				new FarmerSprite.AnimationFrame(29, 100),
				new FarmerSprite.AnimationFrame(30, 100),
				new FarmerSprite.AnimationFrame(31, 100)
			});
			break;
		case 1:
			break;
		}
	}

	public virtual void reachFirstDestinationFromHut(Character c, GameLocation l)
	{
		tryToHarvestHere();
	}

	public virtual void tryToHarvestHere()
	{
		if (base.currentLocation != null)
		{
			if (isHarvestable())
			{
				harvestTimer = 2000;
			}
			else
			{
				pokeToHarvest();
			}
		}
	}

	public virtual void pokeToHarvest()
	{
		JunimoHut junimoHut = home;
		if (junimoHut != null)
		{
			if (!junimoHut.isTilePassable(base.Tile) && Game1.IsMasterGame)
			{
				destroy = true;
			}
			else if (harvestTimer <= 0 && Game1.random.NextDouble() < 0.7)
			{
				pathfindToNewCrop();
			}
		}
	}

	public override bool shouldCollideWithBuildingLayer(GameLocation location)
	{
		return true;
	}

	public void setMoving(int xSpeed, int ySpeed)
	{
		motion.X = xSpeed;
		motion.Y = ySpeed;
	}

	public void setMoving(Vector2 motion)
	{
		this.motion = motion;
	}

	public override void Halt()
	{
		base.Halt();
		motion = Vector2.Zero;
	}

	public override bool canTalk()
	{
		return false;
	}

	public void junimoReachedHut(Character c, GameLocation l)
	{
		controller = null;
		motion.X = 0f;
		motion.Y = -1f;
		destroy = true;
	}

	public virtual bool foundCropEndFunction(PathNode currentNode, Point endPoint, GameLocation location, Character c)
	{
		if (location.terrainFeatures.TryGetValue(new Vector2(currentNode.x, currentNode.y), out var value))
		{
			if (location.isCropAtTile(currentNode.x, currentNode.y) && (value as HoeDirt).readyForHarvest())
			{
				return true;
			}
			if (value is Bush bush && bush.readyForHarvest())
			{
				return true;
			}
		}
		return false;
	}

	public virtual void pathfindToNewCrop()
	{
		JunimoHut junimoHut = home;
		if (junimoHut == null)
		{
			return;
		}
		if (Game1.timeOfDay > 1900)
		{
			if (controller == null)
			{
				returnToJunimoHut(base.currentLocation);
			}
			return;
		}
		if (Game1.random.NextDouble() < 0.035 || junimoHut.noHarvest.Value)
		{
			pathfindToRandomSpotAroundHut();
			return;
		}
		controller = new PathFindController(this, base.currentLocation, foundCropEndFunction, -1, reachFirstDestinationFromHut, 100, Point.Zero);
		Point? point = controller.pathToEndPoint?.Last();
		if (!point.HasValue || Math.Abs(point.Value.X - (junimoHut.tileX.Value + 1)) > junimoHut.cropHarvestRadius || Math.Abs(point.Value.Y - (junimoHut.tileY.Value + 1)) > junimoHut.cropHarvestRadius)
		{
			if (Game1.random.NextBool() && !junimoHut.lastKnownCropLocation.Equals(Point.Zero))
			{
				controller = new PathFindController(this, base.currentLocation, junimoHut.lastKnownCropLocation, -1, reachFirstDestinationFromHut, 100);
			}
			else if (Game1.random.NextDouble() < 0.25)
			{
				netAnimationEvent.Fire(0);
				returnToJunimoHut(base.currentLocation);
			}
			else
			{
				pathfindToRandomSpotAroundHut();
			}
		}
		else
		{
			netAnimationEvent.Fire(0);
		}
	}

	public virtual void returnToJunimoHut(GameLocation location)
	{
		if (Utility.isOnScreen(Utility.Vector2ToPoint(position.Value / 64f), 64, base.currentLocation))
		{
			jump();
		}
		collidesWithOtherCharacters.Value = false;
		if (Game1.IsMasterGame)
		{
			JunimoHut junimoHut = home;
			if (junimoHut == null)
			{
				return;
			}
			controller = new PathFindController(this, location, new Point(junimoHut.tileX.Value + 1, junimoHut.tileY.Value + 1), 0, junimoReachedHut);
			if (controller.pathToEndPoint == null || controller.pathToEndPoint.Count == 0 || location.isCollidingPosition(nextPosition, Game1.viewport, isFarmer: false, 0, glider: false, this))
			{
				destroy = true;
			}
		}
		if (Utility.isOnScreen(Utility.Vector2ToPoint(position.Value / 64f), 64, base.currentLocation))
		{
			location.playSound("junimoMeep1");
		}
	}

	public override void faceDirection(int direction)
	{
	}

	protected override void updateSlaveAnimation(GameTime time)
	{
	}

	protected virtual bool isHarvestable()
	{
		if (base.currentLocation.terrainFeatures.TryGetValue(base.Tile, out var value))
		{
			if (value is HoeDirt hoeDirt)
			{
				return hoeDirt.readyForHarvest();
			}
			if (value is Bush bush)
			{
				return bush.readyForHarvest();
			}
		}
		return false;
	}

	public override void update(GameTime time, GameLocation location)
	{
		netAnimationEvent.Poll();
		base.update(time, location);
		if (isPrismatic.Value)
		{
			color.Value = Utility.GetPrismaticColor(whichJunimoFromThisHut);
		}
		forceUpdateTimer = 99999;
		if (EventActor)
		{
			return;
		}
		if (destroy)
		{
			alphaChange = -0.05f;
		}
		alpha += alphaChange;
		if (alpha > 1f)
		{
			alpha = 1f;
		}
		else if (alpha < 0f)
		{
			alpha = 0f;
			if (destroy && Game1.IsMasterGame)
			{
				location.characters.Remove(this);
				home?.myJunimos.Remove(this);
			}
		}
		if (Game1.IsMasterGame)
		{
			if (harvestTimer > 0)
			{
				int num = harvestTimer;
				harvestTimer -= time.ElapsedGameTime.Milliseconds;
				if (harvestTimer > 1800)
				{
					netAnimationEvent.Fire(2);
				}
				else if (harvestTimer > 1600)
				{
					netAnimationEvent.Fire(3);
				}
				else if (harvestTimer > 1000)
				{
					netAnimationEvent.Fire(4);
					shake(50);
				}
				else if (num >= 1000 && harvestTimer < 1000)
				{
					netAnimationEvent.Fire(2);
					JunimoHut junimoHut = home;
					if (base.currentLocation != null && junimoHut != null && !junimoHut.noHarvest.Value && isHarvestable())
					{
						netAnimationEvent.Fire(5);
						lastItemHarvested = null;
						TerrainFeature terrainFeature = base.currentLocation.terrainFeatures[base.Tile];
						if (!(terrainFeature is Bush bush))
						{
							if (terrainFeature is HoeDirt hoeDirt && hoeDirt.crop.harvest(base.TilePoint.X, base.TilePoint.Y, hoeDirt, this))
							{
								hoeDirt.destroyCrop(base.currentLocation.farmers.Any());
							}
						}
						else if (bush.readyForHarvest())
						{
							tryToAddItemToHut(ItemRegistry.Create("(O)815"));
							bush.tileSheetOffset.Value = 0;
							bush.setUpSourceRect();
							if (Utility.isOnScreen(base.TilePoint, 64, base.currentLocation))
							{
								bush.performUseAction(base.Tile);
							}
							if (Utility.isOnScreen(base.TilePoint, 64, base.currentLocation))
							{
								DelayedAction.playSoundAfterDelay("coin", 260, base.currentLocation);
							}
						}
						if (lastItemHarvested != null)
						{
							bool flag = false;
							if (home.raisinDays.Value > 0 && Game1.random.NextDouble() < 0.2)
							{
								flag = true;
								Item one = lastItemHarvested.getOne();
								one.Quality = lastItemHarvested.Quality;
								tryToAddItemToHut(one);
							}
							if (base.currentLocation.farmers.Any())
							{
								ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(lastItemHarvested.QualifiedItemId);
								float num2 = (float)base.StandingPixel.Y / 10000f + 0.01f;
								if (flag)
								{
									for (int i = 0; i < 2; i++)
									{
										Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(dataOrErrorItem.TextureName, dataOrErrorItem.GetSourceRect(), 1000f, 1, 0, base.Position + new Vector2(0f, -40f), flicker: false, flipped: false, num2, 0.02f, Color.White, 4f, -0.01f, 0f, 0f)
										{
											motion = new Vector2((float)((i != 0) ? 1 : (-1)) * 0.5f, -0.25f),
											delayBeforeAnimationStart = 200
										});
										if (lastItemHarvested is ColoredObject coloredObject)
										{
											Rectangle sourceRect = ItemRegistry.GetDataOrErrorItem(lastItemHarvested.QualifiedItemId).GetSourceRect(1);
											Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(dataOrErrorItem.TextureName, sourceRect, 1000f, 1, 0, base.Position + new Vector2(0f, -40f), flicker: false, flipped: false, num2 + 0.005f, 0.02f, coloredObject.color.Value, 4f, -0.01f, 0f, 0f)
											{
												motion = new Vector2((float)((i != 0) ? 1 : (-1)) * 0.5f, -0.25f),
												delayBeforeAnimationStart = 200
											});
										}
									}
								}
								else
								{
									Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(dataOrErrorItem.TextureName, dataOrErrorItem.GetSourceRect(), 1000f, 1, 0, base.Position + new Vector2(0f, -40f), flicker: false, flipped: false, num2, 0.02f, Color.White, 4f, -0.01f, 0f, 0f)
									{
										motion = new Vector2(0.08f, -0.25f)
									});
									if (lastItemHarvested is ColoredObject coloredObject2)
									{
										Rectangle sourceRect2 = ItemRegistry.GetDataOrErrorItem(lastItemHarvested.QualifiedItemId).GetSourceRect(1);
										Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(dataOrErrorItem.TextureName, sourceRect2, 1000f, 1, 0, base.Position + new Vector2(0f, -40f), flicker: false, flipped: false, num2 + 0.005f, 0.02f, coloredObject2.color.Value, 4f, -0.01f, 0f, 0f)
										{
											motion = new Vector2(0.08f, -0.25f)
										});
									}
								}
							}
						}
					}
				}
				else if (harvestTimer <= 0)
				{
					pokeToHarvest();
				}
			}
			else if (alpha > 0f && controller == null)
			{
				if ((addedSpeed > 0f || base.speed > 3 || isCharging) && Game1.IsMasterGame)
				{
					destroy = true;
				}
				nextPosition = GetBoundingBox();
				nextPosition.X += (int)motion.X;
				bool flag2 = false;
				if (!location.isCollidingPosition(nextPosition, Game1.viewport, this))
				{
					position.X += (int)motion.X;
					flag2 = true;
				}
				nextPosition.X -= (int)motion.X;
				nextPosition.Y += (int)motion.Y;
				if (!location.isCollidingPosition(nextPosition, Game1.viewport, this))
				{
					position.Y += (int)motion.Y;
					flag2 = true;
				}
				if (!motion.Equals(Vector2.Zero) && flag2 && Game1.random.NextDouble() < 0.005)
				{
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(Game1.random.Choose(10, 11), base.Position, color.Value)
					{
						motion = motion / 4f,
						alphaFade = 0.01f,
						layerDepth = 0.8f,
						scale = 0.75f,
						alpha = 0.75f
					});
				}
				if (Game1.random.NextDouble() < 0.002)
				{
					switch (Game1.random.Next(6))
					{
					case 0:
						netAnimationEvent.Fire(6);
						break;
					case 1:
						netAnimationEvent.Fire(7);
						break;
					case 2:
						netAnimationEvent.Fire(0);
						break;
					case 3:
						jumpWithoutSound();
						yJumpVelocity /= 2f;
						netAnimationEvent.Fire(0);
						break;
					case 4:
					{
						JunimoHut junimoHut2 = home;
						if (junimoHut2 != null && !junimoHut2.noHarvest.Value)
						{
							pathfindToNewCrop();
						}
						break;
					}
					case 5:
						netAnimationEvent.Fire(8);
						break;
					}
				}
			}
		}
		bool flag3 = moveRight;
		bool flag4 = moveLeft;
		bool flag5 = moveUp;
		bool flag6 = moveDown;
		if (Game1.IsMasterGame)
		{
			if (controller == null && motion.Equals(Vector2.Zero))
			{
				return;
			}
			flag3 |= Math.Abs(motion.X) > Math.Abs(motion.Y) && motion.X > 0f;
			flag4 |= Math.Abs(motion.X) > Math.Abs(motion.Y) && motion.X < 0f;
			flag5 |= Math.Abs(motion.Y) > Math.Abs(motion.X) && motion.Y < 0f;
			flag6 |= Math.Abs(motion.Y) > Math.Abs(motion.X) && motion.Y > 0f;
		}
		else
		{
			flag4 = IsRemoteMoving() && FacingDirection == 3;
			flag3 = IsRemoteMoving() && FacingDirection == 1;
			flag5 = IsRemoteMoving() && FacingDirection == 0;
			flag6 = IsRemoteMoving() && FacingDirection == 2;
			if (!flag3 && !flag4 && !flag5 && !flag6)
			{
				return;
			}
		}
		Sprite.CurrentAnimation = null;
		if (flag3)
		{
			flip = false;
			if (Sprite.Animate(time, 16, 8, 50f))
			{
				Sprite.currentFrame = 16;
			}
		}
		else if (flag4)
		{
			if (Sprite.Animate(time, 16, 8, 50f))
			{
				Sprite.currentFrame = 16;
			}
			flip = true;
		}
		else if (flag5)
		{
			if (Sprite.Animate(time, 32, 8, 50f))
			{
				Sprite.currentFrame = 32;
			}
		}
		else if (flag6)
		{
			Sprite.Animate(time, 0, 8, 50f);
		}
	}

	public virtual void pathfindToRandomSpotAroundHut()
	{
		JunimoHut junimoHut = home;
		if (junimoHut != null)
		{
			controller = new PathFindController(endPoint: Utility.Vector2ToPoint(new Vector2(junimoHut.tileX.Value + 1 + Game1.random.Next(-junimoHut.cropHarvestRadius, junimoHut.cropHarvestRadius + 1), junimoHut.tileY.Value + 1 + Game1.random.Next(-junimoHut.cropHarvestRadius, junimoHut.cropHarvestRadius + 1))), c: this, location: base.currentLocation, finalFacingDirection: -1, endBehaviorFunction: reachFirstDestinationFromHut, limit: 100);
		}
	}

	public virtual void tryToAddItemToHut(Item i)
	{
		lastItemHarvested = i;
		Item item = home?.GetOutputChest().addItem(i);
		if (item != null)
		{
			for (int j = 0; j < item.Stack; j++)
			{
				Game1.createItemDebris(i.getOne(), base.Position, -1, base.currentLocation);
			}
		}
	}

	public override void draw(SpriteBatch b, float alpha = 1f)
	{
		if (this.alpha > 0f)
		{
			float num = (float)(base.StandingPixel.Y + 2) / 10000f;
			b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(Sprite.SpriteWidth * 4 / 2, (float)Sprite.SpriteHeight * 3f / 4f * 4f / (float)Math.Pow(Sprite.SpriteHeight / 16, 2.0) + (float)yJumpOffset - 8f) + ((shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), Sprite.SourceRect, color.Value * this.alpha, rotation, new Vector2(Sprite.SpriteWidth * 4 / 2, (float)(Sprite.SpriteHeight * 4) * 3f / 4f) / 4f, Math.Max(0.2f, scale.Value) * 4f, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : num));
			if (!swimming.Value)
			{
				b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, base.Position + new Vector2((float)(Sprite.SpriteWidth * 4) / 2f, 44f)), Game1.shadowTexture.Bounds, color.Value * this.alpha, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), (4f + (float)yJumpOffset / 40f) * scale.Value, SpriteEffects.None, Math.Max(0f, num) - 1E-06f);
			}
		}
	}
}
