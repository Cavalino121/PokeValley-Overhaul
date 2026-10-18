using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.Projectiles;
using xTile.Dimensions;
using xTile.Layers;

namespace StardewValley.Monsters;

public class Ghost : Monster
{
	public enum GhostVariant
	{
		Normal,
		Putrid
	}

	public const float rotationIncrement = (float)Math.PI / 64f;

	[XmlIgnore]
	public int wasHitCounter;

	[XmlIgnore]
	public float targetRotation;

	[XmlIgnore]
	public bool turningRight;

	[XmlIgnore]
	public int identifier = Game1.random.Next(-99999, 99999);

	[XmlIgnore]
	public new int yOffset;

	[XmlIgnore]
	public int yOffsetExtra;

	[XmlIgnore]
	public string lightSourceId;

	public NetInt currentState = new NetInt(0);

	public float stateTimer = -1f;

	public float nextParticle;

	public NetEnum<GhostVariant> variant = new NetEnum<GhostVariant>(GhostVariant.Normal);

	public Ghost()
	{
		lightSourceId = GenerateLightSourceId(identifier);
	}

	public Ghost(Vector2 position)
		: base("Ghost", position)
	{
		lightSourceId = GenerateLightSourceId(identifier);
		base.Slipperiness = 8;
		isGlider.Value = true;
		base.HideShadow = true;
	}

	/// <summary>
	/// constructor for non-default ghosts
	/// </summary>
	/// <param name="position"></param>
	/// <param name="name"></param>
	public Ghost(Vector2 position, string name)
		: base(name, position)
	{
		lightSourceId = GenerateLightSourceId(identifier);
		base.Slipperiness = 8;
		isGlider.Value = true;
		base.HideShadow = true;
		if (name == "Putrid Ghost")
		{
			variant.Value = GhostVariant.Putrid;
		}
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(variant, "variant").AddField(currentState, "currentState");
		currentState.fieldChangeVisibleEvent += delegate
		{
			stateTimer = -1f;
		};
	}

	/// <inheritdoc />
	public override void reloadSprite(bool onlyAppearance = false)
	{
		Sprite = new AnimatedSprite("Characters\\Monsters\\" + name.Value);
	}

	public override int GetBaseDifficultyLevel()
	{
		if (variant.Value == GhostVariant.Putrid)
		{
			return 1;
		}
		return base.GetBaseDifficultyLevel();
	}

	public override List<Item> getExtraDropItems()
	{
		if (Game1.random.NextDouble() < 0.095 && Game1.player.team.SpecialOrderActive("Wizard") && !Game1.MasterPlayer.hasOrWillReceiveMail("ectoplasmDrop"))
		{
			Object obj = ItemRegistry.Create<Object>("(O)875");
			obj.specialItem = true;
			obj.questItem.Value = true;
			return new List<Item> { obj };
		}
		return base.getExtraDropItems();
	}

	public override void drawAboveAllLayers(SpriteBatch b)
	{
		int y = base.StandingPixel.Y;
		b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(32f, 21 + yOffset), Sprite.SourceRect, Color.White, 0f, new Vector2(8f, 16f), Math.Max(0.2f, scale.Value) * 4f, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : ((float)y / 10000f)));
		b.Draw(Game1.shadowTexture, getLocalPosition(Game1.viewport) + new Vector2(32f, 64f), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f + (float)yOffset / 20f, SpriteEffects.None, (float)(y - 1) / 10000f);
	}

	public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
	{
		if (variant.Value == GhostVariant.Putrid && currentState.Value <= 2)
		{
			currentState.Value = 0;
		}
		int num = Math.Max(1, damage - resilience.Value);
		base.Slipperiness = 8;
		Utility.addSprinklesToLocation(base.currentLocation, base.TilePoint.X, base.TilePoint.Y, 2, 2, 101, 50, Color.LightBlue);
		if (Game1.random.NextDouble() < missChance.Value - missChance.Value * addedPrecision)
		{
			num = -1;
		}
		else
		{
			base.Health -= num;
			if (base.Health <= 0)
			{
				deathAnimation();
			}
			setTrajectory(xTrajectory, yTrajectory);
		}
		addedSpeed = -1f;
		Utility.removeLightSource(lightSourceId);
		return num;
	}

	protected override void localDeathAnimation()
	{
		base.currentLocation.localSound("ghost");
		base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(Sprite.textureName.Value, new Microsoft.Xna.Framework.Rectangle(0, 96, 16, 24), 100f, 4, 0, base.Position, flicker: false, flipped: false, 0.9f, 0.001f, Color.White, 4f, 0.01f, 0f, (float)Math.PI / 64f));
	}

	protected override void sharedDeathAnimation()
	{
	}

	protected override void updateAnimation(GameTime time)
	{
		nextParticle -= (float)time.ElapsedGameTime.TotalSeconds;
		if (nextParticle <= 0f)
		{
			nextParticle = 1f;
			if (variant.Value == GhostVariant.Putrid)
			{
				if (currentLocationRef.Value != null)
				{
					int y = base.StandingPixel.Y;
					TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(Sprite.textureName.Value, new Microsoft.Xna.Framework.Rectangle(Game1.random.Next(4) * 16, 168, 16, 24), 100f, 1, 10, base.Position + new Vector2(Utility.RandomFloat(-16f, 16f), Utility.RandomFloat(-16f, 0f) - (float)yOffset), flicker: false, flipped: false, (float)y / 10000f, 0.01f, Color.White, 4f, -0.01f, 0f, 0f);
					temporaryAnimatedSprite.acceleration = new Vector2(0f, 0.025f);
					base.currentLocation.temporarySprites.Add(temporaryAnimatedSprite);
				}
				nextParticle = Utility.RandomFloat(0.3f, 0.5f);
			}
		}
		yOffset = (int)(Math.Sin((double)((float)time.TotalGameTime.Milliseconds / 1000f) * (Math.PI * 2.0)) * 20.0) - yOffsetExtra;
		if (base.currentLocation == Game1.currentLocation)
		{
			if (Game1.currentLightSources.TryGetValue(lightSourceId, out var value))
			{
				value.position.Value = new Vector2(base.Position.X + 32f, base.Position.Y + 64f + (float)yOffset);
			}
			else if (name.Value == "Carbon Ghost")
			{
				Game1.currentLightSources.Add(new LightSource(lightSourceId, 4, new Vector2(base.Position.X + 8f, base.Position.Y + 64f), 1f, new Color(80, 30, 0), LightSource.LightContext.None, 0L, Game1.currentLocation.NameOrUniqueName));
			}
			else
			{
				Game1.currentLightSources.Add(new LightSource(lightSourceId, 5, new Vector2(base.Position.X + 8f, base.Position.Y + 64f), 1f, Color.White * 0.7f, LightSource.LightContext.None, 0L, Game1.currentLocation.NameOrUniqueName));
			}
		}
		if (variant.Value == GhostVariant.Putrid && UpdateVariantAnimation(time))
		{
			return;
		}
		Point standingPixel = base.StandingPixel;
		Point standingPixel2 = base.Player.StandingPixel;
		float num = -(standingPixel2.X - standingPixel.X);
		float num2 = standingPixel2.Y - standingPixel.Y;
		float num3 = 400f;
		num /= num3;
		num2 /= num3;
		if (wasHitCounter <= 0)
		{
			targetRotation = (float)Math.Atan2(0f - num2, num) - (float)Math.PI / 2f;
			if ((double)(Math.Abs(targetRotation) - Math.Abs(rotation)) > Math.PI * 7.0 / 8.0 && Game1.random.NextBool())
			{
				turningRight = true;
			}
			else if ((double)(Math.Abs(targetRotation) - Math.Abs(rotation)) < Math.PI / 8.0)
			{
				turningRight = false;
			}
			if (turningRight)
			{
				rotation -= (float)Math.Sign(targetRotation - rotation) * ((float)Math.PI / 64f);
			}
			else
			{
				rotation += (float)Math.Sign(targetRotation - rotation) * ((float)Math.PI / 64f);
			}
			rotation %= (float)Math.PI * 2f;
			wasHitCounter = 0;
		}
		float num4 = Math.Min(4f, Math.Max(1f, 5f - num3 / 64f / 2f));
		num = (float)Math.Cos((double)rotation + Math.PI / 2.0);
		num2 = 0f - (float)Math.Sin((double)rotation + Math.PI / 2.0);
		xVelocity += (0f - num) * num4 / 6f + (float)Game1.random.Next(-10, 10) / 100f;
		yVelocity += (0f - num2) * num4 / 6f + (float)Game1.random.Next(-10, 10) / 100f;
		if (Math.Abs(xVelocity) > Math.Abs((0f - num) * 5f))
		{
			xVelocity -= (0f - num) * num4 / 6f;
		}
		if (Math.Abs(yVelocity) > Math.Abs((0f - num2) * 5f))
		{
			yVelocity -= (0f - num2) * num4 / 6f;
		}
		faceGeneralDirection(base.Player.getStandingPosition(), 0, opposite: false, useTileCalculations: false);
		resetAnimationSpeed();
	}

	public virtual bool UpdateVariantAnimation(GameTime time)
	{
		if (variant.Value == GhostVariant.Putrid)
		{
			if (currentState.Value == 0)
			{
				if (Sprite.CurrentFrame >= 20)
				{
					Sprite.CurrentFrame = 0;
				}
				return false;
			}
			if (currentState.Value >= 1 && currentState.Value <= 3)
			{
				shakeTimer = 250;
				if (base.Player != null)
				{
					faceGeneralDirection(base.Player.getStandingPosition(), 0, opposite: false, useTileCalculations: false);
				}
				switch (FacingDirection)
				{
				case 2:
					Sprite.CurrentFrame = 20;
					break;
				case 1:
					Sprite.CurrentFrame = 21;
					break;
				case 0:
					Sprite.CurrentFrame = 22;
					break;
				case 3:
					Sprite.CurrentFrame = 23;
					break;
				}
			}
			else if (currentState.Value >= 4)
			{
				shakeTimer = 250;
				switch (FacingDirection)
				{
				case 2:
					Sprite.CurrentFrame = 24;
					break;
				case 1:
					Sprite.CurrentFrame = 25;
					break;
				case 0:
					Sprite.CurrentFrame = 26;
					break;
				case 3:
					Sprite.CurrentFrame = 27;
					break;
				}
			}
			return true;
		}
		return false;
	}

	public override void noMovementProgressNearPlayerBehavior()
	{
	}

	public override void behaviorAtGameTick(GameTime time)
	{
		if (stateTimer > 0f)
		{
			stateTimer -= (float)time.ElapsedGameTime.TotalSeconds;
			if (stateTimer <= 0f)
			{
				stateTimer = 0f;
			}
		}
		if (variant.Value == GhostVariant.Putrid)
		{
			Farmer player = base.Player;
			switch (currentState.Value)
			{
			case 0:
				if (stateTimer == -1f)
				{
					stateTimer = Utility.RandomFloat(1f, 2f);
				}
				if (player != null && stateTimer == 0f && Math.Abs(player.Position.X - base.Position.X) < 448f && Math.Abs(player.Position.Y - base.Position.Y) < 448f)
				{
					currentState.Value = 1;
					base.currentLocation.playSound("croak");
					stateTimer = 0.5f;
				}
				break;
			case 1:
				xVelocity = 0f;
				yVelocity = 0f;
				if (stateTimer <= 0f)
				{
					currentState.Value = 2;
				}
				break;
			case 2:
			{
				if (player == null)
				{
					currentState.Value = 0;
					break;
				}
				if (Math.Abs(player.Position.X - base.Position.X) < 80f && Math.Abs(player.Position.Y - base.Position.Y) < 80f)
				{
					currentState.Value = 3;
					stateTimer = 0.05f;
					xVelocity = 0f;
					yVelocity = 0f;
					break;
				}
				Vector2 vector = player.getStandingPosition() - getStandingPosition();
				if (vector.LengthSquared() == 0f)
				{
					currentState.Value = 3;
					stateTimer = 0.15f;
					break;
				}
				vector.Normalize();
				vector *= 10f;
				xVelocity = vector.X;
				yVelocity = 0f - vector.Y;
				break;
			}
			case 3:
				xVelocity = 0f;
				yVelocity = 0f;
				if (stateTimer <= 0f)
				{
					currentState.Value = 4;
					stateTimer = 1f;
					Vector2 vector2 = FacingDirection switch
					{
						0 => new Vector2(0f, -1f), 
						3 => new Vector2(-1f, 0f), 
						1 => new Vector2(1f, 0f), 
						2 => new Vector2(0f, 1f), 
						_ => Vector2.Zero, 
					};
					vector2 *= 6f;
					base.currentLocation.playSound("fishSlap");
					BasicProjectile basicProjectile = new BasicProjectile(base.DamageToFarmer, 7, 0, 1, (float)Math.PI / 32f, vector2.X, vector2.Y, base.Position, null, null, null, explode: false, damagesMonsters: false, base.currentLocation, this);
					basicProjectile.debuff.Value = "25";
					basicProjectile.scaleGrow.Value = 0.05f;
					basicProjectile.ignoreTravelGracePeriod.Value = true;
					basicProjectile.IgnoreLocationCollision = true;
					basicProjectile.maxTravelDistance.Value = 192;
					base.currentLocation.projectiles.Add(basicProjectile);
				}
				break;
			case 4:
				if (stateTimer <= 0f)
				{
					xVelocity = 0f;
					yVelocity = 0f;
					currentState.Value = 0;
					stateTimer = Utility.RandomFloat(3f, 4f);
				}
				break;
			}
		}
		base.behaviorAtGameTick(time);
		Microsoft.Xna.Framework.Rectangle boundingBox = base.Player.GetBoundingBox();
		if (!GetBoundingBox().Intersects(boundingBox) || !base.Player.temporarilyInvincible || currentState.Value != 0)
		{
			return;
		}
		Layer layer = base.currentLocation.map.RequireLayer("Back");
		Point center = boundingBox.Center;
		int i = 0;
		Vector2 vector3 = new Vector2(center.X / 64 + Game1.random.Next(-12, 12), center.Y / 64 + Game1.random.Next(-12, 12));
		for (; i < 3; i++)
		{
			if (!(vector3.X >= (float)layer.LayerWidth) && !(vector3.Y >= (float)layer.LayerHeight) && !(vector3.X < 0f) && !(vector3.Y < 0f) && layer.Tiles[(int)vector3.X, (int)vector3.Y] != null && base.currentLocation.isTilePassable(new Location((int)vector3.X, (int)vector3.Y), Game1.viewport) && !vector3.Equals(new Vector2(center.X / 64, center.Y / 64)))
			{
				break;
			}
			vector3 = new Vector2(center.X / 64 + Game1.random.Next(-12, 12), center.Y / 64 + Game1.random.Next(-12, 12));
		}
		if (i < 3)
		{
			base.Position = new Vector2(vector3.X * 64f, vector3.Y * 64f - 32f);
			Halt();
		}
	}
}
