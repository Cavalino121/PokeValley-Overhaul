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
					SendBattlePacket($"CHALLENGE|{who.UniqueMultiplayerID}|{who.Name}|{target.UniqueMultiplayerID}", target.UniqueMultiplayerID);
					Game1.addHUDMessage(new HUDMessage($"Desafio enviado para {target.Name}!", 2));

					// In local split-screen immediate dispatch:
					if (LocalMultiplayer.IsLocalMultiplayer())
					{
						ReceiveBattlePacket(who.UniqueMultiplayerID, $"CHALLENGE|{who.UniqueMultiplayerID}|{who.Name}|{target.UniqueMultiplayerID}");
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
								SendBattlePacket($"ACCEPT|{targetId}|{challengerId}", challengerId);
								if (LocalMultiplayer.IsLocalMultiplayer())
								{
									ReceiveBattlePacket(targetId, $"ACCEPT|{targetId}|{challengerId}");
								}
								var challengerParty = PlayerParty.GetForFarmer(challengerId);
								Game1.activeClickableMenu = new PokemonBattleMenu(challengerName, challengerParty.Team);
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
						var acceptorFarmer = Game1.getFarmerMaybeOffline(acceptorId);
						string targetName = acceptorFarmer?.Name ?? "Treinador";
						var targetParty = PlayerParty.GetForFarmer(acceptorId);
						Game1.playSound("reward");
						Game1.activeClickableMenu = new PokemonBattleMenu(targetName, targetParty.Team);
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
		}
	}
}
