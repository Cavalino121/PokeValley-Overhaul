using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Delegates;
using StardewValley.GameData;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;

namespace StardewValley.Objects;

/// <summary>A mannequin which can be placed in the world and used to store and display clothing.</summary>
public class Mannequin : Object
{
	protected string _description;

	protected MannequinData _data;

	public string displayNameOverride;

	public readonly NetMutex changeMutex = new NetMutex();

	public readonly NetRef<Hat> hat = new NetRef<Hat>();

	public readonly NetRef<Clothing> shirt = new NetRef<Clothing>();

	public readonly NetRef<Clothing> pants = new NetRef<Clothing>();

	public readonly NetRef<Boots> boots = new NetRef<Boots>();

	public readonly NetDirection facing = new NetDirection();

	public readonly NetBool swappedWithFarmerTonight = new NetBool();

	private Farmer renderCache;

	internal int eyeTimer;

	public override string TypeDefinitionId { get; } = "(M)";

	public Mannequin()
	{
	}

	public Mannequin(string itemId)
		: this()
	{
		base.ItemId = itemId;
		base.name = itemId;
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(itemId);
		base.ParentSheetIndex = dataOrErrorItem.SpriteIndex;
		bigCraftable.Value = true;
		canBeSetDown.Value = true;
		setIndoors.Value = true;
		setOutdoors.Value = true;
		base.Type = "interactive";
		facing.Value = 2;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(changeMutex.NetFields, "changeMutex.NetFields").AddField(hat, "hat").AddField(shirt, "shirt")
			.AddField(pants, "pants")
			.AddField(boots, "boots")
			.AddField(facing, "facing")
			.AddField(swappedWithFarmerTonight, "swappedWithFarmerTonight");
		hat.fieldChangeVisibleEvent += OnMannequinUpdated;
		shirt.fieldChangeVisibleEvent += OnMannequinUpdated;
		pants.fieldChangeVisibleEvent += OnMannequinUpdated;
		boots.fieldChangeVisibleEvent += OnMannequinUpdated;
	}

	private void OnMannequinUpdated<TNetField, TValue>(TNetField field, TValue oldValue, TValue newValue)
	{
		renderCache = null;
	}

	protected internal MannequinData GetMannequinData()
	{
		if (_data == null)
		{
			_data = DataLoader.Mannequins(Game1.content).GetValueOrDefault(base.ItemId);
		}
		return _data;
	}

	protected override string loadDisplayName()
	{
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.ItemId);
		if (displayNameOverride == null)
		{
			return dataOrErrorItem.DisplayName;
		}
		return displayNameOverride;
	}

	public override string getDescription()
	{
		if (_description == null)
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(base.ItemId);
			_description = Game1.parseText(TokenParser.ParseText(dataOrErrorItem.Description), Game1.smallFont, getDescriptionWidth());
		}
		return _description;
	}

	public override bool isPlaceable()
	{
		return true;
	}

	/// <inheritdoc />
	public override bool ForEachItem(ForEachItemDelegate handler, GetForEachItemPathDelegate getPath)
	{
		if (base.ForEachItem(handler, getPath) && ForEachItemHelper.ApplyToField(hat, handler, getPath) && ForEachItemHelper.ApplyToField(shirt, handler, getPath) && ForEachItemHelper.ApplyToField(pants, handler, getPath))
		{
			return ForEachItemHelper.ApplyToField(boots, handler, getPath);
		}
		return false;
	}

	public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
	{
		Vector2 key = new Vector2(x / 64, y / 64);
		Mannequin value = getOne() as Mannequin;
		location.Objects.Add(key, value);
		location.playSound("woodyStep");
		return true;
	}

	private void emitGhost()
	{
		Location.temporarySprites.Add(new TemporaryAnimatedSprite(GetMannequinData().Texture, new Rectangle((!(Game1.random.NextDouble() < 0.5)) ? 64 : 0, 64, 16, 32), TileLocation * 64f + new Vector2(0f, -1f) * 64f, flipped: false, 0.004f, Color.White)
		{
			scale = 4f,
			layerDepth = 1f,
			motion = new Vector2(7 + Game1.random.Next(-1, 6), -8 + Game1.random.Next(-1, 5)),
			acceleration = new Vector2(-0.4f + (float)Game1.random.Next(10) / 100f, 0f),
			animationLength = 4,
			totalNumberOfLoops = 99,
			interval = 80f,
			scaleChangeChange = 0.01f
		});
		Location.playSound("cursed_mannequin");
	}

	/// <inheritdoc />
	public override bool minutesElapsed(int minutes)
	{
		if (Game1.random.NextDouble() < 0.001 && GetMannequinData().Cursed)
		{
			if (Game1.timeOfDay > Game1.getTrulyDarkTime(Location) && Game1.random.NextDouble() < 0.1)
			{
				emitGhost();
			}
			else if (Game1.random.NextDouble() < 0.66)
			{
				if (Game1.random.NextDouble() < 0.5)
				{
					foreach (Farmer farmer in Location.farmers)
					{
						facing.Value = Utility.GetOppositeFacingDirection(Utility.getDirectionFromChange(TileLocation, farmer.Tile));
						renderCache = null;
					}
				}
				else
				{
					eyeTimer = 2500;
				}
			}
			else
			{
				Location.playSound("cursed_mannequin");
				shakeTimer = Game1.random.Next(500, 4000);
			}
		}
		return base.minutesElapsed(minutes);
	}

	public override void actionOnPlayerEntry()
	{
		if (Game1.random.NextDouble() < 0.001 && GetMannequinData().Cursed)
		{
			shakeTimer = Game1.random.Next(500, 1000);
		}
		base.actionOnPlayerEntry();
	}

	public override void DayUpdate()
	{
		base.DayUpdate();
		if (Game1.IsMasterGame && GetMannequinData().Cursed && Location != null && (Location is FarmHouse || Location is IslandFarmHouse || Location is Shed))
		{
			if (Game1.random.NextDouble() < 0.05)
			{
				Vector2 oldTile = TileLocation;
				Utility.spawnObjectAround(TileLocation, this, Location, playSound: false, delegate
				{
					if (!TileLocation.Equals(oldTile))
					{
						Location.objects.Remove(oldTile);
					}
				});
			}
			else if (swappedWithFarmerTonight.Value)
			{
				swappedWithFarmerTonight.Value = false;
			}
			else
			{
				if (Game1.random.NextDouble() < 0.005)
				{
					if (Location.farmers.Count <= 0)
					{
						return;
					}
					using FarmerCollection.Enumerator enumerator = Location.farmers.GetEnumerator();
					if (enumerator.MoveNext())
					{
						Farmer current = enumerator.Current;
						Vector2 oldTile2 = TileLocation;
						Vector2 vector = current.mostRecentBed / 64f;
						vector.X = (int)vector.X;
						vector.Y = (int)vector.Y;
						if (Utility.spawnObjectAround(vector, this, Location, playSound: false, delegate
						{
							if (!TileLocation.Equals(oldTile2))
							{
								Location.objects.Remove(oldTile2);
							}
						}))
						{
							facing.Value = Utility.GetOppositeFacingDirection(Utility.getDirectionFromChange(TileLocation, current.Tile));
							renderCache = null;
							eyeTimer = 2000;
						}
					}
					return;
				}
				if (Game1.random.NextDouble() < 0.001)
				{
					DecoratableLocation decoratableLocation = Location as DecoratableLocation;
					string floorID = decoratableLocation.GetFloorID((int)TileLocation.X, (int)TileLocation.Y);
					string text = null;
					for (int num = (int)TileLocation.Y; num > 0; num--)
					{
						text = decoratableLocation.GetWallpaperID((int)TileLocation.X, num);
						if (text != null)
						{
							break;
						}
					}
					if (floorID != null)
					{
						decoratableLocation.SetFloor("MoreFloors:6", floorID);
					}
					if (text != null)
					{
						decoratableLocation.SetWallpaper("MoreWalls:21", text);
					}
					shakeTimer = 10000;
				}
				else
				{
					if (!(Game1.random.NextDouble() < 0.02))
					{
						return;
					}
					DecoratableLocation decoratableLocation2 = Location as DecoratableLocation;
					if (Game1.random.NextDouble() < 0.33)
					{
						for (int num2 = 0; num2 < 30; num2++)
						{
							int num3 = Game1.random.Next(2, Location.Map.Layers[0].LayerWidth - 2);
							for (int num4 = 1; num4 < Location.Map.Layers[0].LayerHeight; num4++)
							{
								Vector2 vector2 = new Vector2(num3, num4);
								if (Location.isTileLocationOpen(vector2) && Location.isTilePlaceable(vector2) && !decoratableLocation2.isTileOnWall(num3, num4) && !Location.IsTileOccupiedBy(vector2))
								{
									facing.Value = 2;
									renderCache = null;
									Location.objects.Remove(TileLocation);
									TileLocation = vector2;
									Location.objects.Add(TileLocation, this);
									return;
								}
							}
						}
						return;
					}
					int num5;
					int num6;
					int num7;
					if (Game1.random.NextDouble() < 0.5)
					{
						num5 = 1;
						num6 = Location.Map.Layers[0].LayerWidth - 1;
						num7 = 1;
					}
					else
					{
						num5 = Location.Map.Layers[0].LayerWidth - 1;
						num6 = 1;
						num7 = -1;
					}
					for (int num8 = 0; num8 < 30; num8++)
					{
						int num9 = Game1.random.Next(2, Location.Map.Layers[0].LayerHeight - 2);
						for (int num10 = num5; num10 != num6; num10 += num7)
						{
							Vector2 vector3 = new Vector2(num10, num9);
							if (Location.isTileLocationOpen(vector3) && Location.isTilePlaceable(vector3) && !decoratableLocation2.isTileOnWall(num10, num9) && !Location.IsTileOccupiedBy(vector3))
							{
								facing.Value = ((num7 == 1) ? 1 : 3);
								renderCache = null;
								Location.objects.Remove(TileLocation);
								TileLocation = vector3;
								Location.objects.Add(TileLocation, this);
								return;
							}
						}
					}
				}
			}
		}
		else if (Game1.IsMasterGame && Location is SeedShop && TileLocation.X > 33f && TileLocation.Y > 14f)
		{
			if (base.ItemId.Equals("CursedMannequinMale"))
			{
				base.ItemId = "MannequinMale";
			}
			else if (base.ItemId.Equals("CursedMannequinFemale"))
			{
				base.ItemId = "MannequinFemale";
			}
			ResetParentSheetIndex();
			renderCache = null;
			_data = null;
		}
	}

	public override void updateWhenCurrentLocation(GameTime time)
	{
		base.updateWhenCurrentLocation(time);
		changeMutex.Update(Location);
		if (eyeTimer > 0)
		{
			eyeTimer -= (int)time.ElapsedGameTime.TotalMilliseconds;
		}
	}

	public override bool performToolAction(Tool t)
	{
		if (t == null)
		{
			return false;
		}
		if (!(t is MeleeWeapon) && t.isHeavyHitter())
		{
			if (hat.Value != null || shirt.Value != null || pants.Value != null || boots.Value != null)
			{
				if (hat.Value != null)
				{
					DropItem(Utility.PerformSpecialItemGrabReplacement(hat.Value));
					hat.Value = null;
				}
				else if (shirt.Value != null)
				{
					DropItem(Utility.PerformSpecialItemGrabReplacement(shirt.Value));
					shirt.Value = null;
				}
				else if (pants.Value != null)
				{
					DropItem(Utility.PerformSpecialItemGrabReplacement(pants.Value));
					pants.Value = null;
				}
				else if (boots.Value != null)
				{
					DropItem(Utility.PerformSpecialItemGrabReplacement(boots.Value));
					boots.Value = null;
				}
				Location.playSound("hammer");
				shakeTimer = 100;
				return false;
			}
			Location.objects.Remove(TileLocation);
			Location.playSound("hammer");
			DropItem(new Mannequin(base.ItemId));
			return false;
		}
		return false;
	}

	public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
	{
		if (who.CurrentItem is Hat || who.CurrentItem is Clothing || who.CurrentItem is Boots)
		{
			return false;
		}
		if (justCheckingForActivity)
		{
			return true;
		}
		if (hat.Value == null && shirt.Value == null && pants.Value == null && boots.Value == null)
		{
			facing.Value = (facing.Value + 1) % 4;
			renderCache = null;
			Game1.playSound("shwip");
		}
		else
		{
			changeMutex.RequestLock(delegate
			{
				hat.Value = who.Equip(hat.Value, who.hat);
				shirt.Value = who.Equip(shirt.Value, who.shirtItem);
				pants.Value = who.Equip(pants.Value, who.pantsItem);
				boots.Value = who.Equip(boots.Value, who.boots);
				changeMutex.ReleaseLock();
			});
			Game1.playSound("coin");
		}
		if (GetMannequinData().Cursed && Game1.random.NextDouble() < 0.001)
		{
			emitGhost();
		}
		return true;
	}

	/// <inheritdoc />
	public override bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false)
	{
		if (!(dropInItem is Hat hat))
		{
			if (!(dropInItem is Clothing clothing))
			{
				if (!(dropInItem is Boots boots))
				{
					return false;
				}
				if (!probe)
				{
					DropItem(this.boots.Value);
					this.boots.Value = (Boots)boots.getOne();
				}
			}
			else if (!probe)
			{
				if (clothing.clothesType.Value == Clothing.ClothesType.SHIRT)
				{
					DropItem(shirt.Value);
					shirt.Value = (Clothing)clothing.getOne();
				}
				else
				{
					DropItem(pants.Value);
					pants.Value = (Clothing)clothing.getOne();
				}
			}
		}
		else if (!probe)
		{
			DropItem(this.hat.Value);
			this.hat.Value = (Hat)hat.getOne();
		}
		if (!probe)
		{
			Game1.playSound("dirtyHit");
		}
		return true;
	}

	public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		base.draw(spriteBatch, x, y, alpha);
		if (eyeTimer > 0 && facing.Value != 0)
		{
			float layerDepth = Math.Max(0f, (float)((y + 1) * 64 - 24) / 10000f) + (float)x * 1.1E-05f;
			Vector2 position = Game1.GlobalToLocal(new Vector2(x, y) * 64f + new Vector2(20f, -40f));
			switch (facing.Value)
			{
			case 1:
				position.X += 12f;
				break;
			case 3:
				position.X += 4f;
				break;
			}
			if (facing.Value != 2)
			{
				position.Y -= 4f;
			}
			spriteBatch.Draw(Game1.mouseCursors_1_6, position, new Rectangle(377 + 5 * (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1620.0 / 60.0), 330, 5 + ((facing.Value != 2) ? (-3) : 0), 3), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth);
		}
		float num = Math.Max(0f, (float)((y + 1) * 64 - 24) / 10000f) + (float)x * 1E-05f;
		Farmer farmerForRendering = GetFarmerForRendering();
		farmerForRendering.position.Value = new Vector2(x * 64, y * 64 - 4 + (GetMannequinData().DisplaysClothingAsMale ? 20 : 16));
		if (shakeTimer > 0)
		{
			farmerForRendering.position.Value += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
		}
		farmerForRendering.FarmerRenderer.draw(spriteBatch, farmerForRendering.FarmerSprite, farmerForRendering.FarmerSprite.SourceRect, farmerForRendering.getLocalPosition(Game1.viewport), new Vector2(0f, farmerForRendering.GetBoundingBox().Height), num + 0.0001f, Color.White, 0f, farmerForRendering);
		FarmerRenderer.FarmerSpriteLayers layer = FarmerRenderer.FarmerSpriteLayers.Arms;
		if (farmerForRendering.facingDirection.Value == 0)
		{
			layer = FarmerRenderer.FarmerSpriteLayers.ArmsUp;
		}
		if (farmerForRendering.FarmerSprite.CurrentAnimationFrame.armOffset > 0)
		{
			Rectangle sourceRect = farmerForRendering.FarmerSprite.SourceRect;
			sourceRect.Offset(-288 + farmerForRendering.FarmerSprite.CurrentAnimationFrame.armOffset * 16, 0);
			spriteBatch.Draw(farmerForRendering.FarmerRenderer.baseTexture, farmerForRendering.getLocalPosition(Game1.viewport) + new Vector2(0f, farmerForRendering.GetBoundingBox().Height) + farmerForRendering.FarmerRenderer.positionOffset + farmerForRendering.armOffset, sourceRect, Color.White, 0f, new Vector2(0f, farmerForRendering.GetBoundingBox().Height), 4f * scale, farmerForRendering.FarmerSprite.CurrentAnimationFrame.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, FarmerRenderer.GetLayerDepth(num + 0.0001f, layer));
		}
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Mannequin(base.ItemId);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is Mannequin mannequin)
		{
			hat.Value = mannequin.hat.Value?.getOne() as Hat;
			shirt.Value = mannequin.shirt.Value?.getOne() as Clothing;
			pants.Value = mannequin.pants.Value?.getOne() as Clothing;
			boots.Value = mannequin.boots.Value?.getOne() as Boots;
		}
	}

	private void DropItem(Item item)
	{
		if (item != null)
		{
			Vector2 debrisOrigin = new Vector2((TileLocation.X + 0.5f) * 64f, (TileLocation.Y + 0.5f) * 64f);
			Location.debris.Add(new Debris(item, debrisOrigin));
		}
	}

	private Farmer GetFarmerForRendering()
	{
		renderCache = renderCache ?? CreateInstance();
		return renderCache;
		Farmer CreateInstance()
		{
			MannequinData mannequinData = GetMannequinData();
			Farmer farmer = new Farmer();
			farmer.changeGender(mannequinData.DisplaysClothingAsMale);
			farmer.faceDirection(facing.Value);
			farmer.changeHairColor(Color.Transparent);
			farmer.skin.Set(farmer.FarmerRenderer.recolorSkin(-12345));
			farmer.hat.Value = hat.Value;
			farmer.shirtItem.Value = shirt.Value;
			if (shirt.Value != null)
			{
				farmer.changeShirt("-1");
			}
			farmer.pantsItem.Value = pants.Value;
			if (pants.Value != null)
			{
				farmer.changePantStyle("-1");
			}
			farmer.boots.Value = boots.Value;
			if (boots.Value != null)
			{
				farmer.changeShoeColor(boots.Value.GetBootsColorString());
			}
			farmer.FarmerRenderer.textureName.Value = mannequinData.FarmerTexture;
			farmer.FarmerSprite.PauseForSingleAnimation = true;
			farmer.currentEyes = 0;
			return farmer;
		}
	}
}
