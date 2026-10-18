using System;
using Microsoft.Xna.Framework;
using StardewValley.Companions;
using StardewValley.Extensions;
using StardewValley.Monsters;

namespace StardewValley.Objects.Trinkets;

/// <summary>Implements the special behavior for a <see cref="T:StardewValley.Objects.Trinkets.Trinket" /> which summons a fairy which heals the player.</summary>
public class FairyBoxTrinketEffect : TrinketEffect
{
	/// <summary>The number of milliseconds until the fairy next heals the player.</summary>
	public float HealTimer;

	/// <summary>The number of milliseconds between each heal.</summary>
	public float HealDelay = 4000f;

	/// <summary>The power rating applied to the heal amount.</summary>
	public float Power = 0.25f;

	/// <summary>The amount of damage taken by the player since the last heal.</summary>
	public int DamageSinceLastHeal;

	/// <inheritdoc />
	public FairyBoxTrinketEffect(Trinket trinket)
		: base(trinket)
	{
	}

	/// <inheritdoc />
	public override bool GenerateRandomStats(Trinket trinket)
	{
		Random random = Utility.CreateRandom(trinket.generationSeed.Value);
		int num = 1;
		if (random.NextBool(0.45))
		{
			num = 2;
		}
		else if (random.NextBool(0.25))
		{
			num = 3;
		}
		else if (random.NextBool(0.125))
		{
			num = 4;
		}
		else if (random.NextBool(0.0675))
		{
			num = 5;
		}
		HealDelay = 5000 - num * 300;
		Power = 0.7f + (float)num * 0.1f;
		trinket.descriptionSubstitutionTemplates.Clear();
		trinket.descriptionSubstitutionTemplates.Add(num.ToString());
		return true;
	}

	/// <inheritdoc />
	public override void OnDamageMonster(Farmer farmer, Monster monster, int damageAmount, bool isBomb, bool isCriticalHit)
	{
		DamageSinceLastHeal += damageAmount;
		base.OnDamageMonster(farmer, monster, damageAmount, isBomb, isCriticalHit);
	}

	/// <inheritdoc />
	public override void OnReceiveDamage(Farmer farmer, int damageAmount)
	{
		DamageSinceLastHeal += damageAmount;
		base.OnReceiveDamage(farmer, damageAmount);
	}

	/// <inheritdoc />
	public override void Update(Farmer farmer, GameTime time, GameLocation location)
	{
		HealTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
		if (HealTimer >= HealDelay)
		{
			if (farmer.health < farmer.maxHealth && DamageSinceLastHeal >= 0)
			{
				int num = (int)Math.Min(Math.Pow(DamageSinceLastHeal, 0.33000001311302185), (float)farmer.maxHealth / 10f);
				num = (int)((float)num * Power);
				num += Game1.random.Next((int)((float)(-num) * 0.25f), (int)((float)num * 0.25f) + 1);
				if (num > 0)
				{
					farmer.health = Math.Min(farmer.maxHealth, farmer.health + num);
					location.debris.Add(new Debris(num, farmer.getStandingPosition(), Color.Lime, 1f, farmer));
					Game1.playSound("fairy_heal");
					DamageSinceLastHeal = 0;
				}
			}
			HealTimer = 0f;
		}
		base.Update(farmer, time, location);
	}

	/// <inheritdoc />
	public override void Apply(Farmer farmer)
	{
		HealTimer = 0f;
		DamageSinceLastHeal = 0;
		Companion = new FlyingCompanion(0);
		if (Game1.gameMode == 3)
		{
			farmer.AddCompanion(Companion);
		}
		base.Apply(farmer);
	}

	/// <inheritdoc />
	public override void Unapply(Farmer farmer)
	{
		farmer.RemoveCompanion(Companion);
	}
}
