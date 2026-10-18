using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;

namespace StardewValley.Menus;

public class BuildingPaintMenu : IClickableMenu
{
	/// <summary>The data model for a paint region.</summary>
	public class RegionData
	{
		/// <summary>The unique region ID within the building's paint regions.</summary>
		public string Id { get; }

		/// <summary>The localized display name.</summary>
		public string DisplayName { get; }

		/// <summary>The minimum brightness allowed.</summary>
		public int MinBrightness { get; }

		/// <summary>The maximum brightness allowed.</summary>
		public int MaxBrightness { get; }

		/// <summary>Construct an instance.</summary>
		/// <param name="id">The unique region ID within the building's paint regions.</param>
		/// <param name="displayName">The localized display name.</param>
		/// <param name="minBrightness">The minimum brightness allowed.</param>
		/// <param name="maxBrightness">The maximum brightness allowed.</param>
		public RegionData(string id, string displayName, int minBrightness, int maxBrightness)
		{
			Id = id;
			DisplayName = displayName;
			MinBrightness = minBrightness;
			MaxBrightness = maxBrightness;
		}
	}

	public class ColorSliderPanel
	{
		public BuildingPaintMenu buildingPaintMenu;

		public int regionIndex;

		public string regionId = "Paint Region Name";

		public Rectangle rectangle;

		public Vector2 colorDrawPosition;

		public List<KeyValuePair<string, List<int>>> colors = new List<KeyValuePair<string, List<int>>>();

		public int selectedColor;

		public BuildingColorSlider hueSlider;

		public BuildingColorSlider saturationSlider;

		public BuildingColorSlider lightnessSlider;

		public int minimumBrightness = -100;

		public int maximumBrightness = 100;

		public ColorSliderPanel(BuildingPaintMenu menu, int region_index, string regionId, int min_brightness = -100, int max_brightness = 100)
		{
			regionIndex = region_index;
			buildingPaintMenu = menu;
			this.regionId = regionId;
			minimumBrightness = min_brightness;
			maximumBrightness = max_brightness;
		}

		public virtual int GetHeight()
		{
			return rectangle.Height;
		}

		public virtual Rectangle Reposition(Rectangle start_rect)
		{
			buildingPaintMenu.sliderHandles.Clear();
			rectangle.X = start_rect.X;
			rectangle.Y = start_rect.Y;
			rectangle.Width = start_rect.Width;
			rectangle.Height = 0;
			lightnessSlider = null;
			hueSlider = null;
			saturationSlider = null;
			colorDrawPosition = new Vector2(start_rect.X + start_rect.Width - 64, start_rect.Y);
			hueSlider = new BuildingColorSlider(buildingPaintMenu, 106, new Rectangle(rectangle.Left, rectangle.Bottom, rectangle.Width - 100, 12), 0, 360, delegate
			{
				switch (regionIndex)
				{
				case 0:
					buildingPaintMenu.colorTarget.Color1Default.Value = false;
					break;
				case 1:
					buildingPaintMenu.colorTarget.Color2Default.Value = false;
					break;
				default:
					buildingPaintMenu.colorTarget.Color3Default.Value = false;
					break;
				}
				ApplyColors();
			});
			BuildingColorSlider buildingColorSlider = hueSlider;
			buildingColorSlider.getDrawColor = (Func<float, Color>)Delegate.Combine(buildingColorSlider.getDrawColor, (Func<float, Color>)((float val) => GetColorForValues(val, 100f)));
			switch (regionIndex)
			{
			case 0:
				hueSlider.SetValue(buildingPaintMenu.colorTarget.Color1Hue.Value, skip_value_set: true);
				break;
			case 1:
				hueSlider.SetValue(buildingPaintMenu.colorTarget.Color2Hue.Value, skip_value_set: true);
				break;
			default:
				hueSlider.SetValue(buildingPaintMenu.colorTarget.Color3Hue.Value, skip_value_set: true);
				break;
			}
			rectangle.Height += 24;
			saturationSlider = new BuildingColorSlider(buildingPaintMenu, 107, new Rectangle(rectangle.Left, rectangle.Bottom, rectangle.Width - 100, 12), 0, 75, delegate
			{
				switch (regionIndex)
				{
				case 0:
					buildingPaintMenu.colorTarget.Color1Default.Value = false;
					break;
				case 1:
					buildingPaintMenu.colorTarget.Color2Default.Value = false;
					break;
				default:
					buildingPaintMenu.colorTarget.Color3Default.Value = false;
					break;
				}
				ApplyColors();
			});
			BuildingColorSlider buildingColorSlider2 = saturationSlider;
			buildingColorSlider2.getDrawColor = (Func<float, Color>)Delegate.Combine(buildingColorSlider2.getDrawColor, (Func<float, Color>)((float val) => GetColorForValues(hueSlider.GetValue(), val)));
			switch (regionIndex)
			{
			case 0:
				saturationSlider.SetValue(buildingPaintMenu.colorTarget.Color1Saturation.Value, skip_value_set: true);
				break;
			case 1:
				saturationSlider.SetValue(buildingPaintMenu.colorTarget.Color2Saturation.Value, skip_value_set: true);
				break;
			default:
				saturationSlider.SetValue(buildingPaintMenu.colorTarget.Color3Saturation.Value, skip_value_set: true);
				break;
			}
			rectangle.Height += 24;
			lightnessSlider = new BuildingColorSlider(buildingPaintMenu, 108, new Rectangle(rectangle.Left, rectangle.Bottom, rectangle.Width - 100, 12), minimumBrightness, maximumBrightness, delegate
			{
				switch (regionIndex)
				{
				case 0:
					buildingPaintMenu.colorTarget.Color1Default.Value = false;
					break;
				case 1:
					buildingPaintMenu.colorTarget.Color2Default.Value = false;
					break;
				default:
					buildingPaintMenu.colorTarget.Color3Default.Value = false;
					break;
				}
				ApplyColors();
			});
			BuildingColorSlider buildingColorSlider3 = lightnessSlider;
			buildingColorSlider3.getDrawColor = (Func<float, Color>)Delegate.Combine(buildingColorSlider3.getDrawColor, (Func<float, Color>)((float val) => GetColorForValues(hueSlider.GetValue(), saturationSlider.GetValue(), val)));
			switch (regionIndex)
			{
			case 0:
				lightnessSlider.SetValue(buildingPaintMenu.colorTarget.Color1Lightness.Value, skip_value_set: true);
				break;
			case 1:
				lightnessSlider.SetValue(buildingPaintMenu.colorTarget.Color2Lightness.Value, skip_value_set: true);
				break;
			default:
				lightnessSlider.SetValue(buildingPaintMenu.colorTarget.Color3Lightness.Value, skip_value_set: true);
				break;
			}
			rectangle.Height += 24;
			if ((regionIndex == 0 && buildingPaintMenu.colorTarget.Color1Default.Value) || (regionIndex == 1 && buildingPaintMenu.colorTarget.Color2Default.Value) || (regionIndex == 2 && buildingPaintMenu.colorTarget.Color3Default.Value))
			{
				hueSlider.SetValue(hueSlider.min, skip_value_set: true);
				saturationSlider.SetValue(saturationSlider.max, skip_value_set: true);
				lightnessSlider.SetValue((lightnessSlider.min + lightnessSlider.max) / 2, skip_value_set: true);
			}
			buildingPaintMenu.sliderHandles.Add(hueSlider.handle);
			buildingPaintMenu.sliderHandles.Add(saturationSlider.handle);
			buildingPaintMenu.sliderHandles.Add(lightnessSlider.handle);
			hueSlider.handle.upNeighborID = 104;
			hueSlider.handle.downNeighborID = 107;
			saturationSlider.handle.downNeighborID = 108;
			saturationSlider.handle.upNeighborID = 106;
			lightnessSlider.handle.upNeighborID = 107;
			rectangle.Height += 32;
			start_rect.Y += rectangle.Height;
			return start_rect;
		}

		public virtual void ApplyColors()
		{
			switch (regionIndex)
			{
			case 0:
				buildingPaintMenu.colorTarget.Color1Hue.Value = hueSlider.GetValue();
				buildingPaintMenu.colorTarget.Color1Saturation.Value = saturationSlider.GetValue();
				buildingPaintMenu.colorTarget.Color1Lightness.Value = lightnessSlider.GetValue();
				break;
			case 1:
				buildingPaintMenu.colorTarget.Color2Hue.Value = hueSlider.GetValue();
				buildingPaintMenu.colorTarget.Color2Saturation.Value = saturationSlider.GetValue();
				buildingPaintMenu.colorTarget.Color2Lightness.Value = lightnessSlider.GetValue();
				break;
			default:
				buildingPaintMenu.colorTarget.Color3Hue.Value = hueSlider.GetValue();
				buildingPaintMenu.colorTarget.Color3Saturation.Value = saturationSlider.GetValue();
				buildingPaintMenu.colorTarget.Color3Lightness.Value = lightnessSlider.GetValue();
				break;
			}
		}

		public virtual void Draw(SpriteBatch b)
		{
			if ((regionIndex != 0 || !buildingPaintMenu.colorTarget.Color1Default.Value) && (regionIndex != 1 || !buildingPaintMenu.colorTarget.Color2Default.Value) && (regionIndex != 2 || !buildingPaintMenu.colorTarget.Color3Default.Value))
			{
				Color colorForValues = GetColorForValues(hueSlider.GetValue(), saturationSlider.GetValue(), lightnessSlider.GetValue());
				b.Draw(Game1.staminaRect, new Rectangle((int)colorDrawPosition.X - 4, (int)colorDrawPosition.Y - 4, 72, 72), null, Game1.textColor, 0f, Vector2.Zero, SpriteEffects.None, 1f);
				b.Draw(Game1.staminaRect, new Rectangle((int)colorDrawPosition.X, (int)colorDrawPosition.Y, 64, 64), null, colorForValues, 0f, Vector2.Zero, SpriteEffects.None, 1f);
			}
			hueSlider?.Draw(b);
			saturationSlider?.Draw(b);
			lightnessSlider?.Draw(b);
		}

		public Color GetColorForValues(float hue_slider, float saturation_slider)
		{
			Utility.HSLtoRGB(hue_slider, saturation_slider / 100f, 0.5, out var r, out var g, out var b);
			return new Color((byte)r, g, b);
		}

		public Color GetColorForValues(float hue_slider, float saturation_slider, float lightness_slider)
		{
			Utility.HSLtoRGB(hue_slider, saturation_slider / 100f, Utility.Lerp(0.25f, 0.5f, (lightness_slider - (float)lightnessSlider.min) / (float)(lightnessSlider.max - lightnessSlider.min)), out var r, out var g, out var b);
			return new Color((byte)r, g, b);
		}

		public virtual bool ApplyMovementKey(int direction)
		{
			if (direction == 3 || direction == 1)
			{
				if (saturationSlider.handle == buildingPaintMenu.currentlySnappedComponent)
				{
					saturationSlider.ApplyMovementKey(direction);
					return true;
				}
				if (hueSlider.handle == buildingPaintMenu.currentlySnappedComponent)
				{
					hueSlider.ApplyMovementKey(direction);
					return true;
				}
				if (lightnessSlider.handle == buildingPaintMenu.currentlySnappedComponent)
				{
					lightnessSlider.ApplyMovementKey(direction);
					return true;
				}
			}
			return false;
		}

		public virtual void PerformHoverAction(int x, int y)
		{
		}

		public virtual bool ReceiveLeftClick(int x, int y, bool play_sound = true)
		{
			hueSlider?.ReceiveLeftClick(x, y);
			saturationSlider?.ReceiveLeftClick(x, y);
			lightnessSlider?.ReceiveLeftClick(x, y);
			return false;
		}
	}

	public class BuildingColorSlider
	{
		public ClickableTextureComponent handle;

		public BuildingPaintMenu buildingPaintMenu;

		public Rectangle bounds;

		protected float _sliderPosition;

		public int min;

		public int max;

		public Action<int> onValueSet;

		public Func<float, Color> getDrawColor;

		protected int _displayedValue;

		public BuildingColorSlider(BuildingPaintMenu bpm, int handle_id, Rectangle bounds, int min, int max, Action<int> on_value_set = null)
		{
			handle = new ClickableTextureComponent(new Rectangle(0, 0, 4, 5), Game1.mouseCursors, new Rectangle(72, 256, 16, 20), 1f);
			handle.myID = handle_id;
			handle.upNeighborID = -99998;
			handle.upNeighborImmutable = true;
			handle.downNeighborID = -99998;
			handle.downNeighborImmutable = true;
			handle.leftNeighborImmutable = true;
			handle.rightNeighborImmutable = true;
			buildingPaintMenu = bpm;
			this.bounds = bounds;
			this.min = min;
			this.max = max;
			onValueSet = on_value_set;
		}

		public virtual void ApplyMovementKey(int direction)
		{
			int num = Math.Max((max - min) / 50, 1);
			if (direction == 3)
			{
				SetValue(_displayedValue - num);
			}
			else
			{
				SetValue(_displayedValue + num);
			}
			if (buildingPaintMenu.currentlySnappedComponent == handle && Game1.options.SnappyMenus)
			{
				buildingPaintMenu.snapCursorToCurrentSnappedComponent();
			}
		}

		public virtual void ReceiveLeftClick(int x, int y)
		{
			if (bounds.Contains(x, y))
			{
				buildingPaintMenu.activeSlider = this;
				SetValueFromPosition(x, y);
			}
		}

		public virtual void SetValueFromPosition(int x, int y)
		{
			if (bounds.Width != 0 && min != max)
			{
				float num = x - bounds.Left;
				num /= (float)bounds.Width;
				if (num < 0f)
				{
					num = 0f;
				}
				if (num > 1f)
				{
					num = 1f;
				}
				int num2 = max - min;
				num /= (float)num2;
				num *= (float)num2;
				if (_sliderPosition != num)
				{
					_sliderPosition = num;
					SetValue(min + (int)(_sliderPosition * (float)num2));
				}
			}
		}

		public void SetValue(int value, bool skip_value_set = false)
		{
			if (value > max)
			{
				value = max;
			}
			if (value < min)
			{
				value = min;
			}
			_sliderPosition = (float)(value - min) / (float)(max - min);
			handle.bounds.X = (int)Utility.Lerp(bounds.Left, bounds.Right, _sliderPosition) - handle.bounds.Width / 2 * 4;
			handle.bounds.Y = bounds.Top - 4;
			if (_displayedValue != value)
			{
				_displayedValue = value;
				if (!skip_value_set)
				{
					onValueSet?.Invoke(value);
				}
			}
		}

		public int GetValue()
		{
			return _displayedValue;
		}

		public virtual void Draw(SpriteBatch b)
		{
			int num = 20;
			for (int i = 0; i < num; i++)
			{
				Rectangle destinationRectangle = new Rectangle((int)((float)bounds.X + (float)bounds.Width / (float)num * (float)i), bounds.Y, (int)Math.Ceiling((float)bounds.Width / (float)num), bounds.Height);
				Color color = Color.Black;
				if (getDrawColor != null)
				{
					color = getDrawColor(Utility.Lerp(min, max, (float)i / (float)num));
				}
				b.Draw(Game1.staminaRect, destinationRectangle, color);
			}
			handle.draw(b);
		}

		public virtual void Update(int x, int y)
		{
			SetValueFromPosition(x, y);
		}
	}

	public const int region_colorButtons = 1000;

	public const int region_okButton = 101;

	public const int region_nextRegion = 102;

	public const int region_prevRegion = 103;

	public const int region_copyColor = 104;

	public const int region_defaultColor = 105;

	public const int region_hueSlider = 106;

	public const int region_saturationSlider = 107;

	public const int region_lightnessSlider = 108;

	public const int region_appearanceButton = 109;

	public static int WINDOW_WIDTH = 1024;

	public static int WINDOW_HEIGHT = 576;

	public Rectangle previewPane;

	public Rectangle colorPane;

	public BuildingColorSlider activeSlider;

	public ClickableTextureComponent appearanceButton;

	public ClickableTextureComponent okButton;

	public static List<Vector3> savedColors = null;

	public List<Color> buttonColors = new List<Color>();

	public ColorSliderPanel colorSliderPanel;

	private string hoverText = "";

	public Building building;

	public string buildingType = "";

	public BuildingPaintColor colorTarget;

	protected Dictionary<string, string> _paintData;

	public int currentPaintRegion;

	/// <summary>The paint regions for the building.</summary>
	public List<RegionData> regions;

	public ClickableTextureComponent nextRegionButton;

	public ClickableTextureComponent previousRegionButton;

	public ClickableTextureComponent copyColorButton;

	public ClickableTextureComponent defaultColorButton;

	public List<ClickableTextureComponent> savedColorButtons = new List<ClickableTextureComponent>();

	public List<ClickableComponent> sliderHandles = new List<ClickableComponent>();

	public BuildingPaintMenu(Building target_building)
		: base(Game1.uiViewport.Width / 2 - WINDOW_WIDTH / 2, Game1.uiViewport.Height / 2 - WINDOW_HEIGHT / 2, WINDOW_WIDTH, WINDOW_HEIGHT)
	{
		InitializeSavedColors();
		_paintData = DataLoader.PaintData(Game1.content);
		Game1.player.Halt();
		building = target_building;
		colorTarget = target_building.netBuildingPaintColor.Value;
		buildingType = building.buildingType.Value;
		SetRegion(0);
		populateClickableComponentList();
		if (Game1.options.SnappyMenus)
		{
			snapToDefaultClickableComponent();
		}
	}

	public virtual void InitializeSavedColors()
	{
		if (savedColors == null)
		{
			savedColors = new List<Vector3>();
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(101);
		snapCursorToCurrentSnappedComponent();
	}

	public override void applyMovementKey(int direction)
	{
		if (!colorSliderPanel.ApplyMovementKey(direction))
		{
			base.applyMovementKey(direction);
		}
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		switch (button)
		{
		case Buttons.RightTrigger:
			Game1.playSound("shwip");
			SetRegion((currentPaintRegion + 1 + regions.Count) % regions.Count);
			break;
		case Buttons.LeftTrigger:
			Game1.playSound("shwip");
			SetRegion((currentPaintRegion - 1 + regions.Count) % regions.Count);
			break;
		}
		base.receiveGamePadButton(button);
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		activeSlider?.Update(Game1.getMouseX(), Game1.getMouseY());
		base.update(time);
	}

	/// <inheritdoc />
	public override void releaseLeftClick(int x, int y)
	{
		activeSlider = null;
		base.releaseLeftClick(x, y);
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		for (int i = 0; i < savedColorButtons.Count; i++)
		{
			if (savedColorButtons[i].containsPoint(x, y))
			{
				savedColors.RemoveAt(i);
				RepositionElements();
				Game1.playSound("coin");
				return;
			}
		}
		base.receiveRightClick(x, y, playSound);
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (colorSliderPanel.ReceiveLeftClick(x, y, playSound))
		{
			return;
		}
		if (defaultColorButton.containsPoint(x, y))
		{
			switch (currentPaintRegion)
			{
			case 0:
				colorTarget.Color1Default.Value = true;
				break;
			case 1:
				colorTarget.Color2Default.Value = true;
				break;
			default:
				colorTarget.Color3Default.Value = true;
				break;
			}
			Game1.playSound("coin");
			RepositionElements();
			return;
		}
		for (int i = 0; i < savedColorButtons.Count; i++)
		{
			if (savedColorButtons[i].containsPoint(x, y))
			{
				colorSliderPanel.hueSlider.SetValue((int)savedColors[i].X);
				colorSliderPanel.saturationSlider.SetValue((int)savedColors[i].Y);
				colorSliderPanel.lightnessSlider.SetValue((int)Utility.Lerp(colorSliderPanel.lightnessSlider.min, colorSliderPanel.lightnessSlider.max, savedColors[i].Z));
				Game1.playSound("coin");
				return;
			}
		}
		if (copyColorButton.containsPoint(x, y))
		{
			if (SaveColor())
			{
				Game1.playSound("coin");
				RepositionElements();
			}
			else
			{
				Game1.playSound("cancel");
			}
		}
		else if (okButton.containsPoint(x, y))
		{
			exitThisMenu(playSound);
		}
		else if (appearanceButton.containsPoint(x, y))
		{
			Game1.playSound("smallSelect");
			BuildingSkinMenu buildingSkinMenu = new BuildingSkinMenu(building);
			buildingSkinMenu.behaviorBeforeCleanup = (Action<IClickableMenu>)Delegate.Combine(buildingSkinMenu.behaviorBeforeCleanup, (Action<IClickableMenu>)delegate
			{
				if (building.CanBePainted())
				{
					BuildingPaintMenu buildingPaintMenu = new BuildingPaintMenu(building);
					IClickableMenu clickableMenu = Game1.activeClickableMenu;
					IClickableMenu clickableMenu2 = null;
					while (clickableMenu.GetChildMenu() != null)
					{
						clickableMenu2 = clickableMenu;
						clickableMenu = clickableMenu.GetChildMenu();
						if (clickableMenu is BuildingPaintMenu)
						{
							break;
						}
					}
					if (clickableMenu2 == null)
					{
						Game1.activeClickableMenu = buildingPaintMenu;
					}
					else
					{
						clickableMenu2.SetChildMenu(buildingPaintMenu);
					}
					if (Game1.options.SnappyMenus)
					{
						buildingPaintMenu.setCurrentlySnappedComponentTo(109);
						buildingPaintMenu.snapCursorToCurrentSnappedComponent();
					}
				}
				else
				{
					exitThisMenuNoSound();
				}
			});
			SetChildMenu(buildingSkinMenu);
		}
		else if (previousRegionButton.containsPoint(x, y))
		{
			Game1.playSound("shwip");
			SetRegion((currentPaintRegion - 1 + regions.Count) % regions.Count);
		}
		else if (nextRegionButton.containsPoint(x, y))
		{
			Game1.playSound("shwip");
			SetRegion((currentPaintRegion + 1) % regions.Count);
		}
		else
		{
			base.receiveLeftClick(x, y, playSound);
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoverText = "";
		okButton.tryHover(x, y);
		previousRegionButton.tryHover(x, y);
		nextRegionButton.tryHover(x, y);
		copyColorButton.tryHover(x, y);
		defaultColorButton.tryHover(x, y);
		appearanceButton.tryHover(x, y);
		if (appearanceButton.containsPoint(x, y))
		{
			hoverText = appearanceButton.name;
		}
		foreach (ClickableTextureComponent savedColorButton in savedColorButtons)
		{
			savedColorButton.tryHover(x, y);
		}
		colorSliderPanel.PerformHoverAction(x, y);
	}

	public virtual void RepositionElements()
	{
		previewPane.X = xPositionOnScreen;
		previewPane.Y = yPositionOnScreen;
		previewPane.Width = 512;
		previewPane.Height = 576;
		colorPane.Width = 448;
		colorPane.X = xPositionOnScreen + width - colorPane.Width;
		colorPane.Y = yPositionOnScreen;
		colorPane.Height = 576;
		Rectangle start_rect = colorPane;
		start_rect.Inflate(-32, -32);
		previousRegionButton = new ClickableTextureComponent(new Rectangle(start_rect.Left, start_rect.Top, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 44), 1f)
		{
			myID = 103,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			downNeighborID = 105,
			upNeighborID = -99998,
			fullyImmutable = true
		};
		nextRegionButton = new ClickableTextureComponent(new Rectangle(start_rect.Right - 64, start_rect.Top, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 33), 1f)
		{
			myID = 102,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			downNeighborID = 105,
			upNeighborID = -99998,
			fullyImmutable = true
		};
		start_rect.Y += 64;
		start_rect.Height = 0;
		int left = start_rect.Left;
		defaultColorButton = new ClickableTextureComponent(new Rectangle(left, start_rect.Bottom, 64, 64), Game1.mouseCursors2, new Rectangle(80, 144, 16, 16), 4f)
		{
			region = 1000,
			myID = 105,
			upNeighborID = -99998,
			downNeighborID = -99998,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			fullyImmutable = true
		};
		left += 80;
		savedColorButtons.Clear();
		buttonColors.Clear();
		for (int i = 0; i < savedColors.Count; i++)
		{
			if (left + 64 > start_rect.X + start_rect.Width)
			{
				left = start_rect.X;
				start_rect.Y += 72;
			}
			ClickableTextureComponent item = new ClickableTextureComponent(new Rectangle(left, start_rect.Bottom, 64, 64), Game1.mouseCursors2, new Rectangle(96, 144, 16, 16), 4f)
			{
				region = 1000,
				myID = i,
				upNeighborID = -99998,
				downNeighborID = -99998,
				leftNeighborID = -99998,
				rightNeighborID = -99998,
				fullyImmutable = true
			};
			left += 80;
			savedColorButtons.Add(item);
			Vector3 vector = savedColors[i];
			Utility.HSLtoRGB(vector.X, vector.Y / 100f, Utility.Lerp(0.25f, 0.5f, vector.Z), out var r, out var g, out var b);
			buttonColors.Add(new Color((byte)r, (byte)g, (byte)b));
		}
		if (left + 64 > start_rect.X + start_rect.Width)
		{
			left = start_rect.X;
			start_rect.Y += 72;
		}
		copyColorButton = new ClickableTextureComponent(new Rectangle(left, start_rect.Bottom, 64, 64), Game1.mouseCursors, new Rectangle(274, 284, 16, 16), 4f)
		{
			region = 1000,
			myID = 104,
			upNeighborID = -99998,
			downNeighborID = -99998,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			fullyImmutable = true
		};
		start_rect.Y += 80;
		start_rect = colorSliderPanel.Reposition(start_rect);
		start_rect.Y += 64;
		okButton = new ClickableTextureComponent(new Rectangle(colorPane.Right - 64 - 16, colorPane.Bottom - 64 - 16, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
		{
			myID = 101,
			upNeighborID = 108,
			leftNeighborID = 109
		};
		appearanceButton = new ClickableTextureComponent(Game1.content.LoadString("Strings\\UI:Carpenter_ChangeAppearance"), new Rectangle(previewPane.Right - 64 - 16, colorPane.Bottom - 64 - 16, 64, 64), null, null, Game1.mouseCursors2, new Rectangle(96, 208, 16, 16), 4f)
		{
			myID = 109,
			upNeighborID = 108,
			rightNeighborID = 101,
			visible = building.CanBeReskinned()
		};
		populateClickableComponentList();
	}

	public override bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
	{
		if (a.region == 1000 && b.region != 1000)
		{
			switch (direction)
			{
			case 1:
			case 3:
				return false;
			case 2:
				if (b.myID != 106)
				{
					return false;
				}
				break;
			}
		}
		return base.IsAutomaticSnapValid(direction, a, b);
	}

	public virtual bool SaveColor()
	{
		if ((currentPaintRegion == 0 && colorTarget.Color1Default.Value) || (currentPaintRegion == 1 && colorTarget.Color2Default.Value) || (currentPaintRegion == 2 && colorTarget.Color3Default.Value))
		{
			return false;
		}
		Vector3 item = new Vector3(colorSliderPanel.hueSlider.GetValue(), colorSliderPanel.saturationSlider.GetValue(), (float)(colorSliderPanel.lightnessSlider.GetValue() - colorSliderPanel.lightnessSlider.min) / (float)(colorSliderPanel.lightnessSlider.max - colorSliderPanel.lightnessSlider.min));
		if (savedColors.Count >= 8)
		{
			savedColors.RemoveAt(0);
		}
		savedColors.Add(item);
		return true;
	}

	public virtual void SetRegion(int new_region)
	{
		if (regions == null)
		{
			LoadRegionData();
		}
		if (new_region < regions.Count && new_region >= 0)
		{
			currentPaintRegion = new_region;
			RegionData regionData = regions[new_region];
			colorSliderPanel = new ColorSliderPanel(this, new_region, regionData.Id, regionData.MinBrightness, regionData.MaxBrightness);
		}
		RepositionElements();
	}

	public virtual void LoadRegionData()
	{
		if (regions != null)
		{
			return;
		}
		regions = new List<RegionData>();
		string paintDataKey = building.GetPaintDataKey(_paintData);
		string value;
		string text = ((paintDataKey != null && _paintData.TryGetValue(paintDataKey, out value)) ? value.Replace("\n", "").Replace("\t", "") : null);
		if (text == null)
		{
			return;
		}
		string[] array = text.Split('/');
		for (int i = 0; i < array.Length / 2; i++)
		{
			if (array[i].Trim() == "")
			{
				continue;
			}
			string text2 = array[i * 2];
			string[] array2 = ArgUtility.SplitBySpace(array[i * 2 + 1]);
			int minBrightness = -100;
			int maxBrightness = 100;
			if (array2.Length >= 2)
			{
				try
				{
					minBrightness = int.Parse(array2[0]);
					maxBrightness = int.Parse(array2[1]);
				}
				catch (Exception)
				{
				}
			}
			string displayName = Game1.content.LoadStringReturnNullIfNotFound("Strings/Buildings:Paint_Region_" + text2) ?? text2;
			regions.Add(new RegionData(text2, displayName, minBrightness, maxBrightness));
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (!Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
		}
		Game1.DrawBox(previewPane.X, previewPane.Y, previewPane.Width, previewPane.Height);
		Rectangle scissorRectangle = previewPane;
		scissorRectangle.Inflate(0, 0);
		b.End();
		b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, Utility.ScissorEnabled);
		b.GraphicsDevice.ScissorRectangle = scissorRectangle;
		Vector2 vector = new Vector2(previewPane.X + previewPane.Width / 2, previewPane.Y + previewPane.Height / 2 - 16);
		Rectangle rectangle = building.getSourceRectForMenu() ?? building.getSourceRect();
		building.drawInMenu(b, (int)vector.X - (int)((float)building.tilesWide.Value / 2f * 64f), (int)vector.Y - rectangle.Height * 4 / 2);
		b.End();
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		Game1.DrawBox(colorPane.X, colorPane.Y, colorPane.Width, colorPane.Height);
		RegionData regionData = regions[currentPaintRegion];
		int heightOfString = SpriteText.getHeightOfString(regionData.DisplayName);
		SpriteText.drawStringHorizontallyCenteredAt(b, regionData.DisplayName, colorPane.X + colorPane.Width / 2, nextRegionButton.bounds.Center.Y - heightOfString / 2);
		okButton.draw(b);
		appearanceButton.draw(b);
		colorSliderPanel.Draw(b);
		nextRegionButton.draw(b);
		previousRegionButton.draw(b);
		copyColorButton.draw(b);
		defaultColorButton.draw(b);
		for (int i = 0; i < savedColorButtons.Count; i++)
		{
			savedColorButtons[i].draw(b, buttonColors[i], 1f);
		}
		if (GetChildMenu() == null)
		{
			drawMouse(b);
			string text = hoverText;
			if (text != null && text.Length > 0)
			{
				IClickableMenu.drawHoverText(b, hoverText, Game1.dialogueFont);
			}
		}
	}
}
