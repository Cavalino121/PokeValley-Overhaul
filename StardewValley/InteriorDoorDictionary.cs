using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Network;

namespace StardewValley;

public class InteriorDoorDictionary : NetPointDictionary<bool, InteriorDoor>
{
	public struct DoorCollection : IEnumerable<InteriorDoor>, IEnumerable
	{
		public struct Enumerator : IEnumerator<InteriorDoor>, IEnumerator, IDisposable
		{
			private readonly InteriorDoorDictionary _dict;

			private Dictionary<Point, InteriorDoor>.Enumerator _enumerator;

			private InteriorDoor _current;

			private bool _done;

			public InteriorDoor Current => _current;

			object IEnumerator.Current
			{
				get
				{
					if (_done)
					{
						throw new InvalidOperationException();
					}
					return _current;
				}
			}

			public Enumerator(InteriorDoorDictionary dict)
			{
				_dict = dict;
				_enumerator = _dict.FieldDict.GetEnumerator();
				_current = null;
				_done = false;
			}

			public bool MoveNext()
			{
				if (_enumerator.MoveNext())
				{
					KeyValuePair<Point, InteriorDoor> current = _enumerator.Current;
					_current = current.Value;
					_current.Location = _dict.location;
					_current.Position = current.Key;
					return true;
				}
				_done = true;
				_current = null;
				return false;
			}

			public void Dispose()
			{
			}

			void IEnumerator.Reset()
			{
				_enumerator = _dict.FieldDict.GetEnumerator();
				_current = null;
				_done = false;
			}
		}

		private InteriorDoorDictionary _dict;

		public DoorCollection(InteriorDoorDictionary dict)
		{
			_dict = dict;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(_dict);
		}

		IEnumerator<InteriorDoor> IEnumerable<InteriorDoor>.GetEnumerator()
		{
			return new Enumerator(_dict);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(_dict);
		}
	}

	private GameLocation location;

	public DoorCollection Doors => new DoorCollection(this);

	public InteriorDoorDictionary(GameLocation location)
	{
		this.location = location;
	}

	protected override void setFieldValue(InteriorDoor door, Point position, bool open)
	{
		door.Location = location;
		door.Position = position;
		base.setFieldValue(door, position, open);
	}

	public void ResetSharedState()
	{
		if (location.isOutdoors.Value)
		{
			return;
		}
		foreach (Point item in GetDoorTilesFromMapProperty(location))
		{
			base[item] = false;
		}
	}

	public void ResetLocalState()
	{
		if (location.isOutdoors.Value)
		{
			return;
		}
		foreach (Point item in GetDoorTilesFromMapProperty(location))
		{
			if (ContainsKey(item))
			{
				InteriorDoor interiorDoor = base.FieldDict[item];
				interiorDoor.Location = location;
				interiorDoor.Position = item;
				interiorDoor.ResetLocalState();
			}
		}
	}

	/// <summary>Get the tile positions containing doors based on the <c>Doors</c> map property.</summary>
	/// <param name="location">The location whose map property to read.</param>
	public static IEnumerable<Point> GetDoorTilesFromMapProperty(GameLocation location)
	{
		string[] fields = location.GetMapPropertySplitBySpaces("Doors");
		for (int i = 0; i < fields.Length; i += 4)
		{
			if (ArgUtility.TryGetPoint(fields, i, out var value, out var error, "Point tile"))
			{
				yield return value;
			}
			else
			{
				location.LogMapPropertyError("Doors", fields, error);
			}
		}
	}

	public void MakeMapModifications()
	{
		foreach (InteriorDoor door in Doors)
		{
			door.ApplyMapModifications();
		}
	}

	public void CleanUpLocalState()
	{
		foreach (InteriorDoor door in Doors)
		{
			door.CleanUpLocalState();
		}
	}

	public void Update(GameTime time)
	{
		foreach (InteriorDoor door in Doors)
		{
			door.Update(time);
		}
	}

	public void Draw(SpriteBatch b)
	{
		foreach (InteriorDoor door in Doors)
		{
			door.Draw(b);
		}
	}
}
