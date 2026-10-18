using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Network;

namespace StardewValley.BellsAndWhistles;

public class Bird
{
	public enum BirdState
	{
		Idle,
		Flying
	}

	public Vector2 position;

	public Point startPosition;

	public Point endPosition;

	public float pathPosition;

	public float velocity;

	public int framesUntilNextMove;

	public BirdState birdState;

	public PerchingBirds context;

	public int peckFrames;

	public int nextPeck;

	public int peckDirection;

	public int birdType;

	public int flapFrames = 2;

	public float flyArcHeight;

	public Bird()
	{
		position = new Vector2(0f, 0f);
		startPosition = new Point(0, 0);
		endPosition = new Point(0, 0);
		birdType = Game1.random.Next(0, 4);
	}

	public Bird(Point point, PerchingBirds context, int bird_type = 0, int flap_frames = 2)
	{
		startPosition.X = (endPosition.X = point.X);
		startPosition.Y = (endPosition.Y = point.Y);
		position.X = ((float)startPosition.X + 0.5f) * 64f;
		position.Y = ((float)startPosition.Y + 0.5f) * 64f;
		this.context = context;
		birdType = bird_type;
		framesUntilNextMove = Game1.random.Next(100, 300);
		peckDirection = Game1.random.Next(0, 2);
		flapFrames = flap_frames;
	}

	public virtual void Draw(SpriteBatch b)
	{
		Vector2 vector = new Vector2(position.X, position.Y);
		vector.X += (float)Math.Sin((float)Game1.currentGameTime.TotalGameTime.Milliseconds * 0.0025f) * velocity * 2f;
		vector.Y += (float)Math.Sin((float)Game1.currentGameTime.TotalGameTime.Milliseconds * 0.006f) * velocity * 2f;
		vector.Y += (float)Math.Sin((double)pathPosition * Math.PI) * (0f - flyArcHeight);
		SpriteEffects effects = SpriteEffects.None;
		int num;
		if (birdState == BirdState.Idle)
		{
			if (peckDirection == 1)
			{
				effects = SpriteEffects.FlipHorizontally;
			}
			num = ((!context.ShouldBirdsRoost()) ? ((peckFrames > 0) ? 1 : 0) : ((peckFrames <= 0) ? 8 : 9));
		}
		else
		{
			Vector2 vector2 = new Vector2(endPosition.X - startPosition.X, endPosition.Y - startPosition.Y);
			vector2.Normalize();
			if (Math.Abs(vector2.X) > Math.Abs(vector2.Y))
			{
				num = 2;
				if (vector2.X > 0f)
				{
					effects = SpriteEffects.FlipHorizontally;
				}
			}
			else if (vector2.Y > 0f)
			{
				num = 2 + flapFrames;
				if (vector2.X > 0f)
				{
					effects = SpriteEffects.FlipHorizontally;
				}
			}
			else
			{
				num = 2 + flapFrames * 2;
				if (vector2.X < 0f)
				{
					effects = SpriteEffects.FlipHorizontally;
				}
			}
			if (pathPosition > 0.95f)
			{
				num += Game1.currentGameTime.TotalGameTime.Milliseconds / 50 % flapFrames;
			}
			else if (!(pathPosition > 0.75f))
			{
				num += Game1.currentGameTime.TotalGameTime.Milliseconds / 100 % flapFrames;
			}
		}
		Rectangle value = new Rectangle(context.GetBirdWidth() * num, context.GetBirdHeight() * birdType, context.GetBirdWidth(), context.GetBirdHeight());
		Rectangle destinationRectangle = Game1.GlobalToLocal(Game1.viewport, new Rectangle((int)vector.X, (int)vector.Y, context.GetBirdWidth() * 4, context.GetBirdHeight() * 4));
		b.Draw(context.GetTexture(), destinationRectangle, value, Color.White, 0f, context.GetBirdOrigin(), effects, position.Y / 10000f);
	}

	public virtual void FlyToNewPoint()
	{
		Point freeBirdPoint = context.GetFreeBirdPoint(this, 500);
		if (freeBirdPoint != default(Point))
		{
			context.ReserveBirdPoint(this, freeBirdPoint);
			startPosition = endPosition;
			endPosition = freeBirdPoint;
			pathPosition = 0f;
			velocity = 0f;
			if (context.ShouldBirdsRoost())
			{
				birdState = BirdState.Idle;
			}
			else
			{
				birdState = BirdState.Flying;
			}
			float num = Utility.distance(startPosition.X, endPosition.X, startPosition.Y, endPosition.Y);
			if (num >= 7f)
			{
				flyArcHeight = 200f;
			}
			else if (num >= 5f)
			{
				flyArcHeight = 150f;
			}
			else
			{
				flyArcHeight = 20f;
			}
		}
		else
		{
			framesUntilNextMove = Game1.random.Next(800, 1200);
		}
	}

	public virtual void Update(GameTime time)
	{
		if (peckFrames > 0)
		{
			peckFrames--;
		}
		else
		{
			nextPeck--;
			if (nextPeck <= 0)
			{
				if (context.ShouldBirdsRoost())
				{
					peckFrames = 50;
				}
				else
				{
					peckFrames = context.peckDuration;
				}
				nextPeck = Game1.random.Next(10, 30);
				if (Game1.random.NextDouble() <= 0.75)
				{
					nextPeck += Game1.random.Next(50, 100);
					if (!context.ShouldBirdsRoost())
					{
						peckDirection = Game1.random.Next(0, 2);
					}
				}
			}
		}
		switch (birdState)
		{
		case BirdState.Idle:
		{
			if (context.ShouldBirdsRoost())
			{
				break;
			}
			using FarmerCollection.Enumerator enumerator = Game1.currentLocation.farmers.GetEnumerator();
			if (enumerator.MoveNext())
			{
				Farmer current = enumerator.Current;
				float num5 = Utility.distance(current.position.X, position.X, current.position.Y, position.Y);
				framesUntilNextMove--;
				if (num5 < 200f || framesUntilNextMove <= 0)
				{
					FlyToNewPoint();
				}
			}
			break;
		}
		case BirdState.Flying:
		{
			float num = Utility.distance((float)(endPosition.X * 64) + 32f, position.X, (float)(endPosition.Y * 64) + 32f, position.Y);
			float birdSpeed = context.birdSpeed;
			float num2 = 0.25f;
			if (num > birdSpeed / num2)
			{
				velocity = Utility.MoveTowards(velocity, birdSpeed, 0.5f);
			}
			else
			{
				velocity = Math.Max(Math.Min(num * num2, velocity), 1f);
			}
			float num3 = Utility.distance((float)endPosition.X + 32f, (float)startPosition.X + 32f, (float)endPosition.Y + 32f, (float)startPosition.Y + 32f) * 64f;
			if (num3 <= 0.0001f)
			{
				num3 = 0.0001f;
			}
			float num4 = velocity / num3;
			pathPosition += num4;
			position = new Vector2(Utility.Lerp((float)(startPosition.X * 64) + 32f, (float)(endPosition.X * 64) + 32f, pathPosition), Utility.Lerp((float)(startPosition.Y * 64) + 32f, (float)(endPosition.Y * 64) + 32f, pathPosition));
			if (pathPosition >= 1f)
			{
				position = new Vector2((float)(endPosition.X * 64) + 32f, (float)(endPosition.Y * 64) + 32f);
				birdState = BirdState.Idle;
				velocity = 0f;
				framesUntilNextMove = Game1.random.Next(350, 500);
				if (Game1.random.NextDouble() < 0.75)
				{
					framesUntilNextMove += Game1.random.Next(200, 300);
				}
			}
			break;
		}
		}
	}
}
