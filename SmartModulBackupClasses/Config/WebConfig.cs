using SmartModulBackupClasses.WebApi;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    public class WebConfig : INotifyPropertyChanged
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

        public string Username { get; set; } = "";
        public Pwd Password { get; set; } = new Pwd();

        /// <summary>
        /// Pokud false, znamená to, že používáme aplikaci offline; pokud true, znamená to, že se připojujeme na API
        /// </summary>
        public bool Online { get; set; } = false;


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
