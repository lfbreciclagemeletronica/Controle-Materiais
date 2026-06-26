# Reestruturar Estoque — Mês Atual + Vendas Diárias

Mudar o cálculo do estoque para usar `estoque-inicial.json` + `MM-YYYY.json` (mês atual apenas) - `venda-DD-MM-YYYY.json`, remover `estoque.json`, criar arquivos de venda diários, fix acentos e adicionar botão Atualizar Estoque.

---

## Contexto atual

- `VendaViewModel.SalvarVendaAsync()` salva:
  - PDF em `Recibos_Venda/`
  - `.meta.json` com `{ cliente, pesoTotal, valorVenda, data }` — **não tem itens individuais**
  - Subtrai do `estoque.json`
- `EstoqueViewModel` usa `estoque.json` como fonte única
- `ReconstruirBancoDadosService` gera `MM-YYYY.json` com registros mensais de pesagem

---

## Novo formato dos arquivos de venda

**Nome do arquivo:** `venda-DD-MM-YYYY.json` (ex: `venda-23-06-2026.json`)

```json
{
  "data": "23/06/2026",
  "registros": [
    {
      "nome": "Gabriel Fanto Stundner",
      "materiais": [
        { "descricao": "Placa Notebook C", "peso": 0.524 },
        { "descricao": "HD Completo", "peso": 0.458 }
      ]
    }
  ]
}
```

Se houver múltiplas vendas no mesmo dia → todas no mesmo arquivo, array de registros.

---

## Plano de mudanças

### 1. Criar script/tool de migração — `MigrarVendasParaJson.cs`
- Ler todos os PDFs em `Recibos_Venda/`
- Para cada PDF:
  - Extrair nome do cliente do nome do arquivo (regex)
  - Extrair data do nome do arquivo
  - Extrair materiais via `ReciboParserService.ExtrairPesos(pdf)`
  - Agrupar por data (DD-MM-YYYY) → `venda-DD-MM-YYYY.json`
- Salvar com `UnsafeRelaxedJsonEscaping` + `Encoding.UTF8`

### 2. `VendaViewModel.SalvarVendaAsync()` — criar `venda-DD-MM-YYYY.json`
- Após gerar PDF e antes de subtrair do estoque:
  - Extrair materiais da venda atual (já estão em `Itens`)
  - Ler `venda-DD-MM-YYYY.json` existente (se houver)
  - Adicionar novo registro ao array `registros`
  - Salvar com encoding correto
- Remover chamada a `EstoqueViewModel.GravarEstoque()` (não usa mais `estoque.json`)
- Atualizar callback de sucesso do Git para publicar `venda-DD-MM-YYYY.json` (não `estoque.json`)

### 3. `EstoqueViewModel` — novo cálculo do estoque
- Remover `GravarEstoque()` (não usa mais `estoque.json`)
- Remover `LerEstoque()` (não usa mais `estoque.json`)
- Novo método `CalcularEstoqueAtual()`:
  ```
  totais = LerEstoqueInicial()  // estoque-inicial.json
  totais += LerMesAtual()       // MM-YYYY.json do mês atual (DateTime.Now)
  totais -= LerVendas()         // todos venda-DD-MM-YYYY.json
  ```
- `Recarregar()` chama `CalcularEstoqueAtual()` e preenche `Itens`
- Remover `ProcessarNovosJsons()` (não processa mais JSONs antigos)
- `SincronizarGitAsync()` não publica mais `estoque.json`

### 4. `EstoqueView.axaml` — botão "Atualizar Estoque"
- Adicionar botão ao lado de "Sincronizar"
- Command: `AtualizarEstoqueCommand` → chama `Recarregar()`

### 5. Fix acentos em arquivos existentes
- Ler `06-2026.json` e `estoque-inicial.json`
- Re-salvar com `UnsafeRelaxedJsonEscaping` + `Encoding.UTF8`

### 6. `EstoqueInicialViewModel` — já fixado na sessão anterior
- Verificar que `GravarEstoqueInicialLocal()` usa encoding correto (já implementado)

---

## Arquivos modificados/criados

| Arquivo | Ação |
|---|---|
| `Services/MigrarVendasParaJson.cs` | NOVO — script de migração |
| `ViewModels/VendaViewModel.cs` | Criar `venda-DD-MM-YYYY.json`, remover `GravarEstoque` |
| `ViewModels/EstoqueViewModel.cs` | Novo cálculo, remover `estoque.json` |
| `Views/EstoqueView.axaml` | Adicionar botão Atualizar Estoque |
| `Services/ReconstruirBancoDadosService.cs` | Não usar mais `estoque.json` |

---

## Notas

- O `.meta.json` de venda continua sendo criado para compatibilidade com a aba de recibos
- `estoque.json` é completamente removido do fluxo
- Apenas o mês/ano atual é considerado no cálculo do estoque (meses anteriores ficam armazenados mas não afetam o estoque atual)
