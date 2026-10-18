using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Network;

namespace StardewValley.Locations;

public class Racer : INetObject<NetFields>
{
	public NetBool moving = new NetBool();

	public Vector2? lastPosition;

	public NetPosition position = new NetPosition();

	public NetInt direction = new NetInt();

	public float horizontalPosition = -1f;

	public int currentTrackIndex = -1;

	public Vector2 segmentStart = Vector2.Zero;

	public Vector2 segmentEnd = Vector2.Zero;

	public NetVector2 jumpSegmentStart = new NetVector2();

	public NetVector2 jumpSegmentEnd = new NetVector2();

	public NetBool jumping = new NetBool();

	public NetBool tripping = new NetBool();

	public NetBool drawAboveMap = new NetBool();

	public float moveSpeed = 3f;

	public float minMoveSpeed = 3f;

	public float maxMoveSpeed = 6f;

	public float height;

	public float tripTimer;

	public NetInt racerIndex = new NetInt();

	protected Texture2D _texture;

	public bool frame;

	public float nextFrameSwap;

	public float burstDuration;

	public float nextBurst;

	public float extraLuck;

	public float gravity;

	public int _tripLeaps;

	public float progress;

	public NetInt sabotages = new NetInt(0);

	[XmlIgnore]
	public NetFields NetFields { get; } = new NetFields("DesertFestival.Racer");

	public Racer()
	{
		InitNetFields();
		direction.Value = 3;
		_texture = Game1.content.Load<Texture2D>("LooseSprites\\DesertRacers");
	}

	public Racer(int index)
		: this()
	{
		racerIndex.Value = index;
		ResetMoveSpeed();
	}

	public virtual void ResetMoveSpeed()
	{
		minMoveSpeed = 1.5f;
		maxMoveSpeed = 4f;
		extraLuck = Utility.RandomFloat(-0.25f, 0.25f);
		if (racerIndex.Value == 3)
		{
			minMoveSpeed = 0.5f;
			maxMoveSpeed = 3.5f;
		}
		SpeedBurst();
	}

	private void InitNetFields()
	{
		NetFields.SetOwner(this).AddField(racerIndex, "racerIndex").AddField(position.NetFields, "position.NetFields")
			.AddField(direction, "direction")
			.AddField(jumpSegmentStart, "jumpSegmentStart")
			.AddField(jumpSegmentEnd, "jumpSegmentEnd")
			.AddField(jumping, "jumping")
			.AddField(drawAboveMap, "drawAboveMap")
			.AddField(tripping, "tripping")
			.AddField(sabotages, "sabotages")
			.AddField(moving, "moving");
		jumpSegmentStart.Interpolated(interpolate: false, wait: false);
		jumpSegmentEnd.Interpolated(interpolate: false, wait: false);
	}

	public virtual void UpdateRaceProgress(DesertFestival location)
	{
		if (currentTrackIndex < 0)
		{
			progress = location.raceTrack.Length;
			return;
		}
		Vector2 value = segmentEnd - segmentStart;
		float num = value.Length();
		value.Normalize();
		Vector2 value2 = position.Value - segmentStart;
		float num2 = Vector2.Dot(value, value2);
		if (num > 0f)
		{
			num = num2 / num;
		}
		progress = (float)currentTrackIndex + num;
	}

	public virtual void Update(DesertFestival location)
	{
		if (Game1.IsMasterGame)
		{
			bool value = false;
			if (location.currentRaceState.Value == DesertFestival.RaceState.StartingLine && currentTrackIndex < 0)
			{
				if (horizontalPosition < 0f)
				{
					int num = location.netRacers.IndexOf(this);
					horizontalPosition = (float)num / (float)(location.racerCount - 1);
				}
				currentTrackIndex = 0;
				Vector3 trackPosition = location.GetTrackPosition(currentTrackIndex, horizontalPosition);
				segmentStart = position.Value;
				segmentEnd = new Vector2(trackPosition.X, trackPosition.Y);
			}
			float num2 = maxMoveSpeed;
			if (location.currentRaceState.Value == DesertFestival.RaceState.Go)
			{
				if (location.finishedRacers.Count <= 0)
				{
					if (burstDuration > 0f)
					{
						moveSpeed = maxMoveSpeed;
						burstDuration -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
						if (burstDuration <= 0f)
						{
							burstDuration = 0f;
							nextBurst = Utility.RandomFloat(0.75f, 1.5f);
							if (Game1.random.NextDouble() + (double)extraLuck < 0.25)
							{
								nextBurst *= 0.5f;
							}
							if (racerIndex.Value == 3)
							{
								nextBurst *= 0.25f;
							}
							float num3 = location.raceTrack.Length;
							foreach (Racer netRacer in location.netRacers)
							{
								num3 = Math.Min(num3, netRacer.progress);
							}
							if (progress > num3 && Game1.random.NextDouble() < (double)Math.Min(0.05f + (float)sabotages.Value * 0.2f, 0.5f))
							{
								tripping.Value = true;
								tripTimer = Utility.RandomFloat(1.5f, 2f);
							}
						}
					}
					else if (nextBurst > 0f)
					{
						moveSpeed = Utility.MoveTowards(moveSpeed, minMoveSpeed, 0.5f);
						nextBurst -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
						if (nextBurst <= 0f)
						{
							SpeedBurst();
							nextBurst = 0f;
						}
					}
					num2 = moveSpeed;
				}
				if (tripTimer > 0f)
				{
					tripTimer -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
					if (tripTimer < 0f)
					{
						tripTimer = 0f;
						tripping.Value = false;
					}
				}
			}
			if (jumping.Value)
			{
				num2 = ((!((segmentEnd - segmentStart).Length() / 64f > 3f)) ? 3f : 6f);
			}
			else if (tripping.Value)
			{
				num2 = 0.25f;
			}
			if (segmentStart == segmentEnd && position.Value == segmentEnd && currentTrackIndex < 0)
			{
				num2 = 0f;
			}
			while (num2 > 0f)
			{
				float num4 = Math.Min((segmentEnd - position.Value).Length(), num2);
				num2 -= num4;
				Vector2 vector = segmentEnd - position.Value;
				if (vector.X != 0f || vector.Y != 0f)
				{
					vector.Normalize();
					position.Value += vector * num4;
					value = true;
					if (Math.Abs(vector.Y) > Math.Abs(vector.X))
					{
						if (vector.Y < 0f)
						{
							direction.Value = 0;
						}
						else
						{
							direction.Value = 2;
						}
					}
					else if (vector.X < 0f)
					{
						direction.Value = 3;
					}
					else
					{
						direction.Value = 1;
					}
				}
				if (!((position.Value - segmentEnd).Length() < 0.01f))
				{
					continue;
				}
				position.Value = segmentEnd;
				if (location.currentRaceState.Value == DesertFestival.RaceState.Go && currentTrackIndex >= 0)
				{
					Vector3 trackPosition2 = location.GetTrackPosition(currentTrackIndex, horizontalPosition);
					if (trackPosition2.Z > 0f)
					{
						tripping.Value = false;
						tripTimer = 0f;
						jumping.Value = true;
					}
					else
					{
						jumping.Value = false;
					}
					float z = trackPosition2.Z;
					if (z != 2f)
					{
						if (z == 3f)
						{
							drawAboveMap.Value = false;
						}
					}
					else
					{
						drawAboveMap.Value = true;
					}
					currentTrackIndex++;
					if (currentTrackIndex >= location.raceTrack.Length)
					{
						currentTrackIndex = -2;
						segmentStart = segmentEnd;
						segmentEnd = new Vector2(44.5f, 37.5f - (float)location.finishedRacers.Count) * 64f;
						horizontalPosition = (float)(location.racerCount - 1 - location.finishedRacers.Count) / (float)(location.racerCount - 1);
						location.finishedRacers.Add(racerIndex.Value);
						if (location.finishedRacers.Count == 1)
						{
							location.announceRaceEvent.Fire("Race_Finish");
							location.OnRaceWon(racerIndex.Value);
						}
					}
					else
					{
						trackPosition2 = location.GetTrackPosition(currentTrackIndex, horizontalPosition);
						segmentStart = segmentEnd;
						segmentEnd = new Vector2(trackPosition2.X, trackPosition2.Y);
					}
					if (jumping.Value)
					{
						jumpSegmentStart.Value = segmentStart;
						jumpSegmentEnd.Value = segmentEnd;
					}
				}
				else
				{
					num2 = 0f;
					segmentStart = segmentEnd;
					if (location.currentRaceState.Value >= DesertFestival.RaceState.StartingLine && location.currentRaceState.Value < DesertFestival.RaceState.Go)
					{
						direction.Value = 0;
					}
					else
					{
						direction.Value = 3;
					}
				}
			}
			moving.Value = value;
		}
		if (!lastPosition.HasValue)
		{
			lastPosition = position.Value;
		}
		float num5 = (lastPosition.Value - position.Value).Length();
		nextFrameSwap -= num5;
		while (nextFrameSwap <= 0f)
		{
			frame = !frame;
			nextFrameSwap += 8f;
		}
		lastPosition = position.Value;
		if (!jumping.Value)
		{
			if (moving.Value)
			{
				if (tripping.Value && height == 0f)
				{
					if (_tripLeaps == 0)
					{
						gravity = 1f;
					}
					else
					{
						gravity = Utility.RandomFloat(0.5f, 0.75f);
					}
					_tripLeaps++;
				}
				else if (racerIndex.Value == 2 && height == 0f)
				{
					gravity = Utility.RandomFloat(0.25f, 0.5f);
				}
			}
			if (height != 0f || gravity != 0f)
			{
				height += gravity;
				gravity -= (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds * 2f;
				if (gravity == 0f)
				{
					gravity = -0.0001f;
				}
				if (height <= 0f)
				{
					gravity = 0f;
					height = 0f;
				}
			}
		}
		if (!tripping.Value)
		{
			_tripLeaps = 0;
		}
		if (jumping.Value)
		{
			Vector2 value2 = jumpSegmentEnd.Value - jumpSegmentStart.Value;
			float num6 = value2.Length();
			value2.Normalize();
			Vector2 value3 = position.Value - jumpSegmentStart.Value;
			float num7 = Vector2.Dot(value2, value3);
			if (num6 > 0f)
			{
				height = (float)Math.Sin((double)Utility.Clamp(num7 / num6, 0f, 1f) * Math.PI) * 48f;
			}
		}
		else if (gravity == 0f)
		{
			height = 0f;
		}
	}

	public virtual void SpeedBurst()
	{
		burstDuration = Utility.RandomFloat(0.25f, 1f);
		if (Game1.random.NextDouble() + (double)extraLuck < 0.25)
		{
			burstDuration *= 2f;
		}
		if (racerIndex.Value == 3)
		{
			burstDuration *= 0.25f;
		}
		moveSpeed = maxMoveSpeed;
	}

	public virtual void Draw(SpriteBatch sb)
	{
		float num = (position.Y + (float)racerIndex.Value * 0.1f) / 10000f;
		float num2 = Utility.Clamp(1f - height / 12f, 0f, 1f);
		sb.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, position.Value), null, Color.White * 0.75f * num2, 0f, new Vector2(Game1.shadowTexture.Width / 2, Game1.shadowTexture.Height / 2), new Vector2(3f, 3f), SpriteEffects.None, num / 10000f - 1E-07f);
		SpriteEffects effects = SpriteEffects.None;
		Rectangle value = new Rectangle(0, 0, 16, 16);
		value.Y = racerIndex.Value * 16;
		switch (direction.Value)
		{
		case 0:
			value.X = 0;
			break;
		case 2:
			value.X = 64;
			break;
		case 3:
			value.X = 32;
			effects = SpriteEffects.FlipHorizontally;
			break;
		case 1:
			value.X = 32;
			break;
		}
		if (frame)
		{
			value.X += 16;
		}
		Vector2 zero = Vector2.Zero;
		if (tripping.Value)
		{
			value.X = 96;
			zero.X += (float)Game1.random.Next(-1, 2) * 0.5f;
			zero.Y += (float)Game1.random.Next(-1, 2) * 0.5f;
		}
		sb.Draw(_texture, Game1.GlobalToLocal(position.Value + new Vector2(zero.X, 0f - height + zero.Y) * 4f), value, Color.White, 0f, new Vector2(8f, 14f), 4f, effects, num);
	}
}
