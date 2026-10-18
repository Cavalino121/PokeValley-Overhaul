using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.GameData.FloorsAndPaths;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Tools;

namespace StardewValley.TerrainFeatures;

public class Flooring : TerrainFeature
{
	private struct NeighborLoc
	{
		public readonly Vector2 Offset;

		public readonly byte Direction;

		public readonly byte InvDirection;

		public NeighborLoc(Vector2 a, byte b, byte c)
		{
			Offset = a;
			Direction = b;
			InvDirection = c;
		}
	}

	private struct Neighbor
	{
		public readonly Flooring feature;

		public readonly byte direction;

		public readonly byte invDirection;

		public Neighbor(Flooring a, byte b, byte c)
		{
			feature = a;
			direction = b;
			invDirection = c;
		}
	}

	public const byte N = 1;

	public const byte E = 2;

	public const byte S = 4;

	public const byte W = 8;

	public const byte NE = 16;

	public const byte NW = 32;

	public const byte SE = 64;

	public const byte SW = 128;

	public const byte Cardinals = 15;

	public static readonly Vector2 N_Offset = new Vector2(0f, -1f);

	public static readonly Vector2 E_Offset = new Vector2(1f, 0f);

	public static readonly Vector2 S_Offset = new Vector2(0f, 1f);

	public static readonly Vector2 W_Offset = new Vector2(-1f, 0f);

	public static readonly Vector2 NE_Offset = new Vector2(1f, -1f);

	public static readonly Vector2 NW_Offset = new Vector2(-1f, -1f);

	public static readonly Vector2 SE_Offset = new Vector2(1f, 1f);

	public static readonly Vector2 SW_Offset = new Vector2(-1f, 1f);

	public const string wood = "0";

	public const string stone = "1";

	public const string ghost = "2";

	public const string iceTile = "3";

	public const string straw = "4";

	public const string gravel = "5";

	public const string boardwalk = "6";

	public const string colored_cobblestone = "7";

	public const string cobblestone = "8";

	public const string steppingStone = "9";

	public const string brick = "10";

	public const string plankFlooring = "11";

	public const string townFlooring = "12";

	[XmlIgnore]
	public Texture2D floorTexture;

	[XmlIgnore]
	public Texture2D floorTextureWinter;

	[InstancedStatic]
	public static Dictionary<byte, int> drawGuide;

	[InstancedStatic]
	public static List<int> drawGuideList;

	[XmlElement("whichFloor")]
	public readonly NetString whichFloor = new NetString();

	[XmlElement("whichView")]
	public readonly NetInt whichView = new NetInt();

	private byte neighborMask;

	protected static Dictionary<string, string> _FloorPathItemLookup;

	private static readonly NeighborLoc[] _offsets = new NeighborLoc[8]
	{
		new NeighborLoc(N_Offset, 1, 4),
		new NeighborLoc(S_Offset, 4, 1),
		new NeighborLoc(E_Offset, 2, 8),
		new NeighborLoc(W_Offset, 8, 2),
		new NeighborLoc(NE_Offset, 16, 128),
		new NeighborLoc(NW_Offset, 32, 64),
		new NeighborLoc(SE_Offset, 64, 32),
		new NeighborLoc(SW_Offset, 128, 16)
	};

	private List<Neighbor> _neighbors = new List<Neighbor>();

	public Flooring()
		: base(needsTick: false)
	{
		loadSprite();
		if (drawGuide == null)
		{
			populateDrawGuide();
		}
	}

	public Flooring(string which)
		: this()
	{
		whichFloor.Value = which;
		ApplyFlooringFlags();
	}

	public override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(whichFloor, "whichFloor").AddField(whichView, "whichView");
	}

	public virtual void ApplyFlooringFlags()
	{
		FloorPathData data = GetData();
		if (data != null && data.ConnectType == FloorPathConnectType.Random)
		{
			whichView.Value = Game1.random.Next(16);
		}
	}

	public static Dictionary<string, string> GetFloorPathItemLookup()
	{
		if (_FloorPathItemLookup == null)
		{
			LoadFloorPathItemLookup();
		}
		return _FloorPathItemLookup;
	}

	/// <summary>Get the flooring or path's data from <see cref="F:StardewValley.Game1.floorPathData" />, if found.</summary>
	public FloorPathData GetData()
	{
		if (!TryGetData(whichFloor.Value, out var data))
		{
			return null;
		}
		return data;
	}

	/// <summary>Try to get a flooring or path's data from <see cref="F:StardewValley.Game1.floorPathData" />.</summary>
	/// <param name="id">The flooring or path type ID (i.e. the key in <see cref="F:StardewValley.Game1.floorPathData" />).</param>
	/// <param name="data">The flooring or path data, if found.</param>
	/// <returns>Returns whether the flooring or path data was found.</returns>
	public static bool TryGetData(string id, out FloorPathData data)
	{
		if (id == null)
		{
			data = null;
			return false;
		}
		return Game1.floorPathData.TryGetValue(id, out data);
	}

	protected static void LoadFloorPathItemLookup()
	{
		_FloorPathItemLookup = new Dictionary<string, string>();
		foreach (KeyValuePair<string, FloorPathData> floorPathDatum in Game1.floorPathData)
		{
			string key = floorPathDatum.Key;
			string itemId = floorPathDatum.Value.ItemId;
			if (!string.IsNullOrEmpty(itemId))
			{
				_FloorPathItemLookup[itemId] = key;
			}
		}
	}

	public override Rectangle getBoundingBox()
	{
		Vector2 tile = Tile;
		return new Rectangle((int)(tile.X * 64f), (int)(tile.Y * 64f), 64, 64);
	}

	public static void populateDrawGuide()
	{
		drawGuide = new Dictionary<byte, int>
		{
			[0] = 0,
			[6] = 1,
			[14] = 2,
			[12] = 3,
			[4] = 16,
			[7] = 17,
			[15] = 18,
			[13] = 19,
			[5] = 32,
			[3] = 33,
			[11] = 34,
			[9] = 35,
			[1] = 48,
			[2] = 49,
			[10] = 50,
			[8] = 51
		};
		drawGuideList = new List<int>(drawGuide.Count);
		foreach (KeyValuePair<byte, int> item in drawGuide)
		{
			drawGuideList.Add(item.Value);
		}
	}

	public override void loadSprite()
	{
	}

	public override void doCollisionAction(Rectangle positionOfCollider, int speedOfCollision, Vector2 tileLocation, Character who)
	{
		base.doCollisionAction(positionOfCollider, speedOfCollision, tileLocation, who);
		FloorPathData data = GetData();
		GameLocation location = Location;
		if (who is Farmer farmer && (location is Farm || location is IslandWest))
		{
			float temporarySpeedBuff = 0.1f;
			if (data != null && data.FarmSpeedBuff >= 0f)
			{
				temporarySpeedBuff = data.FarmSpeedBuff;
			}
			farmer.temporarySpeedBuff = temporarySpeedBuff;
		}
	}

	/// <inheritdoc />
	public override bool isPassable(Character c = null)
	{
		return true;
	}

	public string getFootstepSound()
	{
		return GetData()?.FootstepSound ?? "stoneStep";
	}

	public Point GetTextureCorner(bool useSeasonalVariants = true)
	{
		if (!useSeasonalVariants || !ShouldDrawWinterVersion())
		{
			return GetData().Corner;
		}
		return GetData().WinterCorner;
	}

	public Texture2D GetTexture(bool useSeasonalVariants = true)
	{
		if (useSeasonalVariants && ShouldDrawWinterVersion())
		{
			if (floorTextureWinter == null)
			{
				floorTextureWinter = Game1.content.Load<Texture2D>(GetData().WinterTexture);
			}
			return floorTextureWinter;
		}
		if (floorTexture == null)
		{
			floorTexture = Game1.content.Load<Texture2D>(GetData().Texture);
		}
		return floorTexture;
	}

	public bool ShouldDrawWinterVersion()
	{
		if (Location != null && !Location.isGreenhouse.Value && GetData().WinterTexture != null)
		{
			return Location.IsWinterHere();
		}
		return false;
	}

	public override bool performToolAction(Tool t, int damage, Vector2 tileLocation)
	{
		GameLocation gameLocation = Location ?? Game1.currentLocation;
		if ((t != null || damage > 0) && (damage > 0 || t is Pickaxe || t is Axe))
		{
			FloorPathData data = GetData();
			if (data != null)
			{
				gameLocation.playSound(data.RemovalSound ?? data.PlacementSound, tileLocation);
				Game1.createRadialDebris(gameLocation, data.RemovalDebrisType, (int)tileLocation.X, (int)tileLocation.Y, 4, resource: false);
				if (data.ItemId != null)
				{
					Item item = ItemRegistry.Create(data.ItemId);
					if (item != null)
					{
						gameLocation.debris.Add(new Debris(item, tileLocation * 64f + new Vector2(32f, 32f)));
					}
				}
			}
			return true;
		}
		return false;
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 positionOnScreen, Vector2 tileLocation, float scale, float layerDepth)
	{
	}

	public override void draw(SpriteBatch spriteBatch)
	{
		Vector2 tile = Tile;
		FloorPathData data = GetData();
		if (data == null)
		{
			IItemDataDefinition itemDataDefinition = ItemRegistry.RequireTypeDefinition("(O)");
			spriteBatch.Draw(itemDataDefinition.GetErrorTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f, tile.Y * 64f)), itemDataDefinition.GetErrorSourceRect(), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-09f);
			return;
		}
		Texture2D texture = GetTexture();
		Point textureCorner = GetTextureCorner();
		float num = 1f;
		switch (data.ConnectType)
		{
		case FloorPathConnectType.CornerDecorated:
		{
			int cornerSize2 = data.CornerSize;
			if ((neighborMask & 9) == 9 && (neighborMask & 0x20) == 0)
			{
				spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f, tile.Y * 64f)), new Rectangle(64 - cornerSize2 + textureCorner.X, 48 - cornerSize2 + textureCorner.Y, cornerSize2, cornerSize2), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tile.Y * 64f + 2f + tile.X / 10000f) / 20000f);
			}
			if ((neighborMask & 3) == 3 && (neighborMask & 0x10) == 0)
			{
				spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f + 64f - (float)(cornerSize2 * 4), tile.Y * 64f)), new Rectangle(16 + textureCorner.X, 48 - cornerSize2 + textureCorner.Y, cornerSize2, cornerSize2), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tile.Y * 64f + 2f + tile.X / 10000f + num) / 20000f);
			}
			if ((neighborMask & 6) == 6 && (neighborMask & 0x40) == 0)
			{
				spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f + 64f - (float)(cornerSize2 * 4), tile.Y * 64f + 64f - (float)(cornerSize2 * 4))), new Rectangle(16 + textureCorner.X, textureCorner.Y, cornerSize2, cornerSize2), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tile.Y * 64f + 2f + tile.X / 10000f) / 20000f);
			}
			if ((neighborMask & 0xC) == 12 && (neighborMask & 0x80) == 0)
			{
				spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f, tile.Y * 64f + 64f - (float)(cornerSize2 * 4))), new Rectangle(64 - cornerSize2 + textureCorner.X, textureCorner.Y, cornerSize2, cornerSize2), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tile.Y * 64f + 2f + tile.X / 10000f) / 20000f);
			}
			break;
		}
		case FloorPathConnectType.Default:
		{
			int cornerSize = data.CornerSize;
			if ((neighborMask & 9) == 9 && (neighborMask & 0x20) == 0)
			{
				spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f, tile.Y * 64f)), new Rectangle(64 - cornerSize + textureCorner.X, 48 - cornerSize + textureCorner.Y, cornerSize, cornerSize), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tile.Y * 64f + 2f + tile.X / 10000f) / 20000f);
			}
			if ((neighborMask & 3) == 3 && (neighborMask & 0x10) == 0)
			{
				spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f + 64f - (float)(cornerSize * 4), tile.Y * 64f)), new Rectangle(16 + textureCorner.X, 48 - cornerSize + textureCorner.Y, cornerSize, cornerSize), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tile.Y * 64f + 2f + tile.X / 10000f + num) / 20000f);
			}
			if ((neighborMask & 6) == 6 && (neighborMask & 0x40) == 0)
			{
				spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f + 64f - (float)(cornerSize * 4), tile.Y * 64f + 48f)), new Rectangle(16 + textureCorner.X, textureCorner.Y, cornerSize, cornerSize), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tile.Y * 64f + 2f + tile.X / 10000f) / 20000f);
			}
			if ((neighborMask & 0xC) == 12 && (neighborMask & 0x80) == 0)
			{
				spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f, tile.Y * 64f + 64f - (float)(cornerSize * 4))), new Rectangle(64 - cornerSize + textureCorner.X, textureCorner.Y, cornerSize, cornerSize), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tile.Y * 64f + 2f + tile.X / 10000f) / 20000f);
			}
			break;
		}
		}
		byte key = (byte)(neighborMask & 0xF);
		int num2 = drawGuide[key];
		if (data.ConnectType == FloorPathConnectType.Random)
		{
			num2 = drawGuideList[whichView.Value];
		}
		switch (data.ShadowType)
		{
		case FloorPathShadowType.Square:
			spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)(tile.X * 64f) - 4 - Game1.viewport.X, (int)(tile.Y * 64f) + 4 - Game1.viewport.Y, 64, 64), Color.Black * 0.33f);
			break;
		case FloorPathShadowType.Contoured:
		{
			Color black = Color.Black;
			black.A = (byte)((float)(int)black.A * 0.33f);
			spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f, tile.Y * 64f)) + new Vector2(-4f, 4f), new Rectangle(textureCorner.X + num2 * 16 % 256, num2 / 16 * 16 + textureCorner.Y, 16, 16), black, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-10f);
			break;
		}
		}
		spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f, tile.Y * 64f)), new Rectangle(textureCorner.X + num2 * 16 % 256, num2 / 16 * 16 + textureCorner.Y, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-09f);
	}

	public override bool tickUpdate(GameTime time)
	{
		base.NeedsUpdate = false;
		return false;
	}

	private List<Neighbor> gatherNeighbors()
	{
		List<Neighbor> neighbors = _neighbors;
		neighbors.Clear();
		GameLocation location = Location;
		Vector2 tile = Tile;
		NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>> terrainFeatures = location.terrainFeatures;
		NeighborLoc[] offsets = _offsets;
		for (int i = 0; i < offsets.Length; i++)
		{
			NeighborLoc neighborLoc = offsets[i];
			Vector2 vector = tile + neighborLoc.Offset;
			TerrainFeature value;
			if (location.map != null && !location.isTileOnMap(vector))
			{
				Neighbor item = new Neighbor(null, neighborLoc.Direction, neighborLoc.InvDirection);
				neighbors.Add(item);
			}
			else if (terrainFeatures.TryGetValue(vector, out value) && value is Flooring flooring && flooring.whichFloor.Value == whichFloor.Value)
			{
				Neighbor item2 = new Neighbor(flooring, neighborLoc.Direction, neighborLoc.InvDirection);
				neighbors.Add(item2);
			}
		}
		return neighbors;
	}

	public void OnAdded(GameLocation loc, Vector2 tilePos)
	{
		Location = loc;
		Tile = tilePos;
		List<Neighbor> list = gatherNeighbors();
		neighborMask = 0;
		foreach (Neighbor item in list)
		{
			neighborMask |= item.direction;
			item.feature?.OnNeighborAdded(item.invDirection);
		}
	}

	public void OnRemoved()
	{
		List<Neighbor> list = gatherNeighbors();
		neighborMask = 0;
		foreach (Neighbor item in list)
		{
			item.feature?.OnNeighborRemoved(item.invDirection);
		}
	}

	public void OnNeighborAdded(byte direction)
	{
		neighborMask |= direction;
	}

	public void OnNeighborRemoved(byte direction)
	{
		neighborMask = (byte)(neighborMask & ~direction);
	}
}
