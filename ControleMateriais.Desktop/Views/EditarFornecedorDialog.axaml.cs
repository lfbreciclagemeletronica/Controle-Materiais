using Avalonia.Controls;
using Avalonia.Interactivity;
using ControleMateriais.Data.Models;

namespace ControleMateriais.Desktop.Views;

public partial class EditarFornecedorDialog : Window
{
    public bool Confirmado { get; private set; }
    public Usuario Usuario { get; }

    public EditarFornecedorDialog(Usuario usuario)
    {
        InitializeComponent();
        Usuario = usuario;

        this.FindControl<TextBox>("NomeBox")!.Text = usuario.Nome;
        this.FindControl<TextBox>("CpfBox")!.Text = usuario.Cpf;
        this.FindControl<TextBox>("CnpjBox")!.Text = usuario.Cnpj;
        this.FindControl<TextBox>("ChavePixBox")!.Text = usuario.ChavePix;
    }

    private void Salvar_Click(object? sender, RoutedEventArgs e)
    {
        var nome = this.FindControl<TextBox>("NomeBox")!.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nome))
        {
            var erro = this.FindControl<TextBlock>("ErroText")!;
            erro.Text = "O nome é obrigatório.";
            erro.IsVisible = true;
            return;
        }

        Usuario.Nome = nome;
        Usuario.Cpf = NullIfEmpty(this.FindControl<TextBox>("CpfBox")!.Text);
        Usuario.Cnpj = NullIfEmpty(this.FindControl<TextBox>("CnpjBox")!.Text);
        Usuario.ChavePix = NullIfEmpty(this.FindControl<TextBox>("ChavePixBox")!.Text);

        Confirmado = true;
        Close();
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close();
}
