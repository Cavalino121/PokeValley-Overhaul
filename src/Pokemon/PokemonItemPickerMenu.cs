using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

/// <summary>
/// Dedicated Item Picker Menu for Mega Stones, Z-Crystals, and Held Items.
/// Optimized with zero draw-thread disk reads, cached icons, and pixel-fitted text.
/// </summary>
public class PokemonItemPickerMenu : IClickableMenu
{
	public enum PickerMode
	{
		MegaStones,
		ZCrystals,
		HeldItems
	}

	public class ItemEntry
	{
		public string ItemId { get; set; }
		public string DisplayName { get; set; }
		public string Subtitle { get; set; }
		public Color SubtitleColor { get; set; }
	}

	private readonly IClickableMenu _parentMenu;
	private PickerMode _mode;
	private readonly List<ItemEntry> _items = new();
	private readonly List<ClickableComponent> _itemButtons = new();
	private readonly List<ClickableComponent> _modeButtons = new();

	private ClickableComponent _prevPageBtn;
	private ClickableComponent _nextPageBtn;
	private ClickableComponent _giveAllCurrentBtn;
	private ClickableComponent _backBtn;

	private int _currentPage = 0;
	private const int ItemsPerPage = 18; // 3 columns x 6 rows

	// Cache textures specifically for item picker to prevent lag
	private static readonly Dictionary<string, (Texture2D tex, Rectangle rect)> _pickerIconCache = new(StringComparer.OrdinalIgnoreCase);

	public PokemonItemPickerMenu(IClickableMenu parentMenu, PickerMode initialMode = PickerMode.MegaStones)
		: base(
			(Game1.uiViewport.Width - 920) / 2,
			(Game1.uiViewport.Height - 640) / 2,
			920,
			640,
			showUpperRightCloseButton: true)
	{
		_parentMenu = parentMenu;
		_mode = initialMode;
		RefreshItems();
		SetupButtons();
		Game1.playSound("bigSelect");
	}

	private void RefreshItems()
	{
		_items.Clear();
		if (_mode == PickerMode.MegaStones)
		{
			foreach (var kv in PokemonItemManager.MegaStoneDefinitions)
			{
				string megaTarget = kv.Value.TargetMegaInternal;
				string subtitle = "Mega Evolução";
				if (!string.IsNullOrEmpty(megaTarget))
				{
					int slash = megaTarget.IndexOf('/');
					string monName = slash > 0 ? megaTarget.Substring(0, slash) : megaTarget;
					if (monName.Length > 0)
					{
						monName = char.ToUpperInvariant(monName[0]) + monName.Substring(1);
					}
					subtitle = "Mega " + monName;
				}

				_items.Add(new ItemEntry
				{
					ItemId = kv.Key,
					DisplayName = kv.Value.DisplayName,
					Subtitle = subtitle,
					SubtitleColor = new Color(130, 40, 150)
				});
			}
		}
		else if (_mode == PickerMode.ZCrystals)
		{
			foreach (var kv in PokemonItemManager.ZCrystalDefinitions)
			{
				_items.Add(new ItemEntry
				{
					ItemId = kv.Key,
					DisplayName = kv.Value.DisplayName,
					Subtitle = "Tipo: " + kv.Value.Type.ToString(),
					SubtitleColor = PokemonTypeColors.Get(kv.Value.Type)
				});
			}
			foreach (var kv in PokemonItemManager.SignatureZCrystalDefinitions)
			{
				string monName = char.ToUpperInvariant(kv.Value.RequiredSpecies[0]) + kv.Value.RequiredSpecies.Substring(1);
				_items.Add(new ItemEntry
				{
					ItemId = kv.Key,
					DisplayName = kv.Value.DisplayName,
					Subtitle = $"Assinatura: {monName} ({kv.Value.ZMoveName})",
					SubtitleColor = PokemonTypeColors.Get(kv.Value.Type)
				});
			}
		}
		else // HeldItems
		{
			foreach (var kv in PokemonHeldItemManager.AllHeldItems)
			{
				string subtitle = kv.Value.BoostedType.HasValue ? $"Bônus: {kv.Value.BoostedType}" : (kv.Value.IsConsumable ? "Descartável" : "Competitivo");
				Color subColor = kv.Value.BoostedType.HasValue ? PokemonTypeColors.Get(kv.Value.BoostedType.Value) : new Color(40, 110, 180);

				_items.Add(new ItemEntry
				{
					ItemId = kv.Key,
					DisplayName = kv.Value.DisplayName,
					Subtitle = subtitle,
					SubtitleColor = subColor
				});
			}
		}

		// Pre-cache icons for items on first page without blocking
		foreach (var entry in _items)
		{
			if (!_pickerIconCache.ContainsKey(entry.ItemId))
			{
				if (PokemonItemManager.TryGetCustomTexture(entry.ItemId, out Texture2D tex, out Rectangle rect) && tex != null)
				{
					_pickerIconCache[entry.ItemId] = (tex, rect);
				}
				else
				{
					_pickerIconCache[entry.ItemId] = (null, Rectangle.Empty);
				}
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

		_modeButtons.Clear();
		int tabStartX = xPositionOnScreen + 32;
		int tabY = yPositionOnScreen + 44;
		int tabW = 180;
		int tabH = 36;

		int megaCount = PokemonItemManager.MegaStoneDefinitions.Count;
		int zCount = PokemonItemManager.ZCrystalDefinitions.Count;
		int heldCount = PokemonHeldItemManager.AllHeldItems.Count;

		_modeButtons.Add(new ClickableComponent(new Rectangle(tabStartX, tabY, tabW, tabH), "MegaStones", $"🧬 Megas ({megaCount})"));
		_modeButtons.Add(new ClickableComponent(new Rectangle(tabStartX + tabW + 10, tabY, tabW, tabH), "ZCrystals", $"💎 Z-Moves ({zCount})"));
		_modeButtons.Add(new ClickableComponent(new Rectangle(tabStartX + (tabW + 10) * 2, tabY, tabW, tabH), "HeldItems", $"🎒 Held ({heldCount})"));

		_itemButtons.Clear();
		int maxPages = Math.Max(0, (_items.Count - 1) / ItemsPerPage);
		if (_currentPage > maxPages) _currentPage = maxPages;
		if (_currentPage < 0) _currentPage = 0;

		int startIndex = _currentPage * ItemsPerPage;
		int countOnPage = Math.Min(ItemsPerPage, _items.Count - startIndex);

		int gridStartX = xPositionOnScreen + 32;
		int gridStartY = tabY + 48;
		int cardW = 276;
		int cardH = 60;
		int gapX = 14;
		int gapY = 8;

		for (int i = 0; i < countOnPage; i++)
		{
			var item = _items[startIndex + i];
			int col = i % 3;
			int row = i / 3;
			int bx = gridStartX + col * (cardW + gapX);
			int by = gridStartY + row * (cardH + gapY);

			_itemButtons.Add(new ClickableComponent(new Rectangle(bx, by, cardW, cardH), item.ItemId, item.DisplayName));
		}

		// Bottom controls
		int bottomY = yPositionOnScreen + height - 58;
		_prevPageBtn = new ClickableComponent(new Rectangle(gridStartX, bottomY, 110, 36), "prev", "◀ Anterior");
		_nextPageBtn = new ClickableComponent(new Rectangle(gridStartX + 120, bottomY, 110, 36), "next", "Próximo ▶");

		string giveAllLabel = _mode switch
		{
			PickerMode.MegaStones => "📦 +1 Todas as Megas",
			PickerMode.ZCrystals => "📦 +1 Todos os Z-Crystals",
			_ => "📦 +1 Todos os Held Items"
		};
		_giveAllCurrentBtn = new ClickableComponent(new Rectangle(gridStartX + 250, bottomY, 260, 36), "give_all", giveAllLabel);

		_backBtn = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 210, bottomY, 178, 36), "back", "↩ Voltar ao Debug");
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (upperRightCloseButton != null && upperRightCloseButton.containsPoint(x, y))
		{
			CloseMenu();
			return;
		}

		if (_backBtn != null && _backBtn.containsPoint(x, y))
		{
			CloseMenu();
			return;
		}

		foreach (var mb in _modeButtons)
		{
			if (mb.containsPoint(x, y))
			{
				PickerMode newMode = Enum.Parse<PickerMode>(mb.name);
				if (_mode != newMode)
				{
					_mode = newMode;
					_currentPage = 0;
					RefreshItems();
					SetupButtons();
					Game1.playSound("shwip");
				}
				return;
			}
		}

		if (_prevPageBtn != null && _prevPageBtn.containsPoint(x, y))
		{
			int maxPages = Math.Max(0, (_items.Count - 1) / ItemsPerPage);
			_currentPage = (_currentPage - 1 + (maxPages + 1)) % (maxPages + 1);
			SetupButtons();
			Game1.playSound("shwip");
			return;
		}

		if (_nextPageBtn != null && _nextPageBtn.containsPoint(x, y))
		{
			int maxPages = Math.Max(0, (_items.Count - 1) / ItemsPerPage);
			_currentPage = (_currentPage + 1) % (maxPages + 1);
			SetupButtons();
			Game1.playSound("shwip");
			return;
		}

		if (_giveAllCurrentBtn != null && _giveAllCurrentBtn.containsPoint(x, y))
		{
			int added = 0;
			foreach (var it in _items)
			{
				string fullId = "(O)" + it.ItemId;
				if (Game1.player != null && !Game1.player.Items.ContainsId(fullId))
				{
					Game1.player.addItemToInventoryBool(ItemRegistry.Create(fullId));
					added++;
				}
			}
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage($"✨ +{added} itens adicionados ao inventário!", 2));
			return;
		}

		// Click on specific item card: gives 1 to player
		for (int i = 0; i < _itemButtons.Count; i++)
		{
			var btn = _itemButtons[i];
			if (btn.containsPoint(x, y))
			{
				int startIndex = _currentPage * ItemsPerPage;
				if (startIndex + i < _items.Count)
				{
					var entry = _items[startIndex + i];
					string fullId = "(O)" + entry.ItemId;
					Item item = ItemRegistry.Create(fullId);
					if (item != null && Game1.player != null)
					{
						if (Game1.player.addItemToInventoryBool(item))
						{
							Game1.playSound("coin");
							Game1.addHUDMessage(new HUDMessage($"✨ Obteve {entry.DisplayName}!", 2));
						}
						else
						{
							Game1.playSound("cancel");
							Game1.addHUDMessage(new HUDMessage("Inventário cheio!", 3));
						}
					}
					else
					{
						Game1.playSound("cancel");
					}
				}
				return;
			}
		}

		base.receiveLeftClick(x, y, playSound);
	}

	public override void receiveScrollWheelAction(int direction)
	{
		int maxPages = Math.Max(0, (_items.Count - 1) / ItemsPerPage);
		if (direction > 0 && _currentPage > 0)
		{
			_currentPage--;
			SetupButtons();
			Game1.playSound("shwip");
		}
		else if (direction < 0 && _currentPage < maxPages)
		{
			_currentPage++;
			SetupButtons();
			Game1.playSound("shwip");
		}
		base.receiveScrollWheelAction(direction);
	}

	private int _gamepadIndex = 0;

	public override bool areGamePadControlsImplemented() => true;

	public override void receiveGamePadButton(Buttons button)
	{
		if (button == Buttons.B || button == Buttons.Back)
		{
			CloseMenu();
			return;
		}
		if (button == Buttons.LeftShoulder)
		{
			_mode = (PickerMode)(((int)_mode + 2) % 3);
			_currentPage = 0;
			_gamepadIndex = 0;
			RefreshItems();
			SetupButtons();
			Game1.playSound("shwip");
			return;
		}
		if (button == Buttons.RightShoulder)
		{
			_mode = (PickerMode)(((int)_mode + 1) % 3);
			_currentPage = 0;
			_gamepadIndex = 0;
			RefreshItems();
			SetupButtons();
			Game1.playSound("shwip");
			return;
		}
		int maxPages = Math.Max(0, (_items.Count - 1) / ItemsPerPage);
		if (button == Buttons.LeftTrigger)
		{
			_currentPage = (_currentPage - 1 + (maxPages + 1)) % (maxPages + 1);
			SetupButtons();
			_gamepadIndex = Math.Clamp(_gamepadIndex, 0, Math.Max(0, _itemButtons.Count - 1));
			Game1.playSound("shwip");
			return;
		}
		if (button == Buttons.RightTrigger)
		{
			_currentPage = (_currentPage + 1) % (maxPages + 1);
			SetupButtons();
			_gamepadIndex = Math.Clamp(_gamepadIndex, 0, Math.Max(0, _itemButtons.Count - 1));
			Game1.playSound("shwip");
			return;
		}
		if (button == Buttons.X)
		{
			if (_giveAllCurrentBtn != null)
			{
				receiveLeftClick(_giveAllCurrentBtn.bounds.Center.X, _giveAllCurrentBtn.bounds.Center.Y);
			}
			return;
		}
		if (_itemButtons.Count == 0) return;

		if (button == Buttons.DPadLeft || button == Buttons.LeftThumbstickLeft)
		{
			if (_gamepadIndex > 0) { _gamepadIndex--; Game1.playSound("shiny"); }
		}
		else if (button == Buttons.DPadRight || button == Buttons.LeftThumbstickRight)
		{
			if (_gamepadIndex + 1 < _itemButtons.Count) { _gamepadIndex++; Game1.playSound("shiny"); }
		}
		else if (button == Buttons.DPadUp || button == Buttons.LeftThumbstickUp)
		{
			if (_gamepadIndex >= 3) { _gamepadIndex -= 3; Game1.playSound("shiny"); }
		}
		else if (button == Buttons.DPadDown || button == Buttons.LeftThumbstickDown)
		{
			if (_gamepadIndex + 3 < _itemButtons.Count) { _gamepadIndex += 3; Game1.playSound("shiny"); }
		}
		else if (button == Buttons.A || button == Buttons.Start)
		{
			if (_gamepadIndex >= 0 && _gamepadIndex < _itemButtons.Count)
			{
				var b = _itemButtons[_gamepadIndex].bounds;
				receiveLeftClick(b.Center.X, b.Center.Y);
			}
		}
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape)
		{
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
		}
		else
		{
			Game1.exitActiveMenu();
		}
	}

	public override void draw(SpriteBatch b)
	{
		// Shaded background
		b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.65f);

		// Menu window frame
		drawTextureBox(
			b,
			Game1.menuTexture,
			new Rectangle(0, 256, 60, 60),
			xPositionOnScreen,
			yPositionOnScreen,
			width,
			height,
			Color.White,
			1f,
			drawShadow: true);

		// Header Title
		string title = _mode switch
		{
			PickerMode.MegaStones => "🧬 Seletor de Mega Pedras (Mega Stones)",
			PickerMode.ZCrystals => "💎 Seletor de Cristais Z (Z-Crystals)",
			_ => "🎒 Seletor de Itens Segurados (Held Items)"
		};
		Utility.drawTextWithShadow(
			b,
			title,
			Game1.dialogueFont,
			new Vector2(xPositionOnScreen + 32, yPositionOnScreen + 10),
			Game1.textColor,
			0.90f);

		// Mode buttons (Tabs)
		foreach (var mb in _modeButtons)
		{
			bool isSelected = (_mode == PickerMode.MegaStones && mb.name == "MegaStones") ||
			                  (_mode == PickerMode.ZCrystals && mb.name == "ZCrystals") ||
			                  (_mode == PickerMode.HeldItems && mb.name == "HeldItems");
			bool hovered = mb.containsPoint(Game1.getMouseX(), Game1.getMouseY());

			Color boxColor = isSelected ? new Color(255, 235, 170) : (hovered ? Color.Wheat : Color.White * 0.85f);
			drawTextureBox(
				b,
				Game1.menuTexture,
				new Rectangle(0, 256, 60, 60),
				mb.bounds.X,
				mb.bounds.Y,
				mb.bounds.Width,
				mb.bounds.Height,
				boxColor,
				1f,
				drawShadow: false);

			PokemonUI.Text(b, Game1.smallFont, mb.label, mb.bounds, isSelected ? Color.DarkBlue : Game1.textColor, center: true, maxScale: 0.68f);
		}

		// Items Grid
		int startIndex = _currentPage * ItemsPerPage;
		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		for (int i = 0; i < _itemButtons.Count; i++)
		{
			var btn = _itemButtons[i];
			var entry = _items[startIndex + i];
			bool isGamepadFocus = (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected) && _gamepadIndex == i;
			bool hovered = btn.containsPoint(mouseX, mouseY) || isGamepadFocus;

			Color cardBg = hovered ? new Color(255, 245, 205) : new Color(245, 236, 215);
			drawTextureBox(
				b,
				Game1.menuTexture,
				new Rectangle(0, 256, 60, 60),
				btn.bounds.X,
				btn.bounds.Y,
				btn.bounds.Width,
				btn.bounds.Height,
				cardBg,
				1f,
				drawShadow: false);

			if (hovered)
			{
				drawTextureBox(
					b,
					Game1.menuTexture,
					new Rectangle(0, 256, 60, 60),
					btn.bounds.X - 2,
					btn.bounds.Y - 2,
					btn.bounds.Width + 4,
					btn.bounds.Height + 4,
					Color.Gold * 0.7f,
					1f,
					drawShadow: false);
			}

			// Draw Item Icon
			int iconBoxX = btn.bounds.X + 8;
			int iconBoxY = btn.bounds.Y + 9;
			int iconBoxSize = 42;

			b.Draw(
				Game1.staminaRect,
				new Rectangle(iconBoxX, iconBoxY, iconBoxSize, iconBoxSize),
				Color.Black * 0.08f);

			if (_pickerIconCache.TryGetValue(entry.ItemId, out var cached) && cached.tex != null && !cached.tex.IsDisposed)
			{
				float scale = Math.Min(34f / cached.rect.Width, 34f / cached.rect.Height);
				Vector2 iconPos = new Vector2(
					iconBoxX + (iconBoxSize - cached.rect.Width * scale) / 2f,
					iconBoxY + (iconBoxSize - cached.rect.Height * scale) / 2f);
				b.Draw(cached.tex, iconPos, cached.rect, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.88f);
			}
			else
			{
				Texture2D genericBall = PokemonItemManager.GetItemBallTexture();
				if (genericBall != null)
				{
					b.Draw(genericBall, new Vector2(iconBoxX + 9, iconBoxY + 9), Color.White);
				}
			}

			// Text Information with auto-fitted bounds to guarantee zero overflowing or overlapping
			int textX = iconBoxX + iconBoxSize + 8;
			int textW = btn.bounds.Width - iconBoxSize - 18;

			// Display Name
			PokemonUI.Text(b, Game1.smallFont, entry.DisplayName, new Rectangle(textX, btn.bounds.Y + 6, textW, 26), Color.DarkBlue, maxScale: 0.62f, shadow: false);

			// Subtitle / Compatible mon or type
			PokemonUI.Text(b, Game1.tinyFont, entry.Subtitle, new Rectangle(textX, btn.bounds.Y + 32, textW, 20), entry.SubtitleColor, maxScale: 0.65f, shadow: false);
		}

		// Page Controls
		if (_prevPageBtn != null)
		{
			bool hovered = _prevPageBtn.containsPoint(mouseX, mouseY);
			drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), _prevPageBtn.bounds.X, _prevPageBtn.bounds.Y, _prevPageBtn.bounds.Width, _prevPageBtn.bounds.Height, hovered ? Color.Wheat : Color.White, 1f, drawShadow: false);
			PokemonUI.Text(b, Game1.smallFont, _prevPageBtn.label, _prevPageBtn.bounds, Color.Black, center: true, maxScale: 0.70f);
		}

		int maxPages = Math.Max(0, (_items.Count - 1) / ItemsPerPage);
		string pageStr = $"Página {_currentPage + 1}/{maxPages + 1}";
		Vector2 pageSz = Game1.smallFont.MeasureString(pageStr) * 0.75f;
		b.DrawString(Game1.smallFont, pageStr, new Vector2(xPositionOnScreen + 160, yPositionOnScreen + height - 48), Color.DarkSlateGray, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0.88f);

		if (_nextPageBtn != null)
		{
			bool hovered = _nextPageBtn.containsPoint(mouseX, mouseY);
			drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), _nextPageBtn.bounds.X, _nextPageBtn.bounds.Y, _nextPageBtn.bounds.Width, _nextPageBtn.bounds.Height, hovered ? Color.Wheat : Color.White, 1f, drawShadow: false);
			PokemonUI.Text(b, Game1.smallFont, _nextPageBtn.label, _nextPageBtn.bounds, Color.Black, center: true, maxScale: 0.70f);
		}

		// Give all current category button
		if (_giveAllCurrentBtn != null)
		{
			bool hovered = _giveAllCurrentBtn.containsPoint(mouseX, mouseY);
			drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), _giveAllCurrentBtn.bounds.X, _giveAllCurrentBtn.bounds.Y, _giveAllCurrentBtn.bounds.Width, _giveAllCurrentBtn.bounds.Height, hovered ? new Color(255, 235, 170) : new Color(230, 245, 220), 1f, drawShadow: false);
			PokemonUI.Text(b, Game1.smallFont, _giveAllCurrentBtn.label, _giveAllCurrentBtn.bounds, Color.DarkGreen, center: true, maxScale: 0.68f);
		}

		// Back Button
		if (_backBtn != null)
		{
			bool hovered = _backBtn.containsPoint(mouseX, mouseY);
			drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), _backBtn.bounds.X, _backBtn.bounds.Y, _backBtn.bounds.Width, _backBtn.bounds.Height, hovered ? Color.Wheat : Color.White, 1f, drawShadow: false);
			PokemonUI.Text(b, Game1.smallFont, _backBtn.label, _backBtn.bounds, Color.DarkRed, center: true, maxScale: 0.70f);
		}

		upperRightCloseButton?.draw(b);
		drawMouse(b);
	}
}
