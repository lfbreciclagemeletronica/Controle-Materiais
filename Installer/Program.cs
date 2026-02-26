using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace LFBInstaller;

class Program
{
    // ── Configurações ─────────────────────────────────────────────────────────
    const string GITHUB_OWNER   = "lfbreciclagemeletronica";
    const string GITHUB_REPO    = "Controle-Materiais";
    const string ASSET_NAME     = "ControleMateriais-win-x64.zip";
    const string APP_EXE        = "ControleMateriais.Desktop.exe";
    const string APP_FOLDER     = "ControleMateriais.LFB";
    const string APP_DISPLAY    = "Controle de Materiais LFB";
    const string GIT_URL        = "https://github.com/git-for-windows/git/releases/download/v2.44.0.windows.1/Git-2.44.0-64-bit.exe";

    static readonly string InstallDir  = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        APP_FOLDER);
    static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    static readonly string ShortcutPath = Path.Combine(DesktopPath, $"{APP_DISPLAY}.lnk");

    // ── Cores ─────────────────────────────────────────────────────────────────
    static void SetGreen()   { Console.ForegroundColor = ConsoleColor.Green; }
    static void SetCyan()    { Console.ForegroundColor = ConsoleColor.Cyan; }
    static void SetYellow()  { Console.ForegroundColor = ConsoleColor.Yellow; }
    static void SetMagenta() { Console.ForegroundColor = ConsoleColor.Magenta; }
    static void SetRed()     { Console.ForegroundColor = ConsoleColor.Red; }
    static void SetGray()    { Console.ForegroundColor = ConsoleColor.DarkGray; }
    static void SetWhite()   { Console.ForegroundColor = ConsoleColor.White; }
    static void Reset()      { Console.ResetColor(); }

    static void LogInfo(string msg)  { SetCyan();    Console.Write("  [*] "); Reset(); Console.WriteLine(msg); }
    static void LogOk(string msg)    { SetGreen();   Console.Write("  [+] "); Reset(); Console.WriteLine(msg); }
    static void LogWarn(string msg)  { SetYellow();  Console.Write("  [!] "); Reset(); Console.WriteLine(msg); }
    static void LogError(string msg) { SetRed();     Console.Write("  [X] "); Reset(); Console.WriteLine(msg); }
    static void LogStep(string msg)  { SetMagenta(); Console.Write("  >>> "); Reset(); Console.WriteLine(msg); }

    static void DrawHeader()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.Clear();
        SetGreen();
        Console.WriteLine();
        Console.WriteLine(@"  ██╗     ███████╗██████╗      ██████╗ ███████╗ ██████╗██╗██████╗  ");
        Console.WriteLine(@"  ██║     ██╔════╝██╔══██╗     ██╔══██╗██╔════╝██╔════╝██║██╔══██╗ ");
        Console.WriteLine(@"  ██║     █████╗  ██████╔╝     ██████╔╝█████╗  ██║     ██║██████╔╝ ");
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine(@"  ██║     ██╔══╝  ██╔══██╗     ██╔══██╗██╔══╝  ██║     ██║██╔══██╗ ");
        Console.WriteLine(@"  ███████╗██║     ██████╔╝     ██║  ██║███████╗╚██████╗██║██║  ██║ ");
        SetCyan();
        Console.WriteLine(@"  ╚══════╝╚═╝     ╚═════╝      ╚═╝  ╚═╝╚══════╝ ╚═════╝╚═╝╚═╝  ╚═╝");
        Console.WriteLine();
        SetCyan();
        Console.WriteLine("  ╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║    CONTROLE DE MATERIAIS — INSTALADOR v1.0  [WIN-X64]       ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════════════╝");
        Reset();
        Console.WriteLine();
    }

    static bool PromptYesNo(string question)
    {
        Console.WriteLine();
        SetYellow(); Console.Write("  [?] ");
        Reset(); Console.Write(question);
        SetWhite(); Console.Write(" [S/N] ");
        Reset();
        while (true)
        {
            var k = Console.ReadKey(intercept: true);
            if (k.KeyChar is 'S' or 's' or 'Y' or 'y')
            {
                SetGreen(); Console.WriteLine("S"); Reset();
                return true;
            }
            if (k.KeyChar is 'N' or 'n')
            {
                SetRed(); Console.WriteLine("N"); Reset();
                return false;
            }
        }
    }

    static void DrawBar(int percent, string label = "")
    {
        const int width = 50;
        int filled = (int)Math.Round(percent / 100.0 * width);
        int empty  = width - filled;
        Console.Write("\r  ");
        SetCyan();    Console.Write("[");
        SetGreen();   Console.Write(new string('█', filled));
        SetGray();    Console.Write(new string('░', empty));
        SetCyan();    Console.Write("] ");
        SetWhite();   Console.Write($"{percent,3}% ");
        SetGray();    Console.Write(label.Length > 40 ? label[..40] : label);
        Reset();
    }

    static void FinishBar() => Console.WriteLine();

    // ─────────────────────────────────────────────────────────────────────────
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        DrawHeader();

        // ── Verificar instalação existente ───────────────────────────────────
        LogStep("Verificando instalações existentes...");
        if (Directory.Exists(InstallDir))
        {
            LogWarn($"Versão existente detectada em:");
            SetYellow(); Console.WriteLine($"      {InstallDir}"); Reset();

            bool remove = PromptYesNo("Deseja remover a versão existente e instalar a mais nova?");
            if (!remove)
            {
                LogInfo("Instalação cancelada pelo usuário.");
                Pause(); return;
            }
            Console.WriteLine();
            LogStep("Removendo versão anterior...");
            await AnimateBar("Removendo arquivos antigos...", 600);
            try
            {
                Directory.Delete(InstallDir, recursive: true);
                if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath);
                LogOk("Versão anterior removida.");
            }
            catch (Exception ex)
            {
                LogError($"Falha ao remover: {ex.Message}");
                Pause(); return;
            }
            Console.WriteLine();
        }

        // ── Verificar Git ────────────────────────────────────────────────────
        LogStep("Verificando Git...");
        bool gitFound = IsGitInstalled();
        if (gitFound)
        {
            string ver = RunCommand("git", "--version");
            LogOk($"Git encontrado: {ver.Trim()}");
        }
        else
        {
            LogWarn("Git não encontrado no sistema.");
            bool instGit = PromptYesNo("Deseja instalar o Git for Windows agora?");
            if (instGit)
            {
                Console.WriteLine();
                await InstallGitAsync();
            }
            else
            {
                LogInfo("Pulando instalação do Git.");
            }
        }
        Console.WriteLine();

        // ── Buscar latest release no GitHub ──────────────────────────────────
        LogStep("Buscando latest release no GitHub...");
        string zipUrl;
        string tagName;
        try
        {
            (zipUrl, tagName) = await GetLatestReleaseAssetAsync();
            LogOk($"Release encontrada: {tagName}");
            SetGray(); Console.WriteLine($"      {zipUrl}"); Reset();
        }
        catch (Exception ex)
        {
            LogError($"Falha ao consultar GitHub: {ex.Message}");
            LogWarn("Verifique sua conexão ou se a release foi publicada.");
            Pause(); return;
        }
        Console.WriteLine();

        // ── Baixar ZIP ────────────────────────────────────────────────────────
        string zipPath = Path.Combine(Path.GetTempPath(), ASSET_NAME);
        LogStep("Baixando aplicativo...");
        Console.WriteLine();
        try
        {
            await DownloadWithProgressAsync(zipUrl, zipPath);
            LogOk("Download concluído.");
        }
        catch (Exception ex)
        {
            LogError($"Falha no download: {ex.Message}");
            Pause(); return;
        }
        Console.WriteLine();

        // ── Extrair e instalar ────────────────────────────────────────────────
        LogStep("Instalando arquivos...");
        Console.WriteLine();
        try
        {
            Directory.CreateDirectory(InstallDir);
            await ExtractWithProgressAsync(zipPath, InstallDir);
            File.Delete(zipPath);
            LogOk($"Instalado em: {InstallDir}");
        }
        catch (Exception ex)
        {
            LogError($"Falha na extração: {ex.Message}");
            Pause(); return;
        }
        Console.WriteLine();

        // ── Atalho na Área de Trabalho ────────────────────────────────────────
        bool atalho = PromptYesNo("Criar atalho na Área de Trabalho?");
        if (atalho)
        {
            Console.WriteLine();
            LogStep("Criando atalho...");
            await AnimateBar("Configurando atalho...", 400);
            CreateShortcut(
                shortcutPath: ShortcutPath,
                targetPath:   Path.Combine(InstallDir, APP_EXE),
                workDir:      InstallDir,
                description:  $"{APP_DISPLAY} — LFB Reciclagem Eletrônica");
            if (File.Exists(ShortcutPath))
                LogOk($"Atalho criado: {ShortcutPath}");
            else
                LogWarn("Atalho não pôde ser criado.");
        }
        else
        {
            LogInfo("Atalho não criado.");
        }
        Console.WriteLine();

        // ── Concluído ─────────────────────────────────────────────────────────
        SetGreen();
        Console.WriteLine("  ╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║                                                              ║");
        Console.WriteLine("  ║   [+] INSTALAÇÃO CONCLUÍDA COM SUCESSO!                     ║");
        Console.WriteLine("  ║                                                              ║");
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"  ║   Versão instalada : {tagName,-41}║");
        Console.WriteLine($"  ║   Local            : {InstallDir.PadRight(41)[..41]}║");
        Console.WriteLine("  ║                                                              ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════════════╝");
        Reset();
        Console.WriteLine();
        Pause();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static async Task<(string url, string tag)> GetLatestReleaseAssetAsync()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "LFB-Installer/1.0");
        string apiUrl = $"https://api.github.com/repos/{GITHUB_OWNER}/{GITHUB_REPO}/releases/latest";
        string json = await http.GetStringAsync(apiUrl);
        using var doc = JsonDocument.Parse(json);
        string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "unknown";
        var assets = doc.RootElement.GetProperty("assets");
        foreach (var asset in assets.EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? "";
            if (name.Equals(ASSET_NAME, StringComparison.OrdinalIgnoreCase))
            {
                string url = asset.GetProperty("browser_download_url").GetString()!;
                return (url, tag);
            }
        }
        throw new Exception($"Asset '{ASSET_NAME}' não encontrado na release '{tag}'.");
    }

    static async Task DownloadWithProgressAsync(string url, string destPath)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "LFB-Installer/1.0");
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        long? totalBytes = response.Content.Headers.ContentLength;
        using var stream = await response.Content.ReadAsStreamAsync();
        using var file   = File.Create(destPath);
        var buffer   = new byte[81920];
        long downloaded = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;
            if (totalBytes.HasValue)
            {
                int pct = (int)(downloaded * 100L / totalBytes.Value);
                string label = $"{downloaded / 1024 / 1024:0.0} MB / {totalBytes.Value / 1024 / 1024:0.0} MB";
                DrawBar(pct, label);
            }
            else
            {
                SetCyan(); Console.Write($"\r  Baixando... {downloaded / 1024} KB"); Reset();
            }
        }
        FinishBar();
    }

    static async Task ExtractWithProgressAsync(string zipPath, string destDir)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        int total = archive.Entries.Count;
        int done  = 0;
        foreach (var entry in archive.Entries)
        {
            string destFile = Path.Combine(destDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                Directory.CreateDirectory(destFile);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                entry.ExtractToFile(destFile, overwrite: true);
            }
            done++;
            int pct = (int)(done * 100L / total);
            DrawBar(pct, Path.GetFileName(entry.FullName));
            await Task.Yield();
        }
        FinishBar();
    }

    static async Task AnimateBar(string label, int durationMs)
    {
        int steps = 20;
        int delay = durationMs / steps;
        for (int i = 0; i <= steps; i++)
        {
            int pct = i * 100 / steps;
            DrawBar(pct, label);
            await Task.Delay(delay);
        }
        FinishBar();
    }

    static bool IsGitInstalled()
    {
        try
        {
            var p = new ProcessStartInfo("git", "--version")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(p);
            proc?.WaitForExit(3000);
            return proc?.ExitCode == 0;
        }
        catch { return false; }
    }

    static string RunCommand(string cmd, string args)
    {
        try
        {
            var p = new ProcessStartInfo(cmd, args)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(p)!;
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);
            return output;
        }
        catch { return ""; }
    }

    static async Task InstallGitAsync()
    {
        LogStep("Baixando Git for Windows...");
        string gitInstaller = Path.Combine(Path.GetTempPath(), "GitInstaller.exe");
        Console.WriteLine();
        try
        {
            await DownloadWithProgressAsync(GIT_URL, gitInstaller);
            LogOk("Download concluído.");
            LogStep("Instalando Git silenciosamente...");
            await AnimateBar("Aguardando instalação do Git...", 500);
            var p = new ProcessStartInfo(gitInstaller,
                "/VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS /COMPONENTS=icons,ext\\reg\\shellhere,assoc,assoc_sh")
            {
                UseShellExecute = true
            };
            var proc = Process.Start(p)!;
            proc.WaitForExit();
            if (File.Exists(gitInstaller)) File.Delete(gitInstaller);
            LogOk("Git instalado com sucesso.");
        }
        catch (Exception ex)
        {
            LogWarn($"Não foi possível instalar o Git: {ex.Message}");
        }
    }

    [System.Runtime.InteropServices.DllImport("ole32.dll")]
    static extern int CoCreateInstance(ref Guid clsid, IntPtr inner,
        uint context, ref Guid uuid, [System.Runtime.InteropServices.MarshalAs(
        System.Runtime.InteropServices.UnmanagedType.Interface)] out object? ppv);

    static void CreateShortcut(string shortcutPath, string targetPath, string workDir, string description)
    {
        try
        {
            // Usa WScript.Shell via PowerShell para criar o atalho (compatível com .NET single-file)
            string ps = $@"
$wsh = New-Object -ComObject WScript.Shell
$s   = $wsh.CreateShortcut('{shortcutPath.Replace("'", "''")}')
$s.TargetPath       = '{targetPath.Replace("'", "''")}'
$s.WorkingDirectory = '{workDir.Replace("'", "''")}'
$s.Description      = '{description.Replace("'", "''")}'
$s.IconLocation     = '{targetPath.Replace("'", "''")}',0
$s.Save()
";
            var psi = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -Command \"{ps.Replace("\"", "\\\"")}\"")
            {
                UseShellExecute = false,
                CreateNoWindow  = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };
            using var proc = Process.Start(psi)!;
            proc.WaitForExit(10000);
        }
        catch (Exception ex)
        {
            LogWarn($"Atalho não criado: {ex.Message}");
        }
    }

    static void Pause()
    {
        Console.WriteLine();
        SetGray(); Console.Write("  Pressione qualquer tecla para sair...");
        Reset();
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}
