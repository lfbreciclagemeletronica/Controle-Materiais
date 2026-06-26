using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ControleMateriais.Desktop.ViewModels;

namespace ControleMateriais.Desktop.Views;

public partial class EstoqueInicialView : UserControl
{
    public EstoqueInicialView()
    {
        InitializeComponent();
    }

    // --- Itens do catálogo ---
    private void PesoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoInicialWrapper w) { w.IniciarEdicao(); tb.SelectAll(); }
    }

    private void PesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoInicialWrapper w)
        {
            if (e.Key == Key.Enter)  { w.ConfirmarEdicao(); e.Handled = true; }
            else if (e.Key == Key.Escape) { w.CancelarEdicao(); TopLevel.GetTopLevel(tb)?.Focus(); e.Handled = true; }
        }
    }

    private void PesoTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoInicialWrapper w) w.ConfirmarEdicao();
    }

    // --- Impurezas ---
    private void ImpurezasPesoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && DataContext is EstoqueInicialViewModel vm) { vm.IniciarEdicaoImpurezas(); tb.SelectAll(); }
    }

    private void ImpurezasPesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && DataContext is EstoqueInicialViewModel vm)
        {
            if (e.Key == Key.Enter)  { vm.ConfirmarEdicaoImpurezas(); e.Handled = true; }
            else if (e.Key == Key.Escape) { vm.CancelarEdicaoImpurezas(); TopLevel.GetTopLevel(tb)?.Focus(); e.Handled = true; }
        }
    }

    private void ImpurezasPesoTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EstoqueInicialViewModel vm) vm.ConfirmarEdicaoImpurezas();
    }

    // --- Itens personalizados (peso) ---
    private void CustomPesoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ItemPersonalizadoInicial c) { c.IniciarEdicao(); tb.SelectAll(); }
    }

    private void CustomPesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ItemPersonalizadoInicial c)
        {
            if (e.Key == Key.Enter)  { c.ConfirmarEdicao(); e.Handled = true; }
            else if (e.Key == Key.Escape) { c.CancelarEdicao(); TopLevel.GetTopLevel(tb)?.Focus(); e.Handled = true; }
        }
    }

    private void CustomPesoTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ItemPersonalizadoInicial c) c.ConfirmarEdicao();
    }
}
