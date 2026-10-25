using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework.Audio;

namespace StardewValley.Pokemon;

public enum PokemonMusicTrack
{
	None,
	WildBattle,
	TrainerBattle,
	MegaBossBattle,
	LegendaryBattle,
	VictoryFanfare,
	RouteOverworld
}

/// <summary>
/// Gerencia a reprodução das trilhas sonoras autênticas de batalha, vitória e rotas de Pokémon,
/// suportando reprodução direta de MIDI oficial (.mid via winmm MCI), arquivos de áudio .ogg/.wav,
/// e fallback para síntese procedural em memória.
/// Respeita rigorosamente o volume configurado nas opções do Stardew Valley.
/// </summary>
public static class PokemonMusicManager
{
	private const string MidiAlias = "poke_bgm";
	private static bool _midiPlaying = false;

	[DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi)]
	private static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

	private static readonly Dictionary<PokemonMusicTrack, SoundEffect> _cachedEffects = new();
	private static SoundEffectInstance _activeInstance;
	private static PokemonMusicTrack _currentTrack = PokemonMusicTrack.None;
	private static bool _currentTrackLooped = false;
	private static bool _pausedStardewSong = false;
	private static float _overworldSilenceTimer = 0f;
	public static bool EnableOverworldPokemonMusic { get; set; } = true;

	public static PokemonMusicTrack CurrentTrack => _currentTrack;

	public static void TickLoop()
	{
		try
		{
			float volMult = (_currentTrack == PokemonMusicTrack.RouteOverworld) ? 0.35f : 0.65f;
			float targetVol = Math.Clamp(Game1.options?.musicVolumeLevel ?? 0.7f, 0f, 1f) * volMult;

			if (_midiPlaying)
			{
				int vol1000 = (int)Math.Clamp(targetVol * 1000f, 0f, 1000f);
				mciSendString($"setaudio {MidiAlias} volume to {vol1000}", null, 0, IntPtr.Zero);
			}

			if (_activeInstance != null && !_activeInstance.IsDisposed && _currentTrack != PokemonMusicTrack.None)
			{
				_activeInstance.Volume = targetVol;

				if (_currentTrackLooped && _activeInstance.State == SoundState.Stopped)
				{
					_activeInstance.Play();
				}
			}
		}
		catch { }
	}

	public static void UpdateOverworldMusic(float dt)
	{
		try
		{
			TickLoop();

			// Não interfere se estiver dentro de uma batalha
			if (Game1.activeClickableMenu is PokemonBattleMenu)
				return;

			bool stardewPlaying = Game1.currentSong != null && Game1.currentSong.IsPlaying && !_pausedStardewSong;
			if (!EnableOverworldPokemonMusic || stardewPlaying)
			{
				_overworldSilenceTimer = 0f;
				if (_currentTrack == PokemonMusicTrack.RouteOverworld)
				{
					StopActivePokemonTrackOnly();
				}
				return;
			}

			// Se já estiver tocando RouteOverworld, mantém
			if (_currentTrack == PokemonMusicTrack.RouteOverworld && (_midiPlaying || (_activeInstance != null && !_activeInstance.IsDisposed && _activeInstance.State == SoundState.Playing)))
			{
				return;
			}

			_overworldSilenceTimer += dt;
			if (_overworldSilenceTimer >= 4.0f)
			{
				_overworldSilenceTimer = 0f;
				PlayTrack(PokemonMusicTrack.RouteOverworld, loop: true, pauseStardew: false);
			}
		}
		catch { }
	}

	public static void PlayBattleMusic(bool isWild, string trainerName, PokemonInstance enemyMon)
	{
		try
		{
			PauseStardewMusic();
			StopActivePokemonTrackOnly();

			string specificName = null;
			PokemonMusicTrack track = isWild ? PokemonMusicTrack.WildBattle : PokemonMusicTrack.TrainerBattle;
			if (!isWild)
			{
				bool isSpecial = !string.IsNullOrEmpty(trainerName) && (
					trainerName.Equals("Red", StringComparison.OrdinalIgnoreCase) ||
					trainerName.Equals("Champion", StringComparison.OrdinalIgnoreCase) ||
					trainerName.Equals("Steven", StringComparison.OrdinalIgnoreCase) ||
					trainerName.Equals("Wallace", StringComparison.OrdinalIgnoreCase) ||
					trainerName.Contains("Aqua", StringComparison.OrdinalIgnoreCase) ||
					trainerName.Contains("Magma", StringComparison.OrdinalIgnoreCase) ||
					trainerName.Equals("Wizard", StringComparison.OrdinalIgnoreCase) ||
					trainerName.Equals("Qi", StringComparison.OrdinalIgnoreCase));
				if (isSpecial)
				{
					track = PokemonMusicTrack.MegaBossBattle;
					specificName = (trainerName.Equals("Champion", StringComparison.OrdinalIgnoreCase) || trainerName.Equals("Steven", StringComparison.OrdinalIgnoreCase))
						? "champion" : "gym_leader";
				}
				else
				{
					// Alterna aleatoriamente entre Emerald Trainer e Red/Blue Trainer
					specificName = (Game1.random.Next(2) == 0) ? "emerald_trainer" : "rb_trainer";
				}
			}
			else if (enemyMon?.Species != null)
			{
				string monName = enemyMon.Species.Name;
				if (monName.Equals("Rayquaza", StringComparison.OrdinalIgnoreCase)) specificName = "rayquaza";
				else if (monName.Equals("Kyogre", StringComparison.OrdinalIgnoreCase) || monName.Equals("Groudon", StringComparison.OrdinalIgnoreCase)) specificName = "primal";
				else if (monName.Equals("Mewtwo", StringComparison.OrdinalIgnoreCase)) specificName = "mewtwo";
				else if (monName.Equals("Deoxys", StringComparison.OrdinalIgnoreCase)) specificName = "deoxys";
				else
				{
					var entry = PokemonPokedex.Instance?.GetEntry(monName);
					bool isLegendary = (entry != null && (entry.Tier == "Legendary" || entry.Tier == "Mythical"))
						|| monName is "Arceus" or "Giratina" or "Dialga" or "Palkia" or "Mew" or "Celebi" or "Lugia" or "Ho-Oh";
					if (isLegendary)
					{
						track = PokemonMusicTrack.LegendaryBattle;
						specificName = "champion";
					}
					else if (Game1.currentLocation is Locations.MineShaft || (enemyMon.HasType(PokemonType.Ghost) && Game1.isDarkOut(Game1.currentLocation)))
					{
						specificName = "lavender_town";
					}
					else
					{
						// Alterna aleatoriamente entre Emerald Wild e Red/Blue Wild
						specificName = (Game1.random.Next(2) == 0) ? "emerald_wild" : "rb_wild";
					}
				}
			}

			PlayTrack(track, loop: true, pauseStardew: true, specificFile: specificName);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PokemonMusicManager] Erro ao iniciar música de batalha: {ex.Message}");
		}
	}

	public static void PlayVictoryMusic()
	{
		try
		{
			PauseStardewMusic();
			StopActivePokemonTrackOnly();
			PlayTrack(PokemonMusicTrack.VictoryFanfare, loop: false, pauseStardew: true);
		}
		catch { }
	}

	public static void PlayMidiFile(string fileName, bool loop = true)
	{
		try
		{
			PauseStardewMusic();
			StopActivePokemonTrackOnly();
			PlayTrack(PokemonMusicTrack.WildBattle, loop: loop, pauseStardew: true, specificFile: fileName);
		}
		catch { }
	}

	private static string ResolveMusicFilePath(string baseName, string ext)
	{
		if (string.IsNullOrWhiteSpace(baseName)) return null;
		string nameWithExt = baseName.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ? baseName : (baseName + ext);
		string[] searchDirs = new[]
		{
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Pokemon", "Audio", "music"),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald", "sound", "songs", "midi"),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokeemerald", "sound", "songs")
		};
		foreach (var dir in searchDirs)
		{
			if (Directory.Exists(dir))
			{
				string p = Path.Combine(dir, nameWithExt);
				if (File.Exists(p)) return p;
			}
		}
		return null;
	}

	private static bool PlayMidiDirect(string path, bool loop, float volume)
	{
		try
		{
			mciSendString($"close {MidiAlias}", null, 0, IntPtr.Zero);
			int openRes = mciSendString($"open \"{path}\" type sequencer alias {MidiAlias}", null, 0, IntPtr.Zero);
			if (openRes != 0) return false;

			int vol1000 = (int)Math.Clamp(volume * 1000f, 0f, 1000f);
			mciSendString($"setaudio {MidiAlias} volume to {vol1000}", null, 0, IntPtr.Zero);

			string playCmd = loop ? $"play {MidiAlias} repeat" : $"play {MidiAlias}";
			int playRes = mciSendString(playCmd, null, 0, IntPtr.Zero);
			_midiPlaying = (playRes == 0);
			return _midiPlaying;
		}
		catch
		{
			return false;
		}
	}

	public static void PlayTrack(PokemonMusicTrack track, bool loop, bool pauseStardew = true, string specificFile = null)
	{
		try
		{
			if (pauseStardew)
			{
				PauseStardewMusic();
			}
			StopActivePokemonTrackOnly();

			float volMult = (track == PokemonMusicTrack.RouteOverworld) ? 0.35f : 0.65f;
			float vol = Math.Clamp(Game1.options?.musicVolumeLevel ?? 0.7f, 0f, 1f) * volMult;
			if (vol <= 0.005f)
			{
				_currentTrack = track;
				_currentTrackLooped = loop;
				return;
			}

			string trackName = specificFile ?? track switch
			{
				PokemonMusicTrack.WildBattle => (Game1.random.Next(2) == 0 ? "emerald_wild" : "rb_wild"),
				PokemonMusicTrack.TrainerBattle => (Game1.random.Next(2) == 0 ? "emerald_trainer" : "rb_trainer"),
				PokemonMusicTrack.MegaBossBattle => "gym_leader",
				PokemonMusicTrack.LegendaryBattle => "champion",
				PokemonMusicTrack.RouteOverworld => "mus_cycling",
				PokemonMusicTrack.VictoryFanfare => "mus_caught",
				_ => "emerald_wild"
			};

			// 1. Tenta carregar e tocar MIDI original via MCI nativo
			string midPath = ResolveMusicFilePath(trackName, ".mid");
			if (!string.IsNullOrEmpty(midPath) && File.Exists(midPath))
			{
				if (PlayMidiDirect(midPath, loop, vol))
				{
					_currentTrack = track;
					_currentTrackLooped = loop;
					return;
				}
			}

			// 2. Tenta carregar stream de arquivo OGG ou WAV
			string oggPath = ResolveMusicFilePath(trackName, ".ogg") ?? ResolveMusicFilePath(trackName, ".wav");
			if (!string.IsNullOrEmpty(oggPath) && File.Exists(oggPath))
			{
				try
				{
					using var stream = File.OpenRead(oggPath);
					var sfx = SoundEffect.FromStream(stream);
					if (sfx != null)
					{
						_activeInstance = sfx.CreateInstance();
						_activeInstance.Volume = vol;
						_activeInstance.Play();
						_currentTrack = track;
						_currentTrackLooped = loop;
						return;
					}
				}
				catch { }
			}

			// 3. Fallback: síntese procedural nostálgica
			if (!_cachedEffects.TryGetValue(track, out var fallbackSfx) || fallbackSfx == null || fallbackSfx.IsDisposed)
			{
				fallbackSfx = BuildTrackSoundEffect(track);
				_cachedEffects[track] = fallbackSfx;
			}

			if (fallbackSfx != null)
			{
				_activeInstance = fallbackSfx.CreateInstance();
				_activeInstance.Volume = vol * 0.45f;
				_activeInstance.Play();
				_currentTrack = track;
				_currentTrackLooped = loop;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PokemonMusicManager] PlayTrack falhou: {ex.Message}");
		}
	}

	public static void StopAndRestoreStardewMusic()
	{
		try
		{
			StopActivePokemonTrackOnly();
			ResumeStardewMusic();
		}
		catch { }
	}

	private static void StopActivePokemonTrackOnly()
	{
		try
		{
			if (_midiPlaying)
			{
				mciSendString($"stop {MidiAlias}", null, 0, IntPtr.Zero);
				mciSendString($"close {MidiAlias}", null, 0, IntPtr.Zero);
				_midiPlaying = false;
			}
			if (_activeInstance != null)
			{
				if (!_activeInstance.IsDisposed)
				{
					_activeInstance.Stop();
					_activeInstance.Dispose();
				}
				_activeInstance = null;
			}
			_currentTrack = PokemonMusicTrack.None;
		}
		catch { }
	}

	private static void PauseStardewMusic()
	{
		try
		{
			if (Game1.currentSong != null && Game1.currentSong.IsPlaying)
			{
				Game1.currentSong.Pause();
				_pausedStardewSong = true;
			}
		}
		catch { }
	}

	private static void ResumeStardewMusic()
	{
		try
		{
			if (_pausedStardewSong && Game1.currentSong != null && Game1.currentSong.IsPaused)
			{
				Game1.currentSong.Resume();
			}
			_pausedStardewSong = false;
		}
		catch { }
	}

	private static SoundEffect BuildTrackSoundEffect(PokemonMusicTrack track)
	{
		const int sampleRate = 22050;
		List<(float lead, float bass, float dur)> seq = track switch
		{
			PokemonMusicTrack.WildBattle      => BuildWildBattleSequence(),
			PokemonMusicTrack.TrainerBattle   => BuildTrainerBattleSequence(),
			PokemonMusicTrack.MegaBossBattle  => BuildMegaBossSequence(),
			PokemonMusicTrack.LegendaryBattle => BuildLegendarySequence(),
			PokemonMusicTrack.VictoryFanfare  => BuildVictorySequence(),
			PokemonMusicTrack.RouteOverworld  => BuildRouteOverworldSequence(),
			_                                 => BuildWildBattleSequence()
		};

		int totalSamples = 0;
		foreach (var n in seq)
		{
			totalSamples += Math.Max(1, (int)(n.dur * sampleRate));
		}

		byte[] pcm16 = new byte[totalSamples * 2];
		int sampleIdx = 0;
		double leadPhase = 0.0;
		double bassPhase = 0.0;

		foreach (var n in seq)
		{
			int count = Math.Max(1, (int)(n.dur * sampleRate));
			for (int i = 0; i < count && sampleIdx < totalSamples; i++)
			{
				float env = 1.0f;
				float rel = i / (float)count;
				if (rel < 0.04f) env = rel / 0.04f;
				else if (rel > 0.86f) env = Math.Max(0f, (1f - rel) / 0.14f);

				float leadSample = 0f;
				if (n.lead > 20f)
				{
					leadPhase += n.lead / sampleRate;
					if (leadPhase >= 1.0) leadPhase -= 1.0;
					float pulse = (leadPhase < 0.25) ? 0.55f : -0.25f;
					float tri = (float)(Math.Sin(leadPhase * Math.PI * 2.0) * 0.35);
					leadSample = (pulse + tri) * env;
				}

				float bassSample = 0f;
				if (n.bass > 20f)
				{
					bassPhase += n.bass / sampleRate;
					if (bassPhase >= 1.0) bassPhase -= 1.0;
					float triangle = (float)(Math.Abs((bassPhase * 4.0) - 2.0) - 1.0);
					bassSample = triangle * 0.65f * env;
				}

				float mixed = (leadSample * 0.55f) + (bassSample * 0.45f);
				short s16 = (short)Math.Clamp((int)(mixed * 16000f), -32767, 32767);
				pcm16[sampleIdx * 2] = (byte)(s16 & 0xFF);
				pcm16[(sampleIdx * 2) + 1] = (byte)((s16 >> 8) & 0xFF);
				sampleIdx++;
			}
		}

		return new SoundEffect(pcm16, sampleRate, AudioChannels.Mono);
	}

	private static List<(float lead, float bass, float dur)> BuildWildBattleSequence()
	{
		var s = new List<(float, float, float)>();
		float[] introNotes = { 440f, 493.88f, 523.25f, 587.33f, 659.25f, 698.46f, 783.99f, 880f };
		foreach (var n in introNotes) s.Add((n, n * 0.5f, 0.06f));
		float[] riff = { 587.33f, 587.33f, 659.25f, 587.33f, 698.46f, 659.25f, 587.33f, 523.25f };
		float[] bass = { 146.83f, 146.83f, 164.81f, 146.83f, 174.61f, 164.81f, 146.83f, 130.81f };
		for (int rep = 0; rep < 4; rep++)
		{
			for (int i = 0; i < riff.Length; i++) s.Add((riff[i], bass[i], 0.12f));
		}
		return s;
	}

	private static List<(float lead, float bass, float dur)> BuildTrainerBattleSequence()
	{
		var s = new List<(float, float, float)>();
		for (int i = 0; i < 4; i++) { s.Add((523.25f, 130.81f, 0.07f)); s.Add((0f, 130.81f, 0.05f)); }
		float[] lead = { 659.25f, 659.25f, 698.46f, 783.99f, 698.46f, 659.25f, 587.33f, 523.25f };
		float[] bass = { 164.81f, 164.81f, 174.61f, 196.00f, 174.61f, 164.81f, 146.83f, 130.81f };
		for (int rep = 0; rep < 4; rep++)
		{
			for (int i = 0; i < lead.Length; i++) s.Add((lead[i], bass[i], 0.11f));
		}
		return s;
	}

	private static List<(float lead, float bass, float dur)> BuildMegaBossSequence()
	{
		var s = new List<(float, float, float)>();
		float[] intro = { 220f, 246.94f, 261.63f, 293.66f, 329.63f, 349.23f, 392f, 440f };
		foreach (var n in intro) s.Add((n * 2f, n, 0.05f));
		float[] lead = { 440f, 440f, 523.25f, 440f, 587.33f, 523.25f, 440f, 392f };
		float[] bass = { 110f, 110f, 130.81f, 110f, 146.83f, 130.81f, 110f, 98f };
		for (int rep = 0; rep < 4; rep++)
		{
			for (int i = 0; i < lead.Length; i++) s.Add((lead[i] * 1.5f, bass[i], 0.10f));
		}
		return s;
	}

	private static List<(float lead, float bass, float dur)> BuildLegendarySequence()
	{
		var s = new List<(float, float, float)>();
		float[] intro = { 130.81f, 146.83f, 164.81f, 174.61f, 196.00f, 220.00f, 246.94f, 261.63f };
		foreach (var n in intro) s.Add((n * 2.5f, n, 0.07f));
		float[] lead = { 523.25f, 493.88f, 523.25f, 587.33f, 659.25f, 587.33f, 523.25f, 440f };
		float[] bass = { 130.81f, 123.47f, 130.81f, 146.83f, 164.81f, 146.83f, 130.81f, 110f };
		for (int rep = 0; rep < 4; rep++)
		{
			for (int i = 0; i < lead.Length; i++) s.Add((lead[i], bass[i], 0.13f));
		}
		return s;
	}

	private static List<(float lead, float bass, float dur)> BuildVictorySequence()
	{
		var s = new List<(float, float, float)>();
		float[] fan = { 523.25f, 523.25f, 523.25f, 659.25f, 783.99f, 659.25f, 783.99f, 1046.50f };
		float[] dur = { 0.12f, 0.12f, 0.12f, 0.24f, 0.18f, 0.12f, 0.18f, 0.60f };
		for (int i = 0; i < fan.Length; i++) s.Add((fan[i], fan[i] * 0.5f, dur[i]));
		return s;
	}

	private static List<(float lead, float bass, float dur)> BuildRouteOverworldSequence()
	{
		var s = new List<(float, float, float)>();
		float[] notes = { 329.63f, 392f, 440f, 493.88f, 523.25f, 493.88f, 440f, 392f, 329.63f, 293.66f, 329.63f, 392f, 440f, 392f, 329.63f, 293.66f };
		float[] bass = { 164.81f, 196f, 220f, 246.94f, 261.63f, 246.94f, 220f, 196f, 164.81f, 146.83f, 164.81f, 196f, 220f, 196f, 164.81f, 146.83f };
		for (int i = 0; i < notes.Length; i++) s.Add((notes[i], bass[i], 0.22f));
		return s;
	}
}
