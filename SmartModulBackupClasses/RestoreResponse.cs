using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    [DataContract]
    public class RestoreResponse
    {
        /// <summary>
        /// Původní request. SavedSource jsou doplněné o informaci o tom, jak byly úspěšné.
        /// </summary>
        [DataMember]
        public Restore info { get; set; }

        public RestoreResponse(Restore info) => this.info = info;

        /// <summary>
        /// Seznam chyb
        /// </summary>
        [DataMember]
        public List<Error> errors { get; set; } = new List<Error>();

        /// <summary>
        /// Zdali byla obnova úspěšná
        /// </summary>
        [DataMember]
        public SuccessLevel Success { get; set; }
    }
}
