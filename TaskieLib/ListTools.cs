using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace TaskieLib
{
    public class ListTools
    {

        private static bool _isawopen;
        public static bool isAWOpen
        {
            get => _isawopen;
            set { if (!value) { AWClosedEvent?.Invoke(); } else { AWOpenEvent?.Invoke(); } _isawopen = value; }
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



        public static (string name, string id, string emoji)[] GetLists()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                string[] files = Directory.GetFiles(localFolder.Path, "*.json");
                List<(string name, string id, string emoji)> lists = new List<(string name, string id, string emoji)>();

                foreach (string filePath in files)
                {
                    string content = File.ReadAllText(filePath);
                    JsonDocument doc = JsonDocument.Parse(content);

                    if (doc.RootElement.TryGetProperty("listmetadata", out JsonElement metadataElement))
                    {
                        var metadata = JsonSerializer.Deserialize<ListMetadata>(
                            metadataElement.GetRawText()
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[List getter] Exception: {ex.Message}");
                return Array.Empty<(string, string, string)>();
            }
        }

        private static string GenerateUniqueListName(string listName)
        {
            string uniqueName = listName;
            int count = 2;
            while (GetLists().Select(t => t.name).Contains(uniqueName))
            {
                uniqueName = $"{listName} ({count++})";
            }
            return uniqueName;
        }

        public static ListData ReadList(string listId)
        {
            try
            {
                var taskFileContent = GetTaskFileContent(listId);
                if (taskFileContent != null)
                {
                    JsonDocument doc = JsonDocument.Parse(taskFileContent);
                    JsonElement root = doc.RootElement;

                    ListMetadata metadata = null;
                    List<ListTask> tasks = null;

                    if (root.TryGetProperty("listmetadata", out var metadataElement))
                    {
                        metadata = JsonSerializer.Deserialize<ListMetadata>(metadataElement.GetRawText());
                    }

                    if (root.TryGetProperty("tasks", out var tasksElement))
                    {
                        tasks = JsonSerializer.Deserialize<List<ListTask>>(
                            tasksElement.GetRawText()
                        );
                    }

                    return (
                        new ListData(
                        metadata ?? new ListMetadata(),
                        tasks ?? new List<ListTask>())
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[List reader] Exception: {ex.Message}");
            }
            return new ListData(new ListMetadata(), new List<ListTask>());
        }

        public static string CreateList(string listName, string emoji = "📋")
        {
            try
            {
                string listId = Guid.NewGuid().ToString();
                var metadata = new ListMetadata
                {
                    CreationDate = DateTime.UtcNow,
                    Name = GenerateUniqueListName(listName),
                    Emoji = emoji
                };
                SaveList(listId, new List<ListTask>(), metadata);
                ListCreatedEvent?.Invoke(listId, listName);
                return listId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[List creation] Exception: {ex.Message}");
                return null;
            }
        }

        public static void SaveList(string listId, List<ListTask> tasks, ListMetadata metadata)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = true
                };

                DataPackage pkg = new DataPackage();
                Clipboard.SetContent(pkg);
                var listData = new ListData(metadata, tasks);
                File.WriteAllText(
                    GetFilePath(listId),
                    JsonSerializer.Serialize(listData, options)
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[List saving] Exception: {ex.Message}");
            }
        }

        public static void DeleteList(string listId)
        {
            Debug.WriteLine("Deleted list:" + listId);
            try
            {
                string filePath = GetFilePath(listId);
                if (File.Exists(filePath))
                {
                    string listName = ReadList(listId).Metadata.Name;
                    File.Delete(filePath);
                    ListDeletedEvent?.Invoke(listId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[List deletion] Exception occured: " + ex.Message);
            }
            Tools.RemoveAttachmentsFromList(listId);
        }

        public static void RenameList(string listId, string newListName)
        {
            try
            {
                ListData data = ReadList(listId);
                string oldName = data.Metadata.Name;
                data.Metadata.Name = newListName;
                SaveList(listId, data.Tasks, data.Metadata);
                ListRenamedEvent?.Invoke(listId, newListName, data.Metadata.Emoji);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[List renaming] Exception occured: " + ex.Message);
            }
        }

        public static void ChangeListEmoji(string listId, string newEmoji)
        {
            try
            {
                ListData data = ReadList(listId);
                ListMetadata newData = data.Metadata;
                newData.Emoji = newEmoji;
                SaveList(listId, data.Tasks, newData);
                ListEmojiChangedEvent?.Invoke(listId, newData.Name, newEmoji);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[List emoji change] Exception occured: " + ex.Message);
            }
        }

        public static void ChangeListFont(string listId, string font)
        {
            try
            {
                ListData data = ReadList(listId);
                ListMetadata newData = data.Metadata;
                newData.TitleFont = font;
                SaveList(listId, data.Tasks, newData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[List font change] Exception occured: " + ex.Message);
            }
        }




        private static string GetFilePath(string listId)
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, $"{listId}.json");
        }

        public static string GetTaskFileContent(string listId)
        {
            string filePath = GetFilePath(listId);
            return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
        }

        public static async Task<StorageFile> ExportedLists()
        {
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            string exportPath = Path.Combine(tempFolder.Path, "Export.taskie");

            if (File.Exists(exportPath))
                File.Delete(exportPath);


            StorageFolder exportFolder = await tempFolder.CreateFolderAsync(
                "forexport",
                CreationCollisionOption.ReplaceExisting
            );

            foreach (StorageFile file in await ApplicationData.Current.LocalFolder.GetFilesAsync())
            {
                if (file.Name.EndsWith(".json"))
                {
                    await file.CopyAsync(exportFolder, file.Name, NameCollisionOption.ReplaceExisting);
                }
            }

            ZipFile.CreateFromDirectory(
                exportFolder.Path,
                exportPath
            );

            await exportFolder.DeleteAsync();
            return await tempFolder.GetFileAsync("Export.taskie");
        }

        public static async Task ImportJson(StorageFile jsonFile)
        {
            ListData data = JsonSerializer.Deserialize<ListData>(
                    (await jsonFile.OpenReadAsync()).AsStreamForRead()
                );
            if (GetLists().Select(t => t.name).Contains(data.Metadata.Name))
            {
                data.Metadata.Name = GenerateUniqueListName(data.Metadata.Name);
            }
            SaveList(
                Guid.NewGuid().ToString(),
                data.Tasks,
                data.Metadata
            );
        }

        public static async Task ImportFile(StorageFile file)
        {
            if (file.Name.EndsWith(".json"))
            {
                await ImportJson(file);
            }
            else
            {
                try
                {
                    StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
                    StorageFile tempFile = await file.CopyAsync(tempFolder, "import.taskie", NameCollisionOption.ReplaceExisting);
                    ZipFile.ExtractToDirectory(tempFile.Path, Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "tempexport"), true);
                    foreach (StorageFile f in await tempFolder.GetFilesAsync())
                    {
                        if (f.Name.EndsWith(".json"))
                        {
                            await ImportJson(f);
                        }
                    }
                    await tempFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    await (await tempFolder.GetFolderAsync("tempexport")).DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[File import] Exception: {ex.Message}");
                }
            }
        }

        public static async Task ChangeListBackground(string listId, StorageFile file)
        {
            await file.CopyAsync(ApplicationData.Current.LocalFolder, "bg_" + listId, NameCollisionOption.ReplaceExisting);
        }


    }
}