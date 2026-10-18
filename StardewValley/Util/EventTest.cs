using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Menus;

namespace StardewValley.Util;

public class EventTest
{
	private int currentEventIndex;

	private int currentLocationIndex;

	private int aButtonTimer;

	private List<string> specificEventsToDo = new List<string>();

	private bool doingSpecifics;

	public EventTest(string startingLocationName = "", int startingEventIndex = 0)
	{
		currentLocationIndex = 0;
		if (startingLocationName.Length > 0)
		{
			for (int i = 0; i < Game1.locations.Count; i++)
			{
				if (Game1.locations[i].Name.Equals(startingLocationName))
				{
					currentLocationIndex = i;
					break;
				}
			}
		}
		currentEventIndex = startingEventIndex;
	}

	public EventTest(string[] whichEvents)
	{
		for (int i = 1; i < whichEvents.Length; i += 2)
		{
			specificEventsToDo.Add(whichEvents[i] + " " + whichEvents[i + 1]);
		}
		doingSpecifics = true;
		currentLocationIndex = -1;
	}

	public void update()
	{
		if (!Game1.eventUp && !Game1.fadeToBlack)
		{
			if (currentLocationIndex >= Game1.locations.Count)
			{
				return;
			}
			if (doingSpecifics && currentLocationIndex == -1)
			{
				if (specificEventsToDo.Count == 0)
				{
					return;
				}
				for (int i = 0; i < Game1.locations.Count; i++)
				{
					string text = specificEventsToDo.Last();
					string[] array = ArgUtility.SplitBySpace(text);
					if (!Game1.locations[i].Name.Equals(array[0]))
					{
						continue;
					}
					currentLocationIndex = i;
					int num = -1;
					foreach (KeyValuePair<string, string> item in Game1.content.Load<Dictionary<string, string>>("Data\\Events\\" + Game1.locations[i].Name))
					{
						num++;
						if (int.TryParse(item.Key.Split('/')[0], out var result) && result == Convert.ToInt32(array[1]))
						{
							currentEventIndex = num;
							break;
						}
					}
					specificEventsToDo.Remove(text);
					break;
				}
			}
			GameLocation gameLocation = Game1.locations[currentLocationIndex];
			if (gameLocation.currentEvent != null)
			{
				return;
			}
			string text2 = gameLocation.name.Value;
			if (text2 == "Pool")
			{
				text2 = "BathHouse_Pool";
			}
			bool flag = true;
			Dictionary<string, string> dictionary = null;
			try
			{
				dictionary = Game1.content.Load<Dictionary<string, string>>("Data\\Events\\" + text2);
			}
			catch (Exception)
			{
				flag = false;
			}
			if (flag && currentEventIndex < dictionary.Count)
			{
				KeyValuePair<string, string> keyValuePair = dictionary.ElementAt(currentEventIndex);
				string key = keyValuePair.Key;
				string script = keyValuePair.Value;
				if (key.Contains('/') && !script.Equals("null"))
				{
					if (Game1.currentLocation.Name.Equals(text2))
					{
						Game1.eventUp = true;
						Game1.currentLocation.currentEvent = new Event(script);
					}
					else
					{
						LocationRequest locationRequest = Game1.getLocationRequest(text2);
						locationRequest.OnLoad += delegate
						{
							Game1.currentLocation.currentEvent = new Event(script);
						};
						Game1.warpFarmer(locationRequest, 8, 8, Game1.player.FacingDirection);
					}
				}
			}
			currentEventIndex++;
			if (!flag || currentEventIndex >= dictionary.Count)
			{
				currentEventIndex = 0;
				currentLocationIndex++;
			}
			if (doingSpecifics)
			{
				currentLocationIndex = -1;
			}
			return;
		}
		aButtonTimer -= (int)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
		if (aButtonTimer < 0)
		{
			aButtonTimer = 100;
			if (Game1.activeClickableMenu is DialogueBox dialogueBox)
			{
				dialogueBox.performHoverAction(Game1.graphics.GraphicsDevice.Viewport.Width / 2, Game1.graphics.GraphicsDevice.Viewport.Height - 64 - Game1.random.Next(300));
				dialogueBox.receiveLeftClick(Game1.graphics.GraphicsDevice.Viewport.Width / 2, Game1.graphics.GraphicsDevice.Viewport.Height - 64 - Game1.random.Next(300));
			}
		}
	}
}
