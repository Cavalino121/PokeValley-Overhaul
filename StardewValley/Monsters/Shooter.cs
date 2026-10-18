using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Projectiles;

namespace StardewValley.Monsters;

public class Shooter : Monster
{
	public NetBool shooting = new NetBool();

	public int shotsLeft;

	public float nextShot;

	public int projectileSpeed = 12;

	public string projectileDebuff = "26";

	public int numberOfShotsPerFire = 1;

	public float aimTime = 0.25f;

	public float burstTime = 0.25f;

	public float aimEndTime = 1f;

	public int firedProjectile = 12;

	public string damageSound = "shadowHit";

	public string fireSound = "Cowboy_gunshot";

	public int projectileRange = 10;

	public int desiredDistance = 5;

	public int fireRange = 8;

	[XmlIgnore]
	public NetEvent0 fireEvent = new NetEvent0();

	public Shooter()
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(shooting, "shooting").AddField(fireEvent, "fireEvent");
		fireEvent.onEvent += OnFire;
	}

	public override int GetBaseDifficultyLevel()
	{
		return 1;
	}

	public virtual void OnFire()
	{
		shakeTimer = 250;
	}

	public override bool ShouldActuallyMoveAwayFromPlayer()
	{
		if (base.Player != null)
		{
			Point tilePoint = base.Player.TilePoint;
			Point tilePoint2 = base.TilePoint;
			if (Math.Abs(tilePoint.X - tilePoint2.X) < desiredDistance && Math.Abs(tilePoint.Y - tilePoint2.Y) < desiredDistance)
			{
				return true;
			}
		}
		return base.ShouldActuallyMoveAwayFromPlayer();
	}

	public Shooter(Vector2 position)
		: base("Shadow Sniper", position)
	{
		Sprite.SpriteHeight = 32;
		Sprite.SpriteWidth = 32;
		forceOneTileWide.Value = true;
		Sprite.UpdateSourceRect();
		InitializeVariant();
	}

	public Shooter(Vector2 position, string monster_name)
		: base(monster_name, position)
	{
		Sprite.SpriteHeight = 32;
		Sprite.SpriteWidth = 32;
		forceOneTileWide.Value = true;
		Sprite.UpdateSourceRect();
		InitializeVariant();
	}

	public virtual void InitializeVariant()
	{
		nextShot = 1f;
	}

	/// <inheritdoc />
	public override void reloadSprite(bool onlyAppearance = false)
	{
		Sprite = new AnimatedSprite("Characters\\Monsters\\" + base.Name);
		Sprite.SpriteHeight = 32;
		Sprite.UpdateSourceRect();
	}

	protected override void updateAnimation(GameTime time)
	{
		if (shooting.Value)
		{
			switch (FacingDirection)
			{
			case 2:
				Sprite.CurrentFrame = 16;
				break;
			case 1:
				Sprite.CurrentFrame = 17;
				break;
			case 0:
				Sprite.CurrentFrame = 18;
				break;
			case 3:
				Sprite.CurrentFrame = 19;
				break;
			}
		}
		if (!Game1.IsMasterGame && isMoving())
		{
			switch (FacingDirection)
			{
			case 0:
				Sprite.AnimateUp(time);
				break;
			case 3:
				Sprite.AnimateLeft(time);
				break;
			case 1:
				Sprite.AnimateRight(time);
				break;
			case 2:
				Sprite.AnimateDown(time);
				break;
			}
		}
	}

	public override void behaviorAtGameTick(GameTime time)
	{
		if (!shooting.Value)
		{
			if (nextShot > 0f)
			{
				nextShot -= (float)time.ElapsedGameTime.TotalSeconds;
			}
			else if (base.Player != null)
			{
				Point tilePoint = base.Player.TilePoint;
				Point tilePoint2 = base.TilePoint;
				int x = tilePoint.X;
				int y = tilePoint.Y;
				int x2 = tilePoint2.X;
				int y2 = tilePoint2.Y;
				if (Math.Abs(x - x2) <= fireRange && Math.Abs(y - y2) <= fireRange && (Math.Abs(x - x2) < 2 || Math.Abs(y - y2) < 2))
				{
					Halt();
					faceGeneralDirection(base.Player.getStandingPosition());
					shooting.Value = true;
					nextShot = aimTime;
					shotsLeft = numberOfShotsPerFire;
				}
			}
		}
		else
		{
			xVelocity = 0f;
			yVelocity = 0f;
			if (shotsLeft > 0)
			{
				if (nextShot > 0f)
				{
					nextShot -= (float)time.ElapsedGameTime.TotalSeconds;
					if (nextShot <= 0f)
					{
						Vector2 vector;
						float value;
						switch (FacingDirection)
						{
						case 0:
							vector = new Vector2(0f, -1f);
							value = 0f;
							break;
						case 3:
							vector = new Vector2(-1f, 0f);
							value = -(float)Math.PI / 2f;
							break;
						case 1:
							vector = new Vector2(1f, 0f);
							value = (float)Math.PI / 2f;
							break;
						case 2:
							vector = new Vector2(0f, 1f);
							value = (float)Math.PI;
							break;
						default:
							vector = Vector2.Zero;
							value = 0f;
							break;
						}
						vector *= (float)projectileSpeed;
						fireEvent.Fire();
						base.currentLocation.playSound(fireSound);
						BasicProjectile basicProjectile = new BasicProjectile(base.DamageToFarmer, firedProjectile, 0, 0, 0f, vector.X, vector.Y, base.Position, null, null, null, explode: false, damagesMonsters: false, base.currentLocation, this);
						basicProjectile.startingRotation.Value = value;
						basicProjectile.height.Value = 24f;
						basicProjectile.debuff.Value = projectileDebuff;
						basicProjectile.ignoreTravelGracePeriod.Value = true;
						basicProjectile.IgnoreLocationCollision = true;
						basicProjectile.maxTravelDistance.Value = 64 * projectileRange;
						base.currentLocation.projectiles.Add(basicProjectile);
						shotsLeft--;
						if (shotsLeft == 0)
						{
							nextShot = aimEndTime;
						}
						else
						{
							nextShot = burstTime;
						}
					}
				}
			}
			else if (nextShot > 0f)
			{
				nextShot -= (float)time.ElapsedGameTime.TotalSeconds;
			}
			else
			{
				shooting.Value = false;
				nextShot = 2f;
			}
		}
		base.behaviorAtGameTick(time);
	}

	public override void updateMovement(GameLocation location, GameTime time)
	{
		if (shooting.Value)
		{
			MovePosition(time, Game1.viewport, location);
		}
		else
		{
			base.updateMovement(location, time);
		}
	}

	public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
	{
		shooting.Value = false;
		shotsLeft = 0;
		nextShot = Math.Max(0.5f, nextShot);
		base.currentLocation.playSound(damageSound);
		return base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
	}

	protected override void localDeathAnimation()
	{
		if (base.Name == "Shadow Sniper")
		{
			Utility.makeTemporarySpriteJuicier(new TemporaryAnimatedSprite(45, base.Position, Color.White, 10), base.currentLocation);
			for (int i = 1; i < 3; i++)
			{
				base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(6, base.Position + new Vector2(0f, 1f) * 64f * i, Color.Gray * 0.75f, 10)
				{
					delayBeforeAnimationStart = i * 159
				});
				base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(6, base.Position + new Vector2(0f, -1f) * 64f * i, Color.Gray * 0.75f, 10)
				{
					delayBeforeAnimationStart = i * 159
				});
				base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(6, base.Position + new Vector2(1f, 0f) * 64f * i, Color.Gray * 0.75f, 10)
				{
					delayBeforeAnimationStart = i * 159
				});
				base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(6, base.Position + new Vector2(-1f, 0f) * 64f * i, Color.Gray * 0.75f, 10)
				{
					delayBeforeAnimationStart = i * 159
				});
			}
			base.currentLocation.localSound("shadowDie");
		}
	}

	protected override void sharedDeathAnimation()
	{
		Point standingPixel = base.StandingPixel;
		Game1.createRadialDebris(base.currentLocation, Sprite.textureName.Value, new Rectangle(Sprite.SourceRect.X, Sprite.SourceRect.Y, 16, 5), 16, standingPixel.X, standingPixel.Y - 32, 1, standingPixel.Y / 64, Color.White, 4f);
		Game1.createRadialDebris(base.currentLocation, Sprite.textureName.Value, new Rectangle(Sprite.SourceRect.X + 2, Sprite.SourceRect.Y + 5, 16, 5), 10, standingPixel.X, standingPixel.Y - 32, 1, standingPixel.Y / 64, Color.White, 4f);
	}

	public override void update(GameTime time, GameLocation location)
	{
		base.update(time, location);
		fireEvent.Poll();
	}
}
