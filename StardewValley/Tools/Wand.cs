using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using StardewValley.Locations;

namespace StardewValley.Tools;

public class Wand : Tool
{
	public Wand()
		: base("Return Scepter", 0, 2, 2, stackable: false)
	{
		base.InstantUse = true;
	}

	/// <inheritdoc />
	protected override void MigrateLegacyItemId()
	{
		base.ItemId = "ReturnScepter";
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Wand();
	}

	public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
	{
		if (!who.bathingClothes.Value && who.IsLocalPlayer && !who.onBridge.Value)
		{
			indexOfMenuItemView.Value = 2;
			base.CurrentParentTileIndex = 2;
			for (int i = 0; i < 12; i++)
			{
				Game1.multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(354, Game1.random.Next(25, 75), 6, 1, new Vector2(Game1.random.Next((int)who.position.X - 256, (int)who.position.X + 192), Game1.random.Next((int)who.position.Y - 256, (int)who.position.Y + 192)), flicker: false, Game1.random.NextBool()));
			}
			if (PlayUseSounds)
			{
				who.playNearbySoundAll("wand");
			}
			Game1.displayFarmer = false;
			who.temporarilyInvincible = true;
			who.temporaryInvincibilityTimer = -2000;
			who.Halt();
			who.faceDirection(2);
			who.CanMove = false;
			who.freezePause = 2000;
			Game1.flashAlpha = 1f;
			DelayedAction.fadeAfterDelay(wandWarpForReal, 1000);
			Rectangle boundingBox = who.GetBoundingBox();
			new Rectangle(boundingBox.X, boundingBox.Y, 64, 64).Inflate(192, 192);
			int num = 0;
			Point tilePoint = who.TilePoint;
			for (int num2 = tilePoint.X + 8; num2 >= tilePoint.X - 8; num2--)
			{
				Game1.multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(6, new Vector2(num2, tilePoint.Y) * 64f, Color.White, 8, flipped: false, 50f)
				{
					layerDepth = 1f,
					delayBeforeAnimationStart = num * 25,
					motion = new Vector2(-0.25f, 0f)
				});
				num++;
			}
			base.CurrentParentTileIndex = base.IndexOfMenuItemView;
		}
	}

	/// <inheritdoc />
	public override bool actionWhenPurchased(string shopId)
	{
		Game1.player.mailReceived.Add("ReturnScepter");
		return base.actionWhenPurchased(shopId);
	}

	private void wandWarpForReal()
	{
		FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(Game1.player);
		if (homeOfFarmer != null)
		{
			Point frontDoorSpot = homeOfFarmer.getFrontDoorSpot();
			Game1.warpFarmer("Farm", frontDoorSpot.X, frontDoorSpot.Y, flip: false);
			Game1.fadeToBlackAlpha = 0.99f;
			Game1.screenGlow = false;
			lastUser.temporarilyInvincible = false;
			lastUser.temporaryInvincibilityTimer = 0;
			Game1.displayFarmer = true;
			lastUser.CanMove = true;
		}
	}
}
