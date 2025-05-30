using System.Collections.Concurrent;
using HorselessRepro.PythonOrchestrator.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HorselessRepro.PythonOrchestrator.ApiService
{


    public interface ICosmosChangeFeedService : IHostedService
    {
        public ConcurrentQueue<ToDoItem> ChangeFeedQueue { get; }
        public ConcurrentQueue<ToDoItem> PythonChangeFeedQueue { get; } 

    }

    public class CosmosChangeFeedService : ICosmosChangeFeedService 
    {
        public ConcurrentQueue<ToDoItem> ChangeFeedQueue { get; } = new ConcurrentQueue<ToDoItem>();
        public ConcurrentQueue<ToDoItem> PythonChangeFeedQueue { get; } = new ConcurrentQueue<ToDoItem>();

        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _configuration;
        private ChangeFeedProcessor? _changeFeedProcessor;

        private ChangeFeedProcessor? _pyFuncsChangeFeedProcessor;

        public CosmosChangeFeedService(CosmosClient cosmosClient, IConfiguration configuration)
        {
            _cosmosClient = cosmosClient;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            string databaseName = _configuration["SourceDatabaseName"] ?? "reprodb";
            string sourceContainerName = _configuration["SourceContainerName"] ?? Constants.CosmosContainerEntries;
            string pythonFuncsContainerName = _configuration["PythonFuncsContainerName"] ?? Constants.CosmosContainerPyEntries;

            string leaseContainerName = _configuration["LeasesContainerName"] ?? "feed-leases";

            Container leaseContainer = _cosmosClient.GetContainer(databaseName, leaseContainerName);
            _changeFeedProcessor = _cosmosClient.GetContainer(databaseName, sourceContainerName)
                .GetChangeFeedProcessorBuilder<ToDoItem>(
                    processorName: "changeFeedSample",
                    onChangesDelegate: HandleChangesAsync)
                .WithInstanceName("consoleHost")
                .WithLeaseContainer(leaseContainer)
                .Build();

            _pyFuncsChangeFeedProcessor = _cosmosClient.GetContainer(databaseName, pythonFuncsContainerName)
                .GetChangeFeedProcessorBuilder<ToDoItem>(
                    processorName: "pythonfuncschangeFeedSample",
                    onChangesDelegate: HandlePythonChangesAsync)
                .WithInstanceName("pythonfuncsconsoleHost")
                .WithLeaseContainer(leaseContainer)
                .Build();

            Console.WriteLine("Starting Change Feed Processor...");
            await _changeFeedProcessor.StartAsync();
            await _pyFuncsChangeFeedProcessor.StartAsync();
            Console.WriteLine("Change Feed Processor started.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_changeFeedProcessor != null)
            {
                Console.WriteLine("Stopping Change Feed Processor...");
                await _changeFeedProcessor.StopAsync();
                Console.WriteLine("Change Feed Processor stopped.");
            }
        }

        /// <summary>
        /// The delegate receives batches of changes as they are generated in the change feed and can process them.
        /// </summary>
        public async Task HandleChangesAsync(
            ChangeFeedProcessorContext context,
            IReadOnlyCollection<ToDoItem> changes,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Started handling changes for lease {context.LeaseToken}...");
            Console.WriteLine($"Change Feed request consumed {context.Headers.RequestCharge} RU.");
            Console.WriteLine($"SessionToken {context.Headers.Session}");

            if (context.Diagnostics.GetClientElapsedTime() > TimeSpan.FromSeconds(1))
            {
                Console.WriteLine($"Change Feed request took longer than expected. Diagnostics:" + context.Diagnostics.ToString());
            }

            foreach (ToDoItem item in changes)
            {
                ChangeFeedQueue.Enqueue(item);
                await Task.Delay(10, cancellationToken);
            }

            Console.WriteLine("Finished handling changes.");
        }

        public async Task HandlePythonChangesAsync(
            ChangeFeedProcessorContext context,
            IReadOnlyCollection<ToDoItem> changes,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Started handling changes for lease {context.LeaseToken}...");
            Console.WriteLine($"Change Feed request consumed {context.Headers.RequestCharge} RU.");
            Console.WriteLine($"SessionToken {context.Headers.Session}");

            if (context.Diagnostics.GetClientElapsedTime() > TimeSpan.FromSeconds(1))
            {
                Console.WriteLine($"Change Feed request took longer than expected. Diagnostics:" + context.Diagnostics.ToString());
            }

            foreach (ToDoItem item in changes)
            {
                PythonChangeFeedQueue.Enqueue(item);
                await Task.Delay(10, cancellationToken);
            }

            Console.WriteLine("Finished handling changes.");
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCosmosChangeFeedService(this IServiceCollection services)
        {
            services.AddSingleton<ICosmosChangeFeedService, CosmosChangeFeedService>();
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ICosmosChangeFeedService>());
            return services;
        }
    }
}