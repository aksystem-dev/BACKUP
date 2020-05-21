//using smart_modul_BACKUP_service.WCF;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace smart_modul_BACKUP_service
//{
//    /// <summary>
//    /// Umožňuje updatovat progress bar v GUI.
//    /// </summary>
//    public class GuiProgressBar
//    {
//        public int ID;
//        public string StateMessage;
//        public float Progress;

//        private ISmartModulBackupInterfaceCallback _callback;

//        public GuiProgressBar(ISmartModulBackupInterfaceCallback callback)
//        {
//            _callback = callback;
//        }

//        public void Update(string stateMessage = null, float? _progress = null)
//        {
//            try
//            {
//                Logger.Log("ProgressBar.Update");

//                StateMessage = stateMessage ?? StateMessage;
//                Progress = _progress ?? Progress;
//                _callback.SetProgress(ID, StateMessage, Progress);
//            }
//            catch (Exception e)
//            {
//                Logger.Log($"ProgressBar.Update - Výjimka ({e.GetType().Name})\n{e.Message}");
//            }
//        }

//        public void Remove()
//        {
//            try
//            {
//                Logger.Log("ProgressBar.Update");

//                _callback.RemoveProgressBar(ID);
//            }
//            catch (Exception e)
//            {
//                Logger.Log($"ProgressBar.Update - Výjimka ({e.GetType().Name})\n{e.Message}");
//            }
//        }
//    }
//}
