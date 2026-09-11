using ControleMateriais.Data.Common;
using ControleMateriais.Data.Interfaces;
using ControleMateriais.Data.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ControleMateriais.Data.Repositories;

public class SqliteItemRepository : IItemRepository
{
    private SqliteConnection CreateConnection() =>
        new SqliteConnection(DbSettings.GetConnectionString());

    public async Task<IEnumerable<Item>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Itens WHERE Ativo = 1 ORDER BY Ordem;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<Item>(sql);
    }

    public async Task<Item?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Itens WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryFirstOrDefaultAsync<Item>(sql, new { id });
    }

    public async Task<int> InsertAsync(Item item)
    {
        const string sql = @"
            INSERT INTO Itens (Nome, Ordem, Ativo)
            VALUES (@nome, @ordem, @ativo);
            SELECT last_insert_rowid();";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(sql, item);
    }

    public async Task<int> UpdateAsync(Item item)
    {
        const string sql = @"
            UPDATE Itens
            SET Nome = @nome, Ordem = @ordem, Ativo = @ativo
            WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, item);
    }

    public async Task<int> DeleteAsync(int id)
    {
        const string sql = "UPDATE Itens SET Ativo = 0 WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, new { id });
    }
}
