using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Extensions;
using StardewValley.Locations;
using xTile.Dimensions;
using xTile.Layers;

namespace StardewValley;

public class Background
{
	public int defaultChunkIndex;

	public int numChunksInSheet;

	public double chanceForDeviationFromDefault;

	protected Texture2D backgroundImage;

	protected Texture2D cloudsTexture;

	protected Vector2 position = Vector2.Zero;

	protected int chunksWide;

	protected int chunksHigh;

	protected int chunkWidth;

	protected int chunkHeight;

	protected int[] chunks;

	protected float zoom;

	public Color c;

	protected bool summitBG;

	protected bool onlyMapBG;

	public int yOffset;

	public TemporaryAnimatedSpriteList tempSprites;

	protected int initialViewportY;

	public bool cursed;

	/// <summary>The location for which to render a background.</summary>
	protected GameLocation location;

	/// <summary>
	/// constructor for summit background
	/// </summary>
	public Background(Summit location)
	{
		this.location = location;
		summitBG = true;
		c = Color.White;
		initialViewportY = Game1.viewport.Y;
		cloudsTexture = Game1.content.Load<Texture2D>("Minigames\\Clouds");
	}

	public Background(GameLocation location, Color color, bool onlyMapBG)
	{
		this.location = location;
		c = color;
		this.onlyMapBG = onlyMapBG;
		tempSprites = new TemporaryAnimatedSpriteList();
	}

	public Background(GameLocation location, Texture2D bgImage, int seedValue, int chunksWide, int chunksHigh, int chunkWidth, int chunkHeight, float zoom, int defaultChunkIndex, int numChunksInSheet, double chanceForDeviation, Color c)
	{
		this.location = location;
		backgroundImage = bgImage;
		this.chunksWide = chunksWide;
		this.chunksHigh = chunksHigh;
		this.zoom = zoom;
		this.chunkWidth = chunkWidth;
		this.chunkHeight = chunkHeight;
		this.defaultChunkIndex = defaultChunkIndex;
		this.numChunksInSheet = numChunksInSheet;
		chanceForDeviationFromDefault = chanceForDeviation;
		this.c = c;
		Random random = Utility.CreateRandom(seedValue);
		chunks = new int[chunksWide * chunksHigh];
		for (int i = 0; i < chunksHigh * chunksWide; i++)
		{
			if (random.NextDouble() < chanceForDeviationFromDefault)
			{
				chunks[i] = random.Next(numChunksInSheet);
			}
			else
			{
				chunks[i] = defaultChunkIndex;
			}
		}
	}

	public virtual void update(xTile.Dimensions.Rectangle viewport)
	{
		Layer layer = Game1.currentLocation.map.RequireLayer("Back");
		position.X = 0f - (float)(viewport.X + viewport.Width / 2) / ((float)layer.LayerWidth * 64f) * ((float)(chunksWide * chunkWidth) * zoom - (float)viewport.Width);
		position.Y = 0f - (float)(viewport.Y + viewport.Height / 2) / ((float)layer.LayerHeight * 64f) * ((float)(chunksHigh * chunkHeight) * zoom - (float)viewport.Height);
	}

	public virtual void draw(SpriteBatch b)
	{
		if (summitBG)
		{
			if (Game1.viewport.X <= -1000)
			{
				return;
			}
			Season seasonForLocation = Game1.GetSeasonForLocation(location);
			bool flag = seasonForLocation == Season.Winter;
			int num = seasonForLocation switch
			{
				Season.Fall => 1, 
				Season.Winter => 2, 
				_ => 0, 
			};
			int num2 = -Game1.viewport.Y / 4 + initialViewportY / 4;
			float num3 = 1f;
			float num4 = 1f;
			Color color = Color.White;
			int num5 = (int)((float)(Game1.timeOfDay - Game1.timeOfDay % 100) + (float)(Game1.timeOfDay % 100 / 10) * 16.66f);
			int num6 = (flag ? 30 : 0);
			if (Game1.timeOfDay >= 1800)
			{
				c = new Color(255f, 255f - Math.Max(100f, (float)num5 + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f - 1800f), 255f - Math.Max(100f, ((float)num5 + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f - 1800f) / 2f));
				color = (flag ? (Color.Black * 0.5f) : (Color.Blue * 0.5f));
				num3 = Math.Max(0f, Math.Min(1f, (2000f - ((float)num5 + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f)) / 200f));
				num4 = Math.Max(0f, Math.Min(1f, (2200f - ((float)num5 + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f)) / 400f));
				Game1.ambientLight = new Color((int)Utility.Lerp(0f, 30f, 1f - num3), (int)Utility.Lerp(0f, 60f, 1f - num3), (int)Utility.Lerp(0f, 15f, 1f - num3));
			}
			b.Draw(Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), new Microsoft.Xna.Framework.Rectangle(639, 858, 1, 144), c * num4, 0f, Vector2.Zero, SpriteEffects.None, 5E-08f);
			b.Draw(Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), (seasonForLocation == Season.Fall) ? new Microsoft.Xna.Framework.Rectangle(639, 1051, 1, 400) : new Microsoft.Xna.Framework.Rectangle(639 + (num + 1), 1051, 1, 400), c * num3, 0f, Vector2.Zero, SpriteEffects.None, 1E-07f);
			if (Game1.timeOfDay >= 1800)
			{
				b.Draw(Game1.mouseCursors, new Vector2(0f, Game1.viewport.Height / 2 - 780), new Microsoft.Xna.Framework.Rectangle(0, 1453, 638, 195), Color.White * (1f - num3), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-05f);
			}
			if (Game1.dayOfMonth == 28 && Game1.timeOfDay > 1900)
			{
				b.Draw(Game1.mouseCursors, new Vector2(((float)num5 + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f) / 2600f * (float)Game1.viewport.Width / 4f, (float)(Game1.viewport.Height / 2 + 176) - ((float)(num5 - 1900) + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f) / 700f * (float)Game1.viewport.Height / 2f), new Microsoft.Xna.Framework.Rectangle(642, 834, 43, 44), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 5E-08f);
			}
			if (!flag && (Game1.currentLocation.IsDebrisWeatherHere() || Game1.currentLocation.IsRainingHere()))
			{
				b.Draw(cloudsTexture, new Vector2((float)Game1.viewport.Width - ((float)num5 + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f) / 2600f * (float)(Game1.viewport.Width + 2048), Game1.viewport.Height - 584 - 600 + num2 / 2 + Game1.dayOfMonth * 6), new Microsoft.Xna.Framework.Rectangle(0, 0, 512, 340), Color.White * num3, 0f, Vector2.Zero, 4f, SpriteEffects.None, 5.6E-08f);
			}
			if (!cursed)
			{
				b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle(0, Game1.viewport.Height - 584 + num2 / 2, Game1.viewport.Width, Game1.viewport.Height / 2), new Microsoft.Xna.Framework.Rectangle(0, 0, 1, 1), new Color((int)((float)num6 + 60f * num4), (int)((float)(num6 + 10) + 170f * num4), (int)((float)(num6 + 20) + 205f * num4)), 0f, Vector2.Zero, SpriteEffects.None, 2E-07f);
				b.Draw(Game1.mouseCursors, new Vector2(2556f, Game1.viewport.Height - 596 + num2), new Microsoft.Xna.Framework.Rectangle(0, 736 + num * 149, 639, 149), Color.White * Math.Max((int)c.A, 0.5f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-06f);
				b.Draw(Game1.mouseCursors, new Vector2(2556f, Game1.viewport.Height - 596 + num2), new Microsoft.Xna.Framework.Rectangle(0, 736 + num * 149, 639, 149), color * (1f - num3), 0f, Vector2.Zero, 4f, SpriteEffects.None, 2E-06f);
				b.Draw(Game1.mouseCursors, new Vector2(0f, Game1.viewport.Height - 596 + num2), new Microsoft.Xna.Framework.Rectangle(0, 736 + num * 149, 639, 149), Color.White * Math.Max((int)c.A, 0.5f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-06f);
				b.Draw(Game1.mouseCursors, new Vector2(0f, Game1.viewport.Height - 596 + num2), new Microsoft.Xna.Framework.Rectangle(0, 736 + num * 149, 639, 149), color * (1f - num3), 0f, Vector2.Zero, 4f, SpriteEffects.None, 2E-06f);
				foreach (TemporaryAnimatedSprite temporarySprite in Game1.currentLocation.temporarySprites)
				{
					temporarySprite.draw(b);
				}
				b.Draw(cloudsTexture, new Vector2(0f, (float)(Game1.viewport.Height - 568) + (float)num2 * 2f), new Microsoft.Xna.Framework.Rectangle(0, 554 + num * 153, 164, 142), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.99f);
				b.Draw(cloudsTexture, new Vector2(Game1.viewport.Width - 488, (float)(Game1.viewport.Height - 612) + (float)num2 * 2f), new Microsoft.Xna.Framework.Rectangle(390, 543 + num * 153, 122, 153), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.99f);
				b.Draw(cloudsTexture, new Vector2(0f, (float)(Game1.viewport.Height - 568) + (float)num2 * 2f), new Microsoft.Xna.Framework.Rectangle(0, 554 + num * 153, 164, 142), Color.Black * (1f - num3), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				b.Draw(cloudsTexture, new Vector2(Game1.viewport.Width - 488, (float)(Game1.viewport.Height - 612) + (float)num2 * 2f), new Microsoft.Xna.Framework.Rectangle(390, 543 + num * 153, 122, 153), Color.Black * (1f - num3), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			}
			else if (Game1.options.screenFlash)
			{
				Random random = new Random((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds - Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000.0));
				for (int i = 0; i < 20; i++)
				{
					Texture2D texture2D = random.Choose<Texture2D>(Game1.mouseCursors, Game1.mouseCursors2, Game1.objectSpriteSheet, Game1.menuTexture, Game1.uncoloredMenuTexture, Game1.mouseCursors_1_6, Game1.bigCraftableSpriteSheet, Game1.cropSpriteSheet);
					b.Draw(texture2D, new Vector2(random.Next(Game1.viewport.Width) - 100, random.Next(Game1.viewport.Height) - 100) + new Vector2((float)(int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000.0) * 0.03f), new Microsoft.Xna.Framework.Rectangle(random.Next(texture2D.Width / 16) * 16, random.Next(texture2D.Height / 16) * 16, 16, 16), Utility.getRandomRainbowColor(random), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-06f);
				}
			}
			return;
		}
		if (backgroundImage == null)
		{
			Microsoft.Xna.Framework.Rectangle destinationRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height);
			if (onlyMapBG)
			{
				destinationRectangle.X = Math.Max(0, -Game1.viewport.X);
				destinationRectangle.Y = Math.Max(0, -Game1.viewport.Y);
				destinationRectangle.Width = Math.Min(Game1.viewport.Width, Game1.currentLocation.map.DisplayWidth);
				destinationRectangle.Height = Math.Min(Game1.viewport.Height, Game1.currentLocation.map.DisplayHeight);
			}
			b.Draw(Game1.staminaRect, destinationRectangle, Game1.staminaRect.Bounds, c, 0f, Vector2.Zero, SpriteEffects.None, 0f);
			for (int num7 = tempSprites.Count - 1; num7 >= 0; num7--)
			{
				if (tempSprites[num7].update(Game1.currentGameTime))
				{
					tempSprites.RemoveAt(num7);
				}
				else
				{
					tempSprites[num7].draw(b);
				}
			}
			return;
		}
		Vector2 zero = Vector2.Zero;
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(0, 0, chunkWidth, chunkHeight);
		for (int j = 0; j < chunks.Length; j++)
		{
			zero.X = position.X + (float)(j * chunkWidth % (chunksWide * chunkWidth)) * zoom;
			zero.Y = position.Y + (float)(j * chunkWidth / (chunksWide * chunkWidth) * chunkHeight) * zoom;
			if (backgroundImage == null)
			{
				b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((int)zero.X, (int)zero.Y, Game1.viewport.Width, Game1.viewport.Height), value, c, 0f, Vector2.Zero, SpriteEffects.None, 0f);
				continue;
			}
			value.X = chunks[j] * chunkWidth % backgroundImage.Width;
			value.Y = chunks[j] * chunkWidth / backgroundImage.Width * chunkHeight;
			b.Draw(backgroundImage, zero, value, c, 0f, Vector2.Zero, zoom, SpriteEffects.None, 0f);
		}
	}
}
