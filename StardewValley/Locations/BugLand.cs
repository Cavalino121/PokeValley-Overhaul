using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using StardewValley.Monsters;
using xTile.Layers;

namespace StardewValley.Locations;

public class BugLand : GameLocation
{
	[XmlElement("hasSpawnedBugsToday")]
	public bool hasSpawnedBugsToday;

	public BugLand()
	{
	}

	public BugLand(string map, string name)
		: base(map, name)
	{
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		if (l is BugLand bugLand)
		{
			hasSpawnedBugsToday = bugLand.hasSpawnedBugsToday;
		}
		base.TransferDataFromSavedLocation(l);
	}

	public override void hostSetup()
	{
		base.hostSetup();
		if (Game1.IsMasterGame && !hasSpawnedBugsToday)
		{
			InitializeBugLand();
		}
	}

	public override void DayUpdate(int dayOfMonth)
	{
		base.DayUpdate(dayOfMonth);
		characters.RemoveWhere((NPC npc) => npc is Grub || npc is Fly);
		hasSpawnedBugsToday = false;
	}

	public virtual void InitializeBugLand()
	{
		if (hasSpawnedBugsToday)
		{
			return;
		}
		hasSpawnedBugsToday = true;
		Layer layer = map.RequireLayer("Paths");
		for (int i = 0; i < map.Layers[0].LayerWidth; i++)
		{
			for (int j = 0; j < map.Layers[0].LayerHeight; j++)
			{
				if (!(Game1.random.NextDouble() < 0.33))
				{
					continue;
				}
				int tileIndexAt = layer.GetTileIndexAt(i, j);
				if (tileIndexAt == -1)
				{
					continue;
				}
				Vector2 vector = new Vector2(i, j);
				switch (tileIndexAt)
				{
				case 13:
				case 14:
				case 15:
					if (!objects.ContainsKey(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(GameLocation.getWeedForSeason(Game1.random, Season.Spring)));
					}
					break;
				case 16:
					if (!objects.ContainsKey(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(Game1.random.Choose("(O)343", "(O)450")));
					}
					break;
				case 17:
					if (!objects.ContainsKey(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(Game1.random.Choose("(O)343", "(O)450")));
					}
					break;
				case 18:
					if (!objects.ContainsKey(vector))
					{
						objects.Add(vector, ItemRegistry.Create<Object>(Game1.random.Choose("(O)294", "(O)295")));
					}
					break;
				case 28:
					if (CanSpawnCharacterHere(vector) && characters.Count < 50)
					{
						characters.Add(new Grub(new Vector2(vector.X * 64f, vector.Y * 64f), hard: true));
					}
					break;
				}
			}
		}
	}
}
