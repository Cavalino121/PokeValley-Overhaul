using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Characters;
using StardewValley.GameData.Pets;
using StardewValley.GameData.Shops;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Network.NetEvents;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.SpecialOrders;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using StardewValley.Triggers;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley;

public class Event
{
	/// <summary>The low-level event commands defined by the base game. Most code should use <see cref="T:StardewValley.Event" /> methods instead.</summary>
	/// <remarks>Every method within this class is an event command whose name matches the method name. All event commands must be static, public, and match <see cref="T:StardewValley.Delegates.EventCommandDelegate" />.</remarks>
	public static class DefaultCommands
	{
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void IgnoreEventTileOffset(Event @event, string[] args, EventContext context)
		{
			@event.ignoreTileOffsets = true;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Move(Event @event, string[] args, EventContext context)
		{
			bool? flag = null;
			int num = (args.Length - 1) % 4;
			if (num == 1)
			{
				if (!ArgUtility.TryGetOptionalBool(args, args.Length - 1, out var value, out var error, defaultValue: false, "bool rawValue"))
				{
					context.LogErrorAndSkip(error);
					return;
				}
				flag = value;
			}
			else if (num > 1)
			{
				context.LogErrorAndSkip("invalid number of arguments, expected sets of [actor x y direction] fields plus an optional continue-after-move boolean field");
				return;
			}
			if (!flag.HasValue || args.Length > 2)
			{
				for (int i = 1; i < args.Length && ArgUtility.HasIndex(args, i + 3); i += 4)
				{
					if (!ArgUtility.TryGet(args, i, out var value2, out var error2, allowBlank: true, "string actorName") || !ArgUtility.TryGetPoint(args, i + 1, out var value3, out error2, "Point tile") || !ArgUtility.TryGetDirection(args, i + 3, out var value4, out error2, "int facingDirection"))
					{
						context.LogError(error2);
						continue;
					}
					if (@event.IsFarmerActorId(value2, out var farmerNumber))
					{
						if (!@event.actorPositionsAfterMove.ContainsKey(value2))
						{
							Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
							if (farmerActor != null)
							{
								farmerActor.canOnlyWalk = false;
								farmerActor.setRunning(isRunning: false, force: true);
								farmerActor.canOnlyWalk = true;
								farmerActor.convertEventMotionCommandToMovement(Utility.PointToVector2(value3));
								@event.actorPositionsAfterMove.Add(value2, @event.getPositionAfterMove(farmerActor, value3.X, value3.Y, value4));
							}
						}
						continue;
					}
					bool isOptionalNpc;
					NPC actorByName = @event.getActorByName(value2, out isOptionalNpc);
					if (actorByName == null)
					{
						if (!isOptionalNpc)
						{
							context.LogErrorAndSkip("no NPC found with name '" + value2 + "'");
							return;
						}
					}
					else if (!@event.actorPositionsAfterMove.ContainsKey(actorByName.Name))
					{
						actorByName.convertEventMotionCommandToMovement(Utility.PointToVector2(value3));
						@event.actorPositionsAfterMove.Add(actorByName.Name, @event.getPositionAfterMove(actorByName, value3.X, value3.Y, value4));
					}
				}
			}
			if (!flag.HasValue)
			{
				return;
			}
			if (flag == true)
			{
				@event.continueAfterMove = true;
				@event.CurrentCommand++;
				return;
			}
			@event.continueAfterMove = false;
			if (args.Length == 2 && @event.actorPositionsAfterMove.Count == 0)
			{
				@event.CurrentCommand++;
			}
		}

		/// <summary>Run an action string.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Action(Event @event, string[] args, EventContext context)
		{
			Exception exception;
			if (!ArgUtility.TryGetRemainder(args, 1, out var value, out var error, ' ', "string action"))
			{
				context.LogErrorAndSkip(error);
			}
			else if (!TriggerActionManager.TryRunAction(value, out error, out exception))
			{
				if (exception != null)
				{
					error += $"\n{exception}";
				}
				context.LogErrorAndSkip(error);
			}
			else
			{
				@event.CurrentCommand++;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Speak(Event @event, string[] args, EventContext context)
		{
			if (@event.skipped)
			{
				return;
			}
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string textOrTranslationKey"))
			{
				context.LogErrorAndSkip(error);
			}
			else
			{
				if (Game1.dialogueUp)
				{
					return;
				}
				@event.timeAccumulator += context.Time.ElapsedGameTime.Milliseconds;
				if (@event.timeAccumulator < 500f)
				{
					return;
				}
				@event.timeAccumulator = 0f;
				bool isOptionalNpc;
				NPC n = @event.getActorByName(value, out isOptionalNpc) ?? Game1.getCharacterFromName(value.TrimEnd('?'));
				if (n == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					if (!isOptionalNpc)
					{
						Game1.eventFinished();
					}
					return;
				}
				Game1.player.NotifyQuests((Quest quest) => quest.OnNpcSocialized(n));
				if (n.CanSocialize && !Game1.player.friendshipData.ContainsKey(n.Name))
				{
					Game1.player.friendshipData.Add(n.Name, new Friendship(0));
				}
				Dialogue item = (Game1.content.IsValidTranslationKey(value2) ? new Dialogue(n, value2) : new Dialogue(n, null, value2));
				n.CurrentDialogue.Push(item);
				Game1.drawDialogue(n);
			}
		}

		/// <summary>Try to execute all commands in one tick until <see cref="M:StardewValley.Event.DefaultCommands.EndSimultaneousCommand(StardewValley.Event,System.String[],StardewValley.EventContext)" /> is called.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void BeginSimultaneousCommand(Event @event, string[] args, EventContext context)
		{
			@event.simultaneousCommand = true;
			@event.CurrentCommand++;
		}

		/// <summary>If commands are being executed in one tick due to <see cref="M:StardewValley.Event.DefaultCommands.BeginSimultaneousCommand(StardewValley.Event,System.String[],StardewValley.EventContext)" />, stop doing so for the remaining commands.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void EndSimultaneousCommand(Event @event, string[] args, EventContext context)
		{
			@event.simultaneousCommand = false;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void MineDeath(Event @event, string[] args, EventContext context)
		{
			if (!Game1.dialogueUp)
			{
				Random random = Utility.CreateDaySaveRandom(Game1.timeOfDay);
				int val = random.Next(Game1.player.Money / 40, Game1.player.Money / 8);
				val = Math.Min(val, 15000);
				val -= (int)((double)Game1.player.LuckLevel * 0.01 * (double)val);
				val -= val % 100;
				int num = Game1.player.LoseItemsOnDeath(random);
				Game1.player.Stamina = Math.Min(Game1.player.Stamina, 2f);
				Game1.player.Money = Math.Max(0, Game1.player.Money - val);
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1057") + " " + ((val <= 0) ? "" : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1058", val)) + ((num <= 0) ? ((val <= 0) ? "" : ".") : ((val <= 0) ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1060") + ((num == 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1061") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1062", num))) : (Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1063") + ((num == 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1061") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1062", num))))));
				@event.InsertNextCommand("showItemsLost");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void HospitalDeath(Event @event, string[] args, EventContext context)
		{
			if (!Game1.dialogueUp)
			{
				int num = Game1.player.LoseItemsOnDeath();
				Game1.player.Stamina = Math.Min(Game1.player.Stamina, 2f);
				int num2 = Math.Min(1000, Game1.player.Money);
				Game1.player.Money -= num2;
				Game1.drawObjectDialogue(((num2 > 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1068", num2) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1070")) + ((num > 0) ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1071") + ((num == 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1061") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1062", num))) : ""));
				@event.InsertNextCommand("showItemsLost");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ShowItemsLost(Event @event, string[] args, EventContext context)
		{
			if (Game1.activeClickableMenu == null)
			{
				Game1.activeClickableMenu = new ItemListMenu(Game1.content.LoadString("Strings\\UI:ItemList_ItemsLost"), Game1.player.itemsLostLastDeath.ToList());
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void End(Event @event, string[] args, EventContext context)
		{
			@event.endBehaviors(args, context.Location);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void LocationSpecificCommand(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string command"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			string[] args2 = args.Skip(2).ToArray();
			if (context.Location.RunLocationSpecificEventCommand(@event, value, !@event._repeatingLocationSpecificCommand, args2))
			{
				@event._repeatingLocationSpecificCommand = false;
				@event.CurrentCommand++;
			}
			else
			{
				@event._repeatingLocationSpecificCommand = true;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Unskippable(Event @event, string[] args, EventContext context)
		{
			@event.skippable = false;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Skippable(Event @event, string[] args, EventContext context)
		{
			@event.skippable = true;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void SetSkipActions(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetRemainder(args, 1, out var value, out var error, ' ', "string skipActions"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (string.IsNullOrWhiteSpace(value))
			{
				@event.actionsOnSkip = null;
			}
			else
			{
				string[] array = LegacyShims.SplitAndTrim(value, '#');
				string[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					if (!TriggerActionManager.TryValidateActionExists(array2[i], out error))
					{
						context.LogErrorAndSkip(error);
						return;
					}
				}
				@event.actionsOnSkip = array;
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Emote(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int emoteId") || !ArgUtility.TryGetOptionalBool(args, 3, out var value3, out error, defaultValue: false, "bool nextCommandImmediate"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				@event.GetFarmerActor(farmerNumber)?.doEmote(value2, !value3);
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				if (!actorByName.isEmoting)
				{
					actorByName.doEmote(value2, !value3);
				}
			}
			if (value3)
			{
				@event.CurrentCommand++;
				@event.Update(context.Location, context.Time);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void StopMusic(Event @event, string[] args, EventContext context)
		{
			Game1.changeMusicTrack("none", track_interruptable: false, MusicContext.Event);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void PlayPetSound(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string sound"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Pet pet = null;
			foreach (NPC actor in @event.actors)
			{
				if (actor is Pet)
				{
					pet = actor as Pet;
					break;
				}
			}
			if (pet == null)
			{
				pet = Game1.player.getPet();
			}
			float num = 1200f;
			if (pet != null)
			{
				PetData petData = pet.GetPetData();
				PetBreed petBreed = petData?.GetBreedById(pet.whichBreed.Value);
				if (petBreed != null)
				{
					num *= petBreed.VoicePitch;
					if (value == petData.BarkSound && petBreed.BarkOverride != null)
					{
						value = petBreed.BarkOverride;
					}
				}
			}
			Game1.playSound(value, (int)num);
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void PlaySound(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string soundId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.playSound(value, out var cue);
			@event.TrackSound(cue);
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void StopSound(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string soundId") || !ArgUtility.TryGetOptionalBool(args, 2, out var value2, out error, defaultValue: true, "bool immediate"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			@event.StopTrackedSound(value, value2);
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void TossConcession(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string concessionId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			MovieConcession concessionItem = MovieTheater.GetConcessionItem(value2);
			if (concessionItem == null)
			{
				context.LogErrorAndSkip("no concession found with ID '" + value2 + "'");
				return;
			}
			Texture2D texture = concessionItem.GetTexture();
			int spriteIndex = concessionItem.GetSpriteIndex();
			Game1.playSound("dwop");
			context.Location.temporarySprites.Add(new TemporaryAnimatedSprite
			{
				texture = texture,
				sourceRect = Game1.getSourceRectForStandardTileSheet(texture, spriteIndex, 16, 16),
				animationLength = 1,
				totalNumberOfLoops = 1,
				motion = new Vector2(0f, -6f),
				acceleration = new Vector2(0f, 0.2f),
				interval = 1000f,
				scale = 4f,
				position = @event.OffsetPosition(new Vector2(actorByName.Position.X, actorByName.Position.Y - 96f)),
				layerDepth = (float)actorByName.StandingPixel.Y / 10000f
			});
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Pause(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int pauseTime"))
			{
				context.LogErrorAndSkip(error);
			}
			else if (Game1.pauseTime <= 0f)
			{
				Game1.pauseTime = value;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void PrecisePause(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int pauseTime"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.stopWatch == null)
			{
				@event.stopWatch = new Stopwatch();
			}
			if (!@event.stopWatch.IsRunning)
			{
				@event.stopWatch.Start();
			}
			if (@event.stopWatch.ElapsedMilliseconds >= value)
			{
				@event.stopWatch.Stop();
				@event.stopWatch.Reset();
				@event.CurrentCommand++;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ResetVariable(Event @event, string[] args, EventContext context)
		{
			@event.specialEventVariable1 = false;
			@event.currentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void FaceDirection(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetDirection(args, 2, out var value2, out error, "int faceDirection") || !ArgUtility.TryGetOptionalBool(args, 3, out var value3, out error, defaultValue: false, "bool continueImmediate"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
				if (farmerActor != null)
				{
					farmerActor.FarmerSprite.StopAnimation();
					farmerActor.completelyStopAnimatingOrDoingAction();
					farmerActor.faceDirection(value2);
				}
			}
			else if (value.Contains("spouse"))
			{
				NPC actorByName = @event.getActorByName(Game1.player.spouse);
				if (actorByName != null && !Game1.player.hasRoommate())
				{
					actorByName.faceDirection(value2);
				}
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName2 = @event.getActorByName(value, out isOptionalNpc);
				if (actorByName2 == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				actorByName2.faceDirection(value2);
			}
			if (value3)
			{
				@event.CurrentCommand++;
				@event.Update(context.Location, context.Time);
			}
			else if (Game1.pauseTime <= 0f)
			{
				Game1.pauseTime = 500f;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Warp(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetVector2(args, 2, out var value2, out error, integerOnly: true, "Vector2 tile") || !ArgUtility.TryGetOptionalBool(args, 4, out var value3, out error, defaultValue: false, "bool continueImmediate"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
				if (farmerActor != null)
				{
					farmerActor.setTileLocation(@event.OffsetTile(value2));
					farmerActor.position.Y -= 16f;
					if (@event.farmerActors.Contains(farmerActor))
					{
						farmerActor.completelyStopAnimatingOrDoingAction();
					}
				}
			}
			else if (value.Contains("spouse"))
			{
				NPC actorByName = @event.getActorByName(Game1.player.spouse);
				if (actorByName != null && !Game1.player.hasRoommate())
				{
					@event.npcControllers?.RemoveAll((NPCController npcController) => npcController.puppet.Name == Game1.player.spouse);
					actorByName.Position = @event.OffsetPosition(value2 * 64f);
					actorByName.IsWalkingInSquare = false;
				}
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName2 = @event.getActorByName(value, out isOptionalNpc);
				if (actorByName2 == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				actorByName2.position.X = @event.OffsetPositionX(value2.X * 64f + 4f);
				actorByName2.position.Y = @event.OffsetPositionY(value2.Y * 64f);
				actorByName2.IsWalkingInSquare = false;
			}
			@event.CurrentCommand++;
			if (value3)
			{
				@event.Update(context.Location, context.Time);
			}
		}

		/// <summary>Change the event position for all connected farmers.</summary>
		/// <remarks>This expects at least four fields:
		///   1. zero or more [x y direction] triplets (one per possible farmer);
		///   2. an offset direction (up/down/left/right), which sets where each subsequent farmer is set when using the default triplet;
		///   3. and a default [x y direction] triplet which applies to any unlisted farmer.</remarks>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void WarpFarmers(Event @event, string[] args, EventContext context)
		{
			int num = (args.Length - 1) % 3;
			if (args.Length < 5 || num != 1)
			{
				context.LogErrorAndSkip("invalid number of arguments; expected zero or more [x y direction] triplets, one offset direction (up/down/left/right), and one triplet which applies to any other farmer");
				return;
			}
			int num2 = args.Length - 4;
			if (!ArgUtility.TryGetDirection(args, num2, out var value, out var error, "int offsetDirection") || !ArgUtility.TryGetPoint(args, num2 + 1, out var value2, out error, "Point defaultPosition") || !ArgUtility.TryGetDirection(args, num2 + 3, out var value3, out error, "int defaultFacingDirection"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			List<Vector3> list = new List<Vector3>();
			for (int i = 1; i < num2; i += 3)
			{
				if (!ArgUtility.TryGetPoint(args, i, out var value4, out error, "Point position") || !ArgUtility.TryGetDirection(args, i + 2, out var value5, out error, "int facingDirection"))
				{
					context.LogErrorAndSkip(error);
					return;
				}
				list.Add(new Vector3(value4.X, value4.Y, value5));
			}
			Point point;
			switch (value)
			{
			case 3:
				point = new Point(-1, 0);
				break;
			case 1:
				point = new Point(1, 0);
				break;
			case 0:
				point = new Point(0, -1);
				break;
			case 2:
				point = new Point(0, 1);
				break;
			default:
				context.LogErrorAndSkip($"invalid offset direction '{value}'; must be one of 'left', 'right', 'up', or 'down'");
				return;
			}
			int num3 = value2.X;
			int num4 = value2.Y;
			for (int j = 0; j < Game1.numberOfPlayers(); j++)
			{
				Farmer farmerActor = @event.GetFarmerActor(j + 1);
				float x;
				float y;
				int direction;
				if (j < list.Count)
				{
					x = list[j].X;
					y = list[j].Y;
					direction = (int)list[j].Z;
				}
				else
				{
					x = num3;
					y = num4;
					direction = value3;
					num3 += point.X;
					num4 += point.Y;
					if (context.Location.map.GetLayer("Buildings")?.Tiles[num3, num4] != null && point != Point.Zero)
					{
						num3 -= point.X;
						num4 -= point.Y;
						point = Point.Zero;
					}
				}
				if (farmerActor != null)
				{
					farmerActor.setTileLocation(@event.OffsetTile(new Vector2(x, y)));
					farmerActor.faceDirection(direction);
					farmerActor.position.Y -= 16f;
					farmerActor.completelyStopAnimatingOrDoingAction();
				}
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Speed(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int speed"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				if (@event.IsCurrentFarmerActorId(farmerNumber))
				{
					@event.farmerAddedSpeed = value2;
				}
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				actorByName.speed = value2;
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void StopAdvancedMoves(Event @event, string[] args, EventContext context)
		{
			string text = ArgUtility.Get(args, 1);
			if (text != null)
			{
				if (!(text == "next"))
				{
					context.LogErrorAndSkip("unknown option " + text + ", must be 'next' or omitted");
					return;
				}
				foreach (NPCController npcController in @event.npcControllers)
				{
					npcController.destroyAtNextCrossroad();
				}
			}
			else
			{
				@event.npcControllers.Clear();
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void DoAction(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetPoint(args, 1, out var value, out var error, "Point tile"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Location tileLocation = new Location(@event.OffsetTileX(value.X), @event.OffsetTileY(value.Y));
			Game1.hooks.OnGameLocation_CheckAction(context.Location, tileLocation, Game1.viewport, @event.farmer, () => context.Location.checkAction(tileLocation, Game1.viewport, @event.farmer));
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void RemoveTile(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetPoint(args, 1, out var value, out var error, "Point tile") || !ArgUtility.TryGet(args, 3, out var value2, out error, allowBlank: true, "string layerId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			context.Location.removeTile(@event.OffsetTileX(value.X), @event.OffsetTileY(value.Y), value2);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void TextAboveHead(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string text"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			actorByName.showTextAboveHead(value2);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ShowFrame(Event @event, string[] args, EventContext context)
		{
			bool value = false;
			string value2;
			int value3;
			string error2;
			if (args.Length == 2)
			{
				value2 = "farmer";
				if (!ArgUtility.TryGetInt(args, 1, out value3, out var error, "frame"))
				{
					context.LogErrorAndSkip(error);
					return;
				}
			}
			else if (!ArgUtility.TryGet(args, 1, out value2, out error2, allowBlank: true, "actorName") || !ArgUtility.TryGetInt(args, 2, out value3, out error2, "frame") || !ArgUtility.TryGetOptionalBool(args, 3, out value, out error2, defaultValue: false, "flip"))
			{
				context.LogErrorAndSkip(error2);
				return;
			}
			if (!@event.IsFarmerActorId(value2, out var farmerNumber))
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value2, out isOptionalNpc);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value2 + "'", isOptionalNpc);
					return;
				}
				if (value2 == "spouse" && actorByName.Gender == Gender.Male && value3 >= 36 && value3 <= 38)
				{
					value3 += 12;
				}
				actorByName.Sprite.CurrentFrame = value3;
			}
			else
			{
				Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
				if (farmerActor != null)
				{
					farmerActor.FarmerSprite.setCurrentAnimation(new FarmerSprite.AnimationFrame[1]
					{
						new FarmerSprite.AnimationFrame(value3, 100, secondaryArm: false, value)
					});
					farmerActor.FarmerSprite.loop = true;
					farmerActor.FarmerSprite.loopThisAnimation = true;
					farmerActor.FarmerSprite.PauseForSingleAnimation = true;
					farmerActor.Sprite.currentFrame = value3;
				}
			}
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void FarmerAnimation(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int animationId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			@event.farmer.FarmerSprite.setCurrentSingleAnimation(value);
			@event.farmer.FarmerSprite.PauseForSingleAnimation = true;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void IgnoreMovementAnimation(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetOptionalBool(args, 2, out var value2, out error, defaultValue: true, "bool ignore"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
				if (farmerActor != null)
				{
					farmerActor.ignoreMovementAnimation = value2;
				}
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value, out isOptionalNpc, legacyReplaceUnderscores: true);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				actorByName.ignoreMovementAnimation = value2;
			}
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Animate(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetBool(args, 2, out var value2, out error, "bool flip") || !ArgUtility.TryGetBool(args, 3, out var value3, out error, "bool loop") || !ArgUtility.TryGetInt(args, 4, out var value4, out error, "int frameDuration"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			List<FarmerSprite.AnimationFrame> list = new List<FarmerSprite.AnimationFrame>();
			for (int i = 5; i < args.Length; i++)
			{
				if (!ArgUtility.TryGetInt(args, i, out var value5, out error, "int frame"))
				{
					context.LogErrorAndSkip(error);
					return;
				}
				list.Add(new FarmerSprite.AnimationFrame(value5, value4, secondaryArm: false, value2));
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
				if (farmerActor != null)
				{
					farmerActor.FarmerSprite.setCurrentAnimation(list.ToArray());
					farmerActor.FarmerSprite.loop = true;
					farmerActor.FarmerSprite.loopThisAnimation = value3;
					farmerActor.FarmerSprite.PauseForSingleAnimation = true;
				}
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value, out isOptionalNpc, legacyReplaceUnderscores: true);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				actorByName.Sprite.setCurrentAnimation(list);
				actorByName.Sprite.loop = value3;
			}
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void StopAnimation(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetOptionalInt(args, 2, out var value2, out error, -1, "int endFrame"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
				if (farmerActor != null)
				{
					farmerActor.completelyStopAnimatingOrDoingAction();
					farmerActor.Halt();
					farmerActor.FarmerSprite.CurrentAnimation = null;
					switch (farmerActor.FacingDirection)
					{
					case 0:
						farmerActor.FarmerSprite.setCurrentSingleFrame(12, 32000);
						break;
					case 1:
						farmerActor.FarmerSprite.setCurrentSingleFrame(6, 32000);
						break;
					case 2:
						farmerActor.FarmerSprite.setCurrentSingleFrame(0, 32000);
						break;
					case 3:
						farmerActor.FarmerSprite.setCurrentSingleFrame(6, 32000, secondaryArm: false, flip: true);
						break;
					}
				}
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				actorByName.Sprite.StopAnimation();
				if (value2 > -1)
				{
					actorByName.Sprite.currentFrame = value2;
					actorByName.Sprite.UpdateSourceRect();
				}
			}
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ChangeLocation(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string locationName"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Point tilePoint = @event.farmer.TilePoint;
			@event.changeLocation(value, tilePoint.X, tilePoint.Y, delegate
			{
				Game1.currentLocation.ResetForEvent(@event);
				@event.CurrentCommand++;
			});
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Halt(Event @event, string[] args, EventContext context)
		{
			foreach (NPC actor in @event.actors)
			{
				actor.Halt();
			}
			@event.farmer.Halt();
			@event.CurrentCommand++;
			@event.continueAfterMove = false;
			@event.actorPositionsAfterMove.Clear();
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Message(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string dialogue"))
			{
				context.LogError(error);
			}
			if (!Game1.dialogueUp && Game1.activeClickableMenu == null)
			{
				Game1.drawDialogueNoTyping(Game1.parseText(value));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddCookingRecipe(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetRemainder(args, 1, out var value, out var error, ' ', "string recipeKey"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.player.cookingRecipes.TryAdd(value, 0);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ItemAboveHead(Event @event, string[] args, EventContext context)
		{
			string text = ArgUtility.Get(args, 1);
			bool showMessage = ArgUtility.GetBool(args, 2, defaultValue: true);
			switch (text?.ToLower())
			{
			case "pan":
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(T)Pan"), showMessage);
				break;
			case "hero":
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(BC)116"), showMessage);
				break;
			case "sculpture":
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(F)1306"), showMessage);
				break;
			case "samboombox":
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(F)1309"), showMessage);
				break;
			case "joja":
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(BC)117"), showMessage);
				break;
			case "slimeegg":
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(O)680"), showMessage);
				break;
			case "rod":
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(T)BambooPole"), showMessage);
				break;
			case "sword":
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(W)0"), showMessage);
				break;
			case "ore":
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(O)334"), showMessage);
				break;
			case "pot":
				showMessage = ArgUtility.GetBool(args, 2);
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(BC)62"), showMessage);
				break;
			case "jukebox":
				showMessage = ArgUtility.GetBool(args, 2);
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create("(BC)209"), showMessage);
				break;
			case null:
				@event.farmer.holdUpItemThenMessage(null, showMessage: false);
				break;
			default:
				@event.farmer.holdUpItemThenMessage(ItemRegistry.Create(text), showMessage);
				break;
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddCraftingRecipe(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetRemainder(args, 1, out var value, out var error, ' ', "string recipeKey"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.player.craftingRecipes.TryAdd(value, 0);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void HostMail(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string mailId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (Game1.IsMasterGame && !Game1.player.hasOrWillReceiveMail(value))
			{
				Game1.addMailForTomorrow(value);
			}
			@event.CurrentCommand++;
		}

		/// <summary>Add a letter to the mailbox tomorrow.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Mail(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string mailId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (!Game1.player.hasOrWillReceiveMail(value))
			{
				Game1.addMailForTomorrow(value);
			}
			@event.CurrentCommand++;
		}

		/// <summary>Add a letter to the mailbox immediately.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void MailToday(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string mailId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (!Game1.player.hasOrWillReceiveMail(value))
			{
				Game1.addMail(value);
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Shake(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int duration"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			actorByName.shake(value2);
			@event.CurrentCommand++;
		}

		/// <remarks>Main format: <c>temporaryAnimatedSprite texture rect_x rect_y rect_width rect_height animation_interval animation_length number_of_loops tile_x tile_y flicker flipped layer_depth alpha_fade scale scale_change rotation rotation_change</c>. This also supports a number of extended options (like <c>color Green</c>).</remarks>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void TemporaryAnimatedSprite(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string textureName") || !ArgUtility.TryGetRectangle(args, 2, out var value2, out error, "Rectangle sourceRect") || !ArgUtility.TryGetFloat(args, 6, out var value3, out error, "float animationInterval") || !ArgUtility.TryGetInt(args, 7, out var value4, out error, "int animationLength") || !ArgUtility.TryGetInt(args, 8, out var value5, out error, "int numberOfLoops") || !ArgUtility.TryGetVector2(args, 9, out var value6, out error, integerOnly: true, "Vector2 tile") || !ArgUtility.TryGetBool(args, 11, out var value7, out error, "bool flicker") || !ArgUtility.TryGetBool(args, 12, out var value8, out error, "bool flip") || !ArgUtility.TryGetFloat(args, 13, out var value9, out error, "float layerDepth") || !ArgUtility.TryGetFloat(args, 14, out var value10, out error, "float alphaFade") || !ArgUtility.TryGetInt(args, 15, out var value11, out error, "int scale") || !ArgUtility.TryGetFloat(args, 16, out var value12, out error, "float scaleChange") || !ArgUtility.TryGetFloat(args, 17, out var value13, out error, "float rotation") || !ArgUtility.TryGetFloat(args, 18, out var value14, out error, "float rotationChange"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(value, value2, value3, value4, value5, @event.OffsetPosition(value6 * 64f), value7, value8, @event.OffsetPosition(new Vector2(0f, value9) * 64f).Y / 10000f, value10, Color.White, 4 * value11, value12, value13, value14);
			for (int i = 19; i < args.Length; i++)
			{
				switch (args[i])
				{
				case "color":
				{
					if (!ArgUtility.TryGet(args, i + 1, out var value18, out error, allowBlank: true, "string rawColor"))
					{
						context.LogError(error);
						break;
					}
					Color? color = Utility.StringToColor(value18);
					if (color.HasValue)
					{
						temporaryAnimatedSprite.color = color.Value;
					}
					else
					{
						context.LogError($"index {i + 1} has value '{value18}', which can't be parsed as a color");
					}
					i++;
					break;
				}
				case "hold_last_frame":
					temporaryAnimatedSprite.holdLastFrame = true;
					break;
				case "ping_pong":
					temporaryAnimatedSprite.pingPong = true;
					break;
				case "motion":
				{
					if (!ArgUtility.TryGetVector2(args, i + 1, out var value16, out error, integerOnly: false, "Vector2 value"))
					{
						context.LogError(error);
						break;
					}
					temporaryAnimatedSprite.motion = value16;
					i += 2;
					break;
				}
				case "acceleration":
				{
					if (!ArgUtility.TryGetVector2(args, i + 1, out var value17, out error, integerOnly: false, "Vector2 value"))
					{
						context.LogError(error);
						break;
					}
					temporaryAnimatedSprite.acceleration = value17;
					i += 2;
					break;
				}
				case "acceleration_change":
				{
					if (!ArgUtility.TryGetVector2(args, i + 1, out var value15, out error, integerOnly: false, "Vector2 value"))
					{
						context.LogError(error);
						break;
					}
					temporaryAnimatedSprite.accelerationChange = value15;
					i += 2;
					break;
				}
				default:
					context.LogError("unknown option '" + args[i] + "'");
					break;
				}
			}
			context.Location.TemporarySprites.Add(temporaryAnimatedSprite);
			@event.CurrentCommand++;
		}

		/// <remarks>Format: <c>temporarySprite xTile yTile rowInAnimationSheet animationLength animationInterval=300 flipped=false layerDepth=-1</c>.</remarks>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void TemporarySprite(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetVector2(args, 1, out var value, out var error, integerOnly: true, "Vector2 tile") || !ArgUtility.TryGetInt(args, 3, out var value2, out error, "int rowInAnimationSheet") || !ArgUtility.TryGetInt(args, 4, out var value3, out error, "int animationLength") || !ArgUtility.TryGetOptionalFloat(args, 5, out var value4, out error, 300f, "float animationInterval") || !ArgUtility.TryGetOptionalBool(args, 6, out var value5, out error, defaultValue: false, "bool flipped") || !ArgUtility.TryGetOptionalFloat(args, 7, out var value6, out error, -1f, "float layerDepth"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			context.Location.TemporarySprites.Add(new TemporaryAnimatedSprite(value2, @event.OffsetPosition(value * 64f), Color.White, value3, value5, value4, 0, 64, value6));
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void RemoveTemporarySprites(Event @event, string[] args, EventContext context)
		{
			context.Location.TemporarySprites.Clear();
			@event.CurrentCommand++;
		}

		/// <summary>A command that does nothing. Used just to wait for another event to finish.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Null(Event @event, string[] args, EventContext context)
		{
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void SpecificTemporarySprite(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string spriteId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			@event.addSpecificTemporarySprite(value, context.Location, args);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void PlayMusic(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetRemainder(args, 1, out var value, out var error, ' ', "string musicId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (value == "samBand")
			{
				if (Game1.player.DialogueQuestionsAnswered.Contains("78"))
				{
					Game1.changeMusicTrack("shimmeringbastion", track_interruptable: false, MusicContext.Event);
				}
				else if (Game1.player.DialogueQuestionsAnswered.Contains("79"))
				{
					Game1.changeMusicTrack("honkytonky", track_interruptable: false, MusicContext.Event);
				}
				else if (Game1.player.DialogueQuestionsAnswered.Contains("77"))
				{
					Game1.changeMusicTrack("heavy", track_interruptable: false, MusicContext.Event);
				}
				else
				{
					Game1.changeMusicTrack("poppy", track_interruptable: false, MusicContext.Event);
				}
			}
			else if (Game1.options.musicVolumeLevel > 0f)
			{
				Game1.changeMusicTrack(value, track_interruptable: false, MusicContext.Event);
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void MakeInvisible(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetPoint(args, 1, out var value, out var error, "Point tile") || !ArgUtility.TryGetOptionalInt(args, 3, out var value2, out error, 1, "int width") || !ArgUtility.TryGetOptionalInt(args, 4, out var value3, out error, 1, "int height"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			GameLocation location = context.Location;
			int num = @event.OffsetTileX(value.X);
			int num2 = @event.OffsetTileY(value.Y);
			for (int i = num2; i < num2 + value3; i++)
			{
				for (int j = num; j < num + value2; j++)
				{
					Object objectAtTile = location.getObjectAtTile(j, i);
					TerrainFeature value4;
					if (objectAtTile != null)
					{
						if (objectAtTile is BedFurniture bedFurniture && bedFurniture.GetBoundingBox().Contains(Utility.Vector2ToPoint(Game1.player.mostRecentBed)))
						{
							@event.CurrentCommand++;
							return;
						}
						objectAtTile.isTemporarilyInvisible = true;
					}
					else if (location.terrainFeatures.TryGetValue(new Vector2(j, i), out value4))
					{
						value4.isTemporarilyInvisible = true;
					}
				}
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddObject(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetPoint(args, 1, out var value, out var error, "Point tile") || !ArgUtility.TryGet(args, 3, out var value2, out error, allowBlank: true, "string itemId") || !ArgUtility.TryGetOptionalFloat(args, 4, out var value3, out error, -1f, "float layerDepth"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Vector2 position = @event.OffsetPosition(new Vector2(value.X * 64, value.Y * 64));
			TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(null, Microsoft.Xna.Framework.Rectangle.Empty, position, flipped: false, 0f, Color.White)
			{
				layerDepth = ((value3 >= 0f) ? value3 : ((float)(@event.OffsetTileY(value.Y) * 64) / 10000f))
			};
			temporaryAnimatedSprite.CopyAppearanceFromItemId(value2);
			context.Location.TemporarySprites.Add(temporaryAnimatedSprite);
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddBigProp(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetVector2(args, 1, out var value, out var error, integerOnly: true, "Vector2 tile") || !ArgUtility.TryGet(args, 3, out var value2, out error, allowBlank: true, "string itemId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Object obj = ItemRegistry.Create<Object>("(BC)" + value2);
			obj.TileLocation = @event.OffsetTile(value);
			@event.props.Add(obj);
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddFloorProp(Event @event, string[] args, EventContext context)
		{
			AddProp(@event, args, context);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddProp(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 0, out var value, out var error, allowBlank: true, "string commandName") || !ArgUtility.TryGetInt(args, 1, out var value2, out error, "int index") || !ArgUtility.TryGetPoint(args, 2, out var value3, out error, "Point tile") || !ArgUtility.TryGetOptionalInt(args, 4, out var value4, out error, 1, "int drawWidth") || !ArgUtility.TryGetOptionalInt(args, 5, out var value5, out error, 1, "int drawHeight") || !ArgUtility.TryGetOptionalInt(args, 6, out var value6, out error, value5, "int boundingHeight") || !ArgUtility.TryGetOptionalInt(args, 7, out var value7, out error, 0, "int tilesHorizontal") || !ArgUtility.TryGetOptionalInt(args, 8, out var value8, out error, 0, "int tilesVertical"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			int num = @event.OffsetTileX(value3.X);
			int num2 = @event.OffsetTileY(value3.Y);
			bool solid = !value.EqualsIgnoreCase("AddFloorProp");
			@event.festivalProps.Add(new Prop(@event.festivalTexture, value2, value4, value6, value5, num, num2, solid));
			if (value7 != 0)
			{
				for (int num3 = num + value7; num3 != num; num3 -= Math.Sign(value7))
				{
					@event.festivalProps.Add(new Prop(@event.festivalTexture, value2, value4, value6, value5, num3, num2, solid));
				}
			}
			if (value8 != 0)
			{
				for (int num4 = num2 + value8; num4 != num2; num4 -= Math.Sign(value8))
				{
					@event.festivalProps.Add(new Prop(@event.festivalTexture, value2, value4, value6, value5, num, num4, solid));
				}
			}
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void RemoveObject(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetPoint(args, 1, out var value, out var error, "Point tile"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			GameLocation location = context.Location;
			Vector2 position = @event.OffsetPosition(new Vector2(value.X, value.Y) * 64f);
			location.temporarySprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.position == position);
			@event.CurrentCommand++;
			@event.Update(location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Glow(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int red") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int green") || !ArgUtility.TryGetInt(args, 3, out var value3, out error, "int blue") || !ArgUtility.TryGetOptionalBool(args, 4, out var value4, out error, defaultValue: false, "bool hold"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.screenGlowOnce(new Color(value, value2, value3), value4);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void StopGlowing(Event @event, string[] args, EventContext context)
		{
			Game1.screenGlowUp = false;
			Game1.screenGlowHold = false;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddQuest(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string questId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.player.addQuest(value);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void RemoveQuest(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string questId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.player.removeQuest(value);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddSpecialOrder(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string orderId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.player.team.AddSpecialOrder(value);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void RemoveSpecialOrder(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var orderId, out var error, allowBlank: true, "string orderId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.player.team.specialOrders.RemoveWhere((SpecialOrder order) => order.questKey.Value == orderId);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddItem(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string itemId") || !ArgUtility.TryGetOptionalInt(args, 2, out var value2, out error, 1, "int count") || !ArgUtility.TryGetOptionalInt(args, 3, out var value3, out error, 0, "int quality"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Item item = ItemRegistry.Create(value, value2, value3);
			if (item != null)
			{
				Game1.player.addItemByMenuIfNecessary(item);
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AwardFestivalPrize(Event @event, string[] args, EventContext context)
		{
			if (args.Length == 1)
			{
				string id = @event.id;
				if (id == "festival_spring13")
				{
					if (@event.festivalWinners.Contains(Game1.player.UniqueMultiplayerID))
					{
						if (Game1.player.mailReceived.Add("Egg Festival"))
						{
							if (Game1.activeClickableMenu == null)
							{
								Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(H)4"));
							}
							@event.CurrentCommand++;
							if (Game1.activeClickableMenu == null)
							{
								@event.CurrentCommand++;
							}
						}
						else
						{
							if (Game1.activeClickableMenu == null)
							{
								Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(O)PrizeTicket"));
							}
							@event.CurrentCommand++;
							if (Game1.activeClickableMenu == null)
							{
								@event.CurrentCommand++;
							}
						}
					}
					else
					{
						@event.CurrentCommand += 2;
					}
					return;
				}
				if (id == "festival_winter8")
				{
					if (@event.festivalWinners.Contains(Game1.player.UniqueMultiplayerID))
					{
						if (Game1.player.mailReceived.Add("Ice Festival"))
						{
							if (Game1.activeClickableMenu == null)
							{
								Game1.activeClickableMenu = new ItemGrabMenu(new Item[4]
								{
									ItemRegistry.Create("(H)17"),
									ItemRegistry.Create("(O)687"),
									ItemRegistry.Create("(O)691"),
									ItemRegistry.Create("(O)703")
								}, @event).setEssential(essential: true);
							}
							@event.CurrentCommand++;
						}
						else
						{
							if (Game1.activeClickableMenu == null)
							{
								Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(O)PrizeTicket"));
							}
							@event.CurrentCommand++;
							if (Game1.activeClickableMenu == null)
							{
								@event.CurrentCommand++;
							}
						}
					}
					else
					{
						@event.CurrentCommand += 2;
					}
					return;
				}
			}
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string itemId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			switch (value.ToLower())
			{
			case "meowmere":
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(W)65"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "birdiereward":
				Game1.player.team.RequestLimitedNutDrops("Birdie", null, 0, 0, 5, 5);
				if (!Game1.MasterPlayer.hasOrWillReceiveMail("gotBirdieReward"))
				{
					Game1.addMailForTomorrow("gotBirdieReward", noLetter: true, sendToEveryone: true);
				}
				@event.CurrentCommand++;
				@event.CurrentCommand++;
				break;
			case "memento":
			{
				Object obj = ItemRegistry.Create<Object>("(O)864");
				obj.specialItem = true;
				obj.questItem.Value = true;
				Game1.player.addItemByMenuIfNecessary(obj);
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			}
			case "emilyclothes":
			{
				Clothing clothing = ItemRegistry.Create<Clothing>("(P)8");
				clothing.Dye(new Color(0, 143, 239), 1f);
				Game1.player.addItemsByMenuIfNecessary(new List<Item>
				{
					ItemRegistry.Create("(B)804"),
					ItemRegistry.Create("(H)41"),
					ItemRegistry.Create("(S)1127"),
					clothing
				});
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			}
			case "qimilk":
				if (Game1.player.mailReceived.Add("qiCave"))
				{
					Game1.player.maxHealth += 25;
				}
				@event.CurrentCommand++;
				break;
			case "pan":
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(T)Pan"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "sculpture":
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(F)1306"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "samboombox":
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(F)1309"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "marniepainting":
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(F)1802"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "rod":
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(T)BambooPole"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "pot":
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(BC)62"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "jukebox":
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(BC)209"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "sword":
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(W)0"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "hero":
				Game1.getSteamAchievement("Achievement_LocalLegend");
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(BC)116"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "joja":
				Game1.getSteamAchievement("Achievement_Joja");
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(BC)117"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			case "slimeegg":
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(O)680"));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			default:
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create(value));
				if (Game1.activeClickableMenu == null)
				{
					@event.CurrentCommand++;
				}
				@event.CurrentCommand++;
				break;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AttachCharacterToTempSprite(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			TemporaryAnimatedSprite temporaryAnimatedSprite = context.Location.temporarySprites.Last();
			if (temporaryAnimatedSprite != null)
			{
				temporaryAnimatedSprite.attachedCharacter = @event.getActorByName(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Fork(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string requiredId") || !ArgUtility.TryGetOptional(args, 2, out var value2, out error, null, allowBlank: true, "string newKey") || !ArgUtility.TryGetOptionalBool(args, 3, out var value3, out error, defaultValue: false, "bool isTranslationKey"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (value2 == null)
			{
				value2 = value;
				value = null;
			}
			bool num;
			if (value == null)
			{
				num = @event.specialEventVariable1;
			}
			else
			{
				if (Game1.player.mailReceived.Contains(value))
				{
					goto IL_008f;
				}
				num = Game1.player.dialogueQuestionsAnswered.Contains(value);
			}
			if (!num)
			{
				@event.CurrentCommand++;
				return;
			}
			goto IL_008f;
			IL_008f:
			string[] commands;
			if (value3)
			{
				string text = Game1.content.LoadStringReturnNullIfNotFound(value2);
				if (text == null)
				{
					context.LogErrorAndSkip("can't load new script from translation key '" + value2 + "' because that translation wasn't found");
					return;
				}
				commands = ParseCommands(text, context.Event.farmer);
			}
			else if (@event.isFestival)
			{
				if (!@event.TryGetFestivalDataForYear(value2, out var data))
				{
					context.LogErrorAndSkip($"can't load new script from festival field '{value2}' because there's no such key in the '{@event.id}' festival");
					return;
				}
				commands = ParseCommands(data, context.Event.farmer);
			}
			else
			{
				string text2 = "Data\\Events\\" + Game1.currentLocation.Name;
				if (!Game1.content.DoesAssetExist<Dictionary<string, string>>(text2))
				{
					context.LogErrorAndSkip("can't load new script from event asset '" + text2 + "' because it doesn't exist");
					return;
				}
				if (!Game1.content.Load<Dictionary<string, string>>(text2).TryGetValue(value2, out var value4))
				{
					context.LogErrorAndSkip($"can't load new script from event asset '{text2}' because it doesn't contain the required '{value2}' key");
					return;
				}
				commands = ParseCommands(value4, context.Event.farmer);
			}
			@event.ReplaceAllCommands(commands);
			@event.forked = true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void SwitchEvent(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string newKey"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			string[] commands;
			if (@event.isFestival)
			{
				if (!@event.TryGetFestivalDataForYear(value, out var data))
				{
					context.LogErrorAndSkip($"can't load new event from festival field '{value}' because there's no such key in the '{@event.id}' festival");
					return;
				}
				commands = ParseCommands(data, context.Event.farmer);
			}
			else
			{
				string text = "Data\\Events\\" + Game1.currentLocation.Name;
				if (!Game1.content.DoesAssetExist<Dictionary<string, string>>(text))
				{
					context.LogErrorAndSkip("can't load new event from asset '" + text + "' because it doesn't exist");
					return;
				}
				if (!Game1.content.Load<Dictionary<string, string>>(text).TryGetValue(value, out var value2))
				{
					context.LogErrorAndSkip($"can't load new event from asset '{text}' because it doesn't contain the required '{value}' key");
					return;
				}
				commands = ParseCommands(value2, context.Event.farmer);
			}
			@event.ReplaceAllCommands(commands);
			@event.eventSwitched = true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void GlobalFade(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetOptionalFloat(args, 1, out var value, out var error, 0.007f, "float fadeSpeed") || !ArgUtility.TryGetOptionalBool(args, 2, out var value2, out error, defaultValue: false, "bool continueEventDuringFade"))
			{
				context.LogErrorAndSkip(error);
			}
			else if (!Game1.globalFade)
			{
				if (value2)
				{
					Game1.globalFadeToBlack(null, value);
					@event.CurrentCommand++;
				}
				else
				{
					Game1.globalFadeToBlack(@event.incrementCommandAfterFade, value);
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void GlobalFadeToClear(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetOptionalFloat(args, 1, out var value, out var error, 0.007f, "float fadeSpeed") || !ArgUtility.TryGetOptionalBool(args, 2, out var value2, out error, defaultValue: false, "bool continueEventDuringFade"))
			{
				context.LogErrorAndSkip(error);
			}
			else if (!Game1.globalFade)
			{
				if (value2)
				{
					Game1.globalFadeToClear(null, value);
					@event.CurrentCommand++;
				}
				else
				{
					Game1.globalFadeToClear(@event.incrementCommandAfterFade, value);
				}
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Cutscene(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string cutsceneId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			GameLocation location = context.Location;
			GameTime time = context.Time;
			if (@event.currentCustomEventScript != null)
			{
				if (@event.currentCustomEventScript.update(time, @event))
				{
					@event.currentCustomEventScript = null;
					@event.CurrentCommand++;
				}
			}
			else
			{
				if (Game1.currentMinigame != null)
				{
					return;
				}
				switch (value)
				{
				case "greenTea":
					@event.currentCustomEventScript = new EventScript_GreenTea(new Vector2(-64000f, -64000f), @event);
					break;
				case "linusMoneyGone":
					foreach (TemporaryAnimatedSprite temporarySprite in location.temporarySprites)
					{
						temporarySprite.alphaFade = 0.01f;
						temporarySprite.motion = new Vector2(0f, -1f);
					}
					@event.CurrentCommand++;
					return;
				case "marucomet":
					Game1.currentMinigame = new MaruComet();
					break;
				case "AbigailGame":
					Game1.currentMinigame = new AbigailGame(@event.getActorByName("Abigail") ?? Game1.RequireCharacter("Abigail"));
					break;
				case "robot":
					Game1.currentMinigame = new RobotBlastoff();
					break;
				case "haleyCows":
					Game1.currentMinigame = new HaleyCowPictures();
					break;
				case "boardGame":
					Game1.currentMinigame = new FantasyBoardGame();
					@event.CurrentCommand++;
					break;
				case "plane":
					Game1.currentMinigame = new PlaneFlyBy();
					break;
				case "balloonDepart":
				{
					TemporaryAnimatedSprite temporarySpriteByID = location.getTemporarySpriteByID(1);
					temporarySpriteByID.attachedCharacter = @event.farmer;
					temporarySpriteByID.motion = new Vector2(0f, -2f);
					TemporaryAnimatedSprite temporarySpriteByID2 = location.getTemporarySpriteByID(2);
					temporarySpriteByID2.attachedCharacter = @event.getActorByName("Harvey");
					temporarySpriteByID2.motion = new Vector2(0f, -2f);
					location.getTemporarySpriteByID(3).scaleChange = -0.01f;
					@event.CurrentCommand++;
					return;
				}
				case "clearTempSprites":
					location.temporarySprites.Clear();
					@event.CurrentCommand++;
					break;
				case "balloonChangeMap":
					@event.eventPositionTileOffset = Vector2.Zero;
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 1183, 84, 160), 10000f, 1, 99999, @event.OffsetPosition(new Vector2(22f, 36f) * 64f + new Vector2(-23f, 0f) * 4f), flicker: false, flipped: false, 2E-05f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(0f, -2f),
						yStopCoordinate = (int)@event.OffsetPositionY(576f),
						reachedStopCoordinate = @event.balloonInSky,
						attachedCharacter = @event.farmer,
						id = 1
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(84, 1205, 38, 26), 10000f, 1, 99999, @event.OffsetPosition(new Vector2(22f, 36f) * 64f + new Vector2(0f, 134f) * 4f), flicker: false, flipped: false, 0.2625f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(0f, -2f),
						id = 2,
						attachedCharacter = @event.getActorByName("Harvey")
					});
					@event.CurrentCommand++;
					break;
				case "bandFork":
				{
					int answerChoice = 76;
					if (Game1.player.dialogueQuestionsAnswered.Contains("77"))
					{
						answerChoice = 77;
					}
					else if (Game1.player.dialogueQuestionsAnswered.Contains("78"))
					{
						answerChoice = 78;
					}
					else if (Game1.player.dialogueQuestionsAnswered.Contains("79"))
					{
						answerChoice = 79;
					}
					@event.answerDialogue("bandFork", answerChoice);
					@event.CurrentCommand++;
					return;
				}
				case "eggHuntWinner":
					@event.eggHuntWinner();
					@event.CurrentCommand++;
					return;
				case "governorTaste":
					@event.governorTaste();
					@event.currentCommand++;
					return;
				case "addSecretSantaItem":
				{
					Item giftFromNPC = Utility.getGiftFromNPC(@event.mySecretSanta);
					Game1.player.addItemByMenuIfNecessaryElseHoldUp(giftFromNPC);
					@event.currentCommand++;
					return;
				}
				case "iceFishingWinner":
					@event.iceFishingWinner();
					@event.currentCommand++;
					return;
				case "iceFishingWinnerMP":
					@event.iceFishingWinnerMP();
					@event.currentCommand++;
					return;
				}
				Game1.globalFadeToClear(null, 0.01f);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void WaitForTempSprite(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int spriteId"))
			{
				context.LogErrorAndSkip(error);
			}
			else if (Game1.currentLocation.getTemporarySpriteByID(value) != null)
			{
				@event.CurrentCommand++;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Cave(Event @event, string[] args, EventContext context)
		{
			if (Game1.activeClickableMenu == null)
			{
				Response[] answerChoices = new Response[2]
				{
					new Response("Mushrooms", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1220")),
					new Response("Bats", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1222"))
				};
				Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1223"), answerChoices, "cave");
				Game1.dialogueTyping = false;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void UpdateMinigame(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int eventData"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.currentMinigame?.receiveEventPoke(value);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void StartJittering(Event @event, string[] args, EventContext context)
		{
			@event.farmer.jitterStrength = 1f;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Money(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int amount"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			@event.farmer.Money += value;
			if (@event.farmer.Money < 0)
			{
				@event.farmer.Money = 0;
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void StopJittering(Event @event, string[] args, EventContext context)
		{
			@event.farmer.stopJittering();
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddLantern(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int initialParentSheetIndex") || !ArgUtility.TryGetVector2(args, 2, out var value2, out error, integerOnly: true, "Vector2 tile") || !ArgUtility.TryGetInt(args, 4, out var value3, out error, "int lightRadius"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			context.Location.TemporarySprites.Add(new TemporaryAnimatedSprite(value, 999999f, 1, 0, @event.OffsetPosition(value2 * 64f), flicker: false, flipped: false)
			{
				lightId = @event.GenerateLightSourceId($"{"AddLantern"}_{value2.X}_{value2.Y}"),
				lightRadius = value3
			});
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void RustyKey(Event @event, string[] args, EventContext context)
		{
			Game1.player.hasRustyKey = true;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Swimming(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
				if (farmerActor != null)
				{
					farmerActor.bathingClothes.Value = true;
					farmerActor.swimming.Value = true;
				}
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				actorByName.swimming.Value = true;
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void StopSwimming(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
				if (farmerActor != null)
				{
					farmerActor.bathingClothes.Value = context.Location is BathHousePool;
					farmerActor.swimming.Value = false;
				}
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				actorByName.swimming.Value = false;
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void TutorialMenu(Event @event, string[] args, EventContext context)
		{
			if (Game1.activeClickableMenu == null)
			{
				Game1.activeClickableMenu = new TutorialMenu();
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AnimalNaming(Event @event, string[] args, EventContext context)
		{
			GameLocation currentLocation = Game1.currentLocation;
			AnimalHouse animalHouse = currentLocation as AnimalHouse;
			if (animalHouse == null)
			{
				context.LogErrorAndSkip("this command only works when run in an AnimalHouse location");
			}
			else if (Game1.activeClickableMenu == null)
			{
				Game1.activeClickableMenu = new NamingMenu(delegate(string animalName)
				{
					animalHouse.addNewHatchedAnimal(animalName);
					@event.CurrentCommand++;
				}, Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1236"));
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void SplitSpeak(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string dialogue"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			string[] array = LegacyShims.SplitAndTrim(value2, '~');
			if (Game1.dialogueUp)
			{
				return;
			}
			@event.timeAccumulator += context.Time.ElapsedGameTime.Milliseconds;
			if (!(@event.timeAccumulator < 500f))
			{
				@event.timeAccumulator = 0f;
				bool isOptionalNpc;
				NPC nPC = @event.getActorByName(value, out isOptionalNpc) ?? Game1.getCharacterFromName(value);
				if (nPC == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				if (!ArgUtility.HasIndex(array, @event.previousAnswerChoice))
				{
					@event.CurrentCommand++;
					return;
				}
				nPC.CurrentDialogue.Push(new Dialogue(nPC, null, array[@event.previousAnswerChoice]));
				Game1.drawDialogue(nPC);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void CatQuestion(Event @event, string[] args, EventContext context)
		{
			if (!Game1.isQuestion && Game1.activeClickableMenu == null)
			{
				PetData data;
				string sub = (Pet.TryGetData(Game1.player.whichPetType, out data) ? (TokenParser.ParseText(data.DisplayName) ?? "pet") : "pet");
				Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:AdoptPet", sub), Game1.currentLocation.createYesNoResponses(), "pet");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AmbientLight(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int red") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int green") || !ArgUtility.TryGetInt(args, 3, out var value3, out error, "int blue"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.ambientLight = new Color(value, value2, value3);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void BgColor(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int red") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int green") || !ArgUtility.TryGetInt(args, 3, out var value3, out error, "int blue"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.setBGColor((byte)value, (byte)value2, (byte)value3);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ElliottBookTalk(Event @event, string[] args, EventContext context)
		{
			if (!Game1.dialogueUp)
			{
				string translationKey = (Game1.player.dialogueQuestionsAnswered.Contains("958699") ? "Strings\\StringsFromCSFiles:Event.cs.1257" : (Game1.player.dialogueQuestionsAnswered.Contains("958700") ? "Strings\\StringsFromCSFiles:Event.cs.1258" : ((!Game1.player.dialogueQuestionsAnswered.Contains("9586701")) ? "Strings\\StringsFromCSFiles:Event.cs.1260" : "Strings\\StringsFromCSFiles:Event.cs.1259")));
				NPC nPC = @event.getActorByName("Elliott") ?? Game1.getCharacterFromName("Elliott");
				nPC.CurrentDialogue.Push(new Dialogue(nPC, translationKey));
				Game1.drawDialogue(nPC);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void RemoveItem(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string itemId") || !ArgUtility.TryGetOptionalInt(args, 2, out var value2, out error, 1, "int count"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.player.removeFirstOfThisItemFromInventory(value, value2);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Friendship(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int friendshipChange"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			NPC characterFromName = Game1.getCharacterFromName(value);
			if (characterFromName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'");
				return;
			}
			Game1.player.changeFriendship(value2, characterFromName);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void SetRunning(Event @event, string[] args, EventContext context)
		{
			@event.farmer.setRunning(isRunning: true);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ExtendSourceRect(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string rawOption"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool flag = value2 == "reset";
			int value3 = -1;
			int value4 = -1;
			bool value5 = false;
			if (!flag && (!ArgUtility.TryGetInt(args, 2, out value3, out error, "horizontal") || !ArgUtility.TryGetInt(args, 3, out value4, out error, "vertical") || !ArgUtility.TryGetOptionalBool(args, 4, out value5, out error, defaultValue: false, "ignoreSourceRectUpdates")))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			if (flag)
			{
				actorByName.reloadSprite();
				actorByName.Sprite.SpriteWidth = 16;
				actorByName.Sprite.SpriteHeight = 32;
				actorByName.HideShadow = false;
			}
			else
			{
				actorByName.extendSourceRect(value3, value4, value5);
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void WaitForOtherPlayers(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string gateId"))
			{
				context.LogErrorAndSkip(error);
			}
			else if (Game1.IsMultiplayer)
			{
				Game1.netReady.SetLocalReady(value, ready: true);
				if (Game1.netReady.IsReady(value))
				{
					if (Game1.activeClickableMenu is ReadyCheckDialog)
					{
						Game1.exitActiveMenu();
					}
					@event.CurrentCommand++;
				}
				else if (Game1.activeClickableMenu == null)
				{
					Game1.activeClickableMenu = new ReadyCheckDialog(value, allowCancel: false);
				}
			}
			else
			{
				@event.CurrentCommand++;
			}
		}

		/// <summary>Used in the movie theater, requests that the server end the movie.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void RequestMovieEnd(Event @event, string[] args, EventContext context)
		{
			Game1.player.team.requestMovieEndEvent.Fire(Game1.player.UniqueMultiplayerID);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void RestoreStashedItem(Event @event, string[] args, EventContext context)
		{
			Game1.player.TemporaryItem = null;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AdvancedMove(Event @event, string[] args, EventContext context)
		{
			@event.setUpAdvancedMove(args);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void StopRunning(Event @event, string[] args, EventContext context)
		{
			@event.farmer.setRunning(isRunning: false);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Eyes(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int eyes") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int blinkTimer"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			@event.farmer.currentEyes = value;
			@event.farmer.blinkTimer = value2;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		[OtherNames(new string[] { "mailReceived" })]
		public static void AddMailReceived(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string mailId") || !ArgUtility.TryGetOptionalBool(args, 2, out var value2, out error, defaultValue: true, "bool add"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.player.mailReceived.Toggle(value, value2);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddWorldState(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string worldStateId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.worldStateIDs.Add(value);
			Game1.netWorldState.Value.addWorldStateID(value);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Fade(Event @event, string[] args, EventContext context)
		{
			string text = ArgUtility.Get(args, 1);
			if (text == "unfade")
			{
				Game1.fadeIn = false;
				Game1.fadeToBlack = false;
				@event.CurrentCommand++;
				return;
			}
			Game1.fadeToBlack = true;
			Game1.fadeIn = true;
			if (Game1.fadeToBlackAlpha >= 0.97f)
			{
				if (text == null)
				{
					Game1.fadeIn = false;
				}
				@event.CurrentCommand++;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ChangeMapTile(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string layerId") || !ArgUtility.TryGetPoint(args, 2, out var value2, out error, "Point tilePos") || !ArgUtility.TryGetInt(args, 4, out var value3, out error, "int newTileIndex"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Layer layer = context.Location.map.GetLayer(value);
			if (layer == null)
			{
				context.LogErrorAndSkip("the '" + context.Location.NameOrUniqueName + "' location doesn't have required map layer " + value);
				return;
			}
			int num = @event.OffsetTileX(value2.X);
			int num2 = @event.OffsetTileY(value2.Y);
			Tile tile = layer.Tiles[num, num2];
			if (tile == null)
			{
				context.LogErrorAndSkip($"the '{context.Location.NameOrUniqueName}' location doesn't have required tile ({value2.X}, {value2.Y})" + ((num != value2.X || num2 != value2.Y) ? $" (adjusted to ({num}, {num2})" : "") + " on layer " + value);
			}
			else
			{
				tile.TileIndex = value3;
				@event.CurrentCommand++;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ChangeSprite(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetOptional(args, 2, out var value2, out error, null, allowBlank: true, "string spriteSuffix"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			if (value2 != null)
			{
				actorByName.spriteOverridden = true;
				actorByName.Sprite.LoadTexture("Characters\\" + NPC.getTextureNameForCharacter(actorByName.Name) + "_" + value2);
			}
			else
			{
				actorByName.spriteOverridden = false;
				actorByName.ChooseAppearance();
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void WaitForAllStationary(Event @event, string[] args, EventContext context)
		{
			List<NPCController> npcControllers = @event.npcControllers;
			bool flag = npcControllers != null && npcControllers.Count > 0;
			if (!flag)
			{
				foreach (NPC actor in @event.actors)
				{
					if (actor.isMoving())
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				foreach (Farmer farmerActor in @event.farmerActors)
				{
					if (farmerActor.isMoving())
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				@event.CurrentCommand++;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ProceedPosition(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Character characterByName = @event.getCharacterByName(value);
			if (characterByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'");
				return;
			}
			@event.continueAfterMove = true;
			try
			{
				if (characterByName.isMoving())
				{
					List<NPCController> npcControllers = @event.npcControllers;
					if (npcControllers == null || npcControllers.Count != 0)
					{
						return;
					}
				}
				characterByName.Halt();
				@event.CurrentCommand++;
			}
			catch
			{
				@event.CurrentCommand++;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ChangePortrait(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetOptional(args, 2, out var value2, out error, null, allowBlank: true, "string portraitSuffix"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC nPC = @event.getActorByName(value, out isOptionalNpc) ?? Game1.getCharacterFromName(value);
			if (nPC == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			if (value2 != null)
			{
				nPC.portraitOverridden = true;
				nPC.Portrait = Game1.content.Load<Texture2D>("Portraits\\" + NPC.getTextureNameForCharacter(nPC.Name) + "_" + value2);
			}
			else
			{
				nPC.portraitOverridden = false;
				nPC.ChooseAppearance();
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ChangeYSourceRectOffset(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int ySourceRectOffset"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			actorByName.ySourceRectOffset = value2;
			@event.CurrentCommand++;
		}

		/// <summary>Set the display name for an event actor to an exact value.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ChangeName(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string newName"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			actorByName.displayName = value2;
			@event.CurrentCommand++;
		}

		/// <summary>Set the display name for an event actor to a translation key.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void TranslateName(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string translationKey"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			actorByName.displayName = Game1.content.LoadString(value2);
			@event.CurrentCommand++;
		}

		/// <summary>Replace an NPC in the event with a temporary copy that only exists for the duration of the event. This allows changing the NPC in the event (e.g. renaming them) without affecting the real NPC.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ReplaceWithClone(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			@event.actors.Remove(actorByName);
			@event.actors.Add(new NPC(actorByName.Sprite.Clone(), actorByName.Position, actorByName.FacingDirection, actorByName.Name)
			{
				Birthday_Day = actorByName.Birthday_Day,
				Birthday_Season = actorByName.Birthday_Season,
				Gender = actorByName.Gender,
				Portrait = actorByName.Portrait,
				EventActor = true,
				displayName = actorByName.displayName,
				drawOffset = actorByName.drawOffset,
				TemporaryDialogue = new Stack<Dialogue>(actorByName.CurrentDialogue.Select((Dialogue p) => new Dialogue(p)))
			});
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void PlayFramesAhead(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int framesToSkip"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			@event.CurrentCommand++;
			for (int i = 0; i < value; i++)
			{
				@event.Update(context.Location, context.Time);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ShowKissFrame(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetOptionalBool(args, 2, out var value2, out error, defaultValue: false, "bool flip"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
				return;
			}
			CharacterData data = actorByName.GetData();
			int frame = data?.KissSpriteIndex ?? 28;
			bool flag = data?.KissSpriteFacingRight ?? true;
			if (value2)
			{
				flag = !flag;
			}
			actorByName.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
			{
				new FarmerSprite.AnimationFrame(frame, 1000, secondaryArm: false, flag)
			});
			@event.CurrentCommand++;
		}

		/// <remarks>Format: <c>addTemporaryActor name spriteWidth spriteHeight xPosition yPosition facingDirection breather=true animal=false</c>.</remarks>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddTemporaryActor(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string spriteAssetName") || !ArgUtility.TryGetPoint(args, 2, out var value2, out error, "Point spriteSize") || !ArgUtility.TryGetVector2(args, 4, out var value3, out error, integerOnly: false, "Vector2 tile") || !ArgUtility.TryGetDirection(args, 6, out var value4, out error, "int facingDirection") || !ArgUtility.TryGetOptionalBool(args, 7, out var value5, out error, defaultValue: true, "bool isBreather") || !ArgUtility.TryGetOptional(args, 8, out var value6, out error, null, allowBlank: true, "string typeOrDisplayName") || !ArgUtility.TryGetOptional(args, 9, out var value7, out error, null, allowBlank: false, "string overrideName"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			string text = "Characters\\";
			bool flag = true;
			switch (value6?.ToLower())
			{
			case "animal":
				text = "Animals\\";
				break;
			case "monster":
				text = "Characters\\Monsters\\";
				break;
			default:
				flag = false;
				break;
			case "character":
				break;
			}
			string text2 = text + value;
			if (!Game1.content.DoesAssetExist<Texture2D>(text2))
			{
				string text3 = value.Replace('_', ' ');
				string text4 = text + text3;
				if (text3 != value && Game1.content.DoesAssetExist<Texture2D>(text4))
				{
					value = text3;
					text2 = text4;
				}
			}
			NPC nPC = new NPC(new AnimatedSprite(@event.festivalContent, text2, 0, value2.X, value2.Y), @event.OffsetPosition(value3 * 64f), value4, value, @event.festivalContent);
			nPC.AllowDynamicAppearance = false;
			nPC.Breather = value5;
			nPC.HideShadow = nPC.Sprite.SpriteWidth >= 32;
			nPC.TemporaryDialogue = new Stack<Dialogue>();
			if (!flag && value6 != null)
			{
				nPC.displayName = value6;
			}
			if (@event.isFestival && @event.TryGetFestivalDialogueForYear(nPC, nPC.Name, out var dialogue))
			{
				nPC.CurrentDialogue.Push(dialogue);
			}
			if (value7 != null)
			{
				nPC.Name = value7;
			}
			nPC.EventActor = true;
			@event.actors.Add(nPC);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ChangeToTemporaryMap(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string mapName") || !ArgUtility.TryGetOptionalBool(args, 2, out var value2, out error, defaultValue: true, "bool shouldPan"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			@event.temporaryLocation = ((value == "Town") ? new Town("Maps\\Town", "Temp") : ((@event.isFestival && value.Contains("Town")) ? new Town("Maps\\" + value, "Temp") : new GameLocation("Maps\\" + value, "Temp")));
			@event.temporaryLocation.map.LoadTileSheets(Game1.mapDisplayDevice);
			Event currentEvent = Game1.currentLocation.currentEvent;
			Game1.currentLocation.cleanupBeforePlayerExit();
			Game1.currentLocation.currentEvent = null;
			Game1.currentLightSources.Clear();
			Game1.currentLocation = @event.temporaryLocation;
			Game1.currentLocation.resetForPlayerEntry();
			Game1.currentLocation.UpdateMapSeats();
			Game1.currentLocation.currentEvent = currentEvent;
			@event.CurrentCommand++;
			Game1.player.currentLocation = Game1.currentLocation;
			@event.farmer.currentLocation = Game1.currentLocation;
			Game1.currentLocation.ResetForEvent(@event);
			if (value2)
			{
				Game1.panScreen(0, 0);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void PositionOffset(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetPoint(args, 2, out var value2, out error, "Point offset") || !ArgUtility.TryGetOptionalBool(args, 4, out var value3, out error, defaultValue: false, "bool continueImmediately"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
				if (farmerActor != null)
				{
					farmerActor.position.X += value2.X;
					farmerActor.position.Y += value2.Y;
				}
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				actorByName.position.X += value2.X;
				actorByName.position.Y += value2.Y;
			}
			@event.CurrentCommand++;
			if (value3)
			{
				@event.Update(context.Location, context.Time);
			}
		}

		/// <remarks>Format: <c>question &lt;questionKey (forkN to make the nth answer fork)&gt; "question#answer1#answer2#...#answerN"</c>.</remarks>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Question(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string dialogueKey") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string rawQuestionsAndAnswers"))
			{
				context.LogErrorAndSkip(error);
			}
			else if (!Game1.isQuestion && Game1.activeClickableMenu == null)
			{
				string[] array = LegacyShims.SplitAndTrim(value2, '#');
				string question = array[0];
				Response[] array2 = new Response[array.Length - 1];
				for (int i = 1; i < array.Length; i++)
				{
					array2[i - 1] = new Response((i - 1).ToString(), array[i]);
				}
				Game1.currentLocation.createQuestionDialogue(question, array2, value);
			}
		}

		/// <remarks>Format: <c>quickQuestion question#answer1#answer2#...#answerN(break)answerLogic1(break)answerLogic2(break)...(break)answerLogicN</c>. Use <c>\</c> instead of <c>/</c> inside the <c>answerLogic</c> sections.</remarks>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void QuickQuestion(Event @event, string[] args, EventContext context)
		{
			if (!Game1.isQuestion && Game1.activeClickableMenu == null)
			{
				string currentCommand = @event.GetCurrentCommand();
				string[] array = LegacyShims.SplitAndTrim(LegacyShims.SplitAndTrim(currentCommand.Substring(currentCommand.IndexOf(' ') + 1), "(break)")[0], '#');
				string question = array[0];
				Response[] array2 = new Response[array.Length - 1];
				for (int i = 1; i < array.Length; i++)
				{
					array2[i - 1] = new Response((i - 1).ToString(), array[i]);
				}
				Game1.currentLocation.createQuestionDialogue(question, array2, "quickQuestion");
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void DrawOffset(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetVector2(args, 2, out var value2, out error, integerOnly: true, "Vector2 offset"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc = false;
			int farmerNumber;
			Character character = (@event.IsFarmerActorId(value, out farmerNumber) ? ((Character)@event.GetFarmerActor(farmerNumber)) : ((Character)@event.getActorByName(value, out isOptionalNpc)));
			if (character == null)
			{
				context.LogErrorAndSkip("no actor found with name '" + value + "'", isOptionalNpc);
				return;
			}
			character.drawOffset = value2 * 4f;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void HideShadow(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetBool(args, 2, out var value2, out error, "bool hideShadow"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			bool isOptionalNpc;
			NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
			if (actorByName == null)
			{
				context.LogErrorAndSkip("no actor found with name '" + value + "'", isOptionalNpc);
				return;
			}
			actorByName.HideShadow = value2;
			@event.CurrentCommand++;
		}

		/// <summary>Animates properties of character "jumps". If any argument is set to "keep", it'll retain the current value.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AnimateHeight(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string rawHeight") || !ArgUtility.TryGet(args, 3, out var value3, out error, allowBlank: true, "string rawGravity") || !ArgUtility.TryGet(args, 4, out var value4, out error, allowBlank: true, "string rawVelocity"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			int? num = null;
			float? num2 = null;
			float? num3 = null;
			if (value2 != "keep")
			{
				if (!int.TryParse(value2, out var result))
				{
					context.LogErrorAndSkip("required index 2 must be 'keep' or an integer height");
					return;
				}
				num = result;
			}
			if (value3 != "keep")
			{
				if (!float.TryParse(value3, out var result2))
				{
					context.LogErrorAndSkip("required index 3 must be 'keep' or a float gravity value");
					return;
				}
				num2 = result2;
			}
			if (value4 != "keep")
			{
				if (!float.TryParse(value4, out var result3))
				{
					context.LogErrorAndSkip("required index 4 must be 'keep' or a float velocity value");
					return;
				}
				num3 = result3;
			}
			bool isOptionalNpc = false;
			int farmerNumber;
			Character character = (@event.IsFarmerActorId(value, out farmerNumber) ? ((Character)@event.GetFarmerActor(farmerNumber)) : ((Character)@event.getActorByName(value, out isOptionalNpc)));
			if (character == null)
			{
				context.LogErrorAndSkip("no actor found with name '" + value + "'", isOptionalNpc);
				return;
			}
			if (num.HasValue)
			{
				character.yJumpOffset = -num.Value;
			}
			if (num2.HasValue)
			{
				character.yJumpGravity = num2.Value;
			}
			if (num3.HasValue)
			{
				character.yJumpVelocity = num3.Value;
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Jump(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetOptionalFloat(args, 2, out var value2, out error, 8f, "float jumpV") || !ArgUtility.TryGetOptionalBool(args, 3, out var value3, out error, defaultValue: false, "bool noSound"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				@event.GetFarmerActor(farmerNumber)?.jump(value2);
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				if (value3)
				{
					actorByName.jumpWithoutSound(value2);
				}
				else
				{
					actorByName.jump(value2);
				}
			}
			@event.CurrentCommand++;
			@event.Update(context.Location, context.Time);
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void FarmerEat(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string itemId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Object o = ItemRegistry.Create<Object>("(O)" + value);
			@event.farmer.eatObject(o, overrideFullness: true);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void SpriteText(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out var error, "int colorIndex") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string text"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			@event.int_useMeForAnything2 = value;
			@event.float_useMeForAnything += context.Time.ElapsedGameTime.Milliseconds;
			if (@event.float_useMeForAnything > 80f)
			{
				if (@event.int_useMeForAnything >= value2.Length)
				{
					if (@event.float_useMeForAnything >= 2500f)
					{
						@event.int_useMeForAnything = 0;
						@event.float_useMeForAnything = 0f;
						@event.spriteTextToDraw = "";
						@event.CurrentCommand++;
					}
				}
				else
				{
					@event.int_useMeForAnything++;
					@event.float_useMeForAnything = 0f;
					Game1.playSound("dialogueCharacter");
				}
			}
			@event.spriteTextToDraw = value2;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void IgnoreCollisions(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string actorName"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.IsFarmerActorId(value, out var farmerNumber))
			{
				Farmer farmerActor = @event.GetFarmerActor(farmerNumber);
				if (farmerActor != null)
				{
					farmerActor.ignoreCollisions = true;
				}
			}
			else
			{
				bool isOptionalNpc;
				NPC actorByName = @event.getActorByName(value, out isOptionalNpc);
				if (actorByName == null)
				{
					context.LogErrorAndSkip("no NPC found with name '" + value + "'", isOptionalNpc);
					return;
				}
				actorByName.isCharging = true;
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void ScreenFlash(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetFloat(args, 1, out var value, out var error, "float flashAlpha"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.flashAlpha = value;
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void GrandpaCandles(Event @event, string[] args, EventContext context)
		{
			int grandpaCandlesFromScore = Utility.getGrandpaCandlesFromScore(Utility.getGrandpaScore());
			Game1.getFarm().grandpaScore.Value = grandpaCandlesFromScore;
			for (int i = 0; i < grandpaCandlesFromScore; i++)
			{
				DelayedAction.playSoundAfterDelay("fireball", 100 * i);
			}
			Game1.getFarm().addGrandpaCandles();
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void GrandpaEvaluation2(Event @event, string[] args, EventContext context)
		{
			switch (Utility.getGrandpaCandlesFromScore(Utility.getGrandpaScore()))
			{
			case 1:
				@event.ReplaceCurrentCommand("speak Grandpa \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1306") + "\"");
				break;
			case 2:
				@event.ReplaceCurrentCommand("speak Grandpa \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1307") + "\"");
				break;
			case 3:
				@event.ReplaceCurrentCommand("speak Grandpa \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1308") + "\"");
				break;
			case 4:
				@event.ReplaceCurrentCommand("speak Grandpa \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1309") + "\"");
				break;
			}
			Game1.player.eventsSeen.Remove("2146991");
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void GrandpaEvaluation(Event @event, string[] args, EventContext context)
		{
			switch (Utility.getGrandpaCandlesFromScore(Utility.getGrandpaScore()))
			{
			case 1:
				@event.ReplaceCurrentCommand("speak Grandpa \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1315") + "\"");
				break;
			case 2:
				@event.ReplaceCurrentCommand("speak Grandpa \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1316") + "\"");
				break;
			case 3:
				@event.ReplaceCurrentCommand("speak Grandpa \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1317") + "\"");
				break;
			case 4:
				@event.ReplaceCurrentCommand("speak Grandpa \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1318") + "\"");
				break;
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void LoadActors(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string layerId"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Layer layer = @event.temporaryLocation?.map.GetLayer(value);
			if (layer == null)
			{
				context.LogErrorAndSkip("the '" + context.Location.NameOrUniqueName + "' location doesn't have required map layer " + value);
				return;
			}
			@event.actors.Clear();
			@event.npcControllers?.Clear();
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			foreach (KeyValuePair<string, CharacterData> characterDatum in Game1.characterData)
			{
				int festivalVanillaActorIndex = characterDatum.Value.FestivalVanillaActorIndex;
				if (festivalVanillaActorIndex >= 0 && !dictionary.TryAdd(festivalVanillaActorIndex, characterDatum.Key))
				{
					Game1.log.Warn($"NPC '{characterDatum.Key}' has the same festival actor index as '{dictionary[festivalVanillaActorIndex]}' in Data/Characters, so it'll be ignored for festival placement.");
				}
			}
			HashSet<string> hashSet = new HashSet<string>();
			for (int i = 0; i < layer.LayerWidth; i++)
			{
				for (int j = 0; j < layer.LayerHeight; j++)
				{
					Tile tile = layer.Tiles[i, j];
					if (tile != null)
					{
						int tileIndex = tile.TileIndex;
						int key = tileIndex / 4;
						int facingDirection = tileIndex % 4;
						if (dictionary.TryGetValue(key, out var value2) && Game1.getCharacterFromName(value2) != null && (!(value2 == "Leo") || Game1.MasterPlayer.mailReceived.Contains("leoMoved")))
						{
							@event.addActor(value2, i, j, facingDirection, @event.temporaryLocation);
							hashSet.Add(value2);
						}
					}
				}
			}
			if (@event.festivalData != null && @event.TryGetFestivalDataForYear(value + "_additionalCharacters", out var data, out var actualKey))
			{
				string[] array = ParseCommands(data, context.Event.farmer);
				for (int k = 0; k < array.Length; k++)
				{
					string[] array2 = ArgUtility.SplitBySpaceQuoteAware(array[k]);
					if (!ArgUtility.TryGet(array2, 0, out var value3, out error, allowBlank: true, "string actorName") || !ArgUtility.TryGetPoint(array2, 1, out var value4, out error, "Point tile") || !ArgUtility.TryGetDirection(array2, 3, out var value5, out error, "int direction"))
					{
						context.LogError($"'{actualKey}' festival field has invalid additional character entry '{string.Join(" ", array2)}': {error}");
					}
					else if (Game1.getCharacterFromName(value3) != null)
					{
						if (!(value3 == "Leo") || Game1.MasterPlayer.mailReceived.Contains("leoMoved"))
						{
							@event.addActor(value3, value4.X, value4.Y, value5, @event.temporaryLocation);
							hashSet.Add(value3);
						}
					}
					else
					{
						context.LogError($"'{actualKey}' festival field has invalid additional character entry '{string.Join(" ", array2)}': no NPC found with name '{value3}'");
					}
				}
			}
			if (value == "Set-Up")
			{
				foreach (string item in hashSet)
				{
					NPC characterFromName = Game1.getCharacterFromName(item);
					if (!characterFromName.isMarried() || characterFromName.getSpouse() == null || characterFromName.getSpouse().getChildren().Count <= 0)
					{
						continue;
					}
					Farmer farmer = Game1.player;
					if (characterFromName.getSpouse() != null)
					{
						farmer = characterFromName.getSpouse();
					}
					List<Child> children = farmer.getChildren();
					characterFromName = @event.getCharacterByName(item) as NPC;
					for (int l = 0; l < children.Count; l++)
					{
						Child child = children[l];
						if (child.Age < 3)
						{
							continue;
						}
						Child child2 = new Child(child.Name, child.Gender == Gender.Male, child.darkSkinned.Value, farmer);
						child2.NetFields.CopyFrom(child.NetFields);
						child2.Halt();
						Point[] array3 = characterFromName.FacingDirection switch
						{
							0 => new Point[4]
							{
								new Point(0, 1),
								new Point(-1, 0),
								new Point(1, 0),
								new Point(0, -1)
							}, 
							2 => new Point[4]
							{
								new Point(0, -1),
								new Point(1, 0),
								new Point(-1, 0),
								new Point(0, 1)
							}, 
							3 => new Point[4]
							{
								new Point(1, 0),
								new Point(0, -1),
								new Point(0, 1),
								new Point(-1, 0)
							}, 
							1 => new Point[4]
							{
								new Point(-1, 0),
								new Point(0, 1),
								new Point(0, -1),
								new Point(1, 0)
							}, 
							_ => new Point[4]
							{
								new Point(-1, 0),
								new Point(1, 0),
								new Point(0, -1),
								new Point(0, 1)
							}, 
						};
						Point point = characterFromName.TilePoint;
						List<Point> list = new List<Point>();
						Point[] array4 = array3;
						for (int k = 0; k < array4.Length; k++)
						{
							Point point2 = array4[k];
							list.Add(new Point(point.X + point2.X, point.Y + point2.Y));
						}
						bool flag = false;
						for (int m = 0; m < 5; m++)
						{
							if (flag)
							{
								break;
							}
							int count = list.Count;
							for (int n = 0; n < count; n++)
							{
								Point point3 = list[0];
								list.RemoveAt(0);
								if (IsWalkableTileCheck(point3))
								{
									if (HasClearanceCheck(point3))
									{
										flag = true;
										point = point3;
										break;
									}
									array4 = array3;
									for (int k = 0; k < array4.Length; k++)
									{
										Point point4 = array4[k];
										list.Add(new Point(point3.X + point4.X, point3.Y + point4.Y));
									}
								}
							}
						}
						if (flag)
						{
							child2.setTilePosition(point.X, point.Y);
							child2.DefaultPosition = characterFromName.DefaultPosition;
							child2.faceDirection(characterFromName.FacingDirection);
							child2.EventActor = true;
							child2.lastCrossroad = new Microsoft.Xna.Framework.Rectangle(point.X * 64, point.Y * 64, 64, 64);
							child2.squareMovementFacingPreference = -1;
							child2.walkInSquare(3, 3, 2000);
							child2.controller = null;
							child2.temporaryController = null;
							@event.actors.Add(child2);
						}
					}
				}
			}
			@event.CurrentCommand++;
			bool HasClearanceCheck(Point point5)
			{
				int num = 1;
				for (int num2 = point5.X - num; num2 <= point5.X + num; num2++)
				{
					for (int num3 = point5.Y - num; num3 <= point5.Y + num; num3++)
					{
						if (@event.temporaryLocation.IsTileBlockedBy(new Vector2(num2, num3)))
						{
							return false;
						}
						foreach (NPC actor in @event.actors)
						{
							if (!(actor is Child))
							{
								Point tilePoint = actor.TilePoint;
								if (tilePoint.X == num2 && tilePoint.Y == num3)
								{
									return false;
								}
							}
						}
					}
				}
				return true;
			}
			bool IsWalkableTileCheck(Point point5)
			{
				return @event.temporaryLocation.isTilePassable(new Location(point5.X, point5.Y), Game1.viewport);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void PlayerControl(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string sequenceId"))
			{
				context.LogErrorAndSkip(error);
			}
			else if (!@event.playerControlSequence)
			{
				@event.setUpPlayerControlSequence(value);
			}
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void RemoveSprite(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetVector2(args, 1, out var value, out var error, integerOnly: true, "Vector2 tile"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Vector2 tilePixel = @event.OffsetPosition(value * 64f);
			Game1.currentLocation.temporarySprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.position == tilePixel);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Viewport(Event @event, string[] args, EventContext context)
		{
			if (ArgUtility.Get(args, 1) == "move")
			{
				if (!ArgUtility.TryGetPoint(args, 2, out var value, out var error, "Point direction") || !ArgUtility.TryGetInt(args, 4, out var value2, out error, "int duration"))
				{
					context.LogErrorAndSkip(error);
					return;
				}
				@event.viewportTarget = new Vector3(value.X, value.Y, value2);
			}
			else
			{
				Point value3 = Point.Zero;
				string value4 = null;
				bool value5 = false;
				string value6 = null;
				string error3;
				if (!int.TryParse(args[1], out var _) && ArgUtility.TryGet(args, 1, out var value7, out var error2, allowBlank: true, "string NPCTarget"))
				{
					value3 = ((!(value7 == "player")) ? @event.getActorByName(value7).TilePoint : Game1.MasterPlayer.TilePoint);
					if (!ArgUtility.TryGetOptional(args, 2, out value4, out error2, null, allowBlank: true, "action") || !ArgUtility.TryGetOptionalBool(args, (value4 == "clamp") ? 3 : 2, out value5, out error2, defaultValue: false, "shouldFade") || !ArgUtility.TryGetOptional(args, (value4 == "clamp") ? 4 : 2, out value6, out error2, null, allowBlank: true, "option"))
					{
						context.LogErrorAndSkip(error2);
					}
				}
				else if (!ArgUtility.TryGetPoint(args, 1, out value3, out error3, "position") || !ArgUtility.TryGetOptional(args, 3, out value4, out error3, null, allowBlank: true, "action") || !ArgUtility.TryGetOptionalBool(args, (value4 == "clamp") ? 4 : 3, out value5, out error3, defaultValue: false, "shouldFade") || !ArgUtility.TryGetOptional(args, (value4 == "clamp") ? 5 : 4, out value6, out error3, null, allowBlank: true, "option"))
				{
					context.LogErrorAndSkip(error3);
					return;
				}
				if (@event.aboveMapSprites != null && value3.X < 0)
				{
					@event.aboveMapSprites.Clear();
					@event.aboveMapSprites = null;
				}
				Game1.viewportFreeze = true;
				int num = @event.OffsetTileX(value3.X);
				int num2 = @event.OffsetTileY(value3.Y);
				if (@event.id == "2146991")
				{
					Point grandpaShrinePosition = Game1.getFarm().GetGrandpaShrinePosition();
					num = grandpaShrinePosition.X;
					num2 = grandpaShrinePosition.Y;
				}
				Game1.viewport.X = num * 64 + 32 - Game1.viewport.Width / 2;
				Game1.viewport.Y = num2 * 64 + 32 - Game1.viewport.Height / 2;
				if (Game1.viewport.X > 0 && Game1.viewport.Width > Game1.currentLocation.Map.DisplayWidth)
				{
					Game1.viewport.X = (Game1.currentLocation.Map.DisplayWidth - Game1.viewport.Width) / 2;
				}
				if (Game1.viewport.Y > 0 && Game1.viewport.Height > Game1.currentLocation.Map.DisplayHeight)
				{
					Game1.viewport.Y = (Game1.currentLocation.Map.DisplayHeight - Game1.viewport.Height) / 2;
				}
				if (value4 == "clamp")
				{
					if (Game1.currentLocation.map.DisplayWidth >= Game1.viewport.Width)
					{
						if (Game1.viewport.X + Game1.viewport.Width > Game1.currentLocation.Map.DisplayWidth)
						{
							Game1.viewport.X = Game1.currentLocation.Map.DisplayWidth - Game1.viewport.Width;
						}
						if (Game1.viewport.X < 0)
						{
							Game1.viewport.X = 0;
						}
					}
					else
					{
						Game1.viewport.X = Game1.currentLocation.Map.DisplayWidth / 2 - Game1.viewport.Width / 2;
					}
					if (Game1.currentLocation.map.DisplayHeight >= Game1.viewport.Height)
					{
						if (Game1.viewport.Y + Game1.viewport.Height > Game1.currentLocation.Map.DisplayHeight)
						{
							Game1.viewport.Y = Game1.currentLocation.Map.DisplayHeight - Game1.viewport.Height;
						}
					}
					else
					{
						Game1.viewport.Y = Game1.currentLocation.Map.DisplayHeight / 2 - Game1.viewport.Height / 2;
					}
					if (Game1.viewport.Y < 0)
					{
						Game1.viewport.Y = 0;
					}
				}
				if (value5)
				{
					Game1.fadeScreenToBlack();
					Game1.fadeToBlackAlpha = 1f;
					Game1.nonWarpFade = true;
				}
				if (value6 == "unfreeze")
				{
					Game1.viewportFreeze = false;
				}
				if (Game1.gameMode == 2)
				{
					Game1.viewport.X = Game1.currentLocation.Map.DisplayWidth - Game1.viewport.Width;
				}
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void BroadcastEvent(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGetOptionalBool(args, 1, out var value, out var error, defaultValue: false, "bool useLocalFarmer"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.farmer == Game1.player)
			{
				if (@event.id == "558291" || @event.id == "558292")
				{
					value = true;
				}
				Game1.multiplayer.broadcastEvent(@event, Game1.currentLocation, Game1.player.positionBeforeEvent, value);
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void AddConversationTopic(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string topicId") || !ArgUtility.TryGetOptionalInt(args, 2, out var value2, out error, 4, "int daysDuration"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (@event.isMemory)
			{
				@event.CurrentCommand++;
				return;
			}
			Game1.player.activeDialogueEvents.TryAdd(value, value2);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void Dump(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: true, "string which"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			if (!(value == "girls"))
			{
				if (!(value == "guys"))
				{
					context.LogErrorAndSkip("unknown ID '" + value + "', expected 'girls' or 'guys'");
					return;
				}
				Game1.player.activeDialogueEvents["dumped_Guys"] = 7;
				Game1.player.activeDialogueEvents["secondChance_Guys"] = 14;
			}
			else
			{
				Game1.player.activeDialogueEvents["dumped_Girls"] = 7;
				Game1.player.activeDialogueEvents["secondChance_Girls"] = 14;
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void EventSeen(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: false, "string eventId") || !ArgUtility.TryGetOptionalBool(args, 2, out var value2, out error, defaultValue: true, "bool seen"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.player.eventsSeen.Toggle(value, value2);
			if (value == @event.id)
			{
				@event.markEventSeen = false;
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.DebugCommandHandlerDelegate" />
		public static void QuestionAnswered(Event @event, string[] args, EventContext context)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: false, "string questionId") || !ArgUtility.TryGetOptionalBool(args, 2, out var value2, out error, defaultValue: true, "bool seen"))
			{
				context.LogErrorAndSkip(error);
				return;
			}
			Game1.player.dialogueQuestionsAnswered.Toggle(value, value2);
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void GainSkill(Event @event, string[] args, EventContext context)
		{
			int skillNumberFromName = Farmer.getSkillNumberFromName(args[1]);
			int num = Convert.ToInt32(args[2]);
			if (Game1.player.GetUnmodifiedSkillLevel(skillNumberFromName) < num)
			{
				Game1.player.setSkillLevel(args[1], num);
			}
			@event.CurrentCommand++;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.EventCommandDelegate" />
		public static void MoveToSoup(Event @event, string[] args, EventContext context)
		{
			if (Game1.year % 2 == 1)
			{
				@event.setUpAdvancedMove(new string[9] { "", "Gus", "false", "0", "-1", "5", "0", "4", "1000" });
				@event.setUpAdvancedMove(new string[5] { "", "Jodi", "false", "0", "-2" });
				@event.setUpAdvancedMove(new string[11]
				{
					"", "Clint", "false", "0", "1", "-1", "0", "0", "3", "-2",
					"0"
				});
				@event.setUpAdvancedMove(new string[5] { "", "Emily", "false", "3", "0" });
				@event.setUpAdvancedMove(new string[7] { "", "Pam", "false", "0", "2", "7", "0" });
			}
			else
			{
				@event.setUpAdvancedMove(new string[5] { "", "Pierre", "false", "3", "0" });
				@event.setUpAdvancedMove(new string[9] { "", "Pam", "false", "0", "2", "-4", "0", "0", "1" });
				@event.setUpAdvancedMove(new string[9] { "", "Abigail", "false", "4", "0", "0", "-3", "1", "4000" });
				@event.setUpAdvancedMove(new string[9] { "", "Alex", "false", "-5", "0", "0", "-1", "3", "2000" });
				@event.setUpAdvancedMove(new string[5] { "", "Gus", "false", "0", "-1" });
			}
			@event.CurrentCommand++;
		}
	}

	/// <summary>The event commands indexed by name.</summary>
	/// <remarks>Command names are case-insensitive.</remarks>
	protected static readonly Dictionary<string, EventCommandDelegate> Commands = new Dictionary<string, EventCommandDelegate>(StringComparer.OrdinalIgnoreCase);

	/// <summary>Alternate names for event commands.</summary>
	protected static readonly Dictionary<string, string> CommandAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>The registered command names.</summary>
	protected static readonly HashSet<string> CommandNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>The event preconditions indexed by name.</summary>
	/// <remarks>Precondition names are case-<strong>sensitive</strong>.</remarks>
	protected static readonly Dictionary<string, EventPreconditionDelegate> Preconditions = new Dictionary<string, EventPreconditionDelegate>(StringComparer.OrdinalIgnoreCase);

	/// <summary>Alternate names for event preconditions (e.g. shorthand or acronyms).</summary>
	/// <remarks>Aliases are case-sensitive for compatibility with older preconditions like 'h' vs 'H'.</remarks>
	private static readonly Dictionary<string, string> PreconditionAliases = new Dictionary<string, string>();

	private const float timeBetweenSpeech = 500f;

	public const string festivalTextureName = "Maps\\Festivals";

	private string festivalDataAssetName;

	/// <summary>
	///   The unique identifier for the event, if available. This may be...
	///   <list type="bullet">
	///     <item><description>for a regular event, the unique event ID from its data file (i.e. the first number in its entry key);</description></item>
	///     <item><description>for a generated event, an <see cref="T:StardewValley.Constants.EventIds" /> constant;</description></item>
	///     <item><description>for a festival, <c>festival_{asset name}</c> (like <c>festival_fall16</c>);</description></item>
	///     <item><description>else <see cref="F:StardewValley.Constants.EventIds.Unknown" />.</description></item>
	///   </list>
	/// </summary>
	public string id = "-1";

	/// <summary>The data asset name from which the event script was taken, or <c>null</c> for a generated event.</summary>
	public string fromAssetName;

	public bool isFestival;

	public bool isWedding;

	public bool isMemory;

	/// <summary>Whether the player can skip the rest of the event.</summary>
	public bool skippable;

	/// <summary>The actions to perform when the event is skipped, if any.</summary>
	public string[] actionsOnSkip;

	public bool skipped;

	public bool forked;

	public bool eventSwitched;

	/// <summary>Whether we need to notify the dedicated server once the event finishes running.</summary>
	internal bool notifyWhenDone;

	/// <summary>The location name sent to the dedicated server to identify an event.</summary>
	internal string notifyLocationName;

	/// <summary>Whether the location name sent to the dedicated server corresponds to a structure.</summary>
	internal byte notifyLocationIsStructure;

	private readonly LocalizedContentManager festivalContent = Game1.content.CreateTemporary();

	public string[] eventCommands;

	public int currentCommand;

	private Dictionary<string, Vector3> actorPositionsAfterMove;

	private float timeAccumulator;

	private Vector3 viewportTarget;

	private Color previousAmbientLight;

	private HashSet<long> festivalWinners = new HashSet<long>();

	private GameLocation temporaryLocation;

	private Dictionary<string, string> festivalData;

	private Texture2D _festivalTexture;

	private bool drawTool;

	private string hostMessageKey;

	private int previousFacingDirection = -1;

	private int previousAnswerChoice = -1;

	private bool startSecretSantaAfterDialogue;

	private List<Farmer> iceFishWinners;

	protected static LocalizedContentManager FestivalReadContentLoader;

	protected bool _playerControlSequence;

	protected bool _repeatingLocationSpecificCommand;

	[NonInstancedStatic]
	public static HashSet<string> invalidFestivals = new HashSet<string>();

	public List<NPC> actors = new List<NPC>();

	public List<Object> props = new List<Object>();

	public List<Prop> festivalProps = new List<Prop>();

	public List<Farmer> farmerActors = new List<Farmer>();

	public Dictionary<string, Dictionary<ISalable, ItemStockInformation>> festivalShops;

	public List<NPCController> npcControllers;

	internal NPC festivalHost;

	public NPC secretSantaRecipient;

	public NPC mySecretSanta;

	public TemporaryAnimatedSpriteList underwaterSprites;

	public TemporaryAnimatedSpriteList aboveMapSprites;

	/// <summary>The custom sounds started during the event via <see cref="M:StardewValley.Event.DefaultCommands.PlaySound(StardewValley.Event,System.String[],StardewValley.EventContext)" />.</summary>
	public IDictionary<string, List<ICue>> CustomSounds = new Dictionary<string, List<ICue>>();

	public ICustomEventScript currentCustomEventScript;

	public bool simultaneousCommand;

	public int farmerAddedSpeed;

	public int int_useMeForAnything;

	public int int_useMeForAnything2;

	public float float_useMeForAnything;

	public string playerControlSequenceID;

	public string spriteTextToDraw;

	public bool showActiveObject;

	public bool continueAfterMove;

	public bool specialEventVariable1;

	public bool specialEventVariable2;

	public bool showGroundObjects = true;

	public bool doingSecretSanta;

	public bool showWorldCharacters;

	public bool ignoreObjectCollisions = true;

	public Point playerControlTargetTile;

	public List<Vector2> characterWalkLocations = new List<Vector2>();

	public Vector2 eventPositionTileOffset = Vector2.Zero;

	public int festivalTimer;

	public int grangeScore = -1000;

	public bool grangeJudged;

	/// <summary>Used to offset positions specified in events.</summary>
	public bool ignoreTileOffsets;

	private Stopwatch stopWatch;

	public LocationRequest exitLocation;

	public Action onEventFinished;

	/// <summary>Whether to add this event's ID to <see cref="F:StardewValley.Farmer.eventsSeen" /> when it ends, if it has a valid ID.</summary>
	/// <remarks>This has no effect on <see cref="F:StardewValley.Game1.eventsSeenSinceLastLocationChange" />, which is updated regardless (if it has a valid ID) to prevent event loops.</remarks>
	public bool markEventSeen = true;

	private bool eventFinished;

	private bool gotPet;

	public string FestivalName
	{
		get
		{
			if (!TryGetFestivalDataForYear("name", out var data))
			{
				return "";
			}
			return data;
		}
	}

	public bool playerControlSequence
	{
		get
		{
			return _playerControlSequence;
		}
		set
		{
			if (_playerControlSequence != value)
			{
				_playerControlSequence = value;
				if (!_playerControlSequence)
				{
					OnPlayerControlSequenceEnd(playerControlSequenceID);
				}
			}
		}
	}

	public Farmer farmer
	{
		get
		{
			if (farmerActors.Count <= 0)
			{
				return Game1.player;
			}
			return farmerActors[0];
		}
	}

	public Texture2D festivalTexture
	{
		get
		{
			if (_festivalTexture == null)
			{
				_festivalTexture = festivalContent.Load<Texture2D>("Maps\\Festivals");
			}
			return _festivalTexture;
		}
	}

	public int CurrentCommand
	{
		get
		{
			return currentCommand;
		}
		set
		{
			currentCommand = value;
		}
	}

	/// <summary>Register an event command.</summary>
	/// <param name="name">The command name that can be used in event scripts. This is case-insensitive.</param>
	/// <param name="action">The handler to call when the command is used.</param>
	public static void RegisterCommand(string name, EventCommandDelegate action)
	{
		SetupEventCommandsIfNeeded();
		if (Commands.ContainsKey(name))
		{
			Game1.log.Warn("Warning: event command " + name + " is already defined and will be overwritten.");
		}
		Commands[name] = action;
		CommandNames.Add(name);
		Game1.log.Verbose("Registered event command: " + name);
	}

	/// <summary>Register an alternate name for an event command.</summary>
	/// <param name="alias">The alternate name. This is case-insensitive.</param>
	/// <param name="commandName">The original command name to alias. This is case-insensitive.</param>
	public static void RegisterCommandAlias(string alias, string commandName)
	{
		SetupEventCommandsIfNeeded();
		string value;
		if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(commandName))
		{
			Game1.log.Error($"Can't register event command alias '{alias}' for '{commandName}' because the alias and command name must both be non-null and non-empty strings.");
		}
		else if (Commands.ContainsKey(alias))
		{
			Game1.log.Error($"Can't register event command alias '{alias}' for command '{commandName}', because there's a command with that name.");
		}
		else if (CommandAliases.TryGetValue(alias, out value))
		{
			Game1.log.Error($"Can't register event command alias '{alias}' for command '{commandName}', because that's already an alias for '{value}'.");
		}
		else if (!Commands.ContainsKey(commandName))
		{
			Game1.log.Error($"Can't register event command alias '{alias}' for command '{commandName}', because there's no such command.");
		}
		else
		{
			CommandAliases[alias] = commandName;
		}
	}

	/// <summary>Get the actual command name which matches an input command or alias name.</summary>
	/// <param name="name">The command or alias name to resolve.</param>
	/// <param name="actualName">The resolved command name.</param>
	/// <returns>Returns whether a matching command was found.</returns>
	/// <remarks>For example, this can be used to resolve an alias (like <c>mailReceived</c> → <c>AddMailReceived</c>) or normalize the capitalization (like <c>itemnamed</c> → <c>ItemNamed</c>).</remarks>
	public static bool TryResolveCommandName(string name, out string actualName)
	{
		SetupEventCommandsIfNeeded();
		if (CommandAliases.TryGetValue(name, out actualName))
		{
			return true;
		}
		if (CommandNames.TryGetValue(name, out actualName))
		{
			return true;
		}
		actualName = null;
		return false;
	}

	/// <summary>Register an event precondition.</summary>
	/// <param name="name">The precondition key that can be used in event precondition strings. This is case-insensitive.</param>
	/// <param name="action">The handler to call when the precondition is used.</param>
	public static void RegisterPrecondition(string name, EventPreconditionDelegate action)
	{
		SetupEventCommandsIfNeeded();
		if (Preconditions.ContainsKey(name))
		{
			Game1.log.Warn("Warning: event precondition " + name + " is already defined and will be overwritten.");
		}
		if (PreconditionAliases.Remove(name))
		{
			Game1.log.Warn("Warning: '" + name + "' was previously registered as a precondition alias. The alias was removed.");
		}
		Preconditions[name] = action;
		Game1.log.Verbose("Registered precondition: " + name);
	}

	/// <summary>Register an alternate name for an event precondition.</summary>
	/// <param name="alias">The alternate name. This is <strong>case-sensitive</strong> for legacy reasons.</param>
	/// <param name="preconditionName">The original precondition name to alias. This is case-insensitive.</param>
	public static void RegisterPreconditionAlias(string alias, string preconditionName)
	{
		SetupEventCommandsIfNeeded();
		string value;
		if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(preconditionName))
		{
			Game1.log.Error($"Can't register event precondition alias '{alias}' for '{preconditionName}' because the alias and precondition name must both be non-null and non-empty strings.");
		}
		else if (Preconditions.ContainsKey(alias))
		{
			Game1.log.Error($"Can't register event precondition alias '{alias}' for precondition '{preconditionName}', because there's a precondition with that name.");
		}
		else if (PreconditionAliases.TryGetValue(alias, out value))
		{
			Game1.log.Error($"Can't register event precondition alias '{alias}' for precondition '{preconditionName}', because that's already an alias for '{value}'.");
		}
		else if (!Preconditions.ContainsKey(preconditionName))
		{
			Game1.log.Error($"Can't register event precondition alias '{alias}' for precondition '{preconditionName}', because there's no such precondition.");
		}
		else
		{
			PreconditionAliases[alias] = preconditionName;
		}
	}

	/// <summary>Register the vanilla event commands and preconditions if they haven't already been registered.</summary>
	private static void SetupEventCommandsIfNeeded()
	{
		MethodInfo[] array;
		if (Commands.Count == 0)
		{
			MethodInfo[] methods = typeof(DefaultCommands).GetMethods(BindingFlags.Static | BindingFlags.Public);
			array = methods;
			foreach (MethodInfo methodInfo in array)
			{
				EventCommandDelegate value = (EventCommandDelegate)Delegate.CreateDelegate(typeof(EventCommandDelegate), methodInfo);
				Commands.Add(methodInfo.Name, value);
				CommandNames.Add(methodInfo.Name);
			}
			array = methods;
			foreach (MethodInfo methodInfo2 in array)
			{
				OtherNamesAttribute customAttribute = methodInfo2.GetCustomAttribute<OtherNamesAttribute>();
				if (customAttribute != null)
				{
					string[] aliases = customAttribute.Aliases;
					for (int j = 0; j < aliases.Length; j++)
					{
						RegisterCommandAlias(aliases[j], methodInfo2.Name);
					}
				}
			}
		}
		if (Preconditions.Count != 0)
		{
			return;
		}
		MethodInfo[] methods2 = typeof(Preconditions).GetMethods(BindingFlags.Static | BindingFlags.Public);
		array = methods2;
		foreach (MethodInfo methodInfo3 in array)
		{
			EventPreconditionDelegate value2 = (EventPreconditionDelegate)Delegate.CreateDelegate(typeof(EventPreconditionDelegate), methodInfo3);
			Preconditions[methodInfo3.Name] = value2;
		}
		array = methods2;
		foreach (MethodInfo methodInfo4 in array)
		{
			OtherNamesAttribute customAttribute2 = methodInfo4.GetCustomAttribute<OtherNamesAttribute>();
			if (customAttribute2 != null)
			{
				string[] aliases = customAttribute2.Aliases;
				for (int j = 0; j < aliases.Length; j++)
				{
					RegisterPreconditionAlias(aliases[j], methodInfo4.Name);
				}
			}
		}
	}

	/// <summary>Get the handler for a precondition key, if any.</summary>
	/// <param name="key">The precondition key, which can be either the case-insensitive canonical name (like <c>DaysPlayed</c>) or case-sensitive alias (like <c>j</c>).</param>
	/// <param name="handler">The precondition handler, if found.</param>
	/// <returns>Returns whether a handler was found for the precondition key.</returns>
	public static bool TryGetPreconditionHandler(string key, out EventPreconditionDelegate handler)
	{
		SetupEventCommandsIfNeeded();
		if (PreconditionAliases.TryGetValue(key, out var value))
		{
			key = value;
		}
		return Preconditions.TryGetValue(key, out handler);
	}

	/// <summary>Get whether an event precondition matches the current context.</summary>
	/// <param name="location">The location which is checking the event.</param>
	/// <param name="eventId">The unique ID for the event being checked.</param>
	/// <param name="precondition">The event precondition string, including the precondition name.</param>
	public static bool CheckPrecondition(GameLocation location, string eventId, string precondition)
	{
		string[] array = ArgUtility.SplitBySpaceQuoteAware(precondition);
		string text = array[0];
		bool flag = true;
		if (text.StartsWith('!'))
		{
			text = text.Substring(1);
			flag = false;
		}
		if (!TryGetPreconditionHandler(text, out var handler))
		{
			Game1.log.Warn("Unknown precondition for event " + eventId + ": " + precondition);
			return false;
		}
		try
		{
			return handler(location, eventId, array) == flag;
		}
		catch (Exception exception)
		{
			Game1.log.Error($"Failed checking precondition '{precondition}' for event {eventId}.", exception);
			return false;
		}
	}

	/// <summary>Get the handler for an event command key, if any.</summary>
	/// <param name="key">The event command key, which can be either the case-insensitive canonical name (like <c>AddMailReceived</c>) or case-sensitive alias (like <c>mailReceived</c>).</param>
	/// <param name="handler">The event command handler, if found.</param>
	/// <returns>Returns whether a handler was found for the event command key.</returns>
	public static bool TryGetEventCommandHandler(string key, out EventCommandDelegate handler)
	{
		if (CommandAliases.TryGetValue(key, out var value))
		{
			key = value;
		}
		return Commands.TryGetValue(key, out handler);
	}

	/// <summary>Try to run an event command for the current event.</summary>
	/// <param name="location">The location in which the event is running.</param>
	/// <param name="time">The current game execution time.</param>
	/// <param name="args">The space-delimited event command string, including the command name.</param>
	public virtual void tryEventCommand(GameLocation location, GameTime time, string[] args)
	{
		string text = ArgUtility.Get(args, 0);
		if (string.IsNullOrWhiteSpace(text))
		{
			LogCommandErrorAndSkip(args, "can't run an empty or null command");
			return;
		}
		if (!TryGetEventCommandHandler(text, out var handler))
		{
			LogCommandErrorAndSkip(args, "unknown command '" + text + "'");
			return;
		}
		try
		{
			EventContext context = new EventContext(this, location, time, args);
			handler(this, args, context);
		}
		catch (Exception e)
		{
			LogErrorAndHalt(e);
		}
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="eventString">The raw event script.</param>
	/// <param name="farmerActor">The player to add as an actor in the event script, or <c>null</c> to use <see cref="P:StardewValley.Game1.player" />.</param>
	public Event(string eventString, Farmer farmerActor = null)
		: this(eventString, null, "-1", farmerActor)
	{
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="eventString">The raw event script.</param>
	/// <param name="fromAssetName">The data asset name from which the event script was taken, or <c>null</c> for a generated event.</param>
	/// <param name="eventID">The event's unique ID from the event data files, if known. This may be a number matching one of the <see cref="T:StardewValley.Event" /> constants in <see cref="T:StardewValley.Constants.EventIds" /> for a generated event.</param>
	/// <param name="farmerActor">The player to add as an actor in the event script, or <c>null</c> to use <see cref="P:StardewValley.Game1.player" />.</param>
	public Event(string eventString, string fromAssetName, string eventID, Farmer farmerActor = null)
		: this()
	{
		this.fromAssetName = fromAssetName;
		id = eventID;
		eventCommands = ParseCommands(eventString, farmerActor);
		actorPositionsAfterMove = new Dictionary<string, Vector3>();
		previousAmbientLight = Game1.ambientLight;
		if (farmerActor != null)
		{
			farmerActors.Add(farmerActor);
		}
		farmer.canOnlyWalk = true;
		farmer.showNotCarrying();
		drawTool = false;
		if (eventID == "-2")
		{
			isWedding = true;
		}
	}

	/// <summary>Construct an instance.</summary>
	public Event()
	{
		SetupEventCommandsIfNeeded();
	}

	~Event()
	{
		notifyDone();
	}

	public static void OnNewDay()
	{
		FestivalReadContentLoader?.Unload();
	}

	/// <summary>Load the raw data for a festival, if it exists and is valid.</summary>
	/// <param name="festival">The festival ID to load, matching the asset name under <c>Data/Festivals</c> (like <samp>spring13</samp>).</param>
	/// <param name="assetName">The asset name for the loaded festival data.</param>
	/// <param name="data">The loaded festival data.</param>
	/// <param name="locationName">The location name in which the festival takes place.</param>
	/// <param name="startTime">The time of day when the festival opens.</param>
	/// <param name="endTime">The time of day when the festival closes.</param>
	/// <returns>Returns whether the festival data was loaded successfully.</returns>
	public static bool tryToLoadFestivalData(string festival, out string assetName, out Dictionary<string, string> data, out string locationName, out int startTime, out int endTime)
	{
		assetName = "Data\\Festivals\\" + festival;
		data = null;
		locationName = null;
		startTime = 0;
		endTime = 0;
		if (invalidFestivals.Contains(festival))
		{
			return false;
		}
		if (FestivalReadContentLoader == null)
		{
			FestivalReadContentLoader = Game1.content.CreateTemporary();
		}
		try
		{
			if (!FestivalReadContentLoader.DoesAssetExist<Dictionary<string, string>>(assetName))
			{
				invalidFestivals.Add(festival);
				return false;
			}
			data = FestivalReadContentLoader.Load<Dictionary<string, string>>(assetName);
		}
		catch
		{
			invalidFestivals.Add(festival);
			return false;
		}
		if (!data.TryGetValue("conditions", out var value))
		{
			Game1.log.Error("Festival '" + festival + "' doesn't have the required 'conditions' data field.");
			return false;
		}
		string[] array = LegacyShims.SplitAndTrim(value, '/');
		if (!ArgUtility.TryGet(array, 0, out locationName, out var error, allowBlank: false, "locationName") || !ArgUtility.TryGet(array, 1, out var value2, out error, allowBlank: false, "string rawTimeSpan"))
		{
			Game1.log.Error($"Festival '{festival}' has preconditions '{value}' which couldn't be parsed: {error}.");
			return false;
		}
		string[] array2 = ArgUtility.SplitBySpace(value2);
		if (!ArgUtility.TryGetInt(array2, 0, out startTime, out error, "startTime") || !ArgUtility.TryGetInt(array2, 1, out endTime, out error, "endTime"))
		{
			Game1.log.Error($"Festival '{festival}' has preconditions '{value}' with time range '{string.Join(" ", array2)}' which couldn't be parsed: {error}.");
			return false;
		}
		return true;
	}

	/// <summary>Load a festival if it exists and its preconditions match the current time and the local player's current location.</summary>
	/// <param name="festival">The festival ID to load, matching the asset name under <c>Data/Festivals</c> (like <samp>spring13</samp>).</param>
	/// <param name="ev">The loaded festival event, if it was loaded successfully.</param>
	/// <returns>Returns whether the festival was loaded successfully.</returns>
	public static bool tryToLoadFestival(string festival, out Event ev)
	{
		ev = null;
		if (!tryToLoadFestivalData(festival, out var assetName, out var data, out var locationName, out var startTime, out var endTime))
		{
			return false;
		}
		if (locationName != Game1.currentLocation.Name || Game1.timeOfDay < startTime || Game1.timeOfDay >= endTime)
		{
			return false;
		}
		ev = new Event
		{
			id = "festival_" + festival,
			isFestival = true,
			festivalDataAssetName = assetName,
			festivalData = data,
			actorPositionsAfterMove = new Dictionary<string, Vector3>(),
			previousAmbientLight = Game1.ambientLight
		};
		ev.festivalData["file"] = festival;
		if (!ev.TryGetFestivalDataForYear("set-up", out var data2))
		{
			Game1.log.Error("Festival " + ev.id + " doesn't have the required 'set-up' data field.");
		}
		ev.eventCommands = ParseCommands(data2, ev.farmer);
		Game1.player.festivalScore = 0;
		Game1.setRichPresence("festival", festival);
		return true;
	}

	/// <summary>Try to get an NPC dialogue from the festival data, automatically adjusted to use the closest <c>{key}_y{year}</c> variant if any.</summary>
	/// <param name="npc">The NPC for which to get a dialogue.</param>
	/// <param name="key">The base field key for the dialogue text.</param>
	/// <param name="data">The resulting dialogue instance, or <c>null</c> if the key wasn't found.</param>
	/// <returns>Returns whether a matching dialogue was found.</returns>
	public bool TryGetFestivalDialogueForYear(NPC npc, string key, out Dialogue dialogue)
	{
		if (TryGetFestivalDataForYear(key, out var data, out var actualKey))
		{
			dialogue = new Dialogue(npc, festivalDataAssetName + ":" + actualKey, data);
			return true;
		}
		dialogue = null;
		return false;
	}

	/// <summary>Try to get a value from the festival data, automatically adjusted to use the closest <c>{key}_y{year}</c> variant if any.</summary>
	/// <param name="key">The base field key.</param>
	/// <param name="data">The resolved data, or <c>null</c> if the key wasn't found.</param>
	/// <param name="actualKey">The resolved field key, including the variant suffix if applicable, or <c>null</c> if the key wasn't found.</param>
	/// <returns>Returns whether a matching field was found.</returns>
	public bool TryGetFestivalDataForYear(string key, out string data, out string actualKey)
	{
		if (festivalData == null)
		{
			data = null;
			actualKey = null;
			return false;
		}
		int num = 1;
		while (festivalData.ContainsKey($"{key}_y{num + 1}"))
		{
			num++;
		}
		int num2 = Game1.year % num;
		if (num2 == 0)
		{
			num2 = num;
		}
		actualKey = ((num2 > 1) ? $"{key}_y{num2}" : key);
		if (festivalData.TryGetValue(actualKey, out data))
		{
			return true;
		}
		actualKey = null;
		data = null;
		return false;
	}

	/// <summary>Get a value from the festival data, automatically adjusted to use the closest <c>{key}_y{year}</c> variant if any.</summary>
	/// <param name="key">The base field key.</param>
	/// <param name="data">The resolved data, or <c>null</c> if the key wasn't found.</param>
	/// <returns>Returns whether a matching field was found.</returns>
	public bool TryGetFestivalDataForYear(string key, out string data)
	{
		string actualKey;
		return TryGetFestivalDataForYear(key, out data, out actualKey);
	}

	/// <summary>Set the location and tile position at which to warp the player once the event ends.</summary>
	/// <param name="warp">The warp whose endpoint to use as the exit location.</param>
	public void setExitLocation(Warp warp)
	{
		setExitLocation(warp.TargetName, warp.TargetX, warp.TargetY);
	}

	/// <summary>Set the location and tile position at which to warp the player once the event ends.</summary>
	/// <param name="location">The location name.</param>
	/// <param name="x">The X tile position.</param>
	/// <param name="y">The Y tile position.</param>
	public void setExitLocation(string location, int x, int y)
	{
		if (string.IsNullOrEmpty(Game1.player.locationBeforeForcedEvent.Value))
		{
			exitLocation = Game1.getLocationRequest(location);
			Game1.player.positionBeforeEvent = new Vector2(x, y);
		}
	}

	public void endBehaviors(GameLocation location = null)
	{
		endBehaviors(LegacyShims.EmptyArray<string>(), location ?? Game1.currentLocation);
	}

	public void endBehaviors(string[] args, GameLocation location)
	{
		if (Game1.getMusicTrackName().Contains(Game1.currentSeason) && ArgUtility.Get(eventCommands, 0) != "continue")
		{
			Game1.stopMusicTrack(MusicContext.Default);
		}
		switch (ArgUtility.Get(args, 1))
		{
		case "qiSummitCheat":
			Game1.playSound("death");
			Game1.player.health = -1;
			Game1.player.position.X = -99999f;
			Game1.background = null;
			Game1.viewport.X = -999999;
			Game1.viewport.Y = -999999;
			Game1.viewportHold = 6000;
			Game1.eventOver = true;
			CurrentCommand += 2;
			Game1.screenGlowHold = false;
			Game1.screenGlowOnce(Color.Black, hold: true, 1f, 1f);
			break;
		case "Leo":
			if (!isMemory)
			{
				Game1.addMailForTomorrow("leoMoved", noLetter: true, sendToEveryone: true);
				Game1.player.team.requestLeoMove.Fire();
			}
			break;
		case "bed":
			Game1.player.Position = Game1.player.mostRecentBed + new Vector2(0f, 64f);
			break;
		case "newDay":
			Game1.player.faceDirection(2);
			setExitLocation(Game1.player.homeLocation.Value, (int)Game1.player.mostRecentBed.X / 64, (int)Game1.player.mostRecentBed.Y / 64);
			if (!Game1.IsMultiplayer)
			{
				exitLocation.OnWarp += delegate
				{
					Game1.NewDay(0f);
					Game1.player.currentLocation.lastTouchActionLocation = new Vector2((int)Game1.player.mostRecentBed.X / 64, (int)Game1.player.mostRecentBed.Y / 64);
				};
			}
			Game1.player.completelyStopAnimatingOrDoingAction();
			if (Game1.player.bathingClothes.Value)
			{
				Game1.player.changeOutOfSwimSuit();
			}
			Game1.player.swimming.Value = false;
			Game1.player.CanMove = false;
			Game1.changeMusicTrack("none");
			break;
		case "invisibleWarpOut":
		{
			if (!ArgUtility.TryGet(args, 2, out var value4, out var error3, allowBlank: false, "string npcName"))
			{
				LogCommandError(args, error3);
				break;
			}
			NPC characterFromName3 = Game1.getCharacterFromName(value4);
			if (characterFromName3 == null)
			{
				LogCommandError(args, "NPC '" + value4 + "' not found");
				break;
			}
			characterFromName3.IsInvisible = true;
			characterFromName3.daysUntilNotInvisible = 1;
			setExitLocation(location.GetFirstPlayerWarp());
			Game1.fadeScreenToBlack();
			Game1.eventOver = true;
			CurrentCommand += 2;
			Game1.screenGlowHold = false;
			break;
		}
		case "invisible":
		{
			if (!ArgUtility.TryGet(args, 2, out var value7, out var error5, allowBlank: false, "string npcName"))
			{
				LogCommandError(args, error5);
			}
			else if (!isMemory)
			{
				NPC characterFromName5 = Game1.getCharacterFromName(value7);
				if (characterFromName5 == null)
				{
					LogCommandError(args, "NPC '" + value7 + "' not found");
					break;
				}
				characterFromName5.IsInvisible = true;
				characterFromName5.daysUntilNotInvisible = 1;
			}
			break;
		}
		case "warpOut":
			setExitLocation(location.GetFirstPlayerWarp());
			Game1.eventOver = true;
			CurrentCommand += 2;
			Game1.screenGlowHold = false;
			break;
		case "dialogueWarpOut":
		{
			if (!ArgUtility.TryGet(args, 2, out var value2, out var error2, allowBlank: false, "string npcName") || !ArgUtility.TryGet(args, 3, out var value3, out error2, allowBlank: true, "string dialogue"))
			{
				LogCommandError(args, error2);
				break;
			}
			setExitLocation(location.GetFirstPlayerWarp());
			NPC characterFromName2 = Game1.getCharacterFromName(value2);
			if (characterFromName2 == null)
			{
				LogCommandError(args, "NPC '" + value2 + "' not found");
				break;
			}
			characterFromName2.CurrentDialogue.Clear();
			characterFromName2.CurrentDialogue.Push(new Dialogue(characterFromName2, null, value3));
			Game1.eventOver = true;
			CurrentCommand += 2;
			Game1.screenGlowHold = false;
			break;
		}
		case "Maru1":
			(Game1.getCharacterFromName("Demetrius") ?? getActorByName("Demetrius"))?.setNewDialogue("Strings\\StringsFromCSFiles:Event.cs.1018");
			(Game1.getCharacterFromName("Maru") ?? getActorByName("Maru"))?.setNewDialogue("Strings\\StringsFromCSFiles:Event.cs.1020");
			setExitLocation(location.GetFirstPlayerWarp());
			Game1.fadeScreenToBlack();
			Game1.eventOver = true;
			CurrentCommand += 2;
			break;
		case "wedding":
		{
			Game1.RequireCharacter("Lewis").CurrentDialogue.Push(new Dialogue(Game1.getCharacterFromName("Lewis"), "Strings\\StringsFromCSFiles:Event.cs.1025"));
			FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(Game1.player);
			Point porchStandingSpot = homeOfFarmer.getPorchStandingSpot();
			if (homeOfFarmer is Cabin)
			{
				setExitLocation("Farm", porchStandingSpot.X + 1, porchStandingSpot.Y);
			}
			else
			{
				setExitLocation("Farm", porchStandingSpot.X - 1, porchStandingSpot.Y);
			}
			if (!Game1.IsMasterGame)
			{
				break;
			}
			NPC characterFromName = Game1.getCharacterFromName(farmer.spouse);
			if (characterFromName != null)
			{
				characterFromName.ClearSchedule();
				characterFromName.ignoreScheduleToday = true;
				characterFromName.shouldPlaySpousePatioAnimation.Value = false;
				characterFromName.controller = null;
				characterFromName.temporaryController = null;
				characterFromName.currentMarriageDialogue.Clear();
				Game1.warpCharacter(characterFromName, "Farm", Utility.getHomeOfFarmer(farmer).getPorchStandingSpot());
				characterFromName.faceDirection(2);
				if (Game1.content.LoadStringReturnNullIfNotFound("Strings\\StringsFromCSFiles:" + characterFromName.Name + "_AfterWedding") != null)
				{
					characterFromName.addMarriageDialogue("Strings\\StringsFromCSFiles", characterFromName.Name + "_AfterWedding", false);
				}
				else
				{
					characterFromName.addMarriageDialogue("Strings\\StringsFromCSFiles", "Game1.cs.2782", false);
				}
			}
			break;
		}
		case "dialogue":
		{
			if (!ArgUtility.TryGet(args, 2, out var value5, out var error4, allowBlank: false, "string npcName") || !ArgUtility.TryGet(args, 3, out var value6, out error4, allowBlank: true, "string dialogue"))
			{
				LogCommandError(args, error4);
				break;
			}
			NPC characterFromName4 = Game1.getCharacterFromName(value5);
			if (characterFromName4 == null)
			{
				LogCommandError(args, "NPC '" + value5 + "' not found");
				break;
			}
			characterFromName4.shouldSayMarriageDialogue.Value = false;
			characterFromName4.currentMarriageDialogue.Clear();
			characterFromName4.CurrentDialogue.Clear();
			characterFromName4.CurrentDialogue.Push(new Dialogue(characterFromName4, null, value6));
			break;
		}
		case "beginGame":
			Game1.gameMode = 3;
			setExitLocation("FarmHouse", 9, 9);
			Game1.NewDay(1000f);
			exitEvent();
			Game1.eventFinished();
			return;
		case "credits":
			Game1.debrisWeather.Clear();
			Game1.isDebrisWeather = false;
			Game1.changeMusicTrack("wedding", track_interruptable: false, MusicContext.Event);
			Game1.gameMode = 10;
			CurrentCommand += 2;
			break;
		case "position":
		{
			if (!ArgUtility.TryGetVector2(args, 2, out var value, out var error, integerOnly: true, "Vector2 position"))
			{
				LogCommandError(args, error);
			}
			else if (string.IsNullOrEmpty(Game1.player.locationBeforeForcedEvent.Value))
			{
				Game1.player.positionBeforeEvent = value;
			}
			break;
		}
		case "islandDepart":
		{
			Game1.player.orientationBeforeEvent = 2;
			string whereIsTodaysFest = Game1.whereIsTodaysFest;
			if (!(whereIsTodaysFest == "Beach"))
			{
				if (whereIsTodaysFest == "Town")
				{
					Game1.player.orientationBeforeEvent = 3;
					setExitLocation("BusStop", 43, 23);
				}
				else
				{
					setExitLocation("BoatTunnel", 6, 9);
				}
			}
			else
			{
				Game1.player.orientationBeforeEvent = 0;
				setExitLocation("Town", 54, 109);
			}
			GameLocation left_location = Game1.currentLocation;
			exitLocation.OnLoad += delegate
			{
				foreach (NPC actor in actors)
				{
					actor.shouldShadowBeOffset = true;
					actor.drawOffset.Y = 0f;
				}
				foreach (Farmer farmerActor in farmerActors)
				{
					farmerActor.shouldShadowBeOffset = true;
					farmerActor.drawOffset.Y = 0f;
				}
				Game1.player.drawOffset = Vector2.Zero;
				Game1.player.shouldShadowBeOffset = false;
				if (left_location is IslandSouth islandSouth)
				{
					islandSouth.ResetBoat();
				}
			};
			break;
		}
		case "tunnelDepart":
			if (Game1.player.hasOrWillReceiveMail("seenBoatJourney"))
			{
				Game1.warpFarmer("IslandSouth", 21, 43, 0);
			}
			break;
		}
		exitEvent();
	}

	public void exitEvent()
	{
		eventFinished = true;
		if (!string.IsNullOrEmpty(id) && id != "-1")
		{
			if (markEventSeen)
			{
				Game1.player.eventsSeen.Add(id);
			}
			Game1.eventsSeenSinceLastLocationChange.Add(id);
		}
		notifyDone();
		Game1.stopMusicTrack(MusicContext.Event);
		StopTrackedSounds();
		if (id == "1039573")
		{
			Game1.addMail("addedParrotBoy", noLetter: true, sendToEveryone: true);
			Game1.player.team.requestAddCharacterEvent.Fire("Leo");
		}
		Game1.player.ignoreCollisions = false;
		Game1.player.canOnlyWalk = false;
		Game1.nonWarpFade = true;
		if (!Game1.fadeIn || Game1.fadeToBlackAlpha >= 1f)
		{
			Game1.fadeScreenToBlack();
		}
		Game1.eventOver = true;
		Game1.fadeToBlack = true;
		Game1.setBGColor(5, 3, 4);
		CurrentCommand += 2;
		Game1.screenGlowHold = false;
		if (isFestival)
		{
			Game1.timeOfDayAfterFade = 2200;
			if (festivalData != null && (isSpecificFestival("summer28") || isSpecificFestival("fall27")))
			{
				Game1.timeOfDayAfterFade = 2400;
			}
			int num = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, Game1.timeOfDayAfterFade);
			if (Game1.IsMasterGame)
			{
				Point mainFarmHouseEntry = Game1.getFarm().GetMainFarmHouseEntry();
				setExitLocation("Farm", mainFarmHouseEntry.X, mainFarmHouseEntry.Y);
			}
			else
			{
				Point porchStandingSpot = Utility.getHomeOfFarmer(Game1.player).getPorchStandingSpot();
				setExitLocation("Farm", porchStandingSpot.X, porchStandingSpot.Y);
			}
			Game1.player.toolOverrideFunction = null;
			isFestival = false;
			foreach (NPC actor in actors)
			{
				if (actor != null)
				{
					resetDialogueIfNecessary(actor);
				}
			}
			if (Game1.IsMasterGame)
			{
				foreach (NPC allVillager in Utility.getAllVillagers())
				{
					if (allVillager.getSpouse() != null)
					{
						Farmer spouse = allVillager.getSpouse();
						if (spouse.isMarriedOrRoommates())
						{
							allVillager.controller = null;
							allVillager.temporaryController = null;
							FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(spouse);
							allVillager.Halt();
							Game1.warpCharacter(allVillager, homeOfFarmer, Utility.PointToVector2(homeOfFarmer.getSpouseBedSpot(spouse.spouse)));
							if (homeOfFarmer.GetSpouseBed() != null)
							{
								FarmHouse.spouseSleepEndFunction(allVillager, Utility.getHomeOfFarmer(spouse));
							}
							allVillager.ignoreScheduleToday = true;
							if (Game1.timeOfDayAfterFade >= 1800)
							{
								allVillager.currentMarriageDialogue.Clear();
								allVillager.checkForMarriageDialogue(1800, Utility.getHomeOfFarmer(spouse));
							}
							else if (Game1.timeOfDayAfterFade >= 1100)
							{
								allVillager.currentMarriageDialogue.Clear();
								allVillager.checkForMarriageDialogue(1100, Utility.getHomeOfFarmer(spouse));
							}
							continue;
						}
					}
					if (allVillager.currentLocation != null && allVillager.defaultMap.Value != null)
					{
						allVillager.doingEndOfRouteAnimation.Value = false;
						allVillager.nextEndOfRouteMessage = null;
						allVillager.endOfRouteMessage.Value = null;
						allVillager.controller = null;
						allVillager.temporaryController = null;
						allVillager.Halt();
						Game1.warpCharacter(allVillager, allVillager.defaultMap.Value, allVillager.DefaultPosition / 64f);
						allVillager.ignoreScheduleToday = true;
					}
				}
			}
			foreach (GameLocation location in Game1.locations)
			{
				foreach (Vector2 item in new List<Vector2>(location.objects.Keys))
				{
					if (location.objects[item].minutesElapsed(num))
					{
						location.objects.Remove(item);
					}
				}
				if (location is Farm farm)
				{
					farm.timeUpdate(num);
				}
			}
			Game1.player.freezePause = 1500;
		}
		else
		{
			Game1.player.forceCanMove();
		}
	}

	public void notifyDone()
	{
		if (!(id == "-1") && !string.IsNullOrEmpty(id) && notifyWhenDone && notifyLocationName != null && Game1.HasDedicatedHost && Game1.client != null)
		{
			Game1.client.sendMessage(33, (byte)0, notifyLocationName, notifyLocationIsStructure, id);
			notifyWhenDone = false;
		}
	}

	public void resetDialogueIfNecessary(NPC n)
	{
		if (!Game1.player.hasTalkedToFriendToday(n.Name))
		{
			n.resetCurrentDialogue();
		}
		else
		{
			n.CurrentDialogue?.Clear();
		}
	}

	public void incrementCommandAfterFade()
	{
		CurrentCommand++;
		Game1.globalFade = false;
	}

	public void cleanup()
	{
		Game1.ambientLight = previousAmbientLight;
		_festivalTexture = null;
		festivalContent.Unload();
	}

	private void changeLocation(string locationName, int x, int y, Action onComplete = null)
	{
		Event e = Game1.currentLocation.currentEvent;
		Game1.currentLocation.currentEvent = null;
		LocationRequest locationRequest = Game1.getLocationRequest(locationName);
		locationRequest.OnLoad += delegate
		{
			if (!e.isFestival)
			{
				Game1.currentLocation.currentEvent = e;
			}
			temporaryLocation = null;
			onComplete?.Invoke();
			locationRequest.Location.ResetForEvent(this);
		};
		locationRequest.OnWarp += delegate
		{
			farmer.currentLocation = Game1.currentLocation;
			if (e.isFestival)
			{
				Game1.currentLocation.currentEvent = e;
			}
		};
		Game1.warpFarmer(locationRequest, x, y, farmer.FacingDirection);
	}

	/// <summary>Log an error indicating that an event command format is invalid.</summary>
	/// <param name="args">The space-delimited event command string, including the command name.</param>
	/// <param name="error">The error to log.</param>
	/// <param name="willSkip">Whether the event command will be skipped entirely. If false, the event command will be applied without the argument(s) that failed. This only affects the wording of the message logged.</param>
	public void LogCommandError(string[] args, string error, bool willSkip = false)
	{
		Game1.log.Error(willSkip ? $"Event '{id}' has command '{string.Join(" ", args)}' which couldn't be parsed: {error}." : $"Event '{id}' has command '{string.Join(" ", args)}' which reported errors: {error}.");
	}

	/// <summary>Log an error indicating that a command format is invalid and skip the current command.</summary>
	/// <param name="args">The space-delimited event command string, including the command name.</param>
	/// <param name="error">The error to log.</param>
	/// <param name="hideError">Whether to skip without logging an error message.</param>
	public void LogCommandErrorAndSkip(string[] args, string error, bool hideError = false)
	{
		if (!hideError)
		{
			LogCommandError(args, error, willSkip: true);
		}
		CurrentCommand++;
	}

	/// <summary>Log an error indicating that the entire event has failed, and immediately stop the event.</summary>
	/// <param name="error">An error message indicating why the event failed.</param>
	/// <param name="e">The exception which caused the error, if applicable.</param>
	public void LogErrorAndHalt(string error, Exception e = null)
	{
		string text = "Error running event script " + fromAssetName + "#" + id;
		Game1.chatBox.addErrorMessage("Event script error: " + error);
		string text2 = GetCurrentCommand();
		if (text2 != null)
		{
			text += $" on line #{CurrentCommand} ({text2})";
			Game1.chatBox.addErrorMessage($"On line #{CurrentCommand}: {text2}");
		}
		Game1.log.Error(text + ".", e);
		skipEvent();
	}

	/// <summary>Log an error indicating that the entire event has failed, and immediately stop the event.</summary>
	/// <param name="e">The exception which caused the error.</param>
	public void LogErrorAndHalt(Exception e)
	{
		LogErrorAndHalt(e?.Message ?? "An unknown error occurred.", e);
	}

	/// <summary>Log an error indicating that an event precondition is invalid.</summary>
	/// <param name="location">The location containing the event.</param>
	/// <param name="eventId">The unique event ID whose preconditions are being checked.</param>
	/// <param name="args">The precondition arguments, including the precondition key at the zeroth index.</param>
	/// <param name="error">The error phrase indicating why the precondition is invalid.</param>
	/// <returns>Returns false to simplify failing the precondition.</returns>
	public static bool LogPreconditionError(GameLocation location, string eventId, string[] args, string error)
	{
		Game1.log.Error($"Event '{eventId}' in location '{location.NameOrUniqueName}' has invalid event precondition '{string.Join(" ", args)}': {error}.");
		return false;
	}

	/// <summary>Update the event state.</summary>
	/// <param name="location">The location in which the event is running.</param>
	/// <param name="time">The current game execution time.</param>
	public void Update(GameLocation location, GameTime time)
	{
		try
		{
			if (eventFinished)
			{
				return;
			}
			int num;
			if (CurrentCommand == 0 && !forked)
			{
				num = ((!eventSwitched) ? 1 : 0);
				if (num != 0)
				{
					InitializeEvent(location, time);
				}
			}
			else
			{
				num = 0;
			}
			bool flag = UpdateBeforeNextCommand(location, time);
			if (num == 0 && flag)
			{
				CheckForNextCommand(location, time);
			}
		}
		catch (Exception e)
		{
			LogErrorAndHalt(e);
		}
	}

	/// <summary>Initialize the event when it first starts.</summary>
	/// <param name="location">The location in which the event is running.</param>
	/// <param name="time">The current game execution time.</param>
	protected void InitializeEvent(GameLocation location, GameTime time)
	{
		farmer.speed = 2;
		farmer.running = false;
		Game1.eventOver = false;
		if (!ArgUtility.TryGet(eventCommands, 0, out var value, out var error, allowBlank: true, "string musicId") || !ArgUtility.TryGet(eventCommands, 1, out var value2, out error, allowBlank: false, "string rawCameraPosition") || !ArgUtility.TryGet(eventCommands, 2, out var value3, out error, allowBlank: false, "string rawCharacterPositions") || !ArgUtility.TryGetOptional(eventCommands, 3, out var value4, out error, null, allowBlank: true, "string rawOption"))
		{
			Game1.log.Error($"Event '{id}' has initial fields '{string.Join("/", eventCommands.Take(3))}' which couldn't be parsed: {error}.");
			LogErrorAndHalt("event script is invalid");
			return;
		}
		if (string.IsNullOrWhiteSpace(value))
		{
			value = "none";
		}
		Point value5;
		if (value2 != "follow")
		{
			string[] array = ArgUtility.SplitBySpace(value2);
			if (!ArgUtility.TryGetPoint(array, 0, out value5, out error, "cameraPosition"))
			{
				Game1.log.Error($"Event '{id}' has initial fields '{string.Join("/", eventCommands.Take(3))}' with camera value '{string.Join(" ", array)}' which couldn't be parsed (must be 'follow' or tile coordinates): {error}.");
				LogErrorAndHalt("event script is invalid");
				return;
			}
		}
		else
		{
			value5 = new Point(-1000, -1000);
		}
		if (value4 == "ignoreEventTileOffset")
		{
			ignoreTileOffsets = true;
		}
		if ((value != "none" || !Game1.isRaining) && value != "continue" && !value.Contains("pause"))
		{
			Game1.changeMusicTrack(value, track_interruptable: false, MusicContext.Event);
		}
		if (location is Farm && value5.X >= -1000 && id != "-2" && !ignoreTileOffsets)
		{
			Point frontDoorPositionForFarmer = Farm.getFrontDoorPositionForFarmer(farmer);
			frontDoorPositionForFarmer.X *= 64;
			frontDoorPositionForFarmer.Y *= 64;
			Game1.viewport.X = (Game1.currentLocation.IsOutdoors ? Math.Max(0, Math.Min(frontDoorPositionForFarmer.X - Game1.graphics.GraphicsDevice.Viewport.Width / 2, Game1.currentLocation.Map.DisplayWidth - Game1.graphics.GraphicsDevice.Viewport.Width)) : (frontDoorPositionForFarmer.X - Game1.graphics.GraphicsDevice.Viewport.Width / 2));
			Game1.viewport.Y = (Game1.currentLocation.IsOutdoors ? Math.Max(0, Math.Min(frontDoorPositionForFarmer.Y - Game1.graphics.GraphicsDevice.Viewport.Height / 2, Game1.currentLocation.Map.DisplayHeight - Game1.graphics.GraphicsDevice.Viewport.Height)) : (frontDoorPositionForFarmer.Y - Game1.graphics.GraphicsDevice.Viewport.Height / 2));
		}
		else if (value2 != "follow")
		{
			try
			{
				Game1.viewportFreeze = true;
				int num = OffsetTileX(value5.X) * 64 + 32;
				int num2 = OffsetTileY(value5.Y) * 64 + 32;
				if (num < 0)
				{
					Game1.viewport.X = num;
					Game1.viewport.Y = num2;
				}
				else
				{
					Game1.viewport.X = (Game1.currentLocation.IsOutdoors ? Math.Max(0, Math.Min(num - Game1.viewport.Width / 2, Game1.currentLocation.Map.DisplayWidth - Game1.viewport.Width)) : (num - Game1.viewport.Width / 2));
					Game1.viewport.Y = (Game1.currentLocation.IsOutdoors ? Math.Max(0, Math.Min(num2 - Game1.viewport.Height / 2, Game1.currentLocation.Map.DisplayHeight - Game1.viewport.Height)) : (num2 - Game1.viewport.Height / 2));
				}
				if (num > 0 && Game1.graphics.GraphicsDevice.Viewport.Width > Game1.currentLocation.Map.DisplayWidth)
				{
					Game1.viewport.X = (Game1.currentLocation.Map.DisplayWidth - Game1.viewport.Width) / 2;
				}
				if (num2 > 0 && Game1.graphics.GraphicsDevice.Viewport.Height > Game1.currentLocation.Map.DisplayHeight)
				{
					Game1.viewport.Y = (Game1.currentLocation.Map.DisplayHeight - Game1.viewport.Height) / 2;
				}
			}
			catch (Exception)
			{
				forked = true;
				return;
			}
		}
		setUpCharacters(value3, location);
		trySpecialSetUp(location);
		populateWalkLocationsList();
		CurrentCommand = 3;
	}

	/// <summary>Run any updates needed before checking for the next script command.</summary>
	/// <param name="location">The location in which the event is running.</param>
	/// <param name="time">The current game execution time.</param>
	/// <returns>Returns whether to run the next command.</returns>
	protected bool UpdateBeforeNextCommand(GameLocation location, GameTime time)
	{
		if (skipped || Game1.farmEvent != null)
		{
			return false;
		}
		foreach (NPC actor in actors)
		{
			actor.update(time, Game1.currentLocation);
			if (actor.Sprite.CurrentAnimation != null)
			{
				actor.Sprite.animateOnce(time);
			}
		}
		aboveMapSprites?.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.update(time));
		if (underwaterSprites != null)
		{
			foreach (TemporaryAnimatedSprite underwaterSprite in underwaterSprites)
			{
				underwaterSprite.update(time);
			}
		}
		if (!playerControlSequence)
		{
			farmer.setRunning(isRunning: false);
		}
		if (npcControllers != null)
		{
			for (int num = npcControllers.Count - 1; num >= 0; num--)
			{
				npcControllers[num].puppet.isCharging = !isFestival;
				if (npcControllers[num].update(time, location, npcControllers))
				{
					npcControllers.RemoveAt(num);
				}
			}
		}
		if (isFestival)
		{
			festivalUpdate(time);
		}
		if (temporaryLocation != null && !Game1.currentLocation.Equals(temporaryLocation))
		{
			temporaryLocation.updateEvenIfFarmerIsntHere(time, ignoreWasUpdatedFlush: true);
		}
		if (!Game1.fadeToBlack || actorPositionsAfterMove.Count > 0 || CurrentCommand > 3 || forked)
		{
			if (eventCommands.Length <= CurrentCommand)
			{
				return false;
			}
			if (viewportTarget != Vector3.Zero)
			{
				int speed = farmer.speed;
				farmer.speed = (int)viewportTarget.X;
				int x = Game1.viewport.X;
				Game1.viewport.X += (int)viewportTarget.X;
				if (x > 0 && Game1.viewport.X <= 0 && location.IsOutdoors)
				{
					Game1.viewport.X = 0;
					viewportTarget.X = 0f;
				}
				else if (x < location.map.DisplayWidth - Game1.viewport.Width && Game1.viewport.X >= location.Map.DisplayWidth - Game1.viewport.Width)
				{
					Game1.viewport.X = location.Map.DisplayWidth - Game1.viewport.Width;
					viewportTarget.X = 0f;
				}
				if (viewportTarget.X != 0f)
				{
					Game1.updateRainDropPositionForPlayerMovement((!(viewportTarget.X < 0f)) ? 1 : 3, Math.Abs(viewportTarget.X + (float)((farmer.isMoving() && farmer.FacingDirection == 3) ? (-farmer.speed) : ((farmer.isMoving() && farmer.FacingDirection == 1) ? farmer.speed : 0))));
				}
				int y = Game1.viewport.Y;
				Game1.viewport.Y += (int)viewportTarget.Y;
				if (y > 0 && Game1.viewport.Y <= 0 && location.IsOutdoors)
				{
					Game1.viewport.Y = 0;
					viewportTarget.Y = 0f;
				}
				else if (y < location.map.DisplayHeight - Game1.viewport.Height && Game1.viewport.Y >= location.Map.DisplayHeight - Game1.viewport.Height)
				{
					Game1.viewport.Y = location.Map.DisplayHeight - Game1.viewport.Height;
					viewportTarget.Y = 0f;
				}
				farmer.speed = (int)viewportTarget.Y;
				if (viewportTarget.Y != 0f)
				{
					Game1.updateRainDropPositionForPlayerMovement((!(viewportTarget.Y < 0f)) ? 2 : 0, Math.Abs(viewportTarget.Y - (float)((farmer.isMoving() && farmer.FacingDirection == 0) ? (-farmer.speed) : ((farmer.isMoving() && farmer.FacingDirection == 2) ? farmer.speed : 0))));
				}
				farmer.speed = speed;
				viewportTarget.Z -= time.ElapsedGameTime.Milliseconds;
				if (viewportTarget.Z <= 0f)
				{
					viewportTarget = Vector3.Zero;
				}
			}
			if (actorPositionsAfterMove.Count > 0)
			{
				string[] array = actorPositionsAfterMove.Keys.ToArray();
				foreach (string text in array)
				{
					Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle((int)actorPositionsAfterMove[text].X * 64, (int)actorPositionsAfterMove[text].Y * 64, 64, 64);
					rectangle.Inflate(-4, 0);
					NPC actorByName = getActorByName(text);
					if (actorByName != null)
					{
						Microsoft.Xna.Framework.Rectangle boundingBox = actorByName.GetBoundingBox();
						if (boundingBox.Width > 64)
						{
							rectangle.Inflate(4, 0);
							rectangle.Width = boundingBox.Width + 4;
							rectangle.Height = boundingBox.Height + 4;
							rectangle.X += 8;
							rectangle.Y += 16;
						}
					}
					if (IsFarmerActorId(text, out var farmerNumber))
					{
						Farmer farmerActor = GetFarmerActor(farmerNumber);
						if (farmerActor != null)
						{
							Microsoft.Xna.Framework.Rectangle boundingBox2 = farmerActor.GetBoundingBox();
							float movementSpeed = farmerActor.getMovementSpeed();
							if (rectangle.Contains(boundingBox2) && (((float)(boundingBox2.Y - rectangle.Top) <= 16f + movementSpeed && farmerActor.FacingDirection != 2) || ((float)(rectangle.Bottom - boundingBox2.Bottom) <= 16f + movementSpeed && farmerActor.FacingDirection == 2)))
							{
								farmerActor.showNotCarrying();
								farmerActor.Halt();
								farmerActor.faceDirection((int)actorPositionsAfterMove[text].Z);
								farmerActor.FarmerSprite.StopAnimation();
								farmerActor.Halt();
								actorPositionsAfterMove.Remove(text);
							}
							else if (farmerActor != null)
							{
								farmerActor.canOnlyWalk = false;
								farmerActor.setRunning(isRunning: false, force: true);
								farmerActor.canOnlyWalk = true;
								farmerActor.lastPosition = farmer.Position;
								farmerActor.MovePosition(time, Game1.viewport, location);
							}
						}
						continue;
					}
					foreach (NPC actor2 in actors)
					{
						Microsoft.Xna.Framework.Rectangle boundingBox3 = actor2.GetBoundingBox();
						if (actor2.Name.Equals(text) && rectangle.Contains(boundingBox3) && boundingBox3.Y - rectangle.Top <= 16)
						{
							actor2.Halt();
							actor2.faceDirection((int)actorPositionsAfterMove[text].Z);
							actorPositionsAfterMove.Remove(text);
							break;
						}
						if (actor2.Name.Equals(text))
						{
							if (actor2 is Monster)
							{
								actor2.MovePosition(time, Game1.viewport, location);
							}
							else
							{
								actor2.MovePosition(time, Game1.viewport, null);
							}
							break;
						}
					}
				}
				if (actorPositionsAfterMove.Count == 0)
				{
					if (continueAfterMove)
					{
						continueAfterMove = false;
					}
					else
					{
						CurrentCommand++;
					}
				}
				if (!continueAfterMove)
				{
					return false;
				}
			}
		}
		return true;
	}

	protected void CheckForNextCommand(GameLocation location, GameTime time)
	{
		string[] array = ArgUtility.SplitBySpaceQuoteAware(eventCommands[Math.Min(eventCommands.Length - 1, CurrentCommand)]);
		bool num = ArgUtility.Get(array, 0)?.StartsWith("--") ?? false;
		if (temporaryLocation != null && !Game1.currentLocation.Equals(temporaryLocation))
		{
			temporaryLocation.updateEvenIfFarmerIsntHere(time, ignoreWasUpdatedFlush: true);
		}
		if (num)
		{
			CurrentCommand++;
		}
		else
		{
			tryEventCommand(location, time, array);
		}
	}

	/// <summary>Get the text of the current event command being executed.</summary>
	public string GetCurrentCommand()
	{
		return ArgUtility.Get(eventCommands, currentCommand);
	}

	/// <summary>Replace the command at the current index.</summary>
	/// <param name="command">The new command text to parse.</param>
	public void ReplaceCurrentCommand(string command)
	{
		if (ArgUtility.HasIndex(eventCommands, currentCommand))
		{
			eventCommands[currentCommand] = command;
		}
	}

	/// <summary>Replace the entire list of commands with the given values.</summary>
	/// <param name="commands">The new commands to parse.</param>
	public void ReplaceAllCommands(params string[] commands)
	{
		eventCommands = commands;
		CurrentCommand = 0;
	}

	/// <summary>Add a new event command to run after the current one.</summary>
	/// <param name="command">The new command text to parse.</param>
	public void InsertNextCommand(string command)
	{
		int num = currentCommand + 1;
		List<string> list = eventCommands.ToList();
		if (num <= list.Count)
		{
			list.Insert(num, command);
		}
		else
		{
			list.Add(command);
		}
		eventCommands = list.ToArray();
	}

	/// <summary>Register a sound cue to remove when the event ends.</summary>
	/// <param name="cue">The audio cue to register.</param>
	public void TrackSound(ICue cue)
	{
		if (cue != null)
		{
			if (!CustomSounds.TryGetValue(cue.Name, out var value))
			{
				value = (CustomSounds[cue.Name] = new List<ICue>());
			}
			value.Add(cue);
		}
	}

	/// <summary>Stop a tracked sound registered via <see cref="M:StardewValley.Event.TrackSound(StardewValley.ICue)" />.</summary>
	/// <param name="cueId">The audio cue ID to stop.</param>
	/// <param name="immediate">Whether to stop the sound immediately, instead of letting it finish the current loop.</param>
	public void StopTrackedSound(string cueId, bool immediate)
	{
		if (cueId == null || !CustomSounds.TryGetValue(cueId, out var value))
		{
			return;
		}
		foreach (ICue item in value)
		{
			item.Stop(immediate ? AudioStopOptions.Immediate : AudioStopOptions.AsAuthored);
		}
		if (immediate)
		{
			CustomSounds.Remove(cueId);
		}
	}

	/// <summary>Stop all tracked sounds registered via <see cref="M:StardewValley.Event.TrackSound(StardewValley.ICue)" />.</summary>
	public void StopTrackedSounds()
	{
		foreach (List<ICue> value in CustomSounds.Values)
		{
			foreach (ICue item in value)
			{
				item.Stop(AudioStopOptions.Immediate);
			}
		}
		CustomSounds.Clear();
	}

	public bool isTileWalkedOn(int x, int y)
	{
		return characterWalkLocations.Contains(new Vector2(x, y));
	}

	private void populateWalkLocationsList()
	{
		characterWalkLocations.Add(farmer.Tile);
		foreach (NPC actor in actors)
		{
			characterWalkLocations.Add(actor.Tile);
		}
		for (int i = 2; i < eventCommands.Length; i++)
		{
			string[] array = ArgUtility.SplitBySpace(eventCommands[i]);
			if (ArgUtility.Get(array, 0) != "move" || (ArgUtility.Get(array, 1) == "false" && array.Length == 2))
			{
				continue;
			}
			if (!ArgUtility.TryGet(array, 1, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetPoint(array, 2, out var value2, out error, "Point position"))
			{
				LogCommandError(array, error);
				continue;
			}
			Character character = (IsCurrentFarmerActorId(value) ? ((Character)farmer) : ((Character)getActorByName(value)));
			if (character != null)
			{
				Vector2 tile = character.Tile;
				for (int j = 0; j < Math.Abs(value2.X); j++)
				{
					tile.X += Math.Sign(value2.X);
					characterWalkLocations.Add(tile);
				}
				for (int k = 0; k < Math.Abs(value2.Y); k++)
				{
					tile.Y += Math.Sign(value2.Y);
					characterWalkLocations.Add(tile);
				}
			}
		}
	}

	/// <summary>Get an NPC actor in the event by its name.</summary>
	/// <param name="name">The actor name.</param>
	/// <param name="legacyReplaceUnderscores">Whether to try replacing underscores with spaces in <paramref name="name" /> if an exact match wasn't found. This is only meant for backwards compatibility, for event commands which predate argument quoting.</param>
	/// <returns>Returns the matching actor, else <c>null</c>.</returns>
	public NPC getActorByName(string name, bool legacyReplaceUnderscores = false)
	{
		bool isOptionalNpc;
		return getActorByName(name, out isOptionalNpc, legacyReplaceUnderscores);
	}

	/// <summary>Get an NPC actor in the event by its name.</summary>
	/// <param name="name">The actor name.</param>
	/// <param name="isOptionalNpc">Whether the NPC is marked optional, so no error should be shown if they're missing.</param>
	/// <param name="legacyReplaceUnderscores">Whether to try replacing underscores with spaces in <paramref name="name" /> if an exact match wasn't found. This is only meant for backwards compatibility, for event commands which predate argument quoting.</param>
	/// <returns>Returns the matching actor, else <c>null</c>.</returns>
	public NPC getActorByName(string name, out bool isOptionalNpc, bool legacyReplaceUnderscores = false)
	{
		isOptionalNpc = name?.EndsWith('?') ?? false;
		if (isOptionalNpc)
		{
			name = name.Substring(0, name.Length - 1);
		}
		if (name != null)
		{
			if (name == "spouse")
			{
				name = farmer.spouse;
			}
			foreach (NPC actor in actors)
			{
				if (actor.Name == name)
				{
					return actor;
				}
			}
			if (legacyReplaceUnderscores)
			{
				string text = name.Replace('_', ' ');
				if (text != name)
				{
					foreach (NPC actor2 in actors)
					{
						if (actor2.Name == text)
						{
							return actor2;
						}
					}
				}
			}
			return null;
		}
		return null;
	}

	private void addActor(string name, int x, int y, int facingDirection, GameLocation location)
	{
		bool isOptionalNpc;
		NPC actorByName = getActorByName(name, out isOptionalNpc);
		if (actorByName != null)
		{
			actorByName.Position = new Vector2(x * 64, y * 64);
			actorByName.FacingDirection = facingDirection;
			return;
		}
		if (isOptionalNpc)
		{
			name = name.Substring(0, name.Length - 1);
			if (!NPC.TryGetData(name, out var data) || !GameStateQuery.CheckConditions(data.UnlockConditions))
			{
				return;
			}
		}
		NPC nPC;
		try
		{
			string textureNameForCharacter = NPC.getTextureNameForCharacter(name);
			Texture2D portrait = null;
			try
			{
				portrait = Game1.content.Load<Texture2D>("Portraits\\" + textureNameForCharacter);
			}
			catch (Exception)
			{
			}
			int num = ((name.Contains("Dwarf") || name.Equals("Krobus")) ? 96 : 128);
			nPC = new NPC(new AnimatedSprite("Characters\\" + textureNameForCharacter, 0, 16, num / 4), new Vector2(x * 64, y * 64), location.Name, facingDirection, name, portrait, eventActor: true);
			nPC.EventActor = true;
			if (isFestival)
			{
				try
				{
					if (TryGetFestivalDialogueForYear(nPC, nPC.Name, out var dialogue))
					{
						nPC.setNewDialogue(dialogue);
					}
				}
				catch (Exception)
				{
				}
			}
			if (nPC.name.Equals("MrQi"))
			{
				nPC.displayName = Game1.content.LoadString("Strings\\NPCNames:MisterQi");
			}
		}
		catch (Exception exception)
		{
			Game1.log.Error($"Event '{id}' has character '{name}' which couldn't be added.", exception);
			return;
		}
		nPC.EventActor = true;
		actors.Add(nPC);
	}

	/// <summary>Get the player in the event matching a farmer number, if found.</summary>
	/// <param name="farmerNumber">The farmer number. This can be -1 (current player), 1 (main player), or higher numbers for farmhands.</param>
	/// <returns>Returns the matching event actor or real farmer, or <c>null</c> if neither was found.</returns>
	public Farmer GetFarmerActor(int farmerNumber)
	{
		Farmer farmer = ((farmerNumber < 1) ? this.farmer : Utility.getFarmerFromFarmerNumber(farmerNumber));
		if (farmer == null)
		{
			return null;
		}
		foreach (Farmer farmerActor in farmerActors)
		{
			if (farmerActor.UniqueMultiplayerID == farmer.UniqueMultiplayerID)
			{
				return farmerActor;
			}
		}
		return farmer;
	}

	/// <summary>Get whether an actor ID is the current player.</summary>
	/// <param name="actor">The actor ID to check.</param>
	public bool IsCurrentFarmerActorId(string actor)
	{
		if (IsFarmerActorId(actor, out var farmerNumber))
		{
			return IsCurrentFarmerActorId(farmerNumber);
		}
		return false;
	}

	/// <summary>Get whether an actor ID is the current player.</summary>
	/// <param name="farmerNumber">The farmer number to check.</param>
	public bool IsCurrentFarmerActorId(int farmerNumber)
	{
		if (farmerNumber >= 1)
		{
			return farmerNumber == Utility.getFarmerNumberFromFarmer(Game1.player);
		}
		return true;
	}

	/// <summary>Get whether an actor ID is a farmer like <c>farmer</c> (current player) or <samp>farmer3</samp> (player #3), regardless of whether that player is present.</summary>
	/// <param name="actor">The actor ID to check.</param>
	/// <param name="farmerNumber">The parsed farmer number, if applicable. This can be <samp>-1</samp> (current player), 1 (main player), or higher numbers for farmhands.</param>
	public bool IsFarmerActorId(string actor, out int farmerNumber)
	{
		if (actor == null || !actor.StartsWith("farmer"))
		{
			farmerNumber = -1;
			return false;
		}
		if (actor.Length == "farmer".Length)
		{
			farmerNumber = -1;
			return true;
		}
		return int.TryParse(actor.Substring("farmer".Length), out farmerNumber);
	}

	public Character getCharacterByName(string name)
	{
		if (IsFarmerActorId(name, out var farmerNumber))
		{
			return GetFarmerActor(farmerNumber);
		}
		foreach (NPC actor in actors)
		{
			if (actor.Name.Equals(name))
			{
				return actor;
			}
		}
		return null;
	}

	public Vector3 getPositionAfterMove(Character c, int xMove, int yMove, int facingDirection)
	{
		Vector2 tile = c.Tile;
		return new Vector3(tile.X + (float)xMove, tile.Y + (float)yMove, facingDirection);
	}

	private void trySpecialSetUp(GameLocation location)
	{
		string text = id;
		if (text == null)
		{
			return;
		}
		switch (text.Length)
		{
		case 7:
			switch (text[6])
			{
			case '0':
				if (text == "9333220" && location is FarmHouse { upgradeLevel: 1 })
				{
					farmer.Position = new Vector2(1920f, 400f);
					getActorByName("Sebastian").setTilePosition(31, 6);
				}
				break;
			case '3':
			{
				if (!(text == "4324303") || !(location is FarmHouse farmHouse3))
				{
					break;
				}
				Point playerBedSpot = farmHouse3.GetPlayerBedSpot();
				playerBedSpot.X--;
				farmer.Position = new Vector2(playerBedSpot.X * 64, playerBedSpot.Y * 64 + 16);
				getActorByName("Penny").setTilePosition(playerBedSpot.X - 1, playerBedSpot.Y);
				Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle(23, 12, 10, 10);
				if (farmHouse3.upgradeLevel == 1)
				{
					rectangle = new Microsoft.Xna.Framework.Rectangle(20, 3, 8, 7);
				}
				Point center = rectangle.Center;
				if (!rectangle.Contains(Game1.player.TilePoint))
				{
					List<string> list = new List<string>(eventCommands);
					int num = 56;
					list.Insert(num, "globalFade 0.03");
					num++;
					list.Insert(num, "beginSimultaneousCommand");
					num++;
					list.Insert(num, "viewport " + center.X + " " + center.Y);
					num++;
					list.Insert(num, "globalFadeToClear 0.03");
					num++;
					list.Insert(num, "endSimultaneousCommand");
					num++;
					list.Insert(num, "pause 2000");
					num++;
					list.Insert(num, "globalFade 0.03");
					num++;
					list.Insert(num, "beginSimultaneousCommand");
					num++;
					list.Insert(num, "viewport " + Game1.player.TilePoint.X + " " + Game1.player.TilePoint.Y);
					num++;
					list.Insert(num, "globalFadeToClear 0.03");
					num++;
					list.Insert(num, "endSimultaneousCommand");
					num++;
					eventCommands = list.ToArray();
				}
				for (int num2 = 0; num2 < eventCommands.Length; num2++)
				{
					if (!eventCommands[num2].StartsWith("makeInvisible"))
					{
						continue;
					}
					string[] array = ArgUtility.SplitBySpace(eventCommands[num2]);
					if (!ArgUtility.TryGetPoint(array, 1, out var value, out var error, "Point tile"))
					{
						LogCommandError(array, error);
						continue;
					}
					array[1] = (value.X - 26 + playerBedSpot.X).ToString() ?? "";
					array[2] = (value.Y - 13 + playerBedSpot.Y).ToString() ?? "";
					if (location.getObjectAtTile(value.X, value.Y) == farmHouse3.GetPlayerBed())
					{
						eventCommands[num2] = "makeInvisible -1000 -1000";
					}
					else
					{
						eventCommands[num2] = string.Join(" ", array);
					}
				}
				break;
			}
			case '4':
				if (text == "4325434" && location is FarmHouse { upgradeLevel: 1 })
				{
					farmer.Position = new Vector2(512f, 336f);
					getActorByName("Penny").setTilePosition(5, 5);
				}
				break;
			case '2':
			{
				if (!(text == "3912132") || !(location is FarmHouse farmHouse7))
				{
					break;
				}
				Point playerBedSpot2 = farmHouse7.GetPlayerBedSpot();
				playerBedSpot2.X--;
				if (!location.CanItemBePlacedHere(Utility.PointToVector2(playerBedSpot2) + new Vector2(-2f, 0f)))
				{
					playerBedSpot2.X++;
				}
				farmer.setTileLocation(Utility.PointToVector2(playerBedSpot2));
				getActorByName("Elliott").setTileLocation(Utility.PointToVector2(playerBedSpot2) + new Vector2(-2f, 0f));
				for (int num3 = 0; num3 < eventCommands.Length; num3++)
				{
					if (!eventCommands[num3].StartsWith("makeInvisible"))
					{
						continue;
					}
					string[] array2 = ArgUtility.SplitBySpace(eventCommands[num3]);
					if (!ArgUtility.TryGetPoint(array2, 1, out var value2, out var error2, "Point tile"))
					{
						LogCommandError(array2, error2);
						continue;
					}
					array2[1] = (value2.X - 26 + playerBedSpot2.X).ToString() ?? "";
					array2[2] = (value2.Y - 13 + playerBedSpot2.Y).ToString() ?? "";
					if (location.getObjectAtTile(value2.X, value2.Y) == farmHouse7.GetPlayerBed())
					{
						eventCommands[num3] = "makeInvisible -1000 -1000";
					}
					else
					{
						eventCommands[num3] = string.Join(" ", array2);
					}
				}
				break;
			}
			case '1':
				if (!(text == "8675611"))
				{
					if (!(text == "3917601") || !(location is DecoratableLocation decoratableLocation))
					{
						break;
					}
					foreach (Furniture item in decoratableLocation.furniture)
					{
						if (item.furniture_type.Value == 14 && !location.IsTileBlockedBy(item.TileLocation + new Vector2(0f, 1f), CollisionMask.All, CollisionMask.All) && !location.IsTileBlockedBy(item.TileLocation + new Vector2(1f, 1f), CollisionMask.All, CollisionMask.All))
						{
							getActorByName("Emily").setTilePosition((int)item.TileLocation.X, (int)item.TileLocation.Y + 1);
							farmer.Position = new Vector2((item.TileLocation.X + 1f) * 64f, (item.tileLocation.Y + 1f) * 64f + 16f);
							item.isOn.Value = true;
							item.setFireplace(playSound: false);
							return;
						}
					}
					if (location is FarmHouse { upgradeLevel: 1 })
					{
						getActorByName("Emily").setTilePosition(4, 5);
						farmer.Position = new Vector2(320f, 336f);
					}
				}
				else if (location is FarmHouse { upgradeLevel: 1 })
				{
					getActorByName("Haley").setTilePosition(4, 5);
					farmer.Position = new Vector2(320f, 336f);
				}
				break;
			case '6':
				if (text == "3917666" && location is FarmHouse { upgradeLevel: 1 })
				{
					getActorByName("Maru").setTilePosition(4, 5);
					farmer.Position = new Vector2(320f, 336f);
				}
				break;
			case '5':
				break;
			}
			break;
		case 6:
			if (text == "739330")
			{
				if (!Game1.player.friendshipData.ContainsKey("Willy"))
				{
					Game1.player.friendshipData.Add("Willy", new Friendship(0));
				}
				NPC willy = Game1.getCharacterFromName("Willy");
				Game1.player.NotifyQuests((Quest quest) => quest.OnNpcSocialized(willy));
			}
			break;
		}
	}

	private void setUpCharacters(string description, GameLocation location)
	{
		this.farmer.Halt();
		if (string.IsNullOrEmpty(Game1.player.locationBeforeForcedEvent.Value) && !isMemory)
		{
			Game1.player.positionBeforeEvent = Game1.player.Tile;
			Game1.player.orientationBeforeEvent = Game1.player.FacingDirection;
		}
		string[] array = ArgUtility.SplitBySpace(description);
		for (int i = 0; i < array.Length; i += 4)
		{
			if (!ArgUtility.TryGet(array, i, out var value, out var error, allowBlank: true, "string actorName") || !ArgUtility.TryGetPoint(array, i + 1, out var value2, out error, "Point tile") || !ArgUtility.TryGetInt(array, i + 3, out var value3, out error, "int direction"))
			{
				Game1.log.Error($"Event '{id}' has character positions '{string.Join(" ", array)}' which couldn't be parsed: {error}.");
				continue;
			}
			int farmerNumber;
			bool flag = IsFarmerActorId(value, out farmerNumber);
			bool flag2 = flag && IsCurrentFarmerActorId(farmerNumber);
			if (value2.X == -1 && !flag2)
			{
				foreach (NPC character in location.characters)
				{
					if (character.Name == value)
					{
						actors.Add(character);
					}
				}
			}
			else if (value != "farmer")
			{
				if (value == "otherFarmers")
				{
					int num = OffsetTileX(value2.X);
					int num2 = OffsetTileY(value2.Y);
					foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
					{
						if (onlineFarmer.UniqueMultiplayerID != this.farmer.UniqueMultiplayerID)
						{
							Farmer farmer = onlineFarmer.CreateFakeEventFarmer();
							farmer.completelyStopAnimatingOrDoingAction();
							farmer.hidden.Value = false;
							farmer.faceDirection(value3);
							farmer.setTileLocation(new Vector2(num, num2));
							farmer.currentLocation = Game1.currentLocation;
							num++;
							farmerActors.Add(farmer);
						}
					}
					continue;
				}
				if (flag)
				{
					int num3 = OffsetTileX(value2.X);
					int num4 = OffsetTileY(value2.Y);
					Farmer farmerActor = GetFarmerActor(farmerNumber);
					if (farmerActor != null)
					{
						Farmer farmer2 = farmerActor.CreateFakeEventFarmer();
						farmer2.completelyStopAnimatingOrDoingAction();
						farmer2.hidden.Value = false;
						farmer2.faceDirection(value3);
						farmer2.setTileLocation(new Vector2(num3, num4));
						farmer2.currentLocation = Game1.currentLocation;
						farmer2.isFakeEventActor = true;
						farmerActors.Add(farmer2);
					}
					continue;
				}
				string name = ((!(value == "spouse")) ? value : this.farmer.spouse);
				switch (value)
				{
				case "cat":
				{
					Pet pet3 = new Pet(OffsetTileX(value2.X), OffsetTileY(value2.Y), Game1.player.whichPetBreed, "Cat");
					pet3.Name = "Cat";
					pet3.position.X -= 32f;
					actors.Add(pet3);
					continue;
				}
				case "dog":
				{
					Pet pet = new Pet(OffsetTileX(value2.X), OffsetTileY(value2.Y), Game1.player.whichPetBreed, "Dog");
					pet.Name = "Dog";
					pet.position.X -= 42f;
					actors.Add(pet);
					continue;
				}
				case "pet":
				{
					Pet pet2 = new Pet(OffsetTileX(value2.X), OffsetTileY(value2.Y), Game1.player.whichPetBreed, Game1.player.whichPetType);
					pet2.Name = "PetActor";
					if (Pet.TryGetData(Game1.player.whichPetType, out var data))
					{
						pet2.Position = new Vector2(pet2.Position.X + (float)data.EventOffset.X, pet2.Position.Y + (float)data.EventOffset.Y);
					}
					actors.Add(pet2);
					continue;
				}
				case "golem":
				{
					NPC nPC = new NPC(new AnimatedSprite("Characters\\Monsters\\Wilderness Golem", 0, 16, 24), OffsetPosition(new Vector2(value2.X, value2.Y) * 64f), 0, "Golem");
					nPC.AllowDynamicAppearance = false;
					actors.Add(nPC);
					continue;
				}
				case "Junimo":
					actors.Add(new Junimo(OffsetPosition(new Vector2(value2.X * 64, value2.Y * 64 - 32)), Game1.currentLocation.Name.Equals("AbandonedJojaMart") ? 6 : (-1))
					{
						Name = "Junimo",
						EventActor = true
					});
					continue;
				}
				int x = OffsetTileX(value2.X);
				int y = OffsetTileY(value2.Y);
				int facingDirection = value3;
				if (location is Farm && id != "-2" && !ignoreTileOffsets)
				{
					x = Farm.getFrontDoorPositionForFarmer(this.farmer).X;
					y = Farm.getFrontDoorPositionForFarmer(this.farmer).Y + 2;
					facingDirection = 0;
				}
				addActor(name, x, y, facingDirection, location);
			}
			else if (value2.X != -1)
			{
				this.farmer.position.X = OffsetPositionX(value2.X * 64);
				this.farmer.position.Y = OffsetPositionY(value2.Y * 64 + 16);
				this.farmer.faceDirection(value3);
				if (location is Farm && id != "-2" && !ignoreTileOffsets)
				{
					this.farmer.position.X = Farm.getFrontDoorPositionForFarmer(this.farmer).X * 64;
					this.farmer.position.Y = (Farm.getFrontDoorPositionForFarmer(this.farmer).Y + 1) * 64;
					this.farmer.faceDirection(2);
				}
				this.farmer.FarmerSprite.StopAnimation();
			}
		}
	}

	private void beakerSmashEndFunction(int extraInfo)
	{
		Game1.playSound("breakingGlass");
		Game1.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(47, new Vector2(9f, 16f) * 64f, Color.LightBlue, 10));
		Game1.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(400, 3008, 64, 64), 99999f, 2, 0, new Vector2(9f, 16f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.LightBlue, 1f, 0f, 0f, 0f)
		{
			delayBeforeAnimationStart = 700
		});
		Game1.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(46, new Vector2(9f, 16f) * 64f, Color.White * 0.75f, 10)
		{
			motion = new Vector2(0f, -1f)
		});
	}

	private void eggSmashEndFunction(int extraInfo)
	{
		Game1.playSound("slimedead");
		Game1.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(47, new Vector2(9f, 16f) * 64f, Color.White, 10));
		Game1.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(177, 99999f, 9999, 0, new Vector2(6f, 5f) * 64f, flicker: false, flipped: false)
		{
			layerDepth = 1E-06f
		});
	}

	private void balloonInSky(int extraInfo)
	{
		TemporaryAnimatedSprite temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(2);
		if (temporarySpriteByID != null)
		{
			temporarySpriteByID.motion = Vector2.Zero;
		}
		temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(1);
		if (temporarySpriteByID != null)
		{
			temporarySpriteByID.motion = Vector2.Zero;
		}
	}

	private void marcelloBalloonLand(int extraInfo)
	{
		Game1.playSound("thudStep");
		Game1.playSound("dirtyHit");
		TemporaryAnimatedSprite temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(2);
		if (temporarySpriteByID != null)
		{
			temporarySpriteByID.motion = Vector2.Zero;
		}
		temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(3);
		if (temporarySpriteByID != null)
		{
			temporarySpriteByID.scaleChange = 0f;
		}
		Game1.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 2944, 64, 64), 120f, 8, 1, (new Vector2(25f, 39f) + eventPositionTileOffset) * 64f + new Vector2(-32f, 32f), flicker: false, flipped: true, 1f, 0f, Color.White, 1f, 0f, 0f, 0f));
		Game1.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 2944, 64, 64), 120f, 8, 1, (new Vector2(27f, 39f) + eventPositionTileOffset) * 64f + new Vector2(0f, 48f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
		{
			delayBeforeAnimationStart = 300
		});
		CurrentCommand++;
	}

	private void samPreOllie(int extraInfo)
	{
		getActorByName("Sam").Sprite.currentFrame = 27;
		farmer.faceDirection(0);
		TemporaryAnimatedSprite temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(92473);
		temporarySpriteByID.xStopCoordinate = 1408;
		temporarySpriteByID.reachedStopCoordinate = samOllie;
		temporarySpriteByID.motion = new Vector2(2f, 0f);
	}

	private void samOllie(int extraInfo)
	{
		Game1.playSound("crafting");
		getActorByName("Sam").Sprite.currentFrame = 26;
		TemporaryAnimatedSprite temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(92473);
		temporarySpriteByID.currentNumberOfLoops = 0;
		temporarySpriteByID.totalNumberOfLoops = 1;
		temporarySpriteByID.motion.Y = -9f;
		temporarySpriteByID.motion.X = 2f;
		temporarySpriteByID.acceleration = new Vector2(0f, 0.4f);
		temporarySpriteByID.animationLength = 1;
		temporarySpriteByID.interval = 530f;
		temporarySpriteByID.timer = 0f;
		temporarySpriteByID.endFunction = samGrind;
		temporarySpriteByID.destroyable = false;
	}

	private void samGrind(int extraInfo)
	{
		Game1.playSound("hammer");
		getActorByName("Sam").Sprite.currentFrame = 28;
		TemporaryAnimatedSprite temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(92473);
		temporarySpriteByID.currentNumberOfLoops = 0;
		temporarySpriteByID.totalNumberOfLoops = 9999;
		temporarySpriteByID.motion.Y = 0f;
		temporarySpriteByID.motion.X = 2f;
		temporarySpriteByID.acceleration = new Vector2(0f, 0f);
		temporarySpriteByID.animationLength = 1;
		temporarySpriteByID.interval = 99999f;
		temporarySpriteByID.timer = 0f;
		temporarySpriteByID.xStopCoordinate = 1664;
		temporarySpriteByID.yStopCoordinate = -1;
		temporarySpriteByID.reachedStopCoordinate = samDropOff;
	}

	private void samDropOff(int extraInfo)
	{
		NPC actorByName = getActorByName("Sam");
		actorByName.Sprite.currentFrame = 31;
		TemporaryAnimatedSprite temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(92473);
		temporarySpriteByID.currentNumberOfLoops = 9999;
		temporarySpriteByID.totalNumberOfLoops = 0;
		temporarySpriteByID.motion.Y = 0f;
		temporarySpriteByID.motion.X = 2f;
		temporarySpriteByID.acceleration = new Vector2(0f, 0.4f);
		temporarySpriteByID.animationLength = 1;
		temporarySpriteByID.interval = 99999f;
		temporarySpriteByID.yStopCoordinate = 5760;
		temporarySpriteByID.reachedStopCoordinate = samGround;
		temporarySpriteByID.endFunction = null;
		actorByName.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
		{
			new FarmerSprite.AnimationFrame(29, 100),
			new FarmerSprite.AnimationFrame(30, 100),
			new FarmerSprite.AnimationFrame(31, 100),
			new FarmerSprite.AnimationFrame(32, 100)
		});
		actorByName.Sprite.loop = false;
	}

	private void samGround(int extraInfo)
	{
		TemporaryAnimatedSprite temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(92473);
		Game1.playSound("thudStep");
		temporarySpriteByID.attachedCharacter = null;
		temporarySpriteByID.reachedStopCoordinate = null;
		temporarySpriteByID.totalNumberOfLoops = -1;
		temporarySpriteByID.interval = 0f;
		temporarySpriteByID.destroyable = true;
		CurrentCommand++;
	}

	private void catchFootball(int extraInfo)
	{
		TemporaryAnimatedSprite temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(56232);
		Game1.playSound("fishSlap");
		temporarySpriteByID.motion = new Vector2(2f, -8f);
		temporarySpriteByID.rotationChange = (float)Math.PI / 24f;
		temporarySpriteByID.reachedStopCoordinate = footballLand;
		temporarySpriteByID.yStopCoordinate = 1088;
		farmer.jump();
	}

	private void footballLand(int extraInfo)
	{
		TemporaryAnimatedSprite temporarySpriteByID = Game1.currentLocation.getTemporarySpriteByID(56232);
		Game1.playSound("sandyStep");
		temporarySpriteByID.motion = new Vector2(0f, 0f);
		temporarySpriteByID.rotationChange = 0f;
		temporarySpriteByID.reachedStopCoordinate = null;
		temporarySpriteByID.animationLength = 1;
		temporarySpriteByID.interval = 999999f;
		CurrentCommand++;
	}

	private void parrotSplat(int extraInfo)
	{
		Game1.playSound("drumkit0");
		DelayedAction.playSoundAfterDelay("drumkit5", 100);
		Game1.playSound("slimeHit");
		foreach (TemporaryAnimatedSprite aboveMapSprite in aboveMapSprites)
		{
			aboveMapSprite.alpha = 0f;
		}
		Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(174, 168, 4, 11), 99999f, 1, 99999, new Vector2(1504f, 5568f), flicker: false, flipped: false, 0.02f, 0.01f, Color.White, 4f, 0f, (float)Math.PI / 2f, (float)Math.PI / 64f)
		{
			motion = new Vector2(2f, -2f),
			acceleration = new Vector2(0f, 0.1f)
		});
		Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(174, 168, 4, 11), 99999f, 1, 99999, new Vector2(1504f, 5568f), flicker: false, flipped: false, 0.02f, 0.01f, Color.White, 4f, 0f, (float)Math.PI / 4f, (float)Math.PI / 64f)
		{
			motion = new Vector2(-2f, -1f),
			acceleration = new Vector2(0f, 0.1f)
		});
		Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(174, 168, 4, 11), 99999f, 1, 99999, new Vector2(1504f, 5568f), flicker: false, flipped: false, 0.02f, 0.01f, Color.White, 4f, 0f, (float)Math.PI, (float)Math.PI / 64f)
		{
			motion = new Vector2(1f, 1f),
			acceleration = new Vector2(0f, 0.1f)
		});
		Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(174, 168, 4, 11), 99999f, 1, 99999, new Vector2(1504f, 5568f), flicker: false, flipped: false, 0.02f, 0.01f, Color.White, 4f, 0f, 0f, (float)Math.PI / 64f)
		{
			motion = new Vector2(-2f, -2f),
			acceleration = new Vector2(0f, 0.1f)
		});
		Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(148, 165, 25, 23), 99999f, 1, 99999, new Vector2(1504f, 5568f), flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
		{
			id = 666
		});
		CurrentCommand++;
	}

	public virtual Vector2 OffsetPosition(Vector2 original)
	{
		return new Vector2(OffsetPositionX(original.X), OffsetPositionY(original.Y));
	}

	public virtual Vector2 OffsetTile(Vector2 original)
	{
		return new Vector2(OffsetTileX((int)original.X), OffsetTileY((int)original.Y));
	}

	public virtual float OffsetPositionX(float original)
	{
		if (original < 0f || ignoreTileOffsets)
		{
			return original;
		}
		return original + eventPositionTileOffset.X * 64f;
	}

	public virtual float OffsetPositionY(float original)
	{
		if (original < 0f || ignoreTileOffsets)
		{
			return original;
		}
		return original + eventPositionTileOffset.Y * 64f;
	}

	public virtual int OffsetTileX(int original)
	{
		if (original < 0 || ignoreTileOffsets)
		{
			return original;
		}
		return (int)((float)original + eventPositionTileOffset.X);
	}

	public virtual int OffsetTileY(int original)
	{
		if (original < 0 || ignoreTileOffsets)
		{
			return original;
		}
		return (int)((float)original + eventPositionTileOffset.Y);
	}

	private void addSpecificTemporarySprite(string key, GameLocation location, string[] args)
	{
		if (key == null)
		{
			return;
		}
		switch (key.Length)
		{
		case 13:
			switch (key[9])
			{
			case 'n':
				if (!(key == "raccoondance2"))
				{
					if (key == "raccoondance1")
					{
						TemporaryAnimatedSprite temporarySpriteByID6 = location.getTemporarySpriteByID(9786);
						TemporaryAnimatedSprite temporarySpriteByID7 = location.getTemporarySpriteByID(9785);
						temporarySpriteByID6.sourceRect.Y = 96;
						temporarySpriteByID6.sourceRectStartingPos.Y = 96f;
						temporarySpriteByID6.currentParentTileIndex = 1;
						temporarySpriteByID6.motion.X = 0.07f;
						temporarySpriteByID6.timer = 0f;
						temporarySpriteByID7.sourceRect.Y = 32;
						temporarySpriteByID7.sourceRectStartingPos.Y = 32f;
						temporarySpriteByID7.currentParentTileIndex = 1;
						temporarySpriteByID7.motion.X = -0.07f;
						temporarySpriteByID7.timer = 0f;
					}
				}
				else
				{
					location.removeTemporarySpritesWithIDLocal(9786);
					TemporaryAnimatedSprite temporarySpriteByID8 = location.getTemporarySpriteByID(9785);
					temporarySpriteByID8.sourceRect.Y = 64;
					temporarySpriteByID8.sourceRectStartingPos.Y = 64f;
					temporarySpriteByID8.currentParentTileIndex = 0;
					temporarySpriteByID8.motion.X = 0f;
					temporarySpriteByID8.interval *= 2f;
					temporarySpriteByID8.timer = 0f;
					temporarySpriteByID8.sourceRect.X = 0;
					temporarySpriteByID8.position.X -= 32f;
					temporarySpriteByID8.position.Y += 8f;
				}
				break;
			case 'r':
				if (key == "raccoonCircle")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Characters\\raccoon", new Microsoft.Xna.Framework.Rectangle(0, 0, 32, 32), 148f, 8, 999, new Vector2(54.5f, 7f) * 64f, flicker: false, flipped: false)
					{
						scale = 4f,
						layerDepth = 0.051840004f,
						usePreciseTiming = true,
						id = 9786
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Characters\\mrs_raccoon", new Microsoft.Xna.Framework.Rectangle(0, 0, 32, 32), 148f, 8, 999, new Vector2(56.5f, 7f) * 64f, flicker: false, flipped: false)
					{
						scale = 4f,
						layerDepth = 0.0512f,
						usePreciseTiming = true,
						id = 9785
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\raccoon_circle_cutout", new Microsoft.Xna.Framework.Rectangle(0, 0, 1, 1), Vector2.Zero, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						vectorScale = new Vector2(3090f, 1052f),
						interval = 99999f,
						totalNumberOfLoops = 1,
						id = 997799
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\raccoon_circle_cutout", new Microsoft.Xna.Framework.Rectangle(0, 0, 1, 1), new Vector2(56.5f, 0f) * 64f + new Vector2(131.5f, 0f) * 4f, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						vectorScale = new Vector2(5536f, 1052f),
						interval = 99999f,
						totalNumberOfLoops = 1,
						id = 997799
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\raccoon_circle_cutout", new Microsoft.Xna.Framework.Rectangle(0, 0, 1, 1), new Vector2(0f, 876f), flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						vectorScale = new Vector2(7552f, 7488f),
						interval = 99999f,
						totalNumberOfLoops = 1,
						id = 997799
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\raccoon_circle_cutout", new Microsoft.Xna.Framework.Rectangle(0, 0, 263, 263), new Vector2(56.5f, 0f) * 64f - new Vector2(131.5f, 44f) * 4f, flipped: false, 0f, Color.Black)
					{
						drawAboveAlwaysFront = true,
						interval = 297f,
						animationLength = 3,
						totalNumberOfLoops = 99999,
						id = 997799,
						scale = 4f
					});
				}
				break;
			case 'T':
				if (!(key == "trashBearTown"))
				{
					if (key == "stopShakeTent")
					{
						location.getTemporarySpriteByID(999).shakeIntensity = 0f;
					}
					break;
				}
				aboveMapSprites = new TemporaryAnimatedSpriteList();
				aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(46, 80, 46, 56), new Vector2(43f, 64f) * 64f, flipped: false, 0f, Color.White)
				{
					animationLength = 1,
					interval = 999999f,
					motion = new Vector2(4f, 0f),
					scale = 4f,
					layerDepth = 1f,
					yPeriodic = true,
					yPeriodicLoopTime = 2000f,
					yPeriodicRange = 32f,
					id = 777,
					xStopCoordinate = 3392,
					reachedStopCoordinate = delegate
					{
						aboveMapSprites[0].xStopCoordinate = -1;
						aboveMapSprites[0].motion = new Vector2(4f, 0f);
						location.ApplyMapOverride("Town-TrashGone", (Microsoft.Xna.Framework.Rectangle?)null, (Microsoft.Xna.Framework.Rectangle?)new Microsoft.Xna.Framework.Rectangle(57, 68, 17, 5));
						location.ApplyMapOverride("Town-DogHouse", (Microsoft.Xna.Framework.Rectangle?)null, (Microsoft.Xna.Framework.Rectangle?)new Microsoft.Xna.Framework.Rectangle(51, 65, 5, 6));
						Game1.flashAlpha = 0.75f;
						Game1.screenGlowOnce(Color.Lime, hold: false, 0.25f, 1f);
						location.playSound("yoba");
						TemporaryAnimatedSprite temporaryAnimatedSprite7 = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(497, 1918, 11, 11), new Vector2(3456f, 4160f), flipped: false, 0f, Color.White)
						{
							yStopCoordinate = 4372,
							motion = new Vector2(-0.5f, -10f),
							acceleration = new Vector2(0f, 0.25f),
							scale = 4f,
							alphaFade = 0f,
							extraInfoForEndBehavior = -777
						};
						temporaryAnimatedSprite7.reachedStopCoordinate = temporaryAnimatedSprite7.bounce;
						temporaryAnimatedSprite7.initialPosition.Y = 4372f;
						aboveMapSprites.Add(temporaryAnimatedSprite7);
						aboveMapSprites.AddRange(Utility.getStarsAndSpirals(location, 54, 69, 6, 5, 1000, 10, Color.Lime));
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(324, 1936, 12, 20), 80f, 4, 99999, new Vector2(53f, 67f) * 64f + new Vector2(3f, 3f) * 4f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							id = 1,
							delayBeforeAnimationStart = 3000,
							startSound = "dogWhining"
						});
					}
				});
				break;
			case 'S':
				if (key == "shakeBushStop")
				{
					location.getTemporarySpriteByID(777).shakeIntensity = 0f;
				}
				break;
			case 'F':
				if (key == "sebastianFrog")
				{
					Texture2D texture11 = Game1.temporaryContent.Load<Texture2D>("TileSheets\\critters");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture11,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 224, 16, 16),
						animationLength = 4,
						sourceRectStartingPos = new Vector2(0f, 224f),
						interval = 120f,
						totalNumberOfLoops = 9999,
						position = new Vector2(45f, 36f) * 64f,
						scale = 4f,
						layerDepth = 0.00064f,
						motion = new Vector2(2f, 0f),
						xStopCoordinate = 3136,
						id = 777,
						reachedStopCoordinate = delegate
						{
							int num49 = CurrentCommand;
							CurrentCommand = num49 + 1;
							location.removeTemporarySpritesWithID(777);
						}
					});
				}
				break;
			case 'W':
				if (key == "haleyCakeWalk")
				{
					Texture2D texture10 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture10,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 400, 144, 112),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(0f, 400f),
						interval = 5000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(26f, 65f) * 64f,
						scale = 4f,
						layerDepth = 0.00064f
					});
				}
				break;
			case 'a':
				if (key == "pamYobaStatue")
				{
					location.objects.Remove(new Vector2(26f, 9f));
					location.objects.Add(new Vector2(26f, 9f), ItemRegistry.Create<Object>("(BC)34"));
					GameLocation gameLocation = Game1.RequireLocation("Trailer_Big");
					gameLocation.objects.Remove(new Vector2(26f, 9f));
					gameLocation.objects.Add(new Vector2(26f, 9f), ItemRegistry.Create<Object>("(BC)34"));
				}
				break;
			case 'p':
				if (key == "EmilySleeping")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(574, 1892, 11, 11), 1000f, 2, 99999, new Vector2(20f, 3f) * 64f + new Vector2(8f, 32f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999
					});
				}
				break;
			case 'i':
				if (!(key == "shaneHospital"))
				{
					if (key == "grandpaSpirit")
					{
						TemporaryAnimatedSprite temporaryAnimatedSprite3 = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(555, 1956, 18, 35), 9999f, 1, 99999, new Vector2(-1000f, -1010f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							yStopCoordinate = -64128,
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							motion = new Vector2(0f, 1f),
							overrideLocationDestroy = true,
							id = 77777
						};
						location.temporarySprites.Add(temporaryAnimatedSprite3);
						for (int num32 = 0; num32 < 19; num32++)
						{
							location.temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(32f, 32f), Color.White)
							{
								parentSprite = temporaryAnimatedSprite3,
								delayBeforeAnimationStart = (num32 + 1) * 500,
								overrideLocationDestroy = true,
								scale = 1f,
								alpha = 1f
							});
						}
					}
				}
				else
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(533, 1864, 19, 10), 99999f, 1, 99999, new Vector2(20f, 3f) * 64f + new Vector2(16f, 12f), flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999
					});
				}
				break;
			case 'w':
				if (key == "shaneThrowCan")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(542, 1893, 4, 6), 99999f, 1, 99999, new Vector2(103f, 95f) * 64f + new Vector2(0f, 4f) * 4f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(0f, -4f),
						acceleration = new Vector2(0f, 0.25f),
						rotationChange = (float)Math.PI / 128f
					});
					Game1.playSound("shwip");
				}
				break;
			case 'm':
				if (key == "WizardPromise")
				{
					Utility.addSprinklesToLocation(location, 16, 15, 9, 9, 2000, 50, Color.White);
				}
				break;
			case 'f':
				if (key == "linusCampfire")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), 50f, 4, 99999, new Vector2(29f, 9f) * 64f + new Vector2(8f, 0f), flicker: false, flipped: false, 0.0576f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId("linusCampfire"),
						lightRadius = 3f,
						lightcolor = Color.Black
					});
				}
				break;
			case 't':
				if (key == "ccCelebration")
				{
					aboveMapSprites = new TemporaryAnimatedSpriteList();
					for (int num33 = 0; num33 < 32; num33++)
					{
						Vector2 vector4 = new Vector2(Game1.random.Next(Game1.viewport.Width - 128), Game1.viewport.Height + num33 * 64);
						aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(534, 1413, 11, 16), 99999f, 1, 99999, vector4, flicker: false, flipped: false, 1f, 0f, Utility.getRandomRainbowColor(), 4f, 0f, 0f, 0f)
						{
							local = true,
							motion = new Vector2(0.25f, -1.5f),
							acceleration = new Vector2(0f, -0.001f),
							id = 79797 + num33
						});
						aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(545, 1413, 11, 34), 99999f, 1, 99999, vector4 + new Vector2(0f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							local = true,
							motion = new Vector2(0.25f, -1.5f),
							acceleration = new Vector2(0f, -0.001f),
							id = 79797 + num33
						});
					}
					if (Game1.IsWinter)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\marnie_winter_dance", new Microsoft.Xna.Framework.Rectangle(0, 0, 20, 26), 400f, 3, 99999, new Vector2(53f, 21f) * 64f, flicker: false, flipped: false, 0.5f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							pingPong = true
						});
					}
					else
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(558, 1425, 20, 26), 400f, 3, 99999, new Vector2(53f, 21f) * 64f, flicker: false, flipped: false, 0.5f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							pingPong = true
						});
					}
				}
				break;
			case 'g':
				if (key == "alexDiningDog")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(324, 1936, 12, 20), 80f, 4, 99999, new Vector2(7f, 2f) * 64f + new Vector2(2f, -8f) * 4f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 1
					});
				}
				break;
			case 'd':
				if (key == "skateboardFly")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1875, 16, 6), 9999f, 1, 999, new Vector2(26f, 90f) * 64f, flicker: false, flipped: false, 1E-05f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						rotationChange = (float)Math.PI / 24f,
						motion = new Vector2(-8f, -10f),
						acceleration = new Vector2(0.02f, 0.3f),
						yStopCoordinate = 5824,
						xStopCoordinate = 1024,
						layerDepth = 1f
					});
				}
				break;
			case 'R':
				if (key == "sebastianRide")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(405, 1843, 14, 9), 40f, 4, 999, new Vector2(19f, 8f) * 64f + new Vector2(0f, 28f), flicker: false, flipped: false, 0.1792f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-2f, 0f)
					});
				}
				break;
			case 'D':
				if (key == "haleyRoomDark")
				{
					Game1.currentLightSources.Clear();
					Game1.ambientLight = new Color(200, 200, 100);
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(743, 999999f, 1, 0, new Vector2(4f, 1f) * 64f, flicker: false, flipped: false)
					{
						lightId = GenerateLightSourceId("haleyRoomDark"),
						lightcolor = new Color(0, 255, 255),
						lightRadius = 2f
					});
				}
				break;
			case 'c':
				if (key == "maruTelescope")
				{
					for (int num31 = 0; num31 < 9; num31++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(256, 1680, 16, 16), 80f, 5, 0, new Vector2(Game1.random.Next(1, 28), Game1.random.Next(1, 20)) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							delayBeforeAnimationStart = 8000 + num31 * Game1.random.Next(2000),
							motion = new Vector2(4f, 4f)
						});
					}
					if (id == "5183338")
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(206, 1827, 15, 27), 80f, 4, 999, new Vector2(-2f, 13f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 1.2f, 0f)
						{
							delayBeforeAnimationStart = 7000,
							motion = new Vector2(2f, -0.5f),
							alpha = 0.01f,
							alphaFade = -0.005f
						});
					}
				}
				break;
			case 'y':
				if (key == "abbyGraveyard")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(736, 999999f, 1, 0, new Vector2(48f, 86f) * 64f, flicker: false, flipped: false));
				}
				break;
			}
			break;
		case 18:
			switch (key[10])
			{
			case 't':
				if (key == "raccoonbutterflies")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(128, 336, 16, 16), new Vector2(52.5f, 0f) * 64f - new Vector2(131.5f, -60f) * 4f, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						interval = 148f,
						animationLength = 4,
						pingPong = true,
						totalNumberOfLoops = 99999,
						id = 997799,
						scale = 4f,
						xPeriodic = true,
						xPeriodicRange = 32f,
						xPeriodicLoopTime = 2800f,
						alpha = 0.01f,
						alphaFade = -0.01f,
						yPeriodic = true,
						yPeriodicRange = 8f,
						yPeriodicLoopTime = 3800f,
						overrideLocationDestroy = true
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(192, 336, 16, 16), new Vector2(56.5f, 0f) * 64f - new Vector2(131.5f, 0f) * 4f, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						interval = 148f,
						animationLength = 4,
						pingPong = true,
						totalNumberOfLoops = 99999,
						id = 997799,
						scale = 4f,
						xPeriodic = true,
						xPeriodicRange = 32f,
						xPeriodicLoopTime = 2600f,
						alpha = 0.01f,
						alphaFade = -0.01f,
						yPeriodic = true,
						yPeriodicRange = 4f,
						yPeriodicLoopTime = 2900f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(128, 288, 16, 16), new Vector2(53.5f, 0f) * 64f + new Vector2(263f, 24f) * 4f, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						interval = 148f,
						animationLength = 4,
						pingPong = true,
						totalNumberOfLoops = 99999,
						id = 997799,
						scale = 4f,
						xPeriodic = true,
						xPeriodicRange = 32f,
						xPeriodicLoopTime = 3000f,
						alpha = 0.01f,
						alphaFade = -0.01f,
						yPeriodic = true,
						yPeriodicRange = 6f,
						yPeriodicLoopTime = 3100f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(192, 288, 16, 16), new Vector2(52.5f, 0f) * 64f + new Vector2(131.5f, 220f) * 4f, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						interval = 148f,
						animationLength = 4,
						pingPong = true,
						totalNumberOfLoops = 99999,
						id = 997799,
						scale = 4f,
						xPeriodic = true,
						xPeriodicRange = 32f,
						xPeriodicLoopTime = 2400f,
						alpha = 0.01f,
						alphaFade = -0.01f,
						yPeriodic = true,
						yPeriodicRange = 12f,
						yPeriodicLoopTime = 2800f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(64, 288, 16, 16), new Vector2(52.5f, 0f) * 64f + new Vector2(186.5f, 150f) * 4f, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						interval = 148f,
						animationLength = 4,
						pingPong = true,
						totalNumberOfLoops = 99999,
						id = 997799,
						scale = 4f,
						xPeriodic = true,
						xPeriodicRange = 32f,
						xPeriodicLoopTime = 3400f,
						alpha = 0.01f,
						alphaFade = -0.01f,
						yPeriodic = true,
						yPeriodicRange = 4f,
						yPeriodicLoopTime = 3200f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(128, 96, 16, 16), new Vector2(52.5f, 0f) * 64f + new Vector2(211.5f, 180f) * 4f, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						interval = 148f,
						animationLength = 4,
						pingPong = true,
						totalNumberOfLoops = 99999,
						id = 997799,
						scale = 4f,
						xPeriodic = true,
						xPeriodicRange = 32f,
						xPeriodicLoopTime = 3500f,
						alpha = 0.01f,
						alphaFade = -0.01f,
						yPeriodic = true,
						yPeriodicRange = 4f,
						yPeriodicLoopTime = 2700f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(192, 112, 16, 16), new Vector2(52.5f, 0f) * 64f - new Vector2(126.5f, -120f) * 4f, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						interval = 148f,
						animationLength = 4,
						pingPong = true,
						totalNumberOfLoops = 99999,
						id = 997799,
						scale = 4f,
						xPeriodic = true,
						xPeriodicRange = 16f,
						xPeriodicLoopTime = 2500f,
						alpha = 0.01f,
						alphaFade = -0.01f,
						yPeriodic = true,
						yPeriodicRange = 4f,
						yPeriodicLoopTime = 3300f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\critters", new Microsoft.Xna.Framework.Rectangle(128, 288, 16, 16), new Vector2(49.5f, 0f) * 64f - new Vector2(126.5f, -100f) * 4f, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						interval = 148f,
						animationLength = 4,
						pingPong = true,
						totalNumberOfLoops = 99999,
						id = 997799,
						scale = 4f,
						xPeriodic = true,
						xPeriodicRange = 16f,
						xPeriodicLoopTime = 2200f,
						alpha = 0.01f,
						alphaFade = -0.01f,
						yPeriodic = true,
						yPeriodicRange = 4f,
						yPeriodicLoopTime = 3400f
					});
					TemporaryAnimatedSprite temporarySpriteByID4 = location.getTemporarySpriteByID(9786);
					TemporaryAnimatedSprite temporarySpriteByID5 = location.getTemporarySpriteByID(9785);
					temporarySpriteByID4.sourceRect.Y = 224;
					temporarySpriteByID4.sourceRectStartingPos.Y = 224f;
					temporarySpriteByID4.currentParentTileIndex = 3;
					temporarySpriteByID4.timer = 0f;
					temporarySpriteByID4.sourceRect.X = 96;
					temporarySpriteByID5.sourceRect.Y = 224;
					temporarySpriteByID5.sourceRectStartingPos.Y = 224f;
					temporarySpriteByID5.currentParentTileIndex = 3;
					temporarySpriteByID5.timer = 0f;
					temporarySpriteByID5.sourceRect.X = 96;
				}
				break;
			case 'a':
			{
				if (!(key == "terraria_cat_leave"))
				{
					break;
				}
				TemporaryAnimatedSprite terraria_cat = location.getTemporarySpriteByID(777);
				if (terraria_cat == null)
				{
					break;
				}
				terraria_cat.sourceRect.Y = 0;
				terraria_cat.sourceRect.X = terraria_cat.currentParentTileIndex * 16;
				terraria_cat.paused = false;
				terraria_cat.motion = new Vector2(1f, 0f);
				terraria_cat.xStopCoordinate = 1152;
				terraria_cat.flipped = true;
				Microsoft.Xna.Framework.Rectangle warpRect2 = new Microsoft.Xna.Framework.Rectangle(1024, 120, 144, 272);
				terraria_cat.reachedStopCoordinate = delegate
				{
					terraria_cat.position.X = -4000f;
					location.removeTemporarySpritesWithID(888);
					Game1.playSound("terraria_warp");
					for (int num49 = 0; num49 < 80; num49++)
					{
						Vector2 randomPositionInThisRectangle2 = Utility.getRandomPositionInThisRectangle(warpRect2, Game1.random);
						Vector2 vector7 = randomPositionInThisRectangle2 - Utility.PointToVector2(warpRect2.Center);
						vector7.Normalize();
						vector7 *= (float)(Game1.random.Next(10, 21) / 10);
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(113 + Game1.random.Next(3) * 5, 123, 5, 5), 999f, 1, 9999, randomPositionInThisRectangle2, flicker: false, flipped: false, 0.8f, 0.02f, Color.White, 4f, 0f, 0f, 0f)
						{
							layerDepth = 0.99f,
							rotationChange = (float)Game1.random.Next(-10, 10) / 100f,
							motion = vector7,
							acceleration = -vector7 / 150f,
							scaleChange = (float)Game1.random.Next(-10, 0) / 500f,
							delayBeforeAnimationStart = num49 * 5
						});
					}
				};
				break;
			}
			case 'm':
				if (key == "trashBearUmbrella1")
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(0, 80, 46, 56), new Vector2(102f, 94.5f) * 64f, flipped: false, 0f, Color.White)
					{
						animationLength = 1,
						interval = 999999f,
						motion = new Vector2(0f, -9f),
						acceleration = new Vector2(0f, 0.4f),
						scale = 4f,
						layerDepth = 1f,
						id = 777,
						yStopCoordinate = 6144,
						reachedStopCoordinate = delegate(int param)
						{
							location.getTemporarySpriteByID(777).yStopCoordinate = -1;
							location.getTemporarySpriteByID(777).motion = new Vector2(0f, (float)param * 0.75f);
							location.getTemporarySpriteByID(777).acceleration = new Vector2(0.04f, -0.19f);
							location.getTemporarySpriteByID(777).accelerationChange = new Vector2(0f, 0.0015f);
							location.getTemporarySpriteByID(777).sourceRect.X += 46;
							location.playSound("batFlap");
							location.playSound("tinyWhip");
						}
					});
				}
				break;
			case 'e':
				if (key == "movieTheater_setup")
				{
					Game1.currentLightSources.Add(new LightSource("Event_MovieProjector", 7, new Vector2(192f, 64f) + new Vector2(64f, 80f) * 4f, 4f, LightSource.LightContext.None, 0L));
					location.temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.temporaryContent.Load<Texture2D>("Maps\\MovieTheaterScreen_TileSheet"),
						sourceRect = new Microsoft.Xna.Framework.Rectangle(224, 0, 96, 112),
						sourceRectStartingPos = new Vector2(224f, 0f),
						animationLength = 1,
						interval = 5000f,
						totalNumberOfLoops = 9999,
						scale = 4f,
						position = new Vector2(4f, 4f) * 64f,
						layerDepth = 1f,
						id = 999,
						delayBeforeAnimationStart = 7950
					});
				}
				break;
			case 'i':
				if (key == "missingJunimoStars")
				{
					location.removeTemporarySpritesWithID(999);
					Texture2D texture8 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					for (int num30 = 0; num30 < 48; num30++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite
						{
							texture = texture8,
							sourceRect = new Microsoft.Xna.Framework.Rectangle(477, 306, 28, 28),
							sourceRectStartingPos = new Vector2(477f, 306f),
							animationLength = 1,
							interval = 5000f,
							totalNumberOfLoops = 10,
							scale = Game1.random.Next(1, 5),
							position = Utility.getTopLeftPositionForCenteringOnScreen(Game1.viewport, 84, 84) + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)),
							rotationChange = (float)Math.PI / (float)Game1.random.Next(16, 128),
							motion = new Vector2((float)Game1.random.Next(-30, 40) / 10f, (float)Game1.random.Next(20, 90) * -0.1f),
							acceleration = new Vector2(0f, 0.05f),
							local = true,
							layerDepth = (float)num30 / 100f,
							color = (Game1.random.NextBool() ? Color.White : Utility.getRandomRainbowColor())
						});
					}
				}
				break;
			case 'r':
				if (key == "sebastianFrogHouse")
				{
					Point spouseRoomCorner = (location as FarmHouse).GetSpouseRoomCorner();
					spouseRoomCorner.X++;
					spouseRoomCorner.Y += 6;
					Vector2 vector3 = Utility.PointToVector2(spouseRoomCorner);
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(641, 1534, 48, 37),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(641f, 1534f),
						interval = 5000f,
						totalNumberOfLoops = 9999,
						position = vector3 * 64f + new Vector2(0f, -5f) * 4f,
						scale = 4f,
						layerDepth = (vector3.Y + 2f + 0.1f) * 64f / 10000f
					});
					Texture2D texture6 = Game1.temporaryContent.Load<Texture2D>("TileSheets\\critters");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture6,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 224, 16, 16),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(0f, 224f),
						interval = 5000f,
						totalNumberOfLoops = 9999,
						position = vector3 * 64f + new Vector2(25f, 2f) * 4f,
						scale = 4f,
						flipped = true,
						layerDepth = (vector3.Y + 2f + 0.11f) * 64f / 10000f,
						id = 777
					});
				}
				break;
			case 'h':
				if (!(key == "harveyKitchenFlame"))
				{
					if (key == "harveyKitchenSetup")
					{
						Texture2D texture9 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
						location.TemporarySprites.Add(new TemporaryAnimatedSprite
						{
							texture = texture9,
							sourceRect = new Microsoft.Xna.Framework.Rectangle(379, 251, 31, 13),
							animationLength = 1,
							sourceRectStartingPos = new Vector2(379f, 251f),
							interval = 5000f,
							totalNumberOfLoops = 9999,
							position = new Vector2(22f, 22f) * 64f + new Vector2(-2f, 6f) * 4f,
							scale = 4f,
							layerDepth = 0.15551999f
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite
						{
							texture = texture9,
							sourceRect = new Microsoft.Xna.Framework.Rectangle(391, 235, 5, 13),
							animationLength = 1,
							sourceRectStartingPos = new Vector2(391f, 235f),
							interval = 5000f,
							totalNumberOfLoops = 9999,
							position = new Vector2(21f, 22f) * 64f + new Vector2(8f, 4f) * 4f,
							scale = 4f,
							layerDepth = 0.15551999f
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite
						{
							texture = texture9,
							sourceRect = new Microsoft.Xna.Framework.Rectangle(399, 229, 11, 21),
							animationLength = 1,
							sourceRectStartingPos = new Vector2(399f, 229f),
							interval = 5000f,
							totalNumberOfLoops = 9999,
							position = new Vector2(19f, 22f) * 64f + new Vector2(8f, -5f) * 4f,
							scale = 4f,
							layerDepth = 0.15551999f
						});
						location.temporarySprites.Add(new TemporaryAnimatedSprite(27, new Vector2(21f, 22f) * 64f + new Vector2(0f, -5f) * 4f, Color.White, 10)
						{
							totalNumberOfLoops = 999,
							layerDepth = 0.15616f
						});
						location.temporarySprites.Add(new TemporaryAnimatedSprite(27, new Vector2(21f, 22f) * 64f + new Vector2(24f, -5f) * 4f, Color.White, 10)
						{
							totalNumberOfLoops = 999,
							flipped = true,
							delayBeforeAnimationStart = 400,
							layerDepth = 0.15616f
						});
					}
				}
				else
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11),
						animationLength = 4,
						sourceRectStartingPos = new Vector2(276f, 1985f),
						interval = 100f,
						totalNumberOfLoops = 6,
						position = new Vector2(22f, 22f) * 64f + new Vector2(8f, 5f) * 4f,
						scale = 4f,
						layerDepth = 0.15584001f
					});
				}
				break;
			case 'P':
				if (key == "farmerHoldPainting")
				{
					Texture2D texture7 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					location.getTemporarySpriteByID(888).sourceRect.X += 15;
					location.getTemporarySpriteByID(888).sourceRectStartingPos.X += 15f;
					location.removeTemporarySpritesWithID(444);
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture7,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(476, 394, 25, 22),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(476f, 394f),
						interval = 5000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(75f, 40f) * 64f + new Vector2(-4f, -33f) * 4f,
						scale = 4f,
						layerDepth = 1f,
						id = 777
					});
				}
				break;
			case 's':
			{
				if (!(key == "farmerForestVision"))
				{
					break;
				}
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(393, 1973, 1, 1), 9999f, 1, 999999, new Vector2(0f, 0f) * 64f, flicker: false, flipped: false, 0.9f, 0f, Color.LimeGreen * 0.85f, Game1.viewport.Width * 2, 0f, 0f, 0f, local: true)
				{
					alpha = 0f,
					alphaFade = -0.002f,
					id = 1
				});
				Game1.player.mailReceived.Add("canReadJunimoText");
				int num26 = -64;
				int num27 = -64;
				int num28 = 0;
				int num29 = 0;
				while (num27 < Game1.viewport.Height + 128)
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(367 + ((num28 % 2 == 0) ? 8 : 0), 1969, 8, 8), 9999f, 1, 999999, new Vector2(num26, num27), flicker: false, flipped: false, 0.99f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true)
					{
						alpha = 0f,
						alphaFade = -0.0015f,
						xPeriodic = true,
						xPeriodicLoopTime = 4000f,
						xPeriodicRange = 64f,
						yPeriodic = true,
						yPeriodicLoopTime = 5000f,
						yPeriodicRange = 96f,
						rotationChange = (float)Game1.random.Next(-1, 2) * (float)Math.PI / 256f,
						id = 1,
						delayBeforeAnimationStart = 20 * num28
					});
					num26 += 128;
					if (num26 > Game1.viewport.Width + 64)
					{
						num29++;
						num26 = ((num29 % 2 == 0) ? (-64) : 64);
						num27 += 128;
					}
					num28++;
				}
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 895, 51, 101), 9999f, 1, 999999, new Vector2(Game1.viewport.Width / 2 - 100, Game1.viewport.Height / 2 - 240), flicker: false, flipped: false, 1f, 0f, Color.White, 3f, 0f, 0f, 0f, local: true)
				{
					alpha = 0f,
					alphaFade = -0.001f,
					id = 1,
					delayBeforeAnimationStart = 6000,
					scaleChange = 0.004f,
					xPeriodic = true,
					xPeriodicLoopTime = 4000f,
					xPeriodicRange = 64f,
					yPeriodic = true,
					yPeriodicLoopTime = 5000f,
					yPeriodicRange = 32f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 895, 51, 101), 9999f, 1, 999999, new Vector2(Game1.viewport.Width / 4 - 100, Game1.viewport.Height / 4 - 120), flicker: false, flipped: false, 0.99f, 0f, Color.White, 3f, 0f, 0f, 0f, local: true)
				{
					alpha = 0f,
					alphaFade = -0.001f,
					id = 1,
					delayBeforeAnimationStart = 9000,
					scaleChange = 0.004f,
					xPeriodic = true,
					xPeriodicLoopTime = 4000f,
					xPeriodicRange = 64f,
					yPeriodic = true,
					yPeriodicLoopTime = 5000f,
					yPeriodicRange = 32f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 895, 51, 101), 9999f, 1, 999999, new Vector2(Game1.viewport.Width * 3 / 4, Game1.viewport.Height / 3 - 120), flicker: false, flipped: false, 0.98f, 0f, Color.White, 3f, 0f, 0f, 0f, local: true)
				{
					alpha = 0f,
					alphaFade = -0.001f,
					id = 1,
					delayBeforeAnimationStart = 12000,
					scaleChange = 0.004f,
					xPeriodic = true,
					xPeriodicLoopTime = 4000f,
					xPeriodicRange = 64f,
					yPeriodic = true,
					yPeriodicLoopTime = 5000f,
					yPeriodicRange = 32f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 895, 51, 101), 9999f, 1, 999999, new Vector2(Game1.viewport.Width / 3 - 60, Game1.viewport.Height * 3 / 4 - 120), flicker: false, flipped: false, 0.97f, 0f, Color.White, 3f, 0f, 0f, 0f, local: true)
				{
					alpha = 0f,
					alphaFade = -0.001f,
					id = 1,
					delayBeforeAnimationStart = 15000,
					scaleChange = 0.004f,
					xPeriodic = true,
					xPeriodicLoopTime = 4000f,
					xPeriodicRange = 64f,
					yPeriodic = true,
					yPeriodicLoopTime = 5000f,
					yPeriodicRange = 32f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 895, 51, 101), 9999f, 1, 999999, new Vector2(Game1.viewport.Width * 2 / 3, Game1.viewport.Height * 2 / 3 - 120), flicker: false, flipped: false, 0.96f, 0f, Color.White, 3f, 0f, 0f, 0f, local: true)
				{
					alpha = 0f,
					alphaFade = -0.001f,
					id = 1,
					delayBeforeAnimationStart = 18000,
					scaleChange = 0.004f,
					xPeriodic = true,
					xPeriodicLoopTime = 4000f,
					xPeriodicRange = 64f,
					yPeriodic = true,
					yPeriodicLoopTime = 5000f,
					yPeriodicRange = 32f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 895, 51, 101), 9999f, 1, 999999, new Vector2(Game1.viewport.Width / 8, Game1.viewport.Height / 5 - 120), flicker: false, flipped: false, 0.95f, 0f, Color.White, 3f, 0f, 0f, 0f, local: true)
				{
					alpha = 0f,
					alphaFade = -0.001f,
					id = 1,
					delayBeforeAnimationStart = 19500,
					scaleChange = 0.004f,
					xPeriodic = true,
					xPeriodicLoopTime = 4000f,
					xPeriodicRange = 64f,
					yPeriodic = true,
					yPeriodicLoopTime = 5000f,
					yPeriodicRange = 32f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 895, 51, 101), 9999f, 1, 999999, new Vector2(Game1.viewport.Width * 2 / 3, Game1.viewport.Height / 5 - 120), flicker: false, flipped: false, 0.94f, 0f, Color.White, 3f, 0f, 0f, 0f, local: true)
				{
					alpha = 0f,
					alphaFade = -0.001f,
					id = 1,
					delayBeforeAnimationStart = 21000,
					scaleChange = 0.004f,
					xPeriodic = true,
					xPeriodicLoopTime = 4000f,
					xPeriodicRange = 64f,
					yPeriodic = true,
					yPeriodicLoopTime = 5000f,
					yPeriodicRange = 32f
				});
				break;
			}
			}
			break;
		case 14:
			switch (key[6])
			{
			case 'n':
				if (key == "raccoonCircle2")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\raccoon_circle_cutout", new Microsoft.Xna.Framework.Rectangle(0, 0, 263, 263), new Vector2(56.5f, 0f) * 64f - new Vector2(131.5f, 44f) * 4f, flipped: false, 0f, Color.White)
					{
						drawAboveAlwaysFront = true,
						interval = 297f,
						animationLength = 3,
						totalNumberOfLoops = 99999,
						id = 997797,
						scale = 4f,
						alpha = 0.01f,
						alphaFade = -0.003f,
						layerDepth = 0.8f
					});
				}
				break;
			case 'L':
				if (key == "georgeLeekGift")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1231, 16, 16), 100f, 6, 1, new Vector2(17f, 19f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999,
						paused = false,
						holdLastFrame = true
					});
				}
				break;
			case 'P':
				if (key == "parrotPerchHut")
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Microsoft.Xna.Framework.Rectangle(0, 0, 24, 24), new Vector2(7f, 4f) * 64f, flipped: false, 0f, Color.White)
					{
						animationLength = 1,
						interval = 999999f,
						scale = 4f,
						layerDepth = 1f,
						id = 999
					});
				}
				break;
			case 'e':
				if (key == "trashBearMagic")
				{
					Utility.addStarsAndSpirals(location, 95, 103, 24, 12, 2000, 10, Color.Lime);
					(location as Forest).removeSewerTrash();
					Game1.flashAlpha = 0.75f;
					Game1.screenGlowOnce(Color.Lime, hold: false, 0.25f, 1f);
				}
				break;
			case 'l':
				if (key == "gridballGameTV")
				{
					Texture2D texture5 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture5,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(368, 336, 19, 14),
						animationLength = 7,
						sourceRectStartingPos = new Vector2(368f, 336f),
						interval = 5000f,
						totalNumberOfLoops = 99999,
						position = new Vector2(34f, 3f) * 64f + new Vector2(7f, 13f) * 4f,
						scale = 4f,
						layerDepth = 1f
					});
				}
				break;
			case 'h':
				if (key == "waterShaneDone")
				{
					farmer.completelyStopAnimatingOrDoingAction();
					farmer.TemporaryItem = null;
					drawTool = false;
					location.removeTemporarySpritesWithID(999);
				}
				break;
			case 'a':
				if (key == "shanePassedOut")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(533, 1864, 19, 27), 99999f, 1, 99999, new Vector2(25f, 7f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(552, 1862, 31, 21), 99999f, 1, 99999, new Vector2(25f, 7f) * 64f + new Vector2(-16f, 0f), flicker: false, flipped: false, 0.0001f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			case 'C':
				if (key == "junimoCageGone")
				{
					location.removeTemporarySpritesWithID(1);
				}
				break;
			case 'G':
				if (key == "secretGiftOpen")
				{
					TemporaryAnimatedSprite temporarySpriteByID3 = location.getTemporarySpriteByID(666);
					if (temporarySpriteByID3 != null)
					{
						temporarySpriteByID3.animationLength = 6;
						temporarySpriteByID3.interval = 100f;
						temporarySpriteByID3.totalNumberOfLoops = 1;
						temporarySpriteByID3.timer = 0f;
						temporarySpriteByID3.holdLastFrame = true;
					}
				}
				break;
			case 'B':
				if (key == "candleBoatMove")
				{
					showGroundObjects = false;
					location.getTemporarySpriteByID(1).motion = new Vector2(0f, 2f);
				}
				break;
			case 'i':
				if (key == "pennyFieldTrip")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 1813, 86, 54), 999999f, 1, 0, new Vector2(68f, 44f) * 64f, flicker: false, flipped: false, 0.0001f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			}
			break;
		case 11:
			switch (key[5])
			{
			case 'o':
				if (key == "raccoonSong")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(279, 55, 12, 15), 297f, 8, 999, new Vector2(3706f, 340f) - new Vector2(6.5f, 12f) * 4f, flicker: false, flipped: false)
					{
						scale = 4f,
						layerDepth = 0.044809997f,
						usePreciseTiming = true
					});
					for (int num14 = 0; num14 < 8; num14++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(304, 397, 11, 11), 49f, 12, 1, new Vector2(3706f, 340f) + new Vector2(14f, -12f) * 4f, flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.05057f,
							delayBeforeAnimationStart = 2376 * num14,
							usePreciseTiming = true,
							motion = new Vector2(1f, 0f),
							acceleration = new Vector2(0f, 0.001f),
							color = new Color(255, 200, 200),
							rotationChange = (float)Game1.random.Next(-20, 20) / 1000f
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(455, 414, 14, 17), 2376f, 1, 999, new Vector2(3706f, 340f) + new Vector2(7f, -12f) * 4f, flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.051209997f,
							delayBeforeAnimationStart = 2376 * num14,
							alphaFade = 0.02f,
							usePreciseTiming = true
						});
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(374, 55, 12, 15), 297f, 8, 999, new Vector2(54f, 4f) * 64f + new Vector2(0f, -16f), flicker: false, flipped: true)
					{
						scale = 4f,
						layerDepth = 0.044809997f,
						delayBeforeAnimationStart = 297,
						usePreciseTiming = true
					});
					for (int num15 = 0; num15 < 8; num15++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(385, 414, 14, 17), 2376f, 1, 999, new Vector2(54f, 4f) * 64f + new Vector2(16f, -8f) + new Vector2(-15f, -17f) * 4f, flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.051209997f,
							delayBeforeAnimationStart = 2376 * num15 + 297,
							alphaFade = 0.02f,
							usePreciseTiming = true
						});
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(279, 55, 12, 15), 297f, 8, 999, new Vector2(3462f, 433f) - new Vector2(6.5f, 12f) * 4f, flicker: false, flipped: false)
					{
						scale = 4f,
						layerDepth = 0.044809997f,
						delayBeforeAnimationStart = 594,
						usePreciseTiming = true
					});
					for (int num16 = 0; num16 < 8; num16++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(304, 397, 11, 11), 49f, 12, 1, new Vector2(3462f, 433f) + new Vector2(-20f, -16f) + new Vector2(-15f, -17f) * 4f, flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.05057f,
							delayBeforeAnimationStart = 2376 * num16 + 594,
							usePreciseTiming = true,
							motion = new Vector2(-1f, -1f),
							acceleration = new Vector2(0f, 0.001f),
							color = new Color(180, 200, 255),
							rotationChange = (float)Game1.random.Next(-20, 20) / 1000f
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(371, 414, 14, 17), 2376f, 1, 999, new Vector2(3462f, 433f) + new Vector2(-20f, -16f) + new Vector2(-15f, -17f) * 4f, flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.051209997f,
							delayBeforeAnimationStart = 2376 * num16 + 594,
							alphaFade = 0.013f,
							usePreciseTiming = true
						});
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(374, 55, 12, 15), 297f, 8, 999, new Vector2(58f, 4f) * 64f + new Vector2(0f, -24f), flicker: false, flipped: false)
					{
						scale = 4f,
						layerDepth = 0.044809997f,
						delayBeforeAnimationStart = 891,
						usePreciseTiming = true
					});
					for (int num17 = 0; num17 < 8; num17++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(440, 415, 14, 15), 2376f, 1, 999, new Vector2(58f, 4f) * 64f + new Vector2(48f, -56f), flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.051209997f,
							delayBeforeAnimationStart = 2376 * num17 + 891,
							alphaFade = 0.02f,
							usePreciseTiming = true
						});
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(279, 55, 12, 15), 297f, 8, 999, new Vector2(3770f, 408f) - new Vector2(6.5f, 12f) * 4f, flicker: false, flipped: false)
					{
						scale = 4f,
						layerDepth = 0.044809997f,
						delayBeforeAnimationStart = 1188,
						usePreciseTiming = true
					});
					for (int num18 = 0; num18 < 8; num18++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(469, 415, 14, 14), 2376f, 1, 999, new Vector2(3770f, 408f) + new Vector2(24f, -64f), flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.051209997f,
							delayBeforeAnimationStart = 2376 * num18 + 1188,
							alphaFade = 0.02f,
							usePreciseTiming = true
						});
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(279, 55, 12, 15), 297f, 8, 999, new Vector2(55f, 3f) * 64f + new Vector2(12f, 4f) - new Vector2(6.5f, 12f) * 4f, flicker: false, flipped: false)
					{
						scale = 4f,
						layerDepth = 0.044809997f,
						delayBeforeAnimationStart = 1485,
						usePreciseTiming = true
					});
					for (int num19 = 0; num19 < 8; num19++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(400, 414, 12, 16), 2376f, 1, 999, new Vector2(55f, 3f) * 64f + new Vector2(-32f, -100f), flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.051209997f,
							delayBeforeAnimationStart = 2376 * num19 + 1485,
							alphaFade = 0.02f,
							usePreciseTiming = true
						});
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(279, 55, 12, 15), 297f, 8, 999, new Vector2(56f, 3f) * 64f + new Vector2(40f, -8f) - new Vector2(6.5f, 12f) * 4f, flicker: false, flipped: false)
					{
						scale = 4f,
						layerDepth = 0.044809997f,
						delayBeforeAnimationStart = 1782,
						usePreciseTiming = true
					});
					for (int num20 = 0; num20 < 8; num20++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(304, 397, 11, 11), 49f, 12, 1, new Vector2(56f, 3f) * 64f + new Vector2(12f, -112f), flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.05057f,
							delayBeforeAnimationStart = 2376 * num20 + 1782,
							usePreciseTiming = true,
							motion = new Vector2(-0.25f, -1.5f),
							acceleration = new Vector2(0f, 0.001f),
							color = new Color(220, 255, 180),
							rotationChange = (float)Game1.random.Next(-20, 20) / 1000f
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(414, 414, 12, 16), 2376f, 1, 999, new Vector2(56f, 3f) * 64f + new Vector2(12f, -112f), flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.051209997f,
							delayBeforeAnimationStart = 2376 * num20 + 1782,
							alphaFade = 0.013f,
							usePreciseTiming = true
						});
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(374, 55, 12, 15), 297f, 8, 999, new Vector2(58f, 3f) * 64f + new Vector2(-24f, -52f), flicker: false, flipped: false)
					{
						scale = 4f,
						layerDepth = 0.044809997f,
						delayBeforeAnimationStart = 2079,
						usePreciseTiming = true
					});
					for (int num21 = 0; num21 < 8; num21++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(426, 414, 14, 15), 2376f, 1, 999, new Vector2(58f, 3f) * 64f + new Vector2(28f, -88f), flicker: false, flipped: false)
						{
							scale = 4f,
							layerDepth = 0.051209997f,
							delayBeforeAnimationStart = 2376 * num21 + 2079,
							alphaFade = 0.02f,
							usePreciseTiming = true
						});
					}
				}
				break;
			case 's':
				if (!(key == "krobusBeach"))
				{
					if (!(key == "krobusraven"))
					{
						break;
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Characters\\KrobusRaven", new Microsoft.Xna.Framework.Rectangle(0, 0, 32, 32), 100f, 5, 999999, new Vector2(Game1.viewport.Width, (float)Game1.viewport.Height * 0.33f), flicker: false, flipped: false, 0.9f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true)
					{
						pingPong = true,
						motion = new Vector2(-2f, 0f),
						yPeriodic = true,
						yPeriodicLoopTime = 3000f,
						yPeriodicRange = 16f,
						startSound = "shadowpeep"
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Characters\\KrobusRaven", new Microsoft.Xna.Framework.Rectangle(0, 32, 32, 32), 30f, 5, 999999, new Vector2(Game1.viewport.Width, (float)Game1.viewport.Height * 0.33f), flicker: false, flipped: false, 0.9f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true)
					{
						motion = new Vector2(-2.5f, 0f),
						yPeriodic = true,
						yPeriodicLoopTime = 2800f,
						yPeriodicRange = 16f,
						delayBeforeAnimationStart = 8000
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Characters\\KrobusRaven", new Microsoft.Xna.Framework.Rectangle(0, 64, 32, 39), 100f, 4, 999999, new Vector2(Game1.viewport.Width, (float)Game1.viewport.Height * 0.33f), flicker: false, flipped: false, 0.9f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true)
					{
						pingPong = true,
						motion = new Vector2(-3f, 0f),
						yPeriodic = true,
						yPeriodicLoopTime = 2000f,
						yPeriodicRange = 16f,
						delayBeforeAnimationStart = 15000,
						startSound = "fireball"
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1886, 35, 29), 9999f, 1, 999999, new Vector2(Game1.viewport.Width, (float)Game1.viewport.Height * 0.33f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-3f, 0f),
						yPeriodic = true,
						yPeriodicLoopTime = 2200f,
						yPeriodicRange = 32f,
						local = true,
						delayBeforeAnimationStart = 20000
					});
					for (int num22 = 0; num22 < 12; num22++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(16, 594, 16, 12), 100f, 2, 999999, new Vector2(Game1.viewport.Width, (float)Game1.viewport.Height * 0.33f + (float)Game1.random.Next(-128, 128)), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(-2f, 0f),
							yPeriodic = true,
							yPeriodicLoopTime = Game1.random.Next(1500, 2000),
							yPeriodicRange = 32f,
							local = true,
							delayBeforeAnimationStart = 24000 + num22 * 200,
							startSound = ((num22 == 0) ? "yoba" : null)
						});
					}
					int num23 = 0;
					if (Game1.player.mailReceived.Contains("Capsule_Broken"))
					{
						for (int num24 = 0; num24 < 3; num24++)
						{
							location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(639, 785, 16, 16), 100f, 4, 999999, new Vector2(Game1.viewport.Width, (float)Game1.viewport.Height * 0.33f + (float)Game1.random.Next(-128, 128)), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
							{
								motion = new Vector2(-2f, 0f),
								yPeriodic = true,
								yPeriodicLoopTime = Game1.random.Next(1500, 2000),
								yPeriodicRange = 16f,
								local = true,
								delayBeforeAnimationStart = 30000 + num24 * 500,
								startSound = ((num24 == 0) ? "UFO" : null)
							});
						}
						num23 += 5000;
					}
					if (Game1.year <= 2)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(150, 259, 9, 9), 10f, 4, 9999999, new Vector2(Game1.viewport.Width + 4, (float)Game1.viewport.Height * 0.33f + 44f), flicker: false, flipped: false, 0.9f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true)
						{
							motion = new Vector2(-2f, 0f),
							yPeriodic = true,
							yPeriodicLoopTime = 3000f,
							yPeriodicRange = 8f,
							delayBeforeAnimationStart = 30000 + num23
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("Characters\\KrobusRaven", new Microsoft.Xna.Framework.Rectangle(2, 129, 120, 27), 1090f, 1, 999999, new Vector2(Game1.viewport.Width, (float)Game1.viewport.Height * 0.33f), flicker: false, flipped: false, 0.9f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true)
						{
							motion = new Vector2(-2f, 0f),
							yPeriodic = true,
							yPeriodicLoopTime = 3000f,
							yPeriodicRange = 8f,
							startSound = "discoverMineral",
							delayBeforeAnimationStart = 30000 + num23
						});
						num23 += 5000;
					}
					else if (Game1.year <= 3)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(150, 259, 9, 9), 10f, 4, 9999999, new Vector2(Game1.viewport.Width + 4, (float)Game1.viewport.Height * 0.33f + 44f), flicker: false, flipped: false, 0.9f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true)
						{
							motion = new Vector2(-2f, 0f),
							yPeriodic = true,
							yPeriodicLoopTime = 3000f,
							yPeriodicRange = 8f,
							delayBeforeAnimationStart = 30000 + num23
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("Characters\\KrobusRaven", new Microsoft.Xna.Framework.Rectangle(1, 104, 100, 24), 1090f, 1, 999999, new Vector2(Game1.viewport.Width, (float)Game1.viewport.Height * 0.33f), flicker: false, flipped: false, 0.9f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true)
						{
							motion = new Vector2(-2f, 0f),
							yPeriodic = true,
							yPeriodicLoopTime = 3000f,
							yPeriodicRange = 8f,
							startSound = "newArtifact",
							delayBeforeAnimationStart = 30000 + num23
						});
						num23 += 5000;
					}
					if (Game1.MasterPlayer.totalMoneyEarned >= 100000000)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("Characters\\KrobusRaven", new Microsoft.Xna.Framework.Rectangle(125, 108, 34, 50), 1090f, 1, 999999, new Vector2(Game1.viewport.Width, (float)Game1.viewport.Height * 0.33f), flicker: false, flipped: false, 0.9f, 0f, Color.White, 4f, 0f, 0f, 0f, local: true)
						{
							motion = new Vector2(-2f, 0f),
							yPeriodic = true,
							yPeriodicLoopTime = 3000f,
							yPeriodicRange = 8f,
							startSound = "discoverMineral",
							delayBeforeAnimationStart = 30000 + num23
						});
						num23 += 5000;
					}
				}
				else
				{
					for (int num25 = 0; num25 < 8; num25++)
					{
						location.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), 150f, 4, 0, new Vector2(84f + ((num25 % 2 == 0) ? 0.25f : (-0.05f)), 41f) * 64f, flicker: false, Game1.random.NextBool(), 0.001f, 0.02f, Color.White, 0.75f, 0.003f, 0f, 0f)
						{
							delayBeforeAnimationStart = 500 + num25 * 1000,
							startSound = "waterSlosh"
						});
					}
					underwaterSprites = new TemporaryAnimatedSpriteList
					{
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(82f, 52f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId("krobusBeach_1"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2688,
							delayBeforeAnimationStart = 0,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(82f, 52f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId("krobusBeach_2"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 3008,
							delayBeforeAnimationStart = 2000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(88f, 52f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId("krobusBeach_3"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2688,
							delayBeforeAnimationStart = 150,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(88f, 52f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId("krobusBeach_4"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 3008,
							delayBeforeAnimationStart = 2000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(90f, 52f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId("krobusBeach_5"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2816,
							delayBeforeAnimationStart = 300,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(79f, 52f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId("krobusBeach_6"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2816,
							delayBeforeAnimationStart = 1000,
							pingPong = true
						}
					};
				}
				break;
			case 'w':
				if (key == "woodswalker")
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
						sourceRect = new Microsoft.Xna.Framework.Rectangle(448, 419, 16, 21),
						sourceRectStartingPos = new Vector2(448f, 419f),
						animationLength = 4,
						totalNumberOfLoops = 7,
						interval = 150f,
						scale = 4f,
						position = new Vector2(4f, 1f) * 64f + new Vector2(5f, 22f) * 4f,
						shakeIntensity = 1f,
						motion = new Vector2(1f, 0f),
						xStopCoordinate = 576,
						layerDepth = 1f,
						id = 996
					});
				}
				break;
			case 'g':
				if (key == "springOnion")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(1, 129, 16, 16), 200f, 8, 999999, new Vector2(84f, 39f) * 64f, flicker: false, flipped: false, 0.4736f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999
					});
				}
				break;
			case 'i':
				if (key == "curtainOpen")
				{
					location.getTemporarySpriteByID(999).sourceRect.X = 672;
					Game1.playSound("shwip");
				}
				break;
			case 't':
				switch (key)
				{
				case "parrotSlide":
					location.getTemporarySpriteByID(666).yStopCoordinate = 5632;
					location.getTemporarySpriteByID(666).motion.X = 0f;
					location.getTemporarySpriteByID(666).motion.Y = 1f;
					break;
				case "parrotSplat":
					aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 165, 24, 22), 100f, 6, 9999, new Vector2(Game1.viewport.X + Game1.graphics.GraphicsDevice.Viewport.Width, Game1.viewport.Y + 64), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999,
						motion = new Vector2(-2f, 4f),
						acceleration = new Vector2(-0.1f, 0f),
						delayBeforeAnimationStart = 0,
						yStopCoordinate = 5568,
						xStopCoordinate = 1504,
						reachedStopCoordinate = parrotSplat
					});
					break;
				case "elliottBoat":
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(461, 1843, 32, 51), 1000f, 2, 9999, new Vector2(15f, 26f) * 64f + new Vector2(-28f, 0f), flicker: false, flipped: false, 0.1664f, 0f, Color.White, 4f, 0f, 0f, 0f));
					break;
				}
				break;
			case 'C':
				if (key == "shaneCliffs")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(533, 1864, 19, 27), 99999f, 1, 99999, new Vector2(83f, 98f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(552, 1862, 31, 21), 99999f, 1, 99999, new Vector2(83f, 98f) * 64f + new Vector2(-16f, 0f), flicker: false, flipped: false, 0.0001f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(549, 1891, 19, 12), 99999f, 1, 99999, new Vector2(84f, 99f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(549, 1891, 19, 12), 99999f, 1, 99999, new Vector2(82f, 98f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(542, 1893, 4, 6), 99999f, 1, 99999, new Vector2(83f, 99f) * 64f + new Vector2(-8f, 4f) * 4f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			case 'f':
				if (key == "jasGiftOpen")
				{
					location.getTemporarySpriteByID(999).paused = false;
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(537, 1850, 11, 10), 1500f, 1, 1, new Vector2(23f, 16f) * 64f + new Vector2(16f, -48f), flicker: false, flipped: false, 0.99f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(0f, -0.25f),
						delayBeforeAnimationStart = 500,
						yStopCoordinate = 928
					});
					location.temporarySprites.AddRange(Utility.sparkleWithinArea(new Microsoft.Xna.Framework.Rectangle(1440, 992, 128, 64), 5, Color.White, 300));
				}
				break;
			case 'd':
				if (key == "wizardWarp2")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(387, 1965, 16, 31), 9999f, 1, 999999, new Vector2(54f, 34f) * 64f + new Vector2(0f, 4f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-1f, 2f),
						acceleration = new Vector2(-0.1f, 0.2f),
						scaleChange = 0.03f,
						alphaFade = 0.001f
					});
				}
				break;
			case 'L':
				if (key == "linusLights")
				{
					string text = GenerateLightSourceId("linusLights");
					Game1.currentLightSources.Add(new LightSource(text + "1", 2, new Vector2(55f, 62f) * 64f, 2f, LightSource.LightContext.None, 0L));
					Game1.currentLightSources.Add(new LightSource(text + "2", 2, new Vector2(60f, 62f) * 64f, 2f, LightSource.LightContext.None, 0L));
					Game1.currentLightSources.Add(new LightSource(text + "3", 2, new Vector2(57f, 60f) * 64f, 3f, LightSource.LightContext.None, 0L));
					Game1.currentLightSources.Add(new LightSource(text + "4", 2, new Vector2(57f, 60f) * 64f, 2f, LightSource.LightContext.None, 0L));
					Game1.currentLightSources.Add(new LightSource(text + "5", 2, new Vector2(47f, 70f) * 64f, 2f, LightSource.LightContext.None, 0L));
					Game1.currentLightSources.Add(new LightSource(text + "6", 2, new Vector2(52f, 63f) * 64f, 2f, LightSource.LightContext.None, 0L));
				}
				break;
			case 'l':
				if (key == "dickGlitter")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(432, 1435, 16, 16), 100f, 6, 99999, new Vector2(47f, 8f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 2f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(432, 1435, 16, 16), 100f, 6, 99999, new Vector2(47f, 8f) * 64f + new Vector2(32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						delayBeforeAnimationStart = 200
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(432, 1435, 16, 16), 100f, 6, 99999, new Vector2(47f, 8f) * 64f + new Vector2(32f, 32f), flicker: false, flipped: false, 1f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						delayBeforeAnimationStart = 300
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(432, 1435, 16, 16), 100f, 6, 99999, new Vector2(47f, 8f) * 64f + new Vector2(0f, 32f), flicker: false, flipped: false, 1f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						delayBeforeAnimationStart = 100
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(432, 1435, 16, 16), 100f, 6, 99999, new Vector2(47f, 8f) * 64f + new Vector2(16f, 16f), flicker: false, flipped: false, 1f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						delayBeforeAnimationStart = 400
					});
				}
				break;
			}
			break;
		case 19:
			switch (key[0])
			{
			case 't':
			{
				if (!(key == "terraria_warp_begin"))
				{
					break;
				}
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(0, 18, 36, 68), 90f, 3, 9999, new Vector2(16f, 5f) * 64f + new Vector2(0f, -50f) * 4f, flicker: false, flipped: false, 0.8f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					layerDepth = 0.8f,
					id = 888
				});
				TemporaryAnimatedSprite cat_sprite = new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 16), 90f, 8, 9999, new Vector2(16f, 5f) * 64f + new Vector2(34f, -12f) * 4f, flicker: false, flipped: false, 0.8f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					id = 777,
					layerDepth = 0.85f,
					motion = new Vector2(-1f, 0f),
					delayBeforeAnimationStart = 1000,
					xStopCoordinate = 960
				};
				cat_sprite.reachedStopCoordinate = delegate
				{
					cat_sprite.paused = true;
					cat_sprite.sourceRect = new Microsoft.Xna.Framework.Rectangle(112, 16, 16, 16);
					DelayedAction.functionAfterDelay(delegate
					{
						Game1.playSound("terraria_meowmere");
						cat_sprite.shakeIntensity = 1f;
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\weapons", new Microsoft.Xna.Framework.Rectangle(16, 128, 16, 16), 1000f, 1, 1, new Vector2(15f, 5f) * 64f, flicker: false, flipped: false, 0.8f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							layerDepth = 0.86f,
							motion = new Vector2(-1f, -4f),
							acceleration = new Vector2(0f, 0.1f)
						});
					}, 1000);
					DelayedAction.functionAfterDelay(delegate
					{
						cat_sprite.shakeIntensity = 0f;
					}, 1300);
				};
				location.TemporarySprites.Add(cat_sprite);
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(4, 88, 19, 15), 90f, 3, 9999, new Vector2(16f, 5f) * 64f + new Vector2(31f, -10f) * 4f, flicker: false, flipped: false, 0.8f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					layerDepth = 0.9f,
					id = 888
				});
				Microsoft.Xna.Framework.Rectangle r = new Microsoft.Xna.Framework.Rectangle(1024, 120, 144, 272);
				for (int num13 = 0; num13 < 80; num13++)
				{
					Vector2 randomPositionInThisRectangle = Utility.getRandomPositionInThisRectangle(r, Game1.random);
					Vector2 vector2 = randomPositionInThisRectangle - Utility.PointToVector2(r.Center);
					vector2.Normalize();
					vector2 *= (float)(Game1.random.Next(10, 21) / 10);
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\terraria_cat", new Microsoft.Xna.Framework.Rectangle(113 + Game1.random.Next(3) * 5, 123, 5, 5), 999f, 1, 9999, randomPositionInThisRectangle, flicker: false, flipped: false, 0.8f, 0.02f, Color.White, 4f, 0f, 0f, 0f)
					{
						layerDepth = 0.99f,
						rotationChange = (float)Game1.random.Next(-10, 10) / 100f,
						motion = vector2,
						acceleration = -vector2 / 150f,
						scaleChange = (float)Game1.random.Next(-10, 0) / 500f,
						delayBeforeAnimationStart = num13 * 5
					});
				}
				break;
			}
			case 'm':
			{
				if (!(key == "movieTheater_screen"))
				{
					break;
				}
				if (!ArgUtility.TryGet(args, 2, out var value, out var error, allowBlank: true, "string movieId") || !ArgUtility.TryGetInt(args, 3, out var value2, out error, "int screenIndex") || !ArgUtility.TryGetBool(args, 4, out var value3, out error, "bool shake"))
				{
					LogCommandError(args, error);
					break;
				}
				value = MovieTheater.GetMovieIdFromLegacyIndex(value);
				if (!MovieTheater.TryGetMovieData(value, out var data2))
				{
					LogCommandError(args, "No movie found with ID '" + value + "'.");
					break;
				}
				Microsoft.Xna.Framework.Rectangle sourceRectForScreen2 = MovieTheater.GetSourceRectForScreen(data2.SheetIndex, value2);
				location.removeTemporarySpritesWithIDLocal(998);
				if (value2 >= 0)
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.temporaryContent.Load<Texture2D>(data2.Texture ?? "LooseSprites\\Movies"),
						sourceRect = sourceRectForScreen2,
						sourceRectStartingPos = new Vector2(sourceRectForScreen2.X, sourceRectForScreen2.Y),
						animationLength = 1,
						totalNumberOfLoops = 9999,
						interval = 5000f,
						scale = 4f,
						position = new Vector2(4f, 1f) * 64f + new Vector2(3f, 7f) * 4f,
						shakeIntensity = (value3 ? 1f : 0f),
						layerDepth = 0.0128f,
						id = 998
					});
				}
				break;
			}
			case 'w':
				if (key == "willyCrabExperiment")
				{
					Texture2D texture3 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 127, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 127f),
						pingPong = true,
						interval = 250f,
						totalNumberOfLoops = 99999,
						id = 11,
						position = new Vector2(2f, 4f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 146, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 146f),
						pingPong = true,
						interval = 200f,
						totalNumberOfLoops = 99999,
						id = 1,
						initialPosition = new Vector2(2f, 6f) * 64f,
						yPeriodic = true,
						yPeriodicLoopTime = 8000f,
						yPeriodicRange = 32f,
						position = new Vector2(2f, 6f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 127, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 127f),
						pingPong = true,
						interval = 100f,
						totalNumberOfLoops = 99999,
						id = 11,
						position = new Vector2(1f, 5.75f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 127, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 127f),
						pingPong = true,
						interval = 100f,
						totalNumberOfLoops = 99999,
						id = 11,
						position = new Vector2(5f, 3f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 127, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 127f),
						pingPong = true,
						interval = 140f,
						totalNumberOfLoops = 99999,
						id = 22,
						position = new Vector2(4f, 6f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 127, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 127f),
						pingPong = true,
						interval = 140f,
						totalNumberOfLoops = 99999,
						id = 22,
						position = new Vector2(8.5f, 5f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 146, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 146f),
						pingPong = true,
						interval = 170f,
						totalNumberOfLoops = 99999,
						id = 222,
						position = new Vector2(6f, 3.25f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 146, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 146f),
						pingPong = true,
						interval = 190f,
						totalNumberOfLoops = 99999,
						id = 222,
						position = new Vector2(6f, 6f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 146, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 146f),
						pingPong = true,
						interval = 150f,
						totalNumberOfLoops = 99999,
						id = 222,
						position = new Vector2(7f, 4f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 146, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 146f),
						pingPong = true,
						interval = 200f,
						totalNumberOfLoops = 99999,
						id = 2,
						position = new Vector2(4f, 7f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 127, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 127f),
						pingPong = true,
						interval = 180f,
						totalNumberOfLoops = 99999,
						id = 3,
						position = new Vector2(8f, 6f) * 64f,
						yPeriodic = true,
						yPeriodicLoopTime = 10000f,
						yPeriodicRange = 32f,
						initialPosition = new Vector2(8f, 6f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 146, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 146f),
						pingPong = true,
						interval = 220f,
						totalNumberOfLoops = 99999,
						id = 33,
						position = new Vector2(9f, 6f) * 64f,
						scale = 4f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture3,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(259, 146, 18, 18),
						animationLength = 3,
						sourceRectStartingPos = new Vector2(259f, 146f),
						pingPong = true,
						interval = 150f,
						totalNumberOfLoops = 99999,
						id = 33,
						position = new Vector2(10f, 5f) * 64f,
						scale = 4f
					});
				}
				break;
			case 'E':
			{
				if (!(key == "EmilySongBackLights"))
				{
					break;
				}
				aboveMapSprites = new TemporaryAnimatedSpriteList();
				for (int l = 0; l < 5; l++)
				{
					for (int m = 0; m < Game1.graphics.GraphicsDevice.Viewport.Height + 48; m += 48)
					{
						aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(681, 1890, 18, 12), 42241f, 1, 1, new Vector2((l + 1) * Game1.graphics.GraphicsDevice.Viewport.Width / 5 - Game1.graphics.GraphicsDevice.Viewport.Width / 7, -24 + m), flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							xPeriodic = true,
							xPeriodicLoopTime = 1760f,
							xPeriodicRange = 128 + m / 12 * 4,
							delayBeforeAnimationStart = l * 100 + m / 4,
							local = true
						});
					}
				}
				for (int n = 0; n < 27; n++)
				{
					int num2 = 0;
					int num3 = Game1.random.Next(64, Game1.graphics.GraphicsDevice.Viewport.Height - 64);
					int num4 = Game1.random.Next(800, 2000);
					int num5 = Game1.random.Next(32, 64);
					bool pulse = Game1.random.NextDouble() < 0.25;
					int num6 = Game1.random.Next(-6, -3);
					for (int num7 = 0; num7 < 8; num7++)
					{
						aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(616 + num2 * 10, 1891, 10, 10), 42241f, 1, 1, new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width, num3), flicker: false, flipped: false, 0.01f, 0f, Color.White * (1f - (float)num7 * 0.11f), 4f, 0f, 0f, 0f)
						{
							yPeriodic = true,
							motion = new Vector2(num6, 0f),
							yPeriodicLoopTime = num4,
							pulse = pulse,
							pulseTime = 440f,
							pulseAmount = 1.5f,
							yPeriodicRange = num5,
							delayBeforeAnimationStart = 14000 + n * 900 + num7 * 100,
							local = true
						});
					}
				}
				for (int num8 = 0; num8 < 15; num8++)
				{
					int num9 = 0;
					int num10 = Game1.random.Next(Game1.graphics.GraphicsDevice.Viewport.Width - 128);
					for (int num11 = Game1.graphics.GraphicsDevice.Viewport.Height; num11 >= -64; num11 -= 48)
					{
						aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(597, 1888, 16, 16), 99999f, 1, 99999, new Vector2(num10, num11), flicker: false, flipped: false, 1f, 0.02f, Color.White, 4f, 0f, -(float)Math.PI / 2f, 0f)
						{
							delayBeforeAnimationStart = 27500 + num8 * 880 + num9 * 25,
							local = true
						});
						num9++;
					}
				}
				for (int num12 = 0; num12 < 120; num12++)
				{
					aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(626 + num12 / 28 * 10, 1891, 10, 10), 2000f, 1, 1, new Vector2(Game1.random.Next(Game1.graphics.GraphicsDevice.Viewport.Width), Game1.random.Next(Game1.graphics.GraphicsDevice.Viewport.Height)), flicker: false, flipped: false, 0.01f, 0f, Color.White, 0.1f, 0f, 0f, 0f)
					{
						motion = new Vector2(0f, -2f),
						alphaFade = 0.002f,
						scaleChange = 0.5f,
						scaleChangeChange = -0.0085f,
						delayBeforeAnimationStart = 27500 + num12 * 110,
						local = true
					});
				}
				break;
			}
			}
			break;
		case 15:
			switch (key[10])
			{
			case 's':
				if (key == "LeoWillyFishing")
				{
					for (int j = 0; j < 20; j++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite(0, new Vector2(42.5f, 38f) * 64f + new Vector2(Game1.random.Next(64), Game1.random.Next(64)), Color.White * 0.7f)
						{
							layerDepth = (float)(1280 + j) / 10000f,
							delayBeforeAnimationStart = j * 150
						});
					}
				}
				break;
			case 'o':
				if (key == "LeoLinusCooking")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\springobjects", new Microsoft.Xna.Framework.Rectangle(240, 128, 16, 16), 9999f, 1, 1, new Vector2(29f, 8.5f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						layerDepth = 1f
					});
					for (int k = 0; k < 10; k++)
					{
						Utility.addSmokePuff(location, new Vector2(29.5f, 8.6f) * 64f, k * 500);
					}
				}
				break;
			case 'L':
				if (key == "BoatParrotLeave")
				{
					TemporaryAnimatedSprite temporaryAnimatedSprite = aboveMapSprites[0];
					temporaryAnimatedSprite.motion = new Vector2(4f, -6f);
					temporaryAnimatedSprite.sourceRect.X = 48;
					temporaryAnimatedSprite.sourceRectStartingPos.X = 48f;
					temporaryAnimatedSprite.animationLength = 3;
					temporaryAnimatedSprite.pingPong = true;
				}
				break;
			case 'q':
				if (key == "parrotHutSquawk")
				{
					(location as IslandHut).parrotUpgradePerches[0].timeUntilSqwawk = 1f;
				}
				break;
			case 'r':
				if (key == "coldstarMiracle")
				{
					if (!MovieTheater.TryGetMovieData("winter_movie_0", out var data))
					{
						Game1.log.Error("Can't find data for movie 'winter_movie_0'.");
						break;
					}
					Microsoft.Xna.Framework.Rectangle sourceRectForScreen = MovieTheater.GetSourceRectForScreen(data.SheetIndex, 9);
					location.temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.temporaryContent.Load<Texture2D>(data.Texture ?? "LooseSprites\\Movies"),
						sourceRect = sourceRectForScreen,
						sourceRectStartingPos = new Vector2(sourceRectForScreen.X, sourceRectForScreen.Y),
						animationLength = 1,
						totalNumberOfLoops = 1,
						interval = 99999f,
						alpha = 0.01f,
						alphaFade = -0.01f,
						scale = 4f,
						position = new Vector2(4f, 1f) * 64f + new Vector2(3f, 7f) * 4f,
						layerDepth = 0.8535f,
						id = 989
					});
				}
				break;
			case 'l':
				if (key == "junimoSpotlight")
				{
					actors[0].drawOnTop = true;
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
						sourceRect = new Microsoft.Xna.Framework.Rectangle(316, 123, 67, 43),
						sourceRectStartingPos = new Vector2(316f, 123f),
						animationLength = 1,
						interval = 5000f,
						totalNumberOfLoops = 9999,
						scale = 4f,
						position = Utility.getTopLeftPositionForCenteringOnScreen(Game1.viewport, 268, 172, 0, -20),
						layerDepth = 0.0001f,
						local = true,
						id = 999
					});
				}
				break;
			case 'e':
				switch (key)
				{
				case "harveyDinnerSet":
				{
					Vector2 vector = new Vector2(5f, 16f);
					if (location is DecoratableLocation decoratableLocation)
					{
						foreach (Furniture item in decoratableLocation.furniture)
						{
							if (item.furniture_type.Value == 14 && !location.hasTileAt((int)item.tileLocation.X, (int)item.tileLocation.Y + 1, "Buildings") && !location.hasTileAt((int)item.tileLocation.X + 1, (int)item.tileLocation.Y + 1, "Buildings") && !location.hasTileAt((int)item.tileLocation.X + 2, (int)item.tileLocation.Y + 1, "Buildings") && !location.hasTileAt((int)item.tileLocation.X - 1, (int)item.tileLocation.Y + 1, "Buildings"))
							{
								vector = new Vector2((int)item.TileLocation.X, (int)item.TileLocation.Y + 1);
								item.isOn.Value = true;
								item.setFireplace(playSound: false);
								break;
							}
						}
					}
					location.TemporarySprites.Clear();
					getActorByName("Harvey").setTilePosition((int)vector.X + 2, (int)vector.Y);
					getActorByName("Harvey").Position = new Vector2(getActorByName("Harvey").Position.X - 32f, getActorByName("Harvey").Position.Y);
					farmer.Position = new Vector2(vector.X * 64f - 32f, vector.Y * 64f + 32f);
					Object objectAtTile = location.getObjectAtTile((int)vector.X, (int)vector.Y);
					if (objectAtTile != null)
					{
						objectAtTile.isTemporarilyInvisible = true;
					}
					objectAtTile = location.getObjectAtTile((int)vector.X + 1, (int)vector.Y);
					if (objectAtTile != null)
					{
						objectAtTile.isTemporarilyInvisible = true;
					}
					objectAtTile = location.getObjectAtTile((int)vector.X - 1, (int)vector.Y);
					if (objectAtTile != null)
					{
						objectAtTile.isTemporarilyInvisible = true;
					}
					objectAtTile = location.getObjectAtTile((int)vector.X + 2, (int)vector.Y);
					if (objectAtTile != null)
					{
						objectAtTile.isTemporarilyInvisible = true;
					}
					Texture2D texture2 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture2,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(385, 423, 48, 32),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(385f, 423f),
						interval = 5000f,
						totalNumberOfLoops = 9999,
						position = vector * 64f + new Vector2(-8f, -16f) * 4f,
						scale = 4f,
						layerDepth = (vector.Y + 0.2f) * 64f / 10000f,
						lightId = GenerateLightSourceId("harveyDinnerSet"),
						lightRadius = 4f,
						lightcolor = Color.Black
					});
					List<string> list2 = eventCommands.ToList();
					list2.Insert(CurrentCommand + 1, "viewport " + (int)vector.X + " " + (int)vector.Y + " true");
					eventCommands = list2.ToArray();
					break;
				}
				case "ClothingTherapy":
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(644, 1405, 28, 46), 999999f, 1, 99999, new Vector2(5f, 6f) * 64f + new Vector2(-32f, -144f), flicker: false, flipped: false, 0.0424f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999
					});
					break;
				case "getEndSlideshow":
				{
					Summit obj = location as Summit;
					string[] collection = ParseCommands(obj.getEndSlideshow());
					List<string> list = eventCommands.ToList();
					list.InsertRange(CurrentCommand + 1, collection);
					eventCommands = list.ToArray();
					obj.isShowingEndSlideshow = true;
					break;
				}
				}
				break;
			case 'n':
				switch (key)
				{
				case "shaneSaloonCola":
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(552, 1862, 31, 21),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(552f, 1862f),
						interval = 999999f,
						totalNumberOfLoops = 99999,
						position = new Vector2(32f, 17f) * 64f + new Vector2(10f, 3f) * 4f,
						scale = 4f,
						layerDepth = 1E-07f
					});
					break;
				case "springOnionPeel":
				{
					TemporaryAnimatedSprite temporarySpriteByID2 = location.getTemporarySpriteByID(777);
					temporarySpriteByID2.sourceRectStartingPos = new Vector2(144f, 327f);
					temporarySpriteByID2.sourceRect = new Microsoft.Xna.Framework.Rectangle(144, 327, 112, 112);
					break;
				}
				case "springOnionDemo":
				{
					Texture2D texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(144, 215, 112, 112),
						animationLength = 2,
						sourceRectStartingPos = new Vector2(144f, 215f),
						interval = 200f,
						totalNumberOfLoops = 99999,
						id = 777,
						position = new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width / 2 - 264, Game1.graphics.GraphicsDevice.Viewport.Height / 3 - 264),
						local = true,
						scale = 4f,
						destroyable = false,
						overrideLocationDestroy = true
					});
					break;
				}
				case "sebastianOnBike":
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 1600, 64, 128), 80f, 8, 9999, new Vector2(19f, 27f) * 64f + new Vector2(32f, -16f), flicker: false, flipped: true, 0.1792f, 0f, Color.White, 1f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(405, 1854, 47, 33), 9999f, 1, 999, new Vector2(17f, 27f) * 64f + new Vector2(0f, -8f), flicker: false, flipped: false, 0.1792f, 0f, Color.White, 4f, 0f, 0f, 0f));
					break;
				}
				break;
			case 'P':
				if (key == "shaneCliffProps")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(549, 1891, 19, 12), 99999f, 1, 99999, new Vector2(104f, 96f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999
					});
				}
				break;
			case 'm':
				if (key == "grandpaThumbsUp")
				{
					TemporaryAnimatedSprite temporarySpriteByID = location.getTemporarySpriteByID(77777);
					temporarySpriteByID.texture = Game1.mouseCursors2;
					temporarySpriteByID.sourceRect = new Microsoft.Xna.Framework.Rectangle(186, 265, 22, 34);
					temporarySpriteByID.sourceRectStartingPos = new Vector2(186f, 265f);
					temporarySpriteByID.yPeriodic = true;
					temporarySpriteByID.yPeriodicLoopTime = 1000f;
					temporarySpriteByID.yPeriodicRange = 16f;
					temporarySpriteByID.xPeriodicLoopTime = 2500f;
					temporarySpriteByID.xPeriodicRange = 16f;
					temporarySpriteByID.initialPosition = temporarySpriteByID.position;
				}
				break;
			case 'G':
				if (key == "junimoCageGone2")
				{
					location.removeTemporarySpritesWithID(1);
					Game1.viewportFreeze = true;
					Game1.viewport.X = -1000;
					Game1.viewport.Y = -1000;
				}
				break;
			case 'C':
				if (key == "iceFishingCatch")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(160, 368, 16, 32), 500f, 3, 99999, new Vector2(68f, 30f) * 64f, flicker: false, flipped: false, 0.1984f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(160, 368, 16, 32), 510f, 3, 99999, new Vector2(74f, 30f) * 64f, flicker: false, flipped: false, 0.1984f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(160, 368, 16, 32), 490f, 3, 99999, new Vector2(67f, 36f) * 64f, flicker: false, flipped: false, 0.2368f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(160, 368, 16, 32), 500f, 3, 99999, new Vector2(76f, 35f) * 64f, flicker: false, flipped: false, 0.2304f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			case 'a':
				if (key == "sebastianGarage")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1843, 48, 42), 9999f, 1, 999, new Vector2(17f, 23f) * 64f + new Vector2(0f, 8f), flicker: false, flipped: false, 0.1472f, 0f, Color.White, 4f, 0f, 0f, 0f));
					getActorByName("Sebastian").HideShadow = true;
				}
				break;
			case 'c':
				if (key == "abbyvideoscreen")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(167, 1714, 19, 14), 100f, 3, 9999, new Vector2(2f, 3f) * 64f + new Vector2(7f, 12f) * 4f, flicker: false, flipped: false, 0.0002f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			}
			break;
		case 16:
			switch (key[0])
			{
			case 'B':
				if (key == "BoatParrotSquawk")
				{
					TemporaryAnimatedSprite temporaryAnimatedSprite6 = aboveMapSprites[0];
					temporaryAnimatedSprite6.sourceRect.X = 24;
					temporaryAnimatedSprite6.sourceRectStartingPos.X = 24f;
					Game1.playSound("parrot_squawk");
				}
				break;
			case 'i':
				if (key == "islandFishSplash")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\springobjects", new Microsoft.Xna.Framework.Rectangle(336, 544, 16, 16), 100000f, 1, 1, new Vector2(81f, 92f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 9999,
						motion = new Vector2(-2f, -8f),
						acceleration = new Vector2(0f, 0.2f),
						flipped = true,
						rotationChange = -0.02f,
						yStopCoordinate = 5952,
						layerDepth = 0.99f,
						reachedStopCoordinate = delegate
						{
							location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\springobjects", new Microsoft.Xna.Framework.Rectangle(48, 16, 16, 16), 100f, 5, 1, location.getTemporarySpriteByID(9999).position, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
							{
								layerDepth = 1f
							});
							location.removeTemporarySpritesWithID(9999);
							Game1.playSound("waterSlosh");
						}
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\springobjects", new Microsoft.Xna.Framework.Rectangle(48, 16, 16, 16), 100f, 5, 1, new Vector2(81f, 92f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						layerDepth = 1f
					});
				}
				break;
			case 't':
				if (key == "trashBearPrelude")
				{
					Utility.addStarsAndSpirals(location, 95, 106, 23, 4, 10000, 275, Color.Lime);
				}
				break;
			case 'l':
				if (key == "leahHoldPainting")
				{
					Texture2D texture16 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					location.getTemporarySpriteByID(999).sourceRect.X += 15;
					location.getTemporarySpriteByID(999).sourceRectStartingPos.X += 15f;
					int num48 = ((!Game1.netWorldState.Value.hasWorldStateID("m_painting0")) ? (Game1.netWorldState.Value.hasWorldStateID("m_painting1") ? 1 : 2) : 0);
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture16,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(400 + num48 * 25, 394, 25, 23),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(400 + num48 * 25, 394f),
						interval = 5000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(73f, 38f) * 64f + new Vector2(-2f, -16f) * 4f,
						scale = 4f,
						layerDepth = 1f,
						id = 777
					});
				}
				break;
			case 'E':
				if (key == "EmilyBoomBoxStop")
				{
					location.getTemporarySpriteByID(999).pulse = false;
					location.getTemporarySpriteByID(999).scale = 4f;
				}
				break;
			case 'w':
				if (key == "wizardSewerMagic")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), 50f, 4, 20, new Vector2(15f, 13f) * 64f + new Vector2(8f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId("wizardSewerMagic_1"),
						lightRadius = 1f,
						lightcolor = Color.Black,
						alphaFade = 0.005f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), 50f, 4, 20, new Vector2(17f, 13f) * 64f + new Vector2(8f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId("wizardSewerMagic_2"),
						lightRadius = 1f,
						lightcolor = Color.Black,
						alphaFade = 0.005f
					});
				}
				break;
			case 'm':
				if (key == "moonlightJellies")
				{
					int num47 = 1;
					showGroundObjects = false;
					npcControllers?.Clear();
					underwaterSprites = new TemporaryAnimatedSpriteList
					{
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(26f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2560,
							delayBeforeAnimationStart = 10000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(29f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2560,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(31f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2624,
							delayBeforeAnimationStart = 12000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(20f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 1728,
							delayBeforeAnimationStart = 14000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(17f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 1856,
							delayBeforeAnimationStart = 19500,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(16f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2048,
							delayBeforeAnimationStart = 20300,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(17f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2496,
							delayBeforeAnimationStart = 21500,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(16f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2816,
							delayBeforeAnimationStart = 22400,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(12f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2688,
							delayBeforeAnimationStart = 23200,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(9f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2752,
							delayBeforeAnimationStart = 24000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(18f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 1920,
							delayBeforeAnimationStart = 24600,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(33f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2560,
							delayBeforeAnimationStart = 25600,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(36f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2496,
							delayBeforeAnimationStart = 26900,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(304, 16, 16, 16), 200f, 3, 9999, new Vector2(21f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1.5f),
							xPeriodic = true,
							xPeriodicLoopTime = 2500f,
							xPeriodicRange = 10f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2176,
							delayBeforeAnimationStart = 28000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(304, 16, 16, 16), 200f, 3, 9999, new Vector2(20f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1.5f),
							xPeriodic = true,
							xPeriodicLoopTime = 2500f,
							xPeriodicRange = 10f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2240,
							delayBeforeAnimationStart = 28500,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(304, 16, 16, 16), 200f, 3, 9999, new Vector2(22f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1.5f),
							xPeriodic = true,
							xPeriodicLoopTime = 2500f,
							xPeriodicRange = 10f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2304,
							delayBeforeAnimationStart = 28500,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(304, 16, 16, 16), 200f, 3, 9999, new Vector2(33f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1.5f),
							xPeriodic = true,
							xPeriodicLoopTime = 2500f,
							xPeriodicRange = 10f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2752,
							delayBeforeAnimationStart = 29000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(304, 16, 16, 16), 200f, 3, 9999, new Vector2(36f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1.5f),
							xPeriodic = true,
							xPeriodicLoopTime = 2500f,
							xPeriodicRange = 10f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2752,
							delayBeforeAnimationStart = 30000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 32, 16, 16), 250f, 3, 9999, new Vector2(28f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(-0.5f, -0.5f),
							xPeriodic = true,
							xPeriodicLoopTime = 4000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 2f,
							xStopCoordinate = 1216,
							yStopCoordinate = 2432,
							delayBeforeAnimationStart = 32000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(40f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2560,
							delayBeforeAnimationStart = 10000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(42f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2752,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(43f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2624,
							delayBeforeAnimationStart = 12000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(45f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2496,
							delayBeforeAnimationStart = 14000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(46f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 1856,
							delayBeforeAnimationStart = 19500,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(48f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2240,
							delayBeforeAnimationStart = 20300,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(49f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2560,
							delayBeforeAnimationStart = 21500,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(50f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 1920,
							delayBeforeAnimationStart = 22400,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(51f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2112,
							delayBeforeAnimationStart = 23200,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(52f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2432,
							delayBeforeAnimationStart = 24000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(53f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2240,
							delayBeforeAnimationStart = 24600,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(54f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 1920,
							delayBeforeAnimationStart = 25600,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(55f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2560,
							delayBeforeAnimationStart = 26900,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(4f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 1920,
							delayBeforeAnimationStart = 24000,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(5f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2560,
							delayBeforeAnimationStart = 24600,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(3f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2176,
							delayBeforeAnimationStart = 25600,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(6f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2368,
							delayBeforeAnimationStart = 26900,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(256, 16, 16, 16), 250f, 3, 9999, new Vector2(8f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1f),
							xPeriodic = true,
							xPeriodicLoopTime = 3000f,
							xPeriodicRange = 16f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2688,
							delayBeforeAnimationStart = 26900,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(304, 16, 16, 16), 200f, 3, 9999, new Vector2(50f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1.5f),
							xPeriodic = true,
							xPeriodicLoopTime = 2500f,
							xPeriodicRange = 10f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2688,
							delayBeforeAnimationStart = 28500,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(304, 16, 16, 16), 200f, 3, 9999, new Vector2(51f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1.5f),
							xPeriodic = true,
							xPeriodicLoopTime = 2500f,
							xPeriodicRange = 10f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2752,
							delayBeforeAnimationStart = 28500,
							pingPong = true
						},
						new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(304, 16, 16, 16), 200f, 3, 9999, new Vector2(52f, 49f) * 64f, flicker: false, flipped: false, 0.1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(0f, -1.5f),
							xPeriodic = true,
							xPeriodicLoopTime = 2500f,
							xPeriodicRange = 10f,
							lightId = GenerateLightSourceId($"moonlightJellies_{num47++}"),
							lightcolor = Color.Black,
							lightRadius = 1f,
							yStopCoordinate = 2816,
							delayBeforeAnimationStart = 29000,
							pingPong = true
						}
					};
				}
				break;
			case 'a':
				if (key == "abbyOuijaCandles")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(737, 999999f, 1, 0, new Vector2(5f, 9f) * 64f, flicker: false, flipped: false)
					{
						lightId = GenerateLightSourceId("abbyOuijaCandles_1"),
						lightRadius = 1f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(737, 999999f, 1, 0, new Vector2(7f, 8f) * 64f, flicker: false, flipped: false)
					{
						lightId = GenerateLightSourceId("abbyOuijaCandles_2"),
						lightRadius = 1f
					});
				}
				break;
			}
			break;
		case 10:
			switch (key[6])
			{
			case 'r':
				if (!(key == "BoatParrot"))
				{
					if (!(key == "movieFrame"))
					{
						break;
					}
					if (!ArgUtility.TryGet(args, 2, out var value13, out var error7, allowBlank: true, "string movieId") || !ArgUtility.TryGetInt(args, 3, out var value14, out error7, "int frame") || !ArgUtility.TryGetInt(args, 4, out var value15, out error7, "int duration"))
					{
						LogCommandError(args, error7);
						break;
					}
					value13 = MovieTheater.GetMovieIdFromLegacyIndex(value13);
					if (!MovieTheater.TryGetMovieData(value13, out var data3))
					{
						LogCommandError(args, "no movie found with ID '" + value13 + "'");
						break;
					}
					Microsoft.Xna.Framework.Rectangle sourceRectForScreen3 = MovieTheater.GetSourceRectForScreen(data3.SheetIndex, value14);
					location.temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.temporaryContent.Load<Texture2D>(data3.Texture ?? "LooseSprites\\Movies"),
						sourceRect = sourceRectForScreen3,
						sourceRectStartingPos = new Vector2(sourceRectForScreen3.X, sourceRectForScreen3.Y),
						animationLength = 1,
						totalNumberOfLoops = 1,
						interval = value15,
						scale = 4f,
						position = new Vector2(4f, 1f) * 64f + new Vector2(3f, 7f) * 4f,
						shakeIntensity = 0.25f,
						layerDepth = 0.0192f,
						id = 997
					});
					break;
				}
				aboveMapSprites = new TemporaryAnimatedSpriteList();
				aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\parrots", new Microsoft.Xna.Framework.Rectangle(48, 0, 24, 24), 100f, 3, 99999, new Vector2(Game1.viewport.X - 64, 2112f), flicker: false, flipped: true, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					id = 999,
					motion = new Vector2(6f, 1f),
					delayBeforeAnimationStart = 0,
					pingPong = true,
					xStopCoordinate = 1040,
					reachedStopCoordinate = delegate
					{
						TemporaryAnimatedSprite temporaryAnimatedSprite7 = aboveMapSprites[0];
						if (temporaryAnimatedSprite7 != null)
						{
							temporaryAnimatedSprite7.motion = new Vector2(0f, 2f);
							temporaryAnimatedSprite7.yStopCoordinate = 2336;
							temporaryAnimatedSprite7.reachedStopCoordinate = delegate
							{
								TemporaryAnimatedSprite temporaryAnimatedSprite8 = aboveMapSprites[0];
								temporaryAnimatedSprite8.animationLength = 1;
								temporaryAnimatedSprite8.pingPong = false;
								temporaryAnimatedSprite8.sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 0, 24, 24);
								temporaryAnimatedSprite8.sourceRectStartingPos = Vector2.Zero;
							};
						}
					}
				});
				break;
			case 'b':
				if (key == "evilRabbit")
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.temporaryContent.Load<Texture2D>("TileSheets\\critters"),
						sourceRect = new Microsoft.Xna.Framework.Rectangle(264, 209, 19, 16),
						sourceRectStartingPos = new Vector2(264f, 209f),
						animationLength = 1,
						totalNumberOfLoops = 999,
						interval = 999f,
						scale = 4f,
						position = new Vector2(4f, 1f) * 64f + new Vector2(38f, 23f) * 4f,
						layerDepth = 1f,
						motion = new Vector2(-2f, -2f),
						acceleration = new Vector2(0f, 0.1f),
						yStopCoordinate = 204,
						xStopCoordinate = 316,
						flipped = true,
						id = 778
					});
				}
				break;
			case 'S':
				if (key == "junimoShow")
				{
					Texture2D texture15 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture15,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(393, 350, 19, 14),
						animationLength = 6,
						sourceRectStartingPos = new Vector2(393f, 350f),
						interval = 90f,
						totalNumberOfLoops = 86,
						position = new Vector2(52f, 24f) * 64f + new Vector2(7f, -2f) * 4f,
						scale = 4f,
						layerDepth = 0.95f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture15,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(393, 364, 19, 14),
						animationLength = 4,
						sourceRectStartingPos = new Vector2(393f, 364f),
						interval = 90f,
						totalNumberOfLoops = 31,
						position = new Vector2(52f, 24f) * 64f + new Vector2(7f, -2f) * 4f,
						scale = 4f,
						layerDepth = 0.97f,
						delayBeforeAnimationStart = 11034
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture15,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(393, 378, 19, 14),
						animationLength = 6,
						sourceRectStartingPos = new Vector2(393f, 378f),
						interval = 90f,
						totalNumberOfLoops = 21,
						position = new Vector2(52f, 24f) * 64f + new Vector2(7f, -2f) * 4f,
						scale = 4f,
						layerDepth = 1f,
						delayBeforeAnimationStart = 22069
					});
				}
				break;
			case 'o':
				if (!(key == "luauShorts"))
				{
					if (key == "linusMoney")
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(397, 1941, 19, 20), 9999f, 1, 99999, new Vector2(-1002f, -1000f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							startSound = "money",
							delayBeforeAnimationStart = 10,
							overrideLocationDestroy = true
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(397, 1941, 19, 20), 9999f, 1, 99999, new Vector2(-1003f, -1002f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							startSound = "money",
							delayBeforeAnimationStart = 100,
							overrideLocationDestroy = true
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(397, 1941, 19, 20), 9999f, 1, 99999, new Vector2(-999f, -1000f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							startSound = "money",
							delayBeforeAnimationStart = 200,
							overrideLocationDestroy = true
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(397, 1941, 19, 20), 9999f, 1, 99999, new Vector2(-1004f, -1001f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							startSound = "money",
							delayBeforeAnimationStart = 300,
							overrideLocationDestroy = true
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(397, 1941, 19, 20), 9999f, 1, 99999, new Vector2(-1001f, -998f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							startSound = "money",
							delayBeforeAnimationStart = 400,
							overrideLocationDestroy = true
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(397, 1941, 19, 20), 9999f, 1, 99999, new Vector2(-998f, -999f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							startSound = "money",
							delayBeforeAnimationStart = 500,
							overrideLocationDestroy = true
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(397, 1941, 19, 20), 9999f, 1, 99999, new Vector2(-998f, -1002f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							startSound = "money",
							delayBeforeAnimationStart = 600,
							overrideLocationDestroy = true
						});
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(397, 1941, 19, 20), 9999f, 1, 99999, new Vector2(-997f, -1001f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							startSound = "money",
							delayBeforeAnimationStart = 700,
							overrideLocationDestroy = true
						});
					}
				}
				else
				{
					Vector2 vector6 = ((Game1.year % 2 == 0) ? new Vector2(24f, 10f) : new Vector2(35f, 10f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\springobjects", new Microsoft.Xna.Framework.Rectangle(336, 512, 16, 16), 9999f, 1, 99999, vector6 * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-2f, -8f),
						acceleration = new Vector2(0f, 0.25f),
						yStopCoordinate = ((int)vector6.Y + 1) * 64,
						xStopCoordinate = ((int)vector6.X - 2) * 64
					});
				}
				break;
			case 'B':
			{
				if (!(key == "arcaneBook"))
				{
					if (key == "candleBoat")
					{
						showGroundObjects = false;
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(240, 112, 16, 32), 1000f, 2, 99999, new Vector2(22f, 36f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							id = 1,
							lightId = GenerateLightSourceId("candleBoat"),
							lightRadius = 2f,
							lightcolor = Color.Black
						});
					}
					break;
				}
				for (int num45 = 0; num45 < 16; num45++)
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(536, 1945, 8, 8), new Vector2(128f, 792f) + new Vector2(Game1.random.Next(32), Game1.random.Next(32) - num45 * 4), flipped: false, 0f, Color.White)
					{
						interval = 50f,
						totalNumberOfLoops = 99999,
						animationLength = 7,
						layerDepth = 1f,
						scale = 4f,
						alphaFade = 0.008f,
						motion = new Vector2(0f, -0.5f)
					});
				}
				aboveMapSprites = new TemporaryAnimatedSpriteList
				{
					new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(325, 1977, 18, 18), new Vector2(160f, 800f), flipped: false, 0f, Color.White)
					{
						interval = 25f,
						totalNumberOfLoops = 99999,
						animationLength = 3,
						layerDepth = 1f,
						scale = 1f,
						scaleChange = 1f,
						scaleChangeChange = -0.05f,
						alpha = 0.65f,
						alphaFade = 0.005f,
						motion = new Vector2(-8f, -8f),
						acceleration = new Vector2(0.4f, 0.4f)
					}
				};
				for (int num46 = 0; num46 < 16; num46++)
				{
					aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Vector2(2f, 12f) * 64f + new Vector2(Game1.random.Next(-32, 64), 0f), flipped: false, 0.002f, Color.Gray)
					{
						alpha = 0.75f,
						motion = new Vector2(1f, -1f) + new Vector2((float)(Game1.random.Next(100) - 50) / 100f, (float)(Game1.random.Next(100) - 50) / 100f),
						interval = 99999f,
						layerDepth = 0.0384f + (float)Game1.random.Next(100) / 10000f,
						scale = 3f,
						scaleChange = 0.01f,
						rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f,
						delayBeforeAnimationStart = num46 * 25
					});
				}
				location.setMapTile(2, 12, 2143, "Front", "untitled tile sheet");
				break;
			}
			case 'G':
				if (!(key == "parrotGone"))
				{
					if (key == "secretGift")
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1231, 16, 16), new Vector2(30f, 70f) * 64f + new Vector2(0f, -21f), flipped: false, 0f, Color.White)
						{
							animationLength = 1,
							interval = 999999f,
							id = 666,
							scale = 4f
						});
					}
				}
				else
				{
					location.removeTemporarySpritesWithID(666);
				}
				break;
			case 'h':
				if (key == "waterShane")
				{
					drawTool = true;
					farmer.TemporaryItem = ItemRegistry.Create("(T)WateringCan");
					farmer.CurrentTool.Update(1, 0, farmer);
					farmer.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[4]
					{
						new FarmerSprite.AnimationFrame(58, 0, secondaryArm: false, flip: false),
						new FarmerSprite.AnimationFrame(58, 75, secondaryArm: false, flip: false, Farmer.showToolSwipeEffect),
						new FarmerSprite.AnimationFrame(59, 100, secondaryArm: false, flip: false, Farmer.useTool, behaviorAtEndOfFrame: true),
						new FarmerSprite.AnimationFrame(45, 500, secondaryArm: true, flip: false, Farmer.canMoveNow, behaviorAtEndOfFrame: true)
					});
				}
				break;
			case 'W':
				if (key == "wizardWarp")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(387, 1965, 16, 31), 9999f, 1, 999999, new Vector2(8f, 16f) * 64f + new Vector2(0f, 4f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(2f, -2f),
						acceleration = new Vector2(0.1f, 0f),
						scaleChange = -0.02f,
						alphaFade = 0.001f
					});
				}
				break;
			case 'l':
				if (key == "witchFlyby")
				{
					Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1886, 35, 29), 9999f, 1, 999999, new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width, 192f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-4f, 0f),
						acceleration = new Vector2(-0.025f, 0f),
						yPeriodic = true,
						yPeriodicLoopTime = 2000f,
						yPeriodicRange = 64f,
						local = true
					});
				}
				break;
			case 'C':
				if (key == "junimoCage")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(325, 1977, 18, 19), 60f, 3, 999999, new Vector2(10f, 17f) * 64f + new Vector2(0f, -4f), flicker: false, flipped: false, 0f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId("junimoCage_1"),
						lightRadius = 1f,
						lightcolor = Color.Black,
						id = 1,
						shakeIntensity = 0f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(379, 1991, 5, 5), 9999f, 1, 999999, new Vector2(10f, 17f) * 64f + new Vector2(0f, -4f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId("junimoCage_2"),
						lightRadius = 0.5f,
						lightcolor = Color.Black,
						id = 1,
						xPeriodic = true,
						xPeriodicLoopTime = 2000f,
						xPeriodicRange = 24f,
						yPeriodic = true,
						yPeriodicLoopTime = 2000f,
						yPeriodicRange = 24f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(379, 1991, 5, 5), 9999f, 1, 999999, new Vector2(10f, 17f) * 64f + new Vector2(72f, -4f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId("junimoCage_3"),
						lightRadius = 0.5f,
						lightcolor = Color.Black,
						id = 1,
						xPeriodic = true,
						xPeriodicLoopTime = 2000f,
						xPeriodicRange = -24f,
						yPeriodic = true,
						yPeriodicLoopTime = 2000f,
						yPeriodicRange = 24f,
						delayBeforeAnimationStart = 250
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(379, 1991, 5, 5), 9999f, 1, 999999, new Vector2(10f, 17f) * 64f + new Vector2(0f, 52f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId("junimoCage_3"),
						lightRadius = 0.5f,
						lightcolor = Color.Black,
						id = 1,
						xPeriodic = true,
						xPeriodicLoopTime = 2000f,
						xPeriodicRange = -24f,
						yPeriodic = true,
						yPeriodicLoopTime = 2000f,
						yPeriodicRange = 24f,
						delayBeforeAnimationStart = 450
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(379, 1991, 5, 5), 9999f, 1, 999999, new Vector2(10f, 17f) * 64f + new Vector2(72f, 52f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId("junimoCage_4"),
						lightRadius = 0.5f,
						lightcolor = Color.Black,
						id = 1,
						xPeriodic = true,
						xPeriodicLoopTime = 2000f,
						xPeriodicRange = 24f,
						yPeriodic = true,
						yPeriodicLoopTime = 2000f,
						yPeriodicRange = 24f,
						delayBeforeAnimationStart = 650
					});
				}
				break;
			case 'n':
				if (key == "joshDinner")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(649, 9999f, 1, 9999, new Vector2(6f, 4f) * 64f + new Vector2(8f, 32f), flicker: false, flipped: false)
					{
						layerDepth = 0.0256f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(664, 9999f, 1, 9999, new Vector2(8f, 4f) * 64f + new Vector2(-8f, 32f), flicker: false, flipped: false)
					{
						layerDepth = 0.0256f
					});
				}
				break;
			case 't':
				if (key == "beachStuff")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(324, 1887, 47, 29), 9999f, 1, 999, new Vector2(44f, 21f) * 64f, flicker: false, flipped: false, 1E-05f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			case 'p':
				if (key == "leahLaptop")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(130, 1849, 19, 19), 9999f, 1, 999, new Vector2(12f, 10f) * 64f + new Vector2(0f, 24f), flicker: false, flipped: false, 0.1856f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			case 'c':
				if (key == "leahPicnic")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(96, 1808, 32, 48), 9999f, 1, 999, new Vector2(75f, 37f) * 64f, flicker: false, flipped: false, 0.2496f, 0f, Color.White, 4f, 0f, 0f, 0f));
					NPC nPC = new NPC(new AnimatedSprite(festivalContent, "Characters\\" + (farmer.IsMale ? "LeahExMale" : "LeahExFemale"), 0, 16, 32), new Vector2(-100f, -100f) * 64f, 2, "LeahEx");
					nPC.AllowDynamicAppearance = false;
					actors.Add(nPC);
				}
				break;
			case 'a':
				if (key == "maruBeaker")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(738, 1380f, 1, 0, new Vector2(9f, 14f) * 64f + new Vector2(0f, 32f), flicker: false, flipped: false)
					{
						rotationChange = (float)Math.PI / 24f,
						motion = new Vector2(0f, -7f),
						acceleration = new Vector2(0f, 0.2f),
						endFunction = beakerSmashEndFunction,
						layerDepth = 1f
					});
				}
				break;
			case 'e':
				if (key == "abbyOneBat")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(640, 1664, 16, 16), 80f, 4, 9999, new Vector2(23f, 9f) * 64f, flicker: false, flipped: false, 1f, 0.003f, Color.White, 4f, 0f, 0f, 0f)
					{
						xPeriodic = true,
						xPeriodicLoopTime = 2000f,
						xPeriodicRange = 128f,
						motion = new Vector2(0f, -8f)
					});
				}
				break;
			case 'w':
				if (key == "swordswipe")
				{
					if (!ArgUtility.TryGetVector2(args, 2, out var value12, out var error6, integerOnly: true, "Vector2 position"))
					{
						LogCommandError(args, error6);
					}
					else
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 960, 128, 128), 60f, 4, 0, value12 * 64f + new Vector2(0f, -32f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f));
					}
				}
				break;
			case 'L':
				if (key == "abbyAtLake")
				{
					int num44 = 1;
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(735, 999999f, 1, 0, new Vector2(48f, 30f) * 64f, flicker: false, flipped: false)
					{
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 2f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(48f, 30f) * 64f + new Vector2(32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 2000f,
						yPeriodicLoopTime = 1600f,
						xPeriodicRange = 32f,
						yPeriodicRange = 21f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(48f, 30f) * 64f + new Vector2(32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 1000f,
						yPeriodicLoopTime = 1600f,
						xPeriodicRange = 16f,
						yPeriodicRange = 21f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(48f, 30f) * 64f + new Vector2(32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 2400f,
						yPeriodicLoopTime = 2800f,
						xPeriodicRange = 21f,
						yPeriodicRange = 32f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(48f, 30f) * 64f + new Vector2(32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 2000f,
						yPeriodicLoopTime = 2400f,
						xPeriodicRange = 16f,
						yPeriodicRange = 16f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(66f, 34f) * 64f + new Vector2(-32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 2000f,
						yPeriodicLoopTime = 2600f,
						xPeriodicRange = 21f,
						yPeriodicRange = 48f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(66f, 34f) * 64f + new Vector2(32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 2000f,
						yPeriodicLoopTime = 2600f,
						xPeriodicRange = 32f,
						yPeriodicRange = 21f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(66f, 34f) * 64f + new Vector2(32f, 32f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 4000f,
						yPeriodicLoopTime = 5000f,
						xPeriodicRange = 42f,
						yPeriodicRange = 32f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(66f, 34f) * 64f + new Vector2(0f, -32f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 4000f,
						yPeriodicLoopTime = 5500f,
						xPeriodicRange = 32f,
						yPeriodicRange = 32f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(69f, 28f) * 64f + new Vector2(-32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 2400f,
						yPeriodicLoopTime = 3600f,
						xPeriodicRange = 32f,
						yPeriodicRange = 21f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(69f, 28f) * 64f + new Vector2(32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 2500f,
						yPeriodicLoopTime = 3600f,
						xPeriodicRange = 42f,
						yPeriodicRange = 51f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(69f, 28f) * 64f + new Vector2(32f, 32f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 4500f,
						yPeriodicLoopTime = 3000f,
						xPeriodicRange = 21f,
						yPeriodicRange = 32f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(69f, 28f) * 64f + new Vector2(0f, -32f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 5000f,
						yPeriodicLoopTime = 4500f,
						xPeriodicRange = 64f,
						yPeriodicRange = 48f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(72f, 33f) * 64f + new Vector2(-32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 2000f,
						yPeriodicLoopTime = 3000f,
						xPeriodicRange = 32f,
						yPeriodicRange = 21f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(72f, 33f) * 64f + new Vector2(32f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 2900f,
						yPeriodicLoopTime = 3200f,
						xPeriodicRange = 21f,
						yPeriodicRange = 32f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(72f, 33f) * 64f + new Vector2(32f, 32f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 4200f,
						yPeriodicLoopTime = 3300f,
						xPeriodicRange = 16f,
						yPeriodicRange = 32f
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(232, 328, 4, 4), 9999999f, 1, 0, new Vector2(72f, 33f) * 64f + new Vector2(0f, -32f), flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f)
					{
						lightcolor = Color.Orange,
						lightId = GenerateLightSourceId($"abbyAtLake_{num44++}"),
						lightRadius = 0.2f,
						xPeriodic = true,
						yPeriodic = true,
						xPeriodicLoopTime = 5100f,
						yPeriodicLoopTime = 4000f,
						xPeriodicRange = 32f,
						yPeriodicRange = 16f
					});
				}
				break;
			}
			break;
		case 12:
			switch (key[4])
			{
			case 'i':
			{
				string value7;
				string error5;
				Microsoft.Xna.Framework.Rectangle value8;
				Vector2 value9;
				int value10;
				float value11;
				if (!(key == "staticSprite"))
				{
					if (key == "morrisFlying")
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(105, 1318, 13, 31), 9999f, 1, 99999, new Vector2(32f, 13f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(4f, -8f),
							rotationChange = (float)Math.PI / 16f,
							shakeIntensity = 1f
						});
					}
				}
				else if (!ArgUtility.TryGet(args, 2, out value7, out error5, allowBlank: true, "string textureName") || !ArgUtility.TryGetRectangle(args, 3, out value8, out error5, "Rectangle sourceRect") || !ArgUtility.TryGetVector2(args, 7, out value9, out error5, integerOnly: false, "Vector2 tile") || !ArgUtility.TryGetOptionalInt(args, 9, out value10, out error5, 999, "int id") || !ArgUtility.TryGetOptionalFloat(args, 10, out value11, out error5, 1f, "float layerDepth"))
				{
					LogCommandError(args, error5);
				}
				else
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite(value7, value8, value9 * 64f, flipped: false, 0f, Color.White)
					{
						animationLength = 1,
						interval = 999999f,
						scale = 4f,
						layerDepth = value11,
						id = value10
					});
				}
				break;
			}
			case 'v':
				if (key == "removeSprite")
				{
					if (!ArgUtility.TryGetInt(args, 2, out var value5, out var error3, "int spriteId"))
					{
						LogCommandError(args, error3);
					}
					else
					{
						location.removeTemporarySpritesWithID(value5);
					}
				}
				break;
			case 'y':
				if (!(key == "EmilyCamping"))
				{
					if (key == "EmilyBoomBox")
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(586, 1871, 24, 14), 99999f, 1, 99999, new Vector2(15f, 4f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							id = 999
						});
					}
					break;
				}
				showGroundObjects = false;
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(644, 1578, 59, 53), 999999f, 1, 99999, new Vector2(26f, 9f) * 64f + new Vector2(-16f, 0f), flicker: false, flipped: false, 0.0788f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					id = 999
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(675, 1299, 29, 24), 999999f, 1, 99999, new Vector2(27f, 14f) * 64f, flicker: false, flipped: false, 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					id = 99
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), new Vector2(27f, 14f) * 64f + new Vector2(8f, 4f) * 4f, flipped: false, 0f, Color.White)
				{
					interval = 50f,
					totalNumberOfLoops = 99999,
					animationLength = 4,
					lightId = GenerateLightSourceId("EmilyCamping_1"),
					id = 666,
					lightRadius = 2f,
					scale = 4f,
					layerDepth = 0.01f
				});
				Game1.currentLightSources.Add(new LightSource(GenerateLightSourceId("EmilyCamping_2"), 4, new Vector2(27f, 14f) * 64f, 2f, LightSource.LightContext.None, 0L));
				location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(585, 1846, 26, 22), 999999f, 1, 99999, new Vector2(25f, 12f) * 64f + new Vector2(-32f, 0f), flicker: false, flipped: false, 0.001f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					id = 96
				});
				AmbientLocationSounds.addSound(new Vector2(27f, 14f), 1);
				break;
			case 'a':
				if (key == "curtainClose")
				{
					location.getTemporarySpriteByID(999).sourceRect.X = 644;
					Game1.playSound("shwip");
				}
				break;
			case 'd':
				if (key == "grandpaNight")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 1453, 639, 176), 9999f, 1, 999999, new Vector2(0f, 1f) * 64f, flicker: false, flipped: false, 0.9f, 0f, Color.Cyan, 4f, 0f, 0f, 0f, local: true)
					{
						alpha = 0.01f,
						alphaFade = -0.002f,
						local = true
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 1453, 639, 176), 9999f, 1, 999999, new Vector2(0f, 768f), flicker: false, flipped: true, 0.9f, 0f, Color.Blue, 4f, 0f, 0f, 0f, local: true)
					{
						alpha = 0.01f,
						alphaFade = -0.002f,
						local = true
					});
				}
				break;
			case 'C':
				if (key == "jojaCeremony")
				{
					aboveMapSprites = new TemporaryAnimatedSpriteList();
					for (int num43 = 0; num43 < 16; num43++)
					{
						Vector2 vector5 = new Vector2(Game1.random.Next(Game1.viewport.Width - 128), Game1.viewport.Height + num43 * 64);
						aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(534, 1413, 11, 16), 99999f, 1, 99999, vector5, flicker: false, flipped: false, 0.99f, 0f, Color.DeepSkyBlue, 4f, 0f, 0f, 0f)
						{
							local = true,
							motion = new Vector2(0.25f, -1.5f),
							acceleration = new Vector2(0f, -0.001f),
							id = 79797 + num43
						});
						aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(545, 1413, 11, 34), 99999f, 1, 99999, vector5 + new Vector2(0f, 0f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							local = true,
							motion = new Vector2(0.25f, -1.5f),
							acceleration = new Vector2(0f, -0.001f),
							id = 79797 + num43
						});
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 1363, 114, 58), 99999f, 1, 99999, new Vector2(50f, 20f) * 64f, flicker: false, flipped: false, 0.1472f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(595, 1387, 14, 34), 200f, 3, 99999, new Vector2(48f, 20f) * 64f, flicker: false, flipped: false, 0.15720001f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						pingPong = true
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(595, 1387, 14, 34), 200f, 3, 99999, new Vector2(49f, 20f) * 64f, flicker: false, flipped: false, 0.15720001f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						pingPong = true
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(595, 1387, 14, 34), 210f, 3, 99999, new Vector2(62f, 20f) * 64f, flicker: false, flipped: false, 0.15720001f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						pingPong = true
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(595, 1387, 14, 34), 190f, 3, 99999, new Vector2(60f, 20f) * 64f, flicker: false, flipped: false, 0.15720001f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						pingPong = true
					});
				}
				break;
			case 'F':
				if (key == "joshFootball")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(405, 1916, 14, 8), 40f, 6, 9999, new Vector2(25f, 16f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						rotation = -(float)Math.PI / 4f,
						rotationChange = (float)Math.PI / 200f,
						motion = new Vector2(6f, -4f),
						acceleration = new Vector2(0f, 0.2f),
						xStopCoordinate = 1856,
						reachedStopCoordinate = catchFootball,
						layerDepth = 1f,
						id = 56232
					});
				}
				break;
			case 'o':
				if (key == "balloonBirds")
				{
					if (!ArgUtility.TryGetOptionalInt(args, 2, out var value6, out var error4, 0, "int positionOffset"))
					{
						LogCommandError(args, error4);
						break;
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(48f, value6 + 12) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-3f, 0f),
						delayBeforeAnimationStart = 1500
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(47f, value6 + 13) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-3f, 0f),
						delayBeforeAnimationStart = 1250
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(46f, value6 + 14) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-3f, 0f),
						delayBeforeAnimationStart = 1100
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(45f, value6 + 15) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-3f, 0f),
						delayBeforeAnimationStart = 1000
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(46f, value6 + 16) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-3f, 0f),
						delayBeforeAnimationStart = 1080
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(47f, value6 + 17) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-3f, 0f),
						delayBeforeAnimationStart = 1300
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(48f, value6 + 18) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-3f, 0f),
						delayBeforeAnimationStart = 1450
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(46f, value6 + 15) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(-4f, 0f),
						delayBeforeAnimationStart = 5450
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(48f, value6 + 10) * 64f, flicker: false, flipped: false, 0f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						motion = new Vector2(-2f, 0f),
						delayBeforeAnimationStart = 500
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(47f, value6 + 11) * 64f, flicker: false, flipped: false, 0f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						motion = new Vector2(-2f, 0f),
						delayBeforeAnimationStart = 250
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(46f, value6 + 12) * 64f, flicker: false, flipped: false, 0f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						motion = new Vector2(-2f, 0f),
						delayBeforeAnimationStart = 100
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(45f, value6 + 13) * 64f, flicker: false, flipped: false, 0f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						motion = new Vector2(-2f, 0f)
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(46f, value6 + 14) * 64f, flicker: false, flipped: false, 0f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						motion = new Vector2(-2f, 0f),
						delayBeforeAnimationStart = 80
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(47f, value6 + 15) * 64f, flicker: false, flipped: false, 0f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						motion = new Vector2(-2f, 0f),
						delayBeforeAnimationStart = 300
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(388, 1894, 24, 22), 100f, 6, 9999, new Vector2(48f, value6 + 16) * 64f, flicker: false, flipped: false, 0f, 0f, Color.White, 2f, 0f, 0f, 0f)
					{
						motion = new Vector2(-2f, 0f),
						delayBeforeAnimationStart = 450
					});
				}
				break;
			case 'e':
				if (key == "marcelloLand")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 1183, 84, 160), 10000f, 1, 99999, (new Vector2(25f, 19f) + eventPositionTileOffset) * 64f + new Vector2(-23f, 0f) * 4f, flicker: false, flipped: false, 2E-05f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(0f, 2f),
						yStopCoordinate = (41 + (int)eventPositionTileOffset.Y) * 64 - 640,
						reachedStopCoordinate = marcelloBalloonLand,
						attachedCharacter = getActorByName("Marcello"),
						id = 1
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(84, 1205, 38, 26), 10000f, 1, 99999, (new Vector2(25f, 19f) + eventPositionTileOffset) * 64f + new Vector2(0f, 134f) * 4f, flicker: false, flipped: false, (41f + eventPositionTileOffset.Y) * 64f / 10000f + 0.0001f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(0f, 2f),
						id = 2
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(24, 1343, 36, 19), 7000f, 1, 99999, (new Vector2(25f, 40f) + eventPositionTileOffset) * 64f, flicker: false, flipped: false, 1E-05f, 0f, Color.White, 0f, 0f, 0f, 0f)
					{
						scaleChange = 0.01f,
						id = 3
					});
				}
				break;
			case 'T':
				if (key == "maruTrapdoor")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(640, 1632, 16, 32), 150f, 4, 0, new Vector2(1f, 5f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(688, 1632, 16, 32), 99999f, 1, 0, new Vector2(1f, 5f) * 64f, flicker: false, flipped: false, 0.99f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						delayBeforeAnimationStart = 500
					});
				}
				break;
			case 'M':
				if (key == "abbyManyBats")
				{
					for (int num41 = 0; num41 < 100; num41++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(640, 1664, 16, 16), 80f, 4, 9999, new Vector2(23f, 9f) * 64f, flicker: false, flipped: false, 1f, 0.003f, Color.White, 4f, 0f, 0f, 0f)
						{
							xPeriodic = true,
							xPeriodicLoopTime = Game1.random.Next(1500, 2500),
							xPeriodicRange = Game1.random.Next(64, 192),
							motion = new Vector2(Game1.random.Next(-2, 3), Game1.random.Next(-8, -4)),
							delayBeforeAnimationStart = num41 * 30,
							startSound = ((num41 % 10 == 0 || Game1.random.NextDouble() < 0.1) ? "batScreech" : null)
						});
					}
					for (int num42 = 0; num42 < 100; num42++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(640, 1664, 16, 16), 80f, 4, 9999, new Vector2(23f, 9f) * 64f, flicker: false, flipped: false, 1f, 0.003f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(Game1.random.Next(-4, 5), Game1.random.Next(-8, -4)),
							delayBeforeAnimationStart = 10 + num42 * 30
						});
					}
				}
				break;
			}
			break;
		case 8:
			switch (key[4])
			{
			case 'y':
				if (key == "WillyWad")
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\Cursors2"),
						sourceRect = new Microsoft.Xna.Framework.Rectangle(192, 61, 32, 32),
						sourceRectStartingPos = new Vector2(192f, 61f),
						animationLength = 2,
						totalNumberOfLoops = 99999,
						interval = 400f,
						scale = 4f,
						position = new Vector2(50f, 23f) * 64f,
						layerDepth = 0.1536f,
						id = 996
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(51, new Vector2(3328f, 1728f), Color.White, 10, flipped: false, 80f, 999999));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(51, new Vector2(3264f, 1792f), Color.White, 10, flipped: false, 70f, 999999));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(51, new Vector2(3392f, 1792f), Color.White, 10, flipped: false, 85f, 999999));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(160, 368, 16, 32), 500f, 3, 99999, new Vector2(53f, 24f) * 64f, flicker: false, flipped: false, 0.1984f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(160, 368, 16, 32), 510f, 3, 99999, new Vector2(54f, 23f) * 64f, flicker: false, flipped: false, 0.1984f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			case 'J':
				if (key == "frogJump")
				{
					TemporaryAnimatedSprite temporarySpriteByID9 = location.getTemporarySpriteByID(777);
					temporarySpriteByID9.motion = new Vector2(-2f, 0f);
					temporarySpriteByID9.animationLength = 4;
					temporarySpriteByID9.interval = 150f;
				}
				break;
			case 'm':
				if (key == "golemDie")
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite(46, new Vector2(40f, 11f) * 64f, Color.DarkGray, 10));
					Utility.makeTemporarySpriteJuicier(new TemporaryAnimatedSprite(44, new Vector2(40f, 11f) * 64f, Color.LimeGreen, 10), location, 2);
					Texture2D texture14 = Game1.temporaryContent.Load<Texture2D>("Characters\\Monsters\\Wilderness Golem");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture14,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 24),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(0f, 0f),
						interval = 5000f,
						totalNumberOfLoops = 9999,
						position = new Vector2(40f, 11f) * 64f + new Vector2(2f, -8f) * 4f,
						scale = 4f,
						layerDepth = 0.01f,
						rotation = (float)Math.PI / 2f,
						motion = new Vector2(0f, 4f),
						yStopCoordinate = 832
					});
				}
				break;
			case 'o':
				if (key == "parrots1")
				{
					aboveMapSprites = new TemporaryAnimatedSpriteList
					{
						new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 165, 24, 22), 100f, 6, 9999, new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width, 256f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(-3f, 0f),
							yPeriodic = true,
							yPeriodicLoopTime = 2000f,
							yPeriodicRange = 32f,
							delayBeforeAnimationStart = 0,
							local = true
						},
						new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 165, 24, 22), 100f, 6, 9999, new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width, 192f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(-3f, 0f),
							yPeriodic = true,
							yPeriodicLoopTime = 2000f,
							yPeriodicRange = 32f,
							delayBeforeAnimationStart = 600,
							local = true
						},
						new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 165, 24, 22), 100f, 6, 9999, new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width, 320f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							motion = new Vector2(-3f, 0f),
							yPeriodic = true,
							yPeriodicLoopTime = 2000f,
							yPeriodicRange = 32f,
							delayBeforeAnimationStart = 1200,
							local = true
						}
					};
				}
				break;
			case 'e':
				if (key == "umbrella")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(324, 1843, 27, 23), 80f, 3, 9999, new Vector2(12f, 39f) * 64f + new Vector2(-20f, -104f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			case 'S':
				if (key == "leahShow")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(144, 688, 16, 32), 9999f, 1, 999, new Vector2(29f, 59f) * 64f - new Vector2(0f, 16f), flicker: false, flipped: false, 0.37750003f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(112, 656, 16, 64), 9999f, 1, 999, new Vector2(29f, 56f) * 64f, flicker: false, flipped: false, 0.3776f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(144, 688, 16, 32), 9999f, 1, 999, new Vector2(33f, 59f) * 64f - new Vector2(0f, 16f), flicker: false, flipped: false, 0.37750003f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(128, 688, 16, 32), 9999f, 1, 999, new Vector2(33f, 58f) * 64f, flicker: false, flipped: false, 0.3776f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(160, 656, 32, 64), 9999f, 1, 999, new Vector2(29f, 60f) * 64f, flicker: false, flipped: false, 0.4032f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(144, 688, 16, 32), 9999f, 1, 999, new Vector2(34f, 63f) * 64f, flicker: false, flipped: false, 0.4031f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(113, 592, 16, 64), 100f, 4, 99999, new Vector2(34f, 60f) * 64f, flicker: false, flipped: false, 0.4032f, 0f, Color.White, 4f, 0f, 0f, 0f));
					NPC nPC = new NPC(new AnimatedSprite(festivalContent, "Characters\\" + (farmer.IsMale ? "LeahExMale" : "LeahExFemale"), 0, 16, 32), new Vector2(46f, 57f) * 64f, 2, "LeahEx");
					nPC.AllowDynamicAppearance = false;
					actors.Add(nPC);
				}
				break;
			case 'T':
				if (key == "leahTree")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(744, 999999f, 1, 0, new Vector2(42f, 8f) * 64f, flicker: false, flipped: false));
				}
				break;
			}
			break;
		case 7:
			switch (key[1])
			{
			case 'u':
				if (key == "sunroom")
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
						sourceRect = new Microsoft.Xna.Framework.Rectangle(304, 486, 24, 26),
						sourceRectStartingPos = new Vector2(304f, 486f),
						animationLength = 1,
						totalNumberOfLoops = 997,
						interval = 99999f,
						scale = 4f,
						position = new Vector2(4f, 8f) * 64f + new Vector2(8f, -8f) * 4f,
						layerDepth = 0.0512f,
						id = 996
					});
					location.addCritter(new Butterfly(location, location.getRandomTile()).setStayInbounds(stayInbounds: true));
					while (Game1.random.NextBool())
					{
						location.addCritter(new Butterfly(location, location.getRandomTile()).setStayInbounds(stayInbounds: true));
					}
				}
				break;
			case 'a':
				if (key == "jasGift")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1231, 16, 16), 100f, 6, 1, new Vector2(22f, 16f) * 64f, flicker: false, flipped: false, 0.01f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 999,
						paused = true,
						holdLastFrame = true
					});
				}
				break;
			case 'e':
				if (key == "wedding")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(540, 1196, 98, 54), 99999f, 1, 99999, new Vector2(25f, 60f) * 64f + new Vector2(0f, -64f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(540, 1250, 98, 25), 99999f, 1, 99999, new Vector2(25f, 60f) * 64f + new Vector2(0f, 54f) * 4f + new Vector2(0f, -64f), flicker: false, flipped: false, 0f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(527, 1249, 12, 25), 99999f, 1, 99999, new Vector2(24f, 62f) * 64f, flicker: false, flipped: false, 0f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(527, 1249, 12, 25), 99999f, 1, 99999, new Vector2(32f, 62f) * 64f, flicker: false, flipped: false, 0f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(527, 1249, 12, 25), 99999f, 1, 99999, new Vector2(24f, 69f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(527, 1249, 12, 25), 99999f, 1, 99999, new Vector2(32f, 69f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			case 'i':
				if (key == "dickBag")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(528, 1435, 16, 16), 99999f, 1, 99999, new Vector2(48f, 7f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			case 'o':
			{
				if (!(key == "JoshMom"))
				{
					if (key == "joshDog")
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(324, 1916, 12, 20), 500f, 6, 9999, new Vector2(53f, 67f) * 64f + new Vector2(3f, 3f) * 4f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
						{
							id = 1
						});
					}
					break;
				}
				TemporaryAnimatedSprite temporaryAnimatedSprite5 = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(416, 1931, 58, 65), 750f, 2, 99999, new Vector2(Game1.viewport.Width / 2, Game1.viewport.Height), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
				{
					alpha = 0.6f,
					local = true,
					xPeriodic = true,
					xPeriodicLoopTime = 2000f,
					xPeriodicRange = 32f,
					motion = new Vector2(0f, -1.25f),
					initialPosition = new Vector2(Game1.viewport.Width / 2, Game1.viewport.Height)
				};
				location.temporarySprites.Add(temporaryAnimatedSprite5);
				for (int num40 = 0; num40 < 19; num40++)
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(516, 1916, 7, 10), 99999f, 1, 99999, new Vector2(64f, 32f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						alphaFade = 0.01f,
						local = true,
						motion = new Vector2(-1f, -1f),
						parentSprite = temporaryAnimatedSprite5,
						delayBeforeAnimationStart = (num40 + 1) * 1000
					});
				}
				break;
			}
			case 'r':
				if (key == "dropEgg")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(176, 800f, 1, 0, new Vector2(6f, 4f) * 64f + new Vector2(0f, 32f), flicker: false, flipped: false)
					{
						rotationChange = (float)Math.PI / 24f,
						motion = new Vector2(0f, -7f),
						acceleration = new Vector2(0f, 0.3f),
						endFunction = eggSmashEndFunction,
						layerDepth = 1f
					});
				}
				break;
			}
			break;
		case 9:
			switch (key[5])
			{
			case 'G':
				if (key == "sauceGood")
				{
					Utility.addSprinklesToLocation(location, OffsetTileX(64), OffsetTileY(16), 3, 1, 800, 200, Color.White);
				}
				break;
			case 'F':
				if (key == "sauceFire")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11),
						animationLength = 4,
						sourceRectStartingPos = new Vector2(276f, 1985f),
						interval = 100f,
						totalNumberOfLoops = 5,
						position = OffsetPosition(new Vector2(64f, 16f) * 64f + new Vector2(3f, -4f) * 4f),
						scale = 4f,
						layerDepth = 1f
					});
					aboveMapSprites = new TemporaryAnimatedSpriteList();
					for (int num39 = 0; num39 < 8; num39++)
					{
						aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), OffsetPosition(new Vector2(64f, 16f) * 64f) + new Vector2(Game1.random.Next(-16, 32), 0f), flipped: false, 0.002f, Color.Gray)
						{
							alpha = 0.75f,
							motion = new Vector2(1f, -1f) + new Vector2((float)(Game1.random.Next(100) - 50) / 100f, (float)(Game1.random.Next(100) - 50) / 100f),
							interval = 99999f,
							layerDepth = 0.0384f + (float)Game1.random.Next(100) / 10000f,
							scale = 3f,
							scaleChange = 0.01f,
							rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f,
							delayBeforeAnimationStart = num39 * 25
						});
					}
				}
				break;
			case 'B':
				if (!(key == "shakeBush"))
				{
					if (key == "movieBush")
					{
						location.temporarySprites.Add(new TemporaryAnimatedSprite
						{
							texture = Game1.temporaryContent.Load<Texture2D>("TileSheets\\bushes"),
							sourceRect = new Microsoft.Xna.Framework.Rectangle(65, 58, 30, 35),
							sourceRectStartingPos = new Vector2(65f, 58f),
							animationLength = 1,
							totalNumberOfLoops = 999,
							interval = 999f,
							scale = 4f,
							position = new Vector2(4f, 1f) * 64f + new Vector2(33f, 13f) * 4f,
							layerDepth = 0.99f,
							id = 777
						});
					}
				}
				else
				{
					location.getTemporarySpriteByID(777).shakeIntensity = 1f;
				}
				break;
			case 'T':
				if (key == "shakeTent")
				{
					location.getTemporarySpriteByID(999).shakeIntensity = 1f;
				}
				break;
			case 'S':
			{
				if (!(key == "EmilySign"))
				{
					break;
				}
				aboveMapSprites = new TemporaryAnimatedSpriteList();
				for (int num35 = 0; num35 < 10; num35++)
				{
					int num36 = 0;
					int num37 = Game1.random.Next(Game1.graphics.GraphicsDevice.Viewport.Height - 128);
					for (int num38 = Game1.graphics.GraphicsDevice.Viewport.Width; num38 >= -64; num38 -= 48)
					{
						aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(597, 1888, 16, 16), 99999f, 1, 99999, new Vector2(num38, num37), flicker: false, flipped: false, 1f, 0.02f, Color.White, 4f, 0f, 0f, 0f)
						{
							delayBeforeAnimationStart = num35 * 600 + num36 * 25,
							startSound = ((num36 == 0) ? "dwoop" : null),
							local = true
						});
						num36++;
					}
				}
				break;
			}
			case 't':
				if (key == "joshSteak")
				{
					location.temporarySprites.Clear();
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(324, 1936, 12, 20), 80f, 4, 99999, new Vector2(53f, 67f) * 64f + new Vector2(3f, 3f) * 4f, flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						id = 1
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(497, 1918, 11, 11), 999f, 1, 9999, new Vector2(50f, 68f) * 64f + new Vector2(32f, -8f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			case 'a':
				if (key == "samSkate1")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 0, 0, 0), 9999f, 1, 999, new Vector2(12f, 90f) * 64f, flicker: false, flipped: false, 1E-05f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(4f, 0f),
						acceleration = new Vector2(-0.008f, 0f),
						xStopCoordinate = 1344,
						reachedStopCoordinate = samPreOllie,
						attachedCharacter = getActorByName("Sam"),
						id = 92473
					});
				}
				break;
			case 'C':
				if (key == "pennyCook")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(256, 1856, 64, 128), new Vector2(10f, 6f) * 64f, flipped: false, 0f, Color.White)
					{
						layerDepth = 1f,
						animationLength = 6,
						interval = 75f,
						motion = new Vector2(0f, -0.5f)
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(256, 1856, 64, 128), new Vector2(10f, 6f) * 64f + new Vector2(16f, 0f), flipped: false, 0f, Color.White)
					{
						layerDepth = 0.1f,
						animationLength = 6,
						interval = 75f,
						motion = new Vector2(0f, -0.5f),
						delayBeforeAnimationStart = 500
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(256, 1856, 64, 128), new Vector2(10f, 6f) * 64f + new Vector2(-16f, 0f), flipped: false, 0f, Color.White)
					{
						layerDepth = 1f,
						animationLength = 6,
						interval = 75f,
						motion = new Vector2(0f, -0.5f),
						delayBeforeAnimationStart = 750
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(256, 1856, 64, 128), new Vector2(10f, 6f) * 64f, flipped: false, 0f, Color.White)
					{
						layerDepth = 0.1f,
						animationLength = 6,
						interval = 75f,
						motion = new Vector2(0f, -0.5f),
						delayBeforeAnimationStart = 1000
					});
				}
				break;
			case 'M':
				if (key == "pennyMess")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(739, 999999f, 1, 0, new Vector2(10f, 5f) * 64f, flicker: false, flipped: false));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(740, 999999f, 1, 0, new Vector2(15f, 5f) * 64f, flicker: false, flipped: false));
					location.TemporarySprites.Add(new TemporaryAnimatedSprite(741, 999999f, 1, 0, new Vector2(16f, 6f) * 64f, flicker: false, flipped: false));
				}
				break;
			case 'u':
				if (key == "abbyOuija")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 960, 128, 128), 60f, 4, 0, new Vector2(6f, 9f) * 64f, flicker: false, flipped: false, 1f, 0f, Color.White, 1f, 0f, 0f, 0f));
				}
				break;
			}
			break;
		case 17:
			switch (key[0])
			{
			case 'l':
				if (key == "leahPaintingSetup")
				{
					Texture2D texture13 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture13,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(368, 393, 15, 28),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(368f, 393f),
						interval = 5000f,
						totalNumberOfLoops = 99999,
						position = new Vector2(72f, 38f) * 64f + new Vector2(3f, -13f) * 4f,
						scale = 4f,
						layerDepth = 0.1f,
						id = 999
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture13,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(368, 393, 15, 28),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(368f, 393f),
						interval = 5000f,
						totalNumberOfLoops = 99999,
						position = new Vector2(74f, 40f) * 64f + new Vector2(3f, -17f) * 4f,
						scale = 4f,
						layerDepth = 0.1f,
						id = 888
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture13,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(369, 424, 11, 15),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(369f, 424f),
						interval = 9999f,
						totalNumberOfLoops = 99999,
						position = new Vector2(75f, 40f) * 64f + new Vector2(-2f, -11f) * 4f,
						scale = 4f,
						layerDepth = 0.01f,
						id = 444
					});
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = Game1.mouseCursors,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(96, 1822, 32, 34),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(96f, 1822f),
						interval = 5000f,
						totalNumberOfLoops = 99999,
						position = new Vector2(79f, 36f) * 64f,
						scale = 4f,
						layerDepth = 0.1f
					});
				}
				break;
			case 's':
				if (key == "springOnionRemove")
				{
					location.removeTemporarySpritesWithID(777);
				}
				break;
			case 'E':
				if (key == "EmilyBoomBoxStart")
				{
					location.getTemporarySpriteByID(999).pulse = true;
					location.getTemporarySpriteByID(999).pulseTime = 420f;
				}
				break;
			case 'd':
				if (key == "doneWithSlideShow")
				{
					(location as Summit).isShowingEndSlideshow = false;
				}
				break;
			case 'm':
				if (key == "maruElectrocution")
				{
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(432, 1664, 16, 32), 40f, 1, 20, new Vector2(7f, 5f) * 64f - new Vector2(-4f, 8f), flicker: true, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f));
				}
				break;
			}
			break;
		case 5:
			switch (key[0])
			{
			case 's':
				if (key == "samTV")
				{
					Texture2D texture12 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
					location.TemporarySprites.Add(new TemporaryAnimatedSprite
					{
						texture = texture12,
						sourceRect = new Microsoft.Xna.Framework.Rectangle(368, 350, 25, 29),
						animationLength = 1,
						sourceRectStartingPos = new Vector2(368f, 350f),
						interval = 5000f,
						totalNumberOfLoops = 99999,
						position = new Vector2(52f, 24f) * 64f + new Vector2(4f, -12f) * 4f,
						scale = 4f,
						layerDepth = 0.9f
					});
				}
				break;
			case 'h':
				if (key == "heart")
				{
					if (!ArgUtility.TryGetVector2(args, 2, out var value4, out var error2, integerOnly: true, "Vector2 tile"))
					{
						LogCommandError(args, error2);
						break;
					}
					location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(211, 428, 7, 6), 2000f, 1, 0, OffsetPosition(value4) * 64f + new Vector2(-16f, -16f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						motion = new Vector2(0f, -0.5f),
						alphaFade = 0.01f
					});
				}
				break;
			case 'r':
				if (key == "robot")
				{
					TemporaryAnimatedSprite temporaryAnimatedSprite4 = new TemporaryAnimatedSprite(getActorByName("robot").Sprite.textureName.Value, new Microsoft.Xna.Framework.Rectangle(35, 42, 35, 42), 50f, 1, 9999, new Vector2(13f, 27f) * 64f - new Vector2(0f, 32f), flicker: false, flipped: false, 0.98f, 0f, Color.White, 4f, 0f, 0f, 0f)
					{
						acceleration = new Vector2(0f, -0.01f),
						accelerationChange = new Vector2(0f, -0.0001f)
					};
					location.temporarySprites.Add(temporaryAnimatedSprite4);
					for (int num34 = 0; num34 < 420; num34++)
					{
						location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(Game1.random.Next(4) * 64, 320, 64, 64), new Vector2(Game1.random.Next(96), 136f), flipped: false, 0.01f, Color.White * 0.75f)
						{
							layerDepth = 1f,
							delayBeforeAnimationStart = num34 * 10,
							animationLength = 1,
							currentNumberOfLoops = 0,
							interval = 9999f,
							motion = new Vector2(Game1.random.Next(-100, 100) / (num34 + 20), 0.25f + (float)num34 / 100f),
							parentSprite = temporaryAnimatedSprite4
						});
					}
				}
				break;
			}
			break;
		case 20:
			if (key == "BoatParrotSquawkStop")
			{
				TemporaryAnimatedSprite temporaryAnimatedSprite2 = aboveMapSprites[0];
				temporaryAnimatedSprite2.sourceRect.X = 0;
				temporaryAnimatedSprite2.sourceRectStartingPos.X = 0f;
			}
			break;
		case 23:
			if (key == "leahStopHoldingPainting")
			{
				location.getTemporarySpriteByID(999).sourceRect.X -= 15;
				location.getTemporarySpriteByID(999).sourceRectStartingPos.X -= 15f;
				location.removeTemporarySpritesWithIDLocal(777);
				Game1.playSound("thudStep");
			}
			break;
		case 6:
			if (key == "qiCave")
			{
				Texture2D texture4 = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1");
				location.TemporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = texture4,
					sourceRect = new Microsoft.Xna.Framework.Rectangle(415, 216, 96, 89),
					animationLength = 1,
					sourceRectStartingPos = new Vector2(415f, 216f),
					interval = 999999f,
					totalNumberOfLoops = 99999,
					position = new Vector2(2f, 2f) * 64f + new Vector2(112f, 25f) * 4f,
					scale = 4f,
					layerDepth = 1E-07f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = texture4,
					sourceRect = new Microsoft.Xna.Framework.Rectangle(370, 272, 107, 64),
					animationLength = 1,
					sourceRectStartingPos = new Vector2(370f, 216f),
					interval = 999999f,
					totalNumberOfLoops = 99999,
					position = new Vector2(2f, 2f) * 64f + new Vector2(67f, 81f) * 4f,
					scale = 4f,
					layerDepth = 1.1E-07f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = Game1.objectSpriteSheet,
					sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 803, 16, 16),
					sourceRectStartingPos = new Vector2(Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 803, 16, 16).X, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 803, 16, 16).Y),
					animationLength = 1,
					interval = 999999f,
					id = 803,
					totalNumberOfLoops = 99999,
					position = new Vector2(13f, 7f) * 64f + new Vector2(1f, 9f) * 4f,
					scale = 4f,
					layerDepth = 2.1E-06f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = texture4,
					sourceRect = new Microsoft.Xna.Framework.Rectangle(432, 171, 16, 30),
					animationLength = 5,
					sourceRectStartingPos = new Vector2(432f, 171f),
					pingPong = true,
					interval = 100f,
					totalNumberOfLoops = 99999,
					id = 11,
					position = new Vector2(8f, 6f) * 64f,
					scale = 4f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = texture4,
					sourceRect = new Microsoft.Xna.Framework.Rectangle(432, 171, 16, 30),
					animationLength = 5,
					sourceRectStartingPos = new Vector2(432f, 171f),
					pingPong = true,
					interval = 90f,
					totalNumberOfLoops = 99999,
					id = 11,
					position = new Vector2(5f, 7f) * 64f,
					scale = 4f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = texture4,
					sourceRect = new Microsoft.Xna.Framework.Rectangle(432, 171, 16, 30),
					animationLength = 5,
					sourceRectStartingPos = new Vector2(432f, 171f),
					pingPong = true,
					interval = 120f,
					totalNumberOfLoops = 99999,
					id = 11,
					position = new Vector2(7f, 10f) * 64f,
					scale = 4f,
					layerDepth = 1f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = texture4,
					sourceRect = new Microsoft.Xna.Framework.Rectangle(432, 171, 16, 30),
					animationLength = 5,
					sourceRectStartingPos = new Vector2(432f, 171f),
					pingPong = true,
					interval = 80f,
					totalNumberOfLoops = 99999,
					id = 11,
					position = new Vector2(15f, 7f) * 64f,
					scale = 4f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = texture4,
					sourceRect = new Microsoft.Xna.Framework.Rectangle(432, 171, 16, 30),
					animationLength = 5,
					sourceRectStartingPos = new Vector2(432f, 171f),
					pingPong = true,
					interval = 100f,
					totalNumberOfLoops = 99999,
					id = 11,
					position = new Vector2(12f, 11f) * 64f,
					scale = 4f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = texture4,
					sourceRect = new Microsoft.Xna.Framework.Rectangle(432, 171, 16, 30),
					animationLength = 5,
					sourceRectStartingPos = new Vector2(432f, 171f),
					pingPong = true,
					interval = 105f,
					totalNumberOfLoops = 99999,
					id = 11,
					position = new Vector2(16f, 10f) * 64f,
					scale = 4f
				});
				location.TemporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = texture4,
					sourceRect = new Microsoft.Xna.Framework.Rectangle(432, 171, 16, 30),
					animationLength = 5,
					sourceRectStartingPos = new Vector2(432f, 171f),
					pingPong = true,
					interval = 85f,
					totalNumberOfLoops = 99999,
					id = 11,
					position = new Vector2(3f, 9f) * 64f,
					scale = 4f
				});
			}
			break;
		case 3:
			if (key == "wed")
			{
				aboveMapSprites = new TemporaryAnimatedSpriteList();
				Game1.flashAlpha = 1f;
				for (int i = 0; i < 150; i++)
				{
					Vector2 position = new Vector2(Game1.random.Next(Game1.viewport.Width - 128), Game1.random.Next(Game1.viewport.Height));
					int num = Game1.random.Next(2, 5);
					aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(424, 1266, 8, 8), 60f + (float)Game1.random.Next(-10, 10), 7, 999999, position, flicker: false, flipped: false, 0.99f, 0f, Color.White, num, 0f, 0f, 0f)
					{
						local = true,
						motion = new Vector2(0.1625f, -0.25f) * num
					});
				}
				Game1.changeMusicTrack("wedding", track_interruptable: false, MusicContext.Event);
				Game1.musicPlayerVolume = 0f;
			}
			break;
		case 4:
		case 21:
		case 22:
			break;
		}
	}

	private Microsoft.Xna.Framework.Rectangle skipBounds()
	{
		int num = 4;
		int num2 = 22 * num;
		Microsoft.Xna.Framework.Rectangle bounds = new Microsoft.Xna.Framework.Rectangle(Game1.viewport.Width - num2 - 8, Game1.viewport.Height - 64, num2, 15 * num);
		Utility.makeSafe(ref bounds);
		return bounds;
	}

	public void receiveMouseClick(int x, int y)
	{
		if (!Game1.options.SnappyMenus && !skipped && skippable && skipBounds().Contains(x, y))
		{
			skipped = true;
			skipEvent();
			Game1.freezeControls = false;
		}
		popBalloons(x, y);
	}

	public void skipEvent()
	{
		if (playerControlSequence)
		{
			EndPlayerControlSequence();
		}
		Game1.playSound("drumkit6");
		actorPositionsAfterMove.Clear();
		foreach (NPC actor in actors)
		{
			bool ignoreStopAnimation = actor.Sprite.ignoreStopAnimation;
			actor.Sprite.ignoreStopAnimation = true;
			actor.Halt();
			actor.Sprite.ignoreStopAnimation = ignoreStopAnimation;
			resetDialogueIfNecessary(actor);
		}
		farmer.Halt();
		farmer.ignoreCollisions = false;
		Game1.exitActiveMenu();
		Game1.fadeClear();
		Game1.dialogueUp = false;
		Game1.dialogueTyping = false;
		Game1.pauseTime = 0f;
		string[] array = actionsOnSkip;
		if (array != null && array.Length != 0)
		{
			string[] array2 = actionsOnSkip;
			foreach (string text in array2)
			{
				if (!TriggerActionManager.TryRunAction(text, out var error, out var exception))
				{
					Game1.log.Error($"Event '{id}' failed applying post-skip action '{text}': {error}.", exception);
				}
			}
			Game1.log.Verbose($"Event '{id}' applied post-skip actions [{string.Join(", ", actionsOnSkip)}].");
		}
		switch (id)
		{
		case "33":
			Game1.player.craftingRecipes.TryAdd("Drum Block", 0);
			Game1.player.craftingRecipes.TryAdd("Flute Block", 0);
			endBehaviors();
			break;
		case "897405":
		case "1590166":
			if (!gotPet)
			{
				string name = ((!Game1.player.IsMale) ? Game1.content.LoadString((Game1.player.whichPetType == "Dog") ? "Strings\\StringsFromCSFiles:Event.cs.1797" : "Strings\\StringsFromCSFiles:Event.cs.1796") : Game1.content.LoadString(Game1.player.catPerson ? "Strings\\StringsFromCSFiles:Event.cs.1794" : "Strings\\StringsFromCSFiles:Event.cs.1795"));
				namePet(name);
			}
			endBehaviors();
			break;
		case "980559":
			if (Game1.player.GetSkillLevel(1) < 1)
			{
				Game1.player.setSkillLevel("Fishing", 1);
			}
			if (!Game1.player.Items.ContainsId("(T)TrainingRod"))
			{
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(T)TrainingRod"));
			}
			endBehaviors();
			break;
		case "-157039427":
			endBehaviors(new string[2] { "End", "islandDepart" }, Game1.currentLocation);
			break;
		case "-888999":
		{
			Object obj = ItemRegistry.Create<Object>("(O)864");
			obj.specialItem = true;
			obj.questItem.Value = true;
			Game1.player.addItemByMenuIfNecessary(obj);
			Game1.player.addQuest("130");
			endBehaviors();
			break;
		}
		case "-666777":
			if (!Game1.netWorldState.Value.ActivatedGoldenParrot)
			{
				Game1.player.team.RequestLimitedNutDrops("Birdie", null, 0, 0, 5, 5);
			}
			if (!Game1.MasterPlayer.hasOrWillReceiveMail("gotBirdieReward"))
			{
				Game1.addMailForTomorrow("gotBirdieReward", noLetter: true, sendToEveryone: true);
			}
			Game1.player.craftingRecipes.TryAdd("Fairy Dust", 0);
			endBehaviors();
			break;
		case "6497428":
			endBehaviors(new string[2] { "End", "Leo" }, Game1.currentLocation);
			break;
		case "-78765":
			endBehaviors(new string[2] { "End", "tunnelDepart" }, Game1.currentLocation);
			break;
		case "690006":
			if (!Game1.player.Items.ContainsId("(O)680"))
			{
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(O)680"));
			}
			endBehaviors();
			break;
		case "191393":
			if (!Game1.player.Items.ContainsId("(BC)116"))
			{
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(BC)116"));
			}
			endBehaviors(new string[4] { "End", "position", "52", "20" }, Game1.currentLocation);
			break;
		case "2123343":
			endBehaviors(new string[2] { "End", "newDay" }, Game1.currentLocation);
			break;
		case "404798":
			if (!Game1.player.Items.ContainsId("(T)Pan"))
			{
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(T)Pan"));
			}
			endBehaviors();
			break;
		case "26":
			Game1.player.craftingRecipes.TryAdd("Wild Bait", 0);
			endBehaviors();
			break;
		case "611173":
			if (!Game1.player.activeDialogueEvents.ContainsKey("pamHouseUpgradeAnonymous"))
			{
				Game1.player.activeDialogueEvents.TryAdd("pamHouseUpgrade", 4);
			}
			endBehaviors();
			break;
		case "3091462":
			if (!Game1.player.Items.ContainsId("(F)1802"))
			{
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(F)1802"));
			}
			endBehaviors();
			break;
		case "3918602":
			if (!Game1.player.Items.ContainsId("(F)1309"))
			{
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(F)1309"));
			}
			endBehaviors();
			break;
		case "19":
			Game1.player.cookingRecipes.TryAdd("Cookies", 0);
			endBehaviors();
			break;
		case "992553":
			Game1.player.craftingRecipes.TryAdd("Furnace", 0);
			Game1.player.addQuest("11");
			endBehaviors();
			break;
		case "900553":
			Game1.player.craftingRecipes.TryAdd("Garden Pot", 0);
			if (!Game1.player.Items.ContainsId("(BC)62"))
			{
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(BC)62"));
			}
			endBehaviors();
			break;
		case "980558":
			Game1.player.craftingRecipes.TryAdd("Mini-Jukebox", 0);
			if (!Game1.player.Items.ContainsId("(BC)209"))
			{
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(BC)209"));
			}
			endBehaviors();
			break;
		case "60367":
			endBehaviors(new string[2] { "End", "beginGame" }, Game1.currentLocation);
			break;
		case "739330":
			if (!Game1.player.Items.ContainsId("(T)BambooPole"))
			{
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(T)BambooPole"));
			}
			endBehaviors(new string[4] { "End", "position", "43", "36" }, Game1.currentLocation);
			break;
		case "112":
			endBehaviors();
			Game1.player.mailReceived.Add("canReadJunimoText");
			break;
		case "558292":
			Game1.player.eventsSeen.Remove("2146991");
			endBehaviors(new string[2] { "End", "bed" }, Game1.currentLocation);
			break;
		case "100162":
			if (!Game1.player.Items.ContainsId("(W)0"))
			{
				Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(W)0"));
			}
			Game1.player.Position = new Vector2(-9999f, -99999f);
			endBehaviors();
			break;
		default:
			endBehaviors();
			break;
		}
	}

	public void receiveActionPress(int xTile, int yTile)
	{
		if (xTile != playerControlTargetTile.X || yTile != playerControlTargetTile.Y)
		{
			return;
		}
		string text = playerControlSequenceID;
		if (!(text == "haleyBeach"))
		{
			if (text == "haleyBeach2")
			{
				EndPlayerControlSequence();
				CurrentCommand++;
			}
		}
		else
		{
			props.Clear();
			Game1.playSound("coin");
			playerControlTargetTile = new Point(35, 11);
			playerControlSequenceID = "haleyBeach2";
		}
	}

	public void startSecretSantaEvent()
	{
		playerControlSequence = false;
		playerControlSequenceID = null;
		if (!TryGetFestivalDataForYear("secretSanta", out var data))
		{
			Game1.log.Error("Festival " + id + " doesn't have the required 'secretSanta' data field.");
		}
		eventCommands = ParseCommands(data);
		doingSecretSanta = true;
		setUpSecretSantaCommands();
		currentCommand = 0;
	}

	public void festivalUpdate(GameTime time)
	{
		Game1.player.team.festivalScoreStatus.UpdateState(Game1.player.festivalScore.ToString() ?? "");
		if (festivalTimer > 0)
		{
			int num = festivalTimer;
			festivalTimer -= time.ElapsedGameTime.Milliseconds;
			if (playerControlSequenceID == "iceFishing")
			{
				if (!Game1.player.UsingTool)
				{
					Game1.player.forceCanMove();
				}
				if (num % 500 < festivalTimer % 500)
				{
					NPC actorByName = getActorByName("Pam");
					actorByName.Sprite.sourceRect.Offset(actorByName.Sprite.SourceRect.Width, 0);
					if (actorByName.Sprite.sourceRect.X >= actorByName.Sprite.Texture.Width)
					{
						actorByName.Sprite.sourceRect.Offset(-actorByName.Sprite.Texture.Width, 0);
					}
					actorByName = getActorByName("Elliott");
					actorByName.Sprite.sourceRect.Offset(actorByName.Sprite.SourceRect.Width, 0);
					if (actorByName.Sprite.sourceRect.X >= actorByName.Sprite.Texture.Width)
					{
						actorByName.Sprite.sourceRect.Offset(-actorByName.Sprite.Texture.Width, 0);
					}
					actorByName = getActorByName("Willy");
					actorByName.Sprite.sourceRect.Offset(actorByName.Sprite.SourceRect.Width, 0);
					if (actorByName.Sprite.sourceRect.X >= actorByName.Sprite.Texture.Width)
					{
						actorByName.Sprite.sourceRect.Offset(-actorByName.Sprite.Texture.Width, 0);
					}
				}
				if (num % 29900 < festivalTimer % 29900)
				{
					getActorByName("Willy").shake(500);
					Game1.playSound("dwop");
					temporaryLocation.temporarySprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(112, 432, 16, 16), getActorByName("Willy").Position + new Vector2(0f, -96f), flipped: false, 0.015f, Color.White)
					{
						layerDepth = 1f,
						scale = 4f,
						interval = 9999f,
						motion = new Vector2(0f, -1f)
					});
				}
				if (num % 45900 < festivalTimer % 45900)
				{
					getActorByName("Pam").shake(500);
					Game1.playSound("dwop");
					temporaryLocation.temporarySprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(112, 432, 16, 16), getActorByName("Pam").Position + new Vector2(0f, -96f), flipped: false, 0.015f, Color.White)
					{
						layerDepth = 1f,
						scale = 4f,
						interval = 9999f,
						motion = new Vector2(0f, -1f)
					});
				}
				if (num % 59900 < festivalTimer % 59900)
				{
					getActorByName("Elliott").shake(500);
					Game1.playSound("dwop");
					temporaryLocation.temporarySprites.Add(new TemporaryAnimatedSprite("Maps\\Festivals", new Microsoft.Xna.Framework.Rectangle(112, 432, 16, 16), getActorByName("Elliott").Position + new Vector2(0f, -96f), flipped: false, 0.015f, Color.White)
					{
						layerDepth = 1f,
						scale = 4f,
						interval = 9999f,
						motion = new Vector2(0f, -1f)
					});
				}
			}
			if (festivalTimer <= 0)
			{
				Game1.player.Halt();
				string text = playerControlSequenceID;
				if (!(text == "eggHunt"))
				{
					if (text == "iceFishing")
					{
						EndPlayerControlSequence();
						if (!TryGetFestivalDataForYear("afterIceFishing", out var data))
						{
							Game1.log.Error("Festival " + id + " doesn't have the required 'afterIceFishing' data field.");
						}
						eventCommands = ParseCommands(data);
						currentCommand = 0;
						if (Game1.activeClickableMenu != null)
						{
							Game1.activeClickableMenu.emergencyShutDown();
						}
						Game1.activeClickableMenu = null;
						if (Game1.player.UsingTool && Game1.player.CurrentTool is FishingRod fishingRod)
						{
							fishingRod.doneFishing(Game1.player);
						}
						Game1.screenOverlayTempSprites.Clear();
						Game1.player.forceCanMove();
					}
				}
				else
				{
					EndPlayerControlSequence();
					if (!TryGetFestivalDataForYear("afterEggHunt", out var data2))
					{
						Game1.log.Error("Festival " + id + " doesn't have the required 'afterEggHunt' data field.");
					}
					eventCommands = ParseCommands(data2);
					currentCommand = 0;
				}
			}
		}
		if (startSecretSantaAfterDialogue && !Game1.dialogueUp)
		{
			Game1.globalFadeToBlack(startSecretSantaEvent, 0.01f);
			startSecretSantaAfterDialogue = false;
		}
		Game1.player.festivalScore = Math.Min(Game1.player.festivalScore, 9999);
	}

	private void setUpSecretSantaCommands()
	{
		Point tilePoint;
		try
		{
			tilePoint = getActorByName(mySecretSanta.Name).TilePoint;
		}
		catch
		{
			mySecretSanta = getActorByName("Lewis");
			tilePoint = getActorByName(mySecretSanta.Name).TilePoint;
		}
		string text = mySecretSanta.Dialogue?.GetValueOrDefault("WinterStar_GiveGift_Before");
		string text2 = mySecretSanta.Dialogue?.GetValueOrDefault("WinterStar_GiveGift_After");
		if (Game1.player.spouse == mySecretSanta.Name)
		{
			text = mySecretSanta.Dialogue?.GetValueOrDefault("WinterStar_GiveGift_Before_" + (Game1.player.isRoommate(mySecretSanta.Name) ? "Roommate" : "Spouse")) ?? text;
			text2 = mySecretSanta.Dialogue?.GetValueOrDefault("WinterStar_GiveGift_After_" + (Game1.player.isRoommate(mySecretSanta.Name) ? "Roommate" : "Spouse")) ?? text2;
		}
		if (mySecretSanta.Age == 2)
		{
			if (text == null)
			{
				text = Game1.LoadStringByGender(mySecretSanta.Gender, "Strings\\StringsFromCSFiles:Event.cs.1497");
			}
			if (text2 == null)
			{
				text2 = Game1.LoadStringByGender(mySecretSanta.Gender, "Strings\\StringsFromCSFiles:Event.cs.1498");
			}
		}
		else if (mySecretSanta.Manners == 2)
		{
			if (text == null)
			{
				text = Game1.LoadStringByGender(mySecretSanta.Gender, "Strings\\StringsFromCSFiles:Event.cs.1501");
			}
			if (text2 == null)
			{
				text2 = Game1.LoadStringByGender(mySecretSanta.Gender, "Strings\\StringsFromCSFiles:Event.cs.1504");
			}
		}
		else
		{
			if (text == null)
			{
				text = Game1.LoadStringByGender(mySecretSanta.Gender, "Strings\\StringsFromCSFiles:Event.cs.1499");
			}
			if (text2 == null)
			{
				text2 = Game1.LoadStringByGender(mySecretSanta.Gender, "Strings\\StringsFromCSFiles:Event.cs.1500");
			}
		}
		for (int i = 0; i < eventCommands.Length; i++)
		{
			eventCommands[i] = eventCommands[i].Replace("secretSanta", mySecretSanta.Name);
			eventCommands[i] = eventCommands[i].Replace("warpX", tilePoint.X.ToString() ?? "");
			eventCommands[i] = eventCommands[i].Replace("warpY", tilePoint.Y.ToString() ?? "");
			eventCommands[i] = eventCommands[i].Replace("dialogue1", text);
			eventCommands[i] = eventCommands[i].Replace("dialogue2", text2);
		}
	}

	public void drawFarmers(SpriteBatch b)
	{
		foreach (Farmer farmerActor in farmerActors)
		{
			farmerActor.draw(b);
		}
	}

	public virtual bool ShouldHideCharacter(NPC n)
	{
		if (n is Child && doingSecretSanta)
		{
			return true;
		}
		return false;
	}

	public void draw(SpriteBatch b)
	{
		if (currentCustomEventScript != null)
		{
			currentCustomEventScript.draw(b);
			return;
		}
		foreach (NPC actor in actors)
		{
			if (!ShouldHideCharacter(actor))
			{
				actor.Name.Equals("Marcello");
				if (actor.ySourceRectOffset == 0)
				{
					actor.draw(b);
				}
				else
				{
					actor.draw(b, actor.ySourceRectOffset);
				}
			}
		}
		foreach (Object prop in props)
		{
			prop.drawAsProp(b);
		}
		foreach (Prop festivalProp in festivalProps)
		{
			festivalProp.draw(b);
		}
		if (isSpecificFestival("fall16"))
		{
			Vector2 vector = Game1.GlobalToLocal(Game1.viewport, new Vector2(37f, 56f) * 64f);
			vector.X += 4f;
			int num = (int)vector.X + 168;
			vector.Y += 8f;
			for (int i = 0; i < Game1.player.team.grangeDisplay.Count; i++)
			{
				if (Game1.player.team.grangeDisplay[i] != null)
				{
					vector.Y += 42f;
					vector.X += 4f;
					b.Draw(Game1.shadowTexture, vector, Game1.shadowTexture.Bounds, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.0001f);
					vector.Y -= 42f;
					vector.X -= 4f;
					Game1.player.team.grangeDisplay[i].drawInMenu(b, vector, 1f, 1f, (float)i / 1000f + 0.001f, StackDrawType.Hide);
				}
				vector.X += 60f;
				if (vector.X >= (float)num)
				{
					vector.X = num - 168;
					vector.Y += 64f;
				}
			}
		}
		if (drawTool)
		{
			Game1.drawTool(farmer);
		}
	}

	public void drawUnderWater(SpriteBatch b)
	{
		if (underwaterSprites == null)
		{
			return;
		}
		foreach (TemporaryAnimatedSprite underwaterSprite in underwaterSprites)
		{
			underwaterSprite.draw(b);
		}
	}

	public void drawAfterMap(SpriteBatch b)
	{
		if (aboveMapSprites != null)
		{
			foreach (TemporaryAnimatedSprite aboveMapSprite in aboveMapSprites)
			{
				aboveMapSprite.draw(b);
			}
		}
		if (!Game1.game1.takingMapScreenshot && playerControlSequenceID != null)
		{
			switch (playerControlSequenceID)
			{
			case "eggHunt":
				b.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(32, 32, 224, 160), Color.Black * 0.5f);
				Game1.drawWithBorder(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1514", festivalTimer / 1000), Color.Black, Color.Yellow, new Vector2(64f, 64f), 0f, 1f, 1f, tiny: false);
				Game1.drawWithBorder(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1515", Game1.player.festivalScore), Color.Black, Color.Pink, new Vector2(64f, 128f), 0f, 1f, 1f, tiny: false);
				if (Game1.IsMultiplayer)
				{
					Game1.player.team.festivalScoreStatus.Draw(b, new Vector2(32f, Game1.viewport.Height - 32), 4f, 0.99f, PlayerStatusList.HorizontalAlignment.Left, PlayerStatusList.VerticalAlignment.Bottom);
				}
				break;
			case "fair":
				b.End();
				Game1.PushUIMode();
				b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				b.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(16, 16, 128 + ((Game1.player.festivalScore > 999) ? 16 : 0), 64), Color.Black * 0.75f);
				b.Draw(Game1.mouseCursors, new Vector2(32f, 32f), new Microsoft.Xna.Framework.Rectangle(338, 400, 8, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				Game1.drawWithBorder(Game1.player.festivalScore.ToString() ?? "", Color.Black, Color.White, new Vector2(72f, 21 + ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en) ? 8 : (LocalizedContentManager.CurrentLanguageLatin ? 16 : 8))), 0f, 1f, 1f, tiny: false);
				if (Game1.activeClickableMenu == null)
				{
					Game1.dayTimeMoneyBox.drawMoneyBox(b, Game1.dayTimeMoneyBox.xPositionOnScreen, 4);
				}
				b.End();
				Game1.PopUIMode();
				b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				if (Game1.IsMultiplayer)
				{
					Game1.player.team.festivalScoreStatus.Draw(b, new Vector2(32f, Game1.viewport.Height - 32), 4f, 0.99f, PlayerStatusList.HorizontalAlignment.Left, PlayerStatusList.VerticalAlignment.Bottom);
				}
				break;
			case "iceFishing":
				b.End();
				b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				b.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(16, 16, 128 + ((Game1.player.festivalScore > 999) ? 16 : 0), 128), Color.Black * 0.75f);
				b.Draw(festivalTexture, new Vector2(32f, 16f), new Microsoft.Xna.Framework.Rectangle(112, 432, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
				Game1.drawWithBorder(Game1.player.festivalScore.ToString() ?? "", Color.Black, Color.White, new Vector2(96f, 21 + ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en) ? 8 : (LocalizedContentManager.CurrentLanguageLatin ? 16 : 8))), 0f, 1f, 1f, tiny: false);
				Game1.drawWithBorder(Utility.getMinutesSecondsStringFromMilliseconds(festivalTimer), Color.Black, Color.White, new Vector2(32f, 93f), 0f, 1f, 1f, tiny: false);
				b.End();
				b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
				if (Game1.IsMultiplayer)
				{
					Game1.player.team.festivalScoreStatus.Draw(b, new Vector2(32f, Game1.viewport.Height - 32), 4f, 0.99f, PlayerStatusList.HorizontalAlignment.Left, PlayerStatusList.VerticalAlignment.Bottom);
				}
				break;
			}
		}
		string text = spriteTextToDraw;
		if (text != null && text.Length > 0)
		{
			Color colorFromIndex = SpriteText.getColorFromIndex(int_useMeForAnything2);
			SpriteText.drawStringHorizontallyCenteredAt(b, spriteTextToDraw, Game1.graphics.GraphicsDevice.Viewport.Width / 2, Game1.graphics.GraphicsDevice.Viewport.Height - 192, int_useMeForAnything, -1, 999999, 1f, 1f, junimoText: false, colorFromIndex);
		}
		foreach (NPC actor in actors)
		{
			actor.drawAboveAlwaysFrontLayer(b);
		}
		if (skippable && !Game1.options.SnappyMenus && !Game1.game1.takingMapScreenshot)
		{
			Microsoft.Xna.Framework.Rectangle rectangle = skipBounds();
			Color white = Color.White;
			if (rectangle.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
			{
				white *= 0.5f;
			}
			b.Draw(sourceRectangle: new Microsoft.Xna.Framework.Rectangle(205, 406, 22, 15), texture: Game1.mouseCursors, position: Utility.PointToVector2(rectangle.Location), color: white, rotation: 0f, origin: Vector2.Zero, scale: 4f, effects: SpriteEffects.None, layerDepth: 0.92f);
		}
		currentCustomEventScript?.drawAboveAlwaysFront(b);
	}

	public void EndPlayerControlSequence()
	{
		playerControlSequence = false;
		playerControlSequenceID = null;
	}

	public void OnPlayerControlSequenceEnd(string id)
	{
		Game1.player.StopSitting();
		Game1.player.CanMove = false;
		Game1.player.Halt();
	}

	public void setUpPlayerControlSequence(string id)
	{
		playerControlSequenceID = id;
		playerControlSequence = true;
		Game1.player.CanMove = true;
		Game1.viewportFreeze = false;
		Game1.forceSnapOnNextViewportUpdate = true;
		Game1.globalFade = false;
		doingSecretSanta = false;
		if (id == null)
		{
			return;
		}
		switch (id.Length)
		{
		case 10:
			switch (id[0])
			{
			case 'h':
				if (id == "haleyBeach")
				{
					Vector2 tileLocation = new Vector2(53f, 8f);
					Object obj = ItemRegistry.Create<Object>("(O)742");
					obj.TileLocation = tileLocation;
					obj.Flipped = false;
					props.Add(obj);
					playerControlTargetTile = new Point(53, 8);
					Game1.player.canOnlyWalk = false;
				}
				break;
			case 'p':
				if (id == "parrotRide")
				{
					Game1.player.canOnlyWalk = false;
					currentCommand++;
				}
				break;
			case 'i':
				if (id == "iceFishing")
				{
					Tool tool = ItemRegistry.Create<Tool>("(T)BambooPole");
					tool.AttachmentSlotsCount = 2;
					tool.attachments[1] = ItemRegistry.Create<Object>("(O)687");
					festivalTimer = 120000;
					farmer.festivalScore = 0;
					farmer.CurrentToolIndex = 0;
					farmer.TemporaryItem = tool;
					farmer.CurrentToolIndex = 0;
				}
				break;
			}
			break;
		case 11:
			switch (id[0])
			{
			case 'e':
				if (id == "eggFestival")
				{
					festivalHost = getActorByName("Lewis");
					hostMessageKey = "Strings\\StringsFromCSFiles:Event.cs.1521";
				}
				break;
			case 'i':
			{
				if (!(id == "iceFestival"))
				{
					break;
				}
				festivalHost = getActorByName("Lewis");
				hostMessageKey = "Strings\\StringsFromCSFiles:Event.cs.1548";
				if (Game1.year % 2 == 0)
				{
					temporaryLocation.setFireplace(on: true, 46, 16, playSound: false, -28, 28);
					temporaryLocation.setFireplace(on: true, 61, 43, playSound: false, -28, 28);
				}
				else
				{
					temporaryLocation.setFireplace(on: true, 11, 44, playSound: false, -28, 28);
					temporaryLocation.setFireplace(on: true, 65, 45, playSound: false, -28, 28);
				}
				if (!Game1.MasterPlayer.mailReceived.Contains("raccoonTreeFallen"))
				{
					break;
				}
				for (int i = 52; i < 60; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						temporaryLocation.removeTile(i, j, "AlwaysFront");
					}
				}
				if (!NetWorldState.checkAnywhereForWorldStateID("forestStumpFixed"))
				{
					temporaryLocation.ApplyMapOverride("Forest_RaccoonStump", (Microsoft.Xna.Framework.Rectangle?)null, (Microsoft.Xna.Framework.Rectangle?)new Microsoft.Xna.Framework.Rectangle(53, 2, 7, 6));
				}
				else
				{
					temporaryLocation.ApplyMapOverride("Forest_RaccoonHouse", (Microsoft.Xna.Framework.Rectangle?)null, (Microsoft.Xna.Framework.Rectangle?)new Microsoft.Xna.Framework.Rectangle(53, 2, 7, 6));
				}
				break;
			}
			}
			break;
		case 4:
			switch (id[0])
			{
			case 'l':
				if (id == "luau")
				{
					festivalHost = getActorByName("Lewis");
					hostMessageKey = "Strings\\StringsFromCSFiles:Event.cs.1527";
				}
				break;
			case 'f':
				if (id == "fair")
				{
					festivalHost = getActorByName("Lewis");
					hostMessageKey = "Strings\\StringsFromCSFiles:Event.cs.1535";
				}
				break;
			}
			break;
		case 7:
			switch (id[0])
			{
			case 'j':
				if (id == "jellies")
				{
					festivalHost = getActorByName("Lewis");
					hostMessageKey = "Strings\\StringsFromCSFiles:Event.cs.1531";
				}
				break;
			case 'e':
			{
				if (!(id == "eggHunt"))
				{
					break;
				}
				Layer layer = Game1.currentLocation.map.RequireLayer("Paths");
				for (int num = 0; num < layer.LayerWidth; num++)
				{
					for (int num2 = 0; num2 < layer.LayerHeight; num2++)
					{
						Tile tile = layer.Tiles[num, num2];
						if (tile != null && tile.TileSheet.Id.StartsWith("fest"))
						{
							festivalProps.Add(new Prop(festivalTexture, tile.TileIndex, 1, 1, 1, num, num2));
						}
					}
				}
				festivalTimer = 52000;
				currentCommand++;
				break;
			}
			}
			break;
		case 9:
			switch (id[0])
			{
			case 'h':
				if (id == "halloween")
				{
					if (Game1.year % 2 == 0)
					{
						temporaryLocation.objects.Add(new Vector2(63f, 16f), new Chest(new List<Item> { ItemRegistry.Create("(O)PrizeTicket") }, new Vector2(63f, 16f)));
					}
					else
					{
						temporaryLocation.objects.Add(new Vector2(33f, 13f), new Chest(new List<Item> { ItemRegistry.Create("(O)373") }, new Vector2(33f, 13f)));
					}
				}
				break;
			case 'c':
				if (id == "christmas")
				{
					secretSantaRecipient = Utility.GetRandomWinterStarParticipant();
					mySecretSanta = Utility.GetRandomWinterStarParticipant((string name) => name == secretSantaRecipient.Name || NPC.IsDivorcedFrom(farmer, name)) ?? secretSantaRecipient;
					Game1.debugOutput = "Secret Santa Recipient: " + secretSantaRecipient.Name + "  My Secret Santa: " + mySecretSanta.Name;
				}
				break;
			}
			break;
		case 14:
			if (id == "flowerFestival")
			{
				festivalHost = getActorByName("Lewis");
				hostMessageKey = "Strings\\StringsFromCSFiles:Event.cs.1524";
				if (NetWorldState.checkAnywhereForWorldStateID("trashBearDone"))
				{
					Game1.currentLocation.removeMapTile(62, 28, "Buildings");
					Game1.currentLocation.removeMapTile(64, 28, "Buildings");
					Game1.currentLocation.removeMapTile(73, 48, "Buildings");
				}
			}
			break;
		case 8:
			if (id == "boatRide")
			{
				Game1.viewportFreeze = true;
				Game1.currentViewportTarget = Utility.PointToVector2(Game1.viewportCenter);
				currentCommand++;
			}
			break;
		case 5:
		case 6:
		case 12:
		case 13:
			break;
		}
	}

	public bool canMoveAfterDialogue()
	{
		if (playerControlSequenceID != null && playerControlSequenceID.Equals("eggHunt"))
		{
			Game1.player.canMove = true;
			CurrentCommand++;
		}
		return playerControlSequence;
	}

	public void forceFestivalContinue()
	{
		bool flag = isSpecificFestival("fall16");
		if (flag)
		{
			initiateGrangeJudging();
		}
		else
		{
			Game1.dialogueUp = false;
			if (Game1.activeClickableMenu != null)
			{
				Game1.activeClickableMenu.emergencyShutDown();
			}
			Game1.exitActiveMenu();
			if (!TryGetFestivalDataForYear("mainEvent", out var data))
			{
				Game1.log.Error("Festival " + id + " doesn't have the required 'mainEvent' data field.");
			}
			string[] array = ParseCommands(data);
			eventCommands = array;
			CurrentCommand = 0;
			eventSwitched = true;
			playerControlSequence = false;
			setUpFestivalMainEvent();
			Game1.player.Halt();
		}
		if (Game1.IsServer && (flag || !Game1.HasDedicatedHost))
		{
			Game1.multiplayer.sendServerToClientsMessage("festivalEvent");
		}
	}

	/// <summary>Split an event's key into its ID and preconditions.</summary>
	/// <param name="rawScript">The event key to split.</param>
	public static string[] SplitPreconditions(string rawScript)
	{
		return ArgUtility.SplitQuoteAware(rawScript, '/', StringSplitOptions.RemoveEmptyEntries, keepQuotesAndEscapes: true);
	}

	/// <summary>Split and preprocess a raw event script into its component commands.</summary>
	/// <param name="rawScript">The raw event script to split.</param>
	/// <param name="player">The player for which the event is being parsed.</param>
	public static string[] ParseCommands(string rawScript, Farmer player = null)
	{
		rawScript = Dialogue.applyGenderSwitchBlocks(((player != null) ? new Gender?(player.Gender) : Game1.player?.Gender).GetValueOrDefault(), rawScript);
		rawScript = TokenParser.ParseText(rawScript);
		return ArgUtility.SplitQuoteAware(rawScript, '/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, keepQuotesAndEscapes: true);
	}

	public bool isSpecificFestival(string festivalId)
	{
		if (isFestival)
		{
			return id == "festival_" + festivalId;
		}
		return false;
	}

	public void setUpFestivalMainEvent()
	{
		if (!isSpecificFestival("spring24"))
		{
			return;
		}
		List<NetDancePartner> list = new List<NetDancePartner>();
		List<NetDancePartner> list2 = new List<NetDancePartner>();
		List<string> list3 = new List<string> { "Abigail", "Penny", "Leah", "Maru", "Haley", "Emily" };
		List<string> list4 = new List<string> { "Sebastian", "Sam", "Elliott", "Harvey", "Alex", "Shane" };
		List<Farmer> list5 = (from f in Game1.getOnlineFarmers()
			orderby f.UniqueMultiplayerID
			select f).ToList();
		while (list5.Count > 0)
		{
			Farmer farmer = list5[0];
			list5.RemoveAt(0);
			if (Game1.multiplayer.isDisconnecting(farmer) || farmer.dancePartner.Value == null)
			{
				continue;
			}
			if (farmer.dancePartner.GetGender() == Gender.Female)
			{
				list.Add(farmer.dancePartner);
				if (farmer.dancePartner.IsVillager())
				{
					list3.Remove(farmer.dancePartner.TryGetVillager().Name);
				}
				list2.Add(new NetDancePartner(farmer));
			}
			else
			{
				list2.Add(farmer.dancePartner);
				if (farmer.dancePartner.IsVillager())
				{
					list4.Remove(farmer.dancePartner.TryGetVillager().Name);
				}
				list.Add(new NetDancePartner(farmer));
			}
			if (farmer.dancePartner.IsFarmer())
			{
				list5.Remove(farmer.dancePartner.TryGetFarmer());
			}
		}
		while (list.Count < 6)
		{
			string text = list3.Last();
			if (list4.Contains(Utility.getLoveInterest(text)))
			{
				list.Add(new NetDancePartner(text));
				list2.Add(new NetDancePartner(Utility.getLoveInterest(text)));
			}
			list3.Remove(text);
		}
		if (!TryGetFestivalDataForYear("mainEvent", out var data))
		{
			data = string.Empty;
		}
		for (int num = 1; num <= 6; num++)
		{
			string newValue = ((!list[num - 1].IsVillager()) ? ("farmer" + Utility.getFarmerNumberFromFarmer(list[num - 1].TryGetFarmer())) : list[num - 1].TryGetVillager().Name);
			string newValue2 = ((!list2[num - 1].IsVillager()) ? ("farmer" + Utility.getFarmerNumberFromFarmer(list2[num - 1].TryGetFarmer())) : list2[num - 1].TryGetVillager().Name);
			data = data.Replace("Girl" + num, newValue);
			data = data.Replace("Guy" + num, newValue2);
		}
		List<KeyValuePair<NetDancePartner, NetDancePartner>> list6 = new List<KeyValuePair<NetDancePartner, NetDancePartner>>();
		List<KeyValuePair<NetDancePartner, NetDancePartner>> list7 = new List<KeyValuePair<NetDancePartner, NetDancePartner>>();
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			NetDancePartner netDancePartner = list[num2];
			NetDancePartner netDancePartner2 = list2[num2];
			if (netDancePartner.IsFarmer() || netDancePartner2.IsFarmer())
			{
				list7.Add(new KeyValuePair<NetDancePartner, NetDancePartner>(netDancePartner, netDancePartner2));
				list.RemoveAt(num2);
				list2.RemoveAt(num2);
			}
		}
		list6.AddRange(list7.OrderBy(delegate(KeyValuePair<NetDancePartner, NetDancePartner> pair)
		{
			int farmerNumberFromFarmer = Utility.getFarmerNumberFromFarmer(pair.Key.TryGetFarmer());
			int farmerNumberFromFarmer2 = Utility.getFarmerNumberFromFarmer(pair.Value.TryGetFarmer());
			if (farmerNumberFromFarmer > -1 && farmerNumberFromFarmer2 > -1)
			{
				return Math.Min(farmerNumberFromFarmer, farmerNumberFromFarmer2);
			}
			return (farmerNumberFromFarmer <= -1) ? farmerNumberFromFarmer2 : farmerNumberFromFarmer;
		}));
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			list6.Add(new KeyValuePair<NetDancePartner, NetDancePartner>(list[num3], list2[num3]));
		}
		list.Clear();
		list2.Clear();
		bool flag = true;
		foreach (KeyValuePair<NetDancePartner, NetDancePartner> item in list6)
		{
			if (flag)
			{
				list.Insert(0, item.Key);
				list2.Insert(0, item.Value);
			}
			else
			{
				list.Add(item.Key);
				list2.Add(item.Value);
			}
			flag = !flag;
		}
		List<string> list8 = new List<string>(ParseCommands(data));
		for (int num4 = 0; num4 < list8.Count; num4++)
		{
			string text2 = list8[num4];
			List<NetDancePartner> list9 = null;
			string oldValue = null;
			if (text2.Contains("Girls"))
			{
				oldValue = "Girls";
				list9 = list;
			}
			else if (text2.Contains("Guys"))
			{
				oldValue = "Guys";
				list9 = list2;
			}
			if (list9 == null)
			{
				continue;
			}
			float num5 = 10f / (float)(list9.Count - 1);
			if (num5 < 1f)
			{
				num5 = 1f;
			}
			for (int num6 = 0; num6 < list9.Count; num6++)
			{
				string newValue3 = (list9[num6].IsVillager() ? list9[num6].TryGetVillager().Name : ("farmer" + Utility.getFarmerNumberFromFarmer(list9[num6].TryGetFarmer())));
				string text3 = text2.Replace(oldValue, newValue3);
				if (text3.StartsWith("warp "))
				{
					string[] array = ArgUtility.SplitBySpace(text3);
					int num7 = int.Parse(array[2]);
					array[2] = (num7 + (int)Math.Round((float)num6 * num5)).ToString();
					text3 = string.Join(" ", array);
				}
				list8.Insert(num4 + num6, text3);
			}
			num4 += list9.Count;
			list8.RemoveAt(num4);
			num4--;
		}
		data = string.Join("/", list8);
		Regex regex = new Regex("showFrame (?<farmerName>farmer\\d) 44");
		Regex regex2 = new Regex("showFrame (?<farmerName>farmer\\d) 40");
		Regex regex3 = new Regex("animate (?<farmerName>farmer\\d) false true 600 44 45");
		Regex regex4 = new Regex("animate (?<farmerName>farmer\\d) false true 600 43 41 43 42");
		Regex regex5 = new Regex("animate (?<farmerName>farmer\\d) false true 300 46 47");
		Regex regex6 = new Regex("animate (?<farmerName>farmer\\d) false true 600 46 47");
		data = regex.Replace(data, "showFrame $1 12/faceDirection $1 0");
		data = regex2.Replace(data, "showFrame $1 0/faceDirection $1 2");
		data = regex3.Replace(data, "animate $1 false true 600 12 13 12 14");
		data = regex4.Replace(data, "animate $1 false true 596 4 0");
		data = regex5.Replace(data, "animate $1 false true 150 12 13 12 14");
		data = regex6.Replace(data, "animate $1 false true 600 0 3");
		eventCommands = ParseCommands(data);
	}

	private void judgeGrange()
	{
		int num = 14;
		Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
		int num2 = 0;
		bool flag = false;
		foreach (Item item in Game1.player.team.grangeDisplay)
		{
			if (item is Object obj)
			{
				if (IsItemMayorShorts(obj))
				{
					flag = true;
				}
				num += obj.Quality + 1;
				int num3 = obj.sellToStorePrice(-1L);
				if (num3 >= 20)
				{
					num++;
				}
				if (num3 >= 90)
				{
					num++;
				}
				if (num3 >= 200)
				{
					num++;
				}
				if (num3 >= 300 && obj.Quality < 2)
				{
					num++;
				}
				if (num3 >= 400 && obj.Quality < 1)
				{
					num++;
				}
				switch (obj.Category)
				{
				case -75:
					dictionary[-75] = true;
					break;
				case -79:
					dictionary[-79] = true;
					break;
				case -18:
				case -14:
				case -6:
				case -5:
					dictionary[-5] = true;
					break;
				case -12:
				case -2:
					dictionary[-12] = true;
					break;
				case -4:
					dictionary[-4] = true;
					break;
				case -81:
				case -80:
				case -27:
					dictionary[-81] = true;
					break;
				case -7:
					dictionary[-7] = true;
					break;
				case -26:
					dictionary[-26] = true;
					break;
				}
			}
			else if (item == null)
			{
				num2++;
			}
		}
		num += Math.Min(30, dictionary.Count * 5);
		int num4 = 9 - 2 * num2;
		num += num4;
		grangeScore = num;
		if (flag)
		{
			grangeScore = -666;
		}
	}

	private void lewisDoneJudgingGrange()
	{
		if (Game1.activeClickableMenu == null)
		{
			Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1584")));
			Game1.player.Halt();
		}
		interpretGrangeResults();
	}

	public void interpretGrangeResults()
	{
		List<Character> list = new List<Character>
		{
			getActorByName("Pierre"),
			getActorByName("Marnie"),
			getActorByName("Willy")
		};
		if (grangeScore >= 90)
		{
			list.Insert(0, Game1.player);
		}
		else if (grangeScore >= 75)
		{
			list.Insert(1, Game1.player);
		}
		else if (grangeScore >= 60)
		{
			list.Insert(2, Game1.player);
		}
		else
		{
			list.Add(Game1.player);
		}
		bool flag = (list[0] as NPC)?.Name == "Pierre";
		bool flag2 = Game1.player.team.grangeDisplay.Count == 0;
		bool flag3 = grangeScore == -666;
		foreach (NPC actor in actors)
		{
			Dialogue dialogue = null;
			dialogue = ((!flag) ? (actor.TryGetDialogue("Fair_Judged_PlayerWon") ?? actor.TryGetDialogue("Fair_Judged")) : ((flag3 ? actor.TryGetDialogue("Fair_Judged_PlayerLost_PurpleShorts") : null) ?? (flag2 ? actor.TryGetDialogue("Fair_Judged_PlayerLost_Skipped") : null) ?? actor.TryGetDialogue("Fair_Judged_PlayerLost") ?? actor.TryGetDialogue("Fair_Judged")));
			if (dialogue != null)
			{
				actor.setNewDialogue(dialogue);
			}
		}
		grangeJudged = true;
		if (!(list[0] is Farmer))
		{
			return;
		}
		foreach (Farmer allFarmer in Game1.getAllFarmers())
		{
			allFarmer.autoGenerateActiveDialogueEvent("wonGrange");
		}
	}

	private void initiateGrangeJudging()
	{
		judgeGrange();
		hostMessageKey = null;
		setUpAdvancedMove(ArgUtility.SplitBySpace("advancedMove Lewis False 2 0 0 7 8 0 4 3000 3 0 4 3000 3 0 4 3000 3 0 4 3000 -14 0 2 1000"), lewisDoneJudgingGrange);
		getActorByName("Lewis").CurrentDialogue.Clear();
		if (getActorByName("Marnie") != null)
		{
			npcControllers.RemoveAll((NPCController npcController) => npcController.puppet.Name == "Marnie");
		}
		setUpAdvancedMove(ArgUtility.SplitBySpace("advancedMove Marnie False 0 1 4 1000"));
		foreach (NPC actor in actors)
		{
			Dialogue dialogue = actor.TryGetDialogue("Fair_Judging");
			if (dialogue != null)
			{
				actor.setNewDialogue(dialogue);
			}
		}
	}

	public void answerDialogueQuestion(NPC who, string answerKey)
	{
		if (!isFestival)
		{
			return;
		}
		switch (answerKey)
		{
		case "yes":
			if (Game1.HasDedicatedHost)
			{
				if (isSpecificFestival("fall16"))
				{
					if (Game1.IsServer)
					{
						forceFestivalContinue();
					}
					else
					{
						Game1.dedicatedServer.DoHostAction("JudgeGrange");
					}
					break;
				}
				string checkName = "MainEvent_" + id;
				Game1.netReady.SetLocalReady(checkName, ready: true);
				Game1.activeClickableMenu = new ReadyCheckDialog(checkName, allowCancel: true, delegate
				{
					forceFestivalContinue();
				});
			}
			else
			{
				forceFestivalContinue();
			}
			break;
		case "danceAsk":
			if (Game1.player.spouse != null && who.Name == Game1.player.spouse)
			{
				Game1.player.dancePartner.Value = who;
				who.setNewDialogue(who.TryGetDialogue("FlowerDance_Accept_" + (Game1.player.isRoommate(who.Name) ? "Roommate" : "Spouse")) ?? who.TryGetDialogue("FlowerDance_Accept") ?? new Dialogue(who, "Strings\\StringsFromCSFiles:Event.cs.1632"));
				foreach (NPC actor in actors)
				{
					Stack<Dialogue> currentDialogue = actor.CurrentDialogue;
					if (currentDialogue != null && currentDialogue.Count > 0 && actor.CurrentDialogue.Peek().getCurrentDialogue().Equals("..."))
					{
						actor.CurrentDialogue.Clear();
					}
				}
			}
			else if (!who.HasPartnerForDance && Game1.player.getFriendshipLevelForNPC(who.Name) >= 1000 && !who.isMarried())
			{
				try
				{
					Game1.player.changeFriendship(250, Game1.getCharacterFromName(who.Name));
				}
				catch
				{
				}
				Game1.player.dancePartner.Value = who;
				who.setNewDialogue(who.TryGetDialogue("FlowerDance_Accept") ?? ((who.Gender == Gender.Female) ? new Dialogue(who, "Strings\\StringsFromCSFiles:Event.cs.1634") : new Dialogue(who, "Strings\\StringsFromCSFiles:Event.cs.1633")));
				foreach (NPC actor2 in actors)
				{
					Stack<Dialogue> currentDialogue2 = actor2.CurrentDialogue;
					if (currentDialogue2 != null && currentDialogue2.Count > 0 && actor2.CurrentDialogue.Peek().getCurrentDialogue().Equals("..."))
					{
						actor2.CurrentDialogue.Clear();
					}
				}
			}
			else if (who.HasPartnerForDance)
			{
				who.setNewDialogue("Strings\\StringsFromCSFiles:Event.cs.1635");
			}
			else
			{
				Dialogue dialogue = who.TryGetDialogue("FlowerDance_Decline") ?? who.TryGetDialogue("danceRejection");
				if (dialogue == null)
				{
					break;
				}
				who.setNewDialogue(dialogue);
			}
			Game1.drawDialogue(who);
			who.immediateSpeak = true;
			who.facePlayer(Game1.player);
			who.Halt();
			break;
		case "no":
			break;
		}
	}

	public void addItemToGrangeDisplay(Item i, int position, bool force)
	{
		while (Game1.player.team.grangeDisplay.Count < 9)
		{
			Game1.player.team.grangeDisplay.Add(null);
		}
		if (position >= 0 && position < Game1.player.team.grangeDisplay.Count && (Game1.player.team.grangeDisplay[position] == null || force))
		{
			Game1.player.team.grangeDisplay[position] = i;
		}
	}

	private bool onGrangeChange(Item i, int position, Item old, StorageContainer container, bool onRemoval)
	{
		if (!onRemoval)
		{
			if (i.Stack > 1 || (i.Stack == 1 && old != null && old.Stack == 1 && i.canStackWith(old)))
			{
				if (old != null && i != null && old.canStackWith(i))
				{
					container.ItemsToGrabMenu.actualInventory[position].Stack = 1;
					container.heldItem = old;
					return false;
				}
				if (old != null)
				{
					Utility.addItemToInventory(old, position, container.ItemsToGrabMenu.actualInventory);
					container.heldItem = i;
					return false;
				}
				int stack = i.Stack - 1;
				Item one = i.getOne();
				one.Stack = stack;
				container.heldItem = one;
				i.Stack = 1;
			}
		}
		else if (old != null && old.Stack > 1 && !old.Equals(i))
		{
			return false;
		}
		addItemToGrangeDisplay((onRemoval && (old == null || old.Equals(i))) ? null : i, position, force: true);
		return true;
	}

	public bool canPlayerUseTool()
	{
		if (isSpecificFestival("winter8") && festivalTimer > 0 && !Game1.player.UsingTool)
		{
			previousFacingDirection = Game1.player.FacingDirection;
			return true;
		}
		return false;
	}

	public bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		if (isFestival)
		{
			if (temporaryLocation != null && temporaryLocation.objects.TryGetValue(new Vector2(tileLocation.X, tileLocation.Y), out var value))
			{
				value.checkForAction(who);
			}
			GameLocation currentLocation = Game1.currentLocation;
			switch (id)
			{
			case "festival_fall16":
				switch (currentLocation.getTileIndexAt(tileLocation.X, tileLocation.Y, "Buildings", "untitled tile sheet"))
				{
				case 175:
				case 176:
					if (who.IsLocalPlayer)
					{
						Game1.player.eatObject(ItemRegistry.Create<Object>("(O)241"), overrideFullness: true);
					}
					return true;
				case 308:
				case 309:
					if (who.IsLocalPlayer)
					{
						Response[] answerChoices3 = new Response[3]
						{
							new Response("Orange", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1645")),
							new Response("Green", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1647")),
							new Response("I", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1650"))
						};
						currentLocation.createQuestionDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1652")), answerChoices3, "wheelBet");
					}
					return true;
				case 87:
				case 88:
					if (who.IsLocalPlayer)
					{
						Response[] answerChoices5 = new Response[2]
						{
							new Response("Buy", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1654")),
							new Response("Leave", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1656"))
						};
						currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1659"), answerChoices5, "StarTokenShop");
					}
					return true;
				case 501:
				case 502:
					if (who.IsLocalPlayer)
					{
						Response[] answerChoices4 = new Response[2]
						{
							new Response("Play", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1662")),
							new Response("Leave", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1663"))
						};
						currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1666"), answerChoices4, "slingshotGame");
					}
					return true;
				case 510:
				case 511:
					if (who.IsLocalPlayer)
					{
						currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1672"), currentLocation.createYesNoResponses(), "starTokenShop");
					}
					return true;
				case 349:
				case 350:
				case 351:
					Game1.player.team.grangeMutex.RequestLock(delegate
					{
						while (Game1.player.team.grangeDisplay.Count < 9)
						{
							Game1.player.team.grangeDisplay.Add(null);
						}
						Game1.activeClickableMenu = new StorageContainer(Game1.player.team.grangeDisplay.ToList(), 9, 3, onGrangeChange, Utility.highlightSmallObjects);
					});
					return true;
				case 503:
				case 504:
					if (who.IsLocalPlayer)
					{
						Response[] answerChoices2 = new Response[2]
						{
							new Response("Play", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1662")),
							new Response("Leave", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1663"))
						};
						currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1681"), answerChoices2, "fishingGame");
					}
					return true;
				case 540:
					if (who.IsLocalPlayer)
					{
						if (who.TilePoint.X == 29)
						{
							Game1.activeClickableMenu = new StrengthGame();
						}
						else
						{
							Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1684")));
						}
					}
					return true;
				case 505:
				case 506:
					if (who.IsLocalPlayer)
					{
						if (who.Money >= 100 && !who.mailReceived.Contains("fortuneTeller" + Game1.year))
						{
							Response[] answerChoices = new Response[2]
							{
								new Response("Read", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1688")),
								new Response("No", Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1690"))
							};
							currentLocation.createQuestionDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1691")), answerChoices, "fortuneTeller");
						}
						else if (who.mailReceived.Contains("fortuneTeller" + Game1.year))
						{
							Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1694")));
						}
						else
						{
							Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1695")));
						}
						who.Halt();
					}
					return true;
				}
				break;
			case "festival_fall27":
				if (currentLocation.getTileIndexAt(tileLocation.X, tileLocation.Y, "Buildings", "Landscape") == 958 && ((tileLocation.X == 44 && tileLocation.Y == 9) || (tileLocation.X == 61 && tileLocation.Y == 13)))
				{
					if (who.IsLocalPlayer)
					{
						currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\1_6_Strings:SpiritsEveCart"), currentLocation.createYesNoResponses(), "spirits_eve_shortcut");
					}
					return true;
				}
				break;
			case "festival_winter8":
			{
				int tileIndexAt = currentLocation.getTileIndexAt(tileLocation.X, tileLocation.Y, "Buildings", "fest");
				if ((uint)(tileIndexAt - 1009) <= 1u || (uint)(tileIndexAt - 1012) <= 1u)
				{
					Game1.playSound("pig");
					return true;
				}
				break;
			}
			}
			string text = currentLocation.doesTileHaveProperty(tileLocation.X, tileLocation.Y, "Action", "Buildings");
			if (text != null)
			{
				try
				{
					string[] array = ArgUtility.SplitBySpace(text);
					switch (ArgUtility.Get(array, 0))
					{
					case "OpenShop":
					case "Shop":
					{
						if (!ArgUtility.TryGet(array, 1, out var value2, out var error, allowBlank: true, "string shop_id"))
						{
							currentLocation.LogTileActionError(array, tileLocation.X, tileLocation.Y, error);
							return false;
						}
						if (!who.IsLocalPlayer)
						{
							return false;
						}
						bool flag = false;
						if (value2 == "shop" && isFestival)
						{
							switch (id)
							{
							case "festival_fall27":
								value2 = "Festival_SpiritsEve_Pierre";
								break;
							case "festival_spring13":
								value2 = "Festival_EggFestival_Pierre";
								break;
							case "festival_spring24":
								value2 = "Festival_FlowerDance_Pierre";
								break;
							case "festival_summer11":
								value2 = "Festival_Luau_Pierre";
								break;
							case "festival_summer28":
								value2 = "Festival_DanceOfTheMoonlightJellies_Pierre";
								break;
							case "festival_winter8":
								value2 = "Festival_FestivalOfIce_TravelingMerchant";
								break;
							case "festival_winter25":
								value2 = "Festival_FeastOfTheWinterStar_Pierre";
								break;
							}
						}
						if (festivalData.TryGetValue(value2, out var value3))
						{
							if (festivalShops == null)
							{
								festivalShops = new Dictionary<string, Dictionary<ISalable, ItemStockInformation>>();
							}
							if (!festivalShops.TryGetValue(value2, out var value4))
							{
								string[] array2 = ArgUtility.SplitBySpace(value3);
								value4 = new Dictionary<ISalable, ItemStockInformation>();
								for (int num = 0; num < array2.Length; num += 4)
								{
									if (!ArgUtility.TryGet(array, num, out var value5, out error, allowBlank: true, "string type") || !ArgUtility.TryGet(array, num + 1, out var value6, out error, allowBlank: true, "string itemId") || !ArgUtility.TryGetInt(array, num + 2, out var value7, out error, "int price") || !ArgUtility.TryGetInt(array, num + 3, out var value8, out error, "int stock"))
									{
										Game1.log.Error($"Festival '{id}' has legacy shop inventory '{string.Join(" ", array2)}' which couldn't be parsed: {error}.");
										break;
									}
									Item itemFromStandardTextDescription = Utility.getItemFromStandardTextDescription(value5, value6, value8, who);
									if (itemFromStandardTextDescription != null)
									{
										if (itemFromStandardTextDescription.Category == -74)
										{
											value7 = (int)Math.Max(1f, (float)value7 * Game1.MasterPlayer.difficultyModifier);
										}
										if (!itemFromStandardTextDescription.IsRecipe || !who.knowsRecipe(itemFromStandardTextDescription.Name))
										{
											value4.Add(itemFromStandardTextDescription, new ItemStockInformation(value7, (value8 <= 0) ? int.MaxValue : value8, null, null, LimitedStockMode.Player));
										}
									}
								}
								festivalShops[value2] = value4;
							}
							if (value4 != null && value4.Count > 0)
							{
								who.team.synchronizedShopStock.UpdateLocalStockWithSyncedQuanitities(who.currentLocation.Name + value2, value4);
								Game1.activeClickableMenu = new ShopMenu(id + "_" + value2, value4);
								flag = true;
							}
						}
						bool showedClosedMessage = false;
						if (!flag && Utility.TryOpenShopMenu(value2, temporaryLocation, null, null, forceOpen: false, playOpenSound: true, delegate(string message)
						{
							showedClosedMessage = true;
							Game1.drawObjectDialogue(message);
						}))
						{
							flag = true;
						}
						if (!flag && !showedClosedMessage)
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1714"));
						}
						break;
					}
					case "Message":
					{
						if (!ArgUtility.TryGet(array, 1, out var value10, out var error3, allowBlank: true, "string translationKey"))
						{
							currentLocation.LogTileActionError(array, tileLocation.X, tileLocation.Y, error3);
							return false;
						}
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromMaps:" + value10.Replace("\"", "")));
						break;
					}
					case "Dialogue":
					{
						if (!ArgUtility.TryGetRemainder(array, 1, out var value9, out var error2, ' ', "string dialogue"))
						{
							currentLocation.LogTileActionError(array, tileLocation.X, tileLocation.Y, error2);
							return false;
						}
						Game1.drawObjectDialogue(value9.Replace("#", " "));
						break;
					}
					case "LuauSoup":
						if (!specialEventVariable2)
						{
							Game1.activeClickableMenu = new ItemGrabMenu(null, reverseGrab: true, showReceivingMenu: false, Utility.highlightLuauSoupItems, clickToAddItemToLuauSoup, Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1719"), null, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: false, 0, null, -1, this);
						}
						break;
					}
				}
				catch (Exception)
				{
				}
				return false;
			}
			if (who.IsLocalPlayer && (!playerControlSequence || !playerControlSequenceID.Equals("iceFishing")))
			{
				foreach (NPC actor in actors)
				{
					Point tilePoint = actor.TilePoint;
					Microsoft.Xna.Framework.Rectangle value11 = new Microsoft.Xna.Framework.Rectangle(tileLocation.X * 64, tileLocation.Y * 64, 64, 64);
					if (tilePoint.X == tileLocation.X && tilePoint.Y == tileLocation.Y && actor is Child child)
					{
						child.checkAction(who, temporaryLocation);
						return true;
					}
					if (((tilePoint.X != tileLocation.X || (tilePoint.Y != tileLocation.Y && tilePoint.Y != tileLocation.Y + 1)) && !actor.GetBoundingBox().Intersects(value11)) || (actor.CurrentDialogue.Count < 1 && (actor.CurrentDialogue.Count <= 0 || actor.CurrentDialogue.Peek().isOnFinalDialogue()) && !actor.Equals(festivalHost) && (!actor.datable.Value || !isSpecificFestival("spring24")) && (secretSantaRecipient == null || !actor.Name.Equals(secretSantaRecipient.Name))))
					{
						continue;
					}
					Friendship value12;
					bool flag2 = who.friendshipData.TryGetValue(actor.Name, out value12) && value12.IsDivorced();
					if ((grangeScore > -100 || grangeScore == -666) && actor.Equals(festivalHost) && grangeJudged)
					{
						Dialogue dialogue;
						if (grangeScore >= 90)
						{
							Game1.playSound("reward");
							dialogue = Dialogue.FromTranslation(actor, "Strings\\StringsFromCSFiles:Event.cs.1723", grangeScore);
							Game1.player.festivalScore += 1000;
							Game1.getAchievement(37);
						}
						else if (grangeScore >= 75)
						{
							Game1.playSound("reward");
							dialogue = Dialogue.FromTranslation(actor, "Strings\\StringsFromCSFiles:Event.cs.1726", grangeScore);
							Game1.player.festivalScore += 500;
						}
						else if (grangeScore >= 60)
						{
							Game1.playSound("newArtifact");
							dialogue = Dialogue.FromTranslation(actor, "Strings\\StringsFromCSFiles:Event.cs.1729", grangeScore);
							Game1.player.festivalScore += 250;
						}
						else if (grangeScore == -666)
						{
							Game1.playSound("secret1");
							dialogue = new Dialogue(actor, "Strings\\StringsFromCSFiles:Event.cs.1730");
							Game1.player.festivalScore += 750;
						}
						else
						{
							Game1.playSound("newArtifact");
							dialogue = Dialogue.FromTranslation(actor, "Strings\\StringsFromCSFiles:Event.cs.1732", grangeScore);
							Game1.player.festivalScore += 50;
						}
						grangeScore = -100;
						actor.setNewDialogue(dialogue);
					}
					else if ((Game1.HasDedicatedHost || Game1.serverHost == null || Game1.player.Equals(Game1.serverHost.Value)) && actor.Equals(festivalHost) && (actor.CurrentDialogue.Count == 0 || actor.CurrentDialogue.Peek().isOnFinalDialogue()) && hostMessageKey != null)
					{
						actor.setNewDialogue(hostMessageKey);
					}
					if (isSpecificFestival("spring24") && !flag2)
					{
						bool? flag3 = actor.GetData()?.FlowerDanceCanDance;
						bool num2;
						if (!flag3.HasValue)
						{
							if (actor.datable.Value)
							{
								goto IL_0f2f;
							}
							num2 = actor.Name == who.spouse;
						}
						else
						{
							num2 = flag3 == true;
						}
						if (num2)
						{
							goto IL_0f2f;
						}
					}
					goto IL_1149;
					IL_0f2f:
					actor.grantConversationFriendship(who);
					if (who.dancePartner.Value == null)
					{
						if (actor.CurrentDialogue.Count > 0 && actor.CurrentDialogue.Peek().getCurrentDialogue().Equals("..."))
						{
							actor.CurrentDialogue.Clear();
						}
						if (actor.CurrentDialogue.Count == 0)
						{
							actor.CurrentDialogue.Push(new Dialogue(actor, null, "..."));
							if (actor.name.Value == who.spouse)
							{
								actor.setNewDialogue(Dialogue.FromTranslation(actor, "Strings\\StringsFromCSFiles:Event.cs.1736", actor.displayName), add: true);
							}
							else
							{
								actor.setNewDialogue(Dialogue.FromTranslation(actor, "Strings\\StringsFromCSFiles:Event.cs.1738", actor.displayName), add: true);
							}
						}
						else if (actor.CurrentDialogue.Peek().isOnFinalDialogue())
						{
							Dialogue item = actor.CurrentDialogue.Peek();
							if (who.spouse != null && actor.Name == who.spouse)
							{
								Dialogue dialogue2 = null;
								if (actor.isRoommate())
								{
									TryGetFestivalDialogueForYear(actor, actor.Name + "_roommate", out dialogue2);
								}
								if (dialogue2 == null)
								{
									TryGetFestivalDialogueForYear(actor, actor.Name + "_spouse", out dialogue2);
								}
								if (dialogue2 != null)
								{
									actor.CurrentDialogue.Clear();
									actor.CurrentDialogue.Push(dialogue2);
									item = actor.CurrentDialogue.Peek();
								}
							}
							Game1.drawDialogue(actor);
							actor.faceTowardFarmerForPeriod(3000, 2, faceAway: false, who);
							who.Halt();
							actor.CurrentDialogue = new Stack<Dialogue>();
							actor.CurrentDialogue.Push(new Dialogue(actor, null, "..."));
							actor.CurrentDialogue.Push(item);
							return true;
						}
					}
					else if (actor.CurrentDialogue.Count > 0 && actor.CurrentDialogue.Peek().getCurrentDialogue().Equals("..."))
					{
						actor.CurrentDialogue.Clear();
					}
					goto IL_1149;
					IL_1149:
					if (!flag2 && secretSantaRecipient != null && actor.Name.Equals(secretSantaRecipient.Name))
					{
						actor.grantConversationFriendship(who);
						currentLocation.createQuestionDialogue(Game1.parseText((secretSantaRecipient.Gender == Gender.Male) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1740", secretSantaRecipient.displayName) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1741", secretSantaRecipient.displayName)), currentLocation.createYesNoResponses(), "secretSanta");
						who.Halt();
						return true;
					}
					if (actor.CurrentDialogue.Count == 0)
					{
						return true;
					}
					if (who.spouse != null && actor.Name == who.spouse && !isSpecificFestival("spring24"))
					{
						Dialogue dialogue3 = null;
						if (actor.isRoommate())
						{
							TryGetFestivalDialogueForYear(actor, actor.Name + "_roommate", out dialogue3);
						}
						if (dialogue3 == null)
						{
							TryGetFestivalDialogueForYear(actor, actor.Name + "_spouse", out dialogue3);
						}
						if (dialogue3 != null && (actor.CurrentDialogue.Count == 0 || !actor.CurrentDialogue.Peek().TranslationKey.Equals(dialogue3.TranslationKey)))
						{
							actor.CurrentDialogue.Clear();
							actor.CurrentDialogue.Push(dialogue3);
						}
					}
					if (flag2)
					{
						actor.CurrentDialogue.Clear();
						actor.CurrentDialogue.Push(new Dialogue(actor, "Characters\\Dialogue\\" + actor.Name + ":divorced"));
					}
					actor.grantConversationFriendship(who);
					if (actor.CurrentDialogue == null || actor.CurrentDialogue.Count == 0 || !actor.CurrentDialogue.Peek().dontFaceFarmer)
					{
						actor.faceTowardFarmerForPeriod(3000, 2, faceAway: false, who);
					}
					Game1.drawDialogue(actor);
					who.Halt();
					return true;
				}
			}
			if (festivalData != null && isSpecificFestival("spring13"))
			{
				Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle(tileLocation.X * 64, tileLocation.Y * 64, 64, 64);
				for (int num3 = festivalProps.Count - 1; num3 >= 0; num3--)
				{
					if (festivalProps[num3].isColliding(rectangle))
					{
						who.festivalScore++;
						festivalProps.RemoveAt(num3);
						who.team.FestivalPropsRemoved(rectangle);
						if (who.IsLocalPlayer)
						{
							Game1.playSound("coin");
						}
						return true;
					}
				}
			}
			foreach (MapSeat mapSeat in currentLocation.mapSeats)
			{
				if (mapSeat.OccupiesTile(tileLocation.X, tileLocation.Y) && !mapSeat.IsBlocked(currentLocation))
				{
					who.BeginSitting(mapSeat);
					return true;
				}
			}
		}
		return false;
	}

	public void removeFestivalProps(Microsoft.Xna.Framework.Rectangle rect)
	{
		festivalProps.RemoveAll((Prop prop) => prop.isColliding(rect));
	}

	public void checkForSpecialCharacterIconAtThisTile(Vector2 tileLocation)
	{
		if (isFestival && festivalHost != null && festivalHost.Tile == tileLocation)
		{
			Game1.mouseCursor = Game1.cursor_talk;
		}
	}

	public void forceEndFestival(Farmer who)
	{
		Game1.currentMinigame = null;
		Game1.exitActiveMenu();
		Game1.player.Halt();
		endBehaviors();
		if (Game1.IsServer)
		{
			Game1.multiplayer.sendServerToClientsMessage("endFest");
		}
		Game1.changeMusicTrack("none");
	}

	public bool checkForCollision(Microsoft.Xna.Framework.Rectangle position, Farmer who)
	{
		Microsoft.Xna.Framework.Rectangle boundingBox = who.GetBoundingBox();
		foreach (NPC actor in actors)
		{
			Microsoft.Xna.Framework.Rectangle boundingBox2 = actor.GetBoundingBox();
			if (boundingBox2.Intersects(position) && !farmer.temporarilyInvincible && farmer.TemporaryPassableTiles.IsEmpty() && !actor.IsInvisible && !boundingBox.Intersects(boundingBox2) && !actor.farmerPassesThrough)
			{
				return true;
			}
		}
		if (Game1.currentLocation.IsOutOfBounds(position))
		{
			TryStartEndFestivalDialogue(who);
			return true;
		}
		foreach (Object prop in props)
		{
			if (prop.GetBoundingBox().Intersects(position))
			{
				return true;
			}
		}
		if (temporaryLocation != null)
		{
			foreach (Object value in temporaryLocation.objects.Values)
			{
				if (value.GetBoundingBox().Intersects(position))
				{
					return true;
				}
			}
		}
		foreach (Prop festivalProp in festivalProps)
		{
			if (festivalProp.isColliding(position))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Show the dialogue to end the current festival when the player tries to leave the location.</summary>
	/// <param name="who">The local player instance.</param>
	/// <returns>Returns whether the dialogue was displayed.</returns>
	public bool TryStartEndFestivalDialogue(Farmer who)
	{
		if (!who.IsLocalPlayer || !isFestival)
		{
			return false;
		}
		who.Halt();
		who.Position = who.lastPosition;
		if (!Game1.IsMultiplayer && Game1.activeClickableMenu == null)
		{
			Game1.activeClickableMenu = new ConfirmationDialog(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1758", FestivalName), forceEndFestival);
		}
		else if (Game1.activeClickableMenu == null)
		{
			Game1.netReady.SetLocalReady("festivalEnd", ready: true);
			Game1.activeClickableMenu = new ReadyCheckDialog("festivalEnd", allowCancel: true, forceEndFestival);
		}
		return true;
	}

	public void answerDialogue(string questionKey, int answerChoice)
	{
		previousAnswerChoice = answerChoice;
		if (questionKey.Contains("fork"))
		{
			int num = Convert.ToInt32(questionKey.Replace("fork", ""));
			if (answerChoice == num)
			{
				specialEventVariable1 = !specialEventVariable1;
			}
		}
		else if (questionKey.Contains("quickQuestion"))
		{
			string obj = eventCommands[Math.Min(eventCommands.Length - 1, CurrentCommand)];
			string[] collection = obj.Substring(obj.IndexOf(' ') + 1).Split("(break)")[1 + answerChoice].Split('\\');
			List<string> list = eventCommands.ToList();
			list.InsertRange(CurrentCommand + 1, collection);
			eventCommands = list.ToArray();
		}
		else
		{
			if (questionKey == null)
			{
				return;
			}
			switch (questionKey.Length)
			{
			case 11:
				switch (questionKey[1])
				{
				case 'h':
					if (questionKey == "shaneCliffs")
					{
						switch (answerChoice)
						{
						case 0:
							eventCommands[currentCommand + 2] = "speak Shane \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1760") + "\"";
							break;
						case 1:
							eventCommands[currentCommand + 2] = "speak Shane \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1761") + "\"";
							break;
						case 2:
							eventCommands[currentCommand + 2] = "speak Shane \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1763") + "\"";
							break;
						case 3:
							eventCommands[currentCommand + 2] = "speak Shane \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1764") + "\"";
							break;
						}
					}
					break;
				case 'i':
					if (questionKey == "fishingGame" && answerChoice == 0)
					{
						if (Game1.player.Money >= 50)
						{
							Game1.globalFadeToBlack(FishingGame.startMe, 0.01f);
							Game1.player.Money -= 50;
						}
						else
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1780"));
						}
					}
					break;
				case 'e':
					if (questionKey == "secretSanta" && answerChoice == 0)
					{
						Game1.activeClickableMenu = new ItemGrabMenu(null, reverseGrab: true, showReceivingMenu: false, Utility.highlightSantaObjects, chooseSecretSantaGift, Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1788", secretSantaRecipient.displayName), null, snapToBottom: false, canBeExitedWithKey: false, playRightClickSound: true, allowRightClick: true, showOrganizeButton: false, 0, null, -1, this);
					}
					break;
				case 'f':
				case 'g':
					break;
				}
				break;
			case 13:
				switch (questionKey[0])
				{
				case 'h':
					if (questionKey == "haleyDarkRoom")
					{
						switch (answerChoice)
						{
						case 0:
							specialEventVariable1 = true;
							eventCommands[currentCommand + 1] = "fork decorate";
							break;
						case 1:
							specialEventVariable1 = true;
							eventCommands[currentCommand + 1] = "fork leave";
							break;
						case 2:
							break;
						}
					}
					break;
				case 'S':
					if (questionKey == "StarTokenShop" && answerChoice == 0)
					{
						Game1.activeClickableMenu = new NumberSelectionMenu(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1774"), buyStarTokens, 50, 0, 999);
					}
					break;
				case 'f':
					if (questionKey == "fortuneTeller" && answerChoice == 0)
					{
						Game1.globalFadeToBlack(readFortune);
						Game1.player.Money -= 100;
						Game1.player.mailReceived.Add("fortuneTeller" + Game1.year);
					}
					break;
				case 's':
					if (!(questionKey == "slingshotGame"))
					{
						if (questionKey == "starTokenShop" && answerChoice == 0 && Utility.TryOpenShopMenu("Festival_StardewValleyFair_StarTokens", temporaryLocation, null, null, forceOpen: false, playOpenSound: false) && Game1.activeClickableMenu is ShopMenu shopMenu)
						{
							if (shopMenu.IsOutOfStock())
							{
								shopMenu.exitThisMenuNoSound();
								Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1785")));
							}
							else
							{
								shopMenu.PlayOpenSound();
							}
						}
					}
					else if (answerChoice == 0)
					{
						if (Game1.player.Money >= 50)
						{
							Game1.globalFadeToBlack(TargetGame.startMe, 0.01f);
							Game1.player.Money -= 50;
						}
						else
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1780"));
						}
					}
					break;
				}
				break;
			case 8:
				switch (questionKey[0])
				{
				case 'b':
					if (questionKey == "bandFork")
					{
						switch (answerChoice)
						{
						case 76:
							specialEventVariable1 = true;
							eventCommands[currentCommand + 1] = "fork poppy";
							break;
						case 77:
							specialEventVariable1 = true;
							eventCommands[currentCommand + 1] = "fork heavy";
							break;
						case 78:
							specialEventVariable1 = true;
							eventCommands[currentCommand + 1] = "fork techno";
							break;
						case 79:
							specialEventVariable1 = true;
							eventCommands[currentCommand + 1] = "fork honkytonk";
							break;
						}
					}
					break;
				case 'w':
					if (questionKey == "wheelBet")
					{
						specialEventVariable2 = answerChoice == 1;
						if (answerChoice != 2)
						{
							Game1.activeClickableMenu = new NumberSelectionMenu(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1776"), betStarTokens, -1, 1, Game1.player.festivalScore, Math.Min(1, Game1.player.festivalScore));
						}
					}
					break;
				}
				break;
			case 20:
				if (questionKey == "spirits_eve_shortcut" && answerChoice == 0)
				{
					Game1.player.freezePause = 2000;
					Game1.globalFadeToBlack(delegate
					{
						Game1.player.Position = new Vector2(32f, 49f) * 64f;
						Game1.player.faceDirection(2);
						Game1.playSound("stairsdown");
						Game1.globalFadeToClear();
					});
				}
				break;
			case 9:
				if (questionKey == "shaneLoan")
				{
					if (answerChoice != 0)
					{
						_ = 1;
						break;
					}
					specialEventVariable1 = true;
					eventCommands[currentCommand + 1] = "fork giveShaneLoan";
					Game1.player.Money -= 3000;
				}
				break;
			case 15:
				if (questionKey == "chooseCharacter")
				{
					switch (answerChoice)
					{
					case 0:
						specialEventVariable1 = true;
						eventCommands[currentCommand + 1] = "fork warrior";
						break;
					case 1:
						specialEventVariable1 = true;
						eventCommands[currentCommand + 1] = "fork healer";
						break;
					case 2:
						break;
					}
				}
				break;
			case 4:
				if (questionKey == "cave")
				{
					Game1.dedicatedServer.DoHostAction("ChooseCave", answerChoice);
				}
				break;
			case 3:
				if (questionKey == "pet")
				{
					if (answerChoice == 0)
					{
						string title = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1236");
						Game1.activeClickableMenu = new NamingMenu(defaultName: (!Game1.player.IsMale) ? Game1.content.LoadString((Game1.player.whichPetType == "Dog") ? "Strings\\StringsFromCSFiles:Event.cs.1797" : "Strings\\StringsFromCSFiles:Event.cs.1796") : Game1.content.LoadString(Game1.player.catPerson ? "Strings\\StringsFromCSFiles:Event.cs.1794" : "Strings\\StringsFromCSFiles:Event.cs.1795"), b: namePet, title: title);
						break;
					}
					Game1.player.team.RequestSetMail(PlayerActionTarget.Host, "rejectedPet", MailType.Received, add: true);
					eventCommands = new string[2];
					eventCommands[1] = "end";
					eventCommands[0] = "speak Marnie \"" + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1798") + "\"";
					currentCommand = 0;
					eventSwitched = true;
					specialEventVariable1 = true;
				}
				break;
			}
		}
	}

	internal static void hostActionChooseCave(Farmer who, BinaryReader reader)
	{
		if (reader.ReadInt32() == 0)
		{
			Game1.MasterPlayer.caveChoice.Value = 2;
			Game1.RequireLocation<FarmCave>("FarmCave").setUpMushroomHouse();
		}
		else
		{
			Game1.MasterPlayer.caveChoice.Value = 1;
		}
	}

	internal static void hostActionNamePet(Farmer who, BinaryReader reader)
	{
		string name = reader.ReadString();
		Pet pet = new Pet(68, 13, Game1.player.whichPetBreed, Game1.player.whichPetType);
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
	}

	private void namePet(string name)
	{
		gotPet = true;
		Game1.dedicatedServer.DoHostAction("NamePet", name);
		Game1.exitActiveMenu();
		CurrentCommand++;
	}

	public void chooseSecretSantaGift(Item i, Farmer who)
	{
		if (i == null)
		{
			return;
		}
		Object obj = i as Object;
		if (obj != null)
		{
			if (obj.Stack > 1)
			{
				obj.Stack--;
				who.addItemToInventory(obj);
			}
			Game1.exitActiveMenu();
			NPC recipient = getActorByName(secretSantaRecipient.Name);
			recipient.faceTowardFarmerForPeriod(15000, 5, faceAway: false, who);
			recipient.receiveGift(obj, who, updateGiftLimitInfo: false, 5f, showResponse: false);
			recipient.CurrentDialogue.Clear();
			string article = Lexicon.getProperArticleForWord(obj.DisplayName);
			recipient.CurrentDialogue.Push(recipient.TryGetDialogue("WinterStar_ReceiveGift_" + obj.QualifiedItemId, obj.DisplayName, article) ?? (from tag in obj.GetContextTags()
				select recipient.TryGetDialogue("WinterStar_ReceiveGift_" + tag, obj.DisplayName, article)).FirstOrDefault((Dialogue p) => p != null) ?? recipient.TryGetDialogue("WinterStar_ReceiveGift", obj.DisplayName, article) ?? Dialogue.FromTranslation(recipient, "Strings\\StringsFromCSFiles:Event.cs.1801", obj.DisplayName, article));
			Game1.drawDialogue(recipient);
			secretSantaRecipient = null;
			startSecretSantaAfterDialogue = true;
			who.Halt();
			who.completelyStopAnimatingOrDoingAction();
			who.faceGeneralDirection(recipient.Position, 0, opposite: false, useTileCalculations: false);
		}
		else
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1803"));
		}
	}

	public void perfectFishing()
	{
		if (isFestival && Game1.currentMinigame is FishingGame fishingGame && isSpecificFestival("fall16"))
		{
			fishingGame.perfections++;
		}
	}

	public void caughtFish(string itemId, int size, Farmer who)
	{
		if (itemId == null || !isFestival)
		{
			return;
		}
		if (Game1.currentMinigame is FishingGame fishingGame && isSpecificFestival("fall16"))
		{
			fishingGame.score += ((size <= 0) ? 1 : (size + 5));
			if (size > 0)
			{
				fishingGame.fishCaught++;
			}
			Game1.player.FarmerSprite.PauseForSingleAnimation = false;
			Game1.player.FarmerSprite.StopAnimation();
		}
		else if (isSpecificFestival("winter8"))
		{
			if (size > 0 && who.TilePoint.X < 79 && who.TilePoint.Y < 43)
			{
				who.festivalScore++;
				Game1.playSound("newArtifact");
			}
			who.forceCanMove();
			if (previousFacingDirection != -1)
			{
				who.faceDirection(previousFacingDirection);
			}
		}
	}

	public void readFortune()
	{
		Game1.globalFade = true;
		Game1.fadeToBlackAlpha = 1f;
		NPC topRomanticInterest = Utility.getTopRomanticInterest(Game1.player);
		NPC topNonRomanticInterest = Utility.getTopNonRomanticInterest(Game1.player);
		int highestSkill = Utility.getHighestSkill(Game1.player);
		string[] array = new string[5];
		if (topNonRomanticInterest != null && Game1.player.getFriendshipLevelForNPC(topNonRomanticInterest.Name) > 100)
		{
			if (Utility.getNumberOfFriendsWithinThisRange(Game1.player, Game1.player.getFriendshipLevelForNPC(topNonRomanticInterest.Name) - 100, Game1.player.getFriendshipLevelForNPC(topNonRomanticInterest.Name)) > 3 && Game1.random.NextBool())
			{
				array[0] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1810");
			}
			else
			{
				switch (Game1.random.Next(4))
				{
				case 0:
					array[0] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1811", topNonRomanticInterest.displayName);
					break;
				case 1:
					array[0] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1813", topNonRomanticInterest.displayName) + ((topNonRomanticInterest.Gender == Gender.Male) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1815") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1816"));
					break;
				case 2:
					array[0] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1818", topNonRomanticInterest.displayName);
					break;
				case 3:
					array[0] = ((topNonRomanticInterest.Gender == Gender.Male) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1820") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1821")) + Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1823", topNonRomanticInterest.displayName);
					break;
				}
			}
		}
		else
		{
			array[0] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1825");
		}
		if (topRomanticInterest != null && Game1.player.getFriendshipLevelForNPC(topRomanticInterest.Name) > 250)
		{
			if (Utility.getNumberOfFriendsWithinThisRange(Game1.player, Game1.player.getFriendshipLevelForNPC(topRomanticInterest.Name) - 100, Game1.player.getFriendshipLevelForNPC(topRomanticInterest.Name), romanceOnly: true) > 2)
			{
				array[1] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1826");
			}
			else
			{
				switch (Game1.random.Next(4))
				{
				case 0:
					array[1] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1827", topRomanticInterest.displayName);
					break;
				case 1:
					array[1] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1829", topRomanticInterest.displayName);
					break;
				case 2:
					array[1] = ((topRomanticInterest.Gender != Gender.Male) ? ((topRomanticInterest.SocialAnxiety == 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1833") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1834")) : ((topRomanticInterest.SocialAnxiety == 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1831") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1832"))) + " " + ((topRomanticInterest.Gender == Gender.Male) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1837", topRomanticInterest.displayName[0]) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1838", topRomanticInterest.displayName[0]));
					break;
				case 3:
					array[1] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1843", topRomanticInterest.displayName);
					break;
				}
			}
		}
		else
		{
			array[1] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1845");
		}
		switch (highestSkill)
		{
		case 0:
			array[2] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1846");
			break;
		case 3:
			array[2] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1847");
			break;
		case 4:
			array[2] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1848");
			break;
		case 1:
			array[2] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1849");
			break;
		case 2:
			array[2] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1850");
			break;
		case 5:
			array[2] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1851");
			break;
		}
		array[3] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1852");
		array[4] = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1853");
		Game1.multipleDialogues(array);
		Game1.afterDialogues = fadeClearAndviewportUnfreeze;
		Game1.viewportFreeze = true;
		Game1.viewport.X = -9999;
	}

	public void fadeClearAndviewportUnfreeze()
	{
		Game1.fadeClear();
		Game1.viewportFreeze = false;
	}

	public void betStarTokens(int value, int price, Farmer who)
	{
		if (value <= who.festivalScore)
		{
			Game1.playSound("smallSelect");
			Game1.activeClickableMenu = new WheelSpinGame(value);
		}
	}

	public void buyStarTokens(int value, int price, Farmer who)
	{
		if (value > 0 && value * price <= who.Money)
		{
			who.Money -= price * value;
			who.festivalScore += value;
			Game1.playSound("purchase");
			Game1.exitActiveMenu();
		}
	}

	public void clickToAddItemToLuauSoup(Item i, Farmer who)
	{
		addItemToLuauSoup(i, who);
	}

	public void setUpAdvancedMove(string[] args, NPCController.endBehavior endBehavior = null)
	{
		if (!ArgUtility.TryGet(args, 1, out var value, out var error, allowBlank: false, "string actorName") || !ArgUtility.TryGetBool(args, 2, out var value2, out error, "bool loop"))
		{
			LogCommandError(args, error);
			return;
		}
		List<Vector2> list = new List<Vector2>();
		for (int i = 3; i < args.Length; i += 2)
		{
			if (ArgUtility.TryGetVector2(args, i, out var value3, out error, integerOnly: true, "Vector2 tile"))
			{
				list.Add(value3);
			}
			else
			{
				LogCommandError(args, error);
			}
		}
		if (npcControllers == null)
		{
			npcControllers = new List<NPCController>();
		}
		if (IsFarmerActorId(value, out var farmerNumber))
		{
			Farmer farmerActor = GetFarmerActor(farmerNumber);
			if (farmerActor != null)
			{
				npcControllers.Add(new NPCController(farmerActor, list, value2, endBehavior));
			}
		}
		else
		{
			NPC actorByName = getActorByName(value, legacyReplaceUnderscores: true);
			if (actorByName != null)
			{
				npcControllers.Add(new NPCController(actorByName, list, value2, endBehavior));
			}
		}
	}

	public static bool IsItemMayorShorts(Item i)
	{
		if (!(i?.QualifiedItemId == "(O)789"))
		{
			return i?.QualifiedItemId == "(O)71";
		}
		return true;
	}

	public void addItemToLuauSoup(Item i, Farmer who)
	{
		if (i == null)
		{
			return;
		}
		who.team.luauIngredients.Add(i.getOne());
		if (who.IsLocalPlayer)
		{
			specialEventVariable2 = true;
			bool flag = IsItemMayorShorts(i);
			if (i != null && i.Stack > 1 && !flag)
			{
				i.Stack--;
				who.addItemToInventory(i);
			}
			else if (flag)
			{
				who.addItemToInventory(i);
			}
			Game1.exitActiveMenu();
			Game1.playSound("dropItemInWater");
			if (i != null)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1857", i.DisplayName));
			}
			string text = "";
			switch (i.Quality)
			{
			case 1:
				text = " ([51])";
				break;
			case 2:
				text = " ([52])";
				break;
			case 4:
				text = " ([53])";
				break;
			}
			if (!flag)
			{
				Game1.multiplayer.globalChatInfoMessage("LuauSoup", Game1.player.Name, TokenStringBuilder.ItemNameFor(i) + text);
			}
		}
	}

	private void governorTaste()
	{
		int num = 5;
		foreach (Item luauIngredient in Game1.player.team.luauIngredients)
		{
			Object obj = luauIngredient as Object;
			int num2 = 5;
			if (IsItemMayorShorts(obj))
			{
				num = 6;
				break;
			}
			if ((obj.Quality >= 2 && obj.price.Value >= 160) || (obj.Quality == 1 && obj.price.Value >= 300 && obj.edibility.Value > 10))
			{
				num2 = 4;
				Utility.improveFriendshipWithEveryoneInRegion(Game1.player, 120, "Town");
			}
			else if (obj.edibility.Value >= 20 || obj.price.Value >= 100 || (obj.price.Value >= 70 && obj.Quality >= 1))
			{
				num2 = 3;
				Utility.improveFriendshipWithEveryoneInRegion(Game1.player, 60, "Town");
			}
			else if ((obj.price.Value > 20 && obj.edibility.Value >= 10) || (obj.price.Value >= 40 && obj.edibility.Value >= 5))
			{
				num2 = 2;
			}
			else if (obj.edibility.Value >= 0)
			{
				num2 = 1;
				Utility.improveFriendshipWithEveryoneInRegion(Game1.player, -50, "Town");
			}
			if (obj.edibility.Value > -300 && obj.edibility.Value < 0)
			{
				num2 = 0;
				Utility.improveFriendshipWithEveryoneInRegion(Game1.player, -100, "Town");
			}
			if (num2 < num)
			{
				num = num2;
			}
		}
		int num3 = Game1.numberOfPlayers() - (Game1.HasDedicatedHost ? 1 : 0);
		if (num != 6 && Game1.player.team.luauIngredients.Count < num3)
		{
			num = 5;
		}
		eventCommands[CurrentCommand + 1] = "switchEvent governorReaction" + num;
		if (num == 4)
		{
			Game1.getAchievement(38);
		}
	}

	private void eggHuntWinner()
	{
		int num = (Game1.numberOfPlayers() - (Game1.HasDedicatedHost ? 1 : 0)) switch
		{
			1 => 9, 
			2 => 6, 
			3 => 5, 
			_ => 4, 
		};
		List<Farmer> list = new List<Farmer>();
		int festivalScore = Game1.player.festivalScore;
		foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
		{
			if (onlineFarmer.festivalScore > festivalScore)
			{
				festivalScore = onlineFarmer.festivalScore;
			}
		}
		foreach (Farmer onlineFarmer2 in Game1.getOnlineFarmers())
		{
			if (onlineFarmer2.festivalScore == festivalScore)
			{
				list.Add(onlineFarmer2);
				festivalWinners.Add(onlineFarmer2.UniqueMultiplayerID);
			}
		}
		string dialogueText = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1862");
		if (festivalScore >= num)
		{
			foreach (Farmer item in list)
			{
				item.autoGenerateActiveDialogueEvent("wonEggHunt");
			}
			if (list.Count == 1)
			{
				dialogueText = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.es) ? ("¡" + list[0].displayName + "!") : (list[0].displayName + "!"));
			}
			else
			{
				dialogueText = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1864");
				for (int i = 0; i < list.Count; i++)
				{
					if (i == list.Count - 1)
					{
						dialogueText += Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1865");
					}
					dialogueText = dialogueText + " " + list[i].displayName;
					if (i < list.Count - 1)
					{
						dialogueText += ",";
					}
				}
				dialogueText += "!";
			}
			specialEventVariable1 = false;
		}
		else
		{
			specialEventVariable1 = true;
		}
		NPC actorByName = getActorByName("Lewis");
		actorByName.CurrentDialogue.Push(new Dialogue(actorByName, null, dialogueText));
		Game1.drawDialogue(actorByName);
	}

	private void iceFishingWinner()
	{
		int num = 5;
		iceFishWinners = new List<Farmer>();
		int festivalScore = Game1.player.festivalScore;
		for (int i = 1; i <= Game1.numberOfPlayers(); i++)
		{
			Farmer farmerActor = GetFarmerActor(i);
			if (farmerActor != null && farmerActor.festivalScore > festivalScore)
			{
				festivalScore = farmerActor.festivalScore;
			}
		}
		for (int j = 1; j <= Game1.numberOfPlayers(); j++)
		{
			Farmer farmerActor2 = GetFarmerActor(j);
			if (farmerActor2 != null && farmerActor2.festivalScore == festivalScore)
			{
				iceFishWinners.Add(farmerActor2);
				festivalWinners.Add(farmerActor2.UniqueMultiplayerID);
			}
		}
		string dialogueText = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1871");
		if (festivalScore >= num)
		{
			foreach (Farmer iceFishWinner in iceFishWinners)
			{
				iceFishWinner.autoGenerateActiveDialogueEvent("wonIceFishing");
			}
			if (iceFishWinners.Count == 1)
			{
				dialogueText = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1872", iceFishWinners[0].displayName, iceFishWinners[0].festivalScore);
			}
			else
			{
				dialogueText = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1864");
				for (int k = 0; k < iceFishWinners.Count; k++)
				{
					if (k == iceFishWinners.Count - 1)
					{
						dialogueText += Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1865");
					}
					dialogueText = dialogueText + " " + iceFishWinners[k].displayName;
					if (k < iceFishWinners.Count - 1)
					{
						dialogueText += ",";
					}
				}
				dialogueText += "!";
			}
			specialEventVariable1 = false;
		}
		else
		{
			specialEventVariable1 = true;
		}
		NPC actorByName = getActorByName("Lewis");
		actorByName.CurrentDialogue.Push(new Dialogue(actorByName, null, dialogueText));
		Game1.drawDialogue(actorByName);
	}

	private void iceFishingWinnerMP()
	{
		specialEventVariable1 = !iceFishWinners.Contains(Game1.player);
	}

	public void popBalloons(int x, int y)
	{
		if ((!id.Equals("191393") && !id.Equals("502261")) || aboveMapSprites == null)
		{
			return;
		}
		List<int> idsToRemove = new List<int>();
		for (int num = aboveMapSprites.Count - 1; num >= 0; num--)
		{
			TemporaryAnimatedSprite temporaryAnimatedSprite = aboveMapSprites[num];
			int width = temporaryAnimatedSprite.sourceRect.Width * 4;
			int height = temporaryAnimatedSprite.sourceRect.Height * 4;
			Microsoft.Xna.Framework.Rectangle r = new Microsoft.Xna.Framework.Rectangle((int)temporaryAnimatedSprite.Position.X, (int)temporaryAnimatedSprite.Position.Y, width, height);
			if (r.Contains(x, y))
			{
				idsToRemove.Add(temporaryAnimatedSprite.id);
				if (temporaryAnimatedSprite.sourceRect.Height <= 16)
				{
					for (int i = 0; i < 3; i++)
					{
						aboveMapSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(280 + Game1.random.Choose(8, 0), 1954, 8, 8), 1000f, 1, 99, Utility.getRandomPositionInThisRectangle(r, Game1.random), flicker: false, flipped: false, 1f, 0f, temporaryAnimatedSprite.color, 4f, 0f, 0f, (float)Game1.random.Next(-10, 11) / 100f)
						{
							motion = new Vector2(Game1.random.Next(-4, 5), -8f + (float)Game1.random.Next(-10, 1) / 100f),
							acceleration = new Vector2(0f, 0.3f),
							local = true
						});
					}
				}
			}
		}
		aboveMapSprites.RemoveWhere((TemporaryAnimatedSprite sprite) => sprite.id == 9988 || idsToRemove.Contains(sprite.id));
		if (idsToRemove.Count > 0)
		{
			int_useMeForAnything++;
			aboveMapSprites.Add(new TemporaryAnimatedSprite(null, Microsoft.Xna.Framework.Rectangle.Empty, new Vector2(16f, 16f), flipped: false, 0f, Color.White)
			{
				text = (int_useMeForAnything.ToString() ?? ""),
				layerDepth = 1f,
				animationLength = 1,
				totalNumberOfLoops = 10,
				interval = 300f,
				scale = 2f,
				local = true,
				id = 9988
			});
			Game1.playSound("coin");
		}
	}

	/// <summary>Auto-generate a default light source ID for an event.</summary>
	/// <param name="suffix">A suffix which distinguishes the specific light source for this event.</param>
	public virtual string GenerateLightSourceId(string suffix)
	{
		return $"{"Event"}_{id}_{suffix}";
	}
}
