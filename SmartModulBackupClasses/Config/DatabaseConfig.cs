using System.Data.SqlClient;
using System.ComponentModel;
using System.Xml.Serialization;
using System;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Info o připojení k SQL databázi.
    /// </summary>
    public class DatabaseConfig : INotifyPropertyChanged
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
        private bool integratedSecurity = false;
        private string server = @"";

        public string Server { get; set; } = "";
        public bool IntegratedSecurity { get; set; } = false;
        public string Username { get; set; } = "";

        public Pwd Password { get; set; } = new Pwd();

        public event PropertyChangedEventHandler PropertyChanged;

        public string GetConnectionString(int timeout)
        {
            var strbuilder = new SqlConnectionStringBuilder();
            strbuilder.DataSource = Server;
            strbuilder.InitialCatalog = "Master";
            strbuilder.IntegratedSecurity = IntegratedSecurity;
            strbuilder.ConnectTimeout = timeout;
            if (!IntegratedSecurity)
            {
                strbuilder.UserID = Username;
                strbuilder.Password = Password.Value;
            }
            return strbuilder.ConnectionString;
        }
    }
}
