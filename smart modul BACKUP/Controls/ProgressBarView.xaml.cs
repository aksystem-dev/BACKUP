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
    /// ProgressBar a Label k tomu
    /// </summary>
    public partial class ProgressBarView : UserControl
    {

        public float Progress
        {
            get { return (float)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(float), typeof(ProgressBarView), new PropertyMetadata(0f));


        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ProgressBarView), new PropertyMetadata(""));



        public string Label1
        {
            get { return (string)GetValue(Label1Property); }
            set { SetValue(Label1Property, value); }
        }

        public static readonly DependencyProperty Label1Property =
            DependencyProperty.Register("Label1", typeof(string), typeof(ProgressBarView), new PropertyMetadata(""));

        public string Label2
        {
            get { return (string)GetValue(Label2Property); }
            set { SetValue(Label2Property, value); }
        }

        public static readonly DependencyProperty Label2Property =
            DependencyProperty.Register("Label2", typeof(string), typeof(ProgressBarView), new PropertyMetadata(""));




        public ProgressBarView()
        {
            InitializeComponent();
        }
    }
}
