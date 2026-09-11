using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IReciboVendaRepository
{
    Task<IEnumerable<ReciboVenda>> GetAllAsync();
    Task<ReciboVenda?> GetByIdAsync(int id);
    Task<int> InsertAsync(ReciboVenda recibo);
    Task<int> UpdateAsync(ReciboVenda recibo);
    Task<int> DeleteAsync(int id);
}
