using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.GameData.Pets;
using StardewValley.Locations;
using StardewValley.Menus;

namespace StardewValley.Objects;

public class PetLicense : Object
{
	/// <summary>The delimiter between the pet ID and breed ID in the <see cref="P:StardewValley.Object.Name" /> field.</summary>
	public const char Delimiter = '|';

	public PetLicense()
		: base("PetLicense", 1)
	{
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
		AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
		if (drawShadow && !bigCraftable.Value && base.QualifiedItemId != "(O)590" && base.QualifiedItemId != "(O)SeedSpot")
		{
			spriteBatch.Draw(Game1.shadowTexture, location + new Vector2(32f, 48f), Game1.shadowTexture.Bounds, color * 0.5f, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f, SpriteEffects.None, layerDepth - 0.0001f);
		}
		ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId);
		float num = scaleSize;
		if (bigCraftable.Value && num > 0.2f)
		{
			num /= 2f;
		}
		string[] array = Name.Split('|');
		if (Game1.petData.TryGetValue(array[0], out var value))
		{
			PetBreed breedById = value.GetBreedById(array[1]);
			if (breedById != null)
			{
				Rectangle iconSourceRect = breedById.IconSourceRect;
				spriteBatch.Draw(Game1.content.Load<Texture2D>(breedById.IconTexture), location + new Vector2(32f, 32f), iconSourceRect, color * transparency, 0f, new Vector2(iconSourceRect.Width / 2, iconSourceRect.Height / 2), 4f * num, SpriteEffects.None, layerDepth);
			}
		}
		DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
	}

	public override bool actionWhenPurchased(string shopId)
	{
		Game1.exitActiveMenu();
		string title = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1236");
		Game1.activeClickableMenu = new NamingMenu(namePet, title, Dialogue.randomName());
		Game1.playSound("purchaseClick");
		return true;
	}

	private void namePet(string name)
	{
		string[] array = Name.Split('|');
		FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(Game1.player);
		Point point = new Point(3, 7);
		if (homeOfFarmer.upgradeLevel == 1)
		{
			point = new Point(9, 7);
		}
		else if (homeOfFarmer.upgradeLevel >= 2)
		{
			point = new Point(27, 26);
		}
		Pet pet = new Pet(point.X, point.Y, array[1], array[0]);
		pet.currentLocation = homeOfFarmer;
		homeOfFarmer.characters.Add(pet);
		pet.warpToFarmHouse(Game1.player);
		pet.Name = name;
		pet.displayName = pet.name.Value;
		foreach (Building building in Game1.getFarm().buildings)
		{
			if (building is PetBowl petBowl && !petBowl.HasPet())
			{
				petBowl.AssignPet(pet);
				break;
			}
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			allFarmer.autoGenerateActiveDialogueEvent("gotPet");
		}
		Game1.exitActiveMenu();
		if (Game1.currentLocation.getCharacterFromName("Marnie") != null)
		{
			Game1.DrawDialogue(Game1.currentLocation.getCharacterFromName("Marnie"), "Strings\\1_6_Strings:AdoptedPet_Marnie", name);
		}
		else
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:AdoptedPet", name));
		}
	}
}
