using ControleMateriais.Data.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Services;

/// <summary>
/// Lê os PDFs de recibos de venda já existentes em Recibos_Venda/ e grava as informações
/// (cliente, data, valor total, peso total e itens) nas tabelas RecibosVenda/RecibosVendaItens
/// do banco de dados SQLite, gerando um código de recibo de 6 dígitos para cada um.
/// </summary>
public static class MigrarRecibosVendaParaBancoService
{
    public static async Task<(int importados, int ignorados, int comErro)> MigrarAsync(
        string rootDir, Action<string>? progresso = null)
    {
        var vendaDir = ControleMateriais.Desktop.ViewModels.VendaViewModel.RecibosVendaDir(rootDir);
        if (!Directory.Exists(vendaDir))
        {
            progresso?.Invoke("Diretório Recibos_Venda não encontrado.");
            return (0, 0, 0);
        }

        var pdfs = Directory.GetFiles(vendaDir, "*.pdf", SearchOption.TopDirectoryOnly)
            .Where(f => !Path.GetFileName(f).StartsWith("ESTOQUE", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();

        progresso?.Invoke($"Encontrado(s) {pdfs.Count} PDF(s) de venda.");

        var existentes = (await DataService.ReciboVendaRepository.GetAllAsync()).ToList();

        int importados = 0, ignorados = 0, comErro = 0;

        for (int i = 0; i < pdfs.Count; i++)
        {
            var pdf = pdfs[i];
            var nomeArquivo = Path.GetFileName(pdf);
            progresso?.Invoke($"Processando {i + 1}/{pdfs.Count}: {nomeArquivo}");

            try
            {
                var (nomeCliente, dataStr) = ParsearNomeArquivo(pdf);

                // Já importado anteriormente? (mesmo cliente + mesma data)
                if (existentes.Any(e =>
                        e.ClienteNome.Equals(nomeCliente, StringComparison.OrdinalIgnoreCase) &&
                        e.DataVenda == dataStr))
                {
                    ignorados++;
                    continue;
                }

                var itensPeso = ReciboParserService.ExtrairPesos(pdf);
                if (itensPeso.Count == 0)
                {
                    progresso?.Invoke("  Aviso: nenhum item reconhecido, pulando.");
                    comErro++;
                    continue;
                }

                var (_, valorStr) = ReciboParserService.ExtrairCabecalho(pdf);
                decimal valorTotal = 0;
                if (!string.IsNullOrEmpty(valorStr))
                    decimal.TryParse(valorStr, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out valorTotal);

                var pesoTotal = itensPeso.Values.Sum();
                var codigo = await DataService.ReciboVendaRepository.GerarCodigoReciboAsync();

                var reciboVenda = new ReciboVenda
                {
                    ClienteNome = nomeCliente,
                    DataVenda = dataStr,
                    CodigoRecibo = codigo,
                    ValorTotal = valorTotal,
                    PesoTotalKg = pesoTotal
                };

                foreach (var (nome, peso) in itensPeso)
                {
                    var item = await DataService.ItemRepository.GetOrCreateAsync(nome);
                    reciboVenda.Itens.Add(new ReciboVendaItem { ItemId = item.Id, PesoKg = peso, PrecoPorKg = 0 });
                }

                await DataService.ReciboVendaRepository.InsertAsync(reciboVenda);
                existentes.Add(reciboVenda);
                importados++;
            }
            catch (Exception ex)
            {
                progresso?.Invoke($"  ERRO: {ex.Message}");
                comErro++;
            }
        }

        progresso?.Invoke($"Concluído: {importados} importado(s), {ignorados} já existente(s), {comErro} com erro.");
        return (importados, ignorados, comErro);
    }

    // Extrai nome do cliente e data de um nome de arquivo de recibo de venda.
    // Padrões: NOME_ddMMyyyyHHmmss (novo), NOME_dd-MM-yyyy (antigo)
    private static (string nome, string data) ParsearNomeArquivo(string filePath)
    {
        var semExt = Path.GetFileNameWithoutExtension(filePath);

        var mNovo = Regex.Match(semExt, @"^(.+?)_(\d{14})$");
        if (mNovo.Success && DateTime.TryParseExact(mNovo.Groups[2].Value, "ddMMyyyyHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtNovo))
            return (mNovo.Groups[1].Value.Replace("_", " "), dtNovo.ToString("dd/MM/yyyy"));

        var mAntigo = Regex.Match(semExt, @"^(.+?)_(\d{2}-\d{2}-\d{4})$");
        if (mAntigo.Success && DateTime.TryParseExact(mAntigo.Groups[2].Value, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtAntigo))
            return (mAntigo.Groups[1].Value.Replace("_", " "), dtAntigo.ToString("dd/MM/yyyy"));

        return (semExt.Replace("_", " "), File.GetLastWriteTime(filePath).ToString("dd/MM/yyyy"));
    }
}
