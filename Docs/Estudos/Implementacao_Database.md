# Implementação do ControleMateriais.Data

Este documento descreve a arquitetura implementada no projeto `ControleMateriais.Data`, onde fica o banco de dados e como verificar que tudo está funcionando corretamente.

---

## Visão Geral da Arquitetura

```
ControleMateriais.Data/
├── Common/
│   └── DbSettings.cs              ← Configuração centralizada (caminho, connection string)
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
    ├── DatabaseInitializer.cs     ← Cria tabelas, views e seed de 50 itens
    ├── BackupService.cs           ← Backup automático com timestamp (máx 7)
    └── AuditService.cs            ← Log de alterações em JSON na tabela Auditoria
```

### Tecnologias utilizadas

| Tecnologia | Versão | Função |
|---|---|---|
| `Microsoft.Data.Sqlite` | 10.0.12 | Driver SQLite para .NET (inclui provider nativo embutido) |
| `Dapper` | 2.1.35 | Micro-ORM para mapeamento SQL → C# |

---

## Onde o banco de dados fica no disco

O banco é salvo automaticamente em:

```
%USERPROFILE%\Downloads\ControleMateriaisLFB\banco-de-dados\
├── database.db            ← Banco principal
├── backups\
│   ├── database_2026-07-15_140000.db
│   └── database_2026-07-14_080000.db
└── logs\
    └── (pasta reservada para logs futuros)
```

Em uma máquina com usuário `Gabriel_Stundner`, o caminho completo seria:
```
C:\Users\Gabriel_Stundner\Downloads\ControleMateriaisLFB\banco-de-dados\database.db
```

---

## Fluxo de inicialização

Quando a aplicação iniciar, ela deve chamar:

```csharp
DatabaseInitializer.EnsureDatabase();
```

Isso executa automaticamente:
1. Cria as pastas `banco-de-dados/`, `backups/` e `logs/`
2. Abre (ou cria) o arquivo `database.db`
3. Executa os `CREATE TABLE IF NOT EXISTS` e `CREATE VIEW IF NOT EXISTS`
4. Insere os 50 itens do catálogo (se ainda não existirem)

---

## Como testar a implementação

### Pré-requisito: instalar SQLite CLI (opcional, para inspeção manual)

```powershell
winget install SQLite.SQLite
```

> **Atenção:** O banco **não** é criptografado. O `sqlite3` padrão ou o DB Browser for SQLite conseguem abrir o arquivo `database.db` diretamente, sem senha.

---

### Teste 1: Build do projeto

Confirmar que o projeto compila sem erros:

```powershell
cd c:\Users\Gabriel_Stundner\source\repos\GITHUB\LFB\Controle-Materiais
dotnet build ControleMateriais.Data/ControleMateriais.Data.csproj
```

**Resultado esperado:**
```
Build succeeded.
```

---

### Teste 2: Criar um projeto de console temporário para testar

Para testar sem modificar o Desktop, crie um console app temporário na raiz da solution:

```powershell
dotnet new console -n ControleMateriais.TestConsole -o TestConsole --framework net10.0
cd TestConsole
dotnet add reference ../ControleMateriais.Data/ControleMateriais.Data.csproj
```

Cole o seguinte código em `TestConsole/Program.cs`:

```csharp
using ControleMateriais.Data.Interfaces;
using ControleMateriais.Data.Models;
using ControleMateriais.Data.Repositories;
using ControleMateriais.Data.Services;

Console.WriteLine("=== Teste ControleMateriais.Data ===\n");

// 1. Inicializar o banco
Console.WriteLine("[1] Inicializando banco...");
DatabaseInitializer.EnsureDatabase();
Console.WriteLine("    OK - Banco criado/verificado.\n");

// 2. Criar backup
Console.WriteLine("[2] Criando backup...");
BackupService.CreateBackup();
Console.WriteLine("    OK - Backup gerado.\n");

// 3. Listar itens do catálogo
Console.WriteLine("[3] Listando itens do catálogo...");
IItemRepository itemRepo = new SqliteItemRepository();
var itens = await itemRepo.GetAllAsync();
foreach (var item in itens.Take(5))
    Console.WriteLine($"    [{item.Id:D2}] {item.Nome}");
Console.WriteLine($"    ... e mais {itens.Count() - 5} itens.\n");

// 4. Inserir um usuário
Console.WriteLine("[4] Inserindo usuário de teste...");
IUsuarioRepository usuarioRepo = new SqliteUsuarioRepository();
var usuario = new Usuario
{
    Nome = "Fornecedor Teste",
    Cpf = "12345678900",
    ChavePix = "teste@email.com",
    CriadoEm = DateTime.Now,
    AtualizadoEm = DateTime.Now,
    Ativo = true
};
var usuarioId = await usuarioRepo.InsertAsync(usuario);
Console.WriteLine($"    OK - Usuário inserido com Id = {usuarioId}\n");

// 5. Inserir um recibo de compra
Console.WriteLine("[5] Inserindo recibo de compra...");
IReciboCompraRepository reciboCompraRepo = new SqliteReciboCompraRepository();
var recibo = new ReciboCompra
{
    UsuarioId = usuarioId,
    NumeroRecibo = 1507202612345,
    DataRecibo = DateTime.Now.ToString("dd/MM/yyyy"),
    ValorTotal = 3.45m,
    PesoTotalKg = 1.5m,
    Itens = new List<ReciboCompraItem>
    {
        new ReciboCompraItem { ItemId = 1, PesoKg = 1.5m, PrecoPorKg = 2.30m }
    }
};
var reciboId = await reciboCompraRepo.InsertAsync(recibo);
Console.WriteLine($"    OK - Recibo inserido com Id = {reciboId}\n");

// 6. Consultar estoque
Console.WriteLine("[6] Consultando estoque (View)...");
IEstoqueRepository estoqueRepo = new SqliteEstoqueRepository();
var estoque = await estoqueRepo.GetEstoqueAsync();
foreach (var e in estoque.Where(x => x.TotalComprasKg > 0))
    Console.WriteLine($"    {e.ItemNome}: Compras={e.TotalComprasKg:F2}kg  Vendas={e.TotalVendasKg:F2}kg  Saldo={e.SaldoKg:F2}kg");
Console.WriteLine();

// 7. Registrar auditoria
Console.WriteLine("[7] Registrando entrada de auditoria...");
AuditService.Log("Usuarios", "INSERT", usuarioId, null, usuario, "teste-console");
Console.WriteLine("    OK - Log de auditoria registrado.\n");

Console.WriteLine("=== Todos os testes concluídos com sucesso! ===");
```

Execute:

```powershell
dotnet run
```

---

### Teste 3: Verificar o arquivo do banco no disco

```powershell
$dbPath = "$env:USERPROFILE\Downloads\ControleMateriaisLFB\banco-de-dados\database.db"
Test-Path $dbPath
(Get-Item $dbPath).Length / 1KB
```

**Resultado esperado:**
```
True
(tamanho em KB > 0)
```

---

### Teste 4: Listar backups criados

```powershell
$backupDir = "$env:USERPROFILE\Downloads\ControleMateriaisLFB\banco-de-dados\backups"
Get-ChildItem $backupDir -Filter "database_*.db" | Sort-Object Name -Descending
```

---

### Teste 5: Inspecionar o banco com DB Browser for SQLite

1. Baixar: https://sqlitebrowser.org/dl/
2. Abrir o arquivo `database.db` (nenhuma senha é solicitada)
3. Verificar as tabelas: `Usuarios`, `Itens`, `RecibosCompra`, etc.
4. Verificar as views: `EstoqueView`, `EstoqueInicialView`
5. Executar na aba SQL:

```sql
SELECT COUNT(*) AS TotalItens FROM Itens;
```

**Esperado:** `50`

```sql
SELECT * FROM EstoqueView LIMIT 5;
```

```sql
SELECT * FROM Auditoria;
```

---

## Segurança do banco

O banco SQLite **não é criptografado** (a criptografia via SQLCipher foi removida). O arquivo `database.db` pode ser aberto por qualquer ferramenta SQLite padrão sem senha. Se for necessário proteger o conteúdo do banco no futuro, considere controlar o acesso ao arquivo pelo sistema operacional (permissões de pasta) em vez de reintroduzir criptografia na camada de dados.

---

## Padrões de projeto utilizados

### Repository Pattern
Cada entidade tem:
- Uma **interface** (`I*Repository`) definindo o contrato
- Uma **implementação** (`Sqlite*Repository`) usando Dapper

Isso permite trocar a implementação (ex: usar um banco diferente para testes) sem modificar o restante do código.

### Soft Delete
`Usuario` e `Item` usam `Ativo = 0` no lugar de `DELETE` real, preservando o histórico nos recibos.

### Transações
`SqliteReciboCompraRepository.InsertAsync` e `SqliteReciboVendaRepository.InsertAsync` usam transações: se a inserção de qualquer item do recibo falhar, **tudo é revertido** (`Rollback`), garantindo consistência.

### Views de Estoque
- `EstoqueView` — Saldo = Compras − Vendas (sem estoque inicial)
- `EstoqueInicialView` — Saldo = EstoqueInicial + Compras − Vendas (com estoque inicial)

---

## Próximo passo: integrar ao Desktop

Quando estiver pronto para integrar ao `ControleMateriais.Desktop`:

1. Adicionar referência ao projeto Data:

```powershell
dotnet add ControleMateriais.Desktop/ControleMateriais.Desktop.csproj reference ControleMateriais.Data/ControleMateriais.Data.csproj
```

2. Chamar na inicialização da aplicação (ex: `App.axaml.cs`):

```csharp
using ControleMateriais.Data.Services;

// No método OnFrameworkInitializationCompleted()
DatabaseInitializer.EnsureDatabase();
BackupService.CreateBackup();
```

3. Injetar os repositórios nos ViewModels conforme necessário.
