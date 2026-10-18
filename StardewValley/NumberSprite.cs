using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley;

public class NumberSprite
{
	public const int textureX = 512;

	public const int textureY = 128;

	public const int digitWidth = 8;

	public const int digitHeight = 8;

	public const int groupWidth = 48;

	public static void draw(int number, SpriteBatch b, Vector2 position, Color c, float scale, float layerDepth, float alpha, int secondDigitOffset, int spaceBetweenDigits = 0)
	{
		int num = 1;
		secondDigitOffset = Math.Min(0, secondDigitOffset);
		do
		{
			int num2 = number % 10;
			number /= 10;
			int x = 512 + num2 * 8 % 48;
			int y = 128 + num2 * 8 / 48 * 8;
			b.Draw(Game1.mouseCursors, position + new Vector2(0f, (num == 2) ? secondDigitOffset : 0), new Rectangle(x, y, 8, 8), c * alpha, 0f, new Vector2(4f, 4f), 4f * scale, SpriteEffects.None, layerDepth);
			position.X -= 8f * scale * 4f - 4f - (float)spaceBetweenDigits;
			num++;
		}
		while (number > 0);
	}

	public static int getHeight()
	{
		return 8;
	}

	public static int getWidth(string number)
	{
		return getWidth(Convert.ToInt32(number));
	}

	public static int getWidth(int number)
	{
		int num = 8;
		number /= 10;
		while (number != 0)
		{
			number /= 10;
			num += 8;
		}
		return num;
	}

	public static int numberOfDigits(int number)
	{
		int num = 1;
		number /= 10;
		while (number != 0)
		{
			number /= 10;
			num++;
		}
		return num;
	}
}
