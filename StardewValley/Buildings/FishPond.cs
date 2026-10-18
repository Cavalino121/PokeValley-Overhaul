using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.FishPonds;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StardewValley.Buildings;

public class FishPond : Building
{
	public const int MAXIMUM_OCCUPANCY = 10;

	public static readonly float FISHING_MILLISECONDS = 1000f;

	public static readonly int HARVEST_BASE_EXP = 10;

	public static readonly float HARVEST_OUTPUT_EXP_MULTIPLIER = 0.04f;

	public static readonly int QUEST_BASE_EXP = 20;

	public static readonly float QUEST_SPAWNRATE_EXP_MULTIPIER = 5f;

	public const int NUMBER_OF_NETTING_STYLE_TYPES = 4;

	[XmlArrayItem("int")]
	public readonly NetString fishType = new NetString();

	public readonly NetInt lastUnlockedPopulationGate = new NetInt(0);

	public readonly NetBool hasCompletedRequest = new NetBool(value: false);

	/// <summary>Whether a player has added a golden cracker to the fish pond.</summary>
	public readonly NetBool goldenAnimalCracker = new NetBool(value: false);

	/// <summary>Whether a player has added a golden cracker to the fish pond, but it hasn't landed yet.</summary>
	[XmlIgnore]
	public readonly NetBool isPlayingGoldenCrackerAnimation = new NetBool(value: false);

	public readonly NetRef<Object> sign = new NetRef<Object>();

	public readonly NetColor overrideWaterColor = new NetColor(Color.White);

	public readonly NetRef<Item> output = new NetRef<Item>();

	public readonly NetRef<Item> neededItem = new NetRef<Item>();

	public readonly NetIntDelta neededItemCount = new NetIntDelta(0);

	public readonly NetInt daysSinceSpawn = new NetInt(0);

	public readonly NetInt nettingStyle = new NetInt(0);

	public readonly NetInt seedOffset = new NetInt(0);

	public readonly NetBool hasSpawnedFish = new NetBool(value: false);

	[XmlIgnore]
	public readonly NetMutex needsMutex = new NetMutex();

	[XmlIgnore]
	protected bool _hasAnimatedSpawnedFish;

	[XmlIgnore]
	protected float _delayUntilFishSilhouetteAdded;

	[XmlIgnore]
	protected int _numberOfFishToJump;

	[XmlIgnore]
	protected float _timeUntilFishHop;

	[XmlIgnore]
	protected Object _fishObject;

	[XmlIgnore]
	public List<PondFishSilhouette> _fishSilhouettes = new List<PondFishSilhouette>();

	[XmlIgnore]
	public List<JumpingFish> _jumpingFish = new List<JumpingFish>();

	[XmlIgnore]
	private readonly NetEvent0 animateHappyFishEvent = new NetEvent0();

	[XmlIgnore]
	public TemporaryAnimatedSpriteList animations = new TemporaryAnimatedSpriteList();

	[XmlIgnore]
	protected FishPondData _fishPondData;

	public int FishCount => currentOccupants.Value;

	public FishPond(Vector2 tileLocation)
		: base("Fish Pond", tileLocation)
	{
		UpdateMaximumOccupancy();
		fadeWhenPlayerIsBehind.Value = false;
		Reseed();
	}

	public FishPond()
		: this(Vector2.Zero)
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(fishType, "fishType").AddField(output, "output").AddField(daysSinceSpawn, "daysSinceSpawn")
			.AddField(lastUnlockedPopulationGate, "lastUnlockedPopulationGate")
			.AddField(animateHappyFishEvent, "animateHappyFishEvent")
			.AddField(hasCompletedRequest, "hasCompletedRequest")
			.AddField(goldenAnimalCracker, "goldenAnimalCracker")
			.AddField(isPlayingGoldenCrackerAnimation, "isPlayingGoldenCrackerAnimation")
			.AddField(neededItem, "neededItem")
			.AddField(seedOffset, "seedOffset")
			.AddField(hasSpawnedFish, "hasSpawnedFish")
			.AddField(needsMutex.NetFields, "needsMutex.NetFields")
			.AddField(neededItemCount, "neededItemCount")
			.AddField(overrideWaterColor, "overrideWaterColor")
			.AddField(sign, "sign")
			.AddField(nettingStyle, "nettingStyle");
		animateHappyFishEvent.onEvent += AnimateHappyFish;
		fishType.fieldChangeVisibleEvent += OnFishTypeChanged;
	}

	public virtual void OnFishTypeChanged(NetString field, string old_value, string new_value)
	{
		_fishSilhouettes.Clear();
		_jumpingFish.Clear();
		_fishObject = null;
	}

	public virtual void Reseed()
	{
		seedOffset.Value = DateTime.UtcNow.Millisecond;
	}

	public List<PondFishSilhouette> GetFishSilhouettes()
	{
		return _fishSilhouettes;
	}

	public void UpdateMaximumOccupancy()
	{
		GetFishPondData();
		if (_fishPondData == null)
		{
			return;
		}
		if (_fishPondData.MaxPopulation > 0)
		{
			maxOccupants.Value = _fishPondData.MaxPopulation;
			return;
		}
		for (int i = 1; i <= 10; i++)
		{
			if (i <= lastUnlockedPopulationGate.Value)
			{
				maxOccupants.Set(i);
				continue;
			}
			if (!(_fishPondData.PopulationGates?.ContainsKey(i) ?? false))
			{
				maxOccupants.Set(i);
				continue;
			}
			break;
		}
	}

	public FishPondData GetFishPondData()
	{
		FishPondData rawData = GetRawData(fishType.Value);
		if (rawData == null)
		{
			return null;
		}
		_fishPondData = rawData;
		if (_fishPondData.SpawnTime == -1)
		{
			int price = GetFishObject().Price;
			if (price <= 30)
			{
				_fishPondData.SpawnTime = 1;
			}
			else if (price <= 80)
			{
				_fishPondData.SpawnTime = 2;
			}
			else if (price <= 120)
			{
				_fishPondData.SpawnTime = 3;
			}
			else if (price <= 250)
			{
				_fishPondData.SpawnTime = 4;
			}
			else
			{
				_fishPondData.SpawnTime = 5;
			}
		}
		return _fishPondData;
	}

	/// <summary>Get the data entry matching a fish item ID.</summary>
	/// <param name="itemId">The unqualified fish item ID.</param>
	public static FishPondData GetRawData(string itemId)
	{
		if (itemId == null)
		{
			return null;
		}
		HashSet<string> baseContextTags = ItemContextTagManager.GetBaseContextTags(itemId);
		if (baseContextTags.Contains("fish_pond_ignore"))
		{
			return null;
		}
		FishPondData fishPondData = null;
		foreach (FishPondData item in DataLoader.FishPondData(Game1.content))
		{
			if (!(fishPondData?.Precedence <= item.Precedence) && ItemContextTagManager.DoAllTagsMatch(item.RequiredTags, baseContextTags))
			{
				fishPondData = item;
			}
		}
		return fishPondData;
	}

	public Item GetFishProduce(Random random = null)
	{
		if (random == null)
		{
			random = Game1.random;
		}
		FishPondData fishPondData = GetFishPondData();
		if (fishPondData == null)
		{
			return null;
		}
		GameLocation parentLocation = GetParentLocation();
		Object fish = GetFishObject();
		FishPondReward selectedOutput = null;
		foreach (FishPondReward producedItem in fishPondData.ProducedItems)
		{
			if (!(selectedOutput?.Precedence <= producedItem.Precedence) && currentOccupants.Value >= producedItem.RequiredPopulation && random.NextBool(producedItem.Chance) && GameStateQuery.CheckConditions(producedItem.Condition, parentLocation, null, null, fish))
			{
				selectedOutput = producedItem;
			}
		}
		Item item = null;
		if (selectedOutput != null)
		{
			item = ItemQueryResolver.TryResolveRandomItem(selectedOutput, new ItemQueryContext(parentLocation, null, null, $"fish pond data '{fishType.Value}' > reward '{selectedOutput.Id}'"), avoidRepeat: false, null, (string id) => (!(ItemRegistry.QualifyItemId(selectedOutput.ItemId) == "(O)812")) ? id : ("FLAVORED_ITEM Roe " + fish.QualifiedItemId), fish);
		}
		if (item != null)
		{
			if (item.Name.Contains("Roe"))
			{
				while (random.NextDouble() < 0.2)
				{
					item.Stack++;
				}
			}
			if (goldenAnimalCracker.Value)
			{
				item.Stack *= 2;
			}
		}
		return item;
	}

	private Item CreateFishInstance()
	{
		return new Object(fishType.Value, 1);
	}

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (daysOfConstructionLeft.Value <= 0 && occupiesTile(tileLocation))
		{
			if (who.isMoving())
			{
				Game1.haltAfterCheck = false;
			}
			if (who.ActiveObject != null && performActiveObjectDropInAction(who, probe: false))
			{
				return true;
			}
			if (output.Value != null)
			{
				Item value = output.Value;
				output.Value = null;
				if (who.addItemToInventoryBool(value))
				{
					Game1.playSound("coin");
					int num = 0;
					if (value is Object obj)
					{
						num = (int)((float)obj.sellToStorePrice(-1L) * HARVEST_OUTPUT_EXP_MULTIPLIER);
					}
					who.gainExperience(1, num + HARVEST_BASE_EXP);
				}
				else
				{
					output.Value = value;
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
				}
				return true;
			}
			if (who.ActiveObject != null && HasUnresolvedNeeds() && who.ActiveObject.QualifiedItemId == neededItem.Value.QualifiedItemId)
			{
				if (neededItemCount.Value == 1)
				{
					showObjectThrownIntoPondAnimation(who, who.ActiveObject, delegate
					{
						if (neededItemCount.Value <= 0)
						{
							Game1.playSound("jingle1");
						}
					});
				}
				else
				{
					showObjectThrownIntoPondAnimation(who, who.ActiveObject);
				}
				who.reduceActiveItemByOne();
				if (who == Game1.player)
				{
					neededItemCount.Value--;
					if (neededItemCount.Value <= 0)
					{
						needsMutex.RequestLock(delegate
						{
							needsMutex.ReleaseLock();
							ResolveNeeds(who);
						});
						neededItemCount.Value = -1;
					}
				}
				if (neededItemCount.Value <= 0)
				{
					animateHappyFishEvent.Fire();
				}
				return true;
			}
			if (who.ActiveObject != null && (who.ActiveObject.Category == -4 || who.ActiveObject.QualifiedItemId == "(O)393" || who.ActiveObject.QualifiedItemId == "(O)397"))
			{
				if (fishType.Value != null)
				{
					if (!isLegalFishForPonds(fishType.Value))
					{
						string displayName = who.ActiveObject.DisplayName;
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:CantPutInPonds", displayName.ToLower()));
						return true;
					}
					if (who.ActiveObject.ItemId != fishType.Value)
					{
						string displayName2 = who.ActiveObject.DisplayName;
						if (who.ActiveObject.QualifiedItemId == "(O)393" || who.ActiveObject.QualifiedItemId == "(O)397")
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:WrongFishTypeCoral", displayName2));
						}
						else
						{
							string displayName3 = ItemRegistry.GetDataOrErrorItem(fishType.Value).DisplayName;
							if (Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.de)
							{
								Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:WrongFishType", displayName2, displayName3));
							}
							else
							{
								Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:WrongFishType", displayName2.ToLower(), displayName3.ToLower()));
							}
						}
						return true;
					}
					if (currentOccupants.Value >= maxOccupants.Value)
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:PondFull"));
						return true;
					}
					return addFishToPond(who, who.ActiveObject);
				}
				if (!isLegalFishForPonds(who.ActiveObject.ItemId))
				{
					string displayName4 = who.ActiveObject.DisplayName;
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:CantPutInPonds", displayName4));
					return true;
				}
				return addFishToPond(who, who.ActiveObject);
			}
			if (fishType.Value != null)
			{
				if (Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
				{
					Game1.playSound("bigSelect");
					Game1.activeClickableMenu = new PondQueryMenu(this);
					return true;
				}
			}
			else if (Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:NoFish"));
				return true;
			}
		}
		return base.doAction(tileLocation, who);
	}

	public void AnimateHappyFish()
	{
		_numberOfFishToJump = currentOccupants.Value;
		_timeUntilFishHop = 1f;
	}

	public Vector2 GetItemBucketTile()
	{
		return new Vector2(tileX.Value + 4, tileY.Value + 4);
	}

	public Vector2 GetRequestTile()
	{
		return new Vector2(tileX.Value + 2, tileY.Value + 2);
	}

	public Vector2 GetCenterTile()
	{
		return new Vector2(tileX.Value + 2, tileY.Value + 2);
	}

	public void ResolveNeeds(Farmer who)
	{
		Reseed();
		hasCompletedRequest.Value = true;
		lastUnlockedPopulationGate.Value = maxOccupants.Value + 1;
		UpdateMaximumOccupancy();
		daysSinceSpawn.Value = 0;
		int num = 0;
		FishPondData fishPondData = GetFishPondData();
		if (fishPondData != null)
		{
			num = (int)((float)fishPondData.SpawnTime * QUEST_SPAWNRATE_EXP_MULTIPIER);
		}
		who.gainExperience(1, num + QUEST_BASE_EXP);
		Random r = Utility.CreateDaySaveRandom(seedOffset.Value);
		Game1.showGlobalMessage(PondQueryMenu.getCompletedRequestString(this, GetFishObject(), r));
	}

	public override void resetLocalState()
	{
		base.resetLocalState();
		_jumpingFish.Clear();
		while (_fishSilhouettes.Count < currentOccupants.Value)
		{
			PondFishSilhouette pondFishSilhouette = new PondFishSilhouette(this);
			_fishSilhouettes.Add(pondFishSilhouette);
			pondFishSilhouette.position = (GetCenterTile() + new Vector2(Utility.Lerp(-0.5f, 0.5f, (float)Game1.random.NextDouble()) * (float)(tilesWide.Value - 2), Utility.Lerp(-0.5f, 0.5f, (float)Game1.random.NextDouble()) * (float)(tilesHigh.Value - 2))) * 64f;
		}
	}

	private bool isLegalFishForPonds(string itemId)
	{
		return GetRawData(itemId) != null;
	}

	private void showObjectThrownIntoPondAnimation(Farmer who, Object whichObject, Action callback = null)
	{
		who.faceGeneralDirection(GetCenterTile() * 64f + new Vector2(32f, 32f));
		if (who.FacingDirection == 1 || who.FacingDirection == 3)
		{
			float num = Vector2.Distance(who.Position, GetCenterTile() * 64f);
			float num2 = GetCenterTile().Y * 64f + 32f - who.position.Y;
			num -= 8f;
			float num3 = 0.0025f;
			float num4 = (float)((double)num * Math.Sqrt(num3 / (2f * (num + 96f))));
			float num5 = 2f * (num4 / num3) + (float)((Math.Sqrt(num4 * num4 + 2f * num3 * 96f) - (double)num4) / (double)num3);
			num5 += num2;
			float num6 = 0f;
			if (num2 > 0f)
			{
				num6 = num2 / 832f;
				num5 += num6 * 200f;
			}
			Game1.playSound("throwDownITem");
			TemporaryAnimatedSpriteList temporaryAnimatedSpriteList = new TemporaryAnimatedSpriteList();
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(whichObject.QualifiedItemId);
			temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite(dataOrErrorItem.GetTextureName(), dataOrErrorItem.GetSourceRect(), who.Position + new Vector2(0f, -64f), flipped: false, 0f, Color.White)
			{
				scale = 4f,
				layerDepth = 1f,
				totalNumberOfLoops = 1,
				interval = num5,
				motion = new Vector2((float)((who.FacingDirection != 3) ? 1 : (-1)) * (num4 - num6), (0f - num4) * 3f / 2f),
				acceleration = new Vector2(0f, num3),
				timeBasedMotion = true
			});
			temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite(28, 100f, 2, 1, GetCenterTile() * 64f, flicker: false, flipped: false)
			{
				delayBeforeAnimationStart = (int)num5,
				layerDepth = (((float)tileY.Value + 0.5f) * 64f + 2f) / 10000f
			});
			temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 55f, 8, 0, GetCenterTile() * 64f, flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
			{
				delayBeforeAnimationStart = (int)num5
			});
			temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 65f, 8, 0, GetCenterTile() * 64f + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-16, 32)), flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
			{
				delayBeforeAnimationStart = (int)num5
			});
			temporaryAnimatedSpriteList.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 75f, 8, 0, GetCenterTile() * 64f + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-16, 32)), flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
			{
				delayBeforeAnimationStart = (int)num5
			});
			if (who.IsLocalPlayer)
			{
				DelayedAction.playSoundAfterDelay("waterSlosh", (int)num5, who.currentLocation);
				if (callback != null)
				{
					DelayedAction.functionAfterDelay(callback, (int)num5);
				}
			}
			if (fishType.Value != null && whichObject.ItemId == fishType.Value)
			{
				_delayUntilFishSilhouetteAdded = num5 / 1000f;
			}
			Game1.multiplayer.broadcastSprites(who.currentLocation, temporaryAnimatedSpriteList);
			return;
		}
		float num7 = Vector2.Distance(who.Position, GetCenterTile() * 64f);
		float num8 = Math.Abs(num7);
		if (who.FacingDirection == 0)
		{
			num7 = 0f - num7;
			num8 += 64f;
		}
		float num9 = GetCenterTile().X * 64f - who.position.X;
		float num10 = 0.0025f;
		float num11 = (float)Math.Sqrt(2f * num10 * num8);
		float num12 = (float)(Math.Sqrt(2f * (num8 - num7) / num10) + (double)(num11 / num10));
		num12 *= 1.05f;
		num12 = ((who.FacingDirection != 0) ? (num12 * 2.5f) : (num12 * 0.7f));
		num12 -= Math.Abs(num9) / ((who.FacingDirection == 0) ? 100f : 2f);
		Game1.playSound("throwDownITem");
		TemporaryAnimatedSpriteList temporaryAnimatedSpriteList2 = new TemporaryAnimatedSpriteList();
		ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(whichObject.QualifiedItemId);
		temporaryAnimatedSpriteList2.Add(new TemporaryAnimatedSprite(dataOrErrorItem2.GetTextureName(), dataOrErrorItem2.GetSourceRect(), who.Position + new Vector2(0f, -64f), flipped: false, 0f, Color.White)
		{
			scale = 4f,
			layerDepth = 1f,
			totalNumberOfLoops = 1,
			interval = num12,
			motion = new Vector2(num9 / ((who.FacingDirection == 0) ? 900f : 1000f), 0f - num11),
			acceleration = new Vector2(0f, num10),
			timeBasedMotion = true
		});
		temporaryAnimatedSpriteList2.Add(new TemporaryAnimatedSprite(28, 100f, 2, 1, GetCenterTile() * 64f, flicker: false, flipped: false)
		{
			delayBeforeAnimationStart = (int)num12,
			layerDepth = (((float)tileY.Value + 0.5f) * 64f + 2f) / 10000f
		});
		temporaryAnimatedSpriteList2.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 55f, 8, 0, GetCenterTile() * 64f, flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
		{
			delayBeforeAnimationStart = (int)num12
		});
		temporaryAnimatedSpriteList2.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 65f, 8, 0, GetCenterTile() * 64f + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-16, 32)), flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
		{
			delayBeforeAnimationStart = (int)num12
		});
		temporaryAnimatedSpriteList2.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 75f, 8, 0, GetCenterTile() * 64f + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-16, 32)), flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
		{
			delayBeforeAnimationStart = (int)num12
		});
		if (who.IsLocalPlayer)
		{
			DelayedAction.playSoundAfterDelay("waterSlosh", (int)num12, who.currentLocation);
			if (callback != null)
			{
				DelayedAction.functionAfterDelay(callback, (int)num12);
			}
		}
		if (fishType.Value != null && whichObject.ItemId == fishType.Value)
		{
			_delayUntilFishSilhouetteAdded = num12 / 1000f;
		}
		Game1.multiplayer.broadcastSprites(who.currentLocation, temporaryAnimatedSpriteList2);
	}

	private bool addFishToPond(Farmer who, Object fish)
	{
		who.reduceActiveItemByOne();
		currentOccupants.Value++;
		if (currentOccupants.Value == 1)
		{
			fishType.Value = fish.ItemId;
			_fishPondData = null;
			UpdateMaximumOccupancy();
		}
		showObjectThrownIntoPondAnimation(who, fish);
		return true;
	}

	public override void dayUpdate(int dayOfMonth)
	{
		hasSpawnedFish.Value = false;
		_hasAnimatedSpawnedFish = false;
		if (hasCompletedRequest.Value)
		{
			neededItem.Value = null;
			neededItemCount.Set(-1);
			hasCompletedRequest.Value = false;
		}
		FishPondData fishPondData = GetFishPondData();
		if (currentOccupants.Value > 0 && fishPondData != null)
		{
			Random random = Utility.CreateDaySaveRandom(tileX.Value * 1000, tileY.Value * 2000);
			if ((fishPondData.BaseMinProduceChance >= fishPondData.BaseMaxProduceChance) ? random.NextBool(fishPondData.BaseMinProduceChance) : (random.NextDouble() < (double)Utility.Lerp(fishPondData.BaseMinProduceChance, fishPondData.BaseMaxProduceChance, (float)currentOccupants.Value / 10f)))
			{
				output.Value = GetFishProduce(random);
			}
			daysSinceSpawn.Value++;
			if (daysSinceSpawn.Value > fishPondData.SpawnTime)
			{
				daysSinceSpawn.Value = fishPondData.SpawnTime;
			}
			if (daysSinceSpawn.Value >= fishPondData.SpawnTime)
			{
				if (TryGetNeededItemData(out var itemId, out var count))
				{
					if (currentOccupants.Value >= maxOccupants.Value && neededItem.Value == null)
					{
						neededItem.Value = ItemRegistry.Create(itemId);
						neededItemCount.Value = count;
					}
				}
				else
				{
					SpawnFish();
				}
			}
			if (currentOccupants.Value == 10 && fishType.Value == "717")
			{
				foreach (Farmer allFarmer in Game1.getAllFarmers())
				{
					if (allFarmer.mailReceived.Add("FullCrabPond"))
					{
						allFarmer.activeDialogueEvents["FullCrabPond"] = 14;
					}
				}
			}
			doFishSpecificWaterColoring();
		}
		base.dayUpdate(dayOfMonth);
	}

	private void doFishSpecificWaterColoring()
	{
		FishPondData fishPondData = GetFishPondData();
		Color? color = null;
		if (fishPondData != null && fishPondData.WaterColor?.Count > 0)
		{
			foreach (FishPondWaterColor item in fishPondData.WaterColor)
			{
				if (currentOccupants.Value >= item.MinPopulation && lastUnlockedPopulationGate.Value >= item.MinUnlockedPopulationGate && (item.Condition == null || GameStateQuery.CheckConditions(item.Condition, GetParentLocation(), null, null, GetFishObject())))
				{
					if (item.Color.EqualsIgnoreCase("CopyFromInput"))
					{
						Object fishObject = GetFishObject();
						ColoredObject obj = fishObject as ColoredObject;
						color = ((obj != null) ? new Color?(obj.color.Value) : ItemContextTagManager.GetColorFromTags(fishObject));
					}
					else
					{
						color = Utility.StringToColor(item.Color);
					}
					break;
				}
			}
		}
		overrideWaterColor.Value = color ?? Color.White;
	}

	/// <inheritdoc />
	public override Color? GetWaterColor(Vector2 tile)
	{
		if (!(overrideWaterColor.Value != Color.White))
		{
			return null;
		}
		return overrideWaterColor.Value;
	}

	public bool JumpFish()
	{
		if (_fishSilhouettes.Count == 0)
		{
			return false;
		}
		PondFishSilhouette pondFishSilhouette = Game1.random.ChooseFrom(_fishSilhouettes);
		_fishSilhouettes.Remove(pondFishSilhouette);
		_jumpingFish.Add(new JumpingFish(this, pondFishSilhouette.position, (GetCenterTile() + new Vector2(0.5f, 0.5f)) * 64f));
		return true;
	}

	public void SpawnFish()
	{
		if (currentOccupants.Value < maxOccupants.Value && currentOccupants.Value > 0)
		{
			hasSpawnedFish.Value = true;
			daysSinceSpawn.Value = 0;
			currentOccupants.Value += 1;
			if (currentOccupants.Value > maxOccupants.Value)
			{
				currentOccupants.Value = maxOccupants.Value;
			}
		}
	}

	public override bool performActiveObjectDropInAction(Farmer who, bool probe)
	{
		Object activeObject = who.ActiveObject;
		if (IsValidSignItem(activeObject) && (sign.Value == null || activeObject.QualifiedItemId != sign.Value.QualifiedItemId))
		{
			if (probe)
			{
				return true;
			}
			Object value = sign.Value;
			sign.Value = (Object)activeObject.getOne();
			who.reduceActiveItemByOne();
			if (value != null)
			{
				Game1.createItemDebris(value, new Vector2((float)tileX.Value + 0.5f, tileY.Value + tilesHigh.Value) * 64f, 3, who.currentLocation);
			}
			who.currentLocation.playSound("axe");
			return true;
		}
		if (activeObject?.QualifiedItemId == "(O)GoldenAnimalCracker" && !goldenAnimalCracker.Value && currentOccupants.Value > 0)
		{
			if (probe)
			{
				return true;
			}
			who.reduceActiveItemByOne();
			goldenAnimalCracker.Value = true;
			isPlayingGoldenCrackerAnimation.Value = true;
			showObjectThrownIntoPondAnimation(who, activeObject, delegate
			{
				isPlayingGoldenCrackerAnimation.Value = false;
			});
			return true;
		}
		return base.performActiveObjectDropInAction(who, probe);
	}

	public override void performToolAction(Tool t, int tileX, int tileY)
	{
		if ((t is Axe || t is Pickaxe) && sign.Value != null)
		{
			if (t.getLastFarmerToUse() != null)
			{
				Game1.createItemDebris(sign.Value, new Vector2((float)base.tileX.Value + 0.5f, base.tileY.Value + tilesHigh.Value) * 64f, 3, t.getLastFarmerToUse().currentLocation);
			}
			sign.Value = null;
			t.getLastFarmerToUse().currentLocation.playSound("hammer", new Vector2(tileX, tileY));
		}
		base.performToolAction(t, tileX, tileY);
	}

	/// <inheritdoc />
	public override void performActionOnConstruction(GameLocation location, Farmer who)
	{
		base.performActionOnConstruction(location, who);
		nettingStyle.Value = (tileX.Value / 3 + tileY.Value / 3) % 3;
	}

	/// <inheritdoc />
	public override void performActionOnBuildingPlacement()
	{
		base.performActionOnBuildingPlacement();
		nettingStyle.Value = (tileX.Value / 3 + tileY.Value / 3) % 3;
	}

	public bool HasUnresolvedNeeds()
	{
		if (neededItem.Value != null && TryGetNeededItemData(out var _, out var _))
		{
			return !hasCompletedRequest.Value;
		}
		return false;
	}

	private bool TryGetNeededItemData(out string itemId, out int count)
	{
		itemId = null;
		count = 1;
		if (currentOccupants.Value < maxOccupants.Value)
		{
			return false;
		}
		GetFishPondData();
		if (_fishPondData?.PopulationGates != null)
		{
			if (maxOccupants.Value + 1 <= lastUnlockedPopulationGate.Value)
			{
				return false;
			}
			if (_fishPondData.PopulationGates.TryGetValue(maxOccupants.Value + 1, out var value))
			{
				Random random = Utility.CreateDaySaveRandom(Utility.CreateRandomSeed(tileX.Value * 1000, tileY.Value * 2000));
				string[] array = ArgUtility.SplitBySpace(random.ChooseFrom(value));
				if (array.Length >= 1)
				{
					itemId = array[0];
				}
				if (array.Length >= 3)
				{
					count = random.Next(Convert.ToInt32(array[1]), Convert.ToInt32(array[2]) + 1);
				}
				else if (array.Length >= 2)
				{
					count = Convert.ToInt32(array[1]);
				}
				return true;
			}
		}
		return false;
	}

	public void ClearPond()
	{
		Rectangle boundingBox = GetBoundingBox();
		for (int i = 0; i < currentOccupants.Value; i++)
		{
			Vector2 pixelOrigin = Utility.PointToVector2(boundingBox.Center);
			int num = Game1.random.Next(4);
			switch (num)
			{
			case 0:
				pixelOrigin = new Vector2(Game1.random.Next(boundingBox.Left, boundingBox.Right), boundingBox.Top);
				break;
			case 1:
				pixelOrigin = new Vector2(boundingBox.Right, Game1.random.Next(boundingBox.Top, boundingBox.Bottom));
				break;
			case 2:
				pixelOrigin = new Vector2(Game1.random.Next(boundingBox.Left, boundingBox.Right), boundingBox.Bottom);
				break;
			case 3:
				pixelOrigin = new Vector2(boundingBox.Left, Game1.random.Next(boundingBox.Top, boundingBox.Bottom));
				break;
			}
			Game1.createItemDebris(CreateFishInstance(), pixelOrigin, num, Game1.currentLocation, -1, flopFish: true);
		}
		_hasAnimatedSpawnedFish = false;
		hasSpawnedFish.Value = false;
		_fishSilhouettes.Clear();
		_jumpingFish.Clear();
		goldenAnimalCracker.Value = false;
		isPlayingGoldenCrackerAnimation.Value = false;
		_fishObject = null;
		currentOccupants.Value = 0;
		daysSinceSpawn.Value = 0;
		neededItem.Value = null;
		neededItemCount.Value = -1;
		lastUnlockedPopulationGate.Value = 0;
		fishType.Value = null;
		Reseed();
		overrideWaterColor.Value = Color.White;
	}

	public Object CatchFish()
	{
		if (currentOccupants.Value == 0)
		{
			return null;
		}
		currentOccupants.Value--;
		return (Object)CreateFishInstance();
	}

	public Object GetFishObject()
	{
		if (_fishObject == null)
		{
			_fishObject = new Object(fishType.Value, 1);
		}
		return _fishObject;
	}

	public override void Update(GameTime time)
	{
		needsMutex.Update(GetParentLocation());
		animateHappyFishEvent.Poll();
		if (!_hasAnimatedSpawnedFish && hasSpawnedFish.Value && _numberOfFishToJump <= 0 && Utility.isOnScreen((GetCenterTile() + new Vector2(0.5f, 0.5f)) * 64f, 64))
		{
			_hasAnimatedSpawnedFish = true;
			if (fishType.Value != "393" && fishType.Value != "397")
			{
				_numberOfFishToJump = 1;
				_timeUntilFishHop = Utility.RandomFloat(2f, 5f);
			}
		}
		if (_delayUntilFishSilhouetteAdded > 0f)
		{
			_delayUntilFishSilhouetteAdded -= (float)time.ElapsedGameTime.TotalSeconds;
			if (_delayUntilFishSilhouetteAdded < 0f)
			{
				_delayUntilFishSilhouetteAdded = 0f;
			}
		}
		if (_numberOfFishToJump > 0 && _timeUntilFishHop > 0f)
		{
			_timeUntilFishHop -= (float)time.ElapsedGameTime.TotalSeconds;
			if (_timeUntilFishHop <= 0f && JumpFish())
			{
				_numberOfFishToJump--;
				_timeUntilFishHop = Utility.RandomFloat(0.15f, 0.25f);
			}
		}
		while (_fishSilhouettes.Count > currentOccupants.Value - _jumpingFish.Count)
		{
			_fishSilhouettes.RemoveAt(0);
		}
		if (_delayUntilFishSilhouetteAdded <= 0f)
		{
			while (_fishSilhouettes.Count < currentOccupants.Value - _jumpingFish.Count)
			{
				_fishSilhouettes.Add(new PondFishSilhouette(this));
			}
		}
		for (int i = 0; i < _fishSilhouettes.Count; i++)
		{
			_fishSilhouettes[i].Update((float)time.ElapsedGameTime.TotalSeconds);
		}
		for (int j = 0; j < _jumpingFish.Count; j++)
		{
			if (_jumpingFish[j].Update((float)time.ElapsedGameTime.TotalSeconds))
			{
				PondFishSilhouette pondFishSilhouette = new PondFishSilhouette(this);
				pondFishSilhouette.position = _jumpingFish[j].position;
				_fishSilhouettes.Add(pondFishSilhouette);
				_jumpingFish.RemoveAt(j);
				j--;
			}
		}
		base.Update(time);
	}

	public override bool isTileFishable(Vector2 tile)
	{
		if (daysOfConstructionLeft.Value > 0)
		{
			return false;
		}
		if (tile.X > (float)tileX.Value && tile.X < (float)(tileX.Value + tilesWide.Value - 1) && tile.Y > (float)tileY.Value)
		{
			return tile.Y < (float)(tileY.Value + tilesHigh.Value - 1);
		}
		return false;
	}

	public override bool CanRefillWateringCan()
	{
		return daysOfConstructionLeft.Value <= 0;
	}

	public override Rectangle? getSourceRectForMenu()
	{
		return new Rectangle(0, 0, 80, 80);
	}

	public override void drawInMenu(SpriteBatch b, int x, int y)
	{
		BuildingData data = GetData();
		y += 32;
		if (ShouldDrawShadow(data))
		{
			drawShadow(b, x, y);
		}
		b.Draw(texture.Value, new Vector2(x, y), new Rectangle(0, 80, 80, 80), new Color(60, 126, 150) * alpha, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, 0.75f);
		for (int i = tileY.Value; i < tileY.Value + 5; i++)
		{
			for (int j = tileX.Value; j < tileX.Value + 4; j++)
			{
				bool num = i == tileY.Value + 4;
				bool flag = i == tileY.Value;
				if (num)
				{
					b.Draw(Game1.mouseCursors, new Vector2(x + j * 64 + 32, y + (i + 1) * 64 - (int)Game1.currentLocation.waterPosition - 32), new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((j + i) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)), 64, 32 + (int)Game1.currentLocation.waterPosition - 5), Game1.currentLocation.waterColor.Value, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.8f);
				}
				else
				{
					b.Draw(Game1.mouseCursors, new Vector2(x + j * 64 + 32, y + i * 64 + 32 - (int)((!flag) ? Game1.currentLocation.waterPosition : 0f)), new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((j + i) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)) + (flag ? ((int)Game1.currentLocation.waterPosition) : 0), 64, 64 + (flag ? ((int)(0f - Game1.currentLocation.waterPosition)) : 0)), Game1.currentLocation.waterColor.Value, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.8f);
				}
			}
		}
		b.Draw(texture.Value, new Vector2(x, y), new Rectangle(0, 0, 80, 80), color * alpha, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, 0.9f);
		b.Draw(texture.Value, new Vector2(x + 64, y + 44 + ((Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2500.0 < 1250.0) ? 4 : 0)), new Rectangle(16, 160, 48, 7), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.95f);
		b.Draw(texture.Value, new Vector2(x, y - 128), new Rectangle(80, 0, 80, 48), color * alpha, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, 1f);
	}

	public override void OnEndMove()
	{
		foreach (PondFishSilhouette fishSilhouette in _fishSilhouettes)
		{
			fishSilhouette.position = (GetCenterTile() + new Vector2(Utility.Lerp(-0.5f, 0.5f, (float)Game1.random.NextDouble()) * (float)(tilesWide.Value - 2), Utility.Lerp(-0.5f, 0.5f, (float)Game1.random.NextDouble()) * (float)(tilesHigh.Value - 2))) * 64f;
		}
	}

	public override void draw(SpriteBatch b)
	{
		if (base.isMoving)
		{
			return;
		}
		if (daysOfConstructionLeft.Value > 0)
		{
			drawInConstruction(b);
			return;
		}
		BuildingData data = GetData();
		for (int num = animations.Count - 1; num >= 0; num--)
		{
			animations[num].draw(b);
		}
		if (ShouldDrawShadow(data))
		{
			drawShadow(b);
		}
		b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64)), new Rectangle(0, 80, 80, 80), ((overrideWaterColor.Value == Color.White) ? new Color(60, 126, 150) : overrideWaterColor.Value) * alpha, 0f, new Vector2(0f, 80f), 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f - 3f) / 10000f);
		for (int i = tileY.Value; i < tileY.Value + 5; i++)
		{
			for (int j = tileX.Value; j < tileX.Value + 4; j++)
			{
				bool num2 = i == tileY.Value + 4;
				bool flag = i == tileY.Value;
				if (num2)
				{
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(j * 64 + 32, (i + 1) * 64 - (int)Game1.currentLocation.waterPosition - 32)), new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((j + i) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)), 64, 32 + (int)Game1.currentLocation.waterPosition - 5), overrideWaterColor.Equals(Color.White) ? Game1.currentLocation.waterColor.Value : (overrideWaterColor.Value * 0.5f), 0f, Vector2.Zero, 1f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f - 2f) / 10000f);
				}
				else
				{
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(j * 64 + 32, i * 64 + 32 - (int)((!flag) ? Game1.currentLocation.waterPosition : 0f))), new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((j + i) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)) + (flag ? ((int)Game1.currentLocation.waterPosition) : 0), 64, 64 + (flag ? ((int)(0f - Game1.currentLocation.waterPosition)) : 0)), (overrideWaterColor.Value == Color.White) ? Game1.currentLocation.waterColor.Value : (overrideWaterColor.Value * 0.5f), 0f, Vector2.Zero, 1f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f - 2f) / 10000f);
				}
			}
		}
		if (overrideWaterColor.Value.Equals(Color.White))
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 64, tileY.Value * 64 + 44 + ((Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2500.0 < 1250.0) ? 4 : 0))), new Rectangle(16, 160, 48, 7), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f);
		}
		b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64)), new Rectangle(0, 0, 80, 80), color * alpha, 0f, new Vector2(0f, 80f), 4f, SpriteEffects.None, ((float)tileY.Value + 0.5f) * 64f / 10000f);
		if (nettingStyle.Value < 3)
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64 - 128)), new Rectangle(80, nettingStyle.Value * 48, 80, 48), color * alpha, 0f, new Vector2(0f, 80f), 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 2f) / 10000f);
		}
		if (sign.Value != null)
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(sign.Value.QualifiedItemId);
			b.Draw(dataOrErrorItem.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 8, tileY.Value * 64 + tilesHigh.Value * 64 - 128 - 32)), dataOrErrorItem.GetSourceRect(), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 2f) / 10000f);
			if (fishType.Value != null)
			{
				ParsedItemData data2 = ItemRegistry.GetData(fishType.Value);
				if (data2 != null)
				{
					Texture2D texture2D = data2.GetTexture();
					Rectangle sourceRect = data2.GetSourceRect();
					float num3 = ((maxOccupants.Value == 1) ? 6f : 0f);
					b.Draw(texture2D, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 8 + 8 - 4, (float)(tileY.Value * 64 + tilesHigh.Value * 64 - 128 - 8 + 4) + num3)), sourceRect, Color.Black * 0.4f * alpha, 0f, Vector2.Zero, 3f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 3f) / 10000f);
					b.Draw(texture2D, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 8 + 8 - 1, (float)(tileY.Value * 64 + tilesHigh.Value * 64 - 128 - 8 + 1) + num3)), sourceRect, color * alpha, 0f, Vector2.Zero, 3f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 4f) / 10000f);
					if (maxOccupants.Value > 1)
					{
						Utility.drawTinyDigits(currentOccupants.Value, b, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 32 + 8 + ((currentOccupants.Value < 10) ? 8 : 0), tileY.Value * 64 + tilesHigh.Value * 64 - 96)), 3f, (((float)tileY.Value + 0.5f) * 64f + 5f) / 10000f, Color.LightYellow * alpha);
					}
				}
			}
		}
		if (_fishObject != null && (_fishObject.QualifiedItemId == "(O)393" || _fishObject.QualifiedItemId == "(O)397"))
		{
			for (int k = 0; k < currentOccupants.Value; k++)
			{
				Vector2 vector = Vector2.Zero;
				int num4 = (k + seedOffset.Value) % 10;
				switch (num4)
				{
				case 0:
					vector = new Vector2(0f, 0f);
					break;
				case 1:
					vector = new Vector2(48f, 32f);
					break;
				case 2:
					vector = new Vector2(80f, 72f);
					break;
				case 3:
					vector = new Vector2(140f, 28f);
					break;
				case 4:
					vector = new Vector2(96f, 0f);
					break;
				case 5:
					vector = new Vector2(0f, 96f);
					break;
				case 6:
					vector = new Vector2(140f, 80f);
					break;
				case 7:
					vector = new Vector2(64f, 120f);
					break;
				case 8:
					vector = new Vector2(140f, 140f);
					break;
				case 9:
					vector = new Vector2(0f, 150f);
					break;
				}
				b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 64 + 7, tileY.Value * 64 + 64 + 32) + vector), Game1.shadowTexture.Bounds, color * alpha, 0f, Vector2.Zero, 3f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f - 2f) / 10000f - 1.1E-05f);
				ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem("(O)" + fishType.Value);
				Texture2D texture2D2 = dataOrErrorItem2.GetTexture();
				Rectangle sourceRect2 = dataOrErrorItem2.GetSourceRect();
				b.Draw(texture2D2, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 64, tileY.Value * 64 + 64) + vector), sourceRect2, color * alpha * 0.75f, 0f, Vector2.Zero, 3f, (num4 % 3 == 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f - 2f) / 10000f - 1E-05f);
			}
		}
		else
		{
			for (int l = 0; l < _fishSilhouettes.Count; l++)
			{
				_fishSilhouettes[l].Draw(b);
			}
		}
		for (int m = 0; m < _jumpingFish.Count; m++)
		{
			_jumpingFish[m].Draw(b);
		}
		if (HasUnresolvedNeeds())
		{
			Vector2 globalPosition = GetRequestTile() * 64f;
			globalPosition += 64f * new Vector2(0.5f, 0.5f);
			float num5 = 3f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			float layerDepth = (globalPosition.Y + 160f) / 10000f + 1E-06f;
			globalPosition.Y += num5 - 32f;
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, globalPosition), new Rectangle(403, 496, 5, 14), Color.White * 0.75f, 0f, new Vector2(2f, 14f), 4f, SpriteEffects.None, layerDepth);
		}
		bool flag2 = goldenAnimalCracker.Value && !isPlayingGoldenCrackerAnimation.Value;
		if (flag2)
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64) + new Vector2(65f, 59f) * 4f), new Rectangle(130, 160, 15, 16), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 2f) / 10000f);
		}
		if (output.Value != null)
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64) + new Vector2(65f, 59f) * 4f), new Rectangle(0, 160, 15, 16), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f);
			if (flag2)
			{
				b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64) + new Vector2(65f, 59f) * 4f), new Rectangle(145, 160, 15, 16), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 3f) / 10000f);
			}
			Vector2 vector2 = GetItemBucketTile() * 64f;
			float y = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			Vector2 vector3 = vector2 + new Vector2(0f, -2f) * 64f + new Vector2(0f, y);
			Vector2 vector4 = new Vector2(40f, 36f);
			float layerDepth2 = (vector2.Y + 64f) / 10000f + 1E-06f;
			float num6 = (vector2.Y + 64f) / 10000f + 1E-05f;
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, vector3), new Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth2);
			ParsedItemData dataOrErrorItem3 = ItemRegistry.GetDataOrErrorItem(output.Value.QualifiedItemId);
			Texture2D texture2D3 = dataOrErrorItem3.GetTexture();
			b.Draw(texture2D3, Game1.GlobalToLocal(Game1.viewport, vector3 + vector4), dataOrErrorItem3.GetSourceRect(), Color.White * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, num6);
			if (output.Value is ColoredObject coloredObject)
			{
				Rectangle sourceRect3 = ItemRegistry.GetDataOrErrorItem(output.Value.QualifiedItemId).GetSourceRect(1);
				b.Draw(texture2D3, Game1.GlobalToLocal(Game1.viewport, vector3 + vector4), sourceRect3, coloredObject.color.Value * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, num6 + 1E-05f);
			}
			if (output.Value.Stack > 1)
			{
				Utility.drawTinyDigits(output.Value.Stack, b, Game1.GlobalToLocal(Game1.viewport, vector3 + vector4 + new Vector2(16f, 12f)), 3f, num6 + 2E-05f, Color.LightYellow * alpha);
			}
		}
	}

	/// <summary>Get whether an item can be placed on the fish pond as a sign.</summary>
	/// <param name="item">The item to check.</param>
	public bool IsValidSignItem(Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (!item.HasContextTag("sign_item"))
		{
			return item.QualifiedItemId == "(BC)34";
		}
		return true;
	}
}
