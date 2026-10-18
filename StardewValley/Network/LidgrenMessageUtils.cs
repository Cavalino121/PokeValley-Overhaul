using System.IO;
using Lidgren.Network;

namespace StardewValley.Network;

/// <summary>A set of utilities for packing/unpacking data within Lidgren messages.</summary>
public static class LidgrenMessageUtils
{
	/// <summary>Write (and potentially compress) the message from <paramref name="srcMsg" /> into <paramref name="destMsg" />.</summary>
	/// <param name="srcMsg">The outgoing message to read data from.</param>
	/// <param name="destMsg">The net outgoing message to write (and potentially compress) data into.</param>
	internal static void WriteMessage(OutgoingMessage srcMsg, NetOutgoingMessage destMsg)
	{
		byte[] data;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using BinaryWriter writer = new BinaryWriter(memoryStream);
			srcMsg.Write(writer);
			data = memoryStream.ToArray();
		}
		using MemoryStream memoryStream2 = new MemoryStream(Program.netCompression.CompressAbove(data, 1024));
		using NetBufferWriteStream destination = new NetBufferWriteStream(destMsg);
		memoryStream2.CopyTo(destination);
	}

	/// <summary>Reads a message from <paramref name="stream" /> into <paramref name="msg" />, and decompresses it if necessary.</summary>
	/// <param name="stream">The stream to read message data from.</param>
	/// <param name="msg">The message to write (and potentially decompress) data into.</param>
	internal static void ReadStreamToMessage(NetBufferReadStream stream, IncomingMessage msg)
	{
		Stream input = stream;
		if (Program.netCompression.TryDecompressStream(stream, out var decompressed))
		{
			input = new MemoryStream(decompressed);
		}
		using BinaryReader reader = new BinaryReader(input);
		msg.Read(reader);
	}
}
