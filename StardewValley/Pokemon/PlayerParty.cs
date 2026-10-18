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
	public List<string> Moves { get; set; } = new();
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

	public static string GetCurrentSaveDirectory()
	{
		try
		{
			if (Game1.uniqueIDForThisGame != 0UL)
			{
				string folderName = SaveGame.FilterFileName(Game1.GetSaveGameName()) + "_" + Game1.uniqueIDForThisGame;
				string savesPath = Program.GetSavesFolder();
				if (!string.IsNullOrEmpty(savesPath))
				{
					string dir = Path.Combine(savesPath, folderName);
					if (!Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}
					return dir;
				}
			}
		}
		catch { }
		return null;
	}

	private static string GetSaveFilePath()
	{
		try
		{
			string saveDir = GetCurrentSaveDirectory();
			if (!string.IsNullOrEmpty(saveDir))
			{
				return Path.Combine(saveDir, "pokemon_party_data.json");
			}
			string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");
			if (Directory.Exists(appData))
			{
				return Path.Combine(appData, "pokemon_party_data.json");
			}
		}
		catch { }
		return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokemon_party_data.json");
	}

	public void ResetForNewGame()
	{
		Team.Clear();
		ActiveIndex = 0;
	}

	public void Reload()
	{
		Team.Clear();
		ActiveIndex = 0;
		LoadParty();
	}

	public void SaveParty()
	{
		try
		{
			var list = new List<PokemonSaveDto>();
			foreach (var p in Team)
			{
				string saveSpecies = p.Species.InternalName;
				if (p.IsMegaEvolved && p.PreMegaSpecies != null)
				{
					saveSpecies = p.PreMegaSpecies.InternalName;
				}
				else if (p.Species.IsMega && !string.IsNullOrEmpty(p.Species.BaseSpeciesName))
				{
					saveSpecies = p.Species.BaseSpeciesName;
				}

				var dto = new PokemonSaveDto
				{
					Species = saveSpecies,
					Nickname = p.Nickname,
					Level = p.Level,
					CurrentHP = p.CurrentHP,
					CurrentExp = p.CurrentExp,
					Nature = p.Nature.ToString(),
					IV_HP = p.IV_HP,
					IV_Attack = p.IV_Attack,
					IV_Defense = p.IV_Defense,
					IV_SpAttack = p.IV_SpAttack,
					IV_SpDefense = p.IV_SpDefense,
					IV_Speed = p.IV_Speed,
					EV_HP = p.EV_HP,
					EV_Attack = p.EV_Attack,
					EV_Defense = p.EV_Defense,
					EV_SpAttack = p.EV_SpAttack,
					EV_SpDefense = p.EV_SpDefense,
					EV_Speed = p.EV_Speed,
					IsEgg = p.IsEgg,
					EggSteps = p.EggSteps,
					IsShiny = p.IsShiny,
					Gender = p.Gender ?? "♂",
					Status = p.Status.ToString(),
					UsedMegaStoneId = p.UsedMegaStoneId,
					HeldItem = p.HeldItem,
					Ability = p.Ability
				};
				foreach (var m in p.Moves) dto.Moves.Add(m.Name);
				list.Add(dto);
			}
			string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(GetSaveFilePath(), json);
		}
		catch { }
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
			string path = GetSaveFilePath();
			if (File.Exists(path))
			{
				string json = File.ReadAllText(path);
				var list = JsonSerializer.Deserialize<List<PokemonSaveDto>>(json);
				if (list != null && list.Count > 0)
				{
					Team.Clear();
					foreach (var dto in list)
					{
						var species = PokemonSpecies.Get(dto.Species);
						if (species == null) continue;
						if (species.IsMega && !string.IsNullOrEmpty(species.BaseSpeciesName))
						{
							var baseSpecies = PokemonSpecies.Get(species.BaseSpeciesName);
							if (baseSpecies != null) species = baseSpecies;
						}
						var inst = new PokemonInstance(species, dto.Level, dto.Moves?.ToArray());
						inst.Nickname = string.IsNullOrWhiteSpace(dto.Nickname) ? species.Name : dto.Nickname;
						if (!string.IsNullOrEmpty(dto.Nature) && Enum.TryParse<PokemonNature>(dto.Nature, out var nat))
						{
							inst.Nature = nat;
						}
						inst.IV_HP = dto.IV_HP;
						inst.IV_Attack = dto.IV_Attack;
						inst.IV_Defense = dto.IV_Defense;
						inst.IV_SpAttack = dto.IV_SpAttack;
						inst.IV_SpDefense = dto.IV_SpDefense;
						inst.IV_Speed = dto.IV_Speed;
						inst.EV_HP = dto.EV_HP;
						inst.EV_Attack = dto.EV_Attack;
						inst.EV_Defense = dto.EV_Defense;
						inst.EV_SpAttack = dto.EV_SpAttack;
						inst.EV_SpDefense = dto.EV_SpDefense;
						inst.EV_Speed = dto.EV_Speed;
						inst.IsEgg = dto.IsEgg;
						inst.EggSteps = dto.EggSteps;
						inst.IsShiny = dto.IsShiny;
						inst.UsedMegaStoneId = dto.UsedMegaStoneId;
						inst.HeldItem = dto.HeldItem;
						if (!string.IsNullOrEmpty(dto.Ability)) inst.Ability = dto.Ability;
						if (!string.IsNullOrEmpty(dto.Gender)) inst.Gender = dto.Gender;
						if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<PokemonStatusCondition>(dto.Status, out var st))
							inst.Status = st;
						inst.RecalculateStats();
						inst.CurrentHP = Math.Clamp(dto.CurrentHP, 0, inst.MaxHP);
						inst.CurrentExp = dto.CurrentExp;
						Team.Add(inst);
					}
					return;
				}
			}
		}
		catch { }
	}
}
