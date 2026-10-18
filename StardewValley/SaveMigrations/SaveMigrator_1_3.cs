using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Objects;

namespace StardewValley.SaveMigrations;

/// <summary>Migrates existing save files for compatibility with Stardew Valley 1.3.</summary>
public class SaveMigrator_1_3 : ISaveMigrator
{
	/// <inheritdoc />
	public Version GameVersion { get; } = new Version(1, 3);

	/// <inheritdoc />
	public bool ApplySaveFix(SaveFixes saveFix)
	{
		return false;
	}

	/// <summary>Apply one-time save migrations which predate <see cref="T:StardewValley.SaveMigrations.SaveFixes" />.</summary>
	public static void ApplyLegacyChanges()
	{
		if (!Game1.IsMasterGame)
		{
			return;
		}
		FarmHouse farmHouse = Game1.RequireLocation<FarmHouse>("FarmHouse");
		farmHouse.furniture.Add(new Furniture("1792", Utility.PointToVector2(farmHouse.getFireplacePoint())));
		GameLocation gameLocation = Game1.RequireLocation("Town");
		if (!Game1.MasterPlayer.mailReceived.Contains("JojaMember") && gameLocation.CanItemBePlacedHere(new Vector2(57f, 16f)))
		{
			gameLocation.objects.Add(new Vector2(57f, 16f), ItemRegistry.Create<Object>("(BC)55"));
		}
		MarkFloorChestAsCollectedIfNecessary(10);
		MarkFloorChestAsCollectedIfNecessary(20);
		MarkFloorChestAsCollectedIfNecessary(40);
		MarkFloorChestAsCollectedIfNecessary(50);
		MarkFloorChestAsCollectedIfNecessary(60);
		MarkFloorChestAsCollectedIfNecessary(70);
		MarkFloorChestAsCollectedIfNecessary(80);
		MarkFloorChestAsCollectedIfNecessary(90);
		MarkFloorChestAsCollectedIfNecessary(100);
		Utility.ForEachVillager(delegate(NPC villager)
		{
			if (villager.datingFarmer == true)
			{
				if (Game1.player.friendshipData.TryGetValue(villager.Name, out var value) && !value.IsDating())
				{
					value.Status = FriendshipStatus.Dating;
				}
				villager.datingFarmer = null;
			}
			if (villager.divorcedFromFarmer == true)
			{
				if (Game1.player.friendshipData.TryGetValue(villager.Name, out var value2) && !value2.IsDating() && !value2.IsDivorced())
				{
					value2.Status = FriendshipStatus.Divorced;
				}
				villager.divorcedFromFarmer = null;
			}
			return true;
		});
		MigrateHorseIds();
		Game1.hasApplied1_3_UpdateChanges = true;
	}

	/// <summary>Mark a mine floor chest as collected if needed.</summary>
	/// <param name="floorNumber">The mine level.</param>
	/// <remarks>This should only be used on pre-1.3 saves, because the addition of multiplayer means it's not safe to assume that the local player is the one who opened the chest.</remarks>
	public static void MarkFloorChestAsCollectedIfNecessary(int floorNumber)
	{
		if (MineShaft.permanentMineChanges != null && MineShaft.permanentMineChanges.TryGetValue(floorNumber, out var value) && value.chestsLeft <= 0)
		{
			Game1.player.chestConsumedMineLevels[floorNumber] = true;
		}
	}

	/// <summary>Migrate the obsolete <see cref="F:StardewValley.Farmer.obsolete_friendships" /> into the new <see cref="F:StardewValley.Farmer.friendshipData" /> field, if applicable.</summary>
	/// <param name="player">The player whose data to migrate.</param>
	public static void MigrateFriendshipData(Farmer player)
	{
		if (player.obsolete_friendships != null && player.friendshipData.Length == 0)
		{
			foreach (KeyValuePair<string, int[]> obsolete_friendship in player.obsolete_friendships)
			{
				player.friendshipData[obsolete_friendship.Key] = new Friendship(obsolete_friendship.Value[0])
				{
					GiftsThisWeek = obsolete_friendship.Value[1],
					TalkedToToday = (obsolete_friendship.Value[2] != 0),
					GiftsToday = obsolete_friendship.Value[3],
					ProposalRejected = (obsolete_friendship.Value[4] != 0)
				};
			}
			player.obsolete_friendships = null;
		}
		if (string.IsNullOrEmpty(player.spouse))
		{
			return;
		}
		bool flag = player.spouse.Contains("engaged");
		string text = player.spouse.Replace("engaged", "");
		Friendship friendship = player.friendshipData[text];
		if (friendship.Status == FriendshipStatus.Friendly || friendship.Status == FriendshipStatus.Dating || flag)
		{
			friendship.Status = (flag ? FriendshipStatus.Engaged : FriendshipStatus.Married);
			player.spouse = text;
			if (!flag)
			{
				friendship.WeddingDate = WorldDate.Now();
				friendship.WeddingDate.TotalDays -= player.obsolete_daysMarried.GetValueOrDefault();
				player.obsolete_daysMarried = null;
			}
		}
	}

	/// <summary>Fix the <see cref="P:StardewValley.Characters.Horse.HorseId" /> value for pre-1.3 horses.</summary>
	private static void MigrateHorseIds()
	{
		List<Stable> stablesMissingHorses = new List<Stable>();
		Utility.ForEachBuilding(delegate(Stable stable3)
		{
			if (stable3.getStableHorse() == null && stable3.GetParentLocation() != null)
			{
				stablesMissingHorses.Add(stable3);
			}
			return true;
		});
		for (int num = stablesMissingHorses.Count - 1; num >= 0; num--)
		{
			Stable stable = stablesMissingHorses[num];
			GameLocation parentLocation = stable.GetParentLocation();
			Rectangle boundingBox = stable.GetBoundingBox();
			foreach (NPC character in parentLocation.characters)
			{
				if (character is Horse horse && horse.HorseId == Guid.Empty && boundingBox.Intersects(horse.GetBoundingBox()))
				{
					horse.HorseId = stable.HorseId;
					stablesMissingHorses.RemoveAt(num);
					break;
				}
			}
		}
		for (int num2 = stablesMissingHorses.Count - 1; num2 >= 0; num2--)
		{
			Stable stable2 = stablesMissingHorses[num2];
			foreach (NPC character2 in stable2.GetParentLocation().characters)
			{
				if (character2 is Horse horse2 && horse2.HorseId == Guid.Empty)
				{
					horse2.HorseId = stable2.HorseId;
					stablesMissingHorses.RemoveAt(num2);
					break;
				}
			}
		}
		foreach (Stable item in stablesMissingHorses)
		{
			item.grabHorse();
		}
	}
}
