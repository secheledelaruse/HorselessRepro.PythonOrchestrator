using System.ComponentModel;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);

var storageConnectionString = builder.AddConnectionString("AzureWebJobsStorage");

var storage = builder.AddAzureStorage("storage").RunAsEmulator(
                     azurite =>
                     {

                         // azurite.WithDataVolume();

                         azurite.WithContainerRuntimeArgs("-p", $"10000:10000");
                         azurite.WithContainerRuntimeArgs("-p", $"10001:10001");
                         azurite.WithContainerRuntimeArgs("-p", $"10002:10002");

                     });
var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");

#pragma warning disable ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var cosmosConnection = builder.AddConnectionString("cosmosdb");

var cosmos = builder.AddAzureCosmosDB("cosmosresource")
    .RunAsPreviewEmulator(emulator =>
    {
        emulator.WithGatewayPort(8081);
        emulator.WithDataExplorer();
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });
var db = cosmos.AddCosmosDatabase("reprodb");
// Add a container for entries
var container = db.AddContainer("entries", "/PartitionKey");

var feedLeasesContainer = db.AddContainer("feed-leases", "/id");

// Add a container for Python functions entries
var pythonFuncsContainer = db.AddContainer("pyentries", "/PartitionKey");

#pragma warning restore ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var initPod = builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_Models_InitPod>("initpod") 
    .WithReference(cosmos) 
    .WithReference(blobs)
    .WithReference(storageConnectionString)
    .WithReference(db)
    .WaitFor(storage)
    .WaitFor(cosmos)
    .WaitFor(cosmosConnection);



var apiService = builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_ApiService>("apiservice")
    .WithReference(cosmos)
    .WithReference(cosmosConnection)
    .WithEnvironment("COSMOS_DB_ENDPOINT", builder.Configuration["CosmosEndpointConfig__AccountEndpoint"])
    .WithEnvironment("COSMOS_DB_KEY", builder.Configuration["CosmosEndpointConfig__AccountKey"])
    .WithEnvironment("ConnectionStrings__cosmosdb", builder.Configuration["CosmosEndpointConfig__cosmosdb"])
    .WaitFor(storage)
    .WaitFor(initPod);

var pythonFuncs = builder.AddDockerfile("repro-python-funcs", "../HorselessRepro.PythonOrchestrator.ReproFunctions.Python")
    .WithReference(blobs) 
    .WithReference(cosmos)
    .WithEnvironment("COSMOS_DB_ENDPOINT", builder.Configuration["CosmosEndpointConfig__AccountEndpoint"])
    .WithEnvironment("COSMOS_DB_KEY", builder.Configuration["CosmosEndpointConfig__AccountKey"])
    .WithEnvironment("ConnectionStrings__AzureWebJobsStorage", builder.Configuration["ConnectionStrings__AzureWebJobsStorage"])
    .WithEnvironment("AzureWebJobsStorage", builder.Configuration["ConnectionStrings__AzureWebJobsStorage"])
    .WithEnvironment("ConnectionStrings:cosmosdb", builder.Configuration["CosmosEndpointConfig__cosmosdb"])
    .WaitFor(blobs)
    .WaitFor(cosmosConnection)
    .WaitFor(cosmos)
    .WaitForCompletion(initPod);


var functions = builder.AddAzureFunctionsProject<Projects.HorselessRepro_PythonOrchestrator_ReproFunctions>("functions")
    .WithExternalHttpEndpoints()
    .WithReference(queues) 
    .WithReference(cosmos)
    .WithReference(cosmosConnection)
    //.WithEnvironment("ConnectionStrings:AzureWebjobsStorage", builder.Configuration["ConnectionStrings__AzureWebJobsStorage"])
    //.WithEnvironment("cosmosdb", builder.Configuration["CosmosEndpointConfig__cosmosdb"])
    //.WithEnvironment("Values__ConnectionStrings__cosmosdb", builder.Configuration["CosmosEndpointConfig__cosmosdb"])
    .WithHostStorage(storage)
    .WaitFor(storage)
    .WaitFor(pythonFuncs)
    .WaitFor(cosmos)
    .WaitForCompletion(initPod);

builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_Web>("webfrontend")
    .WithExternalHttpEndpoints() 
    .WithReference(cosmos)
    .WithReference(apiService)
    .WaitForCompletion(initPod)
    .WaitFor(apiService);

builder.Build().Run();
