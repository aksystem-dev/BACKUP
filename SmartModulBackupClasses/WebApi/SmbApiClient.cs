using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses.WebApi
{
    public class SmbApiClient : INotifyPropertyChanged
    {
        public readonly string smb_url;

        public bool CredentialsChangedSinceLastRequest { get; set; }
        public DateTime LastApiCall { get; set; }

        private string username;
        private string password;

        private void ch(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public string Username
        {
            get => username;
            set
            {
                if (value == username)
                    return;

                username = value;
                CredentialsChangedSinceLastRequest = true;
                ch(nameof(Username));
            }
        }
        public string Password
        {
            get => password;
            set
            {
                if (value == password)
                    return;

                password = value;
                CredentialsChangedSinceLastRequest = true;
                ch(nameof(Password));
            }
        }

        public readonly string pc_id;
        public readonly string pc_name;
        private string token => Convert.ToBase64String(Encoding.UTF8.GetBytes(Username + ":" + Password));

        private WebRequestHandler requestHandler = new WebRequestHandler();
        private HttpClient client;

        public bool LogRequests { get; set; } = false;
        private List<RequestRecord> requests = new List<RequestRecord>();

        public event PropertyChangedEventHandler PropertyChanged;


        /// <summary>
        /// Pokud true, SmbApiClient nebude volat api ale rovnou vyhodí HttpStatusCode.Unauthorized, pokud poslední volání
        /// api s aktuálními přihlašovacími údaji v časovém rozmezí SmartAuthTimeout vyhodilo tento StatusCode.
        /// </summary>
        public bool SmartAuth { get; set; } = true;

        public TimeSpan SmartAuthTimeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Zaznamenává volání API. První hodnota je poslední volání
        /// </summary>
        public RequestRecord[] Requests => requests.ToArray();

        /// <summary>
        /// Vyprázdní Requests.
        /// </summary>
        public void ClearLog() => requests.Clear();

        /// <summary>
        /// Zadli poslední pokud o zavolání api spadl na hubu kvůli nesprávným přihlašovacím údajům.
        /// </summary>
        public bool LastAuthFailed { get; private set; } = false;

        /// <summary>
        /// Timeout v ms
        /// </summary>
        public int Timeout
        {
            get => (int)client.Timeout.TotalMilliseconds;
            set => client.Timeout = TimeSpan.FromMilliseconds(value);
        }

        public SmbApiClient(string username = null, string password = null, string pc_id = null, string pc_name = null, string smb_url = null, int? ms_timeout = null, bool ignoreCertificate = true)
        {
            this.Username = username ?? "";
            this.Password = password ?? "";
            this.pc_id = pc_id ?? SMB_Utils.GetComputerId();
            this.pc_name = pc_name ?? SMB_Utils.GetComputerName();
            this.smb_url = smb_url ?? "https://localhost:5001";

            client = new HttpClient(requestHandler);
            client.BaseAddress = new Uri(this.smb_url);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            if (ignoreCertificate)
                requestHandler.ServerCertificateValidationCallback = (_1,_2,_3,_4) => true;

            if (ms_timeout.HasValue)
                Timeout = ms_timeout.Value;
        }

        public SmbApiClient(WebConfig config, int? ms_timeout = null)
            : this(config.Username, config.Password.Value, null, null, null, ms_timeout)
        {
        }

        public async Task<ApiResponse> GetAsync(string url)
        {
            HttpResponseMessage response = null;
            string body = null;
            ApiResponse resp = null;

            if (!canPassThroughSmartAuth())
                throw new HttpStatusException(HttpStatusCode.Unauthorized);

            try
            {
                response = await client.GetAsync(url); 
                if (response.IsSuccessStatusCode)
                {
                    body = await response.Content.ReadAsStringAsync();
                    resp = deXml<ApiResponse>(body);
                    return resp;
                }

                throw new HttpStatusException(response.StatusCode);
            }
            finally
            {
                LastApiCall = DateTime.Now;
                LastAuthFailed = response?.StatusCode == HttpStatusCode.Unauthorized;

                if (LogRequests)
                {
                    var logged = new RequestRecord()
                    {
                        ApiError = resp?.Error,
                        DateTime = DateTime.Now,
                        RequestSerializable = null,
                        StatusCode = response?.StatusCode,
                        Uri = url
                    };

                    requests.Insert(0, logged);
                }
            }
        }

        public async Task<ApiResponse> PostAsync(string url, object content)
        {
            HttpResponseMessage response = null;
            string body = null;
            ApiResponse resp = null;

            if (!canPassThroughSmartAuth())
                throw new HttpStatusException(HttpStatusCode.Unauthorized);

            try
            {
                var request_body = new StringContent(toXml(content), Encoding.Unicode, "application/xml");
                response = await client.PostAsync(url, request_body);
                if (response.IsSuccessStatusCode)
                {
                    body = await response.Content.ReadAsStringAsync();
                    resp = deXml<ApiResponse>(body);
                    return resp;
                }

                throw new HttpStatusException(response.StatusCode);
            }
            finally
            {
                LastApiCall = DateTime.Now;
                LastAuthFailed = response.StatusCode == HttpStatusCode.Unauthorized;

                if (LogRequests)
                {
                    var logged = new RequestRecord()
                    {
                        ApiError = resp?.Error,
                        DateTime = DateTime.Now,
                        RequestSerializable = content,
                        StatusCode = response?.StatusCode,
                        Uri = url
                    };

                    requests.Insert(0, logged);
                }
            }
        }

        private bool canPassThroughSmartAuth()
        {
            if (!SmartAuth)
                return true;

            if (LastAuthFailed && (DateTime.Now - LastApiCall) < SmartAuthTimeout)
                return false;

            return true;
        }

        /// <summary>
        /// Získá info o dostupných plánech, popř. aktuálně používaný plán.
        /// </summary>
        /// <returns></returns>
        public async Task<HelloResponse> HelloAsync()
        {
            var response = await GetAsync("/api/Client/Hello?pc_id=" + this.pc_id).ConfigureAwait(false);
            if (response.Success)
                return response.Content as HelloResponse;
            else
                throw new SmbApiException(response.Error, response.ErrorMessage);
        }

        /// <summary>
        /// Získá info o dostupných plánech, popř. aktuálně používaný plán.
        /// </summary>
        /// <returns></returns>
        public HelloResponse Hello() => SMB_Utils.Sync(HelloAsync);

        /// <summary>
        /// Aktivuje na tomto PC daný plán.
        /// </summary>
        /// <param name="plan_id"></param>
        /// <returns></returns>
        public async Task ActivateAsync(int plan_id)
        {
            var request_obj = new ActivateRequest()
            {
                PC_ID = pc_id,
                PC_Name = pc_name,
                PlanID = plan_id
            };

            var response_obj = await PostAsync("/api/Client/Activate", request_obj);
            if (!response_obj.Success)
                throw new SmbApiException(response_obj.Error, response_obj.ErrorMessage);
            return;
        }

        /// <summary>
        /// Aktivuje na tomto PC daný plán.
        /// </summary>
        /// <param name="plan_id"></param>
        /// <returns></returns>
        public void Activate(int plan_id)
            => SMB_Utils.Sync(() => ActivateAsync(plan_id));

        /// <summary>
        /// Deaktivuje na tomto PC aktuální plán.
        /// </summary>
        /// <returns></returns>
        public async Task DeactivateAsync()
        {
            var request_obj = new DeactivateRequest()
            {
                PC_ID = pc_id
            };

            var response_obj = await PostAsync("/api/Client/Deactivate", request_obj);
            if (!response_obj.Success)
                throw new SmbApiException(response_obj.Error, response_obj.ErrorMessage);
            return;
        }

        /// <summary>
        /// Deaktivuje na tomto PC aktuální plán.
        /// </summary>
        /// <returns></returns>
        public void Deactivate()
            => SMB_Utils.Sync(() => DeactivateAsync());

        /// <summary>
        /// Získá seznam BackupRule uložených na serveru.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<BackupRule>> GetBackupRulesAsync()
        {
            var response = await GetAsync("/api/Rules/List?pc_id=" + pc_id);//.ConfigureAwait(false);
            if (!response.Success)
                throw new SmbApiException(response.Error, response.ErrorMessage);

            if (response.Content is BackupRule[] ruleArray)
                return ruleArray;
            else
                throw new FormatException("Přišel mi špatný formát ze serveru.");
        }

        /// <summary>
        /// Získá seznam BackupRule uložených na serveru.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BackupRule> GetBackupRules()
            => SMB_Utils.Sync(GetBackupRulesAsync);

        /// <summary>
        /// Nahraje daná pravidla na server. Ty, která už na serveru jsou, updatuje, ty, která ještě nejsou, přidá.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public async Task UpdateRulesAsync(IEnumerable<BackupRule> rules)
        {
            var request = new UploadRulesRequest()
            {
                PC_ID = pc_id,
                Rules = rules.ToArray()
            };

            var response = await PostAsync("/api/Rules/Update", request);

            if (!response.Success)
                throw new SmbApiException(response.Error, response.ErrorMessage);
        }

        /// <summary>
        /// Nahraje daná pravidla na server. Ty, která už na serveru jsou, updatuje, ty, která ještě nejsou, přidá.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public async Task UpdateRulesAsync(params BackupRule[] rules)
            => await UpdateRulesAsync(rules as IEnumerable<BackupRule>);

        /// <summary>
        /// Nahraje daná pravidla na server. Ty, která už na serveru jsou, updatuje, ty, která ještě nejsou, přidá.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public void UpdateRules(IEnumerable<BackupRule> rules)
            => SMB_Utils.Sync(() => UpdateRulesAsync(rules));

        /// <summary>
        /// Nahraje daná pravidla na server. Ty, která už na serveru jsou, updatuje, ty, která ještě nejsou, přidá.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public void UpdateRules(params BackupRule[] rules)
            => UpdateRules(rules as IEnumerable<BackupRule>);

        /// <summary>
        /// Odstraní ze serveru daná pravidla (podle LocalID)
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public async Task DeleteRulesAsync(IEnumerable<int> rules)
        {
            var request = new ConfirmRulesRequest()
            {
                PC_ID = pc_id,
                LocalRuleIDs = rules.ToArray()
            };

            var response = await PostAsync("/api/Rules/Delete", request);

            if (!response.Success)
                throw new SmbApiException(response.Error, response.ErrorMessage);
        }

        /// <summary>
        /// Odstraní ze serveru daná pravidla (podle LocalID)
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public async Task DeleteRulesAsync(params int[] rules)
            => await DeleteRulesAsync(rules as IEnumerable<int>);

        /// <summary>
        /// Odstraní ze serveru daná pravidla (podle LocalID)
        /// </summary>
        /// <param name="rules"></param>
        public void DeleteRules(IEnumerable<int> rules)
            => SMB_Utils.Sync(() => DeleteRulesAsync(rules));

        /// <summary>
        /// Odstraní ze serveru daná pravidla (podle LocalID)
        /// </summary>
        /// <param name="rules"></param>
        public void DeleteRules(params int[] rules)
            => DeleteRules(rules as IEnumerable<int>);

        /// <summary>
        /// Nahraje info o záloze na web.
        /// </summary>
        /// <param name="backup"></param>
        /// <param name="plan_id"></param>
        /// <returns></returns>
        public async Task AddBackupAsync(Backup backup, int plan_id)
        {
            var request = new UploadBackupRequest()
            {
                Backup = backup,
                PC_ID = pc_id,
                PlanID = plan_id
            };

            var response = await PostAsync("/api/Backups/Add", request);

            if (!response.Success)
                throw new SmbApiException(response.Error, response.ErrorMessage);
        }

        /// <summary>
        /// Nahraje info o záloze na web.
        /// </summary>
        /// <param name="backup"></param>
        /// <param name="plan_id"></param>
        public void AddBackup(Backup backup, int plan_id)
            => SMB_Utils.Sync(() => AddBackupAsync(backup, plan_id));

        public async Task UpdateBackupAsync(Backup backup, int plan_id)
        {
            var request = new UploadBackupRequest()
            {
                Backup = backup,
                PC_ID = pc_id,
                PlanID = plan_id
            };

            var response = await PostAsync("/api/Backups/Update", request);

            if (!response.Success)
                throw new SmbApiException(response.Error, response.ErrorMessage);
        }

        public void UpdateBackup(Backup backup, int plan_id)
            => SMB_Utils.Sync(() => UpdateBackupAsync(backup, plan_id));

        public async Task DeleteBackupAsync(int backupLocalId, int plan_id)
        {
            var request = new DeleteBackupRequest()
            {
                localBackupId = backupLocalId,
                PC_ID = pc_id,
                PlanID = plan_id
            };

            var response = await PostAsync("/api/Backups/Delete", request);

            if (!response.Success)
                throw new SmbApiException(response.Error, response.ErrorMessage);
        }

        public void DeleteBackup(int backupLocalId, int plan_id)
            => SMB_Utils.Sync(() => DeleteBackupAsync(backupLocalId, plan_id));

        /// <summary>
        /// Vrátí všechny zálohy daného plánu.
        /// </summary>
        /// <param name="plan_id"></param>
        /// <returns></returns>
        public async Task<Backup[]> ListBackupsAsync(int plan_id)
        {
            var response = await GetAsync("/api/Backups/List?plan_id=" + plan_id.ToString());

            if (!response.Success)
                throw new SmbApiException(response.Error, response.ErrorMessage);

            return response.Content as Backup[];
        }

        /// <summary>
        /// Vrátí všechny zálohy daného plánu.
        /// </summary>
        /// <param name="plan_id"></param>
        public void ListBackups(int plan_id)
            => SMB_Utils.Sync(() => ListBackupsAsync(plan_id));

        public async Task ConfirmRulesAsync(IEnumerable<int> local_rule_ids)
        {
            var request = new ConfirmRulesRequest()
            {
                LocalRuleIDs = local_rule_ids.ToArray(),
                PC_ID = pc_id
            };

            var response = await PostAsync("/api/rules/Confirm", request);

            if (!response.Success)
                throw new SmbApiException(response.Error, response.ErrorMessage);
        }

        public async Task ConfirmRulesAsync(params int[] local_rule_ids)
            => await ConfirmRulesAsync(local_rule_ids as IEnumerable<int>);

        public void ConfirmRules(IEnumerable<int> local_rule_ids)
            => SMB_Utils.Sync(() => ConfirmRulesAsync(local_rule_ids));

        public void ConfirmRules(params int[] local_rule_ids)
            => SMB_Utils.Sync(() => ConfirmRulesAsync(local_rule_ids));

        public async Task<SftpResponse> GetSftpAsync()
        {
            var response = await GetAsync("/api/Client/GetSftp?pc_id=" + this.pc_id).ConfigureAwait(false);
            if (response.Success)
                return response.Content as SftpResponse;
            else
                throw new SmbApiException(response.Error, response.ErrorMessage);
        }

        public SftpResponse GetSftp()
            => SMB_Utils.Sync(GetSftpAsync);

        private T deXml<T>(string str) where T : class
        {
            var xml = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(str))
                return xml.Deserialize(reader) as T;
        }

        //private string toXml<T>(T obj) where T : class
        //{
        //    var xml = new XmlSerializer(typeof(T));
        //    using (var writer = new StringWriter())
        //    {
        //        xml.Serialize(writer, obj);
        //        return writer.ToString();
        //    }
        //}

        private string toXml(object obj)
        {
            var xml = new XmlSerializer(obj.GetType());
            using (var writer = new StringWriter())
            {
                xml.Serialize(writer, obj);
                return writer.ToString();
            }
        }
    }

    public class SmbApiException : Exception
    {
        public ApiError Error;

        public SmbApiException() : base() { }
        public SmbApiException(ApiError error, string message = null) : base(message)
        {
            Error = error;
        }
    }

    public class HttpStatusException : Exception
    {
        public HttpStatusCode StatusCode;

        public HttpStatusException() : base() { }
        public HttpStatusException(HttpStatusCode status, string message = null) : base(message)
        {
            StatusCode = status;
        }
    }
}
