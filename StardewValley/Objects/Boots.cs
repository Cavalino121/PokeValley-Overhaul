using System;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Buffs;
using StardewValley.ItemTypeDefinitions;

namespace StardewValley.Objects;

public class Boots : Item
{
	[XmlElement("defenseBonus")]
	public readonly NetInt defenseBonus = new NetInt();

	[XmlElement("immunityBonus")]
	public readonly NetInt immunityBonus = new NetInt();

	[XmlElement("indexInTileSheet")]
	public readonly NetInt indexInTileSheet = new NetInt();

	[XmlElement("price")]
	public readonly NetInt price = new NetInt();

	[XmlElement("indexInColorSheet")]
	public readonly NetInt indexInColorSheet = new NetInt();

	[XmlElement("appliedBootSheetIndex")]
	public readonly NetString appliedBootSheetIndex = new NetString();

	/// <summary>The cached value for <see cref="P:StardewValley.Objects.Boots.DisplayName" />.</summary>
	[XmlIgnore]
	public string displayName;

	[XmlIgnore]
	public string description;

	/// <inheritdoc />
	public override string TypeDefinitionId { get; } = "(B)";

	/// <inheritdoc />
	[XmlIgnore]
	public override string DisplayName
	{
		get
		{
			if (displayName == null)
			{
				loadDisplayFields();
			}
			return displayName;
		}
	}

	public Boots()
	{
		base.Category = -97;
	}

	public Boots(string itemId)
		: this()
	{
		itemId = ValidateUnqualifiedItemId(itemId);
		base.ItemId = itemId;
		reloadData();
		base.Category = -97;
	}

	/// <inheritdoc />
	protected override void MigrateLegacyItemId()
	{
		base.ItemId = indexInTileSheet.Value.ToString();
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(defenseBonus, "defenseBonus").AddField(immunityBonus, "immunityBonus").AddField(indexInTileSheet, "indexInTileSheet")
			.AddField(price, "price")
			.AddField(indexInColorSheet, "indexInColorSheet")
			.AddField(appliedBootSheetIndex, "appliedBootSheetIndex");
	}

	public virtual void reloadData()
	{
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		string[] array = DataLoader.Boots(Game1.content)[base.ItemId].Split('/');
		Name = ArgUtility.Get(array, 0, null, allowBlank: false) ?? dataOrErrorItem.InternalName;
		price.Value = Convert.ToInt32(array[2]);
		defenseBonus.Value = Convert.ToInt32(array[3]);
		immunityBonus.Value = Convert.ToInt32(array[4]);
		indexInColorSheet.Value = Convert.ToInt32(array[5]);
		indexInTileSheet.Value = dataOrErrorItem.SpriteIndex;
	}

	public void applyStats(Boots applied_boots)
	{
		reloadData();
		if (defenseBonus.Value == applied_boots.defenseBonus.Value && immunityBonus.Value == applied_boots.immunityBonus.Value)
		{
			appliedBootSheetIndex.Value = null;
		}
		else
		{
			appliedBootSheetIndex.Value = applied_boots.getStatsIndex();
		}
		defenseBonus.Value = applied_boots.defenseBonus.Value;
		immunityBonus.Value = applied_boots.immunityBonus.Value;
		price.Value = applied_boots.price.Value;
		loadDisplayFields();
	}

	public virtual string getStatsIndex()
	{
		return appliedBootSheetIndex.Value ?? base.ItemId;
	}

	/// <inheritdoc />
	public override int salePrice(bool ignoreProfitMargins = false)
	{
		return defenseBonus.Value * 100 + immunityBonus.Value * 100;
	}

	/// <inheritdoc />
	public override void onEquip(Farmer who)
	{
		base.onEquip(who);
		who.changeShoeColor(GetBootsColorString());
	}

	/// <inheritdoc />
	public override void onUnequip(Farmer who)
	{
		base.onUnequip(who);
		who.changeShoeColor("12");
	}

	public override void AddEquipmentEffects(BuffEffects effects)
	{
		base.AddEquipmentEffects(effects);
		effects.Defense.Value += defenseBonus.Value;
		effects.Immunity.Value += immunityBonus.Value;
	}

	public string GetBootsColorString()
	{
		if (DataLoader.Boots(Game1.content).TryGetValue(base.ItemId, out var value))
		{
			string[] array = value.Split('/');
			if (array.Length > 7 && array[7] != "")
			{
				return array[7] + ":" + indexInColorSheet.Value;
			}
		}
		return indexInColorSheet.Value.ToString();
	}

	public int getNumberOfDescriptionCategories()
	{
		if (immunityBonus.Value > 0 && defenseBonus.Value > 0)
		{
			return 2;
		}
		return 1;
	}

	public override void drawTooltip(SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
	{
		Utility.drawTextWithShadow(spriteBatch, Game1.parseText(description, Game1.smallFont, getDescriptionWidth()), font, new Vector2(x + 16, y + 16 + 4), Game1.textColor);
		y += (int)font.MeasureString(Game1.parseText(description, Game1.smallFont, getDescriptionWidth())).Y;
		if (defenseBonus.Value > 0)
		{
			Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(110, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
			Utility.drawTextWithShadow(spriteBatch, Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", defenseBonus), font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
			y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
		}
		if (immunityBonus.Value > 0)
		{
			Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(150, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
			Utility.drawTextWithShadow(spriteBatch, Game1.content.LoadString("Strings\\UI:ItemHover_ImmunityBonus", immunityBonus), font, new Vector2(x + 16 + 52, y + 16 + 12), Game1.textColor * 0.9f * alpha);
			y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
		}
	}

	public override Point getExtraSpaceNeededForTooltipSpecialIcons(SpriteFont font, int minWidth, int horizontalBuffer, int startingHeight, StringBuilder descriptionText, string boldTitleText, int moneyAmountToDisplayAtBottom)
	{
		int num = 9999;
		Point result = new Point(0, startingHeight);
		result.Y -= (int)font.MeasureString(descriptionText).Y;
		result.Y += (int)((float)(getNumberOfDescriptionCategories() * 4 * 12) + font.MeasureString(Game1.parseText(description, Game1.smallFont, getDescriptionWidth())).Y);
		result.X = (int)Math.Max(minWidth, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", num)).X + (float)horizontalBuffer, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_ImmunityBonus", num)).X + (float)horizontalBuffer));
		return result;
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		spriteBatch.Draw(dataOrErrorItem.GetTexture(), location + new Vector2(32f, 32f) * scaleSize, dataOrErrorItem.GetSourceRect(), color * transparency, 0f, new Vector2(8f, 8f) * scaleSize, scaleSize * 4f, SpriteEffects.None, layerDepth);
		DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
	}

	public override int maximumStackSize()
	{
		return 1;
	}

	/// <inheritdoc />
	public override string getCategoryName()
	{
		return Object.GetCategoryDisplayName(-97);
	}

	public override string getDescription()
	{
		if (description == null)
		{
			loadDisplayFields();
		}
		return Game1.parseText(description + Environment.NewLine + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:Boots.cs.12500", immunityBonus.Value + defenseBonus.Value), Game1.smallFont, getDescriptionWidth());
	}

	public override bool isPlaceable()
	{
		return false;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Boots(base.ItemId);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is Boots boots)
		{
			appliedBootSheetIndex.Value = boots.appliedBootSheetIndex.Value;
			indexInColorSheet.Value = boots.indexInColorSheet.Value;
			defenseBonus.Value = boots.defenseBonus.Value;
			immunityBonus.Value = boots.immunityBonus.Value;
			loadDisplayFields();
		}
	}

	protected virtual bool loadDisplayFields()
	{
		if (DataLoader.Boots(Game1.content).TryGetValue(base.ItemId, out var value))
		{
			string[] array = value.Split('/');
			displayName = Name;
			if (array.Length > 6)
			{
				displayName = array[6];
			}
			if (appliedBootSheetIndex.Value != null)
			{
				displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:CustomizedBootItemName", DisplayName);
			}
			description = array[1];
			return true;
		}
		return false;
	}
}
