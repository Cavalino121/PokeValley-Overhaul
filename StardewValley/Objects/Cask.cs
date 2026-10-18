using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.GameData.Machines;
using StardewValley.Locations;
using StardewValley.Tools;

namespace StardewValley.Objects;

public class Cask : Object
{
	public const int defaultDaysToMature = 56;

	[XmlElement("agingRate")]
	public readonly NetFloat agingRate = new NetFloat();

	[XmlElement("daysToMature")]
	public readonly NetFloat daysToMature = new NetFloat();

	/// <inheritdoc />
	public override string TypeDefinitionId => "(BC)";

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(agingRate, "agingRate").AddField(daysToMature, "daysToMature");
	}

	public Cask()
	{
	}

	public Cask(Vector2 v)
		: base(v, "163")
	{
	}

	public override bool performToolAction(Tool t)
	{
		if (t != null && t.isHeavyHitter() && !(t is MeleeWeapon))
		{
			if (heldObject.Value != null)
			{
				Game1.createItemDebris(heldObject.Value, tileLocation.Value * 64f, -1);
			}
			playNearbySoundAll("woodWhack");
			if (heldObject.Value == null)
			{
				return true;
			}
			heldObject.Value = null;
			readyForHarvest.Value = false;
			minutesUntilReady.Value = -1;
			return false;
		}
		return base.performToolAction(t);
	}

	public virtual bool IsValidCaskLocation()
	{
		GameLocation location = Location;
		if (location == null)
		{
			return false;
		}
		if (!(location is Cellar))
		{
			return location.HasMapPropertyWithValue("CanCaskHere");
		}
		return true;
	}

	/// <summary>Get the output item to produce for a cask.</summary>
	/// <inheritdoc cref="T:StardewValley.Delegates.MachineOutputDelegate" />
	public static Item OutputCask(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
	{
		overrideMinutesUntilReady = null;
		if (!(machine is Cask cask))
		{
			return null;
		}
		if (!cask.IsValidCaskLocation())
		{
			if (Object.autoLoadFrom == null && !probe)
			{
				Game1.showRedMessageUsingLoadString("Strings\\Objects:CaskNoCellar");
			}
			return null;
		}
		if (cask.quality.Value >= 4)
		{
			return null;
		}
		if (inputItem.Quality >= 4)
		{
			return null;
		}
		float result = 1f;
		if (outputData?.CustomData != null && outputData.CustomData.TryGetValue("AgingMultiplier", out var value) && (!float.TryParse(value, out result) || result <= 0f))
		{
			Game1.log.Error("Failed to parse cask aging multiplier '" + value + "' for trigger rule. This must be a positive float value.");
			return null;
		}
		if (result > 0f)
		{
			Object obj = (Object)inputItem.getOne();
			if (!probe)
			{
				cask.agingRate.Value = result;
				cask.daysToMature.Value = cask.GetDaysForQuality(obj.Quality);
				overrideMinutesUntilReady = ((obj.Quality >= 4) ? 1 : 999999);
				return obj;
			}
			return obj;
		}
		return null;
	}

	/// <inheritdoc />
	public override bool TryApplyFairyDust(bool probe = false)
	{
		if (heldObject.Value == null)
		{
			return false;
		}
		if (heldObject.Value.Quality == 4)
		{
			return false;
		}
		if (!probe)
		{
			Utility.addSprinklesToLocation(Location, (int)tileLocation.X, (int)tileLocation.Y, 1, 2, 400, 40, Color.White);
			Game1.playSound("yoba");
			daysToMature.Value = GetDaysForQuality(GetNextQuality(heldObject.Value.Quality));
			checkForMaturity();
		}
		return true;
	}

	public override void DayUpdate()
	{
		base.DayUpdate();
		if (heldObject.Value != null)
		{
			minutesUntilReady.Value = 999999;
			daysToMature.Value -= agingRate.Value;
			checkForMaturity();
		}
	}

	public float GetDaysForQuality(int quality)
	{
		return quality switch
		{
			4 => 0f, 
			2 => 28f, 
			1 => 42f, 
			_ => 56f, 
		};
	}

	public int GetNextQuality(int quality)
	{
		switch (quality)
		{
		case 2:
		case 4:
			return 4;
		case 1:
			return 2;
		default:
			return 1;
		}
	}

	public void checkForMaturity()
	{
		if (daysToMature.Value <= GetDaysForQuality(GetNextQuality(heldObject.Value.quality.Value)))
		{
			heldObject.Value.quality.Value = GetNextQuality(heldObject.Value.quality.Value);
			if (heldObject.Value.Quality == 4)
			{
				minutesUntilReady.Value = 1;
			}
		}
	}

	public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		base.draw(spriteBatch, x, y, alpha);
		Object value = heldObject.Value;
		if (value != null && value.quality.Value > 0)
		{
			Vector2 vector = ((base.MinutesUntilReady > 0) ? new Vector2(Math.Abs(scale.X - 5f), Math.Abs(scale.Y - 5f)) : Vector2.Zero);
			vector *= 4f;
			Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 64));
			Rectangle destinationRectangle = new Rectangle((int)(vector2.X + 32f - 8f - vector.X / 2f) + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(vector2.Y + 64f + 8f - vector.Y / 2f) + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(16f + vector.X), (int)(16f + vector.Y / 2f));
			spriteBatch.Draw(Game1.mouseCursors, destinationRectangle, (heldObject.Value.quality.Value < 4) ? new Rectangle(338 + (heldObject.Value.quality.Value - 1) * 8, 400, 8, 8) : new Rectangle(346, 392, 8, 8), Color.White * 0.95f, 0f, Vector2.Zero, SpriteEffects.None, (float)((y + 1) * 64) / 10000f);
		}
	}
}
