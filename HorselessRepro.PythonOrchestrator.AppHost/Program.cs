using System.ComponentModel;

var builder = DistributedApplication.CreateBuilder(args);

var storageConnectionString = builder.AddConnectionString("AzureWebJobsStorage");

var storage = builder.AddAzureStorage("storage").RunAsEmulator(
                     azurite =>
                     {

                         //azurite.WithDataVolume();
                         azurite.WithLifetime(ContainerLifetime.Persistent);
                         //azurite.WithBlobPort(10000)
                         //       .WithQueuePort(10001)
                         //       .WithTablePort(10002);


                         azurite.WithContainerRuntimeArgs("--blobHost", "0.0.0.0");
                         azurite.WithContainerRuntimeArgs("--queueHost", "0.0.0.0");
                         azurite.WithContainerRuntimeArgs("--tableHost", "0.0.0.0");

                         azurite.WithContainerRuntimeArgs("-p", $"0.0.0.0:10000:10000");
                         azurite.WithContainerRuntimeArgs("-p", $"0.0.0.0:10001:10001");
                         azurite.WithContainerRuntimeArgs("-p", $"0.0.0.0:10002:10002");

                     }) ;
var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");
// get the ipAddress of the host
var ipAddress = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
    .AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();

var storageUri = $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://{ipAddress}:10000/devstoreaccount1;QueueEndpoint=http://{ipAddress}:10001/devstoreaccount1;TableEndpoint=http://{ipAddress}:10002/devstoreaccount1;";
 

var functions = builder.AddAzureFunctionsProject<Projects.HorselessRepro_PythonOrchestrator_ReproFunctions>("functions")
                        .WithExternalHttpEndpoints()
                        .WithReference(queues)
                        .WithEnvironment("AzureWebjobsStorage", storageConnectionString)
                        .WithHostStorage(storage)
                        .WaitFor(storage);

var apiService = builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_ApiService>("apiservice");

var pythonFuncs = builder.AddDockerfile("repro-python-funcs", "../HorselessRepro.PythonOrchestrator.ReproFunctions.Python")
    .WithReference(blobs) 
    .WithEnvironment("AzureWebJobsStorage", storageUri)
    // .WithReference(storageConnectionString)
    .WaitFor(blobs);
 

builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
