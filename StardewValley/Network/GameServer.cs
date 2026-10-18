using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Network.Dedicated;
using StardewValley.SDKs.Steam;
using StardewValley.SaveSerialization;

namespace StardewValley.Network;

public class GameServer : IGameServer, IBandwidthMonitor
{
	internal List<Server> servers = new List<Server>();

	private Dictionary<Action, Func<bool>> pendingGameAvailableActions = new Dictionary<Action, Func<bool>>();

	/// <summary>A set of connections that are waiting to receive the list of available farmhands.</summary>
	private readonly HashSet<string> pendingAvailableFarmhands = new HashSet<string>();

	private List<Action> completedPendingActions = new List<Action>();

	private List<string> bannedUsers = new List<string>();

	protected bool _wasConnected;

	protected bool _isLocalMultiplayerInitiatedServer;

	public int connectionsCount => servers.Sum((Server s) => s.connectionsCount);

	public BandwidthLogger BandwidthLogger
	{
		get
		{
			foreach (Server server in servers)
			{
				if (server.connectionsCount > 0)
				{
					return server.BandwidthLogger;
				}
			}
			return null;
		}
	}

	public bool LogBandwidth
	{
		get
		{
			foreach (Server server in servers)
			{
				if (server.connectionsCount > 0)
				{
					return server.LogBandwidth;
				}
			}
			return false;
		}
		set
		{
			foreach (Server server in servers)
			{
				if (server.connectionsCount > 0)
				{
					server.LogBandwidth = value;
					break;
				}
			}
		}
	}

	public GameServer(bool local_multiplayer = false)
	{
		if (Game1.options != null)
		{
			Game1.options.enableServer = true;
		}
		servers.Add(Game1.multiplayer.InitServer(new LidgrenServer(this)));
		_isLocalMultiplayerInitiatedServer = local_multiplayer;
		if (!_isLocalMultiplayerInitiatedServer && Program.sdk.Networking != null)
		{
			if (Program.sdk.Networking is SteamNetHelper steamNetHelper)
			{
				servers.Add(steamNetHelper.CreateSteamServer(this));
			}
			Server server = Program.sdk.Networking.CreateServer(this);
			if (server != null)
			{
				servers.Add(server);
			}
		}
	}

	public bool isConnectionActive(string connectionId)
	{
		foreach (Server server in servers)
		{
			if (server.isConnectionActive(connectionId))
			{
				return true;
			}
		}
		return false;
	}

	public virtual void onConnect(string connectionID)
	{
		UpdateLocalOnlyFlag();
	}

	public virtual void onDisconnect(string connectionID)
	{
		UpdateLocalOnlyFlag();
	}

	public bool IsLocalMultiplayerInitiatedServer()
	{
		return _isLocalMultiplayerInitiatedServer;
	}

	public virtual void UpdateLocalOnlyFlag()
	{
		if (!Game1.game1.IsMainInstance)
		{
			return;
		}
		bool flag = true;
		HashSet<long> local_clients = new HashSet<long>();
		GameRunner.instance.ExecuteForInstances(delegate
		{
			Client client = Game1.client;
			if (client == null && Game1.activeClickableMenu is FarmhandMenu farmhandMenu)
			{
				client = farmhandMenu.client;
			}
			if (client is LidgrenClient lidgrenClient)
			{
				local_clients.Add(lidgrenClient.client.UniqueIdentifier);
			}
		});
		foreach (Server server in servers)
		{
			if (server is LidgrenServer lidgrenServer)
			{
				foreach (NetConnection connection in lidgrenServer.server.Connections)
				{
					if (!local_clients.Contains(connection.RemoteUniqueIdentifier))
					{
						flag = false;
						break;
					}
				}
			}
			else if (server.connectionsCount > 0)
			{
				flag = false;
				break;
			}
			if (!flag)
			{
				break;
			}
		}
		if (Game1.hasLocalClientsOnly != flag)
		{
			Game1.hasLocalClientsOnly = flag;
			if (Game1.hasLocalClientsOnly)
			{
				Game1.log.Verbose("Game has only local clients.");
			}
			else
			{
				Game1.log.Verbose("Game has remote clients.");
			}
		}
	}

	public string getInviteCode()
	{
		foreach (Server server in servers)
		{
			string inviteCode = server.getInviteCode();
			if (inviteCode != null)
			{
				return inviteCode;
			}
		}
		return null;
	}

	public string getUserName(long farmerId)
	{
		foreach (Server server in servers)
		{
			string userName = server.getUserName(farmerId);
			if (userName != null)
			{
				return userName;
			}
		}
		return null;
	}

	public float getPingToClient(long farmerId)
	{
		foreach (Server server in servers)
		{
			if (server.getPingToClient(farmerId) != -1f)
			{
				return server.getPingToClient(farmerId);
			}
		}
		return -1f;
	}

	protected void initialize()
	{
		foreach (Server server in servers)
		{
			server.initialize();
		}
		whenGameAvailable(updateLobbyData);
	}

	public void setPrivacy(ServerPrivacy privacy)
	{
		foreach (Server server in servers)
		{
			server.setPrivacy(privacy);
		}
		if (Game1.netWorldState != null && Game1.netWorldState.Value != null)
		{
			Game1.netWorldState.Value.ServerPrivacy = privacy;
		}
	}

	public void stopServer()
	{
		if (Game1.chatBox != null)
		{
			Game1.chatBox.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_DisablingServer"));
		}
		foreach (Server server in servers)
		{
			server.stopServer();
		}
	}

	public void receiveMessages()
	{
		foreach (Server server in servers)
		{
			server.receiveMessages();
		}
		completedPendingActions.Clear();
		foreach (Action key in pendingGameAvailableActions.Keys)
		{
			if (pendingGameAvailableActions[key]())
			{
				key();
				completedPendingActions.Add(key);
			}
		}
		foreach (Action completedPendingAction in completedPendingActions)
		{
			pendingGameAvailableActions.Remove(completedPendingAction);
		}
		completedPendingActions.Clear();
		if (Game1.chatBox == null)
		{
			return;
		}
		bool flag = anyServerConnected();
		if (_wasConnected != flag)
		{
			_wasConnected = flag;
			if (_wasConnected)
			{
				Game1.chatBox.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_StartingServer"));
			}
		}
	}

	public void sendMessage(long peerId, OutgoingMessage message)
	{
		foreach (Server server in servers)
		{
			server.sendMessage(peerId, message);
		}
	}

	public bool canAcceptIPConnections()
	{
		return servers.Select((Server s) => s.canAcceptIPConnections()).Aggregate(seed: false, (bool a, bool b) => a || b);
	}

	public bool canOfferInvite()
	{
		return servers.Select((Server s) => s.canOfferInvite()).Aggregate(seed: false, (bool a, bool b) => a || b);
	}

	public void offerInvite()
	{
		foreach (Server server in servers)
		{
			if (server.canOfferInvite())
			{
				server.offerInvite();
			}
		}
	}

	public bool anyServerConnected()
	{
		foreach (Server server in servers)
		{
			if (server.connected())
			{
				return true;
			}
		}
		return false;
	}

	public bool connected()
	{
		foreach (Server server in servers)
		{
			if (!server.connected())
			{
				return false;
			}
		}
		return true;
	}

	public void sendMessage(long peerId, byte messageType, Farmer sourceFarmer, params object[] data)
	{
		sendMessage(peerId, new OutgoingMessage(messageType, sourceFarmer, data));
	}

	public void sendMessages()
	{
		foreach (Farmer value in Game1.otherFarmers.Values)
		{
			foreach (OutgoingMessage item in value.messageQueue)
			{
				sendMessage(value.UniqueMultiplayerID, item);
			}
			value.messageQueue.Clear();
		}
	}

	public void startServer()
	{
		_wasConnected = false;
		Game1.log.Verbose("Starting server. Protocol version: " + Multiplayer.protocolVersion);
		initialize();
		if (Game1.netWorldState == null)
		{
			Game1.netWorldState = new NetRoot<NetWorldState>(new NetWorldState());
		}
		Game1.netWorldState.Clock.InterpolationTicks = 0;
		Game1.netWorldState.Value.UpdateFromGame1();
	}

	public void initializeHost()
	{
		if (Game1.serverHost == null)
		{
			Game1.serverHost = new NetFarmerRoot();
		}
		Game1.serverHost.Value = Game1.player;
		using (List<Server>.Enumerator enumerator = servers.GetEnumerator())
		{
			while (enumerator.MoveNext() && !enumerator.Current.PopulatePlatformData(Game1.player))
			{
			}
		}
		Game1.serverHost.MarkClean();
		Game1.serverHost.Clock.InterpolationTicks = Game1.multiplayer.defaultInterpolationTicks;
	}

	public void sendServerIntroduction(long peer)
	{
		sendMessage(peer, new OutgoingMessage(1, Game1.serverHost.Value, Game1.multiplayer.writeObjectFullBytes(Game1.serverHost, peer), Game1.multiplayer.writeObjectFullBytes(Game1.player.teamRoot, peer), Game1.multiplayer.writeObjectFullBytes(Game1.netWorldState, peer)));
		foreach (KeyValuePair<long, NetRoot<Farmer>> root in Game1.otherFarmers.Roots)
		{
			if (root.Key != Game1.player.UniqueMultiplayerID && root.Key != peer)
			{
				sendMessage(peer, new OutgoingMessage(2, root.Value.Value, getUserName(root.Value.Value.UniqueMultiplayerID), Game1.multiplayer.writeObjectFullBytes(root.Value, peer)));
			}
		}
	}

	public void kick(long disconnectee)
	{
		foreach (Server server in servers)
		{
			server.kick(disconnectee);
		}
	}

	public string ban(long farmerId)
	{
		string text = null;
		foreach (Server server in servers)
		{
			text = server.getUserId(farmerId);
			if (text != null)
			{
				break;
			}
		}
		if (text != null && !Game1.bannedUsers.ContainsKey(text))
		{
			string text2 = Game1.multiplayer.getUserName(farmerId);
			if (text2 == "" || text2 == text)
			{
				text2 = null;
			}
			Game1.bannedUsers.Add(text, text2);
			kick(farmerId);
			return text;
		}
		return null;
	}

	public void playerDisconnected(long disconnectee)
	{
		Game1.otherFarmers.TryGetValue(disconnectee, out var value);
		Game1.multiplayer.playerDisconnected(disconnectee);
		if (value == null)
		{
			return;
		}
		OutgoingMessage message = new OutgoingMessage(19, value);
		foreach (long key in Game1.otherFarmers.Keys)
		{
			if (key != disconnectee)
			{
				sendMessage(key, message);
			}
		}
	}

	public bool isGameAvailable()
	{
		bool flag = Game1.currentMinigame is Intro || Game1.Date.DayOfMonth == 0;
		bool flag2 = Game1.CurrentEvent != null && Game1.CurrentEvent.isWedding;
		bool flag3 = Game1.newDaySync.hasInstance() && !Game1.newDaySync.hasFinished();
		bool flag4 = Game1.player.team.demolishLock.IsLocked();
		if (!Game1.isFestival() && !flag2 && !flag && !flag3 && !flag4 && Game1.weddingsToday.Count == 0)
		{
			return Game1.gameMode != 6;
		}
		return false;
	}

	public bool whenGameAvailable(Action action, Func<bool> customAvailabilityCheck = null)
	{
		Func<bool> func = ((customAvailabilityCheck != null) ? customAvailabilityCheck : new Func<bool>(isGameAvailable));
		if (func())
		{
			action();
			return true;
		}
		pendingGameAvailableActions.Add(action, func);
		return false;
	}

	private void rejectFarmhandRequest(string userId, string connectionId, NetFarmerRoot farmer, Action<OutgoingMessage> sendMessage)
	{
		sendAvailableFarmhands(userId, connectionId, sendMessage);
		Game1.log.Verbose("Rejected request for farmhand " + ((farmer.Value != null) ? farmer.Value.UniqueMultiplayerID.ToString() : "???"));
	}

	public bool isUserBanned(string userID)
	{
		return Game1.bannedUsers.ContainsKey(userID);
	}

	private bool authCheck(string userID, Farmer farmhand)
	{
		if (!Game1.options.enableFarmhandCreation && !IsLocalMultiplayerInitiatedServer() && !farmhand.isCustomized.Value)
		{
			return false;
		}
		if (!(userID == "") && !(farmhand.userID.Value == ""))
		{
			return farmhand.userID.Value == userID;
		}
		return true;
	}

	public bool IsFarmhandAvailable(Farmer farmhand)
	{
		if (!Game1.netWorldState.Value.TryAssignFarmhandHome(farmhand))
		{
			return false;
		}
		Cabin obj = Utility.getHomeOfFarmer(farmhand) as Cabin;
		if (obj != null && obj.isInventoryOpen())
		{
			return false;
		}
		return true;
	}

	public void checkFarmhandRequest(string userId, string connectionId, NetFarmerRoot farmer, Action<OutgoingMessage> sendMessage, Action approve)
	{
		if (farmer.Value == null)
		{
			rejectFarmhandRequest(userId, connectionId, farmer, sendMessage);
			return;
		}
		long id = farmer.Value.UniqueMultiplayerID;
		if (isGameAvailable())
		{
			Check();
		}
		else
		{
			sendAvailableFarmhands(userId, connectionId, sendMessage);
		}
		void Check()
		{
			Farmer farmer2 = Game1.netWorldState.Value.farmhandData[farmer.Value.UniqueMultiplayerID];
			if (!isConnectionActive(connectionId))
			{
				Game1.log.Verbose("Rejected request for connection ID " + connectionId + ": Connection not active.");
			}
			else if (farmer2 == null)
			{
				Game1.log.Verbose("Rejected request for farmhand " + id + ": doesn't exist");
				rejectFarmhandRequest(userId, connectionId, farmer, sendMessage);
			}
			else if (!authCheck(userId, farmer2))
			{
				Game1.log.Verbose("Rejected request for farmhand " + id + ": authorization failure " + userId + " " + farmer2.userID.Value);
				rejectFarmhandRequest(userId, connectionId, farmer, sendMessage);
			}
			else if ((Game1.otherFarmers.ContainsKey(id) && !Game1.multiplayer.isDisconnecting(id)) || Game1.serverHost.Value.UniqueMultiplayerID == id)
			{
				Game1.log.Verbose("Rejected request for farmhand " + id + ": already in use");
				rejectFarmhandRequest(userId, connectionId, farmer, sendMessage);
			}
			else if (!IsFarmhandAvailable(farmer.Value))
			{
				Game1.log.Verbose("Rejected request for farmhand " + id + ": farmhand availability failed");
				rejectFarmhandRequest(userId, connectionId, farmer, sendMessage);
			}
			else if (!Game1.netWorldState.Value.TryAssignFarmhandHome(farmer.Value))
			{
				Game1.log.Verbose("Rejected request for farmhand " + id + ": farmhand has no assigned cabin, and none is available to assign.");
				rejectFarmhandRequest(userId, connectionId, farmer, sendMessage);
			}
			else
			{
				Game1.log.Verbose("Approved request for farmhand " + id);
				approve();
				Game1.updateCellarAssignments();
				Game1.multiplayer.addPlayer(farmer);
				Game1.multiplayer.broadcastPlayerIntroduction(farmer);
				foreach (GameLocation location in Game1.locations)
				{
					if (Game1.multiplayer.isAlwaysActiveLocation(location))
					{
						sendLocation(id, location);
					}
				}
				if (farmer.Value.disconnectDay.Value == Game1.MasterPlayer.stats.DaysPlayed)
				{
					GameLocation locationFromName = Game1.getLocationFromName(farmer.Value.disconnectLocation.Value);
					if (locationFromName != null && !Game1.multiplayer.isAlwaysActiveLocation(locationFromName))
					{
						sendLocation(id, locationFromName, force_current: true);
					}
				}
				else if (!string.IsNullOrEmpty(farmer.Value.lastSleepLocation.Value))
				{
					GameLocation locationFromName2 = Game1.getLocationFromName(farmer.Value.lastSleepLocation.Value);
					if (locationFromName2 != null && Game1.isLocationAccessible(locationFromName2.Name) && !Game1.multiplayer.isAlwaysActiveLocation(locationFromName2))
					{
						sendLocation(id, locationFromName2, force_current: true);
					}
				}
				sendServerIntroduction(id);
				updateLobbyData();
			}
		}
	}

	public void sendAvailableFarmhands(string userId, string connectionId, Action<OutgoingMessage> sendMessage)
	{
		if (!isGameAvailable())
		{
			sendMessage(new OutgoingMessage(11, Game1.player, "Strings\\UI:Client_WaitForHostAvailability"));
			if (pendingAvailableFarmhands.Contains(connectionId))
			{
				Game1.log.Verbose("Connection " + connectionId + " is already waiting to receive available farmhands");
				return;
			}
			Game1.log.Verbose("Postponing sending available farmhands to connection ID " + connectionId);
			pendingAvailableFarmhands.Add(connectionId);
			whenGameAvailable(delegate
			{
				pendingAvailableFarmhands.Remove(connectionId);
				if (isConnectionActive(connectionId))
				{
					sendAvailableFarmhands(userId, connectionId, sendMessage);
				}
				else
				{
					Game1.log.Verbose("Failed to send available farmhands to connection ID " + connectionId + ": Connection not active.");
				}
			});
			return;
		}
		Game1.log.Verbose("Sending available farmhands to connection ID " + connectionId);
		List<NetRef<Farmer>> list = new List<NetRef<Farmer>>();
		foreach (NetRef<Farmer> value in Game1.netWorldState.Value.farmhandData.FieldDict.Values)
		{
			if ((!value.Value.isActive() || Game1.multiplayer.isDisconnecting(value.Value.UniqueMultiplayerID)) && IsFarmhandAvailable(value.Value))
			{
				list.Add(value);
			}
		}
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(Game1.year);
		binaryWriter.Write(Game1.seasonIndex);
		binaryWriter.Write(Game1.dayOfMonth);
		binaryWriter.Write((byte)list.Count);
		foreach (NetRef<Farmer> item in list)
		{
			try
			{
				item.Serializer = SaveSerializer.GetSerializer(typeof(Farmer));
				item.WriteFull(binaryWriter);
			}
			finally
			{
				item.Serializer = null;
			}
		}
		memoryStream.Seek(0L, SeekOrigin.Begin);
		sendMessage(new OutgoingMessage(9, Game1.player, memoryStream.ToArray()));
	}

	public T GetServer<T>() where T : Server
	{
		foreach (Server server in servers)
		{
			if (server is T result)
			{
				return result;
			}
		}
		return null;
	}

	private void sendLocation(long peer, GameLocation location, bool force_current = false)
	{
		sendMessage(peer, 3, Game1.serverHost.Value, force_current, Game1.multiplayer.writeObjectFullBytes(Game1.multiplayer.locationRoot(location), peer));
	}

	private void warpFarmer(Farmer farmer, short x, short y, string name, bool isStructure)
	{
		GameLocation gameLocation = Game1.RequireLocation(name, isStructure);
		if (Game1.IsMasterGame)
		{
			gameLocation.hostSetup();
		}
		farmer.currentLocation = gameLocation;
		farmer.Position = new Vector2(x * 64, y * 64 - (farmer.Sprite.getHeight() - 32) + 16);
		sendLocation(farmer.UniqueMultiplayerID, gameLocation);
	}

	public void processIncomingMessage(IncomingMessage message)
	{
		switch (message.MessageType)
		{
		case 5:
		{
			short x = message.Reader.ReadInt16();
			short y = message.Reader.ReadInt16();
			string name = message.Reader.ReadString();
			byte b = message.Reader.ReadByte();
			bool isStructure = (b & 1) != 0;
			bool warpingForForcedRemoteEvent = (b & 2) != 0;
			bool flag = (b & 4) != 0;
			int facingDirection = 0;
			if ((b & 0x10) != 0)
			{
				facingDirection = 1;
			}
			else if ((b & 0x20) != 0)
			{
				facingDirection = 2;
			}
			else if ((b & 0x40) != 0)
			{
				facingDirection = 3;
			}
			if (flag)
			{
				warpFarmer(message.SourceFarmer, x, y, name, isStructure);
			}
			Game1.dedicatedServer.HandleFarmerWarp(new DedicatedServer.FarmerWarp(message.SourceFarmer, x, y, name, isStructure, facingDirection, warpingForForcedRemoteEvent));
			break;
		}
		case 2:
			message.Reader.ReadString();
			Game1.multiplayer.processIncomingMessage(message);
			break;
		case 10:
		{
			long num = message.Reader.ReadInt64();
			message.Reader.BaseStream.Position -= 8L;
			if (num == Multiplayer.AllPlayers || num == Game1.player.UniqueMultiplayerID)
			{
				Game1.multiplayer.processIncomingMessage(message);
			}
			rebroadcastClientMessage(message, num);
			break;
		}
		default:
			Game1.multiplayer.processIncomingMessage(message);
			break;
		}
		if (Game1.multiplayer.isClientBroadcastType(message.MessageType))
		{
			rebroadcastClientMessage(message, Multiplayer.AllPlayers);
		}
	}

	private void rebroadcastClientMessage(IncomingMessage message, long peerID)
	{
		OutgoingMessage message2 = new OutgoingMessage(message);
		foreach (long key in Game1.otherFarmers.Keys)
		{
			if (key != message.FarmerID && (peerID == Multiplayer.AllPlayers || key == peerID))
			{
				sendMessage(key, message2);
			}
		}
	}

	private void setLobbyData(string key, string value)
	{
		foreach (Server server in servers)
		{
			server.setLobbyData(key, value);
		}
	}

	private bool unclaimedFarmhandsExist()
	{
		foreach (Farmer value in Game1.netWorldState.Value.farmhandData.Values)
		{
			if (value.userID.Value == "")
			{
				return true;
			}
		}
		return false;
	}

	public void updateLobbyData()
	{
		setLobbyData("farmName", Game1.player.farmName.Value);
		setLobbyData("farmType", Convert.ToString(Game1.whichFarm));
		if (Game1.whichFarm == 7)
		{
			setLobbyData("modFarmType", Game1.GetFarmTypeID());
		}
		else
		{
			setLobbyData("modFarmType", "");
		}
		WorldDate worldDate = WorldDate.Now();
		setLobbyData("date", Convert.ToString(worldDate.TotalDays));
		IEnumerable<string> source = from farmhand in Game1.getAllFarmhands()
			select farmhand.userID.Value;
		setLobbyData("farmhands", string.Join(",", source.Where((string user) => user != "")));
		setLobbyData("newFarmhands", Convert.ToString(Game1.options.enableFarmhandCreation && unclaimedFarmhandsExist()));
	}
}
