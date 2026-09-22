using ControleMateriais.Data.Interfaces;
using ControleMateriais.Data.Repositories;

namespace ControleMateriais.Desktop;

public static class DataService
{
    public static IUsuarioRepository UsuarioRepository => new SqliteUsuarioRepository();
    public static IItemRepository ItemRepository => new SqliteItemRepository();
    public static IReciboCompraRepository ReciboCompraRepository => new SqliteReciboCompraRepository();
    public static IReciboVendaRepository ReciboVendaRepository => new SqliteReciboVendaRepository();
    public static IEstoqueRepository EstoqueRepository => new SqliteEstoqueRepository();
}
