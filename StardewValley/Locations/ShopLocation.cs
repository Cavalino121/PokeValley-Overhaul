using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;

namespace StardewValley.Locations;

public class ShopLocation : GameLocation
{
	public const int maxItemsToSellFromPlayer = 11;

	public readonly NetObjectList<Item> itemsFromPlayerToSell = new NetObjectList<Item>();

	public readonly NetObjectList<Item> itemsToStartSellingTomorrow = new NetObjectList<Item>();

	public ShopLocation()
	{
	}

	public ShopLocation(string map, string name)
		: base(map, name)
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(itemsFromPlayerToSell, "itemsFromPlayerToSell").AddField(itemsToStartSellingTomorrow, "itemsToStartSellingTomorrow");
	}

	/// <summary>Get a dialogue for an NPC when the player purchases an item from the shop, if they have any.</summary>
	/// <param name="i">The item that was purchased.</param>
	/// <param name="n">The NPC for which to get a dialogue.</param>
	/// <returns>Returns a dialogue to use, or <c>null</c> to skip this NPC.</returns>
	public virtual Dialogue getPurchasedItemDialogueForNPC(Object i, NPC n)
	{
		Dialogue result = null;
		string[] array = Game1.content.LoadString("Strings\\Lexicon:GenericPlayerTerm").Split('^');
		string text = array[0];
		if (array.Length > 1 && !Game1.player.IsMale)
		{
			text = array[1];
		}
		string text2 = ((Game1.random.NextDouble() < (double)(Game1.player.getFriendshipLevelForNPC(n.Name) / 1250)) ? Game1.player.Name : text);
		if (n.Age != 0)
		{
			text2 = Game1.player.Name;
		}
		string text3 = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en) ? Lexicon.getProperArticleForWord(i.name) : "");
		if ((i.Category == -4 || i.Category == -75 || i.Category == -79) && Game1.random.NextBool())
		{
			text3 = Game1.content.LoadString("Strings\\StringsFromCSFiles:SeedShop.cs.9701");
		}
		int num = Game1.random.Next(5);
		if (n.Manners == 2)
		{
			num = 2;
		}
		switch (num)
		{
		case 0:
			result = ((!(Game1.random.NextDouble() < (double)i.quality.Value * 0.5 + 0.2)) ? Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_1_QualityLow", text2, text3, i.DisplayName, Lexicon.getRandomNegativeFoodAdjective(n)) : Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_1_QualityHigh", text2, text3, i.DisplayName, Lexicon.getRandomDeliciousAdjective(n)));
			break;
		case 1:
			result = ((i.quality.Value != 0) ? ((!n.Name.Equals("Jodi")) ? Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_2_QualityHigh", text2, text3, i.DisplayName) : Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_2_QualityHigh_Jodi", text2, text3, i.DisplayName)) : Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_2_QualityLow", text2, text3, i.DisplayName));
			break;
		case 2:
			if (n.Manners == 2)
			{
				result = ((i.quality.Value == 2) ? Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_3_QualityHigh_Rude", text2, text3, i.DisplayName, i.salePrice() / 2, Lexicon.getRandomSlightlyPositiveAdjectiveForEdibleNoun(n)) : Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_3_QualityLow_Rude", text2, text3, i.DisplayName, i.salePrice() / 2, Lexicon.getRandomNegativeFoodAdjective(n), Lexicon.getRandomNegativeItemSlanderNoun()));
			}
			else
			{
				Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_3_NonRude", text2, text3, i.DisplayName, i.salePrice() / 2);
			}
			break;
		case 3:
			result = Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_4", text2, text3, i.DisplayName);
			break;
		case 4:
			switch (i.Category)
			{
			case -79:
			case -75:
				result = Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_5_VegetableOrFruit", text2, text3, i.DisplayName);
				break;
			case -7:
			{
				string randomPositiveAdjectiveForEventOrPerson = Lexicon.getRandomPositiveAdjectiveForEventOrPerson(n);
				result = Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_5_Cooking", text2, text3, i.DisplayName, Lexicon.getProperArticleForWord(randomPositiveAdjectiveForEventOrPerson), randomPositiveAdjectiveForEventOrPerson);
				break;
			}
			default:
				result = Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_5_Foraged", text2, text3, i.DisplayName);
				break;
			}
			break;
		}
		if (n.Age == 1 && Game1.random.NextDouble() < 0.6)
		{
			result = Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_Teen", text2, text3, i.DisplayName);
		}
		switch (n.Name)
		{
		case "Abigail":
			result = ((i.quality.Value != 0) ? Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_Abigail_QualityHigh", text2, text3, i.DisplayName) : Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_Abigail_QualityLow", text2, text3, i.DisplayName, Lexicon.getRandomNegativeItemSlanderNoun()));
			break;
		case "Caroline":
		{
			string translationKey2 = ((i.quality.Value == 0) ? "Data\\ExtraDialogue:PurchasedItem_Caroline_QualityLow" : "Data\\ExtraDialogue:PurchasedItem_Caroline_QualityHigh");
			result = Dialogue.FromTranslation(n, translationKey2, text2, text3, i.DisplayName);
			break;
		}
		case "Pierre":
		{
			string translationKey = ((i.quality.Value == 0) ? "Data\\ExtraDialogue:PurchasedItem_Pierre_QualityLow" : "Data\\ExtraDialogue:PurchasedItem_Pierre_QualityHigh");
			result = Dialogue.FromTranslation(n, translationKey, text2, text3, i.DisplayName);
			break;
		}
		case "Haley":
			result = Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_Haley", text2, text3, i.DisplayName);
			break;
		case "Elliott":
			result = Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_Elliott", text2, text3, i.DisplayName);
			break;
		case "Alex":
			result = Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_Alex", text2, text3, i.DisplayName);
			break;
		case "Leah":
			result = Dialogue.FromTranslation(n, "Data\\ExtraDialogue:PurchasedItem_Leah", text2, text3, i.DisplayName);
			break;
		}
		return result;
	}

	public override void DayUpdate(int dayOfMonth)
	{
		itemsToStartSellingTomorrow.RemoveWhere((Item p) => p == null);
		itemsFromPlayerToSell.RemoveWhere((Item p) => p == null);
		for (int num = itemsToStartSellingTomorrow.Count - 1; num >= 0; num--)
		{
			Item item = itemsToStartSellingTomorrow[num];
			if (itemsFromPlayerToSell.Count < 11)
			{
				bool flag = false;
				foreach (Item item2 in itemsFromPlayerToSell)
				{
					if (item2.Name == item.Name && item2.Quality == item.Quality)
					{
						item2.Stack += item.Stack;
						flag = true;
						break;
					}
				}
				itemsToStartSellingTomorrow.RemoveAt(num);
				if (!flag)
				{
					itemsFromPlayerToSell.Add(item);
				}
			}
		}
		base.DayUpdate(dayOfMonth);
	}
}
