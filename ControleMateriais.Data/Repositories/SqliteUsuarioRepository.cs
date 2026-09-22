using ControleMateriais.Data.Common;
using ControleMateriais.Data.Interfaces;
using ControleMateriais.Data.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ControleMateriais.Data.Repositories;

public class SqliteUsuarioRepository : IUsuarioRepository
{
    private SqliteConnection CreateConnection() =>
        new SqliteConnection(DbSettings.GetConnectionString());

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Usuarios WHERE Ativo = 1 ORDER BY Nome;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<Usuario>(sql);
    }

    public async Task<IEnumerable<Usuario>> GetTodosAsync()
    {
        const string sql = "SELECT * FROM Usuarios ORDER BY Nome;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<Usuario>(sql);
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Usuarios WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryFirstOrDefaultAsync<Usuario>(sql, new { id });
    }

    // Garante que o fornecedor exista na tabela (cria se necessário, sem Cpf/Cnpj/ChavePix)
    // e retorna o registro com o Id. Usado ao lançar recibos com fornecedores ainda não cadastrados.
    public async Task<Usuario> GetOrCreateAsync(string nome)
    {
        const string insertSql = @"
            INSERT INTO Usuarios (Nome, Cpf, Cnpj, ChavePix)
            VALUES (@nome, NULL, NULL, NULL)
            ON CONFLICT(Nome) DO NOTHING;";
        const string selectSql = "SELECT * FROM Usuarios WHERE Nome = @nome;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        await connection.ExecuteAsync(insertSql, new { nome });
        return await connection.QueryFirstAsync<Usuario>(selectSql, new { nome });
    }

    public async Task<int> InsertAsync(Usuario usuario)
    {
        const string sql = @"
            INSERT INTO Usuarios (Nome, Cpf, Cnpj, ChavePix, CriadoEm, AtualizadoEm, Ativo)
            VALUES (@Nome, @Cpf, @Cnpj, @ChavePix, @CriadoEm, @AtualizadoEm, @Ativo);
            SELECT last_insert_rowid();";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(sql, usuario);
    }

    public async Task<int> UpdateAsync(Usuario usuario)
    {
        const string sql = @"
            UPDATE Usuarios
            SET Nome = @Nome, Cpf = @Cpf, Cnpj = @Cnpj, ChavePix = @ChavePix,
                AtualizadoEm = @AtualizadoEm, Ativo = @Ativo
            WHERE Id = @Id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, usuario);
    }

    public async Task<int> DeleteAsync(int id)
    {
        const string sql = "UPDATE Usuarios SET Ativo = 0, AtualizadoEm = @atualizadoEm WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, new { id, atualizadoEm = DateTime.Now });
    }

    public async Task<int> ReativarAsync(int id)
    {
        const string sql = "UPDATE Usuarios SET Ativo = 1, AtualizadoEm = @atualizadoEm WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, new { id, atualizadoEm = DateTime.Now });
    }
}
