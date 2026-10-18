using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StardewValley.Pokemon;

/// <summary>
/// Overworld Companion Follower Pokémon system.
/// The active team Pokémon walks behind the player, mirroring movement,
/// playing authentic walking frames, and interacting when clicked.
/// </summary>
public class PokemonFollower
{
	private static PokemonFollower _instance;
	public static PokemonFollower Instance => _instance ??= new PokemonFollower();

	public bool Enabled { get; set; } = true;

	private Vector2 _position = Vector2.Zero;
	private int _facingDirection = 2; // 0=Up, 1=Right, 2=Down, 3=Left
	private float _walkTimer = 0f;
	private int _walkFrame = 0;
	private bool _isMoving = false;
	private readonly Queue<Vector2> _pathHistory = new();
	private string _lastSpecies = null;
	private bool _lastShiny = false;
	private Texture2D _walkingSheet = null;
	private bool _hasDirectionalSheet = false;

	private string _lastLocationName = null;
	private ButtonState _lastMouseRight = ButtonState.Released;
	private ButtonState _lastMouseLeft = ButtonState.Released;
	private Rectangle _lastDrawnWorldBounds = Rectangle.Empty;
	private float _petCooldown = 0f;
	private float _stuckTimer = 0f;
	private Vector2 _lastFramePos = Vector2.Zero;

	private PokemonFollower() { }

	private float _heartTimer = 0f;
	private Vector2 _heartPos = Vector2.Zero;
	private float _walkDistanceCounter = 0f;

	/// <summary>
	/// Finds a passable, non-colliding world position near the player using a spiral tile search
	/// so the follower never spawns trapped inside beds, walls, stairs, or furniture.
	/// </summary>
	private static Vector2 FindSafeSpawnPosition(GameLocation loc, Vector2 playerCenter, int facingDir)
	{
		if (loc == null)
		{
			return playerCenter + new Vector2(28f, 0);
		}

		Point playerTile = new Point((int)(playerCenter.X / 64f), (int)(playerCenter.Y / 64f));

		// When entering rooms or spawning, prioritize safe walkable tiles beside or in front of the player,
		// avoiding doorways, warps, walls, or off-map areas.
		List<Point> candidateOffsets = new List<Point>
		{
			new Point(-1, 0),  // Left of player
			new Point(1, 0),   // Right of player
			facingDir switch   // Step forward into the room
			{
				0 => new Point(0, -1),
				1 => new Point(1, 0),
				2 => new Point(0, 1),
				3 => new Point(-1, 0),
				_ => new Point(0, -1)
			},
			new Point(-1, -1),
			new Point(1, -1),
			new Point(-1, 1),
			new Point(1, 1),
			new Point(0, -1),
			new Point(0, 1)
		};

		for (int r = 2; r <= 3; r++)
		{
			for (int dx = -r; dx <= r; dx++)
			{
				for (int dy = -r; dy <= r; dy++)
				{
					if (Math.Abs(dx) == r || Math.Abs(dy) == r)
					{
						candidateOffsets.Add(new Point(dx, dy));
					}
				}
			}
		}

		foreach (Point offset in candidateOffsets)
		{
			int tx = playerTile.X + offset.X;
			int ty = playerTile.Y + offset.Y;
			if (loc.Map?.Layers == null || loc.Map.Layers.Count == 0 ||
			    tx < 0 || ty < 0 || tx >= loc.Map.Layers[0].LayerWidth || ty >= loc.Map.Layers[0].LayerHeight)
			{
				continue;
			}

			Vector2 tileVec = new Vector2(tx, ty);
			if (loc.Objects.TryGetValue(tileVec, out var obj) && obj != null && !obj.isPassable())
			{
				continue;
			}

			// Do not spawn on warp or doorway transition tiles
			Rectangle tileRect = new Rectangle(tx * 64, ty * 64, 64, 64);
			if (loc.isCollidingWithWarpOrDoor(tileRect) != null)
			{
				continue;
			}

			Vector2 candWorld = new Vector2(tx * 64f + 32f, ty * 64f + 32f);
			if (IsPositionFree(loc, candWorld))
			{
				return candWorld;
			}
		}

		// Safe fallback: spawn on the player's own tile offset by 26px to avoid overlap,
		// guaranteed to be within the room interior and walkable!
		float sideOffset = (facingDir == 1) ? -26f : 26f;
		return playerCenter + new Vector2(sideOffset, 0);
	}

	private static bool IsPositionFree(GameLocation loc, Vector2 pos)
	{
		if (loc == null) return true;
		int tx = (int)(pos.X / 64f);
		int ty = (int)(pos.Y / 64f);
		if (loc.Map?.Layers == null || loc.Map.Layers.Count == 0 ||
		    tx < 0 || ty < 0 || tx >= loc.Map.Layers[0].LayerWidth || ty >= loc.Map.Layers[0].LayerHeight)
		{
			return false;
		}

		Vector2 tileVec = new Vector2(tx, ty);
		if (loc.Objects.TryGetValue(tileVec, out var obj) && obj != null && !obj.isPassable())
		{
			return false;
		}

		// Use 16x16 bounding box and isFarmer: true with character: Game1.player
		// This checks solid map buildings, walls, and hard furniture without colliding with the farmer, rugs, or open doorways!
		Rectangle box = new Rectangle((int)pos.X - 8, (int)pos.Y - 8, 16, 16);
		return !loc.isCollidingPosition(
			box,
			Game1.viewport,
			isFarmer: true,
			damagesFarmer: 0,
			glider: false,
			character: Game1.player,
			pathfinding: false,
			projectile: false,
			ignoreCharacterRequirement: true,
			skipCollisionEffects: true);
	}

	public void Update(GameTime gameTime)
	{
		PokemonEncounterManager.UpdateOverworldSpawns(gameTime);
		if (!Enabled || Game1.player == null || Game1.currentLocation == null) return;
		var active = PlayerParty.Instance.ActivePokemon;
		if (active == null || active.IsFainted)
		{
			_pathHistory.Clear();
			_isMoving = false;
			_position = Vector2.Zero;
			_lastDrawnWorldBounds = Rectangle.Empty;
			_lastFramePos = Vector2.Zero;
			return;
		}

		float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

		if (_heartTimer > 0f) _heartTimer -= dt;
		if (_petCooldown > 0f) _petCooldown -= dt;

		// Ensure texture is loaded for active species & shiny state
		if (_lastSpecies != active.Species.InternalName || _lastShiny != active.IsShiny)
		{
			_lastSpecies = active.Species.InternalName;
			_lastShiny = active.IsShiny;
			LoadFollowerSprite(_lastSpecies, _lastShiny);
		}

		Rectangle playerBox = Game1.player.GetBoundingBox();
		Vector2 playerCenter = new Vector2(playerBox.Center.X, playerBox.Center.Y);
		string curLocName = Game1.currentLocation.NameOrUniqueName;

		// Initialize position on first spawn, map transition, or extreme warp using safe tile search
		if (_position == Vector2.Zero || _lastLocationName != curLocName || Vector2.Distance(_position, playerCenter) > 580f)
		{
			_lastLocationName = curLocName;
			_position = FindSafeSpawnPosition(Game1.currentLocation, playerCenter, Game1.player.FacingDirection);
			_lastFramePos = _position;
			_stuckTimer = 0f;
			_pathHistory.Clear();
			_facingDirection = Game1.player.FacingDirection;
		}

		// 1. Separation / Anti-Overlap: follower gently steps away if player stands on top of it
		float distToPlayer = Vector2.Distance(_position, playerCenter);
		if (distToPlayer < 46f)
		{
			Vector2 pushVec = _position - playerCenter;
			if (pushVec.LengthSquared() < 1f)
			{
				pushVec = Game1.player.FacingDirection switch
				{
					0 => new Vector2(0, 52),
					1 => new Vector2(-52, 0),
					2 => new Vector2(0, -52),
					3 => new Vector2(52, 0),
					_ => new Vector2(0, 52)
				};
			}
			pushVec.Normalize();
			float pushAmount = (46f - distToPlayer) * Math.Min(1f, dt * 10f);
			Vector2 testPos = _position + pushVec * pushAmount;
			if (IsPositionFree(Game1.currentLocation, testPos))
			{
				_position = testPos;
			}
			else
			{
				// Try perpendicular slide so follower never gets pinned under the player
				Vector2 perp1 = new Vector2(-pushVec.Y, pushVec.X);
				Vector2 perp2 = new Vector2(pushVec.Y, -pushVec.X);
				if (IsPositionFree(Game1.currentLocation, _position + perp1 * pushAmount))
					_position += perp1 * pushAmount;
				else if (IsPositionFree(Game1.currentLocation, _position + perp2 * pushAmount))
					_position += perp2 * pushAmount;
			}
		}

		// 2. Track player movement history (breadcrumb trail)
		if (Game1.player.isMoving())
		{
			Vector2 lastQueued = _pathHistory.Count > 0 ? _lastQueuedPos : _position;
			if (_pathHistory.Count == 0 || Vector2.Distance(playerCenter, lastQueued) >= 24f)
			{
				_pathHistory.Enqueue(playerCenter);
				_lastQueuedPos = playerCenter;
				while (_pathHistory.Count > 14) _pathHistory.Dequeue();
			}
		}

		// Advance waypoints once follower reaches near them
		while (_pathHistory.Count > 0 && Vector2.Distance(_position, _pathHistory.Peek()) < 28f)
		{
			_pathHistory.Dequeue();
		}

		// Follow the active waypoint or player
		Vector2 targetPos = _pathHistory.Count > 0 ? _pathHistory.Peek() : playerCenter;
		float distToTarget = Vector2.Distance(_position, targetPos);

		if (distToPlayer > 62f)
		{
			_isMoving = true;
			Vector2 moveDir = (distToTarget > 2f) ? Vector2.Normalize(targetPos - _position) : Vector2.Normalize(playerCenter - _position);
			float speed = MathHelper.Lerp(165f, 360f, Math.Clamp((distToPlayer - 62f) / 140f, 0f, 1f));
			Vector2 nextPos = _position + moveDir * speed * dt;

			if (IsPositionFree(Game1.currentLocation, nextPos))
			{
				_position = nextPos;
			}
			else
			{
				// Slide along unblocked axis
				Vector2 nextX = new Vector2(nextPos.X, _position.Y);
				Vector2 nextY = new Vector2(_position.X, nextPos.Y);
				if (IsPositionFree(Game1.currentLocation, nextX))
				{
					_position.X = nextPos.X;
				}
				else if (IsPositionFree(Game1.currentLocation, nextY))
				{
					_position.Y = nextPos.Y;
				}
				else
				{
					if (_pathHistory.Count > 0) _pathHistory.Dequeue();
				}
			}

			// Anti-stuck detector: rescue to safe tile near player only if follower is stuck behind an obstacle for >2.2s and far from player
			if (distToPlayer > 260f && Vector2.Distance(_position, _lastFramePos) < 1.0f)
			{
				_stuckTimer += dt;
				if (_stuckTimer >= 2.2f)
				{
					_position = FindSafeSpawnPosition(Game1.currentLocation, playerCenter, Game1.player.FacingDirection);
					_pathHistory.Clear();
					_stuckTimer = 0f;
				}
			}
			else
			{
				_stuckTimer = 0f;
			}

			// Determine facing direction
			if (Math.Abs(moveDir.X) > Math.Abs(moveDir.Y))
			{
				_facingDirection = moveDir.X > 0 ? 1 : 3;
			}
			else
			{
				_facingDirection = moveDir.Y > 0 ? 2 : 0;
			}

			_walkTimer += dt * 5.2f; // Smoother walking animation cadence
			_walkFrame = (int)_walkTimer % 4;

			_walkDistanceCounter += speed * dt;
			if (_walkDistanceCounter >= 128f * 16f)
			{
				_walkDistanceCounter = 0f;
				var activeMon = PlayerParty.Instance.ActivePokemon;
				if (activeMon != null)
				{
					int gain = (string.Equals(activeMon.HeldItem, "Poke_SootheBell", StringComparison.OrdinalIgnoreCase) || string.Equals(activeMon.HeldItem, "SootheBell", StringComparison.OrdinalIgnoreCase)) ? 2 : 1;
					activeMon.AdjustFriendship(gain);
				}
			}
		}
		else
		{
			_isMoving = false;
			_walkFrame = 0;
			_walkTimer = 0f;
			_stuckTimer = 0f;
			_pathHistory.Clear();
		}

		_lastFramePos = _position;

		// Check player interaction (click on follower sprite in world space)
		CheckInteraction();
	}

	private Vector2 _lastQueuedPos = Vector2.Zero;

	private void CheckInteraction()
	{
		if (Game1.activeClickableMenu != null) return;
		var curMouse = Game1.input.GetMouseState();
		bool rightJustPressed = curMouse.RightButton == ButtonState.Pressed && _lastMouseRight == ButtonState.Released;
		bool leftJustPressed = curMouse.LeftButton == ButtonState.Pressed && _lastMouseLeft == ButtonState.Released;
		_lastMouseRight = curMouse.RightButton;
		_lastMouseLeft = curMouse.LeftButton;

		if (_petCooldown > 0f) return;
		if (!rightJustPressed && !leftJustPressed) return;

		int uiMx = Game1.getMouseX(true);
		int uiMy = Game1.getMouseY(true);
		if (PokemonHUD.Instance != null && PokemonHUD.Instance.IsMouseOverHUD(uiMx, uiMy))
			return;

		// Exact world coordinates under the mouse cursor (matches GameLocation.draw world space)
		int worldMouseX = Game1.getMouseX(ui_scale: false) + Game1.viewport.X;
		int worldMouseY = Game1.getMouseY(ui_scale: false) + Game1.viewport.Y;

		Rectangle hitBounds = _lastDrawnWorldBounds;
		if (hitBounds.IsEmpty)
		{
			hitBounds = new Rectangle((int)_position.X - 30, (int)_position.Y - 58, 60, 62);
		}

		bool clickedSprite = hitBounds.Contains(worldMouseX, worldMouseY);
		Rectangle pBox = Game1.player.GetBoundingBox();
		float playerDist = Vector2.Distance(new Vector2(pBox.Center.X, pBox.Center.Y), _position);

		if (clickedSprite && playerDist <= 240f)
		{
			var active = PlayerParty.Instance.ActivePokemon;
			if (active != null)
			{
				_petCooldown = 0.65f;
				Game1.playSound("dwop");
				Game1.player.doEmote(Farmer.heartEmote);
				_heartTimer = 1.6f;
				_heartPos = _position + new Vector2(0, -52);
				int petBonus = (string.Equals(active.HeldItem, "Poke_SootheBell", StringComparison.OrdinalIgnoreCase) || string.Equals(active.HeldItem, "SootheBell", StringComparison.OrdinalIgnoreCase)) ? 6 : 3;
				active.AdjustFriendship(petBonus);

				string[] reactions = new string[]
				{
					$"{active.Nickname} olhou alegremente para você! ♥",
					$"{active.Nickname} está feliz caminhando ao seu lado! ♥",
					$"{active.Nickname} aninhou-se com carinho! ♥",
					$"{active.Nickname} soltou um som amigável e balançou o corpo! ♥",
					$"{active.Nickname} parece cheio de energia e pronto para a aventura! ♥"
				};
				string reaction = reactions[Game1.random.Next(reactions.Length)];
				Game1.addHUDMessage(new HUDMessage(reaction, 2));
			}
		}
	}

	private void LoadFollowerSprite(string species, bool isShiny)
	{
		_hasDirectionalSheet = false;
		_walkingSheet = null;

		// Notice: pikachu.png, mewtwo.png and rayquaza.png in object_events are single-row cutscene sheets, not 4-dir walking sheets.
		// For them, fallback cleanly to animated icon with walking bounce.
		if (string.Equals(species, "pikachu", StringComparison.OrdinalIgnoreCase) ||
		    string.Equals(species, "mewtwo", StringComparison.OrdinalIgnoreCase) ||
		    string.Equals(species, "rayquaza", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		try
		{
			string[] searchDirs = new string[]
			{
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald", "graphics", "object_events", "pics", "pokemon"),
				@"C:\Users\Cavalino\Downloads\Nova pasta\pokeemerald\graphics\object_events\pics\pokemon",
				@"C:\Users\Cavalino\Downloads\Nova pasta\pokefirered\graphics\object_events\pics\pokemon"
			};

			string palFolder = PokemonInstance.FindPokemonFolder(species);

			foreach (var dir in searchDirs)
			{
				if (!Directory.Exists(dir)) continue;
				string p = Path.Combine(dir, species + ".png");
				if (File.Exists(p))
				{
					Texture2D tex = isShiny
						? PokemonInstance.LoadSpriteWithColorKey(Game1.graphics.GraphicsDevice, p, true, palFolder, out _, isSharedPaletteSprite: true)
						: PokemonUI.LoadColorKeyed(p);
					if (tex != null && tex.Height >= 64 && tex.Width >= 32)
					{
						_walkingSheet = tex;
						_hasDirectionalSheet = true;
						return;
					}
				}
			}
		}
		catch { }
	}

	public void Draw(SpriteBatch b)
	{
		PokemonEncounterManager.DrawOverworldSpawns(b);
		if (!Enabled || Game1.player == null || Game1.currentLocation == null) return;
		if (Game1.activeClickableMenu is PokemonHatchMenu) return;
		var active = PlayerParty.Instance.ActivePokemon;
		if (active == null || active.IsFainted) return;

		// Convert world coordinates to screen viewport (in World space, matching GameLocation.draw)
		Vector2 screenPos = _position - new Vector2(Game1.viewport.X, Game1.viewport.Y);

		// Frustum cull if off screen
		if (screenPos.X < -128 || screenPos.X > Game1.viewport.Width + 128 || screenPos.Y < -128 || screenPos.Y > Game1.viewport.Height + 128)
		{
			_lastDrawnWorldBounds = Rectangle.Empty;
			return;
		}

		// Layer depth: sorts naturally with Farmer (Farmer.getDrawLayer() = BoundingBox.Center.Y / 10000f)
		float depth = Math.Clamp(_position.Y / 10000f, 0.001f, 0.999f);

		// Subtle atmospheric tint (GameLocation.draw already receives Stardew's world lightmap, so keep tint very gentle!)
		Color drawTint = Color.White;
		if (Game1.isRaining || (Game1.currentLocation != null && Game1.currentLocation.IsRainingHere()))
		{
			drawTint = new Color(236, 242, 255);
		}
		else if (Game1.currentLocation != null && Game1.currentLocation.IsOutdoors && Game1.timeOfDay >= 1900)
		{
			drawTint = new Color(235, 238, 250);
		}

		// Ground shadow drawn right beneath the follower's feet
		int shadowW = string.Equals(active.Species.InternalName, "rayquaza", StringComparison.OrdinalIgnoreCase) ? 54 : 36;
		PokemonUI.SoftShadow(b, (int)screenPos.X, (int)screenPos.Y + 2, shadowW, 12, 0.32f, Math.Max(0.0001f, depth - 0.0002f));

		if (_hasDirectionalSheet && _walkingSheet != null)
		{
			// 4-Directional sheet
			int frameW = Math.Min(32, _walkingSheet.Width / Math.Max(1, _walkingSheet.Width / 32));
			int frameH = Math.Min(32, _walkingSheet.Height / Math.Max(1, _walkingSheet.Height / 32));
			if (frameW <= 0) frameW = 16;
			if (frameH <= 0) frameH = 16;

			int dirRow = _facingDirection switch
			{
				2 => 0, // Down
				0 => 1, // Up
				3 => 2, // Left
				1 => 3, // Right
				_ => 0
			};

			int fCol = _isMoving ? _walkFrame % 3 : 0;
			int srcX = Math.Min(_walkingSheet.Width - frameW, fCol * frameW);
			int srcY = Math.Min(_walkingSheet.Height - frameH, dirRow * frameH);
			Rectangle srcRect = new Rectangle(srcX, srcY, frameW, frameH);

			Vector2 origin = new Vector2(frameW / 2f, frameH - 4);
			float scale = PokemonInstance.GetOverworldScale(active, isIcon: true) * 1.05f;
			int drawnW = (int)(frameW * scale);
			int drawnH = (int)(frameH * scale);
			_lastDrawnWorldBounds = new Rectangle((int)_position.X - drawnW / 2 - 6, (int)_position.Y - drawnH + 6, drawnW + 12, drawnH + 4);

			b.Draw(_walkingSheet, screenPos, srcRect, drawTint, 0f, origin, scale, SpriteEffects.None, depth);
		}
		else
		{
			// High quality 32x32 animated icon with gentle walking bounce
			Texture2D icon = active.GetIconSprite(Game1.graphics.GraphicsDevice) ?? active.GetFrontSprite(Game1.graphics.GraphicsDevice);
			if (icon != null)
			{
				Rectangle srcRect = (icon == active.GetIconSprite(Game1.graphics.GraphicsDevice))
					? active.GetIconSourceRect()
					: active.GetFrontSourceRect();
				float bounceY = _isMoving ? -(float)Math.Abs(Math.Sin(_walkTimer * Math.PI * 0.5)) * 4f : 0f;
				Vector2 drawPos = screenPos + new Vector2(0, bounceY);
				Vector2 origin = new Vector2(srcRect.Width / 2f, srcRect.Height - 4);
				// Standard icons face left by default: when moving right (FacingDir == 1), flip horizontally to look right!
				SpriteEffects flip = (_facingDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

				// Scale dynamically based on registered height in Pokédex
				float scale = PokemonInstance.GetOverworldScale(active, isIcon: (srcRect.Width <= 40));
				int drawnW = (int)(srcRect.Width * scale);
				int drawnH = (int)(srcRect.Height * scale);
				_lastDrawnWorldBounds = new Rectangle((int)_position.X - drawnW / 2 - 6, (int)(_position.Y + bounceY) - drawnH + 6, drawnW + 12, drawnH + 4);

				b.Draw(icon, drawPos, srcRect, drawTint, 0f, origin, scale, flip, depth);
			}
		}

		// Pet Heart Emote Floating Above Follower Head
		if (_heartTimer > 0f)
		{
			Vector2 heartScreenPos = _heartPos - new Vector2(Game1.viewport.X, Game1.viewport.Y) - new Vector2(0, (1.6f - _heartTimer) * 18f);
			b.Draw(Game1.mouseCursors, heartScreenPos, new Rectangle(211, 428, 7, 6), Color.White, 0f, new Vector2(3.5f, 3f), 3.0f, SpriteEffects.None, Math.Min(0.999f, depth + 0.002f));
		}
	}
}
