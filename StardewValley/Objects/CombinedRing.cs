using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Buffs;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Monsters;

namespace StardewValley.Objects;

public class CombinedRing : Ring
{
	public NetList<Ring, NetRef<Ring>> combinedRings = new NetList<Ring, NetRef<Ring>>();

	public CombinedRing()
		: base("880")
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(combinedRings, "combinedRings");
		combinedRings.OnElementChanged += delegate
		{
			OnCombinedRingsChanged();
		};
		combinedRings.OnArrayReplaced += delegate
		{
			OnCombinedRingsChanged();
		};
	}

	protected override bool loadDisplayFields()
	{
		base.loadDisplayFields();
		description = "";
		foreach (Ring combinedRing in combinedRings)
		{
			combinedRing.getDescription();
			description = description + combinedRing.description + "\n\n";
		}
		description = description.Trim();
		return true;
	}

	public override bool GetsEffectOfRing(string ringId)
	{
		foreach (Ring combinedRing in combinedRings)
		{
			if (combinedRing.GetsEffectOfRing(ringId))
			{
				return true;
			}
		}
		return base.GetsEffectOfRing(ringId);
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new CombinedRing();
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (!(source is CombinedRing combinedRing))
		{
			return;
		}
		combinedRings.Clear();
		foreach (Ring combinedRing2 in combinedRing.combinedRings)
		{
			Ring item = (Ring)combinedRing2.getOne();
			combinedRings.Add(item);
		}
	}

	public override int GetEffectsOfRingMultiplier(string ringId)
	{
		int num = 0;
		foreach (Ring combinedRing in combinedRings)
		{
			num += combinedRing.GetEffectsOfRingMultiplier(ringId);
		}
		return num;
	}

	/// <inheritdoc />
	public override void onEquip(Farmer who)
	{
		foreach (Ring combinedRing in combinedRings)
		{
			combinedRing.onEquip(who);
		}
		base.onEquip(who);
	}

	/// <inheritdoc />
	public override void onUnequip(Farmer who)
	{
		foreach (Ring combinedRing in combinedRings)
		{
			combinedRing.onUnequip(who);
		}
		base.onUnequip(who);
	}

	public override void AddEquipmentEffects(BuffEffects effects)
	{
		base.AddEquipmentEffects(effects);
		foreach (Ring combinedRing in combinedRings)
		{
			combinedRing.AddEquipmentEffects(effects);
		}
	}

	public override void onLeaveLocation(Farmer who, GameLocation environment)
	{
		foreach (Ring combinedRing in combinedRings)
		{
			combinedRing.onLeaveLocation(who, environment);
		}
		base.onLeaveLocation(who, environment);
	}

	/// <inheritdoc />
	public override void onMonsterSlay(Monster m, GameLocation location, Farmer who)
	{
		foreach (Ring combinedRing in combinedRings)
		{
			combinedRing.onMonsterSlay(m, location, who);
		}
		base.onMonsterSlay(m, location, who);
	}

	public override void onNewLocation(Farmer who, GameLocation environment)
	{
		foreach (Ring combinedRing in combinedRings)
		{
			combinedRing.onNewLocation(who, environment);
		}
		base.onNewLocation(who, environment);
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		if (combinedRings.Count >= 2)
		{
			AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
			float num = scaleSize;
			scaleSize = 1f;
			location.Y -= (num - 1f) * 32f;
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(combinedRings[0].QualifiedItemId);
			Texture2D texture = dataOrErrorItem.GetTexture();
			Rectangle value = dataOrErrorItem.GetSourceRect().Clone();
			value.X += 5;
			value.Y += 7;
			value.Width = 4;
			value.Height = 6;
			spriteBatch.Draw(texture, location + new Vector2(51f, 51f) * scaleSize + new Vector2(-12f, 8f) * scaleSize, value, color * transparency, 0f, new Vector2(1.5f, 2f) * 4f * scaleSize, scaleSize * 4f, SpriteEffects.None, layerDepth);
			value.X++;
			value.Y += 4;
			value.Width = 3;
			value.Height = 1;
			spriteBatch.Draw(texture, location + new Vector2(51f, 51f) * scaleSize + new Vector2(-8f, 4f) * scaleSize, value, color * transparency, 0f, new Vector2(1.5f, 2f) * 4f * scaleSize, scaleSize * 4f, SpriteEffects.None, layerDepth);
			ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(combinedRings[1].QualifiedItemId);
			texture = dataOrErrorItem2.GetTexture();
			value = dataOrErrorItem2.GetSourceRect().Clone();
			value.X += 9;
			value.Y += 7;
			value.Width = 4;
			value.Height = 6;
			spriteBatch.Draw(texture, location + new Vector2(51f, 51f) * scaleSize + new Vector2(4f, 8f) * scaleSize, value, color * transparency, 0f, new Vector2(1.5f, 2f) * 4f * scaleSize, scaleSize * 4f, SpriteEffects.None, layerDepth);
			value.Y += 4;
			value.Width = 3;
			value.Height = 1;
			spriteBatch.Draw(texture, location + new Vector2(51f, 51f) * scaleSize + new Vector2(4f, 4f) * scaleSize, value, color * transparency, 0f, new Vector2(1.5f, 2f) * 4f * scaleSize, scaleSize * 4f, SpriteEffects.None, layerDepth);
			Color? dyeColor = TailoringMenu.GetDyeColor(combinedRings[0]);
			Color? dyeColor2 = TailoringMenu.GetDyeColor(combinedRings[1]);
			Color color2 = Color.Red;
			Color color3 = Color.Blue;
			if (dyeColor.HasValue)
			{
				color2 = dyeColor.Value;
			}
			if (dyeColor2.HasValue)
			{
				color3 = dyeColor2.Value;
			}
			base.drawInMenu(spriteBatch, location + new Vector2(-5f, -1f), scaleSize, transparency, layerDepth, drawStackNumber, Utility.Get2PhaseColor(color2, color3), drawShadow);
			spriteBatch.Draw(Game1.objectSpriteSheet, location + new Vector2(13f, 35f) * scaleSize, new Rectangle(263, 579, 4, 2), Utility.Get2PhaseColor(color2, color3, 0, 1f, 1125f) * transparency, -(float)Math.PI / 2f, new Vector2(2f, 1.5f) * scaleSize, scaleSize * 4f, SpriteEffects.None, layerDepth);
			spriteBatch.Draw(Game1.objectSpriteSheet, location + new Vector2(49f, 35f) * scaleSize, new Rectangle(263, 579, 4, 2), Utility.Get2PhaseColor(color2, color3, 0, 1f, 375f) * transparency, (float)Math.PI / 2f, new Vector2(2f, 1.5f) * scaleSize, scaleSize * 4f, SpriteEffects.None, layerDepth);
			spriteBatch.Draw(Game1.objectSpriteSheet, location + new Vector2(31f, 53f) * scaleSize, new Rectangle(263, 579, 4, 2), Utility.Get2PhaseColor(color2, color3, 0, 1f, 750f) * transparency, (float)Math.PI, new Vector2(2f, 1.5f) * scaleSize, scaleSize * 4f, SpriteEffects.None, layerDepth);
			DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
		}
		else
		{
			base.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
		}
	}

	public override void update(GameTime time, GameLocation environment, Farmer who)
	{
		foreach (Ring combinedRing in combinedRings)
		{
			combinedRing.update(time, environment, who);
		}
		base.update(time, environment, who);
	}

	/// <summary>Update data when the <see cref="F:StardewValley.Objects.CombinedRing.combinedRings" /> list changes.</summary>
	protected virtual void OnCombinedRingsChanged()
	{
		description = null;
	}
}
