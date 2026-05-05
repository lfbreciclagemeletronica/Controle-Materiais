# Release 4 — LFB Controle de Materiais

## Visão Geral

Introduz o módulo de **Venda de Estoque** — um sistema completo para registrar vendas de materiais do estoque, gerar recibos PDF dedicados e sincronizar automaticamente com o GitHub. Complementa o módulo de Estoque existente (Release 3.x) com uma segunda aba de visualização de recibos de venda, filtros por cliente e mês, e botões de ação por linha. O botão "Atualizar" foi removido do cabeçalho do Estoque e integrado à aba de Recibos de Venda.

---

## ✨ Features Implementadas

### 💰 Sistema de Venda de Estoque (`VendaView` / `VendaViewModel`)

Nova tela acessível pelo botão **💰 Venda** na tela de Controle de Estoque:

- Lista todos os **51 itens do catálogo** com campo de quantidade em kg editável por linha.
- Campo **Nome do Cliente** (obrigatório para salvar).
- Campo **Valor da Venda (R$)** com formatação automática:
  - `GotFocus` limpa o campo para digitação.
  - `LostFocus` e `Enter` formatam o valor como `R$ X.XXX,XX`.
- Badge **PESO TOTAL** em verde, atualizado em tempo real conforme os pesos são preenchidos.
- Comportamento de peso **idêntico ao sistema de pesagens/recibos**:
  - `GotFocus` apaga `0,000` e seleciona o campo.
  - `LostFocus` e `Enter` confirmam e formatam como `N3` pt-BR.
- Botão **Limpar** — zera todos os campos.
- Botão **💾 Salvar Venda** — azul (`Classes="primary"`), validação antes de salvar.
- Botão **← Voltar** — retorna ao Controle de Estoque.

**Ao salvar:**
1. Gera PDF do recibo de venda em `Recibos/Recibos_Venda/`.
2. Salva `.pdf.meta.json` ao lado do PDF com `cliente`, `pesoTotal`, `valorVenda` e `data`.
3. Subtrai os itens vendidos do `estoque.json` local (nunca fica negativo).
4. Abre o modal **VendaSucessoDialog** com confirmação visual.
5. Em paralelo ao modal: publica PDF em `Recibos/Recibos_Venda/` no GitHub e faz pull+push do `estoque.json` atualizado no repositório `banco-de-dados`.

---

### 🧾 Recibo de Venda PDF

Layout profissional gerado com QuestPDF:

- Mesmo cabeçalho da LFB: logo verde, CNPJ, IE, endereço.
- Faixa de informações: **CLIENTE** | **PESO** | **VALOR** | **DATA**.
- Título destacado: `RECIBO DE VENDA DE ESTOQUE`.
- Tabela de itens simplificada: apenas **MATERIAL** e **KG** (sem preço/kg, sem total por item).
- Nome do arquivo gerado: `{NomeCliente}_{dd-MM-yyyy}.pdf`.

---

### 🪟 Modal de Sucesso (`VendaSucessoDialog`)

Exibido após PDF gerado com sucesso:

- Ícone ✔ verde com mensagem "Recibo de venda gerado com sucesso!".
- Nome do arquivo gerado.
- Status de sincronização Git em **tempo real** (atualiza enquanto o push acontece).
- Botão **📄 Abrir PDF** — abre o PDF no visualizador padrão do sistema.
- Botão **Fechar** — fecha o modal.

---

### 📋 Aba "Recibos de Venda" no Controle de Estoque

Segunda aba adicionada ao `TabControl` do Controle de Estoque:

**Barra de filtros:**
- Campo **"Filtrar por cliente..."** — filtragem em tempo real por nome do cliente (case-insensitive).
- Campo **"Mês/Ano (ex: 05/2026)"** — filtragem por mês/ano da data do recibo.
- Botão **⟳ Atualizar** — recarrega a lista de recibos do diretório local.

**Tabela de recibos:**

| Coluna | Descrição |
|--------|-----------|
| **Cliente** | Nome do cliente lido do `.pdf.meta.json` |
| **Peso Total** | Peso total da venda em laranja (`N3 kg`) |
| **Valor Total** | Valor em reais em verde (`C` pt-BR), vazio se não disponível |
| **Data** | Data da venda em texto primário (visível no tema escuro) |
| **Ações** | Botão 📄 Abrir (76px) + Botão 🗑 Excluir (80px, fundo vermelho) |

- Ordenada do mais recente para o mais antigo.
- Mensagem de estado vazio quando nenhum recibo encontrado.

---

### 🗂️ Metadados de Recibo (`.pdf.meta.json`)

Para cada PDF salvo em `Recibos_Venda/`, é criado um arquivo `<nome>.pdf.meta.json` ao lado:

```json
{
  "cliente": "Nome do Cliente",
  "pesoTotal": 1.200,
  "valorVenda": 200.00,
  "data": "05/05/2026"
}
```

Permite exibir peso, valor e data corretos na aba sem depender do nome do arquivo ou de repositórios externos.

---

### 🔧 GitHubService — Novos Métodos

| Método | Descrição |
|--------|-----------|
| `PublicarReciboVendaAsync` | Commit e push de um PDF no subdiretório `Recibos_Venda/` dentro do repo `Recibos` |

O `PublicarJsonBancoDadosAsync` existente é reaproveitado para pull+push do `estoque.json` após cada venda.

---

### 🏗️ Arquitetura — Navegação

- `AppPage.Venda` adicionado ao enum de páginas.
- `IrParaVendaCommand` no `EstoqueViewModel` (recebe `Action?` no construtor) e no `MainWindowViewModel`.
- `VendaVM` instanciado no `MainWindowViewModel` com callback de retorno ao Estoque.
- Painel `VendaView` registrado no `MainWindow.axaml` com `IsVisible="{Binding IsVendaPage}"`.

---

## 🔧 Detalhes Técnicos

### Novos Arquivos

```
ControleMateriais.Desktop/
├── ViewModels/
│   └── VendaViewModel.cs              — ViewModel completo de venda de estoque
├── Views/
│   ├── VendaView.axaml(.cs)           — UI de registro de venda
│   └── VendaSucessoDialog.axaml(.cs)  — Modal de sucesso com status Git em tempo real
```

### Arquivos Modificados

```
ControleMateriais.Desktop/
├── ViewModels/
│   ├── EstoqueViewModel.cs            — ReciboVendaItem, filtros, AtualizarCommand,
│   │                                    AbrirReciboVendaCommand, ExcluirReciboVendaCommand,
│   │                                    CarregarRecibosVenda (lê .pdf.meta.json)
│   └── MainWindowViewModel.cs         — AppPage.Venda, VendaVM, IrParaVendaCommand, IsVendaPage
├── Views/
│   ├── EstoqueView.axaml              — Aba Recibos de Venda com filtros, tabela e botões
│   ├── EstoqueView.axaml.cs           — ConfirmarExclusaoCallback via ConfirmDeleteDialog
│   └── MainWindow.axaml               — Painel VendaView com IsVisible=IsVendaPage
├── Services/
│   └── GitHubService.cs               — PublicarReciboVendaAsync
README.md                              — Atualizado com Release 4
```

### Estrutura de Diretórios — Novos Caminhos

```
~/Downloads/ControleMateriaisLFB/
├── banco-de-dados/
│   └── estoque.json                   ← atualizado a cada venda
└── Recibos/
    └── Recibos_Venda/
        ├── Cliente_05-05-2026.pdf
        └── Cliente_05-05-2026.pdf.meta.json
```

### Fluxo — Registrar Venda

```
Controle de Estoque → 💰 Venda
  → preencher nome do cliente + pesos por item + valor R$
  → 💾 Salvar Venda
      ↓ gera PDF em Recibos/Recibos_Venda/
      ↓ salva .pdf.meta.json (peso, valor, data, cliente)
      ↓ subtrai itens do estoque.json local
      ↓ VendaSucessoDialog (✔ + status Git em tempo real)
          ↓ push PDF → GitHub Recibos/Recibos_Venda/
          ↓ pull+push estoque.json → GitHub banco-de-dados/
  → Fechar modal → Controle de Estoque (aba Recibos de Venda atualizada)
```

---

## 🕐 Esforço de Desenvolvimento

| Sessão | Descrição | Tempo |
|--------|-----------|-------|
| 1 | VendaViewModel + VendaView + GitHubService.PublicarReciboVendaAsync | 1h30 |
| 2 | Integração AppPage.Venda + navegação + compilação | 45min |
| 3 | Lógica de salvamento simplificada (sem JSON banco-dados) | 20min |
| 4 | Ajustes UI: R$ formatado, peso idêntico recibos, botão azul, modal VendaSucessoDialog | 1h |
| 5 | Fix peso 0,000: .pdf.meta.json (ChangeExtension→concatenação), botões padrão | 30min |
| 6 | Ajuste colunas aba Recibos de Venda + Release4.md + README | 20min |
| **Total** | | **~4h25** |

---

**Versão**: Release 4  
**Branch**: `Release/4.0`  
**Data**: 2026-05-05  
**Responsável**: Cascade (AI Pair Programmer)  
**Repositório**: `lfbreciclagemeletronica/Controle-Materiais`
