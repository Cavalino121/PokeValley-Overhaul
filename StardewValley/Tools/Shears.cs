using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;

namespace StardewValley.Tools;

public class Shears : Tool
{
	[XmlIgnore]
	private readonly NetEvent0 finishEvent = new NetEvent0();

	/// <summary>The farm animal the shears are being used on, if any.</summary>
	[XmlIgnore]
	public FarmAnimal animal;

	public Shears()
		: base("Shears", -1, 7, 7, stackable: false)
	{
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Shears();
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
		who.Halt();
		int currentFrame = who.FarmerSprite.CurrentFrame;
		who.FarmerSprite.animateOnce(283 + who.FacingDirection, 50f, 4);
		who.FarmerSprite.oldFrame = currentFrame;
		who.UsingTool = true;
		who.CanMove = false;
		return true;
	}

	public static void playSnip(Farmer who)
	{
		who.playNearbySoundAll("scissors");
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
		if (PlayUseSounds)
		{
			playSnip(who);
		}
		base.CurrentParentTileIndex = 7;
		base.IndexOfMenuItemView = 7;
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
				animal.currentProduce.Value = null;
				if (PlayUseSounds)
				{
					Game1.playSound("coin");
				}
				animal.friendshipTowardFarmer.Value = Math.Min(1000, animal.friendshipTowardFarmer.Value + 5);
				animal.ReloadTextureIfNeeded();
				who.gainExperience(0, 5);
			}
		}
		else
		{
			string text = null;
			if (animal != null)
			{
				text = (animal.CanGetProduceWithTool(this) ? (animal.isBaby() ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Shears.cs.14246", animal.displayName) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Shears.cs.14247", animal.displayName)) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Shears.cs.14245", animal.displayName));
			}
			if (text != null)
			{
				Game1.drawObjectDialogue(text);
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
