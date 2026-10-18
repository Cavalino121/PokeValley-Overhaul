using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData.Pets;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;

namespace StardewValley.Characters;

public class Pet : NPC
{
	/// <summary>The cat's pet type ID in <c>Data/Pets</c>.</summary>
	public const string type_cat = "Cat";

	/// <summary>The dog's pet type ID in <c>Data/Pets</c>.</summary>
	public const string type_dog = "Dog";

	/// <summary>A unique ID for this pet.</summary>
	/// <remarks>This matches the <see cref="F:StardewValley.Buildings.PetBowl.petId" /> of the pet's bowl, if any. See also <see cref="M:StardewValley.Characters.Pet.GetPetBowl" />.</remarks>
	[XmlElement("guid")]
	public NetGuid petId = new NetGuid(Guid.NewGuid());

	public const int bedTime = 2000;

	public const int maxFriendship = 1000;

	public const string behavior_Walk = "Walk";

	public const string behavior_Sleep = "Sleep";

	public const string behavior_SitDown = "SitDown";

	public const string behavior_Sprint = "Sprint";

	protected int behaviorTimer = -1;

	protected int animationLoopsLeft;

	[XmlElement("petType")]
	public readonly NetString petType = new NetString("Dog");

	[XmlElement("whichBreed")]
	public readonly NetString whichBreed = new NetString("0");

	private readonly NetString netCurrentBehavior = new NetString();

	/// <summary>The unique name of the location containing the pet's bowl, if any.</summary>
	[XmlElement("homeLocationName")]
	public readonly NetString homeLocationName = new NetString();

	[XmlIgnore]
	public readonly NetEvent1Field<long, NetLong> petPushEvent = new NetEvent1Field<long, NetLong>();

	[XmlIgnore]
	protected string _currentBehavior;

	[XmlElement("lastPetDay")]
	public NetLongDictionary<int, NetInt> lastPetDay = new NetLongDictionary<int, NetInt>();

	[XmlElement("grantedFriendshipForPet")]
	public NetBool grantedFriendshipForPet = new NetBool(value: false);

	[XmlElement("friendshipTowardFarmer")]
	public NetInt friendshipTowardFarmer = new NetInt(0);

	[XmlElement("timesPet")]
	public NetInt timesPet = new NetInt(0);

	[XmlElement("hat")]
	public readonly NetRef<Hat> hat = new NetRef<Hat>();

	protected int _walkFromPushTimer;

	public NetBool isSleepingOnFarmerBed = new NetBool(value: false);

	[XmlIgnore]
	public readonly NetMutex mutex = new NetMutex();

	private int pushingTimer;

	/// <inheritdoc />
	[XmlIgnore]
	public override bool IsVillager => false;

	public string CurrentBehavior
	{
		get
		{
			return netCurrentBehavior.Value;
		}
		set
		{
			if (netCurrentBehavior.Value != value)
			{
				netCurrentBehavior.Value = value;
			}
		}
	}

	public override void reloadData()
	{
	}

	protected override string translateName()
	{
		return name.Value.Trim();
	}

	public Pet(int xTile, int yTile, string petBreed, string petType)
	{
		base.Name = petType;
		displayName = name.Value;
		this.petType.Value = petType;
		whichBreed.Value = petBreed;
		Sprite = new AnimatedSprite(getPetTextureName(), 0, 32, 32);
		base.Position = new Vector2(xTile, yTile) * 64f;
		base.Breather = false;
		base.willDestroyObjectsUnderfoot = false;
		base.currentLocation = Game1.currentLocation;
		base.HideShadow = true;
	}

	public Pet()
		: this(0, 0, "0", "Dog")
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(petId, "petId").AddField(petType, "petType").AddField(whichBreed, "whichBreed")
			.AddField(netCurrentBehavior, "netCurrentBehavior")
			.AddField(homeLocationName, "homeLocationName")
			.AddField(petPushEvent, "petPushEvent")
			.AddField(lastPetDay, "lastPetDay")
			.AddField(grantedFriendshipForPet, "grantedFriendshipForPet")
			.AddField(friendshipTowardFarmer, "friendshipTowardFarmer")
			.AddField(isSleepingOnFarmerBed, "isSleepingOnFarmerBed")
			.AddField(mutex.NetFields, "mutex.NetFields")
			.AddField(hat, "hat")
			.AddField(timesPet, "timesPet");
		name.FilterStringEvent += Utility.FilterDirtyWords;
		name.fieldChangeVisibleEvent += delegate
		{
			resetCachedDisplayName();
		};
		petPushEvent.onEvent += OnPetPush;
		friendshipTowardFarmer.fieldChangeVisibleEvent += delegate
		{
			GrantLoveMailIfNecessary();
		};
		isSleepingOnFarmerBed.fieldChangeVisibleEvent += delegate
		{
			UpdateSleepingOnBed();
		};
		petType.fieldChangeVisibleEvent += delegate
		{
			reloadBreedSprite();
		};
		whichBreed.fieldChangeVisibleEvent += delegate
		{
			reloadBreedSprite();
		};
		netCurrentBehavior.fieldChangeVisibleEvent += delegate
		{
			if (_currentBehavior != CurrentBehavior)
			{
				_OnNewBehavior();
			}
		};
	}

	public virtual void OnPetPush(long farmerId)
	{
		pushingTimer = 0;
		if (Game1.IsMasterGame)
		{
			Farmer farmer = Game1.GetPlayer(farmerId) ?? Game1.player;
			Vector2 awayFromPlayerTrajectory = Utility.getAwayFromPlayerTrajectory(GetBoundingBox(), farmer);
			setTrajectory((int)awayFromPlayerTrajectory.X / 2, (int)awayFromPlayerTrajectory.Y / 2);
			_walkFromPushTimer = 250;
			CurrentBehavior = "Walk";
			OnNewBehavior();
			Halt();
			faceDirection(farmer.FacingDirection);
			setMovingInFacingDirection();
		}
	}

	public override int getTimeFarmerMustPushBeforeStartShaking()
	{
		return 300;
	}

	public override int getTimeFarmerMustPushBeforePassingThrough()
	{
		return 750;
	}

	public override void behaviorOnFarmerLocationEntry(GameLocation location, Farmer who)
	{
		base.behaviorOnFarmerLocationEntry(location, who);
		if (location is Farm && Game1.timeOfDay >= 2000 && !location.farmers.Any())
		{
			if (CurrentBehavior != "Sleep" || base.currentLocation is Farm)
			{
				Game1.player.team.requestPetWarpHomeEvent.Fire(Game1.player.UniqueMultiplayerID);
			}
		}
		else if (Game1.timeOfDay < 2000 && Game1.random.NextBool() && _currentBehavior != "Sleep")
		{
			CurrentBehavior = "Sleep";
			_OnNewBehavior();
			Sprite.UpdateSourceRect();
		}
		UpdateSleepingOnBed();
	}

	public override void behaviorOnLocalFarmerLocationEntry(GameLocation location)
	{
		base.behaviorOnLocalFarmerLocationEntry(location);
		netCurrentBehavior.CancelInterpolation();
		if (netCurrentBehavior.Value == "Sleep")
		{
			position.NetFields.CancelInterpolation();
			if (_currentBehavior != "Sleep")
			{
				_OnNewBehavior();
				Sprite.UpdateSourceRect();
			}
		}
		UpdateSleepingOnBed();
	}

	public override bool canTalk()
	{
		return false;
	}

	/// <summary>Get the data from <c>Data/Pets</c> for the pet type, if it's valid.</summary>
	public PetData GetPetData()
	{
		if (!TryGetData(petType.Value, out var data))
		{
			return null;
		}
		return data;
	}

	/// <summary>Get the underlying content data for a pet type, if any.</summary>
	/// <param name="petType">The pet type's ID in <c>Data/Pets</c>.</param>
	/// <param name="data">The pet data, if found.</param>
	/// <returns>Returns whether the pet data was found.</returns>
	public static bool TryGetData(string petType, out PetData data)
	{
		if (petType != null && Game1.petData.TryGetValue(petType, out data))
		{
			return true;
		}
		data = null;
		return false;
	}

	/// <summary>Get the icon to show in the game menu for this pet.</summary>
	/// <param name="assetName">The asset name for the texture.</param>
	/// <param name="sourceRect">The 16x16 pixel area within the texture for the icon.</param>
	public void GetPetIcon(out string assetName, out Rectangle sourceRect)
	{
		PetData petData = GetPetData();
		PetData data;
		PetBreed petBreed = petData?.GetBreedById(whichBreed.Value) ?? petData?.Breeds?.FirstOrDefault() ?? ((!TryGetData("Dog", out data)) ? null : data.Breeds?.FirstOrDefault());
		if (petBreed != null)
		{
			assetName = petBreed.IconTexture;
			sourceRect = petBreed.IconSourceRect;
		}
		else
		{
			assetName = "Animals\\dog";
			sourceRect = new Rectangle(208, 208, 16, 16);
		}
	}

	public virtual string getPetTextureName()
	{
		try
		{
			PetData petData = GetPetData();
			if (petData != null)
			{
				return petData.GetBreedById(whichBreed.Value).Texture;
			}
		}
		catch (Exception)
		{
		}
		return "Animals\\dog";
	}

	public void reloadBreedSprite()
	{
		Sprite?.LoadTexture(getPetTextureName());
	}

	/// <inheritdoc />
	public override void reloadSprite(bool onlyAppearance = false)
	{
		reloadBreedSprite();
		base.HideShadow = true;
		base.Breather = false;
		if (!onlyAppearance)
		{
			base.DefaultPosition = new Vector2(54f, 8f) * 64f;
			setAtFarmPosition();
			if (GetPetBowl() == null)
			{
				warpToFarmHouse(Game1.MasterPlayer);
			}
			GrantLoveMailIfNecessary();
		}
	}

	/// <inheritdoc />
	public override void ChooseAppearance(LocalizedContentManager content = null)
	{
		if (Sprite?.Texture == null)
		{
			reloadSprite(onlyAppearance: true);
		}
	}

	public void warpToFarmHouse(Farmer who)
	{
		PetData petData = GetPetData();
		isSleepingOnFarmerBed.Value = false;
		FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(who);
		int i = 0;
		Vector2 vector = new Vector2(Game1.random.Next(2, homeOfFarmer.map.Layers[0].LayerWidth - 3), Game1.random.Next(3, homeOfFarmer.map.Layers[0].LayerHeight - 5));
		List<Furniture> list = new List<Furniture>();
		foreach (Furniture item in homeOfFarmer.furniture)
		{
			if (item.furniture_type.Value == 12)
			{
				list.Add(item);
			}
		}
		BedFurniture playerBed = homeOfFarmer.GetPlayerBed();
		float num = 0f;
		float num2 = 0.3f;
		float num3 = 0.5f;
		if (petData != null)
		{
			num = petData.SleepOnBedChance;
			num2 = petData.SleepNearBedChance;
			num3 = petData.SleepOnRugChance;
		}
		if (playerBed != null && !Game1.newDay && Game1.timeOfDay >= 2000 && Game1.random.NextDouble() <= (double)num)
		{
			vector = Utility.PointToVector2(playerBed.GetBedSpot()) + new Vector2(-1f, 0f);
			if (homeOfFarmer.isCharacterAtTile(vector) == null)
			{
				Game1.warpCharacter(this, homeOfFarmer, vector);
				base.NetFields.CancelInterpolation();
				CurrentBehavior = "Sleep";
				isSleepingOnFarmerBed.Value = true;
				Rectangle boundingBox = GetBoundingBox();
				foreach (Furniture item2 in homeOfFarmer.furniture)
				{
					if (item2 is BedFurniture bedFurniture && bedFurniture.GetBoundingBox().Intersects(boundingBox))
					{
						bedFurniture.ReserveForNPC();
						break;
					}
				}
				UpdateSleepingOnBed();
				_OnNewBehavior();
				Sprite.UpdateSourceRect();
				return;
			}
		}
		else if (Game1.random.NextDouble() <= (double)num2)
		{
			vector = Utility.PointToVector2(homeOfFarmer.getBedSpot()) + new Vector2(0f, 2f);
		}
		else if (Game1.random.NextDouble() <= (double)num3)
		{
			Furniture furniture = Game1.random.ChooseFrom(list);
			if (furniture != null)
			{
				vector = Utility.getRandomPositionInThisRectangle(furniture.boundingBox.Value, Game1.random) / 64f;
			}
		}
		for (; i < 50; i++)
		{
			if (homeOfFarmer.canPetWarpHere(vector) && homeOfFarmer.CanItemBePlacedHere(vector, itemIsPassable: false, ~CollisionMask.Farmers) && homeOfFarmer.CanItemBePlacedHere(vector + new Vector2(1f, 0f), itemIsPassable: false, ~CollisionMask.Farmers) && !homeOfFarmer.isTileOnWall((int)vector.X, (int)vector.Y))
			{
				break;
			}
			vector = new Vector2(Game1.random.Next(2, homeOfFarmer.map.Layers[0].LayerWidth - 3), Game1.random.Next(3, homeOfFarmer.map.Layers[0].LayerHeight - 4));
		}
		if (i < 50)
		{
			Game1.warpCharacter(this, homeOfFarmer, vector);
			CurrentBehavior = "Sleep";
		}
		else
		{
			WarpToPetBowl();
		}
		UpdateSleepingOnBed();
		_OnNewBehavior();
		Sprite.UpdateSourceRect();
	}

	public virtual void UpdateSleepingOnBed()
	{
		drawOnTop = false;
		collidesWithOtherCharacters.Value = !isSleepingOnFarmerBed.Value;
		farmerPassesThrough = isSleepingOnFarmerBed.Value;
	}

	public override void dayUpdate(int dayOfMonth)
	{
		isSleepingOnFarmerBed.Value = false;
		UpdateSleepingOnBed();
		base.DefaultPosition = new Vector2(54f, 8f) * 64f;
		Sprite.loop = false;
		base.Breather = false;
		if (Game1.IsMasterGame && GetPetBowl() == null)
		{
			foreach (Building building in Game1.getFarm().buildings)
			{
				if (building is PetBowl petBowl && !petBowl.HasPet())
				{
					petBowl.AssignPet(this);
					break;
				}
			}
		}
		PetBowl petBowl2 = GetPetBowl();
		if (Game1.isRaining)
		{
			CurrentBehavior = "SitDown";
			warpToFarmHouse(Game1.player);
		}
		else if (petBowl2 != null && base.currentLocation is FarmHouse)
		{
			setAtFarmPosition();
		}
		else if (petBowl2 == null)
		{
			warpToFarmHouse(Game1.player);
		}
		if (Game1.IsMasterGame)
		{
			if (petBowl2 != null && petBowl2.watered.Value)
			{
				friendshipTowardFarmer.Set(Math.Min(1000, friendshipTowardFarmer.Value + 6));
				petBowl2.watered.Set(newValue: false);
			}
			if (petBowl2 == null)
			{
				friendshipTowardFarmer.Value -= 10;
			}
		}
		if (petBowl2 == null)
		{
			Game1.addMorningFluffFunction(delegate
			{
				doEmote(28);
			});
		}
		Halt();
		CurrentBehavior = "Sleep";
		grantedFriendshipForPet.Set(newValue: false);
		_OnNewBehavior();
		Sprite.UpdateSourceRect();
	}

	public void GrantLoveMailIfNecessary()
	{
		if (friendshipTowardFarmer.Value < 1000)
		{
			return;
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer != null && allFarmer.mailReceived.Add("petLoveMessage") && allFarmer == Game1.player)
			{
				if (Game1.newDay)
				{
					Game1.addMorningFluffFunction(delegate
					{
						Game1.showGlobalMessage(Game1.content.LoadString("Strings\\Characters:PetLovesYou", displayName));
					});
				}
				else
				{
					Game1.showGlobalMessage(Game1.content.LoadString("Strings\\Characters:PetLovesYou", displayName));
				}
			}
			if (!allFarmer.hasOrWillReceiveMail("MarniePetAdoption"))
			{
				Game1.addMailForTomorrow("MarniePetAdoption");
			}
		}
	}

	/// <summary>Get the pet bowl assigned to this pet, if any.</summary>
	public PetBowl GetPetBowl()
	{
		foreach (Building building in (Game1.getLocationFromName(homeLocationName.Value) ?? Game1.getFarm()).buildings)
		{
			if (building is PetBowl petBowl && petBowl.petId.Value == petId.Value)
			{
				return petBowl;
			}
		}
		return null;
	}

	/// <summary>Warp the pet to its assigned pet bowl, if any.</summary>
	public virtual void WarpToPetBowl()
	{
		PetBowl petBowl = GetPetBowl();
		if (petBowl != null)
		{
			faceDirection(2);
			Game1.warpCharacter(this, petBowl.parentLocationName.Value, petBowl.GetPetSpot());
		}
	}

	public void setAtFarmPosition()
	{
		if (Game1.IsMasterGame)
		{
			if (!Game1.isRaining)
			{
				WarpToPetBowl();
			}
			else
			{
				warpToFarmHouse(Game1.MasterPlayer);
			}
		}
	}

	public override bool shouldCollideWithBuildingLayer(GameLocation location)
	{
		return true;
	}

	public override bool canPassThroughActionTiles()
	{
		return false;
	}

	public void unassignPetBowl()
	{
		foreach (Building building in (Game1.getLocationFromName(homeLocationName.Value) ?? Game1.getFarm()).buildings)
		{
			if (building is PetBowl petBowl && petBowl.petId.Value == petId.Value)
			{
				petBowl.petId.Value = Guid.Empty;
			}
		}
	}

	public void applyButterflyPowder(Farmer who, string responseKey)
	{
		if (responseKey.Contains("Yes"))
		{
			GameLocation gameLocation = base.currentLocation;
			unassignPetBowl();
			gameLocation.characters.Remove(this);
			playContentSound();
			Game1.playSound("fireball");
			Rectangle boundingBox = GetBoundingBox();
			boundingBox.Inflate(32, 32);
			boundingBox.X -= 32;
			boundingBox.Y -= 32;
			gameLocation.temporarySprites.AddRange(Utility.sparkleWithinArea(boundingBox, 6, Color.White, 50));
			gameLocation.temporarySprites.Add(new TemporaryAnimatedSprite(5, Utility.PointToVector2(GetBoundingBox().Center) - new Vector2(32f), Color.White, 8, flipped: false, 50f));
			for (int i = 0; i < 8; i++)
			{
				gameLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(372, 1956, 10, 10), base.Position + new Vector2(32f) + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-32, 16)), flipped: false, 0.002f, Color.White)
				{
					alphaFade = 0.0043333336f,
					alpha = 0.75f,
					motion = new Vector2((float)Game1.random.Next(-10, 11) / 20f, -1f),
					acceleration = new Vector2(0f, 0f),
					interval = 99999f,
					layerDepth = 1f,
					scale = 3f,
					scaleChange = 0.01f,
					rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f
				});
			}
			gameLocation.instantiateCrittersList();
			gameLocation.addCritter(new Butterfly(gameLocation, base.Tile + new Vector2(0f, 1f)));
			who.reduceActiveItemByOne();
			if (hat.Value != null)
			{
				Game1.createItemDebris(hat.Value, base.Position, -1, gameLocation);
			}
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:ButterflyPowder_Goodbye", base.Name));
		}
	}

	public override bool checkAction(Farmer who, GameLocation l)
	{
		if (who.Items.Count > who.CurrentToolIndex && who.Items[who.CurrentToolIndex] != null && who.Items[who.CurrentToolIndex] is Hat && (petType.Value == "Cat" || petType.Value == "Dog"))
		{
			if (hat.Value != null)
			{
				Game1.createItemDebris(hat.Value, base.Position, FacingDirection);
				hat.Value = null;
			}
			else
			{
				Hat value = who.Items[who.CurrentToolIndex] as Hat;
				who.Items[who.CurrentToolIndex] = null;
				hat.Value = value;
				Game1.playSound("dirtyHit");
			}
			mutex.ReleaseLock();
		}
		if (who.CurrentItem != null && who.CurrentItem.QualifiedItemId.Equals("(O)ButterflyPowder"))
		{
			l.createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:ButterflyPowder_Question", base.Name), l.createYesNoResponses(), applyButterflyPowder);
		}
		if (!lastPetDay.TryGetValue(who.UniqueMultiplayerID, out var value2) || value2 != Game1.Date.TotalDays)
		{
			lastPetDay[who.UniqueMultiplayerID] = Game1.Date.TotalDays;
			mutex.RequestLock(delegate
			{
				if (!grantedFriendshipForPet.Value)
				{
					grantedFriendshipForPet.Set(newValue: true);
					friendshipTowardFarmer.Set(Math.Min(1000, friendshipTowardFarmer.Value + 12));
					if (Utility.CreateDaySaveRandom(timesPet.Value, 71928.0, petId.Value.GetHashCode()).NextDouble() < (double)GetPetData().GiftChance)
					{
						Item item = TryGetGiftItem(GetPetData().Gifts);
						if (item != null)
						{
							Game1.createMultipleItemDebris(item, base.Position, -1, l, -1, flopFish: true);
						}
					}
					timesPet.Value++;
				}
				mutex.ReleaseLock();
			});
			doEmote(20);
			playContentSound();
			return true;
		}
		return false;
	}

	public virtual void playContentSound()
	{
		if (!Utility.isOnScreen(base.TilePoint, 128, base.currentLocation) || Game1.options.muteAnimalSounds)
		{
			return;
		}
		PetData petData = GetPetData();
		if (petData == null || petData.ContentSound == null)
		{
			return;
		}
		string contentSound = petData.ContentSound;
		PlaySound(contentSound, is_voice: true, -1, -1);
		if (petData.RepeatContentSoundAfter >= 0)
		{
			DelayedAction.functionAfterDelay(delegate
			{
				PlaySound(contentSound, is_voice: true, -1, -1);
			}, petData.RepeatContentSoundAfter);
		}
	}

	public void hold(Farmer who)
	{
		FarmerSprite.AnimationFrame animationFrame = Sprite.CurrentAnimation.Last();
		flip = animationFrame.flip;
		Sprite.CurrentFrame = animationFrame.frame;
		Sprite.CurrentAnimation = null;
		Sprite.loop = false;
	}

	public override void behaviorOnFarmerPushing()
	{
		if (!(CurrentBehavior == "Sprint"))
		{
			pushingTimer += 2;
			if (pushingTimer > 100)
			{
				petPushEvent.Fire(Game1.player.UniqueMultiplayerID);
			}
		}
	}

	public override void update(GameTime time, GameLocation location, long id, bool move)
	{
		base.update(time, location, id, move);
		pushingTimer = Math.Max(0, pushingTimer - 1);
	}

	public override void update(GameTime time, GameLocation location)
	{
		base.update(time, location);
		petPushEvent.Poll();
		if (isSleepingOnFarmerBed.Value && CurrentBehavior != "Sleep" && Game1.IsMasterGame)
		{
			isSleepingOnFarmerBed.Value = false;
			UpdateSleepingOnBed();
		}
		if (base.currentLocation == null)
		{
			base.currentLocation = location;
		}
		mutex.Update(location);
		if (Game1.eventUp)
		{
			return;
		}
		if (_currentBehavior != CurrentBehavior)
		{
			_OnNewBehavior();
		}
		RunState(time);
		if (Game1.IsMasterGame)
		{
			PetBehavior currentPetBehavior = GetCurrentPetBehavior();
			if (currentPetBehavior != null && currentPetBehavior.WalkInDirection)
			{
				if (currentPetBehavior.Animation == null)
				{
					MovePosition(time, Game1.viewport, location);
				}
				else
				{
					tryToMoveInDirection(FacingDirection, isFarmer: false, -1, glider: false);
				}
			}
		}
		flip = false;
		if (FacingDirection == 3 && Sprite.CurrentFrame >= 16)
		{
			flip = true;
		}
	}

	public Item TryGetGiftItem(List<PetGift> gifts)
	{
		float totalWeight = 0f;
		gifts = new List<PetGift>(gifts);
		gifts.RemoveAll(delegate(PetGift gift)
		{
			if (friendshipTowardFarmer.Value >= gift.MinimumFriendshipThreshold && GameStateQuery.CheckConditions(gift.Condition))
			{
				totalWeight += gift.Weight;
				return false;
			}
			return true;
		});
		if (gifts.Count > 0)
		{
			totalWeight = Utility.RandomFloat(0f, totalWeight);
			foreach (PetGift gift in gifts)
			{
				totalWeight -= gift.Weight;
				if (totalWeight <= 0f)
				{
					Item item = ItemQueryResolver.TryResolveRandomItem(gift, null);
					if (item != null && !item.Name.Contains("Error Item"))
					{
						return item;
					}
				}
			}
		}
		return null;
	}

	public bool TryBehaviorChange(List<PetBehaviorChanges> changes)
	{
		float num = 0f;
		foreach (PetBehaviorChanges change in changes)
		{
			if (!change.OutsideOnly || base.currentLocation.IsOutdoors)
			{
				num += change.Weight;
			}
		}
		num = Utility.RandomFloat(0f, num);
		foreach (PetBehaviorChanges change2 in changes)
		{
			if (change2.OutsideOnly && !base.currentLocation.IsOutdoors)
			{
				continue;
			}
			num -= change2.Weight;
			if (num <= 0f)
			{
				string text = null;
				switch (FacingDirection)
				{
				case 0:
					text = change2.UpBehavior;
					break;
				case 2:
					text = change2.DownBehavior;
					break;
				case 3:
					text = change2.LeftBehavior;
					break;
				case 1:
					text = change2.RightBehavior;
					break;
				}
				if (text == null)
				{
					text = change2.Behavior;
				}
				if (text != null)
				{
					CurrentBehavior = text;
				}
				return true;
			}
		}
		return false;
	}

	public PetBehavior GetCurrentPetBehavior()
	{
		PetData petData = GetPetData();
		if (petData?.Behaviors != null)
		{
			foreach (PetBehavior behavior in petData.Behaviors)
			{
				if (behavior.Id == CurrentBehavior)
				{
					return behavior;
				}
			}
		}
		return null;
	}

	public virtual void RunState(GameTime time)
	{
		if (_currentBehavior == "Walk" && Game1.IsMasterGame && _walkFromPushTimer <= 0 && base.currentLocation.isCollidingPosition(nextPosition(FacingDirection), Game1.viewport, this))
		{
			int direction = Game1.random.Next(0, 4);
			if (!base.currentLocation.isCollidingPosition(nextPosition(FacingDirection), Game1.viewport, this))
			{
				faceDirection(direction);
			}
		}
		if (Game1.IsMasterGame && Game1.timeOfDay >= 2000 && Sprite.CurrentAnimation == null && xVelocity == 0f && yVelocity == 0f)
		{
			CurrentBehavior = "Sleep";
		}
		if (CurrentBehavior == "Sleep")
		{
			if (Game1.IsMasterGame && Game1.timeOfDay < 2000 && Game1.random.NextDouble() < 0.001)
			{
				CurrentBehavior = "Walk";
			}
			if (Game1.random.NextDouble() < 0.002)
			{
				doEmote(24);
			}
		}
		if (_walkFromPushTimer > 0)
		{
			_walkFromPushTimer -= (int)time.ElapsedGameTime.TotalMilliseconds;
			if (_walkFromPushTimer <= 0)
			{
				_walkFromPushTimer = 0;
			}
		}
		PetBehavior currentPetBehavior = GetCurrentPetBehavior();
		if (currentPetBehavior == null || !Game1.IsMasterGame)
		{
			return;
		}
		if (behaviorTimer >= 0)
		{
			behaviorTimer -= (int)time.ElapsedGameTime.TotalMilliseconds;
			if (behaviorTimer <= 0)
			{
				behaviorTimer = -1;
				TryBehaviorChange(currentPetBehavior.TimeoutBehaviorChanges);
				return;
			}
		}
		if (_walkFromPushTimer <= 0)
		{
			if (currentPetBehavior.RandomBehaviorChanges != null && currentPetBehavior.RandomBehaviorChangeChance > 0f && Game1.random.NextDouble() < (double)currentPetBehavior.RandomBehaviorChangeChance)
			{
				TryBehaviorChange(currentPetBehavior.RandomBehaviorChanges);
				return;
			}
			if (currentPetBehavior.PlayerNearbyBehaviorChanges != null && withinPlayerThreshold(2))
			{
				TryBehaviorChange(currentPetBehavior.PlayerNearbyBehaviorChanges);
				return;
			}
		}
		if (currentPetBehavior.JumpLandBehaviorChanges != null && yJumpOffset == 0 && yJumpVelocity == 0f)
		{
			TryBehaviorChange(currentPetBehavior.JumpLandBehaviorChanges);
		}
	}

	protected override void updateSlaveAnimation(GameTime time)
	{
		if (Sprite.CurrentAnimation != null)
		{
			Sprite.animateOnce(time);
		}
		else
		{
			if (!(CurrentBehavior == "Walk"))
			{
				return;
			}
			Sprite.faceDirection(FacingDirection);
			if (isMoving())
			{
				animateInFacingDirection(time);
				int num = -1;
				switch (FacingDirection)
				{
				case 0:
					num = 12;
					break;
				case 2:
					num = 4;
					break;
				case 3:
					num = 16;
					break;
				case 1:
					num = 8;
					break;
				}
				if (Sprite.CurrentFrame == num)
				{
					Sprite.CurrentFrame -= 4;
				}
			}
			else
			{
				Sprite.StopAnimation();
			}
		}
	}

	protected void _OnNewBehavior()
	{
		_currentBehavior = CurrentBehavior;
		Halt();
		Sprite.CurrentAnimation = null;
		OnNewBehavior();
	}

	public virtual void OnNewBehavior()
	{
		Sprite.loop = false;
		Sprite.CurrentAnimation = null;
		behaviorTimer = -1;
		animationLoopsLeft = -1;
		if (CurrentBehavior == "Sleep")
		{
			Sprite.loop = true;
			bool flag = Game1.random.NextBool();
			Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
			{
				new FarmerSprite.AnimationFrame(28, 1000, secondaryArm: false, flag),
				new FarmerSprite.AnimationFrame(29, 1000, secondaryArm: false, flag)
			});
		}
		PetBehavior currentPetBehavior = GetCurrentPetBehavior();
		if (currentPetBehavior == null)
		{
			return;
		}
		if (Game1.IsMasterGame)
		{
			if (_walkFromPushTimer <= 0)
			{
				if (Utility.TryParseDirection(currentPetBehavior.Direction, out var parsed))
				{
					FacingDirection = parsed;
				}
				if (currentPetBehavior.RandomizeDirection)
				{
					FacingDirection = (currentPetBehavior.IsSideBehavior ? Game1.random.Choose(3, 1) : Game1.random.Next(4));
				}
			}
			if ((FacingDirection == 0 || FacingDirection == 2) && currentPetBehavior.IsSideBehavior)
			{
				FacingDirection = ((!Game1.random.NextBool()) ? 1 : 3);
			}
			if (currentPetBehavior.WalkInDirection)
			{
				if (currentPetBehavior.MoveSpeed >= 0)
				{
					base.speed = currentPetBehavior.MoveSpeed;
				}
				setMovingInFacingDirection();
			}
			if (currentPetBehavior.Duration >= 0)
			{
				behaviorTimer = currentPetBehavior.Duration;
			}
			else if (currentPetBehavior.MinimumDuration >= 0 && currentPetBehavior.MaximumDuration >= 0)
			{
				behaviorTimer = Game1.random.Next(currentPetBehavior.MinimumDuration, currentPetBehavior.MaximumDuration + 1);
			}
		}
		if (currentPetBehavior.SoundOnStart != null)
		{
			PlaySound(currentPetBehavior.SoundOnStart, currentPetBehavior.SoundIsVoice, currentPetBehavior.SoundRangeFromBorder, currentPetBehavior.SoundRange);
		}
		if (currentPetBehavior.Shake > 0)
		{
			shake(currentPetBehavior.Shake);
		}
		if (currentPetBehavior.Animation == null)
		{
			return;
		}
		Sprite.ClearAnimation();
		for (int i = 0; i < currentPetBehavior.Animation.Count; i++)
		{
			FarmerSprite.AnimationFrame frame = new FarmerSprite.AnimationFrame(currentPetBehavior.Animation[i].Frame, currentPetBehavior.Animation[i].Duration, secondaryArm: false, flip: false);
			if (currentPetBehavior.Animation[i].HitGround)
			{
				frame.AddFrameAction(hitGround);
			}
			if (currentPetBehavior.Animation[i].Jump)
			{
				jump();
			}
			if (currentPetBehavior.AnimationMinimumLoops >= 0 && currentPetBehavior.AnimationMaximumLoops >= 0)
			{
				animationLoopsLeft = Game1.random.Next(currentPetBehavior.AnimationMinimumLoops, currentPetBehavior.AnimationMaximumLoops + 1);
			}
			if (currentPetBehavior.Animation[i].Sound != null)
			{
				frame.AddFrameAction(_PerformAnimationSound);
			}
			if (i == currentPetBehavior.Animation.Count - 1)
			{
				if (animationLoopsLeft > 0 || currentPetBehavior.AnimationEndBehaviorChanges != null)
				{
					frame.AddFrameEndAction(_TryAnimationEndBehaviorChange);
				}
				if (currentPetBehavior.LoopMode == PetAnimationLoopMode.Hold)
				{
					if (currentPetBehavior.AnimationEndBehaviorChanges != null)
					{
						frame.AddFrameEndAction(hold);
					}
					else
					{
						frame.AddFrameAction(hold);
					}
				}
			}
			Sprite.AddFrame(frame);
			if (currentPetBehavior.Animation.Count == 1 && currentPetBehavior.LoopMode == PetAnimationLoopMode.Hold)
			{
				Sprite.AddFrame(frame);
			}
			Sprite.UpdateSourceRect();
		}
		Sprite.loop = currentPetBehavior.LoopMode == PetAnimationLoopMode.Loop || animationLoopsLeft > 0;
	}

	public void _PerformAnimationSound(Farmer who)
	{
		PetBehavior currentPetBehavior = GetCurrentPetBehavior();
		if (currentPetBehavior?.Animation != null && Sprite.currentAnimationIndex >= 0 && Sprite.currentAnimationIndex < currentPetBehavior.Animation.Count)
		{
			PetAnimationFrame petAnimationFrame = currentPetBehavior.Animation[Sprite.currentAnimationIndex];
			if (petAnimationFrame.Sound != null)
			{
				PlaySound(petAnimationFrame.Sound, petAnimationFrame.SoundIsVoice, petAnimationFrame.SoundRangeFromBorder, petAnimationFrame.SoundRange);
			}
		}
	}

	public void PlaySound(string sound, bool is_voice, int range_from_border, int range)
	{
		if ((Game1.options.muteAnimalSounds && is_voice) || !IsSoundInRange(range_from_border, range))
		{
			return;
		}
		float num = 1f;
		PetBreed breedById = GetPetData().GetBreedById(whichBreed.Value);
		if (sound == "BARK")
		{
			sound = GetPetData().BarkSound;
			if (breedById.BarkOverride != null)
			{
				sound = breedById.BarkOverride;
			}
		}
		if (is_voice)
		{
			num = breedById.VoicePitch;
		}
		if (num != 1f)
		{
			playNearbySoundAll(sound, (int)(1200f * num));
		}
		else
		{
			Game1.playSound(sound);
		}
	}

	public bool IsSoundInRange(int range_from_border, int sound_range)
	{
		if (sound_range > 0)
		{
			return withinLocalPlayerThreshold(sound_range);
		}
		if (range_from_border > 0)
		{
			return Utility.isOnScreen(base.TilePoint, range_from_border * 64, base.currentLocation);
		}
		return true;
	}

	public virtual void _TryAnimationEndBehaviorChange(Farmer who)
	{
		if (animationLoopsLeft <= 0)
		{
			if (animationLoopsLeft == 0)
			{
				animationLoopsLeft = -1;
				hold(who);
			}
			PetBehavior currentPetBehavior = GetCurrentPetBehavior();
			if (currentPetBehavior != null && Game1.IsMasterGame)
			{
				TryBehaviorChange(currentPetBehavior.AnimationEndBehaviorChanges);
			}
		}
		else
		{
			animationLoopsLeft--;
		}
	}

	public override Rectangle GetBoundingBox()
	{
		Vector2 vector = base.Position;
		return new Rectangle((int)vector.X + 16, (int)vector.Y + 16, Sprite.SpriteWidth * 4 * 3 / 4, 32);
	}

	public virtual void drawHat(SpriteBatch b, Vector2 shake)
	{
		if (hat.Value == null)
		{
			return;
		}
		Vector2 zero = Vector2.Zero;
		zero *= 4f;
		if (zero.X <= -100f)
		{
			return;
		}
		float num = Math.Max(0f, isSleepingOnFarmerBed.Value ? (((float)base.StandingPixel.Y + 112f) / 10000f) : ((float)base.StandingPixel.Y / 10000f));
		zero.X = -2f;
		zero.Y = -24f;
		num += 1E-07f;
		int num2 = 2;
		bool flag = flip || (sprite.Value.CurrentAnimation != null && sprite.Value.CurrentAnimation[sprite.Value.currentAnimationIndex].flip);
		float scaleSize = 1.3333334f;
		string value = petType.Value;
		if (!(value == "Cat"))
		{
			if (value == "Dog")
			{
				zero.Y -= 20f;
				switch (Sprite.CurrentFrame)
				{
				case 16:
					zero.Y += 20f;
					num2 = 2;
					break;
				case 0:
				case 2:
					zero.Y += 28f;
					num2 = 2;
					break;
				case 1:
				case 3:
					zero.Y += 32f;
					num2 = 2;
					break;
				case 4:
				case 6:
					num2 = 1;
					zero.X += 26f;
					zero.Y += 24f;
					break;
				case 5:
				case 7:
					num2 = 1;
					zero.X += 26f;
					zero.Y += 28f;
					break;
				case 30:
				case 31:
					num2 = ((!flag) ? 1 : 3);
					zero.X += 18f;
					zero.Y += 8f;
					break;
				case 8:
				case 10:
					num2 = 0;
					zero.Y += 4f;
					break;
				case 9:
				case 11:
					num2 = 0;
					zero.Y += 8f;
					break;
				case 12:
				case 14:
					num2 = 3;
					zero.X -= 26f;
					zero.Y += 24f;
					break;
				case 13:
				case 15:
					zero.Y += 24f;
					zero.Y += 4f;
					num2 = 3;
					zero.X -= 26f;
					break;
				case 23:
					num2 = ((!flag) ? 1 : 3);
					zero.X += 18f;
					zero.Y += 8f;
					break;
				case 20:
					num2 = ((!flag) ? 1 : 3);
					zero.X += 26f;
					zero.Y += ((whichBreed.Value == "2") ? 16 : ((whichBreed.Value == "1") ? 24 : 20));
					break;
				case 21:
					num2 = ((!flag) ? 1 : 3);
					zero.X += 22f;
					zero.Y += ((whichBreed.Value == "2") ? 12 : ((whichBreed.Value == "1") ? 20 : 16));
					break;
				case 22:
					num2 = ((!flag) ? 1 : 3);
					zero.X += 18f;
					zero.Y += ((whichBreed.Value == "2") ? 8 : ((whichBreed.Value == "1") ? 8 : 12));
					break;
				case 17:
					zero.Y += 12f;
					break;
				case 18:
				case 19:
					zero.Y += 8f;
					break;
				case 24:
				case 25:
					num2 = ((!flag) ? 1 : 3);
					zero.X += 21 - (flag ? 4 : 4) + 1;
					zero.Y += 8f;
					break;
				case 26:
					num2 = ((!flag) ? 1 : 3);
					zero.X += 18f;
					zero.Y -= 8f;
					break;
				case 27:
					num2 = 2;
					zero.Y += 12 + ((whichBreed.Value == "2") ? (-4) : 0);
					break;
				case 28:
				case 29:
					scaleSize = 1.3333334f;
					zero.Y += 48f;
					zero.X += (flag ? 6 : 5) * 4;
					zero.X += 2f;
					num2 = 2;
					break;
				case 32:
					num2 = ((!flag) ? 1 : 3);
					zero.X += 26f;
					zero.Y += ((whichBreed.Value == "2") ? 12 : 16);
					break;
				case 33:
					num2 = ((!flag) ? 1 : 3);
					zero.X += 26f;
					zero.Y += ((whichBreed.Value == "2") ? 16 : 20);
					break;
				case 34:
					num2 = ((!flag) ? 1 : 3);
					zero.X += 26f;
					zero.Y += ((whichBreed.Value == "2") ? 20 : 24);
					break;
				}
				string value2 = whichBreed.Value;
				if (!(value2 == "2"))
				{
					if (value2 == "3" && num2 == 3 && Sprite.CurrentFrame > 16)
					{
						zero.X += 4f;
					}
				}
				else
				{
					if (num2 == 1)
					{
						zero.X -= 4f;
					}
					zero.Y += 8f;
				}
				if (flag)
				{
					zero.X *= -1f;
				}
			}
		}
		else
		{
			switch (Sprite.CurrentFrame)
			{
			case 16:
				zero.Y += 20f;
				num2 = 2;
				break;
			case 0:
			case 2:
				zero.Y += 28f;
				num2 = 2;
				break;
			case 1:
			case 3:
				zero.Y += 32f;
				num2 = 2;
				break;
			case 4:
			case 6:
				num2 = 1;
				zero.X += 23f;
				zero.Y += 20f;
				break;
			case 5:
			case 7:
				zero.Y += 4f;
				num2 = 1;
				zero.X += 23f;
				zero.Y += 20f;
				break;
			case 30:
			case 31:
				num2 = ((!flag) ? 1 : 3);
				zero.X += ((!flag) ? 1 : (-1)) * 25;
				zero.Y += 32f;
				break;
			case 8:
			case 10:
				num2 = 0;
				zero.Y -= 4f;
				break;
			case 9:
			case 11:
				num2 = 0;
				break;
			case 12:
			case 14:
				num2 = 3;
				zero.X -= 22f;
				zero.Y += 20f;
				break;
			case 13:
			case 15:
				zero.Y += 20f;
				zero.Y += 4f;
				num2 = 3;
				zero.X -= 22f;
				break;
			case 21:
			case 23:
				zero.Y += 16f;
				break;
			case 17:
			case 20:
			case 22:
				zero.Y += 12f;
				break;
			case 18:
			case 19:
				zero.Y += 8f;
				break;
			case 24:
				num2 = ((!flag) ? 1 : 3);
				zero.X += ((!flag) ? 1 : (-1)) * 29;
				zero.Y += 28f;
				break;
			case 25:
				num2 = ((!flag) ? 1 : 3);
				zero.X += ((!flag) ? 1 : (-1)) * 29;
				zero.Y += 36f;
				break;
			case 26:
				num2 = ((!flag) ? 1 : 3);
				zero.X += ((!flag) ? 1 : (-1)) * 29;
				zero.Y += 40f;
				break;
			case 27:
				num2 = ((!flag) ? 1 : 3);
				zero.X += ((!flag) ? 1 : (-1)) * 29;
				zero.Y += 44f;
				break;
			case 28:
			case 29:
				scaleSize = 1.2f;
				zero.Y += 46f;
				zero.X -= ((!flag) ? (-1) : 0) * 4;
				zero.X += ((!flag) ? 1 : (-1)) * 2;
				num2 = (flag ? 1 : 3);
				break;
			}
			if ((whichBreed.Value == "3" || whichBreed.Value == "4") && num2 == 3)
			{
				zero.X -= 4f;
			}
		}
		zero += shake;
		if (flag)
		{
			zero.X -= 4f;
		}
		hat.Value.draw(b, getLocalPosition(Game1.viewport) + zero + new Vector2(30f, -42f), scaleSize, 1f, num, num2, useAnimalTexture: true);
	}

	public override void draw(SpriteBatch b)
	{
		int y = base.StandingPixel.Y;
		Vector2 vector = ((shakeTimer > 0 && !isSleepingOnFarmerBed.Value) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero);
		b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(Sprite.SpriteWidth * 4 / 2, GetBoundingBox().Height / 2) + vector, Sprite.SourceRect, Color.White, rotation, new Vector2(Sprite.SpriteWidth / 2, (float)Sprite.SpriteHeight * 3f / 4f), Math.Max(0.2f, scale.Value) * 4f, (flip || (Sprite.CurrentAnimation != null && Sprite.CurrentAnimation[Sprite.currentAnimationIndex].flip)) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, isSleepingOnFarmerBed.Value ? (((float)y + 112f) / 10000f) : ((float)y / 10000f)));
		drawHat(b, vector);
		if (base.IsEmoting)
		{
			Vector2 localPosition = getLocalPosition(Game1.viewport);
			Point point = GetPetData()?.EmoteOffset ?? Point.Zero;
			b.Draw(position: new Vector2(localPosition.X + 32f + (float)point.X, localPosition.Y - 96f + (float)point.Y), texture: Game1.emoteSpriteSheet, sourceRectangle: new Rectangle(base.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, base.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), color: Color.White, rotation: 0f, origin: Vector2.Zero, scale: 4f, effects: SpriteEffects.None, layerDepth: (float)y / 10000f + 0.0001f);
		}
	}

	public virtual bool withinLocalPlayerThreshold(int threshold)
	{
		if (base.currentLocation != Game1.currentLocation)
		{
			return false;
		}
		Vector2 tile = base.Tile;
		Vector2 tile2 = Game1.player.Tile;
		if (Math.Abs(tile.X - tile2.X) <= (float)threshold)
		{
			return Math.Abs(tile.Y - tile2.Y) <= (float)threshold;
		}
		return false;
	}

	public override bool withinPlayerThreshold(int threshold)
	{
		if (base.currentLocation != null && !base.currentLocation.farmers.Any())
		{
			return false;
		}
		Vector2 tile = base.Tile;
		foreach (Farmer farmer in base.currentLocation.farmers)
		{
			Vector2 tile2 = farmer.Tile;
			if (Math.Abs(tile.X - tile2.X) <= (float)threshold && Math.Abs(tile.Y - tile2.Y) <= (float)threshold)
			{
				return true;
			}
		}
		return false;
	}

	public void hitGround(Farmer who)
	{
		if (Utility.isOnScreen(base.TilePoint, 128, base.currentLocation))
		{
			base.currentLocation.playTerrainSound(base.Tile, this, showTerrainDisturbAnimation: false);
		}
	}
}
