using Avalonia.Controls;
using ControleMateriais.Desktop.ViewModels;
using System.Linq;

namespace ControleMateriais.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private async void OnAbrirTabelaPrecosClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Obter itens pré configurados para colocar na página de preços
        if (DataContext is MainWindowViewModel vm)
        {
            vm.AbrirEdicaoPrecosCommand.Execute(null);
        }
    }
}
