using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewValley.Delegates;
using StardewValley.GameData;
using StardewValley.Network.NetEvents;
using StardewValley.SpecialOrders;

namespace StardewValley.Triggers;

/// <summary>Manages trigger actions defined in the <c>Data/TriggerActions</c> asset, which perform actions when their conditions are met.</summary>
public static class TriggerActionManager
{
	/// <summary>The low-level trigger actions defined by the base game. Most code should use <see cref="T:StardewValley.Triggers.TriggerActionManager" /> methods instead.</summary>
	/// <remarks>Every method within this class is an action whose name matches the method name. All actions must be static, public, and match <see cref="T:StardewValley.Delegates.TriggerActionDelegate" />.</remarks>
	public static class DefaultActions
	{
		/// <summary>An action which does nothing.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool Null(string[] args, TriggerActionContext context, out string error)
		{
			error = null;
			return true;
		}

		/// <summary>Perform an action if a game state query matches, with an optional fallback action.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool If(string[] args, TriggerActionContext context, out string error)
		{
			int num = -1;
			for (int i = 1; i < args.Length; i++)
			{
				if (args[i] == "##")
				{
					num = i + 1;
					break;
				}
			}
			if (num == -1 || num == args.Length)
			{
				return InvalidFormatError(out error);
			}
			int num2 = -1;
			for (int j = num + 1; j < args.Length; j++)
			{
				if (args[j] == "##")
				{
					num2 = j + 1;
					break;
				}
			}
			if (num2 == args.Length - 1)
			{
				return InvalidFormatError(out error);
			}
			Exception exception;
			if (GameStateQuery.CheckConditions(ArgUtility.UnsplitQuoteAware(args, ' ', 1, num - 1 - 1)))
			{
				int count = ((num2 > -1) ? (num2 - num - 1) : int.MaxValue);
				string text = ArgUtility.UnsplitQuoteAware(args, ' ', num, count);
				if (!TryRunAction(text, out error, out exception))
				{
					error = "failed applying if-true action '" + text + "': " + error;
					return false;
				}
			}
			else if (num2 > -1)
			{
				string text2 = ArgUtility.UnsplitQuoteAware(args, ' ', num2);
				if (!TryRunAction(text2, out error, out exception))
				{
					error = "failed applying if-false action '" + text2 + "': " + error;
					return false;
				}
			}
			error = null;
			return true;
			static bool InvalidFormatError(out string outError)
			{
				outError = "invalid format: expected a string in the form 'If <game state query> ## <do if true>' or 'If <game state query> ## <do if true> ## <do if false>'";
				return false;
			}
		}

		/// <summary>Apply a buff to the current player.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool AddBuff(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: true, "string buffId") || !ArgUtility.TryGetOptionalInt(args, 2, out var value2, out error, -1, "int duration"))
			{
				return false;
			}
			Buff buff = new Buff(value, null, null, value2);
			Game1.player.applyBuff(buff);
			return true;
		}

		/// <summary>Remove a buff from the current player.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool RemoveBuff(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: true, "string buffId"))
			{
				return false;
			}
			Game1.player.buffs.Remove(value);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool AddMail(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGetEnum<PlayerActionTarget>(args, 1, out var value, out error, "PlayerActionTarget playerTarget") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string mailId") || !ArgUtility.TryGetOptionalEnum(args, 3, out var value3, out error, MailType.Tomorrow, "MailType mailType"))
			{
				return false;
			}
			Game1.player.team.RequestSetMail(value, value2, value3, add: true);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool RemoveMail(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGetEnum<PlayerActionTarget>(args, 1, out var value, out error, "PlayerActionTarget playerTarget") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string mailId") || !ArgUtility.TryGetOptionalEnum(args, 3, out var value3, out error, MailType.All, "MailType mailType"))
			{
				return false;
			}
			Game1.player.team.RequestSetMail(value, value2, value3, add: false);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool AddQuest(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: true, "string questId"))
			{
				return false;
			}
			Game1.player.addQuest(value);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool RemoveQuest(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: true, "string questId"))
			{
				return false;
			}
			Game1.player.removeQuest(value);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool AddSpecialOrder(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: true, "string orderId"))
			{
				return false;
			}
			Game1.player.team.AddSpecialOrder(value);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool RemoveSpecialOrder(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var orderId, out error, allowBlank: true, "string orderId"))
			{
				return false;
			}
			Game1.player.team.specialOrders.RemoveWhere((SpecialOrder order) => order.questKey.Value == orderId);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool AddItem(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: true, "string itemId") || !ArgUtility.TryGetOptionalInt(args, 2, out var value2, out error, 1, "int count") || !ArgUtility.TryGetOptionalInt(args, 3, out var value3, out error, 0, "int quality"))
			{
				return false;
			}
			Item item = ItemRegistry.Create(value, value2, value3);
			if (item != null)
			{
				Game1.player.addItemByMenuIfNecessary(item);
			}
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool RemoveItem(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: true, "string itemId") || !ArgUtility.TryGetOptionalInt(args, 2, out var value2, out error, 1, "int count"))
			{
				return false;
			}
			Game1.player.removeFirstOfThisItemFromInventory(value, value2);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool AddMoney(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGetInt(args, 1, out var value, out error, "int amount"))
			{
				return false;
			}
			Game1.player.Money += value;
			if (Game1.player.Money < 0)
			{
				Game1.player.Money = 0;
			}
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool AddFriendshipPoints(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: true, "string npcName") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int points"))
			{
				return false;
			}
			NPC characterFromName = Game1.getCharacterFromName(value);
			if (characterFromName == null)
			{
				error = "no NPC found with name '" + value + "'";
				return false;
			}
			Game1.player.changeFriendship(value2, characterFromName);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool AddConversationTopic(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: true, "string topicId") || !ArgUtility.TryGetOptionalInt(args, 2, out var value2, out error, 4, "int daysDuration"))
			{
				return false;
			}
			Game1.player.activeDialogueEvents[value] = value2;
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool RemoveConversationTopic(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: true, "string topicId"))
			{
				return false;
			}
			Game1.player.activeDialogueEvents.Remove(value);
			return true;
		}

		/// <summary>Increment or decrement a stats value for the current player.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool IncrementStat(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: false, "string statKey") || !ArgUtility.TryGetOptionalInt(args, 2, out var value2, out error, 1, "int amount"))
			{
				return false;
			}
			Game1.player.stats.Increment(value, value2);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool MarkActionApplied(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGetEnum<PlayerActionTarget>(args, 1, out var value, out error, "PlayerActionTarget playerTarget") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: false, "string actionId") || !ArgUtility.TryGetOptionalBool(args, 3, out var value3, out error, defaultValue: true, "bool applied"))
			{
				return false;
			}
			Game1.player.team.RequestSetSimpleFlag(SimpleFlagType.ActionApplied, value, value2, value3);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool MarkCookingRecipeKnown(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGetEnum<PlayerActionTarget>(args, 1, out var value, out error, "PlayerActionTarget playerTarget") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string recipeKey") || !ArgUtility.TryGetOptionalBool(args, 3, out var value3, out error, defaultValue: true, "bool learned"))
			{
				return false;
			}
			Game1.player.team.RequestSetSimpleFlag(SimpleFlagType.CookingRecipeKnown, value, value2, value3);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool MarkCraftingRecipeKnown(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGetEnum<PlayerActionTarget>(args, 1, out var value, out error, "PlayerActionTarget playerTarget") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: true, "string recipeKey") || !ArgUtility.TryGetOptionalBool(args, 3, out var value3, out error, defaultValue: true, "bool learned"))
			{
				return false;
			}
			Game1.player.team.RequestSetSimpleFlag(SimpleFlagType.CraftingRecipeKnown, value, value2, value3);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool MarkEventSeen(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGetEnum<PlayerActionTarget>(args, 1, out var value, out error, "PlayerActionTarget playerTarget") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: false, "string eventId") || !ArgUtility.TryGetOptionalBool(args, 3, out var value3, out error, defaultValue: true, "bool seen"))
			{
				return false;
			}
			Game1.player.team.RequestSetSimpleFlag(SimpleFlagType.EventSeen, value, value2, value3);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool MarkQuestionAnswered(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGetEnum<PlayerActionTarget>(args, 1, out var value, out error, "PlayerActionTarget playerTarget") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: false, "string questionId") || !ArgUtility.TryGetOptionalBool(args, 3, out var value3, out error, defaultValue: true, "bool answered"))
			{
				return false;
			}
			Game1.player.team.RequestSetSimpleFlag(SimpleFlagType.DialogueAnswerSelected, value, value2, value3);
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool MarkSongHeard(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGetEnum<PlayerActionTarget>(args, 1, out var value, out error, "PlayerActionTarget playerTarget") || !ArgUtility.TryGet(args, 2, out var value2, out error, allowBlank: false, "string trackId") || !ArgUtility.TryGetOptionalBool(args, 3, out var value3, out error, defaultValue: true, "bool heard"))
			{
				return false;
			}
			Game1.player.team.RequestSetSimpleFlag(SimpleFlagType.SongHeard, value, value2, value3);
			return true;
		}

		/// <summary>Remove all temporary animated sprites in the current location.</summary>
		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool RemoveTemporaryAnimatedSprites(string[] args, TriggerActionContext context, out string error)
		{
			Game1.currentLocation?.TemporarySprites.Clear();
			error = null;
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool SetNpcInvisible(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: false, "string npcName") || !ArgUtility.TryGetInt(args, 2, out var value2, out error, "int daysDuration"))
			{
				return false;
			}
			NPC characterFromName = Game1.getCharacterFromName(value);
			if (characterFromName == null)
			{
				error = "no NPC found with name '" + value + "'";
				return false;
			}
			characterFromName.IsInvisible = true;
			characterFromName.daysUntilNotInvisible = value2;
			return true;
		}

		/// <inheritdoc cref="T:StardewValley.Delegates.TriggerActionDelegate" />
		public static bool SetNpcVisible(string[] args, TriggerActionContext context, out string error)
		{
			if (!ArgUtility.TryGet(args, 1, out var value, out error, allowBlank: false, "string npcName"))
			{
				return false;
			}
			NPC characterFromName = Game1.getCharacterFromName(value);
			if (characterFromName == null)
			{
				error = "no NPC found with name '" + value + "'";
				return false;
			}
			characterFromName.IsInvisible = false;
			characterFromName.daysUntilNotInvisible = 0;
			return true;
		}
	}

	/// <summary>The trigger type raised overnight immediately before the game changes the date, sets up the new day, and saves.</summary>
	public const string trigger_dayEnding = "DayEnding";

	/// <summary>The trigger type raised when the player starts a day, after either sleeping or loading.</summary>
	public const string trigger_dayStarted = "DayStarted";

	/// <summary>The trigger type raised when the player arrives in a new location.</summary>
	public const string trigger_locationChanged = "LocationChanged";

	/// <summary>The trigger type used for actions that are triggered elsewhere than <c>Data/TriggerActions</c>.</summary>
	public const string trigger_manual = "Manual";

	/// <summary>The trigger types that can be used in the <see cref="F:StardewValley.GameData.TriggerActionData.Trigger" /> field.</summary>
	private static readonly HashSet<string> ValidTriggerTypes;

	/// <summary>The action handlers indexed by name.</summary>
	/// <remarks>Action names are case-insensitive.</remarks>
	private static readonly Dictionary<string, TriggerActionDelegate> ActionHandlers;

	/// <summary>A cached lookup of actions by trigger name.</summary>
	private static readonly Dictionary<string, List<CachedTriggerAction>> ActionsByTrigger;

	/// <summary>A cached lookup of parsed action strings.</summary>
	private static readonly Dictionary<string, CachedAction> ActionCache;

	/// <summary>A parsed action which does nothing.</summary>
	private static readonly CachedAction NullAction;

	/// <summary>The trigger action context used for a default manual option.</summary>
	private static readonly TriggerActionContext EmptyManualContext;

	/// <summary>Register a trigger type.</summary>
	/// <param name="name">The trigger key. This is case-insensitive.</param>
	public static void RegisterTrigger(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			Game1.log.Error("Can't register an empty trigger type for Data/Triggers.");
			return;
		}
		ValidTriggerTypes.Add(name);
		Game1.log.Verbose("Registered trigger type for Data/Triggers: " + name + ".");
	}

	/// <summary>Register an action handler.</summary>
	/// <param name="name">The action name. This is case-insensitive.</param>
	/// <param name="action">The handler to call when the action should apply.</param>
	public static void RegisterAction(string name, TriggerActionDelegate action)
	{
		if (ActionHandlers.TryAdd(name, action))
		{
			Game1.log.Verbose("Registered trigger action handler '" + name + "'.");
		}
		else
		{
			Game1.log.Warn("Can't add trigger action handler '" + name + "' because that name is already registered.");
		}
	}

	/// <summary>Run all actions for a given trigger key.</summary>
	/// <param name="trigger">The trigger key to raise.</param>
	/// <param name="triggerArgs">The contextual arguments provided with the trigger, if applicable. For example, an 'item received' trigger might provide the item instance and index.</param>
	/// <param name="location">The location for which to check action conditions, or <c>null</c> to use the current location.</param>
	/// <param name="player">The player for which to check action conditions, or <c>null</c> to use the current player.</param>
	/// <param name="targetItem">The target item (e.g. machine output or tree fruit) for which to check action conditions, or <c>null</c> if not applicable.</param>
	/// <param name="inputItem">The input item (e.g. machine input) for which to check action conditions, or <c>null</c> if not applicable.</param>
	public static void Raise(string trigger, object[] triggerArgs = null, GameLocation location = null, Farmer player = null, Item targetItem = null, Item inputItem = null)
	{
		if (ValidTriggerTypes.TryGetValue(trigger, out var actualValue))
		{
			trigger = actualValue;
			triggerArgs = triggerArgs ?? LegacyShims.EmptyArray<object>();
			{
				foreach (CachedTriggerAction item in GetActionsForTrigger(trigger))
				{
					TryRunActions(item, trigger, triggerArgs, location, player, targetItem, inputItem);
				}
				return;
			}
		}
		Game1.log.Error("Can't raise unknown trigger type '" + trigger + "'.");
	}

	/// <summary>Parse a raw action value.</summary>
	/// <param name="action">The action string to parse.</param>
	/// <remarks>This is a low-level method. Most code should use <see cref="M:StardewValley.Triggers.TriggerActionManager.TryRunAction(System.String,System.String@,System.Exception@)" /> instead.</remarks>
	public static CachedAction ParseAction(string action)
	{
		if (string.IsNullOrWhiteSpace(action))
		{
			return NullAction;
		}
		action = action.Trim();
		if (!ActionCache.TryGetValue(action, out var value))
		{
			string[] array = ArgUtility.SplitBySpaceQuoteAware(action);
			string text = array[0];
			value = (TryGetActionHandler(text, out var handler) ? new CachedAction(array, handler, null, isNullHandler: false) : new CachedAction(array, NullAction.Handler, $"unknown action '{text}' ignored (expected one of '{string.Join("', '", ActionHandlers.Keys.OrderBy<string, string>((string p) => p, StringComparer.OrdinalIgnoreCase))}')", isNullHandler: true));
			ActionCache[action] = value;
		}
		return value;
	}

	/// <summary>Get whether an action matches an existing action.</summary>
	/// <param name="action">The action string to validate.</param>
	/// <param name="error">An error phrase indicating why parsing the action failed (like 'unknown action X'), if applicable.</param>
	/// <returns>Returns whether the action was parsed successfully and matches an existing command.</returns>
	public static bool TryValidateActionExists(string action, out string error)
	{
		CachedAction cachedAction = ParseAction(action);
		error = cachedAction.Error;
		return error == null;
	}

	/// <summary>Run an action if it's valid.</summary>
	/// <param name="action">The action string to run.</param>
	/// <param name="error">An error phrase indicating why parsing or running the action failed (like 'unknown action X'), if applicable.</param>
	/// <param name="exception">An exception which accompanies <paramref name="error" />, if applicable.</param>
	/// <returns>Returns whether the action was applied successfully (regardless of whether it did anything).</returns>
	public static bool TryRunAction(string action, out string error, out Exception exception)
	{
		bool num = TryRunAction(ParseAction(action), EmptyManualContext, out error, out exception);
		if (!num && string.IsNullOrWhiteSpace(error))
		{
			error = ((exception != null) ? "an unhandled error occurred" : "the action failed but didn't provide an error message");
		}
		return num;
	}

	/// <summary>Run an action if it's valid.</summary>
	/// <param name="action">The action string to run.</param>
	/// <param name="trigger">The trigger key to raise.</param>
	/// <param name="triggerArgs">The contextual arguments provided with the trigger, if applicable. For example, an 'item received' trigger might provide the item instance and index.</param>
	/// <param name="error">An error phrase indicating why parsing or running the action failed (like 'unknown action X'), if applicable.</param>
	/// <param name="exception">An exception which accompanies <paramref name="error" />, if applicable.</param>
	/// <returns>Returns whether the action was applied successfully (regardless of whether it did anything).</returns>
	public static bool TryRunAction(string action, string trigger, object[] triggerArgs, out string error, out Exception exception)
	{
		if (trigger == null)
		{
			throw new ArgumentNullException("trigger");
		}
		if (triggerArgs == null)
		{
			throw new ArgumentNullException("triggerArgs");
		}
		TriggerActionContext context = ((trigger == "Manual" && triggerArgs.Length == 0) ? EmptyManualContext : new TriggerActionContext(trigger, triggerArgs, null));
		return TryRunAction(ParseAction(action), context, out error, out exception);
	}

	/// <summary>Run an action if it's valid.</summary>
	/// <param name="action">The action to run.</param>
	/// <param name="context">The trigger action context.</param>
	/// <param name="error">An error phrase indicating why parsing or running the action failed (like 'unknown action X'), if applicable.</param>
	/// <param name="exception">An exception which accompanies <paramref name="error" />, if applicable.</param>
	/// <returns>Returns whether the action was applied successfully (regardless of whether it did anything).</returns>
	/// <remarks>This is a low-level method. Most code should use <see cref="M:StardewValley.Triggers.TriggerActionManager.TryRunAction(System.String,System.String@,System.Exception@)" /> instead.</remarks>
	public static bool TryRunAction(CachedAction action, TriggerActionContext context, out string error, out Exception exception)
	{
		if (action == null)
		{
			error = null;
			exception = null;
			return true;
		}
		if (action.Error != null)
		{
			error = action.Error;
			exception = null;
			return false;
		}
		try
		{
			action.Handler(action.Args, context, out error);
			if (error != null)
			{
				exception = null;
				return false;
			}
			exception = null;
			return true;
		}
		catch (Exception ex)
		{
			error = "an unexpected error occurred";
			exception = ex;
			return false;
		}
	}

	/// <summary>Run all actions from a given <c>Data/TriggerActions</c> entry, if its fields match the current context.</summary>
	/// <param name="entry">The entry to apply from <c>Data/TriggerActions</c>, as returned by <see cref="M:StardewValley.Triggers.TriggerActionManager.GetActionsForTrigger(System.String)" />.</param>
	/// <param name="trigger">The trigger key to raise.</param>
	/// <param name="triggerArgs">The contextual arguments provided with the trigger, if applicable. For example, an 'item received' trigger might provide the item instance and index.</param>
	/// <param name="location">The location for which to check action conditions, or <c>null</c> to use the current location.</param>
	/// <param name="player">The player for which to check action conditions, or <c>null</c> to use the current player.</param>
	/// <param name="targetItem">The target item (e.g. machine output or tree fruit) for which to check action conditions, or <c>null</c> if not applicable.</param>
	/// <param name="inputItem">The input item (e.g. machine input) for which to check action conditions, or <c>null</c> if not applicable.</param>
	/// <returns>Returns whether any of the actions were applied.</returns>
	public static bool TryRunActions(CachedTriggerAction entry, string trigger, object[] triggerArgs = null, GameLocation location = null, Farmer player = null, Item targetItem = null, Item inputItem = null)
	{
		TriggerActionData data = entry.Data;
		if (Game1.player.triggerActionsRun.Contains(data.Id))
		{
			return false;
		}
		if (data.SkipPermanentlyCondition != null && GameStateQuery.CheckConditions(data.SkipPermanentlyCondition))
		{
			Game1.player.triggerActionsRun.Add(data.Id);
			return false;
		}
		if (!CanApplyIgnoringRun(data, location, player, targetItem, inputItem))
		{
			return false;
		}
		TriggerActionContext context = new TriggerActionContext(trigger, triggerArgs, data);
		CachedAction[] actions = entry.Actions;
		foreach (CachedAction cachedAction in actions)
		{
			if (!TryRunAction(cachedAction, context, out var error, out var exception))
			{
				Game1.log.Error($"Trigger action '{data.Id}' has action string '{string.Join(" ", cachedAction.Args)}' which couldn't be applied: {error}.", exception);
			}
		}
		if (data.MarkActionApplied)
		{
			Game1.player.triggerActionsRun.Add(data.Id);
		}
		Game1.log.Verbose($"Applied trigger action '{data.Id}' with actions [{string.Join("], [", entry.ActionStrings)}].");
		return true;
	}

	/// <summary>Get the handler for an action key, if any.</summary>
	/// <param name="key">The action key. This is case-insensitive.</param>
	/// <param name="handler">The action handler, if found.</param>
	/// <returns>Returns whether a handler was found for the action key.</returns>
	public static bool TryGetActionHandler(string key, out TriggerActionDelegate handler)
	{
		return ActionHandlers.TryGetValue(key, out handler);
	}

	/// <summary>Get the trigger actions in <c>Data/TriggerActions</c> registered for a given trigger, or an empty list if none are registered.</summary>
	/// <param name="trigger">The trigger key to raise.</param>
	/// <remarks>This is a low-level method. Most code should use <see cref="M:StardewValley.Triggers.TriggerActionManager.TryRunAction(System.String,System.String@,System.Exception@)" /> instead.</remarks>
	public static IReadOnlyList<CachedTriggerAction> GetActionsForTrigger(string trigger)
	{
		if (GetActionsByTrigger().TryGetValue(trigger, out var value))
		{
			return value;
		}
		return LegacyShims.EmptyArray<CachedTriggerAction>();
	}

	/// <summary>Get whether an action can be applied based on its conditions and whether it has already been run.</summary>
	/// <param name="action">The action to check.</param>
	/// <param name="location">The location for which to check action conditions, or <c>null</c> to use the current location.</param>
	/// <param name="player">The player for which to check action conditions, or <c>null</c> to use the current player.</param>
	/// <param name="targetItem">The target item (e.g. machine output or tree fruit) for which to check action conditions, or <c>null</c> if not applicable.</param>
	/// <param name="inputItem">The input item (e.g. machine input) for which to check action conditions, or <c>null</c> if not applicable.</param>
	public static bool CanApply(TriggerActionData action, GameLocation location = null, Farmer player = null, Item targetItem = null, Item inputItem = null)
	{
		if (!Game1.player.triggerActionsRun.Contains(action.Id))
		{
			return CanApplyIgnoringRun(action, location, player, targetItem, inputItem);
		}
		return false;
	}

	/// <summary>Rebuild the cached data from <c>Data/TriggerActions</c>.</summary>
	public static void ResetDataCache()
	{
		ActionCache.Clear();
		ActionsByTrigger.Clear();
	}

	/// <summary>Initialize the base static state.</summary>
	static TriggerActionManager()
	{
		ValidTriggerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DayEnding", "DayStarted", "LocationChanged", "Manual" };
		ActionHandlers = new Dictionary<string, TriggerActionDelegate>(StringComparer.OrdinalIgnoreCase);
		ActionsByTrigger = new Dictionary<string, List<CachedTriggerAction>>(StringComparer.OrdinalIgnoreCase);
		ActionCache = new Dictionary<string, CachedAction>(StringComparer.OrdinalIgnoreCase);
		EmptyManualContext = new TriggerActionContext("Manual", LegacyShims.EmptyArray<object>(), null);
		MethodInfo[] methods = typeof(DefaultActions).GetMethods(BindingFlags.Static | BindingFlags.Public);
		foreach (MethodInfo methodInfo in methods)
		{
			TriggerActionDelegate value = (TriggerActionDelegate)Delegate.CreateDelegate(typeof(TriggerActionDelegate), methodInfo);
			ActionHandlers.Add(methodInfo.Name, value);
		}
		NullAction = new CachedAction(LegacyShims.EmptyArray<string>(), ActionHandlers["Null"], null, isNullHandler: true);
	}

	/// <summary>Get the registered actions by trigger, loading them if needed.</summary>
	private static Dictionary<string, List<CachedTriggerAction>> GetActionsByTrigger()
	{
		Dictionary<string, List<CachedTriggerAction>> actionsByTrigger = ActionsByTrigger;
		if (actionsByTrigger.Count == 0)
		{
			foreach (string validTriggerType in ValidTriggerTypes)
			{
				actionsByTrigger[validTriggerType] = new List<CachedTriggerAction>();
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<CachedAction> list = new List<CachedAction>();
			foreach (TriggerActionData item2 in DataLoader.TriggerActions(Game1.content))
			{
				if (string.IsNullOrWhiteSpace(item2.Id))
				{
					Game1.log.Error("Trigger action has no ID field and will be ignored.");
					continue;
				}
				if (string.IsNullOrWhiteSpace(item2.Trigger))
				{
					Game1.log.Error($"Trigger action '{item2.Id}' has no trigger; expected one of '{string.Join("', '", ValidTriggerTypes)}'.");
					continue;
				}
				if (string.IsNullOrWhiteSpace(item2.Action))
				{
					List<string> actions = item2.Actions;
					if (actions == null || actions.Count <= 0)
					{
						Game1.log.Error("Trigger action '" + item2.Id + "' has no defined actions.");
						continue;
					}
				}
				if (!hashSet.Add(item2.Id))
				{
					Game1.log.Error("Trigger action '" + item2.Id + "' has a duplicate ID. Only the first instance will be used.");
					continue;
				}
				list.Clear();
				if (item2.Action != null)
				{
					CachedAction cachedAction = ParseAction(item2.Action);
					if (cachedAction.Error != null)
					{
						Game1.log.Error($"Trigger action '{item2.Id}' will skip invalid action '{item2.Action}': {cachedAction.Error}.");
					}
					else if (!cachedAction.IsNullHandler)
					{
						list.Add(cachedAction);
					}
				}
				if (item2.Actions != null)
				{
					foreach (string action in item2.Actions)
					{
						CachedAction cachedAction2 = ParseAction(action);
						if (cachedAction2.Error != null)
						{
							Game1.log.Error($"Trigger action '{item2.Id}' will skip invalid action '{item2.Action}': {cachedAction2.Error}.");
						}
						else if (!cachedAction2.IsNullHandler)
						{
							list.Add(cachedAction2);
						}
					}
				}
				CachedTriggerAction item = new CachedTriggerAction(item2, list.ToArray());
				string[] array = ArgUtility.SplitBySpace(item2.Trigger);
				foreach (string text in array)
				{
					if (!ValidTriggerTypes.Contains(text))
					{
						Game1.log.Error($"Trigger action '{item2.Id}' has unknown trigger '{text}'; expected one of '{string.Join("', '", ValidTriggerTypes)}'.");
					}
					else
					{
						actionsByTrigger[text].Add(item);
					}
				}
			}
		}
		return actionsByTrigger;
	}

	/// <summary>Get whether an action can be applied based on its conditions, ignoring whether it has already been run.</summary>
	/// <param name="action">The action to check.</param>
	/// <param name="location">The location for which to check action conditions, or <c>null</c> to use the current location.</param>
	/// <param name="player">The player for which to check action conditions, or <c>null</c> to use the current player.</param>
	/// <param name="targetItem">The target item (e.g. machine output or tree fruit) for which to check action conditions, or <c>null</c> if not applicable.</param>
	/// <param name="inputItem">The input item (e.g. machine input) for which to check action conditions, or <c>null</c> if not applicable.</param>
	private static bool CanApplyIgnoringRun(TriggerActionData action, GameLocation location = null, Farmer player = null, Item targetItem = null, Item inputItem = null)
	{
		if (!action.HostOnly || Game1.IsMasterGame)
		{
			return GameStateQuery.CheckConditions(action.Condition, location, player, targetItem, inputItem);
		}
		return false;
	}
}
