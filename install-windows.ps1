#!/usr/bin/env pwsh
# LFB RECICLAGEM ELETRONICA - INSTALADOR WINDOWS
# Controle de Materiais  |  install-windows.ps1
#
# Uso (duas formas):
#   1) A partir do ZIP de release (pasta release\win-x64 presente ao lado):
#        powershell -ExecutionPolicy Bypass -File install-windows.ps1
#
#   2) Download automatico do GitHub (sem arquivos locais de release):
#        powershell -ExecutionPolicy Bypass -File install-windows.ps1 -Online
#
# Parametros opcionais:
#   -Online          Forca download da release mais recente do GitHub
#   -InstallDir <p>  Pasta de destino (padrao: %LOCALAPPDATA%\ControleMateriais.LFB)
#   -NoShortcut      Pula criacao de atalho sem perguntar
#   -Uninstall       Remove a instalacao existente

#Requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$Online,
    [string]$InstallDir = "",
    [switch]$NoShortcut,
    [switch]$Uninstall
)

# -- Configuracoes -------------------------------------------------------------
$GITHUB_OWNER   = "lfbreciclagemeletronica"
$GITHUB_REPO    = "Controle-Materiais"
$ASSET_NAME     = "ControleMateriais-win-x64.zip"
$APP_NAME       = "Controle de Materiais LFB"
$APP_EXE        = "ControleMateriais.Desktop.exe"
$APP_FOLDER     = "ControleMateriais.LFB"
$GIT_URL        = "https://github.com/git-for-windows/git/releases/download/v2.44.0.windows.1/Git-2.44.0-64-bit.exe"

$DEST_DIR       = if ($InstallDir) { $InstallDir } else { Join-Path $env:LOCALAPPDATA $APP_FOLDER }
$DESKTOP        = [Environment]::GetFolderPath("Desktop")
$SHORTCUT_PATH  = Join-Path $DESKTOP "$APP_NAME.lnk"
$RELEASE_DIR    = Join-Path $PSScriptRoot "release\win-x64"

$USE_ONLINE     = $Online.IsPresent -or (-not (Test-Path (Join-Path $RELEASE_DIR $APP_EXE)))

# -- TUI -----------------------------------------------------------------------
function Show-Header {
    try { $Host.UI.RawUI.BackgroundColor = "Black" } catch {}
    Clear-Host
    Write-Host ""
    Write-Host "  LFB RECICLAGEM ELETRONICA" -ForegroundColor Green
    Write-Host "  CONTROLE DE MATERIAIS" -ForegroundColor Cyan
    Write-Host ""
    $mode = if ($USE_ONLINE) { "ONLINE" } else { "LOCAL " }
    Write-Host "  +$("=" * 68)+" -ForegroundColor DarkCyan
    Write-Host "  |   CONTROLE DE MATERIAIS  -  INSTALADOR  [WIN-X64]  [$mode]   |" -ForegroundColor DarkCyan
    Write-Host "  +$("=" * 68)+" -ForegroundColor DarkCyan
    $destPad = $DEST_DIR.PadRight(55)
    Write-Host "  |   Destino : $destPad|" -ForegroundColor DarkGray
    Write-Host "  +$("=" * 68)+" -ForegroundColor DarkCyan
    Write-Host ""
}

function Write-Info  ([string]$m) { Write-Host "  " -NoNewline; Write-Host "[*]" -ForegroundColor Cyan    -NoNewline; Write-Host " $m" }
function Write-Ok    ([string]$m) { Write-Host "  " -NoNewline; Write-Host "[+]" -ForegroundColor Green   -NoNewline; Write-Host " $m" }
function Write-Warn  ([string]$m) { Write-Host "  " -NoNewline; Write-Host "[!]" -ForegroundColor Yellow  -NoNewline; Write-Host " $m" }
function Write-Err   ([string]$m) { Write-Host "  " -NoNewline; Write-Host "[X]" -ForegroundColor Red     -NoNewline; Write-Host " $m" }
function Write-Step  ([string]$m) { Write-Host "  " -NoNewline; Write-Host ">>>" -ForegroundColor Magenta -NoNewline; Write-Host " $m" }

function Ask-YesNo ([string]$Question) {
    Write-Host ""
    Write-Host "  " -NoNewline
    Write-Host "[?]" -ForegroundColor Yellow -NoNewline
    Write-Host " $Question " -NoNewline
    Write-Host "[S/N]" -ForegroundColor White -NoNewline
    Write-Host " " -NoNewline
    $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    if ($key.Character -match '[SsYy]') {
        Write-Host $key.Character -ForegroundColor Green
        return $true
    } else {
        Write-Host $key.Character -ForegroundColor Red
        return $false
    }
}

function Show-Bar ([int]$Pct, [string]$Label = "", [switch]$Newline) {
    $w      = 48
    $filled = [int]([Math]::Round($Pct / 100.0 * $w))
    $empty  = $w - $filled
    $lbl    = if ($Label.Length -gt 36) { $Label.Substring(0,36) } else { $Label }
    Write-Host "`r  " -NoNewline
    Write-Host "[" -ForegroundColor DarkCyan -NoNewline
    Write-Host ("#" * $filled)  -ForegroundColor Green    -NoNewline
    Write-Host ("-" * $empty)   -ForegroundColor DarkGray -NoNewline
    Write-Host "]" -ForegroundColor DarkCyan -NoNewline
    Write-Host " $($Pct.ToString().PadLeft(3))% " -ForegroundColor White -NoNewline
    Write-Host $lbl -ForegroundColor DarkGray -NoNewline
    if ($Newline) { Write-Host "" }
}

function Show-AnimBar ([string]$Label, [int]$Ms = 600) {
    $steps = 30
    $delay = [Math]::Max(1, [int]($Ms / $steps))
    for ($i = 0; $i -le $steps; $i++) {
        Show-Bar ([int]($i / $steps * 100)) $Label
        Start-Sleep -Milliseconds $delay
    }
    Show-Bar 100 $Label -Newline
}

function Show-Divider ([string]$Title = "") {
    Write-Host ""
    if ($Title) {
        $pad = [Math]::Max(0, 66 - $Title.Length - 6)
        Write-Host "  -- $Title $("-" * $pad)" -ForegroundColor DarkGray
    } else {
        Write-Host "  $("-" * 68)" -ForegroundColor DarkGray
    }
    Write-Host ""
}

function Wait-Key {
    Write-Host ""
    Write-Host "  Pressione qualquer tecla para sair..." -ForegroundColor DarkGray
    $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null
    Write-Host ""
}

# -- GitHub helpers ------------------------------------------------------------
function Get-LatestRelease {
    Write-Step "Consultando GitHub Releases..."
    try {
        $ProgressPreference = 'SilentlyContinue'
        $api  = "https://api.github.com/repos/$GITHUB_OWNER/$GITHUB_REPO/releases/latest"
        $hdrs = @{ 'User-Agent' = 'LFB-Installer/2.0' }
        $rel  = Invoke-RestMethod -Uri $api -Headers $hdrs -UseBasicParsing
        $tag  = $rel.tag_name
        $asset = $rel.assets | Where-Object { $_.name -eq $ASSET_NAME } | Select-Object -First 1
        if (-not $asset) { throw "Asset '$ASSET_NAME' nao encontrado na release '$tag'." }
        Write-Ok "Release encontrada: $tag"
        Write-Host "      $($asset.browser_download_url)" -ForegroundColor DarkGray
        return @{ url = $asset.browser_download_url; tag = $tag; size = $asset.size }
    } catch {
        Write-Err "Falha ao consultar GitHub: $_"
        Write-Warn "Verifique sua conexao com a internet."
        return $null
    }
}

function Invoke-Download ([string]$Url, [string]$Dest, [long]$SizeBytes = 0) {
    $ProgressPreference = 'SilentlyContinue'
    try {
        $req = [System.Net.HttpWebRequest]::Create($Url)
        $req.UserAgent = "LFB-Installer/2.0"
        $req.Method    = "GET"
        $resp   = $req.GetResponse()
        $total  = if ($SizeBytes -gt 0) { $SizeBytes } else { $resp.ContentLength }
        $stream = $resp.GetResponseStream()
        $fs     = [System.IO.File]::Create($Dest)
        $buf    = New-Object byte[] 81920
        $dl     = 0L
        while (($read = $stream.Read($buf, 0, $buf.Length)) -gt 0) {
            $fs.Write($buf, 0, $read)
            $dl += $read
            if ($total -gt 0) {
                $pct = [int]($dl * 100 / $total)
                $lbl = "{0:0.0} MB / {1:0.0} MB" -f ($dl/1MB), ($total/1MB)
                Show-Bar $pct $lbl
            } else {
                $lbl = "{0:0.0} MB baixados" -f ($dl/1MB)
                Show-Bar 0 $lbl
            }
        }
        $fs.Close(); $stream.Close(); $resp.Close()
        Show-Bar 100 "Concluido" -Newline
        return $true
    } catch {
        Write-Host ""
        Write-Err "Falha no download: $_"
        return $false
    }
}

function Expand-ZipWithProgress ([string]$ZipPath, [string]$DestDir) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip   = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    $total = $zip.Entries.Count
    $done  = 0
    foreach ($entry in $zip.Entries) {
        $target = [System.IO.Path]::Combine($DestDir, $entry.FullName.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        if ($entry.FullName.EndsWith('/') -or $entry.FullName.EndsWith('\')) {
            [System.IO.Directory]::CreateDirectory($target) | Out-Null
        } else {
            [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($target)) | Out-Null
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
        }
        $done++
        Show-Bar ([int]($done * 100 / $total)) ([System.IO.Path]::GetFileName($entry.FullName))
    }
    $zip.Dispose()
    Show-Bar 100 "Extracao concluida" -Newline
}

function Copy-FilesWithProgress ([string]$Src, [string]$Dst) {
    New-Item -ItemType Directory -Path $Dst -Force | Out-Null
    $files = Get-ChildItem -Path $Src -Recurse
    $total = $files.Count
    $done  = 0
    foreach ($f in $files) {
        $rel  = $f.FullName.Substring($Src.Length).TrimStart('\','/')
        $dest = Join-Path $Dst $rel
        if ($f.PSIsContainer) {
            New-Item -ItemType Directory -Path $dest -Force | Out-Null
        } else {
            New-Item -ItemType Directory -Path (Split-Path $dest) -Force | Out-Null
            Copy-Item -Path $f.FullName -Destination $dest -Force
        }
        $done++
        Show-Bar ([int]($done * 100 / $total)) $rel
    }
    Show-Bar 100 "Concluido" -Newline
    Write-Ok "Arquivos copiados: $total itens."
}

function New-Shortcut ([string]$Target, [string]$WorkDir) {
    try {
        $wsh                   = New-Object -ComObject WScript.Shell
        $link                  = $wsh.CreateShortcut($SHORTCUT_PATH)
        $link.TargetPath       = $Target
        $link.WorkingDirectory = $WorkDir
        $link.Description      = "$APP_NAME - LFB Reciclagem Eletronica"
        $link.IconLocation     = "$Target,0"
        $link.Save()
        return $true
    } catch {
        Write-Warn "Atalho nao criado: $_"
        return $false
    }
}

# =============================================================================
# MAIN
# =============================================================================
Show-Header

# -- Modo desinstalacao --------------------------------------------------------
if ($Uninstall) {
    Show-Divider "DESINSTALACAO"
    if (-not (Test-Path $DEST_DIR)) {
        Write-Warn "Nenhuma instalacao encontrada em: $DEST_DIR"
        Wait-Key; exit 0
    }
    Write-Warn "Sera removido: $DEST_DIR"
    if (Ask-YesNo "Confirmar desinstalacao?") {
        Write-Host ""
        Show-AnimBar "Removendo arquivos..." 800
        Remove-Item -Recurse -Force $DEST_DIR -ErrorAction SilentlyContinue
        if (Test-Path $SHORTCUT_PATH) { Remove-Item -Force $SHORTCUT_PATH }
        Write-Ok "Desinstalado com sucesso."
    } else {
        Write-Info "Operacao cancelada."
    }
    Wait-Key; exit 0
}

# -- Verificar instalacao existente --------------------------------------------
if (Test-Path $DEST_DIR) {
    Show-Divider "INSTALACAO EXISTENTE"
    Write-Warn "Versao instalada detectada em:"
    Write-Host "      $DEST_DIR" -ForegroundColor Yellow
    if (-not (Ask-YesNo "Deseja remover a versao existente e reinstalar?")) {
        Write-Host ""
        Write-Info "Instalacao cancelada."
        Wait-Key; exit 0
    }
    Write-Host ""
    Write-Step "Removendo instalacao anterior..."
    Show-AnimBar "Removendo arquivos antigos..." 700
    try {
        Remove-Item -Recurse -Force $DEST_DIR
        if (Test-Path $SHORTCUT_PATH) { Remove-Item -Force $SHORTCUT_PATH }
        Write-Ok "Versao anterior removida."
    } catch {
        Write-Err "Falha ao remover: $_"
        Wait-Key; exit 1
    }
}

# -- Verificar / Instalar Git --------------------------------------------------
Show-Divider "GIT"
Write-Step "Verificando Git..."
if (Get-Command git -ErrorAction SilentlyContinue) {
    Write-Ok "Git encontrado: $(git --version 2>&1)"
} else {
    Write-Warn "Git nao encontrado no sistema."
    if (Ask-YesNo "Instalar Git for Windows agora?") {
        Write-Host ""
        Write-Step "Baixando Git for Windows..."
        $gitFile = Join-Path $env:TEMP "GitInstaller.exe"
        if (Invoke-Download $GIT_URL $gitFile) {
            Write-Ok "Download concluido."
            Write-Step "Executando instalador do Git (silencioso)..."
            Show-AnimBar "Aguardando instalacao..." 2000
            Start-Process -FilePath $gitFile `
                -ArgumentList "/VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS /COMPONENTS=icons,ext\reg\shellhere,assoc,assoc_sh" `
                -Wait
            Remove-Item $gitFile -Force -ErrorAction SilentlyContinue
            if (Get-Command git -ErrorAction SilentlyContinue) {
                Write-Ok "Git instalado: $(git --version 2>&1)"
            } else {
                Write-Warn "Instalacao do Git pode requerer reinicio do terminal."
            }
        }
    } else {
        Write-Info "Pulando instalacao do Git."
    }
}

# -- Obter arquivos do aplicativo ----------------------------------------------
Show-Divider "DOWNLOAD / ARQUIVOS"

if ($USE_ONLINE) {
    $release = Get-LatestRelease
    if (-not $release) { Wait-Key; exit 1 }
    Write-Host ""
    Write-Step "Baixando $ASSET_NAME..."
    $zipPath = Join-Path $env:TEMP $ASSET_NAME
    if (-not (Invoke-Download $release.url $zipPath $release.size)) { Wait-Key; exit 1 }
    Write-Ok "Download concluido."
    Write-Host ""
    Write-Step "Extraindo arquivos..."
    try {
        New-Item -ItemType Directory -Path $DEST_DIR -Force | Out-Null
        Expand-ZipWithProgress $zipPath $DEST_DIR
        Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
        Write-Ok "Instalado em: $DEST_DIR"
    } catch {
        Write-Err "Falha na extracao: $_"
        Wait-Key; exit 1
    }
    $tagLabel = $release.tag
} else {
    Write-Info "Usando arquivos locais de: $RELEASE_DIR"
    Write-Host ""
    Write-Step "Copiando arquivos..."
    try {
        Copy-FilesWithProgress $RELEASE_DIR $DEST_DIR
    } catch {
        Write-Err "Falha ao copiar arquivos: $_"
        Wait-Key; exit 1
    }
    $tagLabel = "(local)"
}

# -- Atalho na Area de Trabalho ------------------------------------------------
Show-Divider "ATALHO"
$exePath = Join-Path $DEST_DIR $APP_EXE

if (-not $NoShortcut -and (Ask-YesNo "Criar atalho na Area de Trabalho?")) {
    Write-Host ""
    Write-Step "Criando atalho..."
    Show-AnimBar "Configurando atalho..." 400
    if (New-Shortcut $exePath $DEST_DIR) {
        Write-Ok "Atalho criado: $SHORTCUT_PATH"
    }
} else {
    Write-Info "Atalho nao criado."
}

# -- Atalho no Menu Iniciar ----------------------------------------------------
$startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
$startLink = Join-Path $startMenu "$APP_NAME.lnk"
if (-not (Test-Path $startLink)) {
    try {
        $wsh2                  = New-Object -ComObject WScript.Shell
        $lnk2                  = $wsh2.CreateShortcut($startLink)
        $lnk2.TargetPath       = $exePath
        $lnk2.WorkingDirectory = $DEST_DIR
        $lnk2.Description      = "$APP_NAME - LFB Reciclagem Eletronica"
        $lnk2.IconLocation     = "$exePath,0"
        $lnk2.Save()
        Write-Ok "Atalho no Menu Iniciar criado."
    } catch {
        Write-Warn "Atalho no Menu Iniciar nao pode ser criado: $_"
    }
}

# -- Concluido -----------------------------------------------------------------
$pad1 = $tagLabel.PadRight(43)
$pad2 = if ($DEST_DIR.Length -gt 56) { $DEST_DIR.Substring(0,53) + "..." } else { $DEST_DIR.PadRight(56) }
Write-Host ""
Write-Host "  +$("=" * 68)+" -ForegroundColor Green
Write-Host "  |                                                                  |" -ForegroundColor Green
Write-Host "  |   [+] INSTALACAO CONCLUIDA COM SUCESSO!                         |" -ForegroundColor Green
Write-Host "  |                                                                  |" -ForegroundColor DarkGreen
Write-Host "  |   Versao  : $pad1 |" -ForegroundColor DarkGreen
Write-Host "  |   Local   : $pad2 |" -ForegroundColor DarkGreen
Write-Host "  |                                                                  |" -ForegroundColor DarkGreen
Write-Host "  +$("=" * 68)+" -ForegroundColor Green
Wait-Key
