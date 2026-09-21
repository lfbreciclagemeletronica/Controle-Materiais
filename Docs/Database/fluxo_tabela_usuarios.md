# Fluxo da Tabela de Usuários (Fornecedores)

## Visão Geral

A tabela `Usuarios` do banco de dados SQLite é usada para armazenar os fornecedores cadastrados no sistema. Cada fornecedor é um registro que pode ser associado a recibos de compra.

## SQL Original da Tabela

```sql
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
```

### Explicação das Colunas

| Coluna | Tipo | Descrição |
|--------|------|-----------|
| `Id` | INTEGER (PK) | Identificador único auto-incrementado |
| `Nome` | TEXT (UNIQUE, NOT NULL) | Nome do fornecedor. Deve ser único e obrigatório |
| `Cpf` | TEXT (UNIQUE) | CPF do fornecedor (opcional, único se informado) |
| `Cnpj` | TEXT (UNIQUE) | CNPJ do fornecedor (opcional, único se informado) |
| `ChavePix` | TEXT | Chave Pix para pagamentos (opcional) |
| `CriadoEm` | TEXT | Data/hora de criação do registro (ISO 8601) |
| `AtualizadoEm` | TEXT | Data/hora da última atualização (ISO 8601) |
| `Ativo` | INTEGER | Flag de ativação (1 = ativo, 0 = inativo) |

---

## Fluxo de Criação e Gerenciamento

### 1. Inicialização do Banco

**Arquivo:** `ControleMateriais.Data/Services/DatabaseInitializer.cs`

O banco é inicializado no startup da aplicação (`Program.cs`):

```csharp
DatabaseInitializer.EnsureDatabase();
```

O `DatabaseInitializer`:
- Cria a tabela `Usuarios` se não existir
- **Não faz seed de usuários** (diferente de `Itens`, que são pré-cadastrados)
- Preserva dados existentes se o banco já tiver registros

### 2. Leitura de Usuários (Listagem)

**Arquivo:** `ControleMateriais.Data/Repositories/SqliteUsuarioRepository.cs`

**Método:** `GetAllAsync()`

```csharp
public async Task<IEnumerable<Usuario>> GetAllAsync()
{
    const string sql = "SELECT * FROM Usuarios WHERE Ativo = 1 ORDER BY Nome;";

    using var connection = CreateConnection();
    await connection.OpenAsync();
    return await connection.QueryAsync<Usuario>(sql);
}
```

**SQL executado:**
```sql
SELECT * FROM Usuarios WHERE Ativo = 1 ORDER BY Nome;
```

**Comportamento:**
- Retorna apenas usuários com `Ativo = 1` (soft-delete)
- Ordena alfabeticamente por nome
- Usado na aba "Fornecedores" do `PesagensView`

**Arquivo Desktop:** `ControleMateriais.Desktop/ViewModels/PesagensViewModel.cs`

```csharp
public async Task CarregarFornecedoresAsync()
{
    CarregandoFornecedores = true;
    try
    {
        var usuarios = await DataService.UsuarioRepository.GetAllAsync();
        Fornecedores.Clear();
        foreach (var u in usuarios.OrderBy(u => u.Nome))
            Fornecedores.Add(u);
    }
    finally
    {
        CarregandoFornecedores = false;
    }
}
```

### 3. Criação de Usuário

**Arquivo:** `ControleMateriais.Data/Repositories/SqliteUsuarioRepository.cs`

**Método:** `InsertAsync(Usuario usuario)`

```csharp
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
```

**SQL executado:**
```sql
INSERT INTO Usuarios (Nome, Cpf, Cnpj, ChavePix, CriadoEm, AtualizadoEm, Ativo)
VALUES (@Nome, @Cpf, @Cnpj, @ChavePix, @CriadoEm, @AtualizadoEm, @Ativo);
SELECT last_insert_rowid();
```

> **Nota sobre os nomes dos parâmetros:** Quando um objeto POCO (como `Usuario`) é passado diretamente ao Dapper (`ExecuteScalarAsync<int>(sql, usuario)`), os placeholders `@Nome`, `@Cpf`, etc. precisam ter **exatamente o mesmo case** das propriedades do objeto (`Nome`, `Cpf`, `Cnpj`...). Isso porque o combo Dapper + Microsoft.Data.Sqlite não faz o matching de forma totalmente case-insensitive em todos os cenários, ao contrário do que ocorre com outros provedores (SQL Server, por exemplo). Usar `@nome` (minúsculo) enquanto a propriedade é `Nome` (maiúsculo) pode causar o erro `Must add values for the following parameters: ...`. Por isso os parâmetros aqui usam PascalCase, igual às propriedades do model.

**Comportamento:**
- Insere novo registro com `Ativo = 1`
- Retorna o `Id` gerado
- Se `Nome` já existir, o SQLite lançará erro devido à constraint `UNIQUE`

**Nota:** A UI atual não tem botão "Novo Fornecedor", apenas Editar/Inativar. Para adicionar criação, seria necessário adicionar um botão e um dialog similar ao `EditarFornecedorDialog`.

### 4. Edição de Usuário

**Arquivo:** `ControleMateriais.Data/Repositories/SqliteUsuarioRepository.cs`

**Método:** `UpdateAsync(Usuario usuario)`

```csharp
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
```

**SQL executado:**
```sql
UPDATE Usuarios
SET Nome = @Nome, Cpf = @Cpf, Cnpj = @Cnpj, ChavePix = @ChavePix,
    AtualizadoEm = @AtualizadoEm, Ativo = @Ativo
WHERE Id = @Id;
```

**Comportamento:**
- Atualiza todos os campos exceto `Id` e `CriadoEm`
- `AtualizadoEm` é atualizado automaticamente pelo código antes de chamar o método
- Se `Nome` for alterado para um valor que já existe em outro registro, erro de constraint

**Arquivo Desktop:** `ControleMateriais.Desktop/ViewModels/PesagensViewModel.cs`

```csharp
public async Task SalvarFornecedorAsync(Usuario usuario)
{
    try
    {
        usuario.AtualizadoEm = DateTime.Now;
        await DataService.UsuarioRepository.UpdateAsync(usuario);
        await CarregarFornecedoresAsync();
    }
    catch (Exception ex)
    {
        StatusFornecedores = $"Erro ao salvar fornecedor: {ex.Message}";
    }
}
```

### 5. Inativação de Usuário (Soft-Delete)

**Arquivo:** `ControleMateriais.Data/Repositories/SqliteUsuarioRepository.cs`

**Método:** `DeleteAsync(int id)`

```csharp
public async Task<int> DeleteAsync(int id)
{
    const string sql = "UPDATE Usuarios SET Ativo = 0 WHERE Id = @id;";

    using var connection = CreateConnection();
    await connection.OpenAsync();
    return await connection.ExecuteAsync(sql, new { id });
}
```

**SQL executado:**
```sql
UPDATE Usuarios SET Ativo = 0 WHERE Id = @id;
```

**Comportamento:**
- **Soft-delete**: marca `Ativo = 0` em vez de deletar o registro
- O registro permanece no banco, mas não aparece em `GetAllAsync()`
- Preserva integridade referencial com recibos
- **Na UI, o botão é exibido como "Inativar"** (não "Excluir") para deixar claro que é uma inativação

**Arquivo Desktop:** `ControleMateriais.Desktop/ViewModels/PesagensViewModel.cs`

```csharp
private async Task InativarFornecedorAsync(Usuario? usuario)
{
    if (usuario is null) return;

    if (ConfirmarInativarFornecedorCallback is not null)
    {
        var ok = await ConfirmarInativarFornecedorCallback(usuario);
        if (!ok) return;
    }

    try
    {
        await DataService.UsuarioRepository.DeleteAsync(usuario.Id);
        await CarregarFornecedoresAsync();
    }
    catch (Exception ex)
    {
        StatusFornecedores = $"Erro ao inativar fornecedor: {ex.Message}";
    }
}
```

---

## Relacionamento com Recibos de Compra

### Estrutura da Tabela `RecibosCompra`

```sql
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
```

### O que acontece ao inativar um usuário?

**Resposta:** O usuário é marcado como inativo (`Ativo = 0`), mas **os recibos associados continuam existindo** no banco.

**Por que?**
- A FOREIGN KEY é definida sem `ON DELETE CASCADE`
- A inativação é soft-delete, não há deleção física do registro
- Os recibos continuam com o `UsuarioId` original

**Impacto:**
- Recibos de compra continuam acessíveis e podem ser consultados
- O nome do fornecedor pode ser obtido via JOIN com a tabela `Usuarios`
- Se precisar reativar o usuário, basta mudar `Ativo` de volta para `1`

**Exemplo de consulta que mostra recibos com fornecedor inativo:**

```sql
SELECT
    rc.Id, rc.CodigoRecibo, rc.DataRecibo, rc.ValorTotal,
    u.Nome AS UsuarioNome, u.Ativo AS UsuarioAtivo
FROM RecibosCompra rc
JOIN Usuarios u ON u.Id = rc.UsuarioId
WHERE rc.UsuarioId = @usuarioId;
```

### Se você quiser ON DELETE CASCADE

Se preferir que a exclusão de um usuário **delete fisicamente** os recibos associados, altere a tabela:

```sql
CREATE TABLE IF NOT EXISTS RecibosCompra (
    ...
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE
);
```

**Atenção:** Isso é **perigoso** — você perderia todo o histórico de compras desse fornecedor. O soft-delete atual é mais seguro.

---

## Service Locator

**Arquivo:** `ControleMateriais.Desktop/DataService.cs`

```csharp
public static class DataService
{
    public static IUsuarioRepository UsuarioRepository => new SqliteUsuarioRepository();
    public static IItemRepository ItemRepository => new SqliteItemRepository();
    public static IReciboCompraRepository ReciboCompraRepository => new SqliteReciboCompraRepository();
    public static IReciboVendaRepository ReciboVendaRepository => new SqliteReciboVendaRepository();
    public static IEstoqueRepository EstoqueRepository => new SqliteEstoqueRepository();
}
```

**Propósito:** Fornece acesso aos repositórios de forma centralizada. No futuro, pode ser substituído por injeção de dependência (DI).

---

## Dúvidas Frequentes

### 1. Posso ter CPF e CNPJ no mesmo registro?
**Não.** Embora o SQL permita (ambos são `UNIQUE` individualmente), isso não faz sentido do ponto de vista de negócio. A UI deve validar: se CPF for preenchido, CNPJ deve estar vazio, e vice-versa.

### 2. O que acontece se tentar criar dois usuários com o mesmo nome?
**Erro de constraint.** O SQLite lançará uma exceção devido à constraint `Nome TEXT NOT NULL UNIQUE`. A UI deve capturar esse erro e mostrar uma mensagem amigável.

### 3. Como restaurar um usuário inativado?
Como a inativação é soft-delete, é possível restaurar com:
```sql
UPDATE Usuarios SET Ativo = 1, AtualizadoEm = @atualizadoEm WHERE Id = @id;
```
Isso já está implementado na UI: na aba **Fornecedores**, a coluna **Status** mostra "Ativo" (verde) ou "Inativo" (vermelho). Clicar no status "Inativo" abre uma confirmação e, se aceito, chama `IUsuarioRepository.ReativarAsync(id)`.

### 4. Por que `ChavePix` não é UNIQUE?
Porque múltiplos fornecedores podem compartilhar a mesma chave Pix (ex: chave de uma conta bancária da empresa).

### 5. Posso usar `UsuarioId = NULL` em `RecibosCompra`?
**Não.** A coluna `UsuarioId` é `NOT NULL`. Todo recibo de compra deve estar associado a um fornecedor.

### 6. Por que o botão diz "Inativar" e não "Excluir"?
Para deixar claro que é uma inativação (soft-delete) e não uma deleção física. Os recibos associados não são afetados, e o usuário pode ser reativado posteriormente.

---

## Cadastro Automático e Persistência do Recibo no Banco de Dados

**Arquivo:** `ControleMateriais.Desktop/ViewModels/MainWindowViewModel.cs`

Na tela "Recibos de compras", o campo de nome do cliente é um `AutoCompleteBox` que busca sugestões em `NomesFornecedores` (carregado de `Usuarios WHERE Ativo = 1`, ou seja, **somente fornecedores ativos**).

Ao clicar em "Novo recibo de compra" (`ExportarCommand` → `ExportarAsync`), depois que o PDF é gerado e o repositório Git preparado, o fluxo grava os dados no banco SQLite seguindo exatamente a ordem de dependências das tabelas:

1. **Usuarios** — `GarantirFornecedorCadastradoAsync` chama `IUsuarioRepository.GetOrCreateAsync(nome)`, que executa `INSERT ... ON CONFLICT(Nome) DO NOTHING` e retorna o registro (com `Id`), seja ele novo ou já existente.
2. **RecibosCompra** — `SalvarReciboNoBancoDeDadosAsync` gera um código de recibo aleatório de 5 dígitos único (`IReciboCompraRepository.GerarCodigoReciboAsync`) e monta o `ReciboCompra` (UsuarioId, DataRecibo, CodigoRecibo, ValorTotal, PesoTotalKg).
3. **Itens** — para cada material pesado (catálogo + itens personalizados/extras com peso > 0), `IItemRepository.GetOrCreateAsync(nome)` garante que o item existe (também via `ON CONFLICT(Nome) DO NOTHING`), incluindo itens fora do catálogo padrão (ex.: "OUTROS").
4. **ReciboCompraItens** — `IReciboCompraRepository.InsertAsync(reciboCompra)` insere o cabeçalho do recibo e, na mesma transação, um registro por item com `PesoKg`/`PrecoPorKg`.

```csharp
// Trecho de ExportarAsync (MainWindowViewModel.cs)
AtualizarStatusTela("Gerando PDF...");
GerarReciboPdf(filePath);

var itensRecibo = Itens.Where(i => i.PesoAtual > 0)
    .Select(i => (i.Nome, i.PesoAtual, i.PrecoPorKg))
    .Concat(ItensPersonalizados
        .Where(c => c.PesoAtual > 0 && !string.IsNullOrWhiteSpace(c.Nome))
        .Select(c => (c.Nome, c.PesoAtual, c.PrecoPorKg)))
    .ToList();

var usuarioRecibo = await GarantirFornecedorCadastradoAsync(NomeCliente);
if (usuarioRecibo is not null)
    await SalvarReciboNoBancoDeDadosAsync(usuarioRecibo, data, itensRecibo);
```

```csharp
private async Task SalvarReciboNoBancoDeDadosAsync(Usuario usuario, DateTime data,
    IReadOnlyList<(string Nome, decimal PesoAtual, decimal PrecoPorKg)> itensRecibo)
{
    var codigoRecibo = await DataService.ReciboCompraRepository.GerarCodigoReciboAsync();

    var reciboCompra = new ReciboCompra
    {
        UsuarioId = usuario.Id,
        DataRecibo = data.ToString("dd/MM/yyyy"),
        CodigoRecibo = codigoRecibo,
        ValorTotal = TotalGeral,
        PesoTotalKg = PesoTotal
    };

    foreach (var (nome, peso, precoPorKg) in itensRecibo)
    {
        var item = await DataService.ItemRepository.GetOrCreateAsync(nome);
        reciboCompra.Itens.Add(new ReciboCompraItem { ItemId = item.Id, PesoKg = peso, PrecoPorKg = precoPorKg });
    }

    await DataService.ReciboCompraRepository.InsertAsync(reciboCompra);
}
```

Esse fluxo é exatamente equivalente ao SQL de exemplo:

```sql
INSERT INTO Usuarios (Nome, Cpf, Cnpj, ChavePix) VALUES ('ADEMAR NUNES FLORÃO', NULL, NULL, NULL)
    ON CONFLICT(Nome) DO NOTHING;

INSERT INTO RecibosCompra (UsuarioId, DataRecibo, CodigoRecibo, ValorTotal, PesoTotalKg)
    VALUES ((SELECT Id FROM Usuarios WHERE Nome = 'ADEMAR NUNES FLORÃO'), '27/05/2026', '32840', 1100.89, 55.013);

INSERT INTO Itens (Nome) VALUES ('Placa Drive') ON CONFLICT(Nome) DO NOTHING;

INSERT INTO ReciboCompraItens (ReciboCodigo, ItemId, PesoKg, PrecoPorKg, Total)
    VALUES (32840, (SELECT Id FROM Itens WHERE Nome = 'Placa Drive'), 0.016, 103, 1.65);
-- ... um par de INSERT INTO Itens / INSERT INTO ReciboCompraItens por material pesado no recibo
```

**Métodos novos em `ControleMateriais.Data` para suportar esse fluxo:**
- `IUsuarioRepository.GetOrCreateAsync(string nome)` — cria o fornecedor se não existir e retorna o registro com `Id`
- `IItemRepository.GetOrCreateAsync(string nome)` — cria o item/material se não existir (usado para itens personalizados fora do catálogo) e retorna o registro com `Id`
- `IReciboCompraRepository.GerarCodigoReciboAsync()` — gera um código numérico aleatório de 5 dígitos e garante que ele ainda não existe em `RecibosCompra`

**Comportamento:**
- Falhas no cadastro do fornecedor ou na gravação do recibo **não bloqueiam** a emissão do PDF nem a sincronização com o GitHub — apenas exibem um aviso (toast) ao usuário
- A gravação ocorre logo após `GerarReciboPdf()`, ou seja, o PDF já foi gerado quando os dados vão para o banco

---

## Próximos Passos Sugeridos

1. **Adicionar validação de CPF/CNPJ:** Não permitir ambos preenchidos no mesmo registro
2. **Adicionar botão "Novo Fornecedor":** Criar dialog de criação similar ao de edição
3. **Adicionar reativação:** Botão para reativar usuários inativados
4. **Adicionar histórico de recibos:** Na aba Fornecedores, mostrar quantos recibos cada fornecedor tem
