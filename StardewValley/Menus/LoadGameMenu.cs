using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.SaveSerialization;

namespace StardewValley.Menus;

public class LoadGameMenu : IClickableMenu, IDisposable
{
	public abstract class MenuSlot : IDisposable
	{
		public int ActivateDelay;

		protected LoadGameMenu menu;

		public MenuSlot(LoadGameMenu menu)
		{
			this.menu = menu;
		}

		public abstract void Activate();

		public abstract void Draw(SpriteBatch b, int i);

		public virtual void Dispose()
		{
		}
	}

	public class SaveFileSlot : MenuSlot
	{
		/// <summary>The farmer to load for this save slot.</summary>
		public Farmer Farmer;

		/// <summary>The slot number to display for this slot, or <c>null</c> to calculate it automatically from its position in the list.</summary>
		public int? SlotNumber;

		public double redTimer;

		public int versionComparison;

		public SaveFileSlot(LoadGameMenu menu, Farmer farmer, int? slotNumber)
			: base(menu)
		{
			ActivateDelay = 2150;
			Farmer = farmer;
			SlotNumber = slotNumber;
			versionComparison = Utility.CompareGameVersions(Game1.version, farmer.gameVersion, ignore_platform_specific: true);
		}

		public override void Activate()
		{
			SaveGame.Load(Farmer.slotName);
			Game1.exitActiveMenu();
		}

		protected virtual void drawSlotSaveNumber(SpriteBatch b, int i)
		{
			MenuSlot menuSlot = menu.MenuSlots[menu.currentItemIndex + i];
			ClickableComponent clickableComponent = menu.slotButtons[i];
			string s = ((menuSlot as SaveFileSlot)?.SlotNumber ?? (menu.currentItemIndex + i + 1)) + ".";
			SpriteText.drawString(b, s, clickableComponent.bounds.X + 28 + 32 - SpriteText.getWidthOfString(s) / 2, clickableComponent.bounds.Y + 36);
		}

		protected virtual string slotName()
		{
			return Farmer.Name;
		}

		public virtual float getSlotAlpha()
		{
			return 1f;
		}

		protected virtual void drawSlotName(SpriteBatch b, int i)
		{
			SpriteText.drawString(b, slotName(), menu.slotButtons[i].bounds.X + 128 + 36, menu.slotButtons[i].bounds.Y + 36, 999999, -1, 999999, getSlotAlpha());
		}

		protected virtual void drawSlotShadow(SpriteBatch b, int i)
		{
			Vector2 vector = portraitOffset();
			b.Draw(Game1.shadowTexture, new Vector2((float)menu.slotButtons[i].bounds.X + vector.X + 32f, menu.slotButtons[i].bounds.Y + 128 + 16), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, 0.8f);
		}

		protected virtual Vector2 portraitOffset()
		{
			return new Vector2(92f, 20f);
		}

		protected virtual void drawSlotFarmer(SpriteBatch b, int i)
		{
			Vector2 vector = portraitOffset();
			FarmerRenderer.isDrawingForUI = true;
			Farmer.FarmerRenderer.draw(b, new FarmerSprite.AnimationFrame(0, 0, secondaryArm: false, flip: false), 0, new Rectangle(0, 0, 16, 32), new Vector2((float)menu.slotButtons[i].bounds.X + vector.X, (float)menu.slotButtons[i].bounds.Y + vector.Y), Vector2.Zero, 0.8f, 2, Color.White, 0f, 1f, Farmer);
			FarmerRenderer.isDrawingForUI = false;
		}

		protected virtual void drawSlotDate(SpriteBatch b, int i)
		{
			string text = ((!Farmer.dayOfMonthForSaveGame.HasValue || !Farmer.seasonForSaveGame.HasValue || !Farmer.yearForSaveGame.HasValue) ? Farmer.dateStringForSaveGame : Utility.getDateStringFor(Farmer.dayOfMonthForSaveGame.Value, Farmer.seasonForSaveGame.Value, Farmer.yearForSaveGame.Value));
			Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2(menu.slotButtons[i].bounds.X + 128 + 32, menu.slotButtons[i].bounds.Y + 64 + 40), Game1.textColor * getSlotAlpha());
		}

		protected virtual string slotSubName()
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11019", Farmer.farmName);
		}

		protected virtual void drawSlotSubName(SpriteBatch b, int i)
		{
			string text = slotSubName();
			Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2((float)(menu.slotButtons[i].bounds.X + menu.width - 128) - Game1.dialogueFont.MeasureString(text).X, menu.slotButtons[i].bounds.Y + 44), Game1.textColor * getSlotAlpha());
		}

		protected virtual void drawSlotMoney(SpriteBatch b, int i)
		{
			string text = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", Utility.getNumberWithCommas(Farmer.Money));
			if (Farmer.Money == 1 && LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.pt)
			{
				text = text.Substring(0, text.Length - 1);
			}
			int num = (int)Game1.dialogueFont.MeasureString(text).X;
			Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(menu.slotButtons[i].bounds.X + menu.width - 192 - 100 - num, menu.slotButtons[i].bounds.Y + 64 + 44), new Rectangle(193, 373, 9, 9), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
			Vector2 position = new Vector2(menu.slotButtons[i].bounds.X + menu.width - 192 - 60 - num, menu.slotButtons[i].bounds.Y + 64 + 44);
			if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.en)
			{
				position.Y += 5f;
			}
			Utility.drawTextWithShadow(b, text, Game1.dialogueFont, position, Game1.textColor * getSlotAlpha());
		}

		protected virtual void drawSlotTimer(SpriteBatch b, int i)
		{
			Utility.drawWithShadow(position: new Vector2(menu.slotButtons[i].bounds.X + menu.width - 192 - 44, menu.slotButtons[i].bounds.Y + 64 + 36), b: b, texture: Game1.mouseCursors, sourceRect: new Rectangle(595, 1748, 9, 11), color: Color.White, rotation: 0f, origin: Vector2.Zero, scale: 4f, flipped: false, layerDepth: 1f);
			Vector2 position = new Vector2(menu.slotButtons[i].bounds.X + menu.width - 192 - 4, menu.slotButtons[i].bounds.Y + 64 + 44);
			if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.en)
			{
				position.Y += 5f;
			}
			Utility.drawTextWithShadow(b, Utility.getHoursMinutesStringFromMilliseconds(Farmer.millisecondsPlayed), Game1.dialogueFont, position, Game1.textColor * getSlotAlpha());
		}

		public virtual void drawVersionMismatchSlot(SpriteBatch b, int i)
		{
			SpriteText.drawString(b, slotName(), menu.slotButtons[i].bounds.X + 128, menu.slotButtons[i].bounds.Y + 36);
			string text = slotSubName();
			Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2((float)(menu.slotButtons[i].bounds.X + menu.width - 128) - Game1.dialogueFont.MeasureString(text).X, menu.slotButtons[i].bounds.Y + 44), Game1.textColor);
			string text2 = Farmer.gameVersion;
			if (text2 == "-1")
			{
				text2 = "<1.4";
			}
			string text3 = Game1.content.LoadString("Strings\\UI:VersionMismatch", text2);
			Color color = Game1.textColor;
			if (Game1.currentGameTime.TotalGameTime.TotalSeconds < redTimer && (int)((redTimer - Game1.currentGameTime.TotalGameTime.TotalSeconds) / 0.25) % 2 == 1)
			{
				color = Color.Red;
			}
			Utility.drawTextWithShadow(b, text3, Game1.dialogueFont, new Vector2(menu.slotButtons[i].bounds.X + 128, menu.slotButtons[i].bounds.Y + 64 + 40), color);
		}

		public override void Draw(SpriteBatch b, int i)
		{
			drawSlotSaveNumber(b, i);
			if (versionComparison < 0)
			{
				drawVersionMismatchSlot(b, i);
				return;
			}
			drawSlotName(b, i);
			drawSlotShadow(b, i);
			drawSlotFarmer(b, i);
			drawSlotDate(b, i);
			drawSlotSubName(b, i);
			drawSlotMoney(b, i);
			drawSlotTimer(b, i);
		}

		public new void Dispose()
		{
			Farmer.unload();
		}
	}

	protected const int CenterOffset = 0;

	public const int region_upArrow = 800;

	public const int region_downArrow = 801;

	public const int region_okDelete = 802;

	public const int region_cancelDelete = 803;

	public const int region_slots = 900;

	public const int region_deleteButtons = 901;

	public const int region_navigationButtons = 902;

	public const int region_deleteConfirmations = 903;

	public const int itemsPerPage = 4;

	public List<ClickableComponent> slotButtons = new List<ClickableComponent>();

	public List<ClickableTextureComponent> deleteButtons = new List<ClickableTextureComponent>();

	public int currentItemIndex;

	public int timerToLoad;

	public int selected = -1;

	public int selectedForDelete = -1;

	public ClickableTextureComponent upArrow;

	public ClickableTextureComponent downArrow;

	public ClickableTextureComponent scrollBar;

	public ClickableTextureComponent okDeleteButton;

	public ClickableTextureComponent cancelDeleteButton;

	public ClickableComponent backButton;

	public bool scrolling;

	public bool deleteConfirmationScreen;

	protected List<MenuSlot> menuSlots = new List<MenuSlot>();

	private Rectangle scrollBarRunner;

	protected string hoverText = "";

	public bool loading;

	public bool drawn;

	public bool deleting;

	private int _updatesSinceLastDeleteConfirmScreen;

	private Task<List<Farmer>> _initTask;

	private Task _deleteTask;

	private bool disposedValue;

	public virtual List<MenuSlot> MenuSlots
	{
		get
		{
			return menuSlots;
		}
		set
		{
			menuSlots = value;
		}
	}

	public bool IsDoingTask()
	{
		if (_initTask == null && _deleteTask == null && !loading)
		{
			return deleting;
		}
		return true;
	}

	public override bool readyToClose()
	{
		if (!IsDoingTask())
		{
			return _updatesSinceLastDeleteConfirmScreen > 1;
		}
		return false;
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="filter">A search filter to apply to the displayed list of saves, if any.</param>
	public LoadGameMenu(string filter = null)
		: base(Game1.uiViewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2, Game1.uiViewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 1100 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2)
	{
		backButton = new ClickableComponent(new Rectangle(Game1.uiViewport.Width + -66 * TitleMenu.pixelZoom - 8 * TitleMenu.pixelZoom * 2, Game1.uiViewport.Height - 27 * TitleMenu.pixelZoom - 8 * TitleMenu.pixelZoom, 66 * TitleMenu.pixelZoom, 27 * TitleMenu.pixelZoom), "")
		{
			myID = 81114,
			upNeighborID = -99998,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			downNeighborID = -99998
		};
		upArrow = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + 16, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f)
		{
			myID = 800,
			downNeighborID = 801,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			region = 902
		};
		downArrow = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f)
		{
			myID = 801,
			upNeighborID = 800,
			leftNeighborID = -99998,
			downNeighborID = -99998,
			rightNeighborID = -99998,
			region = 902
		};
		scrollBar = new ClickableTextureComponent(new Rectangle(upArrow.bounds.X + 12, upArrow.bounds.Y + upArrow.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
		scrollBarRunner = new Rectangle(scrollBar.bounds.X, upArrow.bounds.Y + upArrow.bounds.Height + 4, scrollBar.bounds.Width, height - 64 - upArrow.bounds.Height - 28);
		okDeleteButton = new ClickableTextureComponent(Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10992"), new Rectangle((int)Utility.getTopLeftPositionForCenteringOnScreen(64, 64).X - 64, (int)Utility.getTopLeftPositionForCenteringOnScreen(64, 64).Y + 128, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
		{
			myID = 802,
			rightNeighborID = 803,
			region = 903
		};
		cancelDeleteButton = new ClickableTextureComponent(Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10993"), new Rectangle((int)Utility.getTopLeftPositionForCenteringOnScreen(64, 64).X + 64, (int)Utility.getTopLeftPositionForCenteringOnScreen(64, 64).Y + 128, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47), 1f)
		{
			myID = 803,
			leftNeighborID = 802,
			region = 903
		};
		for (int i = 0; i < 4; i++)
		{
			slotButtons.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 16 + i * (height / 4), width - 32, height / 4 + 4), i.ToString() ?? "")
			{
				myID = i,
				region = 900,
				downNeighborID = ((i < 3) ? (-99998) : (-7777)),
				upNeighborID = ((i > 0) ? (-99998) : (-7777)),
				rightNeighborID = -99998,
				fullyImmutable = true
			});
			if (hasDeleteButtons())
			{
				deleteButtons.Add(new ClickableTextureComponent("", new Rectangle(xPositionOnScreen + width - 64 - 4, yPositionOnScreen + 32 + 4 + i * (height / 4), 48, 48), "", Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10994"), Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 3f)
				{
					myID = i + 100,
					region = 901,
					leftNeighborID = -99998,
					downNeighborImmutable = true,
					downNeighborID = -99998,
					upNeighborImmutable = true,
					upNeighborID = ((i > 0) ? (-99998) : (-1)),
					rightNeighborID = -99998
				});
			}
		}
		startListPopulation(filter);
		if (Game1.options.snappyMenus && Game1.options.gamepadControls)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
		UpdateButtons();
	}

	protected virtual bool hasDeleteButtons()
	{
		return true;
	}

	/// <summary>Asynchronously populate the list of saves.</summary>
	/// <param name="filter">A search filter to apply to the displayed list of saves, if any.</param>
	protected virtual void startListPopulation(string filter)
	{
		if (LocalMultiplayer.IsLocalMultiplayer())
		{
			addSaveFiles(FindSaveGames(filter));
			saveFileScanComplete();
			return;
		}
		_initTask = new Task<List<Farmer>>(delegate
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			return FindSaveGames(filter);
		});
		Game1.hooks.StartTask(_initTask, "Find Save Games");
	}

	public virtual void UpdateButtons()
	{
		for (int i = 0; i < slotButtons.Count; i++)
		{
			ClickableTextureComponent clickableTextureComponent = null;
			if (hasDeleteButtons() && i >= 0 && i < deleteButtons.Count)
			{
				clickableTextureComponent = deleteButtons[i];
			}
			if (currentItemIndex + i < MenuSlots.Count)
			{
				slotButtons[i].visible = true;
				if (clickableTextureComponent != null)
				{
					clickableTextureComponent.visible = true;
				}
			}
			else
			{
				slotButtons[i].visible = false;
				if (clickableTextureComponent != null)
				{
					clickableTextureComponent.visible = false;
				}
			}
		}
	}

	protected virtual void addSaveFiles(List<Farmer> files)
	{
		int num = MenuSlots.Count + 1;
		for (int i = 0; i < files.Count; i++)
		{
			Farmer farmer = files[i];
			if (farmer != null)
			{
				MenuSlots.Add(new SaveFileSlot(this, farmer, num + i));
			}
		}
		UpdateButtons();
	}

	/// <summary>Get the save games to show in the menu.</summary>
	/// <param name="filter">A search filter to apply to the displayed list of saves, if any.</param>
	/// <returns>Returns a list of farmers for which to add save slots. If a <paramref name="filter" /> is given, filtered-out slots will be null in the list.</returns>
	private static List<Farmer> FindSaveGames(string filter)
	{
		List<Farmer> list = new List<Farmer>();
		string savesFolder = Program.GetSavesFolder();
		if (Directory.Exists(savesFolder))
		{
			foreach (string item in Directory.EnumerateDirectories(savesFolder).ToList())
			{
				string text = item.Split(Path.DirectorySeparatorChar).Last();
				string pathToFile = Path.Combine(savesFolder, item, "SaveGameInfo");
				string pathToSave = Path.Combine(savesFolder, item, text);
				if (!File.Exists(pathToSave) && !File.Exists(pathToSave + "_old") && !File.Exists(pathToSave + "_STARDEWVALLEYSAVETMP"))
				{
					continue;
				}
				Farmer farmer = null;
				try
				{
					Exception loadError;
					object obj = TryReadSaveInfo(null, out loadError);
					if (obj == null)
					{
						obj = TryReadSaveInfo("_old", out var loadError2) ?? TryReadSaveInfo("_STARDEWVALLEYSAVETMP", out loadError2);
						if (obj == null)
						{
							obj = TryReadSaveData(null, out loadError2) ?? TryReadSaveData("_old", out loadError2) ?? TryReadSaveData("_STARDEWVALLEYSAVETMP", out loadError2);
						}
					}
					farmer = (Farmer)obj;
					if (farmer == null)
					{
						Game1.log.Error("Exception occurred trying to access file '" + pathToFile + "'", loadError);
						continue;
					}
					SaveGame.loadDataToFarmer(farmer);
					farmer.slotName = text;
					list.Add(farmer);
				}
				catch (Exception exception)
				{
					Game1.log.Error("Exception occurred trying to access file '" + pathToFile + "'", exception);
					farmer?.unload();
				}
				Farmer TryReadSaveData(string suffix, out Exception loadError3)
				{
					return TryReadFile<SaveGame>(pathToSave + suffix, out loadError3, SaveSerializer.Deserialize<SaveGame>)?.player;
				}
				Farmer TryReadSaveInfo(string suffix, out Exception loadError3)
				{
					return TryReadFile<Farmer>(pathToFile + suffix, out loadError3, SaveSerializer.Deserialize<Farmer>);
				}
			}
		}
		list.Sort();
		if (!string.IsNullOrWhiteSpace(filter))
		{
			for (int i = 0; i < list.Count; i++)
			{
				Farmer farmer2 = list[i];
				string name = farmer2.Name;
				if (name != null && name.IndexOfIgnoreCase(filter) == -1)
				{
					string value = farmer2.farmName.Value;
					if (value != null && value.IndexOfIgnoreCase(filter) == -1)
					{
						list[i] = null;
					}
				}
			}
		}
		return list;
		static TData TryReadFile<TData>(string path, out Exception reference, Func<Stream, TData> load)
		{
			try
			{
				using FileStream arg = File.OpenRead(path);
				reference = null;
				return load(arg);
			}
			catch (Exception ex)
			{
				reference = ex;
				return default(TData);
			}
		}
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		if (button == Buttons.B && deleteConfirmationScreen)
		{
			deleteConfirmationScreen = false;
			selectedForDelete = -1;
			Game1.playSound("smallSelect");
			if (Game1.options.snappyMenus && Game1.options.gamepadControls)
			{
				currentlySnappedComponent = getComponentWithID(0);
				snapCursorToCurrentSnappedComponent();
			}
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		if (deleteConfirmationScreen)
		{
			currentlySnappedComponent = getComponentWithID(803);
		}
		else
		{
			currentlySnappedComponent = getComponentWithID(0);
		}
		snapCursorToCurrentSnappedComponent();
	}

	protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
	{
		switch (direction)
		{
		case 2:
			if (currentItemIndex < Math.Max(0, MenuSlots.Count - 4))
			{
				downArrowPressed();
				currentlySnappedComponent = getComponentWithID(3);
				snapCursorToCurrentSnappedComponent();
			}
			break;
		case 0:
			if (currentItemIndex > 0)
			{
				upArrowPressed();
				currentlySnappedComponent = getComponentWithID(0);
				snapCursorToCurrentSnappedComponent();
			}
			break;
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		xPositionOnScreen = (newBounds.Width - width) / 2;
		yPositionOnScreen = (newBounds.Height - (height + 32)) / 2;
		backButton.bounds.X = Game1.uiViewport.Width + -66 * TitleMenu.pixelZoom - 8 * TitleMenu.pixelZoom * 2;
		backButton.bounds.Y = Game1.uiViewport.Height - 27 * TitleMenu.pixelZoom - 8 * TitleMenu.pixelZoom;
		upArrow.bounds.X = xPositionOnScreen + width + 16;
		upArrow.bounds.Y = yPositionOnScreen + 16;
		downArrow.bounds.X = xPositionOnScreen + width + 16;
		downArrow.bounds.Y = yPositionOnScreen + height - 64;
		scrollBar = new ClickableTextureComponent(new Rectangle(upArrow.bounds.X + 12, upArrow.bounds.Y + upArrow.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
		scrollBarRunner = new Rectangle(scrollBar.bounds.X, upArrow.bounds.Y + upArrow.bounds.Height + 4, scrollBar.bounds.Width, height - 64 - upArrow.bounds.Height - 28);
		okDeleteButton.bounds.X = (int)Utility.getTopLeftPositionForCenteringOnScreen(64, 64).X - 64;
		okDeleteButton.bounds.Y = (int)Utility.getTopLeftPositionForCenteringOnScreen(64, 64).Y + 128;
		cancelDeleteButton.bounds.X = (int)Utility.getTopLeftPositionForCenteringOnScreen(64, 64).X + 64;
		cancelDeleteButton.bounds.Y = (int)Utility.getTopLeftPositionForCenteringOnScreen(64, 64).Y + 128;
		for (int i = 0; i < slotButtons.Count; i++)
		{
			slotButtons[i].bounds.X = xPositionOnScreen + 16;
			slotButtons[i].bounds.Y = yPositionOnScreen + 16 + i * (height / 4);
		}
		for (int j = 0; j < deleteButtons.Count; j++)
		{
			deleteButtons[j].bounds.X = xPositionOnScreen + width - 64 - 4;
			deleteButtons[j].bounds.Y = yPositionOnScreen + 32 + 4 + j * (height / 4);
		}
		if (Game1.options.snappyMenus && Game1.options.gamepadControls)
		{
			int id = ((currentlySnappedComponent != null) ? currentlySnappedComponent.myID : 81114);
			populateClickableComponentList();
			currentlySnappedComponent = getComponentWithID(id);
			snapCursorToCurrentSnappedComponent();
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoverText = "";
		base.performHoverAction(x, y);
		if (deleteConfirmationScreen)
		{
			okDeleteButton.tryHover(x, y);
			cancelDeleteButton.tryHover(x, y);
			if (okDeleteButton.containsPoint(x, y))
			{
				hoverText = "";
			}
			else if (cancelDeleteButton.containsPoint(x, y))
			{
				hoverText = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10993");
			}
			return;
		}
		upArrow.tryHover(x, y);
		downArrow.tryHover(x, y);
		scrollBar.tryHover(x, y);
		foreach (ClickableTextureComponent deleteButton in deleteButtons)
		{
			deleteButton.tryHover(x, y, 0.2f);
			if (deleteButton.containsPoint(x, y))
			{
				hoverText = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10994");
				return;
			}
		}
		if (scrolling)
		{
			return;
		}
		for (int i = 0; i < slotButtons.Count; i++)
		{
			if (currentItemIndex + i < MenuSlots.Count && slotButtons[i].containsPoint(x, y))
			{
				if (slotButtons[i].scale == 1f)
				{
					Game1.playSound("Cowboy_gunshot");
				}
				slotButtons[i].scale = Math.Min(slotButtons[i].scale + 0.03f, 1.1f);
			}
			else
			{
				slotButtons[i].scale = Math.Max(1f, slotButtons[i].scale - 0.03f);
			}
		}
	}

	/// <inheritdoc />
	public override void leftClickHeld(int x, int y)
	{
		base.leftClickHeld(x, y);
		if (scrolling)
		{
			int y2 = scrollBar.bounds.Y;
			scrollBar.bounds.Y = Math.Min(yPositionOnScreen + height - 64 - 12 - scrollBar.bounds.Height, Math.Max(y, yPositionOnScreen + upArrow.bounds.Height + 20));
			float num = (float)(y - scrollBarRunner.Y) / (float)scrollBarRunner.Height;
			currentItemIndex = Math.Min(MenuSlots.Count - 4, Math.Max(0, (int)((float)MenuSlots.Count * num)));
			setScrollBarToCurrentIndex();
			if (y2 != scrollBar.bounds.Y)
			{
				Game1.playSound("shiny4");
			}
		}
	}

	/// <inheritdoc />
	public override void releaseLeftClick(int x, int y)
	{
		base.releaseLeftClick(x, y);
		scrolling = false;
	}

	protected void setScrollBarToCurrentIndex()
	{
		if (MenuSlots.Count > 0)
		{
			scrollBar.bounds.Y = scrollBarRunner.Height / Math.Max(1, MenuSlots.Count - 4 + 1) * currentItemIndex + upArrow.bounds.Bottom + 4;
			if (currentItemIndex == MenuSlots.Count - 4)
			{
				scrollBar.bounds.Y = downArrow.bounds.Y - scrollBar.bounds.Height - 4;
			}
		}
		UpdateButtons();
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		base.receiveScrollWheelAction(direction);
		if (direction > 0 && currentItemIndex > 0)
		{
			upArrowPressed();
		}
		else if (direction < 0 && currentItemIndex < Math.Max(0, MenuSlots.Count - 4))
		{
			downArrowPressed();
		}
	}

	private void downArrowPressed()
	{
		downArrow.scale = downArrow.baseScale;
		currentItemIndex++;
		Game1.playSound("shwip");
		setScrollBarToCurrentIndex();
	}

	private void upArrowPressed()
	{
		upArrow.scale = upArrow.baseScale;
		currentItemIndex--;
		Game1.playSound("shwip");
		setScrollBarToCurrentIndex();
	}

	private void deleteFile(int which)
	{
		if (!(MenuSlots[which] is SaveFileSlot saveFileSlot))
		{
			return;
		}
		string slotName = saveFileSlot.Farmer.slotName;
		string path = Path.Combine(Program.GetSavesFolder(), slotName);
		if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
		}
		for (int i = 0; i < 50; i++)
		{
			if (!Directory.Exists(path))
			{
				break;
			}
			Thread.Sleep(100);
		}
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (timerToLoad > 0 || loading || deleting)
		{
			return;
		}
		if (deleteConfirmationScreen)
		{
			if (cancelDeleteButton.containsPoint(x, y))
			{
				deleteConfirmationScreen = false;
				selectedForDelete = -1;
				Game1.playSound("smallSelect");
				if (Game1.options.snappyMenus && Game1.options.gamepadControls)
				{
					currentlySnappedComponent = getComponentWithID(0);
					snapCursorToCurrentSnappedComponent();
				}
			}
			else
			{
				if (!okDeleteButton.containsPoint(x, y))
				{
					return;
				}
				deleting = true;
				if (LocalMultiplayer.IsLocalMultiplayer())
				{
					deleteFile(selectedForDelete);
					deleting = false;
				}
				else
				{
					_deleteTask = new Task(delegate
					{
						Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
						deleteFile(selectedForDelete);
					});
					Game1.hooks.StartTask(_deleteTask, "Farm_Delete");
				}
				deleteConfirmationScreen = false;
				if (Game1.options.snappyMenus && Game1.options.gamepadControls)
				{
					currentlySnappedComponent = getComponentWithID(0);
					snapCursorToCurrentSnappedComponent();
				}
				Game1.playSound("trashcan");
			}
			return;
		}
		base.receiveLeftClick(x, y, playSound);
		if (downArrow.containsPoint(x, y) && currentItemIndex < Math.Max(0, MenuSlots.Count - 4))
		{
			downArrowPressed();
		}
		else if (upArrow.containsPoint(x, y) && currentItemIndex > 0)
		{
			upArrowPressed();
		}
		else if (scrollBar.containsPoint(x, y))
		{
			scrolling = true;
		}
		else if (!downArrow.containsPoint(x, y) && x > xPositionOnScreen + width && x < xPositionOnScreen + width + 128 && y > yPositionOnScreen && y < yPositionOnScreen + height)
		{
			scrolling = true;
			leftClickHeld(x, y);
			releaseLeftClick(x, y);
		}
		if (selected == -1)
		{
			for (int num = 0; num < deleteButtons.Count; num++)
			{
				if (deleteButtons[num].containsPoint(x, y) && num < MenuSlots.Count && !deleteConfirmationScreen)
				{
					deleteConfirmationScreen = true;
					Game1.playSound("drumkit6");
					selectedForDelete = currentItemIndex + num;
					if (Game1.options.snappyMenus && Game1.options.gamepadControls)
					{
						currentlySnappedComponent = getComponentWithID(803);
						snapCursorToCurrentSnappedComponent();
					}
					return;
				}
			}
		}
		if (!deleteConfirmationScreen)
		{
			for (int num2 = 0; num2 < slotButtons.Count; num2++)
			{
				if (!slotButtons[num2].containsPoint(x, y) || num2 >= MenuSlots.Count)
				{
					continue;
				}
				if (MenuSlots[currentItemIndex + num2] is SaveFileSlot { versionComparison: <0 } saveFileSlot)
				{
					saveFileSlot.redTimer = Game1.currentGameTime.TotalGameTime.TotalSeconds + 1.0;
					Game1.playSound("cancel");
					continue;
				}
				Game1.playSound("select");
				timerToLoad = MenuSlots[currentItemIndex + num2].ActivateDelay;
				if (timerToLoad > 0)
				{
					loading = true;
					selected = currentItemIndex + num2;
				}
				else
				{
					MenuSlots[currentItemIndex + num2].Activate();
				}
				return;
			}
		}
		currentItemIndex = Math.Max(0, Math.Min(MenuSlots.Count - 4, currentItemIndex));
	}

	protected virtual void saveFileScanComplete()
	{
		Game1.game1.ResetGameStateOnTitleScreen();
	}

	protected virtual bool checkListPopulation()
	{
		if (!deleteConfirmationScreen)
		{
			_updatesSinceLastDeleteConfirmScreen++;
		}
		else
		{
			_updatesSinceLastDeleteConfirmScreen = 0;
		}
		if (_initTask != null)
		{
			if (_initTask.IsCanceled || _initTask.IsCompleted || _initTask.IsFaulted)
			{
				if (_initTask.IsCompleted)
				{
					addSaveFiles(_initTask.Result);
					saveFileScanComplete();
				}
				_initTask = null;
			}
			return true;
		}
		return false;
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		if (checkListPopulation())
		{
			return;
		}
		if (_deleteTask != null)
		{
			if (_deleteTask.IsCanceled || _deleteTask.IsCompleted || _deleteTask.IsFaulted)
			{
				if (!_deleteTask.IsCompleted)
				{
					selectedForDelete = -1;
				}
				_deleteTask = null;
				deleting = false;
			}
			return;
		}
		if (selectedForDelete != -1 && !deleteConfirmationScreen && !deleting && MenuSlots[selectedForDelete] is SaveFileSlot saveFileSlot)
		{
			saveFileSlot.Farmer.unload();
			MenuSlots.RemoveAt(selectedForDelete);
			selectedForDelete = -1;
			slotButtons.Clear();
			deleteButtons.Clear();
			for (int i = 0; i < 4; i++)
			{
				slotButtons.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 16 + i * (height / 4), width - 32, height / 4 + 4), i.ToString() ?? ""));
				if (hasDeleteButtons())
				{
					deleteButtons.Add(new ClickableTextureComponent("", new Rectangle(xPositionOnScreen + width - 64 - 4, yPositionOnScreen + 32 + 4 + i * (height / 4), 48, 48), "", "Delete File", Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 3f));
				}
			}
			if (MenuSlots.Count <= 4)
			{
				currentItemIndex = 0;
				setScrollBarToCurrentIndex();
			}
		}
		if (timerToLoad <= 0)
		{
			return;
		}
		timerToLoad -= time.ElapsedGameTime.Milliseconds;
		if (timerToLoad <= 0)
		{
			if (MenuSlots.Count > selected)
			{
				MenuSlots[selected].Activate();
			}
			else
			{
				Game1.ExitToTitle();
			}
		}
	}

	protected virtual string getStatusText()
	{
		if (_initTask != null)
		{
			return Game1.content.LoadString("Strings\\UI:LoadGameMenu_LookingForSavedGames");
		}
		if (deleting)
		{
			return Game1.content.LoadString("Strings\\UI:LoadGameMenu_Deleting");
		}
		if (MenuSlots.Count == 0)
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11022");
		}
		return null;
	}

	protected virtual void drawExtra(SpriteBatch b)
	{
	}

	protected virtual void drawSlotBackground(SpriteBatch b, int i, MenuSlot slot)
	{
		IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), slotButtons[i].bounds.X, slotButtons[i].bounds.Y, slotButtons[i].bounds.Width, slotButtons[i].bounds.Height, ((currentItemIndex + i != selected || timerToLoad % 150 <= 75 || timerToLoad <= 1000) && (selected != -1 || !(slotButtons[i].scale > 1f) || scrolling || deleteConfirmationScreen)) ? Color.White : ((deleteButtons.Count > i && deleteButtons[i].containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY())) ? Color.White : Color.Wheat), 4f, drawShadow: false);
	}

	protected virtual void drawBefore(SpriteBatch b)
	{
	}

	protected virtual void drawStatusText(SpriteBatch b)
	{
		string statusText = getStatusText();
		if (statusText != null)
		{
			SpriteText.drawStringHorizontallyCenteredAt(b, statusText, Game1.graphics.GraphicsDevice.Viewport.Bounds.Center.X, Game1.graphics.GraphicsDevice.Viewport.Bounds.Center.Y);
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		drawBefore(b);
		IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), xPositionOnScreen, yPositionOnScreen, width, height + 32, Color.White, 4f);
		if (selectedForDelete == -1 || !deleting || deleteConfirmationScreen)
		{
			for (int i = 0; i < slotButtons.Count; i++)
			{
				if (currentItemIndex + i < MenuSlots.Count)
				{
					drawSlotBackground(b, i, MenuSlots[currentItemIndex + i]);
					MenuSlots[currentItemIndex + i].Draw(b, i);
					if (deleteButtons.Count > i)
					{
						deleteButtons[i].draw(b, Color.White * 0.75f, 1f);
					}
				}
			}
		}
		drawStatusText(b);
		upArrow.draw(b);
		downArrow.draw(b);
		if (MenuSlots.Count > 4)
		{
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), scrollBarRunner.X, scrollBarRunner.Y, scrollBarRunner.Width, scrollBarRunner.Height, Color.White, 4f, drawShadow: false);
			scrollBar.draw(b);
		}
		if (deleteConfirmationScreen && MenuSlots[selectedForDelete] is SaveFileSlot saveFileSlot)
		{
			b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.75f);
			string s = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11023", saveFileSlot.Farmer.Name);
			int num = okDeleteButton.bounds.X + (cancelDeleteButton.bounds.X - okDeleteButton.bounds.X) / 2 + okDeleteButton.bounds.Width / 2;
			SpriteText.drawString(b, s, num - SpriteText.getWidthOfString(s) / 2, (int)Utility.getTopLeftPositionForCenteringOnScreen(192, 64).Y, 9999, -1, 9999, 1f, 1f, junimoText: false, -1, "", SpriteText.color_White);
			okDeleteButton.draw(b);
			cancelDeleteButton.draw(b);
		}
		base.draw(b);
		if (hoverText.Length > 0)
		{
			IClickableMenu.drawHoverText(b, hoverText, Game1.dialogueFont);
		}
		drawExtra(b);
		if (selected != -1 && timerToLoad < 1000)
		{
			b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * (1f - (float)timerToLoad / 1000f));
		}
		if (Game1.activeClickableMenu == this && (!Game1.options.SnappyMenus || currentlySnappedComponent != null) && !IsDoingTask())
		{
			drawMouse(b, ignore_transparency: false, loading ? 1 : (-1));
		}
		drawn = true;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposedValue)
		{
			return;
		}
		if (disposing)
		{
			if (MenuSlots != null)
			{
				foreach (MenuSlot menuSlot in MenuSlots)
				{
					menuSlot.Dispose();
				}
				MenuSlots.Clear();
				MenuSlots = null;
			}
			_initTask = null;
			_deleteTask = null;
		}
		disposedValue = true;
	}

	~LoadGameMenu()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public override bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
	{
		if (a.region == 901 && b.region != 901 && direction == 2 && b.myID != 81114)
		{
			return true;
		}
		if (a.region == 901 && direction == 3 && b.region != 900)
		{
			return false;
		}
		if (direction == 1 && a.region == 900 && hasDeleteButtons() && b.region != 901)
		{
			return false;
		}
		if (a.region != 903 && b.region == 903)
		{
			return false;
		}
		if ((direction == 0 || direction == 2) && a.myID == 81114 && b.region == 902)
		{
			return false;
		}
		return base.IsAutomaticSnapValid(direction, a, b);
	}

	protected override bool _ShouldAutoSnapPrioritizeAlignedElements()
	{
		return false;
	}

	[Conditional("LOG_FS_IO")]
	private static void LogFsio(string format, params object[] args)
	{
		Game1.log.Verbose(string.Format(format, args));
	}
}
