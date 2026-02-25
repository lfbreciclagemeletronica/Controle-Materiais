using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ControleMateriais.Desktop.Converters
{

    public sealed class BooleanNegationConverter : IValueConverter
    {
        // Opcional: instância única, caso queira usar x:Static depois
        public static readonly BooleanNegationConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : value;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }

}
