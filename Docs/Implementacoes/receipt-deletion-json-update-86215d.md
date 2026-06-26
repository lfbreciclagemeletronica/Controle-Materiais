# Plano de Exclusão de Recibos com Atualização de JSON

Este plano descreve a atualização da funcionalidade de exclusão de recibos para remover registros dos arquivos JSON do banco de dados (compra-MM-YYYY.json e venda-DD-MM-YYYY.json) além de deletar os PDFs, recalcular o estoque e sincronizar com GitHub quando configurado.

## Análise do Estado Atual

**Funcionalidade atual:**
- `EstoqueViewModel.ExcluirReciboVendaAsync`: Deleta apenas o PDF e o .meta.json de recibos de venda
- `PesagensViewModel.DeletarReciboAsync`: Deleta o PDF e um JSON com o mesmo nome (estrutura antiga)
- Ambos não atualizam os arquivos JSON do banco de dados (compra-MM-YYYY.json, venda-DD-MM-YYYY.json)
- Não recalcular o estoque após exclusão

**Estrutura de arquivos:**
- `compra-MM-YYYY.json`: Contém registros de compras do mês com estrutura `{ "mes": "06-2026", "registros": [{ "nome": "cliente", "materiais": [...] }] }`
- `venda-DD-MM-YYYY.json`: Contém registros de vendas do dia com estrutura `{ "mes": "06-2026", "data": "09-06-2026", "registros": [{ "nome": "cliente", "materiais": [...] }] }`
- `.pdf.meta.json`: Contém `{ "cliente": "nome", "pesoTotal": 1.200, "valorVenda": 200.00, "data": "05/05/2026" }`

## Requisitos

1. **Aplicar a ambos os tipos de recibos**: Compras (pesagem) e Vendas
2. **Deletar PDF e atualizar JSON**: Remover o PDF e os registros correspondentes do JSON do banco de dados
3. **Extrair dados do meta.json**: Usar o arquivo .meta.json para obter nome do cliente e data
4. **Edge cases**:
   - Se o recibo não existir no JSON, não modificar o JSON
   - Se existirem múltiplos registros para o mesmo cliente e data, deletar todos
   - Se o JSON ficar vazio após exclusão, deletar o arquivo JSON
5. **Recalcular estoque**: Chamar `Recarregar()` após exclusão para recalcular o estoque
6. **Sincronizar com GitHub**: Se credenciais configuradas, remover também do repositório remoto

## Plano de Implementação

### Fase 1: Criar método auxiliar para remoção de registros de JSON

**Arquivo:** `EstoqueViewModel.cs`
**Novo método privado:** `RemoverRegistroDoJson`

**Funcionalidade:**
- Aceitar parâmetros: tipo (compra/venda), nome do cliente, data (formato dd/MM/yyyy do meta.json)
- Para compras: Ler `compra-MM-YYYY.json`, remover registros onde `nome` == cliente
- Para vendas: Ler `venda-DD-MM-YYYY.json`, remover registros onde `nome` == cliente E `data` == data
- Se o array de registros ficar vazio, deletar o arquivo JSON
- Salvar o JSON atualizado
- Retornar true se modificou o arquivo, false caso contrário

**Implementação:**
```csharp
private bool RemoverRegistroDoJson(string tipo, string cliente, string data)
{
    var dir = GitHubService.BancoDadosRepoDir(_rootDir);
    if (!Directory.Exists(dir)) return false;

    string? jsonPath = null;
    string? campoData = null;

    if (tipo.Equals("compra", StringComparison.OrdinalIgnoreCase))
    {
        // Extrair mês/ano da data (dd/MM/yyyy -> MM-yyyy)
        if (!DateTime.TryParseExact(data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return false;
        var mesAno = dt.ToString("MM-yyyy");
        jsonPath = Path.Combine(dir, $"compra-{mesAno}.json");
    }
    else if (tipo.Equals("venda", StringComparison.OrdinalIgnoreCase))
    {
        // Converter data para formato dd-MM-yyyy
        if (!DateTime.TryParseExact(data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return false;
        var dataChave = dt.ToString("dd-MM-yyyy");
        jsonPath = Path.Combine(dir, $"venda-{dataChave}.json");
        campoData = dataChave;
    }

    if (!File.Exists(jsonPath)) return false;

    try
    {
        var obj = JsonNode.Parse(File.ReadAllText(jsonPath))?.AsObject();
        if (obj is null || !obj.ContainsKey("registros")) return false;

        var registros = obj["registros"]!.AsArray();
        var originalCount = registros.Count;

        // Filtrar registros
        var novosRegistros = new JsonArray();
        foreach (var reg in registros)
        {
            if (reg is JsonObject regObj)
            {
                var nome = regObj.ContainsKey("nome") ? regObj["nome"]!.GetValue<string>() : string.Empty;
                
                bool deveRemover = false;
                if (tipo.Equals("compra", StringComparison.OrdinalIgnoreCase))
                {
                    deveRemover = nome.Equals(cliente, StringComparison.OrdinalIgnoreCase);
                }
                else if (tipo.Equals("venda", StringComparison.OrdinalIgnoreCase))
                {
                    var regData = regObj.ContainsKey("data") ? regObj["data"]!.GetValue<string>() : string.Empty;
                    deveRemover = nome.Equals(cliente, StringComparison.OrdinalIgnoreCase) && 
                                  regData.Equals(campoData, StringComparison.OrdinalIgnoreCase);
                }

                if (!deveRemover)
                    novosRegistros.Add(reg);
            }
        }

        if (novosRegistros.Count == originalCount) return false; // Nenhum registro removido

        if (novosRegistros.Count == 0)
        {
            // Deletar o arquivo JSON se ficar vazio
            File.Delete(jsonPath);
        }
        else
        {
            obj["registros"] = novosRegistros;
            File.WriteAllText(jsonPath, obj.ToJsonString(_jsonOpts), Encoding.UTF8);
        }

        return true;
    }
    catch
    {
        return false;
    }
}
```

### Fase 2: Atualizar ExcluirReciboVendaAsync em EstoqueViewModel

**Arquivo:** `EstoqueViewModel.cs`
**Método:** `ExcluirReciboVendaAsync` (linhas 164-180)

**Alterações:**
1. Ler dados do .meta.json antes de deletar
2. Chamar `RemoverRegistroDoJson("venda", cliente, data)`
3. Se modificou o JSON, sincronizar com GitHub usando `GitHubService.RemoverJsonBancoDadosAsync`
4. Manter deleção do PDF e meta.json
5. Chamar `Recarregar()` ao final para recalcular estoque

**Nova implementação:**
```csharp
private async Task ExcluirReciboVendaAsync(ReciboVendaItem? item)
{
    if (item is null) return;
    if (ConfirmarExclusaoCallback is not null)
    {
        var ok = await ConfirmarExclusaoCallback($"Excluir recibo \"{item.NomeArquivo}\"?");
        if (!ok) return;
    }
    try
    {
        // Extrair dados do meta.json antes de deletar
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

        // Remover registro do JSON de vendas
        bool jsonModificado = RemoverRegistroDoJson("venda", cliente, data);
        
        // Sincronizar JSON modificado com GitHub
        if (jsonModificado && GitHubService.CredenciaisExistem(_rootDir))
        {
            var mesAno = DateTime.ParseExact(data, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
            var nomeJson = $"venda-{mesAno}.json";
            await GitHubService.RemoverJsonBancoDadosAsync(_rootDir, nomeJson, msg => Status = msg);
        }

        // Deletar PDF e meta.json
        if (File.Exists(item.CaminhoCompleto)) File.Delete(item.CaminhoCompleto);
        if (File.Exists(metaPath)) File.Delete(metaPath);

        // Recarregar para recalcular estoque
        Recarregar();
    }
    catch (Exception ex) { Status = $"Erro ao excluir: {ex.Message}"; }
}
```

### Fase 3: Atualizar DeletarReciboAsync em PesagensViewModel

**Arquivo:** `PesagensViewModel.cs`
**Método:** `DeletarReciboAsync` (linhas 496-536)

**Alterações:**
1. Ler dados do .meta.json antes de deletar
2. Chamar método auxiliar para remover de `compra-MM-YYYY.json` (precisa criar método em PesagensViewModel ou mover para GitHubService)
3. Se modificou o JSON, sincronizar com GitHub
4. Manter deleção do PDF
5. Chamar `EstoqueRecarregarCallback?.Invoke()` para recalcular estoque

**Nota:** Como PesagensViewModel não tem acesso direto aos métodos de EstoqueViewModel, precisamos:
- Opção A: Criar o método `RemoverRegistroDoJson` em uma classe estática de utilitários (Services/JsonHelperService.cs)
- Opção B: Criar o método em GitHubService como método público estático

**Recomendação:** Opção B - Adicionar a GitHubService para manter consistência com outros métodos de manipulação de JSON.

### Fase 4: Adicionar RemoverRegistroDoJson em GitHubService

**Arquivo:** `GitHubService.cs`
**Novo método público estático:** `RemoverRegistroDoJson`

**Funcionalidade:** Mesma lógica descrita na Fase 1, mas como método público estático para ser usado por ambos ViewModels.

**Assinatura:**
```csharp
public static bool RemoverRegistroDoJson(string rootDir, string tipo, string cliente, string data)
```

### Fase 5: Atualizar chamadas nos ViewModels

**EstoqueViewModel:**
- Usar `GitHubService.RemoverRegistroDoJson(_rootDir, "venda", cliente, data)`

**PesagensViewModel:**
- Usar `GitHubService.RemoverRegistroDoJson(RootDir, "compra", cliente, data)`
- Chamar `EstoqueRecarregarCallback?.Invoke()` ao final

## Ordem de Implementação

1. **Fase 4** - Adicionar RemoverRegistroDoJson em GitHubService (base para as outras fases)
2. **Fase 2** - Atualizar ExcluirReciboVendaAsync em EstoqueViewModel
3. **Fase 3** - Atualizar DeletarReciboAsync em PesagensViewModel
4. **Testes** - Verificar exclusão de ambos os tipos de recibos

## Considerações Especiais

### Formato de data
- meta.json usa formato "dd/MM/yyyy" (ex: "05/05/2026")
- JSON de compras usa "MM-yyyy" (ex: "05-2026")
- JSON de vendas usa "dd-MM-yyyy" (ex: "05-05-2026")
- Conversão necessária entre formatos

### Sincronização com GitHub
- Para vendas: Usar `RemoverJsonBancoDadosAsync` com nome do arquivo `venda-DD-MM-YYYY.json`
- Para compras: Usar `RemoverJsonBancoDadosAsync` com nome do arquivo `compra-MM-YYYY.json`
- Se o arquivo JSON foi deletado (ficou vazio), o método ainda deve ser chamado para garantir remoção do remoto

### Recálculo de estoque
- EstoqueViewModel: Chamar `Recarregar()` que já recalcula o estoque
- PesagensViewModel: Chamar `EstoqueRecarregarCallback?.Invoke()` para notificar EstoqueViewModel

### Tratamento de erros
- Se JSON não existir, não é erro - apenas não modificar nada
- Se parsing falhar, não é erro - usar dados do item (NomeCliente, DataCriacao)
- Se sincronização Git falhar, mostrar erro mas não impedir deleção local
