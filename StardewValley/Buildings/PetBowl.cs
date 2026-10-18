using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.GameData.Buildings;
using StardewValley.Tools;

namespace StardewValley.Buildings;

public class PetBowl : Building
{
	/// <summary>Whether the pet bowl is full.</summary>
	[XmlElement("watered")]
	public readonly NetBool watered = new NetBool();

	private int nameTimer;

	private string nameTimerMessage;

	/// <summary>The pet to which this bowl belongs, if any.</summary>
	/// <remarks>When a pet is assigned, this matches <see cref="F:StardewValley.Characters.Pet.petId" />.</remarks>
	[XmlElement("petGuid")]
	public readonly NetGuid petId = new NetGuid();

	public PetBowl(Vector2 tileLocation)
		: base("Pet Bowl", tileLocation)
	{
	}

	public PetBowl()
		: this(Vector2.Zero)
	{
	}

	/// <summary>Assign a pet to this pet bowl.</summary>
	/// <param name="pet">The pet to assign.</param>
	public virtual void AssignPet(Pet pet)
	{
		petId.Value = pet.petId.Value;
		pet.homeLocationName.Value = parentLocationName.Value;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(watered, "watered").AddField(petId, "petId");
	}

	public virtual Point GetPetSpot()
	{
		return new Point(tileX.Value, tileY.Value + 1);
	}

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (!isTilePassable(tileLocation))
		{
			_ = petId.Value;
			Pet pet = Utility.findPet(petId.Value);
			if (pet != null)
			{
				nameTimer = 3500;
				nameTimerMessage = Game1.content.LoadString("Strings\\1_6_Strings:PetBowlName", pet.displayName);
			}
		}
		return base.doAction(tileLocation, who);
	}

	public override void Update(GameTime time)
	{
		if (nameTimer > 0)
		{
			nameTimer -= (int)time.ElapsedGameTime.TotalMilliseconds;
		}
		base.Update(time);
	}

	public override void performToolAction(Tool t, int tileX, int tileY)
	{
		if (t is WateringCan)
		{
			string property_value = null;
			if (doesTileHaveProperty(tileX, tileY, "PetBowl", "Buildings", ref property_value))
			{
				watered.Value = true;
			}
		}
		base.performToolAction(t, tileX, tileY);
	}

	/// <summary>Get whether any pet has been assigned to this pet bowl.</summary>
	public bool HasPet()
	{
		return petId.Value != Guid.Empty;
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		if (base.isMoving || isUnderConstruction())
		{
			return;
		}
		if (watered.Value)
		{
			BuildingData data = GetData();
			float num = (tileY.Value + tilesHigh.Value) * 64;
			if (data != null)
			{
				num -= data.SortTileOffset * 64f;
			}
			num += 1.5f;
			num /= 10000f;
			Vector2 vector = new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64);
			Vector2 vector2 = Vector2.Zero;
			if (data != null)
			{
				vector2 = data.DrawOffset * 4f;
			}
			Rectangle sourceRect = getSourceRect();
			sourceRect.X += sourceRect.Width;
			b.Draw(origin: new Vector2(0f, sourceRect.Height), texture: texture.Value, position: Game1.GlobalToLocal(Game1.viewport, vector + vector2), sourceRectangle: sourceRect, color: color * alpha, rotation: 0f, scale: 4f, effects: SpriteEffects.None, layerDepth: num);
		}
		if (nameTimer > 0)
		{
			BuildingData data2 = GetData();
			float num2 = (tileY.Value + tilesHigh.Value) * 64;
			if (data2 != null)
			{
				num2 -= data2.SortTileOffset * 64f;
			}
			num2 += 1.5f;
			num2 /= 10000f;
			SpriteText.drawSmallTextBubble(b, nameTimerMessage, Game1.GlobalToLocal(new Vector2(((float)tileX.Value + 1.5f) * 64f, tileY.Value * 64 - 32)), -1, num2 + 1E-06f);
		}
	}
}
