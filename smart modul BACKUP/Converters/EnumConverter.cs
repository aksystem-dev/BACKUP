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
    /// Vezme enum, udělá z něj int, a vrací prvek v poli Values na daném indexu.
    /// </summary>
    public class EnumConverter : IValueConverter
    {
        public object[] Values
        {
            get;
            set;
        }
            = new object[] { };

        public EnumConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = value.GetType();

            if (!type.IsEnum)
                return null;

            var index = Array.IndexOf(type.GetEnumValues(), value);

            if (index >= Values.Length)
                return Values.Last();
            else
                return Values[index];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = parameter as Type;

            if (value.GetType() != type)
                return null;

            return Enum.Parse(type, value.ToString());
        }
    }
}
