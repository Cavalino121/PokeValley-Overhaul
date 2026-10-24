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

	public static bool IsEvolutionItem(string localId) => localId != null && (
		localId == "PokemonWaterStone" || localId == "PokemonFireStone" || localId == "PokemonThunderStone" ||
		localId == "PokemonLeafStone" || localId == "PokemonMoonStone" || localId == "PokemonSunStone" ||
		localId == "PokemonShinyStone" || localId == "PokemonDuskStone" || localId == "PokemonDawnStone" ||
		localId == "PokemonIceStone" || localId == "PokemonLinkCable" || localId == "LinkCable");

	/// <summary>True for items whose use is handled by this class (and which must never be placed / eaten).</summary>
	public static bool IsUsable(string qualifiedId)
	{
		string id = LocalId(qualifiedId);
		return IsMedicine(id) || IsRepel(id) || IsTm(id) || IsEvolutionItem(id) || IsVitamin(id);
	}

	public static string DisplayName(string localId)
	{
		string clean = localId ?? "";
		if (clean.StartsWith("Pokemon")) clean = clean.Substring(7);
		if (clean.StartsWith("Poke_")) clean = clean.Substring(5);

		if (clean is "HPUp") return "HP Up";
		if (clean is "Protein") return "Protein";
		if (clean is "Iron") return "Iron";
		if (clean is "Calcium") return "Calcium";
		if (clean is "Zinc") return "Zinc";
		if (clean is "Carbos") return "Carbos";
		if (clean is "PPUp") return "PP Up";
		if (clean is "PPMax") return "PP Max";

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

		var team = PlayerParty.Instance.Team;
		if (team.Count == 0)
		{
			Game1.showRedMessage("Você não possui Pokémon!");
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

/// <summary>Menu used to pick a target Pokémon (or the move to replace when learning a TM).</summary>
public class PokemonTargetMenu : IClickableMenu
{
	private readonly string _title;
	private readonly Func<PokemonInstance, bool> _canTarget;
	private readonly Action<int> _onPick;

	private readonly PokemonInstance _learner;
	private readonly string _newMove;
	private readonly bool _moveMode;

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

	private void Layout()
	{
		xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
		yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
		_rows.Clear();
		int count = _moveMode ? 4 : Math.Min(6, PlayerParty.Instance.Team.Count);
		for (int i = 0; i < count; i++)
		{
			_rows.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 28, yPositionOnScreen + 86 + i * 58, width - 56, 52), i.ToString()));
		}
		_cancel = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 190, yPositionOnScreen + height - 66, 160, 42), "cancel", _moveMode ? "Não aprender" : "Cancelar");
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
		if (_moveMode) _onPick(-1);
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

			if (_moveMode)
			{
				bool has = i < _learner.Moves.Count;
				IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, has ? (hov ? Color.LemonChiffon : Color.White) : Color.Gray * 0.4f);
				if (!has) continue;
				var m = _learner.Moves[i];
				PokemonUI.Text(b, Game1.smallFont, $"Esquecer: {m.Name}", new Rectangle(r.X + 16, r.Y + 6, r.Width - 190, 26), hov ? Color.DarkRed : Game1.textColor, maxScale: 0.9f);
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
