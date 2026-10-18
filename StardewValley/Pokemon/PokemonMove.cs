using System;
using System.Collections.Generic;

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

public class PokemonMove
{
	public string Name { get; set; }
	public PokemonType Type { get; set; }
	public int Power { get; set; }
	public int Accuracy { get; set; }
	public int MaxPP { get; set; }
	public int CurrentPP { get; set; }
	public MoveCategory Category { get; set; }
	public PokemonStatusCondition StatusEffect { get; set; } = PokemonStatusCondition.None;
	public int StatusChance { get; set; } = 0;
	public bool InflictsConfusion { get; set; } = false;
	public int ConfusionChance { get; set; } = 0;
	public float DrainRatio { get; set; } = 0f;
	public float SelfHealRatio { get; set; } = 0f;

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
	}

	public PokemonMove Clone()
	{
		return new PokemonMove(Name, Type, Power, Accuracy, MaxPP, Category)
		{
			CurrentPP = this.CurrentPP,
			StatusEffect = this.StatusEffect,
			StatusChance = this.StatusChance,
			InflictsConfusion = this.InflictsConfusion,
			ConfusionChance = this.ConfusionChance,
			DrainRatio = this.DrainRatio,
			SelfHealRatio = this.SelfHealRatio
		};
	}

	private static readonly Dictionary<string, PokemonMove> _database = new(StringComparer.OrdinalIgnoreCase);

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
		Register("Feint Attack", PokemonType.Dark, 60, 100, 20, MoveCategory.Special);
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
	}

	private static void Register(string name, PokemonType type, int power, int acc, int pp, MoveCategory cat)
	{
		_database[name] = new PokemonMove(name, type, power, acc, pp, cat);
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
		if (_database.TryGetValue(name, out var move))
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
