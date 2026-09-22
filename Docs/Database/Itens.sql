-- CRIA A TABELA DE ITENS
CREATE TABLE IF NOT EXISTS Itens (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Nome TEXT NOT NULL UNIQUE,
  Ordem INTEGER NOT NULL DEFAULT 0,
  Ativo INTEGER NOT NULL DEFAULT 1
)

-- INSERE OS ITENS PADRÕES
INSERT INTO Itens (Nome, Ordem) VALUES
('Placa Drive', 1),
('Placa Notebook A', 2),
('Placa Notebook B', 3),
('Placa Notebook C', 4),
('Placa Mae A', 5),
('Placa Mae B', 6),
('Placa Mae C', 7),
('Placa Mae D', 8),
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
('Celular Sem tampa e bateria (Botao e Flip)', 31),
('Celular Smart sem tampa e Sem bateria', 32),
('Celular Smart Com Bateria Blindada', 33),
('Celular Replicas blindado e sem Baterias', 34),
('Mem¢rias Douradas', 35),
('Mem¢ria Prata', 36),
('Processadores Plástico Chapa A', 37),
('Processadores Plástico Chapa B', 38),
('Processadores Plástico', 39),
('Processadores Slot', 40),
('Processadores Pl stico Preto', 41),
('Processadores Cerâmico A', 42),
('Processadores Cerâmico B', 43),
('Processadores CerÂmico C', 44),
('Baterias de Notebook', 45),
('Baterias de Tablet', 46),
('Baterias de Celular', 47),
('Fonte completa', 48),
('Raio X', 49),
('Desmanche Eletrônicos Consultar', 50)
ON CONFLICT(Nome) DO NOTHING;

-- BUSCA TODOS OS ITENS ATIVOS
SELECT * FROM Itens where Ativo = 1;

-- DELETA TODA A TABELA
DROP TABLE Itens;
