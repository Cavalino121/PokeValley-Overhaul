using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Locations;

namespace StardewValley.Menus;

public class SkillsPage : IClickableMenu
{
	public const int region_special1 = 10201;

	public const int region_special2 = 10202;

	public const int region_special3 = 10203;

	public const int region_special4 = 10204;

	public const int region_special5 = 10205;

	public const int region_special6 = 10206;

	public const int region_special7 = 10207;

	public const int region_special8 = 10208;

	public const int region_special9 = 10209;

	public const int region_special_skullkey = 10210;

	public const int region_special_townkey = 10211;

	public const int region_ccTracker = 30211;

	public const int region_skillArea1 = 0;

	public const int region_skillArea2 = 1;

	public const int region_skillArea3 = 2;

	public const int region_skillArea4 = 3;

	public const int region_skillArea5 = 4;

	public List<ClickableTextureComponent> skillBars = new List<ClickableTextureComponent>();

	public List<ClickableTextureComponent> skillAreas = new List<ClickableTextureComponent>();

	public List<ClickableTextureComponent> specialItems = new List<ClickableTextureComponent>();

	public List<ClickableComponent> ccTrackerButtons = new List<ClickableComponent>();

	private string hoverText = "";

	private string hoverTitle = "";

	private int professionImage = -1;

	private int playerPanelIndex;

	private int playerPanelTimer;

	private Rectangle playerPanel;

	private int[] playerPanelFrames = new int[4] { 0, 1, 0, 2 };

	private int timesClickedJunimo;

	public SkillsPage(int x, int y, int width, int height)
		: base(x, y, width, height)
	{
		_ = xPositionOnScreen;
		_ = IClickableMenu.spaceToClearSideBorder;
		_ = yPositionOnScreen;
		_ = IClickableMenu.spaceToClearTopBorder;
		_ = (float)height / 2f;
		playerPanel = new Rectangle(xPositionOnScreen + 64, yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder, 128, 192);
		ClickableComponent.SetUpNeighbors(specialItems, 4);
		ClickableComponent.ChainNeighborsLeftRight(specialItems);
		if (!Game1.MasterPlayer.hasCompletedCommunityCenter() && !Game1.MasterPlayer.hasOrWillReceiveMail("JojaMember") && (Game1.MasterPlayer.hasOrWillReceiveMail("canReadJunimoText") || Game1.player.hasOrWillReceiveMail("canReadJunimoText")))
		{
			int num = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (int)((float)height / 2f) + 21;
			int num2 = xPositionOnScreen + IClickableMenu.spaceToClearSideBorder * 2;
			num2 += 80;
			num += 16;
			CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("ccBulletin"))
			{
				ccTrackerButtons.Add(new ClickableComponent(new Rectangle(num2, num, 44, 44), 5.ToString() ?? "", communityCenter.shouldNoteAppearInArea(5) ? Game1.content.LoadString("Strings\\Locations:CommunityCenter_AreaName_BulletinBoard") : "???")
				{
					myID = 30211,
					downNeighborID = -99998,
					rightNeighborID = -99998,
					leftNeighborID = -99998,
					upNeighborID = 4
				});
			}
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("ccBoilerRoom"))
			{
				ccTrackerButtons.Add(new ClickableComponent(new Rectangle(num2 + 60, num + 28, 44, 44), 3.ToString() ?? "", communityCenter.shouldNoteAppearInArea(3) ? Game1.content.LoadString("Strings\\Locations:CommunityCenter_AreaName_BoilerRoom") : "???")
				{
					myID = 30212,
					upNeighborID = 30211,
					leftNeighborID = 30211,
					downNeighborID = 30213,
					rightNeighborID = 4
				});
			}
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("ccVault"))
			{
				ccTrackerButtons.Add(new ClickableComponent(new Rectangle(num2 + 60, num + 88, 44, 44), 4.ToString() ?? "", communityCenter.shouldNoteAppearInArea(4) ? Game1.content.LoadString("Strings\\Locations:CommunityCenter_AreaName_Vault") : "???")
				{
					myID = 30213,
					upNeighborID = 30212,
					downNeighborID = 30216,
					leftNeighborID = 30215
				});
			}
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("ccCraftsRoom"))
			{
				ccTrackerButtons.Add(new ClickableComponent(new Rectangle(num2 - 60, num + 28, 44, 44), 1.ToString() ?? "", communityCenter.shouldNoteAppearInArea(1) ? Game1.content.LoadString("Strings\\Locations:CommunityCenter_AreaName_CraftsRoom") : "???")
				{
					myID = 30214,
					upNeighborID = 30211,
					downNeighborID = 30215,
					rightNeighborID = 30212
				});
			}
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("ccFishTank"))
			{
				ccTrackerButtons.Add(new ClickableComponent(new Rectangle(num2 - 60, num + 88, 44, 44), 2.ToString() ?? "", communityCenter.shouldNoteAppearInArea(2) ? Game1.content.LoadString("Strings\\Locations:CommunityCenter_AreaName_FishTank") : "???")
				{
					myID = 30215,
					upNeighborID = 30214,
					downNeighborID = 30216,
					rightNeighborID = 30213
				});
			}
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("ccPantry"))
			{
				ccTrackerButtons.Add(new ClickableComponent(new Rectangle(num2, num + 120, 44, 44), 0.ToString() ?? "", communityCenter.shouldNoteAppearInArea(0) ? Game1.content.LoadString("Strings\\Locations:CommunityCenter_AreaName_Pantry") : "???")
				{
					myID = 30216,
					upNeighborID = 30211,
					rightNeighborID = 30213,
					leftNeighborID = 30215
				});
			}
		}
		int num3 = 0;
		int num4 = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.it) ? (xPositionOnScreen + width - 448 - 48 + 4) : (xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 256 - 4));
		int num5 = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - 12;
		for (int i = 4; i < 10; i += 5)
		{
			for (int j = 0; j < 5; j++)
			{
				string professionBlurb = "";
				string professionTitle = "";
				bool flag = false;
				int whichProfession = -1;
				switch (j)
				{
				case 0:
					flag = Game1.player.FarmingLevel > i;
					whichProfession = Game1.player.getProfessionForSkill(0, i + 1);
					parseProfessionDescription(ref professionBlurb, ref professionTitle, LevelUpMenu.getProfessionDescription(whichProfession));
					break;
				case 1:
					flag = Game1.player.MiningLevel > i;
					whichProfession = Game1.player.getProfessionForSkill(3, i + 1);
					parseProfessionDescription(ref professionBlurb, ref professionTitle, LevelUpMenu.getProfessionDescription(whichProfession));
					break;
				case 2:
					flag = Game1.player.ForagingLevel > i;
					whichProfession = Game1.player.getProfessionForSkill(2, i + 1);
					parseProfessionDescription(ref professionBlurb, ref professionTitle, LevelUpMenu.getProfessionDescription(whichProfession));
					break;
				case 3:
					flag = Game1.player.FishingLevel > i;
					whichProfession = Game1.player.getProfessionForSkill(1, i + 1);
					parseProfessionDescription(ref professionBlurb, ref professionTitle, LevelUpMenu.getProfessionDescription(whichProfession));
					break;
				case 4:
					flag = Game1.player.CombatLevel > i;
					whichProfession = Game1.player.getProfessionForSkill(4, i + 1);
					parseProfessionDescription(ref professionBlurb, ref professionTitle, LevelUpMenu.getProfessionDescription(whichProfession));
					break;
				case 5:
					flag = Game1.player.LuckLevel > i;
					whichProfession = Game1.player.getProfessionForSkill(5, i + 1);
					parseProfessionDescription(ref professionBlurb, ref professionTitle, LevelUpMenu.getProfessionDescription(whichProfession));
					break;
				}
				if (flag && (i + 1) % 5 == 0)
				{
					skillBars.Add(new ClickableTextureComponent(whichProfession.ToString() ?? "", new Rectangle(num3 + num4 - 4 + i * 36, num5 + j * 68, 56, 36), null, professionBlurb, Game1.mouseCursors, new Rectangle(159, 338, 14, 9), 4f, drawShadow: true)
					{
						myID = ((i + 1 == 5) ? (100 + j) : (200 + j)),
						leftNeighborID = ((i + 1 == 5) ? j : (100 + j)),
						rightNeighborID = ((i + 1 == 5) ? (200 + j) : (-1)),
						downNeighborID = -99998
					});
				}
			}
			num3 += 24;
		}
		for (int k = 0; k < skillBars.Count; k++)
		{
			if (k < skillBars.Count - 1 && Math.Abs(skillBars[k + 1].myID - skillBars[k].myID) < 50)
			{
				skillBars[k].downNeighborID = skillBars[k + 1].myID;
				skillBars[k + 1].upNeighborID = skillBars[k].myID;
			}
		}
		if (skillBars.Count > 1 && skillBars.Last().myID >= 200 && skillBars[skillBars.Count - 2].myID >= 200)
		{
			skillBars.Last().upNeighborID = skillBars[skillBars.Count - 2].myID;
		}
		for (int l = 0; l < 5; l++)
		{
			int num6 = l switch
			{
				1 => 3, 
				3 => 1, 
				_ => l, 
			};
			string text = "";
			switch (num6)
			{
			case 0:
				if (Game1.player.FarmingLevel > 0)
				{
					text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11592", Game1.player.FarmingLevel) + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11594", Game1.player.FarmingLevel);
				}
				break;
			case 2:
				if (Game1.player.ForagingLevel > 0)
				{
					text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11596", Game1.player.ForagingLevel);
				}
				break;
			case 1:
				if (Game1.player.FishingLevel > 0)
				{
					text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11598", Game1.player.FishingLevel);
				}
				break;
			case 3:
				if (Game1.player.MiningLevel > 0)
				{
					text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11600", Game1.player.MiningLevel);
				}
				break;
			case 4:
				if (Game1.player.CombatLevel > 0)
				{
					text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11602", Game1.player.CombatLevel * 5);
				}
				break;
			}
			skillAreas.Add(new ClickableTextureComponent(num6.ToString() ?? "", new Rectangle(num4 - 128 - 48, num5 + l * 68, 148, 36), num6.ToString() ?? "", text, null, Rectangle.Empty, 1f)
			{
				myID = l,
				downNeighborID = ((l < 4) ? (l + 1) : (-99998)),
				upNeighborID = ((l > 0) ? (l - 1) : 12341),
				rightNeighborID = 100 + l
			});
		}
	}

	private void parseProfessionDescription(ref string professionBlurb, ref string professionTitle, List<string> professionDescription)
	{
		if (professionDescription.Count <= 0)
		{
			return;
		}
		professionTitle = professionDescription[0];
		for (int i = 1; i < professionDescription.Count; i++)
		{
			professionBlurb += professionDescription[i];
			if (i < professionDescription.Count - 1)
			{
				professionBlurb += Environment.NewLine;
			}
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = ((skillAreas.Count > 0) ? getComponentWithID(0) : null);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (x > xPositionOnScreen + IClickableMenu.spaceToClearSideBorder * 2 && x < xPositionOnScreen + IClickableMenu.spaceToClearSideBorder * 2 + 200 && y > yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (int)((float)height / 2f) + 21 && y < yPositionOnScreen + height && Game1.MasterPlayer.hasCompletedCommunityCenter() && !Game1.MasterPlayer.hasOrWillReceiveMail("JojaMember") && !Game1.player.mailReceived.Contains("activatedJungleJunimo"))
		{
			timesClickedJunimo++;
			if (timesClickedJunimo > 6)
			{
				Game1.playSound("discoverMineral");
				Game1.playSound("leafrustle");
				Game1.player.mailReceived.Add("activatedJungleJunimo");
			}
			else
			{
				Game1.playSound("hammer");
			}
		}
		foreach (ClickableComponent ccTrackerButton in ccTrackerButtons)
		{
			if (ccTrackerButton != null && ccTrackerButton.containsPoint(x, y) && !ccTrackerButton.label.Equals("???"))
			{
				Game1.activeClickableMenu = new JunimoNoteMenu(fromGameMenu: true, Convert.ToInt32(ccTrackerButton.name), fromThisMenu: true)
				{
					gameMenuTabToReturnTo = GameMenu.skillsTab
				};
				break;
			}
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoverText = "";
		hoverTitle = "";
		professionImage = -1;
		foreach (ClickableComponent ccTrackerButton in ccTrackerButtons)
		{
			if (ccTrackerButton != null && ccTrackerButton.containsPoint(x, y))
			{
				hoverText = ccTrackerButton.label;
				break;
			}
		}
		foreach (ClickableTextureComponent skillBar in skillBars)
		{
			skillBar.scale = 4f;
			if (skillBar.containsPoint(x, y) && skillBar.hoverText.Length > 0 && !skillBar.name.Equals("-1"))
			{
				hoverText = skillBar.hoverText;
				hoverTitle = LevelUpMenu.getProfessionTitleFromNumber(Convert.ToInt32(skillBar.name));
				professionImage = Convert.ToInt32(skillBar.name);
				skillBar.scale = 0f;
			}
		}
		foreach (ClickableTextureComponent skillArea in skillAreas)
		{
			if (skillArea.containsPoint(x, y) && skillArea.hoverText.Length > 0)
			{
				hoverText = skillArea.hoverText;
				hoverTitle = Farmer.getSkillDisplayNameFromIndex(Convert.ToInt32(skillArea.name));
				break;
			}
		}
		if (playerPanel.Contains(x, y))
		{
			playerPanelTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
			if (playerPanelTimer <= 0)
			{
				playerPanelIndex = (playerPanelIndex + 1) % 4;
				playerPanelTimer = 150;
			}
		}
		else
		{
			playerPanelIndex = 0;
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		int num = xPositionOnScreen + 64 - 8;
		int num2 = yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder;
		b.Draw((Game1.timeOfDay >= 1900) ? Game1.nightbg : Game1.daybg, new Vector2(num, num2 - 16 - 4), Color.White);
		FarmerRenderer.isDrawingForUI = true;
		Game1.player.FarmerRenderer.draw(b, new FarmerSprite.AnimationFrame(Game1.player.bathingClothes.Value ? 108 : playerPanelFrames[playerPanelIndex], 0, secondaryArm: false, flip: false), Game1.player.bathingClothes.Value ? 108 : playerPanelFrames[playerPanelIndex], new Rectangle(playerPanelFrames[playerPanelIndex] * 16, Game1.player.bathingClothes.Value ? 576 : 0, 16, 32), new Vector2(num + 32, num2 + 16 - 4), Vector2.Zero, 0.8f, 2, Color.White, 0f, 1f, Game1.player);
		if (Game1.timeOfDay >= 1900)
		{
			Game1.player.FarmerRenderer.draw(b, new FarmerSprite.AnimationFrame(playerPanelFrames[playerPanelIndex], 0, secondaryArm: false, flip: false), playerPanelFrames[playerPanelIndex], new Rectangle(playerPanelFrames[playerPanelIndex] * 16, 0, 16, 32), new Vector2(num + 32, num2 + 16 - 4), Vector2.Zero, 0.8f, 2, Color.DarkBlue * 0.3f, 0f, 1f, Game1.player);
		}
		FarmerRenderer.isDrawingForUI = false;
		b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder * 2, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (int)((float)height / 2f) + 21, width - IClickableMenu.spaceToClearSideBorder * 4 - 8, 4), new Color(214, 143, 84));
		b.DrawString(Game1.smallFont, Game1.player.Name, new Vector2((float)(num + 64) - Game1.smallFont.MeasureString(Game1.player.Name).X / 2f, num2 + 192 - 17), Game1.textColor);
		b.DrawString(Game1.smallFont, Game1.player.getTitle(), new Vector2((float)(num + 64) - Game1.smallFont.MeasureString(Game1.player.getTitle()).X / 2f, num2 + 256 - 32 - 19), Game1.textColor);
		num = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.it) ? (xPositionOnScreen + width - 448 - 48) : (xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 256 - 8));
		num2 = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - 8;
		int num3 = 0;
		int num4 = 68;
		for (int i = 0; i < 10; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				bool flag = false;
				bool flag2 = false;
				string text = "";
				int num5 = 0;
				Rectangle value = Rectangle.Empty;
				switch (j)
				{
				case 0:
					flag = Game1.player.FarmingLevel > i;
					if (i == 0)
					{
						text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11604");
					}
					num5 = Game1.player.FarmingLevel;
					flag2 = Game1.player.buffs.FarmingLevel > 0;
					value = new Rectangle(10, 428, 10, 10);
					break;
				case 1:
					flag = Game1.player.MiningLevel > i;
					if (i == 0)
					{
						text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11605");
					}
					num5 = Game1.player.MiningLevel;
					flag2 = Game1.player.buffs.MiningLevel > 0;
					value = new Rectangle(30, 428, 10, 10);
					break;
				case 2:
					flag = Game1.player.ForagingLevel > i;
					if (i == 0)
					{
						text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11606");
					}
					num5 = Game1.player.ForagingLevel;
					flag2 = Game1.player.buffs.ForagingLevel > 0;
					value = new Rectangle(60, 428, 10, 10);
					break;
				case 3:
					flag = Game1.player.FishingLevel > i;
					if (i == 0)
					{
						text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11607");
					}
					num5 = Game1.player.FishingLevel;
					flag2 = Game1.player.buffs.FishingLevel > 0;
					value = new Rectangle(20, 428, 10, 10);
					break;
				case 4:
					flag = Game1.player.CombatLevel > i;
					if (i == 0)
					{
						text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11608");
					}
					num5 = Game1.player.CombatLevel;
					flag2 = Game1.player.buffs.CombatLevel > 0;
					value = new Rectangle(120, 428, 10, 10);
					break;
				case 5:
					flag = Game1.player.LuckLevel > i;
					if (i == 0)
					{
						text = Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11609");
					}
					num5 = Game1.player.LuckLevel;
					flag2 = Game1.player.buffs.LuckLevel > 0;
					value = new Rectangle(50, 428, 10, 10);
					break;
				}
				if (!text.Equals(""))
				{
					b.DrawString(Game1.smallFont, text, new Vector2((float)num - Game1.smallFont.MeasureString(text).X + 4f - 64f, num2 + 4 + j * num4), Game1.textColor);
					b.Draw(Game1.mouseCursors, new Vector2(num - 56, num2 + j * num4), value, Color.Black * 0.3f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.85f);
					b.Draw(Game1.mouseCursors, new Vector2(num - 52, num2 - 4 + j * num4), value, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.87f);
				}
				if (!flag && (i + 1) % 5 == 0)
				{
					b.Draw(Game1.mouseCursors, new Vector2(num3 + num - 4 + i * 36, num2 + j * num4), new Rectangle(145, 338, 14, 9), Color.Black * 0.35f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.87f);
					b.Draw(Game1.mouseCursors, new Vector2(num3 + num + i * 36, num2 - 4 + j * num4), new Rectangle(145 + (flag ? 14 : 0), 338, 14, 9), Color.White * (flag ? 1f : 0.65f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.87f);
				}
				else if ((i + 1) % 5 != 0)
				{
					b.Draw(Game1.mouseCursors, new Vector2(num3 + num - 4 + i * 36, num2 + j * num4), new Rectangle(129, 338, 8, 9), Color.Black * 0.35f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.85f);
					b.Draw(Game1.mouseCursors, new Vector2(num3 + num + i * 36, num2 - 4 + j * num4), new Rectangle(129 + (flag ? 8 : 0), 338, 8, 9), Color.White * (flag ? 1f : 0.65f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.87f);
				}
				if (i == 9)
				{
					NumberSprite.draw(num5, b, new Vector2(num3 + num + (i + 2) * 36 + 12 + ((num5 >= 10) ? 12 : 0), num2 + 16 + j * num4), Color.Black * 0.35f, 1f, 0.85f, 1f, 0);
					NumberSprite.draw(num5, b, new Vector2(num3 + num + (i + 2) * 36 + 16 + ((num5 >= 10) ? 12 : 0), num2 + 12 + j * num4), (flag2 ? Color.LightGreen : Color.SandyBrown) * ((num5 == 0) ? 0.75f : 1f), 1f, 0.87f, 1f, 0);
				}
			}
			if ((i + 1) % 5 == 0)
			{
				num3 += 24;
			}
		}
		foreach (ClickableTextureComponent skillBar in skillBars)
		{
			skillBar.draw(b);
		}
		foreach (ClickableTextureComponent skillBar2 in skillBars)
		{
			if (skillBar2.scale == 0f)
			{
				IClickableMenu.drawTextureBox(b, skillBar2.bounds.X - 16 - 8, skillBar2.bounds.Y - 16 - 16, 96, 96, Color.White);
				b.Draw(Game1.mouseCursors, new Vector2(skillBar2.bounds.X - 8, skillBar2.bounds.Y - 32 + 16), new Rectangle(professionImage % 6 * 16, 624 + professionImage / 6 * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			}
		}
		num = xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + 32 - 8;
		num2 = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 320 - 36;
		if (Game1.netWorldState.Value.GoldenWalnuts > 0)
		{
			b.Draw(Game1.objectSpriteSheet, new Vector2(num + ((Game1.player.QiGems <= 0) ? 24 : 0), num2), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 73, 16, 16), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
			num += ((Game1.player.QiGems <= 0) ? 60 : 36);
			b.DrawString(Game1.smallFont, Game1.netWorldState.Value.GoldenWalnuts.ToString() ?? "", new Vector2(num, num2), Game1.textColor);
			num += 56;
		}
		if (Game1.player.QiGems > 0)
		{
			b.Draw(Game1.objectSpriteSheet, new Vector2(num + ((Game1.netWorldState.Value.GoldenWalnuts <= 0) ? 24 : 0), num2), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 858, 16, 16), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
			num += ((Game1.netWorldState.Value.GoldenWalnuts <= 0) ? 60 : 36);
			b.DrawString(Game1.smallFont, Game1.player.QiGems.ToString() ?? "", new Vector2(num, num2), Game1.textColor);
			num += 64;
		}
		num2 = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (int)((float)height / 2f) + 21;
		num = xPositionOnScreen + IClickableMenu.spaceToClearSideBorder * 2;
		bool flag3 = Game1.MasterPlayer.mailReceived.Contains("JojaMember");
		num += 80;
		num2 += 16;
		if (flag3 || Game1.MasterPlayer.hasOrWillReceiveMail("canReadJunimoText") || Game1.player.hasOrWillReceiveMail("canReadJunimoText"))
		{
			if (!flag3)
			{
				b.Draw(Game1.mouseCursors_1_6, new Vector2(num, num2), new Rectangle(Game1.MasterPlayer.hasOrWillReceiveMail("ccBulletin") ? 374 : 363, 298 + (flag3 ? 11 : 0), 11, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
			}
			else
			{
				b.Draw(Game1.mouseCursors_1_6, new Vector2(num - 80, num2 - 16), new Rectangle(363, 250, 51, 48), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
			}
			b.Draw(Game1.mouseCursors_1_6, new Vector2(num + 60, num2 + 28), new Rectangle(Game1.MasterPlayer.hasOrWillReceiveMail("ccBoilerRoom") ? 374 : 363, 298 + (flag3 ? 11 : 0), 11, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
			b.Draw(Game1.mouseCursors_1_6, new Vector2(num + 60, num2 + 88), new Rectangle(Game1.MasterPlayer.hasOrWillReceiveMail("ccVault") ? 374 : 363, 298 + (flag3 ? 11 : 0), 11, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
			b.Draw(Game1.mouseCursors_1_6, new Vector2(num - 60, num2 + 28), new Rectangle(Game1.MasterPlayer.hasOrWillReceiveMail("ccCraftsRoom") ? 374 : 363, 298 + (flag3 ? 11 : 0), 11, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
			b.Draw(Game1.mouseCursors_1_6, new Vector2(num - 60, num2 + 88), new Rectangle(Game1.MasterPlayer.hasOrWillReceiveMail("ccFishTank") ? 374 : 363, 298 + (flag3 ? 11 : 0), 11, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
			b.Draw(Game1.mouseCursors_1_6, new Vector2(num, num2 + 120), new Rectangle(Game1.MasterPlayer.hasOrWillReceiveMail("ccPantry") ? 374 : 363, 298 + (flag3 ? 11 : 0), 11, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
			if (!Utility.hasFinishedJojaRoute() && Game1.MasterPlayer.hasCompletedCommunityCenter())
			{
				b.Draw(Game1.mouseCursors_1_6, new Vector2((float)(num - 4) + 30f, (float)(num2 + 52) + 30f), new Rectangle(386, 299, 13, 15), Color.White, 0f, new Vector2(7.5f), 4f + (float)timesClickedJunimo * 0.2f, SpriteEffects.None, 0.7f);
				if (Game1.player.mailReceived.Contains("activatedJungleJunimo"))
				{
					b.Draw(Game1.mouseCursors_1_6, new Vector2(num - 80, num2 - 16), new Rectangle(311, 251, 51, 48), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
				}
			}
		}
		else
		{
			b.Draw(Game1.mouseCursors_1_6, new Vector2(num - 80, num2 - 16), new Rectangle(414, 250, 52, 47), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
		}
		num += 124;
		b.Draw(Game1.staminaRect, new Rectangle(num, num2 - 16, 4, (int)((float)height / 3f) - 32 - 4), new Color(214, 143, 84));
		int num6 = 0;
		if (Game1.smallFont.MeasureString(Game1.content.LoadString("Strings\\UI:Inventory_PortraitHover_Level", Game1.player.houseUpgradeLevel.Value + 1)).X > 120f)
		{
			num6 -= 20;
		}
		num2 += 108;
		num += 28;
		b.Draw(Game1.mouseCursors, new Vector2(num + num6 + 20, num2 - 4), new Rectangle(653, 880, 10, 10), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
		Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:Inventory_PortraitHover_Level", Game1.player.houseUpgradeLevel.Value + 1), Game1.smallFont, new Vector2(num + num6 + 72, num2), Game1.textColor);
		if (Game1.player.houseUpgradeLevel.Value >= 3)
		{
			int num7 = 709;
			b.Draw(Game1.mouseCursors, new Vector2((float)(num + num6) + 50f, (float)num2 - 4f) + new Vector2(0f, (float)((0.0 - Game1.currentGameTime.TotalGameTime.TotalMilliseconds) % 2000.0) * 0.01f), new Rectangle(372, 1956, 10, 10), new Color(80, 80, 80) * 1f * 0.53f * (1f - (float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000.0) / 2000f), (float)((0.0 - Game1.currentGameTime.TotalGameTime.TotalMilliseconds) % 2000.0) * 0.001f, new Vector2(3f, 3f), 0.5f + (float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000.0) / 1000f, SpriteEffects.None, 0.7f);
			b.Draw(Game1.mouseCursors, new Vector2((float)(num + num6) + 50f, (float)num2 - 4f) + new Vector2(0f, (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num7)) % 2000.0) * 0.01f), new Rectangle(372, 1956, 10, 10), new Color(80, 80, 80) * 1f * 0.53f * (1f - (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num7) % 2000.0) / 2000f), (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num7)) % 2000.0) * 0.001f, new Vector2(5f, 5f), 0.5f + (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num7) % 2000.0) / 1000f, SpriteEffects.None, 0.7f);
			b.Draw(Game1.mouseCursors, new Vector2((float)(num + num6) + 50f, (float)num2 - 4f) + new Vector2(0f, (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num7 * 2))) % 2000.0) * 0.01f), new Rectangle(372, 1956, 10, 10), new Color(80, 80, 80) * 1f * 0.53f * (1f - (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num7 * 2)) % 2000.0) / 2000f), (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num7 * 2))) % 2000.0) * 0.001f, new Vector2(4f, 4f), 0.5f + (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num7 * 2)) % 2000.0) / 1000f, SpriteEffects.None, 0.7f);
		}
		num += 180;
		num2 -= 8;
		bool flag4 = false;
		int num8 = MineShaft.lowestLevelReached;
		if (num8 > 120)
		{
			num8 -= 120;
			flag4 = true;
		}
		b.Draw(Game1.mouseCursors_1_6, new Vector2(num + 8, num2), new Rectangle((num8 == 0) ? 434 : 385, 315, 13, 13), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
		if (num8 != 0)
		{
			Utility.drawTextWithShadow(b, num8.ToString() ?? "", Game1.smallFont, new Vector2(num + 72 + (flag4 ? 8 : 0), num2 + 8), Game1.textColor);
		}
		if (flag4)
		{
			b.Draw(Game1.mouseCursors_1_6, new Vector2(num + 40, num2 + 24), new Rectangle(412, 319, 8, 9), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
		}
		num += 120;
		int num9 = Utility.numStardropsFound();
		if (num9 > 0)
		{
			b.Draw(Game1.mouseCursors_1_6, new Vector2(num + 32, num2 - 4), new Rectangle(399, 314, 12, 14), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
			Utility.drawTextWithShadow(b, "x " + num9, Game1.smallFont, new Vector2(num + 88, num2 + 8), (num9 >= 7) ? new Color(160, 30, 235) : Game1.textColor);
		}
		else
		{
			b.Draw(Game1.mouseCursors_1_6, new Vector2(num + 32, num2 - 4), new Rectangle(421, 314, 12, 14), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
		}
		if (Game1.stats.Get("MasteryExp") != 0)
		{
			int currentMasteryLevel = MasteryTrackerMenu.getCurrentMasteryLevel();
			string text2 = Game1.content.LoadString("Strings\\1_6_Strings:Mastery");
			text2 = text2.TrimEnd(':');
			float x = Game1.smallFont.MeasureString(text2).X;
			int num10 = (int)x - 64;
			int num11 = 84;
			b.DrawString(Game1.smallFont, text2, new Vector2(xPositionOnScreen + 256, num11 + yPositionOnScreen + 408), Game1.textColor);
			Utility.drawWithShadow(b, Game1.mouseCursors_1_6, new Vector2(num10 + xPositionOnScreen + 332, num11 + yPositionOnScreen + 400), new Rectangle(457, 298, 11, 11), Color.White, 0f, Vector2.Zero);
			float num12 = 0.64f;
			num12 -= (x - 100f) / 800f;
			if (Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ru)
			{
				num12 += 0.1f;
			}
			b.Draw(Game1.staminaRect, new Rectangle(num10 + xPositionOnScreen + 380 - 1, num11 + yPositionOnScreen + 408, (int)(584f * num12) + 4, 40), Color.Black * 0.35f);
			b.Draw(Game1.staminaRect, new Rectangle(num10 + xPositionOnScreen + 384, num11 + yPositionOnScreen + 404, (int)((float)(((currentMasteryLevel >= 5) ? 144 : 146) * 4) * num12) + 4, 40), new Color(60, 60, 25));
			b.Draw(Game1.staminaRect, new Rectangle(num10 + xPositionOnScreen + 388, num11 + yPositionOnScreen + 408, (int)(576f * num12), 32), new Color(173, 129, 79));
			MasteryTrackerMenu.drawBar(b, new Vector2(num10 + xPositionOnScreen + 276, num11 + yPositionOnScreen + 264), num12);
			NumberSprite.draw(currentMasteryLevel, b, new Vector2(num10 + xPositionOnScreen + 408 + (int)(584f * num12), num11 + yPositionOnScreen + 428), Color.Black * 0.35f, 1f, 0.85f, 1f, 0);
			NumberSprite.draw(currentMasteryLevel, b, new Vector2(num10 + xPositionOnScreen + 412 + (int)(584f * num12), num11 + yPositionOnScreen + 424), Color.SandyBrown * ((currentMasteryLevel == 0) ? 0.75f : 1f), 1f, 0.87f, 1f, 0);
		}
		else
		{
			b.Draw(Game1.mouseCursors_1_6, new Vector2(num - 304, num2 - 88), new Rectangle(366, 236, 142, 12), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
		}
		Rectangle value2 = new Rectangle(394, 120 + Game1.seasonIndex * 23, 33, 23);
		if (Game1.isGreenRain)
		{
			value2 = new Rectangle(427, 143, 33, 23);
		}
		else if (Game1.player.activeDialogueEvents.ContainsKey("married"))
		{
			value2 = new Rectangle(427, 97, 33, 23);
		}
		else if (Game1.IsSpring && Game1.dayOfMonth == 13)
		{
			value2.X += 33;
		}
		else if (Game1.IsSummer && Game1.dayOfMonth == 11)
		{
			value2.X += 66;
		}
		else if (Game1.IsFall && Game1.dayOfMonth == 27)
		{
			value2.X += 33;
		}
		else if (Game1.IsWinter && Game1.dayOfMonth == 25)
		{
			value2.X += 33;
		}
		b.Draw(Game1.mouseCursors_1_6, new Vector2(num + 144, num2 - 20), value2, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
		if (Game1.IsWinter && Game1.player.mailReceived.Contains("sawSecretSanta" + Game1.year) && ((Game1.dayOfMonth >= 18 && Game1.dayOfMonth < 25) || (Game1.dayOfMonth == 25 && Game1.timeOfDay < 1500)))
		{
			NPC randomWinterStarParticipant = Utility.GetRandomWinterStarParticipant();
			Texture2D texture;
			try
			{
				texture = Game1.content.Load<Texture2D>("Characters\\" + randomWinterStarParticipant.Name + "_Winter");
			}
			catch
			{
				texture = randomWinterStarParticipant.Sprite.Texture;
			}
			Rectangle mugShotSourceRect = randomWinterStarParticipant.getMugShotSourceRect();
			mugShotSourceRect.Height -= 5;
			b.Draw(texture, new Vector2(num + 180, num2), mugShotSourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
			b.Draw(Game1.mouseCursors, new Vector2(num + 244, num2 + 40), new Rectangle(147, 412, 10, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.7f);
		}
		if (hoverText.Length > 0)
		{
			IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont, 0, 0, -1, (hoverTitle.Length > 0) ? hoverTitle : null);
		}
	}
}
