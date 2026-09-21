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

    // Garante que o item exista na tabela (cria se necessário) e retorna o registro com o Id.
    // Usado ao lançar recibos, onde itens personalizados podem não estar no catálogo pré-cadastrado.
    public async Task<Item> GetOrCreateAsync(string nome)
    {
        const string insertSql = @"
            INSERT INTO Itens (Nome)
            VALUES (@nome)
            ON CONFLICT(Nome) DO NOTHING;";
        const string selectSql = "SELECT * FROM Itens WHERE Nome = @nome;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        await connection.ExecuteAsync(insertSql, new { nome });
        return await connection.QueryFirstAsync<Item>(selectSql, new { nome });
    }

    public async Task<int> InsertAsync(Item item)
    {
        const string sql = @"
            INSERT INTO Itens (Nome, Ordem, Ativo)
            VALUES (@Nome, @Ordem, @Ativo);
            SELECT last_insert_rowid();";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(sql, item);
    }

    public async Task<int> UpdateAsync(Item item)
    {
        const string sql = @"
            UPDATE Itens
            SET Nome = @Nome, Ordem = @Ordem, Ativo = @Ativo
            WHERE Id = @Id;";

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
