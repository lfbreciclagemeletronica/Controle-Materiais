# Release 5.0.8 - Resumo de Modificações

Esta release é focada na **implementação real do banco de dados SQLite** (`ControleMateriais.Data`) como
fonte de verdade para Fornecedores, Recibos de Compra e Recibos de Venda — substituindo/complementando
o fluxo baseado apenas em PDFs e JSONs que existia desde a Release 5.0.7 (onde a sincronização do
banco-de-dados estava "pausada — Em Desenvolvimento").

## Geral

### 1. Versão do projeto atualizada para 5.0.8
- **Arquivos:** `ControleMateriais.Desktop/ControleMateriais.Desktop.csproj`, `ControleMateriais.Desktop/Views/HomeView.axaml`
- **Alteração:** `<Version>` de `5.0.7` para `5.0.8`; label na Home atualizado para `V5.0.8`

---

## 1. Conexão entre `ControleMateriais.Data` e `ControleMateriais.Desktop`

### 2. Referência de projeto adicionada
- **Arquivo:** `ControleMateriais.Desktop/ControleMateriais.Desktop.csproj`
- **Alteração:** `dotnet add reference ..\ControleMateriais.Data\ControleMateriais.Data.csproj`
- **Motivo:** Permitir que a camada Desktop use os repositórios/models do `ControleMateriais.Data` (Dapper + Microsoft.Data.Sqlite)

### 3. Service locator `DataService`
- **Arquivo (novo):** `ControleMateriais.Desktop/DataService.cs`
- **Alteração:** Classe estática expondo `UsuarioRepository`, `ItemRepository`, `ReciboCompraRepository`, `ReciboVendaRepository`, `EstoqueRepository`
- **Motivo:** Ponto único de acesso aos repositórios sem precisar de injeção de dependência completa

### 4. Inicialização do banco no startup
- **Arquivos:** `ControleMateriais.Desktop/Program.cs`, `ControleMateriais.Desktop/App.axaml.cs`
- **Alteração:** `DatabaseInitializer.EnsureDatabase()` movido de `Program.Main` para o callback `onFinished` da `SplashWindow` (depois do pull do banco de dados, ver seção 10)
- **Motivo:** Evitar que o `database.db` seja criado localmente **antes** do `git clone`/`pull` do repositório remoto, o que causaria conflito de checkout (`error: untracked working tree files would be overwritten`)

---

## 2. Esquema do Banco de Dados (SQLite)

O banco vive em `Downloads/ControleMateriaisLFB/banco-de-dados/database.db` — a mesma pasta que já é
um repositório Git (`banco-de-dados`), permitindo versionar o arquivo binário junto com os JSONs legados.

### 5. Tabelas principais criadas em `DatabaseInitializer.EnsureDatabase()`

```sql
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Itens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL UNIQUE,
    Ordem INTEGER NOT NULL DEFAULT 0,
    Ativo INTEGER NOT NULL DEFAULT 1
);

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

CREATE TABLE IF NOT EXISTS RecibosCompra (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UsuarioId INTEGER NOT NULL,
    DataRecibo TEXT NOT NULL,
    CodigoRecibo TEXT NOT NULL,       -- código aleatório de 5 dígitos
    ValorTotal REAL NOT NULL DEFAULT 0,
    PesoTotalKg REAL NOT NULL DEFAULT 0,
    CriadoEm TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
);

CREATE TABLE IF NOT EXISTS ReciboCompraItens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReciboCodigo TEXT NOT NULL,
    ItemId INTEGER NOT NULL,
    PesoKg REAL NOT NULL CHECK (PesoKg >= 0),
    PrecoPorKg REAL NOT NULL DEFAULT 0,
    Total REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (ReciboCodigo) REFERENCES RecibosCompra(CodigoRecibo) ON DELETE CASCADE,
    FOREIGN KEY (ItemId) REFERENCES Itens(Id)
);

-- Exigido pelo SQLite: a coluna referenciada por uma FOREIGN KEY precisa ser
-- PRIMARY KEY ou ter um índice UNIQUE. Sem isso o INSERT falha com "foreign key mismatch".
CREATE UNIQUE INDEX IF NOT EXISTS UX_RecibosCompra_CodigoRecibo ON RecibosCompra(CodigoRecibo);

CREATE TABLE IF NOT EXISTS RecibosVenda (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClienteNome TEXT NOT NULL,
    DataVenda TEXT NOT NULL,
    CodigoRecibo TEXT NOT NULL,       -- código aleatório de 6 dígitos
    ValorTotal REAL NOT NULL DEFAULT 0,
    PesoTotalKg REAL NOT NULL DEFAULT 0,
    CriadoEm TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_RecibosVenda_CodigoRecibo ON RecibosVenda(CodigoRecibo);

CREATE TABLE IF NOT EXISTS RecibosVendaItens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReciboCodigo TEXT NOT NULL,
    ItemId INTEGER NOT NULL,
    PesoKg REAL NOT NULL CHECK (PesoKg >= 0),
    PrecoPorKg REAL NOT NULL DEFAULT 0,
    Total REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (ReciboCodigo) REFERENCES RecibosVenda(CodigoRecibo) ON DELETE CASCADE,
    FOREIGN KEY (ItemId) REFERENCES Itens(Id)
);

CREATE VIEW IF NOT EXISTS EstoqueView AS
SELECT
    i.Id AS ItemId, i.Nome AS ItemNome, i.Ordem,
    COALESCE(SUM(rci.PesoKg), 0) AS TotalComprasKg,
    0 AS TotalVendasKg,
    COALESCE(SUM(rci.PesoKg), 0) AS SaldoKg
FROM Itens i
LEFT JOIN ReciboCompraItens rci ON rci.ItemId = i.Id
WHERE i.Ativo = 1
GROUP BY i.Id, i.Nome, i.Ordem
ORDER BY i.Ordem;
```

- **Arquivo:** `ControleMateriais.Data/Services/DatabaseInitializer.cs`
- **Motivo:** Esquema oficial pedido pelo usuário, com `Usuarios` (fornecedores), `Itens` (catálogo de materiais),
  `RecibosCompra`/`ReciboCompraItens` (recibos de compra de material) e, mais adiante nesta release,
  `RecibosVenda`/`RecibosVendaItens` (recibos de venda de estoque)
- **Padrão de relacionamento:** em vez de `ReciboCompraId`/`ReciboVendaId` (chave numérica interna),
  os itens referenciam o **código do recibo** (`ReciboCodigo`), que é o identificador visível ao usuário
  (impresso no PDF). Isso exige o índice único mencionado acima.

### 6. Migração automática de esquemas antigos
- **Arquivo:** `ControleMateriais.Data/Services/DatabaseInitializer.cs`
- **Método:** `MigrarEsquemaExistente()`
- **Alteração:** Como `CREATE TABLE IF NOT EXISTS` **não altera** tabelas já existentes, esse método
  verifica via `PRAGMA table_info(ReciboCompraItens)` se a coluna `ReciboCodigo` ainda está com o tipo
  antigo (`INTEGER`) e, se estiver, recria a tabela com o tipo correto (`TEXT`), preservando os dados:
  ```sql
  ALTER TABLE ReciboCompraItens RENAME TO ReciboCompraItens_old;
  CREATE TABLE ReciboCompraItens (...);  -- schema novo
  INSERT INTO ReciboCompraItens (Id, ReciboCodigo, ItemId, PesoKg, PrecoPorKg, Total)
      SELECT Id, CAST(ReciboCodigo AS TEXT), ItemId, PesoKg, PrecoPorKg, Total
      FROM ReciboCompraItens_old;
  DROP TABLE ReciboCompraItens_old;
  ```
- **Motivo:** Corrigir, sem perda de dados, bancos criados antes da correção do bug de "foreign key mismatch" (seção 8)

### 7. Seed de itens só quando a tabela está vazia
- **Arquivo:** `ControleMateriais.Data/Services/DatabaseInitializer.cs`
- **Método:** `SeedItemsIfEmpty()`
- **Alteração:** `SELECT COUNT(*) FROM Itens` antes de inserir os ~50 materiais padrão; só popula se a tabela estiver vazia
- **Motivo:** Preservar itens já cadastrados/editados em bancos existentes ao rodar `EnsureDatabase()` novamente

---

## 3. Bug crítico: "foreign key mismatch" ao salvar recibo

### 8. Correção do índice único e do tipo de coluna
- **Sintoma:** `SQLite Error 1: 'foreign key mismatch - "ReciboCompraItens" referencing "RecibosCompra"'`
  ao tentar inserir os itens do recibo, mesmo com o cabeçalho `RecibosCompra` salvo com sucesso
- **Causa raiz:** o SQLite exige que a coluna referenciada por uma `FOREIGN KEY` seja `PRIMARY KEY` ou tenha
  um índice `UNIQUE`. `RecibosCompra.CodigoRecibo` era `TEXT NOT NULL` **sem** `UNIQUE`; além disso,
  `ReciboCompraItens.ReciboCodigo` estava `INTEGER` enquanto `CodigoRecibo` era `TEXT` (tipos divergentes)
- **Correção:** `CREATE UNIQUE INDEX ... ON RecibosCompra(CodigoRecibo)` + migração do tipo da coluna (seção 6)
- **Arquivo:** `ControleMateriais.Data/Services/DatabaseInitializer.cs`

### 9. Bug de binding de parâmetros Dapper + Microsoft.Data.Sqlite
- **Sintoma:** `Must add values for the following parameters: @nome, @cpf, @cnpj, @chavePix, @atualizadoEm, @ativo, @id`
  ao salvar a edição de um fornecedor
- **Causa raiz:** quando um objeto POCO é passado diretamente ao Dapper (`ExecuteAsync(sql, usuario)`), o
  binding via reflexão nesse combo específico **não é totalmente case-insensitive** — os placeholders no SQL
  (`@nome`) precisam ter exatamente o mesmo case das propriedades do objeto (`Nome`)
- **Correção:** placeholders alterados para PascalCase (`@Nome`, `@Cpf`, `@Id`, ...), igual às propriedades dos models
- **Arquivos:** `ControleMateriais.Data/Repositories/SqliteUsuarioRepository.cs`, `SqliteItemRepository.cs`

### 10. Erro de compilação por aspas não escapadas em string verbatim
- **Sintoma:** ~40 erros em cascata (`CS1002`, `CS1010`, `CS1003`...) em `DatabaseInitializer.cs`
- **Causa raiz:** um comentário SQL com aspas duplas (`"foreign key mismatch"`) dentro de uma string `@"..."` do
  C#, sem escapar como `""`, fechou a string prematuramente
- **Correção:** aspas removidas do comentário; nenhuma alteração de comportamento, só erro de compilação corrigido

---

## 4. Persistência do Recibo de Compra no Banco de Dados

### 11. Novos métodos "get or create" nos repositórios
- **Arquivos:** `IUsuarioRepository`/`SqliteUsuarioRepository`, `IItemRepository`/`SqliteItemRepository`
- **Método:** `GetOrCreateAsync(string nome)`
  ```sql
  INSERT INTO Usuarios (Nome, Cpf, Cnpj, ChavePix) VALUES (@nome, NULL, NULL, NULL)
      ON CONFLICT(Nome) DO NOTHING;
  SELECT * FROM Usuarios WHERE Nome = @nome;
  ```
  (mesma lógica para `Itens`)
- **Motivo:** Cadastrar fornecedor/item automaticamente se não existir, sem duplicar (usa `ON CONFLICT DO NOTHING`
  + `SELECT` para sempre retornar o registro com `Id`, seja ele novo ou já existente)

### 12. Geração de código de recibo único
- **Arquivos:** `IReciboCompraRepository`/`SqliteReciboCompraRepository`, `IReciboVendaRepository`/`SqliteReciboVendaRepository`
- **Método:** `GerarCodigoReciboAsync()`
  ```sql
  SELECT COUNT(*) FROM RecibosCompra WHERE CodigoRecibo = @codigo;  -- (ou RecibosVenda)
  ```
- **Alteração:** Gera um número aleatório (`Random.Shared.Next`) — **5 dígitos** para compras (10000–99999),
  **6 dígitos** para vendas (100000–999999) — e verifica unicidade antes de usar (até 20 tentativas)

### 13. Fluxo completo de gravação do recibo de compra
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/MainWindowViewModel.cs`
- **Métodos:** `GarantirFornecedorCadastradoAsync()`, `SalvarReciboNoBancoDeDadosAsync()`, chamados dentro de `ExportarAsync()`
- **Ordem de execução (na sequência das dependências de FK):**
  1. PDF do recibo é gerado normalmente
  2. `Usuarios` — `GetOrCreateAsync(nomeCliente)` garante o fornecedor e retorna o `Id`
  3. `RecibosCompra` — monta o cabeçalho (`UsuarioId`, `DataRecibo`, `CodigoRecibo`, `ValorTotal`, `PesoTotalKg`)
  4. `Itens` — para cada material pesado (catálogo + itens extras), `GetOrCreateAsync(nome)` garante o item
  5. `ReciboCompraItens` — `InsertAsync` grava o cabeçalho e os itens numa única transação
- **Comportamento:** falhas na gravação do banco **não bloqueiam** a emissão do PDF nem a sincronização
  Git — apenas exibem um aviso (toast)

---

## 5. Sincronização Git do Banco de Dados

### 14. Pull do banco-de-dados reativado no Splash
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/SplashViewModel.cs`
- **Alteração:** Removido o placeholder "Em Desenvolvimento" da Release 5.0.7;
  `PullBancoDadosAsync()` volta a rodar em paralelo com Recibos e Pesagens ao abrir o app
- **Motivo:** Garantir que o `database.db` local esteja atualizado antes do uso

### 15. Push do banco de dados após salvar um recibo
- **Arquivo (novo método):** `ControleMateriais.Desktop/Services/GitHubService.cs` → `PublicarArquivoBancoDadosAsync()`
- **Alteração:** Faz `git fetch`/`rebase` (pull) e depois `git add database.db` + `git commit` + `git push`
  de um arquivo binário já existente no repositório (diferente de `PublicarJsonBancoDadosAsync`, que
  **escreve** conteúdo textual antes de comitar)
- **Uso:** Chamado em `MainWindowViewModel.SalvarReciboNoBancoDeDadosAsync()` logo após o `InsertAsync` ter sucesso
- **Motivo:** Cada recibo salvo localmente também é enviado ao repositório remoto, mantendo o `database.db`
  sincronizado entre máquinas

---

## 6. Módulo de Fornecedores (CRUD completo sobre `Usuarios`)

### 16. Nova aba "Fornecedores" no Sistema de Recibos
- **Arquivos:** `PesagensView.axaml`/`.axaml.cs`, `PesagensViewModel.cs`
- **Alteração:** Lista de fornecedores (Nome, CNPJ, Chave Pix, Status) com busca por nome, botões Editar/Inativar
- **SQL usado:**
  ```sql
  SELECT * FROM Usuarios WHERE Ativo = 1 ORDER BY Nome;   -- GetAllAsync (dropdown de recibos)
  SELECT * FROM Usuarios ORDER BY Nome;                    -- GetTodosAsync (tela de Fornecedores, ativos e inativos)
  UPDATE Usuarios SET Nome=@Nome, Cpf=@Cpf, Cnpj=@Cnpj, ChavePix=@ChavePix, AtualizadoEm=@AtualizadoEm, Ativo=@Ativo WHERE Id=@Id;
  UPDATE Usuarios SET Ativo = 0, AtualizadoEm = @atualizadoEm WHERE Id = @id;   -- "Inativar" (soft-delete)
  UPDATE Usuarios SET Ativo = 1, AtualizadoEm = @atualizadoEm WHERE Id = @id;   -- "Reativar"
  ```
- **Motivo:** "Inativar" em vez de "Excluir" — a exclusão física quebraria a `FOREIGN KEY` de `RecibosCompra.UsuarioId`
  e apagaria o histórico de compras daquele fornecedor. O soft-delete (`Ativo = 0`) preserva os recibos já emitidos.

### 17. Status visual e reativação por clique
- **Arquivo:** `PesagensView.axaml`
- **Alteração:** Coluna "Status" mostra "Ativo" (verde) / "Inativo" (vermelho); clicar no status inativo
  pergunta se deseja reativar (chama `ReativarAsync`)

### 18. Autocompletar de fornecedor + cadastro automático
- **Arquivos:** `ReciboView.axaml`, `WeightCalculatorView.axaml`, `MainWindowViewModel.cs`, `WeightCalculatorViewModel.cs`
- **Alteração:** Campo de nome do cliente trocado de `TextBox` para `AutoCompleteBox` com `FilterMode="Contains"`,
  alimentado por `NomesFornecedores` (`Usuarios WHERE Ativo = 1`)
- **Motivo:** Se o nome digitado não existir na tabela, ele é cadastrado automaticamente após o recibo ser
  processado (via `GetOrCreateAsync`), evitando cadastro manual repetitivo

---

## 7. Controle de Estoque — Novas Abas Ligadas ao Banco de Dados

### 19. Aba "Compras" (recibos de compra do banco)
- **Arquivos:** `EstoqueView.axaml`, `EstoqueViewModel.cs`
- **Alteração:** Lista Cliente/Código/Data/Valor Total vinda de `IReciboCompraRepository.GetAllAsync()`
  (`JOIN` com `Usuarios` para trazer o nome); busca por cliente, código, data ou valor;
  ordenação corrigida para usar a **data real parseada** (`DateTime`) em vez de ordenação alfabética da
  string `dd/MM/yyyy` (que ordenava errado); clique na linha abre modal com os itens (`EntradaDetalhesDialog`)

### 20. Aba "Estoque por Mês" (quantidade por item, por mês)
- **Arquivos:** `EstoqueView.axaml`, `EstoqueViewModel.cs`
- **Novo model:** `ControleMateriais.Data/Models/ItemMensal.cs`
- **SQL (`IReciboCompraRepository.GetTotaisPorMesAsync()`):**
  ```sql
  SELECT
      substr(rc.DataRecibo, 4, 2) || '/' || substr(rc.DataRecibo, 7, 4) AS Mes,
      i.Nome AS ItemNome,
      SUM(rci.PesoKg) AS TotalKg
  FROM ReciboCompraItens rci
  JOIN RecibosCompra rc ON rc.CodigoRecibo = rci.ReciboCodigo
  JOIN Itens i ON i.Id = rci.ItemId
  GROUP BY Mes, i.Nome
  ORDER BY Mes DESC, i.Nome;
  ```
- **Motivo:** Como `DataRecibo` é texto `dd/MM/yyyy`, o mês/ano é extraído com `substr()` (não é possível
  usar `strftime()` do SQLite diretamente, pois essa função espera o formato ISO `yyyy-MM-dd`)
- **UI:** `ComboBox` de meses (rótulo "setembro/2026" via `MesOpcao`) + lista Material/Total no Mês

### 21. Aba "Vendas" (recibos de venda do banco) + importação de PDFs existentes
- **Novas tabelas:** `RecibosVenda`/`RecibosVendaItens` (seção 5 do esquema)
- **Novo model:** `ControleMateriais.Data/Models/ReciboVenda.cs` (simplificado: `ClienteNome`, `DataVenda`,
  `CodigoRecibo`, `ValorTotal`, `PesoTotalKg`)
- **Novo serviço:** `ControleMateriais.Desktop/Services/MigrarRecibosVendaParaBancoService.cs`
  - Lê todos os PDFs em `Recibos_Venda/`
  - Extrai cliente e data do **nome do arquivo** (suporta os dois padrões: `NOME_ddMMyyyyHHmmss.pdf` novo
    e `NOME_dd-MM-yyyy.pdf` antigo — ver Release 5.0.7, seção "Padrão de Nome de Arquivo de Recibo")
  - Extrai os itens/pesos reaproveitando `ReciboParserService.ExtrairPesos()` (o mesmo parser de PDF usado
    para recibos de compra) e o valor total via `ReciboParserService.ExtrairCabecalho()`
  - Garante cada item em `Itens` (`GetOrCreateAsync`), gera o código de 6 dígitos e grava tudo via
    `IReciboVendaRepository.InsertAsync`
  - **Idempotente:** compara `ClienteNome` + `DataVenda` com os registros já existentes para não duplicar
    em execuções repetidas
- **UI:** botão "Importar PDFs" na nova aba "Vendas" do Controle de Estoque; lista Cliente/Código/Data/Valor
  Total; clique abre modal (`VendaDetalhesDialog`) com Material + Peso (Kg) de cada item
  (recibos de venda **não têm** preço por item no PDF original — apenas o valor total da venda — por isso
  o modal de vendas não exibe coluna de valor por item, diferente do modal de compras)

### 22. Botão "Migrar Vendas" (baseado em JSON) removido
- **Arquivos:** `EstoqueView.axaml`, `EstoqueViewModel.cs`
- **Alteração:** Removidos `Migrando`, `MigrarVendasCommand`, `MigrarVendasAsync()` (gerava arquivos
  `venda-DD-MM-YYYY.json`) — substituído pelo fluxo de importação para o banco SQLite (seção 21)

---

## 8. Reorganização das Telas "Sistema de Recibos" e "Controle de Estoque"

### 23. Aba "Recibos" renomeada para "Compras"
- **Arquivo:** `PesagensView.axaml`
- **Motivo:** Deixar explícito que a aba trata de recibos de **compra** de material, já que agora existe
  também uma aba de **Vendas**

### 24. Aba "Vendas" movida do Controle de Estoque para o Sistema de Recibos
- **Arquivos:** `PesagensViewModel.cs` (nova propriedade `EstoqueVM`), `MainWindowViewModel.cs`, `PesagensView.axaml/.axaml.cs`
- **Alteração:** Em vez de duplicar a lógica de PDFs de venda, a aba "Vendas" do Sistema de Recibos
  **reaproveita a mesma instância** de `EstoqueViewModel` (via `PesagensViewModel.EstoqueVM`), preservando
  filtros, abertura de PDF e recálculo de estoque
- **Botão "Excluir"** removido dessa lista (recibos de venda não podem mais ser excluídos por essa tela)

### 25. Botões "Nova Compra" / "Nova Venda"
- **Arquivo:** `PesagensView.axaml`
- **Alteração:** `"+ Novo"` → `"Nova Compra"`; novo botão `"Nova Venda"` navegando direto para a tela de Venda
  (`PesagensViewModel.IrParaVendaCommand`, ligado por `MainWindowViewModel`)

### 26. Ícones removidos de todos os botões
- **Arquivos:** `PesagensView.axaml`, `EstoqueView.axaml`
- **Alteração:** Removidos emojis/glyphs (`📄`, `🗑`, `🔄`, `✏`, `💰`, `📈`, `⟳`) que apareciam como
  losangos quebrados (`�`) por falta de suporte de fonte; botões passaram a ser só texto

### 27. Botão "Venda" removido do Controle de Estoque
- **Arquivo:** `EstoqueView.axaml`
- **Motivo:** Substituído pelo botão "Nova Venda" no Sistema de Recibos (seção 25)

---

## 9. Observabilidade — Sistema de Logs com CorrelationId

### 28. `AppLogger` — logger em memória com rastreamento por operação
- **Arquivo (novo):** `ControleMateriais.Desktop/Services/AppLogger.cs`
- **Alteração:** `AppLogger.NewCorrelationId()` gera um ID curto (8 caracteres) por operação;
  `Info/Aviso/Erro(correlationId, origem, mensagem)` grava no Console/Debug e numa
  `ObservableCollection<LogEntry>` em memória (até 2000 entradas)
- **Motivo:** Permitir rastrear **todas as mensagens de um mesmo processamento** (ex.: salvar um recibo,
  sincronizar, editar fornecedor) filtrando pelo `CorrelationId`, sem precisar de arquivo de log

### 29. Janela "Ver Logs"
- **Arquivos (novos):** `LogViewerWindow.axaml/.axaml.cs`, `LogLevelBrushConverter.cs`
- **Alteração:** Janela não-modal com lista de logs em tempo real (auto-scroll), busca por texto/CorrelationId,
  cores por nível (Info/Aviso/Erro); acessível pelo botão "Ver Logs" na tela inicial (`HomeView`)

### 30. Instrumentação dos fluxos principais
- **Arquivos:** `MainWindowViewModel.cs`, `PesagensViewModel.cs`, `WeightCalculatorViewModel.cs`, `VendaViewModel.cs`, `EstoqueViewModel.cs`
- **Alteração:** Geração de recibo, CRUD de fornecedores, sincronizações Git, salvar pesagem/venda e
  importação de vendas para o banco agora geram logs detalhados com `CorrelationId`

---

## Resumo das Tabelas Novas/Alteradas

| Tabela | Situação | Descrição |
|---|---|---|
| `Itens` | Já existia | Catálogo de materiais (seed automático se vazia) |
| `Usuarios` | Já existia | Fornecedores; agora com CRUD completo (Editar/Inativar/Reativar) |
| `RecibosCompra` | Corrigida | `CodigoRecibo` (5 dígitos) agora com índice `UNIQUE`, exigido para a FK |
| `ReciboCompraItens` | Corrigida | `ReciboCodigo` migrado de `INTEGER` para `TEXT` |
| `RecibosVenda` | **Nova** | Cabeçalho do recibo de venda (`CodigoRecibo` de 6 dígitos) |
| `RecibosVendaItens` | **Nova** | Itens do recibo de venda (Material + Peso, sem preço unitário) |
| `EstoqueView` (view) | Já existia | Ajustada para refletir apenas compras (`TotalVendasKg` fixo em 0 por ora) |

## Próximos Passos Sugeridos
1. Popular `EstoqueView.TotalVendasKg` a partir de `RecibosVendaItens` (hoje fixo em `0`)
2. Adicionar botão "Novo Fornecedor" (criação manual, hoje só ocorre automaticamente ao salvar um recibo)
3. Avaliar se `RecibosVenda` deveria referenciar `Usuarios` por `UsuarioId` em vez de `ClienteNome` livre,
   para reaproveitar cadastro/autocompletar como já ocorre nas compras
