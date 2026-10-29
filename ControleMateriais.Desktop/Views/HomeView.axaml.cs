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
                config.Token, config.GitUsuario, config.GitEmail,
                config.UrlPesagens, config.UrlRecibos, config.UrlTabelaPrecos, config.UrlBancoDados);

            if (DataContext is MainWindowViewModel vm)
                vm.GitConfigurado = true;
        }
    }

    private void AbrirRelease_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel { NovaVersaoUrl.Length: > 0 } vm)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(vm.NovaVersaoUrl) { UseShellExecute = true });
    }

    private static LogViewerWindow? _logViewer;

    private void VerLogs_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;

        // Reutiliza a janela se já estiver aberta, só traz para frente.
        if (_logViewer is not null)
        {
            _logViewer.Activate();
            return;
        }

        _logViewer = new LogViewerWindow();
        _logViewer.Closed += (_, _) => _logViewer = null;

        if (owner is not null)
            _logViewer.Show(owner);
        else
            _logViewer.Show();
    }
}
