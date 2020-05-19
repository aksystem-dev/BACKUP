using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Informace o proběhnuté záloze.
    /// </summary>
    public class Backup : IHaveID, INotifyPropertyChanged
    {
        public Backup()
        {
            
        }

        /// <summary>
        /// Id počítače, na kterém byla záloha vytvořena.
        /// </summary>
        public string ComputerId { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public int GetID() => ID;

        /// <summary>
        /// ID zálohy
        /// </summary>
        public int ID { get; set; }

        private bool _availableLocally;

        /// <summary>
        /// Jestli je záloha dostupná na počítači, na kterém byla vytvořena.
        /// </summary>
        public bool AvailableLocally
        {
            get => _availableLocally;
            set
            {
                _availableLocally = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableLocally)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableOnThisComputer)));
            }
        }

        /// <summary>
        /// Jestli je záloha dostupná na tomto počítači (to znamená, že jsme na počítači, na kterém byla záloha vytvořena)
        /// </summary>
        [XmlIgnore]
        public bool AvailableOnThisComputer => AvailableLocally && ComputerId == SMB_Utils.GetComputerId();

        private bool _availableRemotely;
        /// <summary>
        /// Jestli je záloha dostupná na serveru
        /// </summary>
        public bool AvailableRemotely
        {
            get => _availableRemotely;
            set
            {
                _availableRemotely = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableRemotely)));
            }
        }

        /// <summary>
        /// Cesta k lokální záloze na počítači, na kterém byla vytvořena.
        /// </summary>
        public string LocalPath { get; set; }
        
        /// <summary>
        /// Cesta k záloze na serveru.
        /// </summary>
        public string RemotePath { get; set; }

        /// <summary>
        /// ID pravidla, podle nějž byla záloha vytvořena.
        /// </summary>
        public int RefRule { get; set; }

        /// <summary>
        /// Jméno pravidla, podle nějž byla záloha vytvořena.
        /// </summary>
        public string RefRuleName { get; set; }

        /// <summary>
        /// Velikost zálohy v bytech.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Seznam zdrojů zálohy.
        /// </summary>
        public List<SavedSource> Sources { get; set; }

        /// <summary>
        /// Zdali byla záloha úspěšná.
        /// </summary>
        public bool Success { get; set; }

        [XmlIgnore]
        public BackupSuccessLevel SuccessLevel
        {
            get
            {
                if (Success) return Sources.All(f => f.Success == BackupSuccessLevel.EverythingWorked) ? BackupSuccessLevel.EverythingWorked : BackupSuccessLevel.SomeErrors;
                return Sources.Any() ? BackupSuccessLevel.SomeErrors : BackupSuccessLevel.TotalFailure;
            }
        }

        /// <summary>
        /// Seznam chyb při záloze.
        /// </summary>
        public List<BackupError> Errors { get; set; }

        /// <summary>
        /// Datum a čas, kdy byla záloha započata.
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Datum a čas, kdy byla záloha dokončena.
        /// </summary>
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// Pokud tato záloha tvrdí, že je dostupná lokálně, tato metoda zkontroluje, jestli tomu tak skutečně je (ověří, 
        /// zdali existuje lokální soubor zálohy). Podle toho poté nastaví AvailableLocally a stejnou hodnotu vrátí.
        /// </summary>
        /// <returns></returns>
        public bool CheckLocalAvailibility()
        {
            if (!AvailableLocally)
                return false;

            AvailableLocally = File.Exists(LocalPath);
            return AvailableLocally;
        }

        public bool CheckRemoteAvailability(SftpClient client)
        {
            if (!AvailableRemotely)
                return false;

            var close = !client.IsConnected;
            if (close)
                client.Connect();

            AvailableRemotely = client.Exists(RemotePath.FixPathForSFTP());

            if (close)
                client.Disconnect();
            return AvailableRemotely;

        }
    }

    public enum BackupSuccessLevel { TotalFailure, SomeErrors, EverythingWorked }
}
