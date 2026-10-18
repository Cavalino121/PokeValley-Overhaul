using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Locations;

public class IslandForestLocation : IslandLocation
{
	protected Color _ambientLightColor = Color.White;

	private List<Wisp> _wisps;

	private List<WeatherDebris> weatherDebris;

	protected Texture2D _rayTexture;

	protected int _raySeed;

	public IslandForestLocation()
	{
	}

	public IslandForestLocation(string map, string name)
		: base(map, name)
	{
	}

	public override void tryToAddCritters(bool onlyIfOnScreen = false)
	{
	}

	protected override void resetLocalState()
	{
		_raySeed = (int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
		_rayTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\LightRays");
		_ambientLightColor = new Color(150, 120, 50);
		ignoreOutdoorLighting.Value = false;
		base.resetLocalState();
		_updateWoodsLighting();
		_wisps = new List<Wisp>();
		for (int i = 0; i < 30; i++)
		{
			Wisp item = new Wisp(i);
			_wisps.Add(item);
		}
		weatherDebris = new List<WeatherDebris>();
		int num = 192;
		int which = 3;
		for (int j = 0; j < 10; j++)
		{
			weatherDebris.Add(new WeatherDebris(new Vector2(j * num % Game1.graphics.GraphicsDevice.Viewport.Width + Game1.random.Next(num), j * num / Game1.graphics.GraphicsDevice.Viewport.Width * num % Game1.graphics.GraphicsDevice.Viewport.Height + Game1.random.Next(num)), which, (float)Game1.random.Next(15) / 500f, (float)Game1.random.Next(-10, 0) / 50f, (float)Game1.random.Next(10) / 50f));
		}
	}

	public override void cleanupBeforePlayerExit()
	{
		_wisps?.Clear();
		weatherDebris?.Clear();
		base.cleanupBeforePlayerExit();
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		_updateWoodsLighting();
		if (_wisps != null)
		{
			for (int i = 0; i < _wisps.Count; i++)
			{
				_wisps[i].Update(time);
			}
		}
		if (weatherDebris == null)
		{
			return;
		}
		foreach (WeatherDebris weatherDebri in weatherDebris)
		{
			weatherDebri.update();
		}
		Game1.updateDebrisWeatherForMovement(weatherDebris);
	}

	protected void _updateWoodsLighting()
	{
		if (Game1.currentLocation != this)
		{
			return;
		}
		int num = Utility.ConvertTimeToMinutes(Game1.getModeratelyDarkTime(this)) - 60;
		int num2 = Utility.ConvertTimeToMinutes(Game1.getTrulyDarkTime(this));
		int num3 = Utility.ConvertTimeToMinutes(Game1.getStartingToGetDarkTime(this));
		int num4 = Utility.ConvertTimeToMinutes(Game1.getModeratelyDarkTime(this));
		float num5 = (float)Utility.ConvertTimeToMinutes(Game1.timeOfDay) + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameMinute;
		float t = Utility.Clamp((num5 - (float)num) / (float)(num2 - num), 0f, 1f);
		float t2 = Utility.Clamp((num5 - (float)num3) / (float)(num4 - num3), 0f, 1f);
		Game1.ambientLight.R = (byte)Utility.Lerp((int)_ambientLightColor.R, (int)Game1.eveningColor.R, t);
		Game1.ambientLight.G = (byte)Utility.Lerp((int)_ambientLightColor.G, (int)Game1.eveningColor.G, t);
		Game1.ambientLight.B = (byte)Utility.Lerp((int)_ambientLightColor.B, (int)Game1.eveningColor.B, t);
		Game1.ambientLight.A = (byte)Utility.Lerp((int)_ambientLightColor.A, (int)Game1.eveningColor.A, t);
		Color black = Color.Black;
		black.A = (byte)Utility.Lerp(255f, 0f, t2);
		foreach (LightSource value in Game1.currentLightSources.Values)
		{
			if (value.lightContext.Value == LightSource.LightContext.MapLight)
			{
				value.color.Value = black;
			}
		}
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		if (_wisps != null)
		{
			for (int i = 0; i < _wisps.Count; i++)
			{
				_wisps[i].Draw(b);
			}
		}
	}

	public virtual void DrawRays(SpriteBatch b)
	{
		Random random = Utility.CreateRandom(_raySeed);
		float num = (float)Game1.graphics.GraphicsDevice.Viewport.Height * 0.6f / 128f;
		int num2 = -(int)(128f / num);
		int num3 = Game1.graphics.GraphicsDevice.Viewport.Width / (int)(32f * num);
		for (int i = num2; i < num3; i++)
		{
			Color white = Color.White;
			float num4 = (float)Game1.viewport.X * Utility.RandomFloat(0.75f, 1f, random) + (float)Game1.viewport.Y * Utility.RandomFloat(0.2f, 0.5f, random) + (float)Game1.currentGameTime.TotalGameTime.TotalSeconds * 20f;
			num4 %= 360f;
			float num5 = num4 * ((float)Math.PI / 180f);
			white *= Utility.Clamp((float)Math.Sin(num5), 0f, 1f) * Utility.RandomFloat(0.15f, 0.4f, random);
			float num6 = Utility.Lerp(0f - Utility.RandomFloat(24f, 32f, random), 0f, num4 / 360f);
			b.Draw(_rayTexture, new Vector2(((float)(i * 32) - num6) * num, Utility.RandomFloat(0f, -32f * num, random)), new Rectangle(128 * random.Next(0, 2), 0, 128, 128), white, 0f, Vector2.Zero, num, SpriteEffects.None, 1f);
		}
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		base.drawAboveAlwaysFrontLayer(b);
		DrawRays(b);
	}
}
