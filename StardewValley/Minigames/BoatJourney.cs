using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Extensions;
using StardewValley.GameData;

namespace StardewValley.Minigames;

public class BoatJourney : IMinigame
{
	public class WaterSparkle : Entity
	{
		protected Vector2 _startPosition;

		public WaterSparkle(BoatJourney context)
			: base(context, GetAssetName(), new Rectangle(647, 524, 1, 1), new Vector2(0f, 0f), new Vector2(0f, 0f))
		{
			currentFrame = Game1.random.Next(0, 7);
			numFrames = 7;
			frameInterval = 0.1f;
			_startPosition = position;
			RandomizePosition();
		}

		public void RandomizePosition()
		{
			Rectangle r = new Rectangle(0, 112, 640, 528);
			do
			{
				_startPosition = (position = Utility.getRandomPositionInThisRectangle(r, Game1.random));
			}
			while (new Rectangle(508, 11, 125, 138).Contains((int)_startPosition.X, (int)_startPosition.Y));
			velocity.X = Utility.RandomFloat(-0.1f, 0.1f);
		}

		public override void OnAnimationFinished()
		{
			RandomizePosition();
			base.OnAnimationFinished();
		}

		public override float GetLayerDepth()
		{
			if (layerDepth >= 0f)
			{
				return layerDepth;
			}
			return 0.0001f;
		}
	}

	public class Wave : Entity
	{
		protected Vector2 _startPosition;

		public Wave(BoatJourney context, Vector2 position = default(Vector2))
			: base(context, GetAssetName(), new Rectangle(640, 506, 32, 12), new Vector2(16f, 6f), position)
		{
			numFrames = 2;
			frameInterval = 1.25f;
			_startPosition = position;
		}

		public override bool Update(GameTime time)
		{
			position = _startPosition + new Vector2(1f, 0f) * (float)Math.Sin(_startPosition.X * 0.333f + _startPosition.Y * 0.1f + _age) * 3f;
			return base.Update(time);
		}

		public override float GetLayerDepth()
		{
			if (layerDepth >= 0f)
			{
				return layerDepth;
			}
			return 0.0003f;
		}
	}

	public class Boat : Entity
	{
		protected float nextSmokeStackSmoke;

		protected float nextRipple;

		public Vector2? smokeStack;

		public Vector2 _lastPosition;

		public float idleAnimationInterval = 0.75f;

		public float moveAnimationInterval = 0.25f;

		public Boat(BoatJourney context, string texture_path, Rectangle source_rect, Vector2 origin = default(Vector2), Vector2 position = default(Vector2))
			: base(context, texture_path, source_rect, origin, position)
		{
		}

		public override bool Update(GameTime time)
		{
			bool flag = false;
			if (_lastPosition != position)
			{
				_lastPosition = position;
				flag = true;
			}
			if (flag)
			{
				frameInterval = moveAnimationInterval;
			}
			else
			{
				frameInterval = idleAnimationInterval;
			}
			if (smokeStack.HasValue)
			{
				if (nextSmokeStackSmoke <= 0f)
				{
					nextSmokeStackSmoke = 0.25f;
					if (flag)
					{
						Entity entity = new Entity(_context, GetAssetName(), new Rectangle(689, 337, 2, 2), new Vector2(1f, 1f), position + smokeStack.Value);
						entity.numFrames = 3;
						Vector2 vector = new Vector2(Utility.RandomFloat(-0.04f, -0.03f), Utility.RandomFloat(-0.05f, -0.1f));
						entity.velocity = vector;
						entity.destroyAfterAnimation = true;
						_context.entities.Add(entity);
					}
				}
				else
				{
					nextSmokeStackSmoke -= (float)time.ElapsedGameTime.TotalSeconds;
				}
			}
			if (nextRipple <= 0f)
			{
				nextRipple = 0.25f;
				if (flag)
				{
					Entity entity2 = new Entity(_context, GetAssetName(), new Rectangle(640, 336, 9, 16), new Vector2(4f, 0f), position + new Vector2(0f, 0f));
					entity2.numFrames = 5;
					entity2.layerDepth = 2E-05f;
					entity2.destroyAfterAnimation = true;
					_context.entities.Add(entity2);
				}
			}
			else
			{
				nextRipple -= (float)time.ElapsedGameTime.TotalSeconds;
			}
			return base.Update(time);
		}
	}

	public class Entity
	{
		protected BoatJourney _context;

		public Vector2 position;

		protected Texture2D _texture;

		protected Rectangle _sourceRect;

		protected float lifeTime;

		protected float _age;

		public Vector2 velocity;

		public Vector2 origin;

		public bool flipX;

		protected float _frameTime;

		public float frameInterval = 0.25f;

		public int currentFrame;

		public int numFrames = 1;

		public int columns;

		public bool destroyAfterAnimation;

		public bool drawOnTop;

		public float layerDepth = -1f;

		public Entity(BoatJourney context, string texture_path, Rectangle source_rect, Vector2 origin = default(Vector2), Vector2 position = default(Vector2))
		{
			_context = context;
			_texture = Game1.temporaryContent.Load<Texture2D>(texture_path);
			_sourceRect = source_rect;
			this.origin = origin;
			this.position = position;
		}

		public virtual bool Update(GameTime time)
		{
			_age += (float)time.ElapsedGameTime.TotalSeconds;
			_frameTime += (float)time.ElapsedGameTime.TotalSeconds;
			if (lifeTime > 0f && lifeTime >= _age)
			{
				return true;
			}
			if (frameInterval > 0f && _frameTime > frameInterval)
			{
				_frameTime -= frameInterval;
				currentFrame++;
				if (currentFrame >= numFrames)
				{
					OnAnimationFinished();
					currentFrame -= numFrames;
					if (destroyAfterAnimation)
					{
						return true;
					}
				}
			}
			position += velocity;
			return false;
		}

		public virtual void OnAnimationFinished()
		{
		}

		public virtual void SetSourceRect(Rectangle rectangle)
		{
			_sourceRect = rectangle;
		}

		public virtual Rectangle GetSourceRect()
		{
			int num = currentFrame;
			int num2 = 0;
			if (columns > 0)
			{
				num2 = num / columns;
				num %= columns;
			}
			return new Rectangle(_sourceRect.X + num * _sourceRect.Width, _sourceRect.Y + num2 * _sourceRect.Width, _sourceRect.Width, _sourceRect.Height);
		}

		public virtual float GetLayerDepth()
		{
			if (layerDepth >= 0f)
			{
				return layerDepth;
			}
			return position.Y / 100000f;
		}

		public virtual void Draw(SpriteBatch b)
		{
			b.Draw(_texture, _context.TransformDraw(position), GetSourceRect(), Color.White, 0f, origin, _context._zoomLevel, flipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth());
		}
	}

	public float _age;

	public Texture2D texture;

	public Rectangle mapSourceRectangle;

	protected float _zoomLevel = 1f;

	protected Vector2 viewTarget = new Vector2(0f, 0f);

	protected Vector2 _upperLeft;

	public List<Entity> entities;

	protected float _currentBoatSpeed;

	public float boatSpeed = 0.5f;

	public float dockSpeed = 0.1f;

	protected float _nextSlosh;

	protected bool _fadeComplete;

	public Vector2[] points = new Vector2[9]
	{
		new Vector2(286f, 53f),
		new Vector2(286f, 60f),
		new Vector2(287f, 88f),
		new Vector2(340f, 121f),
		new Vector2(357f, 215f),
		new Vector2(204f, 633f),
		new Vector2(274f, 750f),
		new Vector2(352f, 720f),
		new Vector2(352f, 700f)
	};

	protected List<Vector2> _interpolatedPoints;

	protected List<float> _cumulativeDistances;

	protected float _totalPathDistance;

	protected float traveledBoatDistance;

	protected float nextSmoke;

	public float departureDelay = 1.5f;

	protected Boat _boat;

	protected List<Entity> _seagulls = new List<Entity>();

	public BoatJourney()
	{
		Game1.globalFadeToClear();
		Game1.changeMusicTrack("sweet", track_interruptable: false, MusicContext.MiniGame);
		mapSourceRectangle = new Rectangle(0, 0, 640, 849);
		texture = Game1.temporaryContent.Load<Texture2D>(GetAssetName());
		changeScreenSize();
		Rectangle r = new Rectangle(0, 112, 640, 528);
		_interpolatedPoints = new List<Vector2>();
		_cumulativeDistances = new List<float>();
		_interpolatedPoints.Add(points[0]);
		for (int i = 0; i < points.Length - 3; i++)
		{
			_interpolatedPoints.Add(points[i + 1]);
			for (int j = 0; j < 10; j++)
			{
				Vector2 item = Vector2.CatmullRom(points[i], points[i + 1], points[i + 2], points[i + 3], (float)j / 10f);
				_interpolatedPoints.Add(item);
			}
			_interpolatedPoints.Add(points[i + 2]);
		}
		_interpolatedPoints.Add(points[points.Length - 1]);
		Vector2 vector = _interpolatedPoints[0];
		_totalPathDistance = 0f;
		for (int k = 0; k < _interpolatedPoints.Count; k++)
		{
			_totalPathDistance += (vector - _interpolatedPoints[k]).Length();
			vector = _interpolatedPoints[k];
			_cumulativeDistances.Add(_totalPathDistance);
		}
		entities = new List<Entity>();
		for (int l = 0; l < 8; l++)
		{
			Vector2 randomPositionInThisRectangle = Utility.getRandomPositionInThisRectangle(r, Game1.random);
			Rectangle source_rect = new Rectangle(640, 0, 150, 130);
			if (Game1.random.NextDouble() < 0.44999998807907104)
			{
				source_rect = new Rectangle(640, 136, 150, 120);
			}
			else if (Game1.random.NextDouble() < 0.25)
			{
				source_rect = new Rectangle(640, 256, 150, 80);
			}
			Entity item2 = new Entity(this, GetAssetName(), source_rect, new Vector2(source_rect.Width / 2, source_rect.Height), randomPositionInThisRectangle)
			{
				velocity = new Vector2(-1f, -1f) * Utility.RandomFloat(0.05f, 0.15f),
				drawOnTop = true
			};
			entities.Add(item2);
		}
		List<Vector2> other_boat_positions = new List<Vector2>();
		for (int m = 0; m < 2; m++)
		{
			if (Game1.random.NextDouble() < 0.30000001192092896)
			{
				SpawnBoat(new Rectangle(640, 416, 32, 32), new Vector2(-1f, 0f), other_boat_positions);
			}
		}
		if (Game1.random.NextDouble() < 0.20000000298023224)
		{
			SpawnBoat(new Rectangle(704, 416, 32, 32), new Vector2(-1f, 0f), other_boat_positions);
		}
		for (int n = 0; n < 2; n++)
		{
			if (Game1.random.NextDouble() < 0.30000001192092896)
			{
				SpawnBoat(new Rectangle(640, 448, 32, 32), new Vector2(1f, 0f), other_boat_positions);
			}
		}
		for (int num = 0; num < 16; num++)
		{
			Vector2 randomPositionInThisRectangle2 = Utility.getRandomPositionInThisRectangle(r, Game1.random);
			Wave item3 = new Wave(this, randomPositionInThisRectangle2);
			entities.Add(item3);
		}
		for (int num2 = 0; num2 < 8; num2++)
		{
			WaterSparkle item4 = new WaterSparkle(this);
			entities.Add(item4);
		}
		Vector2 randomPositionInThisRectangle3 = Utility.getRandomPositionInThisRectangle(r, Game1.random);
		CreateFlockOfSeagulls((int)randomPositionInThisRectangle3.X, (int)randomPositionInThisRectangle3.Y, Game1.random.Next(4, 8));
		for (int num3 = 0; num3 < 3; num3++)
		{
			randomPositionInThisRectangle3 = Utility.getRandomPositionInThisRectangle(r, Game1.random);
			CreateFlockOfSeagulls((int)randomPositionInThisRectangle3.X, (int)randomPositionInThisRectangle3.Y, 1);
		}
		_seagulls.Sort((Entity a, Entity b) => a.position.Y.CompareTo(b.position.Y));
		_boat = new Boat(this, GetAssetName(), new Rectangle(640, 352, 32, 32), new Vector2(16f, 16f), new Vector2(293f, 53f));
		_boat.smokeStack = new Vector2(0f, -12f);
		_boat.numFrames = 2;
		entities.Add(_boat);
		Entity item5 = new Entity(this, GetAssetName(), new Rectangle(643, 538, 29, 17), Vector2.Zero, new Vector2(16f, 829f))
		{
			numFrames = 2,
			frameInterval = 0.75f
		};
		entities.Add(item5);
	}

	/// <summary>Get the asset name for the main boat journey texture.</summary>
	private static string GetAssetName()
	{
		return "Minigames\\" + Game1.currentSeason + "_boatJourneyMap";
	}

	public void SpawnBoat(Rectangle boat_sprite_rect, Vector2 direction, List<Vector2> other_boat_positions)
	{
		Vector2 vector;
		while (true)
		{
			vector = Game1.random.ChooseFrom(_interpolatedPoints);
			if (!new Rectangle(0, 112, 640, 528).Contains((int)vector.X, (int)vector.Y))
			{
				continue;
			}
			vector += direction * Utility.RandomFloat(8f, 64f);
			bool flag = false;
			foreach (Vector2 other_boat_position in other_boat_positions)
			{
				if ((other_boat_position - vector).Length() < 24f)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
		}
		Boat boat = new Boat(this, GetAssetName(), boat_sprite_rect, new Vector2(16f, 14f), vector);
		boat.velocity = direction * Utility.RandomFloat(0.05f, 0.1f);
		boat.numFrames = 2;
		boat.frameInterval = 0.75f;
		other_boat_positions.Add(vector);
		entities.Add(boat);
	}

	public void CreateFlockOfSeagulls(int x, int y, int depth)
	{
		Vector2 vector = new Vector2(-0.15f, -0.25f);
		Entity entity = new Entity(this, GetAssetName(), new Rectangle(646, 560, 5, 14), new Vector2(2f, 14f), new Vector2(x, y));
		entity.numFrames = 8;
		entity.currentFrame = Game1.random.Next(0, 8);
		entity.velocity = vector + new Vector2(Utility.RandomFloat(-0.001f, 0.001f), Utility.RandomFloat(-0.001f, 0.001f));
		entity.frameInterval = Utility.RandomFloat(0.1f, 0.15f);
		entities.Add(entity);
		_seagulls.Add(entity);
		Vector2 position = new Vector2(x, y);
		Vector2 position2 = new Vector2(x, y);
		for (int i = 1; i < depth; i++)
		{
			position.X -= Game1.random.Next(5, 8);
			position.Y += Game1.random.Next(6, 9);
			position2.X += Game1.random.Next(5, 8);
			position2.Y += Game1.random.Next(6, 9);
			entity = new Entity(this, GetAssetName(), new Rectangle(646, 560, 5, 14), new Vector2(2f, 14f), position);
			entity.numFrames = 8;
			entity.currentFrame = Game1.random.Next(0, 8);
			entity.velocity = vector + new Vector2(Utility.RandomFloat(-0.001f, 0.001f), Utility.RandomFloat(-0.001f, 0.001f));
			entity.frameInterval = Utility.RandomFloat(0.1f, 0.15f);
			entities.Add(entity);
			_seagulls.Add(entity);
			entity = new Entity(this, GetAssetName(), new Rectangle(646, 560, 5, 14), new Vector2(2f, 14f), position2);
			entity.numFrames = 8;
			entity.currentFrame = Game1.random.Next(0, 8);
			entity.velocity = vector + new Vector2(Utility.RandomFloat(-0.001f, 0.001f), Utility.RandomFloat(-0.001f, 0.001f));
			entity.frameInterval = Utility.RandomFloat(0.1f, 0.15f);
			entities.Add(entity);
			_seagulls.Add(entity);
		}
	}

	public Vector2 TransformDraw(Vector2 position)
	{
		position.X = (int)(position.X * _zoomLevel) - (int)_upperLeft.X;
		position.Y = (int)(position.Y * _zoomLevel) - (int)_upperLeft.Y;
		return position;
	}

	public Rectangle TransformDraw(Rectangle dest)
	{
		dest.X = (int)((float)dest.X * _zoomLevel) - (int)_upperLeft.X;
		dest.Y = (int)((float)dest.Y * _zoomLevel) - (int)_upperLeft.Y;
		dest.Width = (int)((float)dest.Width * _zoomLevel);
		dest.Height = (int)((float)dest.Height * _zoomLevel);
		return dest;
	}

	public bool tick(GameTime time)
	{
		if (_fadeComplete)
		{
			Game1.warpFarmer("IslandSouth", 21, 43, 0);
			return true;
		}
		_age += (float)time.ElapsedGameTime.TotalSeconds;
		entities.RemoveAll((Entity entity2) => entity2.Update(time));
		viewTarget.X = _boat.position.X;
		viewTarget.Y = _boat.position.Y;
		List<Entity> seagulls = _seagulls;
		if (seagulls != null && seagulls.Count > 0 && _boat.position.Y > _seagulls[0].position.Y)
		{
			if (Math.Abs(_boat.position.X - _seagulls[0].position.X) < 128f && Game1.random.NextDouble() < 0.25)
			{
				Game1.playSound("seagulls");
			}
			_seagulls.RemoveAt(0);
		}
		if (_interpolatedPoints.Count > 1)
		{
			if (departureDelay > 0f)
			{
				departureDelay -= (float)time.ElapsedGameTime.TotalSeconds;
			}
			else
			{
				if (traveledBoatDistance < _totalPathDistance)
				{
					float to = boatSpeed;
					if (_interpolatedPoints.Count <= 2)
					{
						to = dockSpeed;
					}
					_currentBoatSpeed = Utility.MoveTowards(_currentBoatSpeed, to, 0.01f);
					traveledBoatDistance += _currentBoatSpeed;
					if (traveledBoatDistance > _totalPathDistance)
					{
						traveledBoatDistance = _totalPathDistance;
					}
				}
				_nextSlosh -= (float)time.ElapsedGameTime.TotalSeconds;
				if (_nextSlosh <= 0f)
				{
					_nextSlosh = 0.75f;
					Game1.playSound("waterSlosh");
				}
			}
			while (_interpolatedPoints.Count >= 2 && traveledBoatDistance >= _cumulativeDistances[1])
			{
				_interpolatedPoints.RemoveAt(0);
				_cumulativeDistances.RemoveAt(0);
			}
			if (_interpolatedPoints.Count <= 1)
			{
				_interpolatedPoints.Clear();
				_cumulativeDistances.Clear();
				Game1.globalFadeToBlack(delegate
				{
					_fadeComplete = true;
				});
			}
			else
			{
				Vector2 vector = _interpolatedPoints[1] - _interpolatedPoints[0];
				if (Math.Abs(vector.X) > Math.Abs(vector.Y))
				{
					if (vector.X < 0f)
					{
						_boat.SetSourceRect(new Rectangle(704, 384, 32, 32));
					}
					else
					{
						_boat.SetSourceRect(new Rectangle(704, 352, 32, 32));
					}
				}
				else if (vector.Y > 0f)
				{
					_boat.SetSourceRect(new Rectangle(640, 384, 32, 32));
				}
				else
				{
					_boat.SetSourceRect(new Rectangle(640, 352, 32, 32));
				}
				float t = (traveledBoatDistance - _cumulativeDistances[0]) / (_cumulativeDistances[1] - _cumulativeDistances[0]);
				_boat.position = new Vector2(Utility.Lerp(_interpolatedPoints[0].X, _interpolatedPoints[1].X, t), Utility.Lerp(_interpolatedPoints[0].Y, _interpolatedPoints[1].Y, t));
			}
		}
		_upperLeft.X = viewTarget.X * _zoomLevel - (float)(Game1.viewport.Width / 2);
		_upperLeft.Y = viewTarget.Y * _zoomLevel - (float)(Game1.viewport.Height / 2);
		if (_upperLeft.Y < 0f)
		{
			_upperLeft.Y = 0f;
		}
		if (_upperLeft.Y + (float)Game1.viewport.Height > (float)mapSourceRectangle.Height * _zoomLevel)
		{
			_upperLeft.Y = (float)mapSourceRectangle.Height * _zoomLevel - (float)Game1.viewport.Height;
		}
		if (nextSmoke <= 0f)
		{
			nextSmoke = 0.75f;
			Entity entity = new Entity(this, GetAssetName(), new Rectangle(640, 480, 16, 16), new Vector2(8f, 8f), new Vector2(350f, 665f));
			entity.numFrames = 7;
			Vector2 velocity = new Vector2(Utility.RandomFloat(-0.04f, -0.03f), Utility.RandomFloat(-0.1f, -0.2f));
			entity.velocity = velocity;
			entity.destroyAfterAnimation = true;
			entities.Add(entity);
		}
		else
		{
			nextSmoke -= (float)time.ElapsedGameTime.TotalSeconds;
		}
		return false;
	}

	public void afterFade()
	{
		Game1.currentMinigame = null;
		Game1.globalFadeToClear();
		if (Game1.currentLocation.currentEvent != null)
		{
			Game1.currentLocation.currentEvent.CurrentCommand++;
			Game1.currentLocation.temporarySprites.Clear();
		}
	}

	public bool forceQuit()
	{
		return false;
	}

	public void receiveLeftClick(int x, int y, bool playSound = true)
	{
	}

	public void leftClickHeld(int x, int y)
	{
	}

	public void receiveRightClick(int x, int y, bool playSound = true)
	{
	}

	public void releaseLeftClick(int x, int y)
	{
	}

	public void releaseRightClick(int x, int y)
	{
	}

	public void receiveKeyPress(Keys k)
	{
		if (k == Keys.Escape)
		{
			forceQuit();
		}
	}

	public void receiveKeyRelease(Keys k)
	{
	}

	public Color getWaterColorForSeason()
	{
		return Game1.season switch
		{
			Season.Summer => new Color(51, 90, 174), 
			Season.Fall => new Color(56, 70, 128), 
			Season.Winter => new Color(43, 74, 164), 
			_ => new Color(49, 79, 155), 
		};
	}

	public void draw(SpriteBatch b)
	{
		b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
		b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), null, getWaterColorForSeason(), 0f, Vector2.Zero, SpriteEffects.None, 0f);
		b.Draw(Game1.staminaRect, TransformDraw(new Rectangle(-Game1.viewport.Width, 400, Game1.viewport.Width * 3, Game1.viewport.Height)), null, new Color(49, 79, 155), 0f, Vector2.Zero, SpriteEffects.None, 5E-06f);
		b.Draw(texture, TransformDraw(mapSourceRectangle), mapSourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1E-05f);
		b.Draw(texture, TransformDraw(new Rectangle(-640, 331, 640, 294)), new Rectangle(0, 337, 640, 294), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1E-05f);
		b.Draw(texture, TransformDraw(new Rectangle(640, 343, 640, 294)), new Rectangle(0, 337, 640, 294), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1E-05f);
		for (int i = 0; i < entities.Count; i++)
		{
			if (!entities[i].drawOnTop)
			{
				entities[i].Draw(b);
			}
		}
		b.End();
		b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
		for (int j = 0; j < entities.Count; j++)
		{
			if (entities[j].drawOnTop)
			{
				entities[j].Draw(b);
			}
		}
		b.End();
	}

	public void changeScreenSize()
	{
		_zoomLevel = 4f;
		if ((float)mapSourceRectangle.Height * _zoomLevel < (float)Game1.viewport.Height)
		{
			_zoomLevel = (float)Game1.viewport.Height / (float)mapSourceRectangle.Height;
		}
	}

	public void unload()
	{
		Game1.stopMusicTrack(MusicContext.MiniGame);
	}

	public void receiveEventPoke(int data)
	{
		throw new NotImplementedException();
	}

	public string minigameId()
	{
		return null;
	}

	public bool doMainGameUpdates()
	{
		return false;
	}

	public bool overrideFreeMouseMovement()
	{
		return Game1.options.SnappyMenus;
	}
}
