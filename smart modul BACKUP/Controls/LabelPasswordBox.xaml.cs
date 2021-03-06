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
    /// Label a k němu PasswordBox.
    /// </summary>
    public partial class LabelPasswordBox : UserControl
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
            DependencyProperty.Register("LabelText", typeof(string), typeof(LabelPasswordBox));

        /// <summary>
        /// Šířka labelu
        /// </summary>
        public int LabelWidth
        {
            get => (int)GetValue(LabelWidthProperty);
            set => SetValue(LabelWidthProperty, value);
        }

        public static readonly DependencyProperty LabelWidthProperty =
            DependencyProperty.Register("LabelWidth", typeof(int), typeof(LabelPasswordBox));

        public Brush TextBoxBorderBrush
        {
            get { return (Brush)GetValue(TextBoxBorderBrushProperty); }
            set { SetValue(TextBoxBorderBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextBoxBorderBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextBoxBorderBrushProperty =
            DependencyProperty.Register("TextBoxBorderBrush", typeof(Brush), typeof(LabelPasswordBox));



        /// <summary>
        /// Jestli je text v textovém poli validní.
        /// </summary>
        public bool Valid
        {
            get { return (bool)GetValue(ValidProperty); }
            set { SetValue(ValidProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Valid.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValidProperty =
            DependencyProperty.Register("Valid", typeof(bool), typeof(LabelPasswordBox));



        #endregion

        public LabelPasswordBox()
        {
            InitializeComponent();

            //Panel.DataContext = this;
        }

        public void SetPassword(string password)
        {
            PasswordInput.Password = password;
        }

        public string GetPassword()
        {
            return PasswordInput.Password;
        }

    }
}
