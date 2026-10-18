using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Buffs;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData.Weapons;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Projectiles;

namespace StardewValley.Tools;

public class MeleeWeapon : Tool
{
	public const int defenseCooldownTime = 1500;

	public const int daggerCooldownTime = 3000;

	public const int clubCooldownTime = 6000;

	public const int millisecondsPerSpeedPoint = 40;

	public const int defaultSpeed = 400;

	public const int stabbingSword = 0;

	public const int dagger = 1;

	public const int club = 2;

	public const int defenseSword = 3;

	public const int baseClubSpeed = -8;

	public const string scytheId = "47";

	public const string goldenScytheId = "53";

	public const string iridiumScytheID = "66";

	public const string galaxySwordId = "4";

	public const int MAX_FORGES = 3;

	[XmlElement("type")]
	public readonly NetInt type = new NetInt();

	[XmlElement("minDamage")]
	public readonly NetInt minDamage = new NetInt();

	[XmlElement("maxDamage")]
	public readonly NetInt maxDamage = new NetInt();

	[XmlElement("speed")]
	public readonly NetInt speed = new NetInt();

	[XmlElement("addedPrecision")]
	public readonly NetInt addedPrecision = new NetInt();

	[XmlElement("addedDefense")]
	public readonly NetInt addedDefense = new NetInt();

	[XmlElement("addedAreaOfEffect")]
	public readonly NetInt addedAreaOfEffect = new NetInt();

	[XmlElement("knockback")]
	public readonly NetFloat knockback = new NetFloat();

	[XmlElement("critChance")]
	public readonly NetFloat critChance = new NetFloat();

	[XmlElement("critMultiplier")]
	public readonly NetFloat critMultiplier = new NetFloat();

	/// <summary>The qualified item ID for the item whose appearance to use, or <c>null</c> to use the weapon's default appearance.</summary>
	[XmlElement("appearance")]
	public readonly NetString appearance = new NetString(null);

	public bool isOnSpecial;

	public static int defenseCooldown;

	public static int attackSwordCooldown;

	public static int daggerCooldown;

	public static int clubCooldown;

	public static int daggerHitsLeft;

	public static int timedHitTimer;

	private static float addedSwordScale = 0f;

	private static float addedClubScale = 0f;

	private static float addedDaggerScale = 0f;

	private float swipeSpeed;

	[XmlIgnore]
	public Rectangle mostRecentArea;

	[XmlIgnore]
	private readonly NetEvent0 animateSpecialMoveEvent = new NetEvent0();

	[XmlIgnore]
	private readonly NetEvent0 defenseSwordEvent = new NetEvent0();

	[XmlIgnore]
	private readonly NetEvent1Field<int, NetInt> daggerEvent = new NetEvent1Field<int, NetInt>();

	private WeaponData cachedData;

	private bool anotherClick;

	private static Vector2 center = new Vector2(1f, 15f);

	/// <inheritdoc />
	public override string TypeDefinitionId { get; } = "(W)";

	public MeleeWeapon()
	{
		base.Category = -98;
	}

	public MeleeWeapon(string itemId)
		: this()
	{
		itemId = ValidateUnqualifiedItemId(itemId);
		base.ItemId = itemId;
		Stack = 1;
		ReloadData();
	}

	protected void ReloadData()
	{
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		if (TryGetData(itemId.Value, out var data))
		{
			cachedData = data;
			Name = data.Name ?? dataOrErrorItem.InternalName;
			minDamage.Value = data.MinDamage;
			maxDamage.Value = data.MaxDamage;
			knockback.Value = data.Knockback;
			speed.Value = data.Speed;
			addedPrecision.Value = data.Precision;
			addedDefense.Value = data.Defense;
			type.Value = data.Type;
			addedAreaOfEffect.Value = data.AreaOfEffect;
			critChance.Value = data.CritChance;
			critMultiplier.Value = data.CritMultiplier;
			if (type.Value == 0)
			{
				type.Value = 3;
			}
		}
		else
		{
			Name = "Error Item";
		}
		base.InitialParentTileIndex = dataOrErrorItem.SpriteIndex;
		base.CurrentParentTileIndex = dataOrErrorItem.SpriteIndex;
		base.IndexOfMenuItemView = dataOrErrorItem.SpriteIndex;
		base.Category = (isScythe() ? (-99) : (-98));
	}

	/// <inheritdoc />
	protected override void MigrateLegacyItemId()
	{
		base.ItemId = base.InitialParentTileIndex.ToString();
	}

	/// <summary>Get the weapon's data from <see cref="F:StardewValley.Game1.weaponData" />, if found.</summary>
	public WeaponData GetData()
	{
		if (cachedData == null)
		{
			TryGetData(base.ItemId, out cachedData);
		}
		return cachedData;
	}

	/// <summary>Try to get a weapon's data from <see cref="F:StardewValley.Game1.weaponData" />.</summary>
	/// <param name="itemId">The weapon's unqualified item ID (i.e. the key in <see cref="F:StardewValley.Game1.weaponData" />).</param>
	/// <param name="data">The weapon data, if found.</param>
	/// <returns>Returns whether the crop data was found.</returns>
	public static bool TryGetData(string itemId, out WeaponData data)
	{
		if (itemId == null)
		{
			data = null;
			return false;
		}
		return Game1.weaponData.TryGetValue(itemId, out data);
	}

	/// <inheritdoc />
	public override bool CanBeLostOnDeath()
	{
		if (base.CanBeLostOnDeath())
		{
			return GetData()?.CanBeLostOnDeath ?? true;
		}
		return false;
	}

	public override void AddEquipmentEffects(BuffEffects effects)
	{
		base.AddEquipmentEffects(effects);
		effects.Defense.Value += addedDefense.Value;
		foreach (BaseEnchantment enchantment in enchantments)
		{
			enchantment.AddEquipmentEffects(effects);
		}
	}

	public override int GetMaxForges()
	{
		return 3;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new MeleeWeapon(base.ItemId);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is MeleeWeapon meleeWeapon)
		{
			appearance.Value = meleeWeapon.appearance.Value;
			base.IndexOfMenuItemView = meleeWeapon.IndexOfMenuItemView;
		}
	}

	protected override string loadDisplayName()
	{
		return ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).DisplayName;
	}

	protected override string loadDescription()
	{
		return ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).Description;
	}

	/// <inheritdoc />
	public override string getCategoryName()
	{
		if (!isScythe())
		{
			int value = type.Value;
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs.14303", getItemLevel(), Game1.content.LoadString(value switch
			{
				1 => "Strings\\StringsFromCSFiles:Tool.cs.14304", 
				2 => "Strings\\StringsFromCSFiles:Tool.cs.14305", 
				_ => "Strings\\StringsFromCSFiles:Tool.cs.14306", 
			}));
		}
		return base.getCategoryName();
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(type, "type").AddField(minDamage, "minDamage").AddField(maxDamage, "maxDamage")
			.AddField(speed, "speed")
			.AddField(addedPrecision, "addedPrecision")
			.AddField(addedDefense, "addedDefense")
			.AddField(addedAreaOfEffect, "addedAreaOfEffect")
			.AddField(knockback, "knockback")
			.AddField(critChance, "critChance")
			.AddField(critMultiplier, "critMultiplier")
			.AddField(appearance, "appearance")
			.AddField(animateSpecialMoveEvent, "animateSpecialMoveEvent")
			.AddField(defenseSwordEvent, "defenseSwordEvent")
			.AddField(daggerEvent, "daggerEvent");
		animateSpecialMoveEvent.onEvent += doAnimateSpecialMove;
		defenseSwordEvent.onEvent += doDefenseSwordFunction;
		daggerEvent.onEvent += doDaggerFunction;
		itemId.fieldChangeVisibleEvent += delegate
		{
			ReloadData();
		};
	}

	public override string checkForSpecialItemHoldUpMeessage()
	{
		if (base.QualifiedItemId == "(W)4")
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:MeleeWeapon.cs.14122");
		}
		return null;
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
		float num = 0f;
		float num2 = 0f;
		if (!isScythe())
		{
			switch (type.Value)
			{
			case 0:
			case 3:
				if (defenseCooldown > 0)
				{
					num = (float)defenseCooldown / 1500f;
				}
				num2 = addedSwordScale;
				break;
			case 2:
				if (clubCooldown > 0)
				{
					num = (float)clubCooldown / 6000f;
				}
				num2 = addedClubScale;
				break;
			case 1:
				if (daggerCooldown > 0)
				{
					num = (float)daggerCooldown / 3000f;
				}
				num2 = addedDaggerScale;
				break;
			}
		}
		bool flag = drawShadow && drawStackNumber == StackDrawType.Hide;
		if (!drawShadow || flag)
		{
			num2 = 0f;
		}
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(GetDrawnItemId());
		Texture2D texture = dataOrErrorItem.GetTexture();
		Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
		spriteBatch.Draw(texture, location + ((type.Value == 1) ? new Vector2(38f, 25f) : new Vector2(32f, 32f)), sourceRect, color * transparency, 0f, new Vector2(8f, 8f), 4f * (scaleSize + num2), SpriteEffects.None, layerDepth);
		if (num > 0f && drawShadow && !flag && !isScythe() && (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is ShopMenu) || scaleSize != 1f))
		{
			spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)location.X, (int)location.Y + (64 - (int)(num * 64f)), 64, (int)(num * 64f)), Color.Red * 0.66f);
		}
		DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
	}

	public override int maximumStackSize()
	{
		return 1;
	}

	/// <inheritdoc />
	public override int salePrice(bool ignoreProfitMargins = false)
	{
		if (!IsScythe(itemId.Value))
		{
			return getItemLevel() * 100;
		}
		return 0;
	}

	public static void weaponsTypeUpdate(GameTime time)
	{
		if (addedSwordScale > 0f)
		{
			addedSwordScale -= 0.01f;
		}
		if (addedClubScale > 0f)
		{
			addedClubScale -= 0.01f;
		}
		if (addedDaggerScale > 0f)
		{
			addedDaggerScale -= 0.01f;
		}
		if ((float)timedHitTimer > 0f)
		{
			timedHitTimer -= (int)time.ElapsedGameTime.TotalMilliseconds;
		}
		if (defenseCooldown > 0)
		{
			defenseCooldown -= time.ElapsedGameTime.Milliseconds;
			if (defenseCooldown <= 0)
			{
				addedSwordScale = 0.5f;
				Game1.playSound("objectiveComplete");
			}
		}
		if (attackSwordCooldown > 0)
		{
			attackSwordCooldown -= time.ElapsedGameTime.Milliseconds;
			if (attackSwordCooldown <= 0)
			{
				addedSwordScale = 0.5f;
				Game1.playSound("objectiveComplete");
			}
		}
		if (daggerCooldown > 0)
		{
			daggerCooldown -= time.ElapsedGameTime.Milliseconds;
			if (daggerCooldown <= 0)
			{
				addedDaggerScale = 0.5f;
				Game1.playSound("objectiveComplete");
			}
		}
		if (clubCooldown > 0)
		{
			clubCooldown -= time.ElapsedGameTime.Milliseconds;
			if (clubCooldown <= 0)
			{
				addedClubScale = 0.5f;
				Game1.playSound("objectiveComplete");
			}
		}
	}

	public override void tickUpdate(GameTime time, Farmer who)
	{
		lastUser = who;
		base.tickUpdate(time, who);
		animateSpecialMoveEvent.Poll();
		defenseSwordEvent.Poll();
		daggerEvent.Poll();
		if (isOnSpecial && type.Value == 1 && daggerHitsLeft > 0 && !who.UsingTool)
		{
			quickStab(who);
			triggerDaggerFunction(who, daggerHitsLeft);
		}
		if (anotherClick)
		{
			leftClick(who);
		}
	}

	public override bool doesShowTileLocationMarker()
	{
		return false;
	}

	public int getNumberOfDescriptionCategories()
	{
		int num = 1;
		if (speed.Value != ((type.Value == 2) ? (-8) : 0))
		{
			num++;
		}
		if (addedDefense.Value > 0)
		{
			num++;
		}
		float num2 = critChance.Value;
		if (type.Value == 1)
		{
			num2 += 0.005f;
			num2 *= 1.12f;
		}
		if ((double)num2 / 0.02 >= 1.100000023841858)
		{
			num++;
		}
		if ((double)(critMultiplier.Value - 3f) / 0.02 >= 1.0)
		{
			num++;
		}
		if (knockback.Value != defaultKnockBackForThisType(type.Value))
		{
			num++;
		}
		if (enchantments.Count > 0 && enchantments[enchantments.Count - 1] is DiamondEnchantment)
		{
			num++;
		}
		return num;
	}

	public override void leftClick(Farmer who)
	{
		if (who.health > 0 && Game1.activeClickableMenu == null && Game1.farmEvent == null && !Game1.eventUp && !who.swimming.Value && !who.bathingClothes.Value && !who.onBridge.Value)
		{
			if (!isScythe() && who.FarmerSprite.currentAnimationIndex > ((type.Value == 2) ? 5 : ((type.Value != 1) ? 5 : 0)))
			{
				who.completelyStopAnimatingOrDoingAction();
				who.CanMove = false;
				who.UsingTool = true;
				who.canReleaseTool = true;
				setFarmerAnimating(who);
			}
			else if (!isScythe() && who.FarmerSprite.currentAnimationIndex > ((type.Value == 2) ? 3 : ((type.Value != 1) ? 3 : 0)))
			{
				anotherClick = true;
			}
		}
	}

	/// <inheritdoc />
	public override bool isScythe()
	{
		return IsScythe(base.QualifiedItemId);
	}

	/// <summary>Get whether an item ID matches a scythe tool.</summary>
	/// <param name="id">The item ID.</param>
	public static bool IsScythe(string id)
	{
		switch (id)
		{
		case "(W)47":
		case "(W)53":
		case "(W)66":
		case "47":
		case "53":
		case "66":
			return true;
		default:
			return false;
		}
	}

	public virtual int getItemLevel()
	{
		float num = 0f;
		num += (float)(int)((double)((maxDamage.Value + minDamage.Value) / 2) * (1.0 + 0.03 * (double)(Math.Max(0, speed.Value) + ((type.Value == 1) ? 15 : 0))));
		num += (float)(int)((double)(addedPrecision.Value / 2 + addedDefense.Value) + ((double)critChance.Value - 0.02) * 200.0 + (double)((critMultiplier.Value - 3f) * 6f));
		string qualifiedItemId = base.QualifiedItemId;
		if (!(qualifiedItemId == "(W)2"))
		{
			if (qualifiedItemId == "(W)3")
			{
				num += 15f;
			}
		}
		else
		{
			num += 20f;
		}
		num += (float)(addedDefense.Value * 2);
		return (int)(num / 7f + 1f);
	}

	public static Item attemptAddRandomInnateEnchantment(Item item, Random r, bool force = false, List<BaseEnchantment> enchantsToReroll = null)
	{
		if (r == null)
		{
			r = Game1.random;
		}
		if (item is MeleeWeapon meleeWeapon && (force || r.NextBool()))
		{
			while (true)
			{
				int itemLevel = meleeWeapon.getItemLevel();
				if (r.NextDouble() < 0.125 && itemLevel <= 10)
				{
					meleeWeapon.AddEnchantment(new DefenseEnchantment
					{
						Level = Math.Max(1, Math.Min(2, r.Next(itemLevel + 1) / 2 + 1))
					});
				}
				else if (r.NextDouble() < 0.125)
				{
					meleeWeapon.AddEnchantment(new LightweightEnchantment
					{
						Level = r.Next(1, 6)
					});
				}
				else if (r.NextDouble() < 0.125)
				{
					meleeWeapon.AddEnchantment(new SlimeGathererEnchantment());
				}
				switch (r.Next(5))
				{
				case 0:
					meleeWeapon.AddEnchantment(new AttackEnchantment
					{
						Level = Math.Max(1, Math.Min(5, r.Next(itemLevel + 1) / 2 + 1))
					});
					break;
				case 1:
					meleeWeapon.AddEnchantment(new CritEnchantment
					{
						Level = Math.Max(1, Math.Min(3, r.Next(itemLevel) / 3))
					});
					break;
				case 2:
					meleeWeapon.AddEnchantment(new WeaponSpeedEnchantment
					{
						Level = Math.Max(1, Math.Min(Math.Max(1, 4 - meleeWeapon.speed.Value), r.Next(itemLevel)))
					});
					break;
				case 3:
					meleeWeapon.AddEnchantment(new SlimeSlayerEnchantment());
					break;
				case 4:
					meleeWeapon.AddEnchantment(new CritPowerEnchantment
					{
						Level = Math.Max(1, Math.Min(3, r.Next(itemLevel) / 3))
					});
					break;
				}
				if (enchantsToReroll == null)
				{
					break;
				}
				bool flag = false;
				foreach (BaseEnchantment item2 in enchantsToReroll)
				{
					foreach (BaseEnchantment enchantment in meleeWeapon.enchantments)
					{
						if (item2.GetType().Equals(enchantment.GetType()))
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
				if (!flag)
				{
					break;
				}
				meleeWeapon.enchantments.RemoveWhere((BaseEnchantment enchantment) => enchantment.IsSecondaryEnchantment() && !(enchantment is GalaxySoulEnchantment));
			}
		}
		return item;
	}

	public override string getDescription()
	{
		if (!isScythe())
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(Game1.parseText(base.description, Game1.smallFont, getDescriptionWidth()));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(Game1.content.LoadString("Strings\\StringsFromCSFiles:MeleeWeapon.cs.14132", minDamage, maxDamage));
			if (speed.Value != 0)
			{
				stringBuilder.AppendLine(Game1.content.LoadString("Strings\\StringsFromCSFiles:MeleeWeapon.cs.14134", (speed.Value > 0) ? "+" : "-", Math.Abs(speed.Value)));
			}
			if (addedAreaOfEffect.Value > 0)
			{
				stringBuilder.AppendLine(Game1.content.LoadString("Strings\\StringsFromCSFiles:MeleeWeapon.cs.14136", addedAreaOfEffect));
			}
			if (addedPrecision.Value > 0)
			{
				stringBuilder.AppendLine(Game1.content.LoadString("Strings\\StringsFromCSFiles:MeleeWeapon.cs.14138", addedPrecision));
			}
			if (addedDefense.Value > 0)
			{
				stringBuilder.AppendLine(Game1.content.LoadString("Strings\\StringsFromCSFiles:MeleeWeapon.cs.14140", addedDefense));
			}
			if ((double)critChance.Value / 0.02 >= 2.0)
			{
				stringBuilder.AppendLine(Game1.content.LoadString("Strings\\StringsFromCSFiles:MeleeWeapon.cs.14142", (int)((double)critChance.Value / 0.02)));
			}
			if ((double)(critMultiplier.Value - 3f) / 0.02 >= 1.0)
			{
				stringBuilder.AppendLine(Game1.content.LoadString("Strings\\StringsFromCSFiles:MeleeWeapon.cs.14144", (int)((double)(critMultiplier.Value - 3f) / 0.02)));
			}
			if (knockback.Value != defaultKnockBackForThisType(type.Value))
			{
				stringBuilder.AppendLine(Game1.content.LoadString("Strings\\StringsFromCSFiles:MeleeWeapon.cs.14140", (knockback.Value > defaultKnockBackForThisType(type.Value)) ? "+" : "", (int)Math.Ceiling(Math.Abs(knockback.Value - defaultKnockBackForThisType(type.Value)) * 10f)));
			}
			return stringBuilder.ToString();
		}
		return Game1.parseText(base.description, Game1.smallFont, getDescriptionWidth());
	}

	public virtual float defaultKnockBackForThisType(int type)
	{
		switch (type)
		{
		case 1:
			return 0.5f;
		case 0:
		case 3:
			return 1f;
		case 2:
			return 1.5f;
		default:
			return -1f;
		}
	}

	public virtual Rectangle getAreaOfEffect(int x, int y, int facingDirection, ref Vector2 tileLocation1, ref Vector2 tileLocation2, Rectangle wielderBoundingBox, int indexInCurrentAnimation)
	{
		Rectangle result = Rectangle.Empty;
		int num;
		int num2;
		int num3;
		int num4;
		if (type.Value == 1)
		{
			num = 74;
			num2 = 48;
			num3 = 42;
			num4 = -32;
		}
		else
		{
			num = 64;
			num2 = 64;
			num4 = -32;
			num3 = 0;
		}
		if (type.Value == 1)
		{
			switch (facingDirection)
			{
			case 0:
				result = new Rectangle(x - num / 2, wielderBoundingBox.Y - num2 - num3, num / 2, num2 + num3);
				tileLocation1 = new Vector2(Game1.random.Choose(result.Left, result.Right) / 64, result.Top / 64);
				tileLocation2 = new Vector2(result.Center.X / 64, result.Top / 64);
				result.Offset(20, -16);
				result.Height += 16;
				result.Width += 20;
				break;
			case 1:
				result = new Rectangle(wielderBoundingBox.Right, y - num2 / 2 + num4, (int)((float)num2 * 1.15f), num);
				tileLocation1 = new Vector2(result.Center.X / 64, Game1.random.Choose(result.Top, result.Bottom) / 64);
				tileLocation2 = new Vector2(result.Center.X / 64, result.Center.Y / 64);
				result.Offset(-4, 0);
				result.Width += 16;
				break;
			case 2:
				result = new Rectangle(x - num / 2, wielderBoundingBox.Bottom, num, (int)((float)num2 * 1.75f));
				tileLocation1 = new Vector2(Game1.random.Choose(result.Left, result.Right) / 64, result.Center.Y / 64);
				tileLocation2 = new Vector2(result.Center.X / 64, result.Center.Y / 64);
				result.Offset(12, -8);
				result.Width -= 21;
				break;
			case 3:
				result = new Rectangle(wielderBoundingBox.Left - (int)((float)num2 * 1.15f), y - num2 / 2 + num4, (int)((float)num2 * 1.15f), num);
				tileLocation1 = new Vector2(result.Left / 64, Game1.random.Choose(result.Top, result.Bottom) / 64);
				tileLocation2 = new Vector2(result.Left / 64, result.Center.Y / 64);
				result.Offset(-12, 0);
				result.Width += 16;
				break;
			}
		}
		else
		{
			switch (facingDirection)
			{
			case 0:
				result = new Rectangle(x - num / 2, wielderBoundingBox.Y - num2 - num3, num, num2 + num3);
				tileLocation1 = new Vector2(Game1.random.Choose(result.Left, result.Right) / 64, result.Top / 64);
				tileLocation2 = new Vector2(result.Center.X / 64, result.Top / 64);
				switch (indexInCurrentAnimation)
				{
				case 5:
					result.Offset(76, -32);
					break;
				case 4:
					result.Offset(56, -32);
					result.Height += 32;
					break;
				case 3:
					result.Offset(40, -60);
					result.Height += 48;
					break;
				case 2:
					result.Offset(-12, -68);
					result.Height += 48;
					break;
				case 1:
					result.Offset(-48, -56);
					result.Height += 32;
					break;
				case 0:
					result.Offset(-60, -12);
					break;
				}
				break;
			case 2:
				result = new Rectangle(x - num / 2, wielderBoundingBox.Bottom, num, (int)((float)num2 * 1.5f));
				tileLocation1 = new Vector2(Game1.random.Choose(result.Left, result.Right) / 64, result.Center.Y / 64);
				tileLocation2 = new Vector2(result.Center.X / 64, result.Center.Y / 64);
				switch (indexInCurrentAnimation)
				{
				case 0:
					result.Offset(72, -92);
					break;
				case 1:
					result.Offset(56, -32);
					break;
				case 2:
					result.Offset(40, -28);
					break;
				case 3:
					result.Offset(-12, -8);
					break;
				case 4:
					result.Offset(-80, -24);
					result.Width += 32;
					break;
				case 5:
					result.Offset(-68, -44);
					break;
				}
				break;
			case 1:
				result = new Rectangle(wielderBoundingBox.Right, y - num2 / 2 + num4, num2, num);
				tileLocation1 = new Vector2(result.Center.X / 64, Game1.random.Choose(result.Top, result.Bottom) / 64);
				tileLocation2 = new Vector2(result.Center.X / 64, result.Center.Y / 64);
				switch (indexInCurrentAnimation)
				{
				case 0:
					result.Offset(-44, -84);
					break;
				case 1:
					result.Offset(4, -44);
					break;
				case 2:
					result.Offset(12, -4);
					break;
				case 3:
					result.Offset(12, 37);
					break;
				case 4:
					result.Offset(-28, 60);
					break;
				case 5:
					result.Offset(-60, 72);
					break;
				}
				break;
			case 3:
				result = new Rectangle(wielderBoundingBox.Left - num2, y - num2 / 2 + num4, num2, num);
				tileLocation1 = new Vector2(result.Left / 64, Game1.random.Choose(result.Top, result.Bottom) / 64);
				tileLocation2 = new Vector2(result.Left / 64, result.Center.Y / 64);
				switch (indexInCurrentAnimation)
				{
				case 0:
					result.Offset(56, -76);
					break;
				case 1:
					result.Offset(-8, -56);
					break;
				case 2:
					result.Offset(-16, -4);
					break;
				case 3:
					result.Offset(0, 37);
					break;
				case 4:
					result.Offset(24, 60);
					break;
				case 5:
					result.Offset(64, 64);
					break;
				}
				break;
			}
		}
		result.Inflate(addedAreaOfEffect.Value, addedAreaOfEffect.Value);
		return result;
	}

	public void triggerDefenseSwordFunction(Farmer who)
	{
		defenseSwordEvent.Fire();
	}

	private void doDefenseSwordFunction()
	{
		isOnSpecial = false;
		lastUser.UsingTool = false;
		lastUser.CanMove = true;
		lastUser.FarmerSprite.PauseForSingleAnimation = false;
	}

	public void triggerDaggerFunction(Farmer who, int dagger_hits_left)
	{
		daggerEvent.Fire(dagger_hits_left);
	}

	private void doDaggerFunction(int dagger_hits)
	{
		Vector2 uniformPositionAwayFromBox = lastUser.getUniformPositionAwayFromBox(lastUser.FacingDirection, 48);
		int num = daggerHitsLeft;
		daggerHitsLeft = dagger_hits;
		DoDamage(Game1.currentLocation, (int)uniformPositionAwayFromBox.X, (int)uniformPositionAwayFromBox.Y, lastUser.FacingDirection, 1, lastUser);
		daggerHitsLeft = num;
		if (lastUser != null && lastUser.IsLocalPlayer)
		{
			daggerHitsLeft--;
		}
		isOnSpecial = false;
		lastUser.UsingTool = false;
		lastUser.CanMove = true;
		lastUser.FarmerSprite.PauseForSingleAnimation = false;
		if (daggerHitsLeft > 0 && lastUser != null && lastUser.IsLocalPlayer)
		{
			quickStab(lastUser);
		}
	}

	public void triggerClubFunction(Farmer who)
	{
		if (PlayUseSounds)
		{
			who.playNearbySoundAll("clubSmash");
		}
		who.currentLocation.damageMonster(new Rectangle((int)who.Position.X - 192, who.GetBoundingBox().Y - 192, 384, 384), minDamage.Value, maxDamage.Value, isBomb: false, 1.5f, 100, 0f, 1f, triggerMonsterInvincibleTimer: false, who);
		Game1.viewport.Y -= 21;
		Game1.viewport.X += Game1.random.Next(-32, 32);
		Vector2 uniformPositionAwayFromBox = who.getUniformPositionAwayFromBox(who.FacingDirection, 64);
		switch (who.FacingDirection)
		{
		case 0:
		case 2:
			uniformPositionAwayFromBox.X -= 32f;
			uniformPositionAwayFromBox.Y -= 32f;
			break;
		case 1:
			uniformPositionAwayFromBox.X -= 42f;
			uniformPositionAwayFromBox.Y -= 32f;
			break;
		case 3:
			uniformPositionAwayFromBox.Y -= 32f;
			break;
		}
		Game1.multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 128, 64, 64), 40f, 4, 0, uniformPositionAwayFromBox, flicker: false, who.FacingDirection == 1));
		who.jitterStrength = 2f;
	}

	private void beginSpecialMove(Farmer who)
	{
		if (!Game1.fadeToBlack)
		{
			isOnSpecial = true;
			who.UsingTool = true;
			who.CanMove = false;
		}
	}

	private void quickStab(Farmer who)
	{
		AnimatedSprite.endOfAnimationBehavior endOfBehaviorFunction = delegate(Farmer f)
		{
			triggerDaggerFunction(f, daggerHitsLeft);
		};
		if (!who.IsLocalPlayer)
		{
			endOfBehaviorFunction = null;
		}
		switch (who.FacingDirection)
		{
		case 0:
			((FarmerSprite)who.Sprite).animateOnce(276, 15f, 2, endOfBehaviorFunction);
			Update(0, 0, who);
			break;
		case 1:
			((FarmerSprite)who.Sprite).animateOnce(274, 15f, 2, endOfBehaviorFunction);
			Update(1, 0, who);
			break;
		case 2:
			((FarmerSprite)who.Sprite).animateOnce(272, 15f, 2, endOfBehaviorFunction);
			Update(2, 0, who);
			break;
		case 3:
			((FarmerSprite)who.Sprite).animateOnce(278, 15f, 2, endOfBehaviorFunction);
			Update(3, 0, who);
			break;
		}
		FireProjectile(who);
		beginSpecialMove(who);
		if (PlayUseSounds)
		{
			who.playNearbySoundLocal("daggerswipe");
		}
	}

	protected virtual int specialCooldown()
	{
		return type.Value switch
		{
			3 => defenseCooldown, 
			1 => daggerCooldown, 
			2 => clubCooldown, 
			0 => attackSwordCooldown, 
			_ => 0, 
		};
	}

	public virtual void animateSpecialMove(Farmer who)
	{
		lastUser = who;
		if ((type.Value != 3 || (!Name.Contains("Scythe") && !isScythe())) && !Game1.fadeToBlack && specialCooldown() <= 0)
		{
			animateSpecialMoveEvent.Fire();
		}
	}

	protected virtual void doAnimateSpecialMove()
	{
		if (lastUser == null || lastUser.CurrentTool != this)
		{
			return;
		}
		if (lastUser.isEmoteAnimating)
		{
			lastUser.EndEmoteAnimation();
		}
		switch (type.Value)
		{
		case 3:
		{
			AnimatedSprite.endOfAnimationBehavior endOfBehaviorFunction = triggerDefenseSwordFunction;
			if (!lastUser.IsLocalPlayer)
			{
				endOfBehaviorFunction = null;
			}
			switch (lastUser.FacingDirection)
			{
			case 0:
				((FarmerSprite)lastUser.Sprite).animateOnce(252, 500f, 1, endOfBehaviorFunction);
				Update(0, 0, lastUser);
				break;
			case 1:
				((FarmerSprite)lastUser.Sprite).animateOnce(243, 500f, 1, endOfBehaviorFunction);
				Update(1, 0, lastUser);
				break;
			case 2:
				((FarmerSprite)lastUser.Sprite).animateOnce(234, 500f, 1, endOfBehaviorFunction);
				Update(2, 0, lastUser);
				break;
			case 3:
				((FarmerSprite)lastUser.Sprite).animateOnce(259, 500f, 1, endOfBehaviorFunction);
				Update(3, 0, lastUser);
				break;
			}
			if (PlayUseSounds)
			{
				lastUser.playNearbySoundLocal("batFlap");
			}
			beginSpecialMove(lastUser);
			if (lastUser.IsLocalPlayer)
			{
				defenseCooldown = 1500;
			}
			if (lastUser.professions.Contains(28))
			{
				defenseCooldown /= 2;
			}
			if (hasEnchantmentOfType<ArtfulEnchantment>())
			{
				defenseCooldown /= 2;
			}
			break;
		}
		case 2:
		{
			AnimatedSprite.endOfAnimationBehavior endOfBehaviorFunction2 = triggerClubFunction;
			if (!lastUser.IsLocalPlayer)
			{
				endOfBehaviorFunction2 = null;
			}
			if (PlayUseSounds)
			{
				lastUser.playNearbySoundLocal("clubswipe");
			}
			switch (lastUser.FacingDirection)
			{
			case 0:
				((FarmerSprite)lastUser.Sprite).animateOnce(176, 40f, 8, endOfBehaviorFunction2);
				Update(0, 0, lastUser);
				break;
			case 1:
				((FarmerSprite)lastUser.Sprite).animateOnce(168, 40f, 8, endOfBehaviorFunction2);
				Update(1, 0, lastUser);
				break;
			case 2:
				((FarmerSprite)lastUser.Sprite).animateOnce(160, 40f, 8, endOfBehaviorFunction2);
				Update(2, 0, lastUser);
				break;
			case 3:
				((FarmerSprite)lastUser.Sprite).animateOnce(184, 40f, 8, endOfBehaviorFunction2);
				Update(3, 0, lastUser);
				break;
			}
			beginSpecialMove(lastUser);
			if (lastUser.IsLocalPlayer)
			{
				clubCooldown = 6000;
			}
			if (lastUser.professions.Contains(28))
			{
				clubCooldown /= 2;
			}
			if (hasEnchantmentOfType<ArtfulEnchantment>())
			{
				clubCooldown /= 2;
			}
			break;
		}
		case 1:
			daggerHitsLeft = 4;
			quickStab(lastUser);
			if (lastUser.IsLocalPlayer)
			{
				daggerCooldown = 3000;
			}
			if (lastUser.professions.Contains(28))
			{
				daggerCooldown /= 2;
			}
			if (hasEnchantmentOfType<ArtfulEnchantment>())
			{
				daggerCooldown /= 2;
			}
			break;
		}
	}

	public void doSwipe(int type, Vector2 position, int facingDirection, float swipeSpeed, Farmer f)
	{
		if (f == null || f.CurrentTool != this)
		{
			return;
		}
		if (f.IsLocalPlayer)
		{
			f.TemporaryPassableTiles.Clear();
			f.currentLocation.lastTouchActionLocation = Vector2.Zero;
		}
		swipeSpeed *= 1.3f;
		switch (type)
		{
		case 3:
			if (f.CurrentTool == this)
			{
				switch (f.FacingDirection)
				{
				case 0:
					((FarmerSprite)f.Sprite).animateOnce(248, swipeSpeed, 6);
					Update(0, 0, f);
					break;
				case 1:
					((FarmerSprite)f.Sprite).animateOnce(240, swipeSpeed, 6);
					Update(1, 0, f);
					break;
				case 2:
					((FarmerSprite)f.Sprite).animateOnce(232, swipeSpeed, 6);
					Update(2, 0, f);
					break;
				case 3:
					((FarmerSprite)f.Sprite).animateOnce(256, swipeSpeed, 6);
					Update(3, 0, f);
					break;
				}
			}
			if (PlayUseSounds && f.ShouldHandleAnimationSound())
			{
				f.playNearbySoundLocal("swordswipe");
			}
			break;
		case 2:
			if (f.CurrentTool == this)
			{
				switch (f.FacingDirection)
				{
				case 0:
					((FarmerSprite)f.Sprite).animateOnce(248, swipeSpeed, 8);
					Update(0, 0, f);
					break;
				case 1:
					((FarmerSprite)f.Sprite).animateOnce(240, swipeSpeed, 8);
					Update(1, 0, f);
					break;
				case 2:
					((FarmerSprite)f.Sprite).animateOnce(232, swipeSpeed, 8);
					Update(2, 0, f);
					break;
				case 3:
					((FarmerSprite)f.Sprite).animateOnce(256, swipeSpeed, 8);
					Update(3, 0, f);
					break;
				}
			}
			if (PlayUseSounds)
			{
				f.playNearbySoundLocal("clubswipe");
			}
			break;
		}
	}

	public virtual void FireProjectile(Farmer who)
	{
		if (cachedData?.Projectiles == null)
		{
			return;
		}
		foreach (WeaponProjectile projectile in cachedData.Projectiles)
		{
			float num = 0f;
			float num2 = 1f;
			switch (who.facingDirection.Value)
			{
			case 0:
				num = 90f;
				break;
			case 1:
				num = 0f;
				break;
			case 3:
				num = 180f;
				num2 = -1f;
				break;
			case 2:
				num = 270f;
				break;
			}
			num += (projectile.MinAngleOffset + (float)Game1.random.NextDouble() * (projectile.MaxAngleOffset - projectile.MinAngleOffset)) * num2;
			num *= (float)Math.PI / 180f;
			string text = null;
			if (projectile.Item != null)
			{
				text = ItemQueryResolver.TryResolveRandomItem(projectile.Item, new ItemQueryContext(who.currentLocation, who, null, $"weapon '{base.QualifiedItemId}' > projectile data '{projectile.Id}'"))?.QualifiedItemId;
				if (text == null)
				{
					continue;
				}
			}
			Vector2 vector = who.getStandingPosition() - new Vector2(32f, 32f);
			BasicProjectile basicProjectile = new BasicProjectile(projectile.Damage, projectile.SpriteIndex, projectile.Bounces, projectile.TailLength, (float)projectile.RotationVelocity * ((float)Math.PI / 180f), (float)projectile.Velocity * (float)Math.Cos(num), (float)projectile.Velocity * (float)(0.0 - Math.Sin(num)), vector, firingSound: projectile.FireSound, collisionSound: projectile.CollisionSound, bounceSound: projectile.BounceSound, explode: projectile.Explodes, damagesMonsters: true, location: who.currentLocation, firer: who, collisionBehavior: null, shotItemId: text);
			basicProjectile.ignoreTravelGracePeriod.Value = true;
			basicProjectile.ignoreMeleeAttacks.Value = true;
			basicProjectile.maxTravelDistance.Value = projectile.MaxDistance * 64;
			basicProjectile.height.Value = 32f;
			who.currentLocation.projectiles.Add(basicProjectile);
		}
	}

	public virtual void setFarmerAnimating(Farmer who)
	{
		anotherClick = false;
		who.FarmerSprite.PauseForSingleAnimation = false;
		who.FarmerSprite.StopAnimation();
		swipeSpeed = (float)(400 - speed.Value * 40) - who.addedSpeed * 40f;
		swipeSpeed *= 1f - who.buffs.WeaponSpeedMultiplier;
		if (who.IsLocalPlayer)
		{
			foreach (BaseEnchantment enchantment in enchantments)
			{
				if (enchantment is BaseWeaponEnchantment baseWeaponEnchantment)
				{
					baseWeaponEnchantment.OnSwing(this, who);
				}
			}
			FireProjectile(who);
		}
		if (type.Value != 1)
		{
			doSwipe(type.Value, who.Position, who.FacingDirection, swipeSpeed / (float)((type.Value == 2) ? 5 : 8), who);
			who.lastClick = Vector2.Zero;
			Vector2 toolLocation = who.GetToolLocation(ignoreClick: true);
			DoDamage(who.currentLocation, (int)toolLocation.X, (int)toolLocation.Y, who.FacingDirection, 1, who);
		}
		else
		{
			if (PlayUseSounds && who.IsLocalPlayer)
			{
				who.playNearbySoundAll("daggerswipe");
			}
			swipeSpeed /= 4f;
			switch (who.FacingDirection)
			{
			case 0:
				((FarmerSprite)who.Sprite).animateOnce(276, swipeSpeed, 2);
				Update(0, 0, who);
				break;
			case 1:
				((FarmerSprite)who.Sprite).animateOnce(274, swipeSpeed, 2);
				Update(1, 0, who);
				break;
			case 2:
				((FarmerSprite)who.Sprite).animateOnce(272, swipeSpeed, 2);
				Update(2, 0, who);
				break;
			case 3:
				((FarmerSprite)who.Sprite).animateOnce(278, swipeSpeed, 2);
				Update(3, 0, who);
				break;
			}
			Vector2 toolLocation2 = who.GetToolLocation(ignoreClick: true);
			DoDamage(who.currentLocation, (int)toolLocation2.X, (int)toolLocation2.Y, who.FacingDirection, 1, who);
		}
		if (who.CurrentTool == null)
		{
			who.completelyStopAnimatingOrDoingAction();
			who.forceCanMove();
		}
	}

	public override void actionWhenStopBeingHeld(Farmer who)
	{
		who.UsingTool = false;
		anotherClick = false;
		base.actionWhenStopBeingHeld(who);
	}

	public virtual void RecalculateAppliedForges(bool force = false)
	{
		if (enchantments.Count == 0 && !force)
		{
			return;
		}
		foreach (BaseEnchantment enchantment in enchantments)
		{
			if (enchantment.IsForge())
			{
				enchantment.UnapplyTo(this);
			}
		}
		WeaponData data = GetData();
		if (data != null)
		{
			Name = data.Name;
			minDamage.Value = data.MinDamage;
			maxDamage.Value = data.MaxDamage;
			knockback.Value = data.Knockback;
			speed.Value = data.Speed;
			addedPrecision.Value = data.Precision;
			addedDefense.Value = data.Defense;
			type.Value = data.Type;
			addedAreaOfEffect.Value = data.AreaOfEffect;
			critChance.Value = data.CritChance;
			critMultiplier.Value = data.CritMultiplier;
			if (type.Value == 0)
			{
				type.Value = 3;
			}
		}
		foreach (BaseEnchantment enchantment2 in enchantments)
		{
			if (enchantment2.IsForge())
			{
				enchantment2.ApplyTo(this);
			}
		}
	}

	public virtual void DoDamage(GameLocation location, int x, int y, int facingDirection, int power, Farmer who)
	{
		if (!who.IsLocalPlayer)
		{
			return;
		}
		isOnSpecial = false;
		if (type.Value != 2)
		{
			base.DoFunction(location, x, y, power, who);
		}
		lastUser = who;
		Vector2 tileLocation = Vector2.Zero;
		Vector2 tileLocation2 = Vector2.Zero;
		Rectangle areaOfEffect = getAreaOfEffect(x, y, facingDirection, ref tileLocation, ref tileLocation2, who.GetBoundingBox(), who.FarmerSprite.currentAnimationIndex);
		mostRecentArea = areaOfEffect;
		float num = critChance.Value;
		if (type.Value == 1)
		{
			num += 0.005f;
			num *= 1.12f;
		}
		if (location.damageMonster(areaOfEffect, (int)((float)minDamage.Value * (1f + who.buffs.AttackMultiplier)), (int)((float)maxDamage.Value * (1f + who.buffs.AttackMultiplier)), isBomb: false, knockback.Value * (1f + who.buffs.KnockbackMultiplier), (int)((float)addedPrecision.Value * (1f + who.buffs.WeaponPrecisionMultiplier)), num * (1f + who.buffs.CriticalChanceMultiplier), critMultiplier.Value * (1f + who.buffs.CriticalPowerMultiplier), type.Value != 1 || !isOnSpecial, who) && type.Value == 2 && PlayUseSounds)
		{
			who.playNearbySoundAll("clubhit");
		}
		string text = "";
		location.projectiles.RemoveWhere(delegate(Projectile projectile)
		{
			if (areaOfEffect.Intersects(projectile.getBoundingBox()) && !projectile.ignoreMeleeAttacks.Value)
			{
				projectile.behaviorOnCollisionWithOther(location);
			}
			return projectile.destroyMe;
		});
		foreach (Vector2 item in Utility.removeDuplicates(Utility.getListOfTileLocationsForBordersOfNonTileRectangle(areaOfEffect)))
		{
			if (location.terrainFeatures.TryGetValue(item, out var value) && value.performToolAction(this, 0, item))
			{
				location.terrainFeatures.Remove(item);
			}
			if (location.objects.TryGetValue(item, out var value2) && value2.performToolAction(this))
			{
				location.objects.Remove(item);
			}
			if (location.performToolAction(this, (int)item.X, (int)item.Y))
			{
				break;
			}
		}
		if (PlayUseSounds && !text.Equals(""))
		{
			Game1.playSound(text);
		}
		base.CurrentParentTileIndex = base.IndexOfMenuItemView;
		if (who != null && who.isRidingHorse())
		{
			who.completelyStopAnimatingOrDoingAction();
		}
	}

	/// <summary>Get the qualified item ID to draw for this weapon.</summary>
	public string GetDrawnItemId()
	{
		return appearance.Value ?? base.QualifiedItemId;
	}

	public override void drawTooltip(SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
	{
		Utility.drawTextWithShadow(spriteBatch, Game1.parseText(base.description, Game1.smallFont, getDescriptionWidth()), font, new Vector2(x + 16, y + 16 + 4), Game1.textColor);
		y += (int)font.MeasureString(Game1.parseText(base.description, Game1.smallFont, getDescriptionWidth())).Y;
		if (isScythe())
		{
			return;
		}
		Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(120, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
		Color color = Game1.textColor;
		if (hasEnchantmentOfType<RubyEnchantment>())
		{
			color = new Color(0, 120, 120);
		}
		Utility.drawTextWithShadow(spriteBatch, Game1.content.LoadString("Strings\\UI:ItemHover_Damage", minDamage, maxDamage), font, new Vector2(x + 16 + 52, y + 16 + 12), color * 0.9f * alpha);
		y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
		if (speed.Value != ((type.Value == 2) ? (-8) : 0))
		{
			Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(130, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
			bool flag = (type.Value == 2 && speed.Value < -8) || (type.Value != 2 && speed.Value < 0);
			Color color2 = Game1.textColor;
			if (hasEnchantmentOfType<EmeraldEnchantment>())
			{
				color2 = new Color(0, 120, 120);
			}
			Utility.drawTextWithShadow(spriteBatch, Game1.content.LoadString("Strings\\UI:ItemHover_Speed", ((((type.Value == 2) ? (speed.Value - -8) : speed.Value) > 0) ? "+" : "") + ((type.Value == 2) ? (speed.Value - -8) : speed.Value) / 2), font, new Vector2(x + 16 + 52, y + 16 + 12), flag ? Color.DarkRed : (color2 * 0.9f * alpha));
			y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
		}
		if (addedDefense.Value > 0)
		{
			Color color3 = Game1.textColor;
			if (hasEnchantmentOfType<TopazEnchantment>())
			{
				color3 = new Color(0, 120, 120);
			}
			Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(110, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
			Utility.drawTextWithShadow(spriteBatch, Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", addedDefense), font, new Vector2(x + 16 + 52, y + 16 + 12), color3 * 0.9f * alpha);
			y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
		}
		float num = critChance.Value;
		if (type.Value == 1)
		{
			num += 0.005f;
			num *= 1.12f;
		}
		if ((double)num / 0.02 >= 1.100000023841858)
		{
			Color color4 = Game1.textColor;
			if (hasEnchantmentOfType<AquamarineEnchantment>())
			{
				color4 = new Color(0, 120, 120);
			}
			Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(40, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
			Utility.drawTextWithShadow(spriteBatch, Game1.content.LoadString("Strings\\UI:ItemHover_CritChanceBonus", (int)Math.Round((double)(num - 0.001f) / 0.02)), font, new Vector2(x + 16 + 52, y + 16 + 12), color4 * 0.9f * alpha);
			y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
		}
		if ((double)(critMultiplier.Value - 3f) / 0.02 >= 1.0)
		{
			Color color5 = Game1.textColor;
			if (hasEnchantmentOfType<JadeEnchantment>())
			{
				color5 = new Color(0, 120, 120);
			}
			Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16, y + 16 + 4), new Rectangle(160, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
			Utility.drawTextWithShadow(spriteBatch, Game1.content.LoadString("Strings\\UI:ItemHover_CritPowerBonus", (int)((double)(critMultiplier.Value - 3f) / 0.02)), font, new Vector2(x + 16 + 44, y + 16 + 12), color5 * 0.9f * alpha);
			y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
		}
		if (knockback.Value != defaultKnockBackForThisType(type.Value))
		{
			Color color6 = Game1.textColor;
			if (hasEnchantmentOfType<AmethystEnchantment>())
			{
				color6 = new Color(0, 120, 120);
			}
			Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(70, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
			Utility.drawTextWithShadow(spriteBatch, Game1.content.LoadString("Strings\\UI:ItemHover_Weight", (((float)(int)Math.Ceiling(Math.Abs(knockback.Value - defaultKnockBackForThisType(type.Value)) * 10f) > defaultKnockBackForThisType(type.Value)) ? "+" : "") + (int)Math.Ceiling(Math.Abs(knockback.Value - defaultKnockBackForThisType(type.Value)) * 10f)), font, new Vector2(x + 16 + 52, y + 16 + 12), color6 * 0.9f * alpha);
			y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
		}
		if (enchantments.Count > 0 && enchantments[enchantments.Count - 1] is DiamondEnchantment)
		{
			Color color7 = new Color(0, 120, 120);
			int num2 = GetMaxForges() - GetTotalForgeLevels();
			string text = ((num2 == 1) ? Game1.content.LoadString("Strings\\UI:ItemHover_DiamondForge_Singular", num2) : Game1.content.LoadString("Strings\\UI:ItemHover_DiamondForge_Plural", num2));
			Utility.drawTextWithShadow(spriteBatch, text, font, new Vector2(x + 16, y + 16 + 12), color7 * 0.9f * alpha);
			y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
		}
		foreach (BaseEnchantment enchantment in enchantments)
		{
			if (enchantment.ShouldBeDisplayed())
			{
				Color color8 = new Color(120, 0, 210);
				if (enchantment.IsSecondaryEnchantment())
				{
					Utility.drawWithShadow(spriteBatch, Game1.mouseCursors_1_6, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(502, 430, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
					color8 = new Color(120, 50, 100);
				}
				else
				{
					Utility.drawWithShadow(spriteBatch, Game1.mouseCursors2, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(127, 35, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
				}
				Utility.drawTextWithShadow(spriteBatch, ((BaseEnchantment.hideEnchantmentName && !enchantment.IsSecondaryEnchantment()) || (BaseEnchantment.hideSecondaryEnchantName && enchantment.IsSecondaryEnchantment())) ? "???" : enchantment.GetDisplayName(), font, new Vector2(x + 16 + 52, y + 16 + 12), color8 * 0.9f * alpha);
				y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
			}
		}
	}

	public override Point getExtraSpaceNeededForTooltipSpecialIcons(SpriteFont font, int minWidth, int horizontalBuffer, int startingHeight, StringBuilder descriptionText, string boldTitleText, int moneyAmountToDisplayAtBottom)
	{
		int num = 9999;
		Point result = new Point(0, 0);
		result.Y += Math.Max(60, (int)((boldTitleText != null) ? (Game1.dialogueFont.MeasureString(boldTitleText).Y + 16f) : 0f) + 32) + (int)font.MeasureString("T").Y + (int)((moneyAmountToDisplayAtBottom > -1) ? (font.MeasureString(moneyAmountToDisplayAtBottom.ToString() ?? "").Y + 4f) : 0f);
		result.Y += ((!isScythe()) ? (getNumberOfDescriptionCategories() * 4 * 12) : 0);
		result.Y += (int)font.MeasureString(Game1.parseText(base.description, Game1.smallFont, getDescriptionWidth())).Y;
		result.X = (int)Math.Max(minWidth, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Damage", num, num)).X + (float)horizontalBuffer, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Speed", num)).X + (float)horizontalBuffer, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", num)).X + (float)horizontalBuffer, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_CritChanceBonus", num)).X + (float)horizontalBuffer, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_CritPowerBonus", num)).X + (float)horizontalBuffer, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Weight", num)).X + (float)horizontalBuffer))))));
		if (enchantments.Count > 0 && enchantments[enchantments.Count - 1] is DiamondEnchantment)
		{
			result.X = (int)Math.Max(result.X, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_DiamondForge_Plural", GetMaxForges())).X);
		}
		foreach (BaseEnchantment enchantment in enchantments)
		{
			if (enchantment.ShouldBeDisplayed())
			{
				result.Y += (int)Math.Max(font.MeasureString("TT").Y, 48f);
			}
		}
		return result;
	}

	public virtual void ResetIndexOfMenuItemView()
	{
		base.IndexOfMenuItemView = base.InitialParentTileIndex;
	}

	public virtual void drawDuringUse(int frameOfFarmerAnimation, int facingDirection, SpriteBatch spriteBatch, Vector2 playerPosition, Farmer f)
	{
		drawDuringUse(frameOfFarmerAnimation, facingDirection, spriteBatch, playerPosition, f, GetDrawnItemId(), type.Value, isOnSpecial);
	}

	public override bool CanForge(Item item)
	{
		if (item is MeleeWeapon meleeWeapon && meleeWeapon.type.Value == type.Value)
		{
			return true;
		}
		return base.CanForge(item);
	}

	public override bool CanAddEnchantment(BaseEnchantment enchantment)
	{
		if (enchantment is GalaxySoulEnchantment && !isGalaxyWeapon())
		{
			return false;
		}
		return base.CanAddEnchantment(enchantment);
	}

	public bool isGalaxyWeapon()
	{
		if (!(base.QualifiedItemId == "(W)4") && !(base.QualifiedItemId == "(W)23"))
		{
			return base.QualifiedItemId == "(W)29";
		}
		return true;
	}

	/// <summary>Convert this weapon to a new item ID. This reloads the weapon data but keeps any previous enchantments, mod data, etc.</summary>
	/// <param name="newItemId">The new unqualified item ID.</param>
	public void transform(string newItemId)
	{
		base.ItemId = newItemId;
		appearance.Value = null;
		RecalculateAppliedForges(force: true);
	}

	public override bool Forge(Item item, bool count_towards_stats = false)
	{
		if (isScythe())
		{
			return false;
		}
		if (item is MeleeWeapon meleeWeapon && meleeWeapon.type.Value == type.Value)
		{
			appearance.Value = meleeWeapon.QualifiedItemId;
			return true;
		}
		return base.Forge(item, count_towards_stats);
	}

	public static void drawDuringUse(int frameOfFarmerAnimation, int facingDirection, SpriteBatch spriteBatch, Vector2 playerPosition, Farmer f, string weaponItemId, int type, bool isOnSpecial)
	{
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(weaponItemId);
		Texture2D texture = dataOrErrorItem.GetTexture() ?? Tool.weaponsTexture;
		Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
		float drawLayer = f.getDrawLayer();
		FarmerRenderer.FarmerSpriteLayers layer = f.FacingDirection switch
		{
			0 => FarmerRenderer.FarmerSpriteLayers.ToolUp, 
			2 => FarmerRenderer.FarmerSpriteLayers.ToolDown, 
			_ => FarmerRenderer.FarmerSpriteLayers.TOOL_IN_USE_SIDE, 
		};
		float layerDepth = FarmerRenderer.GetLayerDepth(drawLayer, FarmerRenderer.FarmerSpriteLayers.ToolUp);
		float layerDepth2 = FarmerRenderer.GetLayerDepth(drawLayer, layer);
		if (type != 1)
		{
			if (isOnSpecial)
			{
				switch (type)
				{
				case 3:
					switch (f.FacingDirection)
					{
					case 0:
						spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 8f, playerPosition.Y - 44f), sourceRect, Color.White, (float)Math.PI * -9f / 16f, center, 4f, SpriteEffects.None, layerDepth2);
						break;
					case 1:
						spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 8f, playerPosition.Y - 4f), sourceRect, Color.White, (float)Math.PI * -3f / 16f, center, 4f, SpriteEffects.None, layerDepth2);
						break;
					case 2:
						spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 52f, playerPosition.Y + 4f), sourceRect, Color.White, -5.105088f, center, 4f, SpriteEffects.None, layerDepth2);
						break;
					case 3:
						spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 56f, playerPosition.Y - 4f), sourceRect, Color.White, (float)Math.PI * 3f / 16f, new Vector2(15f, 15f), 4f, SpriteEffects.FlipHorizontally, layerDepth2);
						break;
					}
					break;
				case 2:
					switch (facingDirection)
					{
					case 1:
						switch (frameOfFarmerAnimation)
						{
						case 0:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 32f - 12f, playerPosition.Y - 80f), sourceRect, Color.White, (float)Math.PI * -3f / 8f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 1:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f, playerPosition.Y - 64f - 48f), sourceRect, Color.White, (float)Math.PI / 8f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 2:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X + 128f - 16f, playerPosition.Y - 64f - 12f), sourceRect, Color.White, (float)Math.PI * 3f / 8f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 3:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X + 72f, playerPosition.Y - 64f + 16f - 32f), sourceRect, Color.White, (float)Math.PI / 8f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 4:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X + 96f, playerPosition.Y - 64f + 16f - 16f), sourceRect, Color.White, (float)Math.PI / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 5:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X + 96f - 12f, playerPosition.Y - 64f + 16f), sourceRect, Color.White, (float)Math.PI / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 6:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X + 96f - 16f, playerPosition.Y - 64f + 40f - 8f), sourceRect, Color.White, (float)Math.PI / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 7:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X + 96f - 8f, playerPosition.Y + 40f), sourceRect, Color.White, (float)Math.PI * 5f / 16f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						}
						break;
					case 3:
						switch (frameOfFarmerAnimation)
						{
						case 0:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 4f + 8f, playerPosition.Y - 56f - 64f), sourceRect, Color.White, (float)Math.PI / 8f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 1:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 32f, playerPosition.Y - 32f), sourceRect, Color.White, (float)Math.PI * -5f / 8f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 2:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 12f, playerPosition.Y + 8f), sourceRect, Color.White, (float)Math.PI * -7f / 8f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 3:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 32f - 4f, playerPosition.Y + 8f), sourceRect, Color.White, (float)Math.PI * -3f / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 4:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 16f - 24f, playerPosition.Y + 64f + 12f - 64f), sourceRect, Color.White, 4.31969f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 5:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 20f, playerPosition.Y + 64f + 40f - 64f), sourceRect, Color.White, 3.926991f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 6:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 16f, playerPosition.Y + 64f + 56f), sourceRect, Color.White, 3.926991f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 7:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 8f, playerPosition.Y + 64f + 64f), sourceRect, Color.White, 3.7306414f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						}
						break;
					default:
						switch (frameOfFarmerAnimation)
						{
						case 0:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 24f, playerPosition.Y - 21f - 8f - 64f), sourceRect, Color.White, -(float)Math.PI / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 1:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 16f, playerPosition.Y - 21f - 64f + 4f), sourceRect, Color.White, -(float)Math.PI / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 2:
							spriteBatch.Draw(texture, new Vector2(playerPosition.X - 16f, playerPosition.Y - 21f + 20f - 64f), sourceRect, Color.White, -(float)Math.PI / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							break;
						case 3:
							if (facingDirection == 2)
							{
								spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f + 8f, playerPosition.Y + 32f), sourceRect, Color.White, -3.926991f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							}
							else
							{
								spriteBatch.Draw(texture, new Vector2(playerPosition.X - 16f, playerPosition.Y - 21f + 32f - 64f), sourceRect, Color.White, -(float)Math.PI / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							}
							break;
						case 4:
							if (facingDirection == 2)
							{
								spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f + 8f, playerPosition.Y + 32f), sourceRect, Color.White, -3.926991f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							}
							break;
						case 5:
							if (facingDirection == 2)
							{
								spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f + 12f, playerPosition.Y + 64f - 20f), sourceRect, Color.White, (float)Math.PI * 3f / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							}
							break;
						case 6:
							if (facingDirection == 2)
							{
								spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f + 12f, playerPosition.Y + 64f + 54f), sourceRect, Color.White, (float)Math.PI * 3f / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							}
							break;
						case 7:
							if (facingDirection == 2)
							{
								spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f + 12f, playerPosition.Y + 64f + 58f), sourceRect, Color.White, (float)Math.PI * 3f / 4f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
							}
							break;
						}
						if (f.FacingDirection == 0)
						{
							f.FarmerRenderer.draw(spriteBatch, f.FarmerSprite, f.FarmerSprite.SourceRect, f.getLocalPosition(Game1.viewport), new Vector2(0f, (f.yOffset + 128f - (float)(f.GetBoundingBox().Height / 2)) / 4f + 4f), layerDepth2, Color.White, 0f, f);
						}
						break;
					}
					break;
				}
				return;
			}
			switch (facingDirection)
			{
			case 1:
				switch (frameOfFarmerAnimation)
				{
				case 0:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 40f, playerPosition.Y - 64f + 8f), sourceRect, Color.White, -(float)Math.PI / 4f, center, 4f, SpriteEffects.None, layerDepth);
					break;
				case 1:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 56f, playerPosition.Y - 64f + 28f), sourceRect, Color.White, 0f, center, 4f, SpriteEffects.None, layerDepth);
					break;
				case 2:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 4f, playerPosition.Y - 16f), sourceRect, Color.White, (float)Math.PI / 4f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 3:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 4f, playerPosition.Y - 4f), sourceRect, Color.White, (float)Math.PI / 2f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 4:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 28f, playerPosition.Y + 4f), sourceRect, Color.White, (float)Math.PI * 5f / 8f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 5:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 48f, playerPosition.Y + 4f), sourceRect, Color.White, (float)Math.PI * 3f / 4f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 6:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 48f, playerPosition.Y + 4f), sourceRect, Color.White, (float)Math.PI * 3f / 4f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 7:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 16f, playerPosition.Y + 64f + 12f), sourceRect, Color.White, 1.9634954f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				}
				break;
			case 3:
				switch (frameOfFarmerAnimation)
				{
				case 0:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X - 16f, playerPosition.Y - 64f - 16f), sourceRect, Color.White, (float)Math.PI / 4f, center, 4f, SpriteEffects.FlipHorizontally, layerDepth);
					break;
				case 1:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X - 48f, playerPosition.Y - 64f + 20f), sourceRect, Color.White, 0f, center, 4f, SpriteEffects.FlipHorizontally, layerDepth);
					break;
				case 2:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X - 64f + 32f, playerPosition.Y + 16f), sourceRect, Color.White, -(float)Math.PI / 4f, center, 4f, SpriteEffects.FlipHorizontally, layerDepth2);
					break;
				case 3:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 4f, playerPosition.Y + 44f), sourceRect, Color.White, -(float)Math.PI / 2f, center, 4f, SpriteEffects.FlipHorizontally, layerDepth2);
					break;
				case 4:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 44f, playerPosition.Y + 52f), sourceRect, Color.White, (float)Math.PI * -5f / 8f, center, 4f, SpriteEffects.FlipHorizontally, layerDepth2);
					break;
				case 5:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 80f, playerPosition.Y + 40f), sourceRect, Color.White, (float)Math.PI * -3f / 4f, center, 4f, SpriteEffects.FlipHorizontally, layerDepth2);
					break;
				case 6:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 80f, playerPosition.Y + 40f), sourceRect, Color.White, (float)Math.PI * -3f / 4f, center, 4f, SpriteEffects.FlipHorizontally, layerDepth2);
					break;
				case 7:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X - 44f, playerPosition.Y + 96f), sourceRect, Color.White, -5.105088f, center, 4f, SpriteEffects.FlipVertically, layerDepth2);
					break;
				}
				break;
			case 0:
				switch (frameOfFarmerAnimation)
				{
				case 0:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 32f, playerPosition.Y - 32f), sourceRect, Color.White, (float)Math.PI * -3f / 4f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 1:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 32f, playerPosition.Y - 48f), sourceRect, Color.White, -(float)Math.PI / 2f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 2:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 48f, playerPosition.Y - 52f), sourceRect, Color.White, (float)Math.PI * -3f / 8f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 3:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 48f, playerPosition.Y - 52f), sourceRect, Color.White, -(float)Math.PI / 8f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 4:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 8f, playerPosition.Y - 40f), sourceRect, Color.White, 0f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 5:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f, playerPosition.Y - 40f), sourceRect, Color.White, (float)Math.PI / 8f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 6:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f, playerPosition.Y - 40f), sourceRect, Color.White, (float)Math.PI / 8f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 7:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 44f, playerPosition.Y + 64f), sourceRect, Color.White, -1.9634954f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				}
				break;
			case 2:
				switch (frameOfFarmerAnimation)
				{
				case 0:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 56f, playerPosition.Y - 16f), sourceRect, Color.White, (float)Math.PI / 8f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 1:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 52f, playerPosition.Y - 8f), sourceRect, Color.White, (float)Math.PI / 2f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 2:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 40f, playerPosition.Y), sourceRect, Color.White, (float)Math.PI / 2f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 3:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 16f, playerPosition.Y + 4f), sourceRect, Color.White, (float)Math.PI * 3f / 4f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 4:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 8f, playerPosition.Y + 8f), sourceRect, Color.White, (float)Math.PI, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 5:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 12f, playerPosition.Y), sourceRect, Color.White, 3.5342917f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 6:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 12f, playerPosition.Y), sourceRect, Color.White, 3.5342917f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				case 7:
					spriteBatch.Draw(texture, new Vector2(playerPosition.X + 44f, playerPosition.Y + 64f), sourceRect, Color.White, -5.105088f, center, 4f, SpriteEffects.None, layerDepth2);
					break;
				}
				break;
			}
			return;
		}
		frameOfFarmerAnimation %= 2;
		switch (facingDirection)
		{
		case 1:
			switch (frameOfFarmerAnimation)
			{
			case 0:
				spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 16f, playerPosition.Y - 16f), sourceRect, Color.White, (float)Math.PI / 4f, center, 4f, SpriteEffects.None, layerDepth2);
				break;
			case 1:
				spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 8f, playerPosition.Y - 24f), sourceRect, Color.White, (float)Math.PI / 4f, center, 4f, SpriteEffects.None, layerDepth2);
				break;
			}
			break;
		case 3:
			switch (frameOfFarmerAnimation)
			{
			case 0:
				spriteBatch.Draw(texture, new Vector2(playerPosition.X + 16f, playerPosition.Y - 16f), sourceRect, Color.White, (float)Math.PI * -3f / 4f, center, 4f, SpriteEffects.None, layerDepth2);
				break;
			case 1:
				spriteBatch.Draw(texture, new Vector2(playerPosition.X + 8f, playerPosition.Y - 24f), sourceRect, Color.White, (float)Math.PI * -3f / 4f, center, 4f, SpriteEffects.None, layerDepth2);
				break;
			}
			break;
		case 0:
			switch (frameOfFarmerAnimation)
			{
			case 0:
				spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 4f, playerPosition.Y - 40f), sourceRect, Color.White, -(float)Math.PI / 4f, center, 4f, SpriteEffects.None, layerDepth2);
				break;
			case 1:
				spriteBatch.Draw(texture, new Vector2(playerPosition.X + 64f - 16f, playerPosition.Y - 48f), sourceRect, Color.White, -(float)Math.PI / 4f, center, 4f, SpriteEffects.None, layerDepth2);
				break;
			}
			break;
		case 2:
			switch (frameOfFarmerAnimation)
			{
			case 0:
				spriteBatch.Draw(texture, new Vector2(playerPosition.X + 32f, playerPosition.Y - 8f), sourceRect, Color.White, (float)Math.PI * 3f / 4f, center, 4f, SpriteEffects.None, layerDepth2);
				break;
			case 1:
				spriteBatch.Draw(texture, new Vector2(playerPosition.X + 21f, playerPosition.Y + 20f), sourceRect, Color.White, (float)Math.PI * 3f / 4f, center, 4f, SpriteEffects.None, layerDepth2);
				break;
			}
			break;
		}
	}
}
