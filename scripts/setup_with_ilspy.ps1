# ==============================================================================
# PokeValley Overhaul - Clean-Room ILSpy Decompiler & Builder (Windows PowerShell)
# ==============================================================================
# Este script implementa a arquitetura Clean-Room para garantir 100% de
# conformidade legal contra DMCA:
# 1. Ele NUNCA distribui código de Stardew Valley nem sprites de Pokémon.
# 2. Utiliza a ferramenta open-source ILSpy (ilspycmd) da ICSharpCode.
# 3. Descompila localmente o "Stardew Valley.dll" original do jogador.
# 4. Aplica o código do PokeValley e compila o jogo modificado nativamente.
# ==============================================================================

[CmdletBinding()]
param(
    [string]$StardewGameDir = "",
    [string]$OutputDir = (Join-Path (Get-Item $PSScriptRoot).Parent.FullName "build_decompiled")
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  PokeValley Overhaul - ILSpy Local Decompiler & Builder   " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Verificar .NET SDK
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "[X] .NET SDK não encontrado. Instale o .NET 6.0 ou 8.0 SDK." -ForegroundColor Red
    exit 1
}

# 2. Instalar ou verificar a ferramenta oficial ilspycmd
Write-Host "`n[-] Verificando a ferramenta open-source ILSpy (ilspycmd)..." -ForegroundColor Gray
$installedTools = dotnet tool list -g
if ($installedTools -notmatch "ilspycmd") {
    Write-Host "    Instalando ilspycmd globalmente via dotnet tool..." -ForegroundColor Yellow
    dotnet tool install -g ilspycmd
} else {
    Write-Host "    [OK] ilspycmd já está instalado." -ForegroundColor Green
}

# 3. Detectar a pasta do Stardew Valley original
if ([string]::IsNullOrWhiteSpace($StardewGameDir)) {
    $commonPaths = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley",
        "C:\Program Files\Steam\steamapps\common\Stardew Valley",
        "D:\SteamLibrary\steamapps\common\Stardew Valley",
        "E:\SteamLibrary\steamapps\common\Stardew Valley",
        "C:\GOG Games\Stardew Valley"
    )

    foreach ($p in $commonPaths) {
        if (Test-Path (Join-Path $p "Stardew Valley.dll")) {
            $StardewGameDir = $p
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($StardewGameDir) -or -not (Test-Path (Join-Path $StardewGameDir "Stardew Valley.dll"))) {
    Write-Host "`n[?] Por favor, digite o caminho da sua pasta do Stardew Valley (onde está 'Stardew Valley.dll'):" -ForegroundColor Yellow
    $StardewGameDir = Read-Host "Caminho da pasta"
}

$stardewDll = Join-Path $StardewGameDir "Stardew Valley.dll"
if (-not (Test-Path $stardewDll)) {
    Write-Host "[X] 'Stardew Valley.dll' não encontrado em: $StardewGameDir" -ForegroundColor Red
    exit 1
}

Write-Host "[+] Jogo original validado: $stardewDll" -ForegroundColor Green

# 4. Descompilar com ILSpy localmente
Write-Host "`n[-] Descompilando localmente com ILSpy (apenas no seu computador)..." -ForegroundColor Cyan
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
}

Write-Host "    Executando: ilspycmd -p -o `"$OutputDir`" `"$stardewDll`"" -ForegroundColor Gray
ilspycmd -p -o "$OutputDir" "$stardewDll"

# 5. Integrar o código do PokeValley
$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$pokemonSrc = Join-Path $repoRoot "StardewValley\Pokemon"
$targetPokemon = Join-Path $OutputDir "StardewValley\Pokemon"

Write-Host "`n[-] Integrando código-fonte do PokeValley..." -ForegroundColor Cyan
if (-not (Test-Path $targetPokemon)) {
    New-Item -ItemType Directory -Force -Path $targetPokemon | Out-Null
}
Copy-Item -Path "$pokemonSrc\*" -Destination $targetPokemon -Recurse -Force

# 6. Baixar Sprites dos Decomps
Write-Host "`n[-] Integrando sprites comunitários (pokeemerald / pokefirered)..." -ForegroundColor Cyan
$assetsScript = Join-Path $PSScriptRoot "setup_assets.ps1"
if (Test-Path $assetsScript) {
    & powershell -ExecutionPolicy Bypass -File "$assetsScript" -TargetDir "$OutputDir"
}

# 7. Compilar
Write-Host "`n[-] Compilando mod com dotnet..." -ForegroundColor Cyan
$csprojPath = Join-Path $OutputDir "Stardew Valley.csproj"
if (Test-Path $csprojPath) {
    dotnet build "$csprojPath" -c Release
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  Processo Clean-Room concluído com sucesso!              " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "O seu Stardew Valley modificado foi gerado em:" -ForegroundColor White
Write-Host "  $OutputDir\bin\Release\netcoreapp6.0\Stardew Valley.dll`n" -ForegroundColor Yellow
