using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StardewValley.FirstPerson;

/// <summary>
/// Master coordinator for the First-Person (2.5D FPS) engine in Stardew Valley.
/// </summary>
public class FirstPersonManager : IDisposable
{
	private static FirstPersonManager _instance;
	public static FirstPersonManager Instance => _instance ??= new FirstPersonManager();

	public bool IsEnabled = false; // Player can toggle with F5

	public FPSCamera Camera { get; private set; }
	public FPSRenderer Renderer { get; private set; }
	public FPSController Controller { get; private set; }
	public GraphicsDevice GraphicsDevice { get; private set; }

	public bool IsInitialized { get; private set; } = false;

	public void Initialize(GraphicsDevice graphicsDevice)
	{
		if (IsInitialized) return;

		GraphicsDevice = graphicsDevice;
		Camera = new FPSCamera();
		Renderer = new FPSRenderer(graphicsDevice);
		Controller = new FPSController(Camera);

		IsInitialized = true;
	}

	public void Update(GameTime gameTime)
	{
		if (!IsInitialized) return;

		int width = Game1.viewport.Width;
		int height = Game1.viewport.Height;

		Controller.Update(gameTime, width, height, ref IsEnabled);
	}

	public bool ShouldOverrideWorldRender()
	{
		return IsEnabled && IsInitialized && Game1.currentLocation != null;
	}

	public void DrawWorld3D(GameTime gameTime)
	{
		if (!ShouldOverrideWorldRender()) return;

		Renderer.RenderWorld(Camera, Game1.currentLocation, gameTime);
	}

	public void DrawHUD(SpriteBatch spriteBatch, GameTime gameTime)
	{
		if (!IsEnabled || !IsInitialized) return;

		int width = Game1.uiViewport.Width;
		int height = Game1.uiViewport.Height;

		Renderer.RenderHUD(spriteBatch, gameTime, width, height);
	}

	/// <summary>
	/// Transforms player movement booleans so that W always moves in the camera's viewing direction,
	/// S moves backwards, A strafes left, and D strafes right relative to view angle.
	/// </summary>
	public void ProcessMovement(ref bool moveUpHeld, ref bool moveRightHeld, ref bool moveDownHeld, ref bool moveLeftHeld)
	{
		if (!IsEnabled || !IsInitialized || Camera == null) return;

		KeyboardState kstate = Keyboard.GetState();
		bool w = Game1.isOneOfTheseKeysDown(kstate, Game1.options.moveUpButton);
		bool s = Game1.isOneOfTheseKeysDown(kstate, Game1.options.moveDownButton);
		bool a = Game1.isOneOfTheseKeysDown(kstate, Game1.options.moveLeftButton);
		bool d = Game1.isOneOfTheseKeysDown(kstate, Game1.options.moveRightButton);

		if (!w && !s && !a && !d)
		{
			return;
		}

		// Calculate world direction vectors from Camera Yaw:
		// Forward on 2D map: X = sin(Yaw), Y = -cos(Yaw)
		// Right on 2D map: X = cos(Yaw), Y = sin(Yaw)
		float yaw = Camera.Yaw;
		Vector2 forward = new Vector2((float)Math.Sin(yaw), -(float)Math.Cos(yaw));
		Vector2 right = new Vector2((float)Math.Cos(yaw), (float)Math.Sin(yaw));

		Vector2 dir = Vector2.Zero;
		if (w) dir += forward;
		if (s) dir -= forward;
		if (d) dir += right;
		if (a) dir -= right;

		if (dir != Vector2.Zero)
		{
			dir.Normalize();
			moveUpHeld = dir.Y < -0.38f;
			moveDownHeld = dir.Y > 0.38f;
			moveLeftHeld = dir.X < -0.38f;
			moveRightHeld = dir.X > 0.38f;
		}
		else
		{
			moveUpHeld = false;
			moveRightHeld = false;
			moveDownHeld = false;
			moveLeftHeld = false;
		}
	}

	/// <summary>
	/// Captures a high-resolution screenshot directly from the backbuffer and saves it as a PNG file.
	/// </summary>
	public void CaptureScreenshot(string customName = null)
	{
		try
		{
			if (GraphicsDevice == null) return;

			int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
			int h = GraphicsDevice.PresentationParameters.BackBufferHeight;

			Color[] pixelData = new Color[w * h];
			GraphicsDevice.GetBackBufferData(pixelData);

			using Texture2D texture = new Texture2D(GraphicsDevice, w, h, false, SurfaceFormat.Color);
			texture.SetData(pixelData);

			string screenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "screenshots");
			if (!Directory.Exists(screenshotDir))
			{
				screenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
			}
			Directory.CreateDirectory(screenshotDir);

			string fileName = customName ?? $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
			string filePath = Path.Combine(screenshotDir, fileName);

			using (Stream stream = File.Create(filePath))
			{
				texture.SaveAsPng(stream, w, h);
			}

			Game1.addHUDMessage(new HUDMessage($"Screenshot salvo: {fileName}", 2));
		}
		catch (Exception ex)
		{
			Game1.log.Error("Falha ao capturar screenshot", ex);
		}
	}

	public void Dispose()
	{
		Controller?.UnlockMouse();
		Renderer?.Dispose();
		IsInitialized = false;
	}
}
