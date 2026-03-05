using ControleMateriais.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace ControleMateriais.Desktop.ViewModels;

public class WeightCalculatorViewModel : ViewModelBase
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

    public ObservableCollection<WeightItemWrapper> Itens { get; } = new();

    private decimal _pesoTotal;
    public decimal PesoTotal
    {
        get => _pesoTotal;
        private set { if (value != _pesoTotal) { _pesoTotal = value; OnPropertyChanged(); } }
    }

    public ICommand VoltarCommand { get; }
    public ICommand LimparCommand { get; }

    public WeightCalculatorViewModel(Action voltarCallback)
    {
        foreach (var nome in ItemCatalog.OrderedItems)
            Itens.Add(new WeightItemWrapper(nome, RecalcularTotal));

        VoltarCommand = new DelegateCommand(voltarCallback);
        LimparCommand = new DelegateCommand(Limpar);

        RecalcularTotal();
    }

    public void SelecionarItem(object? item)
    {
        foreach (var w in Itens)
            w.IsSelected = ReferenceEquals(w, item);
    }

    private void RecalcularTotal()
    {
        decimal total = 0m;
        foreach (var w in Itens)
            total += w.PesoAtual;
        PesoTotal = total;
    }

    private void Limpar()
    {
        foreach (var w in Itens)
            w.Resetar();
        RecalcularTotal();
    }
}

public class WeightItemWrapper : ViewModelBase
{
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");
    private readonly Action _onChanged;

    private bool _editando;
    private string _pesoTextoAnterior = string.Empty;
    private decimal _pesoAtual;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { if (value != _isSelected) { _isSelected = value; OnPropertyChanged(); } }
    }

    public string Nome { get; }

    public decimal PesoAtual
    {
        get => _pesoAtual;
        private set { if (value != _pesoAtual) { _pesoAtual = value; OnPropertyChanged(); _onChanged(); } }
    }

    private string _pesoTexto;
    public string PesoTexto
    {
        get => _pesoTexto;
        set { if (value != _pesoTexto) { _pesoTexto = value; OnPropertyChanged(); } }
    }

    public WeightItemWrapper(string nome, Action onChanged)
    {
        Nome = nome;
        _onChanged = onChanged;
        _pesoAtual = 0m;
        _pesoTexto = "0,000";
    }

    public void IniciarEdicao()
    {
        _pesoTextoAnterior = _pesoTexto;
        _editando = true;
        PesoTexto = string.Empty;
    }

    public void CancelarEdicao()
    {
        if (!_editando) return;
        _editando = false;
        PesoTexto = _pesoTextoAnterior;
    }

    public void ConfirmarEdicao()
    {
        if (!_editando) return;
        _editando = false;
        var raw = PesoTexto.Trim().Replace(" ", "");
        if (raw.Contains(',') && raw.Contains('.'))
            raw = raw.Replace(".", "").Replace(",", ".");
        else
            raw = raw.Replace(",", ".");
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            parsed = _pesoAtual;
        PesoAtual = parsed;
        PesoTexto = parsed.ToString("N3", PtBR);
    }

    public void Resetar()
    {
        _editando = false;
        _pesoAtual = 0m;
        _pesoTexto = "0,000";
        OnPropertyChanged(nameof(PesoTexto));
        OnPropertyChanged(nameof(PesoAtual));
    }
}
