#!/usr/bin/env bash
# ╔══════════════════════════════════════════════════════════════════╗
# ║   LFB RECICLAGEM ELETRÔNICA — INSTALADOR LINUX                  ║
# ║   Controle de Materiais                                          ║
# ╚══════════════════════════════════════════════════════════════════╝
# Uso: chmod +x install-linux.sh && ./install-linux.sh

set -euo pipefail

# ── Configurações ─────────────────────────────────────────────────────────────
APP_NAME="Controle de Materiais LFB"
APP_EXE="ControleMateriais.Desktop"
APP_FOLDER="ControleMateriais.LFB"
INSTALL_DIR="$HOME/.local/share/$APP_FOLDER"
BIN_LINK="/usr/local/bin/controle-materiais-lfb"
DESKTOP_DIR="$HOME/Desktop"
DESKTOP_FILE="$DESKTOP_DIR/ControleMateriais.desktop"
APPS_DIR="$HOME/.local/share/applications"
APPS_DESKTOP="$APPS_DIR/ControleMateriais.desktop"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RELEASE_DIR="$SCRIPT_DIR/release/linux-x64"

# ── Cores ANSI ────────────────────────────────────────────────────────────────
C_RESET="\033[0m"
C_GREEN="\033[0;32m"
C_DGREEN="\033[0;32m"
C_CYAN="\033[0;36m"
C_DCYAN="\033[0;36m"
C_YELLOW="\033[0;33m"
C_MAGENTA="\033[0;35m"
C_RED="\033[0;31m"
C_WHITE="\033[0;37m"
C_DGRAY="\033[0;90m"
C_BOLD="\033[1m"

# ── Funções TUI ───────────────────────────────────────────────────────────────
header() {
    clear
    echo -e "${C_GREEN}"
    echo "  ██╗     ███████╗██████╗      ██████╗ ███████╗ ██████╗██╗██████╗  "
    echo "  ██║     ██╔════╝██╔══██╗     ██╔══██╗██╔════╝██╔════╝██║██╔══██╗ "
    echo "  ██║     █████╗  ██████╔╝     ██████╔╝█████╗  ██║     ██║██████╔╝ "
    echo "  ██║     ██╔══╝  ██╔══██╗     ██╔══██╗██╔══╝  ██║     ██║██╔══██╗ "
    echo "  ███████╗██║     ██████╔╝     ██║  ██║███████╗╚██████╗██║██║  ██║ "
    echo "  ╚══════╝╚═╝     ╚═════╝      ╚═╝  ╚═╝╚══════╝ ╚═════╝╚═╝╚═╝  ╚═╝"
    echo -e "${C_DCYAN}"
    echo "  ╔══════════════════════════════════════════════════════════════╗"
    echo "  ║    CONTROLE DE MATERIAIS — INSTALADOR v1.0  [LINUX-X64]     ║"
    echo "  ╚══════════════════════════════════════════════════════════════╝"
    echo -e "${C_RESET}"
}

log_info()  { echo -e "  ${C_CYAN}[*]${C_RESET} $1"; }
log_ok()    { echo -e "  ${C_GREEN}[+]${C_RESET} $1"; }
log_warn()  { echo -e "  ${C_YELLOW}[!]${C_RESET} $1"; }
log_error() { echo -e "  ${C_RED}[X]${C_RESET} $1"; }
log_step()  { echo -e "  ${C_MAGENTA}>>>${C_RESET} $1"; }

prompt_yn() {
    local question="$1"
    echo ""
    echo -ne "  ${C_YELLOW}[?]${C_RESET} $question ${C_WHITE}[S/N]${C_RESET} "
    read -r -n 1 answer
    echo ""
    [[ "$answer" =~ ^[SsYy]$ ]]
}

progress_bar() {
    local label="${1:-Processando...}"
    local duration="${2:-1}"
    local width=50
    local steps=40
    local delay
    delay=$(echo "scale=4; $duration / $steps" | bc 2>/dev/null || echo "0.025")

    for ((i=0; i<=steps; i++)); do
        local pct=$(( i * 100 / steps ))
        local filled=$(( i * width / steps ))
        local empty=$(( width - filled ))
        local bar=""
        for ((f=0; f<filled; f++)); do bar+="█"; done
        for ((e=0; e<empty;  e++)); do bar+="░"; done
        printf "\r  ${C_DCYAN}[${C_GREEN}%s${C_DGRAY}%s${C_DCYAN}]${C_RESET} ${C_WHITE}%3d%%${C_DGRAY} %s${C_RESET}" \
            "${bar:0:$filled}" "${bar:$filled}" "$pct" "$label"
        sleep "$delay" 2>/dev/null || sleep 0.05
    done
    echo ""
}

copy_with_progress() {
    local src="$1"
    local dst="$2"

    mkdir -p "$dst"
    local files
    mapfile -t files < <(find "$src" -type f)
    local total=${#files[@]}
    local done_count=0
    local width=50

    for f in "${files[@]}"; do
        local rel="${f#$src/}"
        local dest_file="$dst/$rel"
        mkdir -p "$(dirname "$dest_file")"
        cp "$f" "$dest_file"
        done_count=$(( done_count + 1 ))
        local pct=$(( done_count * 100 / total ))
        local filled=$(( done_count * width / total ))
        local empty=$(( width - filled ))
        local bar=""
        for ((ff=0; ff<filled; ff++)); do bar+="█"; done
        for ((ee=0; ee<empty;  ee++)); do bar+="░"; done
        printf "\r  ${C_DCYAN}[${C_GREEN}%s${C_DGRAY}%s${C_DCYAN}]${C_RESET} ${C_WHITE}%3d%%${C_DGRAY} %s${C_RESET}" \
            "${bar:0:$filled}" "${bar:$filled}" "$pct" "$rel"
    done
    echo ""
    log_ok "Arquivos copiados: $total itens."
}

# ═════════════════════════════════════════════════════════════════════════════
header

# ── Verificar fonte do instalador ─────────────────────────────────────────────
log_step "Verificando arquivos de instalação..."
if [[ ! -d "$RELEASE_DIR" ]]; then
    log_error "Pasta release/linux-x64 não encontrada!"
    log_warn  "Execute primeiro: ./publish.ps1  (ou  dotnet publish ... -r linux-x64)"
    echo ""
    exit 1
fi
if [[ ! -f "$RELEASE_DIR/$APP_EXE" ]]; then
    log_error "Executável '$APP_EXE' não encontrado em $RELEASE_DIR"
    echo ""
    exit 1
fi
log_ok "Arquivos de instalação encontrados."
echo ""

# ── Verificar instalação existente ────────────────────────────────────────────
if [[ -d "$INSTALL_DIR" ]]; then
    log_warn "Instalação existente detectada em:"
    echo "  $INSTALL_DIR"
    echo ""
    if prompt_yn "Deseja remover a versão existente e instalar a mais nova?"; then
        echo ""
        log_step "Removendo versão anterior..."
        progress_bar "Removendo arquivos antigos..." 0.8
        rm -rf "$INSTALL_DIR"
        [[ -f "$DESKTOP_FILE" ]] && rm -f "$DESKTOP_FILE"
        [[ -f "$APPS_DESKTOP" ]] && rm -f "$APPS_DESKTOP"
        [[ -L "$BIN_LINK" ]]     && sudo rm -f "$BIN_LINK" 2>/dev/null || true
        log_ok "Versão anterior removida com sucesso."
    else
        log_info "Instalação cancelada pelo usuário."
        echo ""
        exit 0
    fi
    echo ""
fi

# ── Verificar / Instalar Git ───────────────────────────────────────────────────
log_step "Verificando Git..."
if command -v git &>/dev/null; then
    GIT_VER=$(git --version)
    log_ok "Git já instalado: $GIT_VER"
else
    log_warn "Git não encontrado no sistema."
    if prompt_yn "Deseja instalar o Git agora?"; then
        echo ""
        log_step "Instalando Git..."
        if command -v apt-get &>/dev/null; then
            progress_bar "Atualizando repositórios..." 1
            sudo apt-get update -qq
            progress_bar "Instalando git via apt..." 2
            sudo apt-get install -y git -qq
        elif command -v dnf &>/dev/null; then
            progress_bar "Instalando git via dnf..." 2
            sudo dnf install -y git -q
        elif command -v pacman &>/dev/null; then
            progress_bar "Instalando git via pacman..." 2
            sudo pacman -S --noconfirm git
        elif command -v zypper &>/dev/null; then
            progress_bar "Instalando git via zypper..." 2
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
echo ""

# ── Copiar arquivos do aplicativo ─────────────────────────────────────────────
log_step "Instalando $APP_NAME..."
echo ""
copy_with_progress "$RELEASE_DIR" "$INSTALL_DIR"

# Garantir que o executável tenha permissão de execução
chmod +x "$INSTALL_DIR/$APP_EXE"
echo ""

# ── Symlink no PATH ───────────────────────────────────────────────────────────
if prompt_yn "Criar link simbólico em /usr/local/bin (requer sudo)?"; then
    log_step "Criando symlink..."
    progress_bar "Configurando PATH..." 0.5
    sudo ln -sf "$INSTALL_DIR/$APP_EXE" "$BIN_LINK"
    log_ok "Symlink criado: $BIN_LINK"
else
    log_info "Symlink não criado. Execute diretamente de: $INSTALL_DIR/$APP_EXE"
fi
echo ""

# ── Atalho na Área de Trabalho ────────────────────────────────────────────────
if prompt_yn "Criar atalho na Área de Trabalho?"; then
    log_step "Criando atalho..."
    progress_bar "Criando atalho..." 0.5

    mkdir -p "$DESKTOP_DIR"
    mkdir -p "$APPS_DIR"

    DESKTOP_CONTENT="[Desktop Entry]
Version=1.0
Type=Application
Name=Controle de Materiais LFB
Comment=LFB Reciclagem Eletrônica — Controle de Materiais
Exec=$INSTALL_DIR/$APP_EXE
Icon=$INSTALL_DIR/Assets/lfb-logo.png
Terminal=false
Categories=Office;Finance;
StartupWMClass=ControleMateriais"

    echo "$DESKTOP_CONTENT" > "$DESKTOP_FILE"
    echo "$DESKTOP_CONTENT" > "$APPS_DESKTOP"
    chmod +x "$DESKTOP_FILE"

    # Atualizar banco de aplicações do desktop
    command -v update-desktop-database &>/dev/null && \
        update-desktop-database "$APPS_DIR" 2>/dev/null || true

    log_ok "Atalho criado em: $DESKTOP_FILE"
else
    log_info "Atalho não criado."
fi
echo ""

# ── Concluído ─────────────────────────────────────────────────────────────────
echo -e "${C_GREEN}"
echo "  ╔══════════════════════════════════════════════════════════════╗"
echo "  ║                                                              ║"
echo "  ║   [+] INSTALAÇÃO CONCLUÍDA COM SUCESSO!                     ║"
echo "  ║                                                              ║"
echo -e "  ║   Programa instalado em:${C_RESET}                                     ${C_GREEN}║"
echo "  ║   $INSTALL_DIR"
echo "  ║                                                              ║"
echo "  ╚══════════════════════════════════════════════════════════════╝"
echo -e "${C_RESET}"
echo -e "  ${C_DGRAY}Pressione Enter para sair...${C_RESET}"
read -r
