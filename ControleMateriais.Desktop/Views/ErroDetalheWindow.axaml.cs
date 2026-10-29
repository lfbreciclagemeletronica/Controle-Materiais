using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControleMateriais.Desktop.Views;

public partial class ErroDetalheWindow : Window
{
    public ErroDetalheWindow() : this("", "") { }

    public ErroDetalheWindow(string titulo, string detalhe)
    {
        InitializeComponent();
        TituloText.Text = $"✘  {titulo}";
        DetalheText.Text = detalhe;
    }

    private async void Copiar_Click(object? sender, RoutedEventArgs e)
    {
        if (Clipboard is not null) await Clipboard.SetTextAsync(DetalheText.Text ?? "");
    }

    private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();
}
