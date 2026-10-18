using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

public class PokemonWarpMenu : IClickableMenu
{
	public class WarpDestination
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string TargetLocation { get; set; }
		public int TargetX { get; set; }
		public int TargetY { get; set; }
		public Vector2 MapCoord { get; set; } // Position on the Stardew map graphic
		public Rectangle Bounds { get; set; }
	}

	private readonly bool _isFly;
	private readonly PokemonInstance _caster;
	private readonly List<WarpDestination> _destinations = new();
	private WarpDestination _hoveredDest = null;
	private Texture2D _mapTexture;
	private Rectangle _mapSourceRect;
	private Rectangle _mapDestRect;
	private ClickableTextureComponent _closeButton;

	public PokemonWarpMenu(PokemonInstance caster, bool isFly)
		: base(Game1.uiViewport.Width / 2 - 460, Game1.uiViewport.Height / 2 - 320, 920, 640, showUpperRightCloseButton: true)
	{
		_caster = caster;
		_isFly = isFly;

		try
		{
			_mapTexture = Game1.content.Load<Texture2D>("LooseSprites\\map");
			// In Stardew Valley LooseSprites\map, Pelican Town is in rectangle (0, 0, 300, 180)
			_mapSourceRect = new Rectangle(0, 0, Math.Min(300, _mapTexture.Width), Math.Min(180, _mapTexture.Height));
		}
		catch { }

		_closeButton = new ClickableTextureComponent(
			new Rectangle(xPositionOnScreen + width - 36, yPositionOnScreen + 10, 32, 32),
			Game1.mouseCursors,
			new Rectangle(337, 494, 12, 12),
			2.5f
		);

		SetupDestinations();
	}

	private void SetupDestinations()
	{
		_destinations.Clear();

		// Map area on right side of modal
		int mapX = xPositionOnScreen + 340;
		int mapY = yPositionOnScreen + 70;
		int mapW = 550;
		int mapH = 430;
		_mapDestRect = new Rectangle(mapX, mapY, mapW, mapH);

		// Destinations list on left side
		int listX = xPositionOnScreen + 24;
		int listY = yPositionOnScreen + 70;
		int itemH = 68;

		var dests = new[]
		{
			new WarpDestination
			{
				Name = "Sua Fazenda",
				Description = "Retorne imediatamente à sua casa e plantações.",
				TargetLocation = "Farm",
				TargetX = 64,
				TargetY = 15,
				MapCoord = new Vector2(mapX + (int)(mapW * 0.28f), mapY + (int)(mapH * 0.40f))
			},
			new WarpDestination
			{
				Name = "Armazém do Pierre",
				Description = "Vila Pelicanos, praça central e lojas.",
				TargetLocation = "Town",
				TargetX = 43,
				TargetY = 58,
				MapCoord = new Vector2(mapX + (int)(mapW * 0.52f), mapY + (int)(mapH * 0.46f))
			},
			new WarpDestination
			{
				Name = "Mercado Joja",
				Description = "Lado leste da Vila Pelicanos.",
				TargetLocation = "Town",
				TargetX = 96,
				TargetY = 51,
				MapCoord = new Vector2(mapX + (int)(mapW * 0.76f), mapY + (int)(mapH * 0.42f))
			},
			new WarpDestination
			{
				Name = "Praia de Pelicanos",
				Description = "Cais de pesca do Willy e oceano aberto.",
				TargetLocation = "Beach",
				TargetX = 28,
				TargetY = 9,
				MapCoord = new Vector2(mapX + (int)(mapW * 0.58f), mapY + (int)(mapH * 0.85f))
			},
			new WarpDestination
			{
				Name = "Floresta Cinzenta",
				Description = "Rancho da Marnie e Torre do Mago.",
				TargetLocation = "Forest",
				TargetX = 35,
				TargetY = 12,
				MapCoord = new Vector2(mapX + (int)(mapW * 0.25f), mapY + (int)(mapH * 0.68f))
			},
			new WarpDestination
			{
				Name = "Montanhas & Minas",
				Description = "Entrada das Minas, Carpintaria e Lago.",
				TargetLocation = "Mountain",
				TargetX = 31,
				TargetY = 20,
				MapCoord = new Vector2(mapX + (int)(mapW * 0.65f), mapY + (int)(mapH * 0.20f))
			},
			new WarpDestination
			{
				Name = "Deserto de Calico",
				Description = "Oásis árido, Loja da Sandy e Caverna da Caveira.",
				TargetLocation = "Desert",
				TargetX = 18,
				TargetY = 28,
				MapCoord = new Vector2(mapX + (int)(mapW * 0.08f), mapY + (int)(mapH * 0.12f))
			}
		};

		for (int i = 0; i < dests.Length; i++)
		{
			var d = dests[i];
			d.Bounds = new Rectangle(listX, listY + i * (itemH + 6), 300, itemH);
			_destinations.Add(d);
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (_closeButton != null && _closeButton.containsPoint(x, y))
		{
			Game1.playSound("bigDeSelect");
			exitThisMenu();
			return;
		}

		// Check clicking left list or map pins
		foreach (var d in _destinations)
		{
			Rectangle pinHitbox = new Rectangle((int)d.MapCoord.X - 16, (int)d.MapCoord.Y - 16, 32, 32);
			if (d.Bounds.Contains(x, y) || pinHitbox.Contains(x, y))
			{
				ExecuteWarp(d);
				return;
			}
		}
	}

	private void ExecuteWarp(WarpDestination dest)
	{
		exitThisMenu();

		if (_isFly)
		{
			Game1.playSound("flute");
			Game1.playSound("parrot");
			Game1.warpFarmer(dest.TargetLocation, dest.TargetX, dest.TargetY, false);
			Game1.addHUDMessage(new HUDMessage($"[{_caster?.Nickname ?? "Pokémon"}] Voou majestosamente até {dest.Name}!", 2));
		}
		else
		{
			Game1.playSound("wand");
			Utility.addStarsAndSpirals(Game1.currentLocation, (int)Game1.player.Tile.X, (int)Game1.player.Tile.Y, 2, 2, 90, 25, Color.MediumPurple);
			Game1.warpFarmer(dest.TargetLocation, dest.TargetX, dest.TargetY, false);
			Game1.addHUDMessage(new HUDMessage($"[{_caster?.Nickname ?? "Pokémon"}] Teletransportou para {dest.Name}!", 2));
		}
	}

	public override void performHoverAction(int x, int y)
	{
		_closeButton?.tryHover(x, y);
		_hoveredDest = null;

		foreach (var d in _destinations)
		{
			Rectangle pinHitbox = new Rectangle((int)d.MapCoord.X - 16, (int)d.MapCoord.Y - 16, 32, 32);
			if (d.Bounds.Contains(x, y) || pinHitbox.Contains(x, y))
			{
				_hoveredDest = d;
				break;
			}
		}
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape || key == Keys.B)
		{
			Game1.playSound("bigDeSelect");
			exitThisMenu();
			return;
		}
		base.receiveKeyPress(key);
	}

	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.65f);

		// Outer GBA casing
		Rectangle frame = new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height);
		PokemonUI.Border(b, frame, 3, new Color(24, 48, 56));
		b.Draw(Game1.staminaRect, new Rectangle(frame.X + 3, frame.Y + 3, frame.Width - 6, frame.Height - 6), new Color(236, 246, 242));

		// Header Banner
		Rectangle header = new Rectangle(frame.X + 3, frame.Y + 3, frame.Width - 6, 48);
		Color headerCol = _isFly ? new Color(36, 110, 140) : new Color(110, 48, 140);
		b.Draw(Game1.staminaRect, header, headerCol);
		PokemonUI.Border(b, header, 1, Color.White * 0.4f);

		string title = _isFly
			? $"✈️ [HM02 FLY] - Escolha seu Destino de Voo ({_caster?.Nickname})"
			: $"🔮 [TELEPORT] - Selecione as Coordenadas de Salto ({_caster?.Nickname})";
		PokemonUI.Text(b, Game1.dialogueFont, title, new Rectangle(header.X + 16, header.Y + 8, header.Width - 80, 34), Color.White, center: false, maxScale: 0.85f);

		// Draw Stardew Valley Town Map
		if (_mapTexture != null)
		{
			b.Draw(_mapTexture, _mapDestRect, _mapSourceRect, Color.White);
			PokemonUI.Border(b, _mapDestRect, 2, new Color(24, 48, 56));
		}
		else
		{
			b.Draw(Game1.staminaRect, _mapDestRect, Color.DarkSlateGray * 0.4f);
			PokemonUI.Border(b, _mapDestRect, 2, new Color(24, 48, 56));
		}

		// Draw Destination List (Left Column)
		for (int i = 0; i < _destinations.Count; i++)
		{
			var d = _destinations[i];
			bool isHov = (_hoveredDest == d);

			Color cardBg = isHov ? new Color(255, 250, 215) : Color.White;
			Color borderCol = isHov ? new Color(220, 60, 40) : new Color(160, 185, 180);
			b.Draw(Game1.staminaRect, d.Bounds, cardBg);
			PokemonUI.Border(b, d.Bounds, isHov ? 2 : 1, borderCol);

			// Indicator icon on left of card
			string icon = _isFly ? "✈" : "✦";
			PokemonUI.Text(b, Game1.smallFont, icon, new Rectangle(d.Bounds.X + 6, d.Bounds.Y + 12, 24, 24), isHov ? Color.DarkRed : headerCol, center: true, maxScale: 0.85f);

			// Name
			PokemonUI.Text(b, Game1.smallFont, d.Name, new Rectangle(d.Bounds.X + 34, d.Bounds.Y + 8, d.Bounds.Width - 40, 22), Color.DarkSlateBlue, maxScale: 0.82f);
			// Description
			PokemonUI.Text(b, Game1.tinyFont, d.Description, new Rectangle(d.Bounds.X + 34, d.Bounds.Y + 34, d.Bounds.Width - 40, 24), Color.DimGray, maxScale: 0.72f);
		}

		// Draw Map Pins & Glowing Pulses
		float pulse = (float)(0.8 + 0.2 * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 5.0));
		foreach (var d in _destinations)
		{
			bool isHov = (_hoveredDest == d);
			Vector2 pinPos = d.MapCoord;

			if (isHov)
			{
				// Pulsing glow circle
				b.Draw(Game1.staminaRect, new Rectangle((int)pinPos.X - 18, (int)pinPos.Y - 18, 36, 36), Color.Yellow * 0.45f * pulse);
			}

			// Pin dot
			Color pinColor = isHov ? Color.Red : new Color(30, 120, 200);
			b.Draw(Game1.staminaRect, new Rectangle((int)pinPos.X - 8, (int)pinPos.Y - 8, 16, 16), pinColor);
			PokemonUI.Border(b, new Rectangle((int)pinPos.X - 8, (int)pinPos.Y - 8, 16, 16), 1, Color.White);

			// Tiny label next to pin
			if (isHov)
			{
				Vector2 labelSize = Game1.tinyFont.MeasureString(d.Name);
				Rectangle tagRect = new Rectangle((int)pinPos.X + 12, (int)pinPos.Y - 12, (int)labelSize.X + 12, (int)labelSize.Y + 6);
				b.Draw(Game1.staminaRect, tagRect, Color.Black * 0.85f);
				PokemonUI.Border(b, tagRect, 1, Color.Gold);
				PokemonUI.Text(b, Game1.tinyFont, d.Name, tagRect, Color.White, center: true, maxScale: 0.85f);
			}
		}

		_closeButton?.draw(b);
		drawMouse(b);
	}
}
