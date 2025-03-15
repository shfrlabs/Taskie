using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskieLib;
using Windows.ApplicationModel.Core;
using Windows.Security.Credentials.UI;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Taskie {
    public sealed partial class SettingsPage : Page {
        private bool isUpdating = false;

        public SettingsPage() {
            this.InitializeComponent();
            SetAppearance();
            SetSecurity();
            CheckSecurity();
            ActualThemeChanged += SettingsPage_ActualThemeChanged;
        }

        #region Feature avialability checks

        private async void CheckSecurity() {
            UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();
            if (availability != UserConsentVerifierAvailability.Available | !Settings.isPro) {
                AuthToggle.IsOn = false;
                AuthToggle.IsEnabled = false;
                Settings.isAuthUsed = false;
            }
        }

        #endregion

        #region Restoring switch/radio states

        private void SetSecurity() {
            AuthToggle.IsOn = Settings.isAuthUsed;
        }

        private void SetAppearance() {
            isUpdating = true;

            if (Settings.Theme == "System") {
                SystemRadio.IsChecked = true;
                DarkRadio.IsChecked = false;
                LightRadio.IsChecked = false;
            }
            else if (Settings.Theme == "Light") {
                SystemRadio.IsChecked = false;
                DarkRadio.IsChecked = false;
                LightRadio.IsChecked = true;
            }
            else if (Settings.Theme == "Dark") {
                SystemRadio.IsChecked = false;
                DarkRadio.IsChecked = true;
                LightRadio.IsChecked = false;
            }

            isUpdating = false;
        }

        #endregion

        #region Click and toggle handlers

        private void AuthToggleSwitch_Toggled(object sender, RoutedEventArgs e) {
            if ((sender as ToggleSwitch)?.Tag?.ToString() == "Auth") {
                Settings.isAuthUsed = (sender as ToggleSwitch).IsOn;
            }
        }

        private void RadioButton_StateChanged(object sender, RoutedEventArgs e) {
            if (isUpdating)
                return;

            var radioButton = sender as RadioButton;
            if (radioButton == null)
                return;

            string selectedTheme = radioButton.Tag?.ToString();

            if (radioButton.IsChecked == true) {
                isUpdating = true;
                Settings.Theme = selectedTheme;
                isUpdating = false;
            }
        }

        private async void export_Click(object sender, RoutedEventArgs e) {
            StorageFile exportFile = await ListTools.ExportedLists();
            FileSavePicker savePicker = new FileSavePicker {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            savePicker.SuggestedFileName = exportFile.Name;
            savePicker.FileTypeChoices.Add(exportFile.FileType, new List<string> { exportFile.FileType });
            StorageFile destinationFile = await savePicker.PickSaveFileAsync();
            if (destinationFile != null) {
                await exportFile.CopyAndReplaceAsync(destinationFile);
            }
            else {
            }
            File.Delete(exportFile.Path);
        }

        private async void import_Click(object sender, RoutedEventArgs e) {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".taskie");
            picker.FileTypeFilter.Add(".json");

            var files = await picker.PickMultipleFilesAsync();
            if (files != null) {
                foreach (StorageFile file in files) {
                    string fileExtension = Path.GetExtension(file.Name).ToLower();
                    if (fileExtension == ".json") {
                        ListTools.ImportFile(file);
                    }
                    else if (fileExtension == ".taskie") {
                        await ProcessTaskieFile(file);
                    }
                }
            }
        }

        private async void RestartButton_Click(object sender, RoutedEventArgs e) {
            var result = await CoreApplication.RequestRestartAsync("After import");
        }

        private async void RestartButtonTheme_Click(object sender, RoutedEventArgs e) {
            var result = await CoreApplication.RequestRestartAsync("Theme has been changed");
        }

        #endregion

        #region Other methods and events

        private async Task ProcessTaskieFile(StorageFile taskieFile) {
            using (var zipStream = await taskieFile.OpenStreamForReadAsync()) {
                using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read)) {
                    foreach (var entry in archive.Entries) {
                        if (Path.GetExtension(entry.FullName).ToLower() == ".json") {
                            using (var entryStream = entry.Open()) {
                                var memoryStream = new MemoryStream();
                                await entryStream.CopyToAsync(memoryStream);
                                memoryStream.Position = 0;
                                var unzippedFile = await CreateStorageFileFromStreamAsync(entry.FullName, memoryStream);
                                ListTools.ImportFile(unzippedFile);
                            }
                        }
                    }
                }
            }
        }

        private async Task<StorageFile> CreateStorageFileFromStreamAsync(string fileName, Stream stream) {
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var tempFile = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

            using (var fileStream = await tempFile.OpenStreamForWriteAsync()) {
                await stream.CopyToAsync(fileStream);
            }

            return tempFile;
        }

        private void SettingsPage_ActualThemeChanged(FrameworkElement sender, object args) {
            (this.Background as AcrylicBrush).TintColor = (Color)Application.Current.Resources["SystemAltHighColor"];
            (this.Background as AcrylicBrush).FallbackColor = (Color)Application.Current.Resources["SystemAltLowColor"];
        }

        private class SettingCategory {
            public string Emoji { get; set; }
            public string Name { get; set; }
            public string Page { get; set; }
        }

        #endregion
    }
}