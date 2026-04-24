using Avalonia.Controls;
using Avalonia.Input;
using ControleMateriais.Desktop.Services;
using ControleMateriais.Desktop.ViewModels;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Views;

public partial class WeightCalculatorView : UserControl
{
    public WeightCalculatorView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ConectarCallbacks();
    }

    private void ConectarCallbacks()
    {
        if (DataContext is WeightCalculatorViewModel vm)
        {
            vm.SolicitarConfiguracaoGitHubCallback = AbrirDialogoGitHubAsync;
            vm.MostrarSucessoCallback = AbrirDialogoSucessoAsync;
        }
    }

    private async Task AbrirDialogoSucessoAsync(string nomeArquivo)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) return;

        var dialog = new EnvioSucessoDialog(nomeArquivo);
        await dialog.ShowDialog(owner);

        if (DataContext is WeightCalculatorViewModel vm)
            vm.LimparCommand.Execute(null);
    }

    private async Task AbrirDialogoGitHubAsync()
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) return;

        var dialog = new GitHubConfigDialog();
        await dialog.ShowDialog(owner);

        var config = (GitHubConfigViewModel)dialog.DataContext!;
        if (config.Confirmado && DataContext is WeightCalculatorViewModel vm)
            GitHubService.SalvarCredenciais(vm.RootDir, config.Token, config.GitUsuario, config.GitEmail);
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
