using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;

namespace ControleMateriais.Desktop.Services;

public enum LogLevel
{
    Info,
    Aviso,
    Erro
}

public class LogEntry
{
    public DateTime Timestamp { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public LogLevel Level { get; init; }
    public string Origem { get; init; } = string.Empty;
    public string Mensagem { get; init; } = string.Empty;

    public string LevelTexto => Level switch
    {
        LogLevel.Info  => "INFO",
        LogLevel.Aviso => "AVISO",
        LogLevel.Erro  => "ERRO",
        _ => "INFO"
    };

    public string Linha =>
        $"{Timestamp:HH:mm:ss.fff} [{CorrelationId}] [{LevelTexto,-5}] {Origem}: {Mensagem}";
}

/// <summary>
/// Logger simples em memória para debugging local do aplicativo Desktop.
///
/// Toda operação relevante (salvar recibo, sincronizar, editar/inativar fornecedor, etc.)
/// deve gerar um CorrelationId via <see cref="NewCorrelationId"/> no início e usá-lo em
/// todas as mensagens de log daquela operação, para que seja possível filtrar/rastrear
/// todas as mensagens de um mesmo processamento na tela de Logs.
///
/// As mensagens são escritas no Console/Debug output (visível ao rodar via terminal ou
/// pela janela de Output da IDE) e também mantidas em memória para exibição na janela
/// "Ver Logs" do próprio aplicativo.
/// </summary>
public static class AppLogger
{
    private const int MaxEntradas = 2000;
    private static readonly object _lock = new();

    public static ObservableCollection<LogEntry> Entries { get; } = new();

    /// <summary>Gera um novo identificador curto para correlacionar as mensagens de uma operação.</summary>
    public static string NewCorrelationId() => Guid.NewGuid().ToString("N")[..8];

    public static void Info(string correlationId, string origem, string mensagem) =>
        Log(LogLevel.Info, correlationId, origem, mensagem);

    public static void Aviso(string correlationId, string origem, string mensagem) =>
        Log(LogLevel.Aviso, correlationId, origem, mensagem);

    public static void Erro(string correlationId, string origem, string mensagem) =>
        Log(LogLevel.Erro, correlationId, origem, mensagem);

    public static void Erro(string correlationId, string origem, Exception ex) =>
        Log(LogLevel.Erro, correlationId, origem, ex.Message);

    private static void Log(LogLevel level, string correlationId, string origem, string mensagem)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            CorrelationId = correlationId,
            Level = level,
            Origem = origem,
            Mensagem = mensagem
        };

        // Sempre escreve no output (Console + Debug), independente da UI estar aberta.
        Console.WriteLine(entry.Linha);
        System.Diagnostics.Debug.WriteLine(entry.Linha);

        lock (_lock)
        {
            void Adicionar()
            {
                Entries.Add(entry);
                while (Entries.Count > MaxEntradas)
                    Entries.RemoveAt(0);
            }

            if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
                Adicionar();
            else
                Avalonia.Threading.Dispatcher.UIThread.Post(Adicionar);
        }
    }

    public static void Clear()
    {
        void Limpar() => Entries.Clear();

        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            Limpar();
        else
            Avalonia.Threading.Dispatcher.UIThread.Post(Limpar);
    }
}
