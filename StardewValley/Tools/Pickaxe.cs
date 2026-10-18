using System;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Extensions;

namespace StardewValley.Tools;

public class Pickaxe : Tool
{
	public const int hitMargin = 8;

	public const int BoulderStrength = 4;

	private int boulderTileX;

	private int boulderTileY;

	private int hitsToBoulder;

	public NetInt additionalPower = new NetInt(0);

	public Pickaxe()
		: base("Pickaxe", 0, 105, 131, stackable: false)
	{
	}

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
			base.ItemId = "Pickaxe";
			break;
		case 1:
			base.ItemId = "CopperPickaxe";
			break;
		case 2:
			base.ItemId = "SteelPickaxe";
			break;
		case 3:
			base.ItemId = "GoldPickaxe";
			break;
		case 4:
			base.ItemId = "IridiumPickaxe";
			break;
		default:
			base.ItemId = "Pickaxe";
			break;
		}
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Pickaxe();
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is Pickaxe pickaxe)
		{
			additionalPower.Value = pickaxe.additionalPower.Value;
		}
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
		power = who.toolPower.Value;
		if (!isEfficient.Value)
		{
			who.Stamina -= (float)(2 * (power + 1)) - (float)who.MiningLevel * 0.1f;
		}
		Utility.clampToTile(new Vector2(x, y));
		int num = x / 64;
		int num2 = y / 64;
		Vector2 vector = new Vector2(num, num2);
		if (location.performToolAction(this, num, num2))
		{
			return;
		}
		location.Objects.TryGetValue(vector, out var value);
		if (value == null)
		{
			if (who.FacingDirection == 0 || who.FacingDirection == 2)
			{
				num = (x - 8) / 64;
				location.Objects.TryGetValue(new Vector2(num, num2), out value);
				if (value == null)
				{
					num = (x + 8) / 64;
					location.Objects.TryGetValue(new Vector2(num, num2), out value);
				}
			}
			else
			{
				num2 = (y + 8) / 64;
				location.Objects.TryGetValue(new Vector2(num, num2), out value);
				if (value == null)
				{
					num2 = (y - 8) / 64;
					location.Objects.TryGetValue(new Vector2(num, num2), out value);
				}
			}
			x = num * 64;
			y = num2 * 64;
			if (location.terrainFeatures.TryGetValue(vector, out var value2) && value2.performToolAction(this, 0, vector))
			{
				location.terrainFeatures.Remove(vector);
			}
		}
		vector = new Vector2(num, num2);
		if (value != null)
		{
			if (value.IsBreakableStone())
			{
				if (PlayUseSounds)
				{
					location.playSound("hammer", vector);
				}
				if (value.MinutesUntilReady > 0)
				{
					int num3 = Math.Max(1, upgradeLevel.Value + 1) + additionalPower.Value;
					value.minutesUntilReady.Value -= num3;
					value.shakeTimer = 200;
					if (value.MinutesUntilReady > 0)
					{
						Game1.createRadialDebris(Game1.currentLocation, 14, num, num2, Game1.random.Next(2, 5), resource: false);
						return;
					}
				}
				TemporaryAnimatedSprite temporaryAnimatedSprite = ((ItemRegistry.GetDataOrErrorItem(value.QualifiedItemId).TextureName == "Maps\\springobjects" && value.ParentSheetIndex < 200 && !Game1.objectData.ContainsKey((value.ParentSheetIndex + 1).ToString()) && value.QualifiedItemId != "(O)25") ? new TemporaryAnimatedSprite(value.ParentSheetIndex + 1, 300f, 1, 2, new Vector2(x - x % 64, y - y % 64), flicker: true, value.flipped.Value)
				{
					alphaFade = 0.01f
				} : new TemporaryAnimatedSprite(47, new Vector2(num * 64, num2 * 64), Color.Gray, 10, flipped: false, 80f));
				Game1.multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
				Game1.createRadialDebris(location, 14, num, num2, Game1.random.Next(2, 5), resource: false);
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(46, new Vector2(num * 64, num2 * 64), Color.White, 10, flipped: false, 80f)
				{
					motion = new Vector2(0f, -0.6f),
					acceleration = new Vector2(0f, 0.002f),
					alphaFade = 0.015f
				});
				location.OnStoneDestroyed(value.ItemId, num, num2, getLastFarmerToUse());
				if (who != null && who.stats.Get("Book_Diamonds") != 0 && Game1.random.NextDouble() < 0.0066)
				{
					Game1.createObjectDebris("(O)72", num, num2, who.UniqueMultiplayerID, location);
					if (who.professions.Contains(19) && Game1.random.NextBool())
					{
						Game1.createObjectDebris("(O)72", num, num2, who.UniqueMultiplayerID, location);
					}
				}
				if (value.MinutesUntilReady <= 0)
				{
					value.performRemoveAction();
					location.Objects.Remove(new Vector2(num, num2));
					if (PlayUseSounds)
					{
						location.playSound("stoneCrack", vector);
					}
					Game1.stats.RocksCrushed++;
				}
			}
			else if (value.Name.Contains("Boulder"))
			{
				if (PlayUseSounds)
				{
					location.playSound("hammer", vector);
				}
				if (base.UpgradeLevel < 2)
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Pickaxe.cs.14194")));
					return;
				}
				if (num == boulderTileX && num2 == boulderTileY)
				{
					hitsToBoulder += power + 1;
					value.shakeTimer = 190;
				}
				else
				{
					hitsToBoulder = 0;
					boulderTileX = num;
					boulderTileY = num2;
				}
				if (hitsToBoulder >= 4)
				{
					location.removeObject(vector, showDestroyedObject: false);
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(5, new Vector2(64f * vector.X - 32f, 64f * (vector.Y - 1f)), Color.Gray, 8, Game1.random.NextBool(), 50f)
					{
						delayBeforeAnimationStart = 0
					});
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(5, new Vector2(64f * vector.X + 32f, 64f * (vector.Y - 1f)), Color.Gray, 8, Game1.random.NextBool(), 50f)
					{
						delayBeforeAnimationStart = 200
					});
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(5, new Vector2(64f * vector.X, 64f * (vector.Y - 1f) - 32f), Color.Gray, 8, Game1.random.NextBool(), 50f)
					{
						delayBeforeAnimationStart = 400
					});
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(5, new Vector2(64f * vector.X, 64f * vector.Y - 32f), Color.Gray, 8, Game1.random.NextBool(), 50f)
					{
						delayBeforeAnimationStart = 600
					});
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(25, new Vector2(64f * vector.X, 64f * vector.Y), Color.White, 8, Game1.random.NextBool(), 50f, 0, -1, -1f, 128));
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(25, new Vector2(64f * vector.X + 32f, 64f * vector.Y), Color.White, 8, Game1.random.NextBool(), 50f, 0, -1, -1f, 128)
					{
						delayBeforeAnimationStart = 250
					});
					Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(25, new Vector2(64f * vector.X - 32f, 64f * vector.Y), Color.White, 8, Game1.random.NextBool(), 50f, 0, -1, -1f, 128)
					{
						delayBeforeAnimationStart = 500
					});
					if (PlayUseSounds)
					{
						location.playSound("boulderBreak", vector);
					}
				}
			}
			else if (value.performToolAction(this))
			{
				value.performRemoveAction();
				if (value.Type == "Crafting" && value.fragility.Value != 2)
				{
					Game1.currentLocation.debris.Add(new Debris(value.QualifiedItemId, who.GetToolLocation(), Utility.PointToVector2(who.StandingPixel)));
				}
				Game1.currentLocation.Objects.Remove(vector);
			}
		}
		else
		{
			if (PlayUseSounds)
			{
				location.playSound("woodyHit", vector);
			}
			if (location.doesTileHaveProperty(num, num2, "Diggable", "Back") != null)
			{
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(12, new Vector2(num * 64, num2 * 64), Color.White, 8, flipped: false, 80f)
				{
					alphaFade = 0.015f
				});
			}
		}
	}
}
