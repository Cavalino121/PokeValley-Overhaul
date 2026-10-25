using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewValley.Pokemon;

public class PokemonDebugMenu : IClickableMenu
{
	public static bool IsDebugUnlocked { get; set; } = false;

	private enum Tab
	{
		Pokemon,
		Items,
		TMs,
		Battle,
		Cheats,
		Mechanics,
		UpdatesV2,
		Legendaries
	}

	private enum StyleFilter
	{
		Normal = 0,
		Shiny = 1,
		Mega = 2,
		MegaShiny = 3
	}

	private Tab _currentTab = Tab.Pokemon;
	private int _selectedLevel = 25;
	private int _speciesPage = 0;
	private int _tmPage = 0;
	private int _legendaryPage = 0;
	private bool _filterFieldMovesOnly = false;
	private bool _selectingWildBattleTarget = false;
	private bool _spawnAsEgg = false;

	private int _filterGen = 0; // 0 = Todas, 1..9 = Gen 1..9
	private string _filterType = "Todos"; // Todos or Type name
	private string _filterRegion = "Todas"; // Todas, Kanto..Paldea
	private StyleFilter _filterStyle = StyleFilter.Normal;
	private int _battlePage = 0;
	private static PlayerParty Party => PlayerParty.GetForFarmer(Game1.player);

	private readonly Dictionary<string, PokemonInstance> _previewInstances = new(StringComparer.OrdinalIgnoreCase);

	private readonly List<ClickableComponent> _tabButtons = new();
	private readonly List<ClickableComponent> _pokemonButtons = new();
	private readonly List<ClickableComponent> _levelButtons = new();
	private readonly List<ClickableComponent> _genFilterButtons = new();
	private readonly List<ClickableComponent> _regionFilterButtons = new();
	private readonly List<ClickableComponent> _typeFilterButtons = new();
	private readonly List<ClickableComponent> _styleFilterButtons = new();
	private ClickableComponent _eggToggleButton;
	private readonly List<ClickableComponent> _itemButtons = new();
	private readonly List<ClickableComponent> _tmButtons = new();
	private readonly List<ClickableComponent> _battleButtons = new();
	private readonly List<ClickableComponent> _cheatButtons = new();
	private readonly List<ClickableComponent> _mechanicButtons = new();
	private readonly List<ClickableComponent> _updateButtons = new();
	private readonly List<ClickableComponent> _legendaryButtons = new();

	private ClickableComponent _prevPageButton;
	private ClickableComponent _nextPageButton;
	private ClickableComponent _battlePrevPageButton;
	private ClickableComponent _battleNextPageButton;
	private ClickableComponent _tmFilterButton;
	private ClickableComponent _tmTypeFilterButton;
	private ClickableComponent _giveFieldTmsButton;
	private ClickableComponent _giveAllTmsButton;
	private ClickableComponent _tmPrevPageButton;
	private ClickableComponent _tmNextPageButton;
	private ClickableComponent _legPrevPageButton;
	private ClickableComponent _legNextPageButton;
	private ClickableTextureComponent _closeButton;

	private static readonly int[] DebugLevels = new[] { 5, 15, 30, 50, 75, 100 };

	private static readonly string[] AllLegendariesList = new[]
	{
		// Gen 1
		"Articuno", "Zapdos", "Moltres", "Mewtwo", "Mew",
		// Gen 2
		"Raikou", "Entei", "Suicune", "Lugia", "Ho-Oh", "Celebi",
		// Gen 3
		"Regirock", "Regice", "Registeel", "Latias", "Latios", "Kyogre", "Groudon", "Rayquaza", "Jirachi", "Deoxys",
		// Gen 4
		"Uxie", "Mesprit", "Azelf", "Dialga", "Palkia", "Heatran", "Regigigas", "Giratina", "Cresselia", "Phione", "Manaphy", "Darkrai", "Shaymin", "Arceus",
		// Gen 5
		"Victini", "Cobalion", "Terrakion", "Virizion", "Tornadus", "Thundurus", "Reshiram", "Zekrom", "Landorus", "Kyurem", "Keldeo", "Meloetta", "Genesect",
		// Gen 6
		"Xerneas", "Yveltal", "Zygarde", "Diancie", "Hoopa", "Volcanion",
		// Gen 7
		"Type: Null", "Silvally", "Tapu Koko", "Tapu Lele", "Tapu Bulu", "Tapu Fini", "Cosmog", "Cosmoem", "Solgaleo", "Lunala", "Nihilego", "Buzzwole", "Pheromosa", "Xurkitree", "Celesteela", "Kartana", "Guzzlord", "Necrozma", "Magearna", "Marshadow", "Poipole", "Naganadel", "Stakataka", "Blacephalon", "Zeraora", "Meltan", "Melmetal",
		// Gen 8
		"Zacian", "Zamazenta", "Eternatus", "Kubfu", "Urshifu", "Zarude", "Regieleki", "Regidrago", "Glastrier", "Spectrier", "Calyrex", "Enamorus",
		// Gen 9
		"Wo-Chien", "Chien-Pao", "Ting-Lu", "Chi-Yu", "Roaring Moon", "Iron Valiant", "Koraidon", "Miraidon", "Walking Wake", "Iron Leaves", "Okidogi", "Munkidori", "Fezandipiti", "Ogerpon", "Gouging Fire", "Raging Bolt", "Iron Boulder", "Iron Crown", "Terapagos", "Pecharunt"
	};

	private static readonly string[] AllRegionsList = new[]
	{
		"Todas", "Kanto", "Johto", "Hoenn", "Sinnoh", "Unova", "Kalos", "Alola", "Galar", "Paldea"
	};

	private static readonly string[] AllTypesList = new[]
	{
		"Todos", "Normal", "Fire", "Water", "Electric", "Grass", "Ice", "Fighting",
		"Poison", "Ground", "Flying", "Psychic", "Bug", "Rock", "Ghost", "Dragon", "Dark", "Steel", "Fairy"
	};

	public PokemonDebugMenu()
		: base(0, 0, 900, 620)
	{
		xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
		yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

		_closeButton = new ClickableTextureComponent(
			new Rectangle(xPositionOnScreen + width - 48, yPositionOnScreen + 14, 44, 44),
			Game1.mouseCursors,
			new Rectangle(337, 494, 12, 12),
			3.6f
		);

		SetupButtons();
	}

	public void SetDebugState(int tabIndex, int page = 0, bool fieldMovesOnly = false)
	{
		_currentTab = (Tab)Math.Clamp(tabIndex, 0, 7);
		_speciesPage = page;
		_tmPage = page;
		_filterFieldMovesOnly = fieldMovesOnly;
		SetupButtons();
	}

	private string GetStyleLabel()
	{
		return _filterStyle switch
		{
			StyleFilter.Shiny => "★ Shiny",
			StyleFilter.Mega => "🧬 Mega",
			StyleFilter.MegaShiny => "★🧬 Mega Shiny",
			_ => "Estilo: Normal"
		};
	}

	private PokemonInstance GetPreviewInstance(PokemonSpecies sp, bool isShiny)
	{
		string key = $"{sp.InternalName}:{(isShiny ? 1 : 0)}";
		if (!_previewInstances.TryGetValue(key, out var inst) || inst == null)
		{
			inst = new PokemonInstance(sp, _selectedLevel, isShiny);
			_previewInstances[key] = inst;
		}
		return inst;
	}

	public void SetupButtons()
	{
		_tabButtons.Clear();
		_pokemonButtons.Clear();
		_levelButtons.Clear();
		_genFilterButtons.Clear();
		_regionFilterButtons.Clear();
		_typeFilterButtons.Clear();
		_styleFilterButtons.Clear();
		_itemButtons.Clear();
		_tmButtons.Clear();
		_battleButtons.Clear();
		_cheatButtons.Clear();
		_mechanicButtons.Clear();
		_updateButtons.Clear();
		_legendaryButtons.Clear();

		int startX = xPositionOnScreen + 30;
		int startY = yPositionOnScreen + 44;

		// 8 Tabs across 840px (101px width + 4px gap)
		int tabW = 101;
		int tabGap = 4;
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX, startY, tabW, 40), "Pokemon", "1. Pokémon"));
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX + (tabW + tabGap), startY, tabW, 40), "Items", "2. Itens"));
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX + (tabW + tabGap) * 2, startY, tabW, 40), "TMs", "3. TMs"));
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX + (tabW + tabGap) * 3, startY, tabW, 40), "Battle", "4. Batalha"));
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX + (tabW + tabGap) * 4, startY, tabW, 40), "Cheats", "5. Extras"));
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX + (tabW + tabGap) * 5, startY, tabW, 40), "Mechanics", "6. Mecânicas"));
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX + (tabW + tabGap) * 6, startY, tabW, 40), "UpdatesV2", "7. v2.0 Novos"));
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX + (tabW + tabGap) * 7, startY, tabW, 40), "Legendaries", "8. Lendários"));

		int contentY = startY + 50;

		// 1. Level selector buttons (for Pokemon Tab)
		for (int l = 0; l < DebugLevels.Length; l++)
		{
			int lvl = DebugLevels[l];
			_levelButtons.Add(new ClickableComponent(new Rectangle(startX + 30 + l * 32, contentY, 30, 28), lvl.ToString(), $"L{lvl}"));
		}

		// Category filter buttons (Gen 1-9, Região, Tipo, Estilo, Ovo 10 passos)
		_genFilterButtons.Add(new ClickableComponent(new Rectangle(startX + 225, contentY, 68, 28), "gen_filter", _filterGen == 0 ? "Gen:All" : $"Gen {_filterGen}"));
		_regionFilterButtons.Add(new ClickableComponent(new Rectangle(startX + 296, contentY, 86, 28), "region_filter", $"Reg:{_filterRegion}"));
		_typeFilterButtons.Add(new ClickableComponent(new Rectangle(startX + 385, contentY, 84, 28), "type_filter", $"Tipo:{_filterType}"));
		_styleFilterButtons.Add(new ClickableComponent(new Rectangle(startX + 472, contentY, 108, 28), "style_filter", GetStyleLabel()));
		_eggToggleButton = new ClickableComponent(new Rectangle(startX + 583, contentY, 96, 28), "egg_toggle", _spawnAsEgg ? "🥚 OVO: ON(10p)" : "🥚 OVO: OFF");

		// Pagination buttons for Pokemon Tab
		_prevPageButton = new ClickableComponent(new Rectangle(startX + 752, contentY, 38, 28), "prev_page", "◄");
		_nextPageButton = new ClickableComponent(new Rectangle(startX + 794, contentY, 38, 28), "next_page", "►");

		// 2. Pokemon Grid (3 columns x 6 rows = 18 per page)
		var allSpecies = GetFilteredSpecies();

		int maxPage = Math.Max(0, (allSpecies.Count - 1) / 18);
		if (_speciesPage > maxPage) _speciesPage = maxPage;
		if (_speciesPage < 0) _speciesPage = 0;

		int startIndex = _speciesPage * 18;
		int countOnPage = Math.Min(18, allSpecies.Count - startIndex);
		int pStartY = contentY + 38;

		for (int i = 0; i < countOnPage; i++)
		{
			var sp = allSpecies[startIndex + i];
			int col = i % 3;
			int row = i / 3;
			int bx = startX + col * 280;
			int by = pStartY + row * 64;
			_pokemonButtons.Add(new ClickableComponent(new Rectangle(bx, by, 272, 58), sp.Name));
		}

		// 3. Item Buttons (compact 40px height rows to fit all new Gimmick Rings, Mega Stones & Z-Crystals)
		int itemStartY = contentY + 6;
		int btnW = 408;
		int col2X = startX + 424;
		int rowStep = 44;
		_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY, btnW, 40), "pack_balls", "+20 De Todas Poké Bolas (21 Tipos)"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY, btnW, 40), "pack_potions", "+10 Potion, Super Potion & Revive"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep, btnW, 40), "pack_seeds", "+20 De Todas Sementes de Apricorn"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep, btnW, 40), "pack_apricorns", "+20 De Todos Apricorns Colhidos"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 2, btnW, 40), "pack_candy_repel", "+10 Rare/Friend Candy & Repels"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 2, btnW, 40), "pack_field_tms", "+1 Apenas TMs de Campo (HMs)"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 3, btnW, 40), "pack_tms", "+1 De Todos os TMs (Gen 1 a Gen 9)"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 3, btnW, 40), "super_rod", "+1 Vara Super Rod (Pescar Pokémon)"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 4, btnW, 40), "healing_machine", "+1 Mesa de Recuperação Pokémon"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 4, btnW, 40), "pack_stones", "+5 De Todas Pedras de Evolução"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 5, btnW, 40), "pokemon_pc", "💻 +1 PC de Pokémon (Ou Abrir PC)"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 5, btnW, 40), "trade_terminal", "🔄 +1 Terminal de Troca (Ou Abrir)"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 6, btnW, 40), "exp_share_ring", "💍 +1 Anel Exp. Share (Equipável - Gen 6)"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 6, btnW, 40), "gimmick_rings", "💍 +1 Anéis: Mega Bracelete, Z-Ring & Dynamax"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 7, btnW, 40), "choose_mega_stone", "🧬 Escolher Mega Pedra Individual (Seletor)"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 7, btnW, 40), "choose_z_crystal", "💎 Escolher Cristal Z Individual (Seletor)"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 8, btnW, 40), "choose_held_item", "🎒 Escolher Held Item Individual (Seletor)"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 8, btnW, 40), "pack_held_items", "📦 +1 De Todos os Held Items Competitivos"));
		_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 9, btnW * 2 + 16, 42), "all_items", "★ Gerar TODOS os Itens, Anéis, Mega Pedras e Cristais Z de uma vez"));

		// 4. TMs & HMs Controls and Grid
		string filterLabel = _filterFieldMovesOnly ? "★ TMs Campo (HMs)" : "Todos os TMs";
		_tmFilterButton = new ClickableComponent(new Rectangle(startX, contentY, 150, 30), "tm_filter", filterLabel);
		_tmTypeFilterButton = new ClickableComponent(new Rectangle(startX + 156, contentY, 125, 30), "tm_type_filter", $"Tipo: {_filterType}");
		_giveFieldTmsButton = new ClickableComponent(new Rectangle(startX + 286, contentY, 145, 30), "give_field_tms", "+1 TMs Campo");
		_giveAllTmsButton = new ClickableComponent(new Rectangle(startX + 436, contentY, 120, 30), "give_all_tms", "+1 Todos TMs");
		_tmPrevPageButton = new ClickableComponent(new Rectangle(startX + 730, contentY, 48, 30), "tm_prev", "◄");
		_tmNextPageButton = new ClickableComponent(new Rectangle(startX + 784, contentY, 48, 30), "tm_next", "►");

		var allMoves = PokemonMove.AllMoveNames();
		if (_filterFieldMovesOnly)
		{
			allMoves = allMoves.FindAll(m => PokemonMove.Get(m).GetFieldAbility() != FieldAbilityType.None);
		}
		if (_filterType != "Todos")
		{
			allMoves = allMoves.FindAll(m => string.Equals(PokemonMove.Get(m).Type.ToString(), _filterType, StringComparison.OrdinalIgnoreCase));
		}

		int tmMaxPage = Math.Max(0, (allMoves.Count - 1) / 18);
		if (_tmPage > tmMaxPage) _tmPage = tmMaxPage;
		if (_tmPage < 0) _tmPage = 0;

		int tmStartIndex = _tmPage * 18;
		int tmCountOnPage = Math.Min(18, allMoves.Count - tmStartIndex);
		int tmGridStartY = contentY + 38;

		for (int i = 0; i < tmCountOnPage; i++)
		{
			string moveName = allMoves[tmStartIndex + i];
			int col = i % 3;
			int row = i / 3;
			int bx = startX + col * 280;
			int by = tmGridStartY + row * 64;
			_tmButtons.Add(new ClickableComponent(new Rectangle(bx, by, 272, 58), moveName));
		}

		// 5. Battle Buttons
		int bStartY = contentY + 12;
		_battleButtons.Add(new ClickableComponent(new Rectangle(startX, bStartY, btnW, 40), "wild_pikachu", "Selvagem: Pikachu (Lv. 15)"));
		_battleButtons.Add(new ClickableComponent(new Rectangle(col2X, bStartY, btnW, 40), "wild_gyarados", "Selvagem: Gyarados (Lv. 25)"));
		_battleButtons.Add(new ClickableComponent(new Rectangle(startX, bStartY + 44, btnW * 2 + 16, 40), "trainer_custom", "⚔️ Escolher Pokémon Selvagem Sob Demanda (Abre Seletor)"));
		_battleButtons.Add(new ClickableComponent(new Rectangle(startX, bStartY + 88, btnW * 2 + 16, 40), "sim_player_battle", "🎮 Simular Batalha vs Treinador Player (Escolher Avatar Jogável)"));

		// Paginated NPC trainers
		var allNpcs = NPCTeams.GetAllNPCNames();
		int npcsPerPage = 8;
		int maxBattlePages = Math.Max(0, (allNpcs.Count - 1) / npcsPerPage);
		if (_battlePage > maxBattlePages) _battlePage = maxBattlePages;
		if (_battlePage < 0) _battlePage = 0;

		int npcGridY = bStartY + 168;
		_battlePrevPageButton = new ClickableComponent(new Rectangle(startX, npcGridY - 32, 140, 28), "battle_prev", "◄ Treinadores");
		_battleNextPageButton = new ClickableComponent(new Rectangle(startX + 692, npcGridY - 32, 140, 28), "battle_next", "Treinadores ►");

		int startNpcIdx = _battlePage * npcsPerPage;
		int countNpc = Math.Min(npcsPerPage, allNpcs.Count - startNpcIdx);
		for (int i = 0; i < countNpc; i++)
		{
			string npcName = allNpcs[startNpcIdx + i];
			int col = i % 2;
			int row = i / 2;
			int bx = col == 0 ? startX : col2X;
			int by = npcGridY + row * 48;
			_battleButtons.Add(new ClickableComponent(new Rectangle(bx, by, btnW, 42), "trainer_" + npcName, $"Treinador: {npcName} (6 Pokémon)"));
		}

		// 6. Cheat Buttons
		int cStartY = contentY + 14;
		_cheatButtons.Add(new ClickableComponent(new Rectangle(startX, cStartY, btnW, 46), "heal_team", "💖 Curar Equipe Completa (HP/PP/Status 100%)"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(col2X, cStartY, btnW, 46), "cycle_skin", $"👤 Treinador Batalha: {PokemonUI.TrainerSkin} (Seletor)"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(startX, cStartY + 52, btnW, 46), "weather_rain", "🌧️ Forçar Chuva Imediata"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(col2X, cStartY + 52, btnW, 46), "weather_sun", "☀️ Forçar Céu Ensolarado"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(startX, cStartY + 104, btnW, 46), "give_money", "💰 Ganhar +10.000 Ouros"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(col2X, cStartY + 104, btnW, 46), "cycle_shiny_chance", $"✨ Chance Shiny Selvagem: {PokemonEncounterManager.WildShinyChancePercent:F0}%"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(startX, cStartY + 156, btnW, 46), "open_dwarf_hints", "⛏️ Abrir Loja de Dicas do Anão (Pokédex)"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(col2X, cStartY + 156, btnW, 46), "reset_dwarf_daily", "🔄 Resetar Limite Diário de Dicas do Anão"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(startX, cStartY + 208, btnW, 46), "unlock_seen_dex", "👁️ Marcar Todos os 1025 Pokémon como VISTOS"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(col2X, cStartY + 208, btnW, 46), "unlock_caught_dex", "📖 Completar Pokédex Nacional (1025 Capturados)"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(startX, cStartY + 260, btnW, 46), "cycle_poke_music", $"🎵 Tocar Música Pokémon: {PokemonMusicManager.CurrentTrack}"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(col2X, cStartY + 260, btnW, 46), "toggle_overworld_music", $"🎶 Música Pokémon no Mapa: {(PokemonMusicManager.EnableOverworldPokemonMusic ? "ON" : "OFF")}"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(startX, cStartY + 312, btnW * 2 + 16, 44), "reset_dex", "🔄 Resetar Toda a Pokédex (Zerar Vistos e Capturados)"));
		_cheatButtons.Add(new ClickableComponent(new Rectangle(startX, cStartY + 364, btnW * 2 + 16, 44), "max_friendship", "💖 Maximizar Amizade de Toda a Equipe (255/255)"));

		// 7. Mechanics Buttons (Tab 6: 8 Funções)
		int mStartY = contentY + 12;
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY, btnW, 40), "mech_learnset_canonical", "1. 📖 Ensinar Golpes Canônicos Nv. (Equipe)"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(col2X, mStartY, btnW, 40), "mech_level_team", "1. 📈 +10 Níveis & Novos Golpes Canônicos"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY + 44, btnW, 40), "mech_validate_tms", "2. 🧬 Validar Restrições Biológicas TM/HM"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(col2X, mStartY + 44, btnW, 40), "mech_give_all_tms", "2. 💿 Obter Todos os TMs e HMs Canônicos"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY + 88, btnW, 40), "mech_cycle_status", "3. ⚡ Aplicar Condições de Status na Equipe"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(col2X, mStartY + 88, btnW, 40), "mech_apply_confusion", "3. 💫 Aplicar Confusão Secundária na Equipe"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY + 132, btnW, 40), "mech_give_crops", "4. 🌱 +20 Mudas de Apricorn & Berries"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(col2X, mStartY + 132, btnW, 40), "mech_grow_all_crops", "4. 🌾 Acelerar Colheita de Plantações"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY + 176, btnW, 40), "mech_give_signature_z", "5. 💎 Obter 17 Cristais Z de Assinatura"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(col2X, mStartY + 176, btnW, 40), "mech_test_ultra_necrozma", "5. 🌟 Necrozma + Ultranecrozium Z"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY + 220, btnW, 40), "mech_give_geodes", "6. ⛏️ Obter Geodos (Magma/Gelo/Omni/Norm)"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(col2X, mStartY + 220, btnW, 40), "mech_test_geode_pity", "6. 🎲 Testar Sistema de Pity de Geodos"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY + 264, btnW, 40), "mech_give_friend_candy", "7. 🍬 +10 Friendship Candies (Amizade 255)"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(col2X, mStartY + 264, btnW, 40), "mech_trigger_friend_evo", "7. 💖 Iniciar Teste de Evolução Amizade"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY + 308, btnW, 40), "mech_toggle_spirits_luck", "7. 🍀 Alternar Sorte dos Espíritos (Spawns)"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(col2X, mStartY + 308, btnW, 40), "mech_give_tera_pack", "8. 🔮 Orbe Tera + 50 de Todos Tera Shards"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY + 352, btnW * 2 + 16, 42), "mech_cycle_tera_type", "8. 💎 Ciclar Tera Type do Pokémon Ativo da Equipe"));

		// 8. UpdatesV2 Buttons (Tab 7: Novidades v2.0, v1.4 e v1.3)
		int uStartY = contentY + 2;
		int uRowStep = 41;
		_updateButtons.Add(new ClickableComponent(new Rectangle(startX, uStartY, btnW, 38), "v2_battle_red", "🔴 Batalhar vs Campeão Red (Cume - Nv. 50)"));
		_updateButtons.Add(new ClickableComponent(new Rectangle(col2X, uStartY, btnW, 38), "v2_battle_steven", "💎 Batalhar vs Mestre Steven Stone (Cume - Nv. 40)"));

		_updateButtons.Add(new ClickableComponent(new Rectangle(startX, uStartY + uRowStep, btnW, 38), "v2_warp_summit", "🚀 Teleportar para o Cume da Montanha (Summit)"));
		_updateButtons.Add(new ClickableComponent(new Rectangle(col2X, uStartY + uRowStep, btnW, 38), "v2_test_npc_reactions", "🗣️ Testar Reatividade dos Aldeões (Companheiro)"));

		_updateButtons.Add(new ClickableComponent(new Rectangle(startX, uStartY + uRowStep * 2, btnW, 38), "v2_battle_rain", "🌧️ Batalha com Clima: Chuva (Rain - 5 Turnos)"));
		_updateButtons.Add(new ClickableComponent(new Rectangle(col2X, uStartY + uRowStep * 2, btnW, 38), "v2_battle_sun", "☀️ Batalha com Clima: Sol Forte (Harsh Sun - 5t)"));

		_updateButtons.Add(new ClickableComponent(new Rectangle(startX, uStartY + uRowStep * 3, btnW, 38), "v2_give_incubator_egg", "🥚 Obter Chocadeira (Incubator) + Ovos"));
		_updateButtons.Add(new ClickableComponent(new Rectangle(col2X, uStartY + uRowStep * 3, btnW, 38), "v2_warp_coop_incubator", "🏠 Teleportar p/ Chocadeira (Coop / Fazenda)"));

		_updateButtons.Add(new ClickableComponent(new Rectangle(startX, uStartY + uRowStep * 4, btnW, 38), "v2_daycare_setup", "💞 Configurar Pais no Daycare (Pikachu F+M)"));
		_updateButtons.Add(new ClickableComponent(new Rectangle(col2X, uStartY + uRowStep * 4, btnW, 38), "v2_daycare_collect", "🧺 Gerar & Coletar Ovo Imediato do Daycare"));

		_updateButtons.Add(new ClickableComponent(new Rectangle(startX, uStartY + uRowStep * 5, btnW, 38), "v2_warp_daycare", "🐎 Teleportar para o Daycare (Rancho Marnie)"));
		_updateButtons.Add(new ClickableComponent(new Rectangle(col2X, uStartY + uRowStep * 5, btnW, 38), "v2_hatch_1_step", "🔥 Acelerar Ovos da Equipe (Falta 1 Passo)"));

		_updateButtons.Add(new ClickableComponent(new Rectangle(startX, uStartY + uRowStep * 6, btnW, 38), "v2_give_linking_cord", "🔗 +5 Cabos de Ligação & 9 Itens de Troca"));
		_updateButtons.Add(new ClickableComponent(new Rectangle(col2X, uStartY + uRowStep * 6, btnW, 38), "v2_test_solo_evo", "⚡ Testar Evolução Solo do Pokémon Ativo"));

		_updateButtons.Add(new ClickableComponent(new Rectangle(startX, uStartY + uRowStep * 7, btnW, 38), "v2_pc_full_restore", "🏥 Executar Rotina de Cura Total do PC Box"));
		_updateButtons.Add(new ClickableComponent(new Rectangle(col2X, uStartY + uRowStep * 7, btnW, 38), "v2_test_all_gimmick_revert", "🔄 Reverter Todas as Gimmicks Ativas"));

		_updateButtons.Add(new ClickableComponent(new Rectangle(startX, uStartY + uRowStep * 8, btnW, 38), "v2_inspect_rates", "📊 Inspecionar Catch Rate & EXP Canônico"));
		_updateButtons.Add(new ClickableComponent(new Rectangle(col2X, uStartY + uRowStep * 8, btnW, 38), "v2_test_catch_formula", "🎯 Simular Equação Canônica de Captura"));

		// 9. Legendaries Buttons (Tab 8)
		if (_currentTab == Tab.Legendaries)
		{
			int legCardsPerPage = 10;
			int totalLegs = AllLegendariesList.Length;
			int maxLegPages = Math.Max(0, (totalLegs - 1) / legCardsPerPage);
			_legendaryPage = Math.Clamp(_legendaryPage, 0, maxLegPages);

			int startLegIdx = _legendaryPage * legCardsPerPage;
			int endLegIdx = Math.Min(totalLegs, startLegIdx + legCardsPerPage);

			int lStartY = contentY + 2;
			int cardH = 68;
			int rowGap = 6;

			for (int idx = startLegIdx; idx < endLegIdx; idx++)
			{
				int slot = idx - startLegIdx;
				int col = slot % 2;
				int row = slot / 2;
				int cardX = (col == 0) ? startX : col2X;
				int cardY = lStartY + row * (cardH + rowGap);

				string legName = AllLegendariesList[idx];
				_legendaryButtons.Add(new ClickableComponent(
					new Rectangle(cardX + 228, cardY + 14, 86, 38),
					"leg_boss_" + legName,
					"⚔️ Boss"
				));
				_legendaryButtons.Add(new ClickableComponent(
					new Rectangle(cardX + 318, cardY + 14, 86, 38),
					"leg_habitat_" + legName,
					"🧭 Habitat"
				));
			}

			_legPrevPageButton = new ClickableComponent(new Rectangle(startX + 180, lStartY + 5 * (cardH + rowGap) + 8, 140, 36), "leg_prev", "◀ Anterior");
			_legNextPageButton = new ClickableComponent(new Rectangle(startX + 520, lStartY + 5 * (cardH + rowGap) + 8, 140, 36), "leg_next", "Próximo ▶");
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);

		if (_closeButton.containsPoint(x, y))
		{
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
			return;
		}

		// Tabs
		foreach (var tab in _tabButtons)
		{
			if (tab.containsPoint(x, y))
			{
				Game1.playSound("smallSelect");
				_currentTab = Enum.Parse<Tab>(tab.name);
				if (_currentTab != Tab.Pokemon) _selectingWildBattleTarget = false;
				SetupButtons();
				return;
			}
		}

		// Tab 1: Pokemon
		if (_currentTab == Tab.Pokemon)
		{
			if (_prevPageButton != null && _prevPageButton.containsPoint(x, y))
			{
				var all = GetFilteredSpecies();
				int maxPage = Math.Max(0, (all.Count - 1) / 18);
				_speciesPage = (_speciesPage - 1 + (maxPage + 1)) % (maxPage + 1);
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}

			if (_nextPageButton != null && _nextPageButton.containsPoint(x, y))
			{
				var all = GetFilteredSpecies();
				int maxPage = Math.Max(0, (all.Count - 1) / 18);
				_speciesPage = (_speciesPage + 1) % (maxPage + 1);
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}

			foreach (var gb in _genFilterButtons)
			{
				if (gb.containsPoint(x, y))
				{
					_filterGen = (_filterGen + 1) % 10; // 0, 1..9
					_speciesPage = 0;
					Game1.playSound("smallSelect");
					SetupButtons();
					return;
				}
			}

			foreach (var rb in _regionFilterButtons)
			{
				if (rb.containsPoint(x, y))
				{
					int idx = Array.IndexOf(AllRegionsList, _filterRegion);
					_filterRegion = AllRegionsList[(idx + 1) % AllRegionsList.Length];
					_speciesPage = 0;
					Game1.playSound("smallSelect");
					SetupButtons();
					return;
				}
			}

			foreach (var tb in _typeFilterButtons)
			{
				if (tb.containsPoint(x, y))
				{
					int idx = Array.IndexOf(AllTypesList, _filterType);
					_filterType = AllTypesList[(idx + 1) % AllTypesList.Length];
					_speciesPage = 0;
					Game1.playSound("smallSelect");
					SetupButtons();
					return;
				}
			}

			foreach (var sb in _styleFilterButtons)
			{
				if (sb.containsPoint(x, y))
				{
					_filterStyle = (StyleFilter)(((int)_filterStyle + 1) % 4);
					_speciesPage = 0;
					Game1.playSound("smallSelect");
					SetupButtons();
					return;
				}
			}

			if (_eggToggleButton != null && _eggToggleButton.containsPoint(x, y))
			{
				_spawnAsEgg = !_spawnAsEgg;
				Game1.playSound("smallSelect");
				SetupButtons();
				return;
			}

			foreach (var lvlBtn in _levelButtons)
			{
				if (lvlBtn.containsPoint(x, y))
				{
					Game1.playSound("select");
					_selectedLevel = int.Parse(lvlBtn.name);
					return;
				}
			}

			foreach (var pBtn in _pokemonButtons)
			{
				if (pBtn.containsPoint(x, y))
				{
					string speciesName = pBtn.name;
					var species = PokemonSpecies.Get(speciesName);
					bool spawnShiny = (_filterStyle == StyleFilter.Shiny || _filterStyle == StyleFilter.MegaShiny);
					var inst = new PokemonInstance(species, _selectedLevel, spawnShiny);
					if (species.IsMega)
					{
						inst.IsMegaEvolved = true;
						if (!string.IsNullOrWhiteSpace(species.BaseSpeciesName))
							inst.PreMegaSpeciesInternalName = species.BaseSpeciesName;
					}

					if (_selectingWildBattleTarget)
					{
						_selectingWildBattleTarget = false;
						if (Party.Team.Count == 0)
						{
							Party.AddPokemon(new PokemonInstance(PokemonSpecies.Get("Pikachu"), _selectedLevel));
						}
						Game1.playSound("flute");
						Game1.activeClickableMenu = new PokemonBattleMenu(inst, isWild: true);
						return;
					}

					if (_spawnAsEgg)
					{
						inst.IsEgg = true;
						inst.EggSteps = 10;
						inst.Nickname = "Ovo Pokémon";
						Party.AddPokemon(inst);
						Game1.playSound("reward");
						Game1.addHUDMessage(new HUDMessage($"🥚 Ovo de {species.Name} (10 passos p/ chocar) adicionado!", 2));
						return;
					}

					Party.AddPokemon(inst);
					Game1.playSound("reward");
					return;
				}
			}
		}

		// Tab 2: Items
		else if (_currentTab == Tab.Items)
		{
			foreach (var ib in _itemButtons)
			{
				if (ib.containsPoint(x, y))
				{
					HandleItemSpawn(ib.name);
					return;
				}
			}
		}

		// Tab 3: TMs & HMs
		else if (_currentTab == Tab.TMs)
		{
			if (_tmFilterButton != null && _tmFilterButton.containsPoint(x, y))
			{
				_filterFieldMovesOnly = !_filterFieldMovesOnly;
				_tmPage = 0;
				Game1.playSound("smallSelect");
				SetupButtons();
				return;
			}

			if (_tmTypeFilterButton != null && _tmTypeFilterButton.containsPoint(x, y))
			{
				int idx = Array.IndexOf(AllTypesList, _filterType);
				_filterType = AllTypesList[(idx + 1) % AllTypesList.Length];
				_tmPage = 0;
				Game1.playSound("smallSelect");
				SetupButtons();
				return;
			}

			if (_giveFieldTmsButton != null && _giveFieldTmsButton.containsPoint(x, y))
			{
				HandleItemSpawn("pack_field_tms");
				return;
			}

			if (_giveAllTmsButton != null && _giveAllTmsButton.containsPoint(x, y))
			{
				HandleItemSpawn("pack_tms");
				return;
			}

			if (_tmPrevPageButton != null && _tmPrevPageButton.containsPoint(x, y))
			{
				var allMoves = PokemonMove.AllMoveNames();
				if (_filterFieldMovesOnly) allMoves = allMoves.FindAll(m => PokemonMove.Get(m).GetFieldAbility() != FieldAbilityType.None);
				int maxPage = Math.Max(0, (allMoves.Count - 1) / 18);
				_tmPage = (_tmPage - 1 + (maxPage + 1)) % (maxPage + 1);
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}

			if (_tmNextPageButton != null && _tmNextPageButton.containsPoint(x, y))
			{
				var allMoves = PokemonMove.AllMoveNames();
				if (_filterFieldMovesOnly) allMoves = allMoves.FindAll(m => PokemonMove.Get(m).GetFieldAbility() != FieldAbilityType.None);
				int maxPage = Math.Max(0, (allMoves.Count - 1) / 18);
				_tmPage = (_tmPage + 1) % (maxPage + 1);
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}

			foreach (var tb in _tmButtons)
			{
				if (tb.containsPoint(x, y))
				{
					string moveName = tb.name;
					string tmId = PokemonItemUse.TmId(moveName);
					Game1.player.addItemToInventory(new StardewValley.Object("(O)" + tmId, 1));
					Game1.playSound("coin");
					Game1.addHUDMessage(new HUDMessage($"TM {moveName} adicionado!", 2));
					return;
				}
			}
		}

		// Tab 4: Battle
		else if (_currentTab == Tab.Battle)
		{
			if (_battlePrevPageButton != null && _battlePrevPageButton.containsPoint(x, y))
			{
				var allNpcs = NPCTeams.GetAllNPCNames();
				int maxBattlePages = Math.Max(0, (allNpcs.Count - 1) / 8);
				_battlePage = (_battlePage - 1 + (maxBattlePages + 1)) % (maxBattlePages + 1);
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}

			if (_battleNextPageButton != null && _battleNextPageButton.containsPoint(x, y))
			{
				var allNpcs = NPCTeams.GetAllNPCNames();
				int maxBattlePages = Math.Max(0, (allNpcs.Count - 1) / 8);
				_battlePage = (_battlePage + 1) % (maxBattlePages + 1);
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}

			foreach (var bb in _battleButtons)
			{
				if (bb.containsPoint(x, y))
				{
					HandleBattleTrigger(bb.name);
					return;
				}
			}
		}

		// Tab 5: Cheats
		else if (_currentTab == Tab.Cheats)
		{
			foreach (var cb in _cheatButtons)
			{
				if (cb.containsPoint(x, y))
				{
					HandleCheat(cb.name);
					return;
				}
			}
		}

		// Tab 6: Mechanics (8 Funções)
		else if (_currentTab == Tab.Mechanics)
		{
			foreach (var mb in _mechanicButtons)
			{
				if (mb.containsPoint(x, y))
				{
					HandleMechanicTest(mb.name);
					return;
				}
			}
		}

		// Tab 7: UpdatesV2 (Novidades v2.0, v1.4, v1.3)
		else if (_currentTab == Tab.UpdatesV2)
		{
			foreach (var ub in _updateButtons)
			{
				if (ub.containsPoint(x, y))
				{
					HandleUpdateV2Action(ub.name);
					return;
				}
			}
		}

		// Tab 8: Legendaries
		else if (_currentTab == Tab.Legendaries)
		{
			if (_legPrevPageButton != null && _legPrevPageButton.containsPoint(x, y))
			{
				int totalLegs = AllLegendariesList.Length;
				int maxLegPages = Math.Max(0, (totalLegs - 1) / 10);
				_legendaryPage = (_legendaryPage - 1 + (maxLegPages + 1)) % (maxLegPages + 1);
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}
			if (_legNextPageButton != null && _legNextPageButton.containsPoint(x, y))
			{
				int totalLegs = AllLegendariesList.Length;
				int maxLegPages = Math.Max(0, (totalLegs - 1) / 10);
				_legendaryPage = (_legendaryPage + 1) % (maxLegPages + 1);
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}

			foreach (var lb in _legendaryButtons)
			{
				if (lb.containsPoint(x, y))
				{
					if (lb.name.StartsWith("leg_boss_"))
					{
						string monName = lb.name.Substring("leg_boss_".Length);
						var spec = PokemonSpecies.Get(monName);
						if (spec != null)
						{
							var wildInst = new PokemonInstance(spec, 70, isShiny: PokemonEncounterManager.RollWildShiny());
							Game1.playSound("thunder");
							var battle = new PokemonBattleMenu(wildInst, isWild: true);
							Game1.activeClickableMenu = battle;
						}
						return;
					}
					else if (lb.name.StartsWith("leg_habitat_") || lb.name.StartsWith("leg_spawn_"))
					{
						string monName = lb.name.StartsWith("leg_habitat_")
							? lb.name.Substring("leg_habitat_".Length)
							: lb.name.Substring("leg_spawn_".Length);

						var (loc, tx, ty, itemId, itemName) = GetLegendaryHabitat(monName);

						// Grant summon relic item
						if (!string.IsNullOrEmpty(itemId))
						{
							Game1.player.addItemToInventory(ItemRegistry.Create<Object>("(O)" + itemId) ?? new Object(itemId, 1));
						}

						// Teleport directly to lore-accurate Valley habitat
						Game1.warpFarmer(loc, tx, ty, 2);
						Game1.playSound("wand");
						exitThisMenuNoSound();
						Game1.addHUDMessage(new HUDMessage($"🧭 Teleportado para o habitat de {monName} em {loc} ({tx}, {ty})! Relíquia '{itemName}' concedida.", 2));
						return;
					}
				}
			}
		}
	}

	private static void GiveEquipRing(string ringItemId)
	{
		Item ring = ItemRegistry.Create("(O)" + ringItemId);
		if (!(ring is StardewValley.Objects.Ring))
		{
			ring = new StardewValley.Objects.Ring(ringItemId);
		}
		Game1.player.addItemToInventory(ring);
	}

	private void HandleItemSpawn(string action)
	{
		Game1.playSound("coin");

		if (action == "pack_balls" || action == "all_items")
		{
			string[] balls = new[]
			{
				"PokeBall", "GreatBall", "UltraBall", "MasterBall", "PremierBall",
				"LuxuryBall", "NetBall", "NestBall", "DuskBall", "DiveBall",
				"QuickBall", "TimerBall", "RepeatBall", "MoonBall", "LoveBall",
				"LevelBall", "FriendBall", "LureBall", "FastBall", "HeavyBall",
				"HealBall"
			};
			foreach (var b in balls) Game1.player.addItemToInventory(new StardewValley.Object("(O)" + b, 20));
		}

		if (action == "pack_master_ball")
		{
			Game1.player.addItemToInventory(new StardewValley.Object("(O)MasterBall", 10));
		}

		if (action == "exp_share_ring" || action == "all_items")
		{
			GiveEquipRing("PokemonExpShare");
		}

		if (action == "gimmick_rings" || action == "all_items")
		{
			GiveEquipRing("Poke_MegaBracelet");
			GiveEquipRing("Poke_ZRing");
			GiveEquipRing("Poke_ZPowerRing");
			GiveEquipRing("Poke_DynamaxBand");
			GiveEquipRing("Poke_TeraOrb");
		}

		if (action == "choose_mega_stone")
		{
			Game1.playSound("bigSelect");
			Game1.activeClickableMenu = new PokemonItemPickerMenu(this, PokemonItemPickerMenu.PickerMode.MegaStones);
			return;
		}

		if (action == "choose_z_crystal")
		{
			Game1.playSound("bigSelect");
			Game1.activeClickableMenu = new PokemonItemPickerMenu(this, PokemonItemPickerMenu.PickerMode.ZCrystals);
			return;
		}

		if (action == "choose_held_item")
		{
			Game1.playSound("bigSelect");
			Game1.activeClickableMenu = new PokemonItemPickerMenu(this, PokemonItemPickerMenu.PickerMode.HeldItems);
			return;
		}

		if (action == "pack_held_items" || action == "all_items")
		{
			foreach (var (id, name, desc, cost) in PokemonHeldItemManager.GetAllHeldItems())
			{
				Item it = ItemRegistry.Create("(O)" + id, 1, 0, allowNull: true) ?? new StardewValley.Object(id, 1);
				if (it != null) Game1.player.addItemToInventory(it);
			}
			if (action == "pack_held_items")
			{
				Game1.playSound("coin");
				Game1.addHUDMessage(new HUDMessage("Held Items adicionados ao inventário!", 1));
				return;
			}
		}

		if (action == "pack_mega_stones" || action == "all_items")
		{
			foreach (string stoneId in PokemonItemManager.AllMegaStoneIds)
			{
				Game1.player.addItemToInventory(new StardewValley.Object("(O)" + stoneId, 1));
			}
		}

		if (action == "pack_z_crystals" || action == "all_items")
		{
			foreach (string zId in PokemonItemManager.AllZCrystalIds)
			{
				Game1.player.addItemToInventory(new StardewValley.Object("(O)" + zId, 1));
			}
			foreach (string sigZId in PokemonItemManager.SignatureZCrystalDefinitions.Keys)
			{
				Game1.player.addItemToInventory(new StardewValley.Object("(O)" + sigZId, 1));
			}
			foreach (string shardId in PokemonItemManager.AllTeraShardIds)
			{
				Game1.player.addItemToInventory(new StardewValley.Object("(O)" + shardId, 50));
			}
		}

		if (action == "pack_potions" || action == "all_items")
		{
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonPotion", 10));
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonSuperPotion", 10));
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonRevive", 10));
		}

		if (action == "pack_seeds" || action == "all_items")
		{
			foreach (var color in PokemonItemManager.ApricornColors)
			{
				Game1.player.addItemToInventory(new StardewValley.Object($"(O)Poke{color}Seed", 20));
			}
		}

		if (action == "pack_apricorns" || action == "all_items")
		{
			foreach (var color in PokemonItemManager.ApricornColors)
			{
				Game1.player.addItemToInventory(new StardewValley.Object($"(O)Poke{color}Apricorn", 20));
			}
		}

		if (action == "pack_candy_repel" || action == "all_items")
		{
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonRareCandy", 10));
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonFriendshipCandy", 10));
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonRepel", 10));
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonSuperRepel", 10));
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonMaxRepel", 10));
		}

		if (action == "pack_field_tms")
		{
			int count = 0;
			foreach (string move in PokemonMove.AllMoveNames())
			{
				var m = PokemonMove.Get(move);
				if (m.GetFieldAbility() != FieldAbilityType.None)
				{
					string tmId = PokemonItemUse.TmId(move);
					Game1.player.addItemToInventory(new StardewValley.Object("(O)" + tmId, 1));
					count++;
				}
			}
			Game1.addHUDMessage(new HUDMessage($"+{count} TMs de Campo (HMs) adicionados!", 2));
			return;
		}

		if (action == "pack_tms" || action == "all_items")
		{
			foreach (string move in PokemonMove.AllMoveNames())
			{
				string tmId = PokemonItemUse.TmId(move);
				Game1.player.addItemToInventory(new StardewValley.Object("(O)" + tmId, 1));
			}
		}

		if (action == "super_rod" || action == "all_items")
		{
			Game1.player.addItemToInventory(ItemRegistry.Create("(T)PokemonSuperRod"));
			PokemonItemManager.FixInventoryRods(Game1.player);
		}

		if (action == "healing_machine" || action == "all_items")
		{
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonHealingMachine", 1));
		}

		if (action == "pack_stones" || action == "all_items")
		{
			string[] stones = new[]
			{
				"PokemonWaterStone", "PokemonFireStone", "PokemonThunderStone", "PokemonLeafStone",
				"PokemonMoonStone", "PokemonSunStone", "PokemonShinyStone", "PokemonDuskStone",
				"PokemonDawnStone", "PokemonIceStone", "PokemonLinkCable"
			};
			foreach (var s in stones)
			{
				Game1.player.addItemToInventory(new StardewValley.Object("(O)" + s, 5));
			}
		}

		if (action == "pokemon_pc" || action == "all_items")
		{
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonPC", 1));
			if (action == "pokemon_pc")
			{
				Game1.activeClickableMenu = new PokemonStorageMenu();
				return;
			}
		}

		if (action == "trade_terminal" || action == "all_items")
		{
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonTradeTerminal", 1));
			if (action == "trade_terminal")
			{
				Game1.activeClickableMenu = new PokemonTradeMenu();
				return;
			}
		}

		Game1.addHUDMessage(new HUDMessage("Itens Pokémon gerados com sucesso!", 2));
	}

	private void HandleBattleTrigger(string action)
	{
		if (action == "trainer_custom")
		{
			Game1.playSound("smallSelect");
			_selectingWildBattleTarget = true;
			_currentTab = Tab.Pokemon;
			SetupButtons();
			Game1.addHUDMessage(new HUDMessage("Selecione qualquer Pokémon na lista para iniciar a batalha selvagem contra ele!", 2));
			return;
		}

		if (action == "sim_player_battle")
		{
			if (Party.Team.Count == 0)
			{
				Game1.showRedMessage("Adicione ao menos 1 Pokémon à sua equipe primeiro!");
				return;
			}
			Game1.playSound("bigSelect");
			Game1.activeClickableMenu = new PokemonTrainerSelectMenu(this, (skin) =>
			{
				var team = NPCTeams.GetTeam(skin);
				if (team != null && team.Count > 0)
				{
					Game1.activeClickableMenu = new PokemonBattleMenu(skin, team);
				}
				else
				{
					Game1.showRedMessage($"Time do treinador {skin} não encontrado!");
				}
			}, "ESCOLHER TREINADOR PLAYER (SIMULAÇÃO PVP)");
			return;
		}

		Game1.playSound("flute");

		if (Party.Team.Count == 0)
		{
			Game1.showRedMessage("Adicione ao menos 1 Pokémon à sua equipe primeiro!");
			return;
		}

		if (action == "wild_pikachu")
		{
			Game1.activeClickableMenu = new PokemonBattleMenu(new PokemonInstance(PokemonSpecies.Get("Pikachu"), 15));
			return;
		}

		if (action == "wild_gyarados")
		{
			Game1.activeClickableMenu = new PokemonBattleMenu(new PokemonInstance(PokemonSpecies.Get("Gyarados"), 25));
			return;
		}

		if (action.StartsWith("trainer_"))
		{
			string npc = action.Substring("trainer_".Length);
			var team = NPCTeams.GetTeam(npc);
			if (team != null)
			{
				Game1.activeClickableMenu = new PokemonBattleMenu(npc, team);
			}
			else
			{
				Game1.showRedMessage($"Time do treinador {npc} não encontrado!");
			}
			return;
		}
	}

	private void HandleCheat(string action)
	{
		if (action == "heal_team")
		{
			Party.HealAll();
			Game1.playSound("heal");
			Game1.addHUDMessage(new HUDMessage("Toda a sua equipe Pokémon foi restaurada!", 2));
		}
		else if (action == "cycle_skin")
		{
			Game1.playSound("bigSelect");
			Game1.activeClickableMenu = new PokemonTrainerSelectMenu(this);
		}
		else if (action == "cycle_shiny_chance")
		{
			int[] chances = new[] { 1, 2, 5, 10, 25, 50, 100 };
			int idx = Array.IndexOf(chances, PokemonEncounterManager.WildShinyChancePercent);
			PokemonEncounterManager.WildShinyChancePercent = chances[(idx + 1) % chances.Length];
			Game1.playSound("select");
			SetupButtons();
			Game1.addHUDMessage(new HUDMessage($"Chance Shiny Selvagem: {PokemonEncounterManager.WildShinyChancePercent}%", 2));
		}
		else if (action == "open_dwarf_hints")
		{
			Game1.playSound("bigSelect");
			Game1.activeClickableMenu = new PokemonDwarfHintMenu();
		}
		else if (action == "reset_dwarf_daily")
		{
			PokemonPokedex.Instance.ResetDailyHintPurchasesForDebug();
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage("Limite diário de dicas do Anão resetado!", 2));
		}
		else if (action == "unlock_seen_dex")
		{
			foreach (var sp in PokemonSpecies.AllSpecies())
			{
				PokemonPokedex.Instance.RecordSeen(sp.Name);
			}
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage("Todos os 1.025 Pokémon marcados como VISTOS na Pokédex!", 2));
		}
		else if (action == "unlock_caught_dex")
		{
			foreach (var sp in PokemonSpecies.AllSpecies())
			{
				PokemonPokedex.Instance.RecordCaught(sp.Name);
			}
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage("Pokédex Nacional 100% Completa (1.025 Capturados)!", 2));
		}
		else if (action == "weather_rain")
		{
			Game1.isRaining = true;
			Game1.weatherForTomorrow = "Rain";
			if (Game1.netWorldState?.Value != null)
			{
				Game1.netWorldState.Value.WeatherForTomorrow = "Rain";
				var curWeather = Game1.netWorldState.Value.GetWeatherForLocation(Game1.currentLocation?.GetLocationContextId() ?? "Default");
				if (curWeather != null)
				{
					curWeather.Weather = "Rain";
					curWeather.IsRaining = true;
					curWeather.IsSnowing = false;
					curWeather.IsLightning = false;
				}
			}
			Game1.debrisWeather?.Clear();
			Game1.playSound("thunder");
			Game1.changeMusicTrack("rain");
			Game1.addHUDMessage(new HUDMessage("Chuva imediata ativada!", 2));
		}
		else if (action == "weather_sun")
		{
			Game1.isRaining = false;
			Game1.weatherForTomorrow = "Sun";
			if (Game1.netWorldState?.Value != null)
			{
				Game1.netWorldState.Value.WeatherForTomorrow = "Sun";
				var curWeather = Game1.netWorldState.Value.GetWeatherForLocation(Game1.currentLocation?.GetLocationContextId() ?? "Default");
				if (curWeather != null)
				{
					curWeather.Weather = "Sun";
					curWeather.IsRaining = false;
					curWeather.IsSnowing = false;
					curWeather.IsLightning = false;
				}
			}
			Game1.stopMusicTrack(StardewValley.GameData.MusicContext.Default);
			Game1.debrisWeather?.Clear();
			Game1.playSound("fireball");
			Game1.addHUDMessage(new HUDMessage("Céu ensolarado ativado!", 2));
		}
		else if (action == "give_money")
		{
			Game1.player.Money += 10000;
			Game1.playSound("money");
			Game1.addHUDMessage(new HUDMessage("+10.000 Ouros adicionados!", 2));
		}
		else if (action == "cycle_poke_music")
		{
			PokemonMusicTrack next = PokemonMusicManager.CurrentTrack switch
			{
				PokemonMusicTrack.None            => PokemonMusicTrack.RouteOverworld,
				PokemonMusicTrack.RouteOverworld  => PokemonMusicTrack.WildBattle,
				PokemonMusicTrack.WildBattle      => PokemonMusicTrack.TrainerBattle,
				PokemonMusicTrack.TrainerBattle   => PokemonMusicTrack.MegaBossBattle,
				PokemonMusicTrack.MegaBossBattle  => PokemonMusicTrack.LegendaryBattle,
				PokemonMusicTrack.LegendaryBattle => PokemonMusicTrack.VictoryFanfare,
				_                                 => PokemonMusicTrack.None
			};
			if (next == PokemonMusicTrack.None)
			{
				PokemonMusicManager.StopAndRestoreStardewMusic();
				Game1.addHUDMessage(new HUDMessage("Música Pokémon parada (Stardew restaurada)!", 2));
			}
			else
			{
				PokemonMusicManager.PlayTrack(next, loop: next != PokemonMusicTrack.VictoryFanfare, pauseStardew: true);
				Game1.addHUDMessage(new HUDMessage($"🎵 Tocando faixa Pokémon: {next}", 2));
			}
			SetupButtons();
		}
		else if (action == "toggle_overworld_music")
		{
			PokemonMusicManager.EnableOverworldPokemonMusic = !PokemonMusicManager.EnableOverworldPokemonMusic;
			if (!PokemonMusicManager.EnableOverworldPokemonMusic && PokemonMusicManager.CurrentTrack == PokemonMusicTrack.RouteOverworld)
			{
				PokemonMusicManager.StopAndRestoreStardewMusic();
			}
			Game1.playSound("smallSelect");
			SetupButtons();
		}
		else if (action == "reset_dex")
		{
			PokemonPokedex.Instance.ResetPokedex();
			Game1.playSound("trashcan");
			Game1.addHUDMessage(new HUDMessage("Pokédex resetada com sucesso! Todos os registros foram zerados.", 2));
		}
		else if (action == "max_friendship")
		{
			foreach (var p in Party.GetParty())
			{
				p.AdjustFriendship(255);
			}
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage("Amizade de toda a equipe Pokémon maximizada (255/255)!", 2));
		}
	}

	private void HandleMechanicTest(string action)
	{
		Game1.playSound("coin");

		// 1. Learnsets Canônicos
		if (action == "mech_learnset_canonical")
		{
			var party = Party.GetParty();
			if (party.Count == 0)
			{
				Game1.showRedMessage("Adicione ao menos 1 Pokémon à sua equipe primeiro!");
				return;
			}
			int updated = 0;
			foreach (var mon in party)
			{
				var moves = PokemonCanonicalData.GetMovesForInitialLevel(mon.Species.Name, mon.Level);
				if (moves != null && moves.Count > 0)
				{
					mon.Moves.Clear();
					foreach (var mName in moves)
					{
						var mv = PokemonMove.Get(mName);
						if (mv != null) mon.Moves.Add(mv);
					}
					updated++;
				}
			}
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage($"1. Golpes canônicos por nível atualizados para {updated} Pokémon!", 2));
		}
		else if (action == "mech_level_team")
		{
			var party = Party.GetParty();
			if (party.Count == 0)
			{
				Game1.showRedMessage("Adicione ao menos 1 Pokémon à sua equipe primeiro!");
				return;
			}
			foreach (var mon in party)
			{
				mon.Level = Math.Min(100, mon.Level + 10);
				mon.RecalculateStats();
				var newMoves = PokemonCanonicalData.GetMovesForInitialLevel(mon.Species.Name, mon.Level);
				if (newMoves != null)
				{
					foreach (var nm in newMoves)
					{
						if (!mon.Moves.Exists(m => string.Equals(m.Name, nm, StringComparison.OrdinalIgnoreCase)))
						{
							var mv = PokemonMove.Get(nm);
							if (mv != null)
							{
								if (mon.Moves.Count < 4) mon.Moves.Add(mv);
								else mon.Moves[0] = mv;
							}
						}
					}
				}
			}
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage("1. Equipe subiu +10 níveis! Golpes canônicos e atributos recalculados.", 2));
		}

		// 2. TM/HM e Matriz Biológica
		else if (action == "mech_validate_tms")
		{
			var party = Party.GetParty();
			var lead = party.Count > 0 ? party[0] : null;
			if (lead == null)
			{
				Game1.showRedMessage("Adicione um Pokémon líder à equipe primeiro!");
				return;
			}
			bool canSurf = PokemonCanonicalData.IsCanonicallyTeachable(lead.Species.Name, "Surf");
			bool canFly = PokemonCanonicalData.IsCanonicallyTeachable(lead.Species.Name, "Fly");
			bool canCut = PokemonCanonicalData.IsCanonicallyTeachable(lead.Species.Name, "Cut");
			bool canToxic = PokemonCanonicalData.IsCanonicallyTeachable(lead.Species.Name, "Toxic");
			string report = $"{lead.Nickname ?? lead.Species.Name}: Surf={(canSurf ? "SIM" : "NÃO")}, Fly={(canFly ? "SIM" : "NÃO")}, Cut={(canCut ? "SIM" : "NÃO")}, Toxic={(canToxic ? "SIM" : "NÃO")}";
			Game1.playSound("select");
			Game1.addHUDMessage(new HUDMessage($"2. Matriz Biológica: {report}", 2));
		}
		else if (action == "mech_give_all_tms")
		{
			HandleItemSpawn("pack_tms");
			HandleItemSpawn("pack_field_tms");
			Game1.addHUDMessage(new HUDMessage("2. Todos os TMs e HMs canônicos adicionados ao inventário!", 2));
		}

		// 3. Condições de Status & Berries
		else if (action == "mech_cycle_status")
		{
			var party = Party.GetParty();
			var lead = party.Count > 0 ? party[0] : null;
			if (lead == null)
			{
				Game1.showRedMessage("Adicione um Pokémon líder à equipe primeiro!");
				return;
			}
			lead.Status = lead.Status switch
			{
				PokemonStatusCondition.None => PokemonStatusCondition.Burn,
				PokemonStatusCondition.Burn => PokemonStatusCondition.Paralysis,
				PokemonStatusCondition.Paralysis => PokemonStatusCondition.Freeze,
				PokemonStatusCondition.Freeze => PokemonStatusCondition.Sleep,
				PokemonStatusCondition.Sleep => PokemonStatusCondition.Poison,
				PokemonStatusCondition.Poison => PokemonStatusCondition.Toxic,
				_ => PokemonStatusCondition.None
			};
			lead.StatusTurns = (lead.Status == PokemonStatusCondition.Sleep) ? 3 : 0;
			Game1.playSound("grunt");
			Game1.addHUDMessage(new HUDMessage($"3. Status do líder alterado para: {lead.Status}!", 2));
		}
		else if (action == "mech_apply_confusion")
		{
			var party = Party.GetParty();
			var lead = party.Count > 0 ? party[0] : null;
			if (lead == null)
			{
				Game1.showRedMessage("Adicione um Pokémon líder à equipe primeiro!");
				return;
			}
			lead.ConfusionTurns = (lead.ConfusionTurns > 0) ? 0 : 3;
			Game1.playSound("wand");
			Game1.addHUDMessage(new HUDMessage($"3. Confusão no líder: {(lead.ConfusionTurns > 0 ? "ATIVADA (3 turnos)" : "CURADA")}", 2));
		}

		// 4. Plantação de Mudas e Berries
		else if (action == "mech_give_crops")
		{
			foreach (var c in PokemonItemManager.ApricornColors)
			{
				Game1.player.addItemToInventory(new StardewValley.Object($"(O)Poke{c}Seed", 20));
				Game1.player.addItemToInventory(new StardewValley.Object($"(O)Poke{c}Apricorn", 20));
			}
			string[] berries = new[] { "OranBerry", "SitrusBerry", "LumBerry", "CheriBerry", "ChestoBerry", "PechaBerry", "RawstBerry", "AspearBerry", "LeppaBerry", "PersimBerry" };
			foreach (var b in berries)
			{
				Game1.player.addItemToInventory(new StardewValley.Object("(O)" + b, 20));
			}
			Game1.playSound("coin");
			Game1.addHUDMessage(new HUDMessage("4. Mudas de Apricorn e Berries adicionadas ao inventário!", 2));
		}
		else if (action == "mech_grow_all_crops")
		{
			var locs = new List<GameLocation> { Game1.getFarm(), Game1.currentLocation };
			int grownCount = 0;
			foreach (var loc in locs)
			{
				if (loc?.terrainFeatures != null)
				{
					foreach (var pair in loc.terrainFeatures.Pairs)
					{
						if (pair.Value is StardewValley.TerrainFeatures.HoeDirt dirt && dirt.crop != null)
						{
							dirt.crop.currentPhase.Value = dirt.crop.phaseDays.Count - 1;
							dirt.crop.fullyGrown.Value = true;
							grownCount++;
						}
					}
				}
			}
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage($"4. {grownCount} plantações amadureceram instantaneamente!", 2));
		}

		// 5. Z-Moves e Ultra Z-Moves
		else if (action == "mech_give_signature_z")
		{
			GiveEquipRing("Poke_ZPowerRing");
			GiveEquipRing("Poke_ZRing");
			foreach (var sigZ in PokemonItemManager.SignatureZCrystalDefinitions.Keys)
			{
				Game1.player.addItemToInventory(new StardewValley.Object("(O)" + sigZ, 1));
			}
			Game1.playSound("coin");
			Game1.addHUDMessage(new HUDMessage("5. Z-Power Ring + 17 Cristais Z de Assinatura adicionados!", 2));
		}
		else if (action == "mech_test_ultra_necrozma")
		{
			GiveEquipRing("Poke_ZPowerRing");
			Game1.player.addItemToInventory(new StardewValley.Object("(O)Poke_UltranecroziumZ", 1));
			var necro = new PokemonInstance(PokemonSpecies.Get("Necrozma") ?? PokemonSpecies.Get("Pikachu"), 75);
			necro.Nickname = "Ultra Necrozma";
			necro.HeldItem = "Poke_UltranecroziumZ";
			var photon = PokemonMove.Get("Photon Geyser") ?? PokemonMove.Get("Thunderbolt");
			if (photon != null) necro.Moves.Add(photon);
			Party.AddPokemon(necro);
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage("5. Necrozma Lv. 75 segurando Ultranecrozium Z adicionado à equipe!", 2));
		}

		// 6. Geodos e Sistema de Pity
		else if (action == "mech_give_geodes")
		{
			Game1.player.addItemToInventory(new StardewValley.Object("535", 10));
			Game1.player.addItemToInventory(new StardewValley.Object("536", 10));
			Game1.player.addItemToInventory(new StardewValley.Object("537", 10));
			Game1.player.addItemToInventory(new StardewValley.Object("749", 10));
			Game1.playSound("coin");
			Game1.addHUDMessage(new HUDMessage("6. +10 Geodos de cada tipo (Normal, Gelo, Magma, Omni) adicionados!", 2));
		}
		else if (action == "mech_test_geode_pity")
		{
			if (Game1.player.modData != null)
			{
				Game1.player.modData["poke_relicPity"] = "39";
			}
			Item testRelic = PokemonRelics.TryGetGeodeRelic(new StardewValley.Object("537", 1), new Random());
			string relicName = testRelic?.DisplayName ?? "Mega Stone / Z-Crystal";
			Game1.playSound("axe");
			Game1.addHUDMessage(new HUDMessage($"6. Pity ajustado para 39/40! Simulação de abertura garantiu: {relicName}!", 2));
		}

		// 7. Amizade e Sorte dos Espíritos
		else if (action == "mech_give_friend_candy")
		{
			Game1.player.addItemToInventory(new StardewValley.Object("(O)PokemonFriendshipCandy", 10));
			Game1.playSound("coin");
			Game1.addHUDMessage(new HUDMessage("7. +10 Friendship Candies (+255 Amizade instantânea) adicionados!", 2));
		}
		else if (action == "mech_trigger_friend_evo")
		{
			var party = Party.GetParty();
			var lead = party.Count > 0 ? party[0] : null;
			if (lead != null)
			{
				lead.Friendship = 255;
				var targetData = PokemonEvolution.CheckEvolution(lead, EvolutionTrigger.LevelUp);
				if (targetData != null)
				{
					var targetSpec = PokemonSpecies.Get(targetData.TargetSpecies);
					if (targetSpec != null)
					{
						Game1.playSound("reward");
						Game1.activeClickableMenu = new PokemonEvolutionMenu(lead, targetSpec);
						return;
					}
				}
			}
			var riolu = new PokemonInstance(PokemonSpecies.Get("Riolu"), 29);
			riolu.Friendship = 255;
			riolu.Nickname = "Riolu (Amizade 255)";
			Party.AddPokemon(riolu);
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage("7. Riolu com 255 de Amizade adicionado! Eleve 1 nível durante o dia para evoluir a Lucario.", 2));
		}
		else if (action == "mech_toggle_spirits_luck")
		{
			if (Game1.player?.team?.sharedDailyLuck != null)
			{
				if (Game1.player.DailyLuck > 0)
				{
					Game1.player.team.sharedDailyLuck.Value = -0.10;
					Game1.playSound("ghost");
					Game1.addHUDMessage(new HUDMessage("7. Espíritos: Muito Zangados (-10% Sorte) -> Menor chance de spawns raros", 2));
				}
				else
				{
					Game1.player.team.sharedDailyLuck.Value = 0.12;
					Game1.playSound("reward");
					Game1.addHUDMessage(new HUDMessage("7. Espíritos: Muito Felizes (+12% Sorte) -> Chance máxima de spawns raros e lendários!", 2));
				}
			}
		}

		// 8. Terastallization
		else if (action == "mech_give_tera_pack")
		{
			GiveEquipRing("Poke_TeraOrb");
			foreach (PokemonType t in (PokemonType[])Enum.GetValues(typeof(PokemonType)))
			{
				Game1.player.addItemToInventory(new StardewValley.Object($"(O)Poke_TeraShard_{t}", 50));
			}
			Game1.player.addItemToInventory(new StardewValley.Object("(O)Poke_TeraShard_Stellar", 50));
			Game1.playSound("coin");
			Game1.addHUDMessage(new HUDMessage("8. Orbe Tera equipado + 50 de todos os 19 Tera Shards adicionados!", 2));
		}
		else if (action == "mech_cycle_tera_type")
		{
			var party = Party.GetParty();
			var lead = party.Count > 0 ? party[0] : null;
			if (lead == null)
			{
				Game1.showRedMessage("Adicione um Pokémon líder à equipe primeiro!");
				return;
			}
			int nextType = ((int)lead.TeraType + 1) % 18;
			lead.TeraType = (PokemonType)nextType;
			lead.IsTerastallized = false;
			Game1.playSound("crystal");
			Game1.addHUDMessage(new HUDMessage($"8. Tera Type de {lead.Nickname ?? lead.Species.Name} alterado para: {lead.TeraType}!", 2));
		}
	}

	private void HandleUpdateV2Action(string action)
	{
		Game1.playSound("coin");

		// 1. Batalhas do Cume
		if (action == "v2_battle_red")
		{
			var team = NPCTeams.GetTeam("Red");
			if (team != null && team.Count > 0)
			{
				Game1.activeClickableMenu = new PokemonBattleMenu("Red", team);
			}
			else
			{
				Game1.showRedMessage("Time do Campeão Red não encontrado!");
			}
			return;
		}

		if (action == "v2_battle_steven")
		{
			var team = NPCTeams.GetTeam("Steven");
			if (team != null && team.Count > 0)
			{
				Game1.activeClickableMenu = new PokemonBattleMenu("Steven", team);
			}
			else
			{
				Game1.showRedMessage("Time do Mestre Steven Stone não encontrado!");
			}
			return;
		}

		// 2. Teleporte ao Cume
		if (action == "v2_warp_summit")
		{
			Game1.playSound("wand");
			Game1.warpFarmer("Summit", 21, 15, 2);
			Game1.exitActiveMenu();
			Game1.addHUDMessage(new HUDMessage("Teleportado para o Cume da Montanha!", 2));
			return;
		}

		// 3. Testar Reatividade dos Aldeões
		if (action == "v2_test_npc_reactions")
		{
			var lead = Party.ActivePokemon ?? (Party.Team.Count > 0 ? Party.Team[0] : null);
			if (lead == null)
			{
				Game1.showRedMessage("Adicione um Pokémon à sua equipe primeiro!");
				return;
			}

			string[] testNpcs = new[] { "Clint", "Abigail", "Willy", "Maru", "Linus", "Wizard", "Krobus", "Haley" };
			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"[Reações a {lead.Nickname} ({lead.Species.PrimaryType})]:");
			int count = 0;
			foreach (var npc in testNpcs)
			{
				string r = StardewValley.NPC.GetPokemonReactionDialogue(npc, lead);
				if (!string.IsNullOrEmpty(r))
				{
					sb.AppendLine($"• {npc}: \"{r}\"");
					count++;
					if (count >= 3) break;
				}
			}
			if (count == 0)
			{
				sb.AppendLine($"• Os aldeões observam {lead.Nickname} com curiosidade amigável!");
			}
			Game1.drawObjectDialogue(sb.ToString().TrimEnd());
			return;
		}

		// 4. Climas em Batalha
		if (action == "v2_battle_rain")
		{
			var wildMon = new PokemonInstance(PokemonSpecies.Get("Pikachu"), 25);
			var battle = new PokemonBattleMenu(wildMon, isWild: true);
			battle.CurrentWeather = PokemonBattleMenu.BattleWeather.Rain;
			battle.WeatherTurns = 5;
			Game1.activeClickableMenu = battle;
			return;
		}

		if (action == "v2_battle_sun")
		{
			var wildMon = new PokemonInstance(PokemonSpecies.Get("Charizard"), 36);
			var battle = new PokemonBattleMenu(wildMon, isWild: true);
			battle.CurrentWeather = PokemonBattleMenu.BattleWeather.HarshSun;
			battle.WeatherTurns = 5;
			Game1.activeClickableMenu = battle;
			return;
		}

		if (action == "v2_battle_sandstorm")
		{
			var wildMon = new PokemonInstance(PokemonSpecies.Get("Tyranitar"), 55);
			var battle = new PokemonBattleMenu(wildMon, isWild: true);
			battle.CurrentWeather = PokemonBattleMenu.BattleWeather.Sandstorm;
			battle.WeatherTurns = 5;
			Game1.activeClickableMenu = battle;
			return;
		}

		if (action == "v2_battle_snow")
		{
			var wildMon = new PokemonInstance(PokemonSpecies.Get("Abomasnow"), 40);
			var battle = new PokemonBattleMenu(wildMon, isWild: true);
			battle.CurrentWeather = PokemonBattleMenu.BattleWeather.Snow;
			battle.WeatherTurns = 5;
			Game1.activeClickableMenu = battle;
			return;
		}

		// 5. Itens de Troca e Linking Cord
		if (action == "v2_give_linking_cord")
		{
			string[] items = new[]
			{
				"PokemonLinkingCord", "PokemonMetalCoat", "PokemonProtector",
				"PokemonElectirizer", "PokemonMagmarizer", "PokemonUpGrade",
				"PokemonDubiousDisc", "PokemonDragonScale", "PokemonKingsRock", "PokemonReaperCloth"
			};
			foreach (var it in items)
			{
				Item obj = ItemRegistry.Create("(O)" + it, 5, 0, allowNull: true) ?? new StardewValley.Object(it, 5);
				if (obj != null) Game1.player.addItemToInventory(obj);
			}
			Game1.playSound("coin");
			Game1.addHUDMessage(new HUDMessage("+5 Cabos de Ligação & Itens de Troca adicionados!", 1));
			return;
		}

		if (action == "v2_test_solo_evo")
		{
			var lead = Party.ActivePokemon ?? (Party.Team.Count > 0 ? Party.Team[0] : null);
			if (lead == null)
			{
				lead = new PokemonInstance(PokemonSpecies.Get("Kadabra"), 30);
				Party.AddPokemon(lead);
				Game1.addHUDMessage(new HUDMessage("Kadabra Nv. 30 adicionado para teste!", 2));
			}

			var evo = PokemonEvolution.CheckEvolution(lead, EvolutionTrigger.UseItem, "LinkingCord");
			if (evo != null)
			{
				var targetSpec = PokemonSpecies.Get(evo.TargetSpecies);
				if (targetSpec != null)
				{
					Game1.activeClickableMenu = new PokemonEvolutionMenu(lead, targetSpec, () =>
					{
						Party.SaveParty();
					});
					return;
				}
			}

			Game1.showRedMessage($"{lead.Nickname} não possui evolução por Cabo de Ligação. (Dica: Use Kadabra, Haunter, Machoke, Graveler, etc.)");
			return;
		}

		// 6. Acelerar Ovos da Equipe
		if (action == "v2_hatch_1_step")
		{
			var party = Party.GetParty();
			int eggsAdjusted = 0;
			foreach (var mon in party)
			{
				if (mon.IsEgg)
				{
					mon.EggSteps = 1;
					eggsAdjusted++;
				}
			}
			if (eggsAdjusted > 0)
			{
				Game1.playSound("reward");
				Game1.addHUDMessage(new HUDMessage($"{eggsAdjusted} ovo(s) ajustado(s) para chocar no próximo passo!", 1));
			}
			else
			{
				var egg = new PokemonInstance(PokemonSpecies.Get("Eevee"), 1)
				{
					IsEgg = true,
					EggSteps = 1,
					Nickname = "Ovo Pokémon"
				};
				Party.AddPokemon(egg);
				Game1.playSound("reward");
				Game1.addHUDMessage(new HUDMessage("Ovo de Eevee gerado com 1 passo restante para chocar!", 1));
			}
			return;
		}

		// 7. Cura Total do PC Box
		if (action == "v2_pc_full_restore")
		{
			var party = Party.GetParty();
			foreach (var mon in party)
			{
				mon.FullRestore();
				mon.EndBattleGimmicks(revertMega: true);
			}
			int pcHealed = 0;
			for (int b = 0; b < PokemonStorage.TotalBoxes; b++)
			{
				var box = PokemonStorage.Instance.Boxes[b];
				if (box != null)
				{
					for (int s = 0; s < PokemonStorage.SlotsPerBox; s++)
					{
						var mon = box.Slots[s];
						if (mon != null)
						{
							mon.FullRestore();
							mon.EndBattleGimmicks(revertMega: true);
							pcHealed++;
						}
					}
				}
			}
			Party.SaveParty();
			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage($"Cura Total: {party.Count} na Equipe + {pcHealed} no PC restaurados!", 1));
			return;
		}

		// 8. Inspecionar Catch Rate & EXP Canônico
		if (action == "v2_inspect_rates")
		{
			var lead = Party.ActivePokemon ?? (Party.Team.Count > 0 ? Party.Team[0] : null);
			if (lead == null)
			{
				Game1.showRedMessage("Adicione um Pokémon à sua equipe primeiro!");
				return;
			}
			int catchRate = lead.Species.CatchRate;
			int baseExp = lead.Species.BaseExpYield;
			int nextExp = lead.ExpToNextLevel;
			string msg = $"[DADOS CANÔNICOS: {lead.Nickname} (#{lead.Species.DexNumber})]\n" +
						 $"• Tipagem: {lead.Species.PrimaryType}{(lead.Species.SecondaryType.HasValue ? "/" + lead.Species.SecondaryType.Value : "")}\n" +
						 $"• Catch Rate Canônico: {catchRate}/255\n" +
						 $"• Base EXP Yield: {baseExp}\n" +
						 $"• Nível: {lead.Level} | Delta XP p/ Próx Nv: {nextExp} XP\n" +
						 $"• Natureza: {lead.Nature} | Tera Type: {lead.TeraType}\n" +
						 $"• Habilidade: {lead.Ability ?? "Nenhuma"}";
			Game1.drawObjectDialogue(msg);
			return;
		}

		// 9. Simular Equação Canônica de Captura
		if (action == "v2_test_catch_formula")
		{
			var lead = Party.ActivePokemon ?? (Party.Team.Count > 0 ? Party.Team[0] : null);
			int rate = lead?.Species.CatchRate ?? 45;
			string spName = lead?.Nickname ?? "Mewtwo";
			int maxHp = 100;

			double a1 = ((3.0 * maxHp - 2.0 * maxHp) * rate * 1.0 * 1.0) / (3.0 * maxHp);
			double p1 = Math.Min(100.0, (a1 / 255.0) * 100.0);

			double curHpLow = 15;
			double a2 = ((3.0 * maxHp - 2.0 * curHpLow) * rate * 2.0 * 2.5) / (3.0 * maxHp);
			double p2 = Math.Min(100.0, (a2 / 255.0) * 100.0);

			string report = $"[FÓRMULA CANÔNICA GENS 3-8: {spName}]\n" +
							$"Taxa Base (Catch Rate): {rate}/255\n" +
							$"1. Pokébola Comum @ 100% HP (Sem Status):\n" +
							$"   Chance Calculada: {p1:F1}%\n" +
							$"2. Ultra Ball @ 15% HP + Sono (2.5x):\n" +
							$"   Chance Calculada: {p2:F1}%";
			Game1.drawObjectDialogue(report);
			return;
		}

		// 10. Reverter Gimmicks
		if (action == "v2_test_all_gimmick_revert")
		{
			var party = Party.GetParty();
			foreach (var mon in party)
			{
				mon.EndBattleGimmicks(revertMega: true);
			}
			Party.SaveParty();
			Game1.playSound("wand");
			Game1.addHUDMessage(new HUDMessage("Todas as transformações (Mega, Tera, Dynamax) foram revertidas!", 1));
			return;
		}

		// 11. Chocadeira & Ovos
		if (action == "v2_give_incubator_egg")
		{
			Item incubator = ItemRegistry.Create("(BC)101", 1, 0, allowNull: true);
			if (incubator == null) incubator = new StardewValley.Object(Vector2.Zero, "101");
			Game1.player.addItemToInventory(incubator);

			Item eggItem = ItemRegistry.Create("(O)PokemonEgg", 1, 0, allowNull: true);
			if (eggItem == null) eggItem = new StardewValley.Object("PokemonEgg", 1);
			Game1.player.addItemToInventory(eggItem);

			var partyEgg = new PokemonInstance(PokemonSpecies.Get("Eevee"), 1)
			{
				IsEgg = true,
				EggSteps = 100,
				Nickname = "Ovo Pokémon"
			};
			Party.AddPokemon(partyEgg);

			Game1.playSound("coin");
			Game1.addHUDMessage(new HUDMessage("Chocadeira + Ovo (Item) + Ovo (Equipe) adicionados!", 1));
			return;
		}

		if (action == "v2_warp_coop_incubator")
		{
			GameLocation target = null;
			foreach (var loc in Game1.locations)
			{
				if (loc is AnimalHouse ah && (ah.Name.Contains("Coop") || ah.NameOrUniqueName.Contains("Coop")))
				{
					target = loc;
					break;
				}
			}
			if (target != null)
			{
				Game1.playSound("wand");
				Game1.warpFarmer(target.NameOrUniqueName ?? target.Name, 2, 3, 2);
				Game1.exitActiveMenu();
				Game1.addHUDMessage(new HUDMessage("Teleportado para a Chocadeira no Galinheiro (Coop)!", 2));
			}
			else
			{
				Game1.playSound("wand");
				Game1.warpFarmer("Farm", 64, 15, 2);
				Game1.exitActiveMenu();
				Game1.addHUDMessage(new HUDMessage("Teleportado para a Fazenda (Local da Chocadeira)!", 2));
			}
			return;
		}

		// 12. Daycare & Breeding
		if (action == "v2_daycare_setup")
		{
			var spec = PokemonSpecies.Get("Pikachu");
			var p1 = new PokemonInstance(spec, 30);
			p1.Gender = "Female";
			p1.Nickname = "Pikachu Mãe";
			var p2 = new PokemonInstance(spec, 30);
			p2.Gender = "Male";
			p2.Nickname = "Pikachu Pai";

			PokemonBreeding.DaycareParent1 = p1;
			PokemonBreeding.DaycareParent2 = p2;
			PokemonBreeding.ReadyEgg = PokemonBreeding.ProduceEgg(p1, p2);

			Game1.playSound("reward");
			Game1.addHUDMessage(new HUDMessage("Pais (Pikachu F + M) configurados no Daycare e Ovo gerado!", 2));
			return;
		}

		if (action == "v2_daycare_collect")
		{
			if (PokemonBreeding.ReadyEgg == null && PokemonBreeding.DaycareParent1 != null && PokemonBreeding.DaycareParent2 != null)
			{
				PokemonBreeding.ReadyEgg = PokemonBreeding.ProduceEgg(PokemonBreeding.DaycareParent1, PokemonBreeding.DaycareParent2);
			}
			if (PokemonBreeding.ReadyEgg != null)
			{
				var egg = PokemonBreeding.ReadyEgg;
				PokemonBreeding.ReadyEgg = null;
				egg.EggSteps = 5;
				Party.AddPokemon(egg);
				Game1.playSound("reward");
				Game1.addHUDMessage(new HUDMessage($"Ovo de {egg.Species?.Name ?? "Bebê"} coletado do Daycare (5 passos p/ chocar)!", 1));
			}
			else
			{
				var pichuEgg = new PokemonInstance(PokemonSpecies.Get("Pichu"), 1)
				{
					IsEgg = true,
					EggSteps = 5,
					Nickname = "Ovo Pokémon"
				};
				Party.AddPokemon(pichuEgg);
				Game1.playSound("reward");
				Game1.addHUDMessage(new HUDMessage("Ovo de Pichu gerado e coletado (5 passos para chocar)!", 1));
			}
			return;
		}

		if (action == "v2_warp_daycare")
		{
			Game1.playSound("wand");
			Game1.warpFarmer("AnimalShop", 12, 14, 0);
			Game1.exitActiveMenu();
			Game1.addHUDMessage(new HUDMessage("Teleportado para o Rancho da Marnie (Daycare)!", 2));
			return;
		}
	}

	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Escape || key == Keys.F8 || key == Keys.F10)
		{
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
			return;
		}
		base.receiveKeyPress(key);
	}

	public override bool areGamePadControlsImplemented() => true;

	public override void receiveGamePadButton(Buttons button)
	{
		if (button == Buttons.B || button == Buttons.Back)
		{
			Game1.playSound("bigDeSelect");
			Game1.exitActiveMenu();
			return;
		}

		if (button == Buttons.LeftShoulder || button == Buttons.LeftTrigger)
		{
			_currentTab = (Tab)(((int)_currentTab + 6) % 7);
			SetupButtons();
			Game1.playSound("shwip");
			return;
		}
		if (button == Buttons.RightShoulder || button == Buttons.RightTrigger)
		{
			_currentTab = (Tab)(((int)_currentTab + 1) % 7);
			SetupButtons();
			Game1.playSound("shwip");
			return;
		}
	}

	public override void update(GameTime time)
	{
		base.update(time);
	}

	private static void DrawFilterPill(SpriteBatch b, Rectangle bounds, string label, bool isActive, bool isHovered)
	{
		Color bg = isActive ? new Color(255, 230, 140) : (isHovered ? new Color(245, 240, 215) : Color.White);
		IClickableMenu.drawTextureBox(
			b,
			Game1.mouseCursors,
			new Rectangle(432, 439, 9, 9),
			bounds.X,
			bounds.Y,
			bounds.Width,
			bounds.Height,
			bg,
			4f,
			drawShadow: false
		);
		PokemonUI.Text(b, Game1.smallFont, label, new Rectangle(bounds.X + 4, bounds.Y + 2, bounds.Width - 8, bounds.Height - 4), isActive ? Color.DarkRed : new Color(45, 45, 55), center: true, maxScale: 0.60f, shadow: false);
	}

	public override void draw(SpriteBatch b)
	{
		// Scrim
		b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.65f);

		// Frame
		IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

		// Header Title
		string headerTitle = _selectingWildBattleTarget
			? "⚔️ SELECIONE O POKÉMON SELVAGEM PARA BATALHAR"
			: "POKÉMON DEBUG MENU (GEN 1 - GEN 9 + MEGA & SHINY)";
		PokemonUI.Text(b, Game1.dialogueFont, headerTitle, new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + 12, 780, 30), _selectingWildBattleTarget ? Color.Crimson : Color.DarkGoldenrod, center: false, maxScale: 0.78f);

		int mouseX = Game1.getMouseX();
		int mouseY = Game1.getMouseY();

		// Draw Tab Buttons
		foreach (var tab in _tabButtons)
		{
			bool isCurrent = tab.name == _currentTab.ToString();
			bool hovered = tab.containsPoint(mouseX, mouseY);
			Color col = isCurrent ? Color.LightGreen : (hovered ? Color.Wheat : Color.White);
			IClickableMenu.drawTextureBox(b, tab.bounds.X, tab.bounds.Y, tab.bounds.Width, tab.bounds.Height, col);
			PokemonUI.Text(b, Game1.smallFont, tab.label, tab.bounds, isCurrent ? Color.DarkGreen : Game1.textColor, center: true, maxScale: 0.78f, shadow: false);
		}

		int startX = xPositionOnScreen + 30;
		int contentY = yPositionOnScreen + 94;

		// Tab 1: Pokemon
		if (_currentTab == Tab.Pokemon)
		{
			PokemonUI.Text(b, Game1.smallFont, "Nv:", new Rectangle(startX, contentY + 3, 28, 22), Color.DarkSlateGray, center: false, maxScale: 0.64f, shadow: false);
			foreach (var lvl in _levelButtons)
			{
				bool isSelected = int.Parse(lvl.name) == _selectedLevel;
				bool hov = lvl.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, lvl.bounds, lvl.label, isSelected, hov);
			}

			foreach (var gb in _genFilterButtons)
			{
				bool hov = gb.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, gb.bounds, gb.label, _filterGen > 0, hov);
			}

			foreach (var rb in _regionFilterButtons)
			{
				bool hov = rb.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, rb.bounds, rb.label, _filterRegion != "Todas", hov);
			}

			foreach (var tb in _typeFilterButtons)
			{
				bool hov = tb.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, tb.bounds, tb.label, _filterType != "Todos", hov);
			}

			foreach (var sb in _styleFilterButtons)
			{
				bool hov = sb.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, sb.bounds, sb.label, _filterStyle != StyleFilter.Normal, hov);
			}

			if (_eggToggleButton != null)
			{
				bool hov = _eggToggleButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _eggToggleButton.bounds, _eggToggleButton.label, _spawnAsEgg, hov);
			}

			var allSpecies = GetFilteredSpecies();
			int maxPage = Math.Max(0, (allSpecies.Count - 1) / 18);

			if (_prevPageButton != null)
			{
				bool hov = _prevPageButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _prevPageButton.bounds, _prevPageButton.label, false, hov);
			}

			string pageStr = $"{_speciesPage + 1}/{maxPage + 1}";
			PokemonUI.Text(b, Game1.smallFont, pageStr, new Rectangle(startX + 680, contentY + 2, 70, 24), Color.DarkSlateGray, center: true, maxScale: 0.64f, shadow: false);

			if (_nextPageButton != null)
			{
				bool hov = _nextPageButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _nextPageButton.bounds, _nextPageButton.label, false, hov);
			}

			bool isShinyStyle = (_filterStyle == StyleFilter.Shiny || _filterStyle == StyleFilter.MegaShiny);

			foreach (var pBtn in _pokemonButtons)
			{
				bool hov = pBtn.containsPoint(mouseX, mouseY);
				Color cardTint = _selectingWildBattleTarget
					? (hov ? new Color(255, 225, 225) : new Color(255, 248, 248))
					: (hov ? Color.LightGoldenrodYellow : Color.White);
				IClickableMenu.drawTextureBox(b, pBtn.bounds.X, pBtn.bounds.Y, pBtn.bounds.Width, pBtn.bounds.Height, cardTint);

				var species = PokemonSpecies.Get(pBtn.name);
				var dummy = GetPreviewInstance(species, isShinyStyle);
				// For Shiny or Mega preview, prefer front sprite so the exact Shiny palette / Mega form is crystal clear!
				Texture2D previewTex = (isShinyStyle || species.IsMega)
					? (dummy.GetFrontSprite(Game1.graphics.GraphicsDevice) ?? dummy.GetIconSprite(Game1.graphics.GraphicsDevice))
					: (dummy.GetIconSprite(Game1.graphics.GraphicsDevice) ?? dummy.GetFrontSprite(Game1.graphics.GraphicsDevice));

				if (previewTex != null)
				{
					bool isFront = (previewTex == dummy.GetFrontSprite(Game1.graphics.GraphicsDevice));
					Rectangle srcRect = isFront ? dummy.GetFrontSourceRect() : dummy.GetIconSourceRect();
					float scale = isFront ? (40f / Math.Max(1, srcRect.Width)) : 1.0f;
					Vector2 drawPos = isFront ? new Vector2(pBtn.bounds.X + 8, pBtn.bounds.Y + 9) : new Vector2(pBtn.bounds.X + 8, pBtn.bounds.Y + 12);
					b.Draw(previewTex, drawPos, srcRect, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
				}

				string dispName = isShinyStyle ? $"★ {species.Name}" : species.Name;
				Color nameColor = isShinyStyle ? new Color(185, 125, 0) : (hov ? Color.DarkRed : Game1.textColor);
				PokemonUI.Text(b, Game1.smallFont, dispName, new Rectangle(pBtn.bounds.X + 52, pBtn.bounds.Y + 7, pBtn.bounds.Width - 58, 22), nameColor, center: false, maxScale: 0.74f, shadow: false);

				int p1W = DrawTypePill(b, species.PrimaryType, pBtn.bounds.X + 52, pBtn.bounds.Y + 32);
				if (species.SecondaryType.HasValue)
				{
					DrawTypePill(b, species.SecondaryType.Value, pBtn.bounds.X + 52 + p1W + 5, pBtn.bounds.Y + 32);
				}
			}
		}

		// Tab 2: Items
		else if (_currentTab == Tab.Items)
		{
			foreach (var ib in _itemButtons)
			{
				bool hov = ib.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, ib.bounds.X, ib.bounds.Y, ib.bounds.Width, ib.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
				PokemonUI.Text(b, Game1.smallFont, ib.label, new Rectangle(ib.bounds.X + 12, ib.bounds.Y + 6, ib.bounds.Width - 24, ib.bounds.Height - 12), hov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.76f, shadow: false);
			}
		}

		// Tab 3: TMs & HMs
		else if (_currentTab == Tab.TMs)
		{
			var allMoves = PokemonMove.AllMoveNames();
			if (_filterFieldMovesOnly) allMoves = allMoves.FindAll(m => PokemonMove.Get(m).GetFieldAbility() != FieldAbilityType.None);
			if (_filterType != "Todos") allMoves = allMoves.FindAll(m => string.Equals(PokemonMove.Get(m).Type.ToString(), _filterType, StringComparison.OrdinalIgnoreCase));
			int maxPage = Math.Max(0, (allMoves.Count - 1) / 18);

			if (_tmFilterButton != null)
			{
				bool hov = _tmFilterButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _tmFilterButton.bounds, _tmFilterButton.label, _filterFieldMovesOnly, hov);
			}

			if (_tmTypeFilterButton != null)
			{
				bool hov = _tmTypeFilterButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _tmTypeFilterButton.bounds, _tmTypeFilterButton.label, _filterType != "Todos", hov);
			}

			if (_giveFieldTmsButton != null)
			{
				bool hov = _giveFieldTmsButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _giveFieldTmsButton.bounds, _giveFieldTmsButton.label, false, hov);
			}

			if (_giveAllTmsButton != null)
			{
				bool hov = _giveAllTmsButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _giveAllTmsButton.bounds, _giveAllTmsButton.label, false, hov);
			}

			if (_tmPrevPageButton != null)
			{
				bool hov = _tmPrevPageButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _tmPrevPageButton.bounds, _tmPrevPageButton.label, false, hov);
			}

			if (_tmNextPageButton != null)
			{
				bool hov = _tmNextPageButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _tmNextPageButton.bounds, _tmNextPageButton.label, false, hov);
			}

			// TM Page indicator
			string tmPageStr = $"Pág {_tmPage + 1}/{maxPage + 1}";
			PokemonUI.Text(b, Game1.smallFont, tmPageStr, new Rectangle(startX + 575, contentY + 3, 140, 24), Color.DarkSlateGray, center: true, maxScale: 0.70f, shadow: false);

			// Render individual TM cards
			foreach (var tb in _tmButtons)
			{
				bool hov = tb.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, tb.bounds.X, tb.bounds.Y, tb.bounds.Width, tb.bounds.Height, hov ? Color.LightGoldenrodYellow : Color.White);

				string moveName = tb.name;
				var move = PokemonMove.Get(moveName);

				// Disc indicator circle
				int discX = tb.bounds.X + 8;
				int discY = tb.bounds.Y + 14;
				b.Draw(Game1.staminaRect, new Rectangle(discX, discY, 26, 26), Color.LightSteelBlue);
				b.Draw(Game1.staminaRect, new Rectangle(discX + 4, discY + 4, 18, 18), Color.DarkSlateBlue);
				b.Draw(Game1.staminaRect, new Rectangle(discX + 9, discY + 9, 8, 8), Color.Silver);

				// Move name
				PokemonUI.Text(b, Game1.smallFont, move.Name, new Rectangle(tb.bounds.X + 40, tb.bounds.Y + 6, tb.bounds.Width - 48, 22), hov ? Color.DarkRed : Game1.textColor, center: false, maxScale: 0.76f, shadow: false);

				// Type pill
				int pillW = DrawTypePill(b, move.Type, tb.bounds.X + 40, tb.bounds.Y + 32) + 6;

				// Field Ability Badge or Pwr/PP
				var fa = move.GetFieldAbility();
				if (fa != FieldAbilityType.None)
				{
					string faTag = fa switch
					{
						FieldAbilityType.Cut => "HM CORTAR",
						FieldAbilityType.Smash => "HM QUEBRAR",
						FieldAbilityType.Water => "REGAR",
						FieldAbilityType.Growth => "FERTILIZAR",
						FieldAbilityType.Rain => "CLIMA CHUVA",
						FieldAbilityType.Sunny => "CLIMA SOL",
						FieldAbilityType.Zap => "ILUMINAR",
						FieldAbilityType.Dash => "DASH",
						FieldAbilityType.Fly => "HM VOAR",
						FieldAbilityType.Teleport => "TELEPORTE",
						_ => "CAMPO"
					};
					int tagX = tb.bounds.X + 40 + pillW;
					int maxAvailW = Math.Max(40, tb.bounds.Right - 10 - tagX);
					int tagW = Math.Min(maxAvailW, (int)(Game1.smallFont.MeasureString(faTag).X * 0.48f) + 10);
					b.Draw(Game1.staminaRect, new Rectangle(tagX, tb.bounds.Y + 32, tagW, 16), Color.DarkGoldenrod);
					PokemonUI.Border(b, new Rectangle(tagX, tb.bounds.Y + 32, tagW, 16), 1, Color.White * 0.5f);
					PokemonUI.Text(b, Game1.smallFont, faTag, new Rectangle(tagX + 2, tb.bounds.Y + 32, tagW - 4, 16), Color.White, center: true, maxScale: 0.48f, shadow: false);
				}
				else
				{
					string pwrPp = $"Pwr:{move.Power} PP:{move.MaxPP}";
					PokemonUI.Text(b, Game1.smallFont, pwrPp, new Rectangle(tb.bounds.X + 40 + pillW, tb.bounds.Y + 31, tb.bounds.Width - 48 - pillW, 18), Color.DimGray, center: false, maxScale: 0.58f, shadow: false);
				}
			}
		}

		// Tab 4: Battle
		else if (_currentTab == Tab.Battle)
		{
			var allNpcs = NPCTeams.GetAllNPCNames();
			int maxBattlePages = Math.Max(0, (allNpcs.Count - 1) / 8);

			if (_battlePrevPageButton != null)
			{
				bool hov = _battlePrevPageButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _battlePrevPageButton.bounds, _battlePrevPageButton.label, false, hov);
			}

			string battlePageStr = $"Treinadores do Vale: Pág {_battlePage + 1}/{maxBattlePages + 1}";
			PokemonUI.Text(b, Game1.smallFont, battlePageStr, new Rectangle(xPositionOnScreen + 280, yPositionOnScreen + 204, 340, 24), Color.DarkGoldenrod, center: true, maxScale: 0.76f, shadow: false);

			if (_battleNextPageButton != null)
			{
				bool hov = _battleNextPageButton.containsPoint(mouseX, mouseY);
				DrawFilterPill(b, _battleNextPageButton.bounds, _battleNextPageButton.label, false, hov);
			}

			foreach (var bb in _battleButtons)
			{
				bool hov = bb.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, bb.bounds.X, bb.bounds.Y, bb.bounds.Width, bb.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
				PokemonUI.Text(b, Game1.smallFont, bb.label, new Rectangle(bb.bounds.X + 8, bb.bounds.Y + 4, bb.bounds.Width - 16, bb.bounds.Height - 8), hov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.78f, shadow: false);
			}
		}

		// Tab 5: Cheats
		else if (_currentTab == Tab.Cheats)
		{
			foreach (var cb in _cheatButtons)
			{
				bool hov = cb.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, cb.bounds.X, cb.bounds.Y, cb.bounds.Width, cb.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
				PokemonUI.Text(b, Game1.smallFont, cb.label, new Rectangle(cb.bounds.X + 8, cb.bounds.Y + 4, cb.bounds.Width - 16, cb.bounds.Height - 8), hov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.78f, shadow: false);
			}
		}

		// Tab 6: Mechanics (8 Funções)
		else if (_currentTab == Tab.Mechanics)
		{
			foreach (var mb in _mechanicButtons)
			{
				bool hov = mb.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, mb.bounds.X, mb.bounds.Y, mb.bounds.Width, mb.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
				PokemonUI.Text(b, Game1.smallFont, mb.label, new Rectangle(mb.bounds.X + 8, mb.bounds.Y + 4, mb.bounds.Width - 16, mb.bounds.Height - 8), hov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.78f, shadow: false);
			}
		}

		// Tab 7: UpdatesV2 (Novidades v2.0, v1.4, v1.3)
		else if (_currentTab == Tab.UpdatesV2)
		{
			foreach (var ub in _updateButtons)
			{
				bool hov = ub.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, ub.bounds.X, ub.bounds.Y, ub.bounds.Width, ub.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
				PokemonUI.Text(b, Game1.smallFont, ub.label, new Rectangle(ub.bounds.X + 8, ub.bounds.Y + 4, ub.bounds.Width - 16, ub.bounds.Height - 8), hov ? Color.DarkBlue : Game1.textColor, center: true, maxScale: 0.78f, shadow: false);
			}
		}

		// Tab 8: Legendaries
		else if (_currentTab == Tab.Legendaries)
		{
			int legCardsPerPage = 10;
			int totalLegs = AllLegendariesList.Length;
			int maxLegPages = Math.Max(0, (totalLegs - 1) / legCardsPerPage);
			int startLegIdx = _legendaryPage * legCardsPerPage;
			int endLegIdx = Math.Min(totalLegs, startLegIdx + legCardsPerPage);

			int lStartY = contentY + 2;
			int col2X = startX + 424;
			int cardH = 68;
			int cardW = 412;
			int rowGap = 6;

			for (int idx = startLegIdx; idx < endLegIdx; idx++)
			{
				int slot = idx - startLegIdx;
				int col = slot % 2;
				int row = slot / 2;
				int cardX = (col == 0) ? startX : col2X;
				int cardY = lStartY + row * (cardH + rowGap);

				string legName = AllLegendariesList[idx];
				var spec = PokemonSpecies.Get(legName);

				// Draw card background
				Rectangle cardBox = new Rectangle(cardX, cardY, cardW, cardH);
				IClickableMenu.drawTextureBox(b, cardBox.X, cardBox.Y, cardBox.Width, cardBox.Height, Color.White);

				if (spec != null)
				{
					// Draw icon
					var inst = GetPreviewInstance(spec, false);
					Texture2D icon = inst?.GetIconSprite(Game1.graphics.GraphicsDevice);
					if (icon != null)
					{
						Rectangle srcRect = inst.GetIconSourceRect();
						b.Draw(icon, new Vector2(cardX + 8, cardY + 12), srcRect, Color.White, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0.86f);
					}

					// Draw texts
					string typeStr = spec.SecondaryType.HasValue ? $"{spec.PrimaryType}/{spec.SecondaryType.Value}" : $"{spec.PrimaryType}";
					PokemonUI.Text(b, Game1.smallFont, $"#{spec.DexNumber} {spec.Name}", new Rectangle(cardX + 54, cardY + 12, 170, 22), Game1.textColor, center: false, maxScale: 0.80f, shadow: false);
					PokemonUI.Text(b, Game1.tinyFont, $"Tipo: {typeStr} | Nv. 70", new Rectangle(cardX + 54, cardY + 36, 170, 18), Color.DimGray, center: false, maxScale: 0.90f, shadow: false);
				}
			}

			// Draw action buttons
			foreach (var lb in _legendaryButtons)
			{
				bool hov = lb.containsPoint(mouseX, mouseY);
				Color btnColor = lb.name.StartsWith("leg_boss_")
					? (hov ? Color.OrangeRed : Color.MistyRose)
					: (hov ? Color.LightSkyBlue : Color.AliceBlue);
				IClickableMenu.drawTextureBox(b, lb.bounds.X, lb.bounds.Y, lb.bounds.Width, lb.bounds.Height, btnColor);
				PokemonUI.Text(b, Game1.smallFont, lb.label, new Rectangle(lb.bounds.X + 4, lb.bounds.Y + 4, lb.bounds.Width - 8, lb.bounds.Height - 8), Color.Black, center: true, maxScale: 0.78f, shadow: false);
			}

			// Draw pagination buttons and page number
			if (_legPrevPageButton != null)
			{
				bool hov = _legPrevPageButton.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, _legPrevPageButton.bounds.X, _legPrevPageButton.bounds.Y, _legPrevPageButton.bounds.Width, _legPrevPageButton.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
				PokemonUI.Text(b, Game1.smallFont, _legPrevPageButton.label, _legPrevPageButton.bounds, hov ? Color.DarkBlue : Game1.textColor, center: true, maxScale: 0.82f, shadow: false);
			}
			if (_legNextPageButton != null)
			{
				bool hov = _legNextPageButton.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, _legNextPageButton.bounds.X, _legNextPageButton.bounds.Y, _legNextPageButton.bounds.Width, _legNextPageButton.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
				PokemonUI.Text(b, Game1.smallFont, _legNextPageButton.label, _legNextPageButton.bounds, hov ? Color.DarkBlue : Game1.textColor, center: true, maxScale: 0.82f, shadow: false);
			}
			PokemonUI.Text(b, Game1.smallFont, $"Página {_legendaryPage + 1} de {maxLegPages + 1}", new Rectangle(startX + 330, lStartY + 5 * (cardH + rowGap) + 12, 180, 28), Game1.textColor, center: true, maxScale: 0.82f, shadow: false);
		}

		// Close Button
		_closeButton.draw(b);

		// Cursor
		drawMouse(b);
	}

	private List<PokemonSpecies> GetFilteredSpecies()
	{
		bool isMegaStyle = (_filterStyle == StyleFilter.Mega || _filterStyle == StyleFilter.MegaShiny);
		var allSpecies = isMegaStyle ? PokemonSpecies.AllMegaSpecies() : PokemonSpecies.AllSpecies();

		if (_filterGen > 0)
		{
			allSpecies = allSpecies.FindAll(s =>
			{
				int dex = s.DexNumber;
				return _filterGen switch
				{
					1 => dex >= 1 && dex <= 151,
					2 => dex >= 152 && dex <= 251,
					3 => dex >= 252 && dex <= 386,
					4 => dex >= 387 && dex <= 493,
					5 => dex >= 494 && dex <= 649,
					6 => dex >= 650 && dex <= 721,
					7 => dex >= 722 && dex <= 809,
					8 => dex >= 810 && dex <= 905,
					9 => dex >= 906 && dex <= 1025,
					_ => true
				};
			});
		}

		if (_filterRegion != "Todas")
		{
			allSpecies = allSpecies.FindAll(s =>
			{
				int dex = s.DexNumber;
				return _filterRegion switch
				{
					"Kanto" => dex >= 1 && dex <= 151,
					"Johto" => dex >= 152 && dex <= 251,
					"Hoenn" => dex >= 252 && dex <= 386,
					"Sinnoh" => dex >= 387 && dex <= 493,
					"Unova" => dex >= 494 && dex <= 649,
					"Kalos" => dex >= 650 && dex <= 721,
					"Alola" => dex >= 722 && dex <= 809,
					"Galar" => dex >= 810 && dex <= 905,
					"Paldea" => dex >= 906 && dex <= 1025,
					_ => true
				};
			});
		}

		if (_filterType != "Todos")
		{
			allSpecies = allSpecies.FindAll(s =>
				string.Equals(s.PrimaryType.ToString(), _filterType, StringComparison.OrdinalIgnoreCase) ||
				(s.SecondaryType.HasValue && string.Equals(s.SecondaryType.Value.ToString(), _filterType, StringComparison.OrdinalIgnoreCase)));
		}

		return allSpecies;
	}

	private static int DrawTypePill(SpriteBatch b, PokemonType type, int px, int py)
	{
		Color typeCol = PokemonTypeColors.Get(type);
		string name = type.ToString().ToUpperInvariant();
		Vector2 textSz = Game1.smallFont.MeasureString(name) * 0.48f;
		int pillW = Math.Max(42, (int)textSz.X + 10);
		int pillH = 16;
		b.Draw(Game1.staminaRect, new Rectangle(px, py, pillW, pillH), typeCol);
		PokemonUI.Border(b, new Rectangle(px, py, pillW, pillH), 1, Color.White * 0.4f);
		PokemonUI.Text(b, Game1.smallFont, name, new Rectangle(px, py, pillW, pillH), Color.White, center: true, maxScale: 0.48f, shadow: false);
		return pillW;
	}

	public static (string loc, int x, int y, string itemId, string itemName) GetLegendaryHabitat(string name)
	{
		return name switch
		{
			"Articuno" => ("UndergroundMine", 18, 13, "Poke_FreezingFeather", "Pena Glacial"),
			"Zapdos" => ("Mountain", 32, 44, "Poke_ElectricFeather", "Pena Elétrica"),
			"Moltres" => ("Caldera", 28, 28, "Poke_FlameFeather", "Pena de Chamas"),
			"Mewtwo" => ("SkullCave", 18, 18, "Poke_BerserkGene", "Gene Berserk"),
			"Mew" => ("IslandSouthEast", 22, 18, "Poke_OldSeaMap", "Mapa do Mar Antigo"),
			"Raikou" => ("Woods", 15, 20, "Poke_ClearBell", "Sino Claro"),
			"Entei" => ("Mountain", 25, 30, "Poke_ClearBell", "Sino Claro"),
			"Suicune" => ("Beach", 20, 15, "Poke_ClearBell", "Sino Claro"),
			"Lugia" => ("Submarine", 12, 12, "Poke_TidalBell", "Sino das Marés"),
			"Ho-Oh" => ("Summit", 22, 14, "Poke_RainbowWing", "Asa do Arco-Íris"),
			"Celebi" => ("Woods", 28, 15, "Poke_GSBall", "GS Ball"),
			"Regirock" => ("UndergroundMine", 15, 10, "Poke_RelicCrown", "Coroa Relíquia"),
			"Regice" => ("UndergroundMine", 15, 10, "Poke_RelicCrown", "Coroa Relíquia"),
			"Registeel" => ("UndergroundMine", 15, 10, "Poke_RelicCrown", "Coroa Relíquia"),
			"Latias" => ("IslandWest", 30, 40, "Poke_EonFlute", "Flauta Eon"),
			"Latios" => ("Beach", 40, 20, "Poke_EonFlute", "Flauta Eon"),
			"Kyogre" => ("Beach", 28, 35, "Poke_BlueOrb", "Orbe Azul"),
			"Groudon" => ("Caldera", 16, 22, "Poke_RedOrb", "Orbe Vermelho"),
			"Rayquaza" => ("Summit", 21, 10, "Poke_JadeOrb", "Orbe de Jade"),
			"Jirachi" => ("Mountain", 50, 10, "Poke_CometShard", "Fragmento de Cometa"),
			"Deoxys" => ("Railroad", 15, 52, "Poke_MeteoritePiece", "Pedaço de Meteorito"),
			"Dialga" => ("Summit", 21, 12, "Poke_AdamantOrb", "Orbe Diamante"),
			"Palkia" => ("Summit", 23, 12, "Poke_LustrousOrb", "Orbe Pérola"),
			"Giratina" => ("SkullCave", 15, 15, "Poke_GriseousOrb", "Orbe Platina"),
			"Darkrai" => ("Town", 55, 18, "Poke_DarkraiNightmare", "Pesadelo Obscuro"),
			"Shaymin" => ("Forest", 60, 40, "Poke_GracideaFlower", "Flor Gracídea"),
			"Arceus" => ("Summit", 22, 8, "Poke_AzureFlute", "Flauta Azure"),
			"Reshiram" => ("Caldera", 24, 24, "Poke_LightStone", "Pedra da Luz"),
			"Zekrom" => ("Desert", 18, 32, "Poke_DarkStone", "Pedra das Trevas"),
			"Kyurem" => ("UndergroundMine", 18, 13, "Poke_DNA_Splicers", "Fundidores de DNA"),
			"Xerneas" => ("Woods", 35, 18, "Poke_FairyFeather", "Pena Feérica"),
			"Yveltal" => ("SkullCave", 25, 25, "Poke_DarkFeather", "Pena Sombria"),
			"Zygarde" => ("UndergroundMine", 15, 15, "Poke_ZygardeCube", "Cubo Zygarde"),
			"Solgaleo" => ("Desert", 34, 42, "Poke_SunFlute", "Flauta do Sol"),
			"Lunala" => ("Beach", 18, 25, "Poke_MoonFlute", "Flauta da Lua"),
			"Necrozma" => ("SkullCave", 30, 30, "Poke_UltranecroziumZ", "Ultralnecrozium Z"),
			"Zacian" => ("Forest", 45, 50, "Poke_RustedSword", "Espada Enferrujada"),
			"Zamazenta" => ("Mountain", 35, 30, "Poke_RustedShield", "Escudo Enferrujado"),
			"Koraidon" => ("Caldera", 20, 20, "Poke_KoraidonFeather", "Asa Passada"),
			"Miraidon" => ("Railroad", 25, 45, "Poke_MiraidonCore", "Núcleo Futuro"),
			_ => ("Mountain", 48, 12, "Poke_RainbowWing", "Relíquia Lendária")
		};
	}

}
