using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ControleMateriais.Desktop.Services;
using System;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Views;

public partial class SincronizandoDialog : Window
{
    private bool _concluido = false;

    public SincronizandoDialog()
    {
        InitializeComponent();
    }

    public void AtualizarStatus(string mensagem)
    {
        Dispatcher.UIThread.Post(() => StatusText.Text = mensagem);
    }

    public void MarcarConcluido()
    {
        _concluido = true;
        Dispatcher.UIThread.Post(() =>
        {
            StatusText.Text = "Sincronização concluída!";
            Close();
        });
    }

    public void MarcarErro(string mensagem)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusText.Text = $"Erro: {mensagem}";
            FecharBtn.IsVisible = true;
        });
    }

    private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_concluido && FecharBtn.IsVisible == false)
            e.Cancel = true;
        base.OnClosing(e);
    }
}
