using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.GameData.Shirts;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;

namespace StardewValley.Objects;

public class Clothing : Item
{
	public enum ClothesType
	{
		SHIRT,
		PANTS
	}

	public const int SHIRT_SHEET_WIDTH = 128;

	public const string DefaultShirtSheetName = "Characters\\Farmer\\shirts";

	public const string DefaultPantsSheetName = "Characters\\Farmer\\pants";

	public const int MinShirtId = 1000;

	[XmlElement("price")]
	public readonly NetInt price = new NetInt();

	[XmlElement("indexInTileSheet")]
	public readonly NetInt indexInTileSheet = new NetInt();

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.Objects.Clothing.indexInTileSheet" /> instead.</summary>
	[XmlElement("indexInTileSheetFemale")]
	public int? obsolete_indexInTileSheetFemale;

	[XmlIgnore]
	public string description;

	[XmlIgnore]
	public string displayName;

	[XmlElement("clothesType")]
	public readonly NetEnum<ClothesType> clothesType = new NetEnum<ClothesType>();

	[XmlElement("dyeable")]
	public readonly NetBool dyeable = new NetBool(value: false);

	[XmlElement("clothesColor")]
	public readonly NetColor clothesColor = new NetColor(new Color(255, 255, 255));

	[XmlElement("isPrismatic")]
	public readonly NetBool isPrismatic = new NetBool(value: false);

	[XmlIgnore]
	protected bool _loadedData;

	/// <inheritdoc />
	public override string TypeDefinitionId
	{
		get
		{
			if (clothesType.Value != ClothesType.PANTS)
			{
				return "(S)";
			}
			return "(P)";
		}
	}

	public int Price
	{
		get
		{
			return price.Value;
		}
		set
		{
			price.Value = value;
		}
	}

	/// <inheritdoc />
	[XmlIgnore]
	public override string DisplayName
	{
		get
		{
			if (!_loadedData)
			{
				LoadData();
			}
			return displayName;
		}
	}

	public Clothing()
	{
		base.Category = -100;
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(price, "price").AddField(indexInTileSheet, "indexInTileSheet").AddField(clothesType, "clothesType")
			.AddField(dyeable, "dyeable")
			.AddField(clothesColor, "clothesColor")
			.AddField(isPrismatic, "isPrismatic");
	}

	public Clothing(string itemId)
		: this()
	{
		itemId = ValidateUnqualifiedItemId(itemId);
		Name = "Clothing";
		base.Category = -100;
		base.ItemId = itemId;
		LoadData(applyColor: true);
	}

	/// <summary>Apply the data from <see cref="F:StardewValley.Game1.pantsData" /> or <see cref="F:StardewValley.Game1.shirtData" /> to this item instance.</summary>
	/// <param name="applyColor">Whether to parse the tint color in field 6; else the tint is set to neutral white.</param>
	/// <param name="forceReload">Whether to reapply the latest data, even if this item was previously initialized.</param>
	public virtual void LoadData(bool applyColor = false, bool forceReload = false)
	{
		if (_loadedData && !forceReload)
		{
			return;
		}
		base.Category = -100;
		ShirtData value2;
		if (Game1.pantsData.TryGetValue(base.ItemId, out var value))
		{
			Name = value.Name;
			price.Value = value.Price;
			indexInTileSheet.Value = value.SpriteIndex;
			dyeable.Value = value.CanBeDyed;
			if (applyColor)
			{
				clothesColor.Value = Utility.StringToColor(value.DefaultColor) ?? Color.White;
			}
			else if (forceReload)
			{
				clothesColor.Value = Color.White;
			}
			displayName = TokenParser.ParseText(value.DisplayName);
			description = TokenParser.ParseText(value.Description);
			clothesType.Value = ClothesType.PANTS;
			isPrismatic.Value = value.IsPrismatic;
		}
		else if (Game1.shirtData.TryGetValue(base.ItemId, out value2))
		{
			Name = value2.Name;
			price.Value = value2.Price;
			indexInTileSheet.Value = value2.SpriteIndex;
			dyeable.Value = value2.CanBeDyed;
			if (applyColor)
			{
				clothesColor.Value = Utility.StringToColor(value2.DefaultColor) ?? Color.White;
			}
			else if (forceReload)
			{
				clothesColor.Value = Color.White;
			}
			displayName = TokenParser.ParseText(value2.DisplayName);
			description = TokenParser.ParseText(value2.Description);
			clothesType.Value = ClothesType.SHIRT;
			isPrismatic.Value = value2.IsPrismatic;
		}
		else
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
			displayName = dataOrErrorItem.DisplayName;
			description = dataOrErrorItem.Description;
		}
		if (dyeable.Value)
		{
			description = description + Environment.NewLine + Environment.NewLine + Game1.content.LoadString("Strings\\UI:Clothes_Dyeable");
		}
		_loadedData = true;
	}

	/// <inheritdoc />
	public override string getCategoryName()
	{
		return Object.GetCategoryDisplayName(-100);
	}

	/// <inheritdoc />
	public override int salePrice(bool ignoreProfitMargins = false)
	{
		return price.Value;
	}

	public virtual void Dye(Color color, float strength = 0.5f)
	{
		if (dyeable.Value)
		{
			Color value = clothesColor.Value;
			clothesColor.Value = new Color(Utility.MoveTowards((float)(int)value.R / 255f, (float)(int)color.R / 255f, strength), Utility.MoveTowards((float)(int)value.G / 255f, (float)(int)color.G / 255f, strength), Utility.MoveTowards((float)(int)value.B / 255f, (float)(int)color.B / 255f, strength), Utility.MoveTowards((float)(int)value.A / 255f, (float)(int)color.A / 255f, strength));
		}
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
		Color a = clothesColor.Value;
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		Texture2D texture = dataOrErrorItem.GetTexture();
		Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
		Rectangle value = Rectangle.Empty;
		if (!dataOrErrorItem.IsErrorItem)
		{
			if (clothesType.Value == ClothesType.SHIRT)
			{
				value = new Rectangle(sourceRect.X + texture.Width / 2, sourceRect.Y, sourceRect.Width, sourceRect.Height);
			}
			if (isPrismatic.Value)
			{
				a = Utility.GetPrismaticColor();
			}
		}
		switch (clothesType.Value)
		{
		case ClothesType.SHIRT:
		{
			float num = 1E-07f;
			if (layerDepth >= 1f - num)
			{
				layerDepth = 1f - num;
			}
			Vector2 origin = new Vector2(4f, 4f);
			if (dataOrErrorItem.IsErrorItem)
			{
				origin.X = sourceRect.Width / 2;
				origin.Y = sourceRect.Height / 2;
			}
			spriteBatch.Draw(texture, location + new Vector2(32f, 32f), sourceRect, color * transparency, 0f, origin, scaleSize * 4f, SpriteEffects.None, layerDepth);
			spriteBatch.Draw(texture, location + new Vector2(32f, 32f), value, Utility.MultiplyColor(a, color) * transparency, 0f, origin, scaleSize * 4f, SpriteEffects.None, layerDepth + num);
			break;
		}
		case ClothesType.PANTS:
			spriteBatch.Draw(texture, location + new Vector2(32f, 32f), sourceRect, Utility.MultiplyColor(a, color) * transparency, 0f, new Vector2(8f, 8f), scaleSize * 4f, SpriteEffects.None, layerDepth);
			break;
		}
		DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
	}

	public override int maximumStackSize()
	{
		return 1;
	}

	public override string getDescription()
	{
		if (!_loadedData)
		{
			LoadData();
		}
		return Game1.parseText(description, Game1.smallFont, getDescriptionWidth());
	}

	public override bool isPlaceable()
	{
		return false;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Clothing(base.ItemId);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is Clothing clothing)
		{
			clothesColor.Value = clothing.clothesColor.Value;
		}
	}
}
