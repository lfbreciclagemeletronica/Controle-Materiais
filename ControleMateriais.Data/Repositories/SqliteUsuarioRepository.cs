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

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Usuarios WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryFirstOrDefaultAsync<Usuario>(sql, new { id });
    }

    public async Task<int> InsertAsync(Usuario usuario)
    {
        const string sql = @"
            INSERT INTO Usuarios (Nome, Cpf, Cnpj, ChavePix, CriadoEm, AtualizadoEm, Ativo)
            VALUES (@nome, @cpf, @cnpj, @chavePix, @criadoEm, @atualizadoEm, @ativo);
            SELECT last_insert_rowid();";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(sql, usuario);
    }

    public async Task<int> UpdateAsync(Usuario usuario)
    {
        const string sql = @"
            UPDATE Usuarios
            SET Nome = @nome, Cpf = @cpf, Cnpj = @cnpj, ChavePix = @chavePix,
                AtualizadoEm = @atualizadoEm, Ativo = @ativo
            WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, usuario);
    }

    public async Task<int> DeleteAsync(int id)
    {
        const string sql = "UPDATE Usuarios SET Ativo = 0 WHERE Id = @id;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, new { id });
    }
}
