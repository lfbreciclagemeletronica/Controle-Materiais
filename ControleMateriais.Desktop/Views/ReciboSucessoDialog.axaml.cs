using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Diagnostics;

namespace ControleMateriais.Desktop.Views;

public partial class ReciboSucessoDialog : Window
{
    private string _filePath = string.Empty;
    public bool NovoRecibo { get; private set; }

    public ReciboSucessoDialog(string nomeArquivo, string filePath)
    {
        InitializeComponent();
        NomeArquivoText.Text = nomeArquivo;
        _filePath = filePath;
    }

    public void AtualizarStatus(string mensagem, bool ok)
    {
        StatusText.Text      = mensagem;
        StatusText.Foreground = ok ? new SolidColorBrush(Color.Parse("#4CAF50"))
                                   : new SolidColorBrush(Color.Parse("#F44336"));
        StatusText.IsVisible = !string.IsNullOrEmpty(mensagem);
    }

    private void AbrirPdf_Click(object? sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(_filePath) { UseShellExecute = true }); }
        catch { }
    }

    private void NovoRecibo_Click(object? sender, RoutedEventArgs e)
    {
        NovoRecibo = true;
        Close();
    }
}
