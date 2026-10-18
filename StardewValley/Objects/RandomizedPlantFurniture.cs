using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;

namespace StardewValley.Objects;

public class RandomizedPlantFurniture : Furniture
{
	public NetInt topIndex = new NetInt();

	public NetInt middleIndex = new NetInt();

	public NetInt bottomIndex = new NetInt();

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(topIndex, "topIndex").AddField(middleIndex, "middleIndex").AddField(bottomIndex, "bottomIndex");
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new RandomizedPlantFurniture(base.ItemId, tileLocation.Value);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is RandomizedPlantFurniture randomizedPlantFurniture)
		{
			topIndex.Value = randomizedPlantFurniture.topIndex.Value;
			middleIndex.Value = randomizedPlantFurniture.middleIndex.Value;
			bottomIndex.Value = randomizedPlantFurniture.bottomIndex.Value;
		}
	}

	public RandomizedPlantFurniture(string which, Vector2 tile)
		: this(which, tile, Game1.random.Next())
	{
	}

	public RandomizedPlantFurniture(string which, Vector2 tile, int random_seed)
		: base(which, tile)
	{
		Random random = Utility.CreateRandom(random_seed);
		topIndex.Value = random.Next(24);
		middleIndex.Value = random.Next(24);
		bottomIndex.Value = random.Next(16);
	}

	public RandomizedPlantFurniture()
	{
	}

	protected override float getScaleSize()
	{
		return 1.5f;
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		location += new Vector2(32f, 32f);
		DrawFurniture(spriteBatch, location, transparency, new Vector2(8f, 0f), getScaleSize() * scaleSize, layerDepth);
		if (((drawStackNumber == StackDrawType.Draw && maximumStackSize() > 1 && Stack > 1) || drawStackNumber == StackDrawType.Draw_OneInclusive) && (double)scaleSize > 0.3 && Stack != int.MaxValue)
		{
			Utility.drawTinyDigits(stack.Value, spriteBatch, location + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(stack.Value, 3f * scaleSize)) + 3f * scaleSize, 64f - 18f * scaleSize + 2f), 3f * scaleSize, 1f, color);
		}
	}

	public override bool IsHeldOverHead()
	{
		return true;
	}

	public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
	{
		DrawFurniture(spriteBatch, objectPosition, 4f, Vector2.Zero, 4f, (float)(f.StandingPixel.Y + 3) / 10000f);
	}

	public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		if (!isTemporarilyInvisible)
		{
			if (Furniture.isDrawingLocationFurniture)
			{
				x = (int)drawPosition.X;
				y = (int)drawPosition.Y;
			}
			else
			{
				x *= 64;
				y *= 64;
			}
			if (shakeTimer > 0)
			{
				x += Game1.random.Next(-1, 2);
				y += Game1.random.Next(-1, 2);
			}
			DrawFurniture(spriteBatch, Game1.GlobalToLocal(new Vector2(x, y)), alpha, Vector2.Zero, 4f, (float)(boundingBox.Value.Bottom - 8) / 10000f);
		}
	}

	public override void drawAtNonTileSpot(SpriteBatch spriteBatch, Vector2 location, float layerDepth, float alpha = 1f)
	{
		DrawFurniture(spriteBatch, location, 1f, Vector2.Zero, 4f, layerDepth);
	}

	public virtual void DrawFurniture(SpriteBatch sb, Vector2 location, float alpha, Vector2 origin, float scale, float base_sort_y)
	{
		Texture2D texture = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).GetTexture();
		Rectangle value = new Rectangle(0, 96, 16, 16);
		value.X += bottomIndex.Value % 8 * 16;
		value.Y += bottomIndex.Value / 8 * 16;
		sb.Draw(texture, location, value, Color.White * alpha, 0f, origin, scale, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, base_sort_y);
		float x = -1f * scale;
		value = new Rectangle(0, 48, 16, 16);
		value.X += middleIndex.Value % 8 * 16;
		value.Y += middleIndex.Value / 8 * 16;
		sb.Draw(texture, location + new Vector2(x, -8f * scale), value, Color.White * alpha, 0f, origin, scale, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, base_sort_y + 1E-05f);
		value = new Rectangle(0, 0, 16, 16);
		value.X += topIndex.Value % 8 * 16;
		value.Y += topIndex.Value / 8 * 16;
		sb.Draw(texture, location + new Vector2(x, -24f * scale), value, Color.White * alpha, 0f, origin, scale, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, base_sort_y + 1E-05f);
	}
}
