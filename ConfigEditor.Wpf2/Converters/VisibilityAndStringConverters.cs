using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConfigEditor.Wpf2.Converters
{
    public class Boolean2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b) return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v) return v == Visibility.Visible;
            return false;
        }
    }

    public class String2VisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value?.ToString();
            var visible = !string.IsNullOrEmpty(s);
            if (Invert) visible = !visible;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
