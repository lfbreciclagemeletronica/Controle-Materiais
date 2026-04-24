using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControleMateriais.Desktop.Views;

public partial class GitHubAjudaDialog : Window
{
    public GitHubAjudaDialog()
    {
        InitializeComponent();
    }

    private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();
}
