using Avalonia.Controls;
using Avalonia.Interactivity;
using ControleMateriais.Desktop.Services;
using ControleMateriais.Desktop.ViewModels;

namespace ControleMateriais.Desktop.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private async void ConfigurarGitHub_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) return;

        var credExistentes = GitHubService.CarregarCredenciais(MainWindowViewModel.RootDirPublic);
        var dialog = new GitHubConfigDialog(credExistentes);
        await dialog.ShowDialog(owner);

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
}
