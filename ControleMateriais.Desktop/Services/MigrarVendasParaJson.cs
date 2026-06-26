using ControleMateriais.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ControleMateriais.Desktop.Services;

/// <summary>
/// Migração: lê PDFs de venda existentes em Recibos_Venda/ e gera arquivos venda-DD-MM-YYYY.json
/// no banco-de-dados/ com o mesmo formato de registros que os arquivos mensais.
/// </summary>
public static class MigrarVendasParaJson
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Migrar(string rootDir, Action<string>? progresso = null)
    {
        var bancoDadosDir = GitHubService.BancoDadosRepoDir(rootDir);
        var vendaDir      = VendaViewModel.RecibosVendaDir(rootDir);

        Directory.CreateDirectory(bancoDadosDir);

        if (!Directory.Exists(vendaDir))
        {
            progresso?.Invoke("Diretório Recibos_Venda não encontrado.");
            return;
        }

        // ── 1. Ler todos os PDFs de venda ────────────────────────────────────
        var pdfs = Directory.GetFiles(vendaDir, "*.pdf", SearchOption.TopDirectoryOnly)
                           .Where(f => !Path.GetFileName(f).StartsWith("ESTOQUE", StringComparison.OrdinalIgnoreCase))
                           .OrderBy(f => f)
                           .ToList();

        progresso?.Invoke($"Processando {pdfs.Count} PDFs de venda...");

        // ── 2. Agrupar por data: DD-MM-YYYY → lista de registros ───────────────
        var porData = new Dictionary<string, List<(string Nome, Dictionary<string, decimal> Itens)>>(
            StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < pdfs.Count; i++)
        {
            var pdf = pdfs[i];
            progresso?.Invoke($"Venda {i + 1}/{pdfs.Count}: {Path.GetFileName(pdf)}");

            Dictionary<string, decimal> itens;
            try { itens = ReciboParserService.ExtrairPesos(pdf); }
            catch (Exception ex) { progresso?.Invoke($"  ERRO ao ler PDF: {ex.Message}"); continue; }

            if (itens.Count == 0) { progresso?.Invoke("  AVISO: nenhum item reconhecido."); continue; }

            var nomeCliente = ExtrairNomeCliente(pdf);
            var data        = ExtrairData(pdf);
            var chave       = data.ToString("dd-MM-yyyy");   // ex: "23-06-2026"
            var dataLabel   = data.ToString("dd/MM/yyyy");   // ex: "23/06/2026"

            if (!porData.TryGetValue(chave, out var registros))
            {
                registros = new List<(string, Dictionary<string, decimal>)>();
                porData[chave] = registros;
            }
            registros.Add((nomeCliente, itens));
        }

        // ── 3. Gravar arquivos venda-DD-MM-YYYY.json ──────────────────────────
        int gerados = 0;
        foreach (var kv in porData)
        {
            var chave     = kv.Key;                    // "23-06-2026"
            var dataLabel = chave[..2] + "/" + chave[3..5] + "/" + chave[6..]; // "23/06/2026"

            var jsonPath = Path.Combine(bancoDadosDir, "venda-" + chave + ".json");

            var root = new JsonObject();
            root["data"] = dataLabel;

            var registrosArr = new JsonArray();
            foreach (var (nome, itens) in kv.Value)
            {
                var reg = new JsonObject();
                reg["nome"] = nome;

                var materiaisArr = new JsonArray();
                foreach (var item in itens.OrderBy(x => x.Key))
                {
                    var mat = new JsonObject();
                    mat["descricao"] = item.Key;
                    mat["peso"]      = JsonValue.Create(item.Value);
                    materiaisArr.Add(mat);
                }
                reg["materiais"] = materiaisArr;
                registrosArr.Add(reg);
            }
            root["registros"] = registrosArr;

            File.WriteAllText(jsonPath, root.ToJsonString(JsonOpts), Encoding.UTF8);
            gerados++;
        }

        progresso?.Invoke($"Concluído. {gerados} arquivo(s) de venda gerado(s).");
    }

    /// <summary>
    /// Re-salva arquivos JSON existentes com encoding correto (UTF-8 + UnsafeRelaxedJsonEscaping).
    /// Útil para corrigir acentos em arquivos criados antes do fix de encoding.
    /// </summary>
    public static void FixEncoding(string rootDir, Action<string>? progresso = null)
    {
        var bancoDadosDir = GitHubService.BancoDadosRepoDir(rootDir);
        if (!Directory.Exists(bancoDadosDir))
        {
            progresso?.Invoke("Diretório banco-de-dados não encontrado.");
            return;
        }

        var jsonFiles = Directory.GetFiles(bancoDadosDir, "*.json", SearchOption.TopDirectoryOnly);
        progresso?.Invoke($"Verificando {jsonFiles.Length} arquivos JSON...");

        int corrigidos = 0;
        foreach (var file in jsonFiles)
        {
            try
            {
                var content = File.ReadAllText(file, Encoding.UTF8);
                var obj = JsonNode.Parse(content)?.AsObject();
                if (obj is null) continue;

                // Re-salva com encoding correto
                File.WriteAllText(file, obj.ToJsonString(JsonOpts), Encoding.UTF8);
                corrigidos++;
            }
            catch { }
        }

        progresso?.Invoke($"{corrigidos} arquivo(s) corrigido(s) com encoding UTF-8.");
    }

    // Extrai nome do cliente do nome do arquivo PDF (padrão NomeCliente_dd-MM-yyyy.pdf)
    private static string ExtrairNomeCliente(string filePath)
    {
        var semExt = Path.GetFileNameWithoutExtension(filePath);

        // Padrão: NomeCliente_dd-MM-yyyy
        var m = System.Text.RegularExpressions.Regex.Match(semExt, @"^(.+?)_\d{2}-\d{2}-\d{4}$");
        if (m.Success) return m.Groups[1].Value.Replace("_", " ");

        return semExt.Replace("_", " ");
    }

    // Extrai a data do nome do arquivo PDF
    private static DateTime ExtrairData(string filePath)
    {
        var semExt = Path.GetFileNameWithoutExtension(filePath);

        var m = System.Text.RegularExpressions.Regex.Match(semExt, @"_(\d{2}-\d{2}-\d{4})");
        if (m.Success && DateTime.TryParseExact(m.Groups[1].Value, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        return File.GetLastWriteTime(filePath);
    }
}
