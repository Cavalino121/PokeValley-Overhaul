using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using xTile.Layers;

namespace StardewValley.Events;

public class SoundInTheNightEvent : BaseFarmEvent
{
	public const int cropCircle = 0;

	public const int meteorite = 1;

	public const int dogs = 2;

	public const int owl = 3;

	public const int earthquake = 4;

	public const int raccoonStump = 5;

	private readonly NetInt behavior = new NetInt();

	private float timer;

	private float timeUntilText = 7000f;

	private string soundName;

	private string message;

	private bool playedSound;

	private bool showedMessage;

	private bool finished;

	private Vector2 targetLocation;

	private Building targetBuilding;

	public SoundInTheNightEvent()
		: this(0)
	{
	}

	public SoundInTheNightEvent(int which)
	{
		behavior.Value = which;
	}

	/// <inheritdoc />
	public override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(behavior, "behavior");
	}

	/// <inheritdoc />
	public override bool setUp()
	{
		Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed);
		Farm farm = Game1.getFarm();
		farm.updateMap();
		timer = 0f;
		switch (behavior.Value)
		{
		case 5:
			soundName = "windstorm";
			message = Game1.content.LoadString("Strings\\1_6_Strings:windstorm");
			timeUntilText = 14000f;
			Game1.player.mailReceived.Add("raccoonTreeFallen");
			break;
		case 0:
		{
			soundName = "UFO";
			message = Game1.content.LoadString("Strings\\Events:SoundInTheNight_UFO");
			int num2 = 50;
			Layer layer2 = farm.map.RequireLayer("Back");
			while (num2 > 0)
			{
				targetLocation = new Vector2(random.Next(5, layer2.LayerWidth - 4), random.Next(5, layer2.LayerHeight - 4));
				if (farm.CanItemBePlacedHere(targetLocation))
				{
					break;
				}
				num2--;
			}
			if (num2 <= 0)
			{
				return true;
			}
			break;
		}
		case 1:
		{
			soundName = "Meteorite";
			message = Game1.content.LoadString("Strings\\Events:SoundInTheNight_Meteorite");
			Layer layer3 = farm.map.RequireLayer("Back");
			targetLocation = new Vector2(random.Next(5, layer3.LayerWidth - 20), random.Next(5, layer3.LayerHeight - 4));
			for (int i = (int)targetLocation.X; (float)i <= targetLocation.X + 1f; i++)
			{
				for (int j = (int)targetLocation.Y; (float)j <= targetLocation.Y + 1f; j++)
				{
					Vector2 tile = new Vector2(i, j);
					if (!farm.isTileOpenBesidesTerrainFeatures(tile) || !farm.isTileOpenBesidesTerrainFeatures(new Vector2(tile.X + 1f, tile.Y)) || !farm.isTileOpenBesidesTerrainFeatures(new Vector2(tile.X + 1f, tile.Y - 1f)) || !farm.isTileOpenBesidesTerrainFeatures(new Vector2(tile.X, tile.Y - 1f)) || farm.isWaterTile((int)tile.X, (int)tile.Y) || farm.isWaterTile((int)tile.X + 1, (int)tile.Y))
					{
						return true;
					}
				}
			}
			break;
		}
		case 2:
			soundName = "dogs";
			if (random.NextBool())
			{
				return true;
			}
			foreach (Building building in farm.buildings)
			{
				if (building.GetIndoors() is AnimalHouse animalHouse && !building.animalDoorOpen.Value && animalHouse.animalsThatLiveHere.Count > animalHouse.animals.Length && random.NextDouble() < (double)(1f / (float)farm.buildings.Count))
				{
					targetBuilding = building;
					break;
				}
			}
			if (targetBuilding == null)
			{
				return true;
			}
			return false;
		case 3:
		{
			soundName = "owl";
			int num = 50;
			Layer layer = farm.map.RequireLayer("Back");
			while (num > 0)
			{
				targetLocation = new Vector2(random.Next(5, layer.LayerWidth - 4), random.Next(5, layer.LayerHeight - 4));
				if (farm.CanItemBePlacedHere(targetLocation))
				{
					break;
				}
				num--;
			}
			if (num <= 0)
			{
				return true;
			}
			break;
		}
		case 4:
			soundName = "thunder_small";
			message = Game1.content.LoadString("Strings\\Events:SoundInTheNight_Earthquake");
			break;
		}
		Game1.freezeControls = true;
		return false;
	}

	/// <inheritdoc />
	public override bool tickUpdate(GameTime time)
	{
		timer += (float)time.ElapsedGameTime.TotalMilliseconds;
		if (timer > 1500f && !playedSound)
		{
			if (!string.IsNullOrEmpty(soundName))
			{
				Game1.playSound(soundName);
				playedSound = true;
			}
			if (!playedSound && message != null)
			{
				Game1.drawObjectDialogue(message);
				Game1.globalFadeToClear();
				showedMessage = true;
				if (message == null)
				{
					finished = true;
				}
				else
				{
					Game1.afterDialogues = delegate
					{
						finished = true;
					};
				}
			}
		}
		if (timer > timeUntilText && !showedMessage)
		{
			Game1.pauseThenMessage(10, message);
			showedMessage = true;
			if (message == null)
			{
				finished = true;
			}
			else
			{
				Game1.afterDialogues = delegate
				{
					finished = true;
				};
			}
		}
		if (finished)
		{
			Game1.freezeControls = false;
			return true;
		}
		return false;
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height), Color.Black);
		if (!showedMessage)
		{
			b.Draw(Game1.mouseCursors_1_6, new Vector2(12f, Game1.viewport.Height - 12 - 76), new Rectangle(256 + (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 600.0 / 100.0) * 19, 413, 19, 19), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		}
	}

	/// <inheritdoc />
	public override void makeChangesToLocation()
	{
		if (!Game1.IsMasterGame)
		{
			return;
		}
		Farm farm = Game1.getFarm();
		switch (behavior.Value)
		{
		case 0:
		{
			Object obj = ItemRegistry.Create<Object>("(BC)96");
			obj.MinutesUntilReady = 24000 - Game1.timeOfDay;
			farm.objects.Add(targetLocation, obj);
			break;
		}
		case 1:
			farm.terrainFeatures.Remove(targetLocation);
			farm.terrainFeatures.Remove(targetLocation + new Vector2(1f, 0f));
			farm.terrainFeatures.Remove(targetLocation + new Vector2(1f, 1f));
			farm.terrainFeatures.Remove(targetLocation + new Vector2(0f, 1f));
			farm.resourceClumps.Add(new ResourceClump(622, 2, 2, targetLocation));
			break;
		case 2:
		{
			AnimalHouse animalHouse = (AnimalHouse)targetBuilding.GetIndoors();
			long num = 0L;
			foreach (long item in animalHouse.animalsThatLiveHere)
			{
				if (!animalHouse.animals.ContainsKey(item))
				{
					num = item;
					break;
				}
			}
			if (!Game1.getFarm().animals.Remove(num))
			{
				break;
			}
			animalHouse.animalsThatLiveHere.Remove(num);
			{
				foreach (KeyValuePair<long, FarmAnimal> pair in Game1.getFarm().animals.Pairs)
				{
					pair.Value.moodMessage.Value = 5;
				}
				break;
			}
		}
		case 3:
			farm.objects.Add(targetLocation, ItemRegistry.Create<Object>("(BC)95"));
			break;
		}
	}
}
