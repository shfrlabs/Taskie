using System.Collections.Generic;
using System.Text.Json.Serialization;
using TaskieLib;

public sealed class ListData
{
    [JsonPropertyName("listmetadata")]
    public ListMetadata Metadata { get; set; }

    [JsonPropertyName("tasks")]
    public List<ListTask> Tasks { get; set; }

    [JsonConstructor]
    public ListData(ListMetadata metadata, List<ListTask> tasks)
    {
        Metadata = metadata;
        Tasks = tasks;
    }
}
