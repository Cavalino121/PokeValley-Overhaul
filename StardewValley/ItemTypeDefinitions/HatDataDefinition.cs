using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;

namespace StardewValley.ItemTypeDefinitions;

/// <summary>Manages the data for hat items.</summary>
public class HatDataDefinition : BaseItemDataDefinition
{
	/// <inheritdoc />
	public override string Identifier => "(H)";

	/// <inheritdoc />
	public override string StandardDescriptor => "H";

	/// <inheritdoc />
	public override IEnumerable<string> GetAllIds()
	{
		return GetDataSheet().Keys;
	}

	/// <inheritdoc />
	public override bool Exists(string itemId)
	{
		if (itemId != null)
		{
			return GetDataSheet().ContainsKey(itemId);
		}
		return false;
	}

	/// <inheritdoc />
	public override ParsedItemData GetData(string itemId)
	{
		string[] rawData = GetRawData(itemId);
		if (rawData == null)
		{
			return null;
		}
		return new ParsedItemData(this, itemId, GetSpriteIndex(itemId, rawData), ArgUtility.Get(rawData, 7) ?? "Characters\\Farmer\\hats", ArgUtility.Get(rawData, 0), ArgUtility.Get(rawData, 5), ArgUtility.Get(rawData, 1), -95, null, rawData);
	}

	/// <inheritdoc />
	public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (texture == null)
		{
			throw new ArgumentNullException("texture");
		}
		return new Rectangle(spriteIndex * 20 % texture.Width, spriteIndex * 20 / texture.Width * 20 * 4, 20, 20);
	}

	/// <inheritdoc />
	public override Item CreateItem(ParsedItemData data)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		return new Hat(data.ItemId);
	}

	/// <summary>Get the item type's data asset.</summary>
	protected Dictionary<string, string> GetDataSheet()
	{
		return DataLoader.Hats(Game1.content);
	}

	/// <summary>Get the raw data fields for an item.</summary>
	/// <param name="itemId">The unqualified item ID.</param>
	protected string[] GetRawData(string itemId)
	{
		if (itemId == null || !GetDataSheet().TryGetValue(itemId, out var value))
		{
			return null;
		}
		return value.Split('/');
	}

	/// <summary>Get the sprite index for an item.</summary>
	/// <param name="itemId">The unqualified item ID.</param>
	/// <param name="fields">The raw data fields.</param>
	protected int GetSpriteIndex(string itemId, string[] fields)
	{
		int num = ArgUtility.GetInt(fields, 6, -1);
		if (num > -1)
		{
			return num;
		}
		if (int.TryParse(itemId, out var result))
		{
			return result;
		}
		return -1;
	}
}
