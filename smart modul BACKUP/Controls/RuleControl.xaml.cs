using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows;
using System;
using System.Linq;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.IO;
using SmartModulBackupClasses;
using System.Collections.Generic;
using System.Globalization;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro RuleControl.xaml
    /// </summary>
    public partial class RuleControl : UserControl
    {
        private BackupRule Rule { get => DataContext as BackupRule; }

        private ObservableCollection<BackupSourceModel> databases = new ObservableCollection<BackupSourceModel>();
        private ObservableCollection<BackupSourceModel> directories = new ObservableCollection<BackupSourceModel>();
        private ObservableCollection<BackupSourceModel> files = new ObservableCollection<BackupSourceModel>();

        public RuleControl()
        {
            InitializeComponent();
            initFieldArrays();
            CreateMonthsUI();

            //Pokud se klikne na header, pravidlo se roztáhne
            //HeaderBorder.MouseUp += (_, __) => Expand();

            //Tlačítko na přidání složky
            btn_addFolderSource.Click += (_, __) => AddFolderSource();

            //Tlačítko na odebrání složek
            btn_removeFolderSource.Click += (_, __) => RemoveFolderSource();

            btn_addFileSource.Click += (_, __) => AddFileSource();
            btn_removeFileSource.Click += (_, __) => RemoveFileSource();

            //Povolat vyhazovače nečíselných hodnot
            txt_localBackupCount.textbox.TextChanged += Handy.OnlyNumbersClubBouncer;
            txt_remoteBackupsCount.textbox.TextChanged += Handy.OnlyNumbersClubBouncer;

            DataContextChanged += RuleControl_DataContextChanged;


            directories.CollectionChanged += Directories_CollectionChanged;
            files.CollectionChanged += Files_CollectionChanged;
        }


        /// <summary>
        /// pokud true, jakékoliv změny v ObservableCollection directories se automaticky aplikují na pravidlo
        /// </summary>
        bool directories_initialized = false;
        private void Directories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!directories_initialized)
                return;

            Rule.Sources.Directories = directories.Select(f => f.source).ToArray();
        }

        /// <summary>
        /// pokud true, jakékoliv změny v ObservableCollection files se automaticky aplikují na pravidlo
        /// </summary>
        bool files_initialized = false;
        private void Files_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!files_initialized)
                return;

            Rule.Sources.Files = files.Select(f => f.source).ToArray();
        }



        private void RuleControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            //nastavit databases jako zdroj dat pro DbBackups ovládací prvek
            DbBackups.ItemsSource = databases;
            FolderBackups.ItemsSource = directories;
            IndividualFileBackups.ItemsSource = files;

            //načíst databáze do vlastního seznamu
            //z tohoto seznamu se pomocí vlastnosti "selected" budou filtrovat databáze, které pravidlo zná
            //pro každou databázi, kterou pravidlo zná, lze nastavit enabled, aby se nastavilo,
            //  zdali se daná databáze bude zálohovat
            LoadDbs();
            LoadDirs();
            LoadFiles();
            LoadTime();
            LoadWeekdays();
            LoadMonths();

            //directories.CollectionChanged += (_, __) => UpdateRuleSources();
            //databases.CollectionChanged += (_, __) => UpdateRuleSources();

            //všechny složky mají enabled na true
            foreach (var f in Rule.Sources.Directories)
                f.enabled = true;

            //soubory jakbysmet
            foreach (var f in Rule.Sources.Files)
                f.enabled = true;
        }

        private void FitTimeString(TextBox h, TextBox m, string str)
        {
            string[] spl = str.Split(':');
            h.Text = spl[0];
            m.Text = spl[1];
        }

        /// <summary>
        /// načte databáze z pravidla do observablecollection "databases", odkud se budou bindovat na view
        /// </summary>
        private void LoadDbs()
        {
            databases.Clear();

            loadRuleDbs();

            loadServerDbs();
        }

        /// <summary>
        /// Načte databáze, které jsou na aktuálním serveru, a zároveň nejsou v pravidle
        /// </summary>
        private void loadServerDbs()
        {
            //načíst databáze, které pravidlo nezná
            //procházíme všechny db
            foreach (var db in LoadedStatic.availableDatabases)
            {
                //pokud je tato databáze už načtená, načteme ještě název firmy
                var already_laoded = databases.Where(f => f.DbInfo.name.ToLower() == db.name.ToLower());
                if (already_laoded.Any())
                {
                    already_laoded.ForEach(f => f.DbInfo.firma = db.firma);
                    continue;
                }

                //pokud jsme danou databázi ještě nepřidali, přidáme jí
                var source = new BackupSourceModel()
                {
                    source = new BackupSource()
                    {
                        enabled = false,
                        id = null,
                        path = db.name,
                        type = BackupSourceType.Database
                    },
                    selected = false,
                    DbInfo = db
                };

                ////budeme poslouchat změny; nastaví-li se "selected" na true, musíme databázi přidat
                ////pravidlu na seznam známých databází (Rule.Sources.Databases)
                //source.PropertyChanged += (_, __) => UpdateRuleSources();

                databases.Add(source);
            }
        }

        private void loadRuleDbs()
        {
            //načíst databáze, které pravidlo zná
            if (Rule.Sources.Databases != null)
                foreach (var db in Rule.Sources.Databases)
                {
                    BackupSourceModel model = new BackupSourceModel()
                    {
                        selected = true,
                        source = db,
                        DbInfo = new Models.AvailableDatabase() { name = db.path }
                    };

                    ////budeme poslouchat změny
                    //model.PropertyChanged += (_, __) => UpdateRuleSources();

                    databases.Add(model);
                }
        }

        private void LoadDirs()
        {
            directories_initialized = false;

            directories.Clear();
            foreach(var dir in Rule.Sources.Directories)
            {
                directories.Add(new BackupSourceModel()
                {
                    selected = false,
                    source = dir
                });
            }

            directories_initialized = true;
        }

        private void LoadFiles()
        {
            files_initialized = false;

            files.Clear();
            foreach(var file in Rule.Sources.Files)
            {
                files.Add(new BackupSourceModel()
                {
                    selected = false,
                    source = file
                });
            }

            files_initialized = true;
        }

        private void AddFolderSource()
        {
            //přidat nový backupsource na složku
            directories.Add(new BackupSourceModel()
            {
                source = new BackupSource()
                {
                    enabled = true,
                    id = null,
                    path = "",
                    type = BackupSourceType.Directory
                },
                selected = false
            });
        }

        private void RemoveFolderSource()
        {
            //odstranit všechny složky, které jsou vybrané (checkbox je u nich zaškrtnutý)
            foreach (var i in directories.Where(f => f.selected).ToArray())
                directories.Remove(i);
        }

        private void AddFileSource()
        {
            //přidat nový backupsource na soubor
            files.Add(new BackupSourceModel()
            {
                source = new BackupSource()
                {
                    enabled = true,
                    id = null,
                    path = "",
                    type = BackupSourceType.File
                },
                selected = false
            });
        }

        private void RemoveFileSource()
        {
            //odstranit všechny soubory, které jsou vybrané (checkbox je u nich zaškrtnutý)
            foreach (var i in files.Where(f => f.selected).ToArray())
                files.Remove(i);
        }


        private void Remove()
        {
            var dialog = new YesNoDialog()
            {
                PromptText = "ODSTRANIT PRAVIDLO?"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                File.Delete(Rule.path);
                LoadedStatic.rules.Remove(Rule);
            }
        }

        int current_radio_button_id = -1;
        private void RadioButtonLoaded(object sender, RoutedEventArgs e)
        {
            //zde se bavíme o RadioButton výběru databází, kde chceme, aby každé radiobutton mělo vlastní skupinu
            (sender as RadioButton).GroupName = (current_radio_button_id++).ToString();
        }

        private void BackupAllDb(object sender, RoutedEventArgs e)
        {
            foreach(var model in databases)
            {
                if (!model.selected)
                    model.selected = true;
                model.source.enabled = true;
            }
        }

        private void DontBackupAllDb(object sender, RoutedEventArgs e)
        {
            foreach (var model in databases)
            {
                if (!model.selected)
                    model.selected = true;
                model.source.enabled = false;
            }
        }

        private void BackupChecked(object sender, RoutedEventArgs e)
        {
            var radio = (sender as RadioButton);
            var model = (radio.DataContext as BackupSourceModel);
            model.source.enabled = true;
            if (!model.selected)
                model.selected = true;

            //zaškrtnutím zálohovat / nezálohovat přidáme info o zdroji do pravidla (pokud tam není)
            if (!Rule.Sources.All.Contains(model.source))
                Rule.Sources.All.Add(model.source);
        }

        private void BackupUnchecked(object sender, RoutedEventArgs e)
        {
            var radio = (sender as RadioButton);
            var model = (radio.DataContext as BackupSourceModel);
            model.source.enabled = false;
            if (!model.selected)
                model.selected = true;

            //zaškrtnutím zálohovat / nezálohovat přidáme info o zdroji do pravidla (pokud tam není)
            if (!Rule.Sources.All.Contains(model.source))
                Rule.Sources.All.Add(model.source);
        }

        #region PODMÍNKY SPUŠTĚNÍ - ČAS
        private void UpdateTime(object sender, RoutedEventArgs e)
        {
            //pouze čísla berem
            if (sender.GetType() == typeof(TextBox))
                Handy.OnlyNumbersClubBouncer(sender, e as TextChangedEventArgs);

            try
            {
                if ((bool)timeSingle.IsChecked)
                {
                    //pokud je zaškrtnuto timeSingle, zašedivíme ovládací prvky timeInterval
                    txt_h_int.IsEnabled = false;
                    txt_h_max.IsEnabled = false;
                    txt_h_min.IsEnabled = false;
                    txt_m_int.IsEnabled = false;
                    txt_m_max.IsEnabled = false;
                    txt_m_min.IsEnabled = false;

                    //odšedivíme ovládací prvky timeSingle
                    txt_h.IsEnabled = true;
                    txt_m.IsEnabled = true;

                    //parse
                    int h = ParseTxtInt(txt_h, 0, 23);
                    int m = ParseTxtInt(txt_m, 0, 59);

                    //nastavit string času
                    Rule.Conditions.Time = $"{h}:{m}";
                }
                else
                {
                    //timeInterval => odšedivit prvky timeInterval
                    txt_h_int.IsEnabled = true;
                    txt_h_max.IsEnabled = true;
                    txt_h_min.IsEnabled = true;
                    txt_m_int.IsEnabled = true;
                    txt_m_max.IsEnabled = true;
                    txt_m_min.IsEnabled = true;

                    //zašedivit timeSingle
                    txt_h.IsEnabled = false;
                    txt_m.IsEnabled = false;

                    //parse
                    int h_min = ParseTxtInt(txt_h_min, 0, 23);
                    int m_min = ParseTxtInt(txt_m_min, 0, 59);
                    int h_max = ParseTxtInt(txt_h_max, 0, 23);
                    int m_max = ParseTxtInt(txt_m_max, 0, 59);
                    int h_int = ParseTxtInt(txt_h_int, 0, 23);
                    int m_int = ParseTxtInt(txt_m_int, 0, 59);

                    //nastavit string času
                    Rule.Conditions.Time = $"{h_min}:{m_min} - {h_max}:{m_max} / {h_int}:{m_int}";
                }

                //pokud Conditions skuhře, že čas není validní, zjistit proč
                if (!Rule.Conditions.TimeValid)
                {
                    label_timeValid.Visibility = Visibility.Visible;

                    if (Rule.Conditions._time.Any(f => f.Min != f.Max && f.Interval == TimeSpan.Zero) && !(bool)timeSingle.IsChecked)
                        label_timeValid.Content = "Interval nemůže být nulový.";
                    else
                        label_timeValid.Content = "Špatný formát.";
                }
                else
                    label_timeValid.Visibility = Visibility.Hidden;
            }
            catch (NullReferenceException)
            {

            }
        }

        /// <summary>
        /// Načte čas z podmínek pravidla a dá ho do textboxů
        /// </summary>
        private void LoadTime()
        {
            try
            {
                string time = Rule.Conditions.Time.Split(',')[0];

                if (time.Contains('-'))
                {
                    timeInterval.IsChecked = true;

                    string[] tspl = time.Split('-');
                    FitTimeString(txt_h_min, txt_m_min, tspl[0].Trim());
                    if (tspl[1].Contains('/'))
                    {
                        string[] tsplspl = tspl[1].Split('/');
                        FitTimeString(txt_h_max, txt_m_max, tsplspl[0].Trim());
                        FitTimeString(txt_h_int, txt_m_int, tsplspl[1].Trim());
                    }
                    else
                    {
                        FitTimeString(txt_h_max, txt_m_max, tspl[1].Trim());
                        FitTimeString(txt_h_int, txt_m_int, "1:0");
                    }
                }
                else
                {
                    timeSingle.IsChecked = true;

                    FitTimeString(txt_h, txt_m, time);
                }
            }
            catch (NullReferenceException)
            {

            }
        }


        private int ParseTxtInt(TextBox box, int min, int max)
        {
            if (box.Text == "")
                return min;
            else if (int.TryParse(box.Text, out int num))
            {
                if (num > max)
                {
                    box.Text = max.ToString();
                    return max;
                }
                else if (num < min)
                {
                    box.Text = min.ToString();
                    return min;
                }
                else
                    return num;
            }
            else
                throw new FormatException();
        }

        /// <summary>
        /// Seznam seznamů textboxů, které jdou za sebou. Využíváno metodou TimeFieldKeyDown.
        /// </summary>
        private TextBox[][] fieldArrays;
        /// <summary>
        /// Inicializace fieldArrays.
        /// </summary>
        private void initFieldArrays()
        {
            fieldArrays = new TextBox[][]
            {
                new TextBox[]
                {
                    txt_h,
                    txt_m
                },
                new TextBox[]
                {
                    txt_h_min,
                    txt_m_min,
                    txt_h_max,
                    txt_m_max,
                    txt_h_int,
                    txt_m_int
                }
            };
        }

        /// <summary>
        /// Pokud uživatel zadává čas, chceme mu pro jeho pohodlí umožnit stisknout šipku vlevo nebo vpravo,
        /// aby se dostal do sousedních polí.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeFieldKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var box = sender as TextBox;
            if (box == null)
                return;

            int target = 0; //0 = zůstat kde jsme, -1 = doleva, 1 = doprava

            if (e.Key == System.Windows.Input.Key.Left && box.CaretIndex == 0)
                target = -1;
            else if (e.Key == System.Windows.Input.Key.Right && box.CaretIndex == box.Text.Length)
                target = 1;

            if (target == 0)
                return;

            foreach (TextBox[] txt in fieldArrays)
            {
                int box_ind = Array.IndexOf(txt, box);

                if (box_ind >= 0)
                {
                    int new_ind = box_ind + target;
                    if (new_ind >= 0 && new_ind < txt.Length)
                    {
                        var new_txt = txt[new_ind];
                        new_txt.CaretIndex = target == -1 ? new_txt.Text.Length : 0;
                        new_txt.Focus();
                        break;
                    }
                }
            }
        }
        #endregion

        #region PODMÍNKY SPUŠTĚNÍ - DEN V TÝDNU
        private CheckBox[] cb_weekDays;

        private void initWeekDays()
        {
            cb_weekDays = new CheckBox[]
            {
                cb_sunday,
                cb_monday,
                cb_tuesday,
                cb_wednesday,
                cb_thursday,
                cb_friday,
                cb_saturday
            };
        }

        private void LoadWeekdays()
        {
            initWeekDays();

            //toto musíme vypnout, abychom změnou IsChecked neinvokovali UpdateWeekdays, páč to dělá bordel
            updateWeekdaysEnabled = false;

            if (Rule.Conditions._dayInWeek.Length == 0)
            {
                //pokud nejsou v podmínkách uvedeny dny v týdnu, zašedivět dny v týdnu
                //(ale všechny je zaškrtnout, aby to bylo pro uživatele jednodušší, kdyby se rozhodl filtr použít)
                cb_limitWeekdays.IsChecked = false;
                cb_weekDays.ForEach(f =>
                {
                    f.IsChecked = true;
                    f.IsEnabled = false;
                });
            }
            else
            {
                //jinak všechny odšedivět a zaškrtnout ty, které jsou povolené v pravidlu

                cb_limitWeekdays.IsChecked = true;
                cb_weekDays.ForEach(f => f.IsEnabled = true);

                for (int i = 0; i < cb_weekDays.Length; i++)
                    cb_weekDays[i].IsChecked = Rule.Conditions.DayInWeekFits(i);
            }


            updateWeekdaysEnabled = true;
        }

        bool updateWeekdaysEnabled = false;

        /// <summary>
        /// dny v týdnu: GUI -> pravidlo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateWeekdays(object sender, RoutedEventArgs e)
        {
            if (!updateWeekdaysEnabled)
                return;

            if (!(bool)cb_limitWeekdays.IsChecked)
            {
                Rule.Conditions.DayInWeek = "";
                cb_weekDays.ForEach(f => f.IsEnabled = false);
                return;
            }
            else
                cb_weekDays.ForEach(f => f.IsEnabled = true);

            List<int> days = new List<int>();

            for (int i = 0; i < cb_weekDays.Length; i++)
                if ((bool)cb_weekDays[i].IsChecked)
                    days.Add(i);

            Rule.Conditions.DayInWeek = String.Join(", ", days);
        }
        #endregion

        #region PODMÍNKY SPUŠTĚNÍ - MĚSÍC

        private CheckBox[] cb_months;

        /// <summary>
        /// Vygeneruje 12 checkboxů pro 12 měsíců.
        /// </summary>
        private void CreateMonthsUI()
        {
            cb_months = new CheckBox[12];
            for (int i = 0; i < 12; i++)
            {
                string monthname = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(i + 1);

                CheckBox cb = new CheckBox();
                cb.Margin = new Thickness(5);
                cb.Content = monthname.ToUpper();

                cb.Checked += UpdateMonths;
                cb.Unchecked += UpdateMonths;

                panel_months.Children.Add(cb);
                cb_months[i] = cb;
            }
        }

        /// <summary>
        /// Načte měsíce z podmínek pravidla.
        /// </summary>
        private void LoadMonths()
        {
            updateMonthsEnabled = false;

            if (Rule.Conditions._month.Length == 0)
            {
                //pokud nejsou v podmínkách uvedeny měsíce, zašedivět je
                //(ale všechny je zaškrtnout, aby to bylo pro uživatele jednodušší, kdyby se rozhodl filtr použít)
                cb_limitMonths.IsChecked = false;
                cb_months.ForEach(f =>
                {
                    f.IsChecked = true;
                    f.IsEnabled = false;
                });
            }
            else
            {
                //jinak všechny odšedivět a zaškrtnout ty, které jsou povolené v pravidlu

                cb_limitMonths.IsChecked = true;
                cb_months.ForEach(f => f.IsEnabled = true);

                for (int i = 0; i < cb_months.Length; i++)
                    cb_months[i].IsChecked = Rule.Conditions.MonthFits(i);
            }

            updateMonthsEnabled = true;
        }

        private bool updateMonthsEnabled = false;

        /// <summary>
        /// měsíce: GUI -> pravidlo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateMonths(object sender, RoutedEventArgs e)
        {
            if (!updateMonthsEnabled)
                return;

            if (!(bool)cb_limitMonths.IsChecked)
            {
                Rule.Conditions.Month = "";
                cb_months.ForEach(f => f.IsEnabled = false);
                return;
            }
            else
                cb_months.ForEach(f => f.IsEnabled = true);

            List<int> months = new List<int>();
            for (int i = 0; i < 12; i++)
                if ((bool)cb_months[i].IsChecked)
                    months.Add(i + 1);

            Rule.Conditions.Month = String.Join(", ", months);
        }

        #endregion


        private void _dbReload(object sender, RoutedEventArgs e)
        {
            //znovu načíst názvy db ze serveru
            LoadedStatic.LoadAvailableDatabases();

            //odstranit všechny databáze ze seznamu, které nebyly přidány do pravidla
            foreach (var i in databases.ToArray())
                if (!Rule.Sources.All.Contains(i.source))
                    databases.Remove(i);
            
            //znovu načíst serverové databáze do observablecollection
            loadServerDbs();
        }
    }
}
