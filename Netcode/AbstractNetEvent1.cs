using System;
using System.Collections.Generic;
using System.IO;

namespace Netcode;

public abstract class AbstractNetEvent1<T> : AbstractNetSerializable
{
	public class EventRecording
	{
		public T arg;

		public uint timestamp;

		public EventRecording(T arg, uint timestamp)
		{
			this.arg = arg;
			this.timestamp = timestamp;
		}
	}

	public delegate void Event(T arg);

	public bool InterpolationWait = true;

	private List<EventRecording> outgoingEvents = new List<EventRecording>();

	private List<EventRecording> incomingEvents = new List<EventRecording>();

	public event Event onEvent;

	public bool HasPendingEvent(Predicate<T> match)
	{
		return incomingEvents.Exists((EventRecording e) => match(e.arg));
	}

	public void Clear()
	{
		outgoingEvents.Clear();
		incomingEvents.Clear();
	}

	public void Fire(T arg)
	{
		EventRecording item = new EventRecording(arg, GetLocalTick());
		outgoingEvents.Add(item);
		incomingEvents.Add(item);
		MarkDirty();
		Poll();
	}

	public void Poll()
	{
		List<EventRecording> list = null;
		foreach (EventRecording incomingEvent in incomingEvents)
		{
			if (base.Root == null || GetLocalTick() >= incomingEvent.timestamp)
			{
				if (list == null)
				{
					list = new List<EventRecording>();
				}
				list.Add(incomingEvent);
				continue;
			}
			break;
		}
		if (list == null || list.Count <= 0)
		{
			return;
		}
		incomingEvents.RemoveAll(list.Contains);
		if (this.onEvent == null)
		{
			return;
		}
		foreach (EventRecording item in list)
		{
			this.onEvent(item.arg);
		}
	}

	protected abstract T readEventArg(BinaryReader reader, NetVersion version);

	protected abstract void writeEventArg(BinaryWriter writer, T arg);

	public override void Read(BinaryReader reader, NetVersion version)
	{
		uint num = reader.Read7BitEncoded();
		uint num2 = GetLocalTick();
		if (InterpolationWait)
		{
			num2 += (uint)base.Root.Clock.InterpolationTicks;
		}
		for (uint num3 = 0u; num3 < num; num3++)
		{
			uint num4 = reader.ReadUInt32();
			incomingEvents.Add(new EventRecording(readEventArg(reader, version), num2 + num4));
		}
		ChangeVersion.Merge(version);
	}

	public override void ReadFull(BinaryReader reader, NetVersion version)
	{
		ChangeVersion.Merge(version);
	}

	public override void Write(BinaryWriter writer)
	{
		writer.Write7BitEncoded((uint)outgoingEvents.Count);
		if (outgoingEvents.Count > 0)
		{
			uint timestamp = outgoingEvents[0].timestamp;
			foreach (EventRecording outgoingEvent in outgoingEvents)
			{
				writer.Write(outgoingEvent.timestamp - timestamp);
				writeEventArg(writer, outgoingEvent.arg);
			}
		}
		outgoingEvents.Clear();
	}

	protected override void CleanImpl()
	{
		base.CleanImpl();
		outgoingEvents.Clear();
	}

	public override void WriteFull(BinaryWriter writer)
	{
	}
}
