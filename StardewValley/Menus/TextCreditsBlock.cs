using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;

namespace StardewValley.Menus;

internal class TextCreditsBlock : ICreditsBlock
{
	private string text;

	private Color color;

	private bool renderNameInEnglish;

	public TextCreditsBlock(string rawtext)
	{
		string[] array = rawtext.Split(']');
		if (array.Length > 1)
		{
			text = array[1];
			color = SpriteText.getColorFromIndex(Convert.ToInt32(array[0].Substring(1)));
		}
		else
		{
			text = array[0];
			color = SpriteText.color_White;
		}
		if (SpriteText.IsMissingCharacters(rawtext))
		{
			renderNameInEnglish = true;
		}
	}

	public override void draw(int topLeftX, int topLeftY, int widthToOccupy, SpriteBatch b)
	{
		if (renderNameInEnglish)
		{
			int num = text.IndexOf('(');
			if (num != -1 && num > 0)
			{
				string s = text.Substring(0, num);
				string s2 = text.Substring(num);
				SpriteText.forceEnglishFont = true;
				int num2 = (int)((float)SpriteText.getWidthOfString(s) / SpriteText.FontPixelZoom * 3f);
				SpriteText.drawString(b, s, topLeftX, topLeftY, 999999, widthToOccupy, 99999, 1f, 0.88f, junimoText: false, -1, "", color);
				SpriteText.forceEnglishFont = false;
				SpriteText.drawString(b, s2, topLeftX + num2, topLeftY, 999999, -1, 99999, 1f, 0.88f, junimoText: false, -1, "", color);
			}
			else
			{
				SpriteText.forceEnglishFont = true;
				SpriteText.drawString(b, text, topLeftX, topLeftY, 999999, widthToOccupy, 99999, 1f, 0.88f, junimoText: false, -1, "", color);
				SpriteText.forceEnglishFont = false;
			}
		}
		else
		{
			SpriteText.drawString(b, text, topLeftX, topLeftY, 999999, widthToOccupy, 99999, 1f, 0.88f, junimoText: false, -1, "", color);
		}
	}

	public override int getHeight(int maxWidth)
	{
		if (!(text == ""))
		{
			return SpriteText.getHeightOfString(text, maxWidth);
		}
		return 64;
	}
}
