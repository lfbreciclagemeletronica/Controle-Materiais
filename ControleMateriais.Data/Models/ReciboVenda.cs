namespace ControleMateriais.Data.Models;

public class ReciboVenda
{
    public int Id { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string? ClienteCpf { get; set; }
    public string? ClienteCnpj { get; set; }
    public string? ClienteChavePix { get; set; }
    public long? NumeroRecibo { get; set; }
    public string DataVenda { get; set; } = string.Empty; // formato: 10/07/2026
    public decimal ValorTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public DateTime CriadoEm { get; set; }

    public List<ReciboVendaItem> Itens { get; set; } = new();
}
