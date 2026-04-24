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

    public bool PodeSalvar =>
        !string.IsNullOrWhiteSpace(Token) &&
        !string.IsNullOrWhiteSpace(GitUsuario) &&
        !string.IsNullOrWhiteSpace(GitEmail);

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
