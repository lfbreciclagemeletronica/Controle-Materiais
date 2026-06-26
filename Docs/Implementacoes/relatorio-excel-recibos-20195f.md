# Relatório Excel de Pesagens por Dia

Gerar um arquivo `.xlsx` a partir de todos os PDFs de recibos em `Recibos/`, organizando os pesos de cada item por coluna de data (uma coluna por dia), com os itens nas linhas na ordem do catálogo + extras ao final.

---

## 1. Ordenação dos recibos na lista (fix rápido)

**Arquivo:** `PesagensViewModel.cs` — método `AtualizarFiltroRecibos()`

- Trocar `.OrderBy(r => r.DataCriacaoRaw)` por `.OrderByDescending(r => r.DataCriacaoRaw)`
- Resultado: mais recentes aparecem primeiro

---

## 2. Adicionar dependência ClosedXML

**Arquivo:** `ControleMateriais.Desktop.csproj`

```xml
<PackageReference Include="ClosedXML" Version="0.104.1" />
```

Razão: ClosedXML é a biblioteca mais simples para gerar `.xlsx` sem COM/Office. Já temos iText7 para ler PDFs.

---

## 3. Serviço de extração de itens do PDF

**Arquivo novo:** `Services/ReciboParserService.cs`

Lógica:
- Usar `PdfReader` + `PdfTextExtractor` (iText7, já usado em `PriceTableManagerViewModel.cs`)
- Extrair texto do PDF linha a linha
- Para cada linha, tentar fazer match com padrão: `[Nome do Item]   [KG com vírgula]`
- Retornar `Dictionary<string, decimal>` com `{nomeMaterial → peso}`
- Ignorar linhas de cabeçalho (MATERIAL, FORNECEDOR, PESO, VALOR, DATA, LFB, CNPJ, etc.)
- Regex de peso: `\d+[,.]\d{3}` (formato `N3`, ex: `22,710`)

---

## 4. Serviço de geração do Excel

**Arquivo novo:** `Services/RelatorioExcelService.cs`

Estrutura do Excel gerado:

| Item \ Data | 01/06/2026 | 02/06/2026 | ... | TOTAL |
|---|---|---|---|---|
| Placa Drive | 10,000 | 5,000 | ... | 15,000 |
| Placa Notebook A | 0 | 12,000 | ... | 12,000 |
| ... | | | | |
| OUTROS (extra) | 3,500 | 0 | ... | 3,500 |

Algoritmo:
1. Listar todos os PDFs em `Recibos/` (excluindo subpasta `Recibos_Venda/`)
2. Para cada PDF: extrair data do nome do arquivo (via `ParsearNomeArquivoRecibo`) + itens (via `ReciboParserService`)
3. Agrupar por data (dd/MM/yyyy) — datas são colunas, ordenadas crescente (mais antiga → mais recente)
4. Linhas: `ItemCatalog.OrderedItems` primeiro, depois itens extras (alphabético) que apareceram em qualquer recibo mas não estão no catálogo
5. Células: soma de todos os recibos do dia para aquele item (vários clientes no mesmo dia → somados)
6. Coluna final `TOTAL`: soma de todos os dias por linha
7. Linha final `TOTAL`: soma de todos os itens por coluna
8. Salvar com `SaveFileDialog` (`.xlsx`)

---

## 5. Comando e botão na UI

**ViewModel:** `PesagensViewModel.cs`
- Adicionar `ICommand ExportarExcelCommand`
- Método `ExportarExcelAsync()`: chama `RelatorioExcelService`, abre `SaveFilePickerAsync`

**View:** `PesagensView.axaml` — aba Recibos, barra superior ao lado do botão Sincronizar:
```xml
<Button Content="📊 Exportar Excel"
        Command="{Binding ExportarExcelCommand}"
        IsEnabled="{Binding !SincronizandoRecibos}"/>
```

---

## Escopo fora do plano
- Sem modificar lógica de estoque, venda ou pesagens
- Não alterar estrutura de PDFs gerados
- Sem parsing de PDFs de `Recibos_Venda/`
