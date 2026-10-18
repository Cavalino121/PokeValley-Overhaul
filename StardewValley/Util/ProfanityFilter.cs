using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StardewValley.Util;

internal class ProfanityFilter
{
	private readonly List<Regex> _words;

	private readonly StringBuilder _cleanup;

	public ProfanityFilter()
		: this("Content/profanity.regex")
	{
	}

	public ProfanityFilter(string profanityFile)
	{
		_cleanup = new StringBuilder(2048);
		string[] array = File.ReadAllLines(profanityFile);
		_words = new List<Regex>(array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			Regex item = new Regex(array[i], RegexOptions.IgnoreCase | RegexOptions.Compiled);
			_words.Add(item);
		}
	}

	public string Filter(string words)
	{
		if (string.IsNullOrWhiteSpace(words))
		{
			return words;
		}
		for (int i = 0; i < _words.Count; i++)
		{
			MatchCollection matchCollection = _words[i].Matches(words);
			if (matchCollection.Count == 0)
			{
				continue;
			}
			_cleanup.Clear();
			_cleanup.Append(words);
			for (int j = 0; j < matchCollection.Count; j++)
			{
				Match match = matchCollection[j];
				int num = match.Index + match.Length;
				for (int k = match.Index; k < num; k++)
				{
					if (!char.IsWhiteSpace(_cleanup[k]))
					{
						_cleanup[k] = '*';
					}
				}
			}
			words = _cleanup.ToString();
		}
		return words;
	}
}
