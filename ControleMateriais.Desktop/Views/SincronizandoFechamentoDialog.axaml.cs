using Avalonia.Controls;
using Avalonia.Interactivity;
using ControleMateriais.Desktop.Services;
using ControleMateriais.Desktop.ViewModels;
using System;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Views;

public partial class SincronizandoFechamentoDialog : Window
{
    private readonly SincronizandoFechamentoViewModel _vm;

    public SincronizandoFechamentoDialog(string rootDir)
    {
        InitializeComponent();
        _vm = new SincronizandoFechamentoViewModel();
        DataContext = _vm;

        Opened += async (_, _) => await ExecutarSincronizacaoAsync(rootDir);
    }

    private async Task ExecutarSincronizacaoAsync(string rootDir)
    {
        _vm.IniciarLoading();

        try
        {
            await GitHubService.SincronizarTudoAoFecharAsync(rootDir, msg =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => InterpretarMensagem(msg));
            });
        }
        catch (Exception ex)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _vm.MarcarRepoErro("Recibos", ex.Message);
                _vm.MarcarRepoErro("Pesagens", ex.Message);
                _vm.MarcarRepoErro("Banco de Dados", ex.Message);
            });
        }
        finally
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                EnsureAllDone();
                _vm.MarcarConcluido();
            });
        }
    }

    private static readonly string[] _repos = ["Recibos", "Pesagens", "Banco de Dados"];

    private void InterpretarMensagem(string msg)
    {
        foreach (var repo in _repos)
        {
            if (!msg.Contains(repo)) continue;

            if (msg.EndsWith("sincronizado.") || msg.EndsWith("já atualizado."))
                _vm.MarcarRepoOk(repo, msg.EndsWith("atualizado.") ? "Já atualizado" : "Enviado ao GitHub");
            return;
        }
    }

    private void EnsureAllDone()
    {
        if (_vm.SyncRecibos.IsLoading)    _vm.MarcarRepoOk("Recibos", "Concluído");
        if (_vm.SyncPesagens.IsLoading)   _vm.MarcarRepoOk("Pesagens", "Concluído");
        if (_vm.SyncBancoDados.IsLoading) _vm.MarcarRepoOk("Banco de Dados", "Concluído");
    }

    private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();
}
