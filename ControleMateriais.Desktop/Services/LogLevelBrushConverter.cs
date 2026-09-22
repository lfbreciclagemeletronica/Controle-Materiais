using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ControleMateriais.Desktop.Services;

/// <summary>Converte um LogLevel na cor usada para exibir a linha na janela de Logs.</summary>
public sealed class LogLevelBrushConverter : IValueConverter
{
    public static readonly LogLevelBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            LogLevel.Erro  => Brushes.OrangeRed,
            LogLevel.Aviso => Brushes.Gold,
            _              => Brushes.LightGray
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
