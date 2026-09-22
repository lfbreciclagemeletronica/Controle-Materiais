namespace ControleMateriais.Data.Models;

public class ReciboCompra
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string CodigoRecibo { get; set; } = string.Empty; // Valor aleatório de 5 dígitos
    public string DataRecibo { get; set; } = string.Empty; // formato: 10/07/2026
    public decimal ValorTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public DateTime CriadoEm { get; set; }

    public string UsuarioNome { get; set; } = string.Empty; // populado via JOIN

    public List<ReciboCompraItem> Itens { get; set; } = new();
}
