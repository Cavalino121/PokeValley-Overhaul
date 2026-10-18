using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Buffs;
using StardewValley.Extensions;

namespace StardewValley.Menus;

public class BuffsDisplay : IClickableMenu
{
	/// <summary>The buff attributes shown for buffs which don't have their own icon or description.</summary>
	/// <remarks>For example, a food buff which adds +2 fishing and +1 luck will show two buff icons using this data. A buff which has its own icon but no description will show a single icon with a combined description based on this data.</remarks>
	public static readonly List<BuffAttributeDisplay> displayAttributes = new List<BuffAttributeDisplay>
	{
		new BuffAttributeDisplay(0, (BuffEffects buff) => buff.FarmingLevel, "Strings\\StringsFromCSFiles:Buff.cs.480"),
		new BuffAttributeDisplay(1, (BuffEffects buff) => buff.FishingLevel, "Strings\\StringsFromCSFiles:Buff.cs.483"),
		new BuffAttributeDisplay(2, (BuffEffects buff) => buff.MiningLevel, "Strings\\StringsFromCSFiles:Buff.cs.486"),
		new BuffAttributeDisplay(4, (BuffEffects buff) => buff.LuckLevel, "Strings\\StringsFromCSFiles:Buff.cs.489"),
		new BuffAttributeDisplay(5, (BuffEffects buff) => buff.ForagingLevel, "Strings\\StringsFromCSFiles:Buff.cs.492"),
		new BuffAttributeDisplay(16, (BuffEffects buff) => buff.MaxStamina, "Strings\\StringsFromCSFiles:Buff.cs.495"),
		new BuffAttributeDisplay(11, (BuffEffects buff) => buff.Attack, "Strings\\StringsFromCSFiles:Buff.cs.504"),
		new BuffAttributeDisplay(8, (BuffEffects buff) => buff.MagneticRadius, "Strings\\StringsFromCSFiles:Buff.cs.498"),
		new BuffAttributeDisplay(10, (BuffEffects buff) => buff.Defense, "Strings\\StringsFromCSFiles:Buff.cs.501"),
		new BuffAttributeDisplay(9, (BuffEffects buff) => buff.Speed, "Strings\\StringsFromCSFiles:Buff.cs.507")
	};

	private readonly Dictionary<ClickableTextureComponent, Buff> buffs = new Dictionary<ClickableTextureComponent, Buff>();

	/// <summary>The buff IDs added or renewed since the last icon render.</summary>
	public readonly HashSet<string> updatedIDs = new HashSet<string>();

	public bool dirty;

	public string hoverText = "";

	public BuffsDisplay()
	{
		updatePosition();
	}

	private void updatePosition()
	{
		Rectangle titleSafeArea = Game1.game1.GraphicsDevice.Viewport.GetTitleSafeArea();
		int num = 288;
		int num2 = 64;
		int num3 = titleSafeArea.Right - 300 - width;
		int num4 = titleSafeArea.Top + 8;
		if (num3 != xPositionOnScreen || num4 != yPositionOnScreen || num != width || num2 != height)
		{
			xPositionOnScreen = num3;
			yPositionOnScreen = num4;
			width = num;
			height = num2;
			resetIcons();
		}
	}

	public override bool isWithinBounds(int x, int y)
	{
		foreach (KeyValuePair<ClickableTextureComponent, Buff> buff in buffs)
		{
			if (buff.Key.containsPoint(x, y))
			{
				return true;
			}
		}
		return false;
	}

	public int getNumBuffs()
	{
		if (buffs == null)
		{
			return 0;
		}
		return buffs.Count;
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoverText = "";
		foreach (KeyValuePair<ClickableTextureComponent, Buff> buff in buffs)
		{
			if (buff.Key.containsPoint(x, y))
			{
				hoverText = buff.Key.hoverText + ((buff.Value.millisecondsDuration != -2) ? (Environment.NewLine + buff.Value.getTimeLeft()) : "");
				string format = hoverText;
				object[] buffDescriptionTextReplacement = getBuffDescriptionTextReplacement(buff.Value.id);
				hoverText = string.Format(format, buffDescriptionTextReplacement);
				buff.Key.scale = Math.Min(buff.Key.baseScale + 0.1f, buff.Key.scale + 0.02f);
				break;
			}
		}
	}

	public string[] getBuffDescriptionTextReplacement(string buffName)
	{
		if (buffName == "statue_of_blessings_3")
		{
			return new string[1] { Game1.player.stats.Get("blessingOfWaters").ToString() };
		}
		return LegacyShims.EmptyArray<string>();
	}

	public void arrangeTheseComponentsInThisRectangle(int rectangleX, int rectangleY, int rectangleWidthInComponentWidthUnits, int componentWidth, int componentHeight, int buffer, bool rightToLeft)
	{
		int num = 0;
		int num2 = 0;
		foreach (KeyValuePair<ClickableTextureComponent, Buff> buff in buffs)
		{
			ClickableTextureComponent key = buff.Key;
			if (rightToLeft)
			{
				key.bounds = new Rectangle(rectangleX + rectangleWidthInComponentWidthUnits * componentWidth - (num + 1) * (componentWidth + buffer), rectangleY + num2 * (componentHeight + buffer), componentWidth, componentHeight);
			}
			else
			{
				key.bounds = new Rectangle(rectangleX + num * (componentWidth + buffer), rectangleY + num2 * (componentHeight + buffer), componentWidth, componentHeight);
			}
			num++;
			if (num > rectangleWidthInComponentWidthUnits)
			{
				num2++;
				num = 0;
			}
		}
	}

	protected virtual void resetIcons()
	{
		buffs.Clear();
		if (Game1.player == null)
		{
			return;
		}
		IDictionary<string, float> dictionary = new Dictionary<string, float>();
		foreach (KeyValuePair<ClickableTextureComponent, Buff> buff in buffs)
		{
			dictionary[buff.Value.id] = buff.Key.scale;
		}
		foreach (Buff sortedBuff in GetSortedBuffs())
		{
			if (!sortedBuff.visible)
			{
				continue;
			}
			bool flag = updatedIDs.Contains(sortedBuff.id);
			foreach (ClickableTextureComponent clickableComponent in getClickableComponents(sortedBuff))
			{
				float value;
				if (flag)
				{
					clickableComponent.scale = clickableComponent.baseScale + 0.2f;
				}
				else if (dictionary.TryGetValue(sortedBuff.id, out value))
				{
					clickableComponent.scale = Math.Max(clickableComponent.baseScale, value);
				}
				buffs.Add(clickableComponent, sortedBuff);
			}
		}
		updatedIDs.Clear();
		arrangeTheseComponentsInThisRectangle(xPositionOnScreen, yPositionOnScreen, width / 64, 64, 64, 8, rightToLeft: true);
	}

	public new void update(GameTime time)
	{
		if (dirty)
		{
			resetIcons();
			dirty = false;
		}
		if (!Game1.wasMouseVisibleThisFrame)
		{
			hoverText = "";
		}
		foreach (KeyValuePair<ClickableTextureComponent, Buff> buff in buffs)
		{
			ClickableTextureComponent key = buff.Key;
			Buff value = buff.Value;
			key.scale = Math.Max(key.baseScale, key.scale - 0.01f);
			if (!value.alreadyUpdatedIconAlpha && (float)value.millisecondsDuration < Math.Min(10000f, (float)value.totalMillisecondsDuration / 10f) && value.millisecondsDuration != -2)
			{
				value.displayAlphaTimer += (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds / (((float)value.millisecondsDuration < Math.Min(2000f, (float)value.totalMillisecondsDuration / 20f)) ? 1f : 2f);
				value.alreadyUpdatedIconAlpha = true;
			}
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		updatePosition();
		foreach (KeyValuePair<ClickableTextureComponent, Buff> buff in buffs)
		{
			buff.Key.draw(b, Color.White * ((buff.Value.displayAlphaTimer > 0f) ? ((float)(Math.Cos(buff.Value.displayAlphaTimer / 100f) + 3.0) / 4f) : 1f), 0.8f);
			buff.Value.alreadyUpdatedIconAlpha = false;
		}
		if (hoverText.Length != 0 && isWithinBounds(Game1.getOldMouseX(), Game1.getOldMouseY()))
		{
			performHoverAction(Game1.getOldMouseX(), Game1.getOldMouseY());
			IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);
		}
	}

	public IEnumerable<Buff> GetSortedBuffs()
	{
		return from p in Game1.player.buffs.AppliedBuffs.Values
			orderby p.id == "food" descending, p.id == "drink" descending
			select p;
	}

	protected virtual string getDescription(Buff buff)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string displayName = buff.displayName;
		if (displayName != null && displayName.Length > 1)
		{
			stringBuilder.AppendLine(buff.displayName);
			stringBuilder.AppendLine("[line]");
		}
		string description = buff.description;
		if (description != null && description.Length > 1)
		{
			stringBuilder.AppendLine(buff.description);
		}
		foreach (BuffAttributeDisplay displayAttribute in displayAttributes)
		{
			string description2 = getDescription(buff, displayAttribute, withSource: false);
			if (description2 != null)
			{
				stringBuilder.AppendLine(description2);
			}
		}
		string sourceLine = getSourceLine(buff);
		if (sourceLine != null)
		{
			stringBuilder.AppendLine(sourceLine);
		}
		return stringBuilder.ToString().TrimEnd();
	}

	protected virtual string getDescription(Buff buff, BuffAttributeDisplay attribute, bool withSource)
	{
		float num = attribute.Value(buff);
		if (num == 0f)
		{
			return null;
		}
		string text = attribute.Description(num);
		if (withSource)
		{
			string sourceLine = getSourceLine(buff);
			if (sourceLine != null)
			{
				text = text + "\n" + sourceLine;
			}
		}
		return text;
	}

	protected virtual string getSourceLine(Buff buff)
	{
		string text = buff.displaySource ?? buff.source;
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		return Game1.content.LoadString("Strings\\StringsFromCSFiles:Buff.cs.508") + text;
	}

	public virtual IEnumerable<ClickableTextureComponent> getClickableComponents(Buff buff)
	{
		if (!buff.visible)
		{
			yield break;
		}
		if (buff.iconTexture != null)
		{
			Rectangle sourceRectForStandardTileSheet = Game1.getSourceRectForStandardTileSheet(buff.iconTexture, buff.iconSheetIndex, 16, 16);
			yield return new ClickableTextureComponent("", Rectangle.Empty, null, getDescription(buff), buff.iconTexture, sourceRectForStandardTileSheet, 4f);
			yield break;
		}
		foreach (BuffAttributeDisplay displayAttribute in displayAttributes)
		{
			string description = getDescription(buff, displayAttribute, withSource: true);
			if (description != null)
			{
				Rectangle sourceRectForStandardTileSheet2 = Game1.getSourceRectForStandardTileSheet(displayAttribute.Texture(), displayAttribute.SpriteIndex, 16, 16);
				yield return new ClickableTextureComponent("", Rectangle.Empty, null, description, displayAttribute.Texture(), sourceRectForStandardTileSheet2, 4f);
			}
		}
	}
}
