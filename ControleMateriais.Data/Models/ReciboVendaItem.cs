namespace ControleMateriais.Data.Models;

public class ReciboVendaItem
{
    public int Id { get; set; }
    public int ReciboVendaId { get; set; }
    public int ItemId { get; set; }
    public decimal PesoKg { get; set; }
    public decimal PrecoPorKg { get; set; }
    public decimal Total => PesoKg * PrecoPorKg;

    public string ItemNome { get; set; } = string.Empty; // populated via JOIN
}
