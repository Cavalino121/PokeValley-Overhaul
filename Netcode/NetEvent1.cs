using System.IO;

namespace Netcode;

public class NetEvent1<T> : AbstractNetEvent1<T> where T : NetEventArg, new()
{
	protected override T readEventArg(BinaryReader reader, NetVersion version)
	{
		T result = new T();
		result.Read(reader);
		return result;
	}

	protected override void writeEventArg(BinaryWriter writer, T eventArg)
	{
		eventArg.Write(writer);
	}
}
