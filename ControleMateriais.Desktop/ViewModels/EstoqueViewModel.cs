using ControleMateriais.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
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
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _rootDir;
    public string RootDir => _rootDir;

    public ObservableCollection<EstoqueItem>    Itens        { get; } = new();
    public ObservableCollection<ReciboVendaItem> RecibosVenda { get; } = new();
    public ObservableCollection<string> EstoquesIniciaisDisponiveis { get; } = new();
    public bool SemEstoqueInicial => EstoquesIniciaisDisponiveis.Count == 0;

    private string _estoqueInicialSelecionado = string.Empty;
    private bool _recarregando = false;
    public string EstoqueInicialSelecionado
    {
        get => _estoqueInicialSelecionado;
        set
        {
            if (value != _estoqueInicialSelecionado)
            {
                _estoqueInicialSelecionado = value;
                OnPropertyChanged();
                if (!_recarregando)
                    Recarregar();
            }
        }
    }

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

    private bool _migrando;
    public bool Migrando
    {
        get => _migrando;
        private set { if (value != _migrando) { _migrando = value; OnPropertyChanged(); } }
    }

    public ICommand AtualizarCommand       { get; }
    public ICommand MigrarVendasCommand    { get; }
    public ICommand AtualizarEstoqueCommand { get; }
    public ICommand AbrirReciboVendaCommand { get; }
    public ICommand ExcluirReciboVendaCommand { get; }

    // Callback para confirmar exclusão
    public Func<string, Task<bool>>? ConfirmarExclusaoCallback { get; set; }

    // Callback para exibir modal de etapas concluídas
    public Func<List<string>, Task>? AbrirModalExclusaoCallback { get; set; }

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

    public ICommand SincronizarGitCommand        { get; }
    public ICommand IrParaVendaCommand           { get; }
    public ICommand IrParaEstoqueInicialCommand  { get; }

    public EstoqueViewModel(string rootDir, Action? irParaVendaAction = null, Action? irParaEstoqueInicialAction = null)
    {
        _rootDir = rootDir;
        SincronizarGitCommand        = new DelegateCommand(() => _ = SincronizarGitAsync());
        IrParaVendaCommand           = new DelegateCommand(() => irParaVendaAction?.Invoke());
        IrParaEstoqueInicialCommand  = new DelegateCommand(() => irParaEstoqueInicialAction?.Invoke());
        AtualizarCommand          = new DelegateCommand(() => _ = AtualizarAsync());
        MigrarVendasCommand       = new DelegateCommand(() => _ = MigrarVendasAsync());
        AtualizarEstoqueCommand    = new DelegateCommand(Recarregar);
        AbrirReciboVendaCommand   = new DelegateCommand<ReciboVendaItem?>(AbrirReciboVenda);
        ExcluirReciboVendaCommand = new DelegateCommand<ReciboVendaItem?>(r => _ = ExcluirReciboVendaAsync(r));
    }

    private static void AbrirReciboVenda(ReciboVendaItem? item)
    {
        if (item is null || !File.Exists(item.CaminhoCompleto)) return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.CaminhoCompleto) { UseShellExecute = true }); }
        catch { }
    }

    private async Task MigrarVendasAsync()
    {
        if (Migrando) return;
        Migrando = true;
        Status = "Migrando vendas...";
        try
        {
            await Task.Run(() => MigrarVendasParaJson.Migrar(_rootDir, msg => Avalonia.Threading.Dispatcher.UIThread.Post(() => Status = msg)));
            Status = "Migração concluída.";
            Recarregar();
        }
        catch (Exception ex)
        {
            Status = $"Erro na migração: {ex.Message}";
        }
        finally
        {
            Migrando = false;
        }
    }

    private async Task ExcluirReciboVendaAsync(ReciboVendaItem? item)
    {
        if (item is null) return;
        if (ConfirmarExclusaoCallback is not null)
        {
            var ok = await ConfirmarExclusaoCallback($"Excluir recibo \"{item.NomeArquivo}\"?");
            if (!ok) return;
        }
        var etapas = new List<string>();
        try
        {
            // 1. Extrair dados do meta.json
            string cliente = item.NomeCliente;
            string data = item.DataCriacao;
            var metaPath = item.CaminhoCompleto + ".meta.json";
            if (File.Exists(metaPath))
            {
                try
                {
                    var node = JsonNode.Parse(File.ReadAllText(metaPath));
                    if (node?["cliente"] is JsonNode nc) cliente = nc.GetValue<string>();
                    if (node?["data"] is JsonNode nd) data = nd.GetValue<string>();
                }
                catch { }
            }

            // 2. Remover registro do JSON de vendas (operação local)
            Status = $"Buscando: cliente='{cliente}', data='{data}'";
            bool jsonModificado = GitHubService.RemoverRegistroDoJson(_rootDir, "venda", cliente, data);

            if (jsonModificado)
            {
                var dataFmt = DateTime.ParseExact(data, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                var nomeJson = $"venda-{dataFmt}.json";
                var jsonPath = Path.Combine(GitHubService.BancoDadosRepoDir(_rootDir), nomeJson);

                if (File.Exists(jsonPath))
                {
                    // Arquivo ainda tem outros registros → commit com versão atualizada
                    var conteudo = await File.ReadAllTextAsync(jsonPath);
                    await GitHubService.CommitJsonBancoDadosAsync(_rootDir, nomeJson, conteudo, msg => Status = msg);
                    etapas.Add("Registro removido do banco de dados");
                    etapas.Add("Banco de dados atualizado localmente");
                }
                else
                {
                    // Era o único registro → commit de remoção
                    await GitHubService.CommitRemoverJsonBancoDadosAsync(_rootDir, nomeJson, msg => Status = msg);
                    etapas.Add("Registro removido do banco de dados");
                    etapas.Add("Arquivo do banco de dados removido localmente");
                }
            }
            else
            {
                etapas.Add("Nenhum registro encontrado no banco de dados");
            }

            // 3. Deletar PDF e meta.json
            if (File.Exists(item.CaminhoCompleto)) File.Delete(item.CaminhoCompleto);
            if (File.Exists(metaPath)) File.Delete(metaPath);
            etapas.Add("Recibo PDF excluído");

            // 4. Recarregar para recalcular estoque
            Recarregar();
            etapas.Add("Estoque recalculado");

            Status = string.Empty;

            // 5. Abrir modal com etapas concluídas
            if (AbrirModalExclusaoCallback is not null)
                await AbrirModalExclusaoCallback(etapas);
        }
        catch (Exception ex) { Status = $"Erro ao excluir: {ex.Message}"; }
    }

    // ── Caminho do estoque-inicial-MM-YYYY.json (dinâmico baseado na seleção) ──
    private string EstoqueInicialPath
    {
        get
        {
            if (string.IsNullOrEmpty(EstoqueInicialSelecionado))
                return string.Empty;
            return Path.Combine(GitHubService.BancoDadosRepoDir(_rootDir), $"{EstoqueInicialSelecionado}.json");
        }
    }

    // ── Carrega lista de estoques iniciais disponíveis ───────────────────────
    private void CarregarEstoquesIniciaisDisponiveis()
    {
        EstoquesIniciaisDisponiveis.Clear();
        var dir = GitHubService.BancoDadosRepoDir(_rootDir);
        if (!Directory.Exists(dir)) return;

        var arquivos = Directory.GetFiles(dir, "estoque-inicial-*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(f => !string.IsNullOrEmpty(f))
            .OrderByDescending(f => f)
            .ToList();

        foreach (var arquivo in arquivos)
            if (arquivo is not null)
                EstoquesIniciaisDisponiveis.Add(arquivo);

        // Auto-selecionar apenas se ainda não foi selecionado nada
        if (string.IsNullOrEmpty(EstoqueInicialSelecionado) && EstoquesIniciaisDisponiveis.Any())
        {
            var mesAtual = DateTime.Now.ToString("estoque-inicial-MM-yyyy");
            if (EstoquesIniciaisDisponiveis.Contains(mesAtual))
                EstoqueInicialSelecionado = mesAtual;
            else
                EstoqueInicialSelecionado = EstoquesIniciaisDisponiveis.First();
        }

        OnPropertyChanged(nameof(SemEstoqueInicial));
    }

    // Método público para carregar a lista de estoques iniciais (chamado pela View)
    public void CarregarListaEstoquesIniciais()
    {
        CarregarEstoquesIniciaisDisponiveis();
    }

    // ── Lê estoque-inicial-MM-YYYY.json ────────────────────────────────────────
    private Dictionary<string, decimal> LerEstoqueInicial()
    {
        var path = EstoqueInicialPath;
        if (string.IsNullOrEmpty(path))
        {
            Status = "Nenhum estoque inicial encontrado. Clique em Sincronizar ou acesse Estoque Inicial.";
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }
        if (!File.Exists(path))
        {
            Status = "Não existe estoque inicial para o mês/ano selecionado.";
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }
        try
        {
            var obj = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
            if (obj is null) return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var dic = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in obj)
            {
                if (kvp.Key.Equals("data", StringComparison.OrdinalIgnoreCase)) continue;
                dic[kvp.Key] = ExtrairDecimal(kvp.Value);
            }
            return dic;
        }
        catch { return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); }
    }

    // ── Lê arquivo mensal compra-MM-YYYY.json (apenas mês atual ou especificado) ───────────────
    private Dictionary<string, decimal> LerMesAtual(string? mesAno = null)
    {
        var dir = GitHubService.BancoDadosRepoDir(_rootDir);
        if (!Directory.Exists(dir)) return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var chave = mesAno ?? DateTime.Now.ToString("MM-yyyy"); // ex: "06-2026"
        var path = Path.Combine(dir, $"compra-{chave}.json");

        if (!File.Exists(path)) return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var obj = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
            if (obj is null || !obj.ContainsKey("registros")) return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var dic = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var registros = obj["registros"]!.AsArray();
            foreach (var reg in registros)
            {
                if (reg is JsonObject regObj && regObj.ContainsKey("materiais"))
                {
                    var materiais = regObj["materiais"]!.AsArray();
                    foreach (var mat in materiais)
                    {
                        if (mat is JsonObject matObj && matObj.ContainsKey("descricao") && matObj.ContainsKey("peso"))
                        {
                            var nome = matObj["descricao"]!.GetValue<string>();
                            var peso = ExtrairDecimal(matObj["peso"]);
                            dic[nome] = dic.TryGetValue(nome, out var atual) ? atual + peso : peso;
                        }
                    }
                }
            }
            return dic;
        }
        catch { return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); }
    }

    // ── Lê todos os arquivos venda-DD-MM-YYYY.json (ou filtra por mês/ano específico) ─────────────────────────
    private Dictionary<string, decimal> LerVendas(string? mesAno = null)
    {
        var dir = GitHubService.BancoDadosRepoDir(_rootDir);
        if (!Directory.Exists(dir)) return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var dic = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var pattern = string.IsNullOrEmpty(mesAno) ? "venda-*.json" : $"venda-*-{mesAno}.json";
        foreach (var file in Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly))
        {
            try
            {
                var obj = JsonNode.Parse(File.ReadAllText(file))?.AsObject();
                if (obj is null || !obj.ContainsKey("registros")) continue;

                var registros = obj["registros"]!.AsArray();
                foreach (var reg in registros)
                {
                    if (reg is JsonObject regObj && regObj.ContainsKey("materiais"))
                    {
                        var materiais = regObj["materiais"]!.AsArray();
                        foreach (var mat in materiais)
                        {
                            if (mat is JsonObject matObj && matObj.ContainsKey("descricao") && matObj.ContainsKey("peso"))
                            {
                                var nome = matObj["descricao"]!.GetValue<string>();
                                var peso = ExtrairDecimal(matObj["peso"]);
                                dic[nome] = dic.TryGetValue(nome, out var atual) ? atual + peso : peso;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        return dic;
    }

    // ── Calcula estoque atual: vendas - (compras + estoque inicial) ───────────────────
    private Dictionary<string, decimal> CalcularEstoqueAtual()
    {
        // Extrair mês/ano do estoque inicial selecionado
        string mesAno = "01-2026"; // padrão
        if (!string.IsNullOrEmpty(EstoqueInicialSelecionado) &&
            EstoqueInicialSelecionado.StartsWith("estoque-inicial-"))
        {
            mesAno = EstoqueInicialSelecionado.Replace("estoque-inicial-", "");
        }

        // Começa com vendas (positivo)
        var totais = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var vendas = LerVendas(mesAno);
        foreach (var kv in vendas)
            totais[kv.Key] = kv.Value;

        // Subtrai compras do mesmo mês
        var mesAtual = LerMesAtual(mesAno);
        foreach (var kv in mesAtual)
            totais[kv.Key] = totais.TryGetValue(kv.Key, out var atual) ? atual - kv.Value : -kv.Value;

        // Subtrai estoque inicial
        var inicial = LerEstoqueInicial();
        foreach (var kv in inicial)
            totais[kv.Key] = totais.TryGetValue(kv.Key, out var atual) ? atual - kv.Value : -kv.Value;

        // Remove itens com zero
        foreach (var key in totais.Keys.ToList())
            if (totais[key] == 0m) totais.Remove(key);

        return totais;
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

    // ── Recarrega a UI usando o novo cálculo de estoque ───────────────────
    public void Recarregar()
    {
        var totais = CalcularEstoqueAtual();

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
    public async Task SincronizarGitAsync()
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
            Status = "Sincronizado — estoque atualizado.";

            CarregarListaEstoquesIniciais();
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
