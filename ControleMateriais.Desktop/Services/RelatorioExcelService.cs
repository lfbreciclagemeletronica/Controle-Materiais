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
        // ── 1. Listar PDFs (ignora subpastas como Recibos_Venda) ─────────────
        var pdfs = Directory.GetFiles(recibosDir, "*.pdf", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();

        // ── 2. Extrair dados de cada PDF ──────────────────────────────────────
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

        // ── 3. Ordenar datas (colunas) de mais antiga a mais recente ─────────
        var datas = porDia.Keys
            .OrderBy(d => ParseData(d))
            .ToList();

        // ── 4. Montar lista de linhas: catálogo + extras alfabético ───────────
        var linhasItens = ItemCatalog.OrderedItems
            .Concat(extrasGlobal.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // ── 5. Gerar Excel ────────────────────────────────────────────────────
        progresso?.Invoke("Gerando Excel...");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Pesagens por Dia");

        // Estilo base
        var headerFill  = XLColor.FromHtml("#2E7D32");
        var headerFont  = XLColor.White;
        var totalFill   = XLColor.FromHtml("#E8F5E9");
        var extraFill   = XLColor.FromHtml("#FFF8E1");
        var borderColor = XLColor.FromHtml("#BDBDBD");

        int colOffset = 2; // coluna A = nome do item, dados a partir de B

        // ── Cabeçalho linha 1: "Item" + datas + "TOTAL" ─────────────────────
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

        // ── Linhas de itens ───────────────────────────────────────────────────
        var extrasExistem = extrasGlobal.Count > 0;
        int excelRow = 2; // linha 1 = cabeçalho

        // Itens do catálogo
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

            decimal rowTotal = 0m;
            for (int d = 0; d < datas.Count; d++)
            {
                var val = porDia.TryGetValue(datas[d], out var dd) && dd.TryGetValue(nome, out var v) ? v : 0m;
                rowTotal += val;
                var cell = ws.Cell(excelRow, colOffset + d);
                if (val > 0) { cell.Value = val; cell.Style.NumberFormat.Format = "#,##0.000"; }
                if (isExtra) cell.Style.Fill.BackgroundColor = extraFill;
            }
            var cellRowTotal = ws.Cell(excelRow, colOffset + datas.Count);
            if (rowTotal > 0) { cellRowTotal.Value = rowTotal; cellRowTotal.Style.NumberFormat.Format = "#,##0.000"; cellRowTotal.Style.Font.Bold = true; }
            cellRowTotal.Style.Fill.BackgroundColor = totalFill;
            excelRow++;
        }

        foreach (var nome in itensFixos)
            EscreverLinhaItem(nome, isExtra: false);

        if (extrasExistem)
        {
            // Linha separadora
            var sepRange = ws.Range(excelRow, 1, excelRow, colOffset + datas.Count);
            sepRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3E5F5");
            ws.Cell(excelRow, 1).Value = "── Outros itens ──";
            ws.Cell(excelRow, 1).Style.Font.Italic = true;
            ws.Cell(excelRow, 1).Style.Font.FontColor = XLColor.FromHtml("#6A1B9A");
            excelRow++;

            foreach (var nome in itensExtras)
                EscreverLinhaItem(nome, isExtra: true);
        }

        // ── Linha TOTAL (soma por coluna) ────────────────────────────────────
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
            cell.Value = colTotal;
            cell.Style.NumberFormat.Format = "#,##0.000";
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = totalFill;
        }

        var cellGrandTotal = ws.Cell(totalRow, colOffset + datas.Count);
        cellGrandTotal.Value = grandTotal;
        cellGrandTotal.Style.NumberFormat.Format = "#,##0.000";
        cellGrandTotal.Style.Font.Bold = true;
        cellGrandTotal.Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");

        // ── Bordas em toda a tabela ───────────────────────────────────────────
        var tableRange = ws.Range(1, 1, totalRow, colOffset + datas.Count);
        tableRange.Style.Border.OutsideBorder     = XLBorderStyleValues.Thin;
        tableRange.Style.Border.OutsideBorderColor = borderColor;
        tableRange.Style.Border.InsideBorder      = XLBorderStyleValues.Hair;
        tableRange.Style.Border.InsideBorderColor  = borderColor;

        // ── Ajuste automático de largura de colunas ───────────────────────────
        ws.Column(1).Width = 38;
        for (int d = 0; d < datas.Count; d++)
            ws.Column(colOffset + d).Width = 14;
        ws.Column(colOffset + datas.Count).Width = 14;

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
