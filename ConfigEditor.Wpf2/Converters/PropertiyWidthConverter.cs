using System;
using System.Windows.Data;
using ConfigEditor.Wpf2.ViewModels;

namespace ConfigEditor.Wpf2.Converters
{
    public class PropertyWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values[0] is not ConfigViewModel vm)
            {
                return 200;
            }
            if (values[1] is not double v)
            {
                return 200;
            }
            v = v - 40;
            if (vm.Type == ConfigModelType.Object || vm.Type == ConfigModelType.Array)
            {
                return v;
            }
            else
            {
                var count = v / 200;
                count = (int)count;
                var width = v / (count == 0 ? 1 : count);
                return width;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return Array.Empty<object>();
        }
    }
}
