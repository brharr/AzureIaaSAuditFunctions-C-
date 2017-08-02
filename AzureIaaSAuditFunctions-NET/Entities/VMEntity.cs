using System;
using System.Collections.Generic;

namespace AzureIaaSAudit.Entities
{
    public class VMEntity
    {
        public string VMId { get; set; }
        public string VMName { get; set; }
        public string Size { get; set; }
        public string OSType { get; set; }
        public string Publisher { get; set; }
        public string Offer { get; set; }
        public string SKU { get; set; }
        public PublicIPEntity PublicIP { get; set; }
        public NICEntity PrimaryNIC { get; set; }
        public DiskEntity OSDisk { get; set; }

        public List<DiskEntity> SecondaryDisks = new List<DiskEntity>();

        public List<NICEntity> SecondaryNICs = new List<NICEntity>();

        public VMEntity(string id, string name, string size, string ostype)
        {
            this.VMId = id;
            this.VMName = name;
            this.Size = size;
            this.OSType = ostype;
        }
    }
}
