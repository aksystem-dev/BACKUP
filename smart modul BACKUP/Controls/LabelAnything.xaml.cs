using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace smart_modul_BACKUP
{


    /// <summary>
    /// Label a k němu nějaký ovládací prvek.
    /// </summary>
    public partial class LabelAnything : UserControl
    {
        #region DEPENDENCY PROPERTIES

        /// <summary>
        /// Text v labelu
        /// </summary>
        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register("LabelText", typeof(string), typeof(LabelAnything));

        /// <summary>
        /// Šířka labelu
        /// </summary>
        public int LabelWidth
        {
            get => (int)GetValue(LabelWidthProperty);
            set => SetValue(LabelWidthProperty, value);
        }

        public static readonly DependencyProperty LabelWidthProperty =
            DependencyProperty.Register("LabelWidth", typeof(int), typeof(LabelAnything));

        #endregion


        public LabelAnything()
        {
            InitializeComponent();
        }
    }
}
