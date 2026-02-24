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

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
