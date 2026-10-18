using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Delegates;
using StardewValley.Events;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Movies;
using StardewValley.GameData.Pets;
using StardewValley.GameData.Shops;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Logging;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Network.Compress;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.SaveMigrations;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using StardewValley.Triggers;
using StardewValley.Util;
using StardewValley.WorldMaps;
using xTile.Dimensions;
using xTile.Layers;

namespace StardewValley;

/// <summary>The debug commands that can be executed through the console.</summary>
/// <remarks>See also <see cref="T:StardewValley.ChatCommands" />.</remarks>
/// <summary>The debug commands that can be executed through the console.</summary>
/// <remarks>See also <see cref="T:StardewValley.ChatCommands" />.</remarks>
public static class DebugCommands
{
	/// <summary>The low-level handlers for vanilla debug commands. Most code should call <see cref="M:StardewValley.DebugCommands.TryHandle(System.String[],StardewValley.Logging.IGameLogger)" /> instead, which adds error-handling.</summary>
	public static class DefaultHandlers
	{
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void GrowWildTrees(string[] command, IGameLogger log)
		{
			TerrainFeature[] array = Game1.currentLocation.terrainFeatures.Values.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] is Tree tree)
				{
					tree.growthStage.Value = 4;
					tree.fertilized.Value = true;
					tree.dayUpdate();
					tree.fertilized.Value = false;
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Emote(string[] command, IGameLogger log)
		{
			for (int i = 1; i < command.Length; i += 2)
			{
				if (!ArgUtility.TryGet(command, i, out var value, out var error, allowBlank: false, "string npcName") || !ArgUtility.TryGetInt(command, i + 1, out var value2, out error, "int emoteId"))
				{
					log.Warn(error);
					continue;
				}
				NPC nPC = Utility.fuzzyCharacterSearch(value, must_be_villager: false);
				if (nPC == null)
				{
					log.Error("Couldn't find character named " + value);
				}
				else
				{
					nPC.doEmote(value2);
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void EventTestSpecific(string[] command, IGameLogger log)
		{
			Game1.eventTest = new EventTest(command);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void EventTest(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptional(command, 1, out var value, out var error, null, allowBlank: true, "string locationName") || !ArgUtility.TryGetOptionalInt(command, 2, out var value2, out error, 0, "int startingEventIndex"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.eventTest = new EventTest(value ?? "", value2);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void GetAllQuests(string[] command, IGameLogger log)
		{
			foreach (KeyValuePair<string, string> item in DataLoader.Quests(Game1.content))
			{
				Game1.player.addQuest(item.Key);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Movie(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptional(command, 1, out var value, out var error, null, allowBlank: false, "string movieId") || !ArgUtility.TryGetOptional(command, 2, out var value2, out error, null, allowBlank: false, "string invitedNpcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (value != null && !MovieTheater.TryGetMovieData(value, out var _))
			{
				log.Error("No movie found with ID '" + value + "'.");
				return;
			}
			if (value2 != null)
			{
				NPC nPC = Utility.fuzzyCharacterSearch(value2);
				if (nPC != null)
				{
					MovieTheater.Invite(Game1.player, nPC);
				}
				else
				{
					log.Error("No NPC found matching '" + value2 + "'.");
				}
			}
			if (value != null)
			{
				MovieTheater.forceMovieId = value;
			}
			LocationRequest locationRequest = Game1.getLocationRequest("MovieTheater");
			locationRequest.OnWarp += delegate
			{
				((MovieTheater)Game1.currentLocation).performAction("Theater_Doors", Game1.player, Location.Origin);
			};
			Game1.warpFarmer(locationRequest, 10, 10, 0);
		}

		/// <summary>Print the movie schedule for a specified year.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MovieSchedule(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, Game1.year, "int year"))
			{
				LogArgError(log, command, error);
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder);
			handler.AppendLiteral("Movie schedule for ");
			handler.AppendFormatted((value == Game1.year) ? $"this year (year {value})" : $"year {value}");
			handler.AppendLiteral(":");
			StringBuilder stringBuilder2 = stringBuilder.AppendLine(ref handler).AppendLine();
			Season[] array = new Season[4]
			{
				StardewValley.Season.Spring,
				StardewValley.Season.Summer,
				StardewValley.Season.Fall,
				StardewValley.Season.Winter
			};
			foreach (Season season in array)
			{
				List<Tuple<MovieData, int>> list = new List<Tuple<MovieData, int>>();
				string text = null;
				for (int j = 1; j <= 28; j++)
				{
					MovieData movieForDate = MovieTheater.GetMovieForDate(new WorldDate(value, season, j));
					if (movieForDate.Id != text)
					{
						list.Add(Tuple.Create(movieForDate, j));
						text = movieForDate.Id;
					}
				}
				for (int k = 0; k < list.Count; k++)
				{
					MovieData item = list[k].Item1;
					int item2 = list[k].Item2;
					int num = ((list.Count > k + 1) ? (list[k + 1].Item2 - 1) : 28);
					string value2 = TokenParser.ParseText(item.Title);
					stringBuilder2.Append(season).Append(' ').Append(item2);
					if (num != item2)
					{
						stringBuilder2.Append("-").Append(num);
					}
					stringBuilder2.Append(": ").AppendLine(value2);
				}
			}
			log.Info(stringBuilder2.ToString());
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Shop(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string shopId") || !ArgUtility.TryGetOptional(command, 2, out var value2, out error, null, allowBlank: false, "string ownerName"))
			{
				LogArgError(log, command, error);
				return;
			}
			string text = Utility.fuzzySearch(value, DataLoader.Shops(Game1.content).Keys.ToArray());
			if (text == null)
			{
				log.Error("Couldn't find any shop in Data/Shops matching ID '" + value + "'.");
				return;
			}
			value = text;
			if ((value2 != null) ? Utility.TryOpenShopMenu(value, value2) : Utility.TryOpenShopMenu(value, Game1.player.currentLocation, null, null, forceOpen: true))
			{
				log.Info("Opened shop with ID '" + value + "'.");
			}
			else
			{
				log.Error("Failed to open shop with ID '" + value + "'. Is the data in Data/Shops valid?");
			}
		}

		/// <summary>Export a summary of every shop's current inventory.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ExportShops(string[] command, IGameLogger log)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string[] array = new string[2] { "Shop", null };
			foreach (string key2 in DataLoader.Shops(Game1.content).Keys)
			{
				stringBuilder.AppendLine(key2);
				stringBuilder.AppendLine("".PadRight(Math.Max(50, key2.Length), '-'));
				try
				{
					array[1] = key2;
					Shop(array, log);
				}
				catch (Exception ex)
				{
					StringBuilder stringBuilder2 = stringBuilder.Append("    ");
					StringBuilder stringBuilder3 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder2);
					handler.AppendLiteral("Failed to open shop '");
					handler.AppendFormatted(key2);
					handler.AppendLiteral("'.");
					stringBuilder3.AppendLine(ref handler);
					stringBuilder.AppendLine("    " + string.Join("\n    ", ex.ToString().Split('\n')));
					continue;
				}
				if (Game1.activeClickableMenu is ShopMenu shopMenu)
				{
					switch (shopMenu.currency)
					{
					case 0:
						stringBuilder.AppendLine("    Currency: gold");
						break;
					case 1:
						stringBuilder.AppendLine("    Currency: star tokens");
						break;
					case 2:
						stringBuilder.AppendLine("    Currency: Qi coins");
						break;
					case 4:
						stringBuilder.AppendLine("    Currency: Qi gems");
						break;
					default:
					{
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder4 = stringBuilder2;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(20, 2, stringBuilder2);
						handler.AppendFormatted("    ");
						handler.AppendLiteral("Currency: unknown (");
						handler.AppendFormatted(shopMenu.currency);
						handler.AppendLiteral(")");
						stringBuilder4.AppendLine(ref handler);
						break;
					}
					}
					stringBuilder.AppendLine();
					var array2 = shopMenu.itemPriceAndStock.Select(delegate(KeyValuePair<ISalable, ItemStockInformation> entry)
					{
						ISalable key = entry.Key;
						ItemStockInformation value = entry.Value;
						return new
						{
							Id = key.QualifiedItemId,
							Name = key.DisplayName,
							Price = value.Price,
							Trade = ((value.TradeItem != null) ? (value.TradeItem + " x" + (value.TradeItemCount ?? 1)) : null),
							StockLimit = ((value.Stock != int.MaxValue && value.LimitedStockMode != LimitedStockMode.None) ? $"{value.LimitedStockMode} {value.Stock}" : null)
						};
					}).ToArray();
					int num = "id".Length;
					int num2 = "name".Length;
					int num3 = "price".Length;
					int num4 = "trade".Length;
					int length = "stock limit".Length;
					var array3 = array2;
					foreach (var anon in array3)
					{
						num = Math.Max(num, anon.Id.Length);
						num2 = Math.Max(num2, anon.Name.Length);
						num3 = Math.Max(num3, anon.Price.ToString().Length);
						if (anon.Trade != null)
						{
							num4 = Math.Max(num4, anon.Trade.Length);
						}
						if (anon.StockLimit != null)
						{
							num4 = Math.Max(num4, anon.StockLimit.Length);
						}
					}
					stringBuilder.Append("    ").Append("id".PadRight(num)).Append(" | ")
						.Append("name".PadRight(num2))
						.Append(" | ")
						.Append("price".PadRight(num3))
						.Append(" | ")
						.Append("trade".PadRight(num4))
						.AppendLine(" | stock limit");
					stringBuilder.Append("    ").Append("".PadRight(num, '-')).Append(" | ")
						.Append("".PadRight(num2, '-'))
						.Append(" | ")
						.Append("".PadRight(num3, '-'))
						.Append(" | ")
						.Append("".PadRight(num4, '-'))
						.Append(" | ")
						.AppendLine("".PadRight(length, '-'));
					array3 = array2;
					foreach (var anon2 in array3)
					{
						stringBuilder.Append("    ").Append(anon2.Id.PadRight(num)).Append(" | ")
							.Append(anon2.Name.PadRight(num2))
							.Append(" | ")
							.Append(anon2.Price.ToString().PadRight(num3))
							.Append(" | ")
							.Append((anon2.Trade ?? "").PadRight(num4))
							.Append(" | ")
							.AppendLine(anon2.StockLimit);
					}
				}
				else
				{
					StringBuilder stringBuilder2 = stringBuilder.Append("    ");
					StringBuilder stringBuilder5 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(60, 1, stringBuilder2);
					handler.AppendLiteral("Failed to open shop '");
					handler.AppendFormatted(key2);
					handler.AppendLiteral("': shop menu unexpected failed to open.");
					stringBuilder5.AppendLine(ref handler);
				}
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
			}
			string text = Path.Combine(Program.GetLocalAppDataFolder("Exports"), $"{DateTime.Now:yyyy-MM-dd} shop export.txt");
			File.WriteAllText(text, stringBuilder.ToString());
			log.Info("Exported shop data to " + text + ".");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Dating(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.friendshipData[value].Status = FriendshipStatus.Dating;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearActiveDialogueEvents(string[] command, IGameLogger log)
		{
			Game1.player.activeDialogueEvents.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Buff(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string buffId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.applyBuff(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearBuffs(string[] command, IGameLogger log)
		{
			Game1.player.ClearBuffs();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void PauseTime(string[] command, IGameLogger log)
		{
			Game1.isTimePaused = !Game1.isTimePaused;
			Game1.playSound(Game1.isTimePaused ? "bigSelect" : "bigDeSelect");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "fbf" })]
		public static void FrameByFrame(string[] command, IGameLogger log)
		{
			Game1.frameByFrame = !Game1.frameByFrame;
			Game1.playSound(Game1.frameByFrame ? "bigSelect" : "bigDeSelect");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "fbp", "fill", "fillbp" })]
		public static void FillBackpack(string[] command, IGameLogger log)
		{
			for (int i = 0; i < Game1.player.Items.Count; i++)
			{
				if (Game1.player.Items[i] != null)
				{
					continue;
				}
				ItemMetadata itemMetadata = null;
				while (itemMetadata == null)
				{
					itemMetadata = ItemRegistry.ResolveMetadata(Game1.random.Next(1000).ToString());
					ParsedItemData parsedItemData = itemMetadata?.GetParsedData();
					if (parsedItemData == null || parsedItemData.Category == -999 || parsedItemData.ObjectType == "Crafting" || parsedItemData.ObjectType == "Seeds")
					{
						itemMetadata = null;
					}
				}
				Game1.player.Items[i] = itemMetadata.CreateItem();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Bobber(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int bobberStyle"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.bobberStyle.Value = value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "sl" })]
		public static void ShiftToolbarLeft(string[] command, IGameLogger log)
		{
			Game1.player.shiftToolbar(right: false);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "sr" })]
		public static void ShiftToolbarRight(string[] command, IGameLogger log)
		{
			Game1.player.shiftToolbar(right: true);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CharacterInfo(string[] command, IGameLogger log)
		{
			Game1.showGlobalMessage(Game1.currentLocation.characters.Count + " characters on this map");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DoesItemExist(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string itemId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.showGlobalMessage(Utility.doesItemExistAnywhere(value) ? "Yes" : "No");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SpecialItem(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string itemId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.specialItems.Add(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void AnimalInfo(string[] command, IGameLogger log)
		{
			int animalCount = 0;
			int locationCount = 0;
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				int length = location.animals.Length;
				if (length > 0)
				{
					animalCount += length;
					locationCount++;
				}
				return true;
			});
			Game1.showGlobalMessage($"{animalCount} animals in {locationCount} locations");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearChildren(string[] command, IGameLogger log)
		{
			Game1.player.getRidOfChildren();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CreateSplash(string[] command, IGameLogger log)
		{
			Point point = default(Point);
			switch (Game1.player.FacingDirection)
			{
			case 3:
				point.X = -4;
				break;
			case 1:
				point.X = 4;
				break;
			case 0:
				point.Y = 4;
				break;
			case 2:
				point.Y = -4;
				break;
			}
			Game1.player.currentLocation.fishSplashPoint.Set(new Point(Game1.player.TilePoint.X + point.X, Game1.player.TilePoint.Y + point.Y));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Pregnant(string[] command, IGameLogger log)
		{
			WorldDate date = Game1.Date;
			date.TotalDays++;
			Game1.player.GetSpouseFriendship().NextBirthingDate = date;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SpreadSeeds(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var cropId, out var error, allowBlank: false, "string cropId"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.currentLocation?.ForEachDirt(delegate(HoeDirt dirt)
			{
				dirt.crop = new Crop(cropId, (int)dirt.Tile.X, (int)dirt.Tile.Y, dirt.Location);
				return true;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SpreadDirt(string[] command, IGameLogger log)
		{
			GameLocation currentLocation = Game1.currentLocation;
			if (currentLocation == null)
			{
				return;
			}
			for (int i = 0; i < currentLocation.map.Layers[0].LayerWidth; i++)
			{
				for (int j = 0; j < currentLocation.map.Layers[0].LayerHeight; j++)
				{
					if (currentLocation.doesTileHaveProperty(i, j, "Diggable", "Back") != null && currentLocation.CanItemBePlacedHere(new Vector2(i, j), itemIsPassable: true, CollisionMask.All, CollisionMask.None))
					{
						currentLocation.terrainFeatures.Add(new Vector2(i, j), new HoeDirt());
					}
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void RemoveFurniture(string[] command, IGameLogger log)
		{
			Game1.currentLocation.furniture.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MakeEx(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.player.friendshipData[value].RoommateMarriage = false;
			Game1.player.friendshipData[value].Status = FriendshipStatus.Divorced;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DarkTalisman(string[] command, IGameLogger log)
		{
			GameLocation gameLocation = Game1.RequireLocation("Railroad");
			GameLocation gameLocation2 = Game1.RequireLocation("WitchHut");
			gameLocation.setMapTile(54, 35, 287, "Buildings", "untitled tile sheet", "");
			gameLocation.setMapTile(54, 34, 262, "Front", "untitled tile sheet", "");
			gameLocation2.setMapTile(4, 11, 114, "Buildings", "untitled tile sheet", "MagicInk");
			Game1.player.hasDarkTalisman = true;
			Game1.player.hasMagicInk = false;
			Game1.player.mailReceived.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ConventionMode(string[] command, IGameLogger log)
		{
			Game1.conventionMode = !Game1.conventionMode;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void FarmMap(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int farmType"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.locations.RemoveWhere((GameLocation location) => location is Farm || location is FarmHouse);
			Game1.whichFarm = value;
			Game1.locations.Add(new Farm("Maps\\" + Farm.getMapNameFromTypeInt(Game1.whichFarm), "Farm"));
			Game1.locations.Add(new FarmHouse("Maps\\FarmHouse", "FarmHouse"));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearMuseum(string[] command, IGameLogger log)
		{
			Game1.RequireLocation<LibraryMuseum>("ArchaeologyHouse").museumPieces.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Clone(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.currentLocation.characters.Add(Utility.fuzzyCharacterSearch(value));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "zl" })]
		public static void ZoomLevel(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int zoomLevel"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.options.desiredBaseZoomLevel = (float)value / 100f;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "us" })]
		public static void UiScale(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int uiScale"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.options.desiredUIScale = (float)value / 100f;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DeleteArch(string[] command, IGameLogger log)
		{
			Game1.player.archaeologyFound.Clear();
			Game1.player.fishCaught.Clear();
			Game1.player.mineralsFound.Clear();
			Game1.player.mailReceived.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Save(string[] command, IGameLogger log)
		{
			Game1.saveOnNewDay = !Game1.saveOnNewDay;
			Game1.playSound(Game1.saveOnNewDay ? "bigSelect" : "bigDeSelect");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "removeLargeTf" })]
		public static void RemoveLargeTerrainFeature(string[] command, IGameLogger log)
		{
			Game1.currentLocation.largeTerrainFeatures.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Test(string[] command, IGameLogger log)
		{
			Game1.currentMinigame = new Test();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void FenceDecay(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int decayAmount"))
			{
				LogArgError(log, command, error);
				return;
			}
			foreach (Object value2 in Game1.currentLocation.objects.Values)
			{
				if (value2 is Fence fence)
				{
					fence.health.Value -= value;
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "sb" })]
		public static void ShowTextAboveHead(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Utility.fuzzyCharacterSearch(value).showTextAboveHead(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3206"));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Gamepad(string[] command, IGameLogger log)
		{
			Game1.options.gamepadControls = !Game1.options.gamepadControls;
			Game1.options.mouseControls = !Game1.options.gamepadControls;
			Game1.showGlobalMessage(Game1.options.gamepadControls ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3209") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3210"));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Slimecraft(string[] command, IGameLogger log)
		{
			Game1.player.craftingRecipes.Add("Slime Incubator", 0);
			Game1.player.craftingRecipes.Add("Slime Egg-Press", 0);
			Game1.playSound("crystal", 0);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "kms" })]
		public static void KillMonsterStat(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string monsterId") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int kills"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.stats.specificMonstersKilled[value] = value2;
			log.Info(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3159", value, value2));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void RemoveAnimals(string[] command, IGameLogger log)
		{
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				location.Animals.Clear();
				foreach (Building building in location.buildings)
				{
					if (building.GetIndoors() is AnimalHouse animalHouse)
					{
						animalHouse.Animals.Clear();
					}
				}
				return true;
			}, includeInteriors: false);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void FixAnimals(string[] command, IGameLogger log)
		{
			bool fixedAny = false;
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				int num = 0;
				foreach (Building building in location.buildings)
				{
					if (building.GetIndoors() is AnimalHouse animalHouse)
					{
						foreach (FarmAnimal animal in animalHouse.animals.Values)
						{
							foreach (Building building2 in location.buildings)
							{
								if (building2.GetIndoors() is AnimalHouse animalHouse2 && animalHouse2.animalsThatLiveHere.Contains(animal.myID.Value) && !building2.Equals(animal.home))
								{
									num += animalHouse2.animalsThatLiveHere.RemoveWhere((long id) => id == animal.myID.Value);
								}
							}
						}
						num += animalHouse.animalsThatLiveHere.RemoveWhere((long id) => Utility.getAnimal(id) == null);
					}
				}
				if (num > 0)
				{
					Game1.playSound("crystal", 0);
					log.Info($"Fixed {num} animals in the '{location.NameOrUniqueName}' location.");
					fixedAny = true;
				}
				return true;
			}, includeInteriors: false);
			if (!fixedAny)
			{
				log.Info("No animal issues found.");
			}
			Utility.fixAllAnimals();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DisplaceAnimals(string[] command, IGameLogger log)
		{
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (location.animals.Length == 0 && location.buildings.Count == 0)
				{
					return true;
				}
				Utility.fixAllAnimals();
				foreach (Building building in location.buildings)
				{
					if (building.GetIndoors() is AnimalHouse animalHouse)
					{
						foreach (FarmAnimal value in animalHouse.animals.Values)
						{
							value.homeInterior = null;
							value.Position = Utility.recursiveFindOpenTileForCharacter(value, location, new Vector2(40f, 40f), 200) * 64f;
							location.animals.TryAdd(value.myID.Value, value);
						}
						animalHouse.animals.Clear();
						animalHouse.animalsThatLiveHere.Clear();
					}
				}
				return true;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "sdkInfo" })]
		public static void SteamInfo(string[] command, IGameLogger log)
		{
			Program.sdk.DebugInfo();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Achieve(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string achievementId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.getSteamAchievement(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ResetAchievements(string[] command, IGameLogger log)
		{
			Program.sdk.ResetAchievements();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Divorce(string[] command, IGameLogger log)
		{
			Game1.player.divorceTonight.Value = true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void BefriendAnimals(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, 1000, "int friendship"))
			{
				LogArgError(log, command, error);
				return;
			}
			foreach (FarmAnimal value2 in Game1.currentLocation.animals.Values)
			{
				value2.friendshipTowardFarmer.Value = value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void PetToFarm(string[] command, IGameLogger log)
		{
			Game1.RequireCharacter<Pet>(Game1.player.getPetName(), mustBeVillager: false).setAtFarmPosition();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void BefriendPets(string[] command, IGameLogger log)
		{
			foreach (NPC allCharacter in Utility.getAllCharacters())
			{
				if (allCharacter is Pet pet)
				{
					pet.friendshipTowardFarmer.Value = 1000;
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Version(string[] command, IGameLogger log)
		{
			log.Info(typeof(Game1).Assembly.GetName().Version?.ToString() ?? "");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "sdlv" })]
		public static void SdlVersion(string[] command, IGameLogger log)
		{
			Type type = Assembly.GetAssembly(GameRunner.instance.Window.GetType())?.GetType("Sdl");
			if ((object)type == null)
			{
				log.Error("Could not find type 'Sdl'");
				return;
			}
			Type type2 = null;
			object obj = null;
			FieldInfo field = type.GetField("version", BindingFlags.Static | BindingFlags.Public);
			if ((object)field == null)
			{
				log.Error("SDL does not have field 'version'");
				return;
			}
			type2 = field.FieldType;
			obj = field.GetValue(null);
			if ((object)type2 == null)
			{
				log.Error("Could not find type 'Sdl::Type'");
				return;
			}
			if (obj == null)
			{
				log.Error("The obtained from from SDL was null");
				return;
			}
			byte[] array = new byte[3];
			string[] array2 = new string[3] { "Major", "Minor", "Patch" };
			for (int i = 0; i < 3; i++)
			{
				string text = array2[i];
				FieldInfo field2 = type2.GetField(text, BindingFlags.Instance | BindingFlags.Public);
				if ((object)field2 == null)
				{
					log.Error("SDL::Version does not have field '" + text + "'");
					return;
				}
				object value = field2.GetValue(obj);
				if (value is byte)
				{
					_ = (byte)value;
					array[i] = (byte)value;
					continue;
				}
				log.Error("SDL::Version field '" + text + "' is not a byte");
				return;
			}
			log.Info($"SDL Version: {array[0]}.{array[1]}.{array[2]}");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "ns" })]
		public static void NoSave(string[] command, IGameLogger log)
		{
			Game1.saveOnNewDay = !Game1.saveOnNewDay;
			if (!Game1.saveOnNewDay)
			{
				Game1.playSound("bigDeSelect");
			}
			else
			{
				Game1.playSound("bigSelect");
			}
			log.Info("Saving is now " + (Game1.saveOnNewDay ? "enabled" : "disabled"));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "rfh" })]
		public static void ReadyForHarvest(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetVector2(command, 1, out var value, out var error, integerOnly: true, "Vector2 tile"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.currentLocation.objects[value].minutesUntilReady.Value = 1;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void BeachBridge(string[] command, IGameLogger log)
		{
			Beach beach = Game1.RequireLocation<Beach>("Beach");
			beach.bridgeFixed.Value = !beach.bridgeFixed.Value;
			if (!beach.bridgeFixed.Value)
			{
				beach.setMapTile(58, 13, 284, "Buildings", "untitled tile sheet");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		/// <remarks>See also <see cref="M:StardewValley.DebugCommands.DefaultHandlers.DaysPlayed(System.String[],StardewValley.Logging.IGameLogger)" />.</remarks>
		public static void Dp(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int daysPlayed"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.stats.DaysPlayed = (uint)value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "fo" })]
		public static void FrameOffset(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int frame") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int offsetX") || !ArgUtility.TryGetInt(command, 3, out var value3, out error, "int offsetY"))
			{
				LogArgError(log, command, error);
				return;
			}
			FarmerRenderer.featureXOffsetPerFrame[value] = (short)value2;
			FarmerRenderer.featureYOffsetPerFrame[value] = (short)value3;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Horse(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, Game1.player.TilePoint.X, "int tileX") || !ArgUtility.TryGetOptionalInt(command, 1, out var value2, out error, Game1.player.TilePoint.Y, "int tileY"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.currentLocation.characters.Add(new Horse(GuidHelper.NewGuid(), value, value2));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Owl(string[] command, IGameLogger log)
		{
			Game1.currentLocation.addOwl();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Pole(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, 0, "int rodLevel"))
			{
				LogArgError(log, command, error);
				return;
			}
			Item item = value switch
			{
				1 => ItemRegistry.Create("(T)TrainingRod"), 
				2 => ItemRegistry.Create("(T)FiberglassRod"), 
				3 => ItemRegistry.Create("(T)IridiumRod"), 
				_ => ItemRegistry.Create("(T)BambooRod"), 
			};
			Game1.player.addItemToInventoryBool(item);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void RemoveQuest(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string questId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.removeQuest(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CompleteQuest(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string questId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.completeQuest(value);
			}
		}

		/// <summary>Set the current player's preferred pet type and breed. This doesn't change any existing pets; see <see cref="M:StardewValley.DebugCommands.DefaultHandlers.ChangePet(System.String[],StardewValley.Logging.IGameLogger)" /> for that.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SetPreferredPet(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string typeId") || !ArgUtility.TryGetOptional(command, 2, out var breedId, out error, null, allowBlank: false, "string breedId"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (!Pet.TryGetData(value, out var data))
			{
				log.Error($"Can't set the player's preferred pet type to '{value}': no such pet type found. Expected one of ['{string.Join("', '", Game1.petData.Keys)}'].");
				return;
			}
			if (breedId != null && data.Breeds.All((PetBreed p) => p.Id != breedId))
			{
				log.Error($"Can't set the player's preferred pet breed to '{breedId}': no such breed found. Expected one of ['{string.Join("', '", data.Breeds.Select((PetBreed p) => p.Id))}'].");
				return;
			}
			bool flag = false;
			if (Game1.player.whichPetType != value)
			{
				log.Info($"Changed preferred pet type from '{Game1.player.whichPetType}' to '{value}'.");
				Game1.player.whichPetType = value;
				flag = true;
				if (breedId == null)
				{
					breedId = data.Breeds.FirstOrDefault()?.Id;
				}
			}
			if (breedId != null && Game1.player.whichPetBreed != breedId)
			{
				log.Info($"Changed preferred pet breed from '{Game1.player.whichPetBreed}' to '{breedId}'.");
				Game1.player.whichPetBreed = breedId;
				flag = true;
			}
			if (!flag)
			{
				log.Info("The player's pet type and breed already match those values.");
			}
		}

		/// <summary>Change the pet type and/or breed for a specific pet. This doesn't change the player's preferred pet type/breed; see <see cref="M:StardewValley.DebugCommands.DefaultHandlers.SetPreferredPet(System.String[],StardewValley.Logging.IGameLogger)" /> for that.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ChangePet(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string petName") || !ArgUtility.TryGet(command, 2, out var value2, out error, allowBlank: false, "string typeId") || !ArgUtility.TryGetOptional(command, 3, out var breedId, out error, null, allowBlank: false, "string breedId"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (!Pet.TryGetData(value2, out var data))
			{
				log.Error($"Can't set the pet type to '{value2}': no such pet type found. Expected one of ['{string.Join("', '", Game1.petData.Keys)}'].");
				return;
			}
			if (breedId != null && data.Breeds.All((PetBreed p) => p.Id != breedId))
			{
				log.Error($"Can't set the pet breed to '{breedId}': no such breed found. Expected one of ['{string.Join("', '", data.Breeds.Select((PetBreed p) => p.Id))}'].");
				return;
			}
			Pet characterFromName = Game1.getCharacterFromName<Pet>(value, mustBeVillager: false);
			if (characterFromName == null)
			{
				log.Error("No pet found with name '" + value + "'.");
				return;
			}
			bool flag = false;
			if (characterFromName.petType.Value != value2)
			{
				log.Info($"Changed {characterFromName.Name}'s type from '{characterFromName.petType.Value}' to '{value2}'.");
				characterFromName.petType.Value = value2;
				flag = true;
				if (breedId == null)
				{
					breedId = data.Breeds.FirstOrDefault()?.Id;
				}
			}
			if (breedId != null && characterFromName.whichBreed.Value != breedId)
			{
				log.Info($"Changed {characterFromName.Name}'s breed from '{characterFromName.whichBreed.Value}' to '{breedId}'.");
				characterFromName.whichBreed.Value = breedId;
				flag = true;
			}
			if (!flag)
			{
				log.Info(characterFromName.Name + "'s type and breed already match those values.");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearCharacters(string[] command, IGameLogger log)
		{
			Game1.currentLocation.characters.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Cat(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetPoint(command, 1, out var value, out var error, "Point tile") || !ArgUtility.TryGetOptional(command, 3, out var value2, out error, "0", allowBlank: false, "string breedId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.currentLocation.characters.Add(new Pet(value.X, value.Y, value2, "Cat"));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Dog(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetPoint(command, 1, out var value, out var error, "Point tile") || !ArgUtility.TryGetOptional(command, 3, out var value2, out error, "0", allowBlank: false, "string breedId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.currentLocation.characters.Add(new Pet(value.X, value.Y, value2, "Dog"));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Quest(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string questId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.addQuest(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DeliveryQuest(string[] command, IGameLogger log)
		{
			Game1.player.questLog.Add(new ItemDeliveryQuest());
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CollectQuest(string[] command, IGameLogger log)
		{
			Game1.player.questLog.Add(new ResourceCollectionQuest());
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SlayQuest(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalBool(command, 1, out var value, out var error, defaultValue: true, "bool ignoreFarmMonsters"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.player.questLog.Add(new SlayMonsterQuest
			{
				ignoreFarmMonsters = { value }
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Quests(string[] command, IGameLogger log)
		{
			foreach (string key in DataLoader.Quests(Game1.content).Keys)
			{
				if (!Game1.player.hasQuest(key))
				{
					Game1.player.addQuest(key);
				}
			}
			Game1.player.questLog.Add(new ItemDeliveryQuest());
			Game1.player.questLog.Add(new SlayMonsterQuest());
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearQuests(string[] command, IGameLogger log)
		{
			Game1.player.questLog.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "fb" })]
		public static void FillBin(string[] command, IGameLogger log)
		{
			IInventory shippingBin = Game1.getFarm().getShippingBin(Game1.player);
			shippingBin.Add(ItemRegistry.Create("(O)24"));
			shippingBin.Add(ItemRegistry.Create("(O)82"));
			shippingBin.Add(ItemRegistry.Create("(O)136"));
			shippingBin.Add(ItemRegistry.Create("(O)16"));
			shippingBin.Add(ItemRegistry.Create("(O)388"));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Gold(string[] command, IGameLogger log)
		{
			Game1.player.Money += 1000000;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearFarm(string[] command, IGameLogger log)
		{
			Farm farm = Game1.getFarm();
			Layer layer = farm.map.Layers[0];
			farm.removeObjectsAndSpawned(0, 0, layer.LayerWidth, layer.LayerHeight);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SetupFarm(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalBool(command, 1, out var value, out var error, defaultValue: false, "bool clearMore"))
			{
				LogArgError(log, command, error);
				return;
			}
			Farm farm = Game1.getFarm();
			Layer layer = farm.map.Layers[0];
			farm.buildings.Clear();
			farm.AddDefaultBuildings();
			farm.removeObjectsAndSpawned(0, 0, layer.LayerWidth, 16 + (value ? 32 : 0));
			farm.removeObjectsAndSpawned(56, 17, 16, 18);
			for (int i = 58; i < 70; i++)
			{
				for (int j = 19; j < 29; j++)
				{
					farm.terrainFeatures.Add(new Vector2(i, j), new HoeDirt());
				}
			}
			if (farm.buildStructure("Coop", new Vector2(52f, 11f), Game1.player, out var constructed))
			{
				constructed.daysOfConstructionLeft.Value = 0;
			}
			if (farm.buildStructure("Silo", new Vector2(36f, 9f), Game1.player, out var constructed2))
			{
				constructed2.daysOfConstructionLeft.Value = 0;
			}
			if (farm.buildStructure("Barn", new Vector2(42f, 10f), Game1.player, out var constructed3))
			{
				constructed3.daysOfConstructionLeft.Value = 0;
			}
			for (int k = 0; k < Game1.player.Items.Count; k++)
			{
				if (Game1.player.Items[k] is Tool tool)
				{
					string text = null;
					switch (tool.QualifiedItemId)
					{
					case "(T)Axe":
					case "(T)CopperAxe":
					case "(T)SteelAxe":
					case "(T)GoldAxe":
						text = "(T)IridiumAxe";
						break;
					case "(T)Hoe":
					case "(T)CopperHoe":
					case "(T)SteelHoe":
					case "(T)GoldHoe":
						text = "(T)IridiumHoe";
						break;
					case "(T)Pickaxe":
					case "(T)GoldPickaxe":
					case "(T)CopperPickaxe":
					case "(T)SteelPickaxe":
						text = "(T)IridiumPickaxe";
						break;
					case "(T)WateringCan":
					case "(T)CopperWateringCan":
					case "(T)SteelWateringCan":
					case "(T)GoldWateringCan":
						text = "(T)IridiumWateringCan";
						break;
					}
					if (text != null)
					{
						Tool tool2 = ItemRegistry.Create<Tool>(text);
						tool2.UpgradeFrom(tool2);
						Game1.player.Items[k] = tool2;
					}
				}
			}
			Game1.player.Money += 20000;
			Game1.player.addItemToInventoryBool(ItemRegistry.Create("(T)Shears"));
			Game1.player.addItemToInventoryBool(ItemRegistry.Create("(T)MilkPail"));
			Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)472", 999));
			Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)473", 999));
			Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)322", 999));
			Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)388", 999));
			Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)390", 999));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void RemoveBuildings(string[] command, IGameLogger log)
		{
			Game1.currentLocation.buildings.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Build(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var buildingType, out var error, allowBlank: false, "string buildingType") || !ArgUtility.TryGetOptionalInt(command, 2, out var value, out error, Game1.player.TilePoint.X + 1, "int x") || !ArgUtility.TryGetOptionalInt(command, 3, out var value2, out error, Game1.player.TilePoint.Y, "int y") || !ArgUtility.TryGetOptionalBool(command, 4, out var value3, out error, ArgUtility.Get(command, 0) == "ForceBuild", "bool forceBuild"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (!Game1.buildingData.ContainsKey(buildingType))
			{
				buildingType = Game1.buildingData.Keys.FirstOrDefault((string key) => buildingType.EqualsIgnoreCase(key)) ?? buildingType;
			}
			Building constructed;
			if (!Game1.buildingData.ContainsKey(buildingType))
			{
				string[] array = Utility.fuzzySearchAll(buildingType, Game1.buildingData.Keys, sortByScore: false).ToArray();
				log.Warn((array.Length == 0) ? ("There's no building with type '" + buildingType + "'.") : ("There's no building with type '" + buildingType + "'. Did you mean one of these?\n- " + string.Join("\n- ", array)));
			}
			else if (!Game1.currentLocation.buildStructure(buildingType, new Vector2(value, value2), Game1.player, out constructed, magicalConstruction: false, value3))
			{
				log.Warn($"Couldn't place a '{buildingType}' building at position ({value}, {value2}).");
			}
			else
			{
				constructed.daysOfConstructionLeft.Value = 0;
				log.Info($"Placed '{buildingType}' at position ({value}, {value2}).");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ForceBuild(string[] command, IGameLogger log)
		{
			if (ArgUtility.HasIndex(command, 0))
			{
				command[0] = "ForceBuild";
			}
			Build(command, log);
		}

		[OtherNames(new string[] { "fab" })]
		public static void FinishAllBuilds(string[] command, IGameLogger log)
		{
			if (!Game1.IsMasterGame)
			{
				log.Error("Only the host can use this command.");
				return;
			}
			int count = 0;
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				foreach (Building building in location.buildings)
				{
					if (building.daysOfConstructionLeft.Value > 0 || building.daysUntilUpgrade.Value > 0)
					{
						building.FinishConstruction();
						int num = count + 1;
						count = num;
					}
				}
				return true;
			});
			log.Info($"Finished constructing {count} building(s).");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void LocalInfo(string[] command, IGameLogger log)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			foreach (TerrainFeature value in Game1.currentLocation.terrainFeatures.Values)
			{
				if (!(value is Grass))
				{
					if (value is Tree)
					{
						num2++;
					}
					else
					{
						num3++;
					}
				}
				else
				{
					num++;
				}
			}
			string text = "Grass:" + num + ",  " + "Trees:" + num2 + ",  " + "Other Terrain Features:" + num3 + ",  " + "Objects: " + Game1.currentLocation.objects.Length + ",  " + "temporarySprites: " + Game1.currentLocation.temporarySprites.Count + ",  ";
			log.Info(text);
			Game1.drawObjectDialogue(text);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "al" })]
		public static void AmbientLight(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int red") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int green") || !ArgUtility.TryGetInt(command, 3, out var value3, out error, "int blue"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.ambientLight = new Color(value, value2, value3);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ResetMines(string[] command, IGameLogger log)
		{
			MineShaft.permanentMineChanges.Clear();
			Game1.playSound("jingle1");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "db" })]
		public static void SpeakTo(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptional(command, 1, out var value, out var error, "Pierre", allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.activeClickableMenu = new DialogueBox(Utility.fuzzyCharacterSearch(value).CurrentDialogue.Peek());
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SkullKey(string[] command, IGameLogger log)
		{
			Game1.player.hasSkullKey = true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void TownKey(string[] command, IGameLogger log)
		{
			Game1.player.HasTownKey = true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Specials(string[] command, IGameLogger log)
		{
			Game1.player.hasRustyKey = true;
			Game1.player.hasSkullKey = true;
			Game1.player.hasSpecialCharm = true;
			Game1.player.hasDarkTalisman = true;
			Game1.player.hasMagicInk = true;
			Game1.player.hasClubCard = true;
			Game1.player.canUnderstandDwarves = true;
			Game1.player.hasMagnifyingGlass = true;
			Game1.player.eventsSeen.Add("2120303");
			Game1.player.eventsSeen.Add("3910979");
			Game1.player.HasTownKey = true;
			Game1.player.stats.Set("trinketSlots", 1);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SkullGear(string[] command, IGameLogger log)
		{
			int num = 36 - Game1.player.MaxItems;
			if (num > 0)
			{
				Game1.player.increaseBackpackSize(num);
			}
			Game1.player.hasSkullKey = true;
			Game1.player.Equip(ItemRegistry.Create<Ring>("(O)527"), Game1.player.leftRing);
			Game1.player.Equip(ItemRegistry.Create<Ring>("(O)523"), Game1.player.rightRing);
			Game1.player.Equip(ItemRegistry.Create<Boots>("(B)514"), Game1.player.boots);
			Game1.player.clearBackpack();
			Game1.player.addItemToInventory(ItemRegistry.Create("(T)IridiumPickaxe"));
			Game1.player.addItemToInventory(ItemRegistry.Create("(W)4"));
			Game1.player.addItemToInventory(ItemRegistry.Create("(O)226", 20));
			Game1.player.addItemToInventory(ItemRegistry.Create("(O)288", 20));
			Game1.player.professions.Add(24);
			Game1.player.maxHealth = 75;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearSpecials(string[] command, IGameLogger log)
		{
			Game1.player.hasRustyKey = false;
			Game1.player.hasSkullKey = false;
			Game1.player.hasSpecialCharm = false;
			Game1.player.hasDarkTalisman = false;
			Game1.player.hasMagicInk = false;
			Game1.player.hasClubCard = false;
			Game1.player.canUnderstandDwarves = false;
			Game1.player.hasMagnifyingGlass = false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Tv(string[] command, IGameLogger log)
		{
			string itemId = Game1.random.Choose("(F)1466", "(F)1468");
			Game1.player.addItemToInventoryBool(ItemRegistry.Create(itemId));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "sn" })]
		public static void SecretNote(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, -1, "int noteId"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.player.hasMagnifyingGlass = true;
			if (value > -1)
			{
				int num = value;
				Object obj = ItemRegistry.Create<Object>("(O)79");
				obj.name = obj.name + " #" + num;
				Game1.player.addItemToInventory(obj);
			}
			else
			{
				Game1.player.addItemToInventory(Game1.currentLocation.tryToCreateUnseenSecretNote(Game1.player));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Child2(string[] command, IGameLogger log)
		{
			Farmer player = Game1.player;
			List<Child> children = player.getChildren();
			if (children.Count > 1)
			{
				children[1].Age++;
				children[1].reloadSprite();
			}
			else
			{
				Utility.getHomeOfFarmer(player).characters.Add(new Child("Baby2", Game1.random.NextBool(), Game1.random.NextBool(), player));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "kid" })]
		public static void Child(string[] command, IGameLogger log)
		{
			Farmer player = Game1.player;
			List<Child> children = player.getChildren();
			if (children.Count > 0)
			{
				children[0].Age++;
				children[0].reloadSprite();
			}
			else
			{
				Utility.getHomeOfFarmer(player).characters.Add(new Child("Baby", Game1.random.NextBool(), Game1.random.NextBool(), player));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void KillAll(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var safeNpcName, out var error, allowBlank: false, "string safeNpcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (!location.Equals(Game1.currentLocation))
				{
					location.characters.Clear();
				}
				else
				{
					location.characters.RemoveWhere((NPC npc) => npc.Name != safeNpcName);
				}
				return true;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ResetWorldState(string[] command, IGameLogger log)
		{
			Game1.worldStateIDs.Clear();
			Game1.netWorldState.Value = new NetWorldState();
			Game1.game1.parseDebugInput("DeleteArch", log);
			Game1.player.mailReceived.Clear();
			Game1.player.eventsSeen.Clear();
			Game1.eventsSeenSinceLastLocationChange.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void KillAllHorses(string[] command, IGameLogger log)
		{
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (location.characters.RemoveWhere((NPC npc) => npc is Horse) > 0)
				{
					Game1.playSound("drumkit0");
				}
				return true;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DatePlayer(string[] command, IGameLogger log)
		{
			foreach (Farmer allFarmer in Game1.getAllFarmers())
			{
				if (allFarmer != Game1.player && allFarmer.isCustomized.Value)
				{
					Game1.player.team.GetFriendship(Game1.player.UniqueMultiplayerID, allFarmer.UniqueMultiplayerID).Status = FriendshipStatus.Dating;
					break;
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void EngagePlayer(string[] command, IGameLogger log)
		{
			foreach (Farmer allFarmer in Game1.getAllFarmers())
			{
				if (allFarmer != Game1.player && allFarmer.isCustomized.Value)
				{
					Friendship friendship = Game1.player.team.GetFriendship(Game1.player.UniqueMultiplayerID, allFarmer.UniqueMultiplayerID);
					friendship.Status = FriendshipStatus.Engaged;
					friendship.WeddingDate = Game1.Date;
					friendship.WeddingDate.TotalDays++;
					break;
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MarryPlayer(string[] command, IGameLogger log)
		{
			foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
			{
				if (onlineFarmer != Game1.player && onlineFarmer.isCustomized.Value)
				{
					Friendship friendship = Game1.player.team.GetFriendship(Game1.player.UniqueMultiplayerID, onlineFarmer.UniqueMultiplayerID);
					friendship.Status = FriendshipStatus.Married;
					friendship.WeddingDate = Game1.Date;
					break;
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Marry(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value);
			if (nPC == null)
			{
				log.Error("No character found matching '" + value + "'.");
				return;
			}
			if (!Game1.player.friendshipData.TryGetValue(nPC.Name, out var value2))
			{
				value2 = (Game1.player.friendshipData[nPC.Name] = new Friendship());
			}
			Game1.player.changeFriendship(2500, nPC);
			Game1.player.spouse = nPC.Name;
			value2.WeddingDate = new WorldDate(Game1.Date);
			value2.Status = FriendshipStatus.Married;
			Game1.prepareSpouseForWedding(Game1.player);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Engaged(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value);
			if (nPC == null)
			{
				log.Error("No character found matching '" + value + "'.");
				return;
			}
			if (!Game1.player.friendshipData.TryGetValue(nPC.Name, out var value2))
			{
				value2 = (Game1.player.friendshipData[nPC.Name] = new Friendship());
			}
			Game1.player.changeFriendship(2500, nPC);
			Game1.player.spouse = nPC.Name;
			value2.Status = FriendshipStatus.Engaged;
			WorldDate date = Game1.Date;
			date.TotalDays++;
			value2.WeddingDate = date;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearLightGlows(string[] command, IGameLogger log)
		{
			Game1.currentLocation.lightGlows.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "wp" })]
		public static void Wallpaper(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, -1, "int wallpaperId"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (value > -1)
			{
				Game1.player.addItemToInventoryBool(new Wallpaper(value));
				return;
			}
			bool flag = Game1.random.NextBool();
			Game1.player.addItemToInventoryBool(new Wallpaper(flag ? Game1.random.Next(40) : Game1.random.Next(112), flag));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearFurniture(string[] command, IGameLogger log)
		{
			Game1.currentLocation.furniture.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "ff" })]
		public static void Furniture(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptional(command, 1, out var value, out var error, null, allowBlank: false, "string furnitureId"))
			{
				LogArgError(log, command, error);
			}
			else if (value == null)
			{
				Item item = null;
				while (item == null)
				{
					try
					{
						item = ItemRegistry.Create("(F)" + Game1.random.Next(1613));
					}
					catch
					{
					}
				}
				Game1.player.addItemToInventoryBool(item);
			}
			else
			{
				Game1.player.addItemToInventoryBool(ItemRegistry.Create("(F)" + value));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SpawnCoopsAndBarns(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int count"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				if (!(Game1.currentLocation is Farm farm))
				{
					return;
				}
				for (int i = 0; i < value; i++)
				{
					for (int j = 0; j < 20; j++)
					{
						bool flag = Game1.random.NextBool();
						if (farm.buildStructure(flag ? "Deluxe Coop" : "Deluxe Barn", farm.getRandomTile(), Game1.player, out var constructed))
						{
							constructed.daysOfConstructionLeft.Value = 0;
							constructed.doAction(Utility.PointToVector2(constructed.animalDoor.Value) + new Vector2(constructed.tileX.Value, constructed.tileY.Value), Game1.player);
							for (int k = 0; k < 16; k++)
							{
								Utility.addAnimalToFarm(new FarmAnimal(flag ? "White Chicken" : "Cow", Game1.random.Next(int.MaxValue), Game1.player.UniqueMultiplayerID));
							}
							break;
						}
					}
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SetupFishPondFarm(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, 10, "int population"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.game1.parseDebugInput("ClearFarm", log);
			for (int i = 4; i < 77; i += 6)
			{
				for (int j = 9; j < 60; j += 6)
				{
					Game1.game1.parseDebugInput($"{"Build"} \"Fish Pond\" {i} {j}", log);
				}
			}
			foreach (Building building in Game1.getFarm().buildings)
			{
				if (building is FishPond fishPond)
				{
					int num = Game1.random.Next(128, 159);
					if (Game1.random.NextDouble() < 0.15)
					{
						num = Game1.random.Next(698, 724);
					}
					if (Game1.random.NextDouble() < 0.05)
					{
						num = Game1.random.Next(796, 801);
					}
					ParsedItemData data = ItemRegistry.GetData(num.ToString());
					if (data != null && data.Category == -4)
					{
						fishPond.fishType.Value = num.ToString();
					}
					else
					{
						fishPond.fishType.Value = Game1.random.Choose("393", "397");
					}
					fishPond.maxOccupants.Value = 10;
					fishPond.currentOccupants.Value = value;
					fishPond.GetFishObject();
				}
			}
			Game1.game1.parseDebugInput("DayUpdate 1", log);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Grass(string[] command, IGameLogger log)
		{
			GameLocation currentLocation = Game1.currentLocation;
			if (currentLocation == null)
			{
				return;
			}
			for (int i = 0; i < currentLocation.Map.Layers[0].LayerWidth; i++)
			{
				for (int j = 0; j < currentLocation.Map.Layers[0].LayerHeight; j++)
				{
					if (currentLocation.CanItemBePlacedHere(new Vector2(i, j), itemIsPassable: true, CollisionMask.All, CollisionMask.None))
					{
						currentLocation.terrainFeatures.Add(new Vector2(i, j), new Grass(1, 4));
					}
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SetupBigFarm(string[] command, IGameLogger log)
		{
			Farm farm = Game1.getFarm();
			Game1.game1.parseDebugInput("ClearFarm", log);
			Game1.game1.parseDebugInput("Build \"Deluxe Coop\" 4 9", log);
			Game1.game1.parseDebugInput("Build \"Deluxe Coop\" 10 9", log);
			Game1.game1.parseDebugInput("Build \"Deluxe Coop\" 36 11", log);
			Game1.game1.parseDebugInput("Build \"Deluxe Barn\" 16 9", log);
			Game1.game1.parseDebugInput("Build \"Deluxe Barn\" 3 16", log);
			Game1.game1.parseDebugInput("Build Mill 30 20", log);
			Game1.game1.parseDebugInput("Build Stable 46 10", log);
			Game1.game1.parseDebugInput("Build Silo 54 14", log);
			Game1.game1.parseDebugInput("Build \"Junimo Hut\" 48 52", log);
			Game1.game1.parseDebugInput("Build \"Junimo Hut\" 55 52", log);
			Game1.game1.parseDebugInput("Build \"Junimo Hut\" 59 52", log);
			Game1.game1.parseDebugInput("Build \"Junimo Hut\" 65 52", log);
			foreach (Building building in farm.buildings)
			{
				if (!(building.GetIndoors() is AnimalHouse animalHouse))
				{
					continue;
				}
				BuildingData buildingData = building.GetData();
				string[] options = (from p in Game1.farmAnimalData
					where p.Value.House != null && buildingData.ValidOccupantTypes.Contains(p.Value.House)
					select p.Key).ToArray();
				for (int num = 0; num < animalHouse.animalLimit.Value; num++)
				{
					if (animalHouse.isFull())
					{
						break;
					}
					FarmAnimal farmAnimal = new FarmAnimal(Game1.random.ChooseFrom(options), Game1.random.Next(int.MaxValue), Game1.player.UniqueMultiplayerID);
					if (Game1.random.NextBool())
					{
						farmAnimal.growFully();
					}
					animalHouse.adoptAnimal(farmAnimal);
				}
			}
			foreach (Building building2 in farm.buildings)
			{
				building2.doAction(Utility.PointToVector2(building2.animalDoor.Value) + new Vector2(building2.tileX.Value, building2.tileY.Value), Game1.player);
			}
			for (int num2 = 11; num2 < 23; num2++)
			{
				for (int num3 = 14; num3 < 25; num3++)
				{
					farm.terrainFeatures.Add(new Vector2(num2, num3), new Grass(1, 4));
				}
			}
			for (int num4 = 3; num4 < 23; num4++)
			{
				for (int num5 = 57; num5 < 61; num5++)
				{
					farm.terrainFeatures.Add(new Vector2(num4, num5), new Grass(1, 4));
				}
			}
			for (int num6 = 17; num6 < 25; num6++)
			{
				farm.terrainFeatures.Add(new Vector2(64f, num6), new Flooring("6"));
			}
			for (int num7 = 35; num7 < 64; num7++)
			{
				farm.terrainFeatures.Add(new Vector2(num7, 24f), new Flooring("6"));
			}
			for (int num8 = 38; num8 < 76; num8++)
			{
				for (int num9 = 18; num9 < 52; num9++)
				{
					if (farm.CanItemBePlacedHere(new Vector2(num8, num9), itemIsPassable: true, CollisionMask.All, CollisionMask.None))
					{
						HoeDirt hoeDirt = new HoeDirt();
						farm.terrainFeatures.Add(new Vector2(num8, num9), hoeDirt);
						hoeDirt.plant((472 + Game1.random.Next(5)).ToString(), Game1.player, isFertilizer: false);
					}
				}
			}
			Game1.game1.parseDebugInput("GrowCrops 8", log);
			Vector2[] obj = new Vector2[18]
			{
				new Vector2(8f, 25f),
				new Vector2(11f, 25f),
				new Vector2(14f, 25f),
				new Vector2(17f, 25f),
				new Vector2(20f, 25f),
				new Vector2(23f, 25f),
				new Vector2(8f, 28f),
				new Vector2(11f, 28f),
				new Vector2(14f, 28f),
				new Vector2(17f, 28f),
				new Vector2(20f, 28f),
				new Vector2(23f, 28f),
				new Vector2(8f, 31f),
				new Vector2(11f, 31f),
				new Vector2(14f, 31f),
				new Vector2(17f, 31f),
				new Vector2(20f, 31f),
				new Vector2(23f, 31f)
			};
			NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>> terrainFeatures = farm.terrainFeatures;
			Vector2[] array = obj;
			foreach (Vector2 key in array)
			{
				terrainFeatures.Add(key, new FruitTree((628 + Game1.random.Next(2)).ToString(), 4));
			}
			for (int num11 = 3; num11 < 15; num11++)
			{
				for (int num12 = 36; num12 < 45; num12++)
				{
					if (farm.CanItemBePlacedHere(new Vector2(num11, num12)))
					{
						Object obj2 = ItemRegistry.Create<Object>("(BC)12");
						farm.objects.Add(new Vector2(num11, num12), obj2);
						obj2.performObjectDropInAction(ItemRegistry.Create<Object>("(O)454"), probe: false, Game1.player);
					}
				}
			}
			for (int num13 = 16; num13 < 26; num13++)
			{
				for (int num14 = 36; num14 < 45; num14++)
				{
					if (farm.CanItemBePlacedHere(new Vector2(num13, num14)))
					{
						farm.objects.Add(new Vector2(num13, num14), ItemRegistry.Create<Object>("(BC)13"));
					}
				}
			}
			for (int num15 = 3; num15 < 15; num15++)
			{
				for (int num16 = 47; num16 < 57; num16++)
				{
					if (farm.CanItemBePlacedHere(new Vector2(num15, num16)))
					{
						farm.objects.Add(new Vector2(num15, num16), ItemRegistry.Create<Object>("(BC)16"));
					}
				}
			}
			for (int num17 = 16; num17 < 26; num17++)
			{
				for (int num18 = 47; num18 < 57; num18++)
				{
					if (farm.CanItemBePlacedHere(new Vector2(num17, num18)))
					{
						farm.objects.Add(new Vector2(num17, num18), ItemRegistry.Create<Object>("(BC)15"));
					}
				}
			}
			for (int num19 = 28; num19 < 38; num19++)
			{
				for (int num20 = 26; num20 < 46; num20++)
				{
					if (farm.CanItemBePlacedHere(new Vector2(num19, num20)))
					{
						new Torch().placementAction(farm, num19 * 64, num20 * 64, null);
					}
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "hu", "house" })]
		public static void HouseUpgrade(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int upgradeLevel"))
			{
				LogArgError(log, command, error);
				return;
			}
			Utility.getHomeOfFarmer(Game1.player).moveObjectsForHouseUpgrade(value);
			Utility.getHomeOfFarmer(Game1.player).setMapForUpgradeLevel(value);
			Game1.player.HouseUpgradeLevel = value;
			Game1.addNewFarmBuildingMaps();
			Utility.getHomeOfFarmer(Game1.player).ReadWallpaperAndFloorTileData();
			Utility.getHomeOfFarmer(Game1.player).RefreshFloorObjectNeighbors();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "thu", "thishouse" })]
		public static void ThisHouseUpgrade(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int upgradeLevel"))
			{
				LogArgError(log, command, error);
				return;
			}
			FarmHouse farmHouse = (Game1.currentLocation?.getBuildingAt(Game1.player.Tile + new Vector2(0f, -1f))?.GetIndoors() as FarmHouse) ?? (Game1.currentLocation as FarmHouse);
			if (farmHouse != null)
			{
				farmHouse.moveObjectsForHouseUpgrade(value);
				farmHouse.setMapForUpgradeLevel(value);
				farmHouse.upgradeLevel = value;
				Game1.addNewFarmBuildingMaps();
				farmHouse.ReadWallpaperAndFloorTileData();
				farmHouse.RefreshFloorObjectNeighbors();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "ci" })]
		public static void Clear(string[] command, IGameLogger log)
		{
			Game1.player.clearBackpack();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "w" })]
		public static void Wall(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string wallpaperId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.RequireLocation<FarmHouse>("FarmHouse").SetWallpaper(value, null);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Floor(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string floorId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.RequireLocation<FarmHouse>("FarmHouse").SetFloor(value, null);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Sprinkle(string[] command, IGameLogger log)
		{
			Utility.addSprinklesToLocation(Game1.currentLocation, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, 7, 7, 2000, 100, Color.White);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearMail(string[] command, IGameLogger log)
		{
			Game1.player.mailReceived.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void BroadcastMailbox(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string mailId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.addMail(value, noLetter: false, sendToEveryone: true);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "mft" })]
		public static void MailForTomorrow(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string mailId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.addMailForTomorrow(value, command.Length > 2);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void AllMail(string[] command, IGameLogger log)
		{
			foreach (string key in DataLoader.Mail(Game1.content).Keys)
			{
				Game1.addMailForTomorrow(key);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void AllMailRead(string[] command, IGameLogger log)
		{
			foreach (string key in DataLoader.Mail(Game1.content).Keys)
			{
				Game1.player.mailReceived.Add(key);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ShowMail(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string mailId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.activeClickableMenu = new LetterViewerMenu(DataLoader.Mail(Game1.content).GetValueOrDefault(value, ""), value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "where" })]
		public static void WhereIs(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var npcName, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			List<string> lines = new List<string>();
			if (Game1.CurrentEvent != null)
			{
				foreach (NPC actor in Game1.CurrentEvent.actors)
				{
					if (Utility.fuzzyCompare(npcName, actor.Name).HasValue)
					{
						lines.Add($"{actor.Name} is in this event at ({actor.TilePoint.X}, {actor.TilePoint.Y})");
					}
				}
			}
			Utility.ForEachCharacter(delegate(NPC character)
			{
				if (Utility.fuzzyCompare(npcName, character.Name).HasValue)
				{
					lines.Add($"'{character.Name}'{(character.EventActor ? " (event actor)" : "")} is at {character.currentLocation.NameOrUniqueName} ({character.TilePoint.X}, {character.TilePoint.Y})");
				}
				return true;
			}, includeEventActors: true);
			if (lines.Any())
			{
				log.Info(string.Join("\n", lines));
			}
			else
			{
				log.Error("No NPC found matching '" + npcName + "'.");
			}
		}

		/// <summary>List the locations of every item in the game state matching a given item ID or name.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "whereItem" })]
		public static void WhereIsItem(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var itemNameOrId, out var error, allowBlank: false, "string itemNameOrId"))
			{
				LogArgError(log, command, error);
				return;
			}
			string itemId = ItemRegistry.GetData(itemNameOrId)?.QualifiedItemId;
			List<string> lines = new List<string>();
			long count = 0L;
			Utility.ForEachItemContext(delegate(in ForEachItemContext context)
			{
				Item item = context.Item;
				bool num;
				if (itemId == null)
				{
					if (Utility.fuzzyCompare(itemNameOrId, item.Name).HasValue)
					{
						goto IL_005e;
					}
					num = Utility.fuzzyCompare(itemNameOrId, item.DisplayName).HasValue;
				}
				else
				{
					num = item.QualifiedItemId == itemId;
				}
				if (num)
				{
					goto IL_005e;
				}
				goto IL_011c;
				IL_005e:
				count += Math.Min(item.Stack, 1);
				lines.Add($"  - {string.Join(" > ", context.GetDisplayPath(includeItem: true))} ({item.QualifiedItemId}{((item.Stack > 1) ? $" x {item.Stack}" : "")})");
				goto IL_011c;
				IL_011c:
				return true;
			});
			string text = ((itemId != null) ? ("ID '" + itemId + "'") : ("name '" + itemNameOrId + "'"));
			if (lines.Any())
			{
				log.Info($"Found {count} item{((count > 1) ? "s" : "")} matching {text}:\n{string.Join("\n", lines)}");
			}
			else
			{
				log.Error("No item found matching " + text + ".");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "pm" })]
		public static void PanMode(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptional(command, 1, out var value, out var error, null, allowBlank: false, "string option"))
			{
				LogArgError(log, command, error);
			}
			else if (value == null)
			{
				if (!Game1.panMode)
				{
					Game1.panMode = true;
					Game1.viewportFreeze = true;
					Game1.debugMode = true;
					Game1.game1.panFacingDirectionWait = false;
					Game1.game1.panModeString = "";
					log.Info("Screen pan mode enabled.");
				}
				else
				{
					Game1.panMode = false;
					Game1.viewportFreeze = false;
					Game1.game1.panModeString = "";
					Game1.debugMode = false;
					Game1.game1.panFacingDirectionWait = false;
					Game1.inputSimulator = null;
					log.Info("Screen pan mode disabled.");
				}
			}
			else if (Game1.panMode)
			{
				int value2;
				string error2;
				if (value == "clear")
				{
					Game1.game1.panModeString = "";
					Game1.game1.panFacingDirectionWait = false;
				}
				else if (ArgUtility.TryGetInt(command, 1, out value2, out error2, "int time"))
				{
					if (!Game1.game1.panFacingDirectionWait)
					{
						Game1 game = Game1.game1;
						game.panModeString = game.panModeString + ((Game1.game1.panModeString.Length > 0) ? "/" : "") + value2 + " ";
						log.Info(Game1.game1.panModeString + Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3191"));
					}
				}
				else
				{
					LogArgError(log, command, "the first argument must be omitted (to toggle pan mode), 'clear', or a numeric time");
				}
			}
			else
			{
				log.Error("Screen pan mode isn't enabled. You can enable it by using this command without arguments.");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "is" })]
		public static void InputSim(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string option"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.inputSimulator = null;
			string text = value.ToLower();
			if (!(text == "spamtool"))
			{
				if (text == "spamlr")
				{
					Game1.inputSimulator = new LeftRightClickSpamInputSimulator();
				}
				else
				{
					log.Error("No input simulator found for " + value);
				}
			}
			else
			{
				Game1.inputSimulator = new ToolSpamInputSimulator();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Hurry(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Utility.fuzzyCharacterSearch(value).warpToPathControllerDestination();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MorePollen(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int amount"))
			{
				LogArgError(log, command, error);
				return;
			}
			for (int i = 0; i < value; i++)
			{
				Game1.debrisWeather.Add(new WeatherDebris(new Vector2(Game1.random.Next(0, Game1.graphics.GraphicsDevice.Viewport.Width), Game1.random.Next(0, Game1.graphics.GraphicsDevice.Viewport.Height)), 0, (float)Game1.random.Next(15) / 500f, (float)Game1.random.Next(-10, 0) / 50f, (float)Game1.random.Next(10) / 50f));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void FillWithObject(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string id") || !ArgUtility.TryGetOptionalBool(command, 2, out var value2, out error, defaultValue: false, "bool bigCraftable"))
			{
				LogArgError(log, command, error);
				return;
			}
			for (int i = 0; i < Game1.currentLocation.map.Layers[0].LayerHeight; i++)
			{
				for (int j = 0; j < Game1.currentLocation.map.Layers[0].LayerWidth; j++)
				{
					Vector2 vector = new Vector2(j, i);
					if (Game1.currentLocation.CanItemBePlacedHere(vector))
					{
						string text = (value2 ? "(BC)" : "(O)");
						Game1.currentLocation.setObject(vector, ItemRegistry.Create<Object>(text + value));
					}
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SpawnWeeds(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int spawnPasses"))
			{
				LogArgError(log, command, error);
				return;
			}
			for (int i = 0; i < value; i++)
			{
				Game1.currentLocation.spawnWeedsAndStones(1);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void BusDriveBack(string[] command, IGameLogger log)
		{
			Game1.RequireLocation<BusStop>("BusStop").busDriveBack();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void BusDriveOff(string[] command, IGameLogger log)
		{
			Game1.RequireLocation<BusStop>("BusStop").busDriveOff();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CompleteJoja(string[] command, IGameLogger log)
		{
			Game1.player.mailReceived.Add("ccCraftsRoom");
			Game1.player.mailReceived.Add("ccVault");
			Game1.player.mailReceived.Add("ccFishTank");
			Game1.player.mailReceived.Add("ccBoilerRoom");
			Game1.player.mailReceived.Add("ccPantry");
			Game1.player.mailReceived.Add("jojaCraftsRoom");
			Game1.player.mailReceived.Add("jojaVault");
			Game1.player.mailReceived.Add("jojaFishTank");
			Game1.player.mailReceived.Add("jojaBoilerRoom");
			Game1.player.mailReceived.Add("jojaPantry");
			Game1.player.mailReceived.Add("JojaMember");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CompleteCc(string[] command, IGameLogger log)
		{
			Game1.player.mailReceived.Add("ccCraftsRoom");
			Game1.player.mailReceived.Add("ccVault");
			Game1.player.mailReceived.Add("ccFishTank");
			Game1.player.mailReceived.Add("ccBoilerRoom");
			Game1.player.mailReceived.Add("ccPantry");
			Game1.player.mailReceived.Add("ccBulletin");
			Game1.player.mailReceived.Add("ccBoilerRoom");
			Game1.player.mailReceived.Add("ccPantry");
			Game1.player.mailReceived.Add("ccBulletin");
			CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
			for (int i = 0; i < communityCenter.areasComplete.Count; i++)
			{
				communityCenter.markAreaAsComplete(i);
				communityCenter.areasComplete[i] = true;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Break(string[] command, IGameLogger log)
		{
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void WhereOre(string[] command, IGameLogger log)
		{
			log.Info(Convert.ToString(Game1.currentLocation.orePanPoint.Value));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void AllBundles(string[] command, IGameLogger log)
		{
			foreach (KeyValuePair<int, NetArray<bool, NetBool>> item in Game1.RequireLocation<CommunityCenter>("CommunityCenter").bundles.FieldDict)
			{
				for (int i = 0; i < item.Value.Count; i++)
				{
					item.Value[i] = true;
				}
			}
			Game1.playSound("crystal", 0);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void JunimoGoodbye(string[] command, IGameLogger log)
		{
			if (!(Game1.currentLocation is CommunityCenter communityCenter))
			{
				log.Error("The JunimoGoodbye command must be run while inside the community center.");
			}
			else
			{
				communityCenter.junimoGoodbyeDance();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Bundle(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int bundleKey"))
			{
				LogArgError(log, command, error);
				return;
			}
			foreach (KeyValuePair<int, NetArray<bool, NetBool>> item in Game1.RequireLocation<CommunityCenter>("CommunityCenter").bundles.FieldDict)
			{
				if (item.Key == value)
				{
					for (int i = 0; i < item.Value.Count; i++)
					{
						item.Value[i] = true;
					}
				}
			}
			Game1.playSound("crystal", 0);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "lu" })]
		public static void Lookup(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetRemainder(command, 1, out var value, out var error, ' ', "string search"))
			{
				LogArgError(log, command, error);
				return;
			}
			foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
			{
				if (allDatum.InternalName.EqualsIgnoreCase(value))
				{
					log.Info(allDatum.InternalName + " " + allDatum.ItemId);
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CcLoadCutscene(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int areaId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.RequireLocation<CommunityCenter>("CommunityCenter").restoreAreaCutscene(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CcLoad(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int areaId"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.RequireLocation<CommunityCenter>("CommunityCenter").loadArea(value);
			Game1.RequireLocation<CommunityCenter>("CommunityCenter").markAreaAsComplete(value);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Plaque(string[] command, IGameLogger log)
		{
			Game1.RequireLocation<CommunityCenter>("CommunityCenter").addStarToPlaque();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void JunimoStar(string[] command, IGameLogger log)
		{
			CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
			Junimo junimo = communityCenter.characters.OfType<Junimo>().FirstOrDefault();
			if (junimo == null)
			{
				log.Error("No Junimo found in the community center.");
			}
			else
			{
				junimo.returnToJunimoHutToFetchStar(communityCenter);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "j", "aj" })]
		public static void AddJunimo(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetVector2(command, 1, out var value, out var error, integerOnly: true, "Vector2 tile") || !ArgUtility.TryGetInt(command, 3, out var value2, out error, "int areaId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.RequireLocation<CommunityCenter>("CommunityCenter").addCharacter(new Junimo(value * 64f, value2));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ResetJunimoNotes(string[] command, IGameLogger log)
		{
			foreach (NetArray<bool, NetBool> value in Game1.RequireLocation<CommunityCenter>("CommunityCenter").bundles.FieldDict.Values)
			{
				for (int i = 0; i < value.Count; i++)
				{
					value[i] = false;
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "jn" })]
		public static void JunimoNote(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int areaId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.RequireLocation<CommunityCenter>("CommunityCenter").addJunimoNote(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void WaterColor(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int red") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int green") || !ArgUtility.TryGetInt(command, 3, out var value3, out error, "int blue"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.currentLocation.waterColor.Value = new Color(value, value2, value3) * 0.5f;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void FestivalScore(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int score"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.festivalScore += value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void AddOtherFarmer(string[] command, IGameLogger log)
		{
			Farmer farmer = new Farmer(new FarmerSprite("Characters\\Farmer\\farmer_base"), new Vector2(Game1.player.Position.X - 64f, Game1.player.Position.Y), 2, Dialogue.randomName(), null, isMale: true);
			farmer.changeShirt(Game1.random.Next(1000, 1040).ToString());
			farmer.changePantsColor(new Color(Game1.random.Next(255), Game1.random.Next(255), Game1.random.Next(255)));
			farmer.changeHairStyle(Game1.random.Next(FarmerRenderer.hairStylesTexture.Height / 96 * 8));
			if (Game1.random.NextBool())
			{
				farmer.changeHat(Game1.random.Next(-1, FarmerRenderer.hatsTexture.Height / 80 * 12));
			}
			else
			{
				Game1.player.changeHat(-1);
			}
			farmer.changeHairColor(new Color(Game1.random.Next(255), Game1.random.Next(255), Game1.random.Next(255)));
			farmer.changeSkinColor(Game1.random.Next(16));
			farmer.currentLocation = Game1.currentLocation;
			Game1.otherFarmers.Add(Game1.random.Next(), farmer);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void PlayMusic(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string trackName"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.changeMusicTrack(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Jump(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string target") || !ArgUtility.TryGetOptionalFloat(command, 2, out var value2, out error, 8f, "float jumpVelocity"))
			{
				LogArgError(log, command, error);
			}
			else if (value == "farmer")
			{
				Game1.player.jump(value2);
			}
			else
			{
				Utility.fuzzyCharacterSearch(value).jump(value2);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Toss(string[] command, IGameLogger log)
		{
			Game1.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(738, 2700f, 1, 0, Game1.player.Tile * 64f, flicker: false, flipped: false)
			{
				rotationChange = (float)Math.PI / 32f,
				motion = new Vector2(0f, -6f),
				acceleration = new Vector2(0f, 0.08f)
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Rain(string[] command, IGameLogger log)
		{
			string locationContextId = Game1.player.currentLocation.GetLocationContextId();
			LocationWeather weatherForLocation = Game1.netWorldState.Value.GetWeatherForLocation(locationContextId);
			weatherForLocation.IsRaining = !weatherForLocation.IsRaining;
			weatherForLocation.IsDebrisWeather = false;
			if (locationContextId == "Default")
			{
				Game1.isRaining = weatherForLocation.IsRaining;
				Game1.isDebrisWeather = false;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void GreenRain(string[] command, IGameLogger log)
		{
			string locationContextId = Game1.player.currentLocation.GetLocationContextId();
			LocationWeather weatherForLocation = Game1.netWorldState.Value.GetWeatherForLocation(locationContextId);
			weatherForLocation.IsGreenRain = !weatherForLocation.IsGreenRain;
			weatherForLocation.IsDebrisWeather = false;
			if (locationContextId == "Default")
			{
				Game1.isRaining = weatherForLocation.IsRaining;
				Game1.isGreenRain = weatherForLocation.IsGreenRain;
				Game1.isDebrisWeather = false;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "sf" })]
		public static void SetFrame(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int animationId"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.player.FarmerSprite.PauseForSingleAnimation = true;
			Game1.player.FarmerSprite.setCurrentSingleAnimation(value);
		}

		/// <summary>Immediately end the current event.</summary>
		[OtherNames(new string[] { "ee" })]
		public static void EndEvent(string[] command, IGameLogger log)
		{
			Event currentEvent = Game1.CurrentEvent;
			if (currentEvent == null)
			{
				log.Warn("Can't end an event because there's none playing.");
				return;
			}
			if (currentEvent.id == "1590166")
			{
				Game1.player.mailReceived.Add("rejectedPet");
			}
			currentEvent.skipped = true;
			currentEvent.skipEvent();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Language(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new LanguageSelectionMenu();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "rte" })]
		public static void RunTestEvent(string[] command, IGameLogger log)
		{
			Game1.runTestEvent();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "qb" })]
		public static void QiBoard(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new SpecialOrdersBoard("Qi");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "ob" })]
		public static void OrdersBoard(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new SpecialOrdersBoard();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ReturnedDonations(string[] command, IGameLogger log)
		{
			Game1.player.team.CheckReturnedDonations();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "cso" })]
		public static void CompleteSpecialOrders(string[] command, IGameLogger log)
		{
			foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
			{
				foreach (OrderObjective objective in specialOrder.objectives)
				{
					objective.SetCount(objective.maxCount.Value);
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SpecialOrder(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string orderId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.team.AddSpecialOrder(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void BoatJourney(string[] command, IGameLogger log)
		{
			Game1.currentMinigame = new BoatJourney();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Minigame(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string minigame"))
			{
				LogArgError(log, command, error);
				return;
			}
			switch (value)
			{
			case "cowboy":
				Game1.updateViewportForScreenSizeChange(fullscreenChange: false, Game1.graphics.PreferredBackBufferWidth, Game1.graphics.PreferredBackBufferHeight);
				Game1.currentMinigame = new AbigailGame();
				break;
			case "blastoff":
				Game1.currentMinigame = new RobotBlastoff();
				break;
			case "minecart":
				Game1.currentMinigame = new MineCart(0, 3);
				break;
			case "grandpa":
				Game1.currentMinigame = new GrandpaStory();
				break;
			case "marucomet":
				Game1.currentMinigame = new MaruComet();
				break;
			case "haleyCows":
				Game1.currentMinigame = new HaleyCowPictures();
				break;
			case "plane":
				Game1.currentMinigame = new PlaneFlyBy();
				break;
			case "slots":
				Game1.currentMinigame = new Slots();
				break;
			case "target":
				Game1.currentMinigame = new TargetGame();
				break;
			case "fishing":
				Game1.currentMinigame = new FishingGame();
				break;
			case "intro":
				Game1.currentMinigame = new Intro();
				break;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Event(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string locationName") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int eventIndex") || !ArgUtility.TryGetOptionalBool(command, 3, out var value3, out error, defaultValue: true, "bool clearEventsSeen"))
			{
				LogArgError(log, command, error);
				return;
			}
			GameLocation gameLocation = Utility.fuzzyLocationSearch(value);
			if (gameLocation == null)
			{
				log.Error("No location with name " + value);
				return;
			}
			value = gameLocation.Name;
			if (value == "Pool")
			{
				value = "BathHouse_Pool";
			}
			if (value3)
			{
				Game1.player.eventsSeen.Clear();
			}
			string assetName = "Data\\Events\\" + value;
			KeyValuePair<string, string> entry = Game1.content.Load<Dictionary<string, string>>(assetName).ElementAt(value2);
			if (entry.Key.Contains('/'))
			{
				LocationRequest locationRequest = Game1.getLocationRequest(value);
				locationRequest.OnLoad += delegate
				{
					Game1.currentLocation.currentEvent = new Event(entry.Value, assetName, StardewValley.Event.SplitPreconditions(entry.Key)[0]);
				};
				Game1.warpFarmer(locationRequest, 8, 8, Game1.player.FacingDirection);
			}
		}

		/// <summary>Find an event by ID and play it.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "ebi" })]
		public static void EventById(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string eventId"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.player.eventsSeen.Remove(value);
			Game1.eventsSeenSinceLastLocationChange.Remove(value);
			if (Game1.PlayEvent(value, checkPreconditions: false, checkSeen: false))
			{
				log.Info("Starting event " + value);
			}
			else
			{
				log.Error("Event '" + value + "' not found.");
			}
		}

		public static void EventScript(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: true, "string location") || !ArgUtility.TryGetRemainder(command, 2, out var script, out error, ' ', "string script"))
			{
				LogArgError(log, command, error);
			}
			else if (value != Game1.currentLocation.Name)
			{
				LocationRequest locationRequest = Game1.getLocationRequest(value);
				locationRequest.OnLoad += delegate
				{
					Game1.currentLocation.currentEvent = new Event(script);
				};
				int x = 8;
				int y = 8;
				Utility.getDefaultWarpLocation(locationRequest.Name, ref x, ref y);
				Game1.warpFarmer(locationRequest, x, y, Game1.player.FacingDirection);
			}
			else
			{
				Game1.globalFadeToBlack(delegate
				{
					Game1.forceSnapOnNextViewportUpdate = true;
					Game1.currentLocation.startEvent(new Event(script));
					Game1.globalFadeToClear();
				});
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "sfe" })]
		public static void SetFarmEvent(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string eventName"))
			{
				LogArgError(log, command, error);
				return;
			}
			Dictionary<string, Func<FarmEvent>> dictionary = new Dictionary<string, Func<FarmEvent>>(StringComparer.OrdinalIgnoreCase)
			{
				["dogs"] = () => new SoundInTheNightEvent(2),
				["earthquake"] = () => new SoundInTheNightEvent(4),
				["fairy"] = () => new FairyEvent(),
				["meteorite"] = () => new SoundInTheNightEvent(1),
				["owl"] = () => new SoundInTheNightEvent(3),
				["racoon"] = () => new SoundInTheNightEvent(5),
				["ufo"] = () => new SoundInTheNightEvent(0),
				["witch"] = () => new WitchEvent()
			};
			if (dictionary.TryGetValue(value, out var value2))
			{
				Game1.farmEventOverride = value2();
				log.Info("Set farm event to '" + value + "'! The event will play if no other nightly event plays normally.");
			}
			else
			{
				log.Error("Unknown event type; expected one of '" + string.Join("', '", dictionary.Keys) + "'.");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void TestWedding(string[] command, IGameLogger log)
		{
			Event weddingEvent = Utility.getWeddingEvent(Game1.player);
			LocationRequest locationRequest = Game1.getLocationRequest("Town");
			locationRequest.OnLoad += delegate
			{
				Game1.currentLocation.currentEvent = weddingEvent;
			};
			int x = 8;
			int y = 8;
			Utility.getDefaultWarpLocation(locationRequest.Name, ref x, ref y);
			Game1.warpFarmer(locationRequest, x, y, Game1.player.FacingDirection);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Festival(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string festivalId"))
			{
				LogArgError(log, command, error);
				return;
			}
			Dictionary<string, string> dictionary = Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + value);
			if (dictionary != null)
			{
				string text = new string(value.Where(char.IsLetter).ToArray());
				int value2 = Convert.ToInt32(new string(value.Where(char.IsDigit).ToArray()));
				Game1.game1.parseDebugInput("Season " + text, log);
				Game1.game1.parseDebugInput($"{"Day"} {value2}", log);
				string[] array = dictionary["conditions"].Split('/');
				int value3 = Convert.ToInt32(ArgUtility.SplitBySpaceAndGet(array[1], 0));
				Game1.game1.parseDebugInput($"{"Time"} {value3}", log);
				string text2 = array[0];
				Game1.game1.parseDebugInput("Warp " + text2 + " 1 1", log);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "ps" })]
		public static void PlaySound(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string soundId") || !ArgUtility.TryGetOptionalInt(command, 2, out var value2, out error, -1, "int pitch"))
			{
				LogArgError(log, command, error);
			}
			else if (value2 > -1)
			{
				Game1.playSound(value, value2);
			}
			else
			{
				Game1.playSound(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void LogSounds(string[] command, IGameLogger log)
		{
			Game1.sounds.LogSounds = !Game1.sounds.LogSounds;
			log.Info((Game1.sounds.LogSounds ? "Enabled" : "Disabled") + " sound logging.");
		}

		[OtherNames(new string[] { "poali" })]
		public static void PrintOpenAlInfo(string[] command, IGameLogger log)
		{
			Type oalType = Assembly.GetAssembly(Game1.staminaRect.GetType())?.GetType("Microsoft.Xna.Framework.Audio.OpenALSoundController");
			if ((object)oalType == null)
			{
				log.Error("Could not find type 'OpenALSoundController'");
			}
			else
			{
				if (!TryGetField("_instance", BindingFlags.Static | BindingFlags.NonPublic, out var destField) || !TryGetField("availableSourcesCollection", BindingFlags.Instance | BindingFlags.NonPublic, out var destField2) || !TryGetField("inUseSourcesCollection", BindingFlags.Instance | BindingFlags.NonPublic, out var destField3))
				{
					return;
				}
				object value = destField.GetValue(null);
				if (value == null)
				{
					log.Error("OpenALSoundController._instance is null");
					return;
				}
				if (value.GetType() != oalType)
				{
					log.Error("OpenALSoundController._instance is not an instance of " + oalType.ToString());
					return;
				}
				object? value2 = destField2.GetValue(value);
				object value3 = destField3.GetValue(value);
				List<int> list = value2 as List<int>;
				List<int> list2 = value3 as List<int>;
				if (list == null)
				{
					log.Error("OpenALSoundController._instance.availableSourcesCollection is not an instance of List<int>");
					return;
				}
				if (list2 == null)
				{
					log.Error("OpenALSoundController._instance.inUseSourcesCollection is not an instance of List<int>");
					return;
				}
				log.Info($"Available: {list.Count}\nIn Use: {list2.Count}");
			}
			bool TryGetField(string fieldName, BindingFlags fieldFlags, out FieldInfo reference)
			{
				reference = oalType.GetField(fieldName, fieldFlags);
				if ((object)reference == null)
				{
					log.Error("OpenALSoundController does not have field '" + fieldName + "'");
					return false;
				}
				return true;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Crafting(string[] command, IGameLogger log)
		{
			foreach (string key in CraftingRecipe.craftingRecipes.Keys)
			{
				Game1.player.craftingRecipes.TryAdd(key, 0);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Cooking(string[] command, IGameLogger log)
		{
			foreach (string key in CraftingRecipe.cookingRecipes.Keys)
			{
				Game1.player.cookingRecipes.TryAdd(key, 0);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Experience(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string skill") | !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int experiencePoints"))
			{
				LogArgError(log, command, error);
				return;
			}
			switch (value.ToLower())
			{
			case "all":
				Game1.player.gainExperience(0, value2);
				Game1.player.gainExperience(1, value2);
				Game1.player.gainExperience(3, value2);
				Game1.player.gainExperience(2, value2);
				Game1.player.gainExperience(4, value2);
				break;
			case "farming":
				Game1.player.gainExperience(0, value2);
				break;
			case "fishing":
				Game1.player.gainExperience(1, value2);
				break;
			case "mining":
				Game1.player.gainExperience(3, value2);
				break;
			case "foraging":
				Game1.player.gainExperience(2, value2);
				break;
			case "combat":
				Game1.player.gainExperience(4, value2);
				break;
			default:
			{
				if (int.TryParse(value, out var result))
				{
					Game1.player.gainExperience(result, value2);
				}
				else
				{
					LogArgError(log, command, "unknown skill ID '" + value + "'");
				}
				break;
			}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ShowExperience(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int skillId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				log.Info(Game1.player.experiencePoints[value].ToString());
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Profession(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int professionId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.professions.Add(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ClearFishCaught(string[] command, IGameLogger log)
		{
			Game1.player.fishCaught.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "caughtFish" })]
		public static void FishCaught(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int count"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.stats.FishCaught = (uint)value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "r" })]
		public static void ResetForPlayerEntry(string[] command, IGameLogger log)
		{
			Game1.currentLocation.cleanupBeforePlayerExit();
			Game1.currentLocation.resetForPlayerEntry();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Fish(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string fishId"))
			{
				LogArgError(log, command, error);
			}
			else if (Game1.player.CurrentTool is FishingRod fishingRod)
			{
				List<string> tackleQualifiedItemIDs = fishingRod.GetTackleQualifiedItemIDs();
				Game1.activeClickableMenu = new BobberBar(value, 0.5f, treasure: true, tackleQualifiedItemIDs, null, isBossFish: false);
			}
			else
			{
				log.Error("The player must have a fishing rod equipped to use this command.");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void GrowAnimals(string[] command, IGameLogger log)
		{
			foreach (FarmAnimal value in Game1.currentLocation.animals.Values)
			{
				value.growFully();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void PauseAnimals(string[] command, IGameLogger log)
		{
			foreach (FarmAnimal value in Game1.currentLocation.Animals.Values)
			{
				value.pauseTimer = int.MaxValue;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void UnpauseAnimals(string[] command, IGameLogger log)
		{
			foreach (FarmAnimal value in Game1.currentLocation.Animals.Values)
			{
				value.pauseTimer = 0;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "removetf" })]
		public static void RemoveTerrainFeatures(string[] command, IGameLogger log)
		{
			Game1.currentLocation.terrainFeatures.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MushroomTrees(string[] command, IGameLogger log)
		{
			foreach (TerrainFeature value in Game1.currentLocation.terrainFeatures.Values)
			{
				if (value is Tree tree)
				{
					tree.treeType.Value = "7";
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void TrashCan(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int trashCanLevel"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.trashCanLevel = value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void FruitTrees(string[] command, IGameLogger log)
		{
			foreach (KeyValuePair<Vector2, TerrainFeature> pair in Game1.currentLocation.terrainFeatures.Pairs)
			{
				if (pair.Value is FruitTree fruitTree)
				{
					fruitTree.daysUntilMature.Value -= 27;
					fruitTree.dayUpdate();
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Train(string[] command, IGameLogger log)
		{
			Game1.RequireLocation<Railroad>("Railroad").setTrainComing(7500);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DebrisWeather(string[] command, IGameLogger log)
		{
			string locationContextId = Game1.player.currentLocation.GetLocationContextId();
			LocationWeather weatherForLocation = Game1.netWorldState.Value.GetWeatherForLocation(locationContextId);
			weatherForLocation.IsDebrisWeather = !weatherForLocation.IsDebrisWeather;
			if (locationContextId == "Default")
			{
				Game1.isDebrisWeather = weatherForLocation.isDebrisWeather.Value;
			}
			Game1.debrisWeather.Clear();
			if (weatherForLocation.IsDebrisWeather)
			{
				Game1.populateDebrisWeatherArray();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Speed(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int speed") || !ArgUtility.TryGetOptionalInt(command, 2, out var value2, out error, 30, "int minutes"))
			{
				LogArgError(log, command, error);
				return;
			}
			BuffEffects buffEffects = new BuffEffects();
			buffEffects.Speed.Value = value;
			Game1.player.applyBuff(new Buff("debug_speed", "Debug Speed", "Debug Speed", value2 * Game1.realMilliSecondsPerGameMinute, null, 0, buffEffects));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DayUpdate(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int days"))
			{
				LogArgError(log, command, error);
				return;
			}
			for (int i = 0; i < value; i++)
			{
				Game1.currentLocation.DayUpdate(Game1.dayOfMonth);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void FarmerDayUpdate(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int days"))
			{
				LogArgError(log, command, error);
				return;
			}
			for (int i = 0; i < value; i++)
			{
				Game1.player.dayupdate(Game1.timeOfDay);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MuseumLoot(string[] command, IGameLogger log)
		{
			foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
			{
				string itemId = allDatum.ItemId;
				string objectType = allDatum.ObjectType;
				if ((objectType == "Arch" || objectType == "Minerals") && !Game1.player.mineralsFound.ContainsKey(itemId) && !Game1.player.archaeologyFound.ContainsKey(itemId))
				{
					if (objectType == "Arch")
					{
						Game1.player.foundArtifact(itemId, 1);
					}
					else
					{
						Game1.player.addItemToInventoryBool(new Object(itemId, 1));
					}
				}
				if (Game1.player.freeSpotsInInventory() == 0)
				{
					break;
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void NewMuseumLoot(string[] command, IGameLogger log)
		{
			foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
			{
				string qualifiedItemId = allDatum.QualifiedItemId;
				if (LibraryMuseum.IsItemSuitableForDonation(qualifiedItemId) && !LibraryMuseum.HasDonatedArtifact(qualifiedItemId))
				{
					Game1.player.addItemToInventoryBool(ItemRegistry.Create(qualifiedItemId));
				}
				if (Game1.player.freeSpotsInInventory() == 0)
				{
					break;
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CreateDebris(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string itemId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.createObjectDebris(value, Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void RemoveDebris(string[] command, IGameLogger log)
		{
			Game1.currentLocation.debris.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void RemoveDirt(string[] command, IGameLogger log)
		{
			Game1.currentLocation.terrainFeatures.RemoveWhere((KeyValuePair<Vector2, TerrainFeature> pair) => pair.Value is HoeDirt);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DyeAll(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new CharacterCustomization(CharacterCustomization.Source.DyePots);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DyeShirt(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new CharacterCustomization(Game1.player.shirtItem.Value);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DyePants(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new CharacterCustomization(Game1.player.pantsItem.Value);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "cmenu", "customize" })]
		public static void CustomizeMenu(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new CharacterCustomization(CharacterCustomization.Source.NewGame);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CopyOutfit(string[] command, IGameLogger log)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("<Item><OutfitParts>");
			if (Game1.player.hat.Value != null)
			{
				stringBuilder.Append("<Item><ItemId>" + Game1.player.hat.Value.QualifiedItemId + "</ItemId></Item>");
			}
			if (Game1.player.pantsItem.Value != null)
			{
				stringBuilder.Append("<Item><ItemId>" + Game1.player.pantsItem.Value.QualifiedItemId + "</ItemId><Color>" + Game1.player.pantsItem.Value.clothesColor.Value.R + " " + Game1.player.pantsItem.Value.clothesColor.Value.G + " " + Game1.player.pantsItem.Value.clothesColor.Value.B + "</Color></Item>");
			}
			if (Game1.player.shirtItem.Value != null)
			{
				stringBuilder.Append("<Item><ItemId>" + Game1.player.shirtItem.Value.QualifiedItemId + "</ItemId><Color>" + Game1.player.shirtItem.Value.clothesColor.Value.R + " " + Game1.player.shirtItem.Value.clothesColor.Value.G + " " + Game1.player.shirtItem.Value.clothesColor.Value.B + "</Color></Item>");
			}
			stringBuilder.Append("</OutfitParts></Item>");
			string text = stringBuilder.ToString();
			DesktopClipboard.SetText(text);
			Game1.debugOutput = text;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SkinColor(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int skinColor"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.changeSkinColor(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Hat(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int hatId"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.player.changeHat(value);
			Game1.playSound("coin");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Pants(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int red") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int green") || !ArgUtility.TryGetInt(command, 3, out var value3, out error, "int blue"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.changePantsColor(new Color(value, value2, value3));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void HairStyle(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int hairStyle"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.changeHairStyle(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void HairColor(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int red") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int green") || !ArgUtility.TryGetInt(command, 3, out var value3, out error, "int blue"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.changeHairColor(new Color(value, value2, value3));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Shirt(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string shirtId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.changeShirt(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "m", "mv" })]
		public static void MusicVolume(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetFloat(command, 1, out var value, out var error, "float volume"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.musicPlayerVolume = value;
			Game1.options.musicVolumeLevel = value;
			Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void RemoveObjects(string[] command, IGameLogger log)
		{
			Game1.currentLocation.objects.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ListLights(string[] command, IGameLogger log)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(69, 6, stringBuilder2);
			handler.AppendLiteral("The viewport covers tiles (");
			handler.AppendFormatted(Game1.viewport.X / 64);
			handler.AppendLiteral(", ");
			handler.AppendFormatted(Game1.viewport.Y / 64);
			handler.AppendLiteral(") through (");
			handler.AppendFormatted(Game1.viewport.MaxCorner.X / 64);
			handler.AppendLiteral(", ");
			handler.AppendFormatted(Game1.viewport.MaxCorner.Y / 64);
			handler.AppendLiteral("), with the player at (");
			handler.AppendFormatted(Game1.player.TilePoint.X);
			handler.AppendLiteral(", ");
			handler.AppendFormatted(Game1.player.TilePoint.Y);
			handler.AppendLiteral(").");
			stringBuilder3.AppendLine(ref handler);
			stringBuilder.AppendLine();
			if (Game1.currentLightSources.Count > 0)
			{
				foreach (IGrouping<bool, KeyValuePair<string, LightSource>> item in from p in Game1.currentLightSources.ToLookup((KeyValuePair<string, LightSource> light) => light.Value.IsOnScreen())
					orderby p.Key descending
					select p)
				{
					bool key = item.Key;
					KeyValuePair<string, LightSource>[] array = item.ToArray();
					if (array.Length == 0)
					{
						continue;
					}
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder4 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder2);
					handler.AppendLiteral("Lights ");
					handler.AppendFormatted(key ? "in view" : "out of view");
					handler.AppendLiteral(":");
					stringBuilder4.AppendLine(ref handler);
					int num = 1;
					KeyValuePair<string, LightSource>[] array2 = array;
					for (int num2 = 0; num2 < array2.Length; num2++)
					{
						KeyValuePair<string, LightSource> keyValuePair = array2[num2];
						LightSource value = keyValuePair.Value;
						Vector2 vector = new Vector2(value.position.X / 64f, value.position.Y / 64f);
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder5 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(32, 5, stringBuilder2);
						handler.AppendLiteral("  ");
						handler.AppendFormatted(num++);
						handler.AppendLiteral(". '");
						handler.AppendFormatted(value.Id);
						handler.AppendLiteral("' at tile (");
						handler.AppendFormatted(vector.X);
						handler.AppendLiteral(", ");
						handler.AppendFormatted(vector.Y);
						handler.AppendLiteral(") with radius ");
						handler.AppendFormatted(value.radius.Value);
						stringBuilder5.Append(ref handler);
						if (value.onlyLocation.Value != null && value.onlyLocation.Value != Game1.currentLocation?.NameOrUniqueName)
						{
							stringBuilder2 = stringBuilder;
							StringBuilder stringBuilder6 = stringBuilder2;
							handler = new StringBuilder.AppendInterpolatedStringHandler(28, 1, stringBuilder2);
							handler.AppendLiteral(" [only shown in location '");
							handler.AppendFormatted(value.onlyLocation.Value);
							handler.AppendLiteral("']");
							stringBuilder6.Append(ref handler);
						}
						if (value.Id != keyValuePair.Key)
						{
							stringBuilder2 = stringBuilder;
							StringBuilder stringBuilder7 = stringBuilder2;
							handler = new StringBuilder.AppendInterpolatedStringHandler(74, 2, stringBuilder2);
							handler.AppendLiteral(" [WARNING: ID mismatch between dictionary lookup (");
							handler.AppendFormatted(keyValuePair.Key);
							handler.AppendLiteral(") and light instance (");
							handler.AppendFormatted(value.Id);
							handler.AppendLiteral(")]");
							stringBuilder7.Append(ref handler);
						}
						stringBuilder.AppendLine(".");
					}
					stringBuilder.AppendLine();
				}
			}
			else
			{
				stringBuilder.AppendLine("There are no current light sources.");
			}
			log.Info(stringBuilder.ToString().TrimEnd());
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void RemoveLights(string[] command, IGameLogger log)
		{
			Game1.currentLightSources.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "i" })]
		public static void Item(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string itemId") || !ArgUtility.TryGetOptionalInt(command, 2, out var value2, out error, 1, "int count") || !ArgUtility.TryGetOptionalInt(command, 3, out var value3, out error, 0, "int quality"))
			{
				LogArgError(log, command, error);
				return;
			}
			Item item = ItemRegistry.Create(value, value2, value3);
			Game1.playSound("coin");
			Game1.player.addItemToInventoryBool(item);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "iq" })]
		public static void ItemQuery(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetRemainder(command, 1, out var value, out var error, ' ', "string query"))
			{
				LogArgError(log, command, error);
				return;
			}
			ItemQueryResult[] array = ItemQueryResolver.TryResolve(value, null, ItemQuerySearchMode.All, null, null, avoidRepeat: false, null, delegate(string _, string queryError)
			{
				log.Error("Failed parsing that query: " + queryError);
			});
			if (array.Length == 0)
			{
				log.Info("That query did not match any items.");
				return;
			}
			ShopMenu shopMenu = new ShopMenu("DebugItemQuery", new Dictionary<ISalable, ItemStockInformation>());
			ItemQueryResult[] array2 = array;
			foreach (ItemQueryResult itemQueryResult in array2)
			{
				shopMenu.AddForSale(itemQueryResult.Item, new ItemStockInformation(0, int.MaxValue));
			}
			Game1.activeClickableMenu = shopMenu;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "gq" })]
		public static void GameQuery(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetRemainder(command, 1, out var value, out var error, ' ', "string query"))
			{
				LogArgError(log, command, error);
				return;
			}
			var array = (from rawQuery in GameStateQuery.SplitRaw(value)
				select new
				{
					Query = rawQuery,
					Result = GameStateQuery.CheckConditions(rawQuery)
				}).ToArray();
			int totalWidth = Math.Max("Query".Length, array.Max(p => p.Query.Length));
			StringBuilder stringBuilder = new StringBuilder().AppendLine().Append("   ").Append("Query".PadRight(totalWidth, ' '))
				.AppendLine(" | Result")
				.Append("   ")
				.Append("".PadRight(totalWidth, '-'))
				.AppendLine(" | ------");
			bool flag = true;
			var array2 = array;
			foreach (var anon in array2)
			{
				flag = flag && anon.Result;
				stringBuilder.Append("   ").Append(anon.Query.PadRight(totalWidth, ' ')).Append(" | ")
					.AppendLine(anon.Result.ToString().ToLower());
			}
			stringBuilder.AppendLine().Append("Overall result: ").Append(flag.ToString().ToLower())
				.AppendLine(".");
			log.Info(stringBuilder.ToString());
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Tokens(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetRemainder(command, 1, out var value, out var error, ' ', "string input"))
			{
				LogArgError(log, command, error);
				return;
			}
			string text = TokenParser.ParseText(value);
			log.Info("Result: \"" + text + "\".");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void DyeMenu(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new DyeMenu();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Tailor(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new TailoringMenu();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Forge(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new ForgeMenu();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ListTags(string[] command, IGameLogger log)
		{
			if (Game1.player.CurrentItem == null)
			{
				return;
			}
			string text = "Tags on " + Game1.player.CurrentItem.DisplayName + ": ";
			foreach (string contextTag in Game1.player.CurrentItem.GetContextTags())
			{
				text = text + contextTag + " ";
			}
			log.Info(text.Trim());
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void QualifiedId(string[] command, IGameLogger log)
		{
			if (Game1.player.CurrentItem != null)
			{
				string text = "Qualified ID of " + Game1.player.CurrentItem.DisplayName + ": " + Game1.player.CurrentItem.QualifiedItemId;
				log.Info(text.Trim());
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Dye(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string slot") || !ArgUtility.TryGet(command, 2, out var value2, out error, allowBlank: false, "string color") || !ArgUtility.TryGetOptionalFloat(command, 3, out var value3, out error, 1f, "float dyeStrength"))
			{
				LogArgError(log, command, error);
				return;
			}
			Color color = Color.White;
			switch (value2.ToLower().Trim())
			{
			case "black":
				color = Color.Black;
				break;
			case "red":
				color = new Color(220, 0, 0);
				break;
			case "blue":
				color = new Color(0, 100, 220);
				break;
			case "yellow":
				color = new Color(255, 230, 0);
				break;
			case "white":
				color = Color.White;
				break;
			case "green":
				color = new Color(10, 143, 0);
				break;
			}
			string text = value.ToLower().Trim();
			if (!(text == "shirt"))
			{
				if (text == "pants")
				{
					Game1.player.pantsItem.Value?.Dye(color, value3);
				}
			}
			else
			{
				Game1.player.shirtItem.Value?.Dye(color, value3);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void GetIndex(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string itemName"))
			{
				LogArgError(log, command, error);
				return;
			}
			Item item = Utility.fuzzyItemSearch(value);
			if (item != null)
			{
				log.Info(item.DisplayName + "'s qualified ID is " + item.QualifiedItemId);
			}
			else
			{
				log.Error("No item found with name " + value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "f", "fin" })]
		public static void FuzzyItemNamed(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: true, "string itemId") || !ArgUtility.TryGetOptionalInt(command, 2, out var value2, out error, 0, "int count") || !ArgUtility.TryGetOptionalInt(command, 3, out var value3, out error, 0, "int quality"))
			{
				LogArgError(log, command, error);
				return;
			}
			Item item = Utility.fuzzyItemSearch(value, value2);
			if (item == null)
			{
				log.Error("No item found with name '" + value + "'");
				return;
			}
			item.quality.Value = value3;
			MeleeWeapon.attemptAddRandomInnateEnchantment(item, null);
			Game1.player.addItemToInventory(item);
			Game1.playSound("coin");
			log.Info($"Added {item.DisplayName} ({item.QualifiedItemId})");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "in" })]
		public static void ItemNamed(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string itemName") || !ArgUtility.TryGetOptionalInt(command, 2, out var value2, out error, 1, "int count") || !ArgUtility.TryGetOptionalInt(command, 3, out var value3, out error, 0, "int quality"))
			{
				LogArgError(log, command, error);
				return;
			}
			foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
			{
				if (allDatum.InternalName.EqualsIgnoreCase(value))
				{
					Game1.player.addItemToInventory(ItemRegistry.Create("(O)" + allDatum.ItemId, value2, value3));
					Game1.playSound("coin");
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Achievement(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int achievementId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.getAchievement(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Heal(string[] command, IGameLogger log)
		{
			Game1.player.health = Game1.player.maxHealth;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Die(string[] command, IGameLogger log)
		{
			Game1.player.health = 0;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Energize(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, Game1.player.MaxStamina, "int stamina"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.Stamina = value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Exhaust(string[] command, IGameLogger log)
		{
			Game1.player.Stamina = -15f;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Warp(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string locationName") || !ArgUtility.TryGetOptionalInt(command, 2, out var x, out error, -1, "int tileX") || !ArgUtility.TryGetOptionalInt(command, 3, out var y, out error, -1, "int tileY"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (x > -1 && y <= -1)
			{
				LogArgError(log, command, "must specify both X and Y positions, or neither");
				return;
			}
			GameLocation gameLocation = Utility.fuzzyLocationSearch(value);
			if (gameLocation == null)
			{
				log.Error("No location with name " + value);
				return;
			}
			if (x < 0)
			{
				x = 0;
				y = 0;
				Utility.getDefaultWarpLocation(gameLocation.Name, ref x, ref y);
			}
			Game1.warpFarmer(new LocationRequest(gameLocation.NameOrUniqueName, gameLocation.uniqueName.Value != null, gameLocation), x, y, 2);
			log.Info($"Warping Game1.player to {gameLocation.NameOrUniqueName} at {x}, {y}");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "wh" })]
		public static void WarpHome(string[] command, IGameLogger log)
		{
			Game1.warpHome();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Money(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int amount"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.Money = value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CatchAllFish(string[] command, IGameLogger log)
		{
			foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
			{
				if (allDatum.ObjectType == "Fish")
				{
					Game1.player.caughtFish(allDatum.ItemId, 9);
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ActivateCalicoStatue(string[] command, IGameLogger log)
		{
			Game1.mine.calicoStatueSpot.Value = new Point(8, 8);
			Game1.mine.calicoStatueActivated(new NetPoint(new Point(8, 8)), Point.Zero, new Point(8, 8));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Perfection(string[] command, IGameLogger log)
		{
			Game1.game1.parseDebugInput("CompleteCc", log);
			Game1.game1.parseDebugInput("Specials", log);
			Game1.game1.parseDebugInput("FriendAll", log);
			Game1.game1.parseDebugInput("Cooking", log);
			Game1.game1.parseDebugInput("Crafting", log);
			foreach (string key in Game1.player.craftingRecipes.Keys)
			{
				Game1.player.craftingRecipes[key] = 1;
			}
			foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
			{
				string itemId = allDatum.ItemId;
				if (allDatum.ObjectType == "Fish")
				{
					Game1.player.fishCaught.Add(allDatum.QualifiedItemId, new int[3]);
				}
				if (Object.isPotentialBasicShipped(itemId, allDatum.Category, allDatum.ObjectType))
				{
					Game1.player.basicShipped.Add(itemId, 1);
				}
				Game1.player.recipesCooked.Add(itemId, 1);
			}
			Game1.game1.parseDebugInput("Walnut 130", log);
			Game1.player.mailReceived.Add("CF_Fair");
			Game1.player.mailReceived.Add("CF_Fish");
			Game1.player.mailReceived.Add("CF_Sewer");
			Game1.player.mailReceived.Add("CF_Mines");
			Game1.player.mailReceived.Add("CF_Spouse");
			Game1.player.mailReceived.Add("CF_Statue");
			Game1.player.mailReceived.Add("museumComplete");
			Game1.player.miningLevel.Value = 10;
			Game1.player.fishingLevel.Value = 10;
			Game1.player.foragingLevel.Value = 10;
			Game1.player.combatLevel.Value = 10;
			Game1.player.farmingLevel.Value = 10;
			Farm farm = Game1.getFarm();
			farm.buildStructure("Water Obelisk", new Vector2(0f, 0f), Game1.player, out var constructed, magicalConstruction: true, skipSafetyChecks: true);
			farm.buildStructure("Earth Obelisk", new Vector2(4f, 0f), Game1.player, out constructed, magicalConstruction: true, skipSafetyChecks: true);
			farm.buildStructure("Desert Obelisk", new Vector2(8f, 0f), Game1.player, out constructed, magicalConstruction: true, skipSafetyChecks: true);
			farm.buildStructure("Island Obelisk", new Vector2(12f, 0f), Game1.player, out constructed, magicalConstruction: true, skipSafetyChecks: true);
			farm.buildStructure("Gold Clock", new Vector2(16f, 0f), Game1.player, out constructed, magicalConstruction: true, skipSafetyChecks: true);
			foreach (KeyValuePair<string, string> item in DataLoader.Monsters(Game1.content))
			{
				for (int i = 0; i < 500; i++)
				{
					Game1.stats.monsterKilled(item.Key);
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Walnut(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int count"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.netWorldState.Value.GoldenWalnuts += value;
			Game1.netWorldState.Value.GoldenWalnutsFound += value;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Gem(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int count"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.QiGems += value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "removeNpc" })]
		public static void KillNpc(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var npcName, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			bool anyFound = false;
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				location.characters.RemoveWhere(delegate(NPC npc)
				{
					if (npc.Name == npcName)
					{
						log.Info("Removed " + npc.Name + " from " + location.NameOrUniqueName);
						anyFound = true;
						return true;
					}
					return false;
				});
				return true;
			});
			if (!anyFound)
			{
				log.Error("Couldn't find " + npcName + " in any locations.");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		/// <remarks>See also <see cref="M:StardewValley.DebugCommands.DefaultHandlers.Dp(System.String[],StardewValley.Logging.IGameLogger)" />.</remarks>
		[OtherNames(new string[] { "dap" })]
		public static void DaysPlayed(string[] command, IGameLogger log)
		{
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3332", (int)Game1.stats.DaysPlayed));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void FriendAll(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var friendship, out var error, 2500, "int friendship"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (Game1.year == 1)
			{
				Game1.AddCharacterIfNecessary("Kent", bypassConditions: true);
				Game1.AddCharacterIfNecessary("Leo", bypassConditions: true);
			}
			Utility.ForEachVillager(delegate(NPC n)
			{
				if (!n.CanSocialize && n.Name != "Sandy" && n.Name == "Krobus")
				{
					return true;
				}
				if (n.Name == "Marlon")
				{
					return true;
				}
				if (!Game1.player.friendshipData.ContainsKey(n.Name))
				{
					Game1.player.friendshipData.Add(n.Name, new Friendship());
				}
				Game1.player.changeFriendship(friendship, n);
				return true;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "friend" })]
		public static void Friendship(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int friendshipPoints"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value);
			if (nPC == null)
			{
				log.Error("No character found matching '" + value + "'.");
				return;
			}
			if (!Game1.player.friendshipData.TryGetValue(nPC.Name, out var value3))
			{
				value3 = (Game1.player.friendshipData[nPC.Name] = new Friendship());
			}
			value3.Points = value2;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void GetStat(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string statName"))
			{
				LogArgError(log, command, error);
				return;
			}
			uint value2 = Game1.stats.Get(value);
			log.Info($"The '{value}' stat is set to {value2}.");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SetStat(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string statName") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int newValue"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.stats.Set(value, value2);
			log.Info($"Set '{value}' stat to {Game1.stats.Get(value)}.");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "eventSeen" })]
		public static void SeenEvent(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string eventId") || !ArgUtility.TryGetOptionalBool(command, 2, out var value2, out error, defaultValue: true, "bool seen"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.player.eventsSeen.Toggle(value, value2);
			if (!value2)
			{
				Game1.eventsSeenSinceLastLocationChange.Remove(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SeenMail(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string mailId") || !ArgUtility.TryGetOptionalBool(command, 2, out var value2, out error, defaultValue: true, "bool seen"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.mailReceived.Toggle(value, value2);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CookingRecipe(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetRemainder(command, 1, out var value, out var error, ' ', "string recipeName"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.cookingRecipes.Add(value.Trim(), 0);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "craftingRecipe" })]
		public static void AddCraftingRecipe(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetRemainder(command, 1, out var value, out var error, ' ', "string recipeName"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.craftingRecipes.Add(value.Trim(), 0);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void UpgradeHouse(string[] command, IGameLogger log)
		{
			Game1.player.HouseUpgradeLevel = Math.Min(3, Game1.player.HouseUpgradeLevel + 1);
			Game1.addNewFarmBuildingMaps();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void StopRafting(string[] command, IGameLogger log)
		{
			Game1.player.isRafting = false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Time(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int time"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.timeOfDay = value;
			Game1.outdoorLight = Color.White;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void AddMinute(string[] command, IGameLogger log)
		{
			Game1.addMinute();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void AddHour(string[] command, IGameLogger log)
		{
			Game1.addHour();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Water(string[] command, IGameLogger log)
		{
			Game1.currentLocation?.ForEachDirt(delegate(HoeDirt dirt)
			{
				if (dirt.Pot != null)
				{
					dirt.Pot.Water();
				}
				else
				{
					dirt.state.Value = 1;
				}
				return true;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void GrowCrops(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var days, out var error, "int days"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.currentLocation?.ForEachDirt(delegate(HoeDirt dirt)
			{
				if (dirt?.crop != null)
				{
					for (int i = 0; i < days; i++)
					{
						dirt.crop.newDay(1);
						if (dirt.crop == null)
						{
							break;
						}
					}
				}
				return true;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "c", "cm" })]
		public static void CanMove(string[] command, IGameLogger log)
		{
			Game1.player.isEating = false;
			Game1.player.CanMove = true;
			Game1.player.UsingTool = false;
			Game1.player.usingSlingshot = false;
			Game1.player.FarmerSprite.PauseForSingleAnimation = false;
			if (Game1.player.CurrentTool is FishingRod fishingRod)
			{
				fishingRod.isFishing = false;
			}
			Game1.player.mount?.dismount();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Backpack(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int increaseBy"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.increaseBackpackSize(Math.Min(36 - Game1.player.Items.Count, value));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Question(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string questionId") || !ArgUtility.TryGetOptionalBool(command, 2, out var value2, out error, defaultValue: true, "bool seen"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.dialogueQuestionsAnswered.Toggle(value, value2);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Year(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int year"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.year = value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Day(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int day"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.stats.DaysPlayed = (uint)(Game1.seasonIndex * 28 + value + (Game1.year - 1) * 4 * 28);
			Game1.dayOfMonth = value;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Season(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetEnum<Season>(command, 1, out var value, out var error, "Season season"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.season = value;
			Game1.setGraphicsForSeason();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "dialogue" })]
		public static void AddDialogue(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string search") || !ArgUtility.TryGetRemainder(command, 2, out var value2, out error, ' ', "string dialogueText"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value);
			if (nPC == null)
			{
				log.Error("No NPC found matching search '" + value + "'.");
			}
			else
			{
				Game1.DrawDialogue(new Dialogue(nPC, null, value2));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Speech(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string search") || !ArgUtility.TryGetRemainder(command, 2, out var value2, out error, ' ', "string dialogueText"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value);
			if (nPC == null)
			{
				log.Error("No NPC found matching search '" + value + "'.");
			}
			else
			{
				Game1.DrawDialogue(new Dialogue(nPC, null, value2));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void LoadDialogue(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName") || !ArgUtility.TryGet(command, 2, out var value2, out error, allowBlank: false, "string translationKey"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value);
			string dialogueText = Game1.content.LoadString(value2).Replace("{", "<").Replace("}", ">");
			nPC.CurrentDialogue.Push(new Dialogue(nPC, value2, dialogueText));
			Game1.drawDialogue(nPC);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Wedding(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.player.spouse = value;
			Game1.weddingsToday.Add(Game1.player.UniqueMultiplayerID);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void GameMode(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int gameMode"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.setGameMode((byte)value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Volcano(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int level"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.warpFarmer(VolcanoDungeon.GetLevelName(value), 0, 1, 2);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MineLevel(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int level") || !ArgUtility.TryGetOptionalInt(command, 2, out var value2, out error, -1, "int layout"))
			{
				LogArgError(log, command, error);
				return;
			}
			int? num = value2;
			if (num < 0)
			{
				num = null;
			}
			Game1.enterMine(value, num);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MineInfo(string[] command, IGameLogger log)
		{
			log.Info($"MineShaft.lowestLevelReached = {MineShaft.lowestLevelReached}\nplayer.deepestMineLevel = {Game1.player.deepestMineLevel}");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Viewport(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetPoint(command, 1, out var value, out var error, "Point tilePosition"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.viewport.X = value.X * 64;
			Game1.viewport.Y = value.Y * 64;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MakeInedible(string[] command, IGameLogger log)
		{
			if (Game1.player.ActiveObject != null)
			{
				Game1.player.ActiveObject.edibility.Value = -300;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "watm" })]
		public static void WarpAnimalToMe(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string animalName"))
			{
				LogArgError(log, command, error);
				return;
			}
			FarmAnimal farmAnimal = Utility.fuzzyAnimalSearch(value);
			if (farmAnimal == null)
			{
				log.Info("Couldn't find character named " + value);
				return;
			}
			log.Info("Warping " + farmAnimal.displayName);
			farmAnimal.currentLocation.Animals.Remove(farmAnimal.myID.Value);
			Game1.currentLocation.Animals.Add(farmAnimal.myID.Value, farmAnimal);
			farmAnimal.Position = Game1.player.Position;
			farmAnimal.controller = null;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "wctm" })]
		public static void WarpCharacterToMe(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value, must_be_villager: false);
			if (nPC == null)
			{
				log.Error("Couldn't find character named " + value);
				return;
			}
			log.Info("Warping " + nPC.displayName);
			Game1.warpCharacter(nPC, Game1.currentLocation.Name, new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y));
			nPC.controller = null;
			nPC.Halt();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "wc" })]
		public static void WarpCharacter(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName") || !ArgUtility.TryGetPoint(command, 2, out var value2, out error, "Point tile") || !ArgUtility.TryGetOptionalInt(command, 4, out var value3, out error, 2, "int facingDirection"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value, must_be_villager: false);
			if (nPC == null)
			{
				log.Error("Couldn't find character named " + value);
				return;
			}
			Game1.warpCharacter(nPC, Game1.currentLocation.Name, value2);
			nPC.faceDirection(value3);
			nPC.controller = null;
			nPC.Halt();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "wtp" })]
		public static void WarpToPlayer(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var playerName, out var error, allowBlank: false, "string playerName"))
			{
				LogArgError(log, command, error);
				return;
			}
			Farmer farmer = Game1.getOnlineFarmers().FirstOrDefault((Farmer other) => other.displayName.EqualsIgnoreCase(playerName));
			if (farmer == null)
			{
				log.Error("Could not find other farmer " + playerName);
				return;
			}
			Game1.game1.parseDebugInput($"{"Warp"} {farmer.currentLocation.NameOrUniqueName} {farmer.TilePoint.X} {farmer.TilePoint.Y}", log);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "wtc" })]
		public static void WarpToCharacter(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value);
			if (nPC == null)
			{
				log.Error("Could not find valid character " + value);
				return;
			}
			Game1.game1.parseDebugInput($"{"Warp"} {Utility.getGameLocationOfCharacter(nPC).Name} {nPC.TilePoint.X} {nPC.TilePoint.Y}", log);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "wct" })]
		public static void WarpCharacterTo(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName") || !ArgUtility.TryGet(command, 2, out var value2, out error, allowBlank: false, "string locationName") || !ArgUtility.TryGetPoint(command, 3, out var value3, out error, "Point tile") || !ArgUtility.TryGetOptionalInt(command, 5, out var value4, out error, 2, "int facingDirection"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value);
			if (nPC == null)
			{
				log.Error("Could not find valid character " + value);
				return;
			}
			Game1.warpCharacter(nPC, value2, value3);
			nPC.faceDirection(value4);
			nPC.controller = null;
			nPC.Halt();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "ws" })]
		public static void WarpShop(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string shopKey"))
			{
				LogArgError(log, command, error);
				return;
			}
			switch (value.ToLower())
			{
			case "pierre":
				Game1.game1.parseDebugInput("Warp SeedShop 4 19", log);
				Game1.game1.parseDebugInput("WarpCharacterTo Pierre SeedShop 4 17", log);
				break;
			case "robin":
				Game1.game1.parseDebugInput("Warp ScienceHouse 8 20", log);
				Game1.game1.parseDebugInput("WarpCharacterTo Robin ScienceHouse 8 18", log);
				break;
			case "krobus":
				Game1.game1.parseDebugInput("Warp Sewer 31 19", log);
				break;
			case "sandy":
				Game1.game1.parseDebugInput("Warp SandyHouse 2 7", log);
				Game1.game1.parseDebugInput("WarpCharacterTo Sandy SandyHouse 2 5", log);
				break;
			case "marnie":
				Game1.game1.parseDebugInput("Warp AnimalShop 12 16", log);
				Game1.game1.parseDebugInput("WarpCharacterTo Marnie AnimalShop 12 14", log);
				break;
			case "clint":
				Game1.game1.parseDebugInput("Warp Blacksmith 3 15", log);
				Game1.game1.parseDebugInput("WarpCharacterTo Clint Blacksmith 3 13", log);
				break;
			case "gus":
				Game1.game1.parseDebugInput("Warp Saloon 10 20", log);
				Game1.game1.parseDebugInput("WarpCharacterTo Gus Saloon 10 18", log);
				break;
			case "willy":
				Game1.game1.parseDebugInput("Warp FishShop 6 6", log);
				Game1.game1.parseDebugInput("WarpCharacterTo Willy FishShop 6 4", log);
				break;
			case "pam":
				Game1.game1.parseDebugInput("Warp BusStop 7 12", log);
				Game1.game1.parseDebugInput("WarpCharacterTo Pam BusStop 11 10", log);
				break;
			case "dwarf":
				Game1.game1.parseDebugInput("Warp Mine 43 7", log);
				break;
			case "wizard":
				Game1.player.eventsSeen.Add("418172");
				Game1.player.hasMagicInk = true;
				Game1.game1.parseDebugInput("Warp WizardHouse 2 14", log);
				break;
			default:
				log.Error("That npc doesn't have a shop or it isn't handled by this command");
				break;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void FacePlayer(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value);
			if (nPC == null)
			{
				log.Error("Can't find NPC '" + value + "'.");
			}
			else
			{
				nPC.faceTowardFarmer = true;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Refuel(string[] command, IGameLogger log)
		{
			if (Game1.player.getToolFromName("Lantern") is Lantern lantern)
			{
				lantern.fuelLeft = 100;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Lantern(string[] command, IGameLogger log)
		{
			Game1.player.Items.Add(ItemRegistry.Create("(T)Lantern"));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void GrowGrass(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int iterations"))
			{
				LogArgError(log, command, error);
				return;
			}
			Game1.currentLocation.spawnWeeds(weedsOnly: false);
			Game1.currentLocation.growWeedGrass(value);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void AddAllCrafting(string[] command, IGameLogger log)
		{
			foreach (string key in CraftingRecipe.craftingRecipes.Keys)
			{
				Game1.player.craftingRecipes.Add(key, 0);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Animal(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetRemainder(command, 1, out var value, out var error, ' ', "string animalName"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Utility.addAnimalToFarm(new FarmAnimal(value.Trim(), Game1.multiplayer.getNewID(), Game1.player.UniqueMultiplayerID));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MoveBuilding(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetVector2(command, 1, out var value, out var error, integerOnly: true, "Vector2 fromTile") || !ArgUtility.TryGetPoint(command, 3, out var value2, out error, "Point toTile"))
			{
				LogArgError(log, command, error);
				return;
			}
			GameLocation currentLocation = Game1.currentLocation;
			if (currentLocation != null)
			{
				Building buildingAt = currentLocation.getBuildingAt(value);
				if (buildingAt != null)
				{
					buildingAt.tileX.Value = value2.X;
					buildingAt.tileY.Value = value2.Y;
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Fishing(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int level"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.fishingLevel.Value = value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "fd", "face" })]
		public static void FaceDirection(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string targetName") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int facingDirection"))
			{
				LogArgError(log, command, error);
			}
			else if (value == "farmer")
			{
				Game1.player.Halt();
				Game1.player.completelyStopAnimatingOrDoingAction();
				Game1.player.faceDirection(value2);
			}
			else
			{
				Utility.fuzzyCharacterSearch(value).faceDirection(value2);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Note(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int noteId"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (!Game1.player.archaeologyFound.TryGetValue("102", out var value2))
			{
				value2 = (Game1.player.archaeologyFound["102"] = new int[2]);
			}
			value2[0] = 18;
			Game1.netWorldState.Value.LostBooksFound = 18;
			Game1.currentLocation.readNote(value);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void NetHost(string[] command, IGameLogger log)
		{
			Game1.multiplayer.StartServer();
		}

		/// <summary>Connect to a specified IP address.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void NetJoin(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string address"))
			{
				LogArgError(log, command, error);
				return;
			}
			FarmhandMenu farmhandMenu = new FarmhandMenu(Game1.multiplayer.InitClient(new LidgrenClient(value)));
			if (Game1.activeClickableMenu is TitleMenu)
			{
				TitleMenu.subMenu = farmhandMenu;
				return;
			}
			Game1.ExitToTitle(delegate
			{
				(Game1.activeClickableMenu as TitleMenu).skipToTitleButtons();
				TitleMenu.subMenu = farmhandMenu;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ToggleNetCompression(string[] command, IGameLogger log)
		{
			if (Program.defaultCompression.GetType() == typeof(NullNetCompression))
			{
				log.Error("This command can only be used on platforms that support compression.");
				return;
			}
			if (Game1.activeClickableMenu is TitleMenu)
			{
				ToggleCompression();
				return;
			}
			Game1.ExitToTitle(delegate
			{
				(Game1.activeClickableMenu as TitleMenu).skipToTitleButtons();
				ToggleCompression();
			});
			void ToggleCompression()
			{
				bool flag = Program.netCompression.GetType() == typeof(NullNetCompression);
				INetCompression netCompression2;
				if (!flag)
				{
					INetCompression netCompression = new NullNetCompression();
					netCompression2 = netCompression;
				}
				else
				{
					netCompression2 = Program.defaultCompression;
				}
				Program.netCompression = netCompression2;
				log.Info((flag ? "Enabled" : "Disabled") + " net compression.");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void LevelUp(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int skill") || !ArgUtility.TryGetInt(command, 2, out var value2, out error, "int level"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.activeClickableMenu = new LevelUpMenu(value, value2);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Darts(string[] command, IGameLogger log)
		{
			Game1.currentMinigame = new Darts();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MineGame(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptional(command, 1, out var value, out var error, null, allowBlank: false, "string mode"))
			{
				LogArgError(log, command, error);
				return;
			}
			int mode = ((value == "infinite") ? 2 : 3);
			Game1.currentMinigame = new MineCart(0, mode);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Crane(string[] command, IGameLogger log)
		{
			Game1.currentMinigame = new CraneGame();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "trlt" })]
		public static void TailorRecipeListTool(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new TailorRecipeListTool();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "apt" })]
		public static void AnimationPreviewTool(string[] command, IGameLogger log)
		{
			Game1.activeClickableMenu = new AnimationPreviewTool();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void CreateDino(string[] command, IGameLogger log)
		{
			Game1.currentLocation.characters.Add(new DinoMonster(Game1.player.position.Value + new Vector2(100f, 0f)));
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "pta" })]
		public static void PerformTitleAction(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var titleAction, out var error, allowBlank: false, "string titleAction"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (Game1.activeClickableMenu is TitleMenu titleMenu)
			{
				titleMenu.performButtonAction(titleAction);
				return;
			}
			Game1.ExitToTitle(delegate
			{
				if (Game1.activeClickableMenu is TitleMenu titleMenu2)
				{
					titleMenu2.skipToTitleButtons();
					titleMenu2.performButtonAction(titleAction);
				}
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Action(string[] command, IGameLogger log)
		{
			Exception exception;
			if (!ArgUtility.TryGetRemainder(command, 1, out var value, out var error, ' ', "string action"))
			{
				LogArgError(log, command, error);
			}
			else if (TriggerActionManager.TryRunAction(value, out error, out exception))
			{
				log.Info("Applied action '" + value + "'.");
			}
			else
			{
				log.Error("Couldn't apply action '" + value + "': " + error, exception);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void BroadcastMail(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetRemainder(command, 1, out var value, out var error, ' ', "string mailId"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.addMailForTomorrow(value, noLetter: false, sendToEveryone: true);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Phone(string[] command, IGameLogger log)
		{
			Game1.game1.ShowTelephoneMenu();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Renovate(string[] command, IGameLogger log)
		{
			HouseRenovation.ShowRenovationMenu();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Crib(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetInt(command, 1, out var value, out var error, "int style"))
			{
				LogArgError(log, command, error);
			}
			else if (Game1.getLocationFromName(Game1.player.homeLocation.Value) is FarmHouse farmHouse)
			{
				farmHouse.cribStyle.Value = value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void TestNut(string[] command, IGameLogger log)
		{
			Game1.createItemDebris(ItemRegistry.Create("(O)73"), Vector2.Zero, 2);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ShuffleBundles(string[] command, IGameLogger log)
		{
			Game1.GenerateBundles(Game1.BundleType.Remixed, use_seed: false);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Split(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, -1, "int playerIndex"))
			{
				LogArgError(log, command, error);
			}
			else if (value > -1)
			{
				GameRunner.instance.AddGameInstance((PlayerIndex)value);
			}
			else
			{
				Game1.game1.ShowLocalCoopJoinMenu();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "bsm" })]
		public static void SkinBuilding(string[] command, IGameLogger log)
		{
			Building building = Game1.currentLocation?.getBuildingAt(Game1.player.Tile + new Vector2(0f, -1f));
			if (building != null)
			{
				if (building.CanBeReskinned())
				{
					Game1.activeClickableMenu = new BuildingSkinMenu(building);
				}
				else
				{
					log.Error("The '" + building.buildingType.Value + "' building in front of the player can't be skinned.");
				}
			}
			else
			{
				log.Error("No building found in front of player.");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "bpm" })]
		public static void PaintBuilding(string[] command, IGameLogger log)
		{
			Building building = Game1.currentLocation?.getBuildingAt(Game1.player.Tile + new Vector2(0f, -1f));
			if (building != null)
			{
				if (building.CanBePainted())
				{
					Game1.activeClickableMenu = new BuildingPaintMenu(building);
					return;
				}
				log.Error("The '" + building.buildingType.Value + "' building in front of the player can't be painted. Defaulting to main farmhouse.");
			}
			Building mainFarmHouse = Game1.getFarm().GetMainFarmHouse();
			if (mainFarmHouse == null)
			{
				log.Error("The main farmhouse wasn't found.");
			}
			else if (!mainFarmHouse.CanBePainted())
			{
				log.Error("The main farmhouse can't be painted.");
			}
			else
			{
				Game1.activeClickableMenu = new BuildingPaintMenu(mainFarmHouse);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "md" })]
		public static void MineDifficulty(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, -1, "int difficulty"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (value > -1)
			{
				Game1.netWorldState.Value.MinesDifficulty = value;
			}
			log.Info($"Mine difficulty: {Game1.netWorldState.Value.MinesDifficulty}");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "scd" })]
		public static void SkullCaveDifficulty(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, -1, "int difficulty"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (value > -1)
			{
				Game1.netWorldState.Value.SkullCavesDifficulty = value;
			}
			log.Info($"Skull Cave difficulty: {Game1.netWorldState.Value.SkullCavesDifficulty}");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "tls" })]
		public static void ToggleLightingScale(string[] command, IGameLogger log)
		{
			Game1.game1.useUnscaledLighting = !Game1.game1.useUnscaledLighting;
			log.Info($"Toggled Lighting Scale: useUnscaledLighting: {Game1.game1.useUnscaledLighting}");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void FixWeapons(string[] command, IGameLogger log)
		{
			SaveMigrator_1_5.ResetForges();
			log.Info("Reset forged weapon attributes.");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "plsf" })]
		public static void PrintLatestSaveFix(string[] command, IGameLogger log)
		{
			SaveFixes value = SaveFixes.FixDuplicateMissedMail;
			log.Info($"The latest save fix is '{value.ToString()}' (ID: {value})");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "pdb" })]
		public static void PrintGemBirds(string[] command, IGameLogger log)
		{
			log.Info($"Gem birds: North {IslandGemBird.GetBirdTypeForLocation("IslandNorth")} South {IslandGemBird.GetBirdTypeForLocation("IslandSouth")} East {IslandGemBird.GetBirdTypeForLocation("IslandEast")} West {IslandGemBird.GetBirdTypeForLocation("IslandWest")}");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "ppp" })]
		public static void PrintPlayerPos(string[] command, IGameLogger log)
		{
			log.Info($"Player tile position is {Game1.player.Tile} (World position: {Game1.player.Position})");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ShowPlurals(string[] command, IGameLogger log)
		{
			List<string> list = new List<string>();
			foreach (ParsedItemData allDatum in ItemRegistry.GetObjectTypeDefinition().GetAllData())
			{
				list.Add(allDatum.InternalName);
			}
			foreach (ParsedItemData allDatum2 in ItemRegistry.RequireTypeDefinition("(BC)").GetAllData())
			{
				list.Add(allDatum2.InternalName);
			}
			list.Sort();
			foreach (string item in list)
			{
				log.Info(Lexicon.makePlural(item));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void HoldItem(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalBool(command, 1, out var value, out var error, defaultValue: false, "bool showMessage"))
			{
				LogArgError(log, command, error);
			}
			else
			{
				Game1.player.holdUpItemThenMessage(Game1.player.CurrentItem, value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "rm" })]
		public static void RunMacro(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptional(command, 1, out var value, out var error, "macro.txt", allowBlank: false, "string fileName"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (Game1.isRunningMacro)
			{
				log.Error("You cannot run a macro from within a macro.");
				return;
			}
			Game1.isRunningMacro = true;
			try
			{
				StreamReader streamReader = new StreamReader(value);
				string text_to_send;
				while ((text_to_send = streamReader.ReadLine()) != null)
				{
					Game1.chatBox.textBoxEnter(text_to_send);
				}
				log.Info("Executed macro file " + value);
				streamReader.Close();
			}
			catch (Exception exception)
			{
				log.Error("Error running macro file " + value + ".", exception);
			}
			Game1.isRunningMacro = false;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void InviteMovie(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string npcName"))
			{
				LogArgError(log, command, error);
				return;
			}
			NPC nPC = Utility.fuzzyCharacterSearch(value);
			if (nPC == null)
			{
				log.Error("Invalid NPC");
			}
			else
			{
				MovieTheater.Invite(Game1.player, nPC);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Monster(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGet(command, 1, out var value, out var error, allowBlank: false, "string typeName") || !ArgUtility.TryGetPoint(command, 2, out var value2, out error, "Point tile") || !ArgUtility.TryGetOptionalRemainder(command, 4, out var value3))
			{
				LogArgError(log, command, error);
				return;
			}
			string text = "StardewValley.Monsters." + value;
			Type type = Type.GetType(text);
			if ((object)type == null)
			{
				log.Error("There's no monster with type '" + text + "'.");
				return;
			}
			Vector2 vector = new Vector2(value2.X * 64, value2.Y * 64);
			int result;
			object[] args = (string.IsNullOrWhiteSpace(value3) ? new object[1] { vector } : ((!int.TryParse(value3, out result)) ? new object[2] { vector, value3 } : new object[2] { vector, result }));
			Monster item = Activator.CreateInstance(type, args) as Monster;
			Game1.currentLocation.characters.Add(item);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "shaft" })]
		public static void Ladder(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalInt(command, 1, out var value, out var error, Game1.player.TilePoint.X, "int tileX") || !ArgUtility.TryGetOptionalInt(command, 2, out var value2, out error, Game1.player.TilePoint.Y + 1, "int tileY"))
			{
				LogArgError(log, command, error);
				return;
			}
			bool forceShaft = command[0].EqualsIgnoreCase("shaft");
			Game1.mine.createLadderDown(value, value2, forceShaft);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void NetLog(string[] command, IGameLogger log)
		{
			Game1.multiplayer.logging.IsLogging = !Game1.multiplayer.logging.IsLogging;
			log.Info("Turned " + (Game1.multiplayer.logging.IsLogging ? "on" : "off") + " network write logging");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void NetClear(string[] command, IGameLogger log)
		{
			Game1.multiplayer.logging.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void NetDump(string[] command, IGameLogger log)
		{
			log.Info("Wrote log to " + Game1.multiplayer.logging.Dump());
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "tto" })]
		public static void ToggleTimingOverlay(string[] command, IGameLogger log)
		{
			if ((!(Game1.game1?.IsMainInstance)) ?? true)
			{
				log.Error("Cannot toggle timing overlay as a splitscreen instance.");
				return;
			}
			bool flag = Game1.debugTimings.Toggle();
			log.Info((flag ? "Enabled" : "Disabled") + " in-game timing overlay.");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void LogBandwidth(string[] command, IGameLogger log)
		{
			if (Game1.IsServer)
			{
				Game1.server.LogBandwidth = !Game1.server.LogBandwidth;
				log.Info("Turned " + (Game1.server.LogBandwidth ? "on" : "off") + " server bandwidth logging");
			}
			else if (Game1.IsClient)
			{
				Game1.client.LogBandwidth = !Game1.client.LogBandwidth;
				log.Info("Turned " + (Game1.client.LogBandwidth ? "on" : "off") + " client bandwidth logging");
			}
			else
			{
				log.Error("Cannot toggle bandwidth logging in non-multiplayer games");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void LogWallAndFloorWarnings(string[] command, IGameLogger log)
		{
			DecoratableLocation.LogTroubleshootingInfo = !DecoratableLocation.LogTroubleshootingInfo;
			log.Info((DecoratableLocation.LogTroubleshootingInfo ? "Enabled" : "Disabled") + " wall and floor warning logs.");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ChangeWallet(string[] command, IGameLogger log)
		{
			if (Game1.IsMasterGame)
			{
				Game1.player.changeWalletTypeTonight.Value = true;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void SeparateWallets(string[] command, IGameLogger log)
		{
			if (Game1.IsMasterGame)
			{
				ManorHouse.SeparateWallets();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void MergeWallets(string[] command, IGameLogger log)
		{
			if (Game1.IsMasterGame)
			{
				ManorHouse.MergeWallets();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "nd", "newDay", "s" })]
		public static void Sleep(string[] command, IGameLogger log)
		{
			Game1.player.isInBed.Value = true;
			Game1.player.sleptInTemporaryBed.Value = true;
			Game1.currentLocation.answerDialogueAction("Sleep_Yes", null);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "gm", "inv" })]
		public static void Invincible(string[] command, IGameLogger log)
		{
			if (Game1.player.temporarilyInvincible)
			{
				Game1.player.temporaryInvincibilityTimer = 0;
				Game1.playSound("bigDeSelect");
			}
			else
			{
				Game1.player.temporarilyInvincible = true;
				Game1.player.temporaryInvincibilityTimer = -1000000000;
				Game1.playSound("bigSelect");
			}
		}

		/// <summary>Toggle whether multiplayer sync fields should run detailed validation to detect possible bugs. See remarks on <see cref="F:Netcode.NetFields.ShouldValidateNetFields" />.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ValidateNetFields(string[] command, IGameLogger log)
		{
			NetFields.ShouldValidateNetFields = !NetFields.ShouldValidateNetFields;
			log.Info(NetFields.ShouldValidateNetFields ? "Enabled net field validation, which may impact performance. This only affects new net fields created after it's enabled." : "Disabled net field validation.");
		}

		/// <summary>Filter the saves shown in the current load or co-op menu based on a search term.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		[OtherNames(new string[] { "flm" })]
		public static void FilterLoadMenu(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetRemainder(command, 1, out var value, out var error, ' ', "string filter"))
			{
				LogArgError(log, command, error);
				return;
			}
			if (Game1.activeClickableMenu is TitleMenu)
			{
				IClickableMenu subMenu = TitleMenu.subMenu;
				if (subMenu is CoopMenu coopMenu)
				{
					TitleMenu.subMenu = new CoopMenu(coopMenu.tooManyFarms, splitScreen: false, coopMenu.currentTab, value);
					return;
				}
				if (!(subMenu is FarmhandMenu) && subMenu is LoadGameMenu)
				{
					TitleMenu.subMenu = new LoadGameMenu(value);
					return;
				}
			}
			log.Error("The FilterLoadMenu debug command must be run while the list of saved games is open.");
		}

		/// <summary>Toggle the <see cref="F:StardewValley.Menus.MapPage.EnableDebugLines" /> option.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void WorldMapLines(string[] command, IGameLogger log)
		{
			MapPage.WorldMapDebugLineType parsed;
			if (command.Length > 1)
			{
				if (!Utility.TryParseEnum<MapPage.WorldMapDebugLineType>(string.Join(", ", command.Skip(1)), out parsed))
				{
					LogArgError(log, command, "unknown type '" + string.Join(" ", command.Skip(1)) + "', expected space-delimited list of " + string.Join(", ", Enum.GetNames(typeof(MapPage.WorldMapDebugLineType))));
					return;
				}
			}
			else
			{
				parsed = ((MapPage.EnableDebugLines == MapPage.WorldMapDebugLineType.None) ? MapPage.WorldMapDebugLineType.All : MapPage.WorldMapDebugLineType.None);
			}
			MapPage.EnableDebugLines = parsed;
			log.Info((parsed == MapPage.WorldMapDebugLineType.None) ? "World map debug lines disabled." : $"World map debug lines enabled for types {parsed}.");
		}

		/// <summary>Print info about the player's current position for <c>Data/WorldMaps</c> data.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		/// <remarks>This is derived from <see cref="M:StardewValley.WorldMaps.WorldMapManager.GetPositionData(StardewValley.GameLocation,Microsoft.Xna.Framework.Point)" />.</remarks>
		public static void WorldMapPosition(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptionalBool(command, 1, out var value, out var error, defaultValue: false, "bool includeLog"))
			{
				LogArgError(log, command, error);
				return;
			}
			GameLocation currentLocation = Game1.currentLocation;
			Point tilePoint = Game1.player.TilePoint;
			LogBuilder logBuilder = (value ? new LogBuilder(3) : null);
			MapAreaPositionWithContext? positionData = WorldMapManager.GetPositionData(currentLocation, tilePoint, logBuilder);
			StringBuilder stringBuilder = new StringBuilder();
			if (!positionData.HasValue)
			{
				stringBuilder.AppendLine("The player's current position didn't match any entry in Data/WorldMaps.");
			}
			else
			{
				MapAreaPositionWithContext value2 = positionData.Value;
				MapAreaPosition data = positionData.Value.Data;
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(33, 3, stringBuilder2);
				handler.AppendLiteral("The player is currently at ");
				handler.AppendFormatted(currentLocation.NameOrUniqueName);
				handler.AppendLiteral(" (");
				handler.AppendFormatted(tilePoint.X);
				handler.AppendLiteral(", ");
				handler.AppendFormatted(tilePoint.Y);
				handler.AppendLiteral(").");
				stringBuilder3.AppendLine(ref handler);
				if (currentLocation.NameOrUniqueName != value2.Location.NameOrUniqueName || tilePoint != value2.Tile)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder4 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(31, 3, stringBuilder2);
					handler.AppendLiteral("That was translated to '");
					handler.AppendFormatted(value2.Location.NameOrUniqueName);
					handler.AppendLiteral("' (");
					handler.AppendFormatted(value2.Tile.X);
					handler.AppendLiteral(", ");
					handler.AppendFormatted(value2.Tile.Y);
					handler.AppendLiteral(").");
					stringBuilder4.AppendLine(ref handler);
				}
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(53, 3, stringBuilder2);
				handler.AppendLiteral("This matches region '");
				handler.AppendFormatted(data.Region.Id);
				handler.AppendLiteral("', area '");
				handler.AppendFormatted(data.Area.Id);
				handler.AppendLiteral("', and map position '");
				handler.AppendFormatted(data.Data.Id);
				handler.AppendLiteral("'.");
				stringBuilder5.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(79, 3, stringBuilder2);
				handler.AppendLiteral("The position's pixel area is ");
				handler.AppendFormatted(data.GetPixelArea());
				handler.AppendLiteral(", with the player at position ");
				handler.AppendFormatted(value2.GetMapPixelPosition());
				handler.AppendLiteral(" (position ratio: ");
				handler.AppendFormatted(value2.GetPositionRatioIfValid());
				handler.AppendLiteral(").");
				stringBuilder6.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder7 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
				handler.AppendLiteral("Scroll text: ");
				handler.AppendFormatted(value2.GetScrollText() ?? "none");
				handler.AppendLiteral(".");
				stringBuilder7.AppendLine(ref handler);
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Log:");
			if (logBuilder != null)
			{
				stringBuilder.Append(logBuilder.Log);
			}
			else
			{
				stringBuilder.AppendLine("   Run `debug WorldMapPosition true` to show the detailed log.");
			}
			log.Info(stringBuilder.ToString());
		}

		/// <summary>List debug commands in the game.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void Search(string[] command, IGameLogger log)
		{
			if (!ArgUtility.TryGetOptional(command, 1, out var value, out var error, null, allowBlank: false, "string search"))
			{
				LogArgError(log, command, error);
				return;
			}
			List<string> list = SearchCommandNames(value);
			if (list.Count == 0)
			{
				log.Info("No debug commands found matching '" + value + "'.");
				return;
			}
			log.Info(((value != null) ? $"Found {list.Count} debug commands matching search term '{value}':\n" : $"{list.Count} debug commands registered:\n") + "  - " + string.Join("\n  - ", list) + ((value == null) ? "\n\nTip: you can search debug commands like 'debug Search searchTermHere'." : ""));
		}

		/// <summary>Add artifact spots in every available spot in a 9x9 grid around the player.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ArtifactSpots(string[] command, IGameLogger log)
		{
			GameLocation currentLocation = Game1.player.currentLocation;
			Vector2 tile = Game1.player.Tile;
			if (currentLocation == null)
			{
				log.Info("You must be in a location to use this command.");
				return;
			}
			int num = 0;
			Vector2[] surroundingTileLocationsArray = Utility.getSurroundingTileLocationsArray(tile);
			foreach (Vector2 vector in surroundingTileLocationsArray)
			{
				if (currentLocation.terrainFeatures.TryGetValue(vector, out var value) && value is HoeDirt { crop: null })
				{
					currentLocation.terrainFeatures.Remove(vector);
				}
				if (currentLocation.isTilePassable(vector) && !currentLocation.IsTileOccupiedBy(vector, ~(CollisionMask.Characters | CollisionMask.Farmers | CollisionMask.TerrainFeatures)))
				{
					currentLocation.objects.Add(vector, ItemRegistry.Create<Object>("(O)590"));
					num++;
				}
			}
			if (num == 0)
			{
				log.Info("No unoccupied tiles found around the player.");
				return;
			}
			log.Info($"Spawned {num} artifact spots around the player.");
		}

		/// <summary>Enable or disable writing messages to the debug log file.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void LogFile(string[] command, IGameLogger log)
		{
			if (Game1.log is DefaultLogger defaultLogger)
			{
				Game1.log = new DefaultLogger(defaultLogger.ShouldWriteToConsole, !defaultLogger.ShouldWriteToLogFile);
				log.Info((defaultLogger.ShouldWriteToLogFile ? "Disabled" : "Enabled") + " the game log file at " + Program.GetDebugLogPath() + ".");
			}
			else if (Game1.log?.GetType().FullName?.StartsWith("StardewModdingAPI.") ?? false)
			{
				log.Error("The debug log can't be enabled when SMAPI is installed. SMAPI already includes log messages in its own log file.");
			}
			else
			{
				log.Error("The debug log can't be enabled: the game logger has been replaced with unknown implementation '" + Game1.log?.GetType()?.FullName + "'.");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void ToggleCheats(string[] command, IGameLogger log)
		{
			Program.enableCheats = !Program.enableCheats;
			log.Info((Program.enableCheats ? "Enabled" : "Disabled") + " in-game cheats.");
		}
	}

	/// <summary>The supported commands and their resolvers.</summary>
	private static readonly Dictionary<string, DebugCommandHandlerDelegate> Handlers;

	/// <summary>Alternate names for debug commands (e.g. shorthand or acronyms).</summary>
	private static readonly Dictionary<string, string> Aliases;

	/// <summary>Register the default debug commands, defined as <see cref="T:StardewValley.DebugCommands.DefaultHandlers" /> methods.</summary>
	static DebugCommands()
	{
		Handlers = new Dictionary<string, DebugCommandHandlerDelegate>(StringComparer.OrdinalIgnoreCase);
		Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		MethodInfo[] methods = typeof(DefaultHandlers).GetMethods(BindingFlags.Static | BindingFlags.Public);
		MethodInfo[] array = methods;
		foreach (MethodInfo methodInfo in array)
		{
			try
			{
				Handlers[methodInfo.Name] = (DebugCommandHandlerDelegate)Delegate.CreateDelegate(typeof(DebugCommandHandlerDelegate), methodInfo);
			}
			catch (Exception exception)
			{
				Game1.log.Error("Failed to initialize debug command " + methodInfo.Name + ".", exception);
			}
		}
		array = methods;
		foreach (MethodInfo methodInfo2 in array)
		{
			OtherNamesAttribute customAttribute = methodInfo2.GetCustomAttribute<OtherNamesAttribute>();
			if (customAttribute == null)
			{
				continue;
			}
			string[] aliases = customAttribute.Aliases;
			foreach (string text in aliases)
			{
				if (Handlers.ContainsKey(text))
				{
					Game1.log.Error($"Can't register alias '{text}' for debug command '{methodInfo2.Name}', because there's a command with that name.");
				}
				if (Aliases.TryGetValue(text, out var value))
				{
					Game1.log.Error($"Can't register alias '{text}' for debug command '{methodInfo2.Name}', because that's already an alias for '{value}'.");
				}
				Aliases[text] = methodInfo2.Name;
			}
		}
	}

	/// <summary>Try to handle a debug command.</summary>
	/// <param name="command">The full debug command split by spaces, including the command name and arguments.</param>
	/// <param name="log">The log to which to write command output, or <c>null</c> to use <see cref="F:StardewValley.Game1.log" />.</param>
	/// <returns>Returns whether the command was found and executed, regardless of whether the command logic succeeded.</returns>
	public static bool TryHandle(string[] command, IGameLogger log = null)
	{
		if (log == null)
		{
			log = Game1.log;
		}
		string text = ArgUtility.Get(command, 0);
		if (string.IsNullOrWhiteSpace(text))
		{
			log.Error("Can't parse an empty command.");
			return false;
		}
		if (Aliases.TryGetValue(text, out var value))
		{
			text = value;
		}
		if (!Handlers.TryGetValue(text, out var value2))
		{
			log.Error("Unknown debug command '" + text + "'.");
			string[] array = SearchCommandNames(text).Take(10).ToArray();
			if (array.Length != 0)
			{
				log.Info("Did you mean one of these?\n- " + string.Join("\n- ", array));
			}
			return false;
		}
		try
		{
			value2(command, log);
			return true;
		}
		catch (Exception exception)
		{
			log.Error("Error running debug command '" + string.Join(" ", command) + "'.", exception);
			return false;
		}
	}

	/// <summary>Get the list of commands which match the given search text.</summary>
	/// <param name="search">The text to match in command names, or <c>null</c> to list all command names.</param>
	/// <param name="displayAliases">Whether to append aliases in the results, like <c>"houseUpgrade (house, hu)"</c>.</param>
	public static List<string> SearchCommandNames(string search, bool displayAliases = true)
	{
		ILookup<string, string> lookup = Aliases.ToLookup((KeyValuePair<string, string> p) => p.Value, (KeyValuePair<string, string> p) => p.Key);
		List<string> list = new List<string>();
		foreach (string item in Handlers.Keys.OrderBy<string, string>((string p) => p, StringComparer.OrdinalIgnoreCase))
		{
			string[] array = lookup[item].ToArray();
			if (array.Length == 0)
			{
				list.Add(item);
			}
			else if (displayAliases)
			{
				list.Add(item + " (" + string.Join(", ", array.OrderBy<string, string>((string p) => p, StringComparer.OrdinalIgnoreCase)) + ")");
			}
			else
			{
				list.Add("###" + item + "###" + string.Join(",", array));
			}
		}
		if (search != null)
		{
			list.RemoveAll((string line) => !Utility.fuzzyCompare(search, line).HasValue);
		}
		if (!displayAliases)
		{
			for (int num = 0; num < list.Count; num++)
			{
				if (list[num].StartsWith("###"))
				{
					list[num] = list[num].Split("###", 3)[1];
				}
			}
		}
		return list;
	}

	/// <summary>Log an error indicating a command's arguments are invalid.</summary>
	/// <param name="log">The log to which to write debug command output.</param>
	/// <param name="command">The full debug command split by spaces, including the command name.</param>
	/// <param name="error">The error phrase to log.</param>
	private static void LogArgError(IGameLogger log, string[] command, string error)
	{
		string text = ArgUtility.Get(command, 0);
		string text2 = text;
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (!Aliases.TryGetValue(text, out var value))
			{
				foreach (string key in Handlers.Keys)
				{
					if (text.EqualsIgnoreCase(key))
					{
						value = key;
						break;
					}
				}
			}
			text2 = value ?? text;
			if (!text2.EqualsIgnoreCase(text))
			{
				text2 = text + " (" + text2 + ")";
			}
		}
		log.Error($"Failed parsing {text2} command: {error}.");
	}
}
