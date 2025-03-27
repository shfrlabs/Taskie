using System.Collections.Generic;

namespace TaskieLib {
    /// <summary>
    /// Encapsulates a list's data, including its metadata and tasks.
    /// </summary>
    /// <remarks>
    /// This is used in favor of an anonymous type to allow for source-generated JSON serialization.
    /// </remarks>
    public sealed class ListData {
        public ListData(ListMetadata listmetadata, List<ListTask> tasks)
        {
            this.listmetadata = listmetadata;
            this.tasks = tasks;
        }

        public ListMetadata listmetadata { get; set; }
        public List<ListTask> tasks { get; set; }
    }
}