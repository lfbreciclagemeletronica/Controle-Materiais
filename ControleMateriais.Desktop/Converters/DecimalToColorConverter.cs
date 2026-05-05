using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ControleMateriais.Desktop.Converters;

public class DecimalToColorConverter : IValueConverter
{
    public static readonly DecimalToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d && d > 0m)
            return Color.Parse("#4CAF50");
        return Color.Parse("#555555");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
