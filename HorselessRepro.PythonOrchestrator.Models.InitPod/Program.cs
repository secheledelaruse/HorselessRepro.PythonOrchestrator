using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HorselessRepro.PythonOrchestrator.Models.InitPod
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddJsonFile("app.settings.json", optional: true, reloadOnChange: true);

            builder.AddServiceDefaults();
            builder.AddAzureCosmosClient(connectionName: "cosmos-db");
            Console.WriteLine("Hello, World!");

            var host = builder.Build();

            await host.onEnsureContainers();
        }

        public static async Task onEnsureContainers(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {

                try
                {
                    var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();

                    var createdDatabase = await cosmosClient.CreateDatabaseIfNotExistsAsync("reprodb");

                    var leasedResourceContainer = createdDatabase.Database.CreateContainerIfNotExistsAsync(new ContainerProperties()
                    {
                        PartitionKeyPath = "/PartitionKey",
                        Id = "entries"
                    });
                     

                }
                catch (Exception e)
                {
                    Console.WriteLine("initpod exception: " + e.Message);
                }
            }
        }
    }
}
