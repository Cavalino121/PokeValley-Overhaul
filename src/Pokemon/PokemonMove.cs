using System;
using System.Collections.Generic;
using System.Text;

namespace StardewValley.Pokemon;

public enum MoveCategory
{
	Physical,
	Special,
	Status
}

public enum PokemonStatusCondition
{
	None,
	Paralysis,
	Sleep,
	Poison,
	Toxic,
	Burn,
	Freeze
}

/// <summary>Stats that can be raised or lowered during a battle.</summary>
/// <summary>How a damaging move's base power is calculated at runtime.</summary>
public enum MovePowerScaling
{
	None,
	/// <summary>Eruption, Water Spout, Dragon Energy: stronger while the user is healthy.</summary>
	UserHp,
	/// <summary>Wring Out, Crush Grip, Hard Press: stronger while the target is healthy.</summary>
	TargetHp,
	/// <summary>Gyro Ball: stronger the faster the target is compared to the user.</summary>
	GyroBall,
	/// <summary>Electro Ball: stronger the faster the user is compared to the target.</summary>
	ElectroBall,
	/// <summary>Stored Power, Power Trip: +20 power per positive stage on the user.</summary>
	UserBoosts,
	/// <summary>Punishment: +20 power per positive stage on the target.</summary>
	TargetBoosts
}

/// <summary>Moves whose damage ignores the normal formula.</summary>
public enum FixedDamageMode
{
	None,
	/// <summary>Ruination, Nature's Madness, Super Fang: half of the target's current HP.</summary>
	HalfCurrentHp,
	/// <summary>Night Shade, Seismic Toss: damage equal to the user's level.</summary>
	Level,
	/// <summary>Dragon Rage, Sonic Boom: a flat amount.</summary>
	Flat
}

/// <summary>Extra damage condition evaluated after the base power is known.</summary>
public enum MoveBonusCondition
{	None,
	/// <summary>Brine: doubles against a target at or below half HP.</summary>
	TargetBelowHalfHp,
	/// <summary>Hex, Infernal Parade: doubles against any statused target.</summary>
	TargetHasStatus,
	VenoshockTargetPoisoned,
	SmellingSaltsTargetParalyzed,
	WakeUpSlapTargetAsleep
}

/// <summary>One stat change carried by a move.</summary>
public readonly struct StatStageChange
{
	public readonly PokemonStat Stat;
	public readonly int Stages;
	public readonly int Chance;

	public StatStageChange(PokemonStat stat, int stages, int chance = 100)
	{
		Stat = stat;
		Stages = stages;
		Chance = chance;
	}
}

public class PokemonMove
{
	public string Name { get; set; }
	public PokemonType Type { get; set; }
	public int Power { get; set; }
	public int Accuracy { get; set; }
	public int MaxPP { get; set; }
	public int CurrentPP { get; set; }
	public MoveCategory Category { get; set; }
	/// <summary>Bracket used for turn order (-6..+5). Positive values move first.</summary>
	public int Priority { get; set; } = 0;
	public PokemonStatusCondition StatusEffect { get; set; } = PokemonStatusCondition.None;
	public int StatusChance { get; set; } = 0;
	public bool InflictsConfusion { get; set; } = false;
	public int ConfusionChance { get; set; } = 0;
	public float DrainRatio { get; set; } = 0f;
	public float SelfHealRatio { get; set; } = 0f;

	// ---- Advanced secondary effects -------------------------------------
	/// <summary>Stages applied to the user (Shell Smash, Close Combat, Flame Charge...).</summary>
	public List<StatStageChange> SelfStages { get; } = new();
	/// <summary>Stages applied to the target (Acid Spray, Growl, Crunch...).</summary>
	public List<StatStageChange> TargetStages { get; } = new();
	public int FlinchChance { get; set; } = 0;
	public int HitCountMin { get; set; } = 1;
	public int HitCountMax { get; set; } = 1;
	/// <summary>Fraction of the damage dealt that the user takes back.</summary>
	public float RecoilRatio { get; set; } = 0f;
	/// <summary>Fraction of the user's maximum HP paid to use the move (Belly Drum, Fillet Away...).</summary>
	public float HpCostRatio { get; set; } = 0f;
	public MovePowerScaling PowerScaling { get; set; } = MovePowerScaling.None;
	public MoveBonusCondition BonusCondition { get; set; } = MoveBonusCondition.None;
	public bool AlwaysCrit { get; set; } = false;
	public bool HighCrit { get; set; } = false;
	/// <summary>Clear Smog: wipes every stage the target had accumulated.</summary>
	public bool ResetsTargetStages { get; set; } = false;
	/// <summary>Salt Cure: keeps damaging the target at the end of each turn.</summary>
	public bool AppliesSaltCure { get; set; } = false;
	/// <summary>Topsy-Turvy: flips every stage the target had.</summary>
	public bool InvertsTargetStages { get; set; } = false;
	/// <summary>Acupressure: raises a random stat by two stages.</summary>
	public bool RandomSelfBoost { get; set; } = false;
	/// <summary>Fell Stinger: sharply raises Attack when the hit knocks the target out.</summary>
	public bool KoAttackBoost { get; set; } = false;
	/// <summary>Psyshock, Psystrike and Secret Sword use the physical stats despite being special.</summary>
	public bool UsesPhysicalStats { get; set; } = false;
	public FixedDamageMode FixedDamage { get; set; } = FixedDamageMode.None;
	public int FixedDamageValue { get; set; } = 0;

	public PokemonMove(string name, PokemonType type, int power, int accuracy, int pp, MoveCategory category)
	{
		Name = name;
		Type = type;
		Power = power;
		Accuracy = accuracy;
		MaxPP = pp;
		CurrentPP = pp;
		Category = category;
		ConfigureMoveEffects();
	}

	private void ConfigureMoveEffects()
	{
		string n = (Name ?? "").ToLowerInvariant();

		// Drain moves
		if (n is "absorb" or "mega drain" or "giga drain" or "leech life" or "horn leech" or "parabolic charge" or "bitter blade" or "matcha gotcha" or "drain punch")
			DrainRatio = 0.5f;
		else if (n is "draining kiss" or "oblivion wing")
			DrainRatio = 0.75f;
		else if (n is "dream eater")
			DrainRatio = 0.5f;

		// Self-heal moves
		if (n is "recover" or "soft-boiled" or "milk drink" or "slack off" or "roost" or "synthesis" or "morning sun" or "moonlight" or "heal order" or "shore up")
			SelfHealRatio = 0.5f;
		else if (n is "rest")
			SelfHealRatio = 1.0f;

		// Pure status moves & secondary status chances
		if (n is "thunder wave" or "stun spore" or "glare" or "nuzzle")
		{
			StatusEffect = PokemonStatusCondition.Paralysis;
			StatusChance = 100;
		}
		else if (n is "body slam" or "discharge" or "dragon breath" or "spark" or "FORCE PALM" or "force palm" or "lick" or "bounce" or "bolt strike")
		{
			StatusEffect = PokemonStatusCondition.Paralysis;
			StatusChance = 30;
		}
		else if (n is "thundershock" or "thunderbolt" or "thunder punch" or "volt tackle" or "thunder fang")
		{
			StatusEffect = PokemonStatusCondition.Paralysis;
			StatusChance = 10;
		}
		else if (n is "thunder")
		{
			StatusEffect = PokemonStatusCondition.Paralysis;
			StatusChance = 30;
		}
		else if (n is "zap cannon")
		{
			StatusEffect = PokemonStatusCondition.Paralysis;
			StatusChance = 100;
		}
		else if (n is "sing" or "sleep powder" or "hypnosis" or "spore" or "lovely kiss" or "grass whistle" or "yawn" or "dark void" or "relic song")
		{
			StatusEffect = PokemonStatusCondition.Sleep;
			StatusChance = n == "relic song" ? 10 : 100;
		}
		else if (n is "toxic" or "malignant chain")
		{
			StatusEffect = PokemonStatusCondition.Toxic;
			StatusChance = n == "malignant chain" ? 50 : 100;
		}
		else if (n is "poison fang")
		{
			StatusEffect = PokemonStatusCondition.Toxic;
			StatusChance = 50;
		}
		else if (n is "poison powder" or "poison gas")
		{
			StatusEffect = PokemonStatusCondition.Poison;
			StatusChance = 100;
		}
		else if (n is "poison sting" or "sludge" or "sludge bomb" or "poison jab" or "gunk shot" or "sludge wave" or "shell side arm")
		{
			StatusEffect = PokemonStatusCondition.Poison;
			StatusChance = 30;
		}
		else if (n is "poison tail" or "cross poison" or "smog")
		{
			StatusEffect = PokemonStatusCondition.Poison;
			StatusChance = n == "smog" ? 40 : 10;
		}
		else if (n is "will-o-wisp")
		{
			StatusEffect = PokemonStatusCondition.Burn;
			StatusChance = 100;
		}
		else if (n is "scald" or "lava plume" or "sacred fire" or "sear shot" or "steam eruption" or "scorching sands" or "infernal parade")
		{
			StatusEffect = PokemonStatusCondition.Burn;
			StatusChance = n == "sacred fire" ? 50 : 30;
		}
		else if (n is "ember" or "flamethrower" or "fire blast" or "fire punch" or "flame wheel" or "blaze kick" or "heat wave" or "flare blitz" or "fire fang" or "blue flare" or "pyro ball")
		{
			StatusEffect = PokemonStatusCondition.Burn;
			StatusChance = n == "blue flare" ? 20 : 10;
		}
		else if (n is "ice beam" or "blizzard" or "powder snow" or "ice punch" or "ice fang" or "freezing glare")
		{
			StatusEffect = PokemonStatusCondition.Freeze;
			StatusChance = 10;
		}

		// Confusion moves
		if (n is "confuse ray" or "supersonic" or "sweet kiss" or "teeter dance" or "swagger" or "flatter" or "dynamic punch")
		{
			InflictsConfusion = true;
			ConfusionChance = 100;
		}
		else if (n is "confusion" or "psybeam" or "signal beam" or "dizzy punch" or "water pulse" or "rock climb" or "chatter" or "hurricane" or "strange steam")
		{
			InflictsConfusion = true;
			ConfusionChance = n is "dizzy punch" or "water pulse" ? 20 : 10;
		}

		ConfigureAdvancedEffects(n);
	}

	/// <summary>
	/// Secondaries that the original table could not express: stat stages, flinch,
	/// multi-hit, recoil, fixed and variable damage, Salt Cure and the drain/heal
	/// moves added with the full National Dex.
	/// </summary>
	private void ConfigureAdvancedEffects(string n)
	{
		void Status(PokemonStatusCondition condition, int chance)
		{
			if (StatusEffect == PokemonStatusCondition.None)
			{
				StatusEffect = condition;
				StatusChance = chance;
			}
		}

		void Self(PokemonStat stat, int stages, int chance = 100) => SelfStages.Add(new StatStageChange(stat, stages, chance));
		void Foe(PokemonStat stat, int stages, int chance = 100) => TargetStages.Add(new StatStageChange(stat, stages, chance));

		// ---- Status conditions on newer moves ---------------------------
		switch (n)
		{
			case "dire claw": Status(PokemonStatusCondition.Poison, 50); break;
			case "barb barrage": Status(PokemonStatusCondition.Poison, 50); break;
			case "mortal spin": Status(PokemonStatusCondition.Poison, 100); break;
			case "sludge wave": Status(PokemonStatusCondition.Poison, 10); break;
			case "inferno": Status(PokemonStatusCondition.Burn, 100); break;
			case "searing shot": Status(PokemonStatusCondition.Burn, 30); break;
			case "sandsear storm": Status(PokemonStatusCondition.Burn, 20); break;
			case "wildbolt storm": Status(PokemonStatusCondition.Paralysis, 20); break;
			case "toxic thread": Status(PokemonStatusCondition.Poison, 100); Foe(PokemonStat.Speed, -1); break;
			case "freeze shock": Status(PokemonStatusCondition.Paralysis, 30); break;
			case "ice burn": Status(PokemonStatusCondition.Burn, 30); break;
		}

		// ---- Self stat changes ------------------------------------------
		switch (n)
		{
			// Pure setup moves
			case "swords dance": Self(PokemonStat.Attack, 2); break;
			case "nasty plot": Self(PokemonStat.SpAttack, 2); break;
			case "calm mind": Self(PokemonStat.SpAttack, 1); Self(PokemonStat.SpDefense, 1); break;
			case "dragon dance": Self(PokemonStat.Attack, 1); Self(PokemonStat.Speed, 1); break;
			case "quiver dance": Self(PokemonStat.SpAttack, 1); Self(PokemonStat.SpDefense, 1); Self(PokemonStat.Speed, 1); break;
			case "shell smash":
				Self(PokemonStat.Attack, 2); Self(PokemonStat.SpAttack, 2); Self(PokemonStat.Speed, 2);
				Self(PokemonStat.Defense, -1); Self(PokemonStat.SpDefense, -1);
				break;
			case "shift gear": Self(PokemonStat.Speed, 2); Self(PokemonStat.Attack, 1); break;
			case "bulk up": Self(PokemonStat.Attack, 1); Self(PokemonStat.Defense, 1); break;
			case "iron defense": case "acid armor": case "shelter": Self(PokemonStat.Defense, 2); break;
			case "agility": case "rock polish": case "autotomize": Self(PokemonStat.Speed, 2); break;
			case "amnesia": Self(PokemonStat.SpDefense, 2); break;
			case "growth": case "work up": Self(PokemonStat.Attack, 1); Self(PokemonStat.SpAttack, 1); break;
			case "coil": Self(PokemonStat.Attack, 1); Self(PokemonStat.Defense, 1); break;
			case "hone claws": Self(PokemonStat.Attack, 1); break;
			case "tail glow": Self(PokemonStat.SpAttack, 3); break;
			case "belly drum": Self(PokemonStat.Attack, 6); HpCostRatio = 0.5f; break;
			case "curse": Self(PokemonStat.Attack, 1); Self(PokemonStat.Defense, 1); Self(PokemonStat.Speed, -1); break;
			case "clangorous soul":
				Self(PokemonStat.Attack, 1); Self(PokemonStat.Defense, 1); Self(PokemonStat.SpAttack, 1);
				Self(PokemonStat.SpDefense, 1); Self(PokemonStat.Speed, 1); HpCostRatio = 0.33f;
				break;
			case "victory dance": Self(PokemonStat.Attack, 1); Self(PokemonStat.Defense, 1); Self(PokemonStat.Speed, 1); break;
			case "fillet away":
				Self(PokemonStat.Attack, 2); Self(PokemonStat.SpAttack, 2); Self(PokemonStat.Speed, 2);
				HpCostRatio = 0.5f;
				break;
			case "tidy up": Self(PokemonStat.Attack, 1); Self(PokemonStat.Speed, 1); break;
			case "no retreat":
				Self(PokemonStat.Attack, 1); Self(PokemonStat.Defense, 1); Self(PokemonStat.SpAttack, 1);
				Self(PokemonStat.SpDefense, 1); Self(PokemonStat.Speed, 1);
				break;
			case "take heart": Self(PokemonStat.SpAttack, 1); Self(PokemonStat.SpDefense, 1); break;
			case "cosmic power": case "stockpile": case "defend order": case "magnetic flux":
				Self(PokemonStat.Defense, 1); Self(PokemonStat.SpDefense, 1);
				break;
			case "geomancy": Self(PokemonStat.SpAttack, 2); Self(PokemonStat.SpDefense, 2); Self(PokemonStat.Speed, 2); break;
			case "cotton guard": Self(PokemonStat.Defense, 3); break;
			case "decorate": Self(PokemonStat.Attack, 2); Self(PokemonStat.SpAttack, 2); break;
			case "coaching": Self(PokemonStat.Attack, 1); Self(PokemonStat.Defense, 1); break;
			case "gear up": Self(PokemonStat.Attack, 1); Self(PokemonStat.SpAttack, 1); break;
			case "flower shield": Self(PokemonStat.Defense, 1); break;
			case "aromatic mist": Self(PokemonStat.SpDefense, 1); break;
			case "diamond storm": Self(PokemonStat.Defense, 2, 50); break;

			// Damaging moves that raise the user
			case "flame charge": Self(PokemonStat.Speed, 1); break;
			case "power-up punch": Self(PokemonStat.Attack, 1); break;
			case "charge beam": Self(PokemonStat.SpAttack, 1, 70); break;
			case "fiery dance": Self(PokemonStat.SpAttack, 1, 50); break;
			case "meteor beam": Self(PokemonStat.SpAttack, 1); break;
			case "esper wing": Self(PokemonStat.Speed, 1); HighCrit = true; break;
			case "mystical power": Self(PokemonStat.SpAttack, 1); break;
			case "psyshield bash": Self(PokemonStat.Defense, 1); break;
			case "ominous wind": Self(PokemonStat.Attack, 1, 10); Self(PokemonStat.Defense, 1, 10); Self(PokemonStat.SpAttack, 1, 10); Self(PokemonStat.SpDefense, 1, 10); Self(PokemonStat.Speed, 1, 10); break;

			// Damaging moves that lower the user
			case "close combat": Self(PokemonStat.Defense, -1); Self(PokemonStat.SpDefense, -1); break;
			case "headlong rush": Self(PokemonStat.Defense, -1); Self(PokemonStat.SpDefense, -1); break;
			case "dragon ascent": Self(PokemonStat.Defense, -1); Self(PokemonStat.SpDefense, -1); break;
			case "superpower": Self(PokemonStat.Attack, -1); Self(PokemonStat.Defense, -1); break;
			case "overheat": case "draco meteor": case "leaf storm": case "psycho boost": case "fleur cannon":
				Self(PokemonStat.SpAttack, -2);
				break;
			case "clanging scales": Self(PokemonStat.Defense, -1); break;
			case "hammer arm": case "ice hammer": case "spin out": Self(PokemonStat.Speed, -1); break;
		}

		// ---- Target stat changes ----------------------------------------
		switch (n)
		{
			// Status moves
			case "growl": Foe(PokemonStat.Attack, -1); break;
			case "leer": case "tail whip": Foe(PokemonStat.Defense, -1); break;
			case "screech": Foe(PokemonStat.Defense, -2); break;
			case "metal sound": Foe(PokemonStat.SpDefense, -2); break;
			case "scary face": case "string shot": Foe(PokemonStat.Speed, -2); break;
			case "charm": case "feather dance": Foe(PokemonStat.Attack, -2); break;
			case "fake tears": case "captivate": Foe(PokemonStat.SpDefense, -2); break;
			case "baby-doll eyes": case "play nice": Foe(PokemonStat.Attack, -1); break;
			case "noble roar": case "tearful look": Foe(PokemonStat.Attack, -1); Foe(PokemonStat.SpAttack, -1); break;
			case "confide": Foe(PokemonStat.SpAttack, -1); break;
			case "eerie impulse": Foe(PokemonStat.SpAttack, -2); break;
			case "venom drench": Foe(PokemonStat.Attack, -1); Foe(PokemonStat.SpAttack, -1); Foe(PokemonStat.Speed, -1); break;
			case "spicy extract": Foe(PokemonStat.Attack, 2); Foe(PokemonStat.Defense, -2); break;
			case "strength sap": Foe(PokemonStat.Attack, -1); SelfHealRatio = 0.5f; break;
			case "octolock": Foe(PokemonStat.Defense, -1); Foe(PokemonStat.SpDefense, -1); break;
			case "parting shot": Foe(PokemonStat.Attack, -1); Foe(PokemonStat.SpAttack, -1); break;
			case "topsy-turvy": InvertsTargetStages = true; break;
			case "acupressure": RandomSelfBoost = true; break;
			case "bitter malice": Foe(PokemonStat.Attack, -1); break;
			case "bleakwind storm": Foe(PokemonStat.Speed, -1, 30); break;
			case "springtide storm": Foe(PokemonStat.Attack, -1, 30); break;
			case "shadow bone": Foe(PokemonStat.Defense, -1, 20); break;

			// Damaging moves
			case "acid spray": Foe(PokemonStat.SpDefense, -2); break;
			case "apple acid": Foe(PokemonStat.SpDefense, -1); break;
			case "lumina crash": Foe(PokemonStat.SpDefense, -2); break;
			case "grav apple": Foe(PokemonStat.Defense, -1); break;
			case "fire lash": Foe(PokemonStat.Defense, -1); break;
			case "trop kick": Foe(PokemonStat.Attack, -1); break;
			case "skitter smack": Foe(PokemonStat.SpAttack, -1); break;
			case "lunge": Foe(PokemonStat.Attack, -1); break;
			case "low sweep": case "electroweb": case "icy wind": case "bulldoze": case "mud shot": case "rock tomb":
				Foe(PokemonStat.Speed, -1);
				break;
			case "thunderous kick": Foe(PokemonStat.Defense, -1); break;
			case "triple arrows": Foe(PokemonStat.Defense, -1, 50); break;
			case "crunch": case "iron tail": Foe(PokemonStat.Defense, -1, 20); break;
			case "razor shell": Foe(PokemonStat.Defense, -1, 50); break;
			case "struggle bug": Foe(PokemonStat.SpAttack, -1); break;
			case "focus blast": Foe(PokemonStat.SpDefense, -1, 10); break;
			case "syrup bomb": Foe(PokemonStat.Speed, -1); break;
		}

		// ---- Flinch ------------------------------------------------------
		switch (n)
		{
			case "fake out": FlinchChance = 100; break;
			case "double iron bash": FlinchChance = 30; break;
			case "iron head": case "air slash": case "rock slide": case "icicle crash": case "heart stamp": case "headbutt": case "astonish": case "bite": case "stomp": case "zing zap": case "mountain gale": FlinchChance = 30; break;
			case "zen headbutt": case "waterfall": case "dark pulse": case "dragon rush": case "twister": FlinchChance = 20; break;
			case "fiery wrath": FlinchChance = 20; break;
			case "steamroller": FlinchChance = 30; break;
			case "triple arrows": FlinchChance = 30; break;
			case "extrasensory": FlinchChance = 10; break;
		}

		// ---- Multi-hit ---------------------------------------------------
		switch (n)
		{
			case "double hit": case "double iron bash": case "dragon darts": case "dual chop": case "gear grind": case "tachyon cutter": case "twin beam":
				HitCountMin = 2; HitCountMax = 2;
				break;
			case "triple dive": HitCountMin = 3; HitCountMax = 3; break;
			case "population bomb": HitCountMin = 10; HitCountMax = 10; break;
			case "tail slap": case "bullet seed": case "icicle spear": case "rock blast": case "pin missile": case "water shuriken":
				HitCountMin = 2; HitCountMax = 5;
				break;
		}

		// ---- Recoil ------------------------------------------------------
		switch (n)
		{
			case "head smash": case "light of ruin": case "chloroblast": case "steel beam": case "mind blown":
				RecoilRatio = 0.5f;
				break;
			case "double-edge": case "brave bird": case "flare blitz": case "volt tackle": case "wood hammer": case "wave crash":
				RecoilRatio = 1f / 3f;
				break;
			case "take down": case "submission": case "head charge": case "wild charge":
				RecoilRatio = 0.25f;
				break;
		}

		// ---- Fixed and variable damage ----------------------------------
		switch (n)
		{
			case "ruination": case "nature's madness": case "super fang":
				FixedDamage = FixedDamageMode.HalfCurrentHp;
				break;
			case "night shade": case "seismic toss":
				FixedDamage = FixedDamageMode.Level;
				break;
			case "dragon rage": FixedDamage = FixedDamageMode.Flat; FixedDamageValue = 40; break;
			case "sonic boom": FixedDamage = FixedDamageMode.Flat; FixedDamageValue = 20; break;
			case "eruption": case "water spout": case "dragon energy":
				PowerScaling = MovePowerScaling.UserHp;
				break;
			case "wring out": case "crush grip": case "hard press":
				PowerScaling = MovePowerScaling.TargetHp;
				break;
			case "gyro ball": PowerScaling = MovePowerScaling.GyroBall; break;
			case "electro ball": PowerScaling = MovePowerScaling.ElectroBall; break;
			case "stored power": case "power trip": PowerScaling = MovePowerScaling.UserBoosts; break;
			case "punishment": PowerScaling = MovePowerScaling.TargetBoosts; break;
			case "brine": BonusCondition = MoveBonusCondition.TargetBelowHalfHp; break;
			case "hex": case "infernal parade": BonusCondition = MoveBonusCondition.TargetHasStatus; break;
			case "venoshock": BonusCondition = MoveBonusCondition.VenoshockTargetPoisoned; break;
			case "smelling salts": BonusCondition = MoveBonusCondition.SmellingSaltsTargetParalyzed; break;
			case "wake-up slap": BonusCondition = MoveBonusCondition.WakeUpSlapTargetAsleep; break;
		}

		// ---- Crit behaviour, stage reset and residual damage -------------
		switch (n)
		{
			case "storm throw": case "frost breath": AlwaysCrit = true; break;
			case "aqua cutter": case "stone axe": HighCrit = true; break;
			case "clear smog": ResetsTargetStages = true; break;
			case "salt cure": AppliesSaltCure = true; break;
			case "fell stinger": KoAttackBoost = true; break;
			case "psyshock": case "psystrike": case "secret sword": UsesPhysicalStats = true; break;
			case "axe kick": InflictsConfusion = true; ConfusionChance = 30; break;
		}

		// ---- Healing and drain moves added with the full dex -------------
		switch (n)
		{
			case "life dew": case "jungle healing": case "lunar blessing":
				SelfHealRatio = 0.25f;
				break;
			case "floral healing": case "heal pulse":
				SelfHealRatio = 0.5f;
				break;
			case "matcha gotcha": case "bitter blade": case "parabolic charge":
				DrainRatio = 0.5f;
				break;
		}
	}

	public PokemonMove Clone()
	{
		var clone = new PokemonMove(Name, Type, Power, Accuracy, MaxPP, Category)
		{
			Priority = this.Priority,
			CurrentPP = this.CurrentPP,
			StatusEffect = this.StatusEffect,
			StatusChance = this.StatusChance,
			InflictsConfusion = this.InflictsConfusion,
			ConfusionChance = this.ConfusionChance,
			DrainRatio = this.DrainRatio,
			SelfHealRatio = this.SelfHealRatio,
			FlinchChance = this.FlinchChance,
			HitCountMin = this.HitCountMin,
			HitCountMax = this.HitCountMax,
			RecoilRatio = this.RecoilRatio,
			HpCostRatio = this.HpCostRatio,
			PowerScaling = this.PowerScaling,
			BonusCondition = this.BonusCondition,
			AlwaysCrit = this.AlwaysCrit,
			HighCrit = this.HighCrit,
			ResetsTargetStages = this.ResetsTargetStages,
			AppliesSaltCure = this.AppliesSaltCure,
			InvertsTargetStages = this.InvertsTargetStages,
			RandomSelfBoost = this.RandomSelfBoost,
			KoAttackBoost = this.KoAttackBoost,
			UsesPhysicalStats = this.UsesPhysicalStats,
			FixedDamage = this.FixedDamage,
			FixedDamageValue = this.FixedDamageValue
		};
		clone.SelfStages.Clear();
		clone.SelfStages.AddRange(this.SelfStages);
		clone.TargetStages.Clear();
		clone.TargetStages.AddRange(this.TargetStages);
		return clone;
	}

	private static readonly Dictionary<string, PokemonMove> _database = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Maps "thunder shock" / "Thunder-Shock" style spellings to the registered name.</summary>
	private static readonly Dictionary<string, string> _normalizedIndex = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Real spelling differences (not just punctuation) between sources.</summary>
	private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
	{
		["Vise Grip"] = "Vice Grip",
		["Faint Attack"] = "Feint Attack",
		["Hi Jump Kick"] = "High Jump Kick"
	};

	static PokemonMove()
	{
		Register("Pound", PokemonType.Normal, 40, 100, 35, MoveCategory.Physical);
		Register("Karate Chop", PokemonType.Fighting, 50, 100, 25, MoveCategory.Physical);
		Register("Double Slap", PokemonType.Normal, 15, 85, 10, MoveCategory.Physical);
		Register("Comet Punch", PokemonType.Normal, 18, 85, 15, MoveCategory.Physical);
		Register("Mega Punch", PokemonType.Normal, 80, 85, 20, MoveCategory.Physical);
		Register("Pay Day", PokemonType.Normal, 40, 100, 20, MoveCategory.Physical);
		Register("Fire Punch", PokemonType.Fire, 75, 100, 15, MoveCategory.Physical);
		Register("Ice Punch", PokemonType.Ice, 75, 100, 15, MoveCategory.Physical);
		Register("Thunder Punch", PokemonType.Electric, 75, 100, 15, MoveCategory.Physical);
		Register("Scratch", PokemonType.Normal, 40, 100, 35, MoveCategory.Physical);
		Register("Vice Grip", PokemonType.Normal, 55, 100, 30, MoveCategory.Physical);
		Register("Guillotine", PokemonType.Normal, 120, 30, 5, MoveCategory.Physical);
		Register("Razor Wind", PokemonType.Normal, 80, 100, 10, MoveCategory.Physical);
		Register("Swords Dance", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Cut", PokemonType.Normal, 50, 95, 30, MoveCategory.Physical);
		Register("Gust", PokemonType.Flying, 40, 100, 35, MoveCategory.Physical);
		Register("Wing Attack", PokemonType.Flying, 60, 100, 35, MoveCategory.Physical);
		Register("Whirlwind", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Fly", PokemonType.Flying, 70, 95, 15, MoveCategory.Physical);
		Register("Bind", PokemonType.Normal, 15, 75, 20, MoveCategory.Physical);
		Register("Slam", PokemonType.Normal, 80, 75, 20, MoveCategory.Physical);
		Register("Vine Whip", PokemonType.Grass, 35, 100, 10, MoveCategory.Physical);
		Register("Stomp", PokemonType.Normal, 65, 100, 20, MoveCategory.Physical);
		Register("Double Kick", PokemonType.Fighting, 30, 100, 30, MoveCategory.Physical);
		Register("Mega Kick", PokemonType.Normal, 120, 75, 5, MoveCategory.Physical);
		Register("Jump Kick", PokemonType.Fighting, 70, 95, 25, MoveCategory.Physical);
		Register("Rolling Kick", PokemonType.Fighting, 60, 85, 15, MoveCategory.Physical);
		Register("Sand Attack", PokemonType.Ground, 0, 100, 15, MoveCategory.Status);
		Register("Headbutt", PokemonType.Normal, 70, 100, 15, MoveCategory.Physical);
		Register("Horn Attack", PokemonType.Normal, 65, 100, 25, MoveCategory.Physical);
		Register("Fury Attack", PokemonType.Normal, 15, 85, 20, MoveCategory.Physical);
		Register("Horn Drill", PokemonType.Normal, 120, 30, 5, MoveCategory.Physical);
		Register("Tackle", PokemonType.Normal, 35, 95, 35, MoveCategory.Physical);
		Register("Body Slam", PokemonType.Normal, 85, 100, 15, MoveCategory.Physical);
		Register("Wrap", PokemonType.Normal, 15, 85, 20, MoveCategory.Physical);
		Register("Take Down", PokemonType.Normal, 90, 85, 20, MoveCategory.Physical);
		Register("Thrash", PokemonType.Normal, 90, 100, 20, MoveCategory.Physical);
		Register("Double Edge", PokemonType.Normal, 120, 100, 15, MoveCategory.Physical);
		Register("Tail Whip", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Poison Sting", PokemonType.Poison, 15, 100, 35, MoveCategory.Physical);
		Register("Twineedle", PokemonType.Bug, 25, 100, 20, MoveCategory.Physical);
		Register("Pin Missile", PokemonType.Bug, 14, 85, 20, MoveCategory.Physical);
		Register("Leer", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Bite", PokemonType.Dark, 60, 100, 25, MoveCategory.Physical);
		Register("Growl", PokemonType.Normal, 0, 100, 40, MoveCategory.Status);
		Register("Roar", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Sing", PokemonType.Normal, 0, 55, 15, MoveCategory.Status);
		Register("Supersonic", PokemonType.Normal, 0, 55, 20, MoveCategory.Status);
		Register("Sonic Boom", PokemonType.Normal, 50, 90, 20, MoveCategory.Physical);
		Register("Disable", PokemonType.Normal, 0, 55, 20, MoveCategory.Status);
		Register("Acid", PokemonType.Poison, 40, 100, 30, MoveCategory.Physical);
		Register("Ember", PokemonType.Fire, 40, 100, 25, MoveCategory.Special);
		Register("Flamethrower", PokemonType.Fire, 95, 100, 15, MoveCategory.Special);
		Register("Mist", PokemonType.Ice, 0, 100, 30, MoveCategory.Status);
		Register("Water Gun", PokemonType.Water, 40, 100, 25, MoveCategory.Special);
		Register("Hydro Pump", PokemonType.Water, 120, 80, 5, MoveCategory.Special);
		Register("Surf", PokemonType.Water, 95, 100, 15, MoveCategory.Special);
		Register("Ice Beam", PokemonType.Ice, 95, 100, 10, MoveCategory.Special);
		Register("Blizzard", PokemonType.Ice, 120, 70, 5, MoveCategory.Special);
		Register("Psybeam", PokemonType.Psychic, 65, 100, 20, MoveCategory.Special);
		Register("Bubble Beam", PokemonType.Water, 65, 100, 20, MoveCategory.Special);
		Register("Aurora Beam", PokemonType.Ice, 65, 100, 20, MoveCategory.Special);
		Register("Hyper Beam", PokemonType.Normal, 150, 90, 5, MoveCategory.Physical);
		Register("Peck", PokemonType.Flying, 35, 100, 35, MoveCategory.Physical);
		Register("Drill Peck", PokemonType.Flying, 80, 100, 20, MoveCategory.Physical);
		Register("Submission", PokemonType.Fighting, 80, 80, 25, MoveCategory.Physical);
		Register("Low Kick", PokemonType.Fighting, 55, 100, 20, MoveCategory.Physical);
		Register("Counter", PokemonType.Fighting, 55, 100, 20, MoveCategory.Physical);
		Register("Seismic Toss", PokemonType.Fighting, 60, 100, 20, MoveCategory.Physical);
		Register("Strength", PokemonType.Normal, 80, 100, 15, MoveCategory.Physical);
		Register("Absorb", PokemonType.Grass, 20, 100, 20, MoveCategory.Special);
		Register("Mega Drain", PokemonType.Grass, 40, 100, 10, MoveCategory.Special);
		Register("Leech Seed", PokemonType.Grass, 0, 90, 10, MoveCategory.Status);
		Register("Growth", PokemonType.Normal, 0, 100, 40, MoveCategory.Status);
		Register("Razor Leaf", PokemonType.Grass, 55, 95, 25, MoveCategory.Special);
		Register("Solar Beam", PokemonType.Grass, 120, 100, 10, MoveCategory.Special);
		Register("Poison Powder", PokemonType.Poison, 0, 75, 35, MoveCategory.Status);
		Register("Stun Spore", PokemonType.Grass, 0, 75, 30, MoveCategory.Status);
		Register("Sleep Powder", PokemonType.Grass, 0, 75, 15, MoveCategory.Status);
		Register("Petal Dance", PokemonType.Grass, 70, 100, 20, MoveCategory.Physical);
		Register("String Shot", PokemonType.Bug, 0, 95, 40, MoveCategory.Status);
		Register("Dragon Rage", PokemonType.Dragon, 40, 100, 10, MoveCategory.Special);
		Register("Fire Spin", PokemonType.Fire, 15, 70, 15, MoveCategory.Special);
		Register("Thundershock", PokemonType.Electric, 40, 100, 30, MoveCategory.Special);
		Register("Thunderbolt", PokemonType.Electric, 95, 100, 15, MoveCategory.Special);
		Register("Thunder Wave", PokemonType.Electric, 0, 100, 20, MoveCategory.Status);
		Register("Thunder", PokemonType.Electric, 120, 70, 10, MoveCategory.Special);
		Register("Rock Throw", PokemonType.Rock, 50, 90, 15, MoveCategory.Physical);
		Register("Earthquake", PokemonType.Ground, 100, 100, 10, MoveCategory.Physical);
		Register("Fissure", PokemonType.Ground, 120, 30, 5, MoveCategory.Physical);
		Register("Dig", PokemonType.Ground, 60, 100, 10, MoveCategory.Physical);
		Register("Toxic", PokemonType.Poison, 0, 85, 10, MoveCategory.Status);
		Register("Confusion", PokemonType.Psychic, 50, 100, 25, MoveCategory.Special);
		Register("Psychic", PokemonType.Psychic, 90, 100, 10, MoveCategory.Special);
		Register("Hypnosis", PokemonType.Psychic, 0, 60, 20, MoveCategory.Status);
		Register("Meditate", PokemonType.Psychic, 0, 100, 40, MoveCategory.Status);
		Register("Agility", PokemonType.Psychic, 0, 100, 30, MoveCategory.Status);
		Register("Quick Attack", PokemonType.Normal, 40, 100, 30, MoveCategory.Physical);
		Register("Rage", PokemonType.Normal, 20, 100, 20, MoveCategory.Physical);
		Register("Teleport", PokemonType.Psychic, 0, 100, 20, MoveCategory.Status);
		Register("Night Shade", PokemonType.Ghost, 60, 100, 15, MoveCategory.Physical);
		Register("Mimic", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Screech", PokemonType.Normal, 0, 85, 40, MoveCategory.Status);
		Register("Double Team", PokemonType.Normal, 0, 100, 15, MoveCategory.Status);
		Register("Recover", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Harden", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Minimize", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Smokescreen", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Confuse Ray", PokemonType.Ghost, 0, 100, 10, MoveCategory.Status);
		Register("Withdraw", PokemonType.Water, 0, 100, 40, MoveCategory.Status);
		Register("Defense Curl", PokemonType.Normal, 0, 100, 40, MoveCategory.Status);
		Register("Barrier", PokemonType.Psychic, 0, 100, 30, MoveCategory.Status);
		Register("Light Screen", PokemonType.Psychic, 0, 100, 30, MoveCategory.Status);
		Register("Haze", PokemonType.Ice, 0, 100, 30, MoveCategory.Status);
		Register("Reflect", PokemonType.Psychic, 0, 100, 20, MoveCategory.Status);
		Register("Focus Energy", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Bide", PokemonType.Normal, 55, 100, 10, MoveCategory.Physical);
		Register("Metronome", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Mirror Move", PokemonType.Flying, 0, 100, 20, MoveCategory.Status);
		Register("Self-Destruct", PokemonType.Normal, 200, 100, 5, MoveCategory.Physical);
		Register("Egg Bomb", PokemonType.Normal, 100, 75, 10, MoveCategory.Physical);
		Register("Lick", PokemonType.Ghost, 20, 100, 30, MoveCategory.Physical);
		Register("Smog", PokemonType.Poison, 20, 70, 20, MoveCategory.Physical);
		Register("Sludge", PokemonType.Poison, 65, 100, 20, MoveCategory.Physical);
		Register("Bone Club", PokemonType.Ground, 65, 85, 20, MoveCategory.Physical);
		Register("Fire Blast", PokemonType.Fire, 120, 85, 5, MoveCategory.Special);
		Register("Waterfall", PokemonType.Water, 80, 100, 15, MoveCategory.Physical);
		Register("Clamp", PokemonType.Water, 35, 75, 10, MoveCategory.Physical);
		Register("Swift", PokemonType.Normal, 60, 100, 20, MoveCategory.Physical);
		Register("Skull Bash", PokemonType.Normal, 100, 100, 15, MoveCategory.Physical);
		Register("Spike Cannon", PokemonType.Normal, 20, 100, 15, MoveCategory.Physical);
		Register("Constrict", PokemonType.Normal, 10, 100, 35, MoveCategory.Physical);
		Register("Amnesia", PokemonType.Psychic, 0, 100, 20, MoveCategory.Status);
		Register("Kinesis", PokemonType.Psychic, 0, 80, 15, MoveCategory.Status);
		Register("Soft-Boiled", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("High Jump Kick", PokemonType.Fighting, 85, 90, 20, MoveCategory.Physical);
		Register("Glare", PokemonType.Normal, 0, 75, 30, MoveCategory.Status);
		Register("Dream Eater", PokemonType.Psychic, 100, 100, 15, MoveCategory.Special);
		Register("Poison Gas", PokemonType.Poison, 0, 55, 40, MoveCategory.Status);
		Register("Barrage", PokemonType.Normal, 15, 85, 20, MoveCategory.Physical);
		Register("Leech Life", PokemonType.Bug, 20, 100, 15, MoveCategory.Physical);
		Register("Lovely Kiss", PokemonType.Normal, 0, 75, 10, MoveCategory.Status);
		Register("Sky Attack", PokemonType.Flying, 140, 90, 5, MoveCategory.Physical);
		Register("Transform", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Bubble", PokemonType.Water, 20, 100, 30, MoveCategory.Special);
		Register("Dizzy Punch", PokemonType.Normal, 70, 100, 10, MoveCategory.Physical);
		Register("Spore", PokemonType.Grass, 0, 100, 15, MoveCategory.Status);
		Register("Flash", PokemonType.Normal, 0, 70, 20, MoveCategory.Status);
		Register("Psywave", PokemonType.Psychic, 60, 80, 15, MoveCategory.Special);
		Register("Splash", PokemonType.Normal, 0, 100, 40, MoveCategory.Status);
		Register("Acid Armor", PokemonType.Poison, 0, 100, 40, MoveCategory.Status);
		Register("Crabhammer", PokemonType.Water, 90, 85, 10, MoveCategory.Physical);
		Register("Explosion", PokemonType.Normal, 250, 100, 5, MoveCategory.Physical);
		Register("Fury Swipes", PokemonType.Normal, 18, 80, 15, MoveCategory.Physical);
		Register("Bonemerang", PokemonType.Ground, 50, 90, 10, MoveCategory.Physical);
		Register("Rest", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Rock Slide", PokemonType.Rock, 75, 90, 10, MoveCategory.Physical);
		Register("Hyper Fang", PokemonType.Normal, 80, 90, 15, MoveCategory.Physical);
		Register("Sharpen", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Conversion", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Tri Attack", PokemonType.Normal, 80, 100, 10, MoveCategory.Physical);
		Register("Super Fang", PokemonType.Normal, 55, 90, 10, MoveCategory.Physical);
		Register("Slash", PokemonType.Normal, 70, 100, 20, MoveCategory.Physical);
		Register("Substitute", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Struggle", PokemonType.Normal, 50, 100, 1, MoveCategory.Physical);
		Register("Sketch", PokemonType.Normal, 0, 100, 1, MoveCategory.Status);
		Register("Triple Kick", PokemonType.Fighting, 10, 90, 10, MoveCategory.Physical);
		Register("Thief", PokemonType.Dark, 40, 100, 10, MoveCategory.Physical);
		Register("Spider Web", PokemonType.Bug, 0, 100, 10, MoveCategory.Status);
		Register("Mind Reader", PokemonType.Normal, 0, 100, 5, MoveCategory.Status);
		Register("Nightmare", PokemonType.Ghost, 0, 100, 15, MoveCategory.Status);
		Register("Flame Wheel", PokemonType.Fire, 60, 100, 25, MoveCategory.Physical);
		Register("Snore", PokemonType.Normal, 40, 100, 15, MoveCategory.Physical);
		Register("Curse", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Flail", PokemonType.Normal, 55, 100, 15, MoveCategory.Physical);
		Register("Conversion 2", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Aeroblast", PokemonType.Flying, 100, 95, 5, MoveCategory.Physical);
		Register("Cotton Spore", PokemonType.Grass, 0, 85, 40, MoveCategory.Status);
		Register("Reversal", PokemonType.Fighting, 55, 100, 15, MoveCategory.Physical);
		Register("Spite", PokemonType.Ghost, 0, 100, 10, MoveCategory.Status);
		Register("Powder Snow", PokemonType.Ice, 40, 100, 25, MoveCategory.Special);
		Register("Protect", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Mach Punch", PokemonType.Fighting, 40, 100, 30, MoveCategory.Physical);
		Register("Scary Face", PokemonType.Normal, 0, 90, 10, MoveCategory.Status);
		Register("Sweet Kiss", PokemonType.Normal, 0, 75, 10, MoveCategory.Status);
		Register("Belly Drum", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Sludge Bomb", PokemonType.Poison, 90, 100, 10, MoveCategory.Physical);
		Register("Mud-Slap", PokemonType.Ground, 20, 100, 10, MoveCategory.Physical);
		Register("Octazooka", PokemonType.Water, 65, 85, 10, MoveCategory.Special);
		Register("Spikes", PokemonType.Ground, 0, 100, 20, MoveCategory.Status);
		Register("Zap Cannon", PokemonType.Electric, 100, 50, 5, MoveCategory.Special);
		Register("Foresight", PokemonType.Normal, 0, 100, 40, MoveCategory.Status);
		Register("Destiny Bond", PokemonType.Ghost, 0, 100, 5, MoveCategory.Status);
		Register("Perish Song", PokemonType.Normal, 0, 100, 5, MoveCategory.Status);
		Register("Icy Wind", PokemonType.Ice, 55, 95, 15, MoveCategory.Special);
		Register("Detect", PokemonType.Fighting, 0, 100, 5, MoveCategory.Status);
		Register("Bone Rush", PokemonType.Ground, 25, 80, 10, MoveCategory.Physical);
		Register("Lock-On", PokemonType.Normal, 0, 100, 5, MoveCategory.Status);
		Register("Outrage", PokemonType.Dragon, 90, 100, 15, MoveCategory.Physical);
		Register("Sandstorm", PokemonType.Rock, 0, 100, 10, MoveCategory.Status);
		Register("Giga Drain", PokemonType.Grass, 60, 100, 5, MoveCategory.Special);
		Register("Endure", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Charm", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Rollout", PokemonType.Rock, 30, 90, 20, MoveCategory.Physical);
		Register("False Swipe", PokemonType.Normal, 40, 100, 40, MoveCategory.Physical);
		Register("Swagger", PokemonType.Normal, 0, 90, 15, MoveCategory.Status);
		Register("Milk Drink", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Spark", PokemonType.Electric, 65, 100, 20, MoveCategory.Physical);
		Register("Fury Cutter", PokemonType.Bug, 10, 95, 20, MoveCategory.Physical);
		Register("Steel Wing", PokemonType.Steel, 70, 90, 25, MoveCategory.Physical);
		Register("Mean Look", PokemonType.Normal, 0, 100, 5, MoveCategory.Status);
		Register("Attract", PokemonType.Normal, 0, 100, 15, MoveCategory.Status);
		Register("Sleep Talk", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Heal Bell", PokemonType.Normal, 0, 100, 5, MoveCategory.Status);
		Register("Return", PokemonType.Normal, 55, 100, 20, MoveCategory.Physical);
		Register("Present", PokemonType.Normal, 55, 90, 15, MoveCategory.Physical);
		Register("Frustration", PokemonType.Normal, 55, 100, 20, MoveCategory.Physical);
		Register("Safeguard", PokemonType.Normal, 0, 100, 25, MoveCategory.Status);
		Register("Pain Split", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Sacred Fire", PokemonType.Fire, 100, 95, 5, MoveCategory.Special);
		Register("Magnitude", PokemonType.Ground, 55, 100, 30, MoveCategory.Physical);
		Register("Dynamic Punch", PokemonType.Fighting, 100, 50, 5, MoveCategory.Physical);
		Register("Megahorn", PokemonType.Bug, 120, 85, 10, MoveCategory.Physical);
		Register("Dragon Breath", PokemonType.Dragon, 60, 100, 20, MoveCategory.Special);
		Register("Baton Pass", PokemonType.Normal, 0, 100, 40, MoveCategory.Status);
		Register("Encore", PokemonType.Normal, 0, 100, 5, MoveCategory.Status);
		Register("Pursuit", PokemonType.Dark, 40, 100, 20, MoveCategory.Physical);
		Register("Rapid Spin", PokemonType.Normal, 20, 100, 40, MoveCategory.Physical);
		Register("Sweet Scent", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Iron Tail", PokemonType.Steel, 100, 75, 15, MoveCategory.Physical);
		Register("Metal Claw", PokemonType.Steel, 50, 95, 35, MoveCategory.Physical);
		Register("Vital Throw", PokemonType.Fighting, 70, 100, 10, MoveCategory.Physical);
		Register("Morning Sun", PokemonType.Normal, 0, 100, 5, MoveCategory.Status);
		Register("Synthesis", PokemonType.Grass, 0, 100, 5, MoveCategory.Status);
		Register("Moonlight", PokemonType.Normal, 0, 100, 5, MoveCategory.Status);
		Register("Hidden Power", PokemonType.Normal, 55, 100, 15, MoveCategory.Physical);
		Register("Cross Chop", PokemonType.Fighting, 100, 80, 5, MoveCategory.Physical);
		Register("Twister", PokemonType.Dragon, 40, 100, 20, MoveCategory.Special);
		Register("Rain Dance", PokemonType.Water, 0, 100, 5, MoveCategory.Status);
		Register("Sunny Day", PokemonType.Fire, 0, 100, 5, MoveCategory.Status);
		Register("Crunch", PokemonType.Dark, 80, 100, 15, MoveCategory.Physical);
		Register("Mirror Coat", PokemonType.Psychic, 55, 100, 20, MoveCategory.Special);
		Register("Psych Up", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Extreme Speed", PokemonType.Normal, 80, 100, 5, MoveCategory.Physical);
		Register("Ancient Power", PokemonType.Rock, 60, 100, 5, MoveCategory.Physical);
		Register("Shadow Ball", PokemonType.Ghost, 80, 100, 15, MoveCategory.Physical);
		Register("Future Sight", PokemonType.Psychic, 80, 90, 15, MoveCategory.Special);
		Register("Rock Smash", PokemonType.Fighting, 20, 100, 15, MoveCategory.Physical);
		Register("Whirlpool", PokemonType.Water, 15, 70, 15, MoveCategory.Special);
		Register("Beat Up", PokemonType.Dark, 10, 100, 10, MoveCategory.Special);
		Register("Fake Out", PokemonType.Normal, 40, 100, 10, MoveCategory.Physical);
		Register("Uproar", PokemonType.Normal, 50, 100, 10, MoveCategory.Physical);
		Register("Stockpile", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Spit Up", PokemonType.Normal, 100, 100, 10, MoveCategory.Physical);
		Register("Swallow", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Heat Wave", PokemonType.Fire, 100, 90, 10, MoveCategory.Special);
		Register("Hail", PokemonType.Ice, 0, 100, 10, MoveCategory.Status);
		Register("Torment", PokemonType.Dark, 0, 100, 15, MoveCategory.Status);
		Register("Flatter", PokemonType.Dark, 0, 100, 15, MoveCategory.Status);
		Register("Will-O-Wisp", PokemonType.Fire, 0, 75, 15, MoveCategory.Status);
		Register("Memento", PokemonType.Dark, 0, 100, 10, MoveCategory.Status);
		Register("Facade", PokemonType.Normal, 70, 100, 20, MoveCategory.Physical);
		Register("Focus Punch", PokemonType.Fighting, 150, 100, 20, MoveCategory.Physical);
		Register("Smelling Salt", PokemonType.Normal, 60, 100, 10, MoveCategory.Physical);
		Register("Follow Me", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Nature Power", PokemonType.Normal, 0, 95, 20, MoveCategory.Status);
		Register("Charge", PokemonType.Electric, 0, 100, 20, MoveCategory.Status);
		Register("Taunt", PokemonType.Dark, 0, 100, 20, MoveCategory.Status);
		Register("Helping Hand", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Trick", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Role Play", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Wish", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Assist", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Ingrain", PokemonType.Grass, 0, 100, 20, MoveCategory.Status);
		Register("Superpower", PokemonType.Fighting, 120, 100, 5, MoveCategory.Physical);
		Register("Magic Coat", PokemonType.Psychic, 0, 100, 15, MoveCategory.Status);
		Register("Recycle", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Revenge", PokemonType.Fighting, 60, 100, 10, MoveCategory.Physical);
		Register("Brick Break", PokemonType.Fighting, 75, 100, 15, MoveCategory.Physical);
		Register("Yawn", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Knock Off", PokemonType.Dark, 20, 100, 20, MoveCategory.Physical);
		Register("Endeavor", PokemonType.Normal, 55, 100, 5, MoveCategory.Physical);
		Register("Eruption", PokemonType.Fire, 150, 100, 5, MoveCategory.Special);
		Register("Skill Swap", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Imprison", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Refresh", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Grudge", PokemonType.Ghost, 0, 100, 5, MoveCategory.Status);
		Register("Snatch", PokemonType.Dark, 0, 100, 10, MoveCategory.Status);
		Register("Secret Power", PokemonType.Normal, 70, 100, 20, MoveCategory.Physical);
		Register("Dive", PokemonType.Water, 60, 100, 10, MoveCategory.Physical);
		Register("Arm Thrust", PokemonType.Fighting, 15, 100, 20, MoveCategory.Physical);
		Register("Camouflage", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Tail Glow", PokemonType.Bug, 0, 100, 20, MoveCategory.Status);
		Register("Luster Purge", PokemonType.Psychic, 70, 100, 5, MoveCategory.Special);
		Register("Mist Ball", PokemonType.Psychic, 70, 100, 5, MoveCategory.Special);
		Register("Feather Dance", PokemonType.Flying, 0, 100, 15, MoveCategory.Status);
		Register("Teeter Dance", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Blaze Kick", PokemonType.Fire, 85, 90, 10, MoveCategory.Physical);
		Register("Mud Sport", PokemonType.Ground, 0, 100, 15, MoveCategory.Status);
		Register("Ice Ball", PokemonType.Ice, 30, 90, 20, MoveCategory.Physical);
		Register("Needle Arm", PokemonType.Grass, 60, 100, 15, MoveCategory.Physical);
		Register("Slack Off", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Hyper Voice", PokemonType.Normal, 90, 100, 10, MoveCategory.Physical);
		Register("Poison Fang", PokemonType.Poison, 50, 100, 15, MoveCategory.Physical);
		Register("Crush Claw", PokemonType.Normal, 75, 95, 10, MoveCategory.Physical);
		Register("Blast Burn", PokemonType.Fire, 150, 90, 5, MoveCategory.Special);
		Register("Hydro Cannon", PokemonType.Water, 150, 90, 5, MoveCategory.Special);
		Register("Meteor Mash", PokemonType.Steel, 100, 85, 10, MoveCategory.Physical);
		Register("Astonish", PokemonType.Ghost, 30, 100, 15, MoveCategory.Physical);
		Register("Weather Ball", PokemonType.Normal, 50, 100, 10, MoveCategory.Physical);
		Register("Aromatherapy", PokemonType.Grass, 0, 100, 5, MoveCategory.Status);
		Register("Fake Tears", PokemonType.Dark, 0, 100, 20, MoveCategory.Status);
		Register("Air Cutter", PokemonType.Flying, 55, 95, 25, MoveCategory.Physical);
		Register("Overheat", PokemonType.Fire, 140, 90, 5, MoveCategory.Physical);
		Register("Odor Sleuth", PokemonType.Normal, 0, 100, 40, MoveCategory.Status);
		Register("Rock Tomb", PokemonType.Rock, 50, 80, 10, MoveCategory.Physical);
		Register("Silver Wind", PokemonType.Bug, 60, 100, 5, MoveCategory.Physical);
		Register("Metal Sound", PokemonType.Steel, 0, 85, 40, MoveCategory.Status);
		Register("Grass Whistle", PokemonType.Grass, 0, 55, 15, MoveCategory.Status);
		Register("Tickle", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Cosmic Power", PokemonType.Psychic, 0, 100, 20, MoveCategory.Status);
		Register("Water Spout", PokemonType.Water, 150, 100, 5, MoveCategory.Special);
		Register("Signal Beam", PokemonType.Bug, 75, 100, 15, MoveCategory.Physical);
		Register("Shadow Punch", PokemonType.Ghost, 60, 100, 20, MoveCategory.Physical);
		Register("Extrasensory", PokemonType.Psychic, 80, 100, 30, MoveCategory.Special);
		Register("Sky Uppercut", PokemonType.Fighting, 85, 90, 15, MoveCategory.Physical);
		Register("Sand Tomb", PokemonType.Ground, 15, 70, 15, MoveCategory.Physical);
		Register("Sheer Cold", PokemonType.Ice, 120, 30, 5, MoveCategory.Special);
		Register("Muddy Water", PokemonType.Water, 95, 85, 10, MoveCategory.Special);
		Register("Bullet Seed", PokemonType.Grass, 10, 100, 30, MoveCategory.Special);
		Register("Aerial Ace", PokemonType.Flying, 60, 100, 20, MoveCategory.Physical);
		Register("Icicle Spear", PokemonType.Ice, 10, 100, 30, MoveCategory.Special);
		Register("Iron Defense", PokemonType.Steel, 0, 100, 15, MoveCategory.Status);
		Register("Block", PokemonType.Normal, 0, 100, 5, MoveCategory.Status);
		Register("Howl", PokemonType.Normal, 0, 100, 40, MoveCategory.Status);
		Register("Dragon Claw", PokemonType.Dragon, 80, 100, 15, MoveCategory.Physical);
		Register("Frenzy Plant", PokemonType.Grass, 150, 90, 5, MoveCategory.Special);
		Register("Bulk Up", PokemonType.Fighting, 0, 100, 20, MoveCategory.Status);
		Register("Bounce", PokemonType.Flying, 85, 85, 5, MoveCategory.Physical);
		Register("Mud Shot", PokemonType.Ground, 55, 95, 15, MoveCategory.Physical);
		Register("Poison Tail", PokemonType.Poison, 50, 100, 25, MoveCategory.Physical);
		Register("Covet", PokemonType.Normal, 40, 100, 40, MoveCategory.Physical);
		Register("Volt Tackle", PokemonType.Electric, 120, 100, 15, MoveCategory.Physical);
		Register("Magical Leaf", PokemonType.Grass, 60, 100, 20, MoveCategory.Special);
		Register("Water Sport", PokemonType.Water, 0, 100, 15, MoveCategory.Status);
		Register("Calm Mind", PokemonType.Psychic, 0, 100, 20, MoveCategory.Status);
		Register("Leaf Blade", PokemonType.Grass, 70, 100, 15, MoveCategory.Physical);
		Register("Dragon Dance", PokemonType.Dragon, 0, 100, 20, MoveCategory.Status);
		Register("Rock Blast", PokemonType.Rock, 25, 80, 10, MoveCategory.Physical);
		Register("Shock Wave", PokemonType.Electric, 60, 100, 20, MoveCategory.Special);
		Register("Water Pulse", PokemonType.Water, 60, 100, 20, MoveCategory.Special);
		Register("Doom Desire", PokemonType.Steel, 140, 100, 5, MoveCategory.Special);
		Register("Psycho Boost", PokemonType.Psychic, 140, 90, 5, MoveCategory.Special);
		Register("Shadow Sneak", PokemonType.Ghost, 40, 100, 30, MoveCategory.Physical);
		Register("Flash Cannon", PokemonType.Steel, 80, 100, 10, MoveCategory.Special);
		Register("Iron Head", PokemonType.Steel, 80, 100, 15, MoveCategory.Physical);
		Register("X-Scissor", PokemonType.Bug, 80, 100, 15, MoveCategory.Physical);
		Register("Power Gem", PokemonType.Rock, 80, 100, 20, MoveCategory.Special);
		Register("Ice Shard", PokemonType.Ice, 40, 100, 30, MoveCategory.Physical);
		Register("Disarming Voice", PokemonType.Fairy, 40, 100, 15, MoveCategory.Special);
		Register("Draining Kiss", PokemonType.Fairy, 50, 100, 10, MoveCategory.Special);
		Register("Dazzling Gleam", PokemonType.Fairy, 80, 100, 10, MoveCategory.Special);
		Register("Fairy Wind", PokemonType.Fairy, 40, 100, 30, MoveCategory.Special);
		Register("Play Rough", PokemonType.Fairy, 90, 90, 10, MoveCategory.Physical);
		Register("Moonblast", PokemonType.Fairy, 95, 100, 15, MoveCategory.Special);
		Register("Spirit Break", PokemonType.Fairy, 75, 100, 15, MoveCategory.Physical);
		Register("Misty Explosion", PokemonType.Fairy, 100, 100, 5, MoveCategory.Special);

		// Gen 4+ Physical/Special Split Overrides for Gen 1-3 Moves
		Register("Gust", PokemonType.Flying, 40, 100, 35, MoveCategory.Special);
		Register("Razor Wind", PokemonType.Normal, 80, 100, 10, MoveCategory.Special);
		Register("Sonic Boom", PokemonType.Normal, 50, 90, 20, MoveCategory.Special);
		Register("Acid", PokemonType.Poison, 40, 100, 30, MoveCategory.Special);
		Register("Hyper Beam", PokemonType.Normal, 150, 90, 5, MoveCategory.Special);
		Register("Razor Leaf", PokemonType.Grass, 55, 95, 25, MoveCategory.Physical);
		Register("Petal Dance", PokemonType.Grass, 120, 100, 10, MoveCategory.Special);
		Register("Night Shade", PokemonType.Ghost, 60, 100, 15, MoveCategory.Special);
		Register("Sludge", PokemonType.Poison, 65, 100, 20, MoveCategory.Special);
		Register("Swift", PokemonType.Normal, 60, 100, 20, MoveCategory.Special);
		Register("Tri Attack", PokemonType.Normal, 80, 100, 10, MoveCategory.Special);
		Register("Feint Attack", PokemonType.Dark, 60, 100, 20, MoveCategory.Physical);
		Register("Sludge Bomb", PokemonType.Poison, 90, 100, 10, MoveCategory.Special);
		Register("Mud-Slap", PokemonType.Ground, 20, 100, 10, MoveCategory.Special);
		Register("Hidden Power", PokemonType.Normal, 60, 100, 15, MoveCategory.Special);
		Register("Ancient Power", PokemonType.Rock, 60, 100, 5, MoveCategory.Special);
		Register("Shadow Ball", PokemonType.Ghost, 80, 100, 15, MoveCategory.Special);
		Register("Beat Up", PokemonType.Dark, 40, 100, 10, MoveCategory.Physical);
		Register("Uproar", PokemonType.Normal, 90, 100, 10, MoveCategory.Special);
		Register("Spit Up", PokemonType.Normal, 100, 100, 10, MoveCategory.Special);
		Register("Hyper Voice", PokemonType.Normal, 90, 100, 10, MoveCategory.Special);
		Register("Weather Ball", PokemonType.Normal, 50, 100, 10, MoveCategory.Special);
		Register("Air Cutter", PokemonType.Flying, 60, 95, 25, MoveCategory.Special);
		Register("Overheat", PokemonType.Fire, 130, 90, 5, MoveCategory.Special);
		Register("Silver Wind", PokemonType.Bug, 60, 100, 5, MoveCategory.Special);
		Register("Signal Beam", PokemonType.Bug, 75, 100, 15, MoveCategory.Special);
		Register("Bullet Seed", PokemonType.Grass, 25, 100, 30, MoveCategory.Physical);
		Register("Icicle Spear", PokemonType.Ice, 25, 100, 30, MoveCategory.Physical);
		Register("Mud Shot", PokemonType.Ground, 55, 95, 15, MoveCategory.Special);
		Register("Charm", PokemonType.Fairy, 0, 100, 20, MoveCategory.Status);
		Register("Sweet Kiss", PokemonType.Fairy, 0, 75, 10, MoveCategory.Status);
		Register("Moonlight", PokemonType.Fairy, 0, 100, 5, MoveCategory.Status);

		// Gen 4 - Gen 9 Staple & Signature Moves
		Register("Close Combat", PokemonType.Fighting, 120, 100, 5, MoveCategory.Physical);
		Register("Aura Sphere", PokemonType.Fighting, 80, 100, 20, MoveCategory.Special);
		Register("Dragon Pulse", PokemonType.Dragon, 85, 100, 10, MoveCategory.Special);
		Register("Dark Pulse", PokemonType.Dark, 80, 100, 15, MoveCategory.Special);
		Register("Energy Ball", PokemonType.Grass, 90, 100, 10, MoveCategory.Special);
		Register("Earth Power", PokemonType.Ground, 90, 100, 10, MoveCategory.Special);
		Register("Brave Bird", PokemonType.Flying, 120, 100, 15, MoveCategory.Physical);
		Register("Flare Blitz", PokemonType.Fire, 120, 100, 15, MoveCategory.Physical);
		Register("Draco Meteor", PokemonType.Dragon, 130, 90, 5, MoveCategory.Special);
		Register("Leaf Storm", PokemonType.Grass, 130, 90, 5, MoveCategory.Special);
		Register("Giga Impact", PokemonType.Normal, 150, 90, 5, MoveCategory.Physical);
		Register("Stone Edge", PokemonType.Rock, 100, 80, 5, MoveCategory.Physical);
		Register("Poison Jab", PokemonType.Poison, 80, 100, 20, MoveCategory.Physical);
		Register("Night Slash", PokemonType.Dark, 70, 100, 15, MoveCategory.Physical);
		Register("Aqua Tail", PokemonType.Water, 90, 90, 10, MoveCategory.Physical);
		Register("Air Slash", PokemonType.Flying, 75, 95, 15, MoveCategory.Special);
		Register("Bug Buzz", PokemonType.Bug, 90, 100, 10, MoveCategory.Special);
		Register("Psycho Cut", PokemonType.Psychic, 70, 100, 20, MoveCategory.Physical);
		Register("Shadow Claw", PokemonType.Ghost, 70, 100, 15, MoveCategory.Physical);
		Register("Thunder Fang", PokemonType.Electric, 65, 95, 15, MoveCategory.Physical);
		Register("Ice Fang", PokemonType.Ice, 65, 95, 15, MoveCategory.Physical);
		Register("Fire Fang", PokemonType.Fire, 65, 95, 15, MoveCategory.Physical);
		Register("Roar Of Time", PokemonType.Dragon, 150, 90, 5, MoveCategory.Special);
		Register("Spacial Rend", PokemonType.Dragon, 100, 95, 5, MoveCategory.Special);
		Register("Shadow Force", PokemonType.Ghost, 120, 100, 5, MoveCategory.Physical);
		Register("Seed Flare", PokemonType.Grass, 120, 85, 5, MoveCategory.Special);
		Register("Judgment", PokemonType.Normal, 100, 100, 10, MoveCategory.Special);
		Register("Scald", PokemonType.Water, 80, 100, 15, MoveCategory.Special);
		Register("Acrobatics", PokemonType.Flying, 110, 100, 15, MoveCategory.Physical);
		Register("Wild Charge", PokemonType.Electric, 90, 100, 15, MoveCategory.Physical);
		Register("Volt Switch", PokemonType.Electric, 70, 100, 20, MoveCategory.Special);
		Register("U Turn", PokemonType.Bug, 70, 100, 20, MoveCategory.Physical);
		Register("Sacred Sword", PokemonType.Fighting, 90, 100, 15, MoveCategory.Physical);
		Register("Blue Flare", PokemonType.Fire, 130, 85, 5, MoveCategory.Special);
		Register("Bolt Strike", PokemonType.Electric, 130, 85, 5, MoveCategory.Physical);
		Register("Glaciate", PokemonType.Ice, 65, 95, 10, MoveCategory.Special);
		Register("V-Create", PokemonType.Fire, 180, 95, 5, MoveCategory.Physical);
		Register("Hurricane", PokemonType.Flying, 110, 70, 10, MoveCategory.Special);
		Register("Water Shuriken", PokemonType.Water, 75, 100, 20, MoveCategory.Special);
		Register("Oblivion Wing", PokemonType.Flying, 80, 100, 10, MoveCategory.Special);
		Register("Thousand Arrows", PokemonType.Ground, 90, 100, 10, MoveCategory.Physical);
		Register("Spirit Shackle", PokemonType.Ghost, 80, 100, 10, MoveCategory.Physical);
		Register("Darkest Lariat", PokemonType.Dark, 85, 100, 10, MoveCategory.Physical);
		Register("Sparkling Aria", PokemonType.Water, 90, 100, 10, MoveCategory.Special);
		Register("Liquidation", PokemonType.Water, 85, 100, 10, MoveCategory.Physical);
		Register("Sunsteel Strike", PokemonType.Steel, 100, 100, 5, MoveCategory.Physical);
		Register("Moongeist Beam", PokemonType.Ghost, 100, 100, 5, MoveCategory.Special);
		Register("Photon Geyser", PokemonType.Psychic, 100, 100, 5, MoveCategory.Special);
		Register("Plasma Fists", PokemonType.Electric, 100, 100, 15, MoveCategory.Physical);
		Register("Pyro Ball", PokemonType.Fire, 120, 90, 5, MoveCategory.Physical);
		Register("Snipe Shot", PokemonType.Water, 80, 100, 15, MoveCategory.Special);
		Register("Drum Beating", PokemonType.Grass, 80, 100, 10, MoveCategory.Physical);
		Register("Behemoth Blade", PokemonType.Steel, 100, 100, 5, MoveCategory.Physical);
		Register("Behemoth Bash", PokemonType.Steel, 100, 100, 5, MoveCategory.Physical);
		Register("Dynamax Cannon", PokemonType.Dragon, 100, 100, 5, MoveCategory.Special);
		Register("Astral Barrage", PokemonType.Ghost, 120, 100, 5, MoveCategory.Special);
		Register("Glacial Lance", PokemonType.Ice, 120, 100, 5, MoveCategory.Physical);
		Register("Surging Strikes", PokemonType.Water, 75, 100, 5, MoveCategory.Physical);
		Register("Wicked Blow", PokemonType.Dark, 75, 100, 5, MoveCategory.Physical);
		Register("Flower Trick", PokemonType.Grass, 70, 100, 10, MoveCategory.Physical);
		Register("Torch Song", PokemonType.Fire, 80, 100, 10, MoveCategory.Special);
		Register("Aqua Step", PokemonType.Water, 80, 100, 10, MoveCategory.Physical);
		Register("Kowtow Cleave", PokemonType.Dark, 85, 100, 10, MoveCategory.Physical);
		Register("Make It Rain", PokemonType.Steel, 120, 100, 5, MoveCategory.Special);
		Register("Gigaton Hammer", PokemonType.Steel, 160, 100, 5, MoveCategory.Physical);
		Register("Bitter Blade", PokemonType.Fire, 90, 100, 10, MoveCategory.Physical);
		Register("Armor Cannon", PokemonType.Fire, 120, 100, 5, MoveCategory.Special);
		Register("Electro Drift", PokemonType.Electric, 100, 100, 5, MoveCategory.Special);
		Register("Collision Course", PokemonType.Fighting, 100, 100, 5, MoveCategory.Physical);
		Register("Tera Blast", PokemonType.Normal, 80, 100, 10, MoveCategory.Special);
		Register("Ivy Cudgel", PokemonType.Grass, 100, 100, 10, MoveCategory.Physical);
		Register("Blood Moon", PokemonType.Normal, 140, 100, 5, MoveCategory.Special);
		Register("Malignant Chain", PokemonType.Poison, 100, 100, 5, MoveCategory.Special);

		// Modern Gen 4 - Gen 9 TMs & HMs Expansion
		Register("Defog", PokemonType.Flying, 0, 100, 15, MoveCategory.Status);
		Register("Rock Climb", PokemonType.Normal, 90, 85, 20, MoveCategory.Physical);
		Register("Stealth Rock", PokemonType.Rock, 0, 100, 20, MoveCategory.Status);
		Register("Trick Room", PokemonType.Psychic, 0, 100, 5, MoveCategory.Status);
		Register("Roost", PokemonType.Flying, 0, 100, 10, MoveCategory.Status);
		Register("Nasty Plot", PokemonType.Dark, 0, 100, 20, MoveCategory.Status);
		Register("Will-O-Wisp", PokemonType.Fire, 0, 85, 15, MoveCategory.Status);
		Register("Bulldoze", PokemonType.Ground, 60, 100, 20, MoveCategory.Physical);
		Register("Snarl", PokemonType.Dark, 55, 95, 15, MoveCategory.Special);
		Register("Frost Breath", PokemonType.Ice, 60, 90, 10, MoveCategory.Special);
		Register("Mystical Fire", PokemonType.Fire, 75, 100, 10, MoveCategory.Special);
		Register("Flip Turn", PokemonType.Water, 60, 100, 20, MoveCategory.Physical);
		Register("Scale Shot", PokemonType.Dragon, 75, 90, 20, MoveCategory.Physical);
		Register("Triple Axel", PokemonType.Ice, 80, 90, 10, MoveCategory.Physical);
		Register("Poltergeist", PokemonType.Ghost, 110, 90, 5, MoveCategory.Physical);
		Register("Dual Wingbeat", PokemonType.Flying, 80, 90, 10, MoveCategory.Physical);
		Register("Scorching Sands", PokemonType.Ground, 70, 100, 10, MoveCategory.Special);
		Register("Body Press", PokemonType.Fighting, 80, 100, 10, MoveCategory.Physical);
		Register("Breaking Swipe", PokemonType.Dragon, 60, 100, 15, MoveCategory.Physical);
		Register("Trailblaze", PokemonType.Grass, 50, 100, 20, MoveCategory.Physical);
		Register("Chilling Water", PokemonType.Water, 50, 100, 20, MoveCategory.Special);
		Register("Pounce", PokemonType.Bug, 50, 100, 20, MoveCategory.Physical);
		Register("Superpower", PokemonType.Fighting, 120, 100, 5, MoveCategory.Physical);
		Register("High Horsepower", PokemonType.Ground, 95, 95, 10, MoveCategory.Physical);
		Register("Psychic Fangs", PokemonType.Psychic, 85, 100, 10, MoveCategory.Physical);
		Register("Smart Strike", PokemonType.Steel, 70, 100, 10, MoveCategory.Physical);
		Register("Heavy Slam", PokemonType.Steel, 100, 100, 10, MoveCategory.Physical);
		Register("Heat Crash", PokemonType.Fire, 100, 100, 10, MoveCategory.Physical);
		Register("Pollen Puff", PokemonType.Bug, 90, 100, 15, MoveCategory.Special);
		Register("Hex", PokemonType.Ghost, 65, 100, 10, MoveCategory.Special);
		Register("Phantom Force", PokemonType.Ghost, 90, 100, 10, MoveCategory.Physical);
		Register("Accelerock", PokemonType.Rock, 40, 100, 20, MoveCategory.Physical, priority: 1);
		Register("Acid Spray", PokemonType.Poison, 40, 100, 20, MoveCategory.Special);
		Register("Acupressure", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("After You", PokemonType.Normal, 0, 100, 15, MoveCategory.Status);
		Register("Alluring Voice", PokemonType.Fairy, 80, 100, 10, MoveCategory.Special);
		Register("Ally Switch", PokemonType.Psychic, 0, 100, 15, MoveCategory.Status, priority: 2);
		Register("Anchor Shot", PokemonType.Steel, 80, 100, 20, MoveCategory.Physical);
		Register("Apple Acid", PokemonType.Grass, 80, 100, 10, MoveCategory.Special);
		Register("Aqua Cutter", PokemonType.Water, 70, 100, 20, MoveCategory.Physical);
		Register("Aqua Jet", PokemonType.Water, 40, 100, 20, MoveCategory.Physical, priority: 1);
		Register("Aqua Ring", PokemonType.Water, 0, 100, 20, MoveCategory.Status);
		Register("Aromatic Mist", PokemonType.Fairy, 0, 100, 20, MoveCategory.Status);
		Register("Assurance", PokemonType.Dark, 60, 100, 10, MoveCategory.Physical);
		Register("Attack Order", PokemonType.Bug, 90, 100, 15, MoveCategory.Physical);
		Register("Aura Wheel", PokemonType.Electric, 110, 100, 10, MoveCategory.Physical);
		Register("Aurora Veil", PokemonType.Ice, 0, 100, 20, MoveCategory.Status);
		Register("Autotomize", PokemonType.Steel, 0, 100, 15, MoveCategory.Status);
		Register("Avalanche", PokemonType.Ice, 60, 100, 10, MoveCategory.Physical, priority: -4);
		Register("Axe Kick", PokemonType.Fighting, 120, 90, 10, MoveCategory.Physical);
		Register("Baby-Doll Eyes", PokemonType.Fairy, 0, 100, 30, MoveCategory.Status, priority: 1);
		Register("Baneful Bunker", PokemonType.Poison, 0, 100, 10, MoveCategory.Status, priority: 4);
		Register("Barb Barrage", PokemonType.Poison, 60, 100, 10, MoveCategory.Physical);
		Register("Beak Blast", PokemonType.Flying, 100, 100, 15, MoveCategory.Physical, priority: -3);
		Register("Belch", PokemonType.Poison, 120, 90, 10, MoveCategory.Special);
		Register("Bestow", PokemonType.Normal, 0, 100, 15, MoveCategory.Status);
		Register("Bitter Malice", PokemonType.Ghost, 75, 100, 15, MoveCategory.Special);
		Register("Bleakwind Storm", PokemonType.Flying, 100, 80, 10, MoveCategory.Special);
		Register("Bolt Beak", PokemonType.Electric, 85, 100, 10, MoveCategory.Physical);
		Register("Boomburst", PokemonType.Normal, 140, 100, 10, MoveCategory.Special);
		Register("Branch Poke", PokemonType.Grass, 40, 100, 40, MoveCategory.Physical);
		Register("Brine", PokemonType.Water, 65, 100, 10, MoveCategory.Special);
		Register("Brutal Swing", PokemonType.Dark, 60, 100, 20, MoveCategory.Physical);
		Register("Bug Bite", PokemonType.Bug, 60, 100, 20, MoveCategory.Physical);
		Register("Bullet Punch", PokemonType.Steel, 40, 100, 30, MoveCategory.Physical, priority: 1);
		Register("Burn Up", PokemonType.Fire, 130, 100, 5, MoveCategory.Special);
		Register("Burning Bulwark", PokemonType.Fire, 0, 100, 10, MoveCategory.Status, priority: 4);
		Register("Burning Jealousy", PokemonType.Fire, 70, 100, 5, MoveCategory.Special);
		Register("Captivate", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Ceaseless Edge", PokemonType.Dark, 65, 90, 15, MoveCategory.Physical);
		Register("Charge Beam", PokemonType.Electric, 50, 90, 10, MoveCategory.Special);
		Register("Chatter", PokemonType.Flying, 65, 100, 20, MoveCategory.Special);
		Register("Chilly Reception", PokemonType.Ice, 0, 100, 10, MoveCategory.Status);
		Register("Chip Away", PokemonType.Normal, 70, 100, 20, MoveCategory.Physical);
		Register("Chloroblast", PokemonType.Grass, 150, 95, 5, MoveCategory.Special);
		Register("Circle Throw", PokemonType.Fighting, 60, 90, 10, MoveCategory.Physical, priority: -6);
		Register("Clanging Scales", PokemonType.Dragon, 110, 100, 5, MoveCategory.Special);
		Register("Clangorous Soul", PokemonType.Dragon, 0, 100, 5, MoveCategory.Status);
		Register("Clear Smog", PokemonType.Poison, 50, 100, 15, MoveCategory.Special);
		Register("Coaching", PokemonType.Fighting, 0, 100, 10, MoveCategory.Status);
		Register("Coil", PokemonType.Poison, 0, 100, 20, MoveCategory.Status);
		Register("Comeuppance", PokemonType.Dark, 1, 100, 10, MoveCategory.Physical);
		Register("Confide", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Copycat", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Core Enforcer", PokemonType.Dragon, 100, 100, 10, MoveCategory.Special);
		Register("Corrosive Gas", PokemonType.Poison, 0, 100, 40, MoveCategory.Status);
		Register("Cotton Guard", PokemonType.Grass, 0, 100, 10, MoveCategory.Status);
		Register("Court Change", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Crafty Shield", PokemonType.Fairy, 0, 100, 10, MoveCategory.Status, priority: 3);
		Register("Cross Poison", PokemonType.Poison, 70, 100, 20, MoveCategory.Physical);
		Register("Crush Grip", PokemonType.Normal, 120, 100, 5, MoveCategory.Physical);
		Register("Dark Void", PokemonType.Dark, 0, 50, 10, MoveCategory.Status);
		Register("Decorate", PokemonType.Fairy, 0, 100, 15, MoveCategory.Status);
		Register("Defend Order", PokemonType.Bug, 0, 100, 10, MoveCategory.Status);
		Register("Diamond Storm", PokemonType.Rock, 100, 95, 5, MoveCategory.Physical);
		Register("Dire Claw", PokemonType.Poison, 80, 100, 15, MoveCategory.Physical);
		Register("Discharge", PokemonType.Electric, 80, 100, 15, MoveCategory.Special);
		Register("Doodle", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Double Hit", PokemonType.Normal, 35, 90, 10, MoveCategory.Physical);
		Register("Double Iron Bash", PokemonType.Steel, 60, 100, 5, MoveCategory.Physical);
		Register("Double Shock", PokemonType.Electric, 120, 100, 5, MoveCategory.Physical);
		Register("Dragon Ascent", PokemonType.Flying, 120, 100, 5, MoveCategory.Physical);
		Register("Dragon Cheer", PokemonType.Dragon, 0, 100, 15, MoveCategory.Status);
		Register("Dragon Darts", PokemonType.Dragon, 50, 100, 10, MoveCategory.Physical);
		Register("Dragon Energy", PokemonType.Dragon, 150, 100, 5, MoveCategory.Special);
		Register("Dragon Hammer", PokemonType.Dragon, 90, 100, 15, MoveCategory.Physical);
		Register("Dragon Rush", PokemonType.Dragon, 100, 75, 10, MoveCategory.Physical);
		Register("Dragon Tail", PokemonType.Dragon, 60, 90, 10, MoveCategory.Physical, priority: -6);
		Register("Drain Punch", PokemonType.Fighting, 75, 100, 10, MoveCategory.Physical);
		Register("Drill Run", PokemonType.Ground, 80, 95, 10, MoveCategory.Physical);
		Register("Dual Chop", PokemonType.Dragon, 40, 90, 15, MoveCategory.Physical);
		Register("Echoed Voice", PokemonType.Normal, 40, 100, 15, MoveCategory.Special);
		Register("Eerie Impulse", PokemonType.Electric, 0, 100, 15, MoveCategory.Status);
		Register("Eerie Spell", PokemonType.Psychic, 80, 100, 5, MoveCategory.Special);
		Register("Electric Terrain", PokemonType.Electric, 0, 100, 10, MoveCategory.Status);
		Register("Electrify", PokemonType.Electric, 0, 100, 20, MoveCategory.Status);
		Register("Electro Ball", PokemonType.Electric, 1, 100, 10, MoveCategory.Special);
		Register("Electro Shot", PokemonType.Electric, 130, 100, 10, MoveCategory.Special);
		Register("Electroweb", PokemonType.Electric, 55, 95, 15, MoveCategory.Special);
		Register("Embargo", PokemonType.Dark, 0, 100, 15, MoveCategory.Status);
		Register("Entrainment", PokemonType.Normal, 0, 100, 15, MoveCategory.Status);
		Register("Esper Wing", PokemonType.Psychic, 80, 100, 10, MoveCategory.Special);
		Register("Eternabeam", PokemonType.Dragon, 160, 90, 5, MoveCategory.Special);
		Register("Expanding Force", PokemonType.Psychic, 80, 100, 10, MoveCategory.Special);
		Register("Fairy Lock", PokemonType.Fairy, 0, 100, 10, MoveCategory.Status);
		Register("False Surrender", PokemonType.Dark, 80, 100, 10, MoveCategory.Physical);
		Register("Feint", PokemonType.Normal, 30, 100, 10, MoveCategory.Physical, priority: 2);
		Register("Fell Stinger", PokemonType.Bug, 50, 100, 25, MoveCategory.Physical);
		Register("Fickle Beam", PokemonType.Dragon, 80, 100, 5, MoveCategory.Special);
		Register("Fiery Dance", PokemonType.Fire, 80, 100, 10, MoveCategory.Special);
		Register("Fiery Wrath", PokemonType.Dark, 90, 100, 10, MoveCategory.Special);
		Register("Fillet Away", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Final Gambit", PokemonType.Fighting, 1, 100, 5, MoveCategory.Special);
		Register("Fire Lash", PokemonType.Fire, 80, 100, 15, MoveCategory.Physical);
		Register("Fire Pledge", PokemonType.Fire, 80, 100, 10, MoveCategory.Special);
		Register("First Impression", PokemonType.Bug, 90, 100, 10, MoveCategory.Physical, priority: 2);
		Register("Fishious Rend", PokemonType.Water, 85, 100, 10, MoveCategory.Physical);
		Register("Flame Burst", PokemonType.Fire, 70, 100, 15, MoveCategory.Special);
		Register("Flame Charge", PokemonType.Fire, 50, 100, 20, MoveCategory.Physical);
		Register("Fleur Cannon", PokemonType.Fairy, 130, 90, 5, MoveCategory.Special);
		Register("Fling", PokemonType.Dark, 1, 100, 10, MoveCategory.Physical);
		Register("Floral Healing", PokemonType.Fairy, 0, 100, 10, MoveCategory.Status);
		Register("Flower Shield", PokemonType.Fairy, 0, 100, 10, MoveCategory.Status);
		Register("Flying Press", PokemonType.Fighting, 100, 95, 10, MoveCategory.Physical);
		Register("Focus Blast", PokemonType.Fighting, 120, 70, 5, MoveCategory.Special);
		Register("Force Palm", PokemonType.Fighting, 60, 100, 10, MoveCategory.Physical);
		Register("Forest's Curse", PokemonType.Grass, 0, 100, 20, MoveCategory.Status);
		Register("Foul Play", PokemonType.Dark, 95, 100, 15, MoveCategory.Physical);
		Register("Freeze Shock", PokemonType.Ice, 140, 90, 5, MoveCategory.Physical);
		Register("Freeze-Dry", PokemonType.Ice, 70, 100, 20, MoveCategory.Special);
		Register("Freezing Glare", PokemonType.Psychic, 90, 100, 10, MoveCategory.Special);
		Register("Fusion Bolt", PokemonType.Electric, 100, 100, 5, MoveCategory.Physical);
		Register("Fusion Flare", PokemonType.Fire, 100, 100, 5, MoveCategory.Special);
		Register("Gastro Acid", PokemonType.Poison, 0, 100, 10, MoveCategory.Status);
		Register("Gear Grind", PokemonType.Steel, 50, 85, 15, MoveCategory.Physical);
		Register("Gear Up", PokemonType.Steel, 0, 100, 20, MoveCategory.Status);
		Register("Geomancy", PokemonType.Fairy, 0, 100, 10, MoveCategory.Status);
		Register("Glaive Rush", PokemonType.Dragon, 120, 100, 5, MoveCategory.Physical);
		Register("Grass Knot", PokemonType.Grass, 1, 100, 20, MoveCategory.Special);
		Register("Grass Pledge", PokemonType.Grass, 80, 100, 10, MoveCategory.Special);
		Register("Grassy Glide", PokemonType.Grass, 55, 100, 20, MoveCategory.Physical);
		Register("Grassy Terrain", PokemonType.Grass, 0, 100, 10, MoveCategory.Status);
		Register("Grav Apple", PokemonType.Grass, 80, 100, 10, MoveCategory.Physical);
		Register("Gravity", PokemonType.Psychic, 0, 100, 5, MoveCategory.Status);
		Register("Guard Split", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Guard Swap", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Gunk Shot", PokemonType.Poison, 120, 80, 5, MoveCategory.Physical);
		Register("Gyro Ball", PokemonType.Steel, 1, 100, 5, MoveCategory.Physical);
		Register("Hammer Arm", PokemonType.Fighting, 100, 90, 10, MoveCategory.Physical);
		Register("Hard Press", PokemonType.Steel, 100, 100, 10, MoveCategory.Physical);
		Register("Head Charge", PokemonType.Normal, 120, 100, 15, MoveCategory.Physical);
		Register("Head Smash", PokemonType.Rock, 150, 80, 5, MoveCategory.Physical);
		Register("Headlong Rush", PokemonType.Ground, 120, 100, 5, MoveCategory.Physical);
		Register("Heal Block", PokemonType.Psychic, 0, 100, 15, MoveCategory.Status);
		Register("Heal Order", PokemonType.Bug, 0, 100, 10, MoveCategory.Status);
		Register("Heal Pulse", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Healing Wish", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Heart Stamp", PokemonType.Psychic, 60, 100, 25, MoveCategory.Physical);
		Register("Heart Swap", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Hone Claws", PokemonType.Dark, 0, 100, 15, MoveCategory.Status);
		Register("Horn Leech", PokemonType.Grass, 75, 100, 10, MoveCategory.Physical);
		Register("Hydro Steam", PokemonType.Water, 80, 100, 15, MoveCategory.Special);
		Register("Hyper Drill", PokemonType.Normal, 100, 100, 5, MoveCategory.Physical);
		Register("Hyperspace Fury", PokemonType.Dark, 100, 100, 5, MoveCategory.Physical);
		Register("Hyperspace Hole", PokemonType.Psychic, 80, 100, 5, MoveCategory.Special);
		Register("Ice Burn", PokemonType.Ice, 140, 90, 5, MoveCategory.Special);
		Register("Ice Hammer", PokemonType.Ice, 100, 90, 10, MoveCategory.Physical);
		Register("Ice Spinner", PokemonType.Ice, 80, 100, 15, MoveCategory.Physical);
		Register("Icicle Crash", PokemonType.Ice, 85, 90, 10, MoveCategory.Physical);
		Register("Incinerate", PokemonType.Fire, 60, 100, 15, MoveCategory.Special);
		Register("Infernal Parade", PokemonType.Ghost, 60, 100, 15, MoveCategory.Special);
		Register("Inferno", PokemonType.Fire, 100, 50, 5, MoveCategory.Special);
		Register("Infestation", PokemonType.Bug, 20, 100, 20, MoveCategory.Special);
		Register("Instruct", PokemonType.Psychic, 0, 100, 15, MoveCategory.Status);
		Register("Ion Deluge", PokemonType.Electric, 0, 100, 25, MoveCategory.Status, priority: 1);
		Register("Jaw Lock", PokemonType.Dark, 80, 100, 10, MoveCategory.Physical);
		Register("Jet Punch", PokemonType.Water, 60, 100, 15, MoveCategory.Physical, priority: 1);
		Register("Jungle Healing", PokemonType.Grass, 0, 100, 10, MoveCategory.Status);
		Register("King's Shield", PokemonType.Steel, 0, 100, 10, MoveCategory.Status, priority: 4);
		Register("Land's Wrath", PokemonType.Ground, 90, 100, 10, MoveCategory.Physical);
		Register("Laser Focus", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Lash Out", PokemonType.Dark, 75, 100, 5, MoveCategory.Physical);
		Register("Last Resort", PokemonType.Normal, 140, 100, 5, MoveCategory.Physical);
		Register("Last Respects", PokemonType.Ghost, 50, 100, 10, MoveCategory.Physical);
		Register("Lava Plume", PokemonType.Fire, 80, 100, 15, MoveCategory.Special);
		Register("Leaf Tornado", PokemonType.Grass, 65, 90, 10, MoveCategory.Special);
		Register("Leafage", PokemonType.Grass, 40, 100, 40, MoveCategory.Physical);
		Register("Life Dew", PokemonType.Water, 0, 100, 10, MoveCategory.Status);
		Register("Light Of Ruin", PokemonType.Fairy, 140, 90, 5, MoveCategory.Special);
		Register("Low Sweep", PokemonType.Fighting, 65, 100, 20, MoveCategory.Physical);
		Register("Lucky Chant", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Lumina Crash", PokemonType.Psychic, 80, 100, 10, MoveCategory.Special);
		Register("Lunar Blessing", PokemonType.Psychic, 0, 100, 5, MoveCategory.Status);
		Register("Lunar Dance", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Lunge", PokemonType.Bug, 80, 100, 15, MoveCategory.Physical);
		Register("Magic Powder", PokemonType.Psychic, 0, 100, 20, MoveCategory.Status);
		Register("Magic Room", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Magma Storm", PokemonType.Fire, 100, 75, 5, MoveCategory.Special);
		Register("Magnet Bomb", PokemonType.Steel, 60, 100, 20, MoveCategory.Physical);
		Register("Magnet Rise", PokemonType.Electric, 0, 100, 10, MoveCategory.Status);
		Register("Magnetic Flux", PokemonType.Electric, 0, 100, 20, MoveCategory.Status);
		Register("Mat Block", PokemonType.Fighting, 0, 100, 15, MoveCategory.Status);
		Register("Matcha Gotcha", PokemonType.Grass, 80, 90, 15, MoveCategory.Special);
		Register("Me First", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Metal Burst", PokemonType.Steel, 1, 100, 10, MoveCategory.Physical);
		Register("Meteor Assault", PokemonType.Fighting, 150, 100, 5, MoveCategory.Physical);
		Register("Meteor Beam", PokemonType.Rock, 120, 90, 10, MoveCategory.Special);
		Register("Mighty Cleave", PokemonType.Rock, 95, 100, 5, MoveCategory.Physical);
		Register("Mind Blown", PokemonType.Fire, 150, 100, 5, MoveCategory.Special);
		Register("Miracle Eye", PokemonType.Psychic, 0, 100, 40, MoveCategory.Status);
		Register("Mirror Shot", PokemonType.Steel, 65, 85, 10, MoveCategory.Special);
		Register("Misty Terrain", PokemonType.Fairy, 0, 100, 10, MoveCategory.Status);
		Register("Mortal Spin", PokemonType.Poison, 30, 100, 15, MoveCategory.Physical);
		Register("Mountain Gale", PokemonType.Ice, 100, 85, 10, MoveCategory.Physical);
		Register("Mud Bomb", PokemonType.Ground, 65, 85, 10, MoveCategory.Special);
		Register("Multi-Attack", PokemonType.Normal, 120, 100, 10, MoveCategory.Physical);
		Register("Mystical Power", PokemonType.Psychic, 70, 90, 10, MoveCategory.Special);
		Register("Natural Gift", PokemonType.Normal, 1, 100, 15, MoveCategory.Physical);
		Register("Nature's Madness", PokemonType.Fairy, 1, 90, 10, MoveCategory.Special);
		Register("Night Daze", PokemonType.Dark, 85, 95, 10, MoveCategory.Special);
		Register("No Retreat", PokemonType.Fighting, 0, 100, 5, MoveCategory.Status);
		Register("Noble Roar", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Nuzzle", PokemonType.Electric, 20, 100, 20, MoveCategory.Physical);
		Register("Obstruct", PokemonType.Dark, 0, 100, 10, MoveCategory.Status, priority: 4);
		Register("Octolock", PokemonType.Fighting, 0, 100, 15, MoveCategory.Status);
		Register("Ominous Wind", PokemonType.Ghost, 60, 100, 5, MoveCategory.Special);
		Register("Order Up", PokemonType.Dragon, 80, 100, 10, MoveCategory.Physical);
		Register("Origin Pulse", PokemonType.Water, 110, 85, 10, MoveCategory.Special);
		Register("Overdrive", PokemonType.Electric, 80, 100, 10, MoveCategory.Special);
		Register("Parabolic Charge", PokemonType.Electric, 65, 100, 20, MoveCategory.Special);
		Register("Parting Shot", PokemonType.Dark, 0, 100, 20, MoveCategory.Status);
		Register("Payback", PokemonType.Dark, 50, 100, 10, MoveCategory.Physical);
		Register("Petal Blizzard", PokemonType.Grass, 90, 100, 15, MoveCategory.Physical);
		Register("Play Nice", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Pluck", PokemonType.Flying, 60, 100, 20, MoveCategory.Physical);
		Register("Population Bomb", PokemonType.Normal, 20, 90, 10, MoveCategory.Physical);
		Register("Powder", PokemonType.Bug, 0, 100, 20, MoveCategory.Status, priority: 1);
		Register("Power Shift", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Power Split", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Power Swap", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Power Trick", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Power Trip", PokemonType.Dark, 20, 100, 10, MoveCategory.Physical);
		Register("Power Whip", PokemonType.Grass, 120, 85, 10, MoveCategory.Physical);
		Register("Power-Up Punch", PokemonType.Fighting, 40, 100, 20, MoveCategory.Physical);
		Register("Precipice Blades", PokemonType.Ground, 120, 85, 10, MoveCategory.Physical);
		Register("Prismatic Laser", PokemonType.Psychic, 160, 100, 10, MoveCategory.Special);
		Register("Psyblade", PokemonType.Psychic, 80, 100, 15, MoveCategory.Physical);
		Register("Psychic Noise", PokemonType.Psychic, 75, 100, 10, MoveCategory.Special);
		Register("Psychic Terrain", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Psycho Shift", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Psyshield Bash", PokemonType.Psychic, 70, 90, 10, MoveCategory.Physical);
		Register("Psyshock", PokemonType.Psychic, 80, 100, 10, MoveCategory.Special);
		Register("Psystrike", PokemonType.Psychic, 100, 100, 10, MoveCategory.Special);
		Register("Punishment", PokemonType.Dark, 60, 100, 5, MoveCategory.Physical);
		Register("Purify", PokemonType.Poison, 0, 100, 20, MoveCategory.Status);
		Register("Quash", PokemonType.Dark, 0, 100, 15, MoveCategory.Status);
		Register("Quick Guard", PokemonType.Fighting, 0, 100, 15, MoveCategory.Status, priority: 3);
		Register("Quiver Dance", PokemonType.Bug, 0, 100, 20, MoveCategory.Status);
		Register("Rage Fist", PokemonType.Ghost, 50, 100, 10, MoveCategory.Physical);
		Register("Rage Powder", PokemonType.Bug, 0, 100, 20, MoveCategory.Status, priority: 2);
		Register("Raging Bull", PokemonType.Normal, 90, 100, 10, MoveCategory.Physical);
		Register("Raging Fury", PokemonType.Fire, 120, 100, 10, MoveCategory.Physical);
		Register("Razor Shell", PokemonType.Water, 75, 95, 10, MoveCategory.Physical);
		Register("Reflect Type", PokemonType.Normal, 0, 100, 15, MoveCategory.Status);
		Register("Relic Song", PokemonType.Normal, 75, 100, 10, MoveCategory.Special);
		Register("Retaliate", PokemonType.Normal, 70, 100, 5, MoveCategory.Physical);
		Register("Revelation Dance", PokemonType.Normal, 90, 100, 15, MoveCategory.Special);
		Register("Revival Blessing", PokemonType.Normal, 0, 100, 1, MoveCategory.Status);
		Register("Rising Voltage", PokemonType.Electric, 70, 100, 20, MoveCategory.Special);
		Register("Rock Polish", PokemonType.Rock, 0, 100, 20, MoveCategory.Status);
		Register("Rock Wrecker", PokemonType.Rock, 150, 90, 5, MoveCategory.Physical);
		Register("Rototiller", PokemonType.Ground, 0, 100, 10, MoveCategory.Status);
		Register("Round", PokemonType.Normal, 60, 100, 15, MoveCategory.Special);
		Register("Ruination", PokemonType.Dark, 1, 90, 10, MoveCategory.Special);
		Register("Salt Cure", PokemonType.Rock, 40, 100, 15, MoveCategory.Physical);
		Register("Sandsear Storm", PokemonType.Ground, 100, 80, 10, MoveCategory.Special);
		Register("Searing Shot", PokemonType.Fire, 100, 100, 5, MoveCategory.Special);
		Register("Secret Sword", PokemonType.Fighting, 85, 100, 10, MoveCategory.Special);
		Register("Seed Bomb", PokemonType.Grass, 80, 100, 15, MoveCategory.Physical);
		Register("Shadow Bone", PokemonType.Ghost, 85, 100, 10, MoveCategory.Physical);
		Register("Shed Tail", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Shell Side Arm", PokemonType.Poison, 90, 100, 10, MoveCategory.Special);
		Register("Shell Smash", PokemonType.Normal, 0, 100, 15, MoveCategory.Status);
		Register("Shell Trap", PokemonType.Fire, 150, 100, 5, MoveCategory.Special, priority: -3);
		Register("Shelter", PokemonType.Steel, 0, 100, 10, MoveCategory.Status);
		Register("Shift Gear", PokemonType.Steel, 0, 100, 10, MoveCategory.Status);
		Register("Shore Up", PokemonType.Ground, 0, 100, 5, MoveCategory.Status);
		Register("Silk Trap", PokemonType.Bug, 0, 100, 10, MoveCategory.Status, priority: 4);
		Register("Simple Beam", PokemonType.Normal, 0, 100, 15, MoveCategory.Status);
		Register("Skitter Smack", PokemonType.Bug, 70, 90, 10, MoveCategory.Physical);
		Register("Sky Drop", PokemonType.Flying, 60, 100, 10, MoveCategory.Physical);
		Register("Sludge Wave", PokemonType.Poison, 95, 100, 10, MoveCategory.Special);
		Register("Smack Down", PokemonType.Rock, 50, 100, 15, MoveCategory.Physical);
		Register("Smelling Salts", PokemonType.Normal, 70, 100, 10, MoveCategory.Physical);
		Register("Snap Trap", PokemonType.Steel, 35, 100, 15, MoveCategory.Physical);
		Register("Snowscape", PokemonType.Ice, 0, 100, 10, MoveCategory.Status);
		Register("Soak", PokemonType.Water, 0, 100, 20, MoveCategory.Status);
		Register("Solar Blade", PokemonType.Grass, 125, 100, 10, MoveCategory.Physical);
		Register("Spectral Thief", PokemonType.Ghost, 90, 100, 10, MoveCategory.Physical);
		Register("Speed Swap", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Spicy Extract", PokemonType.Grass, 0, 100, 15, MoveCategory.Status);
		Register("Spiky Shield", PokemonType.Grass, 0, 100, 10, MoveCategory.Status, priority: 4);
		Register("Spin Out", PokemonType.Steel, 100, 100, 5, MoveCategory.Physical);
		Register("Spotlight", PokemonType.Normal, 0, 100, 15, MoveCategory.Status, priority: 3);
		Register("Springtide Storm", PokemonType.Fairy, 100, 80, 5, MoveCategory.Special);
		Register("Steam Eruption", PokemonType.Water, 110, 95, 5, MoveCategory.Special);
		Register("Steamroller", PokemonType.Bug, 65, 100, 20, MoveCategory.Physical);
		Register("Steel Beam", PokemonType.Steel, 140, 95, 5, MoveCategory.Special);
		Register("Steel Roller", PokemonType.Steel, 130, 100, 5, MoveCategory.Physical);
		Register("Sticky Web", PokemonType.Bug, 0, 100, 20, MoveCategory.Status);
		Register("Stomping Tantrum", PokemonType.Ground, 75, 100, 10, MoveCategory.Physical);
		Register("Stone Axe", PokemonType.Rock, 65, 90, 15, MoveCategory.Physical);
		Register("Stored Power", PokemonType.Psychic, 20, 100, 10, MoveCategory.Special);
		Register("Storm Throw", PokemonType.Fighting, 60, 100, 10, MoveCategory.Physical);
		Register("Strange Steam", PokemonType.Fairy, 90, 95, 10, MoveCategory.Special);
		Register("Strength Sap", PokemonType.Grass, 0, 100, 10, MoveCategory.Status);
		Register("Struggle Bug", PokemonType.Bug, 50, 100, 20, MoveCategory.Special);
		Register("Stuff Cheeks", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Sucker Punch", PokemonType.Dark, 70, 100, 5, MoveCategory.Physical, priority: 1);
		Register("Supercell Slam", PokemonType.Electric, 100, 95, 15, MoveCategory.Physical);
		Register("Switcheroo", PokemonType.Dark, 0, 100, 10, MoveCategory.Status);
		Register("Synchronoise", PokemonType.Psychic, 120, 100, 10, MoveCategory.Special);
		Register("Syrup Bomb", PokemonType.Grass, 60, 85, 10, MoveCategory.Special);
		Register("Tachyon Cutter", PokemonType.Steel, 50, 100, 10, MoveCategory.Special);
		Register("Tail Slap", PokemonType.Normal, 25, 85, 10, MoveCategory.Physical);
		Register("Tailwind", PokemonType.Flying, 0, 100, 15, MoveCategory.Status);
		Register("Take Heart", PokemonType.Psychic, 0, 100, 15, MoveCategory.Status);
		Register("Tar Shot", PokemonType.Rock, 0, 100, 15, MoveCategory.Status);
		Register("Tearful Look", PokemonType.Normal, 0, 100, 20, MoveCategory.Status);
		Register("Teatime", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Techno Blast", PokemonType.Normal, 120, 100, 5, MoveCategory.Special);
		Register("Telekinesis", PokemonType.Psychic, 0, 100, 15, MoveCategory.Status);
		Register("Temper Flare", PokemonType.Fire, 75, 100, 10, MoveCategory.Physical);
		Register("Tera Starstorm", PokemonType.Normal, 120, 100, 5, MoveCategory.Special);
		Register("Terrain Pulse", PokemonType.Normal, 50, 100, 10, MoveCategory.Special);
		Register("Thousand Waves", PokemonType.Ground, 90, 100, 10, MoveCategory.Physical);
		Register("Throat Chop", PokemonType.Dark, 80, 100, 15, MoveCategory.Physical);
		Register("Thunder Cage", PokemonType.Electric, 80, 90, 15, MoveCategory.Special);
		Register("Thunderclap", PokemonType.Electric, 70, 100, 5, MoveCategory.Special, priority: 1);
		Register("Thunderous Kick", PokemonType.Fighting, 90, 100, 10, MoveCategory.Physical);
		Register("Tidy Up", PokemonType.Normal, 0, 100, 10, MoveCategory.Status);
		Register("Topsy-Turvy", PokemonType.Dark, 0, 100, 20, MoveCategory.Status);
		Register("Toxic Spikes", PokemonType.Poison, 0, 100, 20, MoveCategory.Status);
		Register("Toxic Thread", PokemonType.Poison, 0, 100, 20, MoveCategory.Status);
		Register("Trick-or-Treat", PokemonType.Ghost, 0, 100, 20, MoveCategory.Status);
		Register("Triple Arrows", PokemonType.Fighting, 90, 100, 10, MoveCategory.Physical);
		Register("Triple Dive", PokemonType.Water, 30, 95, 10, MoveCategory.Physical);
		Register("Trop Kick", PokemonType.Grass, 70, 100, 15, MoveCategory.Physical);
		Register("Trump Card", PokemonType.Normal, 1, 100, 5, MoveCategory.Special);
		Register("Twin Beam", PokemonType.Psychic, 40, 100, 10, MoveCategory.Special);
		Register("Upper Hand", PokemonType.Fighting, 65, 100, 15, MoveCategory.Physical, priority: 3);
		Register("Vacuum Wave", PokemonType.Fighting, 40, 100, 30, MoveCategory.Special, priority: 1);
		Register("Venom Drench", PokemonType.Poison, 0, 100, 20, MoveCategory.Status);
		Register("Venoshock", PokemonType.Poison, 65, 100, 10, MoveCategory.Special);
		Register("Victory Dance", PokemonType.Fighting, 0, 100, 20, MoveCategory.Status);
		Register("Wake-Up Slap", PokemonType.Fighting, 70, 100, 10, MoveCategory.Physical);
		Register("Water Pledge", PokemonType.Water, 80, 100, 10, MoveCategory.Special);
		Register("Wave Crash", PokemonType.Water, 120, 100, 10, MoveCategory.Physical);
		Register("Wide Guard", PokemonType.Rock, 0, 100, 10, MoveCategory.Status, priority: 3);
		Register("Wildbolt Storm", PokemonType.Electric, 100, 80, 10, MoveCategory.Special);
		Register("Wonder Room", PokemonType.Psychic, 0, 100, 10, MoveCategory.Status);
		Register("Wood Hammer", PokemonType.Grass, 120, 100, 15, MoveCategory.Physical);
		Register("Work Up", PokemonType.Normal, 0, 100, 30, MoveCategory.Status);
		Register("Worry Seed", PokemonType.Grass, 0, 100, 10, MoveCategory.Status);
		Register("Wring Out", PokemonType.Normal, 120, 100, 5, MoveCategory.Special);
		Register("Zen Headbutt", PokemonType.Psychic, 80, 90, 15, MoveCategory.Physical);
		Register("Zing Zap", PokemonType.Electric, 80, 100, 10, MoveCategory.Physical);
	}

	private static void Register(string name, PokemonType type, int power, int acc, int pp, MoveCategory cat, int priority = 0)
	{
		var move = new PokemonMove(name, type, power, acc, pp, cat)
		{
			Priority = priority
		};
		_database[name] = move;
		_normalizedIndex[NormalizeName(name)] = name;
	}

	/// <summary>Lower-cases a move name and drops spaces, hyphens and punctuation.</summary>
	private static string NormalizeName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "";
		}
		var sb = new StringBuilder(name.Length);
		foreach (char c in name)
		{
			if (char.IsLetterOrDigit(c))
			{
				sb.Append(char.ToLowerInvariant(c));
			}
		}
		return sb.ToString();
	}

	public string GetZMoveName()
	{
		if (Category == MoveCategory.Status)
			return "Z-" + Name;
		return Type switch
		{
			PokemonType.Normal => "Breakneck Blitz",
			PokemonType.Fire => "Inferno Overdrive",
			PokemonType.Water => "Hydro Vortex",
			PokemonType.Grass => "Bloom Doom",
			PokemonType.Electric => "Gigavolt Havoc",
			PokemonType.Ice => "Subzero Slammer",
			PokemonType.Fighting => "All-Out Pummeling",
			PokemonType.Poison => "Acid Downpour",
			PokemonType.Ground => "Tectonic Rage",
			PokemonType.Flying => "Supersonic Skystrike",
			PokemonType.Psychic => "Shattered Psyche",
			PokemonType.Bug => "Savage Spin-Out",
			PokemonType.Rock => "Continental Crush",
			PokemonType.Ghost => "Never-Ending Nightmare",
			PokemonType.Dragon => "Devastating Drake",
			PokemonType.Dark => "Black Hole Eclipse",
			PokemonType.Steel => "Corkscrew Crash",
			PokemonType.Fairy => "Twinkle Tackle",
			_ => "Breakneck Blitz"
		};
	}

	public int GetZMovePower()
	{
		if (Category == MoveCategory.Status) return 0;
		if (Power >= 140) return 200;
		if (Power >= 130) return 195;
		if (Power >= 120) return 190;
		if (Power >= 110) return 185;
		if (Power >= 100) return 180;
		if (Power >= 90) return 175;
		if (Power >= 80) return 160;
		if (Power >= 70) return 140;
		if (Power >= 60) return 120;
		return 100;
	}

	public string GetMaxMoveName(bool isGigantamax = false, string baseSpecies = null)
	{
		if (Category == MoveCategory.Status)
			return "Max Guard";
		if (isGigantamax && !string.IsNullOrEmpty(baseSpecies))
		{
			string sp = baseSpecies.ToLowerInvariant();
			if (sp.Contains("charizard") && Type == PokemonType.Fire) return "G-Max Wildfire";
			if (sp.Contains("venusaur") && Type == PokemonType.Grass) return "G-Max Vine Lash";
			if (sp.Contains("blastoise") && Type == PokemonType.Water) return "G-Max Cannonade";
			if (sp.Contains("pikachu") && Type == PokemonType.Electric) return "G-Max Volt Crash";
			if (sp.Contains("gengar") && Type == PokemonType.Ghost) return "G-Max Terror";
			if (sp.Contains("machamp") && Type == PokemonType.Fighting) return "G-Max Chi Strike";
			if (sp.Contains("lapras") && Type == PokemonType.Ice) return "G-Max Resonance";
			if (sp.Contains("snorlax") && Type == PokemonType.Normal) return "G-Max Replenish";
		}
		return Type switch
		{
			PokemonType.Normal => "Max Strike",
			PokemonType.Fire => "Max Flare",
			PokemonType.Water => "Max Geyser",
			PokemonType.Grass => "Max Overgrowth",
			PokemonType.Electric => "Max Lightning",
			PokemonType.Ice => "Max Hailstorm",
			PokemonType.Fighting => "Max Knuckle",
			PokemonType.Poison => "Max Ooze",
			PokemonType.Ground => "Max Quake",
			PokemonType.Flying => "Max Airstream",
			PokemonType.Psychic => "Max Mindstorm",
			PokemonType.Bug => "Max Flutterby",
			PokemonType.Rock => "Max Rockfall",
			PokemonType.Ghost => "Max Phantasm",
			PokemonType.Dragon => "Max Wyrmwind",
			PokemonType.Dark => "Max Darkness",
			PokemonType.Steel => "Max Steelspike",
			PokemonType.Fairy => "Max Starfall",
			_ => "Max Strike"
		};
	}

	public int GetMaxMovePower()
	{
		if (Category == MoveCategory.Status) return 0;
		if (Type is PokemonType.Fighting or PokemonType.Poison)
		{
			if (Power >= 110) return 100;
			if (Power >= 75) return 90;
			if (Power >= 55) return 80;
			return 70;
		}
		if (Power >= 110) return 140;
		if (Power >= 75) return 130;
		if (Power >= 55) return 110;
		return 90;
	}

	private static readonly HashSet<string> _statusTmWhitelist = new(StringComparer.OrdinalIgnoreCase)
	{
		"Swords Dance", "Calm Mind", "Dragon Dance", "Bulk Up", "Iron Defense", "Nasty Plot",
		"Thunder Wave", "Will-O-Wisp", "Toxic", "Roost", "Recover", "Synthesis", "Moonlight",
		"Hypnosis", "Confuse Ray", "Stealth Rock", "Trick Room", "Protect", "Substitute", "Rest", "Defog"
	};

	public static List<string> AllMoveNames()
	{
		var list = new List<string>();
		foreach (var m in _database.Values)
		{
			if (string.Equals(m.Name, "Struggle", StringComparison.OrdinalIgnoreCase)) continue;
			if (m.Category != MoveCategory.Status || m.GetFieldAbility() != FieldAbilityType.None || _statusTmWhitelist.Contains(m.Name))
			{
				list.Add(m.Name);
			}
		}
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	public static PokemonMove Get(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			name = "Struggle";
		}
		if (_database.TryGetValue(name, out var move))
		{
			return move.Clone();
		}
		if (_aliases.TryGetValue(name, out string canonical) && _database.TryGetValue(canonical, out move))
		{
			return move.Clone();
		}
		if (_normalizedIndex.TryGetValue(NormalizeName(name), out canonical) && _database.TryGetValue(canonical, out move))
		{
			return move.Clone();
		}
		return new PokemonMove(name, PokemonType.Normal, 40, 100, 35, MoveCategory.Physical);
	}

	public FieldAbilityType GetFieldAbility()
	{
		string n = Name.ToLowerInvariant();

		// HM Rock Smash / Strength / Rock Climb
		if (n == "rock smash" || n == "smash" || n == "brick break" || n == "strength" || n == "rock climb")
			return FieldAbilityType.Smash;

		// HM Cut only (or dedicated blade cutting moves)
		if (n == "cut" || n == "slash" || n == "fury cutter" || n == "x-scissor")
			return FieldAbilityType.Cut;

		// Water abilities (watering crops + HM Surf / Waterfall / Dive)
		if (n.Contains("water gun") || n.Contains("surf") || n.Contains("hydro") || n.Contains("bubble") || n == "water pulse" || n == "waterfall" || n == "dive" || n == "muddy water" || n == "scald" || n == "liquidation" || n == "chilling water")
			return FieldAbilityType.Water;

		// Growth abilities (accelerating crops)
		if (n == "growth" || n == "vine whip" || n == "razor leaf" || n == "mega drain" || n == "giga drain" || n == "solar beam" || n == "leaf blade" || n == "magical leaf" || n == "energy ball" || n == "trailblaze")
			return FieldAbilityType.Growth;

		// Weather abilities
		if (n == "rain dance")
			return FieldAbilityType.Rain;

		if (n == "sunny day" || n == "defog")
			return FieldAbilityType.Sunny;

		// Electric flash
		if (n == "flash" || n == "spark" || n == "thundershock" || n == "thunderbolt" || n == "shock wave" || n == "volt tackle")
			return FieldAbilityType.Zap;

		// Ground Tilling (tills soil like a hoe)
		if (n == "dig" || n == "bulldoze" || n == "earthquake")
			return FieldAbilityType.Till;

		// HM Fly (fast-travel soaring) - STRICTLY the move Fly itself!
		if (n == "fly")
			return FieldAbilityType.Fly;

		// Teleport (psychic teleportation)
		if (n == "teleport")
			return FieldAbilityType.Teleport;

		return FieldAbilityType.None;
	}

	/// <summary>
	/// Validates whether a Pokémon species can canonically learn a TM/HM or move.
	/// Prevents illogical moves like teaching Gyarados Giga Drain, Teleport, or Aerial Ace.
	/// </summary>
	public static bool CanLearn(PokemonSpecies species, string moveName)
	{
		if (species == null || string.IsNullOrWhiteSpace(moveName)) return false;

		// 1. Canonical database check (Gen 1-9 pokeemerald-expansion data)
		string specKey = species.InternalName ?? species.Name;
		if (PokemonCanonicalData.IsCanonicallyTeachable(specKey, moveName)) return true;

		// 2. Base species learnset if Mega
		if (species.IsMega && !string.IsNullOrEmpty(species.BaseSpeciesName))
		{
			if (PokemonCanonicalData.IsCanonicallyTeachable(species.BaseSpeciesName, moveName)) return true;
			var baseSpec = PokemonSpecies.Get(species.BaseSpeciesName);
			if (baseSpec != null && CanLearn(baseSpec, moveName)) return true;
		}

		// 3. Natural learnset
		if (species.LearnableMoves != null)
		{
			foreach (var m in species.LearnableMoves)
			{
				if (string.Equals(m, moveName, StringComparison.OrdinalIgnoreCase)) return true;
			}
		}

		string spName = species.Name.ToLowerInvariant();
		string mv = moveName.ToLowerInvariant();
		var moveData = Get(moveName);
		var moveType = moveData.Type;

		// 3. Strict physiological restrictions & exceptions
		if (mv == "fly")
		{
			// Canon: Gyarados CANNOT learn Fly despite being Flying type!
			if (spName == "gyarados") return false;
			return species.PrimaryType == PokemonType.Flying || species.SecondaryType == PokemonType.Flying
				|| spName is "dragonite" or "salamence" or "rayquaza" or "golurk" or "volcarona" or "corviknight" or "latios" or "latias";
		}

		if (mv == "aerial ace")
		{
			// Aerial Ace requires wings, blades, or razor claws. Gyarados cannot learn it.
			if (spName == "gyarados") return false;
			if (species.PrimaryType == PokemonType.Flying || species.SecondaryType == PokemonType.Flying) return true;
			return species.PrimaryType == PokemonType.Bug || species.SecondaryType == PokemonType.Bug
				|| spName is "scyther" or "scizor" or "sceptile" or "charizard" or "greninja" or "kartana";
		}

		if (mv == "teleport")
		{
			// Teleport is strictly Psychic-types, dimensional or fairy/magical species
			if (spName == "gyarados") return false;
			return species.PrimaryType == PokemonType.Psychic || species.SecondaryType == PokemonType.Psychic
				|| spName is "mew" or "mewtwo" or "clefairy" or "clefable" or "starmie" or "porygon" or "celebi";
		}

		if (mv is "giga drain" or "mega drain" or "absorb" or "leech seed")
		{
			// Only Grass, Bug, Poison, and select Ghost/Fairy types. Water/Dragon/Flying without Grass CANNOT learn Giga Drain.
			if (species.PrimaryType == PokemonType.Grass || species.SecondaryType == PokemonType.Grass) return true;
			if (species.PrimaryType == PokemonType.Bug || species.SecondaryType == PokemonType.Bug) return true;
			if (species.PrimaryType == PokemonType.Poison || species.SecondaryType == PokemonType.Poison) return true;
			return spName is "mew" or "gengar" or "haunter" or "gastly" or "chandelure" or "trevenant";
		}

		// STAB compatibility: Any Pokémon can learn TMs matching its Primary or Secondary type
		if (species.PrimaryType == moveType || species.SecondaryType == moveType)
		{
			return true;
		}

		// Common universal TMs
		if (mv is "protect" or "substitute" or "rest" or "facade" or "round" or "snore" or "sleep talk" or "toxic" or "hidden power" or "return" or "frustration")
		{
			if (spName is "magikarp" or "caterpie" or "weedle" or "metapod" or "kakuna" or "ditto" or "unown" or "wynaut" or "cosmog")
				return false;
			return true;
		}

		// Heavy / draconic / elemental coverage for large monsters (like Gyarados, Tyranitar, Nidoking, Snorlax, Dragonite)
		if (spName is "gyarados" or "tyranitar" or "dragonite" or "snorlax" or "nidoking" or "nidoqueen" or "charizard" or "feraligatr" or "garchomp" or "salamence")
		{
			if (moveType is PokemonType.Normal or PokemonType.Water or PokemonType.Ice or PokemonType.Dragon or PokemonType.Dark) return true;
			if (mv is "flamethrower" or "fire blast" or "thunderbolt" or "thunder" or "earthquake" or "stone edge" or "outrage" or "iron head" or "crunch" or "bite") return true;
		}

		// Standard Normal moves for most species
		if (moveType == PokemonType.Normal && moveData.Category == MoveCategory.Physical)
		{
			return true;
		}

		return false;
	}

	public bool IsHM
	{
		get
		{
			string n = Name.ToLowerInvariant();
			return n is "cut" or "fly" or "surf" or "strength" or "flash" or "rock smash" or "waterfall" or "dive" or "defog" or "rock climb";
		}
	}
}

public enum FieldAbilityType
{
	None,
	Cut,
	Smash,
	Water,
	Growth,
	Rain,
	Sunny,
	Zap,
	Dash,
	Fly,
	Teleport,
	Till
}