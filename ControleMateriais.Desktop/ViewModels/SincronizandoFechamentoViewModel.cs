namespace ControleMateriais.Desktop.ViewModels;

public class SincronizandoFechamentoViewModel : ViewModelBase
{
    public SyncItem SyncRecibos    { get; } = new() { Label = "Recibos" };
    public SyncItem SyncPesagens   { get; } = new() { Label = "Pesagens" };
    public SyncItem SyncBancoDados { get; } = new() { Label = "Banco de Dados" };

    private bool _concluido;
    public bool Concluido
    {
        get => _concluido;
        set { _concluido = value; OnPropertyChanged(); }
    }

    public void IniciarLoading()
    {
        SyncRecibos.Status    = SyncStatus.Loading; SyncRecibos.Mensagem    = string.Empty;
        SyncPesagens.Status   = SyncStatus.Loading; SyncPesagens.Mensagem   = string.Empty;
        SyncBancoDados.Status = SyncStatus.Loading; SyncBancoDados.Mensagem = string.Empty;
        Concluido = false;
    }

    public void MarcarRepoOk(string nomeRepo, string mensagem)
    {
        var item = ResolverItem(nomeRepo);
        if (item is null) return;
        item.Status   = SyncStatus.Ok;
        item.Mensagem = mensagem;
    }

    public void MarcarRepoErro(string nomeRepo, string erro)
    {
        var item = ResolverItem(nomeRepo);
        if (item is null) return;
        item.Status   = SyncStatus.Error;
        item.Mensagem = erro;
    }

    public void MarcarConcluido() => Concluido = true;

    private SyncItem? ResolverItem(string nomeRepo) => nomeRepo switch
    {
        "Recibos"      => SyncRecibos,
        "Pesagens"     => SyncPesagens,
        "Banco de Dados" => SyncBancoDados,
        _ => null
    };
}
