#!/usr/bin/env pwsh
# publish.ps1 — Gera builds de release para Windows x64 e Linux x64
# Execute a partir da raiz do repositório: .\publish.ps1
# Para macOS: execute publish-mac.sh em um Mac com .NET SDK instalado

$proj   = "ControleMateriais.Desktop/ControleMateriais.Desktop.csproj"
$outDir = "release"

$targets = @(
    @{ rid = "win-x64";   label = "Windows x64" },
    @{ rid = "linux-x64"; label = "Linux x64"   }
)

foreach ($t in $targets) {
    $dest = "$outDir/$($t.rid)"
    Write-Host "==> Publicando $($t.label) em $dest ..." -ForegroundColor Cyan

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
        Write-Host "ERRO ao publicar $($t.label)" -ForegroundColor Red
        exit 1
    }

    # Empacota como .zip para upload no GitHub Releases
    $zip = "$outDir/ControleMateriais-$($t.rid).zip"
    Compress-Archive -Path "$dest\*" -DestinationPath $zip -Force
    Write-Host "    -> $zip" -ForegroundColor Green
}

Write-Host ""
Write-Host "Builds concluidos! Arquivos em ./$outDir/" -ForegroundColor Green
Write-Host "Faca o upload dos .zip em: https://github.com/lfbreciclagemeletronica/Controle-Materiais/releases/new"
