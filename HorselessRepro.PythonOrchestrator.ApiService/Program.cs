using HorselessRepro.PythonOrchestrator.ApiService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddScoped<IStorageLayer, StorageLayer>();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Expose minimal API endpoints for IStorageLayer methods
app.MapGet("/getBlobMessage", async (IStorageLayer storage) =>
{
    var result = await storage.GetBlobMessageAsync();
    return Results.Ok(result);
})
.WithName("GetBlobMessage");

app.MapGet("/getQueueItems", async (IStorageLayer storage) =>
{
    var result = await storage.GetQueueItemsAsync();
    return Results.Ok(result);
})
.WithName("GetQueueItems");

app.MapGet("/getCosmosDbMessage", async (IStorageLayer storage) =>
{
    var result = await storage.GetCosmosDbMessageAsync();
    return Results.Ok(result);
})
.WithName("GetCosmosDbMessage");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
