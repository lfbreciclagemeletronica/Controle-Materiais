using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ControleMateriais.Desktop.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ControleMateriais.Desktop.ViewModels;

public class VendaItemWrapper : ViewModelBase
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

    public string Nome { get; }

    private decimal _pesoAtual;
    public decimal PesoAtual
    {
        get => _pesoAtual;
        private set { if (value != _pesoAtual) { _pesoAtual = value; OnPropertyChanged(); OnPropertyChanged(nameof(PesoTexto)); PesoChanged?.Invoke(); } }
    }

    private string _pesoTexto = "0,000";
    public string PesoTexto
    {
        get => _pesoTexto;
        set
        {
            if (value != _pesoTexto)
            {
                _pesoTexto = value;
                OnPropertyChanged();
            }
        }
    }

    public Action? PesoChanged { get; set; }

    public VendaItemWrapper(string nome)
    {
        Nome = nome;
    }

    public void IniciarEdicao()
    {
        if (_pesoTexto == "0,000")
        {
            _pesoTexto = string.Empty;
            OnPropertyChanged(nameof(PesoTexto));
        }
    }

    public void ConfirmarEdicao()
    {
        if (decimal.TryParse(_pesoTexto.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
            || decimal.TryParse(_pesoTexto, NumberStyles.Any, PtBR, out v))
            PesoAtual = v >= 0 ? v : 0;
        else
            PesoAtual = 0;
        _pesoTexto = PesoAtual.ToString("N3", PtBR);
        OnPropertyChanged(nameof(PesoTexto));
    }

    public void ResetarPeso()
    {
        PesoAtual  = 0;
        _pesoTexto = "0,000";
        OnPropertyChanged(nameof(PesoTexto));
    }
}

public class VendaViewModel : ViewModelBase
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _rootDir;
    private readonly Action _voltarCallback;

    public ObservableCollection<VendaItemWrapper> Itens      { get; } = new();
    public ObservableCollection<VendaItemWrapper> ItensFixos  { get; } = new();
    public ObservableCollection<VendaItemWrapper> ItensExtras { get; } = new();

    private bool _temItensExtras;
    public bool TemItensExtras
    {
        get => _temItensExtras;
        private set { if (value != _temItensExtras) { _temItensExtras = value; OnPropertyChanged(); } }
    }

    private string _nomeCliente = string.Empty;
    public string NomeCliente
    {
        get => _nomeCliente;
        set { if (value != _nomeCliente) { _nomeCliente = value; OnPropertyChanged(); } }
    }

    private decimal _valorVendaRaw = 0m;
    private string _valorVendaTexto = "R$ 0,00";
    public string ValorVendaTexto
    {
        get => _valorVendaTexto;
        set { if (value != _valorVendaTexto) { _valorVendaTexto = value; OnPropertyChanged(); } }
    }

    public void IniciarEdicaoValor()
    {
        _valorVendaTexto = _valorVendaRaw == 0m ? string.Empty : _valorVendaRaw.ToString("N2", PtBR);
        OnPropertyChanged(nameof(ValorVendaTexto));
    }

    public void ConfirmarEdicaoValor()
    {
        var raw = _valorVendaTexto.Replace("R$", "").Trim();
        if (!decimal.TryParse(raw.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out _valorVendaRaw))
            decimal.TryParse(raw, NumberStyles.Any, PtBR, out _valorVendaRaw);
        if (_valorVendaRaw < 0) _valorVendaRaw = 0;
        _valorVendaTexto = _valorVendaRaw.ToString("C", PtBR);
        OnPropertyChanged(nameof(ValorVendaTexto));
    }

    private decimal _pesoTotal;
    public decimal PesoTotal
    {
        get => _pesoTotal;
        private set { if (value != _pesoTotal) { _pesoTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(PesoTotalStr)); } }
    }
    public string PesoTotalStr => PesoTotal.ToString("N3", PtBR) + " kg";

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        private set { if (value != _status) { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusVisivel)); } }
    }
    public bool StatusVisivel => !string.IsNullOrEmpty(_status);

    private bool _salvando;
    public bool Salvando
    {
        get => _salvando;
        private set { if (value != _salvando) { _salvando = value; OnPropertyChanged(); } }
    }

    public ICommand VoltarCommand  { get; }
    public ICommand SalvarCommand  { get; }
    public ICommand LimparCommand  { get; }

    public Func<Task<Window?>>? ObterJanelaCallback { get; set; }

    // Callback: (filePath, nomeArquivo, progressoGit) => void
    public Action<string, string, Func<Action<string>, Task>>? AbrirModalSucesso { get; set; }

    public VendaViewModel(string rootDir, Action voltarCallback)
    {
        _rootDir       = rootDir;
        _voltarCallback = voltarCallback;

        foreach (var nome in ItemCatalog.OrderedItems)
        {
            var item = new VendaItemWrapper(nome);
            item.PesoChanged = RecalcularTotal;
            Itens.Add(item);
            ItensFixos.Add(item);
        }

        VoltarCommand = new DelegateCommand(() => _voltarCallback());
        SalvarCommand = new DelegateCommand(() => _ = SalvarVendaAsync());
        LimparCommand = new DelegateCommand(Limpar);
    }

    public void CarregarItensExtras()
    {
        var totais = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var bancoDadosDir = GitHubService.BancoDadosRepoDir(_rootDir);
        var estoqueInicialPath = Path.Combine(bancoDadosDir, "estoque-inicial.json");

        if (File.Exists(estoqueInicialPath))
        {
            try
            {
                var obj = JsonNode.Parse(File.ReadAllText(estoqueInicialPath))?.AsObject();
                if (obj is not null)
                {
                    foreach (var kvp in obj)
                    {
                        if (kvp.Key.Equals("data", StringComparison.OrdinalIgnoreCase)) continue;
                        if (kvp.Value is JsonValue jv && jv.TryGetValue<decimal>(out var d))
                            totais[kvp.Key] = d;
                    }
                }
            }
            catch { }
        }

        var nomesExtrasEstoque = totais.Keys
            .Where(k => !ItemCatalog.OrderedItems.Contains(k, StringComparer.OrdinalIgnoreCase))
            .OrderBy(n => n)
            .ToList();

        // Rebuild ItensExtras from scratch to reflect current estoque
        // Remove from Itens any extra that is no longer in the estoque
        var extrasParaRemover = Itens
            .Where(i => !ItemCatalog.OrderedItems.Contains(i.Nome, StringComparer.OrdinalIgnoreCase)
                        && !nomesExtrasEstoque.Contains(i.Nome, StringComparer.OrdinalIgnoreCase))
            .ToList();
        foreach (var r in extrasParaRemover)
            Itens.Remove(r);

        var nomesExtrasAtuais = Itens
            .Where(i => !ItemCatalog.OrderedItems.Contains(i.Nome, StringComparer.OrdinalIgnoreCase))
            .Select(i => i.Nome)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Add new extras not yet in Itens
        foreach (var nome in nomesExtrasEstoque)
        {
            if (!nomesExtrasAtuais.Contains(nome))
            {
                var item = new VendaItemWrapper(nome);
                item.PesoChanged = RecalcularTotal;
                Itens.Add(item);
            }
        }

        // Rebuild ItensExtras observable collection so ItemsControl updates
        ItensExtras.Clear();
        foreach (var nome in nomesExtrasEstoque)
        {
            var item = Itens.First(i => i.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase));
            ItensExtras.Add(item);
        }

        TemItensExtras = ItensExtras.Count > 0;
        RecalcularTotal();
    }

    private void RecalcularTotal()
    {
        PesoTotal = Itens.Sum(i => i.PesoAtual);
    }

    private void Limpar()
    {
        NomeCliente      = string.Empty;
        _valorVendaRaw   = 0m;
        _valorVendaTexto = "R$ 0,00";
        OnPropertyChanged(nameof(ValorVendaTexto));
        foreach (var i in Itens) i.ResetarPeso();
        RecalcularTotal();
        Status = string.Empty;
        CarregarItensExtras();
    }

    private async Task SalvarVendaAsync()
    {
        var itensSelecionados = Itens.Where(i => i.PesoAtual > 0).ToList();
        if (!itensSelecionados.Any())
        {
            Status = "Informe o peso de pelo menos um item antes de salvar.";
            return;
        }
        if (string.IsNullOrWhiteSpace(NomeCliente))
        {
            Status = "Informe o nome do cliente.";
            return;
        }

        Salvando = true;
        Status   = "Salvando venda...";
        var nomeArquivoFinal = string.Empty;
        var filePathFinal    = string.Empty;
        var data = DateTime.Now;
        try
        { 
            var nomeSeguro = string.Concat(NomeCliente.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            var nomeBase   = $"{nomeSeguro}_{data:dd-MM-yyyy}";
            nomeArquivoFinal = nomeBase + ".pdf";

            var vendaDir = RecibosVendaDir(_rootDir);
            Directory.CreateDirectory(vendaDir);
            filePathFinal = Path.Combine(vendaDir, nomeArquivoFinal);

            // 1. Gera PDF
            Status = "Gerando PDF da venda...";
            GerarPdfVenda(filePathFinal, itensSelecionados, _valorVendaRaw, data);

            // 2. Cria/atualiza arquivo venda-DD-MM-YYYY.json no banco-de-dados
            Status = "Salvando dados da venda...";
            var bancoDadosDir = GitHubService.BancoDadosRepoDir(_rootDir);
            Directory.CreateDirectory(bancoDadosDir);
            var vendaJsonFile = $"venda-{data:dd-MM-yyyy}.json";
            var vendaJsonPath = Path.Combine(bancoDadosDir, vendaJsonFile);

            JsonObject root;
            if (File.Exists(vendaJsonPath))
            {
                root = JsonNode.Parse(File.ReadAllText(vendaJsonPath))?.AsObject() ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
                root["data"] = data.ToString("dd/MM/yyyy");
            }

            if (!root.ContainsKey("registros"))
                root["registros"] = new JsonArray();

            var registros = root["registros"]!.AsArray();
            var reg = new JsonObject
            {
                ["nome"] = NomeCliente,
                ["materiais"] = new JsonArray()
            };

            foreach (var item in itensSelecionados.OrderBy(i => i.Nome))
            {
                var mat = new JsonObject
                {
                    ["descricao"] = item.Nome,
                    ["peso"] = JsonValue.Create(item.PesoAtual)
                };
                reg["materiais"]!.AsArray().Add(mat);
            }
            registros.Add(reg);

            File.WriteAllText(vendaJsonPath, root.ToJsonString(JsonOpts), Encoding.UTF8);

            // 3. Salva metadados leves para leitura na aba de recibos
            var meta = new JsonObject
            {
                ["cliente"]    = NomeCliente,
                ["pesoTotal"]  = JsonValue.Create(itensSelecionados.Sum(i => i.PesoAtual)),
                ["valorVenda"] = JsonValue.Create(_valorVendaRaw),
                ["data"]       = data.ToString("dd/MM/yyyy")
            };
            await File.WriteAllTextAsync(
                filePathFinal + ".meta.json",
                meta.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Status = $"Erro ao salvar venda: {ex.Message}";
            Salvando = false;
            return;
        }

        // 3. Abre modal de sucesso — Git roda dentro dele
        Salvando = false;
        Status = string.Empty;
        var clienteSnapshot = NomeCliente;
        var vendaJsonNome   = $"venda-{data:dd-MM-yyyy}.json";
        AbrirModalSucesso?.Invoke(filePathFinal, nomeArquivoFinal,
            async progresso =>
            {
                if (!GitHubService.CredenciaisExistem(_rootDir)) return;
                try
                {
                    await GitHubService.PublicarReciboVendaAsync(_rootDir, filePathFinal,
                        $"Venda {clienteSnapshot} - {DateTime.Now:dd/MM/yyyy}",
                        msg2 => Avalonia.Threading.Dispatcher.UIThread.Post(() => progresso(msg2)));

                    var bancoDadosDir = GitHubService.BancoDadosRepoDir(_rootDir);
                    var vendaJsonPath = Path.Combine(bancoDadosDir, vendaJsonNome);
                    var vendaConteudo = await File.ReadAllTextAsync(vendaJsonPath);
                    await GitHubService.PublicarJsonBancoDadosAsync(_rootDir, vendaJsonNome, vendaConteudo,
                        msg2 => Avalonia.Threading.Dispatcher.UIThread.Post(() => progresso(msg2)));
                }
                catch (Exception ex)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => progresso($"Erro Git: {ex.Message}"));
                }
            });
        Limpar();
    }

    public static string RecibosVendaDir(string rootDir) =>
        Path.Combine(GitHubService.RecibosRepoDir(rootDir), "Recibos_Venda");

    private void GerarPdfVenda(string filePath, List<VendaItemWrapper> itens, decimal valorVenda, DateTime data)
    {
        var pesoTotal = itens.Sum(i => i.PesoAtual);
        var ptBR      = PtBR;

        byte[]? logoBytes = null;
        try
        {
            var uri = new Uri("avares://ControleMateriais.Desktop/Assets/lfb-logo.png");
            using var logoStream = Avalonia.Platform.AssetLoader.Open(uri);
            using var logoMs     = new MemoryStream();
            logoStream.CopyTo(logoMs);
            logoBytes = logoMs.ToArray();
        }
        catch { }

        var borderColor  = Colors.Grey.Darken2;
        var cellFontSize = 10f;
        var headerFontSize = 10f;

        static IContainer InfoLabelCell(IContainer c) =>
            c.Border(0.5f).BorderColor(Colors.Grey.Darken2)
             .Background(Colors.Grey.Lighten3)
             .PaddingVertical(5).PaddingHorizontal(4);

        static IContainer InfoCell(IContainer c) =>
            c.Border(0.5f).BorderColor(Colors.Grey.Darken2)
             .PaddingVertical(5).PaddingHorizontal(4);

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

                    // Cabeçalho
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
                                       .Text("LFB RECICLAGEM ELETRONICA")
                                       .Bold().FontSize(13);
                                   left.Item().PaddingTop(2).AlignCenter()
                                       .Text("CNPJ: 243.250.67/0001-64  |  I.E: 096/4003708  |  End: Rua Sergio Jungblut Dieterich, 1011-B")
                                       .FontSize(7.5f);
                                   left.Item().PaddingTop(4).AlignCenter()
                                       .Text("RECIBO DE VENDA DE ESTOQUE")
                                       .Bold().FontSize(9.5f);
                               });
                           });
                       });

                    // Grade info: CLIENTE | PESO TOTAL | VALOR VENDA | DATA
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

                        info.Cell().Element(InfoLabelCell).AlignCenter().Text("CLIENTE").Bold().FontSize(8f);
                        info.Cell().Element(InfoCell).AlignCenter().Text(NomeCliente).FontSize(9f);
                        info.Cell().Element(InfoLabelCell).AlignCenter().Text("PESO").Bold().FontSize(8f);
                        info.Cell().Element(InfoCell).AlignCenter()
                            .Text($"{pesoTotal:N3} kg").Bold().FontSize(9f);
                        info.Cell().Element(InfoLabelCell).AlignCenter().Text("VALOR").Bold().FontSize(8f);
                        info.Cell().Element(InfoCell).AlignCenter()
                            .Text(valorVenda.ToString("C", ptBR)).Bold().FontSize(9f);
                        info.Cell().Element(InfoLabelCell).AlignCenter().Text("DATA").Bold().FontSize(8f);
                        info.Cell().Element(InfoCell).AlignCenter()
                            .Text($"{data:dd/MM/yyyy}").FontSize(9f);
                    });

                    col.Item().Height(6);

                    // Tabela de itens: só MATERIAL e KG
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5f);
                            c.RelativeColumn(2f);
                        });

                        table.Header(header =>
                        {
                            static IContainer HCell(IContainer c) =>
                                c.Background(Colors.Grey.Lighten3)
                                 .Border(0.5f).BorderColor(Colors.Grey.Darken2)
                                 .PaddingVertical(5).PaddingHorizontal(4);

                            header.Cell().Element(HCell).AlignCenter()
                                  .Text("MATERIAL").Bold().FontSize(headerFontSize);
                            header.Cell().Element(HCell).AlignCenter()
                                  .Text("KG").Bold().FontSize(headerFontSize);
                        });

                        static IContainer BCell(IContainer c) =>
                            c.Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                             .PaddingVertical(4).PaddingHorizontal(5);

                        foreach (var it in itens)
                        {
                            table.Cell().Element(BCell).AlignCenter()
                                 .Text(it.Nome).FontSize(cellFontSize);
                            table.Cell().Element(BCell).AlignCenter()
                                 .Text(it.PesoAtual.ToString("N3", ptBR)).FontSize(cellFontSize);
                        }
                    });
                });
            });
        })
        .GeneratePdf(filePath);
    }
}
