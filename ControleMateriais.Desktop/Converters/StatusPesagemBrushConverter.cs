using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ControleMateriais.Desktop.Converters;

public class StatusPesagemBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string ?? string.Empty;
        return status.ToLowerInvariant() switch
        {
            "pendente"  => new SolidColorBrush(Color.Parse("#FF9800")),
            "concluido" => new SolidColorBrush(Color.Parse("#4CAF50")),
            "falhou"    => new SolidColorBrush(Color.Parse("#F44336")),
            _           => new SolidColorBrush(Color.Parse("#AAAAAA")),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
