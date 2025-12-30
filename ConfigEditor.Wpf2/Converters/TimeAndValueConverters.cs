using System;
using System.Globalization;
using System.Windows.Data;

namespace ConfigEditor.Wpf2.Converters
{
    public class TimeOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return DateTime.Now;
            }
            if (string.IsNullOrEmpty(value.ToString()))
            {
                return DateTime.Now;
            }
            return DateTime.Parse(value.ToString() ?? string.Empty);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime time)
            {
                return time.ToShortTimeString();
            }
            return string.Empty;
        }
    }

    public class StringBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return false;
            }
            return bool.TryParse(value.ToString(), out var b) && b;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString();
        }
    }

    public class StringDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return 0.0;
            }
            if (double.TryParse(value.ToString(), out var v)) return v;
            return 0.0;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString();
        }
    }

    public class ObjStringConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
           return value?.ToString();
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString();
        }
    }
}
