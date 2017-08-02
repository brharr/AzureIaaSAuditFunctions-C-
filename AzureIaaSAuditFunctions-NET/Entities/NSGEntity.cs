using System;
using System.Collections.Generic;

namespace AzureIaaSAudit.Entities
{
    public class NSGEntity
    {
        public string Name { get; set; }

        public List<NSGRuleEntity> Rules = new List<NSGRuleEntity>();

        public NSGEntity() { }
    }
}
