using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Podle vstupu a parametru rozhodne, jestli vrátí FontWeights.Bold, nebo FontWeights.Normal
    /// </summary>
    public class TrueBoldConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolean = (bool)value;
            bool invert = (bool)parameter;
            return (boolean != invert) ? FontWeights.Bold : FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
