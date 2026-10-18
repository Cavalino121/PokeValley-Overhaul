using System;
using Lidgren.Network;

namespace StardewValley.Network;

public class LidgrenClient : HookableClient
{
	public string address;

	public NetClient client;

	private bool serverDiscovered;

	private int maxRetryAttempts;

	private int retryMs = 10000;

	private double lastAttemptMs;

	private int retryAttempts;

	private float lastLatencyMs;

	public LidgrenClient(string address)
	{
		this.address = address;
	}

	public override string getUserID()
	{
		return "";
	}

	public override float GetPingToHost()
	{
		return lastLatencyMs / 2f;
	}

	protected override string getHostUserName()
	{
		return client?.ServerConnection?.RemoteEndPoint?.Address?.ToString() ?? "";
	}

	protected override void connectImpl()
	{
		NetPeerConfiguration netPeerConfiguration = new NetPeerConfiguration("StardewValley");
		netPeerConfiguration.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
		netPeerConfiguration.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
		netPeerConfiguration.ConnectionTimeout = 30f;
		netPeerConfiguration.PingInterval = 5f;
		netPeerConfiguration.MaximumTransmissionUnit = 1200;
		client = new NetClient(netPeerConfiguration);
		client.Start();
		attemptConnection();
	}

	private void attemptConnection()
	{
		int serverPort = 24642;
		if (address.Contains(':'))
		{
			string[] array = address.Split(':');
			address = array[0];
			try
			{
				serverPort = Convert.ToInt32(array[1]);
			}
			catch (Exception)
			{
				serverPort = 24642;
			}
		}
		client.DiscoverKnownPeer(address, serverPort);
		lastAttemptMs = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
	}

	public override void disconnect(bool neatly = true)
	{
		if (client == null)
		{
			return;
		}
		if (client.ConnectionStatus != NetConnectionStatus.Disconnected && client.ConnectionStatus != NetConnectionStatus.Disconnecting)
		{
			if (neatly)
			{
				sendMessage(new OutgoingMessage(19, Game1.player));
			}
			client.FlushSendQueue();
			client.Disconnect("");
			client.FlushSendQueue();
		}
		connectionMessage = null;
	}

	protected virtual bool validateProtocol(string version)
	{
		return version == Multiplayer.protocolVersion;
	}

	protected override void receiveMessagesImpl()
	{
		if (client != null && !serverDiscovered && DateTime.UtcNow.TimeOfDay.TotalMilliseconds >= lastAttemptMs + (double)retryMs && retryAttempts < maxRetryAttempts)
		{
			attemptConnection();
			retryAttempts++;
		}
		NetIncomingMessage netIncomingMessage;
		while ((netIncomingMessage = client.ReadMessage()) != null)
		{
			switch (netIncomingMessage.MessageType)
			{
			case NetIncomingMessageType.ConnectionLatencyUpdated:
				readLatency(netIncomingMessage);
				break;
			case NetIncomingMessageType.DiscoveryResponse:
				if (!serverDiscovered)
				{
					Game1.log.Verbose("Found server at " + netIncomingMessage.SenderEndPoint);
					string text2 = netIncomingMessage.ReadString();
					if (validateProtocol(text2))
					{
						serverName = netIncomingMessage.ReadString();
						receiveHandshake(netIncomingMessage);
						serverDiscovered = true;
						break;
					}
					Game1.log.Warn($"Failed to connect. The server's protocol ({text2}) does not match our own ({Multiplayer.protocolVersion}).");
					connectionMessage = Game1.content.LoadString("Strings\\UI:CoopMenu_FailedProtocolVersion");
					client.Disconnect("");
				}
				break;
			case NetIncomingMessageType.Data:
				parseDataMessageFromServer(netIncomingMessage);
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
			}
		}
	}

	private void readLatency(NetIncomingMessage msg)
	{
		lastLatencyMs = msg.ReadFloat() * 1000f;
	}

	private void receiveHandshake(NetIncomingMessage msg)
	{
		client.Connect(msg.SenderEndPoint.Address.ToString(), msg.SenderEndPoint.Port);
	}

	private void statusChanged(NetIncomingMessage message)
	{
		NetConnectionStatus netConnectionStatus = (NetConnectionStatus)message.ReadByte();
		if (netConnectionStatus == NetConnectionStatus.Disconnected || netConnectionStatus == NetConnectionStatus.Disconnecting)
		{
			string message2 = message.ReadString();
			clientRemotelyDisconnected(netConnectionStatus, message2);
		}
	}

	private void clientRemotelyDisconnected(NetConnectionStatus status, string message)
	{
		timedOut = true;
		if (status == NetConnectionStatus.Disconnected)
		{
			if (message == Multiplayer.kicked)
			{
				pendingDisconnect = Multiplayer.DisconnectType.Kicked;
			}
			else
			{
				pendingDisconnect = Multiplayer.DisconnectType.LidgrenTimeout;
			}
		}
		else
		{
			pendingDisconnect = Multiplayer.DisconnectType.LidgrenDisconnect_Unknown;
		}
	}

	protected virtual void sendMessageImpl(OutgoingMessage message)
	{
		NetOutgoingMessage netOutgoingMessage = client.CreateMessage();
		LidgrenMessageUtils.WriteMessage(message, netOutgoingMessage);
		client.SendMessage(netOutgoingMessage, NetDeliveryMethod.ReliableOrdered);
		bandwidthLogger?.RecordBytesUp(netOutgoingMessage.LengthBytes);
	}

	public override void sendMessage(OutgoingMessage message)
	{
		base.OnSendingMessage(message, sendMessageImpl, delegate
		{
			sendMessageImpl(message);
		});
	}

	private void parseDataMessageFromServer(NetIncomingMessage dataMsg)
	{
		bandwidthLogger?.RecordBytesDown(dataMsg.LengthBytes);
		IncomingMessage message = new IncomingMessage();
		try
		{
			using NetBufferReadStream stream = new NetBufferReadStream(dataMsg);
			while (dataMsg.LengthBits - dataMsg.Position >= 8)
			{
				LidgrenMessageUtils.ReadStreamToMessage(stream, message);
				base.OnProcessingMessage(message, sendMessageImpl, delegate
				{
					processIncomingMessage(message);
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
}
