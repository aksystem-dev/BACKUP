using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Tohle jsem vytvořil, ještě než jsem věděl o existenci ToggleButton. Funguje to zhruba stejně.
    /// </summary>
    public class ButtonWithState : Button
    {
        public bool On
        {
            get { return (bool)GetValue(OnProperty); }
            set { SetValue(OnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for On.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OnProperty =
            DependencyProperty.Register("On", typeof(bool), typeof(ButtonWithState), new PropertyMetadata(false));
    }
}
