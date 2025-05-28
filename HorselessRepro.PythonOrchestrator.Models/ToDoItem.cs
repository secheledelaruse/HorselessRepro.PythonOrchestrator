namespace HorselessRepro.PythonOrchestrator.Models
{
    public class ToDoItem
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public string PartitionKey { get; set; } = "ToDoItem";
    }
}
