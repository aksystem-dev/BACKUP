using SmartModulBackupClasses.Mails;
using System;
using System.Collections.Generic;
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
        private Mailer getMailer() => Manager.Get<Mailer>();
        private EmailConfig getCfg() => Manager.Get<ConfigManager>()?.Config?.EmailConfig;

        public async Task ReportBackupAsync(Backup bk)
        {
            SmbLog.Trace("ReportBackupAsync zavoláno", null, LogCategory.Emails);

            var cfg = getCfg();

            //chceme poslat mail pouze o zálohách, při nichž došlo k chybě
            if (bk.Success || !cfg.SendErrors)
                return;

            //vygenerovat html
            StringBuilder email = new StringBuilder();
            email.AppendLine("<h1>Při záloze došlo k chybě</h1>");
            email.AppendLine($"<div>Název pravidla: \"{bk.RefRuleName}\"</div>");
            email.AppendLine($"<div>Datum zálohy: {bk.EndDateTime.ToString("dd. MM. yyyy, HH:mm:ss")}");

            email.AppendLine($"<h2>Chyby</h2>");
            email.AppendLine("<ul style=\"color:red\">");

            foreach (var error in bk.Errors)
            {
                email.AppendLine($"<li>Obecná chyba: {error.Message}</li>");
            }

            foreach(var src in bk.Sources)
            {
                if (src.Error != null)
                    email.AppendLine($"<li>Chyba při záloze zdroje {src.sourcename}: {src.Error}</li>");
            }

            email.AppendLine("</ul>");
            email.AppendLine("Tento e-mail byl odeslán z klientské aplikace smart modul BACKUP. Konfigurace odesílání e-mailů je dostupná v nastavení.");

            //vytvořit instanci mailu
            var mail = new Mail()
            {
                Content = email.ToString(),
                Html = true,
                Subject = "smart modul BACKUP - Chyba při záloze"
            };

            //předat jí Maileru
            var mailer = getMailer();
            await mailer.SendSmartEachAsync(mail, cfg: cfg);
        }


        /// <summary>
        /// Pro upozornění ohledně toho, že se jedno pravidlo spustilo víckrát najednou.
        /// Toto je minimální interval mezi těmito upozorněními pro každé pravidlo.
        /// </summary>
        static readonly TimeSpan MIN_CONCURRENT_RULE_EXECUTION_NOTIFY_INTERVAL = new TimeSpan(1, 0, 0);

        private readonly Dictionary<int, DateTime> _ruleLastConcurrentExecutionNotified = new Dictionary<int, DateTime>();

        public async Task NotifyConcurrentExecution(BackupRule rule)
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
            var email = new StringBuilder();
            email.AppendLine("<h1>Upozornění</h1>");
            email.AppendLine($"<div>Pravidlo {rule.Name} se spustilo vícekrát najednou.</div>");
            email.AppendLine($"<div>Aktuální čas: {now.ToString("dd. MM. yyyy, HH:mm:ss")}");
            email.AppendLine("<div>Pokud je toto nežádoucí, zaškrtněte v konfiguraci pravidla \"Zakázat vícenásobné spuštění.\"");

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
