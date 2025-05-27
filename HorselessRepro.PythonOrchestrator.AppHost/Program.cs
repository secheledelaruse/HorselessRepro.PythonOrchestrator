var builder = DistributedApplication.CreateBuilder(args);

var storageConnectionString = builder.AddConnectionString("AzureWebJobsStorage");

var storage = builder.AddAzureStorage("storage").RunAsEmulator(
                     azurite =>
                     {
                         azurite.WithDataVolume();
                         azurite.WithLifetime(ContainerLifetime.Persistent);
                         azurite.WithBlobPort(10000)
                                .WithQueuePort(10001)
                                .WithTablePort(10002);
                     });
var blobs = storage.AddBlobs("blobs");

var functions = builder.AddAzureFunctionsProject<Projects.HorselessRepro_PythonOrchestrator_ReproFunctions>("functions")
                        .WithExternalHttpEndpoints()
                        .WithEnvironment("AzureWebjobsStorage", storageConnectionString)
                        .WithHostStorage(storage)
                        .WaitFor(storage);

var apiService = builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_ApiService>("apiservice");

var pythonFuncs = builder.AddDockerfile("repro-python-funcs", "../HorselessRepro.PythonOrchestrator.ReproFunctions.Python")
    .WithReference(blobs) 
    .WithReference(storageConnectionString)
    .WaitFor(blobs);
 

builder.AddProject<Projects.HorselessRepro_PythonOrchestrator_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
