namespace ControleMateriais.Data.Models;

/// <summary>
/// Representa o total (em Kg) comprado de um item em um determinado mês,
/// agregado a partir de ReciboCompraItens + RecibosCompra.
/// </summary>
public class ItemMensal
{
    /// <summary>Mês/ano no formato "MM/yyyy".</summary>
    public string Mes { get; set; } = string.Empty;
    public string ItemNome { get; set; } = string.Empty;
    public decimal TotalKg { get; set; }
}
