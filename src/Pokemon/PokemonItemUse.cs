using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

/// <summary>Overworld use of Pokémon consumables (potions, revive, rare candy, TMs, repels).</summary>
public static class PokemonItemUse
{
	public const string TmPrefix = "PokemonTM_";

	// ---- Repel state (per farmer) ----
	private static readonly Dictionary<long, int> _repelStepsByFarmer = new();

	public static int RepelSteps
	{
		get
		{
			long id = Game1.player?.UniqueMultiplayerID ?? 0L;
			return _repelStepsByFarmer.TryGetValue(id, out int steps) ? steps : 0;
		}
		set
		{
			long id = Game1.player?.UniqueMultiplayerID ?? 0L;
			_repelStepsByFarmer[id] = Math.Max(0, value);
		}
	}

	public static string LocalId(string qualifiedId)
	{
		if (string.IsNullOrEmpty(qualifiedId)) return "";
		string id = qualifiedId;
		int idx = id.IndexOf(')');
		if (idx >= 0) id = id.Substring(idx + 1);
		return id;
	}

	public static string TmId(string moveName) => TmPrefix + moveName.Replace(' ', '_');

	public static string TmMoveName(string localId)
	{
		return localId.Substring(TmPrefix.Length).Replace('_', ' ');
	}

	public static bool IsTm(string localId) => localId != null && localId.StartsWith(TmPrefix, StringComparison.Ordinal);

	public static bool IsRepel(string localId) => localId == "PokemonRepel" || localId == "PokemonSuperRepel" || localId == "PokemonMaxRepel";

	public static bool IsVitamin(string localId)
	{
		string id = localId ?? "";
		if (id.StartsWith("Pokemon")) id = id.Substring(7);
		if (id.StartsWith("Poke_")) id = id.Substring(5);
		return id is "HPUp" or "Protein" or "Iron" or "Calcium" or "Zinc" or "Carbos" or "PPUp" or "PPMax";
	}

	public static bool IsMedicine(string localId) => localId == "PokemonPotion" || localId == "PokemonSuperPotion" || localId == "PokemonMaxPotion" || localId == "PokemonAntidote" || localId == "PokemonParalyzeHeal" || localId == "PokemonAwakening" || localId == "PokemonBurnHeal" || localId == "PokemonIceHeal" || localId == "PokemonFullHeal" || localId == "PokemonRevive" || localId == "PokemonRareCandy" || localId == "PokemonFriendshipCandy" || IsVitamin(localId);

	public static bool IsEvolutionItem(string localId)
	{
		if (string.IsNullOrEmpty(localId)) return false;
		string id = LocalId(localId);
		return id is "PokemonWaterStone" or "PokemonFireStone" or "PokemonThunderStone" or
			"PokemonLeafStone" or "PokemonMoonStone" or "PokemonSunStone" or
			"PokemonShinyStone" or "PokemonDuskStone" or "PokemonDawnStone" or
			"PokemonIceStone" or "PokemonOvalStone" or "PokemonLinkCable" or "LinkCable" or
			"PokemonLinkingCord" or "LinkingCord" or "PokemonMetalCoat" or "MetalCoat" or "Poke_MetalCoat" or
			"PokemonKingsRock" or "PokemonProtector" or "PokemonElectirizer" or "PokemonMagmarizer" or
			"PokemonUpGrade" or "PokemonDubiousDisc" or "PokemonReaperCloth" or "PokemonDragonScale" or
			"PokemonPrismScale" or "PokemonWhippedDream" or "PokemonSachet" or "PokemonRazorClaw" or "PokemonRazorFang" or
			"Poke_ScrollOfDarkness" or "ScrollOfDarkness" or "Poke_ScrollOfWaters" or "ScrollOfWaters" or
			"Poke_GigantamaxNut" or "GigantamaxNut" or "Poke_SunFlute" or "SunFlute" or
			"Poke_MoonFlute" or "MoonFlute" or "Poke_MemoryDrive_Normal" or "MemoryDrive_Normal" or
			"Poke_DragonPoisonBarb" or "DragonPoisonBarb";
	}

	public static bool IsFormChangeItem(string localId)
	{
		if (string.IsNullOrEmpty(localId)) return false;
		string id = LocalId(localId);
		if (id.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase)) id = id.Substring(5);
		return id is "MeteoritePiece" or "Rayquazite" or
			"PrisonBottle" or
			"DNA_Splicers" or "DNASplicers" or
			"NSolarizer" or "NLunarizer" or
			"ReinsOfUnity" or
			"RustedSword" or "RustedShield" or
			"GriseousCore" or "GriseousOrb" or
			"AdamantOrb" or "AdamantCrystal" or
			"LustrousOrb" or "LustrousGlobe" or
			"GracideaFlower" or "Gracidea" or
			"RevealGlass" or "RevealGlass_Love" or "CycloneMirror" or "BoltMirror" or "FertilityMirror" or
			"TealMask" or "WellspringMask" or "HearthflameMask" or "CornerstoneMask" or
			"ZygardeCube" or "IndigoDisk";
	}

	public static bool HasItemInInventoryOrHeld(PokemonInstance p, params string[] itemIds)
	{
		string held = PokemonHeldItemManager.NormalizeId(p?.HeldItem ?? "");
		foreach (var id in itemIds)
		{
			string clean = LocalId(id);
			if (clean.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(5);
			if (string.Equals(held, clean, StringComparison.OrdinalIgnoreCase)) return true;
			if (Game1.player != null)
			{
				if (Game1.player.Items.ContainsId(clean) ||
					Game1.player.Items.ContainsId("(O)" + clean) ||
					Game1.player.Items.ContainsId("Poke_" + clean) ||
					Game1.player.Items.ContainsId("(O)Poke_" + clean))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool CanSwitchFormInSummary(PokemonInstance p)
	{
		if (p?.Species == null) return false;
		string baseSp = (p.Species.BaseSpeciesName ?? p.Species.InternalName ?? "").ToLowerInvariant();

		return baseSp switch
		{
			"shaymin" => HasItemInInventoryOrHeld(p, "Gracidea", "GracideaFlower") && (Game1.timeOfDay >= 600 && Game1.timeOfDay < 1800),
			"deoxys" => HasItemInInventoryOrHeld(p, "MeteoritePiece", "Rayquazite"),
			"hoopa" => HasItemInInventoryOrHeld(p, "PrisonBottle"),
			"kyurem" => HasItemInInventoryOrHeld(p, "DNA_Splicers", "DNASplicers"),
			"necrozma" => HasItemInInventoryOrHeld(p, "NSolarizer", "NLunarizer"),
			"calyrex" => HasItemInInventoryOrHeld(p, "ReinsOfUnity"),
			"zygarde" => HasItemInInventoryOrHeld(p, "ZygardeCube"),
			"tornadus" or "thundurus" or "landorus" or "enamorus" => HasItemInInventoryOrHeld(p, "RevealGlass", "RevealGlass_Love", "CycloneMirror", "BoltMirror", "FertilityMirror"),
			"terapagos" => HasItemInInventoryOrHeld(p, "IndigoDisk"),
			"giratina" => HasItemInInventoryOrHeld(p, "GriseousCore", "GriseousOrb"),
			"dialga" => HasItemInInventoryOrHeld(p, "AdamantCrystal", "AdamantOrb"),
			"palkia" => HasItemInInventoryOrHeld(p, "LustrousGlobe", "LustrousOrb"),
			"zacian" => HasItemInInventoryOrHeld(p, "RustedSword"),
			"zamazenta" => HasItemInInventoryOrHeld(p, "RustedShield"),
			"ogerpon" => HasItemInInventoryOrHeld(p, "TealMask", "WellspringMask", "HearthflameMask", "CornerstoneMask"),
			"urshifu" => HasItemInInventoryOrHeld(p, "ScrollOfDarkness", "ScrollOfWaters"),
			_ => false
		};
	}

	public static bool CanApplyFormChange(string localId, PokemonInstance p)
	{
		if (p?.Species == null || string.IsNullOrEmpty(localId)) return false;
		string id = LocalId(localId);
		if (id.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase)) id = id.Substring(5);
		string baseSp = (p.Species.BaseSpeciesName ?? p.Species.InternalName ?? "").ToLowerInvariant();

		return id switch
		{
			"MeteoritePiece" or "Rayquazite" => baseSp == "deoxys",
			"PrisonBottle" => baseSp == "hoopa",
			"DNA_Splicers" or "DNASplicers" => baseSp == "kyurem",
			"NSolarizer" or "NLunarizer" => baseSp == "necrozma",
			"ReinsOfUnity" => baseSp == "calyrex",
			"RustedSword" => baseSp == "zacian",
			"RustedShield" => baseSp == "zamazenta",
			"GriseousCore" or "GriseousOrb" => baseSp == "giratina",
			"AdamantOrb" or "AdamantCrystal" => baseSp == "dialga",
			"LustrousOrb" or "LustrousGlobe" => baseSp == "palkia",
			"GracideaFlower" or "Gracidea" => baseSp == "shaymin",
			"RevealGlass" or "RevealGlass_Love" or "CycloneMirror" or "BoltMirror" or "FertilityMirror" =>
				baseSp is "tornadus" or "thundurus" or "landorus" or "enamorus",
			"TealMask" or "WellspringMask" or "HearthflameMask" or "CornerstoneMask" => baseSp == "ogerpon",
			"ZygardeCube" => baseSp == "zygarde",
			"IndigoDisk" => baseSp == "terapagos",
			_ => false
		};
	}

	public static bool CanUseFormOrEvoItemOnParty(string localId)
	{
		var team = PlayerParty.Instance?.Team;
		if (team == null || team.Count == 0) return false;
		foreach (var p in team)
		{
			if (p == null) continue;
			if (CanApplyFormChange(localId, p)) return true;
			if (IsEvolutionItem(localId) && PokemonEvolution.CheckEvolution(p, EvolutionTrigger.UseItem, localId) != null)
				return true;
		}
		return false;
	}

	public static bool UseFormOrEvoItem(string qualifiedId)
	{
		return Use(qualifiedId);
	}

	public static bool IsMilestoneRewardItem(string localId)
	{
		string clean = LocalId(localId);
		if (clean.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(5);
		return clean is "RunningShoes" or "CompetitiveCase" or "PortableHealMachine" or
			"BoxLink" or "PortablePC" or "PokemonPortablePC" or
			"MegaStoneSelector" or "AbilityPatch" or "MasterBallBundle";
	}

	/// <summary>True for items whose use is handled by this class (and which must never be placed / eaten).</summary>
	public static bool IsUsable(string qualifiedId)
	{
		string id = LocalId(qualifiedId);
		if (PokemonEncounterManager.IsSummonRelic(id) && !CanUseFormOrEvoItemOnParty(id))
			return false;
		return IsMedicine(id) || IsRepel(id) || IsTm(id) || IsEvolutionItem(id) || IsFormChangeItem(id) || IsVitamin(id) || IsMilestoneRewardItem(id);
	}

	public static string DisplayName(string localId)
	{
		string clean = localId ?? "";
		if (clean.StartsWith("Pokemon")) clean = clean.Substring(7);
		if (clean.StartsWith("Poke_")) clean = clean.Substring(5);

		if (clean is "RunningShoes") return "Tênis de Corrida";
		if (clean is "CompetitiveCase") return "Estojo Competitivo";
		if (clean is "PortableHealMachine") return "Centro Pokémon Portátil";
		if (clean is "BoxLink" or "PortablePC") return "Box Link Portátil";
		if (clean is "MegaStoneSelector") return "Cofre de Mega Pedras";
		if (clean is "AbilityPatch") return "Ability Patch";
		if (clean is "MasterBallBundle") return "Maleta de Master Balls";
		if (clean is "ShinyCharm") return "Amuleto Brilhante";
		if (clean is "HPUp") return "HP Up";
		if (clean is "Protein") return "Protein";
		if (clean is "Iron") return "Iron";
		if (clean is "Calcium") return "Calcium";
		if (clean is "Zinc") return "Zinc";
		if (clean is "Carbos") return "Carbos";
		if (clean is "PPUp") return "PP Up";
		if (clean is "PPMax") return "PP Max";
		if (clean is "ScrollOfDarkness") return "Pergaminho da Escuridão";
		if (clean is "ScrollOfWaters") return "Pergaminho das Águas";
		if (clean is "MeteoritePiece") return "Fragmento de Meteorito";
		if (clean is "PrisonBottle") return "Garrafa Prisão";
		if (clean is "DNA_Splicers" or "DNASplicers") return "DNA Splicers";
		if (clean is "NSolarizer") return "N-Solarizer";
		if (clean is "NLunarizer") return "N-Lunarizer";
		if (clean is "ReinsOfUnity") return "Rédeas da Unidade";
		if (clean is "RustedSword") return "Espada Enferrujada";
		if (clean is "RustedShield") return "Escudo Enferrujado";
		if (clean is "GriseousCore" or "GriseousOrb") return "Orbe Gris";
		if (clean is "AdamantOrb" or "AdamantCrystal") return "Cristal Diamante";
		if (clean is "LustrousOrb" or "LustrousGlobe") return "Globo Lustroso";
		if (clean is "GracideaFlower" or "Gracidea") return "Flor Gracídea";
		if (clean is "RevealGlass" or "RevealGlass_Love" or "CycloneMirror" or "BoltMirror" or "FertilityMirror") return "Espelho Revelador";
		if (clean is "TealMask") return "Máscara Turquesa";
		if (clean is "WellspringMask") return "Máscara da Fonte";
		if (clean is "HearthflameMask") return "Máscara da Chama";
		if (clean is "CornerstoneMask") return "Máscara do Alicerce";
		if (clean is "ZygardeCube") return "Cubo de Zygarde";
		if (clean is "IndigoDisk") return "Disco Índigo";

		return localId switch
		{
			"PokemonPotion" => "Potion",
			"PokemonSuperPotion" => "Super Potion",
			"PokemonMaxPotion" => "Max Potion",
			"PokemonAntidote" => "Antidote",
			"PokemonParalyzeHeal" => "Paralyze Heal",
			"PokemonAwakening" => "Awakening",
			"PokemonBurnHeal" => "Burn Heal",
			"PokemonIceHeal" => "Ice Heal",
			"PokemonFullHeal" => "Full Heal",
			"PokemonRevive" => "Revive",
			"PokemonRareCandy" => "Rare Candy",
			"PokemonFriendshipCandy" => "Friendship Candy",
			"PokemonRepel" => "Repel",
			"PokemonSuperRepel" => "Super Repel",
			"PokemonMaxRepel" => "Max Repel",
			"PokemonWaterStone" => "Pedra da Água",
			"PokemonFireStone" => "Pedra do Fogo",
			"PokemonThunderStone" => "Pedra do Trovão",
			"PokemonLeafStone" => "Pedra da Folha",
			"PokemonMoonStone" => "Pedra da Lua",
			"PokemonSunStone" => "Pedra do Sol",
			"PokemonShinyStone" => "Pedra do Brilho",
			"PokemonDuskStone" => "Pedra do Crepúsculo",
			"PokemonDawnStone" => "Pedra da Alvorada",
			"PokemonIceStone" => "Pedra do Gelo",
			"PokemonLinkCable" => "Cabo de Ligação",
			_ => IsTm(localId) ? "TM " + TmMoveName(localId) : localId
		};
	}

	/// <summary>Returns whether the item can currently be applied to this Pokémon.</summary>
	public static bool CanApply(string localId, PokemonInstance p)
	{
		if (p == null) return false;
		if (IsFormChangeItem(localId))
		{
			return CanApplyFormChange(localId, p);
		}
		if (IsEvolutionItem(localId))
		{
			return PokemonEvolution.CheckEvolution(p, EvolutionTrigger.UseItem, localId) != null;
		}

		if (IsVitamin(localId))
		{
			string vId = localId;
			if (vId.StartsWith("Pokemon")) vId = vId.Substring(7);
			if (vId.StartsWith("Poke_")) vId = vId.Substring(5);
			if (vId is "PPUp" or "PPMax") return p.Moves.Count > 0;
			return p.TotalEVs < 510;
		}

		if (IsTm(localId))
		{
			string moveName = TmMoveName(localId);
			return PokemonMove.CanLearn(p.Species, moveName);
		}

		switch (localId)
		{
			case "PokemonPotion":
			case "PokemonSuperPotion":
			case "PokemonMaxPotion":
				return !p.IsFainted && p.CurrentHP < p.MaxHP;
			case "PokemonAntidote":
				return !p.IsFainted && (p.Status == PokemonStatusCondition.Poison || p.Status == PokemonStatusCondition.Toxic);
			case "PokemonParalyzeHeal":
				return !p.IsFainted && p.Status == PokemonStatusCondition.Paralysis;
			case "PokemonAwakening":
				return !p.IsFainted && p.Status == PokemonStatusCondition.Sleep;
			case "PokemonBurnHeal":
				return !p.IsFainted && p.Status == PokemonStatusCondition.Burn;
			case "PokemonIceHeal":
				return !p.IsFainted && p.Status == PokemonStatusCondition.Freeze;
			case "PokemonFullHeal":
				return !p.IsFainted && (p.Status != PokemonStatusCondition.None || p.IsConfused);
			case "PokemonRevive":
				return p.IsFainted;
			case "PokemonRareCandy":
				return p.Level < 100;
			case "PokemonFriendshipCandy":
				return p.Friendship < 255;
			default:
				return false;
		}
	}

	/// <summary>Applies medicine to the Pokémon. Returns message (never null) and success flag.</summary>
	public static bool ApplyMedicine(string localId, PokemonInstance p, out string message)
	{
		message = "";
		string cleanId = localId ?? "";
		if (cleanId.StartsWith("Pokemon")) cleanId = cleanId.Substring(7);
		if (cleanId.StartsWith("Poke_")) cleanId = cleanId.Substring(5);

		switch (cleanId)
		{
			case "HPUp":
			{
				if (p.EV_HP >= 252 || p.TotalEVs >= 510) { message = $"{p.Nickname} já maximizou seu HP!"; return false; }
				p.AddEV(PokemonStat.HP, 10);
				p.RecalculateStats();
				message = $"{p.Nickname} ganhou +10 de esforço em HP!";
				Game1.playSound("reward");
				return true;
			}
			case "Protein":
			{
				if (p.EV_Attack >= 252 || p.TotalEVs >= 510) { message = $"{p.Nickname} já maximizou seu Ataque!"; return false; }
				p.AddEV(PokemonStat.Attack, 10);
				p.RecalculateStats();
				message = $"{p.Nickname} ganhou +10 de esforço em Ataque!";
				Game1.playSound("reward");
				return true;
			}
			case "Iron":
			{
				if (p.EV_Defense >= 252 || p.TotalEVs >= 510) { message = $"{p.Nickname} já maximizou sua Defesa!"; return false; }
				p.AddEV(PokemonStat.Defense, 10);
				p.RecalculateStats();
				message = $"{p.Nickname} ganhou +10 de esforço em Defesa!";
				Game1.playSound("reward");
				return true;
			}
			case "Calcium":
			{
				if (p.EV_SpAttack >= 252 || p.TotalEVs >= 510) { message = $"{p.Nickname} já maximizou seu Ataque Especial!"; return false; }
				p.AddEV(PokemonStat.SpAttack, 10);
				p.RecalculateStats();
				message = $"{p.Nickname} ganhou +10 de esforço em Ataque Especial!";
				Game1.playSound("reward");
				return true;
			}
			case "Zinc":
			{
				if (p.EV_SpDefense >= 252 || p.TotalEVs >= 510) { message = $"{p.Nickname} já maximizou sua Defesa Especial!"; return false; }
				p.AddEV(PokemonStat.SpDefense, 10);
				p.RecalculateStats();
				message = $"{p.Nickname} ganhou +10 de esforço em Defesa Especial!";
				Game1.playSound("reward");
				return true;
			}
			case "Carbos":
			{
				if (p.EV_Speed >= 252 || p.TotalEVs >= 510) { message = $"{p.Nickname} já maximizou sua Velocidade!"; return false; }
				p.AddEV(PokemonStat.Speed, 10);
				p.RecalculateStats();
				message = $"{p.Nickname} ganhou +10 de esforço em Velocidade!";
				Game1.playSound("reward");
				return true;
			}
			case "PPUp":
			{
				if (p.Moves.Count == 0) { message = $"{p.Nickname} não possui movimentos!"; return false; }
				var m = p.Moves[0];
				m.MaxPP = (int)(m.MaxPP * 1.2f);
				m.CurrentPP = Math.Min(m.CurrentPP + 5, m.MaxPP);
				message = $"O PP máximo de {m.Name} aumentou para {m.MaxPP}!";
				Game1.playSound("reward");
				return true;
			}
			case "PPMax":
			{
				if (p.Moves.Count == 0) { message = $"{p.Nickname} não possui movimentos!"; return false; }
				var m = p.Moves[0];
				m.MaxPP = (int)(m.MaxPP * 1.6f);
				m.CurrentPP = m.MaxPP;
				message = $"O PP máximo de {m.Name} foi maximizado para {m.MaxPP}!";
				Game1.playSound("reward");
				return true;
			}
		}

		switch (localId)
		{
			case "PokemonPotion":
			case "PokemonSuperPotion":
			case "PokemonMaxPotion":
			{
				if (p.IsFainted) { message = $"{p.Nickname} está desmaiado!"; return false; }
				if (p.CurrentHP >= p.MaxHP) { message = $"{p.Nickname} já está com a vida cheia!"; return false; }
				int amount = localId == "PokemonMaxPotion" ? p.MaxHP : (localId == "PokemonPotion" ? 50 : 100);
				int before = p.CurrentHP;
				p.Heal(amount);
				message = $"{p.Nickname} recuperou {p.CurrentHP - before} HP!";
				Game1.playSound("healSound");
				return true;
			}
			case "PokemonAntidote":
			{
				if (p.IsFainted) { message = $"{p.Nickname} está desmaiado!"; return false; }
				if (p.Status != PokemonStatusCondition.Poison && p.Status != PokemonStatusCondition.Toxic) { message = $"{p.Nickname} não está envenenado!"; return false; }
				p.CureStatus();
				message = $"{p.Nickname} foi curado do veneno!";
				Game1.playSound("healSound");
				return true;
			}
			case "PokemonParalyzeHeal":
			{
				if (p.IsFainted) { message = $"{p.Nickname} está desmaiado!"; return false; }
				if (p.Status != PokemonStatusCondition.Paralysis) { message = $"{p.Nickname} não está paralisado!"; return false; }
				p.CureStatus();
				message = $"{p.Nickname} foi curado da paralisia!";
				Game1.playSound("healSound");
				return true;
			}
			case "PokemonAwakening":
			{
				if (p.IsFainted) { message = $"{p.Nickname} está desmaiado!"; return false; }
				if (p.Status != PokemonStatusCondition.Sleep) { message = $"{p.Nickname} não está dormindo!"; return false; }
				p.CureStatus();
				message = $"{p.Nickname} acordou do sono profundo!";
				Game1.playSound("healSound");
				return true;
			}
			case "PokemonBurnHeal":
			{
				if (p.IsFainted) { message = $"{p.Nickname} está desmaiado!"; return false; }
				if (p.Status != PokemonStatusCondition.Burn) { message = $"{p.Nickname} não está queimado!"; return false; }
				p.CureStatus();
				message = $"{p.Nickname} teve suas queimaduras saradas!";
				Game1.playSound("healSound");
				return true;
			}
			case "PokemonIceHeal":
			{
				if (p.IsFainted) { message = $"{p.Nickname} está desmaiado!"; return false; }
				if (p.Status != PokemonStatusCondition.Freeze) { message = $"{p.Nickname} não está congelado!"; return false; }
				p.CureStatus();
				message = $"{p.Nickname} foi descongelado com sucesso!";
				Game1.playSound("healSound");
				return true;
			}
			case "PokemonFullHeal":
			{
				if (p.IsFainted) { message = $"{p.Nickname} está desmaiado!"; return false; }
				if (p.Status == PokemonStatusCondition.None && !p.IsConfused) { message = $"{p.Nickname} está perfeitamente saudável!"; return false; }
				p.CureStatus();
				message = $"{p.Nickname} teve todas as suas alterações de status curadas!";
				Game1.playSound("healSound");
				return true;
			}
			case "PokemonRevive":
			{
				if (!p.IsFainted) { message = $"{p.Nickname} não está desmaiado!"; return false; }
				p.Revive(0.5f);
				message = $"{p.Nickname} reviveu com {p.CurrentHP} HP!";
				Game1.playSound("healSound");
				return true;
			}
			case "PokemonRareCandy":
			{
				if (!p.GainLevel()) { message = $"{p.Nickname} já está no nível máximo!"; return false; }
				message = $"{p.Nickname} subiu para o nível {p.Level}!";
				Game1.playSound("reward");

				var evo = PokemonEvolution.CheckEvolution(p, EvolutionTrigger.LevelUp);
				PokemonSpecies evoTarget = evo != null ? PokemonSpecies.Get(evo.TargetSpecies) : null;

				string learnedMove = PokemonInstance.GetMoveLearnedAtLevel(p.Species, p.Level);
				bool alreadyKnows = false;
				if (!string.IsNullOrEmpty(learnedMove))
				{
					foreach (var m in p.Moves)
					{
						if (string.Equals(m.Name, learnedMove, StringComparison.OrdinalIgnoreCase))
						{
							alreadyKnows = true;
							break;
						}
					}
				}

				if (!string.IsNullOrEmpty(learnedMove) && !alreadyKnows)
				{
					if (p.Moves.Count < 4)
					{
						p.Moves.Add(PokemonMove.Get(learnedMove));
						PlayerParty.Instance.SaveParty();
						Game1.addHUDMessage(new HUDMessage($"{p.Nickname} aprendeu {learnedMove}!", 2));
						if (evoTarget != null)
						{
							Game1.activeClickableMenu = new PokemonEvolutionMenu(p, evoTarget);
						}
					}
					else
					{
						Game1.activeClickableMenu = new PokemonTargetMenu(p, learnedMove, choice =>
						{
							if (choice < 0)
							{
								Game1.addHUDMessage(new HUDMessage($"{p.Nickname} não aprendeu {learnedMove}.", 3));
							}
							else
							{
								string forgotten = p.Moves[choice].Name;
								p.Moves[choice] = PokemonMove.Get(learnedMove);
								PlayerParty.Instance.SaveParty();
								Game1.playSound("reward");
								Game1.addHUDMessage(new HUDMessage($"{p.Nickname} esqueceu {forgotten} e aprendeu {learnedMove}!", 2));
							}

							if (evoTarget != null)
							{
								Game1.activeClickableMenu = new PokemonEvolutionMenu(p, evoTarget);
							}
							else
							{
								Game1.exitActiveMenu();
							}
						});
					}
				}
				else if (evoTarget != null)
				{
					Game1.activeClickableMenu = new PokemonEvolutionMenu(p, evoTarget);
				}
				return true;
			}
			case "PokemonFriendshipCandy":
			{
				p.AdjustFriendship(255);
				message = $"{p.Nickname} agora tem amizade máxima com você! (255/255)";
				Game1.playSound("reward");

				var evo = PokemonEvolution.CheckEvolution(p, EvolutionTrigger.Friendship);
				PokemonSpecies evoTarget = evo != null ? PokemonSpecies.Get(evo.TargetSpecies) : null;
				if (evoTarget != null)
				{
					Game1.activeClickableMenu = new PokemonEvolutionMenu(p, evoTarget);
				}
				return true;
			}
			default:
				return false;
		}
	}

	/// <summary>Called from Object.performUseAction. Returns true when the item should be consumed right away.</summary>
	public static bool Use(string qualifiedId)
	{
		string id = LocalId(qualifiedId);

		if (IsRepel(id))
		{
			int steps = id == "PokemonRepel" ? 100 : (id == "PokemonSuperRepel" ? 200 : 250);
			RepelSteps = steps;
			Game1.playSound("powerup");
			Game1.addHUDMessage(new HUDMessage($"{DisplayName(id)} ativado! Pokémon selvagens ficam longe por {steps} passos.", 2));
			return true;
		}

		string cleanMilestoneId = id;
		if (cleanMilestoneId.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase)) cleanMilestoneId = cleanMilestoneId.Substring(5);

		if (cleanMilestoneId is "RunningShoes")
		{
			Game1.playSound("powerup");
			Game1.addHUDMessage(new HUDMessage("👟 Tênis de Corrida ativos! O bônus de velocidade (+1) já está ativo em sua mochila.", 2));
			return false;
		}

		if (cleanMilestoneId is "PortableHealMachine")
		{
			PlayerParty.Instance.HealAll();
			Game1.playSound("heal");
			Game1.addHUDMessage(new HUDMessage("✨ Toda a sua equipe Pokémon foi completamente curada e restaurada!", 2));
			return false;
		}

		if (cleanMilestoneId is "BoxLink" or "PortablePC")
		{
			Game1.activeClickableMenu = new PokemonStorageMenu(Game1.player);
			return false;
		}

		if (cleanMilestoneId is "CompetitiveCase")
		{
			Game1.player.Items.ReduceId("(O)" + id, 1);
			string[] compItems = new[] { "Poke_LifeOrb", "Poke_ChoiceBand", "Poke_ChoiceSpecs", "Poke_ChoiceScarf", "Poke_FocusSash", "Poke_Leftovers", "Poke_AssaultVest" };
			foreach (var itId in compItems)
			{
				var item = ItemRegistry.Create("(O)" + itId, 1, 0, allowNull: true) ?? new StardewValley.Object(itId, 1);
				if (item != null) Game1.player.addItemByMenuIfNecessary(item);
			}
			Game1.playSound("coin");
			Game1.addHUDMessage(new HUDMessage("🎁 Estojo aberto! Você recebeu Life Orb, Choice Scarf/Band/Specs, Focus Sash, Leftovers e Assault Vest!", 2));
			return false;
		}

		if (cleanMilestoneId is "MasterBallBundle")
		{
			Game1.player.Items.ReduceId("(O)" + id, 1);
			var mb = ItemRegistry.Create("(O)MasterBall", 5, 0, allowNull: true) ?? new StardewValley.Object("MasterBall", 5);
			if (mb != null) Game1.player.addItemByMenuIfNecessary(mb);
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage("👑 Remessa da Silph Co aberta! Você recebeu 5x Master Ball!", 2));
			return false;
		}

		if (cleanMilestoneId is "MegaStoneSelector")
		{
			Game1.player.Items.ReduceId("(O)" + id, 1);
			string[] stones = new[] { "CharizarditeX", "CharizarditeY", "MewtwoniteX", "MewtwoniteY", "Gengarite", "Lucarionite", "Salamencite", "Garchompite" };
			foreach (var ms in stones)
			{
				var item = ItemRegistry.Create("(O)" + ms, 1, 0, allowNull: true) ?? new StardewValley.Object(ms, 1);
				if (item != null) Game1.player.addItemByMenuIfNecessary(item);
			}
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage("💎 Cofre aberto! Mega Pedras ancestrais (Charizardite, Mewtwonite, Gengarite, Lucarionite, etc.) recebidas!", 2));
			return false;
		}

		if (cleanMilestoneId is "AbilityPatch")
		{
			var pTeam = PlayerParty.Instance.Team;
			if (pTeam.Count == 0)
			{
				Game1.showRedMessage("Você não possui Pokémon na equipe!");
				return false;
			}

			Game1.activeClickableMenu = new PokemonTargetMenu(
				"Ability Patch",
				p => p != null && !string.IsNullOrEmpty(PokemonAbility.GetHiddenAbility(p.Species)),
				idx =>
				{
					var p = pTeam[idx];
					string ha = PokemonAbility.GetHiddenAbility(p.Species);
					if (string.IsNullOrEmpty(ha) || p.Ability == ha)
					{
						Game1.playSound("cancel");
						Game1.showRedMessage($"{p.Nickname} já possui sua Habilidade Oculta!");
						return;
					}
					p.Ability = ha;
					Game1.player.Items.ReduceId("(O)" + id, 1);
					Game1.playSound("powerup");
					Game1.addHUDMessage(new HUDMessage($"✨ {p.Nickname} despertou sua Habilidade Oculta: {PokemonAbility.GetDisplayName(ha)}!", 2));
					PlayerParty.Instance.SaveParty();
				});
			return false;
		}

		var team = PlayerParty.Instance.Team;
		if (team.Count == 0)
		{
			Game1.showRedMessage("Você não possui Pokémon!");
			return false;
		}

		if (IsFormChangeItem(id))
		{
			Game1.activeClickableMenu = new PokemonTargetMenu(
				DisplayName(id),
				p => CanApplyFormChange(id, p),
				idx =>
				{
					var p = team[idx];
					ApplyFormChangeOrMenu(p, id, null);
				});
			return false;
		}

		if (IsEvolutionItem(id))
		{
			Game1.activeClickableMenu = new PokemonTargetMenu(
				DisplayName(id),
				p => CanApply(id, p),
				idx =>
				{
					var p = team[idx];
					var evo = PokemonEvolution.CheckEvolution(p, EvolutionTrigger.UseItem, id);
					if (evo != null)
					{
						Game1.player.Items.ReduceId("(O)" + id, 1);
						PokemonSpecies target = PokemonSpecies.Get(evo.TargetSpecies);
						Game1.activeClickableMenu = new PokemonEvolutionMenu(p, target, () =>
						{
							PlayerParty.Instance.SaveParty();
						});
					}
					else
					{
						Game1.playSound("cancel");
						Game1.showRedMessage($"{p.Nickname} não pode evoluir com {DisplayName(id)}!");
					}
				});
			return false;
		}

		if (IsMedicine(id))
		{
			Game1.activeClickableMenu = new PokemonTargetMenu(
				DisplayName(id),
				p => CanApply(id, p),
				idx =>
				{
					var p = team[idx];
					string cleanMed = id;
					if (cleanMed.StartsWith("Pokemon")) cleanMed = cleanMed.Substring(7);
					if (cleanMed.StartsWith("Poke_")) cleanMed = cleanMed.Substring(5);

					if (cleanMed is "PPUp" or "PPMax")
					{
						if (p.Moves.Count == 0)
						{
							Game1.playSound("cancel");
							Game1.addHUDMessage(new HUDMessage($"{p.Nickname} não possui movimentos!", 3));
							return;
						}
						Game1.activeClickableMenu = new PokemonTargetMenu(p, isPpBoost: true, DisplayName(id), moveIdx =>
						{
							if (moveIdx < 0 || moveIdx >= p.Moves.Count)
							{
								Game1.exitActiveMenu();
								return;
							}
							var m = p.Moves[moveIdx];
							int oldMax = m.MaxPP;
							if (cleanMed == "PPUp")
							{
								m.MaxPP = Math.Max(oldMax + 1, (int)Math.Round(oldMax * 1.2f));
								m.CurrentPP = Math.Min(m.CurrentPP + (m.MaxPP - oldMax), m.MaxPP);
							}
							else
							{
								m.MaxPP = Math.Max(oldMax + 3, (int)Math.Round(oldMax * 1.6f));
								m.CurrentPP = m.MaxPP;
							}
							Game1.player.Items.ReduceId("(O)" + id, 1);
							PlayerParty.Instance.SaveParty();
							Game1.playSound("reward");
							Game1.addHUDMessage(new HUDMessage($"O PP máximo de {m.Name} ({p.Nickname}) subiu de {oldMax} para {m.MaxPP}!", 2));
							Game1.exitActiveMenu();
						});
						return;
					}

					if (id == "PokemonRareCandy")
					{
						if (!p.GainLevel())
						{
							Game1.playSound("cancel");
							Game1.addHUDMessage(new HUDMessage($"{p.Nickname} já está no nível máximo!", 3));
							return;
						}
						Game1.player.Items.ReduceId("(O)" + id, 1);
						PlayerParty.Instance.SaveParty();
						Game1.playSound("reward");
						Game1.addHUDMessage(new HUDMessage($"{p.Nickname} subiu para o nível {p.Level}!", 2));

						var evo = PokemonEvolution.CheckEvolution(p, EvolutionTrigger.LevelUp);
						if (evo != null)
						{
							PokemonSpecies target = PokemonSpecies.Get(evo.TargetSpecies);
							if (target != null)
							{
								Game1.activeClickableMenu = new PokemonEvolutionMenu(p, target, () =>
								{
									PlayerParty.Instance.SaveParty();
								});
								return;
							}
						}
						Game1.exitActiveMenu();
						return;
					}

					if (ApplyMedicine(id, p, out string msg))
					{
						Game1.player.Items.ReduceId("(O)" + id, 1);
						PlayerParty.Instance.SaveParty();
						Game1.addHUDMessage(new HUDMessage(msg, 2));
						Game1.exitActiveMenu();
					}
					else
					{
						Game1.playSound("cancel");
						Game1.addHUDMessage(new HUDMessage(msg, 3));
					}
				});
			return false;
		}

		if (IsTm(id))
		{
			string moveName = TmMoveName(id);
			Game1.activeClickableMenu = new PokemonTargetMenu(
				"TM " + moveName,
				p => PokemonMove.CanLearn(p?.Species, moveName),
				idx => TeachTm(id, moveName, team[idx]));
			return false;
		}

		return false;
	}

	public static void OpenFormSwitchFromSummary(PokemonInstance p, IClickableMenu returnMenu = null)
	{
		ApplyFormChangeOrMenu(p, "SummarySwitch", returnMenu);
	}

	public static void ApplyFormChangeOrMenu(PokemonInstance p, string itemId, IClickableMenu returnMenu = null)
	{
		if (p?.Species == null) return;
		if (!CanSwitchFormInSummary(p) && string.Equals(itemId, "SummarySwitch", StringComparison.OrdinalIgnoreCase))
		{
			Game1.playSound("cancel");
			Game1.addHUDMessage(new HUDMessage($"Você não possui o item sagrado necessário para alternar a forma de {p.Nickname}!", 3));
			return;
		}

		string baseSp = (p.Species.BaseSpeciesName ?? p.Species.InternalName ?? "").ToLowerInvariant();
		string curSp = (p.Species.InternalName ?? "").ToLowerInvariant();
		string cleanItem = LocalId(itemId ?? "");
		if (cleanItem.StartsWith("Poke_", StringComparison.OrdinalIgnoreCase)) cleanItem = cleanItem.Substring(5);

		void FinishTransform(string targetSpeciesKey, string signatureMove = null)
		{
			var targetSp = PokemonSpecies.Get(targetSpeciesKey);
			if (targetSp == null)
			{
				Game1.playSound("cancel");
				Game1.addHUDMessage(new HUDMessage($"Forma '{targetSpeciesKey}' não encontrada!", 3));
				return;
			}
			if (p.ChangeForm(targetSp, signatureMove))
			{
				PlayerParty.Instance.SaveParty();
				Game1.playSound("wand");
				Game1.addHUDMessage(new HUDMessage($"✨ {p.Nickname} assumiu a forma {targetSp.Name}!", 2));
			}
			else
			{
				Game1.playSound("smallSelect");
				Game1.addHUDMessage(new HUDMessage($"{p.Nickname} já está na forma {targetSp.Name}.", 2));
			}
			if (returnMenu != null)
				Game1.activeClickableMenu = returnMenu;
			else
				Game1.exitActiveMenu();
		}

		void ShowFormMenu(string title, (string Label, string Key, string Move)[] choices)
		{
			var labels = new List<string>(choices.Length);
			foreach (var c in choices) labels.Add(c.Label);
			Game1.activeClickableMenu = new PokemonTargetMenu(title, labels, pickIdx =>
			{
				if (pickIdx < 0 || pickIdx >= choices.Length)
				{
					if (returnMenu != null) Game1.activeClickableMenu = returnMenu;
					else Game1.exitActiveMenu();
					return;
				}
				var chosen = choices[pickIdx];
				FinishTransform(chosen.Key, chosen.Move);
			});
		}

		switch (baseSp)
		{
			case "deoxys":
				ShowFormMenu($"Escolha a Forma de {p.Nickname}", new[]
				{
					("Deoxys (Forma Normal - Equilibrada)", "deoxys", "Psycho Boost"),
					("Deoxys (Forma Ataque - 180 Atk/SpA)", "deoxys/attack", "Psycho Boost"),
					("Deoxys (Forma Defesa - 160 Def/SpD)", "deoxys/defense", "Psycho Boost"),
					("Deoxys (Forma Velocidade - 180 Spe)", "deoxys/speed", "Psycho Boost")
				});
				return;

			case "hoopa":
				if (curSp == "hoopa/unbound")
					FinishTransform("hoopa", "Hyperspace Hole");
				else
					FinishTransform("hoopa/unbound", "Hyperspace Fury");
				return;

			case "zacian":
				if (curSp == "zacian/crowned_sword")
					FinishTransform("zacian", "Play Rough");
				else
					FinishTransform("zacian/crowned_sword", "Behemoth Blade");
				return;

			case "zamazenta":
				if (curSp == "zamazenta/crowned_shield")
					FinishTransform("zamazenta", "Close Combat");
				else
					FinishTransform("zamazenta/crowned_shield", "Behemoth Bash");
				return;

			case "giratina":
				if (curSp == "giratina/origin")
					FinishTransform("giratina", "Shadow Force");
				else
					FinishTransform("giratina/origin", "Shadow Force");
				return;

			case "dialga":
				if (curSp == "dialga/origin")
					FinishTransform("dialga", "Roar of Time");
				else
					FinishTransform("dialga/origin", "Roar of Time");
				return;

			case "palkia":
				if (curSp == "palkia/origin")
					FinishTransform("palkia", "Spacial Rend");
				else
					FinishTransform("palkia/origin", "Spacial Rend");
				return;

			case "shaymin":
				if (curSp == "shaymin/sky")
					FinishTransform("shaymin", "Seed Flare");
				else
					FinishTransform("shaymin/sky", "Air Slash");
				return;

			case "tornadus":
				if (curSp == "tornadus/therian")
					FinishTransform("tornadus", "Bleakwind Storm");
				else
					FinishTransform("tornadus/therian", "Bleakwind Storm");
				return;

			case "thundurus":
				if (curSp == "thundurus/therian")
					FinishTransform("thundurus", "Wildbolt Storm");
				else
					FinishTransform("thundurus/therian", "Wildbolt Storm");
				return;

			case "landorus":
				if (curSp == "landorus/therian")
					FinishTransform("landorus", "Sandsear Storm");
				else
					FinishTransform("landorus/therian", "Sandsear Storm");
				return;

			case "enamorus":
				if (curSp == "enamorus/therian")
					FinishTransform("enamorus", "Springtide Storm");
				else
					FinishTransform("enamorus/therian", "Springtide Storm");
				return;

			case "urshifu":
				if (curSp == "urshifu/rapid_strike")
					FinishTransform("urshifu", "Wicked Blow");
				else
					FinishTransform("urshifu/rapid_strike", "Surging Strikes");
				return;

			case "kyurem":
				ShowFormMenu($"DNA Splicers: Escolha a Fusão de {p.Nickname}", new[]
				{
					("Kyurem (Forma Base - Dragon/Ice)", "kyurem", "Glaciate"),
					("White Kyurem (Fusão Reshiram - 170 SpA)", "kyurem/white", "Ice Burn"),
					("Black Kyurem (Fusão Zekrom - 170 Atk)", "kyurem/black", "Freeze Shock")
				});
				return;

			case "necrozma":
				if (cleanItem == "NSolarizer")
				{
					if (curSp == "necrozma/dusk_mane")
						FinishTransform("necrozma", "Photon Geyser");
					else
						FinishTransform("necrozma/dusk_mane", "Sunsteel Strike");
					return;
				}
				if (cleanItem == "NLunarizer")
				{
					if (curSp == "necrozma/dawn_wings")
						FinishTransform("necrozma", "Photon Geyser");
					else
						FinishTransform("necrozma/dawn_wings", "Moongeist Beam");
					return;
				}
				ShowFormMenu($"Escolha a Fusão de {p.Nickname}", new[]
				{
					("Necrozma (Forma Base - Psychic)", "necrozma", "Photon Geyser"),
					("Dusk Mane Necrozma (Fusão Solgaleo - Psychic/Steel)", "necrozma/dusk_mane", "Sunsteel Strike"),
					("Dawn Wings Necrozma (Fusão Lunala - Psychic/Ghost)", "necrozma/dawn_wings", "Moongeist Beam")
				});
				return;

			case "calyrex":
				ShowFormMenu($"Rédeas da Unidade: Escolha a Montaria de {p.Nickname}", new[]
				{
					("Calyrex (Forma Base - Psychic/Grass)", "calyrex", "Energy Ball"),
					("Ice Rider Calyrex (Montaria Glastrier - 165 Atk)", "calyrex/ice", "Glacial Lance"),
					("Shadow Rider Calyrex (Montaria Spectrier - 165 SpA)", "calyrex/shadow", "Astral Barrage")
				});
				return;

			case "ogerpon":
				if (cleanItem == "WellspringMask" && curSp != "ogerpon/wellspring")
				{
					FinishTransform("ogerpon/wellspring", "Ivy Cudgel");
					return;
				}
				if (cleanItem == "HearthflameMask" && curSp != "ogerpon/hearthflame")
				{
					FinishTransform("ogerpon/hearthflame", "Ivy Cudgel");
					return;
				}
				if (cleanItem == "CornerstoneMask" && curSp != "ogerpon/cornerstone")
				{
					FinishTransform("ogerpon/cornerstone", "Ivy Cudgel");
					return;
				}
				ShowFormMenu($"Escolha a Máscara de {p.Nickname}", new[]
				{
					("Máscara Turquesa (Grass)", "ogerpon", "Ivy Cudgel"),
					("Máscara da Fonte (Grass / Water)", "ogerpon/wellspring", "Ivy Cudgel"),
					("Máscara da Chama (Grass / Fire)", "ogerpon/hearthflame", "Ivy Cudgel"),
					("Máscara do Alicerce (Grass / Rock)", "ogerpon/cornerstone", "Ivy Cudgel")
				});
				return;

			case "zygarde":
				ShowFormMenu($"Cubo de Zygarde: Escolha a Forma de {p.Nickname}", new[]
				{
					("Zygarde 10% (Forma Canina - 115 Spe)", "zygarde/10_percent", "Thousand Arrows"),
					("Zygarde 50% (Forma Guardião - Equilibrada)", "zygarde", "Thousand Arrows"),
					("Zygarde 100% (Forma Completa - 216 HP)", "zygarde/complete", "Core Enforcer")
				});
				return;

			case "terapagos":
				ShowFormMenu($"Disco Índigo: Escolha a Forma de {p.Nickname}", new[]
				{
					("Terapagos (Forma Normal)", "terapagos", "Tera Starstorm"),
					("Terapagos (Forma Terastal)", "terapagos/terastal", "Tera Starstorm"),
					("Terapagos (Forma Stellar)", "terapagos/stellar", "Tera Starstorm")
				});
				return;
		}
	}

	private static void TeachTm(string id, string moveName, PokemonInstance p)
	{
		if (!PokemonMove.CanLearn(p?.Species, moveName))
		{
			Game1.playSound("cancel");
			Game1.addHUDMessage(new HUDMessage($"{p.Nickname} não pode aprender {moveName}!", 3));
			return;
		}

		foreach (var m in p.Moves)
		{
			if (m.Name.Equals(moveName, StringComparison.OrdinalIgnoreCase))
			{
				Game1.playSound("cancel");
				Game1.addHUDMessage(new HUDMessage($"{p.Nickname} já conhece {moveName}!", 3));
				return;
			}
		}

		if (p.Moves.Count < 4)
		{
			p.Moves.Add(PokemonMove.Get(moveName));
			Game1.player.Items.ReduceId("(O)" + id, 1);
			PlayerParty.Instance.SaveParty();
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage($"{p.Nickname} aprendeu {moveName}!", 2));
			Game1.exitActiveMenu();
			return;
		}

		Game1.activeClickableMenu = new PokemonTargetMenu(p, moveName, choice =>
		{
			if (choice < 0)
			{
				Game1.addHUDMessage(new HUDMessage($"{p.Nickname} não aprendeu {moveName}.", 3));
				Game1.exitActiveMenu();
				return;
			}
			string forgotten = p.Moves[choice].Name;
			p.Moves[choice] = PokemonMove.Get(moveName);
			Game1.player.Items.ReduceId("(O)" + id, 1);
			PlayerParty.Instance.SaveParty();
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage($"{p.Nickname} esqueceu {forgotten} e aprendeu {moveName}!", 2));
			Game1.exitActiveMenu();
		});
	}
}

/// <summary>Menu used to pick a target Pokémon (or a move/option).</summary>
public class PokemonTargetMenu : IClickableMenu
{
	private readonly string _title;
	private readonly Func<PokemonInstance, bool> _canTarget;
	private readonly Action<int> _onPick;

	private readonly PokemonInstance _learner;
	private readonly string _newMove;
	private readonly bool _moveMode;
	private readonly bool _isPpBoost;
	private readonly IList<string> _options;
	private readonly bool _optionMode;

	private readonly List<ClickableComponent> _rows = new();
	private ClickableComponent _cancel;
	private int _gamepadIndex = 0;

	public override bool areGamePadControlsImplemented() => true;

	public override void receiveGamePadButton(Buttons button)
	{
		if (button == Buttons.B || button == Buttons.Back)
		{
			Cancel();
			return;
		}
		int totalItems = _rows.Count + 1; // rows + cancel button
		if (button == Buttons.DPadUp || button == Buttons.LeftThumbstickUp)
		{
			_gamepadIndex = (_gamepadIndex - 1 + totalItems) % totalItems;
			Game1.playSound("shiny");
			return;
		}
		if (button == Buttons.DPadDown || button == Buttons.LeftThumbstickDown)
		{
			_gamepadIndex = (_gamepadIndex + 1) % totalItems;
			Game1.playSound("shiny");
			return;
		}
		if (button == Buttons.A || button == Buttons.Start)
		{
			if (_gamepadIndex == _rows.Count)
			{
				Cancel();
			}
			else if (_gamepadIndex >= 0 && _gamepadIndex < _rows.Count)
			{
				var r = _rows[_gamepadIndex].bounds;
				receiveLeftClick(r.Center.X, r.Center.Y);
			}
		}
	}

	public PokemonTargetMenu(string title, Func<PokemonInstance, bool> canTarget, Action<int> onPick)
		: base(0, 0, 560, 100 + 6 * 58 + 60)
	{
		_title = title;
		_canTarget = canTarget;
		_onPick = onPick;
		Layout();
	}

	/// <summary>Move-replacement mode: choose which of the 4 moves to forget (or -1 to cancel).</summary>
	public PokemonTargetMenu(PokemonInstance learner, string newMove, Action<int> onChoice)
		: base(0, 0, 560, 100 + 4 * 58 + 80)
	{
		_moveMode = true;
		_learner = learner;
		_newMove = newMove;
		_title = $"{learner.Nickname} quer aprender {newMove}";
		_onPick = onChoice;
		Layout();
	}

	/// <summary>PP Boost move selection mode: choose which move of the Pokémon receives PP Up / PP Max.</summary>
	public PokemonTargetMenu(PokemonInstance learner, bool isPpBoost, string itemLabel, Action<int> onChoice)
		: base(0, 0, 560, 100 + 4 * 58 + 80)
	{
		_moveMode = true;
		_isPpBoost = isPpBoost;
		_learner = learner;
		_newMove = itemLabel;
		_title = $"{itemLabel}: Escolha o golpe de {learner.Nickname}";
		_onPick = onChoice;
		Layout();
	}

	/// <summary>Custom option list mode (used for multi-form/fusion selection).</summary>
	public PokemonTargetMenu(string title, IList<string> options, Action<int> onPickOption)
		: base(0, 0, 620, 100 + Math.Max(2, options?.Count ?? 2) * 58 + 80)
	{
		_optionMode = true;
		_options = options ?? Array.Empty<string>();
		_title = title;
		_onPick = onPickOption;
		Layout();
	}

	private void Layout()
	{
		xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
		yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
		_rows.Clear();
		int count = _optionMode ? _options.Count : (_moveMode ? 4 : Math.Min(6, PlayerParty.Instance.Team.Count));
		for (int i = 0; i < count; i++)
		{
			_rows.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 28, yPositionOnScreen + 86 + i * 58, width - 56, 52), i.ToString()));
		}
		string cancelLabel = (_moveMode && !_isPpBoost) ? "Não aprender" : "Cancelar";
		_cancel = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 190, yPositionOnScreen + height - 66, 160, 42), "cancel", cancelLabel);
	}

	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		Layout();
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape)
		{
			Cancel();
			return;
		}
		base.receiveKeyPress(key);
	}

	private void Cancel()
	{
		Game1.playSound("bigDeSelect");
		if (_moveMode || _optionMode) _onPick(-1);
		else Game1.exitActiveMenu();
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (_cancel.containsPoint(x, y))
		{
			Cancel();
			return;
		}
		for (int i = 0; i < _rows.Count; i++)
		{
			if (!_rows[i].containsPoint(x, y)) continue;
			if (_optionMode)
			{
				if (i < _options.Count)
				{
					Game1.playSound("smallSelect");
					_onPick(i);
				}
				return;
			}
			if (_moveMode)
			{
				if (i < _learner.Moves.Count)
				{
					Game1.playSound("smallSelect");
					_onPick(i);
				}
				return;
			}
			var p = PlayerParty.Instance.Team[i];
			if (_canTarget(p))
			{
				Game1.playSound("smallSelect");
				_onPick(i);
			}
			else
			{
				Game1.playSound("cancel");
			}
			return;
		}
	}

	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
		IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

		PokemonUI.Text(b, Game1.dialogueFont, _title, new Rectangle(xPositionOnScreen + 30, yPositionOnScreen + 20, width - 60, 46), Color.SaddleBrown, center: true, maxScale: 0.9f);

		int mx = Game1.getMouseX(true);
		int my = Game1.getMouseY(true);
		bool isGamepad = Game1.options.gamepadControls || Game1.input.GetGamePadState().IsConnected;

		for (int i = 0; i < _rows.Count; i++)
		{
			Rectangle r = _rows[i].bounds;
			bool hov = r.Contains(mx, my) || (isGamepad && _gamepadIndex == i);

			if (_optionMode)
			{
				IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, hov ? Color.LemonChiffon : Color.White);
				PokemonUI.Text(b, Game1.smallFont, _options[i], new Rectangle(r.X + 16, r.Y + 10, r.Width - 32, 32), hov ? Color.DarkSlateBlue : Game1.textColor, maxScale: 0.88f);
				continue;
			}

			if (_moveMode)
			{
				bool has = i < _learner.Moves.Count;
				IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, has ? (hov ? Color.LemonChiffon : Color.White) : Color.Gray * 0.4f);
				if (!has) continue;
				var m = _learner.Moves[i];
				string prefix = _isPpBoost ? "Aumentar PP" : "Esquecer";
				Color textCol = hov ? (_isPpBoost ? Color.DarkGreen : Color.DarkRed) : Game1.textColor;
				PokemonUI.Text(b, Game1.smallFont, $"{prefix}: {m.Name}", new Rectangle(r.X + 16, r.Y + 6, r.Width - 190, 26), textCol, maxScale: 0.9f);
				PokemonUI.Text(b, Game1.tinyFont, $"{m.Type}  Pwr {m.Power}  PP {m.CurrentPP}/{m.MaxPP}", new Rectangle(r.X + 16, r.Y + 30, r.Width - 190, 18), Color.DimGray, maxScale: 0.9f);
				DrawTypePill(b, m.Type, r.Right - 112, r.Y + 17);
				continue;
			}

			var p = PlayerParty.Instance.Team[i];
			bool ok = _canTarget(p);
			Color box = !ok ? Color.Gray * 0.55f : (hov ? Color.LemonChiffon : Color.White);
			IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, box);

			Texture2D icon = p.GetIconSprite(Game1.graphics.GraphicsDevice) ?? p.GetFrontSprite(Game1.graphics.GraphicsDevice);
			if (icon != null)
			{
				b.Draw(icon, new Vector2(r.X + 10, r.Y + 10), p.GetIconSourceRect(), p.IsFainted ? Color.DimGray : Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
			}
			PokemonUI.Text(b, Game1.smallFont, p.Nickname, new Rectangle(r.X + 52, r.Y + 6, 190, 24), p.IsFainted ? Color.DarkRed : Game1.textColor, maxScale: 0.85f);
			PokemonUI.Text(b, Game1.tinyFont, $"Lv.{p.Level}", new Rectangle(r.X + 52, r.Y + 30, 80, 16), Color.DimGray, maxScale: 0.9f);

			int hpBarW = 140;
			int hpBarX = r.Right - hpBarW - 16;
			if (p.IsFainted)
			{
				Rectangle fBadge = new Rectangle(hpBarX - 28, r.Y + 10, 78, 16);
				b.Draw(Game1.staminaRect, fBadge, new Color(180, 40, 40));
				PokemonUI.Text(b, Game1.tinyFont, "DESMAIADO", fBadge, Color.White, center: true, maxScale: 0.70f);
				PokemonUI.HpBar(b, new Rectangle(fBadge.Right + 6, r.Y + 12, hpBarW - 56, 12), 0f);
			}
			else
			{
				PokemonUI.HpTag(b, new Rectangle(hpBarX - 28, r.Y + 10, 24, 14));
				PokemonUI.HpBar(b, new Rectangle(hpBarX, r.Y + 11, hpBarW, 12), (float)p.CurrentHP / Math.Max(1, p.MaxHP));
				PokemonUI.Text(b, Game1.tinyFont, $"{p.CurrentHP}/{p.MaxHP}", new Rectangle(hpBarX, r.Y + 28, hpBarW, 16), Color.DarkSlateGray, center: true, maxScale: 0.85f);
			}
		}

		bool ch = _cancel.containsPoint(mx, my) || (isGamepad && _gamepadIndex == _rows.Count);
		IClickableMenu.drawTextureBox(b, _cancel.bounds.X, _cancel.bounds.Y, _cancel.bounds.Width, _cancel.bounds.Height, ch ? Color.Wheat : Color.White);
		PokemonUI.Text(b, Game1.smallFont, _cancel.label, new Rectangle(_cancel.bounds.X + 10, _cancel.bounds.Y + 6, _cancel.bounds.Width - 20, _cancel.bounds.Height - 12), Game1.textColor, center: true, maxScale: 0.85f);

		drawMouse(b);
	}

	private static void DrawTypePill(SpriteBatch b, PokemonType type, int px, int py)
	{
		string name = type.ToString();
		Rectangle r = new Rectangle(px, py, 96, 16);
		b.Draw(Game1.staminaRect, r, PokemonTypeColors.Get(type));
		PokemonUI.Border(b, r, 1, Color.White * 0.45f);
		PokemonUI.Text(b, Game1.tinyFont, name, new Rectangle(px + 2, py, 92, 16), Color.White, center: true, maxScale: 0.7f);
	}
}
