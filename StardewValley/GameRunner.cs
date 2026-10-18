using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StardewValley;

public class GameRunner : Game
{
	public static GameRunner instance;

	public List<Game1> gameInstances = new List<Game1>();

	public List<Game1> gameInstancesToRemove = new List<Game1>();

	public Game1 gamePtr;

	public bool shouldLoadContent;

	protected bool _initialized;

	protected bool _windowSizeChanged;

	public List<int> startButtonState = new List<int>();

	public List<KeyValuePair<Game1, IEnumerator<int>>> activeNewDayProcesses = new List<KeyValuePair<Game1, IEnumerator<int>>>();

	public int nextInstanceId;

	public static int MaxTextureSize = 4096;

	public GameRunner()
	{
		Program.sdk.EarlyInitialize();
		if (!Program.releaseBuild)
		{
			base.InactiveSleepTime = new TimeSpan(0L);
		}
		Game1.graphics = new GraphicsDeviceManager(this);
		Game1.graphics.PreparingDeviceSettings += delegate(object? sender, PreparingDeviceSettingsEventArgs args)
		{
			args.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
		};
		Game1.graphics.PreferredBackBufferWidth = 1280;
		Game1.graphics.PreferredBackBufferHeight = 720;
		base.Content.RootDirectory = "Content";
		SpriteBatch.TextureTuckAmount = 0.001f;
		LocalMultiplayer.Initialize();
		ItemRegistry.RegisterItemTypes();
		MaxTextureSize = int.MaxValue;
		base.Window.AllowUserResizing = true;
		SubscribeClientSizeChange();
		base.Exiting += delegate(object? sender, EventArgs args)
		{
			ExecuteForInstances(delegate(Game1 instance)
			{
				instance.exitEvent(sender, args);
			});
			Process.GetCurrentProcess().Kill();
		};
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		LocalizedContentManager.OnLanguageChange += delegate
		{
			ExecuteForInstances(delegate(Game1 instance)
			{
				instance.TranslateFields();
			});
		};
		DebugTools.GameConstructed(this);
	}

	protected override void OnActivated(object sender, EventArgs args)
	{
		ExecuteForInstances(delegate(Game1 instance)
		{
			instance.Instance_OnActivated(sender, args);
		});
	}

	public void SubscribeClientSizeChange()
	{
		base.Window.ClientSizeChanged += OnWindowSizeChange;
	}

	public void OnWindowSizeChange(object sender, EventArgs args)
	{
		base.Window.ClientSizeChanged -= OnWindowSizeChange;
		_windowSizeChanged = true;
	}

	protected override void Draw(GameTime gameTime)
	{
		if (_windowSizeChanged)
		{
			ExecuteForInstances(delegate(Game1 instance)
			{
				instance.Window_ClientSizeChanged(null, null);
			});
			_windowSizeChanged = false;
			SubscribeClientSizeChange();
		}
		foreach (Game1 gameInstance in gameInstances)
		{
			LoadInstance(gameInstance);
			Viewport viewport = base.GraphicsDevice.Viewport;
			Game1.graphics.GraphicsDevice.Viewport = new Viewport(0, 0, Math.Min(gameInstance.localMultiplayerWindow.Width, Game1.graphics.GraphicsDevice.PresentationParameters.BackBufferWidth), Math.Min(gameInstance.localMultiplayerWindow.Height, Game1.graphics.GraphicsDevice.PresentationParameters.BackBufferHeight));
			gameInstance.Instance_Draw(gameTime);
			base.GraphicsDevice.Viewport = viewport;
			SaveInstance(gameInstance);
		}
		if (LocalMultiplayer.IsLocalMultiplayer())
		{
			base.GraphicsDevice.Clear(Game1.bgColor);
			foreach (Game1 gameInstance2 in gameInstances)
			{
				Game1.isRenderingScreenBuffer = true;
				gameInstance2.DrawSplitScreenWindow();
				Game1.isRenderingScreenBuffer = false;
			}
		}
		base.Draw(gameTime);
	}

	public int GetNewInstanceID()
	{
		return nextInstanceId++;
	}

	protected override void Initialize()
	{
		DebugTools.BeforeGameInitialize(this);
		InitializeMainInstance();
		base.IsFixedTimeStep = true;
		base.Initialize();
		Game1.graphics.SynchronizeWithVerticalRetrace = true;
		Program.sdk.Initialize();
	}

	public bool WasWindowSizeChanged()
	{
		return _windowSizeChanged;
	}

	public int GetMaxSimultaneousPlayers()
	{
		return 4;
	}

	public void InitializeMainInstance()
	{
		gameInstances = new List<Game1>();
		AddGameInstance(PlayerIndex.One);
	}

	public virtual void ExecuteForInstances(Action<Game1> action)
	{
		Game1 game = Game1.game1;
		if (game != null)
		{
			SaveInstance(game);
		}
		foreach (Game1 gameInstance in gameInstances)
		{
			LoadInstance(gameInstance);
			action(gameInstance);
			SaveInstance(gameInstance);
		}
		if (game != null)
		{
			LoadInstance(game);
		}
		else
		{
			Game1.game1 = null;
		}
	}

	public virtual void RemoveGameInstance(Game1 instance)
	{
		if (gameInstances.Contains(instance) && !gameInstancesToRemove.Contains(instance))
		{
			gameInstancesToRemove.Add(instance);
		}
	}

	public virtual void AddGameInstance(PlayerIndex player_index)
	{
		Game1 game = Game1.game1;
		if (game != null)
		{
			SaveInstance(game, force: true);
		}
		if (gameInstances.Count > 0)
		{
			Game1 game2 = gameInstances[0];
			LoadInstance(game2);
			Game1.StartLocalMultiplayerIfNecessary();
			SaveInstance(game2, force: true);
		}
		Game1 game3 = ((gameInstances.Count == 0) ? CreateGameInstance() : CreateGameInstance(player_index, gameInstances.Count));
		gameInstances.Add(game3);
		if (gamePtr == null)
		{
			gamePtr = game3;
		}
		if (gameInstances.Count > 0)
		{
			game3.staticVarHolder = Activator.CreateInstance(LocalMultiplayer.StaticVarHolderType);
			SetInstanceDefaults(game3);
			LoadInstance(game3);
		}
		Game1.game1 = game3;
		game3.Instance_Initialize();
		if (shouldLoadContent)
		{
			game3.Instance_LoadContent();
		}
		SaveInstance(game3);
		if (game != null)
		{
			LoadInstance(game);
		}
		else
		{
			Game1.game1 = null;
		}
		_windowSizeChanged = true;
	}

	public virtual Game1 CreateGameInstance(PlayerIndex player_index = PlayerIndex.One, int index = 0)
	{
		return new Game1(player_index, index);
	}

	protected override void LoadContent()
	{
		Game1.graphics.PreferredBackBufferWidth = 1280;
		Game1.graphics.PreferredBackBufferHeight = 720;
		Game1.graphics.ApplyChanges();
		LoadInstance(gamePtr);
		gamePtr.Instance_LoadContent();
		SaveInstance(gamePtr);
		DebugTools.GameLoadContent(this);
		foreach (Game1 gameInstance in gameInstances)
		{
			if (gameInstance != gamePtr)
			{
				LoadInstance(gameInstance);
				gameInstance.Instance_LoadContent();
				SaveInstance(gameInstance);
			}
		}
		shouldLoadContent = true;
		base.LoadContent();
	}

	protected override void UnloadContent()
	{
		gamePtr.Instance_UnloadContent();
		base.UnloadContent();
	}

	protected override void Update(GameTime gameTime)
	{
		GameStateQuery.Update();
		for (int i = 0; i < activeNewDayProcesses.Count; i++)
		{
			KeyValuePair<Game1, IEnumerator<int>> keyValuePair = activeNewDayProcesses[i];
			Game1 key = activeNewDayProcesses[i].Key;
			LoadInstance(key);
			if (!keyValuePair.Value.MoveNext())
			{
				key.isLocalMultiplayerNewDayActive = false;
				activeNewDayProcesses.RemoveAt(i);
				i--;
				Utility.CollectGarbage();
			}
			SaveInstance(key);
		}
		while (startButtonState.Count < 4)
		{
			startButtonState.Add(-1);
		}
		for (PlayerIndex playerIndex = PlayerIndex.One; playerIndex <= PlayerIndex.Four; playerIndex++)
		{
			if (GamePad.GetState(playerIndex).IsButtonDown(Buttons.Start))
			{
				if (startButtonState[(int)playerIndex] >= 0)
				{
					startButtonState[(int)playerIndex]++;
				}
			}
			else
			{
				startButtonState[(int)playerIndex] = 0;
			}
		}
		for (int j = 0; j < gameInstances.Count; j++)
		{
			Game1 game = gameInstances[j];
			LoadInstance(game);
			if (j == 0)
			{
				PlayerIndex playerIndex2 = PlayerIndex.Two;
				if (game.instanceOptions.gamepadMode == Options.GamepadModes.ForceOff)
				{
					playerIndex2 = PlayerIndex.One;
				}
				for (PlayerIndex playerIndex3 = playerIndex2; playerIndex3 <= PlayerIndex.Four; playerIndex3++)
				{
					bool flag = false;
					foreach (Game1 gameInstance in gameInstances)
					{
						if (gameInstance.instancePlayerOneIndex == playerIndex3)
						{
							flag = true;
							break;
						}
					}
					if (!flag && game.IsLocalCoopJoinable() && IsStartDown(playerIndex3) && game.ShowLocalCoopJoinMenu())
					{
						InvalidateStartPress(playerIndex3);
					}
				}
			}
			else
			{
				Game1.options.gamepadMode = Options.GamepadModes.ForceOn;
			}
			Game1.debugTimings.StartUpdateTimer();
			game.Instance_Update(gameTime);
			Game1.debugTimings.StopUpdateTimer();
			SaveInstance(game);
		}
		if (gameInstancesToRemove.Count > 0)
		{
			foreach (Game1 item in gameInstancesToRemove)
			{
				LoadInstance(item);
				item.exitEvent(null, null);
				gameInstances.Remove(item);
				Game1.game1 = null;
			}
			for (int k = 0; k < gameInstances.Count; k++)
			{
				gameInstances[k].instanceIndex = k;
			}
			if (gameInstances.Count == 1)
			{
				Game1 game2 = gameInstances[0];
				LoadInstance(game2, force: true);
				game2.staticVarHolder = null;
				Game1.EndLocalMultiplayer();
			}
			bool flag2 = false;
			if (gameInstances.Count > 0)
			{
				foreach (Game1 gameInstance2 in gameInstances)
				{
					if (gameInstance2.instancePlayerOneIndex == PlayerIndex.One)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					gameInstances[0].instancePlayerOneIndex = PlayerIndex.One;
				}
			}
			gameInstancesToRemove.Clear();
			_windowSizeChanged = true;
		}
		base.Update(gameTime);
	}

	public virtual void InvalidateStartPress(PlayerIndex index)
	{
		if (index >= PlayerIndex.One && (int)index < startButtonState.Count)
		{
			startButtonState[(int)index] = -1;
		}
	}

	public virtual bool IsStartDown(PlayerIndex index)
	{
		if (index >= PlayerIndex.One && (int)index < startButtonState.Count)
		{
			return startButtonState[(int)index] == 1;
		}
		return false;
	}

	private static void SetInstanceDefaults(InstanceGame instance)
	{
		for (int i = 0; i < LocalMultiplayer.staticDefaults.Count; i++)
		{
			object value = LocalMultiplayer.staticDefaults[i]?.DeepClone();
			LocalMultiplayer.staticFields[i].SetValue(null, value);
		}
		SaveInstance(instance);
	}

	public static void SaveInstance(InstanceGame instance, bool force = false)
	{
		if (force || LocalMultiplayer.IsLocalMultiplayer())
		{
			if (instance.staticVarHolder == null)
			{
				instance.staticVarHolder = Activator.CreateInstance(LocalMultiplayer.StaticVarHolderType);
			}
			LocalMultiplayer.StaticSave(instance.staticVarHolder);
		}
	}

	public static void LoadInstance(InstanceGame instance, bool force = false)
	{
		Game1.game1 = instance as Game1;
		if ((force || LocalMultiplayer.IsLocalMultiplayer()) && instance.staticVarHolder != null)
		{
			LocalMultiplayer.StaticLoad(instance.staticVarHolder);
			if (Game1.player != null && Game1.player.isCustomized.Value && Game1.splitscreenOptions.TryGetValue(Game1.player.UniqueMultiplayerID, out var value))
			{
				Game1.options = value;
			}
		}
	}
}
