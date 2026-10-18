using Microsoft.Xna.Framework;

namespace StardewValley.Menus;

public class PendingProposalDialog : ConfirmationDialog
{
	public PendingProposalDialog()
		: base(Game1.content.LoadString("Strings\\UI:PendingProposal"), null)
	{
		okButton.visible = false;
		onCancel = cancelProposal;
		setCancelable(cancelable: true);
	}

	public void cancelProposal(Farmer who)
	{
		Proposal outgoingProposal = Game1.player.team.GetOutgoingProposal();
		if (outgoingProposal?.receiver.Value != null && outgoingProposal.receiver.Value.isActive())
		{
			outgoingProposal.canceled.Value = true;
			message = Game1.content.LoadString("Strings\\UI:PendingProposal_Canceling");
			setCancelable(cancelable: false);
		}
	}

	public void setCancelable(bool cancelable)
	{
		cancelButton.visible = cancelable;
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	public override bool readyToClose()
	{
		return false;
	}

	private bool consumesItem(ProposalType pt)
	{
		if (pt != ProposalType.Gift)
		{
			return pt == ProposalType.Marriage;
		}
		return true;
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		Proposal outgoingProposal = Game1.player.team.GetOutgoingProposal();
		if (outgoingProposal?.receiver.Value == null || !outgoingProposal.receiver.Value.isActive())
		{
			Game1.player.team.RemoveOutgoingProposal();
			closeDialog(Game1.player);
		}
		else if (outgoingProposal.cancelConfirmed.Value && outgoingProposal.response.Value != ProposalResponse.Accepted)
		{
			Game1.player.team.RemoveOutgoingProposal();
			closeDialog(Game1.player);
		}
		else
		{
			if (outgoingProposal.response.Value == ProposalResponse.None)
			{
				return;
			}
			if (outgoingProposal.response.Value == ProposalResponse.Accepted)
			{
				if (consumesItem(outgoingProposal.proposalType.Value))
				{
					Game1.player.reduceActiveItemByOne();
				}
				if (outgoingProposal.proposalType.Value == ProposalType.Dance)
				{
					Game1.player.dancePartner.Value = outgoingProposal.receiver.Value;
				}
				outgoingProposal.receiver.Value.doEmote(20);
			}
			Game1.player.team.RemoveOutgoingProposal();
			closeDialog(Game1.player);
			if (outgoingProposal.responseMessageKey.Value != null)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString(outgoingProposal.responseMessageKey.Value, outgoingProposal.receiver.Value.Name));
			}
		}
	}
}
