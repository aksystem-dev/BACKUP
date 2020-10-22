using Renci.SshNet;
using SmartModulBackupClasses.Managers;
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
        public ObservableCollection<RestoreInProgress> InProgress { get; private set; }
            = new ObservableCollection<RestoreInProgress>();

        [Obsolete("Mělo by být pouze používáno uvnitř třídy Backup / XmlSerializerem. Použij Backup.New()")]
        public Backup() { }

        public static Backup New(BackupRule rule)
        {
            return new Backup()
            {
                RefRule = rule.LocalID,
                RefRuleName = rule.Name,
                BackupType = rule.RuleType,
                Errors = new List<BackupError>(),
                Sources = new List<SavedSource>(),
                Success = true,
                StartDateTime = DateTime.Now,
                IdType = SMB_Utils.ID_TYPE_TO_USE,
                ComputerId = SMB_Utils.GetComputerId(),
                Saved = false,
                IsZip = rule.Zip,
                SftpHash = rule.RemoteBackups.enabled ? SMB_Utils.GetSftpHash() : null,
                PlanId = SMB_Utils.GetCurrentPlanId()
            };
        }

        public static Backup New(BackupRule rule, Action<Backup> setters)
        {
            var bk = New(rule);
            setters(bk);
            return bk;
        }


        /// <summary>
        /// typ identifikace počítače (jakého typu je ComputerId)
        /// </summary>
        public ClientIdType IdType { get; set; } = ClientIdType.WindowsKey;

        /// <summary>
        /// Id počítače, na kterém byla záloha vytvořena.
        /// </summary>
        public string ComputerId { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public int GetID() => LocalID;

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
        /// Jestli byla zálohy vytvořena na tomto PC.
        /// </summary>
        [XmlIgnore]
        public bool MadeOnThisComputer => ComputerId == SMB_Utils.GetComputerId(IdType);

        /// <summary>
        /// Jestli je záloha dostupná na tomto počítači (to znamená, že jsme na počítači, na kterém byla záloha vytvořena)
        /// </summary>
        [XmlIgnore]
        public bool AvailableOnThisComputer => AvailableLocally && MadeOnThisComputer;

        /// <summary>
        /// Jestli byla záloha nahrána na aktuální SFTP server.
        /// </summary>
        [XmlIgnore]
        public bool UploadedToCurrentSftpServer => SftpHash == SMB_Utils.GetSftpHash();

        /// <summary>
        /// Zdali je záloha dostupná na aktuálním SFTP serveru.
        /// </summary>
        [XmlIgnore]
        public bool AvailableOnCurrentSftpServer => (UploadedToCurrentSftpServer && AvailableRemotely) || (SftpHash == null && AvailableRemotely);

        /// <summary>
        /// Jestli byla záloha vytvořena k aktuálnímu plánu.
        /// </summary>
        [XmlIgnore]
        public bool UploadedToCurrentPlan => PlanId != -1 && PlanId == SMB_Utils.GetCurrentPlanId();

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
            set => _remotePath = value.NormalizePath();
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

        private bool _isZip = true;

        /// <summary>
        /// Zdalipak je záloha uložena jako zip (true) nebo jako normální složka (false);
        /// </summary>
        public bool IsZip
        {
            get
            {
                if (BackupType == BackupRuleType.OneToOne)
                    return false;
                return _isZip;
            }

            set => _isZip = value;
        }

        public BackupRuleType BackupType { get; set; }

        public OneToOneBackupStatus OneToOneStatus { get; set; }

        /// <summary>
        /// Hash připojení na SFTP, kam byla tato záloha nahrána.
        /// </summary>
        public string SftpHash { get; set; }

        /// <summary>
        /// Pokud byla tato záloha nahrána na webové api, zde bude uložen id plánu, na který byla nahrána.
        /// Pokud záloha byla vytvořena offline, bude zde -1.
        /// </summary>
        [DefaultValue(-1)]
        public int PlanId { get; set; }

        public string ToXml()
        {
            var serializer = new XmlSerializer(typeof(Backup));
            using(var writer = new StringWriter())
            {
                serializer.Serialize(writer, this);
                return writer.ToString();
            }
        }

        /// <summary>
        /// deserializuje info o záloze z xml
        /// </summary>
        /// <param name="xml">samotné xml</param>
        /// <param name="fname">cesta lokálního souboru, odkud bylo xml získáno; pokud nebylo získáno z lokálního souboru, nechat null</param>
        /// <returns></returns>
        public static Backup DeXml(string xml, string fname)
        {
            var serializer = new XmlSerializer(typeof(Backup));
            using (var reader = new StringReader(xml))
            {
                var obj = serializer.Deserialize(reader) as Backup;
                obj._filename = fname;
                return obj;
            }
        }

        /// <summary>
        /// pokud bylo info načteno z lokálního souboru, zde bude cesta k onomu soubru
        /// </summary>
        [XmlIgnore]
        public string _filename = null;

        /// <summary>
        /// Vrátí název souboru, pod kterým by se toto mělo uložit
        /// </summary>
        /// <param name="bk"></param>
        /// <returns></returns>
        public string BkInfoNameStr(bool includeComputerID = true)
        {
            //pokud toto bylo načteno z lokálního souboru, vrátit cestu k němu
            if (_filename != null)
                return _filename;

            //jinak vygenerovat nové jméno
            if (includeComputerID)
                return this.RefRuleName + "_" + this.EndDateTime.ToString("dd-MM-yyyy") + "_" + this.LocalID + "_" + this.ComputerId + ".xml";
            else
                return this.RefRuleName + "_" + this.EndDateTime.ToString("dd-MM-yyyy") + "_" + this.LocalID + ".xml";
        }
    }

    public enum SuccessLevel { TotalFailure, SomeErrors, EverythingWorked }
}
