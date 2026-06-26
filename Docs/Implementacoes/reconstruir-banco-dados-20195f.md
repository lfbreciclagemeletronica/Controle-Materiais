# Reconstruir banco-de-dados e estoque.json a partir dos PDFs

Apagar todos os JSONs de `banco-de-dados/` (exceto `.git/`), regenerar um JSON por PDF de pesagem e um `estoque.json` final que soma tudo de pesagem e subtrai as vendas.

---

## Contexto

- **Recibos de pesagem:** `Recibos/*.pdf` (~260 PDFs) → cada um vira um `.json` no formato atual do app
- **Recibos de venda:** `Recibos/Recibos_Venda/*.pdf` (3 PDFs) → extrair itens/pesos e subtrair do estoque
- **`estoque.json`:** somatório final = Σ pesagens − Σ vendas, por item
- O app usa `EstoqueViewModel.LerItensJson()` + `ProcessarNovosJsons()` para popular o estoque — os novos JSONs gerados devem ser compatíveis com esse formato

---

## Formato dos JSONs gerados (compatível com app atual)

```json
{
  "Placa Mãe C": 25.0,
  "Placa Leve": 3.5,
  "data": "dd-MM-yyyy",
  "status": "Adicionado ao estoque"
}
```

---

## Passos

### 1. Criar `ReconstruirBancoDadosService.cs` (novo serviço, `Services/`)

Método estático `Reconstruir(rootDir, progresso?)` com a seguinte lógica:

**a) Apagar todos os `.json` em `banco-de-dados/`** (menos `estoque.json` se existir e excluindo `.git/`)

**b) Para cada PDF em `Recibos/*.pdf`** (TopDirectoryOnly):
- Extrair data do nome do arquivo (via `ExtrairDataDoNome`, mesma lógica de `RelatorioExcelService`)
- Extrair pesos por item via `ReciboParserService.ExtrairPesos(pdf)`
- Pular PDFs sem nenhum item com peso > 0
- Gerar nome do JSON: `{NomeBase}_{dd-MM-yyyy}.json` (mesmo nome do PDF sem extensão + `.json`)
- Salvar JSON com campos de itens + `"data"` + `"status": "Adicionado ao estoque"`

**c) Calcular estoque:**
1. Somar todos os JSONs gerados em (b) → totais por item
2. Para cada PDF em `Recibos/Recibos_Venda/*.pdf`:
   - Extrair pesos via `ReciboParserService.ExtrairPesos(pdf)` (mesmo parser — PDF de venda tem colunas MATERIAL + KG com mesmo formato N3)
   - Subtrair do totais: `totais[item] = max(0, totais[item] - pesoVendido)`
3. Remover itens com peso ≤ 0 do estoque final
4. Gravar `estoque.json` via `EstoqueViewModel.GravarEstoque(rootDir, totais)`

---

### 2. Expor o comando na UI (`PesagensViewModel.cs`)

Adicionar `ICommand ReconstruirBancoDadosCommand` com método `ReconstruirBancoDadosAsync()`:
- Confirmação prévia via callback (similar a `ConfirmarDeletarReciboCallback`)
- Roda em background (`Task.Run`)
- Mostra progresso via `StatusRecibos`

Adicionar `Func<string, Task<bool>>? ConfirmarAcaoBancoDadosCallback { get; set; }` para a confirmação.

---

### 3. Botão na UI (`PesagensView.axaml`)

Na aba Recibos, na barra superior (ao lado de "📊 Exportar Excel"):

```xml
<Button Content="🔄 Reconstruir Banco"
        Command="{Binding ReconstruirBancoDadosCommand}"
        IsEnabled="{Binding !ExportandoExcel}"
        ToolTip.Tip="Apaga todos os JSONs e reconstrói o banco-de-dados a partir dos PDFs de recibos"/>
```

---

### 4. Conectar callback de confirmação (`PesagensView.axaml.cs`)

Registrar `ConfirmarAcaoBancoDadosCallback` com uma `MessageBox`/dialog de confirmação (texto: "Isso irá apagar todos os JSONs do banco-de-dados e recriá-los a partir dos PDFs. Continuar?").

---

## Notas importantes

- **Colisão de nomes:** PDFs duplicados (ex: `Alex_28-04-2026..pdf` e `Alex_28-04-2026.pdf`) geram JSONs distintos — ambos são incluídos no somatório
- **PDFs sem itens parseados:** pular silenciosamente (não gerar JSON para eles)
- **Campos reservados:** `"data"` e `"status"` são ignorados por `LerItensJson` via `CamposReservados` — compatível
- **Git:** Não faz push automático — deixa o usuário sincronizar manualmente depois (banco-de-dados não tem botão de sync na tela atual)
- **Itens extras:** Items não catalogados (ex: `"OUTROS"`, `"Sucateado"`) são capturados pelo `ReciboParserService` via parser de extras
