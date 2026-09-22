using ControleMateriais.Data.Common;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace ControleMateriais.Data.Services;

public static class AuditService
{
    public static void Log(string tabela, string operacao, int registroId,
                           object? valorAntigo, object? valorNovo, string? usuario = null)
    {
        using var connection = new SqliteConnection(DbSettings.GetConnectionString());
        connection.Open();

        const string sql = @"
            INSERT INTO Auditoria (Tabela, Operacao, RegistroId, ValorAntigo, ValorNovo, Usuario, DataHora)
            VALUES (@tabela, @operacao, @registroId, @valorAntigo, @valorNovo, @usuario, @dataHora);";

        var parametros = new
        {
            tabela,
            operacao,
            registroId,
            valorAntigo = valorAntigo is null ? null : JsonSerializer.Serialize(valorAntigo),
            valorNovo = valorNovo is null ? null : JsonSerializer.Serialize(valorNovo),
            usuario,
            dataHora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        connection.Execute(sql, parametros);
    }
}
