using ControleMateriais.Desktop.Serialization;
using ControleMateriais.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;


namespace ControleMateriais.Desktop.ViewModels
{

    public class PriceTableViewModel : ViewModelBase
    {
        public ObservableCollection<MaterialItem> Precos { get; }
        public ObservableCollection<ItemPrecoWrapper> PrecosEditaveis { get; }

        private bool _salvoComSucesso;
        public bool SalvoComSucesso
        {
            get => _salvoComSucesso;
            private set { if (value != _salvoComSucesso) { _salvoComSucesso = value; OnPropertyChanged(); } }
        }

        public ICommand RetornarCommand { get; }

        public event EventHandler? ValoresAtualizados;

        private string _competenciaMes = DateTime.Now.Month.ToString("D2");
        public string CompetenciaMes
        {
            get => _competenciaMes;
            set
            {
                if (value != _competenciaMes)
                {
                    _competenciaMes = value;
                    OnPropertyChanged();
                    (SalvarCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    _ = TryLoadFromJsonAsync();
                }
            }
        }

        private string _competenciaAno = DateTime.Now.Year.ToString();
        public string CompetenciaAno
        {
            get => _competenciaAno;
            set
            {
                if (value != _competenciaAno)
                {
                    _competenciaAno = value;
                    OnPropertyChanged();
                    (SalvarCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    _ = TryLoadFromJsonAsync();
                }
            }
        }

        public ICommand SalvarCommand { get; }
        public ICommand FecharCommand { get; }

        public event EventHandler? CloseRequested;

        public PriceTableViewModel(ObservableCollection<MaterialItem> itens)
        {
            // Usamos os MESMOS objetos MaterialItem da tela principal
            Precos = itens;
            PrecosEditaveis = new ObservableCollection<ItemPrecoWrapper>(
                itens.Select(i => new ItemPrecoWrapper(i)));

            SalvarCommand = new DelegateCommand(async () => await SalvarAsync(), PodeSalvar);
            RetornarCommand = new DelegateCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
        }

        public void ResetarAposAbrir()
        {
            SalvoComSucesso = false; // botão volta a mostrar "Salvar (JSON)"
            (SalvarCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            foreach (var w in PrecosEditaveis)
                w.AtualizarExibicao();
        }

        private bool PodeSalvar()
        {
            if (!int.TryParse(CompetenciaMes, out var m) || m < 1 || m > 12) return false;
            if (!int.TryParse(CompetenciaAno, out var a) || a < 2000) return false;
            return true;
        }

        private string GetBaseDir()
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "Downloads", "Controle-Materiais-Registros");

        private (string competenciaStr, string filePath) GetTargetFile()
        {
            int.TryParse(CompetenciaMes, out var m);
            int.TryParse(CompetenciaAno, out var a);
            var competenciaStr = $"{a:D4}-{m:D2}";
            var path = Path.Combine(GetBaseDir(), $"valores_{competenciaStr}.json");
            return (competenciaStr, path);
        }

        private async Task SalvarAsync()
        {
            if (!PodeSalvar()) return;

            Directory.CreateDirectory(GetBaseDir());

            var (competenciaStr, filePath) = GetTargetFile();

            var payload = new ValoresMensais
            {
                Competencia = competenciaStr,
                Itens = Precos.Select(p => new Linha { Nome = p.Nome, PrecoPorKg = p.PrecoPorKg }).ToList()
            };

            await using (var fs = File.Create(filePath))
                await System.Text.Json.JsonSerializer.SerializeAsync(
                    fs,
                    payload,
                    AppJsonContext.Default.ValoresMensais
                );

            //Notificar
            SalvoComSucesso = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
            ValoresAtualizados?.Invoke(this, EventArgs.Empty);
        }

        // Carrega JSON (se existir) e APLICA nos mesmos MaterialItem (atualizando a tela principal)
        private async Task TryLoadFromJsonAsync()
        {
            if (!PodeSalvar()) return;

            var (_, filePath) = GetTargetFile();
            if (!File.Exists(filePath))
                return;

            try
            {
                await using var fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                var loaded = await System.Text.Json.JsonSerializer.DeserializeAsync(
                    fs,
                    AppJsonContext.Default.ValoresMensais);
                if (loaded?.Itens is null) return;

                // aplica por nome (case-insensitive)
                var byName = loaded.Itens.ToDictionary(x => x.Nome ?? string.Empty,
                                                       x => x.PrecoPorKg);

                foreach (var item in Precos)
                {
                    if (item.Nome is null) continue;
                    if (byName.TryGetValue(item.Nome, out var preco))
                    {
                        // Settar aqui dispara PropertyChanged e atualiza a tela principal
                        item.PrecoPorKg = preco;
                    }
                }

                foreach (var w in PrecosEditaveis)
                    w.AtualizarExibicao();
            }
            catch
            {
                // você pode logar/avisar; por simplicidade, ignoramos erros de leitura/conversão
            }
        }

        // DTO de persistência (sem Peso/Total)
        public class ValoresMensais
        {
            public string Competencia { get; set; } = string.Empty; // "yyyy-MM"
            public List<Linha> Itens { get; set; } = new();
        }

        public class Linha
        {
            public string Nome { get; set; } = string.Empty;
            public decimal PrecoPorKg { get; set; }
        }
    }

    // Wrapper para edição de preço com suporte a vírgula/ponto e formatação em reais
    public class ItemPrecoWrapper : ViewModelBase
    {
        private readonly MaterialItem _item;
        private string _precoTexto;
        private bool _editando;

        public string Nome => _item.Nome;

        public string PrecoTexto
        {
            get => _precoTexto;
            set
            {
                if (value != _precoTexto)
                {
                    _precoTexto = value;
                    OnPropertyChanged();
                }
            }
        }

        public ItemPrecoWrapper(MaterialItem item)
        {
            _item = item;
            _precoTexto = item.PrecoPorKg.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        }

        // Chamado quando o campo recebe foco: limpa para digitação
        public void IniciarEdicao()
        {
            _editando = true;
            PrecoTexto = string.Empty;
        }

        // Chamado ao pressionar Enter ou perder foco: converte e formata
        public void ConfirmarEdicao()
        {
            if (!_editando) return;
            _editando = false;

            var raw = PrecoTexto.Trim()
                                .Replace("R$", "")
                                .Replace(" ", "")
                                .Trim();

            // suporta tanto ponto quanto vírgula como separador decimal
            // se houver os dois, trata ponto como milhar e vírgula como decimal (ex: 1.234,56)
            // caso contrário, aceita ambos como separador decimal
            decimal parsed = 0m;
            if (raw.Contains(',') && raw.Contains('.'))
            {
                raw = raw.Replace(".", "").Replace(",", ".");
            }
            else
            {
                raw = raw.Replace(",", ".");
            }

            if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                parsed = _item.PrecoPorKg;

            _item.PrecoPorKg = parsed;
            PrecoTexto = parsed.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        }

        // Atualiza a exibição quando o item é alterado externamente (ex: carregamento do JSON)
        public void AtualizarExibicao()
        {
            _editando = false;
            PrecoTexto = _item.PrecoPorKg.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        }
    }

}
