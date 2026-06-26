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
        // Agora com data-recibo por registro → gera colunas por dia de compra
        progresso?.Invoke($"Lendo compras do mês {mesAno}...");
        // porDiaCompra: dataLabel("dd/MM/yyyy") → {item → kg}
        var porDiaCompra    = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);
        // porDiaValorCompra: dataLabel → soma R$ dos recibos desse dia
        var porDiaValorCompra = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var extrasGlobal      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                        if (reg is not JsonObject regObj) continue;

                        // Obter label de data: campo "data-recibo" (DD-MM-YYYY) → "DD/MM/YYYY"
                        string dataLabel = string.Empty;
                        if (regObj.ContainsKey("data-recibo"))
                        {
                            var dr = regObj["data-recibo"]!.GetValue<string>();
                            if (DateTime.TryParseExact(dr, "dd-MM-yyyy",
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtc))
                                dataLabel = dtc.ToString("dd/MM/yyyy");
                        }
                        if (string.IsNullOrEmpty(dataLabel)) dataLabel = "s/data";

                        // Somar valor R$ do recibo nesse dia
                        if (regObj.ContainsKey("valor"))
                        {
                            var valStr = regObj["valor"]!.GetValue<string>();
                            // Formato "2.046,52" → parse pt-BR
                            var valNorm = valStr.Replace(".", "").Replace(",", ".");
                            if (decimal.TryParse(valNorm, NumberStyles.Any, CultureInfo.InvariantCulture, out var valDec))
                            {
                                porDiaValorCompra[dataLabel] =
                                    porDiaValorCompra.TryGetValue(dataLabel, out var vAtual) ? vAtual + valDec : valDec;
                            }
                        }

                        if (!regObj.ContainsKey("materiais")) continue;
                        if (!porDiaCompra.TryGetValue(dataLabel, out var diaDict))
                        {
                            diaDict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                            porDiaCompra[dataLabel] = diaDict;
                        }

                        foreach (var mat in regObj["materiais"]!.AsArray())
                        {
                            if (mat is JsonObject matObj &&
                                matObj.ContainsKey("descricao") && matObj.ContainsKey("peso"))
                            {
                                var nome = matObj["descricao"]!.GetValue<string>();
                                var peso = ExtrairDecimal(matObj["peso"]);
                                diaDict[nome] = diaDict.TryGetValue(nome, out var c) ? c + peso : peso;
                                if (!ItemCatalog.OrderedItems.Contains(nome, StringComparer.OrdinalIgnoreCase))
                                    extrasGlobal.Add(nome);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        var datasCompra = porDiaCompra.Keys
            .OrderBy(d => d == "s/data" ? DateTime.MaxValue : ParseData(d))
            .ToList();

        // Totais de compras por item (soma de todos os dias)
        var comprasTotais = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var diaKv in porDiaCompra)
            foreach (var itemKv in diaKv.Value)
                comprasTotais[itemKv.Key] =
                    comprasTotais.TryGetValue(itemKv.Key, out var ex) ? ex + itemKv.Value : itemKv.Value;

        // ── 2. Ler vendas do mês: venda-DD-MM-yyyy.json agrupadas por dia ─────
        progresso?.Invoke($"Lendo vendas do mês {mesAno}...");
        var porDiaVenda = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(bancoDadosDir))
        {
            foreach (var file in Directory.GetFiles(bancoDadosDir, $"venda-*-{mesAno}.json", SearchOption.TopDirectoryOnly)
                                          .OrderBy(f => f))
            {
                var semExt = Path.GetFileNameWithoutExtension(file);
                var mData  = Regex.Match(semExt, @"^venda-(\d{2}-\d{2}-\d{4})$");
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

        // ── 3. Ler estoque inicial do mês ─────────────────────────────────────
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
                            kvp.Key.Equals("mes",  StringComparison.OrdinalIgnoreCase) ||
                            kvp.Key.Equals("ano",  StringComparison.OrdinalIgnoreCase)) continue;
                        estoqueInicial[kvp.Key] = ExtrairDecimal(kvp.Value);
                    }
                }
            }
            catch { }
        }

        // ── 4. Montar lista de linhas ─────────────────────────────────────────
        var linhasItens = ItemCatalog.OrderedItems
            .Concat(extrasGlobal.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // ── 5. Gerar Excel ────────────────────────────────────────────────────
        progresso?.Invoke("Gerando Excel...");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Pesagens por Dia");
        ws.Workbook.Properties.Company = "LFB Reciclagem Eletrônica";

        // Cores — compras (verde)
        var headerFill   = XLColor.FromHtml("#2E7D32");
        var headerFont   = XLColor.White;
        var compDayFill  = XLColor.FromHtml("#C8E6C9");
        var totalFill    = XLColor.FromHtml("#E8F5E9");
        var extraFill    = XLColor.FromHtml("#FFF8E1");
        var borderColor  = XLColor.FromHtml("#BDBDBD");
        var totalRsFill  = XLColor.FromHtml("#A5D6A7");

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

        // Layout de colunas:
        // Col 1                          = Item
        // Col 2 .. 1+datasCompra.Count   = dias de compra
        // Col 2+datasCompra.Count        = TOTAL (compras kg)
        // Col 3+datasCompra.Count ..     = dias de venda
        // Col 3+datasCompra.Count+datasVenda.Count = TOTAL VENDAS
        // +1 = Estoque Inicial
        // +1 = Estoque Atual
        int colCompDayStart  = 2;
        int colComprasTotal  = colCompDayStart + datasCompra.Count;
        int colVendaStart    = colComprasTotal + 1;
        int colTotalVendas   = colVendaStart + datasVenda.Count;
        int colEstoqueInicial = colTotalVendas + 1;
        int colEstoqueAtual   = colEstoqueInicial + 1;

        // ── Cabeçalho ─────────────────────────────────────────────────────────
        EstiloHeader(ws.Cell(1, 1), headerFill, headerFont);
        ws.Cell(1, 1).Value = "Item";

        for (int d = 0; d < datasCompra.Count; d++)
        {
            var cell = ws.Cell(1, colCompDayStart + d);
            cell.Value = datasCompra[d];
            EstiloHeader(cell, headerFill, headerFont);
        }

        EstiloHeader(ws.Cell(1, colComprasTotal), XLColor.FromHtml("#1B5E20"), headerFont);
        ws.Cell(1, colComprasTotal).Value = "TOTAL";

        for (int d = 0; d < datasVenda.Count; d++)
        {
            var cell = ws.Cell(1, colVendaStart + d);
            cell.Value = datasVenda[d];
            EstiloHeader(cell, vendaHeaderFill, headerFont);
        }

        if (datasVenda.Count > 0)
        {
            EstiloHeader(ws.Cell(1, colTotalVendas), vendaTotalHeaderFill, headerFont);
            ws.Cell(1, colTotalVendas).Value = "TOTAL VENDAS";
        }

        EstiloHeader(ws.Cell(1, colEstoqueInicial), estoqueInicialHeaderFill, headerFont);
        ws.Cell(1, colEstoqueInicial).Value = "Estoque Inicial";

        EstiloHeader(ws.Cell(1, colEstoqueAtual), estoqueHeaderFill, headerFont);
        ws.Cell(1, colEstoqueAtual).Value = "Estoque Atual";

        // ── Linhas de itens ───────────────────────────────────────────────────
        var extrasExistem = extrasGlobal.Count > 0;
        int excelRow = 2;

        var itensFixos  = linhasItens.Where(n =>  ItemCatalog.OrderedItems.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();
        var itensExtras = linhasItens.Where(n => !ItemCatalog.OrderedItems.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();

        void EscreverLinhaItem(string nome, bool isExtra)
        {
            var cellNome = ws.Cell(excelRow, 1);
            cellNome.Value           = nome;
            cellNome.Style.Font.Bold = !isExtra;
            if (isExtra) cellNome.Style.Fill.BackgroundColor = extraFill;

            // — colunas de compra por dia —
            decimal rowTotalCompra = 0m;
            for (int d = 0; d < datasCompra.Count; d++)
            {
                var val = porDiaCompra.TryGetValue(datasCompra[d], out var dc) && dc.TryGetValue(nome, out var vc) ? vc : 0m;
                rowTotalCompra += val;
                var cell = ws.Cell(excelRow, colCompDayStart + d);
                if (val > 0) { cell.SetValue(val.ToString("N3", PtBR)); cell.Style.Font.Bold = true; }
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cell.Style.Fill.BackgroundColor = isExtra ? extraFill : compDayFill;
            }

            // — TOTAL compras —
            var cellComp = ws.Cell(excelRow, colComprasTotal);
            if (rowTotalCompra > 0) { cellComp.SetValue(rowTotalCompra.ToString("N3", PtBR)); cellComp.Style.Font.Bold = true; }
            cellComp.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cellComp.Style.Fill.BackgroundColor = isExtra ? extraFill : totalFill;

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
            decimal estoqueAtualVal = rowTotalVenda - (rowTotalCompra + ei);
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
            var sepRange = ws.Range(excelRow, 1, excelRow, colEstoqueAtual);
            sepRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3E5F5");
            ws.Cell(excelRow, 1).Value = "── Outros itens ──";
            ws.Cell(excelRow, 1).Style.Font.Italic    = true;
            ws.Cell(excelRow, 1).Style.Font.FontColor = XLColor.FromHtml("#6A1B9A");
            excelRow++;

            foreach (var nome in itensExtras)
                EscreverLinhaItem(nome, isExtra: true);
        }

        // ── Linha TOTAL (kg) ──────────────────────────────────────────────────
        int totalRow = excelRow;

        ws.Cell(totalRow, 1).Value = "TOTAL";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        ws.Cell(totalRow, 1).Style.Fill.BackgroundColor = totalFill;

        // TOTAL kg por dia de compra
        decimal grandTotalCompras = 0m;
        for (int d = 0; d < datasCompra.Count; d++)
        {
            var colKg = porDiaCompra.TryGetValue(datasCompra[d], out var dc) ? dc.Values.Sum() : 0m;
            grandTotalCompras += colKg;
            var cell = ws.Cell(totalRow, colCompDayStart + d);
            cell.SetValue(colKg.ToString("N3", PtBR));
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cell.Style.Fill.BackgroundColor = compDayFill;
        }

        var cellGTC = ws.Cell(totalRow, colComprasTotal);
        cellGTC.SetValue(grandTotalCompras.ToString("N3", PtBR));
        cellGTC.Style.Font.Bold = true;
        cellGTC.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cellGTC.Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");

        // TOTAL kg por dia de venda
        decimal grandTotalVenda = 0m;
        for (int d = 0; d < datasVenda.Count; d++)
        {
            var colKg = porDiaVenda.TryGetValue(datasVenda[d], out var dv) ? dv.Values.Sum() : 0m;
            grandTotalVenda += colKg;
            var cell = ws.Cell(totalRow, colVendaStart + d);
            cell.SetValue(colKg.ToString("N3", PtBR));
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cell.Style.Fill.BackgroundColor = vendaTotalColFill;
        }

        if (datasVenda.Count > 0)
        {
            var cellGTV = ws.Cell(totalRow, colTotalVendas);
            cellGTV.SetValue(grandTotalVenda.ToString("N3", PtBR));
            cellGTV.Style.Font.Bold = true;
            cellGTV.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cellGTV.Style.Fill.BackgroundColor = vendaGrandTotalFill;
        }

        decimal grandTotalEI = estoqueInicial.Values.Sum();
        var cellGEI = ws.Cell(totalRow, colEstoqueInicial);
        cellGEI.SetValue(grandTotalEI.ToString("N3", PtBR));
        cellGEI.Style.Font.Bold = true;
        cellGEI.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cellGEI.Style.Fill.BackgroundColor = XLColor.FromHtml("#E1BEE7");

        decimal grandTotalEstoqueAtual = grandTotalVenda - (grandTotalCompras + grandTotalEI);
        var cellGrandEstoque = ws.Cell(totalRow, colEstoqueAtual);
        cellGrandEstoque.SetValue(grandTotalEstoqueAtual.ToString("N3", PtBR));
        cellGrandEstoque.Style.Font.Bold = true;
        cellGrandEstoque.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cellGrandEstoque.Style.Fill.BackgroundColor = estoqueGrandTotalFill;

        // ── Linha TOTAL R$ (valor monetário das compras por dia) ──────────────
        int totalRsRow = totalRow + 1;

        ws.Cell(totalRsRow, 1).Value = "TOTAL R$";
        ws.Cell(totalRsRow, 1).Style.Font.Bold = true;
        ws.Cell(totalRsRow, 1).Style.Fill.BackgroundColor = totalRsFill;

        decimal grandTotalRs = 0m;
        for (int d = 0; d < datasCompra.Count; d++)
        {
            var valDia = porDiaValorCompra.TryGetValue(datasCompra[d], out var vd) ? vd : 0m;
            grandTotalRs += valDia;
            var cell = ws.Cell(totalRsRow, colCompDayStart + d);
            if (valDia > 0)
            {
                cell.SetValue($"R$ {valDia.ToString("N2", PtBR)}");
                cell.Style.Font.Bold = true;
            }
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cell.Style.Fill.BackgroundColor = totalRsFill;
        }

        // TOTAL R$ global na coluna TOTAL compras
        var cellGrandRs = ws.Cell(totalRsRow, colComprasTotal);
        cellGrandRs.SetValue($"R$ {grandTotalRs.ToString("N2", PtBR)}");
        cellGrandRs.Style.Font.Bold = true;
        cellGrandRs.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cellGrandRs.Style.Fill.BackgroundColor = totalRsFill;

        // Estilizar resto da linha TOTAL R$ (colunas de venda e estoque) com fundo neutro
        for (int c = colVendaStart; c <= colEstoqueAtual; c++)
            ws.Cell(totalRsRow, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");

        // ── Bordas em toda a tabela (inclui linha TOTAL R$) ───────────────────
        var tableRange = ws.Range(1, 1, totalRsRow, colEstoqueAtual);
        tableRange.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
        tableRange.Style.Border.OutsideBorderColor = borderColor;
        tableRange.Style.Border.InsideBorder       = XLBorderStyleValues.Hair;
        tableRange.Style.Border.InsideBorderColor  = borderColor;

        // ── Largura de colunas ────────────────────────────────────────────────
        ws.Column(1).Width = 38;
        for (int d = 0; d < datasCompra.Count; d++)
            ws.Column(colCompDayStart + d).Width = 14;
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
