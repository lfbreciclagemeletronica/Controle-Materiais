using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControleMateriais.Desktop.Views;

public partial class EnvioSucessoDialog : Window
{
    public EnvioSucessoDialog(string nomeArquivo)
    {
        InitializeComponent();
        NomeArquivoText.Text = nomeArquivo;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e) => Close();
}
