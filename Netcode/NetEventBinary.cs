using System;
using System.IO;

namespace Netcode;

public class NetEventBinary : AbstractNetEvent1<byte[]>
{
	public delegate void ArgWriter(BinaryWriter writer);

	public void Fire(ArgWriter argWriter)
	{
		byte[] array;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using BinaryWriter writer = new BinaryWriter(memoryStream);
			argWriter(writer);
			memoryStream.Position = 0L;
			array = new byte[memoryStream.Length];
			memoryStream.Read(array, 0, (int)memoryStream.Length);
		}
		Fire(array);
	}

	public void AddReaderHandler(Action<BinaryReader> handler)
	{
		base.onEvent += delegate(byte[] bytes)
		{
			using MemoryStream input = new MemoryStream(bytes);
			using BinaryReader obj = new BinaryReader(input);
			handler(obj);
		};
	}

	protected override byte[] readEventArg(BinaryReader reader, NetVersion version)
	{
		int count = reader.ReadInt32();
		return reader.ReadBytes(count);
	}

	protected override void writeEventArg(BinaryWriter writer, byte[] arg)
	{
		writer.Write(arg.Length);
		writer.Write(arg);
	}
}
