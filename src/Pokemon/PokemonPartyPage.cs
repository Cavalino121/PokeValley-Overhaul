using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

public class PokemonPartyPage : IClickableMenu
{
	private readonly List<ClickableComponent> _renameButtons = new();
	private readonly List<ClickableComponent> _activateButtons = new();
	private ClickableComponent _pcButton;
	private ClickableComponent _pokedexButton;
	private int _selectedSlot = 0;
	private static PlayerParty Party => PlayerParty.GetForFarmer(Game1.player);

	// Authentic Pokemon GBA Palette
	private static readonly Color GbaBorder = new Color(24, 48, 56);
	private static readonly Color GbaHeader = new Color(36, 92, 80);
	private static readonly Color GbaBg = new Color(232, 244, 240);
	private static readonly Color GbaActiveBg = new Color(255, 252, 224);
	private static readonly Color GbaCardBg = new Color(246, 252, 249);
	private static readonly Color GbaFaintedBg = new Color(255, 238, 238);

	public PokemonPartyPage(int x, int y, int width, int height)
		: base(x, y, width, height)
	{
		UpdateLayout();
	}

	private static bool CanAccessPC()
	{
		if (Game1.currentLocation is StardewValley.Locations.FarmHouse)
			return true;
		if (Game1.currentLocation != null)
		{
			Vector2 playerTile = Game1.player.Tile;
			foreach (var obj in Game1.currentLocation.Objects.Values)
			{
				if (obj != null && (obj.QualifiedItemId == "(O)PokemonPC" || obj.QualifiedItemId == "(BC)PokemonPC" || obj.ItemId == "PokemonPC"))
				{
					if (Vector2.Distance(playerTile, obj.TileLocation) <= 4.5f)
						return true;
				}
			}
		}
		return false;
	}

	public void UpdateLayout()
	{
		_renameButtons.Clear();
		_activateButtons.Clear();

		var team = Party.Team;
		int startX = xPositionOnScreen + 32;
		int startY = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 32;
		int cardWidth = width - 64;
		int cardHeight = 80;
		int gap = 6;

		int bannerY = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder - 4;
		_pokedexButton = new ClickableComponent(new Rectangle(startX + cardWidth - 276, bannerY + 2, 130, 26), "open_pokedex", "📖 Pokédex");

		if (CanAccessPC())
		{
			_pcButton = new ClickableComponent(new Rectangle(startX + cardWidth - 140, bannerY + 2, 136, 26), "open_pc", "Acessar PC");
		}
		else
		{
			_pcButton = null;
		}

		int btnW = 110;
		int btnH = 28;
		int btnX = startX + cardWidth - btnW - 14;

		for (int i = 0; i < 6; i++)
		{
			int cy = startY + i * (cardHeight + gap);
			if (i < team.Count)
			{
				_activateButtons.Add(new ClickableComponent(new Rectangle(btnX, cy + 10, btnW, btnH), i.ToString(), (i == Party.ActiveIndex) ? "★ Ativo" : "Tornar Ativo"));
				_renameButtons.Add(new ClickableComponent(new Rectangle(btnX, cy + 44, btnW, btnH), i.ToString(), "Renomear"));
			}
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (_pokedexButton != null && _pokedexButton.containsPoint(x, y))
		{
			Game1.playSound("bigSelect");
			Game1.activeClickableMenu = new PokemonPokedexMenu();
			return;
		}

		if (_pcButton != null && _pcButton.containsPoint(x, y))
		{
			if (CanAccessPC())
			{
				Game1.playSound("computer");
				Game1.activeClickableMenu = new PokemonStorageMenu();
				return;
			}
			else
			{
				Game1.showRedMessage("Aproxime-se de um PC Pokémon ou entre na sua casa para acessar!");
				return;
			}
		}

		var team = Party.Team;

		// 1. Rename Buttons
		for (int i = 0; i < _renameButtons.Count; i++)
		{
			if (_renameButtons[i].containsPoint(x, y) && i < team.Count)
			{
				var p = team[i];
				Game1.playSound("select");

				Game1.activeClickableMenu = new NamingMenu(newName =>
				{
					Game1.keyboardDispatcher.Subscriber = null;
					if (!string.IsNullOrWhiteSpace(newName))
					{
						p.Nickname = newName.Trim();
						Game1.addHUDMessage(new HUDMessage($"Pokémon renomeado para {p.Nickname}!", 2));
					}
					Game1.activeClickableMenu = new GameMenu(GameMenu.pokemonTab);
				}, "Renomear " + p.Species.Name, p.Nickname);
				return;
			}
		}

		// 2. Activate Buttons
		for (int i = 0; i < _activateButtons.Count; i++)
		{
			if (_activateButtons[i].containsPoint(x, y) && i < team.Count)
			{
				if (team[i].IsFainted)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage($"{team[i].Nickname} está desmaiado e não pode ser ativado!");
					return;
				}
				Party.SetActive(i);
				UpdateLayout();
				return;
			}
		}

		// 3. Card Selection
		int startX = xPositionOnScreen + 32;
		int startY = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 32;
		int cardWidth = width - 64;
		int cardHeight = 80;
		int gap = 6;

		for (int i = 0; i < team.Count; i++)
		{
			Rectangle r = new Rectangle(startX, startY + i * (cardHeight + gap), cardWidth, cardHeight);
			if (r.Contains(x, y))
			{
				if (team[i].IsFainted)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage($"{team[i].Nickname} está desmaiado e não pode ser ativado!");
					return;
				}
				_selectedSlot = i;
				Party.SetActive(i);
				Game1.playSound("smallSelect");
				UpdateLayout();
				return;
			}
		}
	}

	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		var team = Party.Team;
		int startX = xPositionOnScreen + 32;
		int startY = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 32;
		int cardWidth = width - 64;
		int cardHeight = 80;
		int gap = 6;

		for (int i = 0; i < team.Count; i++)
		{
			Rectangle r = new Rectangle(startX, startY + i * (cardHeight + gap), cardWidth, cardHeight);
			if (r.Contains(x, y))
			{
				Game1.activeClickableMenu = new PokemonSummaryMenu(team[i], team, Game1.activeClickableMenu);
				return;
			}
		}
	}

	public override bool areGamePadControlsImplemented() => true;

	public override void receiveGamePadButton(Buttons button)
	{
		var team = Party.Team;
		if (team.Count == 0) return;

		if (button == Buttons.DPadUp || button == Buttons.LeftThumbstickUp)
		{
			_selectedSlot = Math.Max(0, _selectedSlot - 1);
			Game1.playSound("shiny");
			return;
		}
		if (button == Buttons.DPadDown || button == Buttons.LeftThumbstickDown)
		{
			_selectedSlot = Math.Min(team.Count - 1, _selectedSlot + 1);
			Game1.playSound("shiny");
			return;
		}
		if (button == Buttons.A)
		{
			if (_selectedSlot >= 0 && _selectedSlot < team.Count)
			{
				if (team[_selectedSlot].IsFainted)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage($"{team[_selectedSlot].Nickname} está desmaiado e não pode ser ativado!");
					return;
				}
				Party.SetActive(_selectedSlot);
				Game1.playSound("smallSelect");
				UpdateLayout();
			}
			return;
		}
		if (button == Buttons.X)
		{
			if (_selectedSlot >= 0 && _selectedSlot < team.Count)
			{
				Game1.activeClickableMenu = new PokemonSummaryMenu(team[_selectedSlot], team, Game1.activeClickableMenu);
			}
			return;
		}
		if (button == Buttons.Y)
		{
			if (CanAccessPC())
			{
				Game1.playSound("computer");
				Game1.activeClickableMenu = new PokemonStorageMenu();
			}
			else
			{
				Game1.playSound("bigSelect");
				Game1.activeClickableMenu = new PokemonPokedexMenu();
			}
			return;
		}
	}

	public override void draw(SpriteBatch b)
	{
		UpdateLayout();
		var team = Party.Team;
		int startX = xPositionOnScreen + 32;
		int startY = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 32;
		int cardWidth = width - 64;
		int cardHeight = 80;
		int gap = 6;

		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		// Header Banner (Pokemon GBA emerald banner)
		int bannerY = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder - 4;
		Rectangle bannerRect = new Rectangle(startX, bannerY, cardWidth, 30);
		b.Draw(Game1.staminaRect, bannerRect, GbaHeader);
		PokemonUI.Border(b, bannerRect, 1, Color.White * 0.35f);
		PokemonUI.Text(b, Game1.smallFont, "EQUIPE POKÉMON", new Rectangle(startX + 12, bannerY + 2, 200, 26), Color.White, center: false, maxScale: 0.85f);
		PokemonUI.Text(b, Game1.tinyFont, $"Vagas: {team.Count}/6  |  Alterne com [R]", new Rectangle(startX + 220, bannerY + 5, 240, 20), Color.LightGreen, center: false, maxScale: 0.76f);

		if (_pokedexButton != null)
		{
			bool dexHov = _pokedexButton.containsPoint(mouseX, mouseY);
			b.Draw(Game1.staminaRect, _pokedexButton.bounds, dexHov ? new Color(255, 248, 180) : new Color(36, 110, 140));
			PokemonUI.Border(b, _pokedexButton.bounds, 1, Color.White * 0.5f);
			PokemonUI.Text(b, Game1.tinyFont, "📖 Pokédex", _pokedexButton.bounds, dexHov ? Color.DarkSlateGray : Color.White, center: true, maxScale: 0.82f);
		}

		if (_pcButton != null)
		{
			bool pcHov = _pcButton.containsPoint(mouseX, mouseY);
			b.Draw(Game1.staminaRect, _pcButton.bounds, pcHov ? new Color(255, 248, 180) : new Color(48, 120, 100));
			PokemonUI.Border(b, _pcButton.bounds, 1, Color.White * 0.5f);
			PokemonUI.Text(b, Game1.tinyFont, "💻 Acessar PC", _pcButton.bounds, pcHov ? Color.DarkSlateGray : Color.White, center: true, maxScale: 0.82f);
		}

		if (team.Count == 0)
		{
			string emptyMsg = "Você ainda não possui Pokémon na equipe.\nFale com qualquer morador de Vila Pelicanos para escolher seu inicial!";
			Vector2 size = Game1.dialogueFont.MeasureString(emptyMsg);
			Utility.drawTextWithShadow(b, emptyMsg, Game1.dialogueFont, new Vector2(xPositionOnScreen + (width - size.X) / 2, yPositionOnScreen + height / 2 - 20), Game1.textColor);
			return;
		}

		for (int i = 0; i < 6; i++)
		{
			int cy = startY + i * (cardHeight + gap);
			Rectangle cardBounds = new Rectangle(startX, cy, cardWidth, cardHeight);

			if (i < team.Count)
			{
				var p = team[i];
				bool isActive = (i == Party.ActiveIndex);
				bool isHovered = cardBounds.Contains(mouseX, mouseY);

				// Background capsule
				Color bgColor = p.IsFainted ? GbaFaintedBg : (isActive ? GbaActiveBg : (isHovered ? Color.White : GbaCardBg));
				b.Draw(Game1.staminaRect, cardBounds, bgColor);

				Color borderColor = p.IsFainted ? new Color(180, 50, 50) : (isActive ? new Color(36, 120, 80) : new Color(160, 190, 185));
				int borderThickness = isActive ? 2 : 1;
				PokemonUI.Border(b, cardBounds, borderThickness, borderColor);

				if ((Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected) && _selectedSlot == i)
				{
					float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 6f);
					int exp = pulse > 0 ? 3 : 2;
					Rectangle glow = new Rectangle(cardBounds.X - exp, cardBounds.Y - exp, cardBounds.Width + exp * 2, cardBounds.Height + exp * 2);
					PokemonUI.Border(b, glow, 3, Color.Gold);
				}

				// Left indicator stripe for active Pokemon
				if (isActive)
				{
					b.Draw(Game1.staminaRect, new Rectangle(cardBounds.X, cardBounds.Y, 5, cardBounds.Height), new Color(36, 140, 90));
				}

				// Slot Number Badge
				Rectangle slotBadge = new Rectangle(startX + 8, cy + 8, 22, 20);
				b.Draw(Game1.staminaRect, slotBadge, isActive ? new Color(36, 120, 80) : new Color(140, 165, 160));
				PokemonUI.Text(b, Game1.tinyFont, $"#{i + 1}", slotBadge, Color.White, center: true, maxScale: 0.70f, shadow: false);

				// Animated 32x32 Pokemon Icon (scaled 1.5f = 48x48)
				Texture2D icon = p.GetIconSprite(Game1.graphics.GraphicsDevice) ?? p.GetFrontSprite(Game1.graphics.GraphicsDevice);
				if (icon != null)
				{
					Rectangle srcRect = p.GetIconSourceRect();
					b.Draw(icon, new Vector2(startX + 34, cy + 16), srcRect, p.IsFainted ? Color.DarkGray : Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0.86f);
				}

				// Col 2: Name, Level, Type Pills & Subtitle
				int col2X = startX + 96;
				string title = $"{p.Nickname} (Lv.{p.Level})";
				PokemonUI.Text(b, Game1.smallFont, title, new Rectangle(col2X, cy + 8, 175, 24), p.IsFainted ? Color.DarkRed : (isActive ? Color.DarkGreen : Game1.textColor), maxScale: 0.85f);

				// Type Pills
				DrawTypePill(b, p.Species.PrimaryType, col2X, cy + 33);
				int p1W = (int)(Game1.tinyFont.MeasureString(p.Species.PrimaryType.ToString()).X * 0.60f) + 12;
				if (p.Species.SecondaryType.HasValue)
				{
					DrawTypePill(b, p.Species.SecondaryType.Value, col2X + p1W, cy + 33);
				}

				// Nature and IVs subtitle
				string subStr = p.IsFainted ? "Desmaiado" : $"{p.Nature} • IVs: {p.TotalIVs}/186 ({p.IVRating})";
				PokemonUI.Text(b, Game1.tinyFont, subStr, new Rectangle(col2X, cy + 54, 185, 18), p.IsFainted ? Color.DarkRed : Color.DarkSlateBlue, maxScale: 0.68f);

				// Col 3: HP and EXP GBA Bars
				int hpColX = startX + 285;
				int hpBarW = 120;
				int hpBarY = cy + 16;
				float hpRatio = (float)p.CurrentHP / Math.Max(1, p.MaxHP);

				if (p.IsFainted)
				{
					Rectangle fBadge = new Rectangle(hpColX, hpBarY - 2, hpBarW + 24, 16);
					b.Draw(Game1.staminaRect, fBadge, new Color(175, 40, 40));
					PokemonUI.Border(b, fBadge, 1, new Color(120, 20, 20));
					PokemonUI.Text(b, Game1.tinyFont, "DESMAIADO (0 HP)", fBadge, Color.White, center: true, maxScale: 0.72f);
					PokemonUI.Text(b, Game1.tinyFont, "Necessita de cura", new Rectangle(hpColX, cy + 28, hpBarW + 24, 16), Color.DarkRed, center: true, maxScale: 0.70f);
				}
				else
				{
					PokemonUI.HpTag(b, new Rectangle(hpColX, hpBarY - 2, 22, 14));
					PokemonUI.HpBar(b, new Rectangle(hpColX + 24, hpBarY, hpBarW, 10), hpRatio);
					string hpText = $"{p.CurrentHP}/{p.MaxHP} HP";
					PokemonUI.Text(b, Game1.tinyFont, hpText, new Rectangle(hpColX + 24, cy + 28, hpBarW, 16), Color.DarkGreen, maxScale: 0.75f);
				}

				// EXP Bar
				int expBarY = cy + 46;
				float expRatio = (float)p.CurrentExp / Math.Max(1, p.ExpToNextLevel);
				PokemonUI.ExpBar(b, new Rectangle(hpColX + 24, expBarY, hpBarW, 6), expRatio);
				PokemonUI.Text(b, Game1.tinyFont, $"EXP {p.CurrentExp}/{p.ExpToNextLevel}", new Rectangle(hpColX + 24, cy + 54, hpBarW, 16), Color.Navy, maxScale: 0.70f);

				// Col 4: Moves List (2 cols x 2 rows)
				int movesStartX = startX + 440;
				for (int m = 0; m < Math.Min(4, p.Moves.Count); m++)
				{
					var move = p.Moves[m];
					int mx = movesStartX + (m % 2) * 116;
					int my = cy + 10 + (m / 2) * 32;

					// Tiny colored bullet indicating move type
					Color moveTypeCol = PokemonTypeColors.Get(move.Type);
					b.Draw(Game1.staminaRect, new Rectangle(mx, my + 4, 6, 6), moveTypeCol);

					PokemonUI.Text(b, Game1.tinyFont, move.Name, new Rectangle(mx + 9, my, 105, 16), Color.MidnightBlue, maxScale: 0.74f);
					string ppStr = $"PP {move.CurrentPP}/{move.MaxPP}";
					if (move.IsHM) ppStr += " [HM]";
					PokemonUI.Text(b, Game1.tinyFont, ppStr, new Rectangle(mx + 9, my + 14, 105, 14), (move.CurrentPP > 0) ? Color.DarkSlateGray : Color.Red, maxScale: 0.68f);
				}

				// Col 5: Activate Button
				if (i < _activateButtons.Count)
				{
					var aBtn = _activateButtons[i];
					bool aHov = aBtn.containsPoint(mouseX, mouseY);
					Color btnFill = p.IsFainted ? new Color(248, 225, 225) : (isActive ? new Color(210, 245, 225) : (aHov ? Color.PaleGoldenrod : Color.White));
					b.Draw(Game1.staminaRect, aBtn.bounds, btnFill);
					PokemonUI.Border(b, aBtn.bounds, 1, p.IsFainted ? new Color(180, 80, 80) : (isActive ? new Color(36, 120, 80) : Color.DarkGray));
					string btnText = p.IsFainted ? "Desmaiado" : (isActive ? "★ ATIVO" : "Tornar Ativo");
					Color textColor = p.IsFainted ? new Color(160, 40, 40) : (isActive ? Color.DarkGreen : Color.DarkSlateGray);
					PokemonUI.Text(b, Game1.tinyFont, btnText, aBtn.bounds, textColor, center: true, maxScale: 0.80f);
				}

				// Col 5: Rename Button
				if (i < _renameButtons.Count)
				{
					var rBtn = _renameButtons[i];
					bool rHov = rBtn.containsPoint(mouseX, mouseY);
					Color rFill = rHov ? Color.PaleGoldenrod : Color.White;
					b.Draw(Game1.staminaRect, rBtn.bounds, rFill);
					PokemonUI.Border(b, rBtn.bounds, 1, Color.DarkGray);
					PokemonUI.Text(b, Game1.tinyFont, "Renomear", rBtn.bounds, Color.DarkSlateGray, center: true, maxScale: 0.80f);
				}
			}
			else
			{
				// Empty Slot
				b.Draw(Game1.staminaRect, cardBounds, new Color(225, 238, 234) * 0.5f);
				PokemonUI.Border(b, cardBounds, 1, new Color(180, 205, 200) * 0.7f);
				PokemonUI.Text(b, Game1.smallFont, $"-- Vaga #{i + 1} Vazia --", cardBounds, Color.DarkSlateGray * 0.6f, center: true, maxScale: 0.82f);
			}
		}

		drawMouse(b);
	}

	private static void DrawTypePill(SpriteBatch b, PokemonType type, int px, int py)
	{
		Color typeCol = PokemonTypeColors.Get(type);
		string name = type.ToString();
		Vector2 textSz = Game1.tinyFont.MeasureString(name) * 0.60f;
		int pillW = (int)textSz.X + 8;
		int pillH = 15;
		b.Draw(Game1.staminaRect, new Rectangle(px, py, pillW, pillH), typeCol);
		PokemonUI.Border(b, new Rectangle(px, py, pillW, pillH), 1, Color.White * 0.4f);
		PokemonUI.Text(b, Game1.tinyFont, name, new Rectangle(px, py, pillW, pillH), Color.White, center: true, maxScale: 0.60f, shadow: false);
	}
}
