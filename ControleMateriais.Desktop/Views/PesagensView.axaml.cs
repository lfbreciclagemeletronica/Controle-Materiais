using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using ControleMateriais.Desktop.ViewModels;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Views;

public partial class PesagensView : UserControl
{
    private bool _sincPesagensFeita;
    private bool _sincRecibosFeita;

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

            vm.ConfirmarDeletarReciboCallback  = AbrirConfirmDeleteReciboAsync;
            vm.ConfirmarDeletarPesagemCallback = AbrirConfirmDeletePesagemAsync;
            vm.ConfirmarReconstruirBancoDadosCallback = async msg =>
            {
                var owner = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
                if (owner is null) return false;
                var dlg = new ConfirmDeleteDialog(msg);
                await dlg.ShowDialog(owner);
                return dlg.Confirmado;
            };

            // Quando um novo recibo é publicado, reseta flag para re-sync na próxima abertura da aba
            vm.NovoReciboPublicadoCallback = () => _sincRecibosFeita = false;

            if (MainTabControl is not null)
            {
                MainTabControl.SelectionChanged -= TabControl_SelectionChanged;
                MainTabControl.SelectionChanged += TabControl_SelectionChanged;
                // Sincroniza aba inicial (Pesagens) apenas se ainda não foi feita
                SincronizarAbaSePrimeira(0, vm);
            }
        }
    }

    private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not PesagensViewModel vm) return;
        if (sender is not TabControl tc) return;
        SincronizarAbaSePrimeira(tc.SelectedIndex, vm);
    }

    private void SincronizarAbaSePrimeira(int index, PesagensViewModel vm)
    {
        if (index == 0 && !_sincPesagensFeita)
        {
            _sincPesagensFeita = true;
            vm.SincronizarCommand.Execute(null);
        }
        else if (index == 1 && !_sincRecibosFeita)
        {
            _sincRecibosFeita = true;
            vm.SincronizarRecibosCommand.Execute(null);
        }
    }

    private void AbrirPdf_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not ReciboItem item) return;
        if (DataContext is not PesagensViewModel vm) return;
        vm.AbrirPdfCommand.Execute(item);
    }

    private void DeletarRecibo_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not ReciboItem item) return;
        if (DataContext is not PesagensViewModel vm) return;
        vm.DeletarReciboCommand.Execute(item);
    }

    private void DeletarPesagem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not PesagemItem item) return;
        if (DataContext is not PesagensViewModel vm) return;
        vm.DeletarPesagemCommand.Execute(item);
    }

    private void PesagemNome_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not PesagensViewModel vm) return;
        if (sender is not TextBlock tb) return;
        if (tb.DataContext is not PesagemItem item) return;
        vm.AbrirReciboCallback?.Invoke(item);
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
        tb.IsChecked = true;
    }

    private async Task AbrirConfirmDeleteReciboAsync(ReciboItem item)
    {
        var owner = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
        if (owner is null) return;

        var mensagem = $"Deseja excluir permanentemente o recibo:\n\n“{item.NomeArquivo}”\n{item.DataCriacao}?\n\nEsta ação não pode ser desfeita.";
        var dialog = new ConfirmDeleteDialog(mensagem);
        await dialog.ShowDialog(owner);

        if (dialog.Confirmado && DataContext is PesagensViewModel vm)
            await vm.DeletarReciboAsync(item);
    }

    private async Task AbrirConfirmDeletePesagemAsync(PesagemItem item)
    {
        var owner = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
        if (owner is null) return;

        var mensagem = $"Deseja excluir permanentemente a pesagem:\n\n“{item.Cliente}”\n{item.Horario}?\n\nEsta ação não pode ser desfeita.";
        var dialog = new ConfirmDeleteDialog(mensagem);
        await dialog.ShowDialog(owner);

        if (dialog.Confirmado && DataContext is PesagensViewModel vm)
            await vm.DeletarPesagemAsync(item);
    }
}
