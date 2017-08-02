using System;

namespace AzureIaaSAudit.Entities
{
    public class NSGRuleEntity
    {
        public string Name { get; set;}
        public string Access { get; set; }
        public string Protocol { get; set; }
        public string DestinationAddress { get; set; }
        public string DestinationPort { get; set; }
        public string SourceAddress { get; set; }
        public string SourcePort { get; set; }
        public int Priority { get; set; }
        public string Direction { get; set; }

        public NSGRuleEntity() { }
    }
}
