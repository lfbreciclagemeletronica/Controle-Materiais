-- Estrutura completa do banco de dados do ControleMateriais.Data
-- SQLite
-- Gerado a partir do Tabelas_Dados.md

PRAGMA foreign_keys = ON;

-- Tabela de Usuarios (clientes/fornecedores)
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

-- Garante que CPF ou CNPJ seja informado, mas nao ambos
CHECK ((Cpf IS NOT NULL AND Cnpj IS NULL) OR (Cpf IS NULL AND Cnpj IS NOT NULL));

-- Tabela de Itens do catalogo
CREATE TABLE IF NOT EXISTS Itens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL UNIQUE,
    Ordem INTEGER NOT NULL DEFAULT 0,
    Ativo INTEGER NOT NULL DEFAULT 1
);

-- Tabela de cabecalhos dos Recibos de Compra
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

-- Tabela de itens dos Recibos de Compra
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

-- Tabela de cabecalhos dos Recibos de Venda
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

-- Tabela de itens dos Recibos de Venda
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

-- Tabela fisica de Estoque (alternativa para performance)
CREATE TABLE IF NOT EXISTS Estoque (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ItemId INTEGER NOT NULL UNIQUE,
    TotalComprasKg REAL NOT NULL DEFAULT 0,
    TotalVendasKg REAL NOT NULL DEFAULT 0,
    SaldoKg REAL NOT NULL DEFAULT 0,
    AtualizadoEm TEXT NOT NULL DEFAULT (datetime('now')),

    FOREIGN KEY (ItemId) REFERENCES Itens(Id)
);

-- Tabela de Estoque Inicial mensal
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

-- VIEW padrao para estoque (compras - vendas)
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

-- VIEW de estoque que inclui estoque inicial (pode resultar em saldo negativo)
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

-- Tabela de Auditoria
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

-- Catalogo padrao de itens
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
ON CONFLICT(Nome) DO NOTHING;
