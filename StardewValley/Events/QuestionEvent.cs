using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Menus;

namespace StardewValley.Events;

public class QuestionEvent : BaseFarmEvent
{
	public const int pregnancyQuestion = 1;

	public const int barnBirth = 2;

	public const int playerPregnancyQuestion = 3;

	private int whichQuestion;

	private AnimalHouse animalHouse;

	public FarmAnimal animal;

	public bool forceProceed;

	public QuestionEvent(int whichQuestion)
	{
		this.whichQuestion = whichQuestion;
	}

	/// <inheritdoc />
	public override bool setUp()
	{
		switch (whichQuestion)
		{
		case 1:
		{
			Response[] answerChoices2 = new Response[2]
			{
				new Response("Yes", Game1.content.LoadString("Strings\\Events:HaveBabyAnswer_Yes")),
				new Response("Not", Game1.content.LoadString("Strings\\Events:HaveBabyAnswer_No"))
			};
			NPC nPC = Game1.RequireCharacter(Game1.player.spouse);
			string path = ((!nPC.isAdoptionSpouse()) ? "Strings\\Events:HaveBabyQuestion" : "Strings\\Events:HaveBabyQuestion_Adoption");
			Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString(path, Game1.player.Name), answerChoices2, answerPregnancyQuestion, nPC);
			Game1.messagePause = true;
			return false;
		}
		case 2:
		{
			FarmAnimal a = null;
			Utility.ForEachBuilding(delegate(Building b)
			{
				if ((b.owner.Value == Game1.player.UniqueMultiplayerID || !Game1.IsMultiplayer) && b.AllowsAnimalPregnancy() && b.GetIndoors() is AnimalHouse animalHouse && !animalHouse.isFull() && Game1.random.NextDouble() < (double)animalHouse.animalsThatLiveHere.Count * 0.0055)
				{
					a = Utility.getAnimal(animalHouse.animalsThatLiveHere[Game1.random.Next(animalHouse.animalsThatLiveHere.Count)]);
					this.animalHouse = animalHouse;
					return false;
				}
				return true;
			});
			if (a != null && !a.isBaby() && a.allowReproduction.Value && a.CanHavePregnancy())
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Events:AnimalBirth", a.displayName, a.shortDisplayType()));
				Game1.messagePause = true;
				animal = a;
				return false;
			}
			break;
		}
		case 3:
		{
			Response[] answerChoices = new Response[2]
			{
				new Response("Yes", Game1.content.LoadString("Strings\\Events:HaveBabyAnswer_Yes")),
				new Response("Not", Game1.content.LoadString("Strings\\Events:HaveBabyAnswer_No"))
			};
			long value = Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).Value;
			Farmer farmer = Game1.otherFarmers[value];
			if (farmer.IsMale != Game1.player.IsMale)
			{
				Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\Events:HavePlayerBabyQuestion", farmer.displayName), answerChoices, answerPlayerPregnancyQuestion);
			}
			else
			{
				Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\Events:HavePlayerBabyQuestion_Adoption", farmer.displayName), answerChoices, answerPlayerPregnancyQuestion);
			}
			Game1.messagePause = true;
			return false;
		}
		}
		return true;
	}

	private void answerPregnancyQuestion(Farmer who, string answer)
	{
		if (answer.Equals("Yes"))
		{
			WorldDate worldDate = new WorldDate(Game1.Date);
			worldDate.TotalDays += 14;
			who.GetSpouseFriendship().NextBirthingDate = worldDate;
		}
	}

	private void answerPlayerPregnancyQuestion(Farmer who, string answer)
	{
		if (answer.Equals("Yes"))
		{
			long value = Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).Value;
			Farmer receiver = Game1.otherFarmers[value];
			Game1.player.team.SendProposal(receiver, ProposalType.Baby);
		}
	}

	/// <inheritdoc />
	public override bool tickUpdate(GameTime time)
	{
		if (forceProceed)
		{
			return true;
		}
		if (whichQuestion == 2 && !Game1.dialogueUp)
		{
			if (Game1.activeClickableMenu == null)
			{
				Game1.activeClickableMenu = new NamingMenu(animalHouse.addNewHatchedAnimal, (animal != null) ? Game1.content.LoadString("Strings\\Events:AnimalNamingTitle", animal.displayType) : Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestionEvent.cs.6692"));
			}
			return false;
		}
		return !Game1.dialogueUp;
	}

	/// <inheritdoc />
	public override void makeChangesToLocation()
	{
		Game1.messagePause = false;
	}
}
