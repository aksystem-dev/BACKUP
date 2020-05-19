﻿using System;
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
        [DataMember]
        public Restore info;

        public RestoreResponse(Restore info) => this.info = info;

        [DataMember]
        public List<int> SuccessfulRestoreSourceIndexes = new List<int>();

        [DataMember]
        public List<string> errors = new List<string>();

        public bool Success => !errors.Any();
    }
}
