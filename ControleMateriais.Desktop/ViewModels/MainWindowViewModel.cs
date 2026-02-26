using Avalonia.Controls;
using ControleMateriais.Desktop.Serialization;
using ControleMateriais.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using IContainer = QuestPDF.Infrastructure.IContainer;


namespace ControleMateriais.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<MaterialItem> Itens { get; } = new();
    public ObservableCollection<PesoWrapper> ItensEditaveis { get; } = new();
    private decimal _totalGeral;
    private decimal _pesoTotal;

    public decimal PesoTotal
    {
        get => _pesoTotal;
        private set
        {
            if (value != _pesoTotal)
            {
                _pesoTotal = value;
                OnPropertyChanged();
            }
        }
    }

    private string _nomeCliente = string.Empty;
    public string NomeCliente
    {
        get => _nomeCliente;
        set { if (value != _nomeCliente) { _nomeCliente = value; OnPropertyChanged(); } }
    }
    public DelegateCommand ExportarCommand { get; }

    public PriceTableManagerViewModel TabelaVM { get; }
    public ICommand AbrirTabelaPrecosCommand { get; }

    private bool _isGerindoTabela;
    public bool IsGerindoTabela
    {
        get => _isGerindoTabela;
        set { if (value != _isGerindoTabela) { _isGerindoTabela = value; OnPropertyChanged(); } }
    }

    public decimal TotalGeral
    {
        get => _totalGeral;
        private set
        {
            if (value != _totalGeral)
            {
                _totalGeral = value;
                OnPropertyChanged();
            }
        }
    }




    // Toast simples (banner no topo)
    private string? _toastMessage;
    private bool _toastIsError;
    private bool _toastIsSuccess;
    private bool _toastVisible;


    public string? ToastMessage { get => _toastMessage; private set { _toastMessage = value; OnPropertyChanged(); } }
    public bool ToastIsError { get => _toastIsError; private set { _toastIsError = value; OnPropertyChanged(); } }
    public bool ToastIsSuccess { get => _toastIsSuccess; private set { _toastIsSuccess = value; OnPropertyChanged(); } }
    public bool ToastVisible { get => _toastVisible; private set { _toastVisible = value; OnPropertyChanged(); } }
    public bool ToastIsInfo => !_toastIsError && !_toastIsSuccess;


    private void ShowToast(string message, bool isError = false, bool isSuccess = false)
    {
        ToastMessage = message;
        ToastIsError = isError;
        ToastIsSuccess = isSuccess;
        ToastVisible = true;
        OnPropertyChanged(nameof(ToastIsInfo));
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            ToastVisible = false;
            OnPropertyChanged(nameof(ToastVisible));
        });
        OnPropertyChanged(nameof(ToastVisible));
    }



    public MainWindowViewModel()
    {
        foreach (var nome in ItemCatalog.OrderedItems)
            Itens.Add(new MaterialItem { Nome = nome, PesoAtual = 0m, PrecoPorKg = 0m });

        // Controlar os valores
        Itens.CollectionChanged += OnItensCollectionChanged;

        foreach (var it in Itens)
        {
            SubscribeItem(it);
            ItensEditaveis.Add(new PesoWrapper(it));
        }
        RecalcularTotalGeral();

        ExportarCommand = new DelegateCommand(async () => await ExportarAsync());

        TabelaVM = new PriceTableManagerViewModel(Itens);
        TabelaVM.CloseRequested += (_, __) => IsGerindoTabela = false;
        TabelaVM.TabelaSalvaRequested += (_, nome) => ShowToast($"Tabela \"{nome}\" salva com sucesso.", isError: false);
        TabelaVM.ImportToastRequested += (_, t) => ShowToast(t.Mensagem, isError: t.IsErro, isSuccess: t.IsSuccess);
        TabelaVM.PrecosAtualizados += (_, e) =>
        {
            foreach (var w in ItensEditaveis)
                w.AtualizarExibicaoPreco();
            RecalcularTotalGeral();
            ShowToast($"Tabela \"{e.NomeTabela}\" ativada com sucesso.", isError: false);
        };

        AbrirTabelaPrecosCommand = new DelegateCommand(async () =>
        {
            await TabelaVM.InicializarAsync();
            IsGerindoTabela = true;
        });

        _ = CarregarPrecosNaInicializacaoAsync();



    }

    private void OnItensCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (MaterialItem it in e.OldItems)
                UnsubscribeItem(it);
        }

        if (e.NewItems is not null)
        {
            foreach (MaterialItem it in e.NewItems)
                SubscribeItem(it);
        }

        RecalcularTotalGeral();
    }

    private void SubscribeItem(MaterialItem item)
    {
        item.PropertyChanged += OnItemPropertyChanged;
    }

    private void UnsubscribeItem(MaterialItem item)
    {
        item.PropertyChanged -= OnItemPropertyChanged;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Sempre que PesoAtual, PrecoPorKg ou Total mudarem, atualize o TotalGeral
        if (e.PropertyName == nameof(MaterialItem.PesoAtual) ||
            e.PropertyName == nameof(MaterialItem.PrecoPorKg) ||
            e.PropertyName == nameof(MaterialItem.Total))
        {
            RecalcularTotalGeral();
        }
    }

    private void RecalcularTotalGeral()
    {
        decimal soma = 0m;
        decimal peso = 0m;
        foreach (var it in Itens)
        {
            soma += it.Total;
            peso += it.PesoAtual;
        }
        TotalGeral = soma;
        PesoTotal = peso;
    }



    private async Task ExportarAsync()
    {
        if (string.IsNullOrWhiteSpace(NomeCliente))
        {
            ShowToast("Informe o nome do fornecedor antes de exportar.", isError: true);
            return;
        }

        var data = DateTime.Now;
        var nomeArquivo = $"{NomeCliente}_{data:dd-MM-yyyy}.pdf"
            .Replace("/", "-").Replace("\\", "-").Replace(":", "-");

        var sfd = new SaveFileDialog
        {
            Title = "Salvar recibo em PDF",
            Filters =
            {
                new FileDialogFilter() { Name = "PDF", Extensions = { "pdf" } }
            },
            InitialFileName = nomeArquivo
        };

        var topLevel = (Avalonia.Application.Current?.ApplicationLifetime as
                        Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?
                        .MainWindow;

        var filePath = await sfd.ShowAsync(topLevel);
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        GerarReciboPdf(filePath);

        ShowToast($"PDF exportado com sucesso: {Path.GetFileName(filePath)}", isError: false);
    }




    private void GerarReciboPdf(string filePath)
    {
        var itensSnapshot = Itens.ToList();
        var pesoTotalSnapshot = itensSnapshot.Sum(i => i.PesoAtual);
        var totalGeralSnapshot = itensSnapshot.Sum(i => i.Total);
        var nomeClienteSnapshot = NomeCliente;
        var dataGeracao = DateTime.Now;
        var ptBR = CultureInfo.GetCultureInfo("pt-BR");

        var borderColor = Colors.Grey.Darken2;
        var cellFontSize = 7f;
        var headerFontSize = 7f;

        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "lfb-logo.png");

        static IContainer InfoLabelCell(IContainer c) =>
            c.Border(0.5f).BorderColor(Colors.Grey.Darken2)
             .Background(Colors.Grey.Lighten3)
             .PaddingVertical(3).PaddingHorizontal(5);

        static IContainer InfoCell(IContainer c) =>
            c.Border(0.5f).BorderColor(Colors.Grey.Darken2)
             .PaddingVertical(3).PaddingHorizontal(5);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(1.2f, Unit.Centimetre);
                page.MarginBottom(1.2f, Unit.Centimetre);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(cellFontSize).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    col.Spacing(0);

                    // ── TÍTULO: texto empresa (esq) + logo (dir) ──────────────────────
                    col.Item().Border(0.5f).BorderColor(borderColor).Table(title =>
                    {
                        title.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);  // texto
                            c.RelativeColumn(1);  // logo
                        });

                        // Esquerda: nome empresa + dados + título pesagem
                        title.Cell().Border(0.5f).BorderColor(borderColor)
                             .PaddingVertical(6).PaddingHorizontal(8).Column(left =>
                             {
                                 left.Item().AlignCenter()
                                     .Text("LFB RECICLAGEM ELETRONICA")
                                     .Bold().FontSize(11);
                                 left.Item().PaddingTop(2).AlignCenter()
                                     .Text("CNPJ: 243.250.67/0001-64  |  I.E: 096/4003708")
                                     .FontSize(7);
                                 left.Item().AlignCenter()
                                     .Text("End: Rua Sergio Jungblut Dieterich, 1011, Letra B Galpao5")
                                     .FontSize(7);
                                 left.Item().PaddingTop(4).AlignCenter()
                                     .Text("RESULTADO DA PESAGEM E TRIAGEM LFB")
                                     .Bold().FontSize(8);
                             });

                        // Direita: logo
                        title.Cell().Border(0.5f).BorderColor(borderColor)
                             .Background("#4CAF50").AlignCenter().AlignMiddle().Padding(4)
                             .Column(logo =>
                             {
                                 if (File.Exists(logoPath))
                                     logo.Item().AlignCenter().Width(55).Image(logoPath);
                                 else
                                     logo.Item().AlignCenter().Text("LFB").Bold()
                                         .FontColor(Colors.White).FontSize(14);
                             });
                    });

                    // ── GRADE INFO: FORNECEDOR / PESO / VALOR / DATA ───────────────────
                    col.Item().Table(info =>
                    {
                        info.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);  // label
                            c.RelativeColumn(3);  // value
                        });

                        info.Cell().Element(InfoLabelCell).Text("FORNECEDOR").Bold().FontSize(7);
                        info.Cell().Element(InfoCell).Text(nomeClienteSnapshot).FontSize(7);

                        info.Cell().Element(InfoLabelCell).Text("PESO TOTAL").Bold().FontSize(7);
                        info.Cell().Element(InfoCell)
                            .Text($"{pesoTotalSnapshot:N3} kg").FontSize(7);

                        info.Cell().Element(InfoLabelCell).Text("VALOR TOTAL").Bold().FontSize(7);
                        info.Cell().Element(InfoCell)
                            .Text(totalGeralSnapshot.ToString("C", ptBR)).FontSize(7);

                        info.Cell().Element(InfoLabelCell).Text("DATA").Bold().FontSize(7);
                        info.Cell().Element(InfoCell)
                            .Text($"{dataGeracao:dd/MM/yyyy}").FontSize(7);
                    });

                    // Espaço entre header e tabela
                    col.Item().Height(5);

                    // ── TABELA DE ITENS ────────────────────────────────────────────────
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);    // Material
                            c.RelativeColumn(1.2f); // KG
                            c.RelativeColumn(1.5f); // VALOR
                            c.RelativeColumn(1.5f); // TOTAL
                        });

                        table.Header(header =>
                        {
                            static IContainer HCell(IContainer c) =>
                                c.Background(Colors.Grey.Lighten3)
                                 .Border(0.5f).BorderColor(Colors.Grey.Darken2)
                                 .PaddingVertical(3).PaddingHorizontal(4);

                            header.Cell().Element(HCell).Text(string.Empty);
                            header.Cell().Element(HCell).AlignCenter()
                                  .Text("KG").Bold().FontSize(headerFontSize);
                            header.Cell().Element(HCell).AlignCenter()
                                  .Text("VALOR").Bold().FontSize(headerFontSize);
                            header.Cell().Element(HCell).AlignCenter()
                                  .Text("TOTAL").Bold().FontSize(headerFontSize);
                        });

                        foreach (var it in itensSnapshot)
                        {
                            static IContainer BCell(IContainer c) =>
                                c.Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                                 .PaddingVertical(2).PaddingHorizontal(4);

                            bool temPeso = it.PesoAtual > 0;

                            table.Cell().Element(BCell)
                                 .Text(it.Nome ?? string.Empty).FontSize(cellFontSize);

                            table.Cell().Element(BCell).AlignCenter()
                                 .Text(temPeso ? it.PesoAtual.ToString("N3") : string.Empty)
                                 .FontSize(cellFontSize);

                            table.Cell().Element(BCell).AlignCenter()
                                 .Text(it.PrecoPorKg > 0
                                     ? it.PrecoPorKg.ToString("C", ptBR)
                                     : string.Empty)
                                 .FontSize(cellFontSize);

                            table.Cell().Element(BCell).AlignCenter()
                                 .Text(it.Total > 0
                                     ? it.Total.ToString("C", ptBR)
                                     : string.Empty)
                                 .FontSize(cellFontSize);
                        }
                    });
                });
            });
        })
        .GeneratePdf(filePath);
    }

    public class ValoresMensais
    {
        public string Competencia { get; set; } = string.Empty;
        public List<LinhaPreco> Itens { get; set; } = new();
    }

    public class LinhaPreco
    {
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoPorKg { get; set; }
    }

    public async Task CarregarPrecosNaInicializacaoAsync()
    {
        var competencia = new DateTimeOffset(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "Controle-Materiais-Registros");

        Directory.CreateDirectory(baseDir);

        var competenciaStr = competencia.ToString("yyyy-MM");
        var filePath = Path.Combine(baseDir, $"valores_{competenciaStr}.json");

        if (File.Exists(filePath))
        {
            await using var fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            var loaded = await JsonSerializer.DeserializeAsync(
                fs,
                AppJsonContext.Default.ValoresMensais);

            if (loaded?.Itens is null)
                return;

            var dict = loaded.Itens.ToDictionary(x => x.Nome ?? string.Empty, x => x.PrecoPorKg);

            foreach (var item in Itens)
            {
                if (dict.TryGetValue(item.Nome ?? string.Empty, out var preco))
                    item.PrecoPorKg = preco;
            }

            foreach (var w in ItensEditaveis)
                w.AtualizarExibicaoPreco();

            RecalcularTotalGeral();
        }
    }


}
public class PesoWrapper : ViewModelBase
{
    private readonly MaterialItem _item;
    private string _pesoTexto;
    private bool _editando;
    private string _precoTexto;
    private bool _editandoPreco;

    public string Nome => _item.Nome;

    public decimal PrecoPorKg => _item.PrecoPorKg;
    public decimal Total => _item.Total;

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

    public string PrecoTexto
    {
        get => _precoTexto;
        set
        {
            if (value != _precoTexto)
            {
                _precoTexto = value;
                OnPropertyChanged();
            }
        }
    }

    public PesoWrapper(MaterialItem item)
    {
        _item = item;
        _pesoTexto = item.PesoAtual.ToString("N3", CultureInfo.GetCultureInfo("pt-BR"));
        _precoTexto = item.PrecoPorKg.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        _item.PropertyChanged += (_, e) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (e.PropertyName == nameof(MaterialItem.PrecoPorKg) ||
                    e.PropertyName == nameof(MaterialItem.Total))
                {
                    OnPropertyChanged(nameof(PrecoPorKg));
                    OnPropertyChanged(nameof(Total));
                    if (!_editandoPreco)
                    {
                        _precoTexto = _item.PrecoPorKg.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
                        OnPropertyChanged(nameof(PrecoTexto));
                    }
                }
                if (e.PropertyName == nameof(MaterialItem.PesoAtual) && !_editando)
                {
                    _pesoTexto = _item.PesoAtual.ToString("N3", CultureInfo.GetCultureInfo("pt-BR"));
                    OnPropertyChanged(nameof(PesoTexto));
                }
            });
        };
    }

    public void IniciarEdicao()
    {
        _editando = true;
        PesoTexto = string.Empty;
    }

    public void ConfirmarEdicao()
    {
        if (!_editando) return;
        _editando = false;

        var raw = PesoTexto.Trim().Replace(" ", "");

        if (raw.Contains(',') && raw.Contains('.'))
        {
            raw = raw.Replace(".", "").Replace(",", ".");
        }
        else
        {
            raw = raw.Replace(",", ".");
        }

        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            parsed = _item.PesoAtual;

        _item.PesoAtual = parsed;
        PesoTexto = parsed.ToString("N3", CultureInfo.GetCultureInfo("pt-BR"));
    }

    public void IniciarEdicaoPreco()
    {
        _editandoPreco = true;
        PrecoTexto = string.Empty;
    }

    public void ConfirmarEdicaoPreco()
    {
        if (!_editandoPreco) return;
        _editandoPreco = false;

        var raw = PrecoTexto.Trim()
                            .Replace("R$", "")
                            .Replace(" ", "")
                            .Trim();

        if (raw.Contains(',') && raw.Contains('.'))
        {
            raw = raw.Replace(".", "").Replace(",", ".");
        }
        else
        {
            raw = raw.Replace(",", ".");
        }

        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            parsed = _item.PrecoPorKg;

        _item.PrecoPorKg = parsed;
        PrecoTexto = parsed.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
    }

    public void AtualizarExibicaoPreco()
    {
        _editandoPreco = false;
        PrecoTexto = _item.PrecoPorKg.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
    }
}

public sealed class DelegateCommand : ICommand
{
    private readonly Func<bool>? _canExecute;
    private readonly Action _execute;

    public DelegateCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

