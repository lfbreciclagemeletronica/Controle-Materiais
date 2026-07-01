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

public class PesoInicialWrapper : ViewModelBase
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

    public string Nome { get; }

    public event EventHandler? PesoChanged;

    private decimal _peso;
    public decimal Peso
    {
        get => _peso;
        set { if (value != _peso) { _peso = value; OnPropertyChanged(); PesoChanged?.Invoke(this, EventArgs.Empty); } }
    }

    private string _pesoTexto = "0,000";
    public string PesoTexto
    {
        get => _pesoTexto;
        set { if (value != _pesoTexto) { _pesoTexto = value; OnPropertyChanged(); } }
    }

    private bool _editando;
    private string _pesoTextoAnterior = string.Empty;

    public PesoInicialWrapper(string nome, decimal peso = 0m)
    {
        Nome = nome;
        _peso = peso;
        _pesoTexto = peso.ToString("N3", PtBR);
    }

    public void IniciarEdicao()
    {
        _pesoTextoAnterior = _pesoTexto;
        _editando = true;
        PesoTexto = string.Empty;
    }

    public void CancelarEdicao()
    {
        if (!_editando) return;
        _editando = false;
        PesoTexto = _pesoTextoAnterior;
    }

    public void ConfirmarEdicao()
    {
        if (!_editando) return;
        _editando = false;
        var raw = PesoTexto.Trim().Replace(" ", "");
        if (raw.Contains(',') && raw.Contains('.'))
            raw = raw.Replace(".", "").Replace(",", ".");
        else
            raw = raw.Replace(",", ".");
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            parsed = _peso;
        Peso = parsed;
        PesoTexto = parsed.ToString("N3", PtBR);
    }
}

public class ItemPersonalizadoInicial : ViewModelBase
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

    public event EventHandler? PesoChanged;

    private string _nome = string.Empty;
    public string Nome
    {
        get => _nome;
        set { if (value != _nome) { _nome = value; OnPropertyChanged(); } }
    }

    private decimal _peso;
    public decimal Peso
    {
        get => _peso;
        private set { if (value != _peso) { _peso = value; OnPropertyChanged(); PesoChanged?.Invoke(this, EventArgs.Empty); } }
    }

    private string _pesoTexto = "0,000";
    public string PesoTexto
    {
        get => _pesoTexto;
        set { if (value != _pesoTexto) { _pesoTexto = value; OnPropertyChanged(); } }
    }

    private bool _editando;
    private string _pesoTextoAnterior = string.Empty;

    public void IniciarEdicao()
    {
        _pesoTextoAnterior = _pesoTexto;
        _editando = true;
        PesoTexto = string.Empty;
    }

    public void CancelarEdicao()
    {
        if (!_editando) return;
        _editando = false;
        PesoTexto = _pesoTextoAnterior;
    }

    public void ConfirmarEdicao()
    {
        if (!_editando) return;
        _editando = false;
        var raw = PesoTexto.Trim().Replace(" ", "");
        if (raw.Contains(',') && raw.Contains('.'))
            raw = raw.Replace(".", "").Replace(",", ".");
        else
            raw = raw.Replace(",", ".");
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            parsed = _peso;
        Peso = parsed;
        PesoTexto = parsed.ToString("N3", PtBR);
    }

    public void Zerar()
    {
        _editando = false;
        Nome = string.Empty;
        Peso = 0m;
        PesoTexto = "0,000";
    }
}

public class EstoqueInicialViewModel : ViewModelBase
{
    private const string LogFileName = "modificacao-estoque-inicial.log";

    private readonly string _rootDir;
    private Action? _voltarCallback;

    public ObservableCollection<PesoInicialWrapper> Itens { get; } = new();
    public ObservableCollection<ItemPersonalizadoInicial> ItensPersonalizados { get; } = new();

    private decimal _impurezasPesoAtual;
    public decimal ImpurezasPesoAtual
    {
        get => _impurezasPesoAtual;
        private set { if (value != _impurezasPesoAtual) { _impurezasPesoAtual = value; OnPropertyChanged(); RecalcularTotal(); } }
    }

    private string _impurezasPesoTexto = "0,000";
    public string ImpurezasPesoTexto
    {
        get => _impurezasPesoTexto;
        set { if (value != _impurezasPesoTexto) { _impurezasPesoTexto = value; OnPropertyChanged(); } }
    }

    private bool _impurezasEditando;
    private string _impurezasPesoTextoAnterior = string.Empty;

    public void IniciarEdicaoImpurezas()
    {
        _impurezasPesoTextoAnterior = _impurezasPesoTexto;
        _impurezasEditando = true;
        ImpurezasPesoTexto = string.Empty;
    }

    public void CancelarEdicaoImpurezas()
    {
        if (!_impurezasEditando) return;
        _impurezasEditando = false;
        ImpurezasPesoTexto = _impurezasPesoTextoAnterior;
    }

    public void ConfirmarEdicaoImpurezas()
    {
        if (!_impurezasEditando) return;
        _impurezasEditando = false;
        var raw = ImpurezasPesoTexto.Trim().Replace(" ", "");
        if (raw.Contains(',') && raw.Contains('.'))
            raw = raw.Replace(".", "").Replace(",", ".");
        else
            raw = raw.Replace(",", ".");
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            parsed = _impurezasPesoAtual;
        ImpurezasPesoAtual = parsed;
        ImpurezasPesoTexto = parsed.ToString("N3", CultureInfo.GetCultureInfo("pt-BR"));
    }

    private decimal _pesoTotalGeral;
    public decimal PesoTotalGeral
    {
        get => _pesoTotalGeral;
        private set { if (value != _pesoTotalGeral) { _pesoTotalGeral = value; OnPropertyChanged(); } }
    }

    private void RecalcularTotal()
    {
        var soma = Itens.Sum(i => i.Peso)
                  + ItensPersonalizados.Sum(c => c.Peso)
                  + _impurezasPesoAtual;
        PesoTotalGeral = soma;
    }

    private int _mesIndex = DateTime.Now.Month - 1;
    public int MesIndex
    {
        get => _mesIndex;
        set { if (value != _mesIndex && value >= 0 && value <= 11) { _mesIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(MesSelecionado)); } }
    }
    public int MesSelecionado
    {
        get => _mesIndex + 1;
        set { MesIndex = Math.Clamp(value, 1, 12) - 1; }
    }

    private int _anoSelecionado = DateTime.Now.Year;
    public int AnoSelecionado
    {
        get => _anoSelecionado;
        set { if (value != _anoSelecionado) { _anoSelecionado = value; OnPropertyChanged(); } }
    }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        private set { if (value != _status) { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusVisivel)); } }
    }
    public bool StatusVisivel => !string.IsNullOrEmpty(_status);

    private bool _salvando;
    public bool Salvando
    {
        get => _salvando;
        private set { if (value != _salvando) { _salvando = value; OnPropertyChanged(); } }
    }

    private bool _registrando;
    public bool Registrando
    {
        get => _registrando;
        private set { if (value != _registrando) { _registrando = value; OnPropertyChanged(); } }
    }

    public ICommand SalvarCommand          { get; }
    public ICommand RegistrarEstoqueCommand { get; }
    public ICommand VoltarCommand           { get; }

    public EstoqueInicialViewModel(string rootDir, Action? voltarCallback = null)
    {
        _rootDir        = rootDir;
        _voltarCallback = voltarCallback;

        SalvarCommand           = new DelegateCommand(() => _ = SalvarAsync());
        RegistrarEstoqueCommand = new DelegateCommand(() => _ = RegistrarEstoqueAsync());
        VoltarCommand           = new DelegateCommand(() => _voltarCallback?.Invoke());

        for (int i = 0; i < 4; i++)
        {
            var custom = new ItemPersonalizadoInicial();
            custom.PesoChanged += (_, __) => RecalcularTotal();
            ItensPersonalizados.Add(custom);
        }

        Carregar();
    }

    private string EstoqueInicialPath => Path.Combine(GitHubService.BancoDadosRepoDir(_rootDir), $"estoque-inicial-{MesSelecionado:D2}-{AnoSelecionado}.json");
    private string LogPath            => Path.Combine(GitHubService.BancoDadosRepoDir(_rootDir), LogFileName);

    public void Carregar()
    {
        Itens.Clear();

        var pesos = LerEstoqueInicial();

        foreach (var nome in ItemCatalog.OrderedItems)
        {
            pesos.TryGetValue(nome, out var kg);
            var wrapper = new PesoInicialWrapper(nome, kg);
            wrapper.PesoChanged += (_, __) => RecalcularTotal();
            Itens.Add(wrapper);
        }

        foreach (var c in ItensPersonalizados) c.Zerar();
        ImpurezasPesoAtual = 0m;
        ImpurezasPesoTexto = "0,000";
        RecalcularTotal();
    }

    private System.Collections.Generic.Dictionary<string, decimal> LerEstoqueInicial()
    {
        var result = new System.Collections.Generic.Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var path = EstoqueInicialPath;
        if (!File.Exists(path)) return result;
        try
        {
            var obj = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
            if (obj is null) return result;

            // Não lê "data" para não sobrescrever o mês/ano selecionado na tela
            foreach (var kvp in obj)
            {
                if (kvp.Key.Equals("data", StringComparison.OrdinalIgnoreCase)) continue;
                if (kvp.Value is JsonValue jv)
                {
                    if (jv.TryGetValue<decimal>(out var d)) result[kvp.Key] = d;
                    else if (jv.TryGetValue<string>(out var s) &&
                             decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dp))
                        result[kvp.Key] = dp;
                }
            }
        }
        catch { }
        return result;
    }

    private async Task SalvarAsync()
    {
        if (Salvando) return;
        Salvando = true;
        Status = "Salvando estoque inicial...";
        try
        {
            GravarEstoqueInicialLocal();
            AppendLog("Salvar estoque-inicial");

            await GitHubService.GarantirBancoDadosRepoAsync(_rootDir, msg => Status = msg);
            var conteudo = await File.ReadAllTextAsync(EstoqueInicialPath);
            var nomeArquivo = Path.GetFileName(EstoqueInicialPath);
            await GitHubService.PublicarJsonBancoDadosAsync(_rootDir, nomeArquivo, conteudo, msg => Status = msg);

            await PublicarLog();

            Status = "Estoque inicial salvo com sucesso.";
        }
        catch (Exception ex)
        {
            Status = $"Erro ao salvar: {ex.Message}";
        }
        finally
        {
            Salvando = false;
        }
    }

    private async Task RegistrarEstoqueAsync()
    {
        if (Registrando) return;
        Registrando = true;
        Status = "Registrando estoque final...";
        try
        {
            var mes = MesSelecionado;
            var ano = AnoSelecionado;
            var nomeFinal = $"estoque-final-{mes:D2}-{ano}.json";

            // Snapshot do estoque atual (inicial + mês atual - vendas)
            var estoqueAtual = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var bancoDadosDir = GitHubService.BancoDadosRepoDir(_rootDir);
            var estoqueInicialPath = EstoqueInicialPath;

            // Lê estoque-inicial-MM-AAAA.json
            if (File.Exists(estoqueInicialPath))
            {
                try
                {
                    var obj = JsonNode.Parse(File.ReadAllText(estoqueInicialPath))?.AsObject();
                    if (obj is not null)
                    {
                        foreach (var kvp in obj)
                        {
                            if (kvp.Key.Equals("data", StringComparison.OrdinalIgnoreCase)) continue;
                            if (kvp.Value is JsonValue jv && jv.TryGetValue<decimal>(out var d))
                                estoqueAtual[kvp.Key] = d;
                        }
                    }
                }
                catch { }
            }

            // Lê mês atual MM-YYYY.json
            var agora = DateTime.Now;
            var chaveMes = agora.ToString("MM-yyyy");
            var mesPath = Path.Combine(bancoDadosDir, chaveMes + ".json");
            if (File.Exists(mesPath))
            {
                try
                {
                    var obj = JsonNode.Parse(File.ReadAllText(mesPath))?.AsObject();
                    if (obj is not null && obj.ContainsKey("registros"))
                    {
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
                                        estoqueAtual[nome] = estoqueAtual.TryGetValue(nome, out var atual) ? atual + peso : peso;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // Subtrai vendas venda-DD-MM-YYYY.json
            foreach (var file in Directory.GetFiles(bancoDadosDir, "venda-*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var obj = JsonNode.Parse(File.ReadAllText(file))?.AsObject();
                    if (obj is not null && obj.ContainsKey("registros"))
                    {
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
                                        estoqueAtual[nome] = estoqueAtual.TryGetValue(nome, out var atual) ? atual - peso : -peso;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // Remove zeros
            foreach (var key in estoqueAtual.Keys.ToList())
                if (estoqueAtual[key] == 0m) estoqueAtual.Remove(key);
            var objFinal = new JsonObject();
            objFinal["data"] = $"{mes:D2}/{ano}";
            foreach (var kv in estoqueAtual.OrderBy(k => k.Key))
                objFinal[kv.Key] = JsonValue.Create(kv.Value);
            var conteudoFinal = objFinal.ToJsonString(JsonOpts);

            await GitHubService.GarantirBancoDadosRepoAsync(_rootDir, msg => Status = msg);

            var pathFinal = Path.Combine(GitHubService.BancoDadosRepoDir(_rootDir), nomeFinal);
            await File.WriteAllTextAsync(pathFinal, conteudoFinal);
            await GitHubService.PublicarJsonBancoDadosAsync(_rootDir, nomeFinal, conteudoFinal, msg => Status = msg);

            AppendLog($"Registrar estoque-final-{mes:D2}-{ano}");

            // Cria novo estoque-inicial com valores do estoque atual e mês/ano atual
            MesSelecionado = DateTime.Now.Month;
            AnoSelecionado = DateTime.Now.Year;

            Itens.Clear();
            foreach (var nome in ItemCatalog.OrderedItems)
            {
                estoqueAtual.TryGetValue(nome, out var kg);
                Itens.Add(new PesoInicialWrapper(nome, kg));
            }

            GravarEstoqueInicialLocal();
            AppendLog("Novo estoque-inicial criado a partir do estoque atual");

            var novoConteudo = await File.ReadAllTextAsync(EstoqueInicialPath);
            var nomeArquivo = Path.GetFileName(EstoqueInicialPath);
            await GitHubService.PublicarJsonBancoDadosAsync(_rootDir, nomeArquivo, novoConteudo, msg => Status = msg);

            await PublicarLog();

            Status = $"Estoque final {mes:D2}/{ano} registrado e novo estoque inicial criado.";
        }
        catch (Exception ex)
        {
            Status = $"Erro ao registrar: {ex.Message}";
        }
        finally
        {
            Registrando = false;
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private void GravarEstoqueInicialLocal()
    {
        var dir = GitHubService.BancoDadosRepoDir(_rootDir);
        Directory.CreateDirectory(dir);

        var obj = new JsonObject();
        obj["data"] = JsonValue.Create($"{MesSelecionado:D2}/{AnoSelecionado}");
        foreach (var item in Itens)
            obj[item.Nome] = JsonValue.Create(item.Peso);

        if (_impurezasPesoAtual > 0)
            obj["Impurezas"] = JsonValue.Create(_impurezasPesoAtual);

        foreach (var c in ItensPersonalizados)
            if (!string.IsNullOrWhiteSpace(c.Nome))
                obj[c.Nome] = JsonValue.Create(c.Peso);

        File.WriteAllText(EstoqueInicialPath, obj.ToJsonString(JsonOpts), Encoding.UTF8);
    }

    private void AppendLog(string acao)
    {
        try
        {
            var dir = GitHubService.BancoDadosRepoDir(_rootDir);
            Directory.CreateDirectory(dir);
            var linha = $"{DateTime.Now:yyyy-MM-ddTHH:mm:ss} | {acao}{Environment.NewLine}";
            File.AppendAllText(LogPath, linha);
        }
        catch { }
    }

    private async Task PublicarLog()
    {
        try
        {
            if (!File.Exists(LogPath)) return;
            var conteudo = await File.ReadAllTextAsync(LogPath);
            await GitHubService.PublicarJsonBancoDadosAsync(_rootDir, LogFileName, conteudo);
        }
        catch { }
    }

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
}
