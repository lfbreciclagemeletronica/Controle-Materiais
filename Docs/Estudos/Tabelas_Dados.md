# Modelagem de Tabelas do Banco de Dados

Este documento define as melhores decisões de configuração das tabelas do banco de dados relacional do projeto `ControleMateriais.Data`.

---

## 1. Visão geral das tabelas

```text
[Usuarios]          1 ----< N    [RecibosCompra]
[Itens]             1 ----< N    [ReciboCompraItens]
[Itens]             1 ----< N    [RecibosVendaItens]
[RecibosVenda]      1 ----< N    [RecibosVendaItens]
[Itens]             1 ---- 1     [Estoque]
```

Relacionamento:

- Um **Usuário** pode ter vários **Recibos de Compra**.
- Um **Recibo de Compra** pode ter vários **Itens**.
- Um **Recibo de Venda** pode ter vários **Itens**.
- Um **Item** pode ter um único registro de **Estoque**.

---

## 2. Decisões importantes antes de modelar

### 2.1 Modelagem da tabela de itens

Você pediu para que cada coluna fosse um item. No entanto, o catálogo do `ControleMateriais` tem cerca de **50 itens diferentes**. Criar uma tabela com 50 colunas (uma para cada item) causa os seguintes problemas:

- Fica difícil adicionar novos itens (é preciso alterar a estrutura da tabela).
- Dificulta consultas e relatórios.
- Perde o controle de estoque individual.
- Dificulta a auditoria.

A **melhor prática** é usar uma tabela `Itens` com uma linha para cada item, e tabelas auxiliares `ReciboCompraItens` e `RecibosVendaItens` para registrar os pesos e preços de cada item dentro de cada recibo.

> Se por algum motivo de negócio for estritamente necessário uma tabela "larga" (colunas dinâmicas), apresentamos uma alternativa no final deste documento.

### 2.2 Tipos de dados no SQLite

O SQLite usa tipos dinâmicos, mas é importante declarar o tipo desejado para clareza e para que Dapper/ORMs mapeiem corretamente:

- `INTEGER`: identificadores, quantidades, flags booleanas (0/1).
- `TEXT`: nomes, CPF, CNPJ, chaves PIX, descrições.
- `REAL`: pesos em quilogramas (1.35 kg, 0.42 kg, etc.).
- `NUMERIC` ou `REAL`: valores monetários. **Cuidado**: `REAL` pode ter imprecisão decimal. Para dinheiro, prefira armazenar em centavos como `INTEGER` ou usar `DECIMAL` no C# com `TEXT` no banco em formato canônico. Neste documento usaremos `REAL` para simplificar, mas sugerimos estudar `INTEGER` de centavos para produção.

### 2.3 Chaves primárias e estrangeiras

- Sempre use `INTEGER PRIMARY KEY AUTOINCREMENT` para IDs.
- Use `FOREIGN KEY` para garantir integridade referencial.
- Ative `PRAGMA foreign_keys = ON` ao criar a conexão.

---

## 3. Tabela de Usuários

A tabela de usuários representa os **clientes/fornecedores** do sistema. Cada pessoa pode ter CPF ou CNPJ, mas nunca os dois ao mesmo tempo.

### SQL

```sql
CREATE TABLE IF NOT EXISTS Usuarios (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL,
    Cpf TEXT UNIQUE,
    Cnpj TEXT UNIQUE,
    ChavePix TEXT NOT NULL,
    CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),
    AtualizadoEm TEXT NOT NULL DEFAULT (datetime('now')),
    Ativo INTEGER NOT NULL DEFAULT 1,
);

-- Garante que CPF ou CNPJ seja informado, mas não ambos
CHECK ((Cpf IS NOT NULL AND Cnpj IS NULL) OR (Cpf IS NULL AND Cnpj IS NOT NULL));
```

### Decisões

- `Cpf` e `Cnpj` são colunas separadas para facilitar validação e formatação.
- Ambos são `UNIQUE` para evitar duplicatas.
- A `CHECK` constraint garante que apenas CPF **ou** CNPJ seja preenchido.
- `ChavePix` é obrigatória, pois os recibos precisam dessa informação.
- `Ativo` usa `INTEGER` com `1` (ativo) ou `0` (inativo), representando um booleano.

### Model C#

```csharp
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

---

## 4. Tabela de Itens

A tabela de itens armazena os produtos padrão do sistema. Os itens foram extraídos do catálogo do projeto (`ControleMateriais.Desktop/ItemCatalog.cs`).

### Lista de itens padrão

```text
Placa Drive
Placa Notebook A
Placa Notebook B
Placa Notebook C
Placa Mãe A
Placa Mãe B
Placa Mãe C
Placa Mãe D
Placa de Servidor
Placa Leve Especial
Placa Leve Especial com Ponta
Placa Leve Especial Completa
Placa Dourada A
Placa Dourada B
Placa Tapete A
Placa Tapete B
Placa Conectora
Placa Leve
Placa Leve com Ponta
Placa Intermediária A
Placa Intermediária B
Placa Intermediária C
Placa Pesada
Placa Pesada com Ponta
Placa Tablet
Placa Marrom
HD Completo
HD sem placa/Sucateado
Placa de HD
Placa de Celular Completa
Celular Sem tampa e bateria (Botão e Flip)
Celular Smart sem tampa e Sem bateria
Celular Smart Com Bateria Blindada
Celular Replicas blindado e sem Baterias
Memórias Douradas
Memória Prata
Processadores Plástico Chapa A
Processadores Plástico Chapa B
Processadores Plástico
Processadores Slot
Processadores Plástico Preto
Processadores Cerâmico A
Processadores Cerâmico B
Processadores Cerâmico C
Baterias de Notebook
Baterias de Tablet
Baterias de Celular
Fonte completa
Raio X
Desmanche Eletrônicos Consultar
```

### SQL

```sql
CREATE TABLE IF NOT EXISTS Itens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL UNIQUE,
    Ordem INTEGER NOT NULL DEFAULT 0,
    Ativo INTEGER NOT NULL DEFAULT 1
);
```

### Decisões

- `Nome` é `UNIQUE` para evitar duplicatas no catálogo.
- `Ordem` define a sequência de exibição na interface, seguindo a ordem apresentada no `ControleMateriais.Desktop`.
- `Ativo` permite desativar itens sem apagar o histórico.

### Model C#

```csharp
public class Item
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Ativo { get; set; }
}
```

### Script SQL para inserir os itens padrão

```sql
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
('Desmanche Eletrônicos Consultar', 50);
```

---

## 5. Tabela de Recibos de Compra

Armazena o cabeçalho de cada recibo de compra. O usuário está vinculado, e os itens ficam em `ReciboCompraItens`.

### SQL

```sql
CREATE TABLE IF NOT EXISTS RecibosCompra (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UsuarioId INTEGER NOT NULL, -- Id do usuario na tabela Usuarios
    NumeroRecibo INTEGER UNIQUE, -- exemplo 10072026083022 (10/07/2026 08:30:22)
    DataRecibo TEXT NOT NULL, -- formato 10/07/2026
    ValorTotal REAL NOT NULL DEFAULT 0,
    PesoTotalKg REAL NOT NULL DEFAULT 0,
    CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),

    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
);
```

### Decisões

- `UsuarioId` é chave estrangeira para `Usuarios`.
- `NumeroRecibo` é um identificador numérico baseado na data/hora (ex: `10072026083022` para 10/07/2026 08:30:22). `UNIQUE` evita duplicatas.
- `DataRecibo` é a data que aparece no recibo, armazenada como `TEXT` no formato `10/07/2026`.
- `ValorTotal` e `PesoTotalKg` são armazenados para facilitar consultas, mas podem ser recalculados dos itens.
- `PesoTotalKg` e todos os campos de peso usam o tipo `REAL`.

### Model C#

```csharp
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

---

## 6. Tabela de Itens dos Recibos de Compra

Registra cada item e o peso correspondente dentro de um recibo de compra.

### SQL

```sql
CREATE TABLE IF NOT EXISTS ReciboCompraItens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReciboCompraId INTEGER NOT NULL,
    ItemId INTEGER NOT NULL,
    PesoKg REAL NOT NULL CHECK (PesoKg >= 0), -- Peso total do item no recibo
    PrecoPorKg REAL NOT NULL DEFAULT 0, -- Preço que foi pago pelo Kg
    Total REAL NOT NULL DEFAULT 0, -- Total que foi pago pelo item

    FOREIGN KEY (ReciboCompraId) REFERENCES RecibosCompra(Id) ON DELETE CASCADE,
    FOREIGN KEY (ItemId) REFERENCES Itens(Id)
);
```

### Decisões

- `PesoKg` armazena valores como `1.35` (1 kg e 350 gramas), usando o tipo `REAL`.
- `PrecoPorKg` guarda o preço aplicado no momento do recibo.
- `Total` é `PesoKg * PrecoPorKg`. Pode ser calculado, mas armazenar facilita consultas.
- `ON DELETE CASCADE` remove os itens se o recibo for excluído.

### Model C#

```csharp
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

---

## 7. Tabela de Recibos de Venda

Semelhante à compra, mas representa vendas. O comprador pode ser uma pessoa/empresa diferente, então registramos o nome e a chave PIX do cliente.

### SQL

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

### Decisões

- `ClienteNome`, `ClienteCpf`, `ClienteCnpj` e `ClienteChavePix` ficam diretamente no recibo, pois o cliente pode não estar cadastrado como usuário.
- A `CHECK` garante que, se informado, apenas CPF **ou** CNPJ seja preenchido, nunca ambos.
- `ValorTotal` e `PesoTotalKg` são denormalizados para facilitar consultas.
- `PesoTotalKg` e todos os campos de peso usam o tipo `REAL`.

### Model C#

```csharp
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

---

## 8. Tabela de Itens dos Recibos de Venda

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

CREATE TABLE IF NOT EXISTS Estoque (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ItemId INTEGER NOT NULL UNIQUE,
    TotalComprasKg REAL NOT NULL DEFAULT 0,
    TotalVendasKg REAL NOT NULL DEFAULT 0,
    SaldoKg REAL NOT NULL DEFAULT 0,
    AtualizadoEm TEXT NOT NULL DEFAULT (datetime('now')),

    FOREIGN KEY (ItemId) REFERENCES Itens(Id)
);
```

### Model C#

```csharp
public class ReciboVendaItem
{
    public int Id { get; set; }
    public int ReciboVendaId { get; set; }
    public int ItemId { get; set; }
    public decimal PesoKg { get; set; }
    public decimal PrecoPorKg { get; set; }
    public decimal Total => PesoKg * PrecoPorKg;

    public string ItemNome { get; set; } = string.Empty;
}
```

---

## 9. Tabela de Estoque

O estoque é calculado a partir das movimentações: soma-se todas as compras de um item, soma-se todas as vendas do mesmo item e o saldo é o resultado de **compras menos vendas**. A opção **padrão e recomendada** é uma **VIEW** que calcula o saldo em tempo real. Também existe a opção de uma tabela física para performance.

### Opção padrão: VIEW (sempre atualizada)

A VIEW `EstoqueView` é a forma principal de consultar o estoque. Ela lê as tabelas `ReciboCompraItens` e `RecibosVendaItens` e calcula o saldo sem precisar de atualização manual.

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

### Model C# para a VIEW

```csharp
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

### Opção alternativa: tabela física

Se a consulta ficar lenta com muitos registros, crie uma tabela física e atualize-a periodicamente. Ela é desnormalizada e contém os mesmos campos da VIEW.

```sql
CREATE TABLE IF NOT EXISTS Estoque (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ItemId INTEGER NOT NULL UNIQUE,
    TotalComprasKg REAL NOT NULL DEFAULT 0,
    TotalVendasKg REAL NOT NULL DEFAULT 0,
    SaldoKg REAL NOT NULL DEFAULT 0,
    AtualizadoEm TEXT NOT NULL DEFAULT (datetime('now')),

    FOREIGN KEY (ItemId) REFERENCES Itens(Id)
);
```

### Decisões

- `TotalComprasKg`: soma de todos os pesos dos itens comprados.
- `TotalVendasKg`: soma de todos os pesos dos itens vendidos.
- `SaldoKg`: `TotalComprasKg - TotalVendasKg`.
- A **VIEW** é a opção padrão porque reflete mudanças nas compras e vendas sem sincronização manual.
- A **tabela física** é uma alternativa para cenários com muitos dados e consultas frequentes.

---

## 9.1. Tabela de Estoque Inicial

Além das compras e vendas, é possível registrar um **peso inicial** para cada item em um determinado mês/ano. Esse peso entra no cálculo do estoque como se fosse uma compra já existente antes do uso do sistema.

### SQL

```sql
CREATE TABLE IF NOT EXISTS EstoqueInicial (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ItemId INTEGER NOT NULL,
    Mes INTEGER NOT NULL, -- 1 a 12
    Ano INTEGER NOT NULL, -- exemplo: 2026
    PesoKg REAL NOT NULL DEFAULT 0, -- Peso inicial registrado
    CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),

    FOREIGN KEY (ItemId) REFERENCES Itens(Id),
    UNIQUE (ItemId, Mes, Ano)
);
```

### Cálculo do estoque com estoque inicial

A consulta abaixo soma o estoque inicial, as compras e as vendas. O saldo pode ficar **negativo** se as vendas forem maiores que o estoque inicial + compras.

```sql
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
LEFT JOIN EstoqueInicial ei ON ei.ItemId = i.Id AND ei.Mes = 7 AND ei.Ano = 2026
LEFT JOIN ReciboCompraItens rci ON rci.ItemId = i.Id
LEFT JOIN RecibosVendaItens rvi ON rvi.ItemId = i.Id
WHERE i.Ativo = 1
GROUP BY i.Id, i.Nome, i.Ordem, ei.PesoKg
ORDER BY i.Ordem;
```

> **Atenção**: essa consulta permite saldo negativo. Se o negócio não permitir venda sem estoque, adicione uma validação na aplicação antes de registrar a venda.

### Model C#

```csharp
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

### Exemplo de cálculo manual

```sql
SELECT
    i.Id,
    i.Nome,
    COALESCE(ei.PesoKg, 0) AS EstoqueInicialKg,
    (SELECT COALESCE(SUM(PesoKg), 0) FROM ReciboCompraItens WHERE ItemId = i.Id) AS TotalComprasKg,
    (SELECT COALESCE(SUM(PesoKg), 0) FROM RecibosVendaItens WHERE ItemId = i.Id) AS TotalVendasKg,
    COALESCE(ei.PesoKg, 0)
        + (SELECT COALESCE(SUM(PesoKg), 0) FROM ReciboCompraItens WHERE ItemId = i.Id)
        - (SELECT COALESCE(SUM(PesoKg), 0) FROM RecibosVendaItens WHERE ItemId = i.Id) AS SaldoKg
FROM Itens i
LEFT JOIN EstoqueInicial ei ON ei.ItemId = i.Id AND ei.Mes = 7 AND ei.Ano = 2026
WHERE i.Ativo = 1;
```

---

## 10. Script completo de criação das tabelas

```sql
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Usuarios (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL,
    Cpf TEXT UNIQUE,
    Cnpj TEXT UNIQUE,
    ChavePix TEXT NOT NULL,
    CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),
    AtualizadoEm TEXT NOT NULL DEFAULT (datetime('now')),
    Ativo INTEGER NOT NULL DEFAULT 1,
);

-- Garante que CPF ou CNPJ seja informado, mas não ambos
CHECK ((Cpf IS NOT NULL AND Cnpj IS NULL) OR (Cpf IS NULL AND Cnpj IS NOT NULL));

CREATE TABLE IF NOT EXISTS Itens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL UNIQUE,
    Ordem INTEGER NOT NULL DEFAULT 0,
    Ativo INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS RecibosCompra (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UsuarioId INTEGER NOT NULL, -- Id do usuario na tabela Usuarios
    NumeroRecibo INTEGER UNIQUE, -- exemplo 10072026083022 (10/07/2026 08:30:22)
    DataRecibo TEXT NOT NULL, -- formato 10/07/2026
    ValorTotal REAL NOT NULL DEFAULT 0,
    PesoTotalKg REAL NOT NULL DEFAULT 0,
    CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),

    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
);

CREATE TABLE IF NOT EXISTS ReciboCompraItens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReciboCompraId INTEGER NOT NULL,
    ItemId INTEGER NOT NULL,
    PesoKg REAL NOT NULL CHECK (PesoKg >= 0), -- Peso total do item no recibo
    PrecoPorKg REAL NOT NULL DEFAULT 0, -- Preço que foi pago pelo Kg
    Total REAL NOT NULL DEFAULT 0, -- Total que foi pago pelo item

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

-- Tabela para estoque inicial mensal
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

-- VIEW padrão para estoque (compras - vendas)
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

-- VIEW que permite saldo negativo ao incluir estoque inicial
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
```

---

## 11. Alternativa: tabela larga com coluna por item

Se o negócio exigir uma tabela onde cada coluna seja um item (por exemplo, para importação/exportação de planilhas legadas), é possível, mas **não recomendado** como estrutura principal.

### Exemplo de estrutura (não recomendado)

```sql
CREATE TABLE IF NOT EXISTS RecibosCompraLarga (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UsuarioId INTEGER NOT NULL,
    DataRecibo TEXT NOT NULL,
    ValorTotal REAL NOT NULL,
    PesoTotalKg REAL NOT NULL,
    -- Uma coluna para cada item do catálogo
    [Placa Drive] REAL DEFAULT 0,
    [Placa Notebook A] REAL DEFAULT 0,
    [HD Completo] REAL DEFAULT 0,
    -- ... mais 50 colunas
    CriadoEm TEXT NOT NULL DEFAULT (datetime('now'))
);
```

### Problemas dessa abordagem

- Adicionar um novo item exige `ALTER TABLE`.
- Nomes com espaços e caracteres especiais exigem colchetes `[...]`.
- Consultas dinâmicas ficam complexas.
- Dificulta relatórios e auditoria.
- Não escala bem.

### Quando usar?

Apenas para integração temporária com planilhas ou importações. Os dados devem ser normalizados para a estrutura principal após a importação.

---

## 12. Exemplos de consultas

Abaixo estão exemplos práticos de consultas SQL que você pode usar no `ControleMateriais.Data`.

### 12.1 Listar todos os itens de um recibo de compra

```sql
SELECT
    rc.Id,
    rc.DataRecibo,
    u.Nome AS Usuario,
    u.ChavePix,
    i.Nome AS Item,
    rci.PesoKg,
    rci.PrecoPorKg,
    rci.Total
FROM RecibosCompra rc
JOIN Usuarios u ON u.Id = rc.UsuarioId
JOIN ReciboCompraItens rci ON rci.ReciboCompraId = rc.Id
JOIN Itens i ON i.Id = rci.ItemId
WHERE rc.Id = 1
ORDER BY i.Ordem;
```

### 12.2 Total comprado por item em um período

```sql
SELECT
    i.Nome AS Item,
    SUM(rci.PesoKg) AS TotalKg,
    SUM(rci.Total) AS TotalGasto
FROM ReciboCompraItens rci
JOIN Itens i ON i.Id = rci.ItemId
JOIN RecibosCompra rc ON rc.Id = rci.ReciboCompraId
WHERE rc.DataRecibo BETWEEN '01/07/2026' AND '31/07/2026'
GROUP BY i.Nome
ORDER BY i.Ordem;
```

### 12.3 Total vendido por item em um período

```sql
SELECT
    i.Nome AS Item,
    SUM(rvi.PesoKg) AS TotalKg,
    SUM(rvi.Total) AS TotalRecebido
FROM RecibosVendaItens rvi
JOIN Itens i ON i.Id = rvi.ItemId
JOIN RecibosVenda rv ON rv.Id = rvi.ReciboVendaId
WHERE rv.DataVenda BETWEEN '01/07/2026' AND '31/07/2026'
GROUP BY i.Nome
ORDER BY i.Ordem;
```

### 12.4 Consultar estoque atual

```sql
SELECT
    ItemId,
    ItemNome,
    TotalComprasKg,
    TotalVendasKg,
    SaldoKg
FROM EstoqueView
ORDER BY Ordem;
```

### 12.5 Consultar estoque com saldo negativo

```sql
SELECT
    ItemId,
    ItemNome,
    EstoqueInicialKg,
    TotalComprasKg,
    TotalVendasKg,
    SaldoKg
FROM EstoqueInicialView
WHERE SaldoKg < 0
ORDER BY SaldoKg ASC;
```

### 12.6 Listar recibos de venda de um cliente

```sql
SELECT
    rv.Id,
    rv.NumeroRecibo,
    rv.DataVenda,
    rv.ValorTotal,
    rv.PesoTotalKg,
    i.Nome AS Item,
    rvi.PesoKg,
    rvi.PrecoPorKg
FROM RecibosVenda rv
JOIN RecibosVendaItens rvi ON rvi.ReciboVendaId = rv.Id
JOIN Itens i ON i.Id = rvi.ItemId
WHERE rv.ClienteNome LIKE '%João%'
ORDER BY rv.DataVenda DESC;
```

### 12.7 Verificar quanto um usuário vendeu para a empresa

```sql
SELECT
    u.Nome AS Usuario,
    COUNT(rc.Id) AS QuantidadeRecibos,
    SUM(rc.PesoTotalKg) AS TotalKg,
    SUM(rc.ValorTotal) AS TotalReais
FROM RecibosCompra rc
JOIN Usuarios u ON u.Id = rc.UsuarioId
WHERE rc.DataRecibo BETWEEN '01/07/2026' AND '31/07/2026'
GROUP BY u.Nome
ORDER BY TotalKg DESC;
```

### 12.8 Buscar itens com estoque inicial em um mês

```sql
SELECT
    i.Nome AS Item,
    ei.PesoKg
FROM EstoqueInicial ei
JOIN Itens i ON i.Id = ei.ItemId
WHERE ei.Mes = 7 AND ei.Ano = 2026
ORDER BY i.Ordem;
```

---

## 13. Dicas finais

- Sempre use `PRAGMA foreign_keys = ON;` ao abrir a conexão SQLite.
- Use `decimal` no C# e `REAL` no SQLite para pesos, mas avalie `INTEGER` para valores monetários em produção.
- Mantenha o catálogo de itens em uma tabela separada para facilitar manutenção.
- Crie índices em colunas frequentemente consultadas, como `DataRecibo`, `UsuarioId` e `ItemId`.
- Use transações ao inserir um recibo e seus itens para garantir integridade.

---

## 14. Links para aprofundar

- [Documentação do SQLite](https://www.sqlite.org/docs.html)
- [Tipos de dados do SQLite](https://www.sqlite.org/datatype3.html)
- [Foreign keys no SQLite](https://www.sqlite.org/foreignkeys.html)
- [Dapper](https://github.com/DapperLib/Dapper)
- [Modelagem de banco de dados relacional](https://learn.microsoft.com/pt-br/aspnet/core/data/ef-mvc/intro/)
- [SQL Injection](https://learn.microsoft.com/pt-br/sql/relational-databases/security/sql-injection)
