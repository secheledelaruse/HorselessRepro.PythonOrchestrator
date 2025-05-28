using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
// add local.settings.json to configuration
builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);

builder.AddServiceDefaults();

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();
builder.AddAzureBlobClient("AzureWebJobsStorage");
builder.AddAzureQueueClient("AzureWebJobsStorage");
builder.Build().Run();
