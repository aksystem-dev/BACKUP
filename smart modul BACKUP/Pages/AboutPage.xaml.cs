using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();

            lbl_version.Content = Utils.GetVersion().ProductVersion;
        }

        private void click_email(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start($"mailto:{e.Uri}");
            }
            catch { }
            e.Handled = true;
        }

        private void click_link(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.ToString());
            }
            catch { }
            e.Handled = true;
        }
    }
}
