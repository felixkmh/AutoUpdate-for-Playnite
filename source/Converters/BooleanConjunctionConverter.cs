using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace AutoUpdate.Converters
{
    internal class BooleanConjunctionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var result = true;
            foreach (bool v in values)
            {
                result &= v;
            }

            if (parameter is "Inverted")
            {
                result = !result;
            }

            if (targetType == typeof(bool))
                return result;

            if (targetType == typeof(string))
                return result.ToString();

            if (targetType == typeof(Visibility))
                return result ? Visibility.Visible : Visibility.Collapsed;

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
