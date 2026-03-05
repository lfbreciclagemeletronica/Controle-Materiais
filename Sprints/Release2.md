# Release 2 — LFB Controle de Materiais

## Visão Geral
Expansão da interface principal com novos campos de entrada, melhorias na geração do recibo PDF e refinamentos na experiência de edição (seleção de linha, cancelamento via Esc).

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

---

## 🔧 Detalhes Técnicos

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

---

## 📂 Arquivos Modificados

```
ControleMateriais.Desktop/
├── ViewModels/
│   ├── MainWindowViewModel.cs        — Impurezas, ItensPersonalizados, SelecionarItem, Cancelar*, DelegateCommand<T>
│   └── PriceTableManagerViewModel.cs — SelecionarItemCommand, CancelarEdicao, IsSelected
├── Views/
│   ├── MainWindow.axaml              — Seção itens personalizados, Impurezas, seleção de linha
│   └── MainWindow.axaml.cs           — Handlers Esc, PointerPressed, Impurezas
└── Converters/
    └── BoolToBrushConverter.cs       — Novo
```

---

**Versão**: Release 2  
**Data**: 2026-03-05  
**Responsável**: Cascade (AI Pair Programmer)  
**Repositório**: `lfbreciclagemeletronica/Controle-Materiais`
