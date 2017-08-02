using System;

namespace AzureIaaSAudit.Entities
{
    public class DiskEntity
    {
        public string Name { get; set; }
        public int Lun { get; set; }
        public int? Size { get; set; }
        public string Type { get; set; }
        public string Caching { get; set; }

        public DiskEntity(string name, int? size, string type, string cache)
        {
            this.Lun = 0;
            this.Name = name;
            this.Size = size;
            this.Type = type;
            this.Caching = cache;
        }

        public DiskEntity(string name, int lun, int? size, string type, string cache)
        {
            this.Lun = lun;
            this.Name = name;
            this.Size = size;
            this.Type = type;
            this.Caching = cache;
        }
    }
}
