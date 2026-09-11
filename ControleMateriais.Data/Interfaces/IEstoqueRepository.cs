using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IEstoqueRepository
{
    Task<IEnumerable<EstoqueView>> GetEstoqueAsync();
}
