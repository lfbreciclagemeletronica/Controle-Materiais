# Specification File: ControleMateriais.Data Class Library Implementation

**Project Name:** ControleMateriais.Data  
**Type:** Class Library (.NET 10.0)  
**Purpose:** Data access layer for SQLite database using Dapper and Repository Pattern  
**Target Framework:** net10.0

---

## Overview

This specification provides a complete, step-by-step guide for implementing the `ControleMateriais.Data` Class Library. This library will handle all database operations for the ControleMateriais application, including encrypted SQLite storage, CRUD operations, backup management, and audit logging.

---

## Prerequisites

- .NET 10.0 SDK installed
- SQLite3 installed on development machine (optional, for manual database inspection)
- Access to the ControleMateriais solution file
- Project `ControleMateriais.Desktop` must exist in the solution

---

## Implementation Steps

### Step 1: Create the Class Library Project

**Location:** Root of ControleMateriais solution (next to .slnx and .sln files)

**Commands:**
```bash
dotnet new classlib -n ControleMateriais.Data -o ControleMateriais.Data --framework net10.0
dotnet sln add ControleMateriais.Data/ControleMateriais.Data.csproj
dotnet add ControleMateriais.Desktop reference ControleMateriais.Data
```

**Expected Outcome:**
- New project `ControleMateriais.Data` created
- Project added to solution
- Reference from Desktop project to Data project established

---

### Step 2: Configure Project File (.csproj)

**File:** `ControleMateriais.Data/ControleMateriais.Data.csproj`

**Content:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.12" />
    <PackageReference Include="Dapper" Version="2.1.35" />
  </ItemGroup>

</Project>
```

**Package Descriptions:**
- `Microsoft.Data.Sqlite` (10.0.12): Official Microsoft ADO.NET driver for SQLite, including the bundled native provider
- `Dapper` (2.1.35): Micro-ORM for SQL execution and object mapping

---

### Step 3: Create Directory Structure

**Base Path:** `ControleMateriais.Data/`

**Directory Structure:**
```
ControleMateriais.Data/
├── Common/
├── Interfaces/
├── Models/
├── Repositories/
└── Services/
```

**Commands:**
```bash
cd ControleMateriais.Data
mkdir Common
mkdir Interfaces
mkdir Models
mkdir Repositories
mkdir Services
```

---

### Step 4: Implement Common/DbSettings.cs

**File:** `ControleMateriais.Data/Common/DbSettings.cs`

**Purpose:** Centralized database configuration including paths and connection string management

**Implementation:**
```csharp
using Microsoft.Data.Sqlite;

namespace ControleMateriais.Data.Common;

public static class DbSettings
{
    public static string DatabaseFolder =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "ControleMateriaisLFB",
            "banco-de-dados");

    public static string DatabasePath =>
        Path.Combine(DatabaseFolder, "database.db");

    public static string BackupFolder =>
        Path.Combine(DatabaseFolder, "backups");

    public static string LogFolder =>
        Path.Combine(DatabaseFolder, "logs");

    public static SqliteConnectionStringBuilder CreateConnectionString()
    {
        return new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };
    }

    public static string GetConnectionString() =>
        CreateConnectionString().ConnectionString;
}
```

**Key Features:**
- Database location in user's Downloads folder
- Separate folders for database, backups, and logs
- Connection string builder for the standard (unencrypted) SQLite provider

---

### Step 5: Implement Model Classes

#### 5.1: Models/Usuario.cs

**File:** `ControleMateriais.Data/Models/Usuario.cs`

```csharp
namespace ControleMateriais.Data.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Cnpj { get; set; }
    public string ChavePix { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public bool Ativo { get; set; }
}
```

#### 5.2: Models/Item.cs

**File:** `ControleMateriais.Data/Models/Item.cs`

```csharp
namespace ControleMateriais.Data.Models;

public class Item
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Ativo { get; set; }
}
```

#### 5.3: Models/ReciboCompra.cs

**File:** `ControleMateriais.Data/Models/ReciboCompra.cs`

```csharp
namespace ControleMateriais.Data.Models;

public class ReciboCompra
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public long? NumeroRecibo { get; set; }
    public string DataRecibo { get; set; } = string.Empty; // formato: 10/07/2026
    public decimal ValorTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public DateTime CriadoEm { get; set; }

    public List<ReciboCompraItem> Itens { get; set; } = new();
}
```

#### 5.4: Models/ReciboCompraItem.cs

**File:** `ControleMateriais.Data/Models/ReciboCompraItem.cs`

```csharp
namespace ControleMateriais.Data.Models;

public class ReciboCompraItem
{
    public int Id { get; set; }
    public int ReciboCompraId { get; set; }
    public int ItemId { get; set; }
    public decimal PesoKg { get; set; }
    public decimal PrecoPorKg { get; set; }
    public decimal Total => PesoKg * PrecoPorKg;

    public string ItemNome { get; set; } = string.Empty; // populated via JOIN
}
```

#### 5.5: Models/ReciboVenda.cs

**File:** `ControleMateriais.Data/Models/ReciboVenda.cs`

```csharp
namespace ControleMateriais.Data.Models;

public class ReciboVenda
{
    public int Id { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string? ClienteCpf { get; set; }
    public string? ClienteCnpj { get; set; }
    public string? ClienteChavePix { get; set; }
    public long? NumeroRecibo { get; set; }
    public string DataVenda { get; set; } = string.Empty; // formato: 10/07/2026
    public decimal ValorTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public DateTime CriadoEm { get; set; }

    public List<ReciboVendaItem> Itens { get; set; } = new();
}
```

#### 5.6: Models/ReciboVendaItem.cs

**File:** `ControleMateriais.Data/Models/ReciboVendaItem.cs`

```csharp
namespace ControleMateriais.Data.Models;

public class ReciboVendaItem
{
    public int Id { get; set; }
    public int ReciboVendaId { get; set; }
    public int ItemId { get; set; }
    public decimal PesoKg { get; set; }
    public decimal PrecoPorKg { get; set; }
    public decimal Total => PesoKg * PrecoPorKg;

    public string ItemNome { get; set; } = string.Empty; // populated via JOIN
}
```

#### 5.7: Models/EstoqueView.cs

**File:** `ControleMateriais.Data/Models/EstoqueView.cs`

```csharp
namespace ControleMateriais.Data.Models;

public class EstoqueView
{
    public int ItemId { get; set; }
    public string ItemNome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public decimal TotalComprasKg { get; set; }
    public decimal TotalVendasKg { get; set; }
    public decimal SaldoKg => TotalComprasKg - TotalVendasKg;
}
```

#### 5.8: Models/EstoqueInicial.cs

**File:** `ControleMateriais.Data/Models/EstoqueInicial.cs`

```csharp
namespace ControleMateriais.Data.Models;

public class EstoqueInicial
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public int Mes { get; set; }
    public int Ano { get; set; }
    public decimal PesoKg { get; set; }
    public DateTime CriadoEm { get; set; }
}
```

---

### Step 6: Implement Repository Interfaces

#### 6.1: Interfaces/IUsuarioRepository.cs

**File:** `ControleMateriais.Data/Interfaces/IUsuarioRepository.cs`

```csharp
using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<int> InsertAsync(Usuario usuario);
    Task<int> UpdateAsync(Usuario usuario);
    Task<int> DeleteAsync(int id);
}
```

#### 6.2: Interfaces/IItemRepository.cs

**File:** `ControleMateriais.Data/Interfaces/IItemRepository.cs`

```csharp
using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IItemRepository
{
    Task<IEnumerable<Item>> GetAllAsync();
    Task<Item?> GetByIdAsync(int id);
    Task<int> InsertAsync(Item item);
    Task<int> UpdateAsync(Item item);
    Task<int> DeleteAsync(int id);
}
```

#### 6.3: Interfaces/IReciboCompraRepository.cs

**File:** `ControleMateriais.Data/Interfaces/IReciboCompraRepository.cs`

```csharp
using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IReciboCompraRepository
{
    Task<IEnumerable<ReciboCompra>> GetAllAsync();
    Task<ReciboCompra?> GetByIdAsync(int id);
    Task<int> InsertAsync(ReciboCompra recibo);
    Task<int> UpdateAsync(ReciboCompra recibo);
    Task<int> DeleteAsync(int id);
}
```

#### 6.4: Interfaces/IReciboVendaRepository.cs

**File:** `ControleMateriais.Data/Interfaces/IReciboVendaRepository.cs`

```csharp
using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IReciboVendaRepository
{
    Task<IEnumerable<ReciboVenda>> GetAllAsync();
    Task<ReciboVenda?> GetByIdAsync(int id);
    Task<int> InsertAsync(ReciboVenda recibo);
    Task<int> UpdateAsync(ReciboVenda recibo);
    Task<int> DeleteAsync(int id);
}
```

#### 6.5: Interfaces/IEstoqueRepository.cs

**File:** `ControleMateriais.Data/Interfaces/IEstoqueRepository.cs`

```csharp
using ControleMateriais.Data.Models;

namespace ControleMateriais.Data.Interfaces;

public interface IEstoqueRepository
{
    Task<IEnumerable<EstoqueView>> GetEstoqueAsync();
    Task<IEnumerable<EstoqueView>> GetEstoqueInicialAsync(int mes, int ano);
    Task<int> InsertOrUpdateEstoqueInicialAsync(EstoqueInicial estoque);
}
```

---

### Step 7: Implement Repository Classes

#### 7.1: Repositories/SqliteUsuarioRepository.cs

**File:** `ControleMateriais.Data/Repositories/SqliteUsuarioRepository.cs`

```csharp
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
```

#### 7.2: Repositories/SqliteItemRepository.cs

**File:** `ControleMateriais.Data/Repositories/SqliteItemRepository.cs`

```csharp
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
```

#### 7.3: Repositories/SqliteReciboCompraRepository.cs

**File:** `ControleMateriais.Data/Repositories/SqliteReciboCompraRepository.cs`

```csharp
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

    public async Task<int> InsertAsync(ReciboCompra recibo)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            const string sqlRecibo = @"
                INSERT INTO RecibosCompra (UsuarioId, NumeroRecibo, DataRecibo, ValorTotal, PesoTotalKg)
                VALUES (@usuarioId, @numeroRecibo, @dataRecibo, @valorTotal, @pesoTotalKg);
                SELECT last_insert_rowid();";

            var parametrosRecibo = new
            {
                usuarioId = recibo.UsuarioId,
                numeroRecibo = recibo.NumeroRecibo,
                dataRecibo = recibo.DataRecibo,
                valorTotal = recibo.ValorTotal,
                pesoTotalKg = recibo.PesoTotalKg
            };

            var reciboId = await connection.ExecuteScalarAsync<int>(sqlRecibo, parametrosRecibo, transaction);

            const string sqlItem = @"
                INSERT INTO ReciboCompraItens (ReciboCompraId, ItemId, PesoKg, PrecoPorKg, Total)
                VALUES (@reciboCompraId, @itemId, @pesoKg, @precoPorKg, @total);";

            foreach (var item in recibo.Itens)
            {
                var parametrosItem = new
                {
                    reciboCompraId = reciboId,
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
                rc.Id, rc.UsuarioId, rc.NumeroRecibo, rc.DataRecibo, rc.ValorTotal, rc.PesoTotalKg, rc.CriadoEm,
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
                rc.Id, rc.UsuarioId, rc.NumeroRecibo, rc.DataRecibo, rc.ValorTotal, rc.PesoTotalKg, rc.CriadoEm
            FROM RecibosCompra rc
            WHERE rc.Id = @id;";

        const string sqlItens = @"
            SELECT
                rci.Id, rci.ReciboCompraId, rci.ItemId, rci.PesoKg, rci.PrecoPorKg, rci.Total,
                i.Nome AS ItemNome
            FROM ReciboCompraItens rci
            JOIN Itens i ON i.Id = rci.ItemId
            WHERE rci.ReciboCompraId = @id;";

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
}
```

#### 7.4: Repositories/SqliteReciboVendaRepository.cs

**File:** `ControleMateriais.Data/Repositories/SqliteReciboVendaRepository.cs`

```csharp
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
```

#### 7.5: Repositories/SqliteEstoqueRepository.cs

**File:** `ControleMateriais.Data/Repositories/SqliteEstoqueRepository.cs`

```csharp
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

    public async Task<IEnumerable<EstoqueView>> GetEstoqueInicialAsync(int mes, int ano)
    {
        const string sql = @"
            SELECT
                ItemId,
                ItemNome,
                Ordem,
                EstoqueInicialKg,
                TotalComprasKg,
                TotalVendasKg,
                SaldoKg
            FROM EstoqueInicialView
            ORDER BY Ordem;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<EstoqueView>(sql, new { mes, ano });
    }

    public async Task<int> InsertOrUpdateEstoqueInicialAsync(EstoqueInicial estoque)
    {
        const string sql = @"
            INSERT INTO EstoqueInicial (ItemId, Mes, Ano, PesoKg)
            VALUES (@itemId, @mes, @ano, @pesoKg)
            ON CONFLICT(ItemId, Mes, Ano) DO UPDATE SET
                PesoKg = excluded.PesoKg,
                CriadoEm = datetime('now');
            SELECT Id FROM EstoqueInicial WHERE ItemId = @itemId AND Mes = @mes AND Ano = @ano;";

        using var connection = CreateConnection();
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            itemId = estoque.ItemId,
            mes = estoque.Mes,
            ano = estoque.Ano,
            pesoKg = estoque.PesoKg
        });
    }
}
```

---

### Step 8: Implement Service Classes

#### 8.1: Services/DatabaseInitializer.cs

**File:** `ControleMateriais.Data/Services/DatabaseInitializer.cs`

```csharp
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

            CREATE TABLE IF NOT EXISTS Usuarios (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT NOT NULL,
                Cpf TEXT UNIQUE,
                Cnpj TEXT UNIQUE,
                ChavePix TEXT NOT NULL,
                CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),
                AtualizadoEm TEXT NOT NULL DEFAULT (datetime('now')),
                Ativo INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Itens (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT NOT NULL UNIQUE,
                Ordem INTEGER NOT NULL DEFAULT 0,
                Ativo INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS RecibosCompra (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UsuarioId INTEGER NOT NULL,
                NumeroRecibo INTEGER UNIQUE,
                DataRecibo TEXT NOT NULL,
                ValorTotal REAL NOT NULL DEFAULT 0,
                PesoTotalKg REAL NOT NULL DEFAULT 0,
                CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),

                FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
            );

            CREATE TABLE IF NOT EXISTS ReciboCompraItens (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ReciboCompraId INTEGER NOT NULL,
                ItemId INTEGER NOT NULL,
                PesoKg REAL NOT NULL CHECK (PesoKg >= 0),
                PrecoPorKg REAL NOT NULL DEFAULT 0,
                Total REAL NOT NULL DEFAULT 0,

                FOREIGN KEY (ReciboCompraId) REFERENCES RecibosCompra(Id) ON DELETE CASCADE,
                FOREIGN KEY (ItemId) REFERENCES Itens(Id)
            );

            CREATE TABLE IF NOT EXISTS RecibosVenda (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ClienteNome TEXT NOT NULL,
                ClienteCpf TEXT,
                ClienteCnpj TEXT,
                ClienteChavePix TEXT,
                NumeroRecibo INTEGER UNIQUE,
                DataVenda TEXT NOT NULL,
                ValorTotal REAL NOT NULL DEFAULT 0,
                PesoTotalKg REAL NOT NULL DEFAULT 0,
                CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),

                CHECK (
                    (ClienteCpf IS NOT NULL AND ClienteCnpj IS NULL) OR
                    (ClienteCpf IS NULL AND ClienteCnpj IS NOT NULL)
                )
            );

            CREATE TABLE IF NOT EXISTS RecibosVendaItens (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ReciboVendaId INTEGER NOT NULL,
                ItemId INTEGER NOT NULL,
                PesoKg REAL NOT NULL CHECK (PesoKg >= 0),
                PrecoPorKg REAL NOT NULL DEFAULT 0,
                Total REAL NOT NULL DEFAULT 0,

                FOREIGN KEY (ReciboVendaId) REFERENCES RecibosVenda(Id) ON DELETE CASCADE,
                FOREIGN KEY (ItemId) REFERENCES Itens(Id)
            );

            CREATE TABLE IF NOT EXISTS EstoqueInicial (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ItemId INTEGER NOT NULL,
                Mes INTEGER NOT NULL,
                Ano INTEGER NOT NULL,
                PesoKg REAL NOT NULL DEFAULT 0,
                CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),

                FOREIGN KEY (ItemId) REFERENCES Itens(Id),
                UNIQUE (ItemId, Mes, Ano)
            );

            CREATE VIEW IF NOT EXISTS EstoqueView AS
            SELECT
                i.Id AS ItemId,
                i.Nome AS ItemNome,
                i.Ordem,
                COALESCE(SUM(rci.PesoKg), 0) AS TotalComprasKg,
                COALESCE(SUM(rvi.PesoKg), 0) AS TotalVendasKg,
                COALESCE(SUM(rci.PesoKg), 0) - COALESCE(SUM(rvi.PesoKg), 0) AS SaldoKg
            FROM Itens i
            LEFT JOIN ReciboCompraItens rci ON rci.ItemId = i.Id
            LEFT JOIN RecibosVendaItens rvi ON rvi.ItemId = i.Id
            WHERE i.Ativo = 1
            GROUP BY i.Id, i.Nome, i.Ordem
            ORDER BY i.Ordem;

            CREATE VIEW IF NOT EXISTS EstoqueInicialView AS
            SELECT
                i.Id AS ItemId,
                i.Nome AS ItemNome,
                i.Ordem,
                COALESCE(ei.PesoKg, 0) AS EstoqueInicialKg,
                COALESCE(SUM(rci.PesoKg), 0) AS TotalComprasKg,
                COALESCE(SUM(rvi.PesoKg), 0) AS TotalVendasKg,
                COALESCE(ei.PesoKg, 0)
                    + COALESCE(SUM(rci.PesoKg), 0)
                    - COALESCE(SUM(rvi.PesoKg), 0) AS SaldoKg
            FROM Itens i
            LEFT JOIN EstoqueInicial ei ON ei.ItemId = i.Id
            LEFT JOIN ReciboCompraItens rci ON rci.ItemId = i.Id
            LEFT JOIN RecibosVendaItens rvi ON rvi.ItemId = i.Id
            WHERE i.Ativo = 1
            GROUP BY i.Id, i.Nome, i.Ordem, ei.PesoKg
            ORDER BY i.Ordem;

            CREATE TABLE IF NOT EXISTS Auditoria (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Tabela TEXT NOT NULL,
                Operacao TEXT NOT NULL,
                RegistroId INTEGER,
                ValorAntigo TEXT,
                ValorNovo TEXT,
                Usuario TEXT,
                DataHora TEXT NOT NULL
            );";

        connection.Execute(createTables);
        SeedItems(connection);
    }

    private static void SeedItems(SqliteConnection connection)
    {
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
```

#### 8.2: Services/BackupService.cs

**File:** `ControleMateriais.Data/Services/BackupService.cs`

```csharp
using ControleMateriais.Data.Common;

namespace ControleMateriais.Data.Services;

public static class BackupService
{
    private const int MaxBackups = 7;

    public static void CreateBackup()
    {
        Directory.CreateDirectory(DbSettings.BackupFolder);

        if (!File.Exists(DbSettings.DatabasePath))
            return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        string backupPath = Path.Combine(DbSettings.BackupFolder, $"database_{timestamp}.db");

        File.Copy(DbSettings.DatabasePath, backupPath, overwrite: true);

        CleanupOldBackups();
    }

    public static void RestoreBackup(string backupPath)
    {
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup não encontrado.", backupPath);

        File.Copy(backupPath, DbSettings.DatabasePath, overwrite: true);
    }

    public static IEnumerable<string> ListBackups()
    {
        Directory.CreateDirectory(DbSettings.BackupFolder);
        return Directory
            .GetFiles(DbSettings.BackupFolder, "database_*.db")
            .OrderByDescending(f => f)
            .ToList();
    }

    private static void CleanupOldBackups()
    {
        var backups = ListBackups().ToList();

        while (backups.Count > MaxBackups)
        {
            File.Delete(backups.Last());
            backups.RemoveAt(backups.Count - 1);
        }
    }
}
```

#### 8.3: Services/AuditService.cs

**File:** `ControleMateriais.Data/Services/AuditService.cs`

```csharp
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
```

---

### Step 9: Integration Example for Desktop Project

**Usage Example (to be implemented in ControleMateriais.Desktop):**

```csharp
using ControleMateriais.Data.Interfaces;
using ControleMateriais.Data.Models;
using ControleMateriais.Data.Repositories;
using ControleMateriais.Data.Services;

// Initialize database on application startup
DatabaseInitializer.EnsureDatabase();

// Create backup on application startup or shutdown
BackupService.CreateBackup();

// Create a user
IUsuarioRepository usuarioRepo = new SqliteUsuarioRepository();

var usuario = new Usuario
{
    Nome = "João da Silva",
    Cpf = "12345678900",
    ChavePix = "joao@email.com",
    CriadoEm = DateTime.Now,
    AtualizadoEm = DateTime.Now,
    Ativo = true
};

int usuarioId = await usuarioRepo.InsertAsync(usuario);

// Insert a purchase receipt
IReciboCompraRepository reciboRepo = new SqliteReciboCompraRepository();

var recibo = new ReciboCompra
{
    UsuarioId = usuarioId,
    NumeroRecibo = 10072026083022,
    DataRecibo = "10/07/2026",
    ValorTotal = 3.45m,
    PesoTotalKg = 1.5m,
    CriadoEm = DateTime.Now,
    Itens = new List<ReciboCompraItem>
    {
        new ReciboCompraItem
        {
            ItemId = 1, // Placa Drive
            PesoKg = 1.5m,
            PrecoPorKg = 2.30m
        }
    }
};

int reciboId = await reciboRepo.InsertAsync(recibo);

// Query stock
IEstoqueRepository estoqueRepo = new SqliteEstoqueRepository();
var estoque = await estoqueRepo.GetEstoqueAsync();

foreach (var item in estoque)
{
    Console.WriteLine($"{item.ItemNome}: Saldo = {item.SaldoKg} kg");
}
```

---

## Security Considerations

### Critical Security Requirements:

1. **Database Key Management**
   - NEVER hardcode the database key in production
   - Use environment variables, Azure Key Vault, or secure configuration
   - Current implementation uses placeholder key for development only

2. **SQL Injection Prevention**
   - ALL SQL queries MUST use parameterized queries
   - NEVER concatenate user input into SQL strings
   - Dapper automatically handles parameterization when used correctly

3. **File System Security**
   - Restrict database folder permissions to application user only
   - Implement proper file access controls in production

4. **Audit Logging**
   - Log all sensitive operations (INSERT, UPDATE, DELETE)
   - Include user context, timestamp, and data changes
   - Store audit logs in secure, tamper-evident storage

5. **Backup Security**
   - Encrypt backups if stored in insecure locations
   - Implement backup retention policies
   - Regular backup restoration testing

---

## Database Schema Summary

### Tables:
1. **Usuarios** - User management with CPF/CNPJ validation
2. **Itens** - Material catalog with ordering
3. **RecibosCompra** - Purchase receipts header
4. **ReciboCompraItens** - Purchase receipt line items
5. **RecibosVenda** - Sales receipts header
6. **RecibosVendaItens** - Sales receipt line items
7. **EstoqueInicial** - Initial stock by month/year
8. **Auditoria** - Audit trail for all operations

### Views:
1. **EstoqueView** - Current stock calculation (purchases - sales)
2. **EstoqueInicialView** - Stock with initial balance included

### Constraints:
- Foreign keys with CASCADE delete for receipt items
- CHECK constraints for CPF/CNPJ mutual exclusivity
- UNIQUE constraints for critical fields
- Non-negative weight constraints

---

## Testing Recommendations

### Unit Tests:
- Test each repository method with mock data
- Verify SQL parameterization prevents injection
- Test transaction rollback on errors

### Integration Tests:
- Test database initialization
- Verify backup creation and restoration
- Test audit logging functionality
- Validate view calculations

### Security Tests:
- Test encryption with wrong password (should fail)
- Verify SQL injection attempts are blocked
- Test file permission restrictions

---

## Build and Verification

### Build Commands:
```bash
cd ControleMateriais.Data
dotnet build
```

### Verification Steps:
1. Build succeeds without errors
2. All references resolve correctly
3. Package versions are compatible
4. Code compiles with nullable reference types enabled

---

## Final Project Structure

```
ControleMateriais/
├── ControleMateriais.slnx
├── ControleMateriais.Desktop/
│   ├── ControleMateriais.Desktop.csproj
│   └── ...
└── ControleMateriais.Data/
    ├── ControleMateriais.Data.csproj
    ├── Common/
    │   └── DbSettings.cs
    ├── Interfaces/
    │   ├── IUsuarioRepository.cs
    │   ├── IItemRepository.cs
    │   ├── IReciboCompraRepository.cs
    │   ├── IReciboVendaRepository.cs
    │   └── IEstoqueRepository.cs
    ├── Models/
    │   ├── Usuario.cs
    │   ├── Item.cs
    │   ├── ReciboCompra.cs
    │   ├── ReciboCompraItem.cs
    │   ├── ReciboVenda.cs
    │   ├── ReciboVendaItem.cs
    │   ├── EstoqueView.cs
    │   └── EstoqueInicial.cs
    ├── Repositories/
    │   ├── SqliteUsuarioRepository.cs
    │   ├── SqliteItemRepository.cs
    │   ├── SqliteReciboCompraRepository.cs
    │   ├── SqliteReciboVendaRepository.cs
    │   └── SqliteEstoqueRepository.cs
    └── Services/
        ├── DatabaseInitializer.cs
        ├── BackupService.cs
        └── AuditService.cs
```

---

## Data Storage Structure

```
%USERPROFILE%\Downloads\ControleMateriaisLFB\banco-de-dados/
├── database.db
├── backups/
│   ├── database_2026-07-10_080000.db
│   └── database_2026-07-09_080000.db
└── logs/
    └── auditoria_2026-07.log
```

---

## Implementation Checklist

- [ ] Create Class Library project
- [ ] Add project to solution
- [ ] Add reference from Desktop to Data project
- [ ] Install NuGet packages
- [ ] Create directory structure
- [ ] Implement DbSettings.cs
- [ ] Implement all Model classes
- [ ] Implement all Repository interfaces
- [ ] Implement all Repository classes
- [ ] Implement DatabaseInitializer.cs
- [ ] Implement BackupService.cs
- [ ] Implement AuditService.cs
- [ ] Build and verify compilation
- [ ] Test database initialization
- [ ] Test backup creation
- [ ] Test repository operations
- [ ] Update Desktop project to use new library

---

## Notes for AI Implementation

1. **Follow the exact namespace structure** specified in this document
2. **Implement all files in the exact order** specified to avoid dependency issues
3. **Use the exact code provided** - do not deviate from the specifications
4. **Ensure all using statements** match the provided code
5. **Verify package versions** are compatible with .NET 10.0
6. **Test compilation** after each major step
7. **Do not skip any files** - all components are required for the system to function
8. **Pay special attention to security notes** - implement secure practices from the start

---

**End of Specification**
