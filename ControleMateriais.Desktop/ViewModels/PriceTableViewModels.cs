using ControleMateriais.Models;
using System;
using System.Collections.ObjectModel;
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

        private DateTimeOffset? _competencia = DateTimeOffset.Now; // mês/ano atual como padrão
        public DateTimeOffset? Competencia
        {
            get => _competencia;
            set
            {
                if (value != _competencia)
                {
                    _competencia = value;
                    OnPropertyChanged();
                    (SalvarCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    // Carrega automaticamente os preços da competência (se existir o JSON)
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

            SalvarCommand = new DelegateCommand(async () => await SalvarAsync(), PodeSalvar);
            FecharCommand = new DelegateCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
        }

        private bool PodeSalvar()
            => Competencia.HasValue && Competencia.Value.Year >= 2000;

        private string GetBaseDir()
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "ControleMateriais", "Valores");

        private (string competenciaStr, string filePath) GetTargetFile()
        {
            var dt = new DateTime(Competencia!.Value.Year, Competencia.Value.Month, 1);
            var competenciaStr = dt.ToString("yyyy-MM");
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

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            await using var fs = File.Create(filePath);
            await JsonSerializer.SerializeAsync(fs, payload, options);
            await fs.FlushAsync();

            // Fecha após salvar (opcional)
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        // Carrega JSON (se existir) e APLICA nos mesmos MaterialItem (atualizando a tela principal)
        private async Task TryLoadFromJsonAsync()
        {
            if (!Competencia.HasValue) return;

            var (_, filePath) = GetTargetFile();
            if (!File.Exists(filePath))
                return;

            try
            {
                await using var fs = File.OpenRead(filePath);
                var loaded = await JsonSerializer.DeserializeAsync<ValoresMensais>(fs);
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
            public System.Collections.Generic.List<Linha> Itens { get; set; } = new();
        }

        public class Linha
        {
            public string Nome { get; set; } = string.Empty;
            public decimal PrecoPorKg { get; set; }
        }
    }

}
