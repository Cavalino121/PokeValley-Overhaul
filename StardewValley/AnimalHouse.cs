using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Events;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Machines;
using StardewValley.TokenizableStrings;
using xTile.Dimensions;

namespace StardewValley;

public class AnimalHouse : GameLocation
{
	[XmlElement("animalLimit")]
	public readonly NetInt animalLimit = new NetInt(4);

	public readonly NetLongList animalsThatLiveHere = new NetLongList();

	[XmlIgnore]
	public bool hasShownIncubatorBuildingFullMessage;

	public AnimalHouse()
	{
	}

	public AnimalHouse(string mapPath, string name)
		: base(mapPath, name)
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(animalLimit, "animalLimit").AddField(animalsThatLiveHere, "animalsThatLiveHere");
	}

	/// <inheritdoc />
	public override void OnParentBuildingUpgraded(Building building)
	{
		base.OnParentBuildingUpgraded(building);
		BuildingData data = building.GetData();
		if (data != null)
		{
			animalLimit.Value = data.MaxOccupants;
		}
		resetPositionsOfAllAnimals();
		loadLights();
	}

	public bool isFull()
	{
		return animalsThatLiveHere.Count >= animalLimit.Value;
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		if (who.ActiveObject?.QualifiedItemId == "(O)178" && doesTileHaveProperty(tileLocation.X, tileLocation.Y, "Trough", "Back") != null && !objects.ContainsKey(new Vector2(tileLocation.X, tileLocation.Y)))
		{
			objects.Add(new Vector2(tileLocation.X, tileLocation.Y), (Object)who.ActiveObject.getOne());
			who.reduceActiveItemByOne();
			who.currentLocation.playSound("coin");
			Game1.haltAfterCheck = false;
			return true;
		}
		return base.checkAction(tileLocation, viewport, who);
	}

	protected override void resetSharedState()
	{
		resetPositionsOfAllAnimals();
		foreach (Object value in objects.Values)
		{
			if (!value.bigCraftable.Value)
			{
				continue;
			}
			MachineData machineData = value.GetMachineData();
			if (machineData == null || !machineData.IsIncubator || value.heldObject.Value == null || value.MinutesUntilReady > 0)
			{
				continue;
			}
			if (!isFull())
			{
				string text = "??";
				FarmAnimalData animalDataFromEgg = FarmAnimal.GetAnimalDataFromEgg(value.heldObject.Value, this);
				if (animalDataFromEgg != null && animalDataFromEgg.BirthText != null)
				{
					text = TokenParser.ParseText(animalDataFromEgg.BirthText);
				}
				currentEvent = new Event("none/-1000 -1000/farmer 2 9 0/pause 250/message \"" + text + "\"/pause 500/animalNaming/pause 500/end");
				break;
			}
			if (!hasShownIncubatorBuildingFullMessage)
			{
				hasShownIncubatorBuildingFullMessage = true;
				Game1.showGlobalMessage(Game1.content.LoadString("Strings\\Locations:AnimalHouse_Incubator_HouseFull"));
			}
		}
		base.resetSharedState();
	}

	/// <summary>Hatch an incubated animal egg that's ready to hatch, if there are any.</summary>
	/// <param name="name">The name of the animal to set.</param>
	public void addNewHatchedAnimal(string name)
	{
		bool flag = false;
		foreach (Object value in objects.Values)
		{
			if (value.bigCraftable.Value)
			{
				MachineData machineData = value.GetMachineData();
				if (machineData != null && machineData.IsIncubator && value.heldObject.Value != null && value.MinutesUntilReady <= 0 && !isFull())
				{
					flag = true;
					string id;
					FarmAnimalData data;
					FarmAnimal farmAnimal = new FarmAnimal(FarmAnimal.TryGetAnimalDataFromEgg(value.heldObject.Value, this, out id, out data) ? id : "White Chicken", Game1.multiplayer.getNewID(), Game1.player.UniqueMultiplayerID);
					farmAnimal.Name = name;
					farmAnimal.displayName = name;
					value.heldObject.Value = null;
					adoptAnimal(farmAnimal);
					break;
				}
			}
		}
		if (!flag && Game1.farmEvent is QuestionEvent questionEvent)
		{
			FarmAnimal farmAnimal2 = new FarmAnimal(questionEvent.animal.type.Value, Game1.multiplayer.getNewID(), Game1.player.UniqueMultiplayerID);
			farmAnimal2.Name = name;
			farmAnimal2.displayName = name;
			farmAnimal2.parentId.Value = questionEvent.animal.myID.Value;
			adoptAnimal(farmAnimal2);
			questionEvent.forceProceed = true;
		}
		Game1.exitActiveMenu();
	}

	/// <summary>Add an animal to this location and set the location as the animal's home.</summary>
	/// <param name="animal">The animal to adopt.</param>
	public void adoptAnimal(FarmAnimal animal)
	{
		animals.Add(animal.myID.Value, animal);
		animal.currentLocation = this;
		animalsThatLiveHere.Add(animal.myID.Value);
		animal.homeInterior = this;
		animal.setRandomPosition(this);
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			string text = animal.displayType;
			if (text == "White Chicken" || text == "Brown Chicken")
			{
				text = "Chicken";
			}
			allFarmer.autoGenerateActiveDialogueEvent("purchasedAnimal_" + text);
		}
	}

	public void resetPositionsOfAllAnimals()
	{
		foreach (KeyValuePair<long, FarmAnimal> pair in animals.Pairs)
		{
			pair.Value.setRandomPosition(this);
		}
	}

	/// <inheritdoc />
	public override bool dropObject(Object obj, Vector2 location, xTile.Dimensions.Rectangle viewport, bool initialPlacement, Farmer who = null)
	{
		Vector2 key = new Vector2((int)(location.X / 64f), (int)(location.Y / 64f));
		if (obj.QualifiedItemId == "(O)178" && doesTileHaveProperty((int)key.X, (int)key.Y, "Trough", "Back") != null)
		{
			return objects.TryAdd(key, obj);
		}
		return base.dropObject(obj, location, viewport, initialPlacement);
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		animalLimit.Value = ((AnimalHouse)l).animalLimit.Value;
		base.TransferDataFromSavedLocation(l);
	}

	public override void DayUpdate(int dayOfMonth)
	{
		base.DayUpdate(dayOfMonth);
		if (HasMapPropertyWithValue("AutoFeed"))
		{
			feedAllAnimals();
		}
	}

	public void feedAllAnimals()
	{
		GameLocation rootLocation = GetRootLocation();
		int num = 0;
		for (int i = 0; i < map.Layers[0].LayerWidth; i++)
		{
			for (int j = 0; j < map.Layers[0].LayerHeight; j++)
			{
				if (doesTileHaveProperty(i, j, "Trough", "Back") == null)
				{
					continue;
				}
				Vector2 key = new Vector2(i, j);
				if (!objects.ContainsKey(key))
				{
					Object hayFromAnySilo = GameLocation.GetHayFromAnySilo(rootLocation);
					if (hayFromAnySilo == null)
					{
						return;
					}
					objects.Add(key, hayFromAnySilo);
					num++;
				}
				if (num >= animalLimit.Value)
				{
					return;
				}
			}
		}
	}
}
