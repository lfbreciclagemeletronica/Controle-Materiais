using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ControleMateriais.Desktop.Services;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace ControleMateriais.Desktop.Views;

public partial class LogViewerWindow : Window
{
    private readonly AvaloniaList<LogEntry> _filtrada = new();
    private string _filtro = string.Empty;

    public LogViewerWindow()
    {
        InitializeComponent();

        LogItemsControl.ItemsSource = _filtrada;

        RecarregarFiltro();
        AppLogger.Entries.CollectionChanged += Entries_CollectionChanged;
        Closed += (_, _) => AppLogger.Entries.CollectionChanged -= Entries_CollectionChanged;
    }

    private void Entries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RecarregarFiltro();
            if (AutoScrollToggle.IsChecked == true)
                LogScrollViewer.ScrollToEnd();
        });
    }

    private void FiltroBox_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        _filtro = FiltroBox.Text?.Trim() ?? string.Empty;
        RecarregarFiltro();
    }

    private void RecarregarFiltro()
    {
        var itens = string.IsNullOrEmpty(_filtro)
            ? AppLogger.Entries.ToList()
            : AppLogger.Entries
                .Where(l => l.Linha.Contains(_filtro, StringComparison.OrdinalIgnoreCase))
                .ToList();

        _filtrada.Clear();
        _filtrada.AddRange(itens);
    }

    private void Limpar_Click(object? sender, RoutedEventArgs e)
    {
        AppLogger.Clear();
        RecarregarFiltro();
    }
}
