using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;

namespace StardewValley.Menus;

public class CollectionsPage : IClickableMenu
{
	public const int region_sideTabShipped = 7001;

	public const int region_sideTabFish = 7002;

	public const int region_sideTabArtifacts = 7003;

	public const int region_sideTabMinerals = 7004;

	public const int region_sideTabCooking = 7005;

	public const int region_sideTabAchivements = 7006;

	public const int region_sideTabSecretNotes = 7007;

	public const int region_sideTabLetters = 7008;

	public const int region_forwardButton = 707;

	public const int region_backButton = 706;

	public static int widthToMoveActiveTab = 8;

	public const int organicsTab = 0;

	public const int fishTab = 1;

	public const int archaeologyTab = 2;

	public const int mineralsTab = 3;

	public const int cookingTab = 4;

	public const int achievementsTab = 5;

	public const int secretNotesTab = 6;

	public const int lettersTab = 7;

	public const int distanceFromMenuBottomBeforeNewPage = 128;

	private string hoverText = "";

	public ClickableTextureComponent backButton;

	public ClickableTextureComponent forwardButton;

	public Dictionary<int, ClickableTextureComponent> sideTabs = new Dictionary<int, ClickableTextureComponent>();

	public int currentTab;

	public int currentPage;

	public int secretNoteImage = -1;

	public Dictionary<int, List<List<ClickableTextureComponent>>> collections = new Dictionary<int, List<List<ClickableTextureComponent>>>();

	public Dictionary<int, string> secretNotesData;

	public Texture2D secretNoteImageTexture;

	public LetterViewerMenu letterviewerSubMenu;

	private Item hoverItem;

	private CraftingRecipe hoverCraftingRecipe;

	private int value;

	public CollectionsPage(int x, int y, int width, int height)
		: base(x, y, width, height)
	{
		sideTabs.Add(0, new ClickableTextureComponent(0.ToString() ?? "", new Rectangle(xPositionOnScreen - 48 + widthToMoveActiveTab, yPositionOnScreen + 64 * (2 + sideTabs.Count), 64, 64), "", Game1.content.LoadString("Strings\\UI:Collections_Shipped"), Game1.mouseCursors, new Rectangle(640, 80, 16, 16), 4f)
		{
			myID = 7001,
			downNeighborID = -99998,
			rightNeighborID = 0
		});
		collections.Add(0, new List<List<ClickableTextureComponent>>());
		sideTabs.Add(1, new ClickableTextureComponent(1.ToString() ?? "", new Rectangle(xPositionOnScreen - 48, yPositionOnScreen + 64 * (2 + sideTabs.Count), 64, 64), "", Game1.content.LoadString("Strings\\UI:Collections_Fish"), Game1.mouseCursors, new Rectangle(640, 64, 16, 16), 4f)
		{
			myID = 7002,
			upNeighborID = -99998,
			downNeighborID = -99998,
			rightNeighborID = 0
		});
		collections.Add(1, new List<List<ClickableTextureComponent>>());
		sideTabs.Add(2, new ClickableTextureComponent(2.ToString() ?? "", new Rectangle(xPositionOnScreen - 48, yPositionOnScreen + 64 * (2 + sideTabs.Count), 64, 64), "", Game1.content.LoadString("Strings\\UI:Collections_Artifacts"), Game1.mouseCursors, new Rectangle(656, 64, 16, 16), 4f)
		{
			myID = 7003,
			upNeighborID = -99998,
			downNeighborID = -99998,
			rightNeighborID = 0
		});
		collections.Add(2, new List<List<ClickableTextureComponent>>());
		sideTabs.Add(3, new ClickableTextureComponent(3.ToString() ?? "", new Rectangle(xPositionOnScreen - 48, yPositionOnScreen + 64 * (2 + sideTabs.Count), 64, 64), "", Game1.content.LoadString("Strings\\UI:Collections_Minerals"), Game1.mouseCursors, new Rectangle(672, 64, 16, 16), 4f)
		{
			myID = 7004,
			upNeighborID = -99998,
			downNeighborID = -99998,
			rightNeighborID = 0
		});
		collections.Add(3, new List<List<ClickableTextureComponent>>());
		sideTabs.Add(4, new ClickableTextureComponent(4.ToString() ?? "", new Rectangle(xPositionOnScreen - 48, yPositionOnScreen + 64 * (2 + sideTabs.Count), 64, 64), "", Game1.content.LoadString("Strings\\UI:Collections_Cooking"), Game1.mouseCursors, new Rectangle(688, 64, 16, 16), 4f)
		{
			myID = 7005,
			upNeighborID = -99998,
			downNeighborID = -99998,
			rightNeighborID = 0
		});
		collections.Add(4, new List<List<ClickableTextureComponent>>());
		sideTabs.Add(5, new ClickableTextureComponent(5.ToString() ?? "", new Rectangle(xPositionOnScreen - 48, yPositionOnScreen + 64 * (2 + sideTabs.Count), 64, 64), "", Game1.content.LoadString("Strings\\UI:Collections_Achievements"), Game1.mouseCursors, new Rectangle(656, 80, 16, 16), 4f)
		{
			myID = 7006,
			upNeighborID = 7005,
			downNeighborID = -99998,
			rightNeighborID = 0
		});
		collections.Add(5, new List<List<ClickableTextureComponent>>());
		sideTabs.Add(7, new ClickableTextureComponent(7.ToString() ?? "", new Rectangle(xPositionOnScreen - 48, yPositionOnScreen + 64 * (2 + sideTabs.Count), 64, 64), "", Game1.content.LoadString("Strings\\UI:Collections_Letters"), Game1.mouseCursors, new Rectangle(688, 80, 16, 16), 4f)
		{
			myID = 7008,
			upNeighborID = -99998,
			downNeighborID = -99998,
			rightNeighborID = 0
		});
		collections.Add(7, new List<List<ClickableTextureComponent>>());
		if (Game1.player.secretNotesSeen.Count > 0)
		{
			sideTabs.Add(6, new ClickableTextureComponent(6.ToString() ?? "", new Rectangle(xPositionOnScreen - 48, yPositionOnScreen + 64 * (2 + sideTabs.Count), 64, 64), "", Game1.content.LoadString("Strings\\UI:Collections_SecretNotes"), Game1.mouseCursors, new Rectangle(672, 80, 16, 16), 4f)
			{
				myID = 7007,
				upNeighborID = -99998,
				rightNeighborID = 0
			});
			collections.Add(6, new List<List<ClickableTextureComponent>>());
		}
		sideTabs[0].upNeighborID = -1;
		sideTabs[0].upNeighborImmutable = true;
		int key = 0;
		int num = 0;
		foreach (int key2 in sideTabs.Keys)
		{
			if (sideTabs[key2].bounds.Y > num)
			{
				num = sideTabs[key2].bounds.Y;
				key = key2;
			}
		}
		sideTabs[key].downNeighborID = -1;
		sideTabs[key].downNeighborImmutable = true;
		widthToMoveActiveTab = 8;
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
		int[] array = new int[8];
		int num2 = xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearSideBorder;
		int num3 = yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - 16;
		int num4 = 10;
		List<ParsedItemData> list = new List<ParsedItemData>(from entry in ItemRegistry.GetObjectTypeDefinition().GetAllData()
			orderby entry.TextureName, entry.SpriteIndex
			select entry);
		List<ParsedItemData> list2 = new List<ParsedItemData>();
		for (int num5 = list.Count - 1; num5 >= 0; num5--)
		{
			string internalName = list[num5].InternalName;
			if (internalName.Equals("Wine") || internalName.Equals("Pickles") || internalName.Equals("Jelly") || internalName.Equals("Juice"))
			{
				list2.Add(list[num5]);
				list.RemoveAt(num5);
			}
			if (list2.Count == 4)
			{
				break;
			}
		}
		list2.Sort((ParsedItemData a, ParsedItemData b) => a.InternalName.CompareTo(b.InternalName));
		list.Insert(278, list2[2]);
		list.Insert(279, list2[0]);
		list.Insert(283, list2[3]);
		list.Insert(284, list2[1]);
		foreach (ParsedItemData item in list)
		{
			string itemId = item.ItemId;
			string objectType = item.ObjectType;
			bool drawShadow = false;
			bool flag = false;
			int num6;
			switch (objectType)
			{
			case "Arch":
				num6 = 2;
				drawShadow = LibraryMuseum.HasDonatedArtifact(itemId);
				break;
			case "Fish":
				if (item.RawData is ObjectData { ExcludeFromFishingCollection: not false })
				{
					continue;
				}
				num6 = 1;
				drawShadow = Game1.player.fishCaught.ContainsKey(item.QualifiedItemId);
				break;
			default:
				if (item.Category != -2)
				{
					if (objectType == "Cooking" || item.Category == -7)
					{
						num6 = 4;
						string text = item.InternalName;
						switch (text)
						{
						case "Cheese Cauli.":
							text = "Cheese Cauliflower";
							break;
						case "Cheese Cauliflower":
							text = "Cheese Cauli.";
							break;
						case "Vegetable Medley":
							text = "Vegetable Stew";
							break;
						case "Cookie":
							text = "Cookies";
							break;
						case "Eggplant Parmesan":
							text = "Eggplant Parm.";
							break;
						case "Cranberry Sauce":
							text = "Cran. Sauce";
							break;
						case "Dish O' The Sea":
							text = "Dish o' The Sea";
							break;
						}
						if (Game1.player.recipesCooked.ContainsKey(itemId))
						{
							drawShadow = true;
						}
						else if (Game1.player.cookingRecipes.ContainsKey(text))
						{
							flag = true;
						}
						switch (itemId)
						{
						case "217":
						case "772":
						case "773":
						case "279":
						case "873":
							continue;
						}
					}
					else
					{
						if (!Object.isPotentialBasicShipped(itemId, item.Category, item.ObjectType))
						{
							continue;
						}
						num6 = 0;
						drawShadow = Game1.player.basicShipped.ContainsKey(itemId);
					}
					break;
				}
				goto case "Minerals";
			case "Minerals":
				num6 = 3;
				drawShadow = LibraryMuseum.HasDonatedArtifact(itemId);
				break;
			}
			int x2 = num2 + array[num6] % num4 * 68;
			int num7 = num3 + array[num6] / num4 * 68;
			if (num7 > yPositionOnScreen + height - 128)
			{
				collections[num6].Add(new List<ClickableTextureComponent>());
				array[num6] = 0;
				x2 = num2;
				num7 = num3;
			}
			if (collections[num6].Count == 0)
			{
				collections[num6].Add(new List<ClickableTextureComponent>());
			}
			List<ClickableTextureComponent> list3 = collections[num6].Last();
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(itemId);
			list3.Add(new ClickableTextureComponent(itemId + " " + drawShadow + " " + flag, new Rectangle(x2, num7, 64, 64), null, "", dataOrErrorItem.GetTexture(), dataOrErrorItem.GetSourceRect(), 4f, drawShadow)
			{
				myID = list3.Count,
				rightNeighborID = (((list3.Count + 1) % num4 == 0) ? (-1) : (list3.Count + 1)),
				leftNeighborID = ((list3.Count % num4 == 0) ? 7001 : (list3.Count - 1)),
				downNeighborID = ((num7 + 68 > yPositionOnScreen + height - 128) ? (-7777) : (list3.Count + num4)),
				upNeighborID = ((list3.Count < num4) ? 12347 : (list3.Count - num4)),
				fullyImmutable = true
			});
			array[num6]++;
		}
		if (collections[5].Count == 0)
		{
			collections[5].Add(new List<ClickableTextureComponent>());
		}
		foreach (KeyValuePair<int, string> achievement in Game1.achievements)
		{
			bool flag2 = Game1.player.achievements.Contains(achievement.Key);
			string[] array2 = achievement.Value.Split('^');
			if (flag2 || (array2[2].Equals("true") && (array2[3].Equals("-1") || farmerHasAchievements(array2[3]))))
			{
				int x3 = num2 + array[5] % num4 * 68;
				int y2 = num3 + array[5] / num4 * 68;
				collections[5][0].Add(new ClickableTextureComponent(achievement.Key + " " + flag2, new Rectangle(x3, y2, 64, 64), null, "", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 25), 1f));
				array[5]++;
			}
			else
			{
				int x4 = num2 + array[5] % num4 * 68;
				int y3 = num3 + array[5] / num4 * 68;
				collections[5][0].Add(new ClickableTextureComponent("??? false", new Rectangle(x4, y3, 64, 64), null, "???", Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 25), 1f));
				array[5]++;
			}
		}
		if (Game1.player.secretNotesSeen.Count > 0)
		{
			if (collections[6].Count == 0)
			{
				collections[6].Add(new List<ClickableTextureComponent>());
			}
			secretNotesData = DataLoader.SecretNotes(Game1.content);
			secretNoteImageTexture = Game1.temporaryContent.Load<Texture2D>("TileSheets\\SecretNotesImages");
			bool flag3 = Game1.player.secretNotesSeen.Contains(GameLocation.JOURNAL_INDEX + 1);
			foreach (int key3 in secretNotesData.Keys)
			{
				if (key3 >= GameLocation.JOURNAL_INDEX)
				{
					if (!flag3)
					{
						continue;
					}
				}
				else if (!Game1.player.hasMagnifyingGlass)
				{
					continue;
				}
				int x5 = num2 + array[6] % num4 * 68;
				int y4 = num3 + array[6] / num4 * 68;
				if (key3 >= GameLocation.JOURNAL_INDEX)
				{
					collections[6][0].Add(new ClickableTextureComponent(key3 + " " + Game1.player.secretNotesSeen.Contains(key3), new Rectangle(x5, y4, 64, 64), null, "", Game1.objectSpriteSheet, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 842, 16, 16), 4f, Game1.player.secretNotesSeen.Contains(key3)));
				}
				else
				{
					collections[6][0].Add(new ClickableTextureComponent(key3 + " " + Game1.player.secretNotesSeen.Contains(key3), new Rectangle(x5, y4, 64, 64), null, "", Game1.objectSpriteSheet, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 79, 16, 16), 4f, Game1.player.secretNotesSeen.Contains(key3)));
				}
				array[6]++;
			}
		}
		if (collections[7].Count == 0)
		{
			collections[7].Add(new List<ClickableTextureComponent>());
		}
		List<ClickableTextureComponent> list4 = collections[7].Last();
		Dictionary<string, string> dictionary = DataLoader.Mail(Game1.content);
		foreach (string item2 in Game1.player.mailReceived)
		{
			if (dictionary.TryGetValue(item2, out var text2))
			{
				int x6 = num2 + array[7] % num4 * 68;
				int num8 = num3 + array[7] / num4 * 68;
				string[] array3 = text2.Split("[#]");
				if (num8 > yPositionOnScreen + height - 128)
				{
					collections[7].Add(new List<ClickableTextureComponent>());
					array[7] = 0;
					x6 = num2;
					num8 = num3;
					list4 = collections[7].Last();
				}
				list4.Add(new ClickableTextureComponent(item2 + " true " + ((array3.Length > 1) ? array3[1] : "???"), new Rectangle(x6, num8, 64, 64), null, "", Game1.mouseCursors, new Rectangle(190, 423, 14, 11), 4f, drawShadow: true)
				{
					myID = list4.Count,
					rightNeighborID = (((list4.Count + 1) % num4 == 0) ? (-1) : (list4.Count + 1)),
					leftNeighborID = ((list4.Count % num4 == 0) ? 7008 : (list4.Count - 1)),
					downNeighborID = ((num8 + 68 > yPositionOnScreen + height - 128) ? (-7777) : (list4.Count + num4)),
					upNeighborID = ((list4.Count < num4) ? 12347 : (list4.Count - num4)),
					fullyImmutable = true
				});
				array[7]++;
			}
		}
	}

	protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
	{
		base.customSnapBehavior(direction, oldRegion, oldID);
		switch (direction)
		{
		case 2:
			if (currentPage > 0)
			{
				currentlySnappedComponent = getComponentWithID(706);
			}
			else if (currentPage == 0 && collections[currentTab].Count > 1)
			{
				currentlySnappedComponent = getComponentWithID(707);
			}
			backButton.upNeighborID = oldID;
			forwardButton.upNeighborID = oldID;
			break;
		case 3:
			if (oldID == 707 && currentPage > 0)
			{
				currentlySnappedComponent = getComponentWithID(706);
			}
			break;
		case 1:
			if (oldID == 706 && collections[currentTab].Count > currentPage + 1)
			{
				currentlySnappedComponent = getComponentWithID(707);
			}
			break;
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		base.snapToDefaultClickableComponent();
		currentlySnappedComponent = getComponentWithID(0);
		snapCursorToCurrentSnappedComponent();
	}

	/// <summary>Restore the page state when it's recreated for a window resize.</summary>
	/// <param name="oldPage">The previous page instance before it was recreated.</param>
	public void postWindowSizeChange(IClickableMenu oldPage)
	{
		if (oldPage is CollectionsPage collectionsPage)
		{
			sideTabs[currentTab].bounds.X -= widthToMoveActiveTab;
			currentTab = collectionsPage.currentTab;
			currentPage = collectionsPage.currentPage;
			sideTabs[currentTab].bounds.X += widthToMoveActiveTab;
		}
	}

	private bool farmerHasAchievements(string listOfAchievementNumbers)
	{
		string[] array = ArgUtility.SplitBySpace(listOfAchievementNumbers);
		foreach (string text in array)
		{
			if (!Game1.player.achievements.Contains(Convert.ToInt32(text)))
			{
				return false;
			}
		}
		return true;
	}

	public override bool readyToClose()
	{
		if (letterviewerSubMenu != null)
		{
			return false;
		}
		return base.readyToClose();
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		if (letterviewerSubMenu == null)
		{
			return;
		}
		letterviewerSubMenu.update(time);
		if (letterviewerSubMenu.destroy)
		{
			letterviewerSubMenu = null;
			if (Game1.options.SnappyMenus)
			{
				snapCursorToCurrentSnappedComponent();
			}
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		base.receiveKeyPress(key);
		letterviewerSubMenu?.receiveKeyPress(key);
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (letterviewerSubMenu != null)
		{
			letterviewerSubMenu.receiveLeftClick(x, y);
			return;
		}
		foreach (KeyValuePair<int, ClickableTextureComponent> sideTab in sideTabs)
		{
			if (sideTab.Value.containsPoint(x, y) && currentTab != sideTab.Key)
			{
				Game1.playSound("smallSelect");
				sideTabs[currentTab].bounds.X -= widthToMoveActiveTab;
				currentTab = Convert.ToInt32(sideTab.Value.name);
				currentPage = 0;
				sideTab.Value.bounds.X += widthToMoveActiveTab;
			}
		}
		if (currentPage > 0 && backButton.containsPoint(x, y))
		{
			currentPage--;
			Game1.playSound("shwip");
			backButton.scale = backButton.baseScale;
			if (Game1.options.snappyMenus && Game1.options.gamepadControls && currentPage == 0)
			{
				currentlySnappedComponent = forwardButton;
				Game1.setMousePosition(currentlySnappedComponent.bounds.Center);
			}
		}
		if (currentPage < collections[currentTab].Count - 1 && forwardButton.containsPoint(x, y))
		{
			currentPage++;
			Game1.playSound("shwip");
			forwardButton.scale = forwardButton.baseScale;
			if (Game1.options.snappyMenus && Game1.options.gamepadControls && currentPage == collections[currentTab].Count - 1)
			{
				currentlySnappedComponent = backButton;
				Game1.setMousePosition(currentlySnappedComponent.bounds.Center);
			}
		}
		switch (currentTab)
		{
		case 7:
		{
			Dictionary<string, string> dictionary = DataLoader.Mail(Game1.content);
			{
				foreach (ClickableTextureComponent item in collections[currentTab][currentPage])
				{
					if (item.containsPoint(x, y))
					{
						string text = ArgUtility.SplitBySpaceAndGet(item.name, 0);
						letterviewerSubMenu = new LetterViewerMenu(dictionary[text], text, fromCollection: true);
					}
				}
				break;
			}
		}
		case 6:
		{
			foreach (ClickableTextureComponent item2 in collections[currentTab][currentPage])
			{
				if (item2.containsPoint(x, y))
				{
					string[] array = ArgUtility.SplitBySpace(item2.name);
					if (array[1] == "True" && int.TryParse(array[0], out var result))
					{
						letterviewerSubMenu = new LetterViewerMenu(result);
						letterviewerSubMenu.isFromCollection = true;
						break;
					}
				}
			}
			break;
		}
		}
	}

	public override bool shouldDrawCloseButton()
	{
		return letterviewerSubMenu == null;
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		letterviewerSubMenu?.receiveRightClick(x, y);
	}

	public override void applyMovementKey(int direction)
	{
		if (letterviewerSubMenu != null)
		{
			letterviewerSubMenu.applyMovementKey(direction);
		}
		else
		{
			base.applyMovementKey(direction);
		}
	}

	/// <inheritdoc />
	public override void gamePadButtonHeld(Buttons b)
	{
		if (letterviewerSubMenu != null)
		{
			letterviewerSubMenu.gamePadButtonHeld(b);
		}
		else
		{
			base.gamePadButtonHeld(b);
		}
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		if (letterviewerSubMenu != null)
		{
			letterviewerSubMenu.receiveGamePadButton(button);
		}
		else
		{
			base.receiveGamePadButton(button);
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoverText = "";
		value = -1;
		secretNoteImage = -1;
		if (letterviewerSubMenu != null)
		{
			letterviewerSubMenu.performHoverAction(x, y);
			return;
		}
		foreach (ClickableTextureComponent value in sideTabs.Values)
		{
			if (value.containsPoint(x, y))
			{
				hoverText = value.hoverText;
				return;
			}
		}
		bool flag = false;
		foreach (ClickableTextureComponent item in collections[currentTab][currentPage])
		{
			if (item.containsPoint(x, y, 2))
			{
				item.scale = Math.Min(item.scale + 0.02f, item.baseScale + 0.1f);
				string[] array = ArgUtility.SplitBySpace(item.name);
				if (currentTab == 5 || (array.Length > 1 && Convert.ToBoolean(array[1])) || (array.Length > 2 && Convert.ToBoolean(array[2])))
				{
					if (currentTab == 7)
					{
						hoverText = Game1.parseText(item.name.Substring(item.name.IndexOf(' ', item.name.IndexOf(' ') + 1) + 1), Game1.smallFont, 256);
					}
					else
					{
						hoverText = createDescription(array[0]);
					}
				}
				else
				{
					if (hoverText != "???")
					{
						hoverItem = null;
					}
					hoverText = "???";
				}
				flag = true;
			}
			else
			{
				item.scale = Math.Max(item.scale - 0.02f, item.baseScale);
			}
		}
		if (!flag)
		{
			hoverItem = null;
		}
		forwardButton.tryHover(x, y, 0.5f);
		backButton.tryHover(x, y, 0.5f);
	}

	public string createDescription(string id)
	{
		string text = "";
		switch (currentTab)
		{
		case 5:
		{
			if (id == "???")
			{
				return "???";
			}
			int key = int.Parse(id);
			string[] array3 = Game1.achievements[key].Split('^');
			text = text + array3[0] + Environment.NewLine + Environment.NewLine;
			text += array3[1];
			break;
		}
		case 6:
		{
			if (secretNotesData == null)
			{
				break;
			}
			int num4 = int.Parse(id);
			text = ((num4 >= GameLocation.JOURNAL_INDEX) ? (text + Game1.content.LoadString("Strings\\Locations:Journal_Name") + " #" + (num4 - GameLocation.JOURNAL_INDEX)) : (text + Game1.content.LoadString("Strings\\Locations:Secret_Note_Name") + " #" + num4));
			if (secretNotesData[num4][0] == '!')
			{
				secretNoteImage = Convert.ToInt32(ArgUtility.SplitBySpaceAndGet(secretNotesData[num4], 1));
				break;
			}
			string text5 = Game1.parseText(Utility.ParseGiftReveals(secretNotesData[num4]).TrimStart(' ', '^').Replace("^", Environment.NewLine)
				.Replace("@", Game1.player.name.Value), Game1.smallFont, 512);
			string[] array4 = text5.Split(Environment.NewLine);
			int num5 = 15;
			if (array4.Length > num5)
			{
				string[] array5 = new string[num5];
				for (int i = 0; i < num5; i++)
				{
					array5[i] = array4[i];
				}
				text5 = string.Join(Environment.NewLine, array5).Trim() + Environment.NewLine + "(...)";
			}
			text = text + Environment.NewLine + Environment.NewLine + text5;
			break;
		}
		default:
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(id);
			string text2 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:" + dataOrErrorItem.ItemId + "_CollectionsTabName") ?? dataOrErrorItem.DisplayName;
			string text3 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:" + dataOrErrorItem.ItemId + "_CollectionsTabDescription") ?? dataOrErrorItem.Description;
			text = text + text2 + Environment.NewLine + Environment.NewLine + Game1.parseText(text3, Game1.smallFont, 256) + Environment.NewLine + Environment.NewLine;
			switch (dataOrErrorItem.ObjectType)
			{
			case "Arch":
			{
				text += (Game1.player.archaeologyFound.TryGetValue(id, out var array2) ? Game1.content.LoadString("Strings\\UI:Collections_Description_ArtifactsFound", array2[0]) : "");
				break;
			}
			case "Cooking":
			{
				text += (Game1.player.recipesCooked.TryGetValue(id, out var num3) ? Game1.content.LoadString("Strings\\UI:Collections_Description_RecipesCooked", num3) : "");
				if (hoverItem == null || hoverItem.ItemId != id)
				{
					hoverItem = new Object(id, 1);
					string text4 = hoverItem.Name;
					switch (text4)
					{
					case "Cheese Cauli.":
						text4 = "Cheese Cauliflower";
						break;
					case "Cheese Cauliflower":
						text4 = "Cheese Cauli.";
						break;
					case "Vegetable Medley":
						text4 = "Vegetable Stew";
						break;
					case "Cookie":
						text4 = "Cookies";
						break;
					case "Eggplant Parmesan":
						text4 = "Eggplant Parm.";
						break;
					case "Cranberry Sauce":
						text4 = "Cran. Sauce";
						break;
					case "Dish O' The Sea":
						text4 = "Dish o' The Sea";
						break;
					}
					hoverCraftingRecipe = new CraftingRecipe(text4, isCookingRecipe: true);
				}
				break;
			}
			case "Fish":
			{
				if (Game1.player.fishCaught.TryGetValue("(O)" + id, out var array))
				{
					text += Game1.content.LoadString("Strings\\UI:Collections_Description_FishCaught", array[0]);
					if (array[1] > 0)
					{
						text = text + Environment.NewLine + Game1.content.LoadString("Strings\\UI:Collections_Description_BiggestCatch", Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14083", (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.en) ? Math.Round((double)array[1] * 2.54) : ((double)array[1])));
					}
				}
				else
				{
					text += Game1.content.LoadString("Strings\\UI:Collections_Description_FishCaught", 0);
				}
				break;
			}
			default:
			{
				text = ((!(dataOrErrorItem.ObjectType == "Minerals") && dataOrErrorItem.Category != -2) ? (text + Game1.content.LoadString("Strings\\UI:Collections_Description_NumberShipped", Game1.player.basicShipped.TryGetValue(id, out var num) ? num : 0)) : (text + Game1.content.LoadString("Strings\\UI:Collections_Description_MineralsFound", Game1.player.mineralsFound.TryGetValue(id, out var num2) ? num2 : 0)));
				break;
			}
			}
			value = ObjectDataDefinition.GetRawPrice(dataOrErrorItem);
			break;
		}
		}
		return text;
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		foreach (ClickableTextureComponent value in sideTabs.Values)
		{
			value.draw(b);
		}
		if (currentPage > 0)
		{
			backButton.draw(b);
		}
		if (currentPage < collections[currentTab].Count - 1)
		{
			forwardButton.draw(b);
		}
		b.End();
		b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
		foreach (ClickableTextureComponent item in collections[currentTab][currentPage])
		{
			string[] array = ArgUtility.SplitBySpace(item.name);
			bool flag = Convert.ToBoolean(array[1]);
			bool flag2 = (currentTab == 4 && Convert.ToBoolean(array[2])) || (currentTab == 5 && !flag && item.hoverText != "???");
			item.draw(b, flag2 ? (Color.DimGray * 0.4f) : (flag ? Color.White : (Color.Black * 0.2f)), 0.86f);
			if (currentTab == 5 && flag)
			{
				int num = Utility.CreateRandom(Convert.ToInt32(array[0])).Next(12);
				b.Draw(Game1.mouseCursors, new Vector2(item.bounds.X + 16 + 16, item.bounds.Y + 20 + 16), new Rectangle(256 + num % 6 * 64 / 2, 128 + num / 6 * 64 / 2, 32, 32), Color.White, 0f, new Vector2(16f, 16f), item.scale, SpriteEffects.None, 0.88f);
			}
		}
		b.End();
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
		if (hoverItem != null)
		{
			string text = hoverItem.getDescription();
			string hoverTitle = hoverItem.DisplayName;
			if (text.Contains("{0}"))
			{
				string text2 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:" + hoverItem.Name + "_CollectionsTabDescription");
				if (text2 != null)
				{
					text = text2;
				}
				string text3 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Objects:" + hoverItem.Name + "_CollectionsTabName");
				if (text3 != null)
				{
					hoverTitle = text3;
				}
			}
			IClickableMenu.drawToolTip(b, text, hoverTitle, hoverItem, heldItem: false, -1, 0, null, -1, hoverCraftingRecipe);
		}
		else if (!hoverText.Equals(""))
		{
			IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont, 0, 0, this.value);
			if (secretNoteImage != -1)
			{
				IClickableMenu.drawTextureBox(b, Game1.getOldMouseX(), Game1.getOldMouseY() + 64 + 32, 288, 288, Color.White);
				b.Draw(secretNoteImageTexture, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 64 + 32 + 16), new Rectangle(secretNoteImage * 64 % secretNoteImageTexture.Width, secretNoteImage * 64 / secretNoteImageTexture.Width * 64, 64, 64), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.865f);
			}
		}
		letterviewerSubMenu?.draw(b);
	}
}
