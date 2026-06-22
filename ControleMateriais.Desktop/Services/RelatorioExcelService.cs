using ClosedXML.Excel;
using ControleMateriais.Desktop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ControleMateriais.Desktop.Services;

public static class RelatorioExcelService
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>
    /// Gera o Excel de pesagens por dia e salva em <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="recibosDir">Diretório raiz dos recibos (ex: …/Recibos)</param>
    /// <param name="outputPath">Caminho completo do .xlsx a salvar</param>
    /// <param name="progresso">Callback opcional para reportar progresso (mensagem)</param>
    public static void Gerar(string recibosDir, string outputPath, Action<string>? progresso = null)
    {
        // ── 1. Listar PDFs de pesagem (ignora subpastas como Recibos_Venda) ──
        var pdfs = Directory.GetFiles(recibosDir, "*.pdf", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();

        // ── 2. Extrair dados de cada PDF de pesagem ───────────────────────────
        // dataStr → {nomeItem → pesoTotal do dia}
        var porDia = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);
        // todos os nomes de itens extras encontrados
        var extrasGlobal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int contador = 0;
        foreach (var pdf in pdfs)
        {
            contador++;
            var dataStr = ExtrairDataDoNome(pdf);
            progresso?.Invoke($"Lendo {contador}/{pdfs.Count}: {Path.GetFileName(pdf)}");

            Dictionary<string, decimal> itens;
            try { itens = ReciboParserService.ExtrairPesos(pdf); }
            catch { continue; }

            if (!porDia.TryGetValue(dataStr, out var diaDict))
            {
                diaDict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                porDia[dataStr] = diaDict;
            }

            foreach (var kv in itens)
            {
                diaDict[kv.Key] = diaDict.TryGetValue(kv.Key, out var atual) ? atual + kv.Value : kv.Value;

                if (!ItemCatalog.OrderedItems.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                    extrasGlobal.Add(kv.Key);
            }
        }

        // ── 3. Ordenar datas de pesagem de mais antiga a mais recente ─────────
        var datas = porDia.Keys
            .OrderBy(d => ParseData(d))
            .ToList();

        // ── 4. Listar PDFs de venda (Recibos_Venda/) ─────────────────────────
        var vendaDir  = Path.Combine(recibosDir, "Recibos_Venda");
        var pdfsVenda = Directory.Exists(vendaDir)
            ? Directory.GetFiles(vendaDir, "*.pdf", SearchOption.TopDirectoryOnly).OrderBy(f => f).ToList()
            : new List<string>();

        var porDiaVenda = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);

        int contadorVenda = 0;
        foreach (var pdf in pdfsVenda)
        {
            contadorVenda++;
            var dataStr = ExtrairDataDoNome(pdf);
            progresso?.Invoke($"Venda {contadorVenda}/{pdfsVenda.Count}: {Path.GetFileName(pdf)}");

            Dictionary<string, decimal> itens;
            try { itens = ReciboParserService.ExtrairPesos(pdf); }
            catch { continue; }

            if (!porDiaVenda.TryGetValue(dataStr, out var diaDict))
            {
                diaDict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                porDiaVenda[dataStr] = diaDict;
            }

            foreach (var kv in itens)
                diaDict[kv.Key] = diaDict.TryGetValue(kv.Key, out var atual) ? atual + kv.Value : kv.Value;
        }

        var datasVenda = porDiaVenda.Keys
            .OrderBy(d => ParseData(d))
            .ToList();

        // ── 5. Montar lista de linhas: catálogo + extras alfabético ───────────
        var linhasItens = ItemCatalog.OrderedItems
            .Concat(extrasGlobal.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // ── 6. Gerar Excel ────────────────────────────────────────────────────
        progresso?.Invoke("Gerando Excel...");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Pesagens por Dia");
        ws.Workbook.Properties.Company = "LFB Reciclagem Eletrônica";

        // Cores — pesagens (verde)
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

        // Cores — estoque atual (azul)
        var estoqueHeaderFill     = XLColor.FromHtml("#0D47A1");
        var estoqueCellFill       = XLColor.FromHtml("#E3F2FD");
        var estoqueGrandTotalFill = XLColor.FromHtml("#BBDEFB");

        int colOffset     = 2;                              // coluna A = nomes, B em diante = datas pesagem
        int colVendaStart = colOffset + datas.Count + 1;   // primeira coluna de venda (após TOTAL verde)

        // ── Cabeçalho linha 1: "Item" + datas pesagem + "TOTAL" ──────────────
        var cellItem = ws.Cell(1, 1);
        cellItem.Value = "Item";
        EstiloHeader(cellItem, headerFill, headerFont);

        for (int d = 0; d < datas.Count; d++)
        {
            var cell = ws.Cell(1, colOffset + d);
            cell.Value = datas[d];
            EstiloHeader(cell, headerFill, headerFont);
        }

        var cellTotal = ws.Cell(1, colOffset + datas.Count);
        cellTotal.Value = "TOTAL";
        EstiloHeader(cellTotal, XLColor.FromHtml("#1B5E20"), headerFont);

        // ── Cabeçalho linha 1: datas venda + "TOTAL VENDAS" ──────────────────
        for (int d = 0; d < datasVenda.Count; d++)
        {
            var cell = ws.Cell(1, colVendaStart + d);
            cell.Value = datasVenda[d];
            EstiloHeader(cell, vendaHeaderFill, headerFont);
        }

        if (datasVenda.Count > 0)
        {
            var cellTotalVendas = ws.Cell(1, colVendaStart + datasVenda.Count);
            cellTotalVendas.Value = "TOTAL VENDAS";
            EstiloHeader(cellTotalVendas, vendaTotalHeaderFill, headerFont);
        }

        // ── Cabeçalho: "Estoque Atual" ────────────────────────────────────────
        int colEstoqueAtual = (datasVenda.Count > 0 ? colVendaStart + datasVenda.Count + 1 : colOffset + datas.Count + 1);
        var cellEstoqueHeader = ws.Cell(1, colEstoqueAtual);
        cellEstoqueHeader.Value = "Estoque Atual";
        EstiloHeader(cellEstoqueHeader, estoqueHeaderFill, headerFont);

        // ── Linhas de itens ───────────────────────────────────────────────────
        var extrasExistem = extrasGlobal.Count > 0;
        int excelRow = 2;

        var itensFixos = linhasItens
            .Where(n => ItemCatalog.OrderedItems.Contains(n, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var itensExtras = linhasItens
            .Where(n => !ItemCatalog.OrderedItems.Contains(n, StringComparer.OrdinalIgnoreCase))
            .ToList();

        void EscreverLinhaItem(string nome, bool isExtra)
        {
            var cellNome = ws.Cell(excelRow, 1);
            cellNome.Value = nome;
            cellNome.Style.Font.Bold = !isExtra;
            if (isExtra) cellNome.Style.Fill.BackgroundColor = extraFill;

            // — colunas de pesagem —
            decimal rowTotal = 0m;
            for (int d = 0; d < datas.Count; d++)
            {
                var val = porDia.TryGetValue(datas[d], out var dd) && dd.TryGetValue(nome, out var v) ? v : 0m;
                rowTotal += val;
                var cell = ws.Cell(excelRow, colOffset + d);
                if (val > 0) { cell.SetValue(val.ToString("N3", PtBR)); }
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                if (isExtra) cell.Style.Fill.BackgroundColor = extraFill;
            }
            var cellRowTotal = ws.Cell(excelRow, colOffset + datas.Count);
            if (rowTotal > 0) { cellRowTotal.SetValue(rowTotal.ToString("N3", PtBR)); cellRowTotal.Style.Font.Bold = true; }
            cellRowTotal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cellRowTotal.Style.Fill.BackgroundColor = totalFill;

            // — colunas de venda —
            decimal rowTotalVenda = 0m;
            for (int d = 0; d < datasVenda.Count; d++)
            {
                var val = porDiaVenda.TryGetValue(datasVenda[d], out var dv) && dv.TryGetValue(nome, out var vv) ? vv : 0m;
                rowTotalVenda += val;
                var cell = ws.Cell(excelRow, colVendaStart + d);
                if (val > 0) { cell.SetValue(val.ToString("N3", PtBR)); }
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
            if (datasVenda.Count > 0)
            {
                var cellRowTotalVenda = ws.Cell(excelRow, colVendaStart + datasVenda.Count);
                if (rowTotalVenda > 0) { cellRowTotalVenda.SetValue(rowTotalVenda.ToString("N3", PtBR)); cellRowTotalVenda.Style.Font.Bold = true; }
                cellRowTotalVenda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cellRowTotalVenda.Style.Fill.BackgroundColor = vendaTotalColFill;
            }

            // — coluna Estoque Atual —
            decimal estoqueAtual = Math.Max(0m, rowTotal - rowTotalVenda);
            var cellEstoqueAtual = ws.Cell(excelRow, colEstoqueAtual);
            if (estoqueAtual > 0) { cellEstoqueAtual.SetValue(estoqueAtual.ToString("N3", PtBR)); cellEstoqueAtual.Style.Font.Bold = true; }
            cellEstoqueAtual.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cellEstoqueAtual.Style.Fill.BackgroundColor = estoqueCellFill;

            excelRow++;
        }

        foreach (var nome in itensFixos)
            EscreverLinhaItem(nome, isExtra: false);

        if (extrasExistem)
        {
            // Linha separadora — estende até a coluna Estoque Atual
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

        // ── Linha TOTAL pesagem (soma por coluna) ─────────────────────────────
        int totalRow = excelRow;

        var cellTotalLabel = ws.Cell(totalRow, 1);
        cellTotalLabel.Value = "TOTAL";
        cellTotalLabel.Style.Font.Bold = true;
        cellTotalLabel.Style.Fill.BackgroundColor = totalFill;

        decimal grandTotal = 0m;
        for (int d = 0; d < datas.Count; d++)
        {
            var colTotal = porDia.TryGetValue(datas[d], out var dd)
                ? dd.Values.Sum()
                : 0m;
            grandTotal += colTotal;

            var cell = ws.Cell(totalRow, colOffset + d);
            cell.SetValue(colTotal.ToString("N3", PtBR));
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cell.Style.Fill.BackgroundColor = totalFill;
        }

        var cellGrandTotal = ws.Cell(totalRow, colOffset + datas.Count);
        cellGrandTotal.SetValue(grandTotal.ToString("N3", PtBR));
        cellGrandTotal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cellGrandTotal.Style.Font.Bold = true;
        cellGrandTotal.Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");

        // ── Linha TOTAL vendas (soma por coluna de venda) ─────────────────────
        if (datasVenda.Count > 0)
        {
            decimal grandTotalVenda = 0m;
            for (int d = 0; d < datasVenda.Count; d++)
            {
                var colTotal = porDiaVenda.TryGetValue(datasVenda[d], out var dv)
                    ? dv.Values.Sum()
                    : 0m;
                grandTotalVenda += colTotal;

                var cell = ws.Cell(totalRow, colVendaStart + d);
                cell.SetValue(colTotal.ToString("N3", PtBR));
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cell.Style.Fill.BackgroundColor = vendaTotalColFill;
            }

            var cellGrandTotalVenda = ws.Cell(totalRow, colVendaStart + datasVenda.Count);
            cellGrandTotalVenda.SetValue(grandTotalVenda.ToString("N3", PtBR));
            cellGrandTotalVenda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cellGrandTotalVenda.Style.Font.Bold = true;
            cellGrandTotalVenda.Style.Fill.BackgroundColor = vendaGrandTotalFill;
        }

        // ── Linha TOTAL estoque atual ─────────────────────────────────────────
        decimal grandTotalEstoqueAtual = Math.Max(0m, grandTotal - (datasVenda.Count > 0 ? porDiaVenda.Values.SelectMany(d => d.Values).Sum() : 0m));
        var cellGrandEstoque = ws.Cell(totalRow, colEstoqueAtual);
        cellGrandEstoque.SetValue(grandTotalEstoqueAtual.ToString("N3", PtBR));
        cellGrandEstoque.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cellGrandEstoque.Style.Font.Bold = true;
        cellGrandEstoque.Style.Fill.BackgroundColor = estoqueGrandTotalFill;

        // ── Bordas em toda a tabela (pesagens + vendas + estoque) ─────────────
        int lastTableCol = colEstoqueAtual;
        var tableRange = ws.Range(1, 1, totalRow, lastTableCol);
        tableRange.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
        tableRange.Style.Border.OutsideBorderColor = borderColor;
        tableRange.Style.Border.InsideBorder       = XLBorderStyleValues.Hair;
        tableRange.Style.Border.InsideBorderColor  = borderColor;

        // ── Ajuste de largura de colunas ──────────────────────────────────────
        ws.Column(1).Width = 38;
        for (int d = 0; d < datas.Count; d++)
            ws.Column(colOffset + d).Width = 14;
        ws.Column(colOffset + datas.Count).Width = 14;   // TOTAL pesagem

        for (int d = 0; d < datasVenda.Count; d++)
            ws.Column(colVendaStart + d).Width = 14;
        if (datasVenda.Count > 0)
            ws.Column(colVendaStart + datasVenda.Count).Width = 16; // TOTAL VENDAS
        ws.Column(colEstoqueAtual).Width = 16;                      // Estoque Atual

        // Congela cabeçalho e coluna de nome
        ws.SheetView.Freeze(1, 1);

        wb.SaveAs(outputPath);
        progresso?.Invoke($"Excel salvo: {Path.GetFileName(outputPath)}");
    }

    private static string ExtrairDataDoNome(string filePath)
    {
        var semExt = Path.GetFileNameWithoutExtension(filePath);

        // Padrão _dd-MM-yyyy_HH-mm
        var m1 = Regex.Match(semExt, @"_(\d{2}-\d{2}-\d{4})_\d{2}-\d{2}$");
        if (m1.Success && DateTime.TryParseExact(m1.Groups[1].Value, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt1))
            return dt1.ToString("dd/MM/yyyy");

        // Padrão _dd-MM-yyyy
        var m2 = Regex.Match(semExt, @"_(\d{2}-\d{2}-\d{4})");
        if (m2.Success && DateTime.TryParseExact(m2.Groups[1].Value, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2))
            return dt2.ToString("dd/MM/yyyy");

        // Fallback: data de modificação do arquivo
        return File.GetLastWriteTime(filePath).ToString("dd/MM/yyyy");
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
