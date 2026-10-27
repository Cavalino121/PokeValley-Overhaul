# ==============================================================================
# PokeValley Overhaul - Build Local Direto (Modo Tradicional de Desenvolvimento)
# ==============================================================================
# Este script utiliza a base descompilada local existente para compilar
# o executável e a DLL diretamente (WinExe / netcoreapp6.0) e aplicar na Steam.
# O repositório Git mantém a conformidade Clean-Room (apenas código-fonte em src/).
# ==============================================================================

[CmdletBinding()]
param(
    [string]$DecompiledDir = "D:\Projetos\Nova pasta\StardewValley-1.6.15-decompiled",
    [string]$SteamGameDir = "D:\SteamLibrary\steamapps\common\Stardew Valley",
    [switch]$LaunchAfterBuild
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   PokeValley Overhaul - Compilador Local Direto           " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName

# 1. Validar diretórios
if (-not (Test-Path $DecompiledDir)) {
    Write-Host "[X] Pasta da base descompilada não encontrada: $DecompiledDir" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $SteamGameDir)) {
    Write-Host "[X] Pasta da Steam não encontrada: $SteamGameDir" -ForegroundColor Red
    exit 1
}

# 2. Sincronizar fontes do repositório para a base local
Write-Host "`n[-] Sincronizando código-fonte (src/Pokemon)..." -ForegroundColor Gray
$targetPokemon = Join-Path $DecompiledDir "StardewValley\Pokemon"
if (-not (Test-Path $targetPokemon)) { New-Item -ItemType Directory -Force -Path $targetPokemon | Out-Null }
Copy-Item -Path "$repoRoot\src\Pokemon\*" -Destination $targetPokemon -Recurse -Force

Write-Host "[-] Sincronizando overrides de hooks (src/Overrides)..." -ForegroundColor Gray
$overridesSrc = Join-Path $repoRoot "src\Overrides\StardewValley"
$targetOverrides = Join-Path $DecompiledDir "StardewValley"
if (Test-Path $overridesSrc) {
    Copy-Item -Path "$overridesSrc\*" -Destination $targetOverrides -Recurse -Force
}

# 2.1 Garantir assets de áudio clean-room
$musicDir = Join-Path $repoRoot "src\Content\Pokemon\Audio\music"
$testSong = Join-Path $musicDir "champion.ogg"
if (-not (Test-Path $testSong)) {
    Write-Host "[-] Baixando assets de áudio clean-room..." -ForegroundColor Cyan
    $downloadScript = Join-Path $PSScriptRoot "download_audio_assets.ps1"
    if (Test-Path $downloadScript) {
        & $downloadScript -TargetDir $musicDir -SteamGameDir (Join-Path $SteamGameDir "Content\Pokemon\Audio\music")
    }
}

Write-Host "[-] Sincronizando Content/Pokemon..." -ForegroundColor Gray
$contentSrc = Join-Path $repoRoot "src\Content\Pokemon"
$targetContent = Join-Path $SteamGameDir "Content\Pokemon"
if (Test-Path $contentSrc) {
    if (-not (Test-Path $targetContent)) { New-Item -ItemType Directory -Force -Path $targetContent | Out-Null }
    Copy-Item -Path "$contentSrc\*" -Destination $targetContent -Recurse -Force
}

# 3. Compilar
Write-Host "`n[-] Compilando Stardew Valley.csproj (Release)..." -ForegroundColor Cyan
$csproj = Join-Path $DecompiledDir "Stardew Valley.csproj"
dotnet build "$csproj" -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "[X] Falha na compilação!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# 4. Copiar DLL e PDB para a pasta da Steam
$builtDll = Join-Path $DecompiledDir "bin\Release\netcoreapp6.0\Stardew Valley.dll"
$builtPdb = Join-Path $DecompiledDir "bin\Release\netcoreapp6.0\Stardew Valley.pdb"
$destDll = Join-Path $SteamGameDir "Stardew Valley.dll"

# Backup de segurança da DLL vanilla se ainda não existir
$vanillaBackup = Join-Path $SteamGameDir "Stardew Valley.dll.vanilla_backup"
if (-not (Test-Path $vanillaBackup) -and (Test-Path $destDll)) {
    Copy-Item -Path $destDll -Destination $vanillaBackup -Force
    Write-Host "    [Backup] DLL vanilla salva em: $vanillaBackup" -ForegroundColor Gray
}

Copy-Item -Path $builtDll -Destination $destDll -Force
if (Test-Path $builtPdb) {
    Copy-Item -Path $builtPdb -Destination (Join-Path $SteamGameDir "Stardew Valley.pdb") -Force
}

$hash = (Get-FileHash -Path $destDll -Algorithm SHA256).Hash
Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  Build e Atualização Concluídos com Sucesso!            " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "DLL Instalada : $destDll" -ForegroundColor White
Write-Host "SHA256        : $hash" -ForegroundColor Yellow

if ($LaunchAfterBuild) {
    Write-Host "`n[-] Iniciando Stardew Valley..." -ForegroundColor Cyan
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = Join-Path $SteamGameDir "Stardew Valley.exe"
    $psi.WorkingDirectory = $SteamGameDir
    $psi.UseShellExecute = $false
    $psi.EnvironmentVariables["SteamAppId"] = "413150"
    $psi.EnvironmentVariables["SteamGameId"] = "413150"
    [System.Diagnostics.Process]::Start($psi) | Out-Null
}
