using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Extensions;

namespace StardewValley.Monsters;

public class Fly : Monster
{
	public const float rotationIncrement = (float)Math.PI / 64f;

	public const int volumeTileRange = 16;

	public const int spawnTime = 1000;

	[XmlIgnore]
	public int spawningCounter = 1000;

	[XmlIgnore]
	public int wasHitCounter;

	[XmlIgnore]
	public float targetRotation;

	public static ICue buzz;

	[XmlIgnore]
	public bool turningRight;

	public bool hard;

	public Fly()
	{
	}

	public Fly(Vector2 position)
		: this(position, hard: false)
	{
	}

	public Fly(Vector2 position, bool hard)
		: base("Fly", position)
	{
		base.Slipperiness = 24 + Game1.random.Next(-10, 10);
		Halt();
		base.IsWalkingTowardPlayer = false;
		this.hard = hard;
		if (hard)
		{
			base.DamageToFarmer *= 2;
			base.MaxHealth *= 3;
			base.Health = base.MaxHealth;
		}
		base.HideShadow = true;
	}

	public void setHard()
	{
		hard = true;
		if (hard)
		{
			base.DamageToFarmer = 12;
			base.MaxHealth = 66;
			base.Health = base.MaxHealth;
		}
	}

	/// <inheritdoc />
	public override void reloadSprite(bool onlyAppearance = false)
	{
		Sprite = new AnimatedSprite("Characters\\Monsters\\Fly");
		base.HideShadow = true;
		if (!onlyAppearance)
		{
			buzz = Game1.soundBank.GetCue("flybuzzing");
		}
	}

	public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
	{
		int num = Math.Max(1, damage - resilience.Value);
		if (Game1.random.NextDouble() < missChance.Value - missChance.Value * addedPrecision)
		{
			num = -1;
		}
		else
		{
			base.Health -= num;
			setTrajectory(xTrajectory / 3, yTrajectory / 3);
			wasHitCounter = 500;
			base.currentLocation?.playSound("hitEnemy");
			if (base.Health <= 0)
			{
				if (base.currentLocation != null)
				{
					base.currentLocation.playSound("monsterdead");
					Utility.makeTemporarySpriteJuicier(new TemporaryAnimatedSprite(44, base.Position, Color.HotPink, 10)
					{
						interval = 70f
					}, base.currentLocation);
				}
				buzz?.Stop(AudioStopOptions.AsAuthored);
			}
		}
		addedSpeed = Game1.random.Next(-1, 1);
		return num;
	}

	public override void drawAboveAllLayers(SpriteBatch b)
	{
		if (Utility.isOnScreen(base.Position, 128))
		{
			int height = GetBoundingBox().Height;
			int y = base.StandingPixel.Y;
			b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(32f, height / 2 - 32), Sprite.SourceRect, hard ? Color.Lime : Color.White, rotation, new Vector2(8f, 16f), Math.Max(0.2f, scale.Value) * 4f, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : ((float)(y + 8) / 10000f)));
			b.Draw(Game1.shadowTexture, getLocalPosition(Game1.viewport) + new Vector2(32f, height / 2), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, (float)(y - 1) / 10000f);
			if (isGlowing)
			{
				b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(32f, height / 2 - 32), Sprite.SourceRect, glowingColor * glowingTransparency, rotation, new Vector2(8f, 16f), Math.Max(0.2f, scale.Value) * 4f, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.99f : ((float)y / 10000f + 0.001f)));
			}
		}
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		if (base.currentLocation != null && base.currentLocation.treatAsOutdoors.Value)
		{
			drawAboveAllLayers(b);
		}
	}

	protected override void updateAnimation(GameTime time)
	{
		if ((buzz == null || !buzz.IsPlaying) && (base.currentLocation == null || base.currentLocation.Equals(Game1.currentLocation)))
		{
			Game1.playSound("flybuzzing", out buzz);
			buzz.SetVariable("Volume", 0f);
		}
		if ((double)Game1.fadeToBlackAlpha > 0.8 && Game1.fadeIn && buzz != null)
		{
			buzz.Stop(AudioStopOptions.AsAuthored);
		}
		else if (buzz != null)
		{
			buzz.SetVariable("Volume", Math.Max(0f, buzz.GetVariable("Volume") - 1f));
			float num = Math.Max(0f, 100f - Vector2.Distance(base.Position, base.Player.Position) / 64f / 16f * 100f);
			if (num > buzz.GetVariable("Volume"))
			{
				buzz.SetVariable("Volume", num);
			}
		}
		if (wasHitCounter >= 0)
		{
			wasHitCounter -= time.ElapsedGameTime.Milliseconds;
		}
		Sprite.Animate(time, (FacingDirection == 0) ? 8 : ((FacingDirection != 2) ? (FacingDirection * 4) : 0), 4, 75f);
		if (spawningCounter >= 0)
		{
			spawningCounter -= time.ElapsedGameTime.Milliseconds;
			base.Scale = 1f - (float)spawningCounter / 1000f;
		}
		else if ((withinPlayerThreshold() || Utility.isOnScreen(base.Position, 256)) && invincibleCountdown <= 0)
		{
			faceDirection(0);
			Point standingPixel = base.StandingPixel;
			Point standingPixel2 = base.Player.StandingPixel;
			float num2 = -(standingPixel2.X - standingPixel.X);
			float num3 = standingPixel2.Y - standingPixel.Y;
			float num4 = Math.Max(1f, Math.Abs(num2) + Math.Abs(num3));
			if (num4 < 64f)
			{
				xVelocity = Math.Max(-7f, Math.Min(7f, xVelocity * 1.1f));
				yVelocity = Math.Max(-7f, Math.Min(7f, yVelocity * 1.1f));
			}
			num2 /= num4;
			num3 /= num4;
			if (wasHitCounter <= 0)
			{
				targetRotation = (float)Math.Atan2(0f - num3, num2) - (float)Math.PI / 2f;
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
				wasHitCounter = 5 + Game1.random.Next(-1, 2);
			}
			float num5 = Math.Min(7f, Math.Max(2f, 7f - num4 / 64f / 2f));
			num2 = (float)Math.Cos((double)rotation + Math.PI / 2.0);
			num3 = 0f - (float)Math.Sin((double)rotation + Math.PI / 2.0);
			xVelocity += (0f - num2) * num5 / 6f + (float)Game1.random.Next(-10, 10) / 100f;
			yVelocity += (0f - num3) * num5 / 6f + (float)Game1.random.Next(-10, 10) / 100f;
			if (Math.Abs(xVelocity) > Math.Abs((0f - num2) * 7f))
			{
				xVelocity -= (0f - num2) * num5 / 6f;
			}
			if (Math.Abs(yVelocity) > Math.Abs((0f - num3) * 7f))
			{
				yVelocity -= (0f - num3) * num5 / 6f;
			}
		}
		resetAnimationSpeed();
	}

	public override void behaviorAtGameTick(GameTime time)
	{
		base.behaviorAtGameTick(time);
		if (double.IsNaN(xVelocity) || double.IsNaN(yVelocity))
		{
			base.Health = -500;
		}
		if (base.Position.X <= -640f || base.Position.Y <= -640f || base.Position.X >= (float)(base.currentLocation.Map.Layers[0].LayerWidth * 64 + 640) || base.Position.Y >= (float)(base.currentLocation.Map.Layers[0].LayerHeight * 64 + 640))
		{
			base.Health = -500;
		}
	}

	public override void Removed()
	{
		base.Removed();
		buzz?.Stop(AudioStopOptions.AsAuthored);
	}
}
