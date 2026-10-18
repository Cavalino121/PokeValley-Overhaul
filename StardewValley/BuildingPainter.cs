using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Extensions;

namespace StardewValley;

[XmlInclude(typeof(BuildingPaintColor))]
public class BuildingPainter
{
	[XmlIgnore]
	public static Dictionary<string, List<List<int>>> paintMaskLookup = new Dictionary<string, List<List<int>>>(StringComparer.OrdinalIgnoreCase);

	public static Texture2D Apply(Texture2D base_texture, string mask_path, BuildingPaintColor color)
	{
		if (!paintMaskLookup.TryGetValue(mask_path, out var value))
		{
			try
			{
				Texture2D texture2D = Game1.content.Load<Texture2D>(mask_path);
				Color[] array = new Color[texture2D.GetElementCount()];
				texture2D.GetData(array);
				value = new List<List<int>>();
				for (int i = 0; i < 3; i++)
				{
					value.Add(new List<int>());
				}
				for (int j = 0; j < array.Length; j++)
				{
					if (array[j] == Color.Red)
					{
						value[0].Add(j);
					}
					else if (array[j] == Color.Lime)
					{
						value[1].Add(j);
					}
					else if (array[j] == Color.Blue)
					{
						value[2].Add(j);
					}
				}
				paintMaskLookup[mask_path] = value;
			}
			catch (Exception)
			{
				paintMaskLookup[mask_path] = null;
			}
		}
		if (value == null)
		{
			return null;
		}
		if (!color.RequiresRecolor())
		{
			return null;
		}
		Color[] array2 = new Color[base_texture.GetElementCount()];
		base_texture.GetData(array2);
		Texture2D texture2D2 = new Texture2D(Game1.graphics.GraphicsDevice, base_texture.GetActualWidth(), base_texture.GetActualHeight())
		{
			Name = "@BuildingPainter.paintedTexture"
		};
		if (base_texture.Width != base_texture.GetActualWidth() || base_texture.Height != base_texture.GetActualHeight())
		{
			texture2D2.SetContentSize(base_texture.Width, base_texture.Height);
		}
		if (!color.Color1Default.Value)
		{
			_ApplyPaint(0, -100, 0, array2, value[0]);
			_ApplyPaint(color.Color1Hue.Value, color.Color1Saturation.Value, color.Color1Lightness.Value, array2, value[0]);
		}
		if (!color.Color2Default.Value)
		{
			_ApplyPaint(0, -100, 0, array2, value[1]);
			_ApplyPaint(color.Color2Hue.Value, color.Color2Saturation.Value, color.Color2Lightness.Value, array2, value[1]);
		}
		if (!color.Color3Default.Value)
		{
			_ApplyPaint(0, -100, 0, array2, value[2]);
			_ApplyPaint(color.Color3Hue.Value, color.Color3Saturation.Value, color.Color3Lightness.Value, array2, value[2]);
		}
		texture2D2.SetData(array2);
		return texture2D2;
	}

	protected static void _ApplyPaint(int h_shift, int s_shift, int l_shift, Color[] pixels, List<int> indices)
	{
		foreach (int index in indices)
		{
			if (index < pixels.Length)
			{
				Color color = pixels[index];
				Utility.RGBtoHSL(color.R, color.G, color.B, out var h, out var s, out var l);
				h += (double)h_shift;
				s += (double)s_shift / 100.0;
				l += (double)l_shift / 100.0;
				while (h > 360.0)
				{
					h -= 360.0;
				}
				for (; h < 0.0; h += 360.0)
				{
				}
				if (s < 0.0)
				{
					s = 0.0;
				}
				if (s > 1.0)
				{
					s = 1.0;
				}
				if (l < 0.0)
				{
					l = 0.0;
				}
				if (l > 1.0)
				{
					l = 1.0;
				}
				Utility.HSLtoRGB(h, s, l, out var r, out var g, out var b);
				color.R = (byte)r;
				color.G = (byte)g;
				color.B = (byte)b;
				pixels[index] = color;
			}
		}
	}
}
