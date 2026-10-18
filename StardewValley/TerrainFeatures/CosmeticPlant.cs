using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.Tools;

namespace StardewValley.TerrainFeatures;

public class CosmeticPlant : Grass
{
	[XmlElement("flipped")]
	public readonly NetBool flipped = new NetBool();

	[XmlElement("xOffset")]
	private readonly NetInt xOffset = new NetInt();

	[XmlElement("yOffset")]
	private readonly NetInt yOffset = new NetInt();

	public CosmeticPlant()
	{
	}

	public CosmeticPlant(int which)
		: base(which, 1)
	{
		flipped.Value = Game1.random.NextBool();
	}

	public override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(flipped, "flipped").AddField(xOffset, "xOffset").AddField(yOffset, "yOffset");
	}

	public override Rectangle getBoundingBox()
	{
		Vector2 tile = Tile;
		return new Rectangle((int)(tile.X * 64f + 16f), (int)((tile.Y + 1f) * 64f - 8f - 4f), 8, 8);
	}

	public override string textureName()
	{
		return "TerrainFeatures\\upperCavePlants";
	}

	public override void loadSprite()
	{
		xOffset.Value = Game1.random.Next(-2, 3) * 4;
		yOffset.Value = Game1.random.Next(-2, 1) * 4;
	}

	public override bool performToolAction(Tool t, int explosion, Vector2 tileLocation)
	{
		GameLocation location = Location;
		if ((t is MeleeWeapon meleeWeapon && meleeWeapon.type.Value != 2) || explosion > 0)
		{
			shake((float)Math.PI * 3f / 32f, (float)Math.PI / 40f, Game1.random.NextBool());
			int num = ((explosion > 0) ? Math.Max(1, explosion + 2 - Game1.random.Next(2)) : ((t.upgradeLevel.Value == 3) ? 3 : (t.upgradeLevel.Value + 1)));
			Game1.createRadialDebris(location, textureName(), new Rectangle(grassType.Value * 16, 6, 7, 6), (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(6, 14));
			numberOfWeeds.Value -= num;
			if (numberOfWeeds.Value <= 0)
			{
				Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)tileLocation.X * 7.0, (double)tileLocation.Y * 11.0, Game1.CurrentMineLevel, Game1.player.timesReachedMineBottom);
				if (random.NextDouble() < 0.005)
				{
					Game1.createObjectDebris("(O)114", (int)tileLocation.X, (int)tileLocation.Y, -1, 0, 1f, location);
				}
				else if (random.NextDouble() < 0.01)
				{
					Game1.createDebris(4, (int)tileLocation.X, (int)tileLocation.Y, random.Next(1, 2), location);
				}
				else if (random.NextDouble() < 0.02)
				{
					Game1.createDebris(92, (int)tileLocation.X, (int)tileLocation.Y, random.Next(2, 4), location);
				}
				return true;
			}
		}
		return false;
	}

	public override void draw(SpriteBatch spriteBatch)
	{
		Vector2 tile = Tile;
		spriteBatch.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f, tile.Y * 64f) + new Vector2(32 + xOffset.Value, 60 + yOffset.Value)), new Rectangle(grassType.Value * 16, 0, 16, 24), Color.White, shakeRotation, new Vector2(8f, 23f), 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, ((float)(getBoundingBox().Y - 4) + tile.X / 900f + 0.01f) / 10000f);
	}
}
