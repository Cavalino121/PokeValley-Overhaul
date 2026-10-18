using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using StardewValley.Extensions;
using StardewValley.GameData.Bundles;

namespace StardewValley;

public class BundleGenerator
{
	public List<RandomBundleData> randomBundleData;

	public Dictionary<string, string> bundleData;

	public Random random;

	public Dictionary<string, string> Generate(List<RandomBundleData> bundle_data, Random rng)
	{
		random = rng;
		randomBundleData = bundle_data;
		this.bundleData = new Dictionary<string, string>(DataLoader.Bundles(Game1.content));
		foreach (RandomBundleData randomBundleDatum in randomBundleData)
		{
			List<int> list = new List<int>();
			string[] array = ArgUtility.SplitBySpace(randomBundleDatum.Keys);
			Dictionary<int, BundleData> dictionary = new Dictionary<int, BundleData>();
			string[] array2 = array;
			foreach (string s in array2)
			{
				list.Add(int.Parse(s));
			}
			BundleSetData bundleSetData = random.ChooseFrom(randomBundleDatum.BundleSets);
			if (bundleSetData != null)
			{
				foreach (BundleData bundle in bundleSetData.Bundles)
				{
					dictionary[bundle.Index] = bundle;
				}
			}
			List<BundleData> list2 = new List<BundleData>();
			foreach (BundleData bundle2 in randomBundleDatum.Bundles)
			{
				list2.Add(bundle2);
			}
			for (int j = 0; j < list.Count; j++)
			{
				if (dictionary.ContainsKey(j))
				{
					continue;
				}
				List<BundleData> list3 = new List<BundleData>();
				foreach (BundleData item2 in list2)
				{
					if (item2.Index == j)
					{
						list3.Add(item2);
					}
				}
				if (list3.Count > 0)
				{
					BundleData bundleData = random.ChooseFrom(list3);
					list2.Remove(bundleData);
					dictionary[j] = bundleData;
					continue;
				}
				foreach (BundleData item3 in list2)
				{
					if (item3.Index == -1)
					{
						list3.Add(item3);
					}
				}
				if (list3.Count > 0)
				{
					BundleData bundleData2 = random.ChooseFrom(list3);
					list2.Remove(bundleData2);
					dictionary[j] = bundleData2;
				}
			}
			foreach (int key in dictionary.Keys)
			{
				BundleData bundleData3 = dictionary[key];
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(bundleData3.Name);
				stringBuilder.Append("/");
				string text = bundleData3.Reward;
				if (text.Length > 0)
				{
					try
					{
						if (char.IsDigit(text[0]))
						{
							string[] array3 = ArgUtility.SplitBySpace(text);
							int stack_count = int.Parse(array3[0]);
							Item item = Utility.fuzzyItemSearch(string.Join(" ", array3, 1, array3.Length - 1), stack_count);
							if (item != null)
							{
								text = Utility.getStandardDescriptionFromItem(item, item.Stack);
							}
						}
					}
					catch (Exception exception)
					{
						Game1.log.Error("ERROR: Malformed reward string in bundle: " + text, exception);
						text = bundleData3.Reward;
					}
				}
				stringBuilder.Append(text);
				stringBuilder.Append("/");
				int color = 0;
				switch (bundleData3.Color)
				{
				case "Red":
					color = 4;
					break;
				case "Blue":
					color = 5;
					break;
				case "Green":
					color = 0;
					break;
				case "Orange":
					color = 2;
					break;
				case "Purple":
					color = 1;
					break;
				case "Teal":
					color = 6;
					break;
				case "Yellow":
					color = 3;
					break;
				}
				ParseItemList(stringBuilder, bundleData3.Items, bundleData3.Pick, bundleData3.RequiredItems, color);
				stringBuilder.Append("/");
				stringBuilder.Append(bundleData3.Sprite);
				stringBuilder.Append('/');
				stringBuilder.Append(bundleData3.Name);
				this.bundleData[randomBundleDatum.AreaName + "/" + list[key]] = stringBuilder.ToString();
			}
		}
		return this.bundleData;
	}

	public string ParseRandomTags(string data)
	{
		int num;
		do
		{
			num = data.LastIndexOf('[');
			if (num >= 0)
			{
				int num2 = data.IndexOf(']', num);
				if (num2 == -1)
				{
					return data;
				}
				string text = data.Substring(num + 1, num2 - num - 1);
				string value = random.ChooseFrom(text.Split('|'));
				data = data.Remove(num, num2 - num + 1);
				data = data.Insert(num, value);
			}
		}
		while (num >= 0);
		return data;
	}

	public Item ParseItemString(string item_string)
	{
		string[] array = ArgUtility.SplitBySpace(item_string);
		int num = 0;
		int num2 = int.Parse(array[num]);
		num++;
		int quality = 0;
		switch (array[num])
		{
		case "NQ":
			quality = 0;
			num++;
			break;
		case "SQ":
			quality = 1;
			num++;
			break;
		case "GQ":
			quality = 2;
			num++;
			break;
		case "IQ":
			quality = 3;
			num++;
			break;
		}
		string text = string.Join(" ", array, num, array.Length - num);
		if (char.IsDigit(text[0]))
		{
			Item item = ItemRegistry.Create("(O)" + text, num2);
			item.Quality = quality;
			return item;
		}
		Item item2 = null;
		if (text.EndsWithIgnoreCase("category"))
		{
			try
			{
				FieldInfo field = typeof(Object).GetField(text);
				if (field != null)
				{
					item2 = new Object(((int)field.GetValue(null)).ToString(), 1);
				}
			}
			catch (Exception)
			{
			}
		}
		if (item2 == null)
		{
			item2 = Utility.fuzzyItemSearch(text);
			item2.Quality = quality;
		}
		if (item2 == null)
		{
			throw new Exception("Invalid item name '" + text + "' encountered while generating a bundle.");
		}
		item2.Stack = num2;
		return item2;
	}

	public void ParseItemList(StringBuilder builder, string item_list, int pick_count, int required_items, int color)
	{
		item_list = ParseRandomTags(item_list);
		string[] array = item_list.Split(',');
		List<string> list = new List<string>();
		for (int i = 0; i < array.Length; i++)
		{
			Item item = ParseItemString(array[i]);
			list.Add(item.ItemId + " " + item.Stack + " " + item.Quality);
		}
		if (pick_count < 0)
		{
			pick_count = list.Count;
		}
		if (required_items < 0)
		{
			required_items = pick_count;
		}
		while (list.Count > pick_count)
		{
			int index = random.Next(list.Count);
			list.RemoveAt(index);
		}
		for (int j = 0; j < list.Count; j++)
		{
			builder.Append(list[j]);
			if (j < list.Count - 1)
			{
				builder.Append(" ");
			}
		}
		builder.Append("/");
		builder.Append(color);
		builder.Append("/");
		builder.Append(required_items);
	}
}
