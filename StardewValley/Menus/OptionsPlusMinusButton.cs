using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Menus;

public class OptionsPlusMinusButton : OptionsPlusMinus
{
	protected Rectangle _buttonBounds;

	protected Rectangle _buttonRect;

	protected Texture2D _buttonTexture;

	protected Action<string> _buttonAction;

	public OptionsPlusMinusButton(string label, int whichOptions, List<string> options, List<string> displayOptions, Texture2D buttonTexture, Rectangle buttonRect, Action<string> buttonAction, int x = -1, int y = -1)
		: base(label, whichOptions, options, displayOptions, x, y)
	{
		_buttonRect = buttonRect;
		_buttonBounds = new Rectangle(bounds.Left, 4 - _buttonRect.Height / 2 + 8, _buttonRect.Width * 4, _buttonRect.Height * 4);
		_buttonTexture = buttonTexture;
		_buttonAction = buttonAction;
		int num = 8;
		plusButton.X += _buttonBounds.Width + num * 4;
		minusButton.X += _buttonBounds.Width + num * 4;
		bounds.Width += _buttonBounds.Width + num * 4;
		int num2 = _buttonBounds.Height - bounds.Height;
		if (num2 > 0)
		{
			bounds.Y -= num2 / 2;
			bounds.Height += num2;
			labelOffset.Y += num2 / 2;
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
	{
		b.Draw(_buttonTexture, new Vector2(slotX + _buttonBounds.X, slotY + _buttonBounds.Y), _buttonRect, Color.White * (greyedOut ? 0.33f : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.4f);
		base.draw(b, slotX, slotY, context);
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y)
	{
		if (!greyedOut && _buttonBounds.Contains(x, y))
		{
			if (_buttonAction != null)
			{
				string obj = "";
				if (selected >= 0 && selected < options.Count)
				{
					obj = options[selected];
				}
				_buttonAction(obj);
			}
		}
		else
		{
			base.receiveLeftClick(x, y);
		}
	}
}
