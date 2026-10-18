using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace StardewValley.Pokemon;

/// <summary>
/// Data Transfer Object for serialized storage box.
/// </summary>
public class PokemonBoxSaveDto
{
	public string Name { get; set; } = "Box";
	public int WallpaperIndex { get; set; } = 0;
	public List<PokemonSaveDto> Slots { get; set; } = new();
}

public class PokemonStorageSaveDto
{
	public int CurrentBoxIndex { get; set; } = 0;
	public List<PokemonBoxSaveDto> Boxes { get; set; } = new();
}

/// <summary>
/// Manages the PC Pokémon Storage System with 16 Boxes of 30 slots each (480 slots total).
/// </summary>
public class PokemonStorage
{
	public const int TotalBoxes = 16;
	public const int SlotsPerBox = 30; // 5 rows x 6 columns
	public const int BoxRows = 5;
	public const int BoxCols = 6;

	private static PokemonStorage _instance;
	public static PokemonStorage Instance => _instance ??= new PokemonStorage();

	public class Box
	{
		public string Name { get; set; }
		public int WallpaperIndex { get; set; }
		public PokemonInstance[] Slots { get; } = new PokemonInstance[SlotsPerBox];

		public Box(string name, int wallpaper = 0)
		{
			Name = name;
			WallpaperIndex = wallpaper;
		}

		public int FirstEmptySlot()
		{
			for (int i = 0; i < SlotsPerBox; i++)
			{
				if (Slots[i] == null) return i;
			}
			return -1;
		}

		public bool IsFull => FirstEmptySlot() == -1;

		public int StoredCount
		{
			get
			{
				int count = 0;
				for (int i = 0; i < SlotsPerBox; i++)
				{
					if (Slots[i] != null) count++;
				}
				return count;
			}
		}
	}

	public List<Box> Boxes { get; } = new();
	public int CurrentBoxIndex { get; set; } = 0;
	public int LastDepositedBox { get; private set; } = 0;

	private PokemonStorage()
	{
		for (int i = 0; i < TotalBoxes; i++)
		{
			Boxes.Add(new Box($"Caixa {i + 1}", i % 8));
		}
		LoadStorage();
	}

	public Box CurrentBox
	{
		get
		{
			if (CurrentBoxIndex < 0 || CurrentBoxIndex >= Boxes.Count)
				CurrentBoxIndex = 0;
			return Boxes[CurrentBoxIndex];
		}
	}

	/// <summary>
	/// Deposits a Pokémon into the PC. If targetBox is -1, uses current box or next available box.
	/// </summary>
	public bool Deposit(PokemonInstance mon, int targetBox = -1)
	{
		if (mon == null) return false;

		// 1. Try specified box or current box
		int startBox = (targetBox >= 0 && targetBox < TotalBoxes) ? targetBox : CurrentBoxIndex;
		int slot = Boxes[startBox].FirstEmptySlot();
		if (slot != -1)
		{
			Boxes[startBox].Slots[slot] = mon;
			LastDepositedBox = startBox;
			SaveStorage();
			return true;
		}

		// 2. Search for any box with space
		for (int b = 0; b < TotalBoxes; b++)
		{
			slot = Boxes[b].FirstEmptySlot();
			if (slot != -1)
			{
				Boxes[b].Slots[slot] = mon;
				LastDepositedBox = b;
				SaveStorage();
				return true;
			}
		}

		return false; // Storage is completely full (480 Pokemon!)
	}

	/// <summary>
	/// Withdraws a Pokémon from the specified box slot into the player's active team.
	/// </summary>
	public bool Withdraw(int boxIndex, int slotIndex)
	{
		if (boxIndex < 0 || boxIndex >= TotalBoxes) return false;
		if (slotIndex < 0 || slotIndex >= SlotsPerBox) return false;

		var mon = Boxes[boxIndex].Slots[slotIndex];
		if (mon == null) return false;

		if (PlayerParty.Instance.Team.Count >= 6)
		{
			Game1.playSound("cancel");
			Game1.showRedMessage("Sua equipe já está cheia (máx 6 Pokémon)!");
			return false;
		}

		Boxes[boxIndex].Slots[slotIndex] = null;
		PlayerParty.Instance.Team.Add(mon);
		PlayerParty.Instance.SaveParty();
		SaveStorage();
		Game1.playSound("coin");
		Game1.addHUDMessage(new HUDMessage($"{mon.Nickname} foi adicionado à sua equipe!", 2));
		return true;
	}

	/// <summary>
	/// Swaps a box slot with a player's team slot.
	/// </summary>
	public bool SwapWithParty(int boxIndex, int slotIndex, int partyIndex)
	{
		if (boxIndex < 0 || boxIndex >= TotalBoxes) return false;
		if (slotIndex < 0 || slotIndex >= SlotsPerBox) return false;
		var party = PlayerParty.Instance.Team;
		if (partyIndex < 0 || partyIndex >= party.Count) return false;

		var boxMon = Boxes[boxIndex].Slots[slotIndex];
		var partyMon = party[partyIndex];

		if (boxMon == null && party.Count <= 1)
		{
			Game1.playSound("cancel");
			Game1.showRedMessage("Você não pode depositar seu único Pokémon!");
			return false;
		}

		if (boxMon == null)
		{
			// Deposit only
			Boxes[boxIndex].Slots[slotIndex] = partyMon;
			party.RemoveAt(partyIndex);
			if (PlayerParty.Instance.ActiveIndex >= party.Count)
				PlayerParty.Instance.ActiveIndex = Math.Max(0, party.Count - 1);
		}
		else
		{
			// Swap
			Boxes[boxIndex].Slots[slotIndex] = partyMon;
			party[partyIndex] = boxMon;
		}

		PlayerParty.Instance.SaveParty();
		SaveStorage();
		Game1.playSound("coin");
		return true;
	}

	/// <summary>
	/// Moves or swaps a Pokémon between two box slots.
	/// </summary>
	public void MoveWithinStorage(int fromBox, int fromSlot, int toBox, int toSlot)
	{
		if (fromBox < 0 || fromBox >= TotalBoxes || toBox < 0 || toBox >= TotalBoxes) return;
		if (fromSlot < 0 || fromSlot >= SlotsPerBox || toSlot < 0 || toSlot >= SlotsPerBox) return;

		var temp = Boxes[toBox].Slots[toSlot];
		Boxes[toBox].Slots[toSlot] = Boxes[fromBox].Slots[fromSlot];
		Boxes[fromBox].Slots[fromSlot] = temp;
		SaveStorage();
		Game1.playSound("smallSelect");
	}

	/// <summary>
	/// Releases a Pokémon from storage into the wild.
	/// </summary>
	public bool Release(int boxIndex, int slotIndex)
	{
		if (boxIndex < 0 || boxIndex >= TotalBoxes || slotIndex < 0 || slotIndex >= SlotsPerBox) return false;
		var mon = Boxes[boxIndex].Slots[slotIndex];
		if (mon == null) return false;

		Boxes[boxIndex].Slots[slotIndex] = null;
		SaveStorage();
		Game1.playSound("trashcan");
		Game1.addHUDMessage(new HUDMessage($"Adeus, {mon.Nickname}! Você o libertou na natureza.", 2));
		return true;
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
				return Path.Combine(saveDir, "pokemon_storage_data.json");
			}
			string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");
			if (Directory.Exists(appData))
			{
				return Path.Combine(appData, "pokemon_storage_data.json");
			}
		}
		catch { }
		return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokemon_storage_data.json");
	}

	public void ResetForNewGame()
	{
		Boxes.Clear();
		for (int i = 0; i < TotalBoxes; i++)
		{
			Boxes.Add(new Box($"Caixa {i + 1}", i % 8));
		}
		CurrentBoxIndex = 0;
	}

	public void Reload()
	{
		ResetForNewGame();
		LoadStorage();
	}

	public void SaveStorage()
	{
		try
		{
			var root = new PokemonStorageSaveDto
			{
				CurrentBoxIndex = CurrentBoxIndex
			};

			for (int b = 0; b < TotalBoxes; b++)
			{
				var boxDto = new PokemonBoxSaveDto
				{
					Name = Boxes[b].Name,
					WallpaperIndex = Boxes[b].WallpaperIndex
				};

				for (int s = 0; s < SlotsPerBox; s++)
				{
					var mon = Boxes[b].Slots[s];
					if (mon == null)
					{
						boxDto.Slots.Add(null);
					}
					else
					{
						var dto = new PokemonSaveDto
						{
							Species = mon.Species.InternalName,
							Nickname = mon.Nickname,
							Level = mon.Level,
							CurrentHP = mon.CurrentHP,
							CurrentExp = mon.CurrentExp,
							Nature = mon.Nature.ToString(),
							IV_HP = mon.IV_HP,
							IV_Attack = mon.IV_Attack,
							IV_Defense = mon.IV_Defense,
							IV_SpAttack = mon.IV_SpAttack,
							IV_SpDefense = mon.IV_SpDefense,
							IV_Speed = mon.IV_Speed,
							EV_HP = mon.EV_HP,
							EV_Attack = mon.EV_Attack,
							EV_Defense = mon.EV_Defense,
							EV_SpAttack = mon.EV_SpAttack,
							EV_SpDefense = mon.EV_SpDefense,
							EV_Speed = mon.EV_Speed,
							IsEgg = mon.IsEgg,
							EggSteps = mon.EggSteps,
							IsShiny = mon.IsShiny,
							Gender = mon.Gender ?? "♂",
							Status = mon.Status.ToString(),
							UsedMegaStoneId = mon.UsedMegaStoneId,
							HeldItem = mon.HeldItem,
							Ability = mon.Ability
						};
						foreach (var m in mon.Moves) dto.Moves.Add(m.Name);
						boxDto.Slots.Add(dto);
					}
				}
				root.Boxes.Add(boxDto);
			}

			string json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(GetSaveFilePath(), json);
		}
		catch { }
	}

	public void LoadStorage()
	{
		try
		{
			string path = GetSaveFilePath();
			if (!File.Exists(path)) return;

			string json = File.ReadAllText(path);
			var root = JsonSerializer.Deserialize<PokemonStorageSaveDto>(json);
			if (root == null || root.Boxes == null) return;

			CurrentBoxIndex = Math.Clamp(root.CurrentBoxIndex, 0, TotalBoxes - 1);
			for (int b = 0; b < Math.Min(root.Boxes.Count, TotalBoxes); b++)
			{
				var boxDto = root.Boxes[b];
				if (boxDto == null) continue;
				if (!string.IsNullOrWhiteSpace(boxDto.Name)) Boxes[b].Name = boxDto.Name;
				Boxes[b].WallpaperIndex = boxDto.WallpaperIndex;

				if (boxDto.Slots != null)
				{
					for (int s = 0; s < Math.Min(boxDto.Slots.Count, SlotsPerBox); s++)
					{
						var dto = boxDto.Slots[s];
						if (dto == null)
						{
							Boxes[b].Slots[s] = null;
						}
						else
						{
							var species = PokemonSpecies.Get(dto.Species);
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
							if (species.IsMega && !string.IsNullOrEmpty(species.BaseSpeciesName))
							{
								inst.IsMegaEvolved = true;
								inst.PreMegaSpecies = PokemonSpecies.Get(species.BaseSpeciesName);
							}
							if (!string.IsNullOrEmpty(dto.Gender)) inst.Gender = dto.Gender;
							if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<PokemonStatusCondition>(dto.Status, out var st))
								inst.Status = st;
							inst.RecalculateStats();
							inst.CurrentHP = Math.Clamp(dto.CurrentHP, 0, inst.MaxHP);
							inst.CurrentExp = dto.CurrentExp;
							Boxes[b].Slots[s] = inst;
						}
					}
				}
			}
		}
		catch { }
	}
}
