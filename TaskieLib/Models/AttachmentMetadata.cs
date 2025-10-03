using System.Text.Json.Serialization;
using Windows.UI;

namespace TaskieLib
{
    public class AttachmentMetadata
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; }
        [JsonPropertyName("IsFairmark")]
        public bool IsFairmark { get; set; }
        [JsonPropertyName("Emoji")]
        public string Emoji { get; set; }
        [JsonPropertyName("Colors")]
        public Windows.UI.Color[] Colors { get; set; }
        [JsonPropertyName("FileName")]
        public string FileName { get; set; }
        [JsonPropertyName("FileType")]
        public string FileType { get; set; }
        [JsonPropertyName("RelativePath")]
        public string RelativePath { get; set; }
    }
}
