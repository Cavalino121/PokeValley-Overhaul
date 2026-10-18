using System;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Locations;
using xTile.Dimensions;

namespace StardewValley.Monsters;

public class Leaper : Monster
{
	public NetFloat leapDuration = new NetFloat(0.75f);

	public NetFloat leapProgress = new NetFloat(0f);

	public NetBool leaping = new NetBool(value: false);

	public NetVector2 leapStartPosition = new NetVector2();

	public NetVector2 leapEndPosition = new NetVector2();

	public float nextLeap;

	public Leaper()
	{
	}

	public Leaper(Vector2 position)
		: base("Spider", position)
	{
		forceOneTileWide.Value = true;
		base.IsWalkingTowardPlayer = false;
		nextLeap = Utility.RandomFloat(1f, 1.5f);
		isHardModeMonster.Value = true;
		reloadSprite();
	}

	public override int GetBaseDifficultyLevel()
	{
		return 1;
	}

	/// <inheritdoc />
	public override void reloadSprite(bool onlyAppearance = false)
	{
		base.reloadSprite(onlyAppearance);
		Sprite.SpriteWidth = 32;
		Sprite.SpriteHeight = 32;
		Sprite.UpdateSourceRect();
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(leapDuration, "leapDuration").AddField(leapProgress, "leapProgress").AddField(leapStartPosition, "leapStartPosition")
			.AddField(leapEndPosition, "leapEndPosition")
			.AddField(leaping, "leaping");
		leapProgress.Interpolated(interpolate: true, wait: true);
		leaping.Interpolated(interpolate: true, wait: true);
		leaping.fieldChangeVisibleEvent += OnLeapingChanged;
	}

	public virtual void OnLeapingChanged(NetBool field, bool old_value, bool new_value)
	{
	}

	public override bool isInvincible()
	{
		if (leaping.Value)
		{
			return true;
		}
		return base.isInvincible();
	}

	public override void updateMovement(GameLocation location, GameTime time)
	{
	}

	protected override void localDeathAnimation()
	{
		base.currentLocation.localSound("monsterdead");
		Utility.makeTemporarySpriteJuicier(new TemporaryAnimatedSprite(44, base.Position, Color.DarkRed, 10)
		{
			holdLastFrame = true,
			alphaFade = 0.01f,
			interval = 70f
		}, base.currentLocation);
	}

	protected override void sharedDeathAnimation()
	{
	}

	public override void defaultMovementBehavior(GameTime time)
	{
	}

	public override void noMovementProgressNearPlayerBehavior()
	{
	}

	public override void update(GameTime time, GameLocation location)
	{
		farmerPassesThrough = true;
		base.update(time, location);
		if (leaping.Value)
		{
			yJumpGravity = 0f;
			float num = leapProgress.Value;
			if (!Game1.IsMasterGame)
			{
				float num2 = (leapStartPosition.Value - leapEndPosition.Value).Length();
				num = ((num2 != 0f) ? ((leapStartPosition.Value - base.Position).Length() / num2) : 0f);
				if (num < 0f)
				{
					num = 0f;
				}
				if (num > 1f)
				{
					num = 1f;
				}
			}
			yJumpOffset = (int)(Math.Sin((double)num * Math.PI) * -64.0 * 3.0);
		}
		else
		{
			yJumpOffset = 0;
		}
	}

	protected override void updateAnimation(GameTime time)
	{
		if (leaping.Value)
		{
			Sprite.CurrentFrame = 2;
		}
		else
		{
			Sprite.Animate(time, 0, 2, 500f);
		}
		Sprite.UpdateSourceRect();
	}

	public virtual bool IsValidLandingTile(Vector2 tile, bool check_other_characters = false)
	{
		if (base.currentLocation is MineShaft mineShaft && !mineShaft.isTileOnClearAndSolidGround(tile))
		{
			return false;
		}
		if (base.currentLocation.IsTileOccupiedBy(tile, ~(CollisionMask.Characters | CollisionMask.Farmers)) || !base.currentLocation.isTileOnMap(tile) || !base.currentLocation.isTilePassable(new Location((int)tile.X, (int)tile.Y), Game1.viewport))
		{
			return false;
		}
		Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
		if (check_other_characters && base.currentLocation != null)
		{
			foreach (NPC character in base.currentLocation.characters)
			{
				if (character != this && character.GetBoundingBox().Intersects(boundingBox))
				{
					return false;
				}
			}
		}
		return true;
	}

	public override void behaviorAtGameTick(GameTime time)
	{
		base.behaviorAtGameTick(time);
		if (leaping.Value)
		{
			leapProgress.Value += (float)time.ElapsedGameTime.TotalSeconds / leapDuration.Value;
			if (leapProgress.Value >= 1f)
			{
				leapProgress.Value = 1f;
			}
			base.Position = new Vector2(Utility.Lerp(leapStartPosition.X, leapEndPosition.X, leapProgress.Value), Utility.Lerp(leapStartPosition.Y, leapEndPosition.Y, leapProgress.Value));
			if (leapProgress.Value == 1f)
			{
				leaping.Value = false;
				leapProgress.Value = 0f;
				if (!IsValidLandingTile(base.Tile, check_other_characters: true))
				{
					nextLeap = 0.1f;
				}
			}
			return;
		}
		if (nextLeap > 0f)
		{
			nextLeap -= (float)time.ElapsedGameTime.TotalSeconds;
		}
		if (!(nextLeap <= 0f))
		{
			return;
		}
		Vector2? vector = null;
		Vector2 tile = base.Tile;
		tile.X = (int)tile.X;
		tile.X = (int)tile.X;
		if (withinPlayerThreshold(5) && base.Player != null)
		{
			Vector2 tile2 = base.Tile;
			if (Game1.random.NextDouble() < 0.6000000238418579)
			{
				nextLeap = Utility.RandomFloat(1.25f, 1.5f);
				tile2 = base.Player.Tile;
				tile2.X = (int)Math.Round(tile2.X);
				tile2.Y = (int)Math.Round(tile2.Y);
				tile2.X += Game1.random.Next(-1, 2);
				tile2.Y += Game1.random.Next(-1, 2);
			}
			else
			{
				nextLeap = Utility.RandomFloat(0.1f, 0.2f);
				tile2.X += Game1.random.Next(-1, 2);
				tile2.Y += Game1.random.Next(-1, 2);
			}
			if (IsValidLandingTile(tile2))
			{
				vector = tile2;
			}
		}
		if (!vector.HasValue)
		{
			for (int i = 0; i < 8; i++)
			{
				Vector2 vector2 = new Vector2(Game1.random.Next(-4, 5), Game1.random.Next(-4, 5));
				if (!(vector2 == Vector2.Zero))
				{
					Vector2 vector3 = tile + vector2;
					if (IsValidLandingTile(vector3))
					{
						nextLeap = Utility.RandomFloat(0.6f, 1.5f);
						vector = vector3;
						break;
					}
				}
			}
		}
		if (vector.HasValue)
		{
			if (Utility.isOnScreen(base.Position, 128))
			{
				base.currentLocation.playSound("batFlap");
			}
			leapProgress.Value = 0f;
			leaping.Value = true;
			leapStartPosition.Value = base.Position;
			leapEndPosition.Value = vector.Value * 64f;
		}
		else
		{
			nextLeap = Utility.RandomFloat(0.25f, 0.5f);
		}
	}

	public override void shedChunks(int number, float scale)
	{
		Point standingPixel = base.StandingPixel;
		Game1.createRadialDebris(base.currentLocation, Sprite.textureName.Value, new Microsoft.Xna.Framework.Rectangle(0, 64, 16, 16), 8, standingPixel.X, standingPixel.Y, number, base.TilePoint.Y, Color.White, 4f);
	}
}
