using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Rewards;
using StardewValley.TokenizableStrings;

namespace StardewValley.Menus;

public class SpecialOrdersBoard : IClickableMenu
{
	private Texture2D billboardTexture;

	public const int basewidth = 338;

	public const int baseheight = 198;

	public ClickableComponent acceptLeftQuestButton;

	public ClickableComponent acceptRightQuestButton;

	public string boardType = "";

	public SpecialOrder leftOrder;

	public SpecialOrder rightOrder;

	public string[] emojiIndices = new string[38]
	{
		"Abigail", "Penny", "Maru", "Leah", "Haley", "Emily", "Alex", "Shane", "Sebastian", "Sam",
		"Harvey", "Elliott", "Sandy", "Evelyn", "Marnie", "Caroline", "Robin", "Pierre", "Pam", "Jodi",
		"Lewis", "Linus", "Marlon", "Willy", "Wizard", "Morris", "Jas", "Vincent", "Krobus", "Dwarf",
		"Gus", "Gunther", "George", "Demetrius", "Clint", "Baby", "Baby", "Bear"
	};

	public SpecialOrdersBoard(string board_type = "")
		: base(0, 0, 0, 0, showUpperRightCloseButton: true)
	{
		SpecialOrder.UpdateAvailableSpecialOrders(board_type, forceRefresh: false);
		boardType = board_type;
		if (boardType == "Qi")
		{
			billboardTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\SpecialOrdersBoard");
		}
		else
		{
			billboardTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\SpecialOrdersBoard");
		}
		width = 1352;
		height = 792;
		Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
		xPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.X;
		yPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.Y;
		acceptLeftQuestButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width / 4 - 128, yPositionOnScreen + height - 128, (int)Game1.dialogueFont.MeasureString(Game1.content.LoadString("Strings\\UI:AcceptQuest")).X + 24, (int)Game1.dialogueFont.MeasureString(Game1.content.LoadString("Strings\\UI:AcceptQuest")).Y + 24), "")
		{
			myID = 0,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			upNeighborID = -99998,
			downNeighborID = -99998
		};
		acceptRightQuestButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width * 3 / 4 - 128, yPositionOnScreen + height - 128, (int)Game1.dialogueFont.MeasureString(Game1.content.LoadString("Strings\\UI:AcceptQuest")).X + 24, (int)Game1.dialogueFont.MeasureString(Game1.content.LoadString("Strings\\UI:AcceptQuest")).Y + 24), "")
		{
			myID = 1,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			upNeighborID = -99998,
			downNeighborID = -99998
		};
		leftOrder = Game1.player.team.GetAvailableSpecialOrder(0, GetOrderType());
		rightOrder = Game1.player.team.GetAvailableSpecialOrder(1, GetOrderType());
		upperRightCloseButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 20, yPositionOnScreen, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);
		Game1.playSound("bigSelect");
		UpdateButtons();
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	public virtual void UpdateButtons()
	{
		if (leftOrder == null)
		{
			acceptLeftQuestButton.visible = false;
		}
		if (rightOrder == null)
		{
			acceptRightQuestButton.visible = false;
		}
		if (Game1.player.team.acceptedSpecialOrderTypes.Contains(GetOrderType()))
		{
			acceptLeftQuestButton.visible = false;
			acceptRightQuestButton.visible = false;
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(0);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		Game1.activeClickableMenu = new SpecialOrdersBoard(boardType);
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		Game1.playSound("bigDeSelect");
		exitThisMenu();
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);
		if (acceptLeftQuestButton.visible && acceptLeftQuestButton.containsPoint(x, y))
		{
			Game1.playSound("newArtifact");
			if (leftOrder != null)
			{
				Game1.player.team.acceptedSpecialOrderTypes.Add(GetOrderType());
				SpecialOrder specialOrder = leftOrder;
				Game1.player.team.AddSpecialOrder(specialOrder.questKey.Value, specialOrder.generationSeed.Value);
				Game1.multiplayer.globalChatInfoMessage("AcceptedSpecialOrder", Game1.player.Name, TokenStringBuilder.SpecialOrderName(specialOrder.questKey.Value));
				UpdateButtons();
			}
		}
		else if (acceptRightQuestButton.visible && acceptRightQuestButton.containsPoint(x, y))
		{
			Game1.playSound("newArtifact");
			if (rightOrder != null)
			{
				Game1.player.team.acceptedSpecialOrderTypes.Add(GetOrderType());
				SpecialOrder specialOrder2 = rightOrder;
				Game1.player.team.AddSpecialOrder(specialOrder2.questKey.Value, specialOrder2.generationSeed.Value);
				Game1.multiplayer.globalChatInfoMessage("AcceptedSpecialOrder", Game1.player.Name, TokenStringBuilder.SpecialOrderName(specialOrder2.questKey.Value));
				UpdateButtons();
			}
		}
	}

	public string GetOrderType()
	{
		return boardType;
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		if (Game1.questOfTheDay != null && !Game1.questOfTheDay.accepted.Value)
		{
			float scale = acceptLeftQuestButton.scale;
			acceptLeftQuestButton.scale = (acceptLeftQuestButton.bounds.Contains(x, y) ? 1.5f : 1f);
			if (acceptLeftQuestButton.scale > scale)
			{
				Game1.playSound("Cowboy_gunshot");
			}
			scale = acceptRightQuestButton.scale;
			acceptRightQuestButton.scale = (acceptRightQuestButton.bounds.Contains(x, y) ? 1.5f : 1f);
			if (acceptRightQuestButton.scale > scale)
			{
				Game1.playSound("Cowboy_gunshot");
			}
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (!Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
		}
		b.Draw(billboardTexture, new Vector2(xPositionOnScreen, yPositionOnScreen), new Rectangle(0, (boardType == "Qi") ? 198 : 0, 338, 198), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		if (leftOrder != null && leftOrder.IsIslandOrder())
		{
			b.Draw(billboardTexture, new Vector2(xPositionOnScreen, yPositionOnScreen), new Rectangle(338, 0, 169, 198), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		}
		if (rightOrder != null && rightOrder.IsIslandOrder())
		{
			b.Draw(billboardTexture, new Vector2(xPositionOnScreen + 676, yPositionOnScreen), new Rectangle(507, 0, 169, 198), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		}
		if (!Game1.player.team.acceptedSpecialOrderTypes.Contains(GetOrderType()))
		{
			SpriteText.drawStringWithScrollCenteredAt(b, Game1.content.LoadString("Strings\\UI:ChooseOne"), xPositionOnScreen + width / 2, Math.Max(10, yPositionOnScreen - 70), SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\UI:ChooseOne") + "W"));
		}
		if (leftOrder != null)
		{
			SpecialOrder order = leftOrder;
			DrawQuestDetails(b, order, xPositionOnScreen + 64 + 32);
		}
		if (rightOrder != null)
		{
			SpecialOrder order2 = rightOrder;
			DrawQuestDetails(b, order2, xPositionOnScreen + 704 + 32);
		}
		if (acceptLeftQuestButton.visible)
		{
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9), acceptLeftQuestButton.bounds.X, acceptLeftQuestButton.bounds.Y, acceptLeftQuestButton.bounds.Width, acceptLeftQuestButton.bounds.Height, (acceptLeftQuestButton.scale > 1f) ? Color.LightPink : Color.White, 4f * acceptLeftQuestButton.scale);
			Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:AcceptQuest"), Game1.dialogueFont, new Vector2(acceptLeftQuestButton.bounds.X + 12, acceptLeftQuestButton.bounds.Y + (LocalizedContentManager.CurrentLanguageLatin ? 16 : 12)), Game1.textColor);
		}
		if (acceptRightQuestButton.visible)
		{
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9), acceptRightQuestButton.bounds.X, acceptRightQuestButton.bounds.Y, acceptRightQuestButton.bounds.Width, acceptRightQuestButton.bounds.Height, (acceptRightQuestButton.scale > 1f) ? Color.LightPink : Color.White, 4f * acceptRightQuestButton.scale);
			Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:AcceptQuest"), Game1.dialogueFont, new Vector2(acceptRightQuestButton.bounds.X + 12, acceptRightQuestButton.bounds.Y + (LocalizedContentManager.CurrentLanguageLatin ? 16 : 12)), Game1.textColor);
		}
		base.draw(b);
		Game1.mouseCursorTransparency = 1f;
		if (!Game1.options.SnappyMenus || acceptLeftQuestButton.visible || acceptRightQuestButton.visible)
		{
			drawMouse(b);
		}
	}

	public KeyValuePair<Texture2D, Rectangle>? GetPortraitForRequester(string requester_name)
	{
		if (requester_name == null)
		{
			return null;
		}
		for (int i = 0; i < emojiIndices.Length; i++)
		{
			if (emojiIndices[i] == requester_name)
			{
				return new KeyValuePair<Texture2D, Rectangle>(ChatBox.emojiTexture, new Rectangle(i % 14 * 9, 99 + i / 14 * 9, 9, 9));
			}
		}
		return null;
	}

	public void DrawQuestDetails(SpriteBatch b, SpecialOrder order, int x)
	{
		bool flag = false;
		bool flag2 = false;
		foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
		{
			if (specialOrder.questState.Value != SpecialOrderStatus.InProgress)
			{
				continue;
			}
			foreach (SpecialOrder availableSpecialOrder in Game1.player.team.availableSpecialOrders)
			{
				if (!(availableSpecialOrder.orderType.Value != GetOrderType()) && specialOrder.questKey.Value == availableSpecialOrder.questKey.Value)
				{
					if (order.questKey.Value != specialOrder.questKey.Value)
					{
						flag = true;
					}
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				break;
			}
		}
		if (!flag2 && Game1.player.team.acceptedSpecialOrderTypes.Contains(GetOrderType()))
		{
			flag = true;
		}
		SpriteFont spriteFont = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko) ? Game1.smallFont : Game1.dialogueFont);
		Color color = Game1.textColor;
		float num = 0.5f;
		float num2 = 1f;
		if (flag)
		{
			color = Game1.textColor * 0.25f;
			num = 0f;
			num2 = 0.25f;
		}
		if (boardType == "Qi")
		{
			color = Color.White;
			num = 0f;
			if (flag)
			{
				color = Color.White * 0.25f;
				num2 = 0.25f;
			}
		}
		int num3 = yPositionOnScreen + 128;
		string name = order.GetName();
		KeyValuePair<Texture2D, Rectangle>? portraitForRequester = GetPortraitForRequester(order.requester.Value);
		if (portraitForRequester.HasValue)
		{
			Utility.drawWithShadow(b, portraitForRequester.Value.Key, new Vector2(x, num3), portraitForRequester.Value.Value, Color.White * num2, 0f, Vector2.Zero, 4f, flipped: false, -1f, -1, -1, num * 0.6f);
		}
		Utility.drawTextWithShadow(b, name, spriteFont, new Vector2((float)(x + 256) - spriteFont.MeasureString(name).X / 2f, num3), color, 1f, -1f, -1, -1, num);
		if (boardType == "" && Game1.player.team.completedSpecialOrders.Contains(order.questKey.Value))
		{
			b.Draw(Game1.mouseCursors_1_6, new Vector2(x, (float)yPositionOnScreen + 576f + 32f + 8f), new Rectangle(404, 213, 11, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
		}
		string description = order.GetDescription();
		string text = Game1.parseText(description, spriteFont, 512);
		float y = spriteFont.MeasureString(text).Y;
		float num4 = 1f;
		float num5 = 400f;
		while (y > num5 && !(num4 <= 0.25f))
		{
			num4 -= 0.05f;
			text = Game1.parseText(description, spriteFont, (int)(512f / num4));
			y = spriteFont.MeasureString(text).Y;
		}
		Utility.drawTextWithShadow(b, text, spriteFont, new Vector2(x, yPositionOnScreen + 192), color, num4, -1f, -1, -1, num);
		if (flag)
		{
			return;
		}
		int daysLeft = order.GetDaysLeft();
		int num6 = yPositionOnScreen + 576;
		Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x, num6), new Rectangle(410, 501, 9, 9), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 0.99f, -1, -1, num * 0.6f);
		Utility.drawTextWithShadow(b, Game1.parseText((daysLeft > 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestLog.cs.11374", daysLeft) : Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestLog.cs.11375", daysLeft), Game1.dialogueFont, width - 128), Game1.dialogueFont, new Vector2(x + 48, num6), color, 1f, -1f, -1, -1, num);
		if (boardType == "Qi")
		{
			int num7 = -1;
			GemsReward gemsReward = null;
			foreach (OrderReward reward in order.rewards)
			{
				if (reward is GemsReward gemsReward2)
				{
					gemsReward = gemsReward2;
					break;
				}
			}
			if (gemsReward != null)
			{
				num7 = gemsReward.amount.Value;
			}
			if (num7 != -1)
			{
				Utility.drawWithShadow(b, Game1.objectSpriteSheet, new Vector2((float)x + 512f / num4 - Game1.dialogueFont.MeasureString(num7.ToString() ?? "").X - 12f - 60f, num6 - 8), new Rectangle(288, 561, 15, 15), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 0.99f, -1, -1, num * 0.6f);
				Utility.drawTextWithShadow(b, Game1.parseText(num7.ToString() ?? "", Game1.dialogueFont, width - 128), Game1.dialogueFont, new Vector2((float)x + 512f / num4 - Game1.dialogueFont.MeasureString(num7.ToString() ?? "").X - 4f, num6), color, 1f, -1f, -1, -1, num);
				Utility.drawTextWithShadow(b, Game1.parseText(Utility.loadStringShort("StringsFromCSFiles", "QuestLog.cs.11376"), Game1.dialogueFont, width - 128), Game1.dialogueFont, new Vector2((float)x + 512f / num4 - Game1.dialogueFont.MeasureString(Utility.loadStringShort("StringsFromCSFiles", "QuestLog.cs.11376")).X + 8f, num6 - 60), color * 0.6f, 1f, -1f, -1, -1, num);
			}
			return;
		}
		Object obj = null;
		foreach (OrderReward reward2 in order.rewards)
		{
			if (reward2 is ObjectReward objectReward)
			{
				obj = objectReward.objectInstance;
				break;
			}
		}
		if (obj != null)
		{
			Utility.drawWithShadow(b, ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId).GetTexture(), new Vector2((float)x + 512f / num4 - Game1.dialogueFont.MeasureString(obj.Stack.ToString() ?? "").X - 12f - 60f, num6 - 8), ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId).GetSourceRect(), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 0.99f, -1, -1, num * 0.6f);
			Utility.drawTextWithShadow(b, Game1.parseText(obj.Stack.ToString() ?? "", Game1.dialogueFont, width - 128), Game1.dialogueFont, new Vector2((float)x + 512f / num4 - Game1.dialogueFont.MeasureString(obj.Stack.ToString() ?? "").X - 4f, num6), color, 1f, -1f, -1, -1, num);
			Utility.drawTextWithShadow(b, Game1.parseText(ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId).DisplayName, Game1.dialogueFont, width - 128), Game1.dialogueFont, new Vector2((float)x + 512f / num4 - Game1.dialogueFont.MeasureString(ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId).DisplayName).X + 8f, num6 - 60), color * 0.6f, 1f, -1f, -1, -1, num);
		}
	}
}
