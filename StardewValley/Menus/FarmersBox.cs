using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Menus;

internal class FarmersBox : IClickableMenu
{
	private readonly List<Farmer> _farmers = new List<Farmer>();

	public float _updateTimer;

	public FarmersBox()
		: base(0, 200, 528, 400)
	{
	}

	private void UpdateFarmers(List<ClickableComponent> parentComponents)
	{
		if (_updateTimer > 0f)
		{
			return;
		}
		_farmers.Clear();
		foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
		{
			_farmers.Add(onlineFarmer);
		}
		_updateTimer = 1f;
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		_updateTimer -= (float)time.ElapsedGameTime.TotalSeconds;
	}

	public void draw(SpriteBatch b, int left, int bottom, ClickableComponent current, List<ClickableComponent> parentComponents)
	{
		UpdateFarmers(parentComponents);
		if (_farmers.Count == 0)
		{
			return;
		}
		int num = 100;
		height = num * _farmers.Count;
		xPositionOnScreen = left;
		yPositionOnScreen = bottom - height;
		IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(301, 288, 15, 15), xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f, drawShadow: false);
		b.End();
		b.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, Utility.ScissorEnabled);
		Rectangle scissorRectangle = b.GraphicsDevice.ScissorRectangle;
		int num2 = xPositionOnScreen + 16;
		int num3 = yPositionOnScreen;
		for (int i = 0; i < _farmers.Count; i++)
		{
			Farmer farmer = _farmers[i];
			Rectangle scissorRectangle2 = scissorRectangle;
			scissorRectangle2.X = num2;
			scissorRectangle2.Y = num3;
			scissorRectangle2.Height = num - 8;
			scissorRectangle2.Width = 200;
			b.GraphicsDevice.ScissorRectangle = scissorRectangle2;
			FarmerRenderer.isDrawingForUI = true;
			farmer.FarmerRenderer.draw(b, new FarmerSprite.AnimationFrame(farmer.bathingClothes.Value ? 108 : 0, 0, secondaryArm: false, flip: false), farmer.bathingClothes.Value ? 108 : 0, new Rectangle(0, farmer.bathingClothes.Value ? 576 : 0, 16, 32), new Vector2(num2, num3), Vector2.Zero, 0.8f, 2, Color.White, 0f, 1f, farmer);
			FarmerRenderer.isDrawingForUI = false;
			b.GraphicsDevice.ScissorRectangle = scissorRectangle;
			int num4 = num2 + 80;
			int num5 = num3 + 12;
			string text = ChatBox.formattedUserName(farmer);
			b.DrawString(Game1.dialogueFont, text, new Vector2(num4, num5), Color.White);
			string userName = Game1.multiplayer.getUserName(farmer.UniqueMultiplayerID);
			if (!string.IsNullOrEmpty(userName))
			{
				num5 += Game1.dialogueFont.LineSpacing + 4;
				string text2 = "(" + userName + ")";
				b.DrawString(Game1.smallFont, text2, new Vector2(num4, num5), Color.White);
			}
			num3 += num;
		}
		b.GraphicsDevice.ScissorRectangle = scissorRectangle;
		b.End();
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
	}
}
