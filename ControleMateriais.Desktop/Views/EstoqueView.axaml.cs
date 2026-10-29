using Avalonia.Controls;
using Avalonia.Input;
using ControleMateriais.Data.Models;
using ControleMateriais.Desktop.ViewModels;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Views;

public partial class EstoqueView : UserControl
{
    public EstoqueView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is EstoqueViewModel vm)
            {
                vm.Recarregar();
                _ = vm.CarregarEntradasAsync();
                _ = vm.CarregarTotaisMensaisAsync();
                _ = vm.CarregarVendasBancoAsync();

                vm.ConfirmarExclusaoCallback = async msg =>
                {
                    var topLevel = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
                    if (topLevel is null) return false;
                    var dlg = new ConfirmDeleteDialog(msg);
                    await dlg.ShowDialog(topLevel);
                    return dlg.Confirmado;
                };
                vm.AbrirModalExclusaoCallback = async etapas =>
                {
                    var topLevel = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
                    if (topLevel is null) return;
                    var dlg = new ExclusaoReciboSucessoDialog(etapas);
                    await dlg.ShowDialog(topLevel);
                };
                vm.AbrirDetalhesEntradaCallback = async recibo =>
                {
                    var topLevel = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
                    if (topLevel is null) return;
                    var dlg = new EntradaDetalhesDialog(recibo);
                    await dlg.ShowDialog(topLevel);
                };
                vm.AbrirDetalhesVendaCallback = async venda =>
                {
                    var topLevel = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
                    if (topLevel is null) return;
                    var dlg = new VendaDetalhesDialog(venda);
                    await dlg.ShowDialog(topLevel);
                };
            }
        };
    }

    private void EntradaRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control ctrl) return;
        if (ctrl.DataContext is not ReciboCompra item) return;
        if (DataContext is not EstoqueViewModel vm) return;
        vm.AbrirEntradaCommand.Execute(item);
    }

    private void VendaBancoRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control ctrl) return;
        if (ctrl.DataContext is not ReciboVenda item) return;
        if (DataContext is not EstoqueViewModel vm) return;
        vm.AbrirVendaBancoCommand.Execute(item);
    }
}
