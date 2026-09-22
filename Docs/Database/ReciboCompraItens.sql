
-- Criação tabela ReciboCompraItens
-- Requer: tabelas RecibosCompra e Item
CREATE TABLE IF NOT EXISTS ReciboCompraItens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReciboCodigo INTEGER NOT NULL,
    ItemId INTEGER NOT NULL,
    PesoKg REAL NOT NULL CHECK (PesoKg >= 0),
    PrecoPorKg REAL NOT NULL DEFAULT 0,
    Total REAL NOT NULL DEFAULT 0,

    FOREIGN KEY (ReciboCodigo) REFERENCES RecibosCompra(CodigoRecibo) ON DELETE CASCADE,
    FOREIGN KEY (ItemId) REFERENCES Itens(Id)
);

-- Deletar tabela
--DROP TABLE RecibosCompra;
--DROP TABLE ReciboCompraItens;
-- Vai deletar o RecibosCompra também

-- INSERT DO ITEM CASO NÃO EXISTA
INSERT INTO Itens(Nome)
VALUES ("Placa Drive")
ON CONFLICT(Nome) DO NOTHING;

-- INSERT de exemplo
INSERT INTO ReciboCompraItens(ReciboCodigo,ItemId,PesoKg,PrecoPorKg,Total)
VALUES ("12345",(SELECT Id from Itens where Nome = "Placa Drive"),1.0,2.30,2.30);

-- SELECT para verificar
SELECT * FROM RecibosCompra WHERE Id IS NOT NULL;
SELECT * FROM ReciboCompraItens where Id IS NOT NULL;
