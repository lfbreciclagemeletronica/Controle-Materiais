# Release 1 — LFB Controle de Materiais

## Visão Geral
Versão estável do sistema de controle de materiais para reciclagem eletrônica, com interface moderna em Avalonia UI, geração de PDFs, importação/exportação de dados e suporte multiplataforma (Windows/Linux).

---

## ✨ Features Implementadas

### 🧮 Gestão de Pesagem e Valores
- Cadastro e edição de 52 materiais (catalogo fixo)
- Pesagem por kg com cálculo automático de totais
- Valores mensais persistidos em JSON (`Downloads/ControleMateriaisLFB/TabelaPrecos/`)
- Interface MVVM com `ItemViewModel` e `MainWindowViewModel`

### 📄 Geração de PDFs
- **Recibo de Pesagem**: Layout profissional com cabeçalho LFB (logo, CNPJ, IE, endereço), grade de informações (fornecedor/peso/valor/data) e tabela de itens (kg/valor/total).
- **Lista de Preços**: Exportação compacta em uma página, com logo LFB, título centralizado, subtítulo (nome da tabela) e tabela centralizada.
- **Tecnologia**: QuestPDF com fontes Arial, margens otimizadas e colunas ajustadas para legibilidade.

### 📥 Importação de Preços via PDF
- Parser inteligente usando iText7 para extrair linhas de PDFs de listas de preço.
- Normalização de espaços (`"6 5,00"` → `"65,00"`) e mapeamento de aliases para compatibilidade com o catálogo.
- Debug opcional (`pdf_linhas_debug.txt` no Desktop) para auditoria.

### 🗂️ Gestão de Tabelas de Preço
- Criação, edição, exclusão e renomeação de tabelas de preço.
- Persistência em JSON (`Downloads/ControleMateriaisLFB/TabelaPrecos/`).
- Interface condicional: mostra apenas lista e botão “Nova Tabela” até selecionar/criar.
- Exportação da lista de preços em PDF com logo e layout compacto.

### 📁 Estrutura de Diretórios Centralizada
- Criado automaticamente no primeiro uso:
  - `~/Downloads/ControleMateriaisLFB/TabelaPrecos/` — JSONs e PDFs de tabelas
  - `~/Downloads/ControleMateriaisLFB/Recibos/` — PDFs de recibos
- Garantido via `EnsureDirectories()` no startup.

### 🎨 Interface do Usuário
- Design limpo com tema Fluent do Avalonia.
- Janela redimensionável (900x680 padrão, mínimo 700x500).
- Notificações toast para feedback de ações.
- Botões de ação agrupados e visibilidade condicional.

### 🖼️ Ícone e Identidade Visual
- Ícone LFB gerado a partir do logo (`Assets/icon.ico`) em múltiplos tamanhos (16,32,48,64,128,256).
- Aplicado ao executável (`ApplicationIcon`) e à janela (`Icon`).
- Logo LFB incorporado nos PDFs (cabeçalho do recibo e lista de preços).

### 🚀 Build e Distribuição
- **Executável único portátil**: self-contained, single-file (sem instalação).
- **Multiplataforma**: Windows x64 e Linux x64 via `publish.ps1`.
- **Scripts**: `publish.ps1` (build), `install-windows.ps1`, `install-linux.sh`, `start-linux.sh`.
- **Sem macOS**: Removido do pipeline conforme solicitado.

---

## 🔧 Detalhes Técnicos

### Linguagem e Framework
- **.NET 10** (net10.0)
- **Avalonia UI 11.3.12** (MVVM, DataGrid, Fluent Theme)
- **CommunityToolkit.Mvvm 8.2.1** para ViewModels

### Bibliotecas Principais
- `QuestPDF 2024.12.3` — Geração de PDFs
- `iText7 8.0.5` — Parser de PDFs
- `supabase-csharp 0.16.2` — Integração (opcional)
- `Newtonsoft.Json 13.0.3` — Serialização

### Segurança
- Vulnerabilidades `Microsoft.IdentityModel.JsonWebTokens` e `System.IdentityModel.Tokens.Jwt` corrigidas (pinned 7.5.1).

### Assets
- `Assets/lfb-logo.png` — Logo para PDFs
- `Assets/icon.ico` — Ícone do aplicativo (gerado via PowerShell)

---

## 📂 Estrutura de Arquivos Relevantes

```
ControleMateriais.Desktop/
├── ViewModels/
│   ├── MainWindowViewModel.cs      — Lógica principal, recibo, diretórios
│   └── PriceTableManagerViewModel.cs — Tabelas de preço, PDF lista, importação
├── Views/
│   └── MainWindow.axaml             — UI principal
├── Models/
│   ├── Item.cs                      — Modelo de material
│   └── ItemCatalog.cs               — Catálogo fixo (52 itens)
├── Assets/
│   ├── lfb-logo.png
│   └── icon.ico
├── Converters/
│   └── BooleanNegationConverter.cs
└── ControleMateriais.Desktop.csproj
```

---

## 🛠️ Scripts de Build e Deploy

### publish.ps1
```powershell
# Gera executáveis portáteis para Windows e/ou Linux
# Uso: .\publish.ps1 -Target all/win/linux -OutDir release
```

### generate-icon.ps1
```powershell
# Converte lfb-logo.png em icon.ico com múltiplos tamanhos
# Usa PowerShell + System.Drawing (sem ImageMagick)
```

---

## 📋 Próximos Passos (Roadmap)

- [ ] Integração com Supabase (opcional)
- [ ] Relatórios periódicos
- [ ] Backup/restore automático
- [ ] Atualização automática de preços via API

---

## 📦 Como Usar

1. **Executar**: `dotnet run` (Debug) ou use o executável gerado via `publish.ps1`.
2. **Importar preços**: PDF → via “Importar de PDF” na tela de tabelas.
3. **Gerar recibo**: Pese os materiais → “Exportar Recibo”.
4. **Exportar lista**: Selecione tabela → “Exportar Tabela”.

---

**Versão**: Release 1  
**Data**: 2026-03-02  
**Responsável**: Cascade (AI Pair Programmer)  
**Repositório**: `lfbreciclagemeletronica/Controle-Materiais`
