using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Triggers;

namespace StardewValley.Menus;

public class LetterViewerMenu : IClickableMenu
{
	public const int region_backButton = 101;

	public const int region_forwardButton = 102;

	public const int region_acceptQuestButton = 103;

	public const int region_itemGrabButton = 104;

	public const int letterWidth = 320;

	public const int letterHeight = 180;

	public Texture2D letterTexture;

	public Texture2D secretNoteImageTexture;

	public int moneyIncluded;

	public int secretNoteImage = -1;

	public int whichBG;

	/// <summary>The ID of the quest attached to the letter being viewed, if any.</summary>
	public string questID;

	/// <summary>The ID of the special order attached to the letter being viewed, if any.</summary>
	public string specialOrderId;

	/// <summary>The translated name of the recipe learned from this letter, if any.</summary>
	public string learnedRecipe = "";

	public string cookingOrCrafting = "";

	public string mailTitle;

	public List<string> mailMessage = new List<string>();

	public int page;

	public readonly List<ClickableComponent> itemsToGrab = new List<ClickableComponent>();

	public float scale;

	public bool isMail;

	public bool isFromCollection;

	public new bool destroy;

	public Color? customTextColor;

	public bool usingCustomBackground;

	public ClickableTextureComponent backButton;

	public ClickableTextureComponent forwardButton;

	public ClickableComponent acceptQuestButton;

	public const float scaleChange = 0.003f;

	/// <summary>Whether the letter has an attached quest or special order which the player can accept.</summary>
	public bool HasQuestOrSpecialOrder
	{
		get
		{
			if (questID == null)
			{
				return specialOrderId != null;
			}
			return true;
		}
	}

	public LetterViewerMenu(string text)
		: base((int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).Y, 1280, 720, showUpperRightCloseButton: true)
	{
		Game1.playSound("shwip");
		backButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
		{
			myID = 101,
			rightNeighborID = 102
		};
		forwardButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 32 - 48, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
		{
			myID = 102,
			leftNeighborID = 101
		};
		letterTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\letterBG");
		text = ApplyCustomFormatting(text);
		mailMessage = SpriteText.getStringBrokenIntoSectionsOfHeight(text, width - 64, height - 128);
		forwardButton.visible = page < mailMessage.Count - 1;
		backButton.visible = page > 0;
		OnPageChange();
		populateClickableComponentList();
		if (Game1.options.SnappyMenus)
		{
			snapToDefaultClickableComponent();
		}
	}

	public LetterViewerMenu(int secretNoteIndex)
		: base((int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).Y, 1280, 720, showUpperRightCloseButton: true)
	{
		Game1.playSound("shwip");
		backButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
		{
			myID = 101,
			rightNeighborID = 102
		};
		forwardButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 32 - 48, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
		{
			myID = 102,
			leftNeighborID = 101
		};
		letterTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\letterBG");
		string text = DataLoader.SecretNotes(Game1.content)[secretNoteIndex];
		if (text[0] == '!')
		{
			secretNoteImageTexture = Game1.temporaryContent.Load<Texture2D>("TileSheets\\SecretNotesImages");
			secretNoteImage = Convert.ToInt32(ArgUtility.SplitBySpaceAndGet(text, 1));
		}
		else
		{
			whichBG = ((secretNoteIndex <= 1000) ? 1 : 0);
			string s = ApplyCustomFormatting(Utility.ParseGiftReveals(text.Replace("@", Game1.player.name.Value)));
			mailMessage = SpriteText.getStringBrokenIntoSectionsOfHeight(s, width - 64, height - 128);
		}
		OnPageChange();
		forwardButton.visible = page < mailMessage.Count - 1;
		backButton.visible = page > 0;
		populateClickableComponentList();
		if (Game1.options.SnappyMenus)
		{
			snapToDefaultClickableComponent();
		}
	}

	public virtual void OnPageChange()
	{
		forwardButton.visible = page < mailMessage.Count - 1;
		backButton.visible = page > 0;
		foreach (ClickableComponent item in itemsToGrab)
		{
			item.visible = ShouldShowInteractable();
		}
		if (acceptQuestButton != null)
		{
			acceptQuestButton.visible = ShouldShowInteractable();
		}
		if (Game1.options.SnappyMenus && (currentlySnappedComponent == null || !currentlySnappedComponent.visible))
		{
			snapToDefaultClickableComponent();
		}
	}

	public LetterViewerMenu(string mail, string mailTitle, bool fromCollection = false)
		: base((int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).Y, 1280, 720, showUpperRightCloseButton: true)
	{
		isFromCollection = fromCollection;
		this.mailTitle = mailTitle;
		isMail = true;
		Game1.playSound("shwip");
		backButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
		{
			myID = 101,
			rightNeighborID = 102
		};
		forwardButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 32 - 48, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
		{
			myID = 102,
			leftNeighborID = 101
		};
		acceptQuestButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 128, yPositionOnScreen + height - 128, (int)Game1.dialogueFont.MeasureString(Game1.content.LoadString("Strings\\UI:AcceptQuest")).X + 24, (int)Game1.dialogueFont.MeasureString(Game1.content.LoadString("Strings\\UI:AcceptQuest")).Y + 24), "")
		{
			myID = 103,
			rightNeighborID = 102,
			leftNeighborID = 101
		};
		letterTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\letterBG");
		if (mailTitle.Equals("winter_5_2") || mailTitle.Equals("winter_12_1") || mailTitle.ContainsIgnoreCase("wizard"))
		{
			whichBG = 2;
		}
		else if (mailTitle.Equals("Sandy"))
		{
			whichBG = 1;
		}
		else if (mailTitle.Contains("Krobus"))
		{
			whichBG = 3;
		}
		else if (mailTitle.Contains("passedOut1") || mailTitle.Equals("landslideDone") || mailTitle.Equals("FizzIntro"))
		{
			whichBG = 4;
		}
		try
		{
			mail = mail.Split("[#]")[0];
			mail = mail.Replace("@", Game1.player.Name);
			mail = Dialogue.applyGenderSwitch(Game1.player.Gender, mail, altTokenOnly: true);
			mail = ApplyCustomFormatting(mail);
			mail = HandleActionCommand(mail);
			mail = HandleItemCommand(mail);
			bool flag = fromCollection && (Game1.season != Season.Winter || Game1.dayOfMonth < 18 || Game1.dayOfMonth > 25);
			mail = mail.Replace("%secretsanta", flag ? "???" : Utility.GetRandomWinterStarParticipant().displayName);
			if (mailTitle.Equals("winter_18") && !fromCollection)
			{
				Game1.player.mailReceived.Add("sawSecretSanta" + Game1.year);
			}
		}
		catch (Exception exception)
		{
			Game1.log.Error("Letter '" + this.mailTitle + "' couldn't be parsed.", exception);
			mail = "...";
		}
		if (mailTitle == "ccBulletinThankYou" && !Game1.player.hasOrWillReceiveMail("ccBulletinThankYouReceived"))
		{
			Utility.ForEachVillager(delegate(NPC n)
			{
				if (!n.datable.Value)
				{
					Game1.player.changeFriendship(500, n);
				}
				return true;
			});
			Game1.addMailForTomorrow("ccBulletinThankYouReceived", noLetter: true);
		}
		int num = height - 128;
		if (HasInteractable())
		{
			num = height - 128 - 32;
		}
		mailMessage = SpriteText.getStringBrokenIntoSectionsOfHeight(mail, width - 64, num);
		if (mailMessage.Count == 0)
		{
			mailMessage.Add("[" + mailTitle + "]");
		}
		forwardButton.visible = page < mailMessage.Count - 1;
		backButton.visible = page > 0;
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
			if (mailMessage.Count <= 1)
			{
				backButton.myID = -100;
				forwardButton.myID = -100;
			}
		}
	}

	/// <summary>Handle the <c>%action</c> command in the mail text, if present. This runs the action(s) and return the mail text with the commands stripped.</summary>
	/// <param name="mail">The mail text to parse.</param>
	public string HandleActionCommand(string mail)
	{
		int startIndex = 0;
		while (true)
		{
			int num = mail.IndexOf("%action", startIndex, StringComparison.InvariantCulture);
			if (num < 0)
			{
				break;
			}
			int num2 = mail.IndexOf("%%", num, StringComparison.InvariantCulture);
			if (num2 < 0)
			{
				break;
			}
			string text = mail.Substring(num, num2 + 2 - num);
			mail = mail.Substring(0, num) + mail.Substring(num + text.Length);
			string text2 = text.Substring("%action".Length, text.Length - "%action".Length - "%%".Length);
			startIndex = num;
			if (!isFromCollection && !TriggerActionManager.TryRunAction(text2, out var error, out var exception))
			{
				Game1.log.Error($"Letter '{mailTitle}' has invalid action command '{text2}': {error}", exception);
			}
		}
		return mail;
	}

	/// <summary>Handle the <c>%item</c> command in the mail text, if present. This adds the matching item to the letter and return the mail text with the command stripped.</summary>
	/// <param name="mail">The mail text to parse.</param>
	public string HandleItemCommand(string mail)
	{
		int startIndex = 0;
		while (true)
		{
			int num = mail.IndexOf("%item", startIndex, StringComparison.InvariantCulture);
			if (num < 0)
			{
				break;
			}
			int num2 = mail.IndexOf("%%", num, StringComparison.InvariantCulture);
			if (num2 < 0)
			{
				break;
			}
			string text = mail.Substring(num, num2 + 2 - num);
			mail = mail.Substring(0, num) + mail.Substring(num + text.Length);
			string[] array = ArgUtility.SplitBySpace(text.Substring("%item".Length, text.Length - "%item".Length - "%%".Length), 2);
			string text2 = array[0];
			string[] array2 = ((array.Length > 1) ? ArgUtility.SplitBySpace(array[1]) : LegacyShims.EmptyArray<string>());
			startIndex = num;
			if (isFromCollection)
			{
				continue;
			}
			switch (text2.ToLower())
			{
			case "id":
			{
				string itemId;
				int amount;
				if (array2.Length == 1)
				{
					itemId = array2[0];
					amount = 1;
				}
				else
				{
					int num3 = Game1.random.Next(array2.Length);
					num3 -= num3 % 2;
					itemId = array2[num3];
					amount = int.Parse(array2[num3 + 1]);
				}
				Item item = ItemRegistry.Create(itemId, amount);
				itemsToGrab.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 48, yPositionOnScreen + height - 32 - 96, 96, 96), item)
				{
					myID = 104,
					leftNeighborID = 101,
					rightNeighborID = 102
				});
				backButton.rightNeighborID = 104;
				forwardButton.leftNeighborID = 104;
				break;
			}
			case "object":
			{
				int num4 = Game1.random.Next(array2.Length);
				num4 -= num4 % 2;
				Item item2 = ItemRegistry.Create(array2[num4], int.Parse(array2[num4 + 1]));
				itemsToGrab.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 48, yPositionOnScreen + height - 32 - 96, 96, 96), item2)
				{
					myID = 104,
					leftNeighborID = 101,
					rightNeighborID = 102
				});
				backButton.rightNeighborID = 104;
				forwardButton.leftNeighborID = 104;
				break;
			}
			case "tools":
			{
				string[] array3 = array2;
				foreach (string text4 in array3)
				{
					Item item3 = null;
					switch (text4)
					{
					case "Axe":
					case "Hoe":
					case "Pickaxe":
						item3 = ItemRegistry.Create("(T)" + text4);
						break;
					case "Can":
						item3 = ItemRegistry.Create("(T)WateringCan");
						break;
					case "Scythe":
						item3 = ItemRegistry.Create("(W)47");
						break;
					}
					if (item3 != null)
					{
						itemsToGrab.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 48, yPositionOnScreen + height - 32 - 96, 96, 96), item3));
					}
				}
				break;
			}
			case "bigobject":
			{
				string text5 = Game1.random.ChooseFrom(array2);
				Item item4 = ItemRegistry.Create("(BC)" + text5);
				itemsToGrab.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 48, yPositionOnScreen + height - 32 - 96, 96, 96), item4)
				{
					myID = 104,
					leftNeighborID = 101,
					rightNeighborID = 102
				});
				backButton.rightNeighborID = 104;
				forwardButton.leftNeighborID = 104;
				break;
			}
			case "furniture":
			{
				string text10 = Game1.random.ChooseFrom(array2);
				Item item5 = ItemRegistry.Create("(F)" + text10);
				itemsToGrab.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 48, yPositionOnScreen + height - 32 - 96, 96, 96), item5)
				{
					myID = 104,
					leftNeighborID = 101,
					rightNeighborID = 102
				});
				backButton.rightNeighborID = 104;
				forwardButton.leftNeighborID = 104;
				break;
			}
			case "money":
			{
				int num5 = ((array2.Length > 1) ? Game1.random.Next(Convert.ToInt32(array2[0]), Convert.ToInt32(array2[1])) : Convert.ToInt32(array2[0]));
				num5 -= num5 % 10;
				Game1.player.Money += num5;
				moneyIncluded = num5;
				break;
			}
			case "conversationtopic":
			{
				string text6 = array2[0];
				int value2 = Convert.ToInt32(array2[1]);
				Game1.player.activeDialogueEvents[text6] = value2;
				if (text6.Equals("ElliottGone3"))
				{
					Utility.getHomeOfFarmer(Game1.player).fridge.Value.addItem(ItemRegistry.Create("(O)732"));
				}
				break;
			}
			case "cookingrecipe":
			{
				Dictionary<string, string> cookingRecipes = CraftingRecipe.cookingRecipes;
				string text7 = string.Join(" ", array2);
				if (string.IsNullOrWhiteSpace(text7))
				{
					int num6 = 1000;
					foreach (string key in cookingRecipes.Keys)
					{
						string[] array4 = ArgUtility.SplitBySpace(ArgUtility.Get(cookingRecipes[key].Split('/'), 3));
						string text8 = ArgUtility.Get(array4, 0);
						string text9 = ArgUtility.Get(array4, 1);
						if (text8 == "f" && text9 == mailTitle.Replace("Cooking", "") && !Game1.player.cookingRecipes.ContainsKey(key))
						{
							int num7 = Convert.ToInt32(array4[2]);
							if (num7 <= num6)
							{
								num6 = num7;
								text7 = key;
							}
						}
					}
				}
				if (!string.IsNullOrWhiteSpace(text7))
				{
					if (cookingRecipes.ContainsKey(text7))
					{
						Game1.player.cookingRecipes.TryAdd(text7, 0);
						learnedRecipe = new CraftingRecipe(text7, isCookingRecipe: true).DisplayName;
						cookingOrCrafting = Game1.content.LoadString("Strings\\UI:LearnedRecipe_cooking");
						break;
					}
					Game1.log.Warn($"Letter '{mailTitle}' has unknown cooking recipe '{text7}'.");
				}
				break;
			}
			case "craftingrecipe":
			{
				Dictionary<string, string> craftingRecipes = CraftingRecipe.craftingRecipes;
				if (craftingRecipes.ContainsKey(array2[0]))
				{
					learnedRecipe = array2[0];
				}
				else
				{
					string text3 = array2[0].Replace('_', ' ');
					if (!craftingRecipes.ContainsKey(text3))
					{
						Game1.log.Warn($"Letter '{mailTitle}' has unknown crafting recipe '{array2[0]}'{((array2[0] != text3) ? (" or '" + text3 + "'") : "")}.");
						break;
					}
					learnedRecipe = text3;
				}
				Game1.player.craftingRecipes.TryAdd(learnedRecipe, 0);
				learnedRecipe = new CraftingRecipe(learnedRecipe, isCookingRecipe: false).DisplayName;
				cookingOrCrafting = Game1.content.LoadString("Strings\\UI:LearnedRecipe_crafting");
				break;
			}
			case "itemrecovery":
				if (Game1.player.recoveredItem != null)
				{
					Item recoveredItem = Game1.player.recoveredItem;
					Game1.player.recoveredItem = null;
					itemsToGrab.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 48, yPositionOnScreen + height - 32 - 96, 96, 96), recoveredItem)
					{
						myID = 104,
						leftNeighborID = 101,
						rightNeighborID = 102
					});
					backButton.rightNeighborID = 104;
					forwardButton.leftNeighborID = 104;
				}
				break;
			case "quest":
				questID = array2[0];
				if (array2.Length > 1)
				{
					if (!Game1.player.mailReceived.Contains("NOQUEST_" + questID))
					{
						Game1.player.addQuest(questID);
					}
					questID = null;
				}
				backButton.rightNeighborID = 103;
				forwardButton.leftNeighborID = 103;
				break;
			case "specialorder":
			{
				specialOrderId = array2[0];
				if (ArgUtility.TryGetBool(array2, 1, out var value, out var _, "bool addImmediately") && value)
				{
					if (!Game1.player.mailReceived.Contains("NOSPECIALORDER_" + specialOrderId))
					{
						Game1.player.team.AddSpecialOrder(specialOrderId);
					}
					specialOrderId = null;
				}
				backButton.rightNeighborID = 103;
				forwardButton.leftNeighborID = 103;
				break;
			}
			}
		}
		return mail;
	}

	public virtual string ApplyCustomFormatting(string text)
	{
		text = Dialogue.applyGenderSwitchBlocks(Game1.player.Gender, text);
		for (int num = text.IndexOf("["); num >= 0; num = text.IndexOf("[", num + 1))
		{
			int num2 = text.IndexOf("]", num);
			if (num2 >= 0)
			{
				bool flag = false;
				try
				{
					string[] array = ArgUtility.SplitBySpace(text.Substring(num + 1, num2 - num - 1));
					string text2 = array[0];
					if (!(text2 == "letterbg"))
					{
						if (text2 == "textcolor")
						{
							string text3 = array[1].ToLower();
							string[] array2 = new string[10] { "black", "blue", "red", "purple", "white", "orange", "green", "cyan", "gray", "jojablue" };
							customTextColor = null;
							for (int i = 0; i < array2.Length; i++)
							{
								if (text3 == array2[i])
								{
									customTextColor = SpriteText.getColorFromIndex(i);
									break;
								}
							}
							flag = true;
						}
					}
					else
					{
						switch (array.Length)
						{
						case 2:
							whichBG = int.Parse(array[1]);
							break;
						case 3:
							usingCustomBackground = true;
							letterTexture = Game1.temporaryContent.Load<Texture2D>(array[1]);
							whichBG = int.Parse(array[2]);
							break;
						}
						flag = true;
					}
				}
				catch (Exception)
				{
				}
				if (flag)
				{
					text = text.Remove(num, num2 - num + 1);
					num--;
				}
			}
		}
		return text;
	}

	public override void snapToDefaultClickableComponent()
	{
		if (HasQuestOrSpecialOrder && ShouldShowInteractable())
		{
			currentlySnappedComponent = getComponentWithID(103);
		}
		else if (itemsToGrab.Count > 0 && ShouldShowInteractable())
		{
			currentlySnappedComponent = getComponentWithID(104);
		}
		else if (currentlySnappedComponent == null || (currentlySnappedComponent != backButton && currentlySnappedComponent != forwardButton))
		{
			currentlySnappedComponent = forwardButton;
		}
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		xPositionOnScreen = (int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).X;
		yPositionOnScreen = (int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).Y;
		backButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
		{
			myID = 101,
			rightNeighborID = 102
		};
		forwardButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 32 - 48, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
		{
			myID = 102,
			leftNeighborID = 101
		};
		acceptQuestButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 128, yPositionOnScreen + height - 128, (int)Game1.dialogueFont.MeasureString(Game1.content.LoadString("Strings\\UI:AcceptQuest")).X + 24, (int)Game1.dialogueFont.MeasureString(Game1.content.LoadString("Strings\\UI:AcceptQuest")).Y + 24), "")
		{
			myID = 103,
			rightNeighborID = 102,
			leftNeighborID = 101
		};
		foreach (ClickableComponent item in itemsToGrab)
		{
			item.bounds = new Rectangle(xPositionOnScreen + width / 2 - 48, yPositionOnScreen + height - 32 - 96, 96, 96);
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (key != Keys.None)
		{
			if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && readyToClose())
			{
				exitThisMenu(ShouldPlayExitSound());
			}
			else
			{
				base.receiveKeyPress(key);
			}
		}
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		base.receiveGamePadButton(button);
		switch (button)
		{
		case Buttons.B:
			if (isFromCollection)
			{
				exitThisMenu(playSound: false);
			}
			break;
		case Buttons.LeftTrigger:
			if (page > 0)
			{
				page--;
				Game1.playSound("shwip");
				OnPageChange();
			}
			break;
		case Buttons.RightTrigger:
			if (page < mailMessage.Count - 1)
			{
				page++;
				Game1.playSound("shwip");
				OnPageChange();
			}
			break;
		}
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (scale < 1f)
		{
			return;
		}
		if (upperRightCloseButton != null && readyToClose() && upperRightCloseButton.containsPoint(x, y))
		{
			if (playSound)
			{
				Game1.playSound("bigDeSelect");
			}
			if (!isFromCollection)
			{
				exitThisMenu(ShouldPlayExitSound());
			}
			else
			{
				destroy = true;
			}
		}
		if (Game1.activeClickableMenu == null && Game1.currentMinigame == null)
		{
			unload();
			return;
		}
		if (ShouldShowInteractable())
		{
			for (int i = 0; i < itemsToGrab.Count; i++)
			{
				ClickableComponent clickableComponent = itemsToGrab[i];
				if (clickableComponent.containsPoint(x, y) && clickableComponent.item != null)
				{
					Game1.playSound("coin");
					Game1.player.addItemByMenuIfNecessary(clickableComponent.item);
					clickableComponent.item = null;
					if (itemsToGrab.Count > 1)
					{
						itemsToGrab.RemoveAt(i);
					}
					return;
				}
			}
		}
		if (backButton.containsPoint(x, y) && page > 0)
		{
			page--;
			Game1.playSound("shwip");
			OnPageChange();
		}
		else if (forwardButton.containsPoint(x, y) && page < mailMessage.Count - 1)
		{
			page++;
			Game1.playSound("shwip");
			OnPageChange();
		}
		else if (ShouldShowInteractable() && acceptQuestButton != null && acceptQuestButton.containsPoint(x, y))
		{
			AcceptQuest();
		}
		else if (isWithinBounds(x, y))
		{
			if (page < mailMessage.Count - 1)
			{
				page++;
				Game1.playSound("shwip");
				OnPageChange();
			}
			else if (!isMail)
			{
				exitThisMenuNoSound();
				Game1.playSound("shwip");
			}
			else if (isFromCollection)
			{
				destroy = true;
			}
		}
		else if (!itemsLeftToGrab())
		{
			if (!isFromCollection)
			{
				exitThisMenuNoSound();
				Game1.playSound("shwip");
			}
			else
			{
				destroy = true;
			}
		}
	}

	public virtual bool ShouldPlayExitSound()
	{
		if (HasQuestOrSpecialOrder)
		{
			return false;
		}
		if (isFromCollection)
		{
			return false;
		}
		return true;
	}

	public bool itemsLeftToGrab()
	{
		foreach (ClickableComponent item in itemsToGrab)
		{
			if (item.item != null)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Add the attached quest or special order to the player.</summary>
	public void AcceptQuest()
	{
		if (questID != null)
		{
			Game1.player.addQuest(questID);
			if (questID == "20")
			{
				MineShaft.CheckForQiChallengeCompletion();
			}
			questID = null;
			Game1.playSound("newArtifact");
		}
		else if (specialOrderId != null)
		{
			Game1.player.team.AddSpecialOrder(specialOrderId);
			specialOrderId = null;
			Game1.playSound("newArtifact");
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		if (ShouldShowInteractable())
		{
			foreach (ClickableComponent item in itemsToGrab)
			{
				if (item.containsPoint(x, y))
				{
					item.scale = Math.Min(item.scale + 0.03f, 1.1f);
				}
				else
				{
					item.scale = Math.Max(1f, item.scale - 0.03f);
				}
			}
		}
		backButton.tryHover(x, y, 0.6f);
		forwardButton.tryHover(x, y, 0.6f);
		if (ShouldShowInteractable() && HasQuestOrSpecialOrder)
		{
			float num = acceptQuestButton.scale;
			acceptQuestButton.scale = (acceptQuestButton.bounds.Contains(x, y) ? 1.5f : 1f);
			if (acceptQuestButton.scale > num)
			{
				Game1.playSound("Cowboy_gunshot");
			}
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		forwardButton.visible = page < mailMessage.Count - 1;
		backButton.visible = page > 0;
		if (scale < 1f)
		{
			scale += (float)time.ElapsedGameTime.Milliseconds * 0.003f;
			if (scale >= 1f)
			{
				scale = 1f;
			}
		}
		if (page < mailMessage.Count - 1 && !forwardButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()))
		{
			forwardButton.scale = 4f + (float)Math.Sin((double)(float)time.TotalGameTime.Milliseconds / (Math.PI * 64.0)) / 1.5f;
		}
	}

	public virtual Color? getTextColor()
	{
		if (customTextColor.HasValue)
		{
			return customTextColor.Value;
		}
		if (usingCustomBackground)
		{
			return null;
		}
		return whichBG switch
		{
			1 => SpriteText.color_Gray, 
			2 => SpriteText.color_Cyan, 
			3 => SpriteText.color_White, 
			4 => SpriteText.color_JojaBlue, 
			_ => null, 
		};
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (!Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
		}
		b.Draw(letterTexture, new Vector2(xPositionOnScreen + width / 2, yPositionOnScreen + height / 2), new Rectangle(whichBG % 4 * 320, (whichBG >= 4) ? (204 + (whichBG / 4 - 1) * 180) : 0, 320, 180), Color.White, 0f, new Vector2(160f, 90f), 4f * scale, SpriteEffects.None, 0.86f);
		if (scale == 1f)
		{
			if (secretNoteImage != -1)
			{
				b.Draw(secretNoteImageTexture, new Vector2(xPositionOnScreen + width / 2 - 128 - 4, yPositionOnScreen + height / 2 - 128 + 8), new Rectangle(secretNoteImage * 64 % secretNoteImageTexture.Width, secretNoteImage * 64 / secretNoteImageTexture.Width * 64, 64, 64), Color.Black * 0.4f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.865f);
				b.Draw(secretNoteImageTexture, new Vector2(xPositionOnScreen + width / 2 - 128, yPositionOnScreen + height / 2 - 128), new Rectangle(secretNoteImage * 64 % secretNoteImageTexture.Width, secretNoteImage * 64 / secretNoteImageTexture.Width * 64, 64, 64), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.865f);
				b.Draw(secretNoteImageTexture, new Vector2(xPositionOnScreen + width / 2 - 40, yPositionOnScreen + height / 2 - 192), new Rectangle(193, 65, 14, 21), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.867f);
			}
			else
			{
				SpriteText.drawString(b, mailMessage[page], xPositionOnScreen + 32, yPositionOnScreen + 32, 999999, width - 64, 999999, 0.75f, 0.865f, junimoText: false, -1, "", getTextColor());
			}
			if (ShouldShowInteractable())
			{
				using (List<ClickableComponent>.Enumerator enumerator = itemsToGrab.GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						ClickableComponent current = enumerator.Current;
						b.Draw(letterTexture, current.bounds, new Rectangle(whichBG * 24, 180, 24, 24), Color.White);
						current.item?.drawInMenu(b, new Vector2(current.bounds.X + 16, current.bounds.Y + 16), current.scale);
					}
				}
				if (moneyIncluded > 0)
				{
					string s = Game1.content.LoadString("Strings\\UI:LetterViewer_MoneyIncluded", moneyIncluded);
					SpriteText.drawString(b, s, xPositionOnScreen + width / 2 - SpriteText.getWidthOfString(s) / 2, yPositionOnScreen + height - 96, 999999, -1, 9999, 0.75f, 0.865f, junimoText: false, -1, "", getTextColor());
				}
				else
				{
					string text = learnedRecipe;
					if (text != null && text.Length > 0)
					{
						string s2 = Game1.content.LoadString("Strings\\UI:LetterViewer_LearnedRecipe", cookingOrCrafting);
						SpriteText.drawStringHorizontallyCenteredAt(b, s2, xPositionOnScreen + width / 2, yPositionOnScreen + height - 32 - SpriteText.getHeightOfString(s2) * 2, 999999, -1, 9999, 0.65f, 0.865f, junimoText: false, getTextColor());
						SpriteText.drawStringHorizontallyCenteredAt(b, Game1.content.LoadString("Strings\\UI:LetterViewer_LearnedRecipeName", learnedRecipe), xPositionOnScreen + width / 2, yPositionOnScreen + height - 32 - SpriteText.getHeightOfString("t"), 999999, -1, 9999, 0.9f, 0.865f, junimoText: false, getTextColor());
					}
				}
			}
			base.draw(b);
			forwardButton.draw(b);
			backButton.draw(b);
			if (ShouldShowInteractable() && HasQuestOrSpecialOrder)
			{
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9), acceptQuestButton.bounds.X, acceptQuestButton.bounds.Y, acceptQuestButton.bounds.Width, acceptQuestButton.bounds.Height, (acceptQuestButton.scale > 1f) ? Color.LightPink : Color.White, 4f * acceptQuestButton.scale);
				Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:AcceptQuest"), Game1.dialogueFont, new Vector2(acceptQuestButton.bounds.X + 12, acceptQuestButton.bounds.Y + (LocalizedContentManager.CurrentLanguageLatin ? 16 : 12)), Game1.textColor);
			}
		}
		if ((!Game1.options.SnappyMenus || !(scale < 1f)) && (!Game1.options.SnappyMenus || forwardButton.visible || backButton.visible || HasQuestOrSpecialOrder || itemsLeftToGrab()))
		{
			drawMouse(b);
		}
	}

	public virtual bool ShouldShowInteractable()
	{
		if (!HasInteractable())
		{
			return false;
		}
		return page == mailMessage.Count - 1;
	}

	public virtual bool HasInteractable()
	{
		if (isFromCollection)
		{
			return false;
		}
		if (HasQuestOrSpecialOrder)
		{
			return true;
		}
		if (moneyIncluded > 0)
		{
			return true;
		}
		if (itemsToGrab.Count > 0)
		{
			return true;
		}
		string text = learnedRecipe;
		if (text != null && text.Length > 0)
		{
			return true;
		}
		return false;
	}

	public void unload()
	{
	}

	/// <inheritdoc />
	protected override void cleanupBeforeExit()
	{
		if (HasQuestOrSpecialOrder)
		{
			AcceptQuest();
		}
		if (itemsLeftToGrab())
		{
			List<Item> list = new List<Item>();
			foreach (ClickableComponent item in itemsToGrab)
			{
				if (item.item != null)
				{
					list.Add(item.item);
				}
			}
			itemsToGrab.Clear();
			if (list.Count > 0)
			{
				Game1.playSound("coin");
				Game1.player.addItemsByMenuIfNecessary(list);
			}
		}
		if (isFromCollection)
		{
			destroy = true;
			Game1.oldKBState = Game1.GetKeyboardState();
			Game1.oldMouseState = Game1.input.GetMouseState();
			Game1.oldPadState = Game1.input.GetGamePadState();
		}
		base.cleanupBeforeExit();
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (isFromCollection)
		{
			destroy = true;
		}
		else
		{
			receiveLeftClick(x, y, playSound);
		}
	}
}
