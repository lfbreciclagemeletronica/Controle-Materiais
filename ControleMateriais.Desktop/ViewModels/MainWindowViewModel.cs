using ControleMateriais.Models;
using System.Collections.ObjectModel;

namespace ControleMateriais.Desktop.ViewModels;

public class MainWindowViewModel
{
    public ObservableCollection<MaterialItem> Itens { get; } = new();

    public MainWindowViewModel()
    {
      Itens.Add(new MaterialItem
        {Nome = "Placas pesadas", PesoAtual = 0m, PrecoPorKg = 2.00m}
      );
      Itens.Add(new MaterialItem
        { Nome = "Placas leves", PesoAtual = 0m, PrecoPorKg = 4.00m }
      );
      Itens.Add(new MaterialItem
        { Nome = "", PesoAtual = 0m, PrecoPorKg = 0m }
      );

    }
}
