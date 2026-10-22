# PokeValley Overhaul — Relatório de correções (v1.1)

Cópia de trabalho: `D:\PokeValley-Overhaul-v1.1`
Projeto original: `D:\Projetos\Nova pasta\PokeValley-Overhaul` (intocado)

Este relatório cobre apenas o que foi **alterado** nesta cópia. As 1025 espécies, os learnsets, os sprites e as quatro gimmicks (Mega Evolução, Z-Moves, Dynamax/Gigantamax e Teracristalização) não foram tocados.

---

## 1. Persistência (P0)

**Antes:** três arquivos JSON independentes (`pokemon_party_data.json`, `pokemon_pokedex_data.json`, `pokemon_storage_data.json`), gravados com `File.WriteAllText` sem atomicidade. A Pokédex ficava em `%AppData%\StardewValley\pokemon_pokedex_data.json`, **um único arquivo global** compartilhado por todos os saves. PP, felicidade e Tera Type não eram salvos; estado de batalha (Mega, Dynamax, status) vazava para o save.

**Depois:** `src/Pokemon/PokemonSaveStore.cs` centraliza tudo em um documento versionado (`SchemaVersion = 3`) gravado em **dois veículos**:

1. **Dentro do `.sv`** — chaves em `Farmer.modData`:
   - `PokeValley.Pokemon.Party`;
   - `PokeValley.Pokemon.Pokedex`;
   - `PokeValley.Pokemon.Storage`;
   - `PokeValley.Pokemon.SavedAt` (carimbo UTC).

   O serializador vanilla grava `Character.modDataForSerialization` (`[XmlElement("modData")]`) dentro do save e o netcode sincroniza o mesmo dicionário. Cada farmer carrega a **sua própria** equipe, Pokédex e caixas, então o host grava todos os jogadores no `.sv` e o dado deixa de depender de arquivos por UID em `%AppData%`.

2. **Espelho JSON** — `<pasta do save>\pokevalley\pokemon_save.json`; se a pasta não existir ou não for gravável, `%AppData%\StardewValley\PokeValley\<save>\pokemon_save_<uid>.json`. Gravação atômica (`.tmp` → `File.Replace`, mantendo `.bak`), documento corrompido isolado como `.corrupt-<timestamp>` e erro de I/O nunca move nem sobrescreve o arquivo. O espelho serve para inspeção, backup e migração; **não é necessário para jogar**.

**Gravação por seção:** `SaveParty`, `PokemonPokedex.Save` e `PokemonStorage.SaveStorage` gravam apenas a sua chave (`Party` / `Pokedex` / `Storage`) e o carimbo, reduzindo o payload sincronizado a cada alteração. O `.sv` continua recebendo o documento completo.

**Precedência e recuperação:**

- o `modData` embutido vence o espelho quando o carimbo é igual ou mais novo;
- single-player/host: se o espelho estiver mais novo que o carimbo do `.sv` (crash entre dois saves), o espelho é recuperado, reinserido no `modData` e o `.sv` seguinte já sai correto;
- farmhands: o `modData` (autoridade do host) vence sempre; o espelho local é apenas cópia de inspeção;
- sonda diferida: se o registro do farmer chegar pelo netcode **depois** do carregamento (entrada em multiplayer), o documento é relido uma vez quando as chaves aparecem;
- migração automática dos três arquivos legados (renomeados para `.migrated`); a Pokédex global só é importada se o save ainda não tiver dados;
- `PokemonDtoMapper` único para equipe, caixas e troca: restaura PP por golpe, felicidade e Tera Type, recalcula stats e limpa estado de batalha.

**Copiar save:** como o estado agora vive no `.sv`, copiar apenas o arquivo `.sv` (ou a pasta inteira) preserva os Pokémon. O espelho é opcional.

**Restaurar um backup antigo:** se você restaurar um `.sv` antigo e mantiver a subpasta `pokevalley/` mais nova, a recuperação de crash pode preferir o espelho. Para voltar de verdade, apague (ou renomeie) `pokevalley/` junto com a restauração.

Hooks alterados: `SaveGame.cs` (salvar/carregar), `Game1.cs` (novo jogo), `PlayerParty.cs`, `PokemonPokedex.cs` (agora por save, com `Reload`/`ResetForNewGame`), `PokemonStorage.cs` e `PokemonTradeMenu.cs` (mappers duplicados removidos).

---

## 2. Multiplayer e trocas

- **Estado por jogador:** cada farmer tem a sua equipe/Pokédex/caixas no próprio `modData`; o host persiste o de todos os farmhands no `.sv`. Não há mais um arquivo global compartilhado.
- **Troca:** o payload é comprimido com GZip, codificado em Base64 e fragmentado em mensagens `PART|...` (300 caracteres por parte, reassemblagem com timeout de 20 s e limite de 64 partes). O pacote é enviado **ao parceiro** (`SendPacket(packet, recipient)`), não a `AllPlayers`; o receptor remonta e desserializa. Há fallback para o JSON antigo sem compressão.
- **Compatibilidade:** o prefixo de chat `[POKE_TRADE]:` e o encaminhamento em `ChatBox.cs` continuam os mesmos; o protocolo novo é transparente para o chat.
- **Entrada em partida:** a sonda diferida do `PokemonSaveStore` cobre o caso em que o registro do farmhand chega depois do load.

Não foi feito um teste ao vivo com dois clientes; a validação foi de compilação e de protocolo (round-trip de compressão/fragmentação).

---

## 3. Golpes: dados e efeitos secundários

- 356 golpes que existiam nos learnsets mas não na tabela foram registrados com dados reais (poder, tipo, precisão, PP, categoria e prioridade), extraídos de `pokeemerald-expansion/src/data/moves_info.h`, resolvendo os ternários `B_UPDATED_MOVE_DATA >= GEN_x` para o valor da Gen 9.
- Resultado: **859 registros / 826 nomes únicos**; os **824 nomes** usados por `canonical_learnsets.json` agora resolvem — **0 faltando**.
- `PokemonMove.Get` normaliza hífen/espaço/maiúsculas (`Double-Edge` ↔ `Double Edge`, `U-turn`, `Thunder Shock`) e tem aliases para diferenças reais de grafia (`Vise Grip`, `Faint Attack`, `Hi Jump Kick`). Não devolve mais o genérico de 40 BP para esses casos.
- Duplicata `Feint Attack` (Special) removida; a versão Physical (Gen 6+) permanece.

**Motor de efeitos (`ConfigureAdvancedEffects` + `PokemonBattleMenu`):**

- estágios de status (self e alvo) com chance, incluindo Swords Dance, Shell Smash, Close Combat, Overheat, Acid Spray, Growl, Charm, Screech, Parting Shot, Bitter Malice, Diamond Storm, Cotton Guard, Decorate, Coaching, Gear Up, entre outros;
- poder variável: Eruption/Water Spout (HP do usuário), Wring Out/Crush Grip (HP do alvo), Gyro Ball, Electro Ball, Stored Power, Punishment; condições de dobra (Brine, Venoshock, Hex, Facade, Smelling Salts, Wake-Up Slap, Bolt Beak etc. conforme a tabela);
- dano fixo: Ruination/Nature's Madness/Super Fang (metade do HP atual), Night Shade/Seismic Toss (nível), Dragon Rage (40), Sonic Boom (20); dano fixo de nível/plano ignora imunidades de tipo, metade-do-HP respeita;
- multi-hit: Double Hit, Double Iron Bash, Dragon Darts, Dual Chop, Gear Grind, Tachyon Cutter, Twin Beam, Triple Dive, Population Bomb, Tail Slap, Bullet Seed, Icicle Spear, Rock Blast, Pin Missile, Water Shuriken — com distribuição 35/35/15/15 e mensagem "Acertou N vezes!";
- crítico: Storm Throw/Frost Breath sempre críticos; Aqua Cutter/Stone Axe com alta taxa; Shell Armor/Battle Armor anulam;
- flinch: Fake Out (100%), Iron Head, Air Slash, Rock Slide, Icicle Crash, Double Iron Bash (30%), Zen Headbutt, Waterfall, Dark Pulse (20%) etc.; o flinch é limpo no início de cada turno e bloqueia a ação de quem ainda não agiu;
- recuo: Head Smash/Light of Ruin/Chloroblast/Steel Beam/Mind Blown (50%), Double-Edge/Brave Bird/Flare Blitz/Volt Tackle/Wood Hammer/Wave Crash (1/3), Take Down/Submission/Head Charge/Wild Charge (25%); Rock Head/Magic Guard anulam; o recuo nunca desmaia o usuário;
- custo de HP: Belly Drum, Clangorous Soul, Fillet Away (nunca deixam o usuário com 0 de HP);
- Salt Cure: residual de 1/8 por turno (1/4 contra Water/Steel), 99 turnos, limpo ao trocar/sair da batalha;
- Clear Smog zera estágios do alvo; Topsy-Turvy inverte; Acupressure sorteia +2; Fell Stinger dá +3 de Attack ao nocaute;
- Psyshock/Psystrike/Secret Sword usam Attack/Defense para o dano, mas continuam especiais para escolha de item/queimadura;
- cura/dreno: Strength Sap, Life Dew, Giga Drain, Drain Punch, Leech Life, Horn Leech, Draining Kiss, Oblivion Wing, Parabolic Charge etc.

**Cobertura medida:** dos 356 golpes novos, **162 têm pelo menos um efeito mapeado**; 194 continuam sem efeito próprio. A maioria dos restantes é de utilidade de campo/charge/trapping que o motor não modela: Protect/Detect, telas (Reflect/Light Screen/Aurora Veil), hazards (Stealth Rock/Spikes/Toxic Spikes/Sticky Web), terreno, Tailwind, Trick Room, charge/recharge (Solar Beam, Meteor Beam, Giga Impact), trapping (Magma Storm, Infestation, Snap Trap), trocas de stat (Power Swap, Guard Swap, Heart Swap), cópia de habilidade e afins. Esses golpes não travam nem causam exceção: caem na mensagem neutra "Mas nada aconteceu!".

---

## 4. Ordem de turno

`PokemonBattleMenu.ExecuteTurn` agora ordena por **prioridade → velocidade → sorteio**:

- `GetEffectivePriority`: Z-Moves e Max Moves sempre +0; Prankster dá +1 a golpes de status contra alvos que não sejam Dark;
- empate de velocidade resolvido com `Game1.random.Next(2)`, em vez de o jogador sempre vencer;
- estágios de Speed entram no cálculo via `EffectiveSpeedOf`/multiplicador de estágio.

`AdvanceMessageQueue` ganhou try/catch: uma exceção no callback não deixa mais o menu preso em `MessageWait`.

---

## 5. Andaime de desenvolvimento e caminhos fixos

- `PokemonHUD.cs`: removidos captura de tela, tour automático, `GlobalUpdate` e o caminho fixo de screenshots (1356 → 1053 linhas). `IsExpanded` restaurado.
- `Game1.cs`: chamadas a `GlobalUpdate`/`TryCaptureScreen` removidas.
- Todas as referências a `C:\Users\Cavalino\...` trocadas por `AppDomain.CurrentDomain.BaseDirectory`; o candidato morto `StardewValley-1.6.15-decompiled\Content\Pokemon` foi removido.

---

## 6. Build

- **.NET SDK 8.0.426 instalado** (`dotnet --version` confirmado); o alvo do jogo continua `net6.0` (o SDK 8 compila normalmente, com o aviso esperado NETSDK1138 de fim de suporte).
- Harness de compilação real em `C:\Users\Cavalino\AppData\Local\Temp\opencode\pvbuild\PokeValley.Build.csproj`: compila o código descompilado do Stardew Valley 1.6.15 + todos os fontes do mod (`src\Overrides` + `src\Pokemon`), sem tocar nos fontes do harness.
- `scripts/setup_with_ilspy.ps1`: cópia duplicada de `Content/Pokemon` removida; guarda de versão aborta se o `Stardew Valley.dll` não for **1.6.15**; etapas renumeradas.

---

## 7. README

- "1.6.15+" → **1.6.15 (exato)**;
- removida a alegação de interface 100% em inglês;
- adicionadas as seções "Estado desta build" e "Limitações conhecidas";
- roadmap: v1.1 = esta entrega; breeding passou para v1.2.

---

## 8. O que não foi alterado

- 1025 espécies, learnsets, sprites e a tabela de efetividade de tipos (conferida: 100% correta).
- Mega Evolução, Z-Moves, Dynamax/Gigantamax e Teracristalização.
- Conteúdo de `src/Content/Pokemon` e o restante de `src/Overrides`.

---

## 9. Validação feita

- **Compilação real** com .NET SDK 8.0.426: **0 erros**. Os únicos avisos vêm de arquivos do harness descompilado (CS1717 em `SaveGame.loadDataToFarmer`, CA1416/CA2200 em `KeyboardDispatcher`/`StartupPreferences`), não dos fontes do mod.
- **Auditoria em runtime** (console externo referenciando o assembly compilado): 356 golpes novos instanciados via `PokemonMove.Get`, **0 exceções**, **162 com pelo menos um efeito mapeado**. Amostras conferidas: Swords Dance, Shell Smash, Acid Spray, Close Combat, Eruption, Gyro Ball, Fake Out, Double Iron Bash (2 hits, 30% flinch), Take Down, Ruination, Salt Cure, Strength Sap, Dire Claw, Stored Power, Triple Arrows, Belly Drum, Life Dew, Growl, Flame Charge, Psyshock.
- **Persistência embutida:** teste real de `XmlSerializer` com `StardewValley.Mods.ModDataDictionary` provou que as chaves/valores sobrevivem ao XML do save (round-trip com 2 chaves), e a reflexão em `Farmer` confirmou `modDataForSerialization` anotado com `[XmlElement("modData")]`. O documento JSON também faz round-trip com `SavedAtUtc`/`ActiveIndex`.
- **Não houve smoke test dentro do jogo** (novo jogo → capturar → salvar → título → carregar) nem teste ao vivo de multiplayer.

---

## 10. Limitações e riscos conhecidos

- **Sem teste em jogo.** A compilação e a auditoria de runtime cobrem a tabela de golpes e a estrutura do save, mas não a integração visual/loop de batalha. O próximo passo é o smoke test acima.
- **Efeitos de campo/charge/trapping** continuam ausentes (Protect/Detect, telas, hazards, terreno, Tailwind, Trick Room, charge/recharge, trapping, troca de itens/abilidades). Os golpes existem e causam dano normal; não há efeito de campo.
- **Curse de Ghost** usa a versão genérica (+Atk/+Def/−Spe) em vez do custo de 50% de HP com dano residual.
- **Meteor Beam** ganha +1 SpA sem o turno de carga (o motor não tem carga); **Solar Beam/Solar Blade** atacam no mesmo turno.
- **Precisão/evasão** não têm estágios no motor; golpes que baixam Accuracy (Mirror Shot, Mud Bomb, Night Daze) não aplicam esse efeito.
- **Peso** não existe em `PokemonSpecies`, então Grass Knot/Low Kick/Heavy Slam/Heat Crash não escalam por peso.
- **Multiplayer** não foi testado com dois clientes; a sincronização depende do netcode vanilla de `modData` e da autoridade do host.
- **Espelho versus backup:** restaurar um `.sv` antigo sem remover `pokevalley/` pode fazer a recuperação de crash preferir o espelho mais novo (ver §1).
- **Tamanho do save:** as chaves de `modData` (especialmente as caixas) aumentam o `.sv` e o `SaveGameInfo`; o documento é compacto (sem indentação) no embutido.
- **Áudio:** cries e músicas continuam os do Stardew Valley.

---

## 11. Recomendação (P2): reescrever como mod SMAPI

O projeto é um fork do código descompilado: qualquer atualização do jogo quebra o build e o binário não pode ser distribuído. A recomendação de médio prazo é migrar a camada Pokémon para um mod SMAPI (Harmony nos hooks de `Game1`/`SaveGame`), mantendo os dados e as telas atuais. Isso resolve de uma vez compatibilidade entre versões, save dentro do `.sv` (via `ModData`), multiplayer e distribuição legal.
