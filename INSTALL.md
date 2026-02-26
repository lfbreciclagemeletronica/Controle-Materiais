# Instalação — Controle de Materiais LFB

## Windows

### Opção A — Instalação online (sem precisar baixar o app separadamente)

Execute diretamente no PowerShell — o script baixa a release mais recente do GitHub automaticamente:

```powershell
powershell -ExecutionPolicy Bypass -File install-windows.ps1 -Online
```

### Opção B — Instalação local (após rodar `publish.ps1`)

```powershell
powershell -ExecutionPolicy Bypass -File install-windows.ps1
```

O instalador Windows irá:
- Verificar se já existe uma versão instalada e perguntar se deseja substituir
- Verificar se o Git está instalado (oferece instalação automática)
- Baixar/copiar os arquivos para `%LOCALAPPDATA%\ControleMateriais.LFB\`
- Criar atalho na Área de Trabalho (opcional)
- Criar atalho no Menu Iniciar automaticamente

**Parâmetros disponíveis:**

| Parâmetro | Descrição |
|---|---|
| `-Online` | Força download da release mais recente do GitHub |
| `-InstallDir <path>` | Pasta de destino personalizada |
| `-NoShortcut` | Pula criação de atalho na Área de Trabalho |
| `-Uninstall` | Remove a instalação existente |

---

## Linux

### Opção A — Instalação online

```bash
chmod +x install-linux.sh && ./install-linux.sh --online
```

### Opção B — Instalação local (após rodar `publish.ps1`)

```bash
chmod +x install-linux.sh && ./install-linux.sh
```

O instalador Linux irá:
- Verificar instalação existente (oferece remoção)
- Instalar Git via `apt` / `dnf` / `pacman` / `zypper` se ausente
- Copiar para `~/.local/share/ControleMateriais.LFB/`
- Criar symlink em `/usr/local/bin/controle-materiais-lfb` (opcional, requer sudo)
- Criar atalho `.desktop` na Área de Trabalho e no menu de aplicações (opcional)

**Flags disponíveis:**

| Flag | Descrição |
|---|---|
| `--online` | Força download da release mais recente do GitHub |
| `--install-dir <path>` | Pasta de destino personalizada |
| `--no-shortcut` | Pula criação de atalho sem perguntar |
| `--uninstall` | Remove a instalação existente |

---

## Gerar builds para distribuição

```powershell
.\publish.ps1
```

Cria em `release/`:
- `win-x64/` + `ControleMateriais-win-x64.zip`
- `linux-x64/` + `ControleMateriais-linux-x64.zip`

Flags: `-Target win`, `-Target linux`, `-SkipZip`
