using Avalonia.Controls;
using Avalonia.Input;
using ControleMateriais.Desktop.ViewModels;

namespace ControleMateriais.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void PrecoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoWrapper wrapper)
        {
            wrapper.IniciarEdicaoPreco();
            tb.SelectAll();
        }
    }

    private void PrecoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb && tb.DataContext is PesoWrapper wrapper)
        {
            wrapper.ConfirmarEdicaoPreco();
            e.Handled = true;
        }
    }

    private void PrecoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoWrapper wrapper)
        {
            wrapper.ConfirmarEdicaoPreco();
        }
    }

    private void TabelaPrecoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ItemPrecoWrapper wrapper)
        {
            wrapper.IniciarEdicao();
            tb.SelectAll();
        }
    }

    private void TabelaPrecoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb && tb.DataContext is ItemPrecoWrapper wrapper)
        {
            wrapper.ConfirmarEdicao();
            e.Handled = true;
        }
    }

    private void TabelaPrecoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ItemPrecoWrapper wrapper)
        {
            wrapper.ConfirmarEdicao();
        }
    }

    private void PesoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoWrapper wrapper)
        {
            wrapper.IniciarEdicao();
            tb.SelectAll();
        }
    }

    private void PesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb && tb.DataContext is PesoWrapper wrapper)
        {
            wrapper.ConfirmarEdicao();
            e.Handled = true;
        }
    }

    private void PesoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoWrapper wrapper)
        {
            wrapper.ConfirmarEdicao();
        }
    }

    private void CustomPesoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper)
        {
            wrapper.IniciarEdicaoPeso();
            tb.SelectAll();
        }
    }

    private void CustomPesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper)
        {
            wrapper.ConfirmarEdicaoPeso();
            e.Handled = true;
        }
    }

    private void CustomPesoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper)
        {
            wrapper.ConfirmarEdicaoPeso();
        }
    }

    private void CustomPrecoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper)
        {
            wrapper.IniciarEdicaoPreco();
            tb.SelectAll();
        }
    }

    private void CustomPrecoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper)
        {
            wrapper.ConfirmarEdicaoPreco();
            e.Handled = true;
        }
    }

    private void CustomPrecoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper)
        {
            wrapper.ConfirmarEdicaoPreco();
        }
    }
}
