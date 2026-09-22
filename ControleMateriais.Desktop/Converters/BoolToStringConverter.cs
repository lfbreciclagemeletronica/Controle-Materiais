using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ControleMateriais.Desktop.Converters
{
    public sealed class BoolToStringConverter : IValueConverter
    {
        public string? TrueText  { get; set; }
        public string? FalseText { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? TrueText : FalseText;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
