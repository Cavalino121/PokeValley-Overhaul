using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StardewValley.Menus;

public class AnimationPreviewTool : IClickableMenu
{
	public List<List<ClickableTextureComponent>> components;

	public Rectangle scrollView;

	public List<ClickableTextureComponent> animationButtons;

	public ClickableTextureComponent okButton;

	public ClickableTextureComponent hairLabel;

	public ClickableTextureComponent shirtLabel;

	public ClickableTextureComponent pantsLabel;

	public float scrollY;

	public AnimationPreviewTool()
		: base(Game1.uiViewport.Width / 2 - (632 + IClickableMenu.borderWidth * 2) / 2, Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2 - 64, 632 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2 + 64)
	{
		Game1.player.faceDirection(2);
		Game1.player.FarmerSprite.StopAnimation();
		FieldInfo[] fields = typeof(FarmerSprite).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
		animationButtons = new List<ClickableTextureComponent>();
		foreach (FieldInfo item2 in fields.Where((FieldInfo fi) => fi.IsLiteral && !fi.IsInitOnly))
		{
			ClickableTextureComponent item = new ClickableTextureComponent(new Rectangle(0, 0, 200, 48), null, default(Rectangle), 1f)
			{
				myID = (int)item2.GetValue(null),
				name = item2.Name
			};
			animationButtons.Add(item);
		}
		okButton = new ClickableTextureComponent("OK", new Rectangle(xPositionOnScreen + width - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 64, yPositionOnScreen + height - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder + 16, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
		{
			upNeighborID = -99998,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			downNeighborID = -99998
		};
		components = new List<List<ClickableTextureComponent>>();
		components.Add(new List<ClickableTextureComponent>(new ClickableTextureComponent[1]
		{
			new ClickableTextureComponent("Hair Heading", new Rectangle(0, 0, 64, 16), "Hair", "", null, default(Rectangle), 1f)
		}));
		hairLabel = new ClickableTextureComponent("Hair Label", new Rectangle(0, 0, 64, 64), "0", "", null, default(Rectangle), 1f);
		components.Add(new List<ClickableTextureComponent>(new ClickableTextureComponent[3]
		{
			new ClickableTextureComponent("Hair Style", new Rectangle(0, 0, 64, 64), null, "", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 44), 1f)
			{
				myID = -1
			},
			hairLabel,
			new ClickableTextureComponent("Hair Style", new Rectangle(0, 0, 64, 64), null, "", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 33), 1f)
			{
				myID = 1
			}
		}));
		components.Add(new List<ClickableTextureComponent>(new ClickableTextureComponent[1]
		{
			new ClickableTextureComponent("Shirt Heading", new Rectangle(0, 0, 64, 16), "Shirt", "", null, default(Rectangle), 1f)
		}));
		shirtLabel = new ClickableTextureComponent("Shirt Label", new Rectangle(0, 0, 64, 64), "0", "", null, default(Rectangle), 1f);
		components.Add(new List<ClickableTextureComponent>(new ClickableTextureComponent[3]
		{
			new ClickableTextureComponent("Shirt Style", new Rectangle(0, 0, 64, 64), null, "", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 44), 1f)
			{
				myID = -1
			},
			shirtLabel,
			new ClickableTextureComponent("Shirt Style", new Rectangle(0, 0, 64, 64), null, "", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 33), 1f)
			{
				myID = 1
			}
		}));
		components.Add(new List<ClickableTextureComponent>(new ClickableTextureComponent[1]
		{
			new ClickableTextureComponent("Pants Heading", new Rectangle(0, 0, 64, 16), "Pants", "", null, default(Rectangle), 1f)
		}));
		pantsLabel = new ClickableTextureComponent("Pants Label", new Rectangle(0, 0, 64, 64), "0", "", null, default(Rectangle), 1f);
		components.Add(new List<ClickableTextureComponent>(new ClickableTextureComponent[3]
		{
			new ClickableTextureComponent("Pants Style", new Rectangle(0, 0, 64, 64), null, "", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 44), 1f)
			{
				myID = -1
			},
			pantsLabel,
			new ClickableTextureComponent("Pants Style", new Rectangle(0, 0, 64, 64), null, "", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 33), 1f)
			{
				myID = 1
			}
		}));
		components.Add(new List<ClickableTextureComponent>(new ClickableTextureComponent[1]
		{
			new ClickableTextureComponent("Toggle Gender", new Rectangle(0, 0, 64, 64), "Toggle Gender", "", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 25), 1f)
		}));
		RepositionElements();
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		xPositionOnScreen = Game1.uiViewport.Width / 2 - (632 + IClickableMenu.borderWidth * 2) / 2;
		yPositionOnScreen = Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2 - 64;
		RepositionElements();
	}

	public void SwitchShirt(int direction)
	{
		Game1.player.rotateShirt(direction);
		UpdateLabels();
	}

	public void SwitchHair(int direction)
	{
		Game1.player.changeHairStyle(Game1.player.hair.Value + direction);
		UpdateLabels();
	}

	public void SwitchPants(int direction)
	{
		Game1.player.rotatePantStyle(direction);
		UpdateLabels();
	}

	private void RepositionElements()
	{
		scrollView = new Rectangle(xPositionOnScreen + 320, yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder, 250, 500);
		if (scrollView.Left < Game1.graphics.GraphicsDevice.ScissorRectangle.Left)
		{
			int num = Game1.graphics.GraphicsDevice.ScissorRectangle.Left - scrollView.Left;
			scrollView.X += num;
			scrollView.Width -= num;
		}
		if (scrollView.Right > Game1.graphics.GraphicsDevice.ScissorRectangle.Right)
		{
			int num2 = scrollView.Right - Game1.graphics.GraphicsDevice.ScissorRectangle.Right;
			scrollView.X -= num2;
			scrollView.Width -= num2;
		}
		if (scrollView.Top < Game1.graphics.GraphicsDevice.ScissorRectangle.Top)
		{
			int num3 = Game1.graphics.GraphicsDevice.ScissorRectangle.Top - scrollView.Top;
			scrollView.Y += num3;
			scrollView.Width -= num3;
		}
		if (scrollView.Bottom > Game1.graphics.GraphicsDevice.ScissorRectangle.Bottom)
		{
			int num4 = scrollView.Bottom - Game1.graphics.GraphicsDevice.ScissorRectangle.Bottom;
			scrollView.Y -= num4;
			scrollView.Width -= num4;
		}
		int num5 = yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 200;
		foreach (List<ClickableTextureComponent> component in components)
		{
			int num6 = xPositionOnScreen + 70;
			int num7 = 0;
			foreach (ClickableTextureComponent item in component)
			{
				item.bounds.X = num6;
				item.bounds.Y = num5;
				num6 += item.bounds.Width + 8;
				num7 = Math.Max(item.bounds.Height, num7);
			}
			num5 += num7 + 8;
		}
		RepositionScrollElements();
		UpdateLabels();
	}

	public void UpdateLabels()
	{
		pantsLabel.label = Game1.player.GetPantsIndex().ToString() ?? "";
		shirtLabel.label = Game1.player.GetShirtIndex().ToString() ?? "";
		hairLabel.label = Game1.player.getHair().ToString() ?? "";
	}

	public void RepositionScrollElements()
	{
		int num = (int)scrollY;
		if (scrollY > 0f)
		{
			scrollY = 0f;
		}
		foreach (ClickableTextureComponent animationButton in animationButtons)
		{
			animationButton.bounds.X = scrollView.X;
			animationButton.bounds.Y = scrollView.Y + num;
			animationButton.bounds.Width = scrollView.Width;
			num += animationButton.bounds.Height;
			if (scrollView.Intersects(animationButton.bounds))
			{
				animationButton.visible = true;
			}
			else
			{
				animationButton.visible = false;
			}
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		foreach (ClickableTextureComponent animationButton in animationButtons)
		{
			if (animationButton.bounds.Contains(x, y) && scrollView.Contains(x, y))
			{
				if (animationButton.name.Contains("Left"))
				{
					Game1.player.faceDirection(3);
				}
				else if (animationButton.name.Contains("Right"))
				{
					Game1.player.faceDirection(1);
				}
				else if (animationButton.name.Contains("Up"))
				{
					Game1.player.faceDirection(0);
				}
				else
				{
					Game1.player.faceDirection(2);
				}
				Game1.player.completelyStopAnimatingOrDoingAction();
				Game1.player.animateOnce(animationButton.myID);
			}
		}
		foreach (List<ClickableTextureComponent> component in components)
		{
			foreach (ClickableTextureComponent item in component)
			{
				if (item.containsPoint(x, y))
				{
					switch (item.name)
					{
					case "Shirt Style":
						SwitchShirt(item.myID);
						break;
					case "Pants Style":
						SwitchPants(item.myID);
						break;
					case "Hair Style":
						SwitchHair(item.myID);
						break;
					case "Toggle Gender":
						Game1.player.changeGender(!Game1.player.IsMale);
						break;
					}
				}
			}
		}
		if (okButton.containsPoint(x, y))
		{
			exitThisMenu();
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		scrollY += direction;
		RepositionScrollElements();
		base.receiveScrollWheelAction(direction);
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
	}

	public bool canLeaveMenu()
	{
		return true;
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, speaker: false, drawOnlyBox: true);
		b.Draw(Game1.daybg, new Vector2(xPositionOnScreen + 64 + 42 - 2, yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - 16), Color.White);
		Game1.player.FarmerRenderer.draw(b, Game1.player.FarmerSprite.CurrentAnimationFrame, Game1.player.FarmerSprite.CurrentFrame, Game1.player.FarmerSprite.SourceRect, new Vector2(xPositionOnScreen - 2 + 42 + 128 - 32, yPositionOnScreen + IClickableMenu.borderWidth - 16 + IClickableMenu.spaceToClearTopBorder + 32), Vector2.Zero, 0.8f, Color.White, 0f, 1f, Game1.player);
		b.End();
		Rectangle scissorRectangle = b.GraphicsDevice.ScissorRectangle;
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, Utility.ScissorEnabled);
		b.GraphicsDevice.ScissorRectangle = scrollView;
		foreach (ClickableTextureComponent animationButton in animationButtons)
		{
			if (animationButton.visible)
			{
				Game1.DrawBox(animationButton.bounds.X, animationButton.bounds.Y, animationButton.bounds.Width, animationButton.bounds.Height);
				Utility.drawTextWithShadow(b, animationButton.name, Game1.smallFont, new Vector2(animationButton.bounds.X, animationButton.bounds.Y), Color.Black);
			}
		}
		b.End();
		b.GraphicsDevice.ScissorRectangle = scissorRectangle;
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		foreach (List<ClickableTextureComponent> component in components)
		{
			foreach (ClickableTextureComponent item in component)
			{
				item.draw(b);
			}
		}
		okButton.draw(b);
		drawMouse(b);
	}
}
