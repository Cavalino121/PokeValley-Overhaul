using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData.HomeRenovations;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;

namespace StardewValley;

public class HouseRenovation : ISalable, IHaveItemTypeId
{
	public enum AnimationType
	{
		Build,
		Destroy
	}

	protected string _displayName;

	protected string _name;

	protected string _description;

	public AnimationType animationType;

	public List<List<Rectangle>> renovationBounds = new List<List<Rectangle>>();

	public string placementText = "";

	public GameLocation location;

	public bool requireClearance = true;

	public Action<HouseRenovation, int> onRenovation;

	public Func<HouseRenovation, int, bool> validate;

	/// <inheritdoc cref="F:StardewValley.GameData.HomeRenovations.HomeRenovation.Price" />
	public int Price;

	/// <inheritdoc cref="F:StardewValley.GameData.HomeRenovations.HomeRenovation.RoomId" />
	public string RoomId;

	/// <inheritdoc />
	public string TypeDefinitionId => "(Salable)";

	/// <inheritdoc />
	public string QualifiedItemId => TypeDefinitionId + "HouseRenovation";

	/// <inheritdoc />
	public string DisplayName => _displayName;

	/// <inheritdoc />
	public string Name => _name;

	public bool IsRecipe
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public int Stack
	{
		get
		{
			return 1;
		}
		set
		{
		}
	}

	public int Quality
	{
		get
		{
			return 0;
		}
		set
		{
		}
	}

	public bool ShouldDrawIcon()
	{
		return false;
	}

	public void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
	}

	public string getDescription()
	{
		return _description;
	}

	public int maximumStackSize()
	{
		return 1;
	}

	public int addToStack(Item stack)
	{
		return 0;
	}

	/// <inheritdoc />
	public int sellToStorePrice(long specificPlayerID = -1L)
	{
		return -1;
	}

	/// <inheritdoc />
	public int salePrice(bool ignoreProfitMargins = false)
	{
		if (Price <= 0)
		{
			return 0;
		}
		return Price;
	}

	/// <inheritdoc />
	public bool appliesProfitMargins()
	{
		return false;
	}

	/// <inheritdoc />
	public bool actionWhenPurchased(string shopId)
	{
		return false;
	}

	public bool canStackWith(ISalable other)
	{
		return false;
	}

	public bool CanBuyItem(Farmer farmer)
	{
		return true;
	}

	public bool IsInfiniteStock()
	{
		return true;
	}

	public ISalable GetSalableInstance()
	{
		return this;
	}

	/// <inheritdoc />
	public void FixStackSize()
	{
	}

	/// <inheritdoc />
	public void FixQuality()
	{
	}

	/// <inheritdoc />
	public string GetItemTypeId()
	{
		return TypeDefinitionId;
	}

	public static void ShowRenovationMenu()
	{
		Game1.activeClickableMenu = new ShopMenu("HouseRenovations", GetAvailableRenovations(), 0, null, OnPurchaseRenovation)
		{
			purchaseSound = null
		};
	}

	public static List<ISalable> GetAvailableRenovations()
	{
		FarmHouse farmhouse = Game1.RequireLocation<FarmHouse>(Game1.player.homeLocation.Value);
		List<ISalable> list = new List<ISalable>();
		Dictionary<string, HomeRenovation> dictionary = DataLoader.HomeRenovations(Game1.content);
		foreach (string key in dictionary.Keys)
		{
			HomeRenovation homeRenovation = dictionary[key];
			bool flag = true;
			foreach (RenovationValue requirement in homeRenovation.Requirements)
			{
				if (requirement.Type == "Value")
				{
					string text = requirement.Value;
					bool flag2 = true;
					if (text.Length > 0 && text[0] == '!')
					{
						text = text.Substring(1);
						flag2 = false;
					}
					int num = int.Parse(text);
					try
					{
						NetInt netInt = (NetInt)farmhouse.GetType().GetField(requirement.Key).GetValue(farmhouse);
						if ((object)netInt == null)
						{
							flag = false;
							break;
						}
						if (netInt.Value == num != flag2)
						{
							flag = false;
							break;
						}
					}
					catch (Exception)
					{
						flag = false;
						break;
					}
				}
				else if (requirement.Type == "Mail" && Game1.player.hasOrWillReceiveMail(requirement.Key) != (requirement.Value == "1"))
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			HouseRenovation houseRenovation = new HouseRenovation
			{
				location = farmhouse,
				_name = key
			};
			string[] array = Game1.content.LoadString(homeRenovation.TextStrings).Split('/');
			try
			{
				houseRenovation._displayName = array[0];
				houseRenovation._description = array[1];
				houseRenovation.placementText = array[2];
			}
			catch (Exception)
			{
				houseRenovation._displayName = "?";
				houseRenovation._description = "?";
				houseRenovation.placementText = "?";
			}
			if (homeRenovation.CheckForObstructions)
			{
				houseRenovation.validate = (Func<HouseRenovation, int, bool>)Delegate.Combine(houseRenovation.validate, new Func<HouseRenovation, int, bool>(EnsureNoObstructions));
			}
			if (homeRenovation.AnimationType == "destroy")
			{
				houseRenovation.animationType = AnimationType.Destroy;
			}
			else
			{
				houseRenovation.animationType = AnimationType.Build;
			}
			houseRenovation.Price = homeRenovation.Price;
			houseRenovation.RoomId = ((!string.IsNullOrEmpty(homeRenovation.RoomId)) ? homeRenovation.RoomId : key);
			if (!string.IsNullOrEmpty(homeRenovation.SpecialRect))
			{
				if (homeRenovation.SpecialRect == "crib")
				{
					Rectangle? cribBounds = farmhouse.GetCribBounds();
					if (!farmhouse.CanModifyCrib() || !cribBounds.HasValue)
					{
						continue;
					}
					houseRenovation.AddRenovationBound(cribBounds.Value);
				}
			}
			else
			{
				foreach (RectGroup rectGroup in homeRenovation.RectGroups)
				{
					List<Rectangle> list2 = new List<Rectangle>();
					foreach (Rect rect in rectGroup.Rects)
					{
						list2.Add(new Rectangle
						{
							X = rect.X,
							Y = rect.Y,
							Width = rect.Width,
							Height = rect.Height
						});
					}
					houseRenovation.AddRenovationBound(list2);
				}
			}
			foreach (RenovationValue renovateAction in homeRenovation.RenovateActions)
			{
				RenovationValue action_data = renovateAction;
				if (action_data.Type == "Value")
				{
					try
					{
						NetInt field = (NetInt)farmhouse.GetType().GetField(action_data.Key).GetValue(farmhouse);
						if (field == null)
						{
							flag = false;
							break;
						}
						houseRenovation.onRenovation = (Action<HouseRenovation, int>)Delegate.Combine(houseRenovation.onRenovation, new Action<HouseRenovation, int>(ActionOnRenovation));
						void ActionOnRenovation(HouseRenovation selectedRenovation, int index)
						{
							if (action_data.Value == "selected")
							{
								field.Value = index;
							}
							else
							{
								int value = int.Parse(action_data.Value);
								field.Value = value;
							}
						}
					}
					catch (Exception)
					{
						flag = false;
						break;
					}
				}
				else if (action_data.Type == "Mail")
				{
					houseRenovation.onRenovation = (Action<HouseRenovation, int>)Delegate.Combine(houseRenovation.onRenovation, new Action<HouseRenovation, int>(MailOnRenovation));
				}
				void MailOnRenovation(HouseRenovation selectedRenovation, int index)
				{
					if (action_data.Value == "0")
					{
						Game1.player.mailReceived.Remove(action_data.Key);
					}
					else
					{
						Game1.player.mailReceived.Add(action_data.Key);
					}
				}
			}
			if (flag)
			{
				houseRenovation.onRenovation = (Action<HouseRenovation, int>)Delegate.Combine(houseRenovation.onRenovation, (Action<HouseRenovation, int>)delegate
				{
					farmhouse.UpdateForRenovation();
				});
				list.Add(houseRenovation);
			}
		}
		return list;
	}

	public static bool EnsureNoObstructions(HouseRenovation renovation, int selected_index)
	{
		if (renovation.location != null)
		{
			foreach (Rectangle item in renovation.renovationBounds[selected_index])
			{
				foreach (Vector2 vector in item.GetVectors())
				{
					if (renovation.location.isTileOccupiedByFarmer(vector) != null)
					{
						Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:RenovationBlocked"));
						return false;
					}
					if (renovation.location.IsTileOccupiedBy(vector))
					{
						Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:RenovationBlocked"));
						return false;
					}
				}
				Rectangle value = new Rectangle(item.X * 64, item.Y * 64, item.Width * 64, item.Height * 64);
				if (!(renovation.location is DecoratableLocation decoratableLocation))
				{
					continue;
				}
				foreach (Furniture item2 in decoratableLocation.furniture)
				{
					if (item2.GetBoundingBox().Intersects(value))
					{
						Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:RenovationBlocked"));
						return false;
					}
				}
			}
			return true;
		}
		return false;
	}

	public static void BuildCrib(HouseRenovation renovation, int selected_index)
	{
		if (renovation.location is FarmHouse farmHouse)
		{
			farmHouse.cribStyle.Value = 1;
		}
	}

	public static void RemoveCrib(HouseRenovation renovation, int selected_index)
	{
		if (renovation.location is FarmHouse farmHouse)
		{
			farmHouse.cribStyle.Value = 0;
		}
	}

	public static void OpenBedroom(HouseRenovation renovation, int selected_index)
	{
		if (renovation.location is FarmHouse farmHouse)
		{
			Game1.player.mailReceived.Add("renovation_bedroom_open");
			farmHouse.UpdateForRenovation();
		}
	}

	public static void CloseBedroom(HouseRenovation renovation, int selected_index)
	{
		if (renovation.location is FarmHouse farmHouse)
		{
			Game1.player.mailReceived.Remove("renovation_bedroom_open");
			farmHouse.UpdateForRenovation();
		}
	}

	public static void OpenSouthernRoom(HouseRenovation renovation, int selected_index)
	{
		if (renovation.location is FarmHouse farmHouse)
		{
			Game1.player.mailReceived.Add("renovation_southern_open");
			farmHouse.UpdateForRenovation();
		}
	}

	public static void CloseSouthernRoom(HouseRenovation renovation, int selected_index)
	{
		if (renovation.location is FarmHouse farmHouse)
		{
			Game1.player.mailReceived.Remove("renovation_southern_open");
			farmHouse.UpdateForRenovation();
		}
	}

	public static void OpenCornernRoom(HouseRenovation renovation, int selected_index)
	{
		if (renovation.location is FarmHouse farmHouse)
		{
			Game1.player.mailReceived.Add("renovation_corner_open");
			farmHouse.UpdateForRenovation();
		}
	}

	public static void CloseCornerRoom(HouseRenovation renovation, int selected_index)
	{
		if (renovation.location is FarmHouse farmHouse)
		{
			Game1.player.mailReceived.Remove("renovation_corner_open");
			farmHouse.UpdateForRenovation();
		}
	}

	/// <summary>Handle a renovation being purchased.</summary>
	/// <inheritdoc cref="T:StardewValley.Menus.ShopMenu.OnPurchaseDelegate" />
	public static bool OnPurchaseRenovation(ISalable salable, Farmer who, int countTaken, ItemStockInformation stock)
	{
		if (salable is HouseRenovation renovation)
		{
			who._money += salable.salePrice();
			Game1.activeClickableMenu = new RenovateMenu(renovation);
			return true;
		}
		return false;
	}

	public virtual void AddRenovationBound(Rectangle bound)
	{
		List<Rectangle> item = new List<Rectangle> { bound };
		renovationBounds.Add(item);
	}

	public virtual void AddRenovationBound(List<Rectangle> bounds)
	{
		renovationBounds.Add(bounds);
	}
}
