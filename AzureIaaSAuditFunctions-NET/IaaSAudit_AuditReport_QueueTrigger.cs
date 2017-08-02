using System;
using System.Net;
using System.Configuration;
using System.Collections.Generic;

using Newtonsoft.Json;
using AzureIaaSAudit.Entities;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System.Linq;

namespace AzureIaaSAudit
{
    public static class IaaSAudit_AuditReport_QueueTrigger
    {
        // Get all of the necessary configuration information
        private static string AppID = ConfigurationManager.AppSettings["AppID"];
        private static string AppKey = ConfigurationManager.AppSettings["AppKey"];
        private static string TenantID = ConfigurationManager.AppSettings["TenantID"];
        private static string SubscriptionID = ConfigurationManager.AppSettings["SubscriptionID"];
        private static string CosmosURI = ConfigurationManager.AppSettings["CosmosURI"];
        private static string CosmosKey = ConfigurationManager.AppSettings["CosmosKey"];

        [FunctionName("IaaSAudit_RGAudit_QueueTrigger")]        
        public static void Run([QueueTrigger("auditresourcegroups", Connection = "auditstorage")]string resourceGroup, TraceWriter log,
                [DocumentDB("azureaudit", "rgiaasaudit", ConnectionStringSetting = "CosmosConn")] out string rgDocument)
        {
            log.Info($"C# Queue trigger function started: {resourceGroup}");

            // Do what is necessary to authenticate against the Commercial Azure Fluent API and against a specific subscription.
            AzureCredentialsFactory factory = new AzureCredentialsFactory();
            AzureCredentials credentials = factory.FromServicePrincipal(AppID, AppKey, TenantID, AzureEnvironment.AzureGlobalCloud);
            Azure azure = (Azure)Azure.Authenticate(credentials).WithSubscription(SubscriptionID);

            var group = azure.ResourceGroups.GetByName(resourceGroup);
            RGEntity AuditGroup = new RGEntity(group.Id, group.Name, SubscriptionID, TenantID);

            // We now need to loop through all of the VNets within the Resource Group
            foreach (var network in azure.Networks.ListByResourceGroup(group.Name))
            {
                NetworkEntity AuditVnet = new NetworkEntity(network.Id, network.Name, network.RegionName);
                foreach (var cidr in network.AddressSpaces)
                {
                    AuditVnet.CIDRs.Add(cidr);
                }

                // We need to loop through Subnets as this is the primary container for all VMs and LBs
                foreach (var subnet in network.Subnets.Values)
                {
                    SubnetEntity AuditSubnet = new SubnetEntity(subnet.Name, subnet.AddressPrefix);

                    // Pull all the Load Balancers and then pull all the resources that are associated with the LB, specifically the VMs
                    List<ILoadBalancer> LBs = GetDevicesInSubnet(azure.LoadBalancers.ListByResourceGroup(group.Name), AuditSubnet.CIDR);
                    List<INetworkInterface> _tmpNics = new List<INetworkInterface>();

                    // Loop through all of the Load Balancers within the Subnet and then Loop through the NICs attached to retrieve the VMs
                    foreach (var lb in LBs)
                    {
                        LBEntity AuditSubnetLB = new LBEntity(lb.Id, lb.Name);
                        foreach (string PublicIPId in lb.PublicIPAddressIds)
                        {
                            var publicip = azure.PublicIPAddresses.GetById(PublicIPId);
                            var PublicIPEntity = new PublicIPEntity(publicip.Id, publicip.Name, publicip.IPAddress);
                            AuditSubnetLB.LBPublicIPs.Add(PublicIPEntity);
                        }
                        foreach (var backPool in lb.Backends.Values)
                        {
                            // Now that we have the Backend Pool for the LB, we can get the VMs that are part of the pool. 
                            // This allows us to get a subset of VMs within the subnet.
                            var vms = backPool.GetVirtualMachineIds();
                            foreach (string vmid in vms)
                            {
                                var vm = azure.VirtualMachines.GetById(vmid);

                                VMEntity AuditVMEntity = CreateVMEntity(vm, azure);

                                var primarynic = vm.GetPrimaryNetworkInterface();
                                _tmpNics.Add(primarynic);

                                AuditSubnetLB.VMs.Add(AuditVMEntity);
                            }
                        }
                        AuditSubnet.LBs.Add(AuditSubnetLB);
                    }

                    // Should also include AppGateways which can provide Level 7 Load Balancing capability
                    //List<IApplicationGateway> AppGateways = GetDevicesinSubnet(azure.ApplicationGateways.ListByResourceGroup(group.Name), AuditSubnet.CIDR);

                    // Not sure if VM Scale Sets are being pulled with the LBs. So may need to pull them separately

                    // Need to pull all of the remaining NICs that fall in the Subnet
                    List<INetworkInterface> NICs = GetDevicesInSubnet(azure.NetworkInterfaces.ListByResourceGroup(group.Name), _tmpNics, AuditSubnet.CIDR);

                    foreach (INetworkInterface nic in NICs)
                    {
                        var vm = azure.VirtualMachines.GetById(nic.VirtualMachineId);
                        VMEntity NonLBVMEntity = CreateVMEntity(vm, azure);

                        AuditSubnet.VMs.Add(NonLBVMEntity);
                    }

                    if (subnet.NetworkSecurityGroupId != null)
                    {
                        var securitygroup = subnet.GetNetworkSecurityGroup();
                        NSGEntity AuditNSG = new NSGEntity();
                        AuditNSG.Name = securitygroup.Name;
                        foreach (var rule in securitygroup.SecurityRules)
                        {
                            NSGRuleEntity AuditRule = new NSGRuleEntity();
                            AuditRule.Name = rule.Value.Name;
                            AuditRule.Access = rule.Value.Access;
                            AuditRule.Priority = rule.Value.Priority;
                            AuditRule.Direction = rule.Value.Direction;
                            AuditRule.DestinationAddress = rule.Value.DestinationAddressPrefix;
                            AuditRule.DestinationPort = rule.Value.DestinationPortRange;
                            AuditRule.SourceAddress = rule.Value.SourceAddressPrefix;
                            AuditRule.SourcePort = rule.Value.SourcePortRange;
                            AuditRule.Protocol = rule.Value.Protocol;

                            AuditNSG.Rules.Add(AuditRule);
                        }
                        AuditSubnet.NSG = AuditNSG;
                    }
                    AuditVnet.Subnets.Add(AuditSubnet);
                }
                AuditGroup.Networks.Add(AuditVnet);
            }
            rgDocument = JsonConvert.SerializeObject(AuditGroup);

            log.Info($"Completed AuditReport_QueueTrigger Function for RG: {resourceGroup} at {DateTime.Now}");
        }

        /// <summary>
        /// Private function that will filter through a list of ILoadBalancer objects to determine if they are within
        /// the IP CIDR range provided
        /// </summary>
        /// <param name="lbs">Enumerable list of ILoadBalancer objects</param>
        /// <param name="cidr">String representation of a IP CIDR range</param>
        /// <returns>List of ILoadBalancer objects to be returned to the caller assuming they meet the criteria</returns>
        private static List<ILoadBalancer> GetDevicesInSubnet(IEnumerable<ILoadBalancer> lbs, string cidr)
        {
            List<ILoadBalancer> lbsinsubnet = new List<ILoadBalancer>();
            IPNetwork subnetNetwork = IPNetwork.Parse(cidr);

            foreach (ILoadBalancer lb in lbs)
            {
                foreach (ILoadBalancerFrontend frontend in lb.Frontends.Values)
                {
                    IPAddress lbAddress = IPAddress.Parse(frontend.Inner.PrivateIPAddress);
                    if (IPNetwork.Contains(subnetNetwork, lbAddress))
                        lbsinsubnet.Add(lb);
                }
            }

            return lbsinsubnet;
        }

        /// <summary>
        /// We need to pull all separate NICs that are part of the subnet, but that are not tied to a specific LB
        /// </summary>
        /// <param name="allnics">List of all INetworkInterfaces that are in the Resource Group</param>
        /// <param name="lbnics">All of the INetworkInterfaces that are attached to LBs within the Subnet</param>
        /// <param name="cidr">String representation of the IP Range within the specific Subnet</param>
        /// <returns></returns>
        private static List<INetworkInterface> GetDevicesInSubnet(IEnumerable<INetworkInterface> allnics, List<INetworkInterface> lbnics, string cidr)
        {
            List<INetworkInterface> nonlbnics = allnics.ToList();
            List<INetworkInterface> nicsinsubnet = new List<INetworkInterface>();

            IPNetwork subnetNetwork = IPNetwork.Parse(cidr);

            // First we need to remove the nics that are already part of a Load Balancer so that we don't add them in twice.
            foreach (INetworkInterface lbnic in lbnics)
            {
                foreach (INetworkInterface allnic in allnics)
                {
                    if (allnic.Id.CompareTo(lbnic.Id) == 0)
                        nonlbnics.Remove(lbnic);
                }
            }

            // Then we only loop through the Non-LB Nics to determine whichs ones are in the subnet and should be added
            foreach (INetworkInterface nic in nonlbnics)
            {
                IPAddress NicAddress = IPAddress.Parse(nic.PrimaryPrivateIP);
                if (IPNetwork.Contains(subnetNetwork, NicAddress))
                    nicsinsubnet.Add(nic);
            }

            return nicsinsubnet;
        }

        /// <summary>
        /// This function will do nothin but pull all the required information for a Virtual Machine and create the necessary Entity object
        /// </summary>
        /// <param name="vm">IVirtualMachine object containing all information about a specific Azure VM</param>
        /// <returns>VMEntity object containing only the information necessary for this application</returns>
        private static VMEntity CreateVMEntity(IVirtualMachine vm, Azure azure)
        {
            VMEntity AuditVM = new VMEntity(vm.Id, vm.Name, vm.Size.Value, vm.StorageProfile.OsDisk.OsType.ToString());
            AuditVM.Publisher = vm.StorageProfile.ImageReference.Publisher;
            AuditVM.SKU = vm.StorageProfile.ImageReference.Sku;
            AuditVM.Offer = vm.StorageProfile.ImageReference.Offer;

            INetworkInterface primarynic = azure.NetworkInterfaces.GetById(vm.PrimaryNetworkInterfaceId);
            AuditVM.PrimaryNIC = new NICEntity(primarynic.Id, primarynic.Name, primarynic.PrimaryPrivateIP);

            // Need to pull all of the Secondary Nics, but make sure that they are really secondary and not add the Primary one to the Secondary list
            string primarynicid = vm.PrimaryNetworkInterfaceId;
            foreach (string nicid in vm.NetworkInterfaceIds)
            {
                if (string.Compare(nicid, primarynicid) != 0)
                {
                    var nic = azure.NetworkInterfaces.GetById(nicid);
                    NICEntity AuditVMNicEntity = new NICEntity(nic.Id, nic.Name, nic.PrimaryPrivateIP);
                    AuditVM.SecondaryNICs.Add(AuditVMNicEntity);
                }
            }

            // Pull all the relevant information about the OS Disk and then all of the Secondary or Data Disks
            Console.WriteLine("OS Disk ID: " + vm.OSDiskId);
            var osDisk = azure.Disks.GetById(vm.OSDiskId);
            AuditVM.OSDisk = new DiskEntity(osDisk.Name, 0, osDisk.SizeInGB, "", vm.StorageProfile.OsDisk.Caching.ToString());
            if (vm.DataDisks.Count > 0)
            {
                foreach (var disk in vm.DataDisks.Values)
                {
                    DiskEntity datadisk = new DiskEntity(disk.Name, disk.Lun, disk.Size, disk.StorageAccountType.Value.ToString(), disk.CachingType.Value.ToString());
                    AuditVM.SecondaryDisks.Add(datadisk);
                }
            }
            if (vm.UnmanagedDataDisks.Count > 0)
            {
                foreach (var disk in vm.UnmanagedDataDisks.Values)
                {
                    DiskEntity datadisk = new DiskEntity(disk.Name, disk.Lun, disk.Size, "", disk.CachingType.ToString());
                    AuditVM.SecondaryDisks.Add(datadisk);
                }
            }

            return AuditVM;
        }
    }
}
