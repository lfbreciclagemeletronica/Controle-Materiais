using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ControleMateriais.Desktop.ViewModels;

namespace ControleMateriais.Desktop.Views;

public partial class VendaView : UserControl
{
    public VendaView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is VendaViewModel vm)
        {
            vm.AbrirModalSucesso = async (filePath, nomeArquivo, gitCallback) =>
            {
                var topLevel = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
                if (topLevel is null) return;

                var dialog = new VendaSucessoDialog(nomeArquivo, filePath);
                var gitTask = dialog.ExecutarGitAsync(gitCallback);
                _ = dialog.ShowDialog(topLevel);
                await gitTask;
            };
        }
    }

    private void PesoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is VendaItemWrapper item)
        {
            item.IniciarEdicao();
            tb.SelectAll();
        }
    }

    private void PesoTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is VendaItemWrapper item)
            item.ConfirmarEdicao();
    }

    private void PesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb && tb.DataContext is VendaItemWrapper item)
        {
            item.ConfirmarEdicao();
            e.Handled = true;
        }
    }

    private void ValorGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is VendaViewModel vm)
        {
            vm.IniciarEdicaoValor();
            (sender as TextBox)?.SelectAll();
        }
    }

    private void ValorLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is VendaViewModel vm)
            vm.ConfirmarEdicaoValor();
    }

    private void ValorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is VendaViewModel vm)
        {
            vm.ConfirmarEdicaoValor();
            (sender as TextBox)?.SelectAll();
            e.Handled = true;
        }
    }
}
