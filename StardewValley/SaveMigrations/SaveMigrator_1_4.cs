using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace StardewValley.SaveMigrations;

/// <summary>Migrates existing save files for compatibility with Stardew Valley 1.4.</summary>
public class SaveMigrator_1_4 : ISaveMigrator
{
	/// <inheritdoc />
	public Version GameVersion { get; } = new Version(1, 4);

	/// <inheritdoc />
	public bool ApplySaveFix(SaveFixes saveFix)
	{
		switch (saveFix)
		{
		case SaveFixes.AddTownBush:
			if (Game1.getLocationFromName("Town") is Town town)
			{
				Vector2 tileLocation = new Vector2(61f, 93f);
				if (town.getLargeTerrainFeatureAt((int)tileLocation.X, (int)tileLocation.Y) == null)
				{
					town.largeTerrainFeatures.Add(new Bush(tileLocation, 2, town));
				}
			}
			return true;
		case SaveFixes.StoredBigCraftablesStackFix:
			Utility.ForEachItem(delegate(Item item)
			{
				if (item is Object obj && obj.bigCraftable.Value && obj.Stack == 0)
				{
					obj.Stack = 1;
				}
				return true;
			});
			return true;
		case SaveFixes.PorchedCabinBushesFix:
			Utility.ForEachBuilding(delegate(Building building)
			{
				if (building.daysOfConstructionLeft.Value <= 0 && building.GetIndoors() is Cabin)
				{
					building.removeOverlappingBushes(Game1.getFarm());
				}
				return true;
			});
			return true;
		case SaveFixes.ChangeObeliskFootprintHeight:
			Utility.ForEachBuilding(delegate(Building building)
			{
				if (building.buildingType.Value.Contains("Obelisk"))
				{
					building.tilesHigh.Value = 2;
					building.tileY.Value++;
				}
				return true;
			});
			return true;
		case SaveFixes.CreateStorageDressers:
			Utility.ForEachItem(delegate(Item item)
			{
				if (item is Clothing)
				{
					item.Category = -100;
				}
				return true;
			});
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (location is DecoratableLocation)
				{
					List<Furniture> list = new List<Furniture>();
					for (int i = 0; i < location.furniture.Count; i++)
					{
						Furniture furniture = location.furniture[i];
						if (furniture.ItemId == "704" || furniture.ItemId == "709" || furniture.ItemId == "714" || furniture.ItemId == "719")
						{
							StorageFurniture item = new StorageFurniture(furniture.ItemId, furniture.TileLocation, furniture.currentRotation.Value);
							list.Add(item);
							location.furniture.RemoveAt(i);
							i--;
						}
					}
					foreach (Furniture item2 in list)
					{
						location.furniture.Add(item2);
					}
				}
				return true;
			});
			return true;
		case SaveFixes.InferPreserves:
		{
			string[] preserveItemIndices = new string[4] { "(O)350", "(O)348", "(O)344", "(O)342" };
			string[] suffixes = new string[3] { " Juice", " Wine", " Jelly" };
			Object.PreserveType[] suffixPreserveTypes = new Object.PreserveType[3]
			{
				Object.PreserveType.Juice,
				Object.PreserveType.Wine,
				Object.PreserveType.Jelly
			};
			string[] prefixes = new string[1] { "Pickled " };
			Object.PreserveType[] prefixPreserveTypes = new Object.PreserveType[1] { Object.PreserveType.Pickle };
			Utility.ForEachItem(delegate(Item item)
			{
				if (!(item is Object obj))
				{
					return true;
				}
				if (!Utility.IsNormalObjectAtParentSheetIndex(obj, obj.ItemId))
				{
					return true;
				}
				if (!preserveItemIndices.Contains(obj.QualifiedItemId))
				{
					return true;
				}
				if (!obj.preserve.Value.HasValue)
				{
					bool flag = false;
					for (int i = 0; i < suffixes.Length; i++)
					{
						string text = suffixes[i];
						if (obj.Name.EndsWith(text))
						{
							string text2 = obj.Name.Substring(0, obj.Name.Length - text.Length);
							string text3 = null;
							foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
							{
								if (allDatum.InternalName == text2)
								{
									text3 = allDatum.ItemId;
									break;
								}
							}
							if (text3 != null)
							{
								obj.preservedParentSheetIndex.Value = text3;
								obj.preserve.Value = suffixPreserveTypes[i];
								flag = true;
								break;
							}
						}
					}
					if (flag)
					{
						return true;
					}
					for (int j = 0; j < prefixes.Length; j++)
					{
						string text4 = prefixes[j];
						if (obj.Name.StartsWith(text4))
						{
							string text5 = obj.Name.Substring(text4.Length);
							string text6 = null;
							foreach (ParsedItemData allDatum2 in ItemRegistry.GetObjectTypeDefinition().GetAllData())
							{
								if (allDatum2.InternalName == text5)
								{
									text6 = allDatum2.ItemId;
									break;
								}
							}
							if (text6 != null)
							{
								obj.preservedParentSheetIndex.Value = text6;
								obj.preserve.Value = prefixPreserveTypes[j];
								break;
							}
						}
					}
				}
				return true;
			});
			return true;
		}
		case SaveFixes.TransferHatSkipHairFlag:
			Utility.ForEachItem(delegate(Item item)
			{
				if (item is Hat { skipHairDraw: not false } hat)
				{
					hat.hairDrawType.Set(0);
					hat.skipHairDraw = false;
				}
				return true;
			});
			return true;
		case SaveFixes.RevealSecretNoteItemTastes:
		{
			Dictionary<int, string> dictionary = DataLoader.SecretNotes(Game1.content);
			for (int num = 0; num < 21; num++)
			{
				if (dictionary.TryGetValue(num, out var value) && Game1.player.secretNotesSeen.Contains(num))
				{
					Utility.ParseGiftReveals(value);
				}
			}
			return true;
		}
		case SaveFixes.TransferHoneyTypeToPreserves:
			return true;
		case SaveFixes.TransferNoteBlockScale:
			Utility.ForEachItem(delegate(Item item)
			{
				if (item is Object obj && (obj.QualifiedItemId == "(O)363" || obj.QualifiedItemId == "(O)464"))
				{
					obj.preservedParentSheetIndex.Value = ((int)obj.scale.X).ToString();
				}
				return true;
			});
			return true;
		case SaveFixes.FixCropHarvestAmountsAndInferSeedIndex:
			return true;
		case SaveFixes.quarryMineBushes:
		{
			GameLocation gameLocation = Game1.RequireLocation("Mountain");
			gameLocation.largeTerrainFeatures.Add(new Bush(new Vector2(101f, 18f), 1, gameLocation));
			gameLocation.largeTerrainFeatures.Add(new Bush(new Vector2(104f, 21f), 0, gameLocation));
			gameLocation.largeTerrainFeatures.Add(new Bush(new Vector2(105f, 18f), 0, gameLocation));
			return true;
		}
		case SaveFixes.MissingQisChallenge:
			foreach (Farmer allFarmer in Game1.getAllFarmers())
			{
				if (allFarmer.mailReceived.Contains("skullCave") && !allFarmer.hasQuest("20") && !allFarmer.hasOrWillReceiveMail("QiChallengeComplete"))
				{
					allFarmer.addQuest("20");
				}
			}
			return true;
		default:
			return false;
		}
	}

	/// <summary>Apply one-time save migrations which predate <see cref="T:StardewValley.SaveMigrations.SaveFixes" />.</summary>
	public static void ApplyLegacyChanges()
	{
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			foreach (string key2 in allFarmer.friendshipData.Keys)
			{
				allFarmer.friendshipData[key2].Points = Math.Min(allFarmer.friendshipData[key2].Points, 3125);
			}
		}
		foreach (KeyValuePair<string, string> bundleDatum in Game1.netWorldState.Value.BundleData)
		{
			int key = Convert.ToInt32(bundleDatum.Key.Split('/')[1]);
			if (!Game1.netWorldState.Value.Bundles.ContainsKey(key))
			{
				Game1.netWorldState.Value.Bundles.Add(key, new NetArray<bool, NetBool>(ArgUtility.SplitBySpace(bundleDatum.Value.Split('/')[2]).Length));
			}
			if (!Game1.netWorldState.Value.BundleRewards.ContainsKey(key))
			{
				Game1.netWorldState.Value.BundleRewards.Add(key, new NetBool(value: false));
			}
		}
		foreach (Farmer allFarmer2 in Game1.getAllFarmers())
		{
			foreach (Item item in allFarmer2.Items)
			{
				if (item != null)
				{
					item.HasBeenInInventory = true;
				}
			}
		}
		RecalculateLostBookCount();
		Utility.iterateChestsAndStorage(delegate(Item item)
		{
			item.HasBeenInInventory = true;
		});
		Game1.hasApplied1_4_UpdateChanges = true;
	}

	/// <summary>Recalculate the number of lost books found.</summary>
	public static void RecalculateLostBookCount()
	{
		int num = 0;
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.archaeologyFound.TryGetValue("102", out var value) && value[0] > 0)
			{
				num = Math.Max(num, value[0]);
				allFarmer.mailForTomorrow.Add("lostBookFound%&NL&%");
			}
		}
		Game1.netWorldState.Value.LostBooksFound = num;
	}
}
