using Alphaleonis.Win32.Vss;
using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Třída sloužící k jednorázovému vytvoření Shadow Copy.
    /// </summary>
    class VssBackuper : IDisposable
    {
        /// <summary>
        /// Jestli už bylo zavoláno DoBackup. Na jednom objektu VssBackuper to může být zavoláno jen jednou.
        /// </summary>
        public bool Used { get; private set; } = false;

        /// <summary>
        /// Informace o snímku.
        /// </summary>
        public VssSnapshotProperties SnapshotProperties { get; private set; } = null;

        public string Root { get; private set; }
        public bool Success { get; private set; }

        private IVssBackupComponents backup = null;
        private Guid setId;
        private Guid snapId;

        /// <summary>
        /// Vytvoří Shadow Copy.
        /// </summary>
        /// <param name="root">Disk pro zálohu</param>
        /// <returns>Zdali byla operace úspěšná.</returns>
        public bool DoBackup(string root, List<BackupError> errors = null)
        {
            if (Used)
                throw new InvalidOperationException("Tento objekt VssBackuper už byl použit. Vytvořte nový objekt.");

            Used = true;
            Root = Path.GetPathRoot(root);
            var vss = VssFactoryProvider.Default.GetVssFactory();
            Console.WriteLine("IVssFactory načteno");

            try
            {
                //viz https://docs.microsoft.com/en-us/windows/win32/vss/overview-of-processing-a-backup-under-vss

                backup = vss.CreateVssBackupComponents();

                backup.InitializeForBackup(null);
                backup.GatherWriterMetadata();
                backup.FreeWriterMetadata();
                backup.SetContext(VssSnapshotContext.Backup);

                setId = backup.StartSnapshotSet();

                if (!backup.IsVolumeSupported(Root))
                {
                    Logger.Error($"Svazek {Root} není podporován Shadow Copy.");

                    errors?.Add(new BackupError(
                        $"Svazek {Root} není podporován Shadow Copy.",
                        BackupErrorType.ShadowCopyError
                        ));

                    Success = false;
                    return false;
                }
                snapId = backup.AddToSnapshotSet(Root);

                backup.SetBackupState(false, true, VssBackupType.Full, false);
                backup.PrepareForBackup();
                backup.GatherWriterStatus();
                backup.DoSnapshotSet();
                backup.GatherWriterStatus();

                SnapshotProperties = backup.GetSnapshotProperties(snapId);

                Logger.Log($"Shadow Copy úspěšně vytvořena, dostupná na {SnapshotProperties.SnapshotDeviceObject}");
            }
            catch (Exception e)
            {
                Logger.Error($"Problém s Shadow Copy: {e.GetType().Name}\n\n{e.Message}");

                errors?.Add(new BackupError(
                    $"Problém s Shadow Copy: {e.GetType().Name}\n\n{e.Message}",
                    BackupErrorType.ShadowCopyError
                    ));

                try
                {
                    //pokud se Shadow Copy nepovedlo, pokusíme se ho zrušit
                    //  (jinak by si Shadow Copy služba mohla myslet, že copy ještě probíhá,
                    //  a to by pak blokovalo další volání této služby)
                    backup.AbortBackup();
                }
                catch (Exception ee)
                {
                    Logger.Error($"Nepodařilo se ani zrušit zálohu... {ee.GetType().Name}\n\n{e.Message}");

                    errors?.Add(new BackupError(
                        $"Nepodařilo se ani zrušit zálohu... {ee.GetType().Name}\n\n{e.Message}",
                        BackupErrorType.ShadowCopyError
                        ));
                }
                Success = false;
                return false;
            }
            Success = true;
            return true;
        }

        /// <summary>
        /// Vrátí cestu k Shadow Copy daného souboru nebo složky
        /// </summary>
        /// <param name="normalPath">cesta k souboru nebo složce</param>
        /// <returns></returns>
        public string GetShadowPath(string normalPath)
        {
            if (SnapshotProperties != null)
                //zde vyvodíme adresu k Shadow Copy (nahrazením kořene vlastností SnapshotProperties.SnapshotDeviceObject)
                return normalPath.Replace(Path.GetPathRoot(normalPath), SnapshotProperties.SnapshotDeviceObject + '\\');
            else
                throw new InvalidOperationException(Used ? 
                    "Nelze získat cestu k Shadow Copy, poněvadž záloha se nepovedla." : 
                    "Nelze získat cestu k Shadow Copy, poněvadž záloha ještě neproběhla.");
        }

        /// <summary>
        /// Označí Shadow Copy za dokončenou a odstraní sadu snímků (BackupComplete, DeleteSnapshotSet)
        /// </summary>
        public void Dispose()
        {
            backup.BackupComplete();

            Logger.Log("Odstraňuji Shadow Copy");
            backup.DeleteSnapshotSet(setId, false);
            backup.Dispose();
        }
    }
}
