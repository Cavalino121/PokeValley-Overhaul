using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Netcode;

namespace StardewValley.Audio;

public class LoopingCueManager
{
	private Dictionary<string, ICue> playingCues = new Dictionary<string, ICue>();

	private List<string> cuesToStop = new List<string>();

	public virtual void Update(GameLocation currentLocation)
	{
		NetDictionary<string, bool, NetBool, SerializableDictionary<string, bool>, StardewValley.Network.NetStringDictionary<bool, NetBool>>.KeysCollection activeCues = currentLocation.netAudio.ActiveCues;
		foreach (string item in activeCues)
		{
			if (!playingCues.ContainsKey(item))
			{
				Game1.playSound(item, out var cue);
				playingCues[item] = cue;
			}
		}
		foreach (KeyValuePair<string, ICue> playingCue in playingCues)
		{
			string key = playingCue.Key;
			if (!activeCues.Contains(key))
			{
				cuesToStop.Add(key);
			}
		}
		foreach (string item2 in cuesToStop)
		{
			playingCues[item2].Stop(AudioStopOptions.AsAuthored);
			playingCues.Remove(item2);
		}
		cuesToStop.Clear();
	}

	public void StopAll()
	{
		foreach (ICue value in playingCues.Values)
		{
			value.Stop(AudioStopOptions.Immediate);
		}
		playingCues.Clear();
	}
}
