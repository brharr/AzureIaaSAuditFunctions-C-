using System;
using System.Collections.Generic;

namespace AzureIaaSAudit.Entities
{
    public class NICEntity
    {
        public string NicID { get; set; }
        public string NicName { get; set; }
        public string IPAddress { get; set; }

        public NICEntity(string id, string name, string ip)
        {
            this.NicID = id;
            this.NicName = name;
            this.IPAddress = ip;
        }
    }
}
