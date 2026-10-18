using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using StardewValley.GameData.Movies;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;

namespace StardewValley.Events;

/// <summary>Generates the event that plays when watching a movie at the <see cref="T:StardewValley.Locations.MovieTheater" />.</summary>
public class MovieTheaterScreeningEvent
{
	public int currentResponse;

	public List<List<Character>> playerAndGuestAudienceGroups;

	public Dictionary<int, Character> _responseOrder = new Dictionary<int, Character>();

	protected Dictionary<Character, Character> _whiteListDependencyLookup;

	protected Dictionary<Character, string> _characterResponses;

	public MovieData movieData;

	protected List<Farmer> _farmers;

	protected Dictionary<Character, MovieConcession> _concessionsData;

	public Event getMovieEvent(string movieId, List<List<Character>> player_and_guest_audience_groups, List<List<Character>> npcOnlyAudienceGroups, Dictionary<Character, MovieConcession> concessions_data = null)
	{
		_concessionsData = concessions_data;
		_responseOrder = new Dictionary<int, Character>();
		_whiteListDependencyLookup = new Dictionary<Character, Character>();
		_characterResponses = new Dictionary<Character, string>();
		movieData = MovieTheater.GetMovieDataById()[movieId];
		playerAndGuestAudienceGroups = player_and_guest_audience_groups;
		currentResponse = 0;
		StringBuilder stringBuilder = new StringBuilder();
		Random theaterRandom = Utility.CreateDaySaveRandom();
		stringBuilder.Append("movieScreenAmbience/-2000 -2000/");
		string text = "farmer" + Utility.getFarmerNumberFromFarmer(Game1.player);
		string text2 = "";
		bool flag = false;
		foreach (List<Character> playerAndGuestAudienceGroup in playerAndGuestAudienceGroups)
		{
			if (!playerAndGuestAudienceGroup.Contains(Game1.player))
			{
				continue;
			}
			for (int i = 0; i < playerAndGuestAudienceGroup.Count; i++)
			{
				if (!(playerAndGuestAudienceGroup[i] is Farmer))
				{
					text2 = playerAndGuestAudienceGroup[i].name.Value;
					flag = true;
					break;
				}
			}
		}
		_farmers = new List<Farmer>();
		foreach (List<Character> playerAndGuestAudienceGroup2 in playerAndGuestAudienceGroups)
		{
			foreach (Character item2 in playerAndGuestAudienceGroup2)
			{
				if (item2 is Farmer item && !_farmers.Contains(item))
				{
					_farmers.Add(item);
				}
			}
		}
		List<Character> list = playerAndGuestAudienceGroups.SelectMany((List<Character> x) => x).ToList();
		if (list.Count <= 12)
		{
			list.AddRange(npcOnlyAudienceGroups.SelectMany((List<Character> x) => x).ToList());
		}
		bool flag2 = true;
		foreach (Character item3 in list)
		{
			if (item3 != null)
			{
				if (!flag2)
				{
					stringBuilder.Append(' ');
				}
				if (item3 is Farmer who)
				{
					stringBuilder.Append("farmer").Append(Utility.getFarmerNumberFromFarmer(who));
				}
				else
				{
					stringBuilder.Append(item3.name.Value);
				}
				stringBuilder.Append(" -1000 -1000 0");
				flag2 = false;
			}
		}
		stringBuilder.Append("/changeToTemporaryMap MovieTheaterScreen false/specificTemporarySprite movieTheater_setup/ambientLight 0 0 0/");
		string[] array = new string[8];
		string[] array2 = new string[6];
		string[] array3 = new string[4];
		playerAndGuestAudienceGroups = playerAndGuestAudienceGroups.OrderBy((List<Character> x) => theaterRandom.Next()).ToList();
		int num = theaterRandom.Next(8 - Math.Min(playerAndGuestAudienceGroups.SelectMany((List<Character> x) => x).Count(), 8) + 1);
		int num2 = 0;
		if (playerAndGuestAudienceGroups.Count > 0)
		{
			for (int num3 = 0; num3 < 8; num3++)
			{
				int num4 = (num3 + num) % 8;
				if (playerAndGuestAudienceGroups[num2].Count == 2 && (num4 == 3 || num4 == 7))
				{
					num3++;
					num4++;
					num4 %= 8;
				}
				for (int num5 = 0; num5 < playerAndGuestAudienceGroups[num2].Count && num4 + num5 < array.Length; num5++)
				{
					array[num4 + num5] = ((playerAndGuestAudienceGroups[num2][num5] is Farmer) ? ("farmer" + Utility.getFarmerNumberFromFarmer(playerAndGuestAudienceGroups[num2][num5] as Farmer)) : playerAndGuestAudienceGroups[num2][num5].name.Value);
					if (num5 > 0)
					{
						num3++;
					}
				}
				num2++;
				if (num2 >= playerAndGuestAudienceGroups.Count)
				{
					break;
				}
			}
		}
		else
		{
			Game1.log.Warn("The movie audience somehow has no players. This is likely a bug.");
		}
		bool flag3 = false;
		if (num2 < playerAndGuestAudienceGroups.Count)
		{
			num = 0;
			for (int num6 = 0; num6 < 4; num6++)
			{
				int num7 = (num6 + num) % 4;
				for (int num8 = 0; num8 < playerAndGuestAudienceGroups[num2].Count && num7 + num8 < array3.Length; num8++)
				{
					array3[num7 + num8] = ((playerAndGuestAudienceGroups[num2][num8] is Farmer) ? ("farmer" + Utility.getFarmerNumberFromFarmer(playerAndGuestAudienceGroups[num2][num8] as Farmer)) : playerAndGuestAudienceGroups[num2][num8].name.Value);
					if (num8 > 0)
					{
						num6++;
					}
				}
				num2++;
				if (num2 >= playerAndGuestAudienceGroups.Count)
				{
					break;
				}
			}
			if (num2 < playerAndGuestAudienceGroups.Count)
			{
				flag3 = true;
				num = 0;
				for (int num9 = 0; num9 < 6; num9++)
				{
					int num10 = (num9 + num) % 6;
					if (playerAndGuestAudienceGroups[num2].Count == 2 && num10 == 2)
					{
						num9++;
						num10++;
						num10 %= 8;
					}
					for (int num11 = 0; num11 < playerAndGuestAudienceGroups[num2].Count && num10 + num11 < array2.Length; num11++)
					{
						array2[num10 + num11] = ((playerAndGuestAudienceGroups[num2][num11] is Farmer) ? ("farmer" + Utility.getFarmerNumberFromFarmer(playerAndGuestAudienceGroups[num2][num11] as Farmer)) : playerAndGuestAudienceGroups[num2][num11].name.Value);
						if (num11 > 0)
						{
							num9++;
						}
					}
					num2++;
					if (num2 >= playerAndGuestAudienceGroups.Count)
					{
						break;
					}
				}
			}
		}
		if (!flag3)
		{
			for (int num12 = 0; num12 < npcOnlyAudienceGroups.Count; num12++)
			{
				int num13 = theaterRandom.Next(3 - npcOnlyAudienceGroups[num12].Count + 1) + num12 * 3;
				for (int num14 = 0; num14 < npcOnlyAudienceGroups[num12].Count; num14++)
				{
					array2[num13 + num14] = npcOnlyAudienceGroups[num12][num14].name.Value;
				}
			}
		}
		int num15 = 0;
		int num16 = 0;
		for (int num17 = 0; num17 < array.Length; num17++)
		{
			if (string.IsNullOrEmpty(array[num17]) || !(array[num17] != text) || !(array[num17] != text2))
			{
				continue;
			}
			num15++;
			if (num15 < 2)
			{
				continue;
			}
			num16++;
			Point backRowSeatTileFromIndex = getBackRowSeatTileFromIndex(num17);
			stringBuilder.Append("warp ").Append(array[num17]).Append(' ')
				.Append(backRowSeatTileFromIndex.X)
				.Append(' ')
				.Append(backRowSeatTileFromIndex.Y)
				.Append("/positionOffset ")
				.Append(array[num17])
				.Append(" 0 -10/");
			if (num16 == 2)
			{
				num16 = 0;
				if (theaterRandom.NextBool() && array[num17] != text2 && array[num17 - 1] != text2 && array[num17 - 1] != null)
				{
					stringBuilder.Append("faceDirection ").Append(array[num17]).Append(" 3 true/");
					stringBuilder.Append("faceDirection ").Append(array[num17 - 1]).Append(" 1 true/");
				}
			}
		}
		num15 = 0;
		num16 = 0;
		for (int num18 = 0; num18 < array2.Length; num18++)
		{
			if (string.IsNullOrEmpty(array2[num18]) || !(array2[num18] != text) || !(array2[num18] != text2))
			{
				continue;
			}
			num15++;
			if (num15 < 2)
			{
				continue;
			}
			num16++;
			Point midRowSeatTileFromIndex = getMidRowSeatTileFromIndex(num18);
			stringBuilder.Append("warp ").Append(array2[num18]).Append(' ')
				.Append(midRowSeatTileFromIndex.X)
				.Append(' ')
				.Append(midRowSeatTileFromIndex.Y)
				.Append("/positionOffset ")
				.Append(array2[num18])
				.Append(" 0 -10/");
			if (num16 == 2)
			{
				num16 = 0;
				if (num18 != 3 && theaterRandom.NextBool() && array2[num18 - 1] != null)
				{
					stringBuilder.Append("faceDirection ").Append(array2[num18]).Append(" 3 true/");
					stringBuilder.Append("faceDirection ").Append(array2[num18 - 1]).Append(" 1 true/");
				}
			}
		}
		num15 = 0;
		num16 = 0;
		for (int num19 = 0; num19 < array3.Length; num19++)
		{
			if (string.IsNullOrEmpty(array3[num19]) || !(array3[num19] != text) || !(array3[num19] != text2))
			{
				continue;
			}
			num15++;
			if (num15 < 2)
			{
				continue;
			}
			num16++;
			Point frontRowSeatTileFromIndex = getFrontRowSeatTileFromIndex(num19);
			stringBuilder.Append("warp ").Append(array3[num19]).Append(' ')
				.Append(frontRowSeatTileFromIndex.X)
				.Append(' ')
				.Append(frontRowSeatTileFromIndex.Y)
				.Append("/positionOffset ")
				.Append(array3[num19])
				.Append(" 0 -10/");
			if (num16 == 2)
			{
				num16 = 0;
				if (theaterRandom.NextBool() && array3[num19 - 1] != null)
				{
					stringBuilder.Append("faceDirection ").Append(array3[num19]).Append(" 3 true/");
					stringBuilder.Append("faceDirection ").Append(array3[num19 - 1]).Append(" 1 true/");
				}
			}
		}
		Point point = new Point(1, 15);
		num15 = 0;
		for (int num20 = 0; num20 < array.Length; num20++)
		{
			if (!string.IsNullOrEmpty(array[num20]) && array[num20] != text && array[num20] != text2)
			{
				Point backRowSeatTileFromIndex2 = getBackRowSeatTileFromIndex(num20);
				if (num15 == 1)
				{
					stringBuilder.Append("warp ").Append(array[num20]).Append(' ')
						.Append(backRowSeatTileFromIndex2.X - 1)
						.Append(" 10")
						.Append("/advancedMove ")
						.Append(array[num20])
						.Append(" false 1 ")
						.Append(200)
						.Append(" 1 0 4 1000/")
						.Append("positionOffset ")
						.Append(array[num20])
						.Append(" 0 -10/");
				}
				else
				{
					stringBuilder.Append("warp ").Append(array[num20]).Append(" 1 12")
						.Append("/advancedMove ")
						.Append(array[num20])
						.Append(" false 1 200 ")
						.Append("0 -2 ")
						.Append(backRowSeatTileFromIndex2.X - 1)
						.Append(" 0 4 1000/")
						.Append("positionOffset ")
						.Append(array[num20])
						.Append(" 0 -10/");
				}
				num15++;
			}
			if (num15 >= 2)
			{
				break;
			}
		}
		num15 = 0;
		for (int num21 = 0; num21 < array2.Length; num21++)
		{
			if (!string.IsNullOrEmpty(array2[num21]) && array2[num21] != text && array2[num21] != text2)
			{
				Point midRowSeatTileFromIndex2 = getMidRowSeatTileFromIndex(num21);
				if (num15 == 1)
				{
					stringBuilder.Append("warp ").Append(array2[num21]).Append(' ')
						.Append(midRowSeatTileFromIndex2.X - 1)
						.Append(" 8")
						.Append("/advancedMove ")
						.Append(array2[num21])
						.Append(" false 1 ")
						.Append(400)
						.Append(" 1 0 4 1000/");
				}
				else
				{
					stringBuilder.Append("warp ").Append(array2[num21]).Append(" 2 9")
						.Append("/advancedMove ")
						.Append(array2[num21])
						.Append(" false 1 300 ")
						.Append("0 -1 ")
						.Append(midRowSeatTileFromIndex2.X - 2)
						.Append(" 0 4 1000/");
				}
				num15++;
			}
			if (num15 >= 2)
			{
				break;
			}
		}
		num15 = 0;
		for (int num22 = 0; num22 < array3.Length; num22++)
		{
			if (!string.IsNullOrEmpty(array3[num22]) && array3[num22] != text && array3[num22] != text2)
			{
				Point frontRowSeatTileFromIndex2 = getFrontRowSeatTileFromIndex(num22);
				if (num15 == 1)
				{
					stringBuilder.Append("warp ").Append(array3[num22]).Append(' ')
						.Append(frontRowSeatTileFromIndex2.X - 1)
						.Append(" 6")
						.Append("/advancedMove ")
						.Append(array3[num22])
						.Append(" false 1 ")
						.Append(400)
						.Append(" 1 0 4 1000/");
				}
				else
				{
					stringBuilder.Append("warp ").Append(array3[num22]).Append(" 3 7")
						.Append("/advancedMove ")
						.Append(array3[num22])
						.Append(" false 1 300 ")
						.Append("0 -1 ")
						.Append(frontRowSeatTileFromIndex2.X - 3)
						.Append(" 0 4 1000/");
				}
				num15++;
			}
			if (num15 >= 2)
			{
				break;
			}
		}
		stringBuilder.Append("viewport 6 8 true/pause 500/");
		for (int num23 = 0; num23 < array.Length; num23++)
		{
			if (!string.IsNullOrEmpty(array[num23]))
			{
				Point backRowSeatTileFromIndex3 = getBackRowSeatTileFromIndex(num23);
				if (array[num23] == text || array[num23] == text2)
				{
					stringBuilder.Append("warp ").Append(array[num23]).Append(' ')
						.Append(point.X)
						.Append(' ')
						.Append(point.Y)
						.Append("/advancedMove ")
						.Append(array[num23])
						.Append(" false 0 -5 ")
						.Append(backRowSeatTileFromIndex3.X - point.X)
						.Append(" 0 4 1000/")
						.Append("pause ")
						.Append(1000)
						.Append("/");
				}
			}
		}
		for (int num24 = 0; num24 < array2.Length; num24++)
		{
			if (!string.IsNullOrEmpty(array2[num24]))
			{
				Point midRowSeatTileFromIndex3 = getMidRowSeatTileFromIndex(num24);
				if (array2[num24] == text || array2[num24] == text2)
				{
					stringBuilder.Append("warp ").Append(array2[num24]).Append(' ')
						.Append(point.X)
						.Append(' ')
						.Append(point.Y)
						.Append("/advancedMove ")
						.Append(array2[num24])
						.Append(" false 0 -7 ")
						.Append(midRowSeatTileFromIndex3.X - point.X)
						.Append(" 0 4 1000/")
						.Append("pause ")
						.Append(1000)
						.Append("/");
				}
			}
		}
		for (int num25 = 0; num25 < array3.Length; num25++)
		{
			if (!string.IsNullOrEmpty(array3[num25]))
			{
				Point frontRowSeatTileFromIndex3 = getFrontRowSeatTileFromIndex(num25);
				if (array3[num25] == text || array3[num25] == text2)
				{
					stringBuilder.Append("warp ").Append(array3[num25]).Append(' ')
						.Append(point.X)
						.Append(' ')
						.Append(point.Y)
						.Append("/advancedMove ")
						.Append(array3[num25])
						.Append(" false 0 -7 1 0 0 -1 1 0 0 -1 ")
						.Append(frontRowSeatTileFromIndex3.X - 3)
						.Append(" 0 4 1000/")
						.Append("pause ")
						.Append(1000)
						.Append("/");
				}
			}
		}
		stringBuilder.Append("pause 3000");
		if (flag)
		{
			stringBuilder.Append("/proceedPosition ").Append(text2);
		}
		stringBuilder.Append("/pause 1000");
		if (!flag)
		{
			stringBuilder.Append("/proceedPosition farmer");
		}
		stringBuilder.Append("/waitForAllStationary/pause 100");
		foreach (Character item4 in list)
		{
			string eventName = getEventName(item4);
			if (eventName != text && eventName != text2)
			{
				if (item4 is Farmer)
				{
					stringBuilder.Append("/faceDirection ").Append(eventName).Append(" 0 true/positionOffset ")
						.Append(eventName)
						.Append(" 0 42 true");
				}
				else
				{
					stringBuilder.Append("/faceDirection ").Append(eventName).Append(" 0 true/positionOffset ")
						.Append(eventName)
						.Append(" 0 12 true");
				}
				if (theaterRandom.NextDouble() < 0.2)
				{
					stringBuilder.Append("/pause 100");
				}
			}
		}
		stringBuilder.Append("/positionOffset ").Append(text).Append(" 0 32");
		if (flag)
		{
			stringBuilder.Append("/positionOffset ").Append(text2).Append(" 0 8");
		}
		stringBuilder.Append("/ambientLight 210 210 120 true/pause 500/viewport move 0 -1 4000/pause 5000");
		List<Character> list2 = new List<Character>();
		foreach (List<Character> playerAndGuestAudienceGroup3 in playerAndGuestAudienceGroups)
		{
			foreach (Character item5 in playerAndGuestAudienceGroup3)
			{
				if (!(item5 is Farmer) && !list2.Contains(item5))
				{
					list2.Add(item5);
				}
			}
		}
		for (int num26 = 0; num26 < list2.Count; num26++)
		{
			int index = theaterRandom.Next(list2.Count);
			Character value = list2[num26];
			list2[num26] = list2[index];
			list2[index] = value;
		}
		int num27 = 0;
		foreach (MovieScene scene in movieData.Scenes)
		{
			if (scene.ResponsePoint == null)
			{
				continue;
			}
			bool flag4 = false;
			for (int num28 = 0; num28 < list2.Count; num28++)
			{
				MovieCharacterReaction reactionsForCharacter = MovieTheater.GetReactionsForCharacter(list2[num28] as NPC);
				if (reactionsForCharacter == null)
				{
					continue;
				}
				foreach (MovieReaction reaction in reactionsForCharacter.Reactions)
				{
					if (!reaction.ShouldApplyToMovie(movieData, MovieTheater.GetPatronNames(), MovieTheater.GetResponseForMovie(list2[num28] as NPC)) || reaction.SpecialResponses?.DuringMovie == null || (!(reaction.SpecialResponses.DuringMovie.ResponsePoint == scene.ResponsePoint) && reaction.Whitelist.Count <= 0))
					{
						continue;
					}
					if (!_whiteListDependencyLookup.ContainsKey(list2[num28]))
					{
						_responseOrder[num27] = list2[num28];
						if (reaction.Whitelist != null)
						{
							for (int num29 = 0; num29 < reaction.Whitelist.Count; num29++)
							{
								Character characterFromName = Game1.getCharacterFromName(reaction.Whitelist[num29]);
								if (characterFromName == null)
								{
									continue;
								}
								_whiteListDependencyLookup[characterFromName] = list2[num28];
								foreach (int key in _responseOrder.Keys)
								{
									if (_responseOrder[key] == characterFromName)
									{
										_responseOrder.Remove(key);
									}
								}
							}
						}
					}
					list2.RemoveAt(num28);
					num28--;
					flag4 = true;
					break;
				}
				if (flag4)
				{
					break;
				}
			}
			if (!flag4)
			{
				for (int num30 = 0; num30 < list2.Count; num30++)
				{
					MovieCharacterReaction reactionsForCharacter2 = MovieTheater.GetReactionsForCharacter(list2[num30] as NPC);
					if (reactionsForCharacter2 == null)
					{
						continue;
					}
					foreach (MovieReaction reaction2 in reactionsForCharacter2.Reactions)
					{
						if (!reaction2.ShouldApplyToMovie(movieData, MovieTheater.GetPatronNames(), MovieTheater.GetResponseForMovie(list2[num30] as NPC)) || reaction2.SpecialResponses?.DuringMovie == null || !(reaction2.SpecialResponses.DuringMovie.ResponsePoint == num27.ToString()))
						{
							continue;
						}
						if (!_whiteListDependencyLookup.ContainsKey(list2[num30]))
						{
							_responseOrder[num27] = list2[num30];
							if (reaction2.Whitelist != null)
							{
								for (int num31 = 0; num31 < reaction2.Whitelist.Count; num31++)
								{
									Character characterFromName2 = Game1.getCharacterFromName(reaction2.Whitelist[num31]);
									if (characterFromName2 == null)
									{
										continue;
									}
									_whiteListDependencyLookup[characterFromName2] = list2[num30];
									foreach (int key2 in _responseOrder.Keys)
									{
										if (_responseOrder[key2] == characterFromName2)
										{
											_responseOrder.Remove(key2);
										}
									}
								}
							}
						}
						list2.RemoveAt(num30);
						num30--;
						flag4 = true;
						break;
					}
					if (flag4)
					{
						break;
					}
				}
			}
			num27++;
		}
		num27 = 0;
		for (int num32 = 0; num32 < list2.Count; num32++)
		{
			if (!_whiteListDependencyLookup.ContainsKey(list2[num32]))
			{
				for (; _responseOrder.ContainsKey(num27); num27++)
				{
				}
				_responseOrder[num27] = list2[num32];
				num27++;
			}
		}
		list2 = null;
		foreach (MovieScene scene2 in movieData.Scenes)
		{
			_ParseScene(stringBuilder, scene2);
		}
		while (currentResponse < _responseOrder.Count)
		{
			_ParseResponse(stringBuilder);
		}
		stringBuilder.Append("/stopMusic");
		stringBuilder.Append("/fade/viewport -1000 -1000");
		stringBuilder.Append("/pause 500/message \"").Append(Game1.content.LoadString("Strings\\Locations:Theater_MovieEnd")).Append("\"/pause 500");
		stringBuilder.Append("/requestMovieEnd");
		return new Event(stringBuilder.ToString(), null, "MovieTheaterScreening");
	}

	protected void _ParseScene(StringBuilder sb, MovieScene scene)
	{
		if (!string.IsNullOrWhiteSpace(scene.Sound))
		{
			sb.Append("/playSound ").Append(scene.Sound);
		}
		if (!string.IsNullOrWhiteSpace(scene.Music))
		{
			sb.Append("/playMusic ").Append(scene.Music);
		}
		if (scene.MessageDelay > 0)
		{
			sb.Append("/pause ").Append(scene.MessageDelay);
		}
		if (scene.Image >= 0)
		{
			sb.Append("/specificTemporarySprite movieTheater_screen ").Append(movieData.Id).Append(' ')
				.Append(scene.Image)
				.Append(' ')
				.Append(scene.Shake);
			if (movieData.Texture != null)
			{
				sb.Append(" \"").Append(ArgUtility.EscapeQuotes(movieData.Texture)).Append('"');
			}
		}
		if (!string.IsNullOrWhiteSpace(scene.Script))
		{
			sb.Append(TokenParser.ParseText(scene.Script));
		}
		if (!string.IsNullOrWhiteSpace(scene.Text))
		{
			sb.Append("/message \"").Append(ArgUtility.EscapeQuotes(TokenParser.ParseText(scene.Text))).Append('"');
		}
		if (scene.ResponsePoint != null)
		{
			_ParseResponse(sb, scene);
		}
	}

	protected void _ParseResponse(StringBuilder sb, MovieScene scene = null)
	{
		if (_responseOrder.TryGetValue(currentResponse, out var value))
		{
			sb.Append("/pause 500");
			bool ignoreScript = false;
			if (!_whiteListDependencyLookup.ContainsKey(value))
			{
				MovieCharacterReaction reactionsForCharacter = MovieTheater.GetReactionsForCharacter(value as NPC);
				if (reactionsForCharacter != null)
				{
					foreach (MovieReaction reaction in reactionsForCharacter.Reactions)
					{
						if (reaction.ShouldApplyToMovie(movieData, MovieTheater.GetPatronNames(), MovieTheater.GetResponseForMovie(value as NPC)) && reaction.SpecialResponses?.DuringMovie != null && (string.IsNullOrEmpty(reaction.SpecialResponses.DuringMovie.ResponsePoint) || (scene != null && reaction.SpecialResponses.DuringMovie.ResponsePoint == scene.ResponsePoint) || reaction.SpecialResponses.DuringMovie.ResponsePoint == currentResponse.ToString() || reaction.Whitelist.Count > 0))
						{
							string value2 = TokenParser.ParseText(reaction.SpecialResponses.DuringMovie.Script);
							string value3 = TokenParser.ParseText(reaction.SpecialResponses.DuringMovie.Text);
							if (!string.IsNullOrWhiteSpace(value2))
							{
								sb.Append(value2);
								ignoreScript = true;
							}
							if (!string.IsNullOrWhiteSpace(value3))
							{
								sb.Append("/speak ").Append(value.name.Value).Append(" \"")
									.Append(value3)
									.Append('"');
							}
							break;
						}
					}
				}
			}
			_ParseCharacterResponse(sb, value, ignoreScript);
			foreach (Character key in _whiteListDependencyLookup.Keys)
			{
				if (_whiteListDependencyLookup[key] == value)
				{
					_ParseCharacterResponse(sb, key);
				}
			}
		}
		currentResponse++;
	}

	protected void _ParseCharacterResponse(StringBuilder sb, Character responding_character, bool ignoreScript = false)
	{
		string responseForMovie = MovieTheater.GetResponseForMovie(responding_character as NPC);
		if (_whiteListDependencyLookup.TryGetValue(responding_character, out var value))
		{
			responseForMovie = MovieTheater.GetResponseForMovie(value as NPC);
		}
		switch (responseForMovie)
		{
		case "love":
			sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
				.Append(200);
			if (!ignoreScript)
			{
				sb.Append("/playSound reward/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(20)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_LoveMovie", responding_character.displayName))
					.Append('"');
			}
			break;
		case "like":
			sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
				.Append(100);
			if (!ignoreScript)
			{
				sb.Append("/playSound give_gift/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(56)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_LikeMovie", responding_character.displayName))
					.Append('"');
			}
			break;
		case "dislike":
			sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
				.Append(0);
			if (!ignoreScript)
			{
				sb.Append("/playSound newArtifact/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(24)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_DislikeMovie", responding_character.displayName))
					.Append('"');
			}
			break;
		}
		if (_concessionsData != null && _concessionsData.TryGetValue(responding_character, out var value2))
		{
			string concessionTasteForCharacter = MovieTheater.GetConcessionTasteForCharacter(responding_character, value2);
			string text = "";
			if (NPC.TryGetData(responding_character.name.Value, out var data))
			{
				switch (data.Gender)
				{
				case Gender.Female:
					text = "_Female";
					break;
				case Gender.Male:
					text = "_Male";
					break;
				}
			}
			string value3 = "eat";
			if (value2.Tags != null && value2.Tags.Contains("Drink"))
			{
				value3 = "gulp";
			}
			switch (concessionTasteForCharacter)
			{
			case "love":
				sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
					.Append(50);
				sb.Append("/tossConcession ").Append(responding_character.Name).Append(' ')
					.Append(value2.Id)
					.Append("/pause 1000");
				sb.Append("/playSound ").Append(value3).Append("/shake ")
					.Append(responding_character.Name)
					.Append(" 500/pause 1000");
				sb.Append("/playSound reward/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(20)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_LoveConcession" + text, responding_character.displayName, value2.DisplayName))
					.Append('"');
				break;
			case "like":
				sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
					.Append(25);
				sb.Append("/tossConcession ").Append(responding_character.Name).Append(' ')
					.Append(value2.Id)
					.Append("/pause 1000");
				sb.Append("/playSound ").Append(value3).Append("/shake ")
					.Append(responding_character.Name)
					.Append(" 500/pause 1000");
				sb.Append("/playSound give_gift/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(56)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_LikeConcession" + text, responding_character.displayName, value2.DisplayName))
					.Append('"');
				break;
			case "dislike":
				sb.Append("/friendship ").Append(responding_character.Name).Append(' ')
					.Append(0);
				sb.Append("/playSound croak/pause 1000");
				sb.Append("/playSound newArtifact/emote ").Append(responding_character.name.Value).Append(' ')
					.Append(40)
					.Append("/message \"")
					.Append(Game1.content.LoadString("Strings\\Characters:MovieTheater_DislikeConcession" + text, responding_character.displayName, value2.DisplayName))
					.Append('"');
				break;
			}
		}
		_characterResponses[responding_character] = responseForMovie;
	}

	public Dictionary<Character, string> GetCharacterResponses()
	{
		return _characterResponses;
	}

	private static string getEventName(Character c)
	{
		if (c is Farmer who)
		{
			return "farmer" + Utility.getFarmerNumberFromFarmer(who);
		}
		return c.name.Value;
	}

	private Point getBackRowSeatTileFromIndex(int index)
	{
		return index switch
		{
			0 => new Point(2, 10), 
			1 => new Point(3, 10), 
			2 => new Point(4, 10), 
			3 => new Point(5, 10), 
			4 => new Point(8, 10), 
			5 => new Point(9, 10), 
			6 => new Point(10, 10), 
			7 => new Point(11, 10), 
			_ => new Point(4, 12), 
		};
	}

	private Point getMidRowSeatTileFromIndex(int index)
	{
		return index switch
		{
			0 => new Point(3, 8), 
			1 => new Point(4, 8), 
			2 => new Point(5, 8), 
			3 => new Point(8, 8), 
			4 => new Point(9, 8), 
			5 => new Point(10, 8), 
			_ => new Point(4, 12), 
		};
	}

	private Point getFrontRowSeatTileFromIndex(int index)
	{
		return index switch
		{
			0 => new Point(4, 6), 
			1 => new Point(5, 6), 
			2 => new Point(8, 6), 
			3 => new Point(9, 6), 
			_ => new Point(4, 12), 
		};
	}
}
