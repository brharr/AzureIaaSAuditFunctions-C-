using System;
using System.Collections.Generic;

namespace AzureIaaSAudit.Entities
{
    public class RGEntity
    {
        public string RGID { get; set; }
        public string RGName { get; set; }
        public string Sub { get; set; }
        public string Tenant { get; set; }
    
        public List<NetworkEntity> Networks = new List<NetworkEntity>();

        public RGEntity(string id, string name, string subid, string tenant) {
            this.RGID = id;
            this.RGName = name;
            this.Sub = subid;
            this.Tenant = tenant;
        }
    }
}
