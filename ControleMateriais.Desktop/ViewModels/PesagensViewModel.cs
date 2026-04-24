using ControleMateriais.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Input;

namespace ControleMateriais.Desktop.ViewModels;

public class PesagemItemPeso
{
    public string Nome { get; set; } = string.Empty;
    public decimal Peso { get; set; }
}

public class PesagemItem
{
    public string NomeArquivo   { get; set; } = string.Empty;
    public string Cliente       { get; set; } = string.Empty;
    public string HorarioRaw    { get; set; } = string.Empty;
    public string StatusPesagem { get; set; } = string.Empty;
    public List<PesagemItemPeso> Itens { get; set; } = new();
    public bool IsPendente  => StatusPesagem.Equals("pendente",  StringComparison.OrdinalIgnoreCase);
    public bool IsConcluido => StatusPesagem.Equals("concluido", StringComparison.OrdinalIgnoreCase);
    public bool IsFalhou    => StatusPesagem.Equals("falhou",    StringComparison.OrdinalIgnoreCase);

    public string Horario
    {
        get
        {
            if (string.IsNullOrEmpty(HorarioRaw)) return string.Empty;
            if (DateTime.TryParse(HorarioRaw, System.Globalization.CultureInfo.InvariantCulture,
                                  System.Globalization.DateTimeStyles.None, out var dt))
                return dt.ToString("dd/MM/yyyy");
            return HorarioRaw;
        }
    }
}

public class PesagensViewModel : ViewModelBase
{
    public string RootDir { get; }

    public ObservableCollection<PesagemItem> Pesagens { get; } = new();

    private string _filtroStatus = "todos";
    public string FiltroStatus
    {
        get => _filtroStatus;
        set
        {
            if (value != _filtroStatus)
            {
                _filtroStatus = value;
                OnPropertyChanged();
                AtualizarFiltro();
            }
        }
    }

    public ObservableCollection<PesagemItem> PesagensFiltradas { get; } = new();

    private void AtualizarFiltro()
    {
        PesagensFiltradas.Clear();
        var filtro = _filtroStatus;
        foreach (var p in Pesagens)
        {
            if (filtro == "todos" || p.StatusPesagem.Equals(filtro, StringComparison.OrdinalIgnoreCase))
                PesagensFiltradas.Add(p);
        }
        OnPropertyChanged(nameof(ListaVazia));
    }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        private set { if (value != _status) { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusVisivel)); } }
    }
    public bool StatusVisivel => !string.IsNullOrEmpty(_status);

    private bool _statusOk = true;
    public bool StatusOk
    {
        get => _statusOk;
        private set { if (value != _statusOk) { _statusOk = value; OnPropertyChanged(); } }
    }

    private bool _sincronizando;
    public bool Sincronizando
    {
        get => _sincronizando;
        private set { if (value != _sincronizando) { _sincronizando = value; OnPropertyChanged(); } }
    }

    public bool RepoPresenteLocal => Directory.Exists(Path.Combine(GitHubService.RepoDir(RootDir), ".git"));
    public bool ListaVazia => RepoPresenteLocal && PesagensFiltradas.Count == 0;

    public ICommand VoltarCommand      { get; }
    public ICommand SincronizarCommand { get; }
    public ICommand AbrirReciboCommand { get; }
    public ICommand CriarNovoCommand   { get; }

    public Func<Task>?               SolicitarConfiguracaoGitHubCallback { get; set; }
    public Action<PesagemItem>?       AbrirReciboCallback { get; set; }
    public Action?                    CriarNovoReciboCallback { get; set; }

    public PesagensViewModel(Action voltarCallback, string rootDir)
    {
        RootDir = rootDir;
        VoltarCommand      = new DelegateCommand(voltarCallback);
        SincronizarCommand = new DelegateCommand(() => _ = SincronizarAsync());
        AbrirReciboCommand = new DelegateCommand<PesagemItem>(item =>
        {
            if (item is not null)
                AbrirReciboCallback?.Invoke(item);
        });
        CriarNovoCommand   = new DelegateCommand(() => CriarNovoReciboCallback?.Invoke());
    }

    public void CarregarPesagens()
    {
        Pesagens.Clear();
        var repoDir = GitHubService.RepoDir(RootDir);
        if (!Directory.Exists(repoDir)) return;

        foreach (var file in Directory.GetFiles(repoDir, "*.json", SearchOption.TopDirectoryOnly)
                                      .OrderByDescending(f => File.GetLastWriteTime(f)))
        {
            try
            {
                var json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var statusPesagem = root.TryGetProperty("StatusPesagem", out var sp)
                    ? sp.GetString() ?? string.Empty
                    : string.Empty;

                var cliente = root.TryGetProperty("Cliente", out var cl) ? cl.GetString() ?? string.Empty : string.Empty;
                var horario = root.TryGetProperty("Horario", out var hr) ? hr.GetString() ?? string.Empty : string.Empty;
                var itensPeso = new List<PesagemItemPeso>();
                if (root.TryGetProperty("Itens", out var itensEl) && itensEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in itensEl.EnumerateArray())
                    {
                        var nome = el.TryGetProperty("Nome", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                        var peso = el.TryGetProperty("Peso", out var p) ? p.GetDecimal() : 0m;
                        itensPeso.Add(new PesagemItemPeso { Nome = nome, Peso = peso });
                    }
                }

                Pesagens.Add(new PesagemItem
                {
                    NomeArquivo   = Path.GetFileName(file),
                    Cliente       = cliente,
                    HorarioRaw    = horario,
                    StatusPesagem = statusPesagem,
                    Itens         = itensPeso
                });
            }
            catch { }
        }

        OnPropertyChanged(nameof(RepoPresenteLocal));
        AtualizarFiltro();
    }

    private async Task SincronizarAsync()
    {
        if (Sincronizando) return;
        Sincronizando = true;
        Status = string.Empty;

        try
        {
            // 1. Verificar credenciais
            if (!GitHubService.CredenciaisExistem(RootDir))
            {
                MostrarStatus("Configure as credenciais do GitHub na tela inicial.", ok: false);
                return;
            }

            // 2. Verificar/instalar git
            MostrarStatus("Verificando Git...", ok: true);
            if (!await GitHubService.GitDisponivel())
            {
                await GitHubService.InstalarGitAsync(msg => MostrarStatus(msg, ok: true));
            }

            var repoDir   = GitHubService.RepoDir(RootDir);
            var gitDir    = Path.Combine(repoDir, ".git");
            var creds     = GitHubService.CarregarCredenciais(RootDir)!;
            var remoteUrl = $"https://{creds.Token}@github.com/lfbreciclagemeletronica/Pesagens.git";

            // 3. Clone ou pull
            if (!Directory.Exists(gitDir))
            {
                MostrarStatus("Clonando repositório...", ok: true);
                Directory.CreateDirectory(repoDir);
                var r = await GitHubService.RunGit($"clone {remoteUrl} .", repoDir);
                if (r.exitCode != 0) throw new Exception($"Clone falhou: {r.stderr}");
                MostrarStatus("Clone concluído.", ok: true);
            }
            else
            {
                MostrarStatus("Atualizando repositório (pull)...", ok: true);
                await GitHubService.RunGit($"remote set-url origin {remoteUrl}", repoDir);
                var r = await GitHubService.RunGit("pull --rebase origin HEAD", repoDir);
                if (r.exitCode != 0) throw new Exception($"Pull falhou: {r.stderr}");
            }

            // 4. Configurar identidade git
            await GitHubService.RunGit($"config user.email \"{creds.GitEmail}\"", repoDir);
            await GitHubService.RunGit($"config user.name \"{creds.GitUsuario}\"", repoDir);

            // 5. Renomear arquivos JSON concluídos que ainda não têm sufixo _concluido
            var renomeados = 0;
            foreach (var file in Directory.GetFiles(repoDir, "*.json", SearchOption.TopDirectoryOnly))
            {
                var nomeArquivo = Path.GetFileName(file);
                if (nomeArquivo.Contains("_concluido", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("StatusPesagem", out var sp)) continue;
                    if (!"concluido".Equals(sp.GetString(), StringComparison.OrdinalIgnoreCase)) continue;

                    var semExt   = Path.GetFileNameWithoutExtension(nomeArquivo);
                    var novoNome = $"{semExt}_concluido.json";
                    var novoCaminho = Path.Combine(repoDir, novoNome);

                    // git mv para manter histórico
                    var mv = await GitHubService.RunGit($"mv \"{nomeArquivo}\" \"{novoNome}\"", repoDir);
                    if (mv.exitCode == 0)
                        renomeados++;
                }
                catch { }
            }

            // 6. Commit e push se houve renomeações
            if (renomeados > 0)
            {
                MostrarStatus($"Renomeando {renomeados} arquivo(s) concluído(s)...", ok: true);
                var commit = await GitHubService.RunGit(
                    $"commit -m \"Renomear {renomeados} pesagem(ns) concluída(s)\"", repoDir);
                if (commit.exitCode == 0)
                {
                    MostrarStatus("Enviando alterações ao GitHub...", ok: true);
                    await GitHubService.RunGit("push origin HEAD", repoDir);
                }
            }

            OnPropertyChanged(nameof(RepoPresenteLocal));
            CarregarPesagens();

            var pendentes = Pesagens.Count(p => p.IsPendente);
            MostrarStatus($"{pendentes} pendente(s) de {Pesagens.Count} pesagem(ns) encontrada(s).", ok: true);
        }
        catch (Exception ex)
        {
            MostrarStatus($"Erro: {ex.Message}", ok: false);
        }
        finally
        {
            Sincronizando = false;
        }
    }

    private void MostrarStatus(string mensagem, bool ok)
    {
        Status    = mensagem;
        StatusOk  = ok;
    }
}
