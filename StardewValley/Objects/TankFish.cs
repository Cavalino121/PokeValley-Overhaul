using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects.Trinkets;

namespace StardewValley.Objects;

public class TankFish
{
	public enum FishType
	{
		Normal,
		Eel,
		Cephalopod,
		Float,
		Ground,
		Crawl,
		Hop,
		Static
	}

	/// <summary>The field index in <c>Data/AquariumFish</c> for the sprite index.</summary>
	public const int field_spriteIndex = 0;

	/// <summary>The field index in <c>Data/AquariumFish</c> for the type.</summary>
	public const int field_type = 1;

	/// <summary>The field index in <c>Data/AquariumFish</c> for the idle animations.</summary>
	public const int field_idleAnimations = 2;

	/// <summary>The field index in <c>Data/AquariumFish</c> for the dart start animation frames.</summary>
	public const int field_dartStartFrames = 3;

	/// <summary>The field index in <c>Data/AquariumFish</c> for the dart hold animation frames.</summary>
	public const int field_dartHoldFrames = 4;

	/// <summary>The field index in <c>Data/AquariumFish</c> for the dart end animation frames.</summary>
	public const int field_dartEndFrames = 5;

	/// <summary>The field index in <c>Data/AquariumFish</c> for the texture, if set.</summary>
	public const int field_texture = 6;

	/// <summary>The field index in <c>Data/AquariumFish</c> for the pixel offset from the upper-left corner of sprite that the hat sits on, if set.</summary>
	public const int field_hatOffset = 7;

	protected FishTankFurniture _tank;

	public Vector2 position;

	public float zPosition;

	public bool facingLeft;

	public Vector2 velocity = Vector2.Zero;

	protected Texture2D _texture;

	public float nextSwim;

	public string fishItemId = "";

	public int fishIndex;

	public int currentFrame;

	public Point? hatPosition;

	public int frogVariant;

	public int numberOfDarts;

	public FishType fishType;

	public float minimumVelocity;

	public float fishScale = 1f;

	public List<int> currentAnimation;

	public List<int> idleAnimation;

	public List<int> dartStartAnimation;

	public List<int> dartHoldAnimation;

	public List<int> dartEndAnimation;

	public int currentAnimationFrame;

	public float currentFrameTime;

	public float nextBubble;

	public bool isErrorFish;

	public TankFish(FishTankFurniture tank, Item item)
	{
		_tank = tank;
		fishItemId = item.ItemId;
		if (!_tank.GetAquariumData().TryGetValue(item.ItemId, out var value))
		{
			value = "0/float";
			isErrorFish = true;
		}
		string[] array = value.Split('/');
		string text = ArgUtility.Get(array, 6, null, allowBlank: false);
		if (text != null)
		{
			try
			{
				_texture = Game1.content.Load<Texture2D>(text);
			}
			catch (Exception)
			{
				isErrorFish = true;
			}
		}
		if (_texture == null)
		{
			_texture = _tank.GetAquariumTexture();
		}
		string text2 = ArgUtility.Get(array, 7, null, allowBlank: false);
		if (text2 != null)
		{
			try
			{
				string[] array2 = ArgUtility.SplitBySpace(text2);
				hatPosition = new Point(int.Parse(array2[0]), int.Parse(array2[1]));
			}
			catch (Exception)
			{
				hatPosition = null;
			}
		}
		fishIndex = int.Parse(array[0]);
		currentFrame = fishIndex;
		zPosition = Utility.RandomFloat(4f, 10f);
		fishScale = 0.75f;
		if (DataLoader.Fish(Game1.content).TryGetValue(item.ItemId, out var value2))
		{
			string[] array3 = value2.Split('/');
			if (!(array3[1] == "trap"))
			{
				minimumVelocity = Utility.RandomFloat(0.25f, 0.35f);
				if (array3[2] == "smooth")
				{
					minimumVelocity = Utility.RandomFloat(0.5f, 0.6f);
				}
				if (array3[2] == "dart")
				{
					minimumVelocity = 0f;
				}
			}
		}
		switch (ArgUtility.Get(array, 1))
		{
		case "eel":
			fishType = FishType.Eel;
			minimumVelocity = Utility.Clamp(fishScale, 0.3f, 0.4f);
			break;
		case "cephalopod":
			fishType = FishType.Cephalopod;
			minimumVelocity = 0f;
			break;
		case "ground":
			fishType = FishType.Ground;
			zPosition = 4f;
			minimumVelocity = 0f;
			break;
		case "static":
			fishType = FishType.Static;
			break;
		case "crawl":
			fishType = FishType.Crawl;
			minimumVelocity = 0f;
			break;
		case "front_crawl":
			fishType = FishType.Crawl;
			zPosition = 3f;
			minimumVelocity = 0f;
			break;
		case "float":
			fishType = FishType.Float;
			break;
		}
		string text3 = ArgUtility.Get(array, 2, null, allowBlank: false);
		if (text3 != null)
		{
			string[] array4 = ArgUtility.SplitBySpace(text3);
			idleAnimation = new List<int>();
			string[] array5 = array4;
			foreach (string s in array5)
			{
				idleAnimation.Add(int.Parse(s));
			}
			SetAnimation(idleAnimation);
		}
		string text4 = ArgUtility.Get(array, 3, null, allowBlank: false);
		if (text4 != null)
		{
			string[] array6 = ArgUtility.SplitBySpace(text4);
			dartStartAnimation = new List<int>();
			string[] array5 = array6;
			foreach (string s2 in array5)
			{
				dartStartAnimation.Add(int.Parse(s2));
			}
		}
		string text5 = ArgUtility.Get(array, 4, null, allowBlank: false);
		if (text5 != null)
		{
			string[] array7 = ArgUtility.SplitBySpace(text5);
			dartHoldAnimation = new List<int>();
			string[] array5 = array7;
			foreach (string s3 in array5)
			{
				dartHoldAnimation.Add(int.Parse(s3));
			}
		}
		string text6 = ArgUtility.Get(array, 5, null, allowBlank: false);
		if (text6 != null)
		{
			string[] array8 = ArgUtility.SplitBySpace(text6);
			dartEndAnimation = new List<int>();
			string[] array5 = array8;
			foreach (string s4 in array5)
			{
				dartEndAnimation.Add(int.Parse(s4));
			}
		}
		Rectangle tankBounds = _tank.GetTankBounds();
		tankBounds.X = 0;
		tankBounds.Y = 0;
		position = Vector2.Zero;
		position = Utility.getRandomPositionInThisRectangle(tankBounds, Game1.random);
		nextSwim = Utility.RandomFloat(0.1f, 10f);
		nextBubble = Utility.RandomFloat(0.1f, 10f);
		facingLeft = Game1.random.Next(2) == 1;
		if (facingLeft)
		{
			velocity = new Vector2(-1f, 0f);
		}
		else
		{
			velocity = new Vector2(1f, 0f);
		}
		velocity *= minimumVelocity;
		if (item.QualifiedItemId == "(TR)FrogEgg")
		{
			fishType = FishType.Hop;
			_texture = Game1.content.Load<Texture2D>("TileSheets\\companions");
			frogVariant = ((item as Trinket).GetEffect() as CompanionTrinketEffect).Variant;
			isErrorFish = false;
		}
		if (fishType == FishType.Ground || fishType == FishType.Crawl || fishType == FishType.Hop || fishType == FishType.Static)
		{
			position.Y = 0f;
		}
		ConstrainToTank();
	}

	public void SetAnimation(List<int> frames)
	{
		if (fishType != FishType.Hop && currentAnimation != frames)
		{
			currentAnimation = frames;
			currentAnimationFrame = 0;
			currentFrameTime = 0f;
			List<int> list = currentAnimation;
			if (list != null && list.Count > 0)
			{
				currentFrame = frames[0];
			}
		}
	}

	public virtual void Draw(SpriteBatch b, float alpha, float draw_layer)
	{
		SpriteEffects effects = SpriteEffects.None;
		int num = -12;
		int num2 = 8;
		if (fishType == FishType.Eel)
		{
			num2 = 4;
		}
		int num3 = num2;
		if (facingLeft)
		{
			effects = SpriteEffects.FlipHorizontally;
			num3 *= -1;
			num = -num - num2;
		}
		float num4 = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 1.25 + (double)(position.X / 32f)) * 2f;
		if (fishType == FishType.Crawl || fishType == FishType.Ground || fishType == FishType.Static)
		{
			num4 = 0f;
		}
		float scale = GetScale();
		int num5 = _texture.Width / 24;
		int num6 = currentFrame % num5 * 24;
		int y = currentFrame / num5 * 48;
		int num7 = 10;
		float num8 = 1f;
		if (fishType == FishType.Eel)
		{
			num7 = 20;
			num4 *= 0f;
		}
		float y2 = -12f;
		float num9 = 0f;
		if (isErrorFish)
		{
			num9 = 0f;
			IItemDataDefinition itemDataDefinition = ItemRegistry.RequireTypeDefinition("(F)");
			b.Draw(itemDataDefinition.GetErrorTexture(), Game1.GlobalToLocal(GetWorldPosition() + new Vector2(0f, num4) * 4f * scale), itemDataDefinition.GetErrorSourceRect(), Color.White * alpha, num9, new Vector2(12f, 12f), 4f * scale, effects, draw_layer);
		}
		else
		{
			switch (fishType)
			{
			case FishType.Ground:
			case FishType.Crawl:
			case FishType.Static:
				num9 = 0f;
				b.Draw(_texture, Game1.GlobalToLocal(GetWorldPosition() + new Vector2(0f, num4) * 4f * scale), new Rectangle(num6, y, 24, 24), Color.White * alpha, num9, new Vector2(12f, 12f), 4f * scale, effects, draw_layer);
				break;
			case FishType.Hop:
			{
				int num14 = 0;
				if (position.Y > 0f)
				{
					num14 = ((!((double)velocity.Y > 0.2)) ? 3 : (((double)velocity.Y > 0.3) ? 1 : 2));
				}
				else if (nextSwim <= 3f)
				{
					num14 = ((Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 400.0 >= 200.0) ? 5 : 6);
				}
				Rectangle value2 = new Rectangle(num14 * 16, 16 + frogVariant * 16, 16, 16);
				Color color = Color.White;
				if (frogVariant == 7)
				{
					color = Utility.GetPrismaticColor();
				}
				b.Draw(_texture, Game1.GlobalToLocal(GetWorldPosition() + new Vector2(16f, -8f)), value2, color * alpha, num9, new Vector2(12f, 12f), 4f * scale, effects, draw_layer);
				break;
			}
			case FishType.Cephalopod:
			case FishType.Float:
				num9 = Utility.Clamp(velocity.X, -0.5f, 0.5f);
				b.Draw(_texture, Game1.GlobalToLocal(GetWorldPosition() + new Vector2(0f, num4) * 4f * scale), new Rectangle(num6, y, 24, 24), Color.White * alpha, num9, new Vector2(12f, 12f), 4f * scale, effects, draw_layer);
				break;
			default:
			{
				for (int i = 0; i < 24 / num2; i++)
				{
					float num10 = (float)(i * num2) / (float)num7;
					num10 = 1f - num10;
					float value = velocity.Length() / 1f;
					float num11 = 1f;
					float num12 = 0f;
					value = Utility.Clamp(value, 0.2f, 1f);
					num10 = Utility.Clamp(num10, 0f, 1f);
					if (fishType == FishType.Eel)
					{
						num10 = 1f;
						value = 1f;
						num11 = 0.1f;
						num12 = 4f;
					}
					if (facingLeft)
					{
						num12 *= -1f;
					}
					float num13 = (float)(Math.Sin((double)(i * 20) + Game1.currentGameTime.TotalGameTime.TotalSeconds * 25.0 * (double)num11 + (double)(num12 * position.X / 16f)) * (double)num8 * (double)num10 * (double)value);
					if (i == 24 / num2 - 1)
					{
						y2 = -12f + num13;
					}
					b.Draw(_texture, Game1.GlobalToLocal(GetWorldPosition() + new Vector2(num + i * num3, num4 + num13) * 4f * scale), new Rectangle(num6 + i * num2, y, num2, 24), Color.White * alpha, 0f, new Vector2(0f, 12f), 4f * scale, effects, draw_layer);
				}
				break;
			}
			}
		}
		float x = (facingLeft ? 12 : (-12));
		b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(new Vector2(GetWorldPosition().X, (float)_tank.GetTankBounds().Bottom - zPosition * 4f)), null, Color.White * alpha * 0.75f, 0f, new Vector2(Game1.shadowTexture.Width / 2, Game1.shadowTexture.Height / 2), new Vector2(4f * scale, 1f), SpriteEffects.None, _tank.GetFishSortRegion().X - 1E-07f);
		int num15 = 0;
		foreach (TankFish item in _tank.tankFish)
		{
			if (item == this)
			{
				break;
			}
			if (item.CanWearHat())
			{
				num15++;
			}
		}
		if (!CanWearHat())
		{
			return;
		}
		int num16 = 0;
		foreach (Item heldItem in _tank.heldItems)
		{
			if (!(heldItem is Hat hat))
			{
				continue;
			}
			if (num16 == num15)
			{
				Vector2 vector = new Vector2(hatPosition.Value.X, hatPosition.Value.Y);
				if (facingLeft)
				{
					vector.X *= -1f;
				}
				Vector2 vector2 = new Vector2(x, y2) + vector;
				if (num9 != 0f)
				{
					float num17 = (float)Math.Cos(num9);
					float num18 = (float)Math.Sin(num9);
					vector2.X = vector2.X * num17 - vector2.Y * num18;
					vector2.Y = vector2.X * num18 + vector2.Y * num17;
				}
				vector2 *= 4f * scale;
				Vector2 location = Game1.GlobalToLocal(GetWorldPosition() + vector2);
				location.Y += num4;
				int direction = ((fishType == FishType.Cephalopod || fishType == FishType.Static) ? 2 : ((!facingLeft) ? 1 : 3));
				location -= new Vector2(10f, 10f);
				location += new Vector2(3f, 3f) * scale * 3f;
				location -= new Vector2(10f, 10f) * scale * 3f;
				hat.draw(b, location, scale, 1f, draw_layer + 1E-08f, direction);
				num15++;
				break;
			}
			num16++;
		}
	}

	[MemberNotNullWhen(true, "hatPosition")]
	public bool CanWearHat()
	{
		return hatPosition.HasValue;
	}

	public Vector2 GetWorldPosition()
	{
		return new Vector2((float)_tank.GetTankBounds().X + position.X, (float)_tank.GetTankBounds().Bottom - position.Y - zPosition * 4f);
	}

	public void ConstrainToTank()
	{
		Rectangle tankBounds = _tank.GetTankBounds();
		Rectangle bounds = GetBounds();
		tankBounds.X = 0;
		tankBounds.Y = 0;
		if (bounds.X < tankBounds.X)
		{
			position.X += tankBounds.X - bounds.X;
			bounds = GetBounds();
		}
		if (bounds.Y < tankBounds.Y)
		{
			position.Y -= tankBounds.Y - bounds.Y;
			bounds = GetBounds();
		}
		if (bounds.Right > tankBounds.Right)
		{
			position.X += tankBounds.Right - bounds.Right;
			bounds = GetBounds();
		}
		if (fishType == FishType.Crawl || fishType == FishType.Ground || fishType == FishType.Static || fishType == FishType.Hop)
		{
			if (position.Y > (float)tankBounds.Bottom)
			{
				position.Y -= (float)tankBounds.Bottom - position.Y;
			}
		}
		else if (bounds.Bottom > tankBounds.Bottom)
		{
			position.Y -= tankBounds.Bottom - bounds.Bottom;
		}
	}

	public virtual float GetScale()
	{
		return fishScale;
	}

	public Rectangle GetBounds()
	{
		Vector2 vector = new Vector2(24f, 18f);
		vector *= 4f * GetScale();
		if (fishType == FishType.Crawl || fishType == FishType.Ground || fishType == FishType.Static || fishType == FishType.Hop)
		{
			return new Rectangle((int)(position.X - vector.X / 2f), (int)((float)_tank.GetTankBounds().Height - position.Y - vector.Y), (int)vector.X, (int)vector.Y);
		}
		return new Rectangle((int)(position.X - vector.X / 2f), (int)((float)_tank.GetTankBounds().Height - position.Y - vector.Y / 2f), (int)vector.X, (int)vector.Y);
	}

	public virtual void Update(GameTime time)
	{
		List<int> list = currentAnimation;
		if (list != null && list.Count > 0)
		{
			currentFrameTime += (float)time.ElapsedGameTime.TotalSeconds;
			float num = 0.125f;
			if (currentFrameTime > num)
			{
				currentAnimationFrame += (int)(currentFrameTime / num);
				currentFrameTime %= num;
				if (currentAnimationFrame >= currentAnimation.Count)
				{
					if (currentAnimation == idleAnimation)
					{
						currentAnimationFrame %= currentAnimation.Count;
						currentFrame = currentAnimation[currentAnimationFrame];
					}
					else if (currentAnimation == dartStartAnimation)
					{
						if (dartHoldAnimation != null)
						{
							SetAnimation(dartHoldAnimation);
						}
						else
						{
							SetAnimation(idleAnimation);
						}
					}
					else if (currentAnimation == dartHoldAnimation)
					{
						currentAnimationFrame %= currentAnimation.Count;
						currentFrame = currentAnimation[currentAnimationFrame];
					}
					else if (currentAnimation == dartEndAnimation)
					{
						SetAnimation(idleAnimation);
					}
				}
				else
				{
					currentFrame = currentAnimation[currentAnimationFrame];
				}
			}
		}
		if (fishType != FishType.Static)
		{
			Rectangle tankBounds = _tank.GetTankBounds();
			tankBounds.X = 0;
			tankBounds.Y = 0;
			float num2 = velocity.X;
			if (fishType == FishType.Crawl)
			{
				num2 = Utility.Clamp(num2, -0.5f, 0.5f);
			}
			position.X += num2;
			Rectangle bounds = GetBounds();
			if (bounds.Left < tankBounds.Left || bounds.Right > tankBounds.Right)
			{
				ConstrainToTank();
				bounds = GetBounds();
				velocity.X *= -1f;
				facingLeft = !facingLeft;
			}
			position.Y += velocity.Y;
			bounds = GetBounds();
			if (bounds.Top < tankBounds.Top || bounds.Bottom > tankBounds.Bottom)
			{
				ConstrainToTank();
				velocity.Y *= 0f;
			}
			float num3 = velocity.Length();
			if (num3 > minimumVelocity)
			{
				float t = 0.015f;
				if (fishType == FishType.Crawl || fishType == FishType.Ground || fishType == FishType.Hop)
				{
					t = 0.03f;
				}
				num3 = Utility.Lerp(num3, minimumVelocity, t);
				if (num3 < 0.0001f)
				{
					num3 = 0f;
				}
				velocity.Normalize();
				velocity *= num3;
				if (currentAnimation == dartHoldAnimation && num3 <= minimumVelocity + 0.5f)
				{
					List<int> list2 = dartEndAnimation;
					if (list2 != null && list2.Count > 0)
					{
						SetAnimation(dartEndAnimation);
					}
					else
					{
						List<int> list3 = idleAnimation;
						if (list3 != null && list3.Count > 0)
						{
							SetAnimation(idleAnimation);
						}
					}
				}
			}
			nextSwim -= (float)time.ElapsedGameTime.TotalSeconds;
			if (nextSwim <= 0f)
			{
				if (numberOfDarts == 0)
				{
					numberOfDarts = Game1.random.Next(1, 4);
					nextSwim = Utility.RandomFloat(6f, 12f);
					switch (fishType)
					{
					case FishType.Cephalopod:
						nextSwim = Utility.RandomFloat(2f, 5f);
						break;
					case FishType.Hop:
						numberOfDarts = 0;
						break;
					}
					if (Game1.random.NextDouble() < 0.30000001192092896)
					{
						facingLeft = !facingLeft;
					}
				}
				else
				{
					nextSwim = Utility.RandomFloat(0.1f, 0.5f);
					numberOfDarts--;
					if (Game1.random.NextDouble() < 0.05000000074505806)
					{
						facingLeft = !facingLeft;
					}
				}
				List<int> list4 = dartStartAnimation;
				if (list4 != null && list4.Count > 0)
				{
					SetAnimation(dartStartAnimation);
				}
				else
				{
					List<int> list5 = dartHoldAnimation;
					if (list5 != null && list5.Count > 0)
					{
						SetAnimation(dartHoldAnimation);
					}
				}
				velocity.X = 1.5f;
				if (_tank.getTilesWide() <= 2)
				{
					velocity.X *= 0.5f;
				}
				if (facingLeft)
				{
					velocity.X *= -1f;
				}
				switch (fishType)
				{
				case FishType.Cephalopod:
					velocity.Y = Utility.RandomFloat(0.5f, 0.75f);
					break;
				case FishType.Ground:
					velocity.X *= 0.5f;
					velocity.Y = Utility.RandomFloat(0.5f, 0.25f);
					break;
				case FishType.Hop:
					velocity.Y = Utility.RandomFloat(0.35f, 0.65f);
					break;
				default:
					velocity.Y = Utility.RandomFloat(-0.5f, 0.5f);
					break;
				}
				if (fishType == FishType.Crawl)
				{
					velocity.Y = 0f;
				}
			}
		}
		if (fishType == FishType.Cephalopod || fishType == FishType.Ground || fishType == FishType.Crawl || fishType == FishType.Static || fishType == FishType.Hop)
		{
			float num4 = 0.2f;
			if (fishType == FishType.Static)
			{
				num4 = 0.6f;
			}
			if (position.Y > 0f)
			{
				position.Y -= num4;
			}
		}
		nextBubble -= (float)time.ElapsedGameTime.TotalSeconds;
		if (nextBubble <= 0f)
		{
			nextBubble = Utility.RandomFloat(1f, 10f);
			float num5 = 0f;
			if (fishType == FishType.Ground || fishType == FishType.Normal || fishType == FishType.Eel)
			{
				num5 = 32f;
			}
			if (facingLeft)
			{
				num5 *= -1f;
			}
			num5 *= fishScale;
			_tank.bubbles.Add(new Vector4(position.X + num5, position.Y + zPosition, zPosition, 0.25f));
		}
		ConstrainToTank();
	}
}
