#!/usr/bin/env bash
# ==============================================================================
# PokeValley-Overhaul - Asset Integration & Decomp Setup Script (Linux / macOS / Steam Deck)
# ==============================================================================
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET_DIR="$(dirname "$SCRIPT_DIR")"

echo "=========================================================="
echo "   PokeValley-Overhaul - Asset Setup & Decomp Integrator  "
echo "=========================================================="
echo "Destino: $TARGET_DIR"

if ! command -v git &> /dev/null; then
    echo "[X] Git não foi encontrado. Instale o Git para prosseguir."
    exit 1
fi

TEMP_DIR="$TARGET_DIR/.asset_cache"
mkdir -p "$TEMP_DIR"

clone_and_extract() {
    local NAME="$1"
    local URL="$2"
    local BRANCH="$3"
    local SRC_GRAPHICS="$4"
    local DEST_FOLDER="$5"

    local CLONE_PATH="$TEMP_DIR/$NAME"
    local FINAL_GRAPHICS="$TARGET_DIR/$DEST_FOLDER"

    echo "----------------------------------------------------------"
    echo "[+] Processando assets: $NAME"
    mkdir -p "$FINAL_GRAPHICS"

    if [ ! -d "$CLONE_PATH" ]; then
        echo "    Clonando repositório decomp ($URL)..."
        git clone --depth 1 --branch "$BRANCH" "$URL" "$CLONE_PATH" --filter=blob:none --sparse
        (cd "$CLONE_PATH" && git sparse-checkout set "$SRC_GRAPHICS")
    else
        echo "    Atualizando repositório decomp..."
        (cd "$CLONE_PATH" && git pull --depth 1)
    fi

    if [ -d "$CLONE_PATH/$SRC_GRAPHICS" ]; then
        echo "    Copiando sprites para $FINAL_GRAPHICS..."
        cp -r "$CLONE_PATH/$SRC_GRAPHICS"/* "$FINAL_GRAPHICS/"
    fi
}

clone_and_extract "pokeemerald" "https://github.com/pret/pokeemerald.git" "master" "graphics" "pokeemerald/graphics"
clone_and_extract "pokefirered" "https://github.com/pret/pokefirered.git" "master" "graphics" "pokefirered/graphics"
clone_and_extract "pokeemerald-expansion" "https://github.com/rh-hideout/pokeemerald-expansion.git" "expansion/master" "graphics" "pokeemerald-expansion/graphics"

mkdir -p "$TARGET_DIR/Content/Pokemon"

echo "=========================================================="
echo "  Integração de Assets concluída com sucesso!              "
echo "=========================================================="
echo "Agora você pode compilar com:"
echo "    dotnet build \"Stardew Valley.csproj\" -c Release"
