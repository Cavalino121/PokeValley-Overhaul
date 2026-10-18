using System;
using Microsoft.Xna.Framework;

namespace StardewValley.Events;

public class DiaryEvent : BaseFarmEvent
{
	public string NPCname;

	/// <inheritdoc />
	public override bool setUp()
	{
		if (Game1.player.isMarriedOrRoommates())
		{
			return true;
		}
		foreach (string item in Game1.player.mailReceived)
		{
			if (item.Contains("diary"))
			{
				string text = item.Split('_')[1];
				if (Game1.player.mailReceived.Add("diary_" + text + "_finished"))
				{
					NPCname = text.Split('/')[0];
					NPC characterFromName = Game1.getCharacterFromName(NPCname);
					string text2 = (Game1.player.IsMale ? Game1.content.LoadString("Strings\\StringsFromCSFiles:DiaryEvent.cs.6658") : Game1.content.LoadString("Strings\\StringsFromCSFiles:DiaryEvent.cs.6660")) + Environment.NewLine + Environment.NewLine + "-" + Utility.capitalizeFirstLetter(Game1.CurrentSeasonDisplayName) + " " + Game1.dayOfMonth + "-" + Environment.NewLine + Game1.content.LoadString("Strings\\StringsFromCSFiles:DiaryEvent.cs.6664", NPCname);
					Response[] answerChoices = new Response[3]
					{
						new Response("...We're", Game1.content.LoadString("Strings\\StringsFromCSFiles:DiaryEvent.cs.6667")),
						new Response("...I", (characterFromName.Gender == Gender.Male) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:DiaryEvent.cs.6669") : Game1.content.LoadString("Strings\\StringsFromCSFiles:DiaryEvent.cs.6670")),
						new Response("(Write", Game1.content.LoadString("Strings\\StringsFromCSFiles:DiaryEvent.cs.6672"))
					};
					Game1.currentLocation.createQuestionDialogue(Game1.parseText(text2), answerChoices, "diary");
					Game1.messagePause = true;
					return false;
				}
			}
		}
		return true;
	}

	/// <inheritdoc />
	public override bool tickUpdate(GameTime time)
	{
		return !Game1.dialogueUp;
	}

	public override void makeChangesToLocation()
	{
		Game1.messagePause = false;
	}
}
