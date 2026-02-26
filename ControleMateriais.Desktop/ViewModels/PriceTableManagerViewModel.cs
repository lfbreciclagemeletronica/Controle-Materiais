using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ControleMateriais.Desktop.Serialization;
using ControleMateriais.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;
using System.Text.RegularExpressions;
using IContainer = QuestPDF.Infrastructure.IContainer;

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
                    (ExportarTabelaPdfCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (ImportarDePdfCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    if (value != null)
                        CarregarEdicaoAsync(value).ConfigureAwait(false);
                }
            }
        }

        public bool TemTabelaSelecionada => _tabelaSelecionada != null;

        // ── Loading de importação ─────────────────────────────────────────────
        private bool _isImportando;
        public bool IsImportando
        {
            get => _isImportando;
            set { if (value != _isImportando) { _isImportando = value; OnPropertyChanged(); } }
        }

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
        public ICommand ExportarTabelaPdfCommand { get; }
        public ICommand ImportarDePdfCommand { get; }

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
            ExportarTabelaPdfCommand = new DelegateCommand(async () => await ExportarTabelaPdfAsync(),
                                                          () => TemTabelaSelecionada);
            ImportarDePdfCommand = new DelegateCommand(async () => await ImportarDePdfAsync());
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

        // ── Importar preços de um PDF ─────────────────────────────────────────
        private async Task ImportarDePdfAsync()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Selecionar PDF com lista de preços",
                Filters = { new FileDialogFilter { Name = "PDF", Extensions = { "pdf" } } },
                AllowMultiple = false
            };

            var topLevel = (Avalonia.Application.Current?.ApplicationLifetime as
                            IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var files = await ofd.ShowAsync(topLevel!);
            if (files is null || files.Length == 0) return;

            var path = files[0];
            if (!File.Exists(path)) return;

            IsImportando = true;
            try
            {
                var precos = await Task.Run(() => ExtrairPrecosDoPdf(path));

                if (precos.Count == 0)
                {
                    ImportToastRequested?.Invoke(this, ("Nenhum item reconhecido no PDF.", true, false));
                    return;
                }

                int encontrados = 0;
                foreach (var w in ItensEdicao)
                {
                    if (precos.TryGetValue(w.Nome, out var valor))
                    {
                        w.SetPreco(valor);
                        encontrados++;
                    }
                }

                var naoEncontrados = ItensEdicao
                    .Where(w => !precos.ContainsKey(w.Nome))
                    .Select(w => w.Nome)
                    .ToList();

                bool sucesso = encontrados > 0;
                var msg = sucesso
                    ? $"{encontrados} de {ItensEdicao.Count} importados. Não encontrados: {(naoEncontrados.Count == 0 ? "nenhum" : string.Join(", ", naoEncontrados))}"
                    : "Nenhum item reconhecido. Verifique se o PDF é uma lista de preços LFB.";
                ImportToastRequested?.Invoke(this, (msg, !sucesso, sucesso));
            }
            finally
            {
                IsImportando = false;
            }
        }

        public event EventHandler<(string Mensagem, bool IsErro, bool IsSuccess)>? ImportToastRequested;

        private static Dictionary<string, decimal> ExtrairPrecosDoPdf(string filePath)
        {
            var resultado = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var invariant = CultureInfo.InvariantCulture;

            // Extrai todas as linhas usando LocationTextExtractionStrategy
            // que respeita a ordem espacial e produz linhas mais fiéis ao visual
            var linhas = new List<string>();
            using (var reader = new PdfReader(filePath))
            using (var doc = new PdfDocument(reader))
            {
                for (int p = 1; p <= doc.GetNumberOfPages(); p++)
                {
                    var strategy = new LocationTextExtractionStrategy();
                    var text = PdfTextExtractor.GetTextFromPage(doc.GetPage(p), strategy);
                    foreach (var l in text.Split('\n'))
                    {
                        var trimmed = l.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            linhas.Add(trimmed);
                    }
                }
            }

            // Regex para qualquer valor monetário na linha
            // (?<!\d) impede capturar "5,00" dentro de "65,00"
            // (?!\d) impede capturar "65,0" dentro de "65,00" (extra dígito após)
            var reValor = new Regex(
                @"R\$\s*([\d]{1,3}(?:\.\d{3})*,\d{2})(?!\d)|" +   // R$ 1.300,00
                @"R\$\s*(\d+,\d{2})(?!\d)|" +                       // R$ 130,00
                @"(?<!\d)([\d]{1,3}(?:\.\d{3})*,\d{2})(?!\d)|" +   // 1.300,00 isolado
                @"(?<!\d)(\d+,\d{2})(?!\d)",                         // 130,00 isolado
                RegexOptions.Compiled);

            // Corrige artefato do iText: "6 5,00" → "65,00", "1 3,00" → "13,00"
            // Ocorre quando o PDF tem kerning especial entre dígitos
            var reEspacoEntreDigitos = new Regex(@"(\d) (\d)", RegexOptions.Compiled);
            for (int idx = 0; idx < linhas.Count; idx++)
            {
                // Aplica repetidamente até não haver mais espaços entre dígitos
                string orig;
                do
                {
                    orig = linhas[idx];
                    linhas[idx] = reEspacoEntreDigitos.Replace(orig, "$1$2");
                } while (linhas[idx] != orig);
            }

            // Pré-normaliza as linhas uma única vez
            var linhasNorm = linhas.Select(NormalizarTexto).ToList();

            // Linhas já consumidas (índice) para evitar que nomes curtos roubem
            // valores que pertencem a nomes mais longos
            var linhasConsumidas = new HashSet<int>();

            // Aliases: variações de nome que podem aparecer no PDF
            // Baseados no conteúdo real extraído pelo iText7
            var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["HD sem Placa/Sucateado"]                  = new[] { "HD sem Placa Sucateado", "HD sem Placa", "HD Sucateado" },
                ["Desmanche Eletrônicos Consultar Itens"]   = new[] { "Desmanche Eletronicos Consultar itens", "Desmanche Eletrônicos Consultar", "Desmanche Eletronicos" },
                ["Celular Smart sem Bateria Botão e Flip"]  = new[] { "Celular Sem bateria Botao e Flip", "Celular Sem bateria Botão e Flip", "Sem bateria Botao e Flip" },
                ["Celular Replicas com e sem Baterias"]     = new[] { "Celular Replicas com e sem Bateria", "Replicas com e sem Baterias" },
                ["Memória Prata"]                           = new[] { "Memorias Prata", "Memórias Prata", "Memoria Prateada" },
                ["Memórias Douradas"]                       = new[] { "Memorias Douradas", "Memoria Dourada" },
            };

            // Ordena por comprimento DECRESCENTE: nomes mais longos têm prioridade
            // "Placa Leve Especial com Ponta" antes de "Placa Leve" evita falso match
            var nomesOrdenados = ItemCatalog.OrderedItems
                .OrderByDescending(n => n.Length)
                .ToList();

            foreach (var nome in nomesOrdenados)
            {
                // Monta lista de termos a tentar: nome principal + aliases
                var termos = new List<string> { NormalizarTexto(nome) };
                if (aliases.TryGetValue(nome, out var alts))
                    termos.AddRange(alts.Select(NormalizarTexto));

                for (int i = 0; i < linhas.Count; i++)
                {
                    var nomeNorm = termos[0];
                    // Tenta cada termo (principal e aliases)
                    bool casou = termos.Any(t =>
                        linhasNorm[i].Contains(t, StringComparison.OrdinalIgnoreCase));
                    if (!casou) continue;

                    // Linha encontrada – tenta extrair valor aqui ou nas próximas 2
                    decimal? val = null;
                    int linhaValor = -1;

                    if (!linhasConsumidas.Contains(i))
                    {
                        val = ExtrairPrimeirValor(linhas[i], reValor, invariant);
                        if (val.HasValue) linhaValor = i;
                    }

                    for (int j = i + 1; val is null && j <= i + 2 && j < linhas.Count; j++)
                    {
                        if (linhasConsumidas.Contains(j)) break;

                        // Para se encontrar o nome de outro item do catálogo
                        bool outraNome = nomesOrdenados.Any(n => n != nome &&
                            linhasNorm[j].Contains(NormalizarTexto(n),
                            StringComparison.OrdinalIgnoreCase));
                        if (outraNome) break;

                        val = ExtrairPrimeirValor(linhas[j], reValor, invariant);
                        if (val.HasValue) linhaValor = j;
                    }

                    if (val.HasValue)
                    {
                        resultado[nome] = val.Value;
                        linhasConsumidas.Add(i);
                        if (linhaValor >= 0 && linhaValor != i)
                            linhasConsumidas.Add(linhaValor);
                        break;
                    }
                }
            }

            return resultado;
        }

        private static string NormalizarTexto(string texto)
        {
            // Remove acentos, normaliza espaços, lowercase
            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalizado.Length);
            foreach (var c in normalizado)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().ToLowerInvariant().Trim();
        }

        private static decimal? ExtrairPrimeirValor(string linha, Regex reValor, CultureInfo invariant)
        {
            var m = reValor.Match(linha);
            if (!m.Success) return null;

            // Pega o primeiro grupo capturado que não seja vazio
            for (int g = 1; g <= m.Groups.Count - 1; g++)
            {
                var raw = m.Groups[g].Value.Trim();
                if (string.IsNullOrEmpty(raw)) continue;

                // Normaliza: remove separador de milhar, troca vírgula por ponto
                raw = raw.Replace(".", "").Replace(",", ".");

                if (decimal.TryParse(raw, NumberStyles.Any, invariant, out var val))
                    return val;
            }

            return null;
        }

        // ── Exportar PDF da tabela de preços ─────────────────────────────────
        private async Task ExportarTabelaPdfAsync()
        {
            if (_tabelaSelecionada is null) return;

            foreach (var w in ItensEdicao)
                w.ConfirmarEdicao();

            var byName = ItensEdicao.ToDictionary(w => w.Nome, w => w.PrecoDecimal);

            var sfd = new SaveFileDialog
            {
                Title = "Salvar lista de preços em PDF",
                Filters = { new FileDialogFilter() { Name = "PDF", Extensions = { "pdf" } } },
                InitialFileName = $"Lista_Precos_{_tabelaSelecionada.Nome}.pdf"
            };

            var topLevel = (Avalonia.Application.Current?.ApplicationLifetime as
                            IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var path = await sfd.ShowAsync(topLevel!);
            if (string.IsNullOrWhiteSpace(path)) return;

            GerarListaPrecosPdf(path, byName, _tabelaSelecionada.Nome);
            TabelaSalvaRequested?.Invoke(this, $"PDF exportado: {System.IO.Path.GetFileName(path)}");
        }

        private static void GerarListaPrecosPdf(string filePath, Dictionary<string, decimal> precos, string nomeTabela)
        {
            var ptBR = CultureInfo.GetCultureInfo("pt-BR");
            var borderColor = Colors.Grey.Darken2;
            var headerBg    = Colors.Grey.Lighten2;
            var sectionBg   = Colors.Grey.Lighten3;

            static IContainer SectionCell(IContainer c) =>
                c.Background(Colors.Grey.Lighten2)
                 .PaddingVertical(3).PaddingHorizontal(6);

            static IContainer ItemCell(IContainer c) =>
                c.BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2)
                 .PaddingVertical(2).PaddingHorizontal(6);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(1.2f, Unit.Centimetre);
                    page.MarginBottom(1.2f, Unit.Centimetre);
                    page.MarginHorizontal(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        col.Spacing(0);

                        // Cabeçalho
                        col.Item().Border(0.5f).BorderColor(borderColor)
                           .Background(Colors.White)
                           .Padding(6)
                           .Row(row =>
                           {
                               row.RelativeItem().AlignLeft().AlignMiddle()
                                  .Text("LISTA DE PREÇOS LFB RECICLAGEM ELETRÔNICA")
                                  .Bold().FontSize(10);
                               row.ConstantItem(60).AlignRight().AlignMiddle()
                                  .Text(nomeTabela).FontSize(7).Italic();
                           });

                        col.Item().Height(4);

                        // Tabela com seções
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                            });

                            foreach (var section in ItemCatalog.Sections)
                            {
                                // Linha de seção (header cinza, colspan via spanning)
                                table.Cell().ColumnSpan(2).Element(SectionCell)
                                     .AlignCenter()
                                     .Text(section.Titulo).Bold().FontSize(8);

                                foreach (var nome in section.Itens)
                                {
                                    precos.TryGetValue(nome, out var valor);

                                    table.Cell().Element(ItemCell)
                                         .Text(nome).FontSize(8);

                                    table.Cell().Element(ItemCell).AlignRight()
                                         .Text(valor > 0
                                             ? valor.ToString("C", ptBR)
                                             : string.Empty)
                                         .FontSize(8);
                                }
                            }
                        });
                    });
                });
            }).GeneratePdf(filePath);
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
