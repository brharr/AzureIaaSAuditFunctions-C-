using System;
using System.Collections.Generic;

namespace AzureIaaSAudit.Entities
{
    public class PublicIPEntity
    {
        public string IPID { get; set; }
        public string IPName { get; set; }
        public string IPAddress { get; set; }

        public PublicIPEntity(string id, string name, string ip) {
            this.IPID = id;
            this.IPName = name;
            this.IPAddress = ip;
        }
    }
}
