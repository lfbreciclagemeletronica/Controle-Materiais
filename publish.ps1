#!/usr/bin/env pwsh
# publish.ps1 - Gera executaveis portateis para Windows x64 e/ou Linux x64
# Execute a partir da raiz do repositorio: .\publish.ps1
#
# O executavel gerado e auto-contido (self-contained) e nao requer
# instalacao nem permissoes especiais — basta copiar e rodar.
#
# Flags opcionais:
#   -Target win    Publica apenas Windows x64 (padrao)
#   -Target linux  Publica apenas Linux x64
#   -Target all    Publica ambos
#   -OutDir <p>    Pasta de saida (padrao: release)

[CmdletBinding()]
param(
    [ValidateSet("all","win","linux")]
    [string]$Target = "all",
    [string]$OutDir = "release"
)

$proj = "ControleMateriais.Desktop/ControleMateriais.Desktop.csproj"
$bar  = "-" * 70

$targets = @(
    @{ rid = "win-x64";   label = "Windows x64"; exe = "ControleMateriais.Desktop.exe"; key = "win"   },
    @{ rid = "linux-x64"; label = "Linux x64";   exe = "ControleMateriais.Desktop";     key = "linux" }
) | Where-Object { $Target -eq "all" -or $_.key -eq $Target }

Write-Host ""
Write-Host "  +$('=' * 68)+" -ForegroundColor Cyan
Write-Host "  |   LFB - CONTROLE DE MATERIAIS  |  publish.ps1$((' ' * 22))|" -ForegroundColor Cyan
Write-Host "  +$('=' * 68)+" -ForegroundColor Cyan
Write-Host ""

$results = @()

foreach ($t in $targets) {
    $dest = "$OutDir\$($t.rid)"
    Write-Host "  $bar" -ForegroundColor DarkGray
    Write-Host "  >>> Gerando executavel portatil $($t.label) -> $dest" -ForegroundColor Magenta
    Write-Host ""

    dotnet publish $proj `
        -c Release `
        -r $t.rid `
        --self-contained true `
        -o $dest `
        /p:PublishSingleFile=true `
        /p:PublishAot=false `
        /p:PublishReadyToRun=false `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:EnableCompressionInSingleFile=true `
        /p:StripSymbols=true `
        /p:DebugType=none `
        /p:DebugSymbols=false

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "  [X] ERRO ao publicar $($t.label)." -ForegroundColor Red
        exit 1
    }

    $exePath = Join-Path $dest $t.exe
    if (-not (Test-Path $exePath)) {
        Write-Host "  [X] Executavel nao encontrado em: $exePath" -ForegroundColor Red
        exit 1
    }

    $sizeMB = [Math]::Round((Get-Item $exePath).Length / 1MB, 1)
    Write-Host "  [+] $($t.label) gerado: $exePath ($sizeMB MB)" -ForegroundColor Green
    $results += @{ label = $t.label; path = $exePath; size = $sizeMB; key = $t.key }
    Write-Host ""
}

Write-Host "  $bar" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  [+] Build(s) concluido(s)!" -ForegroundColor Green
Write-Host ""
foreach ($r in $results) {
    Write-Host "      $($r.label.PadRight(14)): $($r.path)  [$($r.size) MB]" -ForegroundColor White
}
Write-Host ""
Write-Host "  Windows : copie o .exe para qualquer lugar e execute." -ForegroundColor Yellow
Write-Host "  Linux   : copie o binario, use chmod +x e execute; ou use install-linux.sh." -ForegroundColor Yellow
Write-Host ""
