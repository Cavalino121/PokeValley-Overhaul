using System;
using Microsoft.Xna.Framework;
using StardewValley.Projectiles;
using StardewValley.Tools;

namespace StardewValley.Enchantments;

public class MagicEnchantment : BaseWeaponEnchantment
{
	protected override void _OnSwing(MeleeWeapon weapon, Farmer farmer)
	{
		base._OnSwing(weapon, farmer);
		Vector2 vector = default(Vector2);
		Vector2 startingPosition = farmer.getStandingPosition() - new Vector2(32f, 32f);
		switch (farmer.facingDirection.Value)
		{
		case 0:
			vector.Y = -1f;
			break;
		case 1:
			vector.X = 1f;
			break;
		case 3:
			vector.X = -1f;
			break;
		case 2:
			vector.Y = 1f;
			break;
		}
		float num = 32f;
		vector *= 10f;
		BasicProjectile basicProjectile = new BasicProjectile((int)Math.Ceiling((float)weapon.minDamage.Value / 4f), 11, 0, 1, num * ((float)Math.PI / 180f), vector.X, vector.Y, startingPosition, null, null, null, explode: false, damagesMonsters: true, farmer.currentLocation, farmer);
		basicProjectile.ignoreTravelGracePeriod.Value = true;
		basicProjectile.ignoreMeleeAttacks.Value = true;
		basicProjectile.maxTravelDistance.Value = 256;
		basicProjectile.height.Value = 32f;
		farmer.currentLocation.projectiles.Add(basicProjectile);
	}

	public override string GetName()
	{
		return "Starburst";
	}
}
