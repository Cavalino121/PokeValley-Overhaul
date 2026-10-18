#!/usr/bin/env bash
# ==============================================================================
# PokeValley Overhaul - Clean-Room ILSpy Decompiler & Builder (Linux / Steam Deck)
# ==============================================================================
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$REPO_ROOT/build_decompiled"

echo "=========================================================="
echo "  PokeValley Overhaul - ILSpy Local Decompiler & Builder   "
echo "=========================================================="

if ! command -v dotnet &> /dev/null; then
    echo "[X] .NET SDK não encontrado. Instale o .NET 6.0 ou 8.0 SDK."
    exit 1
fi

if ! dotnet tool list -g | grep -q "ilspycmd"; then
    echo "[-] Instalando ilspycmd globalmente..."
    dotnet tool install -g ilspycmd
fi

STARDEW_DIR="$1"
if [ -z "$STARDEW_DIR" ]; then
    COMMON_PATHS=(
        "$HOME/.local/share/Steam/steamapps/common/Stardew Valley"
        "$HOME/.steam/steam/steamapps/common/Stardew Valley"
        "$HOME/GOG Games/Stardew Valley"
    )
    for p in "${COMMON_PATHS[@]}"; do
        if [ -f "$p/Stardew Valley.dll" ]; then
            STARDEW_DIR="$p"
            break
        fi
    done
fi

if [ -z "$STARDEW_DIR" ] || [ ! -f "$STARDEW_DIR/Stardew Valley.dll" ]; then
    read -p "Digite o caminho da pasta do seu Stardew Valley: " STARDEW_DIR
fi

STARDEW_DLL="$STARDEW_DIR/Stardew Valley.dll"
if [ ! -f "$STARDEW_DLL" ]; then
    echo "[X] Stardew Valley.dll não encontrado em: $STARDEW_DIR"
    exit 1
fi

echo "[+] Jogo original validado: $STARDEW_DLL"
mkdir -p "$OUTPUT_DIR"

echo "[-] Descompilando localmente com ILSpy..."
ilspycmd -p -o "$OUTPUT_DIR" "$STARDEW_DLL"

echo "[-] Integrando código-fonte do PokeValley..."
mkdir -p "$OUTPUT_DIR/StardewValley/Pokemon"
POKEMON_SRC="$REPO_ROOT/src/Pokemon"
if [ ! -d "$POKEMON_SRC" ]; then POKEMON_SRC="$REPO_ROOT/StardewValley/Pokemon"; fi
cp -r "$POKEMON_SRC/"* "$OUTPUT_DIR/StardewValley/Pokemon/"

if [ -d "$REPO_ROOT/src/Overrides" ]; then
    echo "    Aplicando overrides de hooks no jogo base..."
    cp -r "$REPO_ROOT/src/Overrides/"* "$OUTPUT_DIR/"
fi

if [ -d "$REPO_ROOT/src/Content/Pokemon" ]; then
    mkdir -p "$OUTPUT_DIR/Content/Pokemon"
    cp -r "$REPO_ROOT/src/Content/Pokemon/"* "$OUTPUT_DIR/Content/Pokemon/"
fi

echo "[-] Baixando sprites dos decomps..."
chmod +x "$SCRIPT_DIR/setup_assets.sh"
"$SCRIPT_DIR/setup_assets.sh"

echo "[-] Compilando..."
dotnet build "$OUTPUT_DIR/Stardew Valley.csproj" -c Release

echo "=========================================================="
echo "  Processo Clean-Room concluído com sucesso!              "
echo "=========================================================="
