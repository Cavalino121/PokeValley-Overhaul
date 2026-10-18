using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Netcode.Validation;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Characters;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley;

public class Farm : GameLocation
{
	public class LightningStrikeEvent : NetEventArg
	{
		public Vector2 boltPosition;

		public bool createBolt;

		public bool bigFlash;

		public bool smallFlash;

		public bool destroyedTerrainFeature;

		public void Read(BinaryReader reader)
		{
			createBolt = reader.ReadBoolean();
			bigFlash = reader.ReadBoolean();
			smallFlash = reader.ReadBoolean();
			destroyedTerrainFeature = reader.ReadBoolean();
			boltPosition.X = reader.ReadInt32();
			boltPosition.Y = reader.ReadInt32();
		}

		public void Write(BinaryWriter writer)
		{
			writer.Write(createBolt);
			writer.Write(bigFlash);
			writer.Write(smallFlash);
			writer.Write(destroyedTerrainFeature);
			writer.Write((int)boltPosition.X);
			writer.Write((int)boltPosition.Y);
		}
	}

	[XmlIgnore]
	[NonInstancedStatic]
	public static Texture2D houseTextures = Game1.content.Load<Texture2D>("Buildings\\houses");

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.Buildings.Building.netBuildingPaintColor" /> instead.</summary>
	[NotNetField]
	public NetRef<BuildingPaintColor> housePaintColor = new NetRef<BuildingPaintColor>();

	public const int default_layout = 0;

	public const int riverlands_layout = 1;

	public const int forest_layout = 2;

	public const int mountains_layout = 3;

	public const int combat_layout = 4;

	public const int fourCorners_layout = 5;

	public const int beach_layout = 6;

	public const int mod_layout = 7;

	public const int layout_max = 7;

	[XmlElement("grandpaScore")]
	public readonly NetInt grandpaScore = new NetInt(0);

	[XmlElement("farmCaveReady")]
	public NetBool farmCaveReady = new NetBool(value: false);

	private TemporaryAnimatedSprite shippingBinLid;

	private Microsoft.Xna.Framework.Rectangle shippingBinLidOpenArea = new Microsoft.Xna.Framework.Rectangle(4480, 832, 256, 192);

	[XmlIgnore]
	private readonly NetRef<Inventory> sharedShippingBin = new NetRef<Inventory>(new Inventory());

	[XmlIgnore]
	public Item lastItemShipped;

	public bool hasSeenGrandpaNote;

	protected Dictionary<string, Dictionary<Point, Tile>> _baseSpouseAreaTiles = new Dictionary<string, Dictionary<Point, Tile>>();

	[XmlIgnore]
	public bool hasMatureFairyRoseTonight;

	[XmlElement("greenhouseUnlocked")]
	public readonly NetBool greenhouseUnlocked = new NetBool();

	[XmlElement("greenhouseMoved")]
	public readonly NetBool greenhouseMoved = new NetBool();

	private readonly NetEvent1Field<Vector2, NetVector2> spawnCrowEvent = new NetEvent1Field<Vector2, NetVector2>();

	public readonly NetEvent1<LightningStrikeEvent> lightningStrikeEvent = new NetEvent1<LightningStrikeEvent>();

	[XmlIgnore]
	public Point? mapGrandpaShrinePosition;

	[XmlIgnore]
	public Point? mapMainMailboxPosition;

	[XmlIgnore]
	public Point? mainFarmhouseEntry;

	[XmlIgnore]
	public Vector2? mapSpouseAreaCorner;

	[XmlIgnore]
	public Vector2? mapShippingBinPosition;

	protected Microsoft.Xna.Framework.Rectangle? _mountainForageRectangle;

	protected bool? _shouldSpawnForestFarmForage;

	protected bool? _shouldSpawnBeachFarmForage;

	protected bool? _oceanCrabPotOverride;

	protected string _fishLocationOverride;

	protected float _fishChanceOverride;

	public Point spousePatioSpot;

	public const int numCropsForCrow = 16;

	public Farm()
	{
	}

	public Farm(string mapPath, string name)
		: base(mapPath, name)
	{
		isAlwaysActive.Value = true;
	}

	public override bool IsBuildableLocation()
	{
		return true;
	}

	/// <inheritdoc />
	public override void AddDefaultBuildings(bool load = true)
	{
		AddDefaultBuilding("Farmhouse", GetStarterFarmhouseLocation(), load);
		AddDefaultBuilding("Greenhouse", GetGreenhouseStartLocation(), load);
		AddDefaultBuilding("Shipping Bin", GetStarterShippingBinLocation(), load);
		AddDefaultBuilding("Pet Bowl", GetStarterPetBowlLocation(), load);
		BuildStartingCabins();
	}

	public override string GetDisplayName()
	{
		return base.GetDisplayName() ?? Game1.content.LoadString("Strings\\StringsFromCSFiles:MapPage.cs.11064", Game1.player.farmName.Value);
	}

	/// <summary>Get the tile position at which the shipping bin should be created when it's missing.</summary>
	public virtual Vector2 GetStarterShippingBinLocation()
	{
		if (!mapShippingBinPosition.HasValue)
		{
			if (!TryGetMapPropertyAs("ShippingBinLocation", out Vector2 parsed, required: false))
			{
				parsed = new Vector2(71f, 14f);
			}
			mapShippingBinPosition = parsed;
		}
		return mapShippingBinPosition.Value;
	}

	/// <summary>Get the tile position at which the pet bowl should be created when it's missing.</summary>
	public virtual Vector2 GetStarterPetBowlLocation()
	{
		if (!TryGetMapPropertyAs("PetBowlLocation", out Vector2 parsed, required: false))
		{
			return new Vector2(53f, 7f);
		}
		return parsed;
	}

	/// <summary>Get the tile position at which the farmhouse should be created when it's missing.</summary>
	/// <remarks>See also <see cref="M:StardewValley.Farm.GetMainFarmHouseEntry" />.</remarks>
	public virtual Vector2 GetStarterFarmhouseLocation()
	{
		Point mainFarmHouseEntry = GetMainFarmHouseEntry();
		return new Vector2(mainFarmHouseEntry.X - 5, mainFarmHouseEntry.Y - 3);
	}

	/// <summary>Get the tile position at which the greenhouse should be created when it's missing.</summary>
	public virtual Vector2 GetGreenhouseStartLocation()
	{
		if (TryGetMapPropertyAs("GreenhouseLocation", out Vector2 parsed, required: false))
		{
			return parsed;
		}
		return Game1.whichFarm switch
		{
			5 => new Vector2(36f, 29f), 
			6 => new Vector2(14f, 14f), 
			_ => new Vector2(25f, 10f), 
		};
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(sharedShippingBin, "sharedShippingBin").AddField(spawnCrowEvent, "spawnCrowEvent").AddField(lightningStrikeEvent, "lightningStrikeEvent")
			.AddField(grandpaScore, "grandpaScore")
			.AddField(greenhouseUnlocked, "greenhouseUnlocked")
			.AddField(greenhouseMoved, "greenhouseMoved")
			.AddField(farmCaveReady, "farmCaveReady");
		spawnCrowEvent.onEvent += doSpawnCrow;
		lightningStrikeEvent.onEvent += doLightningStrike;
		greenhouseMoved.fieldChangeVisibleEvent += delegate
		{
			ClearGreenhouseGrassTiles();
		};
	}

	public virtual void ClearGreenhouseGrassTiles()
	{
		if (map != null && Game1.gameMode != 6 && greenhouseMoved.Value)
		{
			switch (Game1.whichFarm)
			{
			case 0:
			case 3:
			case 4:
				ApplyMapOverride("Farm_Greenhouse_Dirt", (Microsoft.Xna.Framework.Rectangle?)null, (Microsoft.Xna.Framework.Rectangle?)new Microsoft.Xna.Framework.Rectangle((int)GetGreenhouseStartLocation().X, (int)GetGreenhouseStartLocation().Y, 9, 6));
				break;
			case 5:
				ApplyMapOverride("Farm_Greenhouse_Dirt_FourCorners", (Microsoft.Xna.Framework.Rectangle?)null, (Microsoft.Xna.Framework.Rectangle?)new Microsoft.Xna.Framework.Rectangle((int)GetGreenhouseStartLocation().X, (int)GetGreenhouseStartLocation().Y, 9, 6));
				break;
			case 1:
			case 2:
				break;
			}
		}
	}

	public static string getMapNameFromTypeInt(int type)
	{
		switch (type)
		{
		case 0:
			return "Farm";
		case 1:
			return "Farm_Fishing";
		case 2:
			return "Farm_Foraging";
		case 3:
			return "Farm_Mining";
		case 4:
			return "Farm_Combat";
		case 5:
			return "Farm_FourCorners";
		case 6:
			return "Farm_Island";
		case 7:
			if (Game1.whichModFarm != null)
			{
				return Game1.whichModFarm.MapName;
			}
			break;
		}
		return "Farm";
	}

	public void onNewGame()
	{
		if (Game1.whichFarm == 3 || ShouldSpawnMountainOres())
		{
			for (int i = 0; i < 28; i++)
			{
				doDailyMountainFarmUpdate();
			}
		}
		else if (Game1.whichFarm == 5)
		{
			for (int j = 0; j < 10; j++)
			{
				doDailyMountainFarmUpdate();
			}
		}
		else if (Game1.GetFarmTypeID() == "MeadowlandsFarm")
		{
			for (int k = 47; k < 63; k++)
			{
				objects.Add(new Vector2(k, 20f), new Fence(new Vector2(k, 20f), "322", isGate: false));
			}
			for (int l = 16; l < 20; l++)
			{
				objects.Add(new Vector2(47f, l), new Fence(new Vector2(47f, l), "322", isGate: false));
			}
			for (int m = 7; m < 20; m++)
			{
				objects.Add(new Vector2(62f, m), new Fence(new Vector2(62f, m), "322", m == 13));
			}
			Building building = new Building("Coop", new Vector2(54f, 9f));
			building.FinishConstruction(onGameStart: true);
			building.LoadFromBuildingData(building.GetData(), forUpgrade: false, forConstruction: true);
			building.load();
			FarmAnimal farmAnimal = new FarmAnimal("White Chicken", Game1.multiplayer.getNewID(), Game1.player.UniqueMultiplayerID);
			FarmAnimal farmAnimal2 = new FarmAnimal("Brown Chicken", Game1.multiplayer.getNewID(), Game1.player.UniqueMultiplayerID);
			string[] array = Game1.content.LoadString("Strings\\1_6_Strings:StarterChicken_Names").Split('|');
			string text = array[Game1.random.Next(array.Length)];
			farmAnimal.Name = text.Split(',')[0].Trim();
			farmAnimal2.Name = text.Split(',')[1].Trim();
			(building.GetIndoors() as AnimalHouse).adoptAnimal(farmAnimal);
			(building.GetIndoors() as AnimalHouse).adoptAnimal(farmAnimal2);
			buildings.Add(building);
		}
	}

	public override void DayUpdate(int dayOfMonth)
	{
		base.DayUpdate(dayOfMonth);
		UpdatePatio();
		for (int num = characters.Count - 1; num >= 0; num--)
		{
			if (characters[num] is Pet pet && (hasTileAt(pet.TilePoint, "Buildings") || hasTileAt(pet.TilePoint.X + 1, pet.TilePoint.Y, "Buildings") || !CanSpawnCharacterHere(pet.Tile) || !CanSpawnCharacterHere(new Vector2(pet.TilePoint.X + 1, pet.TilePoint.Y))))
			{
				pet.WarpToPetBowl();
			}
		}
		lastItemShipped = null;
		if (characters.Count > 5)
		{
			int num2 = characters.RemoveWhere((NPC npc) => npc is GreenSlime && Game1.random.NextDouble() < 0.035);
			if (num2 > 0)
			{
				Game1.multiplayer.broadcastGlobalMessage((num2 == 1) ? "Strings\\Locations:Farm_1SlimeEscaped" : "Strings\\Locations:Farm_NSlimesEscaped", false, null, num2.ToString() ?? "");
			}
		}
		Vector2 key;
		if (Game1.whichFarm == 5)
		{
			if (CanItemBePlacedHere(new Vector2(5f, 32f), itemIsPassable: false, CollisionMask.All, CollisionMask.None) && CanItemBePlacedHere(new Vector2(6f, 32f), itemIsPassable: false, CollisionMask.All, CollisionMask.None) && CanItemBePlacedHere(new Vector2(6f, 33f), itemIsPassable: false, CollisionMask.All, CollisionMask.None) && CanItemBePlacedHere(new Vector2(5f, 33f), itemIsPassable: false, CollisionMask.All, CollisionMask.None))
			{
				resourceClumps.Add(new ResourceClump(600, 2, 2, new Vector2(5f, 32f)));
			}
			if (objects.Length > 0)
			{
				for (int num3 = 0; num3 < 6; num3++)
				{
					if (Utility.TryGetRandom(objects, out key, out var value) && value.IsWeeds() && value.tileLocation.X < 36f && value.tileLocation.Y < 34f)
					{
						value.SetIdAndSprite(792 + Game1.seasonIndex);
					}
				}
			}
		}
		if (ShouldSpawnBeachFarmForage())
		{
			while (Game1.random.NextDouble() < 0.9)
			{
				Vector2 randomTile = getRandomTile();
				if (!CanItemBePlacedHere(randomTile) || hasTileAt((int)randomTile.X, (int)randomTile.Y, "AlwaysFront"))
				{
					continue;
				}
				string text = null;
				if (doesTileHavePropertyNoNull((int)randomTile.X, (int)randomTile.Y, "BeachSpawn", "Back") != "")
				{
					text = "372";
					Game1.stats.Increment("beachFarmSpawns");
					switch (Game1.random.Next(6))
					{
					case 0:
						text = "393";
						break;
					case 1:
						text = "719";
						break;
					case 2:
						text = "718";
						break;
					case 3:
						text = "723";
						break;
					case 4:
					case 5:
						text = "152";
						break;
					}
					if (Game1.stats.DaysPlayed > 1)
					{
						if (Game1.random.NextDouble() < 0.15 || Game1.stats.Get("beachFarmSpawns") % 4 == 0)
						{
							text = Game1.random.Next(922, 925).ToString();
							objects.Add(randomTile, new Object(text, 1)
							{
								Fragility = 2,
								MinutesUntilReady = 3
							});
							text = null;
						}
						else if (Game1.random.NextDouble() < 0.1)
						{
							text = "397";
						}
						else if (Game1.random.NextDouble() < 0.05)
						{
							text = "392";
						}
						else if (Game1.random.NextDouble() < 0.02)
						{
							text = "394";
						}
					}
				}
				else if (Game1.season != Season.Winter && new Microsoft.Xna.Framework.Rectangle(20, 66, 33, 18).Contains((int)randomTile.X, (int)randomTile.Y) && doesTileHavePropertyNoNull((int)randomTile.X, (int)randomTile.Y, "Type", "Back") == "Grass")
				{
					text = Utility.getRandomBasicSeasonalForageItem(Game1.season, (int)Game1.stats.DaysPlayed);
				}
				if (text != null)
				{
					Object obj = ItemRegistry.Create<Object>("(O)" + text);
					obj.CanBeSetDown = false;
					obj.IsSpawnedObject = true;
					dropObject(obj, randomTile * 64f, Game1.viewport, initialPlacement: true);
				}
			}
		}
		if (Game1.whichFarm == 2)
		{
			for (int num4 = 0; num4 < 20; num4++)
			{
				for (int num5 = 0; num5 < map.Layers[0].LayerHeight; num5++)
				{
					if (getTileIndexAt(num4, num5, "Paths") == 21 && CanItemBePlacedHere(new Vector2(num4, num5), itemIsPassable: false, CollisionMask.All, CollisionMask.None) && CanItemBePlacedHere(new Vector2(num4 + 1, num5), itemIsPassable: false, CollisionMask.All, CollisionMask.None) && CanItemBePlacedHere(new Vector2(num4 + 1, num5 + 1), itemIsPassable: false, CollisionMask.All, CollisionMask.None) && CanItemBePlacedHere(new Vector2(num4, num5 + 1), itemIsPassable: false, CollisionMask.All, CollisionMask.None))
					{
						resourceClumps.Add(new ResourceClump(600, 2, 2, new Vector2(num4, num5)));
					}
				}
			}
		}
		if (ShouldSpawnForestFarmForage() && !Game1.IsWinter)
		{
			while (Game1.random.NextDouble() < 0.75)
			{
				Vector2 vector = new Vector2(Game1.random.Next(18), Game1.random.Next(map.Layers[0].LayerHeight));
				if (Game1.random.NextBool() || Game1.whichFarm != 2)
				{
					vector = getRandomTile();
				}
				if (CanItemBePlacedHere(vector, itemIsPassable: false, CollisionMask.All, CollisionMask.None) && !hasTileAt((int)vector.X, (int)vector.Y, "AlwaysFront") && ((Game1.whichFarm == 2 && vector.X < 18f) || doesTileHavePropertyNoNull((int)vector.X, (int)vector.Y, "Type", "Back").Equals("Grass")))
				{
					Object obj2 = ItemRegistry.Create<Object>(Game1.season switch
					{
						Season.Spring => Game1.random.Next(4) switch
						{
							0 => "(O)" + 16, 
							1 => "(O)" + 22, 
							2 => "(O)" + 20, 
							_ => "(O)257", 
						}, 
						Season.Summer => Game1.random.Next(4) switch
						{
							0 => "(O)402", 
							1 => "(O)396", 
							2 => "(O)398", 
							_ => "(O)404", 
						}, 
						Season.Fall => Game1.random.Next(4) switch
						{
							0 => "(O)281", 
							1 => "(O)420", 
							2 => "(O)422", 
							_ => "(O)404", 
						}, 
						_ => "(O)792", 
					});
					obj2.CanBeSetDown = false;
					obj2.IsSpawnedObject = true;
					dropObject(obj2, vector * 64f, Game1.viewport, initialPlacement: true);
				}
			}
			if (objects.Length > 0)
			{
				for (int num6 = 0; num6 < 6; num6++)
				{
					if (Utility.TryGetRandom(objects, out key, out var value2) && value2.IsWeeds())
					{
						value2.SetIdAndSprite(792 + Game1.seasonIndex);
					}
				}
			}
		}
		if (Game1.whichFarm == 3 || Game1.whichFarm == 5 || ShouldSpawnMountainOres())
		{
			doDailyMountainFarmUpdate();
		}
		if (terrainFeatures.Length > 0 && Game1.season == Season.Fall && Game1.dayOfMonth > 1 && Game1.random.NextDouble() < 0.05)
		{
			for (int num7 = 0; num7 < 10; num7++)
			{
				if (Utility.TryGetRandom(terrainFeatures, out var _, out var value3) && value3 is Tree tree && tree.growthStage.Value >= 5 && !tree.tapped.Value && !tree.isTemporaryGreenRainTree.Value)
				{
					tree.treeType.Value = "7";
					tree.loadSprite();
					break;
				}
			}
		}
		addCrows();
		if (Game1.season != Season.Winter)
		{
			spawnWeedsAndStones((Game1.season == Season.Summer) ? 30 : 20);
		}
		spawnWeeds(weedsOnly: false);
		HandleGrassGrowth(dayOfMonth);
	}

	public void doDailyMountainFarmUpdate()
	{
		double num = 1.0;
		while (Game1.random.NextDouble() < num)
		{
			Vector2 vector = (ShouldSpawnMountainOres() ? Utility.getRandomPositionInThisRectangle(_mountainForageRectangle.Value, Game1.random) : ((Game1.whichFarm == 5) ? Utility.getRandomPositionInThisRectangle(new Microsoft.Xna.Framework.Rectangle(51, 67, 11, 3), Game1.random) : Utility.getRandomPositionInThisRectangle(new Microsoft.Xna.Framework.Rectangle(5, 37, 22, 8), Game1.random)));
			if (doesTileHavePropertyNoNull((int)vector.X, (int)vector.Y, "Type", "Back").Equals("Dirt") && CanItemBePlacedHere(vector, itemIsPassable: false, CollisionMask.All, CollisionMask.None))
			{
				string itemId = "668";
				int minutesUntilReady = 2;
				if (Game1.random.NextDouble() < 0.15)
				{
					objects.Add(vector, ItemRegistry.Create<Object>("(O)590"));
					continue;
				}
				if (Game1.random.NextBool())
				{
					itemId = "670";
				}
				if (Game1.random.NextDouble() < 0.1)
				{
					if (Game1.player.MiningLevel >= 8 && Game1.random.NextDouble() < 0.33)
					{
						itemId = "77";
						minutesUntilReady = 7;
					}
					else if (Game1.player.MiningLevel >= 5 && Game1.random.NextBool())
					{
						itemId = "76";
						minutesUntilReady = 5;
					}
					else
					{
						itemId = "75";
						minutesUntilReady = 3;
					}
				}
				if (Game1.random.NextDouble() < 0.21)
				{
					itemId = "751";
					minutesUntilReady = 3;
				}
				if (Game1.player.MiningLevel >= 4 && Game1.random.NextDouble() < 0.15)
				{
					itemId = "290";
					minutesUntilReady = 4;
				}
				if (Game1.player.MiningLevel >= 7 && Game1.random.NextDouble() < 0.1)
				{
					itemId = "764";
					minutesUntilReady = 8;
				}
				if (Game1.player.MiningLevel >= 10 && Game1.random.NextDouble() < 0.01)
				{
					itemId = "765";
					minutesUntilReady = 16;
				}
				objects.Add(vector, new Object(itemId, 10)
				{
					MinutesUntilReady = minutesUntilReady
				});
			}
			num *= 0.75;
		}
	}

	/// <inheritdoc />
	public override bool catchOceanCrabPotFishFromThisSpot(int x, int y)
	{
		if (map != null)
		{
			if (!_oceanCrabPotOverride.HasValue)
			{
				_oceanCrabPotOverride = map.Properties.ContainsKey("FarmOceanCrabPotOverride");
			}
			if (_oceanCrabPotOverride.Value)
			{
				return true;
			}
		}
		return base.catchOceanCrabPotFishFromThisSpot(x, y);
	}

	public void addCrows()
	{
		int num = 0;
		foreach (KeyValuePair<Vector2, TerrainFeature> pair in terrainFeatures.Pairs)
		{
			if (pair.Value is HoeDirt { crop: not null })
			{
				num++;
			}
		}
		List<Vector2> list = new List<Vector2>();
		foreach (KeyValuePair<Vector2, Object> pair2 in objects.Pairs)
		{
			if (pair2.Value.IsScarecrow())
			{
				list.Add(pair2.Key);
			}
		}
		int num2 = Math.Min(4, num / 16);
		for (int i = 0; i < num2; i++)
		{
			if (!(Game1.random.NextDouble() < 0.3))
			{
				continue;
			}
			for (int j = 0; j < 10; j++)
			{
				if (!Utility.TryGetRandom(terrainFeatures, out var key, out var value) || !(value is HoeDirt hoeDirt2))
				{
					continue;
				}
				Crop crop = hoeDirt2.crop;
				if (crop == null || crop.currentPhase.Value <= 1)
				{
					continue;
				}
				bool flag = false;
				foreach (Vector2 item in list)
				{
					int radiusForScarecrow = objects[item].GetRadiusForScarecrow();
					if (Vector2.Distance(item, key) < (float)radiusForScarecrow)
					{
						flag = true;
						objects[item].SpecialVariable++;
						break;
					}
				}
				if (!flag)
				{
					hoeDirt2.destroyCrop(showAnimation: false);
					spawnCrowEvent.Fire(key);
				}
				break;
			}
		}
	}

	private void doSpawnCrow(Vector2 v)
	{
		if (critters == null && isOutdoors.Value)
		{
			critters = new List<Critter>();
		}
		critters.Add(new Crow((int)v.X, (int)v.Y));
	}

	public static Point getFrontDoorPositionForFarmer(Farmer who)
	{
		Point mainFarmHouseEntry = Game1.getFarm().GetMainFarmHouseEntry();
		mainFarmHouseEntry.Y--;
		return mainFarmHouseEntry;
	}

	public override void performTenMinuteUpdate(int timeOfDay)
	{
		base.performTenMinuteUpdate(timeOfDay);
		if (timeOfDay >= 1300 && Game1.IsMasterGame)
		{
			foreach (NPC item in new List<Character>(characters))
			{
				if (item.isMarried())
				{
					item.returnHomeFromFarmPosition(this);
				}
			}
		}
		foreach (NPC character in characters)
		{
			if (character.getSpouse() == Game1.player)
			{
				character.checkForMarriageDialogue(timeOfDay, this);
			}
			if (character is Child child)
			{
				child.tenMinuteUpdate();
			}
		}
		if (!Game1.spawnMonstersAtNight || Game1.farmEvent != null || Game1.timeOfDay < 1900 || !(Game1.random.NextDouble() < 0.25 - Game1.player.team.AverageDailyLuck() / 2.0))
		{
			return;
		}
		if (Game1.random.NextDouble() < 0.25)
		{
			if (Equals(Game1.currentLocation))
			{
				spawnFlyingMonstersOffScreen();
			}
		}
		else
		{
			spawnGroundMonsterOffScreen();
		}
	}

	public void spawnGroundMonsterOffScreen()
	{
		for (int i = 0; i < 15; i++)
		{
			Vector2 randomTile = getRandomTile();
			if (Utility.isOnScreen(Utility.Vector2ToPoint(randomTile), 64, this))
			{
				randomTile.X -= Game1.viewport.Width / 64;
			}
			if (!CanItemBePlacedHere(randomTile))
			{
				continue;
			}
			int combatLevel = Game1.player.CombatLevel;
			bool flag;
			if (combatLevel >= 8 && Game1.random.NextDouble() < 0.15)
			{
				characters.Add(new ShadowBrute(randomTile * 64f)
				{
					focusedOnFarmers = true,
					wildernessFarmMonster = true
				});
				flag = true;
			}
			else if (Game1.random.NextDouble() < ((Game1.whichFarm == 4) ? 0.66 : 0.33))
			{
				characters.Add(new RockGolem(randomTile * 64f, combatLevel)
				{
					wildernessFarmMonster = true
				});
				flag = true;
			}
			else
			{
				int mineLevel = 1;
				if (combatLevel >= 10)
				{
					mineLevel = 140;
				}
				else if (combatLevel >= 8)
				{
					mineLevel = 100;
				}
				else if (combatLevel >= 4)
				{
					mineLevel = 41;
				}
				characters.Add(new GreenSlime(randomTile * 64f, mineLevel)
				{
					wildernessFarmMonster = true
				});
				flag = true;
			}
			if (!flag || !Game1.currentLocation.Equals(this))
			{
				break;
			}
			{
				foreach (KeyValuePair<Vector2, Object> pair in objects.Pairs)
				{
					if (pair.Value?.QualifiedItemId == "(BC)83")
					{
						pair.Value.shakeTimer = 1000;
						pair.Value.showNextIndex.Value = true;
						Game1.currentLightSources.Add(new LightSource(pair.Value.GenerateLightSourceId(pair.Value.TileLocation), 4, pair.Key * 64f + new Vector2(32f, 0f), 1f, Color.Cyan * 0.75f, LightSource.LightContext.None, 0L, base.NameOrUniqueName));
					}
				}
				break;
			}
		}
	}

	public void spawnFlyingMonstersOffScreen()
	{
		Vector2 zero = Vector2.Zero;
		switch (Game1.random.Next(4))
		{
		case 0:
			zero.X = Game1.random.Next(map.Layers[0].LayerWidth);
			break;
		case 3:
			zero.Y = Game1.random.Next(map.Layers[0].LayerHeight);
			break;
		case 1:
			zero.X = map.Layers[0].LayerWidth - 1;
			zero.Y = Game1.random.Next(map.Layers[0].LayerHeight);
			break;
		case 2:
			zero.Y = map.Layers[0].LayerHeight - 1;
			zero.X = Game1.random.Next(map.Layers[0].LayerWidth);
			break;
		}
		if (Utility.isOnScreen(zero * 64f, 64))
		{
			zero.X -= Game1.viewport.Width;
		}
		int combatLevel = Game1.player.CombatLevel;
		bool flag;
		if (combatLevel >= 10 && Game1.random.NextDouble() < 0.01 && Game1.player.Items.ContainsId("(W)4"))
		{
			characters.Add(new Bat(zero * 64f, 9999)
			{
				focusedOnFarmers = true,
				wildernessFarmMonster = true
			});
			flag = true;
		}
		else if (combatLevel >= 10 && Game1.random.NextDouble() < 0.25)
		{
			characters.Add(new Bat(zero * 64f, 172)
			{
				focusedOnFarmers = true,
				wildernessFarmMonster = true
			});
			flag = true;
		}
		else if (combatLevel >= 10 && Game1.random.NextDouble() < 0.25)
		{
			characters.Add(new Serpent(zero * 64f)
			{
				focusedOnFarmers = true,
				wildernessFarmMonster = true
			});
			flag = true;
		}
		else if (combatLevel >= 8 && Game1.random.NextBool())
		{
			characters.Add(new Bat(zero * 64f, 81)
			{
				focusedOnFarmers = true,
				wildernessFarmMonster = true
			});
			flag = true;
		}
		else if (combatLevel >= 5 && Game1.random.NextBool())
		{
			characters.Add(new Bat(zero * 64f, 41)
			{
				focusedOnFarmers = true,
				wildernessFarmMonster = true
			});
			flag = true;
		}
		else
		{
			characters.Add(new Bat(zero * 64f, 1)
			{
				focusedOnFarmers = true,
				wildernessFarmMonster = true
			});
			flag = true;
		}
		if (!flag || !Game1.currentLocation.Equals(this))
		{
			return;
		}
		foreach (KeyValuePair<Vector2, Object> pair in objects.Pairs)
		{
			if (pair.Value?.QualifiedItemId == "(BC)83")
			{
				pair.Value.shakeTimer = 1000;
				pair.Value.showNextIndex.Value = true;
				Game1.currentLightSources.Add(new LightSource(pair.Value.GenerateLightSourceId(pair.Value.TileLocation), 4, pair.Key * 64f + new Vector2(32f, 0f), 1f, Color.Cyan * 0.75f, LightSource.LightContext.None, 0L, base.NameOrUniqueName));
			}
		}
	}

	public virtual void requestGrandpaReevaluation()
	{
		grandpaScore.Value = 0;
		if (Game1.IsMasterGame)
		{
			Game1.player.eventsSeen.Remove("558292");
			Game1.player.eventsSeen.Add("321777");
		}
		removeTemporarySpritesWithID(6666);
	}

	public override void OnMapLoad(Map map)
	{
		CacheOffBasePatioArea();
		base.OnMapLoad(map);
	}

	/// <inheritdoc />
	public override void OnBuildingMoved(Building building)
	{
		base.OnBuildingMoved(building);
		if (building.HasIndoorsName("FarmHouse"))
		{
			UnsetFarmhouseValues();
		}
		if (building is GreenhouseBuilding)
		{
			greenhouseMoved.Value = true;
		}
		if (building.GetIndoors() is FarmHouse farmHouse && farmHouse.HasNpcSpouseOrRoommate())
		{
			NPC characterFromName = getCharacterFromName(farmHouse.owner.spouse);
			if (characterFromName != null && !characterFromName.shouldPlaySpousePatioAnimation.Value)
			{
				Game1.player.team.requestNPCGoHome.Fire(characterFromName.Name);
			}
		}
	}

	/// <inheritdoc />
	public override bool ShouldExcludeFromNpcPathfinding()
	{
		return true;
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		Point grandpaShrinePosition = GetGrandpaShrinePosition();
		if (tileLocation.X >= grandpaShrinePosition.X - 1 && tileLocation.X <= grandpaShrinePosition.X + 1 && tileLocation.Y == grandpaShrinePosition.Y)
		{
			if (!hasSeenGrandpaNote)
			{
				Game1.addMail("hasSeenGrandpaNote", noLetter: true);
				hasSeenGrandpaNote = true;
				Game1.activeClickableMenu = new LetterViewerMenu(Game1.content.LoadString("Strings\\Locations:Farm_GrandpaNote", Game1.player.Name).Replace('\n', '^'));
				return true;
			}
			if (Game1.year >= 3 && grandpaScore.Value > 0 && grandpaScore.Value < 4)
			{
				if (who.ActiveObject?.QualifiedItemId == "(O)72" && grandpaScore.Value < 4)
				{
					who.reduceActiveItemByOne();
					playSound("stoneStep");
					playSound("fireball");
					DelayedAction.playSoundAfterDelay("yoba", 800, this);
					DelayedAction.showDialogueAfterDelay(Game1.content.LoadString("Strings\\Locations:Farm_GrandpaShrine_PlaceDiamond"), 1200);
					Game1.multiplayer.broadcastGrandpaReevaluation();
					Game1.player.freezePause = 1200;
					return true;
				}
				if (who.ActiveObject?.QualifiedItemId != "(O)72")
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Farm_GrandpaShrine_DiamondSlot"));
					return true;
				}
			}
			else
			{
				if (grandpaScore.Value >= 4 && !Utility.doesItemExistAnywhere("(BC)160"))
				{
					who.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create("(BC)160"), grandpaStatueCallback);
					return true;
				}
				if (grandpaScore.Value == 0 && Game1.year >= 3)
				{
					Game1.player.eventsSeen.Remove("558292");
					Game1.player.eventsSeen.Add("321777");
				}
			}
		}
		if (base.checkAction(tileLocation, viewport, who))
		{
			return true;
		}
		return false;
	}

	public void grandpaStatueCallback(Item item, Farmer who)
	{
		if (item is Object { QualifiedItemId: "(BC)160" })
		{
			who?.mailReceived.Add("grandpaPerfect");
		}
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		Farm farm = (Farm)l;
		base.TransferDataFromSavedLocation(l);
		housePaintColor.Value = farm.housePaintColor.Value;
		farmCaveReady.Value = farm.farmCaveReady.Value;
		if (farm.hasSeenGrandpaNote)
		{
			Game1.addMail("hasSeenGrandpaNote", noLetter: true);
		}
		UnsetFarmhouseValues();
	}

	public IInventory getShippingBin(Farmer who)
	{
		if (Game1.player.team.useSeparateWallets.Value)
		{
			return who.personalShippingBin.Value;
		}
		return sharedShippingBin.Value;
	}

	public void shipItem(Item i, Farmer who)
	{
		if (i != null)
		{
			who.removeItemFromInventory(i);
			getShippingBin(who).Add(i);
			showShipment(i, playThrowSound: false);
			lastItemShipped = i;
			if (Game1.player.ActiveItem == null)
			{
				Game1.player.showNotCarrying();
				Game1.player.Halt();
			}
		}
	}

	public void UnsetFarmhouseValues()
	{
		mainFarmhouseEntry = null;
		mapMainMailboxPosition = null;
	}

	public void showShipment(Item item, bool playThrowSound = true)
	{
		if (playThrowSound)
		{
			localSound("backpackIN");
		}
		DelayedAction.playSoundAfterDelay("Ship", playThrowSound ? 250 : 0);
		int num = Game1.random.Next();
		temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(524, 218, 34, 22), new Vector2(71f, 13f) * 64f + new Vector2(0f, 5f) * 4f, flipped: false, 0f, Color.White)
		{
			interval = 100f,
			totalNumberOfLoops = 1,
			animationLength = 3,
			pingPong = true,
			scale = 4f,
			layerDepth = 0.09601f,
			id = num,
			extraInfoForEndBehavior = num,
			endFunction = base.removeTemporarySpritesWithID
		});
		temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(524, 230, 34, 10), new Vector2(71f, 13f) * 64f + new Vector2(0f, 17f) * 4f, flipped: false, 0f, Color.White)
		{
			interval = 100f,
			totalNumberOfLoops = 1,
			animationLength = 3,
			pingPong = true,
			scale = 4f,
			layerDepth = 0.0963f,
			id = num,
			extraInfoForEndBehavior = num
		});
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
		ColoredObject coloredObject = item as ColoredObject;
		Vector2 position = new Vector2(71f, 13f) * 64f + new Vector2(8 + Game1.random.Next(6), 2f) * 4f;
		bool[] array = new bool[2] { false, true };
		foreach (bool flag in array)
		{
			if (!flag || (coloredObject != null && !coloredObject.ColorSameIndexAsParentSheetIndex))
			{
				temporarySprites.Add(new TemporaryAnimatedSprite(dataOrErrorItem.TextureName, dataOrErrorItem.GetSourceRect(flag ? 1 : 0), position, flipped: false, 0f, Color.White)
				{
					interval = 9999f,
					scale = 4f,
					alphaFade = 0.045f,
					layerDepth = 0.096225f,
					motion = new Vector2(0f, 0.3f),
					acceleration = new Vector2(0f, 0.2f),
					scaleChange = -0.05f,
					color = (coloredObject?.color.Value ?? Color.White)
				});
			}
		}
	}

	public override Item getFish(float millisecondsAfterNibble, string bait, int waterDepth, Farmer who, double baitPotency, Vector2 bobberTile, string location = null)
	{
		if (_fishLocationOverride == null)
		{
			_fishLocationOverride = "";
			string[] mapPropertySplitBySpaces = GetMapPropertySplitBySpaces("FarmFishLocationOverride");
			if (mapPropertySplitBySpaces.Length != 0)
			{
				if (!ArgUtility.TryGet(mapPropertySplitBySpaces, 0, out var value, out var error, allowBlank: true, "string targetLocation") || !ArgUtility.TryGetFloat(mapPropertySplitBySpaces, 1, out var value2, out error, "float chance"))
				{
					LogMapPropertyError("FarmFishLocationOverride", mapPropertySplitBySpaces, error);
				}
				else
				{
					_fishLocationOverride = value;
					_fishChanceOverride = value2;
				}
			}
		}
		if (_fishChanceOverride > 0f && Game1.random.NextDouble() < (double)_fishChanceOverride)
		{
			return base.getFish(millisecondsAfterNibble, bait, waterDepth, who, baitPotency, bobberTile, _fishLocationOverride);
		}
		return base.getFish(millisecondsAfterNibble, bait, waterDepth, who, baitPotency, bobberTile);
	}

	protected override void resetSharedState()
	{
		base.resetSharedState();
		if (!greenhouseUnlocked.Value && Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccPantry"))
		{
			greenhouseUnlocked.Value = true;
		}
		for (int num = characters.Count - 1; num >= 0; num--)
		{
			if (Game1.timeOfDay >= 1300 && characters[num].isMarried() && characters[num].controller == null)
			{
				characters[num].Halt();
				characters[num].drawOffset = Vector2.Zero;
				characters[num].Sprite.StopAnimation();
				FarmHouse farmHouse = Game1.RequireLocation<FarmHouse>(characters[num].getSpouse().homeLocation.Value);
				Game1.warpCharacter(characters[num], characters[num].getSpouse().homeLocation.Value, farmHouse.getKitchenStandingSpot());
				break;
			}
		}
	}

	public virtual void UpdatePatio()
	{
		if (Game1.MasterPlayer.isMarriedOrRoommates() && Game1.MasterPlayer.spouse != null)
		{
			addSpouseOutdoorArea(Game1.MasterPlayer.spouse);
		}
		else
		{
			addSpouseOutdoorArea("");
		}
	}

	public override void MakeMapModifications(bool force = false)
	{
		base.MakeMapModifications(force);
		ClearGreenhouseGrassTiles();
		UpdatePatio();
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		hasSeenGrandpaNote = Game1.player.hasOrWillReceiveMail("hasSeenGrandpaNote");
		if (Game1.player.mailReceived.Add("button_tut_2"))
		{
			Game1.onScreenMenus.Add(new ButtonTutorialMenu(1));
		}
		for (int num = characters.Count - 1; num >= 0; num--)
		{
			if (characters[num] is Child child)
			{
				child.resetForPlayerEntry(this);
			}
		}
		addGrandpaCandles();
		if (Game1.MasterPlayer.mailReceived.Contains("Farm_Eternal") && !Game1.player.mailReceived.Contains("Farm_Eternal_Parrots") && !IsRainingHere())
		{
			for (int i = 0; i < 20; i++)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Microsoft.Xna.Framework.Rectangle(49, 24 * Game1.random.Next(4), 24, 24), new Vector2(Game1.viewport.MaxCorner.X, Game1.viewport.Location.Y + Game1.random.Next(64, Game1.viewport.Height / 2)), flipped: false, 0f, Color.White)
				{
					scale = 4f,
					motion = new Vector2(-5f + (float)Game1.random.Next(-10, 11) / 10f, 4f + (float)Game1.random.Next(-10, 11) / 10f),
					acceleration = new Vector2(0f, -0.02f),
					animationLength = 3,
					interval = 100f,
					pingPong = true,
					totalNumberOfLoops = 999,
					delayBeforeAnimationStart = i * 250,
					drawAboveAlwaysFront = true,
					startSound = "batFlap"
				});
			}
			DelayedAction.playSoundAfterDelay("parrot_squawk", 1000);
			DelayedAction.playSoundAfterDelay("parrot_squawk", 4000);
			DelayedAction.playSoundAfterDelay("parrot", 3000);
			DelayedAction.playSoundAfterDelay("parrot", 5500);
			DelayedAction.playSoundAfterDelay("parrot_squawk", 7000);
			for (int j = 0; j < 20; j++)
			{
				DelayedAction.playSoundAfterDelay("batFlap", 5000 + j * 250);
			}
			Game1.player.mailReceived.Add("Farm_Eternal_Parrots");
		}
	}

	public virtual Vector2 GetSpouseOutdoorAreaCorner()
	{
		if (!mapSpouseAreaCorner.HasValue)
		{
			if (!TryGetMapPropertyAs("SpouseAreaLocation", out Vector2 parsed, required: false))
			{
				parsed = new Vector2(69f, 6f);
			}
			mapSpouseAreaCorner = parsed;
		}
		return mapSpouseAreaCorner.Value;
	}

	public virtual void CacheOffBasePatioArea()
	{
		_baseSpouseAreaTiles = new Dictionary<string, Dictionary<Point, Tile>>();
		List<string> list = new List<string>();
		foreach (Layer layer2 in map.Layers)
		{
			list.Add(layer2.Id);
		}
		foreach (string item in list)
		{
			Layer layer = map.GetLayer(item);
			Dictionary<Point, Tile> dictionary = new Dictionary<Point, Tile>();
			_baseSpouseAreaTiles[item] = dictionary;
			Vector2 spouseOutdoorAreaCorner = GetSpouseOutdoorAreaCorner();
			for (int i = (int)spouseOutdoorAreaCorner.X; i < (int)spouseOutdoorAreaCorner.X + 4; i++)
			{
				for (int j = (int)spouseOutdoorAreaCorner.Y; j < (int)spouseOutdoorAreaCorner.Y + 4; j++)
				{
					if (layer == null)
					{
						dictionary[new Point(i, j)] = null;
					}
					else
					{
						dictionary[new Point(i, j)] = layer.Tiles[i, j];
					}
				}
			}
		}
	}

	public virtual void ReapplyBasePatioArea()
	{
		foreach (string key in _baseSpouseAreaTiles.Keys)
		{
			Layer layer = map.GetLayer(key);
			foreach (Point key2 in _baseSpouseAreaTiles[key].Keys)
			{
				Tile value = _baseSpouseAreaTiles[key][key2];
				if (layer != null)
				{
					layer.Tiles[key2.X, key2.Y] = value;
				}
			}
		}
	}

	public void addSpouseOutdoorArea(string spouseName)
	{
		ReapplyBasePatioArea();
		Point point = Utility.Vector2ToPoint(GetSpouseOutdoorAreaCorner());
		spousePatioSpot = new Point(point.X + 2, point.Y + 3);
		CharacterData data;
		CharacterSpousePatioData characterSpousePatioData = (NPC.TryGetData(spouseName, out data) ? data.SpousePatio : null);
		if (characterSpousePatioData == null)
		{
			return;
		}
		string map_name = characterSpousePatioData.MapAsset ?? "spousePatios";
		Microsoft.Xna.Framework.Rectangle mapSourceRect = characterSpousePatioData.MapSourceRect;
		int width = Math.Min(mapSourceRect.Width, 4);
		int height = Math.Min(mapSourceRect.Height, 4);
		Point point2 = point;
		Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle(point2.X, point2.Y, width, height);
		Point location = mapSourceRect.Location;
		if (_appliedMapOverrides.Contains("spouse_patio"))
		{
			_appliedMapOverrides.Remove("spouse_patio");
		}
		ApplyMapOverride(map_name, "spouse_patio", new Microsoft.Xna.Framework.Rectangle(location.X, location.Y, rectangle.Width, rectangle.Height), rectangle);
		foreach (Point point3 in rectangle.GetPoints())
		{
			if (getTileIndexAt(point3, "Paths") == 7)
			{
				spousePatioSpot = point3;
				break;
			}
		}
	}

	public void addGrandpaCandles()
	{
		Point grandpaShrinePosition = GetGrandpaShrinePosition();
		if (grandpaScore.Value > 0)
		{
			Microsoft.Xna.Framework.Rectangle sourceRect = new Microsoft.Xna.Framework.Rectangle(577, 1985, 2, 5);
			removeTemporarySpritesWithIDLocal(6666);
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", sourceRect, 99999f, 1, 9999, new Vector2((grandpaShrinePosition.X - 1) * 64 + 20, (grandpaShrinePosition.Y - 1) * 64 + 20), flicker: false, flipped: false, (float)((grandpaShrinePosition.Y - 1) * 64) / 10000f, 0f, Color.White, 4f, 0f, 0f, 0f));
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2((grandpaShrinePosition.X - 1) * 64 + 12, (grandpaShrinePosition.Y - 1) * 64 - 4), flipped: false, 0f, Color.White)
			{
				interval = 50f,
				totalNumberOfLoops = 99999,
				animationLength = 7,
				lightId = "Farm_GrandpaCandles_1",
				id = 6666,
				lightRadius = 1f,
				scale = 3f,
				layerDepth = 0.038500004f,
				delayBeforeAnimationStart = 0
			});
			if (grandpaScore.Value > 1)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", sourceRect, 99999f, 1, 9999, new Vector2((grandpaShrinePosition.X - 1) * 64 + 40, (grandpaShrinePosition.Y - 2) * 64 + 24), flicker: false, flipped: false, (float)((grandpaShrinePosition.Y - 1) * 64) / 10000f, 0f, Color.White, 4f, 0f, 0f, 0f));
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2((grandpaShrinePosition.X - 1) * 64 + 36, (grandpaShrinePosition.Y - 2) * 64), flipped: false, 0f, Color.White)
				{
					interval = 50f,
					totalNumberOfLoops = 99999,
					animationLength = 7,
					lightId = "Farm_GrandpaCandles_2",
					id = 6666,
					lightRadius = 1f,
					scale = 3f,
					layerDepth = 0.038500004f,
					delayBeforeAnimationStart = 50
				});
			}
			if (grandpaScore.Value > 2)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", sourceRect, 99999f, 1, 9999, new Vector2((grandpaShrinePosition.X + 1) * 64 + 20, (grandpaShrinePosition.Y - 2) * 64 + 24), flicker: false, flipped: false, (float)((grandpaShrinePosition.Y - 1) * 64) / 10000f, 0f, Color.White, 4f, 0f, 0f, 0f));
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2((grandpaShrinePosition.X + 1) * 64 + 16, (grandpaShrinePosition.Y - 2) * 64), flipped: false, 0f, Color.White)
				{
					interval = 50f,
					totalNumberOfLoops = 99999,
					animationLength = 7,
					lightId = "Farm_GrandpaCandles_3",
					id = 6666,
					lightRadius = 1f,
					scale = 3f,
					layerDepth = 0.038500004f,
					delayBeforeAnimationStart = 100
				});
			}
			if (grandpaScore.Value > 3)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", sourceRect, 99999f, 1, 9999, new Vector2((grandpaShrinePosition.X + 1) * 64 + 40, (grandpaShrinePosition.Y - 1) * 64 + 20), flicker: false, flipped: false, (float)((grandpaShrinePosition.Y - 1) * 64) / 10000f, 0f, Color.White, 4f, 0f, 0f, 0f));
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2((grandpaShrinePosition.X + 1) * 64 + 36, (grandpaShrinePosition.Y - 1) * 64 - 4), flipped: false, 0f, Color.White)
				{
					interval = 50f,
					totalNumberOfLoops = 99999,
					animationLength = 7,
					lightId = "Farm_GrandpaCandles_4",
					id = 6666,
					lightRadius = 1f,
					scale = 3f,
					layerDepth = 0.038500004f,
					delayBeforeAnimationStart = 150
				});
			}
		}
		if (Game1.MasterPlayer.mailReceived.Contains("Farm_Eternal"))
		{
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(176, 157, 15, 16), 99999f, 1, 9999, new Vector2(grandpaShrinePosition.X * 64 + 4, (grandpaShrinePosition.Y - 2) * 64 - 24), flicker: false, flipped: false, (float)((grandpaShrinePosition.Y - 1) * 64) / 10000f, 0f, Color.White, 4f, 0f, 0f, 0f));
		}
	}

	private void openShippingBinLid()
	{
		if (shippingBinLid != null)
		{
			if (shippingBinLid.pingPongMotion != 1 && Game1.currentLocation == this)
			{
				localSound("doorCreak");
			}
			shippingBinLid.pingPongMotion = 1;
			shippingBinLid.paused = false;
		}
	}

	private void closeShippingBinLid()
	{
		if (shippingBinLid != null && shippingBinLid.currentParentTileIndex > 0)
		{
			if (shippingBinLid.pingPongMotion != -1 && Game1.currentLocation == this)
			{
				localSound("doorCreakReverse");
			}
			shippingBinLid.pingPongMotion = -1;
			shippingBinLid.paused = false;
		}
	}

	private void updateShippingBinLid(GameTime time)
	{
		if (isShippingBinLidOpen(requiredToBeFullyOpen: true) && shippingBinLid.pingPongMotion == 1)
		{
			shippingBinLid.paused = true;
		}
		else if (shippingBinLid.currentParentTileIndex == 0 && shippingBinLid.pingPongMotion == -1)
		{
			if (!shippingBinLid.paused && Game1.currentLocation == this)
			{
				localSound("woodyStep");
			}
			shippingBinLid.paused = true;
		}
		shippingBinLid.update(time);
	}

	private bool isShippingBinLidOpen(bool requiredToBeFullyOpen = false)
	{
		if (shippingBinLid != null && shippingBinLid.currentParentTileIndex >= ((!requiredToBeFullyOpen) ? 1 : (shippingBinLid.animationLength - 1)))
		{
			return true;
		}
		return false;
	}

	public override void pokeTileForConstruction(Vector2 tile)
	{
		base.pokeTileForConstruction(tile);
		foreach (NPC character in characters)
		{
			if (character is Pet pet && pet.Tile == tile)
			{
				pet.FacingDirection = Game1.random.Next(0, 4);
				pet.faceDirection(pet.FacingDirection);
				pet.CurrentBehavior = "Walk";
				pet.forceUpdateTimer = 2000;
				pet.setMovingInFacingDirection();
			}
		}
	}

	public override bool shouldShadowBeDrawnAboveBuildingsLayer(Vector2 p)
	{
		if (doesTileHaveProperty((int)p.X, (int)p.Y, "NoSpawn", "Back") == "All" && doesTileHaveProperty((int)p.X, (int)p.Y, "Type", "Back") == "Wood")
		{
			return true;
		}
		return base.shouldShadowBeDrawnAboveBuildingsLayer(p);
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		if (Game1.mailbox.Count > 0)
		{
			float num = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			Point mailboxPosition = Game1.player.getMailboxPosition();
			float num2 = (float)((mailboxPosition.X + 1) * 64) / 10000f + (float)(mailboxPosition.Y * 64) / 10000f;
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(mailboxPosition.X * 64, (float)(mailboxPosition.Y * 64 - 96 - 48) + num)), new Microsoft.Xna.Framework.Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, num2 + 1E-06f);
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(mailboxPosition.X * 64 + 32 + 4, (float)(mailboxPosition.Y * 64 - 64 - 24 - 8) + num)), new Microsoft.Xna.Framework.Rectangle(189, 423, 15, 13), Color.White, 0f, new Vector2(7f, 6f), 4f, SpriteEffects.None, num2 + 1E-05f);
		}
		shippingBinLid?.draw(b);
		if (!hasSeenGrandpaNote)
		{
			Point grandpaShrinePosition = GetGrandpaShrinePosition();
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2((grandpaShrinePosition.X + 1) * 64, grandpaShrinePosition.Y * 64)), new Microsoft.Xna.Framework.Rectangle(575, 1972, 11, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(grandpaShrinePosition.Y * 64) / 10000f + 1E-06f);
		}
	}

	public virtual Point GetMainMailboxPosition()
	{
		if (!mapMainMailboxPosition.HasValue)
		{
			if (!TryGetMapPropertyAs("MailboxLocation", out Point parsed, required: false))
			{
				parsed = new Point(68, 16);
			}
			mapMainMailboxPosition = parsed;
			Building mainFarmHouse = GetMainFarmHouse();
			BuildingData buildingData = mainFarmHouse?.GetData();
			if (buildingData?.ActionTiles != null)
			{
				foreach (BuildingActionTile actionTile in buildingData.ActionTiles)
				{
					if (actionTile.Action == "Mailbox")
					{
						mapMainMailboxPosition = new Point(mainFarmHouse.tileX.Value + actionTile.Tile.X, mainFarmHouse.tileY.Value + actionTile.Tile.Y);
						break;
					}
				}
			}
		}
		return mapMainMailboxPosition.Value;
	}

	public virtual Point GetGrandpaShrinePosition()
	{
		if (!mapGrandpaShrinePosition.HasValue)
		{
			if (!TryGetMapPropertyAs("GrandpaShrineLocation", out Point parsed, required: false))
			{
				parsed = new Point(8, 7);
			}
			mapGrandpaShrinePosition = parsed;
		}
		return mapGrandpaShrinePosition.Value;
	}

	/// <summary>Get the door tile position for the farmhouse.</summary>
	/// <remarks>See also <see cref="M:StardewValley.Farm.GetStarterFarmhouseLocation" />.</remarks>
	public virtual Point GetMainFarmHouseEntry()
	{
		if (!mainFarmhouseEntry.HasValue)
		{
			if (!TryGetMapPropertyAs("FarmHouseEntry", out Point parsed, required: false))
			{
				parsed = new Point(64, 15);
			}
			mainFarmhouseEntry = parsed;
			Building mainFarmHouse = GetMainFarmHouse();
			if (mainFarmHouse != null)
			{
				mainFarmhouseEntry = new Point(mainFarmHouse.tileX.Value + mainFarmHouse.humanDoor.X, mainFarmHouse.tileY.Value + mainFarmHouse.humanDoor.Y + 1);
			}
		}
		return mainFarmhouseEntry.Value;
	}

	/// <summary>Get the main player's farmhouse, if found.</summary>
	public virtual Building GetMainFarmHouse()
	{
		return getBuildingByType("Farmhouse");
	}

	public override void ResetForEvent(Event ev)
	{
		base.ResetForEvent(ev);
		if (ev.id != "-2")
		{
			Point frontDoorPositionForFarmer = getFrontDoorPositionForFarmer(ev.farmer);
			frontDoorPositionForFarmer.Y++;
			int num = frontDoorPositionForFarmer.X - 64;
			int num2 = frontDoorPositionForFarmer.Y - 15;
			ev.eventPositionTileOffset = new Vector2(num, num2);
		}
	}

	public override void updateEvenIfFarmerIsntHere(GameTime time, bool skipWasUpdatedFlush = false)
	{
		spawnCrowEvent.Poll();
		lightningStrikeEvent.Poll();
		base.updateEvenIfFarmerIsntHere(time, skipWasUpdatedFlush);
	}

	public bool isTileOpenBesidesTerrainFeatures(Vector2 tile)
	{
		Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle((int)tile.X * 64, (int)tile.Y * 64, 64, 64);
		foreach (Building building in buildings)
		{
			if (building.intersects(rectangle))
			{
				return false;
			}
		}
		foreach (ResourceClump resourceClump in resourceClumps)
		{
			if (resourceClump.getBoundingBox().Intersects(rectangle))
			{
				return false;
			}
		}
		foreach (KeyValuePair<long, FarmAnimal> pair in animals.Pairs)
		{
			if (pair.Value.Tile == tile)
			{
				return true;
			}
		}
		if (!objects.ContainsKey(tile))
		{
			return isTilePassable(new Location((int)tile.X, (int)tile.Y), Game1.viewport);
		}
		return false;
	}

	private void doLightningStrike(LightningStrikeEvent lightning)
	{
		if (lightning.smallFlash)
		{
			if (Game1.currentLocation.IsOutdoors && !Game1.newDay && Game1.currentLocation.IsLightningHere())
			{
				Game1.flashAlpha = (float)(0.5 + Game1.random.NextDouble());
				if (Game1.random.NextBool())
				{
					DelayedAction.screenFlashAfterDelay((float)(0.3 + Game1.random.NextDouble()), Game1.random.Next(500, 1000));
				}
				DelayedAction.playSoundAfterDelay("thunder_small", Game1.random.Next(500, 1500));
			}
		}
		else if (lightning.bigFlash && Game1.currentLocation.IsOutdoors && Game1.currentLocation.IsLightningHere() && !Game1.newDay)
		{
			Game1.flashAlpha = (float)(0.5 + Game1.random.NextDouble());
			Game1.playSound("thunder");
		}
		if (lightning.createBolt && Game1.currentLocation.name.Equals("Farm"))
		{
			if (lightning.destroyedTerrainFeature)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite(362, 75f, 6, 1, lightning.boltPosition, flicker: false, flipped: false));
			}
			Utility.drawLightningBolt(lightning.boltPosition, this);
		}
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		if (wasUpdated && Game1.gameMode != 0)
		{
			return;
		}
		base.UpdateWhenCurrentLocation(time);
		if (shippingBinLid == null)
		{
			return;
		}
		bool flag = false;
		foreach (Farmer farmer in farmers)
		{
			if (farmer.GetBoundingBox().Intersects(shippingBinLidOpenArea))
			{
				openShippingBinLid();
				flag = true;
			}
		}
		if (!flag)
		{
			closeShippingBinLid();
		}
		updateShippingBinLid(time);
	}

	public bool ShouldSpawnMountainOres()
	{
		if (!_mountainForageRectangle.HasValue)
		{
			_mountainForageRectangle = (TryGetMapPropertyAs("SpawnMountainFarmOreRect", out Microsoft.Xna.Framework.Rectangle parsed, required: false) ? parsed : Microsoft.Xna.Framework.Rectangle.Empty);
		}
		return _mountainForageRectangle.Value.Width > 0;
	}

	public bool ShouldSpawnForestFarmForage()
	{
		if (map != null)
		{
			if (!_shouldSpawnForestFarmForage.HasValue)
			{
				_shouldSpawnForestFarmForage = map.Properties.ContainsKey("SpawnForestFarmForage");
			}
			if (_shouldSpawnForestFarmForage.Value)
			{
				return true;
			}
		}
		return Game1.whichFarm == 2;
	}

	public bool ShouldSpawnBeachFarmForage()
	{
		if (map != null)
		{
			if (!_shouldSpawnBeachFarmForage.HasValue)
			{
				_shouldSpawnBeachFarmForage = map.Properties.ContainsKey("SpawnBeachFarmForage");
			}
			if (_shouldSpawnBeachFarmForage.Value)
			{
				return true;
			}
		}
		return Game1.whichFarm == 6;
	}

	public bool SpawnsForage()
	{
		if (!ShouldSpawnForestFarmForage())
		{
			return ShouldSpawnBeachFarmForage();
		}
		return true;
	}

	public bool doesFarmCaveNeedHarvesting()
	{
		return farmCaveReady.Value;
	}
}
