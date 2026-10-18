using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace StardewValley;

public class SlimeHutch : DecoratableLocation
{
	[XmlElement("slimeMatingsLeft")]
	public readonly NetInt slimeMatingsLeft = new NetInt();

	public readonly NetArray<bool, NetBool> waterSpots = new NetArray<bool, NetBool>(4);

	protected int _slimeCapacity = -1;

	public SlimeHutch()
	{
	}

	public SlimeHutch(string m, string name)
		: base(m, name)
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(slimeMatingsLeft, "slimeMatingsLeft").AddField(waterSpots, "waterSpots");
	}

	/// <inheritdoc />
	public override void OnParentBuildingUpgraded(Building building)
	{
		base.OnParentBuildingUpgraded(building);
		_slimeCapacity = -1;
	}

	public bool isFull()
	{
		if (_slimeCapacity < 0)
		{
			_slimeCapacity = ParentBuilding?.GetData()?.MaxOccupants ?? 20;
		}
		return characters.Count >= _slimeCapacity;
	}

	public override bool canSlimeMateHere()
	{
		int value = slimeMatingsLeft.Value;
		slimeMatingsLeft.Value--;
		if (!isFull())
		{
			return value > 0;
		}
		return false;
	}

	public override bool canSlimeHatchHere()
	{
		return !isFull();
	}

	public override void DayUpdate(int dayOfMonth)
	{
		int num = 0;
		int num2 = Game1.random.Next(waterSpots.Length);
		for (int i = 0; i < waterSpots.Length; i++)
		{
			if (waterSpots[(i + num2) % waterSpots.Length] && num * 5 < characters.Count)
			{
				num++;
				waterSpots[(i + num2) % waterSpots.Length] = false;
			}
		}
		foreach (Object value in objects.Values)
		{
			if (!value.IsSprinkler())
			{
				continue;
			}
			foreach (Vector2 sprinklerTile in value.GetSprinklerTiles())
			{
				if (sprinklerTile.X == 16f && sprinklerTile.Y >= 6f && sprinklerTile.Y <= 9f)
				{
					waterSpots[(int)sprinklerTile.Y - 6] = true;
				}
			}
		}
		for (int num3 = Math.Min(characters.Count / 5, num); num3 > 0; num3--)
		{
			int num4 = 50;
			Vector2 randomTile = getRandomTile();
			while ((!CanItemBePlacedHere(randomTile, itemIsPassable: false, CollisionMask.All, CollisionMask.None) || doesTileHaveProperty((int)randomTile.X, (int)randomTile.Y, "NPCBarrier", "Back") != null || randomTile.Y >= 12f) && num4 > 0)
			{
				randomTile = getRandomTile();
				num4--;
			}
			if (num4 > 0)
			{
				Object obj = ItemRegistry.Create<Object>("(BC)56");
				obj.fragility.Value = 2;
				objects.Add(randomTile, obj);
			}
		}
		while (slimeMatingsLeft.Value > 0)
		{
			if (characters.Count > 1 && !isFull() && characters[Game1.random.Next(characters.Count)] is GreenSlime greenSlime && greenSlime.ageUntilFullGrown.Value <= 0)
			{
				for (int j = 1; j < 10; j++)
				{
					GreenSlime greenSlime2 = (GreenSlime)Utility.checkForCharacterWithinArea(greenSlime.GetType(), greenSlime.Position, this, new Rectangle((int)greenSlime.Position.X - 64 * j, (int)greenSlime.Position.Y - 64 * j, 64 * (j * 2 + 1), 64 * (j * 2 + 1)));
					if (greenSlime2 != null && greenSlime2.cute.Value != greenSlime.cute.Value && greenSlime2.ageUntilFullGrown.Value <= 0)
					{
						greenSlime.mateWith(greenSlime2, this);
						break;
					}
				}
			}
			slimeMatingsLeft.Value--;
		}
		slimeMatingsLeft.Value = characters.Count / 5 + 1;
		base.DayUpdate(dayOfMonth);
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		if (l is SlimeHutch slimeHutch)
		{
			for (int i = 0; i < waterSpots.Length; i++)
			{
				if (i < slimeHutch.waterSpots.Count)
				{
					waterSpots[i] = slimeHutch.waterSpots[i];
				}
			}
		}
		base.TransferDataFromSavedLocation(l);
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		if (objects.TryGetValue(new Vector2(1f, 4f), out var value))
		{
			value.Fragility = 0;
		}
	}

	public override bool performToolAction(Tool t, int tileX, int tileY)
	{
		if (t is WateringCan && tileX == 16 && tileY >= 6 && tileY <= 9)
		{
			waterSpots[tileY - 6] = true;
		}
		return false;
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		for (int i = 0; i < waterSpots.Length; i++)
		{
			int index = (waterSpots[i] ? 2135 : 2134);
			setMapTile(16, 6 + i, index, "Buildings", "untitled tile sheet");
		}
	}
}
