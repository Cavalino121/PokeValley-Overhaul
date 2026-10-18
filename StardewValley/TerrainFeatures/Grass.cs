using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Netcode.Validation;
using StardewValley.Extensions;
using StardewValley.Tools;

namespace StardewValley.TerrainFeatures;

[XmlInclude(typeof(CosmeticPlant))]
[NotImplicitNetField]
public class Grass : TerrainFeature
{
	public const float defaultShakeRate = (float)Math.PI / 80f;

	public const float maximumShake = (float)Math.PI / 8f;

	public const float shakeDecayRate = (float)Math.PI / 350f;

	public const byte springGrass = 1;

	public const byte caveGrass = 2;

	public const byte frostGrass = 3;

	public const byte lavaGrass = 4;

	public const byte caveGrass2 = 5;

	public const byte cobweb = 6;

	public const byte blueGrass = 7;

	/// <summary>The backing field for <see cref="M:StardewValley.TerrainFeatures.Grass.PlayGrassSound" />.</summary>
	public static ICue grassSound;

	[XmlElement("grassType")]
	public readonly NetByte grassType = new NetByte();

	private bool shakeLeft;

	protected float shakeRotation;

	protected float maxShake;

	protected float shakeRate;

	[XmlElement("numberOfWeeds")]
	public readonly NetInt numberOfWeeds = new NetInt();

	[XmlElement("grassSourceOffset")]
	public readonly NetInt grassSourceOffset = new NetInt();

	private int grassBladeHealth = 1;

	[XmlIgnore]
	public Lazy<Texture2D> texture;

	private int[] whichWeed = new int[4];

	private int[] offset1 = new int[4];

	private int[] offset2 = new int[4];

	private int[] offset3 = new int[4];

	private int[] offset4 = new int[4];

	private bool[] flip = new bool[4];

	private double[] shakeRandom = new double[4];

	public Grass()
		: base(needsTick: true)
	{
		texture = new Lazy<Texture2D>(() => Game1.content.Load<Texture2D>(textureName()));
	}

	public Grass(int which, int numberOfWeeds)
		: this()
	{
		grassType.Value = (byte)which;
		loadSprite();
		this.numberOfWeeds.Value = numberOfWeeds;
	}

	public override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(grassType, "grassType").AddField(numberOfWeeds, "numberOfWeeds").AddField(grassSourceOffset, "grassSourceOffset");
	}

	/// <summary>Play the sound of walking through grass, if it's not already playing.</summary>
	public static void PlayGrassSound()
	{
		ICue cue = grassSound;
		if (cue == null || !cue.IsPlaying)
		{
			Game1.playSound("grassyStep", out grassSound);
		}
	}

	public virtual string textureName()
	{
		return "TerrainFeatures\\grass";
	}

	/// <inheritdoc />
	public override bool isPassable(Character c = null)
	{
		return true;
	}

	public override void loadSprite()
	{
		try
		{
			switch (grassType.Value)
			{
			case 1:
				switch (Game1.GetSeasonForLocation(Location))
				{
				case Season.Spring:
					grassSourceOffset.Value = 0;
					break;
				case Season.Summer:
					grassSourceOffset.Value = 20;
					break;
				case Season.Fall:
					grassSourceOffset.Value = 40;
					break;
				case Season.Winter:
					grassSourceOffset.Value = ((Location != null && Location.IsOutdoors) ? 80 : 0);
					break;
				}
				break;
			case 2:
				grassSourceOffset.Value = 60;
				break;
			case 3:
				grassSourceOffset.Value = 80;
				break;
			case 4:
				grassSourceOffset.Value = 100;
				break;
			case 7:
				switch (Game1.GetSeasonForLocation(Location))
				{
				case Season.Spring:
					grassSourceOffset.Value = 160;
					break;
				case Season.Summer:
					grassSourceOffset.Value = 180;
					break;
				case Season.Fall:
					grassSourceOffset.Value = 200;
					break;
				case Season.Winter:
					grassSourceOffset.Value = ((Location != null && Location.IsOutdoors) ? 220 : 160);
					break;
				}
				break;
			default:
				grassSourceOffset.Value = (grassType.Value + 1) * 20;
				break;
			}
		}
		catch
		{
		}
	}

	public override void OnAddedToLocation(GameLocation location, Vector2 tile)
	{
		base.OnAddedToLocation(location, tile);
		loadSprite();
	}

	public override Rectangle getBoundingBox()
	{
		Vector2 tile = Tile;
		return new Rectangle((int)(tile.X * 64f), (int)(tile.Y * 64f), 64, 64);
	}

	public override Rectangle getRenderBounds()
	{
		Vector2 tile = Tile;
		return new Rectangle((int)(tile.X * 64f) - 32, (int)(tile.Y * 64f) - 32, 128, 112);
	}

	public override void doCollisionAction(Rectangle positionOfCollider, int speedOfCollision, Vector2 tileLocation, Character who)
	{
		GameLocation location = Location;
		if (location != Game1.currentLocation)
		{
			return;
		}
		if (speedOfCollision > 0 && maxShake == 0f && positionOfCollider.Intersects(getBoundingBox()))
		{
			if (!(who is FarmAnimal) && Utility.isOnScreen(new Point((int)tileLocation.X, (int)tileLocation.Y), 2, location))
			{
				PlayGrassSound();
			}
			shake((float)Math.PI / 8f / Math.Min(1f, 5f / (float)speedOfCollision), (float)Math.PI / 80f / Math.Min(1f, 5f / (float)speedOfCollision), (float)positionOfCollider.Center.X > tileLocation.X * 64f + 32f);
		}
		if (who is Farmer && Game1.player.CurrentTool is MeleeWeapon { isOnSpecial: not false } meleeWeapon && meleeWeapon.type.Value == 0 && Math.Abs(shakeRotation) < 0.001f && performToolAction(Game1.player.CurrentTool, -1, tileLocation))
		{
			Game1.currentLocation.terrainFeatures.Remove(tileLocation);
		}
		if (who is Farmer farmer)
		{
			if (farmer.stats.Get("Book_Grass") != 0)
			{
				farmer.temporarySpeedBuff = -0.33f;
			}
			else
			{
				farmer.temporarySpeedBuff = -1f;
			}
			if (grassType.Value == 6)
			{
				farmer.temporarySpeedBuff = -3f;
			}

			if (farmer.IsLocalPlayer)
			{
				StardewValley.Pokemon.PokemonEncounterManager.CheckStepEncounter(location, farmer);
			}
		}
	}

	private static int _lastWildEncounterTick = 0;
	private static Vector2 _lastEncounterTile = Vector2.Zero;

	private static void TriggerWildEncounter(GameLocation location)
	{
		string[] wildSpecies;
		if (location is StardewValley.Locations.MineShaft || location.Name.Contains("Cave") || location.Name.Contains("Mine"))
		{
			wildSpecies = new[] { "geodude", "onix", "machop", "aron", "diglett", "zubat", "sableye" };
		}
		else if (location is StardewValley.Locations.Beach || location.Name.Contains("Beach"))
		{
			wildSpecies = new[] { "magikarp", "krabby", "psyduck", "squirtle", "wingull" };
		}
		else if (location is StardewValley.Locations.Forest || location.Name.Contains("Woods") || location.Name.Contains("Forest"))
		{
			wildSpecies = new[] { "bulbasaur", "oddish", "bellsprout", "caterpie", "weedle", "shroomish", "heracross", "scyther", "treecko" };
		}
		else
		{
			wildSpecies = new[] { "pidgey", "rattata", "pikachu", "eevee", "zigzagoon", "poochyena", "bulbasaur", "oddish" };
		}

		string chosen = wildSpecies[Game1.random.Next(wildSpecies.Length)];
		int level = Math.Clamp(Game1.random.Next(3, 10) + (Game1.year - 1) * 2, 3, 50);
		var species = StardewValley.Pokemon.PokemonSpecies.Get(chosen);
		if (species != null)
		{
			var wildInstance = new StardewValley.Pokemon.PokemonInstance(species, level);
			Game1.playSound("dwoop");
			Game1.activeClickableMenu = new StardewValley.Pokemon.PokemonBattleMenu(wildInstance, isWild: true);
		}
	}

	public bool reduceBy(int number, bool showDebris)
	{
		int num = 0;
		grassBladeHealth -= number;
		if (grassBladeHealth > 0)
		{
			return true;
		}
		if (grassType.Value == 7)
		{
			num = 1 + grassBladeHealth / -2;
			grassBladeHealth = 2 - grassBladeHealth % 2;
		}
		else
		{
			grassBladeHealth = 1;
			num = number;
		}
		numberOfWeeds.Value -= num;
		if (showDebris)
		{
			Vector2 tile = Tile;
			Game1.createRadialDebris(Game1.currentLocation, textureName(), new Rectangle(2, 8 + grassSourceOffset.Value, 8, 8), 1, (int)((tile.X + 1f) * 64f), ((int)tile.Y + 1) * 64, Game1.random.Next(2, 5), (int)tile.Y + 1, Color.White, 4f);
			Game1.createRadialDebris(Game1.currentLocation, textureName(), new Rectangle(2, 8 + grassSourceOffset.Value, 8, 8), 1, (int)((tile.X + 1.1f) * 64f), (int)((tile.Y + 1.1f) * 64f), Game1.random.Next(2, 5), (int)tile.Y + 1, Color.White, 4f);
			Game1.createRadialDebris(Game1.currentLocation, textureName(), new Rectangle(2, 8 + grassSourceOffset.Value, 8, 8), 1, (int)((tile.X + 0.9f) * 64f), (int)((tile.Y + 1.1f) * 64f), Game1.random.Next(2, 5), (int)tile.Y + 1, Color.White, 4f);
			createDestroySprites(Game1.currentLocation, tile);
		}
		return numberOfWeeds.Value <= 0;
	}

	protected void shake(float shake, float rate, bool left)
	{
		maxShake = shake;
		shakeRate = rate;
		shakeRotation = 0f;
		shakeLeft = left;
		base.NeedsUpdate = true;
	}

	public override void performPlayerEntryAction()
	{
		base.performPlayerEntryAction();
		if (shakeRandom[0] == 0.0)
		{
			setUpRandom();
		}
	}

	public override bool tickUpdate(GameTime time)
	{
		if (shakeRandom[0] == 0.0)
		{
			setUpRandom();
		}
		if (maxShake > 0f)
		{
			if (shakeLeft)
			{
				shakeRotation -= shakeRate;
				if (Math.Abs(shakeRotation) >= maxShake)
				{
					shakeLeft = false;
				}
			}
			else
			{
				shakeRotation += shakeRate;
				if (shakeRotation >= maxShake)
				{
					shakeLeft = true;
					shakeRotation -= shakeRate;
				}
			}
			maxShake = Math.Max(0f, maxShake - (float)Math.PI / 350f);
		}
		else
		{
			shakeRotation /= 2f;
			if (shakeRotation <= 0.01f)
			{
				base.NeedsUpdate = false;
				shakeRotation = 0f;
			}
		}
		return false;
	}

	public override void dayUpdate()
	{
		GameLocation location = Location;
		if ((grassType.Value == 1 || grassType.Value == 7) && (location.GetSeason() != Season.Winter || location.HasMapPropertyWithValue("AllowGrassGrowInWinter")) && numberOfWeeds.Value < 4)
		{
			numberOfWeeds.Value = Utility.Clamp(numberOfWeeds.Value + Game1.random.Next(1, 4), 0, 4);
		}
		setUpRandom();
		if (grassType.Value == 7)
		{
			grassBladeHealth = 2;
		}
		else
		{
			grassBladeHealth = 1;
		}
	}

	public void setUpRandom()
	{
		Vector2 tile = Tile;
		Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)Game1.stats.DaysPlayed / 28.0, (double)tile.X * 7.0, (double)tile.Y * 11.0);
		bool flag = Location?.hasTileAt((int)tile.X, (int)tile.Y, "Front") ?? false;
		for (int i = 0; i < 4; i++)
		{
			whichWeed[i] = random.Next(3);
			offset1[i] = random.Next(-2, 3);
			offset2[i] = random.Next(-2, 3) + (flag ? (-7) : 0);
			offset3[i] = random.Next(-2, 3);
			offset4[i] = random.Next(-2, 3) + (flag ? (-7) : 0);
			flip[i] = random.NextBool();
			shakeRandom[i] = random.NextDouble();
		}
	}

	/// <inheritdoc />
	public override bool seasonUpdate(bool onLoad)
	{
		if (grassType.Value == 1 || grassType.Value == 7)
		{
			if (Location.IsOutdoors && Location.IsWinterHere() && Location.HasMapPropertyWithValue("AllowGrassSurviveInWinter") && Location.getMapProperty("AllowGrassSurviveInWinter").StartsWithIgnoreCase("f") && !onLoad)
			{
				return true;
			}
			loadSprite();
		}
		return false;
	}

	public override bool performToolAction(Tool t, int explosion, Vector2 tileLocation)
	{
		GameLocation gameLocation = Location ?? Game1.currentLocation;
		MeleeWeapon meleeWeapon = t as MeleeWeapon;
		if ((meleeWeapon != null && meleeWeapon.type.Value != 2) || explosion > 0)
		{
			if (meleeWeapon != null && meleeWeapon.type.Value != 1)
			{
				DelayedAction.playSoundAfterDelay("daggerswipe", 50, gameLocation, tileLocation);
			}
			else
			{
				gameLocation.playSound("swordswipe", tileLocation);
			}
			shake((float)Math.PI * 3f / 32f, (float)Math.PI / 40f, Game1.random.NextBool());
			int num = ((explosion <= 0) ? 1 : Math.Max(1, explosion + 2 - Game1.recentMultiplayerRandom.Next(2)));
			if (meleeWeapon != null && t.ItemId == "53")
			{
				num = 2;
			}
			else if (meleeWeapon != null && t.ItemId == "66")
			{
				num = 4;
			}
			if (grassType.Value == 6 && Game1.random.NextBool())
			{
				num = 0;
			}
			numberOfWeeds.Value -= num;
			createDestroySprites(gameLocation, tileLocation);
			return TryDropItemsOnCut(t);
		}
		return false;
	}

	private void createDestroySprites(GameLocation location, Vector2 tileLocation)
	{
		Color color;
		switch (grassType.Value)
		{
		case 1:
			color = location.GetSeason() switch
			{
				Season.Spring => new Color(60, 180, 58), 
				Season.Summer => new Color(110, 190, 24), 
				Season.Fall => new Color(219, 102, 58), 
				Season.Winter => new Color(63, 167, 156), 
				_ => Color.Green, 
			};
			break;
		case 2:
			color = new Color(148, 146, 71);
			break;
		case 3:
			color = new Color(216, 240, 255);
			break;
		case 4:
			color = new Color(165, 93, 58);
			break;
		case 6:
			color = Color.White * 0.6f;
			break;
		case 7:
			switch (location.GetSeason())
			{
			case Season.Spring:
			case Season.Summer:
				color = new Color(0, 178, 174);
				break;
			case Season.Fall:
				color = new Color(129, 80, 148);
				break;
			case Season.Winter:
				color = new Color(40, 125, 178);
				break;
			default:
				color = Color.Green;
				break;
			}
			break;
		default:
			color = Color.Green;
			break;
		}
		Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(28, tileLocation * 64f + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-16, 16)), color, 8, Game1.random.NextBool(), Game1.random.Next(60, 100)));
	}

	/// <summary>Drop an item when this grass is cut, if any.</summary>
	/// <param name="tool">The tool used to cut the grass.</param>
	/// <param name="addAnimation">Whether to show animations for the cut grass.</param>
	public bool TryDropItemsOnCut(Tool tool, bool addAnimation = true)
	{
		Vector2 tile = Tile;
		GameLocation location = Location;
		if (numberOfWeeds.Value <= 0)
		{
			if (grassType.Value != 1 && grassType.Value != 7)
			{
				Random random = (Game1.IsMultiplayer ? Game1.recentMultiplayerRandom : Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)tile.X * 1000.0, (double)tile.Y * 11.0, Game1.CurrentMineLevel, Game1.player.timesReachedMineBottom));
				if (random.NextDouble() < 0.005)
				{
					Game1.createObjectDebris("(O)114", (int)tile.X, (int)tile.Y, -1, 0, 1f, location);
				}
				else if (random.NextDouble() < 0.01)
				{
					Game1.createDebris(4, (int)tile.X, (int)tile.Y, random.Next(1, 2), location);
				}
				else if (random.NextDouble() < 0.02)
				{
					Game1.createObjectDebris("(O)92", (int)tile.X, (int)tile.Y, random.Next(2, 4), location);
				}
			}
			else if (tool != null && tool.isScythe())
			{
				Farmer farmer = tool.getLastFarmerToUse() ?? Game1.player;
				Random obj = (Game1.IsMultiplayer ? Game1.recentMultiplayerRandom : Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)tile.X * 1000.0, (double)tile.Y * 11.0));
				double num = ((tool.ItemId == "66") ? 1.0 : ((tool.ItemId == "53") ? 0.75 : 0.5));
				if (farmer.currentLocation.IsWinterHere())
				{
					num *= 0.33;
				}
				if (obj.NextDouble() < num)
				{
					int count = ((grassType.Value != 7) ? 1 : 2);
					if (GameLocation.StoreHayInAnySilo(count, Location) == 0)
					{
						if (addAnimation)
						{
							TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite("Maps\\springobjects", Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 178, 16, 16), 750f, 1, 0, farmer.Position - new Vector2(0f, 128f), flicker: false, flipped: false, farmer.Position.Y / 10000f, 0.005f, Color.White, 4f, -0.005f, 0f, 0f);
							temporaryAnimatedSprite.motion.Y = -3f + (float)Game1.random.Next(-10, 11) / 100f;
							temporaryAnimatedSprite.acceleration.Y = 0.07f + (float)Game1.random.Next(-10, 11) / 1000f;
							temporaryAnimatedSprite.motion.X = (float)Game1.random.Next(-20, 21) / 10f;
							temporaryAnimatedSprite.layerDepth = 1f - (float)Game1.random.Next(100) / 10000f;
							temporaryAnimatedSprite.delayBeforeAnimationStart = Game1.random.Next(150);
							Game1.multiplayer.broadcastSprites(Location, temporaryAnimatedSprite);
						}
						Game1.addHUDMessage(HUDMessage.ForItemGained(ItemRegistry.Create("(O)178"), count));
					}
				}
			}
			return true;
		}
		return false;
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 positionOnScreen, Vector2 tileLocation, float scale, float layerDepth)
	{
		Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)Game1.stats.DaysPlayed / 28.0, (double)positionOnScreen.X * 7.0, (double)positionOnScreen.Y * 11.0);
		for (int i = 0; i < numberOfWeeds.Value; i++)
		{
			int num = random.Next(3);
			spriteBatch.Draw(position: (i != 4) ? (tileLocation * 64f + new Vector2((float)(i % 2 * 64 / 2 + random.Next(-2, 2) * 4 - 4) + 30f, i / 2 * 64 / 2 + random.Next(-2, 2) * 4 + 40)) : (tileLocation * 64f + new Vector2((float)(16 + random.Next(-2, 2) * 4 - 4) + 30f, 16 + random.Next(-2, 2) * 4 + 40)), texture: texture.Value, sourceRectangle: new Rectangle(num * 15, grassSourceOffset.Value, 15, 20), color: Color.White, rotation: shakeRotation / (float)(random.NextDouble() + 1.0), origin: Vector2.Zero, scale: scale, effects: SpriteEffects.None, layerDepth: layerDepth + (32f * scale + 300f) / 20000f);
		}
	}

	public override void draw(SpriteBatch spriteBatch)
	{
		Vector2 tile = Tile;
		for (int i = 0; i < numberOfWeeds.Value; i++)
		{
			Vector2 globalPosition = ((i != 4) ? (tile * 64f + new Vector2((float)(i % 2 * 64 / 2 + offset3[i] * 4 - 4) + 30f, i / 2 * 64 / 2 + offset4[i] * 4 + 40)) : (tile * 64f + new Vector2((float)(16 + offset1[i] * 4 - 4) + 30f, 16 + offset2[i] * 4 + 40)));
			spriteBatch.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, globalPosition), new Rectangle(whichWeed[i] * 15, grassSourceOffset.Value, 15, 20), Color.White, shakeRotation / (float)(shakeRandom[i] + 1.0), new Vector2(7.5f, 17.5f), 4f, flip[i] ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (globalPosition.Y + 16f - 20f) / 10000f + globalPosition.X / 10000000f);
		}
	}
}
