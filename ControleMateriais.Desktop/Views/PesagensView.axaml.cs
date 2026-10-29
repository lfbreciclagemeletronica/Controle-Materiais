using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using ControleMateriais.Data.Models;
using ControleMateriais.Desktop.ViewModels;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Views;

public partial class PesagensView : UserControl
{
    private bool _sincComprasFeita;
    private bool _sincVendasFeita;
    private bool _sincPesagensFeita;
    private bool _fornecedoresCarregados;

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

            vm.ConfirmarDeletarPesagemCallback = AbrirConfirmDeletePesagemAsync;
            vm.EditarFornecedorCallback        = AbrirEditarFornecedorAsync;
            vm.ConfirmarInativarFornecedorCallback = AbrirConfirmInativarFornecedorAsync;
            vm.ConfirmarReativarFornecedorCallback = AbrirConfirmReativarFornecedorAsync;

            // Quando um novo recibo é publicado, atualiza a lista imediatamente
            vm.NovoReciboPublicadoCallback = () =>
            {
                _sincComprasFeita = true;
                vm.CarregarRecibos();
            };

            // Callbacks da aba Vendas (reaproveita a instância de EstoqueViewModel)
            if (vm.EstoqueVM is not null)
            {
                vm.EstoqueVM.ConfirmarExclusaoCallback = async msg =>
                {
                    var owner = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
                    if (owner is null) return false;
                    var dlg = new ConfirmDeleteDialog(msg);
                    await dlg.ShowDialog(owner);
                    return dlg.Confirmado;
                };
                vm.EstoqueVM.AbrirModalExclusaoCallback = async etapas =>
                {
                    var owner = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
                    if (owner is null) return;
                    var dlg = new ExclusaoReciboSucessoDialog(etapas);
                    await dlg.ShowDialog(owner);
                };
            }

            if (MainTabControl is not null)
            {
                MainTabControl.SelectionChanged -= TabControl_SelectionChanged;
                MainTabControl.SelectionChanged += TabControl_SelectionChanged;
                // Sincroniza aba inicial (Compras) apenas se ainda não foi feita
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
        if (index == 0 && !_sincComprasFeita)
        {
            _sincComprasFeita = true;
            vm.CarregarRecibos();
        }
        else if (index == 1 && !_sincVendasFeita)
        {
            _sincVendasFeita = true;
            vm.EstoqueVM?.Recarregar();
        }
        else if (index == 2 && !_sincPesagensFeita)
        {
            _sincPesagensFeita = true;
            vm.CarregarPesagens();
        }
        else if (index == 3 && !_fornecedoresCarregados)
        {
            _fornecedoresCarregados = true;
            _ = vm.CarregarFornecedoresAsync();
        }
    }

    private void AbrirPdf_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not ReciboItem item) return;
        if (DataContext is not PesagensViewModel vm) return;
        vm.AbrirPdfCommand.Execute(item);
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

    private void EditarFornecedor_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not Usuario item) return;
        if (DataContext is not PesagensViewModel vm) return;
        vm.EditarFornecedorCommand.Execute(item);
    }

    private void ExcluirFornecedor_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not Usuario item) return;
        if (DataContext is not PesagensViewModel vm) return;
        vm.InativarFornecedorCommand.Execute(item);
    }

    private void StatusFornecedor_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control ctrl) return;
        if (ctrl.DataContext is not Usuario item) return;
        if (item.Ativo) return; // só reage ao clique quando inativo
        if (DataContext is not PesagensViewModel vm) return;
        vm.ReativarFornecedorCommand.Execute(item);
    }

    private async Task AbrirEditarFornecedorAsync(Usuario usuario)
    {
        var owner = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
        if (owner is null) return;

        var dialog = new EditarFornecedorDialog(usuario);
        await dialog.ShowDialog(owner);

        if (dialog.Confirmado && DataContext is PesagensViewModel vm)
            await vm.SalvarFornecedorAsync(dialog.Usuario);
    }

    private async Task<bool> AbrirConfirmInativarFornecedorAsync(Usuario usuario)
    {
        var owner = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
        if (owner is null) return false;

        var mensagem = $"Deseja inativar o fornecedor:\n\n“{usuario.Nome}”?\n\nOs recibos associados não serão afetados.";
        var dialog = new ConfirmDeleteDialog(mensagem, titulo: "Confirmar inativação", textoConfirmar: "Inativar");
        await dialog.ShowDialog(owner);
        return dialog.Confirmado;
    }

    private async Task<bool> AbrirConfirmReativarFornecedorAsync(Usuario usuario)
    {
        var owner = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
        if (owner is null) return false;

        var mensagem = $"Deseja reativar o fornecedor:\n\n“{usuario.Nome}”?";
        var dialog = new ConfirmDeleteDialog(mensagem, titulo: "Confirmar reativação", textoConfirmar: "Reativar",
            corConfirmar: "#4CAF50");
        await dialog.ShowDialog(owner);
        return dialog.Confirmado;
    }
}
