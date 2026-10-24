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

	private static readonly Dictionary<long, PokemonStorage> _instancesByFarmer = new();
	private static readonly object _syncLock = new();

	public static PokemonStorage Instance => GetForFarmer(Game1.player);

	public static PokemonStorage GetForFarmer(Farmer farmer)
	{
		long id = farmer?.UniqueMultiplayerID ?? 0L;
		return GetForFarmer(id);
	}

	public static PokemonStorage GetForFarmer(long farmerId)
	{
		lock (_syncLock)
		{
			if (!_instancesByFarmer.TryGetValue(farmerId, out var storage))
			{
				storage = new PokemonStorage(farmerId);
				_instancesByFarmer[farmerId] = storage;
			}
			return storage;
		}
	}

	public static void ClearAll()
	{
		lock (_syncLock)
		{
			_instancesByFarmer.Clear();
		}
	}

	public long FarmerId { get; }

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

	private PokemonStorage(long farmerId)
	{
		FarmerId = farmerId;
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

		if (!mon.IsEgg)
		{
			mon.EndBattleGimmicks(revertMega: true);
			mon.FullRestore();
		}

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
			if (!partyMon.IsEgg)
			{
				partyMon.EndBattleGimmicks(revertMega: true);
				partyMon.FullRestore();
			}
			Boxes[boxIndex].Slots[slotIndex] = partyMon;
			party.RemoveAt(partyIndex);
			if (PlayerParty.Instance.ActiveIndex >= party.Count)
				PlayerParty.Instance.ActiveIndex = Math.Max(0, party.Count - 1);
		}
		else
		{
			// Swap
			if (!partyMon.IsEgg)
			{
				partyMon.EndBattleGimmicks(revertMega: true);
				partyMon.FullRestore();
			}
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


	public void ResetForNewGame()
	{
		Boxes.Clear();
		for (int i = 0; i < TotalBoxes; i++)
		{
			Boxes.Add(new Box($"Caixa {i + 1}", i % 8));
		}
		CurrentBoxIndex = 0;
		ClearAll();
	}

	public void Reload()
	{
		ResetForNewGame();
		PokemonSaveStore.Reload();
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
					boxDto.Slots.Add(PokemonDtoMapper.ToDto(Boxes[b].Slots[s]));
				}
				root.Boxes.Add(boxDto);
			}

			PokemonSaveFile save = PokemonSaveStore.GetDataForFarmer(FarmerId);
			save.Storage = root;
			PokemonSaveStore.SaveForFarmer(FarmerId, PokemonSaveSection.Storage);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PokemonStorage] Falha ao salvar as caixas do jogador {FarmerId}: {ex.Message}");
		}
	}

	public void LoadStorage()
	{
		try
		{
			PokemonSaveFile save = PokemonSaveStore.GetDataForFarmer(FarmerId);
			PokemonStorageSaveDto root = save.Storage;
			if (root == null || root.Boxes == null) return;

			CurrentBoxIndex = Math.Clamp(root.CurrentBoxIndex, 0, TotalBoxes - 1);
			for (int b = 0; b < Math.Min(root.Boxes.Count, TotalBoxes); b++)
			{
				var boxDto = root.Boxes[b];
				if (boxDto == null) continue;
				if (!string.IsNullOrWhiteSpace(boxDto.Name)) Boxes[b].Name = boxDto.Name;
				Boxes[b].WallpaperIndex = boxDto.WallpaperIndex;

				if (boxDto.Slots == null) continue;
				for (int s = 0; s < Math.Min(boxDto.Slots.Count, SlotsPerBox); s++)
				{
					Boxes[b].Slots[s] = PokemonDtoMapper.FromDto(boxDto.Slots[s]);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PokemonStorage] Falha ao carregar as caixas do jogador {FarmerId}: {ex.Message}");
		}
	}
}