using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Services;

public static class GitHubService
{
    private const string CredenciaisFileName = "credenciais.json";

    // Valores padrão legados (usados somente para migrar credenciais.json existentes sem URL)
    private const string DefaultUrlPesagens     = "https://github.com/lfbreciclagemeletronica/Pesagens.git";
    private const string DefaultUrlRecibos      = "https://github.com/lfbreciclagemeletronica/Recibos.git";
    private const string DefaultUrlTabelaPrecos = "https://github.com/lfbreciclagemeletronica/TabelaPrecos.git";

    public static string CredenciaisPath(string rootDir) =>
        Path.Combine(rootDir, CredenciaisFileName);

    public static bool CredenciaisExistem(string rootDir) =>
        File.Exists(CredenciaisPath(rootDir));

    public static GitHubCredenciais? CarregarCredenciais(string rootDir)
    {
        var path = CredenciaisPath(rootDir);
        if (!File.Exists(path)) return null;

        var creds = JsonSerializer.Deserialize<GitHubCredenciais>(File.ReadAllText(path));
        if (creds is null) return null;

        // Migração: preenche URLs vazias com os valores que eram hardcoded e re-salva
        var alterado = false;
        if (string.IsNullOrWhiteSpace(creds.UrlPesagens))     { creds.UrlPesagens     = DefaultUrlPesagens;     alterado = true; }
        if (string.IsNullOrWhiteSpace(creds.UrlRecibos))      { creds.UrlRecibos      = DefaultUrlRecibos;      alterado = true; }
        if (string.IsNullOrWhiteSpace(creds.UrlTabelaPrecos)) { creds.UrlTabelaPrecos = DefaultUrlTabelaPrecos; alterado = true; }

        if (alterado)
            File.WriteAllText(path, JsonSerializer.Serialize(creds, new JsonSerializerOptions { WriteIndented = true }));

        return creds;
    }

    public static void SalvarCredenciais(
        string rootDir,
        string token,
        string gitUsuario,
        string gitEmail,
        string urlPesagens,
        string urlRecibos,
        string urlTabelaPrecos,
        string urlBancoDados)
    {
        Directory.CreateDirectory(rootDir);
        var obj = new GitHubCredenciais
        {
            Token           = token,
            GitUsuario      = gitUsuario,
            GitEmail        = gitEmail,
            UrlPesagens     = urlPesagens,
            UrlRecibos      = urlRecibos,
            UrlTabelaPrecos = urlTabelaPrecos,
            UrlBancoDados   = urlBancoDados
        };
        File.WriteAllText(CredenciaisPath(rootDir),
            JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
    }

    // Diretório local do clone de Pesagens
    public static string RepoDir(string rootDir) =>
        Path.Combine(rootDir, "Pesagens");

    // Diretório local do clone de Recibos
    public static string RecibosRepoDir(string rootDir) =>
        Path.Combine(rootDir, "Recibos");

    // Diretório local do clone de TabelaPrecos
    public static string TabelaPrecosRepoDir(string rootDir) =>
        Path.Combine(rootDir, "TabelaPrecos");

    // Diretório local do clone de BancoDados
    public static string BancoDadosRepoDir(string rootDir) =>
        Path.Combine(rootDir, "banco-de-dados");

    /// <summary>
    /// Garante que o repo BancoDados está clonado localmente.
    /// </summary>
    public static async Task GarantirBancoDadosRepoAsync(string rootDir, Action<string> progresso)
    {
        if (!CredenciaisExistem(rootDir))
            throw new InvalidOperationException("Configure as credenciais do GitHub antes de usar o estoque.");

        var creds = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlBancoDados))
            throw new InvalidOperationException("URL do repositório de Banco de Dados não configurada. Edite as credenciais do GitHub.");

        var remoteUrl = InjetarToken(creds.UrlBancoDados, creds.Token);
        var repoDir   = BancoDadosRepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");

        if (!Directory.Exists(gitDir))
        {
            progresso("Clonando repositório banco-de-dados...");
            if (Directory.Exists(repoDir))
                Directory.Delete(repoDir, true);
            Directory.CreateDirectory(repoDir);
            var r = await RunAsync("git", $"clone {remoteUrl} .", repoDir);
            if (r.exitCode != 0)
                throw new Exception($"Clone do repo banco-de-dados falhou: {r.stderr}");
            await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
            await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
            progresso("Repositório banco-de-dados pronto.");
        }
        else
        {
            progresso("Atualizando banco-de-dados (pull)...");
            await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
            await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
            await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
            await RunAsync("git", "fetch origin main", repoDir);
            await RunAsync("git", "rebase origin/main", repoDir);
        }
    }

    /// <summary>
    /// Salva o .json de estoque no repo banco-de-dados e faz push.
    /// </summary>
    public static async Task PublicarJsonBancoDadosAsync(string rootDir, string nomeArquivo, string conteudoJson,
                                                          Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;
        var creds = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlBancoDados)) return;

        var repoDir = BancoDadosRepoDir(rootDir);
        var gitDir  = Path.Combine(repoDir, ".git");
        if (!Directory.Exists(gitDir)) return;

        var remoteUrl = InjetarToken(creds.UrlBancoDados, creds.Token);
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        progresso?.Invoke("Atualizando banco-de-dados (pull)...");
        await RunAsync("git", "fetch origin main", repoDir);
        await RunAsync("git", "rebase origin/main", repoDir);

        var destino = Path.Combine(repoDir, nomeArquivo);
        await System.IO.File.WriteAllTextAsync(destino, conteudoJson);

        progresso?.Invoke("Salvando dados no banco-de-dados...");
        await RunAsync("git", $"add \"{nomeArquivo}\"", repoDir);
        var commit = await RunAsync("git", $"commit -m \"Dados recibo {nomeArquivo}\"", repoDir);
        if (commit.exitCode == 0)
        {
            progresso?.Invoke("Enviando banco-de-dados ao GitHub...");
            var branchAtual = await ObterBranchAtual(repoDir);
            await RunAsync("git", $"push origin {branchAtual}", repoDir);
        }
    }

    /// <summary>
    /// Remove o .json de estoque do repo banco-de-dados e faz push.
    /// </summary>
    public static async Task RemoverJsonBancoDadosAsync(string rootDir, string nomeArquivo,
                                                         Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;
        var creds = CarregarCredenciais(rootDir)!;

        var repoDir = BancoDadosRepoDir(rootDir);
        var gitDir  = Path.Combine(repoDir, ".git");

        // Se não tiver repo git, apenas remove o arquivo local
        if (!Directory.Exists(gitDir))
        {
            var localPath = Path.Combine(repoDir, nomeArquivo);
            if (System.IO.File.Exists(localPath)) System.IO.File.Delete(localPath);
            return;
        }

        if (string.IsNullOrWhiteSpace(creds.UrlBancoDados)) return;
        var remoteUrl = InjetarToken(creds.UrlBancoDados, creds.Token);
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        progresso?.Invoke("Atualizando banco-de-dados (pull)...");
        await RunAsync("git", "fetch origin main", repoDir);
        await RunAsync("git", "rebase origin/main", repoDir);

        progresso?.Invoke("Removendo dados do banco-de-dados...");
        var rm = await RunAsync("git", $"rm --ignore-unmatch \"{nomeArquivo}\"", repoDir);

        // Também remove localmente se ainda existir (caso não estivesse no git)
        var filePath = Path.Combine(repoDir, nomeArquivo);
        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

        if (rm.exitCode == 0 && !string.IsNullOrWhiteSpace(rm.stdout))
        {
            progresso?.Invoke("Commitando remoção...");
            var commit = await RunAsync("git", $"commit -m \"Excluir dados recibo {nomeArquivo}\"", repoDir);
            if (commit.exitCode == 0)
            {
                progresso?.Invoke("Enviando ao GitHub...");
                var branchAtual = await ObterBranchAtual(repoDir);
                await RunAsync("git", $"push origin {branchAtual}", repoDir);
            }
        }
    }

    /// <summary>
    /// Remove um registro de compra ou venda do arquivo JSON do banco de dados.
    /// Para compras: remove de compra-MM-YYYY.json onde nome == cliente
    /// Para vendas: remove de venda-DD-MM-YYYY.json onde nome == cliente E data == data
    /// Se o arquivo ficar vazio, deleta o arquivo JSON.
    /// </summary>
    public static bool RemoverRegistroDoJson(string rootDir, string tipo, string cliente, string data)
    {
        var dir = BancoDadosRepoDir(rootDir);
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
            var jsonOpts = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

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
                        deveRemover = nome.Equals(cliente, StringComparison.OrdinalIgnoreCase);
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
                File.WriteAllText(jsonPath, obj.ToJsonString(jsonOpts), Encoding.UTF8);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Garante que o repo Recibos está clonado localmente.
    /// Se já existir um diretório "Recibos" com PDFs (sem .git), migra os PDFs para o repo clonado e remove o diretório antigo.
    /// </summary>
    public static async Task GarantirRecibosRepoAsync(string rootDir, Action<string> progresso)
    {
        if (!CredenciaisExistem(rootDir))
            throw new InvalidOperationException("Configure as credenciais do GitHub antes de exportar.");

        var creds     = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlRecibos))
            throw new InvalidOperationException("URL do repositório de Recibos não configurada. Edite as credenciais do GitHub.");
        var remoteUrl = InjetarToken(creds.UrlRecibos, creds.Token);
        var repoDir   = RecibosRepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");
        var tempDir   = Path.Combine(rootDir, "recibos-repo-temp");

        if (!Directory.Exists(gitDir))
        {
            // Verifica se existe diretório Recibos com PDFs legados (sem .git)
            string[]? pdfsLegados = null;
            if (Directory.Exists(repoDir))
            {
                pdfsLegados = Directory.GetFiles(repoDir, "*.pdf", SearchOption.TopDirectoryOnly);
            }

            // Clona para pasta temporária
            progresso("Clonando repositório de Recibos...");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);
            var r = await RunAsync("git", $"clone {remoteUrl} .", tempDir);
            if (r.exitCode != 0)
                throw new Exception($"Clone do repo Recibos falhou: {r.stderr}");
            progresso("Clone concluído.");

            // Migra PDFs legados para o repo clonado
            if (pdfsLegados is { Length: > 0 })
            {
                progresso($"Migrando {pdfsLegados.Length} recibo(s) existente(s)...");
                foreach (var pdf in pdfsLegados)
                    File.Copy(pdf, Path.Combine(tempDir, Path.GetFileName(pdf)), overwrite: true);

                await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", tempDir);
                await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", tempDir);
                await RunAsync("git", "add .", tempDir);
                var commit = await RunAsync("git", "commit -m \"Migração de recibos existentes\"", tempDir);
                if (commit.exitCode == 0)
                {
                    progresso("Enviando recibos migrados ao GitHub...");
                    await RunAsync("git", "push origin HEAD", tempDir);
                }

                // Remove diretório Recibos antigo e renomeia o temp para Recibos
                Directory.Delete(repoDir, true);
            }
            else if (Directory.Exists(repoDir))
            {
                Directory.Delete(repoDir, true);
            }

            Directory.Move(tempDir, repoDir);
            progresso("Repositório de Recibos pronto.");
        }
        else
        {
            // Já clonado — apenas pull
            progresso("Atualizando repositório de Recibos...");
            await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
            await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
            await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
            // Guarda mudanças locais para não bloquear o rebase
            var stash = await RunAsync("git", "stash --include-untracked", repoDir);
            var temStash = stash.exitCode == 0 && !stash.stdout.Contains("No local changes");
            await RunAsync("git", "fetch origin main", repoDir);
            await RunAsync("git", "rebase origin/main", repoDir);
            if (temStash) await RunAsync("git", "stash pop", repoDir);
        }
    }

    /// <summary>
    /// Sincroniza o repo Recibos: pull dos novos arquivos remotos + push dos PDFs locais não commitados.
    /// </summary>
    public static async Task SincronizarRecibosAsync(string rootDir, Action<string> progresso)
    {
        if (!CredenciaisExistem(rootDir))
            throw new InvalidOperationException("Configure as credenciais do GitHub na tela inicial.");

        var creds     = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlRecibos))
            throw new InvalidOperationException("URL do repositório de Recibos não configurada. Edite as credenciais do GitHub.");
        var remoteUrl = InjetarToken(creds.UrlRecibos, creds.Token);
        var repoDir   = RecibosRepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");

        if (!Directory.Exists(gitDir))
        {
            // Repo ainda não clonado — usa o GarantirRecibosRepoAsync para clonar/migrar
            await GarantirRecibosRepoAsync(rootDir, progresso);
            return;
        }

        // Pull dos novos arquivos remotos
        progresso("Atualizando Recibos (pull)...");
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
        // Guarda mudanças locais para não bloquear o rebase
        var stash = await RunAsync("git", "stash --include-untracked", repoDir);
        var temStash = stash.exitCode == 0 && !stash.stdout.Contains("No local changes");
        await RunAsync("git", "fetch origin main", repoDir);
        var pull = await RunAsync("git", "rebase origin/main", repoDir);
        if (pull.exitCode != 0)
            throw new Exception($"Pull do repo Recibos falhou: {pull.stderr}");
        if (temStash) await RunAsync("git", "stash pop", repoDir);

        // Push de PDFs locais que ainda não foram commitados (untracked ou modified)
        progresso("Verificando recibos locais não enviados...");
        await RunAsync("git", "add .", repoDir);
        var status = await RunAsync("git", "status --porcelain", repoDir);
        if (!string.IsNullOrWhiteSpace(status.stdout))
        {
            progresso("Enviando recibos locais ao GitHub...");
            var commit = await RunAsync("git", "commit -m \"Sincronização de recibos locais\"", repoDir);
            if (commit.exitCode == 0)
            {
                var branchAtual = await ObterBranchAtual(repoDir);
                var push = await RunAsync("git", $"push origin {branchAtual}", repoDir);
                if (push.exitCode != 0)
                    throw new Exception($"Push do repo Recibos falhou: {push.stderr}");
            }
        }

        progresso("Recibos sincronizados.");
    }

    /// <summary>
    /// Garante que o repo TabelaPrecos está clonado localmente.
    /// Se já existir um diretório "TabelaPrecos" com JSONs (sem .git), migra os arquivos para o repo clonado.
    /// </summary>
    public static async Task GarantirTabelaPrecosRepoAsync(string rootDir, Action<string> progresso)
    {
        if (!CredenciaisExistem(rootDir))
            throw new InvalidOperationException("Configure as credenciais do GitHub antes de sincronizar.");

        var creds     = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlTabelaPrecos))
            throw new InvalidOperationException("URL do repositório de Tabela de Preços não configurada. Edite as credenciais do GitHub.");
        var remoteUrl = InjetarToken(creds.UrlTabelaPrecos, creds.Token);
        var repoDir   = TabelaPrecosRepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");
        var tempDir   = Path.Combine(rootDir, "tabelaprecos-repo-temp");

        if (!Directory.Exists(gitDir))
        {
            // Verifica arquivos legados (JSONs sem .git)
            string[]? jsonsLegados = null;
            if (Directory.Exists(repoDir))
                jsonsLegados = Directory.GetFiles(repoDir, "*.json", SearchOption.TopDirectoryOnly);

            // Clona para pasta temporária
            progresso("Clonando repositório de Tabelas de Preços...");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);
            var r = await RunAsync("git", $"clone {remoteUrl} .", tempDir);
            if (r.exitCode != 0)
                throw new Exception($"Clone do repo TabelaPrecos falhou: {r.stderr}");
            progresso("Clone concluído.");

            // Migra JSONs legados para o repo clonado
            if (jsonsLegados is { Length: > 0 })
            {
                progresso($"Migrando {jsonsLegados.Length} tabela(s) existente(s)...");
                await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", tempDir);
                await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", tempDir);
                foreach (var json in jsonsLegados)
                    File.Copy(json, Path.Combine(tempDir, Path.GetFileName(json)), overwrite: true);
                await RunAsync("git", "add .", tempDir);
                var commit = await RunAsync("git", "commit -m \"Migração de tabelas de preços existentes\"", tempDir);
                if (commit.exitCode == 0)
                {
                    progresso("Enviando tabelas migradas ao GitHub...");
                    await RunAsync("git", "push origin main", tempDir);
                }
                Directory.Delete(repoDir, true);
            }
            else if (Directory.Exists(repoDir))
            {
                Directory.Delete(repoDir, true);
            }

            Directory.Move(tempDir, repoDir);
            progresso("Repositório de Tabelas de Preços pronto.");
        }
        else
        {
            progresso("Atualizando repositório de Tabelas de Preços...");
            await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
            await RunAsync("git", "fetch origin main", repoDir);
            await RunAsync("git", "rebase origin/main", repoDir);
        }
    }

    /// <summary>
    /// Sincroniza o repo TabelaPrecos: pull + push de JSONs locais não commitados.
    /// Chama GarantirTabelaPrecosRepoAsync se ainda não clonado.
    /// </summary>
    public static async Task SincronizarTabelaPrecosAsync(string rootDir, Action<string> progresso)
    {
        if (!CredenciaisExistem(rootDir))
            throw new InvalidOperationException("Configure as credenciais do GitHub na tela inicial.");

        var creds     = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlTabelaPrecos))
            throw new InvalidOperationException("URL do repositório de Tabela de Preços não configurada. Edite as credenciais do GitHub.");
        var remoteUrl = InjetarToken(creds.UrlTabelaPrecos, creds.Token);
        var repoDir   = TabelaPrecosRepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");

        if (!Directory.Exists(gitDir))
        {
            await GarantirTabelaPrecosRepoAsync(rootDir, progresso);
            return;
        }

        progresso("Atualizando Tabelas de Preços (pull)...");
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
        await RunAsync("git", "fetch origin main", repoDir);
        var pull = await RunAsync("git", "rebase origin/main", repoDir);
        if (pull.exitCode != 0)
            throw new Exception($"Pull do repo TabelaPrecos falhou: {pull.stderr}");

        // Push de JSONs locais não commitados
        progresso("Verificando tabelas locais não enviadas...");
        await RunAsync("git", "add .", repoDir);
        var status = await RunAsync("git", "status --porcelain", repoDir);
        if (!string.IsNullOrWhiteSpace(status.stdout))
        {
            progresso("Enviando tabelas ao GitHub...");
            var commit = await RunAsync("git", "commit -m \"Sincronização de tabelas de preços\"", repoDir);
            if (commit.exitCode == 0)
            {
                var push = await RunAsync("git", "push origin main", repoDir);
                if (push.exitCode != 0)
                    throw new Exception($"Push do repo TabelaPrecos falhou: {push.stderr}");
            }
        }

        progresso("Tabelas de Preços sincronizadas.");
    }

    /// <summary>
    /// Faz commit e push de um arquivo JSON de tabela de preços específico.
    /// </summary>
    public static async Task PublicarTabelaAsync(string rootDir, string filePath, string mensagemCommit,
                                                  Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;

        var creds     = CarregarCredenciais(rootDir)!;
        var repoDir   = TabelaPrecosRepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");
        if (!Directory.Exists(gitDir)) return;
        if (string.IsNullOrWhiteSpace(creds.UrlTabelaPrecos)) return;

        var remoteUrl = InjetarToken(creds.UrlTabelaPrecos, creds.Token);
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        var destino = Path.Combine(repoDir, Path.GetFileName(filePath));
        if (File.Exists(filePath) && filePath != destino)
            File.Copy(filePath, destino, overwrite: true);

        progresso?.Invoke("Adicionando arquivo...");
        await RunAsync("git", $"add \"{Path.GetFileName(destino)}\"", repoDir);
        progresso?.Invoke("Commitando...");
        var commit = await RunAsync("git", $"commit -m \"{mensagemCommit}\"", repoDir);
        if (commit.exitCode == 0)
        {
            progresso?.Invoke("Enviando para o GitHub...");
            await RunAsync("git", "push origin main", repoDir);
        }
    }

    /// <summary>
    /// Remove um arquivo de tabela do repo e faz push.
    /// </summary>
    public static async Task RemoverTabelaAsync(string rootDir, string nomeArquivo, Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;

        var creds     = CarregarCredenciais(rootDir)!;
        var repoDir   = TabelaPrecosRepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");
        if (!Directory.Exists(gitDir)) return;
        if (string.IsNullOrWhiteSpace(creds.UrlTabelaPrecos)) return;

        var remoteUrl = InjetarToken(creds.UrlTabelaPrecos, creds.Token);
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        progresso?.Invoke("Removendo arquivo...");
        var rm = await RunAsync("git", $"rm --ignore-unmatch \"{nomeArquivo}\"", repoDir);
        if (rm.exitCode == 0 && !string.IsNullOrWhiteSpace(rm.stdout))
        {
            progresso?.Invoke("Commitando remoção...");
            var commit = await RunAsync("git", $"commit -m \"Excluir tabela {nomeArquivo}\"", repoDir);
            if (commit.exitCode == 0)
            {
                progresso?.Invoke("Enviando para o GitHub...");
                await RunAsync("git", "push origin main", repoDir);
            }
        }
    }

    /// <summary>
    /// Faz commit e push de um arquivo PDF no repo Recibos.
    /// </summary>
    public static async Task PublicarReciboAsync(string rootDir, string filePath, string mensagemCommit,
                                                  Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;

        var creds   = CarregarCredenciais(rootDir)!;
        var repoDir = RecibosRepoDir(rootDir);
        var gitDir  = Path.Combine(repoDir, ".git");
        if (!Directory.Exists(gitDir)) return;

        if (string.IsNullOrWhiteSpace(creds.UrlRecibos)) return;
        progresso?.Invoke("Configurando repositório...");
        var remoteUrl = InjetarToken(creds.UrlRecibos, creds.Token);
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        var destino = Path.Combine(repoDir, Path.GetFileName(filePath));
        if (!File.Exists(destino))
            File.Copy(filePath, destino);

        progresso?.Invoke("Adicionando arquivo...");
        await RunAsync("git", $"add \"{Path.GetFileName(filePath)}\"", repoDir);
        progresso?.Invoke("Commitando...");
        var commit = await RunAsync("git", $"commit -m \"{mensagemCommit}\"", repoDir);
        if (commit.exitCode == 0)
        {
            progresso?.Invoke("Enviando para o GitHub...");
            var branchAtual = await ObterBranchAtual(repoDir);
            await RunAsync("git", $"push origin {branchAtual}", repoDir);
        }
    }

    /// <summary>
    /// Faz commit e push de um PDF de venda no subdiretório Recibos_Venda dentro do repo Recibos.
    /// </summary>
    public static async Task PublicarReciboVendaAsync(string rootDir, string filePath, string mensagemCommit,
                                                       Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;
        var creds   = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlRecibos)) return;

        var repoDir = RecibosRepoDir(rootDir);
        var gitDir  = Path.Combine(repoDir, ".git");

        // Garante que o repo está clonado antes de tentar publicar
        if (!Directory.Exists(gitDir))
        {
            progresso?.Invoke("Clonando repositório de Recibos...");
            await GarantirRecibosRepoAsync(rootDir, msg => progresso?.Invoke(msg));
        }

        var remoteUrl = InjetarToken(creds.UrlRecibos, creds.Token);
        progresso?.Invoke("Configurando repositório...");
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        // Pull antes do push para evitar conflitos
        await RunAsync("git", "fetch origin main", repoDir);
        await RunAsync("git", "rebase origin/main", repoDir);

        var subDir  = Path.Combine(repoDir, "Recibos_Venda");
        Directory.CreateDirectory(subDir);

        // Copia PDF
        var destPdf = Path.Combine(subDir, Path.GetFileName(filePath));
        File.Copy(filePath, destPdf, overwrite: true);
        var relPdf = Path.Combine("Recibos_Venda", Path.GetFileName(filePath)).Replace('\\', '/');

        // Copia .meta.json se existir ao lado do PDF original
        var metaOrigem  = filePath + ".meta.json";
        var metaNome    = Path.GetFileName(filePath) + ".meta.json";
        var relMeta     = Path.Combine("Recibos_Venda", metaNome).Replace('\\', '/');
        if (File.Exists(metaOrigem))
            File.Copy(metaOrigem, Path.Combine(subDir, metaNome), overwrite: true);

        progresso?.Invoke("Adicionando arquivos...");
        await RunAsync("git", $"add \"{relPdf}\"", repoDir);
        if (File.Exists(metaOrigem))
            await RunAsync("git", $"add \"{relMeta}\"", repoDir);

        progresso?.Invoke("Commitando...");
        var commit = await RunAsync("git", $"commit -m \"{mensagemCommit}\"", repoDir);
        if (commit.exitCode == 0)
        {
            progresso?.Invoke("Enviando para o GitHub...");
            var branchAtual = await ObterBranchAtual(repoDir);
            await RunAsync("git", $"push origin {branchAtual}", repoDir);
        }
    }

    /// <summary>
    /// Remove um arquivo PDF do repo Recibos e faz push.
    /// </summary>
    public static async Task RemoverReciboAsync(string rootDir, string nomeArquivo, Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;

        var creds   = CarregarCredenciais(rootDir)!;
        var repoDir = RecibosRepoDir(rootDir);
        var gitDir  = Path.Combine(repoDir, ".git");
        if (!Directory.Exists(gitDir)) return;
        if (string.IsNullOrWhiteSpace(creds.UrlRecibos)) return;

        var remoteUrl = InjetarToken(creds.UrlRecibos, creds.Token);
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        progresso?.Invoke("Atualizando repositório (pull)...");
        await RunAsync("git", "fetch origin main", repoDir);
        await RunAsync("git", "rebase origin/main", repoDir);

        progresso?.Invoke("Removendo recibo...");
        var rm = await RunAsync("git", $"rm --ignore-unmatch \"{nomeArquivo}\"", repoDir);
        if (rm.exitCode == 0 && !string.IsNullOrWhiteSpace(rm.stdout))
        {
            progresso?.Invoke("Commitando remoção...");
            var commit = await RunAsync("git", $"commit -m \"Excluir recibo {nomeArquivo}\"", repoDir);
            if (commit.exitCode == 0)
            {
                progresso?.Invoke("Enviando para o GitHub...");
                await RunAsync("git", "push origin main", repoDir);
            }
        }
    }

    /// <summary>
    /// Remove um arquivo JSON de pesagem do repo Pesagens e faz push.
    /// </summary>
    public static async Task RemoverPesagemAsync(string rootDir, string nomeArquivo, Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;

        var creds   = CarregarCredenciais(rootDir)!;
        var repoDir = RepoDir(rootDir);
        var gitDir  = Path.Combine(repoDir, ".git");
        if (!Directory.Exists(gitDir))
        {
            var filePath = Path.Combine(repoDir, nomeArquivo);
            if (File.Exists(filePath)) File.Delete(filePath);
            return;
        }
        if (string.IsNullOrWhiteSpace(creds.UrlPesagens)) return;

        var remoteUrl = InjetarToken(creds.UrlPesagens, creds.Token);
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        progresso?.Invoke("Atualizando repositório (pull)...");
        await RunAsync("git", "fetch origin main", repoDir);
        await RunAsync("git", "rebase origin/main", repoDir);

        progresso?.Invoke("Removendo pesagem...");
        var rm = await RunAsync("git", $"rm --ignore-unmatch \"{nomeArquivo}\"", repoDir);
        if (rm.exitCode == 0 && !string.IsNullOrWhiteSpace(rm.stdout))
        {
            progresso?.Invoke("Commitando remoção...");
            var commit = await RunAsync("git", $"commit -m \"Excluir pesagem {nomeArquivo}\"", repoDir);
            if (commit.exitCode == 0)
            {
                progresso?.Invoke("Enviando para o GitHub...");
                await RunAsync("git", "push origin main", repoDir);
            }
        }
        else
        {
            var filePath = Path.Combine(repoDir, nomeArquivo);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    // Verifica se git está instalado
    public static async Task<bool> GitDisponivel()
    {
        try
        {
            var r = await RunAsync("git", "--version", null);
            return r.exitCode == 0;
        }
        catch { return false; }
    }

    // Instala git silenciosamente via winget
    public static async Task InstalarGitAsync(Action<string> progresso)
    {
        progresso("Instalando Git (aguarde)...");
        var r = await RunAsync("winget",
            "install --id Git.Git -e --source winget --silent --accept-package-agreements --accept-source-agreements",
            null);
        if (r.exitCode != 0)
            throw new Exception($"Falha ao instalar Git: {r.stderr}");
        progresso("Git instalado com sucesso.");
    }

    public static async Task EnviarArquivoAsync(
        string rootDir,
        string conteudoJson,
        string nomeArquivo,
        string mensagemCommit,
        Action<string> progresso)
    {
        var creds = CarregarCredenciais(rootDir)
            ?? throw new InvalidOperationException("credenciais.json não encontrado.");

        if (string.IsNullOrWhiteSpace(creds.UrlPesagens))
            throw new InvalidOperationException("URL do repositório de Pesagens não configurada. Edite as credenciais do GitHub.");
        var remoteUrl = InjetarToken(creds.UrlPesagens, creds.Token);
        var repoDir   = RepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");

        // 1. Clone ou pull
        if (!Directory.Exists(gitDir))
        {
            progresso("Clonando repositório...");
            Directory.CreateDirectory(repoDir);
            var r = await RunAsync("git", $"clone {remoteUrl} .", repoDir);
            if (r.exitCode != 0) throw new Exception($"Clone falhou: {r.stderr}");
            progresso("Clone concluído.");
        }
        else
        {
            progresso("Atualizando repositório (pull)...");
            await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
            await RunAsync("git", "fetch origin main", repoDir);
            var r = await RunAsync("git", "rebase origin/main", repoDir);
            if (r.exitCode != 0) throw new Exception($"Pull falhou: {r.stderr}");
            progresso("Repositório atualizado.");
        }

        // 2. Configurar identidade git
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);

        // 3. Escrever arquivo
        progresso("Salvando arquivo no repositório...");
        await File.WriteAllTextAsync(Path.Combine(repoDir, nomeArquivo), conteudoJson);

        // 4. Commit
        progresso("Criando commit...");
        await RunAsync("git", "add .", repoDir);
        var commit = await RunAsync("git", $"commit -m \"{mensagemCommit}\"", repoDir);
        if (commit.exitCode != 0 && !commit.stdout.Contains("nothing to commit"))
            throw new Exception($"Commit falhou: {commit.stderr}");

        // 5. Push
        progresso("Enviando ao GitHub (push)...");
        var push = await RunAsync("git", "push origin main", repoDir);
        if (push.exitCode != 0) throw new Exception($"Push falhou: {push.stderr}");
    }

    /// <summary>
    /// Injeta o token de autenticação em uma URL HTTPS do GitHub.
    /// Suporta URLs no formato https://github.com/... ou https://token@github.com/...
    /// </summary>
    public static string InjetarTokenPublico(string url, string token) => InjetarToken(url, token);

    private static string InjetarToken(string url, string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return url;
        var uri = new Uri(url);
        return $"{uri.Scheme}://{token}@{uri.Host}{uri.PathAndQuery}";
    }

    public static Task<(int exitCode, string stdout, string stderr)> RunGit(string args, string workDir) =>
        RunAsync("git", args, workDir);

    /// <summary>
    /// Pull simples do repo Recibos. Retorna lista de (NomeCliente, Data) dos PDFs novos
    /// detectados via git diff após o rebase.
    /// </summary>
    public static async Task<List<(string Nome, string Data)>> PullRecibosAsync(
        string rootDir, Action<string> progresso)
    {
        var novos = new List<(string, string)>();
        if (!CredenciaisExistem(rootDir)) return novos;
        var creds = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlRecibos)) return novos;

        var repoDir   = RecibosRepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");
        var remoteUrl = InjetarToken(creds.UrlRecibos, creds.Token);

        if (!Directory.Exists(gitDir))
        {
            progresso("Clonando repositório de Recibos...");
            Directory.CreateDirectory(repoDir);
            var cloneR = await RunAsync("git", $"clone {remoteUrl} .", repoDir);
            if (cloneR.exitCode != 0)
                throw new Exception(ClassificarErroGit(cloneR.stderr, "clone de Recibos"));
            await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
            await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
            return novos;
        }

        progresso("Buscando novos recibos...");
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
        // Salva HEAD antes do pull para poder diff depois
        var headAntes = await RunAsync("git", "rev-parse HEAD", repoDir);
        var stash = await RunAsync("git", "stash --include-untracked", repoDir);
        var temStash = stash.exitCode == 0 && !stash.stdout.Contains("No local changes");
        var fetchR = await RunAsync("git", "fetch origin main", repoDir);
        if (fetchR.exitCode != 0)
            throw new Exception(ClassificarErroGit(fetchR.stderr, "fetch de Recibos"));
        var rebaseR = await RunAsync("git", "rebase origin/main", repoDir);
        if (rebaseR.exitCode != 0)
            throw new Exception(ClassificarErroGit(rebaseR.stderr, "rebase de Recibos"));
        if (temStash) await RunAsync("git", "stash pop", repoDir);

        // Detectar arquivos novos
        var shaAntes = headAntes.exitCode == 0 ? headAntes.stdout.Trim() : string.Empty;
        if (!string.IsNullOrEmpty(shaAntes))
        {
            var diff = await RunAsync("git", $"diff --name-only --diff-filter=A {shaAntes} HEAD", repoDir);
            foreach (var line in diff.stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var arquivo = line.Trim();
                if (!arquivo.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) continue;
                // Nome do arquivo: NomeCliente_dd-MM-yyyy.pdf
                var semExt = Path.GetFileNameWithoutExtension(arquivo);
                var m1 = System.Text.RegularExpressions.Regex.Match(semExt, @"^(.+?)_(\d{2}-\d{2}-\d{4})(_\d{2}-\d{2})?$");
                if (m1.Success)
                    novos.Add((m1.Groups[1].Value.Replace("_", " "), m1.Groups[2].Value));
                else
                    novos.Add((semExt, ""));
            }
        }
        return novos;
    }

    /// <summary>
    /// Pull simples do repo Pesagens. Retorna lista de (NomeCliente, Data) dos JSONs novos.
    /// </summary>
    public static async Task<List<(string Nome, string Data)>> PullPesagensAsync(
        string rootDir, Action<string> progresso)
    {
        var novos = new List<(string, string)>();
        if (!CredenciaisExistem(rootDir)) return novos;
        var creds = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlPesagens)) return novos;

        var repoDir   = RepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");
        var remoteUrl = InjetarToken(creds.UrlPesagens, creds.Token);

        if (!Directory.Exists(gitDir))
        {
            progresso("Clonando repositório de Pesagens...");
            Directory.CreateDirectory(repoDir);
            var cloneP = await RunAsync("git", $"clone {remoteUrl} .", repoDir);
            if (cloneP.exitCode != 0)
                throw new Exception(ClassificarErroGit(cloneP.stderr, "clone de Pesagens"));
            await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
            await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
            return novos;
        }

        progresso("Buscando novas pesagens...");
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
        var headAntes = await RunAsync("git", "rev-parse HEAD", repoDir);
        var fetchP = await RunAsync("git", "fetch origin main", repoDir);
        if (fetchP.exitCode != 0)
            throw new Exception(ClassificarErroGit(fetchP.stderr, "fetch de Pesagens"));
        var rebaseP = await RunAsync("git", "rebase origin/main", repoDir);
        if (rebaseP.exitCode != 0)
            throw new Exception(ClassificarErroGit(rebaseP.stderr, "rebase de Pesagens"));

        var shaAntes = headAntes.exitCode == 0 ? headAntes.stdout.Trim() : string.Empty;
        if (!string.IsNullOrEmpty(shaAntes))
        {
            var diff = await RunAsync("git", $"diff --name-only --diff-filter=A {shaAntes} HEAD", repoDir);
            foreach (var line in diff.stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var arquivo = line.Trim();
                if (!arquivo.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                var semExt = Path.GetFileNameWithoutExtension(arquivo);
                var m1 = System.Text.RegularExpressions.Regex.Match(semExt, @"^(.+?)_(\d{2}-\d{2}-\d{4})(_\d{2}-\d{2})?$");
                if (m1.Success)
                    novos.Add((m1.Groups[1].Value.Replace("_", " "), m1.Groups[2].Value));
                else
                    novos.Add((semExt, ""));
            }
        }
        return novos;
    }

    /// <summary>
    /// Pull simples do repo BancoDados. Retorna lista de (Arquivo, Cliente, Data) de registros novos.
    /// </summary>
    public static async Task<List<(string Arquivo, string Cliente, string Data)>> PullBancoDadosAsync(
        string rootDir, Action<string> progresso)
    {
        var novos = new List<(string Arquivo, string Cliente, string Data)>();
        if (!CredenciaisExistem(rootDir)) return novos;
        var creds = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlBancoDados)) return novos;

        var repoDir   = BancoDadosRepoDir(rootDir);
        var gitDir    = Path.Combine(repoDir, ".git");
        var remoteUrl = InjetarToken(creds.UrlBancoDados, creds.Token);

        if (!Directory.Exists(gitDir))
        {
            progresso("Clonando repositório banco-de-dados...");
            Directory.CreateDirectory(repoDir);
            var cloneB = await RunAsync("git", $"clone {remoteUrl} .", repoDir);
            if (cloneB.exitCode != 0)
                throw new Exception(ClassificarErroGit(cloneB.stderr, "clone de Banco de Dados"));
            await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
            await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
            return novos;
        }

        progresso("Buscando novos registros...");
        await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);
        var headAntes = await RunAsync("git", "rev-parse HEAD", repoDir);
        var fetchB = await RunAsync("git", "fetch origin main", repoDir);
        if (fetchB.exitCode != 0)
            throw new Exception(ClassificarErroGit(fetchB.stderr, "fetch de Banco de Dados"));
        var rebaseB = await RunAsync("git", "rebase origin/main", repoDir);
        if (rebaseB.exitCode != 0)
            throw new Exception(ClassificarErroGit(rebaseB.stderr, "rebase de Banco de Dados"));

        var shaAntes = headAntes.exitCode == 0 ? headAntes.stdout.Trim() : string.Empty;
        if (string.IsNullOrEmpty(shaAntes)) return novos;

        // Arquivos modificados ou adicionados
        var diff = await RunAsync("git", $"diff --name-only --diff-filter=AM {shaAntes} HEAD", repoDir);
        foreach (var line in diff.stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var arquivo = line.Trim();
            if (!arquivo.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            var fullPath = Path.Combine(repoDir, arquivo);
            if (!File.Exists(fullPath)) continue;
            try
            {
                var json = JsonNode.Parse(await File.ReadAllTextAsync(fullPath));
                var registros = json?["registros"]?.AsArray();
                if (registros is null) { novos.Add((arquivo, "", "")); continue; }
                foreach (var reg in registros)
                {
                    var nome = reg?["nome"]?.GetValue<string>() ?? "";
                    var data = reg?["data-recibo"]?.GetValue<string>()
                            ?? reg?["data"]?.GetValue<string>()
                            ?? "";
                    novos.Add((arquivo, nome, data));
                }
            }
            catch { novos.Add((arquivo, "", "")); }
        }
        return novos;
    }

    /// <summary>
    /// Commita o .json no repo banco-de-dados SEM fetch/rebase e SEM push.
    /// Use durante a sessão; o push ocorre ao fechar o app.
    /// </summary>
    public static async Task CommitJsonBancoDadosAsync(string rootDir, string nomeArquivo, string conteudoJson,
                                                        Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;
        var creds = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlBancoDados)) return;

        var repoDir = BancoDadosRepoDir(rootDir);
        var gitDir  = Path.Combine(repoDir, ".git");
        if (!Directory.Exists(gitDir)) return;

        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        var destino = Path.Combine(repoDir, nomeArquivo);
        await System.IO.File.WriteAllTextAsync(destino, conteudoJson);

        progresso?.Invoke("Salvando registro no banco de dados...");
        await RunAsync("git", $"add \"{nomeArquivo}\"", repoDir);
        await RunAsync("git", $"commit -m \"Atualizar dados {nomeArquivo}\"", repoDir);
        progresso?.Invoke("Registro salvo localmente.");
    }

    /// <summary>
    /// Remove o .json do repo banco-de-dados via git rm SEM fetch/rebase e SEM push.
    /// Use durante a sessão; o push ocorre ao fechar o app.
    /// </summary>
    public static async Task CommitRemoverJsonBancoDadosAsync(string rootDir, string nomeArquivo,
                                                               Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;
        var creds = CarregarCredenciais(rootDir)!;

        var repoDir = BancoDadosRepoDir(rootDir);
        var gitDir  = Path.Combine(repoDir, ".git");

        if (!Directory.Exists(gitDir))
        {
            var localPath = Path.Combine(repoDir, nomeArquivo);
            if (System.IO.File.Exists(localPath)) System.IO.File.Delete(localPath);
            return;
        }

        if (string.IsNullOrWhiteSpace(creds.UrlBancoDados)) return;

        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        progresso?.Invoke("Removendo registro do banco de dados...");
        var rm = await RunAsync("git", $"rm --ignore-unmatch \"{nomeArquivo}\"", repoDir);

        var filePath = Path.Combine(repoDir, nomeArquivo);
        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

        if (rm.exitCode == 0 && !string.IsNullOrWhiteSpace(rm.stdout))
        {
            await RunAsync("git", $"commit -m \"Excluir dados {nomeArquivo}\"", repoDir);
            progresso?.Invoke("Registro removido localmente.");
        }
    }

    /// <summary>
    /// Copia PDF + .meta.json para Recibos_Venda e commita SEM fetch/rebase e SEM push.
    /// Use durante a sessão; o push ocorre ao fechar o app.
    /// </summary>
    public static async Task CommitReciboVendaLocalAsync(string rootDir, string filePath, string mensagemCommit,
                                                          Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;
        var creds = CarregarCredenciais(rootDir)!;
        if (string.IsNullOrWhiteSpace(creds.UrlRecibos)) return;

        var repoDir = RecibosRepoDir(rootDir);
        var gitDir  = Path.Combine(repoDir, ".git");
        if (!Directory.Exists(gitDir)) return;

        await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
        await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

        var subDir = Path.Combine(repoDir, "Recibos_Venda");
        Directory.CreateDirectory(subDir);

        var destPdf = Path.Combine(subDir, Path.GetFileName(filePath));
        File.Copy(filePath, destPdf, overwrite: true);
        var relPdf = Path.Combine("Recibos_Venda", Path.GetFileName(filePath)).Replace('\\', '/');

        var metaOrigem = filePath + ".meta.json";
        var metaNome   = Path.GetFileName(filePath) + ".meta.json";
        var relMeta    = Path.Combine("Recibos_Venda", metaNome).Replace('\\', '/');
        if (File.Exists(metaOrigem))
            File.Copy(metaOrigem, Path.Combine(subDir, metaNome), overwrite: true);

        progresso?.Invoke("Adicionando recibo ao repositório local...");
        await RunAsync("git", $"add \"{relPdf}\"", repoDir);
        if (File.Exists(metaOrigem))
            await RunAsync("git", $"add \"{relMeta}\"", repoDir);

        await RunAsync("git", $"commit -m \"{mensagemCommit}\"", repoDir);
        progresso?.Invoke("Recibo registrado localmente.");
    }

    /// <summary>
    /// Faz fetch + rebase + push de todos os repositórios com commits pendentes.
    /// Chamado apenas ao fechar o aplicativo.
    /// </summary>
    public static async Task SincronizarTudoAoFecharAsync(string rootDir, Action<string>? progresso = null)
    {
        if (!CredenciaisExistem(rootDir)) return;
        var creds = CarregarCredenciais(rootDir)!;

        async Task SincronizarRepo(string repoDir, string remoteUrl, string nomeRepo)
        {
            var gitDir = Path.Combine(repoDir, ".git");
            if (!Directory.Exists(gitDir)) return;

            progresso?.Invoke($"Sincronizando {nomeRepo}...");
            await RunAsync("git", $"remote set-url origin {remoteUrl}", repoDir);
            await RunAsync("git", $"config user.email \"{creds.GitEmail}\"", repoDir);
            await RunAsync("git", $"config user.name \"{creds.GitUsuario}\"", repoDir);

            await RunAsync("git", "fetch origin main", repoDir);
            await RunAsync("git", "rebase origin/main", repoDir);

            var status = await RunAsync("git", "status --porcelain", repoDir);
            if (!string.IsNullOrWhiteSpace(status.stdout))
            {
                await RunAsync("git", "add .", repoDir);
                await RunAsync("git", $"commit -m \"Sincronização automática ao fechar\"", repoDir);
            }

            var log = await RunAsync("git", "log origin/main..HEAD --oneline", repoDir);
            if (!string.IsNullOrWhiteSpace(log.stdout))
            {
                progresso?.Invoke($"Enviando {nomeRepo} ao GitHub...");
                var branch = await ObterBranchAtual(repoDir);
                await RunAsync("git", $"push origin {branch}", repoDir);
                progresso?.Invoke($"{nomeRepo} sincronizado.");
            }
            else
            {
                progresso?.Invoke($"{nomeRepo} já atualizado.");
            }
        }

        if (!string.IsNullOrWhiteSpace(creds.UrlBancoDados))
        {
            try
            {
                await SincronizarRepo(
                    BancoDadosRepoDir(rootDir),
                    InjetarToken(creds.UrlBancoDados, creds.Token),
                    "Banco de Dados");
            }
            catch (Exception ex)
            {
                progresso?.Invoke($"[Banco de Dados] {ClassificarErroGit(ex.Message, "sincronização de Banco de Dados")}");
            }
        }

        if (!string.IsNullOrWhiteSpace(creds.UrlRecibos))
        {
            try
            {
                await SincronizarRepo(
                    RecibosRepoDir(rootDir),
                    InjetarToken(creds.UrlRecibos, creds.Token),
                    "Recibos");
            }
            catch (Exception ex)
            {
                progresso?.Invoke($"[Recibos] {ClassificarErroGit(ex.Message, "sincronização de Recibos")}");
            }
        }

        if (!string.IsNullOrWhiteSpace(creds.UrlPesagens))
        {
            try
            {
                await SincronizarRepo(
                    RepoDir(rootDir),
                    InjetarToken(creds.UrlPesagens, creds.Token),
                    "Pesagens");
            }
            catch (Exception ex)
            {
                progresso?.Invoke($"[Pesagens] {ClassificarErroGit(ex.Message, "sincronização de Pesagens")}");
            }
        }

        progresso?.Invoke("Sincronização concluída.");
    }

    /// <summary>
    /// Classifica o stderr de um comando git e retorna uma mensagem orientativa.
    /// </summary>
    internal static string ClassificarErroGit(string stderr, string operacao)
    {
        var s = stderr?.ToLowerInvariant() ?? string.Empty;

        if (s.Contains("authentication failed") || s.Contains("invalid username or password")
            || s.Contains("bad credentials") || s.Contains("401"))
            return $"Falha de autenticação em '{operacao}': token inválido ou expirado. " +
                   "Reconfigure as credenciais do GitHub nas configurações.";

        if (s.Contains("repository not found") || s.Contains("not found") || s.Contains("404")
            || s.Contains("does not exist"))
            return $"Repositório não encontrado em '{operacao}'. " +
                   "Verifique a URL do repositório nas configurações do GitHub.";

        if (s.Contains("unable to connect") || s.Contains("could not resolve host")
            || s.Contains("network") || s.Contains("failed to connect") || s.Contains("timeout"))
            return $"Sem conexão em '{operacao}': não foi possível alcançar o GitHub. " +
                   "Verifique sua conexão com a internet e tente novamente.";

        if (s.Contains("permission denied") || s.Contains("403"))
            return $"Permissão negada em '{operacao}'. " +
                   "O token pode não ter acesso a este repositório.";

        if (s.Contains("conflict") || s.Contains("merge conflict"))
            return $"Conflito de merge em '{operacao}'. " +
                   "Execute 'git rebase --abort' na pasta do repositório e sincronize novamente.";

        var detalhe = string.IsNullOrWhiteSpace(stderr) ? string.Empty : $"\nDetalhe: {stderr.Trim()}";
        return $"Erro na operação '{operacao}'.{detalhe}\nTente reiniciar o aplicativo.";
    }

    private static async Task<(int exitCode, string stdout, string stderr)> RunAsync(
        string exe, string args, string? workDir)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory       = workDir ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };
        using var p = new Process { StartInfo = psi };
        p.Start();
        var stdout = await p.StandardOutput.ReadToEndAsync();
        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return (p.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Obtém o nome da branch atual do repositório Git local.
    /// </summary>
    private static async Task<string> ObterBranchAtual(string repoDir)
    {
        var result = await RunAsync("git", "rev-parse --abbrev-ref HEAD", repoDir);
        return result.exitCode == 0 ? result.stdout.Trim() : "main";
    }
}

public class GitHubCredenciais
{
    public string Token           { get; set; } = string.Empty;
    public string GitUsuario      { get; set; } = string.Empty;
    public string GitEmail        { get; set; } = string.Empty;
    public string UrlPesagens     { get; set; } = string.Empty;
    public string UrlRecibos      { get; set; } = string.Empty;
    public string UrlTabelaPrecos { get; set; } = string.Empty;
    public string UrlBancoDados   { get; set; } = string.Empty;
}
