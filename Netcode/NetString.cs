using System.IO;

namespace Netcode;

public class NetString : NetField<string, NetString>
{
	public delegate string FilterString(string newValue);

	public int Length => base.Value.Length;

	public event FilterString FilterStringEvent;

	public NetString()
		: base((string)null)
	{
	}

	public NetString(string value)
		: base(value)
	{
	}

	public override void Set(string newValue)
	{
		if (canShortcutSet())
		{
			value = newValue;
		}
		else if (newValue != value)
		{
			cleanSet(newValue);
			MarkDirty();
		}
	}

	public bool Contains(string substr)
	{
		if (base.Value != null)
		{
			return base.Value.Contains(substr);
		}
		return false;
	}

	protected override void ReadDelta(BinaryReader reader, NetVersion version)
	{
		string text = null;
		if (reader.ReadBoolean())
		{
			text = reader.ReadString();
			if (this.FilterStringEvent != null)
			{
				text = this.FilterStringEvent(text);
			}
		}
		if (version.IsPriorityOver(ChangeVersion))
		{
			setInterpolationTarget(text);
		}
	}

	protected override void WriteDelta(BinaryWriter writer)
	{
		writer.Write(value != null);
		if (value != null)
		{
			writer.Write(value);
		}
	}
}
