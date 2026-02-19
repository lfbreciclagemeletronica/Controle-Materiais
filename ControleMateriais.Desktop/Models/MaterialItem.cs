using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ControleMateriais.Models
{
  public class MaterialItem : INotifyPropertyChanged
  {
    private string _nome = "";
    private decimal _pesoAtual;
    private decimal _precoPorKg;

    public string Nome
    {
      get => _nome;
      set { if (value != _nome) { _nome = value; OnPropertyChanged(); }}
    }

    public decimal PesoAtual
    {
      get => _pesoAtual;
      set { if (value != _pesoAtual) { _pesoAtual = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); }}
    }

    public decimal PrecoPorKg
    {
      get => _precoPorKg;
      set { if (value != _precoPorKg) { _precoPorKg = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); } }
    }

    public decimal Total => PesoAtual * PrecoPorKg;
    public event PropertyChangedEventHandler? PropertyChanged; // A View é atualizada automaticamente
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));


  }
}
