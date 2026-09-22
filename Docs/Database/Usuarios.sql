-- Criação da tabela
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

-- Exemplo de INSERT
INSERT INTO Usuarios (Nome, Cpf, Cnpj, ChavePix)
VALUES ('Maurício Amaral da Costa', NULL, NULL, NULL)
ON CONFLICT(Nome) DO NOTHING;

-- Select do usuario ativo
SELECT * FROM Usuarios where Ativo = 1;

-- Delete do usuario de ID 1
DELETE FROM Usuarios WHERE Id = 1;

-- Deletar tabela
DROP TABLE Usuarios;
