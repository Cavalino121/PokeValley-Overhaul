using System.Collections.Generic;
using Netcode;
using StardewValley.GameData.Shops;
using StardewValley.Network;

namespace StardewValley.Util;

public class SynchronizedShopStock : INetObject<NetFields>
{
	private readonly NetStringDictionary<int, NetInt> stockDictionary = new NetStringDictionary<int, NetInt>();

	protected static HashSet<string> _usedKeys = new HashSet<string>();

	protected static List<ISalable> _stockSalables = new List<ISalable>();

	public NetFields NetFields { get; } = new NetFields("SynchronizedShopStock");

	public SynchronizedShopStock()
	{
		initNetFields();
	}

	private void initNetFields()
	{
		NetFields.SetOwner(this).AddField(stockDictionary, "stockDictionary");
	}

	public virtual void Clear()
	{
		stockDictionary.Clear();
	}

	public void OnItemPurchased(string shop_id, ISalable item, Dictionary<ISalable, ItemStockInformation> stock, int amount)
	{
		NetStringDictionary<int, NetInt> netStringDictionary = stockDictionary;
		if (stock.TryGetValue(item, out var value) && value.Stock != int.MaxValue)
		{
			string qualifiedSyncedKey = GetQualifiedSyncedKey(shop_id, value);
			value.Stock -= amount;
			netStringDictionary[qualifiedSyncedKey] = value.Stock;
		}
	}

	public string GetQualifiedSyncedKey(string shop_id, ItemStockInformation item)
	{
		if (item.LimitedStockMode == LimitedStockMode.Global)
		{
			return shop_id + "/Global/" + item.SyncedKey;
		}
		return $"{shop_id}/{Game1.player.UniqueMultiplayerID}/{item.SyncedKey}";
	}

	public void UpdateLocalStockWithSyncedQuanitities(string shop_id, Dictionary<ISalable, ItemStockInformation> local_stock)
	{
		_usedKeys.Clear();
		_stockSalables.Clear();
		List<ISalable> list = new List<ISalable>();
		_stockSalables.AddRange(local_stock.Keys);
		foreach (ISalable stockSalable in _stockSalables)
		{
			ItemStockInformation itemStockInformation = local_stock[stockSalable];
			if (itemStockInformation.Stock == int.MaxValue || itemStockInformation.LimitedStockMode == LimitedStockMode.None)
			{
				continue;
			}
			if (itemStockInformation.SyncedKey == null)
			{
				string name = stockSalable.Name;
				string text = name;
				int num = 1;
				while (_usedKeys.Contains(text))
				{
					text = name + num;
					num++;
				}
				_usedKeys.Add(text);
				itemStockInformation.SyncedKey = text;
				local_stock[stockSalable] = itemStockInformation;
			}
			string qualifiedSyncedKey = GetQualifiedSyncedKey(shop_id, itemStockInformation);
			if (stockDictionary.TryGetValue(qualifiedSyncedKey, out var value))
			{
				itemStockInformation.Stock = value;
				local_stock[stockSalable] = itemStockInformation;
				if (value <= 0)
				{
					list.Add(stockSalable);
				}
			}
		}
		_usedKeys.Clear();
		_stockSalables.Clear();
		foreach (Item item in list)
		{
			local_stock.Remove(item);
		}
	}
}
