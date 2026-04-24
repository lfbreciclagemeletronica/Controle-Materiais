using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ControleMateriais.Desktop.Services;
using ControleMateriais.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace ControleMateriais.Desktop.ViewModels;

public class ReciboItemWrapper : ViewModelBase
{
    private decimal _precoPorKg;

    public string Nome { get; }
    public decimal PesoAtual { get; }

    public decimal PrecoPorKg
    {
        get => _precoPorKg;
        set
        {
            if (value != _precoPorKg)
            {
                _precoPorKg = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(PrecoTexto));
            }
        }
    }

    public decimal Total => PesoAtual * PrecoPorKg;
    public string PrecoTexto => PrecoPorKg > 0 ? PrecoPorKg.ToString("C", CultureInfo.GetCultureInfo("pt-BR")) : "—";
    public string PesoTexto  => PesoAtual.ToString("N3", CultureInfo.GetCultureInfo("pt-BR"));
    public string TotalTexto => Total > 0 ? Total.ToString("C", CultureInfo.GetCultureInfo("pt-BR")) : "—";

    public ReciboItemWrapper(string nome, decimal pesoAtual) { Nome = nome; PesoAtual = pesoAtual; }
}

public class TabelaOpcao
{
    public string Nome    { get; set; } = string.Empty;
    public string Arquivo { get; set; } = string.Empty;
    public override string ToString() => Nome;
}

public class ReciboFromPesagemViewModel : ViewModelBase
{
    private readonly string _rootDir;
    private readonly PesagemItem _pesagem;
    private static string TabelaPrecosDir(string root) => Path.Combine(root, "TabelaPrecos");
    private static string RecibosDir(string root)      => Path.Combine(root, "Recibos");

    public string NomeCliente { get; }
    public string Horario     { get; }

    public ObservableCollection<ReciboItemWrapper> Itens { get; } = new();
    public ObservableCollection<TabelaOpcao>       Tabelas { get; } = new();

    private TabelaOpcao? _tabelaSelecionada;
    public TabelaOpcao? TabelaSelecionada
    {
        get => _tabelaSelecionada;
        set
        {
            if (value != _tabelaSelecionada)
            {
                _tabelaSelecionada = value;
                OnPropertyChanged();
                _ = AplicarTabelaAsync(value);
            }
        }
    }

    public decimal TotalGeral => Itens.Sum(i => i.Total);
    public decimal PesoTotal  => Itens.Sum(i => i.PesoAtual);

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        private set { if (value != _status) { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusVisivel)); } }
    }
    public bool StatusVisivel => !string.IsNullOrEmpty(_status);

    private bool _statusOk = true;
    public bool StatusOk
    {
        get => _statusOk;
        private set { if (value != _statusOk) { _statusOk = value; OnPropertyChanged(); } }
    }

    public ICommand VoltarCommand  { get; }
    public ICommand ExportarCommand { get; }

    public ReciboFromPesagemViewModel(PesagemItem pesagem, string rootDir, Action voltarCallback)
    {
        _pesagem  = pesagem;
        _rootDir  = rootDir;
        NomeCliente = pesagem.Cliente;
        Horario     = pesagem.Horario;

        foreach (var it in pesagem.Itens.Where(i => i.Peso > 0))
            Itens.Add(new ReciboItemWrapper(it.Nome, it.Peso));

        VoltarCommand  = new DelegateCommand(voltarCallback);
        ExportarCommand = new DelegateCommand(() => _ = ExportarAsync());

        _ = CarregarTabelasAsync();
    }

    private async Task CarregarTabelasAsync()
    {
        var dir = TabelaPrecosDir(_rootDir);
        Directory.CreateDirectory(dir);
        Tabelas.Clear();

        await Task.Run(() =>
        {
            foreach (var f in Directory.GetFiles(dir, "*.json").OrderBy(x => x))
            {
                var nome = Path.GetFileNameWithoutExtension(f);
                Tabelas.Add(new TabelaOpcao { Nome = nome, Arquivo = f });
            }
        });

        if (Tabelas.Count > 0)
            TabelaSelecionada = Tabelas[0];
    }

    private async Task AplicarTabelaAsync(TabelaOpcao? tabela)
    {
        if (tabela is null || !File.Exists(tabela.Arquivo)) return;

        try
        {
            var json = await File.ReadAllTextAsync(tabela.Arquivo);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Dictionary<string, decimal> precos = new(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("Itens", out var itensEl) && itensEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in itensEl.EnumerateArray())
                {
                    var nome  = el.TryGetProperty("Nome",      out var n) ? n.GetString() ?? string.Empty : string.Empty;
                    var preco = el.TryGetProperty("PrecoPorKg", out var p) ? p.GetDecimal() : 0m;
                    if (!string.IsNullOrEmpty(nome)) precos[nome] = preco;
                }
            }

            foreach (var item in Itens)
            {
                if (precos.TryGetValue(item.Nome, out var p))
                    item.PrecoPorKg = p;
            }
        }
        catch { }

        OnPropertyChanged(nameof(TotalGeral));
        OnPropertyChanged(nameof(PesoTotal));
    }

    private async Task ExportarAsync()
    {
        if (!Itens.Any(i => i.Total > 0))
        {
            MostrarStatus("Selecione uma tabela de preços antes de exportar.", ok: false);
            return;
        }

        var data = DateTime.Now;
        var nomeSeguro = string.Concat(NomeCliente.Split(Path.GetInvalidFileNameChars()));
        var nomeArquivo = $"{nomeSeguro}_{data:dd-MM-yyyy}.pdf";

        Directory.CreateDirectory(RecibosDir(_rootDir));

        var topLevel = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (topLevel is null) return;

        var suggestedFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(RecibosDir(_rootDir)));
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Salvar recibo em PDF",
                SuggestedFileName = nomeArquivo,
                SuggestedStartLocation = suggestedFolder,
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

        var filePath = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(filePath)) return;

        GerarPdf(filePath);
        await MarcarConcluido(filePath, data);
        MostrarStatus($"PDF exportado: {Path.GetFileName(filePath)}", ok: true);
    }

    private void GerarPdf(string filePath)
    {
        var ptBR          = CultureInfo.GetCultureInfo("pt-BR");
        var borderColor   = Colors.Grey.Darken2;
        var cellFontSize  = 10f;
        var headerFontSize = 10f;
        var dataGeracao   = DateTime.Now;

        byte[]? logoBytes = null;
        try
        {
            var uri = new Uri("avares://ControleMateriais.Desktop/Assets/lfb-logo.png");
            using var logoStream = Avalonia.Platform.AssetLoader.Open(uri);
            using var ms = new MemoryStream();
            logoStream.CopyTo(ms);
            logoBytes = ms.ToArray();
        }
        catch { }

        static IContainer InfoLabelCell(IContainer c) =>
            c.Border(0.5f).BorderColor(Colors.Grey.Darken2)
             .Background(Colors.Grey.Lighten3)
             .PaddingVertical(5).PaddingHorizontal(4);

        static IContainer InfoCell(IContainer c) =>
            c.Border(0.5f).BorderColor(Colors.Grey.Darken2)
             .PaddingVertical(5).PaddingHorizontal(4);

        var itens          = Itens.ToList();
        var pesoTotal      = itens.Sum(i => i.PesoAtual);
        var totalGeral     = itens.Sum(i => i.Total);
        var nomeCliente    = NomeCliente;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(0.8f, Unit.Centimetre);
                page.MarginBottom(0.8f, Unit.Centimetre);
                page.MarginHorizontal(0.8f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(cellFontSize).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    col.Spacing(0);

                    col.Item().Border(0.5f).BorderColor(borderColor)
                       .PaddingVertical(6).PaddingHorizontal(8).Column(hdr =>
                       {
                           hdr.Item().Row(row =>
                           {
                               row.ConstantItem(42).AlignMiddle().AlignCenter()
                                  .Background("#4CAF50").Padding(2)
                                  .Column(logo =>
                                  {
                                      if (logoBytes != null)
                                          logo.Item().AlignCenter().Width(38).Image(logoBytes);
                                      else
                                          logo.Item().AlignCenter().Text("LFB").Bold()
                                              .FontColor(Colors.White).FontSize(12);
                                  });

                               row.RelativeItem().PaddingLeft(8).AlignMiddle().Column(left =>
                               {
                                   left.Item().AlignCenter()
                                       .Text("LFB RECICLAGEM ELETRONICA").Bold().FontSize(13);
                                   left.Item().PaddingTop(2).AlignCenter()
                                       .Text("CNPJ: 243.250.67/0001-64  |  I.E: 096/4003708  |  End: Rua Sergio Jungblut Dieterich, 1011-B")
                                       .FontSize(7.5f);
                                   left.Item().PaddingTop(4).AlignCenter()
                                       .Text("RESULTADO DA PESAGEM E TRIAGEM LFB").Bold().FontSize(9.5f);
                               });
                           });
                       });

                    col.Item().Table(info =>
                    {
                        info.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(2.2f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(0.6f);
                            c.RelativeColumn(1.2f);
                        });

                        info.Cell().Element(InfoLabelCell).AlignCenter().Text("FORNECEDOR").Bold().FontSize(8f);
                        info.Cell().Element(InfoCell).AlignCenter().Text(nomeCliente).FontSize(9f);
                        info.Cell().Element(InfoLabelCell).AlignCenter().Text("PESO").Bold().FontSize(8f);
                        info.Cell().Element(InfoCell).AlignCenter().Text($"{pesoTotal:N3} kg").Bold().FontSize(9f);
                        info.Cell().Element(InfoLabelCell).AlignCenter().Text("VALOR").Bold().FontSize(8f);
                        info.Cell().Element(InfoCell).AlignCenter().Text(totalGeral.ToString("C", ptBR)).Bold().FontSize(9f);
                        info.Cell().Element(InfoLabelCell).AlignCenter().Text("DATA").Bold().FontSize(8f);
                        info.Cell().Element(InfoCell).AlignCenter().Text($"{dataGeracao:dd/MM/yyyy}").FontSize(9f);
                    });

                    col.Item().Height(6);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3.5f);
                            c.RelativeColumn(1.3f);
                            c.RelativeColumn(1.6f);
                            c.RelativeColumn(1.6f);
                        });

                        table.Header(header =>
                        {
                            static IContainer HCell(IContainer c) =>
                                c.Background(Colors.Grey.Lighten3)
                                 .Border(0.5f).BorderColor(Colors.Grey.Darken2)
                                 .PaddingVertical(5).PaddingHorizontal(4);

                            header.Cell().Element(HCell).AlignCenter().Text("MATERIAL").Bold().FontSize(headerFontSize);
                            header.Cell().Element(HCell).AlignCenter().Text("KG").Bold().FontSize(headerFontSize);
                            header.Cell().Element(HCell).AlignCenter().Text("VALOR/KG").Bold().FontSize(headerFontSize);
                            header.Cell().Element(HCell).AlignCenter().Text("TOTAL").Bold().FontSize(headerFontSize);
                        });

                        static IContainer BCell(IContainer c) =>
                            c.Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                             .PaddingVertical(4).PaddingHorizontal(5);

                        foreach (var it in itens)
                        {
                            table.Cell().Element(BCell).AlignCenter().Text(it.Nome).FontSize(cellFontSize);
                            table.Cell().Element(BCell).AlignCenter().Text(it.PesoAtual.ToString("N3")).FontSize(cellFontSize);
                            table.Cell().Element(BCell).AlignCenter()
                                 .Text(it.PrecoPorKg > 0 ? it.PrecoPorKg.ToString("C", ptBR) : string.Empty).FontSize(cellFontSize);
                            table.Cell().Element(BCell).AlignCenter()
                                 .Text(it.Total > 0 ? it.Total.ToString("C", ptBR) : string.Empty).FontSize(cellFontSize);
                        }
                    });
                });
            });
        }).GeneratePdf(filePath);
    }

    private async Task MarcarConcluido(string filePath, DateTime dataConclusao)
    {
        var repoDir     = GitHubService.RepoDir(_rootDir);
        var arquivoJson = Path.Combine(repoDir, _pesagem.NomeArquivo);
        if (!File.Exists(arquivoJson)) return;

        try
        {
            var json = await File.ReadAllTextAsync(arquivoJson);
            using var doc = JsonDocument.Parse(json);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();

            dict["StatusPesagem"]   = JsonSerializer.SerializeToElement("concluido");
            dict["DataConclusao"]   = JsonSerializer.SerializeToElement(dataConclusao.ToString("yyyy-MM-ddTHH:mm:ss"));
            dict["NomeRecibo"]      = JsonSerializer.SerializeToElement(Path.GetFileName(filePath));

            var novoJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(arquivoJson, novoJson);

            await CommitConcluidoAsync(arquivoJson, dataConclusao);
        }
        catch { }
    }

    private async Task CommitConcluidoAsync(string arquivoJson, DateTime data)
    {
        try
        {
            if (!GitHubService.CredenciaisExistem(_rootDir)) return;
            var creds     = GitHubService.CarregarCredenciais(_rootDir)!;
            var repoDir   = GitHubService.RepoDir(_rootDir);
            var remoteUrl = $"https://{creds.Token}@github.com/lfbreciclagemeletronica/Pesagens.git";

            await GitHubService.RunGit($"remote set-url origin {remoteUrl}", repoDir);
            await GitHubService.RunGit($"config user.email \"{creds.GitEmail}\"", repoDir);
            await GitHubService.RunGit($"config user.name \"{creds.GitUsuario}\"", repoDir);
            await GitHubService.RunGit($"add \"{Path.GetFileName(arquivoJson)}\"", repoDir);
            await GitHubService.RunGit($"commit -m \"{NomeCliente} - concluido {data:dd/MM/yyyy}\"", repoDir);
            await GitHubService.RunGit("push origin HEAD", repoDir);
        }
        catch { }
    }

    private void MostrarStatus(string mensagem, bool ok)
    {
        Status   = mensagem;
        StatusOk = ok;
    }
}
