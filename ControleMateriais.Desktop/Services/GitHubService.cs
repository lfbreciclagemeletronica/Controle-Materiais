using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Services;

public static class GitHubService
{
    private const string CredenciaisFileName = "credenciais.json";
    private const string RepoOwner = "lfbreciclagemeletronica";
    private const string RepoName  = "Pesagens";
    private const string ReciboRepoName = "Recibos";

    public static string CredenciaisPath(string rootDir) =>
        Path.Combine(rootDir, CredenciaisFileName);

    public static bool CredenciaisExistem(string rootDir) =>
        File.Exists(CredenciaisPath(rootDir));

    public static GitHubCredenciais? CarregarCredenciais(string rootDir)
    {
        var path = CredenciaisPath(rootDir);
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<GitHubCredenciais>(File.ReadAllText(path));
    }

    public static void SalvarCredenciais(string rootDir, string token, string gitUsuario, string gitEmail)
    {
        Directory.CreateDirectory(rootDir);
        var obj = new GitHubCredenciais { Token = token, GitUsuario = gitUsuario, GitEmail = gitEmail };
        File.WriteAllText(CredenciaisPath(rootDir),
            JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
    }

    // Diretório local do clone de Pesagens
    public static string RepoDir(string rootDir) =>
        Path.Combine(rootDir, "Pesagens");

    // Diretório local do clone de Recibos
    public static string RecibosRepoDir(string rootDir) =>
        Path.Combine(rootDir, "Recibos");

    /// <summary>
    /// Garante que o repo Recibos está clonado localmente.
    /// Se já existir um diretório "Recibos" com PDFs (sem .git), migra os PDFs para o repo clonado e remove o diretório antigo.
    /// </summary>
    public static async Task GarantirRecibosRepoAsync(string rootDir, Action<string> progresso)
    {
        if (!CredenciaisExistem(rootDir))
            throw new InvalidOperationException("Configure as credenciais do GitHub antes de exportar.");

        var creds     = CarregarCredenciais(rootDir)!;
        var remoteUrl = $"https://{creds.Token}@github.com/{RepoOwner}/{ReciboRepoName}.git";
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
            await RunAsync("git", "fetch origin main", repoDir);
            await RunAsync("git", "rebase origin/main", repoDir);
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
        var remoteUrl = $"https://{creds.Token}@github.com/{RepoOwner}/{ReciboRepoName}.git";
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
        await RunAsync("git", "fetch origin main", repoDir);
        var pull = await RunAsync("git", "rebase origin/main", repoDir);
        if (pull.exitCode != 0)
            throw new Exception($"Pull do repo Recibos falhou: {pull.stderr}");

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
                var push = await RunAsync("git", "push origin main", repoDir);
                if (push.exitCode != 0)
                    throw new Exception($"Push do repo Recibos falhou: {push.stderr}");
            }
        }

        progresso("Recibos sincronizados.");
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

        progresso?.Invoke("Configurando repositório...");
        var remoteUrl = $"https://{creds.Token}@github.com/{RepoOwner}/{ReciboRepoName}.git";
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
            await RunAsync("git", "push origin main", repoDir);
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

        // git com token embutido na URL
        var remoteUrl = $"https://{creds.Token}@github.com/{RepoOwner}/{RepoName}.git";
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

    public static Task<(int exitCode, string stdout, string stderr)> RunGit(string args, string workDir) =>
        RunAsync("git", args, workDir);

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
}

public class GitHubCredenciais
{
    public string Token      { get; set; } = string.Empty;
    public string GitUsuario { get; set; } = string.Empty;
    public string GitEmail   { get; set; } = string.Empty;
}
