using System;
using System.Collections;
using System.IO;
using Microsoft.Xna.Framework;

namespace Netcode;

public static class BinaryReaderWriterExtensions
{
	public static void ReadSkippable(this BinaryReader reader, Action readAction)
	{
		uint num = reader.ReadUInt32();
		long position = reader.BaseStream.Position;
		readAction();
		if (reader.BaseStream.Position > position + num)
		{
			throw new InvalidOperationException();
		}
		reader.BaseStream.Position = position + num;
	}

	public static byte[] ReadSkippableBytes(this BinaryReader reader)
	{
		uint count = reader.ReadUInt32();
		return reader.ReadBytes((int)count);
	}

	public static void Skip(this BinaryReader reader)
	{
		reader.ReadSkippable(delegate
		{
		});
	}

	public static void WriteSkippable(this BinaryWriter writer, Action writeAction)
	{
		long position = writer.BaseStream.Position;
		writer.Write(0u);
		long position2 = writer.BaseStream.Position;
		writeAction();
		long position3 = writer.BaseStream.Position;
		long num = position3 - position2;
		writer.BaseStream.Position = position;
		writer.Write((uint)num);
		writer.BaseStream.Position = position3;
	}

	public static BitArray ReadBitArray(this BinaryReader reader)
	{
		int num = (int)reader.Read7BitEncoded();
		return new BitArray(reader.ReadBytes((num + 7) / 8))
		{
			Length = num
		};
	}

	public static void WriteBitArray(this BinaryWriter writer, BitArray bits)
	{
		byte[] array = new byte[(bits.Length + 7) / 8];
		bits.CopyTo(array, 0);
		writer.Write7BitEncoded((uint)bits.Length);
		writer.Write(array);
	}

	public static void Write7BitEncoded(this BinaryWriter writer, uint value)
	{
		do
		{
			byte b = (byte)(value & 0x7F);
			value >>= 7;
			if (value != 0)
			{
				b |= 0x80;
			}
			writer.Write(b);
		}
		while (value != 0);
	}

	public static uint Read7BitEncoded(this BinaryReader reader)
	{
		uint num = 0u;
		byte b = reader.ReadByte();
		int num2 = 0;
		while ((b & 0x80) != 0)
		{
			num |= (uint)((b & 0x7F) << num2);
			num2 += 7;
			b = reader.ReadByte();
		}
		return num | (uint)((b & 0x7F) << num2);
	}

	public static Guid ReadGuid(this BinaryReader reader)
	{
		return new Guid(reader.ReadBytes(16));
	}

	public static void WriteGuid(this BinaryWriter writer, Guid guid)
	{
		writer.Write(guid.ToByteArray());
	}

	public static Vector2 ReadVector2(this BinaryReader reader)
	{
		float x = reader.ReadSingle();
		float y = reader.ReadSingle();
		return new Vector2(x, y);
	}

	public static void WriteVector2(this BinaryWriter writer, Vector2 vec)
	{
		writer.Write(vec.X);
		writer.Write(vec.Y);
	}

	public static Point ReadPoint(this BinaryReader reader)
	{
		int x = reader.ReadInt32();
		int y = reader.ReadInt32();
		return new Point(x, y);
	}

	public static void WritePoint(this BinaryWriter writer, Point p)
	{
		writer.Write(p.X);
		writer.Write(p.Y);
	}

	public static Rectangle ReadRectangle(this BinaryReader reader)
	{
		Point point = reader.ReadPoint();
		Point point2 = reader.ReadPoint();
		return new Rectangle(point.X, point.Y, point2.X, point2.Y);
	}

	public static void WriteRectangle(this BinaryWriter writer, Rectangle rect)
	{
		writer.WritePoint(rect.Location);
		writer.WritePoint(new Point(rect.Width, rect.Height));
	}

	public static Color ReadColor(this BinaryReader reader)
	{
		return new Color
		{
			PackedValue = reader.ReadUInt32()
		};
	}

	public static void WriteColor(this BinaryWriter writer, Color color)
	{
		writer.Write(color.PackedValue);
	}

	public static T ReadEnum<T>(this BinaryReader reader) where T : struct, IConvertible
	{
		return (T)Enum.ToObject(typeof(T), reader.ReadInt16());
	}

	public static void WriteEnum<T>(this BinaryWriter writer, T enumValue) where T : struct, IConvertible
	{
		writer.Write(Convert.ToInt16(enumValue));
	}

	public static void WriteEnum(this BinaryWriter writer, object enumValue)
	{
		writer.Write(Convert.ToInt16(enumValue));
	}

	public static void Push(this BinaryWriter writer, string name)
	{
		if (writer is ILoggingWriter loggingWriter)
		{
			loggingWriter.Push(name);
		}
	}

	public static void Pop(this BinaryWriter writer)
	{
		if (writer is ILoggingWriter loggingWriter)
		{
			loggingWriter.Pop();
		}
	}
}
