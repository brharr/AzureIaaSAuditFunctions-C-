using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureIaaSAudit.Entities
{
    public class SubnetEntity
    {
        public string SubnetName { get; set; }
        public string CIDR { get; set; }
        public NSGEntity NSG { get; set; }

        public List<LBEntity> LBs = new List<LBEntity>();

        public List<VMEntity> VMs = new List<VMEntity>();

        public List<NICEntity> OrphanedNICs = new List<NICEntity>();

        public SubnetEntity(string name, string cidr) {
            this.SubnetName = name;
            this.CIDR = cidr;
        }
    }
}
