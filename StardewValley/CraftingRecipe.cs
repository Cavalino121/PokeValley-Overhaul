using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Extensions;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.TokenizableStrings;

namespace StardewValley;

public class CraftingRecipe
{
	public const int wild_seed_special_category = -777;

	/// <summary>The index in <c>Data/CookingRecipes</c> or <c>Data/CraftingRecipes</c> for the ingredient list.</summary>
	public const int index_ingredients = 0;

	/// <summary>The index in <c>Data/CookingRecipes</c> or <c>Data/CraftingRecipes</c> for the produced items.</summary>
	public const int index_output = 2;

	/// <summary>The index in <c>Data/CookingRecipes</c> for the unlock conditions.</summary>
	public const int index_cookingUnlockConditions = 3;

	/// <summary>The index in <c>Data/CookingRecipes</c> for the optional translated recipe name. If omitted, the display name for the first output item is used.</summary>
	public const int index_cookingDisplayName = 4;

	/// <summary>The index in <c>Data/CraftingRecipes</c> for whether it produces a <see cref="F:StardewValley.ItemRegistry.type_bigCraftable" /> item.</summary>
	public const int index_craftingBigCraftable = 3;

	/// <summary>The index in <c>Data/CraftingRecipes</c> for the unlock conditions.</summary>
	public const int index_craftingUnlockConditions = 4;

	/// <summary>The index in <c>Data/CraftingRecipes</c> for the optional translated recipe name. If omitted, the display name for the first output item is used.</summary>
	public const int index_craftingDisplayName = 5;

	/// <summary>The recipe key in <c>Data/CookingRecipes</c> or <c>Data/CraftingRecipes</c>.</summary>
	public string name;

	/// <summary>The translated display name for this recipe.</summary>
	public string DisplayName;

	/// <summary>The translated description for the item produced by recipe.</summary>
	public string description;

	/// <summary>The cached crafting recipe data loaded from <c>Data/CraftingRecipes</c>.</summary>
	public static Dictionary<string, string> craftingRecipes;

	/// <summary>The cached cooking recipe data loaded from <c>Data/CookingRecipes</c>.</summary>
	public static Dictionary<string, string> cookingRecipes;

	/// <summary>The ingredients needed by this recipe, indexed by unqualified item ID or category number.</summary>
	public Dictionary<string, int> recipeList = new Dictionary<string, int>();

	/// <summary>The unqualified item IDs produced by this recipe. If there are multiple items, one is chosen at random each time.</summary>
	public List<string> itemToProduce = new List<string>();

	/// <summary>Whether this recipe produces a <see cref="F:StardewValley.ItemRegistry.type_bigCraftable" /> item, instead of an <see cref="F:StardewValley.ItemRegistry.type_object" /> item.</summary>
	public bool bigCraftable;

	/// <summary>Whether this is a recipe in <c>Data/CookingRecipes</c> (true) or <c>Data/CraftingRecipes</c> (false).</summary>
	public bool isCookingRecipe;

	/// <summary>The number of times this recipe has been crafted by the player.</summary>
	public int timesCrafted;

	/// <summary>The number of the selected item in <see cref="F:StardewValley.CraftingRecipe.itemToProduce" /> to produce.</summary>
	public int numberProducedPerCraft;

	public static void InitShared()
	{
		craftingRecipes = DataLoader.CraftingRecipes(Game1.content);
		cookingRecipes = DataLoader.CookingRecipes(Game1.content);
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="name">The recipe key in <c>Data/CookingRecipes</c> or <c>Data/CraftingRecipes</c>.</param>
	public CraftingRecipe(string name)
		: this(name, cookingRecipes.ContainsKey(name))
	{
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="name">The recipe key in <c>Data/CookingRecipes</c> or <c>Data/CraftingRecipes</c>.</param>
	/// <param name="isCookingRecipe">Whether this is a recipe in <c>Data/CookingRecipes</c> (true) or <c>Data/CraftingRecipes</c> (false).</param>
	public CraftingRecipe(string name, bool isCookingRecipe)
	{
		this.isCookingRecipe = isCookingRecipe;
		this.name = name;
		string text;
		if (isCookingRecipe && cookingRecipes.TryGetValue(name, out var value))
		{
			text = value;
		}
		else if (craftingRecipes.TryGetValue(name, out value))
		{
			text = value;
		}
		else
		{
			this.name = (name = "Torch");
			text = craftingRecipes[name];
		}
		string[] array = text.Split('/');
		if (!ArgUtility.TryGet(array, 0, out var value2, out var error, allowBlank: false, "string rawIngredients"))
		{
			value2 = "";
			LogParseError(text, error);
		}
		if (!ArgUtility.TryGet(array, 2, out var value3, out error, allowBlank: false, "string rawOutputItems"))
		{
			value3 = "";
			LogParseError(text, error);
		}
		if (!ArgUtility.TryGetOptional(array, isCookingRecipe ? 4 : 5, out var value4, out error, null, allowBlank: true, "string tokenizableDisplayName"))
		{
			LogParseError(text, error);
		}
		bigCraftable = !isCookingRecipe && ArgUtility.GetBool(array, 3);
		string[] array2 = ArgUtility.SplitBySpace(value2);
		for (int i = 0; i < array2.Length; i += 2)
		{
			recipeList.Add(array2[i], ArgUtility.GetInt(array2, i + 1, 1));
		}
		string[] array3 = ArgUtility.SplitBySpace(value3);
		for (int j = 0; j < array3.Length; j += 2)
		{
			itemToProduce.Add(array3[j]);
			numberProducedPerCraft = ArgUtility.GetInt(array3, j + 1, 1);
		}
		ParsedItemData itemData = GetItemData(useFirst: true);
		DisplayName = ((!string.IsNullOrWhiteSpace(value4)) ? TokenParser.ParseText(value4) : (itemData?.DisplayName ?? value3));
		description = itemData?.Description ?? "";
		if (!Game1.player.craftingRecipes.TryGetValue(name, out timesCrafted))
		{
			timesCrafted = 0;
		}
		if (name.Equals("Crab Pot") && Game1.player.professions.Contains(7))
		{
			recipeList = new Dictionary<string, int>
			{
				["388"] = 25,
				["334"] = 2
			};
		}
	}

	public virtual string getIndexOfMenuView()
	{
		if (itemToProduce.Count <= 0)
		{
			return "-1";
		}
		return itemToProduce[0];
	}

	public virtual bool doesFarmerHaveIngredientsInInventory(IList<Item> extraToCheck = null)
	{
		foreach (KeyValuePair<string, int> recipe in recipeList)
		{
			int value = recipe.Value;
			value -= Game1.player.getItemCount(recipe.Key);
			if (value <= 0)
			{
				continue;
			}
			if (extraToCheck != null)
			{
				value -= Game1.player.getItemCountInList(extraToCheck, recipe.Key);
				if (value <= 0)
				{
					continue;
				}
			}
			return false;
		}
		return true;
	}

	public virtual void drawMenuView(SpriteBatch b, int x, int y, float layerDepth = 0.88f, bool shadow = true)
	{
		ParsedItemData itemData = GetItemData(useFirst: true);
		Texture2D texture = itemData.GetTexture();
		Rectangle sourceRect = itemData.GetSourceRect();
		if (StardewValley.Pokemon.PokemonItemManager.TryGetCustomTexture(itemData.QualifiedItemId, out Texture2D customTex, out Rectangle customRect))
		{
			texture = customTex;
			sourceRect = customRect;
		}
		Utility.drawWithShadow(b, texture, new Vector2(x, y), sourceRect, Color.White, 0f, Vector2.Zero, 4f, flipped: false, layerDepth);
	}

	/// <summary>Get the item data to produce when this recipe is crafted.</summary>
	/// <param name="useFirst">If this recipe has multiple possible outputs, whether to use the first one instead of a random one.</param>
	public virtual ParsedItemData GetItemData(bool useFirst = false)
	{
		string itemId = (useFirst ? itemToProduce.FirstOrDefault() : Game1.random.ChooseFrom(itemToProduce));
		if (bigCraftable)
		{
			itemId = ItemRegistry.ManuallyQualifyItemId(itemId, "(BC)");
		}
		return ItemRegistry.GetDataOrErrorItem(itemId);
	}

	public virtual Item createItem()
	{
		Item item = ItemRegistry.Create(GetItemData().QualifiedItemId, numberProducedPerCraft);
		if (isCookingRecipe && item is Object obj && Game1.player.team.SpecialOrderRuleActive("QI_COOKING"))
		{
			obj.orderData.Value = "QI_COOKING";
			obj.MarkContextTagsDirty();
		}
		return item;
	}

	/// <summary>Try to parse the skill level requirement from the raw recipe data entry, if applicable.</summary>
	/// <param name="id">The recipe ID.</param>
	/// <param name="rawData">The raw recipe data from <see cref="F:StardewValley.CraftingRecipe.cookingRecipes" /> or <see cref="F:StardewValley.CraftingRecipe.craftingRecipes" />.</param>
	/// <param name="isCooking">Whether this is a cooking recipe (true) or crafting recipe (false).</param>
	/// <param name="skillNumber">The skill number required to learn this recipe (as returned by <see cref="M:StardewValley.Farmer.getSkillNumberFromName(System.String)" />).</param>
	/// <param name="minLevel">The minimum level of the skill needed to learn the recipe.</param>
	/// <param name="logErrors">Whether to log a warning if the recipe has an invalid skill level condition.</param>
	/// <returns>Returns whether the recipe has a skill requirement which was successfully parsed.</returns>
	public static bool TryParseLevelRequirement(string id, string rawData, bool isCooking, out int skillNumber, out int minLevel, bool logErrors = true)
	{
		int index = (isCooking ? 3 : 4);
		string conditions = ArgUtility.Get(rawData?.Split('/'), index);
		string[] array = conditions?.Split(' ');
		int num = 1;
		switch (ArgUtility.Get(array, 0)?.ToLower())
		{
		case "s":
		{
			if (ArgUtility.TryGet(array, num, out var value, out var error, allowBlank: true, "string skillId") && ArgUtility.TryGetInt(array, num + 1, out minLevel, out error, "minLevel"))
			{
				skillNumber = Farmer.getSkillNumberFromName(value);
				if (skillNumber > -1)
				{
					return true;
				}
				LogFormatWarning("no skill found matching ID '" + value + "'.");
			}
			else
			{
				LogFormatWarning(error);
			}
			break;
		}
		case "farming":
		case "fishing":
		case "combat":
		case "mining":
		case "foraging":
		case "luck":
			num = 0;
			goto case "s";
		}
		skillNumber = -1;
		minLevel = -1;
		return false;
		void LogFormatWarning(string value2)
		{
			Game1.log.Warn($"{(isCooking ? "Cooking" : "Crafting")} recipe '{id}' has invalid skill level condition '{conditions}': {value2}");
		}
	}

	public static bool isThereSpecialIngredientRule(Item potentialIngredient, string requiredIngredient)
	{
		if (requiredIngredient == (-777).ToString() && (potentialIngredient.QualifiedItemId == "(O)495" || potentialIngredient.QualifiedItemId == "(O)496" || potentialIngredient.QualifiedItemId == "(O)497" || potentialIngredient.QualifiedItemId == "(O)498"))
		{
			return true;
		}
		return false;
	}

	public virtual void consumeIngredients(List<IInventory> additionalMaterials)
	{
		foreach (KeyValuePair<string, int> recipe in recipeList)
		{
			string key = recipe.Key;
			int num = recipe.Value;
			bool flag = false;
			for (int num2 = Game1.player.Items.Count - 1; num2 >= 0; num2--)
			{
				if (ItemMatchesForCrafting(Game1.player.Items[num2], key))
				{
					int amount = num;
					num -= Game1.player.Items[num2].Stack;
					Game1.player.Items[num2] = Game1.player.Items[num2].ConsumeStack(amount);
					if (num <= 0)
					{
						flag = true;
						break;
					}
				}
			}
			if (additionalMaterials == null || flag)
			{
				continue;
			}
			for (int i = 0; i < additionalMaterials.Count; i++)
			{
				IInventory inventory = additionalMaterials[i];
				if (inventory == null)
				{
					continue;
				}
				bool flag2 = false;
				for (int num3 = inventory.Count - 1; num3 >= 0; num3--)
				{
					if (ItemMatchesForCrafting(inventory[num3], key))
					{
						int num4 = Math.Min(num, inventory[num3].Stack);
						num -= num4;
						inventory[num3] = inventory[num3].ConsumeStack(num4);
						if (inventory[num3] == null)
						{
							flag2 = true;
						}
						if (num <= 0)
						{
							break;
						}
					}
				}
				if (flag2)
				{
					inventory.RemoveEmptySlots();
				}
				if (num <= 0)
				{
					break;
				}
			}
		}
	}

	public static bool DoesFarmerHaveAdditionalIngredientsInInventory(List<KeyValuePair<string, int>> additional_recipe_items, IList<Item> extraToCheck = null)
	{
		foreach (KeyValuePair<string, int> additional_recipe_item in additional_recipe_items)
		{
			int value = additional_recipe_item.Value;
			value -= Game1.player.getItemCount(additional_recipe_item.Key);
			if (value <= 0)
			{
				continue;
			}
			if (extraToCheck != null)
			{
				value -= Game1.player.getItemCountInList(extraToCheck, additional_recipe_item.Key);
				if (value <= 0)
				{
					continue;
				}
			}
			return false;
		}
		return true;
	}

	public static bool ItemMatchesForCrafting(Item item, string item_id)
	{
		if (item == null)
		{
			return false;
		}
		if (item.Category.ToString() == item_id)
		{
			return true;
		}
		if (isThereSpecialIngredientRule(item, item_id))
		{
			return true;
		}
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(item_id);
		if (item.QualifiedItemId == dataOrErrorItem.QualifiedItemId)
		{
			return true;
		}
		return false;
	}

	public static void ConsumeAdditionalIngredients(List<KeyValuePair<string, int>> additionalRecipeItems, List<IInventory> additionalMaterials)
	{
		for (int num = additionalRecipeItems.Count - 1; num >= 0; num--)
		{
			string key = additionalRecipeItems[num].Key;
			int num2 = additionalRecipeItems[num].Value;
			bool flag = false;
			for (int num3 = Game1.player.Items.Count - 1; num3 >= 0; num3--)
			{
				Item item = Game1.player.Items[num3];
				if (ItemMatchesForCrafting(item, key))
				{
					int num4 = Math.Min(num2, item.Stack);
					num2 -= num4;
					Game1.player.Items[num3] = item.ConsumeStack(num4);
					if (num2 <= 0)
					{
						flag = true;
						break;
					}
				}
			}
			if (additionalMaterials != null && !flag)
			{
				for (int i = 0; i < additionalMaterials.Count; i++)
				{
					IInventory inventory = additionalMaterials[i];
					if (inventory == null)
					{
						continue;
					}
					bool flag2 = false;
					for (int num5 = inventory.Count - 1; num5 >= 0; num5--)
					{
						Item item2 = inventory[num5];
						if (ItemMatchesForCrafting(item2, key))
						{
							int num6 = Math.Min(num2, item2.Stack);
							num2 -= num6;
							inventory[num5] = item2.ConsumeStack(num6);
							if (inventory[num5] == null)
							{
								flag2 = true;
							}
							if (num2 <= 0)
							{
								break;
							}
						}
					}
					if (flag2)
					{
						inventory.RemoveEmptySlots();
					}
					if (num2 <= 0)
					{
						break;
					}
				}
			}
		}
	}

	public virtual int getCraftableCount(IList<Chest> additional_material_chests)
	{
		List<Item> list = new List<Item>();
		if (additional_material_chests != null)
		{
			for (int i = 0; i < additional_material_chests.Count; i++)
			{
				list.AddRange(additional_material_chests[i].Items);
			}
		}
		return getCraftableCount(list);
	}

	public virtual int getCraftableCount(IList<Item> additional_materials)
	{
		int num = -1;
		foreach (KeyValuePair<string, int> recipe in recipeList)
		{
			int num2 = 0;
			string text = recipe.Key;
			int value = recipe.Value;
			if (!text.StartsWith("(") && !text.StartsWith("-"))
			{
				text = "(O)" + text;
			}
			for (int num3 = Game1.player.Items.Count - 1; num3 >= 0; num3--)
			{
				if (Game1.player.Items[num3] is Object obj && (obj.QualifiedItemId == text || obj.Category.ToString() == text || isThereSpecialIngredientRule(obj, text)))
				{
					num2 += obj.Stack;
				}
			}
			if (additional_materials != null)
			{
				for (int i = 0; i < additional_materials.Count; i++)
				{
					if (additional_materials[i] is Object obj2 && (obj2.QualifiedItemId == text || obj2.Category.ToString() == text || isThereSpecialIngredientRule(obj2, text)))
					{
						num2 += obj2.Stack;
					}
				}
			}
			int num4 = num2 / value;
			if (num4 < num || num == -1)
			{
				num = num4;
			}
		}
		return num;
	}

	public virtual string getCraftCountText()
	{
		int value2;
		if (isCookingRecipe)
		{
			if (Game1.player.recipesCooked.TryGetValue(getIndexOfMenuView(), out var value) && value > 0)
			{
				return Game1.content.LoadString("Strings\\UI:Collections_Description_RecipesCooked", value);
			}
		}
		else if (Game1.player.craftingRecipes.TryGetValue(name, out value2) && value2 > 0)
		{
			return Game1.content.LoadString("Strings\\UI:Crafting_NumberCrafted", value2);
		}
		return null;
	}

	public virtual int getDescriptionHeight(int width)
	{
		return (int)(Game1.smallFont.MeasureString(Game1.parseText(description, Game1.smallFont, width)).Y + (float)(getNumberOfIngredients() * 36) + (float)(int)Game1.smallFont.MeasureString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.567")).Y + 21f);
	}

	public virtual void drawRecipeDescription(SpriteBatch b, Vector2 position, int width, IList<Item> additional_crafting_items)
	{
		int num = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko) ? 8 : 0);
		b.Draw(Game1.staminaRect, new Rectangle((int)(position.X + 8f), (int)(position.Y + 32f + Game1.smallFont.MeasureString("Ing!").Y) - 4 - 2 - (int)((float)num * 1.5f), width - 32, 2), Game1.textColor * 0.35f);
		Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.567"), Game1.smallFont, position + new Vector2(8f, 28f), Game1.textColor * 0.75f);
		int num2 = -1;
		foreach (KeyValuePair<string, int> recipe in recipeList)
		{
			num2++;
			int value = recipe.Value;
			string key = recipe.Key;
			int itemCount = Game1.player.getItemCount(key);
			int num3 = 0;
			int num4 = value - itemCount;
			if (additional_crafting_items != null)
			{
				num3 = Game1.player.getItemCountInList(additional_crafting_items, key);
				if (num4 > 0)
				{
					num4 -= num3;
				}
			}
			string nameFromIndex = getNameFromIndex(key);
			Color color = ((num4 <= 0) ? Game1.textColor : Color.Red);
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(getSpriteIndexFromRawIndex(key));
			Texture2D texture = dataOrErrorItem.GetTexture();
			Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
			if (StardewValley.Pokemon.PokemonItemManager.TryGetCustomTexture(key, out Texture2D customTex, out Rectangle customRect))
			{
				texture = customTex;
				sourceRect = customRect;
			}
			float num5 = 2f;
			if (sourceRect.Width > 0 || sourceRect.Height > 0)
			{
				num5 *= 16f / (float)Math.Max(sourceRect.Width, sourceRect.Height);
			}
			b.Draw(texture, new Vector2(position.X + 16f, position.Y + 64f + (float)(num2 * 64 / 2) + (float)(num2 * 4) + 16f), sourceRect, Color.White, 0f, new Vector2(sourceRect.Width / 2, sourceRect.Height / 2), num5, SpriteEffects.None, 0.86f);
			Utility.drawTinyDigits(value, b, new Vector2(position.X + 32f - Game1.tinyFont.MeasureString(value.ToString() ?? "").X, position.Y + 64f + (float)(num2 * 64 / 2) + (float)(num2 * 4) + 21f), 2f, 0.87f, Color.AntiqueWhite);
			Vector2 vector = new Vector2(position.X + 32f + 8f, position.Y + 64f + (float)(num2 * 64 / 2) + (float)(num2 * 4) + 4f);
			Utility.drawTextWithShadow(b, nameFromIndex, Game1.smallFont, vector, color);
			if (Game1.options.showAdvancedCraftingInformation)
			{
				vector.X = position.X + (float)width - 40f;
				b.Draw(Game1.mouseCursors, new Rectangle((int)vector.X, (int)vector.Y + 2, 22, 26), new Rectangle(268, 1436, 11, 13), Color.White);
				Utility.drawTextWithShadow(b, (itemCount + num3).ToString() ?? "", Game1.smallFont, vector - new Vector2(Game1.smallFont.MeasureString(itemCount + num3 + " ").X, 0f), color);
			}
		}
		b.Draw(Game1.staminaRect, new Rectangle((int)position.X + 8, (int)position.Y + num + 64 + 4 + recipeList.Count * 36, width - 32, 2), Game1.textColor * 0.35f);
		Utility.drawTextWithShadow(b, Game1.parseText(description, Game1.smallFont, width - 8), Game1.smallFont, position + new Vector2(0f, 76 + recipeList.Count * 36 + num), Game1.textColor * 0.75f);
	}

	public virtual int getNumberOfIngredients()
	{
		return recipeList.Count;
	}

	public virtual string getSpriteIndexFromRawIndex(string item_id)
	{
		switch (item_id)
		{
		case "-1":
			return "(O)20";
		case "-2":
			return "(O)80";
		case "-3":
			return "(O)24";
		case "-4":
			return "(O)145";
		case "-5":
			return "(O)176";
		case "-6":
			return "(O)184";
		default:
			if (item_id == (-777).ToString())
			{
				return "(O)495";
			}
			return item_id;
		}
	}

	public virtual string getNameFromIndex(string item_id)
	{
		if (item_id != null && item_id.StartsWith('-'))
		{
			switch (item_id)
			{
			case "-1":
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.568");
			case "-2":
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.569");
			case "-3":
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.570");
			case "-4":
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.571");
			case "-5":
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.572");
			case "-6":
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.573");
			default:
				if (item_id == (-777).ToString())
				{
					return Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.574");
				}
				return "???";
			}
		}
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(item_id);
		if (dataOrErrorItem != null)
		{
			return dataOrErrorItem.DisplayName;
		}
		return ItemRegistry.GetErrorItemName();
	}

	/// <summary>Log a message indicating the underlying crafting data is invalid.</summary>
	/// <param name="rawData">The raw data being parsed.</param>
	/// <param name="message">The error message indicating why parsing failed.</param>
	private void LogParseError(string rawData, string message)
	{
		Game1.log.Error($"Failed parsing raw recipe data '{rawData}' for {(isCookingRecipe ? "cooking" : "crafting")} recipe '{name}': {message}");
	}
}
