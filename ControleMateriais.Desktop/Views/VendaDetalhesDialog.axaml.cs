using Avalonia.Controls;
using Avalonia.Interactivity;
using ControleMateriais.Data.Models;
using System.Globalization;

namespace ControleMateriais.Desktop.Views;

public partial class VendaDetalhesDialog : Window
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

    public VendaDetalhesDialog(ReciboVenda recibo)
    {
        InitializeComponent();

        this.FindControl<TextBlock>("ClienteText")!.Text = recibo.ClienteNome;
        this.FindControl<TextBlock>("CodigoText")!.Text = recibo.CodigoRecibo;
        this.FindControl<TextBlock>("DataText")!.Text = recibo.DataVenda;
        this.FindControl<TextBlock>("PesoTotalText")!.Text = recibo.PesoTotalKg.ToString("N3", PtBR) + " kg";
        this.FindControl<TextBlock>("ValorTotalText")!.Text = recibo.ValorTotal.ToString("C", PtBR);

        this.FindControl<ItemsControl>("ItensList")!.ItemsSource = recibo.Itens;
    }

    private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();
}
