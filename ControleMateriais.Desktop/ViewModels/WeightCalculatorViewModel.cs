using ControleMateriais.Desktop.Services;
using ControleMateriais.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ControleMateriais.Desktop.ViewModels;

public class WeightCalculatorViewModel : ViewModelBase
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

    public ObservableCollection<WeightItemWrapper> Itens { get; } = new();

    private decimal _pesoTotal;
    public decimal PesoTotal
    {
        get => _pesoTotal;
        private set { if (value != _pesoTotal) { _pesoTotal = value; OnPropertyChanged(); } }
    }

    public ICommand VoltarCommand { get; }
    public ICommand LimparCommand { get; }
    public ICommand SalvarEnviarCommand { get; }

    public string RootDir { get; }

    private string _nomeCliente = string.Empty;
    public string NomeCliente
    {
        get => _nomeCliente;
        set { if (value != _nomeCliente) { _nomeCliente = value; OnPropertyChanged(); } }
    }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        private set { if (value != _status) { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusVisivel)); } }
    }
    public bool StatusVisivel => !string.IsNullOrEmpty(_status);

    private bool _statusOk;
    public bool StatusOk
    {
        get => _statusOk;
        private set { if (value != _statusOk) { _statusOk = value; OnPropertyChanged(); } }
    }

    public Func<Task>? SolicitarConfiguracaoGitHubCallback { get; set; }
    public Func<string, Task>? MostrarSucessoCallback { get; set; }

    public WeightCalculatorViewModel(Action voltarCallback, string rootDir)
    {
        RootDir = rootDir;

        foreach (var nome in ItemCatalog.OrderedItems)
            Itens.Add(new WeightItemWrapper(nome, RecalcularTotal));

        VoltarCommand = new DelegateCommand(voltarCallback);
        LimparCommand = new DelegateCommand(Limpar);
        SalvarEnviarCommand = new DelegateCommand(() => _ = SalvarEnviarAsync());

        RecalcularTotal();
    }

    private async Task SalvarEnviarAsync()
    {
        var itens = Itens.Where(w => w.PesoAtual > 0).ToList();
        if (!itens.Any())
        {
            MostrarStatus("Nenhum peso para salvar.", ok: false);
            return;
        }

        var agora = DateTime.Now;
        var cliente = string.IsNullOrWhiteSpace(NomeCliente) ? "SemNome" : NomeCliente;
        var nomeSeguro = string.Concat(cliente.Split(Path.GetInvalidFileNameChars()));
        var nomeArquivo = $"{nomeSeguro}_{agora:dd-MM-yyyy}.json";
        var mensagemCommit = $"{cliente} - {agora:dd/MM/yyyy}";

        var payload = new
        {
            Cliente = cliente,
            Horario = agora.ToString("yyyy-MM-ddTHH:mm:ss"),
            Itens = itens.Select(w => new { w.Nome, Peso = w.PesoAtual }),
            StatusPesagem = "pendente"
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        // 1. Verificar credenciais GitHub
        if (!GitHubService.CredenciaisExistem(RootDir))
        {
            Status = "Abrindo configuração do GitHub...";
            StatusOk = true;
            if (SolicitarConfiguracaoGitHubCallback is not null)
                await SolicitarConfiguracaoGitHubCallback();

            if (!GitHubService.CredenciaisExistem(RootDir))
            {
                MostrarStatus("Salvo localmente. Configuração GitHub cancelada.", ok: true);
                return;
            }
        }

        // 3. Verificar/instalar git
        Status = "Verificando Git...";
        StatusOk = true;
        if (!await GitHubService.GitDisponivel())
        {
            try
            {
                await GitHubService.InstalarGitAsync(msg => { Status = msg; StatusOk = true; });
            }
            catch (Exception ex)
            {
                MostrarStatus($"Erro ao instalar Git: {ex.Message}", ok: false);
                return;
            }
        }

        // 4. Enviar ao GitHub via git
        try
        {
            await GitHubService.EnviarArquivoAsync(
                RootDir, json, nomeArquivo, mensagemCommit,
                msg => { Status = msg; StatusOk = true; });
            foreach (var w in Itens)
                w.Resetar();
            NomeCliente = string.Empty;
            RecalcularTotal();
            Status = string.Empty;
            if (MostrarSucessoCallback is not null)
                await MostrarSucessoCallback(nomeArquivo);
        }
        catch (Exception ex)
        {
            MostrarStatus($"Erro GitHub: {ex.Message}", ok: false);
        }
    }

    private void MostrarStatus(string mensagem, bool ok)
    {
        Status = mensagem;
        StatusOk = ok;
        _ = Task.Run(async () =>
        {
            await Task.Delay(7000);
            Status = string.Empty;
        });
    }

    public void SelecionarItem(object? item)
    {
        foreach (var w in Itens)
            w.IsSelected = ReferenceEquals(w, item);
    }

    private void RecalcularTotal()
    {
        decimal total = 0m;
        foreach (var w in Itens)
            total += w.PesoAtual;
        PesoTotal = total;
    }

    private void Limpar()
    {
        foreach (var w in Itens)
            w.Resetar();
        NomeCliente = string.Empty;
        RecalcularTotal();
    }
}

public class WeightItemWrapper : ViewModelBase
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");
    private readonly Action _onChanged;

    private bool _editando;
    private string _pesoTextoAnterior = string.Empty;
    private decimal _pesoAtual;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { if (value != _isSelected) { _isSelected = value; OnPropertyChanged(); } }
    }

    public string Nome { get; }

    public decimal PesoAtual
    {
        get => _pesoAtual;
        private set { if (value != _pesoAtual) { _pesoAtual = value; OnPropertyChanged(); _onChanged(); } }
    }

    private string _pesoTexto;
    public string PesoTexto
    {
        get => _pesoTexto;
        set { if (value != _pesoTexto) { _pesoTexto = value; OnPropertyChanged(); } }
    }

    public WeightItemWrapper(string nome, Action onChanged)
    {
        Nome = nome;
        _onChanged = onChanged;
        _pesoAtual = 0m;
        _pesoTexto = "0,000";
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
            parsed = _pesoAtual;
        PesoAtual = parsed;
        PesoTexto = parsed.ToString("N3", PtBR);
    }

    public void Resetar()
    {
        _editando = false;
        _pesoAtual = 0m;
        _pesoTexto = "0,000";
        OnPropertyChanged(nameof(PesoTexto));
        OnPropertyChanged(nameof(PesoAtual));
    }
}
