using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Lidgren.Network;

namespace StardewValley.Network;

public class LidgrenServer : HookableServer
{
	public const int defaultPort = 24642;

	public NetServer server;

	private HashSet<NetConnection> introductionsSent = new HashSet<NetConnection>();

	protected Bimap<long, NetConnection> peers = new Bimap<long, NetConnection>();

	public override int connectionsCount
	{
		get
		{
			if (server == null)
			{
				return 0;
			}
			return server.ConnectionsCount;
		}
	}

	public LidgrenServer(IGameServer gameServer)
		: base(gameServer)
	{
	}

	public override bool isConnectionActive(string connectionID)
	{
		foreach (NetConnection connection in server.Connections)
		{
			if (getConnectionId(connection) == connectionID && connection.Status == NetConnectionStatus.Connected)
			{
				return true;
			}
		}
		return false;
	}

	public override string getUserId(long farmerId)
	{
		if (!peers.ContainsLeft(farmerId))
		{
			return null;
		}
		return peers[farmerId].RemoteEndPoint.Address.ToString();
	}

	public override bool hasUserId(string userId)
	{
		foreach (NetConnection rightValue in peers.RightValues)
		{
			if (rightValue.RemoteEndPoint.Address.ToString().Equals(userId))
			{
				return true;
			}
		}
		return false;
	}

	public override string getUserName(long farmerId)
	{
		if (!peers.ContainsLeft(farmerId))
		{
			return null;
		}
		return peers[farmerId].RemoteEndPoint.Address.ToString();
	}

	public override float getPingToClient(long farmerId)
	{
		if (!peers.ContainsLeft(farmerId))
		{
			return -1f;
		}
		return peers[farmerId].AverageRoundtripTime / 2f * 1000f;
	}

	public override void setPrivacy(ServerPrivacy privacy)
	{
	}

	public override bool canAcceptIPConnections()
	{
		return true;
	}

	public override bool connected()
	{
		return server != null;
	}

	public override void initialize()
	{
		Game1.log.Verbose("Starting LAN server");
		NetPeerConfiguration netPeerConfiguration = new NetPeerConfiguration("StardewValley");
		netPeerConfiguration.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
		netPeerConfiguration.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
		netPeerConfiguration.Port = 24642;
		netPeerConfiguration.ConnectionTimeout = 30f;
		netPeerConfiguration.PingInterval = 5f;
		netPeerConfiguration.MaximumConnections = Game1.multiplayer.playerLimit * 2;
		netPeerConfiguration.MaximumTransmissionUnit = 1200;
		server = new NetServer(netPeerConfiguration);
		server.Start();
	}

	public override void stopServer()
	{
		Game1.log.Verbose("Stopping LAN server");
		server.Shutdown("Server shutting down...");
		server.FlushSendQueue();
		introductionsSent.Clear();
		peers.Clear();
	}

	public static bool IsLocal(string host_name_or_address)
	{
		if (string.IsNullOrEmpty(host_name_or_address))
		{
			return false;
		}
		try
		{
			IPAddress[] hostAddresses = Dns.GetHostAddresses(host_name_or_address);
			IPAddress[] local_ips = Dns.GetHostAddresses(Dns.GetHostName());
			return hostAddresses.Any((IPAddress host_ip) => IPAddress.IsLoopback(host_ip) || local_ips.Contains(host_ip));
		}
		catch
		{
			return false;
		}
	}

	public override void receiveMessages()
	{
		NetIncomingMessage netIncomingMessage;
		while ((netIncomingMessage = server.ReadMessage()) != null)
		{
			bandwidthLogger?.RecordBytesDown(netIncomingMessage.LengthBytes);
			switch (netIncomingMessage.MessageType)
			{
			case NetIncomingMessageType.DiscoveryRequest:
				if ((Game1.options.ipConnectionsEnabled || gameServer.IsLocalMultiplayerInitiatedServer()) && (!gameServer.IsLocalMultiplayerInitiatedServer() || IsLocal(netIncomingMessage.SenderEndPoint.Address.ToString())) && !gameServer.isUserBanned(netIncomingMessage.SenderEndPoint.Address.ToString()))
				{
					sendVersionInfo(netIncomingMessage);
				}
				break;
			case NetIncomingMessageType.ConnectionApproval:
				if (Game1.options.ipConnectionsEnabled || gameServer.IsLocalMultiplayerInitiatedServer())
				{
					netIncomingMessage.SenderConnection.Approve();
				}
				else
				{
					netIncomingMessage.SenderConnection.Deny();
				}
				break;
			case NetIncomingMessageType.Data:
				parseDataMessageFromClient(netIncomingMessage);
				break;
			case NetIncomingMessageType.DebugMessage:
			case NetIncomingMessageType.WarningMessage:
			case NetIncomingMessageType.ErrorMessage:
			{
				string text = netIncomingMessage.ReadString();
				Game1.log.Verbose(netIncomingMessage.MessageType.ToString() + ": " + text);
				Game1.debugOutput = text;
				break;
			}
			case NetIncomingMessageType.StatusChanged:
				statusChanged(netIncomingMessage);
				break;
			default:
				Game1.debugOutput = netIncomingMessage.ToString();
				break;
			}
			server.Recycle(netIncomingMessage);
		}
		foreach (NetConnection conn in server.Connections)
		{
			if (conn.Status == NetConnectionStatus.Connected && !introductionsSent.Contains(conn))
			{
				if (!gameServer.whenGameAvailable(delegate
				{
					gameServer.sendAvailableFarmhands("", getConnectionId(conn), delegate(OutgoingMessage msg)
					{
						sendMessage(conn, msg);
					});
				}, () => Game1.gameMode != 6))
				{
					Game1.log.Verbose("Postponing introduction message");
					sendMessage(conn, new OutgoingMessage(11, Game1.player, "Strings\\UI:Client_WaitForHostLoad"));
				}
				introductionsSent.Add(conn);
			}
		}
		bandwidthLogger?.Update();
	}

	private void sendVersionInfo(NetIncomingMessage message)
	{
		NetOutgoingMessage netOutgoingMessage = server.CreateMessage();
		netOutgoingMessage.Write(Multiplayer.protocolVersion);
		netOutgoingMessage.Write("StardewValley");
		server.SendDiscoveryResponse(netOutgoingMessage, message.SenderEndPoint);
		bandwidthLogger?.RecordBytesUp(netOutgoingMessage.LengthBytes);
	}

	private void statusChanged(NetIncomingMessage message)
	{
		switch ((NetConnectionStatus)message.ReadByte())
		{
		case NetConnectionStatus.Connected:
			onConnect(getConnectionId(message.SenderConnection));
			break;
		case NetConnectionStatus.Disconnecting:
		case NetConnectionStatus.Disconnected:
			onDisconnect(getConnectionId(message.SenderConnection));
			if (peers.ContainsRight(message.SenderConnection))
			{
				playerDisconnected(peers[message.SenderConnection]);
			}
			break;
		}
	}

	public override void kick(long disconnectee)
	{
		base.kick(disconnectee);
		if (peers.ContainsLeft(disconnectee))
		{
			peers[disconnectee].Disconnect(Multiplayer.kicked);
			server.FlushSendQueue();
			playerDisconnected(disconnectee);
		}
	}

	public override void playerDisconnected(long disconnectee)
	{
		base.playerDisconnected(disconnectee);
		introductionsSent.Remove(peers[disconnectee]);
		peers.RemoveLeft(disconnectee);
	}

	protected virtual void parseDataMessageFromClient(NetIncomingMessage dataMsg)
	{
		NetConnection peer = dataMsg.SenderConnection;
		IncomingMessage message = new IncomingMessage();
		try
		{
			using NetBufferReadStream stream = new NetBufferReadStream(dataMsg);
			while (dataMsg.LengthBits - dataMsg.Position >= 8)
			{
				LidgrenMessageUtils.ReadStreamToMessage(stream, message);
				base.OnProcessingMessage(message, delegate(OutgoingMessage outgoing)
				{
					sendMessage(peer, outgoing);
				}, delegate
				{
					if (peers.ContainsLeft(message.FarmerID) && peers[message.FarmerID] == peer)
					{
						gameServer.processIncomingMessage(message);
					}
					else if (message.MessageType == 2)
					{
						NetFarmerRoot farmer = Game1.multiplayer.readFarmer(message.Reader);
						gameServer.checkFarmhandRequest("", getConnectionId(dataMsg.SenderConnection), farmer, delegate(OutgoingMessage msg)
						{
							sendMessage(peer, msg);
						}, delegate
						{
							peers[farmer.Value.UniqueMultiplayerID] = peer;
						});
					}
				});
			}
		}
		finally
		{
			if (message != null)
			{
				((IDisposable)message).Dispose();
			}
		}
	}

	public string getConnectionId(NetConnection connection)
	{
		return "L_" + connection.RemoteUniqueIdentifier;
	}

	public override void sendMessage(long peerId, OutgoingMessage message)
	{
		if (peers.ContainsLeft(peerId))
		{
			sendMessage(peers[peerId], message);
		}
	}

	protected virtual void sendMessage(NetConnection connection, OutgoingMessage message)
	{
		NetOutgoingMessage netOutgoingMessage = server.CreateMessage();
		LidgrenMessageUtils.WriteMessage(message, netOutgoingMessage);
		server.SendMessage(netOutgoingMessage, connection, NetDeliveryMethod.ReliableOrdered);
		bandwidthLogger?.RecordBytesUp(netOutgoingMessage.LengthBytes);
	}

	public override void setLobbyData(string key, string value)
	{
	}
}
