using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

/// <summary>
/// Dedicated Trainer Skin Selection Menu with gender filters (Todos, Masculino, Feminino),
/// interactive list, live battle sprite preview, and confirmation.
/// </summary>
public class PokemonTrainerSelectMenu : IClickableMenu
{
	public enum GenderFilter
	{
		Todos,
		Masculino,
		Feminino
	}

	private readonly IClickableMenu _parentMenu;
	private readonly Action<string> _onSelectCallback;
	private readonly string _customTitle;
	private GenderFilter _filter = GenderFilter.Todos;
	private string _selectedSkin;
	private readonly List<string> _filteredSkins = new();
	private readonly List<ClickableComponent> _skinButtons = new();
	private readonly List<ClickableComponent> _filterButtons = new();
	private ClickableComponent _confirmBtn;
	private ClickableComponent _cancelBtn;
	private ClickableComponent _prevPageBtn;
	private ClickableComponent _nextPageBtn;
	private int _currentPage = 0;
	private const int SkinsPerPage = 10;

	private Texture2D _previewTex;
	private string _previewSkin;

	public PokemonTrainerSelectMenu(IClickableMenu parentMenu, Action<string> onSelectCallback = null, string customTitle = null)
		: base(
			(Game1.uiViewport.Width - 860) / 2,
			(Game1.uiViewport.Height - 600) / 2,
			860,
			600,
			showUpperRightCloseButton: true)
	{
		_parentMenu = parentMenu;
		_onSelectCallback = onSelectCallback;
		_customTitle = customTitle;
		_selectedSkin = PokemonUI.TrainerSkin;
		RefreshFilter();
		SetupButtons();
		Game1.playSound("bigSelect");
	}

	private void RefreshFilter()
	{
		_filteredSkins.Clear();
		foreach (string s in PokemonUI.TrainerSkins)
		{
			bool isFemale = s switch
			{
				"May" or "May (RS)" or "Leaf" or "Dawn" or "Dawn (DP)" or "Cheryl" or "Marley" or "Mira" or "Lyra" or "Hilda" or "Rosa" or "Serena" or "Selene" or "Gloria" or "Juliana" => true,
				_ => false
			};

			if (_filter == GenderFilter.Todos ||
			    (_filter == GenderFilter.Masculino && !isFemale) ||
			    (_filter == GenderFilter.Feminino && isFemale))
			{
				_filteredSkins.Add(s);
			}
		}

		if (_selectedSkin == null || !_filteredSkins.Contains(_selectedSkin))
		{
			if (_filteredSkins.Count > 0)
			{
				_selectedSkin = _filteredSkins[0];
			}
		}
	}

	private void SetupButtons()
	{
		upperRightCloseButton = new ClickableTextureComponent(
			new Rectangle(xPositionOnScreen + width - 36, yPositionOnScreen - 8, 48, 48),
			Game1.mouseCursors,
			new Rectangle(337, 494, 12, 12),
			4f);

		_filterButtons.Clear();
		string[] filters = new[] { "Todos", "Masculino", "Feminino" };
		int fStartX = xPositionOnScreen + 32;
		int fY = yPositionOnScreen + 56;
		int fW = 120;
		for (int i = 0; i < filters.Length; i++)
		{
			_filterButtons.Add(new ClickableComponent(new Rectangle(fStartX + i * (fW + 8), fY, fW, 34), filters[i], filters[i]));
		}

		_skinButtons.Clear();
		int maxPages = Math.Max(0, (_filteredSkins.Count - 1) / SkinsPerPage);
		if (_currentPage > maxPages) _currentPage = maxPages;
		if (_currentPage < 0) _currentPage = 0;

		int startIndex = _currentPage * SkinsPerPage;
		int countOnPage = Math.Min(SkinsPerPage, _filteredSkins.Count - startIndex);

		int listX = xPositionOnScreen + 32;
		int listStartY = yPositionOnScreen + 102;
		int btnW = 390;
		int btnH = 38;

		for (int i = 0; i < countOnPage; i++)
		{
			string skinName = _filteredSkins[startIndex + i];
			int by = listStartY + i * (btnH + 6);
			_skinButtons.Add(new ClickableComponent(new Rectangle(listX, by, btnW, btnH), skinName));
		}

		// Page buttons
		int pageY = yPositionOnScreen + height - 56;
		_prevPageBtn = new ClickableComponent(new Rectangle(listX, pageY, 110, 36), "prev", "◀ Anterior");
		_nextPageBtn = new ClickableComponent(new Rectangle(listX + btnW - 110, pageY, 110, 36), "next", "Próximo ▶");

		// Right action buttons
		int rightX = xPositionOnScreen + 450;
		_confirmBtn = new ClickableComponent(new Rectangle(rightX, yPositionOnScreen + height - 60, 180, 42), "confirm", "✅ Confirmar");
		_cancelBtn = new ClickableComponent(new Rectangle(rightX + 194, yPositionOnScreen + height - 60, 170, 42), "cancel", "❌ Cancelar");
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (upperRightCloseButton != null && upperRightCloseButton.containsPoint(x, y))
		{
			CloseMenu();
			return;
		}

		if (_cancelBtn != null && _cancelBtn.containsPoint(x, y))
		{
			CloseMenu();
			return;
		}

		if (_confirmBtn != null && _confirmBtn.containsPoint(x, y))
		{
			if (!string.IsNullOrEmpty(_selectedSkin))
			{
				if (_onSelectCallback != null)
				{
					Game1.playSound("reward");
					_onSelectCallback(_selectedSkin);
					return;
				}
				PokemonUI.TrainerSkin = _selectedSkin;
				Game1.playSound("reward");
				Game1.addHUDMessage(new HUDMessage($"Treinador de batalha definido como: {_selectedSkin}!", 2));
			}
			CloseMenu();
			return;
		}

		foreach (var fb in _filterButtons)
		{
			if (fb.containsPoint(x, y))
			{
				_filter = Enum.Parse<GenderFilter>(fb.name);
				_currentPage = 0;
				Game1.playSound("smallSelect");
				RefreshFilter();
				SetupButtons();
				return;
			}
		}

		if (_prevPageBtn != null && _prevPageBtn.containsPoint(x, y))
		{
			int maxPages = Math.Max(0, (_filteredSkins.Count - 1) / SkinsPerPage);
			_currentPage = (_currentPage - 1 + (maxPages + 1)) % (maxPages + 1);
			Game1.playSound("shwip");
			SetupButtons();
			return;
		}

		if (_nextPageBtn != null && _nextPageBtn.containsPoint(x, y))
		{
			int maxPages = Math.Max(0, (_filteredSkins.Count - 1) / SkinsPerPage);
			_currentPage = (_currentPage + 1) % (maxPages + 1);
			Game1.playSound("shwip");
			SetupButtons();
			return;
		}

		foreach (var sb in _skinButtons)
		{
			if (sb.containsPoint(x, y))
			{
				_selectedSkin = sb.name;
				Game1.playSound("select");
				return;
			}
		}
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape || key == Keys.Back)
		{
			CloseMenu();
			return;
		}
		if (key == Keys.Enter || key == Keys.Space)
		{
			if (!string.IsNullOrEmpty(_selectedSkin))
			{
				if (_onSelectCallback != null)
				{
					Game1.playSound("reward");
					_onSelectCallback(_selectedSkin);
					return;
				}
				PokemonUI.TrainerSkin = _selectedSkin;
				Game1.playSound("reward");
				Game1.addHUDMessage(new HUDMessage($"Treinador de batalha definido como: {_selectedSkin}!", 2));
			}
			CloseMenu();
			return;
		}
		base.receiveKeyPress(key);
	}

	private void CloseMenu()
	{
		Game1.playSound("bigDeSelect");
		if (_parentMenu != null)
		{
			Game1.activeClickableMenu = _parentMenu;
			if (_parentMenu is PokemonDebugMenu dbg)
			{
				// Refresh parent debug buttons so updated skin name displays immediately
				dbg.SetupButtons();
			}
		}
		else
		{
			exitThisMenuNoSound();
		}
	}

	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.75f);
		IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

		// Header Title
		string title = !string.IsNullOrEmpty(_customTitle) ? _customTitle : "SELEÇÃO DE TREINADOR DE BATALHA (SKIN)";
		PokemonUI.Text(b, Game1.dialogueFont, title, new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + 16, width - 64, 32), Color.DarkGoldenrod, center: false, maxScale: 0.82f);

		int mx = Game1.getMouseX();
		int my = Game1.getMouseY();

		// Draw Filter Buttons
		foreach (var fb in _filterButtons)
		{
			bool isSel = fb.name == _filter.ToString();
			bool hov = fb.containsPoint(mx, my);
			Color col = isSel ? new Color(255, 230, 140) : (hov ? new Color(245, 240, 215) : Color.White);
			IClickableMenu.drawTextureBox(
				b,
				Game1.mouseCursors,
				new Rectangle(432, 439, 9, 9),
				fb.bounds.X,
				fb.bounds.Y,
				fb.bounds.Width,
				fb.bounds.Height,
				col,
				4f,
				drawShadow: false);
			PokemonUI.Text(b, Game1.smallFont, fb.label, fb.bounds, isSel ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.65f, shadow: false);
		}

		// Draw List Frame
		int listX = xPositionOnScreen + 28;
		int listStartY = yPositionOnScreen + 98;
		IClickableMenu.drawTextureBox(b, listX, listStartY, 398, 440, Color.LightGray * 0.45f);

		// Draw Skin Buttons
		foreach (var sb in _skinButtons)
		{
			bool isSel = sb.name == _selectedSkin;
			bool hov = sb.containsPoint(mx, my);
			Color cardTint = isSel ? new Color(220, 255, 210) : (hov ? new Color(255, 250, 220) : Color.White);
			IClickableMenu.drawTextureBox(b, sb.bounds.X, sb.bounds.Y, sb.bounds.Width, sb.bounds.Height, cardTint);

			bool isFemale = sb.name switch
			{
				"May" or "May (RS)" or "Leaf" or "Dawn" or "Dawn (DP)" or "Cheryl" or "Marley" or "Mira" => true,
				_ => false
			};
			string genderIcon = isFemale ? "♀ [F]" : "♂ [M]";
			Color genColor = isFemale ? new Color(235, 90, 150) : new Color(60, 130, 240);

			PokemonUI.Text(b, Game1.smallFont, sb.name, new Rectangle(sb.bounds.X + 14, sb.bounds.Y + 7, 240, 24), isSel ? Color.DarkGreen : Game1.textColor, maxScale: 0.72f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, genderIcon, new Rectangle(sb.bounds.X + sb.bounds.Width - 85, sb.bounds.Y + 8, 75, 22), genColor, center: true, maxScale: 0.64f, shadow: false);
		}

		// Page Buttons
		if (_prevPageBtn != null)
		{
			bool hov = _prevPageBtn.containsPoint(mx, my);
			IClickableMenu.drawTextureBox(b, _prevPageBtn.bounds.X, _prevPageBtn.bounds.Y, _prevPageBtn.bounds.Width, _prevPageBtn.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _prevPageBtn.label, _prevPageBtn.bounds, hov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.66f, shadow: false);
		}

		int maxPages = Math.Max(0, (_filteredSkins.Count - 1) / SkinsPerPage);
		string pStr = $"Pág {_currentPage + 1}/{maxPages + 1}";
		PokemonUI.Text(b, Game1.smallFont, pStr, new Rectangle(listX + 140, yPositionOnScreen + height - 52, 118, 28), Color.DarkGoldenrod, center: true, maxScale: 0.70f);

		if (_nextPageBtn != null)
		{
			bool hov = _nextPageBtn.containsPoint(mx, my);
			IClickableMenu.drawTextureBox(b, _nextPageBtn.bounds.X, _nextPageBtn.bounds.Y, _nextPageBtn.bounds.Width, _nextPageBtn.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _nextPageBtn.label, _nextPageBtn.bounds, hov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.66f, shadow: false);
		}

		// =====================================================================
		// RIGHT PANE: LIVE PREVIEW
		// =====================================================================
		int rightX = xPositionOnScreen + 444;
		int rightY = yPositionOnScreen + 56;
		int rightW = 380;
		int rightH = 466;

		IClickableMenu.drawTextureBox(b, rightX, rightY, rightW, rightH, Color.White);

		if (!string.IsNullOrEmpty(_selectedSkin))
		{
			if (_previewSkin != _selectedSkin || _previewTex == null)
			{
				_previewSkin = _selectedSkin;
				_previewTex = PokemonUI.GetTrainerBackTexture(_previewSkin);
			}

			// Subtitle banner with skin name
			int bannerY = rightY + 16;
			PokemonUI.Text(b, Game1.dialogueFont, _selectedSkin, new Rectangle(rightX + 16, bannerY, rightW - 32, 30), Color.DarkGoldenrod, center: true, maxScale: 0.85f);

			bool isFemale = _selectedSkin switch
			{
				"May" or "May (RS)" or "Leaf" or "Dawn" or "Dawn (DP)" or "Cheryl" or "Marley" or "Mira" => true,
				_ => false
			};
			string genText = isFemale ? "Gênero: Feminino ♀" : "Gênero: Masculino ♂";
			PokemonUI.Text(b, Game1.smallFont, genText, new Rectangle(rightX + 16, bannerY + 30, rightW - 32, 22), isFemale ? new Color(215, 60, 130) : new Color(30, 105, 215), center: true, maxScale: 0.66f, shadow: false);

			// Arena Battle Preview Stage Box
			int stageBoxX = rightX + 24;
			int stageBoxY = bannerY + 58;
			int stageBoxW = rightW - 48;
			int stageBoxH = 240;
			IClickableMenu.drawTextureBox(b, stageBoxX, stageBoxY, stageBoxW, stageBoxH, new Color(245, 248, 255));

			// Battle Platform
			int platW = 200;
			int platH = 34;
			int platX = stageBoxX + (stageBoxW - platW) / 2;
			int platY = stageBoxY + stageBoxH - 52;
			b.Draw(Game1.staminaRect, new Rectangle(platX, platY + 6, platW, platH - 6), new Color(130, 120, 105));
			b.Draw(Game1.staminaRect, new Rectangle(platX + 4, platY, platW - 8, platH - 6), new Color(185, 175, 155));
			PokemonUI.SoftShadow(b, platX + 20, platY + 4, platW - 40, 14, 0.40f);

			// Draw Trainer Back Sprite
			if (_previewTex != null)
			{
				int maxFrames = Math.Max(1, _previewTex.Height / 64);
				int frameIdx = 0;
				Rectangle trSrc = new Rectangle(0, frameIdx * 64, 64, 64);
				Vector2 pos = new Vector2(stageBoxX + stageBoxW / 2f, platY + 8);
				Vector2 origin = new Vector2(32f, 64f);
				float scale = 3.2f;
				b.Draw(_previewTex, pos, trSrc, Color.White, 0f, origin, scale, SpriteEffects.None, 0.88f);
			}

			// Lore and game origin description
			string originGame = _selectedSkin switch
			{
				"Lucas" or "Dawn" or "Lucas (DP)" or "Dawn (DP)" or "Barry" or "Riley" or "Cheryl" or "Marley" or "Buck" or "Mira" => "Origem: Região de Sinnoh (Pokémon Platinum / D&P)",
				"PokeDude" or "Old Man" => "Origem: Região de Kanto (Pokémon FireRed & LeafGreen)",
				"Brendan" or "May" or "Brendan (RS)" or "May (RS)" or "Steven" or "Wally" => "Origem: Região de Hoenn (Pokémon Emerald / R&S)",
				_ => "Origem: Treinador Clássico da Franquia Pokémon"
			};
			PokemonUI.Text(b, Game1.smallFont, originGame, new Rectangle(rightX + 16, stageBoxY + stageBoxH + 12, rightW - 32, 22), Color.DarkSlateBlue, center: true, maxScale: 0.60f, shadow: false);
			PokemonUI.Text(b, Game1.tinyFont, "Esta aparência será utilizada durante todas as suas batalhas!", new Rectangle(rightX + 16, stageBoxY + stageBoxH + 34, rightW - 32, 18), Color.DimGray, center: true, maxScale: 0.75f, shadow: false);
		}

		// Action Buttons
		if (_confirmBtn != null)
		{
			bool hov = _confirmBtn.containsPoint(mx, my);
			IClickableMenu.drawTextureBox(b, _confirmBtn.bounds.X, _confirmBtn.bounds.Y, _confirmBtn.bounds.Width, _confirmBtn.bounds.Height, hov ? new Color(210, 255, 210) : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _confirmBtn.label, _confirmBtn.bounds, hov ? Color.DarkGreen : new Color(25, 95, 35), center: true, maxScale: 0.72f, shadow: false);
		}

		if (_cancelBtn != null)
		{
			bool hov = _cancelBtn.containsPoint(mx, my);
			IClickableMenu.drawTextureBox(b, _cancelBtn.bounds.X, _cancelBtn.bounds.Y, _cancelBtn.bounds.Width, _cancelBtn.bounds.Height, hov ? new Color(255, 220, 220) : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _cancelBtn.label, _cancelBtn.bounds, hov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.72f, shadow: false);
		}

		upperRightCloseButton.draw(b);
		drawMouse(b);
	}
}
