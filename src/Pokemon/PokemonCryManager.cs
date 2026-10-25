using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace StardewValley.Pokemon;

/// <summary>
/// Gerencia a reprodução dos cries (gritos) autênticos de Pokémon.
/// Carrega os arquivos .wav nativos de GBA (pokeemerald/pokefirered) ou sintetiza
/// dinamicamente um cry procedural em chiptune nostálgico para espécies de Gen 4 a Gen 9.
/// </summary>
public static class PokemonCryManager
{
	private static readonly Dictionary<string, SoundEffect> _cryCache = new(StringComparer.OrdinalIgnoreCase);
	private static readonly object _lock = new();

	/// <summary>
	/// Normaliza o nome da espécie para o padrão de arquivo do pokeemerald/pokefirered.
	/// Exemplos: "Nidoran♀" -> "nidoran_f", "Ho-Oh" -> "ho_oh", "Farfetch'd" -> "farfetchd".
	/// </summary>
	public static string NormalizeSpeciesKey(string speciesName)
	{
		if (string.IsNullOrWhiteSpace(speciesName)) return "pikachu";

		string clean = speciesName.Trim().ToLowerInvariant();
		// Casos especiais conhecidos dos nomes de arquivo GBA
		clean = clean.Replace("nidoran♀", "nidoran_f")
					 .Replace("nidoran♂", "nidoran_m")
					 .Replace("nidoran-f", "nidoran_f")
					 .Replace("nidoran-m", "nidoran_m")
					 .Replace("mr. mime", "mr_mime")
					 .Replace("mr.mime", "mr_mime")
					 .Replace("mr-mime", "mr_mime")
					 .Replace("mime jr.", "mime_jr")
					 .Replace("mime-jr", "mime_jr")
					 .Replace("ho-oh", "ho_oh")
					 .Replace("porygon-z", "porygon_z")
					 .Replace("jangmo-o", "jangmo_o")
					 .Replace("hakamo-o", "hakamo_o")
					 .Replace("kommo-o", "kommo_o")
					 .Replace("tapu koko", "tapu_koko")
					 .Replace("tapu lele", "tapu_lele")
					 .Replace("tapu bulu", "tapu_bulu")
					 .Replace("tapu fini", "tapu_fini")
					 .Replace("type: null", "type_null")
					 .Replace("type:null", "type_null")
					 .Replace("type-null", "type_null")
					 .Replace("wo-chien", "wo_chien")
					 .Replace("chien-pao", "chien_pao")
					 .Replace("ting-lu", "ting_lu")
					 .Replace("chi-yu", "chi_yu")
					 .Replace("roaring moon", "roaring_moon")
					 .Replace("iron valiant", "iron_valiant")
					 .Replace("walking wake", "walking_wake")
					 .Replace("iron leaves", "iron_leaves")
					 .Replace("gouging fire", "gouging_fire")
					 .Replace("raging bolt", "raging_bolt")
					 .Replace("iron boulder", "iron_boulder")
					 .Replace("iron crown", "iron_crown")
					 .Replace("great tusk", "great_tusk")
					 .Replace("scream tail", "scream_tail")
					 .Replace("brute bonnet", "brute_bonnet")
					 .Replace("flutter mane", "flutter_mane")
					 .Replace("slither wing", "slither_wing")
					 .Replace("sandy shocks", "sandy_shocks")
					 .Replace("iron treads", "iron_treads")
					 .Replace("iron bundle", "iron_bundle")
					 .Replace("iron hands", "iron_hands")
					 .Replace("iron jugulis", "iron_jugulis")
					 .Replace("iron moth", "iron_moth")
					 .Replace("iron thorns", "iron_thorns");

		// Remove pontuações e caracteres inválidos
		var sb = new StringBuilder();
		foreach (char c in clean)
		{
			if (char.IsLetterOrDigit(c) || c == '_')
			{
				sb.Append(c);
			}
			else if (c == ' ' || c == '-')
			{
				sb.Append('_');
			}
		}
		return sb.ToString();
	}

	/// <summary>
	/// Procura o caminho do arquivo .wav nativo da ROM pokeemerald ou pokefirered.
	/// </summary>
	public static string ResolveNativeWavPath(string speciesName)
	{
		string key = NormalizeSpeciesKey(speciesName);
		try
		{
			// 1. Busca via PokemonUI.FindInProject
			string p1 = PokemonUI.FindInProject("pokeemerald", "sound", "direct_sound_samples", "cries", key + ".wav");
			if (!string.IsNullOrEmpty(p1) && File.Exists(p1)) return p1;

			string p2 = PokemonUI.FindInProject("pokefirered", "sound", "direct_sound_samples", "cries", key + ".wav");
			if (!string.IsNullOrEmpty(p2) && File.Exists(p2)) return p2;

			// 2. Caminho absoluto direto como fallback
			string direct1 = Path.Combine(@"D:\Projetos\Nova pasta\pokeemerald\sound\direct_sound_samples\cries", key + ".wav");
			if (File.Exists(direct1)) return direct1;

			string direct2 = Path.Combine(@"D:\Projetos\Nova pasta\pokefirered\sound\direct_sound_samples\cries", key + ".wav");
			if (File.Exists(direct2)) return direct2;
		}
		catch { }
		return null;
	}

	/// <summary>
	/// Verifica se a espécie possui arquivo de cry nativo .wav.
	/// </summary>
	public static bool HasNativeCry(string speciesName)
	{
		return !string.IsNullOrEmpty(ResolveNativeWavPath(speciesName));
	}

	/// <summary>
	/// Retorna uma descrição da origem do áudio para exibição no menu de debug.
	/// </summary>
	public static string GetCryStatus(string speciesName)
	{
		if (HasNativeCry(speciesName))
		{
			return "WAV Original GBA (pokeemerald)";
		}
		return "Síntese Chiptune Procedural (Gen 4-9)";
	}

	/// <summary>
	/// Obtém ou gera o SoundEffect do cry da espécie especificada.
	/// </summary>
	public static SoundEffect GetCrySoundEffect(string speciesName)
	{
		string key = NormalizeSpeciesKey(speciesName);
		lock (_lock)
		{
			if (_cryCache.TryGetValue(key, out var cached) && cached != null && !cached.IsDisposed)
			{
				return cached;
			}

			// Tenta carregar WAV nativo
			string wavPath = ResolveNativeWavPath(speciesName);
			if (!string.IsNullOrEmpty(wavPath) && File.Exists(wavPath))
			{
				try
				{
					using var stream = File.OpenRead(wavPath);
					var sfx = SoundEffect.FromStream(stream);
					if (sfx != null)
					{
						_cryCache[key] = sfx;
						return sfx;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[PokemonCryManager] Erro ao carregar WAV '{wavPath}': {ex.Message}");
				}
			}

			// Se não existe arquivo .wav nativo, sintetiza cry procedural com base no dex number
			try
			{
				var synth = GenerateProceduralCry(speciesName);
				if (synth != null)
				{
					_cryCache[key] = synth;
					return synth;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[PokemonCryManager] Falha ao sintetizar cry procedural para '{speciesName}': {ex.Message}");
			}

			return null;
		}
	}

	/// <summary>
	/// Executa o cry da espécie com controle de volume e pitch.
	/// </summary>
	public static bool PlayCry(string speciesName, float volumeMult = 1.0f, float pitch = 0.0f)
	{
		try
		{
			var sfx = GetCrySoundEffect(speciesName);
			if (sfx != null)
			{
				float baseVol = Game1.options?.soundVolumeLevel ?? 0.7f;
				float finalVol = Math.Clamp(baseVol * volumeMult, 0f, 1f);
				if (finalVol > 0.01f)
				{
					sfx.Play(finalVol, Math.Clamp(pitch, -1f, 1f), 0f);
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PokemonCryManager] Erro ao tocar cry de '{speciesName}': {ex.Message}");
		}
		return false;
	}

	/// <summary>
	/// Sintetiza um cry em chiptune nostálgico no formato WAV 16-bit mono 22050Hz.
	/// Usa ondas quadradas com modulação de frequência e envelope característico de Pokémon.
	/// </summary>
	private static SoundEffect GenerateProceduralCry(string speciesName)
	{
		var entry = PokemonPokedex.Instance.GetEntry(speciesName);
		int dex = entry?.Number ?? Math.Abs(speciesName.GetHashCode() % 1000 + 1);

		int sampleRate = 22050;
		float duration = 0.40f + (dex % 5) * 0.04f; // 0.40s a 0.60s
		int sampleCount = (int)(sampleRate * duration);

		// Parâmetros de frequência baseados no dex number
		float baseFreq = 220f + (dex % 350) * 2.2f; // Entre 220Hz e 990Hz
		float targetFreq = baseFreq * (0.6f + ((dex * 7) % 100) / 100f);
		float vibratoSpeed = 15f + (dex % 10);
		float vibratoDepth = 25f + (dex % 30);

		short[] samples = new short[sampleCount];
		double phase = 0.0;
		double subPhase = 0.0;
		var rand = new Random(dex * 31);

		for (int i = 0; i < sampleCount; i++)
		{
			float progress = (float)i / sampleCount;

			// Envelope de volume: Ataque rápido (5%), sustentação, decaimento suave (30%)
			float env;
			if (progress < 0.06f)
				env = progress / 0.06f;
			else if (progress > 0.65f)
				env = Math.Max(0f, 1f - ((progress - 0.65f) / 0.35f));
			else
				env = 1.0f;

			// Modulação de frequência (slide + vibrato)
			float currentFreq = MathHelper.Lerp(baseFreq, targetFreq, (float)Math.Sin(progress * Math.PI));
			currentFreq += (float)Math.Sin(progress * vibratoSpeed * Math.PI * 2.0) * vibratoDepth;

			phase += (currentFreq / sampleRate) * Math.PI * 2.0;
			subPhase += ((currentFreq * 0.5f) / sampleRate) * Math.PI * 2.0;

			// Onda quadrada principal com largura de pulso variável (50% a 25%)
			float pulseWidth = 0.35f + (float)Math.Sin(progress * 10f) * 0.15f;
			double primaryWave = (Math.Sin(phase) > Math.Sin(pulseWidth * Math.PI * 2.0)) ? 1.0 : -1.0;

			// Sub-onda para peso harmônico
			double subWave = (Math.Sin(subPhase) > 0.0) ? 0.4 : -0.4;

			// Ruído de atrito para ataque percussivo nos primeiros 15%
			double noise = 0.0;
			if (progress < 0.15f)
			{
				float noiseEnv = (1f - progress / 0.15f) * 0.35f;
				noise = (rand.NextDouble() * 2.0 - 1.0) * noiseEnv;
			}

			double combined = (primaryWave * 0.65 + subWave * 0.25 + noise) * env;
			samples[i] = (short)Math.Clamp(combined * 18000, short.MinValue, short.MaxValue);
		}

		// Empacota em stream WAV com cabeçalho RIFF canônico
		byte[] wavBytes = BuildWavFile(samples, sampleRate);
		using var ms = new MemoryStream(wavBytes);
		return SoundEffect.FromStream(ms);
	}

	/// <summary>
	/// Constrói um cabeçalho RIFF/WAVE de 44 bytes para áudio PCM 16-bit Mono.
	/// </summary>
	private static byte[] BuildWavFile(short[] samples, int sampleRate)
	{
		int subChunk2Size = samples.Length * sizeof(short);
		int chunkSize = 36 + subChunk2Size;

		using var ms = new MemoryStream(44 + subChunk2Size);
		using var writer = new BinaryWriter(ms);

		// RIFF chunk
		writer.Write(Encoding.ASCII.GetBytes("RIFF"));
		writer.Write(chunkSize);
		writer.Write(Encoding.ASCII.GetBytes("WAVE"));

		// fmt subchunk
		writer.Write(Encoding.ASCII.GetBytes("fmt "));
		writer.Write(16); // Subchunk1Size (16 para PCM)
		writer.Write((short)1); // AudioFormat (1 = PCM)
		writer.Write((short)1); // NumChannels (1 = Mono)
		writer.Write(sampleRate);
		writer.Write(sampleRate * 1 * sizeof(short)); // ByteRate
		writer.Write((short)(1 * sizeof(short))); // BlockAlign
		writer.Write((short)16); // BitsPerSample

		// data subchunk
		writer.Write(Encoding.ASCII.GetBytes("data"));
		writer.Write(subChunk2Size);

		// PCM data
		for (int i = 0; i < samples.Length; i++)
		{
			writer.Write(samples[i]);
		}

		return ms.ToArray();
	}
}
