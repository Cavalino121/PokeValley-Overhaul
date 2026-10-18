using System;
using Microsoft.Xna.Framework;

namespace StardewValley.Pokemon;

public static class PokemonTypeColors
{
	public static Color Get(PokemonType type)
	{
		return type switch
		{
			PokemonType.Normal => new Color(168, 168, 120),
			PokemonType.Fire => new Color(240, 128, 48),
			PokemonType.Water => new Color(104, 144, 240),
			PokemonType.Grass => new Color(120, 200, 80),
			PokemonType.Electric => new Color(248, 208, 48),
			PokemonType.Ice => new Color(152, 216, 216),
			PokemonType.Fighting => new Color(192, 48, 40),
			PokemonType.Poison => new Color(160, 64, 160),
			PokemonType.Ground => new Color(224, 192, 104),
			PokemonType.Flying => new Color(168, 144, 240),
			PokemonType.Psychic => new Color(248, 88, 136),
			PokemonType.Bug => new Color(168, 184, 32),
			PokemonType.Rock => new Color(184, 160, 56),
			PokemonType.Ghost => new Color(112, 88, 152),
			PokemonType.Dragon => new Color(112, 56, 248),
			PokemonType.Steel => new Color(184, 184, 208),
			PokemonType.Dark => new Color(112, 88, 72),
			PokemonType.Fairy => new Color(238, 153, 172),
			_ => Color.Gray
		};
	}
}

public enum PokemonType
{
	Normal,
	Fire,
	Water,
	Grass,
	Electric,
	Ice,
	Fighting,
	Poison,
	Ground,
	Flying,
	Psychic,
	Bug,
	Rock,
	Ghost,
	Dragon,
	Steel,
	Dark,
	Fairy
}

public static class TypeChart
{
	public static float GetEffectiveness(PokemonType attackType, PokemonType defenderType)
	{
		return attackType switch
		{
			PokemonType.Normal => defenderType switch
			{
				PokemonType.Rock or PokemonType.Steel => 0.5f,
				PokemonType.Ghost => 0.0f,
				_ => 1.0f
			},
			PokemonType.Fire => defenderType switch
			{
				PokemonType.Fire or PokemonType.Water or PokemonType.Rock or PokemonType.Dragon => 0.5f,
				PokemonType.Grass or PokemonType.Ice or PokemonType.Bug or PokemonType.Steel => 2.0f,
				_ => 1.0f
			},
			PokemonType.Water => defenderType switch
			{
				PokemonType.Water or PokemonType.Grass or PokemonType.Dragon => 0.5f,
				PokemonType.Fire or PokemonType.Ground or PokemonType.Rock => 2.0f,
				_ => 1.0f
			},
			PokemonType.Grass => defenderType switch
			{
				PokemonType.Fire or PokemonType.Grass or PokemonType.Poison or PokemonType.Flying or PokemonType.Bug or PokemonType.Dragon or PokemonType.Steel => 0.5f,
				PokemonType.Water or PokemonType.Ground or PokemonType.Rock => 2.0f,
				_ => 1.0f
			},
			PokemonType.Electric => defenderType switch
			{
				PokemonType.Electric or PokemonType.Grass or PokemonType.Dragon => 0.5f,
				PokemonType.Ground => 0.0f,
				PokemonType.Water or PokemonType.Flying => 2.0f,
				_ => 1.0f
			},
			PokemonType.Ice => defenderType switch
			{
				PokemonType.Fire or PokemonType.Water or PokemonType.Ice or PokemonType.Steel => 0.5f,
				PokemonType.Grass or PokemonType.Ground or PokemonType.Flying or PokemonType.Dragon => 2.0f,
				_ => 1.0f
			},
			PokemonType.Fighting => defenderType switch
			{
				PokemonType.Poison or PokemonType.Flying or PokemonType.Psychic or PokemonType.Bug or PokemonType.Fairy => 0.5f,
				PokemonType.Ghost => 0.0f,
				PokemonType.Normal or PokemonType.Ice or PokemonType.Rock or PokemonType.Dark or PokemonType.Steel => 2.0f,
				_ => 1.0f
			},
			PokemonType.Poison => defenderType switch
			{
				PokemonType.Poison or PokemonType.Ground or PokemonType.Rock or PokemonType.Ghost => 0.5f,
				PokemonType.Steel => 0.0f,
				PokemonType.Grass or PokemonType.Fairy => 2.0f,
				_ => 1.0f
			},
			PokemonType.Ground => defenderType switch
			{
				PokemonType.Grass or PokemonType.Bug => 0.5f,
				PokemonType.Flying => 0.0f,
				PokemonType.Fire or PokemonType.Electric or PokemonType.Poison or PokemonType.Rock or PokemonType.Steel => 2.0f,
				_ => 1.0f
			},
			PokemonType.Flying => defenderType switch
			{
				PokemonType.Electric or PokemonType.Rock or PokemonType.Steel => 0.5f,
				PokemonType.Grass or PokemonType.Fighting or PokemonType.Bug => 2.0f,
				_ => 1.0f
			},
			PokemonType.Psychic => defenderType switch
			{
				PokemonType.Psychic or PokemonType.Steel => 0.5f,
				PokemonType.Dark => 0.0f,
				PokemonType.Fighting or PokemonType.Poison => 2.0f,
				_ => 1.0f
			},
			PokemonType.Bug => defenderType switch
			{
				PokemonType.Fire or PokemonType.Fighting or PokemonType.Poison or PokemonType.Flying or PokemonType.Ghost or PokemonType.Steel or PokemonType.Fairy => 0.5f,
				PokemonType.Grass or PokemonType.Psychic or PokemonType.Dark => 2.0f,
				_ => 1.0f
			},
			PokemonType.Rock => defenderType switch
			{
				PokemonType.Fighting or PokemonType.Ground or PokemonType.Steel => 0.5f,
				PokemonType.Fire or PokemonType.Ice or PokemonType.Flying or PokemonType.Bug => 2.0f,
				_ => 1.0f
			},
			PokemonType.Ghost => defenderType switch
			{
				PokemonType.Dark => 0.5f,
				PokemonType.Normal => 0.0f,
				PokemonType.Psychic or PokemonType.Ghost => 2.0f,
				_ => 1.0f
			},
			PokemonType.Dragon => defenderType switch
			{
				PokemonType.Steel => 0.5f,
				PokemonType.Fairy => 0.0f,
				PokemonType.Dragon => 2.0f,
				_ => 1.0f
			},
			PokemonType.Steel => defenderType switch
			{
				PokemonType.Fire or PokemonType.Water or PokemonType.Electric or PokemonType.Steel => 0.5f,
				PokemonType.Ice or PokemonType.Rock or PokemonType.Fairy => 2.0f,
				_ => 1.0f
			},
			PokemonType.Dark => defenderType switch
			{
				PokemonType.Fighting or PokemonType.Dark or PokemonType.Fairy => 0.5f,
				PokemonType.Psychic or PokemonType.Ghost => 2.0f,
				_ => 1.0f
			},
			PokemonType.Fairy => defenderType switch
			{
				PokemonType.Fire or PokemonType.Poison or PokemonType.Steel => 0.5f,
				PokemonType.Fighting or PokemonType.Dragon or PokemonType.Dark => 2.0f,
				_ => 1.0f
			},
			_ => 1.0f
		};
	}
}
