# PokeValley Overhaul — Relatório de correções (v1.1)

Cópia de trabalho: `D:\PokeValley-Overhaul-v1.1`
Projeto original: `D:\Projetos\Nova pasta\PokeValley-Overhaul` (intocado)

Este relatório cobre apenas o que foi **alterado** nesta cópia. As 1025 espécies, os learnsets, os sprites e as quatro gimmicks (Mega Evolução, Z-Moves, Dynamax/Gigantamax e Teracristalização) não foram tocados.

---

## 1. Persistência (P0)

**Antes:** três arquivos JSON independentes (`pokemon_party_data.json`, `pokemon_pokedex_data.json`, `pokemon_storage_data.json`), gravados com `File.WriteAllText` sem atomicidade. A Pokédex ficava em `%AppData%\StardewValley\pokemon_pokedex_data.json`, **um único arquivo global** compartilhado por todos os saves. PP, felicidade e Tera Type não eram salvos; estado de batalha (Mega, Dynamax, status) vazava para o save.

**Depois:** `src/Pokemon/PokemonSaveStore.cs` (novo) centraliza tudo em um documento versionado (`SchemaVersion = 2`):

- caminho: `<pasta do save>\pokevalley\pokemon_save.json`; se a pasta não existir ou não for gravável, `%AppData%\StardewValley\PokeValley\<save>\pokemon_save_<uid>.json` (por jogador);
- gravação atômica: `.tmp` → `File.Replace` (mantém `.bak`), com fallback para `File.Copy`;
- documento corrompido (somente `JsonException`) é isolado como `.corrupt-<timestamp>`; erro de I/O nunca move nem sobrescreve o arquivo;
- migração automática dos três arquivos legados (renomeados para `.migrated`); a Pokédex global só é importada se o save ainda não tiver dados;
- `PokemonDtoMapper` único para equipe, caixas e troca: restaura PP por golpe, felicidade e Tera Type, recalcula stats e limpa estado de batalha.

Hooks alterados: `SaveGame.cs` (salvar/carregar), `Game1.cs` (novo jogo), `PlayerParty.cs`, `PokemonPokedex.cs` (agora por save, com `Reload`/`ResetForNewGame`), `PokemonStorage.cs` e `PokemonTradeMenu.cs` (mappers duplicados removidos).

---

## 2. Golpes

- 356 golpes que existiam nos learnsets mas não na tabela foram registrados com dados reais (poder, tipo, precisão, PP, categoria e prioridade), extraídos de `pokeemerald-expansion/src/data/moves_info.h`, resolvendo os ternários `B_UPDATED_MOVE_DATA >= GEN_x` para o valor da Gen 9.
- Resultado: **859 registros / 826 nomes únicos**; os **824 nomes** usados por `canonical_learnsets.json` agora resolvem — **0 faltando**.
- `PokemonMove.Get` normaliza hífen/espaço/maiúsculas (`Double-Edge` ↔ `Double Edge`, `U-turn`, `Thunder Shock`) e tem aliases para diferenças reais de grafia (`Vise Grip`, `Faint Attack`, `Hi Jump Kick`). Não devolve mais o genérico de 40 BP para esses casos.
- Duplicata `Feint Attack` (Special) removida; a versão Physical (Gen 6+) permanece.

---

## 3. Ordem de turno

`PokemonBattleMenu.ExecuteTurn` agora ordena por **prioridade → velocidade → sorteio**:

- `GetEffectivePriority`: Z-Moves e Max Moves sempre +0; Prankster dá +1 a golpes de status contra alvos que não sejam Dark;
- empate de velocidade resolvido com `Game1.random.Next(2)`, em vez de o jogador sempre vencer.

`AdvanceMessageQueue` ganhou try/catch: uma exceção no callback não deixa mais o menu preso em `MessageWait`.

---

## 4. Andaime de desenvolvimento e caminhos fixos

- `PokemonHUD.cs`: removidos captura de tela, tour automático, `GlobalUpdate` e o caminho fixo de screenshots (1356 → 1053 linhas). `IsExpanded` restaurado.
- `Game1.cs`: chamadas a `GlobalUpdate`/`TryCaptureScreen` removidas.
- Todas as referências a `C:\Users\Cavalino\...` trocadas por `AppDomain.CurrentDomain.BaseDirectory`; o candidato morto `StardewValley-1.6.15-decompiled\Content\Pokemon` foi removido.

---

## 5. Script de build

`scripts/setup_with_ilspy.ps1`:

- cópia duplicada de `Content/Pokemon` removida;
- guarda de versão: aborta se o `Stardew Valley.dll` não for **1.6.15** (os overrides são escritos sobre esse código exato);
- etapas renumeradas.

---

## 6. README

- "1.6.15+" → **1.6.15 (exato)**;
- removida a alegação de interface 100% em inglês;
- adicionadas as seções "Estado desta build" e "Limitações conhecidas";
- roadmap: v1.1 = esta entrega; breeding passou para v1.2.

---

## 7. O que não foi alterado

- 1025 espécies, learnsets, sprites e a tabela de efetividade de tipos (conferida: 100% correta).
- Mega Evolução, Z-Moves, Dynamax/Gigantamax e Teracristalização.
- Conteúdo de `src/Content/Pokemon` e o restante de `src/Overrides`.

---

## 8. Validação feita (somente estática)

Não há .NET SDK nesta máquina (`dotnet --list-sdks` vazio), portanto **nada foi compilado nem executado**. O que foi verificado:

- 56 arquivos `.cs`, 110.830 linhas: chaves balanceadas em todos (a diferença 1547/1546 em `Utility.cs` é pré-existente, vem de chaves dentro de strings);
- `PokemonMove.cs`: 859 `Register`, 826 únicos, 824 nomes usados, 0 faltando;
- nenhuma ocorrência de `Cavalino`, `RequestScreenshot`, `GlobalUpdate` ou `pokemon_*_data.json` fora da migração;
- nenhuma quebra de linha duplicada (`\r\r`) nos fontes;
- assinaturas de método duplicadas: nenhuma nos arquivos alterados.

---

## 9. Limitações e riscos conhecidos

- **Não compilado.** O próximo passo é `dotnet build` no harness `StardewValley-1.6.15-decompiled` e um smoke test: novo jogo → capturar → salvar → voltar ao título → carregar.
- **Cópia de save:** o menu "Copiar save" do Stardew copia apenas o `.sv`, não a subpasta `pokevalley/`; a cópia perde os Pokémon. Resolver de vez exigiria embutir os dados no `.sv` (mudança de formato, arriscada sem compilador).
- **Multiplayer:** cada cliente grava o próprio arquivo em `%AppData%`; não há sincronização de equipe/caixas entre host e farmhand.
- **Efeitos de golpe:** os 356 golpes novos têm poder/tipo/categoria corretos, mas efeitos secundários só existem para os que passam por `ConfigureMoveEffects`. Golpes de status novos continuam sem efeito de campo.
- **Áudio:** cries e músicas continuam os do Stardew Valley.

---

## 10. Recomendação (P2): reescrever como mod SMAPI

O projeto é um fork do código descompilado: qualquer atualização do jogo quebra o build e o binário não pode ser distribuído. A recomendação de médio prazo é migrar a camada Pokémon para um mod SMAPI (Harmony nos hooks de `Game1`/`SaveGame`), mantendo os dados e as telas atuais. Isso resolve de uma vez compatibilidade entre versões, save dentro do `.sv` (via `ModData`), multiplayer e distribuição legal.
