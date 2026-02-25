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
        if (sender is TextBox tb && tb.DataContext is ItemPrecoWrapper wrapper)
        {
            wrapper.IniciarEdicao();
            tb.SelectAll();
        }
    }

    private void PrecoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb && tb.DataContext is ItemPrecoWrapper wrapper)
        {
            wrapper.ConfirmarEdicao();
            e.Handled = true;
        }
    }

    private void PrecoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ItemPrecoWrapper wrapper)
        {
            wrapper.ConfirmarEdicao();
        }
    }
}
