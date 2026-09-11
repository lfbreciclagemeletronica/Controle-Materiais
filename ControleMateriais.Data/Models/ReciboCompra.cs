namespace ControleMateriais.Data.Models;

public class ReciboCompra
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public long? NumeroRecibo { get; set; }
    public string DataRecibo { get; set; } = string.Empty; // formato: 10/07/2026
    public decimal ValorTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public DateTime CriadoEm { get; set; }

    public List<ReciboCompraItem> Itens { get; set; } = new();
}
