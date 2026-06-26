using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace ControleMateriais.Desktop.Views;

public partial class VendaSucessoDialog : Window
{
    private string _filePath = string.Empty;

    public VendaSucessoDialog(string nomeArquivo, string filePath)
    {
        InitializeComponent();
        NomeArquivoText.Text = nomeArquivo;
        _filePath = filePath;
    }

    private void AbrirPdf_Click(object? sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(_filePath) { UseShellExecute = true }); }
        catch { }
    }

    private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();
}
