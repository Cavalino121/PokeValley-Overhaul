using StardewValley.Tools;

namespace StardewValley.Enchantments;

public class BottomlessEnchantment : WateringCanEnchantment
{
	public override string GetName()
	{
		return "Bottomless";
	}

	protected override void _ApplyTo(Item item)
	{
		base._ApplyTo(item);
		if (item is WateringCan wateringCan)
		{
			wateringCan.IsBottomless = true;
			wateringCan.WaterLeft = wateringCan.waterCanMax;
		}
	}

	protected override void _UnapplyTo(Item item)
	{
		base._UnapplyTo(item);
		if (item is WateringCan wateringCan)
		{
			wateringCan.IsBottomless = false;
		}
	}
}
