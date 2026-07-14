using System;
using System.Collections.Generic;

namespace TaskieLib
{
    public class AppServiceSubTaskItemDto
    {
        public string Name { get; set; }
        public bool IsDone { get; set; }
        public long CreationDate { get; set; }
        public long? ParentCreationDate { get; set; }
    }

    public class AppServiceTaskItemDto
    {
        public string Name { get; set; }
        public bool IsDone { get; set; }
        public long CreationDate { get; set; }
        public List<AppServiceSubTaskItemDto> SubTasks { get; set; }
    }

    public class AppServiceListSummaryDto
    {
        public string name { get; set; }
        public string id { get; set; }
        public string emoji { get; set; }
    }
}
