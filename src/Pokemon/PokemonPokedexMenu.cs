using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

public class PokemonPokedexMenu : IClickableMenu
{
	private enum PokedexFilter
	{
		All,
		Seen,
		Caught,
		Gen1,
		Gen2,
		Gen3,
		Gen4,
		Gen5,
		Gen6,
		Gen7,
		Gen8,
		Gen9
	}

	private PokedexFilter _filter = PokedexFilter.All;
	private int _currentPage = 0;
	private const int EntriesPerPage = 8;

	private readonly List<PokemonSpecies> _filteredList = new();
	private PokemonSpecies _selectedSpecies;

	private readonly List<ClickableComponent> _speciesButtons = new();
	private readonly List<ClickableComponent> _filterButtons = new();
	private ClickableComponent _prevButton;
	private ClickableComponent _nextButton;
	private ClickableTextureComponent _closeButton;

	private int _gamepadIndex = 0;
	private static PokemonPokedex Pokedex => PokemonPokedex.GetForFarmer(Game1.player);

	private PokemonInstance _cachedDummy;
	private string _cachedSpriteSpecies;

	public PokemonPokedexMenu(string initialSpecies = null)
		: base(0, 0, 960, 660)
	{
		xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
		yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

		_closeButton = new ClickableTextureComponent(
			new Rectangle(xPositionOnScreen + width - 52, yPositionOnScreen + 14, 44, 44),
			Game1.mouseCursors,
			new Rectangle(337, 494, 12, 12),
			3.6f
		);

		RefreshFilterList();

		if (!string.IsNullOrWhiteSpace(initialSpecies))
		{
			_selectedSpecies = PokemonSpecies.Get(initialSpecies);
		}
		if (_selectedSpecies == null && _filteredList.Count > 0)
		{
			_selectedSpecies = _filteredList[0];
		}

		SetupButtons();
	}

	private void RefreshFilterList()
	{
		_filteredList.Clear();
		var all = PokemonSpecies.AllSpecies();

		foreach (var sp in all)
		{
			bool isSeen = Pokedex.IsSeen(sp.Name);
			bool isCaught = Pokedex.IsCaught(sp.Name);
			int dex = sp.DexNumber;

			bool match = _filter switch
			{
				PokedexFilter.All    => true,
				PokedexFilter.Seen   => isSeen,
				PokedexFilter.Caught => isCaught,
				PokedexFilter.Gen1   => dex >= 1 && dex <= 151,
				PokedexFilter.Gen2   => dex >= 152 && dex <= 251,
				PokedexFilter.Gen3   => dex >= 252 && dex <= 386,
				PokedexFilter.Gen4   => dex >= 387 && dex <= 493,
				PokedexFilter.Gen5   => dex >= 494 && dex <= 649,
				PokedexFilter.Gen6   => dex >= 650 && dex <= 721,
				PokedexFilter.Gen7   => dex >= 722 && dex <= 809,
				PokedexFilter.Gen8   => dex >= 810 && dex <= 905,
				PokedexFilter.Gen9   => dex >= 906 && dex <= 1025,
				_ => true
			};

			if (match)
			{
				_filteredList.Add(sp);
			}
		}

		int maxPages = Math.Max(0, (_filteredList.Count - 1) / EntriesPerPage);
		if (_currentPage > maxPages) _currentPage = maxPages;
		if (_currentPage < 0) _currentPage = 0;
	}

	private void SetupButtons()
	{
		_filterButtons.Clear();
		_speciesButtons.Clear();

		int startX = xPositionOnScreen + 28;
		int startY = yPositionOnScreen + 54;

		// Filter buttons (styled with Stardew texture box)
		string[] filterLabels = new[] { "Todos", "Vistos", "Capturados", "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9" };
		int currentBx = startX;
		for (int i = 0; i < filterLabels.Length; i++)
		{
			int bw = (i == 2) ? 98 : ((i == 1) ? 66 : ((i == 0) ? 62 : 44));
			_filterButtons.Add(new ClickableComponent(new Rectangle(currentBx, startY, bw, 30), ((PokedexFilter)i).ToString(), filterLabels[i]));
			currentBx += bw + 4;
		}

		// Species buttons on left pane
		int listY = startY + 40;
		int startIndex = _currentPage * EntriesPerPage;
		int countOnPage = Math.Min(EntriesPerPage, _filteredList.Count - startIndex);

		for (int i = 0; i < countOnPage; i++)
		{
			var sp = _filteredList[startIndex + i];
			int by = listY + i * 58;
			_speciesButtons.Add(new ClickableComponent(new Rectangle(startX, by, 444, 54), sp.Name));
		}

		// Pagination controls
		int pageY = listY + EntriesPerPage * 58 + 10;
		_prevButton = new ClickableComponent(new Rectangle(startX + 12, pageY, 130, 34), "prev", "◄ Anterior");
		_nextButton = new ClickableComponent(new Rectangle(startX + 302, pageY, 130, 34), "next", "Próximo ►");
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);

		if (_closeButton.containsPoint(x, y))
		{
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
			return;
		}

		foreach (var fb in _filterButtons)
		{
			if (fb.containsPoint(x, y))
			{
				_filter = Enum.Parse<PokedexFilter>(fb.name);
				_currentPage = 0;
				Game1.playSound("smallSelect");
				RefreshFilterList();
				if (_filteredList.Count > 0 && (_selectedSpecies == null || !_filteredList.Contains(_selectedSpecies)))
				{
					_selectedSpecies = _filteredList[0];
					_cachedDummy = null;
					_cachedSpriteSpecies = null;
				}
				SetupButtons();
				return;
			}
		}

		if (_prevButton != null && _prevButton.containsPoint(x, y))
		{
			int maxPages = Math.Max(0, (_filteredList.Count - 1) / EntriesPerPage);
			_currentPage = (_currentPage - 1 + (maxPages + 1)) % (maxPages + 1);
			Game1.playSound("shwip");
			SetupButtons();
			return;
		}

		if (_nextButton != null && _nextButton.containsPoint(x, y))
		{
			int maxPages = Math.Max(0, (_filteredList.Count - 1) / EntriesPerPage);
			_currentPage = (_currentPage + 1) % (maxPages + 1);
			Game1.playSound("shwip");
			SetupButtons();
			return;
		}

		foreach (var sb in _speciesButtons)
		{
			if (sb.containsPoint(x, y))
			{
				_selectedSpecies = PokemonSpecies.Get(sb.name);
				_cachedDummy = null;
				_cachedSpriteSpecies = null;
				Game1.playSound("select");
				if (_selectedSpecies != null)
				{
					PokemonCryManager.PlayCry(_selectedSpecies.Name);
				}
				return;
			}
		}
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape || key == Keys.P || key == Keys.F8 || key == Keys.F10)
		{
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
			return;
		}
		base.receiveKeyPress(key);
	}

	public override bool areGamePadControlsImplemented() => true;

	public override void receiveGamePadButton(Buttons button)
	{
		if (button == Buttons.B || button == Buttons.Back)
		{
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
			return;
		}

		int countOnPage = _speciesButtons.Count;
		int maxPages = Math.Max(0, (_filteredList.Count - 1) / EntriesPerPage);

		if (button == Buttons.DPadUp || button == Buttons.LeftThumbstickUp)
		{
			if (_gamepadIndex > 0)
			{
				_gamepadIndex--;
				SelectGamepadSpecies();
				Game1.playSound("shiny");
			}
			else if (_currentPage > 0)
			{
				_currentPage--;
				_gamepadIndex = EntriesPerPage - 1;
				SetupButtons();
				SelectGamepadSpecies();
				Game1.playSound("shwip");
			}
			return;
		}

		if (button == Buttons.DPadDown || button == Buttons.LeftThumbstickDown)
		{
			if (_gamepadIndex < countOnPage - 1)
			{
				_gamepadIndex++;
				SelectGamepadSpecies();
				Game1.playSound("shiny");
			}
			else if (_currentPage < maxPages)
			{
				_currentPage++;
				_gamepadIndex = 0;
				SetupButtons();
				SelectGamepadSpecies();
				Game1.playSound("shwip");
			}
			return;
		}

		// Page switching with LB / RB
		if (button == Buttons.LeftShoulder || button == Buttons.LeftTrigger)
		{
			_currentPage = (_currentPage - 1 + (maxPages + 1)) % (maxPages + 1);
			_gamepadIndex = 0;
			SetupButtons();
			SelectGamepadSpecies();
			Game1.playSound("shwip");
			return;
		}
		if (button == Buttons.RightShoulder || button == Buttons.RightTrigger)
		{
			_currentPage = (_currentPage + 1) % (maxPages + 1);
			_gamepadIndex = 0;
			SetupButtons();
			SelectGamepadSpecies();
			Game1.playSound("shwip");
			return;
		}

		// Cycle filters with X / Y
		if (button == Buttons.X || button == Buttons.Y)
		{
			int totalFilters = Enum.GetValues<PokedexFilter>().Length;
			int nextFilter = ((int)_filter + (button == Buttons.Y ? 1 : totalFilters - 1)) % totalFilters;
			_filter = (PokedexFilter)nextFilter;
			_currentPage = 0;
			_gamepadIndex = 0;
			RefreshFilterList();
			if (_filteredList.Count > 0)
			{
				_selectedSpecies = _filteredList[0];
				_cachedDummy = null;
				_cachedSpriteSpecies = null;
			}
			SetupButtons();
			Game1.playSound("smallSelect");
			return;
		}

		if (button == Buttons.A || button == Buttons.Start)
		{
			SelectGamepadSpecies();
			Game1.playSound("select");
			return;
		}
	}

	private void SelectGamepadSpecies()
	{
		int startIndex = _currentPage * EntriesPerPage;
		if (startIndex + _gamepadIndex >= 0 && startIndex + _gamepadIndex < _filteredList.Count)
		{
			_selectedSpecies = _filteredList[startIndex + _gamepadIndex];
			_cachedDummy = null;
			_cachedSpriteSpecies = null;
		}
	}

	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.72f);
		IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

		int seenCount = Pokedex.SeenSpecies.Count;
		int caughtCount = Pokedex.CaughtSpecies.Count;
		int totalDex = PokemonSpecies.AllSpecies().Count;
		string title = "POKÉDEX NACIONAL";
		string stats = $"Vistos: {seenCount}   •   Capturados: {caughtCount}   •   Total Registrável: {totalDex}";

		PokemonUI.Text(b, Game1.dialogueFont, title, new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + 16, 310, 32), Color.DarkGoldenrod, center: false, maxScale: 0.82f);
		PokemonUI.Text(b, Game1.smallFont, stats, new Rectangle(xPositionOnScreen + 345, yPositionOnScreen + 20, 540, 26), Color.DarkGreen, center: false, maxScale: 0.78f);

		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		// Draw Filter buttons with Stardew texture box styling
		foreach (var fb in _filterButtons)
		{
			bool isSelected = fb.name == _filter.ToString();
			bool hov = fb.containsPoint(mouseX, mouseY);
			Color col = isSelected ? new Color(255, 230, 140) : (hov ? new Color(245, 240, 215) : Color.White);
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
				drawShadow: false
			);
			PokemonUI.Text(b, Game1.smallFont, fb.label, fb.bounds, isSelected ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.62f, shadow: false);
		}

		// Left list frame
		int listFrameX = xPositionOnScreen + 22;
		int listFrameY = yPositionOnScreen + 90;
		IClickableMenu.drawTextureBox(b, listFrameX, listFrameY, 456, 476, Color.LightGray * 0.45f);

		// Draw Species entries
		for (int i = 0; i < _speciesButtons.Count; i++)
		{
			var sb = _speciesButtons[i];
			var sp = PokemonSpecies.Get(sb.name);
			bool isSelected = _selectedSpecies != null && _selectedSpecies.InternalName == sp.InternalName;
			bool hov = sb.containsPoint(mouseX, mouseY);
			bool isGamepadFocus = (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected) && _gamepadIndex == i;
			bool isSeen = Pokedex.IsSeen(sp.Name);
			bool isCaught = Pokedex.IsCaught(sp.Name);
			int hintLvl = Pokedex.GetHintLevel(sp.Name);

			Color cardBg = isSelected ? Color.LightGreen : ((hov || isGamepadFocus) ? Color.LightGoldenrodYellow : Color.White);
			IClickableMenu.drawTextureBox(b, sb.bounds.X, sb.bounds.Y, sb.bounds.Width, sb.bounds.Height, cardBg);

			if (isGamepadFocus)
			{
				float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 6f);
				int exp = pulse > 0 ? 3 : 2;
				Rectangle glow = new Rectangle(sb.bounds.X - exp, sb.bounds.Y - exp, sb.bounds.Width + exp * 2, sb.bounds.Height + exp * 2);
				PokemonUI.Border(b, glow, 3, Color.Gold);
			}

			if (isCaught)
			{
				b.Draw(Game1.staminaRect, new Rectangle(sb.bounds.X + 10, sb.bounds.Y + 19, 14, 14), Color.Crimson);
				b.Draw(Game1.staminaRect, new Rectangle(sb.bounds.X + 12, sb.bounds.Y + 21, 10, 10), Color.White);
				b.Draw(Game1.staminaRect, new Rectangle(sb.bounds.X + 15, sb.bounds.Y + 24, 4, 4), Color.Black);
			}
			else if (hintLvl > 0)
			{
				b.Draw(Game1.staminaRect, new Rectangle(sb.bounds.X + 10, sb.bounds.Y + 19, 14, 14), Color.Gold);
				PokemonUI.Border(b, new Rectangle(sb.bounds.X + 10, sb.bounds.Y + 19, 14, 14), 1, Color.DarkGoldenrod);
			}

			string numStr = $"#{sp.DexNumber:D4}";
			PokemonUI.Text(b, Game1.smallFont, numStr, new Rectangle(sb.bounds.X + 28, sb.bounds.Y + 8, 72, 20), Color.DimGray, maxScale: 0.70f, shadow: false);

			string displayName = isSeen ? sp.Name : "???";
			PokemonUI.Text(b, Game1.smallFont, displayName, new Rectangle(sb.bounds.X + 104, sb.bounds.Y + 6, 220, 22), isSelected ? Color.DarkGreen : Game1.textColor, maxScale: 0.80f, shadow: false);

			var dummy = new PokemonInstance(sp, 1);
			Texture2D icon = dummy.GetIconSprite(Game1.graphics.GraphicsDevice) ?? dummy.GetFrontSprite(Game1.graphics.GraphicsDevice);
			if (icon != null)
			{
				Rectangle srcRect = (icon == dummy.GetIconSprite(Game1.graphics.GraphicsDevice))
					? dummy.GetIconSourceRect()
					: dummy.GetFrontSourceRect();
				float iconScale = (srcRect.Width > 32) ? 0.55f : 1.05f;
				Color tint = isSeen ? Color.White : Color.Black * 0.80f;
				b.Draw(icon, new Vector2(sb.bounds.X + 388, sb.bounds.Y + 9), srcRect, tint, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0.86f);
			}

			if (isSeen)
			{
				int pillW1 = DrawSmallTypePill(b, sp.PrimaryType, sb.bounds.X + 104, sb.bounds.Y + 29);
				if (sp.SecondaryType.HasValue)
				{
					DrawSmallTypePill(b, sp.SecondaryType.Value, sb.bounds.X + 104 + pillW1 + 6, sb.bounds.Y + 29);
				}
			}
		}

		// Page indicator & buttons
		int maxPages = Math.Max(0, (_filteredList.Count - 1) / EntriesPerPage);
		if (_prevButton != null)
		{
			bool hov = _prevButton.containsPoint(mouseX, mouseY);
			IClickableMenu.drawTextureBox(b, _prevButton.bounds.X, _prevButton.bounds.Y, _prevButton.bounds.Width, _prevButton.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _prevButton.label, _prevButton.bounds, hov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.72f, shadow: false);
		}

		string pStr = $"Pág {_currentPage + 1}/{maxPages + 1}";
		PokemonUI.Text(b, Game1.smallFont, pStr, new Rectangle(listFrameX + 152, yPositionOnScreen + 572, 150, 28), Color.DarkGoldenrod, center: true, maxScale: 0.78f);

		if (_nextButton != null)
		{
			bool hov = _nextButton.containsPoint(mouseX, mouseY);
			IClickableMenu.drawTextureBox(b, _nextButton.bounds.X, _nextButton.bounds.Y, _nextButton.bounds.Width, _nextButton.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _nextButton.label, _nextButton.bounds, hov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.72f, shadow: false);
		}

		// Right Pane: Detailed Entry Info
		int detailX = xPositionOnScreen + 488;
		int detailY = yPositionOnScreen + 90;
		int detailW = 446;
		int detailH = 534;
		IClickableMenu.drawTextureBox(b, detailX, detailY, detailW, detailH, Color.White);

		DrawDetailedEntry(b, detailX, detailY, detailW, detailH);

		if (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected)
		{
			string legend = "[D-Pad] Navegar  [LB/RB] Páginas  [X/Y] Filtro  [B] Voltar";
			PokemonUI.Text(b, Game1.tinyFont, legend, new Rectangle(xPositionOnScreen + 28, yPositionOnScreen + height - 26, 440, 18), Color.DarkSlateGray * 0.85f, maxScale: 0.72f);
		}

		_closeButton.draw(b);
		drawMouse(b);
	}

	private void DrawDetailedEntry(SpriteBatch b, int x, int y, int w, int h)
	{
		if (_selectedSpecies == null) return;

		bool isSeen = Pokedex.IsSeen(_selectedSpecies.Name);
		bool isCaught = Pokedex.IsCaught(_selectedSpecies.Name);
		int hintLvl = Pokedex.GetHintLevel(_selectedSpecies.Name);
		var entry = Pokedex.GetEntry(_selectedSpecies.Name);

		int boxX = x + 18;
		int boxY = y + 16;
		int boxSize = 130;
		IClickableMenu.drawTextureBox(b, boxX, boxY, boxSize, boxSize, Color.AliceBlue);

		if (_cachedSpriteSpecies != _selectedSpecies.InternalName || _cachedDummy == null)
		{
			_cachedDummy = new PokemonInstance(_selectedSpecies, 1);
			_cachedSpriteSpecies = _selectedSpecies.InternalName;
		}

		Texture2D frontTex = _cachedDummy.GetFrontSprite(Game1.graphics.GraphicsDevice);
		if (frontTex != null)
		{
			Rectangle srcRect = _cachedDummy.GetFrontSourceRect();
			Color tint = isSeen ? Color.White : Color.Black * 0.90f;
			float fitScale = Math.Min(96f / Math.Max(1, srcRect.Width), 96f / Math.Max(1, srcRect.Height));
			Vector2 centerPos = new Vector2(boxX + boxSize / 2f, boxY + boxSize / 2f);
			Vector2 origin = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
			b.Draw(frontTex, centerPos, srcRect, tint, 0f, origin, fitScale, SpriteEffects.None, 0.86f);
		}

		int headerX = boxX + boxSize + 14;
		int headerW = w - boxSize - 50;

		string statusTag = isCaught
			? " [CAPTURADO]"
			: (hintLvl > 0 ? $" [DICA NV.{hintLvl}]" : (isSeen ? " [VISTO]" : ""));
		string numStr = $"No. {_selectedSpecies.DexNumber:D4}{statusTag}";
		PokemonUI.Text(b, Game1.smallFont, numStr, new Rectangle(headerX, boxY + 2, headerW, 20), isCaught ? Color.DarkGreen : (hintLvl > 0 ? Color.DarkGoldenrod : Color.DarkSlateGray), maxScale: 0.66f, shadow: false);

		string nameStr = isSeen ? _selectedSpecies.Name : "??? Desconhecido";
		PokemonUI.Text(b, Game1.dialogueFont, nameStr, new Rectangle(headerX, boxY + 22, headerW, 28), Color.DarkGoldenrod, maxScale: 0.78f);

		string tierPt = entry.Tier switch
		{
			"Starter"   => "Inicial",
			"Legendary" => "Lendário",
			"Mythical"  => "Mítico",
			_           => "Normal"
		};
		string catStr = isSeen ? $"Pokémon {entry.Category} • [{tierPt}]" : "Dados não registrados";
		PokemonUI.Text(b, Game1.smallFont, catStr, new Rectangle(headerX, boxY + 52, headerW, 18), Color.DimGray, maxScale: 0.62f, shadow: false);

		if (isSeen)
		{
			int w1 = DrawSmallTypePill(b, _selectedSpecies.PrimaryType, headerX, boxY + 74);
			if (_selectedSpecies.SecondaryType.HasValue)
			{
				DrawSmallTypePill(b, _selectedSpecies.SecondaryType.Value, headerX + w1 + 6, boxY + 74);
			}

			Rectangle hwRect = new Rectangle(headerX, boxY + 98, headerW, 26);
			b.Draw(Game1.staminaRect, hwRect, new Color(235, 242, 252));
			PokemonUI.Border(b, hwRect, 1, new Color(180, 195, 220));
			string hwStr = $"Altura: {entry.HeightMeters:F1} m   |   Peso: {entry.WeightKg:F1} kg";
			PokemonUI.Text(b, Game1.smallFont, hwStr, new Rectangle(hwRect.X + 6, hwRect.Y + 2, hwRect.Width - 12, 22), Color.DarkSlateBlue, center: true, maxScale: 0.62f, shadow: false);
		}

		int lineY = boxY + boxSize + 10;
		b.Draw(Game1.staminaRect, new Rectangle(x + 18, lineY, w - 36, 2), Color.Silver);

		int ecoY = lineY + 6;
		PokemonUI.Text(b, Game1.smallFont, "🌿 ECOLOGIA, HABITAT, ESTAÇÃO, CLIMA & HORA:", new Rectangle(x + 22, ecoY, w - 44, 20), Color.DarkSlateGray, maxScale: 0.70f);

		if (isSeen)
		{
			string regionName = _selectedSpecies.DexNumber switch
			{
				<= 151 => "Kanto (Gen 1)",
				<= 251 => "Johto (Gen 2)",
				<= 386 => "Hoenn (Gen 3)",
				<= 493 => "Sinnoh (Gen 4)",
				<= 649 => "Unova (Gen 5)",
				<= 721 => "Kalos (Gen 6)",
				<= 809 => "Alola (Gen 7)",
				<= 905 => "Galar / Hisui (Gen 8)",
				_      => "Paldea (Gen 9)"
			};

			// Hint gating:
			// Caught or HintLvl >= 3 -> all unlocked
			// HintLvl == 2 (Avançada) -> Habitat, Season, Weather unlocked; Time locked
			// HintLvl == 1 (Normal)   -> Habitat, Season unlocked; Weather, Time locked
			// HintLvl == 0 (Seen in battle, not caught) -> Habitat & Season unlocked if wild-seen, or show Hint prompt for locked levels
			bool showLocAndSeason = isCaught || hintLvl >= 1 || isSeen;
			bool showWeather = isCaught || hintLvl >= 2 || (hintLvl == 0 && isSeen);
			bool showTime = isCaught || hintLvl >= 3 || (hintLvl == 0 && isSeen);

			// Note: If hintLvl is 1 or 2 (explicitly purchased from the Dwarf), strictly enforce the 3-tier hint visibility!
			if (!isCaught && hintLvl == 1)
			{
				showLocAndSeason = true;
				showWeather = false;
				showTime = false;
			}
			else if (!isCaught && hintLvl == 2)
			{
				showLocAndSeason = true;
				showWeather = true;
				showTime = false;
			}

			bool isPt = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.pt;

			string habRaw = PokemonPokedexCatalog.GetLocalizedHabitat(entry, isPt);
			string seaRaw = PokemonPokedexCatalog.GetLocalizedSeasons(entry, isPt);
			string weaRaw = PokemonPokedexCatalog.GetLocalizedWeather(entry, isPt);
			string timRaw = PokemonPokedexCatalog.GetLocalizedTimeOfDay(entry, isPt);

			string habText = showLocAndSeason ? habRaw : (isPt ? "🔒 [Requer Dica Normal com o Anão]" : "🔒 [Requires Regular Hint from Dwarf]");
			string seaText = showLocAndSeason ? seaRaw : (isPt ? "🔒 [Requer Dica Normal com o Anão]" : "🔒 [Requires Regular Hint from Dwarf]");
			string weaText = showWeather ? weaRaw : (isPt ? "🔒 [Requer Dica Avançada com o Anão ou Captura]" : "🔒 [Requires Advanced Hint or Catch]");
			string timText = showTime ? timRaw : (isPt ? "🔒 [Requer Dica Total com o Anão ou Captura]" : "🔒 [Requires Complete Hint or Catch]");

			int infoY = ecoY + 21;
			string tierLabel = isPt ? tierPt : (entry.Tier ?? "Normal");
			PokemonUI.Text(b, Game1.smallFont, isPt ? $"• Região: {regionName} ({tierLabel})" : $"• Region: {regionName} ({tierLabel})", new Rectangle(x + 22, infoY, w - 44, 18), Color.DarkBlue, maxScale: 0.61f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, isPt ? $"• Local: {habText}" : $"• Location: {habText}", new Rectangle(x + 22, infoY + 19, w - 44, 18), Color.DarkSlateGray, maxScale: 0.59f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, isPt ? $"• Estação: {seaText}" : $"• Season: {seaText}", new Rectangle(x + 22, infoY + 38, w - 44, 18), new Color(35, 105, 50), maxScale: 0.59f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, isPt ? $"• Clima: {weaText}" : $"• Weather: {weaText}", new Rectangle(x + 22, infoY + 57, w - 44, 18), showWeather ? Color.DarkSlateGray : Color.IndianRed, maxScale: 0.59f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, isPt ? $"• Hora: {timText}" : $"• Time: {timText}", new Rectangle(x + 22, infoY + 76, w - 44, 18), showTime ? Color.DarkSlateGray : Color.IndianRed, maxScale: 0.59f, shadow: false);

			int descY = infoY + 98;
			int descBoxH = 125;
			IClickableMenu.drawTextureBox(b, x + 18, descY, w - 36, descBoxH, new Color(254, 252, 245));
			PokemonUI.Text(b, Game1.smallFont, isPt ? "DADOS OFICIAIS DA POKÉDEX:" : "OFFICIAL POKÉDEX DATA:", new Rectangle(x + 28, descY + 6, w - 56, 18), Color.DarkGoldenrod, maxScale: 0.62f, shadow: false);
			string desc = isPt ? PokemonPokedexCatalog.GetPortugueseDescription(entry) : PokemonPokedexCatalog.GetEnglishDescription(entry);
			PokemonUI.WrappedText(b, Game1.smallFont, desc, new Rectangle(x + 28, descY + 26, w - 56, 92), new Color(55, 45, 38), scale: 0.58f);

			int statY = descY + descBoxH + 4;
			string baseStatsLine = isPt
				? $"BASE: HP {_selectedSpecies.BaseHP} | Atq {_selectedSpecies.BaseAttack} | Def {_selectedSpecies.BaseDefense} | SpA {_selectedSpecies.BaseSpAttack} | SpD {_selectedSpecies.BaseSpDefense} | Vel {_selectedSpecies.BaseSpeed}"
				: $"BASE: HP {_selectedSpecies.BaseHP} | Atk {_selectedSpecies.BaseAttack} | Def {_selectedSpecies.BaseDefense} | SpA {_selectedSpecies.BaseSpAttack} | SpD {_selectedSpecies.BaseSpDefense} | Spe {_selectedSpecies.BaseSpeed}";
			PokemonUI.Text(b, Game1.smallFont, baseStatsLine, new Rectangle(x + 20, statY, w - 40, 20), Color.DarkSlateGray, center: true, maxScale: 0.58f, shadow: false);
		}
		else
		{
			bool isPt = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.pt;
			int infoY = ecoY + 28;
			PokemonUI.Text(b, Game1.smallFont, isPt ? "Este Pokémon ainda não foi avistado no Vale." : "This Pokémon has not been sighted in the Valley yet.", new Rectangle(x + 24, infoY, w - 48, 30), Color.Gray, maxScale: 0.72f);
			PokemonUI.WrappedText(b, Game1.smallFont, isPt
				? "Compre dicas de Localização, Estação, Clima e Horário com o Anão nas Minas para marcá-lo como Visto e descobrir onde capturá-lo!"
				: "Purchase Location, Season, Weather, and Time hints from the Dwarf in the Mines to mark it as Seen and discover where to catch it!",
				new Rectangle(x + 24, infoY + 34, w - 48, 100), Color.DarkSlateGray, scale: 0.64f);
		}
	}

	private static int DrawSmallTypePill(SpriteBatch b, PokemonType type, int px, int py)
	{
		Color typeCol = PokemonTypeColors.Get(type);
		string name = type.ToString().ToUpperInvariant();
		Vector2 textSz = Game1.smallFont.MeasureString(name) * 0.52f;
		int pillW = Math.Max(46, (int)textSz.X + 12);
		int pillH = 18;
		b.Draw(Game1.staminaRect, new Rectangle(px, py, pillW, pillH), typeCol);
		PokemonUI.Border(b, new Rectangle(px, py, pillW, pillH), 1, Color.White * 0.45f);
		PokemonUI.Text(b, Game1.smallFont, name, new Rectangle(px, py, pillW, pillH), Color.White, center: true, maxScale: 0.50f, shadow: false);
		return pillW;
	}
}
