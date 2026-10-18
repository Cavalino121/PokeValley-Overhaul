using System.IO;
using Microsoft.Xna.Framework;

namespace Netcode;

public sealed class NetRectangle : NetField<Rectangle, NetRectangle>
{
	public int X
	{
		get
		{
			return base.Value.X;
		}
		set
		{
			Rectangle rectangle = base.value;
			if (rectangle.X != value)
			{
				Rectangle newValue = new Rectangle(value, rectangle.Y, rectangle.Width, rectangle.Height);
				if (canShortcutSet())
				{
					base.value = newValue;
					return;
				}
				cleanSet(newValue);
				MarkDirty();
			}
		}
	}

	public int Y
	{
		get
		{
			return base.Value.Y;
		}
		set
		{
			Rectangle rectangle = base.value;
			if (rectangle.Y != value)
			{
				Rectangle newValue = new Rectangle(rectangle.X, value, rectangle.Width, rectangle.Height);
				if (canShortcutSet())
				{
					base.value = newValue;
					return;
				}
				cleanSet(newValue);
				MarkDirty();
			}
		}
	}

	public int Width
	{
		get
		{
			return base.Value.Width;
		}
		set
		{
			Rectangle rectangle = base.value;
			if (rectangle.Width != value)
			{
				Rectangle newValue = new Rectangle(rectangle.X, rectangle.Y, value, rectangle.Height);
				if (canShortcutSet())
				{
					base.value = newValue;
					return;
				}
				cleanSet(newValue);
				MarkDirty();
			}
		}
	}

	public int Height
	{
		get
		{
			return base.Value.Height;
		}
		set
		{
			Rectangle rectangle = base.value;
			if (rectangle.Height != value)
			{
				Rectangle newValue = new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, value);
				if (canShortcutSet())
				{
					base.value = newValue;
					return;
				}
				cleanSet(newValue);
				MarkDirty();
			}
		}
	}

	public Point Center => value.Center;

	public int Top => value.Top;

	public int Bottom => value.Bottom;

	public int Left => value.Left;

	public int Right => value.Right;

	public NetRectangle()
	{
	}

	public NetRectangle(Rectangle value)
		: base(value)
	{
	}

	public void Set(int x, int y, int width, int height)
	{
		Set(new Rectangle(x, y, width, height));
	}

	public override void Set(Rectangle newValue)
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

	protected override void ReadDelta(BinaryReader reader, NetVersion version)
	{
		int x = reader.ReadInt32();
		int y = reader.ReadInt32();
		int width = reader.ReadInt32();
		int height = reader.ReadInt32();
		if (version.IsPriorityOver(ChangeVersion))
		{
			setInterpolationTarget(new Rectangle(x, y, width, height));
		}
	}

	protected override void WriteDelta(BinaryWriter writer)
	{
		writer.Write(value.X);
		writer.Write(value.Y);
		writer.Write(value.Width);
		writer.Write(value.Height);
	}
}
