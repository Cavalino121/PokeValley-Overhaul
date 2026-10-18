using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Crops;
using StardewValley.GameData.GiantCrops;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Mods;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace StardewValley;

public class Crop : INetObject<NetFields>, IHaveModData
{
	public const string mixedSeedsId = "770";

	public const string mixedSeedsQId = "(O)770";

	public const int seedPhase = 0;

	public const int rowOfWildSeeds = 23;

	public const int finalPhaseLength = 99999;

	public const int forageCrop_springOnion = 1;

	public const string forageCrop_springOnionID = "1";

	public const int forageCrop_ginger = 2;

	public const string forageCrop_gingerID = "2";

	/// <summary>The <see cref="F:StardewValley.Item.specialVariable" /> value which indicates the object was spawned by a farmed forage crop.</summary>
	public const int specialVariable_farmedForageCrop = 724519;

	/// <summary>The backing field for <see cref="P:StardewValley.Crop.currentLocation" />.</summary>
	private GameLocation currentLocationImpl;

	/// <summary>The number of days in each visual step of growth before the crop is harvestable. The last entry in this list is <see cref="F:StardewValley.Crop.finalPhaseLength" />.</summary>
	public readonly NetIntList phaseDays = new NetIntList();

	/// <summary>The index of this crop in the spritesheet texture (one crop per row).</summary>
	[XmlElement("rowInSpriteSheet")]
	public readonly NetInt rowInSpriteSheet = new NetInt();

	[XmlElement("phaseToShow")]
	public readonly NetInt phaseToShow = new NetInt(-1);

	[XmlElement("currentPhase")]
	public readonly NetInt currentPhase = new NetInt();

	/// <summary>The unqualified item ID produced when this crop is harvested.</summary>
	[XmlElement("indexOfHarvest")]
	public readonly NetString indexOfHarvest = new NetString();

	[XmlElement("dayOfCurrentPhase")]
	public readonly NetInt dayOfCurrentPhase = new NetInt();

	/// <summary>The seed ID, if this is a forage or wild seed crop.</summary>
	[XmlElement("whichForageCrop")]
	public readonly NetString whichForageCrop = new NetString();

	/// <summary>If set, the qualified object ID to spawn on the crop's tile when it's full-grown. The crop will be removed when the object is spawned.</summary>
	[XmlElement("overrideHarvestItemId")]
	public readonly NetString replaceWithObjectOnFullGrown = new NetString();

	/// <summary>The tint colors that can be applied to the crop sprite, if any.</summary>
	[XmlElement("tintColor")]
	public readonly NetColor tintColor = new NetColor();

	[XmlElement("flip")]
	public readonly NetBool flip = new NetBool();

	[XmlElement("fullGrown")]
	public readonly NetBool fullyGrown = new NetBool();

	/// <summary>Whether this is a raised crop on a trellis that can't be walked through.</summary>
	[XmlElement("raisedSeeds")]
	public readonly NetBool raisedSeeds = new NetBool();

	/// <summary>Whether to apply the <see cref="F:StardewValley.Crop.tintColor" />.</summary>
	[XmlElement("programColored")]
	public readonly NetBool programColored = new NetBool();

	[XmlElement("dead")]
	public readonly NetBool dead = new NetBool();

	[XmlElement("forageCrop")]
	public readonly NetBool forageCrop = new NetBool();

	/// <summary>The unqualified seed ID, if this is a regular crop.</summary>
	[XmlElement("seedIndex")]
	public readonly NetString netSeedIndex = new NetString();

	/// <summary>The asset name for the crop texture under the game's <c>Content</c> folder, or null to use <see cref="F:StardewValley.Game1.cropSpriteSheetName" />.</summary>
	[XmlElement("overrideTexturePath")]
	public readonly NetString overrideTexturePath = new NetString();

	protected Texture2D _drawnTexture;

	protected bool? _isErrorCrop;

	[XmlIgnore]
	public Vector2 drawPosition;

	[XmlIgnore]
	public Vector2 tilePosition;

	[XmlIgnore]
	public float layerDepth;

	[XmlIgnore]
	public float coloredLayerDepth;

	[XmlIgnore]
	public Rectangle sourceRect;

	[XmlIgnore]
	public Rectangle coloredSourceRect;

	private static Vector2 origin = new Vector2(8f, 24f);

	private static Vector2 smallestTileSizeOrigin = new Vector2(8f, 8f);

	/// <summary>The location containing the crop.</summary>
	[XmlIgnore]
	public GameLocation currentLocation
	{
		get
		{
			return currentLocationImpl;
		}
		set
		{
			if (value != currentLocationImpl)
			{
				currentLocationImpl = value;
				updateDrawMath(tilePosition);
			}
		}
	}

	/// <summary>The dirt which contains this crop.</summary>
	[XmlIgnore]
	public HoeDirt Dirt { get; set; }

	[XmlIgnore]
	public Texture2D DrawnCropTexture
	{
		get
		{
			if (dead.Value)
			{
				return Game1.cropSpriteSheet;
			}
			if (_drawnTexture == null)
			{
				if (overrideTexturePath.Value == null)
				{
					overrideTexturePath.Value = GetData()?.GetCustomTextureName("TileSheets\\crops");
				}
				_drawnTexture = null;
				if (overrideTexturePath.Value != null)
				{
					try
					{
						_drawnTexture = Game1.content.Load<Texture2D>(overrideTexturePath.Value);
					}
					catch (Exception)
					{
						_drawnTexture = null;
					}
				}
				if (_drawnTexture == null)
				{
					_drawnTexture = Game1.cropSpriteSheet;
				}
			}
			return _drawnTexture;
		}
	}

	/// <inheritdoc />
	[XmlIgnore]
	public ModDataDictionary modData { get; } = new ModDataDictionary();

	/// <inheritdoc />
	[XmlElement("modData")]
	public ModDataDictionary modDataForSerialization
	{
		get
		{
			return modData.GetForSerialization();
		}
		set
		{
			modData.SetFromSerialization(value);
		}
	}

	public NetFields NetFields { get; } = new NetFields("Crop");

	public Crop()
	{
		NetFields.SetOwner(this).AddField(phaseDays, "phaseDays").AddField(rowInSpriteSheet, "rowInSpriteSheet")
			.AddField(phaseToShow, "phaseToShow")
			.AddField(currentPhase, "currentPhase")
			.AddField(indexOfHarvest, "indexOfHarvest")
			.AddField(dayOfCurrentPhase, "dayOfCurrentPhase")
			.AddField(whichForageCrop, "whichForageCrop")
			.AddField(replaceWithObjectOnFullGrown, "replaceWithObjectOnFullGrown")
			.AddField(tintColor, "tintColor")
			.AddField(flip, "flip")
			.AddField(fullyGrown, "fullyGrown")
			.AddField(raisedSeeds, "raisedSeeds")
			.AddField(programColored, "programColored")
			.AddField(dead, "dead")
			.AddField(forageCrop, "forageCrop")
			.AddField(netSeedIndex, "netSeedIndex")
			.AddField(overrideTexturePath, "overrideTexturePath")
			.AddField(modData, "modData");
		dayOfCurrentPhase.fieldChangeVisibleEvent += delegate
		{
			updateDrawMath(tilePosition);
		};
		fullyGrown.fieldChangeVisibleEvent += delegate
		{
			updateDrawMath(tilePosition);
		};
		currentLocation = Game1.currentLocation;
	}

	public Crop(bool forageCrop, string which, int tileX, int tileY, GameLocation location)
		: this()
	{
		currentLocation = location;
		this.forageCrop.Value = forageCrop;
		whichForageCrop.Value = which;
		fullyGrown.Value = true;
		currentPhase.Value = 5;
		updateDrawMath(new Vector2(tileX, tileY));
	}

	public Crop(string seedId, int tileX, int tileY, GameLocation location)
		: this()
	{
		currentLocation = location;
		seedId = ResolveSeedId(seedId, location);
		if (TryGetData(seedId, out var data))
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(data.HarvestItemId);
			if (!dataOrErrorItem.HasTypeObject())
			{
				Game1.log.Warn($"Crop seed {seedId} produces non-object item {dataOrErrorItem.QualifiedItemId}, which isn't valid.");
			}
			phaseDays.AddRange(data.DaysInPhase);
			phaseDays.Add(99999);
			rowInSpriteSheet.Value = data.SpriteIndex;
			indexOfHarvest.Value = dataOrErrorItem.ItemId;
			overrideTexturePath.Value = data.GetCustomTextureName("TileSheets\\crops");
			if (isWildSeedCrop())
			{
				whichForageCrop.Value = seedId;
				replaceWithObjectOnFullGrown.Value = getRandomWildCropForSeason(onlyDeterministic: true);
			}
			else
			{
				netSeedIndex.Value = seedId;
			}
			raisedSeeds.Value = data.IsRaised;
			List<string> tintColors = data.TintColors;
			if (tintColors != null && tintColors.Count > 0)
			{
				Color? color = Utility.StringToColor(Utility.CreateRandom((double)tileX * 1000.0, tileY, Game1.dayOfMonth).ChooseFrom(data.TintColors));
				if (color.HasValue)
				{
					tintColor.Value = color.Value;
					programColored.Value = true;
				}
			}
		}
		else
		{
			netSeedIndex.Value = seedId ?? "0";
			indexOfHarvest.Value = seedId ?? "0";
		}
		flip.Value = Game1.random.NextBool();
		updateDrawMath(new Vector2(tileX, tileY));
	}

	/// <summary>Choose a random seed from a bag of mixed seeds, if applicable.</summary>
	/// <param name="itemId">The unqualified item ID for the seed item.</param>
	/// <param name="location">The location for which to resolve the crop.</param>
	/// <returns>Returns the unqualified seed ID to use.</returns>
	public static string ResolveSeedId(string itemId, GameLocation location)
	{
		if (!(itemId == "MixedFlowerSeeds"))
		{
			if (itemId == "770")
			{
				string text = getRandomLowGradeCropForThisSeason(location.GetSeason());
				if (text == "473")
				{
					text = "472";
				}
				if (location is IslandLocation)
				{
					text = Game1.random.Next(4) switch
					{
						0 => "479", 
						1 => "833", 
						2 => "481", 
						_ => "478", 
					};
				}
				return text;
			}
			return itemId;
		}
		return getRandomFlowerSeedForThisSeason(location.GetSeason());
	}

	/// <summary>Get the crop's data from <see cref="F:StardewValley.Game1.cropData" />, if found.</summary>
	public CropData GetData()
	{
		if (!TryGetData(isWildSeedCrop() ? whichForageCrop.Value : netSeedIndex.Value, out var data))
		{
			return null;
		}
		return data;
	}

	/// <summary>Try to get a crop's data from <see cref="F:StardewValley.Game1.cropData" />.</summary>
	/// <param name="seedId">The unqualified item ID for the crop's seed (i.e. the key in <see cref="F:StardewValley.Game1.cropData" />).</param>
	/// <param name="data">The crop data, if found.</param>
	/// <returns>Returns whether the crop data was found.</returns>
	public static bool TryGetData(string seedId, out CropData data)
	{
		if (seedId == null)
		{
			data = null;
			return false;
		}
		return Game1.cropData.TryGetValue(seedId, out data);
	}

	/// <summary>Get whether this crop is in season for the given location.</summary>
	/// <param name="location">The location to check.</param>
	public bool IsInSeason(GameLocation location)
	{
		if (location.SeedsIgnoreSeasonsHere())
		{
			return true;
		}
		return GetData()?.Seasons?.Contains(location.GetSeason()) ?? false;
	}

	/// <summary>Get whether a crop is in season for the given location.</summary>
	/// <param name="location">The location to check.</param>
	/// <param name="seedId">The unqualified item ID for the crop's seed.</param>
	public static bool IsInSeason(GameLocation location, string seedId)
	{
		if (location.SeedsIgnoreSeasonsHere())
		{
			return true;
		}
		if (TryGetData(seedId, out var data))
		{
			return data.Seasons?.Contains(location.GetSeason()) ?? false;
		}
		return false;
	}

	/// <summary>Get the method by which the crop can be harvested.</summary>
	public HarvestMethod GetHarvestMethod()
	{
		return GetData()?.HarvestMethod ?? HarvestMethod.Grab;
	}

	/// <summary>Get whether this crop regrows after it's harvested.</summary>
	public bool RegrowsAfterHarvest()
	{
		CropData data = GetData();
		if (data == null)
		{
			return false;
		}
		return data.RegrowDays > 0;
	}

	public virtual bool IsErrorCrop()
	{
		if (forageCrop.Value)
		{
			return false;
		}
		if (!_isErrorCrop.HasValue)
		{
			_isErrorCrop = GetData() == null;
		}
		return _isErrorCrop.Value;
	}

	public virtual void ResetPhaseDays()
	{
		CropData data = GetData();
		if (data != null)
		{
			phaseDays.Clear();
			phaseDays.AddRange(data.DaysInPhase);
			phaseDays.Add(99999);
		}
	}

	public static string getRandomLowGradeCropForThisSeason(Season season)
	{
		if (season == Season.Winter)
		{
			season = Game1.random.Choose(Season.Spring, Season.Summer, Season.Fall);
		}
		return season switch
		{
			Season.Spring => Game1.random.Next(472, 476).ToString(), 
			Season.Summer => Game1.random.Next(4) switch
			{
				0 => "487", 
				1 => "483", 
				2 => "482", 
				_ => "484", 
			}, 
			Season.Fall => Game1.random.Next(487, 491).ToString(), 
			_ => null, 
		};
	}

	public static string getRandomFlowerSeedForThisSeason(Season season)
	{
		if (season == Season.Winter)
		{
			season = Game1.random.Choose(Season.Spring, Season.Summer, Season.Fall);
		}
		return season switch
		{
			Season.Spring => Game1.random.Choose("427", "429"), 
			Season.Summer => Game1.random.Choose("455", "453", "431"), 
			Season.Fall => Game1.random.Choose("431", "425"), 
			_ => "-1", 
		};
	}

	public virtual void growCompletely()
	{
		currentPhase.Value = phaseDays.Count - 1;
		dayOfCurrentPhase.Value = 0;
		if (RegrowsAfterHarvest())
		{
			fullyGrown.Value = true;
		}
		updateDrawMath(tilePosition);
	}

	public virtual bool hitWithHoe(int xTile, int yTile, GameLocation location, HoeDirt dirt)
	{
		if (forageCrop.Value && whichForageCrop.Value == "2")
		{
			dirt.state.Value = (location.IsRainingHere() ? 1 : 0);
			Object obj = ItemRegistry.Create<Object>("(O)829");
			Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(12, new Vector2(xTile * 64, yTile * 64), Color.White, 8, Game1.random.NextBool(), 50f));
			location.playSound("dirtyHit");
			Game1.createItemDebris(obj.getOne(), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
			return true;
		}
		return false;
	}

	public virtual bool harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null, bool isForcedScytheHarvest = false)
	{
		if (dead.Value)
		{
			if (junimoHarvester != null)
			{
				return true;
			}
			return false;
		}
		bool flag = false;
		if (forageCrop.Value)
		{
			Object obj = null;
			int howMuch = 3;
			Random random = Utility.CreateDaySaveRandom(xTile * 1000, yTile * 2000);
			string value = whichForageCrop.Value;
			if (!(value == "1"))
			{
				if (value == "2")
				{
					soil.shake((float)Math.PI / 48f, (float)Math.PI / 40f, (float)(xTile * 64) < Game1.player.Position.X);
					return false;
				}
			}
			else
			{
				obj = ItemRegistry.Create<Object>("(O)399");
			}
			if (Game1.player.professions.Contains(16))
			{
				obj.Quality = 4;
			}
			else if (random.NextDouble() < (double)((float)Game1.player.ForagingLevel / 30f))
			{
				obj.Quality = 2;
			}
			else if (random.NextDouble() < (double)((float)Game1.player.ForagingLevel / 15f))
			{
				obj.Quality = 1;
			}
			Game1.stats.ItemsForaged += (uint)obj.Stack;
			if (junimoHarvester != null)
			{
				junimoHarvester.tryToAddItemToHut(obj);
				return true;
			}
			if (isForcedScytheHarvest)
			{
				Vector2 vector = new Vector2(xTile, yTile);
				Game1.createItemDebris(obj, new Vector2(vector.X * 64f + 32f, vector.Y * 64f + 32f), -1);
				Game1.player.gainExperience(2, howMuch);
				Game1.player.currentLocation.playSound("moss_cut");
				return true;
			}
			if (Game1.player.addItemToInventoryBool(obj))
			{
				Vector2 vector2 = new Vector2(xTile, yTile);
				Game1.player.animateOnce(279 + Game1.player.FacingDirection);
				Game1.player.canMove = false;
				Game1.player.currentLocation.playSound("harvest");
				DelayedAction.playSoundAfterDelay("coin", 260);
				if (!RegrowsAfterHarvest())
				{
					Game1.multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(17, new Vector2(vector2.X * 64f, vector2.Y * 64f), Color.White, 7, random.NextBool(), 125f));
					Game1.multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(14, new Vector2(vector2.X * 64f, vector2.Y * 64f), Color.White, 7, random.NextBool(), 50f));
				}
				Game1.player.gainExperience(2, howMuch);
				return true;
			}
			Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
		}
		else if (currentPhase.Value >= phaseDays.Count - 1 && (!fullyGrown.Value || dayOfCurrentPhase.Value <= 0))
		{
			if (string.IsNullOrWhiteSpace(indexOfHarvest.Value))
			{
				return true;
			}
			CropData data = GetData();
			Random random2 = Utility.CreateRandom((double)xTile * 7.0, (double)yTile * 11.0, Game1.stats.DaysPlayed, Game1.uniqueIDForThisGame);
			int fertilizerQualityBoostLevel = soil.GetFertilizerQualityBoostLevel();
			double num = 0.2 * ((double)Game1.player.FarmingLevel / 10.0) + 0.2 * (double)fertilizerQualityBoostLevel * (((double)Game1.player.FarmingLevel + 2.0) / 12.0) + 0.01;
			double num2 = Math.Min(0.75, num * 2.0);
			int num3 = 0;
			if (fertilizerQualityBoostLevel >= 3 && random2.NextDouble() < num / 2.0)
			{
				num3 = 4;
			}
			else if (random2.NextDouble() < num)
			{
				num3 = 2;
			}
			else if (random2.NextDouble() < num2 || fertilizerQualityBoostLevel >= 3)
			{
				num3 = 1;
			}
			num3 = MathHelper.Clamp(num3, data?.HarvestMinQuality ?? 0, data?.HarvestMaxQuality ?? num3);
			int num4 = 1;
			if (data != null)
			{
				int harvestMinStack = data.HarvestMinStack;
				int num5 = Math.Max(harvestMinStack, data.HarvestMaxStack);
				if (data.HarvestMaxIncreasePerFarmingLevel > 0f)
				{
					num5 += (int)((float)Game1.player.FarmingLevel * data.HarvestMaxIncreasePerFarmingLevel);
				}
				if (harvestMinStack > 1 || num5 > 1)
				{
					num4 = random2.Next(harvestMinStack, num5 + 1);
				}
			}
			if (data != null && data.ExtraHarvestChance > 0.0)
			{
				while (random2.NextDouble() < Math.Min(0.9, data.ExtraHarvestChance))
				{
					num4++;
				}
			}
			Item item = (programColored.Value ? new ColoredObject(indexOfHarvest.Value, 1, tintColor.Value)
			{
				Quality = num3
			} : ItemRegistry.Create(indexOfHarvest.Value, 1, num3));
			HarvestMethod harvestMethod = data?.HarvestMethod ?? HarvestMethod.Grab;
			if (harvestMethod == HarvestMethod.Scythe || isForcedScytheHarvest)
			{
				if (junimoHarvester != null)
				{
					DelayedAction.playSoundAfterDelay("daggerswipe", 150, junimoHarvester.currentLocation);
					if (Utility.isOnScreen(junimoHarvester.TilePoint, 64, junimoHarvester.currentLocation))
					{
						junimoHarvester.currentLocation.playSound("harvest");
						DelayedAction.playSoundAfterDelay("coin", 260, junimoHarvester.currentLocation);
					}
					junimoHarvester.tryToAddItemToHut(item.getOne());
				}
				else
				{
					Game1.createItemDebris(item.getOne(), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
				}
				flag = true;
			}
			else if (junimoHarvester != null || (item != null && Game1.player.addItemToInventoryBool(item.getOne())))
			{
				Vector2 vector3 = new Vector2(xTile, yTile);
				if (junimoHarvester == null)
				{
					Game1.player.animateOnce(279 + Game1.player.FacingDirection);
					Game1.player.canMove = false;
				}
				else
				{
					junimoHarvester.tryToAddItemToHut(item.getOne());
				}
				if (random2.NextDouble() < Game1.player.team.AverageLuckLevel() / 1500.0 + Game1.player.team.AverageDailyLuck() / 1200.0 + 9.999999747378752E-05)
				{
					num4 *= 2;
					if (junimoHarvester == null)
					{
						Game1.player.currentLocation.playSound("dwoop");
					}
					else if (Utility.isOnScreen(junimoHarvester.TilePoint, 64, junimoHarvester.currentLocation))
					{
						junimoHarvester.currentLocation.playSound("dwoop");
					}
				}
				else if (harvestMethod == HarvestMethod.Grab)
				{
					if (junimoHarvester == null)
					{
						Game1.player.currentLocation.playSound("harvest");
					}
					else if (Utility.isOnScreen(junimoHarvester.TilePoint, 64, junimoHarvester.currentLocation))
					{
						junimoHarvester.currentLocation.playSound("harvest");
					}
					if (junimoHarvester == null)
					{
						DelayedAction.playSoundAfterDelay("coin", 260, Game1.player.currentLocation);
					}
					else if (Utility.isOnScreen(junimoHarvester.TilePoint, 64, junimoHarvester.currentLocation))
					{
						DelayedAction.playSoundAfterDelay("coin", 260, junimoHarvester.currentLocation);
					}
					if (!RegrowsAfterHarvest() && (junimoHarvester == null || junimoHarvester.currentLocation.Equals(Game1.currentLocation)))
					{
						Game1.multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(17, new Vector2(vector3.X * 64f, vector3.Y * 64f), Color.White, 7, Game1.random.NextBool(), 125f));
						Game1.multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(14, new Vector2(vector3.X * 64f, vector3.Y * 64f), Color.White, 7, Game1.random.NextBool(), 50f));
					}
				}
				flag = true;
			}
			else
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
			}
			if (flag)
			{
				if (indexOfHarvest.Value == "421")
				{
					indexOfHarvest.Value = "431";
					num4 = random2.Next(1, 4);
				}
				item = (programColored.Value ? new ColoredObject(indexOfHarvest.Value, 1, tintColor.Value) : ItemRegistry.Create(indexOfHarvest.Value));
				int num6 = 0;
				if (item is Object obj2)
				{
					num6 = obj2.Price;
				}
				float num7 = (float)(16.0 * Math.Log(0.018 * (double)num6 + 1.0, Math.E));
				if (junimoHarvester == null)
				{
					Game1.player.gainExperience(0, (int)Math.Round(num7));
				}
				for (int i = 0; i < num4 - 1; i++)
				{
					if (junimoHarvester == null)
					{
						Game1.createItemDebris(item.getOne(), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
					}
					else
					{
						junimoHarvester.tryToAddItemToHut(item.getOne());
					}
				}
				string value = indexOfHarvest.Value;
				if (!(value == "262"))
				{
					if (value == "771")
					{
						soil?.Location?.playSound("cut");
						if (random2.NextDouble() < 0.1)
						{
							Item item2 = ItemRegistry.Create("(O)770");
							if (junimoHarvester == null)
							{
								Game1.createItemDebris(item2.getOne(), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
							}
							else
							{
								junimoHarvester.tryToAddItemToHut(item2.getOne());
							}
						}
					}
				}
				else if (random2.NextDouble() < 0.4)
				{
					Item item3 = ItemRegistry.Create("(O)178");
					if (junimoHarvester == null)
					{
						Game1.createItemDebris(item3.getOne(), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
					}
					else
					{
						junimoHarvester.tryToAddItemToHut(item3.getOne());
					}
				}
				int num8 = data?.RegrowDays ?? (-1);
				if (num8 <= 0)
				{
					return true;
				}
				fullyGrown.Value = true;
				if (dayOfCurrentPhase.Value == num8)
				{
					updateDrawMath(tilePosition);
				}
				dayOfCurrentPhase.Value = num8;
			}
		}
		return false;
	}

	/// <summary>Get a random qualified object ID to harvest from wild seeds.</summary>
	/// <param name="onlyDeterministic">Only return a value if it can be accurately predicted ahead of time (i.e. the harvest doesn't depend on the date that it's harvested).</param>
	/// <remarks>This uses the season associated with the crop (e.g. spring for Spring Seeds) or the current location's season.</remarks>
	public string getRandomWildCropForSeason(bool onlyDeterministic = false)
	{
		switch (whichForageCrop.Value)
		{
		case "495":
			return getRandomWildCropForSeason(Season.Spring);
		case "496":
			return getRandomWildCropForSeason(Season.Summer);
		case "497":
			return getRandomWildCropForSeason(Season.Fall);
		case "498":
			return getRandomWildCropForSeason(Season.Winter);
		default:
			if (onlyDeterministic && !currentLocation.SeedsIgnoreSeasonsHere())
			{
				return null;
			}
			return getRandomWildCropForSeason(currentLocation.GetSeason());
		}
	}

	/// <summary>Get a random qualified object ID to harvest from wild seeds.</summary>
	/// <param name="season">The season for which to choose a produce.</param>
	public string getRandomWildCropForSeason(Season season)
	{
		return season switch
		{
			Season.Spring => Game1.random.Choose("(O)16", "(O)18", "(O)20", "(O)22"), 
			Season.Summer => Game1.random.Choose("(O)396", "(O)398", "(O)402"), 
			Season.Fall => Game1.random.Choose("(O)404", "(O)406", "(O)408", "(O)410"), 
			Season.Winter => Game1.random.Choose("(O)412", "(O)414", "(O)416", "(O)418"), 
			_ => "(O)22", 
		};
	}

	public virtual Rectangle getSourceRect(int number)
	{
		if (dead.Value)
		{
			return new Rectangle(192 + number % 4 * 16, 384, 16, 32);
		}
		int num = rowInSpriteSheet.Value;
		Season seasonForLocation = Game1.GetSeasonForLocation(currentLocation);
		if (indexOfHarvest.Value == "771")
		{
			switch (seasonForLocation)
			{
			case Season.Fall:
				num = rowInSpriteSheet.Value + 1;
				break;
			case Season.Winter:
				num = rowInSpriteSheet.Value + 2;
				break;
			}
		}
		return new Rectangle(Math.Min(240, ((!fullyGrown.Value) ? (((phaseToShow.Value != -1) ? phaseToShow.Value : currentPhase.Value) + ((((phaseToShow.Value != -1) ? phaseToShow.Value : currentPhase.Value) == 0 && number % 2 == 0) ? (-1) : 0) + 1) : ((dayOfCurrentPhase.Value <= 0) ? 6 : 7)) * 16 + ((num % 2 != 0) ? 128 : 0)), num / 2 * 16 * 2, 16, 32);
	}

	/// <summary>Get the giant crops which can grow from this crop, if any.</summary>
	/// <param name="giantCrops">The giant crops which can grow from this crop.</param>
	/// <returns>Returns whether <paramref name="giantCrops" /> is non-empty.</returns>
	public bool TryGetGiantCrops(out IReadOnlyList<KeyValuePair<string, GiantCropData>> giantCrops)
	{
		giantCrops = GiantCrop.GetGiantCropsFor("(O)" + indexOfHarvest.Value);
		return giantCrops.Count > 0;
	}

	public void Kill()
	{
		dead.Value = true;
		raisedSeeds.Value = false;
	}

	public virtual void newDay(int state)
	{
		GameLocation gameLocation = currentLocation;
		Vector2 vector = tilePosition;
		Utility.Vector2ToPoint(vector);
		if (gameLocation.isOutdoors.Value && (dead.Value || !IsInSeason(gameLocation)))
		{
			Kill();
			return;
		}
		if (state != 1)
		{
			CropData data = GetData();
			if (data == null || data.NeedsWatering)
			{
				goto IL_01f4;
			}
		}
		if (!fullyGrown.Value)
		{
			dayOfCurrentPhase.Value = Math.Min(dayOfCurrentPhase.Value + 1, (phaseDays.Count > 0) ? phaseDays[Math.Min(phaseDays.Count - 1, currentPhase.Value)] : 0);
		}
		else
		{
			dayOfCurrentPhase.Value--;
		}
		if (dayOfCurrentPhase.Value >= ((phaseDays.Count > 0) ? phaseDays[Math.Min(phaseDays.Count - 1, currentPhase.Value)] : 0) && currentPhase.Value < phaseDays.Count - 1)
		{
			currentPhase.Value++;
			dayOfCurrentPhase.Value = 0;
		}
		while (currentPhase.Value < phaseDays.Count - 1 && phaseDays.Count > 0 && phaseDays[currentPhase.Value] <= 0)
		{
			currentPhase.Value++;
		}
		if (isWildSeedCrop() && phaseToShow.Value == -1 && currentPhase.Value > 0)
		{
			phaseToShow.Value = Game1.random.Next(1, 7);
		}
		TryGrowGiantCrop();
		goto IL_01f4;
		IL_01f4:
		if ((!fullyGrown.Value || dayOfCurrentPhase.Value <= 0) && currentPhase.Value >= phaseDays.Count - 1)
		{
			if (replaceWithObjectOnFullGrown.Value != null || isWildSeedCrop())
			{
				if (gameLocation.objects.TryGetValue(vector, out var value))
				{
					if (value is IndoorPot indoorPot)
					{
						indoorPot.heldObject.Value = ItemRegistry.Create<Object>(replaceWithObjectOnFullGrown.Value ?? getRandomWildCropForSeason());
						indoorPot.hoeDirt.Value.crop = null;
					}
					else
					{
						gameLocation.objects.Remove(vector);
					}
				}
				if (!gameLocation.objects.ContainsKey(vector))
				{
					Object obj = ItemRegistry.Create<Object>(replaceWithObjectOnFullGrown.Value ?? getRandomWildCropForSeason());
					obj.IsSpawnedObject = true;
					obj.CanBeGrabbed = true;
					obj.SpecialVariable = 724519;
					gameLocation.objects.Add(vector, obj);
				}
				if (gameLocation.terrainFeatures.TryGetValue(vector, out var value2) && value2 is HoeDirt hoeDirt)
				{
					hoeDirt.crop = null;
				}
			}
			if (indexOfHarvest.Value != null && indexOfHarvest.Value != null && indexOfHarvest.Value.Length > 0 && gameLocation.IsFarm)
			{
				foreach (Farmer allFarmer in Game1.getAllFarmers())
				{
					allFarmer.autoGenerateActiveDialogueEvent("cropMatured_" + indexOfHarvest.Value);
				}
			}
		}
		if (fullyGrown.Value && indexOfHarvest.Value != null && indexOfHarvest.Value != null && indexOfHarvest.Value == "595")
		{
			Game1.getFarm().hasMatureFairyRoseTonight = true;
		}
		updateDrawMath(vector);
	}

	/// <summary>Try to replace the grid of crops with this one at its top-left corner with a giant crop, if valid and the probability check passes.</summary>
	/// <param name="checkPreconditions">Whether to check that the location allows giant crops and the crop is fully grown. Setting this to false won't affect other conditions like having a grid of crops or the per-giant-crop conditions and chance.</param>
	/// <param name="random">The RNG to use for random checks, or <c>null</c> for the default seed logic.</param>
	public virtual bool TryGrowGiantCrop(bool checkPreconditions = true, Random random = null)
	{
		GameLocation gameLocation = currentLocation;
		Vector2 tile = tilePosition;
		if (checkPreconditions)
		{
			if (!(gameLocation is Farm) && !gameLocation.HasMapPropertyWithValue("AllowGiantCrops"))
			{
				return false;
			}
			if (currentPhase.Value != phaseDays.Count - 1)
			{
				return false;
			}
		}
		if (!TryGetGiantCrops(out var giantCrops))
		{
			return false;
		}
		foreach (KeyValuePair<string, GiantCropData> item in giantCrops)
		{
			string key = item.Key;
			GiantCropData value = item.Value;
			if ((value.Chance < 1f && !(random ?? Utility.CreateDaySaveRandom(tile.X, tile.Y, Game1.hash.GetDeterministicHashCode(key))).NextBool(value.Chance)) || !GameStateQuery.CheckConditions(value.Condition, gameLocation))
			{
				continue;
			}
			bool flag = true;
			for (int i = (int)tile.Y; (float)i < tile.Y + (float)value.TileSize.Y; i++)
			{
				for (int j = (int)tile.X; (float)j < tile.X + (float)value.TileSize.X; j++)
				{
					if (!(gameLocation.terrainFeatures.GetValueOrDefault(new Vector2(j, i)) is HoeDirt hoeDirt) || !(hoeDirt.crop?.indexOfHarvest.Value == indexOfHarvest.Value))
					{
						flag = false;
						break;
					}
				}
				if (!flag)
				{
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			for (int k = (int)tile.Y; (float)k < tile.Y + (float)value.TileSize.Y; k++)
			{
				for (int l = (int)tile.X; (float)l < tile.X + (float)value.TileSize.X; l++)
				{
					Vector2 key2 = new Vector2(l, k);
					((HoeDirt)gameLocation.terrainFeatures[key2]).crop = null;
				}
			}
			gameLocation.resourceClumps.Add(new GiantCrop(key, tile));
			return true;
		}
		return false;
	}

	public virtual bool isPaddyCrop()
	{
		return GetData()?.IsPaddyCrop ?? false;
	}

	public virtual bool shouldDrawDarkWhenWatered()
	{
		if (isPaddyCrop())
		{
			return false;
		}
		return !raisedSeeds.Value;
	}

	/// <summary>Get whether this is a vanilla wild seed crop.</summary>
	public virtual bool isWildSeedCrop()
	{
		if (overrideTexturePath.Value == null || overrideTexturePath.Value == Game1.cropSpriteSheet.Name)
		{
			return rowInSpriteSheet.Value == 23;
		}
		return false;
	}

	public virtual void updateDrawMath(Vector2 tileLocation)
	{
		if (tileLocation.Equals(Vector2.Zero))
		{
			return;
		}
		if (forageCrop.Value)
		{
			if (!int.TryParse(whichForageCrop.Value, out var result))
			{
				result = 1;
			}
			drawPosition = new Vector2(tileLocation.X * 64f + ((tileLocation.X * 11f + tileLocation.Y * 7f) % 10f - 5f) + 32f, tileLocation.Y * 64f + ((tileLocation.Y * 11f + tileLocation.X * 7f) % 10f - 5f) + 32f);
			layerDepth = (tileLocation.Y * 64f + 32f + ((tileLocation.Y * 11f + tileLocation.X * 7f) % 10f - 5f)) / 10000f;
			sourceRect = new Rectangle((int)(tileLocation.X * 51f + tileLocation.Y * 77f) % 3 * 16, 128 + result * 16, 16, 16);
		}
		else
		{
			drawPosition = new Vector2(tileLocation.X * 64f + ((!shouldDrawDarkWhenWatered() || currentPhase.Value >= phaseDays.Count - 1) ? 0f : ((tileLocation.X * 11f + tileLocation.Y * 7f) % 10f - 5f)) + 32f, tileLocation.Y * 64f + ((raisedSeeds.Value || currentPhase.Value >= phaseDays.Count - 1) ? 0f : ((tileLocation.Y * 11f + tileLocation.X * 7f) % 10f - 5f)) + 32f);
			layerDepth = (tileLocation.Y * 64f + 32f + ((!shouldDrawDarkWhenWatered() || currentPhase.Value >= phaseDays.Count - 1) ? 0f : ((tileLocation.Y * 11f + tileLocation.X * 7f) % 10f - 5f))) / 10000f / ((currentPhase.Value == 0 && shouldDrawDarkWhenWatered()) ? 2f : 1f);
			sourceRect = getSourceRect((int)tileLocation.X * 7 + (int)tileLocation.Y * 11);
			coloredSourceRect = new Rectangle(((!fullyGrown.Value) ? (currentPhase.Value + 1 + 1) : ((dayOfCurrentPhase.Value <= 0) ? 6 : 7)) * 16 + ((rowInSpriteSheet.Value % 2 != 0) ? 128 : 0), rowInSpriteSheet.Value / 2 * 16 * 2, 16, 32);
			coloredLayerDepth = (tileLocation.Y * 64f + 32f + ((tileLocation.Y * 11f + tileLocation.X * 7f) % 10f - 5f)) / 10000f / (float)((currentPhase.Value != 0 || !shouldDrawDarkWhenWatered()) ? 1 : 2);
		}
		tilePosition = tileLocation;
	}

	public virtual void draw(SpriteBatch b, Vector2 tileLocation, Color toTint, float rotation)
	{
		Vector2 position = Game1.GlobalToLocal(Game1.viewport, drawPosition);
		if (forageCrop.Value)
		{
			if (whichForageCrop.Value == "2")
			{
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + ((tileLocation.X * 11f + tileLocation.Y * 7f) % 10f - 5f) + 32f, tileLocation.Y * 64f + ((tileLocation.Y * 11f + tileLocation.X * 7f) % 10f - 5f) + 64f)), new Rectangle(128 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(tileLocation.X * 111f + tileLocation.Y * 77f)) % 800.0 / 200.0) * 16, 128, 16, 16), Color.White, rotation, new Vector2(8f, 16f), 4f, SpriteEffects.None, (tileLocation.Y * 64f + 32f + ((tileLocation.Y * 11f + tileLocation.X * 7f) % 10f - 5f)) / 10000f);
			}
			else
			{
				b.Draw(Game1.mouseCursors, position, sourceRect, Color.White, 0f, smallestTileSizeOrigin, 4f, SpriteEffects.None, layerDepth);
			}
			return;
		}
		if (IsErrorCrop())
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(O)" + indexOfHarvest.Value);
			b.Draw(dataOrErrorItem.GetTexture(), position, dataOrErrorItem.GetSourceRect(), toTint, rotation, new Vector2(8f, 8f), 4f, SpriteEffects.None, layerDepth);
			return;
		}
		SpriteEffects effects = (flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
		b.Draw(DrawnCropTexture, position, sourceRect, toTint, rotation, origin, 4f, effects, layerDepth);
		Color value = tintColor.Value;
		if (!value.Equals(Color.White) && currentPhase.Value == phaseDays.Count - 1 && !dead.Value)
		{
			b.Draw(DrawnCropTexture, position, coloredSourceRect, value, rotation, origin, 4f, effects, coloredLayerDepth);
		}
	}

	public virtual void drawInMenu(SpriteBatch b, Vector2 screenPosition, Color toTint, float rotation, float scale, float layerDepth)
	{
		if (IsErrorCrop())
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(O)" + indexOfHarvest.Value);
			b.Draw(dataOrErrorItem.GetTexture(), screenPosition, dataOrErrorItem.GetSourceRect(), toTint, rotation, new Vector2(32f, 32f), scale, flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
		}
		else
		{
			b.Draw(DrawnCropTexture, screenPosition, getSourceRect(0), toTint, rotation, new Vector2(32f, 96f), scale, flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
		}
	}

	public virtual void drawWithOffset(SpriteBatch b, Vector2 tileLocation, Color toTint, float rotation, Vector2 offset)
	{
		if (IsErrorCrop())
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(O)" + indexOfHarvest.Value);
			b.Draw(dataOrErrorItem.GetTexture(), Game1.GlobalToLocal(Game1.viewport, offset + new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)), dataOrErrorItem.GetSourceRect(), toTint, rotation, new Vector2(8f, 8f), 4f, flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (tileLocation.Y + 0.66f) * 64f / 10000f + tileLocation.X * 1E-05f);
			return;
		}
		if (forageCrop.Value)
		{
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, offset + new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)), sourceRect, Color.White, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, (tileLocation.Y + 0.66f) * 64f / 10000f + tileLocation.X * 1E-05f);
			return;
		}
		b.Draw(DrawnCropTexture, Game1.GlobalToLocal(Game1.viewport, offset + new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)), sourceRect, toTint, rotation, new Vector2(8f, 24f), 4f, flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (tileLocation.Y + 0.66f) * 64f / 10000f + tileLocation.X * 1E-05f);
		if (!tintColor.Equals(Color.White) && currentPhase.Value == phaseDays.Count - 1 && !dead.Value)
		{
			b.Draw(DrawnCropTexture, Game1.GlobalToLocal(Game1.viewport, offset + new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)), coloredSourceRect, tintColor.Value, rotation, new Vector2(8f, 24f), 4f, flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (tileLocation.Y + 0.67f) * 64f / 10000f + tileLocation.X * 1E-05f);
		}
	}
}
