using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Network;

namespace StardewValley.Objects;

public class Workbench : Object
{
	[XmlIgnore]
	public readonly NetMutex mutex = new NetMutex();

	/// <inheritdoc />
	public override string TypeDefinitionId => "(BC)";

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(mutex.NetFields, "mutex.NetFields");
	}

	public Workbench()
	{
	}

	public Workbench(Vector2 position)
		: base(position, "208")
	{
		Name = "Workbench";
		type.Value = "Crafting";
		bigCraftable.Value = true;
		canBeSetDown.Value = true;
	}

	/// <inheritdoc />
	public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
	{
		GameLocation location = Location;
		if (location == null)
		{
			return false;
		}
		if (justCheckingForActivity)
		{
			return true;
		}
		List<Chest> list = new List<Chest>();
		Point? fridgePosition = location.GetFridgePosition();
		Vector2[] array = new Vector2[8]
		{
			new Vector2(-1f, 1f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f),
			new Vector2(-1f, 0f),
			new Vector2(1f, 0f),
			new Vector2(-1f, -1f),
			new Vector2(0f, -1f),
			new Vector2(1f, -1f)
		};
		for (int i = 0; i < array.Length; i++)
		{
			Vector2 key = new Vector2((int)(tileLocation.X + array[i].X), (int)(tileLocation.Y + array[i].Y));
			if ((int)tileLocation.X == fridgePosition?.X && (int)tileLocation.Y == fridgePosition.Value.Y)
			{
				Chest fridge = location.GetFridge();
				if (fridge != null)
				{
					list.Add(fridge);
				}
			}
			if (location.objects.TryGetValue(key, out var value) && value is Chest chest && (chest.SpecialChestType == Chest.SpecialChestTypes.None || chest.SpecialChestType == Chest.SpecialChestTypes.BigChest))
			{
				list.Add(chest);
			}
		}
		List<NetMutex> list2 = new List<NetMutex>();
		List<IInventory> inventories = new List<IInventory>();
		foreach (Chest item in list)
		{
			list2.Add(item.mutex);
			inventories.Add(item.Items);
		}
		if (!mutex.IsLocked())
		{
			new MultipleMutexRequest(list2, delegate(MultipleMutexRequest request)
			{
				mutex.RequestLock(delegate
				{
					Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2);
					Game1.activeClickableMenu = new CraftingPage((int)topLeftPositionForCenteringOnScreen.X, (int)topLeftPositionForCenteringOnScreen.Y, 800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, cooking: false, standaloneMenu: true, inventories);
					Game1.activeClickableMenu.exitFunction = delegate
					{
						mutex.ReleaseLock();
						request.ReleaseLocks();
					};
				}, request.ReleaseLocks);
			}, delegate
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Workbench_Chest_Warning"));
			});
		}
		return true;
	}

	public override void updateWhenCurrentLocation(GameTime time)
	{
		GameLocation location = Location;
		if (location != null)
		{
			mutex.Update(location);
		}
		base.updateWhenCurrentLocation(time);
	}
}
