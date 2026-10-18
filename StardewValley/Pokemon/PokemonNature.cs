using System;

namespace StardewValley.Pokemon;

public enum PokemonNature
{
	Hardy,
	Lonely,
	Brave,
	Adamant,
	Naughty,
	Bold,
	Docile,
	Relaxed,
	Impish,
	Lax,
	Timid,
	Hasty,
	Serious,
	Jolly,
	Naive,
	Modest,
	Mild,
	Quiet,
	Bashful,
	Rash,
	Calm,
	Gentle,
	Sassy,
	Careful,
	Quirky
}

public enum PokemonStat
{
	HP,
	Attack,
	Defense,
	SpAttack,
	SpDefense,
	Speed
}

public static class PokemonNatureExtensions
{
	public static float GetMultiplier(this PokemonNature nature, PokemonStat stat)
	{
		(PokemonStat increased, PokemonStat decreased) = nature switch
		{
			PokemonNature.Lonely  => (PokemonStat.Attack, PokemonStat.Defense),
			PokemonNature.Brave   => (PokemonStat.Attack, PokemonStat.Speed),
			PokemonNature.Adamant => (PokemonStat.Attack, PokemonStat.SpAttack),
			PokemonNature.Naughty => (PokemonStat.Attack, PokemonStat.SpDefense),

			PokemonNature.Bold    => (PokemonStat.Defense, PokemonStat.Attack),
			PokemonNature.Relaxed => (PokemonStat.Defense, PokemonStat.Speed),
			PokemonNature.Impish  => (PokemonStat.Defense, PokemonStat.SpAttack),
			PokemonNature.Lax     => (PokemonStat.Defense, PokemonStat.SpDefense),

			PokemonNature.Timid   => (PokemonStat.Speed, PokemonStat.Attack),
			PokemonNature.Hasty   => (PokemonStat.Speed, PokemonStat.Defense),
			PokemonNature.Jolly   => (PokemonStat.Speed, PokemonStat.SpAttack),
			PokemonNature.Naive   => (PokemonStat.Speed, PokemonStat.SpDefense),

			PokemonNature.Modest  => (PokemonStat.SpAttack, PokemonStat.Attack),
			PokemonNature.Mild    => (PokemonStat.SpAttack, PokemonStat.Defense),
			PokemonNature.Quiet   => (PokemonStat.SpAttack, PokemonStat.Speed),
			PokemonNature.Rash    => (PokemonStat.SpAttack, PokemonStat.SpDefense),

			PokemonNature.Calm    => (PokemonStat.SpDefense, PokemonStat.Attack),
			PokemonNature.Gentle  => (PokemonStat.SpDefense, PokemonStat.Defense),
			PokemonNature.Sassy   => (PokemonStat.SpDefense, PokemonStat.Speed),
			PokemonNature.Careful => (PokemonStat.SpDefense, PokemonStat.SpAttack),

			_ => (PokemonStat.HP, PokemonStat.HP) // Neutral natures (Hardy, Docile, Serious, Bashful, Quirky)
		};

		if (increased == decreased) return 1.0f;
		if (stat == increased) return 1.10f;
		if (stat == decreased) return 0.90f;
		return 1.0f;
	}

	public static string GetDescription(this PokemonNature nature)
	{
		return nature switch
		{
			PokemonNature.Lonely  => "Solitária (+Ataque, -Defesa)",
			PokemonNature.Brave   => "Brava (+Ataque, -Velocidade)",
			PokemonNature.Adamant => "Firme (+Ataque, -Sp.Atk)",
			PokemonNature.Naughty => "Travessa (+Ataque, -Sp.Def)",

			PokemonNature.Bold    => "Ousada (+Defesa, -Ataque)",
			PokemonNature.Relaxed => "Relaxada (+Defesa, -Velocidade)",
			PokemonNature.Impish  => "Pérfida (+Defesa, -Sp.Atk)",
			PokemonNature.Lax     => "Frouxa (+Defesa, -Sp.Def)",

			PokemonNature.Timid   => "Tímida (+Velocidade, -Ataque)",
			PokemonNature.Hasty   => "Apressada (+Velocidade, -Defesa)",
			PokemonNature.Jolly   => "Alegre (+Velocidade, -Sp.Atk)",
			PokemonNature.Naive   => "Ingênua (+Velocidade, -Sp.Def)",

			PokemonNature.Modest  => "Modesta (+Sp.Atk, -Ataque)",
			PokemonNature.Mild    => "Suave (+Sp.Atk, -Defesa)",
			PokemonNature.Quiet   => "Quieta (+Sp.Atk, -Velocidade)",
			PokemonNature.Rash    => "Imprudente (+Sp.Atk, -Sp.Def)",

			PokemonNature.Calm    => "Calma (+Sp.Def, -Ataque)",
			PokemonNature.Gentle  => "Gentil (+Sp.Def, -Defesa)",
			PokemonNature.Sassy   => "Atrevida (+Sp.Def, -Velocidade)",
			PokemonNature.Careful => "Cuidadosa (+Sp.Def, -Sp.Atk)",

			PokemonNature.Hardy   => "Resistente (Neutra)",
			PokemonNature.Docile  => "Dócil (Neutra)",
			PokemonNature.Serious => "Séria (Neutra)",
			PokemonNature.Bashful => "Envergonhada (Neutra)",
			PokemonNature.Quirky  => "Excêntrica (Neutra)",
			_ => $"{nature} (Neutra)"
		};
	}

	public static string GetDisplayName(this PokemonNature nature) => GetDescription(nature);
}
