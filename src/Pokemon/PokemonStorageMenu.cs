using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

/// <summary>
/// Classic GBA/NDS style PC Pokémon Storage Menu.
/// Allows depositing, withdrawing, swapping, and inspecting Pokémon across 16 Boxes.
/// </summary>
public class PokemonStorageMenu : IClickableMenu
{
	private const int MenuWidth = 1040;
	private const int MenuHeight = 680;

	private readonly ClickableTextureComponent _closeButton;
	private readonly ClickableComponent _prevBoxBtn;
	private readonly ClickableComponent _nextBoxBtn;
	private readonly ClickableComponent _jumpBoxBtn;

	// Grid layout
	private readonly Rectangle _partyArea;
	private readonly Rectangle _boxArea;
	private readonly Rectangle _previewArea;

	// Slots
	private readonly List<ClickableComponent> _partySlots = new();
	private readonly List<ClickableComponent> _boxSlots = new();

	// Selection & Context Menu
	private int _hoverPartyIndex = -1;
	private int _hoverBoxIndex = -1;
	private int _selectedPartyIndex = -1;
	private int _selectedBoxIndex = -1;

	// Gamepad Navigation
	private enum StorageFocusArea
	{
		Box,
		Party,
		ActionMenu
	}
	private StorageFocusArea _focusArea = StorageFocusArea.Box;
	private int _gamepadBoxIndex = 0;
	private int _gamepadPartyIndex = 0;
	private int _gamepadActionIndex = 0;

	private readonly Farmer _farmer;
	private PokemonStorage Storage => PokemonStorage.GetForFarmer(_farmer ?? Game1.player);
	private PlayerParty Party => PlayerParty.GetForFarmer(_farmer ?? Game1.player);

	// Action popup for selected mon
	private bool _showActionMenu = false;
	private Vector2 _actionMenuPos = Vector2.Zero;
	private readonly List<ClickableComponent> _actionButtons = new();
	private bool _actionFromParty = false;
	private int _actionTargetSlot = -1;

	// Authentic GBA Box Wallpapers (pastel tones)
	private static readonly Color[] BoxWallpapers = new Color[]
	{
		new Color(216, 236, 224), // Forest Green
		new Color(236, 220, 216), // Mountain Rust
		new Color(216, 228, 244), // Sea Blue
		new Color(244, 236, 212), // Desert Sand
		new Color(236, 216, 240), // Orchid Pink
		new Color(228, 228, 236), // Sky Grey
		new Color(244, 224, 216), // Volcanic Peach
		new Color(220, 240, 236)  // Crystal Teal
	};

	public PokemonStorageMenu(Farmer farmer = null)
		: base((Game1.uiViewport.Width - MenuWidth) / 2, (Game1.uiViewport.Height - MenuHeight) / 2, MenuWidth, MenuHeight, showUpperRightCloseButton: true)
	{
		_farmer = farmer ?? Game1.player;
		Game1.playSound("bigSelect");

		_closeButton = new ClickableTextureComponent(
			new Rectangle(xPositionOnScreen + width - 42, yPositionOnScreen + 10, 32, 32),
			Game1.mouseCursors,
			new Rectangle(337, 494, 12, 12),
			2.6f
		);

		int headerY = yPositionOnScreen + 16;
		_prevBoxBtn = new ClickableComponent(new Rectangle(xPositionOnScreen + 380, headerY, 40, 36), "prev_box", "<");
		_nextBoxBtn = new ClickableComponent(new Rectangle(xPositionOnScreen + 640, headerY, 40, 36), "next_box", ">");
		_jumpBoxBtn = new ClickableComponent(new Rectangle(xPositionOnScreen + 424, headerY, 212, 36), "box_title", "");

		// Left: Party column
		_partyArea = new Rectangle(xPositionOnScreen + 24, yPositionOnScreen + 64, 320, height - 88);
		// Center-Right: Box Grid (Width 570px to contain 6 cols * (86+8) = 556px)
		_boxArea = new Rectangle(xPositionOnScreen + 360, yPositionOnScreen + 64, width - 384, 380);
		// Bottom-Right: Summary / Preview Area
		_previewArea = new Rectangle(xPositionOnScreen + 360, yPositionOnScreen + 456, width - 384, height - 480);

		SetupSlots();
	}

	private void SetupSlots()
	{
		_partySlots.Clear();
		int slotH = 72;
		int gap = 8;
		for (int i = 0; i < 6; i++)
		{
			int sy = _partyArea.Y + 36 + i * (slotH + gap);
			_partySlots.Add(new ClickableComponent(new Rectangle(_partyArea.X + 8, sy, _partyArea.Width - 16, slotH), i.ToString()));
		}

		_boxSlots.Clear();
		int cols = PokemonStorage.BoxCols; // 6
		int rows = PokemonStorage.BoxRows; // 5
		int slotW = 86;
		int slotHBox = 64;
		int padX = 7;
		int padY = 6;

		for (int r = 0; r < rows; r++)
		{
			for (int c = 0; c < cols; c++)
			{
				int idx = r * cols + c;
				int bx = _boxArea.X + 10 + c * (slotW + padX);
				int by = _boxArea.Y + 16 + r * (slotHBox + padY);
				_boxSlots.Add(new ClickableComponent(new Rectangle(bx, by, slotW, slotHBox), idx.ToString()));
			}
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);

		if (_closeButton.containsPoint(x, y))
		{
			Game1.playSound("bigDeSelect");
			exitThisMenu();
			return;
		}

		// Action menu active
		if (_showActionMenu)
		{
			for (int i = 0; i < _actionButtons.Count; i++)
			{
				if (_actionButtons[i].containsPoint(x, y))
				{
					HandleActionSelection(_actionButtons[i].name);
					_showActionMenu = false;
					return;
				}
			}
			// Click outside closes action menu
			_showActionMenu = false;
			Game1.playSound("smallSelect");
			return;
		}

		// Box Switchers
		if (_prevBoxBtn.containsPoint(x, y))
		{
			Storage.CurrentBoxIndex = (Storage.CurrentBoxIndex - 1 + PokemonStorage.TotalBoxes) % PokemonStorage.TotalBoxes;
			Game1.playSound("shwip");
			return;
		}
		if (_nextBoxBtn.containsPoint(x, y))
		{
			Storage.CurrentBoxIndex = (Storage.CurrentBoxIndex + 1) % PokemonStorage.TotalBoxes;
			Game1.playSound("shwip");
			return;
		}
		if (_jumpBoxBtn.containsPoint(x, y))
		{
			// Cycle wallpaper
			var box = Storage.CurrentBox;
			box.WallpaperIndex = (box.WallpaperIndex + 1) % BoxWallpapers.Length;
			Storage.SaveStorage();
			Game1.playSound("coin");
			return;
		}

		// Click Party Slot
		for (int i = 0; i < _partySlots.Count; i++)
		{
			if (_partySlots[i].containsPoint(x, y))
			{
				var party = Party.Team;
				if (i < party.Count)
				{
					// If we had a box slot selected, swap or withdraw!
					if (_selectedBoxIndex >= 0)
					{
						Storage.SwapWithParty(Storage.CurrentBoxIndex, _selectedBoxIndex, i);
						_selectedBoxIndex = -1;
						return;
					}

					OpenActionMenu(x, y, isParty: true, i);
				}
				else if (_selectedBoxIndex >= 0)
				{
					// Move from box to this empty party slot
					Storage.Withdraw(Storage.CurrentBoxIndex, _selectedBoxIndex);
					_selectedBoxIndex = -1;
				}
				return;
			}
		}

		// Click Box Slot
		for (int i = 0; i < _boxSlots.Count; i++)
		{
			if (_boxSlots[i].containsPoint(x, y))
			{
				var box = Storage.CurrentBox;
				var mon = box.Slots[i];

				// If we had a party slot selected, swap or deposit!
				if (_selectedPartyIndex >= 0)
				{
					Storage.SwapWithParty(Storage.CurrentBoxIndex, i, _selectedPartyIndex);
					_selectedPartyIndex = -1;
					return;
				}

				// If we had another box slot selected, move within box
				if (_selectedBoxIndex >= 0)
				{
					if (_selectedBoxIndex != i)
					{
						Storage.MoveWithinStorage(Storage.CurrentBoxIndex, _selectedBoxIndex, Storage.CurrentBoxIndex, i);
					}
					_selectedBoxIndex = -1;
					return;
				}

				if (mon != null)
				{
					OpenActionMenu(x, y, isParty: false, i);
				}
				return;
			}
		}

		// Reset selection if clicked on blank space
		_selectedPartyIndex = -1;
		_selectedBoxIndex = -1;
	}

	private void OpenActionMenu(int x, int y, bool isParty, int slot)
	{
		_showActionMenu = true;
		_actionFromParty = isParty;
		_actionTargetSlot = slot;
		_actionMenuPos = new Vector2(Math.Min(x, xPositionOnScreen + width - 170), Math.Min(y, yPositionOnScreen + height - 160));

		_actionButtons.Clear();
		int btnW = 160;
		int btnH = 30;
		int gap = 4;
		int curY = (int)_actionMenuPos.Y + 8;
		int curX = (int)_actionMenuPos.X + 8;

		if (isParty)
		{
			_actionButtons.Add(new ClickableComponent(new Rectangle(curX, curY, btnW, btnH), "summary", "Ver Sumário"));
			curY += btnH + gap;
			_actionButtons.Add(new ClickableComponent(new Rectangle(curX, curY, btnW, btnH), "select_move", "Mover / Trocar"));
			curY += btnH + gap;
			_actionButtons.Add(new ClickableComponent(new Rectangle(curX, curY, btnW, btnH), "deposit", "Depositar no PC"));
			curY += btnH + gap;
			_actionButtons.Add(new ClickableComponent(new Rectangle(curX, curY, btnW, btnH), "make_active", "Tornar Principal"));
			curY += btnH + gap;
			_actionButtons.Add(new ClickableComponent(new Rectangle(curX, curY, btnW, btnH), "cancel", "Voltar"));
		}
		else
		{
			_actionButtons.Add(new ClickableComponent(new Rectangle(curX, curY, btnW, btnH), "summary", "Ver Sumário"));
			curY += btnH + gap;
			_actionButtons.Add(new ClickableComponent(new Rectangle(curX, curY, btnW, btnH), "withdraw", "Mover para Equipe"));
			curY += btnH + gap;
			_actionButtons.Add(new ClickableComponent(new Rectangle(curX, curY, btnW, btnH), "select_move", "Mover / Trocar"));
			curY += btnH + gap;
			_actionButtons.Add(new ClickableComponent(new Rectangle(curX, curY, btnW, btnH), "release", "Soltar na Natureza"));
			curY += btnH + gap;
			_actionButtons.Add(new ClickableComponent(new Rectangle(curX, curY, btnW, btnH), "cancel", "Voltar"));
		}

		Game1.playSound("smallSelect");
	}

	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (_showActionMenu)
		{
			_showActionMenu = false;
			Game1.playSound("smallSelect");
			return;
		}

		var party = Party.Team;
		for (int i = 0; i < _partySlots.Count; i++)
		{
			if (_partySlots[i].containsPoint(x, y) && i < party.Count && party[i] != null)
			{
				Game1.activeClickableMenu = new PokemonSummaryMenu(party[i], party, this);
				return;
			}
		}

		var box = Storage.CurrentBox;
		for (int i = 0; i < _boxSlots.Count; i++)
		{
			if (_boxSlots[i].containsPoint(x, y) && i < PokemonStorage.SlotsPerBox && box.Slots[i] != null)
			{
				Game1.activeClickableMenu = new PokemonSummaryMenu(box.Slots[i], null, this);
				return;
			}
		}
	}

	private void HandleActionSelection(string action)
	{
		if (action == "cancel") return;

		if (_actionFromParty)
		{
			var party = Party.Team;
			if (_actionTargetSlot < 0 || _actionTargetSlot >= party.Count) return;

			if (action == "summary")
			{
				Game1.activeClickableMenu = new PokemonSummaryMenu(party[_actionTargetSlot], party, this);
				return;
			}
			if (action == "select_move")
			{
				_selectedPartyIndex = _actionTargetSlot;
				_selectedBoxIndex = -1;
				Game1.playSound("select");
			}
			else if (action == "deposit")
			{
				if (party.Count <= 1)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage("Você não pode depositar seu único Pokémon!");
					return;
				}
				var mon = party[_actionTargetSlot];
				if (Storage.Deposit(mon, Storage.CurrentBoxIndex))
				{
					party.RemoveAt(_actionTargetSlot);
					if (Party.ActiveIndex >= party.Count)
						Party.ActiveIndex = Math.Max(0, party.Count - 1);
					Party.SaveParty();
					Game1.playSound("coin");
					Game1.addHUDMessage(new HUDMessage($"{mon.Nickname} foi guardado na Caixa {Storage.LastDepositedBox + 1}!", 2));
				}
				else
				{
					Game1.playSound("cancel");
					Game1.showRedMessage("Todas as caixas do PC estão cheias!");
				}
			}
			else if (action == "make_active")
			{
				Party.SetActive(_actionTargetSlot);
			}
		}
		else
		{
			int curBox = Storage.CurrentBoxIndex;
			var box = Storage.CurrentBox;
			if (_actionTargetSlot < 0 || _actionTargetSlot >= PokemonStorage.SlotsPerBox) return;

			if (action == "summary" && box.Slots[_actionTargetSlot] != null)
			{
				Game1.activeClickableMenu = new PokemonSummaryMenu(box.Slots[_actionTargetSlot], null, this);
				return;
			}
			if (action == "withdraw")
			{
				Storage.Withdraw(curBox, _actionTargetSlot);
			}
			else if (action == "select_move")
			{
				_selectedBoxIndex = _actionTargetSlot;
				_selectedPartyIndex = -1;
				Game1.playSound("select");
			}
			else if (action == "release")
			{
				Storage.Release(curBox, _actionTargetSlot);
			}
		}
	}

	private bool _usingGamepad = false;
	private int _lastMouseX = -1;
	private int _lastMouseY = -1;

	public override bool areGamePadControlsImplemented() => true;

	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);

		if (x != _lastMouseX || y != _lastMouseY)
		{
			_lastMouseX = x;
			_lastMouseY = y;
			_usingGamepad = false;
		}

		if (_usingGamepad)
		{
			_hoverPartyIndex = -1;
			_hoverBoxIndex = -1;
			return;
		}

		_hoverPartyIndex = -1;
		_hoverBoxIndex = -1;

		for (int i = 0; i < _partySlots.Count; i++)
		{
			if (_partySlots[i].containsPoint(x, y))
			{
				_hoverPartyIndex = i;
				break;
			}
		}

		for (int i = 0; i < _boxSlots.Count; i++)
		{
			if (_boxSlots[i].containsPoint(x, y))
			{
				_hoverBoxIndex = i;
				break;
			}
		}
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape || Game1.options.doesInputListContain(Game1.options.menuButton, key))
		{
			if (_showActionMenu)
			{
				_showActionMenu = false;
				Game1.playSound("smallSelect");
				return;
			}
			if (_selectedPartyIndex >= 0 || _selectedBoxIndex >= 0)
			{
				_selectedPartyIndex = -1;
				_selectedBoxIndex = -1;
				Game1.playSound("smallSelect");
				return;
			}
			exitThisMenu();
			return;
		}
		base.receiveKeyPress(key);
	}

	public override void receiveGamePadButton(Buttons button)
	{
		_usingGamepad = true;
		if (_showActionMenu)
		{
			if (button == Buttons.DPadUp || button == Buttons.LeftThumbstickUp)
			{
				_gamepadActionIndex = Math.Max(0, _gamepadActionIndex - 1);
				Game1.playSound("shiny");
				return;
			}
			if (button == Buttons.DPadDown || button == Buttons.LeftThumbstickDown)
			{
				_gamepadActionIndex = Math.Min(_actionButtons.Count - 1, _gamepadActionIndex + 1);
				Game1.playSound("shiny");
				return;
			}
			if (button == Buttons.A || button == Buttons.Start)
			{
				if (_gamepadActionIndex >= 0 && _gamepadActionIndex < _actionButtons.Count)
				{
					HandleActionSelection(_actionButtons[_gamepadActionIndex].name);
					_showActionMenu = false;
				}
				return;
			}
			if (button == Buttons.B)
			{
				_showActionMenu = false;
				Game1.playSound("smallSelect");
				return;
			}
			return;
		}

		// Box cycling (LB / RB)
		if (button == Buttons.LeftShoulder || button == Buttons.LeftTrigger)
		{
			Storage.CurrentBoxIndex = (Storage.CurrentBoxIndex - 1 + PokemonStorage.TotalBoxes) % PokemonStorage.TotalBoxes;
			Game1.playSound("shwip");
			return;
		}
		if (button == Buttons.RightShoulder || button == Buttons.RightTrigger)
		{
			Storage.CurrentBoxIndex = (Storage.CurrentBoxIndex + 1) % PokemonStorage.TotalBoxes;
			Game1.playSound("shwip");
			return;
		}

		// Directional Navigation
		if (button == Buttons.DPadLeft || button == Buttons.LeftThumbstickLeft)
		{
			if (_focusArea == StorageFocusArea.Box)
			{
				if (_gamepadBoxIndex % PokemonStorage.BoxCols > 0)
				{
					_gamepadBoxIndex--;
					Game1.playSound("shiny");
				}
				else
				{
					_focusArea = StorageFocusArea.Party;
					int row = _gamepadBoxIndex / PokemonStorage.BoxCols;
					_gamepadPartyIndex = Math.Clamp(row, 0, 5);
					Game1.playSound("shiny");
				}
			}
			return;
		}
		if (button == Buttons.DPadRight || button == Buttons.LeftThumbstickRight)
		{
			if (_focusArea == StorageFocusArea.Party)
			{
				_focusArea = StorageFocusArea.Box;
				int row = Math.Clamp(_gamepadPartyIndex, 0, PokemonStorage.BoxRows - 1);
				_gamepadBoxIndex = row * PokemonStorage.BoxCols;
				Game1.playSound("shiny");
			}
			else if (_focusArea == StorageFocusArea.Box)
			{
				if (_gamepadBoxIndex % PokemonStorage.BoxCols < PokemonStorage.BoxCols - 1)
				{
					_gamepadBoxIndex++;
					Game1.playSound("shiny");
				}
			}
			return;
		}
		if (button == Buttons.DPadUp || button == Buttons.LeftThumbstickUp)
		{
			if (_focusArea == StorageFocusArea.Box)
			{
				if (_gamepadBoxIndex >= PokemonStorage.BoxCols)
				{
					_gamepadBoxIndex -= PokemonStorage.BoxCols;
					Game1.playSound("shiny");
				}
			}
			else if (_focusArea == StorageFocusArea.Party)
			{
				if (_gamepadPartyIndex > 0)
				{
					_gamepadPartyIndex--;
					Game1.playSound("shiny");
				}
			}
			return;
		}
		if (button == Buttons.DPadDown || button == Buttons.LeftThumbstickDown)
		{
			if (_focusArea == StorageFocusArea.Box)
			{
				if (_gamepadBoxIndex + PokemonStorage.BoxCols < PokemonStorage.SlotsPerBox)
				{
					_gamepadBoxIndex += PokemonStorage.BoxCols;
					Game1.playSound("shiny");
				}
			}
			else if (_focusArea == StorageFocusArea.Party)
			{
				if (_gamepadPartyIndex < 5)
				{
					_gamepadPartyIndex++;
					Game1.playSound("shiny");
				}
			}
			return;
		}

		// Action Confirm (A or Start)
		if (button == Buttons.A || button == Buttons.Start)
		{
			if (_focusArea == StorageFocusArea.Party)
			{
				var party = Party.Team;
				if (_selectedBoxIndex >= 0)
				{
					Storage.SwapWithParty(Storage.CurrentBoxIndex, _selectedBoxIndex, _gamepadPartyIndex);
					_selectedBoxIndex = -1;
					return;
				}
				if (_selectedPartyIndex >= 0)
				{
					if (_selectedPartyIndex != _gamepadPartyIndex && _selectedPartyIndex < party.Count && _gamepadPartyIndex < party.Count)
					{
						var tmp = party[_selectedPartyIndex];
						party[_selectedPartyIndex] = party[_gamepadPartyIndex];
						party[_gamepadPartyIndex] = tmp;
						Party.SaveParty();
						Game1.playSound("shiny");
					}
					_selectedPartyIndex = -1;
					return;
				}
				if (_gamepadPartyIndex < party.Count)
				{
					OpenActionMenu(_partySlots[_gamepadPartyIndex].bounds.Center.X, _partySlots[_gamepadPartyIndex].bounds.Center.Y, isParty: true, _gamepadPartyIndex);
					_gamepadActionIndex = 0;
				}
			}
			else if (_focusArea == StorageFocusArea.Box)
			{
				var box = Storage.CurrentBox;
				if (_selectedPartyIndex >= 0)
				{
					Storage.SwapWithParty(Storage.CurrentBoxIndex, _gamepadBoxIndex, _selectedPartyIndex);
					_selectedPartyIndex = -1;
					return;
				}
				if (_selectedBoxIndex >= 0)
				{
					if (_selectedBoxIndex != _gamepadBoxIndex)
					{
						Storage.MoveWithinStorage(Storage.CurrentBoxIndex, _selectedBoxIndex, Storage.CurrentBoxIndex, _gamepadBoxIndex);
					}
					_selectedBoxIndex = -1;
					return;
				}
				if (_gamepadBoxIndex < box.Slots.Length && box.Slots[_gamepadBoxIndex] != null)
				{
					OpenActionMenu(_boxSlots[_gamepadBoxIndex].bounds.Center.X, _boxSlots[_gamepadBoxIndex].bounds.Center.Y, isParty: false, _gamepadBoxIndex);
					_gamepadActionIndex = 0;
				}
			}
			return;
		}

		// Button X: Quick Summary
		if (button == Buttons.X)
		{
			if (_focusArea == StorageFocusArea.Party)
			{
				var party = Party.Team;
				if (_gamepadPartyIndex >= 0 && _gamepadPartyIndex < party.Count && party[_gamepadPartyIndex] != null)
				{
					Game1.activeClickableMenu = new PokemonSummaryMenu(party[_gamepadPartyIndex], party, this);
					return;
				}
			}
			else if (_focusArea == StorageFocusArea.Box)
			{
				var box = Storage.CurrentBox;
				if (_gamepadBoxIndex >= 0 && _gamepadBoxIndex < box.Slots.Length && box.Slots[_gamepadBoxIndex] != null)
				{
					Game1.activeClickableMenu = new PokemonSummaryMenu(box.Slots[_gamepadBoxIndex], null, this);
					return;
				}
			}
			return;
		}

		// Button Y: Cycle Box Wallpaper
		if (button == Buttons.Y)
		{
			var box = Storage.CurrentBox;
			box.WallpaperIndex = (box.WallpaperIndex + 1) % BoxWallpapers.Length;
			Storage.SaveStorage();
			Game1.playSound("coin");
			return;
		}

		// Button B: Cancel selection or Exit menu
		if (button == Buttons.B)
		{
			if (_selectedPartyIndex >= 0 || _selectedBoxIndex >= 0)
			{
				_selectedPartyIndex = -1;
				_selectedBoxIndex = -1;
				Game1.playSound("smallSelect");
				return;
			}
			Game1.playSound("bigDeSelect");
			exitThisMenu();
			return;
		}
	}

	public override void draw(SpriteBatch b)
	{
		// Dark background overlay
		b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.72f);

		// Main Window Frame
		PokemonUI.Border(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), 4, new Color(24, 48, 56));
		b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 4, yPositionOnScreen + 4, width - 8, height - 8), new Color(240, 248, 244));

		// Header Bar
		b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 4, yPositionOnScreen + 4, width - 8, 48), new Color(36, 88, 76));
		PokemonUI.Text(b, Game1.dialogueFont, "SISTEMA DE ARMAZENAMENTO POKÉMON", new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 10, 340, 32), Color.White, maxScale: 0.8f);

		// Box Switcher Buttons
		var currentBox = Storage.CurrentBox;
		b.Draw(Game1.staminaRect, _prevBoxBtn.bounds, new Color(48, 112, 96));
		PokemonUI.Border(b, _prevBoxBtn.bounds, 1, new Color(72, 144, 128));
		// Draw clear '<' arrow with small blocks
		int pbx = _prevBoxBtn.bounds.Center.X;
		int pby = _prevBoxBtn.bounds.Center.Y;
		b.Draw(Game1.staminaRect, new Rectangle(pbx - 4, pby - 1, 3, 2), Color.White);
		b.Draw(Game1.staminaRect, new Rectangle(pbx - 1, pby - 5, 3, 10), Color.White);
		b.Draw(Game1.staminaRect, new Rectangle(pbx + 2, pby - 8, 3, 16), Color.White);

		b.Draw(Game1.staminaRect, _jumpBoxBtn.bounds, new Color(24, 64, 52));
		PokemonUI.Border(b, _jumpBoxBtn.bounds, 1, new Color(48, 96, 80));
		string boxTitle = $"{currentBox.Name} ({currentBox.StoredCount}/30)";
		PokemonUI.Text(b, Game1.smallFont, boxTitle, _jumpBoxBtn.bounds, Color.Gold, center: true, maxScale: 0.90f);

		b.Draw(Game1.staminaRect, _nextBoxBtn.bounds, new Color(48, 112, 96));
		PokemonUI.Border(b, _nextBoxBtn.bounds, 1, new Color(72, 144, 128));
		// Draw clear '>' arrow with small blocks
		int nbx = _nextBoxBtn.bounds.Center.X;
		int nby = _nextBoxBtn.bounds.Center.Y;
		b.Draw(Game1.staminaRect, new Rectangle(nbx + 2, nby - 1, 3, 2), Color.White);
		b.Draw(Game1.staminaRect, new Rectangle(nbx - 1, nby - 5, 3, 10), Color.White);
		b.Draw(Game1.staminaRect, new Rectangle(nbx - 4, nby - 8, 3, 16), Color.White);

		_closeButton.draw(b);

		// Left Panel: Party List
		DrawPartyPanel(b);

		// Center-Right Panel: Box Grid
		DrawBoxGrid(b);

		// Bottom Preview / Summary Card
		DrawPreviewCard(b);

		// Action Popup Menu
		if (_showActionMenu)
		{
			DrawActionMenu(b);
		}

		// Gamepad Controls Legend
		if (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected)
		{
			string legend = "[D-Pad] Navegar  [A] Selecionar  [B] Voltar  [X] Sumário  [Y] Tema  [LB/RB] Mudar Caixa";
			PokemonUI.Text(b, Game1.tinyFont, legend, new Rectangle(xPositionOnScreen + 380, yPositionOnScreen + height - 20, width - 400, 16), Color.DarkSlateGray * 0.85f, maxScale: 0.72f);
		}

		// Cursor
		drawMouse(b);
	}

	private void DrawPartyPanel(SpriteBatch b)
	{
		PokemonUI.Border(b, _partyArea, 2, new Color(48, 80, 72));
		b.Draw(Game1.staminaRect, new Rectangle(_partyArea.X + 2, _partyArea.Y + 2, _partyArea.Width - 4, _partyArea.Height - 4), new Color(228, 238, 234));

		// Party Title
		b.Draw(Game1.staminaRect, new Rectangle(_partyArea.X + 2, _partyArea.Y + 2, _partyArea.Width - 4, 28), new Color(48, 96, 84));
		PokemonUI.Text(b, Game1.smallFont, $"SUA EQUIPE ({Party.Team.Count}/6)", new Rectangle(_partyArea.X + 8, _partyArea.Y + 4, _partyArea.Width - 16, 22), Color.White, center: true);

		bool isGamepad = (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected) && !_showActionMenu;
		float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 6f);
		int exp = pulse > 0 ? 3 : 2;

		var team = Party.Team;
		for (int i = 0; i < _partySlots.Count; i++)
		{
			var rect = _partySlots[i].bounds;
			bool isHover = (_hoverPartyIndex == i);
			bool isSelected = (_selectedPartyIndex == i);
			bool isGamepadFocus = isGamepad && (_focusArea == StorageFocusArea.Party) && (_gamepadPartyIndex == i);

			Color cardBg = isSelected ? new Color(255, 248, 200) : ((isHover || isGamepadFocus) ? new Color(245, 252, 248) : new Color(238, 244, 242));
			Color borderCol = isSelected ? Color.DarkOrange : ((isHover || isGamepadFocus) ? new Color(36, 120, 96) : new Color(180, 200, 192));

			b.Draw(Game1.staminaRect, rect, cardBg);
			PokemonUI.Border(b, rect, isSelected ? 3 : 2, borderCol);

			if (isGamepadFocus)
			{
				Rectangle glow = new Rectangle(rect.X - exp, rect.Y - exp, rect.Width + exp * 2, rect.Height + exp * 2);
				PokemonUI.Border(b, glow, 3, Color.Gold);
			}

			if (i < team.Count)
			{
				var p = team[i];
				// Confined 48x48 Icon Box on Left so sprite never overlaps party text
				Rectangle iconBox = new Rectangle(rect.X + 6, rect.Y + 6, 48, rect.Height - 12);
				b.Draw(Game1.staminaRect, iconBox, new Color(216, 230, 224));
				PokemonUI.Border(b, iconBox, 1, new Color(160, 185, 176));

				Texture2D icon = p.GetIconSprite(Game1.graphics.GraphicsDevice);
				if (icon != null)
				{
					Rectangle src = p.GetIconSourceRect();
					b.Draw(icon, new Vector2(iconBox.Center.X, iconBox.Center.Y), src, Color.White, 0f, new Vector2(src.Width / 2f, src.Height / 2f), 1.35f, SpriteEffects.None, 0.9f);
				}

				// Name & Level
				string shinyStar = p.IsShiny ? "★ " : "";
				string status = (i == Party.ActiveIndex) ? " [ATIVO]" : (p.IsFainted ? " [DESM]" : "");
				Color nameColor = p.IsFainted ? Color.Red : new Color(24, 32, 40);
				PokemonUI.Text(b, Game1.smallFont, $"{shinyStar}{p.Nickname}{status}", new Rectangle(rect.X + 60, rect.Y + 6, rect.Width - 68, 20), nameColor, maxScale: 0.82f);
				PokemonUI.Text(b, Game1.tinyFont, $"Lv.{p.Level} {p.Gender} | {p.Species.Name}", new Rectangle(rect.X + 60, rect.Y + 26, rect.Width - 68, 16), Color.DarkSlateGray, maxScale: 0.78f);

				// HP bar
				PokemonUI.HpBar(b, new Rectangle(rect.X + 60, rect.Y + 46, rect.Width - 68, 8), p.HpRatio);
			}
			else
			{
				PokemonUI.Text(b, Game1.tinyFont, "-- Vazio --", rect, Color.Gray * 0.7f, center: true);
			}
		}
	}

	private void DrawBoxGrid(SpriteBatch b)
	{
		var box = Storage.CurrentBox;
		Color wallpaper = BoxWallpapers[box.WallpaperIndex % BoxWallpapers.Length];

		PokemonUI.Border(b, _boxArea, 2, new Color(48, 80, 72));
		b.Draw(Game1.staminaRect, new Rectangle(_boxArea.X + 2, _boxArea.Y + 2, _boxArea.Width - 4, _boxArea.Height - 4), wallpaper);

		bool isGamepad = (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected) && !_showActionMenu;
		float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 6f);
		int exp = pulse > 0 ? 3 : 2;

		// Grid Slots
		for (int i = 0; i < _boxSlots.Count; i++)
		{
			var rect = _boxSlots[i].bounds;
			var mon = box.Slots[i];
			bool isHover = (_hoverBoxIndex == i);
			bool isSelected = (_selectedBoxIndex == i);
			bool isGamepadFocus = isGamepad && (_focusArea == StorageFocusArea.Box) && (_gamepadBoxIndex == i);

			Color slotBg = isSelected ? new Color(255, 248, 180) : ((isHover || isGamepadFocus) ? Color.White * 0.85f : Color.White * 0.45f);
			Color borderCol = isSelected ? Color.DarkOrange : ((isHover || isGamepadFocus) ? new Color(36, 120, 96) : Color.DarkSlateGray * 0.25f);

			b.Draw(Game1.staminaRect, rect, slotBg);
			PokemonUI.Border(b, rect, isSelected ? 3 : 1, borderCol);

			if (isGamepadFocus)
			{
				Rectangle glow = new Rectangle(rect.X - exp, rect.Y - exp, rect.Width + exp * 2, rect.Height + exp * 2);
				PokemonUI.Border(b, glow, 3, Color.Gold);
			}

			if (mon != null)
			{
				Texture2D icon = mon.GetIconSprite(Game1.graphics.GraphicsDevice);
				if (icon != null)
				{
					Rectangle src = mon.GetIconSourceRect();
					b.Draw(icon, new Vector2(rect.Center.X, rect.Center.Y - 4), src, Color.White, 0f, new Vector2(src.Width / 2f, src.Height / 2f), 1.5f, SpriteEffects.None, 0.9f);
				}
				// Level tag in bottom corner
				string tag = mon.IsShiny ? $"★{mon.Level}" : $"Lv.{mon.Level}";
				PokemonUI.Text(b, Game1.tinyFont, tag, new Rectangle(rect.X + 2, rect.Bottom - 16, rect.Width - 4, 14), mon.IsShiny ? new Color(180, 110, 0) : Color.Black, center: true, maxScale: 0.72f);
			}
			else
			{
				// Subtle dot marker for empty slot
				b.Draw(Game1.staminaRect, new Rectangle(rect.Center.X - 2, rect.Center.Y - 2, 4, 4), Color.DarkSlateGray * 0.25f);
			}
		}
	}

	private void DrawPreviewCard(SpriteBatch b)
	{
		PokemonUI.Border(b, _previewArea, 2, new Color(48, 80, 72));
		b.Draw(Game1.staminaRect, new Rectangle(_previewArea.X + 2, _previewArea.Y + 2, _previewArea.Width - 4, _previewArea.Height - 4), new Color(236, 244, 240));

		// Find hovered or selected pokemon
		PokemonInstance target = null;
		string origin = "";

		if (_hoverPartyIndex >= 0 && _hoverPartyIndex < Party.Team.Count)
		{
			target = Party.Team[_hoverPartyIndex];
			origin = "Equipe";
		}
		else if (_hoverBoxIndex >= 0 && _hoverBoxIndex < PokemonStorage.SlotsPerBox)
		{
			target = Storage.CurrentBox.Slots[_hoverBoxIndex];
			origin = "PC";
		}
		else if (_selectedPartyIndex >= 0 && _selectedPartyIndex < Party.Team.Count)
		{
			target = Party.Team[_selectedPartyIndex];
			origin = "Equipe (Selecionado)";
		}
		else if (_selectedBoxIndex >= 0 && _selectedBoxIndex < PokemonStorage.SlotsPerBox)
		{
			target = Storage.CurrentBox.Slots[_selectedBoxIndex];
			origin = "PC (Selecionado)";
		}
		else if (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected)
		{
			if (_focusArea == StorageFocusArea.Party && _gamepadPartyIndex >= 0 && _gamepadPartyIndex < Party.Team.Count)
			{
				target = Party.Team[_gamepadPartyIndex];
				origin = "Equipe";
			}
			else if (_focusArea == StorageFocusArea.Box && _gamepadBoxIndex >= 0 && _gamepadBoxIndex < Storage.CurrentBox.Slots.Length)
			{
				target = Storage.CurrentBox.Slots[_gamepadBoxIndex];
				origin = "PC";
			}
		}

		if (target == null)
		{
			PokemonUI.Text(b, Game1.smallFont, "Passe o cursor sobre um Pokémon (ou clique com Botão Direito / [X] para abrir o Sumário).", _previewArea, Color.DarkSlateGray * 0.75f, center: true, maxScale: 0.85f);
			return;
		}

		// Column 1: Dedicated Framed Portrait Box (96x94) so sprite NEVER overlaps text!
		Rectangle portraitBox = new Rectangle(_previewArea.X + 10, _previewArea.Y + 8, 96, _previewArea.Height - 16);
		b.Draw(Game1.staminaRect, portraitBox, target.IsShiny ? new Color(255, 248, 215) : new Color(214, 228, 222));
		PokemonUI.Border(b, portraitBox, 2, target.IsShiny ? Color.Goldenrod : new Color(72, 112, 98));

		Texture2D front = target.GetFrontSprite(Game1.graphics.GraphicsDevice);
		if (front != null)
		{
			Rectangle src = target.GetFrontSourceRect();
			float scale = Math.Min((portraitBox.Width - 8f) / Math.Max(1, src.Width), (portraitBox.Height - 8f) / Math.Max(1, src.Height));
			Vector2 centerPos = new Vector2(portraitBox.Center.X, portraitBox.Center.Y);
			Vector2 sprOrigin = new Vector2(src.Width / 2f, src.Height / 2f);
			b.Draw(front, centerPos, src, Color.White, 0f, sprOrigin, scale, SpriteEffects.None, 0.9f);
		}

		// Column 2: Identity, Nature, Types & HP
		int infoX = portraitBox.Right + 14;
		int infoY = _previewArea.Y + 8;
		int col2W = 240;

		string shinyBadge = target.IsShiny ? "★ " : "";
		PokemonUI.Text(b, Game1.smallFont, $"{shinyBadge}{target.Nickname} {target.Gender} [Lv.{target.Level}]", new Rectangle(infoX, infoY, col2W, 22), Color.Black, maxScale: 0.84f);
		PokemonUI.Text(b, Game1.tinyFont, $"#{target.Species.DexNumber:D3} {target.Species.Name} | {origin}", new Rectangle(infoX, infoY + 22, col2W, 16), Color.DarkSlateGray, maxScale: 0.76f);

		string typesText = target.Species.SecondaryType.HasValue ? $"{target.Species.PrimaryType}/{target.Species.SecondaryType}" : target.Species.PrimaryType.ToString();
		PokemonUI.Text(b, Game1.tinyFont, $"Tipo: {typesText} | {target.Nature}", new Rectangle(infoX, infoY + 42, col2W, 16), new Color(20, 100, 50), maxScale: 0.76f);
		PokemonUI.Text(b, Game1.tinyFont, $"HP: {target.CurrentHP}/{target.MaxHP}", new Rectangle(infoX, infoY + 62, 105, 16), Color.Black, maxScale: 0.76f);
		PokemonUI.HpBar(b, new Rectangle(infoX + 100, infoY + 66, 125, 8), target.HpRatio);
		PokemonUI.Text(b, Game1.tinyFont, "[Botão Direito / X: Sumário Completo]", new Rectangle(infoX, infoY + 82, col2W, 16), new Color(30, 90, 160), maxScale: 0.70f);

		// Column 3: Stats, IVs/EVs & Moves
		int statX = infoX + col2W + 12;
		int statY = _previewArea.Y + 8;
		int col3W = Math.Max(180, _previewArea.Right - statX - 10);

		PokemonUI.Text(b, Game1.tinyFont, $"ATK {target.Attack} | DEF {target.Defense} | SPA {target.SpAttack}", new Rectangle(statX, statY, col3W, 16), Color.DarkRed, maxScale: 0.75f);
		PokemonUI.Text(b, Game1.tinyFont, $"SPD {target.SpDefense} | SPE {target.Speed} | IV {target.TotalIVs}/186", new Rectangle(statX, statY + 18, col3W, 16), Color.DarkBlue, maxScale: 0.75f);

		int moveY = statY + 38;
		PokemonUI.Text(b, Game1.tinyFont, "Golpes:", new Rectangle(statX, moveY, col3W, 14), Color.DarkSlateGray, maxScale: 0.72f);
		for (int m = 0; m < Math.Min(target.Moves.Count, 4); m++)
		{
			var move = target.Moves[m];
			int mx = statX + (m % 2) * (col3W / 2);
			int my = moveY + 16 + (m / 2) * 18;
			PokemonUI.Text(b, Game1.tinyFont, $"• {move.Name} ({move.CurrentPP}/{move.MaxPP})", new Rectangle(mx, my, (col3W / 2) - 4, 16), Color.DarkSlateBlue, maxScale: 0.68f);
		}
	}

	private void DrawActionMenu(SpriteBatch b)
	{
		int menuW = 176;
		int menuH = _actionButtons.Count * 34 + 16;
		Rectangle menuRect = new Rectangle((int)_actionMenuPos.X, (int)_actionMenuPos.Y, menuW, menuH);

		PokemonUI.Border(b, menuRect, 3, new Color(24, 48, 56));
		b.Draw(Game1.staminaRect, new Rectangle(menuRect.X + 3, menuRect.Y + 3, menuRect.Width - 6, menuRect.Height - 6), new Color(248, 252, 250));

		bool isGamepad = Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected;
		float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 6f);
		int exp = pulse > 0 ? 3 : 2;

		for (int i = 0; i < _actionButtons.Count; i++)
		{
			var btn = _actionButtons[i];
			bool isHover = btn.containsPoint(Game1.getMouseX(), Game1.getMouseY());
			bool isGamepadFocus = isGamepad && (_gamepadActionIndex == i);

			Color btnBg = (isHover || isGamepadFocus) ? new Color(216, 236, 228) : Color.White;
			Color border = (isHover || isGamepadFocus) ? new Color(36, 96, 76) : new Color(180, 200, 192);

			b.Draw(Game1.staminaRect, btn.bounds, btnBg);
			PokemonUI.Border(b, btn.bounds, 1, border);

			if (isGamepadFocus)
			{
				Rectangle glow = new Rectangle(btn.bounds.X - exp, btn.bounds.Y - exp, btn.bounds.Width + exp * 2, btn.bounds.Height + exp * 2);
				PokemonUI.Border(b, glow, 3, Color.Gold);
			}

			PokemonUI.Text(b, Game1.smallFont, btn.label, btn.bounds, (isHover || isGamepadFocus) ? Color.DarkGreen : Color.Black, center: true, maxScale: 0.85f);
		}
	}
}
