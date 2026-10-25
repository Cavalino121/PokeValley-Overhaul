using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

public static class PokemonPvPManager
{
	private const string BattlePrefix = "[POKE_BATTLE]:";
	private static double _lastChallengeTime = 0;

	public static bool HandleFarmerInteraction(Farmer who, Farmer target, GameLocation location)
	{
		if (who == null || target == null || who == target) return false;

		// 1. Check if target is in an active battle -> Prompt to Spectate
		if (PokemonBattleMenu.ActiveBattles.TryGetValue(target.UniqueMultiplayerID, out var activeBattle) && activeBattle != null)
		{
			string q = $"Assistir à batalha Pokémon de {target.Name}?";
			location.createQuestionDialogue(q, location.createYesNoResponses(), delegate(Farmer _, string answer)
			{
				if (answer == "Yes")
				{
					Game1.activeClickableMenu = new PokemonBattleMenu(activeBattle, spectator: true);
				}
			});
			return true;
		}

		// 2. Check if both have Pokemon in team -> Prompt to Challenge
		var myParty = PlayerParty.GetForFarmer(who.UniqueMultiplayerID);
		var targetParty = PlayerParty.GetForFarmer(target.UniqueMultiplayerID);

		if (myParty.Team.Count > 0 && targetParty.Team.Count > 0)
		{
			if (!myParty.Team.Exists(p => !p.IsFainted))
			{
				Game1.showRedMessage("Todos os seus Pokémon estão desmaiados!");
				return true;
			}

			double now = Game1.currentGameTime?.TotalGameTime.TotalSeconds ?? 0;
			if (now - _lastChallengeTime < 3.0) return true; // Debounce

			string q = $"Desafiar {target.Name} para uma Batalha Pokémon?";
			location.createQuestionDialogue(q, location.createYesNoResponses(), delegate(Farmer _, string answer)
			{
				if (answer == "Yes")
				{
					_lastChallengeTime = now;
					int myLead = myParty.ActiveIndex;
					string mySkin = PokemonUI.TrainerSkin;
					string payload = $"CHALLENGE|{who.UniqueMultiplayerID}|{who.Name}|{target.UniqueMultiplayerID}|{mySkin}|{myLead}";
					SendBattlePacket(payload, target.UniqueMultiplayerID);
					Game1.addHUDMessage(new HUDMessage($"Desafio enviado para {target.Name}!", 2));

					// In local split-screen immediate dispatch:
					if (LocalMultiplayer.IsLocalMultiplayer())
					{
						ReceiveBattlePacket(who.UniqueMultiplayerID, payload);
					}
				}
			});
			return true;
		}

		return false;
	}

	public static void SendBattlePacket(string payload, long recipient)
	{
		if (!Game1.IsMultiplayer || Game1.multiplayer == null) return;
		try
		{
			Game1.multiplayer.sendChatMessage(LocalizedContentManager.CurrentLanguageCode, BattlePrefix + payload, recipient);
		}
		catch { }
	}

	public static void ReceiveBattlePacket(long senderId, string packet)
	{
		if (string.IsNullOrEmpty(packet)) return;
		string[] parts = packet.Split('|');
		if (parts.Length < 2) return;

		string cmd = parts[0];
		switch (cmd)
		{
			case "CHALLENGE":
				if (parts.Length >= 4 && long.TryParse(parts[1], out long challengerId) && long.TryParse(parts[3], out long targetId))
				{
					if (Game1.player?.UniqueMultiplayerID == targetId)
					{
						string challengerName = parts[2];
						string challengerSkin = (parts.Length >= 5) ? parts[4] : "Brendan";
						int challengerLead = (parts.Length >= 6 && int.TryParse(parts[5], out int cl)) ? cl : 0;
						var targetParty = PlayerParty.GetForFarmer(targetId);
						if (targetParty.Team.Count == 0 || !targetParty.Team.Exists(p => !p.IsFainted))
						{
							SendBattlePacket($"DECLINE|{targetId}|{Game1.player?.Name}|{challengerId}", challengerId);
							return;
						}

						string q = $"{challengerName} desafiou você para uma Batalha Pokémon! Aceitar?";
						Game1.currentLocation?.createQuestionDialogue(q, Game1.currentLocation.createYesNoResponses(), delegate(Farmer _, string answer)
						{
							if (answer == "Yes")
							{
								int myLead = targetParty.ActiveIndex;
								string mySkin = PokemonUI.TrainerSkin;
								string acceptPayload = $"ACCEPT|{targetId}|{challengerId}|{mySkin}|{myLead}";
								SendBattlePacket(acceptPayload, challengerId);
								if (LocalMultiplayer.IsLocalMultiplayer())
								{
									ReceiveBattlePacket(targetId, acceptPayload);
								}
								var challengerParty = PlayerParty.GetForFarmer(challengerId);
								Game1.activeClickableMenu = new PokemonBattleMenu(challengerId, challengerName, challengerParty.Team, challengerLead, challengerSkin);
							}
							else
							{
								SendBattlePacket($"DECLINE|{targetId}|{Game1.player?.Name}|{challengerId}", challengerId);
								if (LocalMultiplayer.IsLocalMultiplayer())
								{
									ReceiveBattlePacket(targetId, $"DECLINE|{targetId}|{Game1.player?.Name}|{challengerId}");
								}
							}
						});
					}
				}
				break;

			case "ACCEPT":
				if (parts.Length >= 3 && long.TryParse(parts[1], out long acceptorId) && long.TryParse(parts[2], out long origChallengerId))
				{
					if (Game1.player?.UniqueMultiplayerID == origChallengerId)
					{
						string acceptorSkin = (parts.Length >= 4) ? parts[3] : "Brendan";
						int acceptorLead = (parts.Length >= 5 && int.TryParse(parts[4], out int al)) ? al : 0;
						var acceptorFarmer = Game1.getFarmerMaybeOffline(acceptorId);
						string targetName = acceptorFarmer?.Name ?? "Treinador";
						var targetParty = PlayerParty.GetForFarmer(acceptorId);
						Game1.playSound("reward");
						Game1.activeClickableMenu = new PokemonBattleMenu(acceptorId, targetName, targetParty.Team, acceptorLead, acceptorSkin);
					}
				}
				break;

			case "DECLINE":
				if (parts.Length >= 4 && long.TryParse(parts[3], out long targetChallengerId))
				{
					if (Game1.player?.UniqueMultiplayerID == targetChallengerId)
					{
						string declinerName = parts[2];
						Game1.playSound("cancel");
						Game1.showRedMessage($"{declinerName} recusou o desafio.");
					}
				}
				break;

			case "ACTION":
				if (parts.Length >= 11)
				{
					if (PokemonBattleMenu.ActiveBattles.TryGetValue(Game1.player.UniqueMultiplayerID, out var battle) && battle != null && battle.IsPvP)
					{
						var act = new PokemonBattleMenu.PvPAction
						{
							ActionType = Enum.Parse<PokemonBattleMenu.PvPActionType>(parts[2]),
							MoveIndex = int.Parse(parts[3]),
							MoveName = parts[4],
							SwitchSlot = int.Parse(parts[5]),
							UseMega = bool.Parse(parts[6]),
							UseZMove = bool.Parse(parts[7]),
							UseDynamax = bool.Parse(parts[8]),
							UseGMax = bool.Parse(parts[9]),
							UseTera = bool.Parse(parts[10])
						};
						battle.ReceiveOpponentAction(act);
					}
				}
				break;

			case "SWITCH":
				if (parts.Length >= 4 && int.TryParse(parts[2], out int switchSlot))
				{
					if (PokemonBattleMenu.ActiveBattles.TryGetValue(Game1.player.UniqueMultiplayerID, out var battle) && battle != null && battle.IsPvP)
					{
						string nickname = parts[3];
						battle.ReceiveOpponentSwitch(switchSlot, nickname);
					}
				}
				break;

			case "FORFEIT":
				if (PokemonBattleMenu.ActiveBattles.TryGetValue(Game1.player.UniqueMultiplayerID, out var bEnd) && bEnd != null && bEnd.IsPvP)
				{
					bEnd.ReceiveOpponentForfeit();
				}
				break;
		}
	}

	public static void SendPvPAction(long recipient, PokemonBattleMenu.PvPAction act)
	{
		string payload = $"ACTION|{Game1.player?.UniqueMultiplayerID ?? 0L}|{act.ActionType}|{act.MoveIndex}|{act.MoveName}|{act.SwitchSlot}|{act.UseMega}|{act.UseZMove}|{act.UseDynamax}|{act.UseGMax}|{act.UseTera}";
		SendBattlePacket(payload, recipient);

		// Local split screen direct memory dispatch:
		if (PokemonBattleMenu.ActiveBattles.TryGetValue(recipient, out var targetBattle) && targetBattle != null && targetBattle.IsPvP)
		{
			targetBattle.ReceiveOpponentAction(act);
		}
	}

	public static void SendPvPSwitch(long recipient, int newSlot, string nickname)
	{
		string payload = $"SWITCH|{Game1.player?.UniqueMultiplayerID ?? 0L}|{newSlot}|{nickname}";
		SendBattlePacket(payload, recipient);

		if (PokemonBattleMenu.ActiveBattles.TryGetValue(recipient, out var targetBattle) && targetBattle != null && targetBattle.IsPvP)
		{
			targetBattle.ReceiveOpponentSwitch(newSlot, nickname);
		}
	}

	public static void SendPvPBattleEnd(long recipient)
	{
		string payload = $"FORFEIT|{Game1.player?.UniqueMultiplayerID ?? 0L}";
		SendBattlePacket(payload, recipient);

		if (PokemonBattleMenu.ActiveBattles.TryGetValue(recipient, out var targetBattle) && targetBattle != null && targetBattle.IsPvP)
		{
			targetBattle.ReceiveOpponentForfeit();
		}
	}
}
