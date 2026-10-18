using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Triggers;

namespace StardewValley.Buffs;

/// <summary>Manages buffs for a player.</summary>
public class BuffManager : INetObject<NetFields>
{
	/// <summary>The player whose buffs are managed by the instance.</summary>
	protected Farmer Player;

	/// <summary>The combined effects of all current buffs and equipment bonuses, calculated from <see cref="F:StardewValley.Buffs.BuffManager.AppliedBuffs" />.</summary>
	protected readonly BuffEffects CombinedEffects = new BuffEffects();

	/// <summary>An unsynchronized dictionary of buffs that are currently applied to the player.</summary>
	public readonly IDictionary<string, Buff> AppliedBuffs = new Dictionary<string, Buff>();

	/// <summary>A synchronized list of buff IDs currently applied to the player.</summary>
	public readonly NetStringList AppliedBuffIds = new NetStringList();

	/// <summary>Whether the buffs changed and will be recalculated on the next update.</summary>
	public bool Dirty = true;

	public NetFields NetFields { get; } = new NetFields("BuffManager");

	/// <summary>The combined buff to the player's combat skill level.</summary>
	public int CombatLevel => (int)GetValues().CombatLevel.Value;

	/// <summary>The combined buff to the player's farming skill level.</summary>
	public int FarmingLevel => (int)GetValues().FarmingLevel.Value;

	/// <summary>The combined buff to the player's fishing skill level.</summary>
	public int FishingLevel => (int)GetValues().FishingLevel.Value;

	/// <summary>The combined buff to the player's mining skill level.</summary>
	public int MiningLevel => (int)GetValues().MiningLevel.Value;

	/// <summary>The combined buff to the player's luck skill level.</summary>
	public int LuckLevel => (int)GetValues().LuckLevel.Value;

	/// <summary>The combined buff to the player's foraging skill level.</summary>
	public int ForagingLevel => (int)GetValues().ForagingLevel.Value;

	/// <summary>The combined buff to the player's max stamina.</summary>
	public int MaxStamina => (int)GetValues().MaxStamina.Value;

	/// <summary>The combined buff to the player's magnetic radius.</summary>
	public int MagneticRadius => (int)GetValues().MagneticRadius.Value;

	/// <summary>The combined buff to the player's walk speed.</summary>
	public float Speed => GetValues().Speed.Value;

	/// <summary>The combined buff to the player's defense.</summary>
	public int Defense => (int)GetValues().Defense.Value;

	/// <summary>The combined buff to the player's attack power.</summary>
	public int Attack => (int)GetValues().Attack.Value;

	/// <summary>The combined buff to the player's resistance to negative effects.</summary>
	public int Immunity => (int)GetValues().Immunity.Value;

	/// <summary>The combined multiplier applied to the player's attack power.</summary>
	public float AttackMultiplier => GetValues().AttackMultiplier.Value;

	/// <summary>The combined multiplier applied to monster knockback when hit by the player's weapon.</summary>
	public float KnockbackMultiplier => GetValues().KnockbackMultiplier.Value;

	/// <summary>The combined multiplier applied to the player's weapon swing speed.</summary>
	public float WeaponSpeedMultiplier => GetValues().WeaponSpeedMultiplier.Value;

	/// <summary>The combined multiplier applied to the player's critical hit chance.</summary>
	public float CriticalChanceMultiplier => GetValues().CriticalChanceMultiplier.Value;

	/// <summary>The combined multiplier applied to the player's critical hit damage.</summary>
	public float CriticalPowerMultiplier => GetValues().CriticalPowerMultiplier.Value;

	/// <summary>The combined multiplier applied to the player's weapon accuracy.</summary>
	public float WeaponPrecisionMultiplier => GetValues().WeaponPrecisionMultiplier.Value;

	/// <summary>Construct an instance.</summary>
	public BuffManager()
	{
		NetFields.SetOwner(this).AddField(AppliedBuffIds, "AppliedBuffIds").AddField(CombinedEffects.NetFields, "CombinedEffects.NetFields");
	}

	/// <summary>Get the combined buff values, recalculating them if dirty.</summary>
	/// <remarks>Most code should use the properties like <see cref="P:StardewValley.Buffs.BuffManager.Attack" /> instance.</remarks>
	public virtual BuffEffects GetValues()
	{
		if (!Dirty)
		{
			return CombinedEffects;
		}
		Farmer player = Player;
		CombinedEffects.Clear();
		player.stopGlowing();
		foreach (Buff value in AppliedBuffs.Values)
		{
			CombinedEffects.Add(value.effects);
			if (value.glow != Color.White && value.glow.A > 0)
			{
				player.startGlowing(value.glow, border: false, 0.05f);
			}
		}
		AppliedBuffIds.Clear();
		foreach (string key in AppliedBuffs.Keys)
		{
			AppliedBuffIds.Add(key);
		}
		foreach (Item equippedItem in player.GetEquippedItems())
		{
			equippedItem.AddEquipmentEffects(CombinedEffects);
		}
		if (IsLocallyControlled())
		{
			Game1.buffsDisplay.dirty = true;
		}
		Dirty = false;
		player.stamina = Math.Min(player.stamina, player.MaxStamina);
		return CombinedEffects;
	}

	/// <summary>Set the player managed by the instance.</summary>
	/// <param name="player">The player managed by the instance.</param>
	public void SetOwner(Farmer player)
	{
		Player = player;
	}

	/// <summary>Get whether the player has a buff applied.</summary>
	/// <param name="id">The buff ID.</param>
	public bool IsApplied(string id)
	{
		return AppliedBuffIds.Contains(id);
	}

	/// <summary>Get whether the player has a buff with an ID containing the given string.</summary>
	/// <param name="idSubstring">The substring to match in the buff ID.</param>
	public bool HasBuffWithNameContaining(string idSubstring)
	{
		foreach (string appliedBuffId in AppliedBuffIds)
		{
			if (appliedBuffId.Contains(idSubstring))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Get whether this instance is managed by the local player (e.g. it's their own buffs).</summary>
	public virtual bool IsLocallyControlled()
	{
		return Player.UniqueMultiplayerID == Game1.player.UniqueMultiplayerID;
	}

	/// <summary>Add a buff to the player, or refresh it if it's already applied.</summary>
	/// <param name="buff">The buff to add.</param>
	public void Apply(Buff buff)
	{
		if (buff == null)
		{
			Game1.log.Warn("Ignored invalid null buff.");
		}
		else if (string.IsNullOrWhiteSpace(buff.id))
		{
			Game1.log.Warn("Ignored invalid buff with no ID.");
		}
		else if (buff.millisecondsDuration <= 0 && buff.millisecondsDuration != -2)
		{
			Game1.log.Warn($"Ignored invalid buff '{buff.id}' with {((buff.millisecondsDuration < 0) ? "negative" : "no")} duration.");
		}
		else
		{
			if (!IsLocallyControlled())
			{
				return;
			}
			Remove(buff.id);
			AppliedBuffs[buff.id] = buff;
			AppliedBuffIds.Add(buff.id);
			string[] actionsOnApply = buff.actionsOnApply;
			if (actionsOnApply != null && actionsOnApply.Length != 0)
			{
				string[] actionsOnApply2 = buff.actionsOnApply;
				foreach (string text in actionsOnApply2)
				{
					if (TriggerActionManager.TryRunAction(text, out var error, out var exception))
					{
						Game1.log.Verbose($"Applied action [{text}] from buff '{buff.id}'.");
					}
					else
					{
						Game1.log.Error($"Error applying Applied action [{text}] from buff '{buff.id}': {error}.", exception);
					}
				}
			}
			Game1.buffsDisplay.updatedIDs.Add(buff.id);
			Dirty = true;
			buff.OnAdded();
		}
	}

	/// <summary>Remove a buff from the player.</summary>
	/// <param name="id">The buff ID.</param>
	public void Remove(string id)
	{
		if (IsLocallyControlled())
		{
			if (AppliedBuffs.TryGetValue(id, out var value))
			{
				value.OnRemoved();
			}
			if (AppliedBuffs.Remove(id) | AppliedBuffIds.Remove(id) | Game1.buffsDisplay.updatedIDs.Remove(id))
			{
				Dirty = true;
			}
		}
	}

	/// <summary>Remove all buffs from the player.</summary>
	public void Clear()
	{
		if (IsLocallyControlled())
		{
			for (int num = AppliedBuffIds.Count - 1; num >= 0; num--)
			{
				Remove(AppliedBuffIds[num]);
			}
		}
	}

	/// <summary>Update the buff timers and remove expired buffs.</summary>
	/// <param name="time">The elapsed game time.</param>
	public void Update(GameTime time)
	{
		if (!IsLocallyControlled())
		{
			return;
		}
		for (int num = AppliedBuffIds.Count - 1; num >= 0; num--)
		{
			string text = AppliedBuffIds[num];
			if (!AppliedBuffs.TryGetValue(text, out var value) || value.update(time))
			{
				Remove(text);
			}
		}
		if (Dirty)
		{
			GetValues();
		}
	}
}
