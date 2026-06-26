# Exibir Itens Extras do Estoque na Tela de Venda

Adicionar ao `VendaViewModel` o carregamento dinâmico de itens fora do catálogo fixo que existam no `estoque.json`, para que apareçam como linhas vendáveis na tela de Nova Venda, exatamente como acontece na aba de Estoque.

---

## Diagnóstico

| Componente | Situação atual |
|---|---|
| `EstoqueViewModel.Recarregar()` | Exibe itens do catálogo **+ itens extras do `estoque.json`** |
| `VendaViewModel` (construtor) | Carrega **apenas** `ItemCatalog.OrderedItems` — itens extras ficam invisíveis |
| `VendaView.axaml` | Renderiza `ItemsControl` sobre `Itens` — sem seção de extras |
| PDF (`GerarPdfVenda`) | Itera só sobre `itensSelecionados` — extras nunca entram no recibo |

---

## Passos

### 1 — `VendaViewModel.cs`: método `CarregarItens()`
- Extrair a lógica de população de `Itens` do construtor para um método público `CarregarItens(Dictionary<string,decimal> totaisEstoque)`.
- Após adicionar os itens do catálogo fixo, iterar sobre `totaisEstoque` e adicionar `VendaItemWrapper` para cada chave **não presente no catálogo** (mesmo critério de `EstoqueViewModel.Recarregar()`).
- Inicializar `PesoTexto` dos itens extras com `"0,000"` (sem pré-preenchimento do estoque — usuário informa o quanto vender).
- Guardar a quantidade máxima disponível em estoque opcionalmente para validação futura (não bloquear agora).

### 2 — `MainWindowViewModel.cs`: integrar carga de itens extras
- No comando `IrParaVendaCommand` (ou em `VendaViewModel`) chamar `EstoqueViewModel.LerEstoque(RootDir)` e repassar para `VendaVM.CarregarItens(totais)`.
- Garantir que sempre que o usuário navega para a tela de Venda os itens extras são atualizados (estoque pode mudar por sincronização git).

### 3 — `VendaView.axaml`: separador visual para itens extras
- Não exige mudança estrutural; o `ItemsControl` já renderiza todos os `Itens`.
- Adicionar, **abaixo** do separador existente entre o catálogo e os extras, um `TextBlock` "Outros itens em estoque" visível apenas quando existirem itens extras — via `Binding` de um novo `bool TemItensExtras` no VM.

### 4 — `VendaViewModel.GerarPdfVenda()`: nenhuma mudança necessária
- A função já itera `itensSelecionados` (filtro `PesoAtual > 0`) — itens extras com peso preenchido entram automaticamente no PDF.

### 5 — Testes manuais
- Adicionar manualmente um item fora do catálogo no `estoque.json` (ex: `"CELULA": 5.0`).
- Abrir a tela de Venda → confirmar que a linha aparece.
- Preencher peso e salvar → confirmar que o item aparece no PDF e é subtraído do estoque.
- Confirmar que ao voltar ao estoque o saldo está correto.
- Confirmar que uma venda **sem** itens extras ainda funciona normalmente.
