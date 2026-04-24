using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using ControleMateriais.Desktop.ViewModels;

namespace ControleMateriais.Desktop.Views;

public partial class PesagensView : UserControl
{
    public PesagensView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ConectarCallbacks();
    }

    private void ConectarCallbacks()
    {
        if (DataContext is PesagensViewModel vm)
        {
            vm.CarregarPesagens();
            vm.CarregarRecibos();

            if (MainTabControl is not null)
            {
                MainTabControl.SelectionChanged -= TabControl_SelectionChanged;
                MainTabControl.SelectionChanged += TabControl_SelectionChanged;
                // Sincroniza a aba inicial (Pesagens = índice 0)
                SincronizarAba(0, vm);
            }
        }
    }

    private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not PesagensViewModel vm) return;
        if (sender is not TabControl tc) return;
        SincronizarAba(tc.SelectedIndex, vm);
    }

    private static void SincronizarAba(int index, PesagensViewModel vm)
    {
        if (index == 0)
            vm.SincronizarCommand.Execute(null);
        else if (index == 1)
            vm.SincronizarRecibosCommand.Execute(null);
    }

    private void AbrirPdf_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not ReciboItem item) return;
        if (DataContext is not PesagensViewModel vm) return;
        vm.AbrirPdfCommand.Execute(item);
    }

    private void PesagemRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not PesagensViewModel vm) return;
        if (sender is not Border border) return;
        if (border.DataContext is not PesagemItem item) return;
        vm.AbrirReciboCallback?.Invoke(item);
    }

    private void FiltroButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton tb) return;
        if (DataContext is not PesagensViewModel vm) return;
        var tag = tb.Tag as string ?? "todos";
        vm.FiltroStatus = tag;
        // Impede que o ToggleButton fique desmarcado ao clicar novamente no ativo
        tb.IsChecked = true;
    }
}
