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

        public void Load()
        {
            availableDatabases.Clear();

            using (var logfile = new StreamWriter("db_load_log.txt"))
            {
                //nyní se připojíme k serveru a zjistíme, jestli jsou nějaké databáze, o kterých ještě nevíme
                using (var conn = new SqlConnection(config.Connection.GetConnectionString(1)))
                {
                    try
                    {
                        conn.Open();

                        //nejprve stáhneme názvy všech databází
                        SqlCommand com = new SqlCommand("USE master; SELECT name FROM sys.databases", conn);
                        logfile.WriteLine($"posílám sql příkaz \"{com.CommandText}\"");

                        var reader = com.ExecuteReader();
                        while (reader.Read())
                            //nechceme databázi tempdb, páč tu nelze zálohovat
                            if (((string)reader[0]).ToLower() != "tempdb")
                            {
                                string dbname = reader[0] as string;

                                logfile.WriteLine($"načtena databáze {dbname}");

                                if (!availableDatabases.Any(f => f.name == dbname))
                                    availableDatabases.Add(new AvailableDatabase() { firma = null, name = dbname });
                            }
                        reader.Close();

                        logfile.WriteLine("Všechny načtené databáze:");
                        foreach (var d in availableDatabases)
                            logfile.WriteLine($"    - {d.name}");

                        com.Dispose(); //vyplivnout objekt SqlCommand

                        //pokud StwPh_sys neexistuje, jsme hotovi
                        if (!availableDatabases.Any(f => f.name.ToLower() == "stwph_sys"))
                        {
                            logfile.WriteLine("nevidím databázi StwPh_sys, nebudu se v ní tedy hrabat");
                            return;
                        }

                        //pokud StwPh_sys existuje, projdeme jí a načteme firmy přidružené k jednotlivým databázím
                        com = new SqlCommand("USE StwPh_sys; SELECT Soubor, Firma FROM Firma", conn);
                        logfile.WriteLine($"posílám sql příkaz \"{com.CommandText}\"");
                        reader = com.ExecuteReader();

                        logfile.WriteLine("příkaz spuštěn");

                        while (reader.Read())
                        {
                            logfile.WriteLine($"řádek v StwPh_sys");

                            string dbname = reader[0] as string;
                            string firma = reader[1] as string;

                            logfile.WriteLine($"zjištěno, že databáze {dbname} patří k firmě {firma}");

                            var corresponding = availableDatabases.Where(f => f.name.ToLower() == dbname.ToLower());

                            if (!corresponding.Any())
                                logfile.WriteLine($"v seznamu načtených db nenalezena databáze {dbname}");
                            else
                                logfile.WriteLine($"načítám názvy firem");

                            corresponding.ForEach(f => f.firma = firma);
                        }

                        reader.Close();

                        conn.Close();
                    }
                    catch (Exception e)
                    {
                        logfile.WriteLine($"\n!!!!!!!\n{e.GetType().Name}\n{e.Message}\n");
                    }
                }
            }
        }
    }
}
