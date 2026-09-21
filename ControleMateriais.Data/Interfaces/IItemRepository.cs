using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IItemRepository
{
    Task<IEnumerable<Item>> GetAllAsync();
    Task<Item?> GetByIdAsync(int id);
    Task<Item> GetOrCreateAsync(string nome);
    Task<int> InsertAsync(Item item);
    Task<int> UpdateAsync(Item item);
    Task<int> DeleteAsync(int id);
}
