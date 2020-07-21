using smart_modul_BACKUP.ServiceInterface;

using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smart_modul_BACKUP.WCF
{
    /// <summary>
    /// Metody, které volá služba na GUI.
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false, IncludeExceptionDetailInFaults = true)]
    class SmartModulBackupCallbackHandler : ISmartModulBackupInterfaceCallback
    {
        public event Action OnServiceDisconnected;
        public SmartModulBackupInterfaceClient client;

        private InProgress inProgress => Manager.Get<InProgress>();
        private BackupInfoManager backups => Manager.Get<BackupInfoManager>();

        /// <summary>
        /// Služba pravidelně bude na klientovi volat tuto metodu, aby si ověřila, že klient je stále připojen.
        /// </summary>
        public void TestConnection()
        {
            try
            {
                client.ImStillHere();
            }
            catch (Exception e)
            {
                SmbLog.Error($"Chyba v TestConnection()", e, LogCategory.GuiServiceClient);
            }
        }

        /// <summary>
        /// Zobrazí bublinu s chybovou hláškou.
        /// </summary>
        /// <param name="error"></param>
        public void ShowError(string error)
        {
            Manager.Get<NotifyIcon>()?.ShowBalloonTip(2000, "Chyba", error, ToolTipIcon.Error);
        }

        [Obsolete("Nedělá nic.")]
        public void ShowMsg(string msg)
        {
            //MessageBox.Show(msg);
        }

        /// <summary>
        /// Zavolá událost OnServiceDisconnected.
        /// </summary>
        public void Goodbye()
        {
            try
            {
                OnServiceDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                SmbLog.Error("Problém v komunikaci GUI se službou (na straně GUI)", ex, LogCategory.GuiServiceClient);
            }
        }

        /// <summary>
        /// Informuje GUI o spuštěné obnově a nastaví daný RestoreInProgress jako objekt, skrze nějž
        /// lze předávat informace o průběho dané obnovy.
        /// </summary>
        /// <param name="progress"></param>
        public void StartRestore(RestoreInProgress progress)
        {
            try
            {
                if (progress == null)
                {
                    Bubble.Show("???");
                    return;
                }

                inProgress.SetRestore(progress);
                Bubble.Show("Obnova započata");
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Informuje GUI o spuštěné záloze a nastaví daný BackupInProgress jako objekt, skrze nějž
        /// lze předávat informace o průběho dané zálohy.
        /// </summary>
        /// <param name="progress"></param>
        public void StartBackup(BackupInProgress progress)
        {
            try
            {
                if (progress == null)
                {
                    Bubble.Show("???");
                    return;
                }

                inProgress.SetBackup(progress);
                Bubble.Show($"Záloha dle pravidla {progress.RuleName} započata");
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Updatuje informace o průběhu obnovy.
        /// </summary>
        /// <param name="progress"></param>
        public void UpdateRestore(RestoreInProgress progress)
        {
            try
            {
                if (progress == null)
                {
                    Bubble.Show("???");
                    return;
                }

                inProgress.SetRestore(progress);
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Updatuje informace o průběhu zálohy.
        /// </summary>
        /// <param name="progress"></param>
        public void UpdateBackup(BackupInProgress progress)
        {
            try
            {
                if (progress == null)
                {
                    Bubble.Show("???");
                    return;
                }

                inProgress.SetBackup(progress);
            }
            catch(Exception ex)
            {

            }
        }

        /// <summary>
        /// Informuje GUI o tom, že obnova je dokončena.
        /// </summary>
        /// <param name="restore"></param>
        /// <param name="response"></param>
        public void CompleteRestore(RestoreInProgress restore, RestoreResponse response)
        {
            try
            {
                if (restore == null)
                {
                    Bubble.Show("???");
                    return;
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    inProgress.GetRestore(restore.ProgressId).Complete();
                    inProgress.RemoveRestore(restore.ProgressId);
                    //LoadedStatic.LoadSavedBackups();
                    // SEM SE VRÁTIT!
                });

                switch (response.Success)
                {
                    case SuccessLevel.EverythingWorked:
                        Bubble.Show($"Obnova proběhla úspěšně");
                        break;
                    case SuccessLevel.SomeErrors:
                        Bubble.Show($"Obnova byla dokončena s chybami.", icon: ToolTipIcon.Warning);
                        break;
                    case SuccessLevel.TotalFailure:
                        Bubble.Show($"Obnova se nepovedla.", icon: ToolTipIcon.Error);
                        break;
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Informuje GUI o tom, že záloha je dokončena.
        /// </summary>
        /// <param name="backup"></param>
        /// <param name="BackupID"></param>
        public async void CompleteBackup(BackupInProgress backup, int BackupID)
        {
            try
            {
                if (backup == null)
                {
                    Bubble.Show("???");
                    return;
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    inProgress.GetBackup(backup.ProgressId).Complete();
                    inProgress.RemoveBackup(backup.ProgressId);
                });

                await backups.LoadAsync();

                var bak = backups.Backups.FirstOrDefault(f => f.LocalID == BackupID);
                if (bak == null)
                    Bubble.Show("Záloha prý proběhla, ale nebylo o ní nalezeno info.", icon: ToolTipIcon.Error);
                else
                {
                    switch (bak.SuccessLevel)
                    {
                        case SuccessLevel.EverythingWorked:
                            Bubble.Show($"Záloha pravidla {bak.RefRuleName} proběhla úspěšně");
                            break;
                        case SuccessLevel.SomeErrors:
                            Bubble.Show($"Záloha pravidla {bak.RefRuleName} dokončena s chybami.", icon: ToolTipIcon.Warning);
                            break;
                        case SuccessLevel.TotalFailure:
                            Bubble.Show($"Záloha pravidla {bak.RefRuleName} se nepovedla.", icon: ToolTipIcon.Error);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            
        }

        //public void SetProgress(int bar_id, string message, float progress)
        //{
        //    throw new NotImplementedException();
        //}

        //public void RemoveProgressBar(int bar_id)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
