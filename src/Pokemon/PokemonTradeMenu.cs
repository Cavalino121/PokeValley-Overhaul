using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

/// <summary>
/// Synchronized 2-Player Pokémon Trade Terminal menu.
/// In Multiplayer Co-op: connects 2 players over the network, allows selecting Pokémon,
/// live previews of each other's Pokémon, dual confirmation, and animated link cable transfer.
/// Also includes Solo Link Cable fallback with classic trainer presets so trade evolutions are always accessible.
/// </summary>
public class PokemonTradeMenu : IClickableMenu
{
	private const int MenuWidth = 940;
	private const int MenuHeight = 580;

	/// <summary>Prefix that routes a chat message to the trade terminal instead of the chat log.</summary>
	private const string PacketPrefix = "[POKE_TRADE]:";
	/// <summary>Maximum number of payload characters per chat message; larger packets are split.</summary>
	private const int MaxPayloadPerMessage = 300;
	/// <summary>Seconds before a half-received packet is discarded.</summary>
	private const float PacketTimeout = 20f;

	private sealed class PendingPacket
	{
		public int Total;
		public int Received;
		public string[] Parts;
		public double LastSeen;
	}

	private static readonly Dictionary<string, PendingPacket> _pendingPackets = new();

	public static PokemonTradeMenu Instance { get; private set; }

	public enum TradeState
	{
		WaitingForPartner,
		Selecting,
		Transferring,
		Complete
	}

	private TradeState _state = TradeState.WaitingForPartner;
	private bool _isSoloNpcMode = false;
	private long _partnerId = 0;
	private string _partnerName = "";
	private bool _myConfirmed = false;
	private bool _partnerConfirmed = false;

	private readonly ClickableTextureComponent _closeButton;
	private readonly ClickableComponent _tradeButton;
	private readonly ClickableComponent _soloModeButton;
	private readonly List<ClickableComponent> _playerSlots = new();

	private int _selectedPlayerSlot = -1;
	private PokemonInstance _offeredPartnerMon = null;

	// Animation
	private float _animTimer = 0f;
	private float _pingTimer = 0f;
	private Vector2 _ball1Pos = Vector2.Zero;
	private Vector2 _ball2Pos = Vector2.Zero;
	private Texture2D _ballTex;

	// NPC presets for Solo Mode
	public class TradePartnerOption
	{
		public string PartnerName { get; set; }
		public PokemonInstance Pokemon { get; set; }
	}
	private readonly List<TradePartnerOption> _npcPartners = new();
	private int _npcPartnerIndex = 0;
	private readonly ClickableComponent _prevPartnerBtn;
	private readonly ClickableComponent _nextPartnerBtn;

	public PokemonTradeMenu()
		: base((Game1.uiViewport.Width - MenuWidth) / 2, (Game1.uiViewport.Height - MenuHeight) / 2, MenuWidth, MenuHeight, showUpperRightCloseButton: true)
	{
		Instance = this;
		Game1.playSound("bigSelect");

		_closeButton = new ClickableTextureComponent(
			new Rectangle(xPositionOnScreen + width - 42, yPositionOnScreen + 10, 32, 32),
			Game1.mouseCursors,
			new Rectangle(337, 494, 12, 12),
			2.6f
		);

		_tradeButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 140, yPositionOnScreen + height - 68, 280, 48), "confirm_trade", "Confirmar Troca");
		_soloModeButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width / 2 - 150, yPositionOnScreen + height / 2 + 70, 300, 44), "toggle_solo", "Mudar para Modo Solo (NPCs)");

		int rightX = xPositionOnScreen + width - 380;
		int rightY = yPositionOnScreen + 64;
		_prevPartnerBtn = new ClickableComponent(new Rectangle(rightX, rightY - 2, 32, 24), "prev_partner", "◄");
		_nextPartnerBtn = new ClickableComponent(new Rectangle(rightX + 318, rightY - 2, 32, 24), "next_partner", "►");

		// Setup 6 player party slots
		int startX = xPositionOnScreen + 30;
		int startY = yPositionOnScreen + 80;
		int slotW = 340;
		int slotH = 58;
		int gap = 8;
		for (int i = 0; i < 6; i++)
		{
			_playerSlots.Add(new ClickableComponent(new Rectangle(startX, startY + i * (slotH + gap), slotW, slotH), i.ToString()));
		}

		// Load Pokéball texture
		try
		{
			string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald", "graphics", "balls", "poke.png");
			if (File.Exists(p1)) _ballTex = PokemonUI.LoadColorKeyed(p1);
		}
		catch { }

		SetupNpcPresets();

		// If in single player, default to solo NPC mode directly
		if (!Game1.IsMultiplayer)
		{
			_isSoloNpcMode = true;
			_state = TradeState.Selecting;
			_partnerName = _npcPartners.Count > 0 ? _npcPartners[0].PartnerName : "Terminal Link";
			_offeredPartnerMon = _npcPartners.Count > 0 ? _npcPartners[0].Pokemon : null;
		}
		else
		{
			_isSoloNpcMode = false;
			_state = TradeState.WaitingForPartner;
			BroadcastPacket($"WAITING|{Game1.player.UniqueMultiplayerID}|{Game1.player.Name}");
		}
	}

	private void SetupNpcPresets()
	{
		_npcPartners.Clear();

		void AddPreset(string trainer, string species, int level, string nick, string held = null)
		{
			var sp = PokemonSpecies.Get(species);
			if (sp != null)
			{
				var mon = new PokemonInstance(sp, level);
				if (!string.IsNullOrEmpty(nick)) mon.Nickname = nick;
				if (!string.IsNullOrEmpty(held)) mon.HeldItem = held;
				_npcPartners.Add(new TradePartnerOption { PartnerName = trainer, Pokemon = mon });
			}
		}

		AddPreset("Leaf (Pallet Town)", "haunter", 36, "Haunter de Troca");
		AddPreset("Red (Pallet Town)", "machoke", 36, "Machoke de Troca");
		AddPreset("Brendan (Littleroot)", "graveler", 36, "Graveler de Troca");
		AddPreset("May (Littleroot)", "kadabra", 36, "Kadabra de Troca");
		AddPreset("Steven (Mossdeep)", "beldum", 25, "Beldum de Steven");
		AddPreset("Blue (Viridian)", "scyther", 35, "Scyther", "MetalCoat");
		AddPreset("Bill (Goldenrod)", "eevee", 25, "Eevee de Bill");
		AddPreset("Cynthia (Celestic)", "togepi", 20, "Togepi de Cynthia");
		AddPreset("Jasmine (Olivine)", "onix", 35, "Onix", "MetalCoat");
		AddPreset("Oak (Laboratório)", "porygon", 30, "Porygon", "UpGrade");

		if (_npcPartners.Count == 0)
		{
			_npcPartners.Add(new TradePartnerOption { PartnerName = "Terminal Link", Pokemon = new PokemonInstance(PokemonSpecies.Get("eevee"), 25) });
		}
		_npcPartnerIndex = 0;
	}

	public static void HandleNetworkTradePacket(long senderId, string payload)
	{
		if (senderId == Game1.player.UniqueMultiplayerID) return;
		if (string.IsNullOrEmpty(payload)) return;

		if (payload.StartsWith("PART|", StringComparison.Ordinal))
		{
			string assembled = AssembleChunk(senderId, payload);
			if (assembled == null) return;
			Instance?.ReceiveNetworkPacket(senderId, assembled);
			return;
		}

		Instance?.ReceiveNetworkPacket(senderId, payload);
	}

	/// <summary>
	/// Rebuilds a split packet. Returns null while chunks are still missing; a
	/// malformed or stale packet is dropped instead of being half-applied.
	/// </summary>
	private static string AssembleChunk(long senderId, string payload)
	{
		string[] parts = payload.Split('|');
		if (parts.Length < 6) return null;

		string messageId = parts[1];
		if (!int.TryParse(parts[2], out int index)) return null;
		if (!int.TryParse(parts[3], out int total) || total <= 0 || total > 64) return null;
		if (index < 0 || index >= total) return null;

		string key = senderId + ":" + messageId;
		double now = Game1.currentGameTime?.TotalGameTime.TotalSeconds ?? 0.0;

		foreach (string stale in new List<string>(_pendingPackets.Keys))
		{
			if (now - _pendingPackets[stale].LastSeen > PacketTimeout)
			{
				_pendingPackets.Remove(stale);
			}
		}

		if (!_pendingPackets.TryGetValue(key, out PendingPacket pending))
		{
			pending = new PendingPacket { Total = total, Parts = new string[total] };
			_pendingPackets[key] = pending;
		}

		if (pending.Total != total) return null;
		if (pending.Parts[index] == null)
		{
			pending.Parts[index] = string.Join('|', parts, 4, parts.Length - 4);
			pending.Received++;
		}
		pending.LastSeen = now;

		if (pending.Received < pending.Total) return null;

		_pendingPackets.Remove(key);
		var builder = new StringBuilder();
		foreach (string part in pending.Parts)
		{
			if (part == null) return null;
			builder.Append(part);
		}
		return builder.ToString();
	}

	public void ReceiveNetworkPacket(long senderId, string payload)
	{
		if (string.IsNullOrWhiteSpace(payload)) return;
		string[] parts = payload.Split('|');
		string cmd = parts[0];

		switch (cmd)
		{
			case "WAITING":
				if (_state == TradeState.WaitingForPartner && !_isSoloNpcMode)
				{
					_partnerId = senderId;
					_partnerName = parts.Length > 2 ? parts[2] : $"Jogador {senderId % 1000}";
					_state = TradeState.Selecting;
					_myConfirmed = false;
					_partnerConfirmed = false;
					SendPacket($"CONNECT|{Game1.player.UniqueMultiplayerID}|{Game1.player.Name}", senderId);
					Game1.playSound("reward");
					Game1.addHUDMessage(new HUDMessage($"Conectado com {_partnerName} no Terminal de Trocas!", 2));
				}
				break;

			case "CONNECT":
				if ((_state == TradeState.WaitingForPartner || _partnerId == 0) && !_isSoloNpcMode)
				{
					_partnerId = senderId;
					_partnerName = parts.Length > 2 ? parts[2] : $"Jogador {senderId % 1000}";
					_state = TradeState.Selecting;
					_myConfirmed = false;
					_partnerConfirmed = false;
					SendPacket($"CONNECT_ACK|{Game1.player.UniqueMultiplayerID}|{Game1.player.Name}", senderId);
					Game1.playSound("reward");
					Game1.addHUDMessage(new HUDMessage($"Conectado com {_partnerName} no Terminal de Trocas!", 2));
				}
				break;

			case "CONNECT_ACK":
				if (_partnerId == senderId || _partnerId == 0)
				{
					_partnerId = senderId;
					_partnerName = parts.Length > 2 ? parts[2] : $"Jogador {senderId % 1000}";
					_state = TradeState.Selecting;
					_myConfirmed = false;
					_partnerConfirmed = false;
					Game1.playSound("reward");
				}
				break;

			case "OFFER":
				if (_partnerId == senderId)
				{
					try
					{
						if (parts.Length > 2)
						{
							string encoded = string.Join('|', parts, 2, parts.Length - 2);
							string json = DecodePayload(encoded);
							var dto = JsonSerializer.Deserialize<PokemonSaveDto>(json);
							if (dto != null)
							{
								_offeredPartnerMon = PokemonFromDto(dto);
								_partnerConfirmed = false;
								Game1.playSound("smallSelect");
							}
						}
					}
					catch { }
				}
				break;

			case "CONFIRM":
				if (_partnerId == senderId)
				{
					_partnerConfirmed = true;
					Game1.playSound("select");
					CheckStartTransfer();
				}
				break;

			case "UNCONFIRM":
				if (_partnerId == senderId)
				{
					_partnerConfirmed = false;
				}
				break;

			case "START_TRANSFER":
				if (_partnerId == senderId)
				{
					_state = TradeState.Transferring;
					_animTimer = 0f;
					Game1.playSound("wand");
				}
				break;

			case "CANCEL":
				if (_partnerId == senderId)
				{
					Game1.showRedMessage($"{_partnerName} encerrou a sessão de troca.");
					_partnerId = 0;
					_partnerName = "";
					_offeredPartnerMon = null;
					_myConfirmed = false;
					_partnerConfirmed = false;
					if (!_isSoloNpcMode)
					{
						_state = TradeState.WaitingForPartner;
					}
				}
				break;
		}
	}

	private void BroadcastPacket(string packet)
	{
		SendPacket(packet, Multiplayer.AllPlayers);
	}

	/// <summary>
	/// Sends a trade packet through the game chat channel. Chat payloads are capped
	/// in practice, get filtered by the platform, and used to be visible to every
	/// player, so the body is compressed, base64-encoded and split when needed.
	/// </summary>
	private void SendPacket(string packet, long recipient)
	{
		if (!Game1.IsMultiplayer || Game1.multiplayer == null) return;
		if (string.IsNullOrEmpty(packet)) return;

		try
		{
			LocalizedContentManager.LanguageCode language = LocalizedContentManager.CurrentLanguageCode;

			if (packet.Length <= MaxPayloadPerMessage)
			{
				Game1.multiplayer.sendChatMessage(language, PacketPrefix + packet, recipient);
				return;
			}

			string messageId = (Environment.TickCount & 0x7FFFFFF).ToString() + "." + Game1.random.Next(1000, 9999);
			int total = (packet.Length + MaxPayloadPerMessage - 1) / MaxPayloadPerMessage;
			if (total > 64)
			{
				// A single trade never comes close to this; refuse instead of sending a
				// truncated packet that the partner could not deserialize.
				Console.WriteLine("[PokemonTradeMenu] pacote grande demais para o canal de chat; envio cancelado.");
				return;
			}

			for (int i = 0; i < total; i++)
			{
				int start = i * MaxPayloadPerMessage;
				int length = Math.Min(MaxPayloadPerMessage, packet.Length - start);
				if (length <= 0) break;
				string chunk = packet.Substring(start, length);
				Game1.multiplayer.sendChatMessage(
					language,
					$"{PacketPrefix}PART|{messageId}|{i}|{total}|{chunk}",
					recipient);
			}
		}
		catch { }
	}

	private static string EncodePayload(string json)
	{
		using var output = new MemoryStream();
		using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
		{
			byte[] bytes = Encoding.UTF8.GetBytes(json ?? "");
			gzip.Write(bytes, 0, bytes.Length);
		}
		return Convert.ToBase64String(output.ToArray());
	}

	private static string DecodePayload(string encoded)
	{
		try
		{
			byte[] raw = Convert.FromBase64String(encoded);
			using var input = new MemoryStream(raw);
			using var gzip = new GZipStream(input, CompressionMode.Decompress);
			using var reader = new StreamReader(gzip, Encoding.UTF8);
			return reader.ReadToEnd();
		}
		catch
		{
			// Older builds sent raw JSON; accept it so a mixed-version session
			// still completes the trade instead of freezing the menu.
			return encoded;
		}
	}

	private void CheckStartTransfer()
	{
		if (_myConfirmed && _partnerConfirmed)
		{
			SendPacket($"START_TRANSFER|{Game1.player.UniqueMultiplayerID}", _partnerId);
			_state = TradeState.Transferring;
			_animTimer = 0f;
			Game1.playSound("wand");
		}
	}

	public override void update(GameTime time)
	{
		base.update(time);
		float dt = (float)time.ElapsedGameTime.TotalSeconds;

		if (_state == TradeState.WaitingForPartner && !_isSoloNpcMode)
		{
			_pingTimer += dt;
			if (_pingTimer >= 1.5f)
			{
				_pingTimer = 0f;
				BroadcastPacket($"WAITING|{Game1.player.UniqueMultiplayerID}|{Game1.player.Name}");
			}
		}
		else if (_state == TradeState.Transferring)
		{
			_animTimer += dt;
			float p = Math.Clamp(_animTimer / 3.0f, 0f, 1f);

			Vector2 pLeft = new Vector2(xPositionOnScreen + 200, yPositionOnScreen + height / 2 - 30);
			Vector2 pRight = new Vector2(xPositionOnScreen + width - 200, yPositionOnScreen + height / 2 - 30);
			_ball1Pos = Vector2.Lerp(pLeft, pRight, p) - new Vector2(0, (float)Math.Sin(p * Math.PI) * 40f);
			_ball2Pos = Vector2.Lerp(pRight, pLeft, p) + new Vector2(0, (float)Math.Sin(p * Math.PI) * 40f);

			if (p >= 1f)
			{
				_state = TradeState.Complete;
				FinalizeTrade();
			}
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (_state == TradeState.Transferring) return; // Locked during animation

		base.receiveLeftClick(x, y, playSound);

		if (_closeButton.containsPoint(x, y))
		{
			Game1.playSound("bigDeSelect");
			CloseAndCleanup();
			return;
		}

		if (_state == TradeState.WaitingForPartner)
		{
			if (_soloModeButton != null && _soloModeButton.containsPoint(x, y))
			{
				_isSoloNpcMode = true;
				_state = TradeState.Selecting;
				_partnerName = _npcPartners.Count > 0 ? _npcPartners[0].PartnerName : "Terminal Link";
				_offeredPartnerMon = _npcPartners.Count > 0 ? _npcPartners[0].Pokemon : null;
				Game1.playSound("select");
				return;
			}
			return;
		}

		if (_state == TradeState.Selecting)
		{
			if (_isSoloNpcMode)
			{
				if (_prevPartnerBtn != null && _prevPartnerBtn.containsPoint(x, y))
				{
					if (_npcPartners.Count > 1)
					{
						_npcPartnerIndex = (_npcPartnerIndex - 1 + _npcPartners.Count) % _npcPartners.Count;
						_partnerName = _npcPartners[_npcPartnerIndex].PartnerName;
						_offeredPartnerMon = _npcPartners[_npcPartnerIndex].Pokemon;
						Game1.playSound("smallSelect");
					}
					return;
				}

				if (_nextPartnerBtn != null && _nextPartnerBtn.containsPoint(x, y))
				{
					if (_npcPartners.Count > 1)
					{
						_npcPartnerIndex = (_npcPartnerIndex + 1) % _npcPartners.Count;
						_partnerName = _npcPartners[_npcPartnerIndex].PartnerName;
						_offeredPartnerMon = _npcPartners[_npcPartnerIndex].Pokemon;
						Game1.playSound("smallSelect");
					}
					return;
				}
			}

			var party = PlayerParty.Instance.Team;

			// Select player Pokémon slot
			for (int i = 0; i < _playerSlots.Count; i++)
			{
				if (_playerSlots[i].containsPoint(x, y) && i < party.Count)
				{
					if (_selectedPlayerSlot != i)
					{
						_selectedPlayerSlot = i;
						_myConfirmed = false;
						Game1.playSound("smallSelect");

						if (!_isSoloNpcMode && _partnerId != 0)
						{
							string json = JsonSerializer.Serialize(PokemonToDto(party[i]));
							SendPacket($"OFFER|{Game1.player.UniqueMultiplayerID}|{EncodePayload(json)}", _partnerId);
							SendPacket($"UNCONFIRM|{Game1.player.UniqueMultiplayerID}", _partnerId);
						}
					}
					return;
				}
			}

			// Confirm Trade Button
			if (_tradeButton.containsPoint(x, y))
			{
				if (_selectedPlayerSlot < 0 || _selectedPlayerSlot >= party.Count)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage("Selecione um Pokémon da sua equipe para oferecer na troca!");
					return;
				}

				if (_offeredPartnerMon == null)
				{
					Game1.playSound("cancel");
					Game1.showRedMessage("Aguarde o parceiro selecionar um Pokémon para a troca!");
					return;
				}

				if (!_myConfirmed)
				{
					_myConfirmed = true;
					Game1.playSound("select");

					if (!_isSoloNpcMode)
					{
						SendPacket($"CONFIRM|{Game1.player.UniqueMultiplayerID}", _partnerId);
						CheckStartTransfer();
					}
					else
					{
						// In solo mode, immediately start transfer!
						_state = TradeState.Transferring;
						_animTimer = 0f;
						Game1.playSound("wand");
					}
				}
				else if (!_isSoloNpcMode && !_partnerConfirmed)
				{
					// Toggle off confirmation
					_myConfirmed = false;
					SendPacket($"UNCONFIRM|{Game1.player.UniqueMultiplayerID}", _partnerId);
					Game1.playSound("cancel");
				}
			}
		}
	}

	private void FinalizeTrade()
	{
		var party = PlayerParty.Instance.Team;
		if (_selectedPlayerSlot < 0 || _selectedPlayerSlot >= party.Count || _offeredPartnerMon == null)
		{
			CloseAndCleanup();
			return;
		}

		var givenMon = party[_selectedPlayerSlot];
		var receivedMon = _offeredPartnerMon;

		// Swap into party
		party[_selectedPlayerSlot] = receivedMon;
		PlayerParty.Instance.SaveParty();

		Game1.playSound("reward");
		Game1.playSound("coin");

		// Check for Trade Evolution on the received Pokémon!
		var evo = PokemonEvolution.CheckEvolution(receivedMon, EvolutionTrigger.Trade);
		if (evo != null)
		{
			PokemonSpecies target = PokemonSpecies.Get(evo.TargetSpecies);
			if (target != null)
			{
				Instance = null;
				Game1.activeClickableMenu = new PokemonEvolutionMenu(receivedMon, target, () =>
				{
					Game1.activeClickableMenu = null;
				});
				return;
			}
		}

		Game1.addHUDMessage(new HUDMessage($"Troca concluída com sucesso com {_partnerName}! Recebeu {receivedMon.Nickname}!", 2));
		CloseAndCleanup();
	}

	private void CloseAndCleanup()
	{
		if (!_isSoloNpcMode && _partnerId != 0)
		{
			SendPacket($"CANCEL|{Game1.player.UniqueMultiplayerID}", _partnerId);
		}
		Instance = null;
		exitThisMenu();
	}

	public override void emergencyShutDown()
	{
		CloseAndCleanup();
		base.emergencyShutDown();
	}

	public override void draw(SpriteBatch b)
	{
		// Dim overlay
		b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.72f);

		// Menu window frame
		PokemonUI.Border(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), 4, new Color(24, 48, 56));
		b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 4, yPositionOnScreen + 4, width - 8, height - 8), new Color(238, 246, 242));

		// Top Header
		b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 4, yPositionOnScreen + 4, width - 8, 48), new Color(42, 92, 116));
		string headerTitle = _isSoloNpcMode ? "CENTRO DE TROCAS POKÉMON (MODO LINK NPC)" : "CENTRO DE TROCAS POKÉMON (2 JOGADORES CO-OP)";
		PokemonUI.Text(b, Game1.dialogueFont, headerTitle, new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 10, 650, 32), Color.White, maxScale: 0.82f);

		_closeButton.draw(b);

		if (_state == TradeState.WaitingForPartner)
		{
			DrawWaitingForPartner(b);
			drawMouse(b);
			return;
		}

		if (_state == TradeState.Transferring)
		{
			DrawTransferAnimation(b);
			drawMouse(b);
			return;
		}

		// Left Column: Player Team
		DrawPlayerParty(b);

		// Center Link Pipe / Cable
		int cx = xPositionOnScreen + width / 2;
		int cy = yPositionOnScreen + height / 2 - 40;
		b.Draw(Game1.staminaRect, new Rectangle(cx - 50, cy - 8, 100, 16), new Color(64, 140, 180));
		PokemonUI.Border(b, new Rectangle(cx - 50, cy - 8, 100, 16), 2, new Color(32, 70, 90));
		PokemonUI.Text(b, Game1.tinyFont, "◄► CABO LINK", new Rectangle(cx - 50, cy - 26, 100, 16), Color.DarkSlateBlue, center: true, maxScale: 0.75f);

		// Right Column: Partner Offer
		DrawPartnerOffer(b);

		// Confirm Trade Button
		DrawTradeButton(b);

		drawMouse(b);
	}

	private void DrawWaitingForPartner(SpriteBatch b)
	{
		int cx = xPositionOnScreen + width / 2;
		int cy = yPositionOnScreen + height / 2 - 40;

		Rectangle box = new Rectangle(cx - 260, cy - 90, 520, 220);
		b.Draw(Game1.staminaRect, box, new Color(248, 252, 250));
		PokemonUI.Border(b, box, 3, new Color(42, 92, 116));

		PokemonUI.Text(b, Game1.dialogueFont, "AGUARDANDO OUTRO JOGADOR...", new Rectangle(box.X + 20, box.Y + 24, box.Width - 40, 32), Color.DarkSlateBlue, center: true, maxScale: 0.90f);

		string subMsg = "Peça para o outro jogador interagir com o Terminal de Trocas\npara iniciar a conexão sincronizada em tempo real.";
		PokemonUI.Text(b, Game1.smallFont, subMsg, new Rectangle(box.X + 20, box.Y + 68, box.Width - 40, 50), Color.DarkSlateGray, center: true, maxScale: 0.82f);

		// Radar pulse graphic
		float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 4.0) * 0.3f + 0.7f;
		Rectangle radarBar = new Rectangle(box.X + 40, box.Y + 130, (int)((box.Width - 80) * pulse), 8);
		b.Draw(Game1.staminaRect, radarBar, new Color(52, 152, 219) * pulse);

		// Button to switch to solo NPC mode
		bool soloHover = _soloModeButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());
		b.Draw(Game1.staminaRect, _soloModeButton.bounds, soloHover ? new Color(254, 240, 200) : new Color(230, 240, 245));
		PokemonUI.Border(b, _soloModeButton.bounds, 2, soloHover ? Color.DarkGoldenrod : new Color(80, 120, 140));
		PokemonUI.Text(b, Game1.smallFont, _soloModeButton.label, _soloModeButton.bounds, Color.DarkSlateGray, center: true, maxScale: 0.82f);
	}

	private void DrawPlayerParty(SpriteBatch b)
	{
		var party = PlayerParty.Instance.Team;
		int startX = xPositionOnScreen + 30;
		int startY = yPositionOnScreen + 64;

		string title = _myConfirmed ? "SUA EQUIPE (✓ VOCÊ CONFIRMOU):" : "SUA EQUIPE (SELECIONE PARA OFERECER):";
		Color titleCol = _myConfirmed ? new Color(20, 140, 60) : Color.DarkSlateGray;
		PokemonUI.Text(b, Game1.smallFont, title, new Rectangle(startX, startY, 340, 20), titleCol, maxScale: 0.85f);

		for (int i = 0; i < _playerSlots.Count; i++)
		{
			var rect = _playerSlots[i].bounds;
			bool isSelected = (_selectedPlayerSlot == i);
			bool isHover = rect.Contains(Game1.getMouseX(), Game1.getMouseY());

			Color bg = isSelected ? new Color(255, 248, 190) : (isHover ? Color.White : new Color(245, 250, 248));
			Color border = isSelected ? Color.DarkOrange : (isHover ? new Color(52, 152, 219) : new Color(180, 205, 195));

			b.Draw(Game1.staminaRect, rect, bg);
			PokemonUI.Border(b, rect, isSelected ? 3 : 1, border);

			if (i < party.Count)
			{
				var p = party[i];
				Texture2D icon = p.GetIconSprite(Game1.graphics.GraphicsDevice);
				if (icon != null)
				{
					b.Draw(icon, new Vector2(rect.X + 6, rect.Y + 4), p.GetIconSourceRect(), Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0.9f);
				}

				PokemonUI.Text(b, Game1.smallFont, $"{p.Nickname} (Lv.{p.Level})", new Rectangle(rect.X + 60, rect.Y + 6, rect.Width - 66, 22), Color.Black, maxScale: 0.88f);
				PokemonUI.Text(b, Game1.tinyFont, $"{p.Species.Name}  |  HP: {p.CurrentHP}/{p.MaxHP}", new Rectangle(rect.X + 60, rect.Y + 28, rect.Width - 66, 18), Color.DarkSlateGray, maxScale: 0.82f);
			}
			else
			{
				PokemonUI.Text(b, Game1.tinyFont, "-- Vazio --", rect, Color.Gray * 0.6f, center: true);
			}
		}
	}

	private void DrawPartnerOffer(SpriteBatch b)
	{
		int rightX = xPositionOnScreen + width - 380;
		int rightY = yPositionOnScreen + 64;
		int cardW = 350;
		int cardH = 390;
		Rectangle cardRect = new Rectangle(rightX, rightY, cardW, cardH);

		string partnerHeader = string.IsNullOrWhiteSpace(_partnerName) ? "PARCEIRO DE TROCA" : $"PARCEIRO: {_partnerName.ToUpperInvariant()}";
		if (_partnerConfirmed) partnerHeader += " (✓ CONFIRMOU!)";

		Color headerCol = _partnerConfirmed ? new Color(20, 140, 60) : Color.DarkSlateGray;
		PokemonUI.Text(b, Game1.smallFont, partnerHeader, new Rectangle(rightX + 34, rightY, cardW - 68, 20), headerCol, center: true, maxScale: 0.78f);

		if (_isSoloNpcMode)
		{
			bool prevHov = _prevPartnerBtn.containsPoint(Game1.getMouseX(), Game1.getMouseY());
			bool nextHov = _nextPartnerBtn.containsPoint(Game1.getMouseX(), Game1.getMouseY());
			b.Draw(Game1.staminaRect, _prevPartnerBtn.bounds, prevHov ? Color.PaleGoldenrod : Color.White);
			PokemonUI.Border(b, _prevPartnerBtn.bounds, 1, Color.DarkSlateGray);
			PokemonUI.Text(b, Game1.tinyFont, "◄", _prevPartnerBtn.bounds, Color.Black, center: true);

			b.Draw(Game1.staminaRect, _nextPartnerBtn.bounds, nextHov ? Color.PaleGoldenrod : Color.White);
			PokemonUI.Border(b, _nextPartnerBtn.bounds, 1, Color.DarkSlateGray);
			PokemonUI.Text(b, Game1.tinyFont, "►", _nextPartnerBtn.bounds, Color.Black, center: true);
		}

		Rectangle inner = new Rectangle(rightX, rightY + 28, cardW, cardH - 28);
		b.Draw(Game1.staminaRect, inner, new Color(248, 252, 250));
		PokemonUI.Border(b, inner, 2, _partnerConfirmed ? new Color(40, 160, 70) : new Color(42, 92, 116));

		if (_offeredPartnerMon != null)
		{
			Texture2D front = _offeredPartnerMon.GetFrontSprite(Game1.graphics.GraphicsDevice);
			if (front != null)
			{
				b.Draw(front, new Vector2(inner.Center.X - 64, inner.Y + 16), _offeredPartnerMon.GetFrontSourceRect(), Color.White, 0f, Vector2.Zero, 2.0f, SpriteEffects.None, 0.9f);
			}

			PokemonUI.Text(b, Game1.dialogueFont, _offeredPartnerMon.Nickname, new Rectangle(inner.X + 16, inner.Y + 152, inner.Width - 32, 28), Color.Black, center: true, maxScale: 0.85f);
			PokemonUI.Text(b, Game1.smallFont, $"Espécie: {_offeredPartnerMon.Species.Name} (Lv.{_offeredPartnerMon.Level})", new Rectangle(inner.X + 16, inner.Y + 182, inner.Width - 32, 22), Color.DarkSlateGray, center: true, maxScale: 0.85f);

			string type = _offeredPartnerMon.Species.SecondaryType.HasValue ? $"{_offeredPartnerMon.Species.PrimaryType} / {_offeredPartnerMon.Species.SecondaryType}" : _offeredPartnerMon.Species.PrimaryType.ToString();
			PokemonUI.Text(b, Game1.smallFont, $"Tipo: {type}", new Rectangle(inner.X + 16, inner.Y + 206, inner.Width - 32, 20), Color.DarkGreen, center: true, maxScale: 0.85f);
			PokemonUI.Text(b, Game1.smallFont, $"HP: {_offeredPartnerMon.CurrentHP}/{_offeredPartnerMon.MaxHP}", new Rectangle(inner.X + 16, inner.Y + 228, inner.Width - 32, 20), Color.DarkSlateBlue, center: true, maxScale: 0.85f);

			if (!string.IsNullOrEmpty(_offeredPartnerMon.Ability))
			{
				PokemonUI.Text(b, Game1.tinyFont, $"Habilidade: {_offeredPartnerMon.Ability}", new Rectangle(inner.X + 16, inner.Y + 250, inner.Width - 32, 18), Color.DimGray, center: true, maxScale: 0.82f);
			}

			// Trade Evolution badge
			var evo = PokemonEvolution.CheckEvolution(_offeredPartnerMon, EvolutionTrigger.Trade);
			if (evo != null)
			{
				PokemonSpecies evoTarget = PokemonSpecies.Get(evo.TargetSpecies);
				string evoNotice = $"★ Evoluirá para {evoTarget?.Name ?? evo.TargetSpecies} ao receber!";
				PokemonUI.Text(b, Game1.smallFont, evoNotice, new Rectangle(inner.X + 8, inner.Y + 276, inner.Width - 16, 24), Color.DarkMagenta, center: true, maxScale: 0.82f);
			}
		}
		else
		{
			string waitMonMsg = string.IsNullOrWhiteSpace(_partnerName) ? "Aguardando seleção..." : $"Aguardando {_partnerName}\nescolher um Pokémon...";
			PokemonUI.Text(b, Game1.smallFont, waitMonMsg, inner, Color.Gray * 0.7f, center: true, maxScale: 0.85f);
		}
	}

	private void DrawTradeButton(SpriteBatch b)
	{
		bool partySelected = (_selectedPlayerSlot >= 0 && _selectedPlayerSlot < PlayerParty.Instance.Team.Count);
		bool partnerReady = (_offeredPartnerMon != null);
		bool canClick = partySelected && partnerReady;

		string label;
		Color btnBg;
		Color borderCol;

		if (!partySelected)
		{
			label = "Selecione seu Pokémon";
			btnBg = new Color(180, 190, 195);
			borderCol = Color.DarkGray;
		}
		else if (!partnerReady)
		{
			label = $"Aguardando {_partnerName}...";
			btnBg = new Color(200, 200, 160);
			borderCol = Color.DarkGoldenrod;
		}
		else if (_myConfirmed && !_partnerConfirmed)
		{
			label = "✓ Você confirmou! Aguardando parceiro...";
			btnBg = new Color(240, 180, 50);
			borderCol = Color.DarkOrange;
		}
		else if (!_myConfirmed && _partnerConfirmed)
		{
			label = $"★ {_partnerName} confirmou! Clique para Trocar";
			btnBg = new Color(46, 204, 113);
			borderCol = new Color(20, 130, 60);
		}
		else
		{
			label = "Confirmar Troca";
			btnBg = new Color(52, 152, 219);
			borderCol = new Color(24, 80, 120);
		}

		bool hover = _tradeButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) && canClick;
		if (hover) btnBg = Color.Lerp(btnBg, Color.White, 0.25f);

		b.Draw(Game1.staminaRect, _tradeButton.bounds, btnBg);
		PokemonUI.Border(b, _tradeButton.bounds, 2, borderCol);
		PokemonUI.Text(b, Game1.dialogueFont, label, _tradeButton.bounds, Color.White, center: true, maxScale: 0.78f);
	}

	private void DrawTransferAnimation(SpriteBatch b)
	{
		int cx = xPositionOnScreen + width / 2;
		int cy = yPositionOnScreen + height / 2 - 30;

		b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 100, cy - 20, width - 200, 40), new Color(40, 120, 180) * 0.7f);
		PokemonUI.Border(b, new Rectangle(xPositionOnScreen + 100, cy - 20, width - 200, 40), 3, Color.White * 0.8f);

		float beamPulse = (float)Math.Sin(_animTimer * 10f) * 0.2f + 0.8f;
		b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 104, cy - 8, width - 208, 16), Color.Cyan * beamPulse);

		if (_ballTex != null)
		{
			b.Draw(_ballTex, _ball1Pos, new Rectangle(0, 0, 16, 16), Color.White, _animTimer * 12f, new Vector2(8f, 8f), 2.5f, SpriteEffects.None, 0.95f);
			b.Draw(_ballTex, _ball2Pos, new Rectangle(0, 0, 16, 16), Color.White, -_animTimer * 12f, new Vector2(8f, 8f), 2.5f, SpriteEffects.None, 0.95f);
		}

		PokemonUI.Text(b, Game1.dialogueFont, "SINCRONIZANDO E TROCANDO POKÉMON NO CABO LINK...", new Rectangle(xPositionOnScreen + 20, cy + 50, width - 40, 36), Color.White, center: true, maxScale: 0.85f);
	}

	private static PokemonSaveDto PokemonToDto(PokemonInstance p)
	{
		return PokemonDtoMapper.ToDto(p);
	}

	private static PokemonInstance PokemonFromDto(PokemonSaveDto dto)
	{
		return PokemonDtoMapper.FromDto(dto);
	}
}