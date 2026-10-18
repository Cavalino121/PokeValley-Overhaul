using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Crops;
using StardewValley.GameData.Tools;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.SpecialOrders;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using StardewValley.Util;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley.SaveMigrations;

/// <summary>Migrates existing save files for compatibility with Stardew Valley 1.6.</summary>
public class SaveMigrator_1_6 : ISaveMigrator
{
	/// <summary>The pre-1.6 structure of <see cref="T:StardewValley.Quests.DescriptionElement" />.</summary>
	public class LegacyDescriptionElement
	{
		/// <summary>The translation key for the text to render.</summary>
		public string xmlKey;

		/// <summary>The values to substitute for placeholders like <c>{0}</c> in the translation text.</summary>
		public List<object> param;
	}

	/// <inheritdoc />
	public Version GameVersion { get; } = new Version(1, 5);

	/// <inheritdoc />
	public bool ApplySaveFix(SaveFixes saveFix)
	{
		switch (saveFix)
		{
		case SaveFixes.MigrateBuildingsToData:
			Utility.ForEachBuilding(delegate(Building building)
			{
				if (building is JunimoHut { obsolete_output: not null } junimoHut)
				{
					junimoHut.GetOutputChest().Items.AddRange(junimoHut.obsolete_output.Items);
					junimoHut.obsolete_output = null;
				}
				if (building.isUnderConstruction(ignoreUpgrades: false))
				{
					Game1.netWorldState.Value.MarkUnderConstruction("Robin", building);
					if (building.daysUntilUpgrade.Value > 0 && string.IsNullOrWhiteSpace(building.upgradeName.Value))
					{
						building.upgradeName.Value = InferBuildingUpgradingTo(building.buildingType.Value);
					}
				}
				return true;
			});
			return true;
		case SaveFixes.ModularizeFarmhouse:
			Game1.getFarm().AddDefaultBuildings();
			return true;
		case SaveFixes.ModularizePets:
		{
			foreach (Farmer allFarmer in Game1.getAllFarmers())
			{
				allFarmer.whichPetType = ((allFarmer.obsolete_catPerson ?? false) ? "Cat" : "Dog");
				allFarmer.obsolete_catPerson = null;
			}
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				for (int num7 = location.characters.Count - 1; num7 >= 0; num7--)
				{
					if (location.characters[num7] is Pet pet3)
					{
						string text3 = null;
						if (pet3.GetType() == typeof(Cat))
						{
							text3 = "Cat";
						}
						else if (pet3.GetType() == typeof(Dog))
						{
							text3 = "Dog";
						}
						if (text3 != null)
						{
							Pet pet4 = new Pet((int)(pet3.Position.X / 64f), (int)(pet3.Position.X / 64f), pet3.whichBreed.Value, text3)
							{
								Name = pet3.Name,
								displayName = pet3.displayName
							};
							if (pet3.currentLocation != null)
							{
								pet4.currentLocation = pet3.currentLocation;
							}
							pet4.friendshipTowardFarmer.Value = pet3.friendshipTowardFarmer.Value;
							pet4.grantedFriendshipForPet.Value = pet3.grantedFriendshipForPet.Value;
							pet4.lastPetDay.Clear();
							pet4.lastPetDay.CopyFrom(pet3.lastPetDay.Pairs);
							pet4.isSleepingOnFarmerBed.Value = pet3.isSleepingOnFarmerBed.Value;
							pet4.modData.CopyFrom(pet3.modData);
							location.characters[num7] = pet4;
						}
					}
				}
				return true;
			});
			Farm farm2 = Game1.getFarm();
			farm2.AddDefaultBuilding("Pet Bowl", farm2.GetStarterPetBowlLocation());
			PetBowl petBowl2 = farm2.getBuildingByType("Pet Bowl") as PetBowl;
			Pet pet2 = Game1.player.getPet();
			if (petBowl2 != null && pet2 != null)
			{
				petBowl2.AssignPet(pet2);
				pet2.setAtFarmPosition();
			}
			return true;
		}
		case SaveFixes.AddNpcRemovalFlags:
		{
			GameLocation locationFromName6 = Game1.getLocationFromName("WitchSwamp");
			if (locationFromName6 != null && locationFromName6.getCharacterFromName("Henchman") == null)
			{
				Game1.addMail("henchmanGone", noLetter: true, sendToEveryone: true);
			}
			locationFromName6 = Game1.getLocationFromName("SandyHouse");
			if (locationFromName6 != null && locationFromName6.getCharacterFromName("Bouncer") == null)
			{
				Game1.addMail("bouncerGone", noLetter: true, sendToEveryone: true);
			}
			return true;
		}
		case SaveFixes.MigrateFarmhands:
			return true;
		case SaveFixes.MigrateLitterItemData:
			Utility.ForEachItem(delegate(Item item)
			{
				switch (item.QualifiedItemId)
				{
				case "(O)2":
				case "(O)4":
				case "(O)6":
				case "(O)8":
				case "(O)10":
				case "(O)12":
				case "(O)14":
				case "(O)25":
				case "(O)75":
				case "(O)76":
				case "(O)77":
				case "(O)95":
				case "(O)290":
				case "(O)751":
				case "(O)764":
				case "(O)765":
				case "(O)816":
				case "(O)817":
				case "(O)818":
				case "(O)819":
				case "(O)843":
				case "(O)844":
				case "(O)849":
				case "(O)850":
				case "(O)32":
				case "(O)34":
				case "(O)36":
				case "(O)38":
				case "(O)40":
				case "(O)42":
				case "(O)44":
				case "(O)46":
				case "(O)48":
				case "(O)50":
				case "(O)52":
				case "(O)54":
				case "(O)56":
				case "(O)58":
				case "(O)343":
				case "(O)450":
				case "(O)668":
				case "(O)670":
				case "(O)760":
				case "(O)762":
				case "(O)845":
				case "(O)846":
				case "(O)847":
				case "(O)294":
				case "(O)295":
				case "(O)0":
				case "(O)313":
				case "(O)314":
				case "(O)315":
				case "(O)316":
				case "(O)317":
				case "(O)318":
				case "(O)319":
				case "(O)320":
				case "(O)321":
				case "(O)452":
				case "(O)674":
				case "(O)675":
				case "(O)676":
				case "(O)677":
				case "(O)678":
				case "(O)679":
				case "(O)750":
				case "(O)784":
				case "(O)785":
				case "(O)786":
				case "(O)792":
				case "(O)793":
				case "(O)794":
				case "(O)882":
				case "(O)883":
				case "(O)884":
					item.Category = -999;
					if (item is Object obj5)
					{
						obj5.Type = "Litter";
					}
					break;
				case "(O)372":
					item.Category = -4;
					if (item is Object obj4)
					{
						obj4.Type = "Fish";
					}
					break;
				}
				return true;
			});
			return true;
		case SaveFixes.MigrateHoneyItems:
			Utility.ForEachItem(delegate(Item item)
			{
				if (!(item is Object obj4) || obj4.QualifiedItemId != "(O)340")
				{
					return true;
				}
				obj4.preserve.Value = Object.PreserveType.Honey;
				if (obj4.preservedParentSheetIndex.Value == null || obj4.preservedParentSheetIndex.Value == "0")
				{
					string text3 = obj4.obsolete_honeyType;
					if (string.IsNullOrWhiteSpace(text3) && obj4.name.EndsWith(" Honey"))
					{
						text3 = obj4.name.Substring(0, obj4.name.Length - " Honey".Length).Replace(" ", "");
					}
					switch (text3)
					{
					case "Poppy":
						obj4.preservedParentSheetIndex.Value = "376";
						break;
					case "Tulip":
						obj4.preservedParentSheetIndex.Value = "591";
						break;
					case "SummerSpangle":
						obj4.preservedParentSheetIndex.Value = "593";
						break;
					case "FairyRose":
						obj4.preservedParentSheetIndex.Value = "595";
						break;
					case "BlueJazz":
						obj4.preservedParentSheetIndex.Value = "597";
						break;
					default:
						obj4.Name = "Wild Honey";
						obj4.preservedParentSheetIndex.Value = null;
						break;
					}
				}
				if (obj4.Name == "Honey" && obj4.preservedParentSheetIndex.Value == "-1")
				{
					obj4.Name = "Wild Honey";
				}
				obj4.obsolete_honeyType = null;
				return true;
			});
			return true;
		case SaveFixes.MigrateMachineLastOutputRule:
			Utility.ForEachItem(delegate(Item item)
			{
				if (item is Object machine)
				{
					InferMachineInputOutputFields(machine);
				}
				return true;
			});
			return true;
		case SaveFixes.StandardizeBundleFields:
			return true;
		case SaveFixes.MigrateAdventurerGoalFlags:
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary["Gil_Slime Charmer Ring"] = "Gil_Slimes";
			dictionary["Gil_Slime Charmer Ring"] = "Gil_Slimes";
			dictionary["Gil_Savage Ring"] = "Gil_Shadows";
			dictionary["Gil_Vampire Ring"] = "Gil_Bats";
			dictionary["Gil_Skeleton Mask"] = "Gil_Skeletons";
			dictionary["Gil_Insect Head"] = "Gil_Insects";
			dictionary["Gil_Hard Hat"] = "Gil_Duggy";
			dictionary["Gil_Burglar's Ring"] = "Gil_DustSpirits";
			dictionary["Gil_Crabshell Ring"] = "Gil_Crabs";
			dictionary["Gil_Arcane Hat"] = "Gil_Mummies";
			dictionary["Gil_Knight's Helmet"] = "Gil_Dinos";
			dictionary["Gil_Napalm Ring"] = "Gil_Serpents";
			dictionary["Gil_Telephone"] = "Gil_FlameSpirits";
			Dictionary<string, string> dictionary2 = dictionary;
			foreach (Farmer allFarmer2 in Game1.getAllFarmers())
			{
				NetStringHashSet[] array = new NetStringHashSet[2] { allFarmer2.mailReceived, allFarmer2.mailForTomorrow };
				foreach (NetStringHashSet netStringHashSet in array)
				{
					foreach (KeyValuePair<string, string> item in dictionary2)
					{
						if (netStringHashSet.Remove(item.Key))
						{
							netStringHashSet.Add(item.Value);
						}
					}
				}
				IList<string> mailbox = Game1.mailbox;
				for (int num3 = 0; num3 < mailbox.Count; num3++)
				{
					if (dictionary2.TryGetValue(mailbox[num3], out var value2))
					{
						mailbox[num3] = value2;
					}
				}
			}
			return true;
		}
		case SaveFixes.SetCropSeedId:
		{
			Dictionary<string, string> seedsByHarvestId = new Dictionary<string, string>();
			foreach (KeyValuePair<string, CropData> cropDatum in Game1.cropData)
			{
				string key = cropDatum.Key;
				string harvestItemId = cropDatum.Value.HarvestItemId;
				if (harvestItemId != null)
				{
					seedsByHarvestId.TryAdd(harvestItemId, key);
				}
			}
			Utility.ForEachCrop(delegate(Crop crop)
			{
				if (crop.netSeedIndex.Value == "-1")
				{
					crop.netSeedIndex.Value = null;
				}
				if (!string.IsNullOrWhiteSpace(crop.netSeedIndex.Value))
				{
					return true;
				}
				if (crop.isWildSeedCrop() || crop.forageCrop.Value)
				{
					return true;
				}
				if (crop.indexOfHarvest.Value != null && seedsByHarvestId.TryGetValue(crop.indexOfHarvest.Value, out var value9))
				{
					crop.netSeedIndex.Value = value9;
				}
				return true;
			});
			return true;
		}
		case SaveFixes.FixMineBoulderCollisions:
		{
			Mine mine = Game1.RequireLocation<Mine>("Mine");
			Vector2 boulderPosition = mine.GetBoulderPosition();
			if (mine.objects.TryGetValue(boulderPosition, out var value6) && value6.QualifiedItemId == "(BC)78" && value6.TileLocation == Vector2.Zero)
			{
				value6.TileLocation = boulderPosition;
			}
			return true;
		}
		case SaveFixes.MigratePetAndPetBowlIds:
		{
			Pet pet = Game1.player.getPet();
			if (pet != null)
			{
				pet.petId.Value = Guid.NewGuid();
				PetBowl petBowl = (PetBowl)Game1.getFarm().getBuildingByType("Pet Bowl");
				if (petBowl != null)
				{
					petBowl.AssignPet(pet);
					pet.setAtFarmPosition();
				}
			}
			return true;
		}
		case SaveFixes.MigrateHousePaint:
		{
			Farm farm3 = Game1.getFarm();
			if (farm3.housePaintColor.Value != null)
			{
				farm3.GetMainFarmHouse().netBuildingPaintColor.Value.CopyFrom(farm3.housePaintColor.Value);
				farm3.housePaintColor.Value = null;
			}
			return true;
		}
		case SaveFixes.MigrateItemIds:
			Utility.ForEachItem(delegate(Item item)
			{
				if (!(item is Boots boots))
				{
					if (!(item is MeleeWeapon meleeWeapon))
					{
						if (!(item is Fence fence))
						{
							if (!(item is Slingshot slingshot))
							{
								if (item is Torch && item.itemId.Value != item.ParentSheetIndex.ToString())
								{
									item.itemId.Value = null;
								}
							}
							else
							{
								slingshot.ItemId = null;
							}
						}
						else if (fence.obsolete_whichType.HasValue)
						{
							item.itemId.Value = null;
						}
					}
					else
					{
						meleeWeapon.appearance.Value = ((!string.IsNullOrWhiteSpace(meleeWeapon.appearance.Value) && meleeWeapon.appearance.Value != "-1") ? ItemRegistry.ManuallyQualifyItemId(meleeWeapon.appearance.Value, "(W)") : null);
					}
				}
				else if (boots.appliedBootSheetIndex.Value == "-1")
				{
					boots.appliedBootSheetIndex.Value = null;
				}
				_ = item.ItemId;
				return true;
			});
			foreach (Farmer allFarmer3 in Game1.getAllFarmers())
			{
				NetStringIntArrayDictionary fishCaught = allFarmer3.fishCaught;
				if (fishCaught != null)
				{
					KeyValuePair<string, int[]>[] array3 = fishCaught.Pairs.ToArray();
					for (int num2 = 0; num2 < array3.Length; num2++)
					{
						KeyValuePair<string, int[]> keyValuePair2 = array3[num2];
						fishCaught.Remove(keyValuePair2.Key);
						fishCaught[ItemRegistry.ManuallyQualifyItemId(keyValuePair2.Key, "(O)")] = keyValuePair2.Value;
					}
				}
				if (allFarmer3.toolBeingUpgraded.Value != null)
				{
					switch (allFarmer3.toolBeingUpgraded.Value.InitialParentTileIndex)
					{
					case 13:
						allFarmer3.toolBeingUpgraded.Value = ItemRegistry.Create<Tool>("(T)CopperTrashCan");
						break;
					case 14:
						allFarmer3.toolBeingUpgraded.Value = ItemRegistry.Create<Tool>("(T)SteelTrashCan");
						break;
					case 15:
						allFarmer3.toolBeingUpgraded.Value = ItemRegistry.Create<Tool>("(T)GoldTrashCan");
						break;
					case 16:
						allFarmer3.toolBeingUpgraded.Value = ItemRegistry.Create<Tool>("(T)IridiumTrashCan");
						break;
					}
				}
				if (!(allFarmer3.obsolete_isMale ?? allFarmer3.IsMale))
				{
					NetRef<Clothing>[] array4 = new NetRef<Clothing>[2] { allFarmer3.shirtItem, allFarmer3.pantsItem };
					foreach (NetRef<Clothing> netRef in array4)
					{
						Clothing value5 = netRef.Value;
						if (value5 == null)
						{
							continue;
						}
						if (value5.obsolete_indexInTileSheetFemale > -1)
						{
							int num4 = value5.obsolete_indexInTileSheetFemale.Value;
							if (value5.HasTypeId("(S)"))
							{
								num4 += 1000;
							}
							ItemMetadata metadata = ItemRegistry.GetMetadata(value5.TypeDefinitionId + num4);
							if (metadata.Exists())
							{
								Clothing clothing = (Clothing)metadata.CreateItemOrErrorItem();
								clothing.clothesColor.Value = value5.clothesColor.Value;
								clothing.modData.CopyFrom(value5.modData);
								netRef.Value = clothing;
							}
						}
						value5.obsolete_indexInTileSheetFemale = null;
					}
				}
				foreach (Quest item2 in allFarmer3.questLog)
				{
					if (!(item2 is CraftingQuest craftingQuest))
					{
						if (!(item2 is FishingQuest fishingQuest))
						{
							if (!(item2 is ItemDeliveryQuest itemDeliveryQuest))
							{
								if (!(item2 is ItemHarvestQuest itemHarvestQuest))
								{
									if (!(item2 is LostItemQuest lostItemQuest))
									{
										if (!(item2 is ResourceCollectionQuest resourceCollectionQuest))
										{
											if (item2 is SecretLostItemQuest secretLostItemQuest)
											{
												secretLostItemQuest.ItemId.Value = ItemRegistry.ManuallyQualifyItemId(secretLostItemQuest.ItemId.Value, "(O)");
											}
										}
										else
										{
											resourceCollectionQuest.ItemId.Value = ItemRegistry.ManuallyQualifyItemId(resourceCollectionQuest.ItemId.Value, "(O)");
										}
									}
									else
									{
										lostItemQuest.ItemId.Value = ItemRegistry.ManuallyQualifyItemId(lostItemQuest.ItemId.Value, "(O)");
									}
								}
								else
								{
									itemHarvestQuest.ItemId.Value = ItemRegistry.ManuallyQualifyItemId(itemHarvestQuest.ItemId.Value, "(O)");
								}
							}
							else
							{
								itemDeliveryQuest.ItemId.Value = ItemRegistry.ManuallyQualifyItemId(itemDeliveryQuest.ItemId.Value, "(O)");
								if (itemDeliveryQuest.dailyQuest.Value)
								{
									itemDeliveryQuest.moneyReward.Value = itemDeliveryQuest.GetGoldRewardPerItem(ItemRegistry.Create(itemDeliveryQuest.ItemId.Value));
								}
							}
						}
						else
						{
							fishingQuest.ItemId.Value = ItemRegistry.ManuallyQualifyItemId(fishingQuest.ItemId.Value, "(O)");
						}
					}
					else
					{
						craftingQuest.ItemId.Value = ItemRegistry.ManuallyQualifyItemId(craftingQuest.ItemId.Value, (craftingQuest.obsolete_isBigCraftable == true) ? "(BC)" : "(O)");
						craftingQuest.obsolete_isBigCraftable = null;
					}
				}
			}
			foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
			{
				if (specialOrder.itemToRemoveOnEnd.Value == "-1")
				{
					specialOrder.itemToRemoveOnEnd.Value = null;
				}
			}
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (location is IslandShrine islandShrine)
				{
					islandShrine.AddMissingPedestals();
				}
				foreach (KeyValuePair<Vector2, Object> pair in location.objects.Pairs)
				{
					if (pair.Value is Fence fence && fence.obsolete_whichType.HasValue)
					{
						fence.ItemId = null;
					}
				}
				foreach (TerrainFeature value11 in location.terrainFeatures.Values)
				{
					if (value11 is FruitTree fruitTree)
					{
						if (fruitTree.obsolete_treeType != null)
						{
							switch (fruitTree.obsolete_treeType)
							{
							case "0":
								fruitTree.treeId.Value = "628";
								break;
							case "1":
								fruitTree.treeId.Value = "629";
								break;
							case "2":
								fruitTree.treeId.Value = "630";
								break;
							case "3":
								fruitTree.treeId.Value = "631";
								break;
							case "4":
								fruitTree.treeId.Value = "632";
								break;
							case "5":
								fruitTree.treeId.Value = "633";
								break;
							case "7":
								fruitTree.treeId.Value = "69";
								break;
							case "8":
								fruitTree.treeId.Value = "835";
								break;
							default:
								fruitTree.treeId.Value = fruitTree.obsolete_treeType;
								break;
							}
							fruitTree.obsolete_treeType = null;
						}
						if (fruitTree.obsolete_fruitsOnTree.HasValue)
						{
							bool isGreenhouse = fruitTree.Location.IsGreenhouse;
							try
							{
								fruitTree.Location.IsGreenhouse = true;
								for (int l = 0; l < fruitTree.obsolete_fruitsOnTree; l++)
								{
									fruitTree.TryAddFruit();
								}
							}
							finally
							{
								fruitTree.Location.IsGreenhouse = isGreenhouse;
							}
							fruitTree.obsolete_fruitsOnTree = null;
						}
					}
				}
				foreach (Building building in location.buildings)
				{
					if (building is FishPond fishPond && fishPond.fishType.Value == "-1")
					{
						fishPond.fishType.Value = null;
					}
				}
				foreach (FarmAnimal value12 in location.animals.Values)
				{
					if (value12.currentProduce.Value == "-1")
					{
						value12.currentProduce.Value = null;
						value12.ReloadTextureIfNeeded();
					}
				}
				return true;
			});
			return true;
		case SaveFixes.MigrateShedFloorWallIds:
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (location is Shed shed)
				{
					if (shed.appliedFloor.TryGetValue("Floor_0", out var value9))
					{
						shed.appliedFloor.Remove("Floor_0");
						shed.appliedFloor["Floor"] = value9;
					}
					if (shed.appliedWallpaper.TryGetValue("Wall_0", out var value10))
					{
						shed.appliedWallpaper.Remove("Wall_0");
						shed.appliedWallpaper["Wall"] = value10;
					}
				}
				return true;
			});
			return true;
		case SaveFixes.RemoveMeatFromAnimalBundle:
		{
			if (Game1.netWorldState.Value.BundleData.TryGetValue("Pantry/4", out var value7) && value7.StartsWith("Animal/"))
			{
				string[] array5 = value7.Split('/');
				List<string> list2 = ArgUtility.SplitBySpace(ArgUtility.Get(value7.Split('/'), 2)).ToList();
				for (int num6 = 0; num6 < list2.Count; num6 += 3)
				{
					string text2 = list2[num6];
					switch (text2)
					{
					case "639":
					case "640":
					case "641":
					case "642":
					case "643":
						if (ItemRegistry.ResolveMetadata("(O)" + text2) == null)
						{
							list2.RemoveRange(num6, Math.Min(3, list2.Count - 1));
							num6 -= 3;
						}
						break;
					}
				}
				array5[2] = string.Join(" ", list2);
				Game1.netWorldState.Value.BundleData["Pantry/4"] = string.Join("/", array5);
				if (Game1.netWorldState.Value.Bundles.TryGetValue(4, out var value8) && value8.Length > list2.Count)
				{
					Array.Resize(ref value8, list2.Count);
					Game1.netWorldState.Value.Bundles.Remove(4);
					Game1.netWorldState.Value.Bundles.Add(4, value8);
				}
			}
			return true;
		}
		case SaveFixes.RemoveMasteryRoomFoliage:
		{
			GameLocation locationFromName2 = Game1.getLocationFromName("Forest");
			if (locationFromName2 != null)
			{
				locationFromName2.largeTerrainFeatures.RemoveWhere((LargeTerrainFeature feature) => feature.Tile == new Vector2(100f, 74f) || feature.Tile == new Vector2(101f, 76f));
				if (locationFromName2.terrainFeatures.GetValueOrDefault(new Vector2(98f, 75f)) is Tree tree && tree.tapped.Value && locationFromName2.objects.TryGetValue(new Vector2(98f, 75f), out var value))
				{
					if (value.readyForHarvest.Value && value.heldObject != null)
					{
						Game1.player.team.returnedDonations.Add(value.heldObject.Value);
					}
					Game1.player.team.returnedDonations.Add(value);
					Game1.player.team.newLostAndFoundItems.Value = true;
				}
				locationFromName2.terrainFeatures.Remove(new Vector2(98f, 75f));
			}
			return true;
		}
		case SaveFixes.AddTownTrees:
		{
			GameLocation locationFromName = Game1.getLocationFromName("Town");
			Layer layer = locationFromName.map?.GetLayer("Paths");
			if (layer == null)
			{
				return false;
			}
			for (int j = 0; j < locationFromName.map.Layers[0].LayerWidth; j++)
			{
				for (int k = 0; k < locationFromName.map.Layers[0].LayerHeight; k++)
				{
					Tile tile = layer.Tiles[j, k];
					if (tile == null)
					{
						continue;
					}
					Vector2 vector = new Vector2(j, k);
					if (locationFromName.TryGetTreeIdForTile(tile, out var treeId, out var growthStageOnLoad, out var _, out var isFruitTree) && locationFromName.GetFurnitureAt(vector) == null && !locationFromName.terrainFeatures.ContainsKey(vector) && !locationFromName.objects.ContainsKey(vector))
					{
						if (isFruitTree)
						{
							locationFromName.terrainFeatures.Add(vector, new FruitTree(treeId, growthStageOnLoad ?? 4));
						}
						else
						{
							locationFromName.terrainFeatures.Add(vector, new Tree(treeId, growthStageOnLoad ?? 5));
						}
					}
				}
			}
			return true;
		}
		case SaveFixes.MapAdjustments_1_6:
		{
			Game1.getLocationFromName("BusStop").shiftContents(10, 0);
			List<Point> obj = new List<Point>
			{
				new Point(78, 17),
				new Point(79, 17),
				new Point(79, 18),
				new Point(80, 17),
				new Point(80, 18),
				new Point(80, 19),
				new Point(81, 16),
				new Point(81, 17),
				new Point(81, 18),
				new Point(81, 19),
				new Point(82, 15),
				new Point(82, 16),
				new Point(82, 17),
				new Point(82, 18),
				new Point(83, 13),
				new Point(83, 14),
				new Point(83, 15),
				new Point(83, 16),
				new Point(83, 17),
				new Point(84, 13),
				new Point(84, 14),
				new Point(84, 15),
				new Point(84, 16),
				new Point(84, 17),
				new Point(84, 18),
				new Point(85, 13),
				new Point(85, 14),
				new Point(85, 15),
				new Point(85, 16),
				new Point(85, 17),
				new Point(85, 18),
				new Point(86, 14),
				new Point(86, 15),
				new Point(86, 16),
				new Point(86, 17),
				new Point(86, 18),
				new Point(87, 14),
				new Point(87, 15),
				new Point(87, 16),
				new Point(87, 17),
				new Point(87, 18),
				new Point(87, 19),
				new Point(88, 13),
				new Point(88, 14),
				new Point(88, 15),
				new Point(88, 16),
				new Point(88, 17),
				new Point(88, 18),
				new Point(88, 19),
				new Point(89, 13),
				new Point(89, 14),
				new Point(89, 15),
				new Point(89, 16),
				new Point(89, 17),
				new Point(79, 21),
				new Point(79, 22),
				new Point(79, 23),
				new Point(79, 24),
				new Point(79, 25),
				new Point(76, 16),
				new Point(75, 16),
				new Point(74, 16)
			};
			GameLocation locationFromName3 = Game1.getLocationFromName("Mountain");
			foreach (Point item3 in obj)
			{
				locationFromName3.cleanUpTileForMapOverride(item3);
			}
			locationFromName3.terrainFeatures.Remove(new Vector2(79f, 20f));
			locationFromName3.terrainFeatures.Remove(new Vector2(79f, 19f));
			locationFromName3.terrainFeatures.Remove(new Vector2(79f, 16f));
			locationFromName3.terrainFeatures.Remove(new Vector2(80f, 20f));
			locationFromName3.largeTerrainFeatures.Remove(locationFromName3.getLargeTerrainFeatureAt(82, 11));
			locationFromName3.largeTerrainFeatures.Remove(locationFromName3.getLargeTerrainFeatureAt(86, 13));
			locationFromName3.largeTerrainFeatures.Remove(locationFromName3.getLargeTerrainFeatureAt(85, 16));
			locationFromName3.largeTerrainFeatures.Add(new Bush(new Vector2(81f, 9f), 1, locationFromName3));
			locationFromName3.largeTerrainFeatures.Add(new Bush(new Vector2(84f, 18f), 2, locationFromName3));
			locationFromName3.largeTerrainFeatures.Add(new Bush(new Vector2(87f, 19f), 1, locationFromName3));
			List<Point> obj2 = new List<Point>
			{
				new Point(92, 10),
				new Point(93, 10),
				new Point(94, 10),
				new Point(93, 13),
				new Point(95, 13),
				new Point(92, 5),
				new Point(92, 6),
				new Point(97, 9),
				new Point(91, 10),
				new Point(91, 9),
				new Point(91, 8),
				new Point(93, 11),
				new Point(94, 11),
				new Point(95, 11)
			};
			GameLocation locationFromName4 = Game1.getLocationFromName("Town");
			foreach (Point item4 in obj2)
			{
				locationFromName4.cleanUpTileForMapOverride(item4);
			}
			locationFromName4.loadPathsLayerObjectsInArea(103, 16, 16, 27);
			locationFromName4.loadPathsLayerObjectsInArea(120, 57, 7, 12);
			locationFromName4.largeTerrainFeatures.Remove(locationFromName4.getLargeTerrainFeatureAt(105, 42));
			locationFromName4.largeTerrainFeatures.Remove(locationFromName4.getLargeTerrainFeatureAt(108, 42));
			List<Point> obj3 = new List<Point>
			{
				new Point(63, 77),
				new Point(63, 78),
				new Point(63, 79),
				new Point(63, 80),
				new Point(46, 26),
				new Point(46, 27),
				new Point(46, 28),
				new Point(46, 29)
			};
			GameLocation locationFromName5 = Game1.getLocationFromName("Forest");
			foreach (Point item5 in obj3)
			{
				locationFromName5.cleanUpTileForMapOverride(item5);
			}
			locationFromName5.largeTerrainFeatures.Add(new Bush(new Vector2(54f, 8f), 0, locationFromName5));
			locationFromName5.largeTerrainFeatures.Add(new Bush(new Vector2(58f, 8f), 0, locationFromName5));
			return true;
		}
		case SaveFixes.MigrateWalletItems:
		{
			Farmer masterPlayer2 = Game1.MasterPlayer;
			masterPlayer2.hasRustyKey = masterPlayer2.hasRustyKey || (masterPlayer2.obsolete_hasRustyKey ?? false);
			masterPlayer2.hasSkullKey = masterPlayer2.hasSkullKey || (masterPlayer2.obsolete_hasSkullKey ?? false);
			masterPlayer2.canUnderstandDwarves = masterPlayer2.canUnderstandDwarves || (masterPlayer2.obsolete_canUnderstandDwarves ?? false);
			masterPlayer2.obsolete_hasRustyKey = null;
			masterPlayer2.obsolete_hasSkullKey = null;
			masterPlayer2.obsolete_canUnderstandDwarves = null;
			foreach (Farmer allFarmer4 in Game1.getAllFarmers())
			{
				allFarmer4.hasClubCard = allFarmer4.hasClubCard || (allFarmer4.obsolete_hasClubCard ?? false);
				allFarmer4.hasDarkTalisman = allFarmer4.hasDarkTalisman || (allFarmer4.obsolete_hasDarkTalisman ?? false);
				allFarmer4.hasMagicInk = allFarmer4.hasMagicInk || (allFarmer4.obsolete_hasMagicInk ?? false);
				allFarmer4.hasMagnifyingGlass = allFarmer4.hasMagnifyingGlass || (allFarmer4.obsolete_hasMagnifyingGlass ?? false);
				allFarmer4.hasSpecialCharm = allFarmer4.hasSpecialCharm || (allFarmer4.obsolete_hasSpecialCharm ?? false);
				allFarmer4.HasTownKey = allFarmer4.HasTownKey || (allFarmer4.obsolete_hasTownKey ?? false);
				allFarmer4.hasUnlockedSkullDoor = allFarmer4.hasUnlockedSkullDoor || (allFarmer4.obsolete_hasUnlockedSkullDoor ?? false);
				allFarmer4.obsolete_hasClubCard = null;
				allFarmer4.obsolete_hasDarkTalisman = null;
				allFarmer4.obsolete_hasMagicInk = null;
				allFarmer4.obsolete_hasMagnifyingGlass = null;
				allFarmer4.obsolete_hasSpecialCharm = null;
				allFarmer4.obsolete_hasTownKey = null;
				allFarmer4.obsolete_hasUnlockedSkullDoor = null;
				allFarmer4.obsolete_daysMarried = null;
			}
			return true;
		}
		case SaveFixes.MigrateResourceClumps:
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (!(location is Forest forest))
				{
					if (location is Woods woods)
					{
						woods.DayUpdate(Game1.dayOfMonth);
					}
				}
				else if (forest.obsolete_log != null)
				{
					forest.resourceClumps.Add(forest.obsolete_log);
					forest.obsolete_log = null;
				}
				return true;
			}, includeInteriors: false);
			return true;
		case SaveFixes.MigrateFishingRodAttachmentSlots:
			Utility.ForEachItem(delegate(Item item)
			{
				if (item is FishingRod fishingRod)
				{
					ToolData toolData = fishingRod.GetToolData();
					if (toolData == null || toolData.AttachmentSlots < 0 || fishingRod.AttachmentSlotsCount <= toolData.AttachmentSlots)
					{
						return true;
					}
					INetSerializable parent = fishingRod.attachments.Parent;
					fishingRod.attachments.Parent = null;
					try
					{
						int num7 = fishingRod.AttachmentSlotsCount - 1;
						while (fishingRod.AttachmentSlotsCount > toolData.AttachmentSlots && num7 >= 0)
						{
							if (fishingRod.attachments.Count <= num7)
							{
								fishingRod.AttachmentSlotsCount--;
							}
							else if (fishingRod.attachments[num7] == null)
							{
								fishingRod.AttachmentSlotsCount--;
							}
							num7--;
						}
					}
					finally
					{
						fishingRod.attachments.Parent = parent;
					}
				}
				return true;
			});
			return true;
		case SaveFixes.MoveSlimeHutches:
		{
			Farm farm = Game1.getFarm();
			for (int num5 = farm.buildings.Count - 1; num5 >= 0; num5--)
			{
				if (farm.buildings[num5].buildingType.Value == "Slime Hutch")
				{
					farm.buildings[num5].tileX.Value += 2;
					farm.buildings[num5].tileY.Value += 2;
					farm.buildings[num5].ReloadBuildingData();
					farm.buildings[num5].updateInteriorWarps();
				}
			}
			return true;
		}
		case SaveFixes.AddLocationsVisited:
			foreach (Farmer allFarmer5 in Game1.getAllFarmers())
			{
				NetStringHashSet locationsVisited = allFarmer5.locationsVisited;
				Farmer masterPlayer = Game1.MasterPlayer;
				locationsVisited.AddRange(new string[30]
				{
					"Farm", "FarmHouse", "FarmCave", "Cellar", "Town", "JoshHouse", "HaleyHouse", "SamHouse", "Blacksmith", "ManorHouse",
					"SeedShop", "Saloon", "Trailer", "Hospital", "HarveyRoom", "ArchaeologyHouse", "JojaMart", "Beach", "ElliottHouse", "FishShop",
					"Mountain", "ScienceHouse", "SebastianRoom", "Tent", "Forest", "AnimalShop", "LeahHouse", "Backwoods", "BusStop", "Tunnel"
				});
				if (masterPlayer.mailReceived.Contains("ccPantry"))
				{
					locationsVisited.Add("Greenhouse");
				}
				if (Game1.isLocationAccessible("CommunityCenter"))
				{
					locationsVisited.Add("CommunityCenter");
				}
				if (allFarmer5.eventsSeen.Contains("100162"))
				{
					locationsVisited.Add("Mine");
				}
				if (masterPlayer.mailReceived.Contains("ccVault"))
				{
					locationsVisited.AddRange(new string[2] { "Desert", "SkullCave" });
				}
				if (allFarmer5.eventsSeen.Contains("67"))
				{
					locationsVisited.Add("SandyHouse");
				}
				if (masterPlayer.mailReceived.Contains("bouncerGone"))
				{
					locationsVisited.Add("Club");
				}
				if (Game1.isLocationAccessible("Railroad"))
				{
					locationsVisited.AddRange(new string[4]
					{
						"Railroad",
						"BathHouse_Entry",
						allFarmer5.IsMale ? "BathHouse_MensLocker" : "BathHouse_WomensLocker",
						"BathHouse_Pool"
					});
				}
				if (masterPlayer.mailReceived.Contains("Farm_Eternal"))
				{
					locationsVisited.Add("Summit");
				}
				if (masterPlayer.mailReceived.Contains("witchStatueGone"))
				{
					locationsVisited.AddRange(new string[2] { "WitchSwamp", "WitchWarpCave" });
				}
				if (masterPlayer.mailReceived.Contains("henchmanGone"))
				{
					locationsVisited.Add("WitchHut");
				}
				if (allFarmer5.mailReceived.Contains("beenToWoods"))
				{
					locationsVisited.Add("Woods");
				}
				if (Forest.isWizardHouseUnlocked())
				{
					locationsVisited.Add("WizardHouse");
					if (allFarmer5.getFriendshipHeartLevelForNPC("Wizard") >= 4)
					{
						locationsVisited.Add("WizardHouseBasement");
					}
				}
				if (allFarmer5.mailReceived.Add("guildMember"))
				{
					locationsVisited.Add("AdventureGuild");
				}
				if (allFarmer5.mailReceived.Contains("OpenedSewer"))
				{
					locationsVisited.Add("Sewer");
				}
				if (allFarmer5.mailReceived.Contains("krobusUnseal"))
				{
					locationsVisited.Add("BugLand");
				}
				if (masterPlayer.mailReceived.Contains("abandonedJojaMartAccessible"))
				{
					locationsVisited.Add("AbandonedJojaMart");
				}
				if (masterPlayer.mailReceived.Contains("ccMovieTheater"))
				{
					locationsVisited.Add("MovieTheater");
				}
				if (masterPlayer.mailReceived.Contains("pamHouseUpgrade"))
				{
					locationsVisited.Add("Trailer_Big");
				}
				if (allFarmer5.getFriendshipHeartLevelForNPC("Caroline") >= 2)
				{
					locationsVisited.Add("Sunroom");
				}
				if (Game1.year > 1 || (Game1.season == Season.Winter && Game1.dayOfMonth >= 15))
				{
					locationsVisited.AddRange(new string[3] { "BeachNightMarket", "MermaidHouse", "Submarine" });
				}
				if (allFarmer5.mailReceived.Contains("willyBackRoomInvitation"))
				{
					locationsVisited.Add("BoatTunnel");
				}
				if (allFarmer5.mailReceived.Contains("Visited_Island"))
				{
					locationsVisited.AddRange(new string[4] { "IslandSouth", "IslandEast", "IslandHut", "IslandShrine" });
					if (masterPlayer.mailReceived.Contains("Island_FirstParrot"))
					{
						locationsVisited.AddRange(new string[2] { "IslandNorth", "IslandFieldOffice" });
					}
					if (masterPlayer.mailReceived.Contains("islandNorthCaveOpened"))
					{
						locationsVisited.Add("IslandNorthCave1");
					}
					if (masterPlayer.mailReceived.Contains("reachedCaldera"))
					{
						locationsVisited.Add("Caldera");
					}
					if (masterPlayer.mailReceived.Contains("Island_Turtle"))
					{
						locationsVisited.AddRange(new string[2] { "IslandWest", "IslandWestCave1" });
					}
					if (masterPlayer.mailReceived.Contains("Island_UpgradeHouse"))
					{
						locationsVisited.AddRange(new string[2] { "IslandFarmHouse", "IslandFarmCave" });
					}
					if (masterPlayer.team.collectedNutTracker.Contains("Bush_CaptainRoom_2_4"))
					{
						locationsVisited.Add("CaptainRoom");
					}
					if (IslandWest.IsQiWalnutRoomDoorUnlocked(out var _))
					{
						locationsVisited.Add("QiNutRoom");
					}
					if (masterPlayer.mailReceived.Contains("Island_Resort"))
					{
						locationsVisited.AddRange(new string[2] { "IslandSouthEast", "IslandSouthEastCave" });
					}
				}
				if (masterPlayer.mailReceived.Contains("leoMoved"))
				{
					locationsVisited.Add("LeoTreeHouse");
				}
			}
			return true;
		case SaveFixes.MarkStarterGiftBoxes:
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (location is FarmHouse)
				{
					foreach (Object value13 in location.objects.Values)
					{
						if (value13 is Chest chest && chest.giftbox.Value && !chest.playerChest.Value)
						{
							chest.giftboxIsStarterGift.Value = true;
						}
					}
				}
				return true;
			});
			return true;
		case SaveFixes.MigrateMailEventsToTriggerActions:
		{
			Dictionary<string, string> dictionary3 = new Dictionary<string, string>
			{
				["2346097"] = "Mail_Abigail_8heart",
				["2346096"] = "Mail_Penny_10heart",
				["2346095"] = "Mail_Elliott_8heart",
				["2346094"] = "Mail_Elliott_10heart",
				["3333094"] = "Mail_Pierre_ExtendedHours",
				["2346093"] = "Mail_Harvey_10heart",
				["2346092"] = "Mail_Sam_10heart",
				["2346091"] = "Mail_Alex_10heart",
				["68"] = "Mail_Mom_5K",
				["69"] = "Mail_Mom_15K",
				["70"] = "Mail_Mom_32K",
				["71"] = "Mail_Mom_120K",
				["72"] = "Mail_Dad_5K",
				["73"] = "Mail_Dad_15K",
				["74"] = "Mail_Dad_32K",
				["75"] = "Mail_Dad_120K",
				["76"] = "Mail_Tribune_UpAndComing",
				["706"] = "Mail_Pierre_Fertilizers",
				["707"] = "Mail_Pierre_FertilizersHighQuality",
				["909"] = "Mail_Robin_Woodchipper",
				["3872126"] = "Mail_Willy_BackRoomUnlocked"
			};
			Dictionary<string, string> dictionary4 = new Dictionary<string, string>
			{
				["2111194"] = "Mail_Emily_8heart",
				["2111294"] = "Mail_Emily_10heart",
				["3912126"] = "Mail_Elliott_Tour1",
				["3912127"] = "Mail_Elliott_Tour2",
				["3912128"] = "Mail_Elliott_Tour3",
				["3912129"] = "Mail_Elliott_Tour4",
				["3912130"] = "Mail_Elliott_Tour5",
				["3912131"] = "Mail_Elliott_Tour6"
			};
			foreach (Farmer allFarmer6 in Game1.getAllFarmers())
			{
				NetStringHashSet eventsSeen = allFarmer6.eventsSeen;
				NetStringHashSet triggerActionsRun = allFarmer6.triggerActionsRun;
				foreach (KeyValuePair<string, string> item6 in dictionary3)
				{
					if (eventsSeen.Remove(item6.Key))
					{
						triggerActionsRun.Add(item6.Value);
					}
				}
				foreach (KeyValuePair<string, string> item7 in dictionary4)
				{
					if (eventsSeen.Contains(item7.Key))
					{
						triggerActionsRun.Add(item7.Value);
					}
				}
			}
			return true;
		}
		case SaveFixes.ShiftFarmHouseFurnitureForExpansion:
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				FarmHouse house = location as FarmHouse;
				if (house != null && house.upgradeLevel >= 2)
				{
					house.shiftContents(15, 10, delegate(Vector2 vector3, object entity)
					{
						if (entity is BedFurniture)
						{
							int xTile = (int)vector3.X;
							int yTile = (int)vector3.Y;
							if (house.doesTileHaveProperty(xTile, yTile, "DefaultBedPosition", "Back") == null)
							{
								return house.doesTileHaveProperty(xTile, yTile, "DefaultChildBedPosition", "Back") == null;
							}
							return false;
						}
						if (entity is Furniture { QualifiedItemId: "(F)1792" })
						{
							Vector2 vector4 = vector3 - Utility.PointToVector2(house.getFireplacePoint());
							if (!(Math.Abs(vector4.X) > 1E-05f))
							{
								return Math.Abs(vector4.Y) > 1E-05f;
							}
							return true;
						}
						return true;
					});
					foreach (NPC character in house.characters)
					{
						if (!character.TilePoint.Equals(house.getKitchenStandingSpot()))
						{
							character.Position += new Vector2(15f, 10f) * 64f;
						}
						if (house.hasTileAt(character.TilePoint, "Buildings") || !house.hasTileAt(character.TilePoint, "Back"))
						{
							Vector2 vector2 = Utility.recursiveFindOpenTileForCharacter(character, house, Utility.PointToVector2(house.getKitchenStandingSpot()), 99, allowOffMap: false);
							if (vector2 != Vector2.Zero)
							{
								character.setTileLocation(vector2);
							}
							else
							{
								character.setTileLocation(Utility.PointToVector2(house.getKitchenStandingSpot()));
							}
						}
					}
				}
				return true;
			});
			foreach (Farmer allFarmer7 in Game1.getAllFarmers())
			{
				if (allFarmer7.currentLocation is FarmHouse { upgradeLevel: >=2 })
				{
					allFarmer7.Position += new Vector2(15f, 10f) * 64f;
				}
			}
			return true;
		case SaveFixes.MigratePreservesTo16:
		{
			// Pre-1.6 preserves migration
			return true;
		}
		case SaveFixes.MigrateQuestDataTo16:
		{
			Lazy<XmlSerializer> serializer = new Lazy<XmlSerializer>(() => new XmlSerializer(typeof(LegacyDescriptionElement), new Type[3]
			{
				typeof(DescriptionElement),
				typeof(Character),
				typeof(Item)
			}));
			foreach (Farmer allFarmer8 in Game1.getAllFarmers())
			{
				foreach (Quest item8 in allFarmer8.questLog)
				{
					FieldInfo[] fields = item8.GetType().GetFields();
					foreach (FieldInfo fieldInfo in fields)
					{
						if (fieldInfo.FieldType == typeof(NetDescriptionElementList))
						{
							NetDescriptionElementList netDescriptionElementList = (NetDescriptionElementList)fieldInfo.GetValue(item8);
							if (netDescriptionElementList == null)
							{
								continue;
							}
							foreach (DescriptionElement item9 in netDescriptionElementList)
							{
								MigrateLegacyDescriptionElement(serializer, item9);
							}
						}
						else if (fieldInfo.FieldType == typeof(NetDescriptionElementRef))
						{
							MigrateLegacyDescriptionElement(serializer, ((NetDescriptionElementRef)fieldInfo.GetValue(item8))?.Value);
						}
					}
				}
			}
			return true;
		}
		case SaveFixes.SetBushesInPots:
			Utility.ForEachItem(delegate(Item item)
			{
				if (item is IndoorPot indoorPot && indoorPot.bush.Value != null)
				{
					indoorPot.bush.Value.inPot.Value = true;
				}
				return true;
			});
			return true;
		case SaveFixes.FixItemsNotMarkedAsInInventory:
			foreach (Farmer allFarmer9 in Game1.getAllFarmers())
			{
				foreach (Item equippedItem in allFarmer9.GetEquippedItems())
				{
					equippedItem.HasBeenInInventory = true;
				}
				foreach (Item item10 in allFarmer9.Items)
				{
					if (item10 != null)
					{
						item10.HasBeenInInventory = true;
					}
				}
			}
			return true;
		case SaveFixes.BetaFixesFor16:
			Utility.ForEachItem(delegate(Item item)
			{
				if (item is Boots || item is Clothing || item is Hat)
				{
					item.FixStackSize();
				}
				return true;
			});
			return true;
		case SaveFixes.FixBasicWines:
			Utility.ForEachItem(delegate(Item item)
			{
				if (item.ParentSheetIndex == 348 && item.QualifiedItemId.Equals("(O)348"))
				{
					item.ParentSheetIndex = 123;
				}
				return true;
			});
			return true;
		case SaveFixes.ResetForges_1_6:
			SaveMigrator_1_5.ResetForges();
			return true;
		case SaveFixes.RestoreAncientSeedRecipe_1_6:
			foreach (Farmer allFarmer10 in Game1.getAllFarmers())
			{
				if (allFarmer10.mailReceived.Contains("museumCollectedRewardO_499_1"))
				{
					allFarmer10.craftingRecipes.TryAdd("Ancient Seeds", 0);
				}
			}
			return true;
		case SaveFixes.FixInstancedInterior:
			Utility.ForEachBuilding(delegate(Building building)
			{
				if (building.GetIndoorsType() == IndoorsType.Instanced)
				{
					GameLocation indoors = building.GetIndoors();
					if (indoors.uniqueName.Value == null)
					{
						indoors.uniqueName.Value = (building.GetData()?.IndoorMap ?? indoors.Name) + GuidHelper.NewGuid();
					}
					if (indoors is AnimalHouse animalHouse)
					{
						animalHouse.animalsThatLiveHere.RemoveWhere((long id) => Utility.getAnimal(id)?.home != building);
					}
				}
				return true;
			});
			return true;
		case SaveFixes.FixNonInstancedInterior:
			Utility.ForEachBuilding(delegate(Building building)
			{
				if (building.GetIndoorsType() == IndoorsType.Global)
				{
					building.GetIndoors().uniqueName.Value = null;
				}
				return true;
			});
			return true;
		case SaveFixes.PopulateConstructedBuildings:
			Utility.ForEachBuilding(delegate(Building building)
			{
				if (!string.IsNullOrWhiteSpace(building.buildingType.Value))
				{
					if (!building.isUnderConstruction(ignoreUpgrades: false))
					{
						Game1.player.team.constructedBuildings.Add(building.buildingType.Value);
					}
					BuildingData data = building.GetData();
					while (!string.IsNullOrWhiteSpace(data?.BuildingToUpgrade))
					{
						Game1.player.team.constructedBuildings.Add(data.BuildingToUpgrade);
						Building.TryGetData(data.BuildingToUpgrade, out data);
					}
				}
				return true;
			}, ignoreUnderConstruction: false);
			return true;
		case SaveFixes.FixRacoonQuestCompletion:
			if (NetWorldState.checkAnywhereForWorldStateID("forestStumpFixed"))
			{
				Game1.player.removeQuest("134");
				foreach (Farmer offlineFarmhand in Game1.getOfflineFarmhands())
				{
					offlineFarmhand.removeQuest("134");
				}
			}
			return true;
		case SaveFixes.RestoreDwarvish:
			if (Game1.player.hasOrWillReceiveMail("museumCollectedRewardO_326_1"))
			{
				Game1.player.canUnderstandDwarves = true;
			}
			return true;
		case SaveFixes.FixTubOFlowers:
			Utility.ForEachItem(delegate(Item item)
			{
				if (item.QualifiedItemId == "(BC)109")
				{
					item.ItemId = "108";
					item.ResetParentSheetIndex();
					if (item is Object obj4 && (obj4.Location?.IsOutdoors ?? false))
					{
						Season season = obj4.Location.GetSeason();
						if (season == Season.Winter || season == Season.Fall)
						{
							item.ParentSheetIndex = 109;
						}
					}
				}
				return true;
			});
			return true;
		case SaveFixes.MigrateStatFields:
			foreach (Farmer allFarmer11 in Game1.getAllFarmers())
			{
				Stats stats = allFarmer11.stats;
				SerializableDictionary<string, uint> obsolete_stat_dictionary = stats.obsolete_stat_dictionary;
				if (obsolete_stat_dictionary != null && obsolete_stat_dictionary.Count > 0)
				{
					foreach (KeyValuePair<string, uint> item11 in stats.obsolete_stat_dictionary)
					{
						stats.Values[item11.Key] = (stats.Values.TryGetValue(item11.Key, out var value3) ? (value3 + item11.Value) : item11.Value);
					}
					stats.obsolete_stat_dictionary = null;
				}
				if (stats.Values.TryGetValue("walnutsFound", out var value4))
				{
					Game1.netWorldState.Value.GoldenWalnutsFound += (int)value4;
					stats.Values.Remove("walnutsFound");
				}
				KeyValuePair<string, uint>[] array2 = stats.Values.ToArray();
				for (int num2 = 0; num2 < array2.Length; num2++)
				{
					KeyValuePair<string, uint> keyValuePair = array2[num2];
					if (keyValuePair.Value == 0)
					{
						stats.Values.Remove(keyValuePair.Key);
					}
				}
				if (stats.AverageBedtime == 0)
				{
					stats.Set("averageBedtime", stats.obsolete_averageBedtime.GetValueOrDefault());
				}
				stats.obsolete_averageBedtime = null;
				stats.obsolete_beveragesMade = MergeStats("beveragesMade", stats.obsolete_beveragesMade);
				stats.obsolete_caveCarrotsFound = MergeStats("caveCarrotsFound", stats.obsolete_caveCarrotsFound);
				stats.obsolete_cheeseMade = MergeStats("cheeseMade", stats.obsolete_cheeseMade);
				stats.obsolete_chickenEggsLayed = MergeStats("chickenEggsLayed", stats.obsolete_chickenEggsLayed);
				stats.obsolete_copperFound = MergeStats("copperFound", stats.obsolete_copperFound);
				stats.obsolete_cowMilkProduced = MergeStats("cowMilkProduced", stats.obsolete_cowMilkProduced);
				stats.obsolete_cropsShipped = MergeStats("cropsShipped", stats.obsolete_cropsShipped);
				stats.obsolete_daysPlayed = MergeStats("daysPlayed", stats.obsolete_daysPlayed);
				stats.obsolete_diamondsFound = MergeStats("diamondsFound", stats.obsolete_diamondsFound);
				stats.obsolete_dirtHoed = MergeStats("dirtHoed", stats.obsolete_dirtHoed);
				stats.obsolete_duckEggsLayed = MergeStats("duckEggsLayed", stats.obsolete_duckEggsLayed);
				stats.obsolete_fishCaught = MergeStats("fishCaught", stats.obsolete_fishCaught);
				stats.obsolete_geodesCracked = MergeStats("geodesCracked", stats.obsolete_geodesCracked);
				stats.obsolete_giftsGiven = MergeStats("giftsGiven", stats.obsolete_giftsGiven);
				stats.obsolete_goatCheeseMade = MergeStats("goatCheeseMade", stats.obsolete_goatCheeseMade);
				stats.obsolete_goatMilkProduced = MergeStats("goatMilkProduced", stats.obsolete_goatMilkProduced);
				stats.obsolete_goldFound = MergeStats("goldFound", stats.obsolete_goldFound);
				stats.obsolete_goodFriends = MergeStats("goodFriends", stats.obsolete_goodFriends);
				stats.obsolete_individualMoneyEarned = MergeStats("individualMoneyEarned", stats.obsolete_individualMoneyEarned);
				stats.obsolete_iridiumFound = MergeStats("iridiumFound", stats.obsolete_iridiumFound);
				stats.obsolete_ironFound = MergeStats("ironFound", stats.obsolete_ironFound);
				stats.obsolete_itemsCooked = MergeStats("itemsCooked", stats.obsolete_itemsCooked);
				stats.obsolete_itemsCrafted = MergeStats("itemsCrafted", stats.obsolete_itemsCrafted);
				stats.obsolete_itemsForaged = MergeStats("itemsForaged", stats.obsolete_itemsForaged);
				stats.obsolete_itemsShipped = MergeStats("itemsShipped", stats.obsolete_itemsShipped);
				stats.obsolete_monstersKilled = MergeStats("monstersKilled", stats.obsolete_monstersKilled);
				stats.obsolete_mysticStonesCrushed = MergeStats("mysticStonesCrushed", stats.obsolete_mysticStonesCrushed);
				stats.obsolete_notesFound = MergeStats("notesFound", stats.obsolete_notesFound);
				stats.obsolete_otherPreciousGemsFound = MergeStats("otherPreciousGemsFound", stats.obsolete_otherPreciousGemsFound);
				stats.obsolete_piecesOfTrashRecycled = MergeStats("piecesOfTrashRecycled", stats.obsolete_piecesOfTrashRecycled);
				stats.obsolete_preservesMade = MergeStats("preservesMade", stats.obsolete_preservesMade);
				stats.obsolete_prismaticShardsFound = MergeStats("prismaticShardsFound", stats.obsolete_prismaticShardsFound);
				stats.obsolete_questsCompleted = MergeStats("questsCompleted", stats.obsolete_questsCompleted);
				stats.obsolete_rabbitWoolProduced = MergeStats("rabbitWoolProduced", stats.obsolete_rabbitWoolProduced);
				stats.obsolete_rocksCrushed = MergeStats("rocksCrushed", stats.obsolete_rocksCrushed);
				stats.obsolete_sheepWoolProduced = MergeStats("sheepWoolProduced", stats.obsolete_sheepWoolProduced);
				stats.obsolete_slimesKilled = MergeStats("slimesKilled", stats.obsolete_slimesKilled);
				stats.obsolete_stepsTaken = MergeStats("stepsTaken", stats.obsolete_stepsTaken);
				stats.obsolete_stoneGathered = MergeStats("stoneGathered", stats.obsolete_stoneGathered);
				stats.obsolete_stumpsChopped = MergeStats("stumpsChopped", stats.obsolete_stumpsChopped);
				stats.obsolete_timesFished = MergeStats("timesFished", stats.obsolete_timesFished);
				stats.obsolete_timesUnconscious = MergeStats("timesUnconscious", stats.obsolete_timesUnconscious);
				stats.obsolete_totalMoneyGifted = MergeStats("totalMoneyGifted", stats.obsolete_totalMoneyGifted);
				stats.obsolete_trufflesFound = MergeStats("trufflesFound", stats.obsolete_trufflesFound);
				stats.obsolete_weedsEliminated = MergeStats("weedsEliminated", stats.obsolete_weedsEliminated);
				stats.obsolete_seedsSown = MergeStats("seedsSown", stats.obsolete_seedsSown);
				uint? MergeStats(string newKey, uint? oldValue)
				{
					stats.Increment(newKey, oldValue.GetValueOrDefault());
					return null;
				}
			}
			return true;
		case SaveFixes.MakeWildSeedsDeterministic:
			Utility.ForEachCrop(delegate(Crop crop)
			{
				if (crop.isWildSeedCrop())
				{
					crop.replaceWithObjectOnFullGrown.Value = crop.getRandomWildCropForSeason(onlyDeterministic: true);
				}
				return true;
			});
			return true;
		case SaveFixes.FixTranslatedInternalNames:
			Utility.ForEachItem(delegate(Item item)
			{
				switch (item.QualifiedItemId)
				{
				case "(H)15":
				case "(H)17":
				case "(H)18":
				case "(H)23":
				case "(H)28":
				case "(H)35":
				case "(H)41":
				case "(H)50":
				case "(H)51":
				case "(H)82":
				case "(H)90":
				case "(O)804":
				case "(H)AbigailsBow":
				case "(H)GilsHat":
				case "(H)GovernorsHat":
					if (item.Name.Contains('’'))
					{
						item.Name = ItemRegistry.GetData(item.QualifiedItemId)?.InternalName ?? item.Name;
					}
					break;
				case "(H)GoldPanHat":
					if (item.Name == "Steel Pan")
					{
						item.Name = ItemRegistry.GetData(item.QualifiedItemId)?.InternalName ?? item.Name;
					}
					break;
				}
				return true;
			});
			return true;
		case SaveFixes.ConvertBuildingQuests:
			foreach (Farmer allFarmer12 in Game1.getAllFarmers())
			{
				for (int num = 0; num < allFarmer12.questLog.Count; num++)
				{
					Quest quest = allFarmer12.questLog[num];
					if (quest.questType.Value == 8)
					{
						allFarmer12.questLog[num] = new HaveBuildingQuest(quest.obsolete_completionString);
					}
				}
			}
			return true;
		case SaveFixes.AddJunimoKartAndPrairieKingStats:
			foreach (Farmer allFarmer13 in Game1.getAllFarmers())
			{
				if (allFarmer13.hasOrWillReceiveMail("JunimoKart"))
				{
					allFarmer13.stats.Increment("completedJunimoKart", 1);
				}
				if (allFarmer13.hasOrWillReceiveMail("Beat_PK"))
				{
					allFarmer13.stats.Increment("completedPrairieKing", 1);
				}
			}
			return true;
		case SaveFixes.FixEmptyLostAndFoundItemStacks:
			foreach (Item returnedDonation in Game1.player.team.returnedDonations)
			{
				if (returnedDonation != null && returnedDonation.Stack < 1)
				{
					returnedDonation.Stack = 1;
				}
			}
			return true;
		case SaveFixes.FixDuplicateMissedMail:
		{
			HashSet<string> hashSet = new HashSet<string>();
			List<int> list = new List<int>();
			foreach (Farmer allFarmer14 in Game1.getAllFarmers())
			{
				hashSet.Clear();
				list.Clear();
				for (int i = 0; i < allFarmer14.mailbox.Count; i++)
				{
					string text = allFarmer14.mailbox[i];
					if (!hashSet.Add(text))
					{
						switch (text)
						{
						case "robinKitchenLetter":
						case "marnieAutoGrabber":
						case "JunimoKart":
						case "Beat_PK":
							list.Add(i);
							break;
						}
					}
				}
				list.Reverse();
				foreach (int item12 in list)
				{
					allFarmer14.mailbox.RemoveAt(item12);
				}
			}
			return true;
		}
		default:
			return false;
		}
	}

	/// <summary>Convert individually implemented buildings that were saved before Stardew Valley 1.6 to the new Data/BuildingsData format.</summary>
	/// <param name="location">The location whose buildings to convert.</param>
	public static void ConvertBuildingsToData(GameLocation location)
	{
		for (int num = location.buildings.Count - 1; num >= 0; num--)
		{
			Building building = location.buildings[num];
			GameLocation indoors = building.GetIndoors();
			if (indoors != null)
			{
				ConvertBuildingsToData(indoors);
			}
			switch (building.buildingType.Value)
			{
			case "Log Cabin":
			case "Plank Cabin":
			case "Stone Cabin":
				building.skinId.Value = building.buildingType.Value;
				building.buildingType.Value = "Cabin";
				building.ReloadBuildingData();
				building.updateInteriorWarps();
				break;
			}
			string text = building.GetData()?.BuildingType;
			if (text != null && text != building.GetType().FullName)
			{
				Building building2 = Building.CreateInstanceFromId(building.buildingType.Value, new Vector2(building.tileX.Value, building.tileY.Value));
				if (building2 != null)
				{
					building2.indoors.Value = building.indoors.Value;
					building2.buildingType.Value = building.buildingType.Value;
					building2.tileX.Value = building.tileX.Value;
					building2.tileY.Value = building.tileY.Value;
					location.buildings.RemoveAt(num);
					location.buildings.Add(building2);
					TransferValuesToDataBuilding(building, building2);
				}
			}
		}
	}

	/// <summary>Copy values from an older pre-1.6 building to a new data-driven <see cref="T:StardewValley.Buildings.Building" /> instance.</summary>
	/// <param name="oldBuilding">The pre-1.6 building instance.</param>
	/// <param name="newBuilding">The new data-driven building instance that will replace <paramref name="oldBuilding" />.</param>
	public static void TransferValuesToDataBuilding(Building oldBuilding, Building newBuilding)
	{
		newBuilding.animalDoorOpen.Value = oldBuilding.animalDoorOpen.Value;
		newBuilding.animalDoorOpenAmount.Value = oldBuilding.animalDoorOpenAmount.Value;
		newBuilding.netBuildingPaintColor.Value.CopyFrom(oldBuilding.netBuildingPaintColor.Value);
		newBuilding.modData.CopyFrom(oldBuilding.modData.Pairs);
		if (oldBuilding is Mill mill)
		{
			mill.TransferValuesToNewBuilding(newBuilding);
		}
	}

	/// <summary>Migrate all farmhands from Cabin.deprecatedFarmhand into NetWorldState.</summary>
	/// <param name="locations">The locations to scan for cabins.</param>
	public static void MigrateFarmhands(List<GameLocation> locations)
	{
		foreach (GameLocation location in locations)
		{
			foreach (Building building in location.buildings)
			{
				if (building.GetIndoors() is Cabin { obsolete_farmhand: var obsolete_farmhand } cabin)
				{
					cabin.obsolete_farmhand = null;
					Game1.netWorldState.Value.farmhandData[obsolete_farmhand.UniqueMultiplayerID] = obsolete_farmhand;
					cabin.farmhandReference.Value = obsolete_farmhand;
				}
			}
		}
	}

	/// <summary>Migrate saved bundle data from Stardew Valley 1.5.6 or earlier to the new format.</summary>
	/// <param name="bundleData">The raw bundle data to standardize.</param>
	public static void StandardizeBundleFields(Dictionary<string, string> bundleData)
	{
		string[] array = bundleData.Keys.ToArray();
		foreach (string key in array)
		{
			string[] array2 = bundleData[key].Split('/');
			if (array2.Length < 7)
			{
				Array.Resize(ref array2, 7);
				array2[6] = array2[0];
				bundleData[key] = string.Join("/", array2);
			}
		}
	}

	/// <summary>For a building with an upgrade started before 1.6, get the building type it should be upgraded to if possible.</summary>
	/// <param name="fromBuildingType">The building type before the upgrade finishes.</param>
	public static string InferBuildingUpgradingTo(string fromBuildingType)
	{
		switch (fromBuildingType)
		{
		case "Coop":
			return "Big Coop";
		case "Big Coop":
			return "Deluxe Coop";
		case "Barn":
			return "Big Barn";
		case "Big Barn":
			return "Deluxe Barn";
		case "Shed":
			return "Big Shed";
		default:
			foreach (KeyValuePair<string, BuildingData> buildingDatum in Game1.buildingData)
			{
				if (buildingDatum.Value.BuildingToUpgrade == fromBuildingType)
				{
					return buildingDatum.Key;
				}
			}
			return null;
		}
	}

	/// <summary>For a machine which contains output produced before 1.6, set the <see cref="F:StardewValley.Object.lastInputItem" /> and <see cref="F:StardewValley.Object.lastOutputRuleId" /> values when possible. This ensures that some machine logic works as expected (e.g. crystalariums resuming on collect).</summary>
	/// <param name="machine">The machine which produced output.</param>
	/// <remarks>This is heuristic, and some fields may not be set if it's not possible to retroactively infer them.</remarks>
	public static void InferMachineInputOutputFields(Object machine)
	{
		Object value = machine.heldObject.Value;
		string text = value?.QualifiedItemId;
		if (text == null)
		{
			return;
		}
		NetRef<Item> lastInputItem = machine.lastInputItem;
		NetString lastOutputRuleId = machine.lastOutputRuleId;
		string qualifiedItemId = machine.QualifiedItemId;
		if (qualifiedItemId == null)
		{
			return;
		}
		switch (qualifiedItemId.Length)
		{
		case 6:
			switch (qualifiedItemId[5])
			{
			default:
				return;
			case '0':
				break;
			case '7':
				if (qualifiedItemId == "(BC)17" && text == "(O)428")
				{
					lastOutputRuleId.Value = "Default";
					lastInputItem.Value = ItemRegistry.Create("(O)440");
				}
				return;
			case '3':
			{
				if (!(qualifiedItemId == "(BC)13") || text == null)
				{
					return;
				}
				int length = text.Length;
				if (length != 6)
				{
					return;
				}
				switch (text[5])
				{
				case '4':
					if (text == "(O)334")
					{
						lastOutputRuleId.Value = "Default_CopperOre";
						lastInputItem.Value = ItemRegistry.Create("(O)378", 5);
					}
					break;
				case '5':
					if (text == "(O)335")
					{
						lastOutputRuleId.Value = "Default_IronOre";
						lastInputItem.Value = ItemRegistry.Create("(O)380", 5);
					}
					break;
				case '6':
					if (text == "(O)336")
					{
						lastOutputRuleId.Value = "Default_GoldOre";
						lastInputItem.Value = ItemRegistry.Create("(O)384", 5);
					}
					break;
				case '7':
					if (!(text == "(O)337"))
					{
						if (text == "(O)277")
						{
							lastOutputRuleId.Value = "Default_Bouquet";
							lastInputItem.Value = ItemRegistry.Create("(O)458");
						}
					}
					else
					{
						lastOutputRuleId.Value = "Default_IridiumOre";
						lastInputItem.Value = ItemRegistry.Create("(O)386", 5);
					}
					break;
				case '8':
					if (text == "(O)338")
					{
						if (value.Stack > 1)
						{
							lastOutputRuleId.Value = "Default_FireQuartz";
							lastInputItem.Value = ItemRegistry.Create("(O)82");
						}
						else
						{
							lastOutputRuleId.Value = "Default_Quartz";
							lastInputItem.Value = ItemRegistry.Create("(O)80");
						}
					}
					break;
				case '0':
					if (text == "(O)910")
					{
						lastOutputRuleId.Value = "Default_RadioactiveOre";
						lastInputItem.Value = ItemRegistry.Create("(O)909", 5);
					}
					break;
				case '1':
				case '2':
				case '3':
					break;
				}
				return;
			}
			case '2':
			{
				if (!(qualifiedItemId == "(BC)12"))
				{
					return;
				}
				switch (text)
				{
				case "(O)346":
					lastOutputRuleId.Value = "Default_Wheat";
					lastInputItem.Value = ItemRegistry.Create("(O)262");
					return;
				case "(O)303":
					lastOutputRuleId.Value = "Default_Hops";
					lastInputItem.Value = ItemRegistry.Create("(O)304");
					return;
				case "(O)614":
					lastOutputRuleId.Value = "Default_TeaLeaves";
					lastInputItem.Value = ItemRegistry.Create("(O)815");
					return;
				case "(O)395":
					lastOutputRuleId.Value = "Default_CoffeeBeans";
					lastInputItem.Value = ItemRegistry.Create("(O)433", 5);
					return;
				case "(O)340":
					lastOutputRuleId.Value = "Default_Honey";
					lastInputItem.Value = ItemRegistry.Create("(O)459", 5);
					return;
				}
				Object.PreserveType? value2 = value.preserve.Value;
				if (value2.HasValue)
				{
					switch (value2.GetValueOrDefault())
					{
					case Object.PreserveType.Juice:
						lastOutputRuleId.Value = "Default_Juice";
						lastInputItem.Value = ItemRegistry.Create(value.preservedParentSheetIndex.Value, 1, 0, allowNull: true);
						break;
					case Object.PreserveType.Wine:
						lastOutputRuleId.Value = "Default_Wine";
						lastInputItem.Value = ItemRegistry.Create(value.preservedParentSheetIndex.Value, 1, 0, allowNull: true);
						break;
					}
				}
				return;
			}
			case '5':
				if (!(qualifiedItemId == "(BC)15"))
				{
					if (qualifiedItemId == "(BC)25")
					{
						lastOutputRuleId.Value = "Default";
						if (text != "(O)499" && value.HasTypeObject() && Game1.cropData.TryGetValue(value.ItemId, out var value3) && value3.HarvestItemId != null)
						{
							lastInputItem.Value = ItemRegistry.Create(value3.HarvestItemId, 1, 0, allowNull: true);
						}
					}
					return;
				}
				switch (text)
				{
				case "(O)445":
					lastOutputRuleId.Value = "Default_SturgeonRoe";
					lastInputItem.Value = ItemRegistry.GetObjectTypeDefinition().CreateFlavoredRoe(ItemRegistry.Create<Object>("(O)698"));
					break;
				case "(O)447":
					lastOutputRuleId.Value = "Default_Roe";
					lastInputItem.Value = ItemRegistry.GetObjectTypeDefinition().CreateFlavoredRoe(ItemRegistry.Create<Object>(value.preservedParentSheetIndex.Value));
					break;
				case "(O)342":
					lastOutputRuleId.Value = "Default_Pickled";
					lastInputItem.Value = ItemRegistry.Create(value.preservedParentSheetIndex.Value, 1, 0, allowNull: true);
					break;
				case "(O)344":
					lastOutputRuleId.Value = "Default_Jelly";
					lastInputItem.Value = ItemRegistry.Create(value.preservedParentSheetIndex.Value, 1, 0, allowNull: true);
					break;
				}
				return;
			case '6':
				if (!(qualifiedItemId == "(BC)16"))
				{
					return;
				}
				if (!(text == "(O)426"))
				{
					if (text == "(O)424")
					{
						if (value.Quality == 0)
						{
							lastOutputRuleId.Value = "Default_Milk";
							lastInputItem.Value = ItemRegistry.Create("(O)184");
						}
						else
						{
							lastOutputRuleId.Value = "Default_LargeMilk";
							lastInputItem.Value = ItemRegistry.Create("(O)186");
						}
					}
				}
				else if (value.Quality == 0)
				{
					lastOutputRuleId.Value = "Default_GoatMilk";
					lastInputItem.Value = ItemRegistry.Create("(O)436");
				}
				else
				{
					lastOutputRuleId.Value = "Default_LargeGoatMilk";
					lastInputItem.Value = ItemRegistry.Create("(O)438");
				}
				return;
			case '4':
				if (!(qualifiedItemId == "(BC)24"))
				{
					return;
				}
				switch (text)
				{
				case "(O)306":
					switch (value.Stack)
					{
					case 10:
						lastOutputRuleId.Value = "Default_OstrichEgg";
						lastInputItem.Value = ItemRegistry.Create("(O)289", 1, value.Quality);
						break;
					case 3:
						lastOutputRuleId.Value = "Default_GoldenEgg";
						lastInputItem.Value = ItemRegistry.Create("(O)928");
						break;
					default:
						if (value.Quality == 2)
						{
							lastOutputRuleId.Value = "Default_LargeEgg";
							lastInputItem.Value = ItemRegistry.Create("(O)174");
						}
						else
						{
							lastOutputRuleId.Value = "Default_Egg";
							lastInputItem.Value = ItemRegistry.Create("(O)176");
						}
						break;
					}
					break;
				case "(O)307":
					lastOutputRuleId.Value = "Default_DuckEgg";
					lastInputItem.Value = ItemRegistry.Create("(O)442");
					break;
				case "(O)308":
					lastOutputRuleId.Value = "Default_VoidEgg";
					lastInputItem.Value = ItemRegistry.Create("(O)305");
					break;
				case "(O)807":
					lastOutputRuleId.Value = "Default_DinosaurEgg";
					lastInputItem.Value = ItemRegistry.Create("(O)107");
					break;
				}
				return;
			case '9':
				if (qualifiedItemId == "(BC)19" && !(text == "(O)247") && text == "(O)432")
				{
					lastOutputRuleId.Value = "Default_Truffle";
					lastInputItem.Value = ItemRegistry.Create("(O)430");
				}
				return;
			case '1':
				if (qualifiedItemId == "(BC)21")
				{
					lastOutputRuleId.Value = "Default";
					lastInputItem.Value = value.getOne();
				}
				return;
			case '8':
				return;
			}
			switch (qualifiedItemId)
			{
			default:
				return;
			case "(BC)90":
				switch (text)
				{
				case "(O)466":
				case "(O)465":
				case "(O)369":
				case "(O)805":
					lastOutputRuleId.Value = "Default";
					break;
				}
				return;
			case "(BC)20":
				if (text == null)
				{
					return;
				}
				switch (text.Length)
				{
				case 6:
				{
					char c = text[4];
					if ((uint)c <= 51u)
					{
						switch (c)
						{
						default:
							return;
						case '3':
							_ = text == "(O)338";
							return;
						case '2':
							break;
						}
						if (!(text == "(O)428"))
						{
							return;
						}
						break;
					}
					switch (c)
					{
					default:
						return;
					case '8':
						switch (text)
						{
						default:
							return;
						case "(O)382":
						case "(O)380":
							break;
						case "(O)388":
							lastOutputRuleId.Value = "Default_Driftwood";
							lastInputItem.Value = ItemRegistry.Create("(O)169");
							return;
						}
						break;
					case '9':
						if (!(text == "(O)390"))
						{
							return;
						}
						break;
					}
					lastOutputRuleId.Value = "Default_Trash";
					lastInputItem.Value = ItemRegistry.Create("(O)168");
					return;
				}
				case 5:
					if (!(text == "(O)93"))
					{
						return;
					}
					break;
				default:
					return;
				}
				lastOutputRuleId.Value = "Default_SoggyNewspaper";
				lastInputItem.Value = ItemRegistry.Create("(O)172");
				return;
			case "(BC)10":
				break;
			}
			goto IL_0c29;
		case 7:
			switch (qualifiedItemId[5])
			{
			default:
				return;
			case '6':
				switch (qualifiedItemId)
				{
				default:
					_ = qualifiedItemId == "(BC)264";
					return;
				case "(BC)163":
					switch (text)
					{
					case "(O)424":
						lastOutputRuleId.Value = "Cheese";
						break;
					case "(O)426":
						lastOutputRuleId.Value = "GoatCheese";
						break;
					case "(O)348":
						lastOutputRuleId.Value = "Wine";
						break;
					case "(O)459":
						lastOutputRuleId.Value = "Mead";
						break;
					case "(O)303":
						lastOutputRuleId.Value = "PaleAle";
						break;
					case "(O)346":
						lastOutputRuleId.Value = "Beer";
						break;
					}
					if (lastOutputRuleId.Value != null)
					{
						lastInputItem.Value = value.getOne();
						lastInputItem.Value.Quality = 0;
					}
					return;
				case "(BC)265":
					lastOutputRuleId.Value = "Default";
					return;
				case "(BC)160":
					break;
				}
				break;
			case '1':
				switch (qualifiedItemId)
				{
				default:
					return;
				case "(BC)114":
					if (text == "(O)382")
					{
						lastOutputRuleId.Value = "Default";
						lastInputItem.Value = ItemRegistry.Create("(O)388", 10);
					}
					return;
				case "(BC)117":
					break;
				case "(BC)211":
					return;
				}
				break;
			case '0':
				if (!(qualifiedItemId == "(BC)101"))
				{
					_ = qualifiedItemId == "(BC)105";
					return;
				}
				goto IL_0b7a;
			case '5':
				switch (qualifiedItemId)
				{
				default:
					return;
				case "(BC)254":
				case "(BC)156":
					break;
				case "(BC)158":
					lastOutputRuleId.Value = "Default";
					lastInputItem.Value = ItemRegistry.Create("(O)766", 100);
					return;
				case "(BC)154":
					goto end_IL_00a5;
				}
				goto IL_0b7a;
			case '8':
				if (!(qualifiedItemId == "(BC)182"))
				{
					if (!(qualifiedItemId == "(BC)280"))
					{
						return;
					}
					break;
				}
				lastOutputRuleId.Value = "Default";
				return;
			case '4':
				if (!(qualifiedItemId == "(BC)246"))
				{
					return;
				}
				break;
			case '3':
				if (!(qualifiedItemId == "(BC)231"))
				{
					return;
				}
				break;
			case '2':
				if (!(qualifiedItemId == "(BC)127") && !(qualifiedItemId == "(BC)128"))
				{
					return;
				}
				break;
			case '7':
				return;
				IL_0b7a:
				lastOutputRuleId.Value = "Default";
				lastInputItem.Value = value.getOne();
				return;
				end_IL_00a5:
				break;
			}
			goto IL_0c29;
		case 5:
			{
				if (!(qualifiedItemId == "(BC)9"))
				{
					break;
				}
				goto IL_0c29;
			}
			IL_0c29:
			lastOutputRuleId.Value = "Default";
			break;
		}
	}

	/// <summary>Migrate a pre-1.6 quest to the new format.</summary>
	/// <param name="serializer">The XML serializer with which to serialize/deserialize <see cref="T:StardewValley.Quests.DescriptionElement" /> and <see cref="T:StardewValley.SaveMigrations.SaveMigrator_1_6.LegacyDescriptionElement" /> values.</param>
	/// <param name="element">The description element to migrate.</param>
	/// <remarks>
	///   This updates quest data for two changes in 1.6:
	///
	///   <list type="bullet">
	///     <item><description>
	///       The way <see cref="F:StardewValley.Quests.DescriptionElement.substitutions" /> values are stored in the save XML changed from this:
	///
	///       <code>
	///         &lt;objective&gt;
	///           &lt;xmlKey&gt;Strings\StringsFromCSFiles:SocializeQuest.cs.13802&lt;/xmlKey&gt;
	///           &lt;param&gt;
	///             &lt;anyType xsi:type="xsd:int"&gt;4&lt;/anyType&gt;
	///             &lt;anyType xsi:type="xsd:int"&gt;28&lt;/anyType&gt;
	///           &lt;/param&gt;
	///         &lt;/objective&gt;
	///       </code>
	///
	///      To this:
	///
	///       <code>
	///         &lt;objective&gt;
	///           &lt;xmlKey&gt;Strings\StringsFromCSFiles:SocializeQuest.cs.13802&lt;/xmlKey&gt;
	///           &lt;param xsi:type="xsd:int"&gt;4&lt;/param&gt;
	///           &lt;param xsi:type="xsd:int"&gt;28&lt;/param&gt;
	///         &lt;/objective&gt;
	///       </code>
	///
	///       If the given description element is affected, this method re-deserializes the data into the correct format.
	///   </description></item>
	///
	///   <item><description>Some translation keys were merged to fix gender issues.</description></item>
	///   </list>
	/// </remarks>
	public static void MigrateLegacyDescriptionElement(Lazy<XmlSerializer> serializer, DescriptionElement element)
	{
		if (element == null)
		{
			return;
		}
		List<object> substitutions = element.substitutions;
		if (substitutions != null && substitutions.Count == 1 && element.substitutions[0] is XmlNode[] array)
		{
			StringBuilder stringBuilder = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?><LegacyDescriptionElement xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><param>");
			XmlNode[] array2 = array;
			foreach (XmlNode xmlNode in array2)
			{
				stringBuilder.Append(xmlNode.OuterXml);
			}
			stringBuilder.Append("</param></LegacyDescriptionElement>");
			LegacyDescriptionElement legacyDescriptionElement;
			using (StringReader input = new StringReader(stringBuilder.ToString()))
			{
				using XmlReader xmlReader = new XmlTextReader(input);
				legacyDescriptionElement = (LegacyDescriptionElement)serializer.Value.Deserialize(xmlReader);
			}
			if (legacyDescriptionElement != null)
			{
				element.substitutions = legacyDescriptionElement.param;
			}
		}
		switch (element.translationKey)
		{
		case "Strings\\StringsFromCSFiles:FishingQuest.cs.13251":
			element.translationKey = "Strings\\StringsFromCSFiles:FishingQuest.cs.13248";
			break;
		case "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13563":
			element.translationKey = "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13560";
			break;
		case "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13574":
			element.translationKey = "Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13571";
			break;
		}
		List<object> substitutions2 = element.substitutions;
		if (substitutions2 == null || substitutions2.Count <= 0)
		{
			return;
		}
		foreach (object substitution in element.substitutions)
		{
			if (substitution is DescriptionElement element2)
			{
				MigrateLegacyDescriptionElement(serializer, element2);
			}
		}
	}
}
