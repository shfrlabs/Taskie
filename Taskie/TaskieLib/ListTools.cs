using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using System.Text.Json.Serialization.Metadata;
using System.Collections.ObjectModel;

namespace TaskieLib {
    public class ListTools {
        private static readonly JsonSerializerOptions? _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = TaskieJsonContext.Default
        };

        #region Event declarations

        private static bool _isawopen;
        public static bool isAWOpen {
            get => _isawopen;
            set { if (!value) { AWClosedEvent?.Invoke(); } else { AWOpenEvent?.Invoke(); } _isawopen = value; }
        }

        public delegate void AWClosed();
        public static event AWClosed? AWClosedEvent;

        public delegate void AWOpen();
        public static event AWOpen? AWOpenEvent;

        public delegate void ListCreated(string? listID, string? name);
        public static event ListCreated? ListCreatedEvent;

        public delegate void ListDeleted(string? listID);
        public static event ListDeleted? ListDeletedEvent;

        public delegate void ListRenamed(string? listID, string? newname, string? emoji);
        public static event ListRenamed? ListRenamedEvent;

        public delegate void ListEmojiChanged(string? listID, string? name, string? emoji);
        public static event ListEmojiChanged? ListEmojiChangedEvent;

        #endregion

        #region List handling methods

        public static (string? name, string id, string? emoji)[] GetLists() {
            try {
                var localFolder = ApplicationData.Current.LocalFolder;
                var files = Directory.GetFiles(localFolder.Path, "*.json");
                var lists = new List<(string? name, string id, string? emoji)>();

                foreach (var filePath in files) {
                    var content = File.ReadAllText(filePath);
                    using var doc = JsonDocument.Parse(content);

                    if (doc.RootElement.TryGetProperty("listmetadata", out var metadataElement)) {
                        var metadata = JsonSerializer.Deserialize(
                            metadataElement.GetRawText(),
                            TaskieJsonContext.Default.ListMetadata
                        );
                        lists.Add((
                            metadata?.Name,
                            Path.GetFileName(filePath),
                            metadata?.Emoji
                        ));
                    }
                }
                return lists.ToArray();
            }
            catch (Exception ex) {
                Debug.WriteLine($"[List getter] Exception: {ex.Message}");
                return Array.Empty<(string?, string, string?)>();
            }
        }

        private static string GenerateUniqueListName(string listName) {
            string uniqueName = listName;
            int count = 2;
            while (GetLists().Select(t => t.name).Contains(uniqueName)) {
                uniqueName = $"{listName} ({count++})";
            }
            return uniqueName;
        }

        public static ListData ReadList(string? listId) {
            try {
                var taskFileContent = GetTaskFileContent(listId);
                if (taskFileContent != null) {
                    using var doc = JsonDocument.Parse(taskFileContent);
                    var root = doc.RootElement;

                    ListMetadata? metadata = null;
                    List<ListTask>? tasks = null;

                    if (root.TryGetProperty("listmetadata", out var metadataElement)) {
                        metadata = JsonSerializer.Deserialize(
                            metadataElement.GetRawText(),
                            TaskieJsonContext.Default.ListMetadata
                        );
                    }

                    if (root.TryGetProperty("tasks", out var tasksElement)) {
                        tasks = JsonSerializer.Deserialize(
                            tasksElement.GetRawText(),
                            TaskieJsonContext.Default.ListListTask
                        );
                    }

                    return (
                        new ListData(
                        metadata ?? new ListMetadata(),
                        tasks ?? new List<ListTask>())
                    );
                }
            }
            catch (Exception ex) {
                Debug.WriteLine($"[List reader] Exception: {ex.Message}");
            }
            return new ListData(new ListMetadata(), new List<ListTask>());
        }

        public static string? CreateList(string listName, int? groupId = 0, string? emoji = "📋") {
            try {
                string listId = Guid.NewGuid().ToString();
                var metadata = new ListMetadata {
                    CreationDate = DateTime.UtcNow,
                    Name = GenerateUniqueListName(listName),
                    Emoji = emoji,
                    GroupID = groupId
                };
                SaveList(listId, new List<ListTask>(), metadata);
                ListCreatedEvent?.Invoke(listId, listName);
                return listId;
            }
            catch (Exception ex) {
                Debug.WriteLine($"[List creation] Exception: {ex.Message}");
                return null;
            }
        }

        public static void SaveList(string? listId, List<ListTask> tasks, ListMetadata metadata) {
            try {
                var listData = new ListData(metadata, tasks);
                File.WriteAllText(
                    GetFilePath(listId),
                    JsonSerializer.Serialize(
                        listData,
                        TaskieJsonContext.Default.ListData
                    )
                );
            }
            catch (Exception ex) {
                Debug.WriteLine($"[List saving] Exception: {ex.Message}");
            }
        }

        public static void DeleteList(string? listId) {
            Debug.WriteLine("Deleted list:" + listId);
            try {
                string filePath = GetFilePath(listId);
                if (File.Exists(filePath)) {
                    string? listName = ReadList(listId).Metadata.Name;
                    File.Delete(filePath);
                    ListDeletedEvent?.Invoke(listId);
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("[List deletion] Exception occured: " + ex.Message);
            }
        }

        public static void RenameList(string? listId, string? newListName) {
            try {
                ListData data = ReadList(listId);
                string? oldName = data.Metadata.Name;
                data.Metadata.Name = newListName;
                SaveList(listId, data.Tasks, data.Metadata);
                ListRenamedEvent?.Invoke(listId, newListName, data.Metadata.Emoji);
            }
            catch (Exception ex) {
                Debug.WriteLine("[List renaming] Exception occured: " + ex.Message);
            }
        }

        public static void ChangeListEmoji(string? listId, string? newEmoji) {
            try {
                ListData data = ReadList(listId);
                ListMetadata? newData = data.Metadata;
                newData.Emoji = newEmoji;
                Debug.WriteLine(newData.Emoji);
                SaveList(listId, data.Tasks, newData);
                ListEmojiChangedEvent?.Invoke(listId, newData.Name, newEmoji);
            }
            catch (Exception ex) {
                Debug.WriteLine("[List emoji change] Exception occured: " + ex.Message);
            }
        }

        public static void ChangeListFont(string? listId, string? font) {
            try {
                ListData data = ReadList(listId);
                ListMetadata? newData = data.Metadata;
                //newData.TitleFont = font;
                SaveList(listId, data.Tasks, newData);
            }
            catch (Exception ex) {
                Debug.WriteLine("[List font change] Exception occured: " + ex.Message);
            }
        }

        #endregion

        #region File operations

        private static string GetFilePath(string? listId) {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, $"{listId}.json");
        }

        public static string? GetTaskFileContent(string? listId) {
            string filePath = GetFilePath(listId);
            return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
        }

        public static async Task<StorageFile> ExportedLists() {
            StorageFolder? tempFolder = ApplicationData.Current.TemporaryFolder;
            string exportPath = Path.Combine(tempFolder.Path, "Export.taskie");

            if (File.Exists(exportPath))
                File.Delete(exportPath);

            ZipFile.CreateFromDirectory(
                ApplicationData.Current.LocalFolder.Path,
                exportPath
            );
            return await tempFolder.GetFileAsync("Export.taskie");
        }

        public static async Task ImportFile(StorageFile file) {
            try {
                StorageFolder? tempFolder = ApplicationData.Current.TemporaryFolder;
                StorageFile? tempFile = await file.CopyAsync(tempFolder, "import.taskie", NameCollisionOption.ReplaceExisting);
                ZipFile.ExtractToDirectory(tempFile.Path, ApplicationData.Current.LocalFolder.Path, true);
            }
            catch (Exception ex) {
                Debug.WriteLine($"[File import] Exception: {ex.Message}");
            }
        }

        public static async Task ChangeListBackground(string? listId, StorageFile file) {
            await file.CopyAsync(ApplicationData.Current.LocalFolder, "bg_" + listId, NameCollisionOption.ReplaceExisting);
        }

        #endregion
    }
}