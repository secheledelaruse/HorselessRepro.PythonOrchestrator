using System.Text.Json.Serialization;

namespace HorselessRepro.PythonOrchestrator.Models
{
    public class ToDoItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        public string? Description { get; set; }
        public string PartitionKey { get; set; } = "ToDoItem";
    }
}
