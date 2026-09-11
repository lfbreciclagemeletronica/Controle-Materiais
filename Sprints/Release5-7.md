# Release 5.0.7 - Resumo de Modificações

## Geral

### 1. Versão do projeto atualizada para 5.0.7
- **Arquivos:** `ControleMateriais.Desktop/ControleMateriais.Desktop.csproj`, `ControleMateriais.Desktop/Views/HomeView.axaml`
- **Alteração:** `<Version>` alterado de `5.0.6` para `5.0.7`; `TextBlock` da versão na Home atualizado para `V5.0.7`
- **Motivo:** Nova release com correções de sincronização e melhorias no processamento de recibos

---

## Sincronização do Banco de Dados — Pausada Temporariamente

### 2. Sincronização de Banco de Dados desativada no fechamento
- **Arquivo:** `ControleMateriais.Desktop/Services/GitHubService.cs`
- **Método:** `SincronizarTudoAoFecharAsync()`
- **Alteração:** Bloco que sincroniza o repositório `banco-de-dados` foi comentado; adicionada mensagem `Banco de Dados: sincronização pausada (Em Desenvolvimento).`
- **Motivo:** Evitar falhas de sincronização enquanto o módulo de banco de dados passa por revisão

### 3. Salvar estoque inicial sem publicar no GitHub
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/EstoqueInicialViewModel.cs`
- **Métodos:** `SalvarAsync()`, `RegistrarEstoqueAsync()`, `CriarNovoEstoqueInicialAsync()`, `PublicarLog()`
- **Alteração:** Chamadas a `GarantirBancoDadosRepoAsync`, `PublicarJsonBancoDadosAsync` e `PublicarLog` foram comentadas; status atualizado para "Estoque inicial salvo localmente. (Sync GitHub: Em Desenvolvimento)"
- **Motivo:** Salvar localmente sem envio automático ao GitHub durante o período de desenvolvimento

### 4. Sincronização do banco-de-dados desativada na aba Estoque
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/EstoqueViewModel.cs`
- **Método:** `SincronizarGitAsync()` (anteriormente `AtualizarAsync`)
- **Alteração:** Chamada a `GarantirBancoDadosRepoAsync` comentada; status alterado para `Recibos sincronizados. (Banco de Dados: Em Desenvolvimento)`
- **Motivo:** Sincronizar apenas o repositório de recibos até que o banco de dados esteja estável

### 5. Publicação de recibo de compra sem envio ao GitHub
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/MainWindowViewModel.cs`
- **Método:** `ExportarJsonBancoDadosAsync()`
- **Alteração:** Bloco que envia o JSON de compra para `PublicarJsonBancoDadosAsync` foi comentado
- **Motivo:** Gravar o JSON localmente sem publicação automática no banco-de-dados

### 6. Pull do banco-de-dados pausado no splash screen
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/SplashViewModel.cs`
- **Método:** `InicializarAsync()`
- **Alteração:** `PullBancoDadosAsync()` removido do `Task.WhenAll`; `ItemBancoDados` é marcado como `Ok` com mensagem "Banco de Dados: sincronização pausada (Em Desenvolvimento)"
- **Motivo:** Inicialização do app não depende mais do pull do banco-de-dados

### 7. Commit de venda no banco-de-dados desativado
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/VendaViewModel.cs`
- **Método:** `ExportarAsync()`
- **Alteração:** Chamada a `CommitJsonBancoDadosAsync` comentada; geração de `meta.json` removida
- **Motivo:** Venda salva localmente sem envio ao GitHub; metadados extras não são mais necessários

---

## Padrão de Nome de Arquivo de Recibo

### 8. Nome de arquivo de recibo de compra agora inclui hora
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/MainWindowViewModel.cs`
- **Método:** `GerarReciboPdf()`
- **Alteração:** Nome alterado de `NomeCliente_dd-MM-yyyy.pdf` para `NomeSeguroCliente_ddMMyyyyHHmmss.pdf` (14 dígitos contínuos)
- **Motivo:** Permitir múltiplos recibos para o mesmo cliente no mesmo dia; evitar colisão de nomes

### 9. Nome de arquivo de recibo de venda com novo padrão
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/VendaViewModel.cs`
- **Método:** `ExportarAsync()`
- **Alteração:** Nome base alterado de `NomeCliente_dd-MM-yyyy` para `NomeCliente_ddMMyyyyHHmmss`
- **Motivo:** Consistência com o novo padrão de nomenclatura de recibos

### 10. Recibo gerado a partir de pesagem com novo padrão
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/ReciboFromPesagemViewModel.cs`
- **Alteração:** Sufixo de data/hora alterado de `dd-MM-yyyy` para `ddMMyyyyHHmmss`
- **Motivo:** Alinhar com o novo padrão de nome de arquivo de recibo

### 11. Parser de recibos suporta novo padrão de nome
- **Arquivo:** `ControleMateriais.Desktop/Services/ReconstruirBancoDadosService.cs`
- **Métodos:** `ExtrairNomeCliente()`, `ExtrairData()`
- **Alteração:** Adicionado regex para `NomeCliente_\d{14}` (14 dígitos) antes dos padrões antigos `dd-MM-yyyy` e `dd-MM-yyyy_HH-mm`
- **Motivo:** Reconstruir o banco de dados corretamente a partir dos novos nomes de PDF

### 12. PesagensView suporta novo padrão de nome de recibo
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/PesagensViewModel.cs`
- **Método:** `ParsearNomeArquivoRecibo()`
- **Alteração:** Adicionado regex para `NomeCliente_\d{14}`; `ddMMyyyyHHmmss` parseado com `DateTime.TryParseExact`
- **Motivo:** Exibir recibos com novo padrão na lista de pesagens

### 13. EstoqueView suporta novo padrão de nome de recibo de venda
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/EstoqueViewModel.cs`
- **Método:** `ParsearNomeArquivoVenda()` (novo)
- **Alteração:** Extração de nome e data movida para método auxiliar que suporta `_ddMMyyyyHHmmss` (novo) e `_dd-MM-yyyy` (antigo)
- **Motivo:** Listar recibos de venda corretamente independente do padrão de nome

---

## Melhorias no Parser de Recibos

### 14. ReciboParserService detecta início da tabela de materiais
- **Arquivo:** `ControleMateriais.Desktop/Services/ReciboParserService.cs`
- **Método:** `ExtrairItens()`
- **Alteração:** Localiza a linha que contém o cabeçalho "MATERIAL" e ignora tudo antes; iterações dos passos 1 e 2 começam a partir desse índice
- **Motivo:** Evitar que dados do cabeçalho do recibo sejam interpretados como itens

### 15. ReciboParserService ignora cabeçalhos de recibo de venda
- **Arquivo:** `ControleMateriais.Desktop/Services/ReciboParserService.cs`
- **Alteração:** Adicionados termos `recibo de venda de estoque`, `cliente`, `recibo de venda` à lista de palavras ignoradas; adicionado array `_prefixosCabecalhoVenda` (`cliente`, `cnpj`, `i.e.`, `end:`, `rua`, `av.`, `r$`)
- **Motivo:** Evitar que linhas de cabeçalho de venda sejam interpretadas como materiais

### 16. ReciboParserService ignora nomes de clientes em maiúsculas
- **Arquivo:** `ControleMateriais.Desktop/Services/ReciboParserService.cs`
- **Método:** `ExtrairItens()` — Passo 2
- **Alteração:** Linhas compostas apenas por letras e espaços, todas em maiúsculas, são ignoradas (ex: `MARCO ANTONIO`, `VALMARC`)
- **Motivo:** Evitar interpretação de nomes de clientes como materiais

---

## Tela de Estoque

### 17. Aba "Recibos de Venda" simplificada
- **Arquivo:** `ControleMateriais.Desktop/Views/EstoqueView.axaml`
- **Alteração:** Layout da aba reduzido para 3 colunas (Cliente, Data, Ações); removidas colunas Peso Total e Valor Total; botão "Atualizar" movido para direita; filtros reorganizados
- **Motivo:** Exibir apenas informações essenciais, removendo dependência de metadados que não são mais gerados

### 18. Aba "Materiais" ocultada com aviso "Em Progresso"
- **Arquivo:** `ControleMateriais.Desktop/Views/EstoqueView.axaml`
- **Alteração:** Grid da lista de materiais comentado; exibido `StackPanel` com texto "⚙ Em Progresso" e "Esta funcionalidade estará disponível em breve."
- **Motivo:** Desativar funcionalidade em desenvolvimento até que o estoque seja recalculado corretamente

### 19. EstoqueViewModel remove leitura de meta.json
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/EstoqueViewModel.cs`
- **Métodos:** `ExcluirReciboVendaAsync()`, `CarregarRecibosVenda()`
- **Alteração:** Removida leitura/escrita do arquivo `.meta.json`; exclusão do PDF não remove mais meta; `ReciboVendaItem` preenchido com `NomeCliente` e `DataCriacao` a partir do nome do arquivo
- **Motivo:** Simplificar o modelo de dados dos recibos de venda

### 20. SplashView verifica vendas por contagem de PDFs
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/SplashViewModel.cs`
- **Método:** `VerificarVendasAsync()`
- **Alteração:** Lógica simplificada: conta PDFs em `Recibos_Venda` e exibe mensagem de quantidade; removida comparação com registros do banco de dados
- **Motivo:** Verificação de vendas não depende mais do banco de dados, que está em desenvolvimento
