using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Interakční logika pro BackupsPage.xaml
    /// </summary>
    public partial class BackupsPage : Page, INotifyPropertyChanged
    {
        private readonly CollectionViewSource cvs;

        private bool _certainDateEnabled = false;
        private bool _minDateEnabled = false;
        private bool _maxDateEnabled = false;
        private DateTime _certainDate = DateTime.Now.Date;
        private DateTime _maxDate = DateTime.Now.Date;
        private DateTime _minDate = DateTime.Now.Date;

        public event PropertyChangedEventHandler PropertyChanged;

        public BackupInfoManager BkMan { get; set; }

        private void change(params string[] property) => property.ForEach(f => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(f)));

        public bool CertainDateEnabled
        {
            get => _certainDateEnabled;
            set
            {
                _certainDateEnabled = value;
                change(nameof(CertainDateEnabled), nameof(MinDateEnabled), nameof(MaxDateEnabled));
                cvs.View.Refresh();
            }
        }

        public bool MinDateEnabled
        {
            get => _minDateEnabled && !_certainDateEnabled;
            set
            {
                _minDateEnabled = value;
                change(nameof(MinDateEnabled));
                cvs.View.Refresh();
            }
        }

        public bool MaxDateEnabled
        {
            get => _maxDateEnabled && !_certainDateEnabled;
            set
            {
                _maxDateEnabled = value;
                change(nameof(MaxDateEnabled));
                cvs.View.Refresh();
            }
        }

        public DateTime CertainDate
        {
            get => _certainDate;
            set
            {
                _certainDate = value;
                change(nameof(CertainDate));
                cvs.View.Refresh();
            }
        }

        public DateTime MinDate
        {
            get => _minDate;
            set
            {
                _minDate = value;
                change(nameof(MinDate));
                cvs.View.Refresh();
            }
        }

        public DateTime MaxDate
        {
            get => _maxDate;
            set
            {
                _maxDate = value;
                change(nameof(MaxDate));
                cvs.View.Refresh();
            }
        }
        
        public BackupsPage()
        {
            InitializeComponent();
            cvs = Resources["savedBackupsSource"] as CollectionViewSource;
            BkMan = Manager.Get<BackupInfoManager>();

            if (BkMan != null)
                cvs.Source = BkMan.Backups;

            BkMan.PropertyChanged += BkMan_PropertyChanged;
        }

        private void BkMan_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            cvs.Source = BkMan.Backups;
        }

        private bool _backup_certainPassed(Backup b) => !CertainDateEnabled || b.EndDateTime.Date == CertainDate || b.StartDateTime.Date == CertainDate;
        private bool _backup_minPassed(Backup b) => !MinDateEnabled || b.EndDateTime.Date >= MinDate;
        private bool _backup_maxPassed(Backup b) => !MaxDateEnabled || b.StartDateTime.Date <= MaxDate;

        private void savedBackupsFilter(object sender, FilterEventArgs e)
        {
            var b = e.Item as Backup;
            e.Accepted = _backup_certainPassed(b) && _backup_minPassed(b) && _backup_maxPassed(b);
        }

        private void btn_click_restore(object sender, RoutedEventArgs e)
        {
            var backup = (sender as FrameworkElement).DataContext as Backup;
            if (backup != null)
                MainWindow.main.ShowPage(new RestorePage(backup));
        }

        private async void page_loaded(object sender, RoutedEventArgs e)
        {
            await BkMan.LoadAsync();
        }

        private void mousewheel(object sender, MouseWheelEventArgs e)
        {
            //what we're doing here, is that we're invoking the "MouseWheel" event of the parent ScrollViewer.

            //first, we make the object with the event arguments (using the values from the current event)
            var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);

            //then we need to set the event that we're invoking.
            //the ScrollViewer control internally does the scrolling on MouseWheelEvent, so that's what we're going to use:
            args.RoutedEvent = ScrollViewer.MouseWheelEvent;

            //and finally, we raise the event on the parent ScrollViewer.
            scroll_viewer.RaiseEvent(args);
        }
    }
}
