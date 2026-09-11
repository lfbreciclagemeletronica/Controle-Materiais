using ControleMateriais.Data.Common;
using ControleMateriais.Data.Interfaces;
using ControleMateriais.Data.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ControleMateriais.Data.Repositories;

public class SqliteReciboVendaRepository : IReciboVendaRepository
{
    private SqliteConnection CreateConnection() =>
        new SqliteConnection(DbSettings.GetConnectionString());

    public async Task<int> InsertAsync(ReciboVenda recibo)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            const string sqlRecibo = @"
                INSERT INTO RecibosVenda (ClienteNome, ClienteCpf, ClienteCnpj, ClienteChavePix, NumeroRecibo, DataVenda, ValorTotal, PesoTotalKg)
                VALUES (@clienteNome, @clienteCpf, @clienteCnpj, @clienteChavePix, @numeroRecibo, @dataVenda, @valorTotal, @pesoTotalKg);
                SELECT last_insert_rowid();";

            var parametrosRecibo = new
            {
                clienteNome = recibo.ClienteNome,
                clienteCpf = recibo.ClienteCpf,
                clienteCnpj = recibo.ClienteCnpj,
                clienteChavePix = recibo.ClienteChavePix,
                numeroRecibo = recibo.NumeroRecibo,
                dataVenda = recibo.DataVenda,
                valorTotal = recibo.ValorTotal,
                pesoTotalKg = recibo.PesoTotalKg
            };

            var reciboId = await connection.ExecuteScalarAsync<int>(sqlRecibo, parametrosRecibo, transaction);

            const string sqlItem = @"
                INSERT INTO RecibosVendaItens (ReciboVendaId, ItemId, PesoKg, PrecoPorKg, Total)
                VALUES (@reciboVendaId, @itemId, @pesoKg, @precoPorKg, @total);";

            foreach (var item in recibo.Itens)
            {
                var parametrosItem = new
                {
                    reciboVendaId = reciboId,
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

    public async Task<IEnumerable<ReciboVenda>> GetAllAsync()
    {
        const string sql = @"
            SELECT
                rv.Id, rv.ClienteNome, rv.ClienteCpf, rv.ClienteCnpj, rv.ClienteChavePix,
                rv.NumeroRecibo, rv.DataVenda, rv.ValorTotal, rv.PesoTotalKg, rv.CriadoEm
            FROM RecibosVenda rv
            ORDER BY rv.DataVenda DESC;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<ReciboVenda>(sql);
    }

    public async Task<ReciboVenda?> GetByIdAsync(int id)
    {
        const string sqlRecibo = @"
            SELECT
                rv.Id, rv.ClienteNome, rv.ClienteCpf, rv.ClienteCnpj, rv.ClienteChavePix,
                rv.NumeroRecibo, rv.DataVenda, rv.ValorTotal, rv.PesoTotalKg, rv.CriadoEm
            FROM RecibosVenda rv
            WHERE rv.Id = @id;";

        const string sqlItens = @"
            SELECT
                rvi.Id, rvi.ReciboVendaId, rvi.ItemId, rvi.PesoKg, rvi.PrecoPorKg, rvi.Total,
                i.Nome AS ItemNome
            FROM RecibosVendaItens rvi
            JOIN Itens i ON i.Id = rvi.ItemId
            WHERE rvi.ReciboVendaId = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();

        var recibo = await connection.QueryFirstOrDefaultAsync<ReciboVenda>(sqlRecibo, new { id });
        if (recibo is null) return null;

        var itens = await connection.QueryAsync<ReciboVendaItem>(sqlItens, new { id });
        recibo.Itens = itens.ToList();

        return recibo;
    }

    public async Task<int> UpdateAsync(ReciboVenda recibo) =>
        throw new NotSupportedException("Recibos geralmente não são editados. Exclua e recrie se necessário.");

    public async Task<int> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM RecibosVenda WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, new { id });
    }
}
