using smart_modul_BACKUP.Models;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace smart_modul_BACKUP.Managers
{
    /// <summary>
    /// Načítá dostupné databáze ze SQL serveru.
    /// </summary>
    public class AvailableDbLoader
    {
        public ObservableCollection<AvailableDatabase> availableDatabases { get; set; }
            = new ObservableCollection<AvailableDatabase>();

        private Config config => Manager.Get<ConfigManager>().Config;

        private void debug(string msg)
            => SmbLog.Debug(msg, null, LogCategory.GuiAvailableDbLoad);

        public bool Loading { get; private set; } = false;

        public void Load()
        {
            if (Loading)
                return;
            Loading = true;

            //dočasný seznam pro dostupné databáze
            var adbs = new List<AvailableDatabase>();

            //nyní se připojíme k serveru a zjistíme, jestli jsou nějaké databáze, o kterých ještě nevíme
            using (var conn = new SqlConnection(config.Connection.GetConnectionString(1)))
            {
                try
                {
                    conn.Open();

                    //nejprve stáhneme názvy všech databází
                    SqlCommand com = new SqlCommand("USE master; SELECT name FROM sys.databases", conn);
                    debug($"posílám sql příkaz \"{com.CommandText}\"");

                    var reader = com.ExecuteReader();
                    while (reader.Read())
                        //nechceme databázi tempdb, páč tu nelze zálohovat
                        if (((string)reader[0]).ToLower() != "tempdb")
                        {
                            string dbname = reader[0] as string;

                            debug($"načtena databáze {dbname}");

                            if (!adbs.Any(f => f.name == dbname))
                                adbs.Add(new AvailableDatabase() { firma = null, name = dbname });
                        }
                    reader.Close();

                    debug("Všechny načtené databáze:");
                    foreach (var d in adbs)
                        debug($"    - {d.name}");

                    com.Dispose(); //vyplivnout objekt SqlCommand

                    //pokud StwPh_sys neexistuje, jsme hotovi
                    if (!adbs.Any(f => f.name.ToLower() == "stwph_sys"))
                    {
                        debug("nevidím databázi StwPh_sys, nebudu se v ní tedy hrabat");
                        return;
                    }

                    //pokud StwPh_sys existuje, projdeme jí a načteme firmy přidružené k jednotlivým databázím
                    com = new SqlCommand("USE StwPh_sys; SELECT Soubor, Firma FROM Firma", conn);
                    debug($"posílám sql příkaz \"{com.CommandText}\"");
                    reader = com.ExecuteReader();

                    debug("příkaz spuštěn");

                    while (reader.Read())
                    {
                        debug($"řádek v StwPh_sys");

                        string dbname = reader[0] as string;
                        string firma = reader[1] as string;

                        debug($"zjištěno, že databáze {dbname} patří k firmě {firma}");

                        var corresponding = adbs.Where(f => f.name.ToLower() == dbname.ToLower());

                        if (!corresponding.Any())
                            debug($"v seznamu načtených db nenalezena databáze {dbname}");
                        else
                            debug($"načítám názvy firem");

                        corresponding.ForEach(f => f.firma = firma);
                    }

                    reader.Close();

                    conn.Close();
                }
                catch (Exception e)
                {
                    debug($"\n!!!!!!!\n{e.GetType().Name}\n{e.Message}\n");
                }
                finally
                {
                    //nastavit observable collection podle tohoto
                    App.dispatch(() =>
                    {
                        availableDatabases.UpdateCollectionByCompare(adbs, (db1, db2) => db1.name == db2.name);
                        Loading = false;
                    });
                }
            }
        }
    }
}
