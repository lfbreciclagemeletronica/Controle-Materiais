using Avalonia.Controls;
using Avalonia.Input;
using ControleMateriais.Desktop.Services;
using ControleMateriais.Desktop.ViewModels;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainWindowViewModel();
        vm.AbrirDialogoGitHubCallback = AbrirDialogoGitHubAsync;
        DataContext = vm;
        Opened += async (_, _) => await VerificarInicializacaoAsync(vm);
    }

    private async Task VerificarInicializacaoAsync(MainWindowViewModel vm)
    {
        if (!GitHubService.CredenciaisExistem(MainWindowViewModel.RootDirPublic))
        {
            await AbrirDialogoGitHubAsync();
            vm.GitConfigurado = GitHubService.CredenciaisExistem(MainWindowViewModel.RootDirPublic);
        }
    }

    private async Task AbrirDialogoGitHubAsync()
    {
        var credExistentes = GitHubService.CarregarCredenciais(MainWindowViewModel.RootDirPublic);
        var dialog = new GitHubConfigDialog(credExistentes);
        await dialog.ShowDialog(this);
        var config = (GitHubConfigViewModel)dialog.DataContext!;
        if (config.Confirmado)
        {
            GitHubService.SalvarCredenciais(
                MainWindowViewModel.RootDirPublic,
                config.Token, config.GitUsuario, config.GitEmail);
            if (DataContext is MainWindowViewModel vm)
                vm.GitConfigurado = true;
        }
    }

    private void PrecoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoWrapper wrapper
            && DataContext is MainWindowViewModel vm)
        {
            vm.SelecionarItem(wrapper);
            wrapper.IniciarEdicaoPreco();
            tb.SelectAll();
        }
    }

    private void PrecoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoWrapper wrapper)
        {
            if (e.Key == Key.Enter) { wrapper.ConfirmarEdicaoPreco(); e.Handled = true; }
            else if (e.Key == Key.Escape) { wrapper.CancelarEdicaoPreco(); TopLevel.GetTopLevel(tb)?.Focus(); e.Handled = true; }
        }
    }

    private void PrecoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoWrapper wrapper)
        {
            wrapper.ConfirmarEdicaoPreco();
        }
    }

    private void TabelaPrecoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ItemPrecoWrapper wrapper
            && DataContext is MainWindowViewModel vm)
        {
            vm.TabelaVM.SelecionarItemCommand.Execute(wrapper);
            wrapper.IniciarEdicao();
            tb.SelectAll();
        }
    }

    private void TabelaPrecoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ItemPrecoWrapper wrapper)
        {
            if (e.Key == Key.Enter) { wrapper.ConfirmarEdicao(); e.Handled = true; }
            else if (e.Key == Key.Escape) { wrapper.CancelarEdicao(); TopLevel.GetTopLevel(tb)?.Focus(); e.Handled = true; }
        }
    }

    private void TabelaPrecoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ItemPrecoWrapper wrapper)
        {
            wrapper.ConfirmarEdicao();
        }
    }

    private void PesoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoWrapper wrapper
            && DataContext is MainWindowViewModel vm)
        {
            vm.SelecionarItem(wrapper);
            wrapper.IniciarEdicao();
            tb.SelectAll();
        }
    }

    private void PesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoWrapper wrapper)
        {
            if (e.Key == Key.Enter) { wrapper.ConfirmarEdicao(); e.Handled = true; }
            else if (e.Key == Key.Escape) { wrapper.CancelarEdicao(); TopLevel.GetTopLevel(tb)?.Focus(); e.Handled = true; }
        }
    }

    private void PesoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PesoWrapper wrapper)
        {
            wrapper.ConfirmarEdicao();
        }
    }

    private void CustomPesoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper
            && DataContext is MainWindowViewModel vm)
        {
            vm.SelecionarItem(wrapper);
            wrapper.IniciarEdicaoPeso();
            tb.SelectAll();
        }
    }

    private void CustomPesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper)
        {
            if (e.Key == Key.Enter) { wrapper.ConfirmarEdicaoPeso(); e.Handled = true; }
            else if (e.Key == Key.Escape) { wrapper.CancelarEdicaoPeso(); TopLevel.GetTopLevel(tb)?.Focus(); e.Handled = true; }
        }
    }

    private void CustomPesoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper)
        {
            wrapper.ConfirmarEdicaoPeso();
        }
    }

    private void CustomPrecoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper
            && DataContext is MainWindowViewModel vm)
        {
            vm.SelecionarItem(wrapper);
            wrapper.IniciarEdicaoPreco();
            tb.SelectAll();
        }
    }

    private void CustomPrecoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper)
        {
            if (e.Key == Key.Enter) { wrapper.ConfirmarEdicaoPreco(); e.Handled = true; }
            else if (e.Key == Key.Escape) { wrapper.CancelarEdicaoPreco(); TopLevel.GetTopLevel(tb)?.Focus(); e.Handled = true; }
        }
    }

    private void CustomPrecoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CustomItemWrapper wrapper)
        {
            wrapper.ConfirmarEdicaoPreco();
        }
    }

    private void ImpurezasPesoTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb && DataContext is MainWindowViewModel vm)
        {
            vm.IniciarEdicaoImpurezas();
            tb.SelectAll();
        }
    }

    private void ImpurezasPesoTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            if (e.Key == Key.Enter) { vm.ConfirmarEdicaoImpurezas(); e.Handled = true; }
            else if (e.Key == Key.Escape && sender is TextBox tb) { vm.CancelarEdicaoImpurezas(); TopLevel.GetTopLevel(tb)?.Focus(); e.Handled = true; }
        }
    }

    private void ImpurezasPesoTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ConfirmarEdicaoImpurezas();
        }
    }

    private void ItemRow_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Avalonia.Controls.Border border
            && DataContext is MainWindowViewModel vm)
        {
            vm.SelecionarItem(border.DataContext);
        }
    }

    private void TabelaItemRow_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Avalonia.Controls.Border border
            && border.DataContext is ItemPrecoWrapper item
            && DataContext is MainWindowViewModel vm)
        {
            vm.TabelaVM.SelecionarItemCommand.Execute(item);
        }
    }
}
