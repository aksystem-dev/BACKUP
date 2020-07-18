using System.Data.SqlClient;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Info o připojení k SQL databázi.
    /// </summary>
    public class DatabaseConfig : INotifyPropertyChanged
    {
        private bool unsavedChanges;

        [XmlIgnore]
        public bool UnsavedChanges
        {
            get => unsavedChanges;
            set
            {
                if (value == unsavedChanges)
                    return;

                unsavedChanges = value;
                propChanged(nameof(UnsavedChanges));
            }
        }

        public DatabaseConfig()
        {
            PropertyChanged += DatabaseConfig_PropertyChanged;
        }

        private void DatabaseConfig_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UnsavedChanges))
                UnsavedChanges = true;
        }

        private void propChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private Pwd password = new Pwd("");
        private string username = "";
        private bool integratedSecurity = false;
        private string server = @"";

        public string Server
        {
            get => server;
            set
            {
                if (value == server)
                    return;

                server = value;
                propChanged(nameof(Server));
            }
        }
        public bool IntegratedSecurity
        {
            get => integratedSecurity;
            set
            {
                if (value == integratedSecurity)
                    return;

                integratedSecurity = value;
                propChanged(nameof(IntegratedSecurity));
            }
        }
        public string Username
        {
            get => username;
            set
            {
                if (username == value)
                    return;

                username = value;
                propChanged(nameof(Username));
            }
        }

        public Pwd Password
        {
            get => password; 
            set
            {
                if (password == value)
                    return;

                if (password != null)
                    password.PropertyChanged -= DatabaseConfig_PropertyChanged;

                if (value != null)
                    value.PropertyChanged += DatabaseConfig_PropertyChanged;

                password = value;
                propChanged(nameof(Password));
            }
        }

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
