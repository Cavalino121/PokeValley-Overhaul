# ==============================================================================
# PokeValley-Overhaul - Asset Integration & Decomp Setup Script (Windows PowerShell)
# ==============================================================================
# Este script faz o download e organização dos sprites públicos de projetos
# de decompilação comunitários (pret/pokeemerald, pret/pokefirered, rh-hideout)
# para que o código do jogo funcione sem que arquivos protegidos por direitos
# autorais sejam distribuídos no repositório.
# ==============================================================================

[CmdletBinding()]
param(
    [string]$TargetDir = (Get-Item $PSScriptRoot).Parent.FullName,
    [switch]$SkipClone
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   PokeValley-Overhaul - Asset Setup & Decomp Integrator  " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Destino: $TargetDir`n"

# 1. Verificar se o Git está disponível
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "[X] Git não foi encontrado no PATH. Instale o Git para continuar." -ForegroundColor Red
    exit 1
}

$tempDir = Join-Path $TargetDir ".asset_cache"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
}

# 2. Definição dos repositórios de decompilação comunitários
$decomps = @(
    @{
        Name = "pokeemerald"
        Url  = "https://github.com/pret/pokeemerald.git"
        Branch = "master"
        SrcGraphics = "graphics"
        DestFolder = "pokeemerald\graphics"
    },
    @{
        Name = "pokefirered"
        Url  = "https://github.com/pret/pokefirered.git"
        Branch = "master"
        SrcGraphics = "graphics"
        DestFolder = "pokefirered\graphics"
    },
    @{
        Name = "pokeemerald-expansion"
        Url  = "https://github.com/rh-hideout/pokeemerald-expansion.git"
        Branch = "expansion/master"
        SrcGraphics = "graphics"
        DestFolder = "pokeemerald-expansion\graphics"
    }
)

foreach ($d in $decomps) {
    $repoName = $d.Name
    $clonePath = Join-Path $tempDir $repoName
    $finalGraphics = Join-Path $TargetDir $d.DestFolder

    Write-Host "----------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "[+] Processando assets: $repoName" -ForegroundColor Green

    if (-not (Test-Path $finalGraphics)) {
        New-Item -ItemType Directory -Force -Path $finalGraphics | Out-Null
    }

    if (-not $SkipClone) {
        if (-not (Test-Path $clonePath)) {
            Write-Host "    Clonando repositório decomp ($($d.Url))..." -ForegroundColor Gray
            git clone --depth 1 --branch $d.Branch $d.Url $clonePath --filter=blob:none --sparse
            Push-Location $clonePath
            git sparse-checkout set $d.SrcGraphics
            Pop-Location
        } else {
            Write-Host "    Atualizando repositório decomp existente..." -ForegroundColor Gray
            Push-Location $clonePath
            git pull --depth 1
            Pop-Location
        }

        $sourceGraphics = Join-Path $clonePath $d.SrcGraphics
        if (Test-Path $sourceGraphics) {
            Write-Host "    Copiando sprites para $finalGraphics..." -ForegroundColor Gray
            Copy-Item -Path "$sourceGraphics\*" -Destination $finalGraphics -Recurse -Force
        }
    } else {
        Write-Host "    Pulo de clone solicitado (-SkipClone)." -ForegroundColor Yellow
    }
}

# 3. Garantir estrutura de pastas do Content/Pokemon
$contentPokemon = Join-Path $TargetDir "Content\Pokemon"
if (-not (Test-Path $contentPokemon)) {
    New-Item -ItemType Directory -Force -Path $contentPokemon | Out-Null
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  Integração de Assets concluída com sucesso!              " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Agora você pode compilar o jogo normalmente com:" -ForegroundColor Cyan
Write-Host "    dotnet build `"Stardew Valley.csproj`" -c Release`n" -ForegroundColor White
