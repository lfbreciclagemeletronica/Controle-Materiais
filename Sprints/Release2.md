# Release 2 — LFB Controle de Materiais

## Visão Geral
Expansão da interface principal com novos campos de entrada, melhorias na geração do recibo PDF e refinamentos na experiência de edição (seleção de linha, cancelamento via Esc). Inclui também ajustes de UX, correção de warning e atualização de documentação.

---

## ✨ Features Implementadas

### 📝 Itens Personalizados
- Adicionada seção com **4 linhas de itens personalizados** abaixo da lista principal de materiais.
- Cada item possui: nome editável (TextBox com watermark), peso (kg), preço por kg e total calculado automaticamente.
- Totais e pesos dos itens personalizados são integrados ao **Valor Total** e **Peso Total** da tela principal.
- Itens com peso > 0 e nome preenchido são incluídos na tabela do **recibo PDF**.

### 🧪 Campo Impurezas
- Adicionado campo **Impurezas** com entrada exclusiva de peso (campo de preço desabilitado, exibindo `—`).
- O peso de Impurezas soma ao **Peso Total** exibido no cabeçalho, mas não ao valor monetário.
- Quando peso > 0, uma linha "Impurezas" é adicionada ao final da tabela do recibo PDF (colunas de valor/total em branco).

### 🗂️ Filtro de Itens no Recibo PDF
- O recibo PDF agora exibe **somente itens com peso > 0** — itens zerados não aparecem no documento final.
- Aplica-se aos itens do catálogo principal e aos itens personalizados.

### 🖱️ Seleção de Linha (estilo Excel)
- Clicar em qualquer parte de uma linha nas duas listas (principal e tabela de preços) **seleciona a linha**, destacando-a com fundo azul (`#2A4FC3F7`).
- A seleção é exclusiva: selecionar uma linha desseleciona as demais.
- Implementado via `PointerPressed` nas `Border` de cada linha, `IsSelected` nos wrappers e `BoolToBrushConverter`.

### ⌨️ Cancelamento de Edição com Esc
- Pressionar **Esc** em qualquer campo editável:
  1. Restaura o valor que estava exibido antes de iniciar a edição.
  2. Remove o foco do campo, devolvendo-o à janela (`TopLevel.GetTopLevel(tb)?.Focus()`).
- Aplica-se a todos os campos: peso, preço da lista principal, itens personalizados, Impurezas e tabela de preços.

### 🔵 Seleção de Linha ao Entrar em Edição
- Ao clicar diretamente em um `TextBox`, o `GotFocus` handler também chama `SelecionarItem` — a linha é destacada tanto por clique fora quanto por clique direto no campo.
- Corrigido o caso em que o `TextBox` consumia o evento de clique antes da `Border` receber o `PointerPressed`.

### 🔒 Botão "Fechar" Sempre Visível
- O botão **Fechar** da tela de tabelas de preços foi movido do `StackPanel` de ações condicionais para o cabeçalho fixo.
- Agora está sempre visível, mesmo quando nenhuma tabela está selecionada ou em edição.

### 🏷️ Título da Janela
- Título da janela alterado de `"Controle de Materiais"` para **`"LFB Sistema de Recibos"`**.

### � PDF da Lista de Preços — Layout Centralizado
- Tabela centralizada na página A4 com `AlignCenter()` e colunas de largura fixa.
- Coluna Nome: 180pt · Coluna Preço: 78pt (valor completo `R$ 75,00`, alinhado à direita).
- Bordas completas em todas as células (`Border(0.15f)`).
- Eliminada a separação do símbolo `R$` em coluna própria.

### 📚 Documentação
- `README.md` atualizado: título, funcionalidades da Release 2, passo a passo e seção "Como Usar" com itens personalizados, impurezas e novos comportamentos.
- `TUTORIAL.md` criado com 14 seções, imagens de `images/`, tabelas explicativas, atalhos de teclado e localização dos arquivos gerados.

---

## �🔧 Detalhes Técnicos

### Novos Arquivos
- `Converters/BoolToBrushConverter.cs` — Converter `bool → IBrush` configurável via propriedades `TrueBrush`/`FalseBrush`, usado para highlight de linha selecionada.

### Alterações em ViewModels

#### `MainWindowViewModel.cs`
- Adicionada `ObservableCollection<CustomItemWrapper> ItensPersonalizados` (4 itens).
- Adicionado `ICommand SelecionarItemCommand` via `DelegateCommand<object>`.
- Adicionado método `SelecionarItem(object?)` — atualiza `IsSelected` exclusivamente nas duas coleções.
- Adicionadas propriedades e métodos para Impurezas: `ImpurezasPesoAtual`, `ImpurezasPesoTexto`, `IniciarEdicaoImpurezas()`, `ConfirmarEdicaoImpurezas()`, `CancelarEdicaoImpurezas()`.
- `RecalcularTotalGeral()` inclui peso dos itens personalizados e de Impurezas.
- `GerarReciboPdf()` filtra itens por `PesoAtual > 0` e inclui itens personalizados e Impurezas na tabela.
- Adicionado `DelegateCommand<T>` genérico para suportar comandos com parâmetro tipado.
- `DelegateCommand<T>.CanExecuteChanged` usa `add { } remove { }` para eliminar warning `CS0067`.

#### `PesoWrapper`
- Adicionado `IsSelected` (bool, `INotifyPropertyChanged`).
- Adicionados `_pesoTextoAnterior` e `_precoTextoAnterior` para suporte ao cancelamento.
- Adicionados `CancelarEdicao()` e `CancelarEdicaoPreco()`.

#### `CustomItemWrapper`
- Adicionado `IsSelected`.
- Adicionados `_pesoTextoAnterior` e `_precoTextoAnterior`.
- Adicionados `CancelarEdicaoPeso()` e `CancelarEdicaoPreco()`.

#### `PriceTableManagerViewModel.cs`
- Adicionado `ICommand SelecionarItemCommand` com lógica de seleção exclusiva sobre `ItensEdicao`.

#### `ItemPrecoWrapper`
- Adicionado `IsSelected`.
- Adicionado `_precoTextoAnterior`.
- Adicionado `CancelarEdicao()`.

### Alterações na View

#### `MainWindow.axaml`
- Adicionado recurso `BoolToBrushConverter` com chave `SelectedRowBrush`.
- Linhas da lista principal e itens personalizados receberam `PointerPressed="ItemRow_PointerPressed"` e `Background="{Binding IsSelected, Converter=...}"`.
- Linhas da tabela de preços receberam `PointerPressed="TabelaItemRow_PointerPressed"`.
- Adicionada seção visual "Itens Personalizados" com 4 linhas editáveis.
- Adicionada linha "Impurezas" com campo de peso habilitado e preço desabilitado.

#### `MainWindow.axaml.cs`
- Adicionados handlers `ItemRow_PointerPressed` e `TabelaItemRow_PointerPressed`.
- Todos os `KeyDown` handlers atualizados com tratamento de `Key.Escape`: chama `Cancelar*()` e `TopLevel.GetTopLevel(tb)?.Focus()`.
- Adicionados handlers `ImpurezasPesoTextBox_GotFocus`, `ImpurezasPesoTextBox_KeyDown`, `ImpurezasPesoTextBox_LostFocus`.
- Todos os `GotFocus` handlers (`PesoTextBox`, `PrecoTextBox`, `CustomPesoTextBox`, `CustomPrecoTextBox`, `TabelaPrecoTextBox`) chamam `SelecionarItem` para garantir highlight ao clicar diretamente no campo.

---

## 📂 Arquivos Modificados

```
ControleMateriais.Desktop/
├── ViewModels/
│   ├── MainWindowViewModel.cs        — Impurezas, ItensPersonalizados, SelecionarItem, Cancelar*, DelegateCommand<T>, CS0067 fix
│   └── PriceTableManagerViewModel.cs — SelecionarItemCommand, CancelarEdicao, IsSelected, PDF layout centralizado
├── Views/
│   ├── MainWindow.axaml              — Seção itens personalizados, Impurezas, seleção de linha, título, botão Fechar fixo
│   └── MainWindow.axaml.cs           — Handlers Esc, PointerPressed, Impurezas, GotFocus com SelecionarItem
└── Converters/
    └── BoolToBrushConverter.cs       — Novo

Docs/
├── README.md                         — Atualizado com Release 2
└── TUTORIAL.md                       — Novo — guia completo com imagens
```

---

**Versão**: Release 2  
**Data**: 2026-03-05  
**Responsável**: Cascade (AI Pair Programmer)  
**Repositório**: `lfbreciclagemeletronica/Controle-Materiais`
