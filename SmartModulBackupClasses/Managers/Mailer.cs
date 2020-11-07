using MailKit.Net.Smtp;
using MimeKit;
using SmartModulBackupClasses.Mails;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    /// <summary>
    /// Odesílá e-maily.
    /// </summary>
    public class Mailer
    {
        const string MAIL_QUEUE_DIR = "MailQueue";

        private EmailConfig getCfg() => Manager.Get<ConfigManager>()?.Config?.EmailConfig;

        private static void error(string err, Exception ex)
            => SmbLog.Error(err, ex, LogCategory.Emails);

        public void invokeCallback(Action<MailCallbackArgs> callback, MailCallbackArgs args)
        {
            try
            {
                callback(args);
            }
            catch { }
        }

        object mailQueueEnumerableLock = new object();

        /// <summary>
        /// Vrátí IEnumerable umožňující projít maily ve frontě k odeslání.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MailFile> EnumerateMailQueue<TKey>(Func<FileInfo, TKey> order, bool descending)
        {
            lock (mailQueueEnumerableLock)
            {

                //ujistit se, že složka existuje
                Directory.CreateDirectory(MAIL_QUEUE_DIR);

                var files = Directory.EnumerateFiles(MAIL_QUEUE_DIR);
                var orderedFiles = descending ?
                    files.OrderByDescending(fname => order(new FileInfo(fname))) :
                    files.OrderBy(fname => order(new FileInfo(fname)));

                //projít soubory
                foreach (var fname in orderedFiles)
                {
                    Mail mail = null;
                    try
                    {
                        //deserializovat
                        mail = Mail.DeXml(File.ReadAllText(fname));
                    }
                    catch { }

                    //pokud byla deserializace úspěšná, vrátit novou instanci MailFile
                    if (mail != null)
                        yield return new MailFile(mail, fname);
                }

            }
        }

        public const int MAX_PENDING_EMAILS = 20;

        private bool _isDeletingOldPendingEmails = false;

        /// <summary>
        /// Odstraní maily tak, aby jich ve frontě na odeslání zbylo
        /// nejvýše MAX_PENDING_EMAILS. Nejprve odstraňuje ty nejstarší.
        /// </summary>
        private void deleteOldPendingEmails()
        {
            if (_isDeletingOldPendingEmails)
                return;

            _isDeletingOldPendingEmails = true;

            try
            {
                //ujistit se, že složka existuje
                Directory.CreateDirectory(MAIL_QUEUE_DIR);

                //projít soubory
                foreach (var file in EnumerateMailQueue(f => f.CreationTime, true).Skip(10))
                {
                    RemoveFromQueue(file, false);
                }
            }
            finally
            {
                _isDeletingOldPendingEmails = false;
            }
        }

        /// <summary>
        /// Přidá mail do fronty na odeslání.
        /// </summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        public MailFile AddToQueue(Mail mail, bool @throw = false)
        {
            try
            {
                //ujistit se, že adresář existuje
                Directory.CreateDirectory(MAIL_QUEUE_DIR);

                string fname = Guid.NewGuid().ToString(); //vygenerovat název souboru s mailem
                string fpath = Path.GetFullPath(Path.Combine(MAIL_QUEUE_DIR, fname)); //zjistit celou cestu

                //uložit mail do souboru
                File.WriteAllText(fpath, mail.ToXml());

                //vrátit instanci MailFile odpovídající tomuto souboru
                return new MailFile(mail, fpath);
            }
            catch (Exception ex)
            {
                error("Chyba při přidávání mailu do fronty.", ex);
                if (@throw)
                    throw;
                return null;
            }
            finally
            {
                deleteOldPendingEmails();
            }
        }

        /// <summary>
        /// Odstraní mail z fronty pro odeslání
        /// </summary>
        /// <param name="mailFile"></param>
        /// <param name="throw"></param>
        /// <returns></returns>
        public bool RemoveFromQueue(MailFile mailFile, bool @throw = false)
        {
            try
            {
                //fyzicky odstranit mail ze svého umístění
                File.Delete(mailFile.FilePath);

                return true;
            }
            catch (Exception ex)
            {
                error("Problém při odstraňování e-mailu z fronty.", ex);
                if (@throw)
                    throw;
                return false;
            }
        }

        /// <summary>
        /// Převést instanci Mail na instanci MimeMessage pro použití s knihovnou MailKit
        /// </summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        private MimeMessage getMimeMsgToAll(Mail mail, EmailConfig cfg = null)
        {
            cfg = cfg ?? getCfg();

            //pokud v instanci Mail nejsou uvedeni příjemci, nastavit defaultní hodnotu z configu
            mail.ToAddresses = mail.ToAddresses ?? cfg.ToAddresses;

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("smart modul BACKUP", cfg.FromAddress));
            foreach (var to in cfg.ToAddresses)
                msg.To.Add(new MailboxAddress(to, to));
            msg.Subject = mail.Subject;
            msg.Body = new TextPart(mail.Html ? MimeKit.Text.TextFormat.Html : MimeKit.Text.TextFormat.Plain)
            {
                Text = mail.Content
            };

            return msg;

        }

        private IEnumerable<MimeMessage> getMimeMsgToEach(Mail mail, EmailConfig cfg = null)
        {
            cfg = cfg ?? getCfg();

            foreach(var to in mail.ToAddresses ?? cfg.ToAddresses)
            {
                var msg = new MimeMessage();
                msg.From.Add(new MailboxAddress("smart modul BACKUP", cfg.FromAddress));
                msg.To.Add(new MailboxAddress(to, to));
                msg.Subject = mail.Subject;
                msg.Body = new TextPart(mail.Html ? MimeKit.Text.TextFormat.Html : MimeKit.Text.TextFormat.Plain)
                {
                    Text = mail.Content
                };

                yield return msg;
            }
        }

        /// <summary>
        /// Odešle mail zvlášť každému příjemci.
        /// </summary>
        /// <param name="mail">Mail k odeslání</param>
        /// <param name="cancelToken">Žeton</param>
        /// <param name="cfg">Konfigurace pro použití. Pokud null, získá konfiguraci z aktuálního ConfigManageru.</param>
        /// <returns></returns>
        public async Task<MailCallbackArgs> SendDumbEachAsync(
            Mail mail, 
            CancellationToken cancelToken = default(CancellationToken),
            EmailConfig cfg = null,
            SmtpClient client = null)
        {
            SmbLog.Trace("SendDumbEachAsync zavoláno", null, LogCategory.Emails);

            cfg = cfg ?? getCfg();

            bool alles_gute = true;
            Dictionary<string, MailMessageCallbackArgs> successes = new Dictionary<string, MailMessageCallbackArgs>();

            //Pokud jsme nedostali klienta parametrem, vytvoříme si vlastního.
            //pokud jsme ho vytvořili, musíme potom zavolat dispose, proto new_client nastavíme na true,
            //abychom si to pamatovali.
            bool new_client = client == null;
            client = new_client ? new SmtpClient() : client;

            //pokud je v konfiguraci, že důvěřujeme všem certifikátům, nastavit podle toho callback
            if (cfg.TrustAllCertificates)
                client.ServerCertificateValidationCallback = (_1, _2, _3, _4) => true;

            Exception exception = null;
            try
            {
                if (!client.IsConnected)
                    await client.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort, true, cancelToken); //připojení
                if (!client.IsAuthenticated)
                    await client.AuthenticateAsync(cfg.FromAddress, cfg.Password.Value, cancelToken); //oveření

                //e-mail odešleme zvlášť každému příjemci.
                foreach (var msg in getMimeMsgToEach(mail, cfg))
                    try
                    {
                        await client.SendAsync(msg, cancelToken); //odeslání mailu

                        successes[msg.To.First().ToString()] = new MailMessageCallbackArgs()
                        {
                            Exception = null,
                            Success = true
                        };
                    }
                    catch (Exception ex)
                    {
                        successes[msg.To.First().ToString()] = new MailMessageCallbackArgs()
                        {
                            Exception = ex,
                            Success = false
                        };

                        alles_gute = false;

                        error($"Problém při odesílání e-mailu na adresu {msg.To.First().ToString()}", ex);
                    }
            }
            catch (Exception ex)
            {
                alles_gute = false;
                exception = ex;

                error($"Problém při odesílání e-mailu ({mail.Subject})", ex);
            }
            finally
            {
                await client.DisconnectAsync(true, cancelToken); //odpojení
            }

            if (new_client)
                client.Dispose();

            return new MailCallbackArgs()
            {
                Mail = mail,
                Success = alles_gute,
                EachReceiverSuccess = successes,
                Exception = exception
            };
        }

        /// <summary>
        /// Odešle více mailů. Nepřidává je do fronty, pokud se to nepovede.
        /// </summary>
        /// <param name="mails">Maily pro odeslání</param>
        /// <param name="cancelToken">Žeton</param>
        /// <param name="sendCallback">Zavolá se pro každý mail.</param>
        /// <param name="cfg">Konfigurace pro použití. Pokud null, získá konfiguraci z aktuálního ConfigManageru.</param>
        /// <returns></returns>
        public async Task SendMultipleDumbAsync(
            IEnumerable<Mail> mails,
            CancellationToken cancelToken = default(CancellationToken),
            Action<MailCallbackArgs> sendCallback = null,
            EmailConfig cfg = null)
        {
            SmbLog.Trace("SendMultipleDumbAsync zavoláno", null, LogCategory.Emails);

            cfg = cfg ?? getCfg();

            using (var client = new SmtpClient())
            {
                //pokud je v konfiguraci, že důvěřujeme všem certifikátům, nastavit podle toho callback
                if (cfg.TrustAllCertificates)
                    client.ServerCertificateValidationCallback = (_1, _2, _3, _4) => true;

                await client.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort, true, cancelToken); //připojení
                await client.AuthenticateAsync(cfg.FromAddress, cfg.Password.Value, cancelToken); //oveření

                foreach(var mail in mails)
                {
                    var result = await SendDumbEachAsync(mail, cancelToken, cfg, client);
                    sendCallback?.Invoke(result);
                }
            }
        }

        /// <summary>
        /// Odešle mail. Pokud se to nepodaří, přidá ho do fronty k odeslání.
        /// </summary>
        /// <param name="mail">Mail k odeslání</param>
        /// <param name="cancelToken">Žeton</param>
        /// <param name="sendCallback">Zavolá se po odeslání</param>
        /// <param name="cfg">Konfigurace pro použití. Pokud null, získá konfiguraci z aktuálního ConfigManageru.</param>
        /// <returns></returns>
        public async Task<MailCallbackArgs> SendSmartEachAsync(
            Mail mail,
            CancellationToken cancelToken = default(CancellationToken),
            Action<MailCallbackArgs> sendCallback = null,
            EmailConfig cfg = null)
        {
            SmbLog.Info("SendSmartEachAsync zavoláno", null, LogCategory.Emails);

            //pokusit se odeslat mail
            var result = await SendDumbEachAsync(mail, cancelToken, cfg);

            //pokud se mail nepodařilo odeslat
            if (result != null && !result.Success)
            {
                //vytvořit kopii instance mailu
                var to_save = mail.Copy();

                //pokud je ToAddresses null, nastavit podle configu
                to_save.ToAddresses = to_save.ToAddresses ?? cfg.ToAddresses;

                //odstranit ty adresy, které se odeslaly úspěšně
                to_save.ToAddresses.RemoveAll(address =>
                {
                    return result.EachReceiverSuccess.ContainsKey(address) && result.EachReceiverSuccess[address].Success;
                });

                //přidat do fronty
                AddToQueue(to_save);
            }

            return result;
        }

        public bool IsSendingPendingEmails { get; private set; } = false;

        /// <summary>
        /// Odešle maily ve frontě. Odeslané maily se z fronty odstraní.
        /// </summary>
        /// <param name="cancelToken">Žeton</param>
        /// <param name="sendCallback">Zavolá se pro každý mail.</param>
        /// <param name="cfg">Konfigurace pro použití. Pokud null, získá konfiguraci z aktuálního ConfigManageru.</param>
        /// <returns></returns>
        public async Task SendPendingEmailsAsync(
            CancellationToken cancelToken = default(CancellationToken), 
            Action<MailCallbackArgs> sendCallback = null,
            EmailConfig cfg = null)
        {
            if (IsSendingPendingEmails)
                return;

            IsSendingPendingEmails = true;

            try
            {
                SmbLog.Info("SendPendingEmailsAsync zavoláno", null, LogCategory.Emails);

                cfg = cfg ?? getCfg();

                var mails = EnumerateMailQueue(f => f.CreationTime, true).Take(10);

                foreach (var mail_file in mails)
                {
                    RemoveFromQueue(mail_file);
                    await SendSmartEachAsync(mail_file.Mail, cancelToken, sendCallback, cfg);
                }

                SmbLog.Info("SendPendingEmailsAsync hotovo", null, LogCategory.Emails);
            }
            finally
            {
                IsSendingPendingEmails = false;
            }
        }
        
    }
}
