using ControleMateriais.Data.Common;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ControleMateriais.Data.Services;

public static class DatabaseInitializer
{
    public static void EnsureDatabase()
    {
        Directory.CreateDirectory(DbSettings.DatabaseFolder);
        Directory.CreateDirectory(DbSettings.BackupFolder);
        Directory.CreateDirectory(DbSettings.LogFolder);

        using var connection = new SqliteConnection(DbSettings.GetConnectionString());
        connection.Open();

        const string createTables = @"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Itens (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT NOT NULL UNIQUE,
                Ordem INTEGER NOT NULL DEFAULT 0,
                Ativo INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Usuarios (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT NOT NULL UNIQUE,
                Cpf TEXT UNIQUE,
                Cnpj TEXT UNIQUE,
                ChavePix TEXT,
                CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),
                AtualizadoEm TEXT NOT NULL DEFAULT (datetime('now')),
                Ativo INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS RecibosCompra (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UsuarioId INTEGER NOT NULL,
                DataRecibo TEXT NOT NULL,
                CodigoRecibo TEXT NOT NULL,
                ValorTotal REAL NOT NULL DEFAULT 0,
                PesoTotalKg REAL NOT NULL DEFAULT 0,
                CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),

                FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
            );

            CREATE TABLE IF NOT EXISTS ReciboCompraItens (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ReciboCodigo TEXT NOT NULL,
                ItemId INTEGER NOT NULL,
                PesoKg REAL NOT NULL CHECK (PesoKg >= 0),
                PrecoPorKg REAL NOT NULL DEFAULT 0,
                Total REAL NOT NULL DEFAULT 0,

                FOREIGN KEY (ReciboCodigo) REFERENCES RecibosCompra(CodigoRecibo) ON DELETE CASCADE,
                FOREIGN KEY (ItemId) REFERENCES Itens(Id)
            );

            -- SQLite exige que a coluna referenciada por uma FOREIGN KEY seja PRIMARY KEY
            -- ou tenha um indice UNIQUE. Sem isso, o INSERT falha com erro de foreign key mismatch.
            CREATE UNIQUE INDEX IF NOT EXISTS UX_RecibosCompra_CodigoRecibo ON RecibosCompra(CodigoRecibo);

            CREATE TABLE IF NOT EXISTS RecibosVenda (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ClienteNome TEXT NOT NULL,
                DataVenda TEXT NOT NULL,
                CodigoRecibo TEXT NOT NULL,
                ValorTotal REAL NOT NULL DEFAULT 0,
                PesoTotalKg REAL NOT NULL DEFAULT 0,
                CriadoEm TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE UNIQUE INDEX IF NOT EXISTS UX_RecibosVenda_CodigoRecibo ON RecibosVenda(CodigoRecibo);

            CREATE TABLE IF NOT EXISTS RecibosVendaItens (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ReciboCodigo TEXT NOT NULL,
                ItemId INTEGER NOT NULL,
                PesoKg REAL NOT NULL CHECK (PesoKg >= 0),
                PrecoPorKg REAL NOT NULL DEFAULT 0,
                Total REAL NOT NULL DEFAULT 0,

                FOREIGN KEY (ReciboCodigo) REFERENCES RecibosVenda(CodigoRecibo) ON DELETE CASCADE,
                FOREIGN KEY (ItemId) REFERENCES Itens(Id)
            );

            CREATE VIEW IF NOT EXISTS EstoqueView AS
            SELECT
                i.Id AS ItemId,
                i.Nome AS ItemNome,
                i.Ordem,
                COALESCE(SUM(rci.PesoKg), 0) AS TotalComprasKg,
                0 AS TotalVendasKg,
                COALESCE(SUM(rci.PesoKg), 0) AS SaldoKg
            FROM Itens i
            LEFT JOIN ReciboCompraItens rci ON rci.ItemId = i.Id
            WHERE i.Ativo = 1
            GROUP BY i.Id, i.Nome, i.Ordem
            ORDER BY i.Ordem;";

        connection.Execute(createTables);
        MigrarEsquemaExistente(connection);
        SeedItemsIfEmpty(connection);
    }

    /// <summary>
    /// Corrige bancos de dados criados antes da correção do esquema de RecibosCompra/ReciboCompraItens:
    /// - Garante o índice único em RecibosCompra.CodigoRecibo (exigido pelo SQLite para a FOREIGN KEY).
    /// - Corrige o tipo da coluna ReciboCompraItens.ReciboCodigo de INTEGER para TEXT (igual a CodigoRecibo),
    ///   já que "CREATE TABLE IF NOT EXISTS" não altera tabelas já existentes.
    /// </summary>
    private static void MigrarEsquemaExistente(SqliteConnection connection)
    {
        IEnumerable<dynamic> colunas = connection.Query("PRAGMA table_info(ReciboCompraItens);");
        dynamic? colunaReciboCodigo = colunas.FirstOrDefault(c =>
            string.Equals((string)c.name, "ReciboCodigo", StringComparison.OrdinalIgnoreCase));

        if (colunaReciboCodigo is null) return; // tabela nova, já criada com o tipo correto
        if (!string.Equals((string)colunaReciboCodigo.type, "INTEGER", StringComparison.OrdinalIgnoreCase)) return;

        using var transaction = connection.BeginTransaction();
        try
        {
            connection.Execute("ALTER TABLE ReciboCompraItens RENAME TO ReciboCompraItens_old;", transaction: transaction);

            const string recriarTabela = @"
                CREATE TABLE ReciboCompraItens (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ReciboCodigo TEXT NOT NULL,
                    ItemId INTEGER NOT NULL,
                    PesoKg REAL NOT NULL CHECK (PesoKg >= 0),
                    PrecoPorKg REAL NOT NULL DEFAULT 0,
                    Total REAL NOT NULL DEFAULT 0,

                    FOREIGN KEY (ReciboCodigo) REFERENCES RecibosCompra(CodigoRecibo) ON DELETE CASCADE,
                    FOREIGN KEY (ItemId) REFERENCES Itens(Id)
                );";
            connection.Execute(recriarTabela, transaction: transaction);

            const string copiarDados = @"
                INSERT INTO ReciboCompraItens (Id, ReciboCodigo, ItemId, PesoKg, PrecoPorKg, Total)
                SELECT Id, CAST(ReciboCodigo AS TEXT), ItemId, PesoKg, PrecoPorKg, Total
                FROM ReciboCompraItens_old;";
            connection.Execute(copiarDados, transaction: transaction);

            connection.Execute("DROP TABLE ReciboCompraItens_old;", transaction: transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void SeedItemsIfEmpty(SqliteConnection connection)
    {
        const string checkSql = "SELECT COUNT(*) FROM Itens;";
        var count = connection.ExecuteScalar<int>(checkSql);

        if (count > 0) return; // Já tem dados, não faz seed

        const string sql = @"
            INSERT INTO Itens (Nome, Ordem) VALUES
            ('Placa Drive', 1),
            ('Placa Notebook A', 2),
            ('Placa Notebook B', 3),
            ('Placa Notebook C', 4),
            ('Placa Mãe A', 5),
            ('Placa Mãe B', 6),
            ('Placa Mãe C', 7),
            ('Placa Mãe D', 8),
            ('Placa de Servidor', 9),
            ('Placa Leve Especial', 10),
            ('Placa Leve Especial com Ponta', 11),
            ('Placa Leve Especial Completa', 12),
            ('Placa Dourada A', 13),
            ('Placa Dourada B', 14),
            ('Placa Tapete A', 15),
            ('Placa Tapete B', 16),
            ('Placa Conectora', 17),
            ('Placa Leve', 18),
            ('Placa Leve com Ponta', 19),
            ('Placa Intermediária A', 20),
            ('Placa Intermediária B', 21),
            ('Placa Intermediária C', 22),
            ('Placa Pesada', 23),
            ('Placa Pesada com Ponta', 24),
            ('Placa Tablet', 25),
            ('Placa Marrom', 26),
            ('HD Completo', 27),
            ('HD sem placa/Sucateado', 28),
            ('Placa de HD', 29),
            ('Placa de Celular Completa', 30),
            ('Celular Sem tampa e bateria (Botão e Flip)', 31),
            ('Celular Smart sem tampa e Sem bateria', 32),
            ('Celular Smart Com Bateria Blindada', 33),
            ('Celular Replicas blindado e sem Baterias', 34),
            ('Memórias Douradas', 35),
            ('Memória Prata', 36),
            ('Processadores Plástico Chapa A', 37),
            ('Processadores Plástico Chapa B', 38),
            ('Processadores Plástico', 39),
            ('Processadores Slot', 40),
            ('Processadores Plástico Preto', 41),
            ('Processadores Cerâmico A', 42),
            ('Processadores Cerâmico B', 43),
            ('Processadores Cerâmico C', 44),
            ('Baterias de Notebook', 45),
            ('Baterias de Tablet', 46),
            ('Baterias de Celular', 47),
            ('Fonte completa', 48),
            ('Raio X', 49),
            ('Desmanche Eletrônicos Consultar', 50)
            ON CONFLICT(Nome) DO NOTHING;";

        connection.Execute(sql);
    }
}
