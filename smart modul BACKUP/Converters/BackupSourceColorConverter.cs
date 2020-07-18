using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Nastaví barvu labelu pro název databáze podle toho, jestli danou databázi zálohujeme, nezálohujeme,
    /// nebo o ní ani nevíme.
    /// </summary>
    class BackupSourceColorConverter : IValueConverter
    {
        public Brush UnselectedBrush { get; set; }
        public Brush EnabledBrush { get; set; }
        public Brush DisabledBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bak_src = value as BackupSourceModel;

            if (!bak_src.selected)
                return UnselectedBrush;
            else if (bak_src.source.enabled)
                return EnabledBrush;
            else
                return DisabledBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
