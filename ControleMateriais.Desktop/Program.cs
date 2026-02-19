using Avalonia;
using System;
using System.Globalization;

namespace ControleMateriais.Desktop;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
      
      var culture = CultureInfo.GetCultureInfo("pt-BR");
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
