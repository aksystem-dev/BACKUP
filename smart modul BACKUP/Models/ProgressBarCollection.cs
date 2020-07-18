using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Seznam ProgressBarModel. ProgressBar se přidá pomocí Add(), a odstraní zavoláním Remove() na získané instanci.
    /// </summary>
    [Obsolete("Místo tohoto se používá BackupInProgress, RestoreInProgress a ProgressMonitor.")]
    public class ProgressBarCollection : INotifyPropertyChanged
    {
        private List<ProgressBarModel> _progressBars = new List<ProgressBarModel>();
        public ProgressBarModel[] ProgressBars => _progressBars.ToArray();

        public event PropertyChangedEventHandler PropertyChanged;

        private void _change(params string[] names) => names.ForEach(f => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(f)));

        /// <summary>
        /// Přidá novou instanci ProgressBarModel na seznam a vrátí na ni odkaz.
        /// </summary>
        /// <returns></returns>
        public ProgressBarModel Add()
        {
            var model = new ProgressBarModel()
            {
                ID = _progressBars.Any() ? _progressBars.Max(f => f.ID) : 0,
                Progress = 0,
                StateMsg = ""
            };

            model.OnRemoved += Model_OnRemoved;
            _progressBars.Add(model);
            _change(nameof(ProgressBars));
            return model;
        }

        private void Model_OnRemoved(object sender, EventArgs e)
        {
            _progressBars.Remove(sender as ProgressBarModel);
            _change(nameof(ProgressBars));
        }

        /// <summary>
        /// Vrátí ProgressBarModel podle id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ProgressBarModel GetById(int id)
        {
            return _progressBars.FirstOrDefault(f => f.ID == id);
        }
    }
}
