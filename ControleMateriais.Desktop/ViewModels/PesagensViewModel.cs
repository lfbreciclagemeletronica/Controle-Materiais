using ControleMateriais.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

public class ReciboItem
{
    public string NomeArquivo  { get; set; } = string.Empty;
    public string CaminhoCompleto { get; set; } = string.Empty;
    public string DataCriacao  { get; set; } = string.Empty;
    public DateTime DataCriacaoRaw { get; set; }
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

    public int ContPendente  => Pesagens.Count(p => p.IsPendente);
    public int ContConcluido => Pesagens.Count(p => p.IsConcluido);
    public int ContFalhou    => Pesagens.Count(p => p.IsFalhou);

    public string LabelPendente  => $"pendente ({ContPendente})";
    public string LabelConcluido => $"concluido ({ContConcluido})";
    public string LabelFalhou    => $"falhou ({ContFalhou})";

    private void AtualizarFiltro()
    {
        PesagensFiltradas.Clear();
        var filtro = _filtroStatus;

        // Deduplicar: para cada cliente, manter só a pesagem mais recente
        var pesagensDedup = Pesagens
            .GroupBy(p => p.Cliente.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(p => p.HorarioRaw).First())
            .OrderByDescending(p => p.HorarioRaw)
            .ToList();

        foreach (var p in pesagensDedup)
        {
            if (filtro == "todos" || p.StatusPesagem.Equals(filtro, StringComparison.OrdinalIgnoreCase))
                PesagensFiltradas.Add(p);
        }
        OnPropertyChanged(nameof(ListaVazia));
        OnPropertyChanged(nameof(ContPendente));
        OnPropertyChanged(nameof(ContConcluido));
        OnPropertyChanged(nameof(ContFalhou));
        OnPropertyChanged(nameof(LabelPendente));
        OnPropertyChanged(nameof(LabelConcluido));
        OnPropertyChanged(nameof(LabelFalhou));
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

    // ── Recibos PDFs ──────────────────────────────────────────────
    public ObservableCollection<ReciboItem> Recibos { get; } = new();

    private bool _sincronizandoRecibos;
    public bool SincronizandoRecibos
    {
        get => _sincronizandoRecibos;
        private set { if (value != _sincronizandoRecibos) { _sincronizandoRecibos = value; OnPropertyChanged(); } }
    }

    private string _statusRecibos = string.Empty;
    public string StatusRecibos
    {
        get => _statusRecibos;
        private set { if (value != _statusRecibos) { _statusRecibos = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusRecibosVisivel)); } }
    }
    public bool StatusRecibosVisivel => !string.IsNullOrEmpty(_statusRecibos);

    private bool _statusRecibosOk = true;
    public bool StatusRecibosOk
    {
        get => _statusRecibosOk;
        private set { if (value != _statusRecibosOk) { _statusRecibosOk = value; OnPropertyChanged(); } }
    }

    private string _ultimaSincPesagens = string.Empty;
    public string UltimaSincPesagens
    {
        get => _ultimaSincPesagens;
        private set { if (value != _ultimaSincPesagens) { _ultimaSincPesagens = value; OnPropertyChanged(); OnPropertyChanged(nameof(UltimaSincPesagensVisivel)); } }
    }
    public bool UltimaSincPesagensVisivel => !string.IsNullOrEmpty(_ultimaSincPesagens);

    private string _ultimaSincRecibos = string.Empty;
    public string UltimaSincRecibos
    {
        get => _ultimaSincRecibos;
        private set { if (value != _ultimaSincRecibos) { _ultimaSincRecibos = value; OnPropertyChanged(); OnPropertyChanged(nameof(UltimaSincRecibosVisivel)); } }
    }
    public bool UltimaSincRecibosVisivel => !string.IsNullOrEmpty(_ultimaSincRecibos);

    public bool RepoPresenteLocal => Directory.Exists(Path.Combine(GitHubService.RepoDir(RootDir), ".git"));
    public bool ListaVazia => RepoPresenteLocal && PesagensFiltradas.Count == 0;

    public bool RecibosDirPresente => Directory.Exists(Path.Combine(GitHubService.RecibosRepoDir(RootDir), ".git"));
    public bool RecibosListaVazia  => RecibosDirPresente && Recibos.Count == 0;

    public ICommand VoltarCommand          { get; }
    public ICommand SincronizarCommand     { get; }
    public ICommand SincronizarRecibosCommand { get; }
    public ICommand AbrirReciboCommand     { get; }
    public ICommand CriarNovoCommand       { get; }
    public ICommand AbrirPdfCommand        { get; }

    public Func<Task>?               SolicitarConfiguracaoGitHubCallback { get; set; }
    public Action<PesagemItem>?       AbrirReciboCallback { get; set; }
    public Action?                    CriarNovoReciboCallback { get; set; }

    public PesagensViewModel(Action voltarCallback, string rootDir)
    {
        RootDir = rootDir;
        VoltarCommand             = new DelegateCommand(voltarCallback);
        SincronizarCommand        = new DelegateCommand(() => _ = SincronizarAsync());
        SincronizarRecibosCommand = new DelegateCommand(() => _ = SincronizarRecibosAsync());
        AbrirReciboCommand        = new DelegateCommand<PesagemItem>(item =>
        {
            if (item is not null)
                AbrirReciboCallback?.Invoke(item);
        });
        CriarNovoCommand  = new DelegateCommand(() => CriarNovoReciboCallback?.Invoke());
        AbrirPdfCommand   = new DelegateCommand<ReciboItem>(item =>
        {
            if (item is null) return;
            try { Process.Start(new ProcessStartInfo(item.CaminhoCompleto) { UseShellExecute = true }); }
            catch { }
        });
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
                await GitHubService.RunGit("fetch origin main", repoDir);
                var r = await GitHubService.RunGit("rebase origin/main", repoDir);
                if (r.exitCode != 0) throw new Exception($"Pull falhou: {r.stderr}");
            }

            // 4. Configurar identidade git
            await GitHubService.RunGit($"config user.email \"{creds.GitEmail}\"", repoDir);
            await GitHubService.RunGit($"config user.name \"{creds.GitUsuario}\"", repoDir);

            // 5. Renomear arquivos JSON concluídos que ainda não têm sufixo _concluido
            var renomeados = new List<(string novoNome, string cliente)>();
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

                    var clienteNome = root.TryGetProperty("Cliente", out var cl) ? cl.GetString() ?? string.Empty : string.Empty;

                    var semExt   = Path.GetFileNameWithoutExtension(nomeArquivo);
                    var novoNome = $"{semExt}_concluido.json";

                    var mv = await GitHubService.RunGit($"mv \"{nomeArquivo}\" \"{novoNome}\"", repoDir);
                    if (mv.exitCode == 0)
                        renomeados.Add((novoNome, clienteNome));
                }
                catch { }
            }

            // 6. Commit individual por pesagem e push único ao final
            if (renomeados.Count > 0)
            {
                foreach (var (novoNome, cliente) in renomeados)
                {
                    MostrarStatus($"Commitando: {novoNome}...", ok: true);
                    var msgCommit = string.IsNullOrWhiteSpace(cliente)
                        ? $"Pesagem concluída: {novoNome}"
                        : $"{cliente} - pesagem concluída";
                    await GitHubService.RunGit($"add \"{novoNome}\"", repoDir);
                    await GitHubService.RunGit($"commit -m \"{msgCommit}\"", repoDir);
                }
                MostrarStatus("Enviando alterações ao GitHub...", ok: true);
                await GitHubService.RunGit("push origin main", repoDir);
            }

            OnPropertyChanged(nameof(RepoPresenteLocal));
            CarregarPesagens();

            UltimaSincPesagens = $"Última sincronização: {DateTime.Now:dd/MM/yyyy HH:mm}";
            MostrarStatus(string.Empty, ok: true);
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

    public void CarregarRecibos()
    {
        Recibos.Clear();
        var repoDir = GitHubService.RecibosRepoDir(RootDir);
        if (!Directory.Exists(repoDir)) { OnPropertyChanged(nameof(RecibosDirPresente)); OnPropertyChanged(nameof(RecibosListaVazia)); return; }

        foreach (var file in Directory.GetFiles(repoDir, "*.pdf", SearchOption.TopDirectoryOnly)
                                      .OrderByDescending(f => File.GetLastWriteTime(f)))
        {
            var semExt = Path.GetFileNameWithoutExtension(file);
            var (nomeCliente, dataExibicao, dataRaw) = ParsearNomeArquivoRecibo(semExt, file);

            Recibos.Add(new ReciboItem
            {
                NomeArquivo     = nomeCliente,
                CaminhoCompleto = file,
                DataCriacaoRaw  = dataRaw,
                DataCriacao     = dataExibicao
            });
        }

        OnPropertyChanged(nameof(RecibosDirPresente));
        OnPropertyChanged(nameof(RecibosListaVazia));
    }

    // Extrai nome do cliente e data de um nome de arquivo no padrão:
    // NomeCliente_dd-MM-yyyy_HH-mm   (underscores separam data e hora)
    // ou NomeCliente_dd-MM-yyyy (sem hora)
    private static (string nome, string dataExibicao, DateTime dataRaw) ParsearNomeArquivoRecibo(string semExt, string filePath)
    {
        // Padrão: termina com _dd-MM-yyyy_HH-mm
        var matchComHora = System.Text.RegularExpressions.Regex.Match(
            semExt, @"^(.+?)_(\d{2}-\d{2}-\d{4}_\d{2}-\d{2})$");
        if (matchComHora.Success)
        {
            var nomeRaw = matchComHora.Groups[1].Value;
            var dataStr = matchComHora.Groups[2].Value; // dd-MM-yyyy_HH-mm
            if (DateTime.TryParseExact(dataStr, "dd-MM-yyyy_HH-mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt))
                return (nomeRaw, dt.ToString("dd/MM/yyyy"), dt);
        }

        // Padrão sem hora: termina com _dd-MM-yyyy
        var matchSemHora = System.Text.RegularExpressions.Regex.Match(
            semExt, @"^(.+?)_(\d{2}-\d{2}-\d{4})$");
        if (matchSemHora.Success)
        {
            var nomeRaw = matchSemHora.Groups[1].Value;
            var dataStr = matchSemHora.Groups[2].Value;
            if (DateTime.TryParseExact(dataStr, "dd-MM-yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt))
                return (nomeRaw, dt.ToString("dd/MM/yyyy"), dt);
        }

        // Fallback: usa nome completo e data do sistema de arquivos
        var dtFallback = File.GetLastWriteTime(filePath);
        return (semExt, dtFallback.ToString("dd/MM/yyyy"), dtFallback);
    }

    private async Task SincronizarRecibosAsync()
    {
        if (SincronizandoRecibos) return;
        SincronizandoRecibos = true;
        MostrarStatusRecibos("Verificando Git...", ok: true);

        try
        {
            if (!GitHubService.CredenciaisExistem(RootDir))
            {
                MostrarStatusRecibos("Configure as credenciais do GitHub na tela inicial.", ok: false);
                return;
            }

            if (!await GitHubService.GitDisponivel())
                await GitHubService.InstalarGitAsync(msg => MostrarStatusRecibos(msg, ok: true));

            await GitHubService.SincronizarRecibosAsync(RootDir, msg => MostrarStatusRecibos(msg, ok: true));

            CarregarRecibos();
            UltimaSincRecibos = $"Última sincronização: {DateTime.Now:dd/MM/yyyy HH:mm}";
            MostrarStatusRecibos(string.Empty, ok: true);
        }
        catch (Exception ex)
        {
            MostrarStatusRecibos($"Erro: {ex.Message}", ok: false);
        }
        finally
        {
            SincronizandoRecibos = false;
        }
    }

    private void MostrarStatus(string mensagem, bool ok)
    {
        Status   = mensagem;
        StatusOk = ok;
    }

    private void MostrarStatusRecibos(string mensagem, bool ok)
    {
        StatusRecibos    = mensagem;
        StatusRecibosOk  = ok;
    }
}
