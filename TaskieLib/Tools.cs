using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace TaskieLib
{
    public class Tools
    {
        private static bool _isawopen;
        public static bool isAWOpen
        {
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

        public delegate void ListRenamed(string listID, string newname);
        public static event ListRenamed ListRenamedEvent;

        public delegate void ListEmojiChanged(string listID, string emoji);
        public static event ListEmojiChanged ListEmojiChangedEvent;

        public static void SaveList(string listId, List<ListTask> tasks, ListMetadata metadata)
        {
            Debug.WriteLine("Saving " + listId);
            try
            {
                string filePath = GetFilePath(listId);
                var listData = new
                {
                    listmetadata = metadata,
                    tasks = tasks
                };
                File.WriteAllText(filePath, JsonConvert.SerializeObject(listData));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving list: {ex.Message}");
            }
        }

        public static (string name, string id)[] GetLists()
        {
            try
            {
                string localFolderPath = ApplicationData.Current.LocalFolder.Path;
                DirectoryInfo info = new DirectoryInfo(localFolderPath);
                FileInfo[] files = info.GetFiles("*.json");
                List<(string name, string id)> lists = new List<(string name, string id)>();
                foreach (FileInfo file in files)
                {
                    var content = File.ReadAllText(file.FullName);
                    var metadata = JsonConvert.DeserializeObject<dynamic>(content).listmetadata;
                    lists.Add((metadata?.Name.ToString(), file.Name));
                }
                return lists.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting lists: {ex.Message}");
                return new (string name, string id)[0];
            }
        }

        public static (ListMetadata Metadata, List<ListTask> Tasks) ReadList(string listId)
        {
            try
            {
                string taskFileContent = GetTaskFileContent(listId);
                if (taskFileContent != null)
                {
                    dynamic listData = JsonConvert.DeserializeObject<dynamic>(taskFileContent);
                    var metadata = JsonConvert.DeserializeObject<ListMetadata>(listData.listmetadata.ToString());
                    var tasks = JsonConvert.DeserializeObject<List<ListTask>>(listData.tasks.ToString());
                    return (metadata, tasks);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading list: {ex.Message}");
            }

            return (new ListMetadata(), new List<ListTask>());
        }

        public static string CreateList(string listName, int? groupId = 0, string emoji = "📋")
        {
            try
            {
                string listId = Guid.NewGuid().ToString();
                var metadata = new ListMetadata
                {
                    CreationDate = DateTime.UtcNow,
                    Name = GenerateUniqueListName(listName),
                    Emoji = emoji,
                    GroupID = groupId
                };
                SaveList(listId, new List<ListTask>(), metadata);
                ListCreatedEvent?.Invoke(listId, listName);
                return listId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating list: {ex.Message}");
                return null;
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
                Console.WriteLine($"Error deleting list: {ex.Message}");
            }
        }

        public static void RenameList(string listId, string newListName)
        {
            try
            {
                var (metadata, tasks) = ReadList(listId);
                string oldName = metadata.Name;
                metadata.Name = newListName;
                SaveList(listId, tasks, metadata);
                ListRenamedEvent.Invoke(listId, newListName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error renaming list: {ex.Message}");
            }
        }

        public static void ChangeListEmoji(string listId, string newEmoji)
        {
            try
            {
                var (metadata, tasks) = ReadList(listId);
                metadata.Emoji = newEmoji;
                SaveList(listId, tasks, metadata);
                ListEmojiChangedEvent?.Invoke(listId, newEmoji);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing list emoji: {ex.Message}");
            }
        }



        private static string GetFilePath(string listId)
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, $"{listId}.json");
        }

        public static string GetTaskFileContent(string listId)
        {
            string filePath = GetFilePath(listId);
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            return null;
        }

        public static async Task<StorageFile> ExportedLists()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
            ZipFile.CreateFromDirectory(localFolder.Path, $"{tempFolder.Path}\\Export.taskie");
            StorageFile exportedFile = await tempFolder.GetFileAsync("Export.taskie");
            await exportedFile.MoveAsync(localFolder, "Export.taskie", NameCollisionOption.ReplaceExisting);
            return exportedFile;
        }

        public static async void ImportFile(StorageFile file)
        {
            try
            {
                string content = await FileIO.ReadTextAsync(file);
                dynamic listData = JsonConvert.DeserializeObject<dynamic>(content);

                ListMetadata metadata = JsonConvert.DeserializeObject<ListMetadata>(listData.listmetadata.ToString());
                var tasks = JsonConvert.DeserializeObject<List<ListTask>>(listData.tasks.ToString());

                metadata.Name = GenerateUniqueListName(metadata.Name);

                string newListId;
                do
                {
                    newListId = Guid.NewGuid().ToString();
                }
                while (File.Exists(GetFilePath(newListId)));
                SaveList(newListId, tasks, metadata);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing file: {ex.Message}");
            }
        }

    }
}
