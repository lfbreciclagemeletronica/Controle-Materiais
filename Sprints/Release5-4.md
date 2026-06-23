# Release 5.4 - Resumo de Modificações

## Correções de Erros e Vulnerabilidades

### 1. Correção CS0103 em ItemCatalog.cs
- **Arquivo:** `ControleMateriais.Desktop/ItemCatalog.cs`
- **Alteração:** Adicionado `using System;` no topo do arquivo
- **Motivo:** Resolvido erro de compilação CS0103: `StringComparer` não encontrado no contexto atual

### 2. Correção de Vulnerabilidades de Pacotes
- **Arquivo:** `ControleMateriais.Desktop/ControleMateriais.Desktop.csproj`
- **Alterações:**
  - Adicionado override para `System.IO.Packaging` versão `10.0.7` (corrige GHSA-f32c-w444-8ppv e GHSA-qj66-m88j-hmgj)
  - Adicionado override para `Tmds.DBus.Protocol` versão `0.21.3` (corrige GHSA-xrw6-gwf8-vvr9)
- **Motivo:** Resolver vulnerabilidades de alta severidade em pacotes transitivos

## Melhorias no Fluxo de Recibos

### 3. Limpar Campos ao Criar Novo Recibo
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/MainWindowViewModel.cs`
- **Método:** `LimparParaNovoRecibo()`
- **Alterações:**
  - Adicionado `_pesagingAtiva = null` para limpar referência de pesagem ativa
  - Alterado de `custom.ResetarPeso()` para `custom.Zerar()` para também resetar preços de itens personalizados
  - Alterado `CurrentPage = AppPage.Home` para `CurrentPage = AppPage.Recibos` para manter usuário na página de criação de recibos
- **Motivo:** Ao clicar "+ Novo Recibo", o usuário permanece na página de criação com todos os campos zerados

### 4. Resetar Preço por Kg ao Criar Novo Recibo
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/MainWindowViewModel.cs`
- **Classe:** `PesoWrapper`
- **Alterações:**
  - Adicionado método `ResetarPreco()` para resetar preço para R$ 0,00
  - Atualizado `LimparParaNovoRecibo()` para chamar `w.ResetarPreco()` em todos os itens de `ItensEditaveis`
- **Motivo:** Ao clicar "+ Novo Recibo", os preços por kg também são zerados

## Melhorias no Fluxo de Exportação

### 5. Exportar Automaticamente sem Diálogo
- **Arquivo:** `ControleMateriais.Desktop/ViewModels/MainWindowViewModel.cs`
- **Método:** `ExportarAsync()`
- **Alteração:** Removido `SaveFilePickerAsync` e agora salva diretamente em `Path.Combine(recibosDir, nomeArquivo)`
- **Motivo:** O PDF é salvo automaticamente no diretório Recibos sem exibir diálogo de salvamento

## Melhorias de Interface

### 6. Botão de Voltar para Início
- **Arquivo:** `ControleMateriais.Desktop/Views/ReciboView.axaml`
- **Alteração:** Texto do botão alterado de `"← Pesagens"` para `"← Inicio"`
- **Motivo:** Melhor clareza na navegação da página de criação de recibos

## Melhorias no Fluxo Git

### 7. Push Dinâmico para Branch Atual
- **Arquivo:** `ControleMateriais.Desktop/Services/GitHubService.cs`
- **Alterações:**
  - Adicionado método auxiliar `ObterBranchAtual(string repoDir)` que executa `git rev-parse --abbrev-ref HEAD`
  - Substituídos 5 comandos hardcoded `push origin main` por `push origin <branchAtual>`:
    - `PublicarJsonBancoDadosAsync` (linha 157)
    - `RemoverJsonBancoDadosAsync` (linha 205)
    - `SincronizarRecibosAsync` (linha 336)
    - `PublicarReciboAsync` (linha 559)
    - `PublicarReciboVendaAsync` (linha 622)
- **Motivo:** Evitar conflitos com branch main usando a branch ativa local dos repositórios Recibos e BancoDados
