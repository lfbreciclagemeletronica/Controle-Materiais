# Plano: Tela de Estoque Inicial

Implementar uma nova tela de gerenciamento de estoque inicial mensal, acessível a partir do Controle de Estoque, com campos de peso por item (sem nome de cliente/preços), seletor de mês/ano, persistência em `estoque-inicial.json` no banco-de-dados Git, log de modificações e botão "Registrar Estoque" que gera `estoque-final-MM-AAAA.json`.

---

## Arquitetura

### Novos arquivos
- `ViewModels/EstoqueInicialViewModel.cs` — ViewModel da nova tela
- `Views/EstoqueInicialView.axaml` + `.axaml.cs` — View da nova tela

### Arquivos modificados
- `ViewModels/EstoqueViewModel.cs` — adicionar `IrParaEstoqueInicialCommand`
- `Views/EstoqueView.axaml` — botão "Estoque Inicial" na barra de ações
- `Views/MainWindow.axaml` — painel `IsEstoqueInicialPage`
- `ViewModels/MainWindowViewModel.cs` — `IsEstoqueInicialPage`, `EstoqueInicialVM`, `IrParaEstoqueInicialCommand`, `IrParaEstoqueCommand`

---

## Etapas

### 1. `EstoqueInicialViewModel.cs`
- Propriedades:
  - `ObservableCollection<PesoInicialWrapper> Itens` (nome + peso decimal para cada item do `ItemCatalog.OrderedItems`)
  - `int MesSelecionado`, `int AnoSelecionado` (inicializa com mês/ano atual)
  - `string Status`, `bool Salvando`, `bool Registrando`
- **Carregar**: ao inicializar, lê `estoque-inicial.json` do `BancoDadosRepoDir`; se existir, preenche mês/ano e pesos salvos
- **Salvar** (`SalvarCommand`):
  - Serializa pesos + `{ "mes": MM, "ano": AAAA }` em `estoque-inicial.json`
  - Appenda entrada em `modificacao-estoque-inicial.log` com timestamp ISO
  - Faz push via `GitHubService.PublicarJsonBancoDadosAsync`
- **Registrar Estoque** (`RegistrarEstoqueCommand`):
  - Lê `estoque.json` atual (snapshot do estado atual do estoque)
  - Serializa em `estoque-final-MM-AAAA.json` (mês/ano do `estoque-inicial.json`)
  - Faz push via `GitHubService.PublicarJsonBancoDadosAsync`
  - Cria novo `estoque-inicial.json` com os valores do `estoque.json` atual e mês/ano atual
  - Appenda ao log
  - Atualiza a tela com os novos valores
- **Voltar** (`VoltarCommand`) → callback para navegar de volta ao Controle de Estoque

### 2. `PesoInicialWrapper`
- Classe auxiliar simples (pode ficar no mesmo arquivo do VM):
  - `string Nome`
  - `decimal Peso`
  - `string PesoTexto` (TwoWay com parse, idêntico ao `PesoWrapper` existente)

### 3. `EstoqueInicialView.axaml`
Layout idêntico à tela de recibos (`ReciboView`) mas simplificado:
- **Header**: botão `← Controle de Estoque` | "Estoque Inicial" centralizado | botões `Salvar` + `Registrar Estoque`
- **Seletor de mês/ano**: dois `NumericUpDown` (Mês 1-12, Ano 2020-2099) ou `ComboBox`
- **Tabela**: apenas colunas Material + Peso (kg) — sem preço, sem total
- **Status** em tempo real abaixo do header

### 4. `EstoqueView.axaml`
Adicionar botão "📦 Estoque Inicial" na `StackPanel` de ações do cabeçalho, ao lado de "Venda" e "Sincronizar", vinculado a `IrParaEstoqueInicialCommand`.

### 5. `MainWindow.axaml`
Adicionar painel `IsEstoqueInicialPage`:
```xml
<Grid IsVisible="{Binding IsEstoqueInicialPage}">
  <Border ...>
    <views:EstoqueInicialView DataContext="{Binding EstoqueInicialVM}"/>
  </Border>
</Grid>
```

### 6. `MainWindowViewModel.cs`
- Adicionar `EstoqueInicialViewModel EstoqueInicialVM`
- `bool IsEstoqueInicialPage`
- `IrParaEstoqueInicialCommand` → sets `CurrentPage = PageEstoqueInicial`
- Conectar `EstoqueInicialVM.VoltarCommand` → `IrParaEstoqueCommand`

---

## Arquivos JSON e Log

| Arquivo | Caminho | Formato |
|---|---|---|
| `estoque-inicial.json` | `BancoDadosRepoDir/` | `{ "mes": 6, "ano": 2026, "Placa Drive": 1.5, ... }` |
| `estoque-final-06-2026.json` | `BancoDadosRepoDir/` | snapshot de `estoque.json` com `{ "data": "06/2026", ... }` |
| `modificacao-estoque-inicial.log` | `BancoDadosRepoDir/` | linhas `2026-06-23T14:00:00 | ação | usuário` |

---

## Ordem de implementação
1. `PesoInicialWrapper` + `EstoqueInicialViewModel.cs`
2. `EstoqueInicialView.axaml` + `.axaml.cs`
3. Modificar `EstoqueView.axaml` (botão)
4. Modificar `MainWindow.axaml` (painel)
5. Modificar `MainWindowViewModel.cs` (navegação + VM)
6. Ajustar `EstoqueViewModel.cs` se necessário (IrParaEstoqueInicial callback)
