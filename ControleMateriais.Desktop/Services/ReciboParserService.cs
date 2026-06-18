using ControleMateriais.Desktop;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ControleMateriais.Desktop.Services;

/// <summary>
/// Extrai os itens e pesos (kg) de um PDF de recibo de pesagem da LFB.
/// </summary>
public static class ReciboParserService
{
    // Linhas que identificam cabeçalhos/rodapés a ignorar
    private static readonly HashSet<string> _linhasIgnoradas = new(StringComparer.OrdinalIgnoreCase)
    {
        "material", "kg", "valor/kg", "total", "fornecedor", "peso", "valor", "data",
        "lfb reciclagem eletronica", "resultado da pesagem e triagem lfb",
        "cnpj", "impurezas"
    };

    // Regex: número com 3 casas decimais no formato brasileiro (ex: 22,710 ou 1.234,567)
    private static readonly Regex _rePeso = new(
        @"(?<!\d)([\d]{1,3}(?:\.\d{3})*,\d{3})(?!\d)|(?<!\d)(\d+,\d{3})(?!\d)",
        RegexOptions.Compiled);

    // Corrige artefato do iText: "2 2,710" → "22,710"
    private static readonly Regex _reEspacoDigitos = new(@"(\d) (\d)", RegexOptions.Compiled);

    // Aliases para nomes que podem aparecer diferente no PDF extraído
    private static readonly Dictionary<string, string[]> _aliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["HD sem placa/Sucateado"]                  = ["HD sem Placa Sucateado", "HD sem Placa", "HD Sucateado"],
            ["Celular Smart sem Bateria Botão e Flip"]  = ["Celular Sem bateria Botao e Flip", "Celular Sem bateria Botão e Flip", "Sem bateria Botao e Flip"],
            ["Celular Replicas com e sem Baterias"]     = ["Celular Replicas com e sem Bateria", "Replicas com e sem Baterias"],
            ["Memória Prata"]                           = ["Memorias Prata", "Memórias Prata", "Memoria Prateada"],
            ["Memórias Douradas"]                       = ["Memorias Douradas", "Memoria Dourada"],
            ["Desmanche Eletrônicos Consultar"]         = ["Desmanche Eletronicos Consultar", "Desmanche Eletrônicos Consultar Itens", "Desmanche Eletronicos"],
        };

    /// <summary>
    /// Lê o PDF e retorna {nomeMaterial → peso em kg}.
    /// Inclui itens do catálogo E itens extras (não catalogados) encontrados no PDF.
    /// </summary>
    public static Dictionary<string, decimal> ExtrairPesos(string filePath)
    {
        var resultado = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var linhas = ExtrairLinhasPdf(filePath);
        CorrigirEspacosEntreDigitos(linhas);
        var linhasNorm = linhas.Select(NormalizarTexto).ToList();

        // ── Passo 1: itens do catálogo (prioridade por comprimento decrescente) ──
        var catalogoOrdenado = ItemCatalog.OrderedItems
            .OrderByDescending(n => n.Length)
            .ToList();

        var linhasConsumidas = new HashSet<int>();

        foreach (var nome in catalogoOrdenado)
        {
            var termos = new List<string> { NormalizarTexto(nome) };
            if (_aliases.TryGetValue(nome, out var alts))
                termos.AddRange(alts.Select(NormalizarTexto));

            for (int i = 0; i < linhas.Count; i++)
            {
                if (!termos.Any(t => linhasNorm[i].Contains(t, StringComparison.OrdinalIgnoreCase)))
                    continue;

                decimal? val = null;
                int linhaVal = -1;

                if (!linhasConsumidas.Contains(i))
                {
                    val = ExtrairPrimeiroPeso(linhas[i]);
                    if (val.HasValue) linhaVal = i;
                }

                for (int j = i + 1; val is null && j <= i + 2 && j < linhas.Count; j++)
                {
                    if (linhasConsumidas.Contains(j)) break;
                    bool outraItem = catalogoOrdenado.Any(n => n != nome &&
                        linhasNorm[j].Contains(NormalizarTexto(n), StringComparison.OrdinalIgnoreCase));
                    if (outraItem) break;
                    val = ExtrairPrimeiroPeso(linhas[j]);
                    if (val.HasValue) linhaVal = j;
                }

                if (val.HasValue)
                {
                    resultado[nome] = val.Value;
                    linhasConsumidas.Add(i);
                    if (linhaVal >= 0 && linhaVal != i) linhasConsumidas.Add(linhaVal);
                    break;
                }
            }
        }

        // ── Passo 2: itens extras (não catalogados) ───────────────────────────
        // Procura linhas não consumidas que parecem ser nomes seguidos de um peso N3
        // Heurística: linha de texto puro (sem dígitos) + linha seguinte com peso N3
        var nomesExtraEncontrados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < linhas.Count - 1; i++)
        {
            if (linhasConsumidas.Contains(i)) continue;

            var linha = linhas[i].Trim();
            if (string.IsNullOrWhiteSpace(linha)) continue;

            // Descarta linhas que são sabidamente cabeçalho/rodapé
            var linhaNorm = NormalizarTexto(linha);
            if (_linhasIgnoradas.Any(ig => linhaNorm.Contains(ig, StringComparison.OrdinalIgnoreCase))) continue;

            // Ignora linhas que são só números/datas/R$
            if (Regex.IsMatch(linha, @"^[\d\s,./R$%]+$")) continue;

            // Ignora se já é um item do catálogo
            if (catalogoOrdenado.Any(n => linhaNorm.Contains(NormalizarTexto(n), StringComparison.OrdinalIgnoreCase))) continue;

            // Tenta encontrar o peso nessa linha ou na próxima
            decimal? peso = ExtrairPrimeiroPeso(linha);
            int linhaVal = peso.HasValue ? i : -1;

            if (!peso.HasValue && i + 1 < linhas.Count && !linhasConsumidas.Contains(i + 1))
            {
                peso = ExtrairPrimeiroPeso(linhas[i + 1]);
                if (peso.HasValue) linhaVal = i + 1;
            }

            if (!peso.HasValue) continue;

            // Remove dígitos e símbolos do nome, deixa só o texto
            var nomeExtra = Regex.Replace(linha, @"[\d,./R$%]", "").Trim();
            nomeExtra = Regex.Replace(nomeExtra, @"\s{2,}", " ").Trim();
            if (string.IsNullOrWhiteSpace(nomeExtra) || nomeExtra.Length < 2) continue;

            // Evita duplicatas com o catálogo
            if (catalogoOrdenado.Any(n => NormalizarTexto(n).Equals(NormalizarTexto(nomeExtra), StringComparison.OrdinalIgnoreCase))) continue;

            if (!nomesExtraEncontrados.Contains(nomeExtra))
            {
                nomesExtraEncontrados.Add(nomeExtra);
                resultado[nomeExtra] = resultado.TryGetValue(nomeExtra, out var existing) ? existing + peso.Value : peso.Value;
                linhasConsumidas.Add(i);
                if (linhaVal >= 0 && linhaVal != i) linhasConsumidas.Add(linhaVal);
            }
        }

        return resultado;
    }

    private static List<string> ExtrairLinhasPdf(string filePath)
    {
        var linhas = new List<string>();
        using var reader = new PdfReader(filePath);
        using var doc = new PdfDocument(reader);
        for (int p = 1; p <= doc.GetNumberOfPages(); p++)
        {
            var strategy = new LocationTextExtractionStrategy();
            var text = PdfTextExtractor.GetTextFromPage(doc.GetPage(p), strategy);
            foreach (var l in text.Split('\n'))
            {
                var trimmed = l.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    linhas.Add(trimmed);
            }
        }
        return linhas;
    }

    private static void CorrigirEspacosEntreDigitos(List<string> linhas)
    {
        for (int i = 0; i < linhas.Count; i++)
        {
            string orig;
            do
            {
                orig = linhas[i];
                linhas[i] = _reEspacoDigitos.Replace(orig, "$1$2");
            } while (linhas[i] != orig);
        }
    }

    private static decimal? ExtrairPrimeiroPeso(string linha)
    {
        var m = _rePeso.Match(linha);
        if (!m.Success) return null;

        for (int g = 1; g < m.Groups.Count; g++)
        {
            var raw = m.Groups[g].Value.Trim();
            if (string.IsNullOrEmpty(raw)) continue;
            raw = raw.Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                return val;
        }
        return null;
    }

    private static string NormalizarTexto(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);
        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().ToLowerInvariant().Trim();
    }
}
