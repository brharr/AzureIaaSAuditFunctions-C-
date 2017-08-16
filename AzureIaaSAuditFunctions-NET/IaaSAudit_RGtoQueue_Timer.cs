using System;
using System.Configuration;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;


namespace AzureIaaSAudit
{
    public static class IaaSAudit_RGtoQueue_Timer
    {
        [FunctionName("IaaSAudit-RGtoQueue-Timer")]
        public static void Run([TimerTrigger("0 */45 * * * *")]TimerInfo myTimer, [Queue("auditresourcegroups", Connection = "auditstorage")]ICollector<string> resourceGroups, TraceWriter log)
        {
            log.Info($"IaaSAudit RG to Queue Timer trigger function executed at: {DateTime.Now}");

            // Get the information 
            string AppID = ConfigurationManager.AppSettings["AppID"];
            string AppKey = ConfigurationManager.AppSettings["AppKey"];
            string TenantID = ConfigurationManager.AppSettings["TenantID"];
            string SubscriptionID = ConfigurationManager.AppSettings["SubscriptionID"];

            // Do what is necessary to authenticate against the Commercial Azure Fluent API and against a specific subscription.
            AzureCredentialsFactory factory = new AzureCredentialsFactory();
            AzureCredentials credentials = factory.FromServicePrincipal(AppID, AppKey, TenantID, AzureEnvironment.AzureGlobalCloud);
            Azure azure = (Azure)Azure.Authenticate(credentials).WithSubscription(SubscriptionID);

            // For each Resource Group within the Subscription, we pass the name of the Group to the Queue
            foreach (var group in azure.ResourceGroups.List())
            {
                resourceGroups.Add(group.Name);
                log.Info($"Added the following resource group to the queue: {group.Name}");
            }

            log.Info($"IaaSAudit RG to Queue Timer triggger function completed at: {DateTime.Now}");
        }
    }
}