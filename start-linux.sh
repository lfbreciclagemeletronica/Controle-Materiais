#!/usr/bin/env bash
# start-linux.sh — Inicia o Controle de Materiais LFB no Linux
#
# Uso:
#   chmod +x start-linux.sh && ./start-linux.sh
#
# O script localiza o executavel nas seguintes ordens de prioridade:
#   1) Pasta de instalacao padrao: ~/.local/share/ControleMateriais.LFB/
#   2) Pasta de build local:       release/linux-x64/  (apos .\publish.ps1 -Target linux)
#   3) Mesmo diretorio do script

set -euo pipefail

APP_EXE="ControleMateriais.Desktop"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

R="\033[0m"
G="\033[0;32m"
C="\033[0;36m"
Y="\033[0;33m"
RED="\033[0;31m"
GR="\033[0;90m"

echo -e "${C}"
echo "  ╔══════════════════════════════════════════════════════════════════╗"
echo "  ║   LFB RECICLAGEM ELETRÔNICA — CONTROLE DE MATERIAIS             ║"
echo "  ╚══════════════════════════════════════════════════════════════════╝"
echo -e "${R}"

CANDIDATES=(
    "$HOME/.local/share/ControleMateriais.LFB/$APP_EXE"
    "$SCRIPT_DIR/release/linux-x64/$APP_EXE"
    "$SCRIPT_DIR/$APP_EXE"
)

EXE_PATH=""
for candidate in "${CANDIDATES[@]}"; do
    if [[ -f "$candidate" ]]; then
        EXE_PATH="$candidate"
        break
    fi
done

if [[ -z "$EXE_PATH" ]]; then
    echo -e "  ${RED}[X]${R} Executavel nao encontrado. Verifique uma das opcoes:"
    echo ""
    echo -e "  ${Y}  1)${R} Execute o instalador:  ${C}chmod +x install-linux.sh && ./install-linux.sh${R}"
    echo -e "  ${Y}  2)${R} Publique o projeto:    ${C}.\publish.ps1 -Target linux${R}  (no Windows)"
    echo ""
    exit 1
fi

if [[ ! -x "$EXE_PATH" ]]; then
    chmod +x "$EXE_PATH"
fi

EXE_DIR="$(dirname "$EXE_PATH")"
echo -e "  ${G}[+]${R} Iniciando: ${GR}$EXE_PATH${R}"
echo ""

cd "$EXE_DIR"
exec "$EXE_PATH" "$@"
