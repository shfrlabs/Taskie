using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Documents;

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

        public delegate void ListCreated(string name);
        public static event ListCreated ListCreatedEvent;

        public delegate void ListDeleted(string name);
        public static event ListDeleted ListDeletedEvent;

        public delegate void ListRenamed(string oldname, string newname);
        public static event ListRenamed ListRenamedEvent;
        public static void SaveList(string listName, List<ListTask> list)
        {
            try
            {
                string filePath = GetFilePath(listName);
                File.WriteAllText(filePath, JsonConvert.SerializeObject(list));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving list: {ex.Message}");
            }
        }
        public static string[] GetLists()
        {
            try
            {
                string localFolderPath = ApplicationData.Current.LocalFolder.Path;
                DirectoryInfo info = new DirectoryInfo(localFolderPath);
                FileInfo[] files = info.GetFiles("*.json");
                List<string> lists = new List<string>();
                foreach (FileInfo file in files)
                {
                    lists.Add(file.Name.Replace(".json", ""));
                }
                return lists.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting lists: {ex.Message}");
                return new string[0];
            }
        }

        public static List<ListTask> ReadList(string listName)
        {
            try
            {
                string taskFileContent = GetTaskFileContent(listName);
                if (taskFileContent != null)
                {
                    return JsonConvert.DeserializeObject<List<ListTask>>(taskFileContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading list: {ex.Message}");
            }

            return new List<ListTask>();
        }
        public static string CreateList(string listName)
        {
            try
            {
                string newName = GenerateUniqueListName(listName);
                string filePath = GetFilePath(newName);
                File.Create(filePath).Close();
                ListCreatedEvent?.Invoke(newName);
                return newName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating list: {ex.Message}");
                return null;
            }
        }
        public static void DeleteList(string listName)
        {
            try
            {
                string filePath = GetFilePath(listName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    ListDeletedEvent.Invoke(listName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting list: {ex.Message}");
            }
        }
        public static void RenameList(string oldListName, string newListName)
        {
            try
            {
                string oldFilePath = GetFilePath(oldListName);
                string newFilePath = GetFilePath(newListName);

                if (File.Exists(oldFilePath))
                {
                    File.Move(oldFilePath, newFilePath);
                    ListRenamedEvent.Invoke(oldListName, newListName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error renaming list: {ex.Message}");
            }
        }

        private static string GenerateUniqueListName(string listName)
        {
            string uniqueName = listName;
            int count = 2;
            while (File.Exists(GetFilePath(uniqueName)))
            {
                uniqueName = $"{listName} ({count++})";
            }
            return uniqueName;
        }
        private static string GetFilePath(string listName)
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, $"{listName}.json");
        }

        public static string GetTaskFileContent(string listName)
        {
            string filePath = GetFilePath(listName);
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            return null;
        }

        public static async void ImportFile(StorageFile file)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                bool fileExists = false;
                string uniqueName = null;
                try
                {
                    await localFolder.GetFileAsync(file.Name);
                    fileExists = true;
                }
                catch (FileNotFoundException)
                {
                    fileExists = false;
                }
                if (fileExists)
                {
                    uniqueName = GenerateUniqueListName(file.Name.Replace(".json", null));
                }
                if (uniqueName == null)
                {
                    await file.CopyAsync(localFolder);
                }
                else
                {
                    await file.CopyAsync(localFolder, uniqueName + ".json");
                }
            }
            catch { }
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
    }
}
