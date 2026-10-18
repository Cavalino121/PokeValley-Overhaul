using System;
using System.IO;

namespace Netcode;

public class NetRotation : NetField<float, NetRotation>
{
	public NetRotation()
	{
	}

	public NetRotation(float value)
		: base(value)
	{
	}

	public override void Set(float newValue)
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

	protected override float interpolate(float startValue, float endValue, float factor)
	{
		float num = Math.Abs(endValue - startValue);
		float num2 = (float)Math.PI * 2f;
		if (num > 180f)
		{
			if (endValue > startValue)
			{
				startValue += num2;
			}
			else
			{
				endValue += num2;
			}
		}
		return (startValue + (endValue - startValue) * factor) % num2;
	}

	protected override void ReadDelta(BinaryReader reader, NetVersion version)
	{
		float interpolationTarget = reader.ReadSingle();
		if (version.IsPriorityOver(ChangeVersion))
		{
			setInterpolationTarget(interpolationTarget);
		}
	}

	protected override void WriteDelta(BinaryWriter writer)
	{
		writer.Write(value);
	}
}
