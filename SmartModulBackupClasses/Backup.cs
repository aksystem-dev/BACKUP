using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        /// <summary>
        /// Jestli info o záloze již bylo uloženo.
        /// </summary>
        public bool Saved { get; set; }

        /// <summary>
        /// Sem si můžeme uložit nějaká pomocná data.
        /// </summary>
        [XmlIgnore]
        public object TAG { get; set; }

        [XmlIgnore]
        public ObservableCollection<RestoreInProgress> InProgress { get; set; }
            = new ObservableCollection<RestoreInProgress>();

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
        /// Lokální ID zálohy
        /// </summary>
        [Obsolete("Je tu pouze kvůli zpětné kompatibilitě; použij LocalID")]
        public int ID { get => LocalID; set => LocalID = value; }

        /// <summary>
        /// ID nechceme serializovat, ale chceme ho deserializovat (původně se to jmenovalo ID,
        /// ale chci to přejmenovat na LocalID, ať to dává větší smysl, neb ID má být unikátní v webové db,
        /// zatímco LocalID má být unikátní na jednom klientovi.)
        /// </summary>
        /// <returns></returns>
        public bool ShouldSerializeID() => false;

        /// <summary>
        /// Lokální ID zálohy
        /// </summary>
        public int LocalID { get; set; }

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

        public bool DoesLocalFileExist()
        {
            if (IsZip)
                return File.Exists(LocalPath);
            else
                return Directory.Exists(LocalPath);
        }

        public bool DoesRemoteFileExist(SftpClient client)
        {
            if (!client.Exists(RemotePath))
                return false;

            var file = client.Get(RemotePath);

            if (IsZip)
                return file.IsRegularFile;
            else
                return file.IsDirectory;
        }

        /// <summary>
        /// Jestli je záloha dostupná na tomto počítači (to znamená, že jsme na počítači, na kterém byla záloha vytvořena)
        /// </summary>
        [XmlIgnore]
        public bool AvailableOnThisComputer => AvailableLocally && MadeOnThisComputer;

        [XmlIgnore]
        public bool MadeOnThisComputer => ComputerId == SMB_Utils.GetComputerId();

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

        private string _remotePath;
        /// <summary>
        /// Cesta k záloze na serveru.
        /// </summary>
        public string RemotePath
        {
            get => _remotePath;
            set => _remotePath = value.FixPathForSFTP();
        }

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
        public SuccessLevel SuccessLevel
        {
            get
            {
                if (Success) return Sources.All(f => f?.Success == SuccessLevel.EverythingWorked) ? SuccessLevel.EverythingWorked : SuccessLevel.SomeErrors;
                return Sources.Any() ? SuccessLevel.SomeErrors : SuccessLevel.TotalFailure;
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
        /// Zdalipak je záloha uložena jako zip (true) nebo jako normální složka (false);
        /// </summary>
        public bool IsZip { get; set; } = true;

        public BackupRuleType BackupType { get; set; }

        public OneToOneBackupStatus OneToOneStatus { get; set; }

        public string ToXml()
        {
            var serializer = new XmlSerializer(typeof(Backup));
            using(var writer = new StringWriter())
            {
                serializer.Serialize(writer, this);
                return writer.ToString();
            }
        }

        public static Backup DeXml(string xml)
        {
            var serializer = new XmlSerializer(typeof(Backup));
            using(var reader = new StringReader(xml))
                return serializer.Deserialize(reader) as Backup;            
        }
    }

    public enum SuccessLevel { TotalFailure, SomeErrors, EverythingWorked }
}
