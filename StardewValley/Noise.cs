namespace StardewValley;

public class Noise
{
	private static byte[] perm = new byte[512]
	{
		151, 160, 137, 91, 90, 15, 131, 13, 201, 95,
		96, 53, 194, 233, 7, 225, 140, 36, 103, 30,
		69, 142, 8, 99, 37, 240, 21, 10, 23, 190,
		6, 148, 247, 120, 234, 75, 0, 26, 197, 62,
		94, 252, 219, 203, 117, 35, 11, 32, 57, 177,
		33, 88, 237, 149, 56, 87, 174, 20, 125, 136,
		171, 168, 68, 175, 74, 165, 71, 134, 139, 48,
		27, 166, 77, 146, 158, 231, 83, 111, 229, 122,
		60, 211, 133, 230, 220, 105, 92, 41, 55, 46,
		245, 40, 244, 102, 143, 54, 65, 25, 63, 161,
		1, 216, 80, 73, 209, 76, 132, 187, 208, 89,
		18, 169, 200, 196, 135, 130, 116, 188, 159, 86,
		164, 100, 109, 198, 173, 186, 3, 64, 52, 217,
		226, 250, 124, 123, 5, 202, 38, 147, 118, 126,
		255, 82, 85, 212, 207, 206, 59, 227, 47, 16,
		58, 17, 182, 189, 28, 42, 223, 183, 170, 213,
		119, 248, 152, 2, 44, 154, 163, 70, 221, 153,
		101, 155, 167, 43, 172, 9, 129, 22, 39, 253,
		19, 98, 108, 110, 79, 113, 224, 232, 178, 185,
		112, 104, 218, 246, 97, 228, 251, 34, 242, 193,
		238, 210, 144, 12, 191, 179, 162, 241, 81, 51,
		145, 235, 249, 14, 239, 107, 49, 192, 214, 31,
		181, 199, 106, 157, 184, 84, 204, 176, 115, 121,
		50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
		222, 114, 67, 29, 24, 72, 243, 141, 128, 195,
		78, 66, 215, 61, 156, 180, 151, 160, 137, 91,
		90, 15, 131, 13, 201, 95, 96, 53, 194, 233,
		7, 225, 140, 36, 103, 30, 69, 142, 8, 99,
		37, 240, 21, 10, 23, 190, 6, 148, 247, 120,
		234, 75, 0, 26, 197, 62, 94, 252, 219, 203,
		117, 35, 11, 32, 57, 177, 33, 88, 237, 149,
		56, 87, 174, 20, 125, 136, 171, 168, 68, 175,
		74, 165, 71, 134, 139, 48, 27, 166, 77, 146,
		158, 231, 83, 111, 229, 122, 60, 211, 133, 230,
		220, 105, 92, 41, 55, 46, 245, 40, 244, 102,
		143, 54, 65, 25, 63, 161, 1, 216, 80, 73,
		209, 76, 132, 187, 208, 89, 18, 169, 200, 196,
		135, 130, 116, 188, 159, 86, 164, 100, 109, 198,
		173, 186, 3, 64, 52, 217, 226, 250, 124, 123,
		5, 202, 38, 147, 118, 126, 255, 82, 85, 212,
		207, 206, 59, 227, 47, 16, 58, 17, 182, 189,
		28, 42, 223, 183, 170, 213, 119, 248, 152, 2,
		44, 154, 163, 70, 221, 153, 101, 155, 167, 43,
		172, 9, 129, 22, 39, 253, 19, 98, 108, 110,
		79, 113, 224, 232, 178, 185, 112, 104, 218, 246,
		97, 228, 251, 34, 242, 193, 238, 210, 144, 12,
		191, 179, 162, 241, 81, 51, 145, 235, 249, 14,
		239, 107, 49, 192, 214, 31, 181, 199, 106, 157,
		184, 84, 204, 176, 115, 121, 50, 45, 127, 4,
		150, 254, 138, 236, 205, 93, 222, 114, 67, 29,
		24, 72, 243, 141, 128, 195, 78, 66, 215, 61,
		156, 180
	};

	/// <summary>
	/// 1D simplex noise
	/// </summary>
	/// <param name="x"></param>
	/// <returns></returns>
	public static float Generate(float x)
	{
		int num = FastFloor(x);
		int num2 = num + 1;
		float num3 = x - (float)num;
		float num4 = num3 - 1f;
		float num5 = 1f - num3 * num3;
		float num6 = num5 * num5;
		float num7 = num6 * num6 * grad(perm[num & 0xFF], num3);
		float num8 = 1f - num4 * num4;
		float num9 = num8 * num8;
		float num10 = num9 * num9 * grad(perm[num2 & 0xFF], num4);
		return 0.395f * (num7 + num10);
	}

	/// <summary>
	/// 2D simplex noise
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	public static float Generate(float x, float y)
	{
		float num = (x + y) * 0.3660254f;
		float x2 = x + num;
		float x3 = y + num;
		int num2 = FastFloor(x2);
		int num3 = FastFloor(x3);
		float num4 = (float)(num2 + num3) * 0.21132487f;
		float num5 = (float)num2 - num4;
		float num6 = (float)num3 - num4;
		float num7 = x - num5;
		float num8 = y - num6;
		int num9;
		int num10;
		if (num7 > num8)
		{
			num9 = 1;
			num10 = 0;
		}
		else
		{
			num9 = 0;
			num10 = 1;
		}
		float num11 = num7 - (float)num9 + 0.21132487f;
		float num12 = num8 - (float)num10 + 0.21132487f;
		float num13 = num7 - 1f + 0.42264974f;
		float num14 = num8 - 1f + 0.42264974f;
		int num15 = num2 % 256;
		int num16 = num3 % 256;
		float num17 = 0.5f - num7 * num7 - num8 * num8;
		float num18;
		if (num17 < 0f)
		{
			num18 = 0f;
		}
		else
		{
			num17 *= num17;
			num18 = num17 * num17 * grad(perm[num15 + perm[num16]], num7, num8);
		}
		float num19 = 0.5f - num11 * num11 - num12 * num12;
		float num20;
		if (num19 < 0f)
		{
			num20 = 0f;
		}
		else
		{
			num19 *= num19;
			num20 = num19 * num19 * grad(perm[num15 + num9 + perm[num16 + num10]], num11, num12);
		}
		float num21 = 0.5f - num13 * num13 - num14 * num14;
		float num22;
		if (num21 < 0f)
		{
			num22 = 0f;
		}
		else
		{
			num21 *= num21;
			num22 = num21 * num21 * grad(perm[num15 + 1 + perm[num16 + 1]], num13, num14);
		}
		return 40f * (num18 + num20 + num22);
	}

	private static int FastFloor(float x)
	{
		if (!(x > 0f))
		{
			return (int)x - 1;
		}
		return (int)x;
	}

	private static float grad(int hash, float x)
	{
		int num = hash & 0xF;
		float num2 = 1f + (float)(num & 7);
		if ((num & 8) != 0)
		{
			num2 = 0f - num2;
		}
		return num2 * x;
	}

	private static float grad(int hash, float x, float y)
	{
		int num = hash & 7;
		float num2 = ((num < 4) ? x : y);
		float num3 = ((num < 4) ? y : x);
		return (((num & 1) != 0) ? (0f - num2) : num2) + (((num & 2) != 0) ? (-2f * num3) : (2f * num3));
	}

	private static float grad(int hash, float x, float y, float z)
	{
		int num = hash & 0xF;
		float num2 = ((num < 8) ? x : y);
		float num3 = ((num < 4) ? y : ((num == 12 || num == 14) ? x : z));
		return (((num & 1) != 0) ? (0f - num2) : num2) + (((num & 2) != 0) ? (0f - num3) : num3);
	}

	private static float grad(int hash, float x, float y, float z, float t)
	{
		int num = hash & 0x1F;
		float num2 = ((num < 24) ? x : y);
		float num3 = ((num < 16) ? y : z);
		float num4 = ((num < 8) ? z : t);
		return (((num & 1) != 0) ? (0f - num2) : num2) + (((num & 2) != 0) ? (0f - num3) : num3) + (((num & 4) != 0) ? (0f - num4) : num4);
	}
}
