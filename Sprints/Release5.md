# Release 5 — LFB Controle de Materiais

## Visão Geral

Expande o módulo de **Pesagens e Recibos** com três grandes adições: sistema completo de **exportação para Excel** com dados de pesagem e vendas por dia, **reconstrução do banco de dados** a partir dos PDFs existentes, e melhorias de robustez no formulário de venda. Também corrige a disposição visual da mensagem de status na aba de Recibos e adiciona validação de integridade do JSON gerado no fluxo de exportação de recibos.

---

## ✨ Features Implementadas

### 📊 Exportação para Excel (`RelatorioExcelService` + `ReciboParserService`)

Nova funcionalidade acessível pelo botão **📊 Exportar Excel** na aba de Recibos:

- Lê todos os PDFs de pesagem em `Recibos/` (raiz, ignora subpastas).
- Lê todos os PDFs de venda em `Recibos/Recibos_Venda/`.
- Extrai pesos por item de cada PDF usando o novo `ReciboParserService.ExtrairPesos`.
- Gera planilha `.xlsx` com três blocos de colunas:

| Bloco | Cor | Conteúdo |
|-------|-----|----------|
| **Pesagens por dia** | Verde (`#2E7D32`) | Uma coluna por data + coluna **TOTAL** |
| **Vendas por dia** | Laranja (`#E65100` / `#BF360C`) | Uma coluna por data de venda + coluna **TOTAL VENDAS** |
| **Estoque Atual** | Azul (`#0D47A1`) | `TOTAL pesagem − TOTAL VENDAS` por item |

**Detalhes da planilha:**
- Linha de rodapé **TOTAL** com soma por coluna (verde / laranja / azul).
- Itens extras (fora do catálogo) exibidos em seção separada com linha divisória roxa.
- Coluna do item com largura 38; colunas de data com largura 14; colunas totais com largura 16.
- Cabeçalho e coluna de nomes congelados (`Freeze(1,1)`).
- Bordas finas em toda a tabela.
- Compatível com zero vendas: blocos laranja e azul omitidos automaticamente.

---

### 🔍 Extração de Pesos de PDF (`ReciboParserService`)

Novo serviço estático responsável por parsear PDFs de recibos:

- Extrai texto do PDF usando **iText7**.
- Identifica itens pelo **catálogo** (`ItemCatalog.OrderedItems`) com suporte a aliases.
- Lida com itens cujo peso aparece na mesma linha ou nas duas linhas seguintes.
- Normaliza texto (acentos, maiúsculas, espaços entre dígitos por OCR).
- Detecta e acumula **itens extras** (fora do catálogo) como segunda passagem.
- Retorna `Dictionary<string, decimal>` com nome → peso em kg.

---

### 🔄 Reconstrução do Banco de Dados (`ReconstruirBancoDadosService`)

Novo serviço acessível pelo botão **🔄 Reconstruir Banco** na aba de Recibos:

- Apaga todos os JSONs existentes em `banco-de-dados/`.
- Processa cada PDF de pesagem → gera o `.json` correspondente com os pesos extraídos.
- Acumula totais de pesagem em memória.
- Subtrai os pesos de cada PDF de venda (`Recibos_Venda/`) do total acumulado.
- Grava `estoque.json` com o saldo final recalculado.
- Progresso reportado em tempo real na UI via callback `Action<string>`.

**Botão na UI:**
- Fundo vermelho escuro (`#E65100`) + texto branco para destacar ação destrutiva.
- Desabilitado durante execução (`IsEnabled="{Binding !ReconstruindoBancoDados}"`).
- Tooltip explicativo sobre o comportamento de reconstrução.

---

### 📋 Melhorias no Formulário de Venda (`VendaView` / `VendaViewModel`)

Campos adicionais e correções no fluxo de registro de venda:

- Campos obrigatórios anteriormente ausentes adicionados ao formulário.
- Layout da `VendaView.axaml` reorganizado para melhor usabilidade.
- `VendaViewModel` expandido com validações e bindings faltantes.

---

### 🛡️ Validação de Integridade do JSON de Recibo (`MainWindowViewModel`)

Após gravar o JSON do recibo no banco de dados:

```csharp
if (!File.Exists(jsonPath) || new FileInfo(jsonPath).Length == 0)
    throw new InvalidOperationException(
        $"Falha ao criar o arquivo JSON do recibo no banco de dados: {jsonPath}");
```

Garante que falhas silenciosas de I/O são detectadas imediatamente e exibidas ao usuário, prevenindo inconsistências no estoque.

---

### 🖥️ Versão exibida na tela inicial (`HomeView`)

Label **V5.0.0** adicionado abaixo do título "LFB RECICLAGEM ELETRÔNICA":

- Fonte 13px, cor branca, centralizado.
- Permite identificar visualmente a versão em execução sem abrir configurações.

---

### 🗂️ Layout — Mensagem de Status na Aba de Recibos (`PesagensView`)

A área de mensagem de sincronização/erros foi movida para uma linha dedicada acima do conteúdo principal:

- Grid da aba Recibos reorganizado de 3 para 4 linhas:
  - **Row 0** — barra de filtros + botões de ação
  - **Row 1** — mensagem de status (`StatusRecibos`)
  - **Row 2** — última sincronização (`UltimaSincRecibos`)
  - **Row 3** — lista de recibos
- Evita sobreposição da mensagem com os botões de ação.

---

## 🔧 Detalhes Técnicos

### Novos Arquivos

```
ControleMateriais.Desktop/
├── Services/
│   ├── ReciboParserService.cs          — extração de pesos de PDFs (iText7)
│   ├── RelatorioExcelService.cs        — geração do Excel (ClosedXML)
│   └── ReconstruirBancoDadosService.cs — reconstrução completa do banco de dados
```

### Arquivos Modificados

```
ControleMateriais.Desktop/
├── ViewModels/
│   ├── PesagensViewModel.cs            — ExportarExcelCommand, ReconstruirBancoDadosCommand,
│   │                                     ReconstruindoBancoDados, ExportandoExcel
│   └── MainWindowViewModel.cs          — validação de integridade do JSON + ajuste de layout
├── Views/
│   ├── PesagensView.axaml              — botões Exportar Excel e Reconstruir Banco,
│   │                                     layout de 4 linhas na aba Recibos
│   ├── VendaView.axaml                 — campos adicionais no formulário de venda
│   └── HomeView.axaml                  — label V5.0.0 abaixo do título
├── Services/
│   └── RelatorioExcelService.cs        — bloco de vendas + coluna Estoque Atual
├── ControleMateriais.Desktop.csproj    — dependência ClosedXML adicionada
```

### Dependências Adicionadas

| Pacote | Uso |
|--------|-----|
| `ClosedXML` | Geração da planilha `.xlsx` |

### Estrutura do Excel Gerado

```
| Item (38px) | dd/MM/yyyy... | TOTAL | dd/MM/yyyy... | TOTAL VENDAS | Estoque Atual |
|-------------|---------------|-------|---------------|--------------|---------------|
|  (verde)    |   (verde)     |(verde)|   (laranja)   |   (laranja)  |    (azul)     |
```

### Fluxo — Exportar Excel

```
Aba Recibos → 📊 Exportar Excel
  → selecionar caminho de saída (.xlsx)
  → lê PDFs de pesagem em Recibos/*.pdf
  → lê PDFs de venda em Recibos/Recibos_Venda/*.pdf
  → extrai pesos por item via ReciboParserService
  → agrega por data (porDia / porDiaVenda)
  → gera planilha com 3 blocos (pesagem / venda / estoque atual)
  → salva arquivo e exibe mensagem de sucesso
```

### Fluxo — Reconstruir Banco de Dados

```
Aba Recibos → 🔄 Reconstruir Banco
  → apaga todos os JSONs em banco-de-dados/
  → para cada PDF em Recibos/*.pdf:
      → extrai pesos → grava <nome>.json
      → acumula em totaisPesagem
  → para cada PDF em Recibos/Recibos_Venda/*.pdf:
      → subtrai pesos do totaisEstoque
  → grava estoque.json com saldo final
```

---

**Versão**: Release 5 (V5.0.0)  
**Branch**: `Release/4.0`  
**Data**: 2026-06-18  
**Responsável**: Gabriel Stundner  
**Repositório**: `lfbreciclagemeletronica/Controle-Materiais`
