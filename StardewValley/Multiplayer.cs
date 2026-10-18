using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Netcode;
using StardewValley.Buildings;
using StardewValley.GameData.LocationContexts;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.TokenizableStrings;

namespace StardewValley;

public class Multiplayer
{
	public enum PartyWideMessageQueue
	{
		MailForTomorrow,
		SeenMail
	}

	public enum DisconnectType
	{
		None,
		ClosedGame,
		ExitedToMainMenu,
		ExitedToMainMenu_FromFarmhandSelect,
		HostLeft,
		ServerOfflineMode,
		ServerFull,
		Kicked,
		AcceptedOtherInvite,
		ClientTimeout,
		LidgrenTimeout,
		GalaxyTimeout,
		Timeout_FarmhandSelection,
		LidgrenDisconnect_Unknown
	}

	public static readonly long AllPlayers = 0L;

	public const byte farmerDelta = 0;

	public const byte serverIntroduction = 1;

	public const byte playerIntroduction = 2;

	public const byte locationIntroduction = 3;

	public const byte forceEvent = 4;

	public const byte warpFarmer = 5;

	public const byte locationDelta = 6;

	public const byte locationSprites = 7;

	public const byte characterWarp = 8;

	public const byte availableFarmhands = 9;

	public const byte chatMessage = 10;

	public const byte connectionMessage = 11;

	public const byte worldDelta = 12;

	public const byte teamDelta = 13;

	public const byte newDaySync = 14;

	public const byte chatInfoMessage = 15;

	public const byte userNameUpdate = 16;

	public const byte farmerGainExperience = 17;

	public const byte serverToClientsMessage = 18;

	public const byte disconnecting = 19;

	public const byte sharedAchievement = 20;

	public const byte globalMessage = 21;

	public const byte partyWideMail = 22;

	public const byte forceKick = 23;

	public const byte removeLocationFromLookup = 24;

	public const byte farmerKilledMonster = 25;

	public const byte requestGrandpaReevaluation = 26;

	public const byte digBuriedNut = 27;

	public const byte requestPassout = 28;

	public const byte passout = 29;

	public const byte startNewDaySync = 30;

	public const byte readySync = 31;

	public const byte chestHitSync = 32;

	public const byte dedicatedServerSync = 33;

	/// <summary>A compressed message, which must be decompressed to read the actual message.</summary>
	public const byte compressed = 127;

	public const byte WARP_FLAG_STRUCTURE = 1;

	public const byte WARP_FLAG_FORCED = 2;

	public const byte WARP_FLAG_NEEDS_INFO = 4;

	public const byte WARP_FLAG_FACE_UP = 8;

	public const byte WARP_FLAG_FACE_RIGHT = 16;

	public const byte WARP_FLAG_FACE_DOWN = 32;

	public const byte WARP_FLAG_FACE_LEFT = 64;

	/// <summary>A token prefix for messages sent via <see cref="M:StardewValley.Multiplayer.sendChatInfoMessage(System.String,System.String[])" /> that shows the result of <see cref="M:StardewValley.Utility.AOrAn(System.String)" /> for a tokenizable input.</summary>
	public const string chat_token_aOrAn = "aOrAn:";

	public int defaultInterpolationTicks = 15;

	public int farmerDeltaBroadcastPeriod = 3;

	public int locationDeltaBroadcastPeriod = 3;

	public int worldStateDeltaBroadcastPeriod = 3;

	public int playerLimit = 4;

	public static string kicked = "KICKED";

	/// <summary>The override value for <see cref="P:StardewValley.Multiplayer.protocolVersion" />, if set manually in the build settings.</summary>
	internal static string protocolVersionOverride;

	public readonly NetLogger logging = new NetLogger();

	protected List<long> disconnectingFarmers = new List<long>();

	public ulong latestID;

	public Dictionary<string, CachedMultiplayerMap> cachedMultiplayerMaps = new Dictionary<string, CachedMultiplayerMap>();

	protected HashSet<GameLocation> _updatedRoots = new HashSet<GameLocation>();

	public const string MSG_START_FESTIVAL_EVENT = "festivalEvent";

	public const string MSG_END_FESTIVAL = "endFest";

	public const string MSG_TRAIN_APPROACH = "trainApproach";

	/// <summary>A version string sent by the server to new connections. Clients disconnect with an error if it doesn't match their own protocol version, to prevent accidental connection of incompatible games.</summary>
	public static string protocolVersion
	{
		get
		{
			if (protocolVersionOverride != null)
			{
				return protocolVersionOverride;
			}
			return Game1.version + ((Game1.versionLabel != null) ? ("+" + new string(Game1.versionLabel.Where(char.IsLetterOrDigit).ToArray())) : "");
		}
	}

	public virtual int MaxPlayers
	{
		get
		{
			if (Game1.server == null)
			{
				return 1;
			}
			return playerLimit;
		}
	}

	public Multiplayer()
	{
		playerLimit = 8;
	}

	public virtual long getNewID()
	{
		ulong num = ((latestID & 0xFF) + 1) & 0xFF;
		ulong uniqueMultiplayerID = (ulong)Game1.player.UniqueMultiplayerID;
		uniqueMultiplayerID = (uniqueMultiplayerID >> 32) ^ (uniqueMultiplayerID & 0xFFFFFFFFu);
		uniqueMultiplayerID = ((uniqueMultiplayerID >> 16) ^ (uniqueMultiplayerID & 0xFFFF)) & 0xFFFF;
		ulong num2 = (ulong)(DateTime.UtcNow.Ticks / 10000);
		latestID = (num2 << 24) | (uniqueMultiplayerID << 8) | num;
		return (long)latestID;
	}

	public virtual bool isDisconnecting(Farmer farmer)
	{
		return isDisconnecting(farmer.UniqueMultiplayerID);
	}

	public virtual bool isDisconnecting(long uid)
	{
		return disconnectingFarmers.Contains(uid);
	}

	public virtual bool isClientBroadcastType(byte messageType)
	{
		switch (messageType)
		{
		case 0:
		case 2:
		case 4:
		case 6:
		case 7:
		case 12:
		case 13:
		case 14:
		case 15:
		case 19:
		case 20:
		case 21:
		case 22:
		case 24:
		case 26:
			return true;
		default:
			return false;
		}
	}

	public virtual bool allowSyncDelay()
	{
		return !Game1.newDaySync.hasInstance();
	}

	public virtual int interpolationTicks()
	{
		if (!allowSyncDelay())
		{
			return 0;
		}
		if (LocalMultiplayer.IsLocalMultiplayer(is_local_only: true))
		{
			return 4;
		}
		return defaultInterpolationTicks;
	}

	public virtual IEnumerable<NetFarmerRoot> farmerRoots()
	{
		if (Game1.serverHost != null)
		{
			yield return Game1.serverHost;
		}
		foreach (NetRoot<Farmer> value in Game1.otherFarmers.Roots.Values)
		{
			if (Game1.serverHost == null || value != Game1.serverHost)
			{
				yield return value as NetFarmerRoot;
			}
		}
	}

	public virtual NetFarmerRoot farmerRoot(long id)
	{
		if (Game1.serverHost != null && id == Game1.serverHost.Value.UniqueMultiplayerID)
		{
			return Game1.serverHost;
		}
		if (Game1.otherFarmers.Roots.TryGetValue(id, out var value))
		{
			return value as NetFarmerRoot;
		}
		return null;
	}

	public virtual void broadcastFarmerDeltas()
	{
		foreach (NetFarmerRoot item in farmerRoots())
		{
			if (item.Dirty && Game1.player.UniqueMultiplayerID == item.Value.UniqueMultiplayerID)
			{
				broadcastFarmerDelta(item.Value, writeObjectDeltaBytes(item));
			}
		}
		if (Game1.player.teamRoot.Dirty)
		{
			broadcastTeamDelta(writeObjectDeltaBytes(Game1.player.teamRoot));
		}
	}

	protected virtual void broadcastTeamDelta(byte[] delta)
	{
		if (Game1.IsServer)
		{
			foreach (Farmer value in Game1.otherFarmers.Values)
			{
				if (value != Game1.player)
				{
					Game1.server.sendMessage(value.UniqueMultiplayerID, 13, Game1.player, delta);
				}
			}
			return;
		}
		if (Game1.IsClient)
		{
			Game1.client.sendMessage(13, delta);
		}
	}

	protected virtual void broadcastFarmerDelta(Farmer farmer, byte[] delta)
	{
		foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
		{
			if (otherFarmer.Value.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
			{
				otherFarmer.Value.queueMessage(0, farmer, farmer.UniqueMultiplayerID, delta);
			}
		}
	}

	public void updateRoot<T>(T root) where T : INetRoot
	{
		foreach (long disconnectingFarmer in disconnectingFarmers)
		{
			root.Disconnect(disconnectingFarmer);
		}
		root.TickTree();
	}

	public virtual void updateRoots()
	{
		updateRoot(Game1.netWorldState);
		foreach (NetFarmerRoot item in farmerRoots())
		{
			item.Clock.InterpolationTicks = interpolationTicks();
			updateRoot(item);
		}
		Game1.player.teamRoot.Clock.InterpolationTicks = interpolationTicks();
		updateRoot(Game1.player.teamRoot);
		if (Game1.IsClient)
		{
			foreach (GameLocation item2 in activeLocations())
			{
				if (item2.Root != null && _updatedRoots.Add(item2.Root.Value))
				{
					item2.Root.Clock.InterpolationTicks = interpolationTicks();
					updateRoot(item2.Root);
				}
			}
		}
		else
		{
			Utility.ForEachLocation(delegate(GameLocation location)
			{
				if (location.Root != null)
				{
					location.Root.Clock.InterpolationTicks = interpolationTicks();
					updateRoot(location.Root);
				}
				return true;
			}, includeInteriors: false, includeGenerated: true);
		}
		_updatedRoots.Clear();
	}

	public virtual void broadcastLocationDeltas()
	{
		if (Game1.IsClient)
		{
			foreach (GameLocation item in activeLocations())
			{
				if (!(item.Root == null) && item.Root.Dirty)
				{
					broadcastLocationDelta(item);
				}
			}
			return;
		}
		Utility.ForEachLocation(delegate(GameLocation location)
		{
			if (location.Root != null && location.Root.Dirty)
			{
				broadcastLocationDelta(location);
			}
			return true;
		}, includeInteriors: false, includeGenerated: true);
	}

	public virtual void broadcastLocationDelta(GameLocation loc)
	{
		if (!(loc.Root == null) && loc.Root.Dirty)
		{
			byte[] bytes = writeObjectDeltaBytes(loc.Root);
			broadcastLocationBytes(loc, 6, bytes);
		}
	}

	protected virtual void broadcastLocationBytes(GameLocation loc, byte messageType, byte[] bytes)
	{
		OutgoingMessage message = new OutgoingMessage(messageType, Game1.player, loc.isStructure.Value, loc.NameOrUniqueName, bytes);
		broadcastLocationMessage(loc, message);
	}

	protected virtual void broadcastLocationMessage(GameLocation loc, OutgoingMessage message)
	{
		if (Game1.IsClient)
		{
			Game1.client.sendMessage(message);
			return;
		}
		if (isAlwaysActiveLocation(loc))
		{
			foreach (Farmer value in Game1.otherFarmers.Values)
			{
				TellFarmer(value);
			}
			return;
		}
		foreach (Farmer farmer in loc.farmers)
		{
			TellFarmer(farmer);
		}
		foreach (Building building in loc.buildings)
		{
			GameLocation indoors = building.GetIndoors();
			if (indoors == null)
			{
				continue;
			}
			foreach (Farmer farmer2 in indoors.farmers)
			{
				TellFarmer(farmer2);
			}
		}
		void TellFarmer(Farmer f)
		{
			if (f != Game1.player)
			{
				Game1.server.sendMessage(f.UniqueMultiplayerID, message);
			}
		}
	}

	public virtual void broadcastSprites(GameLocation location, TemporaryAnimatedSpriteList sprites)
	{
		broadcastSprites(location, sprites.ToArray());
	}

	public virtual void broadcastSprites(GameLocation location, params TemporaryAnimatedSprite[] sprites)
	{
		location.temporarySprites.AddRange(sprites);
		if (sprites.Length == 0 || !Game1.IsMultiplayer)
		{
			return;
		}
		using MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = createWriter(memoryStream))
		{
			binaryWriter.Push("TemporaryAnimatedSprites");
			binaryWriter.Write(sprites.Length);
			for (int i = 0; i < sprites.Length; i++)
			{
				sprites[i].Write(binaryWriter, location);
			}
			binaryWriter.Pop();
		}
		broadcastLocationBytes(location, 7, memoryStream.ToArray());
	}

	public virtual void broadcastWorldStateDeltas()
	{
		if (!Game1.netWorldState.Dirty)
		{
			return;
		}
		byte[] array = writeObjectDeltaBytes(Game1.netWorldState);
		foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
		{
			if (otherFarmer.Value != Game1.player)
			{
				otherFarmer.Value.queueMessage(12, Game1.player, array);
			}
		}
	}

	public virtual void receiveWorldState(BinaryReader msg)
	{
		Game1.netWorldState.Clock.InterpolationTicks = 0;
		readObjectDelta(msg, Game1.netWorldState);
		Game1.netWorldState.TickTree();
		int timeOfDay = Game1.timeOfDay;
		Game1.netWorldState.Value.WriteToGame1();
		if (!Game1.IsServer && timeOfDay != Game1.timeOfDay && Game1.currentLocation != null && !Game1.newDaySync.hasInstance())
		{
			Game1.performTenMinuteClockUpdate();
		}
	}

	public virtual void requestCharacterWarp(NPC character, GameLocation targetLocation, Vector2 position)
	{
		if (Game1.IsClient)
		{
			GameLocation currentLocation = character.currentLocation;
			if (currentLocation == null)
			{
				throw new ArgumentException("In warpCharacter, the character's currentLocation must not be null");
			}
			Guid guid = currentLocation.characters.GuidOf(character);
			if (guid == Guid.Empty)
			{
				throw new ArgumentException("In warpCharacter, the character must be in its currentLocation");
			}
			OutgoingMessage message = new OutgoingMessage(8, Game1.player, currentLocation.isStructure.Value, currentLocation.NameOrUniqueName, guid, targetLocation.isStructure.Value, targetLocation.NameOrUniqueName, position);
			Game1.serverHost.Value.queueMessage(message);
		}
	}

	public virtual NetRoot<GameLocation> locationRoot(GameLocation location)
	{
		if (location.Root == null && Game1.IsMasterGame)
		{
			new NetRoot<GameLocation>().Set(location);
			location.Root.Clock.InterpolationTicks = interpolationTicks();
			location.Root.MarkClean();
		}
		return location.Root;
	}

	public virtual void sendPassoutRequest()
	{
		object[] data = new object[1] { Game1.player.UniqueMultiplayerID };
		if (Game1.IsMasterGame)
		{
			_receivePassoutRequest(Game1.player);
		}
		else
		{
			Game1.client.sendMessage(28, data);
		}
	}

	public virtual void receivePassoutRequest(IncomingMessage msg)
	{
		if (Game1.IsServer)
		{
			Farmer player = Game1.GetPlayer(msg.Reader.ReadInt64());
			if (player != null)
			{
				_receivePassoutRequest(player);
			}
		}
	}

	protected virtual void _receivePassoutRequest(Farmer farmer)
	{
		if (!Game1.IsMasterGame)
		{
			return;
		}
		GameLocation gameLocation = ((farmer.lastSleepLocation.Value != null && Game1.isLocationAccessible(farmer.lastSleepLocation.Value)) ? Game1.getLocationFromName(farmer.lastSleepLocation.Value) : null);
		bool? flag = gameLocation?.CanWakeUpHere(farmer);
		if (flag.HasValue && flag == true && gameLocation.GetLocationContextId() == farmer.currentLocation.GetLocationContextId())
		{
			if (Game1.IsServer && farmer != Game1.player)
			{
				object[] source = new object[4]
				{
					farmer.lastSleepLocation.Value,
					farmer.lastSleepPoint.X,
					farmer.lastSleepPoint.Y,
					true
				};
				Game1.server.sendMessage(farmer.UniqueMultiplayerID, 29, Game1.player, source.ToArray());
			}
			else
			{
				Farmer.performPassoutWarp(farmer, farmer.lastSleepLocation.Value, farmer.lastSleepPoint.Value, has_bed: true);
			}
			return;
		}
		FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(farmer);
		string text = homeOfFarmer.NameOrUniqueName;
		Point bed_point = homeOfFarmer.GetPlayerBedSpot();
		bool flag2 = homeOfFarmer.GetPlayerBed() != null;
		List<ReviveLocation> list = farmer.currentLocation?.GetLocationContext().PassOutLocations ?? LocationContexts.Default.PassOutLocations;
		if (list != null)
		{
			foreach (ReviveLocation item in list)
			{
				if (!GameStateQuery.CheckConditions(item.Condition, farmer.currentLocation, farmer))
				{
					continue;
				}
				GameLocation locationFromName = Game1.getLocationFromName(item.Location);
				if (locationFromName == null)
				{
					break;
				}
				text = item.Location;
				bed_point = item.Position;
				flag2 = false;
				foreach (Furniture item2 in locationFromName.furniture)
				{
					if (item2 is BedFurniture { bedType: not BedFurniture.BedType.Child } bedFurniture)
					{
						bed_point = bedFurniture.GetBedSpot();
						flag2 = true;
						break;
					}
				}
				break;
			}
		}
		if (Game1.IsServer && farmer != Game1.player)
		{
			object[] source2 = new object[4] { text, bed_point.X, bed_point.Y, flag2 };
			Game1.server.sendMessage(farmer.UniqueMultiplayerID, 29, Game1.player, source2.ToArray());
		}
		else
		{
			Farmer.performPassoutWarp(farmer, text, bed_point, flag2);
		}
	}

	public virtual void receivePassout(IncomingMessage msg)
	{
		if (msg.SourceFarmer == Game1.serverHost.Value)
		{
			string bed_location_name = msg.Reader.ReadString();
			Point bed_point = new Point(msg.Reader.ReadInt32(), msg.Reader.ReadInt32());
			bool has_bed = msg.Reader.ReadBoolean();
			Farmer.performPassoutWarp(Game1.player, bed_location_name, bed_point, has_bed);
		}
	}

	public virtual object[] generateForceEventMessage(string eventId, GameLocation location, int tileX, int tileY, bool use_local_farmer, bool notify_when_done)
	{
		return new object[7]
		{
			eventId,
			use_local_farmer,
			notify_when_done,
			tileX,
			tileY,
			location.isStructure.Value ? ((byte)1) : ((byte)0),
			location.NameOrUniqueName
		};
	}

	public virtual void broadcastEvent(Event evt, GameLocation location, Vector2 positionBeforeEvent, bool use_local_farmer = true, bool notify_when_done = false)
	{
		if (string.IsNullOrEmpty(evt.id) || evt.id == "-1")
		{
			return;
		}
		object[] data = generateForceEventMessage(evt.id, location, (int)positionBeforeEvent.X, (int)positionBeforeEvent.Y, use_local_farmer, notify_when_done);
		if (Game1.IsServer)
		{
			foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
			{
				if (otherFarmer.Value.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
				{
					Game1.server.sendMessage(otherFarmer.Value.UniqueMultiplayerID, 4, Game1.dedicatedServer.FakeFarmer, data);
				}
			}
			return;
		}
		if (Game1.IsClient)
		{
			Game1.client.sendMessage(4, data);
		}
	}

	protected virtual void receiveRequestGrandpaReevaluation(IncomingMessage msg)
	{
		Game1.getFarm()?.requestGrandpaReevaluation();
	}

	protected virtual void receiveFarmerKilledMonster(IncomingMessage msg)
	{
		if (msg.SourceFarmer == Game1.serverHost.Value)
		{
			string text = msg.Reader.ReadString();
			if (text != null)
			{
				Game1.stats.monsterKilled(text);
			}
		}
	}

	public virtual void broadcastRemoveLocationFromLookup(GameLocation location)
	{
		List<object> list = new List<object>();
		list.Add(location.NameOrUniqueName);
		if (Game1.IsServer)
		{
			foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
			{
				if (otherFarmer.Value.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
				{
					Game1.server.sendMessage(otherFarmer.Value.UniqueMultiplayerID, 24, Game1.player, list.ToArray());
				}
			}
			return;
		}
		if (Game1.IsClient)
		{
			Game1.client.sendMessage(24, list.ToArray());
		}
	}

	public virtual void broadcastNutDig(GameLocation location, Point point)
	{
		if (Game1.IsMasterGame)
		{
			_performNutDig(location, point);
			return;
		}
		List<object> list = new List<object>();
		list.Add(location.NameOrUniqueName);
		list.Add(point.X);
		list.Add(point.Y);
		Game1.client.sendMessage(27, list.ToArray());
	}

	protected virtual void receiveNutDig(IncomingMessage msg)
	{
		if (Game1.IsMasterGame)
		{
			string name = msg.Reader.ReadString();
			Point point = new Point(msg.Reader.ReadInt32(), msg.Reader.ReadInt32());
			GameLocation locationFromName = Game1.getLocationFromName(name);
			_performNutDig(locationFromName, point);
		}
	}

	protected virtual void _performNutDig(GameLocation location, Point point)
	{
		if (location is IslandLocation islandLocation && islandLocation.IsBuriedNutLocation(point))
		{
			string item = location.NameOrUniqueName + "_" + point.X + "_" + point.Y;
			if (Game1.netWorldState.Value.FoundBuriedNuts.Add(item))
			{
				Game1.createItemDebris(ItemRegistry.Create("(O)73"), new Vector2(point.X, point.Y) * 64f, -1, islandLocation);
			}
		}
	}

	public virtual void broadcastPartyWideMail(string mail_key, PartyWideMessageQueue message_queue = PartyWideMessageQueue.MailForTomorrow, bool no_letter = false)
	{
		mail_key = mail_key.Trim();
		mail_key = mail_key.Replace(Environment.NewLine, "");
		List<object> list = new List<object>();
		list.Add(mail_key);
		list.Add((int)message_queue);
		list.Add(no_letter);
		_performPartyWideMail(mail_key, message_queue, no_letter);
		if (Game1.IsServer)
		{
			foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
			{
				if (otherFarmer.Value.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
				{
					Game1.server.sendMessage(otherFarmer.Value.UniqueMultiplayerID, 22, Game1.player, list.ToArray());
				}
			}
			return;
		}
		if (Game1.IsClient)
		{
			Game1.client.sendMessage(22, list.ToArray());
		}
	}

	public virtual void broadcastGrandpaReevaluation()
	{
		Game1.getFarm().requestGrandpaReevaluation();
		if (Game1.IsServer)
		{
			foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
			{
				if (otherFarmer.Value.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
				{
					Game1.server.sendMessage(otherFarmer.Value.UniqueMultiplayerID, 26, Game1.player);
				}
			}
			return;
		}
		if (Game1.IsClient)
		{
			Game1.client.sendMessage(26);
		}
	}

	/// <summary>Broadcast a global popup message.</summary>
	/// <param name="translationKey">The translation key for the message text.</param>
	/// <param name="onlyShowIfEmpty">Whether to show the message only when no other messages are showing.</param>
	/// <param name="location">The location where players will see the message, or <see langword="null" /> to show it everywhere.</param>
	/// <param name="substitutions">The token substitutions for placeholders in the translation text, if any.</param>
	public virtual void broadcastGlobalMessage(string translationKey, bool onlyShowIfEmpty = false, GameLocation location = null, params string[] substitutions)
	{
		if ((!onlyShowIfEmpty || Game1.hudMessages.Count == 0) && (location == null || location.NameOrUniqueName == Game1.player.currentLocation?.NameOrUniqueName))
		{
			string[] array = new string[substitutions.Length];
			for (int i = 0; i < substitutions.Length; i++)
			{
				array[i] = TokenParser.ParseText(substitutions[i]);
			}
			LocalizedContentManager content = Game1.content;
			object[] substitutions2 = array;
			Game1.showGlobalMessage(content.LoadString(translationKey, substitutions2));
		}
		List<object> list = new List<object>
		{
			translationKey,
			onlyShowIfEmpty,
			location?.NameOrUniqueName ?? "",
			substitutions.Length
		};
		list.AddRange(substitutions);
		if (Game1.IsServer)
		{
			foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
			{
				if (otherFarmer.Value.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
				{
					Game1.server.sendMessage(otherFarmer.Value.UniqueMultiplayerID, 21, Game1.player, list.ToArray());
				}
			}
			return;
		}
		if (Game1.IsClient)
		{
			Game1.client.sendMessage(21, list.ToArray());
		}
	}

	public virtual NetRoot<T> readObjectFull<T>(BinaryReader reader) where T : class, INetObject<INetSerializable>
	{
		NetRoot<T> netRoot = NetRoot<T>.Connect(reader);
		netRoot.Clock.InterpolationTicks = defaultInterpolationTicks;
		return netRoot;
	}

	protected virtual BinaryWriter createWriter(Stream stream)
	{
		BinaryWriter binaryWriter = new BinaryWriter(stream);
		if (logging.IsLogging)
		{
			binaryWriter = new LoggingBinaryWriter(binaryWriter);
		}
		return binaryWriter;
	}

	public virtual void writeObjectFull<T>(BinaryWriter writer, NetRoot<T> root, long? peer) where T : class, INetObject<INetSerializable>
	{
		root.CreateConnectionPacket(writer, peer);
	}

	public virtual byte[] writeObjectFullBytes<T>(NetRoot<T> root, long? peer) where T : class, INetObject<INetSerializable>
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter writer = createWriter(memoryStream);
		root.CreateConnectionPacket(writer, peer);
		return memoryStream.ToArray();
	}

	public virtual void readObjectDelta<T>(BinaryReader reader, NetRoot<T> root) where T : class, INetObject<INetSerializable>
	{
		root.Read(reader);
	}

	public virtual void writeObjectDelta<T>(BinaryWriter writer, NetRoot<T> root) where T : class, INetObject<INetSerializable>
	{
		root.Write(writer);
	}

	public virtual byte[] writeObjectDeltaBytes<T>(NetRoot<T> root) where T : class, INetObject<INetSerializable>
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter writer = createWriter(memoryStream);
		root.Write(writer);
		return memoryStream.ToArray();
	}

	public virtual NetFarmerRoot readFarmer(BinaryReader reader)
	{
		NetFarmerRoot netFarmerRoot = new NetFarmerRoot();
		netFarmerRoot.ReadConnectionPacket(reader);
		netFarmerRoot.Clock.InterpolationTicks = defaultInterpolationTicks;
		return netFarmerRoot;
	}

	public virtual void addPlayer(NetFarmerRoot f)
	{
		long uniqueMultiplayerID = f.Value.UniqueMultiplayerID;
		f.Value.teamRoot = Game1.player.teamRoot;
		Game1.otherFarmers.Roots[uniqueMultiplayerID] = f;
		disconnectingFarmers.Remove(uniqueMultiplayerID);
		if (Game1.chatBox != null)
		{
			string sub = ChatBox.formattedUserNameLong(f.Value);
			Game1.chatBox.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_PlayerJoined", sub));
		}
	}

	public virtual void receivePlayerIntroduction(BinaryReader reader)
	{
		addPlayer(readFarmer(reader));
	}

	public virtual void broadcastPlayerIntroduction(NetFarmerRoot farmerRoot)
	{
		if (Game1.server == null)
		{
			return;
		}
		foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
		{
			if (farmerRoot.Value.UniqueMultiplayerID != otherFarmer.Value.UniqueMultiplayerID)
			{
				Game1.server.sendMessage(otherFarmer.Value.UniqueMultiplayerID, 2, farmerRoot.Value, Game1.server.getUserName(farmerRoot.Value.UniqueMultiplayerID), writeObjectFullBytes(farmerRoot, otherFarmer.Value.UniqueMultiplayerID));
			}
		}
	}

	public virtual void broadcastUserName(long farmerId, string userName)
	{
		if (Game1.server != null)
		{
			return;
		}
		foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
		{
			Farmer value = otherFarmer.Value;
			if (value.UniqueMultiplayerID != farmerId)
			{
				Game1.server.sendMessage(value.UniqueMultiplayerID, 16, Game1.serverHost.Value, farmerId, userName);
			}
		}
	}

	public virtual string getUserName(long id)
	{
		if (id == Game1.player.UniqueMultiplayerID)
		{
			return Game1.content.LoadString("Strings\\UI:Chat_SelfPlayerID");
		}
		if (Game1.server != null)
		{
			return Game1.server.getUserName(id);
		}
		if (Game1.client != null)
		{
			return Game1.client.getUserName(id);
		}
		return "?";
	}

	public virtual void playerDisconnected(long id)
	{
		if (Game1.otherFarmers.Roots.TryGetValue(id, out var value) && !disconnectingFarmers.Contains(id))
		{
			NetFarmerRoot netFarmerRoot = value as NetFarmerRoot;
			if (netFarmerRoot.Value.mount != null && Game1.IsMasterGame)
			{
				netFarmerRoot.Value.mount.dismount();
			}
			if (Game1.IsMasterGame)
			{
				netFarmerRoot.TargetValue.handleDisconnect();
				netFarmerRoot.TargetValue.companions.Clear();
				saveFarmhand(netFarmerRoot);
				netFarmerRoot.Value.handleDisconnect();
			}
			if (Game1.player.dancePartner.Value is Farmer && ((Farmer)Game1.player.dancePartner.Value).UniqueMultiplayerID == netFarmerRoot.Value.UniqueMultiplayerID)
			{
				Game1.player.dancePartner.Value = null;
			}
			if (Game1.chatBox != null)
			{
				Game1.chatBox.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_PlayerLeft", ChatBox.formattedUserNameLong(Game1.otherFarmers[id])));
			}
			disconnectingFarmers.Add(id);
		}
	}

	protected virtual void removeDisconnectedFarmers()
	{
		foreach (long disconnectingFarmer in disconnectingFarmers)
		{
			Game1.otherFarmers.Remove(disconnectingFarmer);
		}
		disconnectingFarmers.Clear();
	}

	public virtual void sendFarmhand()
	{
		(Game1.player.NetFields.Root as NetFarmerRoot).MarkReassigned();
	}

	protected virtual void saveFarmhand(NetFarmerRoot farmhand)
	{
		Game1.netWorldState.Value.SaveFarmhand(farmhand);
	}

	public virtual void saveFarmhands()
	{
		if (!Game1.IsMasterGame)
		{
			return;
		}
		foreach (NetRoot<Farmer> value in Game1.otherFarmers.Roots.Values)
		{
			saveFarmhand(value as NetFarmerRoot);
		}
	}

	public virtual void clientRemotelyDisconnected(DisconnectType disconnectType)
	{
		LogDisconnect(disconnectType);
		returnToMainMenu();
	}

	private void returnToMainMenu()
	{
		if (!Game1.game1.IsMainInstance)
		{
			GameRunner.instance.RemoveGameInstance(Game1.game1);
			return;
		}
		Game1.ExitToTitle(delegate
		{
			(Game1.activeClickableMenu as TitleMenu).skipToTitleButtons();
			TitleMenu.subMenu = new ConfirmationDialog(Game1.content.LoadString("Strings\\UI:Client_RemotelyDisconnected"), null)
			{
				okButton = 
				{
					visible = false
				}
			};
		});
	}

	public static bool ShouldLogDisconnect(DisconnectType disconnectType)
	{
		switch (disconnectType)
		{
		case DisconnectType.ClosedGame:
		case DisconnectType.ExitedToMainMenu:
		case DisconnectType.ExitedToMainMenu_FromFarmhandSelect:
		case DisconnectType.ServerOfflineMode:
		case DisconnectType.ServerFull:
		case DisconnectType.AcceptedOtherInvite:
			return false;
		default:
			return true;
		}
	}

	public static bool IsTimeout(DisconnectType disconnectType)
	{
		if ((uint)(disconnectType - 9) <= 2u)
		{
			return true;
		}
		return false;
	}

	public static void LogDisconnect(DisconnectType disconnectType)
	{
		if (ShouldLogDisconnect(disconnectType))
		{
			string text = "Disconnected at : " + DateTime.Now.ToLongTimeString() + " - " + disconnectType;
			if (Game1.client != null)
			{
				text = text + " Ping: " + Game1.client.GetPingToHost().ToString("0.#");
				text += ((Game1.client is LidgrenClient) ? " ip" : " friend/invite");
			}
			Program.WriteLog(Program.LogType.Disconnect, text, append: true);
		}
		Game1.log.Verbose("Disconnected: " + disconnectType);
	}

	public virtual void sendSharedAchievementMessage(int achievement)
	{
		if (Game1.IsClient)
		{
			Game1.client.sendMessage(20, achievement);
		}
		else
		{
			if (!Game1.IsServer)
			{
				return;
			}
			foreach (long key in Game1.otherFarmers.Keys)
			{
				Game1.server.sendMessage(key, 20, Game1.player, achievement);
			}
		}
	}

	public virtual void sendServerToClientsMessage(string message)
	{
		if (!Game1.IsServer)
		{
			return;
		}
		foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers)
		{
			otherFarmer.Value.queueMessage(18, Game1.player, message);
		}
	}

	public virtual void sendChatMessage(LocalizedContentManager.LanguageCode language, string message, long recipientID)
	{
		if (Game1.IsClient)
		{
			Game1.client.sendMessage(10, recipientID, language, message);
		}
		else
		{
			if (!Game1.IsServer)
			{
				return;
			}
			if (recipientID == AllPlayers)
			{
				foreach (long key in Game1.otherFarmers.Keys)
				{
					Game1.server.sendMessage(key, 10, Game1.player, recipientID, language, message);
				}
				return;
			}
			Game1.server.sendMessage(recipientID, 10, Game1.player, recipientID, language, message);
		}
	}

	public virtual void receiveChatMessage(Farmer sourceFarmer, long recipientID, LocalizedContentManager.LanguageCode language, string message)
	{
		if (Game1.chatBox != null)
		{
			int chatKind = 0;
			message = Program.sdk.FilterDirtyWords(message);
			if (recipientID != AllPlayers)
			{
				chatKind = 3;
			}
			Game1.chatBox.receiveChatMessage(sourceFarmer.UniqueMultiplayerID, chatKind, language, message);
		}
	}

	/// <summary>In multiplayer, send a chat messages to all connected players including the current player. In single-player, do nothing.</summary>
	/// <inheritdoc cref="M:StardewValley.Multiplayer.receiveChatInfoMessage(StardewValley.Farmer,System.String,System.String[])" />
	public virtual void globalChatInfoMessage(string messageKey, params string[] args)
	{
		if (Game1.IsMultiplayer || Game1.multiplayerMode != 0)
		{
			receiveChatInfoMessage(Game1.player, messageKey, args);
			sendChatInfoMessage(messageKey, args);
		}
	}

	/// <summary>Send a chat messages to all connected players including the current player.</summary>
	/// <inheritdoc cref="M:StardewValley.Multiplayer.receiveChatInfoMessage(StardewValley.Farmer,System.String,System.String[])" />
	public void globalChatInfoMessageEvenInSinglePlayer(string messageKey, params string[] args)
	{
		receiveChatInfoMessage(Game1.player, messageKey, args);
		sendChatInfoMessage(messageKey, args);
	}

	/// <summary>Send a chat messages to all connected players, excluding the current player.</summary>
	/// <inheritdoc cref="M:StardewValley.Multiplayer.receiveChatInfoMessage(StardewValley.Farmer,System.String,System.String[])" />
	protected virtual void sendChatInfoMessage(string messageKey, params string[] args)
	{
		if (Game1.IsClient)
		{
			Game1.client.sendMessage(15, messageKey, args);
		}
		else
		{
			if (!Game1.IsServer)
			{
				return;
			}
			foreach (long key in Game1.otherFarmers.Keys)
			{
				Game1.server.sendMessage(key, 15, Game1.player, messageKey, args);
			}
		}
	}

	/// <summary>Receive a chat message sent via a method like <see cref="M:StardewValley.Multiplayer.globalChatInfoMessage(System.String,System.String[])" /> or <see cref="M:StardewValley.Multiplayer.sendChatInfoMessage(System.String,System.String[])" />.</summary>
	/// <param name="sourceFarmer">The player who sent the message.</param>
	/// <param name="messageKey">The translation key to show. This is prefixed with <c>Strings\UI:Chat_</c> automatically.</param>
	/// <param name="args">The values with which to replace placeholders in the translation text. Localizable values should be <see cref="T:StardewValley.TokenizableStrings.TokenParser">tokenized strings</see> or special tokens like <see cref="F:StardewValley.Multiplayer.chat_token_aOrAn" />, since other players may not be playing in the same language.</param>
	protected virtual void receiveChatInfoMessage(Farmer sourceFarmer, string messageKey, string[] args)
	{
		if (Game1.chatBox == null)
		{
			return;
		}
		try
		{
			string[] array = args.Select((string arg) => arg.StartsWith("aOrAn:") ? Utility.AOrAn(TokenParser.ParseText(arg.Substring("aOrAn:".Length))) : TokenParser.ParseText(arg)).ToArray();
			ChatBox chatBox = Game1.chatBox;
			LocalizedContentManager content = Game1.content;
			string path = "Strings\\UI:Chat_" + messageKey;
			object[] substitutions = array;
			chatBox.addInfoMessage(content.LoadString(path, substitutions));
		}
		catch (ContentLoadException)
		{
		}
		catch (FormatException)
		{
		}
		catch (OverflowException)
		{
		}
		catch (KeyNotFoundException)
		{
		}
	}

	public virtual void parseServerToClientsMessage(string message)
	{
		if (!Game1.IsClient)
		{
			return;
		}
		switch (message)
		{
		case "festivalEvent":
			if (Game1.currentLocation.currentEvent != null)
			{
				Game1.currentLocation.currentEvent.forceFestivalContinue();
			}
			break;
		case "endFest":
			if (Game1.CurrentEvent != null)
			{
				Game1.CurrentEvent.forceEndFestival(Game1.player);
			}
			break;
		case "trainApproach":
			if (Game1.getLocationFromName("Railroad") is Railroad railroad)
			{
				railroad.PlayTrainApproach();
			}
			break;
		}
	}

	public virtual IEnumerable<GameLocation> activeLocations()
	{
		if (Game1.currentLocation != null)
		{
			yield return Game1.currentLocation;
		}
		foreach (GameLocation location in Game1.locations)
		{
			if (!isAlwaysActiveLocation(location))
			{
				continue;
			}
			foreach (GameLocation item in _GetActiveLocationsHere(location))
			{
				yield return item;
			}
		}
	}

	protected virtual IEnumerable<GameLocation> _GetActiveLocationsHere(GameLocation location)
	{
		if (location != Game1.currentLocation)
		{
			yield return location;
		}
		foreach (Building building in location.buildings)
		{
			GameLocation indoors = building.GetIndoors();
			if (indoors == null || (indoors.isAlwaysActive.Value && building.GetIndoorsType() == IndoorsType.Global))
			{
				continue;
			}
			foreach (GameLocation item in _GetActiveLocationsHere(indoors))
			{
				yield return item;
			}
		}
	}

	public virtual bool isAlwaysActiveLocation(GameLocation location)
	{
		if (location.Root != null && location.Root.Value != location && isAlwaysActiveLocation(location.Root.Value))
		{
			return true;
		}
		return location.isAlwaysActive.Value;
	}

	protected virtual void readActiveLocation(IncomingMessage msg)
	{
		bool flag = msg.Reader.ReadBoolean();
		NetRoot<GameLocation> netRoot = readObjectFull<GameLocation>(msg.Reader);
		if (isAlwaysActiveLocation(netRoot.Value))
		{
			for (int i = 0; i < Game1.locations.Count; i++)
			{
				GameLocation gameLocation = Game1.locations[i];
				if (!gameLocation.Equals(netRoot.Value))
				{
					continue;
				}
				if (gameLocation == netRoot.Value)
				{
					break;
				}
				if (gameLocation != null)
				{
					if (Game1.currentLocation == gameLocation)
					{
						Game1.currentLocation = netRoot.Value;
					}
					if (Game1.player.currentLocation == gameLocation)
					{
						Game1.player.currentLocation = netRoot.Value;
					}
					Game1.removeLocationFromLocationLookup(gameLocation);
					gameLocation.OnRemoved();
				}
				Game1.locations[i] = netRoot.Value;
				break;
			}
		}
		if (Game1.locationRequest != null || flag)
		{
			if (Game1.locationRequest != null)
			{
				Game1.currentLocation = Game1.findStructure(netRoot.Value, Game1.locationRequest.Name) ?? netRoot.Value;
			}
			else if (flag)
			{
				Game1.currentLocation = netRoot.Value;
			}
			if (Game1.locationRequest != null)
			{
				Game1.locationRequest.Location = Game1.currentLocation;
				Game1.locationRequest.Loaded(Game1.currentLocation);
			}
			if (Game1.client != null || !(Game1.activeClickableMenu is TitleMenu) || (TitleMenu.subMenu as FarmhandMenu)?.client == null)
			{
				Game1.currentLocation.resetForPlayerEntry();
			}
			Game1.player.currentLocation = Game1.currentLocation;
			Game1.locationRequest?.Warped(Game1.currentLocation);
			Game1.currentLocation.updateSeasonalTileSheets();
			Game1.locationRequest = null;
		}
	}

	public virtual bool isActiveLocation(GameLocation location)
	{
		if (Game1.IsMasterGame)
		{
			return true;
		}
		if ((object)location?.Root == null)
		{
			return false;
		}
		if (Game1.currentLocation != null && Game1.currentLocation.Root != null && Game1.currentLocation.Root.Value == location.Root.Value)
		{
			return true;
		}
		return isAlwaysActiveLocation(location);
	}

	protected virtual GameLocation readLocation(BinaryReader reader)
	{
		bool isStructure = reader.ReadByte() != 0;
		GameLocation locationFromName = Game1.getLocationFromName(reader.ReadString(), isStructure);
		if (locationFromName == null || locationRoot(locationFromName) == null)
		{
			return null;
		}
		if (!isActiveLocation(locationFromName))
		{
			return null;
		}
		return locationFromName;
	}

	protected virtual LocationRequest readLocationRequest(BinaryReader reader)
	{
		bool isStructure = reader.ReadByte() != 0;
		return Game1.getLocationRequest(reader.ReadString(), isStructure);
	}

	protected virtual NPC readNPC(BinaryReader reader)
	{
		GameLocation gameLocation = readLocation(reader);
		Guid id = reader.ReadGuid();
		if (!gameLocation.characters.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public virtual void readSprites(BinaryReader reader, GameLocation location, Action<TemporaryAnimatedSprite> assignSprite)
	{
		int num = reader.ReadInt32();
		TemporaryAnimatedSprite[] array = new TemporaryAnimatedSprite[num];
		for (int i = 0; i < num; i++)
		{
			TemporaryAnimatedSprite temporaryAnimatedSprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite();
			temporaryAnimatedSprite.Read(reader, location);
			temporaryAnimatedSprite.ticksBeforeAnimationStart += interpolationTicks();
			array[i] = temporaryAnimatedSprite;
			assignSprite(temporaryAnimatedSprite);
		}
	}

	protected virtual void receiveTeamDelta(BinaryReader msg)
	{
		readObjectDelta(msg, Game1.player.teamRoot);
	}

	protected virtual void receiveNewDaySync(IncomingMessage msg)
	{
		if (!Game1.newDaySync.hasInstance() && msg.SourceFarmer == Game1.serverHost.Value)
		{
			Game1.NewDay(0f);
		}
		if (Game1.newDaySync.hasInstance())
		{
			Game1.newDaySync.receiveMessage(msg);
		}
	}

	protected virtual void receiveFarmerGainExperience(IncomingMessage msg)
	{
		if (msg.SourceFarmer == Game1.serverHost.Value)
		{
			int which = msg.Reader.ReadInt32();
			int howMuch = msg.Reader.ReadInt32();
			Game1.player.gainExperience(which, howMuch);
		}
	}

	protected virtual void receiveSharedAchievement(IncomingMessage msg)
	{
		Game1.getAchievement(msg.Reader.ReadInt32(), allowBroadcasting: false);
	}

	protected virtual void receiveRemoveLocationFromLookup(IncomingMessage msg)
	{
		Game1.removeLocationFromLocationLookup(msg.Reader.ReadString());
	}

	protected virtual void receivePartyWideMail(IncomingMessage msg)
	{
		string mail_key = msg.Reader.ReadString();
		PartyWideMessageQueue message_queue = (PartyWideMessageQueue)msg.Reader.ReadInt32();
		bool no_letter = msg.Reader.ReadBoolean();
		_performPartyWideMail(mail_key, message_queue, no_letter);
	}

	protected void _performPartyWideMail(string mail_key, PartyWideMessageQueue message_queue, bool no_letter)
	{
		switch (message_queue)
		{
		case PartyWideMessageQueue.MailForTomorrow:
			Game1.addMailForTomorrow(mail_key, no_letter);
			break;
		case PartyWideMessageQueue.SeenMail:
			Game1.addMail(mail_key, no_letter);
			break;
		}
		if (no_letter)
		{
			mail_key += "%&NL&%";
		}
		switch (message_queue)
		{
		case PartyWideMessageQueue.MailForTomorrow:
			mail_key = "%&MFT&%" + mail_key;
			break;
		case PartyWideMessageQueue.SeenMail:
			mail_key = "%&SM&%" + mail_key;
			break;
		}
		if (Game1.IsMasterGame && !Game1.player.team.broadcastedMail.Contains(mail_key))
		{
			Game1.player.team.broadcastedMail.Add(mail_key);
		}
	}

	protected void receiveForceKick()
	{
		if (!Game1.IsServer)
		{
			Disconnect(DisconnectType.Kicked);
			returnToMainMenu();
		}
	}

	protected virtual void receiveGlobalMessage(IncomingMessage msg)
	{
		string path = msg.Reader.ReadString();
		bool num = msg.Reader.ReadBoolean();
		string text = msg.Reader.ReadString();
		if ((!num || Game1.hudMessages.Count <= 0) && (string.IsNullOrEmpty(text) || !(text != Game1.player.currentLocation?.NameOrUniqueName)))
		{
			int num2 = msg.Reader.ReadInt32();
			object[] array = new object[num2];
			for (int i = 0; i < num2; i++)
			{
				array[i] = TokenParser.ParseText(msg.Reader.ReadString());
			}
			Game1.showGlobalMessage(Game1.content.LoadString(path, array));
		}
	}

	protected void receiveStartNewDaySync()
	{
		Game1.newDaySync.flagServerReady();
	}

	protected void receiveReadySync(IncomingMessage msg)
	{
		Game1.netReady.ProcessMessage(msg);
	}

	protected void receiveChestHitSync(IncomingMessage msg)
	{
		Game1.player.team.chestHit.ProcessMessage(msg);
	}

	protected void receiveDedicatedServerSync(IncomingMessage msg)
	{
		Game1.dedicatedServer.ProcessMessage(msg);
	}

	public virtual void processIncomingMessage(IncomingMessage msg)
	{
		GameLocation location;
		int tileX;
		int tileY;
		LocationRequest request;
		switch (msg.MessageType)
		{
		case 0:
		{
			long id = msg.Reader.ReadInt64();
			NetFarmerRoot netFarmerRoot = farmerRoot(id);
			if (netFarmerRoot != null)
			{
				readObjectDelta(msg.Reader, netFarmerRoot);
			}
			break;
		}
		case 3:
			readActiveLocation(msg);
			break;
		case 6:
			location = readLocation(msg.Reader);
			if (location != null)
			{
				readObjectDelta(msg.Reader, location.Root);
			}
			break;
		case 7:
			location = readLocation(msg.Reader);
			if (location != null)
			{
				readSprites(msg.Reader, location, delegate(TemporaryAnimatedSprite sprite)
				{
					location.temporarySprites.Add(sprite);
				});
			}
			break;
		case 8:
		{
			NPC nPC = readNPC(msg.Reader);
			location = readLocation(msg.Reader);
			if (nPC != null && location != null)
			{
				Game1.warpCharacter(nPC, location, msg.Reader.ReadVector2());
			}
			break;
		}
		case 4:
		{
			string eventId = msg.Reader.ReadString();
			bool flag = msg.Reader.ReadBoolean();
			bool notify_when_done = msg.Reader.ReadBoolean();
			tileX = msg.Reader.ReadInt32();
			tileY = msg.Reader.ReadInt32();
			request = readLocationRequest(msg.Reader);
			GameLocation location_for_event_check = Game1.getLocationFromName(request.Name);
			if (location_for_event_check?.findEventById(eventId) == null)
			{
				Game1.log.Warn("Couldn't find event " + eventId + " for broadcast event!");
				break;
			}
			Farmer farmerActor = (flag ? (Game1.player.NetFields.Root as NetRoot<Farmer>).Clone().Value : (msg.SourceFarmer.NetFields.Root as NetRoot<Farmer>).Clone().Value);
			Point oldTile = Game1.player.TilePoint;
			string oldLocation = Game1.player.currentLocation.NameOrUniqueName;
			int direction = Game1.player.facingDirection.Value;
			Game1.player.locationBeforeForcedEvent.Value = oldLocation;
			request.OnWarp += delegate
			{
				farmerActor.currentLocation = Game1.currentLocation;
				farmerActor.completelyStopAnimatingOrDoingAction();
				farmerActor.UsingTool = false;
				farmerActor.Items.Clear();
				farmerActor.hidden.Value = false;
				Event obj = Game1.currentLocation.findEventById(eventId, farmerActor);
				obj.notifyWhenDone = notify_when_done;
				obj.notifyLocationName = location_for_event_check.NameOrUniqueName;
				obj.notifyLocationIsStructure = (request.IsStructure ? ((byte)1) : ((byte)0));
				Game1.currentLocation.startEvent(obj);
				farmerActor.Position = Game1.player.Position;
				Game1.warpingForForcedRemoteEvent = false;
				string value = Game1.player.locationBeforeForcedEvent.Value;
				Game1.player.locationBeforeForcedEvent.Value = null;
				obj.setExitLocation(oldLocation, oldTile.X, oldTile.Y);
				Game1.player.locationBeforeForcedEvent.Value = value;
				Game1.player.orientationBeforeEvent = direction;
			};
			Game1.remoteEventQueue.Add(PerformForcedEvent);
			break;
		}
		case 10:
		{
			long recipientID = msg.Reader.ReadInt64();
			LocalizedContentManager.LanguageCode language = msg.Reader.ReadEnum<LocalizedContentManager.LanguageCode>();
			string message = msg.Reader.ReadString();
			receiveChatMessage(msg.SourceFarmer, recipientID, language, message);
			break;
		}
		case 15:
		{
			string messageKey = msg.Reader.ReadString();
			string[] array = new string[msg.Reader.ReadByte()];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = msg.Reader.ReadString();
			}
			receiveChatInfoMessage(msg.SourceFarmer, messageKey, array);
			break;
		}
		case 2:
			receivePlayerIntroduction(msg.Reader);
			break;
		case 12:
			receiveWorldState(msg.Reader);
			break;
		case 13:
			receiveTeamDelta(msg.Reader);
			break;
		case 14:
			receiveNewDaySync(msg);
			break;
		case 18:
			parseServerToClientsMessage(msg.Reader.ReadString());
			break;
		case 19:
			playerDisconnected(msg.SourceFarmer.UniqueMultiplayerID);
			break;
		case 17:
			receiveFarmerGainExperience(msg);
			break;
		case 25:
			receiveFarmerKilledMonster(msg);
			break;
		case 20:
			receiveSharedAchievement(msg);
			break;
		case 21:
			receiveGlobalMessage(msg);
			break;
		case 22:
			receivePartyWideMail(msg);
			break;
		case 27:
			receiveNutDig(msg);
			break;
		case 23:
			receiveForceKick();
			break;
		case 24:
			receiveRemoveLocationFromLookup(msg);
			break;
		case 26:
			receiveRequestGrandpaReevaluation(msg);
			break;
		case 28:
			receivePassoutRequest(msg);
			break;
		case 29:
			receivePassout(msg);
			break;
		case 30:
			receiveStartNewDaySync();
			break;
		case 31:
			receiveReadySync(msg);
			break;
		case 32:
			receiveChestHitSync(msg);
			break;
		case 33:
			receiveDedicatedServerSync(msg);
			break;
		case 127:
			Game1.log.Warn("Unexpectedly received a compressed multiplayer message that wasn't decompressed by the net client.");
			break;
		}
		void PerformForcedEvent()
		{
			Game1.warpingForForcedRemoteEvent = true;
			Game1.player.completelyStopAnimatingOrDoingAction();
			Game1.warpFarmer(request, tileX, tileY, Game1.player.FacingDirection);
		}
	}

	public virtual void StartLocalMultiplayerServer()
	{
		Game1.server = new GameServer(local_multiplayer: true);
		Game1.server.startServer();
	}

	public virtual void StartServer()
	{
		Game1.server = new GameServer();
		Game1.server.startServer();
	}

	public virtual void Disconnect(DisconnectType disconnectType)
	{
		if (Game1.server != null)
		{
			Game1.server.stopServer();
			Game1.server = null;
			foreach (long key in Game1.otherFarmers.Keys)
			{
				playerDisconnected(key);
			}
		}
		if (Game1.client != null)
		{
			sendFarmhand();
			UpdateLate(forceSync: true);
			Game1.client.disconnect();
			Game1.client = null;
		}
		Game1.otherFarmers.Clear();
		LogDisconnect(disconnectType);
	}

	protected virtual void updatePendingConnections()
	{
		switch (Game1.multiplayerMode)
		{
		case 2:
			if (Game1.server == null && Game1.options.enableServer)
			{
				StartServer();
			}
			break;
		case 1:
			if (Game1.client != null && !Game1.client.readyToPlay)
			{
				Game1.client.receiveMessages();
			}
			break;
		}
	}

	public void UpdateLoading()
	{
		updatePendingConnections();
		if (Game1.server != null)
		{
			Game1.server.receiveMessages();
		}
	}

	public virtual void UpdateEarly()
	{
		updatePendingConnections();
		if (Game1.multiplayerMode == 2 && Game1.serverHost == null && Game1.options.enableServer)
		{
			Game1.server.initializeHost();
		}
		if (Game1.server != null)
		{
			Game1.server.receiveMessages();
		}
		else if (Game1.client != null)
		{
			Game1.client.receiveMessages();
		}
		updateRoots();
		if (Game1.CurrentEvent == null)
		{
			removeDisconnectedFarmers();
		}
	}

	public virtual void UpdateLate(bool forceSync = false)
	{
		if (Game1.multiplayerMode != 0)
		{
			if (!allowSyncDelay() || forceSync || Game1.ticks % farmerDeltaBroadcastPeriod == 0)
			{
				broadcastFarmerDeltas();
			}
			if (!allowSyncDelay() || forceSync || Game1.ticks % locationDeltaBroadcastPeriod == 0)
			{
				broadcastLocationDeltas();
			}
			if (!allowSyncDelay() || forceSync || Game1.ticks % worldStateDeltaBroadcastPeriod == 0)
			{
				broadcastWorldStateDeltas();
			}
		}
		if (Game1.server != null)
		{
			Game1.server.sendMessages();
		}
		if (Game1.client != null)
		{
			Game1.client.sendMessages();
		}
	}

	public virtual void inviteAccepted()
	{
		if (!(Game1.activeClickableMenu is TitleMenu titleMenu))
		{
			return;
		}
		IClickableMenu subMenu = TitleMenu.subMenu;
		if (subMenu != null)
		{
			if (subMenu is FarmhandMenu || subMenu is CoopMenu)
			{
				TitleMenu.subMenu = new FarmhandMenu();
			}
		}
		else
		{
			titleMenu.performButtonAction("Invite");
		}
	}

	public virtual Client InitClient(Client client)
	{
		return client;
	}

	public virtual Server InitServer(Server server)
	{
		return server;
	}
}
