using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData.Museum;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Triggers;
using xTile.Dimensions;

namespace StardewValley.Locations;

public class LibraryMuseum : GameLocation
{
	public const int dwarvenGuide = 0;

	protected static int _totalArtifacts = -1;

	public const int totalNotes = 21;

	private readonly NetMutex mutex = new NetMutex();

	[XmlIgnore]
	protected Dictionary<Item, string> _itemToRewardsLookup = new Dictionary<Item, string>();

	public static int totalArtifacts
	{
		get
		{
			if (_totalArtifacts < 0)
			{
				_totalArtifacts = 0;
				foreach (string allId in ItemRegistry.RequireTypeDefinition("(O)").GetAllIds())
				{
					if (IsItemSuitableForDonation("(O)" + allId, checkDonatedItems: false))
					{
						_totalArtifacts++;
					}
				}
			}
			return _totalArtifacts;
		}
	}

	[XmlElement("museumPieces")]
	public NetVector2Dictionary<string, NetString> museumPieces => Game1.netWorldState.Value.MuseumPieces;

	public LibraryMuseum()
	{
	}

	public LibraryMuseum(string mapPath, string name)
		: base(mapPath, name)
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(mutex.NetFields, "mutex.NetFields");
	}

	public override void updateEvenIfFarmerIsntHere(GameTime time, bool skipWasUpdatedFlush = false)
	{
		mutex.Update(this);
		base.updateEvenIfFarmerIsntHere(time, skipWasUpdatedFlush);
	}

	/// <summary>Get whether any artifacts have been donated to the museum.</summary>
	public static bool HasDonatedArtifacts()
	{
		return Game1.netWorldState.Value.MuseumPieces.Length > 0;
	}

	/// <summary>Get whether an artifact has been placed on a given museum tile.</summary>
	/// <param name="tile">The tile position to check.</param>
	public static bool HasDonatedArtifactAt(Vector2 tile)
	{
		return Game1.netWorldState.Value.MuseumPieces.ContainsKey(tile);
	}

	/// <summary>Get whether an artifact has been donated to the museum.</summary>
	/// <param name="itemId">The qualified or unqualified item ID to check.</param>
	public static bool HasDonatedArtifact(string itemId)
	{
		if (itemId == null)
		{
			return false;
		}
		itemId = ItemRegistry.ManuallyQualifyItemId(itemId, "(O)");
		foreach (KeyValuePair<Vector2, string> pair in Game1.netWorldState.Value.MuseumPieces.Pairs)
		{
			if (itemId == "(O)" + pair.Value)
			{
				return true;
			}
		}
		return false;
	}

	public bool isItemSuitableForDonation(Item i)
	{
		return IsItemSuitableForDonation(i?.QualifiedItemId);
	}

	/// <summary>Get whether an item can be donated to the museum.</summary>
	/// <param name="itemId">The qualified or unqualified item ID.</param>
	/// <param name="checkDonatedItems">Whether to return false if the item has already been donated to the museum.</param>
	public static bool IsItemSuitableForDonation(string itemId, bool checkDonatedItems = true)
	{
		if (itemId == null)
		{
			return false;
		}
		itemId = ItemRegistry.ManuallyQualifyItemId(itemId, "(O)");
		ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(itemId);
		HashSet<string> baseContextTags = ItemContextTagManager.GetBaseContextTags(itemId);
		if (!dataOrErrorItem.HasTypeObject() || baseContextTags.Contains("not_museum_donatable"))
		{
			return false;
		}
		if (checkDonatedItems && HasDonatedArtifact(dataOrErrorItem.QualifiedItemId))
		{
			return false;
		}
		if (!baseContextTags.Contains("museum_donatable") && !baseContextTags.Contains("item_type_arch"))
		{
			return baseContextTags.Contains("item_type_minerals");
		}
		return true;
	}

	public bool doesFarmerHaveAnythingToDonate(Farmer who)
	{
		for (int i = 0; i < who.maxItems.Value; i++)
		{
			if (i < who.Items.Count && who.Items[i] is Object i2 && isItemSuitableForDonation(i2))
			{
				return true;
			}
		}
		return false;
	}

	private Dictionary<int, Vector2> getLostBooksLocations()
	{
		Dictionary<int, Vector2> dictionary = new Dictionary<int, Vector2>();
		for (int i = 0; i < map.Layers[0].LayerWidth; i++)
		{
			for (int j = 0; j < map.Layers[0].LayerHeight; j++)
			{
				string[] tilePropertySplitBySpaces = GetTilePropertySplitBySpaces("Action", "Buildings", i, j);
				if (ArgUtility.Get(tilePropertySplitBySpaces, 0) == "Notes")
				{
					if (ArgUtility.TryGetInt(tilePropertySplitBySpaces, 1, out var value, out var error, "int noteId"))
					{
						dictionary.Add(value, new Vector2(i, j));
					}
					else
					{
						LogTileActionError(tilePropertySplitBySpaces, i, j, error);
					}
				}
			}
		}
		return dictionary;
	}

	protected override void resetLocalState()
	{
		if (!Game1.player.eventsSeen.Contains("0") && doesFarmerHaveAnythingToDonate(Game1.player))
		{
			Game1.player.mailReceived.Add("somethingToDonate");
		}
		if (HasDonatedArtifacts())
		{
			Game1.player.mailReceived.Add("somethingWasDonated");
		}
		base.resetLocalState();
		int lostBooksFound = Game1.netWorldState.Value.LostBooksFound;
		foreach (KeyValuePair<int, Vector2> lostBooksLocation in getLostBooksLocations())
		{
			int key = lostBooksLocation.Key;
			Vector2 value = lostBooksLocation.Value;
			if (key <= lostBooksFound && !Game1.player.mailReceived.Contains("lb_" + key))
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(144, 447, 15, 15), new Vector2(value.X * 64f, value.Y * 64f - 96f - 16f), flipped: false, 0f, Color.White)
				{
					interval = 99999f,
					animationLength = 1,
					totalNumberOfLoops = 9999,
					yPeriodic = true,
					yPeriodicLoopTime = 4000f,
					yPeriodicRange = 16f,
					layerDepth = 1f,
					scale = 4f,
					id = key
				});
			}
		}
	}

	public override void cleanupBeforePlayerExit()
	{
		_itemToRewardsLookup?.Clear();
		base.cleanupBeforePlayerExit();
	}

	public override bool answerDialogueAction(string questionAndAnswer, string[] questionParams)
	{
		if (questionAndAnswer == null)
		{
			return false;
		}
		switch (questionAndAnswer)
		{
		case "Museum_Collect":
			OpenRewardMenu();
			break;
		case "Museum_Donate":
			OpenDonationMenu();
			break;
		case "Museum_Rearrange_Yes":
			OpenRearrangeMenu();
			break;
		}
		return base.answerDialogueAction(questionAndAnswer, questionParams);
	}

	public string getRewardItemKey(Item item)
	{
		return "museumCollectedReward" + Utility.getStandardDescriptionFromItem(item, 1, '_');
	}

	/// <inheritdoc />
	public override bool performAction(string[] action, Farmer who, Location tileLocation)
	{
		if (who.IsLocalPlayer)
		{
			string text = ArgUtility.Get(action, 0);
			if (text == "Gunther")
			{
				OpenGuntherDialogueMenu();
				return true;
			}
			if (text == "Rearrange" && !doesFarmerHaveAnythingToDonate(Game1.player))
			{
				if (HasDonatedArtifacts())
				{
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Rearrange"), createYesNoResponses(), "Museum_Rearrange");
				}
				return true;
			}
		}
		return base.performAction(action, who, tileLocation);
	}

	/// <summary>Get the reward items which can be collected by a player.</summary>
	/// <param name="player">The player collecting rewards.</param>
	public List<Item> getRewardsForPlayer(Farmer player)
	{
		_itemToRewardsLookup.Clear();
		Dictionary<string, MuseumRewards> dictionary = DataLoader.MuseumRewards(Game1.content);
		Dictionary<string, int> donatedByContextTag = GetDonatedByContextTag(dictionary);
		List<Item> list = new List<Item>();
		foreach (KeyValuePair<string, MuseumRewards> item2 in dictionary)
		{
			string key = item2.Key;
			MuseumRewards value = item2.Value;
			if (!CanCollectReward(value, key, player, donatedByContextTag))
			{
				continue;
			}
			bool flag = false;
			if (value.RewardItemId != null)
			{
				Item item = ItemRegistry.Create(value.RewardItemId, value.RewardItemCount);
				item.IsRecipe = value.RewardItemIsRecipe;
				item.specialItem = value.RewardItemIsSpecial;
				if (AddRewardItemIfUncollected(player, list, item))
				{
					_itemToRewardsLookup[item] = key;
					flag = true;
				}
			}
			if (!flag)
			{
				AddNonItemRewards(value, key, player);
			}
		}
		return list;
	}

	/// <summary>Give the player a set of non-item donation rewards.</summary>
	/// <param name="data">The museum donation rewards to give to the player.</param>
	/// <param name="rewardId">The unique ID for <paramref name="data" />.</param>
	/// <param name="player">The player collecting rewards.</param>
	public void AddNonItemRewards(MuseumRewards data, string rewardId, Farmer player)
	{
		if (data.FlagOnCompletion)
		{
			player.mailReceived.Add(rewardId);
		}
		if (data.RewardActions == null)
		{
			return;
		}
		foreach (string rewardAction in data.RewardActions)
		{
			if (!TriggerActionManager.TryRunAction(rewardAction, out var error, out var exception))
			{
				Game1.log.Error($"Museum reward {rewardId} ignored invalid event action '{rewardAction}': {error}", exception);
			}
		}
	}

	/// <summary>Add the item to the reward list only if the item hasn't been marked as collected.</summary>
	/// <param name="player">The player collecting rewards.</param>
	/// <param name="rewards">The list of rewards to update.</param>
	/// <param name="rewardItem">The reward to add if it's uncollected.</param>
	public bool AddRewardItemIfUncollected(Farmer player, List<Item> rewards, Item rewardItem)
	{
		if (!player.mailReceived.Contains(getRewardItemKey(rewardItem)))
		{
			rewards.Add(rewardItem);
			return true;
		}
		return false;
	}

	/// <summary>Get whether the player can collect an item from the reward menu.</summary>
	/// <param name="item">The item to check.</param>
	public bool HighlightCollectableRewards(Item item)
	{
		return Game1.player.couldInventoryAcceptThisItem(item);
	}

	/// <summary>Open the artifact rearranging menu.</summary>
	public void OpenRearrangeMenu()
	{
		if (!mutex.IsLocked())
		{
			mutex.RequestLock(delegate
			{
				Game1.activeClickableMenu = new MuseumMenu(InventoryMenu.highlightNoItems)
				{
					exitFunction = mutex.ReleaseLock
				};
			});
		}
	}

	/// <summary>Open the reward collection menu.</summary>
	public void OpenRewardMenu()
	{
		Game1.activeClickableMenu = new ItemGrabMenu(getRewardsForPlayer(Game1.player), reverseGrab: false, showReceivingMenu: true, HighlightCollectableRewards, null, "Rewards", OnRewardCollected, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: false, allowRightClick: false, showOrganizeButton: false, 0, null, -1, this, ItemExitBehavior.ReturnToPlayer, allowExitWithHeldItem: true);
	}

	/// <summary>Open the artifact donation menu.</summary>
	public void OpenDonationMenu()
	{
		mutex.RequestLock(delegate
		{
			Game1.activeClickableMenu = new MuseumMenu(isItemSuitableForDonation)
			{
				exitFunction = OnDonationMenuClosed
			};
		});
	}

	/// <summary>Handle the player closing the artifact donation screen.</summary>
	public void OnDonationMenuClosed()
	{
		mutex.ReleaseLock();
		getRewardsForPlayer(Game1.player);
	}

	/// <summary>Handle the player collecting an item from the reward screen.</summary>
	/// <param name="item">The item that was collected.</param>
	/// <param name="who">The player collecting rewards.</param>
	public void OnRewardCollected(Item item, Farmer who)
	{
		if (item == null)
		{
			return;
		}
		if (item is Object && _itemToRewardsLookup.TryGetValue(item, out var value))
		{
			if (DataLoader.MuseumRewards(Game1.content).TryGetValue(value, out var value2))
			{
				AddNonItemRewards(value2, value, who);
			}
			_itemToRewardsLookup.Remove(item);
		}
		if (!who.hasOrWillReceiveMail(getRewardItemKey(item)))
		{
			who.mailReceived.Add(getRewardItemKey(item));
			if (item.QualifiedItemId.Equals("(O)499"))
			{
				who.craftingRecipes.TryAdd("Ancient Seeds", 0);
			}
		}
	}

	/// <summary>Open the dialogue menu for Gunther.</summary>
	private void OpenGuntherDialogueMenu()
	{
		if (doesFarmerHaveAnythingToDonate(Game1.player) && !mutex.IsLocked())
		{
			Response[] answerChoices = ((getRewardsForPlayer(Game1.player).Count <= 0) ? new Response[2]
			{
				new Response("Donate", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Donate")),
				new Response("Leave", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Leave"))
			} : new Response[3]
			{
				new Response("Donate", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Donate")),
				new Response("Collect", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Collect")),
				new Response("Leave", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Leave"))
			});
			createQuestionDialogue("", answerChoices, "Museum");
		}
		else if (getRewardsForPlayer(Game1.player).Count > 0)
		{
			createQuestionDialogue("", new Response[2]
			{
				new Response("Collect", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Collect")),
				new Response("Leave", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Leave"))
			}, "Museum");
		}
		else if (doesFarmerHaveAnythingToDonate(Game1.player) && mutex.IsLocked())
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NPC_Busy", Game1.RequireCharacter("Gunther").displayName));
		}
		else
		{
			NPC characterFromName = Game1.getCharacterFromName("Gunther");
			if (Game1.player.achievements.Contains(5))
			{
				Game1.DrawDialogue(new Dialogue(characterFromName, "Data\\ExtraDialogue:Gunther_MuseumComplete", Game1.parseText(Game1.content.LoadString("Data\\ExtraDialogue:Gunther_MuseumComplete"))));
			}
			else if (Game1.player.mailReceived.Contains("artifactFound"))
			{
				Game1.DrawDialogue(new Dialogue(characterFromName, "Data\\ExtraDialogue:Gunther_NothingToDonate", Game1.parseText(Game1.content.LoadString("Data\\ExtraDialogue:Gunther_NothingToDonate"))));
			}
			else
			{
				Game1.DrawDialogue(characterFromName, "Data\\ExtraDialogue:Gunther_NoArtifactsFound");
			}
		}
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		if (museumPieces.TryGetValue(new Vector2(tileLocation.X, tileLocation.Y), out var value) || museumPieces.TryGetValue(new Vector2(tileLocation.X, tileLocation.Y - 1), out value))
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(O)" + value);
			Game1.drawObjectDialogue(Game1.parseText(" - " + dataOrErrorItem.DisplayName + " - " + "^" + dataOrErrorItem.Description));
			return true;
		}
		return base.checkAction(tileLocation, viewport, who);
	}

	public bool isTileSuitableForMuseumPiece(int x, int y)
	{
		if (!HasDonatedArtifactAt(new Vector2(x, y)))
		{
			int tileIndexAt = getTileIndexAt(x, y, "Buildings", "untitled tile sheet");
			if ((uint)(tileIndexAt - 1072) <= 2u || (uint)(tileIndexAt - 1237) <= 1u)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Get a count of donated items by context tag.</summary>
	/// <param name="museumRewardData">The museum rewards for which to count context tags.</param>
	public Dictionary<string, int> GetDonatedByContextTag(Dictionary<string, MuseumRewards> museumRewardData)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (MuseumRewards value in museumRewardData.Values)
		{
			foreach (MuseumDonationRequirement targetContextTag in value.TargetContextTags)
			{
				dictionary[targetContextTag.Tag] = 0;
			}
		}
		string[] array = dictionary.Keys.ToArray();
		foreach (string value2 in museumPieces.Values)
		{
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (text == "" || ItemContextTagManager.HasBaseTag(value2, text))
				{
					dictionary[text]++;
				}
			}
		}
		return dictionary;
	}

	/// <summary>Get whether a reward can be collected by a player.</summary>
	/// <param name="reward">The reward data to check.</param>
	/// <param name="rewardId">The unique ID for the <paramref name="reward" />.</param>
	/// <param name="player">The player collecting rewards.</param>
	/// <param name="countsByTag">The number of donated items matching each context tag.</param>
	public bool CanCollectReward(MuseumRewards reward, string rewardId, Farmer player, Dictionary<string, int> countsByTag)
	{
		if (reward.FlagOnCompletion && player.mailReceived.Contains(rewardId))
		{
			return false;
		}
		foreach (MuseumDonationRequirement targetContextTag in reward.TargetContextTags)
		{
			if (targetContextTag.Tag == "" && targetContextTag.Count == -1)
			{
				if (countsByTag[targetContextTag.Tag] < totalArtifacts)
				{
					return false;
				}
			}
			else if (countsByTag[targetContextTag.Tag] < targetContextTag.Count)
			{
				return false;
			}
		}
		if (reward.RewardItemId != null)
		{
			if (player.canUnderstandDwarves && ItemRegistry.QualifyItemId(reward.RewardItemId) == "(O)326")
			{
				return false;
			}
			if (reward.RewardItemIsSpecial)
			{
				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(reward.RewardItemId);
				if (((dataOrErrorItem.HasTypeId("(F)") || dataOrErrorItem.HasTypeBigCraftable()) ? player.specialBigCraftables : player.specialItems).Contains(dataOrErrorItem.ItemId))
				{
					return false;
				}
			}
		}
		return true;
	}

	public Microsoft.Xna.Framework.Rectangle getMuseumDonationBounds()
	{
		return new Microsoft.Xna.Framework.Rectangle(26, 5, 22, 13);
	}

	public Vector2 getFreeDonationSpot()
	{
		Microsoft.Xna.Framework.Rectangle museumDonationBounds = getMuseumDonationBounds();
		for (int i = museumDonationBounds.X; i <= museumDonationBounds.Right; i++)
		{
			for (int j = museumDonationBounds.Y; j <= museumDonationBounds.Bottom; j++)
			{
				if (isTileSuitableForMuseumPiece(i, j))
				{
					return new Vector2(i, j);
				}
			}
		}
		return new Vector2(26f, 5f);
	}

	public Vector2 findMuseumPieceLocationInDirection(Vector2 startingPoint, int direction, int distanceToCheck = 8, bool ignoreExistingItems = true)
	{
		Vector2 vector = startingPoint;
		Vector2 vector2 = Vector2.Zero;
		switch (direction)
		{
		case 0:
			vector2 = new Vector2(0f, -1f);
			break;
		case 1:
			vector2 = new Vector2(1f, 0f);
			break;
		case 2:
			vector2 = new Vector2(0f, 1f);
			break;
		case 3:
			vector2 = new Vector2(-1f, 0f);
			break;
		}
		for (int i = 0; i < distanceToCheck; i++)
		{
			for (int j = 0; j < distanceToCheck; j++)
			{
				vector += vector2;
				if (isTileSuitableForMuseumPiece((int)vector.X, (int)vector.Y) || (!ignoreExistingItems && HasDonatedArtifactAt(vector)))
				{
					return vector;
				}
			}
			vector = startingPoint;
			int num = ((i % 2 != 0) ? 1 : (-1));
			switch (direction)
			{
			case 0:
			case 2:
				vector.X += num * (i / 2 + 1);
				break;
			case 1:
			case 3:
				vector.Y += num * (i / 2 + 1);
				break;
			}
		}
		return startingPoint;
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		foreach (TemporaryAnimatedSprite temporarySprite in temporarySprites)
		{
			if (temporarySprite.layerDepth >= 1f)
			{
				temporarySprite.draw(b);
			}
		}
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		foreach (KeyValuePair<Vector2, string> pair in museumPieces.Pairs)
		{
			b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, pair.Key * 64f + new Vector2(32f, 52f)), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, (pair.Key.Y * 64f - 2f) / 10000f);
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(O)" + pair.Value);
			b.Draw(dataOrErrorItem.GetTexture(), Game1.GlobalToLocal(Game1.viewport, pair.Key * 64f), dataOrErrorItem.GetSourceRect(), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, pair.Key.Y * 64f / 10000f);
		}
	}
}
