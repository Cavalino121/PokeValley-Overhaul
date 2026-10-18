using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;

namespace StardewValley.BellsAndWhistles;

public class ParrotUpgradePerch : INetObject<NetFields>
{
	public enum UpgradeState
	{
		Idle,
		StartBuilding,
		Building,
		Complete
	}

	public class Parrot
	{
		public Vector2 position;

		public float height;

		protected ParrotUpgradePerch _perch;

		protected Vector2 targetPosition = Vector2.Zero;

		protected Vector2 startPosition = Vector2.Zero;

		public Texture2D texture;

		public bool bounced;

		public bool flipped;

		public bool isPerchedParrot;

		private int baseFrame;

		private int birdType;

		private int flapFrame;

		private float nextFlapTime;

		public float alpha;

		public float moveTime;

		public float moveDuration = 1f;

		public bool firstBounce;

		public bool flyAway;

		private bool soundBird;

		public Parrot(ParrotUpgradePerch perch, Vector2 start_position, bool soundBird = false, bool goldenParrot = false)
		{
			this.soundBird = soundBird;
			_perch = perch;
			texture = perch.texture;
			position = (start_position + new Vector2(0.5f, 0.5f)) * 64f;
			startPosition = start_position;
			height = 64f;
			birdType = Game1.random.Next(0, 4);
			if (goldenParrot)
			{
				birdType = 4;
			}
			FindNewTarget();
			firstBounce = true;
		}

		public virtual void FindNewTarget()
		{
			isPerchedParrot = false;
			firstBounce = false;
			startPosition = position;
			moveTime = 0f;
			moveDuration = 1f;
			Microsoft.Xna.Framework.Rectangle value = _perch.upgradeRect.Value;
			if (_perch.currentState.Value == UpgradeState.Complete)
			{
				flyAway = true;
				moveDuration = 5f;
				value.Inflate(5, 0);
			}
			targetPosition = (Utility.getRandomPositionInThisRectangle(value, Game1.random) + new Vector2(0.5f, 0.5f)) * 64f;
			Vector2 zero = Vector2.Zero;
			zero.X = targetPosition.X - position.X;
			zero.Y = targetPosition.Y - position.Y;
			if (zero.X < 0f)
			{
				flipped = false;
			}
			else if (zero.X > 0f)
			{
				flipped = true;
			}
			if (Math.Abs(zero.X) > Math.Abs(zero.Y))
			{
				baseFrame = 2;
			}
			else if (zero.Y > 0f)
			{
				baseFrame = 5;
			}
			else
			{
				baseFrame = 8;
			}
		}

		public virtual bool Update(GameTime time)
		{
			moveTime += (float)time.ElapsedGameTime.TotalSeconds;
			if (moveTime > moveDuration)
			{
				moveTime = moveDuration;
			}
			float num = moveTime / moveDuration;
			position.X = Utility.Lerp(startPosition.X, targetPosition.X, num);
			position.Y = Utility.Lerp(startPosition.Y, targetPosition.Y, num);
			if (isPerchedParrot)
			{
				height = EaseInOutQuad(num, 24f, -24f, 1f);
				firstBounce = false;
				birdType = 0;
			}
			else if (flyAway)
			{
				height = EaseInQuad(num, 0f, 1536f, 1f);
			}
			else if (firstBounce)
			{
				height = EaseInOutQuad(num, 64f, -64f, 1f);
			}
			else
			{
				float num2 = 24f;
				if ((double)num <= 0.5)
				{
					height = EaseInOutQuad(num, 0f, num2, 0.5f);
				}
				else
				{
					height = EaseInQuad(num - 0.5f, num2, 0f - num2, 0.5f);
				}
			}
			if (num >= 1f)
			{
				if (flyAway)
				{
					return true;
				}
				FindNewTarget();
				if (!firstBounce && _perch.upgradeName.Value != "Turtle")
				{
					_perch.locationRef.Value.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(49 + 16 * (_perch.upgradeName.Value.Contains("Volcano") ? 1 : Game1.random.Next(3)), 229, 16, 6), position, Game1.random.NextBool(), 0f, Color.White)
					{
						motion = new Vector2(Game1.random.Next(-2, 3), -12f),
						acceleration = new Vector2(0f, 0.25f),
						rotationChange = (float)Game1.random.Next(-4, 5) * 0.05f,
						scale = 4f,
						animationLength = 1,
						totalNumberOfLoops = 1,
						interval = 1000 + Game1.random.Next(500),
						layerDepth = 1f,
						drawAboveAlwaysFront = true
					});
				}
			}
			if (firstBounce)
			{
				alpha = Utility.Clamp(num / 0.25f, 0f, 1f);
			}
			else if (flyAway)
			{
				float num3 = 0.1f;
				alpha = 1f - Utility.Clamp((num - (1f - num3)) / num3, 0f, 1f);
			}
			else
			{
				alpha = 1f;
			}
			if (nextFlapTime > 0f)
			{
				nextFlapTime -= (float)time.ElapsedGameTime.TotalSeconds;
			}
			if (nextFlapTime <= 0f)
			{
				flapFrame = (flapFrame + 1) % 3;
				if (flyAway || firstBounce || height < 12f || num < 0.5f)
				{
					if (flapFrame == 0)
					{
						if (_perch.upgradeName.Value != "Hut")
						{
							if ((!flyAway || num < 0.5f) && soundBird)
							{
								_perch.locationRef.Value.localSound("parrot_flap");
							}
							if (!flyAway && !firstBounce)
							{
								if (_perch.upgradeName.Value.Contains("Volcano"))
								{
									if (Game1.random.NextDouble() < 0.15000000596046448)
									{
										_perch.locationRef.Value.localSound("hammer");
									}
								}
								else if (_perch.upgradeName.Value == "Turtle")
								{
									if (Game1.random.NextDouble() < 0.15000000596046448)
									{
										_perch.locationRef.Value.localSound("hitEnemy");
									}
								}
								else
								{
									if (Game1.random.NextDouble() < 0.05000000074505806)
									{
										_perch.locationRef.Value.localSound("axe");
									}
									if (Game1.random.NextDouble() < 0.05000000074505806)
									{
										_perch.locationRef.Value.localSound("dirtyHit");
									}
									if (Game1.random.NextDouble() < 0.05000000074505806)
									{
										_perch.locationRef.Value.localSound("crafting");
									}
								}
							}
						}
						nextFlapTime = 0.1f;
					}
					else
					{
						nextFlapTime = 0.05f;
					}
				}
				else if (flapFrame == 0)
				{
					if (soundBird)
					{
						_perch.locationRef.Value.localSound("parrot_flap");
					}
					nextFlapTime = 0.3f;
				}
				else
				{
					nextFlapTime = 0.2f;
				}
			}
			return false;
		}

		private float EaseInOutQuad(float t, float b, float c, float d)
		{
			if ((t /= d / 2f) < 1f)
			{
				return c / 2f * t * t + b;
			}
			return (0f - c) / 2f * ((t -= 1f) * (t - 2f) - 1f) + b;
		}

		private float EaseInQuad(float t, float b, float c, float d)
		{
			return c * (t /= d) * t + b;
		}

		public virtual void Draw(SpriteBatch b)
		{
			int num = baseFrame + flapFrame;
			b.Draw(texture, Game1.GlobalToLocal(Game1.viewport, Utility.snapDrawPosition(position - new Vector2(0f, height * 4f))), new Microsoft.Xna.Framework.Rectangle(num * 24, birdType * 24, 24, 24), Color.White * alpha, 0f, new Vector2(12f, 18f), 4f, flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, position.Y / 10000f);
		}
	}

	public const string GoldenParrotMailKey = "activateGoldenParrotsTonight";

	public NetEvent0 animationEvent = new NetEvent0();

	public NetMutex upgradeMutex = new NetMutex();

	public NetPoint tilePosition = new NetPoint();

	public Texture2D texture;

	public NetRectangle upgradeRect = new NetRectangle();

	public List<Parrot> parrots = new List<Parrot>();

	public NetEvent0 upgradeCompleteEvent = new NetEvent0();

	public NetEnum<UpgradeState> currentState = new NetEnum<UpgradeState>(UpgradeState.Idle);

	public float stateTimer;

	public NetInt requiredNuts = new NetInt(0);

	public float squawkTime;

	public float timeUntilChomp;

	public float timeUntilSqwawk;

	public float shakeTime;

	public float costShakeTime;

	public const int PARROT_COUNT = 24;

	public bool parrotPresent = true;

	public bool isPlayerNearby;

	public NetString upgradeName = new NetString("");

	public NetString requiredMail = new NetString("");

	public float nextParrotSpawn;

	public NetLocationRef locationRef = new NetLocationRef();

	public Action onApplyUpgrade;

	public Func<bool> onUpdateCompletionStatus;

	protected bool _cachedAvailablity;

	public NetFields NetFields { get; } = new NetFields("ParrotUpgradePerch");

	public ParrotUpgradePerch()
	{
		InitNetFields();
		texture = Game1.content.Load<Texture2D>("LooseSprites\\parrots");
	}

	public virtual void UpdateCompletionStatus()
	{
		if (onUpdateCompletionStatus != null && onUpdateCompletionStatus())
		{
			currentState.Value = UpgradeState.Complete;
		}
	}

	public virtual void InitNetFields()
	{
		NetFields.SetOwner(this).AddField(tilePosition, "tilePosition").AddField(upgradeRect, "upgradeRect")
			.AddField(currentState, "currentState")
			.AddField(upgradeMutex.NetFields, "upgradeMutex.NetFields")
			.AddField(animationEvent.NetFields, "animationEvent.NetFields")
			.AddField(upgradeCompleteEvent.NetFields, "upgradeCompleteEvent.NetFields")
			.AddField(locationRef.NetFields, "locationRef.NetFields")
			.AddField(requiredNuts, "requiredNuts")
			.AddField(upgradeName, "upgradeName")
			.AddField(requiredMail, "requiredMail");
		animationEvent.onEvent += PerformAnimation;
		upgradeCompleteEvent.onEvent += PerformCompleteAnimation;
	}

	public virtual void PerformCompleteAnimation()
	{
		if (upgradeName.Value.Contains("Volcano"))
		{
			for (int i = 0; i < 16; i++)
			{
				locationRef.Value.temporarySprites.Add(new TemporaryAnimatedSprite(5, Utility.getRandomPositionInThisRectangle(upgradeRect.Value, Game1.random) * 64f, Color.White)
				{
					motion = new Vector2(Game1.random.Next(-1, 2), -1f),
					scale = 1f,
					layerDepth = 1f,
					drawAboveAlwaysFront = true,
					delayBeforeAnimationStart = i * 15
				});
				TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(65, 229, 16, 6), Utility.getRandomPositionInThisRectangle(upgradeRect.Value, Game1.random) * 64f, Game1.random.NextBool(), 0f, Color.White)
				{
					motion = new Vector2(Game1.random.Next(-2, 3), -16f),
					acceleration = new Vector2(0f, 0.5f),
					rotationChange = (float)Game1.random.Next(-4, 5) * 0.05f,
					scale = 4f,
					animationLength = 1,
					totalNumberOfLoops = 1,
					interval = 1000 + Game1.random.Next(500),
					layerDepth = 1f,
					drawAboveAlwaysFront = true,
					yStopCoordinate = (upgradeRect.Bottom + 1) * 64
				};
				temporaryAnimatedSprite.reachedStopCoordinate = temporaryAnimatedSprite.bounce;
				locationRef.Value.TemporarySprites.Add(temporaryAnimatedSprite);
				temporaryAnimatedSprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(65, 229, 16, 6), Utility.getRandomPositionInThisRectangle(upgradeRect.Value, Game1.random) * 64f, Game1.random.NextBool(), 0f, Color.White)
				{
					motion = new Vector2(Game1.random.Next(-2, 3), -16f),
					acceleration = new Vector2(0f, 0.5f),
					rotationChange = (float)Game1.random.Next(-4, 5) * 0.05f,
					scale = 4f,
					animationLength = 1,
					totalNumberOfLoops = 1,
					interval = 1000 + Game1.random.Next(500),
					layerDepth = 1f,
					drawAboveAlwaysFront = true,
					yStopCoordinate = (upgradeRect.Bottom + 1) * 64
				};
				temporaryAnimatedSprite.reachedStopCoordinate = temporaryAnimatedSprite.bounce;
				locationRef.Value.TemporarySprites.Add(temporaryAnimatedSprite);
			}
			if (locationRef.Value == Game1.currentLocation)
			{
				Game1.flashAlpha = 1f;
				Game1.playSound("boulderBreak");
			}
		}
		else
		{
			switch (upgradeName.Value)
			{
			case "House":
			{
				for (int j = 0; j < 16; j++)
				{
					locationRef.Value.temporarySprites.Add(new TemporaryAnimatedSprite(5, Utility.getRandomPositionInThisRectangle(upgradeRect.Value, Game1.random) * 64f, Color.White)
					{
						motion = new Vector2(Game1.random.Next(-1, 2), -1f),
						scale = 1f,
						layerDepth = 1f,
						drawAboveAlwaysFront = true,
						delayBeforeAnimationStart = j * 15
					});
					TemporaryAnimatedSprite temporaryAnimatedSprite2 = new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(49 + 16 * Game1.random.Next(3), 229, 16, 6), Utility.getRandomPositionInThisRectangle(upgradeRect.Value, Game1.random) * 64f, Game1.random.NextBool(), 0f, Color.White)
					{
						motion = new Vector2(Game1.random.Next(-2, 3), -16f),
						acceleration = new Vector2(0f, 0.5f),
						rotationChange = (float)Game1.random.Next(-4, 5) * 0.05f,
						scale = 4f,
						animationLength = 1,
						totalNumberOfLoops = 1,
						interval = 1000 + Game1.random.Next(500),
						layerDepth = 1f,
						drawAboveAlwaysFront = true,
						yStopCoordinate = (upgradeRect.Bottom + 1) * 64
					};
					temporaryAnimatedSprite2.reachedStopCoordinate = temporaryAnimatedSprite2.bounce;
					locationRef.Value.TemporarySprites.Add(temporaryAnimatedSprite2);
					temporaryAnimatedSprite2 = new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(49 + 16 * Game1.random.Next(3), 229, 16, 6), Utility.getRandomPositionInThisRectangle(upgradeRect.Value, Game1.random) * 64f, Game1.random.NextBool(), 0f, Color.White)
					{
						motion = new Vector2(Game1.random.Next(-2, 3), -16f),
						acceleration = new Vector2(0f, 0.5f),
						rotationChange = (float)Game1.random.Next(-4, 5) * 0.05f,
						scale = 4f,
						animationLength = 1,
						totalNumberOfLoops = 1,
						interval = 1000 + Game1.random.Next(500),
						layerDepth = 1f,
						drawAboveAlwaysFront = true,
						yStopCoordinate = (upgradeRect.Bottom + 1) * 64
					};
					temporaryAnimatedSprite2.reachedStopCoordinate = temporaryAnimatedSprite2.bounce;
					locationRef.Value.TemporarySprites.Add(temporaryAnimatedSprite2);
				}
				if (locationRef.Value == Game1.currentLocation)
				{
					Game1.flashAlpha = 1f;
					Game1.playSound("boulderBreak");
				}
				break;
			}
			case "Resort":
			case "Trader":
			case "Obelisk":
			case "House_Mailbox":
				if (locationRef.Value == Game1.currentLocation)
				{
					Game1.flashAlpha = 1f;
				}
				break;
			}
		}
		if (locationRef.Value == Game1.currentLocation && upgradeName.Value != "Hut")
		{
			DelayedAction.playSoundAfterDelay("secret1", 800);
		}
		if (locationRef.Value == null || !(locationRef.Value is IslandLocation islandLocation))
		{
			return;
		}
		foreach (ParrotUpgradePerch parrotUpgradePerch in islandLocation.parrotUpgradePerches)
		{
			parrotUpgradePerch.resetCache();
		}
	}

	public ParrotUpgradePerch(GameLocation location, Point tile_position, Microsoft.Xna.Framework.Rectangle upgrade_rectangle, int required_nuts, Action apply_upgrade, Func<bool> update_completion_status, string upgrade_name = "", string required_mail = "")
		: this()
	{
		locationRef.Value = location;
		tilePosition.Value = tile_position;
		upgradeRect.Value = upgrade_rectangle;
		onApplyUpgrade = apply_upgrade;
		onUpdateCompletionStatus = update_completion_status;
		requiredNuts.Value = required_nuts;
		parrots = new List<Parrot>();
		UpdateCompletionStatus();
		upgradeName.Value = upgrade_name;
		if (required_mail != "")
		{
			requiredMail.Value = required_mail;
		}
	}

	public bool IsAtTile(int x, int y)
	{
		if (tilePosition.X == x && tilePosition.Y == y)
		{
			return currentState.Value == UpgradeState.Idle;
		}
		return false;
	}

	public virtual void PerformAnimation()
	{
		currentState.Value = UpgradeState.StartBuilding;
		stateTimer = 3f;
		if (Game1.currentLocation == locationRef.Value)
		{
			Game1.playSound("parrot_squawk");
			parrots.Clear();
			parrotPresent = true;
			Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("Maps\\springobjects", Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 73, 16, 16), 2000f, 1, 0, (Utility.PointToVector2(tilePosition.Value) + new Vector2(0.25f, -2.5f)) * 64f, flicker: false, flipped: false, (float)(tilePosition.Y * 64 + 1) / 10000f, 0f, Color.White, 3f, -0.015f, 0f, 0f)
			{
				motion = new Vector2(-0.1f, -7f),
				acceleration = new Vector2(0f, 0.25f),
				id = 98765,
				drawAboveAlwaysFront = true
			});
			Game1.playSound("dwop");
			if (upgradeMutex.IsLockHeld())
			{
				Game1.player.freezePause = ((upgradeName.Value != "Hut") ? 10000 : 3000);
			}
			timeUntilChomp = 1f;
			squawkTime = 1f;
		}
	}

	public virtual bool IsAvailable(bool use_cached_value = false)
	{
		if (use_cached_value && Game1.currentLocation == locationRef.Value)
		{
			return _cachedAvailablity;
		}
		if (requiredMail.Value == "")
		{
			return true;
		}
		string[] array = requiredMail.Value.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string id = array[i].Trim();
			if (!Game1.MasterPlayer.hasOrWillReceiveMail(id))
			{
				return false;
			}
		}
		return true;
	}

	public virtual void StartAnimation()
	{
		animationEvent.Fire();
	}

	public bool CheckAction(Location tile_location, Farmer farmer)
	{
		if (IsAtTile(tile_location.X, tile_location.Y) && IsAvailable())
		{
			string text;
			if (upgradeName.Value == "GoldenParrot")
			{
				if (Game1.netWorldState.Value.GoldenWalnutsFound >= 130)
				{
					squawkTime = 0.5f;
					shakeTime = 0.5f;
					Game1.playSound("parrot_squawk");
					return true;
				}
				text = Game1.content.LoadStringReturnNullIfNotFound("Strings\\1_6_Strings:GoldenParrot");
			}
			else
			{
				text = Game1.content.LoadStringReturnNullIfNotFound("Strings\\UI:UpgradePerch_" + upgradeName.Value);
			}
			GameLocation value = locationRef.Value;
			if (text != null && value != null)
			{
				text = string.Format(text, requiredNuts.Value);
				costShakeTime = 0.5f;
				squawkTime = 0.5f;
				shakeTime = 0.5f;
				if (locationRef.Value == Game1.currentLocation)
				{
					Game1.playSound("parrot_squawk");
				}
				if (upgradeName.Value == "GoldenParrot")
				{
					if (Game1.MasterPlayer.hasOrWillReceiveMail("activateGoldenParrotsTonight"))
					{
						Game1.playSound("parrot_squawk");
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:GoldenParrot_Tonight"));
						return true;
					}
					int number = (130 - Game1.netWorldState.Value.GoldenWalnutsFound) * 10000;
					value.createQuestionDialogue(text, new Response[2]
					{
						new Response("Yes", string.Format(Game1.content.LoadString("Strings\\1_6_Strings:GoldenParrot_Yes"), Utility.getNumberWithCommas(number))).SetHotKey(Keys.Y),
						new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")).SetHotKey(Keys.Escape)
					}, "GoldenParrot");
				}
				else if (Game1.netWorldState.Value.GoldenWalnuts >= requiredNuts.Value)
				{
					value.createQuestionDialogue(text, value.createYesNoResponses(), "UpgradePerch_" + upgradeName.Value);
				}
				else
				{
					Game1.drawDialogueNoTyping(text);
				}
			}
			else if (Game1.netWorldState.Value.GoldenWalnuts >= requiredNuts.Value)
			{
				AttemptConstruction();
			}
			else
			{
				ShowInsufficientNuts();
			}
			return true;
		}
		return false;
	}

	public virtual bool AnswerQuestion(Response answer)
	{
		if (Game1.currentLocation.lastQuestionKey != null)
		{
			string text = ArgUtility.SplitBySpace(Game1.currentLocation.lastQuestionKey)[0] + "_" + answer.responseKey;
			if (text == "UpgradePerch_" + upgradeName.Value + "_Yes")
			{
				AttemptConstruction();
				return true;
			}
			if (!(text == "GoldenParrot_Yes"))
			{
				if (text == "GoldenParrotConfirm_Yes")
				{
					int num = (130 - Game1.netWorldState.Value.GoldenWalnutsFound) * 10000;
					if (Game1.player.Money < num)
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney3"));
					}
					else
					{
						Game1.player.Money -= num;
						Game1.multiplayer.broadcastPartyWideMail("activateGoldenParrotsTonight", Multiplayer.PartyWideMessageQueue.MailForTomorrow, no_letter: true);
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:GoldenParrot_Tonight"));
						Game1.playSound("parrot_squawk");
					}
					return true;
				}
			}
			else
			{
				int num2 = (130 - Game1.netWorldState.Value.GoldenWalnutsFound) * 10000;
				if (Game1.player.Money >= num2)
				{
					locationRef.Value.createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:GoldenParrot_Sure"), new Response[2]
					{
						new Response("Yes", string.Format(Game1.content.LoadString("Strings\\1_6_Strings:GoldenParrot_Yes"), Utility.getNumberWithCommas(num2))).SetHotKey(Keys.Y),
						new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")).SetHotKey(Keys.Escape)
					}, "GoldenParrotConfirm");
					return true;
				}
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney3"));
			}
		}
		return false;
	}

	public static void ActivateGoldenParrot()
	{
		string[] array = new string[67]
		{
			"Bush_IslandNorth_13_33", "Bush_IslandNorth_5_30", "Buried_IslandNorth_19_39", "Bush_IslandNorth_4_42", "Bush_IslandNorth_45_38", "Bush_IslandNorth_47_40", "IslandLeftPlantRestored", "IslandRightPlantRestored", "IslandBatRestored", "IslandFrogRestored",
			"IslandCenterSkeletonRestored", "IslandSnakeRestored", "Buried_IslandNorth_19_13", "Buried_IslandNorth_57_79", "Buried_IslandNorth_54_21", "Buried_IslandNorth_42_77", "Buried_IslandNorth_62_54", "Buried_IslandNorth_26_81", "Bush_IslandNorth_20_26", "Bush_IslandNorth_9_84",
			"Bush_IslandNorth_56_27", "Bush_IslandSouth_31_5", "TreeNut", "IslandWestCavePuzzle", "SandDuggy", "TigerSlimeNut", "Buried_IslandWest_21_81", "Buried_IslandWest_62_76", "Buried_IslandWest_39_24", "Buried_IslandWest_88_14",
			"Buried_IslandWest_43_74", "Buried_IslandWest_30_75", "MusselStone", "IslandFarming", "Bush_IslandWest_104_3", "Bush_IslandWest_31_24", "Bush_IslandWest_38_56", "Bush_IslandWest_75_29", "Bush_IslandWest_64_30", "Bush_IslandWest_54_18",
			"Bush_IslandWest_25_30", "Bush_IslandWest_15_3", "IslandFishing", "VolcanoNormalChest", "VolcanoRareChest", "VolcanoBarrel", "VolcanoMining", "VolcanoMonsterDrop", "Island_N_BuriedTreasureNut", "Island_W_BuriedTreasureNut",
			"Island_W_BuriedTreasureNut2", "Mermaid", "TreeNutShot", "Buried_IslandSouthEastCave_36_26", "Buried_IslandSouthEast_25_17", "StardropPool", "Bush_Caldera_28_36", "Bush_Caldera_9_34", "Bush_CaptainRoom_2_4", "BananaShrine",
			"Bush_IslandEast_17_37", "Darts", "IslandGourmand1", "IslandGourmand2", "IslandGourmand3", "IslandShrinePuzzle", "Bush_IslandShrine_23_34"
		};
		foreach (string text in array)
		{
			Game1.player.team.limitedNutDrops[text] = 9999;
			Game1.player.team.collectedNutTracker.Add(text);
			Game1.netWorldState.Value.FoundBuriedNuts.Add(text.Replace("Buried_", ""));
		}
		int num = 130 - Game1.netWorldState.Value.GoldenWalnutsFound;
		Game1.netWorldState.Value.GoldenWalnutsFound = 130;
		Game1.netWorldState.Value.GoldenWalnuts += num;
		Game1.netWorldState.Value.GoldenCoconutCracked = true;
		Game1.netWorldState.Value.ActivatedGoldenParrot = true;
		foreach (GameLocation location in Game1.locations)
		{
			if (location is IslandLocation)
			{
				foreach (LargeTerrainFeature largeTerrainFeature in location.largeTerrainFeatures)
				{
					if (largeTerrainFeature is Bush bush)
					{
						bush.tileSheetOffset.Value = 0;
						bush.setUpSourceRect();
					}
				}
			}
			if (!(location is IslandNorth islandNorth))
			{
				if (!(location is IslandWestCave1 islandWestCave))
				{
					if (!(location is IslandEast islandEast))
					{
						if (!(location is IslandHut islandHut))
						{
							if (!(location is IslandSouthEast islandSouthEast))
							{
								if (!(location is IslandShrine islandShrine))
								{
									if (!(location is IslandWest islandWest))
									{
										if (location is IslandFarmCave islandFarmCave)
										{
											islandFarmCave.gourmandRequestsFulfilled.Value = 3;
										}
									}
									else
									{
										islandWest.sandDuggy.Value.whacked.Value = true;
									}
								}
								else
								{
									islandShrine.puzzleFinished.Value = true;
								}
							}
							else
							{
								islandSouthEast.mermaidPuzzleFinished.Value = true;
								islandSouthEast.fishedWalnut.Value = true;
							}
						}
						else
						{
							islandHut.treeNutObtained.Value = true;
						}
					}
					else
					{
						islandEast.bananaShrineComplete.Value = true;
					}
				}
				else
				{
					islandWestCave.completed.Value = true;
				}
				continue;
			}
			islandNorth.treeNutShot.Value = true;
			foreach (ParrotUpgradePerch parrotUpgradePerch in islandNorth.parrotUpgradePerches)
			{
				if (parrotUpgradePerch.upgradeName.Value == "GoldenParrot")
				{
					parrotUpgradePerch.currentState.Value = UpgradeState.Complete;
				}
			}
		}
	}

	public virtual void AttemptConstruction()
	{
		Game1.player.Halt();
		Game1.player.canMove = false;
		upgradeMutex.RequestLock(delegate
		{
			Game1.player.canMove = true;
			if (Game1.netWorldState.Value.GoldenWalnuts >= requiredNuts.Value)
			{
				if (Game1.content.LoadStringReturnNullIfNotFound("Strings\\UI:Chat_UpgradePerch_" + upgradeName.Value) != null)
				{
					Game1.multiplayer.globalChatInfoMessage("UpgradePerch_" + upgradeName.Value, Game1.player.Name);
				}
				Game1.netWorldState.Value.GoldenWalnuts -= requiredNuts.Value;
				StartAnimation();
			}
			else
			{
				ShowInsufficientNuts();
			}
		}, delegate
		{
			Game1.player.canMove = true;
		});
	}

	public virtual void ShowInsufficientNuts()
	{
		if (IsAvailable(use_cached_value: true))
		{
			costShakeTime = 0.5f;
			squawkTime = 0.5f;
			shakeTime = 0.5f;
			if (locationRef.Value == Game1.currentLocation)
			{
				Game1.playSound("parrot_squawk");
			}
		}
	}

	public virtual void ApplyUpgrade()
	{
		onApplyUpgrade?.Invoke();
	}

	public virtual void Cleanup()
	{
		if (upgradeMutex.IsLockHeld())
		{
			upgradeMutex.ReleaseLock();
		}
		if (isPlayerNearby)
		{
			isPlayerNearby = false;
		}
	}

	public virtual void ResetForPlayerEntry()
	{
		resetCache();
	}

	public void resetCache()
	{
		_cachedAvailablity = IsAvailable();
		parrotPresent = currentState.Value == UpgradeState.Idle;
	}

	public virtual void UpdateEvenIfFarmerIsntHere(GameTime time)
	{
		animationEvent.Poll();
		upgradeCompleteEvent.Poll();
		upgradeMutex.Update(locationRef.Value);
		if (!Game1.IsMasterGame)
		{
			return;
		}
		if (stateTimer > 0f)
		{
			stateTimer -= (float)time.ElapsedGameTime.TotalSeconds;
		}
		if (currentState.Value == UpgradeState.StartBuilding && stateTimer <= 0f)
		{
			currentState.Value = UpgradeState.Building;
			stateTimer = 5f;
			if (upgradeName.Value == "Hut")
			{
				stateTimer = 0.1f;
			}
		}
		if (currentState.Value == UpgradeState.Building && stateTimer <= 0f)
		{
			ApplyUpgrade();
			currentState.Value = UpgradeState.Complete;
			upgradeMutex.ReleaseLock();
			upgradeCompleteEvent.Fire();
		}
	}

	public virtual void Update(GameTime time)
	{
		if (squawkTime > 0f)
		{
			squawkTime -= (float)time.ElapsedGameTime.TotalSeconds;
		}
		if (shakeTime > 0f)
		{
			shakeTime -= (float)time.ElapsedGameTime.TotalSeconds;
		}
		if (costShakeTime > 0f)
		{
			costShakeTime -= (float)time.ElapsedGameTime.TotalSeconds;
		}
		if (timeUntilChomp > 0f)
		{
			timeUntilChomp -= (float)time.ElapsedGameTime.TotalSeconds;
			if (timeUntilChomp <= 0f)
			{
				if (locationRef.Value == Game1.currentLocation)
				{
					Game1.playSound("eat");
				}
				timeUntilChomp = 0f;
				shakeTime = 0.25f;
				if (locationRef.Value.getTemporarySpriteByID(98765) != null)
				{
					for (int i = 0; i < 6; i++)
					{
						locationRef.Value.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(9, 252, 3, 3), locationRef.Value.getTemporarySpriteByID(98765).position + new Vector2(8f, 8f) * 4f, Game1.random.NextBool(), 0f, Color.White)
						{
							motion = new Vector2(Game1.random.Next(-1, 2), -6f),
							acceleration = new Vector2(0f, 0.25f),
							rotationChange = (float)Game1.random.Next(-4, 5) * 0.05f,
							scale = 4f,
							animationLength = 1,
							totalNumberOfLoops = 1,
							interval = 500 + Game1.random.Next(500),
							layerDepth = 1f,
							drawAboveAlwaysFront = true
						});
					}
				}
				locationRef.Value.removeTemporarySpritesWithID(98765);
				timeUntilSqwawk = 1f;
			}
		}
		if (timeUntilSqwawk > 0f)
		{
			timeUntilSqwawk -= (float)time.ElapsedGameTime.TotalSeconds;
			if (timeUntilSqwawk <= 0f)
			{
				timeUntilSqwawk = 0f;
				if (locationRef.Value == Game1.currentLocation)
				{
					Game1.playSound("parrot_squawk");
				}
				squawkTime = 0.5f;
				shakeTime = 0.5f;
			}
		}
		if (parrotPresent && currentState.Value > UpgradeState.StartBuilding)
		{
			if (currentState.Value == UpgradeState.Building)
			{
				Parrot parrot = new Parrot(this, Utility.PointToVector2(tilePosition.Value));
				parrot.isPerchedParrot = true;
				parrots.Add(parrot);
			}
			parrotPresent = false;
		}
		if (IsAvailable(use_cached_value: true))
		{
			bool flag = currentState.Value == UpgradeState.Idle && !upgradeMutex.IsLocked() && Math.Abs(Game1.player.TilePoint.X - tilePosition.X) <= 1 && Math.Abs(Game1.player.TilePoint.Y - tilePosition.Y) <= 1;
			if (flag != isPlayerNearby)
			{
				isPlayerNearby = flag;
				if (isPlayerNearby)
				{
					if (locationRef.Value == Game1.currentLocation)
					{
						Game1.playSound("parrot_squawk");
					}
					squawkTime = 0.5f;
					shakeTime = 0.5f;
					costShakeTime = 0.5f;
					Game1.specialCurrencyDisplay.ShowCurrency("walnuts", () => isPlayerNearby, 0f);
				}
			}
		}
		if (currentState.Value == UpgradeState.Building && parrots.Count < 24)
		{
			if (nextParrotSpawn > 0f)
			{
				nextParrotSpawn -= (float)time.ElapsedGameTime.TotalSeconds;
			}
			if (nextParrotSpawn <= 0f)
			{
				nextParrotSpawn = 0.05f;
				Microsoft.Xna.Framework.Rectangle value = upgradeRect.Value;
				value.Inflate(5, 0);
				parrots.Add(new Parrot(this, Utility.getRandomPositionInThisRectangle(value, Game1.random), parrots.Count % 10 == 0));
			}
		}
		parrots.RemoveAll((Parrot parrot2) => parrot2.Update(time));
	}

	public virtual void Draw(SpriteBatch b)
	{
		if (IsAvailable(use_cached_value: true) && (parrotPresent || upgradeName.Value == "Hut"))
		{
			int num = 0;
			Vector2 zero = Vector2.Zero;
			if (squawkTime > 0f)
			{
				num = 1;
			}
			if (shakeTime > 0f)
			{
				zero.X = Utility.RandomFloat(-0.5f, 0.5f) * 4f;
				zero.Y = Utility.RandomFloat(-0.5f, 0.5f) * 4f;
			}
			b.Draw(texture, Game1.GlobalToLocal(Game1.viewport, (Utility.PointToVector2(tilePosition.Value) + new Vector2(0.5f, -1f)) * 64f) + zero, new Microsoft.Xna.Framework.Rectangle(num * 24, (upgradeName.Value == "GoldenParrot") ? 96 : 0, 24, 24), Color.White, 0f, new Vector2(12f, 16f), 4f, SpriteEffects.None, (((float)tilePosition.Y + 1f) * 64f - 1f) / 10000f);
		}
	}

	public virtual void DrawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		if (parrotPresent && IsAvailable(use_cached_value: true) && isPlayerNearby)
		{
			if (upgradeName.Value == "GoldenParrot" && Game1.MasterPlayer.hasOrWillReceiveMail("activateGoldenParrotsTonight"))
			{
				return;
			}
			Vector2 zero = Vector2.Zero;
			if (costShakeTime > 0f)
			{
				zero.X = Utility.RandomFloat(-0.5f, 0.5f) * 4f;
				zero.Y = Utility.RandomFloat(-0.5f, 0.5f) * 4f;
			}
			float num = 2f * (float)Math.Round(Math.Sin(Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds / 250.0), 2);
			Vector2 vector = Utility.PointToVector2(tilePosition.Value);
			float num2 = vector.Y * 64f / 10000f;
			num -= 72f;
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(vector.X * 64f, vector.Y * 64f - 96f - 48f + num)) + zero, new Microsoft.Xna.Framework.Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, num2 + 1E-06f);
			Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(vector.X * 64f + 32f + 8f, vector.Y * 64f - 64f - 32f - 8f + num)) + zero;
			if (upgradeName.Value == "GoldenParrot")
			{
				b.Draw(Game1.mouseCursors_1_6, vector2, new Microsoft.Xna.Framework.Rectangle(131, 0, 16, 16), Color.White, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, num2 + 1E-05f);
			}
			else
			{
				b.Draw(Game1.objectSpriteSheet, vector2, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 73, 16, 16), Color.White, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, num2 + 1E-05f);
				Utility.drawTinyDigits(requiredNuts.Value, b, vector2 + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(requiredNuts.Value, 3f)) + 3f - 32f, 16f), 3f, 1f, Color.White);
			}
		}
		foreach (Parrot parrot in parrots)
		{
			parrot.Draw(b);
		}
	}
}
