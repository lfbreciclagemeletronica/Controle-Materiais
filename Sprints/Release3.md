# Release 3 — LFB Controle de Materiais

## Visão Geral
Maior release do projeto até o momento. Introduz uma arquitetura multi-tela com tela inicial (Home), um sistema completo de pesagem de materiais com envio ao GitHub, e um sistema de visualização e sincronização de pesagens e recibos. O fluxo anterior (direto para a tela de recibos) foi mantido como "Sistema de Recibos" e a nova rota "Sistema de Pesos" foi adicionada lado a lado. A integração com GitHub foi reestruturada em um serviço centralizado (`GitHubService`) que gerencia três repositórios independentes: `Pesagens`, `Recibos` e `TabelaPrecos`.

---

## ✨ Features Implementadas

### 🏠 Tela Inicial (HomeView)
- Adicionada uma **tela Home** como ponto de entrada do aplicativo, exibindo o logo LFB e dois botões de navegação em cartão:
  - **Sistema de Recibos** — acessa o fluxo já existente (tabela de preços + geração de recibo PDF).
  - **Sistema de Pesos** — acessa o novo módulo de pesagem de materiais.
- Badge dinâmico no canto superior direito indica se o GitHub está **configurado** (verde ✔) ou **não configurado** (vermelho ⚠).
- Botão **"⚙ Configurar GitHub"** abre o dialog de credenciais a partir de qualquer tela.
- Navegação bidirecional: cada sub-tela possui botão "← Início" para retornar ao Home.

---

### ⚖️ Sistema de Pesos (`WeightCalculatorView` / `WeightCalculatorViewModel`)
- Nova tela dedicada exclusivamente ao **registro de pesagem de materiais**.
- Lista todos os itens do catálogo (`ItemCatalog.OrderedItems`) com campo de peso editável por item.
- Campo **Nome do Cliente** para identificar a pesagem.
- **Peso Total** calculado automaticamente ao editar qualquer campo.
- Botão **Limpar** zera todos os pesos e o nome do cliente.
- Botão **Salvar e Enviar**: serializa os dados em JSON com `StatusPesagem: "pendente"` e envia via `GitHubService.EnviarArquivoAsync` para o repositório `Pesagens` no GitHub.
- Nome do arquivo gerado: `{NomeCliente}_{dd-MM-yyyy}.json`.
- Se as credenciais do GitHub ainda não estiverem configuradas, abre automaticamente o `GitHubConfigDialog` antes de prosseguir.
- Feedback de progresso em tempo real na tela (clone, pull, commit, push).
- Após envio bem-sucedido, exibe `EnvioSucessoDialog` com confirmação.
- `WeightItemWrapper` com suporte a `IsSelected`, `IniciarEdicao()`, `CancelarEdicao()`, `ConfirmarEdicao()` e `Resetar()`.

---

### 📋 Sistema de Pesagens e Recibos (`PesagensView` / `PesagensViewModel`)
Nova tela com **TabControl de duas abas** acessível pelo "Sistema de Recibos":

#### Aba "Pesagens"
- Lista todas as pesagens do repositório `Pesagens` clonado localmente, exibindo: **Cliente**, **Data** (`dd/MM/yyyy`) e **Status** com cores dinâmicas.
- **Deduplicação automática**: para cada cliente, exibe apenas a pesagem mais recente.
- Filtros por status via **ToggleButton**: Todos / pendente / concluido / falhou. Apenas um filtro ativo por vez (binding via `StringEqualsConverter`).
- Contadores dinâmicos nos botões de filtro: ex. `"pendente (3)"`.
- Botão **"⟳ Sincronizar"**: clona o repo ou faz pull, renomeia JSONs concluídos com sufixo `_concluido`, faz commit individual por pesagem e push único ao final.
- Clicar em uma linha de pesagem com `StatusPesagem == "pendente"` abre a `ReciboFromPesagemView`.
- Indicador de **última sincronização** (`dd/MM/yyyy HH:mm`) exibido abaixo do status.
- Mensagem de estado vazio quando o repo não está clonado localmente.

#### Aba "Recibos"
- Lista todos os **PDFs de recibos** do repositório `Recibos` clonado localmente.
- Exibe: nome do cliente (extraído do nome de arquivo) e data (`dd/MM/yyyy`).
- Botão **"📄 Abrir"** por linha: abre o PDF com o aplicativo padrão do sistema operacional.
- Botão **"⟳ Sincronizar"**: pull do repositório remoto + push de PDFs locais ainda não commitados.
- Indicador de **última sincronização** por aba, independente da aba Pesagens.
- Sincronização automática ao abrir cada aba (uma vez por sessão).
- Mensagem de estado vazio quando o repo de Recibos não está clonado localmente.

#### Botão "+ Novo"
- Disponível no cabeçalho da tela, permite criar um recibo do zero (sem pesagem) abrindo diretamente a `ReciboView`.

---

### 🧾 Recibo a partir de Pesagem (`ReciboFromPesagemView` / `ReciboFromPesagemViewModel`)
- Nova tela que recebe uma `PesagemItem` e exibe os materiais pesados com seu respectivo peso.
- **Seletor de tabela de preços** (`ComboBox`): carrega automaticamente as tabelas disponíveis em `TabelaPrecos/` e aplica os preços por kg aos itens.
- Calcula **Total por item** (`Peso × Preço`) e exibe **Peso Total** e **Valor Total** da pesagem.
- Botão **Exportar Recibo PDF**: gera o PDF com QuestPDF usando o mesmo layout profissional do sistema de recibos existente (cabeçalho LFB, tabela MATERIAL/KG/VALOR KG/TOTAL).
  - Nome do arquivo: `{NomeCliente}_{dd-MM-yyyy}.pdf`.
  - Abre `SaveFilePicker` com pasta padrão em `Recibos/`.
- Após exportar, chama `MarcarConcluido()`: atualiza o JSON da pesagem com `StatusPesagem: "concluido"`, `DataConclusao` e `NomeRecibo`, e faz commit + push automático ao repositório `Pesagens`.
- Feedback de status em tempo real (verde/vermelho).

---

### 🔧 GitHubService — Serviço Centralizado de Git/GitHub
Novo arquivo `Services/GitHubService.cs` centraliza toda a lógica de integração com Git e GitHub:

| Método | Descrição |
|--------|-----------|
| `EnviarArquivoAsync` | Clone/pull do repo `Pesagens`, escreve JSON, commit e push |
| `GarantirRecibosRepoAsync` | Clona repo `Recibos`; migra PDFs legados (sem `.git`) automaticamente |
| `SincronizarRecibosAsync` | Pull + push de PDFs locais não commitados no repo `Recibos` |
| `GarantirTabelaPrecosRepoAsync` | Clona repo `TabelaPrecos`; migra JSONs legados automaticamente |
| `SincronizarTabelaPrecosAsync` | Pull + push de JSONs locais no repo `TabelaPrecos` |
| `PublicarReciboAsync` | Commit e push de um PDF específico no repo `Recibos` |
| `PublicarTabelaAsync` | Commit e push de um JSON de tabela específico no repo `TabelaPrecos` |
| `RemoverTabelaAsync` | `git rm` + commit + push de uma tabela excluída |
| `GitDisponivel` | Verifica se git está instalado via `git --version` |
| `InstalarGitAsync` | Instala git silenciosamente via `winget` |
| `RunGit` | Expõe `RunAsync` para uso externo com resultado `(exitCode, stdout, stderr)` |

- Credenciais (`Token`, `GitUsuario`, `GitEmail`) persistidas em `credenciais.json` no `RootDir` (`~/Downloads/ControleMateriaisLFB/`).
- Autenticação via URL com token embutido: `https://{token}@github.com/{owner}/{repo}.git`.
- Três repositórios separados gerenciados pelo mesmo serviço: `Pesagens`, `Recibos`, `TabelaPrecos`.
- **Migração automática** de arquivos legados: se existir um diretório sem `.git`, os arquivos são copiados para o repo recém-clonado, commitados e o diretório antigo é removido.

---

### ⚙️ Configuração do GitHub (`GitHubConfigDialog` / `GitHubAjudaDialog`)
- Novo **dialog de configuração** de credenciais GitHub com campos: Token, Git Usuário e Git Email.
- Botão "?" abre `GitHubAjudaDialog` com instruções sobre como gerar um Personal Access Token no GitHub.
- `GitHubConfigViewModel` valida os três campos antes de habilitar o botão "Salvar" (`PodeSalvar`).
- Ao salvar, persiste as credenciais em `credenciais.json`.

---

### 🎉 Dialogs de Feedback
- **`EnvioSucessoDialog`** — exibido após envio bem-sucedido de pesagem ao GitHub. Confirma o nome do arquivo JSON enviado.
- **`ReciboSucessoDialog`** — exibido após exportação do recibo PDF. Opções: "Abrir PDF" (não fecha), "+ Novo Recibo" (fecha e limpa), "X" (fecha).

---

### 🎨 Novos Converters
| Converter | Descrição |
|-----------|-----------|
| `StatusPesagemBrushConverter` | Mapeia `"pendente"` → laranja `#FF9800`, `"concluido"` → verde `#4CAF50`, `"falhou"` → vermelho `#F44336` |
| `StringEqualsConverter` | Retorna `true` se `value == ConverterParameter` (usado para `IsChecked` dos `ToggleButton` de filtro) |

---

### 📄 Melhorias no PDF de Recibo
- Layout do cabeçalho refinado: logo LFB em fundo verde (`#4CAF50`) com fallback texto "LFB" em branco.
- Grade de informações com 8 colunas (FORNECEDOR, valor, PESO, valor, VALOR, valor, DATA, valor).
- Tabela de itens com colunas: MATERIAL / KG / VALOR/KG / TOTAL.
- Células em branco (em vez de zeros) para itens sem preço ou sem total.
- Margens reduzidas (`0.8cm`) para melhor aproveitamento da página A4.

---

## 🔧 Detalhes Técnicos

### Novos Arquivos

```
ControleMateriais.Desktop/
├── Services/
│   └── GitHubService.cs                   — Novo — serviço centralizado de Git/GitHub (3 repos)
├── ViewModels/
│   ├── GitHubConfigViewModel.cs           — Novo — credenciais GitHub (Token, Usuário, Email)
│   ├── PesagensViewModel.cs               — Novo — lista pesagens + recibos, filtros, sincronização
│   ├── ReciboFromPesagemViewModel.cs      — Novo — recibo gerado a partir de pesagem existente
│   └── WeightCalculatorViewModel.cs       — Novo — calculadora de pesos + envio ao GitHub
├── Views/
│   ├── EnvioSucessoDialog.axaml(.cs)      — Novo — dialog confirmação após envio de pesagem
│   ├── GitHubAjudaDialog.axaml(.cs)       — Novo — instruções para criar Personal Access Token
│   ├── GitHubConfigDialog.axaml(.cs)      — Novo — formulário de configuração de credenciais
│   ├── HomeView.axaml(.cs)                — Novo — tela inicial com navegação dual
│   ├── PesagensView.axaml(.cs)            — Novo — abas Pesagens + Recibos com sincronização
│   ├── ReciboFromPesagemView.axaml(.cs)   — Novo — tela de recibo a partir de pesagem
│   ├── ReciboSucessoDialog.axaml(.cs)     — Novo — dialog após exportar recibo PDF
│   └── WeightCalculatorView.axaml(.cs)    — Novo — tela de pesagem de materiais
└── Converters/
    ├── StatusPesagemBrushConverter.cs     — Novo — cor por status de pesagem
    └── StringEqualsConverter.cs           — Novo — binding de ToggleButton de filtro
```

### Arquivos Modificados

```
ControleMateriais.Desktop/
├── ViewModels/
│   ├── MainWindowViewModel.cs             — Navegação para Home, Sistema de Pesos e Pesagens;
│   │                                        GitConfigurado/GitNaoConfigurado; IrParaRecibosCommand;
│   │                                        IrParaCalculadoraPesosCommand; ReciboSucessoDialog;
│   │                                        ExportarAsync com status em tempo real
│   └── PriceTableManagerViewModel.cs      — Integração com GitHubService para publicar/remover tabelas
├── Views/
│   ├── MainWindow.axaml                   — ContentControl com troca de view via ViewModel;
│   │                                        reduzido a shell com SplashWindow e navegação
│   ├── MainWindow.axaml.cs                — Handlers de navegação e abertura de dialogs
│   └── SplashWindow.axaml                 — Ajustado para novo fluxo de inicialização
README.md                                  — Atualizado com Release 3, novos sistemas e fluxos
```

### Modelo de dados — Pesagem (JSON)
```json
{
  "Cliente": "Nome do Cliente",
  "Horario": "2026-04-24T14:30:00",
  "StatusPesagem": "pendente",
  "Itens": [
    { "Nome": "Placa Mãe", "Peso": 1.250 },
    { "Nome": "Cobre", "Peso": 0.380 }
  ]
}
```
Após conclusão do recibo, o JSON é atualizado com:
```json
{
  "StatusPesagem": "concluido",
  "DataConclusao": "2026-04-24T15:00:00",
  "NomeRecibo": "NomeCliente_24-04-2026.pdf"
}
```

### Estrutura de Repositórios GitHub

| Repositório | Conteúdo | Sincronização |
|-------------|----------|---------------|
| `lfbreciclagemeletronica/Pesagens` | JSONs de pesagem (`*.json`) | Push ao salvar pesagem; pull+push ao sincronizar |
| `lfbreciclagemeletronica/Recibos` | PDFs de recibo (`*.pdf`) | Push ao exportar; pull+push ao sincronizar |
| `lfbreciclagemeletronica/TabelaPrecos` | JSONs de tabela de preços (`*.json`) | Push ao salvar/excluir tabela; pull+push ao sincronizar |

### Diretórios Locais

```
~/Downloads/ControleMateriaisLFB/
├── credenciais.json       — Token GitHub, nome e e-mail do usuário git
├── Pesagens/              — Clone local do repo Pesagens (com .git)
│   └── *.json
├── Recibos/               — Clone local do repo Recibos (com .git)
│   └── *.pdf
└── TabelaPrecos/          — Clone local do repo TabelaPrecos (com .git)
    └── *.json
```

---

## 📂 Fluxos Principais

### Fluxo — Registrar Pesagem
```
Home → Sistema de Pesos → preencher nome + pesos → Salvar e Enviar
       ↓ (se sem credenciais) GitHubConfigDialog
       ↓ git clone/pull → commit → push → EnvioSucessoDialog
```

### Fluxo — Gerar Recibo a partir de Pesagem
```
Home → Sistema de Recibos → aba Pesagens → ⟳ Sincronizar
       → clicar em pesagem pendente → ReciboFromPesagemView
       → selecionar tabela de preços → Exportar PDF
       → MarcarConcluido (commit no repo Pesagens)
       → ReciboSucessoDialog
```

### Fluxo — Gerar Recibo do Zero
```
Home → Sistema de Recibos → + Novo → ReciboView (fluxo original)
       → preencher nome + pesos + preços → Exportar PDF
       → ReciboSucessoDialog
```

### Fluxo — Configurar GitHub
```
Qualquer tela → ⚙ Configurar GitHub → GitHubConfigDialog
                → preencher Token + Usuário + E-mail → Salvar
                → credenciais.json gravado em ~/Downloads/ControleMateriaisLFB/
```

---

## 🕐 Esforço de Desenvolvimento

| Commit | Descrição | Tempo |
|--------|-----------|-------|
| `856c5a3` | Início do sistema de pesos — tela Home, WeightCalculatorView, navegação | 40 min |
| `6646207` | Sincronização de pesagens — clone/pull/push do repo Pesagens | 2h |
| `167d1e9` | Ajustes no menu principal, configuração do git, criação de recibos | 1h |
| `0b2f6f0` | Abas de recibos e pesagens, controle de sincronização por sessão | 2h30 |
| `8d62256` | Dois sistemas completos sincronizados, suporte a novo usuário | 2h |
| `a3ad3ac` | Atualização da documentação | — |
| **Total** | | **~8h10** |

---

**Versão**: Release 3  
**Branch**: `Release/3.0`  
**Data**: 2026-04-24  
**Responsável**: Cascade (AI Pair Programmer)  
**Repositório**: `lfbreciclagemeletronica/Controle-Materiais`
