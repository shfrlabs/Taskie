using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TaskieLib {
    [JsonSerializable(typeof(ListMetadata))]
    [JsonSerializable(typeof(ListTask))]
    [JsonSerializable(typeof(ListData))]
    [JsonSerializable(typeof(List<ListTask>))]
    [JsonSerializable(typeof(List<(string name, string id, string emoji)>))]
    public partial class JsonContext : JsonSerializerContext { }
}
