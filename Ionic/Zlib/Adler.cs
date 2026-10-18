namespace Ionic.Zlib;

/// <summary>
/// Computes an Adler-32 checksum.
/// </summary>
/// <remarks>
/// The Adler checksum is similar to a CRC checksum, but faster to compute, though less
/// reliable.  It is used in producing RFC1950 compressed streams.  The Adler checksum
/// is a required part of the "ZLIB" standard.  Applications will almost never need to
/// use this class directly.
/// </remarks>
///
/// <exclude />
public sealed class Adler
{
	private static readonly uint BASE = 65521u;

	private static readonly int NMAX = 5552;

	/// <summary>
	///   Calculates the Adler32 checksum.
	/// </summary>
	/// <remarks>
	///   <para>
	///     This is used within ZLIB.  You probably don't need to use this directly.
	///   </para>
	/// </remarks>
	/// <example>
	///    To compute an Adler32 checksum on a byte array:
	///  <code>
	///    var adler = Adler.Adler32(0, null, 0, 0);
	///    adler = Adler.Adler32(adler, buffer, index, length);
	///  </code>
	/// </example>
	public static uint Adler32(uint adler, byte[] buf, int index, int len)
	{
		if (buf == null)
		{
			return 1u;
		}
		uint num = adler & 0xFFFF;
		uint num2 = (adler >> 16) & 0xFFFF;
		while (len > 0)
		{
			int num3 = ((len < NMAX) ? len : NMAX);
			len -= num3;
			while (num3 >= 16)
			{
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num += buf[index++];
				num2 += num;
				num3 -= 16;
			}
			if (num3 != 0)
			{
				do
				{
					num += buf[index++];
					num2 += num;
				}
				while (--num3 != 0);
			}
			num %= BASE;
			num2 %= BASE;
		}
		return (num2 << 16) | num;
	}
}
