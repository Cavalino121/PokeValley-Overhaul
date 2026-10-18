using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace StardewValley;

public class Torch : Object
{
	public const float yVelocity = 1f;

	public const float yDissapearLevel = -100f;

	public const double ashChance = 0.015;

	private float color;

	private Vector2[] ashes = new Vector2[3];

	private float smokePuffTimer;

	public Torch()
		: this(1)
	{
	}

	public Torch(int initialStack)
		: base("93", initialStack)
	{
	}

	public Torch(int initialStack, string itemId)
		: base(itemId, initialStack)
	{
	}

	public Torch(string index, bool bigCraftable)
		: base(Vector2.Zero, index)
	{
	}

	/// <inheritdoc />
	public override void RecalculateBoundingBox()
	{
		boundingBox.Value = new Rectangle((int)tileLocation.X * 64, (int)tileLocation.Y * 64, 64, 64);
	}

	/// <inheritdoc />
	protected override void MigrateLegacyItemId()
	{
		base.ItemId = parentSheetIndex.Value.ToString();
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		if (!bigCraftable.Value)
		{
			return new Torch(1, base.ItemId);
		}
		return new Torch(base.ItemId, bigCraftable: true);
	}

	/// <inheritdoc />
	public override void actionOnPlayerEntry()
	{
		base.actionOnPlayerEntry();
		if (bigCraftable.Value && isOn.Value)
		{
			AmbientLocationSounds.addSound(tileLocation.Value, 1);
		}
	}

	/// <inheritdoc />
	public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
	{
		if (bigCraftable.Value)
		{
			if (justCheckingForActivity)
			{
				return true;
			}
			if (base.QualifiedItemId == "(BC)278")
			{
				Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2);
				Game1.activeClickableMenu = new CraftingPage((int)topLeftPositionForCenteringOnScreen.X, (int)topLeftPositionForCenteringOnScreen.Y, 800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, cooking: true, standaloneMenu: true);
				return true;
			}
			isOn.Value = !isOn.Value;
			if (isOn.Value)
			{
				if (bigCraftable.Value)
				{
					if (who != null)
					{
						Game1.playSound("fireball");
					}
					initializeLightSource(tileLocation.Value);
					AmbientLocationSounds.addSound(tileLocation.Value, 1);
				}
			}
			else if (bigCraftable.Value)
			{
				performRemoveAction();
				if (who != null)
				{
					Game1.playSound("woodyHit");
				}
			}
			return true;
		}
		return base.checkForAction(who, justCheckingForActivity);
	}

	/// <inheritdoc />
	public override bool placementAction(GameLocation location, int x, int y, Farmer who)
	{
		Vector2 key = new Vector2(x / 64, y / 64);
		Torch torch = (bigCraftable.Value ? new Torch(base.ItemId, bigCraftable: true) : new Torch(1, base.ItemId));
		if (bigCraftable.Value)
		{
			torch.isOn.Value = false;
		}
		location.objects.Add(key, torch);
		torch.initializeLightSource(key);
		if (who != null)
		{
			Game1.playSound("woodyStep");
		}
		return true;
	}

	public override bool isPassable()
	{
		return !bigCraftable.Value;
	}

	public override void updateWhenCurrentLocation(GameTime time)
	{
		base.updateWhenCurrentLocation(time);
		GameLocation location = Location;
		if (location == null)
		{
			return;
		}
		updateAshes((int)(tileLocation.X * 2000f + tileLocation.Y));
		smokePuffTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
		if (smokePuffTimer <= 0f)
		{
			smokePuffTimer = 1000f;
			if (base.QualifiedItemId == "(BC)278")
			{
				Utility.addSmokePuff(location, tileLocation.Value * 64f + new Vector2(32f, -32f));
			}
		}
	}

	private void updateAshes(int identifier)
	{
		if (!Utility.isOnScreen(tileLocation.Value * 64f, 256))
		{
			return;
		}
		for (int num = ashes.Length - 1; num >= 0; num--)
		{
			Vector2 vector = ashes[num];
			vector.Y -= 1f * ((float)(num + 1) * 0.25f);
			if (num % 2 != 0)
			{
				vector.X += (float)Math.Sin((double)ashes[num].Y / (Math.PI * 2.0)) / 2f;
			}
			ashes[num] = vector;
			if (Game1.random.NextDouble() < 0.0075 && ashes[num].Y < -100f)
			{
				ashes[num] = new Vector2((float)(Game1.random.Next(-1, 3) * 4) * 0.75f, 0f);
			}
		}
		color = Math.Max(-0.8f, Math.Min(0.7f, color + ashes[0].Y / 1200f));
	}

	public override void performRemoveAction()
	{
		AmbientLocationSounds.removeSound(TileLocation);
		if (bigCraftable.Value)
		{
			isOn.Value = false;
		}
		base.performRemoveAction();
	}

	public override void draw(SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha = 1f)
	{
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		Rectangle value = dataOrErrorItem.GetSourceRect(0, base.ParentSheetIndex).Clone();
		value.Y += 8;
		value.Height /= 2;
		spriteBatch.Draw(dataOrErrorItem.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile, yNonTile + 32)), value, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth);
		value.X = 276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(xNonTile * 320) + (double)(yNonTile * 49)) % 700.0 / 100.0) * 8;
		value.Y = 1965;
		value.Width = 8;
		value.Height = 8;
		spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile + 32 + 4, yNonTile + 16 + 4)), value, Color.White * 0.75f, 0f, new Vector2(4f, 4f), 3f, SpriteEffects.None, layerDepth + 1E-05f);
		spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile + 32 + 4, yNonTile + 16 + 4)), new Rectangle(88, 1779, 30, 30), Color.PaleGoldenrod * (Game1.currentLocation.IsOutdoors ? 0.35f : 0.43f), 0f, new Vector2(15f, 15f), 8f + (float)(32.0 * Math.Sin((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(xNonTile * 777) + (double)(yNonTile * 9746)) % 3140.0 / 1000.0) / 50.0), SpriteEffects.None, 1f);
	}

	public static void drawBasicTorch(SpriteBatch spriteBatch, float x, float y, float layerDepth, float alpha = 1f)
	{
		Rectangle value = new Rectangle(336, 48, 16, 16);
		value.Y += 8;
		value.Height /= 2;
		spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(x, y + 32f)), value, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth);
		spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x + 32f + 2f, y + 16f)), new Rectangle(88, 1779, 30, 30), Color.PaleGoldenrod * (Game1.currentLocation.IsOutdoors ? 0.35f : 0.43f), 0f, new Vector2(15f, 15f), 4f + (float)(64.0 * Math.Sin((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 777f) + (double)(y * 9746f)) % 3140.0 / 1000.0) / 50.0), SpriteEffects.None, 1f);
		value.X = 276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3204f) + (double)(y * 49f)) % 700.0 / 100.0) * 8;
		value.Y = 1965;
		value.Width = 8;
		value.Height = 8;
		spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x + 32f + 4f, y + 16f + 4f)), value, Color.White * 0.75f, 0f, new Vector2(4f, 4f), 3f, SpriteEffects.None, layerDepth + 0.0001f);
	}

	public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		if (Game1.eventUp)
		{
			GameLocation currentLocation = Game1.currentLocation;
			if ((currentLocation == null || currentLocation.currentEvent?.showGroundObjects != true) && !Game1.currentLocation.IsFarm)
			{
				return;
			}
		}
		if (!bigCraftable.Value)
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
			Rectangle value = dataOrErrorItem.GetSourceRect(0, base.ParentSheetIndex).Clone();
			Rectangle boundingBoxAt = GetBoundingBoxAt(x, y);
			value.Y += 8;
			value.Height /= 2;
			spriteBatch.Draw(dataOrErrorItem.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 + 32)), value, Color.White, 0f, Vector2.Zero, (scale.Y > 1f) ? getScale().Y : 4f, SpriteEffects.None, (float)(boundingBoxAt.Center.Y - 16) / 10000f);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + 2, y * 64 + 16)), new Rectangle(88, 1779, 30, 30), Color.PaleGoldenrod * (Game1.currentLocation.IsOutdoors ? 0.35f : 0.43f), 0f, new Vector2(15f, 15f), 4f + (float)(64.0 * Math.Sin((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 64 * 777) + (double)(y * 64 * 9746)) % 3140.0 / 1000.0) / 50.0), SpriteEffects.None, (float)(boundingBoxAt.Center.Y - 15) / 10000f);
			value.X = 276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3204) + (double)(y * 49)) % 700.0 / 100.0) * 8;
			value.Y = 1965;
			value.Width = 8;
			value.Height = 8;
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + 4, y * 64 + 16 + 4)), value, Color.White * 0.75f, 0f, new Vector2(4f, 4f), 3f, SpriteEffects.None, (float)(boundingBoxAt.Center.Y - 16) / 10000f);
			for (int i = 0; i < ashes.Length; i++)
			{
				spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, new Vector2((float)(x * 64 + 32) + ashes[i].X, (float)(y * 64 + 32) + ashes[i].Y)), new Rectangle(344 + i % 3, 53, 1, 1), Color.White * 0.5f * ((-100f - ashes[i].Y / 2f) / -100f), 0f, Vector2.Zero, 3f, SpriteEffects.None, (float)(boundingBoxAt.Center.Y - 16) / 10000f);
			}
			return;
		}
		base.draw(spriteBatch, x, y, alpha);
		float num = Math.Max(0f, (float)((y + 1) * 64 - 24) / 10000f) + (float)x * 1E-05f;
		if (!isOn.Value)
		{
			return;
		}
		if (ItemContextTagManager.HasBaseTag(base.QualifiedItemId, "campfire_item"))
		{
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 16 - 4, y * 64 - 8)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, num + 0.0008f);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 - 12, y * 64)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 2047) + (double)(y * 98)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, num + 0.0009f);
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 - 20, y * 64 + 12)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 2077) + (double)(y * 98)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, num + 0.001f);
			if (base.QualifiedItemId == "(BC)278")
			{
				ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
				Rectangle value2 = dataOrErrorItem2.GetSourceRect(1, base.ParentSheetIndex).Clone();
				value2.Height -= 16;
				Vector2 vector = getScale();
				vector *= 4f;
				Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 64 + 12));
				Rectangle destinationRectangle = new Rectangle((int)(vector2.X - vector.X / 2f) + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(vector2.Y - vector.Y / 2f) + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(64f + vector.X), (int)(64f + vector.Y / 2f));
				spriteBatch.Draw(dataOrErrorItem2.GetTexture(), destinationRectangle, value2, Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, num + 0.0028f);
			}
		}
		else
		{
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 16 - 8, y * 64 - 64 + 8)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, num + 0.0008f);
		}
	}
}
