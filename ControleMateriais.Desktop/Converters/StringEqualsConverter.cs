using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ControleMateriais.Desktop.Converters;

/// <summary>
/// Retorna true se o valor string for igual ao parâmetro string.
/// Usado para marcar ToggleButton de filtro como checked quando FiltroStatus == Tag.
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
