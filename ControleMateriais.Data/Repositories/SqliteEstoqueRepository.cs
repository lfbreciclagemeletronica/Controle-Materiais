using ControleMateriais.Data.Common;
using ControleMateriais.Data.Interfaces;
using ControleMateriais.Data.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ControleMateriais.Data.Repositories;

public class SqliteEstoqueRepository : IEstoqueRepository
{
    private SqliteConnection CreateConnection() =>
        new SqliteConnection(DbSettings.GetConnectionString());

    public async Task<IEnumerable<EstoqueView>> GetEstoqueAsync()
    {
        const string sql = @"
            SELECT
                ItemId,
                ItemNome,
                Ordem,
                TotalComprasKg,
                TotalVendasKg,
                SaldoKg
            FROM EstoqueView
            ORDER BY Ordem;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<EstoqueView>(sql);
    }
}
