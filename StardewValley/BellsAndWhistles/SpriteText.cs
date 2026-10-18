using System;
using System.Collections.Generic;
using System.Linq;
using BmFont;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace StardewValley.BellsAndWhistles;

public class SpriteText
{
	public enum ScrollTextAlignment
	{
		Left,
		Center,
		Right
	}

	public const int scrollStyle_scroll = 0;

	public const int scrollStyle_speechBubble = 1;

	public const int scrollStyle_darkMetal = 2;

	public const int scrollStyle_blueMetal = 3;

	public const int maxCharacter = 999999;

	public const int maxHeight = 999999;

	public const int characterWidth = 8;

	public const int characterHeight = 16;

	public const int horizontalSpaceBetweenCharacters = 0;

	public const int verticalSpaceBetweenCharacters = 2;

	public const char newLine = '^';

	public static float fontPixelZoom = 3f;

	public static float shadowAlpha = 0.15f;

	public static Dictionary<char, FontChar> characterMap;

	public static FontFile FontFile = null;

	public static List<Texture2D> fontPages = null;

	public static Texture2D spriteTexture;

	public static Texture2D coloredTexture;

	public const int color_index_Default = -1;

	public const int color_index_Black = 0;

	public const int color_index_Blue = 1;

	public const int color_index_Red = 2;

	public const int color_index_Purple = 3;

	public const int color_index_White = 4;

	public const int color_index_Orange = 5;

	public const int color_index_Green = 6;

	public const int color_index_Cyan = 7;

	public const int color_index_Gray = 8;

	public const int color_index_JojaBlue = 9;

	public static bool forceEnglishFont = false;

	public static float FontPixelZoom => fontPixelZoom + ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh) ? ((Game1.options.dialogueFontScale - 1f) / (Game1.options.useChineseSmoothFont ? 4f : 2f)) : 0f);

	public static Color color_Default
	{
		get
		{
			if (!LocalizedContentManager.CurrentLanguageLatin && (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.ru || Game1.options.useAlternateFont))
			{
				return new Color(86, 22, 12);
			}
			return Color.White;
		}
	}

	public static Color color_Black { get; } = Color.Black;

	public static Color color_Blue { get; } = Color.SkyBlue;

	public static Color color_Red { get; } = Color.Red;

	public static Color color_Purple { get; } = new Color(110, 43, 255);

	public static Color color_White { get; } = Color.White;

	public static Color color_Orange { get; } = Color.OrangeRed;

	public static Color color_Green { get; } = Color.LimeGreen;

	public static Color color_Cyan { get; } = Color.Cyan;

	public static Color color_Gray { get; } = new Color(60, 60, 60);

	public static Color color_JojaBlue { get; } = new Color(52, 50, 122);

	public static void drawStringHorizontallyCenteredAt(SpriteBatch b, string s, int x, int y, int characterPosition = 999999, int width = -1, int height = 999999, float alpha = 1f, float layerDepth = 0.88f, bool junimoText = false, Color? color = null, int maxWidth = 99999)
	{
		drawString(b, s, x - getWidthOfString(s, maxWidth) / 2, y, characterPosition, width, height, alpha, layerDepth, junimoText, -1, "", color);
	}

	public static int getWidthOfString(string s, int widthConstraint = 999999)
	{
		setUpCharacterMap();
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < s.Length; i++)
		{
			if (isUsingNonSpriteSheetFont() && !forceEnglishFont)
			{
				if (characterMap.TryGetValue(s[i], out var value))
				{
					num += value.XAdvance;
				}
				num2 = Math.Max(num, num2);
				if (s[i] == '^' || (float)num * FontPixelZoom > (float)widthConstraint)
				{
					num = 0;
				}
				continue;
			}
			num += 8 + getWidthOffsetForChar(s[i]);
			if (i > 0)
			{
				num += getWidthOffsetForChar(s[Math.Max(0, i - 1)]);
			}
			num2 = Math.Max(num, num2);
			float num3 = positionOfNextSpace(s, i, (int)((float)num * FontPixelZoom), 0);
			if (s[i] == '^' || (float)num * FontPixelZoom >= (float)widthConstraint || num3 >= (float)widthConstraint)
			{
				num = 0;
			}
		}
		return (int)((float)num2 * FontPixelZoom);
	}

	public static bool IsMissingCharacters(string text)
	{
		setUpCharacterMap();
		if (!LocalizedContentManager.CurrentLanguageLatin && !forceEnglishFont)
		{
			for (int i = 0; i < text.Length; i++)
			{
				if (!characterMap.ContainsKey(text[i]))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static int getHeightOfString(string s, int widthConstraint = 999999)
	{
		if (s.Length == 0)
		{
			return 0;
		}
		Vector2 vector = default(Vector2);
		int num = 0;
		s = s.Replace(Environment.NewLine, "");
		setUpCharacterMap();
		if (isUsingNonSpriteSheetFont() && !forceEnglishFont)
		{
			for (int i = 0; i < s.Length; i++)
			{
				if (s[i] == '^')
				{
					vector.Y += (float)(FontFile.Common.LineHeight + 2) * FontPixelZoom;
					vector.X = 0f;
					continue;
				}
				if (positionOfNextSpace(s, i, (int)vector.X, num) >= widthConstraint)
				{
					vector.Y += (float)(FontFile.Common.LineHeight + 2) * FontPixelZoom;
					num = 0;
					vector.X = 0f;
				}
				if (characterMap.TryGetValue(s[i], out var value))
				{
					vector.X += (float)value.XAdvance * FontPixelZoom;
				}
			}
			return (int)(vector.Y + (float)(FontFile.Common.LineHeight + 2) * FontPixelZoom);
		}
		for (int j = 0; j < s.Length; j++)
		{
			if (s[j] == '^')
			{
				vector.Y += 18f * FontPixelZoom;
				vector.X = 0f;
				num = 0;
				continue;
			}
			if (positionOfNextSpace(s, j, (int)vector.X, num) >= widthConstraint)
			{
				vector.Y += 18f * FontPixelZoom;
				num = 0;
				vector.X = 0f;
			}
			vector.X += 8f * FontPixelZoom + (float)num + (float)getWidthOffsetForChar(s[j]) * FontPixelZoom;
			if (j > 0)
			{
				vector.X += (float)getWidthOffsetForChar(s[j - 1]) * FontPixelZoom;
			}
			num = (int)(0f * FontPixelZoom);
		}
		return (int)(vector.Y + 16f * FontPixelZoom);
	}

	public static Color getColorFromIndex(int index)
	{
		return index switch
		{
			1 => color_Blue, 
			2 => color_Red, 
			3 => color_Purple, 
			-1 => color_Default, 
			4 => color_White, 
			5 => color_Orange, 
			6 => color_Green, 
			7 => color_Cyan, 
			8 => color_Gray, 
			9 => color_JojaBlue, 
			_ => Color.Black, 
		};
	}

	public static string getSubstringBeyondHeight(string s, int width, int height)
	{
		Vector2 vector = default(Vector2);
		int num = 0;
		s = s.Replace(Environment.NewLine, "");
		setUpCharacterMap();
		if (isUsingNonSpriteSheetFont())
		{
			for (int i = 0; i < s.Length; i++)
			{
				if (s[i] == '^')
				{
					vector.Y += (float)(FontFile.Common.LineHeight + 2) * FontPixelZoom;
					vector.X = 0f;
					num = 0;
					continue;
				}
				if (characterMap.TryGetValue(s[i], out var value))
				{
					if (i > 0)
					{
						vector.X += (float)value.XAdvance * FontPixelZoom;
					}
					if (positionOfNextSpace(s, i, (int)vector.X, num) >= width)
					{
						vector.Y += (float)(FontFile.Common.LineHeight + 2) * FontPixelZoom;
						num = 0;
						vector.X = 0f;
					}
				}
				if (vector.Y >= (float)height - (float)FontFile.Common.LineHeight * FontPixelZoom * 2f)
				{
					return s.Substring(getLastSpace(s, i));
				}
			}
			return "";
		}
		for (int j = 0; j < s.Length; j++)
		{
			if (s[j] == '^')
			{
				vector.Y += 18f * FontPixelZoom;
				vector.X = 0f;
				num = 0;
				continue;
			}
			if (j > 0)
			{
				vector.X += 8f * FontPixelZoom + (float)num + (float)(getWidthOffsetForChar(s[j]) + getWidthOffsetForChar(s[j - 1])) * FontPixelZoom;
			}
			num = (int)(0f * FontPixelZoom);
			if (positionOfNextSpace(s, j, (int)vector.X, num) >= width)
			{
				vector.Y += 18f * FontPixelZoom;
				num = 0;
				vector.X = 0f;
			}
			if (vector.Y >= (float)height - 16f * FontPixelZoom * 2f)
			{
				return s.Substring(getLastSpace(s, j));
			}
		}
		return "";
	}

	public static int getIndexOfSubstringBeyondHeight(string s, int width, int height)
	{
		Vector2 vector = default(Vector2);
		int num = 0;
		s = s.Replace(Environment.NewLine, "");
		setUpCharacterMap();
		if (!LocalizedContentManager.CurrentLanguageLatin)
		{
			for (int i = 0; i < s.Length; i++)
			{
				if (s[i] == '^')
				{
					vector.Y += (float)(FontFile.Common.LineHeight + 2) * FontPixelZoom;
					vector.X = 0f;
					num = 0;
					continue;
				}
				if (characterMap.TryGetValue(s[i], out var value))
				{
					if (i > 0)
					{
						vector.X += (float)value.XAdvance * FontPixelZoom;
					}
					if (positionOfNextSpace(s, i, (int)vector.X, num) >= width)
					{
						vector.Y += (float)(FontFile.Common.LineHeight + 2) * FontPixelZoom;
						num = 0;
						vector.X = 0f;
					}
				}
				if (vector.Y >= (float)height - (float)FontFile.Common.LineHeight * FontPixelZoom * 2f)
				{
					return i - 1;
				}
			}
			return s.Length - 1;
		}
		for (int j = 0; j < s.Length; j++)
		{
			if (s[j] == '^')
			{
				vector.Y += 18f * FontPixelZoom;
				vector.X = 0f;
				num = 0;
				continue;
			}
			if (j > 0)
			{
				vector.X += 8f * FontPixelZoom + (float)num + (float)(getWidthOffsetForChar(s[j]) + getWidthOffsetForChar(s[j - 1])) * FontPixelZoom;
			}
			num = (int)(0f * FontPixelZoom);
			if (positionOfNextSpace(s, j, (int)vector.X, num) >= width)
			{
				vector.Y += 18f * FontPixelZoom;
				num = 0;
				vector.X = 0f;
			}
			if (vector.Y >= (float)height - 16f * FontPixelZoom)
			{
				return j - 1;
			}
		}
		return s.Length - 1;
	}

	public static List<string> getStringBrokenIntoSectionsOfHeight(string s, int width, int height)
	{
		List<string> list = new List<string>();
		while (s.Length > 0)
		{
			string stringPreviousToThisHeightCutoff = getStringPreviousToThisHeightCutoff(s, width, height);
			if (stringPreviousToThisHeightCutoff.Length <= 0)
			{
				break;
			}
			list.Add(stringPreviousToThisHeightCutoff);
			s = s.Substring(list.Last().Length);
		}
		return list;
	}

	public static string getStringPreviousToThisHeightCutoff(string s, int width, int height)
	{
		return s.Substring(0, getIndexOfSubstringBeyondHeight(s, width, height) + 1);
	}

	private static int getLastSpace(string s, int startIndex)
	{
		if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ja || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.th)
		{
			return startIndex;
		}
		for (int num = startIndex; num >= 0; num--)
		{
			if (s[num] == ' ')
			{
				return num;
			}
		}
		return startIndex;
	}

	public static int getWidthOffsetForChar(char c)
	{
		switch (c)
		{
		case ',':
		case '.':
			return -2;
		case '!':
		case 'j':
		case 'l':
		case '¡':
			return -1;
		case 'i':
		case 'ì':
		case 'í':
		case 'î':
		case 'ï':
		case 'ı':
			return -1;
		case '^':
			return -8;
		case '$':
			return 1;
		case 'ş':
			return -1;
		default:
			return 0;
		}
	}

	public static void drawStringWithScrollCenteredAt(SpriteBatch b, string s, int x, int y, int width, float alpha = 1f, Color? color = null, int scrollType = 0, float layerDepth = 0.88f, bool junimoText = false)
	{
		drawString(b, s, x - width / 2, y, 999999, width, 999999, alpha, layerDepth, junimoText, scrollType, "", color, ScrollTextAlignment.Center);
	}

	public static void drawSmallTextBubble(SpriteBatch b, string s, Vector2 positionOfBottomCenter, int maxWidth = -1, float layerDepth = -1f, bool drawPointerOnTop = false)
	{
		if (maxWidth != -1)
		{
			s = Game1.parseText(s, Game1.smallFont, maxWidth - 16);
		}
		s = s.Trim();
		Vector2 vector = Game1.smallFont.MeasureString(s);
		IClickableMenu.drawTextureBox(b, Game1.mouseCursors_1_6, new Rectangle(241, 503, 9, 9), (int)(positionOfBottomCenter.X - vector.X / 2f - 4f), (int)(positionOfBottomCenter.Y - vector.Y), (int)vector.X + 16, (int)vector.Y + 12, Color.White, 4f, drawShadow: false, layerDepth);
		if (drawPointerOnTop)
		{
			b.Draw(Game1.mouseCursors_1_6, positionOfBottomCenter + new Vector2(-4f, -3f) * 4f + new Vector2(vector.X / 2f, 0f - vector.Y), new Rectangle(251, 506, 5, 5), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.FlipVertically, layerDepth + 1E-05f);
		}
		else
		{
			b.Draw(Game1.mouseCursors_1_6, positionOfBottomCenter + new Vector2(-2.5f, 1f) * 4f, new Rectangle(251, 506, 5, 5), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth + 1E-05f);
		}
		Utility.drawTextWithShadow(b, s, Game1.smallFont, positionOfBottomCenter - vector + new Vector2(4f + vector.X / 2f, 8f), Game1.textColor, 1f, layerDepth + 2E-05f, -1, -1, 0.5f);
	}

	public static void drawStringWithScrollCenteredAt(SpriteBatch b, string s, int x, int y, string placeHolderWidthText = "", float alpha = 1f, Color? color = null, int scrollType = 0, float layerDepth = 0.88f, bool junimoText = false)
	{
		drawString(b, s, x - getWidthOfString((placeHolderWidthText.Length > 0) ? placeHolderWidthText : s) / 2, y, 999999, -1, 999999, alpha, layerDepth, junimoText, scrollType, placeHolderWidthText, color, ScrollTextAlignment.Center);
	}

	public static void drawStringWithScrollBackground(SpriteBatch b, string s, int x, int y, string placeHolderWidthText = "", float alpha = 1f, Color? color = null, ScrollTextAlignment scroll_text_alignment = ScrollTextAlignment.Left)
	{
		drawString(b, s, x, y, 999999, -1, 999999, alpha, 0.88f, junimoText: false, 0, placeHolderWidthText, color, scroll_text_alignment);
	}

	private static FontFile loadFont(string assetName)
	{
		return FontLoader.Parse(Game1.content.Load<XmlSource>(assetName).Source);
	}

	private static void setUpCharacterMap()
	{
		if (!LocalizedContentManager.CurrentLanguageLatin && characterMap == null)
		{
			LocalizedContentManager.OnLanguageChange += OnLanguageChange;
			LoadFontData(LocalizedContentManager.CurrentLanguageCode);
		}
	}

	public static void drawString(SpriteBatch b, string s, int x, int y, int characterPosition = 999999, int width = -1, int height = 999999, float alpha = 1f, float layerDepth = 0.88f, bool junimoText = false, int drawBGScroll = -1, string placeHolderScrollWidthText = "", Color? color = null, ScrollTextAlignment scroll_text_alignment = ScrollTextAlignment.Left)
	{
		setUpCharacterMap();
		bool hasValue = color.HasValue;
		color = color ?? color_Default;
		bool flag = width != -1;
		if (!flag)
		{
			width = Game1.graphics.GraphicsDevice.Viewport.Width - x;
			if (drawBGScroll == 1)
			{
				width = getWidthOfString(s) * 2;
			}
		}
		if (FontPixelZoom < 4f && LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.ko && LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.zh)
		{
			y += (int)((4f - FontPixelZoom) * 4f);
		}
		Vector2 vector = new Vector2(x, y);
		int num = 0;
		if (drawBGScroll != 1)
		{
			if (vector.X + (float)width > (float)(Game1.graphics.GraphicsDevice.Viewport.Width - 4))
			{
				vector.X = Game1.graphics.GraphicsDevice.Viewport.Width - width - 4;
			}
			if (vector.X < 0f)
			{
				vector.X = 0f;
			}
		}
		switch (drawBGScroll)
		{
		case 0:
		case 2:
		case 3:
		{
			int num4 = getWidthOfString((placeHolderScrollWidthText.Length > 0) ? placeHolderScrollWidthText : s);
			if (flag)
			{
				num4 = width;
			}
			switch (drawBGScroll)
			{
			case 0:
				b.Draw(Game1.mouseCursors, vector + new Vector2(-12f, -3f) * 4f, new Rectangle(325, 318, 12, 18), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth - 0.001f);
				b.Draw(Game1.mouseCursors, vector + new Vector2(0f, -3f) * 4f, new Rectangle(337, 318, 1, 18), Color.White * alpha, 0f, Vector2.Zero, new Vector2(num4, 4f), SpriteEffects.None, layerDepth - 0.001f);
				b.Draw(Game1.mouseCursors, vector + new Vector2(num4, -12f), new Rectangle(338, 318, 12, 18), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth - 0.001f);
				break;
			case 2:
				b.Draw(Game1.mouseCursors, vector + new Vector2(-3f, -3f) * 4f, new Rectangle(327, 281, 3, 17), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth - 0.001f);
				b.Draw(Game1.mouseCursors, vector + new Vector2(0f, -3f) * 4f, new Rectangle(330, 281, 1, 17), Color.White * alpha, 0f, Vector2.Zero, new Vector2(num4 + 4, 4f), SpriteEffects.None, layerDepth - 0.001f);
				b.Draw(Game1.mouseCursors, vector + new Vector2(num4 + 4, -12f), new Rectangle(333, 281, 3, 17), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth - 0.001f);
				break;
			case 3:
				b.Draw(Game1.mouseCursors_1_6, vector + new Vector2(-3f, -3f) * 4f, new Rectangle(86, 145, 3, 17), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth - 0.001f);
				b.Draw(Game1.mouseCursors_1_6, vector + new Vector2(0f, -3f) * 4f, new Rectangle(89, 145, 1, 17), Color.White * alpha, 0f, Vector2.Zero, new Vector2(num4 + 4, 4f), SpriteEffects.None, layerDepth - 0.001f);
				b.Draw(Game1.mouseCursors_1_6, vector + new Vector2(num4 + 4, -12f), new Rectangle(92, 145, 3, 17), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth - 0.001f);
				break;
			}
			switch (scroll_text_alignment)
			{
			case ScrollTextAlignment.Center:
				x += (num4 - getWidthOfString(s)) / 2;
				vector.X = x;
				break;
			case ScrollTextAlignment.Right:
				x += num4 - getWidthOfString(s);
				vector.X = x;
				break;
			}
			vector.Y += (4f - FontPixelZoom) * 4f;
			break;
		}
		case 1:
		{
			int widthOfString = getWidthOfString((placeHolderScrollWidthText.Length > 0) ? placeHolderScrollWidthText : s);
			Vector2 vector2 = vector;
			if (Game1.currentLocation?.map?.Layers[0] != null)
			{
				int num2 = -Game1.viewport.X + 28;
				int num3 = -Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64 - 28;
				if (vector.X < (float)num2)
				{
					vector.X = num2;
				}
				if (vector.X + (float)widthOfString > (float)num3)
				{
					vector.X = num3 - widthOfString;
				}
				vector2.X += widthOfString / 2;
				if (vector2.X < vector.X)
				{
					vector.X += vector2.X - vector.X;
				}
				if (vector2.X > vector.X + (float)widthOfString - 24f)
				{
					vector.X += vector2.X - (vector.X + (float)widthOfString - 24f);
				}
				vector2.X = Utility.Clamp(vector2.X, vector.X, vector.X + (float)widthOfString - 24f);
			}
			b.Draw(Game1.mouseCursors, vector + new Vector2(-7f, -3f) * 4f, new Rectangle(324, 299, 7, 17), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth - 0.001f);
			b.Draw(Game1.mouseCursors, vector + new Vector2(0f, -3f) * 4f, new Rectangle(331, 299, 1, 17), Color.White * alpha, 0f, Vector2.Zero, new Vector2(getWidthOfString((placeHolderScrollWidthText.Length > 0) ? placeHolderScrollWidthText : s), 4f), SpriteEffects.None, layerDepth - 0.001f);
			b.Draw(Game1.mouseCursors, vector + new Vector2(widthOfString, -12f), new Rectangle(332, 299, 7, 17), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth - 0.001f);
			b.Draw(Game1.mouseCursors, vector2 + new Vector2(0f, 52f), new Rectangle(341, 308, 6, 5), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth - 0.0001f);
			x = (int)vector.X;
			if (placeHolderScrollWidthText.Length > 0)
			{
				x += getWidthOfString(placeHolderScrollWidthText) / 2 - getWidthOfString(s) / 2;
				vector.X = x;
			}
			vector.Y += (4f - FontPixelZoom) * 4f;
			break;
		}
		}
		if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko)
		{
			vector.Y -= 8f;
		}
		if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh)
		{
			if (drawBGScroll != -1)
			{
				float num5 = 3.5f;
				if (Game1.options.useChineseSmoothFont)
				{
					vector.Y -= 2f;
					num5 = 3.8f;
				}
				else
				{
					vector.Y += 4f;
				}
				vector.Y -= (FontPixelZoom - 0.75f) * 4f * num5;
			}
			else
			{
				vector.Y += 4f;
			}
		}
		s = s.Replace(Environment.NewLine, "");
		if (!junimoText && (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ja || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.th || (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.mod && LocalizedContentManager.CurrentModLanguage.FontApplyYOffset)))
		{
			vector.Y -= (4f - FontPixelZoom) * 4f;
		}
		s = s.Replace('♡', '<');
		for (int i = 0; i < Math.Min(s.Length, characterPosition); i++)
		{
			if (LocalizedContentManager.CurrentLanguageLatin || (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru && !Game1.options.useAlternateFont) || IsSpecialCharacter(s[i]) || junimoText || forceEnglishFont)
			{
				float num6 = fontPixelZoom;
				if (IsSpecialCharacter(s[i]) || junimoText || forceEnglishFont)
				{
					fontPixelZoom = 3f;
				}
				if (s[i] == '^')
				{
					vector.Y += 18f * FontPixelZoom;
					vector.X = x;
					num = 0;
					fontPixelZoom = num6;
					continue;
				}
				num = (int)(0f * FontPixelZoom);
				bool flag2 = char.IsUpper(s[i]) || s[i] == 'ß';
				Vector2 vector3 = new Vector2(0f, -1 + ((!junimoText && flag2) ? (-3) : 0));
				if (s[i] == 'Ç')
				{
					vector3.Y += 2f;
				}
				if (positionOfNextSpace(s, i, (int)vector.X - x, num) >= width)
				{
					vector.Y += 18f * FontPixelZoom;
					num = 0;
					vector.X = x;
					if (s[i] == ' ')
					{
						fontPixelZoom = num6;
						continue;
					}
				}
				Rectangle sourceRectForChar = getSourceRectForChar(s[i], junimoText);
				b.Draw(hasValue ? coloredTexture : spriteTexture, vector + vector3 * FontPixelZoom, sourceRectForChar, ((IsSpecialCharacter(s[i]) || junimoText) ? Color.White : color.Value) * alpha, 0f, Vector2.Zero, FontPixelZoom, SpriteEffects.None, layerDepth);
				if (i < s.Length - 1)
				{
					vector.X += 8f * FontPixelZoom + (float)num + (float)getWidthOffsetForChar(s[i + 1]) * FontPixelZoom;
				}
				if (s[i] != '^')
				{
					vector.X += (float)getWidthOffsetForChar(s[i]) * FontPixelZoom;
				}
				fontPixelZoom = num6;
				continue;
			}
			if (s[i] == '^')
			{
				vector.Y += (float)(FontFile.Common.LineHeight + 2) * FontPixelZoom;
				vector.X = x;
				num = 0;
				continue;
			}
			if (i > 0 && IsSpecialCharacter(s[i - 1]))
			{
				vector.X += 24f;
			}
			if (characterMap.TryGetValue(s[i], out var value))
			{
				Rectangle value2 = new Rectangle(value.X, value.Y, value.Width, value.Height);
				Texture2D texture = fontPages[value.Page];
				if (positionOfNextSpace(s, i, (int)vector.X, num) >= x + width - 4)
				{
					vector.Y += (float)(FontFile.Common.LineHeight + 2) * FontPixelZoom;
					num = 0;
					vector.X = x;
				}
				Vector2 vector4 = new Vector2(vector.X + (float)value.XOffset * FontPixelZoom, vector.Y + (float)value.YOffset * FontPixelZoom);
				if (drawBGScroll != -1 && LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko)
				{
					vector4.Y -= 8f;
				}
				if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru)
				{
					Vector2 vector5 = new Vector2(-1f, 1f) * FontPixelZoom;
					b.Draw(texture, vector4 + vector5, value2, color.Value * alpha * shadowAlpha, 0f, Vector2.Zero, FontPixelZoom, SpriteEffects.None, layerDepth);
					b.Draw(texture, vector4 + new Vector2(0f, vector5.Y), value2, color.Value * alpha * shadowAlpha, 0f, Vector2.Zero, FontPixelZoom, SpriteEffects.None, layerDepth);
					b.Draw(texture, vector4 + new Vector2(vector5.X, 0f), value2, color.Value * alpha * shadowAlpha, 0f, Vector2.Zero, FontPixelZoom, SpriteEffects.None, layerDepth);
				}
				b.Draw(texture, vector4, value2, color.Value * alpha, 0f, Vector2.Zero, FontPixelZoom, SpriteEffects.None, layerDepth);
				vector.X += (float)value.XAdvance * FontPixelZoom;
			}
		}
	}

	private static bool IsSpecialCharacter(char c)
	{
		if (!c.Equals('<') && !c.Equals('=') && !c.Equals('>') && !c.Equals('@') && !c.Equals('$') && !c.Equals('`'))
		{
			return c.Equals('+');
		}
		return true;
	}

	private static void OnLanguageChange(LocalizedContentManager.LanguageCode code)
	{
		LoadFontData(code);
	}

	public static void LoadFontData(LocalizedContentManager.LanguageCode code)
	{
		if (characterMap != null)
		{
			characterMap.Clear();
		}
		else
		{
			characterMap = new Dictionary<char, FontChar>();
		}
		if (fontPages != null)
		{
			fontPages.Clear();
		}
		else
		{
			fontPages = new List<Texture2D>();
		}
		string text = "Fonts\\";
		switch (code)
		{
		case LocalizedContentManager.LanguageCode.ja:
			FontFile = loadFont(text + "Japanese");
			fontPixelZoom = 1.75f;
			break;
		case LocalizedContentManager.LanguageCode.zh:
			if (Game1.options.useChineseSmoothFont)
			{
				text += "Chinese_round\\";
				fontPixelZoom = 1f;
			}
			else
			{
				fontPixelZoom = 1.5f;
			}
			FontFile = loadFont(text + "Chinese");
			break;
		case LocalizedContentManager.LanguageCode.ru:
			FontFile = loadFont(text + "Russian");
			fontPixelZoom = 3f;
			break;
		case LocalizedContentManager.LanguageCode.th:
			FontFile = loadFont(text + "Thai");
			fontPixelZoom = 1.5f;
			break;
		case LocalizedContentManager.LanguageCode.ko:
			FontFile = loadFont(text + "Korean");
			fontPixelZoom = 1.5f;
			break;
		case LocalizedContentManager.LanguageCode.mod:
			FontFile = loadFont(LocalizedContentManager.CurrentModLanguage.FontFile);
			fontPixelZoom = LocalizedContentManager.CurrentModLanguage.FontPixelZoom;
			break;
		default:
			FontFile = null;
			fontPixelZoom = 3f;
			break;
		}
		if (FontFile == null)
		{
			return;
		}
		foreach (FontChar @char in FontFile.Chars)
		{
			char key = (char)@char.ID;
			characterMap.Add(key, @char);
		}
		foreach (FontPage page in FontFile.Pages)
		{
			fontPages.Add(Game1.content.Load<Texture2D>(text + page.File));
		}
	}

	public static int positionOfNextSpace(string s, int index, int currentXPosition, int accumulatedHorizontalSpaceBetweenCharacters)
	{
		setUpCharacterMap();
		LocalizedContentManager.LanguageCode currentLanguageCode = LocalizedContentManager.CurrentLanguageCode;
		if (currentLanguageCode == LocalizedContentManager.LanguageCode.ja || currentLanguageCode == LocalizedContentManager.LanguageCode.zh || currentLanguageCode == LocalizedContentManager.LanguageCode.th)
		{
			float num = currentXPosition;
			string value = Game1.asianSpacingRegex.Match(s, index).Value;
			foreach (char key in value)
			{
				if (characterMap.TryGetValue(key, out var value2))
				{
					num += (float)value2.XAdvance * FontPixelZoom;
				}
			}
			return (int)num;
		}
		for (int j = index; j < s.Length; j++)
		{
			if (isUsingNonSpriteSheetFont())
			{
				if (s[j] == ' ' || s[j] == '^')
				{
					return currentXPosition;
				}
				currentXPosition = ((!characterMap.TryGetValue(s[j], out var value3)) ? (currentXPosition + (int)((float)FontFile.Common.LineHeight * FontPixelZoom)) : (currentXPosition + (int)((float)value3.XAdvance * FontPixelZoom)));
				continue;
			}
			if (s[j] == ' ' || s[j] == '^')
			{
				return currentXPosition;
			}
			currentXPosition += (int)(8f * FontPixelZoom + (float)accumulatedHorizontalSpaceBetweenCharacters + (float)(getWidthOffsetForChar(s[j]) + getWidthOffsetForChar(s[Math.Max(0, j - 1)])) * FontPixelZoom);
			accumulatedHorizontalSpaceBetweenCharacters = (int)(0f * FontPixelZoom);
		}
		return currentXPosition;
	}

	private static bool isUsingNonSpriteSheetFont()
	{
		if (!LocalizedContentManager.CurrentLanguageLatin)
		{
			if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru)
			{
				return Game1.options.useAlternateFont;
			}
			return true;
		}
		return false;
	}

	private static Rectangle getSourceRectForChar(char c, bool junimoText)
	{
		int num = c - 32;
		switch (c)
		{
		case 'Œ':
			num = 96;
			break;
		case 'œ':
			num = 97;
			break;
		case 'Ğ':
			num = 102;
			break;
		case 'ğ':
			num = 103;
			break;
		case 'İ':
			num = 98;
			break;
		case 'ı':
			num = 99;
			break;
		case 'Ş':
			num = 100;
			break;
		case 'ş':
			num = 101;
			break;
		case '’':
			num = 104;
			break;
		case 'Ő':
			num = 105;
			break;
		case 'ő':
			num = 106;
			break;
		case 'Ű':
			num = 107;
			break;
		case 'ű':
			num = 108;
			break;
		case 'ё':
			num = 560;
			break;
		case 'ґ':
			num = 561;
			break;
		case 'є':
			num = 562;
			break;
		case 'і':
			num = 563;
			break;
		case 'ї':
			num = 564;
			break;
		case 'ў':
			num = 565;
			break;
		case 'Ё':
			num = 512;
			break;
		case '–':
			num = 464;
			break;
		case '—':
			num = 465;
			break;
		case '№':
			num = 466;
			break;
		case 'Ґ':
			num = 513;
			break;
		case 'Є':
			num = 514;
			break;
		case 'І':
			num = 515;
			break;
		case 'Ї':
			num = 516;
			break;
		case 'Ў':
			num = 517;
			break;
		case 'Ą':
			num = 576;
			break;
		case 'ą':
			num = 578;
			break;
		case 'Ć':
			num = 579;
			break;
		case 'ć':
			num = 580;
			break;
		case 'Ę':
			num = 581;
			break;
		case 'ę':
			num = 582;
			break;
		case 'Ł':
			num = 583;
			break;
		case 'ł':
			num = 584;
			break;
		case 'Ń':
			num = 585;
			break;
		case 'ń':
			num = 586;
			break;
		case 'Ź':
			num = 587;
			break;
		case 'ź':
			num = 588;
			break;
		case 'Ż':
			num = 589;
			break;
		case 'ż':
			num = 590;
			break;
		case 'Ś':
			num = 574;
			break;
		case 'ś':
			num = 575;
			break;
		default:
			if (num >= 1008 && num < 1040)
			{
				num -= 528;
			}
			else if (num >= 1040 && num < 1072)
			{
				num -= 512;
			}
			break;
		}
		return new Rectangle(num * 8 % spriteTexture.Width, num * 8 / spriteTexture.Width * 16 + (junimoText ? 224 : 0), 8, 16);
	}
}
