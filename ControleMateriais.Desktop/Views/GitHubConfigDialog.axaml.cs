using Avalonia.Controls;
using Avalonia.Interactivity;
using ControleMateriais.Desktop.ViewModels;

namespace ControleMateriais.Desktop.Views;

public partial class GitHubConfigDialog : Window
{
    public GitHubConfigDialog()
    {
        InitializeComponent();
        var vm = new GitHubConfigViewModel();
        vm.FecharDialog = Close;
        vm.AbrirAjuda = AbrirAjuda;
        DataContext = vm;
    }

    private void Salvar_Click(object? sender, RoutedEventArgs e) =>
        ((GitHubConfigViewModel)DataContext!).Confirmar();

    private void Cancelar_Click(object? sender, RoutedEventArgs e) =>
        ((GitHubConfigViewModel)DataContext!).Cancelar();

    private void Ajuda_Click(object? sender, RoutedEventArgs e) =>
        ((GitHubConfigViewModel)DataContext!).MostrarAjuda();

    private async void AbrirAjuda()
    {
        var ajuda = new GitHubAjudaDialog();
        await ajuda.ShowDialog(this);
    }
}
