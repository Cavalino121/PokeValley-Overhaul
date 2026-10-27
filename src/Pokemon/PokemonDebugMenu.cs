using System.Linq;
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
		Legendaries,
		FormsAndEvos,
		AudioAndVfx,
		Milestones
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
	private int _formsEvosPage = 0;
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
	private readonly List<ClickableComponent> _formsEvosButtons = new();
	private readonly List<ClickableComponent> _audioVfxButtons = new();
	private int _crySpeciesIndex = 24;
	private string _activeVfxAnim = null;
	private float _vfxAnimTimer = 0f;
	private float _vfxAnimDuration = 0.85f;
	private string _activeCutscene = null;
	private float _cutsceneTimer = 0f;

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
	private ClickableComponent _fePrevPageButton;
	private ClickableComponent _feNextPageButton;
	private ClickableComponent _clearSpawnsBottomButton;
	private ClickableComponent _clearInventoryBottomButton;
	private ClickableComponent _resetMilestonesBottomButton;
	private readonly List<ClickableComponent> _milestonesButtons = new();
	private ClickableTextureComponent _closeButton;

	private bool _itemCatalogMode = false;
	private int _itemCatalogPage = 0;
	private string _itemCatalogCategory = "Todos";
	private ClickableComponent _itemCatalogToggleBtn;
	private ClickableComponent _itemCatPrevBtn;
	private ClickableComponent _itemCatNextBtn;
	private readonly List<ClickableComponent> _itemCategoryFilterButtons = new();
	private readonly List<ClickableComponent> _itemCatalogCards = new();

	public record ModItemCatalogEntry(string ItemId, string FullId, string DisplayName, string Category);
	private static List<ModItemCatalogEntry> _allCatalogItems = null;

	private static int _cutsceneSpeciesIdx = 0;
	private static readonly string[] _cutsceneLegendaryList = new[]
	{
		"Rayquaza", "Kyogre", "Groudon", "Mewtwo", "Mew", "Lugia", "Ho-Oh",
		"Dialga", "Palkia", "Giratina", "Arceus", "Reshiram", "Zekrom", "Kyurem",
		"Xerneas", "Yveltal", "Zygarde", "Solgaleo", "Lunala", "Necrozma",
		"Zacian", "Zamazenta", "Eternatus", "Koraidon", "Miraidon", "Terapagos"
	};

	public record LegendaryFormEvoEntry(
		string BaseName,
		string PreviewKey,
		string Title,
		string Subtitle,
		string[] ExtraItems,
		int RareCandyCount = 0,
		string PartnerSpecies = null
	);

	private static readonly LegendaryFormEvoEntry[] AllLegendaryFormEvoEntries = new[]
	{
		new LegendaryFormEvoEntry("Kyogre", "kyogre/primal", "Kyogre ➔ Primal Kyogre", "Item: Blue Orb (Reversão Primordial)", new[] { "Poke_BlueOrb", "Poke_MegaBracelet" }),
		new LegendaryFormEvoEntry("Groudon", "groudon/primal", "Groudon ➔ Primal Groudon", "Item: Red Orb (Reversão Primordial)", new[] { "Poke_RedOrb", "Poke_MegaBracelet" }),
		new LegendaryFormEvoEntry("Rayquaza", "rayquaza/mega", "Rayquaza ➔ Mega Rayquaza", "Golpe Dragon Ascent + Rayquazite", new[] { "Poke_Rayquazite", "Poke_MegaBracelet" }),
		new LegendaryFormEvoEntry("Mewtwo", "mewtwo/mega_y", "Mewtwo ➔ Mega Mewtwo X & Y", "Itens: Mewtwonite X & Mewtwonite Y", new[] { "Poke_MewtwoniteX", "Poke_MewtwoniteY", "Poke_MegaBracelet" }),
		new LegendaryFormEvoEntry("Latias", "latias/mega", "Latias & Latios ➔ Mega Eon Duo", "Itens: Latiasite & Latiosite", new[] { "Poke_Latiasite", "Poke_Latiosite", "Poke_MegaBracelet" }, 0, "Latios"),
		new LegendaryFormEvoEntry("Deoxys", "deoxys/attack", "Deoxys (Normal / Atk / Def / Spe)", "Item: Pedaço de Meteorito (4 Formas)", new[] { "Poke_MeteoritePiece" }),
		new LegendaryFormEvoEntry("Dialga", "dialga/origin", "Dialga ➔ Dialga Origin Forme", "Item: Adamant Crystal / Adamant Orb", new[] { "Poke_AdamantCrystal", "Poke_AdamantOrb" }),
		new LegendaryFormEvoEntry("Palkia", "palkia/origin", "Palkia ➔ Palkia Origin Forme", "Item: Lustrous Globe / Lustrous Orb", new[] { "Poke_LustrousGlobe", "Poke_LustrousOrb" }),
		new LegendaryFormEvoEntry("Giratina", "giratina/origin", "Giratina ➔ Giratina Origin Forme", "Item: Griseous Core & Griseous Orb", new[] { "Poke_GriseousCore", "Poke_GriseousOrb" }),
		new LegendaryFormEvoEntry("Shaymin", "shaymin/sky", "Shaymin ➔ Shaymin Sky Forme", "Item: Flor Gracídea (Gracidea)", new[] { "Poke_Gracidea", "Poke_GracideaFlower" }),
		new LegendaryFormEvoEntry("Tornadus", "tornadus/therian", "Tornadus ➔ Tornadus Therian", "Item: Espelho Revelador (Reveal Glass)", new[] { "Poke_RevealGlass", "Poke_CycloneMirror" }),
		new LegendaryFormEvoEntry("Thundurus", "thundurus/therian", "Thundurus ➔ Thundurus Therian", "Item: Espelho Revelador (Reveal Glass)", new[] { "Poke_RevealGlass", "Poke_BoltMirror" }),
		new LegendaryFormEvoEntry("Landorus", "landorus/therian", "Landorus ➔ Landorus Therian", "Item: Espelho Revelador (Reveal Glass)", new[] { "Poke_RevealGlass", "Poke_FertilityMirror" }),
		new LegendaryFormEvoEntry("Enamorus", "enamorus/therian", "Enamorus ➔ Enamorus Therian", "Item: Espelho Revelador (Reveal Glass)", new[] { "Poke_RevealGlass", "Poke_RevealGlass_Love" }),
		new LegendaryFormEvoEntry("Kyurem", "kyurem/white", "Kyurem + Reshiram/Zekrom (White/Black)", "Item: DNA Splicers + Reshiram/Zekrom", new[] { "Poke_DNASplicers", "Poke_DNA_Splicers", "Poke_LightStone", "Poke_DarkStone" }, 0, "Reshiram"),
		new LegendaryFormEvoEntry("Zygarde", "zygarde/complete", "Zygarde (10% / 50% / 100% Complete)", "Item: Cubo de Zygarde (Zygarde Cube)", new[] { "Poke_ZygardeCube" }),
		new LegendaryFormEvoEntry("Diancie", "diancie/mega", "Diancie ➔ Mega Diancie", "Item: Diancite + Mega Bracelete", new[] { "Poke_Diancite", "Poke_MegaBracelet" }),
		new LegendaryFormEvoEntry("Hoopa", "hoopa/unbound", "Hoopa Confined ➔ Hoopa Unbound", "Item: Garrafa da Prisão (Prison Bottle)", new[] { "Poke_PrisonBottle" }),
		new LegendaryFormEvoEntry("Type: Null", "Silvally", "Type: Null ➔ Silvally", "Evolução: Amizade / Rare Candy / RKS", new[] { "PokemonFriendshipCandy", "Poke_MemoryDrive_Normal" }, 30),
		new LegendaryFormEvoEntry("Cosmog", "Solgaleo", "Cosmog ➔ Cosmoem ➔ Solgaleo / Lunala", "Evo Nv.43/53: +50 Rare Candy + Flautas", new[] { "Poke_SunFlute", "Poke_MoonFlute", "PokemonSunStone", "PokemonMoonStone" }, 50),
		new LegendaryFormEvoEntry("Necrozma", "necrozma/ultra", "Necrozma ➔ Dusk Mane / Dawn Wings / Ultra", "N-Solarizer, N-Lunarizer & Ultranecrozium Z", new[] { "Poke_NSolarizer", "Poke_NLunarizer", "Poke_UltranecroziumZ" }, 0, "Solgaleo"),
		new LegendaryFormEvoEntry("Poipole", "Naganadel", "Poipole ➔ Naganadel", "Evo Dragon Pulse: +30 Rare Candy + Ferrão", new[] { "Poke_DragonPoisonBarb" }, 30),
		new LegendaryFormEvoEntry("Meltan", "Melmetal", "Meltan ➔ Melmetal", "Evo: Metal Coat / Gigantamax Nut + Candy", new[] { "PokemonMetalCoat", "Poke_GigantamaxNut" }, 40),
		new LegendaryFormEvoEntry("Zacian", "zacian/crowned_sword", "Zacian & Zamazenta ➔ Crowned Forms", "Itens: Rusted Sword & Rusted Shield", new[] { "Poke_RustedSword", "Poke_RustedShield" }, 0, "Zamazenta"),
		new LegendaryFormEvoEntry("Kubfu", "urshifu/rapid_strike", "Kubfu ➔ Urshifu (Single & Rapid Strike)", "Itens: Scroll of Darkness & Scroll of Waters", new[] { "Poke_ScrollOfDarkness", "Poke_ScrollOfWaters" }, 50),
		new LegendaryFormEvoEntry("Calyrex", "calyrex/shadow", "Calyrex + Glastrier/Spectrier (Ice/Shadow)", "Item: Rédeas da Unidade + Corcéis", new[] { "Poke_ReinsOfUnity", "Poke_IcerootCarrot", "Poke_ShaderootCarrot" }, 0, "Spectrier"),
		new LegendaryFormEvoEntry("Ogerpon", "ogerpon/hearthflame", "Ogerpon (4 Máscaras Elementais)", "Itens: Teal, Wellspring, Hearthflame & Cornerstone", new[] { "Poke_TealMask", "Poke_WellspringMask", "Poke_HearthflameMask", "Poke_CornerstoneMask" }),
		new LegendaryFormEvoEntry("Terapagos", "terapagos/stellar", "Terapagos (Normal ➔ Terastal ➔ Stellar)", "Item: Disco Índigo (Indigo Disk) + Tera Orb", new[] { "Poke_IndigoDisk", "Poke_TeraOrb" })
	};

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
		: base(0, 0, 900, 712)
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
		_currentTab = (Tab)Math.Clamp(tabIndex, 0, 10);
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
		_formsEvosButtons.Clear();
		_audioVfxButtons.Clear();
		_milestonesButtons.Clear();

		int startX = xPositionOnScreen + 30;
		int startY = yPositionOnScreen + 44;

		// 8 Top Tabs across 840px (101px width + 4px gap)
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

		// Lower Tab Bar (Abas Inferiores): Two clean rows across 840px (3 buttons each, w=272, gap=12)
		int bottomRow1Y = yPositionOnScreen + height - 84;
		int bottomRow2Y = yPositionOnScreen + height - 46;
		int bottomW = 264;
		int bottomGap = 12;

		// Row 1: Sub-Tabs 9, 10, 11
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX, bottomRow1Y, bottomW, 32), "FormsAndEvos", "9. 🧬 Formas & Fusões"));
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX + bottomW + bottomGap, bottomRow1Y, bottomW, 32), "AudioAndVfx", "10. 🎬 Sons, Cries & FX"));
		_tabButtons.Add(new ClickableComponent(new Rectangle(startX + (bottomW + bottomGap) * 2, bottomRow1Y, bottomW, 32), "Milestones", "11. 🏆 Marcos & Dex"));

		// Row 2: Action Buttons
		_clearSpawnsBottomButton = new ClickableComponent(new Rectangle(startX, bottomRow2Y, bottomW, 32), "clear_active_spawns", "🧹 Resetar Spawns");
		_clearInventoryBottomButton = new ClickableComponent(new Rectangle(startX + bottomW + bottomGap, bottomRow2Y, bottomW, 32), "clear_inventory", "🗑️ Limpar Mochila");
		_resetMilestonesBottomButton = new ClickableComponent(new Rectangle(startX + (bottomW + bottomGap) * 2, bottomRow2Y, bottomW, 32), "reset_milestones", "🔄 Zerar Marcos Dex");

		int contentY = startY + 50;
		int btnW = 408;
		int col2X = startX + 424;

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

		// 3. Item Buttons & Catalog Mode
		_itemButtons.Clear();
		_itemCategoryFilterButtons.Clear();
		_itemCatalogCards.Clear();

		int itemStartY = contentY + 8;
		_itemCatalogToggleBtn = new ClickableComponent(
			new Rectangle(startX, itemStartY, 840, 32),
			"toggle_catalog_mode",
			_itemCatalogMode ? "◀ Voltar aos Pacotes Rápidos de Itens" : "📂 Abrir Catálogo Completo: Pegar Itens Individuais (Sem Fechar Menu)"
		);

		if (!_itemCatalogMode)
		{
			itemStartY += 38;
			int rowStep = 40;
			_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY, btnW, 36), "pack_balls", "+20 De Todas Poké Bolas (27 Tipos)"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY, btnW, 36), "pack_potions", "+10 Potion, Super Potion & Revive"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep, btnW, 36), "pack_seeds", "+20 De Todas Sementes de Apricorn"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep, btnW, 36), "pack_apricorns", "+20 De Todos Apricorns Colhidos"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 2, btnW, 36), "pack_candy_repel", "+10 Rare/Friend Candy & Repels"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 2, btnW, 36), "pack_field_tms", "+1 Apenas TMs de Campo (HMs)"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 3, btnW, 36), "pack_tms", "+1 De Todos os TMs (Gen 1 a Gen 9)"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 3, btnW, 36), "super_rod", "+1 Vara Super Rod (Pescar Pokémon)"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 4, btnW, 36), "healing_machine", "+1 Mesa de Recuperação Pokémon"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 4, btnW, 36), "pack_stones", "+5 De Todas Pedras de Evolução"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 5, btnW, 36), "pokemon_pc", "💻 +1 PC de Pokémon (Ou Abrir PC)"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 5, btnW, 36), "trade_terminal", "🔄 +1 Terminal de Troca (Ou Abrir)"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 6, btnW, 36), "exp_share_ring", "💍 +1 Anel Exp. Share (Equipável - Gen 6)"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 6, btnW, 36), "gimmick_rings", "💍 +1 Anéis: Mega Bracelete, Z-Ring & Dynamax"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 7, btnW, 36), "choose_mega_stone", "🧬 Escolher Mega Pedra Individual (Seletor)"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 7, btnW, 36), "choose_z_crystal", "💎 Escolher Cristal Z Individual (Seletor)"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 8, btnW, 36), "choose_held_item", "🎒 Escolher Held Item Individual (Seletor)"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(col2X, itemStartY + rowStep * 8, btnW, 36), "pack_held_items", "📦 +1 De Todos os Held Items Competitivos"));
			_itemButtons.Add(new ClickableComponent(new Rectangle(startX, itemStartY + rowStep * 9, btnW * 2 + 16, 38), "all_items", "★ Gerar TODOS os Itens, Anéis, Mega Pedras e Cristais Z de uma vez"));
		}
		else
		{
			// Category filter pills
			string[] categories = { "Todos", "Bolas", "Cura", "Evolução", "Held", "Megas", "Z-Crystals", "Utils", "Plantio" };
			int catW = 88;
			int catGap = 5;
			int filterY = itemStartY + 38;
			for (int c = 0; c < categories.Length; c++)
			{
				string catName = categories[c];
				_itemCategoryFilterButtons.Add(new ClickableComponent(new Rectangle(startX + c * (catW + catGap), filterY, catW, 26), "cat_" + catName, catName));
			}

			// Pagination
			_itemCatPrevBtn = new ClickableComponent(new Rectangle(startX + 692, filterY + 30, 68, 26), "cat_prev", "◄ Ant");
			_itemCatNextBtn = new ClickableComponent(new Rectangle(startX + 766, filterY + 30, 68, 26), "cat_next", "Prox ►");

			// Filtered items
			var catalog = GetFilteredItemCatalog();
			int itemsPerPage = 18;
			int itemMaxPage = Math.Max(0, (catalog.Count - 1) / itemsPerPage);
			if (_itemCatalogPage > itemMaxPage) _itemCatalogPage = itemMaxPage;
			if (_itemCatalogPage < 0) _itemCatalogPage = 0;

			int startIdx = _itemCatalogPage * itemsPerPage;
			int count = Math.Min(itemsPerPage, catalog.Count - startIdx);
			int gridY = filterY + 60;
			int cardW = 272;
			int cardH = 58;

			for (int i = 0; i < count; i++)
			{
				var entry = catalog[startIdx + i];
				int col = i % 3;
				int row = i / 3;
				int bx = startX + col * 280;
				int by = gridY + row * 62;
				_itemCatalogCards.Add(new ClickableComponent(new Rectangle(bx, by, cardW, cardH), entry.ItemId, entry.DisplayName));
			}
		}

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
		string arenaLabel = PokemonBattleMenu.DebugForcedArenaTheme.HasValue
			? $"🏟️ Arena Forçada: {PokemonBattleMenu.DebugForcedArenaTheme.Value} (Clique p/ Trocar)"
			: "🏟️ Arena: Automática / Canônica do Local (Clique p/ Forçar)";
		_battleButtons.Add(new ClickableComponent(new Rectangle(startX, bStartY + 128, btnW * 2 + 16, 36), "cycle_battle_arena", arenaLabel));

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
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY + 352, btnW * 2 + 16, 38), "mech_cycle_tera_type", "8. 💎 Ciclar Tera Type do Pokémon Ativo da Equipe"));
		_mechanicButtons.Add(new ClickableComponent(new Rectangle(startX, mStartY + 394, btnW * 2 + 16, 38), "mech_open_daycare", "🧺 Abrir Menu Daycare (Creche & Cruzamento de Ovos)"));

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

		// 10. FormsAndEvos Buttons (Tab 9 - Abas Inferiores)
		if (_currentTab == Tab.FormsAndEvos)
		{
			int feCardsPerPage = 10;
			int totalFe = AllLegendaryFormEvoEntries.Length;
			int maxFePages = Math.Max(0, (totalFe - 1) / feCardsPerPage);
			_formsEvosPage = Math.Clamp(_formsEvosPage, 0, maxFePages);

			int startFeIdx = _formsEvosPage * feCardsPerPage;
			int endFeIdx = Math.Min(totalFe, startFeIdx + feCardsPerPage);

			int fStartY = contentY + 2;
			int cardH = 68;
			int rowGap = 6;

			for (int idx = startFeIdx; idx < endFeIdx; idx++)
			{
				int slot = idx - startFeIdx;
				int col = slot % 2;
				int row = slot / 2;
				int cardX = (col == 0) ? startX : col2X;
				int cardY = fStartY + row * (cardH + rowGap);

				_formsEvosButtons.Add(new ClickableComponent(
					new Rectangle(cardX + 234, cardY + 14, 84, 38),
					"fe_give_" + idx,
					"🎁 Dar+Kit"
				));
				_formsEvosButtons.Add(new ClickableComponent(
					new Rectangle(cardX + 322, cardY + 14, 84, 38),
					"fe_kit_" + idx,
					"🧭 Habitat"
				));
			}

			_fePrevPageButton = new ClickableComponent(new Rectangle(startX + 180, fStartY + 5 * (cardH + rowGap) + 8, 140, 36), "fe_prev", "◀ Anterior");
			_feNextPageButton = new ClickableComponent(new Rectangle(startX + 520, fStartY + 5 * (cardH + rowGap) + 8, 140, 36), "fe_next", "Próximo ▶");
		}

		if (_currentTab == Tab.AudioAndVfx)
		{
			SetupAudioVfxButtons(startX, contentY);
		}
		if (_currentTab == Tab.Milestones)
		{
			SetupMilestonesButtons(startX, contentY);
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

		if (_clearSpawnsBottomButton != null && _clearSpawnsBottomButton.containsPoint(x, y))
		{
			int removed = PokemonEncounterManager.ClearAllActiveSpawns();
			Game1.playSound("trashcan");
			Game1.addHUDMessage(new HUDMessage($"🧹 {removed} instâncias ativas de Pokémon removidas de todos os mapas!", 2));
			return;
		}

		if (_clearInventoryBottomButton != null && _clearInventoryBottomButton.containsPoint(x, y))
		{
			for (int i = 0; i < Game1.player.Items.Count; i++)
			{
				Game1.player.Items[i] = null;
			}
			Game1.playSound("trashcan");
			Game1.addHUDMessage(new HUDMessage("🗑️ Todo o inventário da mochila foi esvaziado!", 2));
			return;
		}

		if (_resetMilestonesBottomButton != null && _resetMilestonesBottomButton.containsPoint(x, y))
		{
			PokemonPokedex.Instance.ResetClaimedMilestones();
			Game1.playSound("trashcan");
			Game1.addHUDMessage(new HUDMessage("🔄 Marcos da Pokédex foram zerados para teste!", 2));
			if (_currentTab == Tab.Milestones) SetupButtons();
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
			if (_itemCatalogToggleBtn != null && _itemCatalogToggleBtn.containsPoint(x, y))
			{
				_itemCatalogMode = !_itemCatalogMode;
				_itemCatalogPage = 0;
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}

			if (_itemCatalogMode)
			{
				foreach (var catBtn in _itemCategoryFilterButtons)
				{
					if (catBtn.containsPoint(x, y))
					{
						_itemCatalogCategory = catBtn.name.Substring("cat_".Length);
						_itemCatalogPage = 0;
						Game1.playSound("smallSelect");
						SetupButtons();
						return;
					}
				}

				if (_itemCatPrevBtn != null && _itemCatPrevBtn.containsPoint(x, y))
				{
					var catalog = GetFilteredItemCatalog();
					int maxP = Math.Max(0, (catalog.Count - 1) / 18);
					_itemCatalogPage = (_itemCatalogPage - 1 + (maxP + 1)) % (maxP + 1);
					Game1.playSound("shwip");
					SetupButtons();
					return;
				}

				if (_itemCatNextBtn != null && _itemCatNextBtn.containsPoint(x, y))
				{
					var catalog = GetFilteredItemCatalog();
					int maxP = Math.Max(0, (catalog.Count - 1) / 18);
					_itemCatalogPage = (_itemCatalogPage + 1) % (maxP + 1);
					Game1.playSound("shwip");
					SetupButtons();
					return;
				}

				for (int i = 0; i < _itemCatalogCards.Count; i++)
				{
					var card = _itemCatalogCards[i];
					if (card.containsPoint(x, y))
					{
						var catalog = GetFilteredItemCatalog();
						int idx = _itemCatalogPage * 18 + i;
						if (idx >= 0 && idx < catalog.Count)
						{
							GiveCatalogItem(catalog[idx], 1);
						}
						return;
					}
				}
			}
			else
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

						var (loc, tx, ty, itemId, itemName, weather, targetTime, targetSeason) = GetLegendaryHabitat(monName);

						// Environmental Triggers: Apply weather, time of day and season across all location contexts
						if (weather == "Tempestade" || weather == "Chuva" || weather == "Ensolarado")
						{
							bool isRain = weather == "Tempestade" || weather == "Chuva";
							bool isStorm = weather == "Tempestade";
							Game1.isRaining = isRain;
							Game1.isLightning = isStorm;
							if (isRain) Game1.isSnowing = false;
							if (Game1.netWorldState?.Value != null)
							{
								foreach (string ctx in new[] { "Default", "Island", "Desert" })
								{
									var lw = Game1.netWorldState.Value.GetWeatherForLocation(ctx);
									if (lw != null)
									{
										lw.IsRaining = isRain;
										lw.IsLightning = isStorm;
										if (isRain) lw.IsSnowing = false;
										lw.Weather = isStorm ? "Storm" : (isRain ? "Rain" : "Sun");
									}
								}
							}
						}

						if (targetTime > 0)
						{
							Game1.timeOfDay = targetTime;
						}

						if (targetSeason.HasValue)
						{
							Game1.season = targetSeason.Value;
						}

						// Grant summon relic item (2 copies if also used for form change/evolution)
						if (!string.IsNullOrEmpty(itemId))
						{
							int qty = (PokemonItemUse.IsFormChangeItem(itemId) || PokemonItemUse.IsEvolutionItem(itemId)) ? 2 : 1;
							Item relicItem = ItemRegistry.Create("(O)" + itemId, qty, 0, allowNull: true) ?? new Object(itemId, qty);
							Game1.player.addItemToInventory(relicItem);
						}

						bool isFishingLeg = PokemonEncounterManager.IsFishingSummonRelic(itemId);
						if (isFishingLeg && !Game1.player.Items.Any(i => i is StardewValley.Tools.FishingRod))
						{
							Game1.player.addItemToInventory(ItemRegistry.Create("(T)PokemonSuperRod"));
						}

						// Teleport directly to verified walkable habitat coordinates (or Mine level)
						if (loc.StartsWith("UndergroundMine", StringComparison.OrdinalIgnoreCase) &&
						    int.TryParse(loc.Substring("UndergroundMine".Length), out int mineLvl))
						{
							Game1.enterMine(mineLvl);
						}
						else
						{
							Game1.warpFarmer(loc, tx, ty, 2);
						}
						Game1.playSound("wand");
						exitThisMenuNoSound();
						string actionHint = isFishingLeg
							? $"Pesque na água com '{itemName}' no inventário para fisgar {monName}!"
							: $"Coloque '{itemName}' no chão para invocar {monName}!";
						Game1.addHUDMessage(new HUDMessage($"🧭 Habitat de {monName} em {loc}! {actionHint}", 2));
						return;
					}
				}
			}
		}

		// Tab 9: FormsAndEvos (Evoluções, Formas & Fusões Lendárias)
		else if (_currentTab == Tab.FormsAndEvos)
		{
			if (_fePrevPageButton != null && _fePrevPageButton.containsPoint(x, y))
			{
				int totalFe = AllLegendaryFormEvoEntries.Length;
				int maxFePages = Math.Max(0, (totalFe - 1) / 10);
				_formsEvosPage = (_formsEvosPage - 1 + (maxFePages + 1)) % (maxFePages + 1);
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}
			if (_feNextPageButton != null && _feNextPageButton.containsPoint(x, y))
			{
				int totalFe = AllLegendaryFormEvoEntries.Length;
				int maxFePages = Math.Max(0, (totalFe - 1) / 10);
				_formsEvosPage = (_formsEvosPage + 1) % (maxFePages + 1);
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}

			foreach (var fb in _formsEvosButtons)
			{
				if (!fb.containsPoint(x, y)) continue;

				bool isGiveMon = fb.name.StartsWith("fe_give_");
				bool isKitHabitat = fb.name.StartsWith("fe_kit_");
				if (!isGiveMon && !isKitHabitat) continue;

				string idxStr = fb.name.Substring(isGiveMon ? "fe_give_".Length : "fe_kit_".Length);
				if (!int.TryParse(idxStr, out int entryIdx) || entryIdx < 0 || entryIdx >= AllLegendaryFormEvoEntries.Length)
					return;

				var entry = AllLegendaryFormEvoEntries[entryIdx];

				// Grant MasterBalls + Evolution/Transformation/Fusion Items + Rare Candies
				Game1.player.addItemToInventory(ItemRegistry.Create("(O)MasterBall", 10, 0, allowNull: true) ?? new Object("MasterBall", 10));

				if (entry.ExtraItems != null)
				{
					foreach (string exId in entry.ExtraItems)
					{
						if (string.IsNullOrEmpty(exId)) continue;
						if (exId is "Poke_MegaBracelet" or "Poke_ZRing" or "Poke_ZPowerRing" or "Poke_DynamaxBand" or "Poke_TeraOrb")
						{
							GiveEquipRing(exId);
						}
						else
						{
							Item it = ItemRegistry.Create("(O)" + exId, 1, 0, allowNull: true) ?? new Object(exId, 1);
							if (it != null) Game1.player.addItemToInventory(it);
						}
					}
				}

				if (entry.RareCandyCount > 0)
				{
					Item candies = ItemRegistry.Create("(O)PokemonRareCandy", entry.RareCandyCount, 0, allowNull: true) ?? new Object("PokemonRareCandy", entry.RareCandyCount);
					Game1.player.addItemToInventory(candies);
				}

				if (!string.IsNullOrEmpty(entry.PartnerSpecies))
				{
					var partnerSpec = PokemonSpecies.Get(entry.PartnerSpecies);
					if (partnerSpec != null && !Party.Team.Exists(p => string.Equals(p.Species?.Name, partnerSpec.Name, StringComparison.OrdinalIgnoreCase)))
					{
						Party.AddPokemon(new PokemonInstance(partnerSpec, 70));
					}
				}

				if (isGiveMon)
				{
					var baseSpec = PokemonSpecies.Get(entry.BaseName);
					if (baseSpec != null)
					{
						int startLvl = entry.BaseName is "Cosmog" or "Kubfu" or "Meltan" or "Poipole" or "Type: Null" ? 20 : 70;
						Party.AddPokemon(new PokemonInstance(baseSpec, startLvl));
					}
					Game1.playSound("reward");
					Game1.addHUDMessage(new HUDMessage($"🎁 {entry.BaseName} + Kit de Evolução/Transformação entregues!", 2));
					return;
				}

				// Kit + Habitat Teleport
				var (loc, tx, ty, itemId, itemName, weather, targetTime, targetSeason) = GetLegendaryHabitat(entry.BaseName);

				if (weather == "Tempestade" || weather == "Chuva" || weather == "Ensolarado")
				{
					bool isRain = weather == "Tempestade" || weather == "Chuva";
					bool isStorm = weather == "Tempestade";
					Game1.isRaining = isRain;
					Game1.isLightning = isStorm;
					if (isRain) Game1.isSnowing = false;
					if (Game1.netWorldState?.Value != null)
					{
						foreach (string ctx in new[] { "Default", "Island", "Desert" })
						{
							var lw = Game1.netWorldState.Value.GetWeatherForLocation(ctx);
							if (lw != null)
							{
								lw.IsRaining = isRain;
								lw.IsLightning = isStorm;
								if (isRain) lw.IsSnowing = false;
								lw.Weather = isStorm ? "Storm" : (isRain ? "Rain" : "Sun");
							}
						}
					}
				}

				if (targetTime > 0) Game1.timeOfDay = targetTime;
				if (targetSeason.HasValue) Game1.season = targetSeason.Value;

				if (!string.IsNullOrEmpty(itemId))
				{
					Item relicItem = ItemRegistry.Create("(O)" + itemId, 2, 0, allowNull: true) ?? new Object(itemId, 2);
					Game1.player.addItemToInventory(relicItem);
				}

				bool isFishingLeg = PokemonEncounterManager.IsFishingSummonRelic(itemId);
				if (isFishingLeg && !Game1.player.Items.Any(i => i is StardewValley.Tools.FishingRod))
				{
					Game1.player.addItemToInventory(ItemRegistry.Create("(T)PokemonSuperRod"));
				}

				if (loc.StartsWith("UndergroundMine", StringComparison.OrdinalIgnoreCase) &&
				    int.TryParse(loc.Substring("UndergroundMine".Length), out int mineLvl))
				{
					Game1.enterMine(mineLvl);
				}
				else
				{
					Game1.warpFarmer(loc, tx, ty, 2);
				}
				Game1.playSound("wand");
				exitThisMenuNoSound();
				Game1.addHUDMessage(new HUDMessage($"🧪 Kit de {entry.Title} + 10 MasterBall entregues! Invoque {entry.BaseName} em {loc}!", 2));
				return;
			}
		}
		else if (_currentTab == Tab.AudioAndVfx)
		{
			HandleAudioVfxClick(x, y);
			return;
		}
		else if (_currentTab == Tab.Milestones)
		{
			HandleMilestonesClick(x, y);
			return;
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

		if (action == "cycle_battle_arena")
		{
			if (!PokemonBattleMenu.DebugForcedArenaTheme.HasValue)
			{
				PokemonBattleMenu.DebugForcedArenaTheme = PokemonBattleMenu.ArenaTheme.Farm;
			}
			else
			{
				int next = (int)PokemonBattleMenu.DebugForcedArenaTheme.Value + 1;
				if (next > (int)PokemonBattleMenu.ArenaTheme.Gym)
				{
					PokemonBattleMenu.DebugForcedArenaTheme = null;
				}
				else
				{
					PokemonBattleMenu.DebugForcedArenaTheme = (PokemonBattleMenu.ArenaTheme)next;
				}
			}
			Game1.playSound("smallSelect");
			SetupButtons();
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
		else if (action == "mech_open_daycare")
		{
			Game1.playSound("bigSelect");
			Game1.activeClickableMenu = new PokemonDaycareMenu();
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
			_currentTab = (Tab)(((int)_currentTab + 10) % 11);
			SetupButtons();
			Game1.playSound("shwip");
			return;
		}
		if (button == Buttons.RightShoulder || button == Buttons.RightTrigger)
		{
			_currentTab = (Tab)(((int)_currentTab + 1) % 11);
			SetupButtons();
			Game1.playSound("shwip");
			return;
		}
	}

	public override void update(GameTime time)
	{
		base.update(time);
		float dt = (float)time.ElapsedGameTime.TotalSeconds;
		if (_vfxAnimTimer > 0f) { _vfxAnimTimer -= dt; if (_vfxAnimTimer <= 0f) _activeVfxAnim = null; }
		if (_cutsceneTimer > 0f) { _cutsceneTimer -= dt; if (_cutsceneTimer <= 0f) _activeCutscene = null; }
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

	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		base.receiveRightClick(x, y, playSound);

		if (_currentTab == Tab.Items && _itemCatalogMode)
		{
			for (int i = 0; i < _itemCatalogCards.Count; i++)
			{
				var card = _itemCatalogCards[i];
				if (card.containsPoint(x, y))
				{
					var catalog = GetFilteredItemCatalog();
					int idx = _itemCatalogPage * 18 + i;
					if (idx >= 0 && idx < catalog.Count)
					{
						GiveCatalogItem(catalog[idx], 10);
					}
					return;
				}
			}
		}
	}

	private void GiveCatalogItem(ModItemCatalogEntry entry, int amount)
	{
		try
		{
			Item item = ItemRegistry.Create(entry.FullId, amount);
			if (item == null && entry.FullId.StartsWith("(O)"))
			{
				item = ItemRegistry.Create(entry.ItemId, amount) ?? new StardewValley.Object(entry.ItemId, amount);
			}
			if (item != null)
			{
				Game1.player.addItemToInventoryBool(item);
				Game1.playSound("coin");
				Game1.addHUDMessage(new HUDMessage($"✨ +{amount} {entry.DisplayName}!", 2));
			}
			else
			{
				Game1.playSound("cancel");
			}
		}
		catch (Exception ex)
		{
			Game1.log.Error($"Erro ao gerar item individual {entry.ItemId}: {ex.Message}");
		}
	}

	private List<ModItemCatalogEntry> GetFilteredItemCatalog()
	{
		var all = GetAllCatalogItems();
		if (string.Equals(_itemCatalogCategory, "Todos", StringComparison.OrdinalIgnoreCase))
		{
			return all;
		}
		return all.FindAll(e => string.Equals(e.Category, _itemCatalogCategory, StringComparison.OrdinalIgnoreCase));
	}

	private static List<ModItemCatalogEntry> GetAllCatalogItems()
	{
		if (_allCatalogItems != null) return _allCatalogItems;

		var list = new List<ModItemCatalogEntry>();

		// 1. Bolas (27 tipos)
		string[] ballIds = {
			"PokeBall", "GreatBall", "UltraBall", "MasterBall", "SafariBall", "FastBall", "LevelBall",
			"LureBall", "HeavyBall", "LoveBall", "FriendBall", "MoonBall", "SportBall", "NetBall",
			"DiveBall", "NestBall", "RepeatBall", "TimerBall", "LuxuryBall", "PremierBall", "DuskBall",
			"HealBall", "QuickBall", "CherishBall", "ParkBall", "DreamBall", "BeastBall"
		};
		foreach (var b in ballIds)
		{
			list.Add(new ModItemCatalogEntry(b, "(O)" + b, ItemRegistry.GetData("(O)" + b)?.DisplayName ?? b, "Bolas"));
		}

		// 2. Cura & Doces
		(string id, string name)[] meds = {
			("PokemonPotion", "Poção"),
			("PokemonSuperPotion", "Super Poção"),
			("PokemonHyperPotion", "Hiper Poção"),
			("PokemonMaxPotion", "Poção Máxima"),
			("PokemonFullRestore", "Restaurador Total"),
			("PokemonRevive", "Reviver"),
			("PokemonMaxRevive", "Max Reviver"),
			("PokemonRareCandy", "Doce Raro (+1 Lv)"),
			("Poke_FriendshipSweet", "Doce da Amizade (+30)"),
			("PokemonFriendshipCandy", "Bala de Amizade Máxima (255)"),
			("Poke_AbilityPatch", "Ability Patch (Hidden Ability)"),
			("Poke_AbilityCapsule", "Ability Capsule"),
			("PokemonAntidote", "Antídoto"),
			("PokemonParalyzeHeal", "Cura Paralisia"),
			("PokemonAwakening", "Despertador"),
			("PokemonBurnHeal", "Cura Queimadura"),
			("PokemonIceHeal", "Cura Congelamento"),
			("PokemonFullHeal", "Cura Total"),
			("PokemonRepel", "Repelente"),
			("PokemonSuperRepel", "Super Repelente"),
			("PokemonMaxRepel", "Max Repelente")
		};
		foreach (var (mId, mName) in meds)
		{
			list.Add(new ModItemCatalogEntry(mId, "(O)" + mId, mName, "Cura"));
		}

		// 3. Pedras & Evolução
		(string id, string name)[] evos = {
			("PokemonFireStone", "Pedra do Fogo"),
			("PokemonWaterStone", "Pedra da Água"),
			("PokemonThunderStone", "Pedra do Trovão"),
			("PokemonLeafStone", "Pedra da Folha"),
			("PokemonMoonStone", "Pedra da Lua"),
			("PokemonSunStone", "Pedra do Sol"),
			("PokemonShinyStone", "Pedra do Brilho"),
			("PokemonDuskStone", "Pedra do Anoitecer"),
			("PokemonDawnStone", "Pedra da Alvorada"),
			("PokemonIceStone", "Pedra do Gelo"),
			("PokemonOvalStone", "Pedra Oval"),
			("KingsRock", "Rocha do Rei"),
			("MetalCoat", "Revestimento Metálico"),
			("DragonScale", "Escama de Dragão"),
			("UpGrade", "Upgrade"),
			("DubiousDisc", "Disco Duvidoso"),
			("Protector", "Protetor"),
			("Electirizer", "Eletrizador"),
			("Magmarizer", "Magmarizador"),
			("RazorClaw", "Garra Afiada"),
			("RazorFang", "Presa Afiada"),
			("ReaperCloth", "Manto Noturno"),
			("PrismScale", "Escama Prisma"),
			("WhippedDream", "Sonho Batido"),
			("Sachet", "Sachê Aromático")
		};
		foreach (var (eId, eName) in evos)
		{
			list.Add(new ModItemCatalogEntry(eId, "(O)" + eId, eName, "Evolução"));
		}

		// 4. Utilitários, Anéis & Formas Lendárias
		(string id, string fullId, string name)[] utils = {
			("Poke_RunningShoes", "(R)Poke_RunningShoes", "Tênis de Corrida (+2)"),
			("Poke_ShinyCharm", "(R)Poke_ShinyCharm", "Talismã Shiny (1/128)"),
			("Poke_PortableHealMachine", "(O)Poke_PortableHealMachine", "Centro Pokémon Portátil"),
			("Poke_BoxLink", "(O)Poke_BoxLink", "Box Link Portátil (PC)"),
			("Poke_MegaStoneSelector", "(O)Poke_MegaStoneSelector", "Seletor de Mega Pedras"),
			("PokemonExpShare", "(R)PokemonExpShare", "Exp. Share (Anel Gen 6)"),
			("Poke_MegaBracelet", "(R)Poke_MegaBracelet", "Mega Bracelete (Anel)"),
			("Poke_ZRing", "(R)Poke_ZRing", "Z-Ring (Anel)"),
			("Poke_ZPowerRing", "(R)Poke_ZPowerRing", "Z-Power Ring (Anel)"),
			("Poke_DynamaxBand", "(R)Poke_DynamaxBand", "Bracelete Dynamax (Anel)"),
			("Poke_TeraOrb", "(R)Poke_TeraOrb", "Orbe Terastal (Anel)"),
			("PokemonPC", "(BC)PokemonPC", "Computador Pokémon (Móvel)"),
			("PokemonTradeTerminal", "(BC)PokemonTradeTerminal", "Terminal de Trocas (Móvel)"),
			("PokemonHealingMachine", "(BC)PokemonHealingMachine", "Mesa de Cura (Móvel)"),
			("PokemonSuperRod", "(T)PokemonSuperRod", "Super Rod (Pescar)"),
			("Poke_ScrollOfDarkness", "(O)Poke_ScrollOfDarkness", "Pergaminho das Trevas"),
			("Poke_ScrollOfWaters", "(O)Poke_ScrollOfWaters", "Pergaminho das Águas"),
			("Poke_AdamantCrystal", "(O)Poke_AdamantCrystal", "Cristal Adamante (Dialga)"),
			("Poke_LustrousGlobe", "(O)Poke_LustrousGlobe", "Globo Lustroso (Palkia)"),
			("Poke_GriseousCore", "(O)Poke_GriseousCore", "Núcleo Griseous (Giratina)"),
			("Poke_Gracidea", "(O)Poke_Gracidea", "Flor Gracídea (Shaymin)"),
			("Poke_PrisonBottle", "(O)Poke_PrisonBottle", "Garrafa Prisão (Hoopa)"),
			("Poke_Meteorite", "(O)Poke_Meteorite", "Meteorito (Rayquaza)"),
			("Poke_ReinsOfUnity", "(O)Poke_ReinsOfUnity", "Rédeas da Unidade (Calyrex)"),
			("Poke_RustedSword", "(O)Poke_RustedSword", "Espada Enferrujada (Zacian)"),
			("Poke_RustedShield", "(O)Poke_RustedShield", "Escudo Enferrujado (Zamazenta)"),
			("Poke_DNASplicers", "(O)Poke_DNASplicers", "Fundidores de DNA (Kyurem)")
		};
		foreach (var (uId, uFull, uName) in utils)
		{
			list.Add(new ModItemCatalogEntry(uId, uFull, uName, "Utils"));
		}

		// 5. Held Items Competitivos
		foreach (var (hId, hName, hDesc, hCost) in PokemonHeldItemManager.GetAllHeldItems())
		{
			list.Add(new ModItemCatalogEntry(hId, "(O)" + hId, hName, "Held"));
		}

		// 6. Mega Stones
		foreach (var kv in PokemonItemManager.MegaStoneDefinitions)
		{
			list.Add(new ModItemCatalogEntry(kv.Key, "(O)" + kv.Key, kv.Value.DisplayName, "Megas"));
		}

		// 7. Cristais Z
		foreach (var kv in PokemonItemManager.ZCrystalDefinitions)
		{
			list.Add(new ModItemCatalogEntry(kv.Key, "(O)" + kv.Key, kv.Value.DisplayName, "Z-Crystals"));
		}
		foreach (var kv in PokemonItemManager.SignatureZCrystalDefinitions)
		{
			list.Add(new ModItemCatalogEntry(kv.Key, "(O)" + kv.Key, kv.Value.DisplayName, "Z-Crystals"));
		}

		// 8. Plantio (Apricorns & Berries)
		foreach (var c in PokemonItemManager.ApricornColors)
		{
			list.Add(new ModItemCatalogEntry($"Poke{c}Seed", $"(O)Poke{c}Seed", $"Semente Apricorn {c}", "Plantio"));
			list.Add(new ModItemCatalogEntry($"Poke{c}Apricorn", $"(O)Poke{c}Apricorn", $"Apricorn {c}", "Plantio"));
		}
		string[] berries = { "OranBerry", "SitrusBerry", "LumBerry", "PechaBerry", "CheriBerry", "ChestoBerry", "RawstBerry", "AspearBerry", "LeppaBerry", "PersimBerry" };
		foreach (var br in berries)
		{
			list.Add(new ModItemCatalogEntry(br, "(O)" + br, ItemRegistry.GetData("(O)" + br)?.DisplayName ?? br, "Plantio"));
			list.Add(new ModItemCatalogEntry(br + "Seeds", "(O)" + br + "Seeds", $"Semente {br}", "Plantio"));
		}

		_allCatalogItems = list;
		return _allCatalogItems;
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

		// Draw Tab Buttons (Top + Bottom Abas Inferiores)
		foreach (var tab in _tabButtons)
		{
			bool isCurrent = tab.name == _currentTab.ToString();
			bool hovered = tab.containsPoint(mouseX, mouseY);
			Color col = isCurrent ? Color.LightGreen : (hovered ? Color.Wheat : Color.White);
			IClickableMenu.drawTextureBox(b, tab.bounds.X, tab.bounds.Y, tab.bounds.Width, tab.bounds.Height, col);
			PokemonUI.Text(b, Game1.smallFont, tab.label, tab.bounds, isCurrent ? Color.DarkGreen : Game1.textColor, center: true, maxScale: 0.78f, shadow: false);
		}

		if (_clearSpawnsBottomButton != null)
		{
			bool hovered = _clearSpawnsBottomButton.containsPoint(mouseX, mouseY);
			Color col = hovered ? new Color(255, 200, 200) : new Color(255, 238, 238);
			IClickableMenu.drawTextureBox(b, _clearSpawnsBottomButton.bounds.X, _clearSpawnsBottomButton.bounds.Y, _clearSpawnsBottomButton.bounds.Width, _clearSpawnsBottomButton.bounds.Height, col);
			PokemonUI.Text(b, Game1.smallFont, _clearSpawnsBottomButton.label, _clearSpawnsBottomButton.bounds, hovered ? Color.DarkRed : new Color(140, 25, 25), center: true, maxScale: 0.76f, shadow: false);
		}

		if (_clearInventoryBottomButton != null)
		{
			bool hovered = _clearInventoryBottomButton.containsPoint(mouseX, mouseY);
			Color col = hovered ? new Color(255, 215, 200) : new Color(255, 242, 235);
			IClickableMenu.drawTextureBox(b, _clearInventoryBottomButton.bounds.X, _clearInventoryBottomButton.bounds.Y, _clearInventoryBottomButton.bounds.Width, _clearInventoryBottomButton.bounds.Height, col);
			PokemonUI.Text(b, Game1.smallFont, _clearInventoryBottomButton.label, _clearInventoryBottomButton.bounds, hovered ? Color.DarkRed : new Color(150, 45, 20), center: true, maxScale: 0.74f, shadow: false);
		}

		if (_resetMilestonesBottomButton != null)
		{
			bool hovered = _resetMilestonesBottomButton.containsPoint(mouseX, mouseY);
			Color col = hovered ? new Color(255, 235, 180) : new Color(255, 248, 225);
			IClickableMenu.drawTextureBox(b, _resetMilestonesBottomButton.bounds.X, _resetMilestonesBottomButton.bounds.Y, _resetMilestonesBottomButton.bounds.Width, _resetMilestonesBottomButton.bounds.Height, col);
			PokemonUI.Text(b, Game1.smallFont, _resetMilestonesBottomButton.label, _resetMilestonesBottomButton.bounds, hovered ? Color.DarkGoldenrod : new Color(160, 110, 20), center: true, maxScale: 0.74f, shadow: false);
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
			if (_itemCatalogToggleBtn != null)
			{
				bool hov = _itemCatalogToggleBtn.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, _itemCatalogToggleBtn.bounds.X, _itemCatalogToggleBtn.bounds.Y, _itemCatalogToggleBtn.bounds.Width, _itemCatalogToggleBtn.bounds.Height, hov ? new Color(255, 240, 200) : new Color(240, 248, 255));
				PokemonUI.Text(b, Game1.smallFont, _itemCatalogToggleBtn.label, _itemCatalogToggleBtn.bounds, hov ? Color.DarkRed : Color.DarkBlue, center: true, maxScale: 0.74f, shadow: false);
			}

			if (!_itemCatalogMode)
			{
				foreach (var ib in _itemButtons)
				{
					bool hov = ib.containsPoint(mouseX, mouseY);
					IClickableMenu.drawTextureBox(b, ib.bounds.X, ib.bounds.Y, ib.bounds.Width, ib.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
					PokemonUI.Text(b, Game1.smallFont, ib.label, new Rectangle(ib.bounds.X + 12, ib.bounds.Y + 6, ib.bounds.Width - 24, ib.bounds.Height - 12), hov ? Color.DarkRed : Game1.textColor, center: true, maxScale: 0.76f, shadow: false);
				}
			}
			else
			{
				// Category filter pills
				foreach (var cf in _itemCategoryFilterButtons)
				{
					string cName = cf.name.Substring("cat_".Length);
					bool active = string.Equals(_itemCatalogCategory, cName, StringComparison.OrdinalIgnoreCase);
					bool hov = cf.containsPoint(mouseX, mouseY);
					DrawFilterPill(b, cf.bounds, cf.label, active, hov);
				}

				// Page string
				var catalog = GetFilteredItemCatalog();
				int maxP = Math.Max(0, (catalog.Count - 1) / 18);
				string pageStr = $"Página {_itemCatalogPage + 1} de {maxP + 1} ({catalog.Count} itens)";
				PokemonUI.Text(b, Game1.tinyFont, pageStr, new Rectangle(startX + 10, contentY + 74, 300, 22), Color.DarkSlateBlue, center: false, maxScale: 0.74f, shadow: false);

				if (_itemCatPrevBtn != null)
				{
					bool hov = _itemCatPrevBtn.containsPoint(mouseX, mouseY);
					DrawFilterPill(b, _itemCatPrevBtn.bounds, _itemCatPrevBtn.label, false, hov);
				}
				if (_itemCatNextBtn != null)
				{
					bool hov = _itemCatNextBtn.containsPoint(mouseX, mouseY);
					DrawFilterPill(b, _itemCatNextBtn.bounds, _itemCatNextBtn.label, false, hov);
				}

				// Render Item Cards
				int startIdx = _itemCatalogPage * 18;
				for (int i = 0; i < _itemCatalogCards.Count; i++)
				{
					var card = _itemCatalogCards[i];
					int idx = startIdx + i;
					if (idx >= catalog.Count) break;
					var entry = catalog[idx];

					bool hov = card.containsPoint(mouseX, mouseY);
					Color bgCol = hov ? new Color(255, 248, 220) : new Color(245, 247, 250);
					IClickableMenu.drawTextureBox(b, card.bounds.X, card.bounds.Y, card.bounds.Width, card.bounds.Height, bgCol);

					// Icon Box (40x40)
					int iconBoxX = card.bounds.X + 8;
					int iconBoxY = card.bounds.Y + (card.bounds.Height - 40) / 2;
					b.Draw(Game1.staminaRect, new Rectangle(iconBoxX, iconBoxY, 40, 40), new Color(225, 230, 240));
					PokemonUI.DrawCardBorder(b, new Rectangle(iconBoxX, iconBoxY, 40, 40), new Color(180, 195, 215), 1);

					if (PokemonItemManager.TryGetCustomTexture(entry.ItemId, out Texture2D tex, out Rectangle srcRect) && tex != null)
					{
						float sc = Math.Min(34f / Math.Max(1, srcRect.Width), 34f / Math.Max(1, srcRect.Height));
						Vector2 pos = new Vector2(iconBoxX + (40 - srcRect.Width * sc) / 2f, iconBoxY + (40 - srcRect.Height * sc) / 2f);
						b.Draw(tex, pos, srcRect, Color.White, 0f, Vector2.Zero, sc, SpriteEffects.None, 0.88f);
					}
					else
					{
						Texture2D genericBall = PokemonItemManager.GetItemBallTexture();
						if (genericBall != null)
						{
							b.Draw(genericBall, new Vector2(iconBoxX + 8, iconBoxY + 8), Color.White);
						}
					}

					// Labels
					int textX = iconBoxX + 46;
					int textW = card.bounds.Width - 54;
					PokemonUI.Text(b, Game1.smallFont, entry.DisplayName, new Rectangle(textX, card.bounds.Y + 8, textW, 20), hov ? Color.DarkRed : Color.Black, center: false, maxScale: 0.70f, shadow: false);
					string subText = $"[{entry.Category}] Esq: +1 | Dir: +10";
					PokemonUI.Text(b, Game1.tinyFont, subText, new Rectangle(textX, card.bounds.Y + 30, textW, 16), Color.DimGray, center: false, maxScale: 0.65f, shadow: false);
				}
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

		// Tab 9: FormsAndEvos (Evoluções, Formas & Fusões Lendárias)
		else if (_currentTab == Tab.FormsAndEvos)
		{
			int feCardsPerPage = 10;
			int totalFe = AllLegendaryFormEvoEntries.Length;
			int maxFePages = Math.Max(0, (totalFe - 1) / feCardsPerPage);
			int startFeIdx = _formsEvosPage * feCardsPerPage;
			int endFeIdx = Math.Min(totalFe, startFeIdx + feCardsPerPage);

			int fStartY = contentY + 2;
			int col2X = startX + 424;
			int cardH = 68;
			int cardW = 412;
			int rowGap = 6;

			for (int idx = startFeIdx; idx < endFeIdx; idx++)
			{
				int slot = idx - startFeIdx;
				int col = slot % 2;
				int row = slot / 2;
				int cardX = (col == 0) ? startX : col2X;
				int cardY = fStartY + row * (cardH + rowGap);

				var entry = AllLegendaryFormEvoEntries[idx];
				var spec = PokemonSpecies.Get(entry.PreviewKey) ?? PokemonSpecies.Get(entry.BaseName);

				Rectangle cardBox = new Rectangle(cardX, cardY, cardW, cardH);
				IClickableMenu.drawTextureBox(b, cardBox.X, cardBox.Y, cardBox.Width, cardBox.Height, Color.White);

				if (spec != null)
				{
					var inst = GetPreviewInstance(spec, false);
					Texture2D icon = inst?.GetIconSprite(Game1.graphics.GraphicsDevice) ?? inst?.GetFrontSprite(Game1.graphics.GraphicsDevice);
					if (icon != null)
					{
						bool isFront = icon == inst.GetFrontSprite(Game1.graphics.GraphicsDevice);
						Rectangle srcRect = isFront ? inst.GetFrontSourceRect() : inst.GetIconSourceRect();
						float scale = isFront ? (42f / Math.Max(1, srcRect.Width)) : 1.15f;
						b.Draw(icon, new Vector2(cardX + 8, cardY + 12), srcRect, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
					}
				}

				PokemonUI.Text(b, Game1.smallFont, entry.Title, new Rectangle(cardX + 52, cardY + 10, 178, 22), Color.DarkSlateBlue, center: false, maxScale: 0.64f, shadow: false);
				PokemonUI.Text(b, Game1.tinyFont, entry.Subtitle, new Rectangle(cardX + 52, cardY + 34, 178, 20), Color.DimGray, center: false, maxScale: 0.78f, shadow: false);
			}

			foreach (var fb in _formsEvosButtons)
			{
				bool hov = fb.containsPoint(mouseX, mouseY);
				Color btnColor = fb.name.StartsWith("fe_give_")
					? (hov ? Color.LightGreen : Color.Honeydew)
					: (hov ? Color.LightSkyBlue : Color.AliceBlue);
				IClickableMenu.drawTextureBox(b, fb.bounds.X, fb.bounds.Y, fb.bounds.Width, fb.bounds.Height, btnColor);
				PokemonUI.Text(b, Game1.smallFont, fb.label, new Rectangle(fb.bounds.X + 4, fb.bounds.Y + 4, fb.bounds.Width - 8, fb.bounds.Height - 8), Color.Black, center: true, maxScale: 0.72f, shadow: false);
			}

			if (_fePrevPageButton != null)
			{
				bool hov = _fePrevPageButton.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, _fePrevPageButton.bounds.X, _fePrevPageButton.bounds.Y, _fePrevPageButton.bounds.Width, _fePrevPageButton.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
				PokemonUI.Text(b, Game1.smallFont, _fePrevPageButton.label, _fePrevPageButton.bounds, hov ? Color.DarkBlue : Game1.textColor, center: true, maxScale: 0.82f, shadow: false);
			}
			if (_feNextPageButton != null)
			{
				bool hov = _feNextPageButton.containsPoint(mouseX, mouseY);
				IClickableMenu.drawTextureBox(b, _feNextPageButton.bounds.X, _feNextPageButton.bounds.Y, _feNextPageButton.bounds.Width, _feNextPageButton.bounds.Height, hov ? Color.PaleGoldenrod : Color.White);
				PokemonUI.Text(b, Game1.smallFont, _feNextPageButton.label, _feNextPageButton.bounds, hov ? Color.DarkBlue : Game1.textColor, center: true, maxScale: 0.82f, shadow: false);
			}
			PokemonUI.Text(b, Game1.smallFont, $"Página {_formsEvosPage + 1} de {maxFePages + 1}", new Rectangle(startX + 330, fStartY + 5 * (cardH + rowGap) + 12, 180, 28), Game1.textColor, center: true, maxScale: 0.82f, shadow: false);
		}

		else if (_currentTab == Tab.AudioAndVfx)
		{
			DrawAudioAndVfxTab(b, startX, contentY, mouseX, mouseY);
		}
		else if (_currentTab == Tab.Milestones)
		{
			DrawMilestonesTab(b, startX, contentY, mouseX, mouseY);
		}

		if (_activeCutscene != null)
		{
			DrawCutsceneOverlay(b);
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

	public static (string loc, int x, int y, string itemId, string itemName, string weather, int timeOfDay, Season? season) GetLegendaryHabitat(string name)
	{
		return name switch
		{
			"Articuno" => ("UndergroundMine60", 6, 12, "Poke_FreezingFeather", "Pena Glacial", "Qualquer", 0, Season.Winter),
			"Zapdos" => ("Mountain", 26, 11, "Poke_ElectricFeather", "Pena Elétrica", "Tempestade", 0, null),
			"Moltres" => ("Caldera", 24, 28, "Poke_FlameFeather", "Pena de Chamas", "Qualquer", 0, Season.Summer),
			"Mewtwo" => ("SkullCave", 4, 5, "Poke_BerserkGene", "Gene Berserk", "Qualquer", 2200, null),
			"Mew" => ("IslandSouthEast", 22, 16, "Poke_OldSeaMap", "Mapa do Mar Antigo", "Ensolarado", 1200, Season.Spring),
			"Raikou" => ("Woods", 25, 15, "Poke_RaikouBell", "Sino do Trovão", "Tempestade", 2200, null),
			"Entei" => ("Mountain", 44, 30, "Poke_EnteiBell", "Sino do Magma", "Ensolarado", 1200, Season.Summer),
			"Suicune" => ("Beach", 25, 34, "Poke_SuicuneBell", "Sino das Ondas", "Chuva", 2200, Season.Fall),
			"Lugia" => ("Beach", 28, 36, "Poke_TidalBell", "Sino das Marés", "Tempestade", 2200, Season.Winter),
			"Ho-Oh" => ("Summit", 10, 22, "Poke_RainbowWing", "Asa do Arco-Íris", "Ensolarado", 1200, null),
			"Celebi" => ("Woods", 12, 14, "Poke_GSBall", "GS Ball", "Ensolarado", 1200, Season.Spring),
			"Regirock" => ("UndergroundMine20", 6, 12, "Poke_RelicRock", "Núcleo de Pedra Ancestral", "Qualquer", 0, null),
			"Regice" => ("UndergroundMine60", 6, 12, "Poke_RelicIce", "Núcleo de Gelo Eterno", "Qualquer", 0, Season.Winter),
			"Registeel" => ("UndergroundMine100", 6, 12, "Poke_RelicSteel", "Núcleo de Aço Cósmico", "Qualquer", 0, null),
			"Latias" => ("IslandWest", 76, 61, "Poke_EonFlute_Latias", "Flauta Eon Escarlate", "Ensolarado", 1200, Season.Spring),
			"Latios" => ("Beach", 38, 23, "Poke_EonFlute_Latios", "Flauta Eon Anil", "Ensolarado", 1200, Season.Summer),
			"Kyogre" => ("Beach", 12, 38, "Poke_BlueOrb", "Esfera Azul Primordial", "Tempestade", 0, null),
			"Groudon" => ("Caldera", 23, 28, "Poke_RedOrb", "Esfera Vermelha Primordial", "Ensolarado", 1200, Season.Summer),
			"Rayquaza" => ("Summit", 10, 22, "Poke_JadeOrb", "Esfera de Jade", "Tempestade", 2200, null),
			"Jirachi" => ("Mountain", 44, 30, "Poke_CometShard", "Fragmento de Cometa", "Ensolarado", 2200, Season.Summer),
			"Deoxys" => ("Railroad", 28, 43, "Poke_MeteoritePiece", "Pedaço de Meteorito", "Qualquer", 2200, null),
			"Uxie" => ("Mountain", 44, 30, "Poke_MindGem", "Gema da Sabedoria", "Qualquer", 2200, null),
			"Mesprit" => ("Woods", 12, 14, "Poke_EmotionGem", "Gema da Emoção", "Qualquer", 1200, Season.Spring),
			"Azelf" => ("Forest", 34, 60, "Poke_ValorGem", "Gema da Bravura", "Qualquer", 1200, null),
			"Dialga" => ("Summit", 10, 22, "Poke_AdamantOrb", "Adamant Orb", "Qualquer", 1200, null),
			"Palkia" => ("Summit", 11, 24, "Poke_LustrousOrb", "Lustrous Orb", "Qualquer", 2200, null),
			"Heatran" => ("Caldera", 25, 28, "Poke_MagmaStone", "Pedra de Magma", "Qualquer", 0, Season.Summer),
			"Regigigas" => ("UndergroundMine20", 6, 12, "Poke_ColossalKey", "Chave Colossal", "Qualquer", 0, null),
			"Giratina" => ("Sewer", 16, 17, "Poke_GriseousCore", "Griseous Core", "Qualquer", 2200, null),
			"Cresselia" => ("Beach", 25, 34, "Poke_LunarWing", "Pena Lunar", "Ensolarado", 2200, null),
			"Phione" => ("Beach", 12, 38, "Poke_SeaDriftTear", "Lágrima das Correntes", "Chuva", 1200, Season.Summer),
			"Manaphy" => ("Beach", 13, 38, "Poke_SeaCrown", "Coroa do Oceano", "Chuva", 1200, Season.Summer),
			"Darkrai" => ("Railroad", 33, 54, "Poke_MemberCard", "Cartão de Membro Sombrio", "Qualquer", 2200, Season.Fall),
			"Shaymin" => ("Forest", 64, 40, "Poke_GracideaFlower", "Flor Gracídea", "Ensolarado", 1200, Season.Spring),
			"Arceus" => ("Summit", 10, 22, "Poke_AzureFlute", "Flauta Azure", "Ensolarado", 1200, null),
			"Victini" => ("IslandSouthEast", 22, 16, "Poke_LibertyPass", "Passe da Liberdade", "Ensolarado", 1200, null),
			"Cobalion" => ("UndergroundMine100", 6, 12, "Poke_IronWillRelic", "Relíquia da Lâmina de Ferro", "Qualquer", 0, null),
			"Terrakion" => ("Mountain", 44, 7, "Poke_StoneWillRelic", "Relíquia da Lâmina de Rocha", "Qualquer", 0, null),
			"Virizion" => ("Woods", 25, 15, "Poke_GrassWillRelic", "Relíquia da Lâmina Esmeralda", "Ensolarado", 1200, Season.Spring),
			"Tornadus" => ("Mountain", 26, 11, "Poke_CycloneMirror", "Espelho dos Ciclones", "Tempestade", 0, Season.Fall),
			"Thundurus" => ("Railroad", 28, 43, "Poke_BoltMirror", "Espelho dos Trovões", "Tempestade", 2200, Season.Summer),
			"Reshiram" => ("Caldera", 24, 28, "Poke_LightStone", "Light Stone", "Ensolarado", 1200, Season.Summer),
			"Zekrom" => ("Desert", 19, 12, "Poke_DarkStone", "Dark Stone", "Qualquer", 2200, null),
			"Landorus" => ("Forest", 64, 40, "Poke_FertilityMirror", "Espelho da Fertilidade", "Ensolarado", 1200, Season.Spring),
			"Kyurem" => ("UndergroundMine60", 6, 12, "Poke_DNA_Splicers", "Fundidores de DNA", "Qualquer", 0, Season.Winter),
			"Keldeo" => ("Forest", 34, 60, "Poke_SecretSwordRelic", "Relíquia da Espada Secreta", "Chuva", 1200, null),
			"Meloetta" => ("Town", 51, 59, "Poke_RelicSongSheet", "Partitura Ancestral", "Qualquer", 2200, Season.Spring),
			"Genesect" => ("UndergroundMine100", 6, 12, "Poke_DriveBurn", "Burn Drive Tecnológico", "Qualquer", 0, null),
			"Xerneas" => ("Woods", 25, 15, "Poke_LifeTreeRoot", "Raiz da Árvore da Vida", "Ensolarado", 1200, Season.Spring),
			"Yveltal" => ("SkullCave", 7, 6, "Poke_DestructionCocoon", "Fragmento de Casulo Noturno", "Qualquer", 2200, Season.Fall),
			"Zygarde" => ("UndergroundMine20", 6, 12, "Poke_ZygardeCube", "Cubo de Zygarde", "Qualquer", 0, null),
			"Diancie" => ("UndergroundMine20", 6, 12, "Poke_PinkDiamondRelic", "Diamante Cor-de-Rosa", "Qualquer", 0, null),
			"Hoopa" => ("Desert", 34, 28, "Poke_PrisonBottle", "Garrafa da Prisão", "Qualquer", 2200, null),
			"Volcanion" => ("Caldera", 23, 28, "Poke_SteamCore", "Núcleo de Caldeira Térmica", "Chuva", 0, Season.Summer),
			"Type: Null" => ("Mine", 18, 8, "Poke_SyntheticHelmet", "Máscara de Controle de Fera", "Qualquer", 0, null),
			"Silvally" => ("Mine", 18, 8, "Poke_MemoryDrive_Normal", "Disco de Memória RKS", "Qualquer", 0, null),
			"Tapu Koko" => ("Mountain", 26, 11, "Poke_KokoMask", "Máscara Trovejante de Tapu", "Tempestade", 1200, Season.Summer),
			"Tapu Lele" => ("Woods", 25, 15, "Poke_LeleMask", "Máscara Floral de Tapu", "Ensolarado", 1200, Season.Spring),
			"Tapu Bulu" => ("Forest", 64, 40, "Poke_BuluMask", "Máscara Telúrica de Tapu", "Ensolarado", 1200, Season.Fall),
			"Tapu Fini" => ("Beach", 25, 34, "Poke_FiniMask", "Máscara das Névoas de Tapu", "Chuva", 2200, null),
			"Cosmog" => ("Summit", 10, 22, "Poke_NebulaCore", "Núcleo de Poeira Estelar", "Ensolarado", 2200, null),
			"Cosmoem" => ("Summit", 11, 24, "Poke_CosmicCocoon", "Crisálida das Estrelas", "Ensolarado", 2200, null),
			"Solgaleo" => ("Desert", 34, 28, "Poke_SunFlute", "Flauta do Sol", "Ensolarado", 1200, Season.Summer),
			"Lunala" => ("Beach", 25, 34, "Poke_MoonFlute", "Flauta da Lua", "Ensolarado", 2200, null),
			"Nihilego" => ("UndergroundMine60", 6, 12, "Poke_BeastBall_Nihilego", "Ultra Esfera (Nihilego)", "Qualquer", 2200, null),
			"Buzzwole" => ("Forest", 34, 60, "Poke_BeastBall_Buzzwole", "Ultra Esfera (Buzzwole)", "Ensolarado", 1200, Season.Summer),
			"Pheromosa" => ("Desert", 19, 12, "Poke_BeastBall_Pheromosa", "Ultra Esfera (Pheromosa)", "Qualquer", 2200, null),
			"Xurkitree" => ("Railroad", 28, 43, "Poke_BeastBall_Xurkitree", "Ultra Esfera (Xurkitree)", "Tempestade", 2200, null),
			"Celesteela" => ("Summit", 10, 22, "Poke_BeastBall_Celesteela", "Ultra Esfera (Celesteela)", "Qualquer", 2200, null),
			"Kartana" => ("Woods", 25, 15, "Poke_BeastBall_Kartana", "Ultra Esfera (Kartana)", "Ensolarado", 1200, Season.Fall),
			"Guzzlord" => ("Sewer", 17, 27, "Poke_BeastBall_Guzzlord", "Ultra Esfera (Guzzlord)", "Qualquer", 2200, null),
			"Necrozma" => ("SkullCave", 4, 5, "Poke_UltranecroziumZ", "Ultranecrozium Z", "Qualquer", 2200, null),
			"Magearna" => ("Town", 51, 59, "Poke_SoulHeartRelic", "Alma Coração Mecânica", "Qualquer", 1200, null),
			"Marshadow" => ("BugLand", 29, 46, "Poke_MarshadiumZ", "Marshadium Z", "Qualquer", 2200, null),
			"Poipole" => ("BugLand", 29, 46, "Poke_BeastBall_Poipole", "Ultra Esfera (Poipole)", "Qualquer", 0, null),
			"Naganadel" => ("BugLand", 29, 46, "Poke_DragonPoisonBarb", "Ferrão Venenoso de Dragão", "Qualquer", 2200, null),
			"Stakataka" => ("Mountain", 44, 7, "Poke_BeastBall_Stakataka", "Ultra Esfera (Stakataka)", "Qualquer", 0, null),
			"Blacephalon" => ("Railroad", 33, 54, "Poke_BeastBall_Blacephalon", "Ultra Esfera (Blacephalon)", "Ensolarado", 2200, Season.Summer),
			"Zeraora" => ("Mountain", 26, 11, "Poke_ThunderClawRelic", "Garra da Fagulha Plasma", "Tempestade", 1200, Season.Summer),
			"Meltan" => ("Mine", 18, 8, "Poke_MysteryBox_Steel", "Caixa Misteriosa de Aço Líquido", "Qualquer", 0, null),
			"Melmetal" => ("UndergroundMine100", 6, 12, "Poke_GigantamaxNut", "Porca Hexagonal Titânica", "Qualquer", 0, null),
			"Zacian" => ("Forest", 34, 60, "Poke_RustedSword", "Espada Enferrujada", "Ensolarado", 1200, null),
			"Zamazenta" => ("Mountain", 44, 30, "Poke_RustedShield", "Escudo Enferrujado", "Ensolarado", 1200, null),
			"Eternatus" => ("Summit", 10, 22, "Poke_WishingStar", "Estrela dos Desejos Corrompida", "Tempestade", 2200, null),
			"Kubfu" => ("Desert", 19, 12, "Poke_MartialScroll", "Pergaminho das Artes Marciais", "Ensolarado", 1200, null),
			"Urshifu" => ("Caldera", 25, 28, "Poke_ScrollOfDarkness", "Pergaminho das Trevas", "Qualquer", 2200, null),
			"Zarude" => ("Woods", 25, 15, "Poke_JungleVineRelic", "Cipó da Selva Primordial", "Qualquer", 2200, Season.Summer),
			"Regieleki" => ("Railroad", 28, 43, "Poke_ElekiPattern", "Padrão de Raios em Braille", "Tempestade", 0, null),
			"Regidrago" => ("Caldera", 23, 28, "Poke_DragoPattern", "Mandíbula de Dragão em Braille", "Qualquer", 0, null),
			"Glastrier" => ("Summit", 11, 24, "Poke_IcerootCarrot", "Cenoura Gélida da Tundra", "Qualquer", 1200, Season.Winter),
			"Spectrier" => ("Woods", 12, 14, "Poke_ShaderootCarrot", "Cenoura Espectral das Sombras", "Qualquer", 2200, Season.Fall),
			"Calyrex" => ("Forest", 64, 40, "Poke_ReinsOfUnity", "Rédeas da Unidade Sagrada", "Ensolarado", 1200, Season.Spring),
			"Enamorus" => ("Woods", 25, 15, "Poke_RevealGlass_Love", "Espelho do Amor da Primavera", "Ensolarado", 1200, Season.Spring),
			"Wo-Chien" => ("Forest", 34, 60, "Poke_RuinousLeaves", "Tábua de Madeira da Ruína", "Chuva", 2200, Season.Fall),
			"Chien-Pao" => ("Summit", 10, 22, "Poke_RuinousSword", "Espada Fraturada de Gelo", "Qualquer", 2200, Season.Winter),
			"Ting-Lu" => ("Mountain", 44, 7, "Poke_RuinousVessel", "Vaso de Pedra da Ruína", "Qualquer", 0, null),
			"Chi-Yu" => ("Caldera", 24, 28, "Poke_RuinousBeads", "Contas de Fogo da Ruína", "Qualquer", 2200, Season.Summer),
			"Roaring Moon" => ("SkullCave", 7, 6, "Poke_Booster_RoaringMoon", "Cápsula Ancestral (Roaring Moon)", "Qualquer", 2200, null),
			"Iron Valiant" => ("UndergroundMine100", 6, 12, "Poke_Booster_IronValiant", "Cápsula Futurista (Iron Valiant)", "Qualquer", 0, null),
			"Koraidon" => ("Caldera", 23, 28, "Poke_KoraidonFeather", "Pena do Passado Selvagem", "Ensolarado", 1200, Season.Summer),
			"Miraidon" => ("Railroad", 33, 54, "Poke_MiraidonCore", "Núcleo de Propulsão Futura", "Tempestade", 2200, null),
			"Walking Wake" => ("Beach", 28, 14, "Poke_Booster_WalkingWake", "Cápsula Ancestral (Walking Wake)", "Ensolarado", 1200, Season.Summer),
			"Iron Leaves" => ("Woods", 25, 15, "Poke_Booster_IronLeaves", "Cápsula Futurista (Iron Leaves)", "Qualquer", 1200, null),
			"Okidogi" => ("Mountain", 16, 9, "Poke_ToxicChain_Oki", "Corrente Tóxica de Força", "Qualquer", 1200, null),
			"Munkidori" => ("Woods", 12, 14, "Poke_ToxicChain_Munk", "Corrente Tóxica da Astúcia", "Qualquer", 2200, null),
			"Fezandipiti" => ("Forest", 34, 60, "Poke_ToxicChain_Fez", "Corrente Tóxica da Beleza", "Qualquer", 1200, Season.Spring),
			"Ogerpon" => ("Woods", 25, 15, "Poke_TealMask", "Máscara Turquesa de Hera", "Ensolarado", 1200, Season.Spring),
			"Gouging Fire" => ("Caldera", 25, 28, "Poke_Booster_GougingFire", "Cápsula Ancestral (Gouging Fire)", "Ensolarado", 1200, Season.Summer),
			"Raging Bolt" => ("Mountain", 26, 11, "Poke_Booster_RagingBolt", "Cápsula Ancestral (Raging Bolt)", "Tempestade", 1200, null),
			"Iron Boulder" => ("Mountain", 44, 7, "Poke_Booster_IronBoulder", "Cápsula Futurista (Iron Boulder)", "Qualquer", 0, null),
			"Iron Crown" => ("UndergroundMine100", 6, 12, "Poke_Booster_IronCrown", "Cápsula Futurista (Iron Crown)", "Qualquer", 0, null),
			"Terapagos" => ("Caldera", 24, 28, "Poke_IndigoDisk", "Disco Índigo Cristalino", "Qualquer", 0, null),
			"Pecharunt" => ("Town", 51, 59, "Poke_MythicalPechaMochi", "Mochi Venenoso de Pêssego", "Qualquer", 2200, Season.Fall),
			_ => ("Mountain", 26, 11, "Poke_RainbowWing", "Relíquia Lendária", "Qualquer", 0, null)
		};
	}

	#region Tab 10: Audio, Cries & FX Studio

	private void SetupAudioVfxButtons(int startX, int contentY)
	{
		_audioVfxButtons.Clear();

		// Quadrant 1: Cries (Top-Left, startX, contentY)
		int cryBtnY = contentY + 114;
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 12, cryBtnY, 52, 32), "cry_prev10", "« -10"));
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 68, cryBtnY, 52, 32), "cry_prev", "< Ant"));
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 124, cryBtnY, 164, 32), "cry_play", "TOCAR CRY"));
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 292, cryBtnY, 52, 32), "cry_next", "Prox >"));
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 348, cryBtnY, 52, 32), "cry_next10", "+10 »"));

		// Quick Iconic Species Buttons (2 rows of 6 buttons: width 62, gap 4)
		string[] row1Icons = { "Pikachu", "Charizard", "Blastoise", "Venusaur", "Mewtwo", "Mew" };
		for (int i = 0; i < row1Icons.Length; i++)
		{
			_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 12 + i * 65, contentY + 172, 62, 26), "cry_quick_" + row1Icons[i], row1Icons[i]));
		}
		string[] row2Icons = { "Lugia", "Ho-Oh", "Rayquaza", "Kyogre", "Groudon", "Deoxys" };
		for (int i = 0; i < row2Icons.Length; i++)
		{
			_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 12 + i * 65, contentY + 202, 62, 26), "cry_quick_" + row2Icons[i], row2Icons[i]));
		}

		// Quadrant 2: Move FX & Animations (Top-Right, startX + 424, contentY)
		int vfxStartY = contentY + 118;
		(string tag, string label)[] vfxCol1 =
		{
			("vfx_flamethrower", "Flamethrower"),
			("vfx_hydropump", "Hydro Pump"),
			("vfx_thunderbolt", "Thunderbolt"),
			("vfx_blizzard", "Blizzard"),
			("vfx_earthquake", "Earthquake"),
			("vfx_slash", "Slash / Corte"),
			("vfx_dracometeor", "Draco Meteor")
		};
		for (int i = 0; i < vfxCol1.Length; i++)
		{
			_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 434, vfxStartY + i * 18, 192, 17), vfxCol1[i].tag, vfxCol1[i].label));
		}
		(string tag, string label)[] vfxCol2 =
		{
			("vfx_hyperbeam", "Hyper Beam"),
			("vfx_closecombat", "Close Combat"),
			("vfx_shadowball", "Shadow Ball"),
			("vfx_leafblade", "Leaf Blade"),
			("vfx_protect", "Protect / Heal"),
			("vfx_maxmove", "Max Move / Dinamax"),
			("vfx_psychic", "Psychic / Psíquico")
		};
		for (int i = 0; i < vfxCol2.Length; i++)
		{
			_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 634, vfxStartY + i * 18, 192, 17), vfxCol2[i].tag, vfxCol2[i].label));
		}

		// Quadrant 3: Jukebox BGM (Bottom-Left, startX, contentY + 246)
		int bgmStartY = contentY + 280;
		(string tag, string label)[] bgmCol1 =
		{
			("bgm_wild", "Emerald Wild"),
			("bgm_rb_wild", "Red/Blue Wild"),
			("bgm_trainer", "Emerald Treinador"),
			("bgm_rb_trainer", "Red/Blue Treinador"),
			("bgm_route101", "Rota 101 (Hoenn)"),
			("bgm_petalburg", "Petalburg (Vila)"),
			("bgm_dewford", "Dewford (Praia)")
		};
		for (int i = 0; i < bgmCol1.Length; i++)
		{
			_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 14, bgmStartY + i * 22, 190, 20), bgmCol1[i].tag, bgmCol1[i].label));
		}
		(string tag, string label)[] bgmCol2 =
		{
			("bgm_lavender", "Lavender Town"),
			("bgm_route119", "Rota 119 (Chuva)"),
			("bgm_route110", "Rota 110 (Bike)"),
			("bgm_route111", "Rota 111 (Deserto)"),
			("bgm_chimney", "Mt. Chimney (Vulcão)"),
			("bgm_pacifidlog", "Pacifidlog (Ilha)"),
			("bgm_victory", "Fanfarra Vitória")
		};
		for (int i = 0; i < bgmCol2.Length; i++)
		{
			_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 212, bgmStartY + i * 22, 190, 20), bgmCol2[i].tag, bgmCol2[i].label));
		}
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 14, contentY + 440, 388, 28), "bgm_stop", "PARAR MUSICA / RESTAURAR VANILLA"));

		// Quadrant 4: Cutscenes & Vinhetas (Bottom-Right, startX + 424, contentY + 246)
		string cutSpecies = _cutsceneLegendaryList[_cutsceneSpeciesIdx % _cutsceneLegendaryList.Length];
		int cutStartY = contentY + 288;
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 434, cutStartY, 392, 30), "cut_cycle_species", $"⚡ Lendário Cutscene: {cutSpecies} (Trocar)"));
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 434, cutStartY + 33, 392, 32), "cut_overworld_awakening", $"🎬 EXECUTAR DESPERTAR NO MAPA ({cutSpecies})"));
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 434, cutStartY + 68, 392, 28), "cut_legendary", $"1. Prévia Menu: Despertar de {cutSpecies}"));
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 434, cutStartY + 99, 392, 26), "cut_evolution", "2. Prévia Menu: Sequência de Evolução"));
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 434, cutStartY + 128, 392, 26), "cut_capture", "3. Prévia Menu: Captura Master Ball"));
		_audioVfxButtons.Add(new ClickableComponent(new Rectangle(startX + 434, cutStartY + 157, 392, 26), "cut_status", "4. Prévia Menu: Condições de Status"));
	}

	private void HandleAudioVfxClick(int x, int y)
	{
		var all = PokemonSpecies.AllSpecies();
		int count = (all != null && all.Count > 0) ? all.Count : 1025;

		foreach (var btn in _audioVfxButtons)
		{
			if (!btn.containsPoint(x, y)) continue;

			if (btn.name == "cry_prev10")
			{
				_crySpeciesIndex = (_crySpeciesIndex - 10 + count) % count;
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}
			if (btn.name == "cry_prev")
			{
				_crySpeciesIndex = (_crySpeciesIndex - 1 + count) % count;
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}
			if (btn.name == "cry_next")
			{
				_crySpeciesIndex = (_crySpeciesIndex + 1) % count;
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}
			if (btn.name == "cry_next10")
			{
				_crySpeciesIndex = (_crySpeciesIndex + 10) % count;
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}
			if (btn.name == "cry_play")
			{
				if (all != null && _crySpeciesIndex >= 0 && _crySpeciesIndex < all.Count)
				{
					PokemonCryManager.PlayCry(all[_crySpeciesIndex].Name);
				}
				return;
			}
			if (btn.name.StartsWith("cry_quick_"))
			{
				string targetName = btn.name.Substring("cry_quick_".Length);
				int idx = all.FindIndex(s => string.Equals(s.Name, targetName, StringComparison.OrdinalIgnoreCase));
				if (idx >= 0)
				{
					_crySpeciesIndex = idx;
					PokemonCryManager.PlayCry(all[idx].Name);
					SetupButtons();
				}
				return;
			}

			// VFX Moves
			if (btn.name == "vfx_flamethrower") { _activeVfxAnim = "Flamethrower"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("fireball"); return; }
			if (btn.name == "vfx_hydropump") { _activeVfxAnim = "Hydro Pump"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("drop"); return; }
			if (btn.name == "vfx_thunderbolt") { _activeVfxAnim = "Thunderbolt"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("thunder"); Rumble.rumble(0.5f, 200f); return; }
			if (btn.name == "vfx_blizzard") { _activeVfxAnim = "Blizzard"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("freeze"); return; }
			if (btn.name == "vfx_earthquake") { _activeVfxAnim = "Earthquake"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("rumble"); Rumble.rumble(0.8f, 500f); return; }
			if (btn.name == "vfx_hyperbeam") { _activeVfxAnim = "Hyper Beam"; _vfxAnimTimer = 1.0f; Game1.playSound("explosion"); Rumble.rumble(0.9f, 600f); return; }
			if (btn.name == "vfx_closecombat") { _activeVfxAnim = "Close Combat"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("clubhit"); Rumble.rumble(0.6f, 300f); return; }
			if (btn.name == "vfx_shadowball") { _activeVfxAnim = "Shadow Ball"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("wand"); return; }
			if (btn.name == "vfx_leafblade") { _activeVfxAnim = "Leaf Blade"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("swordswipe"); return; }
			if (btn.name == "vfx_protect") { _activeVfxAnim = "Protect"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("reward"); return; }
			if (btn.name == "vfx_slash") { _activeVfxAnim = "Slash"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("swordswipe"); Rumble.rumble(0.5f, 180f); return; }
			if (btn.name == "vfx_dracometeor") { _activeVfxAnim = "Draco Meteor"; _vfxAnimTimer = 1.15f; Game1.playSound("explosion"); Game1.playSound("fireball"); Rumble.rumble(0.9f, 600f); return; }
			if (btn.name == "vfx_maxmove") { _activeVfxAnim = "Max Move"; _vfxAnimTimer = 1.25f; Game1.playSound("thunder"); Game1.playSound("explosion"); Rumble.rumble(1.0f, 750f); return; }
			if (btn.name == "vfx_psychic") { _activeVfxAnim = "Psychic"; _vfxAnimTimer = _vfxAnimDuration; Game1.playSound("wand"); Game1.playSound("secret1"); Rumble.rumble(0.6f, 250f); return; }

			// BGM
			if (btn.name == "bgm_wild") { PokemonMusicManager.PlayMidiFile("emerald_wild.mid", loop: true); return; }
			if (btn.name == "bgm_rb_wild") { PokemonMusicManager.PlayMidiFile("rb_wild.mid", loop: true); return; }
			if (btn.name == "bgm_trainer") { PokemonMusicManager.PlayMidiFile("emerald_trainer.mid", loop: true); return; }
			if (btn.name == "bgm_rb_trainer") { PokemonMusicManager.PlayMidiFile("rb_trainer.mid", loop: true); return; }
			if (btn.name == "bgm_lavender") { PokemonMusicManager.PlayMidiFile("lavender_town.mid", loop: true); return; }
			if (btn.name == "bgm_gym") { PokemonMusicManager.PlayMidiFile("gym_leader.mid", loop: true); return; }
			if (btn.name == "bgm_champion") { PokemonMusicManager.PlayMidiFile("champion.mid", loop: true); return; }
			if (btn.name == "bgm_rayquaza") { PokemonMusicManager.PlayMidiFile("rayquaza.mid", loop: true); return; }
			if (btn.name == "bgm_kyogre_groudon") { PokemonMusicManager.PlayMidiFile("primal.mid", loop: true); return; }
			if (btn.name == "bgm_mewtwo") { PokemonMusicManager.PlayMidiFile("mewtwo.mid", loop: true); return; }
			if (btn.name == "bgm_route101") { PokemonMusicManager.PlayMidiFile("route101.mid", loop: true); return; }
			if (btn.name == "bgm_petalburg") { PokemonMusicManager.PlayMidiFile("petalburg.mid", loop: true); return; }
			if (btn.name == "bgm_dewford") { PokemonMusicManager.PlayMidiFile("dewford.mid", loop: true); return; }
			if (btn.name == "bgm_route119") { PokemonMusicManager.PlayMidiFile("route119.mid", loop: true); return; }
			if (btn.name == "bgm_route110") { PokemonMusicManager.PlayMidiFile("route110.mid", loop: true); return; }
			if (btn.name == "bgm_route111") { PokemonMusicManager.PlayMidiFile("route111.mid", loop: true); return; }
			if (btn.name == "bgm_chimney") { PokemonMusicManager.PlayMidiFile("mt_chimney.mid", loop: true); return; }
			if (btn.name == "bgm_pacifidlog") { PokemonMusicManager.PlayMidiFile("pacifidlog.mid", loop: true); return; }
			if (btn.name == "bgm_victory") { PokemonMusicManager.PlayMidiFile("victory_fanfare.mid", loop: false); return; }
			if (btn.name == "bgm_stop") { PokemonMusicManager.StopAndRestoreStardewMusic(); Game1.playSound("bigDeSelect"); return; }

			// Cutscenes
			if (btn.name == "cut_cycle_species")
			{
				_cutsceneSpeciesIdx = (_cutsceneSpeciesIdx + 1) % _cutsceneLegendaryList.Length;
				Game1.playSound("shwip");
				SetupButtons();
				return;
			}
			if (btn.name == "cut_overworld_awakening")
			{
				string selSpecies = _cutsceneLegendaryList[_cutsceneSpeciesIdx % _cutsceneLegendaryList.Length];
				var spec = PokemonSpecies.Get(selSpecies) ?? PokemonSpecies.Get("Rayquaza");
				Game1.exitActiveMenu();
				PokemonCutsceneManager.PlayLegendaryAwakening(Game1.currentLocation, Game1.player.TilePoint, spec, "Artefato Ancestral", () =>
				{
					Game1.addHUDMessage(new HUDMessage($"✨ Despertar lendário de {spec.Name} finalizado!", 1));
				});
				return;
			}
			if (btn.name == "cut_legendary")
			{
				string selSpecies = _cutsceneLegendaryList[_cutsceneSpeciesIdx % _cutsceneLegendaryList.Length];
				_activeCutscene = "Legendary";
				_cutsceneTimer = 2.5f;
				Game1.playSound("thunder");
				Game1.playSound("roar");
				Rumble.rumble(0.95f, 1200f);
				PokemonCryManager.PlayCry(selSpecies, volumeMult: 1.0f);
				return;
			}
			if (btn.name == "cut_evolution")
			{
				_activeCutscene = "Evolution";
				_cutsceneTimer = 2.2f;
				Game1.playSound("wand");
				Game1.playSound("reward");
				PokemonCryManager.PlayCry("Charizard", volumeMult: 1.0f);
				return;
			}
			if (btn.name == "cut_capture")
			{
				_activeCutscene = "Capture";
				_cutsceneTimer = 1.8f;
				Game1.playSound("toolSwap");
				Game1.playSound("reward");
				return;
			}
			if (btn.name == "cut_status")
			{
				_activeCutscene = "Status";
				_cutsceneTimer = 1.8f;
				Game1.playSound("debuffHit");
				Game1.playSound("paralyze");
				return;
			}
		}
	}

	private void DrawAudioAndVfxTab(SpriteBatch b, int startX, int contentY, int mouseX, int mouseY)
	{
		var all = PokemonSpecies.AllSpecies();

		// Quadrant 1: Cries (Top-Left, startX, contentY, W = 412, H = 236)
		Rectangle q1Box = new Rectangle(startX, contentY, 412, 236);
		IClickableMenu.drawTextureBox(b, q1Box.X, q1Box.Y, q1Box.Width, q1Box.Height, Color.White);
		b.Draw(Game1.staminaRect, new Rectangle(startX + 8, contentY + 8, 396, 24), new Color(45, 60, 95));
		PokemonUI.Text(b, Game1.smallFont, "TESTADOR DE CRIES (GEN 1 - 9)", new Rectangle(startX + 12, contentY + 10, 388, 20), Color.White, center: true, maxScale: 0.72f, shadow: false);

		if (all != null && all.Count > 0)
		{
			_crySpeciesIndex = Math.Clamp(_crySpeciesIndex, 0, all.Count - 1);
			var sp = all[_crySpeciesIndex];

			Rectangle cardInfo = new Rectangle(startX + 12, contentY + 36, 388, 72);
			b.Draw(Game1.staminaRect, cardInfo, new Color(242, 246, 252));
			PokemonUI.DrawCardBorder(b, cardInfo, new Color(185, 200, 225), 1);

			var inst = GetPreviewInstance(sp, false);
			Texture2D icon = inst?.GetIconSprite(Game1.graphics.GraphicsDevice) ?? inst?.GetFrontSprite(Game1.graphics.GraphicsDevice);
			if (icon != null)
			{
				bool isFront = icon == inst.GetFrontSprite(Game1.graphics.GraphicsDevice);
				Rectangle src = isFront ? inst.GetFrontSourceRect() : inst.GetIconSourceRect();
				float sc = isFront ? (48f / Math.Max(1, src.Width)) : 1.25f;
				b.Draw(icon, new Vector2(startX + 18, contentY + 44), src, Color.White, 0f, Vector2.Zero, sc, SpriteEffects.None, 0.86f);
			}

			PokemonUI.Text(b, Game1.smallFont, $"#{sp.PokedexNumber:D4} {sp.Name}", new Rectangle(startX + 78, contentY + 40, 310, 20), Color.DarkSlateBlue, center: false, maxScale: 0.76f, shadow: false);
			string tStr = $"Tipo: {sp.PrimaryType}" + (sp.SecondaryType.HasValue ? $" / {sp.SecondaryType.Value}" : "");
			PokemonUI.Text(b, Game1.tinyFont, tStr, new Rectangle(startX + 78, contentY + 62, 310, 16), Color.DimGray, center: false, maxScale: 0.72f, shadow: false);
			string cryStatus = PokemonCryManager.GetCryStatus(sp.Name);
			Color stCol = PokemonCryManager.HasNativeCry(sp.Name) ? new Color(25, 130, 45) : new Color(175, 105, 20);
			PokemonUI.Text(b, Game1.tinyFont, $"Audio: {cryStatus}", new Rectangle(startX + 78, contentY + 80, 310, 16), stCol, center: false, maxScale: 0.72f, shadow: false);
		}

		PokemonUI.Text(b, Game1.tinyFont, "Atalhos Rapidos de Especies Famosas:", new Rectangle(startX + 14, contentY + 152, 384, 18), Color.DimGray, center: false, maxScale: 0.72f, shadow: false);

		// Quadrant 2: Move FX (Top-Right, startX + 424, contentY, W = 412, H = 236)
		Rectangle q2Box = new Rectangle(startX + 424, contentY, 412, 236);
		IClickableMenu.drawTextureBox(b, q2Box.X, q2Box.Y, q2Box.Width, q2Box.Height, Color.White);
		b.Draw(Game1.staminaRect, new Rectangle(startX + 432, contentY + 8, 396, 24), new Color(135, 35, 35));
		PokemonUI.Text(b, Game1.smallFont, "ANIMACOES DE GOLPES (VFX & SFX)", new Rectangle(startX + 436, contentY + 10, 388, 20), Color.White, center: true, maxScale: 0.72f, shadow: false);

		// Mini Battle Arena Stage Box
		Rectangle stageBox = new Rectangle(startX + 434, contentY + 36, 392, 78);
		b.Draw(Game1.staminaRect, stageBox, new Color(18, 15, 26));
		PokemonUI.DrawCardBorder(b, stageBox, new Color(75, 60, 95), 1);

		// Attacker Platform & Silhouette
		Rectangle platL = new Rectangle(stageBox.X + 38, stageBox.Y + 54, 58, 14);
		b.Draw(Game1.staminaRect, platL, new Color(45, 65, 85));
		b.Draw(Game1.staminaRect, new Rectangle(platL.Center.X - 10, platL.Y - 18, 20, 20), Color.MediumSeaGreen * 0.85f);

		// Defender Platform & Silhouette
		Rectangle platR = new Rectangle(stageBox.Right - 96, stageBox.Y + 54, 58, 14);
		b.Draw(Game1.staminaRect, platR, new Color(85, 45, 45));
		b.Draw(Game1.staminaRect, new Rectangle(platR.Center.X - 10, platR.Y - 18, 20, 20), Color.IndianRed * 0.85f);

		// Dynamic Attack VFX Rendering
		if (_activeVfxAnim != null)
		{
			float t = Math.Clamp(1.0f - (_vfxAnimTimer / _vfxAnimDuration), 0f, 1f);
			Vector2 aPos = new Vector2(platL.Center.X, platL.Y - 10);
			Vector2 dPos = new Vector2(platR.Center.X, platR.Y - 10);

			if (_activeVfxAnim == "Flamethrower")
			{
				for (int i = 0; i < 5; i++)
				{
					float fT = Math.Clamp(t * 1.3f - i * 0.08f, 0f, 1f);
					Vector2 fPos = Vector2.Lerp(aPos, dPos, fT);
					float fSize = 12f + (float)Math.Sin(fT * Math.PI) * 14f;
					b.Draw(Game1.staminaRect, new Rectangle((int)fPos.X - (int)fSize / 2, (int)fPos.Y - (int)fSize / 2, (int)fSize, (int)fSize), Color.OrangeRed * 0.9f);
					b.Draw(Game1.staminaRect, new Rectangle((int)fPos.X - (int)fSize / 4, (int)fPos.Y - (int)fSize / 4, (int)fSize / 2, (int)fSize / 2), Color.Yellow);
				}
			}
			else if (_activeVfxAnim == "Hydro Pump")
			{
				for (int i = 0; i < 4; i++)
				{
					float wT = Math.Clamp(t * 1.3f - i * 0.08f, 0f, 1f);
					Vector2 wPos = Vector2.Lerp(aPos, dPos, wT);
					float waveOffset = (float)Math.Sin(wT * Math.PI * 4.0 + i) * 12f;
					b.Draw(Game1.staminaRect, new Rectangle((int)wPos.X - 6, (int)(wPos.Y + waveOffset) - 6, 14, 14), Color.DeepSkyBlue * 0.9f);
					b.Draw(Game1.staminaRect, new Rectangle((int)wPos.X - 3, (int)(wPos.Y + waveOffset) - 3, 6, 6), Color.White);
				}
			}
			else if (_activeVfxAnim == "Thunderbolt")
			{
				int segs = 6;
				Vector2 prev = aPos;
				for (int s = 1; s <= segs; s++)
				{
					float segT = s / (float)segs;
					Vector2 cur = Vector2.Lerp(aPos, dPos, segT);
					if (s < segs) cur.Y += ((s % 2 == 0) ? -10f : 10f);
					b.Draw(Game1.staminaRect, new Rectangle((int)cur.X - 4, (int)cur.Y - 4, 8, 8), Color.Gold);
					prev = cur;
				}
				for (int p = 0; p < 4; p++)
				{
					b.Draw(Game1.staminaRect, new Rectangle((int)dPos.X - 16 + p * 8, (int)dPos.Y - 14 + (p % 2) * 8, 6, 6), Color.Yellow);
				}
			}
			else if (_activeVfxAnim == "Blizzard")
			{
				for (int i = 0; i < 6; i++)
				{
					float iT = Math.Clamp(t * 1.2f - i * 0.06f, 0f, 1f);
					Vector2 iPos = Vector2.Lerp(aPos, dPos, iT) + new Vector2(0, (float)Math.Sin(iT * 8f + i) * 10f);
					b.Draw(Game1.staminaRect, new Rectangle((int)iPos.X - 5, (int)iPos.Y - 5, 10, 10), Color.LightCyan * 0.85f);
				}
			}
			else if (_activeVfxAnim == "Earthquake")
			{
				float eqY = (float)Math.Sin(t * 25f) * 4f;
				for (int i = -3; i <= 3; i++)
				{
					int spkH = (int)(Math.Sin(t * Math.PI) * (26 - Math.Abs(i) * 5));
					b.Draw(Game1.staminaRect, new Rectangle((int)dPos.X + i * 14 - 4, (int)(dPos.Y + eqY) - spkH, 10, spkH), new Color(150, 100, 50));
				}
			}
			else if (_activeVfxAnim == "Hyper Beam")
			{
				float beamW = (float)Math.Sin(t * Math.PI) * 28f;
				b.Draw(Game1.staminaRect, new Rectangle((int)aPos.X, (int)aPos.Y - (int)beamW / 2, (int)(dPos.X - aPos.X), (int)beamW), Color.Crimson * 0.85f);
				b.Draw(Game1.staminaRect, new Rectangle((int)aPos.X, (int)aPos.Y - (int)beamW / 4, (int)(dPos.X - aPos.X), (int)beamW / 2), Color.White);
			}
			else if (_activeVfxAnim == "Close Combat")
			{
				float len = MathHelper.Lerp(4f, 32f, t);
				b.Draw(Game1.staminaRect, new Rectangle((int)(dPos.X - len), (int)(dPos.Y - len), (int)(len * 2), 4), Color.Cyan);
				b.Draw(Game1.staminaRect, new Rectangle((int)(dPos.X - len), (int)(dPos.Y + len), (int)(len * 2), 4), Color.Gold);
			}
			else if (_activeVfxAnim == "Shadow Ball")
			{
				Vector2 sbPos = Vector2.Lerp(aPos, dPos, t);
				float sbSize = 18f + (float)Math.Sin(t * 12f) * 6f;
				b.Draw(Game1.staminaRect, new Rectangle((int)sbPos.X - (int)sbSize / 2, (int)sbPos.Y - (int)sbSize / 2, (int)sbSize, (int)sbSize), new Color(85, 35, 130));
				b.Draw(Game1.staminaRect, new Rectangle((int)sbPos.X - (int)sbSize / 4, (int)sbPos.Y - (int)sbSize / 4, (int)sbSize / 2, (int)sbSize / 2), Color.MediumPurple);
			}
			else if (_activeVfxAnim == "Leaf Blade")
			{
				float lT = Math.Clamp(t * 1.3f, 0f, 1f);
				Vector2 lPos = Vector2.Lerp(aPos, dPos, lT) + new Vector2(0, (float)Math.Sin(lT * Math.PI) * -16f);
				b.Draw(Game1.staminaRect, new Rectangle((int)lPos.X - 8, (int)lPos.Y - 4, 16, 8), Color.LimeGreen);
			}
			else if (_activeVfxAnim == "Protect")
			{
				float rad = 18f + (float)Math.Sin(t * Math.PI) * 10f;
				b.Draw(Game1.staminaRect, new Rectangle((int)aPos.X - (int)rad, (int)aPos.Y - (int)rad, (int)(rad * 2), (int)(rad * 2)), Color.LightSkyBlue * 0.45f);
				b.Draw(Game1.staminaRect, new Rectangle((int)aPos.X - 4, (int)aPos.Y - (int)rad - 4, 8, 8), Color.Gold);
			}
			else if (_activeVfxAnim == "Slash")
			{
				float sT = Math.Clamp(t * 1.4f, 0f, 1f);
				int sLen = (int)(sT * 36f);
				Vector2 c = dPos;
				b.Draw(Game1.staminaRect, new Rectangle((int)c.X - sLen, (int)c.Y - sLen, sLen * 2, 4), Color.White * 0.95f);
				b.Draw(Game1.staminaRect, new Rectangle((int)c.X - sLen + 8, (int)c.Y - sLen + 8, sLen * 2, 2), Color.LightCyan);
				b.Draw(Game1.staminaRect, new Rectangle((int)c.X - sLen, (int)c.Y + sLen, sLen * 2, 4), Color.Silver * 0.85f);
			}
			else if (_activeVfxAnim == "Draco Meteor")
			{
				for (int m = 0; m < 4; m++)
				{
					float mT = Math.Clamp(t * 1.3f - m * 0.12f, 0f, 1f);
					Vector2 startSky = new Vector2(dPos.X - 50 + m * 28, stageBox.Y + 4);
					Vector2 target = dPos + new Vector2(-20 + m * 14, 0);
					Vector2 cur = Vector2.Lerp(startSky, target, mT);
					float mSize = 10f + (float)Math.Sin(mT * Math.PI) * 8f;
					b.Draw(Game1.staminaRect, new Rectangle((int)cur.X - (int)mSize / 2, (int)cur.Y - (int)mSize / 2, (int)mSize, (int)mSize), Color.OrangeRed);
					b.Draw(Game1.staminaRect, new Rectangle((int)cur.X - (int)mSize / 4, (int)cur.Y - (int)mSize / 4, (int)mSize / 2, (int)mSize / 2), Color.Gold);
				}
			}
			else if (_activeVfxAnim == "Max Move")
			{
				float pAlpha = (float)Math.Sin(t * Math.PI);
				int colW = (int)(28f + pAlpha * 24f);
				Rectangle pillar = new Rectangle((int)dPos.X - colW / 2, stageBox.Y + 2, colW, stageBox.Height - 8);
				b.Draw(Game1.staminaRect, pillar, Color.DeepPink * (0.4f + pAlpha * 0.5f));
				b.Draw(Game1.staminaRect, new Rectangle((int)dPos.X - colW / 4, stageBox.Y + 2, colW / 2, stageBox.Height - 8), Color.White * (0.6f + pAlpha * 0.4f));
				for (int ring = 0; ring < 3; ring++)
				{
					float rRad = 12f + ring * 14f * pAlpha;
					b.Draw(Game1.staminaRect, new Rectangle((int)dPos.X - (int)rRad, (int)dPos.Y - 4, (int)rRad * 2, 8), Color.Crimson * 0.7f);
				}
			}
			else if (_activeVfxAnim == "Psychic")
			{
				float wave = (float)Math.Sin(t * 18f);
				for (int w = 0; w < 4; w++)
				{
					float wRad = 10f + w * 12f + wave * 4f;
					b.Draw(Game1.staminaRect, new Rectangle((int)dPos.X - (int)wRad, (int)dPos.Y - (int)wRad, (int)wRad * 2, (int)wRad * 2), new Color(180, 70, 220) * (0.35f - w * 0.06f));
					b.Draw(Game1.staminaRect, new Rectangle((int)dPos.X - 3, (int)dPos.Y - (int)wRad, 6, 6), Color.HotPink);
				}
			}
		}
		else
		{
			PokemonUI.Text(b, Game1.tinyFont, "[Selecione um golpe abaixo para testar o efeito visual e som]", new Rectangle(stageBox.X + 8, stageBox.Y + 28, stageBox.Width - 16, 20), Color.Gray * 0.75f, center: true, maxScale: 0.72f, shadow: false);
		}

		// Quadrant 3: Jukebox BGM (Bottom-Left, startX, contentY + 246, W = 412, H = 236)
		Rectangle q3Box = new Rectangle(startX, contentY + 246, 412, 236);
		IClickableMenu.drawTextureBox(b, q3Box.X, q3Box.Y, q3Box.Width, q3Box.Height, Color.White);
		b.Draw(Game1.staminaRect, new Rectangle(startX + 8, contentY + 254, 396, 24), new Color(30, 45, 95));
		PokemonUI.Text(b, Game1.smallFont, "JUKEBOX GBA / FRLG (531+ MIDIS NATIVOS)", new Rectangle(startX + 12, contentY + 256, 388, 20), Color.White, center: true, maxScale: 0.72f, shadow: false);

		// Quadrant 4: Cutscenes (Bottom-Right, startX + 424, contentY + 246, W = 412, H = 236)
		Rectangle q4Box = new Rectangle(startX + 424, contentY + 246, 412, 236);
		IClickableMenu.drawTextureBox(b, q4Box.X, q4Box.Y, q4Box.Width, q4Box.Height, Color.White);
		b.Draw(Game1.staminaRect, new Rectangle(startX + 432, contentY + 254, 396, 24), new Color(130, 95, 25));
		PokemonUI.Text(b, Game1.smallFont, "VINHETAS & CUTSCENES ESPECIAIS", new Rectangle(startX + 436, contentY + 256, 388, 20), Color.White, center: true, maxScale: 0.72f, shadow: false);
		PokemonUI.Text(b, Game1.tinyFont, "Simule as introducoes cinematograficas lendarias e fanfarras:", new Rectangle(startX + 434, contentY + 282, 392, 16), Color.DimGray, center: false, maxScale: 0.72f, shadow: false);

		// Draw all buttons in _audioVfxButtons
		foreach (var btn in _audioVfxButtons)
		{
			bool hov = btn.containsPoint(mouseX, mouseY);
			Color bCol = Color.White;
			Color txtCol = Color.Black;

			if (btn.name == "cry_play")
			{
				bCol = hov ? Color.Gold : Color.PaleGoldenrod;
				txtCol = Color.DarkSlateBlue;
			}
			else if (btn.name.StartsWith("cry_quick_"))
			{
				bCol = hov ? Color.LightSkyBlue : Color.AliceBlue;
				txtCol = Color.DarkSlateGray;
			}
			else if (btn.name.StartsWith("vfx_"))
			{
				bCol = hov ? Color.MistyRose : Color.SeaShell;
				txtCol = Color.DarkRed;
			}
			else if (btn.name.StartsWith("bgm_"))
			{
				if (btn.name == "bgm_stop")
				{
					bCol = hov ? new Color(255, 180, 180) : new Color(255, 220, 220);
					txtCol = Color.DarkRed;
				}
				else
				{
					bCol = hov ? Color.Honeydew : Color.Ivory;
					txtCol = Color.DarkGreen;
				}
			}
			else if (btn.name.StartsWith("cut_"))
			{
				bCol = hov ? Color.PeachPuff : Color.FloralWhite;
				txtCol = Color.SaddleBrown;
			}
			else if (btn.name.StartsWith("cry_"))
			{
				bCol = hov ? Color.LightYellow : Color.White;
			}

			IClickableMenu.drawTextureBox(b, btn.bounds.X, btn.bounds.Y, btn.bounds.Width, btn.bounds.Height, bCol);
			PokemonUI.Text(b, Game1.smallFont, btn.label, new Rectangle(btn.bounds.X + 2, btn.bounds.Y + 2, btn.bounds.Width - 4, btn.bounds.Height - 4), txtCol, center: true, maxScale: 0.70f, shadow: false);
		}
	}

	private void DrawCutsceneOverlay(SpriteBatch b)
	{
		int sw = Game1.uiViewport.Width;
		int sh = Game1.uiViewport.Height;

		if (_activeCutscene == "Legendary")
		{
			float alpha = Math.Clamp(_cutsceneTimer / 2.5f, 0f, 1f);
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, sw, sh), Color.Black * 0.78f * alpha);

			if (_cutsceneTimer > 1.9f && _cutsceneTimer < 2.1f)
			{
				b.Draw(Game1.staminaRect, new Rectangle(0, 0, sw, sh), Color.White * 0.90f);
			}

			Rectangle banner = new Rectangle(sw / 2 - 320, sh / 2 - 60, 640, 120);
			IClickableMenu.drawTextureBox(b, banner.X, banner.Y, banner.Width, banner.Height, new Color(35, 25, 20));
			PokemonUI.DrawCardBorder(b, banner, Color.Gold, 3);
			string cutSpecName = _cutsceneLegendaryList[_cutsceneSpeciesIdx % _cutsceneLegendaryList.Length];
			PokemonUI.Text(b, Game1.dialogueFont, $"O GUARDIÃO {cutSpecName.ToUpperInvariant()} DESPERTOU!", new Rectangle(banner.X, banner.Y + 18, banner.Width, 36), Color.Gold, center: true, maxScale: 0.85f);
			PokemonUI.Text(b, Game1.smallFont, $"O céu ruge e relâmpagos cortam o horizonte sobre o PokeValley!", new Rectangle(banner.X, banner.Y + 65, banner.Width, 28), Color.White, center: true, maxScale: 0.82f, shadow: false);
		}
		else if (_activeCutscene == "Evolution")
		{
			float alpha = Math.Clamp(_cutsceneTimer / 2.2f, 0f, 1f);
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, sw, sh), Color.Indigo * 0.65f * alpha);

			Rectangle banner = new Rectangle(sw / 2 - 300, sh / 2 - 50, 600, 100);
			IClickableMenu.drawTextureBox(b, banner.X, banner.Y, banner.Width, banner.Height, new Color(25, 20, 45));
			PokemonUI.DrawCardBorder(b, banner, Color.Cyan, 3);
			PokemonUI.Text(b, Game1.dialogueFont, "O POKEMON ESTA EVOLUINDO!", new Rectangle(banner.X, banner.Y + 16, banner.Width, 34), Color.Yellow, center: true, maxScale: 0.85f);
			PokemonUI.Text(b, Game1.smallFont, "Uma luz radiante transcende os limites e revela seu potencial definitivo!", new Rectangle(banner.X, banner.Y + 56, banner.Width, 26), Color.White, center: true, maxScale: 0.80f, shadow: false);
		}
		else if (_activeCutscene == "Capture")
		{
			float alpha = Math.Clamp(_cutsceneTimer / 1.8f, 0f, 1f);
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, sw, sh), Color.Black * 0.50f * alpha);

			Rectangle banner = new Rectangle(sw / 2 - 280, sh / 2 - 45, 560, 90);
			IClickableMenu.drawTextureBox(b, banner.X, banner.Y, banner.Width, banner.Height, new Color(20, 35, 50));
			PokemonUI.DrawCardBorder(b, banner, Color.DeepSkyBlue, 3);
			PokemonUI.Text(b, Game1.dialogueFont, "GOTCHA! CAPTURA CONCLUIDA!", new Rectangle(banner.X, banner.Y + 14, banner.Width, 32), Color.LightSkyBlue, center: true, maxScale: 0.85f);
			PokemonUI.Text(b, Game1.smallFont, "Todos os dados da criatura foram salvos com maestria na sua Pokedex!", new Rectangle(banner.X, banner.Y + 52, banner.Width, 24), Color.White, center: true, maxScale: 0.78f, shadow: false);
		}
		else if (_activeCutscene == "Status")
		{
			float alpha = Math.Clamp(_cutsceneTimer / 1.8f, 0f, 1f);
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, sw, sh), Color.Black * 0.40f * alpha);

			Rectangle banner = new Rectangle(sw / 2 - 280, sh / 2 - 45, 560, 90);
			IClickableMenu.drawTextureBox(b, banner.X, banner.Y, banner.Width, banner.Height, new Color(40, 20, 50));
			PokemonUI.DrawCardBorder(b, banner, Color.MediumPurple, 3);
			PokemonUI.Text(b, Game1.dialogueFont, "AFLICAO DE STATUS DE COMBATE", new Rectangle(banner.X, banner.Y + 14, banner.Width, 32), Color.Thistle, center: true, maxScale: 0.85f);
			PokemonUI.Text(b, Game1.smallFont, "Particulas de Sono, Paralisia, Veneno e Queimadura ativas!", new Rectangle(banner.X, banner.Y + 52, banner.Width, 24), Color.White, center: true, maxScale: 0.78f, shadow: false);
		}
	}


	private void SetupMilestonesButtons(int startX, int contentY)
	{
		_milestonesButtons.Clear();

		// Top Actions Bar (contentY = y + 94)
		int btnY = contentY + 18;
		_milestonesButtons.Add(new ClickableComponent(new Rectangle(startX + 340, btnY, 115, 28), "ms_fill_dex", "✨ Encher 100%"));
		_milestonesButtons.Add(new ClickableComponent(new Rectangle(startX + 462, btnY, 105, 28), "ms_clear_dex", "🧹 Zerar Dex"));
		_milestonesButtons.Add(new ClickableComponent(new Rectangle(startX + 574, btnY, 125, 28), "ms_reset_milestones", "🔄 Zerar Marcos"));
		_milestonesButtons.Add(new ClickableComponent(new Rectangle(startX + 706, btnY, 122, 28), "ms_claim_all", "🎁 Pegar Todos"));

		// 6 Milestones: 2 columns of 3 rows
		int colW = 412;
		int cardH = 112;
		int rowGap = 10;
		int gridStartY = contentY + 68;

		int[] milestones = new[] { 10, 25, 50, 75, 90, 100 };
		for (int i = 0; i < milestones.Length; i++)
		{
			int m = milestones[i];
			int col = i / 3;
			int row = i % 3;
			int cX = startX + col * 428;
			int cY = gridStartY + row * (cardH + rowGap);

			int btnW = 145;
			int btnH = 28;
			int bX = cX + colW - btnW - 12;
			int bY = cY + cardH - btnH - 10;
			_milestonesButtons.Add(new ClickableComponent(new Rectangle(bX, bY, btnW, btnH), $"claim_milestone_{m}", $"🎁 Reivindicar {m}%"));
		}
	}

	private void HandleMilestonesClick(int x, int y)
	{
		foreach (var btn in _milestonesButtons)
		{
			if (btn.containsPoint(x, y))
			{
				if (btn.name == "ms_fill_dex")
				{
					PokemonPokedex.Instance.FillAllForDebug();
					Game1.playSound("reward");
					Game1.addHUDMessage(new HUDMessage("✨ Pokédex 100% preenchida com sucesso para testes!", 2));
					SetupButtons();
					return;
				}
				if (btn.name == "ms_clear_dex")
				{
					PokemonPokedex.Instance.ResetPokedex();
					Game1.playSound("trashcan");
					Game1.addHUDMessage(new HUDMessage("🧹 Pokédex resetada para 0 registros!", 2));
					SetupButtons();
					return;
				}
				if (btn.name == "ms_reset_milestones")
				{
					PokemonPokedex.Instance.ResetClaimedMilestones();
					Game1.playSound("trashcan");
					Game1.addHUDMessage(new HUDMessage("🔄 Todos os marcos de recompensa foram zerados!", 2));
					SetupButtons();
					return;
				}
				if (btn.name == "ms_claim_all")
				{
					int claimed = 0;
					int[] mArr = new[] { 10, 25, 50, 75, 90, 100 };
					foreach (int m in mArr)
					{
						if (PokemonPokedex.Instance.CanClaimMilestone(m))
						{
							if (PokemonPokedex.Instance.ClaimMilestone(m, forceDebug: false))
								claimed++;
						}
					}
					if (claimed > 0)
					{
						Game1.playSound("reward");
						Game1.addHUDMessage(new HUDMessage($"🎁 {claimed} marcos elegíveis foram entregues com sucesso!", 2));
					}
					else
					{
						Game1.playSound("cancel");
						Game1.showRedMessage("Nenhum novo marco atingido no momento!");
					}
					SetupButtons();
					return;
				}
				if (btn.name.StartsWith("claim_milestone_"))
				{
					if (int.TryParse(btn.name.Substring("claim_milestone_".Length), out int pct))
					{
						PokemonPokedex.Instance.ClaimMilestone(pct, forceDebug: true);
						SetupButtons();
						return;
					}
				}
			}
		}
	}

	private void DrawMilestonesTab(SpriteBatch b, int startX, int contentY, int mouseX, int mouseY)
	{
		int totalSpecies = PokemonSpecies.AllSpecies()?.Count(s => !s.IsMega) ?? 1025;
		int caughtCount = PokemonPokedex.Instance.CaughtCount;
		float currentPct = PokemonPokedex.Instance.GetCompletionPercentage();

		// Top Status Box (W = 840, H = 58)
		Rectangle topBox = new Rectangle(startX, contentY, 840, 58);
		IClickableMenu.drawTextureBox(b, topBox.X, topBox.Y, topBox.Width, topBox.Height, new Color(240, 245, 255));

		string dexTitle = $"📖 PROGRESSO DA POKÉDEX: {caughtCount} / {totalSpecies} ({currentPct:F1}%)";
		PokemonUI.Text(b, Game1.dialogueFont, dexTitle, new Rectangle(startX + 14, contentY + 10, 310, 24), new Color(25, 45, 85), center: false, maxScale: 0.52f, shadow: false);

		// Progress Bar inside topBox
		int barX = startX + 14;
		int barY = contentY + 36;
		int barW = 310;
		int barH = 12;
		b.Draw(Game1.staminaRect, new Rectangle(barX, barY, barW, barH), new Color(180, 190, 205));
		int fillW = (int)Math.Clamp((currentPct / 100f) * barW, 0, barW);
		if (fillW > 0)
		{
			b.Draw(Game1.staminaRect, new Rectangle(barX, barY, fillW, barH), new Color(40, 160, 75));
		}

		// Milestone Info Definitions
		var milestoneDetails = new (int Percent, string Title, string Items, string Lore)[]
		{
			(10, "10% - Mochileiro Inicial", "👟 Tênis de Corrida (+1 Vel perm.)\n• 10x Ultra Ball\n• 5x Rare Candy", "Aumenta permanentemente a velocidade de corrida no vale."),
			(25, "25% - Treinador Competitivo", "💼 Estojo Competitivo (7 itens meta)\n• Anel Exp. Share (equipe toda)\n• Leftovers + Life Orb", "Kit completo para batalhas competitivas e treinamento."),
			(50, "50% - Especialista de Campo", "🏥 Centro Pokémon Portátil (cura geral)\n• 2x Ability Patch (Habilidade Oculta)\n• 5x Friendship Candy", "Cura e restaura toda a equipe instantaneamente em qualquer lugar."),
			(75, "75% - Mestre da Rede Pokémon", "💻 Box Link Portátil (acesso ao PC)\n• 1x Master Ball\n• 15x Rare Candy", "Acessa e gerencia todas as suas caixas de Pokémon do PC em campo."),
			(90, "90% - Guardião das Mega Formas", "💎 Cofre de Mega Pedras (8 relíquias)\n• Bracelete Mega (anel de Key Stone)\n• Amuleto Brilhante (3x Shiny)", "Desperta o poder ancestral e triplica encontros brilhantes!"),
			(100, "100% - Lenda do Vale (Mestre)", "👑 Remessa Silph Co (5x Master Ball)\n• 50x Rare Candy\n• 10x Ability Patch", "Recompensa suprema de campeão por completar a Pokédex.")
		};

		int colW = 412;
		int cardH = 112;
		int rowGap = 10;
		int gridStartY = contentY + 68;

		for (int i = 0; i < milestoneDetails.Length; i++)
		{
			var (pct, title, items, lore) = milestoneDetails[i];
			int col = i / 3;
			int row = i % 3;
			int cX = startX + col * 428;
			int cY = gridStartY + row * (cardH + rowGap);

			bool claimed = PokemonPokedex.Instance.HasClaimedMilestone(pct);
			bool eligible = currentPct >= pct;

			Color cardBg = claimed ? new Color(230, 248, 235) : (eligible ? new Color(255, 252, 220) : new Color(248, 248, 250));
			IClickableMenu.drawTextureBox(b, cX, cY, colW, cardH, cardBg);

			Color titleCol = claimed ? new Color(20, 110, 40) : (eligible ? new Color(150, 100, 10) : new Color(60, 70, 85));
			PokemonUI.Text(b, Game1.dialogueFont, title, new Rectangle(cX + 12, cY + 8, 260, 20), titleCol, center: false, maxScale: 0.50f, shadow: false);

			string badgeText = claimed ? "✓ CONCLUÍDO" : (eligible ? "★ DISPONÍVEL" : $"BLOQUEADO ({currentPct:F0}%/{pct}%)");
			Color badgeCol = claimed ? new Color(30, 140, 50) : (eligible ? new Color(200, 120, 0) : new Color(130, 140, 150));
			PokemonUI.Text(b, Game1.smallFont, badgeText, new Rectangle(cX + colW - 140, cY + 8, 128, 18), badgeCol, center: true, maxScale: 0.65f, shadow: false);

			PokemonUI.Text(b, Game1.smallFont, items, new Rectangle(cX + 12, cY + 30, colW - 170, 74), new Color(40, 45, 55), center: false, maxScale: 0.62f, shadow: false);
		}

		// Draw All Milestone Buttons
		foreach (var btn in _milestonesButtons)
		{
			bool hovered = btn.containsPoint(mouseX, mouseY);
			Color btnBg = btn.name switch
			{
				"ms_fill_dex" => hovered ? new Color(210, 250, 220) : new Color(235, 255, 240),
				"ms_clear_dex" => hovered ? new Color(255, 220, 220) : new Color(255, 240, 240),
				"ms_reset_milestones" => hovered ? new Color(255, 235, 200) : new Color(255, 245, 225),
				"ms_claim_all" => hovered ? new Color(210, 235, 255) : new Color(235, 245, 255),
				_ => hovered ? new Color(255, 245, 200) : new Color(255, 255, 240)
			};
			Color textCol = btn.name switch
			{
				"ms_fill_dex" => hovered ? Color.DarkGreen : new Color(20, 120, 40),
				"ms_clear_dex" => hovered ? Color.DarkRed : new Color(150, 30, 30),
				"ms_reset_milestones" => hovered ? Color.DarkGoldenrod : new Color(160, 110, 20),
				"ms_claim_all" => hovered ? Color.DarkBlue : new Color(25, 80, 160),
				_ => hovered ? Color.DarkRed : new Color(130, 60, 10)
			};

			IClickableMenu.drawTextureBox(b, btn.bounds.X, btn.bounds.Y, btn.bounds.Width, btn.bounds.Height, btnBg);
			PokemonUI.Text(b, Game1.smallFont, btn.label, btn.bounds, textCol, center: true, maxScale: 0.68f, shadow: false);
		}
	}

	#endregion
}
