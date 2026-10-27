using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Pokemon;

public class PokemonInstance
{
	public PokemonSpecies Species { get; set; }
	public string Nickname { get; set; }
	public int Level { get; set; }
	public int CurrentExp { get; set; }
	public int ExpToNextLevel => (Level >= 100) ? 0 : Math.Max(10, ((Level + 1) * (Level + 1) * (Level + 1)) - (Level * Level * Level));

	public int MaxHP { get; set; }
	public int CurrentHP { get; set; }
	public int Attack { get; set; }
	public int Defense { get; set; }
	public int SpAttack { get; set; }
	public int SpDefense { get; set; }
	public int Speed { get; set; }

	public PokemonNature Nature { get; set; } = PokemonNature.Hardy;

	// Individual Values (0 to 31)
	public int IV_HP { get; set; } = 31;
	public int IV_Attack { get; set; } = 31;
	public int IV_Defense { get; set; } = 31;
	public int IV_SpAttack { get; set; } = 31;
	public int IV_SpDefense { get; set; } = 31;
	public int IV_Speed { get; set; } = 31;

	// Effort Values (0 to 252 each, max 510 total)
	public int EV_HP { get; set; } = 0;
	public int EV_Attack { get; set; } = 0;
	public int EV_Defense { get; set; } = 0;
	public int EV_SpAttack { get; set; } = 0;
	public int EV_SpDefense { get; set; } = 0;
	public int EV_Speed { get; set; } = 0;

	public int TotalIVs => IV_HP + IV_Attack + IV_Defense + IV_SpAttack + IV_SpDefense + IV_Speed;
	public int TotalEVs => EV_HP + EV_Attack + EV_Defense + EV_SpAttack + EV_SpDefense + EV_Speed;

	public bool IsEgg { get; set; } = false;
	public int EggSteps { get; set; } = 0;

	public string IVRating => TotalIVs switch
	{
		>= 151 => "Incrível! (Potencial Lendário)",
		>= 121 => "Superior! (Excepcional)",
		>= 91  => "Acima da Média",
		_      => "Decente"
	};

	public List<PokemonMove> Moves { get; set; } = new();

	public bool IsShiny { get; set; } = false;
	public string Gender { get; set; } = "♂";
	public PokemonStatusCondition Status { get; set; } = PokemonStatusCondition.None;
	public int StatusTurns { get; set; } = 0;
	public int ConfusionTurns { get; set; } = 0;
	public bool IsConfused => ConfusionTurns > 0;

	public void CureStatus()
	{
		Status = PokemonStatusCondition.None;
		StatusTurns = 0;
		ConfusionTurns = 0;
	}

	public PokemonInstance Clone()
	{
		var copy = new PokemonInstance(Species, Level)
		{
			Nickname = Nickname,
			CurrentExp = CurrentExp,
			MaxHP = MaxHP,
			CurrentHP = CurrentHP,
			Attack = Attack,
			Defense = Defense,
			SpAttack = SpAttack,
			SpDefense = SpDefense,
			Speed = Speed,
			Nature = Nature,
			IV_HP = IV_HP,
			IV_Attack = IV_Attack,
			IV_Defense = IV_Defense,
			IV_SpAttack = IV_SpAttack,
			IV_SpDefense = IV_SpDefense,
			IV_Speed = IV_Speed,
			EV_HP = EV_HP,
			EV_Attack = EV_Attack,
			EV_Defense = EV_Defense,
			EV_SpAttack = EV_SpAttack,
			EV_SpDefense = EV_SpDefense,
			EV_Speed = EV_Speed,
			IsEgg = IsEgg,
			EggSteps = EggSteps,
			IsShiny = IsShiny,
			Gender = Gender,
			Status = Status,
			StatusTurns = StatusTurns,
			ConfusionTurns = ConfusionTurns,
			HeldItem = HeldItem,
			Ability = Ability,
			OriginalTrainer = OriginalTrainer,
			TrainerID = TrainerID,
			IsMegaEvolved = IsMegaEvolved,
			IsBattleOnlyMega = IsBattleOnlyMega,
			PreMegaSpecies = PreMegaSpecies,
			UsedMegaStoneId = UsedMegaStoneId,
			DynamaxTurns = DynamaxTurns,
			IsUsingGigantamax = IsUsingGigantamax,
			Friendship = Friendship,
			TeraType = TeraType,
			IsTerastallized = IsTerastallized,
			Moves = Moves != null ? Moves.ConvertAll(m => m?.Clone()) : new List<PokemonMove>()
		};
		return copy;
	}

	// Held Item & Passive Ability (attached directly to this Pokémon)
	public string HeldItem { get; set; } = null;
	private string _ability = null;
	public string Ability
	{
		get => _ability?.Replace(" ", "").Replace("-", "").Replace("'", "");
		set => _ability = value?.Replace(" ", "").Replace("-", "").Replace("'", "");
	}
	public string OriginalTrainer { get; set; } = Game1.player?.Name ?? "Treinador";
	public int TrainerID { get; set; } = (int)(Game1.player?.UniqueMultiplayerID ?? 12345) % 100000;

	// Battle Gimmick States (Gen 6 Mega, Gen 7 Z-Move, Gen 8 Dynamax/Gigantamax)
	public bool IsMegaEvolved { get; set; } = false;
	public bool IsBattleOnlyMega { get; set; } = false;
	public PokemonSpecies PreMegaSpecies { get; set; } = null;
	public string PreMegaSpeciesInternalName
	{
		get => PreMegaSpecies?.InternalName ?? Species?.BaseSpeciesName;
		set => PreMegaSpecies = !string.IsNullOrWhiteSpace(value) ? PokemonSpecies.Get(value) : null;
	}
	public string UsedMegaStoneId { get; set; } = null;
	public int DynamaxTurns { get; set; } = 0;
	public bool IsUsingGigantamax { get; set; } = false;
	public bool IsDynamaxed => DynamaxTurns > 0;
	public bool IsGigantamaxed => DynamaxTurns > 0 && IsUsingGigantamax && Species != null && Species.HasGigantamax;

	// Friendship (0-255 canonical standard)
	public int Friendship { get; set; } = 70;

	// Terastallization (Gen 9)
	public PokemonType TeraType { get; set; } = PokemonType.Normal;
	public bool IsTerastallized { get; set; } = false;

	public bool Terastallize()
	{
		if (IsTerastallized || IsFainted) return false;
		IsTerastallized = true;
		InvalidateSpriteCache();
		return true;
	}

	public bool HasType(PokemonType type)
	{
		if (IsTerastallized)
		{
			return TeraType == type;
		}
		if (Species == null) return false;
		return Species.PrimaryType == type || (Species.SecondaryType.HasValue && Species.SecondaryType.Value == type);
	}

	public List<PokemonType> GetDefensiveTypes()
	{
		if (IsTerastallized)
		{
			return new List<PokemonType> { TeraType };
		}
		var list = new List<PokemonType>();
		if (Species != null)
		{
			list.Add(Species.PrimaryType);
			if (Species.SecondaryType.HasValue) list.Add(Species.SecondaryType.Value);
		}
		return list;
	}

	public void AdjustFriendship(int amount)
	{
		Friendship = Math.Clamp(Friendship + amount, 0, 255);
	}

	// Rampage multi-turn state (Outrage, Thrash, Petal Dance)
	public int RampageTurns { get; set; } = 0;
	public string RampageMoveName { get; set; } = null;

	// Volatile battle-only flags (never persisted; cleared by PokemonDtoMapper)
	/// <summary>Set by a damaging move's flinch effect and consumed before the target acts.</summary>
	public bool Flinched { get; set; } = false;
	/// <summary>Turns of Salt Cure damage remaining (0 = not affected).</summary>
	public int SaltCureTurns { get; set; } = 0;

	public bool IsFainted => CurrentHP <= 0;
	public float HpRatio => MaxHP <= 0 ? 0f : Math.Clamp((float)CurrentHP / MaxHP, 0f, 1f);

	// Cached GBA battle sprites and pixel-perfect bottom grounding rows
	private Texture2D _frontSprite;
	private Texture2D _backSprite;
	private Texture2D _iconSprite;
	private int _frontBottomY = -1;
	private int _backBottomY = -1;
	private bool _cachedSpriteShinyState = false;
	private bool _cachedSpriteGmaxState = false;
	private bool _cachedSpriteDynamaxState = false;
	private bool _cachedSpriteIsEgg = false;

	public void InvalidateSpriteCache()
	{
		_frontSprite = null;
		_backSprite = null;
		_iconSprite = null;
		_frontBottomY = -1;
		_backBottomY = -1;
	}

	public PokemonInstance(PokemonSpecies species, int level, params string[] initialMoves)
	{
		Species = species;
		Nickname = species.Name;
		Level = Math.Max(1, level);
		if (species.IsMega)
		{
			IsMegaEvolved = true;
			if (!string.IsNullOrEmpty(species.BaseSpeciesName))
			{
				PreMegaSpecies = PokemonSpecies.Get(species.BaseSpeciesName);
			}
		}

		// Roll canonical IVs, Nature, Gender, and wild Shiny chance (1/512)
		if (Game1.random != null)
		{
			Nature = (PokemonNature)Game1.random.Next(25);
			IV_HP = Game1.random.Next(32);
			IV_Attack = Game1.random.Next(32);
			IV_Defense = Game1.random.Next(32);
			IV_SpAttack = Game1.random.Next(32);
			IV_SpDefense = Game1.random.Next(32);
			IV_Speed = Game1.random.Next(32);
			IsShiny = Game1.random.Next(512) == 0;
			Gender = RollDefaultGender(species, Game1.random.Next(100) < 50);
		}
		else
		{
			Nature = PokemonNature.Hardy;
			IV_HP = 31; IV_Attack = 31; IV_Defense = 31;
			IV_SpAttack = 31; IV_SpDefense = 31; IV_Speed = 31;
			Gender = RollDefaultGender(species, true);
		}

		RecalculateStats();
		CurrentHP = MaxHP;
		Ability ??= PokemonAbility.GetDefaultAbility(species, species.IsMega);

		TeraType = (Game1.random != null && Game1.random.NextDouble() < 0.05)
			? (PokemonType)Game1.random.Next(1, 19)
			: (species.PrimaryType != PokemonType.Normal ? species.PrimaryType : (species.SecondaryType ?? PokemonType.Normal));

		if (initialMoves != null && initialMoves.Length > 0)
		{
			foreach (string moveName in initialMoves)
			{
				if (Moves.Count < 4)
				{
					Moves.Add(PokemonMove.Get(moveName));
				}
			}
		}
		else
		{
			var canonicalMoves = PokemonCanonicalData.GetMovesForInitialLevel(species.InternalName ?? species.Name, level);
			if (canonicalMoves != null && canonicalMoves.Count > 0)
			{
				foreach (string cm in canonicalMoves)
				{
					if (Moves.Count < 4)
					{
						Moves.Add(PokemonMove.Get(cm));
					}
				}
			}
			else if (species.LearnableMoves != null && species.LearnableMoves.Count > 0)
			{
				int maxCount = (level <= 5) ? Math.Min(2, species.LearnableMoves.Count)
				             : (level <= 12) ? Math.Min(3, species.LearnableMoves.Count)
				             : 4;
				int count = Math.Min(maxCount, species.LearnableMoves.Count);
				int startIdx = 0;
				if (level >= 35 && species.LearnableMoves.Count > 4)
				{
					startIdx = species.LearnableMoves.Count - 4;
				}
				else if (level >= 18 && species.LearnableMoves.Count > 4)
				{
					startIdx = Math.Max(0, (species.LearnableMoves.Count / 2) - 2);
				}
				for (int i = 0; i < count; i++)
				{
					Moves.Add(PokemonMove.Get(species.LearnableMoves[startIdx + i]));
				}
			}
			else
			{
				// Give default moves
				Moves.Add(PokemonMove.Get("Tackle"));
				if (species.PrimaryType == PokemonType.Fire) Moves.Add(PokemonMove.Get("Ember"));
				else if (species.PrimaryType == PokemonType.Water) Moves.Add(PokemonMove.Get("Water Gun"));
				else if (species.PrimaryType == PokemonType.Grass) Moves.Add(PokemonMove.Get("Vine Whip"));
				else if (species.PrimaryType == PokemonType.Electric) Moves.Add(PokemonMove.Get("Thundershock"));
				else if (species.PrimaryType == PokemonType.Psychic) Moves.Add(PokemonMove.Get("Confusion"));
				else if (species.PrimaryType == PokemonType.Ghost) Moves.Add(PokemonMove.Get("Shadow Sneak"));
				else if (species.PrimaryType == PokemonType.Rock) Moves.Add(PokemonMove.Get("Rock Throw"));
				else if (species.PrimaryType == PokemonType.Fighting) Moves.Add(PokemonMove.Get("Karate Chop"));
				else Moves.Add(PokemonMove.Get("Quick Attack"));
			}
		}
	}

	public PokemonInstance(PokemonSpecies species, int level, bool isShiny, params string[] initialMoves)
		: this(species, level, initialMoves)
	{
		IsShiny = isShiny;
	}

	public void GainEffortValues(PokemonSpecies defeated)
	{
		if (defeated == null) return;
		int totalBase = defeated.BaseHP + defeated.BaseAttack + defeated.BaseDefense + defeated.BaseSpAttack + defeated.BaseSpDefense + defeated.BaseSpeed;
		int evYield = totalBase >= 500 ? 3 : (totalBase >= 380 ? 2 : 1);

		PokemonStat bestStat = PokemonStat.HP;
		int bestVal = defeated.BaseHP;
		if (defeated.BaseAttack > bestVal) { bestVal = defeated.BaseAttack; bestStat = PokemonStat.Attack; }
		if (defeated.BaseDefense > bestVal) { bestVal = defeated.BaseDefense; bestStat = PokemonStat.Defense; }
		if (defeated.BaseSpAttack > bestVal) { bestVal = defeated.BaseSpAttack; bestStat = PokemonStat.SpAttack; }
		if (defeated.BaseSpDefense > bestVal) { bestVal = defeated.BaseSpDefense; bestStat = PokemonStat.SpDefense; }
		if (defeated.BaseSpeed > bestVal) { bestVal = defeated.BaseSpeed; bestStat = PokemonStat.Speed; }

		AddEV(bestStat, evYield);
	}

	public void AddEV(PokemonStat stat, int amount)
	{
		if (amount <= 0) return;
		int currentTotal = TotalEVs;
		if (currentTotal >= 510) return;
		int actualAdd = Math.Min(amount, 510 - currentTotal);

		switch (stat)
		{
			case PokemonStat.HP:
				EV_HP = Math.Min(252, EV_HP + actualAdd);
				break;
			case PokemonStat.Attack:
				EV_Attack = Math.Min(252, EV_Attack + actualAdd);
				break;
			case PokemonStat.Defense:
				EV_Defense = Math.Min(252, EV_Defense + actualAdd);
				break;
			case PokemonStat.SpAttack:
				EV_SpAttack = Math.Min(252, EV_SpAttack + actualAdd);
				break;
			case PokemonStat.SpDefense:
				EV_SpDefense = Math.Min(252, EV_SpDefense + actualAdd);
				break;
			case PokemonStat.Speed:
				EV_Speed = Math.Min(252, EV_Speed + actualAdd);
				break;
		}
		RecalculateStats();
	}

	public static string RollDefaultGender(PokemonSpecies species, bool maleRoll)
	{
		if (species == null) return "♂";
		string n = (species.InternalName ?? "").ToLowerInvariant();
		if (n.Contains("nidoran_f") || n.Contains("nidorina") || n.Contains("nidoqueen") || n.Contains("chansey") || n.Contains("blissey") || n.Contains("happiny") || n.Contains("kangaskhan") || n.Contains("jynx") || n.Contains("smoochum") || n.Contains("miltank") || n.Contains("illumise") || n.Contains("latias") || n.Contains("froslass") || n.Contains("cresselia") || n.Contains("tinkatink") || n.Contains("tinkatuff") || n.Contains("tinkaton") || n.Contains("ogerpon"))
			return "♀";
		if (n.Contains("nidoran_m") || n.Contains("nidorino") || n.Contains("nidoking") || n.Contains("hitmon") || n.Contains("tauros") || n.Contains("tyrogue") || n.Contains("volbeat") || n.Contains("latios") || n.Contains("gallade") || n.Contains("grimmsnarl"))
			return "♂";
		if (n.Contains("magnemite") || n.Contains("magneton") || n.Contains("magnezone") || n.Contains("voltorb") || n.Contains("electrode") || n.Contains("staryu") || n.Contains("starmie") || n.Contains("porygon") || n.Contains("ditto") || n.Contains("articuno") || n.Contains("zapdos") || n.Contains("moltres") || n.Contains("mewtwo") || n.Contains("mew") || n.Contains("lugia") || n.Contains("ho_oh") || n.Contains("celebi") || n.Contains("regi") || n.Contains("kyogre") || n.Contains("groudon") || n.Contains("rayquaza") || n.Contains("jirachi") || n.Contains("deoxys") || n.Contains("uxie") || n.Contains("mesprit") || n.Contains("azelf") || n.Contains("dialga") || n.Contains("palkia") || n.Contains("giratina") || n.Contains("darkrai") || n.Contains("shaymin") || n.Contains("arceus") || n.Contains("victini") || n.Contains("reshiram") || n.Contains("zekrom") || n.Contains("kyurem") || n.Contains("xerneas") || n.Contains("yveltal") || n.Contains("zygarde") || n.Contains("solgaleo") || n.Contains("lunala") || n.Contains("necrozma") || n.Contains("zacian") || n.Contains("zamazenta") || n.Contains("eternatus") || n.Contains("koraidon") || n.Contains("miraidon"))
			return "-";
		return maleRoll ? "♂" : "♀";
	}

	public void RecalculateStats(bool healFull = false)
	{
		// Authentic Pokemon Stat formula with Base + IV + EV + Nature
		int baseMaxHp = ((2 * Species.BaseHP + IV_HP + (EV_HP / 4)) * Level / 100) + Level + 10;
		MaxHP = DynamaxTurns > 0 ? baseMaxHp * 2 : baseMaxHp;
		Attack = Math.Max(1, (int)((((2 * Species.BaseAttack + IV_Attack + (EV_Attack / 4)) * Level / 100) + 5) * Nature.GetMultiplier(PokemonStat.Attack)));
		Defense = Math.Max(1, (int)((((2 * Species.BaseDefense + IV_Defense + (EV_Defense / 4)) * Level / 100) + 5) * Nature.GetMultiplier(PokemonStat.Defense)));
		SpAttack = Math.Max(1, (int)((((2 * Species.BaseSpAttack + IV_SpAttack + (EV_SpAttack / 4)) * Level / 100) + 5) * Nature.GetMultiplier(PokemonStat.SpAttack)));
		SpDefense = Math.Max(1, (int)((((2 * Species.BaseSpDefense + IV_SpDefense + (EV_SpDefense / 4)) * Level / 100) + 5) * Nature.GetMultiplier(PokemonStat.SpDefense)));
		Speed = Math.Max(1, (int)((((2 * Species.BaseSpeed + IV_Speed + (EV_Speed / 4)) * Level / 100) + 5) * Nature.GetMultiplier(PokemonStat.Speed)));
		if (healFull)
		{
			CurrentHP = MaxHP;
		}
	}

	public bool CanMegaEvolve()
	{
		if (IsMegaEvolved || Species == null || Species.IsMega) return false;
		var forms = PokemonSpecies.GetMegaForms(Species.BaseSpeciesName ?? Species.InternalName);
		return forms != null && forms.Count > 0;
	}

	public bool MegaEvolve(string megaInternalName, string usedStoneId = null)
	{
		PokemonSpecies target = !string.IsNullOrWhiteSpace(megaInternalName) ? PokemonSpecies.Get(megaInternalName) : null;
		return MegaEvolve(target, usedStoneId);
	}

	public bool MegaEvolve(PokemonSpecies megaTarget = null, string usedStoneId = null)
	{
		if (IsMegaEvolved || Species == null) return false;
		if (megaTarget == null)
		{
			var forms = PokemonSpecies.GetMegaForms(Species.BaseSpeciesName ?? Species.InternalName);
			if (forms == null || forms.Count == 0) return false;
			megaTarget = forms[0];
		}
		PreMegaSpecies = Species;
		if (!string.IsNullOrEmpty(usedStoneId))
		{
			UsedMegaStoneId = usedStoneId;
		}
		bool updateNick = string.Equals(Nickname, Species.Name, StringComparison.OrdinalIgnoreCase);
		Species = megaTarget;
		if (updateNick) Nickname = megaTarget.Name;
		IsMegaEvolved = true;
		Ability = PokemonAbility.GetDefaultAbility(megaTarget, isMega: true);
		float ratio = HpRatio;
		RecalculateStats();
		CurrentHP = Math.Clamp((int)Math.Round(MaxHP * ratio), 1, MaxHP);
		InvalidateSpriteCache();
		return true;
	}

	/// <summary>
	/// Reverts a Mega Evolved Pokémon back to its normal species outside of battle,
	/// restoring its base stats and typing and returning the Mega Stone ID to give back to the player.
	/// </summary>
	public bool RevertMegaEvolution(out string returnedStoneId)
	{
		returnedStoneId = null;
		if (!IsMegaEvolved && (Species == null || !Species.IsMega))
			return false;

		PokemonSpecies baseSpec = PreMegaSpecies;
		if (baseSpec == null && Species != null && !string.IsNullOrEmpty(Species.BaseSpeciesName))
		{
			baseSpec = PokemonSpecies.Get(Species.BaseSpeciesName);
		}
		if (baseSpec == null || baseSpec == Species)
			return false;

		returnedStoneId = !string.IsNullOrEmpty(UsedMegaStoneId)
			? UsedMegaStoneId
			: PokemonItemManager.GetMegaStoneIdForSpecies(baseSpec.InternalName, Species?.InternalName);

		bool updateNick = string.Equals(Nickname, Species.Name, StringComparison.OrdinalIgnoreCase);
		Species = baseSpec;
		if (updateNick) Nickname = baseSpec.Name;
		PreMegaSpecies = null;
		UsedMegaStoneId = null;
		IsMegaEvolved = false;
		IsBattleOnlyMega = false;
		Ability = PokemonAbility.GetDefaultAbility(baseSpec, isMega: false);
		float ratio = HpRatio;
		RecalculateStats();
		CurrentHP = Math.Clamp((int)Math.Round(MaxHP * ratio), IsFainted ? 0 : 1, MaxHP);
		InvalidateSpriteCache();
		return true;
	}

	public bool ChangeForm(PokemonSpecies targetForm, string signatureMove = null)
	{
		if (targetForm == null || Species == null || targetForm == Species)
			return false;

		string oldBaseName = !string.IsNullOrEmpty(Species.BaseSpeciesName) ? Species.BaseSpeciesName : Species.Name;
		bool updateNick = string.IsNullOrWhiteSpace(Nickname) ||
		                  string.Equals(Nickname, Species.Name, StringComparison.OrdinalIgnoreCase) ||
		                  string.Equals(Nickname, oldBaseName, StringComparison.OrdinalIgnoreCase);

		Species = targetForm;
		if (updateNick) Nickname = targetForm.Name;
		IsMegaEvolved = false;
		IsBattleOnlyMega = false;
		PreMegaSpecies = null;
		Ability = PokemonAbility.GetDefaultAbility(targetForm, isMega: false);

		float ratio = HpRatio;
		RecalculateStats();
		CurrentHP = Math.Clamp((int)Math.Round(MaxHP * ratio), IsFainted ? 0 : 1, MaxHP);

		if (!string.IsNullOrWhiteSpace(signatureMove))
		{
			var sigMoveObj = PokemonMove.Get(signatureMove);
			if (sigMoveObj != null && !Moves.Exists(m => string.Equals(m.Name, sigMoveObj.Name, StringComparison.OrdinalIgnoreCase)))
			{
				if (Moves.Count < 4)
					Moves.Insert(0, sigMoveObj.Clone());
				else
					Moves[0] = sigMoveObj.Clone();
			}
		}

		InvalidateSpriteCache();
		return true;
	}

	public void OnHeldItemChanged(string oldHeld, string newHeld)
	{
		string oldClean = PokemonHeldItemManager.NormalizeId(oldHeld ?? "").ToLowerInvariant();
		string newClean = PokemonHeldItemManager.NormalizeId(newHeld ?? "").ToLowerInvariant();
		if (string.Equals(oldClean, newClean, StringComparison.OrdinalIgnoreCase)) return;

		string baseSp = (Species?.BaseSpeciesName ?? Species?.InternalName ?? "").ToLowerInvariant();

		// 1. Mega Rayquaza revert upon removing Meteorite
		if (baseSp == "rayquaza" && (oldClean is "meteoritepiece" or "rayquazite") && newClean is not ("meteoritepiece" or "rayquazite"))
		{
			RevertMegaEvolution(out _);
			var baseRay = PokemonSpecies.Get("rayquaza");
			if (baseRay != null)
			{
				Species = baseRay;
				if (Nickname == "Mega Rayquaza" || Nickname == "Rayquaza") Nickname = "Rayquaza";
				Ability = PokemonAbility.GetDefaultAbility(baseRay, isMega: false);
			}
			RecalculateStats();
			InvalidateSpriteCache();
		}

		// 2. Giratina Origin <-> Altered
		if (baseSp == "giratina")
		{
			bool hadOrb = oldClean is "griseousorb" or "griseouscore";
			bool hasOrb = newClean is "griseousorb" or "griseouscore";
			if (hasOrb && !hadOrb)
			{
				var origin = PokemonSpecies.Get("giratina/origin");
				if (origin != null) ChangeForm(origin);
			}
			else if (hadOrb && !hasOrb)
			{
				var altered = PokemonSpecies.Get("giratina");
				if (altered != null) ChangeForm(altered);
			}
		}

		// 3. Dialga Origin <-> Normal
		if (baseSp == "dialga")
		{
			bool hadItem = oldClean is "adamantcrystal" or "adamantorb";
			bool hasItem = newClean is "adamantcrystal" or "adamantorb";
			if (hasItem && !hadItem)
			{
				var origin = PokemonSpecies.Get("dialga/origin");
				if (origin != null) ChangeForm(origin);
			}
			else if (hadItem && !hasItem)
			{
				var normal = PokemonSpecies.Get("dialga");
				if (normal != null) ChangeForm(normal);
			}
		}

		// 4. Palkia Origin <-> Normal
		if (baseSp == "palkia")
		{
			bool hadItem = oldClean is "lustrousglobe" or "lustrousorb";
			bool hasItem = newClean is "lustrousglobe" or "lustrousorb";
			if (hasItem && !hadItem)
			{
				var origin = PokemonSpecies.Get("palkia/origin");
				if (origin != null) ChangeForm(origin);
			}
			else if (hadItem && !hasItem)
			{
				var normal = PokemonSpecies.Get("palkia");
				if (normal != null) ChangeForm(normal);
			}
		}

		// 5. Zacian Crowned <-> Hero
		if (baseSp == "zacian")
		{
			bool hadItem = oldClean == "rustedsword";
			bool hasItem = newClean == "rustedsword";
			if (hasItem && !hadItem)
			{
				var crowned = PokemonSpecies.Get("zacian/crowned");
				if (crowned != null) ChangeForm(crowned);
			}
			else if (hadItem && !hasItem)
			{
				var normal = PokemonSpecies.Get("zacian");
				if (normal != null) ChangeForm(normal);
			}
		}

		// 6. Zamazenta Crowned <-> Hero
		if (baseSp == "zamazenta")
		{
			bool hadItem = oldClean == "rustedshield";
			bool hasItem = newClean == "rustedshield";
			if (hasItem && !hadItem)
			{
				var crowned = PokemonSpecies.Get("zamazenta/crowned");
				if (crowned != null) ChangeForm(crowned);
			}
			else if (hadItem && !hasItem)
			{
				var normal = PokemonSpecies.Get("zamazenta");
				if (normal != null) ChangeForm(normal);
			}
		}

		// 7. Ogerpon Masks
		if (baseSp == "ogerpon")
		{
			string targetFormKey = newClean switch
			{
				"wellspringmask" => "ogerpon/wellspring",
				"hearthflamemask" => "ogerpon/hearthflame",
				"cornerstonemask" => "ogerpon/cornerstone",
				_ => "ogerpon"
			};
			var targetSpec = PokemonSpecies.Get(targetFormKey);
			if (targetSpec != null) ChangeForm(targetSpec);
		}

		// 8. Shaymin Flor Gracidea
		if (baseSp == "shaymin")
		{
			bool hadItem = oldClean is "gracidea" or "gracideaflower";
			bool hasItem = newClean is "gracidea" or "gracideaflower";
			if (hasItem && !hadItem && (Game1.timeOfDay >= 600 && Game1.timeOfDay < 1800))
			{
				var sky = PokemonSpecies.Get("shaymin/sky");
				if (sky != null) ChangeForm(sky, "Air Slash");
			}
			else if (hadItem && !hasItem)
			{
				var land = PokemonSpecies.Get("shaymin");
				if (land != null) ChangeForm(land, "Seed Flare");
			}
		}

		// 9. Kyogre & Groudon Primal abilities safety check
		if (baseSp == "kyogre")
		{
			if (newClean != "blueorb" && Ability == "Primordial Sea")
			{
				Ability = "Drizzle";
			}
		}
		if (baseSp == "groudon")
		{
			if (newClean != "redorb" && Ability == "Desolate Land")
			{
				Ability = "Drought";
			}
		}

		InvalidateSpriteCache();
	}

	public bool ActivateDynamax(bool asGigantamax = false)
	{
		if (DynamaxTurns > 0 || IsFainted) return false;
		DynamaxTurns = 3;
		IsUsingGigantamax = asGigantamax && Species != null && Species.HasGigantamax;
		RecalculateStats();
		CurrentHP = Math.Min(MaxHP, CurrentHP * 2);
		InvalidateSpriteCache();
		return true;
	}

	public bool TickDynamax()
	{
		if (DynamaxTurns <= 0) return false;
		DynamaxTurns--;
		if (DynamaxTurns <= 0)
		{
			DynamaxTurns = 0;
			IsUsingGigantamax = false;
			RecalculateStats();
			CurrentHP = Math.Clamp((CurrentHP + 1) / 2, IsFainted ? 0 : 1, MaxHP);
			InvalidateSpriteCache();
			return true; // Reverted from Dynamax
		}
		return false;
	}

	public void EndBattleGimmicks(bool revertMega = false)
	{
		ConfusionTurns = 0;
		RampageTurns = 0;
		RampageMoveName = null;
		Flinched = false;
		SaltCureTurns = 0;
		bool changed = false;
		if (DynamaxTurns > 0 || IsUsingGigantamax)
		{
			DynamaxTurns = 0;
			IsUsingGigantamax = false;
			RecalculateStats();
			CurrentHP = Math.Clamp((CurrentHP + 1) / 2, IsFainted ? 0 : 1, MaxHP);
			changed = true;
		}
		if ((revertMega || IsBattleOnlyMega) && IsMegaEvolved && PreMegaSpecies != null)
		{
			bool updateNick = string.Equals(Nickname, Species.Name, StringComparison.OrdinalIgnoreCase);
			Species = PreMegaSpecies;
			if (updateNick) Nickname = PreMegaSpecies.Name;
			Ability = PokemonAbility.GetDefaultAbility(PreMegaSpecies, isMega: false);
			PreMegaSpecies = null;
			IsMegaEvolved = false;
			IsBattleOnlyMega = false;
			RecalculateStats();
			CurrentHP = Math.Min(MaxHP, CurrentHP);
			changed = true;
		}
		if (IsTerastallized)
		{
			IsTerastallized = false;
			changed = true;
		}
		if (changed) InvalidateSpriteCache();
	}

	public void TakeDamage(int damage)
	{
		CurrentHP = Math.Max(0, CurrentHP - damage);
	}

	public bool GainLevel()
	{
		if (Level >= 100) return false;
		bool wasFainted = IsFainted;
		int oldMax = MaxHP;
		Level++;
		CurrentExp = 0;
		RecalculateStats();
		if (!wasFainted) CurrentHP = Math.Min(MaxHP, CurrentHP + Math.Max(0, MaxHP - oldMax));
		return true;
	}

	public static string GetMoveLearnedAtLevel(PokemonSpecies species, int level)
	{
		if (species == null) return null;

		// 1. Canonical database check (Gen 1-9 pokeemerald-expansion data)
		string canonical = PokemonCanonicalData.GetMoveLearnedAtLevel(species.InternalName ?? species.Name, level);
		if (!string.IsNullOrEmpty(canonical)) return canonical;

		if (species.LearnableMoves == null || species.LearnableMoves.Count == 0) return null;

		string sp = species.InternalName?.ToLowerInvariant() ?? "";
		if (sp == "quaxly")
		{
			return level switch
			{
				5 => "Water Gun",
				10 => "Wing Attack",
				17 => "Aqua Cutter",
				24 => "Low Sweep",
				32 => "Water Pulse",
				38 => "Scald",
				45 => "Hydro Pump",
				_ => null
			};
		}
		if (sp == "sprigatito")
		{
			return level switch
			{
				5 => "Leafage",
				10 => "Bite",
				15 => "Quick Attack",
				20 => "Seed Bomb",
				28 => "Slash",
				36 => "Energy Ball",
				45 => "Leaf Storm",
				_ => null
			};
		}
		if (sp == "fuecoco")
		{
			return level switch
			{
				5 => "Ember",
				11 => "Round",
				15 => "Bite",
				20 => "Incinerate",
				28 => "Flamethrower",
				36 => "Hyper Voice",
				45 => "Overheat",
				_ => null
			};
		}

		int targetIndex = level switch
		{
			8 => 1,
			14 => 2,
			20 => 3,
			28 => 4,
			36 => 5,
			45 => 6,
			55 => 7,
			_ => -1
		};

		if (targetIndex >= 0 && targetIndex < species.LearnableMoves.Count)
		{
			return species.LearnableMoves[targetIndex];
		}

		return null;
	}

	public void Heal(int amount)
	{
		CurrentHP = Math.Min(MaxHP, CurrentHP + amount);
	}

	public void FullRestore()
	{
		EndBattleGimmicks(revertMega: false);
		Status = PokemonStatusCondition.None;
		StatusTurns = 0;
		ConfusionTurns = 0;
		CurrentHP = MaxHP;
		foreach (var move in Moves)
		{
			move.CurrentPP = move.MaxPP;
		}
	}

	private static string _cachedExpansionDir;
	private static string _cachedPokemonBaseDir;

	public static string FindPokemonFolder(string speciesName)
	{
		if (string.IsNullOrEmpty(speciesName)) return null;
		string normName = speciesName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

		if (_cachedExpansionDir != null && Directory.Exists(_cachedExpansionDir))
		{
			string candidate = Path.Combine(_cachedExpansionDir, normName);
			if (Directory.Exists(candidate)) return candidate;
		}
		if (_cachedPokemonBaseDir != null && Directory.Exists(_cachedPokemonBaseDir))
		{
			string candidate = Path.Combine(_cachedPokemonBaseDir, normName);
			if (Directory.Exists(candidate)) return candidate;
		}

		string[] searchRoots = new string[]
		{
			AppDomain.CurrentDomain.BaseDirectory,
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".."),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")
		};

		foreach (var root in searchRoots)
		{
			try
			{
				string fullRoot = Path.GetFullPath(root);
				string pExp = Path.Combine(fullRoot, "pokeemerald-expansion", "graphics", "pokemon");
				if (Directory.Exists(pExp))
				{
					_cachedExpansionDir = pExp;
					string candidate = Path.Combine(pExp, normName);
					if (Directory.Exists(candidate)) return candidate;
				}
				string p1 = Path.Combine(fullRoot, "pokeemerald", "graphics", "pokemon");
				if (Directory.Exists(p1))
				{
					_cachedPokemonBaseDir = p1;
					string candidate = Path.Combine(p1, normName);
					if (Directory.Exists(candidate)) return candidate;
				}
				string p2 = Path.Combine(fullRoot, "pokefirered", "graphics", "pokemon");
				if (Directory.Exists(p2))
				{
					string candidate = Path.Combine(p2, normName);
					if (Directory.Exists(candidate)) return candidate;
				}
				string p3 = Path.Combine(fullRoot, "pokeplatinum", "res", "pokemon");
				if (Directory.Exists(p3))
				{
					string candidate = Path.Combine(p3, normName);
					if (Directory.Exists(candidate)) return candidate;
				}
				string p4 = Path.Combine(fullRoot, "pokeheartgold", "files", "poketool", "pokegra");
				if (Directory.Exists(p4))
				{
					string candidate = Path.Combine(p4, normName);
					if (Directory.Exists(candidate)) return candidate;
				}
			}
			catch { }
		}

		return null;
	}

	private static List<Color> ParseJascPal(string palPath)
	{
		if (string.IsNullOrEmpty(palPath) || !File.Exists(palPath)) return null;
		try
		{
			var lines = File.ReadAllLines(palPath);
			var colors = new List<Color>();
			for (int i = 3; i < lines.Length; i++)
			{
				string line = lines[i].Trim();
				if (string.IsNullOrEmpty(line)) continue;
				var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 3 &&
					int.TryParse(parts[0], out int r) &&
					int.TryParse(parts[1], out int g) &&
					int.TryParse(parts[2], out int b))
				{
					colors.Add(new Color(r, g, b));
				}
			}
			return colors.Count > 0 ? colors : null;
		}
		catch
		{
			return null;
		}
	}

	private static readonly Dictionary<string, (Texture2D tex, int bottomY)> _sharedSpriteCache = new(StringComparer.OrdinalIgnoreCase);

	public static Texture2D LoadSpriteWithColorKey(
		GraphicsDevice device,
		string path,
		bool isShiny,
		string baseDir,
		out int bottomOpaqueY,
		bool isSharedPaletteSprite = false,
		bool isDynamax = false)
	{
		string cacheKey = $"{path}_{(isShiny ? "s" : "n")}_{(isDynamax ? "d" : "x")}";
		if (_sharedSpriteCache.TryGetValue(cacheKey, out var cached) && cached.tex != null && !cached.tex.IsDisposed)
		{
			bottomOpaqueY = cached.bottomY;
			return cached.tex;
		}
		using Stream stream = File.OpenRead(path);
		Texture2D tex = Texture2D.FromStream(device, stream);
		Color[] data = new Color[tex.Width * tex.Height];
		tex.GetData(data);
		bool modified = false;

		if (data.Length > 0)
		{
			Color keyColor = data[0];
			if (keyColor.A > 0)
			{
				for (int i = 0; i < data.Length; i++)
				{
					if (data[i].R == keyColor.R && data[i].G == keyColor.G && data[i].B == keyColor.B)
					{
						data[i] = Color.Transparent;
						modified = true;
					}
				}
			}

			List<Color> normalPal = null;
			List<Color> shinyPal = null;
			if (!string.IsNullOrEmpty(baseDir))
			{
				if (path.Contains("overworld"))
				{
					normalPal = ParseJascPal(Path.Combine(baseDir, "overworld_normal.pal"));
					shinyPal = ParseJascPal(Path.Combine(baseDir, "overworld_shiny.pal"));
				}
				if (normalPal == null || shinyPal == null)
				{
					normalPal = ParseJascPal(Path.Combine(baseDir, "normal.pal")) ?? ParseJascPal(Path.Combine(baseDir, "normal_gba.pal"));
					shinyPal = ParseJascPal(Path.Combine(baseDir, "shiny.pal")) ?? ParseJascPal(Path.Combine(baseDir, "shiny_gba.pal"));
				}
			}

			if (normalPal != null && shinyPal != null && normalPal.Count == shinyPal.Count)
			{
				int palCount = normalPal.Count;
				int shinyMatch = 0;
				int normalMatch = 0;
				for (int i = 0; i < data.Length; i += 4)
				{
					Color c = data[i];
					if (c.A == 0) continue;
					for (int p = 1; p < palCount; p++)
					{
						Color np = normalPal[p];
						Color sp = shinyPal[p];
						if (Math.Abs(c.R - sp.R) + Math.Abs(c.G - sp.G) + Math.Abs(c.B - sp.B) <= 18) shinyMatch++;
						if (Math.Abs(c.R - np.R) + Math.Abs(c.G - np.G) + Math.Abs(c.B - np.B) <= 18) normalMatch++;
					}
				}

				bool fileAuthoredAsShiny = (shinyMatch > normalMatch + 15);

				if (isShiny)
				{
					if (fileAuthoredAsShiny)
					{
						// File on disk was already authored with shiny colors (e.g. Mega Gengar, Cloyster back sprites).
						// It is already shiny, preserve authentic shiny appearance!
					}
					else
					{
						bool isIconFile = path.Contains("icon") || isSharedPaletteSprite;
						int maxMatchDist = isIconFile ? 45000 : 1800;

						for (int i = 0; i < data.Length; i++)
						{
							Color c = data[i];
							if (c.A <= 8) continue;
							// Preserve dark pixel art outlines
							if (c.R <= 32 && c.G <= 32 && c.B <= 32) continue;

							int bestIdx = -1;
							int bestDist = int.MaxValue;
							for (int p = 1; p < palCount; p++)
							{
								Color np = normalPal[p];
								int dr = c.R - np.R;
								int dg = c.G - np.G;
								int db = c.B - np.B;
								int dist = dr * dr * 2 + dg * dg * 4 + db * db * 3;
								if (dist < bestDist)
								{
									bestDist = dist;
									bestIdx = p;
									if (dist == 0) break;
								}
							}

							if (bestIdx >= 1 && bestDist < maxMatchDist)
							{
								Color np = normalPal[bestIdx];
								Color sp = shinyPal[bestIdx];

								if (bestDist < 600)
								{
									// Direct authentic palette color
									data[i] = new Color(sp.R, sp.G, sp.B, c.A);
									modified = true;
								}
								else
								{
									// Smooth chromatic shift for anti-aliased and shaded overworld pixels
									int dR = sp.R - np.R;
									int dG = sp.G - np.G;
									int dB = sp.B - np.B;
									int nr = Math.Clamp(c.R + dR, 0, 255);
									int ng = Math.Clamp(c.G + dG, 0, 255);
									int nb = Math.Clamp(c.B + dB, 0, 255);
									data[i] = new Color((byte)nr, (byte)ng, (byte)nb, c.A);
									modified = true;
								}
							}
						}
					}
				}
				else if (!isSharedPaletteSprite)
				{
					if (fileAuthoredAsShiny)
					{
						// Inadvertently saved with shiny colors on disk! Remap to canonical normal.pal colors
						for (int i = 0; i < data.Length; i++)
						{
							Color c = data[i];
							if (c.A == 0) continue;
							int bestIdx = -1;
							int bestDist = 42;
							for (int p = 1; p < palCount; p++)
							{
								Color sp = shinyPal[p];
								int dist = Math.Abs(c.R - sp.R) + Math.Abs(c.G - sp.G) + Math.Abs(c.B - sp.B);
								if (dist < bestDist)
								{
									bestDist = dist;
									bestIdx = p;
									if (dist == 0) break;
								}
							}
							if (bestIdx >= 1)
							{
								Color np = normalPal[bestIdx];
								data[i] = new Color(np.R, np.G, np.B, c.A);
								modified = true;
							}
						}
					}
				}
			}

			// Dynamax visual transformation: crimson/magenta Dynamax energy infusion & glowing rim outline
			if (isDynamax)
			{
				Color[] srcCopy = (Color[])data.Clone();
				int w = tex.Width;
				int h = tex.Height;
				for (int y = 0; y < h; y++)
				{
					int row = y * w;
					for (int x = 0; x < w; x++)
					{
						int idx = row + x;
						Color c = srcCopy[idx];
						if (c.A > 16)
						{
							// Check if edge pixel (adjacent to transparent) for crimson Dynamax energy rim
							bool isEdge = (x > 0 && srcCopy[idx - 1].A <= 16) ||
							              (x < w - 1 && srcCopy[idx + 1].A <= 16) ||
							              (y > 0 && srcCopy[idx - w].A <= 16) ||
							              (y < h - 1 && srcCopy[idx + w].A <= 16);
							if (isEdge)
							{
								data[idx] = new Color(
									(byte)Math.Clamp(c.R / 2 + 135, 0, 255),
									(byte)Math.Clamp(c.G / 3 + 15, 0, 255),
									(byte)Math.Clamp(c.B / 2 + 75, 0, 255),
									c.A);
							}
							else
							{
								data[idx] = new Color(
									(byte)Math.Clamp((int)(c.R * 0.85f + 42), 0, 255),
									(byte)Math.Clamp((int)(c.G * 0.82f + 6), 0, 255),
									(byte)Math.Clamp((int)(c.B * 0.85f + 26), 0, 255),
									c.A);
							}
							modified = true;
						}
					}
				}
			}

			if (modified)
			{
				tex.SetData(data);
			}
		}

		// Compute bottom-most opaque pixel row in Frame 0 so sprites never float in battle!
		int frameW = tex.Width;
		int frameH = tex.Height;
		if (tex.Height >= 128 && tex.Width == 64) { frameW = 64; frameH = 64; }
		else if (tex.Width >= 160 && tex.Height == 80) { frameW = 80; frameH = 80; }
		else if (tex.Height >= 64 && tex.Width == 64) { frameW = 64; frameH = 64; }

		bottomOpaqueY = frameH - 1;
		bool found = false;
		for (int y = frameH - 1; y >= 0 && !found; y--)
		{
			int rowStart = y * tex.Width;
			for (int x = 0; x < frameW; x++)
			{
				if (data[rowStart + x].A > 16)
				{
					bottomOpaqueY = y;
					found = true;
					break;
				}
			}
		}

		_sharedSpriteCache[cacheKey] = (tex, bottomOpaqueY);
		return tex;
	}

	public void Revive(float percent = 0.5f)
	{
		CurrentHP = Math.Max(1, (int)(MaxHP * percent));
	}

	private void CheckDynamicSpriteState()
	{
		if (_cachedSpriteShinyState != IsShiny || _cachedSpriteGmaxState != IsGigantamaxed || _cachedSpriteDynamaxState != IsDynamaxed || _cachedSpriteIsEgg != IsEgg)
		{
			InvalidateSpriteCache();
			_cachedSpriteShinyState = IsShiny;
			_cachedSpriteGmaxState = IsGigantamaxed;
			_cachedSpriteDynamaxState = IsDynamaxed;
			_cachedSpriteIsEgg = IsEgg;
		}
	}

	private string ResolveBattleSpriteFolder()
	{
		if (IsEgg) return "egg";
		if (IsGigantamaxed)
		{
			// Dedicated Gigantamax sprite folder when Gigantamaxed!
			string gmaxFolder = FindPokemonFolder(Species.InternalName + "/gmax");
			if (gmaxFolder != null) return Species.InternalName + "/gmax";
			if (!string.IsNullOrEmpty(Species.BaseSpeciesName))
			{
				string baseGmax = FindPokemonFolder(Species.BaseSpeciesName + "/gmax");
				if (baseGmax != null) return Species.BaseSpeciesName + "/gmax";
			}
		}
		return Species.InternalName;
	}

	public Texture2D GetFrontSprite(GraphicsDevice device)
	{
		CheckDynamicSpriteState();
		if (_frontSprite != null && !_frontSprite.IsDisposed) return _frontSprite;

		string folder = ResolveBattleSpriteFolder();
		string basePath = FindPokemonFolder(folder) ?? FindPokemonFolder(Species.InternalName);
		if (basePath != null)
		{
			string[] candidates = new[]
			{
				Path.Combine(basePath, "anim_front.png"),
				Path.Combine(basePath, "front.png"),
				Path.Combine(basePath, "male_front.png"),
				Path.Combine(basePath, "female_front.png")
			};
			foreach (string candidate in candidates)
			{
				if (File.Exists(candidate))
				{
					try
					{
						_frontSprite = LoadSpriteWithColorKey(device, candidate, !IsEgg && IsShiny, basePath, out _frontBottomY, isSharedPaletteSprite: false, isDynamax: !IsEgg && IsDynamaxed);
						return _frontSprite;
					}
					catch { }
				}
			}
		}
		return null;
	}

	public Texture2D GetBackSprite(GraphicsDevice device)
	{
		CheckDynamicSpriteState();
		if (_backSprite != null && !_backSprite.IsDisposed) return _backSprite;

		string folder = ResolveBattleSpriteFolder();
		string basePath = FindPokemonFolder(folder) ?? FindPokemonFolder(Species.InternalName);
		if (basePath != null)
		{
			string[] candidates = new[]
			{
				Path.Combine(basePath, "back.png"),
				Path.Combine(basePath, "male_back.png"),
				Path.Combine(basePath, "female_back.png"),
				Path.Combine(basePath, "front.png")
			};
			foreach (string candidate in candidates)
			{
				if (File.Exists(candidate))
				{
					try
					{
						_backSprite = LoadSpriteWithColorKey(device, candidate, !IsEgg && IsShiny, basePath, out _backBottomY, isSharedPaletteSprite: false, isDynamax: !IsEgg && IsDynamaxed);
						return _backSprite;
					}
					catch { }
				}
			}
		}
		return null;
	}

	public Texture2D GetIconSprite(GraphicsDevice device)
	{
		CheckDynamicSpriteState();
		if (_iconSprite != null && !_iconSprite.IsDisposed) return _iconSprite;

		string folder = ResolveBattleSpriteFolder();
		string basePath = FindPokemonFolder(folder) ?? FindPokemonFolder(Species.InternalName);
		if (basePath != null)
		{
			string path = Path.Combine(basePath, "icon.png");
			if (!File.Exists(path) && !string.IsNullOrEmpty(Species.BaseSpeciesName))
			{
				string baseFolder = FindPokemonFolder(Species.BaseSpeciesName);
				if (baseFolder != null)
				{
					string basePathIcon = Path.Combine(baseFolder, "icon.png");
					if (File.Exists(basePathIcon))
					{
						path = basePathIcon;
						basePath = baseFolder;
					}
				}
			}

			if (File.Exists(path))
			{
				try
				{
					_iconSprite = LoadSpriteWithColorKey(device, path, !IsEgg && IsShiny, basePath, out _, isSharedPaletteSprite: true, isDynamax: false);
					return _iconSprite;
				}
				catch { }
			}
		}
		return null;
	}

	public int GetFrontBottomY(GraphicsDevice device)
	{
		if (_frontBottomY < 0) GetFrontSprite(device);
		return _frontBottomY > 0 ? _frontBottomY : 63;
	}

	public int GetBackBottomY(GraphicsDevice device)
	{
		if (_backBottomY < 0) GetBackSprite(device);
		return _backBottomY > 0 ? _backBottomY : 63;
	}

	public Rectangle GetIconSourceRect()
	{
		if (IsEgg) return new Rectangle(0, 0, 32, 32);
		int frame = (int)((Game1.currentGameTime.TotalGameTime.TotalSeconds * 1.15) % 2);
		return new Rectangle(0, frame * 32, 32, 32);
	}

	public Rectangle GetFrontSourceRect()
	{
		if (_frontSprite != null)
		{
			if (_frontSprite.Height >= 128 && _frontSprite.Width == 64)
			{
				int frame = (int)((Game1.currentGameTime.TotalGameTime.TotalSeconds * 1.15) % 2);
				return new Rectangle(0, frame * 64, 64, 64);
			}
			if (_frontSprite.Height >= 64 && _frontSprite.Width == 64)
			{
				return new Rectangle(0, 0, 64, 64);
			}
			if (_frontSprite.Width >= 160 && _frontSprite.Height == 80)
			{
				int frame = (int)((Game1.currentGameTime.TotalGameTime.TotalSeconds * 1.15) % 2);
				return new Rectangle(frame * 80, 0, 80, 80);
			}
			return new Rectangle(0, 0, _frontSprite.Width, _frontSprite.Height);
		}
		return new Rectangle(0, 0, 64, 64);
	}

	public Rectangle GetBackSourceRect()
	{
		if (_backSprite != null)
		{
			if (_backSprite.Height >= 128 && _backSprite.Width == 64)
			{
				int frame = (int)((Game1.currentGameTime.TotalGameTime.TotalSeconds * 1.15) % 2);
				return new Rectangle(0, frame * 64, 64, 64);
			}
			if (_backSprite.Height >= 64 && _backSprite.Width == 64)
			{
				return new Rectangle(0, 0, 64, 64);
			}
			if (_backSprite.Width >= 160 && _backSprite.Height == 80)
			{
				int frame = (int)((Game1.currentGameTime.TotalGameTime.TotalSeconds * 1.15) % 2);
				return new Rectangle(frame * 80, 0, 80, 80);
			}
			return new Rectangle(0, 0, _backSprite.Width, _backSprite.Height);
		}
		return new Rectangle(0, 0, 64, 64);
	}

	/// <summary>
	/// Calculates a proportional overworld / follower scale based on species height registered in Pokédex.
	/// Tiny Pokémon (0.2m) stay cute and nimble, standard Pokémon scale around 2.0x - 2.4x,
	/// and giant titans (Dialga, Gyarados, Rayquaza) scale majestically up to 3.4x.
	/// </summary>
	public static float GetOverworldScale(PokemonInstance inst, bool isIcon = true)
	{
		if (inst == null || inst.Species == null) return isIcon ? 2.65f : 1.35f;
		float height = 1.0f;
		try
		{
			var entry = PokemonPokedex.Instance.GetEntry(inst.Species.Name);
			if (entry != null && entry.HeightMeters > 0.05f)
			{
				height = entry.HeightMeters;
			}
		}
		catch { }

		float scale = Math.Clamp(2.65f + (float)Math.Log(Math.Max(0.25f, height)) * 0.55f, 2.15f, 4.35f);
		return isIcon ? scale : (scale * 0.62f);
	}
}
