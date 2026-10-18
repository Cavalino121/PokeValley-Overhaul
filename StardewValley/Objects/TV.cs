using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;

namespace StardewValley.Objects;

public class TV : Furniture
{
	public const int customChannel = 1;

	public const int weatherChannel = 2;

	public const int fortuneTellerChannel = 3;

	public const int tipsChannel = 4;

	public const int cookingChannel = 5;

	public const int fishingChannel = 6;

	private int currentChannel;

	private TemporaryAnimatedSprite screen;

	private TemporaryAnimatedSprite screenOverlay;

	private static Dictionary<int, string> weekToRecipeMap;

	public TV()
	{
	}

	public TV(string itemId, Vector2 tile)
		: base(itemId, tile)
	{
	}

	/// <inheritdoc />
	public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
	{
		if (justCheckingForActivity)
		{
			return true;
		}
		List<Response> list = new List<Response>();
		list.Add(new Response("Weather", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13105")));
		list.Add(new Response("Fortune", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13107")));
		switch (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth))
		{
		case "Mon":
		case "Thu":
			list.Add(new Response("Livin'", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13111")));
			break;
		case "Sun":
			list.Add(new Response("The", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13114")));
			break;
		case "Wed":
			if (Game1.stats.DaysPlayed > 7)
			{
				list.Add(new Response("The", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13117")));
			}
			break;
		}
		if (Game1.Date.Season == Season.Fall && Game1.Date.DayOfMonth == 26 && Game1.stats.Get("childrenTurnedToDoves") != 0 && !who.mailReceived.Contains("cursed_doll"))
		{
			list.Add(new Response("???", "???"));
		}
		if (Game1.player.mailReceived.Contains("pamNewChannel"))
		{
			list.Add(new Response("Fishing", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV_Fishing_Channel")));
		}
		list.Add(new Response("(Leave)", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13118")));
		Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13120"), list.ToArray(), selectChannel);
		Game1.player.Halt();
		return true;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new TV(base.ItemId, tileLocation.Value);
	}

	public virtual void selectChannel(Farmer who, string answer)
	{
		if (Game1.IsGreenRainingHere())
		{
			currentChannel = 9999;
			screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Rectangle(386, 334, 42, 28), 40f, 3, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
			Game1.drawObjectDialogue("...................");
			Game1.afterDialogues = proceedToNextScene;
		}
		else
		{
			switch (ArgUtility.SplitBySpaceAndGet(answer, 0))
			{
			case "Weather":
				currentChannel = 2;
				screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(413, 305, 42, 28), 150f, 2, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
				Game1.drawObjectDialogue(Game1.parseText(getWeatherChannelOpening()));
				Game1.afterDialogues = proceedToNextScene;
				break;
			case "Fortune":
				currentChannel = 3;
				screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(540, 305, 42, 28), 150f, 2, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
				Game1.drawObjectDialogue(Game1.parseText(getFortuneTellerOpening()));
				Game1.afterDialogues = proceedToNextScene;
				break;
			case "Livin'":
				currentChannel = 4;
				screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(517, 361, 42, 28), 150f, 2, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
				Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13124")));
				Game1.afterDialogues = proceedToNextScene;
				break;
			case "The":
				currentChannel = 5;
				screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(602, 361, 42, 28), 150f, 2, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
				Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13127")));
				Game1.afterDialogues = proceedToNextScene;
				break;
			case "???":
				Game1.changeMusicTrack("none");
				currentChannel = 666;
				screen = new TemporaryAnimatedSprite("Maps\\springobjects", new Rectangle(112, 64, 16, 16), 150f, 1, 999999, getScreenPosition() + ((base.QualifiedItemId == "(F)1468") ? new Vector2(56f, 32f) : new Vector2(8f, 8f)), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, 3f, 0f, 0f, 0f);
				Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Cursed_Doll")));
				Game1.afterDialogues = proceedToNextScene;
				break;
			case "Fishing":
				currentChannel = 6;
				screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(172, 33, 42, 28), 150f, 2, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
				Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Fishing_Channel_Intro")));
				Game1.afterDialogues = proceedToNextScene;
				break;
			}
		}
		if (currentChannel > 0)
		{
			Game1.currentLightSources.Add(new LightSource(GenerateLightSourceId(TileLocation) + "_Screen", 2, getScreenPosition() + ((base.QualifiedItemId == "(F)1468") ? new Vector2(88f, 80f) : new Vector2(38f, 48f)), (base.QualifiedItemId == "(F)1468") ? 1f : 0.55f, Color.Black, LightSource.LightContext.None, 0L, Location?.NameOrUniqueName));
		}
	}

	protected virtual string getFortuneTellerOpening()
	{
		switch (Game1.random.Next(5))
		{
		case 0:
			if (!Game1.player.IsMale)
			{
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13130");
			}
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13128");
		case 1:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13132");
		case 2:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13133");
		case 3:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13134");
		case 4:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13135");
		default:
			return "";
		}
	}

	protected virtual string getWeatherChannelOpening()
	{
		return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13136");
	}

	public virtual float getScreenSizeModifier()
	{
		if (!(base.QualifiedItemId == "(F)1468") && !(base.QualifiedItemId == "(F)2326"))
		{
			return 2f;
		}
		return 4f;
	}

	public virtual Vector2 getScreenPosition()
	{
		return base.QualifiedItemId switch
		{
			"(F)1466" => new Vector2(boundingBox.X + 24, boundingBox.Y), 
			"(F)1468" => new Vector2(boundingBox.X + 12, boundingBox.Y - 128 + 32), 
			"(F)2326" => new Vector2(boundingBox.X + 12, boundingBox.Y - 128 + 40), 
			"(F)1680" => new Vector2(boundingBox.X + 24, boundingBox.Y - 12), 
			"(F)RetroTV" => new Vector2(boundingBox.X + 24, boundingBox.Y - 64), 
			_ => Vector2.Zero, 
		};
	}

	public virtual void proceedToNextScene()
	{
		switch (currentChannel)
		{
		case 9999:
			turnOffTV();
			break;
		case 2:
			if (screenOverlay == null)
			{
				if (Utility.isGreenRainDay(Game1.dayOfMonth + 1, Game1.season))
				{
					screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Rectangle(213, 335, 43, 28), 9999f, 1, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f)
					{
						id = 776
					};
				}
				else
				{
					screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(497, 305, 42, 28), 9999f, 1, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f)
					{
						id = 777
					};
				}
				Game1.drawObjectDialogue(Game1.parseText(getWeatherForecast()));
				setWeatherOverlay();
				Game1.afterDialogues = proceedToNextScene;
			}
			else if (Game1.player.hasOrWillReceiveMail("Visited_Island") && screen.id == 777)
			{
				screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(148, 62, 42, 28), 9999f, 1, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
				Game1.drawObjectDialogue(Game1.parseText(getIslandWeatherForecast()));
				setWeatherOverlay(island: true);
				Game1.afterDialogues = proceedToNextScene;
			}
			else
			{
				turnOffTV();
			}
			break;
		case 3:
			if (screenOverlay == null)
			{
				if (Game1.player.team.sharedDailyLuck.Value >= 0.1)
				{
					screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Rectangle(424, 447, 42, 28), 9999f, 1, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
				}
				else if (Game1.player.team.sharedDailyLuck.Value <= -0.1)
				{
					screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Rectangle(424, 476, 42, 28), 9999f, 1, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
				}
				else
				{
					screen = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(624, 305, 42, 28), 9999f, 1, 999999, getScreenPosition(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
				}
				Game1.drawObjectDialogue(Game1.parseText(getFortuneForecast(Game1.player)));
				setFortuneOverlay(Game1.player);
				Game1.afterDialogues = proceedToNextScene;
			}
			else
			{
				turnOffTV();
			}
			break;
		case 4:
			if (screenOverlay == null)
			{
				Game1.drawObjectDialogue(Game1.parseText(getTodaysTip()));
				Game1.afterDialogues = proceedToNextScene;
				screenOverlay = new TemporaryAnimatedSprite
				{
					alpha = 1E-07f
				};
			}
			else
			{
				turnOffTV();
			}
			break;
		case 5:
			if (screenOverlay == null)
			{
				Game1.multipleDialogues(getWeeklyRecipe());
				Game1.afterDialogues = proceedToNextScene;
				screenOverlay = new TemporaryAnimatedSprite
				{
					alpha = 1E-07f
				};
			}
			else
			{
				turnOffTV();
			}
			break;
		case 666:
			Game1.flashAlpha = 1f;
			Game1.playSound("batScreech");
			Game1.createItemDebris(ItemRegistry.Create("(O)103"), Game1.player.getStandingPosition(), 1, Game1.currentLocation);
			Game1.player.mailReceived.Add("cursed_doll");
			turnOffTV();
			break;
		case 6:
			if (screenOverlay == null)
			{
				Game1.multipleDialogues(getFishingInfo());
				Game1.afterDialogues = proceedToNextScene;
				screenOverlay = new TemporaryAnimatedSprite
				{
					alpha = 1E-07f
				};
			}
			else
			{
				turnOffTV();
			}
			break;
		}
	}

	public virtual void turnOffTV()
	{
		currentChannel = 0;
		screen = null;
		screenOverlay = null;
		Utility.removeLightSource(GenerateLightSourceId(TileLocation) + "_Screen");
	}

	protected virtual void setWeatherOverlay(bool island = false)
	{
		WorldDate worldDate = new WorldDate(Game1.Date);
		int totalDays = worldDate.TotalDays + 1;
		worldDate.TotalDays = totalDays;
		string weatherOverlay = (island ? Game1.netWorldState.Value.GetWeatherForLocation("Island").WeatherForTomorrow : ((!Game1.IsMasterGame) ? Game1.getWeatherModificationsForDate(worldDate, Game1.netWorldState.Value.WeatherForTomorrow) : Game1.getWeatherModificationsForDate(worldDate, Game1.weatherForTomorrow)));
		setWeatherOverlay(weatherOverlay);
	}

	protected virtual void setWeatherOverlay(string weatherId)
	{
		switch (weatherId)
		{
		case "Snow":
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(465, 346, 13, 13), 100f, 4, 999999, getScreenPosition() + new Vector2(3f, 3f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
			break;
		case "Rain":
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(465, 333, 13, 13), 70f, 4, 999999, getScreenPosition() + new Vector2(3f, 3f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
			break;
		case "GreenRain":
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Rectangle(178, 363, 13, 13), 80f, 6, 999999, getScreenPosition() + new Vector2(3f, 3f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
			break;
		case "Wind":
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", Game1.IsSpring ? new Rectangle(465, 359, 13, 13) : (Game1.IsFall ? new Rectangle(413, 359, 13, 13) : new Rectangle(465, 346, 13, 13)), 70f, 4, 999999, getScreenPosition() + new Vector2(3f, 3f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
			break;
		case "Storm":
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(413, 346, 13, 13), 120f, 4, 999999, getScreenPosition() + new Vector2(3f, 3f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
			break;
		case "Festival":
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(413, 372, 13, 13), 120f, 4, 999999, getScreenPosition() + new Vector2(3f, 3f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
			break;
		default:
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(413, 333, 13, 13), 100f, 4, 999999, getScreenPosition() + new Vector2(3f, 3f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
			break;
		}
	}

	private string[] getFishingInfo()
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		int seasonIndex = Game1.seasonIndex;
		stringBuilder.AppendLine("---" + Utility.getSeasonNameFromNumber(seasonIndex) + "---^^");
		Dictionary<string, string> dictionary = DataLoader.Fish(Game1.content);
		IDictionary<string, LocationData> locationData = Game1.locationData;
		List<string> list2 = new List<string>();
		int num = 0;
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			if (item.Value.Contains("spring summer fall winter"))
			{
				continue;
			}
			list2.Clear();
			foreach (KeyValuePair<string, LocationData> item2 in locationData)
			{
				string key = item2.Key;
				GameLocation gameLocation = null;
				bool flag = false;
				if (item2.Value.Fish != null)
				{
					foreach (SpawnFishData item3 in item2.Value.Fish)
					{
						if (item3.IsBossFish || (item3.Season.HasValue && item3.Season != Game1.season) || (!(item3.ItemId == item.Key) && !(item3.ItemId == "(O)" + item.Key)))
						{
							continue;
						}
						if (item3.Condition != null)
						{
							gameLocation = gameLocation ?? Game1.getLocationFromName(key);
							if (!GameStateQuery.CheckConditions(item3.Condition, gameLocation))
							{
								continue;
							}
						}
						flag = true;
						break;
					}
				}
				if (flag)
				{
					string sanitizedFishingLocation = getSanitizedFishingLocation(key);
					if (sanitizedFishingLocation != "" && !list2.Contains(sanitizedFishingLocation))
					{
						list2.Add(sanitizedFishingLocation);
					}
				}
			}
			if (list2.Count <= 0)
			{
				continue;
			}
			string[] array = item.Value.Split('/');
			string[] array2 = ArgUtility.SplitBySpace(array[5]);
			string value = ItemRegistry.GetData("(O)" + item.Key)?.DisplayName ?? array[0];
			string text = array[7];
			string value2 = array2[0];
			string value3 = array2[1];
			stringBuilder2.Append(value);
			stringBuilder2.Append("...... ");
			stringBuilder2.Append(Game1.getTimeOfDayString(Convert.ToInt32(value2)).Replace(" ", ""));
			stringBuilder2.Append("-");
			stringBuilder2.Append(Game1.getTimeOfDayString(Convert.ToInt32(value3)).Replace(" ", ""));
			if (text != "both")
			{
				stringBuilder2.Append(", " + Game1.content.LoadString("Strings\\StringsFromCSFiles:TV_Fishing_Channel_" + text));
			}
			bool flag2 = false;
			foreach (string item4 in list2)
			{
				if (item4 != "")
				{
					flag2 = true;
					stringBuilder2.Append(", ");
					stringBuilder2.Append(item4);
				}
			}
			if (flag2)
			{
				stringBuilder2.Append("^^");
				stringBuilder.Append(stringBuilder2.ToString());
				num++;
			}
			stringBuilder2.Clear();
			if (num > 3)
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
				num = 0;
			}
		}
		return list.ToArray();
	}

	private string getSanitizedFishingLocation(string rawLocationName)
	{
		switch (rawLocationName)
		{
		case "Town":
		case "Forest":
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV_Fishing_Channel_River");
		case "Beach":
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV_Fishing_Channel_Ocean");
		case "Mountain":
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV_Fishing_Channel_Lake");
		default:
			return "";
		}
	}

	protected virtual string getTodaysTip()
	{
		if (!DataLoader.Tv_TipChannel(Game1.temporaryContent).TryGetValue((Game1.stats.DaysPlayed % 224).ToString() ?? "", out var value))
		{
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13148");
		}
		return value;
	}

	protected int getRerunWeek()
	{
		int num = Math.Min((int)(Game1.stats.DaysPlayed - 3) / 7, 32);
		if (weekToRecipeMap == null)
		{
			weekToRecipeMap = new Dictionary<int, string>();
			Dictionary<string, string> dictionary = DataLoader.Tv_CookingChannel(Game1.temporaryContent);
			foreach (string key in dictionary.Keys)
			{
				weekToRecipeMap[Convert.ToInt32(key)] = dictionary[key].Split('/')[0];
			}
		}
		List<Farmer> list = new List<Farmer>();
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			if (allFarmer.isCustomized.Value && !allFarmer.IsDedicatedPlayer)
			{
				list.Add(allFarmer);
			}
		}
		List<int> list2 = new List<int>();
		for (int i = 1; i <= num; i++)
		{
			foreach (Farmer item in list)
			{
				if (!item.cookingRecipes.ContainsKey(weekToRecipeMap[i]))
				{
					list2.Add(i);
					break;
				}
			}
		}
		Random random = Utility.CreateDaySaveRandom();
		if (list2.Count == 0)
		{
			return Math.Max(1, 1 + random.Next(num));
		}
		return list2[random.Next(list2.Count)];
	}

	protected virtual string[] getWeeklyRecipe()
	{
		int num = (int)(Game1.stats.DaysPlayed % 224 / 7);
		if (Game1.stats.DaysPlayed % 224 == 0)
		{
			num = 32;
		}
		Dictionary<string, string> channelData = DataLoader.Tv_CookingChannel(Game1.temporaryContent);
		FarmerTeam team = Game1.player.team;
		if (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Wed"))
		{
			if (team.lastDayQueenOfSauceRerunUpdated.Value != Game1.Date.TotalDays)
			{
				team.lastDayQueenOfSauceRerunUpdated.Set(Game1.Date.TotalDays);
				team.queenOfSauceRerunWeek.Set(getRerunWeek());
			}
			num = team.queenOfSauceRerunWeek.Value;
		}
		try
		{
			return getWeeklyRecipe(channelData, num.ToString());
		}
		catch
		{
			return getWeeklyRecipe(channelData, "1");
		}
	}

	private string[] getWeeklyRecipe(Dictionary<string, string> channelData, string id)
	{
		string key = channelData[id].Split('/')[0];
		bool flag = Game1.player.cookingRecipes.ContainsKey(key);
		string sub = new CraftingRecipe(key, isCookingRecipe: true).DisplayName;
		string[] result = new string[2]
		{
			channelData[id].Split('/')[1],
			flag ? Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13151", sub) : Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13153", sub)
		};
		if (!flag)
		{
			Game1.player.cookingRecipes.Add(key, 0);
		}
		return result;
	}

	private string getIslandWeatherForecast()
	{
		WorldDate worldDate = new WorldDate(Game1.Date);
		int totalDays = worldDate.TotalDays + 1;
		worldDate.TotalDays = totalDays;
		string weatherForTomorrow = Game1.netWorldState.Value.GetWeatherForLocation("Island").WeatherForTomorrow;
		string text = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV_IslandWeatherIntro");
		return weatherForTomorrow switch
		{
			"Sun" => text + Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs." + Game1.random.Choose("13182", "13183")), 
			"Rain" => text + Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13184"), 
			"Storm" => text + Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13185"), 
			_ => text + "???", 
		};
	}

	protected virtual string getWeatherForecast()
	{
		WorldDate worldDate = new WorldDate(Game1.Date);
		int totalDays = worldDate.TotalDays + 1;
		worldDate.TotalDays = totalDays;
		string weatherId = ((!Game1.IsMasterGame) ? Game1.getWeatherModificationsForDate(worldDate, Game1.netWorldState.Value.WeatherForTomorrow) : Game1.getWeatherModificationsForDate(worldDate, Game1.weatherForTomorrow));
		return getWeatherForecast(weatherId);
	}

	protected virtual string getWeatherForecast(string weatherId)
	{
		switch (weatherId)
		{
		case "Festival":
		{
			Dictionary<string, string> dictionary;
			try
			{
				dictionary = Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + (Game1.dayOfMonth + 1));
			}
			catch (Exception)
			{
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13164");
			}
			string[] array = dictionary["conditions"].Split('/');
			string[] array2 = ArgUtility.SplitBySpace(array[1]);
			string text = dictionary["name"];
			string text2 = array[0];
			int time = Convert.ToInt32(array2[0]);
			int time2 = Convert.ToInt32(array2[1]);
			string text3 = "";
			switch (text2)
			{
			case "Town":
				text3 = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13170");
				break;
			case "Beach":
				text3 = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13172");
				break;
			case "Forest":
				text3 = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13174");
				break;
			}
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13175", text, text3, Game1.getTimeOfDayString(time), Game1.getTimeOfDayString(time2));
		}
		case "Snow":
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs." + Game1.random.Choose("13180", "13181"));
		case "Rain":
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13184");
		case "GreenRain":
			return Game1.content.LoadString("Strings\\1_6_Strings:GreenRainForecast");
		case "Storm":
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13185");
		case "Wind":
			return Game1.season switch
			{
				Season.Spring => Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13187"), 
				Season.Fall => Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13189"), 
				_ => Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13190"), 
			};
		default:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs." + Game1.random.Choose("13182", "13183"));
		}
	}

	public virtual void setFortuneOverlay(Farmer who)
	{
		if (who.DailyLuck < -0.07)
		{
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(592, 346, 13, 13), 100f, 4, 999999, getScreenPosition() + new Vector2(15f, 1f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
		}
		else if (who.DailyLuck < -0.02)
		{
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(540, 346, 13, 13), 100f, 4, 999999, getScreenPosition() + new Vector2(15f, 1f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
		}
		else if (who.DailyLuck > 0.07)
		{
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(644, 333, 13, 13), 100f, 4, 999999, getScreenPosition() + new Vector2(15f, 1f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
		}
		else if (who.DailyLuck > 0.02)
		{
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(592, 333, 13, 13), 100f, 4, 999999, getScreenPosition() + new Vector2(15f, 1f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
		}
		else
		{
			screenOverlay = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(540, 333, 13, 13), 100f, 4, 999999, getScreenPosition() + new Vector2(15f, 1f) * getScreenSizeModifier(), flicker: false, flipped: false, (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f, 0f, Color.White, getScreenSizeModifier(), 0f, 0f, 0f);
		}
	}

	public virtual string getFortuneForecast(Farmer who)
	{
		string result;
		if (who.team.sharedDailyLuck.Value == -0.12)
		{
			result = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13191");
		}
		else if (who.DailyLuck < -0.07)
		{
			result = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13192");
		}
		else if (!(who.DailyLuck < -0.02))
		{
			result = ((who.team.sharedDailyLuck.Value == 0.12) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13197") : ((who.DailyLuck > 0.07) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13198") : ((!(who.DailyLuck > 0.02)) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13200") : Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13199"))));
		}
		else
		{
			Utility.CreateDaySaveRandom();
			result = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs." + Game1.random.Choose("13193", "13195"));
		}
		if (who.DailyLuck == 0.0)
		{
			result = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13201");
		}
		return result;
	}

	public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
	{
		base.draw(spriteBatch, x, y, alpha);
		if (screen != null)
		{
			screen.update(Game1.currentGameTime);
			screen.draw(spriteBatch);
			if (screenOverlay != null)
			{
				screenOverlay.update(Game1.currentGameTime);
				screenOverlay.draw(spriteBatch);
			}
		}
	}
}
