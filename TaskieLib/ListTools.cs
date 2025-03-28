using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace TaskieLib {
    public class ListTools {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = JsonContext.Default
        };

        #region Event declarations

        private static bool _isawopen;
        public static bool isAWOpen {
            get => _isawopen;
            set { if (!value) { AWClosedEvent.Invoke(); } else { AWOpenEvent.Invoke(); } _isawopen = value; }
        }

        public delegate void AWClosed();
        public static event AWClosed AWClosedEvent;

        public delegate void AWOpen();
        public static event AWOpen AWOpenEvent;

        public delegate void ListCreated(string listID, string name);
        public static event ListCreated ListCreatedEvent;

        public delegate void ListDeleted(string listID);
        public static event ListDeleted ListDeletedEvent;

        public delegate void ListRenamed(string listID, string newname, string emoji);
        public static event ListRenamed ListRenamedEvent;

        public delegate void ListEmojiChanged(string listID, string name, string emoji);
        public static event ListEmojiChanged ListEmojiChangedEvent;

        #endregion

        #region List handling methods

        public static (string name, string id, string emoji)[] GetLists() {
            try {
                string localFolderPath = ApplicationData.Current.LocalFolder.Path;
                DirectoryInfo info = new DirectoryInfo(localFolderPath);
                FileInfo[] files = info.GetFiles("*.json");
                var lists = new List<(string name, string id, string emoji)>();

                foreach (FileInfo file in files) {
                    var content = File.ReadAllText(file.FullName);
                    JsonDocument doc = JsonDocument.Parse(content);

                    if (doc.RootElement.TryGetProperty("listmetadata", out var metadataElement)) {
                        var metadata = JsonSerializer.Deserialize<ListMetadata>(metadataElement.GetRawText(), _jsonOptions);
                        lists.Add((metadata?.Name, file.Name, metadata?.Emoji));
                    }
                    else {
                        lists.Add((null, file.Name, null));
                    }
                }
                return lists.ToArray();
            }
            catch (Exception ex) {
                Debug.WriteLine("[List getter] Exception occured: " + ex.Message);
                return Array.Empty<(string, string, string)>();
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

        public static ListData ReadList(string listId) {
            try {
                string taskFileContent = GetTaskFileContent(listId);
                if (taskFileContent != null) {
                    JsonDocument doc = JsonDocument.Parse(taskFileContent);
                    var root = doc.RootElement;

                    ListMetadata metadata = null;
                    List<ListTask> tasks = null;

                    if (root.TryGetProperty("listmetadata", out var metadataElement)) {
                        metadata = JsonSerializer.Deserialize<ListMetadata>(metadataElement.GetRawText(), _jsonOptions);
                    }

                    if (root.TryGetProperty("tasks", out var tasksElement)) {
                        tasks = JsonSerializer.Deserialize<List<ListTask>>(tasksElement.GetRawText(), _jsonOptions);
                    }

                    return new ListData(metadata ?? new ListMetadata(), tasks ?? new List<ListTask>());
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("[List reader] Exception occured: " + ex.Message);
            }
            return new ListData(new ListMetadata(), new List<ListTask>());
        }

        public static string CreateList(string listName, int? groupId = 0, string emoji = "📋") {
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
                Debug.WriteLine("[List creation] Exception occured: " + ex.Message);
                return null;
            }
        }

        public static void SaveList(string listId, List<ListTask> tasks, ListMetadata metadata) {
            Debug.WriteLine("Saving " + listId);
            try {
                string filePath = GetFilePath(listId);
                ListData listData = new ListData(
                    metadata,
                    tasks
                );
                File.WriteAllText(filePath, JsonSerializer.Serialize(listData, _jsonOptions));
            }
            catch (Exception ex) {
                Debug.WriteLine("[List saving] Exception occured: " + ex.Message);
            }
        }

        public static void DeleteList(string listId) {
            Debug.WriteLine("Deleted list:" + listId);
            try {
                string filePath = GetFilePath(listId);
                if (File.Exists(filePath)) {
                    string listName = ReadList(listId).Metadata.Name;
                    File.Delete(filePath);
                    ListDeletedEvent?.Invoke(listId);
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("[List deletion] Exception occured: " + ex.Message);
            }
        }

        public static void RenameList(string listId, string newListName) {
            try {
                var data = ReadList(listId);
                var metadata = data.Metadata;
                var tasks = data.Tasks;
                string oldName = metadata.Name;
                metadata.Name = newListName;
                SaveList(listId, tasks, metadata);
                ListRenamedEvent.Invoke(listId, newListName, metadata.Emoji);
            }
            catch (Exception ex) {
                Debug.WriteLine("[List renaming] Exception occured: " + ex.Message);
            }
        }

        public static void ChangeListEmoji(string listId, string newEmoji) {
            try {
                var data = ReadList(listId);
                var metadata = data.Metadata;
                var tasks = data.Tasks;
                ListMetadata newData = metadata;
                newData.Emoji = newEmoji;
                Debug.WriteLine(newData.Emoji);
                SaveList(listId, tasks, newData);
                ListEmojiChangedEvent?.Invoke(listId, newData.Name, newEmoji);
            }
            catch (Exception ex) {
                Debug.WriteLine("[List emoji change] Exception occured: " + ex.Message);
            }
        }

        public static void ChangeListFont(string listId, string font) {
            try {
                var data = ReadList(listId);
                var metadata = data.Metadata;
                var tasks = data.Tasks;
                ListMetadata newData = metadata;
                newData.TitleFont = font;
                SaveList(listId, tasks, newData);
            }
            catch (Exception ex) {
                Debug.WriteLine("[List font change] Exception occured: " + ex.Message);
            }
        }

        #endregion

        #region File operations

        private static string GetFilePath(string listId) {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, $"{listId}.json");
        }

        public static string GetTaskFileContent(string listId) {
            string filePath = GetFilePath(listId);
            return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
        }

        public static async Task<StorageFile> ExportedLists() {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            ZipFile.CreateFromDirectory(localFolder.Path, $"{tempFolder.Path}\\Export.taskie");
            StorageFile exportedFile = await tempFolder.GetFileAsync("Export.taskie");
            await exportedFile.MoveAsync(localFolder, "Export.taskie", NameCollisionOption.ReplaceExisting);
            return exportedFile;
        }

        public static async void ImportFile(StorageFile file) {
            try {
                string content = await FileIO.ReadTextAsync(file);
                JsonDocument doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                ListMetadata metadata = null;
                List<ListTask> tasks = null;

                if (root.TryGetProperty("listmetadata", out var metadataElement)) {
                    metadata = JsonSerializer.Deserialize<ListMetadata>(metadataElement.GetRawText(), _jsonOptions);
                }

                if (root.TryGetProperty("tasks", out var tasksElement)) {
                    tasks = JsonSerializer.Deserialize<List<ListTask>>(tasksElement.GetRawText(), _jsonOptions);
                }

                if (metadata != null) {
                    metadata.Name = GenerateUniqueListName(metadata.Name);
                    string newListId;
                    do {
                        newListId = Guid.NewGuid().ToString();
                    } while (File.Exists(GetFilePath(newListId)));

                    SaveList(newListId, tasks, metadata);
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("[File import] Exception occured: " + ex.Message);
            }
        }

        public static async Task ChangeListBackground(string listId, StorageFile file) {
            await file.CopyAsync(ApplicationData.Current.LocalFolder, "bg_" + listId, NameCollisionOption.ReplaceExisting);
        }

        #endregion
    }
}