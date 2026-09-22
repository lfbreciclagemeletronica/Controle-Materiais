using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IReciboVendaRepository
{
    Task<IEnumerable<ReciboVenda>> GetAllAsync();
    Task<ReciboVenda?> GetByIdAsync(int id);
    Task<string> GerarCodigoReciboAsync();
    Task<int> InsertAsync(ReciboVenda recibo);
    Task<int> UpdateAsync(ReciboVenda recibo);
    Task<int> DeleteAsync(int id);
}
