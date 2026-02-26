#!/usr/bin/env pwsh
# publish.ps1 - Gera builds de release para Windows x64 e Linux x64
# Execute a partir da raiz do repositorio: .\publish.ps1
#
# Flags opcionais:
#   -SkipZip      Nao empacota em .zip apos publicar
#   -Target win   Publica apenas Windows
#   -Target linux Publica apenas Linux

[CmdletBinding()]
param(
    [switch]$SkipZip,
    [ValidateSet("all","win","linux")]
    [string]$Target = "all"
)

$proj   = "ControleMateriais.Desktop/ControleMateriais.Desktop.csproj"
$outDir = "release"

$targets = @(
    @{ rid = "win-x64";   label = "Windows x64";  key = "win"   },
    @{ rid = "linux-x64"; label = "Linux x64";    key = "linux" }
) | Where-Object { $Target -eq "all" -or $_.key -eq $Target }

$bar = "-" * 70
Write-Host ""
Write-Host "  +$('=' * 68)+" -ForegroundColor Cyan
Write-Host "  |   LFB - CONTROLE DE MATERIAIS  |  publish.ps1$((' ' * 22))|" -ForegroundColor Cyan
Write-Host "  +$('=' * 68)+" -ForegroundColor Cyan
Write-Host ""

foreach ($t in $targets) {
    $dest = "$outDir/$($t.rid)"
    Write-Host "  $bar" -ForegroundColor DarkGray
    Write-Host "  >>> Publicando $($t.label) -> $dest" -ForegroundColor Magenta
    Write-Host ""

    dotnet publish $proj `
        -c Release `
        -r $t.rid `
        --self-contained true `
        -o $dest `
        /p:PublishSingleFile=true `
        /p:PublishAot=false `
        /p:PublishReadyToRun=true `
        /p:StripSymbols=true

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [X] ERRO ao publicar $($t.label)" -ForegroundColor Red
        exit 1
    }
    Write-Host "  [+] Publicado: $dest" -ForegroundColor Green

    if (-not $SkipZip) {
        $zip = "$outDir/ControleMateriais-$($t.rid).zip"
        Compress-Archive -Path "$dest\*" -DestinationPath $zip -Force
        Write-Host "  [+] ZIP: $zip" -ForegroundColor Green
    }
    Write-Host ""
}

Write-Host "  $bar" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  [+] Builds concluidos! Arquivos em ./$outDir/" -ForegroundColor Green
Write-Host ""
Write-Host "  Para o GitHub Release, faca upload de:" -ForegroundColor Yellow
if ($Target -eq "all" -or $Target -eq "win") {
    Write-Host "    - $outDir/ControleMateriais-win-x64.zip   + install-windows.ps1" -ForegroundColor White
}
if ($Target -eq "all" -or $Target -eq "linux") {
    Write-Host "    - $outDir/ControleMateriais-linux-x64.zip + install-linux.sh" -ForegroundColor White
}
Write-Host ""
Write-Host "  Acesse: https://github.com/lfbreciclagemeletronica/Controle-Materiais/releases/new" -ForegroundColor DarkCyan
Write-Host ""
Write-Host "  Instalacao no Windows (local):" -ForegroundColor DarkGray
Write-Host "    powershell -ExecutionPolicy Bypass -File install-windows.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "  Instalacao no Windows (online, sem ZIP local):" -ForegroundColor DarkGray
Write-Host "    powershell -ExecutionPolicy Bypass -File install-windows.ps1 -Online" -ForegroundColor Gray
Write-Host ""
Write-Host "  Instalacao no Linux (local):" -ForegroundColor DarkGray
Write-Host "    chmod +x install-linux.sh  then  ./install-linux.sh" -ForegroundColor Gray
Write-Host ""
Write-Host "  Instalacao no Linux (online, sem pasta local):" -ForegroundColor DarkGray
Write-Host "    ./install-linux.sh --online" -ForegroundColor Gray
Write-Host ""
