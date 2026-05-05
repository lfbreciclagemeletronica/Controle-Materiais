using System;

namespace ControleMateriais.Desktop.ViewModels;

public class GitHubConfigViewModel : ViewModelBase
{
    private string _token = string.Empty;
    public string Token
    {
        get => _token;
        set { if (value != _token) { _token = value; OnPropertyChanged(); OnPropertyChanged(nameof(PodeSalvar)); } }
    }

    private string _gitUsuario = string.Empty;
    public string GitUsuario
    {
        get => _gitUsuario;
        set { if (value != _gitUsuario) { _gitUsuario = value; OnPropertyChanged(); OnPropertyChanged(nameof(PodeSalvar)); } }
    }

    private string _gitEmail = string.Empty;
    public string GitEmail
    {
        get => _gitEmail;
        set { if (value != _gitEmail) { _gitEmail = value; OnPropertyChanged(); OnPropertyChanged(nameof(PodeSalvar)); } }
    }

    private string _urlPesagens = string.Empty;
    public string UrlPesagens
    {
        get => _urlPesagens;
        set { if (value != _urlPesagens) { _urlPesagens = value; OnPropertyChanged(); OnPropertyChanged(nameof(PodeSalvar)); } }
    }

    private string _urlRecibos = string.Empty;
    public string UrlRecibos
    {
        get => _urlRecibos;
        set { if (value != _urlRecibos) { _urlRecibos = value; OnPropertyChanged(); OnPropertyChanged(nameof(PodeSalvar)); } }
    }

    private string _urlTabelaPrecos = string.Empty;
    public string UrlTabelaPrecos
    {
        get => _urlTabelaPrecos;
        set { if (value != _urlTabelaPrecos) { _urlTabelaPrecos = value; OnPropertyChanged(); OnPropertyChanged(nameof(PodeSalvar)); } }
    }

    private string _urlBancoDados = string.Empty;
    public string UrlBancoDados
    {
        get => _urlBancoDados;
        set { if (value != _urlBancoDados) { _urlBancoDados = value; OnPropertyChanged(); } }
    }

    public bool PodeSalvar =>
        !string.IsNullOrWhiteSpace(Token) &&
        !string.IsNullOrWhiteSpace(GitUsuario) &&
        !string.IsNullOrWhiteSpace(GitEmail) &&
        !string.IsNullOrWhiteSpace(UrlPesagens) &&
        !string.IsNullOrWhiteSpace(UrlRecibos) &&
        !string.IsNullOrWhiteSpace(UrlTabelaPrecos);

    public bool Confirmado { get; private set; }

    public Action? FecharDialog { get; set; }
    public Action? AbrirAjuda { get; set; }

    public void Confirmar()
    {
        if (!PodeSalvar) return;
        Confirmado = true;
        FecharDialog?.Invoke();
    }

    public void Cancelar()
    {
        Confirmado = false;
        FecharDialog?.Invoke();
    }

    public void MostrarAjuda() => AbrirAjuda?.Invoke();
}
