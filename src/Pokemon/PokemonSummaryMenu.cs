using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

/// <summary>
/// Authentic GBA/NDS-style Individual Pokémon Summary Menu.
/// Split into two toggleable tabs:
/// - Tab 1 (Battle): Stats table with nature boosts and 4-move Moveset grid.
/// - Tab 2 (Trainer/Info): Friendship & Happiness (0-255), Ability with clean readable text, Nature, IVs (0-31), EVs (0-252), and OT.
/// Opened via Right-Click on a Pokémon in the Lateral HUD, Party Menu, or PC Storage.
/// </summary>
public class PokemonSummaryMenu : IClickableMenu
{
	public enum SummaryTab
	{
		Battle,
		Trainer
	}

	private PokemonInstance _mon;
	private readonly List<PokemonInstance> _contextList;
	private int _contextIndex = -1;
	private readonly IClickableMenu _returnMenu;

	private const int MenuWidth = 880;
	private const int MenuHeight = 600;

	private SummaryTab _currentTab = SummaryTab.Battle;

	private ClickableComponent _prevMonBtn;
	private ClickableComponent _nextMonBtn;
	private ClickableComponent _battleTabBtn;
	private ClickableComponent _trainerTabBtn;
	private ClickableComponent _heldItemBtn;
	private ClickableComponent _revertMegaBtn;

	private int _hoveredMoveIndex = -1;
	private readonly List<Rectangle> _moveRects = new();
	private readonly Farmer _farmer;

	public PokemonSummaryMenu(PokemonInstance mon, List<PokemonInstance> contextList = null, IClickableMenu returnMenu = null, Farmer farmer = null)
		: base(
			(Game1.uiViewport.Width - MenuWidth) / 2,
			(Game1.uiViewport.Height - MenuHeight) / 2,
			MenuWidth,
			MenuHeight,
			showUpperRightCloseButton: true)
	{
		_mon = mon;
		_contextList = contextList;
		_returnMenu = returnMenu;
		_farmer = farmer ?? Game1.player;
		if (_contextList != null && _mon != null)
		{
			_contextIndex = _contextList.IndexOf(_mon);
		}
		SetupLayout();
		Game1.playSound("bigSelect");
	}

	private bool CanRevertMegaOutOfBattle()
	{
		if (_mon?.Species == null) return false;
		if (_returnMenu is PokemonBattleMenu) return false;
		return _mon.IsMegaEvolved || _mon.Species.IsMega;
	}

	private bool CanMegaEvolveOutOfBattle(out PokemonSpecies targetMega, out string stoneId)
	{
		targetMega = null;
		stoneId = null;
		if (_mon?.Species == null) return false;
		if (_returnMenu is PokemonBattleMenu) return false;
		if (_mon.IsMegaEvolved || _mon.Species.IsMega) return false;

		string baseKey = _mon.Species.BaseSpeciesName ?? _mon.Species.InternalName;
		var megaForms = PokemonSpecies.GetMegaForms(baseKey);
		if (megaForms == null || megaForms.Count == 0) return false;

		string normHeld = PokemonHeldItemManager.NormalizeId(_mon.HeldItem);

		foreach (var mf in megaForms)
		{
			string reqStone = PokemonItemManager.GetMegaStoneIdForMegaForm(mf.InternalName);
			string normReq = PokemonHeldItemManager.NormalizeId(reqStone);
			bool hasMatchingStone = !string.IsNullOrEmpty(normReq) && string.Equals(normHeld, normReq, StringComparison.OrdinalIgnoreCase);
			bool isUltraNecrozma = mf.InternalName.Equals("necrozma/ultra", StringComparison.OrdinalIgnoreCase) &&
			                       string.Equals(normHeld, "UltranecroziumZ", StringComparison.OrdinalIgnoreCase);
			bool isRayquazaDragonAscent = mf.InternalName.Equals("rayquaza/mega", StringComparison.OrdinalIgnoreCase) &&
			                              _mon.Moves != null &&
			                              _mon.Moves.Exists(m => string.Equals(m.Name, "Dragon Ascent", StringComparison.OrdinalIgnoreCase));

			if (hasMatchingStone || isUltraNecrozma || isRayquazaDragonAscent)
			{
				Farmer owner = _farmer ?? Game1.player;
				bool isPrimal = baseKey.Equals("kyogre", StringComparison.OrdinalIgnoreCase) ||
				                baseKey.Equals("groudon", StringComparison.OrdinalIgnoreCase);
				if (isUltraNecrozma)
				{
					if (!PokemonItemManager.IsWearingZPowerRing(owner))
						return false;
				}
				else if (!isPrimal)
				{
					if (!PokemonItemManager.IsWearingMegaBracelet(owner))
						return false;
				}
				targetMega = mf;
				stoneId = !string.IsNullOrEmpty(_mon.HeldItem) ? _mon.HeldItem : reqStone;
				return true;
			}
		}
		return false;
	}

	private void SetupLayout()
	{
		xPositionOnScreen = (Game1.uiViewport.Width - MenuWidth) / 2;
		yPositionOnScreen = (Game1.uiViewport.Height - MenuHeight) / 2;
		upperRightCloseButton = new ClickableTextureComponent(
			new Rectangle(xPositionOnScreen + MenuWidth - 36, yPositionOnScreen - 8, 48, 48),
			Game1.mouseCursors,
			new Rectangle(337, 494, 12, 12),
			4f);

		_prevMonBtn = new ClickableComponent(new Rectangle(xPositionOnScreen + 22, yPositionOnScreen + 14, 114, 36), "prev", "◀ Anterior");
		_nextMonBtn = new ClickableComponent(new Rectangle(xPositionOnScreen + 142, yPositionOnScreen + 14, 114, 36), "next", "Próximo ▶");

		int leftX = xPositionOnScreen + 18;
		int leftW = 300;
		_heldItemBtn = new ClickableComponent(new Rectangle(leftX + 12, yPositionOnScreen + MenuHeight - 96, leftW - 24, 34), "held_item", "Item");
		_revertMegaBtn = new ClickableComponent(new Rectangle(leftX + 12, yPositionOnScreen + MenuHeight - 56, leftW - 24, 34), "revert_mega", "🧬 Destransformar Mega");

		int rightX = xPositionOnScreen + 332;
		int rightW = MenuWidth - 350;
		int tabW = (rightW - 10) / 2;
		_battleTabBtn = new ClickableComponent(new Rectangle(rightX, yPositionOnScreen + 60, tabW, 36), "tab_battle", "⚔️ Batalha & Golpes");
		_trainerTabBtn = new ClickableComponent(new Rectangle(rightX + tabW + 10, yPositionOnScreen + 60, tabW, 36), "tab_trainer", "📋 Info & Treinamento");

		_moveRects.Clear();
		int contentY = yPositionOnScreen + 104;
		int movesStartY = contentY + 242;
		int moveW = (rightW - 12) / 2;
		int moveH = 100;
		for (int i = 0; i < 4; i++)
		{
			int col = i % 2;
			int row = i / 2;
			_moveRects.Add(new Rectangle(rightX + col * (moveW + 12), movesStartY + row * (moveH + 8), moveW, moveH));
		}
	}

	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		SetupLayout();
	}

	private void CyclePokemon(int delta)
	{
		if (_contextList == null || _contextList.Count <= 1 || _contextIndex < 0) return;
		_contextIndex = (_contextIndex + delta + _contextList.Count) % _contextList.Count;
		_mon = _contextList[_contextIndex];
		Game1.playSound("shwip");
		if (_mon?.Species != null)
		{
			PokemonCryManager.PlayCry(_mon.Species.Name);
		}
	}

	private void CloseSummary()
	{
		Game1.playSound("bigDeSelect");
		if (_returnMenu != null)
		{
			Game1.activeClickableMenu = _returnMenu;
		}
		else
		{
			exitThisMenuNoSound();
		}
	}

	private void HandleMegaAction()
	{
		if (CanRevertMegaOutOfBattle())
		{
			HandleRevertMega();
		}
		else if (CanMegaEvolveOutOfBattle(out var targetMega, out var stoneId))
		{
			_mon.UsedMegaStoneId = stoneId;
			_mon.MegaEvolve(targetMega);
			PlayerParty.GetForFarmer(_farmer).SaveParty();
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage($"✨ {_mon.Nickname} Mega Evoluiu para {targetMega.Name}!", 2));
		}
		else if (PokemonItemUse.CanSwitchFormInSummary(_mon))
		{
			PokemonItemUse.OpenFormSwitchFromSummary(_mon, this);
		}
	}

	private void HandleRevertMega()
	{
		if (!CanRevertMegaOutOfBattle()) return;

		string megaInternal = _mon.Species.InternalName;
		string fallbackStoneId = PokemonItemManager.GetMegaStoneIdForMegaForm(megaInternal);
		if (_mon.RevertMegaEvolution(out string returnedStoneId))
		{
			string stoneKept = !string.IsNullOrEmpty(returnedStoneId) ? returnedStoneId : fallbackStoneId;
			if (string.IsNullOrEmpty(_mon.HeldItem) && !string.IsNullOrEmpty(stoneKept))
			{
				_mon.HeldItem = stoneKept;
			}
			PlayerParty.GetForFarmer(_farmer).SaveParty();
			Game1.playSound("wand");
			Game1.addHUDMessage(new HUDMessage($"{_mon.Nickname} voltou à forma normal!", 2));
		}
	}

	private void ConsumeActiveItem(Item item, bool isCursor)
	{
		if (item == null) return;
		if (isCursor)
		{
			if (item.Stack > 1) item.Stack--;
			else Game1.player.CursorSlotItem = null;
		}
		else if (Game1.player?.CurrentItem == item)
		{
			if (item.Stack > 1) item.Stack--;
			else Game1.player.Items[Game1.player.CurrentToolIndex] = null;
		}
		else if (Game1.player?.Items != null)
		{
			Game1.player.Items.ReduceId(item.QualifiedItemId, 1);
		}
	}

	private void HandleHeldItemClick()
	{
		if (_mon == null) return;

		// 1. Determine active item in player's hand or cursor
		Item activeItem = Game1.player?.CursorSlotItem ?? Game1.player?.CurrentItem;
		bool isCursor = Game1.player?.CursorSlotItem != null;

		// 2. If the Pokémon is currently holding an item:
		if (!string.IsNullOrEmpty(_mon.HeldItem))
		{
			if (_mon.IsMegaEvolved)
			{
				Game1.playSound("cancel");
				Game1.addHUDMessage(new HUDMessage("Destransforme a Mega Evolução antes de remover a Mega Pedra!", 3));
				return;
			}

			// If player has an item in hand: attempt to SWAP!
			if (activeItem != null)
			{
				if (!PokemonHeldItemManager.IsHoldable(activeItem.ItemId))
				{
					Game1.playSound("cancel");
					Game1.addHUDMessage(new HUDMessage($"'{activeItem.DisplayName}' não pode ser equipado como Held Item!", 3));
					return;
				}

				string oldHeld = _mon.HeldItem;
				string normalizedNew = PokemonHeldItemManager.NormalizeId(activeItem.ItemId);
				if (string.Equals(oldHeld, normalizedNew, StringComparison.OrdinalIgnoreCase))
				{
					Game1.playSound("cancel");
					Game1.addHUDMessage(new HUDMessage($"{_mon.Nickname} já está segurando este item!", 3));
					return;
				}

				// Perform Swap
				_mon.HeldItem = normalizedNew;
				_mon.OnHeldItemChanged(oldHeld, normalizedNew);
				ConsumeActiveItem(activeItem, isCursor);

				string fullOldId = "(O)" + oldHeld;
				Item returned = ItemRegistry.Create(fullOldId, 1, 0, allowNull: true) ?? new StardewValley.Object(oldHeld, 1);
				if (returned != null)
				{
					Game1.player.addItemToInventoryBool(returned);
				}
				PlayerParty.GetForFarmer(_farmer).SaveParty();
				Game1.playSound("reward");
				Game1.addHUDMessage(new HUDMessage($"Trocou item de {_mon.Nickname} para {activeItem.DisplayName}!", 2));
				return;
			}

			// Player has NO item in hand (empty hand/cursor): Unequip current item to inventory!
			string oldHeldItem = _mon.HeldItem;
			string fullId = "(O)" + oldHeldItem;
			Item unequipped = ItemRegistry.Create(fullId, 1, 0, allowNull: true) ?? new StardewValley.Object(oldHeldItem, 1);
			if (unequipped != null)
			{
				if (Game1.player.addItemToInventoryBool(unequipped))
				{
					_mon.HeldItem = null;
					_mon.OnHeldItemChanged(oldHeldItem, null);
					PlayerParty.GetForFarmer(_farmer).SaveParty();
					Game1.playSound("coin");
					Game1.addHUDMessage(new HUDMessage($"Desequipou {unequipped.DisplayName} de {_mon.Nickname}!", 2));
				}
				else
				{
					Game1.playSound("cancel");
					Game1.addHUDMessage(new HUDMessage("Inventário cheio para desequipar item!", 3));
				}
			}
			return;
		}

		// 3. Pokémon is NOT currently holding an item:
		if (activeItem != null)
		{
			if (PokemonHeldItemManager.IsHoldable(activeItem.ItemId))
			{
				_mon.HeldItem = PokemonHeldItemManager.NormalizeId(activeItem.ItemId);
				_mon.OnHeldItemChanged(null, _mon.HeldItem);
				ConsumeActiveItem(activeItem, isCursor);
				PlayerParty.GetForFarmer(_farmer).SaveParty();
				Game1.playSound("reward");
				Game1.addHUDMessage(new HUDMessage($"Equipou {activeItem.DisplayName} em {_mon.Nickname}!", 2));
			}
			else
			{
				Game1.playSound("cancel");
				Game1.addHUDMessage(new HUDMessage($"'{activeItem.DisplayName}' não pode ser equipado como Held Item!", 3));
			}
			return;
		}

		// Player hand is empty and mon has no item:
		Game1.playSound("cancel");
		Game1.addHUDMessage(new HUDMessage("Selecione um Held Item, Mega Pedra ou Cristal Z na mão para equipar!", 3));
	}

	private void CycleHeldItemFromInventory()
	{
		if (_mon == null || Game1.player?.Items == null) return;

		if (_mon.IsMegaEvolved)
		{
			Game1.playSound("cancel");
			Game1.addHUDMessage(new HUDMessage("Destransforme a Mega Evolução antes de alterar o item!", 3));
			return;
		}

		// Find all holdable items in inventory
		List<Item> holdables = new();
		foreach (var it in Game1.player.Items)
		{
			if (it != null && PokemonHeldItemManager.IsHoldable(it.ItemId))
			{
				holdables.Add(it);
			}
		}

		if (holdables.Count == 0 && string.IsNullOrEmpty(_mon.HeldItem))
		{
			Game1.playSound("cancel");
			Game1.addHUDMessage(new HUDMessage("Nenhum item equipável na mochila!", 3));
			return;
		}

		string oldHeld = _mon.HeldItem;
		Item itemToEquip = null;

		if (string.IsNullOrEmpty(oldHeld))
		{
			if (holdables.Count > 0)
			{
				itemToEquip = holdables[0];
			}
		}
		else
		{
			// Mon currently has item. Unequip it first.
			string fullId = "(O)" + oldHeld;
			Item returned = ItemRegistry.Create(fullId, 1, 0, allowNull: true) ?? new StardewValley.Object(oldHeld, 1);
			_mon.HeldItem = null;
			_mon.OnHeldItemChanged(oldHeld, null);
			if (returned != null)
			{
				Game1.player.addItemToInventoryBool(returned);
			}

			// Re-gather inventory holdables
			holdables.Clear();
			foreach (var it in Game1.player.Items)
			{
				if (it != null && PokemonHeldItemManager.IsHoldable(it.ItemId))
				{
					holdables.Add(it);
				}
			}

			int idx = holdables.FindIndex(it => it.ItemId.Equals(oldHeld, StringComparison.OrdinalIgnoreCase));
			int nextIdx = (idx + 1) % (holdables.Count + 1);
			if (nextIdx < holdables.Count)
			{
				itemToEquip = holdables[nextIdx];
			}
			else
			{
				PlayerParty.GetForFarmer(_farmer).SaveParty();
				Game1.playSound("coin");
				Game1.addHUDMessage(new HUDMessage($"Desequipou {returned?.DisplayName ?? oldHeld} de {_mon.Nickname}!", 2));
				return;
			}
		}

		if (itemToEquip != null)
		{
			_mon.HeldItem = PokemonHeldItemManager.NormalizeId(itemToEquip.ItemId);
			_mon.OnHeldItemChanged(null, _mon.HeldItem);
			Game1.player.Items.ReduceId(itemToEquip.QualifiedItemId, 1);
			PlayerParty.GetForFarmer(_farmer).SaveParty();
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage($"Equipou {itemToEquip.DisplayName} em {_mon.Nickname}!", 2));
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (upperRightCloseButton != null && upperRightCloseButton.containsPoint(x, y))
		{
			CloseSummary();
			return;
		}
		if (_battleTabBtn != null && _battleTabBtn.containsPoint(x, y))
		{
			if (_currentTab != SummaryTab.Battle)
			{
				_currentTab = SummaryTab.Battle;
				Game1.playSound("smallSelect");
			}
			return;
		}
		if (_trainerTabBtn != null && _trainerTabBtn.containsPoint(x, y))
		{
			if (_currentTab != SummaryTab.Trainer)
			{
				_currentTab = SummaryTab.Trainer;
				Game1.playSound("smallSelect");
			}
			return;
		}
		if (_heldItemBtn != null && _heldItemBtn.containsPoint(x, y))
		{
			HandleHeldItemClick();
			return;
		}
		if ((CanRevertMegaOutOfBattle() || CanMegaEvolveOutOfBattle(out _, out _) || PokemonItemUse.CanSwitchFormInSummary(_mon)) && _revertMegaBtn != null && _revertMegaBtn.containsPoint(x, y))
		{
			HandleMegaAction();
			return;
		}

		Rectangle stageRect = new Rectangle(xPositionOnScreen + 18 + (300 - 190) / 2, yPositionOnScreen + 60 + 66, 190, 140);
		if (stageRect.Contains(x, y) && _mon?.Species != null)
		{
			PokemonCryManager.PlayCry(_mon.Species.Name);
			return;
		}
		if (_contextList != null && _contextList.Count > 1)
		{
			if (_prevMonBtn.containsPoint(x, y))
			{
				CyclePokemon(-1);
				return;
			}
			if (_nextMonBtn.containsPoint(x, y))
			{
				CyclePokemon(1);
				return;
			}
		}
	}

	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (_heldItemBtn != null && _heldItemBtn.containsPoint(x, y))
		{
			CycleHeldItemFromInventory();
			return;
		}
		CloseSummary();
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape || key == Keys.Back)
		{
			CloseSummary();
			return;
		}
		if (key == Keys.Tab || key == Keys.Q || key == Keys.E)
		{
			_currentTab = (_currentTab == SummaryTab.Battle) ? SummaryTab.Trainer : SummaryTab.Battle;
			Game1.playSound("smallSelect");
			return;
		}
		if (key == Keys.Left || key == Keys.Up || key == Keys.A || key == Keys.W)
		{
			CyclePokemon(-1);
		}
		else if (key == Keys.Right || key == Keys.Down || key == Keys.D || key == Keys.S)
		{
			CyclePokemon(1);
		}
	}

	public override bool areGamePadControlsImplemented() => true;

	public override void receiveGamePadButton(Buttons button)
	{
		if (button == Buttons.B || button == Buttons.Back)
		{
			CloseSummary();
			return;
		}
		if (button == Buttons.LeftShoulder || button == Buttons.RightShoulder || button == Buttons.LeftTrigger || button == Buttons.RightTrigger)
		{
			_currentTab = (_currentTab == SummaryTab.Battle) ? SummaryTab.Trainer : SummaryTab.Battle;
			Game1.playSound("smallSelect");
			return;
		}
		if (button == Buttons.DPadLeft || button == Buttons.LeftThumbstickLeft || button == Buttons.DPadUp || button == Buttons.LeftThumbstickUp)
		{
			CyclePokemon(-1);
			return;
		}
		if (button == Buttons.DPadRight || button == Buttons.LeftThumbstickRight || button == Buttons.DPadDown || button == Buttons.LeftThumbstickDown)
		{
			CyclePokemon(1);
			return;
		}
		if (button == Buttons.Y || button == Buttons.A)
		{
			CycleHeldItemFromInventory();
			return;
		}
	}

	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		_hoveredMoveIndex = -1;
		if (_currentTab == SummaryTab.Battle)
		{
			for (int i = 0; i < _moveRects.Count; i++)
			{
				if (_moveRects[i].Contains(x, y))
				{
					_hoveredMoveIndex = i;
					break;
				}
			}
		}
	}

	public static void DrawStatusBadge(SpriteBatch b, PokemonStatusCondition status, int x, int y)
	{
		if (status == PokemonStatusCondition.None) return;
		(string label, Color bg) = status switch
		{
			PokemonStatusCondition.Paralysis => ("PAR", new Color(235, 190, 20)),
			PokemonStatusCondition.Sleep     => ("SLP", new Color(130, 145, 165)),
			PokemonStatusCondition.Poison    => ("PSN", new Color(165, 65, 185)),
			PokemonStatusCondition.Toxic     => ("TOX", new Color(120, 30, 155)),
			PokemonStatusCondition.Burn      => ("BRN", new Color(230, 80, 35)),
			PokemonStatusCondition.Freeze    => ("FRZ", new Color(85, 195, 235)),
			_ => ("", Color.Gray)
		};
		if (string.IsNullOrEmpty(label)) return;
		PokemonUI.DrawRoundedRect(b, new Rectangle(x, y, 42, 20), bg, 4);
		PokemonUI.DrawCardBorder(b, new Rectangle(x, y, 42, 20), Color.White * 0.7f, 1);
		PokemonUI.Text(b, Game1.smallFont, label, new Rectangle(x + 2, y + 1, 38, 18), Color.White, center: true, maxScale: 0.56f, shadow: false);
	}

	public override void draw(SpriteBatch b)
	{
		// Dim background
		b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.72f);

		if (_mon == null || _mon.Species == null)
		{
			base.draw(b);
			drawMouse(b);
			return;
		}

		int mx = Game1.getMouseX();
		int my = Game1.getMouseY();

		// Outer frame with Stardew texture box + inner dark GBA surface
		IClickableMenu.drawTextureBox(b, xPositionOnScreen - 6, yPositionOnScreen - 6, MenuWidth + 12, MenuHeight + 12, Color.White);
		Rectangle mainBox = new Rectangle(xPositionOnScreen + 6, yPositionOnScreen + 6, MenuWidth - 12, MenuHeight - 12);
		PokemonUI.DrawRoundedRect(b, mainBox, new Color(24, 30, 44), 6);

		// Top Navigation / Title Bar
		Rectangle topBar = new Rectangle(xPositionOnScreen + 14, yPositionOnScreen + 10, MenuWidth - 28, 44);
		PokemonUI.DrawRoundedRect(b, topBar, new Color(36, 46, 68), 6);

		if (_contextList != null && _contextList.Count > 1)
		{
			bool prevHov = _prevMonBtn.containsPoint(mx, my);
			bool nextHov = _nextMonBtn.containsPoint(mx, my);
			IClickableMenu.drawTextureBox(b, _prevMonBtn.bounds.X, _prevMonBtn.bounds.Y, _prevMonBtn.bounds.Width, _prevMonBtn.bounds.Height, prevHov ? Color.Wheat : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _prevMonBtn.label, new Rectangle(_prevMonBtn.bounds.X + 8, _prevMonBtn.bounds.Y + 4, _prevMonBtn.bounds.Width - 16, _prevMonBtn.bounds.Height - 8), prevHov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.64f, shadow: false);

			IClickableMenu.drawTextureBox(b, _nextMonBtn.bounds.X, _nextMonBtn.bounds.Y, _nextMonBtn.bounds.Width, _nextMonBtn.bounds.Height, nextHov ? Color.Wheat : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _nextMonBtn.label, new Rectangle(_nextMonBtn.bounds.X + 8, _nextMonBtn.bounds.Y + 4, _nextMonBtn.bounds.Width - 16, _nextMonBtn.bounds.Height - 8), nextHov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.64f, shadow: false);
		}

		Rectangle titleRect = new Rectangle(xPositionOnScreen + 264, yPositionOnScreen + 14, MenuWidth - 320, 36);
		string headerTitle = $"SUMÁRIO — #{_mon.Species.DexNumber:D3} {_mon.Species.Name.ToUpperInvariant()}";
		PokemonUI.Text(b, Game1.smallFont, headerTitle, titleRect, new Color(255, 225, 110), center: true, maxScale: 0.80f);

		// =========================================================================
		// LEFT COLUMN: Portrait Stage, Gender, Shiny/Mega, Types, Level/EXP, Held Item
		// =========================================================================
		int leftX = xPositionOnScreen + 18;
		int leftY = yPositionOnScreen + 60;
		int leftW = 300;
		int leftH = MenuHeight - 76;
		Rectangle leftPanel = new Rectangle(leftX, leftY, leftW, leftH);
		PokemonUI.DrawRoundedRect(b, leftPanel, new Color(32, 40, 58), 8);
		PokemonUI.DrawCardBorder(b, leftPanel, new Color(80, 110, 155), 2);

		// Nickname & Gender Row
		string nick = _mon.IsEgg ? "Ovo Pokémon" : _mon.Nickname;
		PokemonUI.Text(b, Game1.smallFont, nick, new Rectangle(leftX + 14, leftY + 10, leftW - 64, 26), Color.White, center: false, maxScale: 0.86f);

		if (!_mon.IsEgg)
		{
			string genderStr = string.IsNullOrEmpty(_mon.Gender) ? "♂" : _mon.Gender;
			Color genderCol = genderStr == "♀" ? new Color(255, 120, 175) : (genderStr == "♂" ? new Color(90, 200, 255) : Color.LightGray);
			Rectangle genderBadge = new Rectangle(leftX + leftW - 42, leftY + 10, 28, 26);
			PokemonUI.DrawRoundedRect(b, genderBadge, new Color(20, 26, 38), 5);
			PokemonUI.DrawCardBorder(b, genderBadge, genderCol, 1);
			PokemonUI.Text(b, Game1.smallFont, genderStr, genderBadge, genderCol, center: true, maxScale: 0.80f, shadow: false);
		}

		// Shiny & Mega Badges
		int badgeX = leftX + 14;
		int badgeY = leftY + 40;
		if (_mon.IsShiny)
		{
			Rectangle shinyPill = new Rectangle(badgeX, badgeY, 76, 20);
			PokemonUI.DrawRoundedRect(b, shinyPill, new Color(215, 165, 20), 4);
			PokemonUI.Text(b, Game1.smallFont, "★ SHINY", new Rectangle(badgeX + 4, badgeY + 1, 68, 18), Color.White, center: true, maxScale: 0.54f, shadow: false);
			badgeX += 82;
		}
		if (_mon.IsMegaEvolved || _mon.Species.IsMega)
		{
			Rectangle megaPill = new Rectangle(badgeX, badgeY, 76, 20);
			PokemonUI.DrawRoundedRect(b, megaPill, new Color(195, 55, 160), 4);
			PokemonUI.Text(b, Game1.smallFont, "🧬 MEGA", new Rectangle(badgeX + 4, badgeY + 1, 68, 18), Color.White, center: true, maxScale: 0.54f, shadow: false);
			badgeX += 82;
		}
		if (_mon.Status != PokemonStatusCondition.None)
		{
			DrawStatusBadge(b, _mon.Status, badgeX, badgeY);
		}
		else
		{
			Rectangle okPill = new Rectangle(badgeX, badgeY, 80, 20);
			PokemonUI.DrawRoundedRect(b, okPill, new Color(45, 140, 75), 4);
			PokemonUI.Text(b, Game1.smallFont, "SAUDÁVEL", new Rectangle(badgeX + 4, badgeY + 1, 72, 18), Color.White, center: true, maxScale: 0.52f, shadow: false);
		}

		// Sprite Stage (190x140)
		Rectangle stageRect = new Rectangle(leftX + (leftW - 190) / 2, leftY + 66, 190, 140);
		PokemonUI.DrawRoundedRect(b, stageRect, _mon.IsShiny ? new Color(44, 42, 26) : new Color(20, 26, 38), 8);
		PokemonUI.DrawCardBorder(b, stageRect, _mon.IsShiny ? new Color(245, 205, 65) : new Color(70, 95, 135), 2);

		// Platform oval inside stage
		Rectangle platOval = new Rectangle(stageRect.X + 25, stageRect.Bottom - 30, 140, 18);
		PokemonUI.DrawRoundedRect(b, platOval, new Color(45, 62, 88), 10);

		Texture2D frontTex = _mon.GetFrontSprite(Game1.graphics.GraphicsDevice);
		if (frontTex != null)
		{
			Rectangle src = _mon.GetFrontSourceRect();
			int bottomY = _mon.GetFrontBottomY(Game1.graphics.GraphicsDevice);
			float scale = src.Width > 64 ? 1.65f : 1.95f;
			Vector2 origin = new Vector2(src.Width / 2f, bottomY + 1);
			Vector2 drawPos = new Vector2(stageRect.Center.X, platOval.Center.Y + 4);
			b.Draw(frontTex, drawPos, src, Color.White, 0f, origin, scale, SpriteEffects.None, 0.9f);
		}

		// Type Pills below stage
		int typeY = stageRect.Bottom + 8;
		if (_mon.Species.SecondaryType.HasValue)
		{
			PokemonUI.DrawTypePill(b, _mon.Species.PrimaryType, leftX + leftW / 2 - 86, typeY, 80, 22);
			PokemonUI.DrawTypePill(b, _mon.Species.SecondaryType.Value, leftX + leftW / 2 + 6, typeY, 80, 22);
		}
		else
		{
			PokemonUI.DrawTypePill(b, _mon.Species.PrimaryType, leftX + (leftW - 86) / 2, typeY, 86, 22);
		}

		// Level & EXP Bar Box
		int lvlBoxY = typeY + 28;
		Rectangle lvlBox = new Rectangle(leftX + 12, lvlBoxY, leftW - 24, 70);
		PokemonUI.DrawRoundedRect(b, lvlBox, new Color(22, 28, 42), 6);
		PokemonUI.DrawCardBorder(b, lvlBox, new Color(55, 75, 110), 1);

		PokemonUI.Text(b, Game1.smallFont, $"Nível {_mon.Level}", new Rectangle(lvlBox.X + 10, lvlBox.Y + 6, 108, 22), new Color(255, 225, 100), center: false, maxScale: 0.74f);
		PokemonUI.Text(b, Game1.smallFont, $"HP: {_mon.CurrentHP}/{_mon.MaxHP}", new Rectangle(lvlBox.X + 120, lvlBox.Y + 6, lvlBox.Width - 130, 22), Color.LightGreen, center: false, maxScale: 0.70f);

		PokemonUI.DrawHpBar(b, new Rectangle(lvlBox.X + 10, lvlBox.Y + 30, lvlBox.Width - 20, 10), _mon.HpRatio, showBorder: true);
		float expRatio = _mon.ExpToNextLevel > 0 ? Math.Clamp((float)_mon.CurrentExp / _mon.ExpToNextLevel, 0f, 1f) : 0f;
		PokemonUI.Text(b, Game1.smallFont, $"EXP: {_mon.CurrentExp}/{_mon.ExpToNextLevel}", new Rectangle(lvlBox.X + 10, lvlBox.Y + 45, 134, 20), new Color(150, 210, 255), center: false, maxScale: 0.56f, shadow: false);
		PokemonUI.DrawExpBar(b, new Rectangle(lvlBox.X + 148, lvlBox.Y + 51, lvlBox.Width - 158, 8), expRatio);

		// Left Column Quick Summary Box (Nature, Ability, Friendship)
		int quickBoxY = lvlBox.Bottom + 8;
		Rectangle quickBox = new Rectangle(leftX + 12, quickBoxY, leftW - 24, 76);
		PokemonUI.DrawRoundedRect(b, quickBox, new Color(22, 28, 42), 6);
		PokemonUI.DrawCardBorder(b, quickBox, new Color(55, 75, 110), 1);

		PokemonUI.Text(b, Game1.smallFont, $"🌱 {_mon.Nature.GetDisplayName()}", new Rectangle(quickBox.X + 10, quickBox.Y + 6, quickBox.Width - 20, 18), new Color(130, 240, 175), center: false, maxScale: 0.74f);
		string quickAbility = !string.IsNullOrEmpty(_mon.Ability) ? PokemonAbility.GetDisplayName(_mon.Ability) : "Nenhuma";
		PokemonUI.Text(b, Game1.smallFont, $"⚡ {quickAbility}", new Rectangle(quickBox.X + 10, quickBox.Y + 28, quickBox.Width - 20, 18), new Color(110, 215, 255), center: false, maxScale: 0.74f);
		PokemonUI.Text(b, Game1.smallFont, $"💖 Amizade: {_mon.Friendship}/255", new Rectangle(quickBox.X + 10, quickBox.Y + 50, quickBox.Width - 20, 18), new Color(255, 160, 200), center: false, maxScale: 0.72f);

		// Held Item Button
		if (_heldItemBtn != null)
		{
			_heldItemBtn.bounds = new Rectangle(leftX + 12, quickBox.Bottom + 8, leftW - 24, 34);
			bool itemHov = _heldItemBtn.containsPoint(mx, my);
			Color itemBg = itemHov ? new Color(42, 54, 78) : new Color(26, 34, 50);
			Color itemBorder = itemHov ? Color.Gold : new Color(70, 95, 135);
			PokemonUI.DrawRoundedRect(b, _heldItemBtn.bounds, itemBg, 4);
			PokemonUI.DrawCardBorder(b, _heldItemBtn.bounds, itemBorder, 1);

			string heldName = !string.IsNullOrEmpty(_mon.HeldItem)
				? PokemonHeldItemManager.GetDisplayName(_mon.HeldItem)
				: "[Nenhum Item Equipado]";
			Color heldColor = !string.IsNullOrEmpty(_mon.HeldItem) ? new Color(255, 225, 120) : Color.Gray;

			PokemonUI.Text(b, Game1.smallFont, $"🎒 {heldName}", new Rectangle(_heldItemBtn.bounds.X + 8, _heldItemBtn.bounds.Y + 6, _heldItemBtn.bounds.Width - 16, 22), heldColor, center: true, maxScale: 0.60f, shadow: false);
		}

		// Mega Evolution / Revert / Form Switch Button
		bool isRevert = CanRevertMegaOutOfBattle();
		bool isMegaEvolve = !isRevert && CanMegaEvolveOutOfBattle(out _, out _);
		bool isFormSwitch = !isRevert && !isMegaEvolve && PokemonItemUse.CanSwitchFormInSummary(_mon);
		bool showMegaBtn = isRevert || isMegaEvolve || isFormSwitch;

		if (showMegaBtn && _revertMegaBtn != null)
		{
			int megaBtnY = _heldItemBtn != null ? _heldItemBtn.bounds.Bottom + 6 : quickBox.Bottom + 8;
			_revertMegaBtn.bounds = new Rectangle(leftX + 12, megaBtnY, leftW - 24, 34);
			_revertMegaBtn.label = isRevert ? "🧬 Destransformar Mega" : (isMegaEvolve ? "🧬 Mega Evoluir" : "🧬 Alternar Forma");
			bool revHov = _revertMegaBtn.containsPoint(mx, my);
			Color boxTint = isRevert
				? (revHov ? new Color(255, 210, 245) : Color.White)
				: (revHov ? new Color(210, 245, 255) : Color.White);
			Color txtColor = isRevert
				? (revHov ? Color.DarkMagenta : new Color(115, 20, 95))
				: (revHov ? Color.DarkBlue : new Color(20, 75, 145));
			IClickableMenu.drawTextureBox(b, _revertMegaBtn.bounds.X, _revertMegaBtn.bounds.Y, _revertMegaBtn.bounds.Width, _revertMegaBtn.bounds.Height, boxTint);
			PokemonUI.Text(b, Game1.smallFont, _revertMegaBtn.label, new Rectangle(_revertMegaBtn.bounds.X + 10, _revertMegaBtn.bounds.Y + 5, _revertMegaBtn.bounds.Width - 20, _revertMegaBtn.bounds.Height - 10), txtColor, center: true, maxScale: 0.66f, shadow: false);
		}

		// =========================================================================
		// RIGHT COLUMN: 2 Toggleable Tabs (Battle vs Trainer/Info)
		// =========================================================================
		int rightX = xPositionOnScreen + 332;
		int rightW = MenuWidth - 350;

		// Tab Buttons
		bool battleActive = _currentTab == SummaryTab.Battle;
		bool battleHov = _battleTabBtn.containsPoint(mx, my);
		Color bTabBg = battleActive ? new Color(48, 64, 96) : (battleHov ? new Color(34, 44, 66) : new Color(24, 30, 44));
		Color bTabBorder = battleActive ? Color.Gold : (battleHov ? Color.LightGoldenrodYellow : new Color(60, 80, 115));
		Color bTabTxt = battleActive ? new Color(255, 225, 110) : Color.LightGray;

		PokemonUI.DrawRoundedRect(b, _battleTabBtn.bounds, bTabBg, 6);
		PokemonUI.DrawCardBorder(b, _battleTabBtn.bounds, bTabBorder, battleActive ? 2 : 1);
		PokemonUI.Text(b, Game1.smallFont, _battleTabBtn.label, _battleTabBtn.bounds, bTabTxt, center: true, maxScale: 0.72f, shadow: false);

		bool trainerActive = _currentTab == SummaryTab.Trainer;
		bool trainerHov = _trainerTabBtn.containsPoint(mx, my);
		Color tTabBg = trainerActive ? new Color(48, 64, 96) : (trainerHov ? new Color(34, 44, 66) : new Color(24, 30, 44));
		Color tTabBorder = trainerActive ? Color.Gold : (trainerHov ? Color.LightGoldenrodYellow : new Color(60, 80, 115));
		Color tTabTxt = trainerActive ? new Color(255, 225, 110) : Color.LightGray;

		PokemonUI.DrawRoundedRect(b, _trainerTabBtn.bounds, tTabBg, 6);
		PokemonUI.DrawCardBorder(b, _trainerTabBtn.bounds, tTabBorder, trainerActive ? 2 : 1);
		PokemonUI.Text(b, Game1.smallFont, _trainerTabBtn.label, _trainerTabBtn.bounds, tTabTxt, center: true, maxScale: 0.72f, shadow: false);

		int contentY = yPositionOnScreen + 104;

		if (_currentTab == SummaryTab.Battle)
		{
			// =====================================================================
			// TAB 1: BATTLE & MOVES
			// =====================================================================
			// 1. Stats Table
			Rectangle statsPanel = new Rectangle(rightX, contentY, rightW, 230);
			PokemonUI.DrawRoundedRect(b, statsPanel, new Color(32, 40, 58), 8);
			PokemonUI.DrawCardBorder(b, statsPanel, new Color(80, 110, 155), 2);

			PokemonUI.Text(b, Game1.smallFont, "ATRIBUTO", new Rectangle(rightX + 16, contentY + 8, 120, 20), new Color(255, 215, 90), center: false, maxScale: 0.58f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, "VALOR", new Rectangle(rightX + 140, contentY + 8, 66, 20), new Color(255, 215, 90), center: true, maxScale: 0.58f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, "BARRA DE ATRIBUTO", new Rectangle(rightX + 214, contentY + 8, 180, 20), new Color(255, 215, 90), center: true, maxScale: 0.56f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, "NATUREZA", new Rectangle(rightX + 404, contentY + 8, 110, 20), new Color(255, 215, 90), center: true, maxScale: 0.56f, shadow: false);

			b.Draw(Game1.staminaRect, new Rectangle(rightX + 12, contentY + 30, rightW - 24, 1), Color.White * 0.2f);

			(string label, PokemonStat statType, int val)[] rows = new[]
			{
				("HP",         PokemonStat.HP,        _mon.MaxHP),
				("Ataque",     PokemonStat.Attack,    _mon.Attack),
				("Defesa",     PokemonStat.Defense,   _mon.Defense),
				("Atq. Esp.",  PokemonStat.SpAttack,  _mon.SpAttack),
				("Def. Esp.",  PokemonStat.SpDefense, _mon.SpDefense),
				("Velocidade", PokemonStat.Speed,     _mon.Speed)
			};

			int rowY = contentY + 36;
			foreach (var r in rows)
			{
				float mult = r.statType == PokemonStat.HP ? 1.0f : _mon.Nature.GetMultiplier(r.statType);
				Color statNameCol = mult > 1.01f ? new Color(110, 255, 140) : (mult < 0.99f ? new Color(255, 120, 120) : Color.White);
				string suffix = mult > 1.01f ? " ▲" : (mult < 0.99f ? " ▼" : "");
				string modText = mult > 1.01f ? "+10%" : (mult < 0.99f ? "-10%" : "Neutro");
				Color modCol = mult > 1.01f ? new Color(110, 255, 140) : (mult < 0.99f ? new Color(255, 120, 120) : Color.Gray);

				Rectangle rowBg = new Rectangle(rightX + 10, rowY, rightW - 20, 28);
				PokemonUI.DrawRoundedRect(b, rowBg, new Color(22, 28, 42), 4);

				PokemonUI.Text(b, Game1.smallFont, r.label + suffix, new Rectangle(rightX + 16, rowY + 3, 120, 22), statNameCol, center: false, maxScale: 0.66f, shadow: false);
				PokemonUI.Text(b, Game1.smallFont, r.val.ToString(), new Rectangle(rightX + 140, rowY + 3, 66, 22), Color.White, center: true, maxScale: 0.70f, shadow: false);

				// Proportional stat bar
				int barW = 176;
				Rectangle barBg = new Rectangle(rightX + 216, rowY + 8, barW, 12);
				PokemonUI.DrawRoundedRect(b, barBg, new Color(12, 16, 24), 3);
				float ratio = Math.Clamp(r.val / 300f, 0.06f, 1f);
				Color barCol = ratio >= 0.65f ? new Color(65, 225, 130) : (ratio >= 0.35f ? new Color(75, 175, 255) : new Color(245, 185, 65));
				PokemonUI.DrawRoundedRect(b, new Rectangle(barBg.X + 1, barBg.Y + 1, Math.Max(4, (int)((barW - 2) * ratio)), 10), barCol, 2);

				// Nature Modifier tag
				PokemonUI.Text(b, Game1.smallFont, modText, new Rectangle(rightX + 404, rowY + 3, 110, 22), modCol, center: true, maxScale: 0.62f, shadow: false);

				rowY += 31;
			}

			// 2. 4 Moves Grid
			PokemonUI.Text(b, Game1.smallFont, "GOLPES CONHECIDOS (MOVESET)", new Rectangle(rightX + 4, contentY + 236, 320, 24), new Color(255, 215, 90), center: false, maxScale: 0.68f);

			for (int i = 0; i < 4; i++)
			{
				Rectangle mRect = _moveRects[i];
				bool isHov = _hoveredMoveIndex == i;
				if (i < _mon.Moves.Count)
				{
					var mv = _mon.Moves[i];
					Color typeCol = PokemonTypeColors.Get(mv.Type);
					PokemonUI.DrawRoundedRect(b, mRect, isHov ? new Color(44, 56, 82) : new Color(30, 38, 56), 6);
					PokemonUI.DrawCardBorder(b, mRect, isHov ? Color.Gold : typeCol * 0.85f, 2);

					// Top row: Move Name + Type Pill
					PokemonUI.Text(b, Game1.smallFont, mv.Name, new Rectangle(mRect.X + 10, mRect.Y + 6, mRect.Width - 90, 22), Color.White, center: false, maxScale: 0.72f);
					PokemonUI.DrawTypePill(b, mv.Type, mRect.Right - 74, mRect.Y + 7, 64, 20);

					// Category Pill (Physical / Special / Status)
					(string catText, Color catCol) = mv.Category switch
					{
						MoveCategory.Physical => ("FÍSICO", new Color(215, 75, 45)),
						MoveCategory.Special  => ("ESPECIAL", new Color(65, 115, 215)),
						_                     => ("STATUS", new Color(120, 130, 145))
					};
					Rectangle catRect = new Rectangle(mRect.X + 10, mRect.Y + 32, 78, 18);
					PokemonUI.DrawRoundedRect(b, catRect, catCol, 3);
					PokemonUI.Text(b, Game1.smallFont, catText, new Rectangle(catRect.X + 3, catRect.Y + 1, catRect.Width - 6, catRect.Height - 2), Color.White, center: true, maxScale: 0.50f, shadow: false);

					// PP display
					PokemonUI.Text(b, Game1.smallFont, $"PP {mv.CurrentPP}/{mv.MaxPP}", new Rectangle(mRect.Right - 96, mRect.Y + 32, 86, 18), new Color(255, 230, 120), center: false, maxScale: 0.58f, shadow: false);

					// Power & Accuracy
					string pwrStr = mv.Power > 0 ? $"Poder: {mv.Power}" : "Poder: —";
					string accStr = $"Prec: {mv.Accuracy}%";
					PokemonUI.Text(b, Game1.smallFont, $"{pwrStr}  |  {accStr}", new Rectangle(mRect.X + 10, mRect.Y + 53, mRect.Width - 20, 20), new Color(205, 220, 240), center: false, maxScale: 0.58f, shadow: false);

					string effectTag = "";
					if (mv.StatusEffect != PokemonStatusCondition.None)
						effectTag = $"Efeito: {mv.StatusEffect} ({mv.StatusChance}%)";
					else if (mv.InflictsConfusion)
						effectTag = $"Efeito: Confusão ({mv.ConfusionChance}%)";
					else if (mv.DrainRatio > 0f)
						effectTag = $"Drena {(int)(mv.DrainRatio * 100)}% do dano";
					else if (mv.SelfHealRatio > 0f)
						effectTag = $"Cura {(int)(mv.SelfHealRatio * 100)}% do HP";
					else if (mv.IsHM)
						effectTag = "HM de Campo";

					if (!string.IsNullOrEmpty(effectTag))
					{
						PokemonUI.Text(b, Game1.smallFont, effectTag, new Rectangle(mRect.X + 10, mRect.Y + 74, mRect.Width - 20, 20), new Color(130, 235, 175), center: false, maxScale: 0.54f, shadow: false);
					}
				}
				else
				{
					PokemonUI.DrawRoundedRect(b, mRect, new Color(20, 25, 36), 6);
					PokemonUI.DrawCardBorder(b, mRect, new Color(45, 55, 75), 1);
					PokemonUI.Text(b, Game1.smallFont, "— Slot Vazio —", mRect, Color.Gray * 0.6f, center: true, maxScale: 0.68f, shadow: false);
				}
			}
		}
		else
		{
			// =====================================================================
			// TAB 2: TRAINING, INFO, HAPPINESS, ABILITY & GENETICS
			// =====================================================================
			// 1. Amizade & Felicidade Card (88px)
			Rectangle happyCard = new Rectangle(rightX, contentY, rightW, 88);
			PokemonUI.DrawRoundedRect(b, happyCard, new Color(32, 40, 58), 8);
			PokemonUI.DrawCardBorder(b, happyCard, new Color(80, 110, 155), 2);

			PokemonUI.Text(b, Game1.smallFont, "💖 AMIZADE & AFETO (0-255)", new Rectangle(happyCard.X + 14, happyCard.Y + 8, 260, 20), new Color(255, 215, 90), center: false, maxScale: 0.68f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, $"{_mon.Friendship} / 255", new Rectangle(happyCard.Right - 120, happyCard.Y + 8, 106, 20), new Color(255, 175, 210), center: true, maxScale: 0.72f);

			// 5 Hearts Graphic Meter
			int heartsStartX = happyCard.X + 14;
			int heartsY = happyCard.Y + 34;
			for (int h = 0; h < 5; h++)
			{
				float heartThreshold = (h + 1) * 51f;
				bool full = _mon.Friendship >= heartThreshold;
				bool partial = !full && _mon.Friendship >= (h * 51f + 10f);
				Color heartCol = full ? new Color(255, 80, 120) : (partial ? new Color(255, 160, 180) : new Color(60, 75, 95));
				Rectangle heartBox = new Rectangle(heartsStartX + h * 24, heartsY, 20, 20);
				PokemonUI.DrawRoundedRect(b, heartBox, heartCol, 4);
				PokemonUI.Text(b, Game1.smallFont, "♥", heartBox, Color.White, center: true, maxScale: 0.65f, shadow: false);
			}

			// Friendship Status Description
			string friendStatus = _mon.Friendship switch
			{
				>= 250 => "Amor Incondicional! Pronto para evoluções por amizade.",
				>= 200 => "Lealdade inabalável e afeição máxima pelo treinador!",
				>= 150 => "Muito apegado a você e um companheiro leal em combate.",
				>= 100 => "Amigável e confiante na sua liderança.",
				>= 50  => "Neutro, ainda se adaptando à rotina da fazenda.",
				_      => "Desconfiado e tímido. Passe mais tempo juntos!"
			};
			Color friendStatusCol = _mon.Friendship >= 220 ? new Color(130, 245, 175) : (_mon.Friendship >= 100 ? new Color(170, 220, 255) : new Color(255, 200, 140));
			PokemonUI.Text(b, Game1.smallFont, friendStatus, new Rectangle(happyCard.X + 142, heartsY + 1, happyCard.Width - 156, 22), friendStatusCol, center: false, maxScale: 0.60f, shadow: false);

			// 2. Habilidade Card (Spacious & Cleanly Rendered) (122px)
			Rectangle abilityCard = new Rectangle(rightX, contentY + 96, rightW, 122);
			PokemonUI.DrawRoundedRect(b, abilityCard, new Color(32, 40, 58), 8);
			PokemonUI.DrawCardBorder(b, abilityCard, new Color(80, 110, 155), 2);

			string abName = !string.IsNullOrEmpty(_mon.Ability) ? PokemonAbility.GetDisplayName(_mon.Ability) : "Nenhuma";
			string abDesc = !string.IsNullOrEmpty(_mon.Ability) ? PokemonAbility.GetDescription(_mon.Ability) : "Nenhuma habilidade especial ativa.";

			PokemonUI.Text(b, Game1.smallFont, "⚡ HABILIDADE:", new Rectangle(abilityCard.X + 14, abilityCard.Y + 8, 120, 22), new Color(255, 215, 90), center: false, maxScale: 0.68f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, abName, new Rectangle(abilityCard.X + 138, abilityCard.Y + 8, abilityCard.Width - 152, 22), new Color(110, 220, 255), center: false, maxScale: 0.80f);

			// Crisp Description Box: generous padding, NO heavy drop-shadow blur!
			Rectangle abDescBox = new Rectangle(abilityCard.X + 12, abilityCard.Y + 34, abilityCard.Width - 24, 78);
			PokemonUI.DrawRoundedRect(b, abDescBox, new Color(16, 22, 34), 6);
			PokemonUI.DrawCardBorder(b, abDescBox, new Color(60, 80, 110), 1);

			string wrappedDesc = Game1.parseText(abDesc, Game1.smallFont, (int)((abDescBox.Width - 24) / 0.74f));
			b.DrawString(Game1.smallFont, wrappedDesc, new Vector2(abDescBox.X + 12, abDescBox.Y + 10), Color.White, 0f, Vector2.Zero, 0.74f, SpriteEffects.None, 0.9f);

			// 3. Natureza Card (56px)
			Rectangle natCard = new Rectangle(rightX, contentY + 226, rightW, 56);
			PokemonUI.DrawRoundedRect(b, natCard, new Color(32, 40, 58), 8);
			PokemonUI.DrawCardBorder(b, natCard, new Color(80, 110, 155), 2);

			PokemonUI.Text(b, Game1.smallFont, "🌱 NATUREZA:", new Rectangle(natCard.X + 14, natCard.Y + 8, 120, 20), new Color(255, 215, 90), center: false, maxScale: 0.68f, shadow: false);
			PokemonUI.Text(b, Game1.smallFont, _mon.Nature.GetDisplayName(), new Rectangle(natCard.X + 138, natCard.Y + 8, 180, 20), new Color(130, 245, 175), center: false, maxScale: 0.76f);

			string natDetail = _mon.Nature.GetDescription();
			PokemonUI.Text(b, Game1.smallFont, natDetail, new Rectangle(natCard.X + 14, natCard.Y + 30, natCard.Width - 28, 20), new Color(205, 225, 245), center: false, maxScale: 0.62f, shadow: false);

			// 4. Genética (IVs) & Esforço (EVs) Panel (190px)
			Rectangle geneCard = new Rectangle(rightX, contentY + 290, rightW, 190);
			PokemonUI.DrawRoundedRect(b, geneCard, new Color(32, 40, 58), 8);
			PokemonUI.DrawCardBorder(b, geneCard, new Color(80, 110, 155), 2);

			PokemonUI.Text(b, Game1.smallFont, "🧬 VALORES INDIVIDUAIS (IV) & ESFORÇO (EV)", new Rectangle(geneCard.X + 14, geneCard.Y + 8, 380, 20), new Color(255, 215, 90), center: false, maxScale: 0.68f, shadow: false);
			b.Draw(Game1.staminaRect, new Rectangle(geneCard.X + 12, geneCard.Y + 30, geneCard.Width - 24, 1), Color.White * 0.2f);

			(string stName, int ivVal, int evVal)[] geneRows = new[]
			{
				("HP",         _mon.IV_HP,        _mon.EV_HP),
				("Ataque",     _mon.IV_Attack,    _mon.EV_Attack),
				("Defesa",     _mon.IV_Defense,   _mon.EV_Defense),
				("Atq. Esp.",  _mon.IV_SpAttack,  _mon.EV_SpAttack),
				("Def. Esp.",  _mon.IV_SpDefense, _mon.EV_SpDefense),
				("Velocidade", _mon.IV_Speed,     _mon.EV_Speed)
			};

			int gy = geneCard.Y + 36;
			for (int idx = 0; idx < geneRows.Length; idx++)
			{
				var g = geneRows[idx];
				int col = idx % 2;
				int row = idx / 2;
				int cellX = geneCard.X + 12 + col * ((geneCard.Width - 32) / 2 + 8);
				int cellY = gy + row * 34;
				int cellW = (geneCard.Width - 32) / 2;

				Rectangle cBox = new Rectangle(cellX, cellY, cellW, 30);
				PokemonUI.DrawRoundedRect(b, cBox, new Color(22, 28, 42), 4);

				// Stat name
				PokemonUI.Text(b, Game1.smallFont, g.stName, new Rectangle(cellX + 8, cellY + 5, 80, 20), Color.White, center: false, maxScale: 0.62f, shadow: false);

				// IV Pill
				Rectangle ivPill = new Rectangle(cellX + 90, cellY + 4, 68, 22);
				PokemonUI.DrawRoundedRect(b, ivPill, new Color(14, 18, 28), 3);
				Color ivColor = g.ivVal == 31 ? new Color(255, 215, 75) : (g.ivVal >= 25 ? new Color(135, 235, 155) : Color.LightGray);
				PokemonUI.Text(b, Game1.smallFont, $"IV {g.ivVal}", ivPill, ivColor, center: true, maxScale: 0.62f, shadow: false);

				// EV Pill
				Rectangle evPill = new Rectangle(cellX + 164, cellY + 4, 76, 22);
				PokemonUI.DrawRoundedRect(b, evPill, new Color(14, 18, 28), 3);
				Color evColor = g.evVal >= 252 ? new Color(110, 225, 255) : (g.evVal > 0 ? new Color(180, 225, 255) : Color.Gray);
				PokemonUI.Text(b, Game1.smallFont, $"EV {g.evVal}", evPill, evColor, center: true, maxScale: 0.62f, shadow: false);
			}

			// Footer Summary in Genetics Card
			int footerY = geneCard.Bottom - 44;
			b.Draw(Game1.staminaRect, new Rectangle(geneCard.X + 12, footerY, geneCard.Width - 24, 1), Color.White * 0.2f);
			PokemonUI.Text(b, Game1.smallFont, $"IV Total: {_mon.TotalIVs}/186 ({_mon.IVRating})", new Rectangle(geneCard.X + 14, footerY + 6, 240, 20), new Color(255, 225, 110), center: false, maxScale: 0.65f);
			PokemonUI.Text(b, Game1.smallFont, $"EV Total: {_mon.TotalEVs}/510  |  Treinador: {_mon.OriginalTrainer}", new Rectangle(geneCard.X + 260, footerY + 6, geneCard.Width - 274, 20), new Color(170, 210, 255), center: false, maxScale: 0.65f);
		}

		// Tooltip on Hover for Held Item
		if (_heldItemBtn != null && _heldItemBtn.containsPoint(mx, my))
		{
			string tip;
			if (!string.IsNullOrEmpty(_mon.HeldItem))
			{
				string disp = PokemonHeldItemManager.GetDisplayName(_mon.HeldItem);
				string desc = PokemonHeldItemManager.GetDescription(_mon.HeldItem);
				tip = $"{disp}\n{desc}\n\n[Clique esquerdo para desequipar ou trocar pelo item na mão]\n[Clique direito para alternar itens da mochila]";
			}
			else
			{
				tip = "Nenhum item equipado.\n[Clique com um item selecionado na mão para equipar]\n[Clique direito para selecionar da mochila]";
			}
			drawHoverText(b, tip, Game1.smallFont);
		}

		base.draw(b);
		drawMouse(b);
	}
}
