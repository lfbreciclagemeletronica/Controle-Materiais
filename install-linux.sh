#!/usr/bin/env bash
# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║   LFB RECICLAGEM ELETRÔNICA — INSTALADOR LINUX                              ║
# ║   Controle de Materiais  |  install-linux.sh                                ║
# ╚══════════════════════════════════════════════════════════════════════════════╝
#
# Uso (duas formas):
#   1) A partir do ZIP de release (pasta release/linux-x64 presente ao lado):
#        chmod +x install-linux.sh && ./install-linux.sh
#
#   2) Download automático do GitHub (sem arquivos locais de release):
#        ./install-linux.sh --online
#
# Flags opcionais:
#   --online           Força download da release mais recente do GitHub
#   --install-dir <p>  Pasta de destino (padrão: ~/.local/share/ControleMateriais.LFB)
#   --no-shortcut      Pula criação de atalho sem perguntar
#   --uninstall        Remove a instalação existente

set -euo pipefail

# ── Configurações ─────────────────────────────────────────────────────────────
GITHUB_OWNER="lfbreciclagemeletronica"
GITHUB_REPO="Controle-Materiais"
ASSET_NAME="ControleMateriais-linux-x64.zip"
APP_NAME="Controle de Materiais LFB"
APP_EXE="ControleMateriais.Desktop"
APP_FOLDER="ControleMateriais.LFB"
BIN_LINK="/usr/local/bin/controle-materiais-lfb"
DESKTOP_DIR="$HOME/Desktop"
APPS_DIR="$HOME/.local/share/applications"
DESKTOP_FILE="$DESKTOP_DIR/ControleMateriais.desktop"
APPS_DESKTOP="$APPS_DIR/ControleMateriais.desktop"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RELEASE_DIR="$SCRIPT_DIR/release/linux-x64"

# ── Parse args ────────────────────────────────────────────────────────────────
USE_ONLINE=false
NO_SHORTCUT=false
UNINSTALL=false
CUSTOM_DIR=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --online)       USE_ONLINE=true ;;
        --no-shortcut)  NO_SHORTCUT=true ;;
        --uninstall)    UNINSTALL=true ;;
        --install-dir)  CUSTOM_DIR="$2"; shift ;;
        *) ;;
    esac
    shift
done

INSTALL_DIR="${CUSTOM_DIR:-$HOME/.local/share/$APP_FOLDER}"

# Se release local não existe, forçar online
[[ ! -f "$RELEASE_DIR/$APP_EXE" ]] && USE_ONLINE=true

# ── Cores ANSI ────────────────────────────────────────────────────────────────
R="\033[0m"
G="\033[0;32m"
DG="\033[0;32m"
C="\033[0;36m"
Y="\033[0;33m"
M="\033[0;35m"
RED="\033[0;31m"
W="\033[0;37m"
GR="\033[0;90m"
B="\033[1m"

# ── TUI ───────────────────────────────────────────────────────────────────────
show_header() {
    clear
    local mode; mode=$( [[ "$USE_ONLINE" == true ]] && echo "ONLINE" || echo "LOCAL" )
    echo -e "${G}"
    echo "  ██╗     ███████╗██████╗      ██████╗ ███████╗ ██████╗██╗██████╗  "
    echo "  ██║     ██╔════╝██╔══██╗     ██╔══██╗██╔════╝██╔════╝██║██╔══██╗ "
    echo "  ██║     █████╗  ██████╔╝     ██████╔╝█████╗  ██║     ██║██████╔╝ "
    echo -e "${DG}"
    echo "  ██║     ██╔══╝  ██╔══██╗     ██╔══██╗██╔══╝  ██║     ██║██╔══██╗ "
    echo "  ███████╗██║     ██████╔╝     ██║  ██║███████╗╚██████╗██║██║  ██║ "
    echo -e "${C}"
    echo "  ╚══════╝╚═╝     ╚═════╝      ╚═╝  ╚═╝╚══════╝ ╚═════╝╚═╝╚═╝  ╚═╝"
    echo ""
    printf "  ${C}╔══════════════════════════════════════════════════════════════════╗${R}\n"
    printf "  ${C}║   CONTROLE DE MATERIAIS  —  INSTALADOR  [LINUX-X64]  [%-6s]  ║${R}\n" "$mode"
    printf "  ${C}╠══════════════════════════════════════════════════════════════════╣${R}\n"
    printf "  ${GR}║   Destino : %-55s║${R}\n" "$INSTALL_DIR"
    printf "  ${C}╚══════════════════════════════════════════════════════════════════╝${R}\n"
    echo ""
}

log_info()  { echo -e "  ${C}[*]${R} $1"; }
log_ok()    { echo -e "  ${G}[+]${R} $1"; }
log_warn()  { echo -e "  ${Y}[!]${R} $1"; }
log_error() { echo -e "  ${RED}[X]${R} $1"; }
log_step()  { echo -e "  ${M}>>>${R} $1"; }

divider() {
    local title="${1:-}"
    echo ""
    if [[ -n "$title" ]]; then
        local pad=$(( 66 - ${#title} - 6 ))
        local dashes; dashes=$(printf '─%.0s' $(seq 1 $pad))
        echo -e "  ${GR}── $title $dashes${R}"
    else
        echo -e "  ${GR}$(printf '─%.0s' $(seq 1 68))${R}"
    fi
    echo ""
}

ask_yn() {
    local q="$1"
    echo ""
    echo -ne "  ${Y}[?]${R} $q ${W}[S/N]${R} "
    local ans
    read -r -n 1 ans
    echo ""
    [[ "$ans" =~ ^[SsYy]$ ]]
}

show_bar() {
    local pct="$1"
    local label="${2:-}"
    local width=48
    local filled=$(( pct * width / 100 ))
    local empty=$(( width - filled ))
    local bar_f bar_e
    bar_f=$(printf '█%.0s' $(seq 1 $filled) 2>/dev/null || true)
    bar_e=$(printf '░%.0s' $(seq 1 $empty)  2>/dev/null || true)
    local lbl="${label:0:36}"
    printf "\r  ${C}[${G}%s${GR}%s${C}]${R} ${W}%3d%%${GR} %s${R}" \
        "$bar_f" "$bar_e" "$pct" "$lbl"
}

anim_bar() {
    local label="${1:-Processando...}"
    local duration="${2:-0.6}"
    local steps=30
    local delay
    delay=$(awk "BEGIN{printf \"%.4f\", $duration/$steps}" 2>/dev/null || echo "0.02")
    for ((i=0; i<=steps; i++)); do
        local pct=$(( i * 100 / steps ))
        show_bar "$pct" "$label"
        sleep "$delay" 2>/dev/null || sleep 0.02
    done
    show_bar 100 "$label"
    echo ""
}

copy_with_progress() {
    local src="$1"
    local dst="$2"
    mkdir -p "$dst"
    local files=()
    while IFS= read -r -d '' f; do
        files+=("$f")
    done < <(find "$src" -type f -print0)
    local total=${#files[@]}
    local done_count=0
    for f in "${files[@]}"; do
        local rel="${f#$src/}"
        local dest_file="$dst/$rel"
        mkdir -p "$(dirname "$dest_file")"
        cp "$f" "$dest_file"
        done_count=$(( done_count + 1 ))
        local pct=$(( done_count * 100 / total ))
        show_bar "$pct" "$rel"
    done
    show_bar 100 "Concluído"
    echo ""
    log_ok "Arquivos copiados: $total itens."
}

extract_with_progress() {
    local zipfile="$1"
    local dst="$2"
    mkdir -p "$dst"
    if ! command -v unzip &>/dev/null; then
        log_warn "unzip não encontrado — extraindo com Python..."
        python3 -c "
import zipfile, os, sys
zf = zipfile.ZipFile('$zipfile')
names = zf.namelist()
total = len(names)
for i, name in enumerate(names, 1):
    zf.extract(name, '$dst')
    pct = int(i*100/total)
    print(f'\r  [{\"#\"*int(pct/2)}{\" \"*(50-int(pct/2))}] {pct}% {name[:36]}', end='', flush=True)
print()
zf.close()
"
    else
        local total
        total=$(unzip -l "$zipfile" | tail -1 | awk '{print $2}')
        [[ -z "$total" ]] && total=1
        local done_count=0
        while IFS= read -r line; do
            done_count=$(( done_count + 1 ))
            local pct=$(( done_count * 100 / total ))
            [[ $pct -gt 100 ]] && pct=100
            local name; name=$(echo "$line" | awk '{$1=$2=$3=$4=""; print $0}' | xargs)
            show_bar "$pct" "$name"
        done < <(unzip -o "$zipfile" -d "$dst" 2>&1 | grep "inflat\|extract" || true)
        show_bar 100 "Extração concluída"
        echo ""
    fi
    log_ok "Arquivos extraídos em: $dst"
}

download_file() {
    local url="$1"
    local dest="$2"
    if command -v curl &>/dev/null; then
        curl -L --progress-bar -o "$dest" "$url" 2>&1 | \
            while IFS= read -r line; do
                echo -ne "\r  ${C}[Baixando]${R} ${GR}$line${R}     "
            done
        echo ""
    elif command -v wget &>/dev/null; then
        wget -q --show-progress -O "$dest" "$url" 2>&1 | \
            while IFS= read -r line; do
                echo -ne "\r  ${C}[Baixando]${R} ${GR}$line${R}     "
            done
        echo ""
    else
        log_error "curl e wget não encontrados. Instale um deles e tente novamente."
        exit 1
    fi
}

get_latest_release() {
    log_step "Consultando GitHub Releases..."
    local api="https://api.github.com/repos/$GITHUB_OWNER/$GITHUB_REPO/releases/latest"
    local json
    if command -v curl &>/dev/null; then
        json=$(curl -s -H "User-Agent: LFB-Installer/2.0" "$api")
    elif command -v wget &>/dev/null; then
        json=$(wget -q -O - --header="User-Agent: LFB-Installer/2.0" "$api")
    else
        log_error "curl ou wget necessário para download online."; exit 1
    fi
    RELEASE_TAG=$(echo "$json" | grep '"tag_name"' | head -1 | sed 's/.*"tag_name": *"\([^"]*\)".*/\1/')
    RELEASE_URL=$(echo "$json" | grep '"browser_download_url"' | grep "$ASSET_NAME" | head -1 | \
        sed 's/.*"browser_download_url": *"\([^"]*\)".*/\1/')
    if [[ -z "$RELEASE_URL" ]]; then
        log_error "Asset '$ASSET_NAME' não encontrado na release '$RELEASE_TAG'."
        log_warn  "Verifique sua conexão com a internet."
        exit 1
    fi
    log_ok "Release encontrada: $RELEASE_TAG"
    echo -e "      ${GR}$RELEASE_URL${R}"
}

wait_key() {
    echo ""
    echo -e "  ${GR}Pressione Enter para sair...${R}"
    read -r
}

# ═════════════════════════════════════════════════════════════════════════════
# MAIN
# ═════════════════════════════════════════════════════════════════════════════
show_header

# ── Modo desinstalação ────────────────────────────────────────────────────────
if [[ "$UNINSTALL" == true ]]; then
    divider "DESINSTALAÇÃO"
    if [[ ! -d "$INSTALL_DIR" ]]; then
        log_warn "Nenhuma instalação encontrada em: $INSTALL_DIR"
        wait_key; exit 0
    fi
    log_warn "Será removido: $INSTALL_DIR"
    if ask_yn "Confirmar desinstalação?"; then
        echo ""
        anim_bar "Removendo arquivos..." 0.8
        rm -rf "$INSTALL_DIR"
        [[ -f "$DESKTOP_FILE" ]]  && rm -f "$DESKTOP_FILE"
        [[ -f "$APPS_DESKTOP" ]]  && rm -f "$APPS_DESKTOP"
        [[ -L "$BIN_LINK" ]]      && sudo rm -f "$BIN_LINK" 2>/dev/null || true
        log_ok "Desinstalado com sucesso."
    else
        log_info "Operação cancelada."
    fi
    wait_key; exit 0
fi

# ── Verificar instalação existente ────────────────────────────────────────────
if [[ -d "$INSTALL_DIR" ]]; then
    divider "INSTALAÇÃO EXISTENTE"
    log_warn "Versão instalada detectada em:"
    echo "      $INSTALL_DIR"
    if ! ask_yn "Deseja remover a versão existente e reinstalar?"; then
        echo ""
        log_info "Instalação cancelada."
        wait_key; exit 0
    fi
    echo ""
    log_step "Removendo instalação anterior..."
    anim_bar "Removendo arquivos antigos..." 0.7
    rm -rf "$INSTALL_DIR"
    [[ -f "$DESKTOP_FILE" ]] && rm -f "$DESKTOP_FILE"
    [[ -f "$APPS_DESKTOP" ]] && rm -f "$APPS_DESKTOP"
    [[ -L "$BIN_LINK" ]]     && sudo rm -f "$BIN_LINK" 2>/dev/null || true
    log_ok "Versão anterior removida."
fi

# ── Verificar / Instalar Git ───────────────────────────────────────────────────
divider "GIT"
log_step "Verificando Git..."
if command -v git &>/dev/null; then
    log_ok "Git encontrado: $(git --version)"
else
    log_warn "Git não encontrado no sistema."
    if ask_yn "Deseja instalar o Git agora?"; then
        echo ""
        log_step "Instalando Git..."
        if command -v apt-get &>/dev/null; then
            anim_bar "Atualizando repositórios..." 1.0
            sudo apt-get update -qq
            anim_bar "Instalando git via apt..." 2.0
            sudo apt-get install -y git -qq
        elif command -v dnf &>/dev/null; then
            anim_bar "Instalando git via dnf..." 2.0
            sudo dnf install -y git -q
        elif command -v pacman &>/dev/null; then
            anim_bar "Instalando git via pacman..." 2.0
            sudo pacman -S --noconfirm git
        elif command -v zypper &>/dev/null; then
            anim_bar "Instalando git via zypper..." 2.0
            sudo zypper install -y git
        else
            log_warn "Gerenciador de pacotes não reconhecido. Instale o Git manualmente."
        fi
        if command -v git &>/dev/null; then
            log_ok "Git instalado: $(git --version)"
        else
            log_warn "Não foi possível confirmar instalação do Git. Continuando..."
        fi
    else
        log_info "Pulando instalação do Git."
    fi
fi

# ── Obter arquivos do aplicativo ───────────────────────────────────────────────
divider "DOWNLOAD / ARQUIVOS"

if [[ "$USE_ONLINE" == true ]]; then
    get_latest_release
    ZIP_PATH="/tmp/$ASSET_NAME"
    echo ""
    log_step "Baixando $ASSET_NAME..."
    download_file "$RELEASE_URL" "$ZIP_PATH"
    log_ok "Download concluído."
    echo ""
    log_step "Extraindo arquivos..."
    extract_with_progress "$ZIP_PATH" "$INSTALL_DIR"
    rm -f "$ZIP_PATH"
    TAG_LABEL="$RELEASE_TAG"
else
    log_info "Usando arquivos locais de: $RELEASE_DIR"
    echo ""
    log_step "Copiando arquivos..."
    copy_with_progress "$RELEASE_DIR" "$INSTALL_DIR"
    TAG_LABEL="(local)"
fi

chmod +x "$INSTALL_DIR/$APP_EXE"

# ── Symlink no PATH ───────────────────────────────────────────────────────────
divider "SYMLINK"
if ask_yn "Criar link simbólico em /usr/local/bin (requer sudo)?"; then
    log_step "Criando symlink..."
    anim_bar "Configurando PATH..." 0.5
    sudo ln -sf "$INSTALL_DIR/$APP_EXE" "$BIN_LINK"
    log_ok "Symlink criado: $BIN_LINK"
else
    log_info "Symlink não criado. Execute diretamente de: $INSTALL_DIR/$APP_EXE"
fi

# ── Atalho na Área de Trabalho e Menu de Aplicações ──────────────────────────
divider "ATALHO"
if [[ "$NO_SHORTCUT" == false ]] && ask_yn "Criar atalho na Área de Trabalho?"; then
    log_step "Criando atalhos..."
    anim_bar "Configurando atalho..." 0.4
    mkdir -p "$DESKTOP_DIR" "$APPS_DIR"
    DESKTOP_CONTENT="[Desktop Entry]
Version=1.0
Type=Application
Name=Controle de Materiais LFB
Comment=LFB Reciclagem Eletrônica — Controle de Materiais
Exec=$INSTALL_DIR/$APP_EXE
Icon=$INSTALL_DIR/Assets/lfb-logo.png
Terminal=false
Categories=Office;Finance;
StartupWMClass=ControleMateriais
Keywords=reciclagem;materiais;lfb;"
    echo "$DESKTOP_CONTENT" > "$DESKTOP_FILE"
    echo "$DESKTOP_CONTENT" > "$APPS_DESKTOP"
    chmod +x "$DESKTOP_FILE"
    command -v update-desktop-database &>/dev/null && \
        update-desktop-database "$APPS_DIR" 2>/dev/null || true
    log_ok "Atalho criado em: $DESKTOP_FILE"
    log_ok "Entrada de aplicação: $APPS_DESKTOP"
else
    log_info "Atalho não criado."
fi

# ── Concluído ─────────────────────────────────────────────────────────────────
echo ""
printf "${G}"
echo "  ╔══════════════════════════════════════════════════════════════════╗"
echo "  ║                                                                  ║"
echo "  ║   [+] INSTALAÇÃO CONCLUÍDA COM SUCESSO!                         ║"
echo "  ║                                                                  ║"
printf "${DG}"
printf "  ║   Versão  : %-52s║\n" "$TAG_LABEL"
printf "  ║   Local   : %-52s║\n" "$INSTALL_DIR"
echo "  ║                                                                  ║"
echo "  ╚══════════════════════════════════════════════════════════════════╝"
printf "${R}"
wait_key
