using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace StardewValley.Pokemon;

public class PokemonSaveDto
{
	public string Species { get; set; }
	public string Nickname { get; set; }
	public int Level { get; set; }
	public int CurrentHP { get; set; }
	public int CurrentExp { get; set; }
	public string Nature { get; set; }
	public int IV_HP { get; set; } = 31;
	public int IV_Attack { get; set; } = 31;
	public int IV_Defense { get; set; } = 31;
	public int IV_SpAttack { get; set; } = 31;
	public int IV_SpDefense { get; set; } = 31;
	public int IV_Speed { get; set; } = 31;
	public int EV_HP { get; set; } = 0;
	public int EV_Attack { get; set; } = 0;
	public int EV_Defense { get; set; } = 0;
	public int EV_SpAttack { get; set; } = 0;
	public int EV_SpDefense { get; set; } = 0;
	public int EV_Speed { get; set; } = 0;
	public bool IsEgg { get; set; } = false;
	public int EggSteps { get; set; } = 0;
	public bool IsShiny { get; set; } = false;
	public string Gender { get; set; } = "♂";
	public string Status { get; set; } = "None";
	public string UsedMegaStoneId { get; set; } = null;
	public string HeldItem { get; set; } = null;
	public string Ability { get; set; } = null;
	public int Friendship { get; set; } = 70;
	public string TeraType { get; set; } = null;
	public List<string> Moves { get; set; } = new();
	/// <summary>Remaining PP per move, parallel to <see cref="Moves"/>.</summary>
	public List<int> MovePP { get; set; } = new();
}

public class PlayerParty
{
	private static PlayerParty _instance;
	public static PlayerParty Instance => _instance ??= new PlayerParty();

	public List<PokemonInstance> Team { get; } = new();
	public List<PokemonInstance> GetParty() => Team;

	public bool HasChosenStarter => Team.Count > 0;

	public int ActiveIndex { get; set; } = 0;

	private PlayerParty()
	{
		LoadParty();
	}

	public PokemonInstance ActivePokemon
	{
		get
		{
			if (ActiveIndex >= 0 && ActiveIndex < Team.Count && !Team[ActiveIndex].IsFainted)
			{
				return Team[ActiveIndex];
			}
			for (int i = 0; i < Team.Count; i++)
			{
				if (!Team[i].IsFainted)
				{
					ActiveIndex = i;
					return Team[i];
				}
			}
			return null;
		}
	}

	public bool SetActive(int index, bool showWarning = true)
	{
		if (index >= 0 && index < Team.Count)
		{
			if (Team[index].IsFainted)
			{
				if (showWarning)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage($"{Team[index].Nickname} está desmaiado e não pode ser ativado!");
				}
				return false;
			}
			ActiveIndex = index;
			Game1.playSound("coin");
			Game1.addHUDMessage(new HUDMessage($"{Team[index].Nickname} agora está ativo!", 2));
			return true;
		}
		return false;
	}

	public void CycleActive()
	{
		if (Team.Count <= 1) return;
		for (int offset = 1; offset <= Team.Count; offset++)
		{
			int next = (ActiveIndex + offset) % Team.Count;
			if (!Team[next].IsFainted)
			{
				SetActive(next, showWarning: false);
				return;
			}
		}
	}

	public void AddPokemon(PokemonInstance pokemon)
	{
		if (pokemon?.Species != null)
		{
			PokemonPokedex.Instance.RecordCaught(pokemon.Species.Name);
		}

		if (Team.Count < 6)
		{
			Team.Add(pokemon);
			Game1.addHUDMessage(new HUDMessage($"{pokemon.Nickname} entrou para sua equipe Pokémon!", 2));
		}
		else
		{
			if (PokemonStorage.Instance.Deposit(pokemon))
			{
				Game1.addHUDMessage(new HUDMessage($"{pokemon.Nickname} foi enviado para o PC (Caixa {PokemonStorage.Instance.LastDepositedBox + 1})!", 2));
			}
			else
			{
				Game1.showRedMessage($"Não há espaço nem na equipe nem no PC para {pokemon.Nickname}!");
			}
		}
		SaveParty();
	}

	public bool RemovePokemon(PokemonInstance pokemon)
	{
		if (pokemon == null) return false;
		bool removed = Team.Remove(pokemon);
		if (removed)
		{
			if (ActiveIndex >= Team.Count)
			{
				ActiveIndex = Math.Max(0, Team.Count - 1);
			}
			SaveParty();
		}
		return removed;
	}

	public void ChooseStarter(string speciesName)
	{
		PokemonSpecies species = PokemonSpecies.Get(speciesName) ?? PokemonSpecies.Get("Pikachu");
		PokemonInstance starter = new PokemonInstance(species, 5);
		Team.Clear();
		AddPokemon(starter);
		PokemonPokedex.Instance.RecordCaught(species.Name);

		// Starter kit: 5 Poké Balls and 1 Potion
		if (Game1.player != null)
		{
			Item balls = ItemRegistry.Create("(O)PokeBall", 5, 0, allowNull: true) ?? new StardewValley.Object("PokeBall", 5);
			Item potion = ItemRegistry.Create("(O)PokemonPotion", 1, 0, allowNull: true) ?? new StardewValley.Object("PokemonPotion", 1);
			if (balls != null) Game1.player.addItemToInventoryBool(balls);
			if (potion != null) Game1.player.addItemToInventoryBool(potion);
		}

		Game1.addHUDMessage(new HUDMessage($"Você escolheu {species.Name} como seu parceiro Pokémon! (+5 Pokébolas, +1 Potion)", 2));
		SaveParty();
	}

	public void HealAll()
	{
		foreach (var p in Team)
		{
			p.FullRestore();
		}
		SaveParty();
	}

	public void Swap(int indexA, int indexB)
	{
		if (indexA < 0 || indexA >= Team.Count || indexB < 0 || indexB >= Team.Count) return;
		if (ActiveIndex == indexA) ActiveIndex = indexB;
		else if (ActiveIndex == indexB) ActiveIndex = indexA;
		(Team[indexA], Team[indexB]) = (Team[indexB], Team[indexA]);
		SaveParty();
	}

	public bool ReviveFirstFainted()
	{
		foreach (var p in Team)
		{
			if (p.IsFainted)
			{
				p.Revive(0.5f);
				SaveParty();
				return true;
			}
		}
		return false;
	}

	public bool HasFaintedPokemon()
	{
		foreach (var p in Team)
		{
			if (p.IsFainted) return true;
		}
		return false;
	}

	/// <summary>Directory that holds the unified Pokémon save document, or null when no save is loaded.</summary>
	public static string GetCurrentSaveDirectory()
	{
		try
		{
			string path = PokemonSaveStore.CurrentFilePath;
			return string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>Clears the team and starts a blank document for a new playthrough.</summary>
	public void ResetForNewGame()
	{
		Team.Clear();
		ActiveIndex = 0;
		PokemonSaveStore.ResetForNewGame();
	}

	/// <summary>Re-reads the whole save document after a save slot is loaded.</summary>
	public void Reload()
	{
		Team.Clear();
		ActiveIndex = 0;
		PokemonSaveStore.Reload();
		LoadParty();
	}

	public void SaveParty()
	{
		try
		{
			PokemonSaveFile save = PokemonSaveStore.Data;
			save.Party = new List<PokemonSaveDto>(Team.Count);
			foreach (var pokemon in Team)
			{
				PokemonSaveDto dto = PokemonDtoMapper.ToDto(pokemon);
				if (dto != null)
				{
					save.Party.Add(dto);
				}
			}
			save.ActiveIndex = ActiveIndex;
			PokemonSaveStore.Save(PokemonSaveSection.Party);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PlayerParty] Falha ao salvar a equipe: {ex.Message}");
		}
	}

	public void MarkSeen(string speciesNameOrInternal)
	{
		if (!string.IsNullOrWhiteSpace(speciesNameOrInternal))
		{
			PokemonPokedex.Instance.RecordSeen(speciesNameOrInternal);
		}
	}

	public void LoadParty()
	{
		try
		{
			PokemonSaveFile save = PokemonSaveStore.Data;
			if (save.Party == null || save.Party.Count == 0)
			{
				return;
			}

			var loaded = new List<PokemonInstance>(save.Party.Count);
			foreach (PokemonSaveDto dto in save.Party)
			{
				PokemonInstance pokemon = PokemonDtoMapper.FromDto(dto);
				if (pokemon != null)
				{
					loaded.Add(pokemon);
				}
			}

			if (loaded.Count == 0)
			{
				return;
			}

			Team.Clear();
			Team.AddRange(loaded);
			ActiveIndex = Math.Clamp(save.ActiveIndex, 0, Team.Count - 1);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PlayerParty] Falha ao carregar a equipe: {ex.Message}");
		}
	}
}