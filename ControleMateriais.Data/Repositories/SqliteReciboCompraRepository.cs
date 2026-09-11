using ControleMateriais.Data.Common;
using ControleMateriais.Data.Interfaces;
using ControleMateriais.Data.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ControleMateriais.Data.Repositories;

public class SqliteReciboCompraRepository : IReciboCompraRepository
{
    private SqliteConnection CreateConnection() =>
        new SqliteConnection(DbSettings.GetConnectionString());

    public async Task<int> InsertAsync(ReciboCompra recibo)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            const string sqlRecibo = @"
                INSERT INTO RecibosCompra (UsuarioId, NumeroRecibo, DataRecibo, ValorTotal, PesoTotalKg)
                VALUES (@usuarioId, @numeroRecibo, @dataRecibo, @valorTotal, @pesoTotalKg);
                SELECT last_insert_rowid();";

            var parametrosRecibo = new
            {
                usuarioId = recibo.UsuarioId,
                numeroRecibo = recibo.NumeroRecibo,
                dataRecibo = recibo.DataRecibo,
                valorTotal = recibo.ValorTotal,
                pesoTotalKg = recibo.PesoTotalKg
            };

            var reciboId = await connection.ExecuteScalarAsync<int>(sqlRecibo, parametrosRecibo, transaction);

            const string sqlItem = @"
                INSERT INTO ReciboCompraItens (ReciboCompraId, ItemId, PesoKg, PrecoPorKg, Total)
                VALUES (@reciboCompraId, @itemId, @pesoKg, @precoPorKg, @total);";

            foreach (var item in recibo.Itens)
            {
                var parametrosItem = new
                {
                    reciboCompraId = reciboId,
                    itemId = item.ItemId,
                    pesoKg = item.PesoKg,
                    precoPorKg = item.PrecoPorKg,
                    total = item.Total
                };

                await connection.ExecuteAsync(sqlItem, parametrosItem, transaction);
            }

            transaction.Commit();
            return reciboId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<ReciboCompra>> GetAllAsync()
    {
        const string sql = @"
            SELECT
                rc.Id, rc.UsuarioId, rc.NumeroRecibo, rc.DataRecibo, rc.ValorTotal, rc.PesoTotalKg, rc.CriadoEm,
                u.Nome AS UsuarioNome
            FROM RecibosCompra rc
            JOIN Usuarios u ON u.Id = rc.UsuarioId
            ORDER BY rc.DataRecibo DESC;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<ReciboCompra>(sql);
    }

    public async Task<ReciboCompra?> GetByIdAsync(int id)
    {
        const string sqlRecibo = @"
            SELECT
                rc.Id, rc.UsuarioId, rc.NumeroRecibo, rc.DataRecibo, rc.ValorTotal, rc.PesoTotalKg, rc.CriadoEm
            FROM RecibosCompra rc
            WHERE rc.Id = @id;";

        const string sqlItens = @"
            SELECT
                rci.Id, rci.ReciboCompraId, rci.ItemId, rci.PesoKg, rci.PrecoPorKg, rci.Total,
                i.Nome AS ItemNome
            FROM ReciboCompraItens rci
            JOIN Itens i ON i.Id = rci.ItemId
            WHERE rci.ReciboCompraId = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();

        var recibo = await connection.QueryFirstOrDefaultAsync<ReciboCompra>(sqlRecibo, new { id });
        if (recibo is null) return null;

        var itens = await connection.QueryAsync<ReciboCompraItem>(sqlItens, new { id });
        recibo.Itens = itens.ToList();

        return recibo;
    }

    public async Task<int> UpdateAsync(ReciboCompra recibo) =>
        throw new NotSupportedException("Recibos geralmente não são editados. Exclua e recrie se necessário.");

    public async Task<int> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM RecibosCompra WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, new { id });
    }
}
