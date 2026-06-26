# Atualização de Exclusão de Recibos com Atualização de JSON

## Descrição

Esta implementação atualiza a funcionalidade de exclusão de recibos para remover registros dos arquivos JSON do banco de dados (`compra-MM-YYYY.json` e `venda-DD-MM-YYYY.json`) além de deletar os PDFs, recalcular o estoque e sincronizar com GitHub quando configurado.

## Arquivos Modificados

### 1. Services/GitHubService.cs

#### Adicionadas using statements
```csharp
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
```

#### Novo método público estático: `RemoverRegistroDoJson`

**Assinatura:**
```csharp
public static bool RemoverRegistroDoJson(string rootDir, string tipo, string cliente, string data)
```

**Funcionalidade:**
- Remove registros de compra ou venda dos arquivos JSON do banco de dados
- Para **compras**: lê `compra-MM-YYYY.json` e remove registros onde `nome == cliente`
- Para **vendas**: lê `venda-DD-MM-YYYY.json` e remove registros onde `nome == cliente` E `data == data`
- Se o array de registros ficar vazio, deleta o arquivo JSON
- Retorna `true` se modificou o arquivo, `false` caso contrário

**Tratamento de data:**
- Recebe data no formato `dd/MM/yyyy` (do meta.json)
- Para compras: converte para `MM-yyyy` para encontrar o arquivo correto
- Para vendas: converte para `dd-MM-yyyy` para encontrar o arquivo correto e comparar com o campo `data` do registro

**Edge cases:**
- Se o arquivo JSON não existir, retorna `false` (não é erro)
- Se parsing falhar, retorna `false` (não é erro)
- Se o registro não existir no JSON, não modifica nada e retorna `false`
- Se existirem múltiplos registros para o mesmo cliente e data, deleta todos
- Se o JSON ficar vazio após exclusão, deleta o arquivo JSON

---

### 2. ViewModels/EstoqueViewModel.cs

#### Método atualizado: `ExcluirReciboVendaAsync`

**Alterações:**

1. **Extrai dados do .meta.json antes de deletar:**
   ```csharp
   string cliente = item.NomeCliente;
   string data = item.DataCriacao;
   var metaPath = item.CaminhoCompleto + ".meta.json";
   if (File.Exists(metaPath))
   {
       try
       {
           var node = JsonNode.Parse(File.ReadAllText(metaPath));
           if (node?["cliente"] is JsonNode nc) cliente = nc.GetValue<string>();
           if (node?["data"] is JsonNode nd) data = nd.GetValue<string>();
       }
       catch { }
   }
   ```

2. **Remove registro do JSON de vendas:**
   ```csharp
   bool jsonModificado = GitHubService.RemoverRegistroDoJson(_rootDir, "venda", cliente, data);
   ```

3. **Sincroniza JSON modificado com GitHub:**
   ```csharp
   if (jsonModificado && GitHubService.CredenciaisExistem(_rootDir))
   {
       var mesAno = DateTime.ParseExact(data, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
       var nomeJson = $"venda-{mesAno}.json";
       await GitHubService.RemoverJsonBancoDadosAsync(_rootDir, nomeJson, msg => Status = msg);
   }
   ```

4. **Deleta PDF e meta.json** (mantido da implementação anterior)

5. **Recarrega para recalcular estoque:**
   ```csharp
   Recarregar();
   ```

**Comportamento anterior:**
- Apenas deletava o PDF e o .meta.json
- Não atualizava o JSON do banco de dados
- Não recalcular o estoque

---

### 3. ViewModels/PesagensViewModel.cs

#### Método atualizado: `DeletarReciboAsync`

**Alterações:**

1. **Extrai dados do .meta.json ou usa dados do item:**
   ```csharp
   string cliente = item.NomeArquivo;
   string data = item.DataCriacao; // Formato dd/MM/yyyy
   var metaPath = item.CaminhoCompleto + ".meta.json";
   if (File.Exists(metaPath))
   {
       try
       {
           var node = JsonNode.Parse(File.ReadAllText(metaPath));
           if (node?["cliente"] is JsonNode nc) cliente = nc.GetValue<string>();
           if (node?["data"] is JsonNode nd) data = nd.GetValue<string>();
       }
       catch { }
   }
   ```

2. **Remove registro do JSON de compras:**
   ```csharp
   bool jsonModificado = GitHubService.RemoverRegistroDoJson(RootDir, "compra", cliente, data);
   ```

3. **Sincroniza JSON modificado com GitHub:**
   ```csharp
   if (jsonModificado && GitHubService.CredenciaisExistem(RootDir))
   {
       var mesAno = DateTime.ParseExact(data, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString("MM-yyyy");
       var nomeJson = $"compra-{mesAno}.json";
       MostrarStatusRecibos("Removendo dados do banco-de-dados...", ok: true);
       await GitHubService.RemoverJsonBancoDadosAsync(RootDir, nomeJson, msg => MostrarStatusRecibos(msg, ok: true));
   }
   ```

4. **Deleta PDF e meta.json** (mantido da implementação anterior)

5. **Remove do GitHub** (mantido da implementação anterior)

6. **Recalcula estoque:**
   ```csharp
   EstoqueRecarregarCallback?.Invoke();
   ```

**Comportamento anterior:**
- Deletava um JSON com o mesmo nome do PDF (estrutura antiga)
- Não atualizava o `compra-MM-YYYY.json`
- Não recalcular o estoque

---

## Como Testar

### Pré-requisitos
- Aplicação compilada e executando
- Credenciais do GitHub configuradas (opcional para teste local)

### Teste 1: Exclusão de Recibo de Venda

1. **Criar um recibo de venda:**
   - Vá para a tela de Venda
   - Crie um recibo para um cliente (ex: "Cliente Teste")
   - Anote o nome do cliente e a data do recibo

2. **Verificar que o recibo foi criado:**
   - Vá para a tela de Estoque
   - Verifique que o recibo aparece na lista de recibos de venda
   - Verifique que o arquivo `venda-DD-MM-YYYY.json` foi criado em `banco-de-dados/`
   - Abra o arquivo JSON e verifique que contém o registro do cliente

3. **Excluir o recibo:**
   - Na tela de Estoque, clique no botão de exclusão do recibo
   - Confirme a exclusão

4. **Verificar resultados:**
   - O recibo deve desaparecer da lista de recibos de venda
   - O arquivo `venda-DD-MM-YYYY.json` deve ter o registro removido
   - Se o JSON ficar vazio, o arquivo deve ser deletado
   - O estoque deve ser recalculado (os materiais vendidos devem voltar ao estoque)
   - Se GitHub estiver configurado, o arquivo JSON deve ser removido do repositório remoto

### Teste 2: Exclusão de Recibo de Compra (Pesagem)

1. **Criar um recibo de compra:**
   - Vá para a tela de Pesagens
   - Crie uma pesagem para um cliente (ex: "Fornecedor Teste")
   - Gere o PDF da pesagem
   - Sincronize com o GitHub se configurado

2. **Verificar que o recibo foi criado:**
   - Vá para a aba Recibos na tela de Pesagens
   - Verifique que o recibo aparece na lista
   - Verifique que o arquivo `compra-MM-YYYY.json` foi criado em `banco-de-dados/`
   - Abra o arquivo JSON e verifique que contém o registro do cliente

3. **Excluir o recibo:**
   - Na aba Recibos, clique no botão de exclusão do recibo
   - Confirme a exclusão

4. **Verificar resultados:**
   - O recibo deve desaparecer da lista de recibos
   - O arquivo `compra-MM-YYYY.json` deve ter o registro removido
   - Se o JSON ficar vazio, o arquivo deve ser deletado
   - O estoque deve ser recalculado (os materiais comprados devem ser removidos do estoque)
   - Se GitHub estiver configurado, o arquivo JSON deve ser removido do repositório remoto

### Teste 3: Edge Cases

#### Caso 1: Recibo não existe no JSON
- Tente excluir um recibo que não tem registro correspondente no JSON
- Resultado esperado: PDF é deletado, JSON não é modificado, não há erro

#### Caso 2: Múltiplos registros para o mesmo cliente e data
- Crie múltiplos recibos para o mesmo cliente na mesma data
- Exclua um deles
- Resultado esperado: Todos os registros correspondentes são removidos do JSON

#### Caso 3: JSON fica vazio após exclusão
- Crie um recibo de venda/compra que seja o único registro no JSON
- Exclua o recibo
- Resultado esperado: O arquivo JSON é deletado (não fica vazio)

#### Caso 4: Arquivo .meta.json não existe
- Exclua um recibo que não tem .meta.json
- Resultado esperado: Usa dados do item (NomeCliente/DataCriacao), não há erro

#### Caso 5: GitHub não configurado
- Desconfigure as credenciais do GitHub
- Exclua um recibo
- Resultado esperado: PDF e JSON local são deletados, estoque recalculado, não há erro de sincronização

### Teste 4: Recálculo de Estoque

1. **Verificar estoque inicial:**
   - Vá para a tela de Estoque
   - Anote os valores atuais do estoque

2. **Criar e excluir recibo de venda:**
   - Crie um recibo de venda vendendo materiais
   - Verifique que o estoque diminuiu
   - Exclua o recibo
   - Verifique que o estoque voltou ao valor original

3. **Criar e excluir recibo de compra:**
   - Crie um recibo de compra comprando materiais
   - Verifique que o estoque aumentou
   - Exclua o recibo
   - Verifique que o estoque voltou ao valor original

### Teste 5: Sincronização com GitHub

1. **Configure o GitHub:**
   - Configure as credenciais do GitHub com URL do repositório `banco-de-dados`

2. **Crie um recibo:**
   - Crie um recibo de venda ou compra
   - Verifique que o arquivo JSON foi criado localmente e enviado ao GitHub

3. **Exclua o recibo:**
   - Exclua o recibo
   - Verifique que o arquivo JSON foi atualizado ou removido localmente
   - Verifique que a alteração foi enviada ao GitHub

4. **Verifique no repositório remoto:**
   - Acesse o repositório `banco-de-dados` no GitHub
   - Verifique que o arquivo JSON foi atualizado ou removido

---

## Resumo das Melhorias

1. **Integridade dos dados:** Os arquivos JSON do banco de dados agora são atualizados corretamente ao excluir recibos
2. **Recálculo automático:** O estoque é recalculado automaticamente após exclusão
3. **Sincronização com GitHub:** Alterações nos JSONs são sincronizadas com o repositório remoto
4. **Tratamento de edge cases:** Implementação robusta que lida com arquivos faltando, JSONs vazios, múltiplos registros, etc.
5. **Consistência:** Tanto recibos de venda quanto de compra agora seguem o mesmo padrão de exclusão
