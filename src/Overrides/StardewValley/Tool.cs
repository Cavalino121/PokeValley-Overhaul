using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Buffs;
using StardewValley.Enchantments;
using StardewValley.GameData.Tools;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;

namespace StardewValley;

[XmlInclude(typeof(Axe))]
[XmlInclude(typeof(ErrorTool))]
[XmlInclude(typeof(FishingRod))]
[XmlInclude(typeof(GenericTool))]
[XmlInclude(typeof(Hoe))]
[XmlInclude(typeof(MeleeWeapon))]
[XmlInclude(typeof(MilkPail))]
[XmlInclude(typeof(Pan))]
[XmlInclude(typeof(Pickaxe))]
[XmlInclude(typeof(Shears))]
[XmlInclude(typeof(Slingshot))]
[XmlInclude(typeof(Wand))]
[XmlInclude(typeof(WateringCan))]
public abstract class Tool : Item
{
	public const int standardStaminaReduction = 2;

	public const int stone = 0;

	public const int copper = 1;

	public const int steel = 2;

	public const int gold = 3;

	public const int iridium = 4;

	public const int hammerSpriteIndex = 105;

	public const int wateringCanSpriteIndex = 273;

	public const int fishingRodSpriteIndex = 8;

	public const int wateringCanMenuIndex = 296;

	public const string weaponsTextureName = "TileSheets\\weapons";

	public static Texture2D weaponsTexture;

	[XmlElement("initialParentTileIndex")]
	public readonly NetInt initialParentTileIndex = new NetInt();

	[XmlElement("currentParentTileIndex")]
	public readonly NetInt currentParentTileIndex = new NetInt();

	[XmlElement("indexOfMenuItemView")]
	public readonly NetInt indexOfMenuItemView = new NetInt();

	[XmlElement("instantUse")]
	public readonly NetBool instantUse = new NetBool();

	[XmlElement("isEfficient")]
	public readonly NetBool isEfficient = new NetBool();

	[XmlElement("animationSpeedModifier")]
	public readonly NetFloat animationSpeedModifier = new NetFloat(1f);

	/// <summary>
	/// increments every swing. Not accurate for how many times the tool has been swung
	/// </summary>
	public int swingTicker = Game1.random.Next(999999);

	[XmlIgnore]
	private string _description;

	[XmlElement("upgradeLevel")]
	public readonly NetInt upgradeLevel = new NetInt();

	[XmlElement("numAttachmentSlots")]
	public readonly NetInt numAttachmentSlots = new NetInt();

	/// <summary>The last player who used this tool, if any.</summary>
	/// <remarks>Most code should use <see cref="M:StardewValley.Tool.getLastFarmerToUse" /> instead.</remarks>
	[XmlIgnore]
	public Farmer lastUser;

	public readonly NetObjectArray<Object> attachments = new NetObjectArray<Object>();

	/// <summary>The cached value for <see cref="P:StardewValley.Tool.DisplayName" />.</summary>
	[XmlIgnore]
	protected string displayName;

	[XmlElement("enchantments")]
	public readonly NetList<BaseEnchantment, NetRef<BaseEnchantment>> enchantments = new NetList<BaseEnchantment, NetRef<BaseEnchantment>>();

	[XmlElement("previousEnchantments")]
	public readonly NetStringList previousEnchantments = new NetStringList();

	/// <summary>Whether to play sounds when this tool is applied to a tile.</summary>
	/// <remarks>This should nearly always be true. It can be disabled for automated tools to avoid hitting audio instance limits.</remarks>
	[XmlIgnore]
	public bool PlayUseSounds = true;

	[XmlIgnore]
	public string description
	{
		get
		{
			if (_description == null)
			{
				_description = loadDescription();
			}
			return _description;
		}
		set
		{
			_description = value;
		}
	}

	/// <inheritdoc />
	public override string TypeDefinitionId { get; } = "(T)";

	/// <inheritdoc />
	[XmlIgnore]
	public override string DisplayName => loadDisplayName();

	public string Description => description;

	[XmlIgnore]
	public int CurrentParentTileIndex
	{
		get
		{
			return currentParentTileIndex.Value;
		}
		set
		{
			currentParentTileIndex.Set(value);
		}
	}

	public int InitialParentTileIndex
	{
		get
		{
			return initialParentTileIndex.Value;
		}
		set
		{
			initialParentTileIndex.Set(value);
		}
	}

	public int IndexOfMenuItemView
	{
		get
		{
			return indexOfMenuItemView.Value;
		}
		set
		{
			indexOfMenuItemView.Set(value);
		}
	}

	[XmlIgnore]
	public int UpgradeLevel
	{
		get
		{
			return upgradeLevel.Value;
		}
		set
		{
			upgradeLevel.Value = value;
		}
	}

	[XmlIgnore]
	public int AttachmentSlotsCount
	{
		get
		{
			return attachmentSlots();
		}
		set
		{
			numAttachmentSlots.Value = value;
			attachments.SetCount(value);
		}
	}

	public bool InstantUse
	{
		get
		{
			return instantUse.Value;
		}
		set
		{
			instantUse.Value = value;
		}
	}

	public bool IsEfficient
	{
		get
		{
			return isEfficient.Value;
		}
		set
		{
			isEfficient.Value = value;
		}
	}

	public float AnimationSpeedModifier
	{
		get
		{
			return animationSpeedModifier.Value;
		}
		set
		{
			animationSpeedModifier.Value = value;
		}
	}

	public Tool()
	{
		initNetFields();
		base.Category = -99;
	}

	public Tool(string name, int upgradeLevel, int initialParentTileIndex, int indexOfMenuItemView, bool stackable, int numAttachmentSlots = 0)
		: this()
	{
		Name = name ?? ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).InternalName;
		SetSpriteIndex(initialParentTileIndex);
		IndexOfMenuItemView = indexOfMenuItemView;
		AttachmentSlotsCount = Math.Max(0, numAttachmentSlots);
		base.Category = -99;
		UpgradeLevel = upgradeLevel;
	}

	/// <summary>Set the single sprite index to display for this tool.</summary>
	/// <param name="spriteIndex">The sprite index.</param>
	/// <remarks>This overrides upgrade level adjustments, so this should be called before setting the upgrade level for tools that have a dynamic sprite index.</remarks>
	public virtual void SetSpriteIndex(int spriteIndex)
	{
		InitialParentTileIndex = spriteIndex;
		IndexOfMenuItemView = spriteIndex;
		CurrentParentTileIndex = spriteIndex;
	}

	protected new virtual void initNetFields()
	{
		base.NetFields.SetOwner(this).AddField(initialParentTileIndex, "initialParentTileIndex").AddField(currentParentTileIndex, "currentParentTileIndex")
			.AddField(indexOfMenuItemView, "indexOfMenuItemView")
			.AddField(instantUse, "instantUse")
			.AddField(upgradeLevel, "upgradeLevel")
			.AddField(numAttachmentSlots, "numAttachmentSlots")
			.AddField(attachments, "attachments")
			.AddField(enchantments, "enchantments")
			.AddField(isEfficient, "isEfficient")
			.AddField(animationSpeedModifier, "animationSpeedModifier")
			.AddField(previousEnchantments, "previousEnchantments");
	}

	/// <inheritdoc />
	protected override void MigrateLegacyItemId()
	{
		base.ItemId = GetType().Name;
	}

	protected virtual string loadDisplayName()
	{
		return ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).DisplayName;
	}

	protected virtual string loadDescription()
	{
		return ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).Description;
	}

	/// <inheritdoc />
	public override bool CanBeLostOnDeath()
	{
		if (base.CanBeLostOnDeath())
		{
			return GetToolData()?.CanBeLostOnDeath ?? true;
		}
		return false;
	}

	/// <inheritdoc />
	public override string getCategoryName()
	{
		return Object.GetCategoryDisplayName(-99);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is Tool tool)
		{
			SetSpriteIndex(tool.InitialParentTileIndex);
			Name = source.Name;
			CurrentParentTileIndex = tool.CurrentParentTileIndex;
			IndexOfMenuItemView = tool.IndexOfMenuItemView;
			InstantUse = tool.InstantUse;
			IsEfficient = tool.IsEfficient;
			AnimationSpeedModifier = tool.AnimationSpeedModifier;
			UpgradeLevel = tool.UpgradeLevel;
			AttachmentSlotsCount = tool.AttachmentSlotsCount;
			CopyEnchantments(tool, this);
		}
	}

	/// <summary>Update this tool when it's created by upgrading a previous tool.</summary>
	/// <param name="other">The previous tool instance being upgraded into this tool.</param>
	public virtual void UpgradeFrom(Tool other)
	{
		CopyEnchantments(other, this);
	}

	/// <inheritdoc />
	public override Color getCategoryColor()
	{
		return Color.DarkSlateGray;
	}

	/// <summary>Get the underlying tool data from <c>Data/Tools</c>, if available.</summary>
	public ToolData GetToolData()
	{
		return ItemRegistry.GetData(base.QualifiedItemId)?.RawData as ToolData;
	}

	public virtual void draw(SpriteBatch b)
	{
		Farmer farmer = lastUser;
		if (farmer == null || farmer.toolPower.Value <= 0 || !lastUser.canReleaseTool || !lastUser.IsLocalPlayer)
		{
			return;
		}
		foreach (Vector2 item in tilesAffected(lastUser.GetToolLocation() / 64f, lastUser.toolPower.Value, lastUser))
		{
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(new Vector2((int)item.X * 64, (int)item.Y * 64)), new Rectangle(194, 388, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.01f);
		}
	}

	public override void drawAttachments(SpriteBatch b, int x, int y)
	{
		y += ((enchantments.Count > 0) ? 8 : 4);
		for (int i = 0; i < AttachmentSlotsCount; i++)
		{
			DrawAttachmentSlot(i, b, x, y + i * 68);
		}
	}

	/// <summary>Draw an attachment slot at the given position.</summary>
	/// <param name="slot">The attachment slot index.</param>
	/// <param name="b">The sprite batch being drawn.</param>
	/// <param name="x">The X position at which to draw the slot.</param>
	/// <param name="y">The Y position at which to draw the slot.</param>
	/// <remarks>This should draw a 64x64 slot.</remarks>
	protected virtual void DrawAttachmentSlot(int slot, SpriteBatch b, int x, int y)
	{
		Vector2 vector = new Vector2(x, y);
		GetAttachmentSlotSprite(slot, out var texture, out var sourceRect);
		b.Draw(texture, vector, sourceRect, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.86f);
		attachments[slot]?.drawInMenu(b, vector, 1f);
	}

	/// <summary>Get the sprite to draw for an attachment slot background.</summary>
	/// <param name="slot">The attachment slot index.</param>
	/// <param name="texture">The texture to draw.</param>
	/// <param name="sourceRect">The pixel area within the texture to draw.</param>
	protected virtual void GetAttachmentSlotSprite(int slot, out Texture2D texture, out Rectangle sourceRect)
	{
		texture = Game1.menuTexture;
		sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10);
	}

	public override void drawTooltip(SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
	{
		base.drawTooltip(spriteBatch, ref x, ref y, font, alpha, overrideText);
		foreach (BaseEnchantment enchantment in enchantments)
		{
			if (enchantment.ShouldBeDisplayed())
			{
				Utility.drawWithShadow(spriteBatch, Game1.mouseCursors2, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(127, 35, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
				Utility.drawTextWithShadow(spriteBatch, BaseEnchantment.hideEnchantmentName ? "???" : enchantment.GetDisplayName(), font, new Vector2(x + 16 + 52, y + 16 + 12), new Color(120, 0, 210) * 0.9f * alpha);
				y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
			}
		}
	}

	public override Point getExtraSpaceNeededForTooltipSpecialIcons(SpriteFont font, int minWidth, int horizontalBuffer, int startingHeight, StringBuilder descriptionText, string boldTitleText, int moneyAmountToDisplayAtBottom)
	{
		Point extraSpaceNeededForTooltipSpecialIcons = base.getExtraSpaceNeededForTooltipSpecialIcons(font, minWidth, horizontalBuffer, startingHeight, descriptionText, boldTitleText, moneyAmountToDisplayAtBottom);
		extraSpaceNeededForTooltipSpecialIcons.Y = startingHeight;
		foreach (BaseEnchantment enchantment in enchantments)
		{
			if (enchantment.ShouldBeDisplayed())
			{
				extraSpaceNeededForTooltipSpecialIcons.Y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
			}
		}
		return extraSpaceNeededForTooltipSpecialIcons;
	}

	public virtual void tickUpdate(GameTime time, Farmer who)
	{
	}

	public virtual bool isHeavyHitter()
	{
		if (!(this is MeleeWeapon) && !(this is Hoe) && !(this is Axe))
		{
			return this is Pickaxe;
		}
		return true;
	}

	/// <summary>Get whether this is a scythe tool.</summary>
	public virtual bool isScythe()
	{
		return false;
	}

	public virtual void Update(int direction, int farmerMotionFrame, Farmer who)
	{
		int num = 0;
		if (!(this is WateringCan))
		{
			if (this is FishingRod)
			{
				switch (direction)
				{
				case 0:
					num = 3;
					break;
				case 1:
					num = 0;
					break;
				case 3:
					num = 0;
					break;
				}
			}
			else
			{
				switch (direction)
				{
				case 0:
					num = 3;
					break;
				case 1:
					num = 2;
					break;
				case 3:
					num = 2;
					break;
				}
			}
		}
		else
		{
			switch (direction)
			{
			case 0:
				num = 4;
				break;
			case 1:
				num = 2;
				break;
			case 2:
				num = 0;
				break;
			case 3:
				num = 2;
				break;
			}
		}
		if (base.QualifiedItemId != "(T)WateringCan")
		{
			if (farmerMotionFrame < 1)
			{
				CurrentParentTileIndex = InitialParentTileIndex;
			}
			else if (who.FacingDirection == 0 || (who.FacingDirection == 2 && farmerMotionFrame >= 2))
			{
				CurrentParentTileIndex = InitialParentTileIndex + 1;
			}
		}
		else if (farmerMotionFrame < 5 || direction == 0)
		{
			CurrentParentTileIndex = InitialParentTileIndex;
		}
		else
		{
			CurrentParentTileIndex = InitialParentTileIndex + 1;
		}
		CurrentParentTileIndex += num;
	}

	/// <inheritdoc />
	public override int salePrice(bool ignoreProfitMargins = false)
	{
		ToolData toolData = GetToolData();
		if (toolData == null || toolData.SalePrice < 0)
		{
			return base.salePrice(ignoreProfitMargins);
		}
		return toolData.SalePrice;
	}

	public override int attachmentSlots()
	{
		return numAttachmentSlots.Value;
	}

	/// <summary>Get the last player who used this tool, if any.</summary>
	public Farmer getLastFarmerToUse()
	{
		return lastUser;
	}

	public virtual void leftClick(Farmer who)
	{
	}

	public virtual void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
	{
		lastUser = who;
		Game1.recentMultiplayerRandom = Utility.CreateRandom((short)Game1.random.Next(-32768, 32768));
		if (isHeavyHitter() && !(this is MeleeWeapon))
		{
			Rumble.rumble(0.1f + (float)(Game1.random.NextDouble() / 4.0), 100 + Game1.random.Next(50));
			location.damageMonster(new Rectangle(x - 32, y - 32, 64, 64), upgradeLevel.Value + 1, (upgradeLevel.Value + 1) * 3, isBomb: false, who);
		}
		if (this is MeleeWeapon meleeWeapon && (!who.UsingTool || Game1.mouseClickPolling >= 50 || meleeWeapon.type.Value == 1 || !(meleeWeapon.ItemId != "47") || MeleeWeapon.timedHitTimer > 0 || who.FarmerSprite.currentAnimationIndex != 5 || !(who.FarmerSprite.timer < who.FarmerSprite.interval / 4f)))
		{
			if (meleeWeapon.type.Value == 2 && meleeWeapon.isOnSpecial)
			{
				meleeWeapon.triggerClubFunction(who);
			}
			else if (who.FarmerSprite.currentAnimationIndex > 0)
			{
				MeleeWeapon.timedHitTimer = 500;
			}
		}
	}

	public virtual void endUsing(GameLocation location, Farmer who)
	{
		swingTicker++;
		who.stopJittering();
		who.canReleaseTool = false;
		int num = ((!(who.Stamina <= 0f)) ? 1 : 2);
		if (Game1.isAnyGamePadButtonBeingPressed() || !who.IsLocalPlayer)
		{
			who.lastClick = who.GetToolLocation();
		}
		if (this is WateringCan wateringCan)
		{
			if (wateringCan.WaterLeft > 0 && who.ShouldHandleAnimationSound() && PlayUseSounds)
			{
				who.playNearbySoundLocal("wateringCan");
			}
			switch (who.FacingDirection)
			{
			case 2:
				((FarmerSprite)who.Sprite).animateOnce(164, 125f * (float)num, 3);
				break;
			case 1:
				((FarmerSprite)who.Sprite).animateOnce(172, 125f * (float)num, 3);
				break;
			case 0:
				((FarmerSprite)who.Sprite).animateOnce(180, 125f * (float)num, 3);
				break;
			case 3:
				((FarmerSprite)who.Sprite).animateOnce(188, 125f * (float)num, 3);
				break;
			}
		}
		else if (this is FishingRod fishingRod && who.IsLocalPlayer && Game1.activeClickableMenu == null)
		{
			if (!fishingRod.hit)
			{
				DoFunction(who.currentLocation, (int)who.lastClick.X, (int)who.lastClick.Y, 1, who);
			}
		}
		else if (!(this is MeleeWeapon) && !(this is Pan) && !(this is Shears) && !(this is MilkPail) && !(this is Slingshot))
		{
			switch (who.FacingDirection)
			{
			case 0:
				((FarmerSprite)who.Sprite).animateOnce(176, 60f * (float)num, 8);
				break;
			case 1:
				((FarmerSprite)who.Sprite).animateOnce(168, 60f * (float)num, 8);
				break;
			case 2:
				((FarmerSprite)who.Sprite).animateOnce(160, 60f * (float)num, 8);
				break;
			case 3:
				((FarmerSprite)who.Sprite).animateOnce(184, 60f * (float)num, 8);
				break;
			}
		}
	}

	public virtual bool beginUsing(GameLocation location, int x, int y, Farmer who)
	{
		lastUser = who;
		if (!instantUse.Value)
		{
			who.Halt();
			Update(who.FacingDirection, 0, who);
			if ((!(this is FishingRod) && upgradeLevel.Value <= 0 && !(this is MeleeWeapon)) || this is Pickaxe)
			{
				who.EndUsingTool();
				return true;
			}
		}
		if (instantUse.Value)
		{
			Game1.toolAnimationDone(who);
			who.CanMove = true;
			who.canReleaseTool = false;
			who.UsingTool = false;
		}
		else if (this is WateringCan && location.CanRefillWateringCanOnTile((int)who.GetToolLocation().X / 64, (int)who.GetToolLocation().Y / 64))
		{
			switch (who.FacingDirection)
			{
			case 2:
				((FarmerSprite)who.Sprite).animateOnce(166, 250f, 2);
				Update(2, 1, who);
				break;
			case 1:
				((FarmerSprite)who.Sprite).animateOnce(174, 250f, 2);
				Update(1, 0, who);
				break;
			case 0:
				((FarmerSprite)who.Sprite).animateOnce(182, 250f, 2);
				Update(0, 1, who);
				break;
			case 3:
				((FarmerSprite)who.Sprite).animateOnce(190, 250f, 2);
				Update(3, 0, who);
				break;
			}
			who.canReleaseTool = false;
		}
		else if (this is WateringCan { WaterLeft: <=0 })
		{
			Game1.toolAnimationDone(who);
			who.CanMove = true;
			who.canReleaseTool = false;
		}
		else if (this is WateringCan)
		{
			who.jitterStrength = 0.25f;
			switch (who.FacingDirection)
			{
			case 0:
				who.FarmerSprite.setCurrentFrame(180);
				Update(0, 0, who);
				break;
			case 1:
				who.FarmerSprite.setCurrentFrame(172);
				Update(1, 0, who);
				break;
			case 2:
				who.FarmerSprite.setCurrentFrame(164);
				Update(2, 0, who);
				break;
			case 3:
				who.FarmerSprite.setCurrentFrame(188);
				Update(3, 0, who);
				break;
			}
		}
		else if (this is FishingRod)
		{
			switch (who.FacingDirection)
			{
			case 0:
				((FarmerSprite)who.Sprite).animateOnce(295, 35f, 8, FishingRod.endOfAnimationBehavior);
				Update(0, 0, who);
				break;
			case 1:
				((FarmerSprite)who.Sprite).animateOnce(296, 35f, 8, FishingRod.endOfAnimationBehavior);
				Update(1, 0, who);
				break;
			case 2:
				((FarmerSprite)who.Sprite).animateOnce(297, 35f, 8, FishingRod.endOfAnimationBehavior);
				Update(2, 0, who);
				break;
			case 3:
				((FarmerSprite)who.Sprite).animateOnce(298, 35f, 8, FishingRod.endOfAnimationBehavior);
				Update(3, 0, who);
				break;
			}
			who.canReleaseTool = false;
		}
		else if (this is MeleeWeapon)
		{
			((MeleeWeapon)this).setFarmerAnimating(who);
		}
		else
		{
			switch (who.FacingDirection)
			{
			case 0:
				who.FarmerSprite.setCurrentFrame(176);
				Update(0, 0, who);
				break;
			case 1:
				who.FarmerSprite.setCurrentFrame(168);
				Update(1, 0, who);
				break;
			case 2:
				who.FarmerSprite.setCurrentFrame(160);
				Update(2, 0, who);
				break;
			case 3:
				who.FarmerSprite.setCurrentFrame(184);
				Update(3, 0, who);
				break;
			}
		}
		return false;
	}

	public virtual bool onRelease(GameLocation location, int x, int y, Farmer who)
	{
		return false;
	}

	public override bool canBeDropped()
	{
		return false;
	}

	/// <summary>Get whether an item can be added to or removed from an attachment slot.</summary>
	/// <param name="o">The item to attach, or <c>null</c> to remove an attached item.</param>
	public virtual bool canThisBeAttached(Object o)
	{
		NetObjectArray<Object> netObjectArray = attachments;
		if (netObjectArray != null && netObjectArray.Count > 0)
		{
			if (o == null)
			{
				return true;
			}
			for (int i = 0; i < attachments.Length; i++)
			{
				if (canThisBeAttached(o, i))
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Get whether an item can be added to or removed from an attachment slot.</summary>
	/// <param name="o">The item to attach.</param>
	/// <param name="slot">The slot index. This is always a valid index when the method is called.</param>
	protected virtual bool canThisBeAttached(Object o, int slot)
	{
		return true;
	}

	/// <summary>Add an item to or remove it from an attachment slot.</summary>
	/// <param name="o">The item to attach, or <c>null</c> to remove an attached item.</param>
	public virtual Object attach(Object o)
	{
		if (o == null)
		{
			for (int i = 0; i < attachments.Length; i++)
			{
				Object obj = attachments[i];
				if (obj != null)
				{
					attachments[i] = null;
					Game1.playSound("dwop");
					return obj;
				}
			}
			return null;
		}
		int num = o.Stack;
		for (int j = 0; j < attachments.Length; j++)
		{
			if (!canThisBeAttached(o, j))
			{
				continue;
			}
			Object obj2 = attachments[j];
			if (obj2 == null)
			{
				attachments[j] = o;
				o = null;
				break;
			}
			if (obj2.canStackWith(o))
			{
				int amount = o.Stack - obj2.addToStack(o);
				if (o.ConsumeStack(amount) == null)
				{
					o = null;
					break;
				}
			}
		}
		if (o == null || o.Stack != num)
		{
			Game1.playSound("button1");
			return o;
		}
		for (int k = 0; k < attachments.Length; k++)
		{
			Object obj3 = attachments[k];
			attachments[k] = null;
			if (canThisBeAttached(o, k))
			{
				attachments[k] = o;
				Game1.playSound("button1");
				return obj3;
			}
			attachments[k] = obj3;
		}
		return o;
	}

	public virtual void actionWhenClaimed()
	{
		if (this is GenericTool)
		{
			int value = indexOfMenuItemView.Value;
			if ((uint)(value - 13) <= 3u)
			{
				Game1.player.trashCanLevel++;
			}
		}
	}

	public override bool CanBuyItem(Farmer who)
	{
		if (Game1.player.toolBeingUpgraded.Value == null && (this is Axe || this is Pickaxe || this is Hoe || this is WateringCan || (this is GenericTool && indexOfMenuItemView.Value >= 13 && indexOfMenuItemView.Value <= 16)))
		{
			return true;
		}
		return base.CanBuyItem(who);
	}

	/// <inheritdoc />
	public override bool actionWhenPurchased(string shopId)
	{
		if (shopId == "ClintUpgrade" && Game1.player.toolBeingUpgraded.Value == null)
		{
			if (this is Axe || this is Pickaxe || this is Hoe || this is WateringCan || this is Pan)
			{
				string text = ShopBuilder.GetToolUpgradeData(GetToolData(), Game1.player)?.RequireToolId;
				if (text != null)
				{
					Item item = Game1.player.Items.GetById(text).FirstOrDefault();
					Game1.player.removeItemFromInventory(item);
					if (item is Tool other)
					{
						UpgradeFrom(other);
					}
				}
				Game1.player.toolBeingUpgraded.Value = (Tool)getOne();
				Game1.player.daysLeftForToolUpgrade.Value = 2;
				Game1.playSound("parry");
				Game1.exitActiveMenu();
				Game1.DrawDialogue(Game1.getCharacterFromName("Clint"), "Strings\\StringsFromCSFiles:Tool.cs.14317");
				return true;
			}
			if (this is GenericTool)
			{
				int value = indexOfMenuItemView.Value;
				if ((uint)(value - 13) <= 3u)
				{
					Game1.player.toolBeingUpgraded.Value = (Tool)getOne();
					Game1.player.daysLeftForToolUpgrade.Value = 2;
					Game1.playSound("parry");
					Game1.exitActiveMenu();
					Game1.DrawDialogue(Game1.getCharacterFromName("Clint"), "Strings\\StringsFromCSFiles:Tool.cs.14317");
					return true;
				}
			}
		}
		return base.actionWhenPurchased(shopId);
	}

	protected List<Vector2> tilesAffected(Vector2 tileLocation, int power, Farmer who)
	{
		power++;
		List<Vector2> list = new List<Vector2>();
		list.Add(tileLocation);
		Vector2 vector = Vector2.Zero;
		switch (who.FacingDirection)
		{
		case 0:
			if (power >= 6)
			{
				vector = new Vector2(tileLocation.X, tileLocation.Y - 2f);
				break;
			}
			if (power >= 2)
			{
				list.Add(tileLocation + new Vector2(0f, -1f));
				list.Add(tileLocation + new Vector2(0f, -2f));
			}
			if (power >= 3)
			{
				list.Add(tileLocation + new Vector2(0f, -3f));
				list.Add(tileLocation + new Vector2(0f, -4f));
			}
			if (power >= 4)
			{
				list.RemoveAt(list.Count - 1);
				list.RemoveAt(list.Count - 1);
				list.Add(tileLocation + new Vector2(1f, -2f));
				list.Add(tileLocation + new Vector2(1f, -1f));
				list.Add(tileLocation + new Vector2(1f, 0f));
				list.Add(tileLocation + new Vector2(-1f, -2f));
				list.Add(tileLocation + new Vector2(-1f, -1f));
				list.Add(tileLocation + new Vector2(-1f, 0f));
			}
			if (power >= 5)
			{
				for (int num3 = list.Count - 1; num3 >= 0; num3--)
				{
					list.Add(list[num3] + new Vector2(0f, -3f));
				}
			}
			break;
		case 1:
			if (power >= 6)
			{
				vector = new Vector2(tileLocation.X + 2f, tileLocation.Y);
				break;
			}
			if (power >= 2)
			{
				list.Add(tileLocation + new Vector2(1f, 0f));
				list.Add(tileLocation + new Vector2(2f, 0f));
			}
			if (power >= 3)
			{
				list.Add(tileLocation + new Vector2(3f, 0f));
				list.Add(tileLocation + new Vector2(4f, 0f));
			}
			if (power >= 4)
			{
				list.RemoveAt(list.Count - 1);
				list.RemoveAt(list.Count - 1);
				list.Add(tileLocation + new Vector2(0f, -1f));
				list.Add(tileLocation + new Vector2(1f, -1f));
				list.Add(tileLocation + new Vector2(2f, -1f));
				list.Add(tileLocation + new Vector2(0f, 1f));
				list.Add(tileLocation + new Vector2(1f, 1f));
				list.Add(tileLocation + new Vector2(2f, 1f));
			}
			if (power >= 5)
			{
				for (int num2 = list.Count - 1; num2 >= 0; num2--)
				{
					list.Add(list[num2] + new Vector2(3f, 0f));
				}
			}
			break;
		case 2:
			if (power >= 6)
			{
				vector = new Vector2(tileLocation.X, tileLocation.Y + 2f);
				break;
			}
			if (power >= 2)
			{
				list.Add(tileLocation + new Vector2(0f, 1f));
				list.Add(tileLocation + new Vector2(0f, 2f));
			}
			if (power >= 3)
			{
				list.Add(tileLocation + new Vector2(0f, 3f));
				list.Add(tileLocation + new Vector2(0f, 4f));
			}
			if (power >= 4)
			{
				list.RemoveAt(list.Count - 1);
				list.RemoveAt(list.Count - 1);
				list.Add(tileLocation + new Vector2(1f, 2f));
				list.Add(tileLocation + new Vector2(1f, 1f));
				list.Add(tileLocation + new Vector2(1f, 0f));
				list.Add(tileLocation + new Vector2(-1f, 2f));
				list.Add(tileLocation + new Vector2(-1f, 1f));
				list.Add(tileLocation + new Vector2(-1f, 0f));
			}
			if (power >= 5)
			{
				for (int num4 = list.Count - 1; num4 >= 0; num4--)
				{
					list.Add(list[num4] + new Vector2(0f, 3f));
				}
			}
			break;
		case 3:
			if (power >= 6)
			{
				vector = new Vector2(tileLocation.X - 2f, tileLocation.Y);
				break;
			}
			if (power >= 2)
			{
				list.Add(tileLocation + new Vector2(-1f, 0f));
				list.Add(tileLocation + new Vector2(-2f, 0f));
			}
			if (power >= 3)
			{
				list.Add(tileLocation + new Vector2(-3f, 0f));
				list.Add(tileLocation + new Vector2(-4f, 0f));
			}
			if (power >= 4)
			{
				list.RemoveAt(list.Count - 1);
				list.RemoveAt(list.Count - 1);
				list.Add(tileLocation + new Vector2(0f, -1f));
				list.Add(tileLocation + new Vector2(-1f, -1f));
				list.Add(tileLocation + new Vector2(-2f, -1f));
				list.Add(tileLocation + new Vector2(0f, 1f));
				list.Add(tileLocation + new Vector2(-1f, 1f));
				list.Add(tileLocation + new Vector2(-2f, 1f));
			}
			if (power >= 5)
			{
				for (int num = list.Count - 1; num >= 0; num--)
				{
					list.Add(list[num] + new Vector2(-3f, 0f));
				}
			}
			break;
		}
		if (power >= 6)
		{
			list.Clear();
			for (int i = (int)vector.X - 2; (float)i <= vector.X + 2f; i++)
			{
				for (int j = (int)vector.Y - 2; (float)j <= vector.Y + 2f; j++)
				{
					list.Add(new Vector2(i, j));
				}
			}
		}
		return list;
	}

	public virtual bool doesShowTileLocationMarker()
	{
		return true;
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
		if (StardewValley.Pokemon.PokemonItemManager.TryGetCustomTexture(base.QualifiedItemId, out Texture2D customTex, out Rectangle customRect))
		{
			spriteBatch.Draw(customTex, location + new Vector2(32f, 32f), customRect, color * transparency, 0f, new Vector2(customRect.Width / 2f, customRect.Height / 2f), 2.5f * scaleSize, SpriteEffects.None, layerDepth);
			DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
			return;
		}
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		spriteBatch.Draw(dataOrErrorItem.GetTexture(), location + new Vector2(32f, 32f), dataOrErrorItem.GetSourceRect(), color * transparency, 0f, new Vector2(8f, 8f), 4f * scaleSize, SpriteEffects.None, layerDepth);
		DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
	}

	public override bool isPlaceable()
	{
		return false;
	}

	public override int maximumStackSize()
	{
		return 1;
	}

	public override string getDescription()
	{
		return Game1.parseText(description, Game1.smallFont, getDescriptionWidth());
	}

	protected override int getDescriptionWidth()
	{
		int num = base.getDescriptionWidth();
		foreach (BaseEnchantment enchantment in enchantments)
		{
			num = Math.Max(num, (int)(Game1.smallFont.MeasureString(enchantment.GetDisplayName()).X + 128f));
		}
		return num;
	}

	public virtual void ClearEnchantments()
	{
		for (int num = enchantments.Count - 1; num >= 0; num--)
		{
			enchantments[num].UnapplyTo(this);
		}
		enchantments.Clear();
	}

	public virtual int GetMaxForges()
	{
		return 0;
	}

	public virtual bool CanAddEnchantment(BaseEnchantment enchantment)
	{
		if (!enchantment.IsForge() && !enchantment.IsSecondaryEnchantment())
		{
			return true;
		}
		if (GetTotalForgeLevels() >= GetMaxForges() && !enchantment.IsSecondaryEnchantment())
		{
			return false;
		}
		if (enchantment != null)
		{
			foreach (BaseEnchantment enchantment2 in enchantments)
			{
				if (enchantment.GetType() == enchantment2.GetType())
				{
					if (enchantment2.GetMaximumLevel() < 0 || enchantment2.GetLevel() < enchantment2.GetMaximumLevel())
					{
						return true;
					}
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public virtual void CopyEnchantments(Tool source, Tool destination)
	{
		foreach (BaseEnchantment enchantment in source.enchantments)
		{
			destination.enchantments.Add(enchantment.GetOne());
			enchantment.GetOne().ApplyTo(destination);
		}
		destination.previousEnchantments.Clear();
		destination.previousEnchantments.AddRange(source.previousEnchantments);
	}

	public int GetTotalForgeLevels(bool for_unforge = false)
	{
		int num = 0;
		foreach (BaseEnchantment enchantment in enchantments)
		{
			if (enchantment is DiamondEnchantment)
			{
				if (for_unforge)
				{
					return num;
				}
			}
			else if (enchantment.IsForge())
			{
				num += enchantment.GetLevel();
			}
		}
		return num;
	}

	public virtual bool AddEnchantment(BaseEnchantment enchantment)
	{
		if (enchantment != null)
		{
			if (this is MeleeWeapon && (enchantment.IsForge() || enchantment.IsSecondaryEnchantment()))
			{
				foreach (BaseEnchantment enchantment2 in enchantments)
				{
					if (enchantment.GetType() == enchantment2.GetType())
					{
						if (enchantment2.GetMaximumLevel() < 0 || enchantment2.GetLevel() < enchantment2.GetMaximumLevel())
						{
							enchantment2.SetLevel(this, enchantment2.GetLevel() + 1);
							return true;
						}
						return false;
					}
				}
				enchantments.Add(enchantment);
				enchantment.ApplyTo(this, lastUser);
				return true;
			}
			for (int num = enchantments.Count - 1; num >= 0; num--)
			{
				BaseEnchantment baseEnchantment = enchantments[num];
				if (!baseEnchantment.IsForge() && !baseEnchantment.IsSecondaryEnchantment())
				{
					baseEnchantment.UnapplyTo(this);
					enchantments.RemoveAt(num);
				}
			}
			enchantments.Add(enchantment);
			enchantment.ApplyTo(this, lastUser);
			return true;
		}
		return false;
	}

	public bool hasEnchantmentOfType<T>()
	{
		foreach (BaseEnchantment enchantment in enchantments)
		{
			if (enchantment is T)
			{
				return true;
			}
		}
		return false;
	}

	public virtual void RemoveEnchantment(BaseEnchantment enchantment)
	{
		if (enchantment != null)
		{
			enchantments.Remove(enchantment);
			enchantment.UnapplyTo(this, lastUser);
		}
	}

	public override void actionWhenBeingHeld(Farmer who)
	{
		base.actionWhenBeingHeld(who);
		if (!who.IsLocalPlayer)
		{
			return;
		}
		foreach (BaseEnchantment enchantment in enchantments)
		{
			enchantment.OnEquip(who);
		}
	}

	public override void actionWhenStopBeingHeld(Farmer who)
	{
		base.actionWhenStopBeingHeld(who);
		if (who.UsingTool)
		{
			who.UsingTool = false;
			if (who.FarmerSprite.PauseForSingleAnimation)
			{
				who.FarmerSprite.PauseForSingleAnimation = false;
			}
		}
		if (!who.IsLocalPlayer)
		{
			return;
		}
		foreach (BaseEnchantment enchantment in enchantments)
		{
			enchantment.OnUnequip(who);
		}
	}

	public virtual bool CanUseOnStandingTile()
	{
		return false;
	}

	public override void AddEquipmentEffects(BuffEffects effects)
	{
		base.AddEquipmentEffects(effects);
		if (hasEnchantmentOfType<MasterEnchantment>())
		{
			effects.FishingLevel.Value += 1f;
		}
	}

	public virtual bool CanForge(Item item)
	{
		BaseEnchantment enchantmentFromItem = BaseEnchantment.GetEnchantmentFromItem(this, item);
		if (enchantmentFromItem != null && CanAddEnchantment(enchantmentFromItem))
		{
			return true;
		}
		if (item != null && item.QualifiedItemId == "(O)852" && this is MeleeWeapon meleeWeapon && meleeWeapon.getItemLevel() < 15 && !Name.Contains("Galaxy"))
		{
			return true;
		}
		return false;
	}

	public T GetEnchantmentOfType<T>() where T : BaseEnchantment
	{
		foreach (BaseEnchantment enchantment in enchantments)
		{
			if (enchantment.GetType() == typeof(T))
			{
				return enchantment as T;
			}
		}
		return null;
	}

	public int GetEnchantmentLevel<T>() where T : BaseEnchantment
	{
		int num = 0;
		foreach (BaseEnchantment enchantment in enchantments)
		{
			if (enchantment.GetType() == typeof(T))
			{
				num += enchantment.GetLevel();
			}
		}
		return num;
	}

	public virtual bool Forge(Item item, bool count_towards_stats = false)
	{
		BaseEnchantment enchantmentFromItem = BaseEnchantment.GetEnchantmentFromItem(this, item);
		if (enchantmentFromItem != null)
		{
			if (AddEnchantment(enchantmentFromItem))
			{
				if (!(enchantmentFromItem is DiamondEnchantment))
				{
					if (enchantmentFromItem is GalaxySoulEnchantment && this is MeleeWeapon meleeWeapon && meleeWeapon.isGalaxyWeapon() && meleeWeapon.GetEnchantmentLevel<GalaxySoulEnchantment>() >= 3)
					{
						string text = null;
						switch (base.QualifiedItemId)
						{
						case "(W)4":
							text = "62";
							break;
						case "(W)29":
							text = "63";
							break;
						case "(W)23":
							text = "64";
							break;
						}
						if (text != null)
						{
							meleeWeapon.transform(text);
							if (count_towards_stats)
							{
								DelayedAction.playSoundAfterDelay("discoverMineral", 400);
								Game1.multiplayer.globalChatInfoMessage("InfinityWeapon", Game1.player.name.Value, TokenStringBuilder.ItemNameFor(this));
								Game1.getAchievement(42);
							}
						}
						GalaxySoulEnchantment enchantmentOfType = GetEnchantmentOfType<GalaxySoulEnchantment>();
						if (enchantmentOfType != null)
						{
							RemoveEnchantment(enchantmentOfType);
						}
					}
				}
				else
				{
					int num = GetMaxForges() - GetTotalForgeLevels();
					List<int> list = new List<int>();
					if (!hasEnchantmentOfType<EmeraldEnchantment>())
					{
						list.Add(0);
					}
					if (!hasEnchantmentOfType<AquamarineEnchantment>())
					{
						list.Add(1);
					}
					if (!hasEnchantmentOfType<RubyEnchantment>())
					{
						list.Add(2);
					}
					if (!hasEnchantmentOfType<AmethystEnchantment>())
					{
						list.Add(3);
					}
					if (!hasEnchantmentOfType<TopazEnchantment>())
					{
						list.Add(4);
					}
					if (!hasEnchantmentOfType<JadeEnchantment>())
					{
						list.Add(5);
					}
					for (int i = 0; i < num; i++)
					{
						if (list.Count == 0)
						{
							break;
						}
						int index = Game1.random.Next(list.Count);
						int num2 = list[index];
						list.RemoveAt(index);
						switch (num2)
						{
						case 0:
							AddEnchantment(new EmeraldEnchantment());
							break;
						case 1:
							AddEnchantment(new AquamarineEnchantment());
							break;
						case 2:
							AddEnchantment(new RubyEnchantment());
							break;
						case 3:
							AddEnchantment(new AmethystEnchantment());
							break;
						case 4:
							AddEnchantment(new TopazEnchantment());
							break;
						case 5:
							AddEnchantment(new JadeEnchantment());
							break;
						}
					}
				}
				if (count_towards_stats && !enchantmentFromItem.IsForge())
				{
					previousEnchantments.Insert(0, enchantmentFromItem.GetName());
					while (previousEnchantments.Count > 2)
					{
						previousEnchantments.RemoveAt(previousEnchantments.Count - 1);
					}
					Game1.stats.Increment("timesEnchanted");
				}
				return true;
			}
		}
		else if (item.QualifiedItemId == "(O)852" && this is MeleeWeapon meleeWeapon2)
		{
			List<BaseEnchantment> oldEnchantments = new List<BaseEnchantment>();
			meleeWeapon2.enchantments.RemoveWhere(delegate(BaseEnchantment curEnchantment)
			{
				if (curEnchantment.IsSecondaryEnchantment() && !(curEnchantment is GalaxySoulEnchantment))
				{
					oldEnchantments.Add(curEnchantment);
					return true;
				}
				return false;
			});
			MeleeWeapon.attemptAddRandomInnateEnchantment(meleeWeapon2, Game1.random, force: true, oldEnchantments);
			return true;
		}
		return false;
	}
}
