# Melhorias Tela Estoque Inicial

Corrigir 6 problemas visuais e funcionais na tela de Estoque Inicial identificados nas screenshots.

---

## Problemas e correções

### 1. Seletor Mês/Ano — espaço insuficiente
**Arquivo:** `EstoqueInicialView.axaml`  
Substituir os dois `NumericUpDown` por um `ComboBox` para o mês (exibe "Janeiro (01)", etc.) e um `TextBox` para o ano com largura adequada. Ou simplesmente aumentar `Width` dos `NumericUpDown` e remover o `FormatString`.

### 2. Remover subtítulo
**Arquivo:** `EstoqueInicialView.axaml`  
Remover o `<TextBlock Text="Apenas pesos por material..."/>` abaixo do título.

### 3. Remover ícones dos botões
**Arquivo:** `EstoqueInicialView.axaml`  
Trocar `Content="💾 Salvar"` → `Content="Salvar"` e `Content="📊 Registrar Estoque"` → `Content="Registrar Estoque"`.

### 4. Adicionar itens personalizados
**Arquivos:** `EstoqueInicialView.axaml`, `EstoqueInicialViewModel.cs`  
- No VM: adicionar `ObservableCollection<PesoInicialWrapper> ItensPersonalizados` (4 linhas com nome editável + peso), expor `PesoPersonalizadoTotal` para o total global, e incluir itens personalizados no `GravarEstoqueInicialLocal()` e `LerEstoqueInicial()`.
- Na View: adicionar seção "Itens Personalizados" com `Impurezas` (nome fixo, só peso) + 4 linhas de `TextBox` nome + `TextBox` peso, idêntico ao recibo — mas sem coluna de preço.
- No código-behind `.axaml.cs`: handlers `GotFocus/KeyDown/LostFocus` para itens personalizados (nome e peso).

### 5. Total em kg no topo
**Arquivos:** `EstoqueInicialView.axaml`, `EstoqueInicialViewModel.cs`  
- No VM: adicionar propriedade `PesoTotalGeral` (decimal) calculada como soma de todos `Itens` + `ItensPersonalizados` + Impurezas; deve ser recalculada quando qualquer `PesoInicialWrapper.Peso` muda (subscrever evento ou usar setter).
- Na View: exibir no cabeçalho centralizado, similar ao "Peso Total / 0,000 kg" da tela de recibos.

### 6. Encoding do JSON — acentos bugados
**Arquivo:** `EstoqueInicialViewModel.cs`  
`JsonSerializerOptions` padrão faz escape de caracteres não-ASCII (`ã` → `\u00E3`). Corrigir adicionando `Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping` e usando `File.WriteAllText(path, json, System.Text.Encoding.UTF8)`.

---

## Arquivos modificados
1. `ViewModels/EstoqueInicialViewModel.cs` — encoding, itens personalizados, total geral, assinatura de mudança de peso
2. `Views/EstoqueInicialView.axaml` — UI: seletor, sem subtítulo, sem ícones, itens personalizados, total
3. `Views/EstoqueInicialView.axaml.cs` — handlers para itens personalizados (nome + peso)
