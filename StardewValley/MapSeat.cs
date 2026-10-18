using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Network;

namespace StardewValley;

public class MapSeat : INetObject<NetFields>, ISittable
{
	[XmlIgnore]
	public static Texture2D mapChairTexture;

	[XmlIgnore]
	public NetLongDictionary<int, NetInt> sittingFarmers = new NetLongDictionary<int, NetInt>();

	[XmlIgnore]
	public NetVector2 tilePosition = new NetVector2();

	[XmlIgnore]
	public NetVector2 size = new NetVector2();

	[XmlIgnore]
	public NetInt direction = new NetInt();

	[XmlIgnore]
	public NetVector2 drawTilePosition = new NetVector2(new Vector2(-1f, -1f));

	[XmlIgnore]
	public NetBool seasonal = new NetBool();

	[XmlIgnore]
	public NetString seatType = new NetString();

	[XmlIgnore]
	public NetString textureFile = new NetString(null);

	[XmlIgnore]
	public string _loadedTextureFile;

	[XmlIgnore]
	public Texture2D overlayTexture;

	[XmlIgnore]
	public int localSittingDirection = 2;

	[XmlIgnore]
	public Vector3? customDrawValues;

	[XmlIgnore]
	public NetFields NetFields { get; } = new NetFields("MapSeat");

	public MapSeat()
	{
		NetFields.SetOwner(this).AddField(sittingFarmers, "sittingFarmers").AddField(tilePosition, "tilePosition")
			.AddField(size, "size")
			.AddField(direction, "direction")
			.AddField(drawTilePosition, "drawTilePosition")
			.AddField(seasonal, "seasonal")
			.AddField(seatType, "seatType")
			.AddField(textureFile, "textureFile");
	}

	public static MapSeat FromData(string data, int x, int y)
	{
		MapSeat mapSeat = new MapSeat();
		try
		{
			string[] array = data.Split('/');
			mapSeat.tilePosition.Set(new Vector2(x, y));
			mapSeat.size.Set(new Vector2(int.Parse(array[0]), int.Parse(array[1])));
			mapSeat.seatType.Value = array[3];
			int parsed;
			if (array[2] == "opposite")
			{
				mapSeat.direction.Value = -2;
			}
			else if (Utility.TryParseDirection(array[2], out parsed))
			{
				mapSeat.direction.Value = parsed;
			}
			else
			{
				mapSeat.direction.Value = 2;
			}
			mapSeat.drawTilePosition.Set(new Vector2(int.Parse(array[4]), int.Parse(array[5])));
			mapSeat.seasonal.Value = array[6] == "true";
			if (array.Length > 7)
			{
				mapSeat.textureFile.Value = array[7];
			}
			else
			{
				mapSeat.textureFile.Value = null;
			}
		}
		catch (Exception)
		{
		}
		return mapSeat;
	}

	public bool IsBlocked(GameLocation location)
	{
		Rectangle seatBounds = GetSeatBounds();
		seatBounds.X *= 64;
		seatBounds.Y *= 64;
		seatBounds.Width *= 64;
		seatBounds.Height *= 64;
		Rectangle value = seatBounds;
		switch (direction.Value)
		{
		case 0:
			value.Y -= 32;
			value.Height += 32;
			break;
		case 2:
			value.Height += 32;
			break;
		case 3:
			value.X -= 32;
			value.Width += 32;
			break;
		case 1:
			value.Width += 32;
			break;
		}
		foreach (NPC item in (Game1.CurrentEvent != null) ? Game1.CurrentEvent.actors : location.characters.ToList())
		{
			Rectangle boundingBox = item.GetBoundingBox();
			if (boundingBox.Intersects(seatBounds))
			{
				return true;
			}
			if (!item.isMovingOnPathFindPath.Value && boundingBox.Intersects(value))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsSittingHere(Farmer who)
	{
		return sittingFarmers.ContainsKey(who.UniqueMultiplayerID);
	}

	public bool HasSittingFarmers()
	{
		return sittingFarmers.Length > 0;
	}

	public List<Vector2> GetSeatPositions(bool ignore_offsets = false)
	{
		customDrawValues = null;
		List<Vector2> list = new List<Vector2>();
		string value = seatType.Value;
		if (!(value == "playground"))
		{
			if (value == "ccdesk")
			{
				Vector2 item = new Vector2(tilePosition.X + 0.5f, tilePosition.Y);
				if (!ignore_offsets)
				{
					item.Y -= 0.4f;
				}
				list.Add(item);
			}
			else if (seatType.Value.StartsWith("custom "))
			{
				float x = 0f;
				float y = 0f;
				float z = 0f;
				string[] array = ArgUtility.SplitBySpace(seatType.Value);
				try
				{
					if (array.Length > 1)
					{
						x = float.Parse(array[1]);
					}
					if (array.Length > 2)
					{
						y = float.Parse(array[2]);
					}
					if (array.Length > 3)
					{
						z = float.Parse(array[3]);
					}
				}
				catch (Exception)
				{
				}
				customDrawValues = new Vector3(x, y, z);
				Vector2 item2 = new Vector2(tilePosition.X + customDrawValues.Value.X, tilePosition.Y);
				if (!ignore_offsets)
				{
					item2.Y += customDrawValues.Value.Y;
				}
				list.Add(item2);
			}
			else
			{
				for (int i = 0; (float)i < size.X; i++)
				{
					for (int j = 0; (float)j < size.Y; j++)
					{
						Vector2 vector = new Vector2(0f, 0f);
						if (seatType.Value.StartsWith("bench"))
						{
							if (direction.Value == 2)
							{
								vector.Y += 0.25f;
							}
							else if ((direction.Value == 3 || direction.Value == 1) && j == 0)
							{
								vector.Y += 0.5f;
							}
						}
						if (seatType.Value.StartsWith("picnic"))
						{
							switch (direction.Value)
							{
							case 2:
								vector.Y -= 0.25f;
								break;
							case 0:
								vector.Y += 0.25f;
								break;
							}
						}
						if (seatType.Value.EndsWith("swings"))
						{
							vector.Y -= 0.5f;
						}
						else if (seatType.Value.EndsWith("summitbench"))
						{
							vector.Y -= 0.2f;
						}
						else if (seatType.Value.EndsWith("tall"))
						{
							vector.Y -= 0.3f;
						}
						else if (seatType.Value.EndsWith("short"))
						{
							vector.Y += 0.3f;
						}
						if (ignore_offsets)
						{
							vector = Vector2.Zero;
						}
						list.Add(tilePosition.Value + new Vector2((float)i + vector.X, (float)j + vector.Y));
					}
				}
			}
		}
		else
		{
			Vector2 item3 = new Vector2(tilePosition.X + 0.75f, tilePosition.Y);
			if (!ignore_offsets)
			{
				item3.Y -= 0.1f;
			}
			list.Add(item3);
		}
		return list;
	}

	public virtual void Draw(SpriteBatch b)
	{
		if (_loadedTextureFile != textureFile.Value)
		{
			_loadedTextureFile = textureFile.Value;
			try
			{
				overlayTexture = Game1.content.Load<Texture2D>(_loadedTextureFile);
			}
			catch (Exception)
			{
				overlayTexture = null;
			}
		}
		if (overlayTexture == null)
		{
			overlayTexture = mapChairTexture;
		}
		if (drawTilePosition.Value.X >= 0f && HasSittingFarmers())
		{
			float num = 0f;
			if (customDrawValues.HasValue)
			{
				num = customDrawValues.Value.Z;
			}
			else if (seatType.Value.StartsWith("highback_chair") || seatType.Value.StartsWith("ccdesk"))
			{
				num = 1f;
			}
			Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(tilePosition.X * 64f, (tilePosition.Y - num) * 64f));
			float layerDepth = (float)(((double)((float)(int)tilePosition.Y + size.Y) + 0.1) * 64.0) / 10000f;
			Rectangle value = new Rectangle((int)drawTilePosition.Value.X * 16, (int)(drawTilePosition.Value.Y - num) * 16, (int)size.Value.X * 16, (int)(size.Value.Y + num) * 16);
			if (seasonal.Value)
			{
				value.X += value.Width * Game1.currentLocation.GetSeasonIndex();
			}
			b.Draw(overlayTexture, position, value, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth);
		}
	}

	public bool OccupiesTile(int x, int y)
	{
		return GetSeatBounds().Contains(x, y);
	}

	public virtual Vector2? AddSittingFarmer(Farmer who)
	{
		if (who == Game1.player)
		{
			localSittingDirection = direction.Value;
			if (seatType.Value.StartsWith("stool"))
			{
				localSittingDirection = Game1.player.FacingDirection;
			}
			if (direction.Value == -2)
			{
				localSittingDirection = Utility.GetOppositeFacingDirection(Game1.player.FacingDirection);
			}
			if (seatType.Value.StartsWith("bathchair") && localSittingDirection == 0)
			{
				localSittingDirection = 2;
			}
		}
		List<Vector2> seatPositions = GetSeatPositions();
		if (seatPositions.Count == 0)
		{
			return null;
		}
		CheckSeatOccupancyIfTemporaryMap(who, seatPositions, out var seatsFilled);
		if (seatsFilled.All((bool occupied) => occupied))
		{
			return null;
		}
		int value = -1;
		Vector2? result = null;
		float num = 96f;
		for (int num2 = 0; num2 < seatPositions.Count; num2++)
		{
			if (!sittingFarmers.Values.Contains(num2) && !seatsFilled[num2])
			{
				float num3 = ((seatPositions[num2] + new Vector2(0.5f, 0.5f)) * 64f - who.getStandingPosition()).Length();
				if (num3 < num)
				{
					num = num3;
					result = seatPositions[num2];
					value = num2;
				}
			}
		}
		if (result.HasValue)
		{
			sittingFarmers[who.UniqueMultiplayerID] = value;
		}
		return result;
	}

	public bool IsSeatHere(GameLocation location)
	{
		return location.mapSeats.Contains(this);
	}

	public int GetSittingDirection()
	{
		return localSittingDirection;
	}

	public Vector2? GetSittingPosition(Farmer who, bool ignore_offsets = false)
	{
		if (sittingFarmers.TryGetValue(who.UniqueMultiplayerID, out var value))
		{
			return GetSeatPositions(ignore_offsets)[value];
		}
		return null;
	}

	public virtual Rectangle GetSeatBounds()
	{
		if (seatType.Value == "chair" && direction.Value == 0)
		{
			new Rectangle((int)tilePosition.X, (int)tilePosition.Y + 1, (int)size.X, (int)size.Y - 1);
		}
		return new Rectangle((int)tilePosition.X, (int)tilePosition.Y, (int)size.X, (int)size.Y);
	}

	public virtual void RemoveSittingFarmer(Farmer farmer)
	{
		sittingFarmers.Remove(farmer.UniqueMultiplayerID);
	}

	public virtual int GetSittingFarmerCount()
	{
		return sittingFarmers.Length;
	}

	/// <summary>Manually check seat occupancy if we're in a non-synced temporary location (e.g. for an event or festival).</summary>
	/// <param name="who">The player for which to load seats.</param>
	/// <param name="seatPositions">The tile positions containing seats.</param>
	/// <param name="seatsFilled">The flags which indicate whether each available seat is occupied.</param>
	private void CheckSeatOccupancyIfTemporaryMap(Farmer who, List<Vector2> seatPositions, out bool[] seatsFilled)
	{
		seatsFilled = new bool[seatPositions.Count];
		GameLocation currentLocation = who.currentLocation;
		if (currentLocation == null || !currentLocation.IsTemporary)
		{
			return;
		}
		FarmerCollection farmerCollection = currentLocation.farmers ?? Game1.getOnlineFarmers();
		if (farmerCollection.Count <= 1)
		{
			return;
		}
		List<Vector2> seatPositions2 = GetSeatPositions(ignore_offsets: true);
		Vector2 value = seatPositions2[0];
		Vector2 value2 = seatPositions2[0];
		for (int i = 1; i < seatPositions2.Count; i++)
		{
			Vector2 value3 = seatPositions2[i];
			Vector2.Min(ref value, ref value3, out value);
			Vector2.Max(ref value2, ref value3, out value2);
		}
		value -= new Vector2(1E-05f, 1E-05f);
		value2 += new Vector2(1E-05f, 1E-05f);
		int num = seatPositions2.Count;
		foreach (Farmer item in farmerCollection)
		{
			if (!item.isSitting.Value || item.uniqueMultiplayerID == who.uniqueMultiplayerID)
			{
				continue;
			}
			Vector2 value4 = item.mapChairSitPosition.Value;
			if (!(value4.X > value.X) || !(value4.X < value2.X) || !(value4.Y > value.Y) || !(value4.Y < value2.Y))
			{
				continue;
			}
			for (int j = 0; j < seatPositions2.Count; j++)
			{
				if (!seatsFilled[j])
				{
					Vector2 vector = seatPositions2[j] - value4;
					if (Math.Abs(vector.X) < 1E-05f && Math.Abs(vector.Y) < 1E-05f)
					{
						seatsFilled[j] = true;
						num--;
						break;
					}
				}
			}
			if (num == 0)
			{
				break;
			}
		}
	}
}
