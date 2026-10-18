using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace StardewValley;

[InstanceStatics]
public static class Rumble
{
	private static float rumbleStrength;

	private static float rumbleTimerMax;

	private static float rumbleTimerCurrent;

	private static float rumbleDuringFade;

	private static float maxRumbleDuringFade;

	private static bool isRumbling;

	private static bool fade;

	public static void update(float milliseconds)
	{
		float num = 0f;
		if (isRumbling)
		{
			num = rumbleStrength;
			rumbleTimerCurrent += milliseconds;
			if (rumbleTimerCurrent > rumbleTimerMax)
			{
				num = 0f;
			}
			else if (fade)
			{
				if (rumbleTimerCurrent > rumbleTimerMax - 1000f)
				{
					rumbleDuringFade = Utility.Lerp(maxRumbleDuringFade, 0f, (rumbleTimerCurrent - (rumbleTimerMax - 1000f)) / 1000f);
				}
				num = rumbleDuringFade;
			}
		}
		if (num <= 0f)
		{
			num = 0f;
			isRumbling = false;
		}
		if ((double)num > 1.0)
		{
			num = 1f;
		}
		if (!Game1.options.gamepadControls || !Game1.options.rumble)
		{
			num = 0f;
		}
		if (Game1.playerOneIndex != (PlayerIndex)(-1))
		{
			GamePad.SetVibration(Game1.playerOneIndex, num, num);
		}
	}

	public static void stopRumbling()
	{
		rumbleStrength = 0f;
		isRumbling = false;
	}

	public static void rumble(float leftPower, float rightPower, float milliseconds)
	{
		rumble(leftPower, milliseconds);
	}

	public static void rumble(float power, float milliseconds)
	{
		if (!isRumbling && Game1.options.gamepadControls && Game1.options.rumble)
		{
			fade = false;
			rumbleTimerCurrent = 0f;
			rumbleTimerMax = milliseconds;
			isRumbling = true;
			rumbleStrength = power;
		}
	}

	public static void rumbleAndFade(float power, float milliseconds)
	{
		if (!isRumbling && Game1.options.gamepadControls && Game1.options.rumble)
		{
			rumbleTimerCurrent = 0f;
			rumbleTimerMax = milliseconds;
			isRumbling = true;
			fade = true;
			rumbleDuringFade = power;
			maxRumbleDuringFade = power;
			rumbleStrength = power;
		}
	}
}
