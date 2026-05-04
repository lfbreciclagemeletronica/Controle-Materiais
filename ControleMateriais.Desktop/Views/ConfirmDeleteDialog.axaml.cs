using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControleMateriais.Desktop.Views;

public partial class ConfirmDeleteDialog : Window
{
    public bool Confirmado { get; private set; }

    public ConfirmDeleteDialog(string mensagem)
    {
        InitializeComponent();
        var tb = this.FindControl<TextBlock>("MensagemText");
        if (tb is not null) tb.Text = mensagem;
    }

    private void Confirmar_Click(object? sender, RoutedEventArgs e)
    {
        Confirmado = true;
        Close();
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close();
}
