using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using HorselessRepro.PythonOrchestrator.Models;
using Microsoft.Azure.Cosmos;

namespace HorselessRepro.PythonOrchestrator.ApiService
{
    public interface IStorageLayer
    {
        Task<string> GetQueueItemsAsync();
        Task<string> GetCosmosDbMessageAsync(string containerName);
        Task<string> GetBlobMessageAsync();
    }
    public class StorageLayer : IStorageLayer
    {
        CosmosClient cosmosClient;
        QueueServiceClient queueServiceClient;
        BlobServiceClient blobClient;

        public StorageLayer(CosmosClient cosmosClient, QueueServiceClient queueServiceClient, BlobServiceClient blobClient)
        {
            this.cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            this.blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
            this.queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));
        }

        public async Task<string> GetQueueItemsAsync()
        {
            var reflectedBlobMessage = "This message is passed through the Python queue function.";
            var queueClient = queueServiceClient.GetQueueClient("myqueue-items");
            if (await queueClient.ExistsAsync())
            {
                var messages = await queueClient.ReceiveMessagesAsync(1);
                if (messages.Value.Length > 0)
                {
                    string base64Message = messages.Value[0].MessageText;
                    try
                    {
                        byte[] data = Convert.FromBase64String(base64Message);
                        reflectedBlobMessage = Encoding.UTF8.GetString(data);
                    }
                    catch (FormatException)
                    {
                        // Not a Base64 string, use as-is
                        reflectedBlobMessage = base64Message;
                    }
                }
                else
                {
                    reflectedBlobMessage = Constants.NoMessagesInQueueMessage; // "No messages in the queue.";
                }
            }
            else
            {
                reflectedBlobMessage = "Queue does not exist.";
            }

            return reflectedBlobMessage;
        }
        public async Task<string> GetBlobMessageAsync()
        {
            var currentBlobMessage = string.Empty;
            var containerClient =this. blobClient.GetBlobContainerClient("reprocontainer");
            var blobClient = containerClient.GetBlobClient("reproblob.txt");

            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadContentAsync();
                currentBlobMessage = response.Value.Content.ToString();
            }
            else
            {
                currentBlobMessage = "Blob does not exist.";
            }

            return currentBlobMessage;
        }

        public async Task<string> GetCosmosDbMessageAsync(string containerName = Constants.CosmosContainerEntries)
        {
            try
            {
                var database = cosmosClient.GetDatabase("reprodb");
                var container = database.GetContainer(containerName);

                var query = "SELECT TOP 1 c.id, c.Description FROM c ORDER BY c._ts DESC";
                var iterator = container.GetItemQueryIterator<ToDoItem>(query);

                var sb = new StringBuilder();
                int count = 0;
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        sb.AppendLine($"Id: {item.Id}, Desc: {item.Description}");
                        count++;
                    }
                }

                return count > 0 ? sb.ToString() : "No ToDoItems found in CosmosDB.";
            }
            catch (Exception ex)
            {
                return $"Error reading CosmosDB: {ex.Message}";
            }
        }

    }


}
