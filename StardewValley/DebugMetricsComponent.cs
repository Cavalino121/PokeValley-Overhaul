using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Network;

namespace StardewValley;

public class DebugMetricsComponent : DrawableGameComponent
{
	private readonly Game _game;

	private SpriteFont _font;

	private SpriteBatch _spriteBatch;

	private int _drawX;

	private int _drawY;

	private double _fps;

	private double _mspf;

	private int _lastCollection;

	private float _lastBaseMB;

	private bool _runningSlowly;

	private StringBuilder _stringBuilder = new StringBuilder(512);

	private Texture2D _opaqueWhite;

	public int XOffset = 10;

	public int YOffset = 10;

	private IBandwidthMonitor bandwidthMonitor;

	private BarGraph bandwidthUpGraph;

	private BarGraph bandwidthDownGraph;

	public SpriteFont Font
	{
		get
		{
			return _font;
		}
		set
		{
			_font = value;
		}
	}

	public DebugMetricsComponent(Game game)
		: base(game)
	{
		_game = game;
		base.DrawOrder = int.MaxValue;
	}

	protected override void LoadContent()
	{
		_spriteBatch = new SpriteBatch(base.GraphicsDevice);
		int num = 2;
		int num2 = 2;
		_opaqueWhite = new Texture2D(base.GraphicsDevice, num, num2, mipmap: false, SurfaceFormat.Color)
		{
			Name = "@DebugMetricsComponent._opaqueWhite"
		};
		Color[] array = new Color[num * num2];
		_opaqueWhite.GetData(array);
		for (int i = 0; i < num * num2; i++)
		{
			array[i] = Color.White;
		}
		_opaqueWhite.SetData(array);
		base.LoadContent();
	}

	public override void Update(GameTime gameTime)
	{
		if (Game1.IsServer)
		{
			bandwidthMonitor = Game1.server;
		}
		else if (Game1.IsClient)
		{
			bandwidthMonitor = Game1.client;
		}
		else
		{
			bandwidthMonitor = null;
		}
		if (bandwidthMonitor == null || !bandwidthMonitor.LogBandwidth)
		{
			bandwidthDownGraph = null;
			bandwidthUpGraph = null;
		}
		if (bandwidthMonitor != null && bandwidthMonitor.LogBandwidth && (bandwidthDownGraph == null || bandwidthUpGraph == null))
		{
			int num = 200;
			int num2 = 150;
			int num3 = 50;
			bandwidthUpGraph = new BarGraph(bandwidthMonitor.BandwidthLogger.LoggedAvgBitsUp, Game1.uiViewport.Width - num - num3, num3, num, num2, 2, BarGraph.DYNAMIC_SCALE_MAX, Color.Yellow * 0.8f, _opaqueWhite);
			bandwidthDownGraph = new BarGraph(bandwidthMonitor.BandwidthLogger.LoggedAvgBitsDown, Game1.uiViewport.Width - num - num3, num3 + num2 + num3, num, num2, 2, BarGraph.DYNAMIC_SCALE_MAX, Color.Cyan * 0.8f, _opaqueWhite);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (!Game1.displayHUD || !Game1.debugMode)
		{
			return;
		}
		if (gameTime.ElapsedGameTime.TotalSeconds > 0.0)
		{
			_fps = 1.0 / gameTime.ElapsedGameTime.TotalSeconds;
			_mspf = gameTime.ElapsedGameTime.TotalSeconds * 1000.0;
		}
		if (gameTime.IsRunningSlowly)
		{
			_runningSlowly = true;
		}
		if (_font == null)
		{
			return;
		}
		_spriteBatch.Begin();
		_drawX = XOffset;
		_drawY = YOffset;
		StringBuilder stringBuilder = _stringBuilder;
		Utility.makeSafe(ref _drawX, ref _drawY, 0, 0);
		int num = GC.CollectionCount(0);
		float num2 = (float)GC.GetTotalMemory(forceFullCollection: false) / 1048576f;
		if (_lastCollection != num)
		{
			_lastCollection = num;
			_lastBaseMB = num2;
		}
		float num3 = num2 - _lastBaseMB;
		stringBuilder.AppendFormatEx("FPS: {0,3}   GC: {1,3}   {2:0.00}MB   +{3:0.00}MB", (int)Math.Round(_fps), _lastCollection % 1000, _lastBaseMB, num3);
		Color color = Color.Yellow;
		if (_runningSlowly)
		{
			stringBuilder.Append("   [IsRunningSlowly]");
			_runningSlowly = false;
			color = Color.Red;
		}
		DrawLine(color, stringBuilder, _drawX);
		if (Game1.IsMultiplayer)
		{
			color = Color.Yellow;
			if (Game1.IsServer)
			{
				foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
				{
					stringBuilder.AppendFormat("Ping({0}): {1:0.0}ms", otherFarmer.Value.Name, Game1.server.getPingToClient(otherFarmer.Key));
					DrawLine(color, stringBuilder, _drawX);
				}
			}
			else
			{
				stringBuilder.AppendFormat("Ping: {0:0.0}ms", Game1.client.GetPingToHost());
				DrawLine(color, stringBuilder, _drawX);
			}
		}
		if (bandwidthMonitor != null && bandwidthMonitor.LogBandwidth)
		{
			stringBuilder.AppendFormat("Up - b/s: {0}  Avg b/s: {1}", (int)bandwidthMonitor.BandwidthLogger.BitsUpPerSecond, (int)bandwidthMonitor.BandwidthLogger.AvgBitsUpPerSecond);
			DrawLine(color, stringBuilder, _drawX);
			stringBuilder.AppendFormat("Down - b/s: {0}  Avg b/s: {1}", (int)bandwidthMonitor.BandwidthLogger.BitsDownPerSecond, (int)bandwidthMonitor.BandwidthLogger.AvgBitsDownPerSecond);
			DrawLine(color, stringBuilder, _drawX);
			stringBuilder.AppendFormat("Total MB Up: {0:0.00}  Total MB Down: {1:0.00}  Total Seconds: {2:0.00}", (float)bandwidthMonitor.BandwidthLogger.TotalBitsUp / 8f / 1000f / 1000f, (float)bandwidthMonitor.BandwidthLogger.TotalBitsDown / 8f / 1000f / 1000f, (float)bandwidthMonitor.BandwidthLogger.TotalMs / 1000f);
			DrawLine(color, stringBuilder, _drawX);
			if (bandwidthUpGraph != null && bandwidthDownGraph != null)
			{
				bandwidthUpGraph.Draw(_spriteBatch);
				bandwidthDownGraph.Draw(_spriteBatch);
			}
		}
		_spriteBatch.End();
	}

	private void DrawLine(Color color, StringBuilder sb, int x)
	{
		if (sb != null)
		{
			Vector2 vector = _font.MeasureString(sb);
			int drawY = _drawY;
			int lineSpacing = _font.LineSpacing;
			lineSpacing -= lineSpacing / 10;
			_spriteBatch.Draw(_opaqueWhite, new Rectangle(x - 1, drawY, (int)vector.X + 2, lineSpacing), null, Color.Black * 0.5f);
			_spriteBatch.DrawString(_font, sb, new Vector2(x, drawY), color);
			_drawY += lineSpacing;
			sb.Clear();
		}
	}
}
