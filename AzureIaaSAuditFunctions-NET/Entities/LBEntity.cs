using System;
using System.Collections.Generic;
using Microsoft.Azure.Management.Network.Fluent;

namespace AzureIaaSAudit.Entities
{
    public class LBEntity
    {
        public string LBID { get; set; }
        public string LBName { get; set; }
        public string LBPrivateIP { get; set; }
        public int LBFrontPort { get; set; }
        public int LBBackPort { get; set; }

        public List<PublicIPEntity> LBPublicIPs = new List<PublicIPEntity>();

        public List<VMEntity> VMs = new List<VMEntity>();

        public LBEntity(string id, string name) {
            this.LBID = id;
            this.LBName = name;
        }

    }
}
