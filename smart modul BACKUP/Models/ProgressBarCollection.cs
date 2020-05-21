using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP
{
    public class ProgressBarCollection : INotifyPropertyChanged
    {
        private List<ProgressBarModel> _progressBars = new List<ProgressBarModel>();
        public ProgressBarModel[] ProgressBars => _progressBars.ToArray();

        public event PropertyChangedEventHandler PropertyChanged;

        private void _change(params string[] names) => names.ForEach(f => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(f)));

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

        public ProgressBarModel GetById(int id)
        {
            return _progressBars.FirstOrDefault(f => f.ID == id);
        }
    }
}
