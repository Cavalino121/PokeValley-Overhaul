using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace StardewValley.FirstPerson;

/// <summary>
/// Handles input and controls for First-Person perspective:
/// Mouse look (Pitch/Yaw), WASD direction alignment, mouse capture management, and tile targeting.
/// </summary>
public class FPSController
{
	private readonly FPSCamera _camera;
	private bool _wasToggleKeyPressed = false;
	private bool _wasScreenshotKeyPressed = false;
	private bool _isMouseLocked = false;
	public bool IsMouseLocked => _isMouseLocked;

	public float MouseSensitivity = 0.0035f;

	public FPSController(FPSCamera camera)
	{
		_camera = camera;
	}

	public void Update(GameTime gameTime, int screenWidth, int screenHeight, ref bool isFirstPersonActive)
	{
		KeyboardState keyState = Keyboard.GetState();

		// 1. Toggle FPS Mode with F5 key
		if (keyState.IsKeyDown(Keys.F5))
		{
			if (!_wasToggleKeyPressed)
			{
				isFirstPersonActive = !isFirstPersonActive;
				_wasToggleKeyPressed = true;
				if (!isFirstPersonActive)
				{
					UnlockMouse();
				}
				Game1.addHUDMessage(new HUDMessage(isFirstPersonActive ? "Modo Primeira Pessoa [3D]: ATIVADO" : "Modo Primeira Pessoa: DESATIVADO", 2));
			}
		}
		else
		{
			_wasToggleKeyPressed = false;
		}

		// 2. F12: Instant screenshot capture for visual verification
		if (keyState.IsKeyDown(Keys.F12))
		{
			if (!_wasScreenshotKeyPressed)
			{
				_wasScreenshotKeyPressed = true;
				FirstPersonManager.Instance.CaptureScreenshot();
			}
		}
		else
		{
			_wasScreenshotKeyPressed = false;
		}

		if (!isFirstPersonActive || Game1.player == null)
		{
			UnlockMouse();
			return;
		}

		// 3. Check if mouse should be locked:
		// Only lock if window is focused/active AND no menus, dialogue, chat, or cutscenes are open
		bool shouldLock = Game1.game1 != null
			&& Game1.game1.IsActive
			&& Game1.activeClickableMenu == null
			&& Game1.CurrentEvent == null
			&& !Game1.dialogueUp
			&& (Game1.chatBox == null || !Game1.chatBox.isActive())
			&& !Game1.eventUp;

		if (!shouldLock)
		{
			UnlockMouse();
			return;
		}

		// 4. Capture mouse for FPS looking
		Point screenCenter = new Point(screenWidth / 2, screenHeight / 2);
		MouseState mouseState = Mouse.GetState();

		if (!_isMouseLocked)
		{
			_isMouseLocked = true;
			Game1.mouseCursorTransparency = 0f; // Hide 2D mouse cursor while in FPS look
			Mouse.SetPosition(screenCenter.X, screenCenter.Y);
			return;
		}

		int deltaX = mouseState.X - screenCenter.X;
		int deltaY = mouseState.Y - screenCenter.Y;

		if (deltaX != 0 || deltaY != 0)
		{
			_camera.Yaw += deltaX * MouseSensitivity;
			_camera.Pitch -= deltaY * MouseSensitivity; // Inverted Y: moving mouse up looks up

			// Reset mouse back to center of window
			Mouse.SetPosition(screenCenter.X, screenCenter.Y);
		}

		// Update Camera matrices
		_camera.Update(Game1.player.Position, screenWidth, screenHeight);

		// Synchronize farmer's facing direction to match camera view angle
		SyncFarmerFacingDirection();

		// Direct raycast to synchronize cursor tile with camera crosshair
		if (_camera.TryGetTargetGroundTile(350f, out Point targetTile))
		{
			Game1.currentCursorTile = new Vector2(targetTile.X, targetTile.Y);
		}
	}

	public void UnlockMouse()
	{
		if (_isMouseLocked)
		{
			_isMouseLocked = false;
			Game1.mouseCursorTransparency = 1f; // Restore 2D cursor visibility
		}
	}

	private void SyncFarmerFacingDirection()
	{
		float yaw = _camera.Yaw;

		// Cardinal directions in radians:
		// 0 or 2*PI = North (facing Up = 0)
		// PI/2 = East (facing Right = 1)
		// PI = South (facing Down = 2)
		// 3*PI/2 = West (facing Left = 3)
		if (yaw >= 7f * MathHelper.PiOver4 || yaw < MathHelper.PiOver4)
		{
			Game1.player.FacingDirection = 0; // Up
		}
		else if (yaw >= MathHelper.PiOver4 && yaw < 3f * MathHelper.PiOver4)
		{
			Game1.player.FacingDirection = 1; // Right
		}
		else if (yaw >= 3f * MathHelper.PiOver4 && yaw < 5f * MathHelper.PiOver4)
		{
			Game1.player.FacingDirection = 2; // Down
		}
		else
		{
			Game1.player.FacingDirection = 3; // Left
		}
	}
}
