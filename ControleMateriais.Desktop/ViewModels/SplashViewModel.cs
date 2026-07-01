using Avalonia.Threading;
using ControleMateriais.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.ViewModels;

public enum SyncStatus { Loading, Ok, Warn, Error }

public class SyncItem : INotifyPropertyChanged
{
    private SyncStatus _status = SyncStatus.Loading;
    private string _mensagem = string.Empty;

    public string Label { get; init; } = string.Empty;

    public SyncStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsLoading)); OnPropertyChanged(nameof(IsOk)); OnPropertyChanged(nameof(IsWarn)); OnPropertyChanged(nameof(IsError)); OnPropertyChanged(nameof(IsDone)); }
    }

    public string Mensagem
    {
        get => _mensagem;
        set { _mensagem = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> Detalhes { get; } = new();

    public bool IsLoading => _status == SyncStatus.Loading;
    public bool IsOk      => _status == SyncStatus.Ok;
    public bool IsWarn    => _status == SyncStatus.Warn;
    public bool IsError   => _status == SyncStatus.Error;
    public bool IsDone    => _status != SyncStatus.Loading;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? p = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}

public class SplashViewModel : INotifyPropertyChanged
{
    private static string RootDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                     "Downloads", "ControleMateriaisLFB");

    private bool _podeContinu  = false;
    private bool _semCredenciais = false;

    public bool PodeContinuar
    {
        get => _podeContinu;
        private set { _podeContinu = value; OnPropertyChanged(); }
    }

    public bool SemCredenciais
    {
        get => _semCredenciais;
        private set { _semCredenciais = value; OnPropertyChanged(); }
    }

    public SyncItem ItemRecibos    { get; } = new() { Label = "Atualizando recibos do sistema remoto..." };
    public SyncItem ItemPesagens   { get; } = new() { Label = "Atualizando pesagens do sistema remoto..." };
    public SyncItem ItemBancoDados { get; } = new() { Label = "Atualizando banco de dados do sistema remoto..." };
    public SyncItem ItemVendas     { get; } = new() { Label = "Verificando recibos de vendas..." };

    public async Task IniciarSincronizacaoAsync()
    {
        if (!GitHubService.CredenciaisExistem(RootDir))
        {
            UI(() =>
            {
                SemCredenciais = true;
                foreach (var item in new[] { ItemRecibos, ItemPesagens, ItemBancoDados, ItemVendas })
                {
                    item.Status   = SyncStatus.Error;
                    item.Mensagem = "GitHub não configurado";
                }
                PodeContinuar = true;
            });
            return;
        }

        // Pull dos 3 repos em paralelo
        var taskRecibos    = PullRecibosAsync();
        var taskPesagens   = PullPesagensAsync();
        var taskBancoDados = PullBancoDadosAsync();

        await Task.WhenAll(taskRecibos, taskPesagens, taskBancoDados);

        // Verificação de vendas (depende do banco de dados pronto)
        await VerificarVendasAsync();

        UI(() => PodeContinuar = true);
    }

    // Helper: marshal an action to the UI thread
    private static void UI(Action a) => Dispatcher.UIThread.Post(a);

    private async Task PullRecibosAsync()
    {
        try
        {
            var novos = await GitHubService.PullRecibosAsync(RootDir,
                msg => { /* progresso interno */ });

            UI(() =>
            {
                if (novos.Count == 0)
                {
                    ItemRecibos.Status   = SyncStatus.Ok;
                    ItemRecibos.Mensagem = "Recibos locais já atualizados, sem novos recibos";
                }
                else
                {
                    ItemRecibos.Status   = SyncStatus.Ok;
                    ItemRecibos.Mensagem = $"{novos.Count} novo(s) recibo(s) recebido(s)";
                    foreach (var (nome, data) in novos)
                    {
                        var dataFmt = FormatarData(data);
                        ItemRecibos.Detalhes.Add(string.IsNullOrEmpty(dataFmt)
                            ? $"• {nome}"
                            : $"• {nome} — {dataFmt}");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            var msg = GitHubService.ClassificarErroGit(ex.Message, "pull de Recibos");
            UI(() => { ItemRecibos.Status = SyncStatus.Error; ItemRecibos.Mensagem = msg; });
        }
    }

    private async Task PullPesagensAsync()
    {
        try
        {
            var novos = await GitHubService.PullPesagensAsync(RootDir,
                msg => { });

            UI(() =>
            {
                if (novos.Count == 0)
                {
                    ItemPesagens.Status   = SyncStatus.Ok;
                    ItemPesagens.Mensagem = "Pesagens locais já atualizadas, sem novas pesagens";
                }
                else
                {
                    ItemPesagens.Status   = SyncStatus.Ok;
                    ItemPesagens.Mensagem = $"{novos.Count} nova(s) pesagem(ns) recebida(s)";
                    foreach (var (nome, data) in novos)
                    {
                        var dataFmt = FormatarData(data);
                        ItemPesagens.Detalhes.Add(string.IsNullOrEmpty(dataFmt)
                            ? $"• {nome}"
                            : $"• {nome} — {dataFmt}");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            var msg = GitHubService.ClassificarErroGit(ex.Message, "pull de Pesagens");
            UI(() => { ItemPesagens.Status = SyncStatus.Error; ItemPesagens.Mensagem = msg; });
        }
    }

    private async Task PullBancoDadosAsync()
    {
        try
        {
            var novos = await GitHubService.PullBancoDadosAsync(RootDir,
                msg => { });

            UI(() =>
            {
                if (novos.Count == 0)
                {
                    ItemBancoDados.Status   = SyncStatus.Ok;
                    ItemBancoDados.Mensagem = "Banco de dados já atualizado, sem novos registros";
                }
                else
                {
                    ItemBancoDados.Status   = SyncStatus.Ok;
                    ItemBancoDados.Mensagem = $"{novos.Count} novo(s) registro(s) no banco de dados";
                    foreach (var (arquivo, cliente, data) in novos)
                    {
                        var dataFmt = FormatarData(data);
                        if (string.IsNullOrEmpty(cliente))
                            ItemBancoDados.Detalhes.Add($"• {arquivo}");
                        else
                            ItemBancoDados.Detalhes.Add(string.IsNullOrEmpty(dataFmt)
                                ? $"• {arquivo}: {cliente}"
                                : $"• {arquivo}: {cliente} — {dataFmt}");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            var msg = GitHubService.ClassificarErroGit(ex.Message, "pull de Banco de Dados");
            UI(() => { ItemBancoDados.Status = SyncStatus.Error; ItemBancoDados.Mensagem = msg; });
        }
    }

    private async Task VerificarVendasAsync()
    {
        try
        {
            var bancoDadosDir = GitHubService.BancoDadosRepoDir(RootDir);
            var recibosDir    = GitHubService.RecibosRepoDir(RootDir);
            var vendaDir      = Path.Combine(recibosDir, "Recibos_Venda");

            var avisos = new List<string>();

            if (!Directory.Exists(vendaDir))
            {
                UI(() => { ItemVendas.Status = SyncStatus.Ok; ItemVendas.Mensagem = "Sem novos recibos de vendas"; });
                return;
            }

            // Carregar todos os registros de venda existentes no banco de dados
            var vendasNoBanco = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(bancoDadosDir))
            {
                foreach (var jsonFile in Directory.GetFiles(bancoDadosDir, "venda-*.json"))
                {
                    try
                    {
                        var json = JsonNode.Parse(await File.ReadAllTextAsync(jsonFile));
                        var regs = json?["registros"]?.AsArray();
                        if (regs is null) continue;
                        foreach (var reg in regs)
                        {
                            var nome = reg?["nome"]?.GetValue<string>() ?? "";
                            var data = reg?["data"]?.GetValue<string>() ?? "";
                            vendasNoBanco.Add($"{nome}|{data}");
                        }
                    }
                    catch { }
                }
            }

            // Verificar PDFs de venda
            var pdfs = Directory.GetFiles(vendaDir, "*.pdf")
                .Where(f => !Path.GetFileName(f).StartsWith("ESTOQUE", StringComparison.OrdinalIgnoreCase))
                .ToList();

            int novosCount = 0;
            var re = new Regex(@"^(.+?)_(\d{2}-\d{2}-\d{4})(_\d{2}-\d{2})?$");

            foreach (var pdf in pdfs)
            {
                var semExt = Path.GetFileNameWithoutExtension(pdf);
                var m = re.Match(semExt);
                if (!m.Success) continue;

                var nome = m.Groups[1].Value.Replace("_", " ");
                var data = m.Groups[2].Value; // dd-MM-yyyy

                // Normaliza para dd-MM-yyyy (já está nesse formato)
                var chave = $"{nome}|{data}";
                if (!vendasNoBanco.Contains(chave))
                {
                    var dataFmt = FormatarData(data);
                    avisos.Add($"⚠ {nome} ({dataFmt}) não encontrado no banco de dados");
                    novosCount++;
                }
            }

            UI(() =>
            {
                if (avisos.Count == 0)
                {
                    ItemVendas.Status   = SyncStatus.Ok;
                    ItemVendas.Mensagem = "Sem novos recibos de vendas";
                }
                else
                {
                    ItemVendas.Status   = SyncStatus.Warn;
                    ItemVendas.Mensagem = $"{avisos.Count} recibo(s) de venda sem registro no banco de dados";
                    foreach (var a in avisos)
                        ItemVendas.Detalhes.Add(a);
                }
            });
        }
        catch (Exception ex)
        {
            UI(() => { ItemVendas.Status = SyncStatus.Error; ItemVendas.Mensagem = $"Erro: {ex.Message}"; });
        }
    }

    private static string FormatarData(string data)
    {
        if (string.IsNullOrWhiteSpace(data)) return string.Empty;
        // dd-MM-yyyy → dd/MM/yyyy
        return data.Replace("-", "/");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? p = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
