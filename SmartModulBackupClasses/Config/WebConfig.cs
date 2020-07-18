using SmartModulBackupClasses.WebApi;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    public class WebConfig : INotifyPropertyChanged
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

        public WebConfig()
        {
            PropertyChanged += WebConfig_PropertyChanged;
        }

        private void propChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }


        private void WebConfig_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UnsavedChanges))
                UnsavedChanges = true;
        }

        private string username = "";
        private Pwd password = new Pwd("");
        private bool online = false;
        //private PlanXml activePlan = null;

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
                    password.PropertyChanged -= WebConfig_PropertyChanged;

                if (value != null)
                    value.PropertyChanged += WebConfig_PropertyChanged;

                password = value;
                propChanged(nameof(Password));
            }
        }

        /// <summary>
        /// Pokud false, znamená to, že používáme aplikaci offline; pokud true, znamená to, že se připojujeme na API
        /// </summary>
        public bool Online
        {
            get => online;
            set
            {
                if (online == value)
                    return;

                online = value;
                propChanged(nameof(Online));
            }
        }

        //[XmlIgnore]
        //public PlanXml ActivePlan
        //{
        //    get => activePlan; 
        //    set
        //    {
        //        if (activePlan == value)
        //            return;

        //        activePlan = value;
        //        propChanged(nameof(ActivePlan));
        //    }
        //}

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
