using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace smart_modul_BACKUP
{
    public static class Handy
    {
        /// <summary>
        /// Pověšeno na událost TextChanged se postará, aby do TextBoxu mohly jen čísla
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnlyNumbersClubBouncer(object sender, TextChangedEventArgs e)
        {
            var txt = (sender as TextBox);
            int rememberCaret = txt.CaretIndex;

            string text = "";
            foreach (char i in txt.Text)
                if (char.IsDigit(i))
                    text += i;

            if (txt.Text != text) rememberCaret--;

            txt.Text = text;
            txt.CaretIndex = rememberCaret;
        }
    }
}
