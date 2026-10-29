using Avalonia.Threading;
using ControleMateriais.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.ViewModels;

public enum SyncStatus { Loading, Ok, Warn, Error }

public class SyncItem : INotifyPropertyChanged
{
    private SyncStatus _status = SyncStatus.Loading;
    private string _mensagem = string.Empty;

    private string _spinner = "⠋";

    public string Label { get; init; } = string.Empty;
    public string LabelConcluido { get; init; } = string.Empty;
    public string LabelAviso { get; init; } = string.Empty;
    public string MensagemErro { get; init; } = "Falha ao atualizar neste computador!";
    public string ErroTecnico { get; set; } = string.Empty;
    public bool TemErroTecnico => !string.IsNullOrEmpty(ErroTecnico);

    public string Spinner
    {
        get => _spinner;
        set { _spinner = value; OnPropertyChanged(); }
    }

    public string Texto => _status switch
    {
        SyncStatus.Loading => Label,
        SyncStatus.Ok => LabelConcluido,
        SyncStatus.Warn => string.IsNullOrEmpty(LabelAviso) ? Label : LabelAviso,
        _ => string.IsNullOrEmpty(_mensagem) ? MensagemErro : _mensagem
    };

    public void DefinirErro(string tecnico)
    {
        ErroTecnico = tecnico;
        OnPropertyChanged(nameof(ErroTecnico));
        OnPropertyChanged(nameof(TemErroTecnico));
        Mensagem = MensagemErro;
        Status = SyncStatus.Error;
    }

    public SyncStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(Texto)); OnPropertyChanged(nameof(IsLoading)); OnPropertyChanged(nameof(IsOk)); OnPropertyChanged(nameof(IsWarn)); OnPropertyChanged(nameof(IsError)); OnPropertyChanged(nameof(IsDone)); }
    }

    public string Mensagem
    {
        get => _mensagem;
        set { _mensagem = value; OnPropertyChanged(); OnPropertyChanged(nameof(Texto)); }
    }

    public ObservableCollection<string> Detalhes { get; } = new();

    public bool IsLoading => _status == SyncStatus.Loading;
    public bool IsOk      => _status == SyncStatus.Ok;
    public bool IsWarn    => _status == SyncStatus.Warn;
    public bool IsError   => _status == SyncStatus.Error;
    public bool IsDone    => _status != SyncStatus.Loading;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? p = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}

public class SplashViewModel : INotifyPropertyChanged
{
    private static string RootDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                     "Downloads", "ControleMateriaisLFB");

    private bool _podeContinu  = false;
    private bool _semCredenciais = false;

    public bool PodeContinuar
    {
        get => _podeContinu;
        private set { _podeContinu = value; OnPropertyChanged(); }
    }

    public bool SemCredenciais
    {
        get => _semCredenciais;
        private set { _semCredenciais = value; OnPropertyChanged(); }
    }

    private static readonly string[] SpinnerFrames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(90) };
    private int _frame;
    private string _barraTexto = string.Empty;

    public SyncItem ItemRecibos    { get; } = new() { Label = "Atualizando recibos de compra...", LabelConcluido = "Recibos de compra atualizados", LabelAviso = "Recibos de compra não atualizados", MensagemErro = "Falha em atualizar os recibos neste computador!" };
    public SyncItem ItemPesagens   { get; } = new() { Label = "Atualizando pesagens...", LabelConcluido = "Pesagens atualizadas", LabelAviso = "Pesagens não atualizadas", MensagemErro = "Falha em atualizar as pesagens neste computador!" };
    public SyncItem ItemBancoDados { get; } = new() { Label = "Atualizando banco de dados...", LabelConcluido = "Banco de dados atualizado", LabelAviso = "Banco de dados não atualizado", MensagemErro = "Falha em atualizar o banco de dados neste computador!" };
    public SyncItem ItemVendas     { get; } = new() { Label = "Atualizando recibos de venda...", LabelConcluido = "Recibos de venda atualizados", LabelAviso = "Recibos de venda não atualizados", MensagemErro = "Falha em atualizar os recibos de venda neste computador!" };

    private bool _semInternet;
    public bool SemInternet
    {
        get => _semInternet;
        private set { _semInternet = value; OnPropertyChanged(); }
    }

    private static async Task<bool> TemInternetAsync()
    {
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            using var r = await http.GetAsync("https://github.com", System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
            return true;
        }
        catch { return false; }
    }

    public IReadOnlyList<SyncItem> Itens { get; }

    public string BarraTexto
    {
        get => _barraTexto;
        private set { _barraTexto = value; OnPropertyChanged(); }
    }

    public SplashViewModel()
    {
        Itens = new[] { ItemRecibos, ItemPesagens, ItemBancoDados, ItemVendas };
        foreach (var i in Itens)
            i.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(SyncItem.Status)) AtualizarBarra(); };
        AtualizarBarra();

        _timer.Tick += (_, _) =>
        {
            var f = SpinnerFrames[_frame++ % SpinnerFrames.Length];
            foreach (var i in Itens) i.Spinner = f;
        };
        _timer.Start();
    }

    private void AtualizarBarra()
    {
        const int largura = 30;
        var pct = Itens.Count(i => i.IsOk) * 100 / Itens.Count;
        var cheio = pct * largura / 100;
        BarraTexto = $"[{new string('█', cheio)}{new string('░', largura - cheio)}] {pct,3}%";
        if (Itens.All(i => i.IsDone)) _timer.Stop();
        if (Itens.All(i => i.IsOk)) SyncInfo.UltimaAtualizacao = DateTime.Now;
    }

    public async Task IniciarSincronizacaoAsync()
    {
        if (!GitHubService.CredenciaisExistem(RootDir))
        {
            UI(() =>
            {
                SemCredenciais = true;
                foreach (var item in new[] { ItemRecibos, ItemPesagens, ItemBancoDados, ItemVendas })
                {
                    item.Status   = SyncStatus.Error;
                    item.Mensagem = "GitHub não configurado";
                }
                PodeContinuar = true;
            });
            return;
        }

        if (!await TemInternetAsync())
        {
            UI(() =>
            {
                SemInternet = true;
                foreach (var item in Itens) item.Status = SyncStatus.Warn;
                PodeContinuar = true;
            });
            return;
        }

        // Pull dos 3 repos em paralelo
        var taskRecibos    = PullRecibosAsync();
        var taskPesagens   = PullPesagensAsync();
        var taskBancoDados = PullBancoDadosAsync();

        await Task.WhenAll(taskRecibos, taskPesagens, taskBancoDados);

        // Verificação de vendas (conta PDFs em Recibos_Venda)
        await VerificarVendasAsync();

        UI(() => PodeContinuar = true);
    }

    private static string Tecnico(Exception ex, string classificacao) =>
        Regex.Replace($"{classificacao}\n\n--- Detalhe técnico ---\n{ex}", @"://[^@/\s]+@", "://***@");

    // Helper: marshal an action to the UI thread
    private static void UI(Action a) => Dispatcher.UIThread.Post(a);

    private async Task PullRecibosAsync()
    {
        try
        {
            await GitHubService.PullRecibosAsync(RootDir, msg => { });
            UI(() => ItemRecibos.Status = SyncStatus.Ok);
        }
        catch (Exception ex)
        {
            var msg = GitHubService.ClassificarErroGit(ex.Message, "pull de Recibos");
            var tec = Tecnico(ex, msg); UI(() => ItemRecibos.DefinirErro(tec));
        }
    }

    private async Task PullPesagensAsync()
    {
        try
        {
            await GitHubService.PullPesagensAsync(RootDir, msg => { });
            UI(() => ItemPesagens.Status = SyncStatus.Ok);
        }
        catch (Exception ex)
        {
            var msg = GitHubService.ClassificarErroGit(ex.Message, "pull de Pesagens");
            var tec = Tecnico(ex, msg); UI(() => ItemPesagens.DefinirErro(tec));
        }
    }

    private async Task PullBancoDadosAsync()
    {
        try
        {
            await GitHubService.PullBancoDadosAsync(RootDir, msg => { });
            UI(() => ItemBancoDados.Status = SyncStatus.Ok);
        }
        catch (Exception ex)
        {
            var msg = GitHubService.ClassificarErroGit(ex.Message, "pull de Banco de Dados");
            var tec = Tecnico(ex, msg); UI(() => ItemBancoDados.DefinirErro(tec));
        }
    }

    private Task VerificarVendasAsync()
    {
        try
        {
            var recibosDir = GitHubService.RecibosRepoDir(RootDir);
            var vendaDir   = Path.Combine(recibosDir, "Recibos_Venda");

            if (Directory.Exists(vendaDir))
                _ = Directory.GetFiles(vendaDir, "*.pdf", SearchOption.TopDirectoryOnly);

            UI(() => ItemVendas.Status = SyncStatus.Ok);
        }
        catch (Exception ex)
        {
            var tec = Tecnico(ex, "Verificação de recibos de venda"); UI(() => ItemVendas.DefinirErro(tec));
        }

        return Task.CompletedTask;
    }

    private static string FormatarData(string data)
    {
        if (string.IsNullOrWhiteSpace(data)) return string.Empty;
        // dd-MM-yyyy → dd/MM/yyyy
        return data.Replace("-", "/");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? p = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}

