## Synopsis

This is the first project to produce a valid Audit Report of Azure IaaS resources within a given Subscription. Additional projects will be coming in additional languages as well as additional functionality.
## Motivation

I built this project as the starting point for the creation of an Audit Report for customers that need to do regularly scheduled Audits, such as PCI and Federal customers.

## Installation

Like any other application that will be deployed to Azure into an App Service, this application can be deployed directly within Visual Studio. In addition, these Functions can be deployed using a number of other means, you can check out the following documentation: [Automate Resource Deployment](https://docs.microsoft.com/en-us/azure/azure-functions/functions-infrastructure-as-code)

## Built With

* [Azure Functions C#](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-csharpg) 
* [Azure Functions Extension for VS 2017 Preview 3](https://blogs.msdn.microsoft.com/webdev/2017/05/10/azure-function-tools-for-visual-studio-2017/)

## Code Example

Once these Functions have been deployed properly to an Azure App Service, five distinct parameters will need to be added to the App Service within the App Service Settings or the Functions will not Run properly:

* auditstorage = Connection String of the Storage Account used for Queue Storage 
* AppID = GUID of the Service Principal User created for use within the Functions
* AppKey = Password associated with the Service Principal user created
* TenantID = GUID of the AAD Tenant where the Service Principal user was created
* SubscriptionID = Guid of the Subscription where the information will be gathered from 
* CosmosConn = Connection String of the Cosmos DB Database and Collection where the JSON documents will be placed

## Contributors

This project is meant to be consumed and then modified on a per customer or developer basis. Please feel free to either file an Issue or perform a Pull request should you find a bug or would like to make an enhancement on your own.