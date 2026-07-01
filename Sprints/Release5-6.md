# Release 5.0.6 - Resumo de Modificações

## Correções Críticas no Estoque Inicial

### 1. RegistrarEstoqueAsync usava nome de arquivo hardcoded
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/EstoqueInicialViewModel.cs`
- **Linha:** ~375
- **Alteração:** Substituído `Path.Combine(bancoDadosDir, "estoque-inicial.json")` por `EstoqueInicialPath`
- **Motivo:** `RegistrarEstoqueAsync` lia `estoque-inicial.json` (inexistente) em vez do arquivo correto `estoque-inicial-MM-AAAA.json`, produzindo estoque final zerado

### 2. SincronizarGitAsync não recarregava lista de estoques após pull
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/EstoqueViewModel.cs`
- **Método:** `SincronizarGitAsync()`
- **Alteração:** Adicionada chamada a `CarregarListaEstoquesIniciais()` antes de `Recarregar()` após o pull do banco-de-dados
- **Motivo:** Após o pull, a lista `EstoquesIniciaisDisponiveis` permanecia vazia, fazendo o cálculo usar o fallback incorreto

### 3. Fallback hardcoded "estoque-inicial.json" removido
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/EstoqueViewModel.cs`
- **Propriedade:** `EstoqueInicialPath`
- **Alteração:** Quando `EstoqueInicialSelecionado` está vazio, retorna `string.Empty` em vez de `"estoque-inicial.json"`
- **Motivo:** O arquivo `estoque-inicial.json` nunca é criado pelo sistema; o fallback causava erro silencioso

### 4. LerEstoqueInicial trata caminho vazio com mensagem orientativa
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/EstoqueViewModel.cs`
- **Método:** `LerEstoqueInicial()`
- **Alteração:** Verifica `string.IsNullOrEmpty(path)` antes de verificar `File.Exists`; exibe status "Nenhum estoque inicial encontrado. Clique em Sincronizar ou acesse Estoque Inicial."
- **Motivo:** Mensagem clara quando não há estoque inicial em vez de falha silenciosa

---

## Melhorias na Tela de Estoque

### 5. Aviso visual quando não há estoque inicial disponível
- **Arquivo:** `ControleMateriais.Desktop/Views/EstoqueView.axaml`
- **Alteração:** Adicionado `TextBlock` laranja com `IsVisible="{Binding SemEstoqueInicial}"` e texto "Nenhum estoque inicial encontrado. Acesse Estoque Inicial para criar um."; ComboBox oculto quando lista vazia
- **Motivo:** Feedback visual imediato ao usuário em vez de exibir estoque zerado silenciosamente

### 6. Propriedade SemEstoqueInicial adicionada ao EstoqueViewModel
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/EstoqueViewModel.cs`
- **Alteração:** Adicionada propriedade `public bool SemEstoqueInicial => EstoquesIniciaisDisponiveis.Count == 0` com `OnPropertyChanged` ao final de `CarregarEstoquesIniciaisDisponiveis()`
- **Motivo:** Binding no AXAML para controle de visibilidade do aviso e do ComboBox

### 7. Botões "Sincronizar" e "Atualizar Estoque" removidos
- **Arquivo:** `ControleMateriais.Desktop/Views/EstoqueView.axaml`
- **Alteração:** Removidos `Button` "Sincronizar" e "Atualizar Estoque" do cabeçalho da tela de Estoque
- **Motivo:** Sincronização e atualização agora são automáticas ao abrir a aba

### 8. Sincronização automática ao abrir a aba de Estoque
- **Arquivo:** `ControleMateriais.Desktop/Views/EstoqueView.axaml.cs`
- **Alteração:** `DataContextChanged` agora chama `_ = vm.SincronizarGitAsync()` em vez de `vm.CarregarListaEstoquesIniciais()` + `vm.Recarregar()` manualmente
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/EstoqueViewModel.cs`
- **Alteração:** `SincronizarGitAsync()` alterado de `private` para `public`
- **Motivo:** Pull do Recibos e banco-de-dados, recarga da lista e recálculo do estoque ocorrem automaticamente sem interação do usuário

---

## Melhorias na Sincronização Git — Mensagens de Erro Claras

### 9. Helper ClassificarErroGit adicionado ao GitHubService
- **Arquivo:** `ControleMateriais.Desktop/Services/GitHubService.cs`
- **Alteração:** Adicionado método `internal static string ClassificarErroGit(string stderr, string operacao)` que categoriza erros git em:
  - Token inválido/expirado (`authentication failed`, `bad credentials`, `401`)
  - Repositório não encontrado (`not found`, `404`, `does not exist`)
  - Sem conexão com internet (`could not resolve host`, `network`, `timeout`)
  - Permissão negada (`permission denied`, `403`)
  - Conflito de merge (`conflict`, `merge conflict`)
  - Erro genérico com detalhe do stderr
- **Motivo:** Mensagens orientativas em português para facilitar o diagnóstico de falhas de sincronização

### 10. Pull*Async lança exceção com stderr classificado em caso de falha
- **Arquivo:** `ControleMateriais.Desktop/Services/GitHubService.cs`
- **Métodos:** `PullRecibosAsync`, `PullPesagensAsync`, `PullBancoDadosAsync`
- **Alteração:** Operações de `clone`, `fetch` e `rebase` verificam `exitCode != 0` e lançam `Exception(ClassificarErroGit(stderr, operacao))`
- **Motivo:** Erros git eram ignorados silenciosamente; agora propagam mensagem orientativa

### 11. SincronizarTudoAoFecharAsync com try/catch individual por repositório
- **Arquivo:** `ControleMateriais.Desktop/Services/GitHubService.cs`
- **Método:** `SincronizarTudoAoFecharAsync()`
- **Alteração:** Cada chamada a `SincronizarRepo(...)` envolvida em `try/catch` individual; erro em um repositório não bloqueia a sincronização dos demais; mensagem de erro no formato `[NomeRepo] mensagem classificada`
- **Motivo:** Falha num repositório não impedia push dos outros; agora cada repo é independente

### 12. SplashViewModel usa mensagens classificadas no catch dos pulls
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/SplashViewModel.cs`
- **Métodos:** `PullRecibosAsync`, `PullPesagensAsync`, `PullBancoDadosAsync`
- **Alteração:** Catches usam `GitHubService.ClassificarErroGit(ex.Message, operacao)` em vez de exibir `ex.Message` direto
- **Motivo:** Mensagem orientativa exibida no splash screen quando pull falha ao iniciar o app

### 13. SincronizandoFechamentoDialog interpreta erros por repositório
- **Arquivo:** `ControleMateriais.Desktop/Views/SincronizandoFechamentoDialog.axaml.cs`
- **Método:** `InterpretarMensagem()`
- **Alteração:** Detecta mensagens no formato `[Repo] erro` e chama `_vm.MarcarRepoErro(repo, detalhe)` com a mensagem classificada; `EnsureAllDone` não sobrescreve itens já marcados com erro
- **Motivo:** Erros de push ao fechar agora aparecem por repositório no diálogo de fechamento
