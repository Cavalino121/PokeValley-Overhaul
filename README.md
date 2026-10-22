# 🎮 PokeValley Overhaul

[![Discord](https://img.shields.io/discord/1234567890?color=7289da&label=Discord&logo=discord&logoColor=white)](https://discord.gg/VBHfbvQeE3)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20SteamDeck-blue)](https://dotnet.microsoft.com/)
[![Target](https://img.shields.io/badge/Stardew%20Valley-1.6.15%20(exato)-brightgreen)](https://www.stardewvalley.net/)
[![Language](https://img.shields.io/badge/Language-C%23%2010%20%2F%20.NET%206.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Educational%20%2F%20Non--Commercial-orange)](#-aviso-legal--propriedade-intelectual)

> **PokeValley Overhaul** é uma transformação total e nativa do *Stardew Valley 1.6.15*, integrando um sistema completo de RPG Pokémon cobrindo todas as 9 Gerações (1 a 1025), combate clássico por turnos no estilo GBA/NDS, reprodução e incubação de ovos, mecânicas agrícolas de Berries e Apricorns, trocas multiplayer P2P e todas as gimmicks competitivas (Mega Evolução, Movimentos Z, Dynamax e Teracristalização).

---

## 📢 Comunidade Oficial & Suporte

Junte-se à nossa comunidade no Discord para reportar bugs, sugerir ideias, participar dos testes de novas versões e trocar Pokémon com outros treinadores:

👉 **[Entrar no Discord Oficial do PokeValley](https://discord.gg/VBHfbvQeE3)**

---

## ⚖️ Aviso Legal & Propriedade Intelectual

Este repositório contém **exclusivamente código-fonte aberto** desenvolvido com propósitos de aprendizado, engenharia reversa e interoperabilidade educacional.
* **Nenhum arquivo binário protegido por direitos autorais, ROMs oficiais ou sprites proprietários da Nintendo, Game Freak ou The Pokémon Company são armazenados ou distribuídos neste repositório.**
* Todos os recursos gráficos são obtidos pelo próprio usuário através de projetos de descompilação comunitária de código aberto ([pret/pokeemerald](https://github.com/pret/pokeemerald), [pret/pokefirered](https://github.com/pret/pokefirered)) por meio do script de automação fornecido.
* Stardew Valley é uma marca registrada de ConcernedApe LLC. Este projeto não possui afiliação comercial com a Nintendo Co., Ltd. ou ConcernedApe.

---

## 🌟 Principais Mecânicas & Funcionalidades

### 1. Pokédex Completa (Gerações 1 a 9 — 1025 Espécies)
* Learnsets canônicos sincronizados por nível e TMs/HMs.
* Sistema completo de Naturezas (+10% / -10% de atributos).
* Cálculo canônico de **IVs (0-31)**, **EVs (até 510)** e **Habilidades Ativas/Passivas**.
* Telas de sumário divididas em abas para leitura cristalina de atributos, ataques, IVs/EVs, natureza e medidor de felicidade.

### 2. Motor de Batalha (Turn-Based Engine)
* Interface retrô inspirada nas telas de combate de Emerald e Platinum.
* Sprites frontais e traseiros animados, partículas de dano, barra de vida dinâmica e tremores de impacto.
* Retratos nativos dos NPCs de Stardew Valley (Abigail, Pierre, Shane, Harvey, etc.) reagindo com expressões de surpresa e derrota na arena.
* Condições de status persistentes (**Burn, Poison, Paralyze, Sleep, Freeze**) — incluindo som e efeito visual de dano na tela a cada passo do jogador fora de batalha, idêntico aos jogos clássicos!

### 3. Agricultura Especializada (Berries & Apricorns)
* **10 Berries Canônicas:** Oran, Sitrus, Lum, Cheri, Chesto, Pecha, Rawst, Aspear, Leppa e Persim.
* Plantio liberado nas **4 estações** do ano com rega e colheita repetida.
* **Alquimia Culinária no Barril (Keg):**
  * Oran Berry ➔ Potion
  * Sitrus Berry ➔ Super Potion
  * Pecha Berry ➔ Antidote
  * Cheri Berry ➔ Paralyze Heal
  * Rawst Berry ➔ Burn Heal
  * Aspear Berry ➔ Ice Heal
  * Chesto Berry ➔ Awakening
  * Lum Berry ➔ Full Heal
* **Apricorns Coloridos:** Podem ser colhidos de mudas na fazenda, cozidos na fornalha e forjados em Poké Bolas clássicas e especiais (Net, Dusk, Luxury, Heavy, Premier, etc.).

### 4. Sistema de Evolução & Cabo de Ligação (Link Cable)
* Evoluções por nível, por pedras evolutivas (Water, Fire, Thunder, Leaf, Moon, Sun, Shiny, Dusk, Dawn, Ice) e por felicidade alta.
* **Cabo de Ligação (Link Cable):** Permite evoluir monstros de troca (Kadabra ➔ Alakazam, Machoke ➔ Machamp, Haunter ➔ Gengar, Graveler ➔ Golem, Scyther ➔ Scizor, Onix ➔ Steelix) diretamente pelo inventário!

### 5. Estação de Trocas Multiplayer P2P
* Terminal dedicado para troca direta entre dois jogadores no modo cooperativo.
* Ambos selecionam suas criaturas, visualizam a proposta na tela e confirmam simultaneamente para efetuar a transferência e disparar animações de troca.

### 6. Gimmicks Competitivas
* **Mega Evolução:** Ativada em combate com o Mega Bracelet e as respectivas Mega Stones.
* **Movimentos Z:** 18 Cristais Elementais + Ultranecrozium Z exclusivo do Necrozma.
* **Dynamax & Gigantamax:** Transformação gigante com bônus de HP durante batalhas de ginásio e chefes.
* **Teracristalização:** Mudança do tipo defensivo e boost ofensivo usando Tera Shards e o Tera Orb.

### 7. Comandos de Console & Debug
* Por padrão, atalhos de desenvolvedor ficam ocultos para preservar a imersão.
* Digite o comando `/debugPoke` no chat do jogo para liberar o menu de testes via atalho `F8`.

---

## 🌐 Linguagens Suportadas

| Idioma | Interface | Golpes & Descrições | Diálogos |
| :--- | :---: | :---: | :---: |
| 🇧🇷 **Português (Brasil)** | Sim | Sim | Sim |
| 🇺🇸 Inglês (English) | Não | Não | Não |

> A interface, os nomes de golpes e os diálogos de batalha estão em português do Brasil. O jogo base continua no idioma escolhido na instalação do Stardew Valley.

### Estado desta build

Este projeto é um fork do código descompilado do Stardew Valley **1.6.15** e **não** é um mod SMAPI. Os overrides foram escritos sobre essa versão exata: o script de build aborta se encontrar outra. Se o jogo for atualizado, os overrides precisam ser reaplicados sobre o novo código.

### Limitações conhecidas

* **Persistência:** equipe, Pokédex e caixas do PC ficam em `pokevalley/pokemon_save.json` dentro da pasta do save (com fallback para `%AppData%\StardewValley\PokeValley\<save>` quando a pasta não é gravável)). A escrita é atômica e mantém um `.bak`; o arquivo **não** é embutido no `.sv`, então copiar apenas o `.sv` não leva os Pokémon junto.
* **Multiplayer:** a troca usa mensagens de chat (`[POKE_TRADE]:`) e funciona apenas entre dois jogadores com esta mesma build.
* **Áudio:** os cries e as músicas de rota ainda são os do Stardew Valley; o roadmap 1.3 continua pendente.

---

## 🛠️ Guia de Instalação via Terminal (Arquitetura Clean-Room)

### Pré-requisitos
1. **.NET SDK 6.0 ou 8.0** instalado ([Download .NET](https://dotnet.microsoft.com/download)).
2. **Git** instalado no terminal (`git --version`).
3. Uma cópia legalmente adquirida do **Stardew Valley 1.6.15** instalada (Steam ou GOG). Outras versões não são suportadas.

---

### 🛡️ Método 1: Descompilação Local com ILSpy (Recomendado & 100% Anti-DMCA)
Para garantir total conformidade jurídica, este repositório adota o modelo **Clean-Room**: ele não hospeda nem distribui o código-fonte comercial do Stardew Valley. Em vez disso, o script utiliza a ferramenta open-source oficial **ILSpy (`ilspycmd`)** da ICSharpCode para ler e descompilar o `Stardew Valley.dll` legalmente existente no seu computador e aplicar o código do PokeValley:

#### No Windows (PowerShell):
```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup_with_ilspy.ps1
```

#### No Linux / macOS / Steam Deck:
```bash
chmod +x scripts/setup_with_ilspy.sh
./scripts/setup_with_ilspy.sh
```

O script automaticamente:
1. Detecta ou solicita a sua pasta do Stardew Valley.
2. Descompila o jogo localmente via `ilspycmd`.
3. Injeta a camada de código do PokeValley.
4. Faz o download dos sprites públicos dos repositórios decomp comunitários (`pret/pokeemerald`, `pret/pokefirered`).
5. Compila o jogo pronto para jogar!

---

### ⚡ Método 2: Instalação Manual de Assets & Compilação
Caso você já possua uma pasta com a base descompilada:

#### Baixar e Integrar os Sprites dos Projetos Decomp:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup_assets.ps1
```

#### No Linux / macOS / Steam Deck:
```bash
chmod +x scripts/setup_assets.sh
./scripts/setup_assets.sh
```

> **Nota:** O script realiza um *sparse clone* apenas das pastas gráficas dos repositórios da comunidade `pret/pokeemerald`, `pret/pokefirered` e `rh-hideout/pokeemerald-expansion`.

---

### Passo 3: Compilar o Projeto
Com os assets organizados, execute a compilação nativa pelo .NET SDK:

```bash
dotnet build "Stardew Valley.csproj" -c Release
```

Os binários compilados serão gerados na pasta `bin/Release/netcoreapp6.0/`.

---

### Passo 4: Instalar no seu Stardew Valley
Copie o arquivo gerado `Stardew Valley.dll` e as pastas de sprites (`pokeemerald/`, `pokefirered/`, `Content/Pokemon/`) para a pasta de instalação do seu Stardew Valley:

```bash
# Exemplo de cópia no Windows
Copy-Item "bin/Release/netcoreapp6.0/Stardew Valley.dll" "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\" -Force
```

---

## 🗺️ Roadmap de Atualizações

```mermaid
flowchart LR
    A["v1.0 - Core Launch<br>1025 Mons + Batalha + Berries"] --> B["v1.1 - Estabilidade<br>Persistência + 356 golpes + prioridade"]
    B --> C["v1.2 - Double Battles<br>Líderes de Ginásio do Vale"]
    C --> D["v1.3 - Som & Cries<br>Cries originais + Músicas de Rota"]
    D --> E["v2.0 - Battle Frontier<br>Torre de Batalha no Deserto"]
```

### 🔹 Versão 1.1 (Versão Atual)
- [x] Persistência unificada e atômica de equipe, Pokédex e caixas (`pokemon_save.json` + `.bak`).
- [x] Pokédex deixou de ser global e passou a ser por save.
- [x] 356 golpes que faltavam na tabela (todos os 824 nomes usados nos learnsets agora resolvem).
- [x] Nomes de golpes tolerantes a hífen/espaço (`Double-Edge`, `U-turn`, `Thunder Shock`).
- [x] Prioridade de golpe no turno + desempate de velocidade por sorteio.
- [x] PP, felicidade e Tera Type persistidos; estado de batalha não vaza para o save.
- [x] Removidos o andaime de captura de tela/tour automático e os caminhos fixos do desenvolvedor.

### 🔹 Versão 1.0 (Base)
- [x] 1025 Pokémon com learnsets, naturezas, EVs/IVs e habilidades.
- [x] Motor de batalha 1v1 com animações de ataque e retratos de NPCs.
- [x] 10 Berries com ciclo em 4 estações e crafts de poções no Barril (Keg).
- [x] Apricorns com forja de Pokébolas artesanais.
- [x] Evolução por Nível, Pedras, Amizade e Cabo de Ligação (Link Cable).
- [x] Terminal de Trocas P2P entre 2 jogadores.
- [x] Gimmicks: Mega Evolução, Movimentos Z, Dynamax e Teracristalização.
- [x] Efeito e som de dano no overworld por veneno/queimadura ao caminhar.

### 🔹 Versão 1.2 (Próximo Update)
- [ ] Ovos Pokémon compatíveis com a Incubadora padrão de aves do galinheiro.
- [ ] Passos do jogador diminuindo ativamente o contador de passos do ovo no inventário.
- [ ] Registro completo na Pokedex da fazenda com visualizador de habitats por estação.

### 🔹 Versão 1.2
- [ ] Batalhas em Dupla (2v2) em eventos festivos e contra Líderes do Vale.
- [ ] Torneio anual na Feira de Stardew Valley com troféus de batalha.

### 🔹 Versão 1.3
- [ ] Sistema de áudio procedural com os cries digitais clássicos de cada espécie.
- [ ] Trilhas sonoras dinâmicas de rota de acordo com o clima e a estação.

### 🔹 Versão 2.0
- [ ] Fronteira de Batalha no Deserto de Calico (Battle Tower & Battle Factory).
- [ ] Modo Nuzlocke opcional selecionável na criação do save.

---

## 🤝 Como Contribuir

1. Faça um Fork do projeto (`git checkout -b feature/NovaMecanica`).
2. Implemente suas alterações seguindo o padrão de código existente.
3. Certifique-se de compilar sem avisos ou erros (`dotnet build`).
4. Abra um Pull Request detalhando as alterações.

Dúvidas e discussões técnicas acontecem diariamente no nosso [Discord](https://discord.gg/VBHfbvQeE3).
