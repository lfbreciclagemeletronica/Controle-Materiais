namespace ControleMateriais.Data.Models;

public class EstoqueView
{
    public int ItemId { get; set; }
    public string ItemNome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public decimal TotalComprasKg { get; set; }
    public decimal TotalVendasKg { get; set; }
    public decimal SaldoKg => TotalComprasKg - TotalVendasKg;
}
