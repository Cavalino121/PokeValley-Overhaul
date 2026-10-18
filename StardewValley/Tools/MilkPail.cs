using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;

namespace StardewValley.Tools;

public class MilkPail : Tool
{
	[XmlIgnore]
	private readonly NetEvent0 finishEvent = new NetEvent0();

	/// <summary>The farm animal the milk pail is being used on, if any.</summary>
	[XmlIgnore]
	public FarmAnimal animal;

	public MilkPail()
		: base("Milk Pail", -1, 6, 6, stackable: false)
	{
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new MilkPail();
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(finishEvent, "finishEvent");
		finishEvent.onEvent += doFinish;
	}

	public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
	{
		x = (int)who.GetToolLocation().X;
		y = (int)who.GetToolLocation().Y;
		animal = Utility.GetBestHarvestableFarmAnimal(toolRect: new Rectangle(x - 32, y - 32, 64, 64), animals: location.animals.Values, tool: this);
		if (animal?.currentProduce.Value != null && animal.isAdult() && animal.CanGetProduceWithTool(this) && who.couldInventoryAcceptThisItem(animal.currentProduce.Value, 1))
		{
			animal.pauseTimer = 1500;
			animal.doEmote(20);
			if (PlayUseSounds)
			{
				who.playNearbySoundLocal("Milking");
			}
		}
		else if (animal?.currentProduce.Value != null && animal.isAdult())
		{
			if (who == Game1.player)
			{
				if (!animal.CanGetProduceWithTool(this))
				{
					string text = animal.GetAnimalData()?.HarvestTool;
					if (text != null)
					{
						Game1.showRedMessage(Game1.content.LoadString("Strings\\Tools:MilkPail_Name", text));
					}
				}
				else if (!who.couldInventoryAcceptThisItem(animal.currentProduce.Value, (!animal.hasEatenAnimalCracker.Value) ? 1 : 2))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
				}
			}
		}
		else if (who == Game1.player)
		{
			if (PlayUseSounds)
			{
				DelayedAction.playSoundAfterDelay("fishingRodBend", 300);
				DelayedAction.playSoundAfterDelay("fishingRodBend", 1200);
			}
			string text2 = null;
			if (animal != null)
			{
				text2 = (animal.CanGetProduceWithTool(this) ? (animal.isBaby() ? Game1.content.LoadString("Strings\\StringsFromCSFiles:MilkPail.cs.14176", animal.displayName) : Game1.content.LoadString("Strings\\StringsFromCSFiles:MilkPail.cs.14177", animal.displayName)) : Game1.content.LoadString("Strings\\StringsFromCSFiles:MilkPail.cs.14175", animal.displayName));
			}
			if (text2 != null)
			{
				DelayedAction.showDialogueAfterDelay(text2, 1000);
			}
		}
		who.Halt();
		int currentFrame = who.FarmerSprite.CurrentFrame;
		who.FarmerSprite.animateOnce(287 + who.FacingDirection, 50f, 4);
		who.FarmerSprite.oldFrame = currentFrame;
		who.UsingTool = true;
		who.CanMove = false;
		return true;
	}

	public override void tickUpdate(GameTime time, Farmer who)
	{
		lastUser = who;
		base.tickUpdate(time, who);
		finishEvent.Poll();
	}

	public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
	{
		base.DoFunction(location, x, y, power, who);
		who.Stamina -= 4f;
		base.CurrentParentTileIndex = 6;
		base.IndexOfMenuItemView = 6;
		if (animal?.currentProduce.Value != null && animal.isAdult() && animal.CanGetProduceWithTool(this))
		{
			Object obj = ItemRegistry.Create<Object>("(O)" + animal.currentProduce.Value);
			obj.CanBeSetDown = false;
			obj.Quality = animal.produceQuality.Value;
			if (animal.hasEatenAnimalCracker.Value)
			{
				obj.Stack = 2;
			}
			if (who.addItemToInventoryBool(obj))
			{
				animal.HandleStatsOnProduceCollected(obj, (uint)obj.Stack);
				if (PlayUseSounds)
				{
					Game1.playSound("coin");
				}
				animal.currentProduce.Value = null;
				animal.friendshipTowardFarmer.Value = Math.Min(1000, animal.friendshipTowardFarmer.Value + 5);
				animal.ReloadTextureIfNeeded();
				who.gainExperience(0, 5);
			}
		}
		finish();
	}

	private void finish()
	{
		finishEvent.Fire();
	}

	private void doFinish()
	{
		animal = null;
		lastUser.CanMove = true;
		lastUser.completelyStopAnimatingOrDoingAction();
		lastUser.UsingTool = false;
		lastUser.canReleaseTool = true;
	}
}
