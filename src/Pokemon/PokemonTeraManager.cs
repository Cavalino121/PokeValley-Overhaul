using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

/// <summary>
/// Manages Clint's Blacksmith Tera Crystal Forging service.
/// Consumes 50 Tera Shards of a specific elemental type to alter the lead Pokémon's Tera Type.
/// </summary>
public static class PokemonTeraManager
{
	public static void HandleClintTeraForge()
	{
		var party = PlayerParty.Instance;
		var lead = party.ActivePokemon;
		if (lead == null)
		{
			NPC clint = Game1.getCharacterFromName("Clint");
			if (clint != null)
			{
				Game1.DrawDialogue(clint, "Você precisa de um Pokémon consciente na sua equipe para que eu possa forjar sua gema Tera!");
			}
			else
			{
				Game1.drawObjectDialogue("Você precisa de um Pokémon consciente na sua equipe para forjar a gema Tera!");
			}
			return;
		}

		// Find which Tera Shards the player has >= 50 of
		var availableTypes = new List<(PokemonType type, string shardId, string name)>();
		foreach (PokemonType t in (PokemonType[])Enum.GetValues(typeof(PokemonType)))
		{
			string shardId = $"Poke_TeraShard_{t}";
			int count = Game1.player.Items.CountId("(O)" + shardId);
			if (count >= 50)
			{
				availableTypes.Add((t, shardId, t.ToString()));
			}
		}

		if (availableTypes.Count == 0)
		{
			string msg = $"Posso forjar uma nova jóia elemental para {lead.Nickname} (Tera Type atual: {lead.TeraType}), mas preciso de pelo menos 50 Tera Shards de um mesmo tipo. Junte os fragmentos nas minas e volte aqui!";
			NPC clint = Game1.getCharacterFromName("Clint");
			if (clint != null)
			{
				Game1.DrawDialogue(clint, msg);
			}
			else
			{
				Game1.drawObjectDialogue(msg);
			}
			return;
		}

		// Build dialogue responses
		List<Response> responses = new List<Response>();
		for (int i = 0; i < availableTypes.Count; i++)
		{
			var item = availableTypes[i];
			responses.Add(new Response($"TeraForge_{item.type}", $"{item.name} (50 Shards)"));
		}
		responses.Add(new Response("Cancel", "Cancelar"));

		Game1.currentLocation.createQuestionDialogue(
			$"Clint: Qual tipo elemental você quer forjar no Tera Type de {lead.Nickname}? (Atual: {lead.TeraType})",
			responses.ToArray(),
			"PokemonTeraForge"
		);
	}

	public static bool HandleTeraForgeAnswer(string answerKey)
	{
		if (string.IsNullOrEmpty(answerKey) || !answerKey.StartsWith("PokemonTeraForge_TeraForge_"))
		{
			return false;
		}

		string typeStr = answerKey.Substring("PokemonTeraForge_TeraForge_".Length);
		if (!Enum.TryParse<PokemonType>(typeStr, out var chosenType))
		{
			return false;
		}

		string shardId = $"Poke_TeraShard_{chosenType}";
		int count = Game1.player.Items.CountId("(O)" + shardId);
		if (count < 50)
		{
			NPC clint = Game1.getCharacterFromName("Clint");
			if (clint != null)
			{
				Game1.DrawDialogue(clint, "Você não tem os 50 fragmentos necessários!");
			}
			else
			{
				Game1.drawObjectDialogue("Você não tem os 50 fragmentos necessários!");
			}
			return true;
		}

		var party = PlayerParty.Instance;
		var lead = party.ActivePokemon;
		if (lead == null) return true;

		// Consume 50 shards
		Game1.player.Items.ReduceId("(O)" + shardId, 50);
		lead.TeraType = chosenType;
		party.SaveParty();

		Game1.playSound("hammer");
		Game1.playSound("crystal");

		string successMsg = $"*CLANG! CLANG!* Pronto! A energia dos 50 cristais foi lapidada e fundida na jóia de {lead.Nickname}! Seu novo Tera Type é {chosenType}!";
		NPC clintObj = Game1.getCharacterFromName("Clint");
		if (clintObj != null)
		{
			Game1.DrawDialogue(clintObj, successMsg);
		}
		else
		{
			Game1.drawObjectDialogue(successMsg);
		}
		Game1.addHUDMessage(new HUDMessage($"✨ Tera Type de {lead.Nickname} alterado para {chosenType}!", 2));
		return true;
	}
}
