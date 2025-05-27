using Azure.Storage.Blobs;
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
        public ReproFunctions(ILogger<ReproFunctions> logger, BlobServiceClient client)
        {
            _logger = logger;
            _blobClient = client;
        }

        [Function(nameof(HttpTriggered))]
        public IActionResult HttpTriggered([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }

        // add a timer trigger with queue storage output
        [Function(nameof(TimerTriggered))]
        public async Task TimerTriggered([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
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
        }
    }
}
