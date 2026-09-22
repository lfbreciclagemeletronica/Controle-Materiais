using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using ControleMateriais.Data.Services;
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

            var splash = new SplashWindow();
            desktop.MainWindow = splash;

            splash.StartAndShow(() =>
            {
                // Só cria/migra o banco de dados local DEPOIS do pull do repositório
                // banco-de-dados (feito na SplashWindow), evitando conflito de clone/checkout
                // com um database.db local que ainda não existia.
                DatabaseInitializer.EnsureDatabase();

                var main = new MainWindow();
                desktop.MainWindow = main;
                main.Show();
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}