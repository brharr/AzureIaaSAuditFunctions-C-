using System;
using System.Collections.Generic;

namespace AzureIaaSAudit.Entities
{
    public class NetworkEntity
    {
        public string VNetID { get; set; }
        public string VNetName { get; set; }

        public List<string> CIDRs = new List<string>();
        public string Location { get; set; }
        public string DNS { get; set; }

        public List<SubnetEntity> Subnets = new List<SubnetEntity>();

        public NetworkEntity(string id, string name, string region) {
            this.VNetID = id;
            this.VNetName = name;
            this.Location = region;
        }
    }
}
