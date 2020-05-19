using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace smart_modul_BACKUP
{
    class ByteSizeToStringConverter : IValueConverter
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double size = System.Convert.ToDouble(value);

            int order = 0;
            while(size > 1024 && order < suffixes.Length - 1)
            {
                size /= 1024;
                order++;
            }

            return $"{size.ToString("0.##")} {suffixes[order]}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
