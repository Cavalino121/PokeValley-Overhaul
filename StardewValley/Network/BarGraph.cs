using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Network;

public class BarGraph
{
	public static double DYNAMIC_SCALE_MAX = -1.0;

	public static double DYNAMIC_SCALE_AVG = -2.0;

	private Queue<double> elements;

	private int height;

	private int width;

	private int x;

	private int y;

	private double maxValue;

	private Color barColor;

	private int elementWidth;

	private Texture2D whiteTexture;

	public BarGraph(Queue<double> elements, int x, int y, int width, int height, int elementWidth, double maxValue, Color barColor, Texture2D whiteTexture)
	{
		this.elements = elements;
		this.width = width;
		this.height = height;
		this.x = x;
		this.y = y;
		this.maxValue = maxValue;
		this.barColor = barColor;
		this.elementWidth = elementWidth;
		this.whiteTexture = whiteTexture;
	}

	public void Draw(SpriteBatch sb)
	{
		double num = maxValue;
		if (num == DYNAMIC_SCALE_MAX)
		{
			foreach (double element in elements)
			{
				num = Math.Max(element, num);
			}
		}
		else if (num == DYNAMIC_SCALE_AVG)
		{
			double num2 = 0.0;
			foreach (double element2 in elements)
			{
				num2 += element2;
			}
			num = num2 / (double)Math.Max(1, elements.Count);
		}
		sb.Draw(whiteTexture, new Rectangle(x - 1, y, width, height), null, Color.Black * 0.5f);
		int num3 = x + width - elementWidth * elements.Count;
		int num4 = 0;
		foreach (double element3 in elements)
		{
			int num5 = num3 + num4 * elementWidth;
			int num6 = y;
			int num7 = (int)((double)(float)element3 / num * (double)height);
			sb.Draw(whiteTexture, new Rectangle(num5, num6 + height - num7, elementWidth, num7), null, barColor);
			num4++;
		}
	}
}
