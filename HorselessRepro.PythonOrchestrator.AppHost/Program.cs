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
    //.RunAsPreviewEmulator(emulator =>
    //{
    //    emulator.WithDataExplorer();
    //    emulator.WithLifetime(ContainerLifetime.Persistent);
    //    // emulator.WithGatewayPort(8081);
    //    emulator.WithEndpoint("emulator", endpoint =>
    //    {
    //        endpoint.Port = 8081;
    //        endpoint.TargetHost = "0.0.0.0";
    //    });

    //    emulator.WithEndpoint(endpointName: "data-explorer", endpoint =>
    //    {
    //        endpoint.UriScheme = "http";
    //        endpoint.TargetPort = 1234;
    //        endpoint.Port = 1234;
    //        endpoint.TargetHost = "0.0.0.0";
    //    });
    //    emulator.WithContainerRuntimeArgs("-p", $"8081:8081");
    //    emulator.WithContainerRuntimeArgs("-p", $"1234:1234");
    //})
    .RunAsPreviewEmulator(emulator =>
    {
        emulator.WithDataExplorer();
        emulator.WithLifetime(ContainerLifetime.Persistent); 
        emulator.WithEndpoint("emulator", endpoint =>
        {
            endpoint.Port = 8081;
            endpoint.TargetHost = "0.0.0.0";
        });
         
        emulator.WithContainerRuntimeArgs("-p", $"8081:8081"); 
    })
    //.WithEndpoint(endpointName: "data-explorer", endpoint =>
    //{
    //    endpoint.UriScheme = "http";
    //    endpoint.TargetPort = 1234;
    //    endpoint.Port = 1234;
    //    endpoint.TargetHost = "0.0.0.0";
    //})
    //.WithEndpoint("emulator", endpoint =>
    //{
    //    endpoint.Port = 8081;
    //    endpoint.TargetHost = "0.0.0.0";
    //})
    ;
var db = cosmos.AddCosmosDatabase("reprodb");
var container = db.AddContainer("entries", "/PartitionKey");

#pragma warning restore ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


// get the ipAddress of the host
var ipAddress = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
    .AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
  
var storageUri = $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://{ipAddress}:10000/devstoreaccount1;QueueEndpoint=http://{ipAddress}:10001/devstoreaccount1;TableEndpoint=http://{ipAddress}:10002/devstoreaccount1;";

var initPod = builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_Models_InitPod>("horselessdistributedlocking-sample-initpod")
        .WithReference(cosmosConnection)
        .WithReference(cosmos) 
        .WithReference(db)
        .WithReference(storageConnectionString) 
        .WaitFor(cosmos)
        .WaitFor(cosmosConnection) ;


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
                        .WaitFor(initPod);

var apiService = builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_ApiService>("apiservice");

var pythonFuncs = builder.AddDockerfile("repro-python-funcs", "../HorselessRepro.PythonOrchestrator.ReproFunctions.Python")
    .WithReference(blobs)
    .WithReference(cosmosConnection)
    .WithReference(cosmos)
    .WithEnvironment("AzureWebJobsStorage", storageUri)
    ///.WithEnvironment("ConnectionStrings:cosmosdb", $"AccountEndpoint=http://{ipAddress}:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;")
    .WithEnvironment("cosmosdb", $"AccountEndpoint=http://{ipAddress}:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;")
    // .WithReference(storageConnectionString)
    .WaitFor(blobs)
    .WaitFor(initPod);
 

builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(initPod)
    .WaitFor(apiService);

builder.Build().Run();
