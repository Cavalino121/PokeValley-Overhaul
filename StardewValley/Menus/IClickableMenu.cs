using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Enchantments;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StardewValley.Menus;

[InstanceStatics]
public abstract class IClickableMenu
{
	public delegate void onExit();

	protected IClickableMenu _childMenu;

	protected IClickableMenu _parentMenu;

	public const int upperRightCloseButton_ID = 9175502;

	public const int currency_g = 0;

	public const int currency_starTokens = 1;

	public const int currency_qiCoins = 2;

	public const int currency_qiGems = 4;

	public const int greyedOutSpotIndex = 57;

	public const int presentIconIndex = 58;

	public const int itemSpotIndex = 10;

	protected string closeSound = "bigDeSelect";

	public static int borderWidth = 40;

	public static int tabYPositionRelativeToMenuY = -48;

	public static int spaceToClearTopBorder = 96;

	public static int spaceToClearSideBorder = 16;

	public const int spaceBetweenTabs = 4;

	/// <summary>The top-left X pixel position at which the menu is drawn.</summary>
	public int xPositionOnScreen;

	/// <summary>The top-left Y pixel position at which the menu is drawn.</summary>
	public int yPositionOnScreen;

	/// <summary>The pixel width of the menu.</summary>
	public int width;

	/// <summary>The pixel height of the menu.</summary>
	public int height;

	/// <summary>A callback to invoke before the menu exits.</summary>
	public Action<IClickableMenu> behaviorBeforeCleanup;

	/// <summary>A callback to invoke after the menu exits.</summary>
	public onExit exitFunction;

	/// <summary>The 'X' button to close the menu.</summary>
	public ClickableTextureComponent upperRightCloseButton;

	public bool destroy;

	protected int _dependencies;

	public List<ClickableComponent> allClickableComponents;

	public ClickableComponent currentlySnappedComponent;

	public static StringBuilder HoverTextStringBuilder = new StringBuilder();

	public Vector2 Position => new Vector2(xPositionOnScreen, yPositionOnScreen);

	/// <summary>Construct an instance.</summary>
	public IClickableMenu()
	{
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="x">The top-left X pixel position at which to position the menu.</param>
	/// <param name="y">The top-left Y pixel position at which to position the menu.</param>
	/// <param name="width">The pixel width of the menu.</param>
	/// <param name="height">The pixel height of the menu.</param>
	/// <param name="showUpperRightCloseButton">Whether the 'X' button to close the menu should be shown.</param>
	public IClickableMenu(int x, int y, int width, int height, bool showUpperRightCloseButton = false)
	{
		Game1.mouseCursorTransparency = 1f;
		initialize(x, y, width, height, showUpperRightCloseButton);
		if (Game1.gameMode == 3 && Game1.player != null && !Game1.eventUp)
		{
			Game1.player.Halt();
		}
	}

	/// <summary>Initialize the menu.</summary>
	/// <param name="x">The top-left X pixel position at which to position the menu.</param>
	/// <param name="y">The top-left Y pixel position at which to position the menu.</param>
	/// <param name="width">The pixel width of the menu.</param>
	/// <param name="height">The pixel height of the menu.</param>
	/// <param name="showUpperRightCloseButton">Whether the 'X' button to close the menu should be shown.</param>
	public void initialize(int x, int y, int width, int height, bool showUpperRightCloseButton = false)
	{
		if (Game1.player != null && !Game1.player.UsingTool && !Game1.eventUp)
		{
			Game1.player.forceCanMove();
		}
		xPositionOnScreen = x;
		yPositionOnScreen = y;
		this.width = width;
		this.height = height;
		if (showUpperRightCloseButton)
		{
			upperRightCloseButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 36, yPositionOnScreen - 8, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f)
			{
				myID = 9175502
			};
		}
		for (int i = 0; i < 4; i++)
		{
			Game1.directionKeyPolling[i] = 250;
		}
	}

	public IClickableMenu GetChildMenu()
	{
		return _childMenu;
	}

	public IClickableMenu GetParentMenu()
	{
		return _parentMenu;
	}

	public void SetChildMenu(IClickableMenu menu)
	{
		_childMenu = menu;
		if (_childMenu != null)
		{
			_childMenu._parentMenu = this;
		}
	}

	public void AddDependency()
	{
		_dependencies++;
	}

	public void RemoveDependency()
	{
		_dependencies--;
		if (_dependencies <= 0 && Game1.activeClickableMenu != this && TitleMenu.subMenu != this)
		{
			(this as IDisposable)?.Dispose();
		}
	}

	public bool HasDependencies()
	{
		return _dependencies > 0;
	}

	public virtual bool areGamePadControlsImplemented()
	{
		return false;
	}

	/// <summary>Handle the player pressing a game pad button.</summary>
	/// <param name="button">The game pad button that was pressed.</param>
	public virtual void receiveGamePadButton(Buttons button)
	{
	}

	public void drawMouse(SpriteBatch b, bool ignore_transparency = false, int cursor = -1)
	{
		if (!Game1.options.hardwareCursor)
		{
			float num = Game1.mouseCursorTransparency;
			if (ignore_transparency)
			{
				num = 1f;
			}
			if (cursor < 0)
			{
				cursor = ((Game1.options.snappyMenus && Game1.options.gamepadControls) ? 44 : 0);
			}
			b.Draw(Game1.mouseCursors, new Vector2(Game1.getMouseX(), Game1.getMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, cursor, 16, 16), Color.White * num, 0f, Vector2.Zero, 4f + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
		}
	}

	public virtual void populateClickableComponentList()
	{
		allClickableComponents = new List<ClickableComponent>();
		FieldInfo[] fields = GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			Type fieldType = fieldInfo.FieldType;
			if (fieldType.IsPrimitive || fieldType == typeof(string) || fieldInfo.GetCustomAttribute<SkipForClickableAggregation>() != null || fieldInfo.DeclaringType == typeof(IClickableMenu))
			{
				continue;
			}
			object value = fieldInfo.GetValue(this);
			if (!(value is ClickableComponent item))
			{
				if (!(value is List<List<ClickableTextureComponent>> list))
				{
					if (!(value is InventoryMenu inventoryMenu))
					{
						if (!(value is List<Dictionary<ClickableTextureComponent, CraftingRecipe>> list2))
						{
							if (!(value is Dictionary<int, List<List<ClickableTextureComponent>>> dictionary))
							{
								if (!(value is IDictionary dictionary2))
								{
									if (!(value is IEnumerable enumerable) || !fieldType.IsGenericType || !(fieldType.GetGenericTypeDefinition() == typeof(List<>)) || !typeof(ClickableComponent).IsAssignableFrom(fieldType.GetGenericArguments()[0]))
									{
										continue;
									}
									foreach (object item5 in enumerable)
									{
										if (item5 is ClickableComponent item2)
										{
											allClickableComponents.Add(item2);
										}
									}
								}
								else
								{
									if (!fieldType.IsGenericType || !(fieldType.GetGenericTypeDefinition() == typeof(Dictionary<, >)))
									{
										continue;
									}
									Type typeFromHandle = typeof(ClickableComponent);
									Type[] genericArguments = fieldType.GetGenericArguments();
									Type c = genericArguments[0];
									Type c2 = genericArguments[1];
									if (!typeFromHandle.IsAssignableFrom(c) && !typeFromHandle.IsAssignableFrom(c2))
									{
										continue;
									}
									foreach (DictionaryEntry item6 in dictionary2)
									{
										if (item6.Key is ClickableComponent item3)
										{
											allClickableComponents.Add(item3);
										}
										if (item6.Value is ClickableComponent item4)
										{
											allClickableComponents.Add(item4);
										}
									}
								}
								continue;
							}
							foreach (List<List<ClickableTextureComponent>> value2 in dictionary.Values)
							{
								foreach (List<ClickableTextureComponent> item7 in value2)
								{
									allClickableComponents.AddRange(item7);
								}
							}
							continue;
						}
						foreach (Dictionary<ClickableTextureComponent, CraftingRecipe> item8 in list2)
						{
							allClickableComponents.AddRange(item8.Keys);
						}
					}
					else
					{
						allClickableComponents.AddRange(inventoryMenu.inventory);
						allClickableComponents.Add(inventoryMenu.dropItemInvisibleButton);
					}
					continue;
				}
				foreach (List<ClickableTextureComponent> item9 in list)
				{
					foreach (ClickableTextureComponent item10 in item9)
					{
						if (item10 != null)
						{
							allClickableComponents.Add(item10);
						}
					}
				}
			}
			else
			{
				allClickableComponents.Add(item);
			}
		}
		if (Game1.activeClickableMenu is GameMenu gameMenu && this == gameMenu.GetCurrentPage())
		{
			gameMenu.AddTabsToClickableComponents(this);
		}
		if (upperRightCloseButton != null)
		{
			allClickableComponents.Add(upperRightCloseButton);
		}
	}

	public virtual void applyMovementKey(int direction)
	{
		if (allClickableComponents == null)
		{
			populateClickableComponentList();
		}
		moveCursorInDirection(direction);
	}

	/// <summary>
	/// return true if this method is overriden and a default clickablecomponent is snapped to.
	/// </summary>
	public virtual void snapToDefaultClickableComponent()
	{
	}

	public void applyMovementKey(Keys key)
	{
		if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
		{
			applyMovementKey(0);
		}
		else if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
		{
			applyMovementKey(1);
		}
		else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
		{
			applyMovementKey(2);
		}
		else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
		{
			applyMovementKey(3);
		}
	}

	/// <summary>
	/// Only use this if the child class overrides
	/// </summary>
	/// <param name="id"></param>
	public virtual void setCurrentlySnappedComponentTo(int id)
	{
		currentlySnappedComponent = getComponentWithID(id);
	}

	public void moveCursorInDirection(int direction)
	{
		if (currentlySnappedComponent == null)
		{
			List<ClickableComponent> list = allClickableComponents;
			if (list != null && list.Count > 0)
			{
				snapToDefaultClickableComponent();
				if (currentlySnappedComponent == null)
				{
					currentlySnappedComponent = allClickableComponents[0];
				}
			}
		}
		if (currentlySnappedComponent == null)
		{
			return;
		}
		ClickableComponent clickableComponent = currentlySnappedComponent;
		switch (direction)
		{
		case 0:
			switch (currentlySnappedComponent.upNeighborID)
			{
			case -99999:
				snapToDefaultClickableComponent();
				break;
			case -99998:
				automaticSnapBehavior(0, currentlySnappedComponent.region, currentlySnappedComponent.myID);
				break;
			case -7777:
				customSnapBehavior(0, currentlySnappedComponent.region, currentlySnappedComponent.myID);
				break;
			default:
				currentlySnappedComponent = getComponentWithID(currentlySnappedComponent.upNeighborID);
				break;
			}
			if (currentlySnappedComponent != null && (clickableComponent == null || (clickableComponent.upNeighborID != -7777 && clickableComponent.upNeighborID != -99998)) && !currentlySnappedComponent.downNeighborImmutable && !currentlySnappedComponent.fullyImmutable)
			{
				currentlySnappedComponent.downNeighborID = clickableComponent.myID;
			}
			if (currentlySnappedComponent == null)
			{
				noSnappedComponentFound(0, clickableComponent.region, clickableComponent.myID);
			}
			break;
		case 1:
			switch (currentlySnappedComponent.rightNeighborID)
			{
			case -99999:
				snapToDefaultClickableComponent();
				break;
			case -99998:
				automaticSnapBehavior(1, currentlySnappedComponent.region, currentlySnappedComponent.myID);
				break;
			case -7777:
				customSnapBehavior(1, currentlySnappedComponent.region, currentlySnappedComponent.myID);
				break;
			default:
				currentlySnappedComponent = getComponentWithID(currentlySnappedComponent.rightNeighborID);
				break;
			}
			if (currentlySnappedComponent != null && (clickableComponent == null || (clickableComponent.rightNeighborID != -7777 && clickableComponent.rightNeighborID != -99998)) && !currentlySnappedComponent.leftNeighborImmutable && !currentlySnappedComponent.fullyImmutable)
			{
				currentlySnappedComponent.leftNeighborID = clickableComponent.myID;
			}
			if (currentlySnappedComponent == null && clickableComponent.tryDefaultIfNoRightNeighborExists)
			{
				snapToDefaultClickableComponent();
			}
			else if (currentlySnappedComponent == null)
			{
				noSnappedComponentFound(1, clickableComponent.region, clickableComponent.myID);
			}
			break;
		case 2:
			switch (currentlySnappedComponent.downNeighborID)
			{
			case -99999:
				snapToDefaultClickableComponent();
				break;
			case -99998:
				automaticSnapBehavior(2, currentlySnappedComponent.region, currentlySnappedComponent.myID);
				break;
			case -7777:
				customSnapBehavior(2, currentlySnappedComponent.region, currentlySnappedComponent.myID);
				break;
			default:
				currentlySnappedComponent = getComponentWithID(currentlySnappedComponent.downNeighborID);
				break;
			}
			if (currentlySnappedComponent != null && (clickableComponent == null || (clickableComponent.downNeighborID != -7777 && clickableComponent.downNeighborID != -99998)) && !currentlySnappedComponent.upNeighborImmutable && !currentlySnappedComponent.fullyImmutable)
			{
				currentlySnappedComponent.upNeighborID = clickableComponent.myID;
			}
			if (currentlySnappedComponent == null && clickableComponent.tryDefaultIfNoDownNeighborExists)
			{
				snapToDefaultClickableComponent();
			}
			else if (currentlySnappedComponent == null)
			{
				noSnappedComponentFound(2, clickableComponent.region, clickableComponent.myID);
			}
			break;
		case 3:
			switch (currentlySnappedComponent.leftNeighborID)
			{
			case -99999:
				snapToDefaultClickableComponent();
				break;
			case -99998:
				automaticSnapBehavior(3, currentlySnappedComponent.region, currentlySnappedComponent.myID);
				break;
			case -7777:
				customSnapBehavior(3, currentlySnappedComponent.region, currentlySnappedComponent.myID);
				break;
			default:
				currentlySnappedComponent = getComponentWithID(currentlySnappedComponent.leftNeighborID);
				break;
			}
			if (currentlySnappedComponent != null && (clickableComponent == null || (clickableComponent.leftNeighborID != -7777 && clickableComponent.leftNeighborID != -99998)) && !currentlySnappedComponent.rightNeighborImmutable && !currentlySnappedComponent.fullyImmutable)
			{
				currentlySnappedComponent.rightNeighborID = clickableComponent.myID;
			}
			if (currentlySnappedComponent == null)
			{
				noSnappedComponentFound(3, clickableComponent.region, clickableComponent.myID);
			}
			break;
		}
		if (currentlySnappedComponent != null && clickableComponent != null && currentlySnappedComponent.region != clickableComponent.region)
		{
			actionOnRegionChange(clickableComponent.region, currentlySnappedComponent.region);
		}
		if (currentlySnappedComponent == null)
		{
			currentlySnappedComponent = clickableComponent;
		}
		snapCursorToCurrentSnappedComponent();
		if (currentlySnappedComponent != clickableComponent)
		{
			Game1.playSound("shiny4");
		}
	}

	public virtual void snapCursorToCurrentSnappedComponent()
	{
		if (currentlySnappedComponent != null)
		{
			Game1.setMousePosition(currentlySnappedComponent.bounds.Right - currentlySnappedComponent.bounds.Width / 4, currentlySnappedComponent.bounds.Bottom - currentlySnappedComponent.bounds.Height / 4, ui_scale: true);
		}
	}

	protected virtual void noSnappedComponentFound(int direction, int oldRegion, int oldID)
	{
	}

	protected virtual void customSnapBehavior(int direction, int oldRegion, int oldID)
	{
	}

	public virtual bool IsActive()
	{
		if (_parentMenu == null)
		{
			return this == Game1.activeClickableMenu;
		}
		IClickableMenu parentMenu = _parentMenu;
		while (parentMenu?._parentMenu != null)
		{
			parentMenu = parentMenu._parentMenu;
		}
		return parentMenu == Game1.activeClickableMenu;
	}

	public virtual void automaticSnapBehavior(int direction, int oldRegion, int oldID)
	{
		if (currentlySnappedComponent == null)
		{
			snapToDefaultClickableComponent();
			return;
		}
		Vector2 zero = Vector2.Zero;
		switch (direction)
		{
		case 3:
			zero.X = -1f;
			zero.Y = 0f;
			break;
		case 1:
			zero.X = 1f;
			zero.Y = 0f;
			break;
		case 0:
			zero.X = 0f;
			zero.Y = -1f;
			break;
		case 2:
			zero.X = 0f;
			zero.Y = 1f;
			break;
		}
		float num = -1f;
		ClickableComponent clickableComponent = null;
		for (int i = 0; i < allClickableComponents.Count; i++)
		{
			ClickableComponent clickableComponent2 = allClickableComponents[i];
			if ((clickableComponent2.leftNeighborID == -1 && clickableComponent2.rightNeighborID == -1 && clickableComponent2.upNeighborID == -1 && clickableComponent2.downNeighborID == -1) || clickableComponent2.myID == -500 || !IsAutomaticSnapValid(direction, currentlySnappedComponent, clickableComponent2) || !clickableComponent2.visible || clickableComponent2 == upperRightCloseButton || clickableComponent2 == currentlySnappedComponent)
			{
				continue;
			}
			Vector2 value = new Vector2(clickableComponent2.bounds.Center.X - currentlySnappedComponent.bounds.Center.X, clickableComponent2.bounds.Center.Y - currentlySnappedComponent.bounds.Center.Y);
			Vector2 value2 = new Vector2(value.X, value.Y);
			value2.Normalize();
			float num2 = Vector2.Dot(zero, value2);
			if (!(num2 > 0.01f))
			{
				continue;
			}
			float num3 = Vector2.DistanceSquared(Vector2.Zero, value);
			bool flag = false;
			switch (direction)
			{
			case 0:
			case 2:
				if (Math.Abs(value.X) < 32f)
				{
					flag = true;
				}
				break;
			case 1:
			case 3:
				if (Math.Abs(value.Y) < 32f)
				{
					flag = true;
				}
				break;
			}
			if (_ShouldAutoSnapPrioritizeAlignedElements() && (num2 > 0.99999f || flag))
			{
				num3 *= 0.01f;
			}
			if (num == -1f || num3 < num)
			{
				num = num3;
				clickableComponent = clickableComponent2;
			}
		}
		if (clickableComponent != null)
		{
			currentlySnappedComponent = clickableComponent;
		}
	}

	protected virtual bool _ShouldAutoSnapPrioritizeAlignedElements()
	{
		return true;
	}

	public virtual bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
	{
		return true;
	}

	/// <summary>Handle the <see cref="F:StardewValley.Menus.IClickableMenu.currentlySnappedComponent" /> region changing.</summary>
	/// <param name="oldRegion">The previous value.</param>
	/// <param name="newRegion">The new value.</param>
	protected virtual void actionOnRegionChange(int oldRegion, int newRegion)
	{
	}

	public ClickableComponent getComponentWithID(int id)
	{
		if (id == -500)
		{
			return null;
		}
		if (allClickableComponents != null)
		{
			for (int i = 0; i < allClickableComponents.Count; i++)
			{
				if (allClickableComponents[i] != null && allClickableComponents[i].myID == id && allClickableComponents[i].visible)
				{
					return allClickableComponents[i];
				}
			}
			for (int j = 0; j < allClickableComponents.Count; j++)
			{
				if (allClickableComponents[j] != null && allClickableComponents[j].myAlternateID == id && allClickableComponents[j].visible)
				{
					return allClickableComponents[j];
				}
			}
		}
		return null;
	}

	public void initializeUpperRightCloseButton()
	{
		upperRightCloseButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 36, yPositionOnScreen - 8, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);
	}

	public virtual void drawBackground(SpriteBatch b)
	{
		if (this is ShopMenu)
		{
			for (int i = 0; i < Game1.uiViewport.Width; i += 400)
			{
				for (int j = 0; j < Game1.uiViewport.Height; j += 384)
				{
					b.Draw(Game1.mouseCursors, new Vector2(i, j), new Rectangle(527, 0, 100, 96), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.08f);
				}
			}
			return;
		}
		if (Game1.isDarkOut(Game1.currentLocation))
		{
			b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(639, 858, 1, 144), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
		}
		else if (Game1.IsRainingHere())
		{
			b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(640, 858, 1, 184), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
		}
		else
		{
			b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Rectangle(639 + Game1.seasonIndex, 1051, 1, 400), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
		}
		b.Draw(Game1.mouseCursors, new Vector2(-120f, Game1.uiViewport.Height - 592), new Rectangle(0, (Game1.season == Season.Winter) ? 1035 : ((Game1.isRaining || Game1.isDarkOut(Game1.currentLocation)) ? 886 : 737), 639, 148), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.08f);
		b.Draw(Game1.mouseCursors, new Vector2(2436f, Game1.uiViewport.Height - 592), new Rectangle(0, (Game1.season == Season.Winter) ? 1035 : ((Game1.isRaining || Game1.isDarkOut(Game1.currentLocation)) ? 886 : 737), 639, 148), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.08f);
		if (Game1.isRaining)
		{
			b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Blue * 0.2f);
		}
	}

	public virtual bool showWithoutTransparencyIfOptionIsSet()
	{
		if (this is GameMenu || this is ShopMenu || this is WheelSpinGame || this is ItemGrabMenu)
		{
			return true;
		}
		return false;
	}

	public virtual void clickAway()
	{
	}

	/// <summary>Update the menu when the game window is resized.</summary>
	/// <param name="oldBounds">The window's previous pixel size.</param>
	/// <param name="newBounds">The window's new pixel size.</param>
	public virtual void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		xPositionOnScreen = (int)((float)(newBounds.Width - width) * ((float)xPositionOnScreen / (float)(oldBounds.Width - width)));
		yPositionOnScreen = (int)((float)(newBounds.Height - height) * ((float)yPositionOnScreen / (float)(oldBounds.Height - height)));
	}

	public virtual void setUpForGamePadMode()
	{
	}

	public virtual bool shouldClampGamePadCursor()
	{
		return false;
	}

	/// <summary>Handle the left-click button being released (including a button resulting in a 'click' through controller selection).</summary>
	/// <param name="x">The cursor's current pixel X coordinate.</param>
	/// <param name="y">The cursor's current pixel Y coordinate.</param>
	public virtual void releaseLeftClick(int x, int y)
	{
	}

	/// <summary>Handle the left-click button being held down (including a button resulting in a 'click' through controller selection). This is called each tick that it's held.</summary>
	/// <param name="x">The cursor's current pixel X coordinate.</param>
	/// <param name="y">The cursor's current pixel Y coordinate.</param>
	public virtual void leftClickHeld(int x, int y)
	{
	}

	/// <summary>Handle a user left-click in the UI (including a 'click' through controller selection).</summary>
	/// <param name="x">The pixel X coordinate that was clicked.</param>
	/// <param name="y">The pixel Y coordinate that was clicked.</param>
	/// <param name="playSound">Whether to play sounds in response to the click, if applicable.</param>
	public virtual void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (upperRightCloseButton != null && readyToClose() && upperRightCloseButton.containsPoint(x, y))
		{
			if (playSound)
			{
				Game1.playSound(closeSound);
			}
			exitThisMenu();
		}
	}

	/// <summary>Get whether controller-style menus should be disabled for this menu.</summary>
	public virtual bool overrideSnappyMenuCursorMovementBan()
	{
		return false;
	}

	/// <summary>Handle a user right-click in the UI (including a 'click' through a controller <see cref="F:Microsoft.Xna.Framework.Input.Buttons.X" /> button).</summary>
	/// <param name="x">The pixel X coordinate that was clicked.</param>
	/// <param name="y">The pixel Y coordinate that was clicked.</param>
	/// <param name="playSound">Whether to play sounds in response to the click, if applicable.</param>
	public virtual void receiveRightClick(int x, int y, bool playSound = true)
	{
	}

	/// <summary>Handle a keyboard button pressed while the menu is open.</summary>
	/// <param name="key">The keyboard button that was pressed.</param>
	public virtual void receiveKeyPress(Keys key)
	{
		if (key != Keys.None)
		{
			if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && readyToClose())
			{
				exitThisMenu();
			}
			else if (Game1.options.snappyMenus && Game1.options.gamepadControls && !overrideSnappyMenuCursorMovementBan())
			{
				applyMovementKey(key);
			}
		}
	}

	/// <summary>Handle a controller button held down while the menu is open. This is called each tick that it's held.</summary>
	/// <param name="b">The button being held.</param>
	public virtual void gamePadButtonHeld(Buttons b)
	{
	}

	public virtual ClickableComponent getCurrentlySnappedComponent()
	{
		return currentlySnappedComponent;
	}

	/// <summary>Handle the scroll wheel being spun while the menu is open. This is called each time the scroll wheel value changes.</summary>
	/// <param name="direction">The change relative to the previous value.</param>
	public virtual void receiveScrollWheelAction(int direction)
	{
	}

	/// <summary>Handle the cursor hovering over the menu. This is called each tick, sometimes regardless of whether the cursor is within the menu's bounds.</summary>
	/// <param name="x">The pixel X coordinate being hovered by the cursor.</param>
	/// <param name="y">The pixel Y coordinate being hovered by the cursor.</param>
	public virtual void performHoverAction(int x, int y)
	{
		upperRightCloseButton?.tryHover(x, y, 0.5f);
	}

	/// <summary>Render the UI.</summary>
	/// <param name="b">The sprite batch being drawn.</param>
	/// <param name="red">If the menu can be tinted, a red tint to apply (as a value between 0 and 255) or -1 for no tint.</param>
	/// <param name="green">If the menu can be tinted, a green tint to apply (as a value between 0 and 255) or -1 for no tint.</param>
	/// <param name="blue">If the menu can be tinted, a blue tint to apply (as a value between 0 and 255) or -1 for no tint.</param>
	public virtual void draw(SpriteBatch b, int red, int green, int blue)
	{
		if (upperRightCloseButton != null && shouldDrawCloseButton())
		{
			upperRightCloseButton.draw(b);
		}
	}

	/// <summary>Render the UI.</summary>
	/// <param name="b">The sprite batch being drawn.</param>
	public virtual void draw(SpriteBatch b)
	{
		if (upperRightCloseButton != null && shouldDrawCloseButton())
		{
			upperRightCloseButton.draw(b);
		}
	}

	public virtual bool isWithinBounds(int x, int y)
	{
		if (x - xPositionOnScreen < width && x - xPositionOnScreen >= 0 && y - yPositionOnScreen < height)
		{
			return y - yPositionOnScreen >= 0;
		}
		return false;
	}

	/// <summary>Update the menu state if needed.</summary>
	/// <param name="time">The elapsed game time.</param>
	public virtual void update(GameTime time)
	{
	}

	/// <summary>Perform any cleanup needed when the menu exits.</summary>
	protected virtual void cleanupBeforeExit()
	{
	}

	public virtual bool shouldDrawCloseButton()
	{
		return true;
	}

	public void exitThisMenuNoSound()
	{
		exitThisMenu(playSound: false);
	}

	public void exitThisMenu(bool playSound = true)
	{
		behaviorBeforeCleanup?.Invoke(this);
		cleanupBeforeExit();
		if (playSound)
		{
			Game1.playSound(closeSound);
		}
		if (this == Game1.activeClickableMenu)
		{
			Game1.exitActiveMenu();
		}
		else if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.GetCurrentPage() == this)
		{
			Game1.exitActiveMenu();
		}
		if (_parentMenu != null)
		{
			IClickableMenu parentMenu = _parentMenu;
			_parentMenu = null;
			parentMenu.SetChildMenu(null);
		}
		if (exitFunction != null)
		{
			onExit onExit = exitFunction;
			exitFunction = null;
			onExit();
		}
	}

	public virtual void emergencyShutDown()
	{
	}

	public virtual bool readyToClose()
	{
		return true;
	}

	protected void drawHorizontalPartition(SpriteBatch b, int yPosition, bool small = false, int red = -1, int green = -1, int blue = -1)
	{
		Color color = ((red == -1) ? Color.White : new Color(red, green, blue));
		Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
		if (small)
		{
			b.Draw(texture, new Rectangle(xPositionOnScreen + 32, yPosition, width - 64, 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 25), color);
			return;
		}
		b.Draw(texture, new Vector2(xPositionOnScreen, yPosition), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 4), color);
		b.Draw(texture, new Rectangle(xPositionOnScreen + 64, yPosition, width - 128, 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 6), color);
		b.Draw(texture, new Vector2(xPositionOnScreen + width - 64, yPosition), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 7), color);
	}

	protected void drawVerticalPartition(SpriteBatch b, int xPosition, bool small = false, int red = -1, int green = -1, int blue = -1, int heightOverride = -1)
	{
		Color color = ((red == -1) ? Color.White : new Color(red, green, blue));
		Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
		if (small)
		{
			b.Draw(texture, new Rectangle(xPosition, yPositionOnScreen + 64 + 32, 64, (heightOverride != -1) ? heightOverride : (height - 128)), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 26), color);
			return;
		}
		b.Draw(texture, new Vector2(xPosition, yPositionOnScreen + 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 1), color);
		b.Draw(texture, new Rectangle(xPosition, yPositionOnScreen + 128, 64, (heightOverride != -1) ? heightOverride : (height - 192)), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 5), color);
		b.Draw(texture, new Vector2(xPosition, yPositionOnScreen + ((heightOverride != -1) ? heightOverride : (height - 64))), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 13), color);
	}

	protected void drawVerticalIntersectingPartition(SpriteBatch b, int xPosition, int yPosition, int red = -1, int green = -1, int blue = -1)
	{
		Color color = ((red == -1) ? Color.White : new Color(red, green, blue));
		Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
		b.Draw(texture, new Vector2(xPosition, yPosition), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 59), color);
		b.Draw(texture, new Rectangle(xPosition, yPosition + 64, 64, yPositionOnScreen + height - 64 - yPosition - 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 63), color);
		b.Draw(texture, new Vector2(xPosition, yPositionOnScreen + height - 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 62), color);
	}

	protected void drawVerticalUpperIntersectingPartition(SpriteBatch b, int xPosition, int partitionHeight, int red = -1, int green = -1, int blue = -1)
	{
		Color color = ((red == -1) ? Color.White : new Color(red, green, blue));
		Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
		b.Draw(texture, new Vector2(xPosition, yPositionOnScreen + 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 44), color);
		b.Draw(texture, new Rectangle(xPosition, yPositionOnScreen + 128, 64, partitionHeight - 32), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 63), color);
		b.Draw(texture, new Vector2(xPosition, yPositionOnScreen + partitionHeight + 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 39), color);
	}

	public static void drawTextureBox(SpriteBatch b, int x, int y, int width, int height, Color color)
	{
		drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, color);
	}

	public static void drawTextureBox(SpriteBatch b, Texture2D texture, Rectangle sourceRect, int x, int y, int width, int height, Color color, float scale = 1f, bool drawShadow = true, float draw_layer = -1f)
	{
		int num = sourceRect.Width / 3;
		float layerDepth = draw_layer - 0.03f;
		if (draw_layer < 0f)
		{
			draw_layer = 0.8f - (float)y * 1E-06f;
			layerDepth = 0.77f;
		}
		if (drawShadow)
		{
			b.Draw(texture, new Vector2(x + width - (int)((float)num * scale) - 8, y + 8), new Rectangle(sourceRect.X + num * 2, sourceRect.Y, num, num), Color.Black * 0.4f, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
			b.Draw(texture, new Vector2(x - 8, y + height - (int)((float)num * scale) + 8), new Rectangle(sourceRect.X, num * 2 + sourceRect.Y, num, num), Color.Black * 0.4f, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
			b.Draw(texture, new Vector2(x + width - (int)((float)num * scale) - 8, y + height - (int)((float)num * scale) + 8), new Rectangle(sourceRect.X + num * 2, num * 2 + sourceRect.Y, num, num), Color.Black * 0.4f, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
			b.Draw(texture, new Rectangle(x + (int)((float)num * scale) - 8, y + 8, width - (int)((float)num * scale) * 2, (int)((float)num * scale)), new Rectangle(sourceRect.X + num, sourceRect.Y, num, num), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
			b.Draw(texture, new Rectangle(x + (int)((float)num * scale) - 8, y + height - (int)((float)num * scale) + 8, width - (int)((float)num * scale) * 2, (int)((float)num * scale)), new Rectangle(sourceRect.X + num, num * 2 + sourceRect.Y, num, num), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
			b.Draw(texture, new Rectangle(x - 8, y + (int)((float)num * scale) + 8, (int)((float)num * scale), height - (int)((float)num * scale) * 2), new Rectangle(sourceRect.X, num + sourceRect.Y, num, num), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
			b.Draw(texture, new Rectangle(x + width - (int)((float)num * scale) - 8, y + (int)((float)num * scale) + 8, (int)((float)num * scale), height - (int)((float)num * scale) * 2), new Rectangle(sourceRect.X + num * 2, num + sourceRect.Y, num, num), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
			b.Draw(texture, new Rectangle((int)((float)num * scale / 2f) + x - 8, (int)((float)num * scale / 2f) + y + 8, width - (int)((float)num * scale), height - (int)((float)num * scale)), new Rectangle(num + sourceRect.X, num + sourceRect.Y, num, num), Color.Black * 0.4f, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
		}
		b.Draw(texture, new Rectangle((int)((float)num * scale) + x, (int)((float)num * scale) + y, width - (int)((float)num * scale * 2f), height - (int)((float)num * scale * 2f)), new Rectangle(num + sourceRect.X, num + sourceRect.Y, num, num), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Vector2(x, y), new Rectangle(sourceRect.X, sourceRect.Y, num, num), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Vector2(x + width - (int)((float)num * scale), y), new Rectangle(sourceRect.X + num * 2, sourceRect.Y, num, num), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Vector2(x, y + height - (int)((float)num * scale)), new Rectangle(sourceRect.X, num * 2 + sourceRect.Y, num, num), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Vector2(x + width - (int)((float)num * scale), y + height - (int)((float)num * scale)), new Rectangle(sourceRect.X + num * 2, num * 2 + sourceRect.Y, num, num), color, 0f, Vector2.Zero, scale, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Rectangle(x + (int)((float)num * scale), y, width - (int)((float)num * scale) * 2, (int)((float)num * scale)), new Rectangle(sourceRect.X + num, sourceRect.Y, num, num), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Rectangle(x + (int)((float)num * scale), y + height - (int)((float)num * scale), width - (int)((float)num * scale) * 2, (int)((float)num * scale)), new Rectangle(sourceRect.X + num, num * 2 + sourceRect.Y, num, num), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Rectangle(x, y + (int)((float)num * scale), (int)((float)num * scale), height - (int)((float)num * scale) * 2), new Rectangle(sourceRect.X, num + sourceRect.Y, num, num), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
		b.Draw(texture, new Rectangle(x + width - (int)((float)num * scale), y + (int)((float)num * scale), (int)((float)num * scale), height - (int)((float)num * scale) * 2), new Rectangle(sourceRect.X + num * 2, num + sourceRect.Y, num, num), color, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
	}

	public void drawBorderLabel(SpriteBatch b, string text, SpriteFont font, int x, int y)
	{
		int num = (int)font.MeasureString(text).X;
		y += 52;
		b.Draw(Game1.mouseCursors, new Vector2(x, y), new Rectangle(256, 267, 6, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.87f);
		b.Draw(Game1.mouseCursors, new Vector2(x + 24, y), new Rectangle(262, 267, 1, 16), Color.White, 0f, Vector2.Zero, new Vector2(num, 4f), SpriteEffects.None, 0.87f);
		b.Draw(Game1.mouseCursors, new Vector2(x + 24 + num, y), new Rectangle(263, 267, 6, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.87f);
		Utility.drawTextWithShadow(b, text, font, new Vector2(x + 24, y + 20), Game1.textColor);
	}

	public static void drawToolTip(SpriteBatch b, string hoverText, string hoverTitle, Item hoveredItem, bool heldItem = false, int healAmountToDisplay = -1, int currencySymbol = 0, string extraItemToShowIndex = null, int extraItemToShowAmount = -1, CraftingRecipe craftingIngredients = null, int moneyAmountToShowAtBottom = -1, IList<Item> additionalCraftMaterials = null)
	{
		bool flag = hoveredItem is Object obj && obj.edibility.Value != -300;
		string[] array = null;
		if (flag && Game1.objectData.TryGetValue(hoveredItem.ItemId, out var value))
		{
			BuffEffects buffEffects = new BuffEffects();
			int num = int.MinValue;
			foreach (Buff item in Object.TryCreateBuffsFromData(value, hoveredItem.Name, hoveredItem.DisplayName, 1f, hoveredItem.ModifyItemBuffs))
			{
				buffEffects.Add(item.effects);
				if (item.millisecondsDuration == -2 || (item.millisecondsDuration > num && num != -2))
				{
					num = item.millisecondsDuration;
				}
			}
			if (buffEffects.HasAnyValue())
			{
				array = buffEffects.ToLegacyAttributeFormat();
				if (num != -2)
				{
					array[12] = " " + Utility.getMinutesSecondsStringFromMilliseconds(num);
				}
			}
		}
		drawHoverText(b, hoverText, Game1.smallFont, heldItem ? 40 : 0, heldItem ? 40 : 0, moneyAmountToShowAtBottom, hoverTitle, flag ? (hoveredItem as Object).edibility.Value : (-1), array, hoveredItem, currencySymbol, extraItemToShowIndex, extraItemToShowAmount, -1, -1, 1f, craftingIngredients, additionalCraftMaterials);
	}

	public static void drawHoverText(SpriteBatch b, string text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, string boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDisplay = null, Item hoveredItem = null, int currencySymbol = 0, string extraItemToShowIndex = null, int extraItemToShowAmount = -1, int overrideX = -1, int overrideY = -1, float alpha = 1f, CraftingRecipe craftingIngredients = null, IList<Item> additional_craft_materials = null, Texture2D boxTexture = null, Rectangle? boxSourceRect = null, Color? textColor = null, Color? textShadowColor = null, float boxScale = 1f, int boxWidthOverride = -1, int boxHeightOverride = -1)
	{
		HoverTextStringBuilder.Clear();
		HoverTextStringBuilder.Append(text);
		drawHoverText(b, HoverTextStringBuilder, font, xOffset, yOffset, moneyAmountToDisplayAtBottom, boldTitleText, healAmountToDisplay, buffIconsToDisplay, hoveredItem, currencySymbol, extraItemToShowIndex, extraItemToShowAmount, overrideX, overrideY, alpha, craftingIngredients, additional_craft_materials, boxTexture, boxSourceRect, textColor, textShadowColor, boxScale, boxWidthOverride, boxHeightOverride);
	}

	public static void drawHoverText(SpriteBatch b, StringBuilder text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, string boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDisplay = null, Item hoveredItem = null, int currencySymbol = 0, string extraItemToShowIndex = null, int extraItemToShowAmount = -1, int overrideX = -1, int overrideY = -1, float alpha = 1f, CraftingRecipe craftingIngredients = null, IList<Item> additional_craft_materials = null, Texture2D boxTexture = null, Rectangle? boxSourceRect = null, Color? textColor = null, Color? textShadowColor = null, float boxScale = 1f, int boxWidthOverride = -1, int boxHeightOverride = -1)
	{
		boxTexture = boxTexture ?? Game1.menuTexture;
		boxSourceRect = boxSourceRect ?? new Rectangle(0, 256, 60, 60);
		textColor = textColor ?? Game1.textColor;
		textShadowColor = textShadowColor ?? Game1.textShadowColor;
		if (text == null || text.Length == 0)
		{
			return;
		}
		if (hoveredItem != null && craftingIngredients != null && hoveredItem.getDescription().Equals(text.ToString()))
		{
			text = new StringBuilder(" ");
		}
		if (moneyAmountToDisplayAtBottom <= -1 && currencySymbol == 0 && hoveredItem != null && Game1.player.stats.Get("Book_PriceCatalogue") != 0 && !(hoveredItem is Furniture) && hoveredItem.CanBeLostOnDeath() && !(hoveredItem is Clothing) && !(hoveredItem is Wallpaper) && (!(hoveredItem is Object) || !(hoveredItem as Object).bigCraftable.Value) && hoveredItem.sellToStorePrice(-1L) > 0)
		{
			moneyAmountToDisplayAtBottom = hoveredItem.sellToStorePrice(-1L) * hoveredItem.Stack;
		}
		string text2 = null;
		if (boldTitleText != null && boldTitleText.Length == 0)
		{
			boldTitleText = null;
		}
		int num = Math.Max((healAmountToDisplay != -1) ? ((int)font.MeasureString(healAmountToDisplay + "+ Energy" + 32).X) : 0, Math.Max((int)font.MeasureString(text).X, (boldTitleText != null) ? ((int)Game1.dialogueFont.MeasureString(boldTitleText).X) : 0)) + 32;
		int num2 = Math.Max(20 * 3, (int)font.MeasureString(text).Y + 32 + (int)((moneyAmountToDisplayAtBottom > -1) ? Math.Max(font.MeasureString(moneyAmountToDisplayAtBottom.ToString() ?? "").Y + 4f, 44f) : 0f) + (int)((boldTitleText != null) ? (Game1.dialogueFont.MeasureString(boldTitleText).Y + 16f) : 0f));
		if (extraItemToShowIndex != null)
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(O)" + extraItemToShowIndex);
			string displayName = dataOrErrorItem.DisplayName;
			Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
			string text3 = Game1.content.LoadString("Strings\\UI:ItemHover_Requirements", extraItemToShowAmount, (extraItemToShowAmount > 1) ? Lexicon.makePlural(displayName) : displayName);
			int num3 = sourceRect.Width * 2 * 4;
			num = Math.Max(num, num3 + (int)font.MeasureString(text3).X);
		}
		if (buffIconsToDisplay != null)
		{
			foreach (string text4 in buffIconsToDisplay)
			{
				if (!text4.Equals("0") && text4 != "")
				{
					num2 += 39;
				}
			}
			num2 += 4;
		}
		if (craftingIngredients != null && Game1.options.showAdvancedCraftingInformation && craftingIngredients.getCraftCountText() != null)
		{
			num2 += (int)font.MeasureString("T").Y + 2;
		}
		string text5 = null;
		if (hoveredItem != null)
		{
			if (hoveredItem is FishingRod)
			{
				if (hoveredItem.attachmentSlots() == 1)
				{
					num2 += 68;
				}
				else if (hoveredItem.attachmentSlots() > 1)
				{
					num2 += 144;
				}
			}
			else
			{
				num2 += 68 * hoveredItem.attachmentSlots();
			}
			text5 = hoveredItem.getCategoryName();
			if (text5.Length > 0)
			{
				num = Math.Max(num, (int)font.MeasureString(text5).X + 32);
				num2 += (int)font.MeasureString("T").Y;
			}
			int num4 = 9999;
			int num5 = 92;
			Point extraSpaceNeededForTooltipSpecialIcons = hoveredItem.getExtraSpaceNeededForTooltipSpecialIcons(font, num, num5, num2, text, boldTitleText, moneyAmountToDisplayAtBottom);
			num = ((extraSpaceNeededForTooltipSpecialIcons.X != 0) ? extraSpaceNeededForTooltipSpecialIcons.X : num);
			num2 = ((extraSpaceNeededForTooltipSpecialIcons.Y != 0) ? extraSpaceNeededForTooltipSpecialIcons.Y : num2);
			if (!(hoveredItem is MeleeWeapon meleeWeapon))
			{
				if (hoveredItem is Object obj && obj.edibility.Value != -300 && obj.edibility.Value != 0)
				{
					healAmountToDisplay = obj.staminaRecoveredOnConsumption();
					num2 = ((healAmountToDisplay == -1) ? (num2 + 40) : (num2 + 40 * ((healAmountToDisplay <= 0 || obj.healthRecoveredOnConsumption() <= 0) ? 1 : 2)));
					if (Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.zh && Game1.options.useChineseSmoothFont)
					{
						num2 += 16;
					}
					num = (int)Math.Max(num, Math.Max(font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Energy", num4)).X + (float)num5, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Health", num4)).X + (float)num5));
				}
			}
			else
			{
				if (meleeWeapon.GetTotalForgeLevels() > 0)
				{
					num2 += (int)font.MeasureString("T").Y;
				}
				if (meleeWeapon.GetEnchantmentLevel<GalaxySoulEnchantment>() > 0)
				{
					num2 += (int)font.MeasureString("T").Y;
				}
			}
			if (buffIconsToDisplay != null)
			{
				for (int j = 0; j < buffIconsToDisplay.Length; j++)
				{
					if (!buffIconsToDisplay[j].Equals("0") && j <= 12)
					{
						num = (int)Math.Max(num, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_Buff" + j, num4)).X + (float)num5);
					}
				}
			}
		}
		Vector2 vector = Vector2.Zero;
		if (craftingIngredients != null)
		{
			if (Game1.options.showAdvancedCraftingInformation)
			{
				int craftableCount = craftingIngredients.getCraftableCount(additional_craft_materials);
				if (craftableCount > 1)
				{
					text2 = " (" + craftableCount + ")";
					vector = Game1.smallFont.MeasureString(text2);
				}
			}
			num = (int)Math.Max(Game1.dialogueFont.MeasureString(boldTitleText).X + vector.X + 12f, 384f);
			num2 += craftingIngredients.getDescriptionHeight(num + 4 - 8) - 32;
			if (craftingIngredients != null && hoveredItem != null && hoveredItem.getDescription().Equals(text.ToString()))
			{
				num2 -= (int)font.MeasureString(text.ToString()).Y;
			}
			if (craftingIngredients != null && Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.zh)
			{
				num2 += 8;
			}
		}
		else if (text2 != null && boldTitleText != null)
		{
			vector = Game1.smallFont.MeasureString(text2);
			num = (int)Math.Max(num, Game1.dialogueFont.MeasureString(boldTitleText).X + vector.X + 12f);
		}
		int x = Game1.getOldMouseX() + 32 + xOffset;
		int num6 = Game1.getOldMouseY() + 32 + yOffset;
		if (overrideX != -1)
		{
			x = overrideX;
		}
		if (overrideY != -1)
		{
			num6 = overrideY;
		}
		if (x + num > Utility.getSafeArea().Right)
		{
			x = Utility.getSafeArea().Right - num;
			num6 += 16;
		}
		if (num6 + num2 > Utility.getSafeArea().Bottom)
		{
			x += 16;
			if (x + num > Utility.getSafeArea().Right)
			{
				x = Utility.getSafeArea().Right - num;
			}
			num6 = Utility.getSafeArea().Bottom - num2;
		}
		num += 4;
		int num7 = ((boxWidthOverride != -1) ? boxWidthOverride : (num + ((craftingIngredients != null) ? 21 : 0)));
		int num8 = ((boxHeightOverride != -1) ? boxHeightOverride : num2);
		drawTextureBox(b, boxTexture, boxSourceRect.Value, x, num6, num7, num8, Color.White * alpha, boxScale);
		if (boldTitleText != null)
		{
			Vector2 vector2 = Game1.dialogueFont.MeasureString(boldTitleText);
			drawTextureBox(b, boxTexture, boxSourceRect.Value, x, num6, num + ((craftingIngredients != null) ? 21 : 0), (int)Game1.dialogueFont.MeasureString(boldTitleText).Y + 32 + (int)((hoveredItem != null && text5.Length > 0) ? font.MeasureString("asd").Y : 0f) - 4, Color.White * alpha, 1f, drawShadow: false);
			b.Draw(Game1.menuTexture, new Rectangle(x + 12, num6 + (int)Game1.dialogueFont.MeasureString(boldTitleText).Y + 32 + (int)((hoveredItem != null && text5.Length > 0) ? font.MeasureString("asd").Y : 0f) - 4, num - 4 * ((craftingIngredients != null) ? 1 : 6), 4), new Rectangle(44, 300, 4, 4), Color.White);
			b.DrawString(Game1.dialogueFont, boldTitleText, new Vector2(x + 16, num6 + 16 + 4) + new Vector2(2f, 2f), textShadowColor.Value);
			b.DrawString(Game1.dialogueFont, boldTitleText, new Vector2(x + 16, num6 + 16 + 4) + new Vector2(0f, 2f), textShadowColor.Value);
			b.DrawString(Game1.dialogueFont, boldTitleText, new Vector2(x + 16, num6 + 16 + 4), textColor.Value);
			if (text2 != null)
			{
				Utility.drawTextWithShadow(b, text2, Game1.smallFont, new Vector2((float)(x + 16) + vector2.X, (int)((float)(num6 + 16 + 4) + vector2.Y / 2f - vector.Y / 2f)), Game1.textColor);
			}
			num6 += (int)Game1.dialogueFont.MeasureString(boldTitleText).Y;
		}
		if (hoveredItem != null && text5.Length > 0)
		{
			num6 -= 4;
			Utility.drawTextWithShadow(b, text5, font, new Vector2(x + 16, num6 + 16 + 4), hoveredItem.getCategoryColor(), 1f, -1f, 2, 2);
			num6 += (int)font.MeasureString("T").Y + ((boldTitleText != null) ? 16 : 0) + 4;
			if (hoveredItem is Tool tool && tool.GetTotalForgeLevels() > 0)
			{
				string text6 = Game1.content.LoadString("Strings\\UI:Item_Tooltip_Forged");
				Utility.drawTextWithShadow(b, text6, font, new Vector2(x + 16, num6 + 16 + 4), Color.DarkRed, 1f, -1f, 2, 2);
				int totalForgeLevels = tool.GetTotalForgeLevels();
				if (totalForgeLevels < tool.GetMaxForges() && !tool.hasEnchantmentOfType<DiamondEnchantment>())
				{
					Utility.drawTextWithShadow(b, " (" + totalForgeLevels + "/" + tool.GetMaxForges() + ")", font, new Vector2((float)(x + 16) + font.MeasureString(text6).X, num6 + 16 + 4), Color.DimGray, 1f, -1f, 2, 2);
				}
				num6 += (int)font.MeasureString("T").Y;
			}
			if (hoveredItem is MeleeWeapon meleeWeapon2 && meleeWeapon2.GetEnchantmentLevel<GalaxySoulEnchantment>() > 0)
			{
				GalaxySoulEnchantment enchantmentOfType = meleeWeapon2.GetEnchantmentOfType<GalaxySoulEnchantment>();
				string text7 = Game1.content.LoadString("Strings\\UI:Item_Tooltip_GalaxyForged");
				Utility.drawTextWithShadow(b, text7, font, new Vector2(x + 16, num6 + 16 + 4), Color.DarkRed, 1f, -1f, 2, 2);
				int level = enchantmentOfType.GetLevel();
				if (level < enchantmentOfType.GetMaximumLevel())
				{
					Utility.drawTextWithShadow(b, " (" + level + "/" + enchantmentOfType.GetMaximumLevel() + ")", font, new Vector2((float)(x + 16) + font.MeasureString(text7).X, num6 + 16 + 4), Color.DimGray, 1f, -1f, 2, 2);
				}
				num6 += (int)font.MeasureString("T").Y;
			}
		}
		else
		{
			num6 += ((boldTitleText != null) ? 16 : 0);
		}
		if (hoveredItem != null && craftingIngredients == null)
		{
			hoveredItem.drawTooltip(b, ref x, ref num6, font, alpha, text);
		}
		else if (text != null && text.Length != 0 && (text.Length != 1 || text[0] != ' ') && (craftingIngredients == null || hoveredItem == null || !hoveredItem.getDescription().Equals(text.ToString())))
		{
			if (text.ToString().Contains("[line]"))
			{
				string[] array = text.ToString().Split("[line]");
				b.DrawString(font, array[0], new Vector2(x + 16, num6 + 16 + 4) + new Vector2(2f, 2f), textShadowColor.Value * alpha);
				b.DrawString(font, array[0], new Vector2(x + 16, num6 + 16 + 4) + new Vector2(0f, 2f), textShadowColor.Value * alpha);
				b.DrawString(font, array[0], new Vector2(x + 16, num6 + 16 + 4) + new Vector2(2f, 0f), textShadowColor.Value * alpha);
				b.DrawString(font, array[0], new Vector2(x + 16, num6 + 16 + 4), textColor.Value * 0.9f * alpha);
				num6 += (int)font.MeasureString(array[0]).Y - 16;
				Utility.drawLineWithScreenCoordinates(x + 16 - 4, num6 + 16 + 4, x + 16 + num - 28, num6 + 16 + 4, b, textShadowColor.Value);
				Utility.drawLineWithScreenCoordinates(x + 16 - 4, num6 + 16 + 5, x + 16 + num - 28, num6 + 16 + 5, b, textShadowColor.Value);
				if (array.Length > 1)
				{
					num6 -= 16;
					b.DrawString(font, array[1], new Vector2(x + 16, num6 + 16 + 4) + new Vector2(2f, 2f), textShadowColor.Value * alpha);
					b.DrawString(font, array[1], new Vector2(x + 16, num6 + 16 + 4) + new Vector2(0f, 2f), textShadowColor.Value * alpha);
					b.DrawString(font, array[1], new Vector2(x + 16, num6 + 16 + 4) + new Vector2(2f, 0f), textShadowColor.Value * alpha);
					b.DrawString(font, array[1], new Vector2(x + 16, num6 + 16 + 4), textColor.Value * 0.9f * alpha);
					num6 += (int)font.MeasureString(array[1]).Y;
				}
				num6 += 4;
			}
			else
			{
				b.DrawString(font, text, new Vector2(x + 16, num6 + 16 + 4) + new Vector2(2f, 2f), textShadowColor.Value * alpha);
				b.DrawString(font, text, new Vector2(x + 16, num6 + 16 + 4) + new Vector2(0f, 2f), textShadowColor.Value * alpha);
				b.DrawString(font, text, new Vector2(x + 16, num6 + 16 + 4) + new Vector2(2f, 0f), textShadowColor.Value * alpha);
				b.DrawString(font, text, new Vector2(x + 16, num6 + 16 + 4), textColor.Value * 0.9f * alpha);
				num6 += (int)font.MeasureString(text).Y + 4;
			}
		}
		if (craftingIngredients != null)
		{
			craftingIngredients.drawRecipeDescription(b, new Vector2(x + 16, num6 - 8), num, additional_craft_materials);
			num6 += craftingIngredients.getDescriptionHeight(num - 8);
		}
		if (healAmountToDisplay != -1)
		{
			int num9 = (hoveredItem as Object).staminaRecoveredOnConsumption();
			if (Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.zh)
			{
				num6 += 8;
			}
			if (num9 >= 0)
			{
				int num10 = (hoveredItem as Object).healthRecoveredOnConsumption();
				if (num9 > 0)
				{
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x + 16 + 4, num6 + 16), new Rectangle(0, 428, 10, 10), Color.White, 0f, Vector2.Zero, 3f, flipped: false, 0.95f);
					Utility.drawTextWithShadow(b, (num9 >= 999) ? " 100%" : Game1.content.LoadString("Strings\\UI:ItemHover_Energy", "+" + num9), font, new Vector2(x + 16 + 34 + 4, num6 + 16), Game1.textColor);
					num6 += 34;
				}
				if (num10 > 0)
				{
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x + 16 + 4, num6 + 16), new Rectangle(0, 438, 10, 10), Color.White, 0f, Vector2.Zero, 3f, flipped: false, 0.95f);
					Utility.drawTextWithShadow(b, (num10 >= 999) ? " 100%" : Game1.content.LoadString("Strings\\UI:ItemHover_Health", "+" + num10), font, new Vector2(x + 16 + 34 + 4, num6 + 16), Game1.textColor);
					num6 += 34;
				}
			}
			else if (num9 != -300)
			{
				Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x + 16 + 4, num6 + 16), new Rectangle(140, 428, 10, 10), Color.White, 0f, Vector2.Zero, 3f, flipped: false, 0.95f);
				Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:ItemHover_Energy", num9.ToString() ?? ""), font, new Vector2(x + 16 + 34 + 4, num6 + 16), Game1.textColor);
				num6 += 34;
			}
		}
		if (buffIconsToDisplay != null)
		{
			num6 += 16;
			b.Draw(Game1.staminaRect, new Rectangle(x + 12, num6 + 6, num - ((craftingIngredients != null) ? 4 : 24), 2), new Color(207, 147, 103) * 0.8f);
			for (int k = 0; k < buffIconsToDisplay.Length; k++)
			{
				if (buffIconsToDisplay[k].Equals("0") || !(buffIconsToDisplay[k] != ""))
				{
					continue;
				}
				if (k == 12)
				{
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x + 16 + 4, num6 + 16), new Rectangle(410, 501, 9, 9), Color.White, 0f, Vector2.Zero, 3f, flipped: false, 0.95f);
					Utility.drawTextWithShadow(b, buffIconsToDisplay[k], font, new Vector2(x + 16 + 34 + 4, num6 + 16), Game1.textColor);
				}
				else
				{
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(x + 16 + 4, num6 + 16), new Rectangle(10 + k * 10, 428, 10, 10), Color.White, 0f, Vector2.Zero, 3f, flipped: false, 0.95f);
					string text8 = ((Convert.ToDouble(buffIconsToDisplay[k]) > 0.0) ? "+" : "") + buffIconsToDisplay[k];
					if (k <= 11)
					{
						text8 = Game1.content.LoadString("Strings\\UI:ItemHover_Buff" + k, text8);
					}
					Utility.drawTextWithShadow(b, text8, font, new Vector2(x + 16 + 34 + 4, num6 + 16), Game1.textColor);
				}
				num6 += 39;
			}
			num6 -= 8;
		}
		if (hoveredItem != null && hoveredItem.attachmentSlots() > 0)
		{
			hoveredItem.drawAttachments(b, x + 16, num6 + 16);
			if (moneyAmountToDisplayAtBottom > -1)
			{
				num6 += 68 * hoveredItem.attachmentSlots();
			}
		}
		if (moneyAmountToDisplayAtBottom > -1)
		{
			b.Draw(Game1.staminaRect, new Rectangle(x + 12, num6 + 22 - ((healAmountToDisplay <= 0) ? 6 : 0), num - ((craftingIngredients != null) ? 4 : 24), 2), new Color(207, 147, 103) * 0.5f);
			string text9 = moneyAmountToDisplayAtBottom.ToString();
			int num11 = 0;
			if ((buffIconsToDisplay != null && buffIconsToDisplay.Length > 1) || healAmountToDisplay > 0 || craftingIngredients != null)
			{
				num11 = 8;
			}
			b.DrawString(font, text9, new Vector2(x + 16, num6 + 16 + 4 + num11) + new Vector2(2f, 2f), textShadowColor.Value);
			b.DrawString(font, text9, new Vector2(x + 16, num6 + 16 + 4 + num11) + new Vector2(0f, 2f), textShadowColor.Value);
			b.DrawString(font, text9, new Vector2(x + 16, num6 + 16 + 4 + num11) + new Vector2(2f, 0f), textShadowColor.Value);
			b.DrawString(font, text9, new Vector2(x + 16, num6 + 16 + 4 + num11), textColor.Value);
			switch (currencySymbol)
			{
			case 0:
				b.Draw(Game1.debrisSpriteSheet, new Vector2((float)(x + 16) + font.MeasureString(text9).X + 20f, num6 + 16 + 20 + num11), Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16), Color.White, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, 0.95f);
				break;
			case 1:
				b.Draw(Game1.mouseCursors, new Vector2((float)(x + 8) + font.MeasureString(text9).X + 20f, num6 + 16 - 5 + num11), new Rectangle(338, 400, 8, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				break;
			case 2:
				b.Draw(Game1.mouseCursors, new Vector2((float)(x + 8) + font.MeasureString(text9).X + 20f, num6 + 16 - 7 + num11), new Rectangle(211, 373, 9, 10), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				break;
			case 4:
				b.Draw(Game1.objectSpriteSheet, new Vector2((float)(x + 8) + font.MeasureString(text9).X + 20f, num6 + 16 - 7 + num11), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 858, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				break;
			}
			num6 += 48;
			if (extraItemToShowIndex != null)
			{
				num6 += num11;
			}
		}
		if (extraItemToShowIndex != null)
		{
			if (moneyAmountToDisplayAtBottom == -1)
			{
				num6 += 8;
			}
			ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(extraItemToShowIndex);
			string displayName2 = dataOrErrorItem2.DisplayName;
			Texture2D texture = dataOrErrorItem2.GetTexture();
			Rectangle sourceRect2 = dataOrErrorItem2.GetSourceRect();
			string text10 = Game1.content.LoadString("Strings\\UI:ItemHover_Requirements", extraItemToShowAmount, displayName2);
			float num12 = Math.Max(font.MeasureString(text10).Y + 21f, 96f);
			drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, num6 + 4, num + ((craftingIngredients != null) ? 21 : 0), (int)num12, Color.White);
			num6 += 20;
			b.DrawString(font, text10, new Vector2(x + 16, num6 + 4) + new Vector2(2f, 2f), textShadowColor.Value);
			b.DrawString(font, text10, new Vector2(x + 16, num6 + 4) + new Vector2(0f, 2f), textShadowColor.Value);
			b.DrawString(font, text10, new Vector2(x + 16, num6 + 4) + new Vector2(2f, 0f), textShadowColor.Value);
			b.DrawString(Game1.smallFont, text10, new Vector2(x + 16, num6 + 4), textColor.Value);
			b.Draw(texture, new Vector2(x + 16 + (int)font.MeasureString(text10).X + 21, num6), sourceRect2, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		}
		if (craftingIngredients != null && Game1.options.showAdvancedCraftingInformation)
		{
			Utility.drawTextWithShadow(b, craftingIngredients.getCraftCountText(), font, new Vector2(x + 16, num6 + 16 + 4), Game1.textColor, 1f, -1f, 2, 2);
			num6 += (int)font.MeasureString("T").Y + 4;
		}
	}
}
