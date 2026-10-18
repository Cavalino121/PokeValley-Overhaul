using Microsoft.Xna.Framework;

namespace StardewValley.Locations;

public class Mine : GameLocation
{
	public Mine()
	{
	}

	public Mine(string map, string name)
		: base(map, name)
	{
		Vector2 boulderPosition = GetBoulderPosition();
		objects.Add(boulderPosition, new Object(boulderPosition, "78"));
	}

	public override void DayUpdate(int dayOfMonth)
	{
		base.DayUpdate(dayOfMonth);
		MineShaft.mushroomLevelsGeneratedToday.Clear();
	}

	/// <summary>Get the tile position for the boulder which initially blocks access to the dwarf.</summary>
	public Vector2 GetBoulderPosition()
	{
		return new Vector2(27f, 8f);
	}
}
