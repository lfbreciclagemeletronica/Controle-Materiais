using ControleMateriais.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ControleMateriais.Desktop.ViewModels;

public class ReciboVendaItem
{
    public string  NomeCliente    { get; set; } = string.Empty;
    public decimal PesoTotal      { get; set; }
    public decimal ValorVenda     { get; set; }
    public string  DataCriacao    { get; set; } = string.Empty;
    public string  CaminhoCompleto{ get; set; } = string.Empty;
    public string  NomeArquivo    => Path.GetFileName(CaminhoCompleto);
    public string  PesoTotalStr   => PesoTotal.ToString("N3", CultureInfo.GetCultureInfo("pt-BR")) + " kg";
    public string  ValorVendaStr  => ValorVenda > 0
        ? ValorVenda.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))
        : string.Empty;
}

public class EstoqueItem
{
    public string  Material   { get; set; } = string.Empty;
    public decimal TotalKg    { get; set; }
    public string  TotalKgStr => TotalKg.ToString("N3", CultureInfo.GetCultureInfo("pt-BR")) + " kg";
}

public class EstoqueViewModel : ViewModelBase
{
    private const string EstoqueFileName = "estoque.json";
    private static readonly string[] CamposReservados = { "data", "status" };

    private readonly string _rootDir;
    public string RootDir => _rootDir;

    public ObservableCollection<EstoqueItem>    Itens        { get; } = new();
    public ObservableCollection<ReciboVendaItem> RecibosVenda { get; } = new();

    private bool _recibosVendaVazia = true;
    public bool RecibosVendaVazia
    {
        get => _recibosVendaVazia;
        private set { if (value != _recibosVendaVazia) { _recibosVendaVazia = value; OnPropertyChanged(); } }
    }

    private string _filtroNome = string.Empty;
    public string FiltroNome
    {
        get => _filtroNome;
        set { if (value != _filtroNome) { _filtroNome = value; OnPropertyChanged(); CarregarRecibosVenda(); } }
    }

    private string _filtroMes = string.Empty;
    public string FiltroMes
    {
        get => _filtroMes;
        set { if (value != _filtroMes) { _filtroMes = value; OnPropertyChanged(); CarregarRecibosVenda(); } }
    }

    public ICommand AtualizarCommand       { get; }
    public ICommand AbrirReciboVendaCommand { get; }
    public ICommand ExcluirReciboVendaCommand { get; }

    // Callback para confirmar exclusão
    public Func<string, Task<bool>>? ConfirmarExclusaoCallback { get; set; }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        private set { if (value != _status) { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusVisivel)); } }
    }
    public bool StatusVisivel => !string.IsNullOrEmpty(_status);

    private bool _sincronizando;
    public bool Sincronizando
    {
        get => _sincronizando;
        private set { if (value != _sincronizando) { _sincronizando = value; OnPropertyChanged(); } }
    }

    private bool _atualizando;
    public bool Atualizando
    {
        get => _atualizando;
        private set { if (value != _atualizando) { _atualizando = value; OnPropertyChanged(); } }
    }

    public ICommand SincronizarGitCommand { get; }
    public ICommand IrParaVendaCommand    { get; }

    public EstoqueViewModel(string rootDir, Action? irParaVendaAction = null)
    {
        _rootDir = rootDir;
        SincronizarGitCommand     = new DelegateCommand(() => _ = SincronizarGitAsync());
        IrParaVendaCommand        = new DelegateCommand(() => irParaVendaAction?.Invoke());
        AtualizarCommand          = new DelegateCommand(() => _ = AtualizarAsync());
        AbrirReciboVendaCommand   = new DelegateCommand<ReciboVendaItem?>(AbrirReciboVenda);
        ExcluirReciboVendaCommand = new DelegateCommand<ReciboVendaItem?>(r => _ = ExcluirReciboVendaAsync(r));
    }

    private static void AbrirReciboVenda(ReciboVendaItem? item)
    {
        if (item is null || !File.Exists(item.CaminhoCompleto)) return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.CaminhoCompleto) { UseShellExecute = true }); }
        catch { }
    }

    private async Task ExcluirReciboVendaAsync(ReciboVendaItem? item)
    {
        if (item is null) return;
        if (ConfirmarExclusaoCallback is not null)
        {
            var ok = await ConfirmarExclusaoCallback($"Excluir recibo \"{item.NomeArquivo}\"?");
            if (!ok) return;
        }
        try
        {
            if (File.Exists(item.CaminhoCompleto)) File.Delete(item.CaminhoCompleto);
            var meta = item.CaminhoCompleto + ".meta.json";
            if (File.Exists(meta)) File.Delete(meta);
            CarregarRecibosVenda();
        }
        catch (Exception ex) { Status = $"Erro ao excluir: {ex.Message}"; }
    }

    // ── Caminho do estoque.json ───────────────────────────────────────────
    private string EstoquePath => Path.Combine(GitHubService.BancoDadosRepoDir(_rootDir), EstoqueFileName);

    // ── Lê o estoque.json atual (ou dicionário vazio) ─────────────────────
    public static Dictionary<string, decimal> LerEstoque(string rootDir)
    {
        var path = Path.Combine(GitHubService.BancoDadosRepoDir(rootDir), EstoqueFileName);
        if (!File.Exists(path)) return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var obj = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
            if (obj is null) return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var dic = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in obj)
                dic[kvp.Key] = ExtrairDecimal(kvp.Value);
            return dic;
        }
        catch { return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); }
    }

    // ── Grava o estoque.json ──────────────────────────────────────────────
    public static void GravarEstoque(string rootDir, Dictionary<string, decimal> totais)
    {
        var dir = GitHubService.BancoDadosRepoDir(rootDir);
        Directory.CreateDirectory(dir);
        var obj = new JsonObject();
        foreach (var kv in totais.OrderBy(k => k.Key))
            obj[kv.Key] = JsonValue.Create(kv.Value);
        File.WriteAllText(
            Path.Combine(dir, EstoqueFileName),
            obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    // ── Extrai decimal de um JsonNode (suporta número ou string) ─────────
    private static decimal ExtrairDecimal(JsonNode? node)
    {
        if (node is null) return 0m;
        if (node is JsonValue jv)
        {
            if (jv.TryGetValue<decimal>(out var d)) return d;
            if (jv.TryGetValue<string>(out var s) &&
                decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dp)) return dp;
        }
        return 0m;
    }

    // ── Lê itens de peso de um JSON de recibo (ignora campos reservados) ──
    public static Dictionary<string, decimal> LerItensJson(string filePath)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var obj = JsonNode.Parse(File.ReadAllText(filePath))?.AsObject();
            if (obj is null) return result;
            foreach (var kvp in obj)
            {
                if (CamposReservados.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase)) continue;
                var peso = ExtrairDecimal(kvp.Value);
                if (peso > 0) result[kvp.Key] = peso;
            }
        }
        catch { }
        return result;
    }

    // ── Marca um JSON de recibo como "Adicionado ao estoque" ─────────────
    public static void MarcarComoAdicionado(string filePath)
    {
        try
        {
            var obj = JsonNode.Parse(File.ReadAllText(filePath))?.AsObject() ?? new JsonObject();
            obj["status"] = "Adicionado ao estoque";
            File.WriteAllText(filePath, obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    // ── Processa JSONs novos (sem status) → soma no estoque.json ─────────
    public int ProcessarNovosJsons()
    {
        var dir = GitHubService.BancoDadosRepoDir(_rootDir);
        if (!Directory.Exists(dir)) return 0;

        var totais   = LerEstoque(_rootDir);
        var contador = 0;

        foreach (var file in Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
        {
            var nome = Path.GetFileName(file);
            if (nome.Equals(EstoqueFileName, StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                var obj = JsonNode.Parse(File.ReadAllText(file))?.AsObject();
                if (obj is null) continue;

                // Pula se já foi processado
                if (obj.TryGetPropertyValue("status", out var st) &&
                    st?.GetValue<string>()?.Equals("Adicionado ao estoque", StringComparison.OrdinalIgnoreCase) == true)
                    continue;

                // Soma os itens
                var itens = LerItensJson(file);
                foreach (var kv in itens)
                    totais[kv.Key] = (totais.TryGetValue(kv.Key, out var atual) ? atual : 0m) + kv.Value;

                // Marca como processado
                MarcarComoAdicionado(file);
                contador++;
            }
            catch { }
        }

        if (contador > 0)
            GravarEstoque(_rootDir, totais);

        return contador;
    }

    // ── Recarrega a UI a partir do estoque.json ───────────────────────────
    public void Recarregar()
    {
        var dir = GitHubService.BancoDadosRepoDir(_rootDir);
        Directory.CreateDirectory(dir);

        // Processa JSONs novos antes de exibir
        ProcessarNovosJsons();

        // Lê estoque.json e constrói a lista com TODOS os itens do catálogo
        var totais = LerEstoque(_rootDir);

        Itens.Clear();
        foreach (var nome in ItemCatalog.OrderedItems)
        {
            totais.TryGetValue(nome, out var kg);
            Itens.Add(new EstoqueItem { Material = nome, TotalKg = kg });
        }

        // Itens que estão no estoque mas não no catálogo (nomes customizados)
        foreach (var kv in totais.OrderBy(k => k.Key))
        {
            if (!ItemCatalog.OrderedItems.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                Itens.Add(new EstoqueItem { Material = kv.Key, TotalKg = kv.Value });
        }

        Status = string.Empty;
        CarregarRecibosVenda();
    }

    // ── Lê PDFs de Recibos_Venda e popula a aba ───────────────────────────
    public void CarregarRecibosVenda()
    {
        RecibosVenda.Clear();
        var vendaDir = VendaViewModel.RecibosVendaDir(_rootDir);
        if (!Directory.Exists(vendaDir))
        {
            RecibosVendaVazia = true;
            return;
        }

        var filtroNomeLower = _filtroNome.Trim().ToLowerInvariant();
        // FiltroMes esperado formato "MM/yyyy" ou "MM-yyyy"
        string? filtroMesMes   = null;
        string? filtroMesAno   = null;
        if (!string.IsNullOrWhiteSpace(_filtroMes))
        {
            var partes = _filtroMes.Trim().Split('/', '-');
            if (partes.Length == 2) { filtroMesMes = partes[0].PadLeft(2,'0'); filtroMesAno = partes[1]; }
        }

        foreach (var file in Directory.GetFiles(vendaDir, "*.pdf", SearchOption.TopDirectoryOnly)
                                      .OrderByDescending(f => File.GetLastWriteTime(f)))
        {
            var semExt  = Path.GetFileNameWithoutExtension(file);
            var match   = System.Text.RegularExpressions.Regex.Match(semExt, @"^(.+?)_(\d{2}-\d{2}-\d{4})$");
            string nome = semExt.Replace("_", " ");
            string data = File.GetLastWriteTime(file).ToString("dd/MM/yyyy");
            decimal pesoTotal  = 0m;
            decimal valorVenda = 0m;

            // Lê .meta.json se existir
            var metaPath = file + ".meta.json";
            if (File.Exists(metaPath))
            {
                try
                {
                    var node = JsonNode.Parse(File.ReadAllText(metaPath));
                    if (node?["cliente"]    is JsonNode nc) nome       = nc.GetValue<string>();
                    if (node?["pesoTotal"]  is JsonNode np) pesoTotal  = np.GetValue<decimal>();
                    if (node?["valorVenda"] is JsonNode nv) valorVenda = nv.GetValue<decimal>();
                    if (node?["data"]       is JsonNode nd) data       = nd.GetValue<string>();
                }
                catch { }
            }
            else if (match.Success)
            {
                nome = match.Groups[1].Value.Replace("_", " ");
                if (DateTime.TryParseExact(match.Groups[2].Value, "dd-MM-yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    data = dt.ToString("dd/MM/yyyy");
            }

            // Filtro por nome
            if (!string.IsNullOrEmpty(filtroNomeLower) &&
                !nome.ToLowerInvariant().Contains(filtroNomeLower))
                continue;

            // Filtro por mês/ano
            if (filtroMesMes != null && filtroMesAno != null)
            {
                var partes = data.Split('/');
                if (partes.Length >= 3)
                {
                    if (partes[1].PadLeft(2,'0') != filtroMesMes || partes[2] != filtroMesAno)
                        continue;
                }
            }

            RecibosVenda.Add(new ReciboVendaItem
            {
                NomeCliente    = nome,
                PesoTotal      = pesoTotal,
                ValorVenda     = valorVenda,
                DataCriacao    = data,
                CaminhoCompleto = file
            });
        }

        RecibosVendaVazia = RecibosVenda.Count == 0;
    }

    // ── Pull Recibos_Venda do GitHub + recarrega lista ────────────────────
    private async Task AtualizarAsync()
    {
        if (Atualizando) return;
        Atualizando = true;
        Status = "Atualizando recibos de venda...";
        try
        {
            if (GitHubService.CredenciaisExistem(_rootDir))
            {
                await GitHubService.GarantirRecibosRepoAsync(_rootDir, msg => Status = msg);
                await GitHubService.SincronizarRecibosAsync(_rootDir, msg => Status = msg);
            }
            Recarregar();
            Status = "Recibos de venda atualizados.";
        }
        catch (Exception ex)
        {
            Status = $"Erro ao atualizar: {ex.Message}";
        }
        finally
        {
            Atualizando = false;
        }
    }

    // ── Pull Git + processa novos JSONs remotos ────────────────────────────
    private async Task SincronizarGitAsync()
    {
        if (Sincronizando) return;
        Sincronizando = true;
        Status = "Conectando ao repositório...";
        try
        {
            // Sincroniza repositório Recibos (inclui Recibos_Venda/)
            await GitHubService.GarantirRecibosRepoAsync(_rootDir, msg => Status = msg);
            await GitHubService.SincronizarRecibosAsync(_rootDir, msg => Status = msg);

            // Sincroniza repositório banco-de-dados
            await GitHubService.GarantirBancoDadosRepoAsync(_rootDir, msg => Status = msg);
            var novos = ProcessarNovosJsons();
            Status = novos > 0
                ? $"Sincronizado — {novos} recibo(s) novos processados."
                : "Sincronizado — estoque já atualizado.";

            // Publica estoque.json atualizado
            var conteudo = File.Exists(EstoquePath) ? await File.ReadAllTextAsync(EstoquePath) : "{}";
            await GitHubService.PublicarJsonBancoDadosAsync(_rootDir, EstoqueFileName, conteudo,
                msg => Status = msg);

            Recarregar();
        }
        catch (Exception ex)
        {
            Status = $"Erro: {ex.Message}";
        }
        finally
        {
            Sincronizando = false;
        }
    }
}
