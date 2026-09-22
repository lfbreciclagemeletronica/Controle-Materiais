using Avalonia.Controls;
using Avalonia.Interactivity;
using ControleMateriais.Data.Models;
using System.Globalization;

namespace ControleMateriais.Desktop.Views;

public partial class EntradaDetalhesDialog : Window
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

    public EntradaDetalhesDialog(ReciboCompra recibo)
    {
        InitializeComponent();

        this.FindControl<TextBlock>("ClienteText")!.Text = recibo.UsuarioNome;
        this.FindControl<TextBlock>("CodigoText")!.Text = recibo.CodigoRecibo;
        this.FindControl<TextBlock>("DataText")!.Text = recibo.DataRecibo;
        this.FindControl<TextBlock>("PesoTotalText")!.Text = recibo.PesoTotalKg.ToString("N3", PtBR) + " kg";
        this.FindControl<TextBlock>("ValorTotalText")!.Text = recibo.ValorTotal.ToString("C", PtBR);

        this.FindControl<ItemsControl>("ItensList")!.ItemsSource = recibo.Itens;
    }

    private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();
}
