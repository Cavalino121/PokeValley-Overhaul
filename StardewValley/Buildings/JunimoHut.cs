using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace StardewValley.Buildings;

public class JunimoHut : Building
{
	public int cropHarvestRadius = 8;

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="M:StardewValley.Buildings.JunimoHut.GetOutputChest" /> instead.</summary>
	[XmlElement("output")]
	public Chest obsolete_output;

	[XmlElement("noHarvest")]
	public readonly NetBool noHarvest = new NetBool();

	[XmlElement("wasLit")]
	public readonly NetBool wasLit = new NetBool(value: false);

	private int junimoSendOutTimer;

	[XmlIgnore]
	public List<JunimoHarvester> myJunimos = new List<JunimoHarvester>();

	[XmlIgnore]
	public Point lastKnownCropLocation = Point.Zero;

	public NetInt raisinDays = new NetInt();

	[XmlElement("shouldSendOutJunimos")]
	public NetBool shouldSendOutJunimos = new NetBool(value: false);

	private Rectangle lightInteriorRect = new Rectangle(195, 0, 18, 17);

	private Rectangle bagRect = new Rectangle(208, 51, 15, 13);

	public JunimoHut(Vector2 tileLocation)
		: base("Junimo Hut", tileLocation)
	{
	}

	public JunimoHut()
		: this(Vector2.Zero)
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(noHarvest, "noHarvest").AddField(wasLit, "wasLit").AddField(shouldSendOutJunimos, "shouldSendOutJunimos")
			.AddField(raisinDays, "raisinDays");
		wasLit.fieldChangeVisibleEvent += delegate
		{
			updateLightState();
		};
	}

	public override Rectangle getRectForAnimalDoor(BuildingData data)
	{
		return new Rectangle((1 + tileX.Value) * 64, (tileY.Value + 1) * 64, 64, 64);
	}

	public override Rectangle? getSourceRectForMenu()
	{
		return new Rectangle(Game1.GetSeasonIndexForLocation(GetParentLocation()) * 48, 0, 48, 64);
	}

	public Chest GetOutputChest()
	{
		return GetBuildingChest("Output");
	}

	public override void dayUpdate(int dayOfMonth)
	{
		base.dayUpdate(dayOfMonth);
		myJunimos.Clear();
		wasLit.Value = false;
		shouldSendOutJunimos.Value = true;
		if (raisinDays.Value > 0 && !Game1.IsWinter)
		{
			raisinDays.Value--;
		}
		if (raisinDays.Value == 0 && !Game1.IsWinter)
		{
			Chest outputChest = GetOutputChest();
			if (outputChest.Items.CountId("(O)Raisins") > 0)
			{
				raisinDays.Value += 7;
				outputChest.Items.ReduceId("(O)Raisins", 1);
			}
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.isActive() && allFarmer.currentLocation != null && (allFarmer.currentLocation is FarmHouse || allFarmer.currentLocation.isStructure.Value))
			{
				shouldSendOutJunimos.Value = false;
			}
		}
	}

	public void sendOutJunimos()
	{
		junimoSendOutTimer = 1000;
	}

	/// <inheritdoc />
	public override void performActionOnConstruction(GameLocation location, Farmer who)
	{
		base.performActionOnConstruction(location, who);
		sendOutJunimos();
	}

	public override void resetLocalState()
	{
		base.resetLocalState();
		updateLightState();
	}

	public void updateLightState()
	{
		if (!IsInCurrentLocation())
		{
			return;
		}
		string identifier = $"{"JunimoHut"}_{tileX}_{tileY}";
		if (wasLit.Value)
		{
			if (Utility.getLightSource(identifier) == null)
			{
				Game1.currentLightSources.Add(new LightSource(identifier, 4, new Vector2(tileX.Value + 1, tileY.Value + 1) * 64f + new Vector2(32f, 32f), 0.5f, LightSource.LightContext.None, 0L, parentLocationName.Value));
			}
			AmbientLocationSounds.addSound(new Vector2(tileX.Value + 1, tileY.Value + 1), 1);
		}
		else
		{
			Utility.removeLightSource(identifier);
			AmbientLocationSounds.removeSound(new Vector2(tileX.Value + 1, tileY.Value + 1));
		}
	}

	public int getUnusedJunimoNumber()
	{
		for (int i = 0; i < 3; i++)
		{
			if (i >= myJunimos.Count)
			{
				return i;
			}
			bool flag = false;
			foreach (JunimoHarvester myJunimo in myJunimos)
			{
				if (myJunimo.whichJunimoFromThisHut == i)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return i;
			}
		}
		return 2;
	}

	public override void updateWhenFarmNotCurrentLocation(GameTime time)
	{
		base.updateWhenFarmNotCurrentLocation(time);
		GameLocation parentLocation = GetParentLocation();
		Chest outputChest = GetOutputChest();
		if (outputChest?.mutex != null)
		{
			outputChest.mutex.Update(parentLocation);
			if (outputChest.mutex.IsLockHeld() && Game1.activeClickableMenu == null)
			{
				outputChest.mutex.ReleaseLock();
			}
		}
		if (!Game1.IsMasterGame || junimoSendOutTimer <= 0 || !shouldSendOutJunimos.Value)
		{
			return;
		}
		junimoSendOutTimer -= time.ElapsedGameTime.Milliseconds;
		if (junimoSendOutTimer > 0 || myJunimos.Count >= 3 || parentLocation.IsWinterHere() || parentLocation.IsRainingHere() || !areThereMatureCropsWithinRadius() || (!(parentLocation.NameOrUniqueName != "Farm") && Game1.farmEvent != null))
		{
			return;
		}
		int unusedJunimoNumber = getUnusedJunimoNumber();
		bool isPrismatic = false;
		Color? gemColor = getGemColor(ref isPrismatic);
		JunimoHarvester junimoHarvester = new JunimoHarvester(parentLocation, new Vector2(tileX.Value + 1, tileY.Value + 1) * 64f + new Vector2(0f, 32f), this, unusedJunimoNumber, gemColor);
		junimoHarvester.isPrismatic.Value = isPrismatic;
		parentLocation.characters.Add(junimoHarvester);
		myJunimos.Add(junimoHarvester);
		junimoSendOutTimer = 1000;
		if (Utility.isOnScreen(Utility.Vector2ToPoint(new Vector2(tileX.Value + 1, tileY.Value + 1)), 64, parentLocation))
		{
			try
			{
				parentLocation.playSound("junimoMeep1");
			}
			catch (Exception)
			{
			}
		}
	}

	public override void Update(GameTime time)
	{
		if (!shouldSendOutJunimos.Value)
		{
			shouldSendOutJunimos.Value = true;
		}
		base.Update(time);
	}

	private Color? getGemColor(ref bool isPrismatic)
	{
		List<Color> list = new List<Color>();
		foreach (Item item in GetOutputChest().Items)
		{
			if (item != null && (item.Category == -12 || item.Category == -2))
			{
				Color? dyeColor = TailoringMenu.GetDyeColor(item);
				if (item.QualifiedItemId == "(O)74")
				{
					isPrismatic = true;
				}
				if (dyeColor.HasValue)
				{
					list.Add(dyeColor.Value);
				}
			}
		}
		if (list.Count > 0)
		{
			return list[Game1.random.Next(list.Count)];
		}
		return null;
	}

	public bool areThereMatureCropsWithinRadius()
	{
		GameLocation parentLocation = GetParentLocation();
		for (int i = tileX.Value + 1 - cropHarvestRadius; i < tileX.Value + 2 + cropHarvestRadius; i++)
		{
			for (int j = tileY.Value - cropHarvestRadius + 1; j < tileY.Value + 2 + cropHarvestRadius; j++)
			{
				if (parentLocation.terrainFeatures.TryGetValue(new Vector2(i, j), out var value))
				{
					if (parentLocation.isCropAtTile(i, j) && ((HoeDirt)value).readyForHarvest())
					{
						lastKnownCropLocation = new Point(i, j);
						return true;
					}
					if (value is Bush bush && bush.readyForHarvest())
					{
						lastKnownCropLocation = new Point(i, j);
						return true;
					}
				}
			}
		}
		lastKnownCropLocation = Point.Zero;
		return false;
	}

	public override void performTenMinuteAction(int timeElapsed)
	{
		base.performTenMinuteAction(timeElapsed);
		GameLocation parentLocation = GetParentLocation();
		if (myJunimos.Count > 0)
		{
			for (int num = myJunimos.Count - 1; num >= 0; num--)
			{
				if (!parentLocation.characters.Contains(myJunimos[num]))
				{
					myJunimos.RemoveAt(num);
				}
				else
				{
					myJunimos[num].pokeToHarvest();
				}
			}
		}
		if (myJunimos.Count < 3 && Game1.timeOfDay < 1900)
		{
			junimoSendOutTimer = 1;
		}
		if (Game1.timeOfDay >= 2000 && Game1.timeOfDay < 2400)
		{
			if (!parentLocation.IsWinterHere() && Game1.random.NextDouble() < 0.2)
			{
				wasLit.Value = true;
			}
		}
		else if (Game1.timeOfDay == 2400 && !parentLocation.IsWinterHere())
		{
			wasLit.Value = false;
		}
	}

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (who.ActiveObject != null && who.ActiveObject.IsFloorPathItem() && who.currentLocation != null && !who.currentLocation.terrainFeatures.ContainsKey(tileLocation))
		{
			return false;
		}
		if (occupiesTile(tileLocation))
		{
			Chest output = GetOutputChest();
			if (output.Items.Count > 36)
			{
				output.clearNulls();
			}
			output.mutex.RequestLock(delegate
			{
				Game1.activeClickableMenu = new ItemGrabMenu(output.Items, reverseGrab: false, showReceivingMenu: true, InventoryMenu.highlightAllItems, output.grabItemFromInventory, null, output.grabItemFromChest, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: true, 1, null, 1, this);
			});
			return true;
		}
		return base.doAction(tileLocation, who);
	}

	public override void drawInMenu(SpriteBatch b, int x, int y)
	{
		drawShadow(b, x, y);
		b.Draw(texture.Value, new Vector2(x, y), new Rectangle(0, 0, 48, 64), color, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, 0.89f);
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
		drawShadow(b);
		Rectangle value = getSourceRectForMenu() ?? getSourceRect();
		b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64)), value, color * alpha, 0f, new Vector2(0f, texture.Value.Bounds.Height), 4f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value - 1) * 64) / 10000f);
		if (raisinDays.Value > 0 && !Game1.IsWinter)
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 12, tileY.Value * 64 + tilesHigh.Value * 64 + 20)), new Rectangle(246, 46, 10, 18), color * alpha, 0f, new Vector2(0f, 18f), 4f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value - 1) * 64 + 2) / 10000f);
		}
		bool flag = false;
		Chest outputChest = GetOutputChest();
		if (outputChest != null)
		{
			foreach (Item item in outputChest.Items)
			{
				if (item != null && item.Category != -12 && item.Category != -2)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 128 + 12, tileY.Value * 64 + tilesHigh.Value * 64 - 32)), bagRect, color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value - 1) * 64 + 1) / 10000f);
		}
		if (Game1.timeOfDay >= 2000 && Game1.timeOfDay < 2400 && wasLit.Value && !GetParentLocation().IsWinterHere())
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 64, tileY.Value * 64 + tilesHigh.Value * 64 - 64)), lightInteriorRect, color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)((tileY.Value + tilesHigh.Value - 1) * 64 + 1) / 10000f);
		}
	}
}
