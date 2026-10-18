using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.Projectiles;
using StardewValley.SpecialOrders;

namespace StardewValley.Monsters;

public class GreenSlime : Monster
{
	public const float mutationFactor = 0.25f;

	public const int matingInterval = 120000;

	public const int childhoodLength = 120000;

	public const int durationOfMating = 2000;

	public const double chanceToMate = 0.001;

	public static int matingRange = 192;

	public const int AQUA_SLIME = 9999899;

	public NetIntDelta stackedSlimes = new NetIntDelta(0)
	{
		Minimum = 0
	};

	public float randomStackOffset;

	[XmlIgnore]
	public NetEvent1Field<Vector2, NetVector2> attackedEvent = new NetEvent1Field<Vector2, NetVector2>();

	[XmlElement("leftDrift")]
	public readonly NetBool leftDrift = new NetBool();

	[XmlElement("cute")]
	public readonly NetBool cute = new NetBool(value: true);

	[XmlIgnore]
	public int readyToJump = -1;

	[XmlIgnore]
	public int matingCountdown;

	[XmlIgnore]
	public new int yOffset;

	[XmlIgnore]
	public int wagTimer;

	public int readyToMate = 120000;

	[XmlElement("ageUntilFullGrown")]
	public readonly NetInt ageUntilFullGrown = new NetInt();

	public int animateTimer;

	public int timeSinceLastJump;

	[XmlElement("specialNumber")]
	public readonly NetInt specialNumber = new NetInt();

	[XmlElement("firstGeneration")]
	public readonly NetBool firstGeneration = new NetBool();

	[XmlElement("color")]
	public readonly NetColor color = new NetColor();

	private readonly NetBool pursuingMate = new NetBool();

	private readonly NetBool avoidingMate = new NetBool();

	private GreenSlime mate;

	public readonly NetBool prismatic = new NetBool();

	private readonly NetVector2 facePosition = new NetVector2();

	private readonly NetEvent1Field<Vector2, NetVector2> jumpEvent = new NetEvent1Field<Vector2, NetVector2>
	{
		InterpolationWait = false
	};

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(leftDrift, "leftDrift").AddField(cute, "cute").AddField(ageUntilFullGrown, "ageUntilFullGrown")
			.AddField(specialNumber, "specialNumber")
			.AddField(firstGeneration, "firstGeneration")
			.AddField(color, "color")
			.AddField(pursuingMate, "pursuingMate")
			.AddField(avoidingMate, "avoidingMate")
			.AddField(facePosition, "facePosition")
			.AddField(jumpEvent, "jumpEvent")
			.AddField(prismatic, "prismatic")
			.AddField(stackedSlimes, "stackedSlimes")
			.AddField(attackedEvent.NetFields, "attackedEvent.NetFields");
		attackedEvent.onEvent += OnAttacked;
		jumpEvent.onEvent += doJump;
	}

	public GreenSlime()
	{
	}

	public GreenSlime(Vector2 position)
		: base("Green Slime", position)
	{
		if (Game1.random.NextBool())
		{
			leftDrift.Value = true;
		}
		base.Slipperiness = 4;
		readyToMate = Game1.random.Next(1000, 120000);
		int num = Game1.random.Next(200, 256);
		color.Value = new Color(num / Game1.random.Next(2, 10), Game1.random.Next(180, 256), (Game1.random.NextDouble() < 0.1) ? 255 : (255 - num));
		firstGeneration.Value = true;
		flip = Game1.random.NextBool();
		cute.Value = Game1.random.NextDouble() < 0.49;
		base.HideShadow = true;
	}

	public GreenSlime(Vector2 position, int mineLevel)
		: base("Green Slime", position)
	{
		randomStackOffset = Utility.RandomFloat(0f, 100f);
		cute.Value = Game1.random.NextDouble() < 0.49;
		flip = Game1.random.NextBool();
		specialNumber.Value = Game1.random.Next(100);
		if (mineLevel < 40)
		{
			parseMonsterInfo("Green Slime");
			int num = Game1.random.Next(200, 256);
			color.Value = new Color(num / Game1.random.Next(2, 10), num, (Game1.random.NextDouble() < 0.01) ? 255 : (255 - num));
			if (Game1.random.NextDouble() < 0.01 && mineLevel % 5 != 0 && mineLevel % 5 != 1)
			{
				color.Value = new Color(205, 255, 0) * 0.7f;
				hasSpecialItem.Value = true;
				base.Health *= 3;
				base.DamageToFarmer *= 2;
			}
			if (Game1.random.NextDouble() < 0.01 && Game1.MasterPlayer.mailReceived.Contains("slimeHutchBuilt"))
			{
				objectsToDrop.Add("680");
			}
		}
		else if (mineLevel < 80)
		{
			base.Name = "Frost Jelly";
			parseMonsterInfo("Frost Jelly");
			int num2 = Game1.random.Next(200, 256);
			color.Value = new Color((Game1.random.NextDouble() < 0.01) ? 180 : (num2 / Game1.random.Next(2, 10)), (Game1.random.NextDouble() < 0.1) ? 255 : (255 - num2 / 3), num2);
			if (Game1.random.NextDouble() < 0.01 && mineLevel % 5 != 0 && mineLevel % 5 != 1)
			{
				color.Value = new Color(0, 0, 0) * 0.7f;
				hasSpecialItem.Value = true;
				base.Health *= 3;
				base.DamageToFarmer *= 2;
			}
			if (Game1.random.NextDouble() < 0.01 && Game1.MasterPlayer.mailReceived.Contains("slimeHutchBuilt"))
			{
				objectsToDrop.Add("413");
			}
		}
		else if (mineLevel >= 77377 && mineLevel < 77387)
		{
			base.Name = "Sludge";
			parseMonsterInfo("Sludge");
		}
		else if (mineLevel > 120)
		{
			base.Name = "Sludge";
			parseMonsterInfo("Sludge");
			color.Value = Color.BlueViolet;
			base.Health *= 2;
			int r = color.R;
			int g = color.G;
			int b = color.B;
			r += Game1.random.Next(-20, 21);
			g += Game1.random.Next(-20, 21);
			b += Game1.random.Next(-20, 21);
			color.R = (byte)Math.Max(Math.Min(255, r), 0);
			color.G = (byte)Math.Max(Math.Min(255, g), 0);
			color.B = (byte)Math.Max(Math.Min(255, b), 0);
			while (Game1.random.NextDouble() < 0.08)
			{
				objectsToDrop.Add("386");
			}
			if (Game1.random.NextDouble() < 0.009)
			{
				objectsToDrop.Add("337");
			}
			if (Game1.random.NextDouble() < 0.01 && Game1.MasterPlayer.mailReceived.Contains("slimeHutchBuilt"))
			{
				objectsToDrop.Add("439");
			}
		}
		else
		{
			base.Name = "Sludge";
			parseMonsterInfo("Sludge");
			int num3 = Game1.random.Next(200, 256);
			color.Value = new Color(num3, (Game1.random.NextDouble() < 0.01) ? 255 : (255 - num3), num3 / Game1.random.Next(2, 10));
			if (Game1.random.NextDouble() < 0.01 && mineLevel % 5 != 0 && mineLevel % 5 != 1)
			{
				color.Value = new Color(50, 10, 50) * 0.7f;
				hasSpecialItem.Value = true;
				base.Health *= 3;
				base.DamageToFarmer *= 2;
			}
			if (Game1.random.NextDouble() < 0.01 && Game1.MasterPlayer.mailReceived.Contains("slimeHutchBuilt"))
			{
				objectsToDrop.Add("437");
			}
		}
		if (cute.Value)
		{
			base.Health += base.Health / 4;
			base.DamageToFarmer++;
		}
		if (Game1.random.NextBool())
		{
			leftDrift.Value = true;
		}
		base.Slipperiness = 3;
		readyToMate = Game1.random.Next(1000, 120000);
		if (Game1.random.NextDouble() < 0.001)
		{
			color.Value = new Color(255, 255, 50);
			objectsToDrop.Add("GoldCoin");
			double val = (double)(int)(Game1.stats.DaysPlayed / 28) * 0.08;
			val = Math.Min(val, 0.55);
			while (Game1.random.NextDouble() < 0.1 + val)
			{
				objectsToDrop.Add("GoldCoin");
			}
		}
		if (mineLevel == 9999899)
		{
			color.Value = new Color(0, 255, 200);
			base.Health *= 2;
			objectsToDrop.Clear();
			if (Game1.random.NextDouble() < 0.02)
			{
				objectsToDrop.Add("394");
			}
			if (Game1.random.NextDouble() < 0.02)
			{
				objectsToDrop.Add("60");
			}
			if (Game1.random.NextDouble() < 0.02)
			{
				objectsToDrop.Add("62");
			}
			if (Game1.random.NextDouble() < 0.01)
			{
				objectsToDrop.Add("797");
			}
			if (Game1.random.NextDouble() < 0.03 && Game1.MasterPlayer.mailReceived.Contains("slimeHutchBuilt"))
			{
				objectsToDrop.Add("413");
			}
			while (Game1.random.NextBool())
			{
				objectsToDrop.Add("766");
			}
		}
		firstGeneration.Value = true;
		base.HideShadow = true;
	}

	public GreenSlime(Vector2 position, Color color)
		: base("Green Slime", position)
	{
		this.color.Value = color;
		firstGeneration.Value = true;
		base.HideShadow = true;
	}

	public void makeTigerSlime(bool onlyAppearance = false)
	{
		string text = base.Name;
		try
		{
			base.Name = "Tiger Slime";
			base.reloadSprite(onlyAppearance: false);
		}
		finally
		{
			if (onlyAppearance)
			{
				base.Name = text;
			}
		}
		Sprite.SpriteHeight = 24;
		Sprite.UpdateSourceRect();
		color.Value = Color.White;
		if (!onlyAppearance)
		{
			parseMonsterInfo("Tiger Slime");
		}
	}

	public void makePrismatic()
	{
		prismatic.Value = true;
		base.Name = "Prismatic Slime";
		base.Health = 1000;
		damageToFarmer.Value = 35;
		hasSpecialItem.Value = false;
	}

	/// <inheritdoc />
	public override void reloadSprite(bool onlyAppearance = false)
	{
		if (base.Name == "Tiger Slime")
		{
			makeTigerSlime(onlyAppearance);
			return;
		}
		string value = name.Value;
		try
		{
			base.Name = "Green Slime";
			base.reloadSprite(onlyAppearance);
		}
		finally
		{
			base.Name = value;
		}
		Sprite.SpriteHeight = 24;
		Sprite.UpdateSourceRect();
		base.HideShadow = true;
	}

	public virtual void OnAttacked(Vector2 trajectory)
	{
		if (Game1.IsMasterGame && stackedSlimes.Value > 0)
		{
			stackedSlimes.Value--;
			if (trajectory.LengthSquared() == 0f)
			{
				trajectory = new Vector2(0f, -1f);
			}
			else
			{
				trajectory.Normalize();
			}
			trajectory *= 16f;
			BasicProjectile basicProjectile = new BasicProjectile(base.DamageToFarmer / 3 * 2, 13, 3, 0, (float)Math.PI / 16f, trajectory.X, trajectory.Y, base.Position, null, null, null, explode: true, damagesMonsters: false, base.currentLocation, this);
			basicProjectile.height.Value = 24f;
			basicProjectile.color.Value = color.Value;
			basicProjectile.ignoreMeleeAttacks.Value = true;
			basicProjectile.hostTimeUntilAttackable = 0.1f;
			if (Game1.random.NextBool())
			{
				basicProjectile.debuff.Value = "13";
			}
			base.currentLocation.projectiles.Add(basicProjectile);
		}
	}

	public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
	{
		if (stackedSlimes.Value > 0)
		{
			attackedEvent.Fire(new Vector2(xTrajectory, -yTrajectory));
			xTrajectory = 0;
			yTrajectory = 0;
			damage = 1;
		}
		int num = Math.Max(1, damage - resilience.Value);
		if (Game1.random.NextDouble() < missChance.Value - missChance.Value * addedPrecision)
		{
			num = -1;
		}
		else
		{
			if (Game1.random.NextDouble() < 0.025 && cute.Value)
			{
				if (!base.focusedOnFarmers)
				{
					base.DamageToFarmer += base.DamageToFarmer / 2;
					shake(1000);
				}
				base.focusedOnFarmers = true;
			}
			base.Slipperiness = 3;
			base.Health -= num;
			setTrajectory(xTrajectory, yTrajectory);
			base.currentLocation.playSound("slimeHit");
			readyToJump = -1;
			base.IsWalkingTowardPlayer = true;
			if (base.Health <= 0)
			{
				base.currentLocation.playSound("slimedead");
				Game1.stats.SlimesKilled++;
				if (mate != null)
				{
					mate.mate = null;
				}
				if (Game1.gameMode == 3 && scale.Value > 1.8f)
				{
					base.Health = 10;
					int num2 = ((!(scale.Value > 1.8f)) ? 1 : Game1.random.Next(3, 5));
					base.Scale *= 2f / 3f;
					Rectangle boundingBox = GetBoundingBox();
					for (int i = 0; i < num2; i++)
					{
						GreenSlime greenSlime = new GreenSlime(base.Position + new Vector2(i * boundingBox.Width, 0f), Game1.CurrentMineLevel);
						greenSlime.setTrajectory(xTrajectory + Game1.random.Next(-20, 20), yTrajectory + Game1.random.Next(-20, 20));
						greenSlime.willDestroyObjectsUnderfoot = false;
						greenSlime.moveTowardPlayer(4);
						greenSlime.Scale = 0.75f + (float)Game1.random.Next(-5, 10) / 100f;
						base.currentLocation.characters.Add(greenSlime);
					}
				}
				else
				{
					Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(44, base.Position, color.Value * 0.66f, 10)
					{
						interval = 70f,
						holdLastFrame = true,
						alphaFade = 0.01f
					});
					Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(44, base.Position + new Vector2(-16f, 0f), color.Value * 0.66f, 10)
					{
						interval = 70f,
						delayBeforeAnimationStart = 0,
						holdLastFrame = true,
						alphaFade = 0.01f
					});
					Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(44, base.Position + new Vector2(0f, 16f), color.Value * 0.66f, 10)
					{
						interval = 70f,
						delayBeforeAnimationStart = 100,
						holdLastFrame = true,
						alphaFade = 0.01f
					});
					Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(44, base.Position + new Vector2(16f, 0f), color.Value * 0.66f, 10)
					{
						interval = 70f,
						delayBeforeAnimationStart = 200,
						holdLastFrame = true,
						alphaFade = 0.01f
					});
				}
			}
		}
		return num;
	}

	public override void shedChunks(int number, float scale)
	{
		Point standingPixel = base.StandingPixel;
		Game1.createRadialDebris(base.currentLocation, Sprite.textureName.Value, new Rectangle(0, 120, 16, 16), 8, standingPixel.X + 32, standingPixel.Y, number, base.TilePoint.Y, color.Value, 4f * scale);
	}

	public override void collisionWithFarmerBehavior()
	{
		farmerPassesThrough = base.Player.isWearingRing("520");
	}

	public override void onDealContactDamage(Farmer who)
	{
		if (Game1.random.NextDouble() < 0.3 && base.Player == Game1.player && !base.Player.temporarilyInvincible && !base.Player.isWearingRing("520") && Game1.random.Next(11) >= who.Immunity && !base.Player.hasBuff("28") && !base.Player.hasTrinketWithID("BasiliskPaw"))
		{
			base.Player.applyBuff("13");
			base.currentLocation.playSound("slime");
		}
		base.onDealContactDamage(who);
	}

	public override void draw(SpriteBatch b)
	{
		if (base.IsInvisible || !Utility.isOnScreen(base.Position, 128))
		{
			return;
		}
		int height = GetBoundingBox().Height;
		int y = base.StandingPixel.Y;
		for (int i = 0; i <= stackedSlimes.Value; i++)
		{
			bool flag = i == stackedSlimes.Value;
			Vector2 vector = Vector2.Zero;
			if (stackedSlimes.Value > 0)
			{
				vector = new Vector2((float)Math.Sin((double)randomStackOffset + Game1.currentGameTime.TotalGameTime.TotalSeconds * Math.PI * 2.0 + (double)(i * 30)) * 8f, -30 * i);
			}
			b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(32f, height / 2 + yOffset) + vector, Sprite.SourceRect, prismatic.Value ? Utility.GetPrismaticColor(348 + specialNumber.Value, 5f) : color.Value, 0f, new Vector2(8f, 16f), 4f * Math.Max(0.2f, scale.Value - 0.4f * ((float)ageUntilFullGrown.Value / 120000f)), SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : ((float)(y + i * 2) / 10000f)));
			b.Draw(Game1.shadowTexture, getLocalPosition(Game1.viewport) + new Vector2(32f, (float)(height / 2 * 7) / 4f + (float)yOffset + 8f * scale.Value - (float)((ageUntilFullGrown.Value > 0) ? 8 : 0)) + vector, Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f + scale.Value - (float)ageUntilFullGrown.Value / 120000f - ((Sprite.currentFrame % 4 % 3 != 0 || i != 0) ? 1f : 0f) + (float)yOffset / 30f, SpriteEffects.None, (float)(y - 1 + i * 2) / 10000f);
			if (ageUntilFullGrown.Value <= 0)
			{
				if (flag && (cute.Value || hasSpecialItem.Value))
				{
					int x = ((isMoving() || wagTimer > 0) ? (16 * Math.Min(7, Math.Abs(((wagTimer > 0) ? (992 - wagTimer) : (Game1.currentGameTime.TotalGameTime.Milliseconds % 992)) - 496) / 62) % 64) : 48);
					int num = ((isMoving() || wagTimer > 0) ? (24 * Math.Min(1, Math.Max(1, Math.Abs(((wagTimer > 0) ? (992 - wagTimer) : (Game1.currentGameTime.TotalGameTime.Milliseconds % 992)) - 496) / 62) / 4)) : 24);
					if (hasSpecialItem.Value)
					{
						num += 48;
					}
					b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + vector + new Vector2(32f, height - 16 + ((readyToJump <= 0) ? (4 * (-2 + Math.Abs(Sprite.currentFrame % 4 - 2))) : (4 + 4 * (Sprite.currentFrame % 4 % 3))) + yOffset) * scale.Value, new Rectangle(x, 168 + num, 16, 24), hasSpecialItem.Value ? Color.White : color.Value, 0f, new Vector2(8f, 16f), 4f * Math.Max(0.2f, scale.Value - 0.4f * ((float)ageUntilFullGrown.Value / 120000f)), flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : ((float)y / 10000f + 0.0001f)));
				}
				b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + vector + (new Vector2(32f, height / 2 + ((readyToJump <= 0) ? (4 * (-2 + Math.Abs(Sprite.currentFrame % 4 - 2))) : (4 - 4 * (Sprite.currentFrame % 4 % 3))) + yOffset) + facePosition.Value) * Math.Max(0.2f, scale.Value - 0.4f * ((float)ageUntilFullGrown.Value / 120000f)), new Rectangle(32 + ((readyToJump > 0 || base.focusedOnFarmers) ? 16 : 0), 120 + ((readyToJump < 0 && (base.focusedOnFarmers || invincibleCountdown > 0)) ? 24 : 0), 16, 24), Color.White * ((FacingDirection == 0) ? 0.5f : 1f), 0f, new Vector2(8f, 16f), 4f * Math.Max(0.2f, scale.Value - 0.4f * ((float)ageUntilFullGrown.Value / 120000f)), SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : ((float)(y + i * 2) / 10000f + 0.0001f)));
			}
			if (isGlowing)
			{
				b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + vector + new Vector2(32f, height / 2 + yOffset), Sprite.SourceRect, glowingColor * glowingTransparency, 0f, new Vector2(8f, 16f), 4f * Math.Max(0.2f, scale.Value), SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.99f : ((float)y / 10000f + 0.001f)));
			}
		}
		if (pursuingMate.Value)
		{
			b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(32f, -32 + yOffset), new Rectangle(16, 120, 8, 8), Color.White, 0f, new Vector2(3f, 3f), 4f, SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : ((float)base.StandingPixel.Y / 10000f)));
		}
		else if (avoidingMate.Value)
		{
			b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(32f, -32 + yOffset), new Rectangle(24, 120, 8, 8), Color.White, 0f, new Vector2(4f, 4f), 4f, SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : ((float)base.StandingPixel.Y / 10000f)));
		}
	}

	public void moveTowardOtherSlime(GreenSlime other, bool moveAway, GameTime time)
	{
		Point standingPixel = base.StandingPixel;
		Point standingPixel2 = other.StandingPixel;
		int num = Math.Abs(standingPixel2.X - standingPixel.X);
		int num2 = Math.Abs(standingPixel2.Y - standingPixel.Y);
		if (num > 4 || num2 > 4)
		{
			int num3 = ((standingPixel2.X > standingPixel.X) ? 1 : (-1));
			int num4 = ((standingPixel2.Y > standingPixel.Y) ? 1 : (-1));
			if (moveAway)
			{
				num3 = -num3;
				num4 = -num4;
			}
			double num5 = (double)num / (double)(num + num2);
			if (Game1.random.NextDouble() < num5)
			{
				tryToMoveInDirection((num3 > 0) ? 1 : 3, isFarmer: false, base.DamageToFarmer, glider: false);
			}
			else
			{
				tryToMoveInDirection((num4 > 0) ? 2 : 0, isFarmer: false, base.DamageToFarmer, glider: false);
			}
		}
		Sprite.AnimateDown(time);
		if (invincibleCountdown > 0)
		{
			invincibleCountdown -= time.ElapsedGameTime.Milliseconds;
			if (invincibleCountdown <= 0)
			{
				stopGlowing();
			}
		}
	}

	public void doneMating()
	{
		readyToMate = 120000;
		matingCountdown = 2000;
		mate = null;
		pursuingMate.Value = false;
		avoidingMate.Value = false;
	}

	public override void noMovementProgressNearPlayerBehavior()
	{
		faceGeneralDirection(base.Player.getStandingPosition());
	}

	public void mateWith(GreenSlime mateToPursue, GameLocation location)
	{
		if (location.canSlimeMateHere())
		{
			GreenSlime greenSlime = new GreenSlime(Vector2.Zero);
			Utility.recursiveFindPositionForCharacter(greenSlime, location, base.Tile, 30);
			Random random = Utility.CreateRandom(Game1.stats.DaysPlayed, (double)Game1.uniqueIDForThisGame / 10.0, (double)scale.Value * 100.0, (double)mateToPursue.scale.Value * 100.0);
			switch (random.Next(4))
			{
			case 0:
				greenSlime.color.Value = new Color(Math.Min(255, Math.Max(0, color.R + random.Next((int)((float)(-color.R) * 0.25f), (int)((float)(int)color.R * 0.25f)))), Math.Min(255, Math.Max(0, color.G + random.Next((int)((float)(-color.G) * 0.25f), (int)((float)(int)color.G * 0.25f)))), Math.Min(255, Math.Max(0, color.B + random.Next((int)((float)(-color.B) * 0.25f), (int)((float)(int)color.B * 0.25f)))));
				break;
			case 1:
			case 2:
				greenSlime.color.Value = Utility.getBlendedColor(color.Value, mateToPursue.color.Value);
				break;
			case 3:
				greenSlime.color.Value = new Color(Math.Min(255, Math.Max(0, mateToPursue.color.R + random.Next((int)((float)(-mateToPursue.color.R) * 0.25f), (int)((float)(int)mateToPursue.color.R * 0.25f)))), Math.Min(255, Math.Max(0, mateToPursue.color.G + random.Next((int)((float)(-mateToPursue.color.G) * 0.25f), (int)((float)(int)mateToPursue.color.G * 0.25f)))), Math.Min(255, Math.Max(0, mateToPursue.color.B + random.Next((int)((float)(-mateToPursue.color.B) * 0.25f), (int)((float)(int)mateToPursue.color.B * 0.25f)))));
				break;
			}
			int r = greenSlime.color.R;
			int g = greenSlime.color.G;
			int b = greenSlime.color.B;
			greenSlime.Name = name.Value;
			if (greenSlime.Name == "Tiger Slime")
			{
				greenSlime.makeTigerSlime();
			}
			else if (r > 100 && b > 100 && g < 50)
			{
				greenSlime.parseMonsterInfo("Sludge");
				while (random.NextDouble() < 0.1)
				{
					greenSlime.objectsToDrop.Add("386");
				}
				if (random.NextDouble() < 0.01)
				{
					greenSlime.objectsToDrop.Add("337");
				}
			}
			else if (r >= 200 && g < 75)
			{
				greenSlime.parseMonsterInfo("Sludge");
			}
			else if (b >= 200 && r < 100)
			{
				greenSlime.parseMonsterInfo("Frost Jelly");
			}
			greenSlime.Health = random.Choose(base.Health, mateToPursue.Health);
			greenSlime.Health = Math.Max(1, base.Health + random.Next(-4, 5));
			greenSlime.DamageToFarmer = random.Choose(base.DamageToFarmer, mateToPursue.DamageToFarmer);
			greenSlime.DamageToFarmer = Math.Max(0, base.DamageToFarmer + random.Next(-1, 2));
			greenSlime.resilience.Value = random.Choose(resilience.Value, mateToPursue.resilience.Value);
			greenSlime.resilience.Value = Math.Max(0, resilience.Value + random.Next(-1, 2));
			greenSlime.missChance.Value = random.Choose(missChance.Value, mateToPursue.missChance.Value);
			greenSlime.missChance.Value = Math.Max(0.0, missChance.Value + (double)((float)random.Next(-1, 2) / 100f));
			greenSlime.Scale = random.Choose(scale.Value, mateToPursue.scale.Value);
			greenSlime.Scale = Math.Max(0.6f, Math.Min(1.5f, scale.Value + (float)random.Next(-2, 3) / 100f));
			greenSlime.Slipperiness = 8;
			base.speed = random.Choose(base.speed, mateToPursue.speed);
			if (random.NextDouble() < 0.015)
			{
				base.speed = Math.Max(1, Math.Min(6, base.speed + random.Next(-1, 2)));
			}
			greenSlime.setTrajectory(Utility.getAwayFromPositionTrajectory(greenSlime.GetBoundingBox(), getStandingPosition()) / 2f);
			greenSlime.ageUntilFullGrown.Value = 120000;
			greenSlime.Halt();
			greenSlime.firstGeneration.Value = false;
			if (Utility.isOnScreen(base.Position, 128))
			{
				base.currentLocation.playSound("slime");
			}
		}
		mateToPursue.doneMating();
		doneMating();
	}

	public override List<Item> getExtraDropItems()
	{
		List<Item> list = new List<Item>();
		if (name.Value != "Tiger Slime")
		{
			if (color.R >= 50 && color.R <= 100 && color.G >= 25 && color.G <= 50 && color.B <= 25)
			{
				list.Add(ItemRegistry.Create("(O)388", Game1.random.Next(3, 7)));
				if (Game1.random.NextDouble() < 0.1)
				{
					list.Add(ItemRegistry.Create("(O)709"));
				}
			}
			else if (color.R < 80 && color.G < 80 && color.B < 80)
			{
				list.Add(ItemRegistry.Create("(O)382"));
				Random random = Utility.CreateRandom((double)base.Position.X * 777.0, (double)base.Position.Y * 77.0, Game1.stats.DaysPlayed);
				if (random.NextDouble() < 0.05)
				{
					list.Add(ItemRegistry.Create("(O)553"));
				}
				if (random.NextDouble() < 0.05)
				{
					list.Add(ItemRegistry.Create("(O)539"));
				}
			}
			else if (color.R > 200 && color.G > 180 && color.B < 50)
			{
				list.Add(ItemRegistry.Create("(O)384", 2));
			}
			else if (color.R > 220 && color.G > 90 && color.G < 150 && color.B < 50)
			{
				list.Add(ItemRegistry.Create("(O)378", 2));
			}
			else if (color.R > 230 && color.G > 230 && color.B > 230)
			{
				if (color.R % 2 == 1)
				{
					list.Add(ItemRegistry.Create("(O)338"));
					if (color.G % 2 == 1)
					{
						list.Add(ItemRegistry.Create("(O)338"));
					}
				}
				else
				{
					list.Add(ItemRegistry.Create("(O)380"));
				}
				if ((color.R % 2 == 0 && color.G % 2 == 0 && color.B % 2 == 0) || color.Equals(Color.White))
				{
					list.Add(new Object("72", 1));
				}
			}
			else if (color.R > 150 && color.G > 150 && color.B > 150)
			{
				list.Add(ItemRegistry.Create("(O)390", 2));
			}
			else if (color.R > 150 && color.B > 180 && color.G < 50 && specialNumber.Value % (firstGeneration.Value ? 4 : 2) == 0)
			{
				list.Add(ItemRegistry.Create("(O)386", 2));
				if (firstGeneration.Value && Game1.random.NextDouble() < 0.005)
				{
					list.Add(ItemRegistry.Create("(O)485"));
				}
			}
		}
		if (Game1.MasterPlayer.mailReceived.Contains("slimeHutchBuilt") && specialNumber.Value == 1)
		{
			switch (base.Name)
			{
			case "Green Slime":
				list.Add(ItemRegistry.Create("(O)680"));
				break;
			case "Frost Jelly":
				list.Add(ItemRegistry.Create("(O)413"));
				break;
			case "Tiger Slime":
				list.Add(ItemRegistry.Create("(O)857"));
				break;
			}
		}
		if (base.Name == "Tiger Slime")
		{
			if (Game1.random.NextDouble() < 0.001)
			{
				list.Add(ItemRegistry.Create("(H)91"));
			}
			if (Game1.random.NextDouble() < 0.1)
			{
				list.Add(ItemRegistry.Create("(O)831"));
				while (Game1.random.NextBool())
				{
					list.Add(ItemRegistry.Create("(O)831"));
				}
			}
			else if (Game1.random.NextDouble() < 0.1)
			{
				list.Add(ItemRegistry.Create("(O)829"));
			}
			else if (Game1.random.NextDouble() < 0.02)
			{
				list.Add(ItemRegistry.Create("(O)833"));
				while (Game1.random.NextBool())
				{
					list.Add(ItemRegistry.Create("(O)833"));
				}
			}
			else if (Game1.random.NextDouble() < 0.006)
			{
				list.Add(ItemRegistry.Create("(O)835"));
			}
		}
		if (prismatic.Value && Game1.player.team.specialOrders.Where((SpecialOrder x) => x.questKey.Value == "Wizard2") != null)
		{
			Object obj = ItemRegistry.Create<Object>("(O)876");
			obj.specialItem = true;
			obj.questItem.Value = true;
			return new List<Item> { obj };
		}
		return list;
	}

	public override void dayUpdate(int dayOfMonth)
	{
		if (ageUntilFullGrown.Value > 0)
		{
			ageUntilFullGrown.Value /= 2;
		}
		if (readyToMate > 0)
		{
			readyToMate /= 2;
		}
		base.dayUpdate(dayOfMonth);
	}

	protected override void updateAnimation(GameTime time)
	{
		if (wagTimer > 0)
		{
			wagTimer -= (int)time.ElapsedGameTime.TotalMilliseconds;
		}
		if (stunTime.Value > 0)
		{
			yOffset = 0;
		}
		else
		{
			yOffset = Math.Max(yOffset - (int)Math.Abs(xVelocity + yVelocity) / 2, -64);
			if (yOffset < 0)
			{
				yOffset = Math.Min(0, yOffset + 4 + (int)((yOffset <= -64) ? ((float)(-yOffset) / 8f) : ((float)(-yOffset) / 16f)));
			}
			timeSinceLastJump += time.ElapsedGameTime.Milliseconds;
		}
		if (Game1.random.NextDouble() < 0.01 && wagTimer <= 0)
		{
			wagTimer = 992;
		}
		if (Math.Abs(xVelocity) >= 0.5f || Math.Abs(yVelocity) >= 0.5f)
		{
			Sprite.AnimateDown(time);
		}
		else if (!base.Position.Equals(lastPosition))
		{
			animateTimer = 500;
		}
		if (animateTimer > 0 && readyToJump <= 0)
		{
			animateTimer -= time.ElapsedGameTime.Milliseconds;
			Sprite.AnimateDown(time);
		}
		resetAnimationSpeed();
	}

	public override void update(GameTime time, GameLocation location)
	{
		base.update(time, location);
		jumpEvent.Poll();
		attackedEvent.Poll();
	}

	public override void behaviorAtGameTick(GameTime time)
	{
		if (mate == null)
		{
			pursuingMate.Value = false;
			avoidingMate.Value = false;
		}
		switch (FacingDirection)
		{
		case 2:
			if (facePosition.X > 0f)
			{
				facePosition.X -= 2f;
			}
			else if (facePosition.X < 0f)
			{
				facePosition.X += 2f;
			}
			if (facePosition.Y < 0f)
			{
				facePosition.Y += 2f;
			}
			break;
		case 1:
			if (facePosition.X < 8f)
			{
				facePosition.X += 2f;
			}
			if (facePosition.Y < 0f)
			{
				facePosition.Y += 2f;
			}
			break;
		case 3:
			if (facePosition.X > -8f)
			{
				facePosition.X -= 2f;
			}
			if (facePosition.Y < 0f)
			{
				facePosition.Y += 2f;
			}
			break;
		case 0:
			if (facePosition.X > 0f)
			{
				facePosition.X -= 2f;
			}
			else if (facePosition.X < 0f)
			{
				facePosition.X += 2f;
			}
			if (facePosition.Y > -8f)
			{
				facePosition.Y -= 2f;
			}
			break;
		}
		if (stackedSlimes.Value <= 0)
		{
			if (ageUntilFullGrown.Value <= 0)
			{
				readyToMate -= time.ElapsedGameTime.Milliseconds;
			}
			else
			{
				ageUntilFullGrown.Value -= time.ElapsedGameTime.Milliseconds;
			}
		}
		if (pursuingMate.Value && mate != null)
		{
			if (readyToMate <= -35000)
			{
				mate.doneMating();
				doneMating();
				return;
			}
			moveTowardOtherSlime(mate, moveAway: false, time);
			if (mate.mate != null && mate.pursuingMate.Value && !mate.mate.Equals(this))
			{
				doneMating();
				return;
			}
			Vector2 standingPosition = getStandingPosition();
			Vector2 standingPosition2 = mate.getStandingPosition();
			if (Vector2.Distance(standingPosition, standingPosition2) < (float)(GetBoundingBox().Width + 4))
			{
				if (mate.mate != null && mate.avoidingMate.Value && mate.mate.Equals(this))
				{
					mate.avoidingMate.Value = false;
					mate.matingCountdown = 2000;
					mate.pursuingMate.Value = true;
				}
				matingCountdown -= time.ElapsedGameTime.Milliseconds;
				if (base.currentLocation != null && matingCountdown <= 0 && pursuingMate.Value && (!base.currentLocation.isOutdoors.Value || Utility.getNumberOfCharactersInRadius(base.currentLocation, Utility.Vector2ToPoint(base.Position), 1) <= 4))
				{
					mateWith(mate, base.currentLocation);
				}
			}
			else if (Vector2.Distance(standingPosition, standingPosition2) > (float)(matingRange * 2))
			{
				mate.mate = null;
				mate.avoidingMate.Value = false;
				mate = null;
			}
			return;
		}
		if (avoidingMate.Value && mate != null)
		{
			moveTowardOtherSlime(mate, moveAway: true, time);
			return;
		}
		if (readyToMate < 0 && cute.Value)
		{
			readyToMate = -1;
			if (Game1.random.NextDouble() < 0.001)
			{
				Point standingPixel = base.StandingPixel;
				GreenSlime greenSlime = (GreenSlime)Utility.checkForCharacterWithinArea(GetType(), base.Position, base.currentLocation, new Rectangle(standingPixel.X - matingRange, standingPixel.Y - matingRange, matingRange * 2, matingRange * 2));
				if (greenSlime != null && greenSlime.readyToMate <= 0 && !greenSlime.cute.Value && greenSlime.stackedSlimes.Value <= 0)
				{
					matingCountdown = 2000;
					mate = greenSlime;
					pursuingMate.Value = true;
					greenSlime.mate = this;
					greenSlime.avoidingMate.Value = true;
					addedSpeed = 1f;
					mate.addedSpeed = 1f;
					return;
				}
			}
		}
		else if (!isGlowing)
		{
			addedSpeed = 0f;
		}
		base.behaviorAtGameTick(time);
		if (readyToJump != -1)
		{
			Halt();
			base.IsWalkingTowardPlayer = false;
			readyToJump -= time.ElapsedGameTime.Milliseconds;
			Sprite.currentFrame = 16 + (800 - readyToJump) / 200;
			if (readyToJump <= 0)
			{
				timeSinceLastJump = timeSinceLastJump;
				base.Slipperiness = 10;
				base.IsWalkingTowardPlayer = true;
				readyToJump = -1;
				invincibleCountdown = 0;
				Vector2 awayFromPlayerTrajectory = Utility.getAwayFromPlayerTrajectory(GetBoundingBox(), base.Player);
				awayFromPlayerTrajectory.X = (0f - awayFromPlayerTrajectory.X) / 2f;
				awayFromPlayerTrajectory.Y = (0f - awayFromPlayerTrajectory.Y) / 2f;
				jumpEvent.Fire(awayFromPlayerTrajectory);
				setTrajectory((int)awayFromPlayerTrajectory.X, (int)awayFromPlayerTrajectory.Y);
			}
		}
		else if (Game1.random.NextDouble() < 0.1 && !base.focusedOnFarmers)
		{
			if (FacingDirection == 0 || FacingDirection == 2)
			{
				if (leftDrift.Value && !base.currentLocation.isCollidingPosition(nextPosition(3), Game1.viewport, isFarmer: false, 1, glider: false, this))
				{
					position.X -= base.speed;
				}
				else if (!leftDrift.Value && !base.currentLocation.isCollidingPosition(nextPosition(1), Game1.viewport, isFarmer: false, 1, glider: false, this))
				{
					position.X += base.speed;
				}
			}
			else if (leftDrift.Value && !base.currentLocation.isCollidingPosition(nextPosition(0), Game1.viewport, isFarmer: false, 1, glider: false, this))
			{
				position.Y -= base.speed;
			}
			else if (!leftDrift.Value && !base.currentLocation.isCollidingPosition(nextPosition(2), Game1.viewport, isFarmer: false, 1, glider: false, this))
			{
				position.Y += base.speed;
			}
			if (Game1.random.NextDouble() < 0.08)
			{
				leftDrift.Value = !leftDrift.Value;
			}
		}
		else if (withinPlayerThreshold() && timeSinceLastJump > (base.focusedOnFarmers ? 1000 : 4000) && Game1.random.NextDouble() < 0.01 && stackedSlimes.Value <= 0)
		{
			if (base.Name.Equals("Frost Jelly") && Game1.random.NextDouble() < 0.25)
			{
				addedSpeed = 2f;
				startGlowing(Color.Cyan, border: false, 0.15f);
			}
			else
			{
				addedSpeed = 0f;
				stopGlowing();
				readyToJump = 800;
			}
		}
	}

	private void doJump(Vector2 trajectory)
	{
		if (Utility.isOnScreen(base.Position, 128))
		{
			base.currentLocation.localSound("slime");
		}
		Sprite.currentFrame = 1;
	}
}
