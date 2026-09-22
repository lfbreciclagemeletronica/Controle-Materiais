# Criando o Class Library `ControleMateriais.Data` para SQLite

Guia passo a passo para adicionar um projeto **Class Library** à solution `ControleMateriais`, responsável por acessar um banco de dados **SQLite**, usando **Dapper** e **Repository Pattern**.

---

## 1. O que é um Class Library e por que usar

Uma **Class Library** (`.csproj` do tipo `classlib`) é um projeto que contém código reutilizável, mas não possui um ponto de entrada (`Main`). Ele gera um arquivo `.dll` que pode ser referenciado por outros projetos.

No nosso caso:

- `ControleMateriais.Desktop` é o aplicativo com interface gráfica (Avalonia).
- `ControleMateriais.Data` será o Class Library que cuida de tudo relacionado ao banco de dados.

Separar as responsabilidades deixa o código mais organizado, facilita testes e permite que outras aplicações (como uma futura API) reutilizem a mesma camada de dados.

---

## 2. Instalar e configurar o SQLite no computador

O SQLite é um banco de dados leve que armazena tudo em um único arquivo `.db` ou `.sqlite`. O .NET precisa apenas do driver (`Microsoft.Data.Sqlite`) para se comunicar com ele, mas ter o `sqlite3` instalado no computador ajuda a inspecionar o banco pelo terminal.

### Opção A — Instalar usando `winget` (Windows)

Abra o PowerShell e execute:

```powershell
winget install SQLite.SQLite
```

Feche e reabra o terminal para atualizar o `PATH`.

### Opção B — Baixar diretamente do site oficial

1. Acesse: https://www.sqlite.org/download.html
2. Baixe a versão **Precompiled Binaries for Windows**.
3. Extraia o arquivo `sqlite3.exe` e coloque em uma pasta (ex: `C:\Tools\sqlite`).
4. Adicione essa pasta à variável de ambiente `PATH`.

### Verificar a instalação

```bash
sqlite3 --version
```

Saída esperada:

```text
3.46.0 2024-05-23 13:25:27 ...
```

---

## 3. Criar o Class Library e adicionar à Solution

Abra o terminal na pasta raiz `ControleMateriais/` (ao lado do arquivo `.slnx` e `.sln`).

Crie o projeto Class Library:

```bash
dotnet new classlib -n ControleMateriais.Data -o ControleMateriais.Data --framework net10.0
```

Adicione o projeto à solution:

```bash
dotnet sln add ControleMateriais.Data/ControleMateriais.Data.csproj
```

Adicione a referência do Class Library no projeto Desktop:

```bash
dotnet add ControleMateriais.Desktop reference ControleMateriais.Data
```

A partir de agora, o projeto `ControleMateriais.Desktop` pode usar as classes públicas do `ControleMateriais.Data`.

---

## 4. Instalar os pacotes NuGet no Class Library

Navegue até a pasta do Class Library:

```bash
cd ControleMateriais.Data
```

Instale os pacotes:

```bash
dotnet add package Microsoft.Data.Sqlite --version 10.0.12
dotnet add package Dapper --version 2.1.35
```

### Para que serve cada pacote?

- **Microsoft.Data.Sqlite**: driver ADO.NET oficial da Microsoft para SQLite, já inclui o provider nativo embutido (`SQLitePCLRaw.bundle_e_sqlite3`).
- **Dapper**: micro-ORM que facilita executar SQL e mapear resultados para objetos C#.

> **Dica:** Sempre verifique se as versões são compatíveis com o .NET 10 e se não há vulnerabilidades conhecidas na dependência transitiva `SQLitePCLRaw.lib.e_sqlite3` (rode `dotnet list package --include-transitive` para checar).

---

## 5. Estrutura de diretórios do Class Library

A estrutura a seguir organiza o código de forma limpa e facilita futura exportação do relatório para PDF. Cada pasta tem uma responsabilidade única.

```text
ControleMateriais.Data/
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

### Explicação das pastas

- **Common**: configurações compartilhadas (caminho do banco, connection string).
- **Interfaces**: contratos (interfaces) que o projeto Desktop conhece.
- **Models**: classes que representam as tabelas do banco.
- **Repositories**: implementações reais das consultas SQL.
- **Services**: operações auxiliares (criar tabelas, backup, auditoria).

### Estrutura de dados no disco

Além do código, o banco de dados e seus backups devem ficar organizados para facilitar auditoria e exportação. Exemplo de estrutura de pastas:

```text
%USERPROFILE%\Downloads\ControleMateriaisLFB\banco-de-dados/
├── database.db
├── backups/
│   ├── database_2026-07-10_080000.db
│   └── database_2026-07-09_080000.db
└── logs/
    └── auditoria_2026-07.log
```

Essa organização deixa claro onde fica o banco, onde estão os backups e onde ficam os registros de auditoria. O app pode gerar um resumo PDF com essa estrutura posteriormente.

---

## 6. O que é Repository Pattern

**Repository Pattern** é um padrão de projeto que isola a lógica de acesso a dados do restante do aplicativo. Ele funciona assim:

1. O projeto Desktop conhece apenas a **interface** (`IMaterialRepository`).
2. O Class Library fornece a **implementação** (`SqliteMaterialRepository`).
3. Se um dia precisar trocar o SQLite por outro banco, basta criar uma nova implementação da interface, sem alterar o Desktop.

Isso facilita testes, manutenção e troca de tecnologia.

---

## 7. Configurar o acesso ao banco

O banco não é criptografado — trata-se de um arquivo SQLite padrão, acessível por qualquer ferramenta compatível.

### `Common/DbSettings.cs`

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

### O que esse código faz?

- Define os caminhos do banco, backups e logs.
- Cria uma string de conexão usando o provider padrão (não criptografado) do SQLite.

---

## 8. Configurar o Dapper para consultas seguras

Dapper é um micro-ORM. Ele permite escrever SQL direto e mapear os resultados para classes C#. O grande benefício é a simplicidade e o desempenho.

### Por que Dapper evita SQL Injection?

Quando você usa **parâmetros** (`@nome`, `@preco`), o Dapper envia os valores separados do comando SQL. O banco trata os valores como dados, não como parte do comando. Nunca concatene strings diretamente no SQL.

### Exemplo inseguro (não fazer)

```csharp
// ERRADO! Vulnerável a SQL Injection
string sql = $"SELECT * FROM Itens WHERE Nome = '{nome}'";
```

### Exemplo seguro (fazer)

```csharp
// CORRETO! Usando parâmetros
string sql = "SELECT * FROM Itens WHERE Nome = @nome";
var resultado = connection.Query<Item>(sql, new { nome });
```

---

## 9. Criar as models e as interfaces

A partir das tabelas definidas em `Tabelas_Dados.md`, crie as classes de modelo no C#.

### `Models/Usuario.cs`

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

### `Models/Item.cs`

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

### `Models/ReciboCompra.cs`

```csharp
namespace ControleMateriais.Data.Models;

public class ReciboCompra
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public long? NumeroRecibo { get; set; }
    public string DataRecibo { get; set; } = string.Empty; // formato 10/07/2026
    public decimal ValorTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public DateTime CriadoEm { get; set; }

    public List<ReciboCompraItem> Itens { get; set; } = new();
}
```

### `Models/ReciboCompraItem.cs`

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

    public string ItemNome { get; set; } = string.Empty; // preenchido via JOIN
}
```

### `Models/ReciboVenda.cs`

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
    public string DataVenda { get; set; } = string.Empty; // formato 10/07/2026
    public decimal ValorTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public DateTime CriadoEm { get; set; }

    public List<ReciboVendaItem> Itens { get; set; } = new();
}
```

### `Models/ReciboVendaItem.cs`

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

    public string ItemNome { get; set; } = string.Empty; // preenchido via JOIN
}
```

### `Models/EstoqueView.cs`

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

### `Models/EstoqueInicial.cs`

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

### Interfaces

Crie interfaces separadas para cada repositório. O projeto `Desktop` depende apenas das interfaces, não das implementações SQLite.

### `Interfaces/IUsuarioRepository.cs`

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

### `Interfaces/IItemRepository.cs`

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

### `Interfaces/IReciboCompraRepository.cs`

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

### `Interfaces/IReciboVendaRepository.cs`

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

### `Interfaces/IEstoqueRepository.cs`

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

As interfaces definem os contratos. O projeto Desktop conhece apenas elas.

---

## 10. Implementar os Repositories

A seguir estão exemplos de implementação dos repositórios. Os repositórios de `Usuario` e `Item` seguem o mesmo padrão CRUD. Aqui focamos em `ReciboCompra` e `Estoque` por serem mais complexos.

### `Repositories/SqliteReciboCompraRepository.cs`

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

### `Repositories/SqliteEstoqueRepository.cs`

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

### O que esse código faz?

- `SqliteReciboCompraRepository` insere um recibo e seus itens dentro de uma transação.
- `SqliteEstoqueRepository` consulta as `VIEW`s `EstoqueView` e `EstoqueInicialView`.
- Sempre usa **parâmetros** (`@id`, `@usuarioId`, etc.), evitando SQL injection.
- `using` garante que a conexão seja fechada automaticamente.

---

## 11. Criar o banco de dados e as tabelas

### `Services/DatabaseInitializer.cs`

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

            CHECK ((Cpf IS NOT NULL AND Cnpj IS NULL) OR (Cpf IS NULL AND Cnpj IS NOT NULL));

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

            CREATE TABLE IF NOT EXISTS Estoque (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ItemId INTEGER NOT NULL UNIQUE,
                TotalComprasKg REAL NOT NULL DEFAULT 0,
                TotalVendasKg REAL NOT NULL DEFAULT 0,
                SaldoKg REAL NOT NULL DEFAULT 0,
                AtualizadoEm TEXT NOT NULL DEFAULT (datetime('now')),

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

### O que esse código faz?

- Cria as pastas do banco, backups e logs.
- Cria o arquivo `.db` (se não existir).
- Cria as tabelas `Usuarios`, `Itens`, `RecibosCompra`, `ReciboCompraItens`, `RecibosVenda`, `RecibosVendaItens`, `Estoque`, `EstoqueInicial` e `Auditoria`.
- Cria as views `EstoqueView` e `EstoqueInicialView`.
- Popula a tabela `Itens` com o catálogo padrão do sistema.
- Usa `Dapper.Execute` para rodar comandos SQL.

---

## 12. Controle de backup

A rotina de backup cria cópias do banco `.db` com data/hora no nome, mantém apenas os N mais recentes e permite restaurar.

### `Services/BackupService.cs`

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

### O que esse código faz?

- `CreateBackup`: copia o `.db` atual com timestamp.
- `RestoreBackup`: restaura uma cópia anterior.
- `ListBackups`: lista todos os backups disponíveis.
- `CleanupOldBackups`: mantém apenas os 7 backups mais recentes.

---

## 13. Processo de auditoria

A auditoria registra quem alterou o quê, quando e qual operação foi realizada. Isso é importante para rastrear mudanças em dados sensíveis.

### `Services/AuditService.cs`

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

### Exemplo de uso no Repository

```csharp
public async Task<int> UpdateAsync(Item item)
{
    var antigo = await GetByIdAsync(item.Id);

    const string sql = @"
        UPDATE Itens
        SET Nome = @nome, Ordem = @ordem, Ativo = @ativo
        WHERE Id = @id";

    using var connection = CreateConnection();
    await connection.OpenAsync();

    var linhas = await connection.ExecuteAsync(sql, item);

    AuditService.Log(
        tabela: "Itens",
        operacao: "UPDATE",
        registroId: item.Id,
        valorAntigo: antigo,
        valorNovo: item,
        usuario: Environment.UserName);

    return linhas;
}
```

### O que esse código faz?

- Registra a operação em uma tabela `Auditoria`.
- Guarda o valor antigo e o novo como JSON, facilitando análise futura.
- Permite identificar o usuário e o horário da alteração.

---

## 14. Configurar o `.csproj` do Class Library

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

- `ImplicitUsings` habilita `using` comuns automaticamente.
- `Nullable` ajuda a evitar erros de referência nula.
- Os dois pacotes são o mínimo necessário para SQLite + Dapper.

---

## 15. Usar os repositórios no projeto Desktop

No `ControleMateriais.Desktop`, você pode consumir as interfaces sem conhecer a implementação:

```csharp
using ControleMateriais.Data.Interfaces;
using ControleMateriais.Data.Models;
using ControleMateriais.Data.Repositories;
using ControleMateriais.Data.Services;

// Inicializar o banco
DatabaseInitializer.EnsureDatabase();

// Criar backup no início ou no fechamento do app
BackupService.CreateBackup();

// Cadastrar um usuário
IUsuarioRepository usuarioRepo = new SqliteUsuarioRepository();

var usuario = new Usuario
{
    Nome = "João da Silva",
    Cpf = "12345678900",
    ChavePix = "joao@email.com"
};

int usuarioId = await usuarioRepo.InsertAsync(usuario);

// Inserir um recibo de compra
IReciboCompraRepository reciboRepo = new SqliteReciboCompraRepository();

var recibo = new ReciboCompra
{
    UsuarioId = usuarioId,
    NumeroRecibo = 10072026083022,
    DataRecibo = "10/07/2026",
    ValorTotal = 3.45m,
    PesoTotalKg = 1.5m,
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

// Consultar o estoque
IEstoqueRepository estoqueRepo = new SqliteEstoqueRepository();
var estoque = await estoqueRepo.GetEstoqueAsync();

foreach (var item in estoque)
{
    Console.WriteLine($"{item.ItemNome}: Saldo = {item.SaldoKg} kg");
}
```

No futuro, com injeção de dependência, você pode registrar `IUsuarioRepository`, `IReciboCompraRepository` e `IEstoqueRepository` para serem resolvidos automaticamente.

---

## 16. Estrutura final esperada

```text
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

Estrutura de dados no disco:

```text
%USERPROFILE%\Downloads\ControleMateriaisLFB\banco-de-dados/
├── database.db
├── backups/
│   ├── database_2026-07-10_080000.db
│   └── database_2026-07-09_080000.db
└── logs/
    └── auditoria_2026-07.log
```

---

## 17. Boas práticas de segurança

- **Nunca concatene SQL com strings do usuário**. Sempre use parâmetros.
- **Faça backup regularmente**. Automatize no início/fim do app.
- **Mantenha logs de auditoria**. Registre quem alterou, quando e o que mudou.
- **Restrinja permissões** da pasta do banco para o usuário atual.

---

## 18. Links para aprofundar

- **.NET Class Library**
  - [Criar uma biblioteca de classes no .NET](https://learn.microsoft.com/pt-br/dotnet/core/tutorials/library-with-visual-studio)

- **SQLite**
  - [Site oficial do SQLite](https://www.sqlite.org/index.html)
  - [Documentação do Microsoft.Data.Sqlite](https://learn.microsoft.com/pt-br/dotnet/standard/data/sqlite/)
  - [SQLitePCLRaw no GitHub](https://github.com/ericsink/SQLitePCL.raw)

- **Dapper**
  - [Repositório do Dapper](https://github.com/DapperLib/Dapper)
  - [Documentação oficial do Dapper](https://www.learndapper.com/)

- **Repository Pattern**
  - [Repository Pattern no .NET](https://learn.microsoft.com/pt-br/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core)

- **SQL Injection**
  - [Prevenção contra SQL Injection](https://learn.microsoft.com/pt-br/sql/relational-databases/security/sql-injection)

- **Auditoria de dados**
  - [Padrões de auditoria em banco de dados](https://learn.microsoft.com/pt-br/aspnet/core/security/authentication/audit-logging)

- **QuestPDF / iText7 (usados no projeto para relatórios PDF)**
  - [QuestPDF](https://www.questpdf.com/)
  - [iText 7](https://itextpdf.com/products/itext-7)
