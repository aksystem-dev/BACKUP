using SmartModulBackupClasses;
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
    /// Interaction logic for ProgressBarView.xaml
    /// </summary>
    public partial class ProgressBarView : UserControl
    {
        public ProgressMonitor Model
        {
            get { return (ProgressMonitor)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(ProgressMonitor), typeof(ProgressBarView), new PropertyMetadata(null));


        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ProgressBarView), new PropertyMetadata(""));



        public ProgressBarView()
        {
            InitializeComponent();
        }
    }
}
