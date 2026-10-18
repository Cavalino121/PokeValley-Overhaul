using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Ionic.Zlib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Characters;
using StardewValley.GameData.Locations;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Minigames;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.Pathfinding;
using StardewValley.Quests;
using StardewValley.SaveMigrations;
using StardewValley.SaveSerialization;
using StardewValley.SpecialOrders;
using StardewValley.TerrainFeatures;
using StardewValley.Util;

namespace StardewValley;

public class SaveGame
{
	/// <summary>The filename suffix for a save file that's currently being written.</summary>
	public const string TempNameSuffix = "_STARDEWVALLEYSAVETMP";

	/// <summary>The filename suffix for a previous save file.</summary>
	public const string BackupNameSuffix = "_old";

	/// <summary>Whether the current platform supports save backup files with the <see cref="F:StardewValley.SaveGame.TempNameSuffix" /> and <see cref="F:StardewValley.SaveGame.BackupNameSuffix" /> suffixes.</summary>
	public const bool PlatformSupportsBackups = true;

	[InstancedStatic]
	public static bool IsProcessing;

	[InstancedStatic]
	public static bool CancelToTitle;

	public Farmer player;

	public List<Farmer> farmhands;

	public List<GameLocation> locations;

	public string currentSeason;

	public string samBandName;

	public string elliottBookName;

	/// <summary>Obsolete. This is only kept to preserve data from old save files.</summary>
	[XmlArray("mailbox")]
	public List<string> obsolete_mailbox;

	public HashSet<string> broadcastedMail;

	public HashSet<string> constructedBuildings;

	public HashSet<string> worldStateIDs;

	public int lostBooksFound = -1;

	public int goldenWalnuts = -1;

	public int goldenWalnutsFound;

	public int miniShippingBinsObtained;

	public bool mineShrineActivated;

	public bool skullShrineActivated;

	public bool goldenCoconutCracked;

	public bool parrotPlatformsUnlocked;

	public bool farmPerfect;

	public List<string> foundBuriedNuts = new List<string>();

	public List<string> checkedGarbage = new List<string>();

	public int visitsUntilY1Guarantee = -1;

	public Game1.MineChestType shuffleMineChests;

	public int dayOfMonth;

	public int year;

	public int? countdownToWedding;

	public double dailyLuck;

	public ulong uniqueIDForThisGame;

	public bool weddingToday;

	public bool isRaining;

	public bool isDebrisWeather;

	public bool isLightning;

	public bool isSnowing;

	public bool shouldSpawnMonsters;

	public bool hasApplied1_3_UpdateChanges;

	public bool hasApplied1_4_UpdateChanges;

	public List<long> weddingsToday;

	/// <summary>Obsolete. This is only kept to preserve data from old save files.</summary>
	[XmlElement("stats")]
	public Stats obsolete_stats;

	[InstancedStatic]
	public static SaveGame loaded;

	public float musicVolume;

	public float soundVolume;

	public Object dishOfTheDay;

	public int highestPlayerLimit = -1;

	public int moveBuildingPermissionMode;

	public bool useLegacyRandom;

	public bool allowChatCheats;

	public bool hasDedicatedHost;

	public SerializableDictionary<string, LocationWeather> locationWeather;

	[XmlArrayItem("item")]
	public SaveablePair<string, BuilderData>[] builders;

	[XmlArrayItem("item")]
	public SaveablePair<string, string>[] bannedUsers = LegacyShims.EmptyArray<SaveablePair<string, string>>();

	[XmlArrayItem("item")]
	public SaveablePair<string, string>[] bundleData = LegacyShims.EmptyArray<SaveablePair<string, string>>();

	[XmlArrayItem("item")]
	public SaveablePair<string, int>[] limitedNutDrops = LegacyShims.EmptyArray<SaveablePair<string, int>>();

	public long latestID;

	public Options options;

	[XmlArrayItem("item")]
	public SaveablePair<long, Options>[] splitscreenOptions = LegacyShims.EmptyArray<SaveablePair<long, Options>>();

	public SerializableDictionary<string, string> CustomData = new SerializableDictionary<string, string>();

	[XmlArrayItem("item")]
	public SaveablePair<int, MineInfo>[] mine_permanentMineChanges;

	public int mine_lowestLevelReached;

	public string weatherForTomorrow;

	public string whichFarm;

	public int mine_lowestLevelReachedForOrder = -1;

	public int skullCavesDifficulty;

	public int minesDifficulty;

	public int currentGemBirdIndex;

	public NetLeaderboards junimoKartLeaderboards;

	public List<SpecialOrder> specialOrders;

	public List<SpecialOrder> availableSpecialOrders;

	public List<string> completedSpecialOrders;

	public List<string> acceptedSpecialOrderTypes = new List<string>();

	public List<Item> returnedDonations;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.SaveGame.globalInventories" /> instead.</summary>
	public List<Item> junimoChest;

	public Item[] shippingBin = LegacyShims.EmptyArray<Item>();

	/// <inheritdoc cref="F:StardewValley.FarmerTeam.globalInventories" />
	[XmlArrayItem("item")]
	public SaveablePair<string, Item[]>[] globalInventories = LegacyShims.EmptyArray<SaveablePair<string, Item[]>>();

	public List<string> collectedNutTracker = new List<string>();

	[XmlArrayItem("item")]
	public SaveablePair<FarmerPair, Friendship>[] farmerFriendships = LegacyShims.EmptyArray<SaveablePair<FarmerPair, Friendship>>();

	[XmlArrayItem("item")]
	public SaveablePair<int, long>[] cellarAssignments = LegacyShims.EmptyArray<SaveablePair<int, long>>();

	public int timesFedRaccoons;

	public int treasureTotemsUsed;

	public int perfectionWaivers;

	public int seasonOfCurrentRaccoonBundle;

	public bool[] raccoonBundles = new bool[2];

	public bool activatedGoldenParrot;

	public int daysPlayedWhenLastRaccoonBundleWasFinished;

	public int lastAppliedSaveFix;

	public string gameVersion = Game1.version;

	public string gameVersionLabel;

	public static XmlSerializer serializer = new XmlSerializer(typeof(SaveGame), new Type[5]
	{
		typeof(Character),
		typeof(GameLocation),
		typeof(Item),
		typeof(Quest),
		typeof(TerrainFeature)
	});

	public static XmlSerializer farmerSerializer = new XmlSerializer(typeof(Farmer), new Type[1] { typeof(Item) });

	public static XmlSerializer locationSerializer = new XmlSerializer(typeof(GameLocation), new Type[3]
	{
		typeof(Character),
		typeof(Item),
		typeof(TerrainFeature)
	});

	public static XmlSerializer descriptionElementSerializer = new XmlSerializer(typeof(DescriptionElement), new Type[2]
	{
		typeof(Character),
		typeof(Item)
	});

	public static XmlSerializer legacyDescriptionElementSerializer = new XmlSerializer(typeof(SaveMigrator_1_6.LegacyDescriptionElement), new Type[3]
	{
		typeof(DescriptionElement),
		typeof(Character),
		typeof(Item)
	});

	/// <summary>Get whether a fix was applied to the loaded data before it was last saved.</summary>
	/// <param name="fix">The save fix to check.</param>
	public bool HasSaveFix(SaveFixes fix)
	{
		return lastAppliedSaveFix >= (int)fix;
	}

	public static IEnumerator<int> Save()
	{
		IsProcessing = true;
		if (LocalMultiplayer.IsLocalMultiplayer())
		{
			IEnumerator<int> save = getSaveEnumerator();
			while (save.MoveNext())
			{
				yield return save.Current;
			}
			yield return 100;
			yield break;
		}
		LogVerbose("SaveGame.Save() called.");
		yield return 1;
		IEnumerator<int> loader = getSaveEnumerator();
		Task saveTask = new Task(delegate
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			if (loader != null)
			{
				while (loader.MoveNext() && loader.Current < 100)
				{
				}
			}
		});
		Game1.hooks.StartTask(saveTask, "Save");
		while (!saveTask.IsCanceled && !saveTask.IsCompleted && !saveTask.IsFaulted)
		{
			yield return 1;
		}
		IsProcessing = false;
		if (saveTask.IsFaulted)
		{
			Exception baseException = saveTask.Exception.GetBaseException();
			LogError("saveTask failed with an exception", baseException);
			if (!(baseException is TaskCanceledException))
			{
				throw baseException;
			}
			Game1.ExitToTitle();
		}
		else
		{
			LogVerbose("SaveGame.Save() completed without exceptions.");
			yield return 100;
		}
	}

	public static string FilterFileName(string fileName)
	{
		StringBuilder stringBuilder = new StringBuilder(fileName.Length);
		string text = fileName;
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c))
			{
				stringBuilder.Append(c);
			}
		}
		fileName = stringBuilder.ToString();
		return fileName;
	}

	/// <summary>Get an enumerator which writes the loaded save to a save file.</summary>
	/// <returns>Returns an enumeration of incrementing progress values between 0 and 100.</returns>
	public static IEnumerator<int> getSaveEnumerator()
	{
		if (CancelToTitle)
		{
			throw new TaskCanceledException();
		}
		yield return 1;
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			allFarmer.UnapplyAllTrinketEffects();
		}
		Game1.player.gameVersion = Game1.version;
		Game1.player.gameVersionLabel = Game1.versionLabel;
		foreach (GameLocation location in Game1.locations)
		{
			location.cleanupBeforeSave();
		}
		Game1.player.team.globalInventories.RemoveWhere((KeyValuePair<string, Inventory> p) => !p.Value.HasAny());
		SaveGame saveData = new SaveGame
		{
			player = Game1.player,
			farmhands = new List<Farmer>(Game1.netWorldState.Value.farmhandData.Values),
			locations = new List<GameLocation>(Game1.locations),
			currentSeason = Game1.currentSeason,
			samBandName = Game1.samBandName,
			broadcastedMail = new HashSet<string>(Game1.player.team.broadcastedMail),
			constructedBuildings = new HashSet<string>(Game1.player.team.constructedBuildings),
			bannedUsers = Game1.bannedUsers.ToSaveableArray(),
			skullCavesDifficulty = Game1.netWorldState.Value.SkullCavesDifficulty,
			minesDifficulty = Game1.netWorldState.Value.MinesDifficulty,
			visitsUntilY1Guarantee = Game1.netWorldState.Value.VisitsUntilY1Guarantee,
			shuffleMineChests = Game1.netWorldState.Value.ShuffleMineChests,
			elliottBookName = Game1.elliottBookName,
			dayOfMonth = Game1.dayOfMonth,
			year = Game1.year,
			dailyLuck = Game1.player.team.sharedDailyLuck.Value,
			isRaining = Game1.isRaining,
			isLightning = Game1.isLightning,
			isSnowing = Game1.isSnowing,
			isDebrisWeather = Game1.isDebrisWeather,
			shouldSpawnMonsters = Game1.spawnMonstersAtNight,
			specialOrders = Game1.player.team.specialOrders.ToList(),
			availableSpecialOrders = Game1.player.team.availableSpecialOrders.ToList(),
			completedSpecialOrders = Game1.player.team.completedSpecialOrders.ToList(),
			collectedNutTracker = Game1.player.team.collectedNutTracker.ToList(),
			acceptedSpecialOrderTypes = Game1.player.team.acceptedSpecialOrderTypes.ToList(),
			returnedDonations = Game1.player.team.returnedDonations.ToList(),
			weddingToday = Game1.weddingToday,
			weddingsToday = Game1.weddingsToday.ToList(),
			shippingBin = Game1.getFarm().getShippingBin(Game1.player).ToArray(),
			globalInventories = DictionarySaver<string, Item[]>.ArrayFrom(Game1.player.team.globalInventories.FieldDict, (NetRef<Inventory> value) => value.Value.ToArray()),
			whichFarm = Game1.GetFarmTypeID(),
			junimoKartLeaderboards = Game1.player.team.junimoKartScores,
			lastAppliedSaveFix = 98,
			locationWeather = SerializableDictionary<string, LocationWeather>.BuildFrom(Game1.netWorldState.Value.LocationWeather.FieldDict, (NetRef<LocationWeather> value) => value.Value),
			builders = DictionarySaver<string, BuilderData>.ArrayFrom(Game1.netWorldState.Value.Builders.FieldDict, (NetRef<BuilderData> value) => value.Value),
			cellarAssignments = DictionarySaver<int, long>.ArrayFrom(Game1.player.team.cellarAssignments.FieldDict, (NetLong value) => value.Value),
			uniqueIDForThisGame = Game1.uniqueIDForThisGame,
			musicVolume = Game1.options.musicVolumeLevel,
			soundVolume = Game1.options.soundVolumeLevel,
			mine_lowestLevelReached = Game1.netWorldState.Value.LowestMineLevel,
			mine_lowestLevelReachedForOrder = Game1.netWorldState.Value.LowestMineLevelForOrder,
			currentGemBirdIndex = Game1.currentGemBirdIndex,
			mine_permanentMineChanges = MineShaft.permanentMineChanges.ToSaveableArray(),
			dishOfTheDay = Game1.dishOfTheDay,
			latestID = (long)Game1.multiplayer.latestID,
			highestPlayerLimit = Game1.netWorldState.Value.HighestPlayerLimit,
			options = Game1.options,
			splitscreenOptions = Game1.splitscreenOptions.ToSaveableArray(),
			CustomData = Game1.CustomData,
			worldStateIDs = Game1.worldStateIDs,
			weatherForTomorrow = Game1.weatherForTomorrow,
			goldenWalnuts = Game1.netWorldState.Value.GoldenWalnuts,
			goldenWalnutsFound = Game1.netWorldState.Value.GoldenWalnutsFound,
			miniShippingBinsObtained = Game1.netWorldState.Value.MiniShippingBinsObtained,
			goldenCoconutCracked = Game1.netWorldState.Value.GoldenCoconutCracked,
			parrotPlatformsUnlocked = Game1.netWorldState.Value.ParrotPlatformsUnlocked,
			farmPerfect = Game1.player.team.farmPerfect.Value,
			lostBooksFound = Game1.netWorldState.Value.LostBooksFound,
			foundBuriedNuts = Game1.netWorldState.Value.FoundBuriedNuts.ToList(),
			checkedGarbage = Game1.netWorldState.Value.CheckedGarbage.ToList(),
			mineShrineActivated = Game1.player.team.mineShrineActivated.Value,
			skullShrineActivated = Game1.player.team.skullShrineActivated.Value,
			timesFedRaccoons = Game1.netWorldState.Value.TimesFedRaccoons,
			treasureTotemsUsed = Game1.netWorldState.Value.TreasureTotemsUsed,
			perfectionWaivers = Game1.netWorldState.Value.PerfectionWaivers,
			seasonOfCurrentRaccoonBundle = Game1.netWorldState.Value.SeasonOfCurrentRacconBundle,
			raccoonBundles = Game1.netWorldState.Value.raccoonBundles.ToArray(),
			activatedGoldenParrot = Game1.netWorldState.Value.ActivatedGoldenParrot,
			daysPlayedWhenLastRaccoonBundleWasFinished = Game1.netWorldState.Value.DaysPlayedWhenLastRaccoonBundleWasFinished,
			gameVersion = Game1.version,
			gameVersionLabel = Game1.versionLabel,
			limitedNutDrops = DictionarySaver<string, int>.ArrayFrom(Game1.player.team.limitedNutDrops.FieldDict, (NetInt value) => value.Value),
			bundleData = Game1.netWorldState.Value.BundleData.ToSaveableArray(),
			moveBuildingPermissionMode = (int)Game1.player.team.farmhandsCanMoveBuildings.Value,
			useLegacyRandom = Game1.player.team.useLegacyRandom.Value,
			allowChatCheats = Game1.player.team.allowChatCheats.Value,
			hasDedicatedHost = Game1.player.team.hasDedicatedHost.Value,
			hasApplied1_3_UpdateChanges = true,
			hasApplied1_4_UpdateChanges = true,
			farmerFriendships = DictionarySaver<FarmerPair, Friendship>.ArrayFrom(Game1.player.team.friendshipData.FieldDict, (NetRef<Friendship> value) => value.Value)
		};
		string text = FilterFileName(Game1.GetSaveGameName()) + "_" + Game1.uniqueIDForThisGame;
		string path = Path.Combine(Program.GetSavesFolder(), text + Path.DirectorySeparatorChar);
		string finalFarmerPath = Path.Combine(path, "SaveGameInfo");
		string finalDataPath = Path.Combine(path, text);
		string tempFarmerPath = finalFarmerPath + "_STARDEWVALLEYSAVETMP";
		string tempDataPath = finalDataPath + "_STARDEWVALLEYSAVETMP";
		ensureFolderStructureExists();
		StardewValley.Pokemon.PlayerParty.Instance.SaveParty();
		StardewValley.Pokemon.PokemonStorage.Instance.SaveStorage();
		Stream fstream = null;
		try
		{
			fstream = File.Open(tempDataPath, FileMode.Create);
		}
		catch (IOException ex)
		{
			if (fstream != null)
			{
				fstream.Close();
				fstream.Dispose();
			}
			Game1.gameMode = 9;
			Game1.debugOutput = Game1.parseText(ex.Message);
			yield break;
		}
		MemoryStream mstream1 = new MemoryStream(1024);
		new MemoryStream(1024);
		if (CancelToTitle)
		{
			throw new TaskCanceledException();
		}
		yield return 2;
		LogVerbose("Saving without compression...");
		XmlWriterSettings settings = new XmlWriterSettings
		{
			CloseOutput = false
		};
		XmlWriter xmlWriter = XmlWriter.Create(mstream1, settings);
		xmlWriter.WriteStartDocument();
		SaveSerializer.Serialize(xmlWriter, saveData);
		xmlWriter.WriteEndDocument();
		xmlWriter.Flush();
		xmlWriter.Close();
		mstream1.Close();
		byte[] buffer1 = mstream1.ToArray();
		if (CancelToTitle)
		{
			throw new TaskCanceledException();
		}
		yield return 2;
		fstream.Write(buffer1, 0, buffer1.Length);
		fstream.Close();
		Game1.player.saveTime = (int)(DateTime.UtcNow - new DateTime(2012, 6, 22)).TotalMinutes;
		try
		{
			fstream = File.Open(tempFarmerPath, FileMode.Create);
		}
		catch (IOException ex2)
		{
			fstream?.Close();
			Game1.gameMode = 9;
			Game1.debugOutput = Game1.parseText(ex2.Message);
			yield break;
		}
		Stream stream = fstream;
		XmlWriter xmlWriter2 = XmlWriter.Create(stream, settings);
		xmlWriter2.WriteStartDocument();
		SaveSerializer.Serialize(xmlWriter2, Game1.player);
		xmlWriter2.WriteEndDocument();
		xmlWriter2.Flush();
		xmlWriter2.Close();
		stream.Close();
		fstream.Close();
		if (CancelToTitle)
		{
			throw new TaskCanceledException();
		}
		yield return 2;
		string destFilePath = finalDataPath + "_old";
		string destFilePath2 = finalFarmerPath + "_old";
		try
		{
			LegacyShims.MoveFileWithOverwrite(finalDataPath, destFilePath);
			LegacyShims.MoveFileWithOverwrite(finalFarmerPath, destFilePath2);
		}
		catch
		{
		}
		LegacyShims.MoveFileWithOverwrite(tempDataPath, finalDataPath);
		LegacyShims.MoveFileWithOverwrite(tempFarmerPath, finalFarmerPath);
		foreach (Farmer allFarmer2 in Game1.getAllFarmers())
		{
			allFarmer2.resetAllTrinketEffects();
		}
		Game1.player.sleptInTemporaryBed.Value = false;
		if (CancelToTitle)
		{
			throw new TaskCanceledException();
		}
		yield return 100;
	}

	public static bool IsNewGameSaveNameCollision(string save_name)
	{
		string path = FilterFileName(save_name) + "_" + Game1.uniqueIDForThisGame;
		return Directory.Exists(Path.Combine(Program.GetSavesFolder(), path));
	}

	public static void ensureFolderStructureExists()
	{
		string path = FilterFileName(Game1.GetSaveGameName()) + "_" + Game1.uniqueIDForThisGame;
		Directory.CreateDirectory(Path.Combine(Program.GetSavesFolder(), path));
	}

	public static void Load(string filename)
	{
		Game1.gameMode = 6;
		Game1.loadingMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGame.cs.4690");
		Game1.currentLoader = getLoadEnumerator(filename);
	}

	public static void LoadFarmType()
	{
		List<ModFarmType> list = DataLoader.AdditionalFarms(Game1.content);
		Game1.whichFarm = -1;
		if (list != null)
		{
			foreach (ModFarmType item in list)
			{
				if (item.Id == loaded.whichFarm)
				{
					Game1.whichModFarm = item;
					Game1.whichFarm = 7;
					break;
				}
			}
		}
		if (loaded.whichFarm == null)
		{
			Game1.whichFarm = 0;
		}
		if (Game1.whichFarm < 0)
		{
			if (int.TryParse(loaded.whichFarm, out var result))
			{
				Game1.whichFarm = result;
				return;
			}
			LogWarn("Ignored unknown farm type '" + loaded.whichFarm + "' which no longer exists in the data.");
			Game1.whichFarm = 0;
			Game1.whichModFarm = null;
		}
	}

	/// <summary>Read a raw save file, if it's valid.</summary>
	/// <param name="file">The save folder name to load, in the form <c>{farmer name}_{unique id}</c>.</param>
	/// <param name="fileNameSuffix">The suffix for the filename within the save folder to load, if supported by the platform. This should usually be <c>null</c> (main file), <see cref="F:StardewValley.SaveGame.BackupNameSuffix" />, or <see cref="F:StardewValley.SaveGame.TempNameSuffix" />.</param>
	/// <param name="error">An error indicating why loading the save file failed, if applicable.</param>
	/// <remarks>This is a low-level method. Most code should use <see cref="M:StardewValley.SaveGame.getLoadEnumerator(System.String)" /> instead.</remarks>
	public static SaveGame TryReadSaveFile(string file, string fileNameSuffix, out string error)
	{
		string text = Path.Combine(Program.GetSavesFolder(), file, file + fileNameSuffix);
		if (!File.Exists(text))
		{
			text += ".xml";
			if (!File.Exists(text))
			{
				return FileDoesNotExist(out error);
			}
		}
		Stream stream = null;
		try
		{
			stream = new MemoryStream(File.ReadAllBytes(text), writable: false);
		}
		catch (IOException ex)
		{
			error = ex.Message;
			stream?.Close();
			return null;
		}
		byte b = (byte)stream.ReadByte();
		stream.Position--;
		if (b == 120)
		{
			LogVerbose("zlib stream detected...");
			stream = new ZlibStream(stream, CompressionMode.Decompress);
		}
		try
		{
			error = null;
			return SaveSerializer.Deserialize<SaveGame>(stream);
		}
		catch (Exception ex2)
		{
			error = ex2.Message;
			return null;
		}
		finally
		{
			stream.Dispose();
		}
		static SaveGame FileDoesNotExist(out string outError)
		{
			outError = "File does not exist";
			return null;
		}
	}

	/// <summary>Read a raw save file with automatic fallback to the backup files, if any of them are valid.</summary>
	/// <param name="file">The save folder name to load, in the form <c>{farmer name}_{unique id}</c>.</param>
	/// <param name="error">An error indicating why loading the save file failed, if applicable.</param>
	/// <param name="autoRecovered">Whether the save was auto-recovered by loading a backup.</param>
	/// <remarks>This is a low-level method. Most code should use <see cref="M:StardewValley.SaveGame.getLoadEnumerator(System.String)" /> instead.</remarks>
	public static SaveGame TryReadSaveFileWithFallback(string file, out string error, out bool autoRecovered)
	{
		SaveGame saveGame = TryReadSaveFile(file, null, out error);
		if (saveGame != null)
		{
			error = null;
			autoRecovered = false;
			return saveGame;
		}
		saveGame = TryReadSaveFile(file, "_old", out var error2);
		if (saveGame != null)
		{
			error = null;
			autoRecovered = true;
			return saveGame;
		}
		saveGame = TryReadSaveFile(file, "_STARDEWVALLEYSAVETMP", out error2);
		if (saveGame != null)
		{
			error = null;
			autoRecovered = true;
			return saveGame;
		}
		error = error ?? "Save could not be loaded";
		autoRecovered = false;
		return null;
	}

	/// <summary>Get an enumerator which loads a save file.</summary>
	/// <param name="file">The save folder name to load, in the form <c>{farmer name}_{unique id}</c>.</param>
	/// <returns>Returns an enumeration of incrementing progress values between 0 and 100.</returns>
	public static IEnumerator<int> getLoadEnumerator(string file)
	{
		LogVerbose("getLoadEnumerator('" + file + "')");
		Stopwatch stopwatch = Stopwatch.StartNew();
		Game1.SetSaveName(Path.GetFileNameWithoutExtension(file).Split('_').FirstOrDefault());
		Game1.loadingMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGame.cs.4690");
		IsProcessing = true;
		if (CancelToTitle)
		{
			Game1.ExitToTitle();
		}
		yield return 1;
		string error = null;
		bool autoRecovered = false;
		Task readSaveTask = new Task(delegate
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			loaded = TryReadSaveFileWithFallback(file, out error, out autoRecovered);
		});
		Game1.hooks.StartTask(readSaveTask, "Load_ReadSave");
		while (!readSaveTask.IsCanceled && !readSaveTask.IsCompleted && !readSaveTask.IsFaulted)
		{
			yield return 15;
		}
		if (loaded == null)
		{
			Game1.gameMode = 9;
			Game1.debugOutput = Game1.parseText(error);
			yield break;
		}
		if (autoRecovered)
		{
			LogWarn("Save file " + file + " was corrupted; auto-recovered it from the backup.");
		}
		yield return 19;
		Game1.hasApplied1_3_UpdateChanges = loaded.hasApplied1_3_UpdateChanges;
		Game1.hasApplied1_4_UpdateChanges = loaded.hasApplied1_4_UpdateChanges;
		Game1.lastAppliedSaveFix = (SaveFixes)loaded.lastAppliedSaveFix;
		Game1.player.team.useLegacyRandom.Value = loaded.useLegacyRandom;
		Game1.loadingMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGame.cs.4697");
		if (CancelToTitle)
		{
			Game1.ExitToTitle();
		}
		yield return 20;
		LoadFarmType();
		Game1.year = loaded.year;
		Game1.netWorldState.Value.CurrentPlayerLimit = Game1.multiplayer.playerLimit;
		if (loaded.highestPlayerLimit >= 0)
		{
			Game1.netWorldState.Value.HighestPlayerLimit = loaded.highestPlayerLimit;
		}
		else
		{
			Game1.netWorldState.Value.HighestPlayerLimit = Math.Max(Game1.netWorldState.Value.HighestPlayerLimit, Game1.multiplayer.MaxPlayers);
		}
		Game1.uniqueIDForThisGame = loaded.uniqueIDForThisGame;
		if (LocalMultiplayer.IsLocalMultiplayer())
		{
			Game1.game1.loadForNewGame(loadedGame: true);
		}
		else
		{
			readSaveTask = new Task(delegate
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				Game1.game1.loadForNewGame(loadedGame: true);
			});
			Game1.hooks.StartTask(readSaveTask, "Load_LoadForNewGame");
			while (!readSaveTask.IsCanceled && !readSaveTask.IsCompleted && !readSaveTask.IsFaulted)
			{
				yield return 24;
			}
			if (readSaveTask.IsFaulted)
			{
				Exception baseException = readSaveTask.Exception.GetBaseException();
				LogError("loadNewGameTask failed with an exception.", baseException);
				throw baseException;
			}
			if (CancelToTitle)
			{
				Game1.ExitToTitle();
			}
			yield return 25;
		}
		Game1.weatherForTomorrow = (int.TryParse(loaded.weatherForTomorrow, out var result) ? Utility.LegacyWeatherToWeather(result) : loaded.weatherForTomorrow);
		Game1.dayOfMonth = loaded.dayOfMonth;
		Game1.year = loaded.year;
		Game1.currentSeason = loaded.currentSeason;
		Game1.worldStateIDs = loaded.worldStateIDs;
		Game1.loadingMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGame.cs.4698");
		if (loaded.mine_permanentMineChanges != null)
		{
			MineShaft.permanentMineChanges = new SerializableDictionary<int, MineInfo>(loaded.mine_permanentMineChanges.ToDictionary());
			Game1.netWorldState.Value.LowestMineLevel = loaded.mine_lowestLevelReached;
			Game1.netWorldState.Value.LowestMineLevelForOrder = loaded.mine_lowestLevelReachedForOrder;
		}
		Game1.currentGemBirdIndex = loaded.currentGemBirdIndex;
		if (loaded.bundleData.Length != 0)
		{
			Dictionary<string, string> dictionary = loaded.bundleData.ToDictionary();
			if (!loaded.HasSaveFix(SaveFixes.StandardizeBundleFields))
			{
				SaveMigrator_1_6.StandardizeBundleFields(dictionary);
			}
			Game1.netWorldState.Value.SetBundleData(dictionary);
		}
		if (CancelToTitle)
		{
			Game1.ExitToTitle();
		}
		yield return 26;
		Game1.isRaining = loaded.isRaining;
		Game1.isLightning = loaded.isLightning;
		Game1.isSnowing = loaded.isSnowing;
		Game1.isGreenRain = Utility.isGreenRainDay();
		if (Game1.IsMasterGame)
		{
			Game1.netWorldState.Value.UpdateFromGame1();
		}
		if (loaded.locationWeather != null)
		{
			Game1.netWorldState.Value.LocationWeather.Clear();
			foreach (KeyValuePair<string, LocationWeather> item in loaded.locationWeather)
			{
				Game1.netWorldState.Value.LocationWeather[item.Key] = item.Value;
			}
		}
		if (loaded.builders != null)
		{
			SaveablePair<string, BuilderData>[] array = loaded.builders;
			for (int num = 0; num < array.Length; num++)
			{
				SaveablePair<string, BuilderData> saveablePair = array[num];
				Game1.netWorldState.Value.Builders[saveablePair.Key] = saveablePair.Value;
			}
		}
		if (LocalMultiplayer.IsLocalMultiplayer())
		{
			loadDataToFarmer(loaded.player);
		}
		else
		{
			readSaveTask = new Task(delegate
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				loadDataToFarmer(loaded.player);
			});
			Game1.hooks.StartTask(readSaveTask, "Load_Farmer");
			while (!readSaveTask.IsCanceled && !readSaveTask.IsCompleted && !readSaveTask.IsFaulted)
			{
				yield return 1;
			}
			if (readSaveTask.IsFaulted)
			{
				Exception baseException2 = readSaveTask.Exception.GetBaseException();
				LogError("loadFarmerTask failed with an exception", baseException2);
				throw baseException2;
			}
		}
		Game1.player = loaded.player;
		Game1.player.team.useLegacyRandom.Value = loaded.useLegacyRandom;
		Game1.player.team.allowChatCheats.Value = loaded.allowChatCheats;
		Game1.player.team.hasDedicatedHost.Value = loaded.hasDedicatedHost;
		Game1.netWorldState.Value.farmhandData.Clear();
		if (Game1.lastAppliedSaveFix < SaveFixes.MigrateFarmhands)
		{
			SaveMigrator_1_6.MigrateFarmhands(loaded.locations);
		}
		if (loaded.farmhands != null)
		{
			foreach (Farmer farmhand in loaded.farmhands)
			{
				Game1.netWorldState.Value.farmhandData[farmhand.UniqueMultiplayerID] = farmhand;
			}
		}
		foreach (Farmer value2 in Game1.netWorldState.Value.farmhandData.Values)
		{
			loadDataToFarmer(value2);
		}
		if (Game1.MasterPlayer.hasOrWillReceiveMail("leoMoved") && Game1.getLocationFromName("Mountain") is Mountain mountain)
		{
			mountain.reloadMap();
			mountain.ApplyTreehouseIfNecessary();
			if (mountain.treehouseDoorDirty)
			{
				mountain.treehouseDoorDirty = false;
				WarpPathfindingCache.PopulateCache();
			}
		}
		if (loaded.farmerFriendships != null)
		{
			SaveablePair<FarmerPair, Friendship>[] array2 = loaded.farmerFriendships;
			for (int num = 0; num < array2.Length; num++)
			{
				SaveablePair<FarmerPair, Friendship> saveablePair2 = array2[num];
				Game1.player.team.friendshipData[saveablePair2.Key] = saveablePair2.Value;
			}
		}
		Game1.spawnMonstersAtNight = loaded.shouldSpawnMonsters;
		Game1.player.team.limitedNutDrops.Clear();
		if (Game1.netWorldState != null && Game1.netWorldState.Value != null)
		{
			Game1.netWorldState.Value.RegisterSpecialCurrencies();
		}
		if (loaded.limitedNutDrops != null)
		{
			SaveablePair<string, int>[] array3 = loaded.limitedNutDrops;
			for (int num = 0; num < array3.Length; num++)
			{
				SaveablePair<string, int> saveablePair3 = array3[num];
				if (saveablePair3.Value > 0)
				{
					Game1.player.team.limitedNutDrops[saveablePair3.Key] = saveablePair3.Value;
				}
			}
		}
		Game1.player.team.completedSpecialOrders.Clear();
		Game1.player.team.completedSpecialOrders.AddRange(loaded.completedSpecialOrders);
		Game1.player.team.specialOrders.Clear();
		foreach (SpecialOrder specialOrder in loaded.specialOrders)
		{
			if (specialOrder != null)
			{
				Game1.player.team.specialOrders.Add(specialOrder);
			}
		}
		Game1.player.team.availableSpecialOrders.Clear();
		foreach (SpecialOrder availableSpecialOrder in loaded.availableSpecialOrders)
		{
			if (availableSpecialOrder != null)
			{
				Game1.player.team.availableSpecialOrders.Add(availableSpecialOrder);
			}
		}
		Game1.player.team.acceptedSpecialOrderTypes.Clear();
		Game1.player.team.acceptedSpecialOrderTypes.AddRange(loaded.acceptedSpecialOrderTypes);
		Game1.player.team.collectedNutTracker.Clear();
		Game1.player.team.collectedNutTracker.AddRange(loaded.collectedNutTracker);
		Game1.player.team.globalInventories.Clear();
		if (loaded.globalInventories != null)
		{
			SaveablePair<string, Item[]>[] array4 = loaded.globalInventories;
			for (int num = 0; num < array4.Length; num++)
			{
				SaveablePair<string, Item[]> saveablePair4 = array4[num];
				Game1.player.team.GetOrCreateGlobalInventory(saveablePair4.Key).AddRange(saveablePair4.Value);
			}
		}
		List<Item> list = loaded.junimoChest;
		if (list != null && list.Count > 0)
		{
			Game1.player.team.GetOrCreateGlobalInventory("JunimoChests").AddRange(loaded.junimoChest);
		}
		Game1.player.team.returnedDonations.Clear();
		foreach (Item returnedDonation in loaded.returnedDonations)
		{
			Game1.player.team.returnedDonations.Add(returnedDonation);
		}
		if (loaded.obsolete_stats != null)
		{
			Game1.player.stats = loaded.obsolete_stats;
		}
		if (loaded.obsolete_mailbox != null && !Game1.player.mailbox.Any())
		{
			Game1.player.mailbox.AddRange(loaded.obsolete_mailbox);
		}
		Game1.random = Utility.CreateDaySaveRandom(1.0);
		Game1.loadingMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGame.cs.4699");
		if (CancelToTitle)
		{
			Game1.ExitToTitle();
		}
		yield return 36;
		Game1.UpdatePassiveFestivalStates();
		if (loaded.cellarAssignments != null)
		{
			SaveablePair<int, long>[] array5 = loaded.cellarAssignments;
			for (int num = 0; num < array5.Length; num++)
			{
				SaveablePair<int, long> saveablePair5 = array5[num];
				Game1.player.team.cellarAssignments[saveablePair5.Key] = saveablePair5.Value;
			}
		}
		if (LocalMultiplayer.IsLocalMultiplayer())
		{
			loadDataToLocations(loaded.locations);
		}
		else
		{
			readSaveTask = new Task(delegate
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				loadDataToLocations(loaded.locations);
			});
			Game1.hooks.StartTask(readSaveTask, "Load_Locations");
			while (!readSaveTask.IsCanceled && !readSaveTask.IsCompleted && !readSaveTask.IsFaulted)
			{
				yield return 1;
			}
			if (readSaveTask.IsFaulted)
			{
				Exception baseException3 = readSaveTask.Exception.GetBaseException();
				LogError("loadLocationsTask failed with an exception", baseException3);
				throw readSaveTask.Exception.GetBaseException();
			}
		}
		if (loaded.shippingBin != null)
		{
			Game1.getFarm().getShippingBin(Game1.player).Clear();
			Game1.getFarm().getShippingBin(Game1.player).AddRange(loaded.shippingBin);
		}
		if (Game1.getLocationFromName("Railroad") is Railroad railroad)
		{
			railroad.ResetTrainForNewDay();
		}
		HashSet<long> validFarmhands = new HashSet<long>();
		Utility.ForEachBuilding(delegate(Building building)
		{
			if (building?.GetIndoors() is Cabin cabin)
			{
				validFarmhands.Add(cabin.farmhandReference.UID);
			}
			return true;
		});
		List<Farmer> list2 = new List<Farmer>();
		foreach (Farmer value3 in Game1.netWorldState.Value.farmhandData.Values)
		{
			if (!value3.isCustomized.Value && !validFarmhands.Contains(value3.UniqueMultiplayerID))
			{
				list2.Add(value3);
			}
		}
		foreach (Farmer item2 in list2)
		{
			Game1.player.team.DeleteFarmhand(item2);
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			int money = allFarmer.Money;
			if (!Game1.player.team.individualMoney.TryGetValue(allFarmer.UniqueMultiplayerID, out var value))
			{
				value = (Game1.player.team.individualMoney[allFarmer.UniqueMultiplayerID] = new NetIntDelta(money));
			}
			value.Value = money;
		}
		Game1.updateCellarAssignments();
		foreach (GameLocation location in Game1.locations)
		{
			foreach (Building building in location.buildings)
			{
				GameLocation indoors = building.GetIndoors();
				if (indoors != null)
				{
					if (indoors is FarmHouse farmHouse)
					{
						farmHouse.updateCellarWarps();
					}
					indoors.parentLocationName.Value = location.NameOrUniqueName;
				}
			}
			if (location is FarmHouse farmHouse2)
			{
				farmHouse2.updateCellarWarps();
			}
		}
		foreach (Farmer value4 in Game1.netWorldState.Value.farmhandData.Values)
		{
			Game1.netWorldState.Value.ResetFarmhandState(value4);
		}
		if (CancelToTitle)
		{
			Game1.ExitToTitle();
		}
		yield return 50;
		yield return 51;
		Game1.isDebrisWeather = loaded.isDebrisWeather;
		if (Game1.isDebrisWeather)
		{
			Game1.populateDebrisWeatherArray();
		}
		else
		{
			Game1.debrisWeather.Clear();
		}
		yield return 53;
		Game1.player.team.sharedDailyLuck.Value = loaded.dailyLuck;
		yield return 54;
		yield return 55;
		Game1.setGraphicsForSeason(onLoad: true);
		yield return 56;
		Game1.samBandName = loaded.samBandName;
		Game1.elliottBookName = loaded.elliottBookName;
		yield return 63;
		Game1.weddingToday = loaded.weddingToday;
		Game1.weddingsToday = loaded.weddingsToday.ToList();
		Game1.loadingMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGame.cs.4700");
		yield return 64;
		Game1.loadingMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGame.cs.4701");
		if (CancelToTitle)
		{
			Game1.ExitToTitle();
		}
		yield return 79;
		Game1.options.musicVolumeLevel = loaded.musicVolume;
		Game1.options.soundVolumeLevel = loaded.soundVolume;
		yield return 83;
		if (loaded.countdownToWedding.HasValue && loaded.countdownToWedding.Value != 0 && !string.IsNullOrEmpty(loaded.player.spouse))
		{
			WorldDate worldDate = WorldDate.Now();
			worldDate.TotalDays += loaded.countdownToWedding.Value;
			Friendship friendship = loaded.player.friendshipData[loaded.player.spouse];
			friendship.Status = FriendshipStatus.Engaged;
			friendship.WeddingDate = worldDate;
		}
		yield return 85;
		yield return 87;
		yield return 88;
		yield return 95;
		Game1.fadeToBlack = true;
		Game1.fadeIn = false;
		Game1.fadeToBlackAlpha = 0.99f;
		if (Game1.player.mostRecentBed.X <= 0f)
		{
			Game1.player.Position = new Vector2(192f, 384f);
		}
		Game1.addNewFarmBuildingMaps();
		GameLocation gameLocation = null;
		if (Game1.player.lastSleepLocation.Value != null && Game1.isLocationAccessible(Game1.player.lastSleepLocation.Value))
		{
			gameLocation = Game1.getLocationFromName(Game1.player.lastSleepLocation.Value);
		}
		bool flag = true;
		if (gameLocation != null && gameLocation.CanWakeUpHere(Game1.player))
		{
			Game1.currentLocation = gameLocation;
			Game1.player.currentLocation = Game1.currentLocation;
			Game1.player.Position = Utility.PointToVector2(Game1.player.lastSleepPoint.Value) * 64f;
			flag = false;
		}
		if (flag)
		{
			Game1.currentLocation = Game1.RequireLocation("FarmHouse");
		}
		Game1.currentLocation.map.LoadTileSheets(Game1.mapDisplayDevice);
		Game1.player.CanMove = true;
		Game1.player.ReequipEnchantments();
		if (loaded.junimoKartLeaderboards != null)
		{
			Game1.player.team.junimoKartScores.LoadScores(loaded.junimoKartLeaderboards.GetScores());
		}
		Game1.options = loaded.options;
		Game1.splitscreenOptions = new SerializableDictionary<long, Options>(loaded.splitscreenOptions.ToDictionary());
		Game1.CustomData = loaded.CustomData;
		Game1.player.team.broadcastedMail.Clear();
		if (loaded.broadcastedMail != null)
		{
			Game1.player.team.broadcastedMail.AddRange(loaded.broadcastedMail);
		}
		Game1.player.team.constructedBuildings.Clear();
		if (loaded.constructedBuildings != null)
		{
			Game1.player.team.constructedBuildings.AddRange(loaded.constructedBuildings);
		}
		if (Game1.options == null)
		{
			Game1.options = new Options();
			Game1.options.LoadDefaultOptions();
		}
		else
		{
			if (Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.zh)
			{
				Game1.options.loadChineseFonts();
			}
			else
			{
				Game1.options.dialogueFontScale = 1f;
			}
			Game1.options.platformClampValues();
			Game1.options.SaveDefaultOptions();
		}
		try
		{
			StartupPreferences startupPreferences = new StartupPreferences();
			startupPreferences.loadPreferences(async: false, applyLanguage: false);
			Game1.options.gamepadMode = startupPreferences.gamepadMode;
		}
		catch
		{
		}
		Game1.initializeVolumeLevels();
		Game1.multiplayer.latestID = (ulong)loaded.latestID;
		Game1.netWorldState.Value.SkullCavesDifficulty = loaded.skullCavesDifficulty;
		Game1.netWorldState.Value.MinesDifficulty = loaded.minesDifficulty;
		Game1.netWorldState.Value.VisitsUntilY1Guarantee = loaded.visitsUntilY1Guarantee;
		Game1.netWorldState.Value.ShuffleMineChests = loaded.shuffleMineChests;
		Game1.netWorldState.Value.DishOfTheDay = loaded.dishOfTheDay;
		if (Game1.IsRainingHere())
		{
			Game1.changeMusicTrack("rain", track_interruptable: true);
		}
		Game1.updateWeatherIcon();
		Game1.netWorldState.Value.MiniShippingBinsObtained = loaded.miniShippingBinsObtained;
		Game1.netWorldState.Value.LostBooksFound = loaded.lostBooksFound;
		Game1.netWorldState.Value.GoldenWalnuts = loaded.goldenWalnuts;
		Game1.netWorldState.Value.GoldenWalnutsFound = loaded.goldenWalnutsFound;
		Game1.netWorldState.Value.GoldenCoconutCracked = loaded.goldenCoconutCracked;
		Game1.netWorldState.Value.FoundBuriedNuts.Clear();
		Game1.netWorldState.Value.FoundBuriedNuts.AddRange(loaded.foundBuriedNuts);
		Game1.netWorldState.Value.CheckedGarbage.Clear();
		Game1.netWorldState.Value.CheckedGarbage.AddRange(loaded.checkedGarbage);
		IslandSouth.SetupIslandSchedules();
		Game1.netWorldState.Value.TimesFedRaccoons = loaded.timesFedRaccoons;
		Game1.netWorldState.Value.TreasureTotemsUsed = loaded.treasureTotemsUsed;
		Game1.netWorldState.Value.PerfectionWaivers = loaded.perfectionWaivers;
		Game1.netWorldState.Value.SeasonOfCurrentRacconBundle = loaded.seasonOfCurrentRaccoonBundle;
		Game1.netWorldState.Value.raccoonBundles.Set(loaded.raccoonBundles);
		Game1.netWorldState.Value.ActivatedGoldenParrot = loaded.activatedGoldenParrot;
		Game1.netWorldState.Value.DaysPlayedWhenLastRaccoonBundleWasFinished = loaded.daysPlayedWhenLastRaccoonBundleWasFinished;
		Game1.PerformPassiveFestivalSetup();
		Game1.player.team.farmhandsCanMoveBuildings.Value = (FarmerTeam.RemoteBuildingPermissions)loaded.moveBuildingPermissionMode;
		Game1.player.team.mineShrineActivated.Value = loaded.mineShrineActivated;
		Game1.player.team.skullShrineActivated.Value = loaded.skullShrineActivated;
		if (Game1.multiplayerMode == 2)
		{
			if (Program.sdk.Networking != null && Game1.options.serverPrivacy == ServerPrivacy.InviteOnly)
			{
				Game1.options.setServerMode("invite");
			}
			else if (Program.sdk.Networking != null && Game1.options.serverPrivacy == ServerPrivacy.FriendsOnly)
			{
				Game1.options.setServerMode("friends");
			}
			else
			{
				Game1.options.setServerMode("friends");
			}
		}
		Game1.bannedUsers = new SerializableDictionary<string, string>(loaded.bannedUsers.ToDictionary());
		bool num2 = loaded.lostBooksFound < 0;
		loaded = null;
		Game1.currentLocation.lastTouchActionLocation = Game1.player.Tile;
		if (Game1.player.horseName.Value == null)
		{
			Horse horse = Utility.findHorse(Guid.Empty);
			if (horse != null && horse.displayName != "")
			{
				Game1.player.horseName.Value = horse.displayName;
				horse.ownerId.Value = Game1.player.UniqueMultiplayerID;
			}
		}
		SaveMigrator.ApplySaveFixes();
		if (num2)
		{
			SaveMigrator_1_4.RecalculateLostBookCount();
		}
		foreach (Item item3 in Game1.player.Items)
		{
			(item3 as Object)?.reloadSprite();
		}
		foreach (Trinket trinketItem in Game1.player.trinketItems)
		{
			trinketItem.reloadSprite();
		}
		Game1.gameMode = 3;
		StardewValley.Pokemon.PlayerParty.Instance.Reload();
		StardewValley.Pokemon.PokemonStorage.Instance.Reload();
		Game1.AddNPCs();
		Game1.AddModNPCs();
		Game1.RefreshQuestOfTheDay();
		try
		{
			Game1.fixProblems();
		}
		catch (Exception exception)
		{
			Game1.log.Error("Failed to fix problems.", exception);
		}
		Utility.ForEachBuilding(delegate(Building building)
		{
			if (building is Stable stable)
			{
				stable.grabHorse();
			}
			else
			{
				GameLocation indoors2 = building.GetIndoors();
				if (!(indoors2 is Cabin cabin))
				{
					if (indoors2 is Shed shed)
					{
						shed.updateLayout();
						building.updateInteriorWarps(shed);
					}
				}
				else
				{
					cabin.updateFarmLayout();
				}
			}
			return true;
		});
		Game1.UpdateHorseOwnership();
		Game1.UpdateFarmPerfection();
		Game1.doMorningStuff();
		if (flag && Game1.player.currentLocation is FarmHouse farmHouse3)
		{
			Game1.player.Position = Utility.PointToVector2(farmHouse3.GetPlayerBedSpot()) * 64f;
		}
		BedFurniture.ShiftPositionForBed(Game1.player);
		Game1.stats.checkForAchievements();
		if (Game1.IsMasterGame)
		{
			Game1.netWorldState.Value.UpdateFromGame1();
		}
		LogVerbose("getLoadEnumerator() exited, elapsed = '" + stopwatch.Elapsed.ToString() + "'");
		if (CancelToTitle)
		{
			Game1.ExitToTitle();
		}
		IsProcessing = false;
		Game1.player.currentLocation.lastTouchActionLocation = Game1.player.Tile;
		if (Game1.IsMasterGame)
		{
			Game1.player.currentLocation.hostSetup();
			Game1.player.currentLocation.interiorDoors.ResetSharedState();
		}
		Game1.player.currentLocation.resetForPlayerEntry();
		Game1.player.sleptInTemporaryBed.Value = false;
		Game1.player.showToolUpgradeAvailability();
		Game1.player.resetAllTrinketEffects();
		Game1.dayTimeMoneyBox.questsDirty = true;
		yield return 100;
	}

	public static void loadDataToFarmer(Farmer target)
	{
		target.gameVersion = target.gameVersion;
		target.Items.OverwriteWith(target.Items);
		target.canMove = true;
		target.Sprite = new FarmerSprite(null);
		target.songsHeard.Add("title_day");
		target.songsHeard.Add("title_night");
		target.maxItems.Value = target.maxItems.Value;
		for (int i = 0; i < target.maxItems.Value; i++)
		{
			if (target.Items.Count <= i)
			{
				target.Items.Add(null);
			}
		}
		if (target.FarmerRenderer == null)
		{
			target.FarmerRenderer = new FarmerRenderer(target.getTexture(), target);
		}
		target.changeGender(target.IsMale);
		target.changeAccessory(target.accessory.Value);
		target.changeShirt(target.shirt.Value);
		target.changePantsColor(target.GetPantsColor());
		target.changeSkinColor(target.skin.Value);
		target.changeHairColor(target.hairstyleColor.Value);
		target.changeHairStyle(target.hair.Value);
		target.changeShoeColor(target.shoes.Value);
		target.changeEyeColor(target.newEyeColor.Value);
		target.Stamina = target.Stamina;
		target.health = target.health;
		target.maxStamina.Value = target.maxStamina.Value;
		target.mostRecentBed = target.mostRecentBed;
		target.Position = target.mostRecentBed;
		target.position.X -= 64f;
		if (!Game1.hasApplied1_3_UpdateChanges)
		{
			SaveMigrator_1_3.MigrateFriendshipData(target);
		}
		target.questLog.RemoveWhere((Quest quest) => quest == null);
		target.ConvertClothingOverrideToClothesItems();
		target.UpdateClothing();
		target._lastEquippedTool = target.CurrentTool;
	}

	public static void loadDataToLocations(List<GameLocation> fromLocations)
	{
		Dictionary<string, string> formerLocationNames = GetFormerLocationNames();
		if (formerLocationNames.Count > 0)
		{
			foreach (GameLocation fromLocation in fromLocations)
			{
				foreach (NPC character in fromLocation.characters)
				{
					string defaultMap = character.DefaultMap;
					if (defaultMap != null && formerLocationNames.TryGetValue(defaultMap, out var value))
					{
						LogDebug($"Updated {character.Name}'s home from '{defaultMap}' to '{value}'.");
						character.DefaultMap = value;
					}
				}
			}
		}
		Game1.netWorldState.Value.ParrotPlatformsUnlocked = loaded.parrotPlatformsUnlocked;
		Game1.player.team.farmPerfect.Value = loaded.farmPerfect;
		List<GameLocation> list = new List<GameLocation>();
		Dictionary<string, Tuple<NPC, GameLocation>> dictionary = new Dictionary<string, Tuple<NPC, GameLocation>>();
		foreach (GameLocation fromLocation2 in fromLocations)
		{
			GameLocation gameLocation = Game1.getLocationFromName(fromLocation2.name.Value);
			if (gameLocation == null)
			{
				if (fromLocation2 is Cellar)
				{
					gameLocation = Game1.CreateGameLocation("Cellar");
					if (gameLocation == null)
					{
						LogError("Couldn't create 'Cellar' location. Was it removed from Data/Locations?");
						continue;
					}
					gameLocation.name.Value = fromLocation2.name.Value;
					Game1.locations.Add(gameLocation);
				}
				if (gameLocation == null && formerLocationNames.TryGetValue(fromLocation2.name.Value, out var value2))
				{
					LogDebug($"Mapped legacy location '{fromLocation2.Name}' to '{value2}'.");
					gameLocation = Game1.getLocationFromName(value2);
				}
				if (gameLocation == null)
				{
					List<string> list2 = new List<string>();
					foreach (NPC character2 in fromLocation2.characters)
					{
						if (character2.IsVillager && character2.Name != null)
						{
							list2.Add(character2.Name);
							dictionary[character2.Name] = Tuple.Create(character2, fromLocation2);
						}
					}
					Game1.log.Warn($"Ignored unknown location '{fromLocation2.NameOrUniqueName}' in save data{((list2.Count > 0) ? $", including NPC{((list2.Count > 1) ? "s" : "")} '{string.Join("', '", list2.OrderBy((string p) => p))}'" : "")}.");
					continue;
				}
			}
			if (!(gameLocation is Farm farm))
			{
				if (!(gameLocation is FarmHouse farmHouse))
				{
					if (!(gameLocation is Forest forest))
					{
						if (!(gameLocation is MovieTheater movieTheater))
						{
							if (!(gameLocation is Town town))
							{
								if (!(gameLocation is Beach beach))
								{
									if (!(gameLocation is Woods woods))
									{
										if (!(gameLocation is CommunityCenter communityCenter))
										{
											if (gameLocation is ShopLocation shopLocation && fromLocation2 is ShopLocation shopLocation2)
											{
												shopLocation.itemsFromPlayerToSell.MoveFrom(shopLocation2.itemsFromPlayerToSell);
												shopLocation.itemsToStartSellingTomorrow.MoveFrom(shopLocation2.itemsToStartSellingTomorrow);
											}
										}
										else if (fromLocation2 is CommunityCenter communityCenter2)
										{
											communityCenter.areasComplete.Set(communityCenter2.areasComplete);
										}
									}
									else if (fromLocation2 is Woods woods2)
									{
										woods.hasUnlockedStatue.Value = woods2.hasUnlockedStatue.Value;
									}
								}
								else if (fromLocation2 is Beach beach2)
								{
									beach.bridgeFixed.Value = beach2.bridgeFixed.Value;
								}
							}
							else if (fromLocation2 is Town town2)
							{
								town.daysUntilCommunityUpgrade.Value = town2.daysUntilCommunityUpgrade.Value;
							}
						}
						else if (fromLocation2 is MovieTheater movieTheater2)
						{
							movieTheater.dayFirstEntered.Set(movieTheater2.dayFirstEntered.Value);
						}
					}
					else if (fromLocation2 is Forest forest2)
					{
						forest.stumpFixed.Value = forest2.stumpFixed.Value;
						forest.obsolete_log = forest2.obsolete_log;
					}
				}
				else if (fromLocation2 is FarmHouse farmHouse2)
				{
					farmHouse.setMapForUpgradeLevel(farmHouse.upgradeLevel);
					farmHouse.fridge.Value = farmHouse2.fridge.Value;
					farmHouse.ReadWallpaperAndFloorTileData();
				}
			}
			else if (fromLocation2 is Farm farm2)
			{
				farm.greenhouseUnlocked.Value = farm2.greenhouseUnlocked.Value;
				farm.greenhouseMoved.Value = farm2.greenhouseMoved.Value;
				farm.hasSeenGrandpaNote = farm2.hasSeenGrandpaNote;
				farm.grandpaScore.Value = farm2.grandpaScore.Value;
				farm.UpdatePatio();
			}
			gameLocation.TransferDataFromSavedLocation(fromLocation2);
			gameLocation.animals.MoveFrom(fromLocation2.animals);
			gameLocation.buildings.Set(fromLocation2.buildings);
			gameLocation.characters.Set(fromLocation2.characters);
			gameLocation.furniture.Set(fromLocation2.furniture);
			gameLocation.largeTerrainFeatures.Set(fromLocation2.largeTerrainFeatures);
			gameLocation.miniJukeboxCount.Value = fromLocation2.miniJukeboxCount.Value;
			gameLocation.miniJukeboxTrack.Value = fromLocation2.miniJukeboxTrack.Value;
			gameLocation.netObjects.Set(fromLocation2.netObjects.Pairs);
			gameLocation.numberOfSpawnedObjectsOnMap = fromLocation2.numberOfSpawnedObjectsOnMap;
			gameLocation.piecesOfHay.Value = fromLocation2.piecesOfHay.Value;
			gameLocation.resourceClumps.Set(new List<ResourceClump>(fromLocation2.resourceClumps));
			gameLocation.terrainFeatures.Set(fromLocation2.terrainFeatures.Pairs);
			if (!loaded.HasSaveFix(SaveFixes.MigrateBuildingsToData))
			{
				SaveMigrator_1_6.ConvertBuildingsToData(gameLocation);
			}
			list.Add(gameLocation);
		}
		MigrateLostVillagers(dictionary);
		foreach (GameLocation item in list)
		{
			item.AddDefaultBuildings(load: false);
			foreach (Building building in item.buildings)
			{
				building.load();
				if (building.GetIndoorsType() == IndoorsType.Instanced)
				{
					building.GetIndoors()?.addLightGlows();
				}
			}
			foreach (FarmAnimal value3 in item.animals.Values)
			{
				value3.reload((GameLocation)null);
			}
			foreach (Furniture item2 in item.furniture)
			{
				item2.updateDrawPosition();
			}
			foreach (LargeTerrainFeature largeTerrainFeature in item.largeTerrainFeatures)
			{
				largeTerrainFeature.Location = item;
				largeTerrainFeature.loadSprite();
			}
			foreach (TerrainFeature value4 in item.terrainFeatures.Values)
			{
				value4.Location = item;
				value4.loadSprite();
				if (value4 is HoeDirt hoeDirt)
				{
					hoeDirt.updateNeighbors();
				}
			}
			foreach (KeyValuePair<Vector2, Object> pair in item.objects.Pairs)
			{
				pair.Value.initializeLightSource(pair.Key);
				pair.Value.reloadSprite();
			}
			item.addLightGlows();
			if (!(item is IslandLocation islandLocation))
			{
				if (item is FarmCave farmCave)
				{
					farmCave.UpdateReadyFlag();
				}
			}
			else
			{
				islandLocation.AddAdditionalWalnutBushes();
			}
		}
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			if (location.characters.Count > 0)
			{
				NPC[] array = location.characters.ToArray();
				foreach (NPC obj in array)
				{
					initializeCharacter(obj, location);
					obj.reloadSprite();
				}
			}
			return true;
		});
		Game1.player.currentLocation = Utility.getHomeOfFarmer(Game1.player);
	}

	public static void initializeCharacter(NPC c, GameLocation location)
	{
		c.currentLocation = location;
		c.reloadData();
		if (!c.DefaultPosition.Equals(Vector2.Zero))
		{
			c.Position = c.DefaultPosition;
		}
	}

	/// <summary>Migrate villager NPCs from the save file based on their <see cref="T:StardewValley.GameData.Characters.CharacterData" /> data.</summary>
	/// <param name="lostVillagers">The villager NPCs from the save data which were in a location that no longer exists.</param>
	public static void MigrateLostVillagers(Dictionary<string, Tuple<NPC, GameLocation>> lostVillagers)
	{
		Dictionary<string, string> formerNpcNames = GetFormerNpcNames((string newName, CharacterData _) => Game1.getCharacterFromName(newName) == null);
		foreach (KeyValuePair<string, Tuple<NPC, GameLocation>> lostVillager in lostVillagers)
		{
			NPC item = lostVillager.Value.Item1;
			GameLocation item2 = lostVillager.Value.Item2;
			if (Game1.getCharacterFromName(item.Name) == null && (!formerNpcNames.TryGetValue(item.Name, out var value) || Game1.getCharacterFromName(value) == null) && NPC.TryGetData(value ?? item.Name, out var _))
			{
				GameLocation gameLocation = null;
				string name = item.Name;
				item.Name = value ?? name;
				_ = item.DefaultMap;
				item.reloadDefaultLocation();
				try
				{
					gameLocation = item.getHome();
				}
				catch (Exception)
				{
					continue;
				}
				item.Name = name;
				if (gameLocation != null)
				{
					gameLocation.characters.Add(item);
					item.currentLocation = gameLocation;
					item.position.Value = item.DefaultPosition * 64f;
					Game1.log.Debug($"Moved NPC '{item.Name}' from deleted location '{item2.Name}' to their new home in '{item.currentLocation.Name}'.");
				}
			}
		}
		foreach (KeyValuePair<string, string> item3 in formerNpcNames)
		{
			string key = item3.Key;
			string value2 = item3.Value;
			NPC characterFromName = Game1.getCharacterFromName(key);
			if (characterFromName == null)
			{
				continue;
			}
			characterFromName.Name = value2;
			foreach (Farmer allFarmer in Game1.getAllFarmers())
			{
				if (allFarmer.spouse == key)
				{
					allFarmer.spouse = value2;
				}
				if (allFarmer.friendshipData.TryGetValue(key, out var value3))
				{
					allFarmer.friendshipData.Remove(key);
					allFarmer.friendshipData.TryAdd(value2, value3);
				}
				if (allFarmer.giftedItems.TryGetValue(key, out var value4))
				{
					allFarmer.giftedItems.Remove(key);
					allFarmer.giftedItems.TryAdd(value2, value4);
				}
			}
			Game1.log.Debug($"Migrated legacy NPC '{key}' in save data to '{value2}'.");
		}
	}

	/// <summary>Get a lookup of former → new location names based on their <see cref="F:StardewValley.GameData.Locations.LocationData.FormerLocationNames" /> field.</summary>
	public static Dictionary<string, string> GetFormerLocationNames()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (KeyValuePair<string, LocationData> locationDatum in Game1.locationData)
		{
			LocationData value = locationDatum.Value;
			List<string> formerLocationNames = value.FormerLocationNames;
			if (formerLocationNames == null || formerLocationNames.Count <= 0)
			{
				continue;
			}
			foreach (string formerLocationName in value.FormerLocationNames)
			{
				string value2;
				if (Game1.locationData.ContainsKey(formerLocationName))
				{
					LogError($"Location '{locationDatum.Key}' in Data/Locations has former name '{formerLocationName}', which can't be added because there's a location with that ID in Data/Locations.");
				}
				else if (dictionary.TryGetValue(formerLocationName, out value2))
				{
					if (value2 != locationDatum.Key)
					{
						LogError($"Location '{locationDatum.Key}' in Data/Locations has former name '{formerLocationName}', which can't be added because that name is already mapped to '{value2}'.");
					}
				}
				else
				{
					dictionary[formerLocationName] = locationDatum.Key;
				}
			}
		}
		return dictionary;
	}

	/// <summary>Get a lookup of former → new NPC names based on their <see cref="F:StardewValley.GameData.Characters.CharacterData.FormerCharacterNames" /> field.</summary>
	/// <param name="filter">A filter to apply to the list of NPCs with former names.</param>
	public static Dictionary<string, string> GetFormerNpcNames(Func<string, CharacterData, bool> filter)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (KeyValuePair<string, CharacterData> characterDatum in Game1.characterData)
		{
			CharacterData value = characterDatum.Value;
			List<string> formerCharacterNames = value.FormerCharacterNames;
			if (formerCharacterNames == null || formerCharacterNames.Count <= 0 || !filter(characterDatum.Key, value))
			{
				continue;
			}
			foreach (string formerCharacterName in value.FormerCharacterNames)
			{
				string value2;
				if (Game1.characterData.ContainsKey(formerCharacterName))
				{
					LogError($"NPC '{characterDatum.Key}' in Data/Characters has former name '{formerCharacterName}', which can't be added because there's an NPC with that ID in Data/Characters.");
				}
				else if (dictionary.TryGetValue(formerCharacterName, out value2))
				{
					if (value2 != characterDatum.Key)
					{
						LogError($"NPC '{characterDatum.Key}' in Data/Characters has former name '{formerCharacterName}', which can't be added because that name is already mapped to '{value2}'.");
					}
				}
				else
				{
					dictionary[formerCharacterName] = characterDatum.Key;
				}
			}
		}
		return dictionary;
	}

	/// <inheritdoc cref="M:StardewValley.Logging.IGameLogger.Verbose(System.String)" />
	private static void LogVerbose(string message)
	{
		Game1.log.Verbose(message);
	}

	/// <inheritdoc cref="M:StardewValley.Logging.IGameLogger.Debug(System.String)" />
	private static void LogDebug(string message)
	{
		Game1.log.Debug(message);
	}

	/// <inheritdoc cref="M:StardewValley.Logging.IGameLogger.Warn(System.String)" />
	private static void LogWarn(string message)
	{
		Game1.log.Warn(message);
	}

	/// <inheritdoc cref="M:StardewValley.Logging.IGameLogger.Error(System.String,System.Exception)" />
	private static void LogError(string message, Exception exception = null)
	{
		Game1.log.Error(message, exception);
	}
}
