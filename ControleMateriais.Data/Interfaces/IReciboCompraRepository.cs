using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IReciboCompraRepository
{
    Task<IEnumerable<ReciboCompra>> GetAllAsync();
    Task<ReciboCompra?> GetByIdAsync(int id);
    Task<int> InsertAsync(ReciboCompra recibo);
    Task<int> UpdateAsync(ReciboCompra recibo);
    Task<int> DeleteAsync(int id);
}
