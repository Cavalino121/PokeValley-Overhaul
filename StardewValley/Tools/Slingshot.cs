using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Projectiles;

namespace StardewValley.Tools;

public class Slingshot : Tool
{
	public const int basicDamage = 5;

	public const string basicSlingshotId = "32";

	public const string masterSlingshotId = "33";

	public const string galaxySlingshotId = "34";

	public const int drawBackSoundThreshold = 8;

	[XmlIgnore]
	public int lastClickX;

	[XmlIgnore]
	public int lastClickY;

	[XmlIgnore]
	public int mouseDragAmount;

	[XmlIgnore]
	public double pullStartTime = -1.0;

	[XmlIgnore]
	public float nextAutoFire = -1f;

	[XmlIgnore]
	public bool canPlaySound;

	[XmlIgnore]
	private readonly NetEvent0 finishEvent = new NetEvent0();

	[XmlIgnore]
	public readonly NetPoint aimPos = new NetPoint().Interpolated(interpolate: true, wait: true);

	/// <inheritdoc />
	public override string TypeDefinitionId { get; } = "(W)";

	public Slingshot()
		: this("32")
	{
	}

	/// <inheritdoc />
	protected override void MigrateLegacyItemId()
	{
		base.ItemId = base.InitialParentTileIndex.ToString();
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Slingshot(base.ItemId);
	}

	protected override string loadDisplayName()
	{
		return ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).DisplayName;
	}

	protected override string loadDescription()
	{
		return ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).Description;
	}

	public override bool doesShowTileLocationMarker()
	{
		return false;
	}

	public Slingshot(string itemId = "32")
	{
		itemId = ValidateUnqualifiedItemId(itemId);
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(W)" + itemId);
		base.ItemId = itemId;
		Name = dataOrErrorItem.InternalName;
		base.InitialParentTileIndex = dataOrErrorItem.SpriteIndex;
		base.CurrentParentTileIndex = dataOrErrorItem.SpriteIndex;
		base.IndexOfMenuItemView = dataOrErrorItem.SpriteIndex;
		numAttachmentSlots.Value = 1;
		attachments.SetCount(1);
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(finishEvent, "finishEvent").AddField(aimPos, "aimPos");
		finishEvent.onEvent += doFinish;
	}

	public int GetBackArmDistance(Farmer who)
	{
		if (CanAutoFire() && nextAutoFire > 0f)
		{
			return (int)Utility.Lerp(20f, 0f, nextAutoFire / GetAutoFireRate());
		}
		if (!Game1.options.useLegacySlingshotFiring)
		{
			return (int)(20f * GetSlingshotChargeTime());
		}
		return Math.Min(20, (int)Vector2.Distance(who.getStandingPosition(), new Vector2(aimPos.X, aimPos.Y)) / 20);
	}

	public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
	{
		base.IndexOfMenuItemView = base.InitialParentTileIndex;
		if (!CanAutoFire())
		{
			PerformFire(location, who);
		}
		finish();
	}

	public virtual void PerformFire(GameLocation location, Farmer who)
	{
		Object obj = attachments[0];
		if (obj != null)
		{
			updateAimPos();
			int x = aimPos.X;
			int y = aimPos.Y;
			int backArmDistance = GetBackArmDistance(who);
			Vector2 shootOrigin = GetShootOrigin(who);
			Vector2 velocityTowardPoint = Utility.getVelocityTowardPoint(GetShootOrigin(who), AdjustForHeight(new Vector2(x, y)), (float)(15 + Game1.random.Next(4, 6)) * (1f + who.buffs.WeaponSpeedMultiplier));
			if (backArmDistance > 4 && !canPlaySound)
			{
				Object obj2 = (Object)obj.getOne();
				if (obj.ConsumeStack(1) == null)
				{
					attachments[0] = null;
				}
				string text = base.ItemId;
				float num = ((text == "33") ? 2f : ((!(text == "34")) ? 1f : 4f));
				int ammoDamage = GetAmmoDamage(obj2);
				string ammoCollisionSound = GetAmmoCollisionSound(obj2);
				BasicProjectile.onCollisionBehavior ammoCollisionBehavior = GetAmmoCollisionBehavior(obj2);
				if (!Game1.options.useLegacySlingshotFiring)
				{
					velocityTowardPoint.X *= -1f;
					velocityTowardPoint.Y *= -1f;
				}
				location.projectiles.Add(new BasicProjectile((int)(num * (float)(ammoDamage + Game1.random.Next(-(ammoDamage / 2), ammoDamage + 2)) * (1f + who.buffs.AttackMultiplier)), -1, 0, 0, (float)(Math.PI / (double)(64f + (float)Game1.random.Next(-63, 64))), 0f - velocityTowardPoint.X, 0f - velocityTowardPoint.Y, shootOrigin - new Vector2(32f, 32f), ammoCollisionSound, null, null, explode: false, damagesMonsters: true, location, who, ammoCollisionBehavior, obj2.ItemId)
				{
					IgnoreLocationCollision = (Game1.currentLocation.currentEvent != null || Game1.currentMinigame != null)
				});
			}
		}
		else
		{
			Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Slingshot.cs.14254"));
		}
		canPlaySound = true;
	}

	/// <summary>Get the damage inflicted by shooting an ammunition item, excluding the slingshot upgrade level multiplier.</summary>
	/// <param name="ammunition">The item that was shot.</param>
	public virtual int GetAmmoDamage(Object ammunition)
	{
		return ammunition?.QualifiedItemId switch
		{
			"(O)388" => 2, 
			"(O)390" => 5, 
			"(O)378" => 10, 
			"(O)380" => 20, 
			"(O)384" => 30, 
			"(O)382" => 15, 
			"(O)386" => 50, 
			"(O)441" => 20, 
			_ => 1, 
		};
	}

	/// <summary>Get the sound to play when the ammunition item hits.</summary>
	/// <param name="ammunition">The item that was shot.</param>
	public virtual string GetAmmoCollisionSound(Object ammunition)
	{
		if (ammunition?.QualifiedItemId == "(O)441")
		{
			return "explosion";
		}
		if (ammunition != null && ammunition.Category == -5)
		{
			return "slimedead";
		}
		return "hammer";
	}

	/// <summary>Get the logic to apply when the ammunition item hits.</summary>
	/// <param name="ammunition">The item that was shot.</param>
	public virtual BasicProjectile.onCollisionBehavior GetAmmoCollisionBehavior(Object ammunition)
	{
		if (ammunition.QualifiedItemId == "(O)441")
		{
			return BasicProjectile.explodeOnImpact;
		}
		return null;
	}

	public Vector2 GetShootOrigin(Farmer who)
	{
		return AdjustForHeight(who.getStandingPosition(), for_cursor: false);
	}

	public Vector2 AdjustForHeight(Vector2 position, bool for_cursor = true)
	{
		if (!Game1.options.useLegacySlingshotFiring && for_cursor)
		{
			return new Vector2(position.X, position.Y);
		}
		return new Vector2(position.X, position.Y - 32f - 8f);
	}

	public void finish()
	{
		finishEvent.Fire();
	}

	private void doFinish()
	{
		if (lastUser != null)
		{
			lastUser.usingSlingshot = false;
			lastUser.canReleaseTool = true;
			lastUser.UsingTool = false;
			lastUser.canMove = true;
			lastUser.Halt();
			if (lastUser == Game1.player && Game1.options.gamepadControls)
			{
				Game1.game1.controllerSlingshotSafeTime = 0.2f;
			}
		}
	}

	/// <inheritdoc />
	protected override bool canThisBeAttached(Object o, int slot)
	{
		switch (o.QualifiedItemId)
		{
		case "(O)378":
		case "(O)388":
		case "(O)380":
		case "(O)390":
		case "(O)382":
		case "(O)384":
		case "(O)386":
		case "(O)441":
			return true;
		default:
			if (!o.bigCraftable.Value)
			{
				if (o.Category != -5 && o.Category != -79)
				{
					return o.Category == -75;
				}
				return true;
			}
			return false;
		}
	}

	public override string getHoverBoxText(Item hoveredItem)
	{
		if (hoveredItem is Object obj && canThisBeAttached(obj))
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Slingshot.cs.14256", DisplayName, obj.DisplayName);
		}
		if (hoveredItem == null && attachments?[0] != null)
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:Slingshot.cs.14258", attachments[0].DisplayName);
		}
		return null;
	}

	public override bool onRelease(GameLocation location, int x, int y, Farmer who)
	{
		DoFunction(location, x, y, 1, who);
		return true;
	}

	public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
	{
		who.usingSlingshot = true;
		who.canReleaseTool = false;
		mouseDragAmount = 0;
		int num = ((who.FacingDirection == 3 || who.FacingDirection == 1) ? 1 : ((who.FacingDirection == 0) ? 2 : 0));
		who.FarmerSprite.setCurrentFrame(42 + num);
		if (!who.IsLocalPlayer)
		{
			return true;
		}
		Game1.oldMouseState = Game1.input.GetMouseState();
		Game1.lastMousePositionBeforeFade = Game1.getMousePosition();
		lastClickX = Game1.getOldMouseX() + Game1.viewport.X;
		lastClickY = Game1.getOldMouseY() + Game1.viewport.Y;
		pullStartTime = Game1.currentGameTime.TotalGameTime.TotalSeconds;
		if (CanAutoFire())
		{
			nextAutoFire = -1f;
		}
		updateAimPos();
		return true;
	}

	public virtual float GetAutoFireRate()
	{
		return 0.3f;
	}

	public virtual bool CanAutoFire()
	{
		return false;
	}

	private void updateAimPos()
	{
		if (lastUser == null || !lastUser.IsLocalPlayer)
		{
			return;
		}
		Point point = Game1.getMousePosition();
		if (Game1.options.gamepadControls && !Game1.lastCursorMotionWasMouse)
		{
			Vector2 vector = Game1.oldPadState.ThumbSticks.Left;
			if (vector.Length() < 0.25f)
			{
				vector.X = 0f;
				vector.Y = 0f;
				if (Game1.oldPadState.DPad.Down == ButtonState.Pressed)
				{
					vector.Y = -1f;
				}
				else if (Game1.oldPadState.DPad.Up == ButtonState.Pressed)
				{
					vector.Y = 1f;
				}
				if (Game1.oldPadState.DPad.Left == ButtonState.Pressed)
				{
					vector.X = -1f;
				}
				if (Game1.oldPadState.DPad.Right == ButtonState.Pressed)
				{
					vector.X = 1f;
				}
				if (vector.X != 0f && vector.Y != 0f)
				{
					vector.Normalize();
					vector *= 1f;
				}
			}
			Vector2 shootOrigin = GetShootOrigin(lastUser);
			if (!Game1.options.useLegacySlingshotFiring && vector.Length() < 0.25f)
			{
				switch (lastUser.FacingDirection)
				{
				case 3:
					vector = new Vector2(-1f, 0f);
					break;
				case 1:
					vector = new Vector2(1f, 0f);
					break;
				case 0:
					vector = new Vector2(0f, 1f);
					break;
				case 2:
					vector = new Vector2(0f, -1f);
					break;
				}
			}
			point = Utility.Vector2ToPoint(shootOrigin + new Vector2(vector.X, 0f - vector.Y) * 600f);
			point.X -= Game1.viewport.X;
			point.Y -= Game1.viewport.Y;
		}
		int x = point.X + Game1.viewport.X;
		int y = point.Y + Game1.viewport.Y;
		aimPos.X = x;
		aimPos.Y = y;
	}

	public override void tickUpdate(GameTime time, Farmer who)
	{
		lastUser = who;
		finishEvent.Poll();
		if (!who.usingSlingshot)
		{
			return;
		}
		if (who.IsLocalPlayer)
		{
			updateAimPos();
			int x = aimPos.X;
			int y = aimPos.Y;
			mouseDragAmount++;
			if (!Game1.options.useLegacySlingshotFiring)
			{
				Vector2 shootOrigin = GetShootOrigin(who);
				Vector2 vector = AdjustForHeight(new Vector2(x, y)) - shootOrigin;
				if (Math.Abs(vector.X) > Math.Abs(vector.Y))
				{
					if (vector.X < 0f)
					{
						who.faceDirection(3);
					}
					if (vector.X > 0f)
					{
						who.faceDirection(1);
					}
				}
				else
				{
					if (vector.Y < 0f)
					{
						who.faceDirection(0);
					}
					if (vector.Y > 0f)
					{
						who.faceDirection(2);
					}
				}
			}
			else
			{
				who.faceGeneralDirection(new Vector2(x, y), 0, opposite: true);
			}
			if (!Game1.options.useLegacySlingshotFiring)
			{
				if (canPlaySound && GetSlingshotChargeTime() >= 1f)
				{
					if (PlayUseSounds)
					{
						who.playNearbySoundAll("slingshot");
					}
					canPlaySound = false;
				}
			}
			else if (canPlaySound && (Math.Abs(x - lastClickX) > 8 || Math.Abs(y - lastClickY) > 8) && mouseDragAmount > 4)
			{
				if (PlayUseSounds)
				{
					who.playNearbySoundAll("slingshot");
				}
				canPlaySound = false;
			}
			if (!CanAutoFire())
			{
				lastClickX = x;
				lastClickY = y;
			}
			if (Game1.options.useLegacySlingshotFiring)
			{
				Game1.mouseCursor = Game1.cursor_none;
			}
			if (CanAutoFire())
			{
				bool flag = false;
				if (GetBackArmDistance(who) >= 20 && nextAutoFire < 0f)
				{
					nextAutoFire = 0f;
					flag = true;
				}
				if (nextAutoFire > 0f || flag)
				{
					nextAutoFire -= (float)time.ElapsedGameTime.TotalSeconds;
					if (nextAutoFire <= 0f)
					{
						PerformFire(who.currentLocation, who);
						nextAutoFire = GetAutoFireRate();
					}
				}
			}
		}
		int num = ((who.FacingDirection == 3 || who.FacingDirection == 1) ? 1 : ((who.FacingDirection == 0) ? 2 : 0));
		who.FarmerSprite.setCurrentFrame(42 + num);
	}

	/// <inheritdoc />
	protected override void GetAttachmentSlotSprite(int slot, out Texture2D texture, out Rectangle sourceRect)
	{
		base.GetAttachmentSlotSprite(slot, out texture, out sourceRect);
		if (attachments[0] == null)
		{
			sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 43);
		}
	}

	public float GetSlingshotChargeTime()
	{
		if (pullStartTime < 0.0)
		{
			return 0f;
		}
		return Utility.Clamp((float)((Game1.currentGameTime.TotalGameTime.TotalSeconds - pullStartTime) / (double)GetRequiredChargeTime()), 0f, 1f);
	}

	public float GetRequiredChargeTime()
	{
		return 0.3f;
	}

	public override void draw(SpriteBatch b)
	{
		if (lastUser.usingSlingshot && lastUser.IsLocalPlayer)
		{
			int x = aimPos.X;
			int y = aimPos.Y;
			Vector2 shootOrigin = GetShootOrigin(lastUser);
			Vector2 velocityTowardPoint = Utility.getVelocityTowardPoint(shootOrigin, AdjustForHeight(new Vector2(x, y)), 256f);
			double num = Math.Sqrt(velocityTowardPoint.X * velocityTowardPoint.X + velocityTowardPoint.Y * velocityTowardPoint.Y) - 181.0;
			double num2 = velocityTowardPoint.X / 256f;
			double num3 = velocityTowardPoint.Y / 256f;
			int num4 = (int)((double)velocityTowardPoint.X - num * num2);
			int num5 = (int)((double)velocityTowardPoint.Y - num * num3);
			if (!Game1.options.useLegacySlingshotFiring)
			{
				num4 *= -1;
				num5 *= -1;
			}
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(shootOrigin.X - (float)num4, shootOrigin.Y - (float)num5)), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 43), Color.White, 0f, new Vector2(32f, 32f), 1f, SpriteEffects.None, 0.999999f);
		}
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
		if (base.IndexOfMenuItemView == 0 || base.IndexOfMenuItemView == 21 || base.ItemId == "47")
		{
			switch (Name)
			{
			case "Slingshot":
				base.CurrentParentTileIndex = int.Parse("32");
				break;
			case "Master Slingshot":
				base.CurrentParentTileIndex = int.Parse("33");
				break;
			case "Galaxy Slingshot":
				base.CurrentParentTileIndex = int.Parse("34");
				break;
			}
			base.IndexOfMenuItemView = base.CurrentParentTileIndex;
		}
		spriteBatch.Draw(Tool.weaponsTexture, location + new Vector2(32f, 29f), Game1.getSourceRectForStandardTileSheet(Tool.weaponsTexture, base.IndexOfMenuItemView, 16, 16), color * transparency, 0f, new Vector2(8f, 8f), scaleSize * 4f, SpriteEffects.None, layerDepth);
		if (drawStackNumber != StackDrawType.Hide && attachments?[0] != null)
		{
			Utility.drawTinyDigits(attachments[0].Stack, spriteBatch, location + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(attachments[0].Stack, 3f * scaleSize)) + 3f * scaleSize, 64f - 18f * scaleSize + 2f), 3f * scaleSize, 1f, Color.White);
		}
		DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
	}
}
