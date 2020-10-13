using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    [Obsolete]
    interface IInfoLoader<T>
    {
        T[] GetInfos();
        int ReserveId();
        void LoadInfos();
        void SaveInfos();
        void AddInfo(T info);
        void RemoveInfos(Func<T, bool> func);
    }
}
