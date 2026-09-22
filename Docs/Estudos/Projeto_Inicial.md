# Projeto Inicial — ControleMateriais.Desktop

Guia passo a passo para criar a estrutura inicial do projeto **ControleMateriais.Desktop** do zero, usando a CLI do .NET 10 e Avalonia.

Cada tópico explica o **porquê** do arquivo ou comando e o que o código faz. Use os links no final para aprofundar.

---

## 1. Pré-requisitos

O **SDK do .NET 10** é o conjunto de ferramentas que o computador precisa para criar, compilar e executar aplicativos .NET. Ele contém o compilador, a CLI (`dotnet`) e as bibliotecas base. Sem ele, os comandos a seguir não funcionam.

- **SDK do .NET 10** instalado.
- Verifique no terminal:

```bash
dotnet --version
```

Saída esperada:

```text
10.0.xxx
```

---

## 2. Criar a pasta raiz do projeto

Antes de criar qualquer arquivo, criamos uma pasta para agrupar tudo. `mkdir` cria uma pasta e `cd` entra nela. A partir daqui, todos os comandos serão executados dentro dessa pasta.

```bash
mkdir ControleMateriais
cd ControleMateriais
```

---

## 3. Criar a solução

Uma **solução** (`.sln` ou `.slnx`) é um contêiner que pode agrupar um ou mais projetos. Ela facilita compilar e gerenciar vários projetos ao mesmo tempo. No nosso caso, teremos apenas um projeto, mas a solução é um padrão importante do .NET.

```bash
dotnet new sln -n ControleMateriais
```

---

## 4. Criar o projeto Avalonia MVVM

Esse comando usa o **template** do Avalonia para gerar um projeto desktop com a arquitetura **MVVM** (Model-View-ViewModel). `Avalonia` é um framework multiplataforma para criar interfaces gráficas (UI) com XAML. `MVVM` separa a interface (View), a lógica de apresentação (ViewModel) e os dados (Model), facilitando manutenção e testes.

```bash
dotnet new avalonia.mvvm -n ControleMateriais.Desktop -o ControleMateriais.Desktop --framework net10.0
```

---

## 5. Adicionar o projeto à solução

Agora ligamos o projeto `.csproj` à solução. Isso permite que o `dotnet build` compile todos os projetos da solução de uma vez.

```bash
dotnet sln add ControleMateriais.Desktop/ControleMateriais.Desktop.csproj
```

---

## 6. Criar a estrutura de pastas

Organizar o código em pastas separadas torna o projeto mais fácil de entender e manter. No padrão **MVVM**:

- **Models**: classes que representam os dados do domínio (ex: `MaterialItem`).
- **Views**: telas em XAML (`MainWindow.axaml`, `HomeView.axaml`).
- **ViewModels**: lógica de apresentação que a View usa (ex: `MainWindowViewModel`).
- **Services**: classes com regras de negócio, acesso a API, GitHub, etc.
- **Converters**: classes que convertem valores para exibição na interface.
- **Serialization**: configuração de serialização JSON.
- **Styles**: recursos visuais compartilhados (cores, estilos de botões).
- **Assets**: imagens, ícones e outros arquivos estáticos.
- **Sync**: pasta auxiliar para sincronização de dados (pode ser usada futuramente).

Navegue até a pasta do projeto:

```bash
cd ControleMateriais.Desktop
```

Crie os diretórios:

```bash
mkdir Models
mkdir Views
mkdir ViewModels
mkdir Services
mkdir Converters
mkdir Serialization
mkdir Styles
mkdir Assets
mkdir Sync
```

---

## 7. Instalar os pacotes NuGet

**NuGet** é o gerenciador de pacotes do .NET. Cada `dotnet add package` baixa e adiciona uma biblioteca ao projeto. Aqui estão os principais pacotes e suas funções:

- **Avalonia**: biblioteca principal da interface gráfica.
- **Avalonia.Desktop**: suporte para executar o app no Windows, Linux e macOS.
- **Avalonia.Themes.Fluent**: tema visual moderno da Microsoft.
- **Avalonia.Fonts.Inter**: fonte Inter para uma tipografia limpa.
- **Avalonia.Controls.DataGrid**: controle de grade (tabela) para exibir dados.
- **Avalonia.Diagnostics**: ferramentas de debug durante o desenvolvimento.
- **CommunityToolkit.Mvvm**: ajuda a criar ViewModels com notificação de propriedades e comandos (`ObservableObject`, `[ObservableProperty]`, `RelayCommand`).
- **QuestPDF**: geração de PDFs.
- **itext7**: manipulação e leitura de PDFs.
- **ClosedXML**: leitura e escrita de arquivos Excel (`.xlsx`).
- **supabase-csharp**: cliente para comunicação com Supabase.
- Os demais pacotes (`IdentityModel`, `System.IO.Packaging`, `Tmds.DBus.Protocol`) são usados para corrigir dependências transitivas e funcionalidades específicas.

```bash
dotnet add package Avalonia --version 11.3.12
dotnet add package Avalonia.Desktop --version 11.3.12
dotnet add package Avalonia.Themes.Fluent --version 11.3.12
dotnet add package Avalonia.Fonts.Inter --version 11.3.12
dotnet add package Avalonia.Controls.DataGrid --version 11.3.12
dotnet add package Avalonia.Diagnostics --version 11.3.12
dotnet add package CommunityToolkit.Mvvm --version 8.2.1
dotnet add package QuestPDF --version 2024.12.3
dotnet add package itext7 --version 8.0.5
dotnet add package ClosedXML --version 0.104.1
dotnet add package supabase-csharp --version 0.16.2
dotnet add package Microsoft.IdentityModel.JsonWebTokens --version 7.5.1
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.5.1
dotnet add package System.IO.Packaging --version 10.0.7
dotnet add package Tmds.DBus.Protocol --version 0.21.3
```

---

## 8. Configurar o arquivo `.csproj`

O arquivo `.csproj` é o **projeto** do .NET. Ele diz ao compilador qual framework usar (`net10.0`), qual tipo de aplicação (`WinExe`), quais pacotes NuGet dependem e quais arquivos devem ser copiados para a saída (imagens, estilos, etc.).

- `<OutputType>WinExe</OutputType>`: gera um executável para Windows.
- `<TargetFramework>net10.0</TargetFramework>`: usa o .NET 10.
- `<Nullable>enable</Nullable>`: ativa avisos de null, ajudando a evitar erros.
- `<ApplicationManifest>`: define o manifesto do Windows.
- `<AvaloniaUseCompiledBindingsByDefault>`: ativa bindings compilados para melhor performance.
- `<AvaloniaResource>` e `<Content>`: incluem imagens e estilos no app.

Substitua o conteúdo de `ControleMateriais.Desktop/ControleMateriais.Desktop.csproj` por:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    <ApplicationIcon>Assets\icon.ico</ApplicationIcon>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <AvaloniaResource Include="Assets\**" />
    <Content Include="Assets\lfb-logo.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <TargetPath>Assets\lfb-logo.png</TargetPath>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <AvaloniaXaml Remove="Styles\Colors.axaml" />
  </ItemGroup>

  <ItemGroup>
    <AvaloniaResource Include="Styles\Colors.axaml" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.12" />
    <PackageReference Include="Avalonia.Controls.DataGrid" Version="11.3.12" />
    <PackageReference Include="Avalonia.Desktop" Version="11.3.12" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.12" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.12" />
    <PackageReference Include="itext7" Version="8.0.5" />
    <PackageReference Include="ClosedXML" Version="0.104.1" />
    <PackageReference Include="QuestPDF" Version="2024.12.3" />
    <PackageReference Include="Avalonia.Diagnostics" Version="11.3.12">
      <IncludeAssets Condition="'$(Configuration)' != 'Debug'">None</IncludeAssets>
      <PrivateAssets Condition="'$(Configuration)' != 'Debug'">All</PrivateAssets>
    </PackageReference>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.1" />
    <PackageReference Include="supabase-csharp" Version="0.16.2" />
    <PackageReference Include="Microsoft.IdentityModel.JsonWebTokens" Version="7.5.1" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.5.1" />
    <PackageReference Include="System.IO.Packaging" Version="10.0.7" />
    <PackageReference Include="Tmds.DBus.Protocol" Version="0.21.3" />
  </ItemGroup>
</Project>
```

---

## 9. Criar o `app.manifest`

O arquivo `app.manifest` é um documento XML usado pelo Windows para entender o comportamento do aplicativo. Ele declara o nome do assembly e quais versões do Windows o app suporta. No caso, o GUID `{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}` representa o Windows 10.

Crie o arquivo `app.manifest` na raiz do projeto:

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="ControleMateriais.Desktop.Desktop"/>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
    </application>
  </compatibility>
</assembly>
```

---

## 10. Criar `App.axaml` e `App.axaml.cs`

`App.axaml` é o arquivo XAML global da aplicação. Ele define:

- **DataTemplates**: como encontrar a View para cada ViewModel (`ViewLocator`).
- **Resources**: recursos compartilhados, como cores e pincéis.
- **Styles**: estilos globais, tema Fluent, fonte Inter, temas do DataGrid.

`App.axaml.cs` é o código C# que roda a lógica de inicialização. Quando a aplicação Avalonia termina de carregar (`OnFrameworkInitializationCompleted`), ele cria a janela principal (`MainWindow`) e define o ViewModel. Também remove a validação de anotações de dados para evitar comportamentos indesejados nos bindings.

`App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ControleMateriais.Desktop.App"
             xmlns:local="using:ControleMateriais.Desktop"
             RequestedThemeVariant="Dark">

    <Application.DataTemplates>
        <local:ViewLocator/>
    </Application.DataTemplates>

    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="Styles/Colors.axaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>

    <Application.Styles>
        <FluentTheme DensityStyle="Compact"/>
        <Style Selector=":is(Window)">
            <Setter Property="FontFamily" Value="avares://Avalonia.Fonts.Inter#Inter"/>
        </Style>
        <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"/>
        <StyleInclude Source="avares://ControleMateriais.Desktop/Styles/Controls.axaml"/>
    </Application.Styles>
</Application>
```

`App.axaml.cs`:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using ControleMateriais.Desktop.ViewModels;
using ControleMateriais.Desktop.Views;

namespace ControleMateriais.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
```

---

## 11. Criar `Program.cs`

`Program.cs` é o **ponto de entrada** do aplicativo. Ele é o primeiro arquivo executado quando o app inicia. Nele configuramos:

- **Licença do QuestPDF**: necessária para gerar PDFs em modo comunitário.
- **Cultura pt-BR**: define formato de data, número, moeda, etc.
- **Encoding UTF-8**: para exibir caracteres especiais corretamente no console.
- **AppBuilder**: configura o app Avalonia, detecta a plataforma e aplica a fonte Inter.

```csharp
using Avalonia;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;

namespace ControleMateriais.Desktop;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

---

## 12. Criar `ViewLocator.cs`

O `ViewLocator` é uma classe que implementa `IDataTemplate`. Ele funciona como um "tradutor" entre ViewModel e View. Quando o Avalonia precisa exibir um ViewModel, ele pergunta ao `ViewLocator`: "qual é a View correspondente?". O código substitui `"ViewModel"` por `"View"` no nome da classe e tenta criar uma instância dela. Se não encontrar, exibe a mensagem `Not Found`.

```csharp
using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ControleMateriais.Desktop.ViewModels;

namespace ControleMateriais.Desktop;

[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
```

---

## 13. Criar `ViewModelBase.cs`

`ViewModelBase` é a classe base de todos os ViewModels. Ela herda de `ObservableObject` do `CommunityToolkit.Mvvm`, que já possui a implementação de `INotifyPropertyChanged`. Isso significa que, quando uma propriedade muda, a interface gráfica é notificada automaticamente.

`ViewModels/ViewModelBase.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace ControleMateriais.Desktop.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
}
```

---

## 14. Criar `MainWindow` inicial

A `MainWindow` é a **janela principal** do aplicativo. O arquivo `.axaml` descreve a interface visual: dimensões, título, ícone e os controles dentro dela. O arquivo `.axaml.cs` (code-behind) é o código C# associado a essa View, onde podemos reagir a eventos da janela. O `ViewModel` (`MainWindowViewModel`) contém os dados que a View exibe. A expressão `{Binding Greeting}` conecta o texto da tela à propriedade `Greeting` do ViewModel.

`Views/MainWindow.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ControleMateriais.Desktop.ViewModels"
        x:Class="ControleMateriais.Desktop.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Width="900" Height="680"
        Title="LFB Reciclagem Eletrônica"
        Icon="avares://ControleMateriais.Desktop/Assets/icon.ico">

    <Grid Background="{DynamicResource Brush.Surface}">
        <TextBlock Text="{Binding Greeting}"
                   Foreground="{DynamicResource Brush.TextPrimary}"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center"
                   FontSize="24"/>
    </Grid>
</Window>
```

`Views/MainWindow.axaml.cs`:

```csharp
using Avalonia.Controls;

namespace ControleMateriais.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

`ViewModels/MainWindowViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace ControleMateriais.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Bem-vindo ao Controle de Materiais!";
}
```

---

## 15. Criar `HomeView` inicial

`HomeView` é um **UserControl**, ou seja, um pedaço reutilizável de interface. Diferente de uma `Window`, um `UserControl` não pode ser uma janela sozinho, mas pode ser colocado dentro de uma `Window`. Aqui criamos uma tela inicial simples com um título e uma descrição.

`Views/HomeView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ControleMateriais.Desktop.ViewModels"
             x:Class="ControleMateriais.Desktop.Views.HomeView"
             x:DataType="vm:MainWindowViewModel">

    <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Spacing="16">
        <TextBlock Text="ControleMateriais.Desktop"
                   FontSize="28"
                   FontWeight="Bold"
                   Foreground="{DynamicResource Brush.TextPrimary}"/>
        <TextBlock Text="Aplicativo de controle de pesagens, recibos e estoque."
                   Foreground="{DynamicResource Brush.TextMuted}"/>
    </StackPanel>
</UserControl>
```

`Views/HomeView.axaml.cs`:

```csharp
using Avalonia.Controls;

namespace ControleMateriais.Desktop.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }
}
```

---

## 16. Criar os estilos

**XAML** permite separar a aparência do aplicativo em arquivos de recursos. `Colors.axaml` define uma paleta de cores (primária, superfície, texto, etc.) como recursos dinâmicos. `Controls.axaml` define o estilo dos controles (nesse exemplo, o botão). Usar estilos centralizados facilita mudar a aparência de todo o app de um único lugar.

`Styles/Colors.axaml`:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <Color x:Key="Brand.Primary">#3B82F6</Color>
  <Color x:Key="Brand.PrimaryHover">#2563EB</Color>
  <Color x:Key="Brand.Border">#2F2F2F</Color>
  <Color x:Key="Brand.Surface">#141414</Color>
  <Color x:Key="Brand.SurfaceAlt">#1B1B1B</Color>
  <Color x:Key="Brand.TextPrimary">#EDEDED</Color>
  <Color x:Key="Brand.TextMuted">#B5B5B5</Color>

  <SolidColorBrush x:Key="Brush.Primary" Color="{DynamicResource Brand.Primary}"/>
  <SolidColorBrush x:Key="Brush.PrimaryHover" Color="{DynamicResource Brand.PrimaryHover}"/>
  <SolidColorBrush x:Key="Brush.Border" Color="{DynamicResource Brand.Border}"/>
  <SolidColorBrush x:Key="Brush.Surface" Color="{DynamicResource Brand.Surface}"/>
  <SolidColorBrush x:Key="Brush.SurfaceAlt" Color="{DynamicResource Brand.SurfaceAlt}"/>
  <SolidColorBrush x:Key="Brush.TextPrimary" Color="{DynamicResource Brand.TextPrimary}"/>
  <SolidColorBrush x:Key="Brush.TextMuted" Color="{DynamicResource Brand.TextMuted}"/>
</ResourceDictionary>
```

`Styles/Controls.axaml`:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Design.PreviewWith>
    <Border Padding="20">
      <StackPanel Spacing="10">
        <Button Content="Botão padrão"/>
        <Button Classes="primary" Content="Botão primário"/>
      </StackPanel>
    </Border>
  </Design.PreviewWith>

  <Style Selector="Button">
    <Setter Property="Background" Value="{DynamicResource Brush.SurfaceAlt}"/>
    <Setter Property="Foreground" Value="{DynamicResource Brush.TextPrimary}"/>
    <Setter Property="BorderBrush" Value="{DynamicResource Brush.Border}"/>
    <Setter Property="Padding" Value="12,6"/>
    <Setter Property="CornerRadius" Value="6"/>
  </Style>

  <Style Selector="Button.primary">
    <Setter Property="Background" Value="{DynamicResource Brush.Primary}"/>
    <Setter Property="Foreground" Value="White"/>
  </Style>
</ResourceDictionary>
```

---

## 17. Criar a model `MaterialItem`

A **Model** representa um dado do mundo real. `MaterialItem` guarda o nome do material, o peso e o preço por quilo. Ela implementa `INotifyPropertyChanged`, um padrão do C# que avisa a interface quando uma propriedade muda. Quando `PesoAtual` ou `PrecoPorKg` mudam, a propriedade calculada `Total` também é atualizada.

`Models/MaterialItem.cs`:

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ControleMateriais.Models
{
    public class MaterialItem : INotifyPropertyChanged
    {
        private string _nome = "";
        private decimal _pesoAtual;
        private decimal _precoPorKg;

        public string Nome
        {
            get => _nome;
            set { if (value != _nome) { _nome = value; OnPropertyChanged(); } }
        }

        public decimal PesoAtual
        {
            get => _pesoAtual;
            set { if (value != _pesoAtual) { _pesoAtual = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); } }
        }

        public decimal PrecoPorKg
        {
            get => _precoPorKg;
            set { if (value != _precoPorKg) { _precoPorKg = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); } }
        }

        public decimal Total => PesoAtual * PrecoPorKg;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
```

---

## 18. Criar `AppJsonContext.cs`

`AppJsonContext` é um **JsonSerializerContext** gerado por source generator. Em vez de usar reflexão em tempo de execução para serializar JSON, o .NET gera o código de serialização no momento da compilação. Isso deixa o app mais rápido e compatível com publicação nativa. `JsonSourceGenerationOptions` configura a saída indentada e o tratamento de valores nulos.

`Serialization/AppJsonContext.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using ControleMateriais.Models;

namespace ControleMateriais.Desktop.Serialization
{
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(MaterialItem))]
    [JsonSerializable(typeof(List<MaterialItem>))]

    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}
```

---

## 19. Adicionar assets iniciais

**Assets** são arquivos estáticos usados pelo app: ícones, logos, imagens, sons, etc. Eles não são compilados como código, mas são embutidos como recursos Avalonia (`AvaloniaResource`) ou copiados para a pasta de saída (`Content`). O arquivo `icon.ico` é usado na barra de título e na janela, enquanto `lfb-logo.png` pode aparecer em PDFs e telas.

Copie os arquivos de imagem para a pasta `Assets/`:

- `Assets/icon.ico`
- `Assets/avalonia-logo.ico`
- `Assets/lfb-logo.png`

> **Nota:** Arquivos de mídia não são criados por CLI. Copie os arquivos reais do projeto original para a pasta `Assets/`.

---

## 20. Restaurar e compilar

Antes de compilar, o .NET precisa **restaurar** os pacotes NuGet, ou seja, baixar as bibliotecas necessárias. Depois, `dotnet build` compila o código e verifica se há erros. Se tudo estiver certo, você verá `Build succeeded`.

```bash
cd ..
dotnet restore
dotnet build
```

Se tudo estiver correto, a saída será similar a:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## 21. Executar o projeto

`dotnet run` compila (se necessário) e inicia o aplicativo. `--project` indica qual projeto executar. Se a janela principal abrir e mostrar a mensagem de boas-vindas, a estrutura inicial está funcionando.

```bash
dotnet run --project ControleMateriais.Desktop
```

---

## Estrutura final esperada

```text
ControleMateriais/
├── ControleMateriais.sln
├── ControleMateriais.Desktop/
│   ├── ControleMateriais.Desktop.csproj
│   ├── app.manifest
│   ├── App.axaml
│   ├── App.axaml.cs
│   ├── Program.cs
│   ├── ViewLocator.cs
│   ├── Assets/
│   ├── Converters/
│   ├── Models/
│   ├── Serialization/
│   ├── Services/
│   ├── Styles/
│   ├── Sync/
│   ├── ViewModels/
│   └── Views/
```

---

## Próximos passos

A partir dessa estrutura inicial, você pode implementar:

- ViewModels das páginas: `PesagensViewModel`, `EstoqueViewModel`, `VendaViewModel`, etc.
- Views correspondentes: `PesagensView`, `EstoqueView`, `VendaView`, etc.
- Serviços: `GitHubService`, `ReciboParserService`, `RelatorioExcelService`.
- Converters: `BoolToBrushConverter`, `BooleanNegationConverter`, etc.
- Tela de configuração do GitHub: `GitHubConfigDialog`.

---

## Observação sobre versões

As versões dos pacotes usadas neste guia refletem a configuração original do projeto. Sempre que possível, mantenha os `PackageReference` com as versões fixadas para evitar quebras de compatibilidade.

---

## Links para aprofundar

Aqui estão alguns recursos para estudar as tecnologias usadas:

- **.NET 10**
  - [Documentação oficial do .NET](https://learn.microsoft.com/pt-br/dotnet/)
  - [Novidades do .NET 10](https://learn.microsoft.com/pt-br/dotnet/core/whats-new/dotnet-10)

- **C#**
  - [Tour do C#](https://learn.microsoft.com/pt-br/dotnet/csharp/tour-of-csharp/)
  - [Referência da linguagem C#](https://learn.microsoft.com/pt-br/dotnet/csharp/language-reference/)

- **Avalonia UI**
  - [Site oficial](https://avaloniaui.net/)
  - [Documentação do Avalonia](https://docs.avaloniaui.net/)
  - [Criar um novo aplicativo Avalonia](https://docs.avaloniaui.net/docs/get-started/test-drive/)

- **XAML**
  - [Visão geral do XAML](https://learn.microsoft.com/pt-br/dotnet/desktop/wpf/xaml/?view=netdesktop-8.0)
  - [Avalonia XAML](https://docs.avaloniaui.net/docs/basics/user-interface/introduction-to-xaml)

- **MVVM (Model-View-ViewModel)**
  - [MVVM no .NET](https://learn.microsoft.com/pt-br/dotnet/architecture/mvvm/)
  - [CommunityToolkit.Mvvm](https://learn.microsoft.com/pt-br/dotnet/communitytoolkit/mvvm/)

- **NuGet**
  - [O que é o NuGet?](https://learn.microsoft.com/pt-br/nuget/what-is-nuget)
  - [Comandos da CLI do NuGet](https://learn.microsoft.com/pt-br/nuget/reference/nuget-cli-cli-reference)

- **QuestPDF**
  - [Documentação do QuestPDF](https://www.questpdf.com/)

- **itext7**
  - [Documentação do iText 7](https://itextpdf.com/products/itext-7)

- **ClosedXML**
  - [Repositório do ClosedXML no GitHub](https://github.com/ClosedXML/ClosedXML)

- **Supabase C#**
  - [supabase-csharp no GitHub](https://github.com/supabase-community/supabase-csharp)

- **Source Generators e JSON no .NET**
  - [Serialização JSON com source generators](https://learn.microsoft.com/pt-br/dotnet/standard/serialization/system-text-json/source-generation)
  - [System.Text.Json](https://learn.microsoft.com/pt-br/dotnet/standard/serialization/system-text-json/overview)

- **Controle de versão com GitHub (usado no projeto original)**
  - [Documentação do GitHub](https://docs.github.com/pt)
  - [Git - guia básico](https://git-scm.com/doc)
