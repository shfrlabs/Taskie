using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskieLib;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Taskie.SettingsPages
{
    public sealed partial class ExportImportPage : Page
    {
        public ExportImportPage()
        {
            this.InitializeComponent();
        }

        private async void export_Click(object sender, RoutedEventArgs e)
        {
            StorageFile exportFile = await Tools.ExportedLists();
            FileSavePicker savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            savePicker.SuggestedFileName = exportFile.Name;
            savePicker.FileTypeChoices.Add(exportFile.FileType, new List<string> { exportFile.FileType });
            StorageFile destinationFile = await savePicker.PickSaveFileAsync();
            if (destinationFile != null)
            {
                await exportFile.CopyAndReplaceAsync(destinationFile);
            }
            else
            {
            }
            File.Delete(exportFile.Path);
        }

        private async void import_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".taskie");
            picker.FileTypeFilter.Add(".json");

            var files = await picker.PickMultipleFilesAsync();
            if (files != null)
            {
                foreach (StorageFile file in files)
                {
                    string fileExtension = Path.GetExtension(file.Name).ToLower();
                    if (fileExtension == ".json")
                    {
                        Tools.ImportFile(file);
                    }
                    else if (fileExtension == ".taskie")
                    {
                        await ProcessTaskieFile(file);
                    }
                }
            }
        }

        private async Task ProcessTaskieFile(StorageFile taskieFile)
        {
            using (var zipStream = await taskieFile.OpenStreamForReadAsync())
            {
                using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (Path.GetExtension(entry.FullName).ToLower() == ".json")
                        {
                            using (var entryStream = entry.Open())
                            {
                                var memoryStream = new MemoryStream();
                                await entryStream.CopyToAsync(memoryStream);
                                memoryStream.Position = 0;
                                var unzippedFile = await CreateStorageFileFromStreamAsync(entry.FullName, memoryStream);
                                Tools.ImportFile(unzippedFile);
                            }
                        }
                    }
                }
            }
        }

        private async Task<StorageFile> CreateStorageFileFromStreamAsync(string fileName, Stream stream)
        {
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var tempFile = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

            using (var fileStream = await tempFile.OpenStreamForWriteAsync())
            {
                await stream.CopyToAsync(fileStream);
            }

            return tempFile;
        }


        private async void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await CoreApplication.RequestRestartAsync("After import");
        }
    }
}
