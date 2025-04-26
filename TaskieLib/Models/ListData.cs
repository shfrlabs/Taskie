using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TaskieLib {
    /// <summary>
    /// Encapsulates a list's data, including its metadata and tasks.
    /// </summary>
    /// <remarks>
    /// This is used in favor of an anonymous type to allow for source-generated JSON serialization.
    /// </remarks>
    public sealed class ListData {
        public ListData(ListMetadata listmetadata, List<ListTask> tasks) {
            this.Metadata = listmetadata;
            this.Tasks = tasks;
        }

        [JsonPropertyName("listmetadata")]
        public ListMetadata Metadata { get; set; }

        [JsonPropertyName("tasks")]
        public List<ListTask> Tasks { get; set; }
    }
}