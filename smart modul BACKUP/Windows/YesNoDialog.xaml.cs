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
using System.Windows.Shapes;

namespace smart_modul_BACKUP
{


    /// <summary>
    /// Interakční logika pro YesNoDialog.xaml
    /// </summary>
    public partial class YesNoDialog : Window
    {
        #region DEPENDENCY PROPERTIES

        public string PromptText
        {
            get { return (string)GetValue(PromptTextProperty); }
            set { SetValue(PromptTextProperty, value); }
        }

        public static readonly DependencyProperty PromptTextProperty =
            DependencyProperty.Register("PromptText", typeof(string), typeof(YesNoDialog), new PropertyMetadata("Jste si jisti?"));

        #endregion

        public YesNoDialog()
        {
            InitializeComponent();

            //když kliknu na čůdl, nastaví se dialogresult a okno se zavře
            btn_yes.Click += (_, __) =>
            {
                DialogResult = true;
                Close();
            };

            btn_no.Click += (_, __) =>
            {
                DialogResult = false;
                Close();
            };
        }
    }
}
