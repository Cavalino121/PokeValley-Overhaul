using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using StardewValley.Audio;
using StardewValley.Extensions;
using StardewValley.Locations;
using xTile.Tiles;

namespace StardewValley.Pathfinding;

/// This class finds a path from one point to another using the A* pathfinding algorithm. Then it will guide the given character along that path.
/// Can only be used on maps where the tile width and height are each 127 or less. 
[InstanceStatics]
public class PathFindController
{
	public delegate bool isAtEnd(PathNode currentNode, Point endPoint, GameLocation location, Character c);

	public delegate void endBehavior(Character c, GameLocation location);

	public const byte impassable = byte.MaxValue;

	public const int timeToWaitBeforeCancelling = 5000;

	private Character character;

	public GameLocation location;

	public Stack<Point> pathToEndPoint;

	public Point endPoint;

	public int finalFacingDirection;

	public int pausedTimer;

	public endBehavior endBehaviorFunction;

	public bool nonDestructivePathing;

	public bool allowPlayerPathingInEvent;

	public bool NPCSchedule;

	protected static readonly sbyte[,] Directions = new sbyte[4, 2]
	{
		{ -1, 0 },
		{ 1, 0 },
		{ 0, 1 },
		{ 0, -1 }
	};

	protected static PriorityQueue _openList = new PriorityQueue();

	protected static HashSet<int> _closedList = new HashSet<int>();

	protected static int _counter = 0;

	public int timerSinceLastCheckPoint;

	public PathFindController(Character c, GameLocation location, Point endPoint, int finalFacingDirection)
		: this(c, location, isAtEndPoint, finalFacingDirection, null, 10000, endPoint)
	{
	}

	public PathFindController(Character c, GameLocation location, Point endPoint, int finalFacingDirection, endBehavior endBehaviorFunction)
		: this(c, location, isAtEndPoint, finalFacingDirection, null, 10000, endPoint)
	{
		this.endPoint = endPoint;
		this.endBehaviorFunction = endBehaviorFunction;
	}

	public PathFindController(Character c, GameLocation location, Point endPoint, int finalFacingDirection, endBehavior endBehaviorFunction, int limit)
		: this(c, location, isAtEndPoint, finalFacingDirection, null, limit, endPoint)
	{
		this.endPoint = endPoint;
		this.endBehaviorFunction = endBehaviorFunction;
	}

	public PathFindController(Character c, GameLocation location, Point endPoint, int finalFacingDirection, bool clearMarriageDialogues = true)
		: this(c, location, isAtEndPoint, finalFacingDirection, null, 10000, endPoint, clearMarriageDialogues)
	{
	}

	public static bool isAtEndPoint(PathNode currentNode, Point endPoint, GameLocation location, Character c)
	{
		if (currentNode.x == endPoint.X)
		{
			return currentNode.y == endPoint.Y;
		}
		return false;
	}

	public PathFindController(Stack<Point> pathToEndPoint, GameLocation location, Character c, Point endPoint)
	{
		this.pathToEndPoint = pathToEndPoint;
		this.location = location;
		character = c;
		this.endPoint = endPoint;
	}

	public PathFindController(Stack<Point> pathToEndPoint, Character c, GameLocation l)
	{
		this.pathToEndPoint = pathToEndPoint;
		character = c;
		location = l;
		NPCSchedule = true;
	}

	public PathFindController(Character c, GameLocation location, isAtEnd endFunction, int finalFacingDirection, endBehavior endBehaviorFunction, int limit, Point endPoint, bool clearMarriageDialogues = true)
	{
		character = c;
		NPC nPC = c as NPC;
		if (nPC != null && nPC.CurrentDialogue.Count > 0 && nPC.CurrentDialogue.Peek().removeOnNextMove)
		{
			nPC.CurrentDialogue.Pop();
		}
		if (nPC != null && clearMarriageDialogues)
		{
			if (nPC.currentMarriageDialogue.Count > 0)
			{
				nPC.currentMarriageDialogue.Clear();
			}
			nPC.shouldSayMarriageDialogue.Value = false;
		}
		this.location = location;
		this.endBehaviorFunction = endBehaviorFunction;
		if (endPoint == Point.Zero)
		{
			endPoint = c.TilePoint;
		}
		this.finalFacingDirection = finalFacingDirection;
		if (!(character is NPC) && !isPlayerPresent() && endFunction == new isAtEnd(isAtEndPoint) && endPoint.X > 0 && endPoint.Y > 0)
		{
			character.Position = new Vector2(endPoint.X * 64, endPoint.Y * 64 - 32);
		}
		else
		{
			pathToEndPoint = findPath(c.TilePoint, endPoint, endFunction, location, character, limit);
		}
	}

	public bool isPlayerPresent()
	{
		return location.farmers.Any();
	}

	public virtual bool update(GameTime time)
	{
		if (pathToEndPoint == null || pathToEndPoint.Count == 0)
		{
			return true;
		}
		if (!NPCSchedule && !isPlayerPresent() && endPoint.X > 0 && endPoint.Y > 0)
		{
			character.Position = new Vector2(endPoint.X * 64, endPoint.Y * 64 - 32);
			return true;
		}
		if (Game1.activeClickableMenu == null || Game1.IsMultiplayer)
		{
			timerSinceLastCheckPoint += time.ElapsedGameTime.Milliseconds;
			Vector2 position = character.Position;
			moveCharacter(time);
			if (character.Position.Equals(position))
			{
				pausedTimer += time.ElapsedGameTime.Milliseconds;
			}
			else
			{
				pausedTimer = 0;
			}
			if (!NPCSchedule && pausedTimer > 5000)
			{
				return true;
			}
		}
		return false;
	}

	public static Stack<Point> findPath(Point startPoint, Point endPoint, isAtEnd endPointFunction, GameLocation location, Character character, int limit)
	{
		if (Interlocked.Increment(ref _counter) != 1)
		{
			throw new Exception();
		}
		try
		{
			bool flag = character is FarmAnimal farmAnimal && farmAnimal.CanSwim() && farmAnimal.isSwimming.Value;
			_openList.Clear();
			_closedList.Clear();
			PriorityQueue openList = _openList;
			HashSet<int> closedList = _closedList;
			int num = 0;
			openList.Enqueue(new PathNode(startPoint.X, startPoint.Y, 0, null), Math.Abs(endPoint.X - startPoint.X) + Math.Abs(endPoint.Y - startPoint.Y));
			int layerWidth = location.map.Layers[0].LayerWidth;
			int layerHeight = location.map.Layers[0].LayerHeight;
			while (!openList.IsEmpty())
			{
				PathNode pathNode = openList.Dequeue();
				if (endPointFunction(pathNode, endPoint, location, character))
				{
					return reconstructPath(pathNode);
				}
				closedList.Add(pathNode.id);
				int num2 = (byte)(pathNode.g + 1);
				for (int i = 0; i < 4; i++)
				{
					int num3 = pathNode.x + Directions[i, 0];
					int num4 = pathNode.y + Directions[i, 1];
					int item = PathNode.ComputeHash(num3, num4);
					if (closedList.Contains(item))
					{
						continue;
					}
					if ((num3 != endPoint.X || num4 != endPoint.Y) && (num3 < 0 || num4 < 0 || num3 >= layerWidth || num4 >= layerHeight))
					{
						closedList.Add(item);
						continue;
					}
					PathNode pathNode2 = new PathNode(num3, num4, pathNode);
					pathNode2.g = (byte)(pathNode.g + 1);
					if (!flag && location.isCollidingPosition(new Rectangle(pathNode2.x * 64 + 1, pathNode2.y * 64 + 1, 62, 62), Game1.viewport, character is Farmer, 0, glider: false, character, pathfinding: true))
					{
						closedList.Add(item);
						continue;
					}
					int priority = num2 + (Math.Abs(endPoint.X - num3) + Math.Abs(endPoint.Y - num4));
					closedList.Add(item);
					openList.Enqueue(pathNode2, priority);
				}
				num++;
				if (num >= limit)
				{
					return null;
				}
			}
			return null;
		}
		finally
		{
			if (Interlocked.Decrement(ref _counter) != 0)
			{
				throw new Exception();
			}
		}
	}

	public static Stack<Point> reconstructPath(PathNode finalNode)
	{
		Stack<Point> stack = new Stack<Point>();
		stack.Push(new Point(finalNode.x, finalNode.y));
		for (PathNode parent = finalNode.parent; parent != null; parent = parent.parent)
		{
			stack.Push(new Point(parent.x, parent.y));
		}
		return stack;
	}

	protected virtual void moveCharacter(GameTime time)
	{
		Point point = pathToEndPoint.Peek();
		Rectangle rectangle = new Rectangle(point.X * 64, point.Y * 64, 64, 64);
		rectangle.Inflate(-2, 0);
		Rectangle boundingBox = character.GetBoundingBox();
		if ((rectangle.Contains(boundingBox) || (boundingBox.Width > rectangle.Width && rectangle.Contains(boundingBox.Center))) && rectangle.Bottom - boundingBox.Bottom >= 2)
		{
			timerSinceLastCheckPoint = 0;
			pathToEndPoint.Pop();
			character.stopWithoutChangingFrame();
			if (pathToEndPoint.Count == 0)
			{
				character.Halt();
				if (finalFacingDirection != -1)
				{
					character.faceDirection(finalFacingDirection);
				}
				if (NPCSchedule)
				{
					NPC nPC = character as NPC;
					nPC.DirectionsToNewLocation = null;
					nPC.endOfRouteMessage.Value = nPC.nextEndOfRouteMessage;
				}
				endBehaviorFunction?.Invoke(character, location);
			}
			return;
		}
		if (character is Farmer farmer)
		{
			farmer.movementDirections.Clear();
		}
		else if (!(location is MovieTheater))
		{
			string name = character.Name;
			for (int i = 0; i < location.characters.Count; i++)
			{
				NPC nPC2 = location.characters[i];
				if (!nPC2.Equals(character) && nPC2.GetBoundingBox().Intersects(boundingBox) && nPC2.isMoving() && string.Compare(nPC2.Name, name, StringComparison.Ordinal) < 0)
				{
					character.Halt();
					return;
				}
			}
		}
		if (boundingBox.Left < rectangle.Left && boundingBox.Right < rectangle.Right)
		{
			character.SetMovingRight(b: true);
		}
		else if (boundingBox.Right > rectangle.Right && boundingBox.Left > rectangle.Left)
		{
			character.SetMovingLeft(b: true);
		}
		else if (boundingBox.Top <= rectangle.Top)
		{
			character.SetMovingDown(b: true);
		}
		else if (boundingBox.Bottom >= rectangle.Bottom - 2)
		{
			character.SetMovingUp(b: true);
		}
		character.MovePosition(time, Game1.viewport, location);
		if (nonDestructivePathing)
		{
			if (rectangle.Intersects(character.nextPosition(character.FacingDirection)))
			{
				Vector2 vector = character.nextPositionVector2();
				Object objectAt = location.getObjectAt((int)vector.X, (int)vector.Y);
				if (objectAt != null)
				{
					if (objectAt is Fence fence && fence.isGate.Value)
					{
						fence.toggleGate(open: true);
					}
					else if (!objectAt.isPassable())
					{
						character.Halt();
						character.controller = null;
						return;
					}
				}
			}
			handleWarps(character.nextPosition(character.getDirection()));
		}
		else if (NPCSchedule)
		{
			handleWarps(character.nextPosition(character.getDirection()));
		}
	}

	public void handleWarps(Rectangle position)
	{
		Warp warp = location.isCollidingWithWarpOrDoor(position, character);
		if (warp == null)
		{
			return;
		}
		if (warp.TargetName == "Trailer" && Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
		{
			warp = new Warp(warp.X, warp.Y, "Trailer_Big", 13, 24, flipFarmer: false);
		}
		if (character is NPC nPC && nPC.isMarried() && nPC.followSchedule)
		{
			GameLocation gameLocation = location;
			if (!(gameLocation is FarmHouse))
			{
				if (gameLocation is BusStop && warp.X <= 9)
				{
					GameLocation home = nPC.getHome();
					Point entryLocation = ((FarmHouse)home).getEntryLocation();
					warp = new Warp(warp.X, warp.Y, home.name.Value, entryLocation.X, entryLocation.Y, flipFarmer: false);
				}
			}
			else
			{
				warp = new Warp(warp.X, warp.Y, "BusStop", 10, 23, flipFarmer: false);
			}
			if (nPC.temporaryController != null && nPC.controller != null)
			{
				nPC.controller.location = Game1.RequireLocation(warp.TargetName);
			}
		}
		string text = warp.TargetName;
		foreach (string activePassiveFestival in Game1.netWorldState.Value.ActivePassiveFestivals)
		{
			if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data) && data.MapReplacements != null && data.MapReplacements.TryGetValue(text, out var value))
			{
				text = value;
				break;
			}
		}
		if (character is NPC nPC2 && (warp.TargetName == "FarmHouse" || warp.TargetName == "Cabin") && nPC2.isMarried() && nPC2.getSpouse() != null)
		{
			location = Utility.getHomeOfFarmer(nPC2.getSpouse());
			Point entryLocation2 = ((FarmHouse)location).getEntryLocation();
			warp = new Warp(warp.X, warp.Y, location.name.Value, entryLocation2.X, entryLocation2.Y, flipFarmer: false);
			if (nPC2.temporaryController != null && nPC2.controller != null)
			{
				nPC2.controller.location = location;
			}
			Game1.warpCharacter(nPC2, location, new Vector2(warp.TargetX, warp.TargetY));
		}
		else
		{
			location = Game1.RequireLocation(text);
			Game1.warpCharacter(character as NPC, warp.TargetName, new Vector2(warp.TargetX, warp.TargetY));
		}
		if (isPlayerPresent() && location.doors.ContainsKey(new Point(warp.X, warp.Y)))
		{
			location.playSound("doorClose", new Vector2(warp.X, warp.Y), null, SoundContext.NPC);
		}
		if (isPlayerPresent() && location.doors.ContainsKey(new Point(warp.TargetX, warp.TargetY - 1)))
		{
			location.playSound("doorClose", new Vector2(warp.TargetX, warp.TargetY), null, SoundContext.NPC);
		}
		if (pathToEndPoint.Count > 0)
		{
			pathToEndPoint.Pop();
		}
		Point tilePoint = character.TilePoint;
		while (pathToEndPoint.Count > 0 && (Math.Abs(pathToEndPoint.Peek().X - tilePoint.X) > 1 || Math.Abs(pathToEndPoint.Peek().Y - tilePoint.Y) > 1))
		{
			pathToEndPoint.Pop();
		}
	}

	[Obsolete("Use findPathForNPCSchedules overload with 'npc' parameter.")]
	public static Stack<Point> findPathForNPCSchedules(Point startPoint, Point endPoint, GameLocation location, int limit)
	{
		return findPathForNPCSchedules(startPoint, endPoint, location, limit, null);
	}

	public static Stack<Point> findPathForNPCSchedules(Point startPoint, Point endPoint, GameLocation location, int limit, Character npc)
	{
		PriorityQueue priorityQueue = new PriorityQueue();
		HashSet<int> hashSet = new HashSet<int>();
		int num = 0;
		priorityQueue.Enqueue(new PathNode(startPoint.X, startPoint.Y, 0, null), Math.Abs(endPoint.X - startPoint.X) + Math.Abs(endPoint.Y - startPoint.Y));
		PathNode pathNode = (PathNode)priorityQueue.Peek();
		int layerWidth = location.map.Layers[0].LayerWidth;
		int layerHeight = location.map.Layers[0].LayerHeight;
		while (!priorityQueue.IsEmpty())
		{
			PathNode pathNode2 = priorityQueue.Dequeue();
			if (pathNode2.x == endPoint.X && pathNode2.y == endPoint.Y)
			{
				return reconstructPath(pathNode2);
			}
			hashSet.Add(pathNode2.id);
			for (int i = 0; i < 4; i++)
			{
				int x = pathNode2.x + Directions[i, 0];
				int y = pathNode2.y + Directions[i, 1];
				int item = PathNode.ComputeHash(x, y);
				if (hashSet.Contains(item))
				{
					continue;
				}
				PathNode pathNode3 = new PathNode(x, y, pathNode2);
				pathNode3.g = (byte)(pathNode2.g + 1);
				if ((pathNode3.x == endPoint.X && pathNode3.y == endPoint.Y) || (pathNode3.x >= 0 && pathNode3.y >= 0 && pathNode3.x < layerWidth && pathNode3.y < layerHeight && !isPositionImpassableForNPCSchedule(location, pathNode3.x, pathNode3.y, npc)))
				{
					int priority = pathNode3.g + getPreferenceValueForTerrainType(location, pathNode3.x, pathNode3.y) + (Math.Abs(endPoint.X - pathNode3.x) + Math.Abs(endPoint.Y - pathNode3.y) + (((pathNode3.x == pathNode2.x && pathNode3.x == pathNode.x) || (pathNode3.y == pathNode2.y && pathNode3.y == pathNode.y)) ? (-2) : 0));
					if (!priorityQueue.Contains(pathNode3, priority))
					{
						priorityQueue.Enqueue(pathNode3, priority);
					}
				}
			}
			pathNode = pathNode2;
			num++;
			if (num >= limit)
			{
				return null;
			}
		}
		return null;
	}

	protected static bool isPositionImpassableForNPCSchedule(GameLocation loc, int x, int y, Character npc)
	{
		Tile tile = loc.Map.RequireLayer("Buildings").Tiles[x, y];
		if (tile != null && tile.TileIndex != -1)
		{
			if (tile.TileIndexProperties.TryGetValue("Action", out var value) || tile.Properties.TryGetValue("Action", out value))
			{
				string actionText = value?.ToString();
				if (actionText != null && actionText.StartsWith("LockedDoorWarp"))
				{
					return true;
				}
				if (actionText != null && !actionText.Contains("Door") && !actionText.Contains("Passable"))
				{
					return true;
				}
			}
			else if (loc.doesTileHaveProperty(x, y, "Passable", "Buildings") == null && loc.doesTileHaveProperty(x, y, "NPCPassable", "Buildings") == null)
			{
				return true;
			}
		}
		if (loc.doesTileHaveProperty(x, y, "NoPath", "Back") != null)
		{
			return true;
		}
		foreach (Warp warp in loc.warps)
		{
			if (warp.X == x && warp.Y == y)
			{
				return true;
			}
		}
		if (((!(loc.terrainFeatures.GetValueOrDefault(new Vector2(x, y))?.isPassable(npc))) ?? false) || !(loc.getLargeTerrainFeatureAt(x, y)?.isPassable(npc) ?? true))
		{
			return true;
		}
		return false;
	}

	/// <summary>Get the precedence value for a tile position when choosing a path, where lower values are preferred.</summary>
	/// <param name="l">The location to check.</param>
	/// <param name="x">The X tile position.</param>
	/// <param name="y">The Y tile position.</param>
	protected static int getPreferenceValueForTerrainType(GameLocation l, int x, int y)
	{
		return l.doesTileHaveProperty(x, y, "Type", "Back")?.ToLower() switch
		{
			"stone" => -7, 
			"wood" => -4, 
			"dirt" => -2, 
			"grass" => -1, 
			_ => 0, 
		};
	}
}
