using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Menus;

public class EmojiMenu : IClickableMenu
{
	public const int EMOJI_SIZE = 9;

	private Texture2D chatBoxTexture;

	private Texture2D emojiTexture;

	private ChatBox chatBox;

	private List<ClickableComponent> emojiSelectionButtons = new List<ClickableComponent>();

	private int pageStartIndex;

	private ClickableComponent upArrow;

	private ClickableComponent downArrow;

	private ClickableComponent sendArrow;

	public static int totalEmojis;

	public static int totalVisibleEmojis;

	public EmojiMenu(ChatBox chatBox, Texture2D emojiTexture, Texture2D chatBoxTexture)
	{
		this.chatBox = chatBox;
		this.chatBoxTexture = chatBoxTexture;
		this.emojiTexture = emojiTexture;
		width = 300;
		height = 248;
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				emojiSelectionButtons.Add(new ClickableComponent(new Rectangle(16 + j * 10 * 4, 16 + i * 10 * 4, 36, 36), (j + i * 6).ToString() ?? ""));
			}
		}
		upArrow = new ClickableComponent(new Rectangle(256, 16, 32, 20), "");
		downArrow = new ClickableComponent(new Rectangle(256, 156, 32, 20), "");
		sendArrow = new ClickableComponent(new Rectangle(256, 188, 32, 32), "");
		totalEmojis = 197;
		totalVisibleEmojis = 196;
	}

	public void leftClick(int x, int y, ChatBox cb)
	{
		if (!isWithinBounds(x, y))
		{
			return;
		}
		int x2 = x - xPositionOnScreen;
		int y2 = y - yPositionOnScreen;
		if (upArrow.containsPoint(x2, y2))
		{
			upArrowPressed();
		}
		else if (downArrow.containsPoint(x2, y2))
		{
			downArrowPressed();
		}
		else if (sendArrow.containsPoint(x2, y2) && cb.chatBox.currentWidth > 0f)
		{
			cb.textBoxEnter(cb.chatBox);
			sendArrow.scale = 0.5f;
			Game1.playSound("shwip");
		}
		foreach (ClickableComponent emojiSelectionButton in emojiSelectionButtons)
		{
			if (emojiSelectionButton.containsPoint(x2, y2))
			{
				int emoji = pageStartIndex + int.Parse(emojiSelectionButton.name);
				cb.chatBox.receiveEmoji(emoji);
				Game1.playSound("coin");
				break;
			}
		}
	}

	private void upArrowPressed(int amountToScroll = 30)
	{
		if (pageStartIndex != 0)
		{
			Game1.playSound("Cowboy_Footstep");
		}
		pageStartIndex = Math.Max(0, pageStartIndex - amountToScroll);
		upArrow.scale = 0.75f;
	}

	private void downArrowPressed(int amountToScroll = 30)
	{
		if (pageStartIndex != totalVisibleEmojis - 30)
		{
			Game1.playSound("Cowboy_Footstep");
		}
		pageStartIndex = Math.Min(totalVisibleEmojis - 30, pageStartIndex + amountToScroll);
		downArrow.scale = 0.75f;
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		if (direction < 0)
		{
			downArrowPressed(6);
		}
		else if (direction > 0)
		{
			upArrowPressed(6);
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		b.Draw(chatBoxTexture, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), new Rectangle(0, 56, 300, 244), Color.White);
		for (int i = 0; i < emojiSelectionButtons.Count; i++)
		{
			b.Draw(emojiTexture, new Vector2(emojiSelectionButtons[i].bounds.X + xPositionOnScreen, emojiSelectionButtons[i].bounds.Y + yPositionOnScreen), new Rectangle((pageStartIndex + i) * 9 % emojiTexture.Width, (pageStartIndex + i) * 9 / emojiTexture.Width * 9, 9, 9), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
		}
		if (upArrow.scale < 1f)
		{
			upArrow.scale += 0.05f;
		}
		if (downArrow.scale < 1f)
		{
			downArrow.scale += 0.05f;
		}
		if (sendArrow.scale < 1f)
		{
			sendArrow.scale += 0.05f;
		}
		b.Draw(chatBoxTexture, new Vector2(upArrow.bounds.X + xPositionOnScreen + 16, upArrow.bounds.Y + yPositionOnScreen + 10), new Rectangle(156, 300, 32, 20), Color.White * ((pageStartIndex == 0) ? 0.25f : 1f), 0f, new Vector2(16f, 10f), upArrow.scale, SpriteEffects.None, 0.9f);
		b.Draw(chatBoxTexture, new Vector2(downArrow.bounds.X + xPositionOnScreen + 16, downArrow.bounds.Y + yPositionOnScreen + 10), new Rectangle(192, 300, 32, 20), Color.White * ((pageStartIndex == totalVisibleEmojis - 30) ? 0.25f : 1f), 0f, new Vector2(16f, 10f), downArrow.scale, SpriteEffects.None, 0.9f);
		b.Draw(chatBoxTexture, new Vector2(sendArrow.bounds.X + xPositionOnScreen + 16, sendArrow.bounds.Y + yPositionOnScreen + 10), new Rectangle(116, 304, 28, 28), Color.White * ((chatBox.chatBox.currentWidth > 0f) ? 1f : 0.4f), 0f, new Vector2(14f, 16f), sendArrow.scale, SpriteEffects.None, 0.9f);
	}
}
