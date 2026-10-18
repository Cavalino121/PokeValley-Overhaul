using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace Netcode;

public sealed class NetVector2 : NetField<Vector2, NetVector2>
{
	public bool AxisAlignedMovement;

	public float ExtrapolationSpeed;

	public float MinDeltaForDirectionChange = 8f;

	public float MaxInterpolationDistance = 320f;

	private bool interpolateXFirst;

	private bool isExtrapolating;

	private bool isFixingExtrapolation;

	public float X
	{
		get
		{
			return base.Value.X;
		}
		set
		{
			Vector2 vector = base.value;
			if (vector.X != value)
			{
				Vector2 newValue = new Vector2(value, vector.Y);
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

	public float Y
	{
		get
		{
			return base.Value.Y;
		}
		set
		{
			Vector2 vector = base.value;
			if (vector.Y != value)
			{
				Vector2 newValue = new Vector2(vector.X, value);
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

	public NetVector2()
	{
	}

	public NetVector2(Vector2 value)
		: base(value)
	{
	}

	public void Set(float x, float y)
	{
		Set(new Vector2(x, y));
	}

	public override void Set(Vector2 newValue)
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

	public Vector2 InterpolationDelta()
	{
		if (base.NeedsTick)
		{
			return targetValue - previousValue;
		}
		return Vector2.Zero;
	}

	protected override bool setUpInterpolation(Vector2 oldValue, Vector2 newValue)
	{
		if ((newValue - oldValue).LengthSquared() >= MaxInterpolationDistance * MaxInterpolationDistance)
		{
			return false;
		}
		if (AxisAlignedMovement)
		{
			if (base.NeedsTick)
			{
				Vector2 vector = targetValue - previousValue;
				Vector2 vector2 = new Vector2(Math.Abs(vector.X), Math.Abs(vector.Y));
				if (interpolateXFirst)
				{
					interpolateXFirst = InterpolationFactor() * (vector2.X + vector2.Y) < vector2.X;
				}
				else
				{
					interpolateXFirst = InterpolationFactor() * (vector2.X + vector2.Y) > vector2.Y;
				}
			}
			else
			{
				Vector2 vector3 = newValue - oldValue;
				Vector2 vector4 = new Vector2(Math.Abs(vector3.X), Math.Abs(vector3.Y));
				interpolateXFirst = vector4.X < vector4.Y;
			}
		}
		return true;
	}

	public Vector2 CurrentInterpolationDirection()
	{
		if (AxisAlignedMovement)
		{
			float num = InterpolationFactor();
			Vector2 vector = InterpolationDelta();
			float num2 = (Math.Abs(vector.X) + Math.Abs(vector.Y)) * num;
			if (Math.Abs(vector.X) < MinDeltaForDirectionChange && Math.Abs(vector.Y) < MinDeltaForDirectionChange)
			{
				return Vector2.Zero;
			}
			if (Math.Abs(vector.X) < MinDeltaForDirectionChange)
			{
				return new Vector2(0f, Math.Sign(vector.Y));
			}
			if (Math.Abs(vector.Y) < MinDeltaForDirectionChange)
			{
				return new Vector2(Math.Sign(vector.X), 0f);
			}
			if (interpolateXFirst)
			{
				if (num2 > Math.Abs(vector.X))
				{
					return new Vector2(0f, Math.Sign(vector.Y));
				}
				return new Vector2(Math.Sign(vector.X), 0f);
			}
			if (num2 > Math.Abs(vector.Y))
			{
				return new Vector2(Math.Sign(vector.X), 0f);
			}
			return new Vector2(0f, Math.Sign(vector.Y));
		}
		Vector2 result = InterpolationDelta();
		result.Normalize();
		return result;
	}

	public float CurrentInterpolationSpeed()
	{
		float num = InterpolationDelta().Length();
		if (InterpolationTicks() == 0)
		{
			return num;
		}
		if (InterpolationFactor() > 1f)
		{
			return ExtrapolationSpeed;
		}
		return num / (float)InterpolationTicks();
	}

	protected override Vector2 interpolate(Vector2 startValue, Vector2 endValue, float factor)
	{
		if (AxisAlignedMovement && factor <= 1f && !isFixingExtrapolation)
		{
			isExtrapolating = false;
			Vector2 vector = InterpolationDelta();
			Vector2 vector2 = new Vector2(Math.Abs(vector.X), Math.Abs(vector.Y));
			float num = (vector2.X + vector2.Y) * factor;
			float x;
			float y;
			if (interpolateXFirst)
			{
				if (num > vector2.X)
				{
					x = endValue.X;
					y = startValue.Y + (num - vector2.X) * (float)Math.Sign(vector.Y);
				}
				else
				{
					x = startValue.X + num * (float)Math.Sign(vector.X);
					y = startValue.Y;
				}
			}
			else if (num > vector2.Y)
			{
				y = endValue.Y;
				x = startValue.X + (num - vector2.Y) * (float)Math.Sign(vector.X);
			}
			else
			{
				y = startValue.Y + num * (float)Math.Sign(vector.Y);
				x = startValue.X;
			}
			return new Vector2(x, y);
		}
		if (factor > 1f)
		{
			isExtrapolating = true;
			uint num2 = base.Root.Clock.GetLocalTick() - interpolationStartTick - (uint)InterpolationTicks();
			Vector2 vector3 = endValue - startValue;
			if (vector3.LengthSquared() > ExtrapolationSpeed * ExtrapolationSpeed)
			{
				vector3.Normalize();
				return endValue + vector3 * num2 * ExtrapolationSpeed;
			}
		}
		isExtrapolating = false;
		return startValue + (endValue - startValue) * factor;
	}

	protected override void ReadDelta(BinaryReader reader, NetVersion version)
	{
		float x = reader.ReadSingle();
		float y = reader.ReadSingle();
		if (version.IsPriorityOver(ChangeVersion))
		{
			isFixingExtrapolation = isExtrapolating;
			setInterpolationTarget(new Vector2(x, y));
			isExtrapolating = false;
		}
	}

	protected override void WriteDelta(BinaryWriter writer)
	{
		writer.Write(base.Value.X);
		writer.Write(base.Value.Y);
	}
}
