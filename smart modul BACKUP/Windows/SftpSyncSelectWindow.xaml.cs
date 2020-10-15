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
using System.Windows.Shapes;

namespace smart_modul_BACKUP.Windows
{
    /// <summary>
    /// Obaluje PC_Info, přidává vlastnost Selected = zdali byl počítač zaškrtnut
    /// </summary>
    public class PC_Info_Wrapper
    {
        public bool Selected { get; set; }
        public PC_Info Value { get; set; }
        public string DisplayName => Value.DisplayName;

        public PC_Info_Wrapper(PC_Info value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Okno umožňující uživateli vybrat si ze seznamu počítačů
    /// </summary>
    public partial class SftpSyncSelectWindow : Window
    {
        /// <summary>
        /// Tento počítač. Používáno pro Binding
        /// </summary>
        public PC_Info_Wrapper ThisPC { get; set; }

        /// <summary>
        /// Ostatní počítače. Používáno pro Binding
        /// </summary>
        public List<PC_Info_Wrapper> OtherPCs { get; set; }

        /// <summary>
        /// Počítače, které uživatel vybral
        /// </summary>
        public IEnumerable<PC_Info> SelectedPCs
        {
            get
            {
                if (ThisPC.Selected)
                    yield return ThisPC.Value;

                foreach (var pc_w in OtherPCs)
                    if (pc_w.Selected)
                        yield return pc_w.Value;
            }
        }

        public SftpSyncSelectWindow(IEnumerable<PC_Info> infosToSelectFrom)
        {
            ThisPC = new PC_Info_Wrapper(PC_Info.This);
            OtherPCs = infosToSelectFrom.Where(pc => !pc.IsThis).Select(pc => new PC_Info_Wrapper(pc)).ToList();

            InitializeComponent();
        }
        
        private void click_ok(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void click_storno(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
