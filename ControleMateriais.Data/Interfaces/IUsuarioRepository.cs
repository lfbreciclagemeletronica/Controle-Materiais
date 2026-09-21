using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<IEnumerable<Usuario>> GetTodosAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<Usuario> GetOrCreateAsync(string nome);
    Task<int> InsertAsync(Usuario usuario);
    Task<int> UpdateAsync(Usuario usuario);
    Task<int> DeleteAsync(int id);
    Task<int> ReativarAsync(int id);
}
