using ControleMateriais.Data.Common;
using ControleMateriais.Data.Interfaces;
using ControleMateriais.Data.Models;
using ControleMateriais.Data.Repositories;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ControleMateriais.Data.Services;

public static class TestDatabaseRead
{
    public static void Test()
    {
        Console.WriteLine($"Database Path: {DbSettings.DatabasePath}");
        Console.WriteLine($"Database Exists: {File.Exists(DbSettings.DatabasePath)}");

        if (!File.Exists(DbSettings.DatabasePath))
        {
            Console.WriteLine("Database file not found!");
            return;
        }

        try
        {
            using var connection = new SqliteConnection(DbSettings.GetConnectionString());
            connection.Open();

            // Testar tabelas
            const string checkTables = @"
                SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
            var tables = connection.Query<string>(checkTables);
            Console.WriteLine("\nTables in database:");
            foreach (var table in tables)
            {
                Console.WriteLine($"  - {table}");
            }

            // Testar Itens
            const string countItens = "SELECT COUNT(*) FROM Itens;";
            var itensCount = connection.ExecuteScalar<int>(countItens);
            Console.WriteLine($"\nItens count: {itensCount}");

            if (itensCount > 0)
            {
                const string selectItens = "SELECT * FROM Itens LIMIT 5;";
                var itens = connection.Query<Item>(selectItens);
                Console.WriteLine("Sample itens:");
                foreach (var item in itens)
                {
                    Console.WriteLine($"  - {item.Id}: {item.Nome} (Ordem: {item.Ordem}, Ativo: {item.Ativo})");
                }
            }

            // Testar Usuarios
            const string countUsuarios = "SELECT COUNT(*) FROM Usuarios;";
            var usuariosCount = connection.ExecuteScalar<int>(countUsuarios);
            Console.WriteLine($"\nUsuarios count: {usuariosCount}");

            if (usuariosCount > 0)
            {
                const string selectUsuarios = "SELECT * FROM Usuarios LIMIT 5;";
                var usuarios = connection.Query<Usuario>(selectUsuarios);
                Console.WriteLine("Sample usuarios:");
                foreach (var usuario in usuarios)
                {
                    Console.WriteLine($"  - {usuario.Id}: {usuario.Nome} (CPF: {usuario.Cpf}, Ativo: {usuario.Ativo})");
                }
            }

            // Testar RecibosCompra
            const string countRecibos = "SELECT COUNT(*) FROM RecibosCompra;";
            var recibosCount = connection.ExecuteScalar<int>(countRecibos);
            Console.WriteLine($"\nRecibosCompra count: {recibosCount}");

            if (recibosCount > 0)
            {
                const string selectRecibos = "SELECT * FROM RecibosCompra LIMIT 5;";
                var recibos = connection.Query<ReciboCompra>(selectRecibos);
                Console.WriteLine("Sample recibos:");
                foreach (var recibo in recibos)
                {
                    Console.WriteLine($"  - {recibo.Id}: UsuarioId={recibo.UsuarioId}, CodigoRecibo={recibo.CodigoRecibo}, Data={recibo.DataRecibo}");
                }
            }

            // Testar ReciboCompraItens
            const string countReciboItens = "SELECT COUNT(*) FROM ReciboCompraItens;";
            var reciboItensCount = connection.ExecuteScalar<int>(countReciboItens);
            Console.WriteLine($"\nReciboCompraItens count: {reciboItensCount}");

            // Testar repositórios
            Console.WriteLine("\n--- Testing Repositories ---");
            IItemRepository itemRepo = new SqliteItemRepository();
            var allItens = itemRepo.GetAllAsync().GetAwaiter().GetResult();
            Console.WriteLine($"Repository returned {allItens.Count()} itens");

            IUsuarioRepository usuarioRepo = new SqliteUsuarioRepository();
            var allUsuarios = usuarioRepo.GetAllAsync().GetAwaiter().GetResult();
            Console.WriteLine($"Repository returned {allUsuarios.Count()} usuarios");

            IReciboCompraRepository reciboRepo = new SqliteReciboCompraRepository();
            var allRecibos = reciboRepo.GetAllAsync().GetAwaiter().GetResult();
            Console.WriteLine($"Repository returned {allRecibos.Count()} recibos");

            Console.WriteLine("\n✓ Database read test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
