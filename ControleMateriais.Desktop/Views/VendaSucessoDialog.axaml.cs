using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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

    public void AtualizarStatus(string mensagem, bool ok = true)
    {
        StatusText.Text      = mensagem;
        StatusText.Foreground = ok ? new SolidColorBrush(Color.Parse("#4CAF50"))
                                   : new SolidColorBrush(Color.Parse("#F44336"));
        StatusText.IsVisible = !string.IsNullOrEmpty(mensagem);
    }

    /// <summary>Executa o callback de Git e atualiza status em tempo real.</summary>
    public async Task ExecutarGitAsync(Func<Action<string>, Task> gitCallback)
    {
        await gitCallback(msg =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => AtualizarStatus(msg)));
        Avalonia.Threading.Dispatcher.UIThread.Post(() => AtualizarStatus("Sincronização concluída!", true));
    }

    private void AbrirPdf_Click(object? sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(_filePath) { UseShellExecute = true }); }
        catch { }
    }

    private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();
}
