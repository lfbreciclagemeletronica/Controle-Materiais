using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControleMateriais.Desktop.Views;

public partial class ConfirmDeleteDialog : Window
{
    public bool Confirmado { get; private set; }

    public ConfirmDeleteDialog(string mensagem, string? titulo = null, string? textoConfirmar = null,
        string? corConfirmar = null)
    {
        InitializeComponent();
        var tb = this.FindControl<TextBlock>("MensagemText");
        if (tb is not null) tb.Text = mensagem;

        if (titulo is not null)
        {
            Title = titulo;
            var tituloTb = this.FindControl<TextBlock>("TituloText");
            if (tituloTb is not null) tituloTb.Text = titulo;
        }

        var confirmarBtn = this.FindControl<Button>("ConfirmarButton");
        if (confirmarBtn is not null)
        {
            if (textoConfirmar is not null) confirmarBtn.Content = textoConfirmar;
            if (corConfirmar is not null)
                confirmarBtn.Background = Avalonia.Media.Brush.Parse(corConfirmar);
        }
    }

    private void Confirmar_Click(object? sender, RoutedEventArgs e)
    {
        Confirmado = true;
        Close();
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close();
}
