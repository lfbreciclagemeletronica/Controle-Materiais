# Reconstruir Banco — Novo Formato + Fix Acentuação

Refatorar `ReconstruirBancoDadosService` para gerar arquivos `MM-YYYY.json` com o novo formato de registros agrupados por cliente/data, gerar log de modificações no banco-de-dados, e corrigir o encoding de acentos no `estoque-inicial.json`.

---

## Contexto atual

- `ReconstruirBancoDadosService.Reconstruir()` já existe em `Services/ReconstruirBancoDadosService.cs`
- Atualmente gera um `.json` por PDF com `{ "NomeItem": peso, "data": "dd-MM-yyyy", "status": "..." }`
- Os PDFs de pesagem ficam em `Recibos/` (não em `Recibos_Venda/`)
- Parser: `ReciboParserService.ExtrairPesos(pdf)` → `Dictionary<string, decimal>`
- Ainda não extrai o nome do cliente dos PDFs — apenas os pesos por material

---

## Novo formato dos arquivos mensais

**Nome do arquivo:** `MM-YYYY.json` (ex: `06-2026.json`)

```json
{
  "data": "06/2026",
  "registros": [
    {
      "nome": "Gabriel Fanto Stundner",
      "materiais": [
        { "descricao": "Placa Notebook C", "peso": 0.524 },
        { "descricao": "HD Completo", "peso": 0.458 }
      ]
    },
    {
      "nome": "Fulana da silva",
      "materiais": [
        { "descricao": "Placa Mãe C", "peso": 3.200 }
      ]
    }
  ]
}
```

Se o arquivo `MM-YYYY.json` já existir, os novos registros do recibo são **adicionados** ao array `registros`.

---

## Plano de mudanças

### 1. `Services/ReconstruirBancoDadosService.cs` — refatorar `Reconstruir()`
- **Não apaga mais** os arquivos `MM-YYYY.json` existentes; em vez disso recria do zero baseado nos PDFs
- Para cada PDF de pesagem em `Recibos/`:
  - Extrair data → `MM-YYYY.json`
  - Extrair nome do cliente via `ParsearNomeArquivoRecibo` (regex já existente no VM)
  - Extrair materiais via `ReciboParserService.ExtrairPesos()` → lista de `{ descricao, peso }`
  - Se o arquivo `MM-YYYY.json` já existir: adicionar o registro ao array `registros`
  - Se não existir: criar com a estrutura acima
- Salvar todos os `MM-YYYY.json` com `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` + `Encoding.UTF8`
- Montar `totaisPesagem` somando todos os materiais de todos os registros de todos os arquivos
- Subtrair vendas (igual ao atual)
- Gravar `estoque.json` (não toca em `estoque-inicial.json`)
- Gerar `modificacao-estoque.log` no `BancoDadosDir`:
  - Uma linha por material adicionado: `2026-06-23T14:30:00 | Gabriel Fanto Stundner | Placa Notebook C | 0.524 kg`

### 2. `EstoqueViewModel.GravarEstoque()` — fix encoding
- Adicionar `Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping` e `Encoding.UTF8` no `File.WriteAllText` para o `estoque.json`

### 3. `EstoqueInicialViewModel.GravarEstoqueInicialLocal()` — já tem o encoder correto (feito na sessão anterior)
- Verificar que `LerEstoqueInicial()` também usa `Encoding.UTF8` ao ler (provavelmente ok pois `File.ReadAllText` usa UTF-8 por padrão no .NET)

---

## Arquivos modificados

| Arquivo | Mudança |
|---|---|
| `Services/ReconstruirBancoDadosService.cs` | Novo formato `MM-YYYY.json` com registros por cliente; log de modificações |
| `ViewModels/EstoqueViewModel.cs` | Fix encoding em `GravarEstoque()` |

---

## Não muda
- `estoque-inicial.json` — não é tocado pelo Reconstruir
- `ReciboParserService` — usa como está
- UI / botão Reconstruir Banco — lógica de confirmação e callback permanecem iguais
- Formato do `estoque.json` — continua flat `{ "Material": peso }`
