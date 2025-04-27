using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TaskieLib {
    [JsonSerializable(typeof(ListMetadata))]
    [JsonSerializable(typeof(List<ListTask>))]
    [JsonSerializable(typeof(ListData))]
    internal partial class TaskieJsonContext : JsonSerializerContext {
    }
}
