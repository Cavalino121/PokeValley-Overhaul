using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

/// <summary>
/// Dedicated Starter Pokémon Selection Menu for new farmers or party resets.
/// Allows picking across Generations 1 through 9 with detailed stats, type pills,
/// live sprite preview, and awards a starter kit of 5 Poké Balls and 1 Potion.
/// </summary>
public class PokemonStarterSelectMenu : IClickableMenu
{
	public class StarterOption
	{
		public string SpeciesName { get; }
		public int Gen { get; }
		public PokemonType PrimaryType { get; }
		public PokemonType? SecondaryType { get; }

		public StarterOption(string speciesName, int gen, PokemonType pri, PokemonType? sec = null)
		{
			SpeciesName = speciesName;
			Gen = gen;
			PrimaryType = pri;
			SecondaryType = sec;
		}
	}

	private static readonly List<StarterOption> AllStarters = new()
	{
		// Gen 1
		new("Charmander", 1, PokemonType.Fire),
		new("Squirtle", 1, PokemonType.Water),
		new("Bulbasaur", 1, PokemonType.Grass, PokemonType.Poison),

		// Gen 2
		new("Cyndaquil", 2, PokemonType.Fire),
		new("Totodile", 2, PokemonType.Water),
		new("Chikorita", 2, PokemonType.Grass),

		// Gen 3
		new("Torchic", 3, PokemonType.Fire),
		new("Mudkip", 3, PokemonType.Water, PokemonType.Ground),
		new("Treecko", 3, PokemonType.Grass),

		// Gen 4
		new("Chimchar", 4, PokemonType.Fire),
		new("Piplup", 4, PokemonType.Water),
		new("Turtwig", 4, PokemonType.Grass),

		// Gen 5
		new("Tepig", 5, PokemonType.Fire),
		new("Oshawott", 5, PokemonType.Water),
		new("Snivy", 5, PokemonType.Grass),

		// Gen 6
		new("Fennekin", 6, PokemonType.Fire),
		new("Froakie", 6, PokemonType.Water),
		new("Chespin", 6, PokemonType.Grass),

		// Gen 7
		new("Litten", 7, PokemonType.Fire),
		new("Popplio", 7, PokemonType.Water),
		new("Rowlet", 7, PokemonType.Grass, PokemonType.Flying),

		// Gen 8
		new("Scorbunny", 8, PokemonType.Fire),
		new("Sobble", 8, PokemonType.Water),
		new("Grookey", 8, PokemonType.Grass),

		// Gen 9
		new("Fuecoco", 9, PokemonType.Fire),
		new("Quaxly", 9, PokemonType.Water),
		new("Sprigatito", 9, PokemonType.Grass),

		// Especiais (Fim da Lista)
		new("Pikachu", 10, PokemonType.Electric),
		new("Eevee", 10, PokemonType.Normal)
	};

	private int _selectedGen = 0; // 0 = Todos, 1..9 = Gen 1..9, 10 = Especiais
	private int _scrollRow = 0;
	private const int VisibleRowCount = 7;
	private StarterOption _selectedStarter;
	private PokemonInstance _previewMon;
	private static readonly Dictionary<string, PokemonInstance> _previewCache = new(StringComparer.OrdinalIgnoreCase);

	private int _gamepadGridIndex = 0;
	private bool _gamepadFocusOnConfirm = false;

	private readonly List<ClickableComponent> _genTabs = new();
	private readonly List<ClickableComponent> _starterButtons = new();
	private ClickableComponent _confirmBtn;
	private ClickableComponent _scrollUpBtn;
	private ClickableComponent _scrollDownBtn;

	public PokemonStarterSelectMenu()
		: base(
			(Game1.uiViewport.Width - 940) / 2,
			(Game1.uiViewport.Height - 640) / 2,
			940,
			640,
			showUpperRightCloseButton: false)
	{
		_selectedStarter = AllStarters[0];
		CreatePreviewInstance();
		SetupLayout();
		Game1.playSound("bigSelect");
	}

	private void CreatePreviewInstance()
	{
		if (_selectedStarter == null) return;
		if (!_previewCache.TryGetValue(_selectedStarter.SpeciesName, out _previewMon))
		{
			PokemonSpecies sp = PokemonSpecies.Get(_selectedStarter.SpeciesName) ?? PokemonSpecies.Get("Pikachu");
			_previewMon = new PokemonInstance(sp, 5);
			_previewCache[_selectedStarter.SpeciesName] = _previewMon;
		}
	}

	private List<StarterOption> GetVisibleStarters()
	{
		List<StarterOption> visibleList = new();
		foreach (var opt in AllStarters)
		{
			if (_selectedGen == 0 || opt.Gen == _selectedGen)
			{
				visibleList.Add(opt);
			}
		}
		return visibleList;
	}

	private void SetupLayout()
	{
		_genTabs.Clear();
		_starterButtons.Clear();

		int leftW = 540;
		int tabY = yPositionOnScreen + 74;
		int tabW = 40;
		int tabH = 30;

		// 11 Tabs: Todos, G1 .. G9, Esp.
		_genTabs.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 20, tabY, 52, tabH), "0", "Todos"));
		for (int g = 1; g <= 9; g++)
		{
			int tx = xPositionOnScreen + 76 + (g - 1) * (tabW + 3);
			_genTabs.Add(new ClickableComponent(new Rectangle(tx, tabY, tabW, tabH), g.ToString(), $"G{g}"));
		}
		_genTabs.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 76 + 9 * (tabW + 3), tabY, 46, tabH), "10", "Esp."));

		// Grid of filtered starters (3 columns x VisibleRowCount)
		List<StarterOption> visibleList = GetVisibleStarters();
		int totalRows = (int)Math.Ceiling(visibleList.Count / 3.0);
		int maxScroll = Math.Max(0, totalRows - VisibleRowCount);
		_scrollRow = Math.Clamp(_scrollRow, 0, maxScroll);

		int gridX = xPositionOnScreen + 20;
		int gridY = tabY + 38;
		int colW = 164;
		int rowH = 50;

		int startIdx = _scrollRow * 3;
		int endIdx = Math.Min(visibleList.Count, startIdx + VisibleRowCount * 3);

		for (int i = startIdx; i < endIdx; i++)
		{
			var opt = visibleList[i];
			int slot = i - startIdx;
			int col = slot % 3;
			int row = slot / 3;
			int bx = gridX + col * (colW + 6);
			int by = gridY + row * (rowH + 6);
			_starterButtons.Add(new ClickableComponent(new Rectangle(bx, by, colW, rowH), opt.SpeciesName, opt.SpeciesName));
		}

		// Scroll Buttons
		if (maxScroll > 0)
		{
			int scrollBtnX = gridX + 3 * (colW + 6) + 4;
			int scrollTotalH = VisibleRowCount * (rowH + 6) - 6;
			_scrollUpBtn = new ClickableComponent(new Rectangle(scrollBtnX, gridY, 24, 32), "scroll_up", "▲");
			_scrollDownBtn = new ClickableComponent(new Rectangle(scrollBtnX, gridY + scrollTotalH - 32, 24, 32), "scroll_down", "▼");
		}
		else
		{
			_scrollUpBtn = null;
			_scrollDownBtn = null;
		}

		// Confirm Button on the right preview panel
		int rightX = xPositionOnScreen + leftW + 20;
		int rightW = width - leftW - 40;
		_confirmBtn = new ClickableComponent(new Rectangle(rightX, yPositionOnScreen + height - 64, rightW, 46), "confirm", "⭐ CONFIRMAR ESCOLHA");
	}

	public override void receiveScrollWheelAction(int direction)
	{
		base.receiveScrollWheelAction(direction);
		List<StarterOption> visibleList = GetVisibleStarters();
		int totalRows = (int)Math.Ceiling(visibleList.Count / 3.0);
		int maxScroll = Math.Max(0, totalRows - VisibleRowCount);
		if (direction > 0 && _scrollRow > 0)
		{
			_scrollRow--;
			SetupLayout();
			Game1.playSound("shiny");
		}
		else if (direction < 0 && _scrollRow < maxScroll)
		{
			_scrollRow++;
			SetupLayout();
			Game1.playSound("shiny");
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);

		// Scroll buttons
		if (_scrollUpBtn != null && _scrollUpBtn.containsPoint(x, y))
		{
			if (_scrollRow > 0)
			{
				_scrollRow--;
				SetupLayout();
				Game1.playSound("shiny");
			}
			return;
		}
		if (_scrollDownBtn != null && _scrollDownBtn.containsPoint(x, y))
		{
			List<StarterOption> visibleList = GetVisibleStarters();
			int totalRows = (int)Math.Ceiling(visibleList.Count / 3.0);
			int maxScroll = Math.Max(0, totalRows - VisibleRowCount);
			if (_scrollRow < maxScroll)
			{
				_scrollRow++;
				SetupLayout();
				Game1.playSound("shiny");
			}
			return;
		}

		// Gen tabs
		foreach (var tab in _genTabs)
		{
			if (tab.containsPoint(x, y))
			{
				if (int.TryParse(tab.name, out int g))
				{
					_selectedGen = g;
					_scrollRow = 0;
					Game1.playSound("shwip");
					SetupLayout();
					return;
				}
			}
		}

		// Starter choices
		foreach (var btn in _starterButtons)
		{
			if (btn.containsPoint(x, y))
			{
				var found = AllStarters.Find(s => s.SpeciesName.Equals(btn.name, StringComparison.OrdinalIgnoreCase));
				if (found != null && found != _selectedStarter)
				{
					_selectedStarter = found;
					CreatePreviewInstance();
					Game1.playSound("smallSelect");
				}
				return;
			}
		}

		// Confirm button
		if (_confirmBtn != null && _confirmBtn.containsPoint(x, y))
		{
			ConfirmChoice();
		}
	}

	public override bool areGamePadControlsImplemented() => true;

	public override bool readyToClose() => PlayerParty.GetForFarmer(Game1.player).HasChosenStarter;

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Enter || key == Keys.Space)
		{
			ConfirmChoice();
		}
	}

	public override void receiveGamePadButton(Buttons button)
	{
		base.receiveGamePadButton(button);

		List<StarterOption> visibleList = GetVisibleStarters();
		int totalRows = (int)Math.Ceiling(visibleList.Count / 3.0);
		int maxScroll = Math.Max(0, totalRows - VisibleRowCount);

		switch (button)
		{
			case Buttons.DPadLeft:
			case Buttons.LeftThumbstickLeft:
				if (_gamepadFocusOnConfirm)
				{
					_gamepadFocusOnConfirm = false;
					int currentRow = _gamepadGridIndex / 3;
					_gamepadGridIndex = Math.Min(_starterButtons.Count - 1, currentRow * 3 + 2);
					Game1.playSound("shiny");
				}
				else if (_gamepadGridIndex % 3 > 0)
				{
					_gamepadGridIndex--;
					UpdateHoverFromGamepad();
					Game1.playSound("shiny");
				}
				break;

			case Buttons.DPadRight:
			case Buttons.LeftThumbstickRight:
				if (!_gamepadFocusOnConfirm)
				{
					if (_gamepadGridIndex % 3 == 2 || _gamepadGridIndex == _starterButtons.Count - 1)
					{
						_gamepadFocusOnConfirm = true;
						Game1.playSound("shiny");
					}
					else if (_gamepadGridIndex + 1 < _starterButtons.Count)
					{
						_gamepadGridIndex++;
						UpdateHoverFromGamepad();
						Game1.playSound("shiny");
					}
				}
				break;

			case Buttons.DPadUp:
			case Buttons.LeftThumbstickUp:
				if (!_gamepadFocusOnConfirm)
				{
					if (_gamepadGridIndex >= 3)
					{
						_gamepadGridIndex -= 3;
						UpdateHoverFromGamepad();
						Game1.playSound("shiny");
					}
					else if (_scrollRow > 0)
					{
						_scrollRow--;
						SetupLayout();
						_gamepadGridIndex = Math.Min(_starterButtons.Count - 1, _gamepadGridIndex);
						UpdateHoverFromGamepad();
						Game1.playSound("shiny");
					}
				}
				break;

			case Buttons.DPadDown:
			case Buttons.LeftThumbstickDown:
				if (!_gamepadFocusOnConfirm)
				{
					if (_gamepadGridIndex + 3 < _starterButtons.Count)
					{
						_gamepadGridIndex += 3;
						UpdateHoverFromGamepad();
						Game1.playSound("shiny");
					}
					else if (_scrollRow < maxScroll)
					{
						_scrollRow++;
						SetupLayout();
						_gamepadGridIndex = Math.Min(_starterButtons.Count - 1, _gamepadGridIndex);
						UpdateHoverFromGamepad();
						Game1.playSound("shiny");
					}
					else
					{
						_gamepadFocusOnConfirm = true;
						Game1.playSound("shiny");
					}
				}
				break;

			case Buttons.LeftShoulder: // LB: Previous Gen Tab
				_selectedGen = (_selectedGen - 1 + 11) % 11;
				_scrollRow = 0;
				_gamepadGridIndex = 0;
				_gamepadFocusOnConfirm = false;
				SetupLayout();
				UpdateHoverFromGamepad();
				Game1.playSound("shwip");
				break;

			case Buttons.RightShoulder: // RB: Next Gen Tab
				_selectedGen = (_selectedGen + 1) % 11;
				_scrollRow = 0;
				_gamepadGridIndex = 0;
				_gamepadFocusOnConfirm = false;
				SetupLayout();
				UpdateHoverFromGamepad();
				Game1.playSound("shwip");
				break;

			case Buttons.A: // Select starter or confirm
				if (_gamepadFocusOnConfirm)
				{
					ConfirmChoice();
				}
				else if (_gamepadGridIndex >= 0 && _gamepadGridIndex < _starterButtons.Count)
				{
					var btn = _starterButtons[_gamepadGridIndex];
					var found = AllStarters.Find(s => s.SpeciesName.Equals(btn.name, StringComparison.OrdinalIgnoreCase));
					if (found != null)
					{
						if (_selectedStarter == found)
						{
							ConfirmChoice();
						}
						else
						{
							_selectedStarter = found;
							CreatePreviewInstance();
							Game1.playSound("smallSelect");
						}
					}
				}
				break;

			case Buttons.Start: // Start immediately confirms choice!
				ConfirmChoice();
				break;

			case Buttons.B:
				Game1.playSound("cancel");
				break;
		}
	}

	private void UpdateHoverFromGamepad()
	{
		if (_gamepadGridIndex >= 0 && _gamepadGridIndex < _starterButtons.Count)
		{
			var btn = _starterButtons[_gamepadGridIndex];
			var found = AllStarters.Find(s => s.SpeciesName.Equals(btn.name, StringComparison.OrdinalIgnoreCase));
			if (found != null && found != _selectedStarter)
			{
				_selectedStarter = found;
				CreatePreviewInstance();
			}
		}
	}

	private void ConfirmChoice()
	{
		if (_selectedStarter != null)
		{
			PlayerParty.GetForFarmer(Game1.player).ChooseStarter(_selectedStarter.SpeciesName);
			Game1.playSound("reward");
			Game1.exitActiveMenu();
		}
	}

	public override void draw(SpriteBatch b)
	{
		// Dark background vignette
		b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.75f);

		// Main Dialogue Box
		IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

		// Header Banner
		int headerH = 56;
		Rectangle headerRect = new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 14, width - 32, headerH);
		PokemonUI.DrawRoundedRect(b, headerRect, new Color(24, 32, 48), 6);
		PokemonUI.DrawCardBorder(b, headerRect, new Color(255, 215, 90), 2);
		PokemonUI.Text(b, Game1.dialogueFont, "ESCOLHA SEU POKÉMON INICIAL", new Rectangle(headerRect.X + 16, headerRect.Y + 6, headerRect.Width - 32, 28), new Color(255, 225, 100), center: true, maxScale: 0.82f);
		PokemonUI.Text(b, Game1.smallFont, "Escolha seu parceiro para explorar Pelican Town! Inclui Starter Kit: 5 Pokébolas + 1 Potion.", new Rectangle(headerRect.X + 16, headerRect.Y + 32, headerRect.Width - 32, 20), new Color(195, 215, 240), center: true, maxScale: 0.58f, shadow: false);

		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		// Draw Gen Tabs
		foreach (var tab in _genTabs)
		{
			bool isSel = (int.TryParse(tab.name, out int g) && g == _selectedGen);
			bool isHov = tab.containsPoint(mouseX, mouseY);
			Color tabBg = isSel ? new Color(215, 160, 45) : (isHov ? new Color(55, 75, 110) : new Color(32, 42, 60));
			PokemonUI.DrawRoundedRect(b, tab.bounds, tabBg, 4);
			PokemonUI.DrawCardBorder(b, tab.bounds, isSel ? Color.White : new Color(75, 100, 140), 1);
			PokemonUI.Text(b, Game1.smallFont, tab.label, tab.bounds, isSel ? Color.White : (isHov ? Color.Gold : Color.LightGray), center: true, maxScale: 0.62f, shadow: false);
		}

		// Draw Starters Grid
		foreach (var btn in _starterButtons)
		{
			bool isSel = (_selectedStarter != null && string.Equals(_selectedStarter.SpeciesName, btn.name, StringComparison.OrdinalIgnoreCase));
			bool isHov = btn.containsPoint(mouseX, mouseY);

			var opt = AllStarters.Find(s => s.SpeciesName.Equals(btn.name, StringComparison.OrdinalIgnoreCase));
			Color typeCol = opt != null ? PokemonTypeColors.Get(opt.PrimaryType) : new Color(60, 80, 110);

			Color cardBg = isSel ? new Color(48, 64, 94) : (isHov ? new Color(38, 50, 74) : new Color(24, 32, 46));
			Color cardBorder = isSel ? Color.Gold : (isHov ? Color.White : typeCol * 0.75f);

			PokemonUI.DrawRoundedRect(b, btn.bounds, cardBg, 5);
			PokemonUI.DrawCardBorder(b, btn.bounds, cardBorder, isSel ? 2 : 1);

			// Mini icon preview
			if (!_previewCache.TryGetValue(btn.name, out var previewItem))
			{
				PokemonSpecies sp = PokemonSpecies.Get(btn.name);
				if (sp != null)
				{
					previewItem = new PokemonInstance(sp, 5);
					_previewCache[btn.name] = previewItem;
				}
			}
			if (previewItem != null)
			{
				Texture2D icon = previewItem.GetIconSprite(Game1.graphics.GraphicsDevice) ?? previewItem.GetFrontSprite(Game1.graphics.GraphicsDevice);
				if (icon != null)
				{
					Rectangle srcRect = new Rectangle(0, 0, Math.Min(icon.Width, icon.Height), Math.Min(icon.Width, icon.Height));
					b.Draw(icon, new Rectangle(btn.bounds.X + 4, btn.bounds.Y + 3, 40, 40), srcRect, Color.White);
				}
			}

			// Name & Generation
			Color nameCol = isSel ? Color.Gold : (isHov ? Color.White : new Color(230, 240, 255));
			PokemonUI.Text(b, Game1.smallFont, btn.name, new Rectangle(btn.bounds.X + 46, btn.bounds.Y + 4, btn.bounds.Width - 48, 20), nameCol, center: false, maxScale: 0.65f, shadow: false);

			if (opt != null)
			{
				PokemonUI.DrawTypePill(b, opt.PrimaryType, btn.bounds.X + 46, btn.bounds.Y + 24, 52, 16);
				if (opt.SecondaryType.HasValue)
				{
					PokemonUI.DrawTypePill(b, opt.SecondaryType.Value, btn.bounds.X + 102, btn.bounds.Y + 24, 52, 16);
				}
			}
		}

		// Draw Scroll Buttons
		if (_scrollUpBtn != null)
		{
			bool upHov = _scrollUpBtn.containsPoint(mouseX, mouseY);
			PokemonUI.DrawRoundedRect(b, _scrollUpBtn.bounds, upHov ? new Color(60, 80, 120) : new Color(30, 40, 60), 4);
			PokemonUI.DrawCardBorder(b, _scrollUpBtn.bounds, upHov ? Color.Gold : new Color(80, 100, 140), 1);
			PokemonUI.Text(b, Game1.smallFont, "▲", _scrollUpBtn.bounds, upHov ? Color.White : Color.LightGray, center: true, maxScale: 0.65f, shadow: false);
		}
		if (_scrollDownBtn != null)
		{
			bool downHov = _scrollDownBtn.containsPoint(mouseX, mouseY);
			PokemonUI.DrawRoundedRect(b, _scrollDownBtn.bounds, downHov ? new Color(60, 80, 120) : new Color(30, 40, 60), 4);
			PokemonUI.DrawCardBorder(b, _scrollDownBtn.bounds, downHov ? Color.Gold : new Color(80, 100, 140), 1);
			PokemonUI.Text(b, Game1.smallFont, "▼", _scrollDownBtn.bounds, downHov ? Color.White : Color.LightGray, center: true, maxScale: 0.65f, shadow: false);
		}

		// Right Preview Panel
		int leftW = 540;
		int rightX = xPositionOnScreen + leftW + 20;
		int rightW = width - leftW - 40;
		int rightH = height - 160;
		Rectangle previewBox = new Rectangle(rightX, yPositionOnScreen + 74, rightW, rightH);

		PokemonUI.DrawRoundedRect(b, previewBox, new Color(26, 34, 50), 8);
		PokemonUI.DrawCardBorder(b, previewBox, new Color(70, 95, 140), 2);

		if (_selectedStarter != null && _previewMon != null)
		{
			// Title & Gen Badge
			PokemonUI.Text(b, Game1.dialogueFont, _selectedStarter.SpeciesName, new Rectangle(previewBox.X + 12, previewBox.Y + 8, previewBox.Width - 24, 28), Color.Gold, center: true, maxScale: 0.78f);
			PokemonUI.Text(b, Game1.smallFont, $"Geração {_selectedStarter.Gen}  •  Nível 5", new Rectangle(previewBox.X + 12, previewBox.Y + 34, previewBox.Width - 24, 20), new Color(185, 215, 255), center: true, maxScale: 0.58f, shadow: false);

			// Big animated front sprite
			Texture2D frontTex = _previewMon.GetFrontSprite(Game1.graphics.GraphicsDevice);
			if (frontTex != null)
			{
				Rectangle srcRect = _previewMon.GetFrontSourceRect();
				int spriteCenterX = previewBox.X + previewBox.Width / 2;
				int spriteCenterY = previewBox.Y + 118;
				float bounce = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 4.0) * 3f;
				Vector2 origin = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
				b.Draw(frontTex, new Vector2(spriteCenterX, spriteCenterY + bounce), srcRect, Color.White, 0f, origin, 2.4f, SpriteEffects.None, 0.88f);
			}

			// Type Pills below sprite
			int typePillW = 66;
			int typePillH = 20;
			int pillsY = previewBox.Y + 172;
			if (_selectedStarter.SecondaryType.HasValue)
			{
				int startPillsX = previewBox.X + (previewBox.Width - (typePillW * 2 + 8)) / 2;
				PokemonUI.DrawTypePill(b, _selectedStarter.PrimaryType, startPillsX, pillsY, typePillW, typePillH);
				PokemonUI.DrawTypePill(b, _selectedStarter.SecondaryType.Value, startPillsX + typePillW + 8, pillsY, typePillW, typePillH);
			}
			else
			{
				int startPillsX = previewBox.X + (previewBox.Width - typePillW) / 2;
				PokemonUI.DrawTypePill(b, _selectedStarter.PrimaryType, startPillsX, pillsY, typePillW, typePillH);
			}

			// Ability
			string abName = !string.IsNullOrEmpty(_previewMon.Ability) ? PokemonAbility.GetDisplayName(_previewMon.Ability) : "Elemental";
			PokemonUI.Text(b, Game1.smallFont, $"Habilidade: {abName}", new Rectangle(previewBox.X + 12, pillsY + 26, previewBox.Width - 24, 20), new Color(125, 230, 255), center: true, maxScale: 0.58f, shadow: false);

			// Base Stats Box
			Rectangle statsBox = new Rectangle(previewBox.X + 14, pillsY + 50, previewBox.Width - 28, 100);
			PokemonUI.DrawRoundedRect(b, statsBox, new Color(18, 24, 36), 6);
			PokemonUI.DrawCardBorder(b, statsBox, new Color(50, 70, 105), 1);

			(string sLabel, int sVal)[] sStats = new[]
			{
				("HP", _previewMon.MaxHP),
				("Ataque", _previewMon.Attack),
				("Defesa", _previewMon.Defense),
				("Atq. Esp.", _previewMon.SpAttack),
				("Def. Esp.", _previewMon.SpDefense),
				("Velocidade", _previewMon.Speed)
			};

			int sRowY = statsBox.Y + 6;
			for (int i = 0; i < sStats.Length; i++)
			{
				int col = i % 2;
				int row = i / 2;
				int sx = statsBox.X + 10 + col * 148;
				int sy = sRowY + row * 28;
				PokemonUI.Text(b, Game1.smallFont, $"{sStats[i].sLabel}:", new Rectangle(sx, sy, 84, 20), new Color(205, 220, 240), center: false, maxScale: 0.54f, shadow: false);
				PokemonUI.Text(b, Game1.smallFont, sStats[i].sVal.ToString(), new Rectangle(sx + 86, sy, 40, 20), Color.Gold, center: false, maxScale: 0.56f, shadow: false);
			}

			// Initial Moves
			string movesList = "";
			for (int m = 0; m < _previewMon.Moves.Count; m++)
			{
				if (m > 0) movesList += "  •  ";
				movesList += _previewMon.Moves[m].Name;
			}
			PokemonUI.Text(b, Game1.smallFont, $"Golpes: {movesList}", new Rectangle(previewBox.X + 12, statsBox.Bottom + 6, previewBox.Width - 24, 20), new Color(145, 245, 175), center: true, maxScale: 0.52f, shadow: false);
		}

		// Draw Confirm Button
		if (_confirmBtn != null)
		{
			bool confHov = _confirmBtn.containsPoint(mouseX, mouseY);
			Color cTint = confHov ? new Color(255, 235, 175) : Color.White;
			IClickableMenu.drawTextureBox(b, _confirmBtn.bounds.X, _confirmBtn.bounds.Y, _confirmBtn.bounds.Width, _confirmBtn.bounds.Height, cTint);
			PokemonUI.Text(b, Game1.dialogueFont, _confirmBtn.label, _confirmBtn.bounds, confHov ? Color.DarkRed : Color.DarkGreen, center: true, maxScale: 0.72f);
		}

		// Draw Gamepad Cursor outline
		if (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected)
		{
			float pulse = 0.85f + (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 6.0) * 0.15f;
			if (_gamepadFocusOnConfirm && _confirmBtn != null)
			{
				Rectangle focusRect = new Rectangle(_confirmBtn.bounds.X - 4, _confirmBtn.bounds.Y - 4, _confirmBtn.bounds.Width + 8, _confirmBtn.bounds.Height + 8);
				PokemonUI.DrawCardBorder(b, focusRect, Color.Gold * pulse, 3);
			}
			else if (_gamepadGridIndex >= 0 && _gamepadGridIndex < _starterButtons.Count)
			{
				Rectangle focusRect = new Rectangle(_starterButtons[_gamepadGridIndex].bounds.X - 3, _starterButtons[_gamepadGridIndex].bounds.Y - 3, _starterButtons[_gamepadGridIndex].bounds.Width + 6, _starterButtons[_gamepadGridIndex].bounds.Height + 6);
				PokemonUI.DrawCardBorder(b, focusRect, Color.Gold * pulse, 3);
			}
		}

		drawMouse(b);
	}
}
