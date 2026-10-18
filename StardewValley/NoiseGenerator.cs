using System;

namespace StardewValley;

[InstanceStatics]
internal static class NoiseGenerator
{
	public static int Seed { get; set; }

	public static int Octaves { get; set; }

	public static double Amplitude { get; set; }

	public static double Persistence { get; set; }

	public static double Frequency { get; set; }

	static NoiseGenerator()
	{
		Seed = new Random().Next(int.MaxValue);
		Octaves = 8;
		Amplitude = 1.0;
		Frequency = 0.015;
		Persistence = 0.65;
	}

	public static double Noise(int x, int y)
	{
		double num = 0.0;
		double num2 = Frequency;
		double num3 = Amplitude;
		for (int i = 0; i < Octaves; i++)
		{
			num += Smooth((double)x * num2, (double)y * num2) * num3;
			num2 *= 2.0;
			num3 *= Persistence;
		}
		if (num < -2.4)
		{
			num = -2.4;
		}
		else if (num > 2.4)
		{
			num = 2.4;
		}
		return num / 2.4;
	}

	public static double NoiseGeneration(int x, int y)
	{
		int num = x + y * 57;
		num = (num << 13) ^ num;
		return 1.0 - (double)((num * (num * num * 15731 + 789221) + Seed) & 0x7FFFFFFF) / 1073741824.0;
	}

	private static double Interpolate(double x, double y, double a)
	{
		double num = (1.0 - Math.Cos(a * Math.PI)) * 0.5;
		return x * (1.0 - num) + y * num;
	}

	private static double Smooth(double x, double y)
	{
		double x2 = NoiseGeneration((int)x, (int)y);
		double y2 = NoiseGeneration((int)x + 1, (int)y);
		double x3 = NoiseGeneration((int)x, (int)y + 1);
		double y3 = NoiseGeneration((int)x + 1, (int)y + 1);
		double x4 = Interpolate(x2, y2, x - (double)(int)x);
		double y4 = Interpolate(x3, y3, x - (double)(int)x);
		return Interpolate(x4, y4, y - (double)(int)y);
	}
}
