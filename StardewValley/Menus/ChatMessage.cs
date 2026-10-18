using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Menus;

public class ChatMessage
{
	public List<ChatSnippet> message = new List<ChatSnippet>();

	public int timeLeftToDisplay;

	public int verticalSize;

	public float alpha = 1f;

	public Color color;

	public LocalizedContentManager.LanguageCode language;

	public void parseMessageForEmoji(string messagePlaintext)
	{
		if (messagePlaintext == null)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < messagePlaintext.Length; i++)
		{
			if (messagePlaintext[i] == '[')
			{
				if (stringBuilder.Length > 0)
				{
					breakNewLines(stringBuilder);
				}
				stringBuilder.Clear();
				int num = messagePlaintext.IndexOf(']', i);
				int num2 = -1;
				if (i + 1 < messagePlaintext.Length)
				{
					num2 = messagePlaintext.IndexOf('[', i + 1);
				}
				if (num != -1 && (num2 == -1 || num2 > num))
				{
					string text = messagePlaintext.Substring(i + 1, num - i - 1);
					if (int.TryParse(text, out var result))
					{
						if (result < EmojiMenu.totalEmojis)
						{
							message.Add(new ChatSnippet(result));
						}
					}
					else
					{
						switch (text)
						{
						case "gray":
						case "jade":
						case "pink":
						case "plum":
						case "aqua":
						case "blue":
						case "jungle":
						case "yellow":
						case "orange":
						case "purple":
						case "salmon":
						case "green":
						case "peach":
						case "brown":
						case "cream":
						case "red":
						case "yellowgreen":
							if (color.Equals(Color.White))
							{
								color = getColorFromName(text);
							}
							break;
						default:
							stringBuilder.Append("[");
							stringBuilder.Append(text);
							stringBuilder.Append("]");
							break;
						}
					}
					i = num;
				}
				else
				{
					stringBuilder.Append("[");
				}
			}
			else
			{
				stringBuilder.Append(messagePlaintext[i]);
			}
		}
		if (stringBuilder.Length > 0)
		{
			breakNewLines(stringBuilder);
		}
	}

	public static Color getColorFromName(string name)
	{
		return name switch
		{
			"aqua" => Color.MediumTurquoise, 
			"jungle" => Color.SeaGreen, 
			"red" => new Color(220, 20, 20), 
			"blue" => Color.DodgerBlue, 
			"jade" => new Color(50, 230, 150), 
			"green" => new Color(0, 180, 10), 
			"yellowgreen" => new Color(182, 214, 0), 
			"pink" => Color.HotPink, 
			"yellow" => new Color(240, 200, 0), 
			"orange" => new Color(255, 100, 0), 
			"purple" => new Color(138, 43, 250), 
			"gray" => Color.Gray, 
			"cream" => new Color(255, 255, 180), 
			"peach" => new Color(255, 180, 120), 
			"brown" => new Color(160, 80, 30), 
			"salmon" => Color.Salmon, 
			"plum" => new Color(190, 0, 190), 
			_ => Color.White, 
		};
	}

	private void breakNewLines(StringBuilder sb)
	{
		string[] array = sb.ToString().Split(Environment.NewLine);
		for (int i = 0; i < array.Length; i++)
		{
			message.Add(new ChatSnippet(array[i], language));
			if (i != array.Length - 1)
			{
				message.Add(new ChatSnippet(Environment.NewLine, language));
			}
		}
	}

	public static string makeMessagePlaintext(List<ChatSnippet> message, bool include_color_information)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ChatSnippet item in message)
		{
			if (item.message != null)
			{
				stringBuilder.Append(item.message);
			}
			else if (item.emojiIndex != -1)
			{
				stringBuilder.Append("[" + item.emojiIndex + "]");
			}
		}
		if (include_color_information && Game1.player.defaultChatColor != null && !getColorFromName(Game1.player.defaultChatColor).Equals(Color.White))
		{
			stringBuilder.Append(" [");
			stringBuilder.Append(Game1.player.defaultChatColor);
			stringBuilder.Append("]");
		}
		return stringBuilder.ToString();
	}

	public void draw(SpriteBatch b, int x, int y)
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < message.Count; i++)
		{
			if (message[i].emojiIndex != -1)
			{
				b.Draw(ChatBox.emojiTexture, new Vector2((float)x + num + 1f, (float)y + num2 - 4f), new Rectangle(message[i].emojiIndex * 9 % ChatBox.emojiTexture.Width, message[i].emojiIndex * 9 / ChatBox.emojiTexture.Width * 9, 9, 9), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.99f);
			}
			else if (message[i].message != null)
			{
				if (message[i].message.Equals(Environment.NewLine))
				{
					num = 0f;
					num2 += ChatBox.messageFont(language).MeasureString("(").Y;
				}
				else
				{
					b.DrawString(ChatBox.messageFont(language), message[i].message, new Vector2((float)x + num, (float)y + num2), color * alpha, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.99f);
				}
			}
			num += message[i].myLength;
			if (num >= 888f)
			{
				num = 0f;
				num2 += ChatBox.messageFont(language).MeasureString("(").Y;
				if (message.Count > i + 1 && message[i + 1].message != null && message[i + 1].message.Equals(Environment.NewLine))
				{
					i++;
				}
			}
		}
	}
}
