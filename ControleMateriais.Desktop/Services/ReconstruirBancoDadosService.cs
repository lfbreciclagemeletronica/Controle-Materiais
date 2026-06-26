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

public static class ReconstruirBancoDadosService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private const string LogFileName = "modificacao-estoque.log";

    /// <summary>
    /// Recria os arquivos compra-MM-YYYY.json e venda-DD-MM-YYYY.json em banco-de-dados/ a partir dos PDFs de pesagem,
    /// gera modificacao-estoque.log.
    /// Mantém todos os arquivos estoque-inicial-*.json intactos.
    /// </summary>
    public static void Reconstruir(string rootDir, Action<string>? progresso = null)
    {
        var bancoDadosDir = GitHubService.BancoDadosRepoDir(rootDir);
        var recibosDir    = GitHubService.RecibosRepoDir(rootDir);
        var vendaDir      = Path.Combine(recibosDir, "Recibos_Venda");
        var logPath       = Path.Combine(bancoDadosDir, LogFileName);

        Directory.CreateDirectory(bancoDadosDir);

        // ── 1. Apagar apenas os arquivos compra-MM-YYYY.json e venda-DD-MM-YYYY.json ────────
        progresso?.Invoke("Removendo arquivos mensais antigos...");
        foreach (var f in Directory.GetFiles(bancoDadosDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            var nome = Path.GetFileNameWithoutExtension(f);
            // Apaga arquivos no padrão "compra-MM-YYYY" (ex: compra-06-2026.json)
            if (System.Text.RegularExpressions.Regex.IsMatch(nome, @"^compra-\d{2}-\d{4}$"))
                File.Delete(f);
            // Apaga arquivos no padrão "venda-DD-MM-YYYY" (ex: venda-09-06-2026.json)
            if (System.Text.RegularExpressions.Regex.IsMatch(nome, @"^venda-\d{2}-\d{2}-\d{4}$"))
                File.Delete(f);
        }

        // ── 2. Determinar PDFs de pesagem (excluindo vendas) ──────────────────
        var nomesVenda = Directory.Exists(vendaDir)
            ? new HashSet<string>(
                Directory.GetFiles(vendaDir, "*.pdf", SearchOption.TopDirectoryOnly)
                         .Select(Path.GetFileName)!,
                StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var pdfs = Directory.Exists(recibosDir)
            ? Directory.GetFiles(recibosDir, "*.pdf", SearchOption.TopDirectoryOnly)
                       .Where(f => !nomesVenda.Contains(Path.GetFileName(f)))
                       .OrderBy(f => f)
                       .ToList()
            : new List<string>();

        progresso?.Invoke($"Processando {pdfs.Count} recibos de pesagem...");

        // ── 3. Montar dicionário em memória: "MM-YYYY" → array de registros (compras) ───
        // registros: List<(nome, List<(descricao, peso)>)>
        var mensais = new Dictionary<string, List<(string Nome, Dictionary<string, decimal> Itens)>>(
            StringComparer.OrdinalIgnoreCase);

        // Dicionário para vendas: "DD-MM-YYYY" → array de registros
        var vendasDiarias = new Dictionary<string, List<(string Nome, Dictionary<string, decimal> Itens)>>(
            StringComparer.OrdinalIgnoreCase);

        var totaisPesagem = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var logLines      = new List<string>();

        for (int i = 0; i < pdfs.Count; i++)
        {
            var pdf = pdfs[i];
            progresso?.Invoke($"Pesagem {i + 1}/{pdfs.Count}: {Path.GetFileName(pdf)}");

            Dictionary<string, decimal> itens;
            try { itens = ReciboParserService.ExtrairPesos(pdf); }
            catch (Exception ex) { progresso?.Invoke($"  ERRO ao ler PDF: {ex.Message}"); continue; }

            if (itens.Count == 0) { progresso?.Invoke("  AVISO: nenhum item reconhecido."); continue; }

            var nomeCliente = ExtrairNomeCliente(pdf);
            var data        = ExtrairData(pdf);
            var chave       = data.ToString("MM-yyyy");   // ex: "06-2026"
            var dataLabel   = data.ToString("MM/yyyy");   // ex: "06/2026"

            if (!mensais.TryGetValue(chave, out var registros))
            {
                registros = new List<(string, Dictionary<string, decimal>)>();
                mensais[chave] = registros;
            }
            registros.Add((nomeCliente, itens));

            // Acumular no total de pesagem
            foreach (var kv in itens)
            {
                totaisPesagem[kv.Key] = totaisPesagem.TryGetValue(kv.Key, out var at) ? at + kv.Value : kv.Value;

                // Linha de log
                logLines.Add($"{DateTime.Now:yyyy-MM-ddTHH:mm:ss} | {nomeCliente} | {kv.Key} | {kv.Value:N3} kg");
            }
        }

        // ── 4. Processar PDFs de vendas ─────────────────────────────────────────────
        if (Directory.Exists(vendaDir))
        {
            var vendaPdfs = Directory.GetFiles(vendaDir, "*.pdf", SearchOption.TopDirectoryOnly)
                                     .Where(f => !Path.GetFileName(f).StartsWith("ESTOQUE", StringComparison.OrdinalIgnoreCase))
                                     .OrderBy(f => f)
                                     .ToList();

            progresso?.Invoke($"Processando {vendaPdfs.Count} recibos de venda...");

            for (int i = 0; i < vendaPdfs.Count; i++)
            {
                var pdf = vendaPdfs[i];
                progresso?.Invoke($"Venda {i + 1}/{vendaPdfs.Count}: {Path.GetFileName(pdf)}");

                Dictionary<string, decimal> itensVenda;
                try { itensVenda = ReciboParserService.ExtrairPesos(pdf); }
                catch (Exception ex) { progresso?.Invoke($"  ERRO ao ler {Path.GetFileName(pdf)}: {ex.Message}"); continue; }

                if (itensVenda.Count == 0)
                {
                    progresso?.Invoke($"  AVISO: {Path.GetFileName(pdf)} — nenhum item reconhecido.");
                    continue;
                }

                var nomeCliente = ExtrairNomeCliente(pdf);
                var data = ExtrairData(pdf);
                var chave = data.ToString("dd-MM-yyyy"); // ex: "09-06-2026"

                if (!vendasDiarias.TryGetValue(chave, out var registros))
                {
                    registros = new List<(string, Dictionary<string, decimal>)>();
                    vendasDiarias[chave] = registros;
                }
                registros.Add((nomeCliente, itensVenda));

                // Linha de log para vendas
                foreach (var kv in itensVenda)
                {
                    logLines.Add($"{DateTime.Now:yyyy-MM-ddTHH:mm:ss} | VENDA: {nomeCliente} | {kv.Key} | {kv.Value:N3} kg");
                }
            }
        }

        // ── 5. Gravar os arquivos compra-MM-YYYY.json ────────────────────────────────
        int gerados = 0;
        foreach (var kv in mensais)
        {
            var chave = kv.Key; // "06-2026"

            var jsonPath = Path.Combine(bancoDadosDir, $"compra-{chave}.json");

            var root = new JsonObject();
            root["mes"] = chave; // "06-2026"

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

        progresso?.Invoke($"{gerados} arquivo(s) de compra gerado(s). Gerando arquivos de venda...");

        // ── 5. Gravar os arquivos venda-DD-MM-YYYY.json ────────────────────────────────
        int vendasGeradas = 0;
        foreach (var kv in vendasDiarias)
        {
            var chave = kv.Key; // "09-06-2026"

            var jsonPath = Path.Combine(bancoDadosDir, $"venda-{chave}.json");

            var root = new JsonObject();
            root["mes"] = chave.Substring(3); // Extrai "06-2026" de "09-06-2026"

            var registrosArr = new JsonArray();
            foreach (var (nome, itens) in kv.Value)
            {
                var reg = new JsonObject();
                reg["nome"] = nome;
                reg["data"] = chave; // "09-06-2026"

                var materiaisArr = new JsonArray();
                foreach (var item in itens.OrderBy(x => x.Key))
                {
                    var mat = new JsonObject();
                    mat["descricao"] = item.Key;
                    mat["peso"] = JsonValue.Create(item.Value);
                    materiaisArr.Add(mat);
                }
                reg["materiais"] = materiaisArr;
                registrosArr.Add(reg);
            }
            root["registros"] = registrosArr;

            File.WriteAllText(jsonPath, root.ToJsonString(JsonOpts), Encoding.UTF8);
            vendasGeradas++;
        }

        progresso?.Invoke($"{vendasGeradas} arquivo(s) de venda gerado(s). Concluído.");

        // ── 6. Gravar log de modificações ─────────────────────────────────────
        try { File.WriteAllLines(logPath, logLines, Encoding.UTF8); } catch { }

        progresso?.Invoke($"Concluído. {gerados} arquivo(s) de compra e {vendasGeradas} arquivo(s) de venda gerados.");
    }

    // Extrai nome do cliente do nome do arquivo PDF (padrão NomeCliente_dd-MM-yyyy[_HH-mm].pdf)
    private static string ExtrairNomeCliente(string filePath)
    {
        var semExt = Path.GetFileNameWithoutExtension(filePath);

        // Padrão com hora: NomeCliente_dd-MM-yyyy_HH-mm
        var m1 = System.Text.RegularExpressions.Regex.Match(semExt, @"^(.+?)_\d{2}-\d{2}-\d{4}_\d{2}-\d{2}$");
        if (m1.Success) return m1.Groups[1].Value.Replace("_", " ");

        // Padrão sem hora: NomeCliente_dd-MM-yyyy
        var m2 = System.Text.RegularExpressions.Regex.Match(semExt, @"^(.+?)_\d{2}-\d{2}-\d{4}$");
        if (m2.Success) return m2.Groups[1].Value.Replace("_", " ");

        return semExt.Replace("_", " ");
    }

    // Extrai a data do nome do arquivo PDF
    private static DateTime ExtrairData(string filePath)
    {
        var semExt = Path.GetFileNameWithoutExtension(filePath);

        var m1 = System.Text.RegularExpressions.Regex.Match(semExt, @"_(\d{2}-\d{2}-\d{4})_\d{2}-\d{2}$");
        if (m1.Success && DateTime.TryParseExact(m1.Groups[1].Value, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt1)) return dt1;

        var m2 = System.Text.RegularExpressions.Regex.Match(semExt, @"_(\d{2}-\d{2}-\d{4})");
        if (m2.Success && DateTime.TryParseExact(m2.Groups[1].Value, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2)) return dt2;

        return File.GetLastWriteTime(filePath);
    }
}
