using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Quests;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;

namespace StardewValley.Menus;

public class QuestLog : IClickableMenu
{
	public const int questsPerPage = 6;

	public const int region_forwardButton = 101;

	public const int region_backButton = 102;

	public const int region_rewardBox = 103;

	public const int region_cancelQuestButton = 104;

	protected List<List<IQuest>> pages;

	public List<ClickableComponent> questLogButtons;

	protected int currentPage;

	protected int questPage = -1;

	public ClickableTextureComponent forwardButton;

	public ClickableTextureComponent backButton;

	public ClickableTextureComponent rewardBox;

	public ClickableTextureComponent cancelQuestButton;

	protected IQuest _shownQuest;

	protected List<string> _objectiveText;

	protected float _contentHeight;

	protected float _scissorRectHeight;

	public float scrollAmount;

	public ClickableTextureComponent upArrow;

	public ClickableTextureComponent downArrow;

	public ClickableTextureComponent scrollBar;

	protected bool scrolling;

	public Rectangle scrollBarBounds;

	private string hoverText = "";

	public QuestLog()
		: base(0, 0, 0, 0, showUpperRightCloseButton: true)
	{
		Game1.dayTimeMoneyBox.DismissQuestPing();
		Game1.playSound("bigSelect");
		paginateQuests();
		width = 832;
		height = 576;
		if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.fr)
		{
			height += 64;
		}
		Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
		xPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.X;
		yPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.Y + 32;
		questLogButtons = new List<ClickableComponent>();
		for (int i = 0; i < 6; i++)
		{
			questLogButtons.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 16 + i * ((height - 32) / 6), width - 32, (height - 32) / 6 + 4), i.ToString() ?? "")
			{
				myID = i,
				downNeighborID = -7777,
				upNeighborID = ((i > 0) ? (i - 1) : (-1)),
				rightNeighborID = -7777,
				leftNeighborID = -7777,
				fullyImmutable = true
			});
		}
		upperRightCloseButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 20, yPositionOnScreen - 8, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);
		backButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen - 64, yPositionOnScreen + 8, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
		{
			myID = 102,
			rightNeighborID = -7777
		};
		forwardButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 64 - 48, yPositionOnScreen + height - 48, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
		{
			myID = 101
		};
		rewardBox = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width / 2 - 80, yPositionOnScreen + height - 32 - 96, 96, 96), Game1.mouseCursors, new Rectangle(293, 360, 24, 24), 4f, drawShadow: true)
		{
			myID = 103
		};
		cancelQuestButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 4, yPositionOnScreen + height + 4, 48, 48), Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 4f, drawShadow: true)
		{
			myID = 104
		};
		int x = xPositionOnScreen + width + 16;
		upArrow = new ClickableTextureComponent(new Rectangle(x, yPositionOnScreen + 96, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
		downArrow = new ClickableTextureComponent(new Rectangle(x, yPositionOnScreen + height - 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
		scrollBarBounds = default(Rectangle);
		scrollBarBounds.X = upArrow.bounds.X + 12;
		scrollBarBounds.Width = 24;
		scrollBarBounds.Y = upArrow.bounds.Y + upArrow.bounds.Height + 4;
		scrollBarBounds.Height = downArrow.bounds.Y - 4 - scrollBarBounds.Y;
		scrollBar = new ClickableTextureComponent(new Rectangle(scrollBarBounds.X, scrollBarBounds.Y, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
	{
		if (oldID >= 0 && oldID < 6 && questPage == -1)
		{
			switch (direction)
			{
			case 2:
				if (oldID < 5 && pages[currentPage].Count - 1 > oldID)
				{
					currentlySnappedComponent = getComponentWithID(oldID + 1);
				}
				break;
			case 1:
				if (currentPage < pages.Count - 1)
				{
					currentlySnappedComponent = getComponentWithID(101);
					currentlySnappedComponent.leftNeighborID = oldID;
				}
				break;
			case 3:
				if (currentPage > 0)
				{
					currentlySnappedComponent = getComponentWithID(102);
					currentlySnappedComponent.rightNeighborID = oldID;
				}
				break;
			}
		}
		else if (oldID == 102)
		{
			if (questPage != -1)
			{
				return;
			}
			currentlySnappedComponent = getComponentWithID(0);
		}
		snapCursorToCurrentSnappedComponent();
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(0);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		switch (button)
		{
		case Buttons.RightTrigger:
			if (questPage == -1 && currentPage < pages.Count - 1)
			{
				nonQuestPageForwardButton();
			}
			break;
		case Buttons.LeftTrigger:
			if (questPage == -1 && currentPage > 0)
			{
				nonQuestPageBackButton();
			}
			break;
		}
	}

	/// <summary>Get the paginated list of quests which should be shown in the quest log.</summary>
	protected virtual void paginateQuests()
	{
		pages = new List<List<IQuest>>();
		IList<IQuest> allQuests = GetAllQuests();
		int num = 0;
		while (num < allQuests.Count)
		{
			List<IQuest> list = new List<IQuest>();
			for (int i = 0; i < 6; i++)
			{
				if (num >= allQuests.Count)
				{
					break;
				}
				list.Add(allQuests[num]);
				num++;
			}
			pages.Add(list);
		}
		if (pages.Count == 0)
		{
			pages.Add(new List<IQuest>());
		}
		currentPage = Utility.Clamp(currentPage, 0, pages.Count - 1);
		questPage = -1;
	}

	/// <summary>Get the quests which should be shown in the quest log.</summary>
	protected virtual IList<IQuest> GetAllQuests()
	{
		List<IQuest> list = new List<IQuest>();
		for (int num = Game1.player.team.specialOrders.Count - 1; num >= 0; num--)
		{
			SpecialOrder specialOrder = Game1.player.team.specialOrders[num];
			if (!specialOrder.IsHidden())
			{
				list.Add(specialOrder);
			}
		}
		for (int num2 = Game1.player.questLog.Count - 1; num2 >= 0; num2--)
		{
			Quest quest = Game1.player.questLog[num2];
			if (quest == null || quest.destroy.Value)
			{
				Game1.player.questLog.RemoveAt(num2);
			}
			else if (!quest.IsHidden())
			{
				list.Add(quest);
			}
		}
		return list;
	}

	public bool NeedsScroll()
	{
		if (_shownQuest != null && _shownQuest.ShouldDisplayAsComplete())
		{
			return false;
		}
		if (questPage != -1)
		{
			return _contentHeight > _scissorRectHeight;
		}
		return false;
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		if (NeedsScroll())
		{
			float num = scrollAmount - (float)(Math.Sign(direction) * 64 / 2);
			if (num < 0f)
			{
				num = 0f;
			}
			if (num > _contentHeight - _scissorRectHeight)
			{
				num = _contentHeight - _scissorRectHeight;
			}
			if (scrollAmount != num)
			{
				scrollAmount = num;
				Game1.playSound("shiny4");
				SetScrollBarFromAmount();
			}
		}
		base.receiveScrollWheelAction(direction);
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoverText = "";
		base.performHoverAction(x, y);
		if (questPage == -1)
		{
			for (int i = 0; i < questLogButtons.Count; i++)
			{
				if (pages.Count > 0 && pages[0].Count > i && questLogButtons[i].containsPoint(x, y) && !questLogButtons[i].containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()))
				{
					Game1.playSound("Cowboy_gunshot");
				}
			}
		}
		else if (_shownQuest.CanBeCancelled() && cancelQuestButton.containsPoint(x, y))
		{
			hoverText = Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestLog.cs.11364");
		}
		forwardButton.tryHover(x, y, 0.2f);
		backButton.tryHover(x, y, 0.2f);
		cancelQuestButton.tryHover(x, y, 0.2f);
		if (NeedsScroll())
		{
			upArrow.tryHover(x, y);
			downArrow.tryHover(x, y);
			scrollBar.tryHover(x, y);
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (Game1.isAnyGamePadButtonBeingPressed() && questPage != -1 && Game1.options.doesInputListContain(Game1.options.menuButton, key))
		{
			exitQuestPage();
		}
		else
		{
			base.receiveKeyPress(key);
		}
		if (Game1.options.doesInputListContain(Game1.options.journalButton, key) && readyToClose())
		{
			Game1.exitActiveMenu();
			Game1.playSound("bigDeSelect");
		}
	}

	private void nonQuestPageForwardButton()
	{
		currentPage++;
		Game1.playSound("shwip");
		if (Game1.options.SnappyMenus && currentPage == pages.Count - 1)
		{
			currentlySnappedComponent = getComponentWithID(0);
			snapCursorToCurrentSnappedComponent();
		}
	}

	private void nonQuestPageBackButton()
	{
		currentPage--;
		Game1.playSound("shwip");
		if (Game1.options.SnappyMenus && currentPage == 0)
		{
			currentlySnappedComponent = getComponentWithID(0);
			snapCursorToCurrentSnappedComponent();
		}
	}

	/// <inheritdoc />
	public override void leftClickHeld(int x, int y)
	{
		if (!GameMenu.forcePreventClose)
		{
			base.leftClickHeld(x, y);
			if (scrolling)
			{
				SetScrollFromY(y);
			}
		}
	}

	/// <inheritdoc />
	public override void releaseLeftClick(int x, int y)
	{
		if (!GameMenu.forcePreventClose)
		{
			base.releaseLeftClick(x, y);
			scrolling = false;
		}
	}

	public virtual void SetScrollFromY(int y)
	{
		int y2 = scrollBar.bounds.Y;
		float value = (float)(y - scrollBarBounds.Y) / (float)(scrollBarBounds.Height - scrollBar.bounds.Height);
		value = Utility.Clamp(value, 0f, 1f);
		scrollAmount = value * (_contentHeight - _scissorRectHeight);
		SetScrollBarFromAmount();
		if (y2 != scrollBar.bounds.Y)
		{
			Game1.playSound("shiny4");
		}
	}

	public void UpArrowPressed()
	{
		upArrow.scale = upArrow.baseScale;
		scrollAmount -= 64f;
		if (scrollAmount < 0f)
		{
			scrollAmount = 0f;
		}
		SetScrollBarFromAmount();
	}

	public void DownArrowPressed()
	{
		downArrow.scale = downArrow.baseScale;
		scrollAmount += 64f;
		if (scrollAmount > _contentHeight - _scissorRectHeight)
		{
			scrollAmount = _contentHeight - _scissorRectHeight;
		}
		SetScrollBarFromAmount();
	}

	private void SetScrollBarFromAmount()
	{
		if (!NeedsScroll())
		{
			scrollAmount = 0f;
			return;
		}
		if (scrollAmount < 8f)
		{
			scrollAmount = 0f;
		}
		if (scrollAmount > _contentHeight - _scissorRectHeight - 8f)
		{
			scrollAmount = _contentHeight - _scissorRectHeight;
		}
		scrollBar.bounds.Y = (int)((float)scrollBarBounds.Y + (float)(scrollBarBounds.Height - scrollBar.bounds.Height) / Math.Max(1f, _contentHeight - _scissorRectHeight) * scrollAmount);
	}

	public override void applyMovementKey(int direction)
	{
		base.applyMovementKey(direction);
		if (NeedsScroll())
		{
			switch (direction)
			{
			case 0:
				UpArrowPressed();
				break;
			case 2:
				DownArrowPressed();
				break;
			}
		}
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);
		if (Game1.activeClickableMenu == null)
		{
			return;
		}
		if (questPage == -1)
		{
			for (int i = 0; i < questLogButtons.Count; i++)
			{
				if (pages.Count > 0 && pages[currentPage].Count > i && questLogButtons[i].containsPoint(x, y))
				{
					Game1.playSound("smallSelect");
					questPage = i;
					_shownQuest = pages[currentPage][i];
					_objectiveText = _shownQuest.GetObjectiveDescriptions();
					_shownQuest.MarkAsViewed();
					scrollAmount = 0f;
					SetScrollBarFromAmount();
					if (Game1.options.SnappyMenus)
					{
						currentlySnappedComponent = getComponentWithID(102);
						currentlySnappedComponent.rightNeighborID = -7777;
						currentlySnappedComponent.downNeighborID = (HasMoneyReward() ? 103 : (_shownQuest.CanBeCancelled() ? 104 : (-1)));
						snapCursorToCurrentSnappedComponent();
					}
					return;
				}
			}
			if (currentPage < pages.Count - 1 && forwardButton.containsPoint(x, y))
			{
				nonQuestPageForwardButton();
				return;
			}
			if (currentPage > 0 && backButton.containsPoint(x, y))
			{
				nonQuestPageBackButton();
				return;
			}
			Game1.playSound("bigDeSelect");
			exitThisMenu();
			return;
		}
		Quest quest = _shownQuest as Quest;
		int num = ((_shownQuest.IsTimedQuest() && _shownQuest.GetDaysLeft() > 0 && SpriteText.getWidthOfString(_shownQuest.GetName()) > width / 2) ? (-48) : 0);
		if (questPage != -1 && _shownQuest.ShouldDisplayAsComplete() && _shownQuest.HasMoneyReward() && rewardBox.containsPoint(x, y + num))
		{
			Game1.player.Money += _shownQuest.GetMoneyReward();
			Game1.playSound("purchaseRepeat");
			_shownQuest.OnMoneyRewardClaimed();
		}
		else if (questPage != -1 && quest != null && !quest.completed.Value && quest.canBeCancelled.Value && cancelQuestButton.containsPoint(x, y))
		{
			quest.accepted.Value = false;
			if (quest.dailyQuest.Value && quest.dayQuestAccepted.Value == Game1.Date.TotalDays)
			{
				Game1.player.acceptedDailyQuest.Set(newValue: false);
			}
			Game1.player.questLog.Remove(quest);
			pages[currentPage].RemoveAt(questPage);
			questPage = -1;
			Game1.playSound("trashcan");
			if (Game1.options.SnappyMenus && currentPage == 0)
			{
				currentlySnappedComponent = getComponentWithID(0);
				snapCursorToCurrentSnappedComponent();
			}
		}
		else if (!NeedsScroll() || backButton.containsPoint(x, y))
		{
			exitQuestPage();
		}
		if (NeedsScroll())
		{
			if (downArrow.containsPoint(x, y) && scrollAmount < _contentHeight - _scissorRectHeight)
			{
				DownArrowPressed();
				Game1.playSound("shwip");
			}
			else if (upArrow.containsPoint(x, y) && scrollAmount > 0f)
			{
				UpArrowPressed();
				Game1.playSound("shwip");
			}
			else if (scrollBar.containsPoint(x, y))
			{
				scrolling = true;
			}
			else if (scrollBarBounds.Contains(x, y))
			{
				scrolling = true;
			}
			else if (!downArrow.containsPoint(x, y) && x > xPositionOnScreen + width && x < xPositionOnScreen + width + 128 && y > yPositionOnScreen && y < yPositionOnScreen + height)
			{
				scrolling = true;
				leftClickHeld(x, y);
				releaseLeftClick(x, y);
			}
		}
	}

	public bool HasReward()
	{
		return _shownQuest.HasReward();
	}

	public bool HasMoneyReward()
	{
		return _shownQuest.HasMoneyReward();
	}

	public void exitQuestPage()
	{
		if (_shownQuest.OnLeaveQuestPage())
		{
			pages[currentPage].RemoveAt(questPage);
		}
		questPage = -1;
		paginateQuests();
		Game1.playSound("shwip");
		if (Game1.options.SnappyMenus)
		{
			snapToDefaultClickableComponent();
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		if (questPage != -1 && HasReward())
		{
			rewardBox.scale = rewardBox.baseScale + Game1.dialogueButtonScale / 20f;
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (!Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
		}
		SpriteText.drawStringWithScrollCenteredAt(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestLog.cs.11373"), xPositionOnScreen + width / 2, yPositionOnScreen - 64);
		if (questPage == -1)
		{
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);
			for (int i = 0; i < questLogButtons.Count; i++)
			{
				if (pages.Count > 0 && pages[currentPage].Count > i)
				{
					IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), questLogButtons[i].bounds.X, questLogButtons[i].bounds.Y, questLogButtons[i].bounds.Width, questLogButtons[i].bounds.Height, questLogButtons[i].containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, 4f, drawShadow: false);
					if (pages[currentPage][i].ShouldDisplayAsNew() || pages[currentPage][i].ShouldDisplayAsComplete())
					{
						Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(questLogButtons[i].bounds.X + 64 + 4, questLogButtons[i].bounds.Y + 44), new Rectangle(pages[currentPage][i].ShouldDisplayAsComplete() ? 341 : 317, 410, 23, 9), Color.White, 0f, new Vector2(11f, 4f), 4f + Game1.dialogueButtonScale * 10f / 250f, flipped: false, 0.99f);
					}
					else
					{
						Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(questLogButtons[i].bounds.X + 32, questLogButtons[i].bounds.Y + 28), pages[currentPage][i].IsTimedQuest() ? new Rectangle(410, 501, 9, 9) : new Rectangle(395 + (pages[currentPage][i].IsTimedQuest() ? 3 : 0), 497, 3, 8), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 0.99f);
					}
					pages[currentPage][i].IsTimedQuest();
					SpriteText.drawString(b, pages[currentPage][i].GetName(), questLogButtons[i].bounds.X + 128 + 4, questLogButtons[i].bounds.Y + 20);
				}
			}
		}
		else
		{
			int widthOfString = SpriteText.getWidthOfString(_shownQuest.GetName());
			if (widthOfString > width / 2)
			{
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), xPositionOnScreen, yPositionOnScreen, width, height + (_shownQuest.ShouldDisplayAsComplete() ? 48 : 0), Color.White, 4f);
				SpriteText.drawStringHorizontallyCenteredAt(b, _shownQuest.GetName(), xPositionOnScreen + width / 2, yPositionOnScreen + 32);
			}
			else
			{
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);
				SpriteText.drawStringHorizontallyCenteredAt(b, _shownQuest.GetName(), xPositionOnScreen + width / 2 + ((_shownQuest.IsTimedQuest() && _shownQuest.GetDaysLeft() > 0) ? (Math.Max(32, SpriteText.getWidthOfString(_shownQuest.GetName()) / 3) - 32) : 0), yPositionOnScreen + 32);
			}
			float num = 0f;
			if (_shownQuest.IsTimedQuest() && _shownQuest.GetDaysLeft() > 0)
			{
				int num2 = 0;
				if (widthOfString > width / 2)
				{
					num2 = 28;
					num = 48f;
				}
				Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(xPositionOnScreen + num2 + 32, (float)(yPositionOnScreen + 48 - 8) + num), new Rectangle(410, 501, 9, 9), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 0.99f);
				Utility.drawTextWithShadow(b, Game1.parseText((pages[currentPage][questPage].GetDaysLeft() > 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestLog.cs.11374", pages[currentPage][questPage].GetDaysLeft()) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Quest_FinalDay"), Game1.dialogueFont, width - 128), Game1.dialogueFont, new Vector2(xPositionOnScreen + num2 + 80, (float)(yPositionOnScreen + 48 - 8) + num), Game1.textColor);
			}
			string text = Game1.parseText(_shownQuest.GetDescription(), Game1.dialogueFont, width - 128);
			Rectangle scissorRectangle = b.GraphicsDevice.ScissorRectangle;
			Vector2 vector = Game1.dialogueFont.MeasureString(text);
			Rectangle scissor_rect = default(Rectangle);
			scissor_rect.X = xPositionOnScreen + 32;
			scissor_rect.Y = yPositionOnScreen + 96 + (int)num;
			scissor_rect.Height = yPositionOnScreen + height - 32 - scissor_rect.Y;
			scissor_rect.Width = width - 64;
			_scissorRectHeight = scissor_rect.Height;
			scissor_rect = Utility.ConstrainScissorRectToScreen(scissor_rect);
			b.End();
			b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState
			{
				ScissorTestEnable = true
			});
			Game1.graphics.GraphicsDevice.ScissorRectangle = scissor_rect;
			Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2(xPositionOnScreen + 64, (float)yPositionOnScreen - scrollAmount + 96f + num), Game1.textColor);
			float num3 = (float)(yPositionOnScreen + 96) + vector.Y + 32f - scrollAmount + num;
			if (_shownQuest.ShouldDisplayAsComplete())
			{
				b.End();
				b.GraphicsDevice.ScissorRectangle = scissorRectangle;
				b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				SpriteText.drawString(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestLog.cs.11376"), xPositionOnScreen + 32 + 4, rewardBox.bounds.Y + 21 + 4 + (int)num);
				rewardBox.draw(b, Color.White, 0.9f, 0, 0, (int)num);
				if (HasMoneyReward())
				{
					b.Draw(Game1.mouseCursors, new Vector2(rewardBox.bounds.X + 16, (float)(rewardBox.bounds.Y + 16) - Game1.dialogueButtonScale / 2f + num), new Rectangle(280, 410, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
					SpriteText.drawString(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", _shownQuest.GetMoneyReward()), xPositionOnScreen + 448, rewardBox.bounds.Y + 21 + 4 + (int)num);
				}
			}
			else
			{
				for (int j = 0; j < _objectiveText.Count; j++)
				{
					string text2 = Game1.parseText(_objectiveText[j], width: width - 192, whichFont: Game1.dialogueFont);
					bool num4 = _shownQuest is SpecialOrder specialOrder && specialOrder.objectives[j].IsComplete();
					Color color = Game1.unselectedOptionColor;
					if (!num4)
					{
						color = Color.DarkBlue;
						Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float)(xPositionOnScreen + 96) + 8f * Game1.dialogueButtonScale / 10f, num3), new Rectangle(412, 495, 5, 4), Color.White, (float)Math.PI / 2f, Vector2.Zero);
					}
					Utility.drawTextWithShadow(b, text2, Game1.dialogueFont, new Vector2(xPositionOnScreen + 128, num3 - 8f), color);
					num3 += Game1.dialogueFont.MeasureString(text2).Y;
					if (_shownQuest is SpecialOrder specialOrder2)
					{
						OrderObjective orderObjective = specialOrder2.objectives[j];
						if (orderObjective.GetMaxCount() > 1 && orderObjective.ShouldShowProgress())
						{
							Color color2 = Color.DarkRed;
							Color color3 = Color.Red;
							if (orderObjective.GetCount() >= orderObjective.GetMaxCount())
							{
								color3 = Color.LimeGreen;
								color2 = Color.Green;
							}
							int num5 = 64;
							int num6 = 160;
							int num7 = 4;
							Rectangle rectangle = new Rectangle(0, 224, 47, 12);
							Rectangle value = new Rectangle(47, 224, 1, 12);
							int num8 = 3;
							int num9 = 3;
							int num10 = 5;
							string text3 = orderObjective.GetCount() + "/" + orderObjective.GetMaxCount();
							int num11 = (int)Game1.dialogueFont.MeasureString(orderObjective.GetMaxCount() + "/" + orderObjective.GetMaxCount()).X;
							int num12 = (int)Game1.dialogueFont.MeasureString(text3).X;
							int num13 = xPositionOnScreen + width - num5 - num12;
							int num14 = xPositionOnScreen + width - num5 - num11;
							Utility.drawTextWithShadow(b, text3, Game1.dialogueFont, new Vector2(num13, num3), Color.DarkBlue);
							Rectangle rectangle2 = new Rectangle(xPositionOnScreen + num5, (int)num3, width - num5 * 2 - num6, rectangle.Height * 4);
							if (rectangle2.Right > num14 - 16)
							{
								int num15 = rectangle2.Right - (num14 - 16);
								rectangle2.Width -= num15;
							}
							b.Draw(Game1.mouseCursors2, new Rectangle(rectangle2.X, rectangle2.Y, num10 * 4, rectangle2.Height), new Rectangle(rectangle.X, rectangle.Y, num10, rectangle.Height), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
							b.Draw(Game1.mouseCursors2, new Rectangle(rectangle2.X + num10 * 4, rectangle2.Y, rectangle2.Width - 2 * num10 * 4, rectangle2.Height), new Rectangle(rectangle.X + num10, rectangle.Y, rectangle.Width - 2 * num10, rectangle.Height), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
							b.Draw(Game1.mouseCursors2, new Rectangle(rectangle2.Right - num10 * 4, rectangle2.Y, num10 * 4, rectangle2.Height), new Rectangle(rectangle.Right - num10, rectangle.Y, num10, rectangle.Height), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
							float num16 = (float)orderObjective.GetCount() / (float)orderObjective.GetMaxCount();
							if (orderObjective.GetMaxCount() < num7)
							{
								num7 = orderObjective.GetMaxCount();
							}
							rectangle2.X += 4 * num8;
							rectangle2.Width -= 4 * num8 * 2;
							for (int k = 1; k < num7; k++)
							{
								b.Draw(Game1.mouseCursors2, new Vector2((float)rectangle2.X + (float)rectangle2.Width * ((float)k / (float)num7), rectangle2.Y), value, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.5f);
							}
							rectangle2.Y += 4 * num9;
							rectangle2.Height -= 4 * num9 * 2;
							Rectangle destinationRectangle = new Rectangle(rectangle2.X, rectangle2.Y, (int)((float)rectangle2.Width * num16) - 4, rectangle2.Height);
							b.Draw(Game1.staminaRect, destinationRectangle, null, color3, 0f, Vector2.Zero, SpriteEffects.None, (float)destinationRectangle.Y / 10000f);
							destinationRectangle.X = destinationRectangle.Right;
							destinationRectangle.Width = 4;
							b.Draw(Game1.staminaRect, destinationRectangle, null, color2, 0f, Vector2.Zero, SpriteEffects.None, (float)destinationRectangle.Y / 10000f);
							num3 += (float)((rectangle.Height + 4) * 4);
						}
					}
					_contentHeight = num3 + scrollAmount - (float)scissor_rect.Y;
				}
				b.End();
				b.GraphicsDevice.ScissorRectangle = scissorRectangle;
				b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				if (_shownQuest.CanBeCancelled())
				{
					cancelQuestButton.draw(b);
				}
				if (NeedsScroll())
				{
					if (scrollAmount > 0f)
					{
						b.Draw(Game1.staminaRect, new Rectangle(scissor_rect.X, scissor_rect.Top, scissor_rect.Width, 4), Color.Black * 0.15f);
					}
					if (scrollAmount < _contentHeight - _scissorRectHeight)
					{
						b.Draw(Game1.staminaRect, new Rectangle(scissor_rect.X, scissor_rect.Bottom - 4, scissor_rect.Width, 4), Color.Black * 0.15f);
					}
				}
			}
		}
		if (NeedsScroll())
		{
			upArrow.draw(b);
			downArrow.draw(b);
			scrollBar.draw(b);
		}
		if (currentPage < pages.Count - 1 && questPage == -1)
		{
			forwardButton.draw(b);
		}
		if (currentPage > 0 || questPage != -1)
		{
			backButton.draw(b);
		}
		base.draw(b);
		Game1.mouseCursorTransparency = 1f;
		drawMouse(b);
		if (hoverText.Length > 0)
		{
			IClickableMenu.drawHoverText(b, hoverText, Game1.dialogueFont);
		}
	}
}
