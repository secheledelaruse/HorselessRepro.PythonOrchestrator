using System.ComponentModel;

var builder = DistributedApplication.CreateBuilder(args);

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


// get the ipAddress of the host
var ipAddress = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
    .AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();

var storageUri = $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://{ipAddress}:10000/devstoreaccount1;QueueEndpoint=http://{ipAddress}:10001/devstoreaccount1;TableEndpoint=http://{ipAddress}:10002/devstoreaccount1;";

var initPod = builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_Models_InitPod>("horselessdistributedlocking-sample-initpod")
        // .WithReference(cosmosConnection)
        .WithReference(cosmos)
        .WithReference(db)
        .WithReference(storageConnectionString)
        .WaitFor(cosmos)
        .WaitFor(cosmosConnection);



var apiService = builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_ApiService>("apiservice")
    .WithReference(cosmos)
    .WithReference(cosmosConnection)
    .WithEnvironment("COSMOS_DB_ENDPOINT", $"http://{ipAddress}:8081/")
    .WithEnvironment("COSMOS_DB_KEY", $"C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==")
    .WithEnvironment("ConnectionStrings__cosmos-db", $"AccountEndpoint=http://{ipAddress}:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==")
    .WaitFor(cosmos)
    .WaitFor(storage)
    .WaitFor(initPod);

var pythonFuncs = builder.AddDockerfile("repro-python-funcs", "../HorselessRepro.PythonOrchestrator.ReproFunctions.Python")
    .WithReference(blobs)
    .WithReference(cosmosConnection)
    .WithReference(cosmos)
    .WithEnvironment("ConnectionStrings__AzureWebJobsStorage", storageUri)
    .WithEnvironment("AzureWebJobsStorage", storageUri)
    .WithEnvironment("ConnectionStrings:cosmosdb", $"AccountEndpoint=http://{ipAddress}:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;")
    // .WithEnvironment("cosmosdb", $"AccountEndpoint=http://{ipAddress}:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;")
    // .WithReference(storageConnectionString)
    .WaitFor(blobs)
    .WaitFor(cosmosConnection)
    .WaitFor(cosmos)
    .WaitForCompletion(initPod);


var functions = builder.AddAzureFunctionsProject<Projects.HorselessRepro_PythonOrchestrator_ReproFunctions>("functions")
                        .WithExternalHttpEndpoints()
                        .WithReference(queues)
                        .WithReference(cosmosConnection)
                        .WithReference(cosmos)
                        .WithEnvironment("ConnectionStrings:AzureWebjobsStorage", storageUri)
                        .WithEnvironment("cosmosdb", $"AccountEndpoint=http://{ipAddress}:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;")
                        .WithEnvironment("Values__ConnectionStrings__cosmosdb", $"AccountEndpoint=http://{ipAddress}:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;")
                        .WithHostStorage(storage)
                        .WaitFor(storage)
                        .WaitFor(pythonFuncs)
                        .WaitForCompletion(initPod);

builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitForCompletion(initPod)
    .WaitFor(apiService);

builder.Build().Run();
