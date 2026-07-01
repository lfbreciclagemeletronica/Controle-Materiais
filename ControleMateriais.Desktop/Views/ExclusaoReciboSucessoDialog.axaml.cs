using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;

namespace ControleMateriais.Desktop.Views;

public partial class ExclusaoReciboSucessoDialog : Window
{
    public ExclusaoReciboSucessoDialog(List<string> etapas)
    {
        InitializeComponent();
        EtapasPanel.ItemsSource = etapas;
    }

    private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();
}
