using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Services;

public enum EstadoVersao { Atualizada, NovaDisponivel, EmDesenvolvimento }

public static class UpdateCheckService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/lfbreciclagemeletronica/Controle-Materiais/releases/latest";

    public static Version VersaoAtual =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public static async Task<(EstadoVersao Estado, string Tag, string Url)?> VerificarAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ControleMateriais");
            var json = await http.GetStringAsync(LatestReleaseUrl);
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString();
            var url = doc.RootElement.GetProperty("html_url").GetString() ?? "";
            if (tag is null || !Version.TryParse(tag.TrimStart('v', 'V'), out var remota)) return null;

            var atual = VersaoAtual;
            var local = new Version(atual.Major, atual.Minor, Math.Max(atual.Build, 0));
            var remotaNorm = new Version(remota.Major, remota.Minor, Math.Max(remota.Build, 0));
            var estado = remotaNorm > local ? EstadoVersao.NovaDisponivel
                       : remotaNorm < local ? EstadoVersao.EmDesenvolvimento
                       : EstadoVersao.Atualizada;
            return (estado, tag, url);
        }
        catch
        {
            return null;
        }
    }
}
