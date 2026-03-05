using Avalonia.Controls;
using Avalonia.Input;
using ControleMateriais.Desktop.ViewModels;

namespace ControleMateriais.Desktop.Views;

public partial class WeightCalculatorView : UserControl
{
    public WeightCalculatorView()
    {
        InitializeComponent();
    }

    private void PesoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is WeightItemWrapper wrapper
            && DataContext is WeightCalculatorViewModel vm)
        {
            vm.SelecionarItem(wrapper);
            wrapper.IniciarEdicao();
            tb.SelectAll();
        }
    }

    private void PesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is WeightItemWrapper wrapper)
        {
            if (e.Key == Key.Enter) { wrapper.ConfirmarEdicao(); e.Handled = true; }
            else if (e.Key == Key.Escape) { wrapper.CancelarEdicao(); TopLevel.GetTopLevel(tb)?.Focus(); e.Handled = true; }
        }
    }

    private void PesoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is WeightItemWrapper wrapper)
            wrapper.ConfirmarEdicao();
    }

    private void ItemRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && DataContext is WeightCalculatorViewModel vm)
            vm.SelecionarItem(border.DataContext);
    }
}
