using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace StardewValley.Pokemon;

public class PokemonHUD
{
	private static PokemonHUD _instance;
	public static PokemonHUD Instance => _instance ??= new PokemonHUD();

	private readonly List<ClickableComponent> _partySlotButtons = new();
	private readonly List<ClickableComponent> _moveButtons = new();
	private ClickableComponent _pokedexToggleButton;
	private ClickableComponent _cycleActiveButton;
	private ClickableComponent _pokedexHUDButton;

	private KeyboardState _previousKeyboardState;
	private MouseState _previousMouseState;
	private double _lastAbilityTime = 0;
	private bool _isExpanded = false;
	private static Texture2D _pokedexTex;
	private static bool _triedLoadPokedexTex = false;

	public bool Visible { get; set; } = true;

	private PokemonHUD()
	{
		SetupBounds();
	}

	private static Texture2D GetPokedexTexture()
	{
		if (_pokedexTex != null && !_pokedexTex.IsDisposed) return _pokedexTex;
		if (_triedLoadPokedexTex) return null;
		_triedLoadPokedexTex = true;

		try
		{
			string[] candidates = new[]
			{
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokefirered", "graphics", "object_events", "pics", "misc", "pokedex.png"),
				@"C:\Users\Cavalino\Downloads\Nova pasta\pokefirered\graphics\object_events\pics\misc\pokedex.png"
			};
			foreach (var p in candidates)
			{
				if (File.Exists(p))
				{
					_pokedexTex = PokemonUI.LoadColorKeyed(p);
					if (_pokedexTex != null) return _pokedexTex;
				}
			}
		}
		catch { }
		return null;
	}

	private void SetupBounds()
	{
		_partySlotButtons.Clear();
		_moveButtons.Clear();

		int panelX = 16;
		int toggleY = 110;
		int panelY = 160;
		int panelW = 275;

		_pokedexToggleButton = new ClickableComponent(new Rectangle(panelX, toggleY, 195, 44), "pokedex_toggle", "Pokédex HUD");
		_cycleActiveButton = new ClickableComponent(new Rectangle(panelX + panelW - 100, panelY + 6, 92, 24), "cycle", "Trocar [R]");

		// 6 Party slots (height 40, stride 44)
		int partyStartY = panelY + 42;
		for (int i = 0; i < 6; i++)
		{
			int sy = partyStartY + i * 44;
			_partySlotButtons.Add(new ClickableComponent(new Rectangle(panelX + 8, sy, panelW - 16, 40), i.ToString()));
		}

		// 4 Move buttons (height 44, stride 48)
		int movesStartY = partyStartY + 6 * 44 + 28;
		for (int m = 0; m < 4; m++)
		{
			int my = movesStartY + m * 48;
			_moveButtons.Add(new ClickableComponent(new Rectangle(panelX + 8, my, panelW - 16, 44), m.ToString()));
		}

		// Pokédex shortcut button at bottom (full width)
		_pokedexHUDButton = new ClickableComponent(new Rectangle(panelX + 8, movesStartY + 4 * 48 + 8, panelW - 16, 32), "pokedex", "📖 Pokédex [P]");
	}

	public bool IsMouseOverHUD(int x, int y)
	{
		if (!Visible || Game1.activeClickableMenu != null) return false;
		if (_pokedexToggleButton != null && _pokedexToggleButton.containsPoint(x, y)) return true;
		if (_isExpanded)
		{
			int panelX = 16;
			int panelY = 160;
			int panelW = 275;
			int totalH = 40 + 6 * 44 + 32 + 4 * 48 + 48;
			Rectangle panelRect = new Rectangle(panelX, panelY, panelW, totalH);
			if (panelRect.Contains(x, y)) return true;
		}
		return false;
	}

	private static bool _pendingScreenshot = false;
	private static string _pendingScreenshotLabel = "capture";
	private int _autoTourStep = -1;
	private double _autoTourNextTime = 0;

	public bool IsExpanded
	{
		get => _isExpanded;
		set => _isExpanded = value;
	}

	public static void RequestScreenshot(string label = "capture")
	{
		_pendingScreenshot = true;
		_pendingScreenshotLabel = label;
	}

	public static void TryCaptureScreen()
	{
		if (!_pendingScreenshot) return;
		_pendingScreenshot = false;
		string dir = @"c:\Users\Cavalino\Downloads\Nova pasta\screenshots";
		Directory.CreateDirectory(dir);
		string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_{_pendingScreenshotLabel}.png";
		string fullPath = Path.Combine(dir, fileName);
		try
		{
			var gd = Game1.graphics.GraphicsDevice;
			int w = gd.PresentationParameters.BackBufferWidth;
			int h = gd.PresentationParameters.BackBufferHeight;
			if (w > 0 && h > 0)
			{
				try
				{
					Color[] data = new Color[w * h];
					gd.GetBackBufferData(data);
					for (int i = 0; i < data.Length; i++) data[i].A = 255;
					using Texture2D tex = new Texture2D(gd, w, h, false, SurfaceFormat.Color);
					tex.SetData(data);
					using Stream s2 = File.Create(fullPath);
					tex.SaveAsPng(s2, w, h);
					try { File.AppendAllText(Path.Combine(dir, "tour.log"), $"[Saved BB] {fileName}\n"); } catch { }
					return;
				}
				catch (Exception bbEx)
				{
					try { File.AppendAllText(Path.Combine(dir, "tour.log"), $"[Warn BB] {bbEx.Message}\n"); } catch { }
				}
			}

			RenderTarget2D rt = Game1.game1?.uiScreen ?? Game1.game1?.screen;
			if (rt != null && !rt.IsDisposed)
			{
				using Stream s = File.Create(fullPath);
				rt.SaveAsPng(s, rt.Width, rt.Height);
				try { File.AppendAllText(Path.Combine(dir, "tour.log"), $"[Saved RT] {fileName}\n"); } catch { }
				return;
			}
		}
		catch (Exception ex)
		{
			try { File.AppendAllText(Path.Combine(dir, "tour.log"), $"[ERR Capture] {ex.Message}\n"); } catch { }
		}
	}

	public void GlobalUpdate(GameTime gameTime)
	{
		if (Game1.options != null)
		{
			Game1.options.pauseWhenOutOfFocus = false;
		}

		KeyboardState currentKb = Keyboard.GetState();

		// F12 Immediate Screenshot to c:\Users\Cavalino\Downloads\Nova pasta\screenshots
		if (IsKeyPressed(currentKb, Keys.F12))
		{
			Game1.playSound("cameraNoise");
			RequestScreenshot("f12");
		}

		// F9 Start Automated Visual Tour & Screenshot Verification
		if (IsKeyPressed(currentKb, Keys.F9))
		{
			_autoTourStep = 0;
			_autoTourNextTime = gameTime.TotalGameTime.TotalSeconds + 0.5;
		}

		string triggerFile = @"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\run_tour.trigger";
		if (_autoTourStep < 0 && File.Exists(triggerFile))
		{
			try { File.Delete(triggerFile); } catch { }
			_autoTourStep = 0;
			_autoTourNextTime = gameTime.TotalGameTime.TotalSeconds + 1.5;
			try { File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", "[Tour] Trigger detected!\n"); } catch { }
		}

		if (_autoTourStep >= 0 && gameTime.TotalGameTime.TotalSeconds >= _autoTourNextTime)
		{
			if (Game1.dialogueFont == null || Game1.objectData == null)
			{
				_autoTourNextTime = gameTime.TotalGameTime.TotalSeconds + 1.0;
				return;
			}
			// If we are still on the TitleMenu or loading, load an experimental save first!
			if (Game1.activeClickableMenu is TitleMenu || Game1.currentLocation == null || Game1.gameMode != 3 || Game1.fadeToBlack || Game1.fadeToBlackAlpha > 0.05f)
			{
				if (SaveGame.loaded == null && Game1.gameMode != 6 && Game1.currentLoader == null)
				{
					try
					{
						string savesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "Saves");
						if (Directory.Exists(savesDir))
						{
							string chosenSave = null;
							foreach (string d in Directory.GetDirectories(savesDir))
							{
								string name = Path.GetFileName(d);
								if (File.Exists(Path.Combine(d, name)))
								{
									chosenSave = name;
									if (name.StartsWith("skz", StringComparison.OrdinalIgnoreCase) || name.StartsWith("pipi", StringComparison.OrdinalIgnoreCase))
										break;
								}
							}
							if (chosenSave != null)
							{
								File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", $"[Tour] Loading valid save: {chosenSave}\n");
								TitleMenu.subMenu = null;
								SaveGame.Load(chosenSave);
								Game1.exitActiveMenu();
							}
						}
					}
					catch (Exception ex)
					{
						try { File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", $"[ERR Load] {ex}\n"); } catch { }
					}
				}
				_autoTourNextTime = gameTime.TotalGameTime.TotalSeconds + 1.0;
				return;
			}
			RunAutoTourStep(gameTime.TotalGameTime.TotalSeconds);
		}
	}

	public void Update(GameTime gameTime)
	{
		KeyboardState currentKb = Keyboard.GetState();

		// F8 Debug Menu - accessible only if unlocked via chat command /debugPoke
		if (IsKeyPressed(currentKb, Keys.F8))
		{
			if (PokemonDebugMenu.IsDebugUnlocked)
			{
				Game1.playSound("select");
				Game1.activeClickableMenu = new PokemonDebugMenu();
				_previousKeyboardState = currentKb;
				return;
			}
		}

		if (Game1.activeClickableMenu != null || Game1.currentLocation == null)
		{
			_previousKeyboardState = GamePad.GetState(PlayerIndex.One).IsConnected ? _previousKeyboardState : currentKb;
			return;
		}

		SetupBounds();
		PokemonItemManager.FixInventoryRods(Game1.player);
		PokemonFollower.Instance.Update(gameTime);

		// Check if new farmer / player hasn't chosen starter yet!
		if (!PlayerParty.Instance.HasChosenStarter && Game1.activeClickableMenu == null && !Game1.fadeToBlack && !Game1.eventUp)
		{
			Game1.activeClickableMenu = new PokemonStarterSelectMenu();
			return;
		}

		// R quick cycle active Pokemon (avoids Tab inventory toolbar swap conflict)
		if (IsKeyPressed(currentKb, Keys.R))
		{
			PlayerParty.Instance.CycleActive();
		}

		bool altHeld = currentKb.IsKeyDown(Keys.LeftAlt) || currentKb.IsKeyDown(Keys.RightAlt);

		// Alt + 1..6 Quick Switch
		if (altHeld)
		{
			if (IsKeyPressed(currentKb, Keys.D1) || IsKeyPressed(currentKb, Keys.NumPad1)) PlayerParty.Instance.SetActive(0);
			else if (IsKeyPressed(currentKb, Keys.D2) || IsKeyPressed(currentKb, Keys.NumPad2)) PlayerParty.Instance.SetActive(1);
			else if (IsKeyPressed(currentKb, Keys.D3) || IsKeyPressed(currentKb, Keys.NumPad3)) PlayerParty.Instance.SetActive(2);
			else if (IsKeyPressed(currentKb, Keys.D4) || IsKeyPressed(currentKb, Keys.NumPad4)) PlayerParty.Instance.SetActive(3);
			else if (IsKeyPressed(currentKb, Keys.D5) || IsKeyPressed(currentKb, Keys.NumPad5)) PlayerParty.Instance.SetActive(4);
			else if (IsKeyPressed(currentKb, Keys.D6) || IsKeyPressed(currentKb, Keys.NumPad6)) PlayerParty.Instance.SetActive(5);
		}
		else
		{
			// F1..F4 trigger active Pokemon moves
			if (IsKeyPressed(currentKb, Keys.F1)) PerformMoveByIndex(0);
			else if (IsKeyPressed(currentKb, Keys.F2)) PerformMoveByIndex(1);
			else if (IsKeyPressed(currentKb, Keys.F3)) PerformMoveByIndex(2);
			else if (IsKeyPressed(currentKb, Keys.F4)) PerformMoveByIndex(3);
		}

		MouseState currentMouse = Mouse.GetState();
		bool leftClicked = (_previousMouseState.LeftButton == ButtonState.Released && currentMouse.LeftButton == ButtonState.Pressed);
		bool rightClicked = (_previousMouseState.RightButton == ButtonState.Released && currentMouse.RightButton == ButtonState.Pressed);
		_previousMouseState = currentMouse;

		if (leftClicked)
		{
			int mouseX = Game1.getMouseX(true);
			int mouseY = Game1.getMouseY(true);
			if (ReceiveLeftClick(mouseX, mouseY))
			{
				Game1.oldMouseState = currentMouse;
				_previousKeyboardState = currentKb;
				return;
			}
		}

		if (rightClicked)
		{
			int mouseX = Game1.getMouseX(true);
			int mouseY = Game1.getMouseY(true);
			if (ReceiveRightClick(mouseX, mouseY))
			{
				Game1.oldMouseState = currentMouse;
				_previousKeyboardState = currentKb;
				return;
			}
		}

		// Spectator Mode: Click on another farmer who is in battle to watch their fight!
		if ((leftClicked || rightClicked) && Game1.currentLocation?.farmers != null)
		{
			Vector2 mouseWorld = new Vector2(Game1.getMouseX() + Game1.viewport.X, Game1.getMouseY() + Game1.viewport.Y);
			foreach (var f in Game1.currentLocation.farmers)
			{
				if (f != null && f != Game1.player)
				{
					Rectangle fBox = f.GetBoundingBox();
					fBox.Inflate(24, 24);
					if (fBox.Contains((int)mouseWorld.X, (int)mouseWorld.Y))
					{
						if (PokemonBattleMenu.ActiveBattles.TryGetValue(f.UniqueMultiplayerID, out var activeBattle) && activeBattle != null)
						{
							Game1.playSound("bigSelect");
							Game1.activeClickableMenu = new PokemonBattleMenu(activeBattle, spectator: true);
							return;
						}
					}
				}
			}
		}

		if (IsKeyPressed(currentKb, Keys.P) || IsKeyPressed(currentKb, Keys.H))
		{
			if (Game1.activeClickableMenu == null)
			{
				Game1.playSound("select");
				Game1.activeClickableMenu = new PokemonPokedexMenu();
			}
		}

		_previousKeyboardState = currentKb;
	}

	private double _lastLeftClickTime = -10.0;
	private double _lastRightClickTime = -10.0;

	public bool ReceiveLeftClick(int mouseX, int mouseY)
	{
		if (!Visible || Game1.activeClickableMenu != null) return false;

		double now = Game1.currentGameTime != null ? Game1.currentGameTime.TotalGameTime.TotalSeconds : 0.0;
		if (IsMouseOverHUD(mouseX, mouseY) && now - _lastLeftClickTime < 0.12)
		{
			return true; // Already processed this click in the current frame!
		}

		// 0. Pokédex Toggle Icon Button (Always clickable to expand/collapse lateral menu)
		if (_pokedexToggleButton != null && _pokedexToggleButton.containsPoint(mouseX, mouseY))
		{
			_lastLeftClickTime = now;
			_isExpanded = !_isExpanded;
			Game1.playSound(_isExpanded ? "shwip" : "bigDeSelect");
			return true;
		}

		// Only process lateral panel clicks when expanded!
		if (_isExpanded && PlayerParty.Instance.Team.Count > 0)
		{
			// 1. Cycle Active Button
			if (_cycleActiveButton != null && _cycleActiveButton.containsPoint(mouseX, mouseY))
			{
				_lastLeftClickTime = now;
				PlayerParty.Instance.CycleActive();
				return true;
			}

			// 2. Party Slots (Left-Click sets Active Pokémon)
			for (int i = 0; i < _partySlotButtons.Count; i++)
			{
				if (_partySlotButtons[i].containsPoint(mouseX, mouseY))
				{
					_lastLeftClickTime = now;
					if (i < PlayerParty.Instance.Team.Count)
					{
						PlayerParty.Instance.SetActive(i);
					}
					return true;
				}
			}

			// 3. Move Buttons
			for (int m = 0; m < _moveButtons.Count; m++)
			{
				if (_moveButtons[m].containsPoint(mouseX, mouseY))
				{
					_lastLeftClickTime = now;
					PerformMoveByIndex(m);
					return true;
				}
			}

			// 4. Pokédex Button
			if (_pokedexHUDButton != null && _pokedexHUDButton.containsPoint(mouseX, mouseY))
			{
				_lastLeftClickTime = now;
				Game1.playSound("select");
				Game1.activeClickableMenu = new PokemonPokedexMenu();
				return true;
			}

			int panelX = 16;
			int panelY = 160;
			int panelW = 275;
			int totalH = 40 + 6 * 44 + 32 + 4 * 48 + 48;
			Rectangle panelRect = new Rectangle(panelX, panelY, panelW, totalH);
			if (panelRect.Contains(mouseX, mouseY))
			{
				_lastLeftClickTime = now;
				return true; // Click consumed by lateral panel background!
			}
		}
		else if (_isExpanded && PlayerParty.Instance.Team.Count == 0)
		{
			Rectangle emptyBox = new Rectangle(16, 160, 220, 48);
			if (emptyBox.Contains(mouseX, mouseY))
			{
				_lastLeftClickTime = now;
				return true;
			}
		}

		return false;
	}

	public bool ReceiveRightClick(int mouseX, int mouseY)
	{
		if (!Visible || Game1.activeClickableMenu != null) return false;

		double now = Game1.currentGameTime != null ? Game1.currentGameTime.TotalGameTime.TotalSeconds : 0.0;
		if (IsMouseOverHUD(mouseX, mouseY) && now - _lastRightClickTime < 0.12)
		{
			return true;
		}

		// Right-Click on collapsed Pokédex toggle opens Summary of Active Pokémon (or expands if none)
		if (_pokedexToggleButton != null && _pokedexToggleButton.containsPoint(mouseX, mouseY))
		{
			_lastRightClickTime = now;
			if (PlayerParty.Instance.ActivePokemon != null)
			{
				Game1.activeClickableMenu = new PokemonSummaryMenu(PlayerParty.Instance.ActivePokemon, PlayerParty.Instance.Team);
			}
			else
			{
				_isExpanded = !_isExpanded;
				Game1.playSound(_isExpanded ? "shwip" : "bigDeSelect");
			}
			return true;
		}

		if (_isExpanded && PlayerParty.Instance.Team.Count > 0)
		{
			for (int i = 0; i < _partySlotButtons.Count; i++)
			{
				if (_partySlotButtons[i].containsPoint(mouseX, mouseY))
				{
					_lastRightClickTime = now;
					if (i < PlayerParty.Instance.Team.Count)
					{
						Game1.activeClickableMenu = new PokemonSummaryMenu(PlayerParty.Instance.Team[i], PlayerParty.Instance.Team);
					}
					return true;
				}
			}
		}

		if (IsMouseOverHUD(mouseX, mouseY))
		{
			_lastRightClickTime = now;
			// Right click on lateral HUD: consume so it never interacts with world behind it!
			return true;
		}
		return false;
	}

	private bool IsKeyPressed(KeyboardState current, Keys key)
	{
		return current.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
	}

	private void RunAutoTourStep(double now)
	{
		try
		{
			switch (_autoTourStep)
			{
				case 0:
					// Ensure player has a rich team with Gen 1-4 Pokemon and Super Rod
					if (PlayerParty.Instance.Team.Count == 0)
					{
						PlayerParty.Instance.AddPokemon(new PokemonInstance(PokemonSpecies.Get("Charizard"), 36, "Flamethrower", "Wing Attack", "Slash", "Fury Cutter"));
						PlayerParty.Instance.AddPokemon(new PokemonInstance(PokemonSpecies.Get("Gyarados"), 32, "Surf", "Waterfall", "Bite", "Dragon Rage"));
						PlayerParty.Instance.AddPokemon(new PokemonInstance(PokemonSpecies.Get("Turtwig"), 25, "Razor Leaf", "Bite", "Mega Drain", "Tackle"));
						PlayerParty.Instance.AddPokemon(new PokemonInstance(PokemonSpecies.Get("Lucario"), 40, "Karate Chop", "Iron Head", "Quick Attack", "Rock Smash"));
					}
					Game1.exitActiveMenu();
					_isExpanded = false;
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 1:
					RequestScreenshot("01_hud_collapsed_pokedex");
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 2:
					_isExpanded = true;
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 3:
					RequestScreenshot("02_hud_expanded_gba");
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 4:
					var dbg1 = new PokemonDebugMenu();
					dbg1.SetDebugState(0, 0); // Gen 1 page 1
					Game1.activeClickableMenu = dbg1;
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 5:
					RequestScreenshot("03_debug_pokemon_gen1");
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 6:
					if (Game1.activeClickableMenu is PokemonDebugMenu dbgG4)
					{
						dbgG4.SetDebugState(0, 21); // Gen 4 Sinnoh page!
					}
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 7:
					RequestScreenshot("04_debug_pokemon_gen4_sinnoh");
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 8:
					if (Game1.activeClickableMenu is PokemonDebugMenu dbgTms)
					{
						dbgTms.SetDebugState(2, 0, fieldMovesOnly: true); // TMs/HMs filtered by field moves
					}
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 9:
					RequestScreenshot("05_debug_tms_hms");
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 10:
					// Open GameMenu and switch to Pokemon Party tab
					var gm = new GameMenu();
					for (int i = 0; i < gm.pages.Count; i++)
					{
						if (gm.pages[i] is PokemonPartyPage)
						{
							gm.currentTab = i;
							break;
						}
					}
					Game1.activeClickableMenu = gm;
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 11:
					RequestScreenshot("06_party_menu_gba");
					try { File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", "[Tour Step 11 done]\n"); } catch { }
					Game1.exitActiveMenu();
					_autoTourNextTime = now + 0.8;
					_autoTourStep++;
					break;
				case 12:
					try { File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", "[Tour Step 12 entering battle]\n"); } catch { }
					// Trigger a wild battle against a Gen 4 Pokemon (Lucario) to inspect trainer throw + sprites
					var wildLucario = new PokemonInstance(PokemonSpecies.Get("Lucario"), 28);
					Game1.activeClickableMenu = new PokemonBattleMenu(wildLucario, isWild: true);
					_autoTourNextTime = now + 1.9;
					_autoTourStep++;
					break;
				case 13:
					RequestScreenshot("07_battle_trainer_sendout");
					try { File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", "[Tour Step 13 done]\n"); } catch { }
					_autoTourNextTime = now + 2.1;
					_autoTourStep++;
					break;
				case 14:
					RequestScreenshot("08_battle_main_menu");
					try { File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", "[Tour Step 14 done]\n"); } catch { }
					_autoTourNextTime = now + 0.8;
					_autoTourStep++;
					break;
				case 15:
					// Step 15: Open Pokedex Menu
					Game1.activeClickableMenu = new PokemonPokedexMenu();
					_autoTourNextTime = now + 0.8;
					_autoTourStep++;
					break;
				case 16:
					RequestScreenshot("09_pokedex_menu_overview");
					try { File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", "[Tour Step 16 done]\n"); } catch { }
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 17:
					// Step 17: Open Fast Travel Map (Fly / Teleport)
					Game1.activeClickableMenu = new PokemonWarpMenu(PlayerParty.Instance.ActivePokemon, isFly: true);
					_autoTourNextTime = now + 0.8;
					_autoTourStep++;
					break;
				case 18:
					RequestScreenshot("10_fast_travel_warp_map");
					try { File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", "[Tour Step 18 done]\n"); } catch { }
					_autoTourNextTime = now + 0.6;
					_autoTourStep++;
					break;
				case 19:
					// Step 19: Debug Menu Battle Tab (NPC Pagination)
					var dbgBtl = new PokemonDebugMenu();
					dbgBtl.SetDebugState(3, 0); // Battle tab is index 3
					Game1.activeClickableMenu = dbgBtl;
					_autoTourNextTime = now + 0.8;
					_autoTourStep++;
					break;
				case 20:
					RequestScreenshot("11_debug_battle_npc_trainers");
					try { File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", "[Tour Step 20 done]\n"); } catch { }
					Game1.exitActiveMenu();
					_isExpanded = false;
					_autoTourStep = -1;
					break;
			}
		}
		catch (Exception ex)
		{
			try { File.AppendAllText(@"c:\Users\Cavalino\Downloads\Nova pasta\screenshots\tour.log", $"[ERR Tour Step {_autoTourStep}] {ex}\n"); } catch { }
			_autoTourStep = -1;
		}
	}

	public void PerformMoveByIndex(int moveIndex)
	{
		if (Game1.currentGameTime.TotalGameTime.TotalSeconds - _lastAbilityTime < 0.5) return;
		_lastAbilityTime = Game1.currentGameTime.TotalGameTime.TotalSeconds;

		var active = PlayerParty.Instance.ActivePokemon;
		if (active == null)
		{
			Game1.showRedMessage("Você não possui Pokémon ativo!");
			return;
		}

		if (active.IsFainted)
		{
			Game1.showRedMessage($"{active.Nickname} está desmaiado! Cure-o para usar ataques.");
			return;
		}

		if (moveIndex < 0 || moveIndex >= active.Moves.Count)
		{
			Game1.showRedMessage("Este golpe não está disponível!");
			return;
		}

		PokemonMove move = active.Moves[moveIndex];
		if (move.CurrentPP <= 0)
		{
			Game1.showRedMessage($"{move.Name} está sem PP!");
			return;
		}

		// Execute Field Ability
		ExecuteFieldAbility(active, move);
	}

	private void ExecuteFieldAbility(PokemonInstance active, PokemonMove move)
	{
		FieldAbilityType ability = move.GetFieldAbility();
		bool executed = false;

		switch (ability)
		{
			case FieldAbilityType.Cut:
				executed = PerformCut(active, move);
				break;
			case FieldAbilityType.Smash:
				executed = PerformSmash(active, move);
				break;
			case FieldAbilityType.Till:
				executed = PerformTill(active, move);
				break;
			case FieldAbilityType.Water:
				executed = PerformWater(active, move);
				break;
			case FieldAbilityType.Growth:
				executed = PerformGrowth(active, move);
				break;
			case FieldAbilityType.Rain:
				executed = PerformRain(active, move);
				break;
			case FieldAbilityType.Sunny:
				executed = PerformSunny(active, move);
				break;
			case FieldAbilityType.Zap:
				executed = PerformZap(active, move);
				break;
			case FieldAbilityType.Fly:
				executed = PerformFly(active, move);
				break;
			case FieldAbilityType.Teleport:
				executed = PerformTeleport(active, move);
				break;
			case FieldAbilityType.None:
			default:
				Game1.showRedMessage($"{move.Name} não possui efeito fora de combate!");
				return;
		}

		if (executed && move.CurrentPP > 0)
		{
			move.CurrentPP--;
		}
	}

	private bool PerformFly(PokemonInstance active, PokemonMove move)
	{
		Game1.playSound("flute");
		Game1.activeClickableMenu = new PokemonWarpMenu(active, true);
		return true;
	}

	private bool PerformTeleport(PokemonInstance active, PokemonMove move)
	{
		Game1.playSound("wand");
		Game1.activeClickableMenu = new PokemonWarpMenu(active, false);
		return true;
	}

	private bool PerformCut(PokemonInstance active, PokemonMove move)
	{
		Vector2 grabTile = Game1.player.GetGrabTile();
		GameLocation loc = Game1.currentLocation;
		if (loc == null) return false;

		try
		{
			if (loc.terrainFeatures.TryGetValue(grabTile, out var tf))
			{
				if (tf is Tree tree)
				{
					tree.Location ??= loc;
					tree.lastPlayerToHit.Value = Game1.player.UniqueMultiplayerID;
					if (tree.instantDestroy(grabTile))
					{
						loc.terrainFeatures.Remove(grabTile);
					}
					Game1.playSound("axchop");
					Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} cortou a árvore!", 2));
					return true;
				}
				if (tf is FruitTree fruitTree)
				{
					loc.terrainFeatures.Remove(grabTile);
					Game1.playSound("axchop");
					Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} cortou a árvore frutífera!", 2));
					return true;
				}
				if (tf is Grass)
				{
					loc.terrainFeatures.Remove(grabTile);
					Game1.playSound("cut");
					Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} cortou o mato!", 2));
					return true;
				}
			}

			// Check large stumps / hollow logs in resourceClumps
			for (int i = loc.resourceClumps.Count - 1; i >= 0; i--)
			{
				var rc = loc.resourceClumps[i];
				if (rc != null && rc.occupiesTile((int)grabTile.X, (int)grabTile.Y) && (rc.parentSheetIndex.Value == 600 || rc.parentSheetIndex.Value == 602))
				{
					Game1.createMultipleObjectDebris("(O)709", (int)grabTile.X, (int)grabTile.Y, 2, Game1.player.UniqueMultiplayerID, loc);
					loc.resourceClumps.RemoveAt(i);
					Game1.playSound("axchop");
					Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} cortou o tronco maciço!", 2));
					return true;
				}
			}

			if (loc.objects.TryGetValue(grabTile, out var obj) && obj != null)
			{
				if (obj.Name.Contains("Weed") || obj.Name.Contains("Twig") || obj.ItemId == "388" || obj.ItemId == "294" || obj.ItemId == "295")
				{
					loc.destroyObject(grabTile, Game1.player);
					Game1.playSound("cut");
					Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} ceifou o obstáculo!", 2));
					return true;
				}
			}
		}
		catch
		{
			loc.terrainFeatures.Remove(grabTile);
			Game1.playSound("cut");
			return true;
		}

		Game1.showRedMessage("Aponte para uma árvore, muda ou galho à sua frente!");
		return false;
	}

	private bool PerformSmash(PokemonInstance active, PokemonMove move)
	{
		Vector2 grabTile = Game1.player.GetGrabTile();
		GameLocation loc = Game1.currentLocation;
		if (loc == null) return false;

		if (loc.objects.TryGetValue(grabTile, out var obj) && obj != null)
		{
			if (obj.Name.Contains("Stone") || obj.ItemId == "390" || obj.ItemId == "668" || obj.ItemId == "670" || obj.IsBreakableStone())
			{
				loc.destroyObject(grabTile, Game1.player);
				Game1.playSound("hammer");
				Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} quebrou a rocha!", 2));
				return true;
			}
		}

		// Also allow smashing large boulders in resourceClumps
		for (int i = loc.resourceClumps.Count - 1; i >= 0; i--)
		{
			var rc = loc.resourceClumps[i];
			if (rc != null && rc.occupiesTile((int)grabTile.X, (int)grabTile.Y) && (rc.parentSheetIndex.Value == 672 || rc.parentSheetIndex.Value == 752 || rc.parentSheetIndex.Value == 754 || rc.parentSheetIndex.Value == 756 || rc.parentSheetIndex.Value == 758))
			{
				Game1.createMultipleObjectDebris("(O)390", (int)grabTile.X, (int)grabTile.Y, 6, Game1.player.UniqueMultiplayerID, loc);
				loc.resourceClumps.RemoveAt(i);
				Game1.playSound("hammer");
				Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} pulverizou o pedregulho!", 2));
				return true;
			}
		}

		Game1.showRedMessage("Aponte para uma pedra à sua frente!");
		return false;
	}

	private bool PerformTill(PokemonInstance active, PokemonMove move)
	{
		GameLocation loc = Game1.currentLocation;
		if (loc == null) return false;
		Vector2 playerTile = Game1.player.Tile;
		int tilledCount = 0;
		int radius = move.Power >= 100 ? 2 : 1; // 5x5 for Earthquake, 3x3 for Dig/Bulldoze

		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				Vector2 target = new Vector2(playerTile.X + dx, playerTile.Y + dy);
				if (!loc.isTileHoeDirt(target) && loc.isTilePassable(target) && !loc.isWaterTile((int)target.X, (int)target.Y))
				{
					if (loc.makeHoeDirt(target, ignoreChecks: true))
					{
						tilledCount++;
					}
				}
			}
		}

		Game1.playSound("hoeHit");
		int side = radius * 2 + 1;
		if (tilledCount > 0)
		{
			Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} arou {tilledCount} blocos de terra (Área {side}x{side})!", 2));
		}
		else
		{
			Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} remexeu o solo com força telúrica!", 1));
		}
		return true;
	}

	public static void SetWorldWeather(bool rain)
	{
		Game1.isRaining = rain;
		Game1.weatherForTomorrow = rain ? "Rain" : "Sun";
		if (Game1.netWorldState?.Value != null)
		{
			Game1.netWorldState.Value.WeatherForTomorrow = rain ? "Rain" : "Sun";
			var curWeather = Game1.netWorldState.Value.GetWeatherForLocation(Game1.currentLocation?.GetLocationContextId() ?? "Default");
			if (curWeather != null)
			{
				curWeather.Weather = rain ? "Rain" : "Sun";
				curWeather.IsRaining = rain;
				curWeather.IsSnowing = false;
				curWeather.IsLightning = false;
			}
		}
		Game1.debrisWeather?.Clear();
		if (rain)
		{
			Game1.playSound("thunder");
			Game1.playSound("rainsound");
			Game1.changeMusicTrack("rain");
		}
		else
		{
			Game1.stopMusicTrack(StardewValley.GameData.MusicContext.Default);
			Game1.playSound("fireball");
		}
	}

	private bool PerformWater(PokemonInstance active, PokemonMove move)
	{
		GameLocation loc = Game1.currentLocation;
		if (loc == null) return false;
		Vector2 playerTile = Game1.player.Tile;
		int wateredCount = 0;
		string mName = move.Name.ToLowerInvariant();

		// Scaled watering radius according to move strength
		int radius = 1; // default 3x3
		if (mName.Contains("hydro") || mName.Contains("spout") || mName.Contains("origin"))
		{
			radius = 3; // 7x7 (49 tiles) - like Iridium can
		}
		else if (mName.Contains("surf") || mName.Contains("waterfall") || mName.Contains("muddy") || mName.Contains("scald") || mName.Contains("dive") || mName.Contains("tail"))
		{
			radius = 2; // 5x5 (25 tiles) - like Gold can
		}
		else if (mName == "water gun" || mName == "bubble")
		{
			radius = 1; // 3x3 - like Copper/Steel can
		}

		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				Vector2 target = new Vector2(playerTile.X + dx, playerTile.Y + dy);
				if (loc.terrainFeatures.TryGetValue(target, out var tf) && tf is HoeDirt dirt)
				{
					dirt.state.Value = 1;
					wateredCount++;
				}
			}
		}

		Game1.playSound("slosh");
		int side = radius * 2 + 1;
		if (wateredCount > 0)
		{
			Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} regou {wateredCount} canteiros (Área {side}x{side})!", 2));
		}
		else
		{
			Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} lançou água em área {side}x{side}!", 1));
		}
		return true;
	}

	private bool PerformGrowth(PokemonInstance active, PokemonMove move)
	{
		GameLocation loc = Game1.currentLocation;
		Vector2 playerTile = Game1.player.Tile;
		int acceleratedCount = 0;

		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dy = -1; dy <= 1; dy++)
			{
				Vector2 target = new Vector2(playerTile.X + dx, playerTile.Y + dy);
				if (loc.terrainFeatures.TryGetValue(target, out var tf) && tf is HoeDirt dirt && dirt.crop != null && !dirt.crop.dead.Value)
				{
					dirt.crop.currentPhase.Value = Math.Min(dirt.crop.phaseDays.Count - 1, dirt.crop.currentPhase.Value + 1);
					dirt.crop.updateDrawMath(target);
					acceleratedCount++;
				}
			}
		}

		Game1.playSound("coin");
		if (acceleratedCount > 0)
		{
			Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} acelerou {acceleratedCount} plantações!", 2));
		}
		else
		{
			Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} emanou essência fértil!", 1));
		}
		return true;
	}

	private bool PerformRain(PokemonInstance active, PokemonMove move)
	{
		SetWorldWeather(true);
		Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} convocou chuva torrencial instantânea!", 2));
		return true;
	}

	private bool PerformSunny(PokemonInstance active, PokemonMove move)
	{
		SetWorldWeather(false);
		Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} dispersou as nuvens com calor solar!", 2));
		return true;
	}

	private bool PerformZap(PokemonInstance active, PokemonMove move)
	{
		Game1.playSound("thunder_small");
		Game1.flashAlpha = 0.5f;
		Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} iluminou a área com faíscas elétricas!", 2));
		return true;
	}

	private bool PerformDash(PokemonInstance active, PokemonMove move)
	{
		var effects = new StardewValley.Buffs.BuffEffects();
		effects.Speed.Value = 3f;
		var buff = new Buff(
			id: "pokemon_quick_dash",
			source: active.Nickname,
			displaySource: move.Name,
			duration: 10000,
			effects: effects,
			displayName: $"{move.Name} Impulso",
			description: $"Velocidade ampliada por {active.Nickname}!"
		);
		Game1.player.applyBuff(buff);
		Game1.playSound("dustSerpent");
		Game1.addHUDMessage(new HUDMessage($"[{move.Name}] {active.Nickname} concedeu +3 de velocidade por 10s!", 1));
		return true;
	}

	private static void DrawPokedexDeviceIcon(SpriteBatch b, int x, int y)
	{
		// Outer red Pokédex casing (32x34)
		Rectangle outer = new Rectangle(x, y, 32, 34);
		b.Draw(Game1.staminaRect, outer, new Color(210, 32, 42));
		PokemonUI.Border(b, outer, 2, new Color(40, 16, 20));

		// Top sensor bar highlight
		b.Draw(Game1.staminaRect, new Rectangle(x + 2, y + 2, 28, 8), new Color(235, 55, 65));
		b.Draw(Game1.staminaRect, new Rectangle(x + 2, y + 10, 28, 2), new Color(120, 15, 22));

		// Cyan camera lens (top-left)
		b.Draw(Game1.staminaRect, new Rectangle(x + 4, y + 3, 7, 7), Color.White);
		b.Draw(Game1.staminaRect, new Rectangle(x + 5, y + 4, 5, 5), new Color(40, 195, 245));
		b.Draw(Game1.staminaRect, new Rectangle(x + 5, y + 4, 2, 2), Color.White);

		// 3 mini indicator LEDs (Red, Yellow, Green)
		b.Draw(Game1.staminaRect, new Rectangle(x + 14, y + 4, 3, 3), new Color(255, 80, 80));
		b.Draw(Game1.staminaRect, new Rectangle(x + 19, y + 4, 3, 3), new Color(255, 220, 50));
		b.Draw(Game1.staminaRect, new Rectangle(x + 24, y + 4, 3, 3), new Color(60, 230, 90));

		// White screen bezel & green LCD display
		b.Draw(Game1.staminaRect, new Rectangle(x + 5, y + 14, 18, 14), new Color(230, 236, 240));
		b.Draw(Game1.staminaRect, new Rectangle(x + 7, y + 16, 14, 10), new Color(115, 215, 130));

		// Right hinge line
		b.Draw(Game1.staminaRect, new Rectangle(x + 26, y + 12, 3, 19), new Color(165, 22, 30));

		// Overlay authentic pokedex.png sprite if loaded
		Texture2D tex = GetPokedexTexture();
		if (tex != null)
		{
			b.Draw(tex, new Vector2(x, y + 1), new Rectangle(0, 0, tex.Width, tex.Height), Color.White, 0f, Vector2.Zero, 2.0f, SpriteEffects.None, 0.92f);
		}
	}

	public void Draw(SpriteBatch b, GameTime gameTime)
	{
		if (!Visible || Game1.activeClickableMenu != null) return;

		int mouseX = Game1.getMouseX(true);
		int mouseY = Game1.getMouseY(true);
		var team = PlayerParty.Instance.Team;
		var active = PlayerParty.Instance.ActivePokemon;

		// 0. Always Draw Pokédex Toggle Button at top-left
		if (_pokedexToggleButton != null)
		{
			bool dexHover = _pokedexToggleButton.containsPoint(mouseX, mouseY);
			Rectangle dexRect = _pokedexToggleButton.bounds;

			// GBA Styled Capsule for Pokédex Toggle
			PokemonUI.Border(b, dexRect, 2, new Color(24, 48, 56));
			Color fillCol = dexHover ? new Color(255, 248, 215) : (_isExpanded ? new Color(210, 238, 228) : new Color(236, 244, 240));
			b.Draw(Game1.staminaRect, new Rectangle(dexRect.X + 2, dexRect.Y + 2, dexRect.Width - 4, dexRect.Height - 4), fillCol);

			// Draw Pokédex Icon on Left of Toggle Button
			DrawPokedexDeviceIcon(b, dexRect.X + 6, dexRect.Y + 5);

			// Draw Active Pokémon mini preview or Toggle Label
			if (active != null && !_isExpanded)
			{
				Texture2D icon = active.GetIconSprite(Game1.graphics.GraphicsDevice) ?? active.GetFrontSprite(Game1.graphics.GraphicsDevice);
				if (icon != null)
				{
					b.Draw(icon, new Vector2(dexRect.X + 42, dexRect.Y + 6), active.GetIconSourceRect(), active.IsFainted ? Color.DarkGray : Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.92f);
				}
				PokemonUI.Text(b, Game1.tinyFont, active.Nickname, new Rectangle(dexRect.X + 76, dexRect.Y + 5, 92, 16), new Color(24, 72, 60), maxScale: 0.74f);
				PokemonUI.HpBar(b, new Rectangle(dexRect.X + 76, dexRect.Y + 24, 85, 7), (float)active.CurrentHP / Math.Max(1, active.MaxHP));
				PokemonUI.Text(b, Game1.smallFont, "▾", new Rectangle(dexRect.Right - 22, dexRect.Y + 10, 18, 24), new Color(210, 32, 42), center: true, maxScale: 0.85f);
			}
			else
			{
				string toggleText = _isExpanded ? "POKÉDEX [Fechar ▴]" : "POKÉDEX [Abrir ▾]";
				PokemonUI.Text(b, Game1.smallFont, toggleText, new Rectangle(dexRect.X + 44, dexRect.Y + 8, dexRect.Width - 50, 28), dexHover ? Color.DarkRed : new Color(24, 72, 60), center: false, maxScale: 0.76f);
			}
		}

		// Keep Lateral Menu Hidden unless Pokédex Toggle is Expanded!
		if (!_isExpanded) return;

		if (team.Count == 0)
		{
			int px = 16;
			int py = 160;
			Rectangle box = new Rectangle(px, py, 220, 48);
			PokemonUI.Border(b, box, 2, new Color(24, 48, 56));
			b.Draw(Game1.staminaRect, new Rectangle(px + 2, py + 2, 216, 44), new Color(230, 242, 238));
			PokemonUI.Text(b, Game1.smallFont, "[F8] Menu Pokémon", box, new Color(36, 92, 80), center: true, maxScale: 0.85f);
			return;
		}

		int panelX = 16;
		int panelY = 160;
		int panelW = 275;
		int totalH = 40 + 6 * 44 + 32 + 4 * 48 + 48;

		// 1. Panel Background (Authentic Pokémon GBA frame)
		Rectangle panelRect = new Rectangle(panelX, panelY, panelW, totalH);
		PokemonUI.Border(b, panelRect, 3, new Color(24, 48, 56));
		b.Draw(Game1.staminaRect, new Rectangle(panelX + 3, panelY + 3, panelW - 6, totalH - 6), new Color(232, 244, 240));

		// Header Banner (Emerald GBA gradient)
		Rectangle headerRect = new Rectangle(panelX + 3, panelY + 3, panelW - 6, 34);
		b.Draw(Game1.staminaRect, headerRect, new Color(36, 92, 80));
		PokemonUI.Border(b, headerRect, 1, new Color(52, 136, 118));

		// Header Title
		PokemonUI.Text(b, Game1.smallFont, "EQUIPE POKÉMON", new Rectangle(panelX + 12, panelY + 7, panelW - 120, 24), Color.White, maxScale: 0.82f);

		// Quick Switch (R) Button in Header
		if (_cycleActiveButton != null)
		{
			bool cycHover = _cycleActiveButton.containsPoint(mouseX, mouseY);
			b.Draw(Game1.staminaRect, _cycleActiveButton.bounds, cycHover ? new Color(248, 224, 56) : new Color(52, 136, 118));
			PokemonUI.Border(b, _cycleActiveButton.bounds, 1, new Color(24, 48, 56));
			PokemonUI.Text(b, Game1.tinyFont, _cycleActiveButton.label, _cycleActiveButton.bounds, cycHover ? Color.DarkRed : Color.White, center: true, maxScale: 0.80f);
		}

		// 2. Draw 6 Party Slots (GBA Capsule Cards)
		int activeIdx = PlayerParty.Instance.ActiveIndex;
		for (int i = 0; i < 6; i++)
		{
			var btn = _partySlotButtons[i];
			bool hovered = btn.containsPoint(mouseX, mouseY);
			bool isActive = (i == activeIdx);

			if (i < team.Count)
			{
				var p = team[i];
				isActive = (i == activeIdx && !p.IsFainted);

				Color cardBg = isActive ? new Color(205, 245, 225) : (hovered ? new Color(254, 252, 230) : Color.White);
				b.Draw(Game1.staminaRect, btn.bounds, cardBg);
				PokemonUI.Border(b, btn.bounds, isActive ? 2 : 1, isActive ? new Color(36, 130, 75) : new Color(160, 185, 178));

				// 32x32 animated Pokemon Icon
				Texture2D icon = p.GetIconSprite(Game1.graphics.GraphicsDevice) ?? p.GetFrontSprite(Game1.graphics.GraphicsDevice);
				if (icon != null)
				{
					Rectangle srcRect = p.GetIconSourceRect();
					b.Draw(icon, new Vector2(btn.bounds.X + 4, btn.bounds.Y + 4), srcRect, p.IsFainted ? Color.DarkGray : Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.9f);
				}

				// Name & Level
				Color nameColor = p.IsFainted ? Color.DarkRed : (isActive ? new Color(16, 92, 52) : Color.DarkSlateGray);
				PokemonUI.Text(b, Game1.smallFont, p.Nickname, new Rectangle(btn.bounds.X + 42, btn.bounds.Y + 3, 115, 18), nameColor, maxScale: 0.75f);
				PokemonUI.Text(b, Game1.tinyFont, $"Lv.{p.Level}", new Rectangle(btn.bounds.X + 160, btn.bounds.Y + 3, 44, 18), Color.DimGray, maxScale: 0.75f);

				// GBA HP Tag & Bar
				int hpTagW = 18;
				int hpBarX = btn.bounds.X + 42 + hpTagW + 2;
				int hpBarW = 95;
				int hpBarY = btn.bounds.Y + 23;

				if (p.IsFainted)
				{
					Rectangle faintedBadge = new Rectangle(btn.bounds.X + 42, hpBarY - 2, 74, 14);
					b.Draw(Game1.staminaRect, faintedBadge, new Color(185, 42, 42));
					PokemonUI.Text(b, Game1.tinyFont, "DESMAIADO", faintedBadge, Color.White, center: true, maxScale: 0.62f);

					int faintBarX = faintedBadge.Right + 6;
					int faintBarW = 44;
					PokemonUI.HpBar(b, new Rectangle(faintBarX, hpBarY + 1, faintBarW, 8), 0f);

					string hpText = $"0/{p.MaxHP}";
					PokemonUI.Text(b, Game1.tinyFont, hpText, new Rectangle(faintBarX + faintBarW + 4, hpBarY - 3, 50, 16), Color.DarkRed, maxScale: 0.70f);
				}
				else
				{
					PokemonUI.HpTag(b, new Rectangle(btn.bounds.X + 42, hpBarY - 2, hpTagW, 12));
					PokemonUI.HpBar(b, new Rectangle(hpBarX, hpBarY, hpBarW, 8), (float)p.CurrentHP / Math.Max(1, p.MaxHP));

					string hpText = $"{p.CurrentHP}/{p.MaxHP}";
					PokemonUI.Text(b, Game1.tinyFont, hpText, new Rectangle(hpBarX + hpBarW + 4, hpBarY - 4, 55, 16), Color.DarkSlateGray, maxScale: 0.70f);
				}

				if (isActive && !p.IsFainted)
				{
					PokemonUI.Text(b, Game1.smallFont, "★", new Rectangle(btn.bounds.Right - 18, btn.bounds.Y + 4, 16, 16), new Color(220, 140, 20), maxScale: 0.85f);
				}
			}
			else
			{
				b.Draw(Game1.staminaRect, btn.bounds, new Color(220, 228, 226) * 0.6f);
				PokemonUI.Border(b, btn.bounds, 1, new Color(180, 195, 190));
				PokemonUI.Text(b, Game1.tinyFont, $"#{i + 1} [Vazio]", btn.bounds, Color.Gray, center: true, maxScale: 0.75f);
			}
		}

		// 3. Active Pokemon Moves Section
		int movesStartY = panelY + 40 + 6 * 44 + 6;

		// Moves Header Banner
		Rectangle movesHeaderRect = new Rectangle(panelX + 3, movesStartY, panelW - 6, 26);
		b.Draw(Game1.staminaRect, movesHeaderRect, new Color(42, 82, 92));
		PokemonUI.Border(b, movesHeaderRect, 1, new Color(60, 110, 120));

		string headerMoves = active != null ? $"GOLPES: {active.Nickname}" : "GOLPES";
		PokemonUI.Text(b, Game1.smallFont, headerMoves, new Rectangle(panelX + 10, movesStartY + 4, panelW - 20, 18), Color.White, maxScale: 0.78f);

		for (int m = 0; m < 4; m++)
		{
			var btn = _moveButtons[m];
			bool hovered = btn.containsPoint(mouseX, mouseY);

			if (active != null && m < active.Moves.Count)
			{
				PokemonMove move = active.Moves[m];
				Color cardBg = hovered ? new Color(254, 250, 220) : Color.White;
				b.Draw(Game1.staminaRect, btn.bounds, cardBg);
				PokemonUI.Border(b, btn.bounds, 1, hovered ? new Color(40, 140, 80) : new Color(160, 180, 175));

				// Type Capsule Pill on Left
				Color typeCol = PokemonTypeColors.Get(move.Type);
				Rectangle typePill = new Rectangle(btn.bounds.X + 5, btn.bounds.Y + 4, 46, 16);
				b.Draw(Game1.staminaRect, typePill, typeCol);
				PokemonUI.Border(b, typePill, 1, Color.White * 0.4f);
				PokemonUI.Text(b, Game1.tinyFont, move.Type.ToString().ToUpperInvariant(), typePill, Color.White, center: true, maxScale: 0.60f, shadow: false);

				// Hotkey + Move Name
				string moveTitle = $"[F{m + 1}] {move.Name}";
				Color nameColor = (move.CurrentPP > 0) ? (hovered ? Color.DarkRed : Color.DarkSlateGray) : Color.Gray;
				PokemonUI.Text(b, Game1.smallFont, moveTitle, new Rectangle(btn.bounds.X + 55, btn.bounds.Y + 3, btn.bounds.Width - 130, 18), nameColor, maxScale: 0.74f);

				// PP Counter
				string ppStr = $"{move.CurrentPP}/{move.MaxPP}";
				PokemonUI.Text(b, Game1.tinyFont, ppStr, new Rectangle(btn.bounds.Right - 70, btn.bounds.Y + 4, 65, 16), (move.CurrentPP > 0) ? new Color(20, 110, 50) : Color.Red, center: true, maxScale: 0.72f);

				// Overworld Tag / Field Ability
				var fType = move.GetFieldAbility();
				string tag = fType switch
				{
					FieldAbilityType.Cut => "✂ [Cortar] Árvores & Matos",
					FieldAbilityType.Smash => "💥 [Quebrar] Rochas do Vale",
					FieldAbilityType.Till => "⛏️ [Arar] Arar Solo em Área",
					FieldAbilityType.Water => (move.Power >= 100 ? "💧 [Regar] Área 7x7 (49 tiles)" : (move.Power >= 80 ? "💧 [Regar] Área 5x5 (25 tiles)" : "💧 [Regar] Área 3x3 (9 tiles)")),
					FieldAbilityType.Growth => "🌿 [Acelerar] Cultivos 3x3",
					FieldAbilityType.Rain => "🌧 [Clima] Invocar Chuva Instantânea",
					FieldAbilityType.Sunny => "☀️ [Clima] Invocar Sol Instantâneo",
					FieldAbilityType.Zap => "⚡ [Luz] Flash Elétrico",
					FieldAbilityType.Dash => "⚡ [Impulso] +3 Velocidade (10s)",
					FieldAbilityType.Fly => "✈️ [Voar] Mapa de Voo Rápido",
					FieldAbilityType.Teleport => "🔮 [Teleport] Teletransporte Global",
					_ => "⚔ [Combate] Sem Efeito de Campo"
				};
				Color tagColor = (fType != FieldAbilityType.None) ? new Color(16, 120, 60) : Color.Gray * 0.8f;
				PokemonUI.Text(b, Game1.tinyFont, tag, new Rectangle(btn.bounds.X + 6, btn.bounds.Y + 24, btn.bounds.Width - 12, 16), tagColor, maxScale: 0.70f);
			}
			else
			{
				b.Draw(Game1.staminaRect, btn.bounds, new Color(220, 228, 226) * 0.6f);
				PokemonUI.Border(b, btn.bounds, 1, new Color(180, 195, 190));
				PokemonUI.Text(b, Game1.tinyFont, $"[F{m + 1}] --", btn.bounds, Color.Gray, center: true, maxScale: 0.75f);
			}
		}

		// 4. Pokédex Button
		if (_pokedexHUDButton != null)
		{
			bool dexHover = _pokedexHUDButton.containsPoint(mouseX, mouseY);
			b.Draw(Game1.staminaRect, _pokedexHUDButton.bounds, dexHover ? new Color(254, 230, 200) : new Color(180, 50, 40));
			PokemonUI.Border(b, _pokedexHUDButton.bounds, 1, new Color(120, 25, 20));
			PokemonUI.Text(b, Game1.smallFont, _pokedexHUDButton.label, _pokedexHUDButton.bounds, dexHover ? Color.DarkRed : Color.White, center: true, maxScale: 0.74f);
		}

	}
}
