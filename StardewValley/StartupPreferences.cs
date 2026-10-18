using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using StardewValley.GameData;
using StardewValley.SaveSerialization;

namespace StardewValley;

public class StartupPreferences
{
	public const int windowed_borderless = 0;

	public const int windowed = 1;

	public const int fullscreen = 2;

	private static readonly string _filename = "startup_preferences";

	public static XmlSerializer serializer = null;

	public bool startMuted;

	public bool levelTenFishing;

	public bool levelTenMining;

	public bool levelTenForaging;

	public bool levelTenCombat;

	public bool skipWindowPreparation;

	public bool sawAdvancedCharacterCreationIndicator;

	public int timesPlayed;

	public int windowMode;

	public int displayIndex = -1;

	public Options.GamepadModes gamepadMode;

	public int playerLimit = -1;

	public int fullscreenResolutionX;

	public int fullscreenResolutionY;

	public string lastEnteredIP = "";

	public string languageCode;

	public Options clientOptions = new Options();

	[XmlIgnore]
	public bool isLoaded;

	private bool _isBusy;

	private bool _pendingApplyLanguage;

	private Task _task;

	[XmlIgnore]
	public bool IsBusy
	{
		get
		{
			lock (this)
			{
				if (!_isBusy)
				{
					return false;
				}
				if (_task == null)
				{
					throw new Exception("StartupPreferences.IsBusy; was busy but task is null?");
				}
				if (_task.IsFaulted)
				{
					Exception baseException = _task.Exception.GetBaseException();
					Game1.log.Error("StartupPreferences._task failed with an exception.", baseException);
					throw baseException;
				}
				if (_task.IsCompleted)
				{
					_task = null;
					_isBusy = false;
					if (_pendingApplyLanguage)
					{
						_SetLanguageFromCode(languageCode);
					}
				}
				return _isBusy;
			}
		}
	}

	private void Init()
	{
		isLoaded = false;
		ensureFolderStructureExists();
	}

	public void OnLanguageChange(LocalizedContentManager.LanguageCode code)
	{
		string text = code.ToString();
		if (code == LocalizedContentManager.LanguageCode.mod && LocalizedContentManager.CurrentModLanguage != null)
		{
			text = LocalizedContentManager.CurrentModLanguage.Id;
		}
		if (isLoaded && languageCode != text)
		{
			savePreferences(async: false, update_language_from_ingame_language: true);
		}
	}

	private void ensureFolderStructureExists()
	{
		Program.GetAppDataFolder();
	}

	public void savePreferences(bool async, bool update_language_from_ingame_language = false)
	{
		lock (this)
		{
			if (update_language_from_ingame_language)
			{
				if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.mod)
				{
					languageCode = LocalizedContentManager.CurrentModLanguage.Id;
				}
				else
				{
					languageCode = LocalizedContentManager.CurrentLanguageCode.ToString();
				}
			}
			try
			{
				_savePreferences();
			}
			catch (Exception ex)
			{
				Game1.log.Error("StartupPreferences._task failed with an exception.", ex);
				throw ex;
			}
		}
	}

	private void _savePreferences()
	{
		string path = Path.Combine(Program.GetAppDataFolder(), _filename);
		try
		{
			ensureFolderStructureExists();
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			using FileStream stream = File.Create(path);
			writeSettings(stream);
		}
		catch (Exception ex)
		{
			Game1.debugOutput = Game1.parseText(ex.Message);
		}
	}

	private long writeSettings(Stream stream)
	{
		XmlWriterSettings settings = new XmlWriterSettings
		{
			CloseOutput = true,
			Indent = true
		};
		using XmlWriter xmlWriter = XmlWriter.Create(stream, settings);
		xmlWriter.WriteStartDocument();
		serializer.SerializeFast(xmlWriter, this);
		xmlWriter.WriteEndDocument();
		xmlWriter.Flush();
		return stream.Length;
	}

	public void loadPreferences(bool async, bool applyLanguage)
	{
		lock (this)
		{
			_pendingApplyLanguage = applyLanguage;
			Init();
			try
			{
				_loadPreferences();
			}
			catch (Exception ex)
			{
				Exception ex2 = _task.Exception?.GetBaseException() ?? ex;
				Game1.log.Error("StartupPreferences._task failed with an exception.", ex2);
				throw ex2;
			}
			if (applyLanguage)
			{
				_SetLanguageFromCode(languageCode);
			}
		}
	}

	protected virtual void _SetLanguageFromCode(string language_code_string)
	{
		List<ModLanguage> list = DataLoader.AdditionalLanguages(Game1.content);
		bool flag = false;
		if (list != null)
		{
			foreach (ModLanguage item in list)
			{
				if (item.Id == language_code_string)
				{
					LocalizedContentManager.SetModLanguage(item);
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			if (Utility.TryParseEnum<LocalizedContentManager.LanguageCode>(language_code_string, out var parsed) && parsed != LocalizedContentManager.LanguageCode.mod)
			{
				LocalizedContentManager.CurrentLanguageCode = parsed;
			}
			else
			{
				LocalizedContentManager.CurrentLanguageCode = LocalizedContentManager.GetDefaultLanguageCode();
			}
		}
	}

	private void _loadPreferences()
	{
		string text = Path.Combine(Program.GetAppDataFolder(), _filename);
		if (!File.Exists(text))
		{
			Game1.log.Verbose("path '" + text + "' did not exist and will be created");
			try
			{
				if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.mod)
				{
					languageCode = LocalizedContentManager.CurrentModLanguage.Id;
				}
				else
				{
					languageCode = LocalizedContentManager.CurrentLanguageCode.ToString();
				}
				using FileStream stream = File.Create(text);
				writeSettings(stream);
			}
			catch (Exception ex)
			{
				Game1.log.Error("_loadPreferences; exception occurred trying to create/write.", ex);
				Game1.debugOutput = Game1.parseText(ex.Message);
				return;
			}
		}
		try
		{
			using (FileStream stream2 = File.Open(text, FileMode.Open, FileAccess.Read))
			{
				readSettings(stream2);
			}
			isLoaded = true;
		}
		catch (Exception ex2)
		{
			Game1.log.Error("_loadPreferences; exception occurred trying open/read.", ex2);
			Game1.debugOutput = Game1.parseText(ex2.Message);
		}
	}

	private void readSettings(Stream stream)
	{
		StartupPreferences startupPreferences = (StartupPreferences)serializer.DeserializeFast(stream);
		startMuted = startupPreferences.startMuted;
		timesPlayed = startupPreferences.timesPlayed + 1;
		levelTenCombat = startupPreferences.levelTenCombat;
		levelTenFishing = startupPreferences.levelTenFishing;
		levelTenForaging = startupPreferences.levelTenForaging;
		levelTenMining = startupPreferences.levelTenMining;
		skipWindowPreparation = startupPreferences.skipWindowPreparation;
		windowMode = startupPreferences.windowMode;
		displayIndex = startupPreferences.displayIndex;
		playerLimit = startupPreferences.playerLimit;
		gamepadMode = startupPreferences.gamepadMode;
		fullscreenResolutionX = startupPreferences.fullscreenResolutionX;
		fullscreenResolutionY = startupPreferences.fullscreenResolutionY;
		lastEnteredIP = startupPreferences.lastEnteredIP;
		languageCode = startupPreferences.languageCode;
		clientOptions = startupPreferences.clientOptions;
	}
}
