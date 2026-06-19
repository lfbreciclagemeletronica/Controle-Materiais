using ControleMateriais.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ControleMateriais.Desktop.Services;

public static class ReconstruirBancoDadosService
{
    /// <summary>
    /// Apaga todos os .json em banco-de-dados/ (exceto .git/), regera um JSON por PDF de pesagem
    /// e grava um novo estoque.json = Σ pesagens − Σ vendas.
    /// </summary>
    public static void Reconstruir(string rootDir, Action<string>? progresso = null)
    {
        var bancoDadosDir = GitHubService.BancoDadosRepoDir(rootDir);
        var recibosDir    = GitHubService.RecibosRepoDir(rootDir);
        var vendaDir      = Path.Combine(recibosDir, "Recibos_Venda");
        var logPath       = Path.Combine(rootDir, "reconstruir-banco.log");

        var logLines    = new System.Collections.Generic.List<string>();
        var progressoOrig = progresso;
        progresso = msg =>
        {
            logLines.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            progressoOrig?.Invoke(msg);
        };

        // ── 1. Apagar todos os .json existentes em banco-de-dados/ ────────────
        progresso?.Invoke("Removendo JSONs antigos...");
        if (Directory.Exists(bancoDadosDir))
        {
            foreach (var f in Directory.GetFiles(bancoDadosDir, "*.json", SearchOption.TopDirectoryOnly))
                File.Delete(f);
        }
        else
        {
            Directory.CreateDirectory(bancoDadosDir);
        }

        // ── 2. Processar cada PDF de pesagem → gerar JSON ────────────────────
        // Exclui PDFs cujo nome também existe em Recibos_Venda/ (são recibos de venda na pasta errada)
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

        var totaisPesagem = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        int gerados = 0;

        for (int i = 0; i < pdfs.Count; i++)
        {
            var pdf = pdfs[i];
            progresso?.Invoke($"Pesagem {i + 1}/{pdfs.Count}: {Path.GetFileName(pdf)}");

            Dictionary<string, decimal> itens;
            try { itens = ReciboParserService.ExtrairPesos(pdf); }
            catch { continue; }

            if (itens.Count == 0) continue;

            // Acumular no total de pesagem
            foreach (var kv in itens)
                totaisPesagem[kv.Key] = totaisPesagem.TryGetValue(kv.Key, out var atual)
                    ? atual + kv.Value
                    : kv.Value;

            // Gerar JSON com mesmo nome do PDF (trocando extensão)
            var nomeSemExt  = Path.GetFileNameWithoutExtension(pdf);
            var dataStr     = ExtrairDataDoNome(pdf);
            var jsonPath    = Path.Combine(bancoDadosDir, nomeSemExt + ".json");

            var obj = new JsonObject();
            foreach (var kv in itens.OrderBy(k => k.Key))
                obj[kv.Key] = JsonValue.Create(kv.Value);
            obj["data"]   = dataStr;
            obj["status"] = "Adicionado ao estoque";

            File.WriteAllText(jsonPath,
                obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            gerados++;
        }

        progresso?.Invoke($"{gerados} JSONs gerados. Processando recibos de venda...");

        // ── 3. Subtrair vendas ───────────────────────────────────────────────
        var totaisEstoque = new Dictionary<string, decimal>(totaisPesagem, StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(vendaDir))
        {
            var vendaPdfs = Directory.GetFiles(vendaDir, "*.pdf", SearchOption.TopDirectoryOnly)
                                     .OrderBy(f => f)
                                     .ToList();

            progresso?.Invoke($"Subtraindo {vendaPdfs.Count} recibos de venda...");

            for (int i = 0; i < vendaPdfs.Count; i++)
            {
                var pdf = vendaPdfs[i];
                progresso?.Invoke($"Venda {i + 1}/{vendaPdfs.Count}: {Path.GetFileName(pdf)}");

                Dictionary<string, decimal> itensVenda;
                try { itensVenda = ReciboParserService.ExtrairPesos(pdf); }
                catch (Exception ex) { progresso?.Invoke($"  ERRO ao ler {Path.GetFileName(pdf)}: {ex.Message}"); continue; }

                if (itensVenda.Count == 0)
                {
                    progresso?.Invoke($"  AVISO: {Path.GetFileName(pdf)} — nenhum item reconhecido pelo parser.");
                    continue;
                }

                progresso?.Invoke($"  {Path.GetFileName(pdf)}: {itensVenda.Count} item(ns) — {string.Join(", ", itensVenda.Select(kv => $"{kv.Key}={kv.Value:N3}"))}");

                foreach (var kv in itensVenda)
                {
                    if (totaisEstoque.TryGetValue(kv.Key, out var atual))
                        totaisEstoque[kv.Key] = Math.Max(0m, atual - kv.Value);
                    else
                        progresso?.Invoke($"  AVISO: item \"{kv.Key}\" não encontrado no estoque de pesagem.");
                }
            }
        }

        // Remove itens com peso zero ou negativo
        foreach (var key in totaisEstoque.Keys.ToList())
        {
            if (totaisEstoque[key] <= 0m)
                totaisEstoque.Remove(key);
        }

        // ── 4. Gravar estoque.json ────────────────────────────────────────────
        progresso?.Invoke("Gravando estoque.json...");
        EstoqueViewModel.GravarEstoque(rootDir, totaisEstoque);

        progresso?.Invoke($"Concluído. {gerados} JSONs + estoque.json gravados.");

        try { File.WriteAllLines(logPath, logLines); } catch { }
    }

    private static string ExtrairDataDoNome(string filePath)
    {
        var semExt = Path.GetFileNameWithoutExtension(filePath);

        var m1 = System.Text.RegularExpressions.Regex.Match(semExt, @"_(\d{2}-\d{2}-\d{4})_\d{2}-\d{2}$");
        if (m1.Success && DateTime.TryParseExact(m1.Groups[1].Value, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt1))
            return dt1.ToString("dd-MM-yyyy");

        var m2 = System.Text.RegularExpressions.Regex.Match(semExt, @"_(\d{2}-\d{2}-\d{4})");
        if (m2.Success && DateTime.TryParseExact(m2.Groups[1].Value, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2))
            return dt2.ToString("dd-MM-yyyy");

        return File.GetLastWriteTime(filePath).ToString("dd-MM-yyyy");
    }
}
