using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StardewValley.Menus;

public class LocalCoopJoinMenu : IClickableMenu
{
	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		int maxSimultaneousPlayers = GameRunner.instance.GetMaxSimultaneousPlayers();
		if (GameRunner.instance.gameInstances.Count >= maxSimultaneousPlayers)
		{
			return;
		}
		for (PlayerIndex playerIndex = PlayerIndex.One; playerIndex <= PlayerIndex.Four; playerIndex++)
		{
			if (GameRunner.instance.gameInstances.Count >= maxSimultaneousPlayers)
			{
				break;
			}
			if (!GameRunner.instance.IsStartDown(playerIndex))
			{
				continue;
			}
			bool flag = false;
			foreach (Game1 gameInstance in GameRunner.instance.gameInstances)
			{
				if (gameInstance.instancePlayerOneIndex == playerIndex && !gameInstance.IsMainInstance)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				if (playerIndex == PlayerIndex.One)
				{
					GameRunner.instance.gameInstances[0].instancePlayerOneIndex = (PlayerIndex)(-1);
				}
				GameRunner.instance.AddGameInstance(playerIndex);
			}
		}
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		if (button == Buttons.B)
		{
			exitThisMenu();
		}
		else
		{
			base.receiveGamePadButton(button);
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape)
		{
			exitThisMenu();
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height), Color.Black * 0.75f);
		Vector2 vector = new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width / 2, Game1.graphics.GraphicsDevice.Viewport.Height / 2);
		SpriteFont smallFont = Game1.smallFont;
		string[] array = Game1.content.LoadString("Strings\\UI:LocalJoinPrompt").Split('*');
		Vector2 vector2 = smallFont.MeasureString(array[0]);
		vector2.X += 32f;
		int num = (int)vector2.X;
		vector2.X += smallFont.MeasureString(array[1]).X;
		vector2.Y = Math.Max(vector2.Y, smallFont.MeasureString(array[1]).Y);
		vector -= vector2 / 2f;
		int num2 = 32;
		int num3 = Math.Max((int)vector2.Y, 32);
		Game1.DrawBox((int)vector.X - num2, (int)vector.Y, (int)vector2.X + num2 * 2, num3);
		b.DrawString(smallFont, array[0], vector + new Vector2(4f, 4f), Game1.textShadowColor);
		b.DrawString(smallFont, array[1], vector + new Vector2(num, 0f) + new Vector2(4f, 4f), Game1.textShadowColor);
		Vector2 vector3 = vector + new Vector2(num - 16, 0f);
		vector3.Y += smallFont.MeasureString("XX").X / 2f;
		b.Draw(Game1.controllerMaps, vector3 + new Vector2(4f, 4f), Utility.controllerMapSourceRect(new Rectangle(653, 260, 28, 28)), Color.Black * 0.25f, 0f, new Vector2(14f, 14f), 1f, SpriteEffects.None, 0.99f);
		b.Draw(Game1.controllerMaps, vector3, Utility.controllerMapSourceRect(new Rectangle(653, 260, 28, 28)), Color.White, 0f, new Vector2(14f, 14f), 1f, SpriteEffects.None, 0.99f);
		b.DrawString(smallFont, array[0], vector, Game1.textColor);
		b.DrawString(smallFont, array[1], vector + new Vector2(num, 0f), Game1.textColor);
		string text = Game1.content.LoadString("Strings\\Locations:MineCart_Destination_Cancel");
		vector.Y -= vector2.Y / 2f;
		vector.Y += num3;
		if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko)
		{
			vector.Y += 48f;
		}
		else
		{
			vector.Y += 32f;
		}
		vector.X += vector2.X + (float)num2;
		vector.X -= smallFont.MeasureString(text).X;
		b.DrawString(smallFont, text, vector, Color.White);
		vector.X -= smallFont.MeasureString("XX").X;
		vector += smallFont.MeasureString("X") / 2f;
		b.Draw(Game1.controllerMaps, vector, Utility.controllerMapSourceRect(new Rectangle(569, 260, 28, 28)), Color.White, 0f, new Vector2(14f, 14f), 1f, SpriteEffects.None, 0.99f);
		if (!Game1.options.SnappyMenus)
		{
			drawMouse(b);
		}
	}
}
