using Avalonia.Controls;
using ControleMateriais.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using IContainer = QuestPDF.Infrastructure.IContainer;


namespace ControleMateriais.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<MaterialItem> Itens { get; } = new();
    private decimal _totalGeral;
    public DelegateCommand ExportarCommand { get; }
    static IContainer CellHeader(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold())
        .BorderBottom(1)
        .PaddingVertical(6)
        .PaddingHorizontal(4);

    static IContainer CellBody(IContainer c) =>
        c.BorderBottom(0.5f)
         .PaddingVertical(4)
         .PaddingHorizontal(4);

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

    public MainWindowViewModel()
    {
      Itens.Add(new MaterialItem
        {Nome = "Placas pesadas", PesoAtual = 0m, PrecoPorKg = 2.00m}
      );
      Itens.Add(new MaterialItem
        { Nome = "Placas leves", PesoAtual = 0m, PrecoPorKg = 4.00m }
      );

        // Controlar os valores
        Itens.CollectionChanged += OnItensCollectionChanged;

        foreach (var it in Itens)
        {
            SubscribeItem(it);
        }
        RecalcularTotalGeral();

        ExportarCommand = new DelegateCommand(async () => await ExportarAsync());
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

        // (Opcional) você pode exibir uma notificação/toast/snackbar
    }




    // Gera um PDF simples com cabeçalho, tabela dos itens e total geral
    private void GerarReciboPdf(string filePath)
    {
        // Se quiser usar a licença Community de forma explícita:
        // QuestPDF.Settings.License = LicenseType.Community;

        var itensSnapshot = Itens.ToList(); // congelar a coleção no momento do export
        var totalGeralSnapshot = itensSnapshot.Sum(i => i.Total);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .AlignCenter()
                    .Text("Recibo de Materiais")
                    .SemiBold().FontSize(18);

                page.Content().Column(col =>
                {
                    col.Spacing(10);

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

