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

    // Gera um código de recibo aleatório de 5 dígitos que ainda não existe em RecibosCompra.
    public async Task<string> GerarCodigoReciboAsync()
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sqlExiste = "SELECT COUNT(*) FROM RecibosCompra WHERE CodigoRecibo = @codigo;";
        var random = Random.Shared;

        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            var codigo = random.Next(10000, 100000).ToString();
            var existe = await connection.ExecuteScalarAsync<int>(sqlExiste, new { codigo });
            if (existe == 0) return codigo;
        }

        throw new InvalidOperationException("Não foi possível gerar um código de recibo único.");
    }

    public async Task<int> InsertAsync(ReciboCompra recibo)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            const string sqlRecibo = @"
                INSERT INTO RecibosCompra (UsuarioId, DataRecibo, CodigoRecibo, ValorTotal, PesoTotalKg)
                VALUES (@usuarioId, @dataRecibo, @codigoRecibo, @valorTotal, @pesoTotalKg);
                SELECT last_insert_rowid();";

            var parametrosRecibo = new
            {
                usuarioId = recibo.UsuarioId,
                dataRecibo = recibo.DataRecibo,
                codigoRecibo = recibo.CodigoRecibo,
                valorTotal = recibo.ValorTotal,
                pesoTotalKg = recibo.PesoTotalKg
            };

            var reciboId = await connection.ExecuteScalarAsync<int>(sqlRecibo, parametrosRecibo, transaction);

            const string sqlItem = @"
                INSERT INTO ReciboCompraItens (ReciboCodigo, ItemId, PesoKg, PrecoPorKg, Total)
                VALUES (@reciboCodigo, @itemId, @pesoKg, @precoPorKg, @total);";

            foreach (var item in recibo.Itens)
            {
                var parametrosItem = new
                {
                    reciboCodigo = recibo.CodigoRecibo,
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
                rc.Id, rc.UsuarioId, rc.CodigoRecibo, rc.DataRecibo, rc.ValorTotal, rc.PesoTotalKg, rc.CriadoEm,
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
                rc.Id, rc.UsuarioId, rc.CodigoRecibo, rc.DataRecibo, rc.ValorTotal, rc.PesoTotalKg, rc.CriadoEm
            FROM RecibosCompra rc
            WHERE rc.Id = @id;";

        const string sqlItens = @"
            SELECT
                rci.Id, rci.ReciboCodigo, rci.ItemId, rci.PesoKg, rci.PrecoPorKg, rci.Total,
                i.Nome AS ItemNome
            FROM ReciboCompraItens rci
            JOIN Itens i ON i.Id = rci.ItemId
            WHERE rci.ReciboCodigo = (SELECT CodigoRecibo FROM RecibosCompra WHERE Id = @id);";

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

    // Agrupa o total comprado (Kg) de cada item por mês (MM/yyyy), a partir de RecibosCompra.DataRecibo
    // (formato dd/MM/yyyy) + ReciboCompraItens + Itens.
    public async Task<IEnumerable<ItemMensal>> GetTotaisPorMesAsync()
    {
        const string sql = @"
            SELECT
                substr(rc.DataRecibo, 4, 2) || '/' || substr(rc.DataRecibo, 7, 4) AS Mes,
                i.Nome AS ItemNome,
                SUM(rci.PesoKg) AS TotalKg
            FROM ReciboCompraItens rci
            JOIN RecibosCompra rc ON rc.CodigoRecibo = rci.ReciboCodigo
            JOIN Itens i ON i.Id = rci.ItemId
            GROUP BY Mes, i.Nome
            ORDER BY Mes DESC, i.Nome;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<ItemMensal>(sql);
    }
}
