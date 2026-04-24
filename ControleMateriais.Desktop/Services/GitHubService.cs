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

    // Diretório local do clone = pasta Pesagens (clone + arquivos no mesmo lugar)
    public static string RepoDir(string rootDir) =>
        Path.Combine(rootDir, "Pesagens");

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
            var r = await RunAsync("git", "pull --rebase origin HEAD", repoDir);
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
        var push = await RunAsync("git", "push origin HEAD", repoDir);
        if (push.exitCode != 0) throw new Exception($"Push falhou: {push.stderr}");
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
}

public class GitHubCredenciais
{
    public string Token      { get; set; } = string.Empty;
    public string GitUsuario { get; set; } = string.Empty;
    public string GitEmail   { get; set; } = string.Empty;
}
