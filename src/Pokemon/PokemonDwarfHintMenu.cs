using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

public class PokemonDwarfHintMenu : IClickableMenu
{
	private class HintOffer
	{
		public string TierKey;
		public string TierDisplayName;
		public int HintLevel; // 1 = Normal, 2 = Avançada, 3 = Total
		public string LevelName;
		public string LevelDesc;
		public int Price;
		public ClickableComponent BuyButton;
	}

	private readonly List<HintOffer> _offers = new();
	private ClickableTextureComponent _closeButton;
	private ClickableComponent _openPokedexButton;
	private string _statusMessage = "“Eu conheço todos os segredos das cavernas, vulcões e florestas... Tenho mapas de Pokémon raros, se você tiver ouro!” — Anão";
	private Color _statusColor = new Color(70, 50, 30);
	private string _lastRevealedSpecies = null;

	private static readonly (string Key, string Label, int[] Prices)[] Tiers = new[]
	{
		("Normal",    "Pokémon Normal",   new[] { 500,  1000,  2000 }),
		("Starter",   "Pokémon Inicial",  new[] { 1500, 3000,  5000 }),
		("Legendary", "Pokémon Lendário", new[] { 5000, 10000, 15000 }),
		("Mythical",  "Pokémon Mítico",   new[] { 5000, 10000, 15000 })
	};

	private static readonly (int Level, string Name, string Desc)[] Levels = new[]
	{
		(1, "Dica Normal",   "Revela Localização + Estação (Season)"),
		(2, "Dica Avançada", "Revela Localização + Estação + Clima (Tempo)"),
		(3, "Dica Total",    "Revela Localização + Estação + Clima + Horário")
	};

	public PokemonDwarfHintMenu()
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

		_openPokedexButton = new ClickableComponent(
			new Rectangle(xPositionOnScreen + width - 250, yPositionOnScreen + height - 56, 220, 38),
			"open_dex",
			"📖 Abrir Pokédex"
		);

		BuildOffers();
	}

	private void BuildOffers()
	{
		_offers.Clear();
		int gridX = xPositionOnScreen + 28;
		int gridY = yPositionOnScreen + 114;
		int colW = 224;
		int rowH = 142;

		for (int c = 0; c < Tiers.Length; c++)
		{
			var tier = Tiers[c];
			for (int r = 0; r < Levels.Length; r++)
			{
				var lvl = Levels[r];
				int bx = gridX + c * (colW + 6);
				int by = gridY + r * (rowH + 8);
				var offer = new HintOffer
				{
					TierKey = tier.Key,
					TierDisplayName = tier.Label,
					HintLevel = lvl.Level,
					LevelName = lvl.Name,
					LevelDesc = lvl.Desc,
					Price = tier.Prices[r],
					BuyButton = new ClickableComponent(new Rectangle(bx, by, colW, rowH), $"{tier.Key}_{lvl.Level}")
				};
				_offers.Add(offer);
			}
		}
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

		if (_openPokedexButton.containsPoint(x, y))
		{
			Game1.playSound("bigSelect");
			Game1.activeClickableMenu = new PokemonPokedexMenu(_lastRevealedSpecies);
			return;
		}

		foreach (var offer in _offers)
		{
			if (offer.BuyButton.containsPoint(x, y))
			{
				TryPurchaseOffer(offer);
				return;
			}
		}
	}

	private void TryPurchaseOffer(HintOffer offer)
	{
		if (PokemonPokedex.Instance.HasBoughtHintToday(offer.TierKey, offer.HintLevel))
		{
			Game1.playSound("cancel");
			_statusMessage = $"Você já comprou uma {offer.LevelName} de {offer.TierDisplayName} hoje! Volte amanhã.";
			_statusColor = Color.DarkRed;
			return;
		}

		int playerMoney = Game1.player?.Money ?? 0;
		if (playerMoney < offer.Price)
		{
			Game1.playSound("cancel");
			_statusMessage = $"Ouro insuficiente! Você precisa de {offer.Price:N0}g para comprar esta dica.";
			_statusColor = Color.DarkRed;
			return;
		}

		PokemonSpecies target = PickCandidateForHint(offer.TierKey, offer.HintLevel);
		if (target == null)
		{
			Game1.playSound("cancel");
			_statusMessage = $"Você já descobriu todas as dicas desse nível para {offer.TierDisplayName}!";
			_statusColor = Color.DarkGoldenrod;
			return;
		}

		if (Game1.player != null)
		{
			Game1.player.Money -= offer.Price;
		}

		PokemonPokedex.Instance.SetHintLevel(target.Name, offer.HintLevel);
		PokemonPokedex.Instance.MarkHintBoughtToday(offer.TierKey, offer.HintLevel);
		_lastRevealedSpecies = target.Name;

		Game1.playSound("purchase");
		var entry = PokemonPokedex.Instance.GetEntry(target.Name);
		string revealedDetails = offer.HintLevel switch
		{
			1 => $"Local: {entry.Habitat} | Estação: {entry.Seasons}",
			2 => $"Local: {entry.Habitat} | Estação: {entry.Seasons} | Clima: {entry.Weather}",
			_ => $"Local: {entry.Habitat} | Estação: {entry.Seasons} | Clima: {entry.Weather} | Hora: {entry.TimeOfDay}"
		};

		_statusMessage = $"✨ Dica Desbloqueada: #{target.DexNumber:D4} {target.Name.ToUpperInvariant()}! ({revealedDetails})";
		_statusColor = Color.DarkGreen;
	}

	private static PokemonSpecies PickCandidateForHint(string tierKey, int hintLevel)
	{
		var all = PokemonSpecies.AllSpecies();
		var unseenCandidates = new List<PokemonSpecies>();
		var upgradeCandidates = new List<PokemonSpecies>();
		var fallbackCandidates = new List<PokemonSpecies>();

		foreach (var sp in all)
		{
			var entry = PokemonPokedex.Instance.GetEntry(sp.Name);
			if (entry == null || !string.Equals(entry.Tier, tierKey, StringComparison.OrdinalIgnoreCase))
				continue;

			bool isCaught = PokemonPokedex.Instance.IsCaught(sp.Name);
			bool isSeen = PokemonPokedex.Instance.IsSeen(sp.Name);
			int currentHint = PokemonPokedex.Instance.GetHintLevel(sp.Name);

			if (!isSeen && !isCaught)
			{
				unseenCandidates.Add(sp);
			}
			else if (!isCaught && currentHint < hintLevel)
			{
				upgradeCandidates.Add(sp);
			}
			else if (currentHint < hintLevel)
			{
				fallbackCandidates.Add(sp);
			}
		}

		if (unseenCandidates.Count > 0)
			return unseenCandidates[Game1.random.Next(unseenCandidates.Count)];
		if (upgradeCandidates.Count > 0)
			return upgradeCandidates[Game1.random.Next(upgradeCandidates.Count)];
		if (fallbackCandidates.Count > 0)
			return fallbackCandidates[Game1.random.Next(fallbackCandidates.Count)];

		// If every single species in this tier is already caught/maxed, pick a random one from the tier
		var anyInTier = new List<PokemonSpecies>();
		foreach (var sp in all)
		{
			var entry = PokemonPokedex.Instance.GetEntry(sp.Name);
			if (entry != null && string.Equals(entry.Tier, tierKey, StringComparison.OrdinalIgnoreCase))
				anyInTier.Add(sp);
		}
		return anyInTier.Count > 0 ? anyInTier[Game1.random.Next(anyInTier.Count)] : null;
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape || key == Keys.E)
		{
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
			return;
		}
		base.receiveKeyPress(key);
	}

	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
		IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

		// Header
		PokemonUI.Text(b, Game1.dialogueFont, "⛏️ INFORMANTE ANÃO — DICAS DA POKÉDEX", new Rectangle(xPositionOnScreen + 28, yPositionOnScreen + 18, 620, 34), Color.DarkGoldenrod, center: false, maxScale: 0.78f);

		int money = Game1.player?.Money ?? 0;
		PokemonUI.Text(b, Game1.smallFont, $"💰 Seu Ouro: {money:N0}g   (Limite: 1 compra por dia de cada nível e raridade)", new Rectangle(xPositionOnScreen + 30, yPositionOnScreen + 52, 850, 22), Color.DarkGreen, center: false, maxScale: 0.72f, shadow: false);

		// Column Headers (Tiers)
		int gridX = xPositionOnScreen + 28;
		int colW = 224;
		Color[] tierColors = new[]
		{
			new Color(70, 110, 150),
			new Color(45, 135, 75),
			new Color(175, 115, 20),
			new Color(135, 55, 160)
		};

		for (int c = 0; c < Tiers.Length; c++)
		{
			int hx = gridX + c * (colW + 6);
			Rectangle hRect = new Rectangle(hx, yPositionOnScreen + 80, colW, 28);
			b.Draw(Game1.staminaRect, hRect, tierColors[c]);
			PokemonUI.Border(b, hRect, 1, Color.Black * 0.4f);
			PokemonUI.Text(b, Game1.smallFont, Tiers[c].Label.ToUpperInvariant(), hRect, Color.White, center: true, maxScale: 0.66f, shadow: false);
		}

		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		// Offer Cards
		foreach (var offer in _offers)
		{
			bool boughtToday = PokemonPokedex.Instance.HasBoughtHintToday(offer.TierKey, offer.HintLevel);
			bool canAfford = money >= offer.Price;
			bool hov = offer.BuyButton.containsPoint(mouseX, mouseY);

			Color boxTint = boughtToday
				? new Color(195, 195, 195)
				: (hov ? new Color(255, 245, 200) : Color.White);

			IClickableMenu.drawTextureBox(
				b,
				Game1.mouseCursors,
				new Rectangle(432, 439, 9, 9),
				offer.BuyButton.bounds.X,
				offer.BuyButton.bounds.Y,
				offer.BuyButton.bounds.Width,
				offer.BuyButton.bounds.Height,
				boxTint,
				4f,
				drawShadow: false
			);

			int rx = offer.BuyButton.bounds.X + 10;
			int ry = offer.BuyButton.bounds.Y + 8;
			int rw = offer.BuyButton.bounds.Width - 20;

			Color titleCol = offer.HintLevel switch
			{
				1 => new Color(40, 90, 150),
				2 => new Color(150, 85, 15),
				_ => new Color(140, 25, 120)
			};

			PokemonUI.Text(b, Game1.smallFont, $"★ {offer.LevelName.ToUpperInvariant()}", new Rectangle(rx, ry, rw, 22), titleCol, center: true, maxScale: 0.70f, shadow: false);
			PokemonUI.WrappedText(b, Game1.smallFont, offer.LevelDesc, new Rectangle(rx, ry + 24, rw, 58), new Color(65, 55, 45), scale: 0.58f);

			Rectangle pricePill = new Rectangle(rx + 6, ry + 88, rw - 12, 32);
			Color pillBg = boughtToday
				? new Color(120, 120, 120)
				: (canAfford ? new Color(55, 135, 65) : new Color(155, 60, 60));
			b.Draw(Game1.staminaRect, pricePill, pillBg);
			PokemonUI.Border(b, pricePill, 1, Color.Black * 0.35f);

			string btnText = boughtToday ? "ESGOTADO HOJE" : $"COMPRAR: {offer.Price:N0}g";
			PokemonUI.Text(b, Game1.smallFont, btnText, pricePill, Color.White, center: true, maxScale: 0.65f, shadow: false);
		}

		// Footer Status & Open Pokédex Button
		Rectangle statusBox = new Rectangle(xPositionOnScreen + 28, yPositionOnScreen + height - 96, width - 290, 78);
		IClickableMenu.drawTextureBox(
			b,
			Game1.mouseCursors,
			new Rectangle(432, 439, 9, 9),
			statusBox.X,
			statusBox.Y,
			statusBox.Width,
			statusBox.Height,
			new Color(252, 248, 235),
			4f,
			drawShadow: false
		);
		PokemonUI.WrappedText(b, Game1.smallFont, _statusMessage, new Rectangle(statusBox.X + 12, statusBox.Y + 10, statusBox.Width - 24, statusBox.Height - 20), _statusColor, scale: 0.65f);

		bool dexHov = _openPokedexButton.containsPoint(mouseX, mouseY);
		IClickableMenu.drawTextureBox(
			b,
			Game1.mouseCursors,
			new Rectangle(432, 439, 9, 9),
			_openPokedexButton.bounds.X,
			_openPokedexButton.bounds.Y,
			_openPokedexButton.bounds.Width,
			_openPokedexButton.bounds.Height,
			dexHov ? new Color(255, 235, 155) : Color.White,
			4f,
			drawShadow: false
		);
		PokemonUI.Text(b, Game1.smallFont, _openPokedexButton.label, _openPokedexButton.bounds, Color.DarkRed, center: true, maxScale: 0.72f, shadow: false);

		_closeButton.draw(b);
		drawMouse(b);
	}
}
