using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using ContentManifest;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StardewValley.GameData;

namespace StardewValley;

/// <summary>Loads assets and translations from the game's content folder.</summary>
/// <summary>Loads assets and translations from the game's content folder.</summary>
public class LocalizedContentManager : ContentManager
{
	public delegate void LanguageChangedHandler(LanguageCode code);

	/// <summary>A language supported by the game.</summary>
	public enum LanguageCode
	{
		/// <summary>The English language.</summary>
		en,
		/// <summary>The Japanese language.</summary>
		ja,
		/// <summary>The Russian language.</summary>
		ru,
		/// <summary>The Chinese language.</summary>
		zh,
		/// <summary>The Portuguese language.</summary>
		pt,
		/// <summary>The Spanish language.</summary>
		es,
		/// <summary>The German language.</summary>
		de,
		/// <summary>The Thai language.</summary>
		th,
		/// <summary>The French language.</summary>
		fr,
		/// <summary>The Korean language.</summary>
		ko,
		/// <summary>The Italian language.</summary>
		it,
		/// <summary>The Turkish language.</summary>
		tr,
		/// <summary>The Hungarian language.</summary>
		hu,
		/// <summary>A custom language added by a mod.</summary>
		mod
	}

	private const bool OnlyCheckManifest = true;

	private static readonly object ManifestLocker = new object();

	private static HashSet<string> _manifest = null;

	public static readonly Dictionary<string, string> localizedAssetNames = new Dictionary<string, string>();

	/// <summary>The backing field for <see cref="M:StardewValley.LocalizedContentManager.GetContentRoot" />.</summary>
	protected string _CachedContentRoot;

	/// <summary>The backing field for <see cref="P:StardewValley.LocalizedContentManager.CurrentLanguageCode" />.</summary>
	private static LanguageCode _currentLangCode = GetDefaultLanguageCode();

	/// <summary>The backing field for <see cref="P:StardewValley.LocalizedContentManager.CurrentLanguageString" />.</summary>
	private static string _currentLangString = null;

	private static ModLanguage _currentModLanguage = null;

	public CultureInfo CurrentCulture;

	protected static StringBuilder _timeFormatStringBuilder = new StringBuilder();

	/// <summary>The current language as a string which appears in localized asset names (like <c>pt-BR</c>).</summary>
	public static string CurrentLanguageString
	{
		get
		{
			if (_currentLangString == null)
			{
				_currentLangString = LanguageCodeString(CurrentLanguageCode);
			}
			return _currentLangString;
		}
	}

	/// <summary>The current language as an enum.</summary>
	/// <remarks>Note that <see cref="F:StardewValley.LocalizedContentManager.LanguageCode.mod" /> is used for any custom language, so you'll need to use <see cref="P:StardewValley.LocalizedContentManager.CurrentLanguageString" /> to distinguish those.</remarks>
	public static LanguageCode CurrentLanguageCode
	{
		get
		{
			return _currentLangCode;
		}
		set
		{
			if (_currentLangCode != value)
			{
				LanguageCode currentLangCode = _currentLangCode;
				_currentLangCode = value;
				_currentLangString = null;
				if (_currentLangCode != LanguageCode.mod)
				{
					_currentModLanguage = null;
				}
				Game1.log.Verbose("LocalizedContentManager.CurrentLanguageCode CHANGING from '" + currentLangCode.ToString() + "' to '" + _currentLangCode.ToString() + "'");
				LocalizedContentManager.OnLanguageChange?.Invoke(_currentLangCode);
				Game1.log.Verbose("LocalizedContentManager.CurrentLanguageCode CHANGED from '" + currentLangCode.ToString() + "' to '" + _currentLangCode.ToString() + "'");
			}
		}
	}

	public static bool CurrentLanguageLatin
	{
		get
		{
			if (CurrentLanguageCode != LanguageCode.en && CurrentLanguageCode != LanguageCode.es && CurrentLanguageCode != LanguageCode.de && CurrentLanguageCode != LanguageCode.pt && CurrentLanguageCode != LanguageCode.fr && CurrentLanguageCode != LanguageCode.it && CurrentLanguageCode != LanguageCode.tr && CurrentLanguageCode != LanguageCode.hu)
			{
				if (CurrentLanguageCode == LanguageCode.mod)
				{
					return _currentModLanguage.UseLatinFont;
				}
				return false;
			}
			return true;
		}
	}

	public static ModLanguage CurrentModLanguage => _currentModLanguage;

	public static event LanguageChangedHandler OnLanguageChange;

	private void PlatformEnsureManifestInitialized()
	{
		if (_manifest != null)
		{
			return;
		}
		_manifest = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = Path.Combine(GetContentRoot(), "ContentHashes.json");
		if (File.Exists(text))
		{
			Dictionary<string, object> dictionary = null;
			try
			{
				dictionary = ContentHashParser.ParseFromFile(text);
			}
			catch (Exception exception)
			{
				Game1.log.Error("Error parsing ContentHashes.json:", exception);
			}
			if (dictionary == null || dictionary.Count == 0)
			{
				Game1.log.Warn("Parsing ContentHashes.json resulted in a null or empty dictionary.");
				return;
			}
			Game1.log.Verbose($"Successfully loaded ContentHashes.json containing {dictionary.Count} file(s);");
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				foreach (string key in dictionary.Keys)
				{
					_manifest.Add(key.Replace('/', '\\'));
				}
				return;
			}
			_manifest.UnionWith(dictionary.Keys);
		}
		else
		{
			Game1.log.Warn("Could not find ContentHashes at path '" + text + "'");
		}
	}

	private void EnsureManifestInitialized()
	{
		if (_manifest != null)
		{
			return;
		}
		lock (ManifestLocker)
		{
			if (_manifest == null)
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				PlatformEnsureManifestInitialized();
				stopwatch.Stop();
				Game1.log.Verbose($"EnsureManifestInitialized() finished, elapsed = '{stopwatch.Elapsed}'");
			}
		}
	}

	public static LanguageCode GetDefaultLanguageCode()
	{
		return LanguageCode.en;
	}

	public LocalizedContentManager(IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture)
		: base(serviceProvider, rootDirectory)
	{
		CurrentCulture = currentCulture;
	}

	public LocalizedContentManager(IServiceProvider serviceProvider, string rootDirectory)
		: this(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture)
	{
	}

	protected static bool _IsStringAt(string source, string string_to_find, int index)
	{
		for (int i = 0; i < string_to_find.Length; i++)
		{
			int num = index + i;
			if (num >= source.Length)
			{
				return false;
			}
			if (source[num] != string_to_find[i])
			{
				return false;
			}
		}
		return true;
	}

	public static StringBuilder FormatTimeString(int time, string format)
	{
		_timeFormatStringBuilder.Clear();
		int num = -1;
		for (int i = 0; i < format.Length; i++)
		{
			char c = format[i];
			switch (c)
			{
			case '[':
			{
				if (num < 0)
				{
					num = i;
					continue;
				}
				for (int k = num; k <= i; k++)
				{
					_timeFormatStringBuilder.Append(format[k]);
				}
				num = i;
				continue;
			}
			case ']':
				if (num < 0)
				{
					break;
				}
				if (_IsStringAt(format, "[HOURS_12]", num))
				{
					_timeFormatStringBuilder.Append((time / 100 % 12 == 0) ? "12" : (time / 100 % 12).ToString());
				}
				else if (_IsStringAt(format, "[HOURS_12_0]", num))
				{
					_timeFormatStringBuilder.Append((time / 100 % 12 == 0) ? "0" : (time / 100 % 12).ToString());
				}
				else if (_IsStringAt(format, "[HOURS_24]", num))
				{
					_timeFormatStringBuilder.Append(time / 100 % 24);
				}
				else if (_IsStringAt(format, "[HOURS_24_00]", num))
				{
					_timeFormatStringBuilder.Append((time / 100 % 24).ToString("00"));
				}
				else if (_IsStringAt(format, "[MINUTES]", num))
				{
					_timeFormatStringBuilder.Append((time % 100).ToString("00"));
				}
				else if (_IsStringAt(format, "[AM_PM]", num))
				{
					if (time < 1200 || time >= 2400)
					{
						_timeFormatStringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10370"));
					}
					else
					{
						_timeFormatStringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10371"));
					}
				}
				else
				{
					for (int j = num; j <= i; j++)
					{
						_timeFormatStringBuilder.Append(format[j]);
					}
				}
				num = -1;
				continue;
			}
			if (num < 0)
			{
				_timeFormatStringBuilder.Append(c);
			}
		}
		return _timeFormatStringBuilder;
	}

	public static void SetModLanguage(ModLanguage new_mod_language)
	{
		if (new_mod_language != _currentModLanguage)
		{
			_currentModLanguage = new_mod_language;
			CurrentLanguageCode = LanguageCode.mod;
		}
	}

	/// <summary>Get the absolute path to the root content directory from which this manager loads assets.</summary>
	public virtual string GetContentRoot()
	{
		if (_CachedContentRoot == null)
		{
			string path = ((string)(typeof(TitleContainer).GetProperty("Location", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException("Can't get TitleContainer.Location property from MonoGame")).GetValue(null, null)) ?? throw new InvalidOperationException("Can't get value of TitleContainer.Location property from MonoGame");
			_CachedContentRoot = Path.Combine(path, base.RootDirectory);
		}
		return _CachedContentRoot;
	}

	/// <summary>Get whether an asset exists without loading it.</summary>
	/// <typeparam name="T">The expected asset type.</typeparam>
	/// <param name="assetName">The asset name to check.</param>
	public virtual bool DoesAssetExist<T>(string assetName)
	{
		if (assetName == null)
		{
			return false;
		}
		bool flag = false;
		char c = ((Environment.OSVersion.Platform == PlatformID.Win32NT) ? '\\' : '/');
		StringBuilder stringBuilder = new StringBuilder(assetName.Length + 4);
		for (int i = 0; i < assetName.Length; i++)
		{
			char c2 = assetName[i];
			if (c2 == '/' || c2 == '\\')
			{
				if (flag)
				{
					continue;
				}
				c2 = c;
				flag = true;
			}
			else
			{
				flag = false;
			}
			stringBuilder.Append(c2);
		}
		stringBuilder.Append(".xnb");
		string item = stringBuilder.ToString();
		EnsureManifestInitialized();
		return _manifest.Contains(item);
	}

	/// <summary>Load an asset through the content pipeline.</summary>
	/// <typeparam name="T">The type of asset to load.</typeparam>
	/// <param name="baseAssetName">The unlocalized asset name relative to the game's root directory.</param>
	/// <param name="localizedAssetName">The localized asset name relative to the game's root directory.</param>
	/// <param name="languageCode">The language for which to load the asset.</param>
	public virtual T LoadImpl<T>(string baseAssetName, string localizedAssetName, LanguageCode languageCode)
	{
		if (!DoesAssetExist<T>(localizedAssetName))
		{
			throw new ContentLoadException("Could not load " + localizedAssetName + " asset!");
		}
		return base.Load<T>(localizedAssetName);
	}

	/// <summary>Load an asset through the content pipeline.</summary>
	/// <typeparam name="T">The type of asset to load.</typeparam>
	/// <param name="assetName">The unlocalized asset name relative to the game's root directory.</param>
	public override T Load<T>(string assetName)
	{
		return Load<T>(assetName, CurrentLanguageCode);
	}

	/// <summary>Load an asset through the content pipeline.</summary>
	/// <typeparam name="T">The type of asset to load.</typeparam>
	/// <param name="assetName">The unlocalized asset name relative to the game's root directory.</param>
	/// <param name="language">The language for which to load the asset.</param>
	public virtual T Load<T>(string assetName, LanguageCode language)
	{
		if (language != LanguageCode.en)
		{
			if (!localizedAssetNames.TryGetValue(assetName, out var _))
			{
				bool flag = false;
				string text = assetName + "." + ((language == CurrentLanguageCode) ? CurrentLanguageString : LanguageCodeString(language));
				if (!DoesAssetExist<T>(text))
				{
					flag = true;
				}
				if (!flag)
				{
					try
					{
						LoadImpl<T>(assetName, text, language);
						localizedAssetNames[assetName] = text;
					}
					catch (ContentLoadException)
					{
						flag = true;
					}
				}
				if (flag)
				{
					flag = false;
					text = assetName + "_international";
					if (!DoesAssetExist<T>(text))
					{
						flag = true;
					}
					if (!flag)
					{
						try
						{
							LoadImpl<T>(assetName, text, language);
							localizedAssetNames[assetName] = text;
						}
						catch (ContentLoadException)
						{
							flag = true;
						}
					}
					if (flag)
					{
						localizedAssetNames[assetName] = assetName;
					}
				}
			}
			return LoadImpl<T>(assetName, localizedAssetNames[assetName], language);
		}
		return LoadImpl<T>(assetName, assetName, LanguageCode.en);
	}

	/// <summary>Get the language string which appears in localized asset names for a language (like <c>pt-BR</c>).</summary>
	/// <param name="code">The language whose asset name code to get.</param>
	/// <remarks>For the current language, see <see cref="P:StardewValley.LocalizedContentManager.CurrentLanguageString" /> instead.</remarks>
	public static string LanguageCodeString(LanguageCode code)
	{
		return code switch
		{
			LanguageCode.ja => "ja-JP", 
			LanguageCode.ru => "ru-RU", 
			LanguageCode.zh => "zh-CN", 
			LanguageCode.pt => "pt-BR", 
			LanguageCode.es => "es-ES", 
			LanguageCode.de => "de-DE", 
			LanguageCode.th => "th-TH", 
			LanguageCode.fr => "fr-FR", 
			LanguageCode.ko => "ko-KR", 
			LanguageCode.it => "it-IT", 
			LanguageCode.tr => "tr-TR", 
			LanguageCode.hu => "hu-HU", 
			LanguageCode.mod => (_currentModLanguage ?? throw new InvalidOperationException("The game language is set to a custom one, but the language info is no longer available.")).LanguageCode, 
			_ => "", 
		};
	}

	/// <summary>Get the current language as an enum.</summary>
	public LanguageCode GetCurrentLanguage()
	{
		return CurrentLanguageCode;
	}

	/// <summary>Read a translation key from a loaded strings asset.</summary>
	/// <param name="strings">The loaded strings asset.</param>
	/// <param name="key">The translation key to load.</param>
	/// <returns>Returns the matching string, or <c>null</c> if it wasn't found.</returns>
	private string GetString(Dictionary<string, string> strings, string key)
	{
		if (strings == null)
		{
			return null;
		}
		if (strings.TryGetValue(key + ".desktop", out var value))
		{
			return value;
		}
		if (!strings.TryGetValue(key, out value))
		{
			return null;
		}
		return value;
	}

	/// <summary>Get whether a string is a valid translation key which can be loaded by methods like <see cref="M:StardewValley.LocalizedContentManager.LoadString(System.String)" />.</summary>
	/// <param name="path">The potential translation key to check.</param>
	public virtual bool IsValidTranslationKey(string path)
	{
		try
		{
			return LoadString(path) != path;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Get translation text from a data asset, if found.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="localeFallback">Whether to get the English text if the translation isn't defined for the current language.</param>
	/// <returns>Returns the loaded string if found, else <c>null</c>.</returns>
	public virtual string LoadStringReturnNullIfNotFound(string path, bool localeFallback = true)
	{
		parseStringPath(path, out var assetName, out var key);
		Dictionary<string, string> strings = Load<Dictionary<string, string>>(assetName);
		string text = GetString(strings, key) ?? (localeFallback ? LoadBaseStringOrNull(path) : null);
		return PreprocessString(text);
	}

	/// <summary>Get translation text from a data asset, if found.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="sub1">The value with which to replace the <c>{0}</c> placeholder in the loaded text.</param>
	/// <param name="localeFallback">Whether to get the English text if the translation isn't defined for the current language.</param>
	/// <returns>Returns the loaded string if found, else <c>null</c>.</returns>
	public virtual string LoadStringReturnNullIfNotFound(string path, string sub1, bool localeFallback = true)
	{
		string text = LoadStringReturnNullIfNotFound(path, localeFallback);
		if (text != null)
		{
			text = string.Format(text, sub1);
		}
		return text;
	}

	/// <summary>Get translation text from a data asset, if found.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="sub1">The value with which to replace the <c>{0}</c> placeholder in the loaded text.</param>
	/// <param name="sub2">The value with which to replace the <c>{1}</c> placeholder in the loaded text.</param>
	/// <param name="localeFallback">Whether to get the English text if the translation isn't defined for the current language.</param>
	/// <returns>Returns the loaded string if found, else <c>null</c>.</returns>
	public virtual string LoadStringReturnNullIfNotFound(string path, string sub1, string sub2, bool localeFallback = true)
	{
		string text = LoadStringReturnNullIfNotFound(path, localeFallback);
		if (text != null)
		{
			text = string.Format(text, sub1, sub2);
		}
		return text;
	}

	/// <summary>Get translation text from a data asset, if found.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="sub1">The value with which to replace the <c>{0}</c> placeholder in the loaded text.</param>
	/// <param name="substitutions">The values with which to replace placeholders like <c>{0}</c> in the loaded text.</param>
	/// <param name="localeFallback">Whether to get the English text if the translation isn't defined for the current language.</param>
	/// <returns>Returns the loaded string if found, else <c>null</c>.</returns>
	public virtual string LoadStringReturnNullIfNotFound(string path, object[] substitutions, bool localeFallback = true)
	{
		string text = LoadStringReturnNullIfNotFound(path, localeFallback);
		if (text != null)
		{
			text = string.Format(text, substitutions);
		}
		return text;
	}

	/// <summary>Get translation text from a data asset.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <returns>Returns the loaded string if found, else the <paramref name="path" />.</returns>
	public virtual string LoadString(string path)
	{
		return LoadStringReturnNullIfNotFound(path) ?? path;
	}

	/// <summary>Apply generic preprocessing to strings loaded from <see cref="M:StardewValley.LocalizedContentManager.LoadString(System.String)" /> and its overloads.</summary>
	/// <param name="text">The text to preprocess.</param>
	public virtual string PreprocessString(string text)
	{
		if (text == null)
		{
			return null;
		}
		Gender gender = Game1.player?.Gender ?? Gender.Male;
		text = Dialogue.applyGenderSwitchBlocks(gender, text);
		text = Dialogue.applyGenderSwitch(gender, text, altTokenOnly: true);
		return text;
	}

	public virtual bool ShouldUseGenderedCharacterTranslations()
	{
		if (CurrentLanguageCode == LanguageCode.pt)
		{
			return true;
		}
		if (CurrentLanguageCode == LanguageCode.mod && CurrentModLanguage != null)
		{
			return CurrentModLanguage.UseGenderedCharacterTranslations;
		}
		return false;
	}

	/// <summary>Get translation text from a data asset.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="sub1">The value with which to replace the <c>{0}</c> placeholder in the loaded text.</param>
	/// <returns>Returns the loaded string if found, else the <paramref name="path" />.</returns>
	public virtual string LoadString(string path, object sub1)
	{
		string text = LoadString(path);
		try
		{
			return string.Format(text, sub1);
		}
		catch (Exception)
		{
			return text;
		}
	}

	/// <summary>Get translation text from a data asset.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="sub1">The value with which to replace the <c>{0}</c> placeholder in the loaded text.</param>
	/// <param name="sub2">The value with which to replace the <c>{1}</c> placeholder in the loaded text.</param>
	/// <returns>Returns the loaded string if found, else the <paramref name="path" />.</returns>
	public virtual string LoadString(string path, object sub1, object sub2)
	{
		string text = LoadString(path);
		try
		{
			return string.Format(text, sub1, sub2);
		}
		catch (Exception)
		{
			return text;
		}
	}

	/// <summary>Get translation text from a data asset.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="sub1">The value with which to replace the <c>{0}</c> placeholder in the loaded text.</param>
	/// <param name="sub2">The value with which to replace the <c>{1}</c> placeholder in the loaded text.</param>
	/// <param name="sub3">The value with which to replace the <c>{2}</c> placeholder in the loaded text.</param>
	/// <returns>Returns the loaded string if found, else the <paramref name="path" />.</returns>
	public virtual string LoadString(string path, object sub1, object sub2, object sub3)
	{
		string text = LoadString(path);
		try
		{
			return string.Format(text, sub1, sub2, sub3);
		}
		catch (Exception)
		{
			return text;
		}
	}

	/// <summary>Get translation text from a data asset.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	/// <param name="substitutions">The values with which to replace placeholders like <c>{0}</c> in the loaded text.</param>
	/// <returns>Returns the loaded string if found, else the <paramref name="path" />.</returns>
	public virtual string LoadString(string path, params object[] substitutions)
	{
		string text = LoadString(path);
		if (substitutions.Length != 0)
		{
			try
			{
				return string.Format(text, substitutions);
			}
			catch (Exception)
			{
			}
		}
		return text;
	}

	/// <summary>Get the default English text for a translation, or <c>null</c> if it's not found.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	public virtual string LoadBaseStringOrNull(string path)
	{
		parseStringPath(path, out var assetName, out var key);
		Dictionary<string, string> strings = LoadImpl<Dictionary<string, string>>(assetName, assetName, LanguageCode.en);
		return GetString(strings, key);
	}

	/// <summary>Get the default English text for a translation, or the input <paramref name="path" /> if it's not found.</summary>
	/// <param name="path">The translation from which to take the text, in the form <c>assetName:fieldKey</c> like <c>Strings/UI:Confirm</c>.</param>
	public virtual string LoadBaseString(string path)
	{
		return LoadBaseStringOrNull(path) ?? path;
	}

	private void parseStringPath(string path, out string assetName, out string key)
	{
		int num = path.IndexOf(':');
		if (num == -1)
		{
			throw new ContentLoadException("Unable to parse string path: " + path);
		}
		assetName = path.Substring(0, num);
		key = path.Substring(num + 1, path.Length - num - 1);
	}

	public virtual LocalizedContentManager CreateTemporary()
	{
		return new LocalizedContentManager(base.ServiceProvider, base.RootDirectory, CurrentCulture);
	}
}
