#!/usr/bin/env pwsh
# ╔══════════════════════════════════════════════════════════════════╗
# ║   LFB RECICLAGEM ELETRÔNICA — INSTALADOR WINDOWS                ║
# ║   Controle de Materiais                                          ║
# ╚══════════════════════════════════════════════════════════════════╝
# Executar como Administrador:
#   powershell -ExecutionPolicy Bypass -File install-windows.ps1

#Requires -Version 5.1

# ── Configurações ────────────────────────────────────────────────────────────
$APP_NAME       = "Controle de Materiais LFB"
$APP_EXE        = "ControleMateriais.Desktop.exe"
$APP_FOLDER     = "ControleMateriais.LFB"
$INSTALL_DIR    = Join-Path $env:LOCALAPPDATA $APP_FOLDER
$DESKTOP        = [Environment]::GetFolderPath("Desktop")
$SHORTCUT_PATH  = Join-Path $DESKTOP "$APP_NAME.lnk"
$RELEASE_DIR    = Join-Path $PSScriptRoot "release\win-x64"
$GIT_INSTALLER  = "https://github.com/git-for-windows/git/releases/download/v2.44.0.windows.1/Git-2.44.0-64-bit.exe"
$GIT_INSTALLER_FILE = Join-Path $env:TEMP "GitInstaller.exe"

# ── Paleta TUI ────────────────────────────────────────────────────────────────
function Header {
    Clear-Host
    $Host.UI.RawUI.BackgroundColor = "Black"
    $Host.UI.RawUI.ForegroundColor = "Green"
    Clear-Host
    Write-Host ""
    Write-Host "  ██╗     ███████╗██████╗      ██████╗ ███████╗ ██████╗██╗██████╗  " -ForegroundColor Green
    Write-Host "  ██║     ██╔════╝██╔══██╗     ██╔══██╗██╔════╝██╔════╝██║██╔══██╗ " -ForegroundColor Green
    Write-Host "  ██║     █████╗  ██████╔╝     ██████╔╝█████╗  ██║     ██║██████╔╝ " -ForegroundColor DarkGreen
    Write-Host "  ██║     ██╔══╝  ██╔══██╗     ██╔══██╗██╔══╝  ██║     ██║██╔══██╗ " -ForegroundColor DarkGreen
    Write-Host "  ███████╗██║     ██████╔╝     ██║  ██║███████╗╚██████╗██║██║  ██║ " -ForegroundColor Cyan
    Write-Host "  ╚══════╝╚═╝     ╚═════╝      ╚═╝  ╚═╝╚══════╝ ╚═════╝╚═╝╚═╝  ╚═╝ " -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  ╔══════════════════════════════════════════════════════════════╗" -ForegroundColor DarkCyan
    Write-Host "  ║      CONTROLE DE MATERIAIS — INSTALADOR v1.0  [WIN-X64]     ║" -ForegroundColor DarkCyan
    Write-Host "  ╚══════════════════════════════════════════════════════════════╝" -ForegroundColor DarkCyan
    Write-Host ""
}

function Log-Info  { param($msg) Write-Host "  [*] $msg" -ForegroundColor Cyan }
function Log-Ok    { param($msg) Write-Host "  [+] $msg" -ForegroundColor Green }
function Log-Warn  { param($msg) Write-Host "  [!] $msg" -ForegroundColor Yellow }
function Log-Error { param($msg) Write-Host "  [X] $msg" -ForegroundColor Red }
function Log-Step  { param($msg) Write-Host "  >>> $msg" -ForegroundColor Magenta }

function Prompt-YesNo {
    param([string]$Question)
    Write-Host ""
    Write-Host "  [?] $Question" -ForegroundColor Yellow -NoNewline
    Write-Host " [S/N] " -ForegroundColor White -NoNewline
    $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    Write-Host $key.Character -ForegroundColor Green
    return ($key.Character -match '[SsYy]')
}

function Draw-ProgressBar {
    param([int]$Percent, [string]$Label = "")
    $width   = 50
    $filled  = [int]([Math]::Round($Percent / 100 * $width))
    $empty   = $width - $filled
    $bar     = ("█" * $filled) + ("░" * $empty)
    Write-Host "`r  [" -NoNewline -ForegroundColor DarkCyan
    Write-Host $bar -NoNewline -ForegroundColor Green
    Write-Host "] " -NoNewline -ForegroundColor DarkCyan
    Write-Host "$Percent% " -NoNewline -ForegroundColor White
    if ($Label) { Write-Host $Label -NoNewline -ForegroundColor DarkGray }
    Write-Host "" 
}

function Animate-Progress {
    param([string]$Label, [int]$DurationMs = 1500)
    $steps = 20
    $delay = [int]($DurationMs / $steps)
    for ($i = 0; $i -le $steps; $i++) {
        $pct = [int]($i / $steps * 100)
        Write-Host "`r  [" -NoNewline -ForegroundColor DarkCyan
        $filled = [int]([Math]::Round($pct / 100 * 50))
        $empty  = 50 - $filled
        Write-Host ("█" * $filled) -NoNewline -ForegroundColor Green
        Write-Host ("░" * $empty)  -NoNewline -ForegroundColor DarkGray
        Write-Host "] " -NoNewline -ForegroundColor DarkCyan
        Write-Host "$pct% $Label" -NoNewline -ForegroundColor DarkGray
        Start-Sleep -Milliseconds $delay
    }
    Write-Host ""
}

# ═════════════════════════════════════════════════════════════════════════════
Header

# ── Verificar fonte do instalador ─────────────────────────────────────────────
Log-Step "Verificando arquivos de instalação..."
if (-not (Test-Path $RELEASE_DIR)) {
    Log-Error "Pasta release\win-x64 não encontrada!"
    Log-Warn  "Execute primeiro: .\publish.ps1"
    Write-Host ""
    Pause
    exit 1
}
if (-not (Test-Path (Join-Path $RELEASE_DIR $APP_EXE))) {
    Log-Error "Executável '$APP_EXE' não encontrado em $RELEASE_DIR"
    Write-Host ""
    Pause
    exit 1
}
Log-Ok "Arquivos de instalação encontrados."
Write-Host ""

# ── Verificar instalação existente ────────────────────────────────────────────
if (Test-Path $INSTALL_DIR) {
    Log-Warn "Instalação existente detectada em:"
    Write-Host "  $INSTALL_DIR" -ForegroundColor Yellow
    Write-Host ""
    $remove = Prompt-YesNo "Deseja remover a versão existente e instalar a mais nova?"
    if (-not $remove) {
        Log-Info "Instalação cancelada pelo usuário."
        Write-Host ""
        Pause
        exit 0
    }
    Write-Host ""
    Log-Step "Removendo versão anterior..."
    Animate-Progress "Removendo arquivos antigos..." 800
    try {
        Remove-Item -Recurse -Force $INSTALL_DIR
        if (Test-Path $SHORTCUT_PATH) { Remove-Item -Force $SHORTCUT_PATH }
        Log-Ok "Versão anterior removida com sucesso."
    } catch {
        Log-Error "Falha ao remover: $_"
        Pause
        exit 1
    }
    Write-Host ""
}

# ── Verificar / Instalar Git ───────────────────────────────────────────────────
Log-Step "Verificando Git..."
$gitExists = $null -ne (Get-Command git -ErrorAction SilentlyContinue)
if ($gitExists) {
    $gitVer = git --version 2>&1
    Log-Ok "Git já instalado: $gitVer"
} else {
    Log-Warn "Git não encontrado no sistema."
    $installGit = Prompt-YesNo "Deseja instalar o Git for Windows agora?"
    if ($installGit) {
        Write-Host ""
        Log-Step "Baixando Git for Windows..."
        try {
            $ProgressPreference = 'SilentlyContinue'
            Invoke-WebRequest -Uri $GIT_INSTALLER -OutFile $GIT_INSTALLER_FILE -UseBasicParsing
            Log-Ok "Download concluído."
            Log-Step "Instalando Git silenciosamente..."
            Animate-Progress "Instalando Git..." 3000
            Start-Process -FilePath $GIT_INSTALLER_FILE `
                -ArgumentList "/VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /COMPONENTS=icons,ext\reg\shellhere,assoc,assoc_sh" `
                -Wait
            Log-Ok "Git instalado com sucesso."
            Remove-Item $GIT_INSTALLER_FILE -Force -ErrorAction SilentlyContinue
        } catch {
            Log-Warn "Falha ao baixar/instalar Git. Continuando sem Git."
        }
    } else {
        Log-Info "Pulando instalação do Git."
    }
}
Write-Host ""

# ── Copiar arquivos do aplicativo ─────────────────────────────────────────────
Log-Step "Instalando Controle de Materiais LFB..."
Write-Host ""
try {
    New-Item -ItemType Directory -Path $INSTALL_DIR -Force | Out-Null

    $files = Get-ChildItem -Path $RELEASE_DIR -Recurse
    $total = $files.Count
    $done  = 0

    foreach ($f in $files) {
        $rel  = $f.FullName.Substring($RELEASE_DIR.Length + 1)
        $dest = Join-Path $INSTALL_DIR $rel
        if ($f.PSIsContainer) {
            New-Item -ItemType Directory -Path $dest -Force | Out-Null
        } else {
            Copy-Item -Path $f.FullName -Destination $dest -Force
        }
        $done++
        $pct = [int]($done / $total * 100)
        Write-Host "`r  [" -NoNewline -ForegroundColor DarkCyan
        $filled = [int]([Math]::Round($pct / 100 * 50))
        $empty  = 50 - $filled
        Write-Host ("█" * $filled) -NoNewline -ForegroundColor Green
        Write-Host ("░" * $empty)  -NoNewline -ForegroundColor DarkGray
        Write-Host "] " -NoNewline -ForegroundColor DarkCyan
        Write-Host "$pct% $rel" -NoNewline -ForegroundColor DarkGray
    }
    Write-Host ""
    Log-Ok "Arquivos copiados: $total itens."
} catch {
    Log-Error "Falha ao copiar arquivos: $_"
    Pause
    exit 1
}
Write-Host ""

# ── Atalho na Área de Trabalho ────────────────────────────────────────────────
$criarAtalho = Prompt-YesNo "Criar atalho na Área de Trabalho?"
if ($criarAtalho) {
    Log-Step "Criando atalho..."
    Animate-Progress "Criando atalho..." 500
    try {
        $wsh     = New-Object -ComObject WScript.Shell
        $link    = $wsh.CreateShortcut($SHORTCUT_PATH)
        $link.TargetPath       = Join-Path $INSTALL_DIR $APP_EXE
        $link.WorkingDirectory = $INSTALL_DIR
        $link.Description      = "Controle de Materiais LFB Reciclagem Eletrônica"
        # Ícone do próprio exe
        $link.IconLocation     = Join-Path $INSTALL_DIR $APP_EXE
        $link.Save()
        Log-Ok "Atalho criado em: $SHORTCUT_PATH"
    } catch {
        Log-Warn "Não foi possível criar o atalho: $_"
    }
} else {
    Log-Info "Atalho não criado."
}
Write-Host ""

# ── Concluído ─────────────────────────────────────────────────────────────────
Write-Host "  ╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "  ║                                                              ║" -ForegroundColor Green
Write-Host "  ║   [+] INSTALAÇÃO CONCLUÍDA COM SUCESSO!                     ║" -ForegroundColor Green
Write-Host "  ║                                                              ║" -ForegroundColor Green
Write-Host "  ║   Programa instalado em:                                     ║" -ForegroundColor DarkGreen
Write-Host "  ║   $($INSTALL_DIR.PadRight(58))║" -ForegroundColor DarkGreen
Write-Host "  ║                                                              ║" -ForegroundColor DarkGreen
Write-Host "  ╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "  Pressione qualquer tecla para sair..." -ForegroundColor DarkGray
$Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null
