using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    public class SftpConfig : INotifyPropertyChanged
    {
        public void AllPropertiesChanged(Action<Action> invoker = null)
        {
            if (PropertyChanged == null)
                return;

            invoker = invoker ?? new Action<Action>(a => a());
            var dgate = PropertyChanged;

            foreach (var prop in GetType().GetProperties())
            {
                if (prop.GetMethod != null && prop.GetMethod.IsPublic)
                    invoker(() => dgate(this, new PropertyChangedEventArgs(prop.Name)));
            }
        }

        private Pwd password = new Pwd("");
        private string username = "";
        private int port = 22;
        private string adress = "";
        private string directory = "";

        public string Host { get; set; } = "";

        public int Port { get; set; } = 22;

        public string Username { get; set; } = "";

        public Pwd Password { get; set; } = new Pwd();

        public string Directory { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
