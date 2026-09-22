-- Criaçao da Tabela de RecibosCompra (requer tabela Usuarios criada)
CREATE TABLE IF NOT EXISTS RecibosCompra (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UsuarioId INTEGER NOT NULL,
    DataRecibo TEXT NOT NULL,
    CodigoRecibo TEXT NOT NULL, -- Valor aleatorio de 5 digitos
    ValorTotal REAL NOT NULL DEFAULT 0,
    PesoTotalKg REAL NOT NULL DEFAULT 0,
    CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),

    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
);

-- Deletar tabela
DROP TABLE RecibosCompra;

-- Insert de um Usuario de Teste
INSERT INTO Usuarios (Nome, Cpf, Cnpj, ChavePix)
VALUES ('Gabriel Fanto', "01353186024", NULL, "123456789")
ON CONFLICT(Nome) DO NOTHING;

-- Verificar o usuario criado
SELECT * from Usuarios where Nome = "Gabriel Fanto";

-- Insert de exemplo
INSERT INTO RecibosCompra(UsuarioId,DataRecibo,CodigoRecibo,ValorTotal,PesoTotalKg)
VALUES ((SELECT Id from Usuarios where Nome = "Gabriel Fanto"),"26/04/1996","12345",123.123,12.00);

-- Verificar o Recibo que foi criado
SELECT * from RecibosCompra where UsuarioId = (SELECT Id from Usuarios where Nome = "Gabriel Fanto");
