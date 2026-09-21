namespace ControleMateriais.Data.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Cnpj { get; set; }
    public string? ChavePix { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public bool Ativo { get; set; }
}
