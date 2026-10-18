using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Characters;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.TokenizableStrings;

namespace StardewValley.Menus;

public class Billboard : IClickableMenu
{
	/// <summary>An event type that can be shown in the calendar.</summary>
	[Flags]
	public enum BillboardEventType
	{
		/// <summary>No event.</summary>
		None = 0,
		/// <summary>An NPC's birthday.</summary>
		Birthday = 1,
		/// <summary>A non-passive festival.</summary>
		Festival = 2,
		/// <summary>A fishing derby like Trophy Derby or Squidfest.</summary>
		FishingDerby = 4,
		/// <summary>A passive festival.</summary>
		PassiveFestival = 8,
		/// <summary>A wedding between a player and a player/NPC.</summary>
		Wedding = 0x10,
		/// <summary>A day that Marcello's Books will be in town</summary>
		Bookseller = 0x20
	}

	/// <summary>The cached data for a calendar day.</summary>
	public class BillboardDay
	{
		/// <summary>The event types on this day.</summary>
		public BillboardEventType Type { get; }

		/// <summary>The events on this day.</summary>
		public BillboardEvent[] Events { get; }

		/// <summary>The combined hover text for the events on this day.</summary>
		public string HoverText { get; }

		/// <summary>The texture to show for the calendar slot, if any.</summary>
		public Texture2D Texture { get; }

		/// <summary>The pixel area to draw within the <see cref="P:StardewValley.Menus.Billboard.BillboardDay.Texture" />, if applicable.</summary>
		public Rectangle TextureSourceRect { get; }

		/// <summary>Construct an instance.</summary>
		/// <param name="events">The events on this day.</param>
		public BillboardDay(BillboardEvent[] events)
		{
			Events = events;
			HoverText = string.Empty;
			foreach (BillboardEvent billboardEvent in events)
			{
				Type |= billboardEvent.Type;
				if (Texture == null && billboardEvent.Texture != null)
				{
					Texture = billboardEvent.Texture;
					TextureSourceRect = billboardEvent.TextureSourceRect;
				}
				HoverText = HoverText + billboardEvent.DisplayName + Environment.NewLine;
			}
			HoverText = HoverText.Trim();
		}

		public BillboardEvent GetEventOfType(BillboardEventType type)
		{
			BillboardEvent[] events = Events;
			foreach (BillboardEvent billboardEvent in events)
			{
				if (billboardEvent.Type == type)
				{
					return billboardEvent;
				}
			}
			return null;
		}
	}

	/// <summary>An event shown on the calendar.</summary>
	public class BillboardEvent
	{
		/// <summary>If this event is currently unavailable. (e.g. Desert festival before desert is open)</summary>
		public bool locked;

		/// <summary>The event type.</summary>
		public BillboardEventType Type { get; }

		/// <summary>The values related to the event (like the names of the players or NPCs getting married).</summary>
		public string[] Arguments { get; }

		/// <summary>The name to show on the calendar.</summary>
		public string DisplayName { get; }

		/// <summary>The texture to show for the calendar slot, if any.</summary>
		public Texture2D Texture { get; }

		/// <summary>The pixel area to draw within the <see cref="P:StardewValley.Menus.Billboard.BillboardEvent.Texture" />, if applicable.</summary>
		public Rectangle TextureSourceRect { get; }

		/// <summary>Construct an instance.</summary>
		/// <param name="type">The event type.</param>
		/// <param name="arguments">The values related to the event (like the names of the players or NPCs getting married).</param>
		/// <param name="displayName">The name to show on the calendar.</param>
		/// <param name="texture">The texture to show for the calendar slot, if any.</param>
		/// <param name="sourceRect">The pixel area to draw within the <paramref name="texture" />, if applicable.</param>
		public BillboardEvent(BillboardEventType type, string[] arguments, string displayName, Texture2D texture = null, Rectangle sourceRect = default(Rectangle))
		{
			Type = type;
			Arguments = arguments;
			DisplayName = displayName;
			Texture = texture;
			TextureSourceRect = sourceRect;
		}
	}

	private Texture2D billboardTexture;

	public const int basewidth = 338;

	public const int baseWidth_calendar = 301;

	public const int baseheight = 198;

	private bool dailyQuestBoard;

	public ClickableComponent acceptQuestButton;

	public List<ClickableTextureComponent> calendarDays;

	private string hoverText = "";

	private List<int> booksellerdays;

	/// <summary>The events to show on the calendar for each day.</summary>
	/// <remarks>This only has entries for days that have events.</remarks>
	public readonly Dictionary<int, BillboardDay> calendarDayData = new Dictionary<int, BillboardDay>();

	public Billboard(bool dailyQuest = false)
		: base(0, 0, 0, 0, showUpperRightCloseButton: true)
	{
		if (!Game1.player.hasOrWillReceiveMail("checkedBulletinOnce"))
		{
			Game1.player.mailReceived.Add("checkedBulletinOnce");
			Game1.RequireLocation<Town>("Town").checkedBoard();
		}
		dailyQuestBoard = dailyQuest;
		billboardTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\Billboard");
		width = (dailyQuest ? 338 : 301) * 4;
		height = 792;
		Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
		xPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.X;
		yPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.Y;
		if (!dailyQuest)
		{
			booksellerdays = Utility.getDaysOfBooksellerThisSeason();
		}
		acceptQuestButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 128, yPositionOnScreen + height - 128, (int)Game1.dialogueFont.MeasureString(Game1.content.LoadString("Strings\\UI:AcceptQuest")).X + 24, (int)Game1.dialogueFont.MeasureString(Game1.content.LoadString("Strings\\UI:AcceptQuest")).Y + 24), "")
		{
			myID = 0
		};
		UpdateDailyQuestButton();
		upperRightCloseButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 20, yPositionOnScreen, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);
		Game1.playSound("bigSelect");
		if (!dailyQuest)
		{
			calendarDays = new List<ClickableTextureComponent>();
			Dictionary<int, List<NPC>> birthdays = GetBirthdays();
			for (int i = 1; i <= 28; i++)
			{
				List<BillboardEvent> eventsForDay = GetEventsForDay(i, birthdays);
				if (eventsForDay.Count > 0)
				{
					calendarDayData[i] = new BillboardDay(eventsForDay.ToArray());
				}
				int num = i - 1;
				calendarDays.Add(new ClickableTextureComponent(i.ToString(), new Rectangle(xPositionOnScreen + 152 + num % 7 * 32 * 4, yPositionOnScreen + 200 + num / 7 * 32 * 4, 124, 124), string.Empty, string.Empty, null, Rectangle.Empty, 1f)
				{
					myID = i,
					rightNeighborID = ((i % 7 != 0) ? (i + 1) : (-1)),
					leftNeighborID = ((i % 7 != 1) ? (i - 1) : (-1)),
					downNeighborID = i + 7,
					upNeighborID = ((i > 7) ? (i - 7) : (-1))
				});
			}
		}
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	/// <summary>Get all NPC birthdays that should be shown on the calendar this month, indexed by day.</summary>
	public virtual Dictionary<int, List<NPC>> GetBirthdays()
	{
		HashSet<string> addedBirthdays = new HashSet<string>();
		Dictionary<int, List<NPC>> birthdays = new Dictionary<int, List<NPC>>();
		Utility.ForEachVillager(delegate(NPC npc)
		{
			if (npc.Birthday_Season != Game1.currentSeason)
			{
				return true;
			}
			CalendarBehavior? calendarBehavior = npc.GetData()?.Calendar;
			if (calendarBehavior == CalendarBehavior.HiddenAlways || (calendarBehavior == CalendarBehavior.HiddenUntilMet && !Game1.player.friendshipData.ContainsKey(npc.Name)))
			{
				return true;
			}
			if (addedBirthdays.Contains(npc.Name))
			{
				return true;
			}
			if (!birthdays.TryGetValue(npc.Birthday_Day, out var value))
			{
				value = (birthdays[npc.Birthday_Day] = new List<NPC>());
			}
			value.Add(npc);
			addedBirthdays.Add(npc.Name);
			return true;
		});
		return birthdays;
	}

	/// <summary>Get the events to show on a given calendar day.</summary>
	/// <param name="day">The day of month.</param>
	/// <param name="birthdays">A cached lookup of birthdays by day.</param>
	public virtual List<BillboardEvent> GetEventsForDay(int day, Dictionary<int, List<NPC>> birthdays)
	{
		List<BillboardEvent> list = new List<BillboardEvent>();
		if (Utility.isFestivalDay(day, Game1.season))
		{
			string text = Game1.currentSeason + day;
			string displayName = Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + text)["name"];
			list.Add(new BillboardEvent(BillboardEventType.Festival, new string[1] { text }, displayName));
		}
		if (Utility.TryGetPassiveFestivalDataForDay(day, Game1.season, null, out var id, out var data, ignoreConditionsCheck: true) && (data?.ShowOnCalendar ?? false))
		{
			string displayName2 = TokenParser.ParseText(data.DisplayName);
			if (!GameStateQuery.CheckConditions(data.Condition))
			{
				list.Add(new BillboardEvent(BillboardEventType.PassiveFestival, new string[1] { id }, "???")
				{
					locked = true
				});
			}
			else
			{
				list.Add(new BillboardEvent(BillboardEventType.PassiveFestival, new string[1] { id }, displayName2));
			}
		}
		if (Game1.IsSummer && (day == 20 || day == 21))
		{
			string displayName3 = Game1.content.LoadString("Strings\\1_6_Strings:TroutDerby");
			list.Add(new BillboardEvent(BillboardEventType.FishingDerby, LegacyShims.EmptyArray<string>(), displayName3));
		}
		else if (Game1.IsWinter && (day == 12 || day == 13))
		{
			string displayName4 = Game1.content.LoadString("Strings\\1_6_Strings:SquidFest");
			list.Add(new BillboardEvent(BillboardEventType.FishingDerby, LegacyShims.EmptyArray<string>(), displayName4));
		}
		if (booksellerdays.Contains(day))
		{
			string displayName5 = Game1.content.LoadString("Strings\\1_6_Strings:Bookseller");
			list.Add(new BillboardEvent(BillboardEventType.Bookseller, LegacyShims.EmptyArray<string>(), displayName5));
		}
		if (birthdays.TryGetValue(day, out var value))
		{
			foreach (NPC item in value)
			{
				char c = item.displayName.Last();
				string displayName6 = ((c == 's' || (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.de && (c == 'x' || c == 'ß' || c == 'z'))) ? Game1.content.LoadString("Strings\\UI:Billboard_SBirthday", item.displayName) : Game1.content.LoadString("Strings\\UI:Billboard_Birthday", item.displayName));
				Texture2D texture;
				try
				{
					texture = Game1.content.Load<Texture2D>("Characters\\" + item.getTextureName());
				}
				catch
				{
					texture = item.Sprite.Texture;
				}
				list.Add(new BillboardEvent(BillboardEventType.Birthday, new string[1] { item.Name }, displayName6, texture, item.getMugShotSourceRect()));
			}
		}
		HashSet<Farmer> hashSet = new HashSet<Farmer>();
		FarmerCollection onlineFarmers = Game1.getOnlineFarmers();
		foreach (Farmer item2 in onlineFarmers)
		{
			if (hashSet.Contains(item2) || !item2.isEngaged() || item2.hasCurrentOrPendingRoommate())
			{
				continue;
			}
			string text2 = null;
			WorldDate worldDate = null;
			NPC characterFromName = Game1.getCharacterFromName(item2.spouse);
			if (characterFromName != null)
			{
				worldDate = item2.friendshipData[item2.spouse].WeddingDate;
				text2 = characterFromName.displayName;
			}
			else
			{
				long? spouse = item2.team.GetSpouse(item2.UniqueMultiplayerID);
				if (spouse.HasValue)
				{
					Farmer player = Game1.GetPlayer(spouse.Value);
					if (player != null && onlineFarmers.Contains(player))
					{
						worldDate = item2.team.GetFriendship(item2.UniqueMultiplayerID, spouse.Value).WeddingDate;
						hashSet.Add(player);
						text2 = player.Name;
					}
				}
			}
			if (!(worldDate == null))
			{
				if (worldDate.TotalDays < Game1.Date.TotalDays)
				{
					worldDate = new WorldDate(Game1.Date);
					worldDate.TotalDays++;
				}
				if (worldDate?.TotalDays >= Game1.Date.TotalDays && Game1.season == worldDate.Season && day == worldDate.DayOfMonth)
				{
					list.Add(new BillboardEvent(BillboardEventType.Wedding, new string[2] { item2.Name, text2 }, Game1.content.LoadString("Strings\\UI:Calendar_Wedding", item2.Name, text2)));
					hashSet.Add(item2);
				}
			}
		}
		return list;
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID((!dailyQuestBoard) ? 1 : 0);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		Game1.activeClickableMenu = new Billboard(dailyQuestBoard);
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		Game1.playSound("bigDeSelect");
		exitThisMenu();
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);
		if (acceptQuestButton.visible && acceptQuestButton.containsPoint(x, y))
		{
			Game1.playSound("newArtifact");
			Game1.questOfTheDay.dailyQuest.Value = true;
			Game1.questOfTheDay.dayQuestAccepted.Value = Game1.Date.TotalDays;
			Game1.questOfTheDay.accepted.Value = true;
			Game1.questOfTheDay.canBeCancelled.Value = true;
			Game1.questOfTheDay.daysLeft.Value = 2;
			Game1.player.questLog.Add(Game1.questOfTheDay);
			Game1.player.acceptedDailyQuest.Set(newValue: true);
			UpdateDailyQuestButton();
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		hoverText = "";
		if (dailyQuestBoard && Game1.questOfTheDay != null && !Game1.questOfTheDay.accepted.Value)
		{
			float scale = acceptQuestButton.scale;
			acceptQuestButton.scale = (acceptQuestButton.bounds.Contains(x, y) ? 1.5f : 1f);
			if (acceptQuestButton.scale > scale)
			{
				Game1.playSound("Cowboy_gunshot");
			}
		}
		if (calendarDays == null)
		{
			return;
		}
		foreach (ClickableTextureComponent calendarDay in calendarDays)
		{
			if (calendarDay.bounds.Contains(x, y))
			{
				hoverText = (calendarDayData.TryGetValue(calendarDay.myID, out var value) ? value.HoverText : string.Empty);
				break;
			}
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		bool flag = false;
		if (!Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
		}
		b.Draw(billboardTexture, new Vector2(xPositionOnScreen, yPositionOnScreen), dailyQuestBoard ? new Rectangle(0, 0, 338, 198) : new Rectangle(0, 198, 301, 198), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		if (!dailyQuestBoard)
		{
			b.DrawString(Game1.dialogueFont, Utility.getSeasonNameFromNumber(Game1.seasonIndex), new Vector2(xPositionOnScreen + 160, yPositionOnScreen + 80), Game1.textColor);
			b.DrawString(Game1.dialogueFont, Game1.content.LoadString("Strings\\UI:Billboard_Year", Game1.year), new Vector2(xPositionOnScreen + 448, yPositionOnScreen + 80), Game1.textColor);
			for (int i = 0; i < calendarDays.Count; i++)
			{
				ClickableTextureComponent clickableTextureComponent = calendarDays[i];
				if (calendarDayData.TryGetValue(clickableTextureComponent.myID, out var value))
				{
					if (value.Texture != null)
					{
						b.Draw(value.Texture, new Vector2(clickableTextureComponent.bounds.X + 48, clickableTextureComponent.bounds.Y + 28), value.TextureSourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
					}
					if (value.Type.HasFlag(BillboardEventType.PassiveFestival))
					{
						Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(clickableTextureComponent.bounds.X + 12, (float)(clickableTextureComponent.bounds.Y + 60) - Game1.dialogueButtonScale / 2f), new Rectangle(346, 392, 8, 8), value.GetEventOfType(BillboardEventType.PassiveFestival).locked ? (Color.Black * 0.3f) : Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
					}
					if (value.Type.HasFlag(BillboardEventType.Festival))
					{
						Utility.drawWithShadow(b, billboardTexture, new Vector2(clickableTextureComponent.bounds.X + 40, (float)(clickableTextureComponent.bounds.Y + 56) - Game1.dialogueButtonScale / 2f), new Rectangle(1 + (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 600.0 / 100.0) * 14, 398, 14, 12), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
					}
					if (value.Type.HasFlag(BillboardEventType.FishingDerby))
					{
						Utility.drawWithShadow(b, Game1.mouseCursors_1_6, new Vector2(calendarDays[i].bounds.X + 8, (float)(calendarDays[i].bounds.Y + 60) - Game1.dialogueButtonScale / 2f), new Rectangle(103, 2, 10, 11), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
					}
					if (value.Type.HasFlag(BillboardEventType.Wedding))
					{
						b.Draw(Game1.mouseCursors2, new Vector2(clickableTextureComponent.bounds.Right - 56, clickableTextureComponent.bounds.Top - 12), new Rectangle(112, 32, 16, 14), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
					}
					if (value.Type.HasFlag(BillboardEventType.Bookseller))
					{
						b.Draw(Game1.mouseCursors_1_6, new Vector2((float)(clickableTextureComponent.bounds.Right - 72) - 2f * (float)Math.Sin((Game1.currentGameTime.TotalGameTime.TotalSeconds + (double)i * 0.3) * 3.0), (float)(clickableTextureComponent.bounds.Top + 52) - 2f * (float)Math.Cos((Game1.currentGameTime.TotalGameTime.TotalSeconds + (double)i * 0.3) * 2.0)), new Rectangle(71, 63, 8, 15), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
					}
				}
				if (Game1.dayOfMonth > i + 1)
				{
					b.Draw(Game1.staminaRect, clickableTextureComponent.bounds, Color.Gray * 0.25f);
				}
				else if (Game1.dayOfMonth == i + 1)
				{
					int num = (int)(4f * Game1.dialogueButtonScale / 8f);
					IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(379, 357, 3, 3), clickableTextureComponent.bounds.X - num, clickableTextureComponent.bounds.Y - num, clickableTextureComponent.bounds.Width + num * 2, clickableTextureComponent.bounds.Height + num * 2, Color.Blue, 4f, drawShadow: false);
				}
			}
		}
		else
		{
			if (Game1.options.SnappyMenus)
			{
				flag = true;
			}
			if (string.IsNullOrEmpty(Game1.questOfTheDay?.currentObjective))
			{
				b.DrawString(Game1.dialogueFont, Game1.content.LoadString("Strings\\UI:Billboard_NothingPosted"), new Vector2(xPositionOnScreen + 384, yPositionOnScreen + 320), Game1.textColor);
			}
			else
			{
				SpriteFont spriteFont = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko) ? Game1.smallFont : Game1.dialogueFont);
				string text = Game1.parseText(Game1.questOfTheDay.questDescription, spriteFont, 640);
				Utility.drawTextWithShadow(b, text, spriteFont, new Vector2(xPositionOnScreen + 320 + 32, yPositionOnScreen + 256), Game1.textColor, 1f, -1f, -1, -1, 0.5f);
				if (acceptQuestButton.visible)
				{
					flag = false;
					IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9), acceptQuestButton.bounds.X, acceptQuestButton.bounds.Y, acceptQuestButton.bounds.Width, acceptQuestButton.bounds.Height, (acceptQuestButton.scale > 1f) ? Color.LightPink : Color.White, 4f * acceptQuestButton.scale);
					Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:AcceptQuest"), Game1.dialogueFont, new Vector2(acceptQuestButton.bounds.X + 12, acceptQuestButton.bounds.Y + (LocalizedContentManager.CurrentLanguageLatin ? 16 : 12)), Game1.textColor);
				}
				if (Game1.stats.Get("BillboardQuestsDone") % 3 == 2 && (acceptQuestButton.visible || !Game1.questOfTheDay.completed.Value))
				{
					Utility.drawWithShadow(b, Game1.content.Load<Texture2D>("TileSheets\\Objects_2"), base.Position + new Vector2(215f, 144f) * 4f, new Rectangle(80, 128, 16, 16), Color.White, 0f, Vector2.Zero, 4f);
					SpriteText.drawString(b, "x1", (int)base.Position.X + 936, (int)base.Position.Y + 596);
				}
			}
			bool flag2 = Game1.stats.Get("BillboardQuestsDone") % 3 == 0 && Game1.questOfTheDay != null && Game1.questOfTheDay.completed.Value;
			for (int j = 0; j < (flag2 ? 3 : (Game1.stats.Get("BillboardQuestsDone") % 3)); j++)
			{
				b.Draw(billboardTexture, base.Position + new Vector2(18 + 12 * j, 36f) * 4f, new Rectangle(140, 397, 10, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.6f);
			}
			if (Game1.player.hasCompletedCommunityCenter())
			{
				b.Draw(billboardTexture, base.Position + new Vector2(290f, 59f) * 4f, new Rectangle(0, 427, 39, 54), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.6f);
			}
		}
		base.draw(b);
		if (!flag)
		{
			Game1.mouseCursorTransparency = 1f;
			drawMouse(b);
			if (hoverText.Length > 0)
			{
				IClickableMenu.drawHoverText(b, hoverText, Game1.dialogueFont);
			}
		}
	}

	public void UpdateDailyQuestButton()
	{
		if (acceptQuestButton != null)
		{
			if (!dailyQuestBoard)
			{
				acceptQuestButton.visible = false;
			}
			else
			{
				acceptQuestButton.visible = Game1.CanAcceptDailyQuest();
			}
		}
	}
}
