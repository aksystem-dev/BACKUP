using SmartModulBackupClasses.Mails;
using SmartModulBackupClasses.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    /// <summary>
    /// Používá třídu Mailer a poskytuje specifické metody pro odesílání různých typů mailů.
    /// </summary>
    public class SmbMailer
    {
        public const string MAIL_TEMPLATES_FOLDER = "MailTemplates";
        public const string BACKUP_ERROR_REPORT_MAIL = "backup_error.html";
        public const string CONCURRENTS_BACKUPS_MAIL = "concurrent_backups.html";

        private Mailer getMailer() => Manager.Get<Mailer>();
        private EmailConfig getCfg() => Manager.Get<ConfigManager>()?.Config?.EmailConfig;

        //public async Task ReportBackupAsync(Backup bk)
        //{
        //    SmbLog.Trace("ReportBackupAsync zavoláno", null, LogCategory.Emails);

        //    var cfg = getCfg();

        //    //chceme poslat mail pouze o zálohách, při nichž došlo k chybě
        //    if (bk.Success || !cfg.SendErrors)
        //        return;

        //    //vygenerovat html
        //    StringBuilder email = new StringBuilder();
        //    email.AppendLine("<h1>Při záloze došlo k chybě</h1>");
        //    email.AppendLine($"<div>Název pravidla: \"{bk.RefRuleName}\"</div>");
        //    email.AppendLine($"<div>Datum zálohy: {bk.EndDateTime.ToString("dd. MM. yyyy, HH:mm:ss")}");

        //    email.AppendLine($"<h2>Chyby</h2>");
        //    email.AppendLine("<ul style=\"color:red\">");

        //    foreach (var error in bk.Errors)
        //    {
        //        email.AppendLine($"<li>Obecná chyba: {error.Message}</li>");
        //    }

        //    foreach(var src in bk.Sources)
        //    {
        //        if (src.Error != null)
        //            email.AppendLine($"<li>Chyba při záloze zdroje {src.sourcename}: {src.Error}</li>");
        //    }

        //    email.AppendLine("</ul>");
        //    email.AppendLine("Tento e-mail byl odeslán z klientské aplikace smart modul BACKUP. Konfigurace odesílání e-mailů je dostupná v nastavení.");

        //    //vytvořit instanci mailu
        //    var mail = new Mail()
        //    {
        //        Content = email.ToString(),
        //        Html = true,
        //        Subject = "smart modul BACKUP - Chyba při záloze"
        //    };

        //    //předat jí Maileru
        //    var mailer = getMailer();
        //    await mailer.SendSmartEachAsync(mail, cfg: cfg);
        //}

        public async Task ReportBackupErrorAsync(Backup bk)
        {
            //získat text mailu
            var email = getBackupMail(bk, BACKUP_ERROR_REPORT_MAIL);
            if (email == null)
            {
                notifyTemplateMissing(BACKUP_ERROR_REPORT_MAIL);
                return;
            }

            //vytvořit instanci mailu
            var mail = new Mail()
            {
                Content = email,
                Html = true,
                Subject = "smart modul BACKUP - Chyba při záloze"
            };

            //předat jí Maileru
            var mailer = getMailer();
            await mailer.SendSmartEachAsync(mail, cfg: getCfg());
        }

        private string getTemplate(string filename)
        {
            var fpath = Path.Combine(MAIL_TEMPLATES_FOLDER, filename);
            return File.Exists(fpath) ? File.ReadAllText(fpath) : null;
        }

        private void notifyTemplateMissing(string filename)
        {
            SmbLog.Error($"Nelze odeslat mail, neboť chybí předloha (soubor ${Path.Combine(MAIL_TEMPLATES_FOLDER, filename)})", null, LogCategory.Emails);
        }

        private void replaceCommon(StringInterpolator email)
        {
            email.Set("pc_name", SMB_Utils.GetComputerName());
            email.Set("datetime", DateTime.Now.ToString());
        }

        private void replaceBackup(StringInterpolator email, Backup bk)
        {
            email.Set("bk_rule_name", bk.RefRuleName);
            email.Set("bk_end_time", bk.EndDateTime.ToString());
            email.Set("bk_start_time", bk.StartDateTime.ToString());
            email.Set("bk_size", $"{bk.Size} B");

            var builder = new StringBuilder();
            builder.Append("<ul style=\"color:red\">");
            foreach (var error in bk.Errors)
            {
                if (error != null)
                    builder.AppendLine($"<li>Obecná chyba: {error.Message}</li>");
            }
            foreach (var src in bk.Sources)
            {
                if (src.Error != null)
                    builder.AppendLine($"<li>Chyba při záloze zdroje {src.sourcename}: {src.Error}</li>");
            }
            builder.Append("</ul>");
            email.Set("errors", builder.ToString());
        }

        private void replaceRule(StringInterpolator email, BackupRule rule)
        {
            email.Set("rule_name", rule.Name);
            email.Set("rule_last_date_time", rule.LastExecution.ToString());
            email.Set("rule_type", rule.RuleType.ToString());            
        }

        private string getBackupMail(Backup bk, string mailType)
        {
            var template = getTemplate(mailType);

            if (template == null)
                return null;

            var email = new StringInterpolator(template);
            replaceCommon(email);
            replaceBackup(email, bk);

            return email.ToString();
        }

        private string getRuleMail(BackupRule rule, string mailType)
        {
            var template = getTemplate(mailType);

            if (template == null)
                return null;

            var email = new StringInterpolator(template);
            replaceCommon(email);
            replaceRule(email, rule);

            return email.ToString();
        }

        /// <summary>
        /// Pro upozornění ohledně toho, že se jedno pravidlo spustilo víckrát najednou.
        /// Toto je minimální interval mezi těmito upozorněními pro každé pravidlo.
        /// </summary>
        static readonly TimeSpan MIN_CONCURRENT_RULE_EXECUTION_NOTIFY_INTERVAL = new TimeSpan(1, 0, 0);

        private readonly Dictionary<int, DateTime> _ruleLastConcurrentExecutionNotified = new Dictionary<int, DateTime>();

        //public async Task NotifyConcurrentExecution(BackupRule rule)
        //{
        //    SmbLog.Trace("NotifyConcurrentExecution zavoláno", null, LogCategory.Emails);

        //    var cfg = getCfg();

        //    //chceme poslat mail pouze o zálohách, při nichž došlo k chybě
        //    if (!cfg.SendErrors)
        //        return;

        //    //zde se zařizuje, abychom si pamatovali, kdy jsme pro dané pravidlo
        //    //naposledy zavolali tuto metodu, a vrátíme, pokud to nebylo před déle
        //    //jak MIN_CONCURRENT_RULE_EXECUTION_NOTIFY_INTERVAL
        //    var now = DateTime.Now;
        //    if (!_ruleLastConcurrentExecutionNotified.ContainsKey(rule.LocalID))
        //        _ruleLastConcurrentExecutionNotified[rule.LocalID] = now;
        //    else
        //    {
        //        var quit = (_ruleLastConcurrentExecutionNotified[rule.LocalID] - now).Duration() < MIN_CONCURRENT_RULE_EXECUTION_NOTIFY_INTERVAL;
        //        _ruleLastConcurrentExecutionNotified[rule.LocalID] = now;
        //        if (quit)
        //            return;                
        //    }

        //    //vygenerovat mail
        //    var email = new StringBuilder();
        //    email.AppendLine("<h1>Upozornění</h1>");
        //    email.AppendLine($"<div>Pravidlo {rule.Name} se spustilo vícekrát najednou.</div>");
        //    email.AppendLine($"<div>Aktuální čas: {now.ToString("dd. MM. yyyy, HH:mm:ss")}");
        //    email.AppendLine("<div>Pokud je toto nežádoucí, zaškrtněte v konfiguraci pravidla \"Zakázat vícenásobné spuštění.\"");

        //    //vytvořit instanci mailu
        //    var mail = new Mail()
        //    {
        //        Content = email.ToString(),
        //        Html = true,
        //        Subject = "smart modul BACKUP - Upozornění"
        //    };

        //    //předat jí Maileru
        //    var mailer = getMailer();
        //    await mailer.SendSmartEachAsync(mail, cfg: cfg);
        //}

        public async Task NotifyConcurrentExecutionAsync(BackupRule rule)
        {
            SmbLog.Trace("NotifyConcurrentExecution zavoláno", null, LogCategory.Emails);

            var cfg = getCfg();

            //chceme poslat mail pouze o zálohách, při nichž došlo k chybě
            if (!cfg.SendErrors)
                return;

            //zde se zařizuje, abychom si pamatovali, kdy jsme pro dané pravidlo
            //naposledy zavolali tuto metodu, a vrátíme, pokud to nebylo před déle
            //jak MIN_CONCURRENT_RULE_EXECUTION_NOTIFY_INTERVAL
            var now = DateTime.Now;
            if (!_ruleLastConcurrentExecutionNotified.ContainsKey(rule.LocalID))
                _ruleLastConcurrentExecutionNotified[rule.LocalID] = now;
            else
            {
                var quit = (_ruleLastConcurrentExecutionNotified[rule.LocalID] - now).Duration() < MIN_CONCURRENT_RULE_EXECUTION_NOTIFY_INTERVAL;
                _ruleLastConcurrentExecutionNotified[rule.LocalID] = now;
                if (quit)
                    return;
            }

            //vygenerovat mail
            var email = getRuleMail(rule, CONCURRENTS_BACKUPS_MAIL);

            //vytvořit instanci mailu
            var mail = new Mail()
            {
                Content = email.ToString(),
                Html = true,
                Subject = "smart modul BACKUP - Upozornění"
            };

            //předat jí Maileru
            var mailer = getMailer();
            await mailer.SendSmartEachAsync(mail, cfg: cfg);
        }
    }
}
