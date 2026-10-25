using System;
using System.Collections.Generic;
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
/// Synthesizes authentic GBA-style Pokémon chiptune battle, victory & route overworld tracks in-memory using MonoGame SoundEffect
/// <summary>
/// Gerencia a reprodução das trilhas sonoras autênticas de batalha, vitória e rotas de Pokémon,
/// pausando a música do Stardew Valley durante as batalhas e integrando-se nativamente ao SoundEffect do MonoGame.
/// Respeita estritamente as opções de volume de música do jogo.
/// </summary>
public static class PokemonMusicManager
{
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
			if (_activeInstance != null && !_activeInstance.IsDisposed && _currentTrack != PokemonMusicTrack.None)
			{
				float volMult = (_currentTrack == PokemonMusicTrack.RouteOverworld) ? 0.28f : 0.42f;
				float targetVol = Math.Clamp(Game1.options?.musicVolumeLevel ?? 0.7f, 0f, 1f) * volMult;
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

			// Do not interfere while a battle menu is active
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

			// Stardew's song finished naturally and the overworld is currently silent
			if (_currentTrack == PokemonMusicTrack.RouteOverworld && (_activeInstance != null && !_activeInstance.IsDisposed && _activeInstance.State == SoundState.Playing))
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
				if (isSpecial) track = PokemonMusicTrack.MegaBossBattle;
			}
			else if (enemyMon?.Species != null)
			{
				string monName = enemyMon.Species.Name;
				var entry = PokemonPokedex.Instance?.GetEntry(monName);
				bool isLegendary = (entry != null && (entry.Tier == "Legendary" || entry.Tier == "Mythical"))
					|| monName is "Rayquaza" or "Groudon" or "Kyogre" or "Mewtwo" or "Mew" or "Deoxys" or "Arceus" or "Giratina" or "Dialga" or "Palkia";
				if (isLegendary)
				{
					track = PokemonMusicTrack.LegendaryBattle;
				}
			}

			PlayTrack(track, loop: true, pauseStardew: true);
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
		string lower = (fileName ?? "").ToLowerInvariant();
		PokemonMusicTrack track = lower switch
		{
			var s when s.Contains("wild") => PokemonMusicTrack.WildBattle,
			var s when s.Contains("trainer") => PokemonMusicTrack.TrainerBattle,
			var s when s.Contains("gym") => PokemonMusicTrack.MegaBossBattle,
			var s when s.Contains("champion") || s.Contains("steven") => PokemonMusicTrack.MegaBossBattle,
			var s when s.Contains("kyogre") || s.Contains("groudon") || s.Contains("rayquaza") || s.Contains("regi") || s.Contains("deoxys") || s.Contains("mewtwo") => PokemonMusicTrack.LegendaryBattle,
			var s when s.Contains("route") => PokemonMusicTrack.RouteOverworld,
			var s when s.Contains("victory") => PokemonMusicTrack.VictoryFanfare,
			_ => PokemonMusicTrack.WildBattle
		};
		PlayTrack(track, loop);
	}

	public static void PlayTrack(PokemonMusicTrack track, bool loop, bool pauseStardew = true)
	{
		try
		{
			if (pauseStardew)
			{
				PauseStardewMusic();
			}
			StopActivePokemonTrackOnly();

			float volMult = (track == PokemonMusicTrack.RouteOverworld) ? 0.28f : 0.42f;
			float vol = Math.Clamp(Game1.options?.musicVolumeLevel ?? 0.7f, 0f, 1f) * volMult;
			if (vol <= 0.005f)
			{
				_currentTrack = track;
				_currentTrackLooped = loop;
				return;
			}

			if (!_cachedEffects.TryGetValue(track, out var sfx) || sfx == null || sfx.IsDisposed)
			{
				sfx = BuildTrackSoundEffect(track);
				_cachedEffects[track] = sfx;
			}

			if (sfx != null)
			{
				_activeInstance = sfx.CreateInstance();
				_activeInstance.Volume = vol;
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
		// Each note is (leadFreqHz, bassFreqHz, durationSeconds)
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
					// GBA 25% pulse wave + soft triangle harmonic
					float pulse = (leadPhase < 0.25) ? 0.55f : -0.25f;
					float tri = (float)(Math.Sin(leadPhase * Math.PI * 2.0) * 0.35);
					leadSample = (pulse + tri) * env;
				}

				float bassSample = 0f;
				if (n.bass > 20f)
				{
					bassPhase += n.bass / sampleRate;
					if (bassPhase >= 1.0) bassPhase -= 1.0;
					// 50% square bass wave
					float sq = (bassPhase < 0.5) ? 0.45f : -0.45f;
					bassSample = sq * Math.Min(1f, env * 1.1f);
				}

				float mixed = Math.Clamp((leadSample * 0.65f + bassSample * 0.35f) * 0.65f, -0.95f, 0.95f);
				short s16 = (short)(mixed * short.MaxValue);
				pcm16[sampleIdx * 2] = (byte)(s16 & 0xFF);
				pcm16[sampleIdx * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
				sampleIdx++;
			}
		}

		return new SoundEffect(pcm16, sampleRate, AudioChannels.Mono);
	}

	private static List<(float lead, float bass, float dur)> BuildWildBattleSequence()
	{
		// Authentic Pokémon Emerald (Hoenn) Wild Battle Theme with classic trumpet hooks & syncopated bassline
		var s = new List<(float, float, float)>();

		// Intro: Rapid Hoenn chromatic trumpet fanfare
		float[] introNotes = { 587.33f, 554.37f, 523.25f, 493.88f, 466.16f, 440.00f, 415.30f, 392.00f, 440.00f, 493.88f, 523.25f, 587.33f, 659.25f, 698.46f, 783.99f, 880.00f };
		foreach (float f in introNotes)
		{
			s.Add((f, 146.83f, 0.065f));
		}

		// Section 1: Main Driving Trumpet Hook (D minor / F major)
		(float l, float b, float d)[] sec1 =
		{
			(587.33f, 146.83f, 0.14f), (587.33f, 146.83f, 0.14f), (698.46f, 174.61f, 0.14f), (587.33f, 146.83f, 0.14f),
			(523.25f, 130.81f, 0.18f), (587.33f, 146.83f, 0.18f), (659.25f, 164.81f, 0.18f), (698.46f, 174.61f, 0.18f),
			(783.99f, 196.00f, 0.28f), (698.46f, 174.61f, 0.14f), (659.25f, 164.81f, 0.18f), (523.25f, 130.81f, 0.18f),
			(587.33f, 146.83f, 0.28f), (440.00f, 110.00f, 0.14f), (523.25f, 130.81f, 0.14f), (587.33f, 146.83f, 0.24f),
			(659.25f, 164.81f, 0.14f), (698.46f, 174.61f, 0.14f), (783.99f, 196.00f, 0.20f), (880.00f, 220.00f, 0.20f),
			(783.99f, 196.00f, 0.20f), (698.46f, 174.61f, 0.20f), (659.25f, 164.81f, 0.28f), (587.33f, 146.83f, 0.28f)
		};
		s.AddRange(sec1);

		// Section 2: Hoenn Emerald Counter-melody with walking syncopated bass
		(float l, float b, float d)[] sec2 =
		{
			(698.46f, 174.61f, 0.16f), (783.99f, 196.00f, 0.16f), (880.00f, 220.00f, 0.24f), (932.33f, 233.08f, 0.24f),
			(880.00f, 220.00f, 0.16f), (783.99f, 196.00f, 0.16f), (698.46f, 174.61f, 0.20f), (659.25f, 164.81f, 0.20f),
			(587.33f, 146.83f, 0.24f), (554.37f, 138.59f, 0.16f), (587.33f, 146.83f, 0.20f), (659.25f, 164.81f, 0.20f),
			(698.46f, 174.61f, 0.28f), (783.99f, 196.00f, 0.16f), (880.00f, 220.00f, 0.24f), (1046.50f, 261.63f, 0.32f),
			(932.33f, 233.08f, 0.18f), (880.00f, 220.00f, 0.18f), (783.99f, 196.00f, 0.22f), (698.46f, 174.61f, 0.22f),
			(659.25f, 164.81f, 0.24f), (587.33f, 146.83f, 0.36f)
		};
		s.AddRange(sec2);

		// Section 3: Climax Brass Arpeggio & Loop Turnaround
		(float l, float b, float d)[] sec3 =
		{
			(880.00f, 220.00f, 0.14f), (783.99f, 196.00f, 0.14f), (698.46f, 174.61f, 0.18f), (659.25f, 164.81f, 0.18f),
			(587.33f, 146.83f, 0.22f), (698.46f, 174.61f, 0.18f), (659.25f, 164.81f, 0.18f), (587.33f, 146.83f, 0.22f),
			(523.25f, 130.81f, 0.18f), (466.16f, 116.54f, 0.18f), (440.00f, 110.00f, 0.28f), (587.33f, 146.83f, 0.36f)
		};
		s.AddRange(sec3);

		return s;
	}

	private static List<(float lead, float bass, float dur)> BuildTrainerBattleSequence()
	{
		// Authentic Pokémon Emerald Trainer Battle Theme (Intense brass triplets & driving baseline)
		var s = new List<(float, float, float)>();

		// Intro: Emerald Trainer battle alert cadence
		float[] intro = { 659.25f, 622.25f, 659.25f, 739.99f, 659.25f, 587.33f, 493.88f, 587.33f };
		foreach (float f in intro) s.Add((f, 164.81f, 0.10f));

		(float l, float b, float d)[] body =
		{
			(659.25f, 164.81f, 0.16f), (739.99f, 185.00f, 0.16f), (783.99f, 196.00f, 0.20f), (880.00f, 220.00f, 0.26f),
			(783.99f, 196.00f, 0.16f), (739.99f, 185.00f, 0.16f), (659.25f, 164.81f, 0.24f), (493.88f, 123.47f, 0.18f),
			(523.25f, 130.81f, 0.18f), (587.33f, 146.83f, 0.18f), (659.25f, 164.81f, 0.26f), (587.33f, 146.83f, 0.18f),
			(523.25f, 130.81f, 0.18f), (493.88f, 123.47f, 0.30f),
			// Secondary development
			(783.99f, 196.00f, 0.16f), (880.00f, 220.00f, 0.16f), (987.77f, 246.94f, 0.24f), (1046.50f, 261.63f, 0.28f),
			(987.77f, 246.94f, 0.16f), (880.00f, 220.00f, 0.16f), (783.99f, 196.00f, 0.24f), (659.25f, 164.81f, 0.20f),
			(739.99f, 185.00f, 0.20f), (783.99f, 196.00f, 0.24f), (880.00f, 220.00f, 0.32f), (659.25f, 164.81f, 0.36f)
		};
		s.AddRange(body);
		return s;
	}

	private static List<(float lead, float bass, float dur)> BuildMegaBossSequence()
	{
		// Intense Gym Leader / Mega Evolution Boss Battle theme
		return new List<(float, float, float)>
		{
			(440.00f, 110.00f, 0.14f), (523.25f, 130.81f, 0.14f), (659.25f, 164.81f, 0.14f), (880.00f, 220.00f, 0.28f),
			(830.61f, 207.65f, 0.18f), (880.00f, 220.00f, 0.18f), (987.77f, 246.94f, 0.28f), (880.00f, 220.00f, 0.22f),
			(783.99f, 196.00f, 0.22f), (698.46f, 174.61f, 0.22f), (659.25f, 164.81f, 0.32f), (523.25f, 130.81f, 0.22f),
			(587.33f, 146.83f, 0.22f), (659.25f, 164.81f, 0.22f), (698.46f, 174.61f, 0.22f), (783.99f, 196.00f, 0.32f),
			(880.00f, 220.00f, 0.36f), (659.25f, 164.81f, 0.28f)
		};
	}

	private static List<(float lead, float bass, float dur)> BuildLegendarySequence()
	{
		// Dramatic Legendary Pokémon Encounter theme
		return new List<(float, float, float)>
		{
			(329.63f, 82.41f, 0.16f), (349.23f, 87.31f, 0.16f), (329.63f, 82.41f, 0.16f), (493.88f, 123.47f, 0.32f),
			(466.16f, 116.54f, 0.24f), (440.00f, 110.00f, 0.24f), (392.00f, 98.00f, 0.24f), (349.23f, 87.31f, 0.24f),
			(329.63f, 82.41f, 0.36f), (659.25f, 164.81f, 0.20f), (698.46f, 174.61f, 0.20f), (783.99f, 196.00f, 0.32f),
			(739.99f, 185.00f, 0.24f), (659.25f, 164.81f, 0.36f)
		};
	}

	private static List<(float lead, float bass, float dur)> BuildVictorySequence()
	{
		// Classic Pokémon Victory Fanfare (da-da-da-daaa!)
		return new List<(float, float, float)>
		{
			(523.25f, 130.81f, 0.13f), (523.25f, 130.81f, 0.13f), (523.25f, 130.81f, 0.13f), (523.25f, 261.63f, 0.42f),
			(415.30f, 207.65f, 0.36f), (466.16f, 233.08f, 0.36f), (523.25f, 261.63f, 0.24f), (466.16f, 233.08f, 0.14f),
			(523.25f, 261.63f, 0.70f)
		};
	}

	private static List<(float lead, float bass, float dur)> BuildRouteOverworldSequence()
	{
		// Cheerful, breezy GBA Route 101 / Littleroot-inspired overworld melody
		return new List<(float, float, float)>
		{
			(523.25f, 130.81f, 0.26f), (587.33f, 146.83f, 0.26f), (659.25f, 164.81f, 0.38f), (783.99f, 196.00f, 0.38f),
			(698.46f, 174.61f, 0.26f), (659.25f, 164.81f, 0.26f), (587.33f, 146.83f, 0.52f),
			(523.25f, 130.81f, 0.26f), (659.25f, 164.81f, 0.26f), (783.99f, 196.00f, 0.38f), (880.00f, 220.00f, 0.38f),
			(783.99f, 196.00f, 0.52f), (659.25f, 164.81f, 0.38f)
		};
	}
}
