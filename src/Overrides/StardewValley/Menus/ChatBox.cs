using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Extensions;
using StardewValley.Logging;

namespace StardewValley.Menus;

public class ChatBox : IClickableMenu
{
	public const int chatMessage = 0;

	public const int errorMessage = 1;

	public const int userNotificationMessage = 2;

	public const int privateMessage = 3;

	public const int defaultMaxMessages = 10;

	public const int timeToDisplayMessages = 600;

	public const int chatboxWidth = 896;

	public const int chatboxHeight = 56;

	public const int region_chatBox = 101;

	public const int region_emojiButton = 102;

	public ChatTextBox chatBox;

	public ClickableComponent chatBoxCC;

	/// <summary>A logger which copies messages to the chat box, used when entering commands through the chat.</summary>
	private readonly IGameLogger CheatCommandChatLogger;

	public List<ChatMessage> messages = new List<ChatMessage>();

	private KeyboardState oldKBState;

	private List<string> cheatHistory = new List<string>();

	private int cheatHistoryPosition = -1;

	public int maxMessages = 10;

	public static Texture2D emojiTexture;

	public ClickableTextureComponent emojiMenuIcon;

	public EmojiMenu emojiMenu;

	public bool choosingEmoji;

	private long lastReceivedPrivateMessagePlayerId;

	public ChatBox()
	{
		CheatCommandChatLogger = new CheatCommandChatLogger(this);
		Texture2D texture2D = Game1.content.Load<Texture2D>("LooseSprites\\chatBox");
		chatBox = new ChatTextBox(texture2D, null, Game1.smallFont, Color.White);
		chatBox.OnEnterPressed += textBoxEnter;
		chatBox.TitleText = "Chat";
		chatBoxCC = new ClickableComponent(new Rectangle(chatBox.X, chatBox.Y, chatBox.Width, chatBox.Height), "")
		{
			myID = 101
		};
		Game1.keyboardDispatcher.Subscriber = chatBox;
		emojiTexture = Game1.content.Load<Texture2D>("LooseSprites\\emojis");
		emojiMenuIcon = new ClickableTextureComponent(new Rectangle(0, 0, 40, 36), emojiTexture, new Rectangle(0, 0, 9, 9), 4f)
		{
			myID = 102,
			leftNeighborID = 101
		};
		emojiMenu = new EmojiMenu(this, emojiTexture, texture2D);
		chatBoxCC.rightNeighborID = 102;
		updatePosition();
		chatBox.Selected = false;
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(101);
		snapCursorToCurrentSnappedComponent();
	}

	private void updatePosition()
	{
		chatBox.Width = 896;
		chatBox.Height = 56;
		width = chatBox.Width;
		height = chatBox.Height;
		xPositionOnScreen = 0;
		yPositionOnScreen = Game1.uiViewport.Height - chatBox.Height;
		Utility.makeSafe(ref xPositionOnScreen, ref yPositionOnScreen, chatBox.Width, chatBox.Height);
		chatBox.X = xPositionOnScreen;
		chatBox.Y = yPositionOnScreen;
		chatBoxCC.bounds = new Rectangle(chatBox.X, chatBox.Y, chatBox.Width, chatBox.Height);
		emojiMenuIcon.bounds.Y = chatBox.Y + 8;
		emojiMenuIcon.bounds.X = chatBox.Width - emojiMenuIcon.bounds.Width - 8;
		if (emojiMenu != null)
		{
			emojiMenu.xPositionOnScreen = emojiMenuIcon.bounds.Center.X - 146;
			emojiMenu.yPositionOnScreen = emojiMenuIcon.bounds.Y - 248;
		}
	}

	public virtual void textBoxEnter(string text_to_send)
	{
		if (text_to_send.Length < 1)
		{
			return;
		}
		if (text_to_send[0] == '/')
		{
			string firstWord = ArgUtility.SplitBySpaceAndGet(text_to_send, 0);
			if (string.Equals(firstWord, "/debugPoke", System.StringComparison.OrdinalIgnoreCase))
			{
				StardewValley.Pokemon.PokemonDebugMenu.IsDebugUnlocked = true;
				addInfoMessage("🔧 [Pokémon] Modo Debug Desbloqueado! Pressione [F8] para abrir o menu.");
				Game1.playSound("reward");
				return;
			}
			string text = firstWord;
			if (text != null && text.Length > 1)
			{
				runCommand(text_to_send.Substring(1));
				return;
			}
		}
		text_to_send = Program.sdk.FilterDirtyWords(text_to_send);
		Game1.multiplayer.sendChatMessage(LocalizedContentManager.CurrentLanguageCode, text_to_send, Multiplayer.AllPlayers);
		receiveChatMessage(Game1.player.UniqueMultiplayerID, 0, LocalizedContentManager.CurrentLanguageCode, text_to_send);
	}

	public virtual void textBoxEnter(TextBox sender)
	{
		bool include_color_information;
		if (sender is ChatTextBox chatTextBox)
		{
			if (chatTextBox.finalText.Count > 0)
			{
				include_color_information = true;
				string message = chatTextBox.finalText[0].message;
				if (message != null && message.StartsWith('/'))
				{
					string text = ArgUtility.SplitBySpaceAndGet(chatTextBox.finalText[0].message, 0);
					if (text != null && text.Length > 1)
					{
						include_color_information = false;
					}
				}
				if (chatTextBox.finalText.Count != 1)
				{
					goto IL_00c8;
				}
				if (chatTextBox.finalText[0].message != null || chatTextBox.finalText[0].emojiIndex != -1)
				{
					string message2 = chatTextBox.finalText[0].message;
					if (message2 == null || message2.Trim().Length != 0)
					{
						goto IL_00c8;
					}
				}
			}
			goto IL_00dc;
		}
		goto IL_00e9;
		IL_00e9:
		sender.Text = "";
		clickAway();
		return;
		IL_00dc:
		chatTextBox.reset();
		cheatHistoryPosition = -1;
		goto IL_00e9;
		IL_00c8:
		string text_to_send = ChatMessage.makeMessagePlaintext(chatTextBox.finalText, include_color_information);
		textBoxEnter(text_to_send);
		goto IL_00dc;
	}

	public virtual void addInfoMessage(string message)
	{
		receiveChatMessage(0L, 2, LocalizedContentManager.CurrentLanguageCode, message);
	}

	public virtual void globalInfoMessage(string messageKey, params string[] args)
	{
		if (Game1.IsMultiplayer)
		{
			Game1.multiplayer.globalChatInfoMessage(messageKey, args);
		}
		else
		{
			addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_" + messageKey, args));
		}
	}

	public virtual void addErrorMessage(string message)
	{
		receiveChatMessage(0L, 1, LocalizedContentManager.CurrentLanguageCode, message);
	}

	public virtual void listPlayers(bool otherPlayersOnly = false, bool onlineOnly = true)
	{
		addInfoMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_ListOnlinePlayers"));
		IEnumerable<Farmer> enumerable;
		if (!onlineOnly)
		{
			enumerable = Game1.getAllFarmers();
		}
		else
		{
			IEnumerable<Farmer> onlineFarmers = Game1.getOnlineFarmers();
			enumerable = onlineFarmers;
		}
		foreach (Farmer item in enumerable)
		{
			if (!otherPlayersOnly || item.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
			{
				addInfoMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_ListOnlinePlayersEntry", formattedUserNameLong(item)));
			}
		}
	}

	protected virtual void runCommand(string commandText)
	{
		if (!ChatCommands.TryHandle(ArgUtility.SplitBySpace(commandText), this) && (ChatCommands.AllowCheats || Game1.isRunningMacro))
		{
			cheat(commandText);
		}
	}

	public virtual void cheat(string command, bool isDebug = false)
	{
		string text = (isDebug ? "debug " : "") + command;
		Game1.debugOutput = null;
		addInfoMessage("/" + text);
		if (!Game1.isRunningMacro)
		{
			cheatHistory.Insert(0, "/" + text);
		}
		if (Game1.game1.parseDebugInput(command, CheatCommandChatLogger))
		{
			if (!string.IsNullOrEmpty(Game1.debugOutput))
			{
				addInfoMessage(Game1.debugOutput);
			}
		}
		else if (!string.IsNullOrEmpty(Game1.debugOutput))
		{
			addErrorMessage(Game1.debugOutput);
		}
		else
		{
			addErrorMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:ChatBox.cs.10261") + " " + ArgUtility.SplitBySpaceAndGet(command, 0));
		}
	}

	public void replyPrivateMessage(string[] command)
	{
		if (!Game1.IsMultiplayer)
		{
			return;
		}
		Farmer value;
		if (lastReceivedPrivateMessagePlayerId == 0L)
		{
			addErrorMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_Reply_NoMessageFound"));
		}
		else if (!Game1.otherFarmers.TryGetValue(lastReceivedPrivateMessagePlayerId, out value) || !value.isActive())
		{
			addErrorMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_Reply_Failed"));
		}
		else
		{
			if (command.Length <= 1)
			{
				return;
			}
			string text = "";
			for (int i = 1; i < command.Length; i++)
			{
				text += command[i];
				if (i < command.Length - 1)
				{
					text += " ";
				}
			}
			text = Program.sdk.FilterDirtyWords(text);
			Game1.multiplayer.sendChatMessage(LocalizedContentManager.CurrentLanguageCode, text, lastReceivedPrivateMessagePlayerId);
			receiveChatMessage(Game1.player.UniqueMultiplayerID, 3, LocalizedContentManager.CurrentLanguageCode, text);
		}
	}

	public Farmer findMatchingFarmer(string[] command, ref int matchingIndex, bool allowMatchingByUserName = false, bool onlineOnly = true)
	{
		Farmer result = null;
		IEnumerable<Farmer> enumerable;
		if (!onlineOnly)
		{
			enumerable = Game1.getAllFarmers();
		}
		else
		{
			IEnumerable<Farmer> values = Game1.otherFarmers.Values;
			enumerable = values;
		}
		foreach (Farmer item in enumerable)
		{
			string[] array = ArgUtility.SplitBySpace(item.displayName);
			bool flag = true;
			int i;
			for (i = 0; i < array.Length; i++)
			{
				if (command.Length > i + 1)
				{
					if (!command[i + 1].EqualsIgnoreCase(array[i]))
					{
						flag = false;
						break;
					}
					continue;
				}
				flag = false;
				break;
			}
			if (flag)
			{
				result = item;
				matchingIndex = i;
				break;
			}
			if (!allowMatchingByUserName)
			{
				continue;
			}
			flag = true;
			string[] array2 = ArgUtility.SplitBySpace(Game1.multiplayer.getUserName(item.UniqueMultiplayerID));
			if (array2.Length == 0)
			{
				continue;
			}
			for (i = 0; i < array2.Length; i++)
			{
				if (command.Length > i + 1)
				{
					if (!command[i + 1].EqualsIgnoreCase(array2[i]))
					{
						flag = false;
						break;
					}
					continue;
				}
				flag = false;
				break;
			}
			if (flag)
			{
				result = item;
				matchingIndex = i;
				break;
			}
		}
		return result;
	}

	public void sendPrivateMessage(string[] command)
	{
		if (!Game1.IsMultiplayer)
		{
			return;
		}
		int matchingIndex = 0;
		Farmer farmer = findMatchingFarmer(command, ref matchingIndex);
		if (farmer == null)
		{
			addErrorMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_Error_NoSuchOnlinePlayer"));
			return;
		}
		string text = "";
		for (int i = matchingIndex + 1; i < command.Length; i++)
		{
			text += command[i];
			if (i < command.Length - 1)
			{
				text += " ";
			}
		}
		text = Program.sdk.FilterDirtyWords(text);
		Game1.multiplayer.sendChatMessage(LocalizedContentManager.CurrentLanguageCode, text, farmer.UniqueMultiplayerID);
		receiveChatMessage(Game1.player.UniqueMultiplayerID, 3, LocalizedContentManager.CurrentLanguageCode, text);
	}

	public bool isActive()
	{
		return chatBox.Selected;
	}

	public void activate()
	{
		chatBox.Selected = true;
		setText("");
	}

	public override void clickAway()
	{
		base.clickAway();
		if (!choosingEmoji || !emojiMenu.isWithinBounds(Game1.getMouseX(), Game1.getMouseY()) || Game1.input.GetKeyboardState().IsKeyDown(Keys.Escape))
		{
			bool selected = chatBox.Selected;
			chatBox.Selected = false;
			choosingEmoji = false;
			setText("");
			cheatHistoryPosition = -1;
			if (selected)
			{
				Game1.oldKBState = Game1.GetKeyboardState();
			}
		}
	}

	public override bool isWithinBounds(int x, int y)
	{
		if (x - xPositionOnScreen >= width || x - xPositionOnScreen < 0 || y - yPositionOnScreen >= height || y - yPositionOnScreen < -getOldMessagesBoxHeight())
		{
			if (choosingEmoji)
			{
				return emojiMenu.isWithinBounds(x, y);
			}
			return false;
		}
		return true;
	}

	public virtual void setText(string text)
	{
		chatBox.setText(text);
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		switch (key)
		{
		case Keys.Up:
			if (cheatHistoryPosition < cheatHistory.Count - 1)
			{
				cheatHistoryPosition++;
				string text2 = cheatHistory[cheatHistoryPosition];
				chatBox.setText(text2);
			}
			break;
		case Keys.Down:
			if (cheatHistoryPosition > 0)
			{
				cheatHistoryPosition--;
				string text = cheatHistory[cheatHistoryPosition];
				chatBox.setText(text);
			}
			break;
		}
		if (!Game1.options.doesInputListContain(Game1.options.moveUpButton, key) && !Game1.options.doesInputListContain(Game1.options.moveRightButton, key) && !Game1.options.doesInputListContain(Game1.options.moveDownButton, key) && !Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
		{
			base.receiveKeyPress(key);
		}
	}

	public override bool readyToClose()
	{
		return false;
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
	}

	public bool isHoveringOverClickable(int x, int y)
	{
		if (emojiMenuIcon.containsPoint(x, y) || (choosingEmoji && emojiMenu.isWithinBounds(x, y)))
		{
			return true;
		}
		return false;
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (!chatBox.Selected)
		{
			return;
		}
		if (emojiMenuIcon.containsPoint(x, y))
		{
			choosingEmoji = !choosingEmoji;
			Game1.playSound("shwip");
			emojiMenuIcon.scale = 4f;
			return;
		}
		if (choosingEmoji && emojiMenu.isWithinBounds(x, y))
		{
			emojiMenu.leftClick(x, y, this);
			return;
		}
		chatBox.Update();
		if (choosingEmoji)
		{
			choosingEmoji = false;
			emojiMenuIcon.scale = 4f;
		}
		if (isWithinBounds(x, y))
		{
			chatBox.Selected = true;
		}
	}

	public static string formattedUserName(Farmer farmer)
	{
		string text = farmer.Name;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = Game1.content.LoadString("Strings\\UI:Chat_PlayerJoinedNewName");
		}
		return Program.sdk.FilterDirtyWords(text);
	}

	public static string formattedUserNameLong(Farmer farmer)
	{
		string text = formattedUserName(farmer);
		string userName = Game1.multiplayer.getUserName(farmer.UniqueMultiplayerID);
		if (string.IsNullOrWhiteSpace(userName))
		{
			return text;
		}
		return Game1.content.LoadString("Strings\\UI:Chat_PlayerName", text, userName);
	}

	public string formatMessage(long sourceFarmer, int chatKind, string message)
	{
		string sub = Game1.content.LoadString("Strings\\UI:Chat_UnknownUserName");
		Farmer value;
		if (sourceFarmer == Game1.player.UniqueMultiplayerID)
		{
			value = Game1.player;
		}
		else if (!Game1.otherFarmers.TryGetValue(sourceFarmer, out value))
		{
			value = null;
		}
		if (value != null)
		{
			sub = formattedUserName(value);
		}
		return chatKind switch
		{
			0 => Game1.content.LoadString("Strings\\UI:Chat_ChatMessageFormat", sub, message), 
			2 => Game1.content.LoadString("Strings\\UI:Chat_UserNotificationMessageFormat", message), 
			3 => Game1.content.LoadString("Strings\\UI:Chat_PrivateMessageFormat", sub, message), 
			_ => Game1.content.LoadString("Strings\\UI:Chat_ErrorMessageFormat", message), 
		};
	}

	public virtual Color messageColor(int chatKind)
	{
		return chatKind switch
		{
			0 => chatBox.TextColor, 
			3 => Color.DarkCyan, 
			2 => Color.Yellow, 
			_ => Color.Red, 
		};
	}

	public virtual void receiveChatMessage(long sourceFarmer, int chatKind, LocalizedContentManager.LanguageCode language, string message)
	{
		if (message != null && message.StartsWith("[POKE_TRADE]:"))
		{
			StardewValley.Pokemon.PokemonTradeMenu.HandleNetworkTradePacket(sourceFarmer, message.Substring("[POKE_TRADE]:".Length));
			return;
		}
		if (message != null && message.StartsWith("[POKE_BATTLE]:"))
		{
			StardewValley.Pokemon.PokemonPvPManager.ReceiveBattlePacket(sourceFarmer, message.Substring("[POKE_BATTLE]:".Length));
			return;
		}

		string text = formatMessage(sourceFarmer, chatKind, message);
		ChatMessage chatMessage = new ChatMessage();
		string text2 = Game1.parseText(text, chatBox.Font, chatBox.Width - 16);
		chatMessage.timeLeftToDisplay = 600;
		chatMessage.verticalSize = (int)chatBox.Font.MeasureString(text2).Y + 4;
		chatMessage.color = messageColor(chatKind);
		chatMessage.language = language;
		chatMessage.parseMessageForEmoji(text2);
		messages.Add(chatMessage);
		if (messages.Count > maxMessages)
		{
			messages.RemoveAt(0);
		}
		if (chatKind == 3 && sourceFarmer != Game1.player.UniqueMultiplayerID)
		{
			lastReceivedPrivateMessagePlayerId = sourceFarmer;
		}
	}

	public virtual void addMessage(string message, Color color)
	{
		ChatMessage chatMessage = new ChatMessage();
		string text = Game1.parseText(message, chatBox.Font, chatBox.Width - 8);
		chatMessage.timeLeftToDisplay = 600;
		chatMessage.verticalSize = (int)chatBox.Font.MeasureString(text).Y + 4;
		chatMessage.color = color;
		chatMessage.language = LocalizedContentManager.CurrentLanguageCode;
		chatMessage.parseMessageForEmoji(text);
		messages.Add(chatMessage);
		if (messages.Count > maxMessages)
		{
			messages.RemoveAt(0);
		}
	}

	/// <summary>Add a "ConcernedApe: Nice try..." Easter egg message to the chat box.</summary>
	public void addNiceTryEasterEggMessage()
	{
		addMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_Error_NiceTry"), new Color(104, 214, 255));
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		emojiMenuIcon.tryHover(x, y, 1f);
		emojiMenuIcon.tryHover(x, y, 1f);
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		KeyboardState keyboardState = Game1.input.GetKeyboardState();
		Keys[] pressedKeys = keyboardState.GetPressedKeys();
		foreach (Keys key in pressedKeys)
		{
			if (!oldKBState.IsKeyDown(key))
			{
				receiveKeyPress(key);
			}
		}
		oldKBState = keyboardState;
		for (int j = 0; j < messages.Count; j++)
		{
			if (messages[j].timeLeftToDisplay > 0)
			{
				messages[j].timeLeftToDisplay--;
			}
			if (messages[j].timeLeftToDisplay < 75)
			{
				messages[j].alpha = (float)messages[j].timeLeftToDisplay / 75f;
			}
		}
		if (chatBox.Selected)
		{
			foreach (ChatMessage message in messages)
			{
				message.alpha = 1f;
			}
		}
		emojiMenuIcon.tryHover(0, 0, 1f);
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		if (choosingEmoji)
		{
			emojiMenu.receiveScrollWheelAction(direction);
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		updatePosition();
	}

	public static SpriteFont messageFont(LocalizedContentManager.LanguageCode language)
	{
		return Game1.content.Load<SpriteFont>("Fonts\\SmallFont", language);
	}

	public int getOldMessagesBoxHeight()
	{
		int num = 20;
		for (int num2 = messages.Count - 1; num2 >= 0; num2--)
		{
			ChatMessage chatMessage = messages[num2];
			if (chatBox.Selected || chatMessage.alpha > 0.01f)
			{
				num += chatMessage.verticalSize;
			}
		}
		return num;
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		int num = 0;
		bool flag = false;
		for (int num2 = messages.Count - 1; num2 >= 0; num2--)
		{
			ChatMessage chatMessage = messages[num2];
			if (chatBox.Selected || chatMessage.alpha > 0.01f)
			{
				num += chatMessage.verticalSize;
				flag = true;
			}
		}
		if (flag)
		{
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(301, 288, 15, 15), xPositionOnScreen, yPositionOnScreen - num - 20 + ((!chatBox.Selected) ? chatBox.Height : 0), chatBox.Width, num + 20, Color.White, 4f, drawShadow: false);
		}
		num = 0;
		for (int num3 = messages.Count - 1; num3 >= 0; num3--)
		{
			ChatMessage chatMessage2 = messages[num3];
			num += chatMessage2.verticalSize;
			chatMessage2.draw(b, xPositionOnScreen + 12, yPositionOnScreen - num - 8 + ((!chatBox.Selected) ? chatBox.Height : 0));
		}
		if (chatBox.Selected)
		{
			chatBox.Draw(b, drawShadow: false);
			emojiMenuIcon.draw(b, Color.White, 0.99f);
			if (choosingEmoji)
			{
				emojiMenu.draw(b);
			}
			if (isWithinBounds(Game1.getMouseX(), Game1.getMouseY()) && !Game1.options.hardwareCursor)
			{
				Game1.mouseCursor = (Game1.options.gamepadControls ? Game1.cursor_gamepad_pointer : Game1.cursor_default);
			}
		}
	}
}
