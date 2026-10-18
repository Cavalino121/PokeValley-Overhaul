using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;

namespace StardewValley.Tools;

public class Axe : Tool
{
	public NetInt additionalPower = new NetInt(0);

	public Axe()
		: base("Axe", 0, 189, 215, stackable: false)
	{
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(additionalPower, "additionalPower");
	}

	/// <inheritdoc />
	protected override void MigrateLegacyItemId()
	{
		switch (base.UpgradeLevel)
		{
		case 0:
			base.ItemId = "Axe";
			break;
		case 1:
			base.ItemId = "CopperAxe";
			break;
		case 2:
			base.ItemId = "SteelAxe";
			break;
		case 3:
			base.ItemId = "GoldAxe";
			break;
		case 4:
			base.ItemId = "IridiumAxe";
			break;
		default:
			base.ItemId = "Axe";
			break;
		}
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Axe();
	}

	public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
	{
		Update(who.FacingDirection, 0, who);
		who.EndUsingTool();
		return true;
	}

	public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
	{
		base.DoFunction(location, x, y, power, who);
		if (!isEfficient.Value)
		{
			who.Stamina -= (float)(2 * power) - (float)who.ForagingLevel * 0.1f;
		}
		int num = x / 64;
		int num2 = y / 64;
		Rectangle tileRect = new Rectangle(num * 64, num2 * 64, 64, 64);
		Vector2 tile = new Vector2(num, num2);
		if (location.Map.RequireLayer("Buildings").Tiles[num, num2] != null && location.Map.RequireLayer("Buildings").Tiles[num, num2].TileIndexProperties.ContainsKey("TreeStump"))
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Axe.cs.14023"));
			return;
		}
		upgradeLevel.Value += additionalPower.Value;
		location.performToolAction(this, num, num2);
		if (location.terrainFeatures.TryGetValue(tile, out var value) && value.performToolAction(this, 0, tile))
		{
			location.terrainFeatures.Remove(tile);
		}
		location.largeTerrainFeatures?.RemoveWhere((LargeTerrainFeature largeFeature) => largeFeature.getBoundingBox().Intersects(tileRect) && largeFeature.performToolAction(this, 0, tile));
		Vector2 key = new Vector2(num, num2);
		if (location.Objects.TryGetValue(key, out var value2) && value2.Type != null && value2.performToolAction(this))
		{
			if (value2.Type == "Crafting" && value2.fragility.Value != 2)
			{
				location.debris.Add(new Debris(value2.QualifiedItemId, who.GetToolLocation(), Utility.PointToVector2(who.StandingPixel)));
			}
			value2.performRemoveAction();
			location.Objects.Remove(key);
		}
		upgradeLevel.Value -= additionalPower.Value;
	}
}
