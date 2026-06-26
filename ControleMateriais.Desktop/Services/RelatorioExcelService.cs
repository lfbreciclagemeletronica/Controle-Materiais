using ClosedXML.Excel;
using ControleMateriais.Desktop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ControleMateriais.Desktop.Services;

public static class RelatorioExcelService
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>
    /// Gera o Excel de pesagens/vendas por dia para o mês selecionado, lendo dos JSONs do banco-de-dados.
    /// </summary>
    /// <param name="bancoDadosDir">Diretório banco-de-dados/ com os JSONs</param>
    /// <param name="mesAno">Mês no formato "MM-yyyy" (ex: "06-2026")</param>
    /// <param name="outputPath">Caminho completo do .xlsx a salvar</param>
    /// <param name="progresso">Callback opcional para reportar progresso</param>
    public static void Gerar(string bancoDadosDir, string mesAno, string outputPath, Action<string>? progresso = null)
    {
        // ── 1. Ler compras do mês: compra-MM-yyyy.json ────────────────────────
        // Estrutura: { "registros": [ { "nome": "...", "materiais": [ {"descricao":"...", "peso": N} ] } ] }
        // Não tem breakdown por dia — todas as compras do mês ficam em "TOTAL"
        progresso?.Invoke($"Lendo compras do mês {mesAno}...");
        var comprasTotais = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var extrasGlobal  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var compraPath = Path.Combine(bancoDadosDir, $"compra-{mesAno}.json");
        if (File.Exists(compraPath))
        {
            try
            {
                var obj = JsonNode.Parse(File.ReadAllText(compraPath))?.AsObject();
                if (obj is not null && obj.ContainsKey("registros"))
                {
                    foreach (var reg in obj["registros"]!.AsArray())
                    {
                        if (reg is JsonObject regObj && regObj.ContainsKey("materiais"))
                        {
                            foreach (var mat in regObj["materiais"]!.AsArray())
                            {
                                if (mat is JsonObject matObj &&
                                    matObj.ContainsKey("descricao") && matObj.ContainsKey("peso"))
                                {
                                    var nome = matObj["descricao"]!.GetValue<string>();
                                    var peso = ExtrairDecimal(matObj["peso"]);
                                    comprasTotais[nome] = comprasTotais.TryGetValue(nome, out var c) ? c + peso : peso;
                                    if (!ItemCatalog.OrderedItems.Contains(nome, StringComparer.OrdinalIgnoreCase))
                                        extrasGlobal.Add(nome);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // ── 2. Ler vendas do mês: venda-DD-MM-yyyy.json agrupadas por dia ─────
        // Filenames: venda-DD-MM-yyyy.json onde DD-MM-yyyy pertence ao mês selecionado
        progresso?.Invoke($"Lendo vendas do mês {mesAno}...");
        var porDiaVenda = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(bancoDadosDir))
        {
            foreach (var file in Directory.GetFiles(bancoDadosDir, $"venda-*-{mesAno}.json", SearchOption.TopDirectoryOnly)
                                          .OrderBy(f => f))
            {
                // Extrair data do nome: venda-DD-MM-yyyy.json
                var semExt = Path.GetFileNameWithoutExtension(file);
                var mData = Regex.Match(semExt, @"^venda-(\d{2}-\d{2}-\d{4})$");
                if (!mData.Success) continue;
                if (!DateTime.TryParseExact(mData.Groups[1].Value, "dd-MM-yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtVenda)) continue;

                var dataLabel = dtVenda.ToString("dd/MM/yyyy");

                try
                {
                    var obj = JsonNode.Parse(File.ReadAllText(file))?.AsObject();
                    if (obj is null || !obj.ContainsKey("registros")) continue;

                    if (!porDiaVenda.TryGetValue(dataLabel, out var diaDict))
                    {
                        diaDict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                        porDiaVenda[dataLabel] = diaDict;
                    }

                    foreach (var reg in obj["registros"]!.AsArray())
                    {
                        if (reg is JsonObject regObj && regObj.ContainsKey("materiais"))
                        {
                            foreach (var mat in regObj["materiais"]!.AsArray())
                            {
                                if (mat is JsonObject matObj &&
                                    matObj.ContainsKey("descricao") && matObj.ContainsKey("peso"))
                                {
                                    var nome = matObj["descricao"]!.GetValue<string>();
                                    var peso = ExtrairDecimal(matObj["peso"]);
                                    diaDict[nome] = diaDict.TryGetValue(nome, out var v) ? v + peso : peso;
                                    if (!ItemCatalog.OrderedItems.Contains(nome, StringComparer.OrdinalIgnoreCase))
                                        extrasGlobal.Add(nome);
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        var datasVenda = porDiaVenda.Keys
            .OrderBy(d => ParseData(d))
            .ToList();

        // ── 3. Ler estoque inicial do mês: estoque-inicial-MM-yyyy.json ───────
        progresso?.Invoke($"Lendo estoque inicial do mês {mesAno}...");
        var estoqueInicial = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var estoqueInicialPath = Path.Combine(bancoDadosDir, $"estoque-inicial-{mesAno}.json");
        if (!File.Exists(estoqueInicialPath))
            estoqueInicialPath = Path.Combine(bancoDadosDir, "estoque-inicial.json");

        if (File.Exists(estoqueInicialPath))
        {
            try
            {
                var obj = JsonNode.Parse(File.ReadAllText(estoqueInicialPath))?.AsObject();
                if (obj is not null)
                {
                    foreach (var kvp in obj)
                    {
                        if (kvp.Key.Equals("data", StringComparison.OrdinalIgnoreCase) ||
                            kvp.Key.Equals("mes", StringComparison.OrdinalIgnoreCase) ||
                            kvp.Key.Equals("ano", StringComparison.OrdinalIgnoreCase)) continue;
                        estoqueInicial[kvp.Key] = ExtrairDecimal(kvp.Value);
                    }
                }
            }
            catch { }
        }

        // ── 4. Montar lista de linhas: catálogo + extras alfabético ───────────
        var linhasItens = ItemCatalog.OrderedItems
            .Concat(extrasGlobal.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // ── 5. Gerar Excel ────────────────────────────────────────────────────
        progresso?.Invoke("Gerando Excel...");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Pesagens por Dia");
        ws.Workbook.Properties.Company = "LFB Reciclagem Eletrônica";

        // Cores — compras (verde)
        var headerFill  = XLColor.FromHtml("#2E7D32");
        var headerFont  = XLColor.White;
        var totalFill   = XLColor.FromHtml("#E8F5E9");
        var extraFill   = XLColor.FromHtml("#FFF8E1");
        var borderColor = XLColor.FromHtml("#BDBDBD");

        // Cores — vendas (laranja)
        var vendaHeaderFill      = XLColor.FromHtml("#E65100");
        var vendaTotalColFill    = XLColor.FromHtml("#FFF3E0");
        var vendaGrandTotalFill  = XLColor.FromHtml("#FFE0B2");
        var vendaTotalHeaderFill = XLColor.FromHtml("#BF360C");

        // Cores — estoque inicial (roxo)
        var estoqueInicialHeaderFill = XLColor.FromHtml("#6A1B9A");
        var estoqueInicialCellFill   = XLColor.FromHtml("#F3E5F5");

        // Cores — estoque atual (azul)
        var estoqueHeaderFill     = XLColor.FromHtml("#0D47A1");
        var estoqueCellFill       = XLColor.FromHtml("#E3F2FD");
        var estoqueGrandTotalFill = XLColor.FromHtml("#BBDEFB");

        // Colunas:
        // Col 1        = Item
        // Col 2        = TOTAL (compras)
        // Col 3..N+2   = dias de venda
        // Col N+3      = TOTAL VENDAS
        // Col N+4      = Estoque Inicial
        // Col N+5      = Estoque Atual
        int colComprasTotal = 2;
        int colVendaStart   = 3;
        int colTotalVendas  = colVendaStart + datasVenda.Count;
        int colEstoqueInicial = colTotalVendas + 1;
        int colEstoqueAtual   = colEstoqueInicial + 1;

        // ── Cabeçalho ─────────────────────────────────────────────────────────
        var cellItem = ws.Cell(1, 1);
        cellItem.Value = "Item";
        EstiloHeader(cellItem, headerFill, headerFont);

        var cellTotalCompras = ws.Cell(1, colComprasTotal);
        cellTotalCompras.Value = "TOTAL";
        EstiloHeader(cellTotalCompras, XLColor.FromHtml("#1B5E20"), headerFont);

        for (int d = 0; d < datasVenda.Count; d++)
        {
            var cell = ws.Cell(1, colVendaStart + d);
            cell.Value = datasVenda[d];
            EstiloHeader(cell, vendaHeaderFill, headerFont);
        }

        if (datasVenda.Count > 0)
        {
            var cellTotalVendasHdr = ws.Cell(1, colTotalVendas);
            cellTotalVendasHdr.Value = "TOTAL VENDAS";
            EstiloHeader(cellTotalVendasHdr, vendaTotalHeaderFill, headerFont);
        }

        var cellEstoqueInicialHdr = ws.Cell(1, colEstoqueInicial);
        cellEstoqueInicialHdr.Value = "Estoque Inicial";
        EstiloHeader(cellEstoqueInicialHdr, estoqueInicialHeaderFill, headerFont);

        var cellEstoqueAtualHdr = ws.Cell(1, colEstoqueAtual);
        cellEstoqueAtualHdr.Value = "Estoque Atual";
        EstiloHeader(cellEstoqueAtualHdr, estoqueHeaderFill, headerFont);

        // ── Linhas de itens ───────────────────────────────────────────────────
        var extrasExistem = extrasGlobal.Count > 0;
        int excelRow = 2;

        var itensFixos  = linhasItens.Where(n =>  ItemCatalog.OrderedItems.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();
        var itensExtras = linhasItens.Where(n => !ItemCatalog.OrderedItems.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();

        void EscreverLinhaItem(string nome, bool isExtra)
        {
            var cellNome = ws.Cell(excelRow, 1);
            cellNome.Value = nome;
            cellNome.Style.Font.Bold = !isExtra;
            if (isExtra) cellNome.Style.Fill.BackgroundColor = extraFill;

            // — TOTAL compras —
            var totalCompras = comprasTotais.TryGetValue(nome, out var tc) ? tc : 0m;
            var cellComp = ws.Cell(excelRow, colComprasTotal);
            if (totalCompras > 0) { cellComp.SetValue(totalCompras.ToString("N3", PtBR)); cellComp.Style.Font.Bold = true; }
            cellComp.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cellComp.Style.Fill.BackgroundColor = totalFill;
            if (isExtra) cellComp.Style.Fill.BackgroundColor = extraFill;

            // — colunas de venda por dia —
            decimal rowTotalVenda = 0m;
            for (int d = 0; d < datasVenda.Count; d++)
            {
                var val = porDiaVenda.TryGetValue(datasVenda[d], out var dv) && dv.TryGetValue(nome, out var vv) ? vv : 0m;
                rowTotalVenda += val;
                var cell = ws.Cell(excelRow, colVendaStart + d);
                if (val > 0) { cell.SetValue(val.ToString("N3", PtBR)); }
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                if (isExtra) cell.Style.Fill.BackgroundColor = extraFill;
            }

            // — TOTAL VENDAS —
            if (datasVenda.Count > 0)
            {
                var cellTV = ws.Cell(excelRow, colTotalVendas);
                if (rowTotalVenda > 0) { cellTV.SetValue(rowTotalVenda.ToString("N3", PtBR)); cellTV.Style.Font.Bold = true; }
                cellTV.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cellTV.Style.Fill.BackgroundColor = vendaTotalColFill;
            }

            // — Estoque Inicial —
            var ei = estoqueInicial.TryGetValue(nome, out var eiVal) ? eiVal : 0m;
            var cellEI = ws.Cell(excelRow, colEstoqueInicial);
            if (ei != 0m) { cellEI.SetValue(ei.ToString("N3", PtBR)); cellEI.Style.Font.Bold = true; }
            cellEI.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cellEI.Style.Fill.BackgroundColor = estoqueInicialCellFill;

            // — Estoque Atual = TOTAL VENDAS − (TOTAL Compras + Estoque Inicial) —
            decimal estoqueAtualVal = rowTotalVenda - (totalCompras + ei);
            var cellEA = ws.Cell(excelRow, colEstoqueAtual);
            if (estoqueAtualVal != 0m) { cellEA.SetValue(estoqueAtualVal.ToString("N3", PtBR)); cellEA.Style.Font.Bold = true; }
            cellEA.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cellEA.Style.Fill.BackgroundColor = estoqueCellFill;

            excelRow++;
        }

        foreach (var nome in itensFixos)
            EscreverLinhaItem(nome, isExtra: false);

        if (extrasExistem)
        {
            int lastCol = colEstoqueAtual;
            var sepRange = ws.Range(excelRow, 1, excelRow, lastCol);
            sepRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3E5F5");
            ws.Cell(excelRow, 1).Value = "── Outros itens ──";
            ws.Cell(excelRow, 1).Style.Font.Italic = true;
            ws.Cell(excelRow, 1).Style.Font.FontColor = XLColor.FromHtml("#6A1B9A");
            excelRow++;

            foreach (var nome in itensExtras)
                EscreverLinhaItem(nome, isExtra: true);
        }

        // ── Linha TOTAL ───────────────────────────────────────────────────────
        int totalRow = excelRow;

        var cellTotalLabel = ws.Cell(totalRow, 1);
        cellTotalLabel.Value = "TOTAL";
        cellTotalLabel.Style.Font.Bold = true;
        cellTotalLabel.Style.Fill.BackgroundColor = totalFill;

        // TOTAL compras
        decimal grandTotalCompras = comprasTotais.Values.Sum();
        var cellGTC = ws.Cell(totalRow, colComprasTotal);
        cellGTC.SetValue(grandTotalCompras.ToString("N3", PtBR));
        cellGTC.Style.Font.Bold = true;
        cellGTC.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cellGTC.Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");

        // TOTAL por dia de venda
        decimal grandTotalVenda = 0m;
        for (int d = 0; d < datasVenda.Count; d++)
        {
            var colTotal = porDiaVenda.TryGetValue(datasVenda[d], out var dv) ? dv.Values.Sum() : 0m;
            grandTotalVenda += colTotal;
            var cell = ws.Cell(totalRow, colVendaStart + d);
            cell.SetValue(colTotal.ToString("N3", PtBR));
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cell.Style.Fill.BackgroundColor = vendaTotalColFill;
        }

        if (datasVenda.Count > 0)
        {
            var cellGTV = ws.Cell(totalRow, colTotalVendas);
            cellGTV.SetValue(grandTotalVenda.ToString("N3", PtBR));
            cellGTV.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cellGTV.Style.Font.Bold = true;
            cellGTV.Style.Fill.BackgroundColor = vendaGrandTotalFill;
        }

        // TOTAL estoque inicial
        decimal grandTotalEI = estoqueInicial.Values.Sum();
        var cellGEI = ws.Cell(totalRow, colEstoqueInicial);
        cellGEI.SetValue(grandTotalEI.ToString("N3", PtBR));
        cellGEI.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cellGEI.Style.Font.Bold = true;
        cellGEI.Style.Fill.BackgroundColor = XLColor.FromHtml("#E1BEE7");

        // TOTAL estoque atual = grandTotalVenda − (grandTotalCompras + grandTotalEI)
        decimal grandTotalEstoqueAtual = grandTotalVenda - (grandTotalCompras + grandTotalEI);
        var cellGrandEstoque = ws.Cell(totalRow, colEstoqueAtual);
        cellGrandEstoque.SetValue(grandTotalEstoqueAtual.ToString("N3", PtBR));
        cellGrandEstoque.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cellGrandEstoque.Style.Font.Bold = true;
        cellGrandEstoque.Style.Fill.BackgroundColor = estoqueGrandTotalFill;

        // ── Bordas em toda a tabela ───────────────────────────────────────────
        var tableRange = ws.Range(1, 1, totalRow, colEstoqueAtual);
        tableRange.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
        tableRange.Style.Border.OutsideBorderColor = borderColor;
        tableRange.Style.Border.InsideBorder       = XLBorderStyleValues.Hair;
        tableRange.Style.Border.InsideBorderColor  = borderColor;

        // ── Largura de colunas ────────────────────────────────────────────────
        ws.Column(1).Width = 38;
        ws.Column(colComprasTotal).Width = 14;
        for (int d = 0; d < datasVenda.Count; d++)
            ws.Column(colVendaStart + d).Width = 14;
        if (datasVenda.Count > 0)
            ws.Column(colTotalVendas).Width = 16;
        ws.Column(colEstoqueInicial).Width = 16;
        ws.Column(colEstoqueAtual).Width   = 16;

        ws.SheetView.Freeze(1, 1);

        wb.SaveAs(outputPath);
        progresso?.Invoke($"Excel salvo: {Path.GetFileName(outputPath)}");
    }

    private static decimal ExtrairDecimal(JsonNode? node)
    {
        if (node is null) return 0m;
        if (node is JsonValue jv)
        {
            if (jv.TryGetValue<decimal>(out var d)) return d;
            if (jv.TryGetValue<string>(out var s) &&
                decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dp)) return dp;
        }
        return 0m;
    }

    private static DateTime ParseData(string dataStr)
    {
        if (DateTime.TryParseExact(dataStr, "dd/MM/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        return DateTime.MinValue;
    }

    private static void EstiloHeader(IXLCell cell, XLColor bgColor, XLColor fontColor)
    {
        cell.Style.Fill.BackgroundColor = bgColor;
        cell.Style.Font.FontColor       = fontColor;
        cell.Style.Font.Bold            = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
}
