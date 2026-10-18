using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewValley.Inventories;
using StardewValley.Objects;

namespace StardewValley.Buildings;

[Obsolete("The Mill class is only used to preserve data from old save files. All mills were converted into plain Building instances based on the rules in Data/Buildings. The input and output items are now stored in Building.buildingChests with the 'Input' and 'Output' keys respectively.")]
public class Mill : Building
{
	/// <summary>Obsolete. The <c>Mill</c> class is only used to preserve data from old save files. All mills were converted into plain <see cref="T:StardewValley.Buildings.Building" /> instances, with the input items in <see cref="F:StardewValley.Buildings.Building.buildingChests" /> with the <c>Input</c> key.</summary>
	[XmlElement("input")]
	public Chest obsolete_input;

	/// <summary>Obsolete. The <c>Mill</c> class is only used to preserve data from old save files. All mills were converted into plain <see cref="T:StardewValley.Buildings.Building" /> instances, with the output items in <see cref="F:StardewValley.Buildings.Building.buildingChests" /> with the <c>Output</c> key.</summary>
	[XmlElement("output")]
	public Chest obsolete_output;

	public Mill(Vector2 tileLocation)
		: base("Mill", tileLocation)
	{
	}

	public Mill()
		: this(Vector2.Zero)
	{
	}

	/// <summary>Copy the data from this mill to a new data-driven building instance.</summary>
	/// <param name="targetBuilding">The new building that will replace this instance.</param>
	public void TransferValuesToNewBuilding(Building targetBuilding)
	{
		Chest chest = obsolete_input;
		if (chest != null && chest.Items?.Count > 0)
		{
			IInventory items = obsolete_input.Items;
			Chest buildingChest = targetBuilding.GetBuildingChest("Input");
			for (int i = 0; i < items.Count; i++)
			{
				Item item = items[i];
				if (item != null)
				{
					items[i] = null;
					buildingChest.addItem(item);
				}
			}
			obsolete_input = null;
		}
		Chest chest2 = obsolete_output;
		if (chest2 == null || !(chest2.Items?.Count > 0))
		{
			return;
		}
		IInventory items2 = obsolete_output.Items;
		Chest buildingChest2 = targetBuilding.GetBuildingChest("Output");
		for (int j = 0; j < items2.Count; j++)
		{
			Item item2 = items2[j];
			if (item2 != null)
			{
				items2[j] = null;
				buildingChest2.addItem(item2);
			}
		}
		obsolete_output = null;
	}
}
