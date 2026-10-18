using System;
using StardewValley.GameData.Weapons;
using StardewValley.Tools;

namespace StardewValley.Enchantments;

public class RubyEnchantment : BaseWeaponEnchantment
{
	protected override void _ApplyTo(Item item)
	{
		base._ApplyTo(item);
		if (item is MeleeWeapon meleeWeapon)
		{
			WeaponData data = meleeWeapon.GetData();
			if (data != null)
			{
				int minDamage = data.MinDamage;
				int maxDamage = data.MaxDamage;
				meleeWeapon.minDamage.Value += Math.Max(1, (int)((float)minDamage * 0.1f)) * GetLevel();
				meleeWeapon.maxDamage.Value += Math.Max(1, (int)((float)maxDamage * 0.1f)) * GetLevel();
			}
		}
	}

	protected override void _UnapplyTo(Item item)
	{
		base._UnapplyTo(item);
		if (item is MeleeWeapon meleeWeapon)
		{
			WeaponData data = meleeWeapon.GetData();
			if (data != null)
			{
				int minDamage = data.MinDamage;
				int maxDamage = data.MaxDamage;
				meleeWeapon.minDamage.Value -= Math.Max(1, (int)((float)minDamage * 0.1f)) * GetLevel();
				meleeWeapon.maxDamage.Value -= Math.Max(1, (int)((float)maxDamage * 0.1f)) * GetLevel();
			}
		}
	}

	public override bool ShouldBeDisplayed()
	{
		return false;
	}

	public override bool IsForge()
	{
		return true;
	}
}
