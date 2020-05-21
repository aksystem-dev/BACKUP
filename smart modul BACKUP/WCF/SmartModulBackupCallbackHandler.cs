using smart_modul_BACKUP.ServiceInterface;

using SmartModulBackupClasses;
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
    /// Zde se zpracovávají zprávy ze služby.
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false, IncludeExceptionDetailInFaults = true)]
    class SmartModulBackupCallbackHandler : ISmartModulBackupInterfaceCallback
    {
        public event Action OnServiceDisconnected;
        public SmartModulBackupInterfaceClient client;

        public void TestConnection()
        {
            try
            {
                client.ImStillHere();
            }
            catch (Exception e)
            {
                GuiLog.Log($"{e.GetType().Name}\n{e.Message}");
            }
        }

        public void ShowError(string error)
        {
            LoadedStatic.notifyIcon?.ShowBalloonTip(2000, "Chyba", error, ToolTipIcon.Error);
        }

        public void ShowMsg(string msg)
        {
            //MessageBox.Show(msg);
        }

        public void Goodbye()
        {
            OnServiceDisconnected?.Invoke();
        }

        public void StartRestore(RestoreInProgress progress)
        {
            try
            {
                if (progress == null)
                {
                    LoadedStatic.MSG("???");
                    return;
                }

                LoadedStatic.InProgress.SetRestore(progress);
                LoadedStatic.MSG("Obnova započata");
            }
            catch (Exception ex)
            {
            }
        }

        public void StartBackup(BackupInProgress progress)
        {
            try
            {
                if (progress == null)
                {
                    LoadedStatic.MSG("???");
                    return;
                }

                LoadedStatic.InProgress.SetBackup(progress);
                LoadedStatic.MSG($"Záloha dle pravidla {progress.RuleName} započata");
            }
            catch (Exception ex)
            {

            }
        }

        public void UpdateRestore(RestoreInProgress progress)
        {
            try
            {
                if (progress == null)
                {
                    LoadedStatic.MSG("???");
                    return;
                }

                LoadedStatic.InProgress.SetRestore(progress);
            }
            catch (Exception ex)
            {

            }
        }

        public void UpdateBackup(BackupInProgress progress)
        {
            try
            {
                if (progress == null)
                {
                    LoadedStatic.MSG("???");
                    return;
                }

                LoadedStatic.InProgress.SetBackup(progress);
            }
            catch(Exception ex)
            {

            }
        }

        public void CompleteRestore(RestoreInProgress restore, RestoreResponse response)
        {
            try
            {
                if (restore == null)
                {
                    LoadedStatic.MSG("???");
                    return;
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    LoadedStatic.InProgress.GetRestore(restore.ProgressId).Complete();
                    LoadedStatic.InProgress.RemoveRestore(restore.ProgressId);
                    LoadedStatic.LoadSavedBackups();
                });

                switch (response.Success)
                {
                    case SuccessLevel.EverythingWorked:
                        LoadedStatic.MSG($"Obnova proběhla úspěšně");
                        break;
                    case SuccessLevel.SomeErrors:
                        LoadedStatic.MSG($"Obnova byla dokončena s chybami.", icon: ToolTipIcon.Warning);
                        break;
                    case SuccessLevel.TotalFailure:
                        LoadedStatic.MSG($"Obnova se nepovedla.", icon: ToolTipIcon.Error);
                        break;
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void CompleteBackup(BackupInProgress backup, int BackupID)
        {
            try
            {
                if (backup == null)
                {
                    LoadedStatic.MSG("???");
                    return;
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    LoadedStatic.InProgress.GetBackup(backup.ProgressId).Complete();
                    LoadedStatic.InProgress.RemoveBackup(backup.ProgressId);
                    LoadedStatic.LoadSavedBackups();
                });

                var bak = LoadedStatic.SavedBackups.FirstOrDefault(f => f.ID == BackupID);
                if (bak == null)
                    LoadedStatic.MSG("Záloha prý proběhla, ale nebylo o ní nalezeno info.", icon: ToolTipIcon.Error);
                else
                {
                    switch (bak.SuccessLevel)
                    {
                        case SuccessLevel.EverythingWorked:
                            LoadedStatic.MSG($"Záloha pravidla {bak.RefRuleName} proběhla úspěšně");
                            break;
                        case SuccessLevel.SomeErrors:
                            LoadedStatic.MSG($"Záloha pravidla {bak.RefRuleName} dokončena s chybami.", icon: ToolTipIcon.Warning);
                            break;
                        case SuccessLevel.TotalFailure:
                            LoadedStatic.MSG($"Záloha pravidla {bak.RefRuleName} se nepovedla.", icon: ToolTipIcon.Error);
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
