using ControleMateriais.Desktop.Serialization;
using ControleMateriais.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ControleMateriais.Desktop.ViewModels
{
    public class PriceTableManagerViewModel : ViewModelBase
    {
        private readonly ObservableCollection<MaterialItem> _itensMain;

        // ── Diretório de persistência ──────────────────────────────────────────
        private static string BaseDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                         "Downloads", "TabelaPrecosControleMateriais");

        // ── Lista de tabelas salvas ────────────────────────────────────────────
        public ObservableCollection<TabelaPrecoInfo> Tabelas { get; } = new();

        // ── Tabela selecionada na lista ────────────────────────────────────────
        private TabelaPrecoInfo? _tabelaSelecionada;
        public TabelaPrecoInfo? TabelaSelecionada
        {
            get => _tabelaSelecionada;
            set
            {
                if (value != _tabelaSelecionada)
                {
                    _tabelaSelecionada = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TemTabelaSelecionada));
                    (SalvarTabelaCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (AtivarTabelaCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (DeletarTabelaCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    if (value != null)
                        CarregarEdicaoAsync(value).ConfigureAwait(false);
                }
            }
        }

        public bool TemTabelaSelecionada => _tabelaSelecionada != null;

        // ── Modo: criando nova tabela ──────────────────────────────────────────
        private bool _criandoNova;
        public bool CriandoNova
        {
            get => _criandoNova;
            set { if (value != _criandoNova) { _criandoNova = value; OnPropertyChanged(); } }
        }

        private string _novoNome = string.Empty;
        public string NovoNome
        {
            get => _novoNome;
            set
            {
                if (value != _novoNome)
                {
                    _novoNome = value;
                    OnPropertyChanged();
                    (SalvarNovaCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        // ── Itens editáveis na tabela em edição ───────────────────────────────
        public ObservableCollection<ItemPrecoWrapper> ItensEdicao { get; } = new();

        // ── Tabela ativa (nome do JSON que está sendo usado na tela principal) ─
        private string? _tabelaAtivaArquivo;
        public string? TabelaAtivaArquivo
        {
            get => _tabelaAtivaArquivo;
            private set { _tabelaAtivaArquivo = value; OnPropertyChanged(); OnPropertyChanged(nameof(TabelaAtivaLabel)); }
        }
        public string TabelaAtivaLabel =>
            _tabelaAtivaArquivo is null
                ? "Nenhuma tabela ativa"
                : $"Ativa: {Path.GetFileNameWithoutExtension(_tabelaAtivaArquivo)}";

        // ── Eventos ───────────────────────────────────────────────────────────
        public event EventHandler? CloseRequested;
        public event EventHandler<TabelaAtivadaEventArgs>? PrecosAtualizados;

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand FecharCommand { get; }
        public ICommand NovaTabelaCommand { get; }
        public ICommand SalvarNovaCommand { get; }
        public ICommand CancelarNovaCommand { get; }
        public ICommand SalvarTabelaCommand { get; }
        public ICommand AtivarTabelaCommand { get; }
        public ICommand DeletarTabelaCommand { get; }

        public PriceTableManagerViewModel(ObservableCollection<MaterialItem> itensMain)
        {
            _itensMain = itensMain;

            // Inicializa itensEdicao com wrappers vazios (preços zerados)
            foreach (var it in itensMain)
                ItensEdicao.Add(new ItemPrecoWrapper(it.Nome));

            FecharCommand      = new DelegateCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
            NovaTabelaCommand  = new DelegateCommand(IniciarNova);
            SalvarNovaCommand  = new DelegateCommand(async () => await SalvarNovaAsync(), PodeSalvarNova);
            CancelarNovaCommand = new DelegateCommand(CancelarNova);
            SalvarTabelaCommand = new DelegateCommand(async () => await SalvarTabelaSelecionadaAsync(),
                                                      () => TemTabelaSelecionada);
            AtivarTabelaCommand = new DelegateCommand(async () => await AtivarTabelaSelecionadaAsync(),
                                                      () => TemTabelaSelecionada);
            DeletarTabelaCommand = new DelegateCommand(async () => await DeletarTabelaSelecionadaAsync(),
                                                       () => TemTabelaSelecionada);
        }

        // ── Inicialização: carrega lista de tabelas ───────────────────────────
        public async Task InicializarAsync()
        {
            Directory.CreateDirectory(BaseDir);
            await RecarregarListaAsync();
        }

        private async Task RecarregarListaAsync()
        {
            Tabelas.Clear();
            var arquivos = Directory.GetFiles(BaseDir, "*.json")
                                    .OrderBy(f => f)
                                    .ToList();
            foreach (var arq in arquivos)
            {
                var nome = Path.GetFileNameWithoutExtension(arq);
                Tabelas.Add(new TabelaPrecoInfo { Nome = nome, Arquivo = arq });
            }

            // Marca qual está ativa
            AtualizarMarcacaoAtiva();
            await Task.CompletedTask;
        }

        private void AtualizarMarcacaoAtiva()
        {
            foreach (var t in Tabelas)
                t.IsAtiva = t.Arquivo == _tabelaAtivaArquivo;
        }

        // ── Carrega tabela selecionada no editor ──────────────────────────────
        private async Task CarregarEdicaoAsync(TabelaPrecoInfo info)
        {
            if (!File.Exists(info.Arquivo)) return;

            try
            {
                await using var fs = new FileStream(info.Arquivo, FileMode.Open,
                                                    FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var loaded = await JsonSerializer.DeserializeAsync(fs,
                    AppJsonContext.Default.TabelaPrecoJson);

                if (loaded?.Itens is null) return;

                var byName = loaded.Itens.ToDictionary(x => x.Nome ?? string.Empty, x => x.PrecoPorKg);
                foreach (var w in ItensEdicao)
                {
                    if (byName.TryGetValue(w.Nome, out var preco))
                        w.SetPreco(preco);
                    else
                        w.SetPreco(0m);
                }

                NovoNome = info.Nome;
                CriandoNova = false;
            }
            catch { }
        }

        // ── Nova tabela ───────────────────────────────────────────────────────
        private void IniciarNova()
        {
            TabelaSelecionada = null;
            NovoNome = string.Empty;
            foreach (var w in ItensEdicao)
                w.SetPreco(0m);
            CriandoNova = true;
        }

        private void CancelarNova()
        {
            CriandoNova = false;
            NovoNome = string.Empty;
        }

        private bool PodeSalvarNova() =>
            !string.IsNullOrWhiteSpace(NovoNome);

        private async Task SalvarNovaAsync()
        {
            if (!PodeSalvarNova()) return;

            // Confirma edição de todos os wrappers antes de salvar
            foreach (var w in ItensEdicao)
                w.ConfirmarEdicao();

            var nome = NovoNome.Trim();
            // Sanitiza nome para arquivo
            foreach (var c in Path.GetInvalidFileNameChars())
                nome = nome.Replace(c, '_');

            var arquivo = Path.Combine(BaseDir, $"{nome}.json");

            var payload = new TabelaPrecoJson
            {
                Nome = NovoNome.Trim(),
                Itens = ItensEdicao.Select(w => new LinhaPrecoTabela
                {
                    Nome = w.Nome,
                    PrecoPorKg = w.PrecoDecimal
                }).ToList()
            };

            Directory.CreateDirectory(BaseDir);
            await using (var fs = File.Create(arquivo))
                await JsonSerializer.SerializeAsync(fs, payload, AppJsonContext.Default.TabelaPrecoJson);

            CriandoNova = false;
            await RecarregarListaAsync();

            // Seleciona a tabela recém-criada
            TabelaSelecionada = Tabelas.FirstOrDefault(t => t.Arquivo == arquivo);
        }

        // ── Salvar tabela selecionada ─────────────────────────────────────────
        private async Task SalvarTabelaSelecionadaAsync()
        {
            if (_tabelaSelecionada is null) return;

            foreach (var w in ItensEdicao)
                w.ConfirmarEdicao();

            await SalvarTabelaAtualAsync(_tabelaSelecionada);
            TabelaSalvaRequested?.Invoke(this, _tabelaSelecionada.Nome);
        }

        public event EventHandler<string>? TabelaSalvaRequested;

        // ── Ativar tabela ─────────────────────────────────────────────────────
        private async Task AtivarTabelaSelecionadaAsync()
        {
            if (_tabelaSelecionada is null) return;

            // Confirma edição pendente
            foreach (var w in ItensEdicao)
                w.ConfirmarEdicao();

            // Salva alterações na tabela selecionada antes de ativar
            await SalvarTabelaAtualAsync(_tabelaSelecionada);

            // Aplica preços na tela principal
            await AplicarPrecosAsync(_tabelaSelecionada.Arquivo);

            TabelaAtivaArquivo = _tabelaSelecionada.Arquivo;
            AtualizarMarcacaoAtiva();

            PrecosAtualizados?.Invoke(this, new TabelaAtivadaEventArgs(_tabelaSelecionada.Nome));
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private async Task SalvarTabelaAtualAsync(TabelaPrecoInfo info)
        {
            var payload = new TabelaPrecoJson
            {
                Nome = info.Nome,
                Itens = ItensEdicao.Select(w => new LinhaPrecoTabela
                {
                    Nome = w.Nome,
                    PrecoPorKg = w.PrecoDecimal
                }).ToList()
            };

            await using var fs = File.Create(info.Arquivo);
            await JsonSerializer.SerializeAsync(fs, payload, AppJsonContext.Default.TabelaPrecoJson);
        }

        private async Task AplicarPrecosAsync(string arquivo)
        {
            if (!File.Exists(arquivo)) return;

            await using var fs = new FileStream(arquivo, FileMode.Open,
                                                FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var loaded = await JsonSerializer.DeserializeAsync(fs, AppJsonContext.Default.TabelaPrecoJson);
            if (loaded?.Itens is null) return;

            var byName = loaded.Itens.ToDictionary(x => x.Nome ?? string.Empty, x => x.PrecoPorKg);
            foreach (var item in _itensMain)
            {
                if (byName.TryGetValue(item.Nome ?? string.Empty, out var preco))
                    item.PrecoPorKg = preco;
            }
        }

        // ── Deletar tabela ────────────────────────────────────────────────────
        private async Task DeletarTabelaSelecionadaAsync()
        {
            if (_tabelaSelecionada is null) return;

            try { File.Delete(_tabelaSelecionada.Arquivo); } catch { }

            if (_tabelaAtivaArquivo == _tabelaSelecionada.Arquivo)
                TabelaAtivaArquivo = null;

            TabelaSelecionada = null;
            await RecarregarListaAsync();
        }

        // ── DTOs de persistência ──────────────────────────────────────────────
        public class TabelaPrecoJson
        {
            public string Nome { get; set; } = string.Empty;
            public List<LinhaPrecoTabela> Itens { get; set; } = new();
        }

        public class LinhaPrecoTabela
        {
            public string Nome { get; set; } = string.Empty;
            public decimal PrecoPorKg { get; set; }
        }
    }

    // ── Info de uma tabela na lista ───────────────────────────────────────────
    public class TabelaPrecoInfo : ViewModelBase
    {
        private bool _isAtiva;
        public string Nome { get; set; } = string.Empty;
        public string Arquivo { get; set; } = string.Empty;
        public bool IsAtiva
        {
            get => _isAtiva;
            set { if (value != _isAtiva) { _isAtiva = value; OnPropertyChanged(); } }
        }
    }

    // ── Wrapper de edição de preço por material ───────────────────────────────
    public class ItemPrecoWrapper : ViewModelBase
    {
        private string _precoTexto;
        private bool _editando;
        private decimal _precoDecimal;

        public string Nome { get; }

        public decimal PrecoDecimal => _precoDecimal;

        public string PrecoTexto
        {
            get => _precoTexto;
            set { if (value != _precoTexto) { _precoTexto = value; OnPropertyChanged(); } }
        }

        public ItemPrecoWrapper(string nome)
        {
            Nome = nome;
            _precoDecimal = 0m;
            _precoTexto = FormatPreco(0m);
        }

        public void SetPreco(decimal valor)
        {
            _editando = false;
            _precoDecimal = valor;
            _precoTexto = FormatPreco(valor);
            OnPropertyChanged(nameof(PrecoTexto));
        }

        public void IniciarEdicao()
        {
            _editando = true;
            PrecoTexto = string.Empty;
        }

        public void ConfirmarEdicao()
        {
            if (!_editando) return;
            _editando = false;

            var raw = PrecoTexto.Trim().Replace("R$", "").Replace(" ", "").Trim();

            if (raw.Contains(',') && raw.Contains('.'))
                raw = raw.Replace(".", "").Replace(",", ".");
            else
                raw = raw.Replace(",", ".");

            if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                parsed = _precoDecimal;

            _precoDecimal = parsed;
            _precoTexto = FormatPreco(parsed);
            OnPropertyChanged(nameof(PrecoTexto));
        }

        private static string FormatPreco(decimal v) =>
            v.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
    }

    public class TabelaAtivadaEventArgs : EventArgs
    {
        public string NomeTabela { get; }
        public TabelaAtivadaEventArgs(string nomeTabela) => NomeTabela = nomeTabela;
    }
}
