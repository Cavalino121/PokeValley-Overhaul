using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;

namespace StardewValley.Menus;

public class PondQueryMenu : IClickableMenu
{
	public const int region_okButton = 101;

	public const int region_emptyButton = 103;

	public const int region_noButton = 105;

	public const int region_nettingButton = 106;

	public new static int width = 384;

	public new static int height = 512;

	public const int unresolved_needs_extra_height = 116;

	protected FishPond _pond;

	protected Object _fishItem;

	protected string _statusText = "";

	public ClickableTextureComponent okButton;

	public ClickableTextureComponent emptyButton;

	public ClickableTextureComponent yesButton;

	public ClickableTextureComponent noButton;

	public ClickableTextureComponent changeNettingButton;

	private bool confirmingEmpty;

	protected Rectangle _confirmationBoxRectangle;

	protected string _confirmationText;

	protected float _age;

	private string hoverText = "";

	public PondQueryMenu(FishPond fish_pond)
		: base(Game1.uiViewport.Width / 2 - width / 2, Game1.uiViewport.Height / 2 - height / 2, width, height)
	{
		Game1.player.Halt();
		width = 384;
		height = 512;
		_pond = fish_pond;
		_fishItem = new Object(_pond.fishType.Value, 1);
		okButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 4, yPositionOnScreen + height - 64 - IClickableMenu.borderWidth, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
		{
			myID = 101,
			upNeighborID = -99998
		};
		emptyButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 4, yPositionOnScreen + height - 256 - IClickableMenu.borderWidth, 64, 64), Game1.mouseCursors, new Rectangle(32, 384, 16, 16), 4f)
		{
			myID = 103,
			downNeighborID = -99998
		};
		changeNettingButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 4, yPositionOnScreen + height - 192 - IClickableMenu.borderWidth, 64, 64), Game1.mouseCursors, new Rectangle(48, 384, 16, 16), 4f)
		{
			myID = 106,
			downNeighborID = -99998,
			upNeighborID = -99998
		};
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
		UpdateState();
		yPositionOnScreen = Game1.uiViewport.Height / 2 - measureTotalHeight() / 2;
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(101);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (Game1.globalFade)
		{
			return;
		}
		if (Game1.options.menuButton.Contains(new InputButton(key)))
		{
			Game1.playSound("smallSelect");
			if (readyToClose())
			{
				Game1.exitActiveMenu();
			}
		}
		else if (Game1.options.SnappyMenus && !Game1.options.menuButton.Contains(new InputButton(key)))
		{
			base.receiveKeyPress(key);
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		_age += (float)time.ElapsedGameTime.TotalSeconds;
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (Game1.globalFade)
		{
			return;
		}
		if (confirmingEmpty)
		{
			if (yesButton.containsPoint(x, y))
			{
				Game1.playSound("fishSlap");
				_pond.ClearPond();
				exitThisMenu();
			}
			else if (noButton.containsPoint(x, y))
			{
				confirmingEmpty = false;
				Game1.playSound("smallSelect");
				if (Game1.options.SnappyMenus)
				{
					currentlySnappedComponent = getComponentWithID(103);
					snapCursorToCurrentSnappedComponent();
				}
			}
			return;
		}
		if (okButton != null && okButton.containsPoint(x, y) && readyToClose())
		{
			Game1.exitActiveMenu();
			Game1.playSound("smallSelect");
		}
		if (changeNettingButton.containsPoint(x, y))
		{
			Game1.playSound("drumkit6");
			_pond.nettingStyle.Value++;
			_pond.nettingStyle.Value %= 4;
		}
		else if (emptyButton.containsPoint(x, y))
		{
			_confirmationBoxRectangle = new Rectangle(0, 0, 400, 100);
			_confirmationBoxRectangle.X = Game1.uiViewport.Width / 2 - _confirmationBoxRectangle.Width / 2;
			_confirmationText = Game1.content.LoadString("Strings\\UI:PondQuery_ConfirmEmpty");
			_confirmationText = Game1.parseText(_confirmationText, Game1.smallFont, _confirmationBoxRectangle.Width);
			Vector2 vector = Game1.smallFont.MeasureString(_confirmationText);
			_confirmationBoxRectangle.Height = (int)vector.Y;
			_confirmationBoxRectangle.Y = Game1.uiViewport.Height / 2 - _confirmationBoxRectangle.Height / 2;
			confirmingEmpty = true;
			yesButton = new ClickableTextureComponent(new Rectangle(Game1.uiViewport.Width / 2 - 64 - 4, _confirmationBoxRectangle.Bottom + 32, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
			{
				myID = 111,
				rightNeighborID = 105
			};
			noButton = new ClickableTextureComponent(new Rectangle(Game1.uiViewport.Width / 2 + 4, _confirmationBoxRectangle.Bottom + 32, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47), 1f)
			{
				myID = 105,
				leftNeighborID = 111
			};
			Game1.playSound("smallSelect");
			if (Game1.options.SnappyMenus)
			{
				populateClickableComponentList();
				currentlySnappedComponent = noButton;
				snapCursorToCurrentSnappedComponent();
			}
		}
	}

	public override bool readyToClose()
	{
		if (base.readyToClose())
		{
			return !Game1.globalFade;
		}
		return false;
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (!Game1.globalFade && readyToClose())
		{
			Game1.exitActiveMenu();
			Game1.playSound("smallSelect");
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoverText = "";
		if (okButton != null)
		{
			if (okButton.containsPoint(x, y))
			{
				okButton.scale = Math.Min(1.1f, okButton.scale + 0.05f);
			}
			else
			{
				okButton.scale = Math.Max(1f, okButton.scale - 0.05f);
			}
		}
		if (emptyButton != null)
		{
			if (emptyButton.containsPoint(x, y))
			{
				emptyButton.scale = Math.Min(4.1f, emptyButton.scale + 0.05f);
				hoverText = Game1.content.LoadString("Strings\\UI:PondQuery_EmptyPond", 10);
			}
			else
			{
				emptyButton.scale = Math.Max(4f, emptyButton.scale - 0.05f);
			}
		}
		if (changeNettingButton != null)
		{
			if (changeNettingButton.containsPoint(x, y))
			{
				changeNettingButton.scale = Math.Min(4.1f, changeNettingButton.scale + 0.05f);
				hoverText = Game1.content.LoadString("Strings\\UI:PondQuery_ChangeNetting", 10);
			}
			else
			{
				changeNettingButton.scale = Math.Max(4f, emptyButton.scale - 0.05f);
			}
		}
		if (yesButton != null)
		{
			if (yesButton.containsPoint(x, y))
			{
				yesButton.scale = Math.Min(1.1f, yesButton.scale + 0.05f);
			}
			else
			{
				yesButton.scale = Math.Max(1f, yesButton.scale - 0.05f);
			}
		}
		if (noButton != null)
		{
			if (noButton.containsPoint(x, y))
			{
				noButton.scale = Math.Min(1.1f, noButton.scale + 0.05f);
			}
			else
			{
				noButton.scale = Math.Max(1f, noButton.scale - 0.05f);
			}
		}
	}

	public static string GetFishTalkSuffix(Object fishItem)
	{
		HashSet<string> contextTags = fishItem.GetContextTags();
		if (contextTags.Contains("fish_talk_rude"))
		{
			return "_Rude";
		}
		if (contextTags.Contains("fish_talk_stiff"))
		{
			return "_Stiff";
		}
		if (contextTags.Contains("fish_talk_demanding"))
		{
			return "_Demanding";
		}
		foreach (string item in contextTags)
		{
			if (!item.StartsWithIgnoreCase("fish_talk_"))
			{
				continue;
			}
			char[] array = item.Substring("fish_talk".Length).ToCharArray();
			bool flag = false;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == '_')
				{
					flag = true;
				}
				else if (flag)
				{
					array[i] = char.ToUpper(array[i]);
					flag = false;
				}
			}
			return new string(array);
		}
		if (contextTags.Contains("fish_carnivorous"))
		{
			return "_Carnivore";
		}
		return "";
	}

	public static string getCompletedRequestString(FishPond pond, Object fishItem, Random r)
	{
		if (fishItem != null)
		{
			string fishTalkSuffix = GetFishTalkSuffix(fishItem);
			if (fishTalkSuffix != "")
			{
				return Lexicon.capitalize(Game1.content.LoadString("Strings\\UI:PondQuery_StatusRequestComplete" + fishTalkSuffix + r.Next(3), pond.neededItem.Value.DisplayName));
			}
		}
		return Game1.content.LoadString("Strings\\UI:PondQuery_StatusRequestComplete" + r.Next(7), pond.neededItem.Value.DisplayName);
	}

	public void UpdateState()
	{
		Random random = Utility.CreateDaySaveRandom(_pond.seedOffset.Value);
		if (_pond.currentOccupants.Value <= 0)
		{
			_statusText = Game1.content.LoadString("Strings\\UI:PondQuery_StatusNoFish");
			return;
		}
		if (_pond.neededItem.Value != null)
		{
			if (_pond.hasCompletedRequest.Value)
			{
				_statusText = getCompletedRequestString(_pond, _fishItem, random);
				return;
			}
			if (_pond.HasUnresolvedNeeds())
			{
				string text = _pond.neededItemCount.Value.ToString() ?? "";
				if (_pond.neededItemCount.Value <= 1)
				{
					text = Lexicon.getProperArticleForWord(_pond.neededItem.Value.DisplayName);
					if (text == "")
					{
						text = Game1.content.LoadString("Strings\\UI:PondQuery_StatusRequestOneCount");
					}
				}
				if (_fishItem != null)
				{
					if (_fishItem.HasContextTag("fish_talk_rude"))
					{
						_statusText = Lexicon.capitalize(Game1.content.LoadString("Strings\\UI:PondQuery_StatusRequestPending_Rude" + random.Next(3) + "_" + (Game1.player.IsMale ? "Male" : "Female"), Lexicon.makePlural(_pond.neededItem.Value.DisplayName, _pond.neededItemCount.Value == 1), text, _pond.neededItem.Value.DisplayName));
						return;
					}
					string fishTalkSuffix = GetFishTalkSuffix(_fishItem);
					if (fishTalkSuffix != "")
					{
						_statusText = Lexicon.capitalize(Game1.content.LoadString("Strings\\UI:PondQuery_StatusRequestPending" + fishTalkSuffix + random.Next(3), Lexicon.makePlural(_pond.neededItem.Value.DisplayName, _pond.neededItemCount.Value == 1), text, _pond.neededItem.Value.DisplayName));
						return;
					}
				}
				_statusText = Lexicon.capitalize(Game1.content.LoadString("Strings\\UI:PondQuery_StatusRequestPending" + random.Next(7), Lexicon.makePlural(_pond.neededItem.Value.DisplayName, _pond.neededItemCount.Value == 1), text, _pond.neededItem.Value.DisplayName));
				return;
			}
		}
		if (_fishItem != null && (_fishItem.QualifiedItemId == "(O)397" || _fishItem.QualifiedItemId == "(O)393"))
		{
			_statusText = Game1.content.LoadString("Strings\\UI:PondQuery_StatusOk_Coral", _fishItem.DisplayName);
		}
		else
		{
			_statusText = Game1.content.LoadString("Strings\\UI:PondQuery_StatusOk" + random.Next(7));
		}
	}

	private int measureTotalHeight()
	{
		return 644 + measureExtraTextHeight(getDisplayedText());
	}

	private int measureExtraTextHeight(string displayed_text)
	{
		return Math.Max(0, (int)Game1.smallFont.MeasureString(displayed_text).Y - 90) + 4;
	}

	private string getDisplayedText()
	{
		return Game1.parseText(_statusText, Game1.smallFont, width - IClickableMenu.spaceToClearSideBorder * 2 - 64);
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (!Game1.globalFade)
		{
			if (!Game1.options.showClearBackgrounds)
			{
				b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
			}
			bool flag = _pond.neededItem.Value != null && _pond.HasUnresolvedNeeds() && !_pond.hasCompletedRequest.Value;
			string text = Game1.content.LoadString("Strings\\UI:PondQuery_Name", _fishItem.DisplayName);
			Vector2 vector = Game1.smallFont.MeasureString(text);
			Game1.DrawBox((int)((float)(Game1.uiViewport.Width / 2) - (vector.X + 64f) * 0.5f), yPositionOnScreen - 4 + 128, (int)(vector.X + 64f), 64);
			Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2((float)(Game1.uiViewport.Width / 2) - vector.X * 0.5f, (float)(yPositionOnScreen - 4) + 160f - vector.Y * 0.5f), Color.Black);
			string displayedText = getDisplayedText();
			int num = 0;
			if (flag)
			{
				num += 116;
			}
			int num2 = measureExtraTextHeight(displayedText);
			Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen + 128, width, height - 128 + num + num2, speaker: false, drawOnlyBox: true);
			string text2 = Game1.content.LoadString("Strings\\UI:PondQuery_Population", _pond.FishCount.ToString() ?? "", _pond.maxOccupants);
			vector = Game1.smallFont.MeasureString(text2);
			Utility.drawTextWithShadow(b, text2, Game1.smallFont, new Vector2(_pond.goldenAnimalCracker.Value ? ((float)(xPositionOnScreen + IClickableMenu.borderWidth + 4)) : ((float)(xPositionOnScreen + width / 2) - vector.X * 0.5f), yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 16 + 128), Game1.textColor);
			int value = _pond.maxOccupants.Value;
			float num3 = 13f;
			int num4 = 0;
			int num5 = 0;
			for (int i = 0; i < value; i++)
			{
				float num6 = (float)Math.Sin(_age * 1f + (float)num4 * 0.75f + (float)num5 * 0.25f) * 2f;
				if (i < _pond.FishCount)
				{
					_fishItem.drawInMenu(b, new Vector2((float)(xPositionOnScreen + width / 2) - num3 * (float)Math.Min(value, 5) * 4f * 0.5f + num3 * 4f * (float)num4 - 12f, (float)(yPositionOnScreen + (int)(num6 * 4f)) + (float)(num5 * 4) * num3 + 275.2f), 0.75f, 1f, 0f, StackDrawType.Hide, Color.White, drawShadow: false);
				}
				else
				{
					_fishItem.drawInMenu(b, new Vector2((float)(xPositionOnScreen + width / 2) - num3 * (float)Math.Min(value, 5) * 4f * 0.5f + num3 * 4f * (float)num4 - 12f, (float)(yPositionOnScreen + (int)(num6 * 4f)) + (float)(num5 * 4) * num3 + 275.2f), 0.75f, 0.35f, 0f, StackDrawType.Hide, Color.Black, drawShadow: false);
				}
				num4++;
				if (num4 == 5)
				{
					num4 = 0;
					num5++;
				}
			}
			vector = Game1.smallFont.MeasureString(displayedText);
			Utility.drawTextWithShadow(b, displayedText, Game1.smallFont, new Vector2((float)(xPositionOnScreen + width / 2) - vector.X * 0.5f, (float)(yPositionOnScreen + height + num2 - (flag ? 32 : 48)) - vector.Y), Game1.textColor);
			if (flag)
			{
				drawHorizontalPartition(b, (int)((float)(yPositionOnScreen + height + num2) - 48f));
				Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float)(xPositionOnScreen + 60) + 8f * Game1.dialogueButtonScale / 10f, yPositionOnScreen + height + num2 + 28), new Rectangle(412, 495, 5, 4), Color.White, (float)Math.PI / 2f, Vector2.Zero);
				string text3 = Game1.content.LoadString("Strings\\UI:PondQuery_StatusRequest_Bring");
				vector = Game1.smallFont.MeasureString(text3);
				int num7 = xPositionOnScreen + 88;
				float num8 = num7;
				float num9 = num8 + vector.X + 4f;
				if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ja || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.tr)
				{
					num9 = num7 - 8;
					num8 = num7 + 76;
				}
				Utility.drawTextWithShadow(b, text3, Game1.smallFont, new Vector2(num8, yPositionOnScreen + height + num2 + 24), Game1.textColor);
				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(_pond.neededItem.Value.QualifiedItemId);
				Texture2D texture = dataOrErrorItem.GetTexture();
				Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
				b.Draw(texture, new Vector2(num9, yPositionOnScreen + height + num2 + 4), sourceRect, Color.Black * 0.4f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				b.Draw(texture, new Vector2(num9 + 4f, yPositionOnScreen + height + num2), sourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				if (_pond.neededItemCount.Value > 1)
				{
					Utility.drawTinyDigits(_pond.neededItemCount.Value, b, new Vector2(num9 + 48f, yPositionOnScreen + height + num2 + 48), 3f, 1f, Color.White);
				}
			}
			if (_pond.goldenAnimalCracker.Value && Game1.objectSpriteSheet_2 != null)
			{
				Utility.drawWithShadow(b, Game1.objectSpriteSheet_2, new Vector2((float)(xPositionOnScreen + width) - 105.6f, (float)yPositionOnScreen + 224f), new Rectangle(16, 240, 16, 16), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 0.89f);
			}
			okButton.draw(b);
			emptyButton.draw(b);
			changeNettingButton.draw(b);
			if (confirmingEmpty)
			{
				if (!Game1.options.showClearBackgrounds)
				{
					b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
				}
				int num10 = 16;
				_confirmationBoxRectangle.Width += num10;
				_confirmationBoxRectangle.Height += num10;
				_confirmationBoxRectangle.X -= num10 / 2;
				_confirmationBoxRectangle.Y -= num10 / 2;
				Game1.DrawBox(_confirmationBoxRectangle.X, _confirmationBoxRectangle.Y, _confirmationBoxRectangle.Width, _confirmationBoxRectangle.Height);
				_confirmationBoxRectangle.Width -= num10;
				_confirmationBoxRectangle.Height -= num10;
				_confirmationBoxRectangle.X += num10 / 2;
				_confirmationBoxRectangle.Y += num10 / 2;
				b.DrawString(Game1.smallFont, _confirmationText, new Vector2(_confirmationBoxRectangle.X, _confirmationBoxRectangle.Y), Game1.textColor);
				yesButton.draw(b);
				noButton.draw(b);
			}
			else
			{
				string text4 = hoverText;
				if (text4 != null && text4.Length > 0)
				{
					IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);
				}
			}
		}
		drawMouse(b);
	}
}
