using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Events;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Movies;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Network;
using StardewValley.Pathfinding;
using StardewValley.TokenizableStrings;
using xTile.Dimensions;
using xTile.Layers;
using xTile.ObjectModel;

namespace StardewValley.Locations;

/// <summary>The movie theater location.</summary>
/// <remarks>See also <see cref="T:StardewValley.Events.MovieTheaterScreeningEvent" />.</remarks>
public class MovieTheater : GameLocation
{
	public enum MovieStates
	{
		Preshow,
		Show,
		PostShow
	}

	protected bool _startedMovie;

	protected static bool _isJojaTheater = false;

	protected static List<MovieData> _movieData;

	protected static Dictionary<string, MovieData> _movieDataById;

	protected static List<MovieCharacterReaction> _genericReactions;

	protected static List<ConcessionTaste> _concessionTastes;

	protected readonly NetStringDictionary<int, NetInt> _spawnedMoviePatrons = new NetStringDictionary<int, NetInt>();

	protected readonly NetStringDictionary<string, NetString> _purchasedConcessions = new NetStringDictionary<string, NetString>();

	protected readonly NetStringDictionary<int, NetInt> _playerInvitedPatrons = new NetStringDictionary<int, NetInt>();

	protected readonly NetStringDictionary<bool, NetBool> _characterGroupLookup = new NetStringDictionary<bool, NetBool>();

	protected Dictionary<int, List<Point>> _hangoutPoints;

	protected Dictionary<int, List<Point>> _availableHangoutPoints;

	protected int _maxHangoutGroups;

	protected int _movieStartTime = -1;

	[XmlElement("dayFirstEntered")]
	public readonly NetInt dayFirstEntered = new NetInt(-1);

	protected static Dictionary<string, MovieConcession> _concessions;

	public const int LOVE_MOVIE_FRIENDSHIP = 200;

	public const int LIKE_MOVIE_FRIENDSHIP = 100;

	public const int DISLIKE_MOVIE_FRIENDSHIP = 0;

	public const int LOVE_CONCESSION_FRIENDSHIP = 50;

	public const int LIKE_CONCESSION_FRIENDSHIP = 25;

	public const int DISLIKE_CONCESSION_FRIENDSHIP = 0;

	public const int OPEN_TIME = 900;

	public const int CLOSE_TIME = 2100;

	/// <summary>The main tile sheet ID for movie theater map tiles.</summary>
	public const string MainTileSheetId = "movieTheater_tileSheet";

	[XmlIgnore]
	protected Dictionary<string, KeyValuePair<Point, int>> _destinationPositions = new Dictionary<string, KeyValuePair<Point, int>>();

	[XmlIgnore]
	public PerchingBirds birds;

	/// <summary>If set, the movie ID to watch when a movie is requested, instead of the movie for the current date.</summary>
	[XmlIgnore]
	public static string forceMovieId;

	protected int _exitX;

	protected int _exitY;

	private NetEvent1<MovieViewerLockEvent> movieViewerLockEvent = new NetEvent1<MovieViewerLockEvent>();

	private NetEvent1<StartMovieEvent> startMovieEvent = new NetEvent1<StartMovieEvent>();

	private NetEvent1Field<long, NetLong> requestStartMovieEvent = new NetEvent1Field<long, NetLong>();

	private NetEvent1Field<long, NetLong> endMovieEvent = new NetEvent1Field<long, NetLong>();

	protected List<Farmer> _viewingFarmers = new List<Farmer>();

	protected List<List<Character>> _viewingGroups = new List<List<Character>>();

	protected List<List<Character>> _playerGroups = new List<List<Character>>();

	protected List<List<Character>> _npcGroups = new List<List<Character>>();

	protected static bool _hasRequestedMovieStart = false;

	protected static int _playerHangoutGroup = -1;

	protected int _farmerCount;

	protected readonly NetInt currentState = new NetInt();

	protected readonly NetInt showingId = new NetInt();

	public static string[][][][] possibleNPCGroups = new string[7][][][]
	{
		new string[3][][]
		{
			new string[1][] { new string[1] { "Lewis" } },
			new string[3][]
			{
				new string[3] { "Jas", "Vincent", "Marnie" },
				new string[3] { "Abigail", "Sebastian", "Sam" },
				new string[2] { "Penny", "Maru" }
			},
			new string[1][] { new string[2] { "Lewis", "Marnie" } }
		},
		new string[3][][]
		{
			new string[3][]
			{
				new string[1] { "Clint" },
				new string[2] { "Demetrius", "Robin" },
				new string[1] { "Lewis" }
			},
			new string[2][]
			{
				new string[2] { "Caroline", "Jodi" },
				new string[3] { "Abigail", "Sebastian", "Sam" }
			},
			new string[2][]
			{
				new string[1] { "Lewis" },
				new string[3] { "Abigail", "Sebastian", "Sam" }
			}
		},
		new string[3][][]
		{
			new string[2][]
			{
				new string[2] { "Evelyn", "George" },
				new string[1] { "Lewis" }
			},
			new string[2][]
			{
				new string[2] { "Penny", "Pam" },
				new string[3] { "Abigail", "Sebastian", "Sam" }
			},
			new string[2][]
			{
				new string[2] { "Sandy", "Emily" },
				new string[1] { "Elliot" }
			}
		},
		new string[3][][]
		{
			new string[3][]
			{
				new string[2] { "Penny", "Pam" },
				new string[3] { "Abigail", "Sebastian", "Sam" },
				new string[1] { "Lewis" }
			},
			new string[2][]
			{
				new string[3] { "Alex", "Haley", "Emily" },
				new string[3] { "Abigail", "Sebastian", "Sam" }
			},
			new string[2][]
			{
				new string[2] { "Pierre", "Caroline" },
				new string[3] { "Shane", "Jas", "Marnie" }
			}
		},
		new string[3][][]
		{
			null,
			new string[3][]
			{
				new string[2] { "Haley", "Emily" },
				new string[3] { "Abigail", "Sebastian", "Sam" },
				new string[1] { "Lewis" }
			},
			new string[2][]
			{
				new string[2] { "Penny", "Pam" },
				new string[3] { "Abigail", "Sebastian", "Sam" }
			}
		},
		new string[3][][]
		{
			new string[1][] { new string[1] { "Lewis" } },
			new string[2][]
			{
				new string[2] { "Penny", "Pam" },
				new string[3] { "Abigail", "Sebastian", "Sam" }
			},
			new string[2][]
			{
				new string[3] { "Harvey", "Maru", "Penny" },
				new string[1] { "Leah" }
			}
		},
		new string[3][][]
		{
			new string[3][]
			{
				new string[2] { "Penny", "Pam" },
				new string[3] { "George", "Evelyn", "Alex" },
				new string[1] { "Lewis" }
			},
			new string[2][]
			{
				new string[2] { "Gus", "Willy" },
				new string[2] { "Maru", "Sebastian" }
			},
			new string[2][]
			{
				new string[2] { "Penny", "Pam" },
				new string[2] { "Sandy", "Emily" }
			}
		}
	};

	protected int CurrentState
	{
		get
		{
			return currentState.Value;
		}
		set
		{
			if (Game1.IsMasterGame)
			{
				currentState.Value = value;
			}
			else
			{
				Game1.log.Warn("Tried to set MovieTheater::CurrentState as a farmhand.");
			}
		}
	}

	protected int ShowingId
	{
		get
		{
			return showingId.Value;
		}
		set
		{
			if (Game1.IsMasterGame)
			{
				showingId.Value = value;
			}
			else
			{
				Game1.log.Warn("Tried to set MovieTheater::ShowingId as a farmhand.");
			}
		}
	}

	public MovieTheater()
	{
	}

	public static void AddMoviePoster(GameLocation location, float x, float y, bool isUpcoming = false)
	{
		MovieData movieData = (isUpcoming ? GetUpcomingMovie() : GetMovieToday());
		if (movieData != null)
		{
			Microsoft.Xna.Framework.Rectangle sourceRectForPoster = GetSourceRectForPoster(movieData.SheetIndex);
			location.temporarySprites.Add(new TemporaryAnimatedSprite
			{
				texture = Game1.temporaryContent.Load<Texture2D>(movieData.Texture ?? "LooseSprites\\Movies"),
				sourceRect = sourceRectForPoster,
				sourceRectStartingPos = new Vector2(sourceRectForPoster.X, sourceRectForPoster.Y),
				animationLength = 1,
				totalNumberOfLoops = 9999,
				interval = 9999f,
				scale = 4f,
				position = new Vector2(x, y),
				layerDepth = 0.01f
			});
		}
	}

	public MovieTheater(string map, string name)
		: base(map, name)
	{
		CurrentState = 0;
		GetMovieData();
		_InitializeMap();
		GetMovieReactions();
	}

	public static List<MovieCharacterReaction> GetMovieReactions()
	{
		if (_genericReactions == null)
		{
			_genericReactions = DataLoader.MoviesReactions(Game1.content);
		}
		return _genericReactions;
	}

	public static string GetConcessionTasteForCharacter(Character character, MovieConcession concession)
	{
		if (_concessionTastes == null)
		{
			_concessionTastes = DataLoader.ConcessionTastes(Game1.content);
		}
		ConcessionTaste concessionTaste = null;
		foreach (ConcessionTaste concessionTaste2 in _concessionTastes)
		{
			if (concessionTaste2.Name == "*")
			{
				concessionTaste = concessionTaste2;
				break;
			}
		}
		foreach (ConcessionTaste concessionTaste3 in _concessionTastes)
		{
			if (!(concessionTaste3.Name == character.Name))
			{
				continue;
			}
			if (concessionTaste3.LovedTags.Contains(concession.Name))
			{
				return "love";
			}
			if (concessionTaste3.LikedTags.Contains(concession.Name))
			{
				return "like";
			}
			if (concessionTaste3.DislikedTags.Contains(concession.Name))
			{
				return "dislike";
			}
			if (concessionTaste != null)
			{
				if (concessionTaste.LovedTags.Contains(concession.Name))
				{
					return "love";
				}
				if (concessionTaste.LikedTags.Contains(concession.Name))
				{
					return "like";
				}
				if (concessionTaste.DislikedTags.Contains(concession.Name))
				{
					return "dislike";
				}
			}
			if (concession.Tags == null)
			{
				break;
			}
			foreach (string tag in concession.Tags)
			{
				if (concessionTaste3.LovedTags.Contains(tag))
				{
					return "love";
				}
				if (concessionTaste3.LikedTags.Contains(tag))
				{
					return "like";
				}
				if (concessionTaste3.DislikedTags.Contains(tag))
				{
					return "dislike";
				}
				if (concessionTaste != null)
				{
					if (concessionTaste.LovedTags.Contains(tag))
					{
						return "love";
					}
					if (concessionTaste.LikedTags.Contains(tag))
					{
						return "like";
					}
					if (concessionTaste.DislikedTags.Contains(tag))
					{
						return "dislike";
					}
				}
			}
			break;
		}
		return "like";
	}

	public static IEnumerable<string> GetPatronNames()
	{
		return (Game1.getLocationFromName("MovieTheater") as MovieTheater)?._spawnedMoviePatrons?.Keys;
	}

	protected void _InitializeMap()
	{
		_hangoutPoints = new Dictionary<int, List<Point>>();
		_maxHangoutGroups = 0;
		Layer layer = map.GetLayer("Paths");
		if (layer != null)
		{
			for (int i = 0; i < layer.LayerWidth; i++)
			{
				for (int j = 0; j < layer.LayerHeight; j++)
				{
					if (layer.Tiles[i, j] != null && layer.GetTileIndexAt(i, j) == 7 && layer.Tiles[i, j].Properties.TryGetValue("group", out var value) && int.TryParse(value, out var result))
					{
						if (!_hangoutPoints.TryGetValue(result, out var value2))
						{
							value2 = (_hangoutPoints[result] = new List<Point>());
						}
						value2.Add(new Point(i, j));
						_maxHangoutGroups = Math.Max(_maxHangoutGroups, result);
					}
				}
			}
		}
		ResetTheater();
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(_spawnedMoviePatrons, "_spawnedMoviePatrons").AddField(_purchasedConcessions, "_purchasedConcessions").AddField(currentState, "currentState")
			.AddField(showingId, "showingId")
			.AddField(movieViewerLockEvent, "movieViewerLockEvent")
			.AddField(requestStartMovieEvent, "requestStartMovieEvent")
			.AddField(startMovieEvent, "startMovieEvent")
			.AddField(endMovieEvent, "endMovieEvent")
			.AddField(_playerInvitedPatrons, "_playerInvitedPatrons")
			.AddField(_characterGroupLookup, "_characterGroupLookup")
			.AddField(dayFirstEntered, "dayFirstEntered");
		movieViewerLockEvent.onEvent += OnMovieViewerLockEvent;
		requestStartMovieEvent.onEvent += OnRequestStartMovieEvent;
		startMovieEvent.onEvent += OnStartMovieEvent;
	}

	public void OnStartMovieEvent(StartMovieEvent e)
	{
		if (e.uid == Game1.player.UniqueMultiplayerID)
		{
			if (Game1.activeClickableMenu is ReadyCheckDialog readyCheckDialog)
			{
				readyCheckDialog.closeDialog(Game1.player);
			}
			MovieTheaterScreeningEvent movieTheaterScreeningEvent = new MovieTheaterScreeningEvent();
			Event viewing_event = movieTheaterScreeningEvent.getMovieEvent(GetMovieToday().Id, e.playerGroups, e.npcGroups, GetConcessionsDictionary());
			Rumble.rumble(0.15f, 200f);
			Game1.player.completelyStopAnimatingOrDoingAction();
			playSound("doorClose", Game1.player.Tile);
			Game1.globalFadeToBlack(delegate
			{
				Game1.changeMusicTrack("none");
				startEvent(viewing_event);
			});
		}
	}

	public void OnRequestStartMovieEvent(long uid)
	{
		if (!Game1.IsMasterGame)
		{
			return;
		}
		if (CurrentState == 0)
		{
			if (Game1.player.team.movieMutex.IsLocked())
			{
				Game1.player.team.movieMutex.ReleaseLock();
			}
			Game1.player.team.movieMutex.RequestLock();
			_playerGroups = new List<List<Character>>();
			_npcGroups = new List<List<Character>>();
			List<Character> list = new List<Character>();
			foreach (string patronName in GetPatronNames())
			{
				Character characterFromName = Game1.getCharacterFromName(patronName);
				list.Add(characterFromName);
			}
			foreach (Farmer viewingFarmer in _viewingFarmers)
			{
				List<Character> list2 = new List<Character>();
				list2.Add(viewingFarmer);
				for (int i = 0; i < Game1.player.team.movieInvitations.Count; i++)
				{
					MovieInvitation movieInvitation = Game1.player.team.movieInvitations[i];
					if (movieInvitation.farmer == viewingFarmer && GetFirstInvitedPlayer(movieInvitation.invitedNPC) == viewingFarmer && list.Contains(movieInvitation.invitedNPC))
					{
						list.Remove(movieInvitation.invitedNPC);
						list2.Add(movieInvitation.invitedNPC);
					}
				}
				_playerGroups.Add(list2);
			}
			foreach (List<Character> playerGroup in _playerGroups)
			{
				foreach (Character item in playerGroup)
				{
					if (item is NPC nPC)
					{
						nPC.lastSeenMovieWeek.Set(Game1.Date.TotalWeeks);
					}
				}
			}
			_npcGroups.Add(new List<Character>(list));
			_PopulateNPCOnlyGroups(_playerGroups, _npcGroups);
			_viewingGroups = new List<List<Character>>();
			List<Character> list3 = new List<Character>();
			foreach (List<Character> playerGroup2 in _playerGroups)
			{
				foreach (Character item2 in playerGroup2)
				{
					list3.Add(item2);
				}
			}
			_viewingGroups.Add(list3);
			foreach (List<Character> npcGroup in _npcGroups)
			{
				_viewingGroups.Add(new List<Character>(npcGroup));
			}
			CurrentState = 1;
		}
		startMovieEvent.Fire(new StartMovieEvent(uid, _playerGroups, _npcGroups));
	}

	public void OnMovieViewerLockEvent(MovieViewerLockEvent e)
	{
		_viewingFarmers = new List<Farmer>();
		_movieStartTime = e.movieStartTime;
		foreach (long uid in e.uids)
		{
			Farmer player = Game1.GetPlayer(uid, onlyOnline: true);
			if (player != null)
			{
				_viewingFarmers.Add(player);
			}
		}
		if (_viewingFarmers.Count > 0 && Game1.IsMultiplayer)
		{
			Game1.showGlobalMessage(Game1.content.LoadString("Strings\\UI:MovieStartRequest"));
		}
		if (Game1.player.team.movieMutex.IsLockHeld())
		{
			_ShowMovieStartReady();
		}
	}

	public void _ShowMovieStartReady()
	{
		if (!Game1.IsMultiplayer)
		{
			requestStartMovieEvent.Fire(Game1.player.UniqueMultiplayerID);
			return;
		}
		string text = $"start_movie_{ShowingId}";
		Game1.netReady.SetLocalRequiredFarmers(text, _viewingFarmers);
		Game1.netReady.SetLocalReady(text, ready: true);
		Game1.dialogueUp = false;
		_hasRequestedMovieStart = true;
		Game1.activeClickableMenu = new ReadyCheckDialog(text, allowCancel: true, delegate(Farmer farmer)
		{
			if (_hasRequestedMovieStart)
			{
				_hasRequestedMovieStart = false;
				requestStartMovieEvent.Fire(farmer.UniqueMultiplayerID);
			}
		}, delegate(Farmer farmer)
		{
			if (Game1.activeClickableMenu is ReadyCheckDialog)
			{
				(Game1.activeClickableMenu as ReadyCheckDialog).closeDialog(farmer);
			}
			if (Game1.player.team.movieMutex.IsLockHeld())
			{
				Game1.player.team.movieMutex.ReleaseLock();
			}
		});
	}

	/// <summary>Get the data for all movies.</summary>
	public static List<MovieData> GetMovieData()
	{
		if (_movieData == null)
		{
			_movieData = new List<MovieData>();
			_movieDataById = new Dictionary<string, MovieData>();
			foreach (MovieData item in DataLoader.Movies(Game1.content))
			{
				if (string.IsNullOrWhiteSpace(item.Id))
				{
					Game1.log.Warn("Ignored movie with no ID.");
				}
				else if (!_movieDataById.TryAdd(item.Id, item))
				{
					Game1.log.Warn("Ignored duplicate movie with ID '" + item.Id + "'.");
				}
				else
				{
					_movieData.Add(item);
				}
			}
		}
		return _movieData;
	}

	/// <summary>Get the data for all movies by ID.</summary>
	public static Dictionary<string, MovieData> GetMovieDataById()
	{
		if (_movieDataById == null)
		{
			GetMovieData();
		}
		return _movieDataById;
	}

	/// <summary>Get the data for a specific movie, if it exists.</summary>
	/// <param name="id">The movie ID in <c>Data/Movies</c>.</param>
	/// <param name="data">The movie data, if found.</param>
	/// <returns>Returns whether the movie data was found.</returns>
	public static bool TryGetMovieData(string id, out MovieData data)
	{
		if (id == null)
		{
			data = null;
			return false;
		}
		return GetMovieDataById().TryGetValue(id, out data);
	}

	/// <summary>Get the movie ID corresponding to a pre-1.6 movie index.</summary>
	/// <param name="id">The movie index.</param>
	public static string GetMovieIdFromLegacyIndex(string id)
	{
		if (int.TryParse(id, out var result))
		{
			foreach (MovieData movieDatum in GetMovieData())
			{
				if (movieDatum.SheetIndex == result && (string.IsNullOrWhiteSpace(movieDatum.Texture) || movieDatum.Texture == "LooseSprites\\Movies"))
				{
					return movieDatum.Id;
				}
			}
		}
		return id;
	}

	/// <summary>Get the pixel area in a movie's spritesheet which contains a screen frame.</summary>
	/// <param name="movieIndex">The movie's sprite index in its spritesheet.</param>
	/// <param name="frame">The screen index within the movie's area.</param>
	public static Microsoft.Xna.Framework.Rectangle GetSourceRectForScreen(int movieIndex, int frame)
	{
		int y = movieIndex * 128 + frame / 5 * 64;
		int num = frame % 5 * 96;
		return new Microsoft.Xna.Framework.Rectangle(16 + num, y, 90, 61);
	}

	/// <summary>Get the pixel area in a movie's spritesheet which contains a screen frame.</summary>
	/// <param name="movieIndex">The movie's sprite index in its spritesheet.</param>
	public static Microsoft.Xna.Framework.Rectangle GetSourceRectForPoster(int movieIndex)
	{
		return new Microsoft.Xna.Framework.Rectangle(0, movieIndex * 128, 13, 19);
	}

	public NPC GetMoviePatron(string name)
	{
		for (int i = 0; i < characters.Count; i++)
		{
			if (characters[i].name.Value == name)
			{
				return characters[i];
			}
		}
		return null;
	}

	protected NPC AddMoviePatronNPC(string name, int x, int y, int facingDirection)
	{
		if (_spawnedMoviePatrons.ContainsKey(name))
		{
			return GetMoviePatron(name);
		}
		string textureNameForCharacter = NPC.getTextureNameForCharacter(name);
		NPC.TryGetData(name, out var data);
		int spriteWidth = data?.Size.X ?? 16;
		int spriteHeight = data?.Size.Y ?? 32;
		NPC nPC = new NPC(new AnimatedSprite("Characters\\" + textureNameForCharacter, 0, spriteWidth, spriteHeight), new Vector2(x * 64, y * 64), base.Name, facingDirection, name, null, eventActor: true);
		nPC.EventActor = true;
		nPC.collidesWithOtherCharacters.Set(newValue: false);
		addCharacter(nPC);
		_spawnedMoviePatrons.Add(name, 1);
		GetDialogueForCharacter(nPC);
		return nPC;
	}

	public void RemoveAllPatrons()
	{
		if (_spawnedMoviePatrons != null)
		{
			characters.RemoveWhere((NPC npc) => _spawnedMoviePatrons.ContainsKey(npc.Name));
			_spawnedMoviePatrons.Clear();
		}
	}

	protected override void resetSharedState()
	{
		base.resetSharedState();
		if (CurrentState == 0)
		{
			MovieData movieToday = GetMovieToday();
			Game1.multiplayer.globalChatInfoMessage("MovieStart", TokenStringBuilder.MovieName(movieToday.Id));
		}
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		Game1.getAchievement(36);
		birds = new PerchingBirds(Game1.birdsSpriteSheet, 2, 16, 16, new Vector2(8f, 14f), new Point[14]
		{
			new Point(19, 5),
			new Point(21, 4),
			new Point(16, 3),
			new Point(10, 13),
			new Point(2, 13),
			new Point(2, 6),
			new Point(9, 2),
			new Point(18, 12),
			new Point(21, 11),
			new Point(3, 11),
			new Point(4, 2),
			new Point(12, 12),
			new Point(11, 5),
			new Point(13, 13)
		}, new Point[6]
		{
			new Point(19, 5),
			new Point(21, 4),
			new Point(16, 3),
			new Point(9, 2),
			new Point(21, 11),
			new Point(4, 2)
		});
		if (!_isJojaTheater && Game1.MasterPlayer.mailReceived.Contains("ccMovieTheaterJoja"))
		{
			_isJojaTheater = true;
		}
		if (dayFirstEntered.Value == -1)
		{
			dayFirstEntered.Value = Game1.Date.TotalDays;
		}
		if (!_isJojaTheater)
		{
			birds.roosting = CurrentState == 2;
			for (int i = 0; i < Game1.random.Next(2, 5); i++)
			{
				int bird_type = Game1.random.Next(0, 4);
				if (IsFallHere())
				{
					bird_type = 10;
				}
				birds.AddBird(bird_type);
			}
			if (Game1.timeOfDay > 2100 && Game1.random.NextBool())
			{
				birds.AddBird(11);
			}
		}
		AddMoviePoster(this, 1104f, 292f);
		loadMap(mapPath.Value, force_reload: true);
		if (_isJojaTheater)
		{
			string text = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en) ? "" : "_international");
			if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru)
			{
				text = ".ru-RU";
			}
			base.Map.RequireTileSheet(0, "movieTheater_tileSheet").ImageSource = "Maps\\MovieTheaterJoja_TileSheet" + text;
			base.Map.LoadTileSheets(Game1.mapDisplayDevice);
		}
		switch (CurrentState)
		{
		case 0:
			addRandomNPCs();
			break;
		case 2:
			Game1.changeMusicTrack("movieTheaterAfter");
			Game1.ambientLight = new Color(150, 170, 80);
			addSpecificRandomNPC(0);
			break;
		}
	}

	private void addRandomNPCs()
	{
		Season season = GetSeason();
		Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.Date.TotalDays);
		critters = new List<Critter>();
		if (dayFirstEntered.Value == Game1.Date.TotalDays || random.NextDouble() < 0.25)
		{
			addSpecificRandomNPC(0);
		}
		if (!_isJojaTheater && random.NextDouble() < 0.28)
		{
			addSpecificRandomNPC(4);
			addSpecificRandomNPC(11);
		}
		else if (_isJojaTheater && random.NextDouble() < 0.33)
		{
			addSpecificRandomNPC(13);
		}
		if (random.NextDouble() < 0.1)
		{
			addSpecificRandomNPC(9);
			addSpecificRandomNPC(7);
		}
		switch (season)
		{
		case Season.Fall:
			if (random.NextBool())
			{
				addSpecificRandomNPC(1);
			}
			break;
		case Season.Spring:
			if (random.NextBool())
			{
				addSpecificRandomNPC(3);
			}
			break;
		}
		if (random.NextDouble() < 0.25)
		{
			addSpecificRandomNPC(2);
		}
		if (random.NextDouble() < 0.25)
		{
			addSpecificRandomNPC(6);
		}
		if (random.NextDouble() < 0.25)
		{
			addSpecificRandomNPC(8);
		}
		if (random.NextDouble() < 0.2)
		{
			addSpecificRandomNPC(10);
		}
		if (random.NextDouble() < 0.2)
		{
			addSpecificRandomNPC(12);
		}
		if (random.NextDouble() < 0.2)
		{
			addSpecificRandomNPC(5);
		}
		if (!_isJojaTheater)
		{
			if (random.NextDouble() < 0.75)
			{
				addCritter(new Butterfly(this, new Vector2(13f, 7f)).setStayInbounds(stayInbounds: true));
			}
			if (random.NextDouble() < 0.75)
			{
				addCritter(new Butterfly(this, new Vector2(4f, 8f)).setStayInbounds(stayInbounds: true));
			}
			if (random.NextDouble() < 0.75)
			{
				addCritter(new Butterfly(this, new Vector2(17f, 10f)).setStayInbounds(stayInbounds: true));
			}
		}
	}

	private void addSpecificRandomNPC(int whichRandomNPC)
	{
		Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.Date.TotalDays, whichRandomNPC);
		switch (whichRandomNPC)
		{
		case 0:
			setMapTile(2, 9, 215, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_CraneMan" + random.Choose("2", ""));
			setMapTile(2, 8, 199, "Front", "movieTheater_tileSheet");
			break;
		case 1:
			setMapTile(19, 7, 216, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_Welwick" + random.Choose("2", ""));
			setMapTile(19, 6, 200, "Front", "movieTheater_tileSheet");
			break;
		case 2:
			setAnimatedMapTile(21, 7, new int[4] { 217, 217, 217, 218 }, 700L, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_ShortsMan" + random.Choose("2", ""));
			setAnimatedMapTile(21, 6, new int[4] { 201, 201, 201, 202 }, 700L, "Front", "movieTheater_tileSheet");
			break;
		case 3:
			setMapTile(5, 9, 219, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_Mother" + random.Choose("2", ""));
			setMapTile(6, 9, 220, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_Child" + random.Choose("2", ""));
			setAnimatedMapTile(5, 8, new int[6] { 203, 203, 203, 204, 204, 204 }, 1000L, "Front", "movieTheater_tileSheet");
			break;
		case 4:
			setMapTile(20, 9, 222, "Front", "movieTheater_tileSheet");
			setMapTile(21, 9, 223, "Front", "movieTheater_tileSheet");
			setMapTile(20, 10, 238, "Buildings", "movieTheater_tileSheet");
			setMapTile(21, 10, 239, "Buildings", "movieTheater_tileSheet");
			setMapTile(20, 11, 254, "Buildings", "movieTheater_tileSheet");
			setMapTile(21, 11, 255, "Buildings", "movieTheater_tileSheet");
			break;
		case 5:
			setAnimatedMapTile(10, 7, new int[4] { 251, 251, 251, 252 }, 900L, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_Lupini" + random.Choose("2", ""));
			setAnimatedMapTile(10, 6, new int[4] { 235, 235, 235, 236 }, 900L, "Front", "movieTheater_tileSheet");
			break;
		case 6:
			setAnimatedMapTile(5, 7, new int[4] { 249, 249, 249, 250 }, 600L, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_ConcessionMan" + random.Choose("2", ""));
			setAnimatedMapTile(5, 6, new int[4] { 233, 233, 233, 234 }, 600L, "Front", "movieTheater_tileSheet");
			break;
		case 7:
			setMapTile(1, 12, 248, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_PurpleHairLady");
			setMapTile(1, 11, 232, "Front", "movieTheater_tileSheet");
			break;
		case 8:
			setMapTile(3, 8, 247, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_RedCapGuy" + random.Choose("2", ""));
			setMapTile(3, 7, 231, "Front", "movieTheater_tileSheet");
			break;
		case 9:
			setMapTile(2, 11, 253, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_Governor" + random.Choose("2", ""));
			setMapTile(2, 10, 237, "Front", "movieTheater_tileSheet");
			break;
		case 10:
			setMapTile(9, 7, 221, "Buildings", "movieTheater_tileSheet", "NPCSpeechMessageNoRadius Gunther MovieTheater_Gunther" + random.Choose("2", ""));
			setMapTile(9, 6, 205, "Front", "movieTheater_tileSheet");
			break;
		case 11:
			setMapTile(19, 10, 208, "Buildings", "movieTheater_tileSheet", "NPCSpeechMessageNoRadius Marlon MovieTheater_Marlon" + random.Choose("2", ""));
			setMapTile(19, 9, 192, "Front", "movieTheater_tileSheet");
			break;
		case 12:
			setMapTile(12, 4, 209, "Buildings", "movieTheater_tileSheet", "MessageSpeech MovieTheater_Marcello" + random.Choose("2", ""));
			setMapTile(12, 3, 193, "Front", "movieTheater_tileSheet");
			break;
		case 13:
			setMapTile(17, 12, 241, "Buildings", "movieTheater_tileSheet", "NPCSpeechMessageNoRadius Morris MovieTheater_Morris" + random.Choose("2", ""));
			setMapTile(17, 11, 225, "Front", "movieTheater_tileSheet");
			break;
		}
	}

	/// <summary>Get the movie that plays today.</summary>
	public static MovieData GetMovieToday()
	{
		if (forceMovieId != null)
		{
			if (TryGetMovieData(forceMovieId, out var data))
			{
				return data;
			}
			Game1.log.Warn($"Ignored invalid {"MovieTheater"}.{"forceMovieId"} override '{forceMovieId}'.");
			forceMovieId = null;
		}
		return GetMovieForDate(Game1.Date);
	}

	/// <summary>Get the movies that play in a given season.</summary>
	/// <param name="date">The date whose season and year to check.</param>
	public static List<MovieData> GetMoviesForSeason(WorldDate date)
	{
		WorldDate worldDate = WorldDate.ForDaysPlayed((int)Game1.player.team.theaterBuildDate.Value);
		int year = date.Year - worldDate.Year;
		List<MovieData> movieData = GetMovieData();
		List<MovieData> list = new List<MovieData>();
		foreach (MovieData item in movieData)
		{
			if (MovieSeasonMatches(item, date.Season) && MovieYearMatches(item, year))
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			foreach (MovieData item2 in movieData)
			{
				if (MovieSeasonMatches(item2, date.Season))
				{
					list.Add(item2);
				}
			}
		}
		if (list.Count == 0)
		{
			list.AddRange(movieData);
		}
		if (list.Count > 28)
		{
			Utility.Shuffle(Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)Game1.season, Game1.year), list);
			list.RemoveRange(28, list.Count - 28);
		}
		return list;
	}

	/// <summary>Get the movie that plays on the given date.</summary>
	/// <param name="date">The date to check.</param>
	public static MovieData GetMovieForDate(WorldDate date)
	{
		List<MovieData> list = GetMoviesForSeason(date);
		if (list.Count == 0)
		{
			Game1.log.Warn($"There are no available movies for {date}. Defaulting to all movies.");
			list = GetMovieData();
		}
		float num = 28f / (float)list.Count;
		int index = ((int)Math.Ceiling((float)date.DayOfMonth / num) - 1) % list.Count;
		return list[index];
	}

	/// <summary>Get the next different movie that will play after today.</summary>
	public static MovieData GetUpcomingMovie()
	{
		return GetUpcomingMovieForDate(Game1.Date);
	}

	/// <summary>Get the next different movie that will play after the given date.</summary>
	/// <param name="afterDate">The date of the current movie for which to get the upcoming movie.</param>
	public static MovieData GetUpcomingMovieForDate(WorldDate afterDate)
	{
		List<MovieData> moviesForSeason = GetMoviesForSeason(afterDate);
		MovieData movieForDate = GetMovieForDate(afterDate);
		bool flag = false;
		foreach (MovieData item in moviesForSeason)
		{
			if (item.Id == movieForDate.Id)
			{
				flag = true;
			}
			else if (flag)
			{
				return item;
			}
		}
		moviesForSeason = GetMoviesForSeason(WorldDate.ForDaysPlayed(afterDate.TotalDays + 28));
		foreach (MovieData item2 in moviesForSeason)
		{
			if (item2.Id != movieForDate.Id)
			{
				return item2;
			}
		}
		return moviesForSeason[0];
	}

	/// <summary>Get whether a movie should play in a given year.</summary>
	/// <param name="movie">The movie data to check.</param>
	/// <param name="year">The relative year when the movie theater was built (e.g. 0 if built this year).</param>
	public static bool MovieYearMatches(MovieData movie, int year)
	{
		int? yearModulus = movie.YearModulus;
		if (!yearModulus.HasValue)
		{
			return true;
		}
		int value = movie.YearModulus.Value;
		int valueOrDefault = movie.YearRemainder.GetValueOrDefault();
		if (value < 1)
		{
			Game1.log.Warn($"Movie '{movie.Id}' has invalid year modulus {movie.YearModulus}, must be a number greater than zero.");
			return false;
		}
		return year % value == valueOrDefault;
	}

	/// <summary>Get whether a movie should play in a given season.</summary>
	/// <param name="movie">The movie data to check.</param>
	/// <param name="season">The calendar season.</param>
	public static bool MovieSeasonMatches(MovieData movie, Season season)
	{
		List<Season> seasons = movie.Seasons;
		if (seasons != null && seasons.Count > 0)
		{
			return movie.Seasons.Contains(season);
		}
		return true;
	}

	public override void DayUpdate(int dayOfMonth)
	{
		ShowingId = 0;
		ResetTheater();
		_ResetHangoutPoints();
		base.DayUpdate(dayOfMonth);
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		if (_farmerCount != farmers.Count)
		{
			_farmerCount = farmers.Count;
			if (Game1.activeClickableMenu is ReadyCheckDialog readyCheckDialog)
			{
				readyCheckDialog.closeDialog(Game1.player);
				if (Game1.player.team.movieMutex.IsLockHeld())
				{
					Game1.player.team.movieMutex.ReleaseLock();
				}
			}
		}
		birds?.Update(time);
		base.UpdateWhenCurrentLocation(time);
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		birds?.Draw(b);
		base.drawAboveAlwaysFrontLayer(b);
	}

	public static bool Invite(Farmer farmer, NPC invited_npc)
	{
		if (farmer == null || invited_npc == null)
		{
			return false;
		}
		MovieInvitation movieInvitation = new MovieInvitation();
		movieInvitation.farmer = farmer;
		movieInvitation.invitedNPC = invited_npc;
		farmer.team.movieInvitations.Add(movieInvitation);
		return true;
	}

	public void ResetTheater()
	{
		_playerHangoutGroup = -1;
		RemoveAllPatrons();
		_playerGroups.Clear();
		_npcGroups.Clear();
		_viewingGroups.Clear();
		_viewingFarmers.Clear();
		_purchasedConcessions.Clear();
		_playerInvitedPatrons.Clear();
		_characterGroupLookup.Clear();
		_ResetHangoutPoints();
		Game1.player.team.movieMutex.ReleaseLock();
		CurrentState = 0;
	}

	public override void updateEvenIfFarmerIsntHere(GameTime time, bool ignoreWasUpdatedFlush = false)
	{
		base.updateEvenIfFarmerIsntHere(time, ignoreWasUpdatedFlush);
		movieViewerLockEvent.Poll();
		requestStartMovieEvent.Poll();
		startMovieEvent.Poll();
		endMovieEvent.Poll();
		if (!Game1.IsMasterGame)
		{
			return;
		}
		for (int i = 0; i < _viewingFarmers.Count; i++)
		{
			Farmer farmer = _viewingFarmers[i];
			if (!Game1.getOnlineFarmers().Contains(farmer))
			{
				_viewingFarmers.RemoveAt(i);
				i--;
			}
			else if (CurrentState == 2 && !farmers.Contains(farmer) && !HasFarmerWatchingBroadcastEventReturningHere() && farmer.currentLocation != null && !farmer.currentLocation.IsTemporary)
			{
				_viewingFarmers.RemoveAt(i);
				i--;
			}
		}
		if (CurrentState != 0 && _viewingFarmers.Count == 0)
		{
			MovieData movieToday = GetMovieToday();
			Game1.multiplayer.globalChatInfoMessage("MovieEnd", TokenStringBuilder.MovieName(movieToday.Id));
			ResetTheater();
			ShowingId++;
		}
		if (Game1.player.team.movieInvitations == null || _playerInvitedPatrons.Count() >= 8)
		{
			return;
		}
		foreach (Farmer farmer2 in farmers)
		{
			for (int j = 0; j < Game1.player.team.movieInvitations.Count; j++)
			{
				MovieInvitation movieInvitation = Game1.player.team.movieInvitations[j];
				if (movieInvitation.fulfilled || _spawnedMoviePatrons.ContainsKey(movieInvitation.invitedNPC.displayName))
				{
					continue;
				}
				if (_playerHangoutGroup < 0)
				{
					_playerHangoutGroup = Game1.random.Next(_maxHangoutGroups);
				}
				int key = _playerHangoutGroup;
				if (movieInvitation.farmer == farmer2 && GetFirstInvitedPlayer(movieInvitation.invitedNPC) == farmer2)
				{
					while (_availableHangoutPoints[key].Count == 0)
					{
						key = Game1.random.Next(_maxHangoutGroups);
					}
					Point point = Game1.random.ChooseFrom(_availableHangoutPoints[key]);
					NPC nPC = AddMoviePatronNPC(movieInvitation.invitedNPC.name.Value, 14, 15, 0);
					_playerInvitedPatrons.Add(nPC.name.Value, 1);
					_availableHangoutPoints[key].Remove(point);
					int result = 2;
					IPropertyCollection properties = map.GetLayer("Paths").Tiles[point.X, point.Y].Properties;
					if (properties != null && properties.TryGetValue("direction", out var value))
					{
						int.TryParse(value, out result);
					}
					_destinationPositions[nPC.Name] = new KeyValuePair<Point, int>(point, result);
					PathCharacterToLocation(nPC, point, result);
					movieInvitation.fulfilled = true;
				}
			}
		}
	}

	public static MovieCharacterReaction GetReactionsForCharacter(NPC character)
	{
		if (character == null)
		{
			return null;
		}
		foreach (MovieCharacterReaction movieReaction in GetMovieReactions())
		{
			if (!(movieReaction.NPCName != character.Name))
			{
				return movieReaction;
			}
		}
		return null;
	}

	/// <inheritdoc />
	public override void checkForMusic(GameTime time)
	{
	}

	public static string GetResponseForMovie(NPC character)
	{
		string result = "like";
		MovieData movieToday = GetMovieToday();
		if (movieToday == null)
		{
			return null;
		}
		if (movieToday != null)
		{
			foreach (MovieCharacterReaction movieReaction in GetMovieReactions())
			{
				if (movieReaction.NPCName != character.Name)
				{
					continue;
				}
				foreach (MovieReaction reaction in movieReaction.Reactions)
				{
					if (reaction.ShouldApplyToMovie(movieToday, GetPatronNames()))
					{
						string response = reaction.Response;
						if (response != null && response.Length > 0)
						{
							result = reaction.Response;
							break;
						}
					}
				}
			}
		}
		return result;
	}

	public Dialogue GetDialogueForCharacter(NPC character)
	{
		MovieData movieToday = GetMovieToday();
		if (movieToday != null)
		{
			foreach (MovieCharacterReaction genericReaction in _genericReactions)
			{
				if (genericReaction.NPCName != character.Name)
				{
					continue;
				}
				foreach (MovieReaction reaction in genericReaction.Reactions)
				{
					if (!reaction.ShouldApplyToMovie(movieToday, GetPatronNames(), GetResponseForMovie(character)))
					{
						continue;
					}
					string response = reaction.Response;
					if (response == null || response.Length <= 0 || reaction.SpecialResponses == null)
					{
						continue;
					}
					switch (CurrentState)
					{
					case 0:
						if (reaction.SpecialResponses.BeforeMovie != null)
						{
							return new Dialogue(character, null, FormatString(reaction.SpecialResponses.BeforeMovie.Text));
						}
						break;
					case 1:
						if (reaction.SpecialResponses.DuringMovie != null)
						{
							return new Dialogue(character, null, FormatString(reaction.SpecialResponses.DuringMovie.Text));
						}
						break;
					case 2:
						if (reaction.SpecialResponses.AfterMovie != null)
						{
							return new Dialogue(character, null, FormatString(reaction.SpecialResponses.AfterMovie.Text));
						}
						break;
					}
					break;
				}
				break;
			}
		}
		return null;
	}

	public string FormatString(string text, params string[] args)
	{
		text = TokenParser.ParseText(text);
		string arg = TokenParser.ParseText(GetMovieToday().Title);
		return string.Format(text, arg, Game1.player.displayName, args);
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		Microsoft.Xna.Framework.Rectangle value = new Microsoft.Xna.Framework.Rectangle(tileLocation.X * 64, tileLocation.Y * 64, 64, 64);
		string[] tilePropertySplitBySpaces = GetTilePropertySplitBySpaces("Action", "Buildings", tileLocation.X, tileLocation.Y);
		if (tilePropertySplitBySpaces.Length != 0)
		{
			return performAction(tilePropertySplitBySpaces, who, tileLocation);
		}
		foreach (NPC character in characters)
		{
			if (character == null || character.IsMonster || (who.isRidingHorse() && character is Horse) || !character.GetBoundingBox().Intersects(value))
			{
				continue;
			}
			if (!character.isMoving())
			{
				bool value2;
				if (_playerInvitedPatrons.ContainsKey(character.Name))
				{
					character.faceTowardFarmerForPeriod(5000, 4, faceAway: false, who);
					Dialogue dialogueForCharacter = GetDialogueForCharacter(character);
					if (dialogueForCharacter != null)
					{
						character.CurrentDialogue.Push(dialogueForCharacter);
						Game1.drawDialogue(character);
						character.grantConversationFriendship(Game1.player);
					}
				}
				else if (_characterGroupLookup.TryGetValue(character.Name, out value2))
				{
					if (!value2)
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Characters:MovieTheater_AfterMovieAlone", character.displayName));
					}
					else
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Characters:MovieTheater_AfterMovie", character.displayName));
					}
				}
			}
			return true;
		}
		return base.checkAction(tileLocation, viewport, who);
	}

	protected void _PopulateNPCOnlyGroups(List<List<Character>> player_groups, List<List<Character>> groups)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (List<Character> player_group in player_groups)
		{
			foreach (Character item2 in player_group)
			{
				if (item2 is NPC)
				{
					hashSet.Add(item2.name.Value);
				}
			}
		}
		foreach (List<Character> group in groups)
		{
			foreach (Character item3 in group)
			{
				if (item3 is NPC)
				{
					hashSet.Add(item3.name.Value);
				}
			}
		}
		Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.Date.TotalDays);
		int num = 0;
		for (int i = 0; i < 2; i++)
		{
			if (random.NextDouble() < 0.75)
			{
				num++;
			}
		}
		int num2 = 0;
		if (_movieStartTime >= 1200)
		{
			num2 = 1;
		}
		if (_movieStartTime >= 1800)
		{
			num2 = 2;
		}
		string[][] array = possibleNPCGroups[(int)Game1.Date.DayOfWeek][num2];
		if (array == null)
		{
			return;
		}
		if (groups.Count > 0 && groups[0].Count == 0)
		{
			groups.RemoveAt(0);
		}
		for (int j = 0; j < num; j++)
		{
			if (groups.Count >= 2)
			{
				break;
			}
			string[] array2 = random.Choose(array);
			bool flag = true;
			string[] array3 = array2;
			foreach (string text in array3)
			{
				bool flag2 = false;
				foreach (Farmer allFarmer in Game1.getAllFarmers())
				{
					if (allFarmer.friendshipData.ContainsKey(text))
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					flag = false;
					break;
				}
				if (hashSet.Contains(text))
				{
					flag = false;
					break;
				}
				if (GetResponseForMovie(Game1.getCharacterFromName(text)) == "dislike" || GetResponseForMovie(Game1.getCharacterFromName(text)) == "reject")
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				List<Character> list = new List<Character>();
				array3 = array2;
				foreach (string text2 in array3)
				{
					NPC item = AddMoviePatronNPC(text2, 1000, 1000, 2);
					list.Add(item);
					hashSet.Add(text2);
					_characterGroupLookup[text2] = array2.Length > 1;
				}
				groups.Add(list);
			}
		}
	}

	public Dictionary<Character, MovieConcession> GetConcessionsDictionary()
	{
		Dictionary<Character, MovieConcession> dictionary = new Dictionary<Character, MovieConcession>();
		foreach (string key in _purchasedConcessions.Keys)
		{
			Character characterFromName = Game1.getCharacterFromName(key);
			if (characterFromName != null && GetConcessions().TryGetValue(_purchasedConcessions[key], out var value))
			{
				dictionary[characterFromName] = value;
			}
		}
		return dictionary;
	}

	protected void _ResetHangoutPoints()
	{
		_destinationPositions.Clear();
		_availableHangoutPoints = new Dictionary<int, List<Point>>();
		foreach (int key in _hangoutPoints.Keys)
		{
			_availableHangoutPoints[key] = new List<Point>(_hangoutPoints[key]);
		}
	}

	public override void cleanupBeforePlayerExit()
	{
		if (!Game1.eventUp)
		{
			Game1.changeMusicTrack("none");
		}
		birds = null;
		base.cleanupBeforePlayerExit();
	}

	public void RequestEndMovie(long uid)
	{
		if (!Game1.IsMasterGame)
		{
			return;
		}
		if (CurrentState == 1)
		{
			CurrentState = 2;
			for (int i = 0; i < _viewingGroups.Count; i++)
			{
				int index = Game1.random.Next(_viewingGroups.Count);
				List<Character> value = _viewingGroups[i];
				_viewingGroups[i] = _viewingGroups[index];
				_viewingGroups[index] = value;
			}
			_ResetHangoutPoints();
			int num = 0;
			for (int j = 0; j < _viewingGroups.Count; j++)
			{
				for (int k = 0; k < _viewingGroups[j].Count; k++)
				{
					if (!(_viewingGroups[j][k] is NPC))
					{
						continue;
					}
					NPC moviePatron = GetMoviePatron(_viewingGroups[j][k].Name);
					if (moviePatron != null)
					{
						moviePatron.setTileLocation(new Vector2(14f, 4f + (float)num * 1f));
						Point point = Game1.random.ChooseFrom(_availableHangoutPoints[j]);
						if (!int.TryParse(doesTileHaveProperty(point.X, point.Y, "direction", "Paths"), out var result))
						{
							result = 2;
						}
						_destinationPositions[moviePatron.Name] = new KeyValuePair<Point, int>(point, result);
						PathCharacterToLocation(moviePatron, point, result);
						_availableHangoutPoints[j].Remove(point);
						num++;
					}
				}
			}
		}
		(Game1.GetPlayer(uid, onlyOnline: true)?.team ?? Game1.MasterPlayer.team).endMovieEvent.Fire(uid);
	}

	public void PathCharacterToLocation(NPC character, Point point, int direction)
	{
		if (character.currentLocation == this)
		{
			PathFindController pathFindController = new PathFindController(character, this, character.TilePoint, direction);
			pathFindController.pathToEndPoint = PathFindController.findPathForNPCSchedules(character.TilePoint, point, this, 30000, character);
			character.temporaryController = pathFindController;
			character.followSchedule = true;
			character.ignoreScheduleToday = true;
		}
	}

	public static Dictionary<string, MovieConcession> GetConcessions()
	{
		if (_concessions == null)
		{
			_concessions = new Dictionary<string, MovieConcession>();
			foreach (ConcessionItemData item in DataLoader.Concessions(Game1.content))
			{
				_concessions[item.Id] = new MovieConcession(item);
			}
		}
		return _concessions;
	}

	/// <summary>Get a movie concession.</summary>
	/// <param name="id">The concession ID.</param>
	public static MovieConcession GetConcessionItem(string id)
	{
		if (id == null || !GetConcessions().TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	/// <summary>Handle a movie concession being bought.</summary>
	/// <inheritdoc cref="T:StardewValley.Menus.ShopMenu.OnPurchaseDelegate" />
	public bool OnPurchaseConcession(ISalable salable, Farmer who, int countTaken, ItemStockInformation stock)
	{
		foreach (MovieInvitation movieInvitation in who.team.movieInvitations)
		{
			if (movieInvitation.farmer == who && GetFirstInvitedPlayer(movieInvitation.invitedNPC) == Game1.player && _spawnedMoviePatrons.ContainsKey(movieInvitation.invitedNPC.Name))
			{
				MovieConcession movieConcession = (MovieConcession)salable;
				_purchasedConcessions[movieInvitation.invitedNPC.Name] = movieConcession.Id;
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Characters:MovieTheater_ConcessionPurchased", movieConcession.DisplayName, movieInvitation.invitedNPC.displayName));
				return true;
			}
		}
		return false;
	}

	public bool HasInvitedSomeone(Farmer who)
	{
		foreach (MovieInvitation movieInvitation in who.team.movieInvitations)
		{
			if (movieInvitation.farmer == who && GetFirstInvitedPlayer(movieInvitation.invitedNPC) == Game1.player && _spawnedMoviePatrons.ContainsKey(movieInvitation.invitedNPC.Name))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasPurchasedConcession(Farmer who)
	{
		if (!HasInvitedSomeone(who))
		{
			return false;
		}
		foreach (MovieInvitation movieInvitation in who.team.movieInvitations)
		{
			if (movieInvitation.farmer != who || GetFirstInvitedPlayer(movieInvitation.invitedNPC) != Game1.player)
			{
				continue;
			}
			foreach (string key in _purchasedConcessions.Keys)
			{
				if (key == movieInvitation.invitedNPC.Name && _spawnedMoviePatrons.ContainsKey(movieInvitation.invitedNPC.Name))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static Farmer GetFirstInvitedPlayer(NPC npc)
	{
		foreach (MovieInvitation movieInvitation in Game1.player.team.movieInvitations)
		{
			if (movieInvitation.invitedNPC.Name == npc.Name)
			{
				return movieInvitation.farmer;
			}
		}
		return null;
	}

	/// <inheritdoc />
	public override void performTouchAction(string[] action, Vector2 playerStandingPosition)
	{
		if (IgnoreTouchActions())
		{
			return;
		}
		if (ArgUtility.Get(action, 0) == "Theater_Exit")
		{
			if (!ArgUtility.TryGetPoint(action, 1, out var value, out var error, "Point exitTile"))
			{
				LogTileTouchActionError(action, playerStandingPosition, error);
				return;
			}
			Point theaterTileOffset = Town.GetTheaterTileOffset();
			_exitX = value.X + theaterTileOffset.X;
			_exitY = value.Y + theaterTileOffset.Y;
			if (Game1.player.lastSeenMovieWeek.Value >= Game1.Date.TotalWeeks)
			{
				_Leave();
				return;
			}
			Game1.player.position.Y -= ((float)Game1.player.Speed + Game1.player.addedSpeed) * 2f;
			Game1.player.Halt();
			Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\Characters:MovieTheater_LeavePrompt"), Game1.currentLocation.createYesNoResponses(), "LeaveMovie");
		}
		else
		{
			base.performTouchAction(action, playerStandingPosition);
		}
	}

	public static List<MovieConcession> GetConcessionsForGuest()
	{
		string npc_name = null;
		foreach (MovieInvitation movieInvitation in Game1.player.team.movieInvitations)
		{
			if (movieInvitation.farmer == Game1.player && GetFirstInvitedPlayer(movieInvitation.invitedNPC) == Game1.player)
			{
				npc_name = movieInvitation.invitedNPC.Name;
				break;
			}
		}
		return GetConcessionsForGuest(npc_name);
	}

	public static List<MovieConcession> GetConcessionsForGuest(string npc_name)
	{
		if (npc_name == null)
		{
			npc_name = "Abigail";
		}
		List<MovieConcession> list = new List<MovieConcession>();
		List<MovieConcession> list2 = GetConcessions().Values.ToList();
		Random random = Utility.CreateDaySaveRandom();
		Utility.Shuffle(random, list2);
		NPC characterFromName = Game1.getCharacterFromName(npc_name);
		if (characterFromName == null)
		{
			return list;
		}
		int num = 1;
		int num2 = 2;
		int num3 = 1;
		int num4 = 5;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < list2.Count; j++)
			{
				MovieConcession movieConcession = list2[j];
				if (GetConcessionTasteForCharacter(characterFromName, movieConcession) == "love" && (!movieConcession.Name.Equals("Stardrop Sorbet") || random.NextDouble() < 0.33))
				{
					list.Add(movieConcession);
					list2.RemoveAt(j);
					j--;
					break;
				}
			}
		}
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < list2.Count; l++)
			{
				MovieConcession movieConcession2 = list2[l];
				if (GetConcessionTasteForCharacter(characterFromName, movieConcession2) == "like")
				{
					list.Add(movieConcession2);
					list2.RemoveAt(l);
					l--;
					break;
				}
			}
		}
		for (int m = 0; m < num3; m++)
		{
			for (int n = 0; n < list2.Count; n++)
			{
				MovieConcession movieConcession3 = list2[n];
				if (GetConcessionTasteForCharacter(characterFromName, movieConcession3) == "dislike")
				{
					list.Add(movieConcession3);
					list2.RemoveAt(n);
					n--;
					break;
				}
			}
		}
		for (int num5 = list.Count; num5 < num4; num5++)
		{
			int num6 = 0;
			if (num6 < list2.Count)
			{
				MovieConcession item = list2[num6];
				list.Add(item);
				list2.RemoveAt(num6);
				num6--;
			}
		}
		if (_isJojaTheater && !list.Exists((MovieConcession x) => x.Name.Equals("JojaCorn")))
		{
			MovieConcession movieConcession4 = list2.Find((MovieConcession x) => x.Name.Equals("JojaCorn"));
			if (movieConcession4 != null)
			{
				list.Add(movieConcession4);
			}
		}
		Utility.Shuffle(random, list);
		return list;
	}

	public override bool answerDialogueAction(string questionAndAnswer, string[] questionParams)
	{
		if (questionAndAnswer == null)
		{
			return false;
		}
		if (!(questionAndAnswer == "LeaveMovie_Yes"))
		{
			if (questionAndAnswer == "Concession_Yes")
			{
				Utility.TryOpenShopMenu("Concessions", this, null, null, forceOpen: true);
				if (Game1.activeClickableMenu is ShopMenu shopMenu)
				{
					shopMenu.onPurchase = OnPurchaseConcession;
				}
				return true;
			}
			return base.answerDialogueAction(questionAndAnswer, questionParams);
		}
		_Leave();
		return true;
	}

	protected void _Leave()
	{
		forceMovieId = null;
		Game1.player.completelyStopAnimatingOrDoingAction();
		Game1.warpFarmer("Town", _exitX, _exitY, 2);
	}

	/// <inheritdoc />
	public override bool performAction(string[] action, Farmer who, Location tileLocation)
	{
		switch (ArgUtility.Get(action, 0))
		{
		case "Concessions":
			if (CurrentState > 0)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Characters:MovieTheater_ConcessionAfterMovie"));
				return true;
			}
			if (!HasInvitedSomeone(who))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Characters:MovieTheater_ConcessionAlone"));
				return true;
			}
			if (HasPurchasedConcession(who))
			{
				foreach (MovieInvitation movieInvitation in who.team.movieInvitations)
				{
					if (movieInvitation.farmer != who || GetFirstInvitedPlayer(movieInvitation.invitedNPC) != Game1.player)
					{
						continue;
					}
					foreach (string key in _purchasedConcessions.Keys)
					{
						if (key == movieInvitation.invitedNPC.Name)
						{
							MovieConcession movieConcession = GetConcessionsDictionary()[Game1.getCharacterFromName(key)];
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Characters:MovieTheater_ConcessionPurchased", movieConcession.DisplayName, Game1.RequireCharacter(key).displayName));
							return true;
						}
					}
				}
				return true;
			}
			Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\Characters:MovieTheater_Concession"), Game1.currentLocation.createYesNoResponses(), "Concession");
			return true;
		case "Theater_Doors":
			if (CurrentState > 0)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Theater_MovieEndReEntry"));
				return true;
			}
			if (Game1.player.team.movieMutex.IsLocked())
			{
				_ShowMovieStartReady();
				return true;
			}
			Game1.player.team.movieMutex.RequestLock(delegate
			{
				List<Farmer> list = new List<Farmer>();
				foreach (Farmer farmer in farmers)
				{
					if (farmer.isActive() && farmer.currentLocation == this)
					{
						list.Add(farmer);
					}
				}
				movieViewerLockEvent.Fire(new MovieViewerLockEvent(list, Game1.timeOfDay));
			});
			return true;
		case "CraneGame":
			if (!hasTileAt(2, 9, "Buildings"))
			{
				createQuestionDialogue(Game1.content.LoadString("Strings\\StringsFromMaps:MovieTheater_CranePlay", 500), createYesNoResponses(), tryToStartCraneGame);
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromMaps:MovieTheater_CraneOccupied"));
			}
			return true;
		default:
			return base.performAction(action, who, tileLocation);
		}
	}

	private void tryToStartCraneGame(Farmer who, string whichAnswer)
	{
		if (!whichAnswer.EqualsIgnoreCase("yes"))
		{
			return;
		}
		if (Game1.player.Money >= 500)
		{
			Game1.player.Money -= 500;
			Game1.changeMusicTrack("none", track_interruptable: false, MusicContext.MiniGame);
			Game1.globalFadeToBlack(delegate
			{
				Game1.currentMinigame = new CraneGame();
			}, 0.008f);
		}
		else
		{
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11325"));
		}
	}

	public static void ClearCachedLocalizedData()
	{
		_concessions = null;
		_genericReactions = null;
		_movieData = null;
	}

	/// <summary>Reset the cached concession tastes, so they're reloaded from <c>Data/ConcessionTastes</c> next time they're accessed.</summary>
	public static void ClearCachedConcessionTastes()
	{
		_concessionTastes = null;
	}
}
