using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.BellsAndWhistles;

public class OverheadParrot : Critter
{
	protected Texture2D _texture;

	public Vector2 velocity;

	public float age;

	public float flyOffset;

	public float height = 64f;

	public Rectangle sourceRect;

	public Vector2 drawOffset;

	public int[] spriteFlapFrames = new int[8] { 0, 0, 0, 0, 1, 2, 2, 1 };

	public int currentFlapIndex;

	public int flapFrameAccumulator;

	public Vector2 swayAmount;

	public Vector2 lastDrawPosition;

	protected bool _shouldDrawShadow;

	public OverheadParrot(Vector2 start_position)
	{
		position = start_position;
		velocity = new Vector2(Utility.RandomFloat(-4f, -2f), Utility.RandomFloat(5f, 6f));
		_texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\parrots");
		sourceRect = new Rectangle(0, 0, 24, 24);
		sourceRect.Y = 24 * Game1.random.Next(4);
		currentFlapIndex = Game1.random.Next(spriteFlapFrames.Length);
		flyOffset = (float)(Game1.random.NextDouble() * 100.0);
		swayAmount.X = Utility.RandomFloat(16f, 32f);
		swayAmount.Y = Utility.RandomFloat(10f, 24f);
	}

	public override bool update(GameTime time, GameLocation environment)
	{
		flapFrameAccumulator++;
		if (flapFrameAccumulator >= 2)
		{
			currentFlapIndex++;
			if (currentFlapIndex >= spriteFlapFrames.Length)
			{
				currentFlapIndex = 0;
			}
			flapFrameAccumulator = 0;
		}
		age += (float)time.ElapsedGameTime.TotalSeconds;
		position += velocity;
		float num = (age + flyOffset) * 1f;
		float num2 = (age + flyOffset) * 2f;
		drawOffset.X = (float)Math.Sin(num) * swayAmount.X;
		drawOffset.Y = (float)Math.Cos(num2) * swayAmount.Y;
		Vector2 drawPosition = GetDrawPosition();
		if (currentFlapIndex == 4 && flapFrameAccumulator == 0 && Utility.isOnScreen(drawPosition, 64))
		{
			Game1.playSound("parrot_flap");
		}
		Vector2 vector = drawPosition - lastDrawPosition;
		lastDrawPosition = drawPosition;
		int num3 = 2;
		if (Math.Abs(vector.X) < Math.Abs(vector.Y))
		{
			num3 = 5;
		}
		sourceRect.X = (spriteFlapFrames[currentFlapIndex] + num3) * 24;
		_shouldDrawShadow = true;
		Vector2 shadowPosition = GetShadowPosition();
		if (!Game1.currentLocation.hasTileAt((int)shadowPosition.X / 64, (int)shadowPosition.Y / 64, "Back"))
		{
			_shouldDrawShadow = false;
		}
		if (position.X < -64f - swayAmount.X * 4f || position.Y > (float)(environment.map.Layers[0].DisplayHeight + 64) + (height + swayAmount.Y) * 4f)
		{
			return true;
		}
		return false;
	}

	public Vector2 GetDrawPosition()
	{
		return position + new Vector2(drawOffset.X, 0f - height + drawOffset.Y) * 4f;
	}

	public Vector2 GetShadowPosition()
	{
		return position + new Vector2(drawOffset.X * 4f, -4f);
	}

	public override void draw(SpriteBatch b)
	{
		if (_shouldDrawShadow)
		{
			b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, GetShadowPosition()), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f, SpriteEffects.None, (position.Y - 1f) / 10000f);
		}
	}

	public override void drawAboveFrontLayer(SpriteBatch b)
	{
		b.Draw(_texture, Game1.GlobalToLocal(Game1.viewport, GetDrawPosition()), sourceRect, Color.White, 0f, new Vector2(12f, 20f), 4f, SpriteEffects.None, position.Y / 10000f);
	}
}
