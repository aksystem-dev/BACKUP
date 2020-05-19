﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.IO;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Umí zálohovat a obnovovat zálohy SQL serveru.
    /// </summary>
    public class SqlBackuper
    {
        public SqlConnection connection { get; private set; }

        public SqlBackuper(string connstr)
        {
            connection = new SqlConnection(connstr);
        }

        //public bool Open(bool catchError = true)
        //{
        //    if (catchError)
        //        try
        //        {
        //            connection.Open();
        //            return true;
        //        }
        //        catch (Exception e)
        //        {
        //            Logger.Error($"Nepodařilo se připojit k SQL serveru \n {e.Message}");
        //            return false;
        //        }
        //    else
        //    {
        //        connection.Open();
        //        return true;
        //    }
        //}

        public void Open() => connection.Open();

        public bool Close(bool catchError = true)
        {
            if (catchError)
                try
                {
                    connection.Close();
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error($"Nepodařilo se odpojit od SQL serveru \n {e.Message}");
                    return false;
                }
            else
            {
                connection.Close();
                return true;
            }
        }


        /// <summary>
        /// Pokusí se zálohovat SQL databázi a vrátí, zdali to bylo úspěšné.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public void Backup(string database, string path)
        {
            Logger.Log($"Zálohuji databázi {database} do {path}");

            //sql musíme poslat absolutní adresu, takle se ujistíme, že je absolutní:
            path = Path.GetFullPath(path);

            var com = new SqlCommand(Properties.Resources.SqlBackup, connection);
            com.Parameters.AddWithValue("@database", database);
            com.Parameters.AddWithValue("@path", path);
            
            com.ExecuteNonQuery();
            File.SetLastWriteTime(path, DateTime.Now);

            Logger.Log($"Záloha databáze {database} úspěšně vytvořena");
        }

        /// <summary>
        /// Pokusí se provést RESTORE databáze z daného souboru.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="path"></param>
        public void Restore(string database, string path)
        {
            Logger.Log($"Obnovuji databázi {database} z {path}");

            path = Path.GetFullPath(path);

            SqlCommand com;

            //tento úsek kódu není třeba provádět v případě master databáze
            if (database != "master")
            {
                //použít master databázi
                com = new SqlCommand(Properties.Resources.SqlUseMaster, connection);
                com.ExecuteNonQuery();
                com.Dispose();

                //zjistit názvy databáze
                com = new SqlCommand("SELECT name FROM sys.databases", connection);
                var reader = com.ExecuteReader();
                //zjistit, jestli je mezi nimi naše databáze
                bool foundOurDb = false;
                while (reader.Read())
                {
                    if (reader[0].ToString() == database)
                    {
                        foundOurDb = true;
                        break;
                    }
                }
                reader.Close();
                com.Dispose();

                //pokud naše databáze existuje
                if (foundOurDb)
                {
                    //zařídit, abychom byli jediní připojeni k naší databázi
                    com = new SqlCommand(Properties.Resources.SqlSingleUser.Replace("@database", database), connection);
                    com.ExecuteNonQuery();
                    com.Dispose();
                }

            }

            //provést restore
            com = new SqlCommand(Properties.Resources.SqlRestore, connection);
            com.Parameters.AddWithValue("@database", database);
            com.Parameters.AddWithValue("@path", path);

            com.ExecuteNonQuery();

            com.Dispose();

            Logger.Log($"Databáze {database} úspěšně obnovena");
        }
    }
}
