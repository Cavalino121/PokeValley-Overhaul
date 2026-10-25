using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Pokemon;

/// <summary>Shared drawing helpers so every Pokémon UI uses the same fitted fonts and GBA-style bars.</summary>
public static class PokemonUI
{
	/// <summary>
	/// Folder that holds the optional community sprite sets (pokeemerald/, pokefirered/,
	/// pokeplatinum/, Content/Pokemon/). They ship beside the game executable.
	/// </summary>
	public static string AssetRoot { get; } = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

	/// <summary>Draws text that is always scaled down to fit inside <paramref name="area"/> (never leaks).</summary>
	public static void Text(SpriteBatch b, SpriteFont font, string text, Rectangle area, Color color, bool center = false, float maxScale = 1f, bool shadow = true)
	{
		if (string.IsNullOrEmpty(text) || area.Width <= 0 || area.Height <= 0) return;
		Vector2 size = font.MeasureString(text);
		if (size.X <= 0f || size.Y <= 0f) return;
		float scale = Math.Min(maxScale, Math.Min(area.Width / size.X, area.Height / size.Y));
		float w = size.X * scale;
		float h = size.Y * scale;
		float x = center ? area.X + (area.Width - w) / 2f : area.X;
		float y = area.Y + (area.Height - h) / 2f;
		Vector2 pos = new Vector2((int)x, (int)y);
		if (shadow)
		{
			b.DrawString(font, text, pos + new Vector2(1f, 1f), Game1.textShadowColor * 0.55f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.99f);
		}
		b.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
	}

	public static void Text(SpriteBatch b, SpriteFont font, string text, int x, int y, int maxWidth, int maxHeight, Color color, bool center = false, float maxScale = 1f)
	{
		Text(b, font, text, new Rectangle(x, y, maxWidth, maxHeight), color, center, maxScale);
	}

	public static void Border(SpriteBatch b, Rectangle r, int t, Color c)
	{
		b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y, r.Width, t), c);
		b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
		b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y, t, r.Height), c);
		b.Draw(Game1.staminaRect, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
	}

	/// <summary>Classic GBA two-tone HP bar (green / yellow / red) with dark frame.</summary>
	public static void HpBar(SpriteBatch b, Rectangle r, float ratio)
	{
		ratio = Math.Clamp(ratio, 0f, 1f);
		b.Draw(Game1.staminaRect, r, new Color(40, 40, 48));
		Rectangle inner = new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4);
		b.Draw(Game1.staminaRect, inner, new Color(96, 104, 104));
		int fill = (int)Math.Round(inner.Width * ratio);
		if (ratio > 0f && fill < 1) fill = 1;
		if (fill > 0)
		{
			Color light, dark;
			if (ratio > 0.5f) { light = new Color(112, 248, 168); dark = new Color(88, 208, 128); }
			else if (ratio > 0.2f) { light = new Color(248, 224, 56); dark = new Color(200, 168, 13); }
			else { light = new Color(248, 88, 56); dark = new Color(168, 64, 72); }
			int half = Math.Max(1, inner.Height / 2);
			b.Draw(Game1.staminaRect, new Rectangle(inner.X, inner.Y, fill, half), light);
			b.Draw(Game1.staminaRect, new Rectangle(inner.X, inner.Y + half, fill, inner.Height - half), dark);
		}
	}

	public static void ExpBar(SpriteBatch b, Rectangle r, float ratio)
	{
		ratio = Math.Clamp(ratio, 0f, 1f);
		b.Draw(Game1.staminaRect, r, new Color(40, 40, 48));
		Rectangle inner = new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
		b.Draw(Game1.staminaRect, inner, new Color(96, 104, 104));
		int fill = (int)Math.Round(inner.Width * ratio);
		if (fill > 0)
		{
			b.Draw(Game1.staminaRect, new Rectangle(inner.X, inner.Y, fill, inner.Height), new Color(64, 200, 248));
		}
	}

	/// <summary>Small "HP" tag drawn left of an HP bar.</summary>
	public static void HpTag(SpriteBatch b, Rectangle r)
	{
		Text(b, Game1.tinyFont, "HP", r, new Color(220, 120, 20), center: true, maxScale: 0.8f);
	}

	/// <summary>Soft oval ground shadow made from stacked rectangles (no hard rectangular edge).</summary>
	public static void SoftShadow(SpriteBatch b, int centerX, int centerY, int width, int height, float alpha = 0.32f, float layerDepth = 0.0001f)
	{
		int rows = Math.Max(3, height / 2);
		for (int i = 0; i < rows; i++)
		{
			float t = (i + 0.5f) / rows * 2f - 1f;
			float w = (float)Math.Sqrt(Math.Max(0f, 1f - t * t));
			int rw = (int)(width * w);
			int y = centerY - height / 2 + (int)(i * (height / (float)rows));
			b.Draw(Game1.staminaRect, new Rectangle(centerX - rw / 2, y, rw, Math.Max(1, height / rows + 1)), null, Color.Black * (alpha / 2f), 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
		}
	}

	public static void SoftShadow(SpriteBatch b, Rectangle r, float alpha = 0.32f, float layerDepth = 0.0001f)
	{
		SoftShadow(b, r.Center.X, r.Center.Y, r.Width, r.Height, alpha, layerDepth);
	}

	public static void DrawRoundedRect(SpriteBatch b, Rectangle r, Color c, int radius = 4)
	{
		if (r.Width <= 0 || r.Height <= 0) return;
		int rad = Math.Clamp(radius, 0, Math.Min(r.Width, r.Height) / 2);
		if (rad <= 1)
		{
			b.Draw(Game1.staminaRect, r, c);
			return;
		}
		b.Draw(Game1.staminaRect, new Rectangle(r.X + rad, r.Y, r.Width - rad * 2, r.Height), c);
		b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y + rad, rad, r.Height - rad * 2), c);
		b.Draw(Game1.staminaRect, new Rectangle(r.Right - rad, r.Y + rad, rad, r.Height - rad * 2), c);
		for (int i = 1; i < rad; i++)
		{
			int inset = rad - i;
			b.Draw(Game1.staminaRect, new Rectangle(r.X + inset, r.Y + i, rad - inset, 1), c);
			b.Draw(Game1.staminaRect, new Rectangle(r.Right - rad, r.Y + i, rad - inset, 1), c);
			b.Draw(Game1.staminaRect, new Rectangle(r.X + inset, r.Bottom - 1 - i, rad - inset, 1), c);
			b.Draw(Game1.staminaRect, new Rectangle(r.Right - rad, r.Bottom - 1 - i, rad - inset, 1), c);
		}
	}

	public static void DrawCardBorder(SpriteBatch b, Rectangle r, Color c, int t = 1)
	{
		Border(b, r, Math.Max(1, t), c);
	}

	public static void DrawGbaFrame(SpriteBatch b, Rectangle r, Color innerBg, Color borderCol, Color outerShadowCol)
	{
		b.Draw(Game1.staminaRect, new Rectangle(r.X + 4, r.Y + 4, r.Width, r.Height), outerShadowCol * 0.7f);
		DrawRoundedRect(b, r, borderCol, 8);
		Rectangle inner = new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, r.Height - 8);
		DrawRoundedRect(b, inner, innerBg, 6);
		Border(b, inner, 1, Color.White * 0.2f);
	}

	public static void DrawText(SpriteBatch b, SpriteFont font, string text, Vector2 pos, Color color, float scale = 1f, bool shadow = true)
	{
		if (string.IsNullOrEmpty(text)) return;
		if (shadow)
		{
			b.DrawString(font, text, pos + new Vector2(1f, 1f), Color.Black * 0.65f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.99f);
		}
		b.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
	}

	public static void DrawTextCentered(SpriteBatch b, SpriteFont font, string text, Rectangle area, Color color, float scale = 1f, bool shadow = true)
	{
		Text(b, font, text, area, color, center: true, maxScale: scale, shadow: shadow);
	}

	public static void DrawTinyText(SpriteBatch b, string text, Vector2 pos, Color color, float scale = 0.8f, bool shadow = true)
	{
		DrawText(b, Game1.smallFont, text, pos, color, scale, shadow);
	}

	public static void DrawTypePill(SpriteBatch b, PokemonType type, int x, int y, int width = 76, int height = 22)
	{
		Color bg = PokemonTypeColors.Get(type);
		Rectangle r = new Rectangle(x, y, width, height);
		DrawRoundedRect(b, r, bg, 4);
		Border(b, r, 1, Color.White * 0.65f);
		Text(b, Game1.smallFont, type.ToString().ToUpperInvariant(), new Rectangle(x + 3, y + 1, width - 6, height - 2), Color.White, center: true, maxScale: 0.62f, shadow: true);
	}

	public static void DrawHpBar(SpriteBatch b, Rectangle r, float ratio, bool showBorder = true)
	{
		HpBar(b, r, ratio);
	}

	public static void DrawExpBar(SpriteBatch b, Rectangle r, float ratio)
	{
		ExpBar(b, r, ratio);
	}

	public static void WrappedText(SpriteBatch b, SpriteFont font, string text, Rectangle area, Color color, float scale = 0.72f)
	{
		if (string.IsNullOrEmpty(text) || area.Width <= 0 || area.Height <= 0) return;
		string[] words = text.Split(' ');
		var lines = new List<string>();
		string current = "";
		float maxLineWidth = area.Width / Math.Max(0.1f, scale);
		foreach (string w in words)
		{
			string test = string.IsNullOrEmpty(current) ? w : current + " " + w;
			if (font.MeasureString(test).X <= maxLineWidth)
			{
				current = test;
			}
			else
			{
				if (!string.IsNullOrEmpty(current)) lines.Add(current);
				current = w;
			}
		}
		if (!string.IsNullOrEmpty(current)) lines.Add(current);

		float lineH = font.MeasureString("Ay").Y * scale * 0.92f;
		float cy = area.Y;
		foreach (string ln in lines)
		{
			if (cy + lineH > area.Bottom + 4) break;
			b.DrawString(font, ln, new Vector2(area.X, cy), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
			cy += lineH;
		}
	}

	private static readonly Dictionary<string, Texture2D> _keyed = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Loads a PNG and turns the colour of the top-left pixel transparent (GBA palette index 0).</summary>
	public static Texture2D LoadColorKeyed(string path)
	{
		if (string.IsNullOrEmpty(path)) return null;
		if (_keyed.TryGetValue(path, out var cached) && cached != null && !cached.IsDisposed) return cached;
		try
		{
			if (!File.Exists(path)) return null;
			using Stream s = File.OpenRead(path);
			Texture2D rawTex = Texture2D.FromStream(Game1.graphics.GraphicsDevice, s);
			int w = rawTex.Width;
			int h = rawTex.Height;
			Color[] data = new Color[w * h];
			rawTex.GetData(data);
			Color key = data[0];

			bool IsBgColor(Color c)
			{
				if (c.A == 0) return true;
				if (c.R == key.R && c.G == key.G && c.B == key.B) return true;
				if (c.R == 180 && c.G == 180 && c.B == 180) return true;
				if (Math.Abs(c.R - key.R) <= 8 && Math.Abs(c.G - key.G) <= 8 && Math.Abs(c.B - key.B) <= 8) return true;
				return false;
			}

			if (key.A > 0 || (key.R == 180 && key.G == 180 && key.B == 180))
			{
				bool[] visited = new bool[w * h];
				Queue<int> q = new Queue<int>();

				void TryEnqueue(int x, int y)
				{
					if (x < 0 || x >= w || y < 0 || y >= h) return;
					int idx = y * w + x;
					if (visited[idx]) return;
					Color c = data[idx];
					if (IsBgColor(c))
					{
						visited[idx] = true;
						q.Enqueue(idx);
					}
				}

				for (int x = 0; x < w; x++)
				{
					TryEnqueue(x, 0);
					TryEnqueue(x, h - 1);
				}
				for (int y = 0; y < h; y++)
				{
					TryEnqueue(0, y);
					TryEnqueue(w - 1, y);
				}

				while (q.Count > 0)
				{
					int curr = q.Dequeue();
					data[curr] = Color.Transparent;
					int cx = curr % w;
					int cy = curr / w;

					TryEnqueue(cx + 1, cy);
					TryEnqueue(cx - 1, cy);
					TryEnqueue(cx, cy + 1);
					TryEnqueue(cx, cy - 1);
				}
			}

			Texture2D tex = new Texture2D(Game1.graphics.GraphicsDevice, w, h);
			tex.SetData(data);
			rawTex.Dispose();
			_keyed[path] = tex;
			return tex;
		}
		catch
		{
			return null;
		}
	}

	public static string FindInProject(params string[] relativeParts)
	{
		string[] roots =
		{
			AppDomain.CurrentDomain.BaseDirectory,
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".."),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."),
			@"D:\Projetos\Nova pasta"
		};
		foreach (string root in roots)
		{
			try
			{
				string full = Path.Combine(Path.GetFullPath(root), Path.Combine(relativeParts));
				if (File.Exists(full)) return full;
			}
			catch { }
		}
		return null;
	}

	// ---- Trainer skin (player battle sprite) ----
	public static readonly string[] TrainerSkins =
	{
		"Brendan",
		"May",
		"Brendan (RS)",
		"May (RS)",
		"Red",
		"Ash",
		"Leaf",
		"Ethan",
		"Lyra",
		"Lucas",
		"Dawn",
		"Lucas (DP)",
		"Dawn (DP)",
		"Hilbert",
		"Hilda",
		"Nate",
		"Rosa",
		"Calem",
		"Serena",
		"Elio",
		"Selene",
		"Victor",
		"Gloria",
		"Florian",
		"Juliana",
		"Steven",
		"Wally",
		"Barry",
		"Riley",
		"Cheryl",
		"Marley",
		"Buck",
		"Mira",
		"PokeDude",
		"Old Man"
	};

	private static string _trainerSkin;

	private static string SettingsPath()
	{
		try
		{
			string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");
			if (Directory.Exists(appData)) return Path.Combine(appData, "pokemon_trainer_skin.txt");
		}
		catch { }
		return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokemon_trainer_skin.txt");
	}

	public static string TrainerSkin
	{
		get
		{
			if (Game1.player?.modData != null && Game1.player.modData.TryGetValue("PokeValley.TrainerSkin", out var val) && !string.IsNullOrWhiteSpace(val))
			{
				return val;
			}
			if (_trainerSkin == null)
			{
				try
				{
					string p = SettingsPath();
					_trainerSkin = File.Exists(p) ? File.ReadAllText(p).Trim() : null;
				}
				catch { }
				if (string.IsNullOrEmpty(_trainerSkin)) _trainerSkin = (Game1.player != null && !Game1.player.IsMale) ? "May" : "Brendan";
			}
			return _trainerSkin;
		}
		set
		{
			_trainerSkin = value;
			if (Game1.player?.modData != null)
			{
				Game1.player.modData["PokeValley.TrainerSkin"] = value;
			}
			_trainerBackCache.Clear();
			_trainerFrontCache.Clear();
			try { File.WriteAllText(SettingsPath(), value); } catch { }
		}
	}

	public static void CycleTrainerSkin(int direction = 1)
	{
		int curIdx = Array.IndexOf(TrainerSkins, TrainerSkin);
		if (curIdx < 0) curIdx = 0;
		int nextIdx = (curIdx + direction + TrainerSkins.Length) % TrainerSkins.Length;
		TrainerSkin = TrainerSkins[nextIdx];
	}

	public static string TrainerBackFile()
	{
		return TrainerSkin switch
		{
			"Brendan"      => "brendan.png",
			"May"          => "may.png",
			"Brendan (RS)" => "brendan_rs.png",
			"May (RS)"     => "may_rs.png",
			"Leaf"         => "leaf.png",
			"Steven"       => "steven.png",
			"Wally"        => "wally.png",
			"PokeDude"     => "pokedude_back_pic.png",
			"Old Man"      => "old_man_back_pic.png",
			_              => "red.png"
		};
	}

	public static string ResolveTrainerBackPath(string skinName = null)
	{
		string skin = skinName ?? TrainerSkin;
		string[] relParts = skin switch
		{
			"Lucas"        => new[] { "pokeplatinum", "res", "trainers", "classes", "player_male", "back.png" },
			"Dawn"         => new[] { "pokeplatinum", "res", "trainers", "classes", "player_female", "back.png" },
			"Lucas (DP)"   => new[] { "pokeplatinum", "res", "trainers", "classes", "dp_player_male", "back.png" },
			"Dawn (DP)"    => new[] { "pokeplatinum", "res", "trainers", "classes", "dp_player_female", "back.png" },
			"Barry"        => new[] { "pokeplatinum", "res", "trainers", "classes", "rival", "back.png" },
			"Riley"        => new[] { "pokeplatinum", "res", "trainers", "classes", "trainer_riley", "back.png" },
			"Cheryl"       => new[] { "pokeplatinum", "res", "trainers", "classes", "trainer_cheryl", "back.png" },
			"Marley"       => new[] { "pokeplatinum", "res", "trainers", "classes", "trainer_marley", "back.png" },
			"Buck"         => new[] { "pokeplatinum", "res", "trainers", "classes", "trainer_buck", "back.png" },
			"Mira"         => new[] { "pokeplatinum", "res", "trainers", "classes", "trainer_mira", "back.png" },
			"PokeDude"     => new[] { "pokefirered", "graphics", "trainers", "back_pics", "pokedude_back_pic.png" },
			"Old Man"      => new[] { "pokefirered", "graphics", "trainers", "back_pics", "old_man_back_pic.png" },
			"Brendan (RS)" or "Brendan RS" or "brendan_rs" => new[] { "pokeemerald", "graphics", "trainers", "back_pics", "brendan_rs.png" },
			"May (RS)" or "May RS" or "may_rs"             => new[] { "pokeemerald", "graphics", "trainers", "back_pics", "may_rs.png" },
			"Brendan"      => new[] { "pokeemerald", "graphics", "trainers", "back_pics", "brendan.png" },
			"May"          => new[] { "pokeemerald", "graphics", "trainers", "back_pics", "may.png" },
			"Leaf"         => new[] { "pokeemerald", "graphics", "trainers", "back_pics", "leaf.png" },
			"Steven"       => new[] { "pokeemerald", "graphics", "trainers", "back_pics", "steven.png" },
			"Wally"        => new[] { "pokeemerald", "graphics", "trainers", "back_pics", "wally.png" },
			_              => new[] { "pokeemerald", "graphics", "trainers", "back_pics", "red.png" }
		};
		return FindInProject(relParts) ?? FindInProject("pokeemerald", "graphics", "trainers", "back_pics", "brendan.png");
	}

	public static string ResolveTrainerFrontPath(string skinName = null)
	{
		string skin = skinName ?? TrainerSkin;
		string[] relParts = skin switch
		{
			"Lucas"        => new[] { "pokeplatinum", "res", "trainers", "classes", "player_male", "front.png" },
			"Dawn"         => new[] { "pokeplatinum", "res", "trainers", "classes", "player_female", "front.png" },
			"Lucas (DP)"   => new[] { "pokeplatinum", "res", "trainers", "classes", "dp_player_male", "front.png" },
			"Dawn (DP)"    => new[] { "pokeplatinum", "res", "trainers", "classes", "dp_player_female", "front.png" },
			"Barry"        => new[] { "pokeplatinum", "res", "trainers", "classes", "rival", "front.png" },
			"Riley"        => new[] { "pokeplatinum", "res", "trainers", "classes", "trainer_riley", "front.png" },
			"Cheryl"       => new[] { "pokeplatinum", "res", "trainers", "classes", "trainer_cheryl", "front.png" },
			"Marley"       => new[] { "pokeplatinum", "res", "trainers", "classes", "trainer_marley", "front.png" },
			"Buck"         => new[] { "pokeplatinum", "res", "trainers", "classes", "trainer_buck", "front.png" },
			"Mira"         => new[] { "pokeplatinum", "res", "trainers", "classes", "trainer_mira", "front.png" },
			"PokeDude"     => new[] { "pokefirered", "graphics", "trainers", "front_pics", "pokedude_front_pic.png" },
			"Old Man"      => new[] { "pokefirered", "graphics", "trainers", "front_pics", "old_man_front_pic.png" },
			"Brendan (RS)" or "Brendan RS" or "brendan_rs" => new[] { "pokeemerald", "graphics", "trainers", "front_pics", "brendan_rs.png" },
			"May (RS)" or "May RS" or "may_rs"             => new[] { "pokeemerald", "graphics", "trainers", "front_pics", "may_rs.png" },
			"Brendan"      => new[] { "pokeemerald", "graphics", "trainers", "front_pics", "brendan.png" },
			"May"          => new[] { "pokeemerald", "graphics", "trainers", "front_pics", "may.png" },
			"Leaf"         => new[] { "pokeemerald", "graphics", "trainers", "front_pics", "leaf.png" },
			"Steven"       => new[] { "pokeemerald", "graphics", "trainers", "front_pics", "steven.png" },
			"Wally"        => new[] { "pokeemerald", "graphics", "trainers", "front_pics", "wally.png" },
			_              => new[] { "pokeemerald", "graphics", "trainers", "front_pics", "red.png" }
		};
		return FindInProject(relParts) ?? FindInProject("pokeemerald", "graphics", "trainers", "front_pics", "brendan.png");
	}

	public static bool IsKnownTrainerSkin(string name)
	{
		if (string.IsNullOrWhiteSpace(name)) return false;
		return name switch
		{
			"Lucas" or "Dawn" or "Lucas (DP)" or "Dawn (DP)" or "Barry" or "Riley" or "Cheryl" or "Marley" or "Buck" or "Mira" or
			"PokeDude" or "Old Man" or "Brendan (RS)" or "Brendan RS" or "brendan_rs" or "May (RS)" or "May RS" or "may_rs" or
			"Brendan" or "May" or "Leaf" or "Steven" or "Wally" or "Red" => true,
			_ => false
		};
	}

	private static readonly Dictionary<string, Texture2D> _trainerBackCache = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, Texture2D> _trainerFrontCache = new(StringComparer.OrdinalIgnoreCase);

	public static Texture2D GetTrainerBackTexture(string skinName = null)
	{
		string key = skinName ?? TrainerSkin;
		if (_trainerBackCache.TryGetValue(key, out var cached) && cached != null && !cached.IsDisposed)
		{
			return cached;
		}

		string path = ResolveTrainerBackPath(skinName ?? TrainerSkin);
		Texture2D tex = LoadColorKeyed(path);
		if (tex != null)
		{
			_trainerBackCache[key] = tex;
		}
		return tex;
	}

	public static Texture2D GetTrainerFrontTexture(string skinName = null)
	{
		string key = skinName ?? TrainerSkin;
		if (_trainerFrontCache.TryGetValue(key, out var cached) && cached != null && !cached.IsDisposed)
		{
			return cached;
		}

		string path = ResolveTrainerFrontPath(skinName ?? TrainerSkin);
		Texture2D tex = LoadColorKeyed(path);
		if (tex != null)
		{
			_trainerFrontCache[key] = tex;
		}
		return tex;
	}
}
