# Release 5.0.1 — LFB Controle de Materiais

## Visão Geral

Patch de correções críticas do **Reconstruir Banco de Dados** e sincronização do estoque. Corrige a subtração das vendas que não estava sendo aplicada ao estoque exibido, elimina contagem duplicada de PDFs de venda que estavam na raiz do repositório e garante que a tela de Estoque atualiza automaticamente após a reconstrução.

---

## 🐛 Correções

### 1. Tela de Estoque não atualizava após Reconstruir Banco

**Causa:** `EstoqueViewModel.Recarregar()` só era chamado quando o `DataContext` da view era atribuído pela primeira vez (ao navegar pela primeira vez para a tela). Como `EstoqueVM` é uma instância única, reconstruir o banco em `PesagensViewModel` gravava o novo `estoque.json` no disco, mas a tela de Estoque continuava exibindo os dados antigos em memória.

**Correção:**
- Adicionado `EstoqueRecarregarCallback` em `PesagensViewModel`.
- Chamado ao final de `ReconstruirBancoDadosAsync` com sucesso.
- Conectado em `MainWindowViewModel`:
  ```csharp
  PesagensVM.EstoqueRecarregarCallback = () => EstoqueVM.Recarregar();
  ```

**Arquivos alterados:**
- `ViewModels/PesagensViewModel.cs`
- `ViewModels/MainWindowViewModel.cs`

---

### 2. PDF de venda na raiz de `Recibos/` era contado como pesagem

**Causa:** O arquivo `ESTOQUE_FINAL_29-05-2026.pdf` (e potencialmente outros) existia tanto em `Recibos/` (raiz) quanto em `Recibos/Recibos_Venda/`. O `Reconstruir` processava todos os PDFs da raiz como pesagens — inflando o total — e depois o mesmo arquivo era subtraído como venda, gerando inconsistência.

**Correção:** Antes de listar os PDFs de pesagem, o `ReconstruirBancoDadosService` agora constrói um `HashSet` com os nomes de arquivos presentes em `Recibos_Venda/` e **exclui** da fase de pesagem qualquer PDF cujo nome coincida:

```csharp
var nomesVenda = new HashSet<string>(
    Directory.GetFiles(vendaDir, "*.pdf").Select(Path.GetFileName)!,
    StringComparer.OrdinalIgnoreCase);

var pdfs = Directory.GetFiles(recibosDir, "*.pdf", SearchOption.TopDirectoryOnly)
    .Where(f => !nomesVenda.Contains(Path.GetFileName(f)))
    ...
```

**Arquivos alterados:**
- `Services/ReconstruirBancoDadosService.cs`

---

### 3. Parser de pesos (`ReciboParserService`) não reconhecia ponto decimal

**Causa:** PDFs de venda gerados antes da correção de cultura (V5.0.0) usavam ponto como separador decimal (ex: `19.129`). O regex `_rePeso` só reconhecia vírgula (ex: `19,129`).

**Correção:**
- Regex estendido com terceiro grupo `(\d+\.\d{3})` para capturar formato invariant.
- `ExtrairPrimeiroPeso` adaptado para distinguir os dois formatos:
  - Com vírgula → trata como pt-BR (`1.234,567` → `1234.567`)
  - Com ponto sem vírgula → trata como invariant (`1.258` → parse direto)

**Arquivos alterados:**
- `Services/ReciboParserService.cs`

---

### 4. Log de diagnóstico gravado em disco após Reconstruir Banco

Para facilitar diagnóstico de problemas futuros, o `ReconstruirBancoDadosService` agora grava um arquivo de log completo em:

```
~/Downloads/ControleMateriaisLFB/reconstruir-banco.log
```

O log contém timestamp, nome de cada PDF processado, itens extraídos e avisos de itens não reconhecidos ou não encontrados no estoque.

---

### 5. Versão atualizada para V5.0.1

Label na tela inicial (`HomeView`) atualizado de `V5.0.0` para `V5.0.1`.

---

## 🔧 Arquivos Modificados

```
ControleMateriais.Desktop/
├── ViewModels/
│   ├── PesagensViewModel.cs        — EstoqueRecarregarCallback declarado e invocado
│   └── MainWindowViewModel.cs      — EstoqueRecarregarCallback conectado ao EstoqueVM
├── Services/
│   ├── ReconstruirBancoDadosService.cs — exclusão de PDFs de venda da fase de pesagem,
│   │                                     logging detalhado em disco
│   └── ReciboParserService.cs      — regex e parser adaptados para ponto decimal
└── Views/
    └── HomeView.axaml              — versão V5.0.1
```

---

**Versão**: Release 5.0.1  
**Branch**: `Release/4.0`  
**Data**: 2026-06-19  
**Responsável**: Gabriel Stundner  
**Repositório**: `lfbreciclagemeletronica/Controle-Materiais`
