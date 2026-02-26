# Instalação — Controle de Materiais LFB

## Opção 1 — Instalador automático (recomendado)

Baixe o `LFB-Installer.exe` na [página de releases](https://github.com/lfbreciclagemeletronica/Controle-Materiais/releases/latest) e execute como **Administrador**.

O instalador irá **automaticamente**:
- Verificar se já existe uma versão instalada e perguntar se deseja substituir
- Verificar se o Git está instalado (oferece instalação automática)
- Baixar a versão mais recente direto do GitHub
- Instalar em `%LOCALAPPDATA%\ControleMateriais.LFB\`
- Perguntar se deseja criar atalho na Área de Trabalho

> Não é necessário baixar o app separadamente — o instalador faz tudo.

---

## Opção 2 — Build e instalação manual

### Pré-requisito: gerar o executável

```powershell
.\publish.ps1
```

Isso cria em `release/`:
- `win-x64/` — binário Windows
- `linux-x64/` — binário Linux
- `installer/LFB-Installer.exe` — instalador Windows

### Windows (manual)

```powershell
powershell -ExecutionPolicy Bypass -File install-windows.ps1
```

### Linux

```bash
chmod +x install-linux.sh
./install-linux.sh
```

O instalador Linux irá:
- Verificar instalação existente (oferece remoção)
- Instalar Git via `apt` / `dnf` / `pacman` / `zypper` se ausente
- Copiar para `~/.local/share/ControleMateriais.LFB/`
- Criar symlink em `/usr/local/bin/` (opcional)
- Criar atalho `.desktop` na Área de Trabalho (opcional)
