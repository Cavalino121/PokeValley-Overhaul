using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Delegates;
using StardewValley.Inventories;
using StardewValley.Network;

namespace StardewValley.Internal;

/// <summary>The metadata and operations for an item being iterated via a method like <see cref="M:StardewValley.Utility.ForEachItem(System.Func{StardewValley.Item,System.Boolean})" />.</summary>
public readonly struct ForEachItemContext
{
	/// <summary>The current item in the iteration.</summary>
	public readonly Item Item;

	/// <summary>Delete this item from the game.</summary>
	public readonly Action RemoveItem;

	/// <summary>Remove this item and replace it with the given instance.</summary>
	public readonly Action<Item> ReplaceItemWith;

	/// <summary>Get the contextual path leading to this item. For example, an item inside a chest would have the location and chest as path values.</summary>
	public readonly GetForEachItemPathDelegate GetPath;

	/// <summary>Set the contextual values. This should only be called by the code which implements the iteration.</summary>
	/// <param name="item"><inheritdoc cref="F:StardewValley.Internal.ForEachItemContext.Item" path="/summary" /></param>
	/// <param name="remove"><inheritdoc cref="F:StardewValley.Internal.ForEachItemContext.RemoveItem" path="/summary" /></param>
	/// <param name="replaceWith"><inheritdoc cref="F:StardewValley.Internal.ForEachItemContext.ReplaceItemWith" path="/summary" /></param>
	/// <param name="getPath"><inheritdoc cref="F:StardewValley.Internal.ForEachItemContext.GetPath" path="/summary" /></param>
	public ForEachItemContext(Item item, Action remove, Action<Item> replaceWith, GetForEachItemPathDelegate getPath)
	{
		Item = item;
		RemoveItem = remove;
		ReplaceItemWith = replaceWith;
		GetPath = getPath;
	}

	/// <summary>Get a human-readable representation of the <see cref="F:StardewValley.Internal.ForEachItemContext.GetPath" /> values.</summary>
	/// <param name="includeItem">Whether to add a segment for the item itself.</param>
	public IList<string> GetDisplayPath(bool includeItem = false)
	{
		List<string> list = new List<string>();
		foreach (object item in GetPath())
		{
			AddDisplayPath(list, item);
		}
		if (includeItem)
		{
			AddDisplayPath(list, Item);
		}
		return list;
	}

	/// <summary>Add human-readable path segments path for a raw <see cref="F:StardewValley.Internal.ForEachItemContext.GetPath" /> value.</summary>
	/// <param name="path">The path to populate.</param>
	/// <param name="pathValue">The segment from <see cref="F:StardewValley.Internal.ForEachItemContext.GetPath" /> to represent.</param>
	private void AddDisplayPath(IList<string> path, object pathValue)
	{
		if (!(pathValue is GameLocation gameLocation))
		{
			if (!(pathValue is Building building))
			{
				if (!(pathValue is Object obj))
				{
					if (!(pathValue is Farmer farmer))
					{
						if (!(pathValue is Item item))
						{
							if (!(pathValue is INetSerializable netSerializable))
							{
								if (!(pathValue is IInventory) && !(pathValue is OverlaidDictionary))
								{
									path.Add(pathValue.ToString());
								}
							}
							else
							{
								path.Add(netSerializable.Name);
							}
						}
						else
						{
							path.Add(item.Name);
						}
					}
					else
					{
						path.Add("player '" + farmer.Name + "'");
					}
				}
				else
				{
					if (path.Count == 0 && obj.Location != null)
					{
						AddDisplayPath(path, obj.Location);
					}
					path.Add((obj.TileLocation != Vector2.Zero) ? $"{obj.Name} at {obj.TileLocation.X}, {obj.TileLocation.Y}" : obj.Name);
				}
				return;
			}
			if (path.Count == 0)
			{
				GameLocation parentLocation = building.GetParentLocation();
				if (parentLocation != null)
				{
					AddDisplayPath(path, parentLocation);
				}
			}
			path.Add($"{building.buildingType.Value} at {building.tileX.Value}, {building.tileY.Value}");
		}
		else
		{
			if (path.Count == 0 && gameLocation.ParentBuilding != null)
			{
				AddDisplayPath(path, gameLocation.ParentBuilding);
			}
			path.Add(gameLocation.NameOrUniqueName);
		}
	}
}
