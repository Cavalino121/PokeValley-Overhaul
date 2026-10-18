using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.ItemTypeDefinitions;

namespace StardewValley.Menus;

public class PI_ItemList : ProfileItem
{
	protected List<Item> _items;

	protected List<ClickableTextureComponent> _components;

	protected float _height;

	protected List<Vector2> _emptyBoxPositions;

	public PI_ItemList(ProfileMenu context, string name, List<Item> values)
		: base(context, name)
	{
		_items = values;
		_components = new List<ClickableTextureComponent>();
		_height = 0f;
		_emptyBoxPositions = new List<Vector2>();
		_UpdateIcons();
	}

	public override void Unload()
	{
		base.Unload();
		_ClearItems();
	}

	protected void _ClearItems()
	{
		for (int i = 0; i < _components.Count; i++)
		{
			_context.UnregisterClickable(_components[i]);
		}
		_components.Clear();
	}

	protected void _UpdateIcons()
	{
		_ClearItems();
		Vector2 vector = new Vector2(0f, 0f);
		for (int i = 0; i < _items.Count; i++)
		{
			Item item = _items[i];
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
			ClickableTextureComponent clickableTextureComponent = new ClickableTextureComponent(item.DisplayName, new Rectangle((int)vector.X, (int)vector.Y, 32, 32), null, "", dataOrErrorItem.GetTexture(), dataOrErrorItem.GetSourceRect(), 2f)
			{
				myID = 0,
				name = item.DisplayName,
				upNeighborID = -99998,
				downNeighborID = -99998,
				leftNeighborID = -99998,
				rightNeighborID = -99998,
				region = 502
			};
			_components.Add(clickableTextureComponent);
			_context.RegisterClickable(clickableTextureComponent);
		}
	}

	public override float HandleLayout(float draw_y, Rectangle content_rectangle, int index)
	{
		_emptyBoxPositions.Clear();
		draw_y = base.HandleLayout(draw_y, content_rectangle, index);
		int i = 0;
		int num = (int)draw_y;
		Point point = new Point(4, 4);
		for (int j = 0; j < _components.Count; j++)
		{
			ClickableTextureComponent clickableTextureComponent = _components[j];
			if (i + clickableTextureComponent.bounds.Width + point.Y > content_rectangle.Width)
			{
				i = 0;
				draw_y += (float)(clickableTextureComponent.bounds.Height + point.Y);
			}
			clickableTextureComponent.bounds.X = content_rectangle.Left + i;
			clickableTextureComponent.bounds.Y = (int)draw_y;
			i += clickableTextureComponent.bounds.Width + point.X;
			num = Math.Max((int)draw_y + clickableTextureComponent.bounds.Height, num);
		}
		for (; i + 32 + point.X <= content_rectangle.Width; i += 32 + point.X)
		{
			_emptyBoxPositions.Add(new Vector2(content_rectangle.Left + i, draw_y));
		}
		return num + 8;
	}

	public override void DrawItem(SpriteBatch b)
	{
		for (int i = 0; i < _components.Count; i++)
		{
			ClickableTextureComponent clickableTextureComponent = _components[i];
			b.Draw(Game1.menuTexture, new Rectangle(clickableTextureComponent.bounds.X, clickableTextureComponent.bounds.Y, 32, 32), new Rectangle(64, 128, 64, 64), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 4.3E-05f);
			b.Draw(Game1.menuTexture, new Rectangle(clickableTextureComponent.bounds.X, clickableTextureComponent.bounds.Y, 32, 32), new Rectangle(128, 128, 64, 64), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 4.3E-05f);
			_components[i].draw(b, Color.White, 4.1E-05f);
			if (Game1.player.Items.ContainsId(_items[i].ItemId))
			{
				b.Draw(Game1.mouseCursors, new Rectangle(_components[i].bounds.X + 32 - 11, _components[i].bounds.Y + 32 - 13, 11, 13), new Rectangle(268, 1436, 11, 13), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 4E-05f);
			}
		}
		for (int j = 0; j < _emptyBoxPositions.Count; j++)
		{
			b.Draw(Game1.menuTexture, new Rectangle((int)_emptyBoxPositions[j].X, (int)_emptyBoxPositions[j].Y, 32, 32), new Rectangle(64, 896, 64, 64), Color.White * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 4.3E-05f);
			b.Draw(Game1.menuTexture, new Rectangle((int)_emptyBoxPositions[j].X, (int)_emptyBoxPositions[j].Y, 32, 32), new Rectangle(128, 128, 64, 64), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 4.3E-05f);
		}
	}

	public override void performHover(int x, int y)
	{
		for (int i = 0; i < _components.Count; i++)
		{
			if (_components[i].bounds.Contains(new Point(x, y)))
			{
				_context.hoveredItem = _items[i];
			}
		}
	}

	public override bool ShouldDraw()
	{
		return _items.Count > 0;
	}
}
