using Avalonia.Controls;
using ControleMateriais.Desktop.Serialization;
using ControleMateriais.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.ObjectModel;
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

    private string _nomeCliente = string.Empty;
    public string NomeCliente
    {
        get => _nomeCliente;
        set { if (value != _nomeCliente) { _nomeCliente = value; OnPropertyChanged(); } }
    }
    public DelegateCommand ExportarCommand { get; }

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


    private bool _isEditandoPrecos;
    public bool IsEditandoPrecos
    {
        get => _isEditandoPrecos;
        set
        {
            if (value != _isEditandoPrecos)
            {
                _isEditandoPrecos = value;
                OnPropertyChanged();
            }
        }
    }

    public PriceTableViewModel PriceVM { get; }
    public ICommand AbrirEdicaoPrecosCommand { get; }


    // Toast simples (banner no topo)
    private string? _toastMessage;
    private bool _toastIsError;
    private bool _toastVisible;


    public string? ToastMessage { get => _toastMessage; private set { _toastMessage = value; OnPropertyChanged(); } }
    public bool ToastIsError { get => _toastIsError; private set { _toastIsError = value; OnPropertyChanged(); } }
    public bool ToastVisible { get => _toastVisible; private set { _toastVisible = value; OnPropertyChanged(); } }


    private void ShowToast(string message, bool isError = false)
    {
        ToastMessage = message;
        ToastIsError = isError;
        ToastVisible = true;
        // opcional: timer para ocultar após alguns segundos
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            ToastVisible = false;
            OnPropertyChanged(nameof(ToastVisible));
        });
        OnPropertyChanged(nameof(ToastVisible));
    }



    public MainWindowViewModel()
    {
        var nomes = new[]
        {
            "PLACA MAE DE NOTEBOOK A",
            "PLACA MAE DE NOTEBOOK B",
            "PLACA MAE DE NOTEBOOK C",
            "PLACA MAE A",
            "PLACA MAE B",
            "PLACA MAE C",
            "PLACA MAE D",
            "PLACA SERVIDOR",
            "PLACA LEVE ESPECIAL",
            "PLACA LEVE ESPECIAL COM PONTA",
            "PLACA LEVE ESPECIAL COMPLETA",
            "PLACA DOURADA",
            "PLACA DOURADA B",
            "PLACA TAPETE A",
            "PLACA TAPETE B",
            "PLACA CONECTOR",
            "PLACA LEVE",
            "PLACA LEVE COM PONTA",
            "PLACA INTERMEDIÁRIA A",
            "PLACA INTERMEDIÁRIA B",
            "PLACA INTERMEDIÁRIA C",
            "PLACA PESADA",
            "PLACA PESADA COM PONTA",
            "PLACA TABLET",
            "PLACA MARROM",
            "HD COMPLETO",
            "HD SEM PLACA E SUCATEADO",
            "PLACA HD",
            "PLACA DE CELULAR MISTA",
            "CELULAR BOTAO E FLIP",
            "SMARTPHONE SEM BATERIA",
            "SMARTPHONE COM BATERIA",
            "CELULAR REPLICA COM E SEM BATERIA",
            "MEMORIA DOURADA",
            "MEMORIA PRATEADA",
            "PROCESSADOR PLASTICO CHAPA A",
            "PROCESSADOR PLASTICO CHAPA B",
            "PROCESSADOR PLASTICO",
            "PROCESSADOR SLOT",
            "PROCESSADOR PLASTICO PRETO",
            "PROCESSADOR CERAMICO A",
            "PROCESSADOR CERAMICO B",
            "PROCESSADOR CERAMICO C",
            "BATERIA DE NOTEBOOK",
            "BATERIA DE TABLET",
            "BATERIA DE CELULAR",
            "FONTE 1",
            "RAIO X",
            "DESMANCHE",
            "SERVIDOR",
            "FONTE 2",
            "OUTROS 1",
            "OUTROS 2",
            "OUTROS 3",
            "IMPUREZAS (PLASTICOS, FERRO, ALUMINIOS, PAPEL)",
        };

        foreach (var nome in nomes)
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

        PriceVM = new PriceTableViewModel(Itens);


        PriceVM.ValoresAtualizados += async (_, __) =>
        {
            await CarregarPrecosNaInicializacaoAsync();
            ShowToast("Valores salvos no JSON com sucesso.", isError: false);
        };

        PriceVM.CloseRequested += (_, __) =>
        {
            IsEditandoPrecos = false;
        };

        AbrirEdicaoPrecosCommand = new DelegateCommand(() =>
        {
            PriceVM.ResetarAposAbrir();
            IsEditandoPrecos = true;
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
        foreach (var it in Itens)
            soma += it.Total;

        TotalGeral = soma;
    }



    private async Task ExportarAsync()
    {
        // 1) Pedir caminho do arquivo
        var sfd = new SaveFileDialog
        {
            Title = "Salvar recibo em PDF",
            Filters =
        {
            new FileDialogFilter() { Name = "PDF", Extensions = { "pdf" } }
        },
            InitialFileName = "recibo.pdf"
        };

        // O dialog precisa de uma Window; vamos tentar usar a Window ativa:
        var topLevel = (Avalonia.Application.Current?.ApplicationLifetime as
                        Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?
                        .MainWindow;

        var filePath = await sfd.ShowAsync(topLevel);
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // 2) Gerar o PDF com QuestPDF
        GerarReciboPdf(filePath);

        ShowToast($"PDF exportado com sucesso: {Path.GetFileName(filePath)}", isError: false);
    }




    // Gera um PDF simples com cabeçalho, tabela dos itens e total geral
    private void GerarReciboPdf(string filePath)
    {
        // Se quiser usar a licença Community de forma explícita:
        // QuestPDF.Settings.License = LicenseType.Community;

        var itensSnapshot = Itens.ToList(); // congelar a coleção no momento do export
        var totalGeralSnapshot = itensSnapshot.Sum(i => i.Total);
        var nomeClienteSnapshot = NomeCliente;
        var dataGeracao = DateTime.Now;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("Recibo LFB Reciclagem Eletrônica").SemiBold().FontSize(18);
                    col.Item().AlignCenter().Text($"Gerado em: {dataGeracao:dd/MM/yyyy}").FontSize(10);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    if (!string.IsNullOrWhiteSpace(nomeClienteSnapshot))
                        col.Item().Text($"Cliente: {nomeClienteSnapshot}").FontSize(12);

                    // Total geral em destaque
                    col.Item().AlignCenter().Text($"Total: {totalGeralSnapshot:C}")
                        .SemiBold().FontSize(14);

                    // Tabela de itens
                    col.Item().Table(table =>
                    {
                        // Definição de colunas (Material | Peso | Preço | Total)
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);  // Material
                            columns.RelativeColumn(1);  // Peso
                            columns.RelativeColumn(1);  // Preço/kg
                            columns.RelativeColumn(1);  // Total
                        });

                        // Cabeçalho
                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("Material");
                            header.Cell().Element(CellHeader).AlignRight().Text("Peso (kg)");
                            header.Cell().Element(CellHeader).AlignRight().Text("Preço/kg");
                            header.Cell().Element(CellHeader).AlignRight().Text("Total");
                        });

                        // Linhas
                        foreach (var it in itensSnapshot)
                        {
                            table.Cell().Element(CellBody).Text(string.IsNullOrWhiteSpace(it.Nome) ? "-" : it.Nome);
                            table.Cell().Element(CellBody).AlignRight().Text($"{it.PesoAtual:N3}");
                            table.Cell().Element(CellBody).AlignRight().Text($"{it.PrecoPorKg:C}");
                            table.Cell().Element(CellBody).AlignRight().Text($"{it.Total:C}");
                        }

                        static IContainer CellHeader(IContainer c) =>
                            c.DefaultTextStyle(x => x.SemiBold())
                             .BorderBottom(1).PaddingVertical(6).PaddingHorizontal(4);

                        static IContainer CellBody(IContainer c) =>
                            c.BorderBottom(0.5f).PaddingVertical(4).PaddingHorizontal(4);
                    });
                });

                page.Footer()
                    .AlignCenter()
                    .Text(txt =>
                    {
                        txt.Span("Página ");
                        txt.CurrentPageNumber();
                        txt.Span(" / ");
                        txt.TotalPages();
                    });
            });
        })
        .GeneratePdf(filePath);
    }

    public async Task CarregarPrecosNaInicializacaoAsync()
    {
        // pega o mês atual
        var competencia = new DateTimeOffset(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        // monta o caminho igual ao PriceTable
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

            // aplica preços por nome (case-sensitive como seu código)
            var dict = loaded.Itens.ToDictionary(x => x.Nome ?? string.Empty, x => x.PrecoPorKg);

            foreach (var item in Itens)
            {
                if (dict.TryGetValue(item.Nome ?? string.Empty, out var preco))
                    item.PrecoPorKg = preco;
            }

            RecalcularTotalGeral();
        }
        else
        {

            // --- CRIA JSON PADRÃO CASO NÃO EXISTA ---
            var novo = new PriceTableViewModel.ValoresMensais
            {
                Competencia = competenciaStr,
                Itens = Itens.Select(i => new PriceTableViewModel.Linha
                {
                    Nome = i.Nome,
                    PrecoPorKg = i.PrecoPorKg
                }).ToList()
            };


            await using (var fsNew = File.Create(filePath))
                await JsonSerializer.SerializeAsync(fsNew, novo, AppJsonContext.Default.ValoresMensais);
        }

    }


}
public class PesoWrapper : ViewModelBase
{
    private readonly MaterialItem _item;
    private string _pesoTexto;
    private bool _editando;

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

    public PesoWrapper(MaterialItem item)
    {
        _item = item;
        _pesoTexto = item.PesoAtual.ToString("N3", CultureInfo.GetCultureInfo("pt-BR"));
        _item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MaterialItem.PrecoPorKg) ||
                e.PropertyName == nameof(MaterialItem.Total))
            {
                OnPropertyChanged(nameof(PrecoPorKg));
                OnPropertyChanged(nameof(Total));
            }
            if (e.PropertyName == nameof(MaterialItem.PesoAtual) && !_editando)
            {
                _pesoTexto = _item.PesoAtual.ToString("N3", CultureInfo.GetCultureInfo("pt-BR"));
                OnPropertyChanged(nameof(PesoTexto));
            }
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

