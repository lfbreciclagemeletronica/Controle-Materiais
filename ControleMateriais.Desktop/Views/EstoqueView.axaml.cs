using Avalonia.Controls;
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
                vm.CarregarListaEstoquesIniciais();
                vm.Recarregar();
                vm.ConfirmarExclusaoCallback = async msg =>
                {
                    var topLevel = TopLevel.GetTopLevel(this) as Avalonia.Controls.Window;
                    if (topLevel is null) return false;
                    var dlg = new ConfirmDeleteDialog(msg);
                    await dlg.ShowDialog(topLevel);
                    return dlg.Confirmado;
                };
            }
        };
    }
}
