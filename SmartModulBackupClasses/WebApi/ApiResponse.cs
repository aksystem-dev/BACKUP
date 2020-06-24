using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SmartModulBackupClasses.WebApi
{
    [XmlInclude(typeof(bool))]
    [XmlInclude(typeof(string))]
    [XmlInclude(typeof(HelloResponse))]
    [XmlInclude(typeof(TestResponse))]
    [XmlInclude(typeof(BackupRule[]))]
    [XmlInclude(typeof(Backup[]))]
    [XmlInclude(typeof(SftpResponse))]
    public class ApiResponse
    {
        public bool Success { get; set; }
        public ApiError Error { get; set; }
        public object Content { get; set; }

        public static ApiResponse Ok(object content = null)
        {
            return new ApiResponse
            {
                Success = true,
                Error = ApiError.NoErrors,
                Content = content
            };
        }

        public static ApiResponse Problem(ApiError error)
        {
            return new ApiResponse
            {
                Success = false,
                Error = error,
                Content = null
            };
        }

        public string ErrorMessage => GetErrorMessage(Error);

        public static string GetErrorMessage(ApiError error)
        {
            switch(error)
            {
                case ApiError.NoErrors:
                    return "Nedošlo k žádným chybám.";
                case ApiError.ArgsMissing:
                    return "Chybí parametry.";
                case ApiError.AuthFailed:
                    return "Nepodařilo se ověřit uživatele.";
                case ApiError.MaxClientsReached:
                    return "Nelze překročit maximální počet klientů.";
                case ApiError.PlanNotFound:
                    return "Plán nenalezen.";
                case ApiError.AlreadyActivated:
                    return "Plán je již na tomto PC aktivní.";
                case ApiError.PC_NotFound:
                    return "Počítač nenalezen.";
                default:
                    return error.ToString();
            }
        }
    }

    public enum ApiError
    {
        /// <summary>
        /// Žádné chyby
        /// </summary>
        NoErrors,

        /// <summary>
        /// Chybí parametry
        /// </summary>
        ArgsMissing,

        /// <summary>
        /// Nepodařilo se ověřit uživatele
        /// </summary>
        AuthFailed,

        /// <summary>
        /// Plán je již na daném PC aktivovaný
        /// </summary>
        AlreadyActivated,

        /// <summary>
        /// Plán nenalezen
        /// </summary>
        PlanNotFound,

        /// <summary>
        /// Maximální počet klientů dosažen
        /// </summary>
        MaxClientsReached,

        /// <summary>
        /// Počítač nenalezen
        /// </summary>
        PC_NotFound,

        /// <summary>
        /// Některé záznamy nenalezeny
        /// </summary>
        SomeEntriesNotFound,

        /// <summary>
        /// Na počítači není aktivovaný žádný plán
        /// </summary>
        NoPlanActivates,

        /// <summary>
        /// Plán není povolen
        /// </summary>
        PlanNotEnabled,

        AlreadyExists,

        NotFound
    }
}
