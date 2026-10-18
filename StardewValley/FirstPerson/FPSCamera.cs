using System;
using Microsoft.Xna.Framework;

namespace StardewValley.FirstPerson;

/// <summary>
/// 3D Camera for First-Person perspective in Stardew Valley.
/// Translates between 2D pixel/tile grid coordinates and 3D world space.
/// </summary>
public class FPSCamera
{
	/// <summary>Player eye height above ground level (in pixels, 1 tile = 64px).</summary>
	public const float DefaultEyeHeight = 44f;

	/// <summary>Camera position in 3D world space (X = East/West, Y = Altitude, Z = North/South).</summary>
	public Vector3 Position;

	/// <summary>Horizontal rotation angle in radians (0 = facing North / -Z, PI/2 = facing East / +X).</summary>
	public float Yaw;

	/// <summary>Vertical look angle in radians (-PI/2 = looking straight down, +PI/2 = looking straight up).</summary>
	public float Pitch;

	/// <summary>Field of view in degrees.</summary>
	public float FieldOfView = 75f;

	/// <summary>Near clipping plane distance.</summary>
	public float NearPlane = 1f;

	/// <summary>Far clipping plane distance.</summary>
	public float FarPlane = 6000f;

	public Matrix ViewMatrix { get; private set; }
	public Matrix ProjectionMatrix { get; private set; }

	public Vector3 Forward { get; private set; }
	public Vector3 Right { get; private set; }
	public Vector3 Up => Vector3.Up;

	public FPSCamera()
	{
		Yaw = 0f;
		Pitch = 0f;
		Position = new Vector3(0f, DefaultEyeHeight, 0f);
	}

	/// <summary>
	/// Update camera position and orientation based on farmer position and look angles.
	/// </summary>
	public void Update(Vector2 farmerPixelPosition, float viewportWidth, float viewportHeight)
	{
		// In Stardew Valley 2D, (X, Y) is top-down on the map.
		// In our 3D space: X = 2D X, Z = 2D Y, Y = Altitude above ground.
		// Center on farmer's head (approx 32px horizontal offset, 44px above ground)
		Position = new Vector3(farmerPixelPosition.X + 32f, DefaultEyeHeight, farmerPixelPosition.Y + 32f);

		// Clamp Pitch so player cannot flip camera upside down (-85 to +85 degrees)
		Pitch = MathHelper.Clamp(Pitch, MathHelper.ToRadians(-85f), MathHelper.ToRadians(85f));

		// Normalize Yaw to [0, 2*PI]
		while (Yaw < 0f) Yaw += MathHelper.TwoPi;
		while (Yaw >= MathHelper.TwoPi) Yaw -= MathHelper.TwoPi;

		// Calculate Forward vector (Z is positive South, negative North in XNA right-handed coordinate system)
		Forward = new Vector3(
			(float)(Math.Sin(Yaw) * Math.Cos(Pitch)),
			(float)Math.Sin(Pitch),
			-(float)(Math.Cos(Yaw) * Math.Cos(Pitch))
		);
		Forward = Vector3.Normalize(Forward);

		// Right vector (perpendicular on XZ ground plane)
		Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.Up));

		// Build View Matrix
		ViewMatrix = Matrix.CreateLookAt(Position, Position + Forward, Vector3.Up);

		// Build Perspective Projection Matrix
		float aspectRatio = Math.Max(1f, viewportWidth / Math.Max(1f, viewportHeight));
		ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
			MathHelper.ToRadians(FieldOfView),
			aspectRatio,
			NearPlane,
			FarPlane
		);
	}

	/// <summary>
	/// Raycast from camera center to find which tile on the ground (Y = 0) the player is aiming at.
	/// </summary>
	/// <param name="maxDistance">Maximum reach distance in pixels (default 256 = 4 tiles).</param>
	/// <param name="targetTile">Output tile coordinates.</param>
	/// <returns>True if the ray intersects the ground within max reach.</returns>
	public bool TryGetTargetGroundTile(float maxDistance, out Point targetTile)
	{
		targetTile = Point.Zero;

		// Ray equation: P(t) = Position + Forward * t
		// Ground plane is Y = 0 => Position.Y + Forward.Y * t = 0 => t = -Position.Y / Forward.Y
		if (Math.Abs(Forward.Y) < 0.0001f || Forward.Y >= 0f)
		{
			// Player is looking up or parallel to ground
			return false;
		}

		float t = -Position.Y / Forward.Y;
		if (t <= 0f || t > maxDistance)
		{
			return false;
		}

		Vector3 hitPoint = Position + Forward * t;
		int tileX = (int)Math.Floor(hitPoint.X / 64f);
		int tileY = (int)Math.Floor(hitPoint.Z / 64f);

		targetTile = new Point(tileX, tileY);
		return true;
	}
}
