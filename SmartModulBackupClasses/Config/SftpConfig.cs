using System.ComponentModel;

namespace SmartModulBackupClasses
{
    public class SftpConfig : INotifyPropertyChanged
    {
        private bool unsavedChanges;
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

        public SftpConfig()
        {
            PropertyChanged += SftpConfig_PropertyChanged;
        }

        private void SftpConfig_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
        private int port = 22;
        private string adress = "";

        public string Host
        {
            get => adress;
            set
            {
                if (value == adress)
                    return;

                adress = value;
                propChanged(nameof(Host));
            }
        }

        public int Port
        {
            get => port; 
            set
            {
                if (value == port)
                    return;

                port = value;
                propChanged(nameof(Port));
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
                    password.PropertyChanged -= SftpConfig_PropertyChanged;

                if (value != null)
                    value.PropertyChanged += SftpConfig_PropertyChanged;

                password = value;
                propChanged(nameof(Password));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
