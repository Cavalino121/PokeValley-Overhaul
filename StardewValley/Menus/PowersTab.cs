using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.GameData.Powers;
using StardewValley.TokenizableStrings;

namespace StardewValley.Menus;

public class PowersTab : IClickableMenu
{
	public const int region_forwardButton = 707;

	public const int region_backButton = 706;

	public const int distanceFromMenuBottomBeforeNewPage = 128;

	public int currentPage;

	public string descriptionText = "";

	public string hoverText = "";

	public ClickableTextureComponent backButton;

	public ClickableTextureComponent forwardButton;

	public List<List<ClickableTextureComponent>> powers;

	public PowersTab(int x, int y, int width, int height)
		: base(x, y, width, height)
	{
		backButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 48, yPositionOnScreen + height - 80, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
		{
			myID = 706,
			rightNeighborID = -7777
		};
		forwardButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 32 - 60, yPositionOnScreen + height - 80, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
		{
			myID = 707,
			leftNeighborID = -7777
		};
	}

	public override void snapToDefaultClickableComponent()
	{
		base.snapToDefaultClickableComponent();
		currentlySnappedComponent = getComponentWithID(0);
		snapCursorToCurrentSnappedComponent();
	}

	public override void populateClickableComponentList()
	{
		if (powers == null)
		{
			powers = new List<List<ClickableTextureComponent>>();
			Dictionary<string, PowersData> dictionary = null;
			try
			{
				dictionary = DataLoader.Powers(Game1.content);
			}
			catch (Exception)
			{
			}
			if (dictionary != null)
			{
				int num = 9;
				int num2 = 0;
				int num3 = xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearSideBorder;
				int num4 = yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - 16;
				foreach (KeyValuePair<string, PowersData> item in dictionary)
				{
					int x = num3 + num2 % num * 76;
					int num5 = num4 + num2 / num * 76;
					bool drawShadow = GameStateQuery.CheckConditions(item.Value.UnlockedCondition);
					string label = TokenParser.ParseText(item.Value.DisplayName) ?? item.Key;
					string text = TokenParser.ParseText(item.Value.Description) ?? "";
					Texture2D texture = Game1.content.Load<Texture2D>(item.Value.TexturePath);
					if (powers.Count == 0 || num5 > yPositionOnScreen + height - 128)
					{
						powers.Add(new List<ClickableTextureComponent>());
						num2 = 0;
						x = num3;
						num5 = num4;
					}
					List<ClickableTextureComponent> list = powers.Last();
					list.Add(new ClickableTextureComponent(item.Key, new Rectangle(x, num5, 64, 64), label, text, texture, new Rectangle(item.Value.TexturePosition.X, item.Value.TexturePosition.Y, 16, 16), 4f, drawShadow)
					{
						myID = list.Count,
						rightNeighborID = (((list.Count + 1) % num == 0) ? (-1) : (list.Count + 1)),
						leftNeighborID = ((list.Count % num == 0) ? (-1) : (list.Count - 1)),
						downNeighborID = ((num5 + 76 > yPositionOnScreen + height - 128) ? (-7777) : (list.Count + num)),
						upNeighborID = ((list.Count < num) ? 12346 : (list.Count - num)),
						fullyImmutable = true,
						drawLabel = false
					});
					num2++;
				}
			}
		}
		base.populateClickableComponentList();
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoverText = "";
		descriptionText = "";
		base.performHoverAction(x, y);
		foreach (ClickableTextureComponent item in powers[currentPage])
		{
			if (item.containsPoint(x, y))
			{
				item.scale = Math.Min(item.scale + 0.02f, item.baseScale + 0.1f);
				hoverText = (item.drawShadow ? item.label : "???");
				descriptionText = Game1.parseText(item.hoverText, Game1.smallFont, Math.Max((int)Game1.dialogueFont.MeasureString(hoverText).X, 320));
			}
			else
			{
				item.scale = Math.Max(item.scale - 0.02f, item.baseScale);
			}
		}
		forwardButton.tryHover(x, y, 0.5f);
		backButton.tryHover(x, y, 0.5f);
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (backButton.containsPoint(x, y) && currentPage > 0)
		{
			if (playSound)
			{
				Game1.playSound("shwip");
			}
			currentPage--;
		}
		else if (forwardButton.containsPoint(x, y) && currentPage < powers.Count - 1)
		{
			if (playSound)
			{
				Game1.playSound("shwip");
			}
			currentPage++;
		}
		else
		{
			base.receiveLeftClick(x, y, playSound);
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (currentPage > 0)
		{
			backButton.draw(b);
		}
		if (currentPage < powers.Count - 1)
		{
			forwardButton.draw(b);
		}
		b.End();
		b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
		foreach (ClickableTextureComponent item in powers[currentPage])
		{
			bool drawShadow = item.drawShadow;
			item.draw(b, drawShadow ? Color.White : (Color.Black * 0.2f), 0.86f);
		}
		b.End();
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		if (!descriptionText.Equals("") && hoverText != "???")
		{
			IClickableMenu.drawHoverText(b, descriptionText, Game1.smallFont, 0, 0, -1, hoverText);
		}
		else if (!hoverText.Equals(""))
		{
			IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);
		}
	}
}
