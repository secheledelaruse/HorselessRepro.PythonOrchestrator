using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using HorselessRepro.PythonOrchestrator.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker; 
using Microsoft.Extensions.Logging;

namespace HorselessRepro.PythonOrchestrator.ReproFunctions
{
    public class ReproFunctions
    {
        private readonly ILogger<ReproFunctions> _logger;
        private BlobServiceClient _blobClient;
        private QueueServiceClient _queueClient;
        public ReproFunctions(ILogger<ReproFunctions> logger, QueueServiceClient queueClient, BlobServiceClient blobClient)
        {
            _logger = logger;
            _blobClient = blobClient;
            _queueClient = queueClient;
        }

        // cosmosdb triggered function
        [Function(nameof(CosmosDbTriggered))]
        public void CosmosDbTriggered([CosmosDBTrigger(databaseName: "reprodb", 
            containerName: "entries",
            LeaseContainerName = "reprodb-leases", 
            CreateLeaseContainerIfNotExists = true,
            Connection = "cosmosdb")] IReadOnlyList<ToDoItem> documents, FunctionContext context)
        {
            if (documents != null && documents.Count > 0)
            {
                _logger.LogInformation($"C# CosmosDB trigger function processed {documents.Count} documents.");
                foreach (var doc in documents)
                {
                    _logger.LogInformation($"Document Id: {doc.Id}, Content: {doc.Description}");
                }
            }
        }

        [Function(nameof(HttpTriggered))]
        public IActionResult HttpTriggered([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }

        // add a timer trigger with queue storage output
        [Function(nameof(TimerTriggered))]
        [QueueOutput("myqueue-items", Connection = "AzureWebJobsStorage")]
        public async Task<string> TimerTriggered([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var queueMessage = $"blob updated at: {DateTime.Now}";

            // update the blob with the current time
            var containerClient = _blobClient.GetBlobContainerClient("reprocontainer");
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient("reproblob.txt");
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(queueMessage)))
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return queueMessage;
        }

        // add a storage queue triggered function that reads a string from the queue
        //[Function(nameof(QueueTriggered))]
        //public async Task QueueTriggered([QueueTrigger("reproqueue")] string queueMessage, FunctionContext context)
        //{
        //    _logger.LogInformation($"C# Queue trigger function processed: {queueMessage}");
        //    var blobClient = _blobClient.GetBlobContainerClient("reprocontainer").GetBlobClient("reproblob.txt");
            
        //    // Read the blob content
        //    var response = await blobClient.DownloadAsync();
        //    using (var reader = new StreamReader(response.Value.Content))
        //    {
        //        var content = await reader.ReadToEndAsync();
        //        _logger.LogInformation($"Blob content: {content}");
        //    }
        //}
    }
}
