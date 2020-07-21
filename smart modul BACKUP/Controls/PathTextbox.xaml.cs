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
    /// Umožňuje vybrat cestu k souboru.
    /// </summary>
    public partial class PathTextbox : UserControl
    {
        #region DEPENDENCY PROPERTIES

        public string Filter
        {
            get { return (string)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(string), typeof(PathTextbox), new PropertyMetadata("Všechny soubory|*.*"));



        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(PathTextbox), new PropertyMetadata(""));

        public DialogType PathDialogType
        {
            get { return (DialogType)GetValue(PathDialogTypeProperty); }
            set { SetValue(PathDialogTypeProperty, value); }
        }

        public static readonly DependencyProperty PathDialogTypeProperty =
            DependencyProperty.Register("DialogType", typeof(DialogType), typeof(PathTextbox), new PropertyMetadata(DialogType.OpenFile));


        #endregion

        public PathTextbox()
        {
            InitializeComponent();

            btn_open.Click += btn_open_click;
        }

        private void btn_open_click(object sender, RoutedEventArgs e)
        {
            if (PathDialogType == DialogType.FolderBrowser)
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog()
                {
                    Description = "Vyberte složku"
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    txt_path.Text = dialog.SelectedPath;
            }
            else if (PathDialogType == DialogType.OpenFile)
            {
                var dialog = new System.Windows.Forms.OpenFileDialog()
                {
                    Title = "Zvolte soubor",
                    Filter = this.Filter
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    txt_path.Text = dialog.FileName;
            }
            else if (PathDialogType == DialogType.CreateFile)
            {
                var dialog = new System.Windows.Forms.SaveFileDialog()
                {
                    Title = "Zvolte soubor",
                    Filter = this.Filter
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    txt_path.Text = dialog.FileName;
            }
            else
                throw new NotImplementedException();
        }
    }

    public enum DialogType { OpenFile, CreateFile, FolderBrowser }
}
