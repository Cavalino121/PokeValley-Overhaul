using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

public class PokemonDaycareMenu : IClickableMenu
{
	private ClickableComponent _parent1ActionButton;
	private ClickableComponent _parent2ActionButton;
	private ClickableComponent _collectEggButton;
	private ClickableTextureComponent _closeButton;

	private bool _selectingParent1 = false;
	private bool _selectingParent2 = false;
	private readonly List<ClickableComponent> _partySelectButtons = new();

	private static PlayerParty Party => PlayerParty.GetForFarmer(Game1.player);

	public PokemonDaycareMenu()
		: base(0, 0, 780, 520)
	{
		xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
		yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

		_closeButton = new ClickableTextureComponent(
			new Rectangle(xPositionOnScreen + width - 44, yPositionOnScreen + 16, 36, 36),
			Game1.mouseCursors,
			new Rectangle(337, 494, 12, 12),
			3.0f
		);

		SetupComponents();
	}

	private void SetupComponents()
	{
		int startX = xPositionOnScreen + 36;
		int startY = yPositionOnScreen + 70;
		int cardW = 330;
		int cardH = 200;

		// Button for Parent 1
		int p1BtnY = startY + cardH - 52;
		string p1Label = (PokemonBreeding.DaycareParent1 == null) ? "➕ Depositar Pokémon" : "↩️ Retirar Pokémon";
		_parent1ActionButton = new ClickableComponent(new Rectangle(startX + 20, p1BtnY, cardW - 40, 40), "parent1_action", p1Label);

		// Button for Parent 2
		int p2X = startX + cardW + 48;
		string p2Label = (PokemonBreeding.DaycareParent2 == null) ? "➕ Depositar Pokémon" : "↩️ Retirar Pokémon";
		_parent2ActionButton = new ClickableComponent(new Rectangle(p2X + 20, p1BtnY, cardW - 40, 40), "parent2_action", p2Label);

		// Collect Egg Button (Center Bottom)
		int eggY = startY + cardH + 74;
		_collectEggButton = new ClickableComponent(new Rectangle(startX + 140, eggY, width - 352, 44), "collect_egg", "🧺 Coletar Ovo Pokémon");

		// Party picker buttons
		_partySelectButtons.Clear();
		if (_selectingParent1 || _selectingParent2)
		{
			var team = Party.Team;
			int pickerY = yPositionOnScreen + 90;
			int btnH = 50;
			for (int i = 0; i < team.Count; i++)
			{
				_partySelectButtons.Add(new ClickableComponent(
					new Rectangle(xPositionOnScreen + 100, pickerY + i * (btnH + 8), width - 200, btnH),
					i.ToString(),
					team[i].Nickname
				));
			}
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);

		if (_closeButton.containsPoint(x, y))
		{
			if (_selectingParent1 || _selectingParent2)
			{
				_selectingParent1 = false;
				_selectingParent2 = false;
				SetupComponents();
				Game1.playSound("smallSelect");
				return;
			}
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
			return;
		}

		// Party picker selection
		if (_selectingParent1 || _selectingParent2)
		{
			for (int i = 0; i < _partySelectButtons.Count; i++)
			{
				if (_partySelectButtons[i].containsPoint(x, y))
				{
					var team = Party.Team;
					if (i < team.Count)
					{
						var chosen = team[i];
						if (chosen.IsEgg)
						{
							Game1.playSound("cancel");
							Game1.showRedMessage("Você não pode depositar um Ovo na Creche!");
							return;
						}
						if (team.Count <= 1)
						{
							Game1.playSound("cancel");
							Game1.showRedMessage("Você não pode depositar seu último Pokémon!");
							return;
						}

						Party.RemovePokemon(chosen);
						if (_selectingParent1)
						{
							PokemonBreeding.DaycareParent1 = chosen;
						}
						else
						{
							PokemonBreeding.DaycareParent2 = chosen;
						}

						Party.SaveParty();
						PokemonSaveStore.SaveForFarmer(Game1.player?.UniqueMultiplayerID ?? 0L, PokemonSaveSection.Daycare);
						Game1.playSound("reward");
						Game1.addHUDMessage(new HUDMessage($"{chosen.Nickname} foi deixado na Creche aos cuidados da Marnie!", 1));
						_selectingParent1 = false;
						_selectingParent2 = false;
						SetupComponents();
						return;
					}
				}
			}
			return;
		}

		// Parent 1 Action (Deposit / Withdraw)
		if (_parent1ActionButton.containsPoint(x, y))
		{
			if (PokemonBreeding.DaycareParent1 == null)
			{
				if (Party.Team.Count == 0)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage("Você não possui Pokémon na equipe para depositar!");
					return;
				}
				_selectingParent1 = true;
				_selectingParent2 = false;
				Game1.playSound("smallSelect");
				SetupComponents();
				return;
			}
			else
			{
				if (Party.Team.Count >= 6)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage("Sua equipe está cheia (máximo 6 Pokémon)!");
					return;
				}
				var mon = PokemonBreeding.DaycareParent1;
				PokemonBreeding.DaycareParent1 = null;
				Party.AddPokemon(mon);
				Party.SaveParty();
				PokemonSaveStore.SaveForFarmer(Game1.player?.UniqueMultiplayerID ?? 0L, PokemonSaveSection.Daycare);
				Game1.playSound("coin");
				Game1.addHUDMessage(new HUDMessage($"{mon.Nickname} voltou para a sua equipe!", 1));
				SetupComponents();
				return;
			}
		}

		// Parent 2 Action (Deposit / Withdraw)
		if (_parent2ActionButton.containsPoint(x, y))
		{
			if (PokemonBreeding.DaycareParent2 == null)
			{
				if (Party.Team.Count == 0)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage("Você não possui Pokémon na equipe para depositar!");
					return;
				}
				_selectingParent2 = true;
				_selectingParent1 = false;
				Game1.playSound("smallSelect");
				SetupComponents();
				return;
			}
			else
			{
				if (Party.Team.Count >= 6)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage("Sua equipe está cheia (máximo 6 Pokémon)!");
					return;
				}
				var mon = PokemonBreeding.DaycareParent2;
				PokemonBreeding.DaycareParent2 = null;
				Party.AddPokemon(mon);
				Party.SaveParty();
				PokemonSaveStore.SaveForFarmer(Game1.player?.UniqueMultiplayerID ?? 0L, PokemonSaveSection.Daycare);
				Game1.playSound("coin");
				Game1.addHUDMessage(new HUDMessage($"{mon.Nickname} voltou para a sua equipe!", 1));
				SetupComponents();
				return;
			}
		}

		// Collect Egg Action
		if (PokemonBreeding.ReadyEgg != null && _collectEggButton.containsPoint(x, y))
		{
			if (Party.Team.Count >= 6)
			{
				Game1.playSound("cancel");
				Game1.showRedMessage("Sua equipe está cheia! Libere um espaço para pegar o Ovo.");
				return;
			}

			var egg = PokemonBreeding.ReadyEgg;
			PokemonBreeding.ReadyEgg = null;
			Party.AddPokemon(egg);
			Party.SaveParty();
			PokemonSaveStore.SaveForFarmer(Game1.player?.UniqueMultiplayerID ?? 0L, PokemonSaveSection.Daycare);
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage($"Você recebeu um Ovo Pokémon da Creche!", 2));
			SetupComponents();
			return;
		}
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape || key == Keys.E)
		{
			if (_selectingParent1 || _selectingParent2)
			{
				_selectingParent1 = false;
				_selectingParent2 = false;
				SetupComponents();
				return;
			}
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
			return;
		}
		base.receiveKeyPress(key);
	}

	public override bool areGamePadControlsImplemented() => true;

	public override void receiveGamePadButton(Buttons button)
	{
		if (button == Buttons.B)
		{
			if (_selectingParent1 || _selectingParent2)
			{
				_selectingParent1 = false;
				_selectingParent2 = false;
				SetupComponents();
				Game1.playSound("smallSelect");
				return;
			}
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
			return;
		}
	}

	public override void draw(SpriteBatch b)
	{
		// Scrim
		b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.65f);

		// Frame
		IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

		// Header
		string title = "🐣 CRECHE POKÉMON DA MARNIE (DAYCARE)";
		PokemonUI.Text(b, Game1.dialogueFont, title, new Rectangle(xPositionOnScreen + 36, yPositionOnScreen + 16, width - 72, 32), Color.DarkGoldenrod, center: false, maxScale: 0.74f);

		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		// If picking parent from party
		if (_selectingParent1 || _selectingParent2)
		{
			string pickerTitle = _selectingParent1 ? "SELECIONE O PRIMEIRO POKÉMON:" : "SELECIONE O SEGUNDO POKÉMON:";
			PokemonUI.Text(b, Game1.smallFont, pickerTitle, new Rectangle(xPositionOnScreen + 100, yPositionOnScreen + 58, width - 200, 24), Color.DarkGreen, center: true, maxScale: 0.85f);

			var team = Party.Team;
			for (int i = 0; i < _partySelectButtons.Count; i++)
			{
				var btn = _partySelectButtons[i];
				bool hov = btn.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, btn.bounds.X, btn.bounds.Y, btn.bounds.Width, btn.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);

				if (i < team.Count)
				{
					var p = team[i];
					var icon = p.GetIconSprite(Game1.graphics.GraphicsDevice);
					if (icon != null)
					{
						b.Draw(icon, new Vector2(btn.bounds.X + 8, btn.bounds.Y + 6), p.GetIconSourceRect(), Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.88f);
					}
					string genderSym = p.Gender == "Female" ? "♀" : (p.Gender == "Male" ? "♂" : "—");
					Color gCol = p.Gender == "Female" ? Color.HotPink : (p.Gender == "Male" ? Color.DodgerBlue : Color.Gray);
					PokemonUI.Text(b, Game1.smallFont, $"{p.Nickname} (Nv. {p.Level})", new Rectangle(btn.bounds.X + 54, btn.bounds.Y + 8, 220, 22), Game1.textColor, center: false, maxScale: 0.80f);
					PokemonUI.Text(b, Game1.smallFont, genderSym, new Rectangle(btn.bounds.X + 276, btn.bounds.Y + 8, 30, 22), gCol, center: false, maxScale: 0.88f);
					PokemonUI.Text(b, Game1.tinyFont, $"Tipo: {p.Species.PrimaryType} | Item: {p.HeldItem ?? "Nenhum"}", new Rectangle(btn.bounds.X + 54, btn.bounds.Y + 28, 260, 18), Color.DimGray, center: false, maxScale: 0.82f);
				}
			}

			_closeButton.draw(b);
			drawMouse(b);
			return;
		}

		// Normal Daycare View
		int startX = xPositionOnScreen + 36;
		int startY = yPositionOnScreen + 64;
		int cardW = 330;
		int cardH = 210;

		// 1. Parent 1 Box
		DrawParentCard(b, startX, startY, cardW, cardH, PokemonBreeding.DaycareParent1, "1º Pokémon (Mãe / Parceiro 1)", _parent1ActionButton, mouseX, mouseY);

		// 2. Parent 2 Box
		int p2X = startX + cardW + 48;
		DrawParentCard(b, p2X, startY, cardW, cardH, PokemonBreeding.DaycareParent2, "2º Pokémon (Pai / Parceiro 2)", _parent2ActionButton, mouseX, mouseY);

		// 3. Status Box (Bottom)
		int statusY = startY + cardH + 16;
		int statusW = width - 72;
		int statusH = 136;
		IClickableMenu.drawTextureBox(b, startX, statusY, statusW, statusH, Color.White);

		// Determine compatibility message
		var p1 = PokemonBreeding.DaycareParent1;
		var p2 = PokemonBreeding.DaycareParent2;
		string statusMsg;
		Color statusColor;

		if (p1 == null && p2 == null)
		{
			statusMsg = "A Creche está vazia. Deixe dois Pokémon sob os cuidados da Marnie para procriar!";
			statusColor = Color.DimGray;
		}
		else if (p1 == null || p2 == null)
		{
			statusMsg = "Apenas um Pokémon depositado. Traga um segundo parceiro compatível para formar um casal!";
			statusColor = Color.DarkSlateBlue;
		}
		else
		{
			bool canBreed = PokemonBreeding.CanBreed(p1, p2);
			if (canBreed)
			{
				statusMsg = "💞 Os dois parecem se dar muito bem! A cada amanhecer, há 65% de chance de gerarem um Ovo.";
				statusColor = Color.DarkGreen;
			}
			else
			{
				statusMsg = "💔 Os dois preferem brincar separados. Grupos de ovos, espécies ou gêneros incompatíveis.";
				statusColor = Color.DarkRed;
			}
		}

		PokemonUI.Text(b, Game1.smallFont, statusMsg, new Rectangle(startX + 16, statusY + 12, statusW - 32, 34), statusColor, center: true, maxScale: 0.78f);

		// 4. Collect Egg Button if ready
		if (PokemonBreeding.ReadyEgg != null)
		{
			bool eHov = _collectEggButton.containsPoint(mouseX, mouseY);
			IClickableMenu.drawTextureBox(b, _collectEggButton.bounds.X, _collectEggButton.bounds.Y, _collectEggButton.bounds.Width, _collectEggButton.bounds.Height, eHov ? Color.Gold : Color.LightYellow);
			PokemonUI.Text(b, Game1.smallFont, "✨ " + _collectEggButton.label + " (Pronto!) ✨", _collectEggButton.bounds, Color.DarkGoldenrod, center: true, maxScale: 0.86f);
		}
		else
		{
			string waitMsg = (p1 != null && p2 != null && PokemonBreeding.CanBreed(p1, p2))
				? "Nenhum ovo foi encontrado hoje. Verifique amanhã pela manhã!"
				: "Nenhum ovo disponível no momento.";
			PokemonUI.Text(b, Game1.tinyFont, waitMsg, new Rectangle(startX + 16, statusY + 70, statusW - 32, 24), Color.Gray, center: true, maxScale: 0.85f);
		}

		// Close Button
		_closeButton.draw(b);

		// Cursor
		drawMouse(b);
	}

	private void DrawParentCard(SpriteBatch b, int x, int y, int w, int h, PokemonInstance mon, string header, ClickableComponent btn, int mouseX, int mouseY)
	{
		IClickableMenu.drawTextureBox(b, x, y, w, h, Color.White);
		PokemonUI.Text(b, Game1.smallFont, header, new Rectangle(x + 10, y + 8, w - 20, 22), Color.DarkGoldenrod, center: true, maxScale: 0.76f);

		if (mon == null)
		{
			PokemonUI.Text(b, Game1.smallFont, "(Nenhum Pokémon)", new Rectangle(x + 10, y + 60, w - 20, 24), Color.LightGray, center: true, maxScale: 0.80f);
		}
		else
		{
			var icon = mon.GetIconSprite(Game1.graphics.GraphicsDevice);
			if (icon != null)
			{
				b.Draw(icon, new Vector2(x + 14, y + 40), mon.GetIconSourceRect(), Color.White, 0f, Vector2.Zero, 1.4f, SpriteEffects.None, 0.88f);
			}

			string genderSym = mon.Gender == "Female" ? "♀" : (mon.Gender == "Male" ? "♂" : "—");
			Color gCol = mon.Gender == "Female" ? Color.HotPink : (mon.Gender == "Male" ? Color.DodgerBlue : Color.Gray);

			PokemonUI.Text(b, Game1.smallFont, $"{mon.Nickname} (Nv. {mon.Level})", new Rectangle(x + 72, y + 38, w - 110, 22), Game1.textColor, center: false, maxScale: 0.78f);
			PokemonUI.Text(b, Game1.smallFont, genderSym, new Rectangle(x + w - 36, y + 38, 26, 22), gCol, center: false, maxScale: 0.85f);

			string typeStr = mon.Species.SecondaryType.HasValue ? $"{mon.Species.PrimaryType}/{mon.Species.SecondaryType.Value}" : $"{mon.Species.PrimaryType}";
			PokemonUI.Text(b, Game1.tinyFont, $"Tipo: {typeStr}", new Rectangle(x + 72, y + 62, w - 82, 18), Color.DimGray, center: false, maxScale: 0.82f);
			PokemonUI.Text(b, Game1.tinyFont, $"Item: {mon.HeldItem ?? "Nenhum"}", new Rectangle(x + 72, y + 82, w - 82, 18), Color.DarkSlateGray, center: false, maxScale: 0.82f);
		}

		// Action Button
		bool hov = btn.containsPoint(mouseX, mouseY);
		Color btnCol = (mon == null) ? (hov ? Color.LightGreen : Color.MintCream) : (hov ? Color.MistyRose : Color.WhiteSmoke);
		IClickableMenu.drawTextureBox(b, btn.bounds.X, btn.bounds.Y, btn.bounds.Width, btn.bounds.Height, btnCol);
		PokemonUI.Text(b, Game1.smallFont, btn.label, btn.bounds, Color.Black, center: true, maxScale: 0.78f);
	}
}
