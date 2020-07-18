using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Načítá seznam položek z xml souboru, umožňuje s těmito položkami pracovat
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Používá se BackupInfoManager a BackupRuleLoader")]
    public class XmlInfoLoader<T> where T : IHaveID
    {
        public string LocalPath { get; private set; }
        public XmlInfoLoader(string fname) => LocalPath = fname;

        private List<T> _infos = new List<T>();

        public event EventHandler<InfoSaveEventArgs<T>> OnInfosSaved;
        public event EventHandler<InfoLoadEventArgs<T>> OnBeforeInfosLoaded;

        public bool UseCounterFile = false;
        public string CounterFile = "";

        /// <summary>
        /// Nastaví, aby tento loader používal daný soubor k určování id
        /// </summary>
        /// <param name="file"></param>
        public void SetCounterFile(string file)
        {
            UseCounterFile = true;
            CounterFile = file;
        }

        public T[] GetInfos()
        { 
            lock(_infos)
                return _infos.ToArray();
        }

        private int nextId
        {
            get
            {
                if (!UseCounterFile)
                    lock (_infos)
                        return allIds.Any() ? allIds.Max() + 1 : 0;
                else
                    using (var reader = new StreamReader(CounterFile))
                        return int.Parse(reader.ReadToEnd()) + 1;
            }
        }

        private IEnumerable<int> allIds => _infos.Select(f => f.GetID()).Union(reservedIds);
        private List<int> reservedIds = new List<int>();

        public virtual int ReserveId()
        {
            lock (_infos)
            {
                int id = nextId;

                if (UseCounterFile)
                    using (var writer = new StreamWriter(CounterFile, false))
                        writer.Write(id.ToString());

                reservedIds.Add(id);
                return id;
            }
        }

        public virtual void ClearInfos()
        {
            _infos.Clear();
        }

        public async Task LoadInfosAsync() => await Task.Run(() => LoadInfos());

        public virtual void LoadInfos() => _loadInfos(append: false, xml: null);

        protected void _loadInfos(bool append = false, string xml = null)
        {
            lock (_infos)
            {
                if (!append)
                    _infos.Clear();

                OnBeforeInfosLoaded?.Invoke(this, new InfoLoadEventArgs<T>() { FilePath = LocalPath });

                if (xml == null && !File.Exists(LocalPath))
                {
                    File.Create(LocalPath).Close();
                    return;
                }
                else
                {
                    xml = xml ?? File.ReadAllText(LocalPath);
                    if (xml.Trim() == "")
                        return;

                    XmlSerializer xmlser = new XmlSerializer(typeof(T[]));
                    foreach (T i in xmlser.Deserialize(new StringReader(xml)) as T[])
                        if (!_infos.Any(f => f.GetID() == i.GetID()))
                            _infos.Add(i);
                }
            }
        }

        Task _flusher = null;

        public virtual void SaveInfos()
        {
            //pokud už ukládání informací probíhá, počkáme, až bude hotovo
            _flusher?.Wait();

            //na novém vlákně
            _flusher = Task.Run(() =>
            {
                //serializujeme seznam informací do daného souboru
                XmlSerializer xmlser = new XmlSerializer(typeof(List<T>));
                using (var writer = new StreamWriter(LocalPath, false))
                    lock (_infos)
                        xmlser.Serialize(writer, _infos);

                //invokujeme událost, že jsme informace uložili
                OnInfosSaved?.Invoke(this, new InfoSaveEventArgs<T>()
                {
                    FilePath = LocalPath,
                    Infos = _infos.ToArray()
                });
            });
        }

        public async Task SaveInfosAsync() => await Task.Run(() => SaveInfos());

        public virtual void AddInfo(T info)
        {
            lock (_infos)
                _infos.Add(info);

            lock (reservedIds)
                if (reservedIds.Contains(info.GetID()))
                    reservedIds.Remove(info.GetID());
        }

        public virtual void RemoveInfos(Func<T, bool> func)
        {
            lock (_infos)
                _infos.RemoveAll(f => func(f));
        }

        public virtual void RemoveInfo(T info)
        {
            lock (_infos)
                _infos.Remove(info);
        }
    }

    public class InfoSaveEventArgs<T>
    {
        /// <summary>
        /// Informace, které se uložily
        /// </summary>
        public T[] Infos;

        /// <summary>
        /// Cesta k souboru, kam se informace uložily
        /// </summary>
        public string FilePath;
    }

    public class InfoLoadEventArgs<T>
    {
        /// <summary>
        /// Cesta k souboru, odkud se budou informace načítat
        /// </summary>
        public string FilePath;
    }
}
