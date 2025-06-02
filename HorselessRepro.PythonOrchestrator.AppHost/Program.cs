#define DEFAULT_EXPERIENCE // note - python funcs will fail to start
// #define CUSTOMIZED_STORAGE_EMULATOR
// #define HARDCODED_URIS

using System.ComponentModel;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);

var storageConnectionString = builder.AddConnectionString("AzureWebJobsStorage");

#if CUSTOMIZED_STORAGE_EMULATOR

var storage = builder.AddAzureStorage("storage").RunAsEmulator(
        azurite =>
        { 
            azurite.WithContainerRuntimeArgs("-p", $"10000:10000");
            azurite.WithContainerRuntimeArgs("-p", $"10001:10001");
            azurite.WithContainerRuntimeArgs("-p", $"10002:10002");

        });
#else

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
#endif

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
// Add a container for entries; these are written by c# functions and are meant to be consumed by Python functions.
var container = db.AddContainer("entries", "/PartitionKey");

var feedLeasesContainer = db.AddContainer("feed-leases", "/id");

// Add a container for Python functions entries.. these are modified entities sourced from the 'entries' container
var pythonFuncsContainer = db.AddContainer("pyentries", "/PartitionKey");

#pragma warning restore ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var initPod = builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_Models_InitPod>("initpod") 
    .WithReference(cosmos) 
    .WithReference(blobs)
    .WithReference(queues)
    .WithReference(storageConnectionString)
    .WithReference(db)
    .WaitFor(storage)
    .WaitFor(cosmos)
    .WaitFor(cosmosConnection);



var apiService = builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_ApiService>("apiservice")
    .WithReference(cosmos)
    .WithReference(cosmosConnection) 
    .WithReference(blobs)
    .WithReference(queues)
    .WaitFor(storage)
    .WaitFor(initPod);

#if DEFAULT_EXPERIENCE

var pythonFuncs = builder.AddDockerfile("repro-python-funcs", "../HorselessRepro.PythonOrchestrator.ReproFunctions.Python")
    .WithReference(blobs)
    .WithReference(cosmos)
    .WithReference(cosmosConnection)
    .WithReference(db)
    .WithReference(storageConnectionString)
    .WithEnvironment("COSMOS_DB_ENDPOINT", builder.Configuration["CosmosEndpointConfig__AccountEndpoint"])
    .WithEnvironment("COSMOS_DB_KEY", builder.Configuration["CosmosEndpointConfig__AccountKey"])
    .WaitFor(blobs)
    .WaitFor(cosmosConnection)
    .WaitFor(cosmos)
    .WaitForCompletion(initPod);


#elif HARDCODED_URIS

var ipAddress = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
    .AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
var storageUri = $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://{ipAddress}:10000/devstoreaccount1;QueueEndpoint=http://{ipAddress}:10001/devstoreaccount1;TableEndpoint=http://{ipAddress}:10002/devstoreaccount1;";

var pythonFuncs = builder.AddDockerfile("repro-python-funcs", "../HorselessRepro.PythonOrchestrator.ReproFunctions.Python")
    .WithReference(blobs)
    .WithReference(cosmos)
    .WithReference(cosmosConnection)
    .WithReference(db)
    .WithReference(storageConnectionString)
    .WithEnvironment("COSMOS_DB_ENDPOINT", builder.Configuration["CosmosEndpointConfig__AccountEndpoint"])
    .WithEnvironment("COSMOS_DB_KEY", builder.Configuration["CosmosEndpointConfig__AccountKey"])
    .WithEnvironment("ConnectionStrings__AzureWebJobsStorage", storageUri)
    .WithEnvironment("AzureWebJobsStorage", storageUri)
    .WithEnvironment("ConnectionStrings:cosmosdb", $"AccountEndpoint=http://{ipAddress}:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;")
    .WaitFor(blobs)
    .WaitFor(cosmosConnection)
    .WaitFor(cosmos)
    .WaitForCompletion(initPod);


#endif


var functions = builder.AddAzureFunctionsProject<Projects.HorselessRepro_PythonOrchestrator_ReproFunctions>("functions")
    .WithExternalHttpEndpoints()
    .WithReference(queues) 
    .WithReference(cosmos)
    .WithReference(db)
    .WithReference(cosmosConnection) 
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
