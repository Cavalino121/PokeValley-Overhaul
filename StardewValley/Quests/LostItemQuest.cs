using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.Extensions;

namespace StardewValley.Quests;

public class LostItemQuest : Quest
{
	/// <summary>The internal name for the NPC who gave the quest.</summary>
	[XmlElement("npcName")]
	public readonly NetString npcName = new NetString();

	/// <summary>The internal name for the location where the item can be found.</summary>
	[XmlElement("locationOfItem")]
	public readonly NetString locationOfItem = new NetString();

	/// <summary>The qualified item ID for the item to find.</summary>
	[XmlElement("itemIndex")]
	public readonly NetString ItemId = new NetString();

	/// <summary>The X tile position within the location where the item can be found.</summary>
	[XmlElement("tileX")]
	public readonly NetInt tileX = new NetInt();

	/// <summary>The Y tile position within the location where the item can be found.</summary>
	[XmlElement("tileY")]
	public readonly NetInt tileY = new NetInt();

	/// <summary>Whether the player has found the item yet.</summary>
	[XmlElement("itemFound")]
	public readonly NetBool itemFound = new NetBool();

	/// <summary>The translatable text segments for the objective shown in the quest log.</summary>
	[XmlElement("objective")]
	public readonly NetDescriptionElementRef objective = new NetDescriptionElementRef();

	/// <summary>Construct an instance.</summary>
	public LostItemQuest()
	{
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="npcName">The internal name for the NPC who gave the quest.</param>
	/// <param name="locationOfItem">The internal name for the location where the item can be found.</param>
	/// <param name="itemId">The qualified or unqualified item ID for the item to find.</param>
	/// <param name="tileX">The X tile position within the location where the item can be found.</param>
	/// <param name="tileY">The Y tile position within the location where the item can be found.</param>
	/// <exception cref="T:System.InvalidOperationException">The <paramref name="itemId" /> matches a non-object-type item, which can't be placed in the world.</exception>
	public LostItemQuest(string npcName, string locationOfItem, string itemId, int tileX, int tileY)
	{
		this.npcName.Value = npcName;
		this.locationOfItem.Value = locationOfItem;
		ItemId.Value = ItemRegistry.QualifyItemId(itemId) ?? itemId;
		this.tileX.Value = tileX;
		this.tileY.Value = tileY;
		questType.Value = 9;
		if (!ItemRegistry.GetDataOrErrorItem(ItemId.Value).HasTypeObject())
		{
			throw new InvalidOperationException($"Can't create {GetType().Name} #{id.Value} because the lost item ({ItemId.Value}) isn't an object-type item.");
		}
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(objective, "objective").AddField(npcName, "npcName").AddField(locationOfItem, "locationOfItem")
			.AddField(ItemId, "ItemId")
			.AddField(tileX, "tileX")
			.AddField(tileY, "tileY")
			.AddField(itemFound, "itemFound");
	}

	/// <inheritdoc />
	public override bool OnWarped(GameLocation location, bool probe = false)
	{
		bool result = base.OnWarped(location, probe);
		if (!itemFound.Value && location.name.Equals(locationOfItem.Value))
		{
			Vector2 vector = new Vector2(tileX.Value, tileY.Value);
			location.overlayObjects.Remove(vector);
			Object obj = ItemRegistry.Create<Object>(ItemId.Value);
			obj.TileLocation = vector;
			obj.questItem.Value = true;
			obj.questId.Value = id.Value;
			obj.IsSpawnedObject = true;
			location.overlayObjects.Add(vector, obj);
			return true;
		}
		return result;
	}

	public new void reloadObjective()
	{
		if (objective.Value != null)
		{
			base.currentObjective = objective.Value.loadDescriptionElement();
		}
	}

	/// <inheritdoc />
	public override bool OnItemReceived(Item item, int numberAdded, bool probe = false)
	{
		bool result = base.OnItemReceived(item, numberAdded, probe);
		if (!completed.Value && !itemFound.Value && item != null && item.QualifiedItemId == ItemId.Value)
		{
			if (!probe)
			{
				itemFound.Value = true;
				string sub = npcName.Value;
				NPC characterFromName = Game1.getCharacterFromName(npcName.Value);
				if (characterFromName != null)
				{
					sub = characterFromName.displayName;
				}
				Game1.player.completelyStopAnimatingOrDoingAction();
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Quests:MessageFoundLostItem", item.DisplayName, sub));
				objective.Value = new DescriptionElement("Strings\\Quests:ObjectiveReturnToNPC", characterFromName);
				Game1.playSound("jingle1");
			}
			return true;
		}
		return result;
	}

	public override bool OnNpcSocialized(NPC npc, bool probe = false)
	{
		bool result = base.OnNpcSocialized(npc, probe);
		if (!completed.Value && itemFound.Value && npc.Name == npcName.Value && npc.IsVillager && Game1.player.Items.ContainsId(ItemId.Value))
		{
			if (!probe)
			{
				questComplete();
				string[] rawQuestFields = Quest.GetRawQuestFields(id.Value);
				Dialogue dialogue = new Dialogue(npc, null, ArgUtility.Get(rawQuestFields, 9, "Data\\ExtraDialogue:LostItemQuest_DefaultThankYou", allowBlank: false));
				npc.setNewDialogue(dialogue);
				Game1.drawDialogue(npc);
				Game1.player.changeFriendship(250, npc);
				Game1.player.removeFirstOfThisItemFromInventory(ItemId.Value);
			}
			return true;
		}
		return result;
	}
}
