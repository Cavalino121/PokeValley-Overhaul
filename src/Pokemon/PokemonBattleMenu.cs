using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;
using StardewValley.Locations;

namespace StardewValley.Pokemon;

public class PokemonBattleMenu : IClickableMenu
{
	public enum BattlePhase
	{
		Intro,
		ActionSelect,
		MoveSelect,
		BagSelect,
		SwitchSelect,
		MessageWait,
		Victory,
		Defeat
	}

	public enum ArenaTheme
	{
		Farm,
		Town,
		Cave,
		Beach,
		Forest,
		Interior
	}

	private class FloatingDamage
	{
		public string Text;
		public Vector2 Position;
		public float Alpha = 1f;
		public float Timer = 0f;
	}

	public static readonly Dictionary<long, PokemonBattleMenu> ActiveBattles = new();

	public bool IsSpectator { get; }
	public PokemonBattleMenu WatchedBattle { get; }
	private ClickableComponent _spectatorExitBtn;

	public bool IsWild { get; }
	public string TrainerName { get; }
	public List<PokemonInstance> EnemyTeam { get; }
	public int EnemyIndex { get; private set; } = 0;
	public PokemonInstance EnemyPokemon => (EnemyIndex >= 0 && EnemyIndex < EnemyTeam.Count) ? EnemyTeam[EnemyIndex] : null;

	public long BattlerFarmerId { get; private set; }
	public List<PokemonInstance> PlayerTeam { get; private set; }
	public int PlayerActiveIndex { get; private set; } = 0;
	public PokemonInstance PlayerPokemon => (PlayerActiveIndex >= 0 && PlayerTeam != null && PlayerActiveIndex < PlayerTeam.Count) ? PlayerTeam[PlayerActiveIndex] : null;
	public PokemonEncounterManager.OverworldWildSpawn SourceWildSpawn { get; set; }

	public BattlePhase Phase { get; private set; } = BattlePhase.Intro;

	private readonly Queue<string> _messageQueue = new();
	private string _currentMessage = "";
	private Action _afterMessagesAction;
	private float _messageTimer = 0f;
	private const float AutoAdvanceDelay = 1.7f;

	// Buttons
	private readonly List<ClickableComponent> _actionButtons = new();
	private readonly List<ClickableComponent> _moveButtons = new();
	private readonly List<ClickableComponent> _bagButtons = new();
	private readonly List<ClickableComponent> _switchButtons = new();
	private ClickableComponent _backButton;
	private ClickableComponent _megaToggleBtn;
	private ClickableComponent _zMoveToggleBtn;
	private ClickableComponent _dynamaxToggleBtn;
	private ClickableComponent _gmaxToggleBtn;
	private ClickableComponent _teraToggleBtn;
	private int _bagPage = 0;
	private ClickableComponent _bagPrevButton;
	private ClickableComponent _bagNextButton;

	// Gamepad Navigation Indices
	private int _gamepadActionIndex = 0;
	private int _gamepadMoveIndex = 0;
	private int _gamepadBagIndex = 0;
	private int _gamepadSwitchIndex = 0;

	// Gen 6-9 Battle Gimmicks State
	private bool _megaUsedThisBattle = false;
	private bool _zMoveUsedThisBattle = false;
	private bool _dynamaxUsedThisBattle = false;
	private bool _teraUsedThisBattle = false;
	private bool _megaToggled = false;
	private bool _zMoveToggled = false;
	private bool _dynamaxToggled = false;
	private bool _gmaxToggled = false;
	private bool _teraToggled = false;
	private float _startupTransitionTimer = 0.70f;

	// Combat Stat Stages (-6 to +6)
	private int _playerAtkStage = 0;
	private int _playerDefStage = 0;
	private int _playerSpAtkStage = 0;
	private int _playerSpDefStage = 0;
	private int _playerSpeedStage = 0;

	private int _enemyAtkStage = 0;
	private int _enemyDefStage = 0;
	private int _enemySpAtkStage = 0;
	private int _enemySpDefStage = 0;
	private int _enemySpeedStage = 0;

	private static float GetStageMultiplier(int stage)
	{
		stage = Math.Clamp(stage, -6, 6);
		return (stage >= 0) ? ((2f + stage) / 2f) : (2f / (2f - stage));
	}

	private ref int StageRef(PokemonInstance mon, PokemonStat stat)
	{
		bool player = mon == PlayerPokemon;
		if (player)
		{
			switch (stat)
			{
				case PokemonStat.Attack: return ref _playerAtkStage;
				case PokemonStat.Defense: return ref _playerDefStage;
				case PokemonStat.SpAttack: return ref _playerSpAtkStage;
				case PokemonStat.SpDefense: return ref _playerSpDefStage;
				default: return ref _playerSpeedStage;
			}
		}

		switch (stat)
		{
			case PokemonStat.Attack: return ref _enemyAtkStage;
			case PokemonStat.Defense: return ref _enemyDefStage;
			case PokemonStat.SpAttack: return ref _enemySpAtkStage;
			case PokemonStat.SpDefense: return ref _enemySpDefStage;
			default: return ref _enemySpeedStage;
		}
	}

	private static string StatLabel(PokemonStat stat)
	{
		return stat switch
		{
			PokemonStat.Attack => "Ataque",
			PokemonStat.Defense => "Defesa",
			PokemonStat.SpAttack => "Ataque Especial",
			PokemonStat.SpDefense => "Defesa Especial",
			_ => "Velocidade"
		};
	}

	/// <summary>Applies one stage change and returns the message to show, or null when nothing happened.</summary>
	private string ApplyStageChange(PokemonInstance target, StatStageChange change)
	{
		if (target == null || target.IsFainted) return null;
		if (change.Chance < 100 && Game1.random.Next(1, 101) > change.Chance) return null;

		int delta = change.Stages;
		if (string.Equals(target.Ability, "Contrary", StringComparison.OrdinalIgnoreCase))
		{
			delta = -delta;
		}

		ref int stage = ref StageRef(target, change.Stat);
		int before = stage;
		stage = Math.Clamp(stage + delta, -6, 6);
		if (stage == before) return null;

		string label = StatLabel(change.Stat);
		bool up = change.Stages > 0;
		string intensity = Math.Abs(change.Stages) >= 2 ? " muito" : "";
		return up
			? $"{label} de {target.Nickname} subiu{intensity}!"
			: $"{label} de {target.Nickname} caiu{intensity}!";
	}

	private void ApplyStatChanges(PokemonInstance target, IReadOnlyList<StatStageChange> changes, Action onDone)
	{
		if (changes == null || changes.Count == 0)
		{
			onDone?.Invoke();
			return;
		}

		var messages = new List<string>();
		foreach (var change in changes)
		{
			if (change.Chance < 100 && Game1.random.Next(1, 101) > change.Chance) continue;
			string message = ApplyStageChange(target, change);
			if (message != null) messages.Add(message);
		}

		if (messages.Count == 0)
		{
			onDone?.Invoke();
			return;
		}

		Game1.playSound(messages[0].Contains("subiu") ? "reward" : "debuffHit");
		QueueMessageChain(messages, 0, onDone);
	}

	private void QueueMessageChain(List<string> messages, int index, Action onDone)
	{
		if (messages == null || index >= messages.Count)
		{
			onDone?.Invoke();
			return;
		}

		QueueMessage(messages[index], () => QueueMessageChain(messages, index + 1, onDone));
	}

	private void ResetStages(PokemonInstance target)
	{
		if (target == null) return;
		StageRef(target, PokemonStat.Attack) = 0;
		StageRef(target, PokemonStat.Defense) = 0;
		StageRef(target, PokemonStat.SpAttack) = 0;
		StageRef(target, PokemonStat.SpDefense) = 0;
		StageRef(target, PokemonStat.Speed) = 0;
	}

	private void InvertStages(PokemonInstance target)
	{
		if (target == null) return;
		StageRef(target, PokemonStat.Attack) = -StageRef(target, PokemonStat.Attack);
		StageRef(target, PokemonStat.Defense) = -StageRef(target, PokemonStat.Defense);
		StageRef(target, PokemonStat.SpAttack) = -StageRef(target, PokemonStat.SpAttack);
		StageRef(target, PokemonStat.SpDefense) = -StageRef(target, PokemonStat.SpDefense);
		StageRef(target, PokemonStat.Speed) = -StageRef(target, PokemonStat.Speed);
	}

	private int PositiveStages(PokemonInstance mon)
	{
		if (mon == null) return 0;
		int total = 0;
		total += Math.Max(0, StageRef(mon, PokemonStat.Attack));
		total += Math.Max(0, StageRef(mon, PokemonStat.Defense));
		total += Math.Max(0, StageRef(mon, PokemonStat.SpAttack));
		total += Math.Max(0, StageRef(mon, PokemonStat.SpDefense));
		total += Math.Max(0, StageRef(mon, PokemonStat.Speed));
		return total;
	}

private PokemonMove SelectEnemyMove()
	{
		if (EnemyPokemon == null || EnemyPokemon.Moves == null || EnemyPokemon.Moves.Count == 0)
			return PokemonMove.Get("Tackle");

		if (EnemyPokemon.Moves.Count == 1)
			return EnemyPokemon.Moves[0];

		if (EnemyPokemon.RampageTurns > 0 && !string.IsNullOrEmpty(EnemyPokemon.RampageMoveName))
		{
			var rampage = EnemyPokemon.Moves.Find(m => m.Name.Equals(EnemyPokemon.RampageMoveName, StringComparison.OrdinalIgnoreCase));
			if (rampage != null) return rampage;
		}

		var player = PlayerPokemon;
		if (player == null || player.Species == null)
		{
			return EnemyPokemon.Moves[Game1.random.Next(EnemyPokemon.Moves.Count)];
		}

		PokemonType pType1 = player.Species.PrimaryType;
		PokemonType? pType2 = player.Species.SecondaryType;
		if (player.IsTerastallized)
		{
			pType1 = player.TeraType;
			pType2 = null;
		}

		bool playerLevitates = string.Equals(player.Ability, "Levitate", StringComparison.OrdinalIgnoreCase) ||
		                       pType1 == PokemonType.Flying ||
		                       pType2 == PokemonType.Flying;

		var scored = new List<(PokemonMove Move, int Weight)>();
		foreach (var move in EnemyPokemon.Moves)
		{
			int weight = 100;
			float eff = TypeChart.GetEffectiveness(move.Type, pType1);
			if (pType2.HasValue) eff *= TypeChart.GetEffectiveness(move.Type, pType2.Value);

			if (eff == 0f || (move.Type == PokemonType.Ground && playerLevitates))
			{
				weight = 0;
			}
			else
			{
				if (eff >= 2.0f) weight += 80;
				else if (eff < 1.0f) weight = Math.Max(10, weight - 40);

				if (move.Power > 0 && (move.Type == EnemyPokemon.Species.PrimaryType || move.Type == EnemyPokemon.Species.SecondaryType))
				{
					weight += 30;
				}

				if (move.Power == 0 && move.StatusEffect != PokemonStatusCondition.None)
				{
					if (player.Status != PokemonStatusCondition.None)
					{
						weight = 0;
					}
				}

				if (move.SelfHealRatio > 0f)
				{
					float hpRatio = (float)EnemyPokemon.CurrentHP / Math.Max(1, EnemyPokemon.MaxHP);
					if (hpRatio > 0.85f) weight = 5;
					else if (hpRatio < 0.45f) weight += 100;
				}
			}

			scored.Add((move, weight));
		}

		int total = 0;
		foreach (var s in scored) total += s.Weight;

		if (total <= 0)
		{
			return EnemyPokemon.Moves[Game1.random.Next(EnemyPokemon.Moves.Count)];
		}

		int roll = Game1.random.Next(total);
		int accum = 0;
		foreach (var s in scored)
		{
			accum += s.Weight;
			if (roll < accum) return s.Move;
		}

		return EnemyPokemon.Moves[0];
	}

	private static int EffectiveSpeedOf(PokemonInstance mon)
	{
		if (mon == null) return 1;
		float speed = mon.Status == PokemonStatusCondition.Paralysis ? mon.Speed * 0.5f : mon.Speed;

		// Weather Abilities
		bool isRain = Game1.isRaining || (Game1.currentLocation != null && Game1.currentLocation.IsRainingHere());
		bool isSunny = !isRain && Game1.currentLocation != null && Game1.currentLocation.IsOutdoors;
		if (isRain && string.Equals(mon.Ability, "SwiftSwim", StringComparison.OrdinalIgnoreCase)) speed *= 2f;
		else if (isSunny && string.Equals(mon.Ability, "Chlorophyll", StringComparison.OrdinalIgnoreCase)) speed *= 2f;
		else if (string.Equals(mon.Ability, "SandRush", StringComparison.OrdinalIgnoreCase) && Game1.currentLocation is StardewValley.Locations.MineShaft) speed *= 2f;
		else if (string.Equals(mon.Ability, "SlushRush", StringComparison.OrdinalIgnoreCase) && Game1.IsWinter) speed *= 2f;
		else if (string.Equals(mon.Ability, "SurgeSurfer", StringComparison.OrdinalIgnoreCase)) speed *= 1.5f;

		// Choice Scarf (+50% Speed)
		if (PokemonHeldItemManager.Matches(mon.HeldItem, "ChoiceScarf")) speed *= 1.5f;

		return Math.Max(1, (int)speed);
	}

	/// <summary>Resolves power scaling and conditional doubling for the current board state.</summary>
	private int ResolveMovePower(PokemonMove move, PokemonInstance attacker, PokemonInstance defender, int basePower)
	{
		if (move == null || attacker == null || defender == null) return basePower;

		int power = basePower;
		switch (move.PowerScaling)
		{
			case MovePowerScaling.UserHp:
				power = Math.Max(1, (int)(basePower * (float)attacker.CurrentHP / Math.Max(1, attacker.MaxHP)));
				break;
			case MovePowerScaling.TargetHp:
				power = Math.Max(1, (int)(basePower * (float)defender.CurrentHP / Math.Max(1, defender.MaxHP)));
				break;
			case MovePowerScaling.GyroBall:
				power = Math.Clamp(25 * EffectiveSpeedOf(defender) / EffectiveSpeedOf(attacker), 1, 150);
				break;
			case MovePowerScaling.ElectroBall:
			{
				float ratio = (float)EffectiveSpeedOf(attacker) / EffectiveSpeedOf(defender);
				power = ratio >= 4f ? 150 : ratio >= 3f ? 120 : ratio >= 2f ? 80 : ratio >= 1f ? 60 : 40;
				break;
			}
			case MovePowerScaling.UserBoosts:
				power = Math.Min(860, basePower + 20 * PositiveStages(attacker));
				break;
			case MovePowerScaling.TargetBoosts:
				power = Math.Min(200, basePower + 20 * PositiveStages(defender));
				break;
		}

		switch (move.BonusCondition)
		{
			case MoveBonusCondition.TargetBelowHalfHp:
				if (defender.CurrentHP <= defender.MaxHP / 2) power *= 2;
				break;
			case MoveBonusCondition.TargetHasStatus:
				if (defender.Status != PokemonStatusCondition.None) power *= 2;
				break;
			case MoveBonusCondition.VenoshockTargetPoisoned:
				if (defender.Status is PokemonStatusCondition.Poison or PokemonStatusCondition.Toxic) power *= 2;
				break;
			case MoveBonusCondition.SmellingSaltsTargetParalyzed:
				if (defender.Status == PokemonStatusCondition.Paralysis) power *= 2;
				break;
			case MoveBonusCondition.WakeUpSlapTargetAsleep:
				if (defender.Status == PokemonStatusCondition.Sleep) power *= 2;
				break;
		}

		return Math.Max(1, power);
	}

	private int ResolveHitCount(PokemonMove move)
	{
		if (move == null || move.HitCountMax <= 1) return 1;
		if (move.HitCountMin == move.HitCountMax) return move.HitCountMin;

		// Gen 5+ distribution: 35% two hits, 35% three, 15% four, 15% five.
		int roll = Game1.random.Next(100);
		if (roll < 35) return 2;
		if (roll < 70) return 3;
		if (roll < 85) return 4;
		return 5;
	}

	private int _boxX;
	private int _boxY;
	private int _boxWidth;
	private int _boxHeight;

	// Animation & Visual States
	private ArenaTheme _currentTheme;
	private float _displayPlayerHP;
	private float _displayEnemyHP;
	private float _playerDamageShake = 0f;
	private float _enemyDamageShake = 0f;
	private Vector2 _playerLungeOffset = Vector2.Zero;
	private Vector2 _enemyLungeOffset = Vector2.Zero;
	private readonly List<FloatingDamage> _floatingDamages = new();


	public enum BattleWeather
	{
		None,
		Rain,
		HarshSun,
		Sandstorm,
		Snow
	}

	public BattleWeather CurrentWeather = BattleWeather.None;
	public int WeatherTurns = 0;

	private struct WeatherParticle
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public float Size;
		public float Alpha;
		public float Rotation;
	}

	private readonly List<WeatherParticle> _weatherParticles = new();
	private float _weatherAnimTimer = 0f;

	private void InitWeatherParticles()
	{
		_weatherParticles.Clear();
		for (int i = 0; i < 50; i++)
		{
			_weatherParticles.Add(new WeatherParticle
			{
				Position = new Vector2(Game1.random.Next(width), Game1.random.Next(_boxY > 0 ? _boxY : 600)),
				Velocity = new Vector2(-40f + Game1.random.Next(-10, 11), 500f + Game1.random.Next(-50, 51)),
				Size = 1.0f + (float)Game1.random.NextDouble() * 1.5f,
				Alpha = 0.4f + (float)Game1.random.NextDouble() * 0.45f,
				Rotation = 0f
			});
		}
	}

	private void UpdateWeatherParticles(float dt)
	{
		_weatherAnimTimer += dt;
		if (CurrentWeather == BattleWeather.None) return;

		int maxY = _boxY > 0 ? _boxY : 600;
		for (int i = 0; i < _weatherParticles.Count; i++)
		{
			var p = _weatherParticles[i];
			if (CurrentWeather == BattleWeather.Rain)
			{
				p.Position += new Vector2(-60f, 650f) * dt;
				if (p.Position.Y > maxY || p.Position.X < -50)
				{
					p.Position = new Vector2(Game1.random.Next(width + 100), -20);
				}
			}
			else if (CurrentWeather == BattleWeather.Sandstorm)
			{
				p.Position += new Vector2(450f, 25f + (float)Math.Sin(_weatherAnimTimer * 3f + i) * 15f) * dt;
				p.Rotation += dt * 4f;
				if (p.Position.X > width + 50)
				{
					p.Position = new Vector2(-30, Game1.random.Next(maxY));
				}
			}
			else if (CurrentWeather == BattleWeather.Snow)
			{
				p.Position += new Vector2((float)Math.Sin(_weatherAnimTimer * 2f + i) * 35f, 90f) * dt;
				p.Rotation += dt * 1.2f;
				if (p.Position.Y > maxY)
				{
					p.Position = new Vector2(Game1.random.Next(width), -15);
				}
			}
			_weatherParticles[i] = p;
		}
	}

	private void DrawWeatherEffects(SpriteBatch b, Rectangle arenaRect)
	{
		if (CurrentWeather == BattleWeather.None) return;

		if (CurrentWeather == BattleWeather.HarshSun)
		{
			// Golden radiant warm ambient wash
			float pulse = 0.08f + (float)Math.Sin(_weatherAnimTimer * 2.5f) * 0.03f;
			b.Draw(Game1.staminaRect, arenaRect, Color.Gold * pulse);

			// Radiant sunbeams across sky
			int beamCount = 6;
			for (int i = 0; i < beamCount; i++)
			{
				float beamX = (arenaRect.Width / (beamCount + 1)) * (i + 1) + (float)Math.Sin(_weatherAnimTimer + i) * 40f;
				Rectangle beamRect = new Rectangle((int)beamX - 18, arenaRect.Y, 36, arenaRect.Height);
				b.Draw(Game1.staminaRect, beamRect, Color.LightGoldenrodYellow * (pulse * 0.75f));
			}
		}
		else if (CurrentWeather == BattleWeather.Rain)
		{
			// Subtle rain ambient tint
			b.Draw(Game1.staminaRect, arenaRect, Color.MidnightBlue * 0.12f);

			// Rain streaks
			foreach (var p in _weatherParticles)
			{
				Rectangle dropRect = new Rectangle((int)p.Position.X, (int)p.Position.Y, 2, (int)(16 * p.Size));
				b.Draw(Game1.staminaRect, dropRect, null, Color.LightSkyBlue * p.Alpha, -0.15f, Vector2.Zero, SpriteEffects.None, 0.95f);
			}
		}
		else if (CurrentWeather == BattleWeather.Sandstorm)
		{
			// Sandy brown atmospheric haze
			b.Draw(Game1.staminaRect, arenaRect, Color.SandyBrown * 0.18f);

			// Swirling dust particles
			foreach (var p in _weatherParticles)
			{
				Rectangle sandRect = new Rectangle((int)p.Position.X, (int)p.Position.Y, (int)(4 * p.Size), (int)(3 * p.Size));
				b.Draw(Game1.staminaRect, sandRect, null, Color.BurlyWood * p.Alpha, p.Rotation, new Vector2(2, 2), SpriteEffects.None, 0.95f);
			}
		}
		else if (CurrentWeather == BattleWeather.Snow)
		{
			// Cold blue-white mist
			b.Draw(Game1.staminaRect, arenaRect, Color.LightCyan * 0.10f);

			// Drifting snow crystals
			foreach (var p in _weatherParticles)
			{
				Rectangle flakeRect = new Rectangle((int)p.Position.X, (int)p.Position.Y, (int)(5 * p.Size), (int)(5 * p.Size));
				b.Draw(Game1.staminaRect, flakeRect, null, Color.White * p.Alpha, p.Rotation, new Vector2(2, 2), SpriteEffects.None, 0.95f);
			}
		}
	}

	private class AttackFx
	{
		public PokemonType Type;
		public string MoveName;
		public PokemonMove Move;
		public bool IsPlayerAttacking;
		public float Timer = 0f;
		public float Duration = 0.65f;
		public Vector2 AttackerPos;
		public Vector2 DefenderPos;
		public int Damage;
		public int Hits = 1;
		public bool IsCrit;
		public float TypeMult;
		public PokemonInstance Attacker;
		public PokemonInstance Defender;
		public Action OnComplete;
	}

	private AttackFx _activeAttackFx;

	// Switch Animation State (Retrieve & Throw)
	private enum SwitchStage
	{
		None,
		Retrieve,
		Throw,
		Appear
	}

	private SwitchStage _switchStage = SwitchStage.None;
	private bool _isSwitching = false;
	private float _switchTimer = 0f;
	private int _switchTargetIndex = -1;
	private float _playerSpriteScale = 0f;
	private Color _playerSpriteTint = Color.White;

	// Trainer Intro Animation State
	private float _playerTrainerSlide = 0f;
	private float _enemyTrainerSlide = 0f;
	private int _playerTrainerFrame = 0;
	private float _playerThrowTimer = 0f;
	private bool _isPlayerThrowing = false;
	private bool _isEnemySending = false;
	private float _enemySendTimer = 0f;
	private bool _isEnemyRetrieving = false;
	private float _enemyRetrieveTimer = 0f;
	private float _enemyPokemonScale = 3.2f;

	// Battle Textures Cache
	private static Texture2D _treeStrip;
	private static Texture2D _cloudsTex;
	private static Texture2D _ballTex;
	private static Texture2D _ballOpenTex;
	private Texture2D _playerTrainerTex;
	private Texture2D _enemyTrainerTex;
	private float _cloudOffset = 0f;

	// Pokeball Capture Animation State
	private enum CaptureStage
	{
		None,
		ThrowArc,
		OpenAndAbsorb,
		BallDrop,
		PauseBeforeWiggle,
		Wiggle,
		CaughtSuccess,
		BreakFree
	}

	private bool _isCapturing = false;
	private CaptureStage _captureStage = CaptureStage.None;
	private float _captureTimer = 0f;
	private string _capturingBallId = "PokeBall";
	private bool _captureResult = false;
	private int _allowedWiggles = 3;
	private int _currentWiggleCount = 0;
	private float _captureBallRot = 0f;
	private Vector2 _captureBallPos = Vector2.Zero;
	private Texture2D _activeCaptureBallTex;
	private readonly List<Vector2> _captureStars = new();
	private static readonly Dictionary<string, Texture2D> _ballTexturesCache = new(StringComparer.OrdinalIgnoreCase);

	private static Texture2D GetBallTexture(string ballId)
	{
		if (_ballTexturesCache.TryGetValue(ballId, out var cached) && cached != null && !cached.IsDisposed)
			return cached;

		string file = ballId switch
		{
			"GreatBall" => "great.png",
			"UltraBall" => "ultra.png",
			"MasterBall" => "master.png",
			"PremierBall" => "premier.png",
			"FastBall" => "timer.png",
			"HeavyBall" => "luxury.png",
			"HealBall" => "nest.png",
			_ => "poke.png"
		};

		string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald", "graphics", "balls", file);
		if (File.Exists(p))
		{
			var tex = PokemonUI.LoadColorKeyed(p);
			_ballTexturesCache[ballId] = tex;
			return tex;
		}
		return _ballTex;
	}

	public override bool readyToClose() => false;

	protected override void cleanupBeforeExit()
	{
		base.cleanupBeforeExit();
		if (IsSpectator)
		{
			return;
		}
		if (Game1.player != null)
		{
			ActiveBattles.Remove(Game1.player.UniqueMultiplayerID);
		}
		PokemonMusicManager.StopAndRestoreStardewMusic();
		if (PlayerTeam != null)
		{
			foreach (var p in PlayerTeam)
			{
				p?.EndBattleGimmicks(revertMega: true);
			}
			PlayerParty.Instance.SaveParty();
		}
		if (EnemyTeam != null)
		{
			foreach (var e in EnemyTeam)
			{
				e?.EndBattleGimmicks(revertMega: true);
			}
		}
	}

	// Trainer Battle Constructor
	public PokemonBattleMenu(string trainerName, List<PokemonInstance> enemyTeam)
		: base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height)
	{
		IsWild = false;
		TrainerName = trainerName;
		EnemyTeam = enemyTeam ?? new List<PokemonInstance>();
		EnemyIndex = 0;

		InitBattle();

		QueueMessage($"O(A) Treinador(a) {TrainerName} desafiou você para uma batalha!", () =>
		{
			_isEnemySending = true;
			_enemySendTimer = 0f;
			QueueMessage($"{TrainerName} enviou {EnemyPokemon?.Nickname}!", () =>
			{
				_isPlayerThrowing = true;
				_playerThrowTimer = 0f;
				QueueMessage($"Vai, {PlayerPokemon?.Nickname}!", () =>
				{
					TriggerSwitchInAbility(PlayerPokemon, EnemyPokemon, () =>
					{
						TriggerSwitchInAbility(EnemyPokemon, PlayerPokemon, () =>
						{
							Phase = BattlePhase.ActionSelect;
						});
					});
				});
			});
		});
	}

	// Wild Pokemon Battle Constructor
	public PokemonBattleMenu(PokemonInstance wildPokemon, bool isWild = true, PokemonEncounterManager.OverworldWildSpawn sourceSpawn = null)
		: base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height)
	{
		IsWild = isWild;
		TrainerName = "Selvagem";
		SourceWildSpawn = sourceSpawn;
		EnemyTeam = new List<PokemonInstance> { wildPokemon };
		EnemyIndex = 0;

		InitBattle();

		QueueMessage($"Um {EnemyPokemon?.Species?.Name} selvagem apareceu!", () =>
		{
			_isPlayerThrowing = true;
			_playerThrowTimer = 0f;
			QueueMessage($"Vai, {PlayerPokemon?.Nickname}!", () =>
			{
				TriggerSwitchInAbility(PlayerPokemon, EnemyPokemon, () =>
				{
					TriggerSwitchInAbility(EnemyPokemon, PlayerPokemon, () =>
					{
						Phase = BattlePhase.ActionSelect;
					});
				});
			});
		});
	}

	// Spectator Mode Constructor
	public PokemonBattleMenu(PokemonBattleMenu targetBattle, bool spectator)
		: base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height)
	{
		IsSpectator = true;
		WatchedBattle = targetBattle;
		BattlerFarmerId = targetBattle?.BattlerFarmerId ?? 0L;
		PlayerTeam = targetBattle?.PlayerTeam ?? new List<PokemonInstance>();
		EnemyTeam = targetBattle?.EnemyTeam ?? new List<PokemonInstance>();
		IsWild = targetBattle?.IsWild ?? false;
		TrainerName = targetBattle?.TrainerName ?? "Batalha";
		if (targetBattle != null)
		{
			_currentTheme = targetBattle._currentTheme;
			CurrentWeather = targetBattle.CurrentWeather;
			WeatherTurns = targetBattle.WeatherTurns;
		}
		_spectatorExitBtn = new ClickableComponent(
			new Rectangle(Game1.uiViewport.Width - 210, 16, 190, 44),
			"spectator_exit",
			"👁️ Sair da Luta (B)");
	}

	private void InitBattle()
	{
		_megaUsedThisBattle = false;
		_zMoveUsedThisBattle = false;
		_dynamaxUsedThisBattle = false;
		_teraUsedThisBattle = false;
		_megaToggled = false;
		_zMoveToggled = false;
		_dynamaxToggled = false;
		_gmaxToggled = false;
		_teraToggled = false;
		_startupTransitionTimer = 0.70f;

		_playerAtkStage = 0;
		_playerDefStage = 0;
		_playerSpAtkStage = 0;
		_playerSpDefStage = 0;
		_playerSpeedStage = 0;
		_enemyAtkStage = 0;
		_enemyDefStage = 0;
		_enemySpAtkStage = 0;
		_enemySpDefStage = 0;
		_enemySpeedStage = 0;

		BattlerFarmerId = Game1.player?.UniqueMultiplayerID ?? 0L;
		PlayerTeam = PlayerParty.GetForFarmer(BattlerFarmerId).Team;

		if (Game1.player != null)
		{
			ActiveBattles[Game1.player.UniqueMultiplayerID] = this;
		}

		if (PlayerTeam != null)
		{
			foreach (var p in PlayerTeam) p?.EndBattleGimmicks(revertMega: true);
		}

		PlayerActiveIndex = PlayerParty.Instance.ActiveIndex;
		if (PlayerActiveIndex < 0 || PlayerActiveIndex >= PlayerTeam.Count || (PlayerTeam[PlayerActiveIndex] != null && PlayerTeam[PlayerActiveIndex].IsFainted))
		{
			int firstAlive = PlayerTeam.FindIndex(p => !p.IsFainted);
			PlayerActiveIndex = firstAlive >= 0 ? firstAlive : 0;
			PlayerParty.Instance.ActiveIndex = PlayerActiveIndex;
		}

		_currentTheme = DetermineArenaTheme();
		_displayPlayerHP = PlayerPokemon?.CurrentHP ?? 100;
		_displayEnemyHP = EnemyPokemon?.CurrentHP ?? 100;

		// Initialize starting battle weather based on location & world weather
		if (Game1.isRaining || (Game1.currentLocation != null && Game1.currentLocation.IsRainingHere()))
		{
			CurrentWeather = BattleWeather.Rain;
			WeatherTurns = 99;
		}
		else if (Game1.currentLocation is MineShaft msIce && msIce.mineLevel >= 40 && msIce.mineLevel <= 79)
		{
			CurrentWeather = BattleWeather.Snow;
			WeatherTurns = 99;
		}
		else if ((Game1.currentLocation is MineShaft msFire && msFire.mineLevel >= 80) || (Game1.currentLocation?.Name?.Contains("Volcano") == true))
		{
			CurrentWeather = BattleWeather.HarshSun;
			WeatherTurns = 99;
		}
		else if (Game1.currentLocation?.Name?.Contains("Desert") == true)
		{
			CurrentWeather = BattleWeather.Sandstorm;
			WeatherTurns = 99;
		}
		else
		{
			CurrentWeather = BattleWeather.None;
			WeatherTurns = 0;
		}

		InitWeatherParticles();

		if (IsWild && EnemyPokemon?.Species != null)
		{
			PokemonPokedex.Instance.RecordSeen(EnemyPokemon.Species.Name);
			PlayerParty.Instance.MarkSeen(EnemyPokemon.Species.Name);
		}

		PokemonMusicManager.PlayBattleMusic(IsWild, TrainerName, EnemyPokemon);

		if (IsWild)
		{
			_enemyPokemonScale = 3.2f;
			_enemyTrainerSlide = 400f;
		}
		else
		{
			_enemyPokemonScale = 0f;
			_enemyTrainerSlide = 0f;
		}

		_playerSpriteScale = 0f;
		_playerTrainerSlide = 0f;
		_playerTrainerFrame = 0;

		EnsureBattleTextures();
		LoadTrainerSprites();
		SetupLayout();
	}

	private static void EnsureBattleTextures()
	{
		try
		{
			if (_treeStrip == null) _treeStrip = Game1.content.Load<Texture2D>("Minigames\\treestrip");
			if (_cloudsTex == null) _cloudsTex = Game1.content.Load<Texture2D>("Minigames\\Clouds");
		}
		catch { }

		try
		{
			if (_ballTex == null)
			{
				string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald", "graphics", "balls", "poke.png");
				if (File.Exists(p1))
				{
					_ballTex = PokemonUI.LoadColorKeyed(p1);
				}
			}
			if (_ballOpenTex == null)
			{
				string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald", "graphics", "balls", "open.png");
				if (File.Exists(p2))
				{
					_ballOpenTex = PokemonUI.LoadColorKeyed(p2);
				}
			}
		}
		catch { }
	}

	private void LoadTrainerSprites()
	{
		try
		{
			_playerTrainerTex = PokemonUI.GetTrainerBackTexture(PokemonUI.TrainerSkin);

			if (!IsWild)
			{
				// 1. Check if TrainerName corresponds to a Valley NPC Portrait (Abigail, Pierre, Shane, Harvey, Morris, etc.)
				if (!string.IsNullOrEmpty(TrainerName))
				{
					string cleanName = TrainerName.Split('(')[0].Trim();

					try
					{
						NPC character = Game1.getCharacterFromName(cleanName);
						if (character != null && character.Portrait != null)
						{
							_enemyTrainerTex = character.Portrait;
						}
					}
					catch { }

					if (_enemyTrainerTex == null)
					{
						try
						{
							_enemyTrainerTex = Game1.content.Load<Texture2D>("Portraits\\" + cleanName);
						}
						catch { }
					}

					// 2. Check if TrainerName is a known Pokémon trainer skin (Lucas, Dawn, Brendan, May, Red, etc.)
					if (_enemyTrainerTex == null && PokemonUI.IsKnownTrainerSkin(cleanName))
					{
						_enemyTrainerTex = PokemonUI.GetTrainerFrontTexture(cleanName);
					}
				}

				if (_enemyTrainerTex == null)
				{
					string cleanName = (TrainerName ?? "").Split('(')[0].Trim().ToLower().Replace(" ", "_");
					string[] enemyCandidates = new[]
					{
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald", "graphics", "trainers", "front_pics", cleanName + ".png"),
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokefirered", "graphics", "trainers", "front_pics", cleanName + "_front_pic.png")
					};

					foreach (var path in enemyCandidates)
					{
						if (File.Exists(path))
						{
							_enemyTrainerTex = PokemonUI.LoadColorKeyed(path);
							if (_enemyTrainerTex != null) break;
						}
					}

					// 3. Last fallback: try any NPC portrait match, otherwise default to Brendan
					if (_enemyTrainerTex == null)
					{
						try
						{
							string cleanNpc = (TrainerName ?? "").Split('(')[0].Trim();
							_enemyTrainerTex = Game1.content.Load<Texture2D>("Portraits\\" + cleanNpc);
						}
						catch { }
					}

					if (_enemyTrainerTex == null)
					{
						_enemyTrainerTex = PokemonUI.GetTrainerFrontTexture("Brendan");
					}
				}
			}
		}
		catch { }
	}

	private ArenaTheme DetermineArenaTheme()
	{
		var loc = Game1.currentLocation;
		if (loc == null) return ArenaTheme.Farm;
		if (loc is StardewValley.Locations.MineShaft || loc.Name.Contains("Cave") || loc.Name.Contains("Mine")) return ArenaTheme.Cave;
		if (loc is StardewValley.Locations.Beach || loc.Name.Contains("Beach") || loc.isWaterTile((int)Game1.player.Tile.X, (int)Game1.player.Tile.Y)) return ArenaTheme.Beach;
		if (loc is StardewValley.Locations.Forest || loc.Name.Contains("Woods") || loc.Name.Contains("Forest")) return ArenaTheme.Forest;
		if (loc is StardewValley.Locations.Town || loc.Name.Contains("Town")) return ArenaTheme.Town;
		if (!loc.IsOutdoors) return ArenaTheme.Interior;
		return ArenaTheme.Farm;
	}

	private void SetupLayout()
	{
		width = Game1.uiViewport.Width;
		height = Game1.uiViewport.Height;
		xPositionOnScreen = 0;
		yPositionOnScreen = 0;

		_boxWidth = width;
		_boxHeight = 180;
		_boxX = 0;
		_boxY = height - _boxHeight;

		// 4 Action Buttons
		_actionButtons.Clear();
		int actionSectionW = Math.Min(560, _boxWidth / 2 - 20);
		int actionStartX = _boxX + _boxWidth - actionSectionW - 24;
		int btnW = (actionSectionW - 16) / 2;
		int btnH = 62;
		_actionButtons.Add(new ClickableComponent(new Rectangle(actionStartX, _boxY + 24, btnW, btnH), "Fight", "1. LUTAR"));
		_actionButtons.Add(new ClickableComponent(new Rectangle(actionStartX + btnW + 16, _boxY + 24, btnW, btnH), "Bag", "2. MOCHILA"));
		_actionButtons.Add(new ClickableComponent(new Rectangle(actionStartX, _boxY + 96, btnW, btnH), "Pokemon", "3. POKÉMON"));
		_actionButtons.Add(new ClickableComponent(new Rectangle(actionStartX + btnW + 16, _boxY + 96, btnW, btnH), "Run", "4. FUGIR"));

		// 4 Move Buttons - With generous height & spacing to completely eliminate text collisions!
		_moveButtons.Clear();
		int moveSectionW = Math.Min(960, _boxWidth - 40);
		int moveStartX = _boxX + (_boxWidth - moveSectionW) / 2;
		int moveW = (moveSectionW - 20) / 2;
		int moveH = 68;
		_moveButtons.Add(new ClickableComponent(new Rectangle(moveStartX, _boxY + 18, moveW, moveH), "Move0"));
		_moveButtons.Add(new ClickableComponent(new Rectangle(moveStartX + moveW + 20, _boxY + 18, moveW, moveH), "Move1"));
		_moveButtons.Add(new ClickableComponent(new Rectangle(moveStartX, _boxY + 94, moveW, moveH), "Move2"));
		_moveButtons.Add(new ClickableComponent(new Rectangle(moveStartX + moveW + 20, _boxY + 94, moveW, moveH), "Move3"));

		_backButton = new ClickableComponent(new Rectangle(_boxX + 24, _boxY - 44, 110, 36), "Back", "Voltar");
		_megaToggleBtn = new ClickableComponent(new Rectangle(_boxX + 144, _boxY - 42, 120, 34), "Mega", "🧬 MEGA");
		_zMoveToggleBtn = new ClickableComponent(new Rectangle(_boxX + 274, _boxY - 42, 125, 34), "ZMove", "💎 Z-MOVE");
		_dynamaxToggleBtn = new ClickableComponent(new Rectangle(_boxX + 409, _boxY - 42, 135, 34), "Dynamax", "🔴 DYNAMAX");
		_gmaxToggleBtn = new ClickableComponent(new Rectangle(_boxX + 554, _boxY - 42, 130, 34), "GMax", "🌟 G-MAX");
		_teraToggleBtn = new ClickableComponent(new Rectangle(_boxX + 694, _boxY - 42, 130, 34), "Tera", "✨ TERA");
	}

	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		SetupLayout();
	}

	public void QueueMessage(string message, Action onComplete = null)
	{
		_messageQueue.Enqueue(message);
		if (onComplete != null)
		{
			_afterMessagesAction = onComplete;
		}
		if (Phase != BattlePhase.MessageWait)
		{
			AdvanceMessageQueue();
		}
	}

	private void AdvanceMessageQueue()
	{
		_messageTimer = 0f;
		if (_messageQueue.Count > 0)
		{
			_currentMessage = _messageQueue.Dequeue();
			Phase = BattlePhase.MessageWait;
		}
		else
		{
			_currentMessage = "";
			Action act = _afterMessagesAction;
			_afterMessagesAction = null;
			try
			{
				act?.Invoke();
			}
			catch (Exception ex)
			{
				// A broken turn must never freeze the battle menu forever.
				Console.WriteLine($"[PokemonBattle] Falha ao continuar o turno: {ex}");
				Phase = BattlePhase.ActionSelect;
			}
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);

		if (IsSpectator)
		{
			if (_spectatorExitBtn != null && _spectatorExitBtn.containsPoint(x, y))
			{
				Game1.playSound("bigDeSelect");
				exitThisMenuNoSound();
			}
			return;
		}

		if (_isCapturing) return;

		if (Phase == BattlePhase.MessageWait)
		{
			Game1.playSound("smallSelect");
			AdvanceMessageQueue();
			return;
		}

		if (Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat)
		{
			exitThisMenuNoSound();
			return;
		}

		if (_backButton.containsPoint(x, y) && (Phase == BattlePhase.MoveSelect || Phase == BattlePhase.BagSelect || Phase == BattlePhase.SwitchSelect))
		{
			if ((PlayerPokemon == null || PlayerPokemon.IsFainted) && Phase == BattlePhase.SwitchSelect)
			{
				Game1.playSound("cancel");
				return;
			}
			Game1.playSound("bigDeSelect");
			Phase = BattlePhase.ActionSelect;
			return;
		}

		if (Phase == BattlePhase.ActionSelect)
		{
			for (int i = 0; i < _actionButtons.Count; i++)
			{
				if (_actionButtons[i].containsPoint(x, y))
				{
					Game1.playSound("smallSelect");
					HandleActionSelection(i);
					return;
				}
			}
		}
		else if (Phase == BattlePhase.MoveSelect)
		{
			if (PlayerPokemon == null) return;

			// Gimmick Toggles (Immediate Mega Evolution / Ultra Burst, Z-Move, Dynamax, Gigantamax, Terastallization)
			if (_megaToggleBtn != null && _megaToggleBtn.containsPoint(x, y))
			{
				bool canUltra = PokemonItemManager.CanUseUltraBurst(Game1.player, PlayerPokemon);
				if (!_megaUsedThisBattle && !PlayerPokemon.IsDynamaxed && (PokemonItemManager.GetAvailableMegaEvolution(Game1.player, PlayerPokemon, out var targetMegaSpecies, out var megaStoneUsed) || canUltra))
				{
					_megaUsedThisBattle = true;
					_megaToggled = false;
					_dynamaxToggled = false;
					_gmaxToggled = false;
					_teraToggled = false;
					string oldName = PlayerPokemon.Nickname;
					if (canUltra)
					{
						PlayerPokemon.IsBattleOnlyMega = true;
						PlayerPokemon.MegaEvolve("necrozma/ultra", "Poke_UltranecroziumZ");
						PlayerParty.Instance.SaveParty();
						_displayPlayerHP = PlayerPokemon.CurrentHP;
						PokemonPokedex.Instance.RecordSeen(PlayerPokemon.Species.Name);
						PlayerParty.Instance.MarkSeen(PlayerPokemon.Species.Name);
						Game1.playSound("wand");
						Game1.playSound("reward");
						QueueMessage($"Necrozma recuperou sua luz original com o Ultranecrozium Z! ULTRA BURST!", () => Phase = BattlePhase.MoveSelect);
					}
					else
					{
						if (!string.IsNullOrEmpty(megaStoneUsed))
						{
							PlayerPokemon.UsedMegaStoneId = megaStoneUsed;
						}
						PlayerPokemon.IsBattleOnlyMega = true;
						PlayerPokemon.MegaEvolve(targetMegaSpecies?.InternalName);
						PlayerParty.Instance.SaveParty();
						_displayPlayerHP = PlayerPokemon.CurrentHP;
						PokemonPokedex.Instance.RecordSeen(PlayerPokemon.Species.Name);
						PlayerParty.Instance.MarkSeen(PlayerPokemon.Species.Name);
						Game1.playSound("wand");
						Game1.playSound("reward");
						QueueMessage($"O Bracelete Mega e a Mega Pedra ressoaram! {oldName} Mega Evoluiu para {PlayerPokemon.Species.Name}!", () => Phase = BattlePhase.MoveSelect);
					}
				}
				else
				{
					Game1.playSound("cancel");
				}
				return;
			}
			if (_zMoveToggleBtn != null && _zMoveToggleBtn.containsPoint(x, y))
			{
				if (!_zMoveUsedThisBattle && !PlayerPokemon.IsDynamaxed && PokemonItemManager.HasAnyMatchingZCrystal(Game1.player, PlayerPokemon))
				{
					_zMoveToggled = !_zMoveToggled;
					if (_zMoveToggled)
					{
						_dynamaxToggled = false;
						_gmaxToggled = false;
						_teraToggled = false;
					}
					Game1.playSound("smallSelect");
				}
				else
				{
					Game1.playSound("cancel");
				}
				return;
			}
			if (_dynamaxToggleBtn != null && _dynamaxToggleBtn.containsPoint(x, y))
			{
				if (!_dynamaxUsedThisBattle && !PlayerPokemon.IsDynamaxed && !PlayerPokemon.IsMegaEvolved && PokemonItemManager.IsWearingDynamaxBand(Game1.player))
				{
					_dynamaxToggled = !_dynamaxToggled;
					if (_dynamaxToggled)
					{
						_gmaxToggled = false;
						_megaToggled = false;
						_zMoveToggled = false;
						_teraToggled = false;
					}
					Game1.playSound("smallSelect");
				}
				else
				{
					Game1.playSound("cancel");
				}
				return;
			}
			if (_gmaxToggleBtn != null && _gmaxToggleBtn.containsPoint(x, y))
			{
				if (!_dynamaxUsedThisBattle && !PlayerPokemon.IsDynamaxed && !PlayerPokemon.IsMegaEvolved && PlayerPokemon.Species != null && PlayerPokemon.Species.HasGigantamax && PokemonItemManager.IsWearingDynamaxBand(Game1.player))
				{
					_gmaxToggled = !_gmaxToggled;
					if (_gmaxToggled)
					{
						_dynamaxToggled = false;
						_megaToggled = false;
						_zMoveToggled = false;
						_teraToggled = false;
					}
					Game1.playSound("smallSelect");
				}
				else
				{
					Game1.playSound("cancel");
				}
				return;
			}
			if (_teraToggleBtn != null && _teraToggleBtn.containsPoint(x, y))
			{
				if (!_teraUsedThisBattle && !PlayerPokemon.IsTerastallized && !PlayerPokemon.IsDynamaxed && PokemonItemManager.CanTerastallize(Game1.player, PlayerPokemon))
				{
					_teraToggled = !_teraToggled;
					if (_teraToggled)
					{
						_dynamaxToggled = false;
						_gmaxToggled = false;
						_zMoveToggled = false;
					}
					Game1.playSound("smallSelect");
				}
				else
				{
					Game1.playSound("cancel");
				}
				return;
			}

			for (int i = 0; i < Math.Min(PlayerPokemon.Moves.Count, 4); i++)
			{
				if (_moveButtons[i].containsPoint(x, y))
				{
					PokemonMove move = PlayerPokemon.Moves[i];
					if (move.CurrentPP > 0)
					{
						Game1.playSound("select");
						ExecuteTurn(move);
					}
					else
					{
						Game1.playSound("cancel");
						QueueMessage("Não há mais PP para este golpe!", () => Phase = BattlePhase.MoveSelect);
					}
					return;
				}
			}
		}
		else if (Phase == BattlePhase.BagSelect)
		{
			if (_bagPrevButton != null && _bagPrevButton.containsPoint(x, y))
			{
				if (_bagPage > 0)
				{
					_bagPage--;
					SetupBagButtons();
					Game1.playSound("shwip");
				}
				return;
			}

			if (_bagNextButton != null && _bagNextButton.containsPoint(x, y))
			{
				_bagPage++;
				SetupBagButtons();
				Game1.playSound("shwip");
				return;
			}

			for (int i = 0; i < _bagButtons.Count; i++)
			{
				if (_bagButtons[i].containsPoint(x, y))
				{
					UseBagItem(_bagButtons[i].name);
					return;
				}
			}
		}
		else if (Phase == BattlePhase.SwitchSelect)
		{
			for (int i = 0; i < _switchButtons.Count; i++)
			{
				if (_switchButtons[i].containsPoint(x, y))
				{
					int idx = Convert.ToInt32(_switchButtons[i].name);
					if (idx >= 0 && idx < PlayerTeam.Count && PlayerTeam[idx].IsFainted)
					{
						Game1.playSound("cancel");
						QueueMessage("Este Pokémon está desmaiado e não pode batalhar!", () => Phase = BattlePhase.SwitchSelect);
						return;
					}
					SwitchPokemon(idx);
					return;
				}
			}
		}
	}

	public override bool areGamePadControlsImplemented() => true;

	public override void receiveGamePadButton(Buttons button)
	{
		base.receiveGamePadButton(button);

		if (IsSpectator)
		{
			if (button is Buttons.A or Buttons.B or Buttons.Start)
			{
				Game1.playSound("bigDeSelect");
				exitThisMenuNoSound();
			}
			return;
		}

		if (_isCapturing) return;

		if (Phase == BattlePhase.MessageWait)
		{
			if (button is Buttons.A or Buttons.B or Buttons.X or Buttons.Y or Buttons.Start)
			{
				Game1.playSound("smallSelect");
				AdvanceMessageQueue();
			}
			return;
		}

		if (Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat)
		{
			if (button is Buttons.A or Buttons.B or Buttons.X or Buttons.Y or Buttons.Start)
			{
				exitThisMenuNoSound();
			}
			return;
		}

		// Button B: Back to ActionSelect
		if (button == Buttons.B && (Phase == BattlePhase.MoveSelect || Phase == BattlePhase.BagSelect || Phase == BattlePhase.SwitchSelect))
		{
			if ((PlayerPokemon == null || PlayerPokemon.IsFainted) && Phase == BattlePhase.SwitchSelect)
			{
				Game1.playSound("cancel");
				return;
			}
			Game1.playSound("bigDeSelect");
			Phase = BattlePhase.ActionSelect;
			return;
		}

		// LB / RB / X / Y: Toggle Gimmick in MoveSelect
		if (Phase == BattlePhase.MoveSelect && (button is Buttons.LeftShoulder or Buttons.RightShoulder or Buttons.X or Buttons.Y))
		{
			ToggleBattleGimmick();
			return;
		}

		switch (Phase)
		{
			case BattlePhase.ActionSelect:
				switch (button)
				{
					case Buttons.DPadLeft:
					case Buttons.LeftThumbstickLeft:
						if (_gamepadActionIndex % 2 == 1) { _gamepadActionIndex--; Game1.playSound("shiny"); }
						break;
					case Buttons.DPadRight:
					case Buttons.LeftThumbstickRight:
						if (_gamepadActionIndex % 2 == 0) { _gamepadActionIndex++; Game1.playSound("shiny"); }
						break;
					case Buttons.DPadUp:
					case Buttons.LeftThumbstickUp:
						if (_gamepadActionIndex >= 2) { _gamepadActionIndex -= 2; Game1.playSound("shiny"); }
						break;
					case Buttons.DPadDown:
					case Buttons.LeftThumbstickDown:
						if (_gamepadActionIndex <= 1) { _gamepadActionIndex += 2; Game1.playSound("shiny"); }
						break;
					case Buttons.A:
					case Buttons.Start:
						Game1.playSound("smallSelect");
						HandleActionSelection(_gamepadActionIndex);
						break;
				}
				break;

			case BattlePhase.MoveSelect:
				if (PlayerPokemon == null) break;
				int moveCount = Math.Min(PlayerPokemon.Moves.Count, 4);
				switch (button)
				{
					case Buttons.DPadLeft:
					case Buttons.LeftThumbstickLeft:
						if (_gamepadMoveIndex % 2 == 1) { _gamepadMoveIndex--; Game1.playSound("shiny"); }
						break;
					case Buttons.DPadRight:
					case Buttons.LeftThumbstickRight:
						if (_gamepadMoveIndex % 2 == 0 && _gamepadMoveIndex + 1 < moveCount) { _gamepadMoveIndex++; Game1.playSound("shiny"); }
						break;
					case Buttons.DPadUp:
					case Buttons.LeftThumbstickUp:
						if (_gamepadMoveIndex >= 2) { _gamepadMoveIndex -= 2; Game1.playSound("shiny"); }
						break;
					case Buttons.DPadDown:
					case Buttons.LeftThumbstickDown:
						if (_gamepadMoveIndex <= 1 && _gamepadMoveIndex + 2 < moveCount) { _gamepadMoveIndex += 2; Game1.playSound("shiny"); }
						break;
					case Buttons.A:
					case Buttons.Start:
						if (_gamepadMoveIndex >= 0 && _gamepadMoveIndex < moveCount)
						{
							PokemonMove move = PlayerPokemon.Moves[_gamepadMoveIndex];
							if (move.CurrentPP > 0)
							{
								Game1.playSound("select");
								ExecuteTurn(move);
							}
							else
							{
								Game1.playSound("cancel");
								QueueMessage("Não há mais PP para este golpe!", () => Phase = BattlePhase.MoveSelect);
							}
						}
						break;
				}
				break;

			case BattlePhase.BagSelect:
				switch (button)
				{
					case Buttons.DPadUp:
					case Buttons.LeftThumbstickUp:
						if (_gamepadBagIndex > 0) { _gamepadBagIndex--; Game1.playSound("shiny"); }
						break;
					case Buttons.DPadDown:
					case Buttons.LeftThumbstickDown:
						if (_gamepadBagIndex + 1 < _bagButtons.Count) { _gamepadBagIndex++; Game1.playSound("shiny"); }
						break;
					case Buttons.DPadLeft:
					case Buttons.LeftThumbstickLeft:
						if (_bagPage > 0) { _bagPage--; SetupBagButtons(); _gamepadBagIndex = 0; Game1.playSound("shwip"); }
						break;
					case Buttons.DPadRight:
					case Buttons.LeftThumbstickRight:
						_bagPage++; SetupBagButtons(); _gamepadBagIndex = 0; Game1.playSound("shwip");
						break;
					case Buttons.A:
					case Buttons.Start:
						if (_gamepadBagIndex >= 0 && _gamepadBagIndex < _bagButtons.Count)
						{
							UseBagItem(_bagButtons[_gamepadBagIndex].name);
						}
						break;
				}
				break;

			case BattlePhase.SwitchSelect:
				switch (button)
				{
					case Buttons.DPadUp:
					case Buttons.LeftThumbstickUp:
						if (_gamepadSwitchIndex > 0) { _gamepadSwitchIndex--; Game1.playSound("shiny"); }
						break;
					case Buttons.DPadDown:
					case Buttons.LeftThumbstickDown:
						if (_gamepadSwitchIndex + 1 < _switchButtons.Count) { _gamepadSwitchIndex++; Game1.playSound("shiny"); }
						break;
					case Buttons.A:
					case Buttons.Start:
						if (_gamepadSwitchIndex >= 0 && _gamepadSwitchIndex < _switchButtons.Count)
						{
							if (int.TryParse(_switchButtons[_gamepadSwitchIndex].name, out int slot))
							{
								if (slot >= 0 && slot < PlayerTeam.Count && PlayerTeam[slot].IsFainted)
								{
									Game1.playSound("cancel");
									QueueMessage("Este Pokémon está desmaiado e não pode batalhar!", () => Phase = BattlePhase.SwitchSelect);
									return;
								}
								SwitchPokemon(slot);
							}
						}
						break;
				}
				break;
		}
	}

	private void ToggleBattleGimmick()
	{
		if (PlayerPokemon == null) return;

		// 1. Terastallize
		if (!_teraUsedThisBattle && !PlayerPokemon.IsTerastallized && !PlayerPokemon.IsDynamaxed && PokemonItemManager.CanTerastallize(Game1.player, PlayerPokemon))
		{
			_teraToggled = !_teraToggled;
			if (_teraToggled) { _dynamaxToggled = false; _gmaxToggled = false; _zMoveToggled = false; _megaToggled = false; }
			Game1.playSound("smallSelect");
			return;
		}

		// 2. Mega Evolution / Ultra Burst
		bool canUltra = PokemonItemManager.CanUseUltraBurst(Game1.player, PlayerPokemon);
		if (!_megaUsedThisBattle && !PlayerPokemon.IsDynamaxed && (PokemonItemManager.GetAvailableMegaEvolution(Game1.player, PlayerPokemon, out _, out _) || canUltra))
		{
			_megaToggled = !_megaToggled;
			if (_megaToggled) { _dynamaxToggled = false; _gmaxToggled = false; _zMoveToggled = false; _teraToggled = false; }
			Game1.playSound("smallSelect");
			return;
		}

		// 3. Dynamax / Gigantamax
		if (!_dynamaxUsedThisBattle && !PlayerPokemon.IsDynamaxed && !PlayerPokemon.IsMegaEvolved && PokemonItemManager.IsWearingDynamaxBand(Game1.player))
		{
			if (PlayerPokemon.Species != null && PlayerPokemon.Species.HasGigantamax)
			{
				_gmaxToggled = !_gmaxToggled;
				if (_gmaxToggled) { _dynamaxToggled = false; _megaToggled = false; _zMoveToggled = false; _teraToggled = false; }
			}
			else
			{
				_dynamaxToggled = !_dynamaxToggled;
				if (_dynamaxToggled) { _gmaxToggled = false; _megaToggled = false; _zMoveToggled = false; _teraToggled = false; }
			}
			Game1.playSound("smallSelect");
			return;
		}

		// 4. Z-Move
		if (!_zMoveUsedThisBattle && !PlayerPokemon.IsDynamaxed && PokemonItemManager.HasAnyMatchingZCrystal(Game1.player, PlayerPokemon))
		{
			_zMoveToggled = !_zMoveToggled;
			if (_zMoveToggled) { _dynamaxToggled = false; _gmaxToggled = false; _megaToggled = false; _teraToggled = false; }
			Game1.playSound("smallSelect");
			return;
		}

		Game1.playSound("cancel");
	}

	public override void receiveKeyPress(Keys key)
	{
		if (Phase == BattlePhase.MessageWait && (key == Keys.Space || key == Keys.Enter || key == Keys.C || key == Keys.X))
		{
			AdvanceMessageQueue();
			return;
		}

		if ((Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat) && (key == Keys.Space || key == Keys.Enter || key == Keys.Escape))
		{
			exitThisMenuNoSound();
			return;
		}

		if (key == Keys.Escape)
		{
			if (Phase == BattlePhase.MoveSelect || Phase == BattlePhase.BagSelect || Phase == BattlePhase.SwitchSelect)
			{
				Phase = BattlePhase.ActionSelect;
				Game1.playSound("bigDeSelect");
			}
			return;
		}

		if (Phase == BattlePhase.ActionSelect)
		{
			if (key == Keys.D1 || key == Keys.NumPad1) HandleActionSelection(0);
			else if (key == Keys.D2 || key == Keys.NumPad2) HandleActionSelection(1);
			else if (key == Keys.D3 || key == Keys.NumPad3) HandleActionSelection(2);
			else if (key == Keys.D4 || key == Keys.NumPad4) HandleActionSelection(3);
		}
		else if (Phase == BattlePhase.MoveSelect)
		{
			if (key == Keys.D1 || key == Keys.NumPad1) TrySelectMove(0);
			else if (key == Keys.D2 || key == Keys.NumPad2) TrySelectMove(1);
			else if (key == Keys.D3 || key == Keys.NumPad3) TrySelectMove(2);
			else if (key == Keys.D4 || key == Keys.NumPad4) TrySelectMove(3);
		}
	}

	private void HandleActionSelection(int index)
	{
		switch (index)
		{
			case 0: // LUTAR
				Phase = BattlePhase.MoveSelect;
				break;
			case 1: // MOCHILA
				SetupBagButtons();
				Phase = BattlePhase.BagSelect;
				break;
			case 2: // POKÉMON
				SetupSwitchButtons();
				Phase = BattlePhase.SwitchSelect;
				break;
			case 3: // FUGIR
				if (IsWild)
				{
					Game1.playSound("run");
					QueueMessage("Você fugiu com sucesso da batalha selvagem!", () =>
					{
						exitThisMenuNoSound();
					});
				}
				else
				{
					Game1.playSound("cancel");
					QueueMessage("Não é possível fugir de uma batalha contra um Treinador!", () =>
					{
						Phase = BattlePhase.ActionSelect;
					});
				}
				break;
		}
	}

	private void TrySelectMove(int moveIndex)
	{
		if (PlayerPokemon == null || moveIndex >= PlayerPokemon.Moves.Count) return;
		PokemonMove move = PlayerPokemon.Moves[moveIndex];
		if (move.CurrentPP > 0)
		{
			Game1.playSound("select");
			ExecuteTurn(move);
		}
		else
		{
			Game1.playSound("cancel");
			QueueMessage("Não há mais PP para este golpe!", () => Phase = BattlePhase.MoveSelect);
		}
	}

	/// <summary>
	/// Priority bracket used for turn order. Z-Moves and Max Moves always resolve at +0,
	/// and Prankster lifts status moves to +1 unless the target is Dark.
	/// </summary>
	private static int GetEffectivePriority(PokemonInstance attacker, PokemonInstance target, PokemonMove move, bool useZMove, bool useMaxMove)
	{
		if (move == null) return 0;
		if (useZMove || useMaxMove) return 0;

		int priority = move.Priority;
		if (priority == 0
			&& attacker != null
			&& move.Category == MoveCategory.Status
			&& string.Equals(attacker.Ability, "Prankster", StringComparison.OrdinalIgnoreCase)
			&& (target == null || !target.HasType(PokemonType.Dark)))
		{
			priority = 1;
		}
		return priority;
	}

	private void ExecuteTurn(PokemonMove playerMove)
	{
		playerMove.CurrentPP--;

		// Flinch only lasts for the current turn: whatever was set last turn is stale now.
		if (PlayerPokemon != null) PlayerPokemon.Flinched = false;
		if (EnemyPokemon != null) EnemyPokemon.Flinched = false;

		bool triggerDynamax = _dynamaxToggled && !_dynamaxUsedThisBattle && !PlayerPokemon.IsMegaEvolved && !PlayerPokemon.IsDynamaxed
			&& PokemonItemManager.IsWearingDynamaxBand(Game1.player);
		bool triggerGmax = _gmaxToggled && !_dynamaxUsedThisBattle && !PlayerPokemon.IsMegaEvolved && !PlayerPokemon.IsDynamaxed
			&& PlayerPokemon.Species != null && PlayerPokemon.Species.HasGigantamax && PokemonItemManager.IsWearingDynamaxBand(Game1.player);
		bool hasSigZ = PokemonItemManager.CanUseSignatureZMove(Game1.player, PlayerPokemon, playerMove, out _, out _, out _);
		bool useZMove = _zMoveToggled && !_zMoveUsedThisBattle && !PlayerPokemon.IsDynamaxed && !triggerDynamax && !triggerGmax
			&& (hasSigZ || PokemonItemManager.HasZCrystalForType(Game1.player, playerMove.Type, PlayerPokemon));
		bool triggerTera = _teraToggled && !_teraUsedThisBattle && !PlayerPokemon.IsTerastallized && !PlayerPokemon.IsDynamaxed && !triggerDynamax && !triggerGmax
			&& PokemonItemManager.CanTerastallize(Game1.player, PlayerPokemon);

		_megaToggled = false;
		_zMoveToggled = false;
		_dynamaxToggled = false;
		_gmaxToggled = false;
		_teraToggled = false;

		Action checkRampageLoop = () =>
		{
			if (PlayerPokemon != null && PlayerPokemon.RampageTurns > 0 && !PlayerPokemon.IsFainted && !string.IsNullOrEmpty(PlayerPokemon.RampageMoveName))
			{
				QueueMessage($"{PlayerPokemon.Nickname} continua enfurecido com {PlayerPokemon.RampageMoveName}!", () =>
				{
					PokemonMove rMove = PlayerPokemon.Moves.Find(m => m.Name.Equals(PlayerPokemon.RampageMoveName, StringComparison.OrdinalIgnoreCase)) ?? PokemonMove.Get(PlayerPokemon.RampageMoveName);
					ExecuteTurn(rMove);
				});
			}
			else
			{
				Phase = BattlePhase.ActionSelect;
			}
		};

		Action runTurnCombat = () =>
		{
			if (useZMove) _zMoveUsedThisBattle = true;
			bool useMaxMove = PlayerPokemon.IsDynamaxed;

			// Pick Enemy Move (or continue Rampage if locked)
			PokemonMove enemyMove = SelectEnemyMove();

			int playerEffSpeed = (int)(EffectiveSpeedOf(PlayerPokemon) * GetStageMultiplier(_playerSpeedStage));
			int enemyEffSpeed = (int)(EffectiveSpeedOf(EnemyPokemon) * GetStageMultiplier(_enemySpeedStage));

			// Turn order: priority bracket first, then Speed, coin flip on a tie.
			int playerPriority = GetEffectivePriority(PlayerPokemon, EnemyPokemon, playerMove, useZMove, useMaxMove);
			int enemyPriority = GetEffectivePriority(EnemyPokemon, PlayerPokemon, enemyMove, false, EnemyPokemon.IsDynamaxed);

			bool playerFirst;
			if (playerPriority != enemyPriority)
			{
				playerFirst = playerPriority > enemyPriority;
			}
			else if (playerEffSpeed != enemyEffSpeed)
			{
				playerFirst = playerEffSpeed > enemyEffSpeed;
			}
			else
			{
				playerFirst = Game1.random.Next(2) == 0;
			}

			if (playerFirst)
			{
				ExecuteAttack(PlayerPokemon, EnemyPokemon, playerMove, () =>
				{
					if (EnemyPokemon.IsFainted)
					{
						HandleEnemyFainted();
					}
					else
					{
						ExecuteAttack(EnemyPokemon, PlayerPokemon, enemyMove, () =>
						{
							if (PlayerPokemon.IsFainted)
							{
								HandlePlayerFainted();
							}
							else
							{
								RunEndOfTurnEffects(checkRampageLoop);
							}
						});
					}
				}, useZMove, useMaxMove);
			}
			else
			{
				ExecuteAttack(EnemyPokemon, PlayerPokemon, enemyMove, () =>
				{
					if (PlayerPokemon.IsFainted)
					{
						HandlePlayerFainted();
					}
					else
					{
						ExecuteAttack(PlayerPokemon, EnemyPokemon, playerMove, () =>
						{
							if (EnemyPokemon.IsFainted)
							{
								HandleEnemyFainted();
							}
							else
							{
								RunEndOfTurnEffects(checkRampageLoop);
							}
						}, useZMove, useMaxMove);
					}
				});
			}
		};

		if (triggerDynamax)
		{
			_dynamaxUsedThisBattle = true;
			PlayerPokemon.ActivateDynamax(asGigantamax: false);
			_displayPlayerHP = PlayerPokemon.CurrentHP;
			Game1.playSound("thunder");
			Game1.playSound("reward");
			QueueMessage($"{PlayerPokemon.Nickname} absorveu energia Dinamax e cresceu gigantescamente! Seu HP dobrou!", runTurnCombat);
		}
		else if (triggerGmax)
		{
			_dynamaxUsedThisBattle = true;
			PlayerPokemon.ActivateDynamax(asGigantamax: true);
			_displayPlayerHP = PlayerPokemon.CurrentHP;
			Game1.playSound("thunder");
			Game1.playSound("reward");
			QueueMessage($"{PlayerPokemon.Nickname} alcançou a magnífica forma GIGANTAMAX! Seu HP dobrou!", runTurnCombat);
		}
		else if (triggerTera)
		{
			_teraUsedThisBattle = true;
			PlayerPokemon.Terastallize();
			Game1.playSound("wand");
			Game1.playSound("reward");
			QueueMessage($"O Tera Orb emanou sua jóia cristalina! {PlayerPokemon.Nickname} Terastalizou para o Tipo {PlayerPokemon.TeraType}!", runTurnCombat);
		}
		else
		{
			runTurnCombat();
		}
	}

	private void RunEndOfTurnEffects(Action onDone)
	{
		Action checkEnemyHeldItems = () =>
		{
			if (EnemyPokemon != null && !EnemyPokemon.IsFainted)
			{
				string held = EnemyPokemon.HeldItem ?? "";
				if ((string.Equals(held, "Poke_Leftovers", StringComparison.OrdinalIgnoreCase) || string.Equals(held, "Leftovers", StringComparison.OrdinalIgnoreCase)) && EnemyPokemon.CurrentHP < EnemyPokemon.MaxHP)
				{
					int heal = Math.Max(1, EnemyPokemon.MaxHP / 16);
					EnemyPokemon.Heal(heal);
					_displayEnemyHP = EnemyPokemon.CurrentHP;
					Game1.playSound("heal");
					QueueMessage($"Os Restos (Leftovers) restauraram {heal} de HP de {EnemyPokemon.Nickname}!", onDone);
					return;
				}
				if (string.Equals(held, "Poke_BlackSludge", StringComparison.OrdinalIgnoreCase) || string.Equals(held, "BlackSludge", StringComparison.OrdinalIgnoreCase))
				{
					bool isPoisonType = EnemyPokemon.Species != null && (EnemyPokemon.Species.PrimaryType == PokemonType.Poison || EnemyPokemon.Species.SecondaryType == PokemonType.Poison);
					if (isPoisonType && EnemyPokemon.CurrentHP < EnemyPokemon.MaxHP)
					{
						int heal = Math.Max(1, EnemyPokemon.MaxHP / 16);
						EnemyPokemon.Heal(heal);
						_displayEnemyHP = EnemyPokemon.CurrentHP;
						Game1.playSound("heal");
						QueueMessage($"O Lodo Negro restaurou {heal} de HP de {EnemyPokemon.Nickname}!", onDone);
						return;
					}
					else if (!isPoisonType)
					{
						int dmg = Math.Max(1, EnemyPokemon.MaxHP / 8);
						EnemyPokemon.TakeDamage(dmg);
						_displayEnemyHP = EnemyPokemon.CurrentHP;
						Game1.playSound("ow");
						QueueMessage($"O Lodo Negro feriu {EnemyPokemon.Nickname} (-{dmg} HP)!", () =>
						{
							if (EnemyPokemon.IsFainted) HandleEnemyFainted();
							else onDone?.Invoke();
						});
						return;
					}
				}
				if ((string.Equals(held, "Poke_FlameOrb", StringComparison.OrdinalIgnoreCase) || string.Equals(held, "FlameOrb", StringComparison.OrdinalIgnoreCase)) && EnemyPokemon.Status == PokemonStatusCondition.None)
				{
					if (CanAfflictStatus(EnemyPokemon, PokemonStatusCondition.Burn, null, out _))
					{
						EnemyPokemon.Status = PokemonStatusCondition.Burn;
						Game1.playSound("fireball");
						QueueMessage($"A Flame Orb causou queimaduras em {EnemyPokemon.Nickname}!", onDone);
						return;
					}
				}
				if ((string.Equals(held, "Poke_ToxicOrb", StringComparison.OrdinalIgnoreCase) || string.Equals(held, "ToxicOrb", StringComparison.OrdinalIgnoreCase)) && EnemyPokemon.Status == PokemonStatusCondition.None)
				{
					if (CanAfflictStatus(EnemyPokemon, PokemonStatusCondition.Toxic, null, out _))
					{
						EnemyPokemon.Status = PokemonStatusCondition.Toxic;
						Game1.playSound("slosh");
						QueueMessage($"A Toxic Orb envenenou gravemente {EnemyPokemon.Nickname}!", onDone);
						return;
					}
				}
			}
			onDone?.Invoke();
		};

		Action checkEnemyStatus = () =>
		{
			if (EnemyPokemon != null && !EnemyPokemon.IsFainted)
			{
				bool magicGuard = string.Equals(EnemyPokemon.Ability, "MagicGuard", StringComparison.OrdinalIgnoreCase);
				if (EnemyPokemon.SaltCureTurns > 0 && !magicGuard)
				{
					bool waterOrSteel = EnemyPokemon.Species != null
						&& (EnemyPokemon.Species.PrimaryType == PokemonType.Water || EnemyPokemon.Species.SecondaryType == PokemonType.Water
							|| EnemyPokemon.Species.PrimaryType == PokemonType.Steel || EnemyPokemon.Species.SecondaryType == PokemonType.Steel);
					int saltDmg = Math.Max(1, EnemyPokemon.MaxHP / (waterOrSteel ? 4 : 8));
					EnemyPokemon.SaltCureTurns--;
					EnemyPokemon.TakeDamage(saltDmg);
					_enemyDamageShake = 0.6f;
					Game1.playSound("dwoop");
					QueueMessage($"{EnemyPokemon.Nickname} sofreu {saltDmg} de dano pelo Sal Cure!", () =>
					{
						if (EnemyPokemon.IsFainted) HandleEnemyFainted();
						else checkEnemyHeldItems();
					});
					return;
				}
				if (EnemyPokemon.Status == PokemonStatusCondition.Burn && !magicGuard)
				{
					int dmg = Math.Max(1, EnemyPokemon.MaxHP / 16);
					EnemyPokemon.TakeDamage(dmg);
					_enemyDamageShake = 0.6f;
					Game1.playSound("fireball");
					QueueMessage($"{EnemyPokemon.Nickname} sofreu {dmg} de dano pela queimadura!", () =>
					{
						if (EnemyPokemon.IsFainted) HandleEnemyFainted();
						else checkEnemyHeldItems();
					});
					return;
				}
				if (EnemyPokemon.Status == PokemonStatusCondition.Poison || EnemyPokemon.Status == PokemonStatusCondition.Toxic)
				{
					if (string.Equals(EnemyPokemon.Ability, "PoisonHeal", StringComparison.OrdinalIgnoreCase))
					{
						int heal = Math.Max(1, EnemyPokemon.MaxHP / 8);
						EnemyPokemon.Heal(heal);
						_displayEnemyHP = EnemyPokemon.CurrentHP;
						Game1.playSound("heal");
						QueueMessage($"A habilidade Poison Heal curou {EnemyPokemon.Nickname} (+{heal} HP)!", checkEnemyHeldItems);
						return;
					}

					if (!magicGuard)
					{
						if (EnemyPokemon.Status == PokemonStatusCondition.Toxic) EnemyPokemon.StatusTurns = Math.Min(15, EnemyPokemon.StatusTurns + 1);
						int dmg = EnemyPokemon.Status == PokemonStatusCondition.Toxic
							? Math.Max(1, EnemyPokemon.MaxHP * Math.Max(1, EnemyPokemon.StatusTurns) / 16)
							: Math.Max(1, EnemyPokemon.MaxHP / 8);
						EnemyPokemon.TakeDamage(dmg);
						_enemyDamageShake = 0.6f;
						Game1.playSound("slosh");
						QueueMessage($"{EnemyPokemon.Nickname} sofreu {dmg} de dano pelo veneno!", () =>
						{
							if (EnemyPokemon.IsFainted) HandleEnemyFainted();
							else checkEnemyHeldItems();
						});
						return;
					}
				}
			}
			checkEnemyHeldItems();
		};

		Action checkPlayerHeldItems = () =>
		{
			if (PlayerPokemon != null && !PlayerPokemon.IsFainted)
			{
				if (PokemonHeldItemManager.Matches(PlayerPokemon.HeldItem, "Leftovers") && PlayerPokemon.CurrentHP < PlayerPokemon.MaxHP)
				{
					int heal = Math.Max(1, PlayerPokemon.MaxHP / 16);
					PlayerPokemon.Heal(heal);
					_displayPlayerHP = PlayerPokemon.CurrentHP;
					Game1.playSound("heal");
					QueueMessage($"Os Restos (Leftovers) restauraram {heal} de HP de {PlayerPokemon.Nickname}!", checkEnemyStatus);
					return;
				}
				if (PokemonHeldItemManager.Matches(PlayerPokemon.HeldItem, "BlackSludge"))
				{
					bool isPoisonType = PlayerPokemon.Species != null && (PlayerPokemon.Species.PrimaryType == PokemonType.Poison || PlayerPokemon.Species.SecondaryType == PokemonType.Poison);
					if (isPoisonType && PlayerPokemon.CurrentHP < PlayerPokemon.MaxHP)
					{
						int heal = Math.Max(1, PlayerPokemon.MaxHP / 16);
						PlayerPokemon.Heal(heal);
						_displayPlayerHP = PlayerPokemon.CurrentHP;
						Game1.playSound("heal");
						QueueMessage($"O Lodo Negro restaurou {heal} de HP de {PlayerPokemon.Nickname}!", checkEnemyStatus);
						return;
					}
					else if (!isPoisonType)
					{
						int dmg = Math.Max(1, PlayerPokemon.MaxHP / 8);
						PlayerPokemon.TakeDamage(dmg);
						_displayPlayerHP = PlayerPokemon.CurrentHP;
						Game1.playSound("ow");
						QueueMessage($"O Lodo Negro feriu {PlayerPokemon.Nickname} (-{dmg} HP)!", () =>
						{
							if (PlayerPokemon.IsFainted) HandlePlayerFainted();
							else checkEnemyStatus();
						});
						return;
					}
				}

				string pHeld = PlayerPokemon.HeldItem ?? "";
				if (PokemonHeldItemManager.Matches(pHeld, "FlameOrb") && PlayerPokemon.Status == PokemonStatusCondition.None)
				{
					if (CanAfflictStatus(PlayerPokemon, PokemonStatusCondition.Burn, null, out _))
					{
						PlayerPokemon.Status = PokemonStatusCondition.Burn;
						Game1.playSound("fireball");
						QueueMessage($"A Flame Orb causou queimaduras em {PlayerPokemon.Nickname}!", checkEnemyStatus);
						return;
					}
				}
				if (PokemonHeldItemManager.Matches(pHeld, "ToxicOrb") && PlayerPokemon.Status == PokemonStatusCondition.None)
				{
					if (CanAfflictStatus(PlayerPokemon, PokemonStatusCondition.Toxic, null, out _))
					{
						PlayerPokemon.Status = PokemonStatusCondition.Toxic;
						Game1.playSound("slosh");
						QueueMessage($"A Toxic Orb envenenou gravemente {PlayerPokemon.Nickname}!", checkEnemyStatus);
						return;
					}
				}

				if (string.Equals(PlayerPokemon.Ability, "SpeedBoost", StringComparison.OrdinalIgnoreCase))
				{
					QueueMessage($"A habilidade Speed Boost de {PlayerPokemon.Nickname} aumentou sua Velocidade!", checkEnemyStatus);
					return;
				}
			}
			checkEnemyStatus();
		};

		Action checkDynamaxTick = () =>
		{
			if (PlayerPokemon != null && !PlayerPokemon.IsFainted && PlayerPokemon.IsDynamaxed)
			{
				PlayerPokemon.TickDynamax();
				_displayPlayerHP = PlayerPokemon.CurrentHP;
				if (!PlayerPokemon.IsDynamaxed)
				{
					QueueMessage($"A energia Dynamax de {PlayerPokemon.Nickname} se dissipou!", checkPlayerHeldItems);
					return;
				}
			}
			checkPlayerHeldItems();
		};

		if (PlayerPokemon != null && !PlayerPokemon.IsFainted)
		{
			bool magicGuard = string.Equals(PlayerPokemon.Ability, "MagicGuard", StringComparison.OrdinalIgnoreCase);
			if (PlayerPokemon.SaltCureTurns > 0 && !magicGuard)
			{
				bool waterOrSteel = PlayerPokemon.Species != null
					&& (PlayerPokemon.Species.PrimaryType == PokemonType.Water || PlayerPokemon.Species.SecondaryType == PokemonType.Water
						|| PlayerPokemon.Species.PrimaryType == PokemonType.Steel || PlayerPokemon.Species.SecondaryType == PokemonType.Steel);
				int saltDmg = Math.Max(1, PlayerPokemon.MaxHP / (waterOrSteel ? 4 : 8));
				PlayerPokemon.SaltCureTurns--;
				PlayerPokemon.TakeDamage(saltDmg);
				_playerDamageShake = 0.6f;
				Game1.playSound("dwoop");
				QueueMessage($"{PlayerPokemon.Nickname} sofreu {saltDmg} de dano pelo Sal Cure!", () =>
				{
					if (PlayerPokemon.IsFainted) HandlePlayerFainted();
					else checkDynamaxTick();
				});
				return;
			}
			if (PlayerPokemon.Status == PokemonStatusCondition.Burn && !magicGuard)
			{
				int dmg = Math.Max(1, PlayerPokemon.MaxHP / 16);
				PlayerPokemon.TakeDamage(dmg);
				_playerDamageShake = 0.6f;
				Game1.playSound("fireball");
				QueueMessage($"{PlayerPokemon.Nickname} sofreu {dmg} de dano pela queimadura!", () =>
				{
					if (PlayerPokemon.IsFainted) HandlePlayerFainted();
					else checkDynamaxTick();
				});
				return;
			}
			if (PlayerPokemon.Status == PokemonStatusCondition.Poison || PlayerPokemon.Status == PokemonStatusCondition.Toxic)
			{
				if (string.Equals(PlayerPokemon.Ability, "PoisonHeal", StringComparison.OrdinalIgnoreCase))
				{
					int heal = Math.Max(1, PlayerPokemon.MaxHP / 8);
					PlayerPokemon.Heal(heal);
					_displayPlayerHP = PlayerPokemon.CurrentHP;
					Game1.playSound("heal");
					QueueMessage($"A habilidade Poison Heal curou {PlayerPokemon.Nickname} (+{heal} HP)!", checkDynamaxTick);
					return;
				}

				if (!magicGuard)
				{
					if (PlayerPokemon.Status == PokemonStatusCondition.Toxic) PlayerPokemon.StatusTurns = Math.Min(15, PlayerPokemon.StatusTurns + 1);
					int dmg = PlayerPokemon.Status == PokemonStatusCondition.Toxic
						? Math.Max(1, PlayerPokemon.MaxHP * Math.Max(1, PlayerPokemon.StatusTurns) / 16)
						: Math.Max(1, PlayerPokemon.MaxHP / 8);
					PlayerPokemon.TakeDamage(dmg);
					_playerDamageShake = 0.6f;
					Game1.playSound("slosh");
					QueueMessage($"{PlayerPokemon.Nickname} sofreu {dmg} de dano pelo veneno!", () =>
					{
						if (PlayerPokemon.IsFainted) HandlePlayerFainted();
						else checkDynamaxTick();
					});
					return;
				}
			}
		}

		checkDynamaxTick();
	}

	public static bool CanAfflictStatus(PokemonInstance target, PokemonStatusCondition status, PokemonInstance attacker, out string failReason)
	{
		failReason = null;
		if (target == null || target.IsFainted) return false;

		if (target.Status != PokemonStatusCondition.None)
		{
			failReason = $"Mas {target.Nickname} já possui uma condição de status!";
			return false;
		}

		// Type Immunities
		if (status == PokemonStatusCondition.Burn && target.HasType(PokemonType.Fire))
		{
			failReason = $"Mas Pokémon do tipo Fogo não podem sofrer queimaduras!";
			return false;
		}
		if (status == PokemonStatusCondition.Freeze && target.HasType(PokemonType.Ice))
		{
			failReason = $"Mas Pokémon do tipo Gelo não podem ser congelados!";
			return false;
		}
		if (status == PokemonStatusCondition.Paralysis && target.HasType(PokemonType.Electric))
		{
			failReason = $"Mas Pokémon do tipo Elétrico não podem ser paralisados!";
			return false;
		}
		if ((status == PokemonStatusCondition.Poison || status == PokemonStatusCondition.Toxic) && (target.HasType(PokemonType.Poison) || target.HasType(PokemonType.Steel)))
		{
			bool hasCorrosion = attacker != null && string.Equals(attacker.Ability, "Corrosion", StringComparison.OrdinalIgnoreCase);
			if (!hasCorrosion)
			{
				failReason = $"Mas Pokémon dos tipos Venenoso ou Aço não podem ser envenenados!";
				return false;
			}
		}

		// Ability Immunities
		string ab = target.Ability ?? "";
		if (status == PokemonStatusCondition.Paralysis && string.Equals(ab, "Limber", StringComparison.OrdinalIgnoreCase))
		{
			failReason = $"A habilidade de {target.Nickname} impede a paralisia!";
			return false;
		}
		if (status == PokemonStatusCondition.Burn && (string.Equals(ab, "WaterVeil", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "WaterBubble", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "Water Veil", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "Water Bubble", StringComparison.OrdinalIgnoreCase)))
		{
			failReason = $"A habilidade de {target.Nickname} previne queimaduras!";
			return false;
		}
		if ((status == PokemonStatusCondition.Poison || status == PokemonStatusCondition.Toxic) && (string.Equals(ab, "Immunity", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "PastelVeil", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "Pastel Veil", StringComparison.OrdinalIgnoreCase)))
		{
			failReason = $"A habilidade de {target.Nickname} confere imunidade a veneno!";
			return false;
		}
		if (status == PokemonStatusCondition.Freeze && (string.Equals(ab, "MagmaArmor", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "Magma Armor", StringComparison.OrdinalIgnoreCase)))
		{
			failReason = $"A armadura magmática de {target.Nickname} impede o congelamento!";
			return false;
		}
		if (status == PokemonStatusCondition.Sleep && (string.Equals(ab, "Insomnia", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "VitalSpirit", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "Vital Spirit", StringComparison.OrdinalIgnoreCase)))
		{
			failReason = $"A habilidade de {target.Nickname} o impede de adormecer!";
			return false;
		}
		if (string.Equals(ab, "Comatose", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "PurifyingSalt", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "Purifying Salt", StringComparison.OrdinalIgnoreCase))
		{
			failReason = $"A habilidade especial de {target.Nickname} bloqueia alterações de status!";
			return false;
		}

		return true;
	}

	private void ExecuteAttack(PokemonInstance attacker, PokemonInstance defender, PokemonMove move, Action onComplete, bool useZMove = false, bool useMaxMove = false)
	{
		bool isPlayerAttacking = (attacker == PlayerPokemon);

		if (attacker.Flinched)
		{
			attacker.Flinched = false;
			Game1.playSound("cancel");
			QueueMessage($"{attacker.Nickname} hesitou e não conseguiu se mover!", onComplete);
			return;
		}

		Action performMove = () =>
		{
			bool hasSigZ = false;
			string sigZName = null;
			int sigZPower = 0;
			PokemonType sigZType = PokemonType.Normal;

			if (useZMove && isPlayerAttacking && PokemonItemManager.CanUseSignatureZMove(Game1.player, attacker, move, out sigZName, out sigZPower, out sigZType))
			{
				hasSigZ = true;
			}

			string displayMoveName = hasSigZ
				? sigZName
				: (useZMove
					? move.GetZMoveName()
					: (useMaxMove ? move.GetMaxMoveName(attacker.IsGigantamaxed, attacker.Species?.InternalName) : move.Name));
			int effectivePower = hasSigZ
				? sigZPower
				: (useZMove
					? move.GetZMovePower()
					: (useMaxMove ? move.GetMaxMovePower() : move.Power));

			PokemonType moveType = hasSigZ ? sigZType : move.Type;
			MoveCategory moveCat = move.Category;

			// Adaptability for Tera Blast
			if (string.Equals(move.Name, "Tera Blast", StringComparison.OrdinalIgnoreCase) && attacker.IsTerastallized)
			{
				moveType = attacker.TeraType;
				if (attacker.Attack > attacker.SpAttack)
				{
					moveCat = MoveCategory.Physical;
				}
			}

			// Type conversion abilities (Pixilate, Aerilate, Refrigerate, Galvanize)
			if (moveType == PokemonType.Normal && !useZMove && !useMaxMove)
			{
				if (string.Equals(attacker.Ability, "Pixilate", StringComparison.OrdinalIgnoreCase)) { moveType = PokemonType.Fairy; effectivePower = (int)(effectivePower * 1.2f); }
				else if (string.Equals(attacker.Ability, "Aerilate", StringComparison.OrdinalIgnoreCase)) { moveType = PokemonType.Flying; effectivePower = (int)(effectivePower * 1.2f); }
				else if (string.Equals(attacker.Ability, "Refrigerate", StringComparison.OrdinalIgnoreCase)) { moveType = PokemonType.Ice; effectivePower = (int)(effectivePower * 1.2f); }
				else if (string.Equals(attacker.Ability, "Galvanize", StringComparison.OrdinalIgnoreCase)) { moveType = PokemonType.Electric; effectivePower = (int)(effectivePower * 1.2f); }
			}

			// Sheer Force (+30% power if move has secondary effect)
			bool sheerForceActive = string.Equals(attacker.Ability, "SheerForce", StringComparison.OrdinalIgnoreCase);
			if (sheerForceActive && (move.TargetStages.Count > 0 || move.StatusEffect != PokemonStatusCondition.None || move.FlinchChance > 0))
			{
				effectivePower = (int)(effectivePower * 1.3f);
			}

			// Terastallization: 60 Base Power Floor (Gen 9 mechanic)
			if (attacker.IsTerastallized && moveType == attacker.TeraType && effectivePower > 0 && effectivePower < 60 && !useZMove && !useMaxMove)
			{
				effectivePower = 60;
			}

			QueueMessage($"{attacker.Nickname} usou {displayMoveName}!", () =>
			{
				// Check Weather moves
				if (move.Name.Equals("Rain Dance", StringComparison.OrdinalIgnoreCase))
				{
					PokemonHUD.SetWorldWeather(true);
					QueueMessage("A chuva começou a cair torrencialmente no vale!", onComplete);
					return;
				}
				if (move.Name.Equals("Sunny Day", StringComparison.OrdinalIgnoreCase))
				{
					PokemonHUD.SetWorldWeather(false);
					QueueMessage("O sol voltou a brilhar intensamente no vale!", onComplete);
					return;
				}

				// Accuracy check (Z-Moves and Max Moves never miss)
				if (!useZMove && !useMaxMove && Game1.random.Next(1, 101) > move.Accuracy)
				{
					Game1.playSound("miss");
					QueueMessage("Mas o ataque errou o alvo!", onComplete);
					return;
				}

				// Pure Status / Utility moves (when not converted to Z-Move or Max Move)
				if (!useZMove && !useMaxMove && (move.Category == MoveCategory.Status || effectivePower <= 0))
				{
					if (move.SelfHealRatio > 0f)
					{
						int healAmt = Math.Max(1, (int)(attacker.MaxHP * move.SelfHealRatio));
						attacker.Heal(healAmt);
						if (move.Name.Equals("Rest", StringComparison.OrdinalIgnoreCase))
						{
							if (CanAfflictStatus(attacker, PokemonStatusCondition.Sleep, null, out string restFail))
							{
								attacker.Status = PokemonStatusCondition.Sleep;
								attacker.StatusTurns = 2;
							}
							else
							{
								QueueMessage(restFail ?? $"{attacker.Nickname} não conseguiu adormecer!", onComplete);
								return;
							}
						}
						Game1.playSound("heal");
						QueueMessage($"{attacker.Nickname} recuperou {healAmt} de HP!", () => ApplyStatChanges(defender, move.TargetStages, onComplete));
						return;
					}

					if (move.StatusEffect != PokemonStatusCondition.None)
					{
						if (CanAfflictStatus(defender, move.StatusEffect, attacker, out string failReason))
						{
							defender.Status = move.StatusEffect;
							if (move.StatusEffect == PokemonStatusCondition.Sleep) defender.StatusTurns = Game1.random.Next(2, 4);
							if (move.StatusEffect == PokemonStatusCondition.Toxic) defender.StatusTurns = 1;
							Game1.playSound("debuffHit");
							string stMsg = move.StatusEffect switch
							{
								PokemonStatusCondition.Paralysis => $"{defender.Nickname} foi paralisado e perdeu velocidade!",
								PokemonStatusCondition.Burn => $"{defender.Nickname} sofreu queimaduras!",
								PokemonStatusCondition.Sleep => $"{defender.Nickname} adormeceu profundamente!",
								PokemonStatusCondition.Freeze => $"{defender.Nickname} foi congelado solidamente!",
								PokemonStatusCondition.Poison => $"{defender.Nickname} foi envenenado!",
								PokemonStatusCondition.Toxic => $"{defender.Nickname} foi gravemente envenenado!",
								_ => $"{defender.Nickname} sofreu alteração de status!"
							};
							QueueMessage(stMsg, onComplete);
						}
						else
						{
							QueueMessage(failReason ?? $"Mas não teve efeito em {defender.Nickname}!", onComplete);
						}
						return;
					}

					if (move.InflictsConfusion)
					{
						defender.ConfusionTurns = Game1.random.Next(2, 5);
						Game1.playSound("debuffHit");
						QueueMessage($"{defender.Nickname} ficou confuso!", onComplete);
						return;
					}

					if (move.HpCostRatio > 0f)
					{
						int cost = Math.Max(1, (int)(attacker.MaxHP * move.HpCostRatio));
						cost = Math.Min(cost, Math.Max(0, attacker.CurrentHP - 1));
						attacker.TakeDamage(cost);
						Game1.playSound("ow");
						QueueMessage($"{attacker.Nickname} sacrificou {cost} de HP!", () =>
						{
							ApplyStatChanges(attacker, move.SelfStages, () => ApplyStatChanges(defender, move.TargetStages, onComplete));
						});
						return;
					}

					if (move.SelfStages.Count > 0 || move.TargetStages.Count > 0)
					{
						ApplyStatChanges(attacker, move.SelfStages, () => ApplyStatChanges(defender, move.TargetStages, onComplete));
						return;
					}

					if (move.RandomSelfBoost)
					{
						PokemonStat[] stats = { PokemonStat.Attack, PokemonStat.Defense, PokemonStat.SpAttack, PokemonStat.SpDefense, PokemonStat.Speed };
						var change = new StatStageChange(stats[Game1.random.Next(stats.Length)], 2);
						ApplyStatChanges(attacker, new List<StatStageChange> { change }, onComplete);
						return;
					}

					if (move.InvertsTargetStages)
					{
						InvertStages(defender);
						Game1.playSound("debuffHit");
						QueueMessage($"Os atributos de {defender.Nickname} foram invertidos!", onComplete);
						return;
					}

					Game1.playSound("heal");
					QueueMessage("Mas nada aconteceu!", onComplete);
					return;
				}

				// Gen 5+ Damage calculation (Physical/Special split, STAB, Burn penalty, Weather, Crit)
				bool physicalStats = moveCat == MoveCategory.Physical || move.UsesPhysicalStats;
				float attackStat = physicalStats ? attacker.Attack : attacker.SpAttack;
				float defStat = physicalStats ? defender.Defense : defender.SpDefense;

				int atkStage = isPlayerAttacking ? (physicalStats ? _playerAtkStage : _playerSpAtkStage) : (physicalStats ? _enemyAtkStage : _enemySpAtkStage);
				int defStage = isPlayerAttacking ? (physicalStats ? _enemyDefStage : _enemySpDefStage) : (physicalStats ? _playerDefStage : _playerSpDefStage);

				attackStat *= GetStageMultiplier(atkStage);
				defStat *= GetStageMultiplier(defStage);

				// STAB calculation with Terastallization
				bool isBaseType = (attacker.Species != null && (moveType == attacker.Species.PrimaryType || (attacker.Species.SecondaryType.HasValue && moveType == attacker.Species.SecondaryType.Value)));
				float stab = 1.0f;
				if (attacker.IsTerastallized)
				{
					if (moveType == attacker.TeraType)
					{
						if (isBaseType)
							stab = string.Equals(attacker.Ability, "Adaptability", StringComparison.OrdinalIgnoreCase) ? 2.25f : 2.0f;
						else
							stab = string.Equals(attacker.Ability, "Adaptability", StringComparison.OrdinalIgnoreCase) ? 2.0f : 1.5f;
					}
					else if (isBaseType)
					{
						stab = string.Equals(attacker.Ability, "Adaptability", StringComparison.OrdinalIgnoreCase) ? 2.0f : 1.5f;
					}
				}
				else if (isBaseType)
				{
					stab = string.Equals(attacker.Ability, "Adaptability", StringComparison.OrdinalIgnoreCase) ? 2.0f : 1.5f;
				}

				float burnMod = (attacker.Status == PokemonStatusCondition.Burn && moveCat == MoveCategory.Physical) ? 0.5f : 1.0f;

				// Defensive Type Effectiveness (defender.GetDefensiveTypes() handles Terastallization single-type replacement)
				float typeMult = 1.0f;
				var defTypes = defender.GetDefensiveTypes();
				if (defTypes != null && defTypes.Count > 0)
				{
					foreach (var dt in defTypes)
					{
						typeMult *= TypeChart.GetEffectiveness(moveType, dt);
					}
				}
				else if (defender.Species != null)
				{
					typeMult = TypeChart.GetEffectiveness(moveType, defender.Species.PrimaryType);
					if (defender.Species.SecondaryType.HasValue)
					{
						typeMult *= TypeChart.GetEffectiveness(moveType, defender.Species.SecondaryType.Value);
					}
				}

				// Mold Breaker bypasses defender defensive abilities
				bool moldBreaker = string.Equals(attacker.Ability, "MoldBreaker", StringComparison.OrdinalIgnoreCase);

				// Ability Immunities & Absorb
				if (!moldBreaker)
				{
					if (string.Equals(defender.Ability, "Levitate", StringComparison.OrdinalIgnoreCase) && moveType == PokemonType.Ground)
					{
						typeMult = 0f;
					}
					else if (string.Equals(defender.Ability, "EarthEater", StringComparison.OrdinalIgnoreCase) && moveType == PokemonType.Ground)
					{
						typeMult = 0f;
						defender.Heal(Math.Max(1, defender.MaxHP / 4));
					}
					else if (string.Equals(defender.Ability, "FlashFire", StringComparison.OrdinalIgnoreCase) && moveType == PokemonType.Fire)
					{
						typeMult = 0f;
					}
					else if ((string.Equals(defender.Ability, "WaterAbsorb", StringComparison.OrdinalIgnoreCase) || string.Equals(defender.Ability, "DrySkin", StringComparison.OrdinalIgnoreCase)) && moveType == PokemonType.Water)
					{
						typeMult = 0f;
						defender.Heal(Math.Max(1, defender.MaxHP / 4));
					}
					else if ((string.Equals(defender.Ability, "VoltAbsorb", StringComparison.OrdinalIgnoreCase) || string.Equals(defender.Ability, "LightningRod", StringComparison.OrdinalIgnoreCase)) && moveType == PokemonType.Electric)
					{
						typeMult = 0f;
						defender.Heal(Math.Max(1, defender.MaxHP / 4));
					}
				}

				// Ability Attack & Stat Modifiers
				if (physicalStats && (string.Equals(attacker.Ability, "HugePower", StringComparison.OrdinalIgnoreCase) || string.Equals(attacker.Ability, "PurePower", StringComparison.OrdinalIgnoreCase)))
				{
					attackStat *= 2.0f;
				}
				if (string.Equals(attacker.Ability, "Technician", StringComparison.OrdinalIgnoreCase) && effectivePower <= 60)
				{
					effectivePower = (int)(effectivePower * 1.5f);
				}
				if (string.Equals(defender.Ability, "ThickFat", StringComparison.OrdinalIgnoreCase) && (moveType == PokemonType.Fire || moveType == PokemonType.Ice))
				{
					typeMult *= 0.5f;
				}
				if (string.Equals(attacker.Ability, "MegaLauncher", StringComparison.OrdinalIgnoreCase) && (move.Name.Contains("Pulse", StringComparison.OrdinalIgnoreCase) || move.Name.Contains("Sphere", StringComparison.OrdinalIgnoreCase)))
				{
					attackStat *= 1.5f;
				}
				if (string.Equals(attacker.Ability, "StrongJaw", StringComparison.OrdinalIgnoreCase) && (move.Name.Contains("Bite", StringComparison.OrdinalIgnoreCase) || move.Name.Contains("Crunch", StringComparison.OrdinalIgnoreCase) || move.Name.Contains("Fang", StringComparison.OrdinalIgnoreCase)))
				{
					attackStat *= 1.5f;
				}
				if (string.Equals(attacker.Ability, "IronFist", StringComparison.OrdinalIgnoreCase) && move.Name.Contains("Punch", StringComparison.OrdinalIgnoreCase))
				{
					attackStat *= 1.2f;
				}
				if (string.Equals(attacker.Ability, "Reckless", StringComparison.OrdinalIgnoreCase) && (move.Name.Contains("Blitz", StringComparison.OrdinalIgnoreCase) || move.Name.Contains("Edge", StringComparison.OrdinalIgnoreCase) || move.Name.Contains("Brave Bird", StringComparison.OrdinalIgnoreCase) || move.Name.Contains("Take Down", StringComparison.OrdinalIgnoreCase)))
				{
					attackStat *= 1.2f;
				}
				if ((string.Equals(defender.Ability, "Filter", StringComparison.OrdinalIgnoreCase) || string.Equals(defender.Ability, "SolidRock", StringComparison.OrdinalIgnoreCase)) && typeMult > 1.1f)
				{
					typeMult *= 0.75f;
				}
				if (string.Equals(defender.Ability, "Multiscale", StringComparison.OrdinalIgnoreCase) && defender.CurrentHP == defender.MaxHP)
				{
					typeMult *= 0.5f;
				}
				if (attacker.CurrentHP <= attacker.MaxHP / 3)
				{
					if (string.Equals(attacker.Ability, "Blaze", StringComparison.OrdinalIgnoreCase) && moveType == PokemonType.Fire) attackStat *= 1.5f;
					else if (string.Equals(attacker.Ability, "Torrent", StringComparison.OrdinalIgnoreCase) && moveType == PokemonType.Water) attackStat *= 1.5f;
					else if (string.Equals(attacker.Ability, "Overgrow", StringComparison.OrdinalIgnoreCase) && moveType == PokemonType.Grass) attackStat *= 1.5f;
					else if (string.Equals(attacker.Ability, "Swarm", StringComparison.OrdinalIgnoreCase) && moveType == PokemonType.Bug) attackStat *= 1.5f;
				}
				if ((string.Equals(attacker.Ability, "HugePower", StringComparison.OrdinalIgnoreCase) || string.Equals(attacker.Ability, "PurePower", StringComparison.OrdinalIgnoreCase)) && moveCat == MoveCategory.Physical)
				{
					attackStat *= 2.0f;
				}
				if (string.Equals(attacker.Ability, "ToughClaws", StringComparison.OrdinalIgnoreCase) && moveCat == MoveCategory.Physical)
				{
					attackStat *= 1.3f;
				}
				if (string.Equals(attacker.Ability, "Guts", StringComparison.OrdinalIgnoreCase) && attacker.Status != PokemonStatusCondition.None && moveCat == MoveCategory.Physical)
				{
					attackStat *= 1.5f;
					burnMod = 1.0f;
				}

				// Held Item Stat Modifiers
				if (PokemonHeldItemManager.Matches(attacker.HeldItem, "ChoiceBand") && moveCat == MoveCategory.Physical)
				{
					attackStat *= 1.5f;
				}
				else if (PokemonHeldItemManager.Matches(attacker.HeldItem, "ChoiceSpecs") && moveCat == MoveCategory.Special)
				{
					attackStat *= 1.5f;
				}

				// Defender Held Item Modifiers
				if (PokemonHeldItemManager.Matches(defender.HeldItem, "AssaultVest") && moveCat == MoveCategory.Special)
				{
					defStat *= 1.5f;
				}
				else if (PokemonHeldItemManager.Matches(defender.HeldItem, "Eviolite"))
				{
					defStat *= 1.5f;
				}

				// Held Item Direct Damage Modifiers
				float itemDamageMult = 1.0f;
				if (PokemonHeldItemManager.Matches(attacker.HeldItem, "LifeOrb"))
				{
					itemDamageMult *= 1.3f;
				}
				else if (PokemonHeldItemManager.Matches(attacker.HeldItem, "ExpertBelt") && typeMult > 1.1f)
				{
					itemDamageMult *= 1.2f;
				}
				else if (PokemonHeldItemManager.GetTypeBoostForHeldItem(attacker.HeldItem) == moveType)
				{
					itemDamageMult *= 1.2f;
				}

				// Z-Power Ring bonus (+10% damage to Z-Moves)
				if (useZMove && isPlayerAttacking && PokemonItemManager.IsWearingZPowerRing(Game1.player))
				{
					itemDamageMult *= 1.10f;
				}

				// Weather multipliers (Canonical Gen 3-8)
				float weatherMult = 1.0f;
				if (CurrentWeather == BattleWeather.Rain)
				{
					if (moveType == PokemonType.Water) weatherMult = 1.5f;
					else if (moveType == PokemonType.Fire) weatherMult = 0.5f;
				}
				else if (CurrentWeather == BattleWeather.HarshSun)
				{
					if (moveType == PokemonType.Fire) weatherMult = 1.5f;
					else if (moveType == PokemonType.Water) weatherMult = 0.5f;
				}

				bool isCrit = move.AlwaysCrit || Game1.random.NextDouble() < (move.HighCrit ? 0.125 : 0.0625);
				if (string.Equals(defender.Ability, "ShellArmor", StringComparison.OrdinalIgnoreCase) || string.Equals(defender.Ability, "BattleArmor", StringComparison.OrdinalIgnoreCase))
				{
					isCrit = false;
				}
				float critMult = isCrit ? 1.5f : 1.0f;
				float randMult = (float)(0.85 + Game1.random.NextDouble() * 0.15);

				int movePower = ResolveMovePower(move, attacker, defender, effectivePower);
				int hitCount = ResolveHitCount(move);

				int baseDmg = (int)((((2f * attacker.Level / 5f + 2f) * movePower * (attackStat / Math.Max(1f, defStat))) / 50f + 2f) * stab * burnMod * typeMult * weatherMult * critMult * randMult * itemDamageMult) * hitCount;
				int finalDamage = (typeMult <= 0.05f) ? 0 : Math.Max(1, baseDmg);

				// Fixed-damage moves ignore the formula. Level/Flat damage also ignore
				// type immunities (Night Shade hits Normal, Seismic Toss hits Ghost);
				// half-HP moves still respect them (Super Fang vs Ghost).
				if (move.FixedDamage != FixedDamageMode.None && !useZMove && !useMaxMove)
				{
					int fixedDamage = move.FixedDamage switch
					{
						FixedDamageMode.HalfCurrentHp => Math.Max(1, defender.CurrentHP / 2),
						FixedDamageMode.Level => Math.Max(1, attacker.Level),
						FixedDamageMode.Flat => Math.Max(1, move.FixedDamageValue),
						_ => finalDamage
					};
					bool ignoresType = move.FixedDamage is FixedDamageMode.Level or FixedDamageMode.Flat;
					finalDamage = ignoresType ? fixedDamage : ((typeMult <= 0.05f) ? 0 : fixedDamage);
				}

				// Focus Sash check
				if (finalDamage >= defender.CurrentHP && defender.CurrentHP == defender.MaxHP && PokemonHeldItemManager.Matches(defender.HeldItem, "FocusSash"))
				{
					finalDamage = Math.Max(0, defender.CurrentHP - 1);
					defender.HeldItem = null;
					Game1.playSound("reward");
					QueueMessage($"{defender.Nickname} resistiu ao golpe com 1 HP graças à Focus Sash!");
				}

				// Positions for visual FX
				Vector2 aPos = isPlayerAttacking
					? new Vector2(xPositionOnScreen + 160, yPositionOnScreen + height - _boxHeight - 190)
					: new Vector2(xPositionOnScreen + width - 230, yPositionOnScreen + 110);
				Vector2 dPos = isPlayerAttacking
					? new Vector2(xPositionOnScreen + width - 230, yPositionOnScreen + 110)
					: new Vector2(xPositionOnScreen + 160, yPositionOnScreen + height - _boxHeight - 190);

				// Animate Attacker Lunge
				if (isPlayerAttacking) _playerLungeOffset = new Vector2(35f, -18f);
				else _enemyLungeOffset = new Vector2(-35f, 18f);

				// Sound effect at cast
				if (move.Type == PokemonType.Fire) Game1.playSound("fireball");
				else if (move.Type == PokemonType.Electric) Game1.playSound("thunder");
				else if (move.Type == PokemonType.Water) Game1.playSound("slosh");
				else if (move.Type == PokemonType.Grass) Game1.playSound("dwoop");
				else Game1.playSound("clubhit");

				// Fire-type attack thaws frozen defender
				if (move.Type == PokemonType.Fire && defender.Status == PokemonStatusCondition.Freeze)
				{
					defender.Status = PokemonStatusCondition.None;
				}

				_activeAttackFx = new AttackFx
				{
					Type = move.Type,
					MoveName = displayMoveName,
					Move = move,
					IsPlayerAttacking = isPlayerAttacking,
					AttackerPos = aPos,
					DefenderPos = dPos,
					Damage = finalDamage,
					Hits = hitCount,
					IsCrit = isCrit,
					TypeMult = typeMult,
					Attacker = attacker,
					Defender = defender,
					OnComplete = onComplete
				};
			});
		};

		// Pre-attack Status Condition Checks (Sleep, Freeze, Paralysis, Confusion)
		Action checkConfusion = () =>
		{
			if (attacker.ConfusionTurns > 0)
			{
				attacker.ConfusionTurns--;
				if (attacker.ConfusionTurns <= 0)
				{
					QueueMessage($"{attacker.Nickname} recuperou a clareza e saiu da confusão!", performMove);
					return;
				}
				if (Game1.random.NextDouble() < 0.33)
				{
					int selfDmg = Math.Max(1, (int)(((2f * attacker.Level / 5f + 2f) * 40f * (attacker.Attack / Math.Max(1f, (float)attacker.Defense))) / 50f + 2f));
					attacker.TakeDamage(selfDmg);
					if (isPlayerAttacking) _playerDamageShake = 0.8f;
					else _enemyDamageShake = 0.8f;
					Game1.playSound("clubhit");
					QueueMessage($"{attacker.Nickname} está confuso e feriu a si mesmo na confusão (-{selfDmg} HP)!", onComplete);
					return;
				}
			}
			performMove();
		};

		if (attacker.Status == PokemonStatusCondition.Sleep)
		{
			if (string.Equals(move.Name, "Snore", StringComparison.OrdinalIgnoreCase) || string.Equals(move.Name, "Sleep Talk", StringComparison.OrdinalIgnoreCase))
			{
				if (attacker.StatusTurns > 0) attacker.StatusTurns--;
				checkConfusion();
				return;
			}

			if (attacker.StatusTurns > 0)
			{
				attacker.StatusTurns--;
				Game1.playSound("dwop");
				QueueMessage($"{attacker.Nickname} está dormindo profundamente!", onComplete);
				return;
			}
			attacker.Status = PokemonStatusCondition.None;
			QueueMessage($"{attacker.Nickname} acordou!", checkConfusion);
			return;
		}

		if (attacker.Status == PokemonStatusCondition.Freeze)
		{
			if (move.Type == PokemonType.Fire || Game1.random.NextDouble() < 0.20)
			{
				attacker.Status = PokemonStatusCondition.None;
				QueueMessage($"{attacker.Nickname} descongelou!", checkConfusion);
				return;
			}
			Game1.playSound("crystal");
			QueueMessage($"{attacker.Nickname} está congelado solidamente e não pode atacar!", onComplete);
			return;
		}

		if (attacker.Status == PokemonStatusCondition.Paralysis && Game1.random.NextDouble() < 0.25)
		{
			Game1.playSound("thunder");
			QueueMessage($"{attacker.Nickname} está paralisado! Não conseguiu se mover!", onComplete);
			return;
		}

		checkConfusion();
	}

	private void HandleEnemyFainted()
	{
		Game1.playSound("monsterdead");
		if (!IsWild)
		{
			_isEnemyRetrieving = true;
			_enemyRetrieveTimer = 0f;
			Game1.playSound("wand");
		}
		else
		{
			SourceWildSpawn?.Remove();
		}

		QueueMessage($"{EnemyPokemon.Nickname} adversário desmaiou!", () =>
		{
			_enemyPokemonScale = 0f;
			_isEnemyRetrieving = false;

			// Give EXP + EVs (Canonical Gen 3-8 Formula)
			int baseExp = EnemyPokemon.Species.BaseExpYield > 0 ? EnemyPokemon.Species.BaseExpYield : 100;
			float trainerMult = !IsWild ? 1.5f : 1.0f;
			int exp = Math.Max(10, (int)((baseExp * EnemyPokemon.Level * trainerMult) / 7f));
			PlayerPokemon.CurrentExp += exp;
			PlayerPokemon.GainEffortValues(EnemyPokemon.Species);

			// Check Gen 6+ Exp. Share Ring
			bool expShareActive = PokemonItemManager.IsWearingExpShare(Game1.player);
			int sharedExp = Math.Max(1, exp / 2);
			int sharedCount = 0;
			if (expShareActive)
			{
				for (int i = 0; i < PlayerTeam.Count; i++)
				{
					var ally = PlayerTeam[i];
					if (ally != null && ally != PlayerPokemon && !ally.IsFainted)
					{
						ally.CurrentExp += sharedExp;
						ally.GainEffortValues(EnemyPokemon.Species);
						while (ally.Level < 100 && ally.CurrentExp >= ally.ExpToNextLevel)
						{
							ally.CurrentExp -= ally.ExpToNextLevel;
							ally.Level++;
							ally.RecalculateStats();
							ally.CurrentHP = Math.Min(ally.MaxHP, ally.CurrentHP + 5);
						}
						sharedCount++;
					}
				}
			}

			string expMsg = (expShareActive && sharedCount > 0)
				? $"{PlayerPokemon.Nickname} ganhou {exp} EXP! (Anel Exp. Share: +{sharedExp} EXP para a equipe!)"
				: $"{PlayerPokemon.Nickname} ganhou {exp} pontos de experiência!";

			QueueMessage(expMsg, () =>
			{
				if (PlayerPokemon.CurrentExp >= PlayerPokemon.ExpToNextLevel)
				{
					PlayerPokemon.CurrentExp -= PlayerPokemon.ExpToNextLevel;
					PlayerPokemon.Level++;
					PlayerPokemon.RecalculateStats();
					PlayerPokemon.CurrentHP = Math.Min(PlayerPokemon.MaxHP, PlayerPokemon.CurrentHP + 6);
					Game1.playSound("reward");
					QueueMessage($"{PlayerPokemon.Nickname} subiu para o Nível {PlayerPokemon.Level}!", () =>
					{
						Action afterMoveLearn = () =>
						{
							var evo = PokemonEvolution.CheckEvolution(PlayerPokemon, EvolutionTrigger.LevelUp);
							if (evo != null)
							{
								PokemonSpecies target = PokemonSpecies.Get(evo.TargetSpecies);
								if (target != null)
								{
									Game1.activeClickableMenu = new PokemonEvolutionMenu(PlayerPokemon, target, () =>
									{
										Game1.activeClickableMenu = this;
										CheckNextEnemy();
									});
									return;
								}
							}
							CheckNextEnemy();
						};

						string learnedMove = PokemonInstance.GetMoveLearnedAtLevel(PlayerPokemon.Species, PlayerPokemon.Level);
						if (!string.IsNullOrEmpty(learnedMove) && !PlayerPokemon.Moves.Exists(m => m.Name.Equals(learnedMove, StringComparison.OrdinalIgnoreCase)))
						{
							if (PlayerPokemon.Moves.Count < 4)
							{
								PlayerPokemon.Moves.Add(PokemonMove.Get(learnedMove));
								PlayerParty.Instance.SaveParty();
								Game1.playSound("reward");
								QueueMessage($"{PlayerPokemon.Nickname} aprendeu {learnedMove}!", afterMoveLearn);
								return;
							}
							else
							{
								Game1.activeClickableMenu = new PokemonTargetMenu(PlayerPokemon, learnedMove, choice =>
								{
									Game1.activeClickableMenu = this;
									if (choice < 0)
									{
										QueueMessage($"{PlayerPokemon.Nickname} não aprendeu {learnedMove}.", afterMoveLearn);
									}
									else
									{
										string forgotten = PlayerPokemon.Moves[choice].Name;
										PlayerPokemon.Moves[choice] = PokemonMove.Get(learnedMove);
										PlayerParty.Instance.SaveParty();
										Game1.playSound("reward");
										QueueMessage($"{PlayerPokemon.Nickname} esqueceu {forgotten} e aprendeu {learnedMove}!", afterMoveLearn);
									}
								});
								return;
							}
						}

						afterMoveLearn();
					});
				}
				else
				{
					CheckNextEnemy();
				}
			});
		});
	}

	private void CheckNextEnemy()
	{
		for (int i = 0; i < EnemyTeam.Count; i++)
		{
			if (!EnemyTeam[i].IsFainted)
			{
				EnemyIndex = i;
				_enemyAtkStage = 0; _enemyDefStage = 0; _enemySpAtkStage = 0; _enemySpDefStage = 0; _enemySpeedStage = 0;
				_displayEnemyHP = EnemyPokemon.CurrentHP;
				_enemyPokemonScale = 0f;
				_isEnemySending = true;
				_enemySendTimer = 0f;
				Game1.playSound("throw");

				if (IsWild && EnemyPokemon?.Species != null)
				{
					PokemonPokedex.Instance.RecordSeen(EnemyPokemon.Species.Name);
					PlayerParty.Instance.MarkSeen(EnemyPokemon.Species.Name);
				}

				bool isEnemyMega = EnemyPokemon != null && (EnemyPokemon.IsMegaEvolved || (EnemyPokemon.Species != null && EnemyPokemon.Species.IsMega));
				string megaText = isEnemyMega ? " (🧬 Mega Evolução!)" : "";
				QueueMessage($"{(IsWild ? "Apareceu" : $"{TrainerName} enviou")} {EnemyPokemon.Nickname}{megaText}!", () =>
				{
					_enemyPokemonScale = 3.2f;
					_isEnemySending = false;
					TriggerSwitchInAbility(EnemyPokemon, PlayerPokemon, () =>
					{
						Phase = BattlePhase.ActionSelect;
					});
				});
				return;
			}
		}

		// All enemies fainted -> Player wins!
		Phase = BattlePhase.Victory;
		Game1.playSound("reward");
		PokemonMusicManager.PlayVictoryMusic();
		if (IsWild)
		{
			QueueMessage("Você venceu a batalha contra o Pokémon selvagem!", () =>
			{
				exitThisMenuNoSound();
			});
		}
		else
		{
			string cleanName = TrainerName.Replace(" ", "").Replace("(", "").Replace(")", "");
			Game1.player?.mailReceived?.Add("Poke_Defeated_" + cleanName);

			int rewardGold = EnemyTeam.Count * 250;
			string specialVictoryMsg = null;

			if (cleanName.Equals("Red", StringComparison.OrdinalIgnoreCase))
			{
				rewardGold = 10000;
				specialVictoryMsg = "Red: ...! ...!\n(Red acena em silêncio com profundo respeito e reconhece você como o novo Campeão Supremo!)";
			}
			else if (cleanName.Equals("Steven", StringComparison.OrdinalIgnoreCase))
			{
				rewardGold = 8000;
				specialVictoryMsg = "Steven: Impressionante! Sua conexão com seus Pokémon brilha com mais intensidade que o mais raro dos meteoritos de Hoenn!";
			}
			else if (cleanName.Equals("Qi", StringComparison.OrdinalIgnoreCase))
			{
				rewardGold = 5000;
				Game1.player.QiGems += 25;
				specialVictoryMsg = "Sr. Qi: Extraordinário, garoto! Você superou todas as minhas expectativas calculadas. Pegue 25 Gemas Qi pelo espetáculo!";
			}
			else if (cleanName.Equals("Wizard", StringComparison.OrdinalIgnoreCase) || cleanName.Equals("Rasmodius", StringComparison.OrdinalIgnoreCase))
			{
				rewardGold = 3500;
				specialVictoryMsg = "M. Rasmodius: Magnífico! As energias elementais do éter fluem em perfeita harmonia através do seu comando!";
			}
			else if (cleanName.Equals("Marlon", StringComparison.OrdinalIgnoreCase))
			{
				rewardGold = 3000;
				specialVictoryMsg = "Marlon: Bom trabalho, aventureiro! Poucos guerreiros na guilda demonstraram tanta fibra e técnica em combate!";
			}
			else if (cleanName.Equals("Clint", StringComparison.OrdinalIgnoreCase))
			{
				rewardGold = 2500;
				specialVictoryMsg = "Clint: Uau... Essa sua técnica foi tão pesada quanto uma bigorna de ferro. Belo combate!";
			}
			else if (cleanName.Equals("Willy", StringComparison.OrdinalIgnoreCase))
			{
				rewardGold = 2000;
				specialVictoryMsg = "Willy: Pelas barbas de Netuno! Você comanda suas criaturas com a força e a astúcia de um velho capitão do mar!";
			}

			Game1.player.Money += rewardGold;
			QueueMessage($"Você derrotou o Treinador {TrainerName}!", () =>
			{
				if (!string.IsNullOrEmpty(specialVictoryMsg))
				{
					QueueMessage(specialVictoryMsg, () =>
					{
						QueueMessage($"Você obteve {rewardGold} ouros pela vitória!", () =>
						{
							exitThisMenuNoSound();
						});
					});
				}
				else
				{
					QueueMessage($"Você obteve {rewardGold} ouros pela vitória!", () =>
					{
						exitThisMenuNoSound();
					});
				}
			});
		}
	}

	private void HandlePlayerFainted()
	{
		Game1.playSound("ow");
		_isSwitching = true;
		_switchStage = SwitchStage.Retrieve;
		_switchTimer = 0f;
		_playerSpriteScale = 3.2f;
		Game1.playSound("wand");

		QueueMessage($"{PlayerPokemon.Nickname} desmaiou!", () =>
		{
			_playerSpriteScale = 0f;
			_isSwitching = false;
			_switchStage = SwitchStage.None;

			int aliveCount = 0;
			for (int i = 0; i < PlayerTeam.Count; i++)
			{
				if (!PlayerTeam[i].IsFainted)
				{
					aliveCount++;
				}
			}

			if (aliveCount > 0)
			{
				SetupSwitchButtons();
				Phase = BattlePhase.SwitchSelect;
				_currentMessage = "Escolha o próximo Pokémon para entrar em batalha!";
			}
			else
			{
				Phase = BattlePhase.Defeat;
				Game1.playSound("fishEscape");
				QueueMessage("Todos os seus Pokémon desmaiaram em combate...", () =>
				{
					exitThisMenuNoSound();
					Game1.addHUDMessage(new HUDMessage("Seus Pokémon precisam de cuidados! Visite o Dr. Harvey ou use uma Mesa de Recuperação.", 3));
				});
			}
		});
	}

	private void SetupBagButtons()
	{
		_bagButtons.Clear();

		var available = new List<(string id, string label)>();

		int potionCount = Game1.player.Items.CountId("(O)PokemonPotion");
		if (potionCount > 0) available.Add(("PokemonPotion", $"Potion (x{potionCount})"));

		int superPotionCount = Game1.player.Items.CountId("(O)PokemonSuperPotion");
		if (superPotionCount > 0) available.Add(("PokemonSuperPotion", $"Super Potion (x{superPotionCount})"));

		int reviveCount = Game1.player.Items.CountId("(O)PokemonRevive");
		if (reviveCount > 0) available.Add(("PokemonRevive", $"Revive (x{reviveCount})"));

		// Poké Balls list (English names!)
		string[] ballIds = new[] { "PokeBall", "GreatBall", "UltraBall", "MasterBall", "PremierBall", "FastBall", "HeavyBall", "HealBall" };
		for (int i = 0; i < ballIds.Length; i++)
		{
			string bid = ballIds[i];
			int count = Game1.player.Items.CountId("(O)" + bid);
			if (count > 0 || bid == "PokeBall")
			{
				string ballName = bid switch
				{
					"PokeBall" => "Poké Ball",
					"GreatBall" => "Great Ball",
					"UltraBall" => "Ultra Ball",
					"MasterBall" => "Master Ball",
					"PremierBall" => "Premier Ball",
					"FastBall" => "Fast Ball",
					"HeavyBall" => "Heavy Ball",
					"HealBall" => "Heal Ball",
					_ => bid
				};
				available.Add((bid, $"{ballName} (x{count})"));
			}
		}

		int itemsPerPage = 6;
		int maxPage = Math.Max(0, (available.Count - 1) / itemsPerPage);
		if (_bagPage > maxPage) _bagPage = maxPage;
		if (_bagPage < 0) _bagPage = 0;

		int startIndex = _bagPage * itemsPerPage;
		int countOnPage = Math.Min(itemsPerPage, available.Count - startIndex);

		int startY = _boxY + 16;
		for (int i = 0; i < countOnPage; i++)
		{
			var (itemId, label) = available[startIndex + i];
			int col = i % 2;
			int row = i / 2;
			int bx = _boxX + 25 + col * 265;
			int by = startY + row * 44;
			_bagButtons.Add(new ClickableComponent(new Rectangle(bx, by, 255, 40), itemId, label));
		}

		_bagPrevButton = new ClickableComponent(new Rectangle(_boxX + _boxWidth - 110, _boxY + 56, 45, 34), "bag_prev", "◄");
		_bagNextButton = new ClickableComponent(new Rectangle(_boxX + _boxWidth - 60, _boxY + 56, 45, 34), "bag_next", "►");
	}

	private void UseBagItem(string itemId)
	{
		if (itemId == "PokemonPotion" || itemId == "PokemonSuperPotion" || itemId == "PokemonRevive")
		{
			string fullId = "(O)" + itemId;
			if (Game1.player.Items.ContainsId(fullId))
			{
				if (itemId == "PokemonRevive")
				{
					var target = (PlayerPokemon != null && PlayerPokemon.IsFainted) ? PlayerPokemon : PlayerTeam.Find(p => p.IsFainted);
					if (target == null)
					{
						Game1.playSound("cancel");
						QueueMessage("Nenhum Pokémon da sua equipe está desmaiado!", () => Phase = BattlePhase.BagSelect);
						return;
					}
					Game1.player.Items.ReduceId(fullId, 1);
					target.Revive(0.5f);
					if (target == PlayerPokemon) _displayPlayerHP = PlayerPokemon.CurrentHP;
					Game1.playSound("heal");
					QueueMessage($"Você usou Revive! {target.Nickname} reanimou com metade do HP!", () =>
					{
						PokemonMove enemyMove = SelectEnemyMove();
						ExecuteAttack(EnemyPokemon, PlayerPokemon, enemyMove, () =>
						{
							if (PlayerPokemon.IsFainted) HandlePlayerFainted();
							else RunEndOfTurnEffects(() => Phase = BattlePhase.ActionSelect);
						});
					});
					return;
				}

				int healAmount = (itemId == "PokemonPotion") ? 50 : 100;
				Game1.player.Items.ReduceId(fullId, 1);
				PlayerPokemon.Heal(healAmount);
				_displayPlayerHP = PlayerPokemon.CurrentHP;
				Game1.playSound("heal");
				QueueMessage($"Você usou {itemId}! {PlayerPokemon.Nickname} recuperou {healAmount} HP!", () =>
				{
					PokemonMove enemyMove = SelectEnemyMove();
					ExecuteAttack(EnemyPokemon, PlayerPokemon, enemyMove, () =>
					{
						if (PlayerPokemon.IsFainted) HandlePlayerFainted();
						else RunEndOfTurnEffects(() => Phase = BattlePhase.ActionSelect);
					});
				});
			}
			else
			{
				Game1.playSound("cancel");
				QueueMessage("Você não possui este item!", () => Phase = BattlePhase.BagSelect);
			}
		}
		else if (itemId.EndsWith("Ball"))
		{
			if (!IsWild)
			{
				Game1.playSound("cancel");
				QueueMessage("O(A) Treinador(a) bloqueou a Poké Bola! É proibido capturar Pokémon de outros treinadores!", () =>
				{
					Phase = BattlePhase.BagSelect;
				});
				return;
			}

			string fullId = "(O)" + itemId;
			if (Game1.player.Items.ContainsId(fullId))
			{
				Game1.player.Items.ReduceId(fullId, 1);
				Game1.playSound("throw");

				float multiplier = itemId switch
				{
					"MasterBall" => 999.0f,
					"GreatBall" => 1.5f,
					"UltraBall" => 2.0f,
					"PremierBall" => 1.0f,
					"FastBall" => (EnemyPokemon.Speed > 70) ? 2.5f : 1.0f,
					"HeavyBall" => 1.8f,
					"HealBall" => 1.2f,
					_ => 1.0f
				};

				float catchRate = EnemyPokemon.Species.CatchRate > 0 ? (float)EnemyPokemon.Species.CatchRate : 45f;
				float hpFactor = (3f * EnemyPokemon.MaxHP - 2f * EnemyPokemon.CurrentHP) / (3f * EnemyPokemon.MaxHP);
				float statusMult = 1.0f;
				if (EnemyPokemon.Status == PokemonStatusCondition.Sleep || EnemyPokemon.Status == PokemonStatusCondition.Freeze)
				{
					statusMult = 2.5f;
				}
				else if (EnemyPokemon.Status != PokemonStatusCondition.None)
				{
					statusMult = 1.5f;
				}
				float chance = (itemId == "MasterBall")
					? 1.0f
					: Math.Clamp((catchRate * hpFactor * multiplier * statusMult) / 255f, 0.015f, 0.99f);

				QueueMessage($"Você arremessou uma {itemId}!", () =>
				{
					StartCaptureAnimation(itemId, chance);
				});
			}
			else
			{
				Game1.playSound("cancel");
				QueueMessage($"Você não possui {itemId} no inventário!", () => Phase = BattlePhase.BagSelect);
			}
		}
	}

	private void StartCaptureAnimation(string itemId, float chance)
	{
		_isCapturing = true;
		_capturingBallId = itemId;
		_activeCaptureBallTex = GetBallTexture(itemId);
		_captureStage = CaptureStage.ThrowArc;
		_captureTimer = 0f;
		_currentWiggleCount = 0;
		_captureBallRot = 0f;
		_captureStars.Clear();

		_captureResult = (itemId == "MasterBall") || (Game1.random.NextDouble() < chance);
		if (_captureResult)
		{
			_allowedWiggles = 3;
		}
		else
		{
			double roll = Game1.random.NextDouble();
			if (roll < 0.25) _allowedWiggles = 0;
			else if (roll < 0.65) _allowedWiggles = 1;
			else _allowedWiggles = 2;
		}

		Phase = BattlePhase.ActionSelect;
	}

	private void TriggerSwitchInAbility(PokemonInstance mon, PokemonInstance opponent, Action onDone)
	{
		if (mon == null || mon.IsFainted || string.IsNullOrEmpty(mon.Ability))
		{
			onDone?.Invoke();
			return;
		}

		string ab = mon.Ability.Trim();
		bool isPlayer = (mon == PlayerPokemon);

		// Intimidate
		if (string.Equals(ab, "Intimidate", StringComparison.OrdinalIgnoreCase))
		{
			if (opponent != null && !opponent.IsFainted)
			{
				if (string.Equals(opponent.Ability, "ClearBody", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(opponent.Ability, "WhiteSmoke", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(opponent.Ability, "HyperCutter", StringComparison.OrdinalIgnoreCase))
				{
					QueueMessage($"A habilidade {opponent.Ability} de {opponent.Nickname} protegeu seus atributos!", onDone);
					return;
				}

				if (isPlayer) _enemyAtkStage = Math.Max(-6, _enemyAtkStage - 1);
				else _playerAtkStage = Math.Max(-6, _playerAtkStage - 1);

				Game1.playSound("debuffHit");
				QueueMessage($"A habilidade Intimidação de {mon.Nickname} reduziu o Ataque de {opponent.Nickname}!", onDone);
				return;
			}
		}

		// Drizzle
		if (string.Equals(ab, "Drizzle", StringComparison.OrdinalIgnoreCase))
		{
			CurrentWeather = BattleWeather.Rain;
			WeatherTurns = 5;
			Game1.playSound("rainsound");
			QueueMessage($"A habilidade Chuva Repentina (Drizzle) de {mon.Nickname} convocou uma tempestade!", onDone);
			return;
		}

		// Drought
		if (string.Equals(ab, "Drought", StringComparison.OrdinalIgnoreCase))
		{
			CurrentWeather = BattleWeather.HarshSun;
			WeatherTurns = 5;
			Game1.playSound("flameSpell");
			QueueMessage($"A habilidade Seca Extrema (Drought) de {mon.Nickname} intensificou os raios solares!", onDone);
			return;
		}

		// Sand Stream
		if (string.Equals(ab, "SandStream", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "Sand Stream", StringComparison.OrdinalIgnoreCase))
		{
			CurrentWeather = BattleWeather.Sandstorm;
			WeatherTurns = 5;
			Game1.playSound("debrisHit");
			QueueMessage($"A habilidade Fluxo de Areia (Sand Stream) de {mon.Nickname} levantou uma tempestade de areia!", onDone);
			return;
		}

		// Snow Warning
		if (string.Equals(ab, "SnowWarning", StringComparison.OrdinalIgnoreCase) || string.Equals(ab, "Snow Warning", StringComparison.OrdinalIgnoreCase))
		{
			CurrentWeather = BattleWeather.Snow;
			WeatherTurns = 5;
			Game1.playSound("freeze");
			QueueMessage($"A habilidade Alerta de Neve (Snow Warning) de {mon.Nickname} trouxe uma nevasca sobre o campo!", onDone);
			return;
		}

		// Primordial Sea
		if (string.Equals(ab, "PrimordialSea", StringComparison.OrdinalIgnoreCase))
		{
			PokemonHUD.SetWorldWeather(true);
			Game1.playSound("rainsound");
			QueueMessage($"O Mar Primordial de {mon.Nickname} cobriu o campo com um dilúvio avassalador!", onDone);
			return;
		}

		// Desolate Land
		if (string.Equals(ab, "DesolateLand", StringComparison.OrdinalIgnoreCase))
		{
			PokemonHUD.SetWorldWeather(false);
			Game1.playSound("flameSpell");
			QueueMessage($"A Terra Desolada de {mon.Nickname} incinerou o ambiente em calor abrasador!", onDone);
			return;
		}

		// Trace
		if (string.Equals(ab, "Trace", StringComparison.OrdinalIgnoreCase))
		{
			if (opponent != null && !opponent.IsFainted && !string.IsNullOrEmpty(opponent.Ability) && !string.Equals(opponent.Ability, "Trace", StringComparison.OrdinalIgnoreCase))
			{
				mon.Ability = opponent.Ability;
				Game1.playSound("heal");
				QueueMessage($"A habilidade Cópia (Trace) de {mon.Nickname} copiou {opponent.Ability} de {opponent.Nickname}!", onDone);
				return;
			}
		}

		// Download
		if (string.Equals(ab, "Download", StringComparison.OrdinalIgnoreCase))
		{
			if (opponent != null && !opponent.IsFainted)
			{
				bool boostAtk = (opponent.Defense < opponent.SpDefense);
				if (isPlayer)
				{
					if (boostAtk) _playerAtkStage = Math.Min(6, _playerAtkStage + 1);
					else _playerSpAtkStage = Math.Min(6, _playerSpAtkStage + 1);
				}
				else
				{
					if (boostAtk) _enemyAtkStage = Math.Min(6, _enemyAtkStage + 1);
					else _enemySpAtkStage = Math.Min(6, _enemySpAtkStage + 1);
				}
				string statName = boostAtk ? "Ataque" : "Ataque Especial";
				Game1.playSound("heal");
				QueueMessage($"A habilidade Download de {mon.Nickname} ajustou seus sistemas e aumentou seu {statName}!", onDone);
				return;
			}
		}

		// Pressure
		if (string.Equals(ab, "Pressure", StringComparison.OrdinalIgnoreCase))
		{
			QueueMessage($"A tremenda Pressão de {mon.Nickname} envolve o campo de batalha!", onDone);
			return;
		}

		// Unnerve
		if (string.Equals(ab, "Unnerve", StringComparison.OrdinalIgnoreCase))
		{
			QueueMessage($"A presença sinistra de {mon.Nickname} deixou o adversário tenso demais para comer frutas!", onDone);
			return;
		}

		// Electric Surge
		if (string.Equals(ab, "ElectricSurge", StringComparison.OrdinalIgnoreCase))
		{
			Game1.playSound("thunder");
			QueueMessage($"Um Campo Elétrico se espalhou pelo chão de combate por {mon.Nickname}!", onDone);
			return;
		}

		// Grassy Surge
		if (string.Equals(ab, "GrassySurge", StringComparison.OrdinalIgnoreCase))
		{
			Game1.playSound("heal");
			QueueMessage($"Um Campo de Grama viçoso floresceu pelo campo por {mon.Nickname}!", onDone);
			return;
		}

		// Psychic Surge
		if (string.Equals(ab, "PsychicSurge", StringComparison.OrdinalIgnoreCase))
		{
			Game1.playSound("wand");
			QueueMessage($"Um Campo Psíquico ondulou pelo espaço por {mon.Nickname}!", onDone);
			return;
		}

		// Misty Surge
		if (string.Equals(ab, "MistySurge", StringComparison.OrdinalIgnoreCase))
		{
			Game1.playSound("wand");
			QueueMessage($"Uma Névoa Protetora cobriu o chão de batalha por {mon.Nickname}!", onDone);
			return;
		}

		// Ruin abilities
		if (string.Equals(ab, "SwordOfRuin", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(ab, "TabletsOfRuin", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(ab, "VesselOfRuin", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(ab, "BeadsOfRuin", StringComparison.OrdinalIgnoreCase))
		{
			Game1.playSound("shadowHit");
			QueueMessage($"A Ruína ancestral de {mon.Nickname} enfraqueceu as forças no campo!", onDone);
			return;
		}

		onDone?.Invoke();
	}

	private void SetupSwitchButtons()
	{
		_switchButtons.Clear();
		for (int i = 0; i < PlayerTeam.Count; i++)
		{
			var p = PlayerTeam[i];
			int col = i % 2;
			int row = i / 2;
			int bx = _boxX + 25 + col * 320;
			int by = _boxY + 15 + row * 45;
			string status = p.IsFainted ? "[Desmaiado]" : $"{p.CurrentHP}/{p.MaxHP} HP";
			_switchButtons.Add(new ClickableComponent(new Rectangle(bx, by, 300, 40), i.ToString(), $"{p.Nickname} (Lv.{p.Level}) - {status}"));
		}
	}

	private void SwitchPokemon(int targetIndex)
	{
		if (targetIndex == PlayerActiveIndex && PlayerPokemon != null && !PlayerPokemon.IsFainted)
		{
			Game1.playSound("cancel");
			QueueMessage($"{PlayerPokemon.Nickname} já está em batalha!", () => Phase = BattlePhase.SwitchSelect);
			return;
		}

		if (targetIndex < 0 || targetIndex >= PlayerTeam.Count || PlayerTeam[targetIndex].IsFainted)
		{
			Game1.playSound("cancel");
			QueueMessage("Este Pokémon está desmaiado e não pode batalhar!", () => Phase = BattlePhase.SwitchSelect);
			return;
		}

		// Volatile battle state ends when the Pokémon leaves the field.
		if (PlayerPokemon != null)
		{
			PlayerPokemon.Flinched = false;
			PlayerPokemon.SaltCureTurns = 0;
		}

		_isSwitching = true;
		_switchTargetIndex = targetIndex;

		if (PlayerPokemon == null || PlayerPokemon.IsFainted)
		{
			PlayerActiveIndex = _switchTargetIndex;
			PlayerParty.Instance.ActiveIndex = _switchTargetIndex;
			_playerAtkStage = 0; _playerDefStage = 0; _playerSpAtkStage = 0; _playerSpDefStage = 0; _playerSpeedStage = 0;
			PlayerParty.Instance.SaveParty();
			_displayPlayerHP = PlayerPokemon.CurrentHP;
			_playerSpriteScale = 0f;

			_switchStage = SwitchStage.Throw;
			_switchTimer = 0f;
			Game1.playSound("throw");

			QueueMessage($"Vai, {PlayerPokemon.Nickname}!", () =>
			{
				_switchStage = SwitchStage.Appear;
				_switchTimer = 0f;
				Game1.playSound("reward");
				TriggerSwitchInAbility(PlayerPokemon, EnemyPokemon, () =>
				{
					Phase = BattlePhase.ActionSelect;
				});
			});
			return;
		}

		_switchStage = SwitchStage.Retrieve;
		_switchTimer = 0f;
		_playerSpriteScale = 3.2f;
		_playerSpriteTint = Color.White;

		// Authentic recall sound (red laser beam recall) - NO WHISTLE!
		Game1.playSound("wand");

		string oldName = PlayerPokemon.Nickname;

		QueueMessage($"Retorne, {oldName}!", () =>
		{
			PlayerActiveIndex = _switchTargetIndex;
			PlayerParty.Instance.ActiveIndex = _switchTargetIndex;
			_playerAtkStage = 0; _playerDefStage = 0; _playerSpAtkStage = 0; _playerSpDefStage = 0; _playerSpeedStage = 0;
			PlayerParty.Instance.SaveParty();
			_displayPlayerHP = PlayerPokemon.CurrentHP;
			_playerSpriteScale = 0f;

			_switchStage = SwitchStage.Throw;
			_switchTimer = 0f;
			Game1.playSound("throw");

			QueueMessage($"Vai, {PlayerPokemon.Nickname}!", () =>
			{
				_switchStage = SwitchStage.Appear;
				_switchTimer = 0f;
				Game1.playSound("reward");

				TriggerSwitchInAbility(PlayerPokemon, EnemyPokemon, () =>
				{
					PokemonMove enemyMove = SelectEnemyMove();
					ExecuteAttack(EnemyPokemon, PlayerPokemon, enemyMove, () =>
					{
						if (PlayerPokemon.IsFainted) HandlePlayerFainted();
						else RunEndOfTurnEffects(() => Phase = BattlePhase.ActionSelect);
					});
				});
			});
		});
	}

	private void QueuePostAttackMessages(AttackFx fx)
	{
		Action showCrit = () =>
		{
			if (fx.IsCrit)
			{
				QueueMessage("Um acerto crítico!", () => CheckEffectiveness(fx));
			}
			else
			{
				CheckEffectiveness(fx);
			}
		};

		if (fx.Hits > 1)
		{
			QueueMessage($"Acertou {fx.Hits} vezes!", showCrit);
		}
		else
		{
			showCrit();
		}
	}

	/// <summary>
	/// Applies the secondaries that ride on a damaging hit: target/self stat stages,
	/// flinch, Salt Cure, Clear Smog and recoil. Runs before the legacy drain/status
	/// chain so a move can both drop a stat and burn.
	/// </summary>
	private void ApplyDamagingSecondaryEffects(AttackFx fx, Action onDone)
	{
		if (fx?.Move == null)
		{
			onDone?.Invoke();
			return;
		}

		PokemonMove move = fx.Move;
		PokemonInstance attacker = fx.Attacker;
		PokemonInstance defender = fx.Defender;

		// Immunities (and Protect-like zero-damage results) must not hand out
		// stat drops, flinch or Salt Cure.
		if (fx.TypeMult <= 0.05f)
		{
			onDone?.Invoke();
			return;
		}

		var messages = new List<string>();

		if (defender != null && !defender.IsFainted)
		{
			foreach (StatStageChange change in move.TargetStages)
			{
				if (change.Chance < 100 && Game1.random.Next(1, 101) > change.Chance) continue;
				string message = ApplyStageChange(defender, change);
				if (message != null) messages.Add(message);
			}

			if (move.ResetsTargetStages)
			{
				ResetStages(defender);
				messages.Add($"Os atributos de {defender.Nickname} voltaram ao normal!");
			}

			if (move.InvertsTargetStages)
			{
				InvertStages(defender);
				messages.Add($"Os atributos de {defender.Nickname} foram invertidos!");
			}

			if (move.FlinchChance > 0 && Game1.random.Next(1, 101) <= move.FlinchChance)
			{
				defender.Flinched = true;
			}

			if (move.AppliesSaltCure && defender.SaltCureTurns <= 0)
			{
				defender.SaltCureTurns = 99;
				messages.Add($"{defender.Nickname} foi coberto de sal!");
			}
		}

		if (attacker != null && !attacker.IsFainted)
		{
			foreach (StatStageChange change in move.SelfStages)
			{
				if (change.Chance < 100 && Game1.random.Next(1, 101) > change.Chance) continue;
				string message = ApplyStageChange(attacker, change);
				if (message != null) messages.Add(message);
			}

			if (move.KoAttackBoost && defender != null && defender.IsFainted)
			{
				string message = ApplyStageChange(attacker, new StatStageChange(PokemonStat.Attack, 3));
				if (message != null) messages.Add(message);
			}
		}

		if (attacker != null && !attacker.IsFainted && move.RecoilRatio > 0f && fx.Damage > 0
			&& !string.Equals(attacker.Ability, "RockHead", StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(attacker.Ability, "MagicGuard", StringComparison.OrdinalIgnoreCase))
		{
			int recoil = Math.Max(1, (int)(fx.Damage * move.RecoilRatio));
			recoil = Math.Min(recoil, Math.Max(0, attacker.CurrentHP - 1));
			attacker.TakeDamage(recoil);
			messages.Add($"{attacker.Nickname} sofreu {recoil} de dano de recuo!");
		}

		if (messages.Count == 0)
		{
			onDone?.Invoke();
			return;
		}

		Game1.playSound(messages[0].Contains("subiu") ? "reward" : "debuffHit");
		QueueMessageChain(messages, 0, onDone);
	}

	private void CheckEffectiveness(AttackFx fx)
	{
		Action coreEffects = () =>
		{
			PokemonMove m = fx.Move;

			// 1. Check Drain moves (Giga Drain, Absorb, Mega Drain, Drain Punch, Leech Life, Horn Leech, Draining Kiss, Oblivion Wing, Parabolic Charge)
			float drainRatio = (m != null && m.DrainRatio > 0f) ? m.DrainRatio : 0f;
			if (drainRatio <= 0f && (
			    fx.MoveName.Equals("Giga Drain", StringComparison.OrdinalIgnoreCase) ||
			    fx.MoveName.Equals("Absorb", StringComparison.OrdinalIgnoreCase) ||
			    fx.MoveName.Equals("Mega Drain", StringComparison.OrdinalIgnoreCase) ||
			    fx.MoveName.Equals("Drain Punch", StringComparison.OrdinalIgnoreCase) ||
			    fx.MoveName.Equals("Leech Life", StringComparison.OrdinalIgnoreCase) ||
			    fx.MoveName.Equals("Draining Kiss", StringComparison.OrdinalIgnoreCase)))
			{
				drainRatio = 0.5f;
			}

			if (drainRatio > 0f && fx.Damage > 0 && !fx.Attacker.IsFainted)
			{
				int drainHeal = Math.Max(1, (int)(fx.Damage * drainRatio));
				fx.Attacker.Heal(drainHeal);
				Game1.playSound("heal");
				QueueMessage($"{fx.Attacker.Nickname} recuperou {drainHeal} de HP da energia drenada!", fx.OnComplete);
				return;
			}

			// Rocky Helmet check
			if (PokemonHeldItemManager.Matches(fx.Defender.HeldItem, "RockyHelmet") && fx.Move?.Category == MoveCategory.Physical && fx.Damage > 0 && !fx.Attacker.IsFainted)
			{
				int helmDmg = Math.Max(1, fx.Attacker.MaxHP / 6);
				fx.Attacker.TakeDamage(helmDmg);
				Game1.playSound("clubhit");
				QueueMessage($"O Capacete Pedregoso de {fx.Defender.Nickname} feriu {fx.Attacker.Nickname} (-{helmDmg} HP)!");
			}

			// Life Orb recoil check
			if (PokemonHeldItemManager.Matches(fx.Attacker.HeldItem, "LifeOrb") && fx.Damage > 0 && !fx.Attacker.IsFainted && !string.Equals(fx.Attacker.Ability, "MagicGuard", StringComparison.OrdinalIgnoreCase))
			{
				int recoil = Math.Max(1, fx.Attacker.MaxHP / 10);
				fx.Attacker.TakeDamage(recoil);
				Game1.playSound("ow");
				QueueMessage($"{fx.Attacker.Nickname} sofreu recuo da Life Orb (-{recoil} HP)!");
			}

			// Contact / On-Hit Abilities (Static, Flame Body, Poison Point, Rough Skin, Cursed Body)
			if (fx.Move?.Category == MoveCategory.Physical && fx.Damage > 0 && !fx.Attacker.IsFainted)
			{
				if (string.Equals(fx.Defender.Ability, "Static", StringComparison.OrdinalIgnoreCase) && fx.Attacker.Status == PokemonStatusCondition.None && Game1.random.NextDouble() < 0.30)
				{
					if (CanAfflictStatus(fx.Attacker, PokemonStatusCondition.Paralysis, fx.Defender, out _))
					{
						fx.Attacker.Status = PokemonStatusCondition.Paralysis;
						Game1.playSound("thunder");
						QueueMessage($"A Estática de {fx.Defender.Nickname} paralisou {fx.Attacker.Nickname}!");
					}
				}
				else if (string.Equals(fx.Defender.Ability, "FlameBody", StringComparison.OrdinalIgnoreCase) && fx.Attacker.Status == PokemonStatusCondition.None && Game1.random.NextDouble() < 0.30)
				{
					if (CanAfflictStatus(fx.Attacker, PokemonStatusCondition.Burn, fx.Defender, out _))
					{
						fx.Attacker.Status = PokemonStatusCondition.Burn;
						Game1.playSound("fireball");
						QueueMessage($"O Corpo em Chamas de {fx.Defender.Nickname} causou queimaduras em {fx.Attacker.Nickname}!");
					}
				}
				else if (string.Equals(fx.Defender.Ability, "PoisonPoint", StringComparison.OrdinalIgnoreCase) && fx.Attacker.Status == PokemonStatusCondition.None && Game1.random.NextDouble() < 0.30)
				{
					if (CanAfflictStatus(fx.Attacker, PokemonStatusCondition.Poison, fx.Defender, out _))
					{
						fx.Attacker.Status = PokemonStatusCondition.Poison;
						Game1.playSound("debuffHit");
						QueueMessage($"O Ponto Tóxico de {fx.Defender.Nickname} envenenou {fx.Attacker.Nickname}!");
					}
				}

				if (string.Equals(fx.Defender.Ability, "RoughSkin", StringComparison.OrdinalIgnoreCase) || string.Equals(fx.Defender.Ability, "IronBarbs", StringComparison.OrdinalIgnoreCase))
				{
					int skinDmg = Math.Max(1, fx.Attacker.MaxHP / 8);
					fx.Attacker.TakeDamage(skinDmg);
					Game1.playSound("clubhit");
					QueueMessage($"A Pele Áspera de {fx.Defender.Nickname} feriu {fx.Attacker.Nickname} (-{skinDmg} HP)!");
				}
			}

			if (fx.Damage > 0 && !fx.Attacker.IsFainted && string.Equals(fx.Defender.Ability, "CursedBody", StringComparison.OrdinalIgnoreCase) && Game1.random.NextDouble() < 0.30)
			{
				Game1.playSound("parry");
				QueueMessage($"O Corpo Amaldiçoado de {fx.Defender.Nickname} desativou temporariamente o golpe {fx.MoveName}!");
			}

			// Faint triggers (Moxie, Soul-Heart)
			if (fx.Defender.IsFainted && !fx.Attacker.IsFainted)
			{
				if (string.Equals(fx.Attacker.Ability, "Moxie", StringComparison.OrdinalIgnoreCase))
				{
					if (fx.Attacker == PlayerPokemon) _playerAtkStage = Math.Min(6, _playerAtkStage + 1);
					else _enemyAtkStage = Math.Min(6, _enemyAtkStage + 1);
					Game1.playSound("reward");
					QueueMessage($"A Autoconfiança (Moxie) de {fx.Attacker.Nickname} aumentou o seu Ataque!");
				}
				else if (string.Equals(fx.Attacker.Ability, "SoulHeart", StringComparison.OrdinalIgnoreCase))
				{
					if (fx.Attacker == PlayerPokemon) _playerSpAtkStage = Math.Min(6, _playerSpAtkStage + 1);
					else _enemySpAtkStage = Math.Min(6, _enemySpAtkStage + 1);
					Game1.playSound("reward");
					QueueMessage($"O Coração das Almas de {fx.Attacker.Nickname} aumentou o seu Ataque Especial!");
				}
			}

			// 2. Check Secondary Status Condition Infliction from Move definition
			if (!fx.Defender.IsFainted && m != null && m.StatusEffect != PokemonStatusCondition.None && m.StatusChance > 0)
			{
				if (Game1.random.Next(1, 101) <= m.StatusChance && CanAfflictStatus(fx.Defender, m.StatusEffect, fx.Attacker, out string _))
				{
					fx.Defender.Status = m.StatusEffect;
					if (m.StatusEffect == PokemonStatusCondition.Sleep) fx.Defender.StatusTurns = Game1.random.Next(2, 4);
					if (m.StatusEffect == PokemonStatusCondition.Toxic) fx.Defender.StatusTurns = 1;
					Game1.playSound("debuffHit");
					string stMsg = m.StatusEffect switch
					{
						PokemonStatusCondition.Paralysis => $"O {fx.Defender.Nickname} foi paralisado!",
						PokemonStatusCondition.Burn => $"O {fx.Defender.Nickname} sofreu queimaduras!",
						PokemonStatusCondition.Sleep => $"O {fx.Defender.Nickname} adormeceu profundamente!",
						PokemonStatusCondition.Freeze => $"O {fx.Defender.Nickname} foi congelado solidamente!",
						PokemonStatusCondition.Poison => $"O {fx.Defender.Nickname} foi envenenado!",
						PokemonStatusCondition.Toxic => $"O {fx.Defender.Nickname} foi gravemente envenenado!",
						_ => $"O {fx.Defender.Nickname} sofreu alteração de status!"
					};
					QueueMessage(stMsg, fx.OnComplete);
					return;
				}
			}

			// 3. Check Secondary Confusion Infliction from Move definition
			if (!fx.Defender.IsFainted && fx.Defender.ConfusionTurns <= 0 && m != null && m.InflictsConfusion && m.ConfusionChance > 0)
			{
				if (Game1.random.Next(1, 101) <= m.ConfusionChance)
				{
					fx.Defender.ConfusionTurns = Game1.random.Next(2, 5);
					Game1.playSound("debuffHit");
					QueueMessage($"O {fx.Defender.Nickname} ficou confuso!", fx.OnComplete);
					return;
				}
			}

			// 4. Rampage Moves State Tracking (Outrage, Thrash, Petal Dance)
			string mName = fx.MoveName ?? fx.Move?.Name ?? "";
			bool isRampageMove = mName.Equals("Outrage", StringComparison.OrdinalIgnoreCase)
				|| mName.Equals("Thrash", StringComparison.OrdinalIgnoreCase)
				|| mName.Equals("Petal Dance", StringComparison.OrdinalIgnoreCase)
				|| mName.Equals("Raging Fury", StringComparison.OrdinalIgnoreCase);

			if (isRampageMove && !fx.Attacker.IsFainted)
			{
				if (fx.Attacker.RampageTurns <= 0)
				{
					fx.Attacker.RampageTurns = Game1.random.Next(2, 4); // 2 or 3 turns of rampage
					fx.Attacker.RampageMoveName = fx.Move?.Name ?? mName;
				}
				else
				{
					fx.Attacker.RampageTurns--;
					if (fx.Attacker.RampageTurns <= 0)
					{
						fx.Attacker.RampageMoveName = null;
						fx.Attacker.ConfusionTurns = Game1.random.Next(2, 5);
						Game1.playSound("debuffHit");
						QueueMessage($"{fx.Attacker.Nickname} ficou confuso pela fadiga da fúria!", fx.OnComplete);
						return;
					}
				}
			}

			fx.OnComplete?.Invoke();
		};

		Action finishChain = () => ApplyDamagingSecondaryEffects(fx, coreEffects);

		if (fx.TypeMult > 1.1f)
		{
			QueueMessage("Foi super efetivo!", finishChain);
		}
		else if (fx.TypeMult < 0.9f && fx.TypeMult > 0.05f)
		{
			QueueMessage("Não foi muito efetivo...", finishChain);
		}
		else if (fx.TypeMult <= 0.05f)
		{
			QueueMessage("Não surtiu nenhum efeito!", finishChain);
		}
		else
		{
			finishChain();
		}
	}

	public override void update(GameTime time)
	{
		if (IsSpectator)
		{
			base.update(time);
			if (WatchedBattle == null || !ActiveBattles.ContainsValue(WatchedBattle))
			{
				Game1.addHUDMessage(new HUDMessage("A batalha assistida terminou.", 2));
				exitThisMenuNoSound();
			}
			return;
		}

		base.update(time);
		PokemonMusicManager.TickLoop();

		float dt = (float)time.ElapsedGameTime.TotalSeconds;
		UpdateWeatherParticles(dt);

		if (_startupTransitionTimer > 0f)
		{
			_startupTransitionTimer = Math.Max(0f, _startupTransitionTimer - dt);
		}

		_cloudOffset += dt * 15f;

		// Smooth HP Bar Drainage without oscillating
		if (PlayerPokemon != null)
		{
			float diff = _displayPlayerHP - PlayerPokemon.CurrentHP;
			if (Math.Abs(diff) <= 0.6f)
			{
				_displayPlayerHP = PlayerPokemon.CurrentHP;
			}
			else if (diff > 0)
			{
				_displayPlayerHP -= Math.Max(0.5f, diff * 0.12f);
			}
			else
			{
				_displayPlayerHP += Math.Max(0.5f, -diff * 0.12f);
			}
		}

		if (EnemyPokemon != null)
		{
			float diff = _displayEnemyHP - EnemyPokemon.CurrentHP;
			if (Math.Abs(diff) <= 0.6f)
			{
				_displayEnemyHP = EnemyPokemon.CurrentHP;
			}
			else if (diff > 0)
			{
				_displayEnemyHP -= Math.Max(0.5f, diff * 0.12f);
			}
			else
			{
				_displayEnemyHP += Math.Max(0.5f, -diff * 0.12f);
			}
		}

		// Attack FX updates
		if (_activeAttackFx != null)
		{
			_activeAttackFx.Timer += dt;
			if (_activeAttackFx.Timer >= _activeAttackFx.Duration)
			{
				AttackFx fx = _activeAttackFx;
				_activeAttackFx = null;

				// 1. Deal damage to defender
				fx.Defender.TakeDamage(fx.Damage);

				// 2. Shake and floating damage
				if (fx.IsPlayerAttacking)
				{
					_enemyDamageShake = 1.0f;
				}
				else
				{
					_playerDamageShake = 1.0f;
				}

				_floatingDamages.Add(new FloatingDamage
				{
					Text = fx.Damage.ToString(),
					Position = fx.DefenderPos + new Vector2(-20, -30)
				});

				Game1.playSound("hitEnemy");

				// 3. Post-attack messages (Crit and Effectiveness AFTER damage impact!)
				QueuePostAttackMessages(fx);
			}
		}

		// Player Trainer Throw Animation
		if (_isPlayerThrowing)
		{
			_playerThrowTimer += dt;
			int maxTrainerFrames = (_playerTrainerTex != null) ? Math.Max(1, _playerTrainerTex.Height / 64) : 4;
			_playerTrainerFrame = Math.Clamp((int)(_playerThrowTimer / 0.11f), 0, maxTrainerFrames - 1);

			if (_playerThrowTimer >= 0.30f && _playerThrowTimer - dt < 0.30f)
			{
				Game1.playSound("throw");
			}

			if (_playerThrowTimer >= 0.45f)
			{
				_playerTrainerSlide = Math.Min(350f, _playerTrainerSlide + dt * 500f);
				float p = Math.Clamp((_playerThrowTimer - 0.45f) / 0.35f, 0f, 1f);
				_playerSpriteScale = MathHelper.Lerp(0f, 3.2f, p);
				_playerSpriteTint = Color.Lerp(Color.LightCyan * 2f, Color.White, p);
				if (_playerThrowTimer - dt < 0.45f)
				{
					Game1.playSound("reward");
				}
			}

			if (_playerThrowTimer >= 0.85f)
			{
				_isPlayerThrowing = false;
				_playerTrainerSlide = 400f;
				_playerSpriteScale = 3.2f;
				_playerSpriteTint = Color.White;
			}
		}

		// Enemy Trainer Send Animation
		if (_isEnemySending)
		{
			_enemySendTimer += dt;
			_enemyTrainerSlide = Math.Min(350f, _enemyTrainerSlide + dt * 450f);

			if (_enemySendTimer >= 0.25f)
			{
				float p = Math.Clamp((_enemySendTimer - 0.25f) / 0.35f, 0f, 1f);
				_enemyPokemonScale = MathHelper.Lerp(0f, 3.2f, p);
				if (_enemySendTimer - dt < 0.25f)
				{
					Game1.playSound("reward");
				}
			}

			if (_enemySendTimer >= 0.65f)
			{
				_isEnemySending = false;
				_enemyTrainerSlide = 400f;
				_enemyPokemonScale = 3.2f;
			}
		}

		if (_isEnemyRetrieving)
		{
			_enemyRetrieveTimer += dt;
			float p = Math.Clamp(_enemyRetrieveTimer / 0.35f, 0f, 1f);
			_enemyPokemonScale = MathHelper.Lerp(3.2f, 0f, p);
			if (p >= 1f)
			{
				_enemyPokemonScale = 0f;
				_isEnemyRetrieving = false;
			}
		}

		// Switch animation updates
		if (_isSwitching)
		{
			_switchTimer += dt;
			if (_switchStage == SwitchStage.Retrieve)
			{
				float progress = Math.Clamp(_switchTimer / 0.35f, 0f, 1f);
				_playerSpriteScale = MathHelper.Lerp(3.2f, 0.0f, progress);
				_playerSpriteTint = Color.Lerp(Color.White, Color.Red, progress);
				if (progress >= 1f)
				{
					_playerSpriteScale = 0.0f;
				}
			}
			else if (_switchStage == SwitchStage.Throw)
			{
				_playerSpriteScale = 0.0f;
			}
			else if (_switchStage == SwitchStage.Appear)
			{
				float progress = Math.Clamp(_switchTimer / 0.30f, 0f, 1f);
				_playerSpriteScale = MathHelper.Lerp(0.0f, 3.2f, progress);
				_playerSpriteTint = Color.Lerp(Color.LightCyan * 2f, Color.White, progress);
				if (progress >= 1f)
				{
					_isSwitching = false;
					_switchStage = SwitchStage.None;
					_playerSpriteScale = 3.2f;
					_playerSpriteTint = Color.White;
				}
			}
		}

		// Pokeball Capture Animation Updates
		if (_isCapturing)
		{
			_captureTimer += dt;

			int epx = xPositionOnScreen + width - 370;
			int epy = yPositionOnScreen + 175;
			int epw = 290;
			float epCenterX = epx + epw / 2f;
			float epSurfaceY = epy + 18f;

			if (_captureStage == CaptureStage.ThrowArc)
			{
				float t = Math.Clamp(_captureTimer / 0.45f, 0f, 1f);
				Vector2 pStart = new Vector2(xPositionOnScreen + 180, yPositionOnScreen + height - _boxHeight - 120);
				Vector2 pEnd = new Vector2(epCenterX, epSurfaceY - 45);
				float arc = (float)Math.Sin(t * Math.PI) * 110f;
				_captureBallPos = Vector2.Lerp(pStart, pEnd, t) - new Vector2(0, arc);
				_captureBallRot = t * 18f;

				if (t >= 1f)
				{
					_captureStage = CaptureStage.OpenAndAbsorb;
					_captureTimer = 0f;
					Game1.playSound("wand");
				}
			}
			else if (_captureStage == CaptureStage.OpenAndAbsorb)
			{
				float p = Math.Clamp(_captureTimer / 0.35f, 0f, 1f);
				_enemyPokemonScale = MathHelper.Lerp(3.2f, 0f, p);
				_enemyDamageShake = (1f - p) * 3f;

				if (p >= 1f)
				{
					_enemyPokemonScale = 0f;
					_captureStage = CaptureStage.BallDrop;
					_captureTimer = 0f;
				}
			}
			else if (_captureStage == CaptureStage.BallDrop)
			{
				float t = Math.Clamp(_captureTimer / 0.35f, 0f, 1f);
				float startY = epSurfaceY - 45;
				float bounce = (float)Math.Abs(Math.Sin(t * Math.PI * 2.0)) * (1f - t) * 20f;
				_captureBallPos = new Vector2(epCenterX, MathHelper.Lerp(startY, epSurfaceY, t) - bounce);
				_captureBallRot = (1f - t) * 0.4f;

				if (t >= 1f)
				{
					_captureBallPos = new Vector2(epCenterX, epSurfaceY);
					_captureBallRot = 0f;
					Game1.playSound("thudStep");
					_captureStage = CaptureStage.PauseBeforeWiggle;
					_captureTimer = 0f;
				}
			}
			else if (_captureStage == CaptureStage.PauseBeforeWiggle)
			{
				_captureBallRot = 0f;
				if (_captureTimer >= 0.35f)
				{
					if (!_captureResult && _currentWiggleCount >= _allowedWiggles)
					{
						_captureStage = CaptureStage.BreakFree;
						_captureTimer = 0f;
						Game1.playSound("wand");
						Game1.playSound("cancel");
					}
					else if (_currentWiggleCount >= 3)
					{
						_captureStage = CaptureStage.CaughtSuccess;
						_captureTimer = 0f;
						Game1.playSound("reward");
						_captureStars.Clear();
						for (int s = 0; s < 12; s++)
						{
							float ang = (float)(s * (Math.PI * 2.0 / 12.0));
							_captureStars.Add(new Vector2((float)Math.Cos(ang) * 32f, (float)Math.Sin(ang) * 32f));
						}
					}
					else
					{
						_currentWiggleCount++;
						_captureStage = CaptureStage.Wiggle;
						_captureTimer = 0f;
						Game1.playSound("toolSwap");
					}
				}
			}
			else if (_captureStage == CaptureStage.Wiggle)
			{
				float w = Math.Clamp(_captureTimer / 0.30f, 0f, 1f);
				_captureBallRot = (float)Math.Sin(w * Math.PI * 2.0) * 0.42f;

				if (w >= 1f)
				{
					_captureBallRot = 0f;
					_captureStage = CaptureStage.PauseBeforeWiggle;
					_captureTimer = 0f;
				}
			}
			else if (_captureStage == CaptureStage.CaughtSuccess)
			{
				if (_captureTimer >= 0.70f)
				{
					_isCapturing = false;
					_captureStage = CaptureStage.None;
					if (_capturingBallId == "HealBall") EnemyPokemon.FullRestore();
					EnemyPokemon.EndBattleGimmicks(revertMega: true);
					SourceWildSpawn?.Remove();
					if (EnemyPokemon.Species != null)
					{
						Game1.player?.mailReceived?.Add("Poke_Caught_" + EnemyPokemon.Species.InternalName.ToLowerInvariant());
					}
					Game1.playSound("reward");
					PokemonMusicManager.PlayVictoryMusic();
					QueueMessage($"Gotcha! O {EnemyPokemon.Species.Name} selvagem foi capturado com sucesso!", () =>
					{
						PlayerParty.Instance.AddPokemon(EnemyPokemon);
						exitThisMenuNoSound();
					});
				}
			}
			else if (_captureStage == CaptureStage.BreakFree)
			{
				_enemyPokemonScale = Math.Min(3.2f, _enemyPokemonScale + dt * 10f);
				if (_captureTimer >= 0.50f)
				{
					_isCapturing = false;
					_captureStage = CaptureStage.None;
					_enemyPokemonScale = 3.2f;
					QueueMessage($"Oh não! O {EnemyPokemon.Species.Name} selvagem escapou da Poké Bola!", () =>
					{
						PokemonMove enemyMove = SelectEnemyMove();
						ExecuteAttack(EnemyPokemon, PlayerPokemon, enemyMove, () =>
						{
							if (PlayerPokemon.IsFainted) HandlePlayerFainted();
							else Phase = BattlePhase.ActionSelect;
						});
					});
				}
			}
		}

		// Animation Timers & Decay
		if (_playerDamageShake > 0) _playerDamageShake = Math.Max(0, _playerDamageShake - dt * 2.5f);
		if (_enemyDamageShake > 0) _enemyDamageShake = Math.Max(0, _enemyDamageShake - dt * 2.5f);

		_playerLungeOffset = Vector2.Lerp(_playerLungeOffset, Vector2.Zero, 0.15f);
		_enemyLungeOffset = Vector2.Lerp(_enemyLungeOffset, Vector2.Zero, 0.15f);

		// Update floating damage texts
		for (int i = _floatingDamages.Count - 1; i >= 0; i--)
		{
			var fd = _floatingDamages[i];
			fd.Timer += dt;
			fd.Position.Y -= dt * 35f;
			fd.Alpha = Math.Max(0f, 1f - fd.Timer / 1.2f);
			if (fd.Timer >= 1.2f)
			{
				_floatingDamages.RemoveAt(i);
			}
		}

		// Auto-advance dialogue messages like classic Pokemon
		if (Phase == BattlePhase.MessageWait)
		{
			_messageTimer += dt;
			if (_messageTimer >= AutoAdvanceDelay)
			{
				_messageTimer = 0f;
				AdvanceMessageQueue();
			}
		}
		else
		{
			_messageTimer = 0f;
		}
	}

	public override void draw(SpriteBatch b)
	{
		if (IsSpectator)
		{
			if (WatchedBattle != null)
			{
				WatchedBattle.draw(b);
			}
			else
			{
				b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
			}

			// Draw spectator overlay banner
			int bannerW = 440;
			int bannerH = 46;
			int bannerX = (Game1.uiViewport.Width - bannerW) / 2;
			int bannerY = 16;
			IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), bannerX, bannerY, bannerW, bannerH, Color.White, 1f, false);
			string specText = $"Modo Espectador - {TrainerName}";
			Vector2 specSize = Game1.smallFont.MeasureString(specText);
			Utility.drawTextWithShadow(b, specText, Game1.smallFont, new Vector2(bannerX + (bannerW - specSize.X) / 2f, bannerY + 12), Color.Gold);

			// Draw exit button
			if (_spectatorExitBtn != null)
			{
				bool hover = _spectatorExitBtn.containsPoint(Game1.getMouseX(), Game1.getMouseY());
				IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), _spectatorExitBtn.bounds.X, _spectatorExitBtn.bounds.Y, _spectatorExitBtn.bounds.Width, _spectatorExitBtn.bounds.Height, hover ? Color.Wheat : Color.White, 1f, false);
				Vector2 sz = Game1.smallFont.MeasureString(_spectatorExitBtn.label);
				Utility.drawTextWithShadow(b, _spectatorExitBtn.label, Game1.smallFont, new Vector2(_spectatorExitBtn.bounds.X + (_spectatorExitBtn.bounds.Width - sz.X) / 2f, _spectatorExitBtn.bounds.Y + 10), Color.DarkSlateGray);
			}

			drawMouse(b);
			return;
		}

		// 1. Fullscreen Arena strictly filling (0, 0, width, _boxY)
		Rectangle arenaRect = new Rectangle(0, 0, width, _boxY);

		// 2. Draw Location-Themed Battle Arena
		DrawLocationArena(b, arenaRect);

		// 3. Draw Battle Platforms
		DrawBattlePlatforms(b);

		// 4. Draw Animated Pokemon Sprites and Trainers
		DrawEnemyPokemon(b);
		DrawPlayerPokemon(b);

		// 4.5 Draw Active Attack Visual Effects
		if (_activeAttackFx != null)
		{
			DrawAttackFx(b, _activeAttackFx);
		}

		// 4.6 Draw Weather Visual Effects
		DrawWeatherEffects(b, arenaRect);
		DrawWeatherBanner(b);

		// 5. Draw Pokemon Status HUDs
		DrawEnemyStatus(b);
		DrawPlayerStatus(b);

		// 6. Draw Floating Damage Numbers
		foreach (var fd in _floatingDamages)
		{
			DrawCleanText(b, Game1.dialogueFont, $"-{fd.Text}", fd.Position, Color.Red * fd.Alpha);
		}

		// Solid background fill for lower control box to prevent any transparency leaks
		b.Draw(Game1.staminaRect, new Rectangle(_boxX, _boxY, _boxWidth, _boxHeight), new Color(248, 238, 218));

		// 8. Draw Lower Control Box
		IClickableMenu.drawTextureBox(b, _boxX, _boxY, _boxWidth, _boxHeight, Color.White);
		b.Draw(Game1.staminaRect, new Rectangle(0, _boxY - 3, width, 4), new Color(60, 45, 35));

		// 9. Draw Subpanels based on Phase (Spectator always sees clean message panel)
		bool isSpectatorViewing = (Game1.activeClickableMenu is PokemonBattleMenu sm && sm.IsSpectator);
		if (isSpectatorViewing)
		{
			DrawMessagePanel(b);
		}
		else
		{
			switch (Phase)
			{
				case BattlePhase.ActionSelect:
					DrawActionSelectPanel(b);
					break;
				case BattlePhase.MoveSelect:
					DrawMoveSelectPanel(b);
					break;
				case BattlePhase.BagSelect:
					DrawBagSelectPanel(b);
					break;
				case BattlePhase.SwitchSelect:
					DrawSwitchSelectPanel(b);
					break;
				default:
					DrawMessagePanel(b);
					break;
			}
		}

		if (_startupTransitionTimer > 0f)
		{
			// Dynamic battle startup opening shutters / transition bars
			float p = _startupTransitionTimer / 0.70f; // 1.0 down to 0.0
			int screenW = Math.Max(Game1.uiViewport.Width, Game1.graphics.GraphicsDevice.Viewport.Width);
			int screenH = Math.Max(Game1.uiViewport.Height, Game1.graphics.GraphicsDevice.Viewport.Height);
			int barCount = 10;
			int barHeight = (screenH / barCount) + 2;
			for (int i = 0; i < barCount; i++)
			{
				int barY = i * barHeight;
				// Alternating direction
				bool fromLeft = (i % 2 == 0);
				int barOffset = (int)((1f - p) * screenW * (fromLeft ? -1f : 1f));
				Rectangle barRect = new Rectangle(barOffset, barY, screenW, barHeight);
				b.Draw(Game1.staminaRect, barRect, Color.Black);
			}

			// Flash burst at beginning of transition
			if (p > 0.75f)
			{
				float flashAlpha = (p - 0.75f) / 0.25f;
				b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, screenW, screenH), Color.White * flashAlpha * 0.85f);
			}
		}

		// 10. Draw Hardware Mouse Cursor
		drawMouse(b);
	}

	private void DrawLocationArena(SpriteBatch b, Rectangle arenaRect)
	{
		Color skyTop;
		Color skyBottom;
		Color hillColor;
		Color groundColor;

		switch (_currentTheme)
		{
			case ArenaTheme.Cave:
				skyTop = new Color(15, 12, 22);
				skyBottom = new Color(30, 25, 40);
				hillColor = new Color(20, 18, 28);
				groundColor = new Color(42, 38, 48);
				break;
			case ArenaTheme.Beach:
				skyTop = new Color(70, 160, 240);
				skyBottom = new Color(170, 225, 255);
				hillColor = new Color(30, 100, 170);
				groundColor = new Color(235, 210, 165);
				break;
			case ArenaTheme.Forest:
				skyTop = new Color(45, 95, 60);
				skyBottom = new Color(110, 165, 120);
				hillColor = new Color(25, 65, 35);
				groundColor = new Color(38, 78, 42);
				break;
			case ArenaTheme.Town:
				skyTop = new Color(120, 150, 190);
				skyBottom = new Color(185, 205, 225);
				hillColor = new Color(100, 110, 125);
				groundColor = new Color(145, 138, 130);
				break;
			case ArenaTheme.Interior:
				skyTop = new Color(130, 90, 60);
				skyBottom = new Color(175, 125, 85);
				hillColor = new Color(105, 70, 45);
				groundColor = new Color(115, 75, 45);
				break;
			case ArenaTheme.Farm:
			default:
				skyTop = new Color(100, 175, 240);
				skyBottom = new Color(190, 230, 255);
				hillColor = new Color(75, 145, 65);
				groundColor = new Color(95, 170, 75);
				break;
		}

		// Sky Gradient strictly inside arenaRect
		int skyHeight = arenaRect.Height * 3 / 5;
		int halfSky = skyHeight / 2;
		b.Draw(Game1.staminaRect, new Rectangle(arenaRect.X, arenaRect.Y, arenaRect.Width, halfSky), skyTop);
		b.Draw(Game1.staminaRect, new Rectangle(arenaRect.X, arenaRect.Y + halfSky, arenaRect.Width, skyHeight - halfSky), skyBottom);

		// Distant Sun / Moon (Soft glowing orb with subtle halo)
		int celestialCenterX = arenaRect.X + arenaRect.Width - 100;
		int celestialCenterY = arenaRect.Y + 45;
		if (_currentTheme != ArenaTheme.Cave && _currentTheme != ArenaTheme.Interior)
		{
			// Outer soft halo
			PokemonUI.SoftShadow(b, new Rectangle(celestialCenterX - 30, celestialCenterY - 30, 60, 60), 0.20f);
			// Inner orb
			Color sunColor = (_currentTheme == ArenaTheme.Beach) ? new Color(255, 250, 205) : new Color(255, 245, 180);
			PokemonUI.SoftShadow(b, new Rectangle(celestialCenterX - 18, celestialCenterY - 18, 36, 36), 0.45f);
			b.Draw(Game1.staminaRect, new Rectangle(celestialCenterX - 10, celestialCenterY - 10, 20, 20), sunColor * 0.85f);
		}

		// Drifting Clouds strictly clamped within arenaRect
		if (_currentTheme != ArenaTheme.Cave && _currentTheme != ArenaTheme.Interior && _cloudsTex != null)
		{
			int cloudW = _cloudsTex.Width;
			int totalClouds = (arenaRect.Width / cloudW) + 2;
			for (int c = 0; c < totalClouds; c++)
			{
				int cx = arenaRect.X + ((int)(c * cloudW - (_cloudOffset % (totalClouds * cloudW)) + (totalClouds * cloudW)) % (totalClouds * cloudW)) - cloudW / 2;
				int visibleLeft = Math.Max(cx, arenaRect.X);
				int visibleRight = Math.Min(cx + cloudW, arenaRect.Right);
				if (visibleRight > visibleLeft)
				{
					int srcX = visibleLeft - cx;
					int srcW = visibleRight - visibleLeft;
					Rectangle src = new Rectangle(srcX, 0, srcW, _cloudsTex.Height);
					Rectangle dst = new Rectangle(visibleLeft, arenaRect.Y + 15, srcW, _cloudsTex.Height);
					b.Draw(_cloudsTex, dst, src, Color.White * 0.35f);
				}
			}
		}

		// Distant Hills / Tree line Silhouette strictly clamped within arenaRect
		int hillHeight = 45;
		int hillY = arenaRect.Y + skyHeight - hillHeight;
		if (_treeStrip != null && _currentTheme != ArenaTheme.Cave && _currentTheme != ArenaTheme.Interior)
		{
			for (int x = arenaRect.X; x < arenaRect.Right; x += _treeStrip.Width)
			{
				int drawW = Math.Min(_treeStrip.Width, arenaRect.Right - x);
				Rectangle src = new Rectangle(0, 0, drawW, _treeStrip.Height);
				Rectangle dst = new Rectangle(x, hillY, drawW, _treeStrip.Height);
				b.Draw(_treeStrip, dst, src, hillColor);
			}
		}
		else
		{
			b.Draw(Game1.staminaRect, new Rectangle(arenaRect.X, hillY, arenaRect.Width, hillHeight), hillColor);
		}

		// Bottom Terrain / Ground strictly inside arenaRect
		int groundHeight = arenaRect.Height - skyHeight;
		b.Draw(Game1.staminaRect, new Rectangle(arenaRect.X, arenaRect.Y + skyHeight, arenaRect.Width, groundHeight), groundColor);

		// Ground Highlight Line
		b.Draw(Game1.staminaRect, new Rectangle(arenaRect.X, arenaRect.Y + skyHeight, arenaRect.Width, 2), Color.White * 0.25f);

		// Arena Outer Border
		DrawBorder(b, arenaRect, 3, Color.SaddleBrown);
	}

	private static void DrawBorder(SpriteBatch b, Rectangle r, int thickness, Color color)
	{
		b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y, r.Width, thickness), color);
		b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
		b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y, thickness, r.Height), color);
		b.Draw(Game1.staminaRect, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
	}

	private void DrawBattlePlatforms(SpriteBatch b)
	{
		Color topPlatColor = _currentTheme switch
		{
			ArenaTheme.Cave => new Color(65, 60, 75),
			ArenaTheme.Beach => new Color(225, 205, 155),
			ArenaTheme.Forest => new Color(55, 120, 60),
			ArenaTheme.Town => new Color(135, 130, 125),
			ArenaTheme.Interior => new Color(150, 105, 70),
			_ => new Color(90, 175, 75)
		};
		Color rimColor = new Color(topPlatColor.R * 6 / 10, topPlatColor.G * 6 / 10, topPlatColor.B * 6 / 10);
		Color highlightColor = new Color(Math.Min(255, topPlatColor.R + 40), Math.Min(255, topPlatColor.G + 40), Math.Min(255, topPlatColor.B + 40));

		// 1. Enemy Platform (Upper Right)
		int epx = xPositionOnScreen + width - 370;
		int epy = yPositionOnScreen + 175;
		int epw = 290;
		int eph = 48;

		// Drop Shadow
		b.Draw(Game1.staminaRect, new Rectangle(epx + 8, epy + 10, epw, eph), Color.Black * 0.35f);
		// Platform 3D Base / Rim
		b.Draw(Game1.staminaRect, new Rectangle(epx, epy + 12, epw, eph - 12), rimColor);
		// Platform Surface
		b.Draw(Game1.staminaRect, new Rectangle(epx + 4, epy, epw - 8, eph - 12), topPlatColor);
		// Platform Top Highlight
		b.Draw(Game1.staminaRect, new Rectangle(epx + 8, epy + 2, epw - 16, 4), highlightColor * 0.6f);
		DrawBorder(b, new Rectangle(epx, epy, epw, eph), 2, rimColor * 0.8f);

		// 2. Player Platform (Lower Left)
		int ppx = xPositionOnScreen + 60;
		int ppy = yPositionOnScreen + height - _boxHeight - 120;
		int ppw = 350;
		int pph = 60;

		// Drop Shadow
		b.Draw(Game1.staminaRect, new Rectangle(ppx + 10, ppy + 12, ppw, pph), Color.Black * 0.35f);
		// Platform 3D Base / Rim
		b.Draw(Game1.staminaRect, new Rectangle(ppx, ppy + 16, ppw, pph - 16), rimColor);
		// Platform Surface
		b.Draw(Game1.staminaRect, new Rectangle(ppx + 6, ppy, ppw - 12, pph - 16), topPlatColor);
		// Platform Top Highlight
		b.Draw(Game1.staminaRect, new Rectangle(ppx + 12, ppy + 3, ppw - 24, 5), highlightColor * 0.6f);
		DrawBorder(b, new Rectangle(ppx, ppy, ppw, pph), 2, rimColor * 0.8f);
	}

	private static float GetBattleSpriteYOffset(PokemonInstance mon, bool isPlayer)
	{
		if (mon?.Species == null) return 0f;
		string name = mon.Species.Name;

		if (isPlayer)
		{
			return name switch
			{
				"Gyarados" or "Gyarados-Mega" => -18f,
				"Onix" or "Steelix" or "Steelix-Mega" => -16f,
				"Rayquaza" or "Rayquaza-Mega" => -24f,
				"Charizard" or "Charizard-Mega-X" or "Charizard-Mega-Y" => -12f,
				"Pidgeot" or "Pidgeot-Mega" => -10f,
				"Aerodactyl" or "Aerodactyl-Mega" => -12f,
				"Dragonite" => -8f,
				"Lugia" or "Ho-Oh" => -20f,
				"Zapdos" or "Moltres" or "Articuno" => -14f,
				"Dialga" or "Palkia" or "Giratina" => -16f,
				"Reshiram" or "Zekrom" or "Kyurem" => -14f,
				"Wailord" => -22f,
				"Gengar" or "Gengar-Mega" => -6f,
				"Gastly" or "Haunter" => -12f,
				"Latios" or "Latias" => -14f,
				"Corviknight" or "Talonflame" => -12f,
				"Salamence" or "Salamence-Mega" => -14f,
				"Volcarona" => -10f,
				"Yveltal" or "Xerneas" => -16f,
				"Lunala" or "Solgaleo" => -16f,
				"Eternatus" => -24f,
				"Koraidon" or "Miraidon" => -12f,
				"Diglett" or "Dugtrio" => 6f,
				_ => 0f
			};
		}
		else
		{
			return name switch
			{
				"Gyarados" or "Gyarados-Mega" => -16f,
				"Onix" or "Steelix" or "Steelix-Mega" => -14f,
				"Rayquaza" or "Rayquaza-Mega" => -22f,
				"Charizard" or "Charizard-Mega-X" or "Charizard-Mega-Y" => -10f,
				"Pidgeot" or "Pidgeot-Mega" => -8f,
				"Aerodactyl" or "Aerodactyl-Mega" => -10f,
				"Dragonite" => -6f,
				"Lugia" or "Ho-Oh" => -18f,
				"Zapdos" or "Moltres" or "Articuno" => -12f,
				"Dialga" or "Palkia" or "Giratina" => -14f,
				"Reshiram" or "Zekrom" or "Kyurem" => -12f,
				"Wailord" => -20f,
				"Gastly" or "Haunter" => -10f,
				"Latios" or "Latias" => -12f,
				"Corviknight" or "Talonflame" => -10f,
				"Salamence" or "Salamence-Mega" => -12f,
				"Volcarona" => -8f,
				"Yveltal" or "Xerneas" => -14f,
				"Lunala" or "Solgaleo" => -14f,
				"Eternatus" => -20f,
				"Koraidon" or "Miraidon" => -10f,
				"Diglett" or "Dugtrio" => 4f,
				_ => 0f
			};
		}
	}

	private void DrawEnemyPokemon(SpriteBatch b)
	{
		if (EnemyPokemon == null) return;

		// 1. Enemy Trainer intro slide
		if (!IsWild && _enemyTrainerSlide < 350f && _enemyTrainerTex != null)
		{
			Vector2 trPos = new Vector2(xPositionOnScreen + width - 260 + _enemyTrainerSlide, yPositionOnScreen + 65);
			bool isPortrait = _enemyTrainerTex.Width == 64 || _enemyTrainerTex.Width == 128;
			Rectangle trSrc = new Rectangle(0, 0, Math.Min(64, _enemyTrainerTex.Width), Math.Min(64, _enemyTrainerTex.Height));
			if (isPortrait)
			{
				int emotion = 0;
				if (EnemyPokemon != null && (EnemyPokemon.IsFainted || _enemyDamageShake > 0)) emotion = 3;
				else if (Phase == BattlePhase.Defeat) emotion = 1;
				else if (Phase == BattlePhase.Victory) emotion = 2;
				int row = emotion / 2;
				int col = emotion % 2;
				if (row * 64 < _enemyTrainerTex.Height && col * 64 < _enemyTrainerTex.Width)
				{
					trSrc = new Rectangle(col * 64, row * 64, 64, 64);
				}
				IClickableMenu.drawTextureBox(b, (int)trPos.X - 6, (int)trPos.Y - 6, (int)(64 * 2.2f) + 12, (int)(64 * 2.2f) + 12, Color.White);
				b.Draw(_enemyTrainerTex, trPos, trSrc, Color.White, 0f, Vector2.Zero, 2.2f, SpriteEffects.None, 0.84f);
			}
			else
			{
				b.Draw(Game1.staminaRect, new Rectangle((int)trPos.X + 20, yPositionOnScreen + 215, 120, 14), Color.Black * 0.3f);
				b.Draw(_enemyTrainerTex, trPos, trSrc, Color.White, 0f, Vector2.Zero, 2.8f, SpriteEffects.None, 0.84f);
			}
		}

		// 2. Pokéball arc during Enemy Trainer send
		if (_isEnemySending && _enemySendTimer < 0.35f && _ballTex != null)
		{
			float tBall = Math.Clamp(_enemySendTimer / 0.35f, 0f, 1f);
			Vector2 pStart = new Vector2(xPositionOnScreen + width - 150, yPositionOnScreen + 90);
			Vector2 pEnd = new Vector2(xPositionOnScreen + width - 230, yPositionOnScreen + 160);
			float arcHeight = (float)Math.Sin(tBall * Math.PI) * 45f;
			Vector2 ballPos = Vector2.Lerp(pStart, pEnd, tBall) - new Vector2(0, arcHeight);
			b.Draw(_ballTex, ballPos, new Rectangle(0, 0, 16, 16), Color.White, tBall * 14f, new Vector2(8f, 8f), 2.2f, SpriteEffects.None, 0.88f);
		}

		// 2b. Flash on Enemy Pokéball opening
		if (_isEnemySending && _enemySendTimer >= 0.25f && _enemySendTimer < 0.45f)
		{
			float flashProgress = (_enemySendTimer - 0.25f) / 0.20f;
			float flashRadius = MathHelper.Lerp(10f, 65f, flashProgress);
			Color flashColor = Color.White * (1f - flashProgress);
			Vector2 center = new Vector2(xPositionOnScreen + width - 230, yPositionOnScreen + 160);
			b.Draw(Game1.staminaRect, new Rectangle((int)(center.X - flashRadius), (int)(center.Y - flashRadius), (int)(flashRadius * 2), (int)(flashRadius * 2)), flashColor);
		}

		// 2c. Red Laser Beam on Enemy Retrieve
		if (_isEnemyRetrieving)
		{
			Vector2 beamStart = new Vector2(xPositionOnScreen + width - 150, yPositionOnScreen + 90);
			Vector2 beamEnd = new Vector2(xPositionOnScreen + width - 225, yPositionOnScreen + 175);
			DrawLine(b, beamStart, beamEnd, Color.Red * 0.85f, 4);
			DrawLine(b, beamStart, beamEnd, Color.White * 0.95f, 2);
		}

		// 3. Enemy Pokémon Sprite - strictly grounded via lowest opaque row (GetFrontBottomY)
		if (_enemyPokemonScale > 0.05f)
		{
			Texture2D sprite = EnemyPokemon.GetFrontSprite(Game1.graphics.GraphicsDevice);
			if (sprite != null)
			{
				Rectangle srcRect = EnemyPokemon.GetFrontSourceRect();
				int bottomY = EnemyPokemon.GetFrontBottomY(Game1.graphics.GraphicsDevice);
				float idleBounce = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 4.0) * 2.5f;
				float shakeX = (_enemyDamageShake > 0) ? (float)Math.Sin(_enemyDamageShake * 30.0) * 8f : 0f;
				Color tint = (_enemyDamageShake > 0) ? Color.Red : (EnemyPokemon.IsDynamaxed ? new Color(255, 210, 225) : Color.White);

				int epx = xPositionOnScreen + width - 370;
				int epy = yPositionOnScreen + 175;
				int epw = 290;
				float epCenterX = epx + epw / 2f;
				float epSurfaceY = epy + 20f;
				float yOffset = GetBattleSpriteYOffset(EnemyPokemon, isPlayer: false);

				Vector2 basePos = new Vector2(epCenterX + shakeX, epSurfaceY + idleBounce + yOffset) + _enemyLungeOffset;

				// Ground soft shadow
				PokemonUI.SoftShadow(b, new Rectangle((int)epCenterX - 55, (int)epSurfaceY - 8, 110, 16), 0.35f);

				float scale = 2.75f * (_enemyPokemonScale / 3.2f) * (EnemyPokemon.IsDynamaxed ? 1.28f : 1.0f);
				Vector2 origin = new Vector2(srcRect.Width / 2f, bottomY + 1);
				b.Draw(sprite, basePos, srcRect, tint, 0f, origin, scale, SpriteEffects.None, 0.84f);
			}
		}

		// 4. Capture Pokéball Animation Drawing
		if (_isCapturing)
		{
			int epx = xPositionOnScreen + width - 370;
			int epy = yPositionOnScreen + 175;
			int epw = 290;
			float epCenterX = epx + epw / 2f;
			float epSurfaceY = epy + 18f;

			if (_captureStage == CaptureStage.OpenAndAbsorb)
			{
				Vector2 beamStart = _captureBallPos;
				Vector2 beamEnd = new Vector2(epCenterX, epSurfaceY - 40);
				DrawLine(b, beamStart, beamEnd, Color.Red * 0.9f, 4);
				DrawLine(b, beamStart, beamEnd, Color.White * 0.95f, 2);

				Texture2D openTex = _ballOpenTex ?? _activeCaptureBallTex ?? _ballTex;
				if (openTex != null)
				{
					b.Draw(openTex, _captureBallPos, new Rectangle(0, 0, openTex.Width, openTex.Height), Color.White, 0f, new Vector2(openTex.Width / 2f, openTex.Height / 2f), 2.5f, SpriteEffects.None, 0.95f);
				}
			}
			else if (_captureStage == CaptureStage.ThrowArc)
			{
				Texture2D ball = _activeCaptureBallTex ?? _ballTex;
				if (ball != null)
				{
					b.Draw(ball, _captureBallPos, new Rectangle(0, 0, 16, 16), Color.White, _captureBallRot, new Vector2(8f, 8f), 2.4f, SpriteEffects.None, 0.95f);
				}
			}
			else if (_captureStage == CaptureStage.BallDrop || _captureStage == CaptureStage.PauseBeforeWiggle || _captureStage == CaptureStage.Wiggle || _captureStage == CaptureStage.CaughtSuccess)
			{
				PokemonUI.SoftShadow(b, (int)_captureBallPos.X, (int)_captureBallPos.Y + 2, 32, 10, 0.40f);

				Texture2D ball = _activeCaptureBallTex ?? _ballTex;
				if (ball != null)
				{
					Color tint = (_captureStage == CaptureStage.CaughtSuccess) ? new Color(200, 200, 200) : Color.White;
					b.Draw(ball, _captureBallPos, new Rectangle(0, 0, 16, 16), tint, _captureBallRot, new Vector2(8f, 15f), 2.4f, SpriteEffects.None, 0.95f);
				}

				if (_captureStage == CaptureStage.CaughtSuccess)
				{
					for (int i = 0; i < _captureStars.Count; i++)
					{
						Vector2 starPos = _captureBallPos + _captureStars[i] * (_captureTimer / 0.70f);
						b.Draw(Game1.staminaRect, new Rectangle((int)starPos.X - 3, (int)starPos.Y - 3, 6, 6), Color.Gold * 0.9f);
					}
				}
			}
			else if (_captureStage == CaptureStage.BreakFree)
			{
				float p = Math.Clamp(_captureTimer / 0.50f, 0f, 1f);
				float radius = MathHelper.Lerp(10f, 70f, p);
				Color flash = Color.White * (1f - p);
				b.Draw(Game1.staminaRect, new Rectangle((int)(_captureBallPos.X - radius), (int)(_captureBallPos.Y - radius), (int)(radius * 2), (int)(radius * 2)), flash);
			}
		}
	}

	private void DrawPlayerPokemon(SpriteBatch b)
	{
		if (PlayerPokemon == null) return;

		// 1. Draw Player Trainer intro back pic (Brendan 4-frame / Red 5-frame throw animation)
		if (_playerTrainerSlide < 300f && _playerTrainerTex != null)
		{
			int maxFrames = Math.Max(1, _playerTrainerTex.Height / 64);
			int frameIdx = Math.Clamp(_playerTrainerFrame, 0, maxFrames - 1);
			float scale = 3.0f;
			float rawX = xPositionOnScreen + 100 - _playerTrainerSlide;
			float rawY = yPositionOnScreen + height - _boxHeight - 190;
			int arenaLeft = xPositionOnScreen + 21;

			int clipLeftPx = 0;
			if (rawX < arenaLeft)
			{
				clipLeftPx = (int)Math.Ceiling((arenaLeft - rawX) / scale);
			}

			if (clipLeftPx < 64)
			{
				Vector2 trPos = new Vector2(rawX + clipLeftPx * scale, rawY);
				Rectangle trSrc = new Rectangle(clipLeftPx, frameIdx * 64, 64 - clipLeftPx, 64);
				float alpha = Math.Clamp(1f - (_playerTrainerSlide / 260f), 0f, 1f);
				b.Draw(_playerTrainerTex, trPos, trSrc, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
			}
		}

		// 2. Pokéball arc during Player Trainer throw intro
		if (_isPlayerThrowing && _playerThrowTimer >= 0.25f && _playerThrowTimer < 0.55f)
		{
			float tBall = (_playerThrowTimer - 0.25f) / 0.30f;
			Vector2 pStart = new Vector2(xPositionOnScreen + 180 - _playerTrainerSlide, yPositionOnScreen + height - _boxHeight - 120);
			Vector2 pEnd = new Vector2(xPositionOnScreen + 220, yPositionOnScreen + height - _boxHeight - 95);
			float arcHeight = (float)Math.Sin(tBall * Math.PI) * 80f;
			Vector2 ballPos = Vector2.Lerp(pStart, pEnd, tBall) - new Vector2(0, arcHeight);
			if (_ballTex != null)
			{
				b.Draw(_ballTex, ballPos, new Rectangle(0, 0, 16, 16), Color.White, tBall * 16f, new Vector2(8f, 8f), 2.2f, SpriteEffects.None, 0.90f);
			}
		}

		// 3. Flash on Player Trainer throw ball opening
		if (_isPlayerThrowing && _playerThrowTimer >= 0.45f && _playerThrowTimer < 0.70f)
		{
			float flashProgress = (_playerThrowTimer - 0.45f) / 0.25f;
			float flashRadius = MathHelper.Lerp(10f, 75f, flashProgress);
			Color flashColor = Color.White * (1f - flashProgress);
			Vector2 center = new Vector2(xPositionOnScreen + 220, yPositionOnScreen + height - _boxHeight - 95);
			b.Draw(Game1.staminaRect, new Rectangle((int)(center.X - flashRadius), (int)(center.Y - flashRadius), (int)(flashRadius * 2), (int)(flashRadius * 2)), flashColor);
		}

		// 4. Player Pokemon Sprite (STRICTLY GROUNDED ON TOP OF PLATFORM SURFACE VIA LOWEST OPAQUE ROW!)
		Texture2D backSprite = PlayerPokemon.GetBackSprite(Game1.graphics.GraphicsDevice);
		Texture2D sprite = backSprite ?? PlayerPokemon.GetFrontSprite(Game1.graphics.GraphicsDevice);
		if (sprite != null && _playerSpriteScale > 0.05f)
		{
			Rectangle srcRect = backSprite != null ? PlayerPokemon.GetBackSourceRect() : PlayerPokemon.GetFrontSourceRect();
			int bottomY = backSprite != null
				? PlayerPokemon.GetBackBottomY(Game1.graphics.GraphicsDevice)
				: PlayerPokemon.GetFrontBottomY(Game1.graphics.GraphicsDevice);

			float shakeX = (_playerDamageShake > 0) ? (float)Math.Sin(_playerDamageShake * 30.0) * 8f : 0f;
			Color tint = (_playerDamageShake > 0)
				? Color.Red
				: (PlayerPokemon.IsDynamaxed ? new Color(255, 205, 225) : _playerSpriteTint);

			int ppx = xPositionOnScreen + 60;
			int ppy = yPositionOnScreen + height - _boxHeight - 120;
			int ppw = 350;
			float ppCenterX = ppx + ppw / 2f;
			float ppSurfaceY = ppy + 22f;

			float yOffset = GetBattleSpriteYOffset(PlayerPokemon, isPlayer: true);

			// Ground position anchored strictly on platform surface with per-species vertical balance
			Vector2 groundPos = new Vector2(ppCenterX + shakeX, Math.Min(ppSurfaceY, _boxY - 46f) + yOffset) + _playerLungeOffset;

			// Ground shadow on platform surface
			PokemonUI.SoftShadow(b, new Rectangle((int)ppCenterX - 65, (int)ppSurfaceY - 10, 130, 16), 0.35f);

			float dynScale = PlayerPokemon.IsDynamaxed ? 1.26f : 1.0f;
			float baseScale = (_playerSpriteScale * 0.90f) * dynScale;

			// Draw Dynamax swirling crimson/magenta clouds above & around the Pokemon
			if (PlayerPokemon.IsDynamaxed)
			{
				float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 5.0) * 6f;
				int cloudY = (int)(groundPos.Y - (bottomY * baseScale) - 18f);
				b.Draw(Game1.staminaRect, new Rectangle((int)(ppCenterX - 65 - pulse), cloudY, (int)(130 + pulse * 2), 10), new Color(220, 20, 95) * 0.75f);
				b.Draw(Game1.staminaRect, new Rectangle((int)(ppCenterX - 45), cloudY - 8, 90, 8), new Color(255, 80, 150) * 0.65f);
			}

			// Bottom-anchored breathing animation: scale Y from bottom opaque row up so feet stay glued to platform surface
			float breathe = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 3.5) * 0.02f;
			Vector2 origin = new Vector2(srcRect.Width / 2f, bottomY + 1);
			Vector2 scaleVec = new Vector2(baseScale * (1f + breathe), baseScale * (1f - breathe));

			b.Draw(sprite, groundPos, srcRect, tint, 0f, origin, scaleVec, SpriteEffects.None, 0.85f);
		}

		// 5. Switching effects: Retrieve Red Laser Beam
		if (_isSwitching && _switchStage == SwitchStage.Retrieve)
		{
			Vector2 beamStart = new Vector2(xPositionOnScreen + 60, yPositionOnScreen + height - _boxHeight - 20);
			Vector2 beamEnd = new Vector2(xPositionOnScreen + 200, yPositionOnScreen + height - _boxHeight - 140);
			DrawLine(b, beamStart, beamEnd, Color.Red * 0.85f, 4);
			DrawLine(b, beamStart, beamEnd, Color.White * 0.95f, 2);
		}

		// 6. Switching effects: Pokéball Throw Arc
		if (_isSwitching && _switchStage == SwitchStage.Throw)
		{
			float tBall = Math.Clamp(_switchTimer / 0.35f, 0f, 1f);
			Vector2 pStart = new Vector2(xPositionOnScreen - 20, yPositionOnScreen + height - _boxHeight);
			Vector2 pEnd = new Vector2(xPositionOnScreen + 200, yPositionOnScreen + height - _boxHeight - 95);
			float arcHeight = (float)Math.Sin(tBall * Math.PI) * 90f;
			Vector2 ballPos = Vector2.Lerp(pStart, pEnd, tBall) - new Vector2(0, arcHeight);
			float rot = tBall * 18f;

			if (_ballTex != null)
			{
				b.Draw(_ballTex, ballPos, new Rectangle(0, 0, 16, 16), Color.White, rot, new Vector2(8f, 8f), 2.2f, SpriteEffects.None, 0.90f);
			}
			else
			{
				b.Draw(Game1.staminaRect, new Rectangle((int)ballPos.X - 10, (int)ballPos.Y - 10, 20, 20), Color.Red);
			}
		}

		// 7. Switching effects: Flash on ball opening
		if (_isSwitching && _switchStage == SwitchStage.Appear && _switchTimer < 0.20f)
		{
			float flashProgress = _switchTimer / 0.20f;
			float flashRadius = MathHelper.Lerp(10f, 75f, flashProgress);
			Color flashColor = Color.White * (1f - flashProgress);
			Vector2 center = new Vector2(xPositionOnScreen + 200, yPositionOnScreen + height - _boxHeight - 95);
			b.Draw(Game1.staminaRect, new Rectangle((int)(center.X - flashRadius), (int)(center.Y - flashRadius), (int)(flashRadius * 2), (int)(flashRadius * 2)), flashColor);
		}
	}

	private void DrawAttackFx(SpriteBatch b, AttackFx fx)
	{
		float t = Math.Clamp(fx.Timer / fx.Duration, 0f, 1f);
		string mName = fx.MoveName ?? fx.Move?.Name ?? "";

		// 1. Max Moves / G-Max Moves (Colossal elemental pillar descending from the sky)
		if (mName.StartsWith("Max ", StringComparison.OrdinalIgnoreCase) || mName.StartsWith("G-Max", StringComparison.OrdinalIgnoreCase))
		{
			float pillarProgress = Math.Clamp(t * 1.4f, 0f, 1f);
			int beamWidth = (int)MathHelper.Lerp(20f, 120f, (float)Math.Sin(pillarProgress * Math.PI));
			Color beamColor = fx.Type switch
			{
				PokemonType.Fire => Color.OrangeRed,
				PokemonType.Water => Color.DeepSkyBlue,
				PokemonType.Electric => Color.Gold,
				PokemonType.Grass => Color.LimeGreen,
				PokemonType.Dragon => Color.MediumPurple,
				PokemonType.Dark => Color.DarkSlateBlue,
				_ => Color.Crimson
			};

			int bx = (int)(fx.DefenderPos.X - beamWidth / 2);
			int by = (int)(fx.DefenderPos.Y - 260);
			int bh = 320;
			b.Draw(Game1.staminaRect, new Rectangle(bx, by, beamWidth, bh), beamColor * 0.75f);
			b.Draw(Game1.staminaRect, new Rectangle(bx + beamWidth / 4, by, beamWidth / 2, bh), Color.White * 0.90f);

			// Ground shockwave
			if (t > 0.40f)
			{
				float shockT = (t - 0.40f) / 0.60f;
				int shockR = (int)MathHelper.Lerp(20f, 140f, shockT);
				b.Draw(Game1.staminaRect, new Rectangle((int)fx.DefenderPos.X - shockR, (int)fx.DefenderPos.Y - 10, shockR * 2, 20), beamColor * (1f - shockT));
			}
			return;
		}

		// 2. Outrage / Thrash / Rampage Dragon Fury
		if (mName.Equals("Outrage", StringComparison.OrdinalIgnoreCase) || mName.Equals("Thrash", StringComparison.OrdinalIgnoreCase))
		{
			// Pulsing dragon aura at attacker
			float auraSize = 35f + (float)Math.Sin(t * 20f) * 12f;
			b.Draw(Game1.staminaRect, new Rectangle((int)(fx.AttackerPos.X - auraSize / 2), (int)(fx.AttackerPos.Y - auraSize / 2), (int)auraSize, (int)auraSize), Color.Crimson * 0.6f);
			b.Draw(Game1.staminaRect, new Rectangle((int)(fx.AttackerPos.X - auraSize / 4), (int)(fx.AttackerPos.Y - auraSize / 4), (int)(auraSize / 2), (int)(auraSize / 2)), Color.Cyan * 0.7f);

			// Multi-slash cross at defender
			if (t > 0.25f)
			{
				float sT = (t - 0.25f) / 0.75f;
				float len = MathHelper.Lerp(15f, 95f, sT);
				Vector2 d = fx.DefenderPos;
				DrawLine(b, d - new Vector2(len, len * 0.7f), d + new Vector2(len, len * 0.7f), Color.Crimson, 5);
				DrawLine(b, d + new Vector2(-len, len * 0.7f), d + new Vector2(len, -len * 0.7f), Color.Cyan, 5);
				DrawLine(b, d - new Vector2(len * 0.5f, 0), d + new Vector2(len * 0.5f, 0), Color.White, 3);
			}
			return;
		}

		// 3. Draco Meteor
		if (mName.Equals("Draco Meteor", StringComparison.OrdinalIgnoreCase))
		{
			for (int i = 0; i < 5; i++)
			{
				float mDelay = i * 0.12f;
				float mT = Math.Clamp((t - mDelay) / 0.40f, 0f, 1f);
				if (mT > 0f && mT < 1f)
				{
					Vector2 mStart = new Vector2(fx.DefenderPos.X - 120 + i * 50, fx.DefenderPos.Y - 250);
					Vector2 mPos = Vector2.Lerp(mStart, fx.DefenderPos + new Vector2((i - 2) * 20, 0), mT);
					b.Draw(Game1.staminaRect, new Rectangle((int)mPos.X - 10, (int)mPos.Y - 10, 20, 20), Color.OrangeRed);
					b.Draw(Game1.staminaRect, new Rectangle((int)mPos.X - 5, (int)mPos.Y - 5, 10, 10), Color.Yellow);
				}
			}
			return;
		}

		// 4. Earthquake / Ground moves
		if (fx.Type == PokemonType.Ground || mName.Equals("Earthquake", StringComparison.OrdinalIgnoreCase) || mName.Equals("Earth Power", StringComparison.OrdinalIgnoreCase))
		{
			// Rising earth spikes
			float gT = Math.Clamp(t * 1.3f, 0f, 1f);
			for (int i = -3; i <= 3; i++)
			{
				int spikeH = (int)(Math.Sin(gT * Math.PI) * (45 - Math.Abs(i) * 8));
				int sx = (int)fx.DefenderPos.X + i * 22;
				int sy = (int)fx.DefenderPos.Y - spikeH / 2;
				b.Draw(Game1.staminaRect, new Rectangle(sx - 8, sy, 16, Math.Max(4, spikeH)), new Color(160, 110, 60));
			}
			return;
		}

		// 5. Psychic moves
		if (fx.Type == PokemonType.Psychic)
		{
			// Concentric telepathic rings
			for (int i = 0; i < 3; i++)
			{
				float ringT = Math.Clamp(t * 1.3f - i * 0.18f, 0f, 1f);
				if (ringT > 0f)
				{
					float rad = MathHelper.Lerp(10f, 85f, ringT);
					Color pCol = Color.Lerp(Color.Magenta, Color.Transparent, ringT);
					b.Draw(Game1.staminaRect, new Rectangle((int)(fx.DefenderPos.X - rad), (int)(fx.DefenderPos.Y - rad), (int)(rad * 2), (int)(rad * 2)), pCol * 0.45f);
				}
			}
			return;
		}

		// 6. Ghost moves (Shadow Ball, etc.)
		if (fx.Type == PokemonType.Ghost)
		{
			Vector2 orbPos = Vector2.Lerp(fx.AttackerPos, fx.DefenderPos, t);
			float orbSize = 26f + (float)Math.Sin(t * 16f) * 6f;
			b.Draw(Game1.staminaRect, new Rectangle((int)(orbPos.X - orbSize / 2), (int)(orbPos.Y - orbSize / 2), (int)orbSize, (int)orbSize), new Color(90, 40, 140));
			b.Draw(Game1.staminaRect, new Rectangle((int)(orbPos.X - orbSize / 4), (int)(orbPos.Y - orbSize / 4), (int)(orbSize / 2), (int)(orbSize / 2)), Color.MediumPurple);

			if (t > 0.65f)
			{
				float blastT = (t - 0.65f) / 0.35f;
				float blastR = MathHelper.Lerp(15f, 75f, blastT);
				b.Draw(Game1.staminaRect, new Rectangle((int)(fx.DefenderPos.X - blastR), (int)(fx.DefenderPos.Y - blastR), (int)(blastR * 2), (int)(blastR * 2)), Color.Purple * (1f - blastT));
			}
			return;
		}

		// 7. Dark moves
		if (fx.Type == PokemonType.Dark)
		{
			if (t > 0.20f)
			{
				float dProgress = (t - 0.20f) / 0.80f;
				float dRadius = MathHelper.Lerp(10f, 90f, dProgress);
				b.Draw(Game1.staminaRect, new Rectangle((int)(fx.DefenderPos.X - dRadius), (int)(fx.DefenderPos.Y - dRadius), (int)(dRadius * 2), (int)(dRadius * 2)), new Color(40, 30, 50) * (1f - dProgress));
				DrawLine(b, fx.DefenderPos - new Vector2(dRadius, 0), fx.DefenderPos + new Vector2(dRadius, 0), Color.DarkRed, 4);
			}
			return;
		}

		// 8. Close Combat / Fighting rapid flurry
		if (mName.Equals("Close Combat", StringComparison.OrdinalIgnoreCase))
		{
			int hitIdx = (int)(t * 6f);
			for (int h = 0; h <= hitIdx; h++)
			{
				int ox = ((h % 2 == 0) ? 1 : -1) * (20 + (h * 7) % 30);
				int oy = ((h % 3 == 0) ? -1 : 1) * (15 + (h * 5) % 25);
				Vector2 hPos = fx.DefenderPos + new Vector2(ox, oy);
				b.Draw(Game1.staminaRect, new Rectangle((int)hPos.X - 10, (int)hPos.Y - 10, 20, 20), Color.OrangeRed * 0.8f);
				b.Draw(Game1.staminaRect, new Rectangle((int)hPos.X - 5, (int)hPos.Y - 5, 10, 10), Color.Yellow);
			}
			return;
		}

		// 9. Fire attacks
		if (fx.Type == PokemonType.Fire)
		{
			Vector2 orbPos = Vector2.Lerp(fx.AttackerPos, fx.DefenderPos, t);
			float orbSize = 28f + (float)Math.Sin(t * Math.PI * 4.0) * 8f;
			b.Draw(Game1.staminaRect, new Rectangle((int)(orbPos.X - orbSize / 2), (int)(orbPos.Y - orbSize / 2), (int)orbSize, (int)orbSize), Color.OrangeRed * 0.9f);
			b.Draw(Game1.staminaRect, new Rectangle((int)(orbPos.X - orbSize / 3), (int)(orbPos.Y - orbSize / 3), (int)(orbSize * 0.66f), (int)(orbSize * 0.66f)), Color.Yellow);

			for (int i = 1; i <= 4; i++)
			{
				float pastT = Math.Max(0f, t - i * 0.05f);
				Vector2 pastPos = Vector2.Lerp(fx.AttackerPos, fx.DefenderPos, pastT);
				float pSize = Math.Max(4f, orbSize - i * 5f);
				b.Draw(Game1.staminaRect, new Rectangle((int)pastPos.X, (int)pastPos.Y, (int)pSize, (int)pSize), Color.Orange * (1f - i * 0.2f));
			}

			if (t > 0.60f)
			{
				float expT = (t - 0.60f) / 0.40f;
				float expRadius = MathHelper.Lerp(15f, 65f, expT);
				Color expColor = Color.Lerp(Color.Yellow, Color.OrangeRed * 0f, expT);
				b.Draw(Game1.staminaRect, new Rectangle((int)(fx.DefenderPos.X - expRadius), (int)(fx.DefenderPos.Y - expRadius), (int)(expRadius * 2), (int)(expRadius * 2)), expColor);
			}
			return;
		}

		// 10. Water / Ice attacks
		if (fx.Type == PokemonType.Water || fx.Type == PokemonType.Ice)
		{
			Color waterColor = (fx.Type == PokemonType.Ice) ? Color.LightCyan : Color.DeepSkyBlue;
			for (int i = 0; i < 4; i++)
			{
				float waveT = Math.Clamp(t * 1.2f - i * 0.08f, 0f, 1f);
				Vector2 wPos = Vector2.Lerp(fx.AttackerPos, fx.DefenderPos, waveT);
				float waveOffset = (float)Math.Sin(waveT * Math.PI * 3.0 + i) * 22f;
				Vector2 offsetPos = wPos + new Vector2(0, waveOffset);
				b.Draw(Game1.staminaRect, new Rectangle((int)offsetPos.X - 10, (int)offsetPos.Y - 10, 20, 20), waterColor * 0.85f);
				b.Draw(Game1.staminaRect, new Rectangle((int)offsetPos.X - 5, (int)offsetPos.Y - 5, 10, 10), Color.White);
			}

			if (t > 0.60f)
			{
				float splashT = (t - 0.60f) / 0.40f;
				float splashR = MathHelper.Lerp(10f, 60f, splashT);
				Color splashColor = Color.Lerp(waterColor, Color.Transparent, splashT);
				b.Draw(Game1.staminaRect, new Rectangle((int)(fx.DefenderPos.X - splashR), (int)(fx.DefenderPos.Y - splashR), (int)(splashR * 2), (int)(splashR * 2)), splashColor);
			}
			return;
		}

		// 11. Electric attacks
		if (fx.Type == PokemonType.Electric)
		{
			int segments = 6;
			Vector2 prev = fx.AttackerPos;
			for (int i = 1; i <= segments; i++)
			{
				float segT = i / (float)segments;
				Vector2 targetSeg = Vector2.Lerp(fx.AttackerPos, fx.DefenderPos, segT);
				if (i < segments)
				{
					targetSeg.X += Game1.random.Next(-18, 19);
					targetSeg.Y += Game1.random.Next(-18, 19);
				}
				DrawLine(b, prev, targetSeg, Color.Yellow, 4);
				DrawLine(b, prev, targetSeg, Color.White, 2);
				prev = targetSeg;
			}

			for (int i = 0; i < 5; i++)
			{
				int sx = (int)fx.DefenderPos.X + Game1.random.Next(-40, 41);
				int sy = (int)fx.DefenderPos.Y + Game1.random.Next(-40, 41);
				b.Draw(Game1.staminaRect, new Rectangle(sx, sy, 8, 8), Color.Gold);
			}
			return;
		}

		// 12. Grass attacks
		if (fx.Type == PokemonType.Grass)
		{
			for (int i = 0; i < 4; i++)
			{
				float leafT = Math.Clamp(t * 1.3f - i * 0.1f, 0f, 1f);
				Vector2 lPos = Vector2.Lerp(fx.AttackerPos, fx.DefenderPos, leafT);
				float arc = (float)Math.Sin(leafT * Math.PI) * -35f;
				lPos.Y += arc;
				float leafRot = leafT * 20f + i;
				b.Draw(Game1.staminaRect, new Rectangle((int)lPos.X, (int)lPos.Y, 18, 9), null, Color.LimeGreen, leafRot, new Vector2(9, 4), SpriteEffects.None, 0.95f);
			}

			if (t > 0.60f)
			{
				for (int i = 0; i < 4; i++)
				{
					int lx = (int)fx.DefenderPos.X + Game1.random.Next(-30, 31);
					int ly = (int)fx.DefenderPos.Y + Game1.random.Next(-30, 31);
					b.Draw(Game1.staminaRect, new Rectangle(lx, ly, 10, 10), Color.GreenYellow);
				}
			}
			return;
		}

		// 13. Default Physical / Normal / Fighting / Flying / Bug / Rock / Steel
		if (t < 0.35f)
		{
			b.Draw(Game1.staminaRect, new Rectangle((int)fx.AttackerPos.X - 30, (int)fx.AttackerPos.Y - 30, 60, 60), Color.White * 0.4f);
		}
		else
		{
			float slashProgress = (t - 0.35f) / 0.65f;
			float slashLen = MathHelper.Lerp(10f, 80f, slashProgress);
			Color slashCol = Color.Lerp(Color.White, Color.Transparent, slashProgress);

			Vector2 d = fx.DefenderPos;
			DrawLine(b, d - new Vector2(slashLen, slashLen), d + new Vector2(slashLen, slashLen), slashCol, 4);
			DrawLine(b, d + new Vector2(-slashLen, slashLen), d + new Vector2(slashLen, -slashLen), slashCol, 4);

			for (int i = 0; i < 4; i++)
			{
				float angle = (float)(i * Math.PI / 2.0) + slashProgress * 2f;
				Vector2 spk = d + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (slashLen * 0.7f);
				b.Draw(Game1.staminaRect, new Rectangle((int)spk.X - 4, (int)spk.Y - 4, 8, 8), Color.Gold * (1f - slashProgress));
			}
		}
	}

	private static void DrawLine(SpriteBatch b, Vector2 p1, Vector2 p2, Color color, int thickness = 2)
	{
		Vector2 delta = p2 - p1;
		float dist = delta.Length();
		if (dist < 0.001f) return;
		float angle = (float)Math.Atan2(delta.Y, delta.X);
		b.Draw(Game1.staminaRect, p1, null, color, angle, Vector2.Zero, new Vector2(dist, thickness), SpriteEffects.None, 0.95f);
	}

	private static void DrawCleanText(SpriteBatch b, SpriteFont font, string text, Vector2 pos, Color color, float scale = 1.0f)
	{
		b.DrawString(font, text, pos + new Vector2(1, 1), Color.Black * 0.65f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.99f);
		b.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1.0f);
	}

	private static void DrawStatusBadge(SpriteBatch b, PokemonInstance p, int x, int y)
	{
		if (p == null || p.Status == PokemonStatusCondition.None) return;
		(string tag, Color bg) = p.Status switch
		{
			PokemonStatusCondition.Paralysis => ("PAR", new Color(215, 165, 20)),
			PokemonStatusCondition.Burn => ("BRN", new Color(210, 55, 35)),
			PokemonStatusCondition.Sleep => ("SLP", new Color(115, 125, 145)),
			PokemonStatusCondition.Freeze => ("FRZ", new Color(65, 175, 220)),
			PokemonStatusCondition.Poison => ("PSN", new Color(150, 55, 175)),
			PokemonStatusCondition.Toxic => ("TOX", new Color(110, 25, 140)),
			_ => ("", Color.Gray)
		};
		if (string.IsNullOrEmpty(tag)) return;
		Rectangle r = new Rectangle(x, y, 36, 16);
		b.Draw(Game1.staminaRect, r, bg);
		PokemonUI.Border(b, r, 1, Color.White * 0.65f);
		PokemonUI.Text(b, Game1.tinyFont, tag, r, Color.White, center: true, maxScale: 0.68f);
	}

	private void DrawEnemyStatus(SpriteBatch b)
	{
		if (EnemyPokemon == null) return;

		int sx = xPositionOnScreen + 40;
		int sy = yPositionOnScreen + 35;
		int sw = 290;
		int sh = 78;

		IClickableMenu.drawTextureBox(b, sx, sy, sw, sh, Color.White);

		// Name, Shiny star, Mega tag, Gender, and Level with fitted bounds
		string shinyPrefix = EnemyPokemon.IsShiny ? "★ " : "";
		if (EnemyPokemon.IsMegaEvolved || (EnemyPokemon.Species != null && EnemyPokemon.Species.IsMega)) shinyPrefix += "🧬 ";
		string genderSuffix = !string.IsNullOrEmpty(EnemyPokemon.Gender) ? " " + EnemyPokemon.Gender : "";
		PokemonUI.Text(b, Game1.smallFont, shinyPrefix + EnemyPokemon.Nickname + genderSuffix, new Rectangle(sx + 14, sy + 8, sw - 125, 26), EnemyPokemon.IsShiny ? new Color(180, 125, 0) : Game1.textColor, maxScale: 0.85f);
		DrawStatusBadge(b, EnemyPokemon, sx + sw - 110, sy + 12);
		PokemonUI.Text(b, Game1.tinyFont, $"Lv.{EnemyPokemon.Level}", new Rectangle(sx + sw - 72, sy + 10, 58, 22), Color.DarkSlateGray, center: true, maxScale: 0.85f);

		// HP Tag & Bar
		PokemonUI.HpTag(b, new Rectangle(sx + 14, sy + 42, 24, 14));
		float ratio = Math.Clamp(_displayEnemyHP / Math.Max(1, EnemyPokemon.MaxHP), 0f, 1f);
		PokemonUI.HpBar(b, new Rectangle(sx + 44, sy + 43, sw - 60, 12), ratio);
	}

	private void DrawPlayerStatus(SpriteBatch b)
	{
		if (PlayerPokemon == null) return;

		int sx = xPositionOnScreen + width - 360;
		int sy = yPositionOnScreen + height - _boxHeight - 110;
		int sw = 330;
		int sh = 88;

		IClickableMenu.drawTextureBox(b, sx, sy, sw, sh, Color.White);

		// Name, Shiny star, Mega/Dynamax tag, and Level with fitted bounds
		string prefix = PlayerPokemon.IsShiny ? "★ " : "";
		if (PlayerPokemon.IsMegaEvolved) prefix += "🧬 ";
		else if (PlayerPokemon.IsDynamaxed) prefix += PlayerPokemon.IsGigantamaxed ? "[G-MAX] " : "[DMAX] ";
		string genderSuffix = !string.IsNullOrEmpty(PlayerPokemon.Gender) ? " " + PlayerPokemon.Gender : "";

		PokemonUI.Text(b, Game1.smallFont, prefix + PlayerPokemon.Nickname + genderSuffix, new Rectangle(sx + 14, sy + 8, sw - 125, 26), PlayerPokemon.IsShiny ? new Color(180, 125, 0) : Game1.textColor, maxScale: 0.85f);
		DrawStatusBadge(b, PlayerPokemon, sx + sw - 110, sy + 12);
		PokemonUI.Text(b, Game1.tinyFont, $"Lv.{PlayerPokemon.Level}", new Rectangle(sx + sw - 72, sy + 10, 58, 22), Color.DarkSlateGray, center: true, maxScale: 0.85f);

		// HP Tag & Bar
		PokemonUI.HpTag(b, new Rectangle(sx + 14, sy + 38, 24, 14));
		float ratio = Math.Clamp(_displayPlayerHP / Math.Max(1, PlayerPokemon.MaxHP), 0f, 1f);
		PokemonUI.HpBar(b, new Rectangle(sx + 44, sy + 39, sw - 60, 12), ratio);

		// Exp Bar
		float expRatio = Math.Clamp((float)PlayerPokemon.CurrentExp / Math.Max(1, PlayerPokemon.ExpToNextLevel), 0f, 1f);
		PokemonUI.ExpBar(b, new Rectangle(sx + 44, sy + 58, sw - 170, 6), expRatio);

		// HP numbers
		string hpStr = $"{(int)_displayPlayerHP} / {PlayerPokemon.MaxHP}";
		PokemonUI.Text(b, Game1.tinyFont, hpStr, new Rectangle(sx + sw - 120, sy + 56, 106, 20), Color.DarkSlateGray, center: true, maxScale: 0.85f);
	}

		private void DrawWeatherBanner(SpriteBatch b)
	{
		if (CurrentWeather == BattleWeather.None) return;

		string weatherText = CurrentWeather switch
		{
			BattleWeather.Rain => "🌧️ Chuva Ativa (Água x1.5 | Fogo x0.5)",
			BattleWeather.HarshSun => "☀️ Sol Escaldante (Fogo x1.5 | Água x0.5)",
			BattleWeather.Sandstorm => "🌪️ Tempestade de Areia (Dano residual | Sp.Def Rocha +50%)",
			BattleWeather.Snow => "❄️ Nevasca Ativa (Defesa Gelo +50%)",
			_ => ""
		};

		if (WeatherTurns > 0 && WeatherTurns < 90)
		{
			weatherText += $" [{WeatherTurns}t]";
		}

		Color boxBg = CurrentWeather switch
		{
			BattleWeather.Rain => Color.DarkSlateBlue * 0.88f,
			BattleWeather.HarshSun => new Color(130, 80, 20) * 0.88f,
			BattleWeather.Sandstorm => new Color(120, 85, 40) * 0.88f,
			BattleWeather.Snow => new Color(40, 90, 130) * 0.88f,
			_ => Color.Black * 0.75f
		};

		Color borderColor = CurrentWeather switch
		{
			BattleWeather.Rain => Color.Cyan,
			BattleWeather.HarshSun => Color.Gold,
			BattleWeather.Sandstorm => Color.SandyBrown,
			BattleWeather.Snow => Color.LightCyan,
			_ => Color.White
		};

		Rectangle bannerRect = new Rectangle(xPositionOnScreen + width / 2 - 210, yPositionOnScreen + 16, 420, 32);
		b.Draw(Game1.staminaRect, bannerRect, boxBg);
		DrawBorder(b, bannerRect, 1, borderColor);
		PokemonUI.Text(b, Game1.tinyFont, weatherText, bannerRect, Color.White, center: true, maxScale: 0.88f);
	}

	private void DrawMessagePanel(SpriteBatch b)
	{
		PokemonUI.Text(b, Game1.smallFont, _currentMessage, new Rectangle(_boxX + 30, _boxY + 22, _boxWidth - 85, _boxHeight - 35), Game1.textColor, maxScale: 0.90f);

		// Animated blinking arrow like classic Pokemon dialogue boxes
		if (((int)(Game1.currentGameTime.TotalGameTime.TotalSeconds * 3.0) % 2) == 0)
		{
			PokemonUI.Text(b, Game1.smallFont, "▼", new Rectangle(_boxX + _boxWidth - 45, _boxY + _boxHeight - 42, 28, 28), Color.DarkRed, center: true, maxScale: 0.90f);
		}
	}

	private void DrawActionSelectPanel(SpriteBatch b)
	{
		string prompt = $"O que {PlayerPokemon?.Nickname} vai fazer?";
		int promptW = Math.Max(260, _boxWidth - 600);
		PokemonUI.Text(b, Game1.smallFont, prompt, new Rectangle(_boxX + 36, _boxY + 35, promptW, _boxHeight - 60), Game1.textColor, maxScale: 0.90f);

		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		for (int i = 0; i < _actionButtons.Count; i++)
		{
			var btn = _actionButtons[i];
			bool hovered = btn.containsPoint(mouseX, mouseY);
			bool gamepadFocused = (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected) && (_gamepadActionIndex == i);
			IClickableMenu.drawTextureBox(b, btn.bounds.X, btn.bounds.Y, btn.bounds.Height > 0 ? btn.bounds.Width : 100, btn.bounds.Height, (hovered || gamepadFocused) ? Color.LemonChiffon : Color.White);
			if (gamepadFocused)
			{
				float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 6f);
				int exp = pulse > 0 ? 3 : 2;
				Rectangle glowRect = new Rectangle(btn.bounds.X - exp, btn.bounds.Y - exp, btn.bounds.Width + exp * 2, btn.bounds.Height + exp * 2);
				PokemonUI.Border(b, glowRect, 3, Color.Gold);
			}
			PokemonUI.Text(b, Game1.smallFont, btn.label, new Rectangle(btn.bounds.X + 8, btn.bounds.Y + 8, btn.bounds.Width - 16, btn.bounds.Height - 16), (hovered || gamepadFocused) ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.85f);
		}
	}

	private static void DrawGimmickPill(SpriteBatch b, ClickableComponent btn, string text, bool available, bool toggled, Color activeColor, int mouseX, int mouseY)
	{
		if (btn == null) return;
		bool hov = available && btn.containsPoint(mouseX, mouseY);

		Color boxTint;
		Color textColor;

		if (!available)
		{
			boxTint = Color.Gray * 0.65f;
			textColor = Color.DarkGray;
		}
		else if (toggled)
		{
			boxTint = new Color(255, 235, 170);
			textColor = Color.DarkRed;
		}
		else if (hov)
		{
			boxTint = Color.LemonChiffon;
			textColor = Color.DarkBlue;
		}
		else
		{
			boxTint = Color.White;
			textColor = Game1.textColor;
		}

		IClickableMenu.drawTextureBox(
			b,
			Game1.menuTexture,
			new Rectangle(0, 256, 60, 60),
			btn.bounds.X,
			btn.bounds.Y,
			btn.bounds.Width,
			btn.bounds.Height,
			boxTint,
			1f,
			drawShadow: toggled);

		if (toggled)
		{
			PokemonUI.Border(b, btn.bounds, 2, activeColor);
		}
		else if (available && hov)
		{
			PokemonUI.Border(b, btn.bounds, 1, Color.Gold * 0.8f);
		}

		PokemonUI.Text(
			b,
			Game1.smallFont,
			text,
			new Rectangle(btn.bounds.X + 4, btn.bounds.Y + 2, btn.bounds.Width - 8, btn.bounds.Height - 4),
			textColor,
			center: true,
			maxScale: 0.76f,
			shadow: false);
	}

	private void DrawMoveSelectPanel(SpriteBatch b)
	{
		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		bool backHovered = _backButton.containsPoint(mouseX, mouseY);
		IClickableMenu.drawTextureBox(b, _backButton.bounds.X, _backButton.bounds.Y, _backButton.bounds.Width, _backButton.bounds.Height, backHovered ? Color.Wheat : Color.White);
		PokemonUI.Text(b, Game1.smallFont, _backButton.label, new Rectangle(_backButton.bounds.X + 4, _backButton.bounds.Y + 4, _backButton.bounds.Width - 8, _backButton.bounds.Height - 8), Color.DarkSlateGray, center: true, maxScale: 0.85f);

		// Draw Mega Evolution, Z-Move, Dynamax/Gigantamax, and Terastallization Toggle Pills ONLY when equipped with the required Ring & Stone/Crystal/Orb
		if (PlayerPokemon != null)
		{
			bool canUltraBurst = !_megaUsedThisBattle && !PlayerPokemon.IsDynamaxed && PokemonItemManager.CanUseUltraBurst(Game1.player, PlayerPokemon);
			bool canMega = !_megaUsedThisBattle && !PlayerPokemon.IsDynamaxed && (PokemonItemManager.GetAvailableMegaEvolution(Game1.player, PlayerPokemon, out _, out _) || canUltraBurst);
			bool canZ = !_zMoveUsedThisBattle && !PlayerPokemon.IsDynamaxed && PokemonItemManager.HasAnyMatchingZCrystal(Game1.player, PlayerPokemon);
			bool canDyn = !_dynamaxUsedThisBattle && !PlayerPokemon.IsDynamaxed && !PlayerPokemon.IsMegaEvolved && PokemonItemManager.IsWearingDynamaxBand(Game1.player);
			bool canGmax = canDyn && PlayerPokemon.Species != null && PlayerPokemon.Species.HasGigantamax;
			bool canTera = !_teraUsedThisBattle && !PlayerPokemon.IsDynamaxed && !PlayerPokemon.IsTerastallized && PokemonItemManager.CanTerastallize(Game1.player, PlayerPokemon);

			if (canMega || PlayerPokemon.IsMegaEvolved)
			{
				string megaLabel = PlayerPokemon.IsMegaEvolved ? "🧬 MEGA ATIVO" : (canUltraBurst ? "🌟 ULTRA BURST" : "🧬 MEGA");
				DrawGimmickPill(b, _megaToggleBtn, megaLabel, canMega, _megaToggled || PlayerPokemon.IsMegaEvolved, new Color(155, 65, 225), mouseX, mouseY);
			}

			if (canZ || (_zMoveUsedThisBattle && PokemonItemManager.IsWearingZRing(Game1.player)))
				DrawGimmickPill(b, _zMoveToggleBtn, _zMoveUsedThisBattle ? "💎 Z USADO" : "💎 Z-MOVE", canZ, _zMoveToggled, new Color(215, 155, 15), mouseX, mouseY);

			string dynLabel = (PlayerPokemon.IsDynamaxed && !PlayerPokemon.IsGigantamaxed) ? $"🔴 DMAX ({PlayerPokemon.DynamaxTurns}T)" : "🔴 DYNAMAX";
			if (canDyn || (PlayerPokemon.IsDynamaxed && !PlayerPokemon.IsGigantamaxed))
				DrawGimmickPill(b, _dynamaxToggleBtn, dynLabel, canDyn, _dynamaxToggled || (PlayerPokemon.IsDynamaxed && !PlayerPokemon.IsGigantamaxed), new Color(215, 30, 95), mouseX, mouseY);

			string gmaxLabel = PlayerPokemon.IsGigantamaxed ? $"🌟 G-MAX ({PlayerPokemon.DynamaxTurns}T)" : "🌟 G-MAX";
			if (canGmax || PlayerPokemon.IsGigantamaxed)
				DrawGimmickPill(b, _gmaxToggleBtn, gmaxLabel, canGmax, _gmaxToggled || PlayerPokemon.IsGigantamaxed, new Color(225, 75, 40), mouseX, mouseY);

			if (canTera || PlayerPokemon.IsTerastallized || (_teraUsedThisBattle && PokemonItemManager.IsWearingTeraOrb(Game1.player)))
			{
				string teraLabel = PlayerPokemon.IsTerastallized
					? $"✨ TERA [{PlayerPokemon.TeraType}]"
					: (_teraUsedThisBattle ? "✨ TERA USADO" : $"✨ TERA ({PlayerPokemon.TeraType})");
				DrawGimmickPill(b, _teraToggleBtn, teraLabel, canTera, _teraToggled || PlayerPokemon.IsTerastallized, new Color(50, 180, 230), mouseX, mouseY);
			}
		}

		bool showMaxMoves = PlayerPokemon != null && (_dynamaxToggled || _gmaxToggled || PlayerPokemon.IsDynamaxed);
		bool showZMoves = PlayerPokemon != null && _zMoveToggled && !showMaxMoves;
		bool isGmaxPreview = PlayerPokemon != null && (_gmaxToggled || PlayerPokemon.IsGigantamaxed);

		for (int i = 0; i < 4; i++)
		{
			var btn = _moveButtons[i];
			if (PlayerPokemon != null && i < PlayerPokemon.Moves.Count)
			{
				PokemonMove move = PlayerPokemon.Moves[i];
				bool hovered = btn.containsPoint(mouseX, mouseY);
				bool gamepadFocused = (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected) && (_gamepadMoveIndex == i);
				string sigName = null;
				int sigPwr = 0;
				PokemonType sigType = PokemonType.Normal;
				bool hasSigZ = showZMoves && PokemonItemManager.CanUseSignatureZMove(Game1.player, PlayerPokemon, move, out sigName, out sigPwr, out sigType);
				bool moveHasZCrystal = showZMoves && (hasSigZ || PokemonItemManager.HasZCrystalForType(Game1.player, move.Type, PlayerPokemon));
				Color cardTint = showMaxMoves
					? ((hovered || gamepadFocused) ? new Color(255, 210, 225) : new Color(255, 230, 240))
					: (moveHasZCrystal ? ((hovered || gamepadFocused) ? new Color(255, 245, 180) : new Color(255, 250, 210)) : ((hovered || gamepadFocused) ? Color.LemonChiffon : Color.White));
				IClickableMenu.drawTextureBox(b, btn.bounds.X, btn.bounds.Y, btn.bounds.Width, btn.bounds.Height, cardTint);

				if (gamepadFocused)
				{
					float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 6f);
					int exp = pulse > 0 ? 3 : 2;
					Rectangle glowRect = new Rectangle(btn.bounds.X - exp, btn.bounds.Y - exp, btn.bounds.Width + exp * 2, btn.bounds.Height + exp * 2);
					PokemonUI.Border(b, glowRect, 3, Color.Gold);
				}

				string dispName = showMaxMoves
					? move.GetMaxMoveName(isGmaxPreview, PlayerPokemon.Species?.InternalName)
					: (hasSigZ ? sigName : (moveHasZCrystal ? move.GetZMoveName() : move.Name));
				int dispPower = showMaxMoves
					? move.GetMaxMovePower()
					: (hasSigZ ? sigPwr : (moveHasZCrystal ? move.GetZMovePower() : move.Power));
				PokemonType dispType = hasSigZ ? sigType : move.Type;
				MoveCategory dispCategory = move.Category;

				// Tera Blast adaptation preview
				if (string.Equals(move.Name, "Tera Blast", StringComparison.OrdinalIgnoreCase) && PlayerPokemon.IsTerastallized)
				{
					dispType = PlayerPokemon.TeraType;
					if (PlayerPokemon.Attack > PlayerPokemon.SpAttack)
						dispCategory = MoveCategory.Physical;
				}

				// Terastallization 60 BP floor preview
				if (PlayerPokemon.IsTerastallized && dispType == PlayerPokemon.TeraType && dispPower > 0 && dispPower < 60 && !showMaxMoves && !moveHasZCrystal)
				{
					dispPower = 60;
				}

				// Line 1: Move Name fitted
				string moveTitle = $"{i + 1}. {dispName}";
				PokemonUI.Text(b, Game1.smallFont, moveTitle, new Rectangle(btn.bounds.X + 16, btn.bounds.Y + 8, btn.bounds.Width - 110, 26), hovered ? Color.DarkRed : Game1.textColor, maxScale: 0.85f);

				// Line 2: Type pill, Category, & PP
				DrawTypePill(b, dispType, btn.bounds.Right - 90, btn.bounds.Y + 12);
				string catTag = dispCategory == MoveCategory.Physical ? "FÍS" : (dispCategory == MoveCategory.Special ? "ESP" : "STA");
				PokemonUI.Text(b, Game1.tinyFont, $"PP: {move.CurrentPP}/{move.MaxPP}  [{catTag} | PWR {(dispPower > 0 ? dispPower.ToString() : "--")}]", new Rectangle(btn.bounds.X + 16, btn.bounds.Y + 36, btn.bounds.Width - 32, 20), Color.DarkSlateGray, maxScale: 0.78f);
			}
			else
			{
				IClickableMenu.drawTextureBox(b, btn.bounds.X, btn.bounds.Y, btn.bounds.Width, btn.bounds.Height, Color.Gray * 0.4f);
				PokemonUI.Text(b, Game1.smallFont, "-", btn.bounds, Color.DarkGray, center: true);
			}
		}
	}

	private static void DrawTypePill(SpriteBatch b, PokemonType type, int px, int py)
	{
		string name = type.ToString();
		Rectangle r = new Rectangle(px, py, 76, 18);
		b.Draw(Game1.staminaRect, r, PokemonTypeColors.Get(type));
		PokemonUI.Border(b, r, 1, Color.White * 0.45f);
		PokemonUI.Text(b, Game1.tinyFont, name, new Rectangle(px + 2, py, 72, 18), Color.White, center: true, maxScale: 0.70f);
	}

	private void DrawBagSelectPanel(SpriteBatch b)
	{
		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		bool backHovered = _backButton.containsPoint(mouseX, mouseY);
		IClickableMenu.drawTextureBox(b, _backButton.bounds.X, _backButton.bounds.Y, _backButton.bounds.Width, _backButton.bounds.Height, backHovered ? Color.Wheat : Color.White);
		PokemonUI.Text(b, Game1.smallFont, _backButton.label, new Rectangle(_backButton.bounds.X + 4, _backButton.bounds.Y + 4, _backButton.bounds.Width - 8, _backButton.bounds.Height - 8), Color.DarkSlateGray, center: true, maxScale: 0.85f);

		for (int i = 0; i < _bagButtons.Count; i++)
		{
			var btn = _bagButtons[i];
			bool hovered = btn.containsPoint(mouseX, mouseY);
			bool gamepadFocused = (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected) && (_gamepadBagIndex == i);
			IClickableMenu.drawTextureBox(b, btn.bounds.X, btn.bounds.Y, btn.bounds.Width, btn.bounds.Height, (hovered || gamepadFocused) ? Color.LemonChiffon : Color.White);
			if (gamepadFocused)
			{
				float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 6f);
				int exp = pulse > 0 ? 3 : 2;
				Rectangle glowRect = new Rectangle(btn.bounds.X - exp, btn.bounds.Y - exp, btn.bounds.Width + exp * 2, btn.bounds.Height + exp * 2);
				PokemonUI.Border(b, glowRect, 3, Color.Gold);
			}
			PokemonUI.Text(b, Game1.smallFont, btn.label, new Rectangle(btn.bounds.X + 8, btn.bounds.Y + 6, btn.bounds.Width - 16, btn.bounds.Height - 12), (hovered || gamepadFocused) ? Color.DarkRed : Game1.textColor, maxScale: 0.85f);
		}

		if (_bagPrevButton != null)
		{
			bool prevHov = _bagPrevButton.containsPoint(mouseX, mouseY);
			IClickableMenu.drawTextureBox(b, _bagPrevButton.bounds.X, _bagPrevButton.bounds.Y, _bagPrevButton.bounds.Width, _bagPrevButton.bounds.Height, prevHov ? Color.Wheat : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _bagPrevButton.label, _bagPrevButton.bounds, Color.DarkSlateGray, center: true, maxScale: 0.85f);
		}

		if (_bagNextButton != null)
		{
			bool nextHov = _bagNextButton.containsPoint(mouseX, mouseY);
			IClickableMenu.drawTextureBox(b, _bagNextButton.bounds.X, _bagNextButton.bounds.Y, _bagNextButton.bounds.Width, _bagNextButton.bounds.Height, nextHov ? Color.Wheat : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _bagNextButton.label, _bagNextButton.bounds, Color.DarkSlateGray, center: true, maxScale: 0.85f);
		}

		PokemonUI.Text(b, Game1.tinyFont, $"Pág. {_bagPage + 1}", new Rectangle(_boxX + _boxWidth - 110, _boxY + 98, 95, 20), Color.DarkSlateGray, center: true, maxScale: 0.85f);
	}

	private void DrawSwitchSelectPanel(SpriteBatch b)
	{
		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		bool mustSwitch = (PlayerPokemon == null || PlayerPokemon.IsFainted);
		if (!mustSwitch)
		{
			bool backHovered = _backButton.containsPoint(mouseX, mouseY);
			IClickableMenu.drawTextureBox(b, _backButton.bounds.X, _backButton.bounds.Y, _backButton.bounds.Width, _backButton.bounds.Height, backHovered ? Color.Wheat : Color.White);
			PokemonUI.Text(b, Game1.smallFont, _backButton.label, new Rectangle(_backButton.bounds.X + 4, _backButton.bounds.Y + 4, _backButton.bounds.Width - 8, _backButton.bounds.Height - 8), Color.DarkSlateGray, center: true, maxScale: 0.85f);
		}
		else
		{
			PokemonUI.Text(b, Game1.smallFont, "SEU POKÉMON DESMAIOU! ESCOLHA O PRÓXIMO COMBATENTE:", new Rectangle(_boxX + 25, _boxY - 32, _boxWidth - 50, 26), Color.Gold, center: true, maxScale: 0.85f);
		}

		for (int i = 0; i < _switchButtons.Count; i++)
		{
			var btn = _switchButtons[i];
			var p = (i < PlayerTeam.Count) ? PlayerTeam[i] : null;
			bool isCurrent = (i == PlayerActiveIndex);
			bool hovered = btn.containsPoint(mouseX, mouseY);
			bool gamepadFocused = (Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected) && (_gamepadSwitchIndex == i);

			Color boxColor = (p != null && p.IsFainted) ? (Color.Gray * 0.5f) : (isCurrent ? Color.LightSkyBlue : ((hovered || gamepadFocused) ? Color.LemonChiffon : Color.White));
			IClickableMenu.drawTextureBox(b, btn.bounds.X, btn.bounds.Y, btn.bounds.Width, btn.bounds.Height, boxColor);

			if (gamepadFocused)
			{
				float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 6f);
				int exp = pulse > 0 ? 3 : 2;
				Rectangle glowRect = new Rectangle(btn.bounds.X - exp, btn.bounds.Y - exp, btn.bounds.Width + exp * 2, btn.bounds.Height + exp * 2);
				PokemonUI.Border(b, glowRect, 3, Color.Gold);
			}

			string label = btn.label;
			if (isCurrent) label += " [Ativo]";
			PokemonUI.Text(b, Game1.smallFont, label, new Rectangle(btn.bounds.X + 12, btn.bounds.Y + 6, btn.bounds.Width - 24, btn.bounds.Height - 12), (p != null && p.IsFainted) ? Color.DarkRed : ((hovered || gamepadFocused) ? Color.DarkRed : Game1.textColor), maxScale: 0.85f);
		}
	}
}
