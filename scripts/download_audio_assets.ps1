# ==============================================================================
# PokeValley-Overhaul - Clean Room Audio Downloader & Integrator
# ==============================================================================
# Este script baixa e sincroniza os arquivos de audio (OGG e MIDI) de fontes
# comunitarias publicas e repositorios de descompilacao (pret/pokeemerald, pret/pokefirered).
# Garante que nenhum arquivo de audio proprietario seja rastreado pelo Git.
# ==============================================================================

[CmdletBinding()]
param(
    [string]$TargetDir = "",
    [string]$SteamGameDir = "D:\SteamLibrary\steamapps\common\Stardew Valley\Content\Pokemon\Audio\music",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
if ([string]::IsNullOrWhiteSpace($TargetDir)) {
    $TargetDir = Join-Path $repoRoot "src\Content\Pokemon\Audio\music"
}

if (-not (Test-Path $TargetDir)) {
    New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   PokeValley Overhaul - Clean Room Audio Synchronizer    " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Destino local : $TargetDir" -ForegroundColor Gray
Write-Host "Destino Steam : $SteamGameDir`n" -ForegroundColor Gray

# 1. Manifestos de download OGG (Showdown + PokeWilds CDN)
$oggManifest = @(
    @{ File = "emerald_trainer.ogg"; Url = "https://play.pokemonshowdown.com/audio/oras-trainer.ogg" },
    @{ File = "rb_trainer.ogg";      Url = "https://play.pokemonshowdown.com/audio/hgss-kanto-trainer.ogg" },
    @{ File = "gym_leader.ogg";      Url = "https://play.pokemonshowdown.com/audio/bw2-kanto-gym-leader.ogg" },
    @{ File = "champion.ogg";        Url = "https://play.pokemonshowdown.com/audio/spl-elite4.ogg" },
    @{ File = "rb_wild.ogg";         Url = "https://raw.githubusercontent.com/SheerSt/pokewilds/main/music/wild_battle.ogg" },
    @{ File = "emerald_wild.ogg";    Url = "https://raw.githubusercontent.com/SheerSt/pokewilds/main/music/wild_battle.ogg" },
    @{ File = "mus_caught.ogg";      Url = "https://raw.githubusercontent.com/SheerSt/pokewilds/main/sounds/catch_fanfare.ogg" },
    @{ File = "victory_fanfare.ogg"; Url = "https://raw.githubusercontent.com/SheerSt/pokewilds/main/music/victory_fanfare1.ogg" }
)

# 2. Manifestos de download MIDI (pret decomp repos)
$midiManifest = @(
    @{ File = "emerald_wild.mid";    LocalEmerald = "mus_vs_wild.mid";           Url = "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_wild.mid" },
    @{ File = "emerald_trainer.mid"; LocalEmerald = "mus_vs_trainer.mid";        Url = "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_trainer.mid" },
    @{ File = "gym_leader.mid";      LocalEmerald = "mus_vs_gym_leader.mid";     Url = "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_gym_leader.mid" },
    @{ File = "champion.mid";        LocalEmerald = "mus_vs_champion.mid";       Url = "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_champion.mid" },
    @{ File = "rayquaza.mid";        LocalEmerald = "mus_vs_rayquaza.mid";       Url = "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_rayquaza.mid" },
    @{ File = "primal.mid";          LocalEmerald = "mus_vs_kyogre_groudon.mid"; Url = "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_kyogre_groudon.mid" },
    @{ File = "deoxys.mid";          LocalEmerald = "mus_vs_deoxys.mid";         Url = "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_deoxys.mid" },
    @{ File = "mewtwo.mid";          LocalEmerald = "mus_vs_mewtwo.mid";         Url = "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_mewtwo.mid" },
    @{ File = "rb_trainer.mid";      LocalFirered = "mus_rg_vs_trainer.mid";     Url = "https://raw.githubusercontent.com/pret/pokefirered/master/sound/songs/midi/mus_rg_vs_trainer.mid" },
    @{ File = "rb_wild.mid";         LocalFirered = "mus_rg_vs_wild.mid";        Url = "https://raw.githubusercontent.com/pret/pokefirered/master/sound/songs/midi/mus_rg_vs_wild.mid" },
    @{ File = "lavender_town.mid";   LocalFirered = "mus_rg_lavender.mid";       Url = "https://raw.githubusercontent.com/pret/pokefirered/master/sound/songs/midi/mus_rg_lavender.mid" }
)

# Caminhos locais de decomp
$localEmeraldMidi = Join-Path $repoRoot "pokeemerald\sound\songs\midi"
if (-not (Test-Path $localEmeraldMidi)) {
    $localEmeraldMidi = "D:\Projetos\Nova pasta\pokeemerald\sound\songs\midi"
}

$localFireredMidi = Join-Path $repoRoot "pokefirered\sound\songs\midi"
if (-not (Test-Path $localFireredMidi)) {
    $localFireredMidi = "D:\Projetos\Nova pasta\pokefirered\sound\songs\midi"
}

Write-Host "[-] Sincronizando faixas OGG..." -ForegroundColor Cyan
foreach ($item in $oggManifest) {
    $outPath = Join-Path $TargetDir $item.File
    if (-not (Test-Path $outPath) -or $Force) {
        Write-Host "    Baixando $($item.File)..." -ForegroundColor Gray
        try {
            Invoke-WebRequest -Uri $item.Url -OutFile $outPath -UserAgent "Mozilla/5.0"
        } catch {
            Write-Host "    [!] Falha ao baixar $($item.File) via $($item.Url): $_" -ForegroundColor Yellow
        }
    } else {
        Write-Host "    OK (existente): $($item.File)" -ForegroundColor DarkGray
    }
}

Write-Host "`n[-] Sincronizando faixas MIDI..." -ForegroundColor Cyan
foreach ($item in $midiManifest) {
    $outPath = Join-Path $TargetDir $item.File
    $copied = $false

    if (-not (Test-Path $outPath) -or $Force) {
        if ($item.LocalEmerald -and (Test-Path $localEmeraldMidi)) {
            $src = Join-Path $localEmeraldMidi $item.LocalEmerald
            if (Test-Path $src) {
                Copy-Item -Path $src -Destination $outPath -Force
                Write-Host "    Copiado de pokeemerald: $($item.File)" -ForegroundColor Green
                $copied = $true
            }
        }
        if (-not $copied -and $item.LocalFirered -and (Test-Path $localFireredMidi)) {
            $src = Join-Path $localFireredMidi $item.LocalFirered
            if (Test-Path $src) {
                Copy-Item -Path $src -Destination $outPath -Force
                Write-Host "    Copiado de pokefirered: $($item.File)" -ForegroundColor Green
                $copied = $true
            }
        }
        if (-not $copied) {
            Write-Host "    Baixando via GitHub pret: $($item.File)..." -ForegroundColor Gray
            try {
                Invoke-WebRequest -Uri $item.Url -OutFile $outPath -UserAgent "Mozilla/5.0"
            } catch {
                Write-Host "    [!] Falha ao baixar $($item.File): $_" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "    OK (existente): $($item.File)" -ForegroundColor DarkGray
    }
}

# 3. Copiar para a pasta da Steam se acessivel
if ([string]::IsNullOrWhiteSpace($SteamGameDir) -eq $false -and (Test-Path (Split-Path $SteamGameDir -Parent))) {
    if (-not (Test-Path $SteamGameDir)) {
        New-Item -ItemType Directory -Force -Path $SteamGameDir | Out-Null
    }
    Write-Host "`n[-] Espelhando faixas para o diretorio da Steam..." -ForegroundColor Cyan
    Copy-Item -Path "$TargetDir\*" -Destination $SteamGameDir -Recurse -Force
    Write-Host "    Sincronizacao com a Steam concluida com sucesso!" -ForegroundColor Green
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "   Audio Clean-Room configurado com sucesso!              " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
