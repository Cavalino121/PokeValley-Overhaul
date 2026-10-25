using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Locations;

namespace StardewValley.Pokemon;

public static class PokemonEncounterManager
{
	public const float WildRate = 0.04f; // 4% step encounter rate

	private static readonly Dictionary<long, int> _lastEncounterTickByFarmer = new();
	private static readonly Dictionary<long, Vector2> _lastEncounterTileByFarmer = new();

	public class OverworldWildSpawn
	{
		public PokemonInstance Instance;
		public Vector2 Position;
		public Vector2 Velocity;
		public float MoveTimer;
		public bool IsMoving;
		public int FacingDir = 2;
		public float WalkAnimTimer;
		public float DespawnTimer = 300f;
		public float ContactCooldown = 0f;
		public bool MarkedForRemoval = false;
		public long CurrentBattlerFarmerId = 0L;

		public void Remove()
		{
			MarkedForRemoval = true;
		}
	}

	private class LocationSpawnState
	{
		public readonly List<OverworldWildSpawn> Spawns = new();
		public float RefreshTimer = 0.5f;
		public int LastUpdateTick = -1;
	}

	private static readonly Dictionary<string, LocationSpawnState> _spawnsByLocation = new(StringComparer.OrdinalIgnoreCase);

	public static bool CheckStepEncounter(GameLocation location, Farmer farmer)
	{
		if (farmer == null || !farmer.IsLocalPlayer || Game1.activeClickableMenu != null || !PlayerParty.GetForFarmer(farmer).HasChosenStarter)
		{
			return false;
		}

		long fid = farmer.UniqueMultiplayerID;
		Vector2 currentTile = farmer.Tile;
		_lastEncounterTileByFarmer.TryGetValue(fid, out Vector2 lastTile);
		_lastEncounterTickByFarmer.TryGetValue(fid, out int lastTick);

		if (currentTile == lastTile || (Game1.ticks - lastTick < 180))
		{
			return false;
		}

		_lastEncounterTileByFarmer[fid] = currentTile;

		if (PokemonItemUse.RepelSteps > 0)
		{
			PokemonItemUse.RepelSteps--;
			if (PokemonItemUse.RepelSteps == 0)
			{
				Game1.addHUDMessage(new HUDMessage("O efeito do Repel passou!", 3));
			}
			return false;
		}

		if (Game1.random.NextDouble() >= WildRate)
		{
			return false;
		}

		_lastEncounterTickByFarmer[fid] = Game1.ticks;
		TriggerWildBattle(location);
		return true;
	}

	public static void UpdateOverworldSpawns(GameTime gameTime)
	{
		var loc = Game1.currentLocation;
		var player = Game1.player;
		if (loc == null || player == null || !PlayerParty.GetForFarmer(player).HasChosenStarter)
			return;

		float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
		PokemonMusicManager.UpdateOverworldMusic(dt);
		string locKey = loc.NameOrUniqueName;

		bool validMap = loc.IsOutdoors || loc is MineShaft || loc is Sewer || loc is VolcanoDungeon || locKey.Contains("Cave") || locKey.Contains("Mine");
		if (!validMap)
		{
			_spawnsByLocation.Remove(locKey);
			return;
		}

		if (!_spawnsByLocation.TryGetValue(locKey, out var state))
		{
			state = new LocationSpawnState();
			_spawnsByLocation[locKey] = state;
		}

		long fid = player.UniqueMultiplayerID;
		_lastEncounterTickByFarmer.TryGetValue(fid, out int lastTick);

		// Avoid double-stepping AI movement if two split-screen players are in the same location on the same tick
		bool stepPhysics = (state.LastUpdateTick != Game1.ticks);
		state.LastUpdateTick = Game1.ticks;

		if (stepPhysics)
		{
			state.RefreshTimer -= dt;
			if (state.RefreshTimer <= 0f)
			{
				state.RefreshTimer = 8.0f;
				if (state.Spawns.Count < 3)
				{
					TrySpawnOverworldPokemon(loc, player, state.Spawns);
				}
			}
		}

		for (int i = state.Spawns.Count - 1; i >= 0; i--)
		{
			var s = state.Spawns[i];
			if (stepPhysics)
			{
				s.WalkAnimTimer += dt * 6f;
				s.MoveTimer -= dt;

				if (s.MoveTimer <= 0f)
				{
					if (Game1.random.NextDouble() < 0.55)
					{
						s.IsMoving = true;
						int dir = Game1.random.Next(4);
						s.FacingDir = dir;
						s.Velocity = dir switch
						{
							0 => new Vector2(0, -58f),
							1 => new Vector2(58f, 0),
							2 => new Vector2(0, 58f),
							_ => new Vector2(-58f, 0)
						};
						s.MoveTimer = 0.8f + (float)Game1.random.NextDouble() * 1.2f;
					}
					else
					{
						s.IsMoving = false;
						s.Velocity = Vector2.Zero;
						s.MoveTimer = 1.0f + (float)Game1.random.NextDouble() * 1.5f;
					}
				}

				if (s.IsMoving)
				{
					Vector2 nextPos = s.Position + s.Velocity * dt;
					Rectangle box = new Rectangle((int)nextPos.X - 14, (int)nextPos.Y - 14, 28, 28);
					Point tilePt = new Point((int)(nextPos.X / 64f), (int)(nextPos.Y / 64f));
					if (loc.isTileOnMap(new Vector2(tilePt.X, tilePt.Y)) &&
					    loc.isTilePassable(new xTile.Dimensions.Location(tilePt.X, tilePt.Y), Game1.viewport) &&
					    !loc.Objects.ContainsKey(new Vector2(tilePt.X, tilePt.Y)) &&
					    !loc.isCollidingPosition(box, Game1.viewport, isFarmer: false, damagesFarmer: 0, glider: false, character: null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true, skipCollisionEffects: true))
					{
						s.Position = nextPos;
					}
					else
					{
						s.IsMoving = false;
						s.Velocity = Vector2.Zero;
					}
				}
			}

			if (s.ContactCooldown > 0f)
			{
				s.ContactCooldown = Math.Max(0f, s.ContactCooldown - dt);
			}

			// Despawn check when no farmers are within 15 tiles (960px)
			bool anyFarmerNearby = false;
			foreach (var f in loc.farmers)
			{
				if (Vector2.Distance(f.Position, s.Position) <= 960f)
				{
					anyFarmerNearby = true;
					break;
				}
			}
			if (!anyFarmerNearby)
			{
				s.DespawnTimer -= dt;
				if (s.DespawnTimer <= 0f)
				{
					s.MarkedForRemoval = true;
				}
			}

			if (s.MarkedForRemoval)
			{
				state.Spawns.RemoveAt(i);
				continue;
			}

			// Trigger wild encounter on player contact
			if (s.CurrentBattlerFarmerId == 0L && Game1.activeClickableMenu == null && s.ContactCooldown <= 0f && (Game1.ticks - lastTick >= 150) && PokemonItemUse.RepelSteps <= 0)
			{
				Rectangle pBox = player.GetBoundingBox();
				float dist = Vector2.Distance(new Vector2(pBox.Center.X, pBox.Center.Y), s.Position);
				if (dist <= 48f)
				{
					var wildInst = s.Instance;
					s.ContactCooldown = 4.0f; // Keep spawn in world, with cooldown if fleeing
					s.CurrentBattlerFarmerId = fid;
					_lastEncounterTickByFarmer[fid] = Game1.ticks;
					Game1.playSound("dwoop");
					Game1.activeClickableMenu = new PokemonBattleMenu(wildInst, isWild: true, sourceSpawn: s);
					return;
				}
			}
		}
	}

	/// <summary>Wild encounter Shiny percentage (default 2%, configurable in Debug Menu).</summary>
	public static int WildShinyChancePercent { get; set; } = 2;

	public static double GetLuckMultiplier()
	{
		double dailyLuck = Game1.player != null ? Game1.player.DailyLuck : 0.0;
		int luckLevel = Game1.player != null ? Game1.player.LuckLevel : 0;
		// Stardew Valley dailyLuck: -0.1 (bad spirits) to +0.1 (very happy spirits)
		// Total net luck factoring LuckLevel buffs:
		double netLuck = dailyLuck + (luckLevel * 0.05);
		// Exponential scaling based on spirits:
		return Math.Max(0.2, Math.Pow(2.0, netLuck * 8.0));
	}

	public static bool RollWildShiny()
	{
		if (Game1.random == null) return false;
		double luckMult = GetLuckMultiplier();
		double effectivePercent = WildShinyChancePercent * luckMult;
		if (effectivePercent <= 0) return Game1.random.Next(512) == 0;
		return (Game1.random.NextDouble() * 100.0) <= effectivePercent;
	}

	private static void TrySpawnOverworldPokemon(GameLocation loc, Farmer player, List<OverworldWildSpawn> targetList)
	{
		Point pTile = player.TilePoint;
		for (int attempt = 0; attempt < 15; attempt++)
		{
			int dx = Game1.random.Next(-10, 11);
			int dy = Game1.random.Next(-8, 9);
			if (Math.Abs(dx) < 4 && Math.Abs(dy) < 4) continue;

			int tx = pTile.X + dx;
			int ty = pTile.Y + dy;
			Vector2 tileVec = new Vector2(tx, ty);
			if (!loc.isTileOnMap(tileVec)) continue;
			if (!loc.isTilePassable(new xTile.Dimensions.Location(tx, ty), Game1.viewport)) continue;
			if (loc.Objects.ContainsKey(tileVec)) continue;
			if (loc.isWaterTile(tx, ty)) continue;

			Vector2 worldPos = new Vector2(tx * 64f + 32f, ty * 64f + 32f);
			Rectangle box = new Rectangle((int)worldPos.X - 16, (int)worldPos.Y - 16, 32, 32);
			if (loc.isCollidingPosition(box, Game1.viewport, isFarmer: false, damagesFarmer: 0, glider: false, character: null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true, skipCollisionEffects: true))
				continue;

			bool isNight = Game1.timeOfDay >= 1800;
			bool isRain = Game1.isRaining;
			string speciesName = GetWildSpeciesForLocation(loc, isNight, isRain);
			var species = PokemonSpecies.Get(speciesName);
			if (species == null) continue;

			int level = GetScaledWildLevel();
			var inst = new PokemonInstance(species, level, isShiny: RollWildShiny());
			targetList.Add(new OverworldWildSpawn
			{
				Instance = inst,
				Position = worldPos,
				MoveTimer = 0.5f,
				FacingDir = Game1.random.Next(4)
			});
			break;
		}
	}

	public static void DrawOverworldSpawns(SpriteBatch b)
	{
		if (Game1.currentLocation == null) return;
		if (!_spawnsByLocation.TryGetValue(Game1.currentLocation.NameOrUniqueName, out var state) || state.Spawns.Count == 0)
			return;

		Color drawTint = Color.White;
		if (Game1.isRaining || Game1.currentLocation.IsRainingHere())
		{
			drawTint = new Color(236, 242, 255);
		}
		else if (Game1.currentLocation.IsOutdoors && Game1.timeOfDay >= 1900)
		{
			drawTint = new Color(235, 238, 250);
		}

		foreach (var s in state.Spawns)
		{
			if (s.Instance == null) continue;
			Vector2 screenPos = s.Position - new Vector2(Game1.viewport.X, Game1.viewport.Y);
			if (screenPos.X < -100 || screenPos.X > Game1.viewport.Width + 100 || screenPos.Y < -100 || screenPos.Y > Game1.viewport.Height + 100)
				continue;

			float depth = Math.Clamp((s.Position.Y + 12f) / 10000f, 0.001f, 0.999f);
			PokemonUI.SoftShadow(b, (int)screenPos.X, (int)screenPos.Y + 2, 32, 10, 0.32f, Math.Max(0.0001f, depth - 0.0002f));

			Texture2D icon = s.Instance.GetIconSprite(Game1.graphics.GraphicsDevice) ?? s.Instance.GetFrontSprite(Game1.graphics.GraphicsDevice);
			if (icon != null)
			{
				Rectangle srcRect = (icon == s.Instance.GetIconSprite(Game1.graphics.GraphicsDevice))
					? s.Instance.GetIconSourceRect()
					: s.Instance.GetFrontSourceRect();
				float bounceY = s.IsMoving ? -(float)Math.Abs(Math.Sin(s.WalkAnimTimer * Math.PI * 0.5)) * 4f : 0f;
				// Standard icons face left by default: when moving right (FacingDir == 1), flip horizontally to look right!
				SpriteEffects flip = (s.FacingDir == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				float scale = PokemonInstance.GetOverworldScale(s.Instance, isIcon: (srcRect.Width <= 40));
				Vector2 origin = new Vector2(srcRect.Width / 2f, srcRect.Height - 4f);

				b.Draw(icon, screenPos + new Vector2(0, bounceY), srcRect, drawTint, 0f, origin, scale, flip, depth);

				if (s.Instance.IsShiny)
				{
					PokemonUI.Text(b, Game1.tinyFont, "★", new Rectangle((int)screenPos.X - 10, (int)screenPos.Y - 46, 20, 18), Color.Gold, center: true, maxScale: 0.85f);
				}
			}
		}
	}

	private static int GetScaledWildLevel()
	{
		int partyMax = 5;
		var party = PlayerParty.GetForFarmer(Game1.player);
		if (party?.Team != null && party.Team.Count > 0)
		{
			foreach (var p in party.Team)
			{
				if (p != null && p.Level > partyMax) partyMax = p.Level;
			}
		}
		int minLv = Math.Clamp(partyMax - 5, 3, 60);
		int maxLv = Math.Clamp(partyMax + 2, 7, 70);
		return Game1.random.Next(minLv, maxLv + 1);
	}

	public static void TriggerWildBattle(GameLocation location)
	{
		bool isNight = Game1.timeOfDay >= 1800;
		bool isRain = Game1.isRaining;
		string speciesName = GetWildSpeciesForLocation(location, isNight, isRain);

		int level = GetScaledWildLevel();
		var species = PokemonSpecies.Get(speciesName);
		if (species != null)
		{
			var wildInstance = new PokemonInstance(species, level, isShiny: RollWildShiny());
			Game1.playSound("dwoop");
			Game1.activeClickableMenu = new PokemonBattleMenu(wildInstance, isWild: true);
		}
	}

	public static OverworldWildSpawn SpawnWildAt(GameLocation loc, Point tile, string speciesName, int level = -1, bool? isShiny = null)
	{
		if (loc == null) loc = Game1.currentLocation;
		if (loc == null) return null;

		var species = PokemonSpecies.Get(speciesName);
		if (species == null) return null;

		string locKey = loc.NameOrUniqueName ?? loc.Name;
		if (!_spawnsByLocation.TryGetValue(locKey, out var state))
		{
			state = new LocationSpawnState();
			_spawnsByLocation[locKey] = state;
		}

		if (level <= 0) level = GetScaledWildLevel();
		bool shiny = isShiny ?? RollWildShiny();

		var inst = new PokemonInstance(species, level, isShiny: shiny);
		Vector2 worldPos = new Vector2(tile.X * 64f + 32f, tile.Y * 64f + 32f);

		var spawn = new OverworldWildSpawn
		{
			Instance = inst,
			Position = worldPos,
			MoveTimer = 0.5f,
			FacingDir = Game1.random.Next(4),
			DespawnTimer = 600f
		};
		state.Spawns.Add(spawn);
		return spawn;
	}

	public static string GetWildSpeciesForLocation(GameLocation location, bool isNight, bool isRain)
	{
		string locName = location?.Name ?? "";

		// Static Legendary Item Summoning Encounters (Unique per save)
		Farmer player = Game1.player;
		if (player?.Items != null)
		{
			if (!player.mailReceived.Contains("Poke_Caught_articuno") && player.Items.ContainsId("(O)Poke_FreezingFeather") && (location is MineShaft msFreeze && msFreeze.mineLevel >= 60 && msFreeze.mineLevel <= 79))
				return "articuno";
			if (!player.mailReceived.Contains("Poke_Caught_zapdos") && player.Items.ContainsId("(O)Poke_ElectricFeather") && (isRain || locName.Contains("Mountain")))
				return "zapdos";
			if (!player.mailReceived.Contains("Poke_Caught_moltres") && player.Items.ContainsId("(O)Poke_FlameFeather") && (locName.Contains("Volcano") || locName.Contains("Caldera") || (location is MineShaft msFire && msFire.mineLevel >= 100)))
				return "moltres";
			if (!player.mailReceived.Contains("Poke_Caught_mewtwo") && player.Items.ContainsId("(O)Poke_BerserkGene") && (location is MineShaft msMewtwo && msMewtwo.mineLevel >= 121))
				return "mewtwo";
			if (!player.mailReceived.Contains("Poke_Caught_mew") && player.Items.ContainsId("(O)Poke_OldSeaMap") && (locName.Contains("Beach") || locName.Contains("Island")))
				return "mew";
			if (!player.mailReceived.Contains("Poke_Caught_raikou") && !player.mailReceived.Contains("Poke_Caught_entei") && !player.mailReceived.Contains("Poke_Caught_suicune") && player.Items.ContainsId("(O)Poke_ClearBell") && (locName.Contains("Forest") || locName.Contains("Woods")))
				return Choose(new[] { "raikou", "entei", "suicune" });
			if (!player.mailReceived.Contains("Poke_Caught_lugia") && player.Items.ContainsId("(O)Poke_SilverWing") && (locName.Contains("Beach") || locName.Contains("IslandSouth")))
				return "lugia";
			if (!player.mailReceived.Contains("Poke_Caught_ho_oh") && player.Items.ContainsId("(O)Poke_RainbowWing") && (locName.Contains("Mountain") || locName.Contains("Summit")))
				return "ho_oh";
			if (!player.mailReceived.Contains("Poke_Caught_kyogre") && player.Items.ContainsId("(O)Poke_BlueOrb") && (locName.Contains("Beach") || locName.Contains("IslandSouth")) && isRain)
				return "kyogre";
			if (!player.mailReceived.Contains("Poke_Caught_groudon") && player.Items.ContainsId("(O)Poke_RedOrb") && (locName.Contains("Volcano") || locName.Contains("Caldera")))
				return "groudon";
			if (!player.mailReceived.Contains("Poke_Caught_rayquaza") && player.Items.ContainsId("(O)Poke_JadeOrb") && (locName.Contains("Mountain") || locName.Contains("Summit")))
				return "rayquaza";
			if (!player.mailReceived.Contains("Poke_Caught_dialga") && player.Items.ContainsId("(O)Poke_AdamantOrb") && (location is MineShaft msTime && msTime.mineLevel >= 100))
				return "dialga";
			if (!player.mailReceived.Contains("Poke_Caught_palkia") && player.Items.ContainsId("(O)Poke_LustrousOrb") && (locName.Contains("Beach") || locName.Contains("Island")))
				return "palkia";
			if (!player.mailReceived.Contains("Poke_Caught_giratina") && player.Items.ContainsId("(O)Poke_GriseousCore") && (location is Sewer || locName.Contains("BugLand")))
				return "giratina";
			if (!player.mailReceived.Contains("Poke_Caught_terapagos") && player.Items.ContainsId("(O)Poke_IndigoDisk") && (locName.Contains("Volcano") || locName.Contains("Caldera")))
				return "terapagos";
		}

		// Exponential daily luck roll for rare / pseudo-legendary encounters
		if (Game1.random.NextDouble() < (0.035 * GetLuckMultiplier()))
		{
			return Choose(new[] {
				"dratini", "larvitar", "bagon", "beldum", "gible", "riolu", "deino", "goomy",
				"jangmo_o", "dreepy", "frigibax", "eevee", "rotom", "spiritomb", "charcadet", "tinkatink", "zorua"
			});
		}

		// 1. Mines, Caves, Skull Cavern, Volcano (Gen 1-9 Cavern & Volcanic Pool)
		if (location is MineShaft || locName.Contains("Cave") || locName.Contains("Mine") || locName.Contains("Volcano") || locName.Contains("Caldera"))
		{
			if (locName.Contains("Volcano") || locName.Contains("Caldera"))
			{
				return Choose(new[] {
					"torkoal", "magcargo", "slugma", "charmander", "charmeleon", "magmar", "numel", "camerupt", "ponyta", "houndour",
					"larvesta", "volcarona", "salandit", "turtonator", "rolycoly", "carkol", "coalossal", "charcadet", "ceruledge", "armarouge", "fuecoco"
				});
			}
			if (location is MineShaft ms)
			{
				if (ms.mineLevel >= 80)
				{
					return Choose(new[] {
						"magmar", "torkoal", "magcargo", "lairon", "aggron", "golem", "rhydon", "sableye", "mawile", "banette", "dusclops", "beldum", "metang", "larvitar", "bagon",
						"gible", "gabite", "riolu", "lucario", "excadrill", "axew", "fraxure", "deino", "zweilous", "pawniard", "bisharp", "honedge", "doublade", "jangmo_o", "dreepy", "drakloak", "tinkatuff", "glimmora"
					});
				}
				if (ms.mineLevel >= 40)
				{
					return Choose(new[] {
						"snorunt", "glalie", "spheal", "sealeo", "swinub", "piloswine", "sneasel", "jynx", "golbat", "graveler", "magneton", "haunter",
						"snover", "abomasnow", "vanillite", "cubchoo", "cryogonal", "bergmite", "snom", "frosmoth", "cetoddle", "frigibax", "arctibax"
					});
				}
			}
			return Choose(new[] {
				"geodude", "graveler", "onix", "zubat", "golbat", "machop", "aron", "sableye", "mawile", "diglett", "paras", "clefairy", "nosepass", "whismur", "makuhita", "duskull", "gastly",
				"roggenrola", "boldore", "drilbur", "timburr", "joltik", "ferroseed", "klang", "carbink", "noibat", "rockruff", "rolycoly", "impidimp", "nacli", "tinkatink", "glimmet"
			});
		}

		// 2. Sewers / Bug Land (Gen 1-9 Poison & Dark Pool)
		if (location is Sewer || locName.Contains("Sewer") || locName.Contains("BugLand"))
		{
			return Choose(new[] {
				"grimer", "muk", "koffing", "weezing", "gulpin", "swalot", "zubat", "golbat", "spinarak", "ariados", "venonat", "venomoth", "rattata", "raticate",
				"skorupi", "croagunk", "trubbish", "garbodor", "venipede", "whirlipede", "scolipede", "mareanie", "salandit", "toxel", "toxtricity", "shroodle", "grafaiai", "varoom"
			});
		}

		// 3. Desert / Ginger Island (Gen 1-9 Arid & Tropical Pool)
		if (location is Desert || locName.Contains("Desert") || locName.Contains("Island"))
		{
			if (locName.Contains("Island"))
			{
				if (isRain) return Choose(new[] {
					"tropius", "pelipper", "ludicolo", "exeggutor", "kingler", "corsola", "lapras",
					"goodra", "goomy", "popplio", "mareanie", "dewpider", "araquanid", "cramorant", "quaxly", "finizen", "palafin"
				});
				return Choose(new[] {
					"tropius", "exeggcute", "exeggutor", "treecko", "torchic", "mudkip", "heracross", "slakoth", "vigoroth", "wingull", "krabby", "marowak",
					"rowlet", "litten", "popplio", "pikipek", "toucannon", "fomantis", "lurantis", "comfey", "oranguru", "passimian", "sandygast", "mimikyu", "jangmo_o", "flamigo"
				});
			}
			if (isNight) return Choose(new[] {
				"cacnea", "cacturne", "trapinch", "vibrava", "sandslash", "houndour", "murkrow", "baltoy", "claydol", "lunatone",
				"sandile", "krokorok", "scraggy", "sigilyph", "yamask", "pawniard", "mandibuzz", "silicobra", "sandaconda", "lokix", "mabosstiff"
			});
			return Choose(new[] {
				"sandshrew", "sandslash", "trapinch", "vibrava", "cacnea", "baltoy", "cubone", "marowak", "doduo", "phanpy", "numel", "solrock",
				"hippopotas", "sandile", "darumaka", "dwebble", "maractus", "helioptile", "mudbray", "silicobra", "orthworm", "capsakid", "scovillain", "rellor"
			});
		}

		// 4. Beach / Ocean shore (Gen 1-9 Coastal Pool)
		if (location is Beach || locName.Contains("Beach"))
		{
			if (isRain) return Choose(new[] {
				"lotad", "lombre", "squirtle", "wartortle", "poliwhirl", "pelipper", "psyduck", "golduck", "marill", "azumarill", "spheal",
				"piplup", "oshawott", "froakie", "popplio", "sobble", "quaxly", "mareanie", "arrokuda", "wattrel", "kilowattrel", "finizen"
			});
			if (isNight) return Choose(new[] {
				"krabby", "kingler", "corphish", "staryu", "shellder", "slowpoke", "murkrow", "shuppet", "psyduck",
				"frillish", "jellicent", "inkay", "malamar", "mareanie", "sandygast", "pyukumuku", "pincurchin", "clodsire"
			});
			return Choose(new[] {
				"krabby", "corphish", "wingull", "pelipper", "psyduck", "slowpoke", "squirtle", "marill", "shellder", "taillow", "pidgey",
				"buizel", "shellos", "oshawott", "tirtouga", "binacle", "clauncher", "skrelp", "wimpod", "chewtle", "cramorant", "wiglett", "tatsugiri"
			});
		}

		// 5. Woods / Secret Woods / Forest (Gen 1-9 Woodland & Fairy Pool)
		if (location is Forest || locName.Contains("Woods") || locName.Contains("Forest"))
		{
			if (isRain) return Choose(new[] {
				"lotad", "lombre", "mudkip", "surskit", "masquerain", "roselia", "oddish", "gloom", "shroomish", "breloom", "poliwag", "wooper",
				"tympole", "palpitoad", "goomy", "sliggoo", "dewpider", "chewtle", "tadbulb", "bellibolt", "wooper", "clodsire"
			});
			if (isNight) return Choose(new[] {
				"poochyena", "mightyena", "hoothoot", "noctowl", "spinarak", "gastly", "haunter", "duskull", "shuppet", "murkrow", "umbreon", "absol",
				"mismagius", "honchkrow", "zorua", "zoroark", "litwick", "lampent", "phantump", "trevenant", "pumpkaboo", "mimikyu", "impidimp", "morgrem", "hatenna", "greavard"
			});
			return Choose(new[] {
				"caterpie", "metapod", "butterfree", "weedle", "beedrill", "wurmple", "beautifly", "oddish", "bellsprout", "shroomish", "seedot", "nuzleaf", "treecko", "bulbasaur", "chikorita", "pikachu", "ralts", "scyther", "pinsir", "heracross",
				"turtwig", "budew", "roserade", "combee", "cherubi", "snivy", "sewaddle", "leavanny", "cottonee", "whimsicott", "petilil", "lilligant", "chespin", "flabebe", "sylveon", "rowlet", "grubbin", "cutiefly", "grookey", "gossifleur", "applin", "sprigatito", "smoliv", "tinkatink"
			});
		}

		// 6. Mountain / Quarry / Railroad (Gen 1-9 Highland & Dragon/Steel Pool)
		if (location is Mountain || locName.Contains("Mountain") || locName.Contains("Railroad") || locName.Contains("Backwoods"))
		{
			if (isRain) return Choose(new[] {
				"electrike", "manectric", "magnemite", "voltorb", "aron", "larvitar", "gligar", "wooper", "quagsire",
				"shinx", "luxio", "magnezone", "joltik", "galvantula", "tynamo", "helioptile", "vikavolt", "toxel", "pawmi", "pawmo", "tadbulb"
			});
			if (isNight) return Choose(new[] {
				"sneasel", "houndour", "absol", "lunatone", "clefairy", "zubat", "golbat", "noctowl", "gligar", "duskull",
				"gliscor", "weavile", "zorua", "deino", "honedge", "noibat", "noivern", "lycanroc", "corviknight", "dreepy", "charcadet", "kingambit"
			});
			return Choose(new[] {
				"machop", "mankey", "spearow", "fearow", "taillow", "swablu", "geodude", "phanpy", "teddiursa", "skarmory", "numel", "spoink", "charmander", "cyndaquil", "torchic",
				"chimchar", "starly", "staravia", "cranidos", "shieldon", "gible", "riolu", "tepig", "roggenrola", "excadrill", "axew", "rufflet", "fennekin", "fletchling", "talonflame", "hawlucha", "rockruff", "mudbray", "scorbunny", "rookidee", "cufant", "duraludon", "fuecoco", "charcadet", "klawf", "frigibax"
			});
		}

		// 7. Town / BusStop / Farm default (Gen 1-9 Meadow & Village Pool)
		if (isRain)
		{
			return Choose(new[] {
				"lotad", "poliwag", "poliwhirl", "wooper", "mudkip", "squirtle", "totodile", "plusle", "minun", "magnemite", "roselia",
				"piplup", "buizel", "oshawott", "tympole", "ducklett", "froakie", "goomy", "popplio", "sobble", "chewtle", "quaxly", "tadbulb"
			});
		}
		if (isNight)
		{
			return Choose(new[] {
				"poochyena", "rattata", "meowth", "hoothoot", "zubat", "gastly", "drowzee", "snubbull", "ralts", "abra", "illumise", "volbeat",
				"shinx", "drifloon", "stunky", "purrloin", "munna", "zorua", "gothita", "espurr", "spritzee", "swirlix", "litten", "nickit", "impidimp", "hatenna", "maschiff", "shroodle"
			});
		}

		// Day & Sunny
		return Choose(new[] {
			"zigzagoon", "sentret", "pidgey", "rattata", "pikachu", "eevee", "oddish", "hoppip", "sunkern", "mareep", "flaaffy", "skitty", "ralts", "abra", "growlithe", "vulpix", "bulbasaur",
			"starly", "bidoof", "shinx", "pachirisu", "buneary", "munchlax", "lillipup", "pidove", "blitzle", "audino", "minccino", "emolga", "bunnelby", "fletchling", "litleo", "skiddo", "pancham", "dedenne", "pikipek", "rockruff", "stufful", "wooloo", "yamper", "lechonk", "pawmi", "tandemaus", "fidough", "sprigatito"
		});
	}

	public static string GetFishingSpecies(GameLocation location, bool isLava, bool isSewer, bool isOcean, bool isRiver, bool isLake, bool isNight, bool isRain)
	{
		// 1. Lava Fishing (Mines 100+, Caldera, Volcano)
		if (isLava)
		{
			return Choose(new[] { "torkoal", "magcargo", "slugma", "magmar", "charmander", "charmeleon", "houndoom", "camerupt", "salazzle", "turtonator", "coalossal", "centiskorch", "ceruledge" });
		}

		// 2. Sewers (Esgoto / Mutated Bug Lair)
		if (isSewer)
		{
			return Choose(new[] { "grimer", "muk", "koffing", "weezing", "qwilfish", "tentacool", "tentacruel", "barboach", "whiscash", "skrelp", "dragalge", "mareanie", "toxapex", "clodsire" });
		}

		// 3. Ocean / Beach
		if (isOcean)
		{
			if (isRain) return Choose(new[] { "gyarados", "lapras", "lanturn", "chinchou", "tentacruel", "starmie", "kingdra", "wailmer", "mantine", "gorebyss", "huntail", "relicanth", "jellicent", "dragalge", "clawitzer", "araquanid", "dhelmise", "basculegion", "palafin", "dondozo" });
			if (isNight) return Choose(new[] { "staryu", "starmie", "chinchou", "lanturn", "shellder", "cloyster", "carvanha", "sharpedo", "horsea", "seadra", "frillish", "jellicent", "skrelp", "mareanie", "pyukumuku", "veluza" });
			return Choose(new[] { "magikarp", "tentacool", "horsea", "krabby", "shellder", "staryu", "corsola", "remoraid", "octillery", "wailmer", "spheal", "clamperl", "luvdisc", "finneon", "lumineon", "tirtouga", "clauncher", "wimpod", "arrokuda", "finizen", "wiglett", "tatsugiri" });
		}

		// 4. Rivers (Town / Forest)
		if (isRiver)
		{
			if (isRain) return Choose(new[] { "politoed", "poliwhirl", "poliwrath", "lombre", "ludicolo", "quagsire", "mudkip", "marshtomp", "azumarill", "feebas", "milotic", "floatzel", "seismitoad", "greninja", "primarina", "inteleon", "quaquaval" });
			if (isNight) return Choose(new[] { "poliwhirl", "golduck", "wooper", "quagsire", "corphish", "crawdaunt", "barboach", "whiscash", "murkrow", "basculin", "greninja", "drednaw", "clodsire" });
			return Choose(new[] { "magikarp", "goldeen", "seaking", "poliwag", "psyduck", "krabby", "corphish", "marill", "lotad", "surskit", "squirtle", "totodile", "mudkip", "piplup", "buizel", "oshawott", "basculin", "froakie", "chewtle", "quaxly" });
		}

		// 5. Lakes (Mountain Lake, Farm Lake, Secret Woods Pond)
		if (isRain) return Choose(new[] { "dratini", "dragonair", "milotic", "feebas", "gyarados", "lapras", "slowbro", "slowking", "quagsire", "goodra", "wishiwashi", "basculegion", "dondozo", "tatsugiri" });
		if (isNight) return Choose(new[] { "dratini", "dragonair", "poliwhirl", "golduck", "gyarados", "carvanha", "whiscash", "basculin", "dewpider", "drednaw", "tatsugiri" });
		return Choose(new[] { "magikarp", "goldeen", "seaking", "psyduck", "slowpoke", "poliwag", "marill", "barboach", "feebas", "dratini", "buizel", "tympole", "basculin", "wishiwashi", "chewtle", "arrokuda", "tatsugiri" });
	}

	private static string Choose(string[] list)
	{
		if (list == null || list.Length == 0) return "magikarp";
		return list[Game1.random.Next(list.Length)].ToLowerInvariant();
	}
}
