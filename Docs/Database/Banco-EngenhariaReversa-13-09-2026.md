# Engenharia Reversa - Projeto ControleMateriais.Data
**Data de Análise:** 13/09/2026

## Localização do Banco de Dados SQLite

O banco de dados SQLite será criado no seguinte caminho:

**Caminho Completo:**
```
C:\Users\[NomeDoUsuario]\Downloads\ControleMateriaisLFB\banco-de-dados\database.db
```

**Estrutura de Diretórios:**
- **Pasta Principal:** `C:\Users\[NomeDoUsuario]\Downloads\ControleMateriaisLFB\banco-de-dados\`
- **Arquivo do Banco:** `database.db`
- **Subpastas Criadas:**
  - `backups\` - Armazena backups automáticos do banco
  - `logs\` - Armazena logs do sistema

**Configuração em DbSettings.cs:**
```csharp
public static string DatabaseFolder =>
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads",
        "ControleMateriaisLFB",
        "banco-de-dados");

public static string DatabasePath =>
    Path.Combine(DatabaseFolder, "database.db");
```

---

## Estrutura Completa do Projeto ControleMateriais.Data

### Diretório Raiz
```
ControleMateriais.Data/
├── ControleMateriais.Data.csproj
├── Common/
├── Interfaces/
├── Models/
├── Repositories/
├── Services/
├── bin/
└── obj/
```

---

## 1. Arquivo de Projeto (ControleMateriais.Data.csproj)

**Localização:** Raiz do projeto

**Conteúdo:**
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

**Análise:**
- **Target Framework:** .NET 10.0
- **ImplicitUsings:** Habilitado (usings automáticos)
- **Nullable Reference Types:** Habilitado (segurança de tipos nulos)
- **Dependências:**
  - `Microsoft.Data.Sqlite` v10.0.12 - Driver oficial SQLite da Microsoft
  - `Dapper` v2.1.35 - Micro-ORM para consultas eficientes

---

## 2. Diretório Common/

### Arquivo: DbSettings.cs

**Propósito:** Configuração centralizada de caminhos e conexões do banco de dados

**Funcionalidades:**
- **DatabaseFolder:** Define o caminho da pasta do banco de dados
- **DatabasePath:** Caminho completo do arquivo database.db
- **BackupFolder:** Caminho da pasta de backups
- **LogFolder:** Caminho da pasta de logs
- **CreateConnectionString():** Cria connection string SQLite
- **GetConnectionString():** Retorna a connection string formatada

**Caminhos Configurados:**
```csharp
DatabaseFolder: C:\Users\[Usuario]\Downloads\ControleMateriaisLFB\banco-de-dados
DatabasePath:   C:\Users\[Usuario]\Downloads\ControleMateriaisLFB\banco-de-dados\database.db
BackupFolder:   C:\Users\[Usuario]\Downloads\ControleMateriaisLFB\banco-de-dados\backups
LogFolder:      C:\Users\[Usuario]\Downloads\ControleMateriaisLFB\banco-de-dados\logs
```

**Connection String:**
- **DataSource:** Caminho do arquivo database.db
- **Mode:** ReadWriteCreate (permite leitura, escrita e criação)

---

## 3. Diretório Interfaces/

### 3.1 IEstoqueRepository.cs

**Propósito:** Interface para consultas de estoque

**Métodos:**
- `Task<IEnumerable<EstoqueView>> GetEstoqueAsync()` - Retorna view de estoque

### 3.2 IItemRepository.cs

**Propósito:** Interface para operações CRUD de itens

**Métodos:**
- `Task<IEnumerable<Item>> GetAllAsync()` - Lista todos os itens ativos
- `Task<Item?> GetByIdAsync(int id)` - Busca item por ID
- `Task<int> InsertAsync(Item item)` - Insere novo item
- `Task<int> UpdateAsync(Item item)` - Atualiza item existente
- `Task<int> DeleteAsync(int id)` - Soft delete (desativa item)

### 3.3 IReciboCompraRepository.cs

**Propósito:** Interface para operações CRUD de recibos de compra

**Métodos:**
- `Task<IEnumerable<ReciboCompra>> GetAllAsync()` - Lista todos os recibos de compra
- `Task<ReciboCompra?> GetByIdAsync(int id)` - Busca recibo por ID com itens
- `Task<int> InsertAsync(ReciboCompra recibo)` - Insere novo recibo com itens
- `Task<int> UpdateAsync(ReciboCompra recibo)` - Não suportado (lança exceção)
- `Task<int> DeleteAsync(int id)` - Exclui recibo

### 3.4 IReciboVendaRepository.cs

**Propósito:** Interface para operações CRUD de recibos de venda

**Métodos:**
- `Task<IEnumerable<ReciboVenda>> GetAllAsync()` - Lista todos os recibos de venda
- `Task<ReciboVenda?> GetByIdAsync(int id)` - Busca recibo por ID com itens
- `Task<int> InsertAsync(ReciboVenda recibo)` - Insere novo recibo com itens
- `Task<int> UpdateAsync(ReciboVenda recibo)` - Não suportado (lança exceção)
- `Task<int> DeleteAsync(int id)` - Exclui recibo

### 3.5 IUsuarioRepository.cs

**Propósito:** Interface para operações CRUD de usuários

**Métodos:**
- `Task<IEnumerable<Usuario>> GetAllAsync()` - Lista todos os usuários ativos
- `Task<Usuario?> GetByIdAsync(int id)` - Busca usuário por ID
- `Task<int> InsertAsync(Usuario usuario)` - Insere novo usuário
- `Task<int> UpdateAsync(Usuario usuario)` - Atualiza usuário existente
- `Task<int> DeleteAsync(int id)` - Soft delete (desativa usuário)

---

## 4. Diretório Models/

### 4.1 EstoqueView.cs

**Propósito:** Model para view de estoque consolidado

**Propriedades:**
- `ItemId` - ID do item
- `ItemNome` - Nome do item
- `Ordem` - Ordem de exibição
- `TotalComprasKg` - Total comprado em kg
- `TotalVendasKg` - Total vendido em kg
- `SaldoKg` - Saldo calculado (Compras - Vendas)

### 4.2 Item.cs

**Propósito:** Model para itens/materiais

**Propriedades:**
- `Id` - Identificador único
- `Nome` - Nome do item
- `Ordem` - Ordem de exibição
- `Ativo` - Flag de ativo/inativo

### 4.3 ReciboCompra.cs

**Propósito:** Model para recibos de compra

**Propriedades:**
- `Id` - Identificador único
- `UsuarioId` - ID do usuário fornecedor
- `NumeroRecibo` - Número do recibo (opcional)
- `DataRecibo` - Data do recibo (formato: 10/07/2026)
- `ValorTotal` - Valor total da compra
- `PesoTotalKg` - Peso total em kg
- `CriadoEm` - Data de criação
- `Itens` - Lista de itens do recibo (ReciboCompraItem)

### 4.4 ReciboCompraItem.cs

**Propósito:** Model para itens de recibo de compra

**Propriedades:**
- `Id` - Identificador único
- `ReciboCompraId` - ID do recibo de compra
- `ItemId` - ID do item
- `PesoKg` - Peso em kg
- `PrecoPorKg` - Preço por kg
- `Total` - Total calculado (PesoKg * PrecoPorKg)
- `ItemNome` - Nome do item (populado via JOIN)

### 4.5 ReciboVenda.cs

**Propósito:** Model para recibos de venda

**Propriedades:**
- `Id` - Identificador único
- `ClienteNome` - Nome do cliente
- `ClienteCpf` - CPF do cliente (opcional)
- `ClienteCnpj` - CNPJ do cliente (opcional)
- `ClienteChavePix` - Chave PIX do cliente
- `NumeroRecibo` - Número do recibo (opcional)
- `DataVenda` - Data da venda (formato: 10/07/2026)
- `ValorTotal` - Valor total da venda
- `PesoTotalKg` - Peso total em kg
- `CriadoEm` - Data de criação
- `Itens` - Lista de itens do recibo (ReciboVendaItem)

### 4.6 ReciboVendaItem.cs

**Propósito:** Model para itens de recibo de venda

**Propriedades:**
- `Id` - Identificador único
- `ReciboVendaId` - ID do recibo de venda
- `ItemId` - ID do item
- `PesoKg` - Peso em kg
- `PrecoPorKg` - Preço por kg
- `Total` - Total calculado (PesoKg * PrecoPorKg)
- `ItemNome` - Nome do item (populado via JOIN)

### 4.7 Usuario.cs

**Propósito:** Model para usuários/fornecedores

**Propriedades:**
- `Id` - Identificador único
- `Nome` - Nome do usuário
- `Cpf` - CPF (opcional)
- `Cnpj` - CNPJ (opcional)
- `ChavePix` - Chave PIX
- `CriadoEm` - Data de criação
- `AtualizadoEm` - Data de atualização
- `Ativo` - Flag de ativo/inativo

---

## 5. Diretório Repositories/

### 5.1 SqliteEstoqueRepository.cs

**Propósito:** Implementação do repositório de estoque

**Funcionalidades:**
- `GetEstoqueAsync()` - Consulta a view EstoqueView ordenada por Ordem

**SQL Utilizado:**
```sql
SELECT ItemId, ItemNome, Ordem, TotalComprasKg, TotalVendasKg, SaldoKg
FROM EstoqueView
ORDER BY Ordem;
```

### 5.2 SqliteItemRepository.cs

**Propósito:** Implementação do repositório de itens

**Funcionalidades:**
- `GetAllAsync()` - Lista itens ativos ordenados por Ordem
- `GetByIdAsync(int id)` - Busca item por ID
- `InsertAsync(Item item)` - Insere novo item e retorna ID
- `UpdateAsync(Item item)` - Atualiza item existente
- `DeleteAsync(int id)` - Soft delete (seta Ativo = 0)

**SQLs Principais:**
- SELECT com filtro Ativo = 1
- INSERT com last_insert_rowid()
- UPDATE com todos os campos
- UPDATE para soft delete

### 5.3 SqliteReciboCompraRepository.cs

**Propósito:** Implementação do repositório de recibos de compra

**Funcionalidades:**
- `InsertAsync(ReciboCompra recibo)` - Insere recibo com itens em transação
- `GetAllAsync()` - Lista recibos com JOIN de usuários
- `GetByIdAsync(int id)` - Busca recibo com itens e nomes dos itens
- `UpdateAsync()` - Lança NotSupportedException (recibos não são editados)
- `DeleteAsync(int id)` - Exclui recibo

**Características Especiais:**
- **Transações:** Usa transação para garantir consistência entre recibo e itens
- **JOINs:** Busca nome do usuário e nome dos itens
- **Cascata:** Itens são excluídos em cascata pelo banco

### 5.4 SqliteReciboVendaRepository.cs

**Propósito:** Implementação do repositório de recibos de venda

**Funcionalidades:**
- `InsertAsync(ReciboVenda recibo)` - Insere recibo com itens em transação
- `GetAllAsync()` - Lista recibos ordenados por data
- `GetByIdAsync(int id)` - Busca recibo com itens e nomes dos itens
- `UpdateAsync()` - Lança NotSupportedException (recibos não são editados)
- `DeleteAsync(int id)` - Exclui recibo

**Características Especiais:**
- **Transações:** Usa transação para garantir consistência
- **JOINs:** Busca nome dos itens
- **Cascata:** Itens são excluídos em cascata pelo banco

### 5.5 SqliteUsuarioRepository.cs

**Propósito:** Implementação do repositório de usuários

**Funcionalidades:**
- `GetAllAsync()` - Lista usuários ativos ordenados por Nome
- `GetByIdAsync(int id)` - Busca usuário por ID
- `InsertAsync(Usuario usuario)` - Insere novo usuário
- `UpdateAsync(Usuario usuario)` - Atualiza usuário
- `DeleteAsync(int id)` - Soft delete (seta Ativo = 0)

**SQLs Principais:**
- SELECT com filtro Ativo = 1
- INSERT com last_insert_rowid()
- UPDATE com todos os campos incluindo AtualizadoEm
- UPDATE para soft delete

---

## 6. Diretório Services/

### 6.1 AuditService.cs

**Propósito:** Serviço estático para auditoria de operações

**Funcionalidades:**
- `Log(string tabela, string operacao, int registroId, object? valorAntigo, object? valorNovo, string? usuario)` - Registra operações de auditoria

**Características:**
- **Serialização JSON:** Valores antigos e novos são serializados em JSON
- **Timestamp:** Registra data/hora da operação
- **Tabela Auditoria:** Insere registros na tabela de auditoria

**SQL Utilizado:**
```sql
INSERT INTO Auditoria (Tabela, Operacao, RegistroId, ValorAntigo, ValorNovo, Usuario, DataHora)
VALUES (@tabela, @operacao, @registroId, @valorAntigo, @valorNovo, @usuario, @dataHora);
```

### 6.2 BackupService.cs

**Propósito:** Serviço estático para gerenciamento de backups

**Funcionalidades:**
- `CreateBackup()` - Cria backup com timestamp
- `RestoreBackup(string backupPath)` - Restaura backup específico
- `ListBackups()` - Lista todos os backups disponíveis
- `CleanupOldBackups()` - Mantém apenas os 7 backups mais recentes

**Características:**
- **MaxBackups:** Mantém no máximo 7 backups
- **Timestamp:** Formato yyyy-MM-dd_HHmmss
- **Nomenclatura:** database_yyyy-MM-dd_HHmmss.db
- **Limpeza Automática:** Remove backups antigos automaticamente

### 6.3 DatabaseInitializer.cs

**Propósito:** Inicialização do banco de dados e tabelas

**Funcionalidades:**
- `EnsureDatabase()` - Cria diretórios, tabelas e dados iniciais
- `SeedItems(SqliteConnection connection)` - Popula tabela de itens com dados iniciais

**Tabelas Criadas:**

#### 1. Usuarios
```sql
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
```

#### 2. Itens
```sql
CREATE TABLE IF NOT EXISTS Itens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL UNIQUE,
    Ordem INTEGER NOT NULL DEFAULT 0,
    Ativo INTEGER NOT NULL DEFAULT 1
);
```

#### 3. RecibosCompra
```sql
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
```

#### 4. ReciboCompraItens
```sql
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
```

#### 5. RecibosVenda
```sql
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
```

#### 6. RecibosVendaItens
```sql
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
```

#### 7. EstoqueView (VIEW)
```sql
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
```

#### 8. Auditoria
```sql
CREATE TABLE IF NOT EXISTS Auditoria (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Tabela TEXT NOT NULL,
    Operacao TEXT NOT NULL,
    RegistroId INTEGER,
    ValorAntigo TEXT,
    ValorNovo TEXT,
    Usuario TEXT,
    DataHora TEXT NOT NULL
);
```

**Dados Iniciais (Seed):**
- 50 tipos de itens pré-cadastrados (placas, processadores, memórias, baterias, etc.)
- Cada item tem Nome e Ordem definidos
- Usa `ON CONFLICT(Nome) DO NOTHING` para evitar duplicatas

---

## 7. Diretórios de Build (bin/ e obj/)

### bin/Debug/net10.0/
- `ControleMateriais.Data.dll` - DLL compilada
- `ControleMateriais.Data.pdb` - Símbolos de debug
- `ControleMateriais.Data.deps.json` - Dependências

### obj/Debug/net10.0/
- Arquivos intermediários de build
- Arquivos de configuração do MSBuild
- Assembly info e metadados

---

## Fluxo de Funcionamento

### 1. Inicialização
1. Aplicação chama `DatabaseInitializer.EnsureDatabase()`
2. Cria diretórios: banco-de-dados, backups, logs
3. Cria arquivo database.db se não existir
4. Cria todas as tabelas se não existirem
5. Popula tabela Itens com 50 itens iniciais

### 2. Operações de Dados
1. Aplicação usa interfaces (IUsuarioRepository, IItemRepository, etc.)
2. Implementações concretas (SqliteUsuarioRepository, etc.) executam SQL
3. Dapper mapeia resultados para objetos C#
4. Transações garantem consistência em operações complexas

### 3. Auditoria
1. Operações críticas chamam `AuditService.Log()`
2. Valores são serializados em JSON
3. Registro é salvo na tabela Auditoria

### 4. Backup
1. `BackupService.CreateBackup()` é chamado periodicamente
2. Cria cópia do database.db com timestamp
3. Mantém apenas os 7 backups mais recentes

---

## Características Técnicas

### Segurança
- **Parameterized Queries:** Dapper protege contra SQL injection
- **Foreign Keys:** PRAGMA foreign_keys = ON habilitado
- **CHECK Constraints:** Validação de regras de negócio
- **UNIQUE Constraints:** Evita duplicidade de CPF/CNPJ
- **Soft Delete:** Preserva histórico com flag Ativo

### Performance
- **Índices:** PRIMARY KEY e UNIQUE criados automaticamente
- **Views:** EstoqueView pré-calcula estoque consolidado
- **Connection Pooling:** Gerenciado pelo SQLite
- **Async Operations:** Todas as operações são assíncronas

### Integridade
- **Transações:** Operações complexas usam transações
- **CASCADE DELETE:** Itens de recibos excluídos em cascata
- **COALESCE:** Tratamento de NULL em views
- **DEFAULT VALUES:** Valores automáticos para timestamps

---

## Conclusão

O projeto ControleMateriais.Data apresenta uma arquitetura bem estruturada seguindo o Repository Pattern com SQLite e Dapper. O banco de dados é criado automaticamente no diretório de Downloads do usuário, com sistema de backup, auditoria e dados iniciais pré-populados. A implementação foca em segurança, performance e integridade dos dados para o sistema de controle de materiais.
